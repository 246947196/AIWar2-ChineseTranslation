using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DarkZenithPerUnitBaseInfo : ExternalSquadBaseInfo
    {
        //This class is mostly for Economic stuff (Terminii and Epistyles)
        //it has a lot of utility functions
        public DZResource Resource; //this is the primary resource for this structure (used for Terminii)

        //how much of each type of resource this structure has
        public readonly Dictionary<DZResource, int> Inventory = Dictionary<DZResource, int>.Create_WillNeverBeGCed( 12, "DarkZenithPerUnitBaseInfo-Inventory" );
        //bonus income, for upgraded epistyles, pirates, etc...
        public readonly Dictionary<DZResource, int> PermanentBonusIncome = Dictionary<DZResource, int>.Create_WillNeverBeGCed( 12, "DarkZenithPerUnitBaseInfo-PermanentBonusIncome" );

        //For each resource type, how much this structure needs it
        public readonly Dictionary<DZResource, int> NeedForResource = Dictionary<DZResource, int>.Create_WillNeverBeGCed( 12, "DarkZenithPerUnitBaseInfo-NeedForResource" );
        //similar to the above, except this lets the DZ have a "priority" for some things (for example, to prevent starvation)
        public readonly Dictionary<DZResource, int> NeedForResourceScore = Dictionary<DZResource, int>.Create_WillNeverBeGCed( 12, "DarkZenithPerUnitBaseInfo-NeedForResourceScore" ); 

        public int TimeWeLastDidConversion;
        public int TimeSinceLastSuccessfulBuild;
        public DZResourceConversion NextConversion;

        //For anything that wants to build/create new stuff using Conversions
        //The rule for conversions: A Terminus has a List of available conversions, like "Turn metal into Green" or "Turn metal into a Transport (if we need it)" or "Turn metal into an Epistyle (if we need it)
        //The latter two are only used if we are out of Transports or Epistyles, and we're going to start writing the code assuming we're starting from square 0
        //The Bag is used by the Epistyle, and allows for different units to be prioritized differently
        //Note that most epistyles can build utility.
        public bool HasList; //Generally used for specifically set conversions for bootstrapping epistyles, or the resource conversions on Terminii
        public bool HasBag; //Used for Epistyles with lots of conversions
        public readonly List<DZResourceConversion> ConversionList = List<DZResourceConversion>.Create_WillNeverBeGCed( 12, "DarkZenithPerUnitBaseInfo-ConversionList" );
        public readonly DrawBag<DZResourceConversion> ConversionBag = DrawBag<DZResourceConversion>.Create_WillNeverBeGCed( 12, "DarkZenithPerUnitBaseInfo-ConversionBag" );

        //Used to keep track of what sorts of things a given Epistyle can do
        public bool CanBuildInfrastructure;
        public bool CanBuildOffensiveUnits;
        public bool CanBuildUpgrades;
        public bool CanBuildUtility;

        //Pirate stuff
        public bool IsPirateEpistyle;
        public int TimeForNextPrivateer;
        public int HomeEpistyleId; //for privateers
        public Int16 NumTransportsAttacked;
        //For Transports
        public int DestinationId; //a PrimaryKeyID for the GameEntity where this unit is heading.
        public int SecondaryDestinationId; //while en route to a primary destination, a transport is allowed to stop off at other terminii for more goodies
        public int SecondaryMostRecentPlanetId; //don't get too distracted; one secondary terminus per planet

        //For Constructors, says "Where and what are we building
        public Int16 DZConstructorTargetPlanetIndex;
        public ArcenPoint DestinationPoint;
        public byte MarkLevel;
        public int NumberOfUnits;
        public GameEntityTypeData Unit;

        //For Jormugandr.
        public bool IsJormugandr; //so we don't have to check a tag
        public bool IsDormant;
        public int SecondsUntilDormancyMove;
        public int SecondsRemaingActive;

        //For things carrying out vassal missions
        public bool IsAssignedToMission;

        public bool HasRalliedToFlagship; //used for the sidekick
        public int UnitsKilled;
        //For the DZ Sidekick
        public bool KeepConversion;
        public bool HighPriority;
        public int MaxOffenseTier; //if > 0, this Epistyle's offensive production is capped to UI Tier <= this value (1=Strikecraft, 2=Frigates, etc); 0 means no cap
        public LazyLoadSquadWrapper PreferredFlagship; //Ships created by this epistyle go to this flagship
        //Non-serialized
        public GameEntity_Squad Destination; //from the DestinationId
        public GameEntity_Squad SecondaryDestination; //from the SecondaryDestinationId
        public Planet SecondaryMostRecentPlanet; //from the SecondaryMostRecentPlanetId
        public GameEntity_Squad InboundTransport;
        public VassalMission MyMission;

        public DarkZenithPerUnitBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            Resource = DZResource.None;
            InitializeDZDictionary( this.Inventory );
            InitializeDZDictionary( this.PermanentBonusIncome );
            InitializeDZDictionary( this.NeedForResourceScore );
            InitializeDZDictionary( NeedForResourceScore );

            TimeWeLastDidConversion = -1;
            TimeSinceLastSuccessfulBuild = -1;
            NextConversion = null;

            HasList = false;
            HasBag = false;
            ConversionList.Clear();
            ConversionBag.Clear();

            CanBuildInfrastructure = false;
            CanBuildOffensiveUnits = false;
            CanBuildUpgrades = false;
            CanBuildUtility = false;

            IsPirateEpistyle = false;
            TimeForNextPrivateer = -1;
            HomeEpistyleId = -1;
            NumTransportsAttacked = 0;

            DestinationId = -1;
            SecondaryDestinationId = -1;
            SecondaryMostRecentPlanetId = -1;

            DZConstructorTargetPlanetIndex = -1;
            DestinationPoint = ArcenPoint.ZeroZeroPoint;
            MarkLevel = 0;
            NumberOfUnits = 0;
            Unit = null;

            IsJormugandr = false;
            IsDormant = true;
            SecondsUntilDormancyMove = -1;
            SecondsRemaingActive = -1;

            Destination = null;
            SecondaryDestination = null;
            SecondaryMostRecentPlanet = null;
            InboundTransport = null;

            IsAssignedToMission = false;
            HasRalliedToFlagship = true;
            MyMission = null;
            UnitsKilled = 0;
            KeepConversion = false;
            HighPriority = false;
            MaxOffenseTier = 0;
            PreferredFlagship.Clear();
        }

        public override void CopyTo( ExternalSquadBaseInfo CopyTarget )
        {
            if ( CopyTarget == null )
                return;
            DarkZenithPerUnitBaseInfo target = CopyTarget as DarkZenithPerUnitBaseInfo;
            if ( target == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not copy from " + this.GetType() + " to " + CopyTarget.GetType() + " because of type mismatch.", Verbosity.ShowAsError );
                return;
            }
            target.Resource = this.Resource;

            target.TimeWeLastDidConversion = this.TimeWeLastDidConversion;
            target.TimeSinceLastSuccessfulBuild = this.TimeSinceLastSuccessfulBuild;
            target.NextConversion = this.NextConversion;

            target.HasList = this.HasList;
            target.HasBag = this.HasBag;
            target.ConversionList.CopyFrom( this.ConversionList );
            target.ConversionBag.CopyFrom( this.ConversionBag );

            target.IsPirateEpistyle = this.IsPirateEpistyle;
            target.TimeForNextPrivateer = this.TimeForNextPrivateer;
            target.HomeEpistyleId = this.HomeEpistyleId;
            target.NumTransportsAttacked = this.NumTransportsAttacked;

            target.DestinationId = this.DestinationId;
            target.SecondaryDestinationId = this.SecondaryDestinationId;
            target.SecondaryMostRecentPlanetId = this.SecondaryMostRecentPlanetId;

            target.DZConstructorTargetPlanetIndex = this.DZConstructorTargetPlanetIndex;
            target.DestinationPoint = this.DestinationPoint;
            target.MarkLevel = this.MarkLevel;
            target.NumberOfUnits = this.NumberOfUnits;
            target.Unit = this.Unit;

            target.IsJormugandr = this.IsJormugandr;
            target.IsDormant = this.IsDormant;
            target.SecondsUntilDormancyMove = this.SecondsUntilDormancyMove;
            target.SecondsRemaingActive = this.SecondsRemaingActive;

            target.Destination = this.Destination;
            target.SecondaryDestination = this.SecondaryDestination;
            target.SecondaryMostRecentPlanet = this.SecondaryMostRecentPlanet;
            target.InboundTransport = this.InboundTransport;

            target.IsAssignedToMission = this.IsAssignedToMission;
            target.HasRalliedToFlagship = this.HasRalliedToFlagship;
            target.MyMission = this.MyMission;
            target.UnitsKilled = this.UnitsKilled;
            target.KeepConversion = this.KeepConversion;
            target.HighPriority = this.HighPriority;
            target.MaxOffenseTier = this.MaxOffenseTier;
            target.PreferredFlagship = this.PreferredFlagship.CreateCopy();
        }

        public override void DoAfterStackSplitAndCopy( int OriginalStackCount, int MyPersonalNewStackCount )
        {
            //nothing seems to be needed here
            //this method is called on both halves of the stack that are split,
            //and you can see the amount that was in the total stack originally, and the portion that is in this half of the new split stack
        }

        public override void DoAfterSingleOtherShipMergedIntoOurStack( ExternalSquadBaseInfo OtherShipBeingDiscarded )
        {
            //the OtherShipBeingDiscarded is being discarded.  If there's any data we want to merge into this
            //(this being a part of the stack that is kept), then we can do it now.
            //if there are 10 squads being merged into one stack, this would be called 9 times on the
            //single squad that is remaining, with the parameter being the other 9 that are disappearing.
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                Buffer.WriteHeaderStringToLogIfLoggingActive( "DZ PerUnit Data" );
                Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)Resource, "DZ-Resource" );
                Buffer.WriteHeaderStringToLogIfLoggingActive( "DZ PerUnit Data - pre dictionaries" );
                FactionUtilityMethods.Instance.SerializeDictionary( MetaData, Inventory, Buffer, "Inventory" );
                FactionUtilityMethods.Instance.SerializeDictionary( MetaData, PermanentBonusIncome, Buffer, "PermanentBonusIncome" );
                FactionUtilityMethods.Instance.SerializeDictionary( MetaData, NeedForResource, Buffer, "NeedForResource" );
                FactionUtilityMethods.Instance.SerializeDictionary( MetaData, NeedForResourceScore, Buffer, "NeedForResourceScore" );
                debugCode = 200;

                Buffer.WriteHeaderStringToLogIfLoggingActive( "DZ PerUnit Data - post dictionaries" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeWeLastDidConversion, "TimeWeLastDidConversion" );
                DarkZenithResourceConversionTable.Instance.SerializeByIndex( MetaData, NextConversion, Buffer, "NextConversion" );

                debugCode = 250;
                Buffer.AddBool( MetaData, this.IsPirateEpistyle, "IsPirateEpistyle" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeForNextPrivateer, "TimeForNextPrivateer" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.HomeEpistyleId, "HomeEpistyleId" );
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, this.NumTransportsAttacked, "NumTransportsAttacked" );

                debugCode = 300;
                Buffer.AddBool( MetaData, HasList, "HasList" );
                Buffer.AddBool( MetaData, HasBag, "HasBag" );
                Buffer.AddBool( MetaData, CanBuildInfrastructure, "CanBuildInfrastructure" );
                Buffer.AddBool( MetaData, CanBuildOffensiveUnits, "CanBuildOffensiveUnits" );
                Buffer.AddBool( MetaData, CanBuildUpgrades, "CanBuildUpgrades" );
                Buffer.AddBool( MetaData, CanBuildUtility, "CanBuildUtility" );
                debugCode = 400;
                Buffer.WriteHeaderStringToLogIfLoggingActive( "DZ PerUnit Data - mid point A" );
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)ConversionList.Count, "ConversionList.Count" );
                debugCode = 500;
                for ( int i = 0; i < ConversionList.Count; i++ )
                {
                    debugCode = 550;
                    if ( ConversionList[i] == null )
                        throw new Exception( "Yo WTF " + i );
                    DarkZenithResourceConversionTable.Instance.SerializeByIndex( MetaData, ConversionList[i], Buffer, "conversion" );
                }
                debugCode = 560;
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)ConversionBag.InternalListSize, "ConversionBag.InternalListSize" );
                for ( int i = 0; i < ConversionBag.InternalListSize; i++ )
                {
                    debugCode = 600;
                    var pair = ConversionBag.GetInternalListItemPairAtIndex( i );
                    Buffer.AddInt32( MetaData, ReadStyle.NonNeg, pair.Count, "numCopies" );
                    DarkZenithResourceConversionTable.Instance.SerializeByIndex( MetaData, pair.Value, Buffer, "conversion" );
                }
                debugCode = 700;
                Buffer.WriteHeaderStringToLogIfLoggingActive( "DZ PerUnit Data - mid point B " );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, DestinationId, "DestinationId" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, SecondaryDestinationId, "SecondaryDestinationId" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, SecondaryMostRecentPlanetId, "SecondaryMostRecentPlanetId" );

                Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, this.DZConstructorTargetPlanetIndex, "DZConstructorTargetPlanetIndex" );
                Buffer.AddArcenPointFromCombatSpace( MetaData, this.DestinationPoint, "DestinationPoint" );
                Buffer.AddByte( MetaData, ReadStyleByte.Normal, this.MarkLevel, "MarkLevel" );
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.NumberOfUnits, "NumberOfUnits" );
                GameEntityTypeDataTable.Instance.SerializeByIndex( MetaData, this.Unit, Buffer, "Unit" );

                Buffer.AddBool( MetaData, this.IsJormugandr, "IsJormugandr" );
                Buffer.AddBool( MetaData, this.IsDormant, "IsDormant" );
                Buffer.AddInt32( MetaData, ReadStyle.Signed, this.SecondsUntilDormancyMove, "DormancyMove" );
                Buffer.AddInt32( MetaData, ReadStyle.Signed, this.SecondsRemaingActive, "SecondsRemainingActive" );

                Buffer.AddBool( MetaData, this.IsAssignedToMission, "IsAssignedToMission" );

                Buffer.AddBool( MetaData, this.HasRalliedToFlagship, "HasRalliedToFlagship" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.UnitsKilled, "UnitsKilled" );
                Buffer.AddBool( MetaData, this.KeepConversion, "KeepConversion" );
                Buffer.AddBool( MetaData, this.HighPriority, "HighPriority" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.PreferredFlagship.GetPrimaryKeyID(), "PreferredFlagship.PrimaryKeyID" );
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.MaxOffenseTier, "MaxOffenseTier" );
                Buffer.WriteHeaderStringToLogIfLoggingActive( "DZ PerUnit Data end" );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in PerUnit serializeTo debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Inventory.Clear();
            PermanentBonusIncome.Clear();
            NeedForResource.Clear();
            NeedForResourceScore.Clear();
            InitializeDZDictionary( this.Inventory );
            InitializeDZDictionary( this.PermanentBonusIncome );
            InitializeDZDictionary( this.NeedForResourceScore );
            InitializeDZDictionary( NeedForResourceScore );

            Buffer.WriteHeaderStringToLogIfLoggingActive( "DZ PerUnit Data" );
            Resource = (DZResource)Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "DZ-Resource" );
            Buffer.WriteHeaderStringToLogIfLoggingActive( "DZ PerUnit Data - pre dictionaries" );
            FactionUtilityMethods.Instance.DeserializeDictionary( MetaData, Inventory, Buffer, "Inventory" );
            FactionUtilityMethods.Instance.DeserializeDictionary( MetaData, PermanentBonusIncome, Buffer, "PermanentBonusIncome" );
            FactionUtilityMethods.Instance.DeserializeDictionary( MetaData, NeedForResource, Buffer, "NeedForResource" );
            FactionUtilityMethods.Instance.DeserializeDictionary( MetaData, NeedForResourceScore, Buffer, "NeedForResourceScore" );
            Buffer.WriteHeaderStringToLogIfLoggingActive( "DZ PerUnit Data - post dictionaries" );
            this.TimeWeLastDidConversion = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeWeLastDidConversion" );
            NextConversion = DarkZenithResourceConversionTable.Instance.DeserializeByIndex( MetaData, Buffer, "NextConversion" );

            IsPirateEpistyle = Buffer.ReadBool( MetaData, "IsPirateEpistyle" );
            TimeForNextPrivateer = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TimeForNextPrivateer" );
            HomeEpistyleId = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "HomeEpistyleId" );
            NumTransportsAttacked = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "NumTransportsAttacked" );

            HasList = Buffer.ReadBool( MetaData, "HasList" );
            HasBag = Buffer.ReadBool( MetaData, "HasBag" );
            CanBuildInfrastructure = Buffer.ReadBool( MetaData, "CanBuildInfrastructure" );
            CanBuildOffensiveUnits = Buffer.ReadBool( MetaData, "CanBuildOffensiveUnits" );
            CanBuildUpgrades = Buffer.ReadBool( MetaData, "CanBuildUpgrades" );
            CanBuildUtility = Buffer.ReadBool( MetaData, "CanBuildUtility" );
            Buffer.WriteHeaderStringToLogIfLoggingActive( "DZ PerUnit Data - mid point A" );
            Int16 listCount = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "ConversionList.Count" );
            ConversionList.Clear();
            for ( int i = 0; i < listCount; i++ )
            {
                ConversionList.Add( DarkZenithResourceConversionTable.Instance.DeserializeByIndex( MetaData, Buffer, "conversion" ) );
                ConversionList[i].DoOnLoad();
            }

            //Chris notes: this will thrash the GC very slightly if there's much data in there, so hopefully there is not.
            //but this is beyond what I felt comfortable updating
            ConversionBag.Clear();
            Int16 bagCount = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "ConversionBag.InternalListSize" );
            for ( int i = 0; i < bagCount; i++ )
            {
                int numCopies = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "numCopies" );
                DZResourceConversion conversion = DarkZenithResourceConversionTable.Instance.DeserializeByIndex( MetaData, Buffer, "conversion" );
                conversion.DoOnLoad();
                ConversionBag.AddItem( conversion, numCopies );
            }

            Buffer.WriteHeaderStringToLogIfLoggingActive( "DZ PerUnit Data - mid point B " );
            DestinationId = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "DestinationId" );
            SecondaryDestinationId = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "SecondaryDestinationId" );
            SecondaryMostRecentPlanetId = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "SecondaryMostRecentPlanetId" );

            DZConstructorTargetPlanetIndex = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "DZConstructorTargetPlanetIndex" );
            DestinationPoint = Buffer.ReadArcenPointFromCombatSpace( MetaData, "DestinationPoint" );
            MarkLevel = Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "MarkLevel" );
            NumberOfUnits = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "NumberOfUnits" );
            Unit = GameEntityTypeDataTable.Instance.DeserializeByIndex( MetaData, Buffer, "Unit" );
            IsJormugandr = Buffer.ReadBool( MetaData, "IsJormugandr" );
            IsDormant = Buffer.ReadBool( MetaData, "IsDormant" );
            SecondsUntilDormancyMove = Buffer.ReadInt32( MetaData, ReadStyle.Signed, "DormancyMove" );
            SecondsRemaingActive = Buffer.ReadInt32( MetaData, ReadStyle.Signed, "SecondsRemainingActive" );
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 3, 778 ) )
                this.IsAssignedToMission = Buffer.ReadBool( MetaData, "IsAssignedToMission" );

            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 016 ) )
                 this.HasRalliedToFlagship = Buffer.ReadBool( MetaData, "HasRalliedToFlagship" );

            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 585 ) )
                UnitsKilled = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "UnitsKilled" );
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 588 ) )
                KeepConversion = Buffer.ReadBool( MetaData, "KeepConversion" );

            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 590 ) )
                HighPriority = Buffer.ReadBool( MetaData, "HighPriority" );

            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 596 ) )
                this.PreferredFlagship = LazyLoadSquadWrapper.Create( Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "PreferredFlagship.PrimaryKeyID" ), true, "DZCastDeser" );

            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 819 ) )
                MaxOffenseTier = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "MaxOffenseTier" );

            Buffer.WriteHeaderStringToLogIfLoggingActive( "DZ PerUnit Data end" );
        }

        /* A bunch of utility functions start here. Generally for economic stuff */
        public DZResourceConversion GetConversionFromListByNameIfPossible( string name )
        {
            //just get the Conversion, if it's bag or List
            //Note potential gotcha: we rely on display name here
            for ( int i = 0; i < this.ConversionList.Count; i++ )
            {
                if ( this.ConversionList[i].DisplayName.Contains( name ) || this.ConversionList[i].InternalName.Contains( name ) )
                    return this.ConversionList[i];
            }

            for ( int i = 0; i < this.ConversionBag.InternalListSize; i++ )
            {
                if ( this.ConversionBag.GetInternalListItemAtIndex( i ).DisplayName.Contains( name ) || this.ConversionBag.GetInternalListItemAtIndex( i ).InternalName.Contains( name ) )
                    return this.ConversionBag.GetInternalListItemAtIndex( i );
            }

            return null;
        }
        private void InitializeDZDictionary( Dictionary<DZResource, int> dict )
        {
            dict[DZResource.Metal] = 0;
            dict[DZResource.Green] = 0;
            dict[DZResource.White] = 0;
            dict[DZResource.Blue] = 0;
            dict[DZResource.Black] = 0;
            dict[DZResource.Red] = 0;
        }
        public void AddStartingResources( int metalAmount, int otherAmount )
        {
            //when the DZ starts its invasion, it gets some free resources at its epistyles
            this.Inventory[DZResource.Metal] = metalAmount;
            this.Inventory[DZResource.Green] = otherAmount;
            this.Inventory[DZResource.White] = otherAmount;
            this.Inventory[DZResource.Blue] = otherAmount;
            this.Inventory[DZResource.Black] = otherAmount;
            this.Inventory[DZResource.Red] = otherAmount;
        }
        public bool HasAnyResourcesAtAll()
        {
            foreach ( KeyValuePair<DZResource, int> kv in this.Inventory )
            {
                if ( kv.Value > 0 )
                    return true;
            }
            return false;
        }
        public bool HasAnyPermanentBonusIncome()
        {
            foreach ( KeyValuePair<DZResource, int> kv in this.PermanentBonusIncome )
            {
                if ( kv.Value > 0 )
                    return true;
            }
            return false;
        }

        public int GenerateAvailableResourceScore()
        {
            //Higher scores = more resources
            //used by transports to decide which terminus to pick up resources from,
            //or for Privateers to decide which transport to attack
            int score = 0;
            foreach ( KeyValuePair<DZResource, int> kv in this.Inventory )
            {
                //int conversionCost = 0;
                if ( this.NextConversion != null && this.NextConversion.Cost[kv.Key] > 0 )
                    continue; //if we will use this
                FInt increase = (FInt)kv.Value;

                if ( kv.Key != DZResource.Metal )
                    increase *= FInt.FromParts( 1, 200 ); //value non-metal resources a bit more
                score += increase.IntValue;
            }
            return score;

        }
        public bool HasResourcesAvailable( bool tracing, ArcenCharacterBuffer tracingBuffer, GameEntity_Squad thisUnit )
        {
            //If we have resources that we aren't about to spend on our next Conversion, they are available for
            //a Transport to pick up.
            int debugCode = 0;
            try
            {
                foreach ( KeyValuePair<DZResource, int> kv in this.Inventory )
                {
                    debugCode = 100;
                    int conversionCost = 0;
                    if ( this.NextConversion != null && this.NextConversion.Cost[kv.Key] > 0 )
                        conversionCost = this.NextConversion.Cost[kv.Key];
                    if ( thisUnit.TypeData.GetHasTag( "DZTerminus" ) && conversionCost > 0 )
                        continue; //This is a terminus and I'm going to be using this resource, so it's not available
                    debugCode = 200;
                    if ( tracing )
                    {
                        debugCode = 300;
                        string unitString = "no unit";
                        if ( thisUnit != null )
                        {
                            debugCode = 310;
                            unitString = thisUnit.ToStringWithPlanet();
                        }
                        debugCode = 320;
                        if ( this.NextConversion == null )
                        {
                            debugCode = 330;
                            //this case is basically a no-op (I think) and clutters the traces
                            // if ( tracing )
                            //     tracingBuffer.Add( "\t" + unitString + ", Checking available resources (no next conversion): " + key.ToString() + " conversionCost " + conversionCost + " inventory " + this.Inventory[key] + ". My resource is " + this.Resource + "\n" );
                        }
                        else
                        {
                            debugCode = 340;
                            if ( tracing && conversionCost > 0 )
                                tracingBuffer.Add( "\t\t\t" + unitString + ", Checking available resources (for " + this.NextConversion.ToString() + "): " + kv.Key.ToString() + " conversionCost " + conversionCost + " inventory " + kv.Value + ". My resource is " + this.Resource + "\n" );
                        }
                    }
                    debugCode = 350;
                    if ( kv.Value > conversionCost )
                        return true;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in HasResourcesAvailable code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            return false;
        }
        public void TransferUnneededResourcesTo( bool tracing, ArcenCharacterBuffer tracingBuffer, ref DarkZenithPerUnitBaseInfo destData, ref string logString )
        {
            int debugCode = 0;
            try
            {
                foreach ( KeyValuePair<DZResource, int> kv in this.Inventory )
                {
                    debugCode = 100;
                    int conversionCost = 0;
                    if ( this.NextConversion != null && this.NextConversion.Cost.ContainsKey( kv.Key ) && this.NextConversion.Cost[kv.Key] > 0 )
                        conversionCost = this.NextConversion.Cost[kv.Key];
                    logString = "conversion cost for " + kv.Key.ToString() + "\n";
                    debugCode = 200;
                    //previously we transferred any unneeded resources
                    //Now we're going to hold on them so we can keep building more copies of whatever this is
                    if ( conversionCost > 0 )
                        continue;
                    // if ( this.Inventory[key] > conversionCost )
                    // {
                    debugCode = 300;
                    int delta = kv.Value - conversionCost;
                    debugCode = 400;
                    logString = "transferring " + delta + " " + kv.Key.ToString() + "\n";
                    destData.Inventory[kv.Key] += delta;
                    this.Inventory[kv.Key] -= delta;
                    // }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in TransferUnneededResourcesTo code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        public void UpdateNeededResources( DZResourceConversion conversion, GameEntity_Squad entity, bool tracing, ArcenCharacterBuffer tracingBuffer )
        {
            //the need for resources is based on how much we need the resource * a factor of how long it's been since we built
            //so we can avoid starvation

            //int divisionForUpgrades = 3; //upgrades are lower priority
            this.NeedForResource.Clear();
            this.NeedForResourceScore.Clear();
            if ( conversion == null )
            {
                return; //we need nothing; transient state if we need to pick a Conversion next sim-step
            }
            int interval = 60; //we increase our need every X seconds
            if ( conversion.Upgrade != null )
                interval = 70; //more slowly for upgrades
            if ( conversion.IsResourceCreation )
                interval = 10;
            bool transportUrgence = false;
            if ( conversion.NameForUnit == "DZTransport" )
            {
                transportUrgence = true;
            }

            int numMultipliers = (World_AIW2.Instance.GameSecond - this.TimeWeLastDidConversion) / interval + 1;
            int timeSinceLastConversion = World_AIW2.Instance.GameSecond - this.TimeWeLastDidConversion;
            int divisionFactorForScore = 10; //scale the numbers down a bit (and a bit more for upgrades)
            if ( conversion.Upgrade != null )
                divisionFactorForScore = 20;
            if ( transportUrgence || conversion.NameForUnit == "WarpingInDZEpistyle" ||
                 conversion.NameForUnit == "WarpingInDZTerminus" )
                divisionFactorForScore = 5;
            foreach ( KeyValuePair<DZResource, int> kv in conversion.Cost )
            {
                int currentValue = 0;
                this.Inventory.TryGetValue( kv.Key, out currentValue );
                if ( currentValue < kv.Value )
                {
                    int neededResources = (kv.Value - currentValue);
                    if ( neededResources < divisionFactorForScore )
                        divisionFactorForScore = 1;
                    int neededResourcesScore = (neededResources / (divisionFactorForScore + 1));
                    //Some additional tweaks to the resource score: things that will take a long time to build are lower priority,
                    //upgrades are lower priority
                    if ( neededResources > 20000 )
                        neededResourcesScore /= 10;
                    if ( neededResources > 10000 )
                        neededResourcesScore /= 5;
                    if ( neededResources < kv.Value / 4 ) //if we're close
                        neededResourcesScore *= 3;
                    if ( transportUrgence )
                        neededResourcesScore *= 10;
                    neededResourcesScore *= numMultipliers;
                    if ( numMultipliers > 5 ) //crank the priority if it's been a long time
                        neededResourcesScore *= ((numMultipliers) / 5) + 1;
                    if ( this.HighPriority )
                    {
                        neededResourcesScore *= 4;
                    }
                    this.NeedForResource[kv.Key] = neededResources;
                    this.NeedForResourceScore[kv.Key] = neededResourcesScore;
                    //This is way too verbose for general use
                    if ( tracing && entity.PrimaryKeyID == 65050 )
                        tracingBuffer.Add( "\t" + kv.Key.ToString() + " need for resources " + this.NeedForResource[kv.Key] + " score " + this.NeedForResourceScore[kv.Key] + ". Interval " + interval + " numMultipliers " + numMultipliers + "\n" );
                }
            }
        }

        public int HowMuchDoWeNeedResourcesFrom( GameEntity_Squad myStructure, DarkZenithPerUnitBaseInfo other, List<SafeSquadWrapper> AllTransports )
        {
            if ( myStructure == null )
                return 0;
            //When a transport is deciding whether it needs to go to a location,
            //it calls this for each epistyle/terminus
            int score = 0;
            int debugCode = 0;

            Dictionary<DZResource, int> IncomingResources = TempCollectionHelper.GetTemporaryDZResourceIntDict( "DZ-HowMuchDoWeNeedResourcesFrom-IncomingResources", 10f );
            if ( IncomingResources == null ) //blocked for teardown/shutdown; bail
                return 0;
            try
            {
                debugCode = 100;
                IncomingResources.Clear();
                for ( int i = 0; i < AllTransports.Count; i++ )
                {
                    debugCode = 180;
                    GameEntity_Squad transport = AllTransports[i].GetSquad();
                    if ( transport == null )
                        continue;
                    debugCode = 200;
                    //figure out how many (if any) incoming resources there are already,
                    //and don't include those in the calculation
                    DarkZenithPerUnitBaseInfo allTData = transport.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    if ( allTData.Destination != myStructure )
                        continue;
                    debugCode = 300;
                    foreach ( KeyValuePair<DZResource, int> kv in allTData.Inventory )
                    {
                        debugCode = 400;
                        if ( kv.Value > 0 )
                        {
                            IncomingResources[kv.Key] += kv.Value;
                        }
                    }
                    debugCode = 500;
                }
                debugCode = 600;
                foreach ( KeyValuePair<DZResource, int> kv in this.NeedForResourceScore )
                {
                    debugCode = 700;
                    if ( !other.Inventory.ContainsKey( kv.Key ) || other.Inventory[kv.Key] <= 0 )
                        continue;
                    int incoming;
                    IncomingResources.TryGetValue( kv.Key, out incoming );
                    int needed = kv.Value - incoming;
                    if ( needed <= 0 )
                        continue;

                    debugCode = 800;
                    FInt adjustment = FInt.One; //in case we need it
                    if ( this.NextConversion != null &&
                         (this.NextConversion.TagForUnit == "DZTransport" ||
                          this.NextConversion.NameForUnit == "DZTransport") )
                        adjustment = FInt.FromParts( 2, 000 );
                    score += (needed * adjustment).IntValue;
                    debugCode = 900;
                    if ( this.Inventory.ContainsKey( kv.Key ) && this.Inventory[kv.Key] >= this.NeedForResource[kv.Key] ) //if this would satisfy all the resource need, bump it up
                        score *= 2;
                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception during HowMuchDoWeNeedResourcesFrom code " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            TempCollectionHelper.ReleaseTemporaryDZResourceIntDict( IncomingResources );
            return score;
        }

        public bool WantsResourcesFrom( DarkZenithPerUnitBaseInfo other )
        {
            bool foundNeededResource = false;
            foreach ( KeyValuePair<DZResource, int> kv in this.NeedForResource )
            {
                if ( other.Inventory.ContainsKey( kv.Key ) )
                {
                    if ( kv.Value > 0 && other.Inventory[kv.Key] > 0 )
                    {
                        foundNeededResource = true;
                        break;
                    }
                }
            }
            return foundNeededResource;
        }
        public void TransferThisResourceFrom( DZResource resource, ref DarkZenithPerUnitBaseInfo other )
        {
            if ( other.Inventory[resource] > 0 )
            {
                this.Inventory[resource] += other.Inventory[resource];
                other.Inventory[resource] = 0;
            }
        }
        public void TransferFullInventoryFrom( ref DarkZenithPerUnitBaseInfo other, ref string logString )
        {
            foreach ( KeyValuePair<DZResource, int> kv in other.Inventory )
            {
                if ( kv.Value > 0 )
                {
                    logString += "Transferring " + kv.Value + " " + kv.Key.ToString();
                    this.Inventory[kv.Key] += kv.Value;
                    other.Inventory[kv.Key] = 0;
                }
            }
        }
        public bool CanWeDoResourceConversion( DZResourceConversion conversion )
        {
            if ( conversion == null )
                return false;
            foreach ( KeyValuePair<DZResource, int> kv in conversion.Cost )
            {
                int currentValue = 0;
                this.Inventory.TryGetValue( kv.Key, out currentValue );
                if ( currentValue < kv.Value )
                    return false;
            }
            return true;
        }
        public void PayForResourceConversion( DZResourceConversion conversion )
        {
            if ( conversion == null )
                throw new Exception( "Somehow a null conversion?" );
            DZResource debugKey = DZResource.None;
            int debugCode = 0;
            try
            {
                debugCode = 100;
                foreach ( KeyValuePair<DZResource, int> kv in conversion.Cost )
                {
                    debugKey = kv.Key;
                    debugCode = 200;
                    int currentInventoryValue = 0;
                    //                    ArcenDebugging.ArcenDebugLogSingleLine("When paying for " + conversion.ToString() + " which costs " +  conversion.Cost[key] + " " + key.ToString(), Verbosity.DoNotShow );
                    if ( kv.Value <= 0 )
                        continue;
                    this.Inventory.TryGetValue( kv.Key, out currentInventoryValue );
                    debugCode = 300;
                    if ( currentInventoryValue < kv.Value )
                        throw new Exception( "Could not pay for conversion! Put more debug data here" );
                    debugCode = 400;
                    this.Inventory[kv.Key] -= kv.Value;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in PayForResourceConversion debugCode " + debugCode + " key " + debugKey.ToString() + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        public void RefundThisResourceConversion( DZResourceConversion conversion )
        {
            foreach ( KeyValuePair<DZResource, int> kv in conversion.Cost )
            {
                if ( kv.Value > 0 )
                    this.Inventory[kv.Key] += kv.Value;
            }
        }

        //returns whether this conversion could be legally done.
        //if it can be done, execute it
        //Situations where things can't be done is currently
        //just the case where we want to build a terminus/epistyle but there
        //are no valid planets.
        //We pass in both the Constructors and Economic Structures to help us enforce our "not too many on a planet" restrictions
        public void ExecuteResourceConversion( GameEntity_Squad structure, DZResourceConversion conversion, DarkZenithFactionBaseInfoRoot dzBaseInfoRoot, DarkZenithDifficulty Difficulty,
            List<SafeSquadWrapper> AllEconomicStructures, List<SafeSquadWrapper> Constructors, List<SafeSquadWrapper> Utilities,
            bool tracing, ArcenCharacterBuffer tracingBuffer, ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData )
        {
            int debugCode = 0;
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //don't even try this on clients

            try
            {
                debugCode = 1000;
                if ( tracing ) tracingBuffer.Add( "Attempting to execute conversion " ).Add( conversion.ToString() ).Add( "\n" );
                this.TimeWeLastDidConversion = World_AIW2.Instance.GameSecond;
                PayForResourceConversion( conversion );
                GenerateOutputResources( conversion );
                debugCode = 1100;
                if ( conversion.Upgrade != null )
                {
                    tracingBuffer?.Add( "Applying upgrade ").Add( conversion.Upgrade.InternalName );
                    dzBaseInfoRoot.ApplyThisUpgrade( conversion.Upgrade );
                    dzBaseInfoRoot.CompletedUpgrades.Add( conversion.Upgrade );
                    return;
                }
                debugCode = 1200;
                GameEntityTypeData entityToCreate = conversion.Unit;
                if ( entityToCreate == null && (!String.IsNullOrEmpty( conversion.TagForUnit ) || !String.IsNullOrEmpty( conversion.NameForUnit )) )
                {
                    //translate the tag or name into the GameEntityTypeData
                    debugCode = 1300;
                    if ( !String.IsNullOrEmpty( conversion.TagForUnit ) )
                        entityToCreate = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, conversion.TagForUnit );
                    else if ( !String.IsNullOrEmpty( conversion.NameForUnit ) )
                        entityToCreate = GameEntityTypeDataTable.Instance.GetRowByName( conversion.NameForUnit );
                    if ( entityToCreate == null )
                        throw new Exception( "Couldn't find entity to create for " + conversion.ToString() );
                }

                if ( entityToCreate == null )
                {
                    return; //no entity to make. Probably shouldn't ever happen
                }
                debugCode = 1400;
                byte MarkLevelToUse = 0;
                if ( conversion.TakeFactionMarkLevelMinusThis >= 0 )
                {
                    Faction facOrNull = structure.GetFactionOrNull_Safe();
                    MarkLevelToUse = (byte)((facOrNull == null ? (byte)1 : facOrNull.CurrentGeneralMarkLevel) - conversion.TakeFactionMarkLevelMinusThis);
                    if ( MarkLevelToUse <= 0 )
                        MarkLevelToUse = 1;
                    if ( MarkLevelToUse > 7 ) //overflow
                        MarkLevelToUse = 7;
                }
                else if ( conversion.MarkLevel > 0 )
                    MarkLevelToUse = conversion.MarkLevel;
                else
                    throw new Exception( "Invalid conversion <" + conversion.ToString() + "> we don't know the intended mark level\n" );

                if ( entityToCreate.IsMobileCombatant || entityToCreate.GetHasTag( "DZHarvester" ) )
                {
                    //This is intended to be "everything that isn't a structure or economic thing like a Transport". Transports are a corner case
                    //and could be built here, I suppose
                    debugCode = 1500;
                    PlanetFaction pFaction = structure.PlanetFaction;
                    for ( int i = 0; i < conversion.NumberOfUnits; i++ )
                    {
                        GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityToCreate, MarkLevelToUse,
                                                                                 pFaction.Faction.LooseFleet, 0, structure.WorldLocation, Context, "DarkZenithConversion-Helper" );
                        if ( newEntity != null )
                        {
                            newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                            newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                            if ( pFaction.Faction.Type == FactionType.Player && entityToCreate.IsMobileCombatant )
                            {
                                DarkZenithPerUnitBaseInfo data = newEntity.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>("DarkZenithPerUnitBaseInfo");
                                if ( data != null )
                                    data.HasRalliedToFlagship = false;
                            }
                            if ( entityToCreate.IsMobileCombatant && dzBaseInfoRoot.IsPlayer )
                            {
                                DarkZenithPerUnitBaseInfo epistyleData = structure.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>("DarkZenithPerUnitBaseInfo");
                                if ( epistyleData != null )
                                {
                                    //this should always be the case, but paranoia
                                    GameEntity_Squad flagship = epistyleData.PreferredFlagship.GetSquad();
                                    if ( flagship != null && flagship.FleetMembership != null && flagship.FleetMembership.Fleet != null )
                                    {
                                        FleetMembership originalMem = newEntity.FleetMembership;
                                        if ( originalMem != null )
                                            newEntity.FleetMembership.RemoveEntity( newEntity, false );
                                        FleetMembership mem = flagship.FleetMembership.Fleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( entityToCreate );
                                        mem.AddEntityToFleetMembership( newEntity, "DarkZenithSidekickFleet" );
                                        SquadRegistryInfo registryInfo = newEntity.GetCentralRegistryInfo_Expensive();
                                        if ( registryInfo != null ) {
                                            registryInfo.Fleet = flagship.FleetMembership.Fleet;
                                        }

                                    }

                                }
                            }
                        }
                    }
                }
                else
                {
                    //This is the "Build Structures" path; we will build a Constructor, then assign it
                    //what and where to build. Then the LRP code will move the Constructor and it will build in the Sim code
                    //when it arrives
                    debugCode = 2000;
                    PlanetFaction pFaction = structure.PlanetFaction;

                    //Rules: defensive structures go next to existing epistyles/terminii
                    //       Epistyles, Utility and Terminii have rules about "How Many Per Planet" from the Difficulty (and can be Upgraded)

                    if ( tracing )
                        tracingBuffer.Add( "\t" ).Add( "Trying to find a planet to build " + entityToCreate.GetDisplayName() ).Add( ". When calculating, we have " + AllEconomicStructures.Count + " economic structures and " + Constructors.Count + " constructors.\n" );

                    List<Planet> PotentialPlanets = Planet.GetTemporaryPlanetList( "DZ-PerUnit-PotentialPlanets", 10f );
                    if ( PotentialPlanets == null ) //blocked for teardown/shutdown; bail
                        return;
                    DictionaryOfLists<Planet, SafeSquadWrapper> WorkingStructuresOnThisPlanet = GameEntity_Squad.GetTemporarySquadsPerPlanetDictOfLists( "DZ-PerUnit-WorkingStructuresOnThisPlanet", 10f );
                    if ( WorkingStructuresOnThisPlanet == null ) //blocked for teardown/shutdown; bail
                    {
                        Planet.ReleaseTemporaryPlanetList( PotentialPlanets ); //release already-acquired temp before bailing
                        return;
                    }

                    UpdatePotentialPlanetsToBuildOn( PotentialPlanets, WorkingStructuresOnThisPlanet, structure, entityToCreate, AllEconomicStructures, Constructors, Utilities, Difficulty, dzBaseInfoRoot, tracing, tracingBuffer, Context, PathCacheData );
                    if ( PotentialPlanets.Count == 0 )
                    {
                        debugCode = 2600;
                        //no safe place to build, so bail out.
                        //Also refund ourselves
                        if ( tracing )
                            tracingBuffer.Add( "We couldn't find a safe planet to build on, so take a refund.\n" );
                        RefundThisResourceConversion( conversion );

                        Planet.ReleaseTemporaryPlanetList( PotentialPlanets );
                        GameEntity_Squad.ReleaseTemporarySquadsPerPlanetDictOfLists( WorkingStructuresOnThisPlanet );
                        return;
                    }
                    debugCode = 3000;
                    //Pick which planet to use.
                    Planet planetToUse = null;
                    if ( entityToCreate.GetHasTag( "WarpingInDZEpistyle" ) || entityToCreate.GetHasTag( "WarpingInDZTerminus" ) )
                    {
                        debugCode = 3130;
                        //if we are building a new epistyle or terminus, prefer new planets
                        for ( int i = 0; i < PotentialPlanets.Count; i++ )
                        {
                            debugCode = 3160;
                            if ( WorkingStructuresOnThisPlanet[PotentialPlanets[i]] == null ||
                                 WorkingStructuresOnThisPlanet[PotentialPlanets[i]].Count < 1 )
                            {
                                planetToUse = PotentialPlanets[i];
                                break;
                            }
                        }
                    }
                    if ( entityToCreate.GetHasTag( "DZTransport" ) )
                        planetToUse = structure.Planet; //just do it on this planet
                    debugCode = 3200;
                    if ( planetToUse == null )
                    {
                        //if we didn't find a planet already, pick one at random
                        debugCode = 3190;
                        planetToUse = PotentialPlanets[Context.RandomToUse.Next( 0, PotentialPlanets.Count )];
                    }
                    if ( planetToUse == null )
                        return;
                    GameEntityTypeData typedata = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DZConstructor" );
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typedata, 1,
                                                                             pFaction.Faction.LooseFleet, 0, structure.WorldLocation, Context, "DarkZenithConversion-Constructor" );
                    newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                    DarkZenithPerUnitBaseInfo data = newEntity.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    data.Unit = entityToCreate;
                    data.TimeWeLastDidConversion = World_AIW2.Instance.GameSecond;
                    if ( entityToCreate.GetHasTag( "WarpingInDZTerminus" ) )
                    {
                        //make sure we are building the right terminus type
                        data.Resource = conversion.RelatedResource;
                        if ( conversion.RelatedResource == DZResource.None )
                            throw new Exception( "Found bad conversion <" + conversion.ToString() + ">: creates " + entityToCreate.GetDisplayName() + " but has releated resource " + conversion.RelatedResource );
                    }

                    data.MarkLevel = MarkLevelToUse;

                    data.NumberOfUnits = conversion.NumberOfUnits;
                    debugCode = 3100;

                    debugCode = 3300;
                    data.DZConstructorTargetPlanetIndex = planetToUse.Index; //this seems to be the only place that DZConstructorTargetPlanetIndex is set.
                    //figure out where to place the unit on the planet
                    if ( entityToCreate.GetHasTag( "DZDefensiveStructure" ) )
                    {
                        debugCode = 3400;
                        if ( WorkingStructuresOnThisPlanet[planetToUse].Count <= 0 )
                            throw new Exception( "There should always be structures when building a defensive structure (that was checked earlier)." );
                        GameEntity_Squad structureToDefend = WorkingStructuresOnThisPlanet[planetToUse][Context.RandomToUse.Next( 0, WorkingStructuresOnThisPlanet[planetToUse].Count )].GetSquad();
                        if ( structureToDefend != null )
                            data.DestinationPoint = planetToUse.GetSafePlacementPoint_AroundEntity( Context, entityToCreate, structureToDefend, FInt.FromParts( 0, 025 ), FInt.FromParts( 0, 100 ) );
                    }
                    else if ( entityToCreate.GetHasTag( "DZUtilityStructure" ) )
                    {
                        debugCode = 3500;
                        //pick a random adjacent planet, then build the utility structure near the wormhole to that planet
                        Planet otherPlanet = planetToUse.GetRandomNeighbor( false, Context );
                        if ( otherPlanet == null )
                            throw new Exception( "Could not find random neighbor for " + planetToUse.Name );
                        GameEntity_Other wormhole = planetToUse.GetWormholeTo( otherPlanet );

                        data.DestinationPoint = planetToUse.GetSafePlacementPoint_AroundEntity( Context, entityToCreate, wormhole, FInt.FromParts( 0, 025 ), FInt.FromParts( 0, 075 ) );
                    }
                    else if ( entityToCreate.GetHasTag( "DZTransport" ) )
                    {
                        debugCode = 3600;

                        data.DestinationPoint = planetToUse.GetSafePlacementPoint_AroundEntity( Context, entityToCreate, structure, FInt.FromParts( 0, 125 ), FInt.FromParts( 0, 250 ) );
                    }
                    else//just pick at random
                    {
                        data.DestinationPoint = planetToUse.GetSafePlacementPointAroundPlanetCenter( Context, entityToCreate, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 700 ) );
                        if ( pFaction.Faction.Type == FactionType.Player )
                        {
                            //DZ Sidekicks prefer not to put their valuable structures near wormholes
                            int retries = 100;
                            do
                            {
                                if ( data.DestinationPoint == ArcenPoint.ZeroZeroPoint )
                                     data.DestinationPoint = planetToUse.GetSafePlacementPointAroundPlanetCenter( Context, entityToCreate, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 750 ) );
                                int range = 100 * retries;
                                foreach ( GameEntity_Other wormhole in planetToUse.Wormholes() )
                                {
                                    if ( Mat.DistanceBetweenPointsImprecise( wormhole.WorldLocation, data.DestinationPoint ) < range )
                                    {
                                        data.DestinationPoint = ArcenPoint.ZeroZeroPoint;
                                        break;
                                    }
                                }
                            }
                            while ( data.DestinationPoint == ArcenPoint.ZeroZeroPoint && retries-- > 0);

                            if ( data.DestinationPoint == ArcenPoint.ZeroZeroPoint ) //just in case
                                data.DestinationPoint = planetToUse.GetSafePlacementPointAroundPlanetCenter( Context, entityToCreate, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 750 ) );

                        }
                    }
                    debugCode = 4000;
                    if ( tracing )
                        tracingBuffer.Add( "We have created " + newEntity.ToStringWithPlanet() + " to build a " + data.Unit.GetDisplayName() + " on " + planetToUse.Name + " at point " + data.DestinationPoint + ". Paranoically checking " + structure.ToStringWithPlanet() + " planet index: " + this.DZConstructorTargetPlanetIndex + " resource " + data.Resource ).Add( "\n" );

                    Planet.ReleaseTemporaryPlanetList( PotentialPlanets );
                    GameEntity_Squad.ReleaseTemporarySquadsPerPlanetDictOfLists( WorkingStructuresOnThisPlanet );
                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in ExecuteResourceConversion for " + structure.ToStringWithPlanet() + " debugCode " + debugCode + " for " + conversion.ToString() + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        public static void UpdatePotentialPlanetsToBuildOn( List<Planet> PotentialPlanets, DictionaryOfLists<Planet, SafeSquadWrapper> WorkingStructuresOnThisPlanet, 
            GameEntity_Squad structure, GameEntityTypeData entityToCreate, List<SafeSquadWrapper> AllEconomicStructures,
            List<SafeSquadWrapper> Constructors, List<SafeSquadWrapper> Utilities, DarkZenithDifficulty Difficulty, DarkZenithFactionBaseInfoRoot DarkZenithBaseInfoRoot, bool tracing,
            ArcenCharacterBuffer tracingBuffer, ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData )
        {
            //Note that this is called by the GetNextConversion code path in the main DZ sim code (to see if we can legally build)
            //as well as in the ExecuteConversion path where we would actually build
            //the output of that data is just used to say "Is there someplace I can expand to?"
            //takes as arguments the structure where we we be building from
            //updates the PotentialPlanets List and also fills the WorkingStructuresOnThisPlanet lookup
            //we are single threaded through all copies of the DZ faction code, so it's safe to use static lists
            WorkingStructuresOnThisPlanet.Clear();
            PotentialPlanets.Clear();
            int debugCode = 0;
            try
            {
                Faction faction = structure.GetFactionOrNull_Safe();
                foreach ( Planet.PlanetAtHopDistance _phd in structure.Planet.PlanetsWithinXHops( -1,
                   delegate ( Planet secondaryPlanet )
                   {
                       PlanetFaction pFaction = secondaryPlanet.GetPlanetFactionForFaction( faction );
                       if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength >
                            (pFaction.DataByStance[FactionStance.Self].TotalStrength + pFaction.DataByStance[FactionStance.Friendly].TotalStrength) / 2 )
                           return PropogationEvaluation.No;
                       return PropogationEvaluation.Yes;
                   } ) )
                {
                    Planet planet = _phd.Planet;
                    debugCode = 2100;
                    if ( planet == null )
                        continue; //paranoid checking
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
                    if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength >
                         (pFaction.DataByStance[FactionStance.Self].TotalStrength + pFaction.DataByStance[FactionStance.Friendly].TotalStrength) / 2 )
                        continue; //no dangerous planets
                    if ( planet.GetControllingOrInfluencingFaction().GetIsHostileTowards( faction ) &&
                         (entityToCreate.GetHasTag( "DZEpistyle" ) ||
                           entityToCreate.GetHasTag( "WarpingInDZEpistyle" )) )
                    {
                        //no epistyles on enemy owned planets
                        continue;
                    }

                    int numEpistyles = 0;
                    int numTerminii = 0;
                    int numUtilities = 0;
                    debugCode = 2200;
                    for ( int i = 0; i < AllEconomicStructures.Count; i++ )
                    {
                        debugCode = 2300;
                        GameEntity_Squad entity = AllEconomicStructures[i].GetSquad();
                        if ( entity == null )
                            continue;
                        if ( entity.Planet != planet )
                            continue;
                        if ( entity.TypeData.GetHasTag( "DZTerminus" ) ||
                             entity.TypeData.GetHasTag( "WarpingInDZTerminus" ) )
                        {
                            numTerminii++;
                            WorkingStructuresOnThisPlanet[entity.Planet].Add( entity );
                        }
                        if ( entity.TypeData.GetHasTag( "DZEpistyle" ) ||
                             entity.TypeData.GetHasTag( "WarpingInDZEpistyle" ) )
                        {
                            WorkingStructuresOnThisPlanet[entity.Planet].Add( entity );
                            numEpistyles++;
                        }
                    }
                    debugCode = 2400;
                    for ( int i = 0; i < Utilities.Count; i++ )
                    {
                        GameEntity_Squad entity = Utilities[i].GetSquad();
                        if ( entity == null )
                            continue;
                        if ( entity.Planet != planet )
                            continue;
                        numUtilities++;
                    }
                    debugCode = 2500;
                    for ( int i = 0; i < Constructors.Count; i++ )
                    {
                        debugCode = 2600;
                        GameEntity_Squad constructor = Constructors[i].GetSquad();
                        if ( constructor == null )
                            continue;
                        debugCode = 2610;
                        DarkZenithPerUnitBaseInfo constructorData = constructor.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                        if ( constructorData.DZConstructorTargetPlanetIndex != planet.Index )
                            continue; //this constructor is off to a different planet than this one
                        debugCode = 2620;
                        if ( constructorData.Unit == null )
                            continue;
                        if ( constructorData.Unit.GetHasTag( "DZUtilityStructure" ) )
                            numUtilities++;
                        if ( constructorData.Unit.GetHasTag( "DZTerminus" ) || constructorData.Unit.GetHasTag( "WarpingInDZTerminus" ) )
                            numTerminii++;
                        if ( constructorData.Unit.GetHasTag( "DZEpistyle" ) || constructorData.Unit.GetHasTag( "WarpingInDZEpistyle" ) )
                            numEpistyles++;
                    }

                    debugCode = 2700;
                    int terminiiComponentFromUpgrades = DarkZenithBaseInfoRoot.AdditionalTerminiiAllowedToBuild();
                    int epistyleComponentFromUpgrades = DarkZenithBaseInfoRoot.AdditionalEpistylesAllowedToBuild();
                    int utilityComponentFromUpgrades = DarkZenithBaseInfoRoot.AdditionalUtilityAllowedToBuild();

                    if ( tracing )
                    {
                        tracingBuffer.Add( "\t\tSeriously considering whether we can create a " + entityToCreate.GetDisplayName() + " on " ).Add( planet.Name ).Add( " numTerminii " ).Add( numTerminii ).Add( " numEpistyles " ).Add( numEpistyles ).Add( " numUtilities " ).Add( numUtilities ).Add( " max terminii per planet (" ).Add( Difficulty.MaxTerminiiPerPlanet ).Add( " + " ).Add( terminiiComponentFromUpgrades ).Add( ") max epistyles per planet: (" ).Add( Difficulty.MaxEpistylesPerPlanet ).Add( " + " ).Add( epistyleComponentFromUpgrades ).Add( ") " ).Add( " max utilities (" + (utilityComponentFromUpgrades + Difficulty.MaxUtilityStructuresPerPlanet) + ")\n" );
                    }
                    debugCode = 2800;

                    //Make sure we don't have too many units of this type already
                    if ( Difficulty.MaxTerminiiPerPlanet + terminiiComponentFromUpgrades <= numTerminii &&
                         entityToCreate.GetHasTag( "WarpingInDZTerminus" ) || entityToCreate.GetHasTag( "DZTerminus" ) )
                    {
                        continue;
                    }
                    debugCode = 2900;
                    if ( Difficulty.MaxEpistylesPerPlanet + epistyleComponentFromUpgrades <= numEpistyles &&
                         entityToCreate.GetHasTag( "WarpingInDZEpistyle" ) || entityToCreate.GetHasTag( "DZEpistyle" ) )
                    {
                        continue;
                    }
                    debugCode = 3000;
                    if ( Difficulty.MaxUtilityStructuresPerPlanet + utilityComponentFromUpgrades <= numUtilities &&
                         entityToCreate.GetHasTag( "DZUtilityStructure" ) )
                    {
                        continue;
                    }

                    debugCode = 3100;

                    //defensive structures are always built near structures worth defending
                    if ( (WorkingStructuresOnThisPlanet[planet] == null ||
                          WorkingStructuresOnThisPlanet[planet].Count == 0) &&
                         (entityToCreate.GetHasTag( "DZDefensiveStructure" ) || entityToCreate.GetHasTag( "DZUtilityStructure" )) )
                        continue;

                    Int16 hops = 0;
                    if ( planet != structure.Planet )
                    {
                        debugCode = 3200;
                        //don't bother with this check if we're on the same planet (slight perf increase)
                        int danger = Fireteam.GetDangerOfPath( structure.GetFactionOrNull_Safe(), Context, PathCacheData, structure.Planet, planet, false, out hops );
                        if ( danger > 2000 ) //only build on planets we can get to safely
                        {
                            continue;
                        }
                    }
                    if ( tracing )
                        tracingBuffer.Add( "\t\t" + planet.Name + " is available to build " + entityToCreate.GetDisplayName() ).Add( "\n" );
                    PotentialPlanets.Add( planet );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in UpdatePotentialPlanetsToBuildOn code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        public void FillHackingBuffer(ArcenDoubleCharacterBuffer buffer)
        {
            if ( this.NextConversion == null )
            {
                buffer.Add("No next conversion");
                return;
            }
            buffer.Add(NextConversion.DisplayName, "a1ffa1");
            //NextConversion.ToHackBuffer(buffer); //this is too long
        }
        public void FillEpistyleNeedsBuffer( ArcenDoubleCharacterBuffer buffer )
        {
            //Lists the resource types this epistyle currently wants, in enum order. This is what actually
            //distinguishes two same-type epistyles on the same planet for the player choosing a deposit target.
            bool any = false;
            for ( DZResource res = DZResource.Metal; res < DZResource.End; res = (DZResource)( (byte)res + 1 ) )
            {
                int need;
                if ( !this.NeedForResource.TryGetValue( res, out need ) || need <= 0 )
                    continue;
                buffer.Add( any ? ", " : "" ).Add( res.ToString(), "a1ffa1" );
                any = true;
            }
            if ( !any )
                buffer.Add( "nothing right now", "8a8aff" );
        }
        public void TransferRequestedInventoryFrom( ref DarkZenithPerUnitBaseInfo other, ref string logString )
        {
            //This transfers all the resources of the desired types from "other" to "this"
            int debugCode = 0;
            try
            {
                foreach ( KeyValuePair<DZResource, int> kv in other.Inventory )
                {
                    debugCode = 110;
                    if ( kv.Value <= 0 )
                    {
                        debugCode = 120;
                        logString += " other doesn't have any " + kv.Key.ToString();
                        continue;
                    }
                    debugCode = 130;
                    if ( !this.NeedForResource.ContainsKey( kv.Key ) || this.NeedForResource[kv.Key] <= 0 )
                        continue;
                    int resourcesToGive = kv.Value; //dump everything, not just the specifically requested amount
                    debugCode = 300;
                    logString += "Transferring " + resourcesToGive + " " + kv.Key.ToString() + "\n";
                    this.Inventory[kv.Key] += resourcesToGive;
                    other.Inventory[kv.Key] -= resourcesToGive;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception during TransferRequestedInventoryFrom code " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        public void TransferRequiredResourcesWithConversionMax( ref DarkZenithPerUnitBaseInfo other, int maxConversionsWorth, ref string logString )
        {
            //This transfers all the resources of the desired types from "other" to "this"
            int debugCode = 0;
            try
            {
                debugCode = 1;
                Dictionary<DZResource, int> dict = this.NextConversion.Cost;
                foreach ( KeyValuePair<DZResource, int> kv in dict )
                {
                    debugCode = 10;
                    int maxToTransfer = 0;
                    int resourcesToGive = 0;
                    if ( kv.Value != 0 )
                    {
                        debugCode = 20;
                        maxToTransfer = kv.Value * maxConversionsWorth;
                        if ( other.Inventory.ContainsKey( kv.Key ) )
                        {
                            int availableResources = other.Inventory[kv.Key];
                            resourcesToGive = Math.Min( availableResources, maxToTransfer );
                            logString += "Transferring " + resourcesToGive + " " + kv.Key.ToString() + "\n";
                            this.Inventory[kv.Key] += resourcesToGive;
                            other.Inventory[kv.Key] -= resourcesToGive;
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception during TransferRequiredResourcesWithConversionMax code " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        public void GenerateOutputResources( DZResourceConversion conversion )
        {
            //how much resource is created from this conversion
            foreach ( KeyValuePair<DZResource, int> kv in conversion.OutputResource )
            {
                if ( kv.Value > 0 )
                {
                    this.Inventory[kv.Key] += kv.Value;
                }
            }

        }
        public void AddResources( DZResource resource, int amount )
        {
            if ( !this.Inventory.ContainsKey( resource ) )
                this.Inventory[resource] = 0;
            this.Inventory[resource] += amount;
        }

    }
}
