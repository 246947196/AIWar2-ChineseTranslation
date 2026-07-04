using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections;

namespace Arcen.AIW2.External
{
    //TEACHING_MOMENT

    //The CBSS is another port from AMU.
    //It's main design goal is to allow factions, game commands and everything else access to a budget-based, highly adaptive and very efficient ship spawning mechanic.
    //It posesses many properties that the ETDB could not (properly) implement efficiently.
    //On the surface it would seem simple, to make an ETDB spawn ships - after all it exists to draw entity types. But there are use cases for the ETDB and the CBSS out of the following differences:
    //The ETDB can dynamically change - If say the AI type is adaptive, and switches over to a new type the ETDB will, if using units from the budget types of the AI, switch to using those immediately.
    //The CBSS can be changed on demand to needs to add, remove, sort, or alter its contents, which would potentially desync an ETDB from host to client, or change spawning properties by bringing the elements out of order.
    //Thus the CBSS can "snapshot" an ETDB, turns its data into cached elements for greater efficiency and adds extra mechanics. In return the CBSS, due to its very purpose of spawning entities only can be used on the host.
    //This way potential desyncs or alteration of underlying data are prevented, while improving the calculation speed drastically at the same time.
    //The CBSS also saves "metadata" about the contents: Cheapest or most expensive items, a current item that budget potentially is accumulated for, the Planet Faction, World Location, etc to spawn with...
    //It requires this data to properly "precalculate" wheter it can afford something, or anything at all, or all in a tier, etc. Which would be impossible to do if the underlying data dynamically changes on its own.
    //In the same way the ETDB can handle multiple "budget types" (EntityTypeDrawingBag_SpawnMode), while for the CBSS all items must have the same factor the cost within is based upon.
    //
    //So at the end of the day:
    //---> Use ETDBs when it's about simply drawing an entity type.
    //---> Use CBSSs when it's about rapidly spawning ships on the map, with all sorts of starting properties, and whenever the spawn pools can dynamically change.
    //Thus in the end the CBSS is kind of a "snapshot ETDB with extra tricks and performance enhancements". The two are relatives, but not copies of one another, and need to be used correctly.
    //
    //
    //TEACHING_MOMENT To correctly set up a CBSS 3 steps must be taken:
    //
    //If you need a new one, get it from the pool. If it already exists, call ClearAndSetCreationParameters
    //Add, remove, modify, do whatever you want to do with its contents
    //When all of that is done call FinalizeCreation
    //
    //And then it's ready to be used. Whenever the inner item pool is modified, at the end of all modifications FinalizeCreation must be called.
    //Many of the normal spawn parameters can be changed, but for the initial caching of the data when big changes must be made these must be altered with ClearAndSetCreationParameters
    //-NR-SirLimbo ToDo: Make a variant of ClearAndSetCreationParameters WITHOUT the clear-part! Additionally, add serialization functions to it!
    //Additionally, to change the min/max tiers, use AdjustTiers. This is quite fast, so tier changes can be doe quickly.
    //
    //
    //Spawning is done in 2 steps:
    //First all the ships are "recorded". This means nothing that make a list of "I want X amount of ship type Y".
    //After all recording for this "session" has been done the ships must be spawned. The nice thing about doing it this way is that the ships will potentially come pre-stacked and thus waaay faster than otherwise.
    public class CostBasedShipSpawner : IConcurrentPoolable<CostBasedShipSpawner>
    {
        public enum WeightMode
        {
            NoAutoAdjust,
            AutoAdjust_CostIsWeight,
            AutoAdjust_CostIsInvertedWeight,
        }

        //Keeps the data on what something costs to be spawned. Since this is a struct there is no need to worry about returning direct references that could be altered
        public struct SpawnData
        {
            //This holds all the info required to give the CBSS the info it needs to properly calculate everything
            public static int GetTotalCost( GameEntityTypeData Type, byte MarkToSpawnAt, EntityTypeDrawingBag_SpawnMode Factor, bool SpawnDroneFleetsFull )
            {
                int cost = EntityTypeDrawingBag_DrawLinkImplementation.GetCostFactorForTypeAndCost( Factor, Type, MarkToSpawnAt );
                if( SpawnDroneFleetsFull && Type.FleetDesignTemplateIUseForDrones != null )
                {
                    foreach ( FleetItem item in Type.FleetDesignTemplateIUseForDrones.FleetItems() )
                    {
                        cost += GetTotalCost( item.TypeData, MarkToSpawnAt, Factor, false );
                    }
                }
                return cost;
            }

            public GameEntityTypeData Type;
            public float CumulativeWeightUpToAndIncludingThisItem;
            public int Cost;

            public SpawnData( GameEntityTypeData Type, byte MarkToSpawnAt, EntityTypeDrawingBag_SpawnMode Factor, bool SpawnDroneFleetsFull, float OverrideWeight = 0 )
            {
                this.Type = Type;
                this.CumulativeWeightUpToAndIncludingThisItem = OverrideWeight;
                this.Cost = GetTotalCost( Type, MarkToSpawnAt, Factor, SpawnDroneFleetsFull );
            }

            public SpawnData( GameEntityTypeData Type, float CumulativeWeightUpToAndIncludingThisItem, int Cost )
            {
                this.Type = Type;
                this.CumulativeWeightUpToAndIncludingThisItem = CumulativeWeightUpToAndIncludingThisItem;//At the start the weight is "normal", but later on the weight is more absolute
                this.Cost = Cost;
            }
        }

        public class SpawnDataDrawList : IList<SpawnData>
        {
            //IMPORTANT: The Weights are organized in such a way that each index is the weight UP TO THIS POINT. So out of a lists with weights equalling 100, 150, 50 the weights contained would become 100, 250, 300!
            //This speeds up getting the maximum possible weight up to X amount of budget. Effectively each weight contained in the end is the cumulative weight up to and including this point.
            protected ThrowawayListCanMemLeak<SpawnData> InternalList;
            public float Weight;

            public bool InitializationHasBeenFinished { get; protected set; } = false;
            public int MinCost { get; protected set; }
            public int MaxCost { get; protected set; }
            public int CumulativeCost { get; protected set; }
            public float AverageCost { get; protected set; }
            public float CumulativeInternalWeight { get; protected set; }

            public int Count => InternalList.Count;

            public SpawnDataDrawList( int Capacity )
            {
                InternalList = new ThrowawayListCanMemLeak<SpawnData>( Capacity );
            }

            public SpawnDataDrawList( int Capacity, float Weight ) : this( Capacity )
            {
                this.Weight = Weight;
            }

            public SpawnData this[int Index]
            {
                get
                {
                    return InternalList[Index];
                }
                set
                {
                    InitializationHasBeenFinished = false;
                    InternalList[Index] = value;
                }
            }

            public SpawnData Draw( ArcenHostOnlySimContext Context )
            {
                return Draw( Context, int.MaxValue );
            }

            public SpawnData Draw( ArcenHostOnlySimContext Context, int Budget )
            {
                if ( !InitializationHasBeenFinished )
                    throw new Exception( "Error: Tried to draw from an uninitialized " + GetType().Name + "!" );
                if ( InternalList.Count == 0 )
                    throw new Exception( "Error: Tried to draw from empty " + GetType().Name + "!" );
                //if ( Budget < MinCost )
                //    throw new Exception( "Error: Tried to with " + Budget + " Budget, but this drawing bag's items cost is at least " + MinCost + "!" );
                if ( InternalList.Count == 1 )
                    return InternalList[0];
                float drawn;
                float max = -1;
                if ( Budget >= MaxCost )
                {
                    max = InternalList[InternalList.Count - 1].CumulativeWeightUpToAndIncludingThisItem;
                } else
                {
                    for ( int i = 1; i < InternalList.Count; i++ )
                    {
                        if ( Budget < InternalList[i].Cost )
                        {
                            max = InternalList[i - 1].CumulativeWeightUpToAndIncludingThisItem;
                            break;
                        }
                    }
                }
                drawn = Context.RandomToUse.NextFloat( max );
                for ( int i = 0; i < InternalList.Count; i++ )
                {
                    if ( drawn <= InternalList[i].CumulativeWeightUpToAndIncludingThisItem )
                        return InternalList[i];
                }
                throw new Exception( "Error in Draw: Max did not match the actual maximum weight, it was too high at a value of " + max + ", drew " + drawn + "!" );
            }

            public void Add( SpawnData Item )
            {
                InitializationHasBeenFinished = false;
                InternalList.Add( Item );
            }

            public void AddRange( System.Collections.Generic.IEnumerable<SpawnData> Item )
            {
                InitializationHasBeenFinished = false;
                InternalList.AddRange( Item );
            }

            public int IndexOf( SpawnData Item )
            {
                return InternalList.IndexOf( Item );
            }

            public void Insert( int Index, SpawnData Item )
            {
                InitializationHasBeenFinished = false;
                InternalList.Insert( Index, Item );
            }

            public bool RemoveAt( int Index, bool ThrowErrors = false )
            {
                InitializationHasBeenFinished = false;
                return InternalList.RemoveAt( Index, ThrowErrors );
            }

            public void Clear()
            {
                InitializationHasBeenFinished = false;
                InternalList.Clear();
            }

            public bool Contains( SpawnData Item )
            {
                throw new NotImplementedException("If you need this implement it, and don't call InternalList.Contains because that boxes every single element in its implementation.");
            }

            public void CopyTo( SpawnData[] Array, int Index )
            {
                InternalList.CopyTo( Array, Index );
            }

            public void CopyFrom( System.Collections.Generic.IEnumerable<SpawnData> List )
            {
                InitializationHasBeenFinished = false;
                this.InternalList.Clear();
                this.InternalList.AddRange( List );
            }

            public void Finalize( WeightMode WeightAutoAdjusting )
            {
                //Lowest cost first
                InternalList.Sort( static delegate ( SpawnData A, SpawnData B )
                {
                    return A.Cost - B.Cost;
                } );

                //Adjust weights
                SpawnData current;
                if ( WeightAutoAdjusting == WeightMode.AutoAdjust_CostIsWeight )
                {
                    float weightUntilHere = 0;
                    for ( int i = 0; i < InternalList.Count; i++ )
                    {
                        current = InternalList[i];
                        weightUntilHere += current.Cost;
                        InternalList[i] = new SpawnData( current.Type, weightUntilHere, current.Cost );
                    }
                } else if ( WeightAutoAdjusting == WeightMode.AutoAdjust_CostIsInvertedWeight )
                {
                    long cumulativeCost = 0;
                    foreach ( SpawnData Data in InternalList )
                        cumulativeCost += Data.Cost;
                    float weightUntilHere = 0;
                    float currentItemWeight;
                    for ( int i = 0; i < InternalList.Count; i++ )
                    {
                        current = InternalList[i];
                        currentItemWeight = cumulativeCost / (float) current.Cost;
                        weightUntilHere += currentItemWeight;
                        InternalList[i] = new SpawnData( current.Type, weightUntilHere, current.Cost );
                    }
                } else
                {
                    float weightUntilHere = 0;
                    for ( int i = 0; i < InternalList.Count; i++ )
                    {
                        current = InternalList[i];
                        weightUntilHere += current.Cost;
                        InternalList[i] = new SpawnData( current.Type, weightUntilHere, current.Cost );
                    }
                }

                CumulativeCost = 0;
                //Final checks
                for ( int i = 0; i < InternalList.Count; i++ )
                {
                    current = InternalList[i];
                    if ( current.Type == null )
                        throw new Exception( "Error at index " + i + ": Entity type in Data item was null! Used " + WeightAutoAdjusting );
                    if ( current.Cost <= 0 )
                        throw new Exception( "Error at index " + i + ": Cost in Data item was <= 0! Used " + WeightAutoAdjusting );
                    if ( current.CumulativeWeightUpToAndIncludingThisItem <= 0 )
                        throw new Exception( "Error at index " + i + ": Weight in Data item was <= 0! Used " + WeightAutoAdjusting );
                    if ( i > 0 && current.CumulativeWeightUpToAndIncludingThisItem <= InternalList[i - 1].CumulativeWeightUpToAndIncludingThisItem )
                        throw new Exception( "Error at index " + i + ": Weight of item at index " + (i - 1) + " was greater: " +
                            current.CumulativeWeightUpToAndIncludingThisItem + " < " + InternalList[i - 1].CumulativeWeightUpToAndIncludingThisItem + "! Used " + WeightAutoAdjusting );
                    CumulativeCost += InternalList[i].Cost;
                }

                //In theory all of these can be taken from the list quickly, but its cleaner and even faster to cache
                CumulativeInternalWeight = InternalList[InternalList.Count - 1].CumulativeWeightUpToAndIncludingThisItem;
                MinCost = InternalList[0].Cost;
                MaxCost = InternalList[InternalList.Count - 1].Cost;
                AverageCost = CumulativeCost / (float) InternalList.Count;

                //The list is now active
                InitializationHasBeenFinished = true;
            }

            public ArcenCharacterBufferBase WriteToBuffer( ArcenCharacterBufferBase Buffer, int Index )
            {
                Buffer.Add( "\n\t" ).Add( GetType().Name ).Add( Index ).Add( ":" )
                    .Add( "\n\t\tWeight: " ).Add( Weight )
                    .Add( "\n\t\tCosts: " ).Add( MinCost ).Add( " - " ).Add( MaxCost ).Add( ", Cumulative: " ).Add( CumulativeCost ).Add( ", Average: " ).Add( AverageCost )
                    .Add( "\n\t\tCumulativeInternalWeight: " ).Add( CumulativeInternalWeight )
                    .Add( "\n\t\tContents:" ); ;
                float lastCost = 0;
                for ( int i = 0; i < InternalList.Count; i++ )
                {
                    Buffer.Add( "\n\t\t\t" ).Add( InternalList[i].Type.DisplayNameForSidebar ).Add( " / Cost = " ).Add( InternalList[i].Cost ).Add( " / Internal Weight: " )
                        .Add( InternalList[i].CumulativeWeightUpToAndIncludingThisItem ).Add( " / Factual Weight: " ).Add( InternalList[i].CumulativeWeightUpToAndIncludingThisItem - lastCost );
                    lastCost = InternalList[i].CumulativeWeightUpToAndIncludingThisItem;
                }
                return Buffer;
            }

            #region IEnumerable

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return InternalList.GetEnumerator();
            }

            System.Collections.Generic.IEnumerator<SpawnData> System.Collections.Generic.IEnumerable<SpawnData>.GetEnumerator()
            {
                return InternalList.GetEnumerator();
            }

            #endregion
        }

        public string InternalName { get; protected set; }

        protected List<GameEntityTypeData> ListForBuilding = List<GameEntityTypeData>.Create_WillNeverBeGCed( 30, "CostBasedShipSpawner-ListForBuilding" );
        protected List<SpawnDataDrawList> DrawData = List<SpawnDataDrawList>.Create_WillNeverBeGCed( 30, "CostBasedShipSpawner-DrawData" );
        protected float CumulativeWeight;

        public bool InitializationHasBeenFinished { get; protected set; } = false;

        //Creation-only Parameters:
        public Faction AIFaction_ForAIBudgets { get; protected set; }
        public WeightMode WeightAutoAdjustingForTierList { get; protected set; }
        public WeightMode WeightAutoAdjustingForTiers { get; protected set; }

        //Settings for spawning itself
        public bool CanGetIntoDebt;
        public bool PreferBigOnTargetedSpawns;
        public int MaxTier { get; protected set; }
        public int MinTier { get; protected set; }

        public byte MarkToSpawnAt { get; protected set; }
        public EntityTypeDrawingBag_SpawnMode Factor { get; protected set; }
        public bool SpawnDroneFleetsFull { get; protected set; }
        public SpawnData CurrentSpawnTarget;
        public int[] RecordedSpawns = null;//The index here is the entity type's row index

        //Cached values
        public int SmallestSpendingPossible { get; protected set; }
        public int LargestSpendingPossible { get; protected set; }

        //Important spawned ship properties (must ALWAYS be set to something!)
        public PlanetFaction PFaction;
        public ArcenPoint CentralSpawnPoint;

        //Optional spawned ship properties (can be set, but needn't, some are simply default settings)
        public Fleet FleetToSpawnInto = null;
        public Fireteam FireteamToSpawnInto = null;
        public AngleDegrees CurrentAngle = AngleDegrees.MinAngle;
        public EntityBehaviorType Behavior = EntityBehaviorType.Attacker_Full;
        public SpawnVisualization SpawnVisuals = SpawnVisualization.Normal;
        public short BehaviorTarget = -1;
        public bool CountAsThreat = false;
        public int MaxActualSpawnDistanceFromSpawnPoint = 0;
        public StateOfMatterTypeData StateOfMatter = null;

        //After all the setting of data has been finished the lists need to be sorted and the cached values updated!
        public void AddData( int Tier, GameEntityTypeData Type, float OverrideWeight = 0 )
        {
            int debugStage = 0;
            InitializationHasBeenFinished = false;
            debugStage = 1;
            var data = new SpawnData( Type, MarkToSpawnAt, Factor, SpawnDroneFleetsFull, OverrideWeight );
            debugStage = 2;
            AddData( Tier, data );
            debugStage = 3;

            if ( debugStage > 0 ) { }//do nothing, but quiet the error
        }

        //Since SpawnData is only a struct which will be cloned on getting there's no way for the user to manipulate the data here
        public SpawnData GetDataFor( int Tier, int IndexInTier )
        {
            return DrawData[Tier][IndexInTier];
        }

        //After all the setting of data has been finished the lists need to be sorted and the cached values updated!
        public void AddData( int Tier, SpawnData NewData )
        {
            InitializationHasBeenFinished = false;
            DrawData[Tier].Add( NewData );
        }

        //After all the setting of data has been finished the lists need to be sorted and the cached values updated!
        public void SetData( int Tier, int IndexInTier, SpawnData NewData )
        {
            InitializationHasBeenFinished = false;
            DrawData[Tier][IndexInTier] = NewData;
        }

        //After all the setting of data has been finished the lists need to be sorted and the cached values updated!
        public void RemoveAt( int Tier, int IndeXinTier )
        {
            InitializationHasBeenFinished = false;
            DrawData[Tier].RemoveAt( IndeXinTier );
        }

        //After all the setting of data has been finished the lists need to be sorted and the cached values updated!
        public void AddTier( System.Collections.Generic.IEnumerable<SpawnData> ToCopyFrom )
        {
            InitializationHasBeenFinished = false;
            DrawData.Add( new SpawnDataDrawList( 30 ) );
            DrawData[DrawData.Count - 1].CopyFrom( ToCopyFrom );
        }

        //After all the setting of data has been finished the lists need to be sorted and the cached values updated!
        public void SetTier( int Tier, System.Collections.Generic.IEnumerable<SpawnData> ToCopyFrom )
        {
            InitializationHasBeenFinished = false;
            DrawData[Tier].CopyFrom( ToCopyFrom );
        }

        //After all the setting of data has been finished the lists need to be sorted and the cached values updated!
        public void AddToTier( int Tier, System.Collections.Generic.IEnumerable<SpawnData> ToAddFrom )
        {
            InitializationHasBeenFinished = false;
            DrawData[Tier].AddRange( ToAddFrom );
        }

        //After all the setting of data has been finished the lists need to be sorted and the cached values updated!
        public void AddTier( System.Collections.Generic.IEnumerable<GameEntityTypeData> ToCopyFrom, float OverrideWeight = 0 )
        {
            InitializationHasBeenFinished = false;
            SpawnDataDrawList lst = new SpawnDataDrawList( 30 );
            DrawData.Add( lst );
            foreach ( GameEntityTypeData type in ToCopyFrom )
                lst.Add( new SpawnData( type, MarkToSpawnAt, Factor, SpawnDroneFleetsFull, OverrideWeight ) );
        }

        //After all the setting of data has been finished the lists need to be sorted and the cached values updated!
        public void SetTier( int Tier, System.Collections.Generic.IEnumerable<GameEntityTypeData> ToCopyFrom, float OverrideWeight = 0 )
        {
            InitializationHasBeenFinished = false;
            SpawnDataDrawList lst = DrawData[Tier];
            lst.Clear();
            foreach ( GameEntityTypeData type in ToCopyFrom )
                lst.Add( new SpawnData( type, MarkToSpawnAt, Factor, SpawnDroneFleetsFull, OverrideWeight ) );
        }

        //After all the setting of data has been finished the lists need to be sorted and the cached values updated!
        public void AddToTier( int Tier, System.Collections.Generic.IEnumerable<GameEntityTypeData> ToAddFrom, float OverrideWeight = 0 )
        {
            InitializationHasBeenFinished = false;
            SpawnDataDrawList lst = DrawData[Tier];
            foreach ( GameEntityTypeData type in ToAddFrom )
                lst.Add( new SpawnData( type, MarkToSpawnAt, Factor, SpawnDroneFleetsFull, OverrideWeight ) );
        }

        //After all the setting of data has been finished the lists need to be sorted and the cached values updated!
        public void SetTierWeight( int Tier, float Weight )
        {
            InitializationHasBeenFinished = false;
            DrawData[Tier].Weight = Weight;
        }

        //Getters for various things
        public float GetTierWeight( int Tier )
        {
            return DrawData[Tier].Weight;
        }

        public int GetTierCount( int Tier )
        {
            return DrawData[Tier].Count;
        }

        public int GetTierMinCost( int Tier )
        {
            return DrawData[Tier].MinCost;
        }

        public int GetTierMaxCost( int Tier )
        {
            return DrawData[Tier].MaxCost;
        }

        //After all the setting of data has been finished the lists need to be sorted and the cached values updated!
        public void RemoveTier( int Tier )
        {
            InitializationHasBeenFinished = false;
            DrawData.RemoveAt( Tier );
        }

        //After all the setting of data has been finished the lists need to be sorted and the cached values updated!
        public void EnsureTierCapacity( int MaxTier )
        {
            InitializationHasBeenFinished = false;
            while ( DrawData.Count <= MaxTier )
                DrawData.Add( new SpawnDataDrawList( 30 ) );
        }

        public void ClearAndSetCreationParameters( byte MarkToSpawnAt, EntityTypeDrawingBag_SpawnMode Factor, Faction AIFaction_ForAIBudgets, bool SpawnDroneFleetsFull,
            WeightMode WeightAutoAdjustingForTierList, WeightMode WeightAutoAdjustingForTiers, int MinTier, int MaxTier )
        {
            InitializationHasBeenFinished = false;
            this.MarkToSpawnAt = MarkToSpawnAt;
            this.Factor = Factor;
            this.AIFaction_ForAIBudgets = AIFaction_ForAIBudgets;
            this.SpawnDroneFleetsFull = SpawnDroneFleetsFull;
            this.WeightAutoAdjustingForTierList = WeightAutoAdjustingForTierList;
            this.WeightAutoAdjustingForTiers = WeightAutoAdjustingForTiers;
            this.MinTier = MinTier;
            this.MaxTier = MaxTier;
            DrawData.Clear();
            EnsureTierCapacity(MaxTier);
            ClearRecordedSpawns();
        }

        //After all the setting of data has been finished the lists need to be sorted and the cached values updated!
        //-NR-SirLimbo TODO: Potentially make a variant that finds the accurate weight for each unit! Would be annoying but awesome!
        public void AddSetOfData( int StartingTier, EntityTypeDrawingBag Bag, float OverrideWeightForTier = 0, float OverrideWeightForItems = 0 )
        {
            int end = StartingTier + Bag.Count;
            EnsureTierCapacity( end );
            for ( int i = StartingTier; i < end; i++ )
            {
                Bag.FillAllPossibleEntityTypes_ForSpecificIndex( ListForBuilding, AIFaction_ForAIBudgets, i );
                AddToTier( i, ListForBuilding, OverrideWeightForItems );
                if ( OverrideWeightForTier > 0 )
                    DrawData[i].Weight = OverrideWeightForTier;
            }
        }

        //After all the setting of data has been finished the lists need to be sorted and the cached values updated!
        public void AddEntityByName( int Tier, string EntityName, float OverrideWeightForItem = 0 )
        {
            GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRowByName( EntityName );
            if ( typeData == null )
                throw new Exception( "Error in " + InternalName + ": Tried to add an entity named " + EntityName + " but it did not exist." );
            EnsureTierCapacity( Tier );
            AddData( Tier, typeData, OverrideWeightForItem );
        }

        //After all the setting of data has been finished the lists need to be sorted and the cached values updated!
        public void AddTiersByEntityNames( int StartingTier, System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<string>> EntityNamesByTier,
            float OverrideWeightForTier = 0, float OverrideWeightForItems = 0 )
        {
            foreach ( System.Collections.Generic.IEnumerable<string> entityNamesOfTier in EntityNamesByTier )
            {
                foreach ( string entityName in entityNamesOfTier )
                    AddEntityByName( StartingTier, entityName, OverrideWeightForItems );
                if ( OverrideWeightForTier > 0 )
                    DrawData[StartingTier].Weight = OverrideWeightForTier;
                StartingTier++;
            }
        }

        //After all the setting of data has been finished the lists need to be sorted and the cached values updated!
        public void AddAllEntitiesWithTag( int Tier, string Tag, float OverrideWeightForItems = 0 )
        {
            List<GameEntityTypeData> list = GameEntityTypeDataTable.Instance.RowsByTag[Tag];
            if ( list == null || list.Count <= 0 )
                throw new Exception( "Error in " + InternalName + ": Tried to add all entities with a tag " + Tag + " but nothing was tagged with it." );
            EnsureTierCapacity( Tier );
            AddToTier( Tier, list, OverrideWeightForItems );
        }

        //After all the setting of data has been finished the lists need to be sorted and the cached values updated!
        public void AddTiersByEntityTags( int StartingTier, System.Collections.Generic.IEnumerable<string> TagsByTier, float OverrideWeightForTier = 0, float OverrideWeightForItems = 0 )
        {
            foreach ( string tag in TagsByTier )
            {
                AddAllEntitiesWithTag( StartingTier, tag, OverrideWeightForItems );
                if ( OverrideWeightForTier > 0 )
                    DrawData[StartingTier].Weight = OverrideWeightForTier;
                StartingTier++;
            }
        }

        //After all the setting of data has been finished the lists need to be sorted and the cached values updated!
        public void AddAllEntitiesFromFleetTemplate( int Tier, string TemplateName, float OverrideWeightForTier = 0, float OverrideWeightForItems = 0 )
        {
            FleetDesignTemplate template = FleetDesignTemplateTable.Instance.GetRowByName( TemplateName );
            if ( template == null )
                throw new Exception( "Error in " + InternalName + ": Tried to add all a FleetDesignTemplate with name " + template + " but but it did not exist." );
            AddAllEntitiesFromFleetTemplate( Tier, template, OverrideWeightForTier, OverrideWeightForItems );
        }

        //After all the setting of data has been finished the lists need to be sorted and the cached values updated!
        public void AddAllEntitiesFromFleetTemplate( int Tier, FleetDesignTemplate Template, float OverrideWeightForTier = 0, float OverrideWeightForItems = 0 )
        {
            EnsureTierCapacity( Tier );
            if ( OverrideWeightForTier > 0 )
                DrawData[Tier].Weight = OverrideWeightForTier;
            else
                DrawData[Tier].Weight = Template.WeightInDrawBags;
            if ( OverrideWeightForItems > 0 )
            {
                foreach ( FleetItem Item in Template.FleetItems() )
                {
                    AddData( Tier, Item.TypeData, OverrideWeightForItems );
                }
            } else
            {
                float totalWeight = 0;
                foreach ( FleetItem Item in Template.FleetItems() )
                {
                    totalWeight += Item.WeightInDrawBags;
                }
                foreach ( FleetItem Item in Template.FleetItems() )
                {
                    AddData( Tier, Item.TypeData, (Item.WeightInDrawBags * 100) / totalWeight );
                }
            }
        }

        //This sets the current data as "permanent" and gives the CBSS free for use
        public void FinalizeCreation()
        {
            //Initial removals and checks
            int removed = 0;
            for ( int i = 0; i < DrawData.Count; i++ )
            {
                if ( DrawData[i].Count <= 0 )
                {
                    if ( i >= MinTier )
                        MinTier--;
                    if ( i >= MaxTier )
                        MaxTier--;
                    DrawData.RemoveAt( i );
                    removed++;
                }
            }
            if ( DrawData.Count <= 0 )
                throw new Exception( "Error in " + InternalName + ": Attempted to sort and update cached values of an empty CostBasedShipSpawner! Removed " + removed + " empty tiers." );

            for ( int i = 0; i < DrawData.Count; i++ )
                DrawData[i].Finalize( WeightAutoAdjustingForTiers );

            if ( WeightAutoAdjustingForTierList == WeightMode.AutoAdjust_CostIsWeight )
            {
                for ( int i = 0; i < DrawData.Count; i++ )
                    DrawData[i].Weight = DrawData[i].CumulativeCost;
            } else if ( WeightAutoAdjustingForTierList == WeightMode.AutoAdjust_CostIsInvertedWeight )
            {
                float cumulativeCost = 0;
                foreach ( SpawnDataDrawList DataList in DrawData )
                    cumulativeCost += DataList.CumulativeCost;
                cumulativeCost *= 100;
                float cumulativeDivsOfTier;
                foreach ( SpawnDataDrawList DataList in DrawData )
                {
                    cumulativeDivsOfTier = 0;
                    for ( int i = 0; i < DataList.Count; i++ )
                        cumulativeDivsOfTier += cumulativeCost / DataList[i].Cost;
                    DataList.Weight = cumulativeDivsOfTier;
                }
            }

            for ( int i = 0; i < DrawData.Count; i++ )
            {
                if ( DrawData[i].Weight <= 0 )
                    throw new Exception( "Error in " + InternalName + ": DrawList item " + i + " had a weight of " + DrawData[i].Weight + ", must be >0! Used " + WeightAutoAdjustingForTiers );
            }

            //Tier adjusting. This is separate because it can happen more often and requires (by comparison) little recalculation
            AdjustTiers( MinTier, MaxTier );

            InitializationHasBeenFinished = true;
        }

        //When adjusting tiers the cached values must also be adjusted!
        public void AdjustTiers( int MinTier, int MaxTier )
        {
            if ( MinTier < 0 )
                throw new Exception( "Error in " + InternalName + ": MinTier was " + MinTier + ", must be >= 0!" );
            if ( MaxTier < 0 )
                throw new Exception( "Error in " + InternalName + ": MaxTier was " + MaxTier + ", must be >= 0!" );
            if ( MinTier >= DrawData.Count )
                MinTier = DrawData.Count - 1;
            if ( MaxTier >= DrawData.Count )
                MaxTier = DrawData.Count - 1;
            if ( MinTier > MaxTier )
                throw new Exception( "Error in " + InternalName + ": MaxTier was " + MaxTier + ", which was lower than MinTier at " + MinTier + "!" );
            this.MinTier = MinTier;
            this.MaxTier = MaxTier;

            SmallestSpendingPossible = int.MaxValue;
            LargestSpendingPossible = 0;
            CumulativeWeight = 0;
            int tmp;
            for ( int i = MinTier; i <= MaxTier; i++ )
            {
                tmp = DrawData[i].MinCost;
                if ( SmallestSpendingPossible > tmp )
                    SmallestSpendingPossible = tmp;
                tmp = DrawData[i].MaxCost;
                if ( LargestSpendingPossible < tmp )
                    LargestSpendingPossible = tmp;
                CumulativeWeight += DrawData[i].Weight;
            }
        }

        //Centralized drawing methods for ease of use
        public SpawnDataDrawList DrawTierWithinLimits( ArcenHostOnlySimContext Context )
        {
            return DrawTierWithinLimits( Context, int.MaxValue );
        }

        public SpawnDataDrawList DrawTierWithinLimits( ArcenHostOnlySimContext Context, int MaxCost )
        {
            //if ( SmallestSpendingPossible > MaxCost )
            //    throw new Exception( "Error in " + InternalName + ": Could not draw anything, was asked to purchase something with MaxCost " + MaxCost + ", when nothing is cheaper than " +
            //        SmallestSpendingPossible + ". Tiers are " + MinTier + " to " + MaxTier + ", DrawData list count is " + DrawData.Count );
            if ( MinTier == MaxTier )
                return DrawData[MinTier];
            float rnd;
            if ( LargestSpendingPossible <= MaxCost )
            {
                rnd = Context.RandomToUse.NextFloat( CumulativeWeight );
                for ( int i = MinTier; i <= MaxTier; i++ )
                {
                    rnd -= DrawData[i].Weight;
                    if ( rnd <= 0 )
                        return DrawData[i];
                }
                throw new Exception( "Error in " + InternalName + ": Was asked to draw something at a random value of " + rnd +
                    ", but ran out of list size while counting up. Check the calculations for CumulativeWeight (" + CumulativeWeight + ") for errors!" );
            } else
            {
                float max = 0;
                for ( int i = MinTier; i <= MaxTier; i++ )
                {
                    if ( DrawData[i].MinCost <= MaxCost )
                        max += DrawData[i].Weight;
                }
                rnd = Context.RandomToUse.NextFloat( max );
                for ( int i = MinTier; i <= MaxTier; i++ )
                {
                    if ( DrawData[i].MinCost > MaxCost )
                        continue;
                    rnd -= DrawData[i].Weight;
                    if ( rnd <= 0 )
                        return DrawData[i];
                }
                throw new Exception( "Error in " + InternalName + ": Was asked to draw something at a random value of " + rnd + " (up to " + max +
                    "), but ran out of list size while counting up. Check the code path for calculating the max for errors!" );
            }
        }

        //For ease of use and correctly respecting the debt mechanic
        public bool CanSpend( int Budget, int SpawnCost )
        {
            return SpawnCost <= Budget || (CanGetIntoDebt && Budget > 0);
        }

        public bool CanSpendCurrent( int Budget )
        {
            return CanSpend( Budget, CurrentSpawnTarget.Cost );
        }

        public bool CanSpendCurrentAndXMore( int Budget, int NextSpawnCost )
        {
            return CanSpend( Budget - CurrentSpawnTarget.Cost, NextSpawnCost );
        }

        public void ClearRecordedSpawns()
        {
            Array.Clear(RecordedSpawns, 0, RecordedSpawns.Length);
        }

        //The following all are functions to record ships with various properties
        public void RecordCurrent( ref int Budget )
        {
            Record( ref Budget, CurrentSpawnTarget );
        }

        public void RecordCurrent()
        {
            Record( CurrentSpawnTarget );
        }

        public void Record( ref int Budget, SpawnData CurrentSpawnData )
        {
            if ( !CanSpend( Budget, CurrentSpawnData.Cost ) )
                throw new Exception( "Error in " + InternalName + ": Was asked to use " + Budget + " to spawn a " + CurrentSpawnTarget.Type + ", but it cost " + CurrentSpawnTarget.Cost + " and thus failed!" );
            Budget -= CurrentSpawnData.Cost;
            Record( CurrentSpawnData );
        }

        public void Record( SpawnData CurrentSpawnData )
        {
            if ( CurrentSpawnData.Type == null )
                throw new Exception( "Error in " + InternalName + ": Tried to spawn a new item - but its entity type was null, probably the default item!" );
            RecordedSpawns[CurrentSpawnData.Type.RowIndexNonSim]++;
        }

        protected void Record_SpendBudgetOnSpecificTier( ref int Budget, int Tier, ArcenHostOnlySimContext Context )
        {
            if ( Tier > DrawData.Count )
                throw new Exception( "Error in " + InternalName + ": Tried to spawn with tier " + Tier + ", which does not exist. DrawData count is only " + DrawData.Count + "!" );
            SpawnDataDrawList tier = DrawData[Tier];
            int minCost = tier.MinCost;
            if ( !CanSpend( Budget, minCost ) )
                throw new Exception( "Error in " + InternalName + ": Tried to spawn with tier " + Tier + " and Budget " + Budget + ", which is not even enough to afford the " + minCost + " minCost to spawn anything!" );
            SpawnData data;
            while ( true )
            {
                data = tier.Draw( Context, Budget );
                if ( !CanSpend( Budget, data.Cost ) )
                    return;
                Record( ref Budget, data );
            }
        }
        protected void Record_AmountOfSpecificTier( int SpawnCount, int Tier, ArcenHostOnlySimContext Context )
        {
            if ( Tier > DrawData.Count )
                throw new Exception( "Error in " + InternalName + ": Tried to spawn with tier " + Tier + ", which does not exist. DrawData count is only " + DrawData.Count + "!" );
            SpawnDataDrawList tier = DrawData[Tier];
            for ( int i = 0; i < SpawnCount; i++ )
                Record( tier.Draw( Context ) );
        }
        protected void Record_AmountOfSpecificTierUpToBudgetLimit( ref int Budget, int SpawnCount, int Tier, ArcenHostOnlySimContext Context )
        {
            if ( Tier > DrawData.Count )
                throw new Exception( "Error in " + InternalName + ": Tried to spawn with tier " + Tier + ", which does not exist. DrawData count is only " + DrawData.Count + "!" );
            SpawnDataDrawList tier = DrawData[Tier];
            int minCost = tier.MinCost;
            if ( !CanSpend( Budget, minCost ) )
                throw new Exception( "Error in " + InternalName + ": Tried to spawn with tier " + Tier + " and Budget " + Budget + ", which is not even enough to afford the " + minCost + " minCost to spawn anything!" );
            SpawnData data;
            for ( int i = 0; i < SpawnCount; i++ )
            {
                data = tier.Draw( Context, Budget );
                if ( !CanSpend( Budget, data.Cost ) )
                    return;
                Record( ref Budget, data );
            }
        }

        //Takes a list of ship counts per tier and tries to spawn them with the budget given, returning whatever is left over before no more can be spawned.
        public void RecordShips_AmountPerTier( int[] ShipsPerTier, ArcenHostOnlySimContext Context )
        {
            if ( ShipsPerTier.Length > DrawData.Count )
                throw new Exception( "Error in " + InternalName + ": Tried to spawn with tiers 0 - " + ShipsPerTier.Length + " but DrawData count is only " + DrawData.Count + "!" );
            for ( int i = 0; i < ShipsPerTier.Length; i++ )
                Record_AmountOfSpecificTier( ShipsPerTier[i], i, Context );
        }

        //Takes a list of ship counts per tier and tries to spawn them with the budget given, returning whatever is left over before no more can be spawned.
        public void RecordShips_AmountPerTierUpToBudgetLimit( ref int Budget, int[] ShipsPerTier, ArcenHostOnlySimContext Context )
        {
            if ( ShipsPerTier.Length > DrawData.Count )
                throw new Exception( "Error in " + InternalName + ": Tried to spawn with tiers 0 - " + ShipsPerTier.Length + " but DrawData count is only " + DrawData.Count + "!" );
            if ( PreferBigOnTargetedSpawns )
            {
                for ( int i = 0; i < ShipsPerTier.Length; i++ )
                {
                    if ( !CanSpend( Budget, DrawData[i].MinCost ) )
                        continue;
                    Record_AmountOfSpecificTierUpToBudgetLimit( ref Budget, ShipsPerTier[i], i, Context );
                }
            } else
            {
                for ( int i = ShipsPerTier.Length; i >= 0; i++ )
                {
                    if ( !CanSpend( Budget, DrawData[i].MinCost ) )
                        continue;
                    Record_AmountOfSpecificTierUpToBudgetLimit( ref Budget, ShipsPerTier[i], i, Context );
                }
            }
        }

        //Randomly spawns entities. If set to TryToExhaustBudgetWithSmallerSpawns, will exclude items that can't be spawned based on the [current] budget as the budget gets lower (if it ever was high enough).
        public void RecordShips_SpendBudgetInTierLimits( ref int Budget, bool TryToExhaustBudgetWithSmallerSpawns, ArcenHostOnlySimContext Context )
        {
            SpawnData data;
            SpawnDataDrawList lst;
            if ( TryToExhaustBudgetWithSmallerSpawns )
            {
                while ( CanSpend( Budget, SmallestSpendingPossible ) )
                {
                    lst = DrawTierWithinLimits( Context, Budget );
                    data = lst.Draw( Context, Budget );
                    Record( ref Budget, data );
                }
            } else
            {
                while ( true )
                {
                    data = DrawTierWithinLimits( Context ).Draw( Context );
                    if ( !CanSpend( Budget, data.Cost ) )
                        return;
                    Record( ref Budget, data );
                }
            }
        }

        public void DrawNextCurrent( ArcenHostOnlySimContext Context, int MaxBudget = int.MaxValue )
        {
            CurrentSpawnTarget = DrawTierWithinLimits( Context, MaxBudget ).Draw( Context, MaxBudget );
        }

        public void ChainRecordCurrentAndSetNext( ref int Budget, bool TryExhaustBudget, ArcenHostOnlySimContext Context )
        {
            if ( CurrentSpawnTarget.Type == null )
            {
                if ( TryExhaustBudget )
                    DrawNextCurrent( Context, Budget );
                else
                    DrawNextCurrent( Context );
            }
            while ( CanSpendCurrent( Budget ) )
            {
                RecordCurrent( ref Budget );
                if ( TryExhaustBudget )
                    DrawNextCurrent( Context, Budget );
                else
                    DrawNextCurrent( Context );
            }
        }

        //The following functions commit the recorded ships
        public void SpawnRecordedShips( ArcenHostOnlySimContext Context, List<GameEntity_Squad> optSpawnedShips = null )
        {
            int maxStacks;
            if ( PFaction.Faction.Type == FactionType.Player )
                maxStacks = AIWar2GalaxySettingQuickAccess.StackingCutoffPlayers;
            else
                maxStacks = AIWar2GalaxySettingQuickAccess.StackingCutoffNPCs;
            for ( int i = 0; i < RecordedSpawns.Length; i++ )
            {
                if ( RecordedSpawns[i] > 0 )
                    SpawnRecordedShip( Context, new EntityTypeAndCount( GameEntityTypeDataTable.Instance.Rows[i], RecordedSpawns[i] ), maxStacks, optSpawnedShips );
            }
            ClearRecordedSpawns();
        }

        public void SpawnRecordedShip( ArcenHostOnlySimContext Context, EntityTypeAndCount Data, int MaxStacks, List<GameEntity_Squad> optSpawnedShips = null )
        {
            int debugStep = 0;
            try
            {
                debugStep = 1000;
                int stacksRemaining = Data.Count;
                int countOfEntities = Math.Max( Math.Min( MaxStacks - PFaction.Entities.GetCountFromListOfEntitiesByEntityType( Data.TypeData ), stacksRemaining ), 1 );
                GameEntity_Squad squad;
                int currentStacks;
                debugStep = 2000;
                for ( int i = 0; i < countOfEntities; i++ )
                {
                    //Depending on whether or not something can be stacked, they are either going by 1 by 1 or balanced between how much can and can't be stacked, given the stack limit.
                    if ( Data.TypeData.CannotBeStacked )
                    {
                        currentStacks = 1;
                    } else
                    {
                        currentStacks = stacksRemaining / (countOfEntities - i);
                    }
                    stacksRemaining -= currentStacks;
                    debugStep = 3000;
                    squad = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( PFaction, Data.TypeData, MarkToSpawnAt, FleetToSpawnInto, 0, GetSpawnPoint( Context, Data.TypeData ), Context, "CBSS.SpawnRecordedShip" );
                    if (optSpawnedShips != null)
                        optSpawnedShips.Add(squad);
                    squad.AddOrSetExtraStackedSquadsInThis( (short) (currentStacks - 1), true );
                    if ( FireteamToSpawnInto != null )
                        squad.FireteamId = FireteamToSpawnInto.FireTeamID;
                    squad.spawnVis = SpawnVisuals;
                    squad.Orders.SetBehaviorDirectlyInSim( Behavior, BehaviorTarget );
                    squad.CurrentAngle = CurrentAngle;
                    squad.ShouldNotBeConsideredAsThreatToHumanTeam = !CountAsThreat;
                    if ( StateOfMatter != null )
                        squad.CurrentStateOfMatter = StateOfMatter;
                    debugStep = 4000;
                    if ( SpawnDroneFleetsFull && squad.TypeData.FleetDesignTemplateIUseForDrones != null )
                    {
                        debugStep = 5000;
                        //Save the original spawn points and maximum distance, but use the current ship's location and no distance for further drone spawning. Same for fleet.
                        ArcenPoint originalPoint = CentralSpawnPoint;
                        int originalDistance = MaxActualSpawnDistanceFromSpawnPoint;
                        Fleet originalFleet = FleetToSpawnInto;
                        CentralSpawnPoint = squad.WorldLocation;
                        MaxActualSpawnDistanceFromSpawnPoint = 0;
                        FleetToSpawnInto = squad.FleetMembership.Fleet;

                        debugStep = 6000;
                        //Spawn all drones needed inside
                        Fleet fleet = squad?.FleetMembership?.Fleet;
                        foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                        {
                            if ( mem.TypeData.IsDrone )
                                mem.AddOrSetNumberCreatedButNotDeployed( (short) mem.EffectiveSquadCap, true );
                        }

                        debugStep = 7000;
                        //Then revert to what was originally there.
                        CentralSpawnPoint = originalPoint;
                        MaxActualSpawnDistanceFromSpawnPoint = originalDistance;
                        FleetToSpawnInto = originalFleet;
                    }
                }
            } catch ( Exception e )
            {
                throw new Exception( "Error in SpawnRecordedShip at debug step " + debugStep + ": " + e.Message );
            }
        }

        //Utility
        public ArcenPoint GetSpawnPoint( ArcenHostOnlySimContext Context, GameEntityTypeData TypeData )
        {
            if ( MaxActualSpawnDistanceFromSpawnPoint <= 0 )
            {
                return CentralSpawnPoint;
            }
            ArcenPoint spp = Engine_AIW2.Instance.CombatCenter;
            int currentMax;
            for ( int i = 0; i < 10 && spp != Engine_AIW2.Instance.CombatCenter; i++ )
            {
                currentMax = ((i + 1) * MaxActualSpawnDistanceFromSpawnPoint) / 10;
                spp = PFaction.Planet.GetSafePlacementPoint( Context, TypeData, CentralSpawnPoint, 0, currentMax, 0, currentMax, 0,
                    Math.Max( 1, currentMax / 5 ), 5, StateOfMatter );
            }
            if ( spp == Engine_AIW2.Instance.CombatCenter )
            {
                return CentralSpawnPoint;
            }
            return spp;
        }

        public ArcenCharacterBufferBase WriteToBuffer( ArcenCharacterBufferBase Buffer )
        {
            Buffer.Add( "Dump of " ).Add( GetType().Name ).Add( ":\nPlanetFaction: " );
            if ( PFaction == null )
            {
                Buffer.Add( "null" );
            } else
            {
                Buffer.Add( PFaction.Faction.GetDisplayName() );
            }
            Buffer.Add( "\n\nSpawn Parameters:" )
                .Add( "\n\tCurrent Spawn: " ).Add( CurrentSpawnTarget.Type?.DisplayName ).Add( " (Cost = " ).Add( CurrentSpawnTarget.Cost ).Add( ", CumulativeWeightUpToAndIncludingThisItem = " ).Add( CurrentSpawnTarget.CumulativeWeightUpToAndIncludingThisItem ).Add( ")" )
                .Add( "\n\tSpawning Mark: " ).Add( MarkToSpawnAt )
                .Add( "\n\tSpawn with drones fleets full: " ).Add( SpawnDroneFleetsFull )

                .Add( "\n\nDrawing Bag Settings:" )
                .Add( "\n\tMin Tier: " ).Add( MinTier )
                .Add( "\n\tMax Tier: " ).Add( MaxTier )
                .Add( "\n\tFactor: " ).Add( Extensions.ToString(Factor) )
                .Add( "\n\tCan Get Into Debt: " ).Add( CanGetIntoDebt )
                .Add( "\n\tPreferBigOnTargetedSpawns: " ).Add( PreferBigOnTargetedSpawns )

                .Add( "\n\nLocational Info:" )
                .Add( "\n\tSpawn Center: " ).Add( CentralSpawnPoint )
                .Add( "\n\tSpawn Radius around Center: " ).Add( MaxActualSpawnDistanceFromSpawnPoint )
                .Add( "\n\tState Of Matter: " ).Add( StateOfMatter?.GetDisplayName() )
                .Add( "\n\tAngle: " ).Add( CurrentAngle.ToString() )

                .Add( "\n\nBehavioral Data:" )
                .Add( "\n\tBehavior: " ).Add( Extensions.ToString(Behavior) )
                .Add( "\n\tBehaviorTarget: " ).Add( BehaviorTarget )
                .Add( "\n\tCountAsThreat: " ).Add( CountAsThreat )
                .Add( "\n\tOnSpawn: " ).Add( Extensions.ToString(SpawnVisuals) )

                .Add( "\n\nAffiliations:" )
                .Add( "\n\tFireteamToSpawnInto: " ).Add( FireteamToSpawnInto?.FireTeamID + "" )
                .Add( "\n\tFleetToSpawnInto: " ).Add( FleetToSpawnInto?.FleetID + "" )

                .Add( "\n\nDrawing Bag Dump:" )
                .Add( "\n\tSpending Possible: " ).Add( SmallestSpendingPossible ).Add( " - " ).Add( LargestSpendingPossible )
                .Add( "\n\tCumulative Weight: " ).Add( CumulativeWeight );
            for ( int i = 0; i < DrawData.Count; i++ )
                DrawData[i].WriteToBuffer( Buffer, i );
            return Buffer;
        }

        public void Print()
        {
            ArcenDebugging.SingleLineQuickDebug( ToString() );
        }

        public override string ToString()
        {
            return ((ArcenCharacterBuffer) WriteToBuffer( ArcenCharacterBuffer.GetFromPoolOrCreate( "CostBasedShipSpawner-ToString" ) )).ToStringAndReturnToPool();
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private CostBasedShipSpawner()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "CostBasedShipSpawners" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<CostBasedShipSpawner> Pool = new ConcurrentPool<CostBasedShipSpawner>( "CostBasedShipSpawners", 999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new CostBasedShipSpawner(); } );

        public static CostBasedShipSpawner GetFromPoolOrCreate( string InternalName, byte MarkToSpawnAt, EntityTypeDrawingBag_SpawnMode Factor, Faction AIFaction_ForAIBudgets, bool SpawnDroneFleetsFull,
            WeightMode WeightAutoAdjustingForTierList, WeightMode WeightAutoAdjustingForTiers, int MinTier, int MaxTier )
        {
            CostBasedShipSpawner cbss = Pool.GetFromPoolOrCreate();
            cbss.InternalName = InternalName;
            cbss.RecordedSpawns = new int[GameEntityTypeDataTable.Instance.Rows.Count];
            cbss.ClearAndSetCreationParameters( MarkToSpawnAt, Factor, AIFaction_ForAIBudgets, SpawnDroneFleetsFull, WeightAutoAdjustingForTierList, WeightAutoAdjustingForTiers, MinTier, MaxTier );
            return cbss;
        }

        public void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<CostBasedShipSpawner> typeAnalyzer;
        protected void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<CostBasedShipSpawner>( new CostBasedShipSpawner() );
            typeAnalyzer.ApplyDefaults( this );
        }

        private bool isInPool = false;

        public bool GetInPoolStatus()
        {
            return isInPool;
        }

        public void SetInPoolStatus( bool IsInPool )
        {
            isInPool = IsInPool;
        }

        public void DoEarlyCleanupWhenGoingBackIntoPool()
        {
            InternalName = null;
            ListForBuilding.Clear();
            DrawData.Clear();
            CumulativeWeight = 0;
            InitializationHasBeenFinished = false;
            CanGetIntoDebt = false;
            PreferBigOnTargetedSpawns = false;
            MinTier = 0;
            MaxTier = 0;
            MarkToSpawnAt = 0;
            Factor = EntityTypeDrawingBag_SpawnMode.RawCount;
            SpawnDroneFleetsFull = false;
            CurrentSpawnTarget = default( SpawnData );
            RecordedSpawns = null;
            SmallestSpendingPossible = 0;
            LargestSpendingPossible = 0;
            PFaction = null;
            CentralSpawnPoint = ArcenPoint.ZeroZeroPoint;
            FleetToSpawnInto = null;
            FireteamToSpawnInto = null;
            CurrentAngle = AngleDegrees.MinAngle;
            Behavior = EntityBehaviorType.Attacker_Full;
            SpawnVisuals = SpawnVisualization.Normal;
            BehaviorTarget = -1;
            CountAsThreat = false;
            MaxActualSpawnDistanceFromSpawnPoint = 0;
            StateOfMatter = null;
        }

        public void DoAnyBelatedCleanupWhenComingOutOfPool()
        {

        }
        #endregion
    }
}
