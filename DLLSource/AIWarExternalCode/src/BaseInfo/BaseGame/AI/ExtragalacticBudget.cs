using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ExtragalacticBudget : ConcurrentPoolable<ExtragalacticBudget>, IProtectedListable
    {
        //Each AI can have an extragalactic budget against either a given faction
        //or faction Allegiance. In particular, each player gets its own faction index and each Hostile To All faction gets its own faction Index,
        //but we need to allow for extragalactic stuff against the Red Team
        public FireteamRequiredTarget Target;

        public FInt Budget;
        public GameEntityTypeData NextExtragalacticUnitToBuy;
        public FInt NormalUnitBudget;
        public GameEntityTypeData NextNormalUnitToBuy;

        //Not serialized
        public FInt PowerLevel;

        //IMPORTANT: any additions to the above need to have a clear entry in SetDefaults!

        #region Pooling
        private static ReferenceTracker RefTracker;
        private ExtragalacticBudget()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ExtragalacticBudget" );
            RefTracker.IncrementObjectCount();
            SetDefaults();
        }

        private static readonly ConcurrentPool<ExtragalacticBudget> Pool = new ConcurrentPool<ExtragalacticBudget>( "ExtragalacticBudget", 3000,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new ExtragalacticBudget(); } );

        public static ExtragalacticBudget GetFromPoolOrCreate()
        {
            ExtragalacticBudget result = Pool.GetFromPoolOrCreate();
            result.Target = FireteamRequiredTarget.GetFromPoolOrCreate();
            return result;
        }

        public ExtragalacticBudget CreateNewForPool()
        {
            return new ExtragalacticBudget();
        }

        public void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        public override void DoAnyBelatedCleanupWhenComingOutOfPool()
        {
        }

        public override void DoEarlyCleanupWhenGoingBackIntoPool()
        {
            SetDefaults();
        }

        //IProtectedListable
        public void DoBeforeRemoveOrClear()
        {
            Pool.ReturnToPool( this );
        }
        #endregion

        private void SetDefaults()
        {
            if ( this.Target != null ) {
                this.Target.ReturnToPool();
            }
            this.Target = null;
            this.Budget = FInt.Zero;
            this.NextExtragalacticUnitToBuy = null;
            this.NormalUnitBudget = FInt.Zero;
            this.NextNormalUnitToBuy = null;

            this.PowerLevel = FInt.Zero;
        }

        public static ExtragalacticBudget Create( Faction Fac, FInt budget, GameEntityTypeData nextUnit )
        {
            ExtragalacticBudget exoBudget = GetFromPoolOrCreate();
            exoBudget.Target.AgainstFaction = Fac;
            exoBudget.Budget = budget;
            exoBudget.NextExtragalacticUnitToBuy = nextUnit;
            return exoBudget;
        }
        public static ExtragalacticBudget Create( string allegiance, FInt budget, GameEntityTypeData nextUnit )
        {
            ExtragalacticBudget exoBudget = GetFromPoolOrCreate();
            exoBudget.Target.AgainstFactionAllegiance = allegiance;
            exoBudget.Budget = budget;
            exoBudget.NextExtragalacticUnitToBuy = nextUnit;
            return exoBudget;
        }

        public void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.Target.SerializeTo( MetaData, Buffer, SerializationCmdType );
            Buffer.AddFInt( MetaData, this.Budget, "ExtragalacticBudgetAmount" );
            Buffer.AddFInt( MetaData, this.NormalUnitBudget, "NormalUnitBudget" );
            GameEntityTypeDataTable.Instance.SerializeByIndex( MetaData, this.NextExtragalacticUnitToBuy, Buffer, "NextExtragalacticUnitToBuy" );
            GameEntityTypeDataTable.Instance.SerializeByIndex( MetaData, this.NextNormalUnitToBuy, Buffer, "NextNormalUnitToBuy" );
        }

        public void DeserializedIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.Target.DeserializedIntoSelf( MetaData, Buffer, SerializationCmdType );
            this.Budget = Buffer.ReadFInt( MetaData, "ExtragalacticBudgetAmount" );
            this.NormalUnitBudget = Buffer.ReadFInt( MetaData, "NormalUnitBudget" );
            this.NextExtragalacticUnitToBuy = GameEntityTypeDataTable.Instance.DeserializeByIndex( MetaData, Buffer, "NextExtragalacticUnitToBuy" );
            this.NextNormalUnitToBuy = GameEntityTypeDataTable.Instance.DeserializeByIndex( MetaData, Buffer, "NextNormalUnitToBuy" );
        }

        public void AppendStateForDebugDisplay( ArcenDoubleCharacterBuffer buffer )
        {
            //for non-colour logging
            if ( Target.AgainstFaction != null )
                buffer.Add( " target faction " ).Add( Target.AgainstFaction.GetDisplayName() ).Add( " idx " + Target.AgainstFaction.FactionIndex );
            if ( NextExtragalacticUnitToBuy != null )
            {
                buffer.Add( " allegiance " ).Add( Target.AgainstFactionAllegiance + " budget " ).Add( Budget ).Add( " next unit " )
                    .Add( NextExtragalacticUnitToBuy.GetDisplayName() ).Add( " power level " ).Add( PowerLevel );
                return;
            }
            buffer.Add( " allegiance " ).Add( Target.AgainstFactionAllegiance ).Add( " budget " ).Add( Budget ).Add( " next unit (null) power level " ).Add( PowerLevel );
        }

        public override void AppendStateForInterfaceDisplay( ArcenCharacterBufferBase buffer )
        {
            if ( this.PowerLevel < FInt.One )
                return;
            //with colour
            if ( Target.AgainstFaction != null )
                buffer.Add( "Budget against " ).Add( Target.AgainstFaction.GetDisplayName(), Target.AgainstFaction.FactionCenterColor.ColorHexBrighter ).Add( " is " ).Add( Budget.ReadableString, "a1ffa1" );
            else if ( !String.IsNullOrEmpty( this.Target.AgainstFactionAllegiance ) )
            {
                buffer.Add( "Budget against " ).Add( Target.AgainstFactionAllegiance, "4649a9" ).Add( " is " ).Add( Budget.ReadableString, "a1ffa1" );
            }
            else
                buffer.Add( "Budget against unknown faction? Probably an old save game, this is fine. " ).Add( " Budget is " ).Add( Budget.ReadableString, "a1ffa1" );
            buffer.Add( " Power Level " ).Add( PowerLevel.ReadableString, "a1a1a1" ).Add( ".\n\t" );
            if ( NextExtragalacticUnitToBuy != null )
                buffer.Add( " Next unit " ).Add( NextExtragalacticUnitToBuy.GetDisplayName(), "802020" ).Add( " at cost " ).Add( NextExtragalacticUnitToBuy.CostForAIToPurchase, "a1a1a1" );
        }

        public static ExtragalacticBudget GetBudgetFromList( ProtectedList<ExtragalacticBudget> list, Faction faction )
        {
            //finds the budget that would cover this faction (or null, to say "no budget found")
            bool debug = false;
            if ( debug )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "List all budgets before finding the budget for " + faction.GetDisplayName() + " idx " + faction.FactionIndex + ", " + faction.BaseInfo.Allegiance + " : ", Verbosity.DoNotShow );
                for ( int i = 0; i < list.Count; i++ )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( i + ": " + list[i].ToString(), Verbosity.DoNotShow );
                }
            }
            for ( int i = 0; i < list.Count; i++ )
            {
                if ( list[i].Target.AgainstFaction == faction )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "found match by index " + list[i].ToString(), Verbosity.DoNotShow );
                    return list[i];
                }
                if ( faction.BaseInfo.Allegiance == "Hostile To All" ) //if we are hostile to all, we must match the faction index
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "skipping " + list[i].ToString() + " since this faction is hostile to all so we must match in index", Verbosity.DoNotShow );
                    continue;
                }
                if ( String.IsNullOrEmpty( list[i].Target.AgainstFactionAllegiance ) )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "skipping " + list[i].ToString() + " since this entry has no allegiance, so it must match on index", Verbosity.DoNotShow );
                    continue;
                }

                if ( (list[i].Target.AgainstFactionAllegiance == faction.BaseInfo.Allegiance) ||
                     (list[i].Target.AgainstFactionAllegiance == "Friendly To Players" && faction.Type == FactionType.Player) )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "found match by allegiance " + list[i].ToString(), Verbosity.DoNotShow );
                    return list[i];
                }
            }
            return null;
        }

        public void GetPlanetsForExtragalacticBudget( List<Planet> listToFill, Faction AIFaction )
        {
            listToFill.Clear();

            //get all the factions controlled by the targets of this budget
            List<Faction> factionsToCheck = Faction.GetTemporaryFactionList( "ExtragalacticBudget-factionsToCheck", 10f );
            if ( factionsToCheck == null ) //blocked for teardown/shutdown; bail
                return;

            if ( Target.AgainstFaction != null )
                factionsToCheck.Add( Target.AgainstFaction );
            else
            {
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction faction = World_AIW2.Instance.Factions[i];
                    if ( faction == null || faction.FactionIsDefeated )
                        continue;
                    if ( !AIFaction.GetIsHostileTowards( faction ) )
                        continue;
                    if ( this.Target.AgainstFactionAllegiance == faction.BaseInfo.Allegiance ||
                        (this.Target.AgainstFactionAllegiance == "Friendly To Players" && // Player friendly includes both players and... those friendly.
                        (faction.Type == FactionType.Player || faction.GetIsFriendlyToAnyPlayerFaction())) )
                        factionsToCheck.Add( faction );
                }
            }
            for ( int i = 0; i < factionsToCheck.Count; i++ )
            {
                Faction faction = factionsToCheck[i];
                foreach ( Planet planet in World_AIW2.Instance.CurrentGalaxy.Planets( false ) )
                {
                    if ( planet.GetControllingFaction() == faction )
                        listToFill.Add( planet );
                    else if ( planet.UnderInfluenceOfFactionIndex.Contains( faction.FactionIndex ) )
                        listToFill.Add( planet );
                }
            }

            Faction.ReleaseTemporaryFactionList( factionsToCheck );
        }
    }
}
