using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class NomadPlanetsFactionBaseInfo : ExternalFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {
        //serialized
        public int TimeForNextExoSpawn;
        public int TimeNomadCrashStarted;
        public int EstimatedTimeToCrash;
        public readonly Dictionary<Int16, int> MoveIntervalForCrash = Dictionary<short, int>.Create_WillNeverBeGCed( 300, "NomadPlanetsFactionBaseInfo-MoveIntervalForCrash" );

        //not serialized
        public static NomadPlanetsFactionBaseInfo Instance; //there can only ever be one of this faction at a time
        public int Intensity = 0;

        public readonly DoubleBufferedList<Planet> NomadPlanetList = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 300, "NomadPlanetList" );
        public readonly DoubleBufferedList<SafeSquadWrapper> NomadPlanetNexuses = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "NomadPlanetNexuses" );

        public NomadPlanetsFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            TimeForNextExoSpawn = -1;
            TimeNomadCrashStarted = -1;
            EstimatedTimeToCrash = -1;

            MoveIntervalForCrash.Clear();

            NomadPlanetList.Clear();
            NomadPlanetNexuses.Clear();

            Instance = null;
            HaveLoadedData = false; //trigger xml reload

            //ArcenDebugging.ArcenDebugLogSingleLine( "Nomad cleanup!", Verbosity.DoNotShow );
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( "5 Load From Nomad Planets" );
            return 5;
        }

        #region Serialization and Deserialization
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeForNextExoSpawn );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeNomadCrashStarted );

            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)MoveIntervalForCrash.Count );
            foreach ( KeyValuePair<short, int> kv in MoveIntervalForCrash )
            {
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, kv.Key );
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, kv.Value );
            }
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, EstimatedTimeToCrash );
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            TimeForNextExoSpawn = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            TimeNomadCrashStarted = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            MoveIntervalForCrash.Clear();

            Int16 count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
            for ( int i = 0; i < count; i++ )
            {
                Int16 planetIndex = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
                MoveIntervalForCrash[planetIndex] = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg );
            }
            EstimatedTimeToCrash = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
        }
        #endregion end Serialization and Deserialization

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return Intensity;
        }

        #region Xml Data
        public bool HaveLoadedData = false;
        public int BaseMoveTime = 1200;
        public int BaseMoveTimeSlow = 1600;
        public int BaseMoveTimeMedium = 1200;
        public int BaseMoveTimeFast = 800;

        public int InitialMoveTime = 900;
        public int VarianceBetweenMoveTimes = 600;
        public int VarianceBetweenMoveTimesCrash = 60;
        public int DistanceToMoveForCrash = 10; //this is a fallback value; in general the real value is calculated in FindNextNomadPoint
        public FInt ExoResponseStrengthPerPlayerPowerLevel = FInt.One; //this is in units of "AI Wave size"
        public int ExoResponseInterval = 10;
        public int MinCrashTime = 360;
        public int MaxCrashTime = 720;
        public int CrashTimeIncreasePerDistanceUnit = -1;
        public int CrashDistanceUnit = -1;
        public int ExoChanceOfIncludingExtragalactic = 30;
        private void LoadCustomDataIfNeeded( bool isDebugMode )
        {
            if ( this.HaveLoadedData )
                return;
            this.HaveLoadedData = true;
            BaseMoveTimeSlow = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NomadPlanets_BaseMoveTimeSlow" );
            BaseMoveTimeMedium = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NomadPlanets_BaseMoveTimeMedium" );
            BaseMoveTimeFast = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NomadPlanets_BaseMoveTimeFast" );
            InitialMoveTime = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NomadPlanets_InitialMoveTime" );
            VarianceBetweenMoveTimes = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NomadPlanets_VarianceBetweenMoveTimes" );
            VarianceBetweenMoveTimesCrash = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NomadPlanets_VarianceBetweenMoveTimesCrash" );
            DistanceToMoveForCrash = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NomadPlanets_DistanceToMoveForCrash" );
            ExoResponseStrengthPerPlayerPowerLevel = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_NomadPlanets_ExoResponseStrengthPerPlayerPowerLevel" );
            ExoResponseInterval = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NomadPlanets_ExoResponseInterval" );
            MinCrashTime = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NomadPlanets_MinCrashTime" );
            MaxCrashTime = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NomadPlanets_MaxCrashTime" );
            CrashTimeIncreasePerDistanceUnit = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NomadPlanets_CrashTimeIncreasePerDistanceUnit" );
            CrashDistanceUnit = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NomadPlanets_CrashDistanceUnit" );
            ExoChanceOfIncludingExtragalactic = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_NomadPlanets_ExoChanceOfIncludeExtragalactic" );
            if ( isDebugMode )
            {
                BaseMoveTime /= 20;
                InitialMoveTime /= 20;
                VarianceBetweenMoveTimes /= 5;
                VarianceBetweenMoveTimesCrash /= 5;
                ExoResponseStrengthPerPlayerPowerLevel /= 10;
            }
            //            ArcenDebugging.ArcenDebugLogSingleLine("The exo response interval is " + ExoResponseInterval, Verbosity.DoNotShow );
            if ( CrashDistanceUnit == -1 || CrashTimeIncreasePerDistanceUnit == -1 )
                throw new Exception( "Could not parse xml for nomad planets" );
        }
        #endregion

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
            //ArcenDebugging.ArcenDebugLogSingleLine( "Nomad assigned!", Verbosity.DoNotShow );
        }
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            ConfigurationForFaction cfg = this.AttachedFaction.Config;
            LoadCustomDataIfNeeded( cfg.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) );
            Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
        }
        #endregion

        #region WriteFactionSlotStatus
        public override void WriteFactionSlotStatus( ArcenCharacterBufferBase buffer )
        {
            string value = AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "Intensity", false );
            if ( value != null )
                buffer.Add( "星系数量: " ).Add( value );
        }
        #endregion

        #region SetStartingFactionRelationships
        public override void SetStartingFactionRelationships()
        {
            base.SetStartingFactionRelationships();
            Faction faction = this.AttachedFaction;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( faction == otherFaction )
                    continue;
                if ( otherFaction.Type == FactionType.NaturalObject )
                    continue;
                switch ( otherFaction.Type )
                {
                    case FactionType.Player:
                        faction.MakeFriendlyTo( otherFaction );
                        otherFaction.MakeFriendlyTo( faction );
                        break;
                    case FactionType.AI:
                    case FactionType.SpecialFaction:
                        faction.MakeHostileTo( otherFaction );
                        otherFaction.MakeHostileTo( faction );
                        break;
                }
            }
        }
        #endregion

        #region UpdatePowerLevel
        public override void UpdatePowerLevel()
        {
            List<Planet> planetList = NomadPlanetList.GetDisplayList();
            for ( int i = 0; i < planetList.Count; i++ )
            {
                Planet planet = planetList[i];
                if ( planet.HasPlanetBeenDestroyed )
                    continue;
                if ( planet.NomadTargetPlanetIdx == -1 )
                    continue;
                if ( planet.IsDisabledNomad )
                    continue;
                //this planet is not disabled but is en route to smash an AI homeworld. Max threat.
            }
        }
        #endregion

        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            string requestedSpeed = AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "NomadMovementSpeed", true );
            if ( requestedSpeed == "Slow" )
                BaseMoveTime = BaseMoveTimeSlow;
            else if ( requestedSpeed == "Medium" )
                BaseMoveTime = BaseMoveTimeMedium;
            else if ( requestedSpeed == "Fast" )
                BaseMoveTime = BaseMoveTimeFast;

            NomadPlanetList.ClearConstructionListForStartingConstruction();
            NomadPlanetNexuses.ClearConstructionListForStartingConstruction();

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.TypeData.Type == PlanetType.Nomad )
                {
                    NomadPlanetList.AddToConstructionListIfNotAlreadyIn( planet );
                }
            }

            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "NomadNexus" ) )
            {
                if (entity.Planet == null || entity.Planet.HasPlanetBeenDestroyed)
                    continue;

                NomadPlanetNexuses.AddToConstructionList( entity );
            }

            NomadPlanetList.SwitchConstructionToDisplay();
            NomadPlanetNexuses.SwitchConstructionToDisplay();

            AllegianceHelper.AllyThisFactionToHumans( AttachedFaction );
        }
        #endregion

        #region GetCrashTime
        public int GetCrashTime( Planet nomadPlanet, Planet targetPlanet, int distance )
        {
            //Nomad Planets start at base time for the crash, then it goes up based on distance between nomad and target to a max
            int baseCrashTime = MinCrashTime;
            int distanceUnits = distance / CrashDistanceUnit;
            int distanceBasedIncrease = distanceUnits * CrashTimeIncreasePerDistanceUnit;
            int totalCrashTime = baseCrashTime + distanceBasedIncrease;
            if ( totalCrashTime > MaxCrashTime )
                totalCrashTime = MaxCrashTime;
            return totalCrashTime;
        }
        #endregion

        #region DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost
        public override void DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost( ArcenClientOrHostSimContextCore Context, bool IsFirstCallToFactionOfThisTypeThisCycle )
        {
            //do this for the first faction of the type only, regardless of how many there are
            if ( !IsFirstCallToFactionOfThisTypeThisCycle )
                return;

            int debugStage = 1;
            try
            {
                debugStage = 240000;
                #region Fill NonSim Notifications List Relating to Nomad Planets
                NotifierFillData crashingNomadPlanetfillData = null;
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( planet.TypeData.Type != PlanetType.Nomad || planet.IsDisabledNomad )
                        continue;
                    if ( planet.NomadTargetPlanetIdx != -1 )
                    {
                        //we are a crashing nomad
                        if ( crashingNomadPlanetfillData == null )
                            crashingNomadPlanetfillData = NotifierFillData.GetFromPoolOrCreate();
                        crashingNomadPlanetfillData.PlanetList.Add( planet );
                        continue;
                    }
                }
                if ( crashingNomadPlanetfillData != null )
                {
                    //sort from soonest to move to latest to move
                    crashingNomadPlanetfillData.PlanetList.Sort( static delegate ( Planet Left, Planet Right )
                    {
                        return Left.TimeForNextMove.CompareTo( Right.TimeForNextMove );
                    } );
                    NotificationNonSim notification = new NotificationNonSim();
                    notification.Assign( PublicCrashingNomadPlanetNotifier.Instance, crashingNomadPlanetfillData, "", 0, "Crashing Nomad Planet Movement", SortedNotificationPriorityLevel.Major );
                }
                #endregion
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Nomad.Notifications Error at debug stage " + debugStage + ":\n" + e, Verbosity.ShowAsError );
            }
        }
        #endregion
    }
}
