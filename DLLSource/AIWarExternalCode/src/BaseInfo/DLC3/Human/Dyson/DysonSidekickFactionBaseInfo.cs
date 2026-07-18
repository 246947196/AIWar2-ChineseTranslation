using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DysonSidekickFactionBaseInfo : ExternalFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {
        //Serialized
        public readonly List<DysonUpgradeEvent> DysonHistory = List<DysonUpgradeEvent>.Create_WillNeverBeGCed( 10, "Faction-DysonUpgradeHistory" );
        public readonly List<DysonUpgrade> DysonCompletedUpgrades = List<DysonUpgrade>.Create_WillNeverBeGCed( 90, "DysonFactionBaseInfo-DysonCompletedUpgrades" ); //this is filled out from the DysonCompletedUpgradeIndices in the dysonmancer faction code
        public int StrengthForNextEnemyAttack;
        public int TimeForNextEnemyAttack;
        public int PlanetsDrilled;
        public bool SphereOnline;
        public bool SphereActivationTriggered;
        //Non-Serialized
        public FInt ScienceIncomeLastSecond;
        public FInt MetalIncomeLastSecond;
        public FInt HackingIncomeLastSecond;
        public FInt ResourceOneIncomeLastSecond;
        public int EnemyIncomeLastSecond;
        public int FlagshipTierLevel;
        public bool HasKing;
        public readonly DoubleBufferedValue<int> GuardianCount = new DoubleBufferedValue<int>( 0 );
        public readonly DoubleBufferedValue<int> DireGuardianCount = new DoubleBufferedValue<int>( 0 );
        public static DysonSidekickFactionBaseInfo Instance; //there can only ever be one of this faction at a time
        public DysonSidekickDifficulty Difficulty;
        public DysonSidekickIncome Income;
        //this gets updated during the process of sim steps, so had to be converted to DoubleBufferedConcurrentLists.  This is less performant
        //than DoubleBufferedList, but won't have cross-threading issues from an activity like that
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> Strongholds = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Dyson-Strongholds" );
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> Moons = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Dyson-Moons" );
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> Shipyards = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Dyson-Shipyars" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Flagships = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Dyson-Flagships" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Guardians = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Dyson-Guardians" );
        public readonly DoubleBufferedList<SafeSquadWrapper> GuardianProducers = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Dyson-GuardianProducers" );
        public readonly DoubleBufferedList<SafeSquadWrapper> GuardianBoosters = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Dyson-GuardianBoosters" );
        public readonly DoubleBufferedList<SafeSquadWrapper> DireGuardianBoosters = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Dyson-DireGuardianBoosters" );
        public readonly DoubleBufferedList<SafeSquadWrapper> BolsterableFlagships = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Dyson-BolsterableFlagships" );
        public readonly DoubleBufferedList<SafeSquadWrapper> DrillsAndOverloaders = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Dyson-DrillsAndOverloaders" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Spheres = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed(20, "Dyson-Spheres");

        public readonly DoubleBufferedList<SafeSquadWrapper> TransportDestinations = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed(20, "Dyson-TransportDestinations");

        public readonly DoubleBufferedList<SafeSquadWrapper> Transports = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed(20, "Dyson-Transports");
        public readonly DoubleBufferedList<DysonUpgrade> AvailableBlueprints = DoubleBufferedList<DysonUpgrade>.Create_WillNeverBeGCed( 20, "Dyson-AvailableBlueprints" );

        public readonly DoubleBufferedList<Planet> RavagedPlanets = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 20, "Dyson-RavagedPlanets" );
        private readonly List<SafeSquadWrapper> RemoteShips = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "DysonSidekickFactionBaseInfo-RemoteShips" ); //this is filled out from the DysonCompletedUpgradeIndices in the dyson faction code
        private readonly List<SafeSquadWrapper> WorkingList = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "DysonSidekickFactionBaseInfo-WorkingList" ); //this is filled out from the DysonCompletedUpgradeIndices in the dyson faction code

        //Some of these are used in the UI code
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> AllResourceGenerators = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "DysonSidekickFactionBaseInfo-AllResourceGenerators" );
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> MetalGenerators = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "DysonSidekickFactionBaseInfo-MetalGenerators" );
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> ResourceOneGenerators = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "DysonSidekickFactionBaseInfo-ResourceOneGenerators" );
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> ScienceGenerators = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "DysonSidekickFactionBaseInfo-ScienceGenerators" );
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> HackingGenerators = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "DysonSidekickFactionBaseInfo-HackingGenerators" );

        public readonly DoubleBufferedDictionary<SafeSquadWrapper, int> GuardiansPerStronghold = DoubleBufferedDictionary<SafeSquadWrapper, int>.Create_WillNeverBeGCed( 20, "DysonSidekickFactionBaseInfo-GuardiansPerStronghold" );
        public readonly DoubleBufferedDictionary<SafeSquadWrapper, int> DireGuardiansPerStronghold = DoubleBufferedDictionary<SafeSquadWrapper, int>.Create_WillNeverBeGCed( 20, "DysonSidekickFactionBaseInfo-DireGuardiansPerStronghold" );
        public readonly DoubleBufferedDictionary<Planet, int> GuardiansPerPlanet = DoubleBufferedDictionary<Planet, int>.Create_WillNeverBeGCed( 500, "DysonSidekickFactionBaseInfo-GuardiansPerPlanet" );

        public int BonusStartingResources = 0;
        public int IncomeModifier = 0;
        public int Intensity = 0;
        public bool StartWithAllUpgrades = false;
        public int NumShipyards = 0;
        public int SpireDistrictTier = 0;
        public int ZenithDistrictTier = 0;
        public int NeinzulDistrictTier = 0;
        public int TemplarDistrictTier = 0;

        public readonly int RequiredHopsBetweenStrongholds = 1;

        public DysonSidekickFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            SphereOnline = false;
            SphereActivationTriggered = false;
            Strongholds.Clear();
            Moons.Clear();
            Shipyards.Clear();
            Flagships.Clear();
            Guardians.Clear();
            GuardianProducers.Clear();
            GuardianBoosters.Clear();
            DireGuardianBoosters.Clear();
            BolsterableFlagships.Clear();
            DrillsAndOverloaders.Clear();
            Spheres.Clear();
            TransportDestinations.Clear();
            Transports.Clear();
            NumShipyards = 0;
            IncomeModifier = 0;
            Intensity = 0;
            SpireDistrictTier = 0;
            ZenithDistrictTier = 0;
            NeinzulDistrictTier = 0;
            TemplarDistrictTier = 0;
            StartWithAllUpgrades = false;
            StrengthForNextEnemyAttack = 0;
            PlanetsDrilled = 0;
            TimeForNextEnemyAttack = 0;
            EnemyIncomeLastSecond = 0;
            ScienceIncomeLastSecond = FInt.Zero;
            MetalIncomeLastSecond = FInt.Zero;
            ResourceOneIncomeLastSecond = FInt.Zero;
            HackingIncomeLastSecond = FInt.Zero;
            FlagshipTierLevel = 0;
            HasKing = false;
            GuardianCount.Clear();
            DireGuardianCount.Clear();
            DysonHistory.Clear();
            DysonCompletedUpgrades.Clear();
            WorkingList.Clear();
            AllResourceGenerators.Clear();
            MetalGenerators.Clear();
            ResourceOneGenerators.Clear();
            ScienceGenerators.Clear();
            HackingGenerators.Clear();
            RavagedPlanets.Clear();
            GuardiansPerStronghold.Clear();
            DireGuardiansPerStronghold.Clear();
            GuardiansPerPlanet.Clear();
            Difficulty = null;
            Income = null;
            Instance = null;
        }

        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "DysonSidekickFactionBaseInfo" );

            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.DysonCompletedUpgrades.Count, "DysonCompletedUpgrades" );
            for ( int i = 0; i < this.DysonCompletedUpgrades.Count; i++ )
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.DysonCompletedUpgrades[i].Index, "DysonCompletedUpgradeIndex" );

            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.DysonHistory.Count, "DysonHistory.Count" );
            for ( int i = 0; i < this.DysonHistory.Count; i++ )
                this.DysonHistory[i].SerializeTo( MetaData, Buffer, SerializationCmdType );
            if (!SerializationCmdType.GetIsNetworkType())
            {
                Buffer.AddInt32(MetaData, ReadStyle.NonNeg, this.StrengthForNextEnemyAttack, "StrengthForNextEnemyAttack");
                Buffer.AddInt32(MetaData, ReadStyle.NonNeg, this.TimeForNextEnemyAttack, "TimeForNextEnemyAttack");
            }
            Buffer.AddInt32(MetaData, ReadStyle.NonNeg, this.PlanetsDrilled, "PlanetsDrilled");
            Buffer.AddBool( MetaData, this.SphereOnline );
            Buffer.AddBool( MetaData, this.SphereActivationTriggered );
        }
        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "DysonSidekickFactionBaseInfo" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "DysonSidekickFactionBaseInfo Ext", TrackerStyle.ByTypeOnly );

            int countToExpect = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "DysonCompletedUpgrades" );
            this.DysonCompletedUpgrades.Clear();
            for ( int i = 0; i < countToExpect; i++ )
                this.DysonCompletedUpgrades.Add( DysonUpgradeTable.Instance.GetRowByIndex( (Int32)Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "DysonCompletedUpgradeIndex" ) ) );

            countToExpect = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "DysonHistory.Count" );
            this.DysonHistory.DeserializeUncertainNumberOfEntriesIntoExistingList( countToExpect,
                delegate { return DysonUpgradeEvent.GetFromPoolOrCreate(); },
                delegate ( DysonUpgradeEvent Event ) { Event.DeserializedIntoSelf( MetaData, Buffer, SerializationCmdType ); } );
            if (!SerializationCmdType.GetIsNetworkType())
            {
                this.StrengthForNextEnemyAttack = Buffer.ReadInt32(MetaData, ReadStyle.NonNeg, "StrengthForNextEnemyAttack");
                this.TimeForNextEnemyAttack = Buffer.ReadInt32(MetaData, ReadStyle.NonNeg, "TimeForNextEnemyAttack");
            }
            this.PlanetsDrilled = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "PlanetsDrilled" );
            SphereOnline = Buffer.ReadBool( MetaData );
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 541 ) )
                this.SphereActivationTriggered = Buffer.ReadBool( MetaData );
            else
                this.SphereActivationTriggered = false;
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( "130 来自戴森球人类的负载" );
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
                BonusStartingResources = cfg.GetIntValueForCustomFieldOrDefaultValue("DysonStartingResources", true);
                IncomeModifier = cfg.GetIntValueForCustomFieldOrDefaultValue("DysonSidekickIncome", true);
                Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue("DysonSidekickIntensity", true);
                StartWithAllUpgrades = cfg.GetBoolValueForCustomFieldOrDefaultValue("DysonUnlockAllTechs", true);
                debugCode = 300;
                if ( DysonSidekickDifficultyTable.Instance == null)
                {
                    debugCode = 310;
                    ArcenDebugging.ArcenDebugLogSingleLine("null instance of diff table", Verbosity.DoNotShow );
                }
                Difficulty = DysonSidekickDifficultyTable.Instance.GetRowByIntensity(this.Intensity, this.AttachedFaction);
                debugCode = 400;
                Income = DysonSidekickIncomeTable.Instance.GetRowByIntensity(this.IncomeModifier, this.AttachedFaction);
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
        public int GetHighestStrongholdMarkLevel()
        {
            int highestLevel = 0;
            foreach ( GameEntity_Squad city in this.Strongholds.DisplaySquads() )
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

        public bool HasAdequateDistrictsToBuild(GameEntityTypeData buildingInfo)
        {
            bool debug = false;
            if ( buildingInfo == null )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("path A: null buildingInfo", Verbosity.DoNotShow );
                return false;
            }
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine("Checking whether we can build " + buildingInfo.GetDisplayName(), Verbosity.DoNotShow );
            if ( this.StartWithAllUpgrades )
                return true;

            // OR logic: if ANY racial gate is satisfied, the entity is buildable.
            // This lets hybrids unlock via either the primary race (district 3) or
            // the secondary race (district 2) without requiring both simultaneously.
            bool hasAnyGate = false;

            if (buildingInfo.GetHasTag("GatedBySpireDistrict"))
            {
                hasAnyGate = true;
                bool spireOk = true;
                if (this.SpireDistrictTier <= 2 && buildingInfo.GetHasTag("RequiresSpireDistrictThree"))
                    spireOk = false;
                if (this.SpireDistrictTier <= 1 && buildingInfo.GetHasTag("RequiresSpireDistrictTwo"))
                    spireOk = false;
                if (spireOk)
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("Can build via spire district", Verbosity.DoNotShow );
                    return true;
                }
            }
            if (buildingInfo.GetHasTag("GatedByZenithDistrict"))
            {
                hasAnyGate = true;
                bool zenithOk = true;
                if (this.ZenithDistrictTier <= 2 && buildingInfo.GetHasTag("RequiresZenithDistrictThree"))
                    zenithOk = false;
                if (this.ZenithDistrictTier <= 1 && buildingInfo.GetHasTag("RequiresZenithDistrictTwo"))
                    zenithOk = false;
                if (zenithOk)
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("Can build via zenith district", Verbosity.DoNotShow );
                    return true;
                }
            }
            if (buildingInfo.GetHasTag("GatedByNeinzulDistrict"))
            {
                hasAnyGate = true;
                bool neinzulOk = true;
                if (this.NeinzulDistrictTier <= 2 && buildingInfo.GetHasTag("RequiresNeinzulDistrictThree"))
                    neinzulOk = false;
                if (this.NeinzulDistrictTier <= 1 && buildingInfo.GetHasTag("RequiresNeinzulDistrictTwo"))
                    neinzulOk = false;
                if (neinzulOk)
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("Can build via neinzul district", Verbosity.DoNotShow );
                    return true;
                }
            }
            if (buildingInfo.GetHasTag("GatedByTemplarDistrict"))
            {
                hasAnyGate = true;
                bool templarOk = true;
                if (this.TemplarDistrictTier <= 2 && buildingInfo.GetHasTag("RequiresTemplarDistrictThree"))
                    templarOk = false;
                if (this.TemplarDistrictTier <= 1 && buildingInfo.GetHasTag("RequiresTemplarDistrictTwo"))
                    templarOk = false;
                if (templarOk)
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("Can build via templar district", Verbosity.DoNotShow );
                    return true;
                }
            }
            if ( hasAnyGate )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("Cannot build: no district requirements met", Verbosity.DoNotShow );
                return false;
            }
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine("Can build", Verbosity.DoNotShow );
            return true;
        }
        #region GetDysonStateForDisplay
        public void GetDysonStateForDisplay( ArcenDoubleCharacterBuffer buffer )
        {
            //For debug, this goes in the Threat menu
            buffer.Add( "\n" );
            buffer.Add( "我们当前拥有 " ).Add( this.DysonCompletedUpgrades.Count ).Add( " 个阵营升级：\n" );
            for ( int i = 0; i < this.DysonCompletedUpgrades.Count; i++ )
            {
                buffer.Add( "\t" ).Add( this.DysonCompletedUpgrades[i].ToString() ).Add( "\n" );
            }
            buffer.Add( "舰队升级：\n" );
            foreach ( Fleet fleet in World_AIW2.Instance.Fleets( this.AttachedFaction, FleetStatus.CenterpieceMustLiveOrLooseFleet ) )
            {
                if ( fleet == null || fleet.Centerpiece.GetSquad() == null )
                    continue;
                DysonSidekickMobileFleetBaseInfo necroMobileInfo = fleet.TryGetExternalBaseInfoAs<DysonSidekickMobileFleetBaseInfo>();
                if ( necroMobileInfo == null ) //wrong kind of fleet
                    continue;
                if ( necroMobileInfo.DysonCompletedUpgrades.Count > 0 )
                {
                    buffer.Add( fleet.GetName() ).Add( ":\n" );
                    for ( int i = 0; i < necroMobileInfo.DysonCompletedUpgrades.Count; i++ )
                    {
                        DysonUpgrade upgrade = necroMobileInfo.DysonCompletedUpgrades[i];
                        buffer.Add( "\t" ).Add( upgrade.ToString() ).Add( "\n" );
                    }
                }
            }
            buffer.Add( "我们拥有 " ).Add( this.AttachedFaction.StoredFactionResourceOne, "a1ffa1" ).Add( " 精华。\n" );
        }
        #endregion

        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                Strongholds.ClearConstructionListForStartingConstruction();
                Moons.ClearConstructionListForStartingConstruction();
                Shipyards.ClearConstructionListForStartingConstruction();
                Flagships.ClearConstructionListForStartingConstruction();
                Guardians.ClearConstructionListForStartingConstruction();
                GuardianProducers.ClearConstructionListForStartingConstruction();
                GuardianBoosters.ClearConstructionListForStartingConstruction();
                DireGuardianBoosters.ClearConstructionListForStartingConstruction();
                GuardianCount.ClearConstructionValueForStartingConstruction();
                DireGuardianCount.ClearConstructionValueForStartingConstruction();

                BolsterableFlagships.ClearConstructionListForStartingConstruction();
                DrillsAndOverloaders.ClearConstructionListForStartingConstruction();
                Spheres.ClearConstructionListForStartingConstruction();
                TransportDestinations.ClearConstructionListForStartingConstruction();
                Transports.ClearConstructionListForStartingConstruction();
                AllResourceGenerators.ClearConstructionListForStartingConstruction();
                MetalGenerators.ClearConstructionListForStartingConstruction();
                ResourceOneGenerators.ClearConstructionListForStartingConstruction();
                HackingGenerators.ClearConstructionListForStartingConstruction();
                RavagedPlanets.ClearConstructionListForStartingConstruction();
                ScienceGenerators.ClearConstructionListForStartingConstruction();
                GuardiansPerStronghold.ClearConstructionDictForStartingConstruction();
                DireGuardiansPerStronghold.ClearConstructionDictForStartingConstruction();
                GuardiansPerPlanet.ClearConstructionDictForStartingConstruction();
                SpireDistrictTier = 1;
                ZenithDistrictTier = 1;
                NeinzulDistrictTier = 1;
                TemplarDistrictTier = 1;
                NumShipyards = 0;
                debugCode = 150;
                if ( Income == null || Intensity == -1 )
                    DoRefreshFromFactionSettings();

                debugCode = 200;
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    debugCode = 300;
                    if ( entity == null )
                        continue;
                    if (entity.TypeData.GetHasTag("DysonStronghold"))
                    {
                        DysonSidekickPerUnitBaseInfo data = entity.CreateExternalBaseInfo<DysonSidekickPerUnitBaseInfo>( "DysonSidekickPerUnitBaseInfo" );
                        Strongholds.AddToConstructionList(entity);
                    }
                    if (entity.TypeData.GetHasTag("DysonMoon"))
                    {
                        DysonSidekickPerUnitBaseInfo data = entity.CreateExternalBaseInfo<DysonSidekickPerUnitBaseInfo>( "DysonSidekickPerUnitBaseInfo" );
                        Moons.AddToConstructionList(entity);
                    }

                    if (entity.TypeData.GetHasTag("GeneratesTemplarGuardians") ||
                        entity.TypeData.GetHasTag("GeneratesTemplarDireGuardiansx") )
                    {
                        GuardianProducers.AddToConstructionList(entity);
                    }
                    if (entity.TypeData.GetHasTag("DysonGuardian"))
                    {
                        DysonSidekickPerUnitBaseInfo data = entity.CreateExternalBaseInfo<DysonSidekickPerUnitBaseInfo>( "DysonSidekickPerUnitBaseInfo" );
                        if ( data.HomeStrongholdSafe.GetSquad() == null &&
                             data.HomeStronghold.GetSquad() != null)
                        {
                            data.HomeStrongholdSafe = SafeSquadWrapper.Create(data.HomeStronghold.GetSquad());
                        }
                        if (data.HomeStrongholdSafe.GetSquad() != null)
                        {
                            GuardiansPerStronghold.Construction[data.HomeStrongholdSafe]++;
                        }
                        GuardianCount.Construction++;
                        GuardiansPerPlanet.Construction[entity.Planet]++;
                    }
                    if (entity.TypeData.GetHasTag("DysonDireGuardian"))
                    {
                        DysonSidekickPerUnitBaseInfo data = entity.CreateExternalBaseInfo<DysonSidekickPerUnitBaseInfo>( "DysonSidekickPerUnitBaseInfo" );
                        if ( data.HomeStrongholdSafe.GetSquad() == null &&
                             data.HomeStronghold.GetSquad() != null)
                        {
                            data.HomeStrongholdSafe = SafeSquadWrapper.Create(data.HomeStronghold.GetSquad());
                        }

                        if ( data.HomeStrongholdSafe.GetSquad() != null )
                            DireGuardiansPerStronghold.Construction[data.HomeStrongholdSafe]++;
                        DireGuardianCount.Construction++;
                        GuardiansPerPlanet.Construction[entity.Planet]++;
                    }

                    if (entity.TypeData.GetHasTag("DysonKing"))
                    {
                        HasKing = true; //for the Empire
                        TransportDestinations.AddToConstructionList( entity );
                        continue;
                    }

                    if ( entity.TypeData.GetHasTag( "DysonShipyard" ) )
                        Shipyards.AddToConstructionList( entity );

                    if ( entity.TypeData.GetHasTag( "BoostsGuardianCap" ) )
                        GuardianBoosters.AddToConstructionList( entity );
                    if ( entity.TypeData.GetHasTag( "BoostsDireGuardianCap" ) )
                        DireGuardianBoosters.AddToConstructionList( entity );

                    if ( entity.TypeData.GetHasTag( "AutoDefenseShip" ) )
                    {
                        Guardians.AddToConstructionList(entity);
                        DysonSidekickPerUnitBaseInfo data = entity.CreateExternalBaseInfo<DysonSidekickPerUnitBaseInfo>( "DysonSidekickPerUnitBaseInfo" );
                        GameEntity_Squad HomeStronghold = data.HomeStronghold.GetSquad();
                        if ( entity.PlanetFaction.Faction != HomeStronghold?.PlanetFaction.Faction ) {
                            HomeStronghold = null;
                            data.HomeStronghold.Clear();
                        }
                        if ( HomeStronghold == null ) {
                            entity.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut ); //if we've lost our castle, we die
                        }
                    }
                    if (entity.TypeData.GetHasTag("DysonFlagship") ||
                         entity.TypeData.GetHasTag("DysonSphereFlagship"))
                    {
                        DysonSidekickPerUnitBaseInfo data = entity.CreateExternalBaseInfo<DysonSidekickPerUnitBaseInfo>( "DysonSidekickPerUnitBaseInfo" );
                        Flagships.AddToConstructionList(entity);
                        if ( !entity.TypeData.GetHasTag("DysonSphereFlagship"))
                            BolsterableFlagships.AddToConstructionList(entity);
                    }
                    if (entity.TypeData.GetHasTag("DysonSidekickSphere") )
                    {
                        DysonSidekickPerUnitBaseInfo data = entity.CreateExternalBaseInfo<DysonSidekickPerUnitBaseInfo>( "DysonSidekickPerUnitBaseInfo" );
                        Spheres.AddToConstructionList(entity);
                        TransportDestinations.AddToConstructionList( entity );
                    }
                    if (entity.TypeData.GetHasTag("DysonSidekickTransport") )
                    {
                        DysonSidekickPerUnitBaseInfo data = entity.CreateExternalBaseInfo<DysonSidekickPerUnitBaseInfo>( "DysonSidekickPerUnitBaseInfo" );
                        Transports.AddToConstructionList(entity);
                    }

                    if (entity.TypeData.GetHasTag("DysonDrill") ||
                        entity.TypeData.GetHasTag("DysonOverloader") )
                    {
                        DysonSidekickPerUnitBaseInfo data = entity.CreateExternalBaseInfo<DysonSidekickPerUnitBaseInfo>( "DysonSidekickPerUnitBaseInfo" );
                        DrillsAndOverloaders.AddToConstructionList(entity);
                    }
                    if ( entity.TypeData.GetHasTag( "DysonShipyard" ) )
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
                    if (entity.TypeData.GetHasTag("DysonResourceGenerator"))
                    {
                        AllResourceGenerators.AddToConstructionList(entity);
                        if ( entity.TypeData.GetHasTag("DysonMetalGenerator") )
                            MetalGenerators.AddToConstructionList(entity);
                        if ( entity.TypeData.GetHasTag("DysonResourceOneGenerator") )
                            ResourceOneGenerators.AddToConstructionList(entity);
                        if ( entity.TypeData.GetHasTag("DysonHackingGenerator") )
                            HackingGenerators.AddToConstructionList(entity);
                        if ( entity.TypeData.GetHasTag("DysonScienceGenerator") )
                            ScienceGenerators.AddToConstructionList(entity);

                    }
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
                    DysonSidekickMobileFleetBaseInfo dysonMobileFleetInfo = mobileCityFedFleet?.TryGetExternalBaseInfoAs<DysonSidekickMobileFleetBaseInfo>();
                    if ( dysonMobileFleetInfo == null )
                        continue;

                    debugCode = 340;

                }

                Strongholds.SwitchConstructionToDisplay();
                Moons.SwitchConstructionToDisplay();
                Shipyards.SwitchConstructionToDisplay();
                Flagships.SwitchConstructionToDisplay();
                Guardians.SwitchConstructionToDisplay();
                GuardianProducers.SwitchConstructionToDisplay();
                GuardianBoosters.SwitchConstructionToDisplay();
                DireGuardianBoosters.SwitchConstructionToDisplay();
                GuardianCount.SwitchConstructionToDisplay();
                GuardiansPerStronghold.SwitchConstructionToDisplay();
                DireGuardiansPerStronghold.SwitchConstructionToDisplay();
                GuardiansPerPlanet.SwitchConstructionToDisplay();
                DireGuardianCount.SwitchConstructionToDisplay();
                BolsterableFlagships.SwitchConstructionToDisplay();
                DrillsAndOverloaders.SwitchConstructionToDisplay();
                Spheres.SwitchConstructionToDisplay();
                Transports.SwitchConstructionToDisplay();
                TransportDestinations.SwitchConstructionToDisplay();
                AllResourceGenerators.SwitchConstructionToDisplay();
                MetalGenerators.SwitchConstructionToDisplay();
                ResourceOneGenerators.SwitchConstructionToDisplay();
                ScienceGenerators.SwitchConstructionToDisplay();
                HackingGenerators.SwitchConstructionToDisplay();
                debugCode = 350;

                UpdateDistrictAndFlagshipLevels();

                debugCode = 400;
                AvailableBlueprints.ClearConstructionListForStartingConstruction();
                for ( int i = 0; i < this.DysonCompletedUpgrades.Count; i++ ) {
                    DysonUpgrade upgrade = this.DysonCompletedUpgrades[i];
                    if ( upgrade.Type == DysonUpgradeType.ClaimBlueprints )
                    {
                        AvailableBlueprints.AddToConstructionList(upgrade);
                    }
                }
                AvailableBlueprints.SwitchConstructionToDisplay();
                foreach ( Planet plan in World_AIW2.Instance.Planets( false ) )
                {
                   if (plan.IsRavaged)
                       RavagedPlanets.AddToConstructionList(plan);
                }
               RavagedPlanets.SwitchConstructionToDisplay();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception during dyson stage 2 debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            AllegianceHelper.AllyThisFactionToHumans( AttachedFaction );
            HandleMetalIncome();
            HandleScienceIncome();
            HandleResourceOneIncome();
            HandleHackingIncome();
            HandleEnemyIncome();
            HandleTransportArrival(Context);
            LoadOrUnloadFlagships( Context );
            UpdateFleetState(Context);
        }
        private void UpdateDistrictAndFlagshipLevels()
        {
            TechUpgrade upgrade = TechUpgradeTable.Instance.GetRowByName("DysonSpireDistrict");
            byte upgradeLevelForDistrict = this.AttachedFaction.TechUnlocks[upgrade.RowIndexNonSim];
            if ( upgradeLevelForDistrict == 1 )
                this.SpireDistrictTier = 2;
            else if ( upgradeLevelForDistrict == 2 )
                this.SpireDistrictTier = 3;

            upgrade = TechUpgradeTable.Instance.GetRowByName("DysonNeinzulDistrict");
            upgradeLevelForDistrict = this.AttachedFaction.TechUnlocks[upgrade.RowIndexNonSim];
            if ( upgradeLevelForDistrict == 1 )
                this.NeinzulDistrictTier = 2;
            else if ( upgradeLevelForDistrict == 2 )
                this.NeinzulDistrictTier = 3;

            upgrade = TechUpgradeTable.Instance.GetRowByName("DysonZenithDistrict");
            upgradeLevelForDistrict = this.AttachedFaction.TechUnlocks[upgrade.RowIndexNonSim];
            if ( upgradeLevelForDistrict == 1 )
                this.ZenithDistrictTier = 2;
            else if ( upgradeLevelForDistrict == 2 )
                this.ZenithDistrictTier = 3;

            upgrade = TechUpgradeTable.Instance.GetRowByName("DysonTemplarDistrict");
            upgradeLevelForDistrict = this.AttachedFaction.TechUnlocks[upgrade.RowIndexNonSim];
            if ( upgradeLevelForDistrict == 1 )
                this.TemplarDistrictTier = 2;
            else if ( upgradeLevelForDistrict == 2 )
                this.TemplarDistrictTier = 3;
            
        }

        public void DismantleStronghold( GameEntity_Squad stronghold, ArcenHostOnlySimContext Context )
        {
            //This is called when a Stronghold is dismantled, we clean up the units.
            //This handles several cases.
            //First, destroying all the ships/structures explicitly from this stronghold
            //Second, destroying all ships that come from our bolstering (this includes the Flagship in the case of dismantling a Major Stronghold)
            bool debug = false;
            int debugCode = 0;
            try
            {
                debugCode = 100;
                this.AttachedFaction.StoredFactionResourceOne += stronghold.TypeData.CostInResourceOne; //just the base cost
                debugCode = 200;
                Fleet fleetToBeDestroyed = stronghold.FleetMembership.Fleet;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("We are destroying " + stronghold.ToStringWithPlanet() + " and fleet " + fleetToBeDestroyed.GetName() + " id " + fleetToBeDestroyed.FleetID, Verbosity.DoNotShow );
                Fleet bolsteredFleet = fleetToBeDestroyed.GetFleetBolsteredByThisCity();
                if ( bolsteredFleet != null && debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("This fleet is bolstering " + bolsteredFleet.GetName(), Verbosity.DoNotShow );
                debugCode = 300;
                
                debugCode = 400;
                foreach ( Fleet fleet in World_AIW2.Instance.Fleets( this.AttachedFaction, FleetStatus.CenterpieceMustLiveOrLooseFleet ) )
                {
                    debugCode = 500;
                    bool destroyEverything = false;
                    bool isBolsteredFleet = false;

                    if ( fleet == bolsteredFleet )
                        isBolsteredFleet = true;
                    if (fleet == fleetToBeDestroyed)
                        destroyEverything = true;
                    if ( isBolsteredFleet &&
                         stronghold.TypeData.GetHasTag("DysonMajorStronghold"))
                        destroyEverything = true;
                    if ( !destroyEverything &&
                         !isBolsteredFleet )
                        continue;

                    if (debug)
                    {
                        if ( isBolsteredFleet && destroyEverything )
                            ArcenDebugging.ArcenDebugLogSingleLine("Destroying all ships associated with mobile fleet " + fleet.GetName(), Verbosity.DoNotShow);
                        if ( destroyEverything )
                            ArcenDebugging.ArcenDebugLogSingleLine("Destroying all ships associated with stronghold fleet " + fleet.GetName(), Verbosity.DoNotShow);
                    }

                    foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                    {
                       debugCode = 600;
                       if (mem.EntitiesOfFMem.GetItemCount() <= 0)
                       {
                           continue; //there aren't any units, so nothing to do here
                       }

                       if (debug)
                       {
                           if ( destroyEverything )
                               ArcenDebugging.ArcenDebugLogSingleLine("\tWe are definitely destroying " + mem.TypeData.GetDisplayName(), Verbosity.DoNotShow);
                           if ( isBolsteredFleet )
                           {
                               if ( debug )
                                   ArcenDebugging.ArcenDebugLogSingleLine("\tThese "  + mem.TypeData.GetDisplayName() + " is bolstered from " + mem.FedHereFromCityFleetID , Verbosity.DoNotShow );
                           }

                       }
                       if ( isBolsteredFleet && !destroyEverything &&
                            mem.FedHereFromCityFleetID != fleetToBeDestroyed.FleetID)
                       {
                           //This ship line on the flagship is from some other Stronghold
                           if (debug )
                               ArcenDebugging.ArcenDebugLogSingleLine("\t\tNot from the dismantled stronghold. SKIP", Verbosity.DoNotShow );
                           continue;
                        }
                      debugCode = 900;
                      if (debug)
                          ArcenDebugging.ArcenDebugLogSingleLine("\tWe will despawn all " + mem.TypeData.GetDisplayName() + " for fleet " + fleet.GetName() + ". Base cap is " + mem.ExplicitBaseSquadCap + " and we have " + mem.EntitiesOfFMem.GetItemCount() , Verbosity.DoNotShow);

                      foreach ( GameEntity_Squad squad in mem.Entities )
                      {
                          debugCode = 1000;
                          // if (debug)
                          //     ArcenDebugging.ArcenDebugLogSingleLine("\t\tDespawning " + squad.ToStringWithPlanet(), Verbosity.DoNotShow);
                          squad.Despawn(Context, true, InstancedRendererDeactivationReason.SelfDestructOnTooHighOfCap);
                      }

                    }
                }

                stronghold.SetToBeRemovedAtEndOfThisFrameForReason(InstancedRendererDeactivationReason.RemoveOldUnitWeNoLongerNeed); //remove the old unit
            } catch( Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in DismantleStronghold debugCode " + debugCode + " " + e.ToString() +". We were trying to destroy " + stronghold.ToString(), Verbosity.DoNotShow );
            }
        }

        private void UpdateFleetState(ArcenClientOrHostSimContextCore Context)
        {
            //this is used for the Auto-Play mode
            foreach ( GameEntity_Squad flagship in Flagships.DisplaySquads() )
            {
                if ( flagship.TypeData.GetHasTag("DysonSphereFlagship") )
                {
                    flagship.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
                    flagship.FleetMembership.Fleet.IsFleetFlagshipAllowedToUseMovementModes = true;
                }
                else
                {
                    //"Normal Flagships", unlike the sphere flagships that always play automatically
                    if ( World_AIW2.Instance.Setup.GetStringBySetting("DysonAutoDefend") == "Disabled" || this.AttachedFaction.UnderPlayerControl() )
                        continue;
                }
                Fleet fleet = flagship.FleetMembership.Fleet;
                if (fleet == null)
                    continue;

                DysonSidekickMobileFleetBaseInfo fleetInfo = fleet.CreateExternalBaseInfo<DysonSidekickMobileFleetBaseInfo>( "DysonSidekickMobileFleetBaseInfo");
                if ( fleetInfo == null )
                {
                    ArcenDebugging.LogSingleLine("no fleet info for " + flagship.ToStringWithPlanet(), Verbosity.DoNotShow );
                    continue;
                }
                int currentStrength = fleet.CalculateEffectiveCurrentFleetStrength_PlayerFleetsOnly();
                int totalstrength = fleet.CalculateEffectiveFullFleetStrength_PlayerFleetsOnly();
                fleetInfo.NeedsToRebuild = DoesFleetNeedToRebuild(fleet);
            }
        }
        public static bool DoesFleetNeedToRebuild( Fleet fleet )
        {
            if ( fleet == null )
                return false;
            GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
            if ( centerpiece != null && centerpiece.GetIsCrippled())
                return true;
            int currentStrength = fleet.CalculateEffectiveCurrentFleetStrength_PlayerFleetsOnly();
            int totalStrength = fleet.CalculateEffectiveFullFleetStrength_PlayerFleetsOnly();
            if ( currentStrength <= totalStrength / 2 )
                return true;
            return false;
        }
        private void LoadOrUnloadFlagships(ArcenClientOrHostSimContextCore Context)
        {
            //The logic is as follows: If a flagship has wormhole orders, do nothing
            //if a flagship has enemies on its planet, unload
            //if a flagship has no enemies on its planet, load
            foreach ( GameEntity_Squad flagship in Flagships.DisplaySquads() )
            {
                if ( !flagship.TypeData.GetHasTag("DysonSphereFlagship") &&
                     (World_AIW2.Instance.Setup.GetStringBySetting("DysonAutoDefend") == "Disabled" ||
                      this.AttachedFaction.UnderPlayerControl() ) )
                    continue;

                Fleet fleet = flagship.FleetMembership.Fleet;
                if (fleet == null)
                    continue;
                if (flagship.GetDestinationPlanet() != flagship.Planet)
                {
                    //if we are en route someplace, make sure we are always in load mode
                    if ( !fleet.IsFleetInTransportLoadMode )
                        fleet.IsFleetInTransportLoadMode = true;

                    continue; //we are going somewhere
                }
                //if we have enemies or are on a hostile planet, unload
                if ( flagship.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength > 0 ||
                     flagship.Planet.GetControllingOrInfluencingFaction().GetIsHostileTowards( this.AttachedFaction ))
                {
                    fleet.IsFleetInTransportLoadMode = false;
                    continue;
                }
                else
                    fleet.IsFleetInTransportLoadMode = true;
            }
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

                    if ( Mat.DistanceBetweenPointsImprecise( transport.WorldLocation, potentialDest.WorldLocation ) < 100 )
                    {
                        transport.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                        DysonSidekickPerUnitBaseInfo data = transport.CreateExternalBaseInfo<DysonSidekickPerUnitBaseInfo>( "DysonSidekickPerUnitBaseInfo" );
                        this.AttachedFaction.StoredFactionResourceOne += data.CuendillarTransported;
                    }
                }
            }
        }
        private void HandleEnemyIncome()
        {
            if ( Difficulty == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("difficulty uninitialized", Verbosity.DoNotShow);
                return;
            }
            FInt income = Difficulty.BaseStrPerSecond;
            FInt aipIncome = FactionUtilityMethods.Instance.GetCurrentAIP() / 10 * Difficulty.IncreasedStrPerSecondPer10AIP;
            FInt metalRelatedIncome = MetalGenerators.Count * Difficulty.IncreasedStrPerSecondPerMetalGenerator;
            FInt scienceRelatedIncome = ScienceGenerators.Count * Difficulty.IncreasedStrPerSecondPerScienceGenerator;
            FInt hackingRelatedIncome = HackingGenerators.Count * Difficulty.IncreasedStrPerSecondPerHackingGenerator;
            FInt resourceOneRelatedIncome = ResourceOneGenerators.Count * Difficulty.IncreasedStrPerSecondPerResourceOneGenerator;
            this.EnemyIncomeLastSecond = (income + aipIncome + metalRelatedIncome + scienceRelatedIncome + hackingRelatedIncome + resourceOneRelatedIncome).IntValue; //this is single threaded for each dyson sidekick, so no case of a race
            this.StrengthForNextEnemyAttack += this.EnemyIncomeLastSecond;
        }
        private void HandleMetalIncome()
        {
            if (Income == null)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("income uninitialized", Verbosity.DoNotShow);
                return;
            }
            FInt income = Income.BaseMetalIncomePerSecond;
            //ArcenDebugging.ArcenDebugLogSingleLine("processing income " + Income.ToString() + ", we are getting " + income + " metal", Verbosity.DoNotShow );
            foreach ( KeyValuePair<SafeSquadWrapper, bool> _kv in this.MetalGenerators.GetDisplayList() )
            {
                SafeSquadWrapper wrapper = _kv.Key;
                GameEntity_Squad generator = wrapper.GetSquad();
                if ( generator == null )
                    continue;
                income += Income.MetalIncomePerGeneratorPerSecond + (generator.CurrentMarkLevel - 1) * this.Income.MetalIncomePerGeneratorPerSecondIncreasePerMarkLevel;
            }
            this.MetalIncomeLastSecond = income;
            this.AttachedFaction.StoredMetal += income.IntValue;
            float incomeTemp = (float)income * World_AIW2.Instance.SimulationProfile.SecondsPerFrameNonSim;
            this.AttachedFaction.LastFrame_MetalProducedOverride = FInt.FromParts((int)incomeTemp, 000);
        }
        public static int PlanetBurningTime = 45;
        public int CalculateDrillTime( GameEntity_Squad drill )
        {
            int resourceOneAmt = (int)drill.Planet.ResourceOneRemainingForAnyPlayer;
            if ( drill.TypeData.GetHasTag("DysonAsteroidDrill"))
            {
                GameEntity_Squad asteroid = FactionUtilityMethods.Instance.GetAsteroidOnPlanetOrNull( drill.Planet );
                GameEntity_Squad chrysalis = FactionUtilityMethods.Instance.GetReaperChrysalisOnPlanetOrNull( drill.Planet );
                GameEntity_Squad planetoid = FactionUtilityMethods.Instance.GetPlanetoidOnPlanetOrNull( drill.Planet );
                if (asteroid != null)
                {
                    DysonSidekickPerUnitBaseInfo astData = asteroid.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                    resourceOneAmt = astData.CuendillarRemaining;
                }
                else if ( chrysalis != null)
                {
                    ReapersPerUnitBaseInfo chrData = chrysalis.TryGetExternalBaseInfoAs<ReapersPerUnitBaseInfo>();
                    resourceOneAmt = chrData.CuendillarRemaining;
                }
                else
                {
                    DysonSidekickPerUnitBaseInfo pltData = planetoid.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                    resourceOneAmt = pltData.CuendillarRemaining;
                }
            }
            int transportsToBuild = resourceOneAmt / this.Difficulty.CuendillarMinedPerTransport;
            if ( resourceOneAmt % this.Difficulty.CuendillarMinedPerTransport != 0 )
                transportsToBuild++;
            int output = 0;
            DysonSidekickPerUnitBaseInfo data = drill.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
            if (drill.TypeData.GetHasTag("DysonAsteroidDrill"))
            {
                output = this.Difficulty.AsteroidTransportSpawnInterval * transportsToBuild; //asteroids don't burn
                if ( data != null && data.DrillingChrysalis )
                    output = this.Difficulty.ChrysalisTransportSpawnInterval * transportsToBuild; //different timing for chrysalises
            }
            else
                output = this.Difficulty.PlanetTransportSpawnInterval * transportsToBuild + PlanetBurningTime;

            return output;
        }
        private void HandleScienceIncome()
        {
            int debugCode = 0;
            try{
                debugCode = 100;
                FInt income = Income.BaseScienceIncomePerSecond;
                //ArcenDebugging.ArcenDebugLogSingleLine("processing income " + Income.ToString() + ", we are getting " + income + " metal", Verbosity.DoNotShow );
                debugCode = 200;
                foreach ( KeyValuePair<SafeSquadWrapper, bool> _kv in this.ScienceGenerators.GetDisplayList() )
                {
                    SafeSquadWrapper wrapper = _kv.Key;
                    debugCode = 300;
                    GameEntity_Squad generator = wrapper.GetSquad();
                    if ( generator == null )
                        continue;
                    debugCode = 400;
                    income += Income.ScienceIncomePerGeneratorPerSecond + (generator.CurrentMarkLevel - 1) * this.Income.ScienceIncomePerGeneratorPerSecondIncreasePerMarkLevel;
                }
                debugCode = 500;
                this.ScienceIncomeLastSecond = income;
                this.AttachedFaction.StoredScience += income;
            } catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine( "debugCode: " + debugCode + " Hit exception in HandleScienceIncome for DS Sidekick: " + e, Verbosity.DoNotShow );
            }
        }
        private void HandleHackingIncome()
        {
            FInt income = Income.BaseHackingIncomePerSecond;
            //ArcenDebugging.ArcenDebugLogSingleLine("processing income " + Income.ToString() + ", we are getting " + income + " metal", Verbosity.DoNotShow );
            foreach ( KeyValuePair<SafeSquadWrapper, bool> _kv in this.HackingGenerators.GetDisplayList() )
            {
                SafeSquadWrapper wrapper = _kv.Key;
                GameEntity_Squad generator = wrapper.GetSquad();
                if ( generator == null )
                    continue;
                income += Income.HackingIncomePerGeneratorPerSecond + (generator.CurrentMarkLevel - 1) * this.Income.HackingIncomePerGeneratorPerSecondIncreasePerMarkLevel;
            }
            this.HackingIncomeLastSecond = income;
            this.AttachedFaction.StoredHacking += income;
        }
        private void HandleResourceOneIncome()
        {
            FInt income = Income.BaseResourceOneIncomePerSecond;
            //ArcenDebugging.ArcenDebugLogSingleLine("processing income " + Income.ToString() + ", we are getting " + income + " metal", Verbosity.DoNotShow );
            foreach ( KeyValuePair<SafeSquadWrapper, bool> _kv in this.ResourceOneGenerators.GetDisplayList() )
            {
                SafeSquadWrapper wrapper = _kv.Key;
                GameEntity_Squad generator = wrapper.GetSquad();
                if ( generator == null )
                    continue;
                income += Income.ResourceOneIncomePerGeneratorPerSecond + (generator.CurrentMarkLevel - 1) * this.Income.ResourceOneIncomePerGeneratorPerSecondIncreasePerMarkLevel;
            }
            this.ResourceOneIncomeLastSecond = income;
            this.AttachedFaction.StoredFactionResourceOne += income;
        }

        public bool DoesPlanetHaveStronghold( Planet planet )
        {
            bool foundStronghold = false;
            foreach ( GameEntity_Squad city in this.Strongholds.DisplaySquads() )
            {
                if ( city.Planet == planet ) {
                    foundStronghold = true;
                    break;
                }
            }
            return foundStronghold;
        }
        public bool DoesPlanetHaveSphere( Planet planet )
        {
            bool foundSphere = false;
            foreach ( GameEntity_Squad sphere in this.Spheres.DisplaySquads() )
            {
                if ( sphere.Planet == planet ) {
                    foundSphere = true;
                    break;
                }
            }
            return foundSphere;
        }
        public bool DoesPlanetHaveDrill( Planet planet )
        {
            bool foundDrill = false;
            foreach ( GameEntity_Squad drill in this.DrillsAndOverloaders.DisplaySquads() )
            {
                if ( drill.Planet == planet ) {
                    foundDrill = true;
                    break;
                }
            }
            return foundDrill;
        }

        #region TotalMarkLevelForAllStrongholds
        public int TotalMarkLevelForAllStrongholds()
        {
            int total = 0;
            foreach ( GameEntity_Squad city in this.Strongholds.DisplaySquads() )
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
        /// Note that the dyson's logic here is different because the killing faction can be null
        /// </summary>
        public override void CheckIfPlayerFactionShouldGetRewardBasedOnAUnitDying( Faction factionThatKilledEntityOrNull, bool IsFromOnlyPartOfStackDying, GameEntity_Squad entity,
            DamageSource Damage, EntitySystem FiringSystemOrNull, int numExtraStacksKilled, ArcenHostOnlySimContext Context )
        {
            FInt multiplier = FInt.One;
            bool debug = false;
            if ( this.GetShouldThisDysonFactionGetARewardBasedOnThisKill( factionThatKilledEntityOrNull, entity, ref multiplier, Context ) )
            {
                DLC3GameEntityTypeDataExtension entity_DLC3TypeData = entity.TypeData.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );
                if ( debug ) {
                    int bonusMultiplier = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "BonusNecromancerResources" );
                    ArcenDebugging.ArcenDebugLogSingleLine( "\t We get resources; multiplier " + multiplier + " (bonus multiplier component: " + bonusMultiplier +")" , Verbosity.DoNotShow );
                }

                if ( entity_DLC3TypeData != null )
                {
                    // FInt scienceToGrantOnDeath = FInt.Zero;
                    // FInt hackingToGrantOnDeath = FInt.Zero;
                    // FInt resourceOneToGrantOnDeath = FInt.Zero;
                    entity_DLC3TypeData.GetDysonResourcesToGrantOnDeath(entity,
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

                    if ( resourceOneToGrantOnDeath > FInt.Zero )
                    {
                        resourceOneToGrantOnDeath *= multiplier;
                        this.AttachedFaction.StoredFactionResourceOne += resourceOneToGrantOnDeath;
                    }
                }
            }
        }
        #endregion
        #region GetShouldThisDysonFactionGetARewardBasedOnThisKill
        private bool GetShouldThisDysonFactionGetARewardBasedOnThisKill( Faction killingFactionOrNull, GameEntity_Squad entity, ref FInt multiplier, ArcenHostOnlySimContext Context )
        {
            //A bunch of rules; fundamentally, the rules are:
            //if we killed the unit, or were part of the fight, we get full resources
            //If this was killed by an Allied NPC faction, we get partial resources
            //If this was killed by another dyson (and we weren't in the fight), we get nothing
            //If this was killed by an allied, non-dyson faction we get partial resources
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
                if ( DysonSidekickFactionBaseInfo.GetIsThisADysonFaction( killingFactionOrNull ) )
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
            //Show the dyson upgrade history here.
            if ( this.DysonHistory.Count == 0 )
            {
                Buffer.Add( "There is no history" );
                return false;
            }
            for ( int i = this.DysonHistory.Count - 1; i >= 0; i-- )
            {
                DysonUpgradeEvent nEvent = this.DysonHistory[i];
                Buffer.Add( nEvent.ToDebugString );
            }
            return true; //return true so it does not also show the local hacking history
        }
        #endregion

        #region IsPlanetEligibleForStronghold
        public bool IsPlanetEligibleForStronghold( Planet planet, out bool isOnStrongholdPlanet )
        {
            isOnStrongholdPlanet = false;
            try
            {
                int fewestHops = -1;
                foreach ( GameEntity_Squad city in this.Strongholds.DisplaySquads() )
                {
                    int hops = city.Planet.GetHopsTo( planet );
                    if ( hops < fewestHops || fewestHops == -1 )
                        fewestHops = hops;
                }
                if ( fewestHops == 0 )
                    isOnStrongholdPlanet = true;

                if ( fewestHops < this.RequiredHopsBetweenStrongholds )
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

        public static int GetDysonFactionCount()
        {
            int count = 0;
            PlayerTypeData playerType;
            
            playerType = PlayerTypeDataTable.Instance.GetRowByName( "DysonEmpire", LookupSwapAllowed.Yes, true );
            if (playerType != null)
                count += playerType.CurrentFactionsInThisGame.Count;
            
            playerType = PlayerTypeDataTable.Instance.GetRowByName( "DysonSidekick", LookupSwapAllowed.Yes, true );
            if (playerType != null)
                count += playerType.CurrentFactionsInThisGame.Count;
            
            return count;
        }

        public static bool GetIsThisADysonFaction( Faction fac )
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
                case "DysonSidekick":
                case "DysonEmpire":
                    return true; 
            }
            return false;
        }

        //Read-only foreach sibling of the (now-retired) DFAllDysonFactions. Yields the same
        //Faction sequence (Dyson empire + sidekick player types) with zero allocations,
        //so callers can use a plain foreach with outer locals in scope instead of a static
        //(non-capturing) delegate plus copy-in/copy-out via static fields.
        public static DysonFactionsEnumerable DysonFactions => new DysonFactionsEnumerable();

        public readonly struct DysonFactionsEnumerable
        {
            public Enumerator GetEnumerator() => new Enumerator( 0, 0 );

            public struct Enumerator
            {
                private int playerTypeIndex;
                private int factionIndex;
                private Faction current;
                internal Enumerator( int startPlayerTypeIndex, int startFactionIndex )
                {
                    this.playerTypeIndex = startPlayerTypeIndex;
                    this.factionIndex = startFactionIndex - 1;
                    this.current = null;
                }
                public Faction Current => this.current;
                public bool MoveNext()
                {
                    var rows = PlayerTypeDataTable.Instance.Rows;
                    int rowCount = rows.Count;
                    while ( this.playerTypeIndex < rowCount )
                    {
                        PlayerTypeData playerType = rows[this.playerTypeIndex];
                        if ( playerType.CurrentFactionsInThisGame.Count > 0 &&
                             ( playerType.InternalName == "DysonSidekick" ||
                               playerType.InternalName == "DysonEmpire" ) )
                        {
                            int facCount = playerType.CurrentFactionsInThisGame.Count;
                            if ( ++this.factionIndex < facCount )
                            {
                                this.current = playerType.CurrentFactionsInThisGame[this.factionIndex];
                                return true;
                            }
                        }
                        this.playerTypeIndex++;
                        this.factionIndex = -1;
                    }
                    this.current = null;
                    return false;
                }
            }
        }

        public static Faction GetFirstDysonFactionOrNull()
        {
            foreach ( PlayerTypeData playerType in PlayerTypeDataTable.Instance.Rows )
            {
                if ( playerType.CurrentFactionsInThisGame.Count <= 0 )
                    continue;
                switch ( playerType.InternalName )
                {
                    case "DysonSidekick":
                    case "DysonEmpire":
                        foreach ( Faction fac in playerType.CurrentFactionsInThisGame )
                        {
                            return fac;
                        }
                        break;
                }
            }
            return null;
        }

        public static Faction GetRandomDysonFaction( ArcenSimContextAnyStatus Context )
        {
            List<Faction> workingFactions = Faction.GetTemporaryFactionList( "NecroFac-GetRandomDysonFaction-workingFactions", 10f );
            if ( workingFactions == null ) //blocked for teardown/shutdown; bail
                return null;
            foreach ( PlayerTypeData playerType in PlayerTypeDataTable.Instance.Rows )
            {
                if ( playerType.CurrentFactionsInThisGame.Count <= 0 )
                    continue;
                switch ( playerType.InternalName )
                {
                    case "DysonSidekick":
                    case "DysonEmpire":
                        foreach ( Faction faction in playerType.CurrentFactionsInThisGame )
                            workingFactions.Add( faction );
                        break;
                }
            }
            Faction fac = null;
            if ( workingFactions.Count == 1 )
                fac = workingFactions[0];
            else if ( workingFactions.Count > 1 )
                fac = workingFactions[Context.RandomToUse.Next( 0, workingFactions.Count )];
            Faction.ReleaseTemporaryFactionList( workingFactions );
            return fac;
        }
        public GameEntity_Squad GetRandomStrongholdForSwapOrNull( ArcenHostOnlySimContext Context )
        {
            WorkingList.Clear();
            foreach ( GameEntity_Squad city in this.Strongholds.DisplaySquads() )
            {
                if ( city.FleetMembership == null )
                    continue;
                if ( city.TypeData.GetHasTag("Phylactery" ) )
                    continue;
                if ( city.GetIsCrippled() )
                    continue;
                WorkingList.Add( city );
            }
            if ( WorkingList.Count == 0 )
                return null;
            WorkingList.Sort( static delegate ( SafeSquadWrapper Left, SafeSquadWrapper Right )
            {
                PlanetFaction lFaction = Left.PlanetFaction;
                PlanetFaction rFaction = Right.PlanetFaction;
                int lHostileStrength = lFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                int rHostileStrength = rFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                if ( lHostileStrength == rHostileStrength )
                    return Left.Planet.Name.CompareTo( Right.Planet.Name );
                return lHostileStrength.CompareTo( rHostileStrength );
            } );
            return WorkingList[0].GetSquad();
        }

        public void SwapStrongholds( GameEntity_Squad oldPhylactery, GameEntity_Squad newPhylactery, ArcenHostOnlySimContext Context )
        {
            ArcenPoint oldLocation = oldPhylactery.WorldLocation;
            Planet oldPlanet = oldPhylactery.Planet;
            Planet newPlanet = newPhylactery.Planet;
            Fleet oldFleet = oldPhylactery.FleetMembership.Fleet;
            Fleet newFleet = newPhylactery.FleetMembership.Fleet;
            GameCommand command = null;

            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "DysonAmplifier" ) )
            {
                //Swap fleets for any amplifiers on the planet
                ArcenPoint newPoint = ArcenPoint.ZeroZeroPoint;
                if ( entity.Planet != oldPlanet && entity.Planet != newPlanet )
                    continue;

                command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                if ( entity.Planet == oldPlanet )
                {
                    command.RelatedIntegers.Add( oldFleet.FleetID );
                    command.RelatedIntegers2.Add( newFleet.FleetID );
                    command.RelatedIntegers3.Add( entity.FleetMembership.UniqueTypeDataDifferentiatorForDuplicates );
                    command.RelatedIntegers4.Add( oldFleet.GetNextUniqueIntToUseOfMatchingMembershipGroupsBasedOnSquadType( entity.TypeData ) );
                    command.RelatedString = "SwapFleetMemberWithEmpty";
                    command.RelatedString2 = entity.TypeData.InternalName;
                }
                if ( entity.Planet == newPlanet )
                {
                    command.RelatedIntegers.Add( newFleet.FleetID );
                    command.RelatedIntegers2.Add( oldFleet.FleetID );
                    command.RelatedIntegers3.Add( entity.FleetMembership.UniqueTypeDataDifferentiatorForDuplicates );
                    command.RelatedIntegers4.Add( newFleet.GetNextUniqueIntToUseOfMatchingMembershipGroupsBasedOnSquadType( entity.TypeData) );
                    command.RelatedString = "SwapFleetMemberWithEmpty";
                    command.RelatedString2 = entity.TypeData.InternalName;
                }
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, true );
            }
            //Now move the strongholdes
            oldPhylactery.WarpToPlanet( newPhylactery.Planet, newPhylactery.WorldLocation, "Swap Stronghold Location" );
            newPhylactery.WarpToPlanet( oldPlanet, oldLocation, "Swap Stronghold Location" );

            //And destroy the old structures
            command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.DestroyDistantFleetMembers], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
            command.RelatedEntityIDs.Add( oldPhylactery.PrimaryKeyID );
            command.RelatedEntityIDs.Add( newPhylactery.PrimaryKeyID );
            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, true );
            if ( oldPhylactery.GetIsCrippled() )
            {
                newPhylactery.TakeDamageDirectly( newPhylactery.GetMaxHullPoints() * 2, null, null, DamageSource.SelfDamageFromMyOwnWeapons, Context );
                newPhylactery.CrippledUntilReachesFullHealth = true;
            }

            //World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
        }
        #region UpdatePowerLevel
        public override void UpdatePowerLevel()
        {
            bool debug = false;
            FInt powerLevelSoFar = FInt.Zero;

            FInt totalStrength = FInt.Zero;
            FInt totalFlagshipMarkLevel = FInt.Zero;
            bool foundMarkVFlagship = false;
            bool foundMarkVStronghold = false;

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
                        foundMarkVStronghold = true;
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
            
            if ( foundMarkVStronghold )
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
