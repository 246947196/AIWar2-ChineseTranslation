using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public struct BreachHistoryEntry
    {
        public int GameSecond;
        public string PlanetName;
        public string BreachDisplayName;
        public MalwareBreachDifficulty Difficulty;
    }

    public class ApkalluFactionBaseInfo : ExternalFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {
        // Serialized
        public int DuruSpawnTime; // game second when next artifact spawns; -1 = not yet scheduled
        public List<MalwareBreach> CompletedBreaches =  List<MalwareBreach>.Create_WillNeverBeGCed( 6, "ApkalluBaseInfo-CompletedBreaches" );
        public List<BreachHistoryEntry> BreachHistory = List<BreachHistoryEntry>.Create_WillNeverBeGCed( 12, "ApkalluBaseInfo-BreachHistory" );

        // Not Serialized
        public ApkalluDifficulty Difficulty;
        private int Intensity = -1;
        public bool SpawnEarlyNexus = false;
        public bool SpawnManyNexuses = false;
        public bool ImmediateDuruSpawn = false;
        public bool UnlockAllBreaches = false;
        //Rangers
        public readonly DoubleBufferedValue<int> RangerCount = new DoubleBufferedValue<int>( 0 );
        public readonly DoubleBufferedValue<int> DireRangerCount = new DoubleBufferedValue<int>( 0 );
        public readonly DoubleBufferedList<SafeSquadWrapper> Rangers = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Apkallu-Rangers" );
        public readonly DoubleBufferedList<SafeSquadWrapper> RangerProducers = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Apkallu-RangerProducers" );
        public readonly DoubleBufferedList<SafeSquadWrapper> RangerOutposts = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Apkallu-RangerOutposts" );
        public readonly DoubleBufferedList<SafeSquadWrapper> DireRangerOutposts = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Apkallu-DireRangerOutposts" );
        public readonly DoubleBufferedDictionary<SafeSquadWrapper, int> RangersPerDuru = DoubleBufferedDictionary<SafeSquadWrapper, int>.Create_WillNeverBeGCed( 20, "ApkalluFactionBaseInfo-RangersPerDuru" );
        public readonly DoubleBufferedDictionary<SafeSquadWrapper, int> DireRangersPerDuru = DoubleBufferedDictionary<SafeSquadWrapper, int>.Create_WillNeverBeGCed( 20, "ApkalluFactionBaseInfo-DireRangersPerDuru" );
        public readonly DoubleBufferedDictionary<Planet, int> RangersPerPlanet = DoubleBufferedDictionary<Planet, int>.Create_WillNeverBeGCed( 500, "ApkalluFactionBaseInfo-RangersPerPlanet" );
        //Locusts
        public readonly DoubleBufferedDictionary<Planet, int> LocustsPerLure = DoubleBufferedDictionary<Planet, int>.Create_WillNeverBeGCed( 500, "ApkalluFactionBaseInfo-LocustsPerLure" );
        public readonly DoubleBufferedDictionary<Planet, int> TotalHopsForLocustsPerLure = DoubleBufferedDictionary<Planet, int>.Create_WillNeverBeGCed( 500, "ApkalluFactionBaseInfo-TotalHopsForLocustsPerLure" );

        
        public readonly DoubleBufferedList<SafeSquadWrapper> Flagships = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 4, "Apkallu-Flagships" );
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> Ziggurats = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "Apkallu-Ziggurats" );
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> Temens = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "Apkallu-Temens" );
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> Durus = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "Apkallu-Durus" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Lamassus = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "Apkallu-Lamassus" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Pilgrims = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "Apkallu-Pilgrims" );
        public readonly DoubleBufferedList<SafeSquadWrapper> LesserPilgrimProducers = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "Apkallu-LesserPilgrimProducers" );
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> OutguardGranters = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "Apkallu-OutguardGranter" );
        
        public readonly DoubleBufferedList<SafeSquadWrapper> MobileShips = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "Apkallu-MobileShips" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Outposts = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "Apkallu-Outposts" );
        public readonly DoubleBufferedList<SafeSquadWrapper> MalwareDisruptors = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "Apkallu-MalwareDisruptors" );

        public readonly DoubleBufferedList<SafeSquadWrapper> Locusts = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "Apkallu-Locusts" );
        



        // Not serialized 鈥?repopulated from completed breach list on load.
        public readonly List<ApkalluFactionResource> UnlockedResources = List<ApkalluFactionResource>.Create_WillNeverBeGCed( 3, "Apkallu-UnlockedResources" );
        public readonly List<GameEntityTypeData> UnlockedDuruStructures = List<GameEntityTypeData>.Create_WillNeverBeGCed( 10, "Apkallu-UnlockedDuruStructures" );
        public readonly List<GameEntityTypeData> UnlockedZigguratStructures = List<GameEntityTypeData>.Create_WillNeverBeGCed( 10, "Apkallu-UnlockedZigguratStructures" );
        public readonly List<GameEntityTypeData> UnlockedTemenStructures = List<GameEntityTypeData>.Create_WillNeverBeGCed( 10, "Apkallu-UnlockedTemenStructures" );

        // Rebuilt each second by HandleOutguardGranters 鈥?the set of outguard group names currently active
        // (i.e. a live HandleOutguardGranters structure is present in a Ziggurat socket).
        public readonly List<string> ActiveOutguardGroups = List<string>.Create_WillNeverBeGCed( 8, "Apkallu-ActiveOutguardGroups" );

        // Populated by Hacking_RefreshLamassuModules on hack completion (one entry per hacked Lamassu PK ID).
        // Cleared by ApkalluFactionDeepInfo after processing. Only the listed Lamassus are refreshed,
        // so a hack on one Lamassu cannot strip modules from another on a different planet.
        public readonly List<int> LamassusNeedingModuleRefresh = List<int>.Create_WillNeverBeGCed( 4, "Apkallu-LamassusNeedingRefresh" );
        // Populated in Stage2; contains PrimaryKeyIDs of Lamassus whose active summoner tags
        // differ from what their Ziggurat's planet currently provides.
        public readonly List<int> LamassuPrimaryKeyIDsOutOfSync = List<int>.Create_WillNeverBeGCed( 4, "Apkallu-LamassuOutOfSync" );
        private static readonly List<string> desyncTagScratch = List<string>.Create_WillNeverBeGCed( 8, "Apkallu-DesyncTagScratch" );

        public void UnlockResource( ApkalluFactionResource resource )
        {
            if ( !UnlockedResources.Contains( resource ) )
                UnlockedResources.Add( resource );
        }

        public ApkalluFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            DuruSpawnTime = -1;
            Difficulty = null;
            Intensity = -1;
            Flagships.Clear();
            MobileShips.Clear();
            MalwareDisruptors.Clear();
            Outposts.Clear();
            Ziggurats.Clear();
            Temens.Clear();
            Durus.Clear();
            Lamassus.Clear();
            OutguardGranters.Clear();
            Locusts.Clear();
            Pilgrims.Clear();
            LesserPilgrimProducers.Clear();
            SpawnEarlyNexus = false;
            SpawnManyNexuses = false;
            ImmediateDuruSpawn = false;
            UnlockAllBreaches = false;
            UnlockedResources.Clear();
            UnlockedDuruStructures.Clear();
            UnlockedZigguratStructures.Clear();
            UnlockedTemenStructures.Clear();
            ActiveOutguardGroups.Clear();
            CompletedBreaches.Clear();
            BreachHistory.Clear();
            LamassusNeedingModuleRefresh.Clear();
            LamassuPrimaryKeyIDsOutOfSync.Clear();

            Rangers.Clear();
            RangerProducers.Clear();
            RangerOutposts.Clear();
            DireRangerOutposts.Clear();
            RangerCount.Clear();
            DireRangerCount.Clear();
            RangersPerDuru.Clear();
            DireRangersPerDuru.Clear();
            RangersPerPlanet.Clear();

            LocustsPerLure.Clear();
            TotalHopsForLocustsPerLure.Clear();
        }

        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "ApkalluFactionBaseInfo" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.DuruSpawnTime, "DuruSpawnTime" );
            // Only serialize breaches with a real, resolvable name. The reader (DeserializeFactionIntoSelf) skips any entry
            // whose name is empty or fails to resolve, so emitting null / empty-name entries here would leave the client's
            // list shorter than the count we announced -> host/client divergence (and the client list is indexed
            // null-UNSAFELY elsewhere, so we must never let a null into it). Count and write only the valid entries so both
            // sides reconstruct the identical list.
            int validBreachCount = 0;
            for ( int i = 0; i < this.CompletedBreaches.Count; i++ )
                if ( this.CompletedBreaches[i] != null && !string.IsNullOrEmpty( this.CompletedBreaches[i].name ) )
                    validBreachCount++;
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, validBreachCount, "CompletedBreaches.Count" );
            for ( int i = 0; i < this.CompletedBreaches.Count; i++ )
                if ( this.CompletedBreaches[i] != null && !string.IsNullOrEmpty( this.CompletedBreaches[i].name ) )
                    Buffer.AddString_Condensed( MetaData, this.CompletedBreaches[i].name, "CompletedBreaches[" + i + "]" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.BreachHistory.Count, "BreachHistory.Count" );
            for ( int i = 0; i < this.BreachHistory.Count; i++ )
            {
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.BreachHistory[i].GameSecond, "BreachHistory[" + i + "].GameSecond" );
                Buffer.AddString_Condensed( MetaData, this.BreachHistory[i].PlanetName, "BreachHistory[" + i + "].PlanetName" );
                Buffer.AddString_Condensed( MetaData, this.BreachHistory[i].BreachDisplayName, "BreachHistory[" + i + "].BreachDisplayName" );
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, (int)this.BreachHistory[i].Difficulty, "BreachHistory[" + i + "].Difficulty" );
            }
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "ApkalluFactionBaseInfo" );
            this.DuruSpawnTime = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "DuruSpawnTime" );

            this.CompletedBreaches.Clear();
            int breachCount = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "CompletedBreaches.Count" );
            for ( int i = 0; i < breachCount; i++ )
            {
                string breachName = Buffer.ReadString_Condensed( MetaData, "CompletedBreaches[" + i + "]" );
                MalwareBreach breach = string.IsNullOrEmpty( breachName ) ? null : MalwareBreachTable.Instance?.GetRowByNameOrNullIfNotFound( breachName );
                if ( breach != null )
                    CompletedBreaches.Add( breach );
            }
            this.BreachHistory.Clear();
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 808 ) )
            {
                int historyCount = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "BreachHistory.Count" );
                for ( int i = 0; i < historyCount; i++ )
                {
                    BreachHistoryEntry entry = new BreachHistoryEntry();
                    entry.GameSecond = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "BreachHistory[" + i + "].GameSecond" );
                    entry.PlanetName = Buffer.ReadString_Condensed( MetaData, "BreachHistory[" + i + "].PlanetName" );
                    entry.BreachDisplayName = Buffer.ReadString_Condensed( MetaData, "BreachHistory[" + i + "].BreachDisplayName" );
                    entry.Difficulty = (MalwareBreachDifficulty)Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "BreachHistory[" + i + "].Difficulty" );
                    this.BreachHistory.Add( entry );
                }
            }
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( "10 来自阿普卡鲁的负载" );
            return 10;
        }

        protected override void DoFactionGeneralAggregationsPausedOrUnpaused() { }

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            if ( Intensity == -1 )
                DoRefreshFromFactionSettings();
            return Intensity;
        }

        protected override void DoRefreshFromFactionSettings()
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                ConfigurationForFaction cfg = this.AttachedFaction.Config;
                Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "ApkalluIntensity", true );
                debugCode = 200;
                if ( ApkalluDifficultyTable.Instance == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "ApkalluFactionBaseInfo: null ApkalluDifficultyTable instance", Verbosity.DoNotShow );
                    return;
                }
                SpawnEarlyNexus = cfg.GetBoolValueForCustomFieldOrDefaultValue( "SpawnEarlyNexus", false );
                SpawnManyNexuses = cfg.GetBoolValueForCustomFieldOrDefaultValue( "SpawnManyNexuses", false );
                ImmediateDuruSpawn = cfg.GetBoolValueForCustomFieldOrDefaultValue( "ImmediateDuruSpawn", false );
                UnlockAllBreaches = cfg.GetBoolValueForCustomFieldOrDefaultValue( "UnlockAllBreaches", false );
                Difficulty = ApkalluDifficultyTable.Instance.GetRowByIntensity( Intensity );
                debugCode = 300;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "ApkalluFactionBaseInfo.DoRefreshFromFactionSettings debugCode " + debugCode + " " + e, Verbosity.DoNotShow );
            }
        }

        // Iterates all Apkallu ships on the given planet and refreshes their multi-phase timer.
        // Called by the HandleActiveBreaches() in MalwareDeepInfo
        public void RefreshDiveOnPlanet( Planet planet )
        {
            StateOfMatterTypeData multiPhase = StateOfMatterTypeDataTable.Instance.GetRowByName( "MultiPhase" );
            if ( multiPhase == null )
                return;
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
            {
                if ( entity == null )
                    continue;
                if ( entity.Planet != planet )
                    continue;
                if ( entity.GetIsCrippled() )
                    continue; //once crippled, a ship drops out of multiphase
                if ( !entity.TypeData.GetHasTag("DiveEligible")  ) 
                    continue;

                entity.CurrentStateOfMatter = multiPhase;
                entity.GameSecondWillExitStateOfMatter = World_AIW2.Instance.GameSecond + 3;
            }
        }

        public bool DoesPlanetHaveZiggurat( Planet planet )
        {
            bool foundZiggurat = false;
            foreach ( GameEntity_Squad ziggurat in this.Ziggurats.DisplaySquads() )
            {
                if ( ziggurat.Planet == planet ) {
                    foundZiggurat = true;
                    break;
                }
            }
            return foundZiggurat;
        }
        public GameEntity_Squad GetZigguratForPlanet( Planet planet )
        {
            GameEntity_Squad output = null;
            foreach ( GameEntity_Squad ziggurat in this.Ziggurats.DisplaySquads() )
            {
                if ( ziggurat.Planet == planet ) {
                    output = ziggurat;
                    break;
                }
            }
            return output;
        }
        public bool DoesPlanetHaveTemen( Planet planet )
        {
            bool foundTemen = false;
            foreach ( GameEntity_Squad temen in this.Temens.DisplaySquads() )
            {
                if ( temen.Planet == planet ) {
                    foundTemen = true;
                    break;
                }
            }
            return foundTemen;
        }
        public GameEntity_Squad GetTemenForPlanet( Planet planet )
        {
            GameEntity_Squad output = null;
            foreach ( GameEntity_Squad temen in this.Temens.DisplaySquads() )
            {
                if ( temen.Planet == planet ) {
                    output = temen;
                    break;
                }
            }
            return output;
        }


        public bool DoesPlanetHaveDuru( Planet planet )
        {
            bool foundDuru = false;
            foreach ( GameEntity_Squad duru in this.Durus.DisplaySquads() )
            {
                if ( duru.Planet == planet ) {
                    foundDuru = true;
                    break;
                }
            }
            return foundDuru;
        }

        public override void SetStartingFactionRelationships()
        {
            base.SetStartingFactionRelationships();
            AllegianceHelper.AllyThisFactionToHumans( this.AttachedFaction );
        }

        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                Flagships.ClearConstructionListForStartingConstruction();
                MobileShips.ClearConstructionListForStartingConstruction();
                Outposts.ClearConstructionListForStartingConstruction();
                MalwareDisruptors.ClearConstructionListForStartingConstruction();
                Ziggurats.ClearConstructionListForStartingConstruction();
                Temens.ClearConstructionListForStartingConstruction();
                Durus.ClearConstructionListForStartingConstruction();
                Lamassus.ClearConstructionListForStartingConstruction();
                OutguardGranters.ClearConstructionListForStartingConstruction();
                Locusts.ClearConstructionListForStartingConstruction();
                Pilgrims.ClearConstructionListForStartingConstruction();
                LesserPilgrimProducers.ClearConstructionListForStartingConstruction();

                Rangers.ClearConstructionListForStartingConstruction();
                RangerProducers.ClearConstructionListForStartingConstruction();
                RangerOutposts.ClearConstructionListForStartingConstruction();
                DireRangerOutposts.ClearConstructionListForStartingConstruction();
                RangerCount.ClearConstructionValueForStartingConstruction();
                DireRangerCount.ClearConstructionValueForStartingConstruction();
                RangersPerDuru.ClearConstructionDictForStartingConstruction();
                DireRangersPerDuru.ClearConstructionDictForStartingConstruction();
                RangersPerPlanet.ClearConstructionDictForStartingConstruction();

                LocustsPerLure.ClearConstructionDictForStartingConstruction();
                TotalHopsForLocustsPerLure.ClearConstructionDictForStartingConstruction();

                debugCode = 150;
                if ( Intensity == -1 )
                    DoRefreshFromFactionSettings();
                debugCode = 200;
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    if ( entity == null )
                        continue;
                    if ( entity.TypeData.GetHasTag( "ApkalluFlagship" ) )
                    {
                        Flagships.AddToConstructionList( entity );
                        if ( entity.TypeData.GetHasTag( "ApkalluLamassu" ) )
                            Lamassus.AddToConstructionList( entity );
                        continue;
                    }
                    if ( entity.TypeData.GetHasTag( "ApkalluOutpost" ) )
                    {
                        entity.CreateExternalBaseInfo<ApkalluPerUnitBaseInfo>( "ApkalluPerUnitBaseInfo" );
                        Outposts.AddToConstructionList( entity );
                        continue;
                    }
                    if ( entity.TypeData.GetHasTag( "MalwareDisruptor" ) )
                    {
                        entity.CreateExternalBaseInfo<ApkalluPerUnitBaseInfo>( "ApkalluPerUnitBaseInfo" );
                        if ( entity.SelfBuildingMetalRemaining > 0 )
                            continue;
                        MalwareDisruptors.AddToConstructionList( entity );
                        continue;
                    }

                    if ( entity.TypeData.GetHasTag( "ApkalluZiggurat" ) )
                    {
                        entity.CreateExternalBaseInfo<ApkalluPerUnitBaseInfo>( "ApkalluPerUnitBaseInfo" );
                        Ziggurats.AddToConstructionList( entity );
                        continue;
                    }
                    if ( entity.TypeData.GetHasTag( "ApkalluTemen" ) )
                    {
                        entity.CreateExternalBaseInfo<ApkalluPerUnitBaseInfo>( "ApkalluPerUnitBaseInfo" );
                        Temens.AddToConstructionList( entity );
                        continue;
                    }

                    if ( entity.TypeData.GetHasTag( "ApkalluLamassu" ) )
                    {
                        Lamassus.AddToConstructionList( entity );
                        continue;
                    }
                    if ( entity.TypeData.GetHasTag( "OutguardGranter" ) )
                    {
                        OutguardGranters.AddToConstructionList( entity );
                        continue;
                    }

                    if ( entity.TypeData.GetHasTag( "ApkalluDuru" ) )
                    {
                        Durus.AddToConstructionList( entity );
                        continue;
                    }
                    if ( entity.TypeData.GetHasTag( "ApkalluPilgrim" ) )
                    {
                        Pilgrims.AddToConstructionList( entity );
                        continue;
                    }
                    if ( entity.TypeData.GetHasTag( "ProducesLesserPilgrims" ) )
                    {
                        entity.CreateExternalBaseInfo<ApkalluPerUnitBaseInfo>( "ApkalluPerUnitBaseInfo" );
                        LesserPilgrimProducers.AddToConstructionList( entity );
                        continue;
                    }

                    if (entity.TypeData.GetHasTag("ProducesRangers") )
                    {
                        entity.CreateExternalBaseInfo<ApkalluPerUnitBaseInfo>( "ApkalluPerUnitBaseInfo" );
                        RangerProducers.AddToConstructionList(entity);
                    }
                    if (entity.TypeData.GetHasTag("ApkalluRanger"))
                    {
                        ApkalluPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ApkalluPerUnitBaseInfo>( "ApkalluPerUnitBaseInfo" );
                        if ( data.HomeDuruSafe.GetSquad() == null &&
                             data.HomeDuru.GetSquad() != null)
                        {
                            data.HomeDuruSafe = SafeSquadWrapper.Create(data.HomeDuru.GetSquad());
                        }
                        if (data.HomeDuruSafe.GetSquad() != null)
                        {
                            RangersPerDuru.Construction[data.HomeDuruSafe]++;
                        }
                        RangerCount.Construction++;
                        RangersPerPlanet.Construction[entity.Planet]++;
                    }
                    if (entity.TypeData.GetHasTag("ApkalluDireRanger"))
                    {
                        ApkalluPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ApkalluPerUnitBaseInfo>( "ApkalluPerUnitBaseInfo" );
                        if ( data.HomeDuruSafe.GetSquad() == null &&
                             data.HomeDuru.GetSquad() != null)
                        {
                            data.HomeDuruSafe = SafeSquadWrapper.Create(data.HomeDuru.GetSquad());
                        }

                        if ( data.HomeDuruSafe.GetSquad() != null )
                            DireRangersPerDuru.Construction[data.HomeDuruSafe]++;
                        DireRangerCount.Construction++;
                        RangersPerPlanet.Construction[entity.Planet]++;
                    }
                    if ( entity.TypeData.GetHasTag( "BoostsRangerCap" ) &&
                         entity.SelfBuildingMetalRemaining <= 0 && //not under construction
                         entity.SecondsSpentAsRemains <= 0 ) //not remains
                    {
                        ApkalluPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ApkalluPerUnitBaseInfo>("ApkalluPerUnitBaseInfo");
                        if ( entity.SelfBuildingMetalRemaining > 0 )
                            continue;

                        RangerOutposts.AddToConstructionList( entity );
                    }
                    if ( entity.TypeData.GetHasTag( "BoostsDireRangerCap" ) &&
                         entity.SelfBuildingMetalRemaining <= 0 && //not under construction
                         entity.SecondsSpentAsRemains <= 0 ) //not remains
                    {
                        ApkalluPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ApkalluPerUnitBaseInfo>("ApkalluPerUnitBaseInfo");
                        if ( entity.SelfBuildingMetalRemaining > 0 )
                            continue;

                        DireRangerOutposts.AddToConstructionList( entity );
                    }

                    if ( entity.TypeData.GetHasTag( "AutoDefenseShip" ) )
                    {
                        Rangers.AddToConstructionList(entity);
                        ApkalluPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ApkalluPerUnitBaseInfo>( "ApkalluPerUnitBaseInfo" );
                        GameEntity_Squad HomeDuru = data.HomeDuru.GetSquad();
                        if ( entity.PlanetFaction.Faction != HomeDuru?.PlanetFaction.Faction ) {
                            HomeDuru = null;
                            data.HomeDuru.Clear();
                        }
                        if ( HomeDuru == null ) {
                            entity.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut ); //if we've lost our Duru, we die
                        }
                    }

                    if ( entity.TypeData.GetHasTag( "ApkalluLocust" ) )
                    {
                        entity.CreateExternalBaseInfo<ApkalluPerUnitBaseInfo>( "ApkalluPerUnitBaseInfo" );
                        Locusts.AddToConstructionList( entity );
                        continue;
                    }

                    MobileShips.AddToConstructionList( entity );
                }
                Flagships.SwitchConstructionToDisplay();
                MobileShips.SwitchConstructionToDisplay();
                Outposts.SwitchConstructionToDisplay();
                MalwareDisruptors.SwitchConstructionToDisplay();
                Ziggurats.SwitchConstructionToDisplay();
                Temens.SwitchConstructionToDisplay();
                Durus.SwitchConstructionToDisplay();
                Lamassus.SwitchConstructionToDisplay();
                OutguardGranters.SwitchConstructionToDisplay();
                Locusts.SwitchConstructionToDisplay();
                Pilgrims.SwitchConstructionToDisplay();
                LesserPilgrimProducers.SwitchConstructionToDisplay();

                Rangers.SwitchConstructionToDisplay();
                RangerProducers.SwitchConstructionToDisplay();
                RangerOutposts.SwitchConstructionToDisplay();
                DireRangerOutposts.SwitchConstructionToDisplay();
                RangerCount.SwitchConstructionToDisplay();
                RangersPerDuru.SwitchConstructionToDisplay();
                DireRangersPerDuru.SwitchConstructionToDisplay();
                RangersPerPlanet.SwitchConstructionToDisplay();
                DireRangerCount.SwitchConstructionToDisplay();

                debugCode = 350;

                // Rebuild the desync cache: which Lamassus have tags that differ from what
                // their Ziggurat's planet currently provides.
                LamassuPrimaryKeyIDsOutOfSync.Clear();
                foreach ( GameEntity_Squad lamassu in Lamassus.DisplaySquads() )
                {
                    // Find the Ziggurat that owns this Lamassu.
                    GameEntity_Squad ownerZiggurat = null;
                    foreach ( GameEntity_Squad ziggurat in Ziggurats.DisplaySquads() )
                    {
                        ApkalluPerUnitBaseInfo zigUnit = ziggurat.TryGetExternalBaseInfoAs<ApkalluPerUnitBaseInfo>();
                        if ( zigUnit != null && zigUnit.LamassuEntityPrimaryKeyID == lamassu.PrimaryKeyID )
                        {
                            ownerZiggurat = ziggurat;
                            break;
                        }
                    }
                    if ( ownerZiggurat?.Planet == null ) continue;
                    // Collect ApkalluSummoner* tags from ZigguratSummoner structures on the Ziggurat's planet.
                    desyncTagScratch.Clear();
                    foreach ( GameEntity_Squad entity in ownerZiggurat.Planet.Squads() )
                    {
                        if ( entity.GetFactionOrNull_Safe() != this.AttachedFaction )
                            continue;
                        if ( entity.TypeData.GetHasTag( "ApkalluZigguratSummoner" ) )
                        {
                            List<string> tags = entity.TypeData.TagsList;
                            if ( tags != null )
                                for ( int t = 0; t < tags.Count; t++ )
                                    if ( tags[t].StartsWith( "ApkalluSummoner" ) && !desyncTagScratch.Contains( tags[t] ) )
                                        desyncTagScratch.Add( tags[t] );
                        }
                    }
                    List<string> activeTags = lamassu.GetEnabledSummonerTagsDirect();
                    int activeCount = activeTags == null ? 0 : activeTags.Count;
                    bool differs = activeCount != desyncTagScratch.Count;
                    if ( !differs )
                        for ( int t = 0; t < desyncTagScratch.Count && !differs; t++ )
                            if ( activeTags == null || !activeTags.Contains( desyncTagScratch[t] ) )
                                differs = true;
                    if ( differs )
                        LamassuPrimaryKeyIDsOutOfSync.Add( lamassu.PrimaryKeyID );
                }

                debugCode = 360;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "ApkalluFactionBaseInfo Stage2 debugCode " + debugCode + " " + e, Verbosity.DoNotShow );
            }
        }
        #endregion

        #region DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            AllegianceHelper.AllyThisFactionToHumans( this.AttachedFaction );
        }
        #endregion
        public static bool GetIsThisAnApkalluFaction( Faction fac )
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
                case "ApkalluSidekick":
                    return true;
                case "ApkalluInfusedEmpire":
                    return true; 

            }
            return false;
        }
        public static int GetApkalluFactionCount()
        {
            int count = 0;
            PlayerTypeData playerType;
            
            playerType = PlayerTypeDataTable.Instance.GetRowByName( "ApkalluSidekick", LookupSwapAllowed.Yes, true );
            if (playerType != null)
                count += playerType.CurrentFactionsInThisGame.Count;

            playerType = PlayerTypeDataTable.Instance.GetRowByName( "ApkalluInfusedEmpire", LookupSwapAllowed.Yes, true );
            if (playerType != null)
                count += playerType.CurrentFactionsInThisGame.Count;

            return count;
        }
        public bool IsDaemonBlocked( GameEntityTypeData daemon )
        {
            for ( int i = 0; i < CompletedBreaches.Count; i++ )
            {
                MalwareBreach breach = CompletedBreaches[i];
                if ( breach?.MalwareEntityTypeBlocked == daemon )
                    return true;
            }
            return false;
        }

        #region CheckIfPlayerFactionShouldGetRewardBasedOnAUnitDying
        public override void CheckIfPlayerFactionShouldGetRewardBasedOnAUnitDying( Faction factionThatKilledEntityOrNull, bool IsFromOnlyPartOfStackDying, GameEntity_Squad entity,
            DamageSource Damage, EntitySystem FiringSystemOrNull, int numExtraStacksKilled, ArcenHostOnlySimContext Context )
        {
            if ( IsFromOnlyPartOfStackDying )
                return;
            if ( entity.PlanetFaction?.Faction == this.AttachedFaction )
                return; // don't reward ourselves for our own deaths

            // Apkallu resource income from killing enemies
            DLC3GameEntityTypeDataExtension entityDLC3Data = entity.TypeData.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );
            if ( entityDLC3Data != null )
            {
                entityDLC3Data.GetApkalluResourcesToGrantOnDeath( entity,
                    out FInt scienceToGrant, out FInt hackingToGrant, out FInt metalToGrant,
                    out FInt resourceOneToGrant, out FInt resourceTwoToGrant, out FInt resourceThreeToGrant );

                if ( scienceToGrant > FInt.Zero )
                    this.AttachedFaction.StoredScience += scienceToGrant;
                if ( hackingToGrant > FInt.Zero )
                    this.AttachedFaction.StoredHacking += hackingToGrant;
                if ( metalToGrant > FInt.Zero )
                    this.AttachedFaction.StoredMetal += metalToGrant;
                if ( resourceOneToGrant > FInt.Zero )
                    this.AttachedFaction.StoredFactionResourceOne += resourceOneToGrant;
                if ( resourceTwoToGrant > FInt.Zero )
                    this.AttachedFaction.StoredFactionResourceTwo += resourceTwoToGrant;
                if ( resourceThreeToGrant > FInt.Zero )
                    this.AttachedFaction.StoredFactionResourceThree += resourceThreeToGrant;
            }

            // Post-betrayal: Malware on-death effects on AI structures and ships
            Faction malwareFaction = FactionUtilityMethods.Instance.GetMalwareForApkalluFaction();
            if ( malwareFaction == null || factionThatKilledEntityOrNull != malwareFaction )
                return;
            MalwareFactionBaseInfo malwareInfo = malwareFaction.TryGetExternalBaseInfoAs<MalwareFactionBaseInfo>();
            if ( malwareInfo == null || !malwareInfo.HasDoneInvasion )
                return;

            SpecialEntityType specialType = entity.TypeData.SpecialType;
            if ( specialType == SpecialEntityType.GuardPost )
            {
                ConvertGuardPostToMalwareAndSpawnSpikes( entity, malwareFaction, 3, Context );
            }
            else if ( specialType == SpecialEntityType.DireGuardPost )
            {
                ConvertGuardPostToMalwareAndSpawnSpikes( entity, malwareFaction, 5, Context );
            }
            else if ( entity.TypeData.GetHasTag( "AIOverlordPhase2_AnyType" ) )
            {
                SpawnArchonOnOverlordKill( entity, malwareFaction, Context );
            }
        }

        private static void SpawnArchonOnOverlordKill( GameEntity_Squad overlord, Faction malwareFaction, ArcenHostOnlySimContext Context )
        {
            GameEntityTypeData archonType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "MalwareArchon" );
            if ( archonType == null )
            {
                ArcenDebugging.LogSingleLine( "Malware SpawnArchonOnOverlordKill: could not find MalwareArchon entity type", Verbosity.DoNotShow );
                return;
            }
            Planet planet = overlord.Planet;
            PlanetFaction malwarePFaction = planet.GetPlanetFactionForFaction( malwareFaction );
            if ( malwarePFaction == null )
                return;
            FInt minRange = FInt.FromParts( 0, 50 );
            FInt maxRange = FInt.FromParts( 0, 150 );
            ArcenPoint spawnPoint = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, archonType, overlord.WorldLocation, minRange, maxRange );
            GameEntity_Squad archon = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( malwarePFaction, archonType, 1,
                malwarePFaction.Faction.LooseFleet, 0, spawnPoint, Context, "MalwareBetrayalArchon" );
            if ( archon == null )
                return;
            archon.ShouldNotBeConsideredAsThreatToHumanTeam = true;
            archon.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
            archon.CreateExternalBaseInfo<MalwarePerUnitBaseInfo>( "MalwarePerUnitBaseInfo" );
        }

        private static void ConvertGuardPostToMalwareAndSpawnSpikes( GameEntity_Squad guardPost, Faction malwareFaction, int numSpikes, ArcenHostOnlySimContext Context )
        {
            Planet planet = guardPost.Planet;
            ArcenPoint location = guardPost.WorldLocation;
            byte markLevel = guardPost.CurrentMarkLevel;

            PlanetFaction malwarePFaction = planet.GetPlanetFactionForFaction( malwareFaction );
            if ( malwarePFaction == null )
                return;

            // TransferEntityToFaction is not safe here 鈥?the entity is already dead when this fires.
            // Spawn a new entity of the same type owned by Malware, matching the nanocaust pattern.
            GameEntity_Squad convertedPost = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( malwarePFaction, guardPost.TypeData, markLevel,
                malwarePFaction.Faction.LooseFleet, 0, location, Context, "MalwareBetrayalConvert" );
            if ( convertedPost != null )
            {
                convertedPost.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                convertedPost.CreateExternalBaseInfo<MalwarePerUnitBaseInfo>( "MalwarePerUnitBaseInfo" );
            }

            FInt minRange = FInt.FromParts( 0, 50 );
            FInt maxRange = FInt.FromParts( 0, 150 );

            for ( int i = 0; i < numSpikes; i++ )
            {
                GameEntityTypeData spikeType = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MalwareDaemonSpike" );
                if ( spikeType == null )
                    break;
                ArcenPoint spawnPoint = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, spikeType, location, minRange, maxRange );
                GameEntity_Squad spike = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( malwarePFaction, spikeType, markLevel,
                    malwarePFaction.Faction.LooseFleet, 0, spawnPoint, Context, "MalwareBetrayalSpike" );
                if ( spike == null )
                    continue;
                spike.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                spike.CreateExternalBaseInfo<MalwarePerUnitBaseInfo>( "MalwarePerUnitBaseInfo" );
            }
        }
        #endregion

    }
}
