using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    //Tracks internal data needed by the Human Marauders
    public class MarauderFactionBaseInfo : ExternalFactionBaseInfoRoot, IAntiMinorFactionWaveDataHolder
    {
        //serialized
        //these planets have recently been attacked by the marauders
        public readonly Dictionary<Planet, int> IneligiblePlanets = Dictionary<Planet, int>.Create_WillNeverBeGCed( 500, "MarauderFactionBaseInfo-IneligiblePlanets" ); 
        public FInt Budget; //current budget for the Marauders
        public Int16 TotalPlanetsCaptured; //capturing a planet from you gives the marauders a permanent buff to their Max Budget
        public FInt MarauderSpecificAIP;
        /* So the default Budget is intended to be something that a human player could face. However, this means the Marauders rarely attack planets deep in AI space until the player has weakened them.
           I'm going to have a separate budget that gets much higher income, but that is only eligible to attack AI planets */
        public FInt AIOnlyBudget;
        public readonly AntiMinorFactionWaveData WaveData = new AntiMinorFactionWaveData();

        public readonly ArcenLessLinkedList<Fireteam> Teams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "MarauderFactionBaseInfo-Teams" );

        //Not Serialized
        public static List<MarauderFactionBaseInfo> AllMarauderFactions = List<MarauderFactionBaseInfo>.Create_WillNeverBeGCed( 8, "MarauderFactionBaseInfo-AllMarauderFactions" );
        public bool HasUpdatedAllegiance; //make sure not to have a timing window for killing command stations after game load
        public DoubleBufferedList<SafeSquadWrapper> Outposts = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "MarauderOutposts" );
        public int Intensity;
        public bool IsInFireteamMode;
        public bool PlayerAllied = false; //while these are in this.Data.Allegiance, the string check is expensive
        public bool aiAllied = false;
        public bool NoMark3Outposts;
        public bool NoMark3OutpostsOnAlliedPlanets;

        public readonly DoubleBufferedDictionary<int, int> RaidersPerOutpost = DoubleBufferedDictionary<int, int>.Create_WillNeverBeGCed( 300, "RaidersPerOutpost" ); //used by description appender
        public int TotalRaiders;
        public int MaxRaidersPerMark3Outpost;
        public readonly DoubleBufferedDictionary<Planet, bool> planetsWithMarauderOutposts = DoubleBufferedDictionary<Planet, bool>.Create_WillNeverBeGCed( 300, "planetsWithMarauderOutposts" );
        public DoubleBufferedValue<int> numMark3Outposts = new DoubleBufferedValue<int>( 0 );
        public bool MaraudersAreSuppressed = false;

        //constants
        public readonly FInt minBudgetForAttack = FInt.FromParts( 300, 000 );
        public readonly FInt OverkillRaidersNonFireteam = FInt.FromParts( 0, 650 ); //raiders must be more powerful for the defenses to attack
        public readonly FInt OverkillRaidersFireteam = FInt.FromParts( 0, 850 ); //raiders must be more powerful for the defenses to attack, but not much

        public MarauderFactionBaseInfo()
        {
            Cleanup();
        }

        #region Cleanup
        protected override void Cleanup()
        {
            this.Budget = FInt.FromParts( -1, 000 ); //current budget for the Marauders
            this.AIOnlyBudget = FInt.Zero;
            this.TotalPlanetsCaptured = 0;
            this.MarauderSpecificAIP = FInt.Zero;
            this.NoMark3Outposts = false;
            this.NoMark3OutpostsOnAlliedPlanets = true;
            this.IsInFireteamMode = false;
            this.HasUpdatedAllegiance = false;
            this.Intensity = 0;
            this.TotalPlanetsCaptured = 0;

            PlayerAllied = false;
            aiAllied = false;

            IneligiblePlanets.Clear();
            Teams.Clear();
            Outposts.Clear();

            RaidersPerOutpost.Clear();
            TotalRaiders = 0;
            MaxRaidersPerMark3Outpost = 0;
            planetsWithMarauderOutposts.Clear();
            numMark3Outposts.Clear();
            MaraudersAreSuppressed = false;

            AllMarauderFactions.Clear();
            HaveLoadedData = false; //trigger a reload of xml

            WaveData.Cleanup();
        }
        #endregion

        #region Ser / Deser
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "MarauderFactionBaseInfo" );
            Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, this.IneligiblePlanets.Count );
            foreach ( KeyValuePair<Planet, int> pair in this.IneligiblePlanets )
            {
                Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, pair.Key.Index );
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, pair.Value );
            }
            Buffer.AddFInt( MetaData, this.Budget );
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, TotalPlanetsCaptured );
            Buffer.AddFInt( MetaData, MarauderSpecificAIP );
            Buffer.AddFInt( MetaData, AIOnlyBudget );
            FireteamBaseUtility.SerializeFireteams( MetaData, Buffer, SerializationCmdType, Teams );
            WaveData.SerializeTo( MetaData, Buffer, SerializationCmdType );
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "MarauderFactionBaseInfo" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "MarauderFactionBaseInfo Ext", TrackerStyle.ByTypeOnly );
            this.IneligiblePlanets.Clear();
            int numLookupElements = Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511 );
            for ( int i = 0; i < numLookupElements; i++ )
            {
                //I break these variables out to make it clear what's happening
                Int16 planetIdx = (Int16)Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511 );
                Planet planet = World_AIW2.Instance.GetPlanetByIndex( planetIdx );
                int numSeconds = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg );
                this.IneligiblePlanets[planet] = numSeconds;
            }
            this.Budget = Buffer.ReadFInt( MetaData );
            if ( Buffer.FromGameVersion.GetLessThan( 5, 017 ) ) {
                Buffer.ReadString_Condensed( MetaData );
            }
            this.TotalPlanetsCaptured = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
            this.MarauderSpecificAIP = Buffer.ReadFInt( MetaData );
            if ( Buffer.FromGameVersion.GetLessThan( 5, 017 ) ) {
                bool AreListsInitialized = Buffer.ReadBool( MetaData );
                if ( AreListsInitialized )
                {
                    int numItems = Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511 );
                    for ( int i = 0; i < numItems; i++ )
                    {
                        Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511 );
                    }
                    numItems = Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511 );
                    for ( int i = 0; i < numItems; i++ )
                    {
                        Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511 );
                    }
                }
            }

            this.AIOnlyBudget = Buffer.ReadFInt( MetaData );
            FireteamBaseUtility.DeserializeFireteamsAndDiscardAnyExtraLeftovers( MetaData, Buffer, SerializationCmdType, Teams, "marauders" );
            WaveData.DeserializeIntoSelf( MetaData, Buffer, SerializationCmdType );
            Buffer.StopTrackerByName( "MarauderFactionBaseInfo Ext" );
        }
        #endregion

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return Intensity;
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            DoRefreshFromFactionSettings();

            int load = 60 + (Intensity * 5);

            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( load ).Add( " Load From Marauders" );
            return load;
        }

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            this.LoadCustomDataIfNeeded();

            if ( !AllMarauderFactions.Contains( this ) )
                AllMarauderFactions.Add( this );
        }
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            ConfigurationForFaction cfg = this.AttachedFaction.Config;
            Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );

            if ( !this.HasUpdatedAllegiance )
                UpdateAllegiance();

            Faction faction = this.AttachedFaction;
            if ( faction.InvasionTime == -1 )
            {
                //initialize the marauder invasion time
                string invasionTime = cfg.GetStringValueForCustomFieldOrDefaultValue( "InvasionTime", true );
                if ( invasionTime == "Immediate" )
                    faction.InvasionTime = 1;
                else if ( invasionTime == "Early Game" )
                    faction.InvasionTime = 1 * (60 * 60); // 1 hr in
                else if ( invasionTime == "Mid Game" )
                    faction.InvasionTime = 2 * (60 * 60); // 2 hr in
                else if ( invasionTime == "Late Game" )
                    faction.InvasionTime = 3 * (60 * 60); // 3 hr in
                if ( faction.InvasionTime > 1 )
                {
                    //plus a bit of randomness
                    //this will be a desync on the client and host, but the host will correct the client in under 5 seconds.
                    if ( Engine_Universal.PermanentQualityRandom.Next( 0, 100 ) < 50 )
                        faction.InvasionTime += Engine_Universal.PermanentQualityRandom.Next( 0, faction.InvasionTime / 10 );
                    else
                        faction.InvasionTime -= Engine_Universal.PermanentQualityRandom.Next( 0, faction.InvasionTime / 10 );
                }
            }


            string intelligence = cfg.GetStringValueForCustomFieldOrDefaultValue( "Intelligence", true );
            this.IsInFireteamMode = intelligence != "Brute Force";

            if ( !this.IsInFireteamMode )
            {
                if ( GameSettings.Current.GetBoolBySetting( "EnableFireteamMarauders" ) )
                    //if the expansion is installed but not enabled in the campaign, then this should STILL not be on.  So this is a good check to leave long-term
                {
                    this.IsInFireteamMode = true;
                }
            }

            if ( AttachedFaction.MinFireteamStrength == -1 )
                AttachedFaction.MinFireteamStrength = 2000;
            if ( AttachedFaction.MaxFireteamStrength == -1 )
                AttachedFaction.MaxFireteamStrength = 4000;
        }
        #endregion

        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            /* This is incremented here, for however many macrophage factions there are,
             * so that we can do some more efficient checking of "are there any macrophage factions at all"
             * in some other code.
             * 
             * This is set to 0 and placed on the SpecialFaction_Human class because of some logic constraints
             * that are explained in more detail on DoPerSecondLogic_Stage1Clearing_OnMainThreadAndPartOfSim of SpecialFaction_Human.
             */
            int debugCode = 0;
            try
            {
                debugCode = 100;
                TotalRaiders = 0;
                debugCode = 150;
                this.Outposts.ClearConstructionListForStartingConstruction();
                this.planetsWithMarauderOutposts.ClearConstructionDictForStartingConstruction();
                this.RaidersPerOutpost.ClearConstructionDictForStartingConstruction();
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads() )
                {
                    debugCode = 200;
                    entity.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                    if ( entity.TypeData.GetHasTag( "MarauderBeacon" ) )
                        MaraudersAreSuppressed = true; //for when the player wants to discover marauders

                    if ( entity.TypeData.GetHasTag( "MarauderOutpost" ) || entity.TypeData.GetHasTag( "WarpingInMarauderOutpost" ) )
                    {
                        MarauderOutpostRaiderPerUnitBaseInfo data = entity.CreateExternalBaseInfo<MarauderOutpostRaiderPerUnitBaseInfo>( "MarauderOutpostRaiderPerUnitBaseInfo" ); //just make sure this external data exists
                        debugCode = 220;
                        Outposts.AddToConstructionList( entity );
                        planetsWithMarauderOutposts.Construction[entity.Planet] = true;
                    }
                    if ( entity.TypeData.GetHasTag( "MarauderRaider" ) )
                    {
                        debugCode = 230;
                        MarauderOutpostRaiderPerUnitBaseInfo data = entity.CreateExternalBaseInfo<MarauderOutpostRaiderPerUnitBaseInfo>( "MarauderOutpostRaiderPerUnitBaseInfo" );

                        //I'm going to track the spawning outpost on the Entity instead of in External Data so I can share logic
                        //in the StackPlanning between all the minor factions that track which entity spawned a unit (ie this and Nanocaust)
                        //and only allow units that share a spawner to stack together
                        if ( entity.MinorFactionStackingID == -1 )
                            entity.MinorFactionStackingID = data.OutpostId;
                        RaidersPerOutpost.Construction[data.OutpostId]++;
                        RaidersPerOutpost.Construction[data.OutpostId] += entity.ExtraStackedSquadsInThis; //this is inexact because raiders from different outposts can be stacked , but it's an improvement at least
                        TotalRaiders += 1 + entity.ExtraStackedSquadsInThis;
                    }
                }

                if ( World_AIW2.Instance.GameSecond < this.AttachedFaction.InvasionTime )
                    MaraudersAreSuppressed = true;
                else
                    MaraudersAreSuppressed = false;

                this.Outposts.SwitchConstructionToDisplay();
                this.planetsWithMarauderOutposts.SwitchConstructionToDisplay();
                this.RaidersPerOutpost.SwitchConstructionToDisplay();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in marauders stage 2, code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        #endregion end DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost

        #region SetStartingFactionRelationships
        public override void SetStartingFactionRelationships()
        {
            //Note that this will be overridden in the PerSimStep code
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
                    case FactionType.AI:
                    case FactionType.SpecialFaction:
                        faction.MakeHostileTo( otherFaction );
                        otherFaction.MakeHostileTo( faction );
                        if ( otherFaction.SpecialFactionData != null && otherFaction.SpecialFactionData.InternalName == "ZenithTrader" )
                        {
                            faction.MakeFriendlyTo( otherFaction );
                            otherFaction.MakeFriendlyTo( faction );
                        }
                        break;
                }
            }
        }
        #endregion

        public const string MARAUDERS_TAG = "HumanMarauders";

        #region Custom Xml Data
        private bool HaveLoadedData;
        public int IneligibleSeconds = 0;
        public FInt FriendlyMinStrength;
        public FInt EnemyMinStrength;
        public FInt RatioForNeutralPlanet;
        public FInt RatioForEnemyPlanet;
        public FInt RatioForFriendlyPlanet;
        public FInt Braveness_Constant;
        public byte ChanceToAttack;
        public FInt BudgetPerSecondLowIntensity;
        public FInt BudgetPerSecondMediumIntensity;
        public FInt BudgetPerSecondHighIntensity;
        public FInt StartingBudget;
        public FInt BudgetIncreaseForPlanetCapture;
        public FInt BonusBudgetPerMarkIIIOutpost;
        public FInt BaseMaxBudget;
        public int timeForMarkIIUpgrade;
        public int timeForMarkIIIUpgrade;
        public int outpostSpawnInterval;
        public int MaxOutpostsPerPlanet;
        public int MaxOutpostsPerAlliedPlanet;
        public FInt BudgetMultiplierIntensity1;
        public FInt BudgetMultiplierIntensity2;
        public FInt BudgetMultiplierIntensity3;
        public FInt BudgetMultiplierIntensity4;
        public FInt BudgetMultiplierIntensity5;
        public FInt BudgetMultiplierIntensity6;
        public FInt BudgetMultiplierIntensity7;
        public FInt BudgetMultiplierIntensity8;
        public FInt BudgetMultiplierIntensity9;
        public FInt BudgetMultiplierIntensity10;
        public int BaseRaiderSpawnInterval;
        public int BaseRaiderSpawnIntervalPlayerAllied;
        public int TurretsPerMark1Outpost;
        public int TurretsPerMark2Outpost;
        public int TurretsPerMark3Outpost;
        public int WaveInterval;
        public FInt BaseWaveBudgetPerMinute;
        public FInt MinWaveSize;
        public int MaxRaidersMark3OutpostLowIntensity;
        public int MaxRaidersMark3OutpostMedIntensity;
        public int MaxRaidersMark3OutpostHighIntensity;
        public int MaxRaidersMark3OutpostLowIntensityPlayerAllied;
        public int MaxRaidersMark3OutpostMedIntensityPlayerAllied;
        public int MaxRaidersMark3OutpostHighIntensityPlayerAllied;

        private void LoadCustomDataIfNeeded()
        {
            if ( this.HaveLoadedData )
                return;
            this.HaveLoadedData = true;
            this.IneligibleSeconds = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_HumanMarauders_IneligibleSeconds" );
            this.FriendlyMinStrength = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_FriendlyMinStrength" );
            this.EnemyMinStrength = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_EnemyMinStrength" );
            this.RatioForNeutralPlanet = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_RatioForNeutralPlanet" );
            this.RatioForEnemyPlanet = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_RatioForEnemyPlanet" );
            this.RatioForFriendlyPlanet = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_RatioForFriendlyPlanet" );
            this.Braveness_Constant = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_BravenessConstant" );
            this.ChanceToAttack = ExternalConstants.Instance.GetCustomByte_Slow( "custom_int_HumanMarauders_ChanceToAttack" );
            this.BudgetPerSecondLowIntensity = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_BudgetPerSecondLow" );
            this.BudgetPerSecondMediumIntensity = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_BudgetPerSecondMedium" );
            this.BudgetPerSecondHighIntensity = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_BudgetPerSecondHigh" );
            this.StartingBudget = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_StartingBudget" ); //the starting budget is mostly for testing
            this.BudgetIncreaseForPlanetCapture = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_BudgetIncreaseForPlanetCapture" );
            this.BudgetIncreaseForPlanetCapture = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_BudgetIncreaseForPlanetCapture" );
            this.BonusBudgetPerMarkIIIOutpost = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_BonusBudgetPerMarkIIIOutpost" );
            this.BaseMaxBudget = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_BaseMaxBudget" );
            this.WaveInterval = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_HumanMarauders_WaveIntervalInMinutes" );
            this.BaseWaveBudgetPerMinute = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_BaseWaveBudgetPerMinute" );
            this.MinWaveSize = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_MinWaveSize" );
            this.MaxRaidersMark3OutpostLowIntensity = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_HumanMarauders_MaxRaidersMark3OutpostLowIntensity" );
            this.MaxRaidersMark3OutpostMedIntensity = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_HumanMarauders_MaxRaidersMark3OutpostMedIntensity" );
            this.MaxRaidersMark3OutpostHighIntensity = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_HumanMarauders_MaxRaidersMark3OutpostHighIntensity" );
            this.MaxRaidersMark3OutpostLowIntensityPlayerAllied = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_HumanMarauders_MaxRaidersMark3OutpostLowIntensityPlayerAllied" );
            this.MaxRaidersMark3OutpostMedIntensityPlayerAllied = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_HumanMarauders_MaxRaidersMark3OutpostMedIntensityPlayerAllied" );
            this.MaxRaidersMark3OutpostHighIntensityPlayerAllied = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_HumanMarauders_MaxRaidersMark3OutpostHighIntensityPlayerAllied" );

            this.TurretsPerMark1Outpost = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_HumanMarauders_TurretsPerMark1Outpost" );
            this.TurretsPerMark2Outpost = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_HumanMarauders_TurretsPerMark2Outpost" );
            this.TurretsPerMark3Outpost = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_HumanMarauders_TurretsPerMark3Outpost" );

            this.timeForMarkIIUpgrade = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_HumanMarauders_timeForMarkIIUpgrade" );
            this.timeForMarkIIIUpgrade = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_HumanMarauders_timeForMarkIIIUpgrade" );
            this.outpostSpawnInterval = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_HumanMarauders_outpostSpawnInterval" );
            this.MaxOutpostsPerPlanet = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_HumanMarauders_MaxOutpostsPerPlanet" );
            this.MaxOutpostsPerAlliedPlanet = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_HumanMarauders_MaxOutpostsPerAlliedPlanet" );
            this.BudgetMultiplierIntensity1 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_BudgetMultiplierIntensity1" );
            this.BudgetMultiplierIntensity2 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_BudgetMultiplierIntensity2" );
            this.BudgetMultiplierIntensity3 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_BudgetMultiplierIntensity3" );
            this.BudgetMultiplierIntensity4 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_BudgetMultiplierIntensity4" );
            this.BudgetMultiplierIntensity5 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_BudgetMultiplierIntensity5" );
            this.BudgetMultiplierIntensity6 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_BudgetMultiplierIntensity6" );
            this.BudgetMultiplierIntensity7 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_BudgetMultiplierIntensity7" );
            this.BudgetMultiplierIntensity8 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_BudgetMultiplierIntensity8" );
            this.BudgetMultiplierIntensity9 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_BudgetMultiplierIntensity9" );
            this.BudgetMultiplierIntensity10 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_HumanMarauders_BudgetMultiplierIntensity10" );
            this.BaseRaiderSpawnIntervalPlayerAllied = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_HumanMarauders_BaseRaiderSpawnIntervalPlayerAllied" );
            this.BaseRaiderSpawnInterval = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_HumanMarauders_BaseRaiderSpawnInterval" );
        }
        #endregion

        #region GetBudgetMutliplier
        public FInt GetBudgetMutliplier()
        {
            FInt multiplier;
            switch ( this.Intensity )
            {
                case 1:
                    multiplier = this.BudgetMultiplierIntensity1;
                    break;
                case 2:
                    multiplier = this.BudgetMultiplierIntensity2;
                    break;
                case 3:
                    multiplier = this.BudgetMultiplierIntensity3;
                    break;
                case 4:
                    multiplier = this.BudgetMultiplierIntensity4;
                    break;
                case 5:
                    multiplier = this.BudgetMultiplierIntensity5;
                    break;
                case 6:
                    multiplier = this.BudgetMultiplierIntensity6;
                    break;
                case 7:
                    multiplier = this.BudgetMultiplierIntensity7;
                    break;
                case 8:
                    multiplier = this.BudgetMultiplierIntensity8;
                    break;
                case 9:
                    multiplier = this.BudgetMultiplierIntensity9;
                    break;
                case 10:
                    multiplier = this.BudgetMultiplierIntensity10;
                    break;
                default:
                    throw new Exception( "Unexpected Human Marauders intensity " + this.Intensity );
            }
            return multiplier;
        }
        #endregion

        #region UpdateAllegiance
        private void UpdateAllegiance()
        {
            bool localDebug = false;
            string allegiance = this.Allegiance;
            if ( ArcenStrings.Equals( allegiance, "Hostile To All" ) || string.IsNullOrEmpty( allegiance ) )
            {
                this.PlayerAllied = false;
                this.aiAllied = false;
                if ( string.IsNullOrEmpty( allegiance ) ) //just fix it to be hostile to all
                    this.SetNewAllegianceIntoCoreSettings( "Hostile To All" );
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "This Marauder faction should be hostile to all (default)", Verbosity.DoNotShow );
                //make sure this isn't set wrong somehow
                AllegianceHelper.EnemyThisFactionToAll( this.AttachedFaction );
            }
            else if ( ArcenStrings.Equals( allegiance, "Hostile To Players Only" ) ||
                    ArcenStrings.Equals( allegiance, "HostileToPlayers" ) )
            {
                this.aiAllied = true;
                AllegianceHelper.AllyThisFactionToAI( this.AttachedFaction );
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "This Marauder faction should be friendly to the AI and hostile to players", Verbosity.DoNotShow );

            }
            else if ( ArcenStrings.Equals( allegiance, "HostileToAI" ) ||
                    ArcenStrings.Equals( allegiance, "Friendly To Players" ) )
            {
                this.PlayerAllied = true;
                AllegianceHelper.AllyThisFactionToHumans( this.AttachedFaction );
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "This Marauder faction should be hostile to the AI and friendly to players", Verbosity.DoNotShow );

            }
            else if ( ArcenStrings.Equals( allegiance, "Minor Faction Team Red" ) )
            {
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( this.AttachedFaction.GetDisplayName() + " is on team red", Verbosity.DoNotShow );
                AllegianceHelper.AllyThisFactionToMinorFactionTeam( this.AttachedFaction, "Minor Faction Team Red" );
            }
            else if ( ArcenStrings.Equals( allegiance, "Minor Faction Team Blue" ) )
            {
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( this.AttachedFaction.GetDisplayName() + " is on team blue", Verbosity.DoNotShow );

                AllegianceHelper.AllyThisFactionToMinorFactionTeam( this.AttachedFaction, "Minor Faction Team Blue" );
            }
            else if ( ArcenStrings.Equals( allegiance, "Minor Faction Team Green" ) )
            {
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( this.AttachedFaction.GetDisplayName() + " is on team green", Verbosity.DoNotShow );

                AllegianceHelper.AllyThisFactionToMinorFactionTeam( this.AttachedFaction, "Minor Faction Team Green" );
            }

            else
            {
                throw new Exception( "unknown Marauder allegiance '" + allegiance + "'" );
            }
            HasUpdatedAllegiance = true;
        }
        #endregion

        #region UpdatePowerLevel
        public override void UpdatePowerLevel()
        {
            FInt newResult = FInt.Zero;
            if ( TotalRaiders > 1 )
                newResult = FInt.FromParts( 0, 100 );
            if ( Outposts.Count > 60 )
                newResult += FInt.FromParts( 0, 250 );
            if ( Outposts.Count > 1000 )
                newResult += FInt.FromParts( 0, 250 );

            if ( TotalRaiders > 200 )
                newResult += FInt.One;

            this.AttachedFaction.OverallPowerLevel = newResult;
        }
        #endregion

        #region GetMarauderStateForDisplay
        public void GetMarauderStateForDisplay( ArcenDoubleCharacterBuffer output )
        {
            if ( this.AttachedFaction.InvasionTime > 0 && this.AttachedFaction.InvasionTime > World_AIW2.Instance.GameSecond )
            {
                output.Add( "The marauders will invade in " + (this.AttachedFaction.InvasionTime - World_AIW2.Instance.GameSecond) + " seconds.\n" );
                return;
            }

            if ( !this.IsInFireteamMode )
            {
                output.Add( "Not in fireteam mode\n" );
                return;
            }
            output.Add( "\nState of " + this.Teams.GetItemCount() + " Marauder Fireteams for  <" + this.Allegiance + ">:\n" );
            output.Add( "Marauder AIP: <color=#ffaaaa>" + this.MarauderSpecificAIP + "</color>, and current anti-me wave strength is " + this.WaveData.currentWaveBudget + " \n" );
            output.Add( "Budget for invasions: " + this.Budget + "\n" );
            output.Add( "There are " + this.Outposts.Count + " outposts.\n" );
            if ( this.Teams.GetItemCount() == 0 )
            {
                output.Add( "No fireteams\n" );
                return;
            }

            int totalStrength = 0;
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( this.Teams ) )
            {
                totalStrength += team.DeepInfo.TeamStrength;
                if ( team.status != FireteamStatus.Disbanded )
                {
                    team.DeepInfo.GetStringForDisplay( output );
                    output.Add( "\n" );
                }
            }
            output.Add( "Total Raider Strength: <color=#ff0000>" + (totalStrength / 1000) + "</color>.\n" );
        }
        #endregion

        #region GetFireteamById
        public override Fireteam GetFireteamById( int id )
        {
            return FireteamBaseUtility.GetFireteamById( this.Teams, id );
        }
        #endregion

        #region GetShouldAttackNormallyExcludedTarget
        public override bool GetShouldAttackNormallyExcludedTarget( GameEntity_Squad Target )
        {
            try
            {
                Faction targetControllingFaction = Target.GetFactionOrNull_Safe();

                if ( !PlayerAllied || AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "MaraudersKillCommandStations" ) )
                {
                    //Human Allied factions will optionally leave an AI command station intact, so as to not drive AIP up so high
                    if ( Target.TypeData.IsCommandStation )
                    {
                        return true;
                    }
                }
                if ( Target.TypeData.GetHasTag( "NormalPlanetNastyPick" ) || Target.TypeData.GetHasTag( "DSAA" ) )
                    return true;

                return false;
            }
            catch ( ArcenPleaseStopThisThreadException )
            {
                //this one is ok -- just means the thread is ending for some reason.  I guess we'll skip trying the normal handling here
                return false;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "HumanMarauders GetShouldAttackNormallyExcludedTarget error:" + e, Verbosity.ShowAsError );
                return false;
            }
        }
        #endregion

        #region DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            UpdateAllegiance();

            if ( this.PlayerAllied )
            {
                this.MaxRaidersPerMark3Outpost = this.MaxRaidersMark3OutpostLowIntensityPlayerAllied;
                if ( this.Intensity > 3 && this.Intensity <= 6 )
                    this.MaxRaidersPerMark3Outpost = this.MaxRaidersMark3OutpostMedIntensityPlayerAllied;
                if ( this.Intensity > 7 )
                    this.MaxRaidersPerMark3Outpost = this.MaxRaidersMark3OutpostHighIntensityPlayerAllied;
            }
            else
            {
                this.MaxRaidersPerMark3Outpost = this.MaxRaidersMark3OutpostLowIntensity;
                if ( this.Intensity > 3 && this.Intensity <= 6 )
                    this.MaxRaidersPerMark3Outpost = this.MaxRaidersMark3OutpostMedIntensity;
                if ( this.Intensity > 7 )
                    this.MaxRaidersPerMark3Outpost = this.MaxRaidersMark3OutpostHighIntensity;
            }
        }
        #endregion

        public AntiMinorFactionWaveData GetAntiMinorFactionWaveData()
        {
            return this.WaveData;
        }
    }

    public struct MarauderOutpostData
    {
        //this is used to track whether a new outpost should be created
        public int numOutposts;
        public int timeYoungestMark1HasExisted;
        public bool HasWarpingInOutposts;
        public MarauderOutpostData( int outposts, int time )
        {
            numOutposts = outposts;
            timeYoungestMark1HasExisted = time;
            HasWarpingInOutposts = false;
        }
    }
}
