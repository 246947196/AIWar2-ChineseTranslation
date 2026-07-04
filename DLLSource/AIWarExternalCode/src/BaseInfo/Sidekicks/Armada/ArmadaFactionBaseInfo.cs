using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ArmadaFactionBaseInfo : ExternalFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {
        //Serialized
        public readonly List<ArmadaUpgradeEvent> ArmadaHistory = List<ArmadaUpgradeEvent>.Create_WillNeverBeGCed( 10, "Faction-ArmadaUpgradeHistory" );
        public readonly List<ArmadaUpgrade> ArmadaCompletedUpgrades = List<ArmadaUpgrade>.Create_WillNeverBeGCed( 90, "ArmadaFactionBaseInfo-ArmadaCompletedUpgrades" ); //this is filled out from the ArmadaCompletedUpgradeIndices in the dysonmancer faction code
        public int StrengthForNextEnemyAttack;
        public int TimeForNextEnemyAttack;
        public int VeinsEmpowered;
        public readonly Dictionary<Planet, int> MiningIneligiblePlanets = Dictionary<Planet, int>.Create_WillNeverBeGCed( 500, "ArmadaFactionBaseInfo-MiningIneligiblePlanets" );
        public readonly Dictionary<Planet, int> PlanetMineCount = Dictionary<Planet, int>.Create_WillNeverBeGCed( 500, "ArmadaFactionBaseInfo-PlanetMineCount" ); 
        //Non-Serialized
        public static ArmadaFactionBaseInfo Instance; //there can only ever be one of this faction at a time
        public ArmadaDifficulty Difficulty;
        public ArmadaIncome Income;
        public readonly DoubleBufferedList<ArmadaUpgrade> AvailableBlueprints = DoubleBufferedList<ArmadaUpgrade>.Create_WillNeverBeGCed( 20, "Armada-AvailableBlueprints" );
        public readonly DoubleBufferedList<SafeSquadWrapper> BolsterableFlagships = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Armada-BolsterableFlagships" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Flagships = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Armada-Flagships" );
        public readonly DoubleBufferedValue<int> RangerCount = new DoubleBufferedValue<int>( 0 );
        public readonly DoubleBufferedValue<int> DireRangerCount = new DoubleBufferedValue<int>( 0 );

        public readonly DoubleBufferedList<SafeSquadWrapper> SwarmLures = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "Scourge-SwarmLures" );
        public readonly DoubleBufferedList<SafeSquadWrapper> SwarmLaunchers = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "Armada-SwarmLaunchers" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Locusts = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "Armada-Locusts" );

        //this gets updated during the process of sim steps, so had to be converted to DoubleBufferedConcurrentLists.  This is less performant
        //than DoubleBufferedList, but won't have cross-threading issues from an activity like that
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> Starbases = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Armada-Starbases" );
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> UnownedStarbases = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Armada-UnownedStarbases" );
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> Mines = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Armada-Mines" );
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> Producers = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Armada-Producers" );
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> Transports = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Armada-Transports" );
        public readonly DoubleBufferedList<SafeSquadWrapper> TransportDestinations = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed(20, "Armada-TransportDestinations");
        //Rangers
        public readonly DoubleBufferedList<SafeSquadWrapper> Rangers = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Armada-Rangers" );
        public readonly DoubleBufferedList<SafeSquadWrapper> RangerProducers = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Armada-RangerProducers" );
        public readonly DoubleBufferedList<SafeSquadWrapper> RangerOutposts = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Armada-RangerOutposts" );
        public readonly DoubleBufferedList<SafeSquadWrapper> DireRangerOutposts = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Armada-DireRangerOutposts" );
        public readonly DoubleBufferedDictionary<SafeSquadWrapper, int> RangersPerStarbase = DoubleBufferedDictionary<SafeSquadWrapper, int>.Create_WillNeverBeGCed( 20, "ArmadaFactionBaseInfo-RangersPerStarbase" );
        public readonly DoubleBufferedDictionary<SafeSquadWrapper, int> DireRangersPerStarbase = DoubleBufferedDictionary<SafeSquadWrapper, int>.Create_WillNeverBeGCed( 20, "ArmadaFactionBaseInfo-DireRangersPerStarbase" );
        public readonly DoubleBufferedDictionary<Planet, int> RangersPerPlanet = DoubleBufferedDictionary<Planet, int>.Create_WillNeverBeGCed( 500, "ArmadaFactionBaseInfo-RangersPerPlanet" );
        public readonly DoubleBufferedDictionary<Planet, int> LocustsPerLure = DoubleBufferedDictionary<Planet, int>.Create_WillNeverBeGCed( 500, "ArmadaFactionBaseInfo-LocustsPerLure" );
        public readonly DoubleBufferedDictionary<Planet, int> TotalHopsForLocustsPerLure = DoubleBufferedDictionary<Planet, int>.Create_WillNeverBeGCed( 500, "ArmadaFactionBaseInfo-TotalHopsForLocustsPerLure" );

        //Some of these are used in the UI code
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> AllResourceGenerators = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "ArmadaFactionBaseInfo-AllResourceGenerators" );

        public int BonusStartingResources = 0;
        public int IncomeModifier = 0;
        public int Intensity = 0;
        public bool StartWithAllUpgrades = false;
        public int NumShipyards = 0;

        public readonly int RequiredHopsBetweenStarbases
            = 1;

        public ArmadaFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            BonusStartingResources = 0;
            Starbases.Clear();
            UnownedStarbases.Clear();
            Mines.Clear();
            Producers.Clear();

            Transports.Clear();
            TransportDestinations.Clear();
            
            BolsterableFlagships.Clear();
            Flagships.Clear();
            SwarmLures.Clear();
            SwarmLaunchers.Clear();
            Locusts.Clear();
            MiningIneligiblePlanets.Clear();
            PlanetMineCount.Clear();
            IncomeModifier = 0;
            Intensity = 0;
            StartWithAllUpgrades = false;
            Difficulty = null;
            Income = null;
            Instance = null;
            NumShipyards = 0;
            Rangers.Clear();
            RangerProducers.Clear();
            RangerOutposts.Clear();
            DireRangerOutposts.Clear();
            RangerCount.Clear();
            DireRangerCount.Clear();
            RangersPerStarbase.Clear();
            DireRangersPerStarbase.Clear();
            RangersPerPlanet.Clear();

            LocustsPerLure.Clear();
            TotalHopsForLocustsPerLure.Clear();

            TimeForNextEnemyAttack = 0;
            StrengthForNextEnemyAttack = 0;
            VeinsEmpowered = 0;
        }

        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "ArmadaFactionBaseInfo" );

            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.ArmadaCompletedUpgrades.Count, "ArmadaCompletedUpgrades" );
            for ( int i = 0; i < this.ArmadaCompletedUpgrades.Count; i++ )
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.ArmadaCompletedUpgrades[i].Index, "ArmadaCompletedUpgradeIndex" );

            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.ArmadaHistory.Count, "ArmadaHistory.Count" );
            for ( int i = 0; i < this.ArmadaHistory.Count; i++ )
            this.ArmadaHistory[i].SerializeTo( MetaData, Buffer, SerializationCmdType );
            Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, this.MiningIneligiblePlanets.Count );
            foreach ( KeyValuePair<Planet, int> pair in this.MiningIneligiblePlanets )
            {
                Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, pair.Key.Index );
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, pair.Value );
            }

            Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, this.PlanetMineCount.Count );
            foreach ( KeyValuePair<Planet, int> pair in this.PlanetMineCount )
            {
                Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, pair.Key.Index );
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, pair.Value );
            }

            Buffer.AddInt32(MetaData, ReadStyle.NonNeg, this.TimeForNextEnemyAttack, "TimeForNextEnemyAttack");
            Buffer.AddInt32(MetaData, ReadStyle.NonNeg, this.VeinsEmpowered, "VeinsEmpowered");
            Buffer.AddInt32(MetaData, ReadStyle.NonNeg, this.StrengthForNextEnemyAttack, "StrengthForNextEnemyAttack");

        }
        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "ArmadaFactionBaseInfo" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "ArmadaFactionBaseInfo Ext", TrackerStyle.ByTypeOnly );

            int countToExpect = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "ArmadaCompletedUpgrades" );
            this.ArmadaCompletedUpgrades.Clear();
            for ( int i = 0; i < countToExpect; i++ )
                this.ArmadaCompletedUpgrades.Add( ArmadaUpgradeTable.Instance.GetRowByIndex( (Int32)Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "ArmadaCompletedUpgradeIndex" ) ) );

            countToExpect = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "ArmadaHistory.Count" );
            this.ArmadaHistory.DeserializeUncertainNumberOfEntriesIntoExistingList( countToExpect,
                delegate { return ArmadaUpgradeEvent.GetFromPoolOrCreate(); },
                delegate ( ArmadaUpgradeEvent Event ) { Event.DeserializedIntoSelf( MetaData, Buffer, SerializationCmdType ); } );
            this.MiningIneligiblePlanets.Clear();
            int numLookupElements = Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511 );
            for ( int i = 0; i < numLookupElements; i++ )
            {
                //I break these variables out to make it clear what's happening
                Int16 planetIdx = (Int16)Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511 );
                Planet planet = World_AIW2.Instance.GetPlanetByIndex( planetIdx );
                int numSeconds = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg );
                this.MiningIneligiblePlanets[planet] = numSeconds;
            }
            if (Buffer.FromGameVersion.GetGreaterThanOrEqualTo(5, 707))
            {
                this.PlanetMineCount.Clear();
                numLookupElements = Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511 );
                for ( int i = 0; i < numLookupElements; i++ )
                {
                    //I break these variables out to make it clear what's happening
                    Int16 planetIdx = (Int16)Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511 );
                    Planet planet = World_AIW2.Instance.GetPlanetByIndex( planetIdx );
                    int numSeconds = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg );
                    this.PlanetMineCount[planet] = numSeconds;
                }
            }

            this.TimeForNextEnemyAttack = Buffer.ReadInt32(MetaData, ReadStyle.NonNeg, "TimeForNextEnemyAttack");
            this.VeinsEmpowered = Buffer.ReadInt32(MetaData, ReadStyle.NonNeg, "VeinsEmpowered");
            this.StrengthForNextEnemyAttack = Buffer.ReadInt32(MetaData, ReadStyle.NonNeg, "StrengthForNextEnemyAttack");
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( "130 Load From Armada" );
            return 130;
        }

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;
        }
        #endregion
        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            if ( Intensity == -1 || Income == null )
                DoRefreshFromFactionSettings();
            return Intensity;
        }

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                ConfigurationForFaction cfg = this.AttachedFaction.Config;
                debugCode = 200;
                BonusStartingResources = cfg.GetIntValueForCustomFieldOrDefaultValue("ArmadaStartingResources", true);
                IncomeModifier = cfg.GetIntValueForCustomFieldOrDefaultValue("ArmadaIncome", true);
                Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue("ArmadaDifficulty", true);
                //StartWithAllUpgrades = cfg.GetBoolValueForCustomFieldOrDefaultValue("ArmadaUnlockAllTechs", true);
                debugCode = 300;
                if ( ArmadaDifficultyTable.Instance == null)
                {
                    debugCode = 310;
                    ArcenDebugging.ArcenDebugLogSingleLine("null instance of diff table, you probably need to add Surrogate Table XML", Verbosity.DoNotShow );
                }
                Difficulty = ArmadaDifficultyTable.Instance.GetRowByIntensity(this.Intensity, this.AttachedFaction);

                if ( ArmadaIncomeTable.Instance == null)
                {
                    debugCode = 310;
                    ArcenDebugging.ArcenDebugLogSingleLine("null instance of income table", Verbosity.DoNotShow );
                }
                debugCode = 400;
                Income = ArmadaIncomeTable.Instance.GetRowByIntensity(this.IncomeModifier, this.AttachedFaction);
            } catch(Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("hit exception in DoRefreshFromFactionSettings debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion

        #region SetStartingFactionRelationships
        public override void SetStartingFactionRelationships()
        {
            base.SetStartingFactionRelationships();
            AllegianceHelper.AllyThisFactionToHumans( this.AttachedFaction );
        }
        #endregion
        public int GetHighestStarbaseMarkLevel()
        {
            int highestLevel = 0;
            foreach ( GameEntity_Squad city in this.Starbases.DisplaySquads() )
            {
                if ( city.CurrentMarkLevel > highestLevel )
                    highestLevel = city.CurrentMarkLevel;
            }
            return highestLevel;
        }
        public int GetHighestFlagshipMarkLevel()
        {
            List<SafeSquadWrapper> flagships = this.Flagships.GetDisplayList();
            int highestLevel = 0;
            for ( int i = 0; i < flagships.Count; i++ )
            {
                GameEntity_Squad flagship = flagships[i].GetSquad();
                if ( flagship == null )
                    continue;
                if ( flagship.CurrentMarkLevel > highestLevel )
                    highestLevel = flagship.CurrentMarkLevel;
            }
            return highestLevel;
        }

        // public bool HasAdequateDistrictsToBuild(GameEntityTypeData buildingInfo)
        // {
        //     bool debug = false;
        //     if ( buildingInfo == null )
        //     {
        //         if ( debug )
        //             ArcenDebugging.ArcenDebugLogSingleLine("path A: null buildingInfo", Verbosity.DoNotShow );
        //         return false;
        //     }
        //     if ( debug )
        //         ArcenDebugging.ArcenDebugLogSingleLine("Checking whether we can build " + buildingInfo.GetDisplayName(), Verbosity.DoNotShow );
        //     if ( this.StartWithAllUpgrades )
        //         return true;
        //     if (buildingInfo.GetHasTag("GatedBySpireDistrict"))
        //     {
        //         if (this.SpireDistrictTier <= 2 &&
        //              buildingInfo.GetHasTag("RequiresSpireDistrictThree"))
        //         {
        //             if ( debug )
        //                 ArcenDebugging.ArcenDebugLogSingleLine("path spire 3", Verbosity.DoNotShow );

        //             return false; //we need to be at district three
        //         }
        //         if (this.SpireDistrictTier <= 1 &&
        //              buildingInfo.GetHasTag("RequiresSpireDistrictTwo"))
        //         {
        //             if ( debug )
        //                 ArcenDebugging.ArcenDebugLogSingleLine("path spire 2", Verbosity.DoNotShow );

        //             return false; //we need to be at district two
        //         }
        //     }
        //     if (buildingInfo.GetHasTag("GatedByZenithDistrict"))
        //     {
        //         if (this.ZenithDistrictTier <= 2 &&
        //              buildingInfo.GetHasTag("RequiresZenithDistrictThree"))
        //         {
        //             if ( debug )
        //                 ArcenDebugging.ArcenDebugLogSingleLine("path zenith 3", Verbosity.DoNotShow );

        //             return false; //we need to be at district three
        //         }
        //         if (this.ZenithDistrictTier <= 1 &&
        //              buildingInfo.GetHasTag("RequiresZenithDistrictTwo"))
        //         {
        //             if ( debug )
        //                 ArcenDebugging.ArcenDebugLogSingleLine("path zenith 2", Verbosity.DoNotShow );

        //             return false; //we need to be at district two
        //         }
        //     }

        //     if (buildingInfo.GetHasTag("GatedByNeinzulDistrict"))
        //     {
        //         if (this.NeinzulDistrictTier <= 2 &&
        //              buildingInfo.GetHasTag("RequiresNeinzulDistrictThree"))
        //         {
        //             if ( debug )
        //                 ArcenDebugging.ArcenDebugLogSingleLine("path Neinzul 3", Verbosity.DoNotShow );

        //             return false; //we need to be at district three
        //         }
        //         if (this.NeinzulDistrictTier <= 1 &&
        //              buildingInfo.GetHasTag("RequiresNeinzulDistrictTwo"))
        //         {
        //             if ( debug )
        //                 ArcenDebugging.ArcenDebugLogSingleLine("path Neinzul 2", Verbosity.DoNotShow );

        //             return false; //we need to be at district two
        //         }
        //     }
        //     if (buildingInfo.GetHasTag("GatedByTemplarDistrict"))
        //     {
        //         if (this.TemplarDistrictTier <= 2 &&
        //              buildingInfo.GetHasTag("RequiresTemplarDistrictThree"))
        //         {
        //             if ( debug )
        //                 ArcenDebugging.ArcenDebugLogSingleLine("path Templar 3", Verbosity.DoNotShow );

        //             return false; //we need to be at district three
        //         }
        //         if (this.TemplarDistrictTier <= 1 &&
        //              buildingInfo.GetHasTag("RequiresTemplarDistrictTwo"))
        //         {
        //             if ( debug )
        //                 ArcenDebugging.ArcenDebugLogSingleLine("path Templar 2", Verbosity.DoNotShow );

        //             return false; //we need to be at district two
        //         }
        //     }
        //     if ( debug )
        //         ArcenDebugging.ArcenDebugLogSingleLine("Can build", Verbosity.DoNotShow );

        //     return true;
        // }
        #region GetArmadaStateForDisplay
        public void GetArmadaStateForDisplay( ArcenDoubleCharacterBuffer buffer )
        {
            //For debug, this goes in the Threat menu
            buffer.Add( "\n" );
            buffer.Add( "We currently have " ).Add( this.ArmadaCompletedUpgrades.Count ).Add( " faction upgrades:\n" );
            for ( int i = 0; i < this.ArmadaCompletedUpgrades.Count; i++ )
            {
                buffer.Add( "\t" ).Add( this.ArmadaCompletedUpgrades[i].ToString() ).Add( "\n" );
            }
            buffer.Add( "We have " ).Add( this.AttachedFaction.StoredFactionResourceOne, "a1ffa1" ).Add( " Essence.\n" );
        }
        #endregion

        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                Starbases.ClearConstructionListForStartingConstruction();
                UnownedStarbases.ClearConstructionListForStartingConstruction();
                Mines.ClearConstructionListForStartingConstruction();
                Producers.ClearConstructionListForStartingConstruction();
                Transports.ClearConstructionListForStartingConstruction();
                TransportDestinations.ClearConstructionListForStartingConstruction();
                BolsterableFlagships.ClearConstructionListForStartingConstruction();
                Flagships.ClearConstructionListForStartingConstruction();

                SwarmLures.ClearConstructionListForStartingConstruction();
                SwarmLaunchers.ClearConstructionListForStartingConstruction();
                Locusts.ClearConstructionListForStartingConstruction();
                Rangers.ClearConstructionListForStartingConstruction();
                RangerProducers.ClearConstructionListForStartingConstruction();
                RangerOutposts.ClearConstructionListForStartingConstruction();
                DireRangerOutposts.ClearConstructionListForStartingConstruction();
                RangerCount.ClearConstructionValueForStartingConstruction();
                DireRangerCount.ClearConstructionValueForStartingConstruction();
                RangersPerStarbase.ClearConstructionDictForStartingConstruction();
                DireRangersPerStarbase.ClearConstructionDictForStartingConstruction();
                RangersPerPlanet.ClearConstructionDictForStartingConstruction();

                LocustsPerLure.ClearConstructionDictForStartingConstruction();
                TotalHopsForLocustsPerLure.ClearConstructionDictForStartingConstruction();

                debugCode = 150;
                if ( Income == null || Intensity == -1 )
                    DoRefreshFromFactionSettings();

                debugCode = 200;
                foreach ( GameEntity_Squad squad in World_AIW2.Instance.Squads( "SeededArmadaStarbase" ) )
                {
                    if ( squad == null )
                        continue;
                    if ( squad.PlanetFaction.Faction != AttachedFaction )
                        UnownedStarbases.AddToConstructionList(squad);
                }
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    debugCode = 300;
                    if ( entity == null )
                        continue;
                    if (entity.TypeData.GetHasTag("ArmadaStarbase"))
                    {
                        ArmadaPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>( "ArmadaPerUnitBaseInfo" );
                        Starbases.AddToConstructionList(entity);
                    }
                    if (entity.TypeData.GetHasTag("ArmadaMine"))
                    {
                        ArmadaPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>( "ArmadaPerUnitBaseInfo" );
                        Mines.AddToConstructionList(entity);
                    }
                    if (entity.TypeData.GetHasTag("ArmadaProducer"))
                    {
                        ArmadaPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>( "ArmadaPerUnitBaseInfo" );
                        Producers.AddToConstructionList(entity);
                    }

                    if (entity.TypeData.GetHasTag("ArmadaTransport"))
                    {
                        ArmadaPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>( "ArmadaPerUnitBaseInfo" );
                        Transports.AddToConstructionList(entity);
                    }
                    if (entity.TypeData.GetHasTag("ArmadaTransportDestination"))
                    {
                        ArmadaPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>( "ArmadaPerUnitBaseInfo" );
                        TransportDestinations.AddToConstructionList(entity);
                    }

                    if (entity.TypeData.GetHasTag("ArmadaMine"))
                    {
                        ArmadaPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>( "ArmadaPerUnitBaseInfo" );
                        Mines.AddToConstructionList(entity);
                    }

                    if (entity.TypeData.GetHasTag("ArmadaFlagship") || entity.TypeData.GetHasTag("ArmadaKing"))
                    {
                        ArmadaPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>( "ArmadaPerUnitBaseInfo" );
                        BolsterableFlagships.AddToConstructionList(entity);
                        Flagships.AddToConstructionList(entity);
                    }

                    if (entity.TypeData.GetHasTag("SwarmLure"))
                    {
                        ArmadaPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>( "ArmadaPerUnitBaseInfo" );
                        SwarmLures.AddToConstructionList(entity);
                    }
                    if (entity.TypeData.GetHasTag("ArmadaLocust"))
                    {
                        Locusts.AddToConstructionList(entity);
                        ArmadaPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>( "ArmadaPerUnitBaseInfo" );
                        if ( data.LocustDestination != null )
                        {
                            LocustsPerLure.Construction[data.LocustDestination]++;
                            TotalHopsForLocustsPerLure.Construction[data.LocustDestination] += data.LocustDestination.GetHopsTo(entity.Planet);
                        }
                    }

                    if (entity.TypeData.GetHasTag("SwarmLauncher"))
                    {
                        SwarmLaunchers.AddToConstructionList(entity);
                    }

                    if (entity.TypeData.GetHasTag("ProducesRangers") )
                    {
                        RangerProducers.AddToConstructionList(entity);
                    }
                    if (entity.TypeData.GetHasTag("ArmadaRanger"))
                    {
                        ArmadaPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>( "ArmadaPerUnitBaseInfo" );
                        if ( data.HomeStarbaseSafe.GetSquad() == null &&
                             data.HomeStarbase.GetSquad() != null)
                        {
                            data.HomeStarbaseSafe = SafeSquadWrapper.Create(data.HomeStarbase.GetSquad());
                        }
                        if (data.HomeStarbaseSafe.GetSquad() != null)
                        {
                            RangersPerStarbase.Construction[data.HomeStarbaseSafe]++;
                        }
                        RangerCount.Construction++;
                        RangersPerPlanet.Construction[entity.Planet]++;
                    }
                    if (entity.TypeData.GetHasTag("ArmadaDireRanger"))
                    {
                        ArmadaPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>( "ArmadaPerUnitBaseInfo" );
                        if ( data.HomeStarbaseSafe.GetSquad() == null &&
                             data.HomeStarbase.GetSquad() != null)
                        {
                            data.HomeStarbaseSafe = SafeSquadWrapper.Create(data.HomeStarbase.GetSquad());
                        }

                        if ( data.HomeStarbaseSafe.GetSquad() != null )
                            DireRangersPerStarbase.Construction[data.HomeStarbaseSafe]++;
                        DireRangerCount.Construction++;
                        RangersPerPlanet.Construction[entity.Planet]++;
                    }
                    if ( entity.TypeData.GetHasTag( "BoostsRangerCap" ) &&
                         entity.SelfBuildingMetalRemaining <= 0 && //not under construction
                         entity.SecondsSpentAsRemains <= 0 ) //not remains
                    {
                        ArmadaPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>("ArmadaPerUnitBaseInfo");
                        if ( entity.SelfBuildingMetalRemaining > 0 )
                            continue;

                        RangerOutposts.AddToConstructionList( entity );
                    }
                    if ( entity.TypeData.GetHasTag( "BoostsDireRangerCap" ) &&
                         entity.SelfBuildingMetalRemaining <= 0 && //not under construction
                         entity.SecondsSpentAsRemains <= 0 ) //not remains
                    {
                        ArmadaPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>("ArmadaPerUnitBaseInfo");
                        if ( entity.SelfBuildingMetalRemaining > 0 )
                            continue;

                        DireRangerOutposts.AddToConstructionList( entity );
                    }

                    if ( entity.TypeData.GetHasTag( "AutoDefenseShip" ) )
                    {
                        Rangers.AddToConstructionList(entity);
                        ArmadaPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>( "ArmadaPerUnitBaseInfo" );
                        GameEntity_Squad HomeStarbase = data.HomeStarbase.GetSquad();
                        if ( entity.PlanetFaction.Faction != HomeStarbase?.PlanetFaction.Faction ) {
                            HomeStarbase = null;
                            data.HomeStarbase.Clear();
                        }
                        if ( HomeStarbase == null ) {
                            entity.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut ); //if we've lost our Starbase, we die
                        }
                    }
                    if (entity.TypeData.GetHasTag("ArmadaTransport") )
                    {
                        ArmadaPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>( "ArmadaPerUnitBaseInfo" );
                        Transports.AddToConstructionList(entity);
                    }

                    if ( entity.TypeData.GetHasTag( "ArmadaShipyard" ) )
                        NumShipyards++;
                    if ( entity.SelfBuildingMetalRemaining > 0 ||
                         entity.SecondsSpentAsRemains > 0 )
                        continue; //if this building is under construction, it doesn't grant any bonuses
                    //Update the District levels

                    
                    // DLC3GameEntityTypeDataExtension entity_DLC3TypeData = entity.TypeData.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );
                    // if ( entity_DLC3TypeData != null &&
                    //      (entity_DLC3TypeData.BonusSkeletonPercent > 0 ||
                    //       entity_DLC3TypeData.BonusWightPercent > 0 ||
                    //       entity_DLC3TypeData.BonusMummyPercent > 0 ) )
                    // {
                    //     //We can't bail out here, since this structure might grant other things to the dyson as well (like Skeleton Lord Homes)
                    //     StructuresGrantingBonuses.Add(entity);
                    // }
                    FleetMembership mem = entity.FleetMembership;
                    if ( mem == null )
                        continue;
                    Fleet unknownFleet = mem.Fleet;
                    if ( unknownFleet == null )
                        continue;

                    debugCode = 310;

                    // if ( entity_DLC3TypeData == null ) {
                    //     continue;
                    // }

                    Fleet mobileCityFedFleet = null;
                    switch (unknownFleet.Category )
                    {
                        case FleetCategory.PlayerCustomCity:
                          mobileCityFedFleet = unknownFleet.GetFleetBolsteredByThisCity();
                            break;
                        case FleetCategory.PlayerCustomCityFedMobile:
                            mobileCityFedFleet = unknownFleet;
                            break;
                    }
                    ArmadaMobileFleetBaseInfo armadaMobileFleetInfo = mobileCityFedFleet?.TryGetExternalBaseInfoAs<ArmadaMobileFleetBaseInfo>();
                    if ( armadaMobileFleetInfo == null )
                        continue;

                    debugCode = 340;

                }

                Starbases.SwitchConstructionToDisplay();
                UnownedStarbases.SwitchConstructionToDisplay();
                Mines.SwitchConstructionToDisplay();
                Producers.SwitchConstructionToDisplay();
                Transports.SwitchConstructionToDisplay();
                TransportDestinations.SwitchConstructionToDisplay();
                BolsterableFlagships.SwitchConstructionToDisplay();
                Flagships.SwitchConstructionToDisplay();
                SwarmLures.SwitchConstructionToDisplay();
                SwarmLaunchers.SwitchConstructionToDisplay();
                Locusts.SwitchConstructionToDisplay();
                Rangers.SwitchConstructionToDisplay();
                RangerProducers.SwitchConstructionToDisplay();
                RangerOutposts.SwitchConstructionToDisplay();
                DireRangerOutposts.SwitchConstructionToDisplay();
                RangerCount.SwitchConstructionToDisplay();
                RangersPerStarbase.SwitchConstructionToDisplay();
                DireRangersPerStarbase.SwitchConstructionToDisplay();
                RangersPerPlanet.SwitchConstructionToDisplay();
                DireRangerCount.SwitchConstructionToDisplay();

                LocustsPerLure.SwitchConstructionToDisplay();
                TotalHopsForLocustsPerLure.SwitchConstructionToDisplay();
                
                debugCode = 350;

                //UpdateDistrictAndFlagshipLevels();

                debugCode = 400;
                AvailableBlueprints.ClearConstructionListForStartingConstruction();
                for ( int i = 0; i < this.ArmadaCompletedUpgrades.Count; i++ ) {
                    ArmadaUpgrade upgrade = this.ArmadaCompletedUpgrades[i];
                    if ( upgrade.Type == ArmadaUpgradeType.ClaimBlueprints )
                    {
                        AvailableBlueprints.AddToConstructionList(upgrade);
                    }
                }
                AvailableBlueprints.SwitchConstructionToDisplay();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception during armada stage 2 debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            AllegianceHelper.AllyThisFactionToHumans( AttachedFaction );

            HandleTransportArrival(Context);
            // HandleMetalIncome();
            // HandleScienceIncome();
            // HandleResourceOneIncome();
            // HandleHackingIncome();
            // HandleEnemyIncome();
            // HandleTransportArrival(Context);
            // LoadOrUnloadFlagships( Context );
            // UpdateFleetState(Context);
        }
        private void HandleTransportArrival(ArcenClientOrHostSimContextCore Context)
        {
            foreach ( GameEntity_Squad transport in Transports.DisplaySquads() )
            {
                List<SafeSquadWrapper> dests = this.TransportDestinations.GetDisplayList();
                for ( int i = 0; i < dests.Count; i++ )
                {
                    GameEntity_Squad potentialDest = dests[i].GetSquad();
                    if ( potentialDest == null )
                        continue;
                    if ( transport.Planet != potentialDest.Planet )
                        continue;
                    int distance = 250;
                    if ( potentialDest.TypeData.IsMobile)
                        distance = 2500;
                    if ( Mat.DistanceBetweenPointsImprecise( transport.WorldLocation, potentialDest.WorldLocation ) < distance )
                    {
                        ArmadaPerUnitBaseInfo data = transport.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>( "ArmadaPerUnitBaseInfo" );
                        this.AttachedFaction.StoredMetal += data.MetalTransported;
                        if ( data.ScienceTransported > 0 )
                            this.AttachedFaction.StoredScience += data.ScienceTransported;
                        if ( data.HackingTransported > 0 )
                            this.AttachedFaction.StoredHacking += data.HackingTransported;
                        if ( data.TiberiumTransported > 0 )
                            this.AttachedFaction.StoredFactionResourceOne += data.TiberiumTransported;
                        transport.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                    }
                }
            }
        }

        public bool DoesPlanetHaveStarbase( Planet planet )
        {
            bool foundStarbase = false;
            foreach ( GameEntity_Squad city in this.Starbases.DisplaySquads() )
            {
                if ( city.Planet == planet ) {
                    foundStarbase = true;
                    break;
                }
            }
            if ( foundStarbase )
                return foundStarbase;
            foreach ( GameEntity_Squad city in this.UnownedStarbases.DisplaySquads() )
            {
                if ( city.Planet == planet ) {
                    foundStarbase = true;
                    break;
                }
            }

            return foundStarbase;
        }
        public bool DoesPlanetHaveLure( Planet planet )
        {
            bool foundLure = false;
            foreach ( GameEntity_Squad lure in this.SwarmLures.DisplaySquads() )
            {
                if ( lure.Planet == planet ) {
                    foundLure = true;
                    break;
                }
            }
            return foundLure;
        }

        public int IsPlanetMiningEligible( Planet planet )
        {
            if ( this.MiningIneligiblePlanets.ContainsKey( planet ) )
                return this.MiningIneligiblePlanets[planet];
            return -1;
        }

        public int GetMiningCooldownSecondsRemaining( Planet planet )
        {
            int eligibleTime = IsPlanetMiningEligible( planet );
            if ( eligibleTime == -1 )
                return 0;
            return Math.Max( 0, eligibleTime - World_AIW2.Instance.GameSecond );
        }
        
        #region TotalMarkLevelForAllStarbases
        public int TotalMarkLevelForAllStarbases()
        {
            int total = 0;
            foreach ( GameEntity_Squad city in this.Starbases.DisplaySquads() )
            {
                total += city.CurrentMarkLevel;
            }
            return total;
        }
        #endregion

        #region DoOnLocalStartNonSimUpdates_OnMainThread
        public override void DoOnLocalStartNonSimUpdates_OnMainThread( ArcenClientOrHostSimContextCore Context )
        {
        }
        #endregion

        #region CheckIfPlayerFactionShouldGetRewardBasedOnAUnitDying
        /// <summary>
        /// This is called on every player faction when any unit is killed, period.  It identifies who the killing faction is, and allows for custom logic to be run.
        /// Many times, this will be utterly unrelated to anything the player faction needs to do.  But if the player faction is "the strongest faction of type X on that planet where the thing died,"
        /// for instance, then this is a method where that sort of thing can be calculated and then some reward can be granted.
        /// Note that the armada's logic here is different because the killing faction can be null
        /// </summary>
        public override void CheckIfPlayerFactionShouldGetRewardBasedOnAUnitDying( Faction factionThatKilledEntityOrNull, bool IsFromOnlyPartOfStackDying, GameEntity_Squad entity,
            DamageSource Damage, EntitySystem FiringSystemOrNull, int numExtraStacksKilled, ArcenHostOnlySimContext Context )
        {
            FInt multiplier = FInt.One;
            bool debug = false;
            if ( this.GetShouldThisArmadaFactionGetARewardBasedOnThisKill( factionThatKilledEntityOrNull, entity, ref multiplier, Context ) )
            {
                DLC3GameEntityTypeDataExtension entity_DLC3TypeData = entity.TypeData.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );
                if ( debug ) {
                    int bonusMultiplier = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "BonusArmadaResources" );
                    ArcenDebugging.ArcenDebugLogSingleLine( "\t We get resources; multiplier " + multiplier + " (bonus multiplier component: " + bonusMultiplier +")" , Verbosity.DoNotShow );
                }

                if ( entity_DLC3TypeData != null )
                {
                    // FInt scienceToGrantOnDeath = FInt.Zero;
                    // FInt hackingToGrantOnDeath = FInt.Zero;
                    // FInt metalToGrantOnDeath = FInt.Zero;
                    // FInt resourceOneToGrantOnDeath = FInt.Zero;
                    entity_DLC3TypeData.GetArmadaResourcesToGrantOnDeath(entity,
                        out FInt scienceToGrantOnDeath, out FInt hackingToGrantOnDeath, out FInt metalToGrantOnDeath, out FInt resourceOneToGrantOnDeath);
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

                    if ( metalToGrantOnDeath > FInt.Zero )
                    {
                        metalToGrantOnDeath *= multiplier;
                        if ( metalToGrantOnDeath < FInt.One )
                            metalToGrantOnDeath = FInt.One;
                        this.AttachedFaction.StoredScience += metalToGrantOnDeath;
                    }

                    if ( resourceOneToGrantOnDeath > FInt.Zero )
                    {
                        resourceOneToGrantOnDeath *= multiplier;
                        this.AttachedFaction.StoredFactionResourceOne += resourceOneToGrantOnDeath;
                    }
                }
            }
        }
        #endregion
        #region GetShouldThisArmadaFactionGetARewardBasedOnThisKill
        private bool GetShouldThisArmadaFactionGetARewardBasedOnThisKill( Faction killingFactionOrNull, GameEntity_Squad entity, ref FInt multiplier, ArcenHostOnlySimContext Context )
        {
            //A bunch of rules; fundamentally, the rules are:
            //if we killed the unit, or were part of the fight, we get full resources
            //If this was killed by an Allied NPC faction, we get partial resources
            //If this was killed by another armada (and we weren't in the fight), we get nothing
            //If this was killed by an allied, non-armada faction we get partial resources
            FInt resourceMultiplierForAlliedHumanKill = FInt.FromParts( 0, 300 );
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
                if ( ArmadaFactionBaseInfo.GetIsThisAnArmadaFaction( killingFactionOrNull ) )
                    return false; //if another necromancer killed this, we get nothing
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
            return false; //if this specific necromancer doesn't get these resources, someone else still might

        }
        #endregion


        #region GetCustomHackingHistory
        /// <summary>
        /// If we want a given player faction to have a custom hacking history in place of the regular one, then we can write this here and return true.
        /// If we want it to have ADDED hacking history in addition to the regular one, we can write it here and then return false.
        /// </summary>
        public override bool GetCustomHackingHistory( ArcenDoubleCharacterBuffer Buffer )
        {
            //Show the armada upgrade history here.
            if ( this.ArmadaHistory.Count == 0 )
            {
                Buffer.Add( "There is no history" );
                return false;
            }
            for ( int i = this.ArmadaHistory.Count - 1; i >= 0; i-- )
            {
                ArmadaUpgradeEvent nEvent = this.ArmadaHistory[i];
                Buffer.Add( nEvent.ToDebugString );
            }
            return true; //return true so it does not also show the local hacking history
        }
        #endregion

        #region IsPlanetEligibleForStarbase
        public bool IsPlanetEligibleForStarbase( Planet planet, out bool isOnStarbasePlanet )
        {
            isOnStarbasePlanet = false;
            try
            {
                int fewestHops = -1;
                foreach ( GameEntity_Squad city in this.Starbases.DisplaySquads() )
                {
                    int hops = city.Planet.GetHopsTo( planet );
                    if ( hops < fewestHops || fewestHops == -1 )
                        fewestHops = hops;
                }
                if ( fewestHops == 0 )
                    isOnStarbasePlanet = true;

                if ( fewestHops < this.RequiredHopsBetweenStarbases )
                    return false;

            }
            catch //(Exception e)
            {
                //this can race with the sim code, so just assume that it's bad
                return false;
            }
            return true;
        }
        #endregion

        public static int GetArmadaFactionCount()
        {
            int count = 0;
            PlayerTypeData playerType;
            
            playerType = PlayerTypeDataTable.Instance.GetRowByName( "ArmadaEmpire", LookupSwapAllowed.Yes, true );
            if (playerType != null)
                count += playerType.CurrentFactionsInThisGame.Count;
            
            return count;
        }

        public static bool GetIsThisAnArmadaFaction( Faction fac )
        {
            if ( fac == null )
                return false;
            if ( fac.Type != FactionType.Player )
                return false;
            PlayerTypeData playerTypeData = fac.PlayerTypeDataOrNull_ModeratelyExpensive;
            if ( playerTypeData == null )
                return false;
            switch (playerTypeData.InternalName)
            {
                case "ArmadaEmpire":
                    return true; 
            }
            return false;
        }

        public static Faction GetFirstArmadaFactionOrNull()
        {
            foreach ( PlayerTypeData playerType in PlayerTypeDataTable.Instance.Rows )
            {
                if ( playerType.CurrentFactionsInThisGame.Count <= 0 )
                    continue;
                switch ( playerType.InternalName )
                {
                    case "ArmadaEmpire":
                        foreach ( Faction fac in playerType.CurrentFactionsInThisGame )
                        {
                            return fac;
                        }
                        break;
                }
            }
            return null;
        }

        #region UpdatePowerLevel
        public override void UpdatePowerLevel()
        {
            bool debug = false;
            FInt powerLevelSoFar = FInt.Zero;

            FInt totalStrength = FInt.Zero;
            FInt totalFlagshipMarkLevel = FInt.Zero;
            bool foundMarkVFlagship = false;
            bool foundMarkVStarbase = false;

            foreach ( Fleet fleet in World_AIW2.Instance.Fleets( this.AttachedFaction, FleetStatus.CenterpieceMustLiveOrLooseFleet ) )
            {
                GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                if ( centerpiece == null )
                    continue;
                if ( centerpiece.PlanetFaction.Faction != this.AttachedFaction )
                    continue;
                if ( centerpiece.GetMatches_SemiSlow( EntityRollupType.MobileFleetFlagships ) ||
                     centerpiece.GetMatches_SemiSlow( EntityRollupType.MobileCombatFlagships ) )
                {
                    totalStrength += fleet.GetMaxStrengthOfFleet_ForUIOnly( false );
                }
                if ( centerpiece.CurrentMarkLevel >= 5 )
                {
                    if ( centerpiece.TypeData.IsMobile )
                        foundMarkVFlagship = true;
                    else
                        foundMarkVStarbase = true;
                }
            }
            if ( debug )
                ArcenDebugging.LogSingleLine("total strength: " + totalStrength + " total flagships: " + this.Flagships.Count , Verbosity.DoNotShow );
            totalStrength /= 1000;
            FInt totalFleetStrengthComponent = FInt.Zero;
            if ( totalStrength > 400 )
            {
                FInt strengthUnits = (totalStrength - 400) / 100;
                if (debug )
                    ArcenDebugging.LogSingleLine("strengthUnits "+ strengthUnits, Verbosity.DoNotShow );
                totalFleetStrengthComponent = FInt.FromParts(0, 050) * strengthUnits;
            }
            if ( totalFleetStrengthComponent > FInt.FromParts( 0, 700 ) )
            {
                if ( debug )
                    ArcenDebugging.LogSingleLine("lowering fleet strength component from " + totalFleetStrengthComponent , Verbosity.DoNotShow );
                
                totalFleetStrengthComponent = FInt.FromParts( 0, 700 );
            }
            powerLevelSoFar += totalFleetStrengthComponent;

            FInt powerLevelPerFlagship = FInt.FromParts(0, 080 );
            FInt powerLevelFromFlagships = powerLevelPerFlagship * this.Flagships.Count;
            powerLevelSoFar += powerLevelFromFlagships;
            
            if ( foundMarkVStarbase )
            {
                if ( debug )
                    ArcenDebugging.LogSingleLine("Adding 0.1 for a mark 5 necropolis", Verbosity.DoNotShow );
                powerLevelSoFar += FInt.FromParts(0, 100 );
            }
            if ( foundMarkVFlagship )
            {
                if ( debug )
                    ArcenDebugging.LogSingleLine("Adding 0.1 for a mark 5 flagship", Verbosity.DoNotShow );
                powerLevelSoFar += FInt.FromParts(0, 100 );
            }
            this.AttachedFaction.OverallPowerLevel = powerLevelSoFar;
            if ( debug )
                ArcenDebugging.LogSingleLine("Got power level " + this.AttachedFaction.OverallPowerLevel + " fleet component " + totalFleetStrengthComponent + " flagship " + powerLevelFromFlagships, Verbosity.DoNotShow );
        }
        #endregion
    }
}
