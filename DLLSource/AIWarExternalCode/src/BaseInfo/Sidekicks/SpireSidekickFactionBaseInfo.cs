using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class SpireSidekickFactionBaseInfo : ExternalFactionBaseInfoRoot, IExoDataHolder, IExternalBaseInfo_Singleton
    {
        //serialized
        public int TimeForNextRelicSpawn;
        public int TimeForNextSpireRelicTrain;
        public Int16 CurrentRelicSpawnPlanetIdx;
        public bool CurrentRelicInSearchMode;
        public readonly List<Int16> PlanetsSearchedForCurrentRelic = List<short>.Create_WillNeverBeGCed( 300, "SpireSidekickFactionBaseInfo-PlanetsSearchedForCurrentRelic" );
        public Int16 NumRelicsCaptured;
        public bool WarpRelicModeEnabled;
        public bool RelicOnMap;
        public Int16 NumExosSent;
        public readonly List<int> TimesForNextSpireDebris = List<int>.Create_WillNeverBeGCed( 300, "SpireSidekickFactionBaseInfo-TimesForNextSpireDebris" );
        public readonly List<int> IncomeForAIShips = List<int>.Create_WillNeverBeGCed( 300, "SpireSidekickFactionBaseInfo-IncomeForAIShips" );
        public int TimeUntilImperialFleetArrives;
        public int TimeImperialFleetSummoned;
        public bool ImperialFleetActive;
        public readonly ArcenLessLinkedList<Fireteam> Teams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "SpireSidekickFactionBaseInfo-Teams" );
        public readonly ExoData exoData = new ExoData();

        // Not Serialized
        public static SpireSidekickFactionBaseInfo Instance; //there can only ever be one fallen spire at a time
        public int Intensity;
        public SpireSidekickDifficulty Difficulty;
        public bool ExpertMode;
        public int CityMarkLevelForJournal;

        //Spire Sidekick specific stuff about rangers
        public readonly DoubleBufferedValue<int> RangerCount = new DoubleBufferedValue<int>( 0 );
        public readonly DoubleBufferedValue<int> DireRangerCount = new DoubleBufferedValue<int>( 0 );

        public readonly DoubleBufferedList<SafeSquadWrapper> Rangers = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "SpireSidekick-Rangers" );
        public readonly DoubleBufferedList<SafeSquadWrapper> RangerProducers = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "SpireSidekick-RangerProducers" );
        public readonly DoubleBufferedList<SafeSquadWrapper> RangerOutposts = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "SpireSidekick-RangerOutposts" );
        public readonly DoubleBufferedList<SafeSquadWrapper> DireRangerOutposts = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "SpireSidekick-DireRangerOutposts" );
        public readonly DoubleBufferedDictionary<SafeSquadWrapper, int> RangersPerCity = DoubleBufferedDictionary<SafeSquadWrapper, int>.Create_WillNeverBeGCed( 20, "SpireSidekickFactionBaseInfo-RangersPerCity" );
        public readonly DoubleBufferedDictionary<SafeSquadWrapper, int> DireRangersPerCity = DoubleBufferedDictionary<SafeSquadWrapper, int>.Create_WillNeverBeGCed( 20, "SpireSidekickFactionBaseInfo-DireRangersPerCity" );
        public readonly DoubleBufferedDictionary<Planet, int> RangersPerPlanet = DoubleBufferedDictionary<Planet, int>.Create_WillNeverBeGCed( 500, "SpireSidekickFactionBaseInfo-RangersPerPlanet" );

        
        public readonly DoubleBufferedList<Faction> DarkSpireFactions = DoubleBufferedList<Faction>.Create_WillNeverBeGCed( 20, "SpireSidekick-DarkSpireFactions" );
        public readonly DoubleBufferedList<SafeSquadWrapper> ActiveRelics = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "SpireSidekick-ActiveRelics" );
        public readonly DoubleBufferedList<SafeSquadWrapper> SortedSpireCities = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "SpireSidekick-SortedSpireCities" );
        public readonly DoubleBufferedList<SafeSquadWrapper> AllSpireCities = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "SpireSidekick-AllSpireCities" );
        public readonly DoubleBufferedList<SafeSquadWrapper> SpirePlayerFleets = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "SpireSidekick-SpirePlayerFleets" );
        public readonly DoubleBufferedValue<SafeSquadWrapper> Transceiver = new DoubleBufferedValue<SafeSquadWrapper>( SafeSquadWrapper.Create( null ) );

        //these both get updated during the process of sim steps, so had to be converted to DoubleBufferedConcurrentLists.  This is less performant
        //than DoubleBufferedList, but won't have cross-threading issues from an activity like that
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> SpireCities = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "SpireSidekick-SpireCities" );
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> SpireDebris = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 40, "SpireSidekick-SpireDebris" );

        #region Custom Data
        private bool HaveLoadedData = false;

        //XML values to load in
        public static int MinHopsBetweenCities;
        public int DebrisSpawnDelay;
        public int DebrisSpawnDelayRandomness;

        public int AIPIncreaseForLostRelic;

        public int NumRelicsRequiredForTrains;
        public int TrainInterval;
        public int TrainIntervalRandomness;
        public int TrainIntervalFirstTrain;
        public int FirstRelicSpawnTime = 0;

        public int RelicSpawnInterval = 0;
        public int RelicSpawnIntervalRandomness = 0;

        private void LoadCustomDataIfNeeded()
        {
            if ( this.HaveLoadedData )
                return;
            this.HaveLoadedData = true;
            FirstRelicSpawnTime = 30; //start this early
            RelicSpawnInterval = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_FallenSpire_RelicSpawnInterval" );
            RelicSpawnIntervalRandomness = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_FallenSpire_RelicSpawnIntervalRandomness" );
            MinHopsBetweenCities = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_FallenSpire_MinHopsBetweenCities" );

            NumRelicsRequiredForTrains = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_FallenSpire_NumRelicsRequiredForTrains" );
            TrainInterval = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_FallenSpire_TrainInterval" );
            TrainIntervalRandomness = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_FallenSpire_TrainIntervalRandomness" );
            TrainIntervalFirstTrain = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_FallenSpire_TrainIntervalFirstTrain" );

            DebrisSpawnDelay = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_FallenSpire_DebrisSpawnDelay" );
            DebrisSpawnDelayRandomness = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_FallenSpire_DebrisSpawnDelayRandomness" );


            if ( FirstRelicSpawnTime <= 0 )
                throw new Exception( "Bug in reading fallenspire external constants." );

            if ( AIPIncreaseForLostRelic != 0 )
                throw new Exception( "AIP increase for lost relic doesn't do anything right now. Unsure if it's even desirable; might be better to give the AI a new ship" );
            // for ( int i = 0; i < ExoIncomePer10AIPPerIntensity.Count; i++ )
            //     ArcenDebugging.ArcenDebugLogSingleLine(i + ": " + ExoIncomePer10AIPPerIntensity[i], Verbosity.DoNotShow );
            // ArcenDebugging.ArcenDebugLogSingleLine("exo income per 10 AIP " + ExoIncomePer10AIP, Verbosity.DoNotShow );

            //sync with CPA, just to be mean
        }
        #endregion

        public SpireSidekickFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            TimeForNextRelicSpawn = -1;
            TimeForNextSpireRelicTrain = -1;
            CurrentRelicSpawnPlanetIdx = -1;
            NumRelicsCaptured = 0;
            this.CurrentRelicInSearchMode = false;
            PlanetsSearchedForCurrentRelic.Clear();
            WarpRelicModeEnabled = false;
            RelicOnMap = false;
            NumExosSent = 0;
            TimesForNextSpireDebris.Clear();
            IncomeForAIShips.Clear();
            TimeUntilImperialFleetArrives = -1;
            ImperialFleetActive = false;
            TimeImperialFleetSummoned = -1;
            CityMarkLevelForJournal = -1;

            Intensity = 0;
            Difficulty = null;
            ExpertMode = false;

            DarkSpireFactions.Clear();
            ActiveRelics.Clear();
            SpireCities.Clear();
            SortedSpireCities.Clear();
            AllSpireCities.Clear();
            SpirePlayerFleets.Clear();
            SpireDebris.Clear();
            Transceiver.Clear();

            Rangers.Clear();
            RangerProducers.Clear();
            RangerOutposts.Clear();
            DireRangerOutposts.Clear();
            RangerCount.Clear();
            DireRangerCount.Clear();
            RangersPerCity.Clear();
            DireRangersPerCity.Clear();
            RangersPerPlanet.Clear();

            exoData.Cleanup();

            Instance = this;

            HaveLoadedData = false; //trigger a reload of xml
        }

        #region Serialization and Deserialization
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "SpireSidekickFactionBaseInfo" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeForNextRelicSpawn );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeForNextSpireRelicTrain );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, this.CurrentRelicSpawnPlanetIdx );
            Buffer.AddBool( MetaData, this.CurrentRelicInSearchMode );
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, this.NumRelicsCaptured );
            Buffer.AddBool( MetaData, this.WarpRelicModeEnabled );
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.PlanetsSearchedForCurrentRelic.Count );
            for ( int i = 0; i < this.PlanetsSearchedForCurrentRelic.Count; i++ )
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, this.PlanetsSearchedForCurrentRelic[i] );
            Buffer.AddBool( MetaData, this.RelicOnMap );
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, this.NumExosSent );
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.TimesForNextSpireDebris.Count );
            for ( int i = 0; i < this.TimesForNextSpireDebris.Count; i++ )
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.TimesForNextSpireDebris[i] );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.IncomeForAIShips.Count );
            for ( int i = 0; i < this.IncomeForAIShips.Count; i++ )
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.IncomeForAIShips[i] );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeUntilImperialFleetArrives );
            Buffer.AddBool( MetaData, ImperialFleetActive );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, TimeImperialFleetSummoned );

            exoData.SerializeTo( MetaData, Buffer, SerializationCmdType );
            FireteamBaseUtility.SerializeFireteams( MetaData, Buffer, SerializationCmdType, this.Teams );
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "SpireSidekickFactionBaseInfo" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "SpireSidekickFactionBaseInfo Ext", TrackerStyle.ByTypeOnly );
            this.TimeForNextRelicSpawn = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            this.TimeForNextSpireRelicTrain = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            this.CurrentRelicSpawnPlanetIdx = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1 );
            this.CurrentRelicInSearchMode = Buffer.ReadBool( MetaData );
            this.NumRelicsCaptured = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
            this.WarpRelicModeEnabled = Buffer.ReadBool( MetaData );
            this.PlanetsSearchedForCurrentRelic.Clear();
            int numEntries = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
            for ( Int16 i = 0; i < numEntries; i++ )
                this.PlanetsSearchedForCurrentRelic.Add( Buffer.ReadInt16( MetaData, ReadStyle.NonNeg ) );

            this.RelicOnMap = Buffer.ReadBool( MetaData );
            this.NumExosSent = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
            this.TimesForNextSpireDebris.Clear();
            numEntries = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
            for ( int i = 0; i < numEntries; i++ )
                this.TimesForNextSpireDebris.Add( Buffer.ReadInt32( MetaData, ReadStyle.NonNeg ) );

            this.IncomeForAIShips.Clear();
            numEntries = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg );
            for ( int i = 0; i < numEntries; i++ )
                this.IncomeForAIShips.Add( Buffer.ReadInt32( MetaData, ReadStyle.NonNeg ) );

            TimeUntilImperialFleetArrives = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            ImperialFleetActive = Buffer.ReadBool( MetaData );
            TimeImperialFleetSummoned = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );

            exoData.DeserializeIntoSelf( MetaData, Buffer, SerializationCmdType );
            FireteamBaseUtility.DeserializeFireteamsAndDiscardAnyExtraLeftovers( MetaData, Buffer, SerializationCmdType, this.Teams, "fallen spire" );
            Buffer.StopTrackerByName( "SpireSidekickFactionBaseInfo Ext" );
        }
        #endregion end Serialization and Deserialization

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
            this.LoadCustomDataIfNeeded();

            this.DoInitializationIfNecessary();
        }
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            ConfigurationForFaction cfg = this.AttachedFaction.Config;
            if ( this.AttachedFaction.Type != FactionType.Player )
                Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
            else
            {
                PlayerTypeData playerType = this.AttachedFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
                if ( playerType.InternalName == "SpireInfusedEmpire" )
                    Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "SpireEmpireIntensity", true );
                else
                {
                    Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "SpireSidekickIntensity", false );
                    if ( Intensity == -1 )
                        Intensity = 5;
                }

            }
            Difficulty = SpireSidekickDifficultyTable.Instance.GetRowByIntensity( Intensity );
            if ( Difficulty == null )
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not find fallen spire difficuly for intensity " + Intensity, Verbosity.ShowAsError );

            ExpertMode = cfg.GetBoolValueForCustomFieldOrDefaultValue( "ExpertMode", false );

            if ( this.AttachedFaction.Type != FactionType.Player )
            {
                //this is for the regular Fallen Spire, not the spire infused empire
                if ( String.IsNullOrEmpty( this.Allegiance ) )
                    this.SetNewAllegianceIntoCoreSettings( "对玩家友好" );
            }
        }
        #endregion

        #region DoInitializationIfNecessary
        private void DoInitializationIfNecessary()
        {
            if ( IncomeForAIShips.Count == 0 )
            {
                for ( int i = 0; i < 5; i++ )
                    IncomeForAIShips.Add( 0 );
            }
        }
        #endregion

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return Intensity;
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( "70 Load From Fallen Spire Faction" );
            return 70;
        }

        public override void SetStartingFactionRelationships()
        {
            base.SetStartingFactionRelationships();
            AllegianceHelper.AllyThisFactionToHumans( AttachedFaction );
        }

        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            DarkSpireFactions.ClearConstructionListForStartingConstruction();
            ActiveRelics.ClearConstructionListForStartingConstruction();
            SpireDebris.ClearConstructionListForStartingConstruction();
            Transceiver.ClearConstructionValueForStartingConstruction();
            SpireCities.ClearConstructionListForStartingConstruction();
            SortedSpireCities.ClearConstructionListForStartingConstruction();
            AllSpireCities.ClearConstructionListForStartingConstruction();
            SpirePlayerFleets.ClearConstructionListForStartingConstruction();

            Rangers.ClearConstructionListForStartingConstruction();
            RangerProducers.ClearConstructionListForStartingConstruction();
            RangerOutposts.ClearConstructionListForStartingConstruction();
            DireRangerOutposts.ClearConstructionListForStartingConstruction();
            RangerCount.ClearConstructionValueForStartingConstruction();
            DireRangerCount.ClearConstructionValueForStartingConstruction();
            RangersPerCity.ClearConstructionDictForStartingConstruction();
            DireRangersPerCity.ClearConstructionDictForStartingConstruction();
            RangersPerPlanet.ClearConstructionDictForStartingConstruction();

            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.GetFactionByIndex( i );
                if ( otherFaction.SpecialFactionData.InternalName == "DarkSpire" )
                    DarkSpireFactions.AddToConstructionList( otherFaction );
            }

            bool isThreAReliceOnTheMap = false;
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "SpireSidekickRelic" ) )
            {
                ActiveRelics.AddToConstructionList( entity );
                isThreAReliceOnTheMap = true;
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "SpireSidekickDebris" ) )
            {
                SpireDebris.AddToConstructionList( entity );
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "SpireFlagship" ) )
            {
                SpirePlayerFleets.AddToConstructionListIfNotAlreadyIn( entity );
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "SummonsImperialFleet" ) )
            {
                Transceiver.Construction = SafeSquadWrapper.Create( entity );
            }

            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "ProducesRangers" ) )
            {
                RangerProducers.AddToConstructionList( entity );
            }

            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "SpireBaseRanger" ) )
            {
                SpireSidekickPerUnitBaseInfo data = entity.CreateExternalBaseInfo<SpireSidekickPerUnitBaseInfo>( "SpireSidekickPerUnitBaseInfo" );
                if ( data.HomeCitySafe.GetSquad() == null &&
                     data.HomeCity.GetSquad() != null)
                {
                    data.HomeCitySafe = SafeSquadWrapper.Create(data.HomeCity.GetSquad());
                }
                if (data.HomeCitySafe.GetSquad() != null)
                {
                    RangersPerCity.Construction[data.HomeCitySafe]++;
                }
                RangerCount.Construction++;
                RangersPerPlanet.Construction[entity.Planet]++;
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "SpireDireRanger" ) )
            {
                SpireSidekickPerUnitBaseInfo data = entity.CreateExternalBaseInfo<SpireSidekickPerUnitBaseInfo>( "SpireSidekickPerUnitBaseInfo" );
                if ( data.HomeCitySafe.GetSquad() == null &&
                     data.HomeCity.GetSquad() != null)
                {
                    data.HomeCitySafe = SafeSquadWrapper.Create(data.HomeCity.GetSquad());
                }

                if ( data.HomeCitySafe.GetSquad() != null )
                    DireRangersPerCity.Construction[data.HomeCitySafe]++;
                DireRangerCount.Construction++;
                RangersPerPlanet.Construction[entity.Planet]++;
            }

            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "BoostsRangerCap" ) )
            {
                SpireSidekickPerUnitBaseInfo data = entity.CreateExternalBaseInfo<SpireSidekickPerUnitBaseInfo>("SpireSidekickPerUnitBaseInfo");
                if ( entity.SelfBuildingMetalRemaining > 0 )
                    continue;
                RangerOutposts.AddToConstructionList( entity );
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "BoostsDireRangerCap" ) )
            {
                SpireSidekickPerUnitBaseInfo data = entity.CreateExternalBaseInfo<SpireSidekickPerUnitBaseInfo>("SpireSidekickPerUnitBaseInfo");
                if ( entity.SelfBuildingMetalRemaining > 0 )
                    continue;

                DireRangerOutposts.AddToConstructionList( entity );
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "AutoDefenseShip" ) )
            {
                Rangers.AddToConstructionList(entity);
                SpireSidekickPerUnitBaseInfo data = entity.CreateExternalBaseInfo<SpireSidekickPerUnitBaseInfo>( "SpireSidekickPerUnitBaseInfo" );
                GameEntity_Squad HomeCity = data.HomeCity.GetSquad();
                if ( entity.PlanetFaction.Faction != HomeCity?.PlanetFaction.Faction ) {
                    HomeCity = null;
                    data.HomeCity.Clear();
                }
                if ( HomeCity == null ) {
                    entity.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut ); //if we've lost our City, we die
                }
            }

            this.RelicOnMap = isThreAReliceOnTheMap;

            foreach ( Faction playerFaction in World_AIW2.Instance.EmpireStylePlayerFactions )
            {
                foreach ( GameEntity_Squad entity in playerFaction.Squads( "SummonsImperialFleet" ) )
                {
                    Transceiver.Construction = SafeSquadWrapper.Create( entity );
                }
                foreach ( GameEntity_Squad entity in playerFaction.Squads( "SpireSidekickCity" ) )
                {
                    SpireCities.AddToConstructionList( entity );
                    SortedSpireCities.AddToConstructionList( entity );
                }

                foreach ( GameEntity_Squad entity in playerFaction.Squads( "SpireFlagship" ) )
                {
                    SpirePlayerFleets.AddToConstructionListIfNotAlreadyIn( entity );
                }
            }
            foreach ( Faction playerFaction in World_AIW2.Instance.SidekickPlayerFactions )
            {
                //To support the spire sidekick
                foreach ( GameEntity_Squad entity in playerFaction.Squads( "SummonsImperialFleet" ) )
                {
                    Transceiver.Construction = SafeSquadWrapper.Create( entity );
                }
                foreach ( GameEntity_Squad entity in playerFaction.Squads( "SpireSidekickCity" ) )
                {
                    SpireSidekickPerUnitBaseInfo data = entity.CreateExternalBaseInfo<SpireSidekickPerUnitBaseInfo>( "SpireSidekickPerUnitBaseInfo" );

                    SpireCities.AddToConstructionList( entity );
                    SortedSpireCities.AddToConstructionList( entity );
                }

                foreach ( GameEntity_Squad entity in playerFaction.Squads( "SpireFlagship" ) )
                {
                    SpirePlayerFleets.AddToConstructionListIfNotAlreadyIn( entity );
                }
            }

            SortedSpireCities.SortConstructionList( delegate ( SafeSquadWrapper left, SafeSquadWrapper right )
            {
                return left.GameSecondCreated.CompareTo( right.GameSecondCreated ); //asc
            } );

            DarkSpireFactions.SwitchConstructionToDisplay();
            ActiveRelics.SwitchConstructionToDisplay();
            SpireDebris.SwitchConstructionToDisplay();
            Transceiver.SwitchConstructionToDisplay();
            SpireCities.SwitchConstructionToDisplay();
            RangerOutposts.SwitchConstructionToDisplay();
            DireRangerOutposts.SwitchConstructionToDisplay();
            SortedSpireCities.SwitchConstructionToDisplay();
            SpirePlayerFleets.SwitchConstructionToDisplay();
            RangerCount.SwitchConstructionToDisplay();
            DireRangerCount.SwitchConstructionToDisplay();
            RangersPerCity.SwitchConstructionToDisplay();
            DireRangersPerCity.SwitchConstructionToDisplay();
            RangersPerPlanet.SwitchConstructionToDisplay();

            List<Faction> darkSpireFactions = DarkSpireFactions.GetDisplayList();
            if ( darkSpireFactions.Count > 0 ) //at least one dark spire faction!
            {
                foreach ( Faction dsFaction in darkSpireFactions )
                {
                    DarkSpireFactionBaseInfo dsdata = dsFaction.GetExternalBaseInfoAs<DarkSpireFactionBaseInfo>();
                    if ( !dsdata.ConquestMode && this.NumRelicsCaptured >= Difficulty.RelicsForDarkSpireConquestMode )
                    {
                        //update conquest mode for the Fallen Spire
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                            World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DarkSpireConquestModeFromFallenSpire", string.Empty, dsFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                        dsdata.ConquestMode = true;
                    }
                    if ( this.AttachedFaction.Type == FactionType.Player &&
                         !dsdata.ConquestMode && this.SpireCities.Count >= Difficulty.RelicsForDarkSpireConquestMode )
                    {
                        //this is for the spire infused empire; we use the number of cities owned, not relics captured
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                            World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DarkSpireConquestModeFromFallenSpire", string.Empty, dsFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                        dsdata.ConquestMode = true;
                    }
                }
            }
        }
        #endregion

        #region GetShouldAttackNormallyExcludedTarget
        public override bool GetShouldAttackNormallyExcludedTarget( GameEntity_Squad Target )
        {
            if ( Target.TypeData.IsCommandStation )
                return true;
            if ( Target.TypeData.GetHasTag( "WarpGate" ) )
                return true;
            if ( ImperialFleetActive &&
                 Target.Planet.IsEligibleForDeepStrike &&
                 Target.TypeData.GetHasTag( "CommandStation" ) )
                return true; //with the imperial spire, also blow up command stations that would trigger deepstrikes; needed for the Infused Empire

            if ( Target.TypeData.GetHasTag( "NormalPlanetNastyPick" ) )
                return true;
            return false;
        }
        #endregion
        public static string SpireCityBlockingReason_ToStringForUI(SpireCityBlockingReason reason)
        {
            string output = "";
            switch ( reason )
            {
              case SpireCityBlockingReason.OnSamePlanetAsOtherCity:
                output = "On same planet as other city";
                break;
              case SpireCityBlockingReason.TooNearOtherCity:
                output = "Too near other city";
                break;
              case SpireCityBlockingReason.IsAIHomeworld:
                output = "Is AI Homeworld";
                break;
              case SpireCityBlockingReason.RelicCannotLeaveInitialPlanet:
                output = "Relic Cannot Leave Initial Planet";
                break;
              case SpireCityBlockingReason.IsAIBastionWorld:
                output = "Is AI Bastion World";
                break;
              default:
                output = "Unknown, " + reason;
                break;
            }
            return output;
        }
        public SpireCityBlockingReason IsPlanetAllowedToBuildCity( Planet possiblePlanet, GameEntity_Squad entityOrNull )
        {
            SpireSidekickPerUnitBaseInfo data = null;
            if ( entityOrNull != null )
                data = entityOrNull.GetExternalBaseInfoAs<SpireSidekickPerUnitBaseInfo>();
            if ( data != null && data.MustBuildOnStartPlanet )
            {
                if ( possiblePlanet != entityOrNull.Planet )
                    return SpireCityBlockingReason.RelicCannotLeaveInitialPlanet;

                bool relicCannotLeaveInitialPlanetButPlanetIsAlreadyCity = false;
                foreach ( GameEntity_Squad city in this.SpireCities.DisplaySquads() )
                {
                    if ( city.Planet == entityOrNull.Planet )
                    {
                        relicCannotLeaveInitialPlanetButPlanetIsAlreadyCity = true;
                        break;
                    }
                }
                if ( relicCannotLeaveInitialPlanetButPlanetIsAlreadyCity )
                    return SpireCityBlockingReason.RelicCannotLeaveInitialPlanetButPlanetIsAlreadyCity; //can't have two cities on one planet!

                return SpireCityBlockingReason.None; //IF this relic must spawn on its current planet, then allow a city to be created on that planet (almost) no matter what.
            }

            if ( possiblePlanet.PopulationType == PlanetPopulationType.AIHomeworld )
                return SpireCityBlockingReason.IsAIHomeworld;
            if ( possiblePlanet.PopulationType == PlanetPopulationType.AIBastionWorld )
                return SpireCityBlockingReason.IsAIBastionWorld;
            //intended to be called from the UI
            bool foundNearbyCity = false;
            bool foundCityOnPlanet = false;
            try
            {
                foreach ( GameEntity_Squad city in this.SpireCities.DisplaySquads() )
                {
                    if ( city.Planet == possiblePlanet )
                    {
                        foundCityOnPlanet = true;
                        break;
                    }
                    if ( city.Planet.GetHopsTo( possiblePlanet ) <= MinHopsBetweenCities )
                    {
                        foundNearbyCity = true;
                    }
                }
            }
            catch //(Exception e )
            {
                return SpireCityBlockingReason.Unknown; //if the Sim code is changing the list out then assume it's an invalid place just to be safe
            }
            if ( foundCityOnPlanet )
                return SpireCityBlockingReason.OnSamePlanetAsOtherCity;
            if ( foundNearbyCity )
                return SpireCityBlockingReason.TooNearOtherCity;
            return SpireCityBlockingReason.None;
        }

        #region UpdatePowerLevel
        public override void UpdatePowerLevel()
        {
            FInt newResult = FInt.Zero;
            if ( SpireCities.Count < Difficulty.RelicsForLevel1Income )
            {
                FInt remainder = (FInt)SpireCities.Count / Difficulty.RelicsForLevel1Income;
                newResult = FInt.Zero + remainder;
            }
            if ( SpireCities.Count >= Difficulty.RelicsForLevel1Income && SpireCities.Count < Difficulty.RelicsForLevel2Income )
            {
                int city = SpireCities.Count - Difficulty.RelicsForLevel1Income;
                int required = Difficulty.RelicsForLevel2Income - Difficulty.RelicsForLevel1Income;
                FInt remainder = (FInt)city / required;
                newResult = FInt.One + remainder;
            }
            if ( SpireCities.Count >= Difficulty.RelicsForLevel2Income && SpireCities.Count < Difficulty.RelicsForLevel3Income )
            {
                int city = SpireCities.Count - Difficulty.RelicsForLevel2Income;
                int required = Difficulty.RelicsForLevel3Income - Difficulty.RelicsForLevel2Income;
                FInt remainder = (FInt)city / required;
                newResult = FInt.FromParts( 2, 000 ) + remainder;
            }
            if ( SpireCities.Count >= Difficulty.RelicsForLevel3Income && SpireCities.Count < Difficulty.RelicsForLevel4Income )
            {
                int city = SpireCities.Count - Difficulty.RelicsForLevel3Income;
                int required = Difficulty.RelicsForLevel4Income - Difficulty.RelicsForLevel3Income;
                FInt remainder = (FInt)city / required;
                newResult = FInt.FromParts( 3, 000 ) + remainder;
            }
            if ( SpireCities.Count >= Difficulty.RelicsForLevel4Income && SpireCities.Count < Difficulty.RelicsForLevel5Income )
            {
                int city = SpireCities.Count - Difficulty.RelicsForLevel4Income;
                int required = Difficulty.RelicsForLevel5Income - Difficulty.RelicsForLevel4Income;
                FInt remainder = (FInt)city / required;
                newResult = FInt.FromParts( 4, 000 ) + remainder;
            }
            if ( SpireCities.Count >= Difficulty.RelicsForLevel5Income || this.TimeUntilImperialFleetArrives > 0 ||
                 this.ImperialFleetActive )
            {
                //we are at max threat level (either tons of cities or the imperial spire fleet is summoned)
                newResult = FInt.FromParts( 5, 000 );
            }

            this.AttachedFaction.OverallPowerLevel = newResult;
        }
        #endregion

        #region CalculateCanCityUpgrade
        public static bool CalculateCanCityUpgrade( int nonSpirePlanetsOwnedByPlayers, GameEntity_Squad city )
        {
            if ( Instance == null )
                return false;
            //For each city, we determine if it can upgrade its mark level based on other cities and what it has inside it.
            if ( city.GetIsCrippled() || city.GetIsNonFunctional() )
                return false; //no upgrades for me right now, since I'm nonfunctional
            if ( city.CurrentMarkLevel > 6 )
                return false; //no upgrades for me, I'm already max mark level
            int requiredUnusedPlanetsForUpgrade = GetRequiredNonSpirePlayerPlanets( city );
            if ( requiredUnusedPlanetsForUpgrade > nonSpirePlanetsOwnedByPlayers )
                return false;
            int othersOfMyMark = 0;
            foreach ( GameEntity_Squad secondCity in Instance.SpireCities.DisplaySquads() )
            {
                if ( secondCity == city )
                    continue;
                if ( secondCity.GetIsCrippled() || secondCity.GetIsNonFunctional() )
                    continue; //don't count a crippled city for upgrade purposes
                if ( secondCity.CurrentMarkLevel == city.CurrentMarkLevel )
                    othersOfMyMark++; //found another city of our mark
                if ( othersOfMyMark >= 2 )
                    break;
            }
            if ( othersOfMyMark >= 2 )
            {
                Fleet cityFleet = city.GetFleetOrNull_Safe();
                if ( cityFleet == null )
                    return false;
                if ( cityFleet.CalculateRemainingCitySockets() <= 0 )
                    return true;
            }
            return false;
        }
        #endregion

        #region GetNonSpirePlanetsOwnedByPlayers
        public static int GetNonSpirePlanetsOwnedByPlayers()
        {
            int count = 0;
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                Faction controllingFaction = planet.GetControllingOrInfluencingFaction();
                if ( controllingFaction.Type == FactionType.Player )
                    count++;
            }
            return count - Instance.SpireCities.Count;
        }
        #endregion

        #region GetRequiredNonSpirePlayerPlanets
        public static int GetRequiredNonSpirePlayerPlanets( GameEntity_Squad entityOrNull )
        {
            //For each mark 3 or 4 city, you must own 1 additional planet without a spire city.
            //For each mark 5 or higher city, you must own 2 additional planets w/o a spire city.
            //This means that to build your first mark 5 city you must control 6 extra planets.
            //And to build a mark 7 spire city you must control 14 extra planets.
            int requiredPlanets = 0;
            foreach ( GameEntity_Squad city in Instance.SpireCities.DisplaySquads() )
            {
                if ( city.CurrentMarkLevel >= 6 )
                    requiredPlanets++;
                if ( city.CurrentMarkLevel >= 5 )
                    requiredPlanets++;
                if ( city.CurrentMarkLevel >= 2 )
                    requiredPlanets++;
            }
            if ( entityOrNull != null )
            {
                //if this entity's upgrading would increase the required count, factor that in
                if ( entityOrNull.CurrentMarkLevel == 1 || entityOrNull.CurrentMarkLevel == 4 || entityOrNull.CurrentMarkLevel == 5 )
                    requiredPlanets++;
            }
            return requiredPlanets;
        }
        #endregion

        #region WriteCityTooltipDetails
        public static void WriteCityTooltipDetails( ArcenCharacterBufferBase tooltipBuffer, GameEntity_Squad city )
        {
            if ( city == null )
                return;
            FleetMembership cityMem = city.FleetMembership;
            if ( cityMem == null )
                return;
            Fleet cityFleet = cityMem.Fleet;
            if ( cityFleet == null )
                return;

            int debugStage = 0;
            try
            {
                debugStage = 100;
                int nonSpirePlanetsOwnedByPlayers = GetNonSpirePlanetsOwnedByPlayers();
                debugStage = 200;
                bool canBeUpgraded = CalculateCanCityUpgrade( nonSpirePlanetsOwnedByPlayers, city );
                debugStage = 300;
                if ( Instance == null )
                {
                    tooltipBuffer.Add( "坠落尖塔阵营数据为空！请取消暂停查看详情。" );
                    return;
                }

                debugStage = 1000;
                if ( canBeUpgraded )
                {
                    debugStage = 1100;
                    tooltipBuffer.Add( "恭喜！您可以将尖塔城市 " ).Add( cityFleet.GetName() ).Add( " 在 " ).Add( city.GetPlanetName_Safe() );
                    tooltipBuffer.Add( " 升级到标记 " ).Add( (cityFleet.AddedMarkLevelsForFleet_FromScience + 2) ).Add( "\n" );
                    tooltipBuffer.Add( "请注意，一旦您升级了它，如果还有其他可以升级的城市，它们可能不再符合条件。请明智地选择升级哪座城市。\n" );
                    tooltipBuffer.Add( "满足条件原因：\n<size=80%>" );

                    tooltipBuffer.Add( "  通过：城市中心未受损或功能正常。" );
                }
                else
                {
                    debugStage = 2000;
                    tooltipBuffer.Add( "尖塔城市 " + cityFleet.GetName() ).Add( " 尚无法升级。\n<size=80%>" );

                    if ( city.CurrentMarkLevel > 6 )
                    {
                        tooltipBuffer.Add( "  失败：城市已达到最大升级等级！\n" );
                        tooltipBuffer.Add( "</size>" );
                        return;
                    }

                    if ( city.GetIsCrippled() || city.GetIsNonFunctional() )
                        tooltipBuffer.Add( "  失败：城市中心受损或功能异常。\n" );
                }


                debugStage = 4000;
                int requiredUnusedPlanetsForUpgrade = GetRequiredNonSpirePlayerPlanets( city );
                debugStage = 4100;
                if ( nonSpirePlanetsOwnedByPlayers >= requiredUnusedPlanetsForUpgrade )
                    tooltipBuffer.Add( "  通过：" );
                else
                    tooltipBuffer.Add( "  失败：" );
                debugStage = 4200;
                tooltipBuffer.Add( "当前有 " ).Add( nonSpirePlanetsOwnedByPlayers ).Add( " 个非尖塔城市人类星球，需要 " ).Add( requiredUnusedPlanetsForUpgrade ).Add( " 个。\n" );

                int othersOfMyMark = 0;
                List<SafeSquadWrapper> cities = Instance.SortedSpireCities.GetDisplayList(); //this is ui only, so fine to use this
                debugStage = 4300;
                for ( int j = 0; j < cities.Count; j++ )
                {
                    GameEntity_Squad secondCity = cities[j].GetSquad();
                    if ( secondCity == null )
                        continue;
                    debugStage = 4400;
                    if ( secondCity == city )
                        continue;
                    if ( secondCity.GetIsCrippled() || secondCity.GetIsNonFunctional() )
                        continue; //don't count a crippled city for upgrade purposes
                    if ( secondCity.CurrentMarkLevel == city.CurrentMarkLevel )
                        othersOfMyMark++; //found another city of our mark
                }

                debugStage = 5000;
                if ( othersOfMyMark >= 2 )
                    tooltipBuffer.Add( "  通过：" );
                else
                    tooltipBuffer.Add( "  失败：" );
                debugStage = 5100;
                tooltipBuffer.Add( "当前有 " ).Add( othersOfMyMark ).Add( " 个（需要2个）同标记 " ).Add( city.CurrentMarkLevel ).Add( " 的尖塔城市。\n" );

                debugStage = 5200;
                int remainingCitySockets = cityFleet.CalculateRemainingCitySockets();
                if ( remainingCitySockets <= 0 )
                    tooltipBuffer.Add( "  通过：该星球的所有 " ).Add( city.TypeData.NameForCitySockets_Plural ).Add( " 均已使用。\n" );
                else
                    tooltipBuffer.Add( "  失败：该星球仍有 " ).Add( remainingCitySockets ).Add( " 个 " ).Add( city.TypeData.NameForCitySockets_Plural ).Add( " 需要使用才能升级。\n" );

                tooltipBuffer.Add( "</size>\n" );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "SpireSidekick.WriteCityTooltipDetails Error at debug stage " + debugStage + ":\n" + e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region SetRelicDestination
        public static void SetRelicDestination( GameEntity_Squad relic, Planet destinationPlanet, ArcenPoint destinationPoint )
        {
            int debugCode = 0;
            try{
                debugCode = 100;

                if ( relic == null )
                    return;
                debugCode = 200;
                if ( destinationPlanet == null )
                    throw new Exception("Unable to find destination planet for relic");
                debugCode = 300;
                SpireSidekickPerUnitBaseInfo data = relic.GetExternalBaseInfoAs<SpireSidekickPerUnitBaseInfo>();
                if ( data == null )
                    throw new Exception("Unable to find destination planet for relic");
                data.DestinationPlanet = destinationPlanet;
                data.DestinationPoint = destinationPoint;
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in SetRelicDestination for Spire Sidekic. debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion

        #region CreateRelic
        public void CreateRelic( Planet planet, Faction fallenSpireFaction, Faction playerThatSummonedRelic, ArcenHostOnlySimContext Context, FInt responseMultiplier, bool cannotLeaveStartingPlanet, ArcenPoint spawnLocation, bool UseSphericalRelic )
        {
            if ( Context == null )
                return; //this would be a client

            //this can be called from the relic hack or from some other source ( killing a relic train, for example)
            //hacking for a relic grants a spherical relic, killing an AI structure grants a non-spherical relic
            if ( playerThatSummonedRelic.Type != FactionType.Player )
                throw new Exception( playerThatSummonedRelic.GetDisplayName() + " is not allowed to summon a spire relic" );
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "SphericalSpireSidekickRelic" );
            if ( !UseSphericalRelic ) //we want to use the angular relic
                entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "AngularSpireSidekickRelic" );
            if ( entityData == null || fallenSpireFaction == null )
                throw new Exception( "Unexpected null values spawning relic. Perhaps there's no XML SpireRelic? UseSphericalRelic: " + UseSphericalRelic );
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( fallenSpireFaction );
            if ( spawnLocation == ArcenPoint.ZeroZeroPoint ) //shouldn't be possible, but just in case
                spawnLocation = Engine_AIW2.Instance.CombatCenter;
            GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                                                                  pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "SpireSidekickRelic" );
            SpireSidekickPerUnitBaseInfo data = entity.CreateExternalBaseInfo<SpireSidekickPerUnitBaseInfo>( "SpireSidekickPerUnitBaseInfo" );
            data.FactionThatFoundThisRelic = playerThatSummonedRelic.FactionIndex;
            data.RelicResponseMultiplier = responseMultiplier;
            data.MustBuildOnStartPlanet = cannotLeaveStartingPlanet;

            if ( SpireCities.Count == 0 )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_FirstRelicFound", string.Empty, fallenSpireFaction, null, planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        }
        #endregion

        #region GetSpireSidekickStateForDisplay
        public void GetSpireSidekickStateForDisplay( ArcenDoubleCharacterBuffer buffer )
        {
            buffer.Add( "坠落尖塔状态\n" );
            buffer.Add( "有 " + this.TimesForNextSpireDebris.Count + " 个残骸时间点：\n" );
            for ( int i = 0; i < this.TimesForNextSpireDebris.Count; i++ )
            {
                buffer.Add( "\t" ).Add( i ).Add( ": " ).Add( this.TimesForNextSpireDebris[i] ).Add( "\n" );
            }
            if ( this.TimeForNextRelicSpawn != -1 )
                buffer.Add( "下次遗物生成时间：" + this.TimeForNextRelicSpawn + " 当前时间 " + World_AIW2.Instance.GameSecond );
        }
        #endregion

        #region GetFireteamById
        public override Fireteam GetFireteamById( int id )
        {
            return FireteamBaseUtility.GetFireteamById( this.Teams, id );
        }
        #endregion

        #region GetSpireSidekickGeneralAIResponseMultiplier
        public static FInt GetSpireSidekickGeneralAIResponseMultiplier()
        {
            if ( Instance == null || Instance.Difficulty == null )
                return FInt.Zero;
            FInt output = Instance.SpireCities.Count * Instance.Difficulty.GeneralAIResponseIncreasePerCity;
            return output;
        }
        #endregion

        #region DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost
        public override void DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost( ArcenClientOrHostSimContextCore Context, bool IsFirstCallToFactionOfThisTypeThisCycle )
        {
            if ( World_AIW2.Instance.GameSecond < 3 )
                return;

            Faction localFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            Faction localFactionOrFirstHumanFactionOrNull = localFactionOrNull;
            if ( localFactionOrFirstHumanFactionOrNull == null )
            {
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction fac = World_AIW2.Instance.Factions[i];
                    if ( fac.Type == FactionType.Player )
                    {
                        localFactionOrFirstHumanFactionOrNull = fac;
                        break;
                    }
                }
            }

            int debugStage = 1;
            try
            {
                debugStage = 1000;
                Faction fallenSpireFaction = localFactionOrNull;

                debugStage = 140000;
                #region Fill NonSim Notifications List Relating to relic trains
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    if ( otherFaction.SpecialFactionData.Type != FactionType.AI )
                        continue;
                    foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.IsTrain ) )
                    {
                        NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                        fillData.Entity = SafeSquadWrapper.Create( entity );
                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( PublicRelicTrainSidekickNotifier.Instance, fillData, "", 0, "Spire Relic Train", SortedNotificationPriorityLevel.Informational );
                    }
                }
                #endregion

                debugStage = 190000;
                #region Fill NonSim Notifications List Relating To Spire Relics
                if ( fallenSpireFaction != null )
                {
                    if ( this.CurrentRelicSpawnPlanetIdx != -1 )
                    {
                        //there's a relic to be found
                        NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                        fillData.Faction = fallenSpireFaction;
                        fillData.Planet = World_AIW2.Instance.GetPlanetByIndex( this.CurrentRelicSpawnPlanetIdx );
                        fillData.planetIdx = this.CurrentRelicSpawnPlanetIdx;
                        fillData.inSearchMode = this.CurrentRelicInSearchMode;
                        fillData.Int16List.AddRange( this.PlanetsSearchedForCurrentRelic );
                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( PublicSpireRelicNotifier.Instance, fillData, "", 0, "Spire Relic", SortedNotificationPriorityLevel.Informational );
                    }
                }
                #endregion

                debugStage = 191000;
                #region Fill NonSim Notifications List Relating To Spire City Upgrades
                if ( fallenSpireFaction != null )
                {
                    int nonSpirePlanetsOwnedByPlayers = GetNonSpirePlanetsOwnedByPlayers();
                    List<SafeSquadWrapper> spireCities = this.SortedSpireCities.GetDisplayList(); //ui only, so fine to use this
                    for ( int i = 0; i < spireCities.Count; i++ )
                    {
                        GameEntity_Squad city = spireCities[i].GetSquad();
                        if ( city == null )
                            continue;
                        if ( city != null && CalculateCanCityUpgrade( nonSpirePlanetsOwnedByPlayers, city ) )
                        {
                            //there's a city that we can upgrade
                            NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                            fillData.Entity = SafeSquadWrapper.Create( city );
                            NotificationNonSim notification = new NotificationNonSim();
                            notification.Assign( PublicSpireSidekickCityUpgradeNotifier.Instance, fillData, "", 0, "Spire City", SortedNotificationPriorityLevel.Informational );
                        }
                    }
                }
                #endregion

                debugStage = 200000;
                #region Fill NonSim Notifications List Relating To Spire Debris
                if ( this.SpireDebris.Count > 0 )
                {
                    NotifierFillData fillData = null;

                    foreach ( GameEntity_Squad debris in this.SpireDebris.DisplaySquads() )
                    {
                        SpireSidekickPerUnitBaseInfo debrisData = debris.TryGetExternalBaseInfoAs<SpireSidekickPerUnitBaseInfo>();
                        if ( debrisData == null )
                            continue;
                        if ( debris.AmIBeingHacked() ) //debris is being actively hacked;
                            continue;
                        if ( fillData == null )
                            fillData = NotifierFillData.GetFromPoolOrCreate();
                        fillData.EntityList.Add( debris );
                    }
                    if ( fillData != null )
                    {
                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( PublicSpireDebrisNotifier.Instance, fillData, "", 0, "Spire Debris", SortedNotificationPriorityLevel.Informational );
                    }
                }
                #endregion

                debugStage = 210000;
                #region Fill NonSim Notifications List Relating To the Imperial Spire warpin
                if ( this.TimeUntilImperialFleetArrives != -1 )
                {
                    //countdown for the imperial fleet arrival
                    NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                    fillData.Faction = this.AttachedFaction;
                    fillData.eventTimeRemaining = this.TimeUntilImperialFleetArrives;
                    NotificationNonSim notification = new NotificationNonSim();
                    notification.Assign( PublicImperialSpireNotifier.Instance, fillData, "", 0, "Imperial Spire", SortedNotificationPriorityLevel.Informational );
                }

                #endregion

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "SpireSidekick.Notifications Error at debug stage " + debugStage + ":\n" + e, Verbosity.ShowAsError );
            }
        }
        #endregion

        public ExoData GetExoData() //from IExoDataHolder
        {
            return this.exoData;
        }
        
        public override void WriteFactionSlotStatus( ArcenCharacterBufferBase buffer )
        {
            if ( World_AIW2.Instance == null )
                return;
            
            var fac = AttachedFaction;

            if (fac.Type != FactionType.Player)
            {
                //buffer.Add("<size=75%><voffset=0.1em><i>     Enselle-illumin</i></voffset></size>", fac.FactionCenterColor.ColorHex );
                buffer.Add( "强度 " ).Add(Intensity);
                return;
            }

            var str = fac.GetStringValueForCustomFieldOrDefaultValue( "StartingFleet", false );
            if ( str == "RandomCombatFleet" )
            {
                buffer.Add( "随机", "ffd965" ).Add( " 舰队" );
                goto done;
            }

            if ( string.IsNullOrWhiteSpace( str ) )
            {
                buffer.Add( "???" );
                goto done;
            }

            var fleet = FleetDesignTemplateTable.Instance.GetRowByNameOrNullIfNotFound( str );
            if ( fleet != null )
            {
                buffer.Add( fleet.GetDisplayName() );
                goto done;
            }

            buffer.Add( str );
            
            done:
            WriteControllingAccount( buffer );
        }
        #region Sidekick
        #region CheckIfPlayerFactionShouldGetRewardBasedOnAUnitDying
        /// <summary>
        /// This is to support the Spire Sidekick
        /// This is called on every player faction when any unit is killed, period.  It identifies who the killing faction is, and allows for custom logic to be run.
        /// Many times, this will be utterly unrelated to anything the player faction needs to do.  But if the player faction is "the strongest faction of type X on that planet where the thing died,"
        /// for instance, then this is a method where that sort of thing can be calculated and then some reward can be granted.
        /// Note that the dyson's logic here is different because the killing faction can be null
        /// </summary>
        public override void CheckIfPlayerFactionShouldGetRewardBasedOnAUnitDying( Faction factionThatKilledEntityOrNull, bool IsFromOnlyPartOfStackDying, GameEntity_Squad entity,
            DamageSource Damage, EntitySystem FiringSystemOrNull, int numExtraStacksKilled, ArcenHostOnlySimContext Context )
        {
            FInt multiplier = FInt.One;
//            bool debug = false;
            if ( this.GetShouldThisSpireFactionGetARewardBasedOnThisKill( factionThatKilledEntityOrNull, entity, ref multiplier, Context ) )
            {
                DLC3GameEntityTypeDataExtension entity_DLC3TypeData = entity.TypeData.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );

                if ( entity_DLC3TypeData != null )
                {
                    // FInt scienceToGrantOnDeath = FInt.Zero;
                    // FInt hackingToGrantOnDeath = FInt.Zero;
                    // FInt resourceOneToGrantOnDeath = FInt.Zero;
                    entity_DLC3TypeData.GetSpireSidekickResourcesToGrantOnDeath(entity,
                            out FInt scienceToGrantOnDeath, out FInt hackingToGrantOnDeath );
                    if ( hackingToGrantOnDeath > FInt.Zero )
                    {
                        hackingToGrantOnDeath *= multiplier;
                        if ( hackingToGrantOnDeath < FInt.One )
                            hackingToGrantOnDeath = FInt.One;

                        this.AttachedFaction.StoredHacking += hackingToGrantOnDeath;
                    }

                    if ( scienceToGrantOnDeath > FInt.Zero )
                    {
                        scienceToGrantOnDeath *= multiplier;
                        if ( scienceToGrantOnDeath < FInt.One )
                            scienceToGrantOnDeath = FInt.One;
                        this.AttachedFaction.StoredScience += scienceToGrantOnDeath;
                    }
                }
            }
        }
        #endregion
        #region GetShouldThisSpireFactionGetARewardBasedOnThisKill
        private bool GetShouldThisSpireFactionGetARewardBasedOnThisKill( Faction killingFactionOrNull, GameEntity_Squad entity, ref FInt multiplier, ArcenHostOnlySimContext Context )
        {
            //A bunch of rules; fundamentally, the rules are:
            //if we killed the unit, or were part of the fight, we get full resources
            //If this was killed by an Allied NPC faction, we get partial resources
            //If this was killed by an allied, non-dyson faction we get partial resources
            FInt resourceMultiplierForAlliedHumanKill = FInt.FromParts( 0, 500 );
            FInt resourceMultiplierForAlliedNPCKill = FInt.FromParts( 0, 750 );

            if ( !entity.PlanetFaction.Faction.GetIsHostileTowards( this.AttachedFaction ) ) 
                return false; //We must be must be hostile to the killed unit
            if ( killingFactionOrNull == this.AttachedFaction )
                return true; //if we killed the unit, we get full resources
            PlanetFaction pFaction = entity.Planet.GetPlanetFactionForFaction( this.AttachedFaction );
            if ( pFaction.DataByStance[FactionStance.Self].TotalStrength > 500 )
                return true; //if we are involved with the fight, we get full resources

            //now for the case where we aren't involved with the fight
            if ( killingFactionOrNull != null &&
                 killingFactionOrNull.GetIsFriendlyTowards( this.AttachedFaction ) )
            {
                //Killed by an ally
                if ( killingFactionOrNull.Type == FactionType.Player )
                {
                    //30% resources from a human player
                    multiplier = resourceMultiplierForAlliedHumanKill;
                    return true;
                }
                else
                {
                    //75% resources from an NPC faction
                    multiplier = resourceMultiplierForAlliedNPCKill;
                    return true;
                }
            }
            return false; //if this specific spire doesn't get these resources, someone else still might

        }
        #endregion  GetShouldThisSpireFactionGetARewardBasedOnThisKill
        public static int GetSpireSidekickFactionCount()
        {
            int count = 0;
            PlayerTypeData playerType;

            playerType = PlayerTypeDataTable.Instance.GetRowByName( "SpireSidekick", LookupSwapAllowed.Yes, true );
            if (playerType != null)
            count += playerType.CurrentFactionsInThisGame.Count;

            return count;
        }
        #endregion Sidekick
    }

}
