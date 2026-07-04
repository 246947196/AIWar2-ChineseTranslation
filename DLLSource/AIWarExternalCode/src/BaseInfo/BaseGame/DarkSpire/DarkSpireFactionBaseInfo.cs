using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DarkSpireFactionBaseInfo : ExternalFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {
        //these track the net and total energy on a per-planet basis
        //total tracks all the energy gained on each planet overall,
        //net tracks the current available energy for each planet. We use the Index
        //to make it easy to serialize/deserialize
        public readonly ProtectedValDictionary<Int16, DarkSpirePerPlanet> PerPlanet = ProtectedValDictionary<Int16, DarkSpirePerPlanet>.Create_WillNeverBeGCed( 300, "DarkSpireFactionBaseInfo-PerPlanet" );
        public int LastLocusAttemptTime;
        public int TimeForNextVengeanceStrike;
        public bool DarkSpireGeneratesEnergy;
        public bool ConquestMode; //the Dark Spire wishes for Peace. The Peace of a Sterile Galaxy
                                  //The Dark Spire will generate energy constantly and use fireteams, instead of wandering vaguely
        public readonly ArcenLessLinkedList<Fireteam> Teams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "DarkSpireFactionBaseInfo-Teams" );

        //not serialized;
        public static DarkSpireFactionBaseInfo Instance; //there can only ever be one dark spire at a time
        public int Intensity;
        public bool UseLoci = true;
        public bool UseAlternateSpawningStyle = false;
        public int LocusAttemptInterval = 0;
        public int ConversionRatioCap = 0; //this is not serialized to disk
        public int BonusConversionRatioPerAttack = 0;//the one we use for the math
        public readonly DoubleBufferedList<Planet> DarkSpirePlanets = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 300, "DarkSpire-DarkSpirePlanets" );
        public readonly DoubleBufferedList<Planet> WardedPlanets= DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 300, "DarkSpire-WardedPlanets" ); //these are planets with Dark Spire Wards on them
        public readonly DoubleBufferedList<SafeSquadWrapper> Loci = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "DarkSpire-Loci" );
        public readonly DoubleBufferedList<SafeSquadWrapper> VGs = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "DarkSpire-VGs" );

        public DarkSpireFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            if ( this.PerPlanet.Count > 0 )
                this.PerPlanet.Clear();
            this.LastLocusAttemptTime = -1;
            this.TimeForNextVengeanceStrike = -1;
            this.DarkSpireGeneratesEnergy = false;
            this.ConquestMode = false;

            Teams.Clear();

            this.Intensity = 0;
            UseLoci = false;
            UseAlternateSpawningStyle = false;
            LocusAttemptInterval = 0;
            this.ConversionRatioCap = 0;
            BonusConversionRatioPerAttack = 0;
            DarkSpirePlanets.Clear();
            WardedPlanets.Clear();
            Loci.Clear();
            VGs.Clear();

            Instance = null;
            HaveLoadedData = false; //trigger a reload of xml
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            DoRefreshFromFactionSettings();

            int load = 60 + (Intensity * 5);

            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( load ).Add( " Load From Dark Spire" );
            return load;
        }

        #region Xml Data
        //these are shared by all instances of the dark spire
        //these are data from xml files, so having them persist through savegames (but not application restart) is okay.
        private static bool HaveLoadedData;
        public static int defaultConversionRatioLowIntensity = 0;
        public static int defaultConversionRatioMediumIntensity = 0;
        public static int defaultConversionRatioHighIntensity = 0;
        public static int BonusConversionRatioForIntensity = 0;
        public static int BonusConversionRatioPerAttackLow = 0;
        public static int BonusConversionRatioPerAttackMed = 0;
        public static int BonusConversionRatioPerAttackHigh = 0;
        public static int ConversionRatioCapLow = 0;
        public static int ConversionRatioCapMed = 0;
        public static int ConversionRatioCapHigh = 0;
        public static FInt defaultEnergyThreshold;
        public static int percentShareEnergy = 0;
        public static int energyToShare = 0;
        public static int LocusWarpInTime = 0;
        public static int LocusAttemptIntervalNonConquest = 0;
        public static int LocusAttemptIntervalConquestMode = 0;
        public static int ShipDesignsDownloadble = 0;
        public static int BaseMinutesBetweenVengeanceStrike = 0;
        public static int TimeInMinutesForFirstVengeanceStrike = 0;
        public static FInt VengeanceStrikeEnergyMultiplier = FInt.Zero;
        public static int MinTimeInMinutesBeforeLocusesAreAllowed = 0;
        public static int EnergyThresholdIncreasePerAttack = 0;
        public static int EnergyIncomePerAIPPerMinute = 0; //this is enabled once the player hacks to download a dark spire ship
        public static FInt EnergyIncomeMultiplierLowIntensity = FInt.Zero;
        public static FInt EnergyIncomeMultiplierMediumIntensity = FInt.Zero;
        public static FInt EnergyIncomeMultiplierHighIntensity = FInt.Zero;
        private void LoadCustomDataIfNeeded()
        {
            if ( HaveLoadedData )
                return;
            HaveLoadedData = true;
            defaultConversionRatioLowIntensity = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_DefaultConversionRatioLowIntensity" );
            defaultConversionRatioMediumIntensity = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_DefaultConversionRatioMediumIntensity" );
            defaultConversionRatioHighIntensity = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_DefaultConversionRatioHighIntensity" );
            ConversionRatioCapLow = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_ConversionRatioCapLow" );
            ConversionRatioCapMed = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_ConversionRatioCapMed" );
            ConversionRatioCapHigh = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_ConversionRatioCapHigh" );
            BonusConversionRatioForIntensity = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_BonusConversionRatioForIntensity" );
            BonusConversionRatioPerAttackLow = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_BonusConversionRatioPerAttackLow" );
            BonusConversionRatioPerAttackMed = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_BonusConversionRatioPerAttackMed" );
            BonusConversionRatioPerAttackHigh = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_BonusConversionRatioPerAttackHigh" );
            defaultEnergyThreshold = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_DarkSpire_DefaultEnergyThreshold" );
            percentShareEnergy = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_PercentToShareEnergy" );
            energyToShare = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_EnergyToShare" );
            LocusWarpInTime = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_LocusWarpInTime" );
            LocusAttemptIntervalNonConquest = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_IntervalBetweenAttempedLoci" );
            LocusAttemptIntervalConquestMode = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_IntervalBetweenAttempedLociConquest" );
            ShipDesignsDownloadble = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_NumShipDesignsDownloadable" );
            BaseMinutesBetweenVengeanceStrike = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_BaseMinutesBetweenVengeanceStrike" );
            TimeInMinutesForFirstVengeanceStrike = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_TimeInMinutesForFirstVengeanceStrike" );
            VengeanceStrikeEnergyMultiplier = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_DarkSpire_VengeanceStrikeEnergyMultiplier" );
            MinTimeInMinutesBeforeLocusesAreAllowed = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_MinTimeInMinutesBeforeLocusesAreAllowed" );
            EnergyThresholdIncreasePerAttack = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_EnergyThresholdIncreasePerAttack" );
            EnergyIncomePerAIPPerMinute = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkSpire_EnergyIncomePerAIPPerMinute" );
            EnergyIncomeMultiplierLowIntensity = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_DarkSpire_EnergyIncomeMultiplierLowIntensity" );
            EnergyIncomeMultiplierMediumIntensity = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_DarkSpire_EnergyIncomeMultiplierMediumIntensity" );
            EnergyIncomeMultiplierHighIntensity = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_DarkSpire_EnergyIncomeMultiplierHighIntensity" );
            if ( EnergyIncomePerAIPPerMinute == 0 || LocusAttemptIntervalConquestMode == 0 )
            {
                throw new Exception( "Dark Spire: XML parsing issues." );
            }
        }
        #endregion

        #region Ser / Deser
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "DarkSpireFactionBaseInfo" );
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.PerPlanet.Count );
            foreach ( KeyValuePair<Int16, DarkSpirePerPlanet> pair in this.PerPlanet )
            {
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, pair.Key );
                pair.Value.SerializeTo( MetaData, Buffer, SerializationCmdType );
            }
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.LastLocusAttemptTime );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeForNextVengeanceStrike );
            Buffer.AddBool( MetaData, this.DarkSpireGeneratesEnergy );
            Buffer.AddBool( MetaData, this.ConquestMode );
            FireteamBaseUtility.SerializeFireteams( MetaData, Buffer, SerializationCmdType, this.Teams );
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "DarkSpireFactionBaseInfo" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "DarkSpireFactionBaseInfo Ext", TrackerStyle.ByTypeOnly );
            this.PerPlanet.Clear();

            Int16 numPlanets = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
            for ( int i = 0; i < numPlanets; i++ )
            {
                Int16 planetIdx = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
                DarkSpirePerPlanet planetData = DarkSpirePerPlanet.GetFromPoolOrCreate();
                planetData.DeserializeIntoSelf( MetaData, Buffer, SerializationCmdType );
                this.PerPlanet[planetIdx] = planetData;
            }

            this.LastLocusAttemptTime = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            this.TimeForNextVengeanceStrike = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            this.DarkSpireGeneratesEnergy = Buffer.ReadBool( MetaData );
            this.ConquestMode = Buffer.ReadBool( MetaData );

            FireteamBaseUtility.DeserializeFireteamsAndDiscardAnyExtraLeftovers( MetaData, Buffer, SerializationCmdType, this.Teams, "dark spire" );
            Buffer.StopTrackerByName( "DarkSpireFactionBaseInfo Ext" );
        }
        #endregion

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return Intensity;
        }

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
            LoadCustomDataIfNeeded();
        }
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            ConfigurationForFaction cfg = this.AttachedFaction.Config;

            Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );

            UseLoci = cfg.GetBoolValueForCustomFieldOrDefaultValue( "EnableExpansion", true );

            UseAlternateSpawningStyle = cfg.GetBoolValueForCustomFieldOrDefaultValue( "AlternateSpawningStyle", true );

            if ( World_AIW2.Instance.CurrentGalaxy != null )
            {
                foreach ( Planet planet in World_AIW2.Instance.CurrentGalaxy.Planets( true ) )
                {
                    try
                    {
                        InitPerPlanetDataIfMissing( planet.Index );
                    }
                    catch { } //it's possible to have a threading issue with this one, though that would be rare
                              //this one is so rare that it is worth handling, but not worth changing the structure of how it's stored (which is more efficient this way)
                }
            }


            if ( AttachedFaction.MinFireteamStrength == -1 )
                AttachedFaction.MinFireteamStrength = 2000;
            if ( AttachedFaction.MaxFireteamStrength == -1 )
                AttachedFaction.MaxFireteamStrength = 4000;

            if ( this.ConquestMode )
                this.LocusAttemptInterval = LocusAttemptIntervalConquestMode;
            else
                this.LocusAttemptInterval = LocusAttemptIntervalNonConquest;

            if ( this.TimeForNextVengeanceStrike == -1 )
                this.TimeForNextVengeanceStrike = TimeInMinutesForFirstVengeanceStrike * 60;

            FInt AIP = FactionUtilityMethods.Instance.GetCurrentAIP();
            if ( this.Intensity < 4 )
            {
                this.BonusConversionRatioPerAttack = BonusConversionRatioPerAttackLow;
                this.ConversionRatioCap = ConversionRatioCapLow + AIP.IntValue / 2;
            }
            else if ( this.Intensity <= 7 )
            {
                this.BonusConversionRatioPerAttack = BonusConversionRatioPerAttackMed;
                this.ConversionRatioCap = ConversionRatioCapMed + AIP.IntValue;
            }
            else
            {
                this.BonusConversionRatioPerAttack = BonusConversionRatioPerAttackHigh;
                this.ConversionRatioCap = ConversionRatioCapHigh + AIP.IntValue;
            }
        }
        #endregion

        #region InitPerPlanetDataIfMissing
        private void InitPerPlanetDataIfMissing( Int16 Index )
        {
            if ( PerPlanet.ContainsKey( Index ) )
                return; //don't replace it if it's already there!

            DarkSpirePerPlanet local = DarkSpirePerPlanet.GetFromPoolOrCreate();
            int intensity = this.Intensity;
            if ( intensity < 4 )
                local.ConversionRatio = defaultConversionRatioLowIntensity + intensity * BonusConversionRatioForIntensity;
            else if ( intensity < 8 )
                local.ConversionRatio = defaultConversionRatioMediumIntensity + intensity * BonusConversionRatioForIntensity;
            else
                local.ConversionRatio = defaultConversionRatioHighIntensity + intensity * BonusConversionRatioForIntensity;
            local.EnergyThresholdForAttack = defaultEnergyThreshold;
            local.hasBeenInitialized = true;
            local.lastTimeAttemptedLocus = World_AIW2.Instance.GameSecond;
            this.PerPlanet[Index] = local;
        }
        #endregion

        #region SetStartingFactionRelationships
        public override void SetStartingFactionRelationships()
        {
            //run the base logic first, just in case
            base.SetStartingFactionRelationships();
            //The Dark Spire doesn't like anyone
            AllegianceHelper.EnemyThisFactionToAll( this.AttachedFaction );
        }
        #endregion

        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            DarkSpirePlanets.ClearConstructionListForStartingConstruction();
            WardedPlanets.ClearConstructionListForStartingConstruction();
            Loci.ClearConstructionListForStartingConstruction();
            VGs.ClearConstructionListForStartingConstruction();

            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "VengeanceGenerator" ) )
            {
                //there can be at most 1 per planet
                DarkSpirePlanets.AddToConstructionList( entity.Planet );
                VGs.AddToConstructionList( entity );
            }
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "DarkSpireWard" ) )
            {
                WardedPlanets.AddToConstructionList( entity.Planet );
            }
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "VengeanceGeneratorLocus" ) )
            {
                Loci.AddToConstructionList( entity );
            }

            DarkSpirePlanets.SwitchConstructionToDisplay();
            WardedPlanets.SwitchConstructionToDisplay();
            Loci.SwitchConstructionToDisplay();
            VGs.SwitchConstructionToDisplay();
        }
        #endregion

        #region GetFireteamById
        public override Fireteam GetFireteamById( int id )
        {
            return FireteamBaseUtility.GetFireteamById( this.Teams, id );
        }
        #endregion

        #region PerformVengeanceStrike
        public void PerformVengeanceStrike()
        {
            foreach ( KeyValuePair<Int16, DarkSpirePerPlanet> pair in this.PerPlanet )
            {
                pair.Value.TimeToAwaken = World_AIW2.Instance.GameSecond;
                pair.Value.NetEnergy += pair.Value.EnergyThresholdForAttack * VengeanceStrikeEnergyMultiplier;
                if ( pair.Value.ConversionRatio < 150 )
                {
                    pair.Value.NetEnergy *= 5; //buff for relatively untouched VGs
                    pair.Value.NetEnergy /= 4;
                }
                pair.Value.NextAttackMustSpawnUnits = true;
            }
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
                debugStage = 150000;
                #region Fill NonSim Notifications List Relating To the Dark Spire
                DarkSpireFactionBaseInfo dsdata = FactionUtilityMethods.Instance.GetDarkSpireFactionBaseInfo();
                if ( dsdata != null && dsdata.TimeForNextVengeanceStrike > 0 &&
                   dsdata.TimeForNextVengeanceStrike <= World_AIW2.Instance.GameSecond + 10 * 60 )
                {
                    debugStage = 150010;
                    //the next vengeance strike is in 10 minutes or less, so drop a notification for it
                    NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                    fillData.eventTimeRemaining = dsdata.TimeForNextVengeanceStrike;
                    fillData.Faction = this.AttachedFaction;
                    NotificationNonSim notification = new NotificationNonSim();
                    notification.Assign( PublicDarkSpireNotifier.Instance, fillData, "", 0, "Incoming Dark Spire Vengeance Strikes", SortedNotificationPriorityLevel.Medium );
                }
                debugStage = 150020;
                if ( this.Loci.Count > 0 )
                {
                    debugStage = 150030;
                    NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                    fillData.Faction = this.AttachedFaction;
                    fillData.EntityList.AddRange( this.Loci.GetDisplayList() );
                    NotificationNonSim notification = new NotificationNonSim();
                    notification.Assign( PublicDarkSpireLocusNotifier.Instance, fillData, "", 0, "Dark Spire Vengeance Loci", SortedNotificationPriorityLevel.Major );
                }
                #endregion
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "DarkSpire.Notifications Error at debug stage " + debugStage + ":\n" + e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region GetShouldAttackNormallyExcludedTarget
        public override bool GetShouldAttackNormallyExcludedTarget( GameEntity_Squad Target )
        {
            if ( Target.TypeData.GetHasTag( "NormalPlanetNastyPick" ) ||
                (Target.TypeData.GetHasTag( "DSAA" ) && this.ConquestMode) )
                return true;
            if ( this.ConquestMode )
            {
                if ( Target.TypeData.IsCommandStation || Target.TypeData.GetHasTag( "WarpGate" ) )
                    return true;

            }
            return false;
        }
        #endregion
    }
}
