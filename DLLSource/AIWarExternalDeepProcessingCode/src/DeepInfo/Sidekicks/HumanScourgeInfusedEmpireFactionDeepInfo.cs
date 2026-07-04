using System;
using Arcen.AIW2.Core;
using Arcen.Universal;



namespace Arcen.AIW2.External
{

    public sealed class ScourgeInfusedHumanEmpireFactionDeepInfo : ExternalFactionDeepInfoRoot, IExternalDeepInfo_Singleton
    {
        public ScourgeInfusedHumanEmpireFactionBaseInfo BaseInfo;
        public Faction VassalFaction;
        public ScourgeVassalFactionBaseInfo VassalBaseInfo;
        private static readonly Dictionary<GameEntityTypeData, int> AttackComposition = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed(500, "DysonFactionDeepInfo-AttackComposition");
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<ScourgeInfusedHumanEmpireFactionBaseInfo>();
            this.VassalBaseInfo = this.BaseInfo.GetVassalBaseInfo();
            this.VassalFaction = this.BaseInfo.VassalFaction;
        }

        protected override void Cleanup()
        {
            BaseInfo = null;
            AttackComposition.Clear();

        }
        public override void DoOnSelfBuildingCompleteLogic_HostOnly( GameEntity_Squad entity, ArcenHostOnlySimContext Context )
        {
        
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ScourgeEpmire-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f ) : null;
            int debugCode = 0;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                if ( VassalBaseInfo == null )
                {
                    this.BaseInfo.GetVassalBaseInfo();
                }

                debugCode = 1000;
                InitializeTech();
                SpawnBanes(Context);
                SpawnNadir(Context);
                HandleFlagshipUpgrades( Context);
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( EntityRollupType.MobileFleetFlagships ) )
                {
                    Fleet fleet = entity.GetFleetOrNull_Safe();
                    if ( fleet != null && !(fleet.BaseInfo is HumanMobileFleetBaseInfo) )
                        fleet.CreateExternalBaseInfo<HumanMobileFleetBaseInfo>( "HumanMobileFleetBaseInfo" );
                }

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Hit exception in scourge empire stage3 sim. debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();

                #region Tracing
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
            }
        }
        private void SpawnNadir(ArcenHostOnlySimContext Context)
        {
            int debugCode = 0;
            try{
                int intervalForNadirTierTwo = 180;
                int intervalForNadirTierOne = 40;
                int numTierOneToSpawn = 3;
                int timeAdjust = 0;
                int WaveInterval = 1290;
                if ( BaseInfo.Difficulty < 5 )
                {
                    timeAdjust += (BaseInfo.Difficulty * 5);
                }
                if ( BaseInfo.Difficulty > 5 )
                {
                    timeAdjust -= BaseInfo.Difficulty;
                }
                intervalForNadirTierOne += timeAdjust;
                intervalForNadirTierTwo += timeAdjust;

                if ( World_AIW2.Instance.GameSecond % intervalForNadirTierOne == 0 )
                {
                    Faction aiFaction = World_AIW2.GetRandomAIFaction( Context );
                    if ( aiFaction == null )
                        return;
                    GameEntity_Squad king = FactionUtilityMethods.Instance.findKing( aiFaction );
                    if ( king == null )
                        return;
                    AISentinelsFactionBaseInfo aiBaseInfo = aiFaction.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
                    PlanetFaction pFaction = king.PlanetFaction;
                    for ( int i = 0; i < numTierOneToSpawn; i++ )
                    {
                        int rand = Context.RandomToUse.Next(0, 100 );

                        if ( rand < 20)
                        {
                            pFaction = king.Planet.GetPlanetFactionForFaction(aiBaseInfo.SubFac_Warden );
                        }
                        else if ( rand < 40)
                            pFaction = king.Planet.GetPlanetFactionForFaction(aiBaseInfo.SubFac_BorderAggression );
                        else if ( rand < 60)
                            pFaction = king.Planet.GetPlanetFactionForFaction(aiBaseInfo.SubFac_Hunter );
                        else if ( rand < 80)
                            pFaction = king.Planet.GetPlanetFactionForFaction(aiBaseInfo.SubFac_Praetorian );
                        GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "AntiDarkZenithTierOne" );
                        ArcenPoint spawnLocation = king.Planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                        GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, pFaction.Faction.CurrentGeneralMarkLevel,
                            pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "SpawnAntiScourgeOne" );
                        if ( newEntity == null )
                            return;
                        newEntity.HullPointsLost = 0;
                        newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                    }
                }
                if ( World_AIW2.Instance.GameSecond % intervalForNadirTierTwo == 0 )
                {
                    Faction aiFaction = World_AIW2.GetRandomAIFaction( Context );
                    if ( aiFaction == null )
                        return;
                    GameEntity_Squad king = FactionUtilityMethods.Instance.findKing( aiFaction );
                    if ( king == null )
                        return;
                    AISentinelsFactionBaseInfo aiBaseInfo = aiFaction.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
                    PlanetFaction pFaction = king.PlanetFaction;
                    int rand = Context.RandomToUse.Next(0, 100 );
                    if ( rand < 20)
                        pFaction = king.Planet.GetPlanetFactionForFaction(aiBaseInfo.SubFac_Warden );
                    else if ( rand < 40)
                        pFaction = king.Planet.GetPlanetFactionForFaction(aiBaseInfo.SubFac_BorderAggression );
                    else if ( rand < 60)
                        pFaction = king.Planet.GetPlanetFactionForFaction(aiBaseInfo.SubFac_Hunter );
                    else if ( rand < 80)
                        pFaction = king.Planet.GetPlanetFactionForFaction(aiBaseInfo.SubFac_Praetorian );

                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "AntiDarkZenithTierTwo" );
                    ArcenPoint spawnLocation = king.Planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, pFaction.Faction.CurrentGeneralMarkLevel,
                        pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "SpawnAntiScourgeTwo" );
                    if ( newEntity == null )
                        return;
                    newEntity.HullPointsLost = 0;
                    newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread

                }
                if ( World_AIW2.Instance.GameSecond % WaveInterval == 0 )
                {
                    Faction aiFaction = World_AIW2.GetRandomAIFaction( Context );
                    if ( aiFaction == null )
                        return;
                    GameEntity_Squad king = FactionUtilityMethods.Instance.findKing( aiFaction );
                    if ( king == null )
                        return;
                    AISentinelsFactionBaseInfo aiBaseInfo = aiFaction.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
                    int WaveSize = aiBaseInfo.GetSpecificBudgetThreshold( AIBudgetType.Wave, GlobalAIWorldBaseInfo.Instance.AIProgress_Effective );
                    SpawnNadirForce( king.Planet, aiBaseInfo.SubFac_BorderAggression, WaveSize, Context, AttackComposition);
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Hit exception in scourge empire spawn nadir. debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        private void SpawnNadirForce( Planet planet, Faction faction, int strength, ArcenHostOnlySimContext Context, Dictionary<GameEntityTypeData, int> AttackCompositionToFill )
        {
            AttackCompositionToFill.Clear();
            int debugCode = 0;
            try{
                bool debug = false;
                string tagOne = "AntiDarkZenithTierOne";
                string tagTwo = "AntiDarkZenithTierTwo";
                int ratio = 8;
                int origStr = strength;
                int attempts = 100;
                debugCode = 100;
                if ( debug )
                    ArcenDebugging.LogSingleLine("spawning a nadir force with strength " + strength, Verbosity.DoNotShow );
                while ( strength > 0 && attempts > 0)
                {
                    debugCode = 200;
                    string tag = tagOne;
                    GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, tagOne );
                    if ( attempts % ratio == 0 )
                        tag = tagTwo;
                    typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, tag );
                    if ( typeData == null )
                        throw new Exception("Could not find entity with tag " + tag + " in the SpawnRavagerForces code path");
                    if ( strength - typeData.CostForAIToPurchase < 0 )
                    {
                        attempts--;
                        continue;
                    }
                    strength -= typeData.CostForAIToPurchase;
                    AttackComposition[typeData]++;
                }
                foreach ( KeyValuePair<GameEntityTypeData, int> pair in AttackComposition )
                {
                    debugCode = 800;
                    int totalSquadsToSpawn = pair.Value;
                    GameEntityTypeData entityType = pair.Key;
                    int numStacksPerSquad = 0;
                    int separateSquadsToSpawn = totalSquadsToSpawn;
                    int remainder = 0;
                    int StackingCutoff = AIWar2GalaxySettingQuickAccess.StackingCutoffNPCs;
                    if (debug)
                        ArcenDebugging.LogSingleLine("\tSpawning " + totalSquadsToSpawn + " of " + pair.Key.GetDisplayName(), Verbosity.DoNotShow );
                    
                    if (StackingCutoff <= 0)
                        throw new Exception("Undefined StackingCutoffNPCs; this means that waves won't spawn");
                    if (totalSquadsToSpawn > StackingCutoff)
                    {
                        separateSquadsToSpawn = StackingCutoff;
                        numStacksPerSquad = totalSquadsToSpawn / separateSquadsToSpawn;
                        remainder = totalSquadsToSpawn % separateSquadsToSpawn;
                    }
                    debugCode = 900;
                    for (int j = 0; j < separateSquadsToSpawn; j++)
                    {
                        debugCode = 1000;
                        FInt minRadius = FInt.FromParts(0, 600);
                        FInt maxRadius = FInt.FromParts(0, 800);
                        
                        ArcenPoint spawnLocation = Engine_AIW2.Instance.CombatCenter;
                        spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity(Context, entityType, spawnLocation, minRadius, maxRadius);
                        byte markLevel = (byte)(FactionUtilityMethods.Instance.GetCurrentAIP() / 60);
                        if (markLevel < 1)
                            markLevel = 1;
                        if (markLevel > 7)
                            markLevel = 7;
                        GameEntity_Squad entity = faction.SpawnNewUnit_ReturnNullIfMPClient(
                            Context, planet, spawnLocation, entityType, markLevel,
                            this.AttachedFaction.LooseFleet, 0,
                            EntityBehaviorType.Attacker_Full, -1, null, "NadirAttackDeployment");
                        
                        if (entity == null)
                            continue;
                        
                        if (numStacksPerSquad > 0)
                        {
                            if (totalSquadsToSpawn < numStacksPerSquad)
                            {
                                entity.AddOrSetExtraStackedSquadsInThis((Int16)totalSquadsToSpawn, true);
                            }
                            else
                                entity.AddOrSetExtraStackedSquadsInThis((Int16)(numStacksPerSquad - 1), true); //don't count the original unit
                            if (remainder > 0)
                            {
                                entity.AddOrSetExtraStackedSquadsInThis(1, false);
                                remainder--;
                            }
                            totalSquadsToSpawn -= entity.ExtraStackedSquadsInThis + 1;
                        }
                    }
                };
            }catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in SpawnNadir debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        private void SpawnBanes(ArcenHostOnlySimContext Context)
        {
            int baneSpawnInterval = 3600;
            if ( World_AIW2.Instance.GameSecond % baneSpawnInterval != 0 )
                return;
            Faction aiFaction = World_AIW2.GetRandomAIFaction( Context );
            if ( aiFaction == null )
                return;
            GameEntity_Squad king = FactionUtilityMethods.Instance.findKing( aiFaction );
            if ( king == null )
                return;
            AISentinelsFactionBaseInfo aiBaseInfo = aiFaction.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
            if ( aiBaseInfo == null )
                return;
            PlanetFaction pFaction = king.Planet.GetPlanetFactionForFaction(aiBaseInfo.SubFac_Hunter );
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "AntiScourgeLeader" );
            ArcenPoint spawnLocation = king.Planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
            GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, pFaction.Faction.CurrentGeneralMarkLevel,
                pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "SpawnAntiScourge" );
            if ( newEntity == null )
                return;
            newEntity.HullPointsLost = 0;
            newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
            newEntity.HasNotYetBeenFullyClaimed = false;
        }
        private void HandleFlagshipUpgrades(ArcenHostOnlySimContext Context)
        {
            List<SafeSquadWrapper> flagships = this.BaseInfo.Flagships.GetDisplayList();
            if ( World_AIW2.Instance.GameSecond % 3 != 0 )
                return;
            for (int i = 0; i < flagships.Count; i++)
            {
                GameEntity_Squad flagship = flagships[i].GetSquad();
                if (flagship == null)
                    continue;
                ScourgePerUnitBaseInfo data = flagship.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                if (data == null)
                    continue;
                if ( flagship.TypeData.KillsToTriggerTransformation <= 0 )
                    continue;
                if ( data.UnitsKilled < flagship.TypeData.KillsToTriggerTransformation)
                    continue;
                if ( flagship.TypeData.TransformAfterKills != null )
                {
                    GameEntityTypeData entityData = flagship.TypeData.TransformAfterKills;
                    flagship.TransformInto(Context, entityData, 1, true);
                }
            }
        }
        private void InitializeTech()
        {
            int debugCode = 0;
            try{
                debugCode = 100;
                if ( World_AIW2.Instance.GameSecond != 1 )
                    return;
                //There is one armory at the beginning of the game; figure out which type it is, then unlock
                //the player's level 1 tech of that type
                debugCode = 200;
                if ( VassalBaseInfo == null )
                    throw new Exception("VassalBaseInfo is null");
                if ( VassalBaseInfo.ArmoriesInGalaxy == null )
                    throw new Exception("Armories are null");
                foreach ( GameEntity_Squad entity in VassalBaseInfo.ArmoriesInGalaxy.DisplaySquads() )
                {
                    debugCode = 300;
                    ScourgePerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                    if ( data == null )
                        throw new Exception("no scourge per unit data");
                    ScourgeTypeData scourgeTypeData = ScourgeTypeDataTable.Instance.GetRowById( data.ScourgeTypeId );
                    if ( scourgeTypeData == null )
                        throw new Exception("Could not find scourgeTypeData for id " + data.ScourgeTypeId);
                    debugCode = 400;
                    string techName = "";
                    if ( scourgeTypeData.TagForWarrior == "ScourgeBurlust")
                    {
                        techName = "ScourgeBurlustUnlock";
                    }
                    if ( scourgeTypeData.TagForWarrior == "ScourgeEvuck")
                    {
                        techName = "ScourgeEvuckUnlock";
                    }
                    if ( scourgeTypeData.TagForWarrior == "ScourgeThoraxian")
                    {
                        techName = "ScourgeThoraxianUnlock";
                    }
                    if ( scourgeTypeData.TagForWarrior == "ScourgePeltian")
                    {
                        techName = "ScourgePeltianUnlock";
                    }
                    if ( scourgeTypeData.TagForWarrior == "ScourgeNeinzul")
                    {
                        techName = "ScourgeNeinzulUnlock";
                    }
                    if ( scourgeTypeData.TagForWarrior == "ScourgeSpire")
                    {
                        techName = "ScourgeSpireUnlock";
                    }
                    if ( scourgeTypeData.TagForWarrior == "ScourgeZenith")
                    {
                        techName = "ScourgeZenithUnlock";
                    }
                    FInt techCost = FInt.FromParts(100, 000);
                    ArcenDebugging.LogSingleLine("Automatically unlocking " + techName + " we hope " + techCost + " is the cost for that", Verbosity.DoNotShow );
                    debugCode = 1000;
                    this.AttachedFaction.StoredFactionResourceOne = techCost; //so we can spend it instantly
                    GameCommand command = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.UnlockTech], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer);
                    debugCode = 1100;
                    command.RelatedFactionIndex = this.AttachedFaction.FactionIndex;
                    command.RelatedString = techName;
                    debugCode = 1200;
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true);
                    break;
                }
            } catch (Exception e )
            {
                ArcenDebugging.LogSingleLine("Exception in InitializeTech debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ScourgeEmpire-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            int debugCode = 0;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                if ( BaseInfo == null )
                    return;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in scourge infused empire LRP, debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();

                #region Tracing
                if ( tracing && !tracingBuffer.GetIsEmpty() ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " " + AttachedFaction.FactionIndex + " DoLongRangePlanning trace ends" );
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
            }
            FleetBehaviorLRP.DoLRP( AttachedFaction, Context );
        }


        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            //            FallenSpireSharedDeepInfo.Instance.SpawnDragons( Context );
            ConfigurationForFaction player_cfg = AttachedFaction.Config;
            if ( ! player_cfg.GetBoolValueForCustomFieldOrDefaultValue("StartsWithBane", true) )
                return;
            foreach ( GameEntity_Squad king in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                if ( king.PlanetFaction.Faction != this.AttachedFaction)
                    continue;
                GameEntityTypeData baneData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ScourgeInfusedFlagshipTierOne" );
                king.Planet.Mapgen_SeedEntity( Context, World_AIW2.Instance.GetNeutralFaction(), baneData, PlanetSeedingZone.MostAnywhere );
                break;
            }
        }

        public override void DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly( ref int debugStage, GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull,
              Faction factionThatKilledEntity, Faction entityOwningFaction, int numExtraStacksKilled, ArcenHostOnlySimContext Context )
        {
            int debugCode = 0;
            try{
                debugCode = 100;
                GameEntity_Squad killingSquad = null;
                if ( FiringSystemOrNull != null )
                {
                    killingSquad = FiringSystemOrNull.ParentEntity;
                }
                debugCode = 200;
                if ( killingSquad == null )
                    return;
                debugCode = 300;
                FleetMembership fMem = killingSquad.FleetMembership;
                if ( fMem == null )
                    return;
                debugCode = 400;
                Fleet fleet = fMem.Fleet;
                if ( fleet == null )
                    return;
                debugCode = 500;
                GameEntity_Squad flagship = fleet.Centerpiece.GetSquad();
                if ( flagship == null )
                    return;
                debugCode = 600;
                if ( flagship.TypeData.KillsToTriggerTransformation <= 0 ||
                     flagship.TypeData.TransformAfterKills == null )
                    return;
                debugCode = 700;
                ScourgePerUnitBaseInfo data = flagship.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                if ( data == null )
                    return;
                data.UnitsKilled++;
            } catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly Scourge Infused Empire debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }

        public override bool SeedUnitsOnStartingPlanetDuringMapGen( Planet StartingPlanet, ConfigurationForFaction factionConfig, PlanetFaction pFaction, 
            ref ArcenPoint commandStationPoint, ref bool stillNeedsToSeedHumanHomeworldStuff, 
            Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull, ArcenHostOnlySimContext Context )
        {
            int debugIndex = 0;
            try
            {
                debugIndex = 10;

                GameEntityTypeData humanKingUnitData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "NormalHumanEmpireHumanHomeCommand" );

                if ( humanKingUnitData == null )
                    throw new Exception( "No humanKingUnitData could be found!  Evidently nothing has the tag of NormalHumanEmpireHumanHomeCommand." );

                debugIndex = 100;
                int offsetDistanceMin = humanKingUnitData.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius * 2;
                int offsetDistanceMax = offsetDistanceMin * 3;
                int minDistance = 30000;
                int loop = 0;

                debugIndex = 200;

                commandStationPoint = ArcenPoint.OutOfRange;
                do
                {
                    if ( commandStationPoint == ArcenPoint.OutOfRange )
                        commandStationPoint = StandardMapPopulator.EnsureFarFromWormholesIfPossible( offsetDistanceMin, offsetDistanceMax, minDistance,
                            30, Engine_AIW2.Instance.CombatCenter, StartingPlanet, Context );

                    minDistance -= 1000;
                }
                while ( loop++ < 30 && commandStationPoint == ArcenPoint.OutOfRange );
                
                GameEntity_Squad king = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, humanKingUnitData, humanKingUnitData.MarkFor( pFaction ),
                               pFaction.FleetUsedAtPlanet, 0, commandStationPoint, Context, "ScourgeInfusedEmpireStartSpawn" );

                stillNeedsToSeedHumanHomeworldStuff = false;
                debugIndex = 500;
                pFaction.SetPlanetFactionBooleanFlag( PlanetFactionBooleanFlag.TryToCapture, true );
                debugIndex = 600;

                FInt placementOffsetScale = FInt.FromParts( 1, 500 );

                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeForcefield )
                    StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, commandStationPoint, pFaction, placementOffsetScale, "StartingForcefieldGenerator", -400, 0 );
                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeEngineers )
                {
                    StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, commandStationPoint, pFaction, placementOffsetScale, "Engineer", -750, 400 );
                    StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, commandStationPoint, pFaction, placementOffsetScale, "Engineer", -600, 400 );
                }
                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeFactory )
                    StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, commandStationPoint, pFaction, placementOffsetScale, "Factory", 700, 0 );

                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeHumanSettlement )
                {
                    int numberOfHomeHumanSettlements = factionConfig.GetIntValueForCustomFieldOrDefaultValue( "HomeHumanSettlementsToStartWith", true );

                    for ( int index = 0; index < numberOfHomeHumanSettlements; index++ )
                        StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, commandStationPoint, pFaction, placementOffsetScale, "HomeHumanSettlement", -1000 + (200 * index), -400 );
                }

                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeHumanCryogenicPods )
                {
                    int numberOfHumanCryoPods = factionConfig.GetIntValueForCustomFieldOrDefaultValue( "HumanCryogenicPodsToStartWith", true );
                    int cryoPodsSoFarThisRow = 0;
                    int cryoPodYoffsetDistanceMax = -600;
                    for ( int index = 0; index < numberOfHumanCryoPods; index++ )
                    {
                        if ( cryoPodsSoFarThisRow >= 10 )
                        {
                            cryoPodsSoFarThisRow = 0;
                            cryoPodYoffsetDistanceMax -= 120;
                        }
                        StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, commandStationPoint, pFaction, placementOffsetScale, "HumanCryogenicPod", -1000 + (120 * cryoPodsSoFarThisRow), cryoPodYoffsetDistanceMax );
                        cryoPodsSoFarThisRow++;
                    }
                }
                debugIndex = 2000;
                int innerSystemMinimumRadius = (StartingPlanet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 150 )).IntValue;
                int innerSystemMaximumRadius = (StartingPlanet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 300 )).IntValue;

                debugIndex = 2100;

                if ( !World_AIW2.Instance.GetIsTutorial() ) //test ships only seed on the human homeworld now in non-tutorials.
                {
                    IList<GameEntityTypeData> testShipDatas = GameEntityTypeDataTable.Instance.RowsByRollup[EntityRollupType.PlayerTestShip];
                    for ( int j = 0; j < testShipDatas.Count; j++ )
                    {
                        GameEntityTypeData testShipData = testShipDatas[j];
                        //int distanceToTestUnit = Context.RandomToUse.Next( innerSystemMinimumRadius, innerSystemMaximumRadius );
                        ArcenPoint otherPoint = Engine_AIW2.Instance.CombatCenter.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                        GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, testShipData, testShipData.MarkFor( pFaction ),
                            pFaction.FleetUsedAtPlanet, 0, otherPoint, Context, "ScourgeInfusedEmpireStartSpawn" );
                    }
                }
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Scourge_Infusion_Overview", string.Empty, pFaction.Faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                debugIndex = 3000;
                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeInitialCombatFleet )
                {
                    FleetDesignTemplate initialPlayerFleet = FleetDesignTemplateTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( "StartingFleet", false ) );
                    if ( TutorialPlanetOrNull != null && TutorialPlanetOrNull.InitialPlayerCombatFleet != null )
                        initialPlayerFleet = TutorialPlanetOrNull.InitialPlayerCombatFleet;
                    if ( initialPlayerFleet == null ) //the "Random" name won't be found, so this will suffice to say "or random!"
                    {
                        //if was null or random, choose one at random.
                        initialPlayerFleet = (FleetDesignTemplate)FleetDesignTemplateTable.Instance.InitialPlayerFleetsAsBaseRows[Engine_Universal.PermanentQualityRandom.Next( 0, FleetDesignTemplateTable.Instance.InitialPlayerFleetsAsBaseRows.Count )];
                    }

                    FleetItem centerpiece = initialPlayerFleet.GetCategory( FleetItemDrawBagCategory.Centerpiece ).DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( centerpiece == null || centerpiece.TypeData == null )
                        Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null centerpiece found in InitialPlayerFleet " + initialPlayerFleet.InternalName );
                    else
                    {
                        debugIndex = 3500;
                        ArcenPoint entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                        entityPoint = StartingPlanet.GetSafePlacementPoint_SpecificPoint( Context, centerpiece.TypeData, entityPoint, 200, 1000 );
                        GameEntity_Squad actualCenterpiece = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, centerpiece.TypeData, 1, null, 0, entityPoint, Context, "ScourgeInfusedEmpireStartSpawn" );
                        if ( actualCenterpiece == null )
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null actualCenterpiece found in InitialPlayerFleet FleetItem " + centerpiece.TypeData.InternalName );
                        else
                        {
                            Fleet centerpieceFleet = actualCenterpiece.GetFleetOrNull_Safe();
                            if ( centerpieceFleet != null )
                            {
                                centerpieceFleet.ClearAllMembershipsExceptForCenterpiece();
                                centerpieceFleet.SetAllMembershipsUpFromDesignTemplates_HostOnly( initialPlayerFleet, FleetDesignLogic.Unused, null );
                            }
                        }
                    }
                }

                bool isExpertMode = World_AIW2.Instance.CampaignType.HarshnessRating >= 200;

                debugIndex = 4000;
                //Battlestation1
                bool wasRandomBattle1 = false;
                FleetDesignTemplate itemToAvoidForBattle2 = null;
                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeInitialBattlestation )
                {
                    FleetDesignTemplate initialPlayerBattlestation1 = FleetDesignTemplateTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( "StartingBattlestation1", false ) );
                    if ( initialPlayerBattlestation1 == null ) //the "Random" name won't be found, so this will suffice to say "or random!"
                    {
                        wasRandomBattle1 = true;
                        //if was null or random, choose one at random.
                        retry:
                        initialPlayerBattlestation1 = (FleetDesignTemplate)FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows[Engine_Universal.PermanentQualityRandom.Next( 0, FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows.Count )];
                        if ( initialPlayerBattlestation1.WeightInDrawBags == 0 )
                            goto retry;
                    }
                    if ( TutorialPlanetOrNull != null && TutorialPlanetOrNull.InitialPlayerBattlestationFleet != null )
                        initialPlayerBattlestation1 = TutorialPlanetOrNull.InitialPlayerBattlestationFleet;

                    if ( !wasRandomBattle1 )
                    {
                        //if they choose the turtle option, then yell at them
                        if ( isExpertMode && initialPlayerBattlestation1.InternalName == "Mod_TurtleDefenses" )
                        {
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Whoah, apologies!  The starting battlestation of " + initialPlayerBattlestation1.DisplayName + " is too powerful.  Please choose another." );
                            return false;
                        }
                    }
                    else
                    {
                        if ( isExpertMode ) //if they were given the turtle option randomly, give them something else in expert mode
                        {
                            while ( initialPlayerBattlestation1.InternalName == "Mod_TurtleDefenses" || initialPlayerBattlestation1.WeightInDrawBags == 0)
                                initialPlayerBattlestation1 = (FleetDesignTemplate)FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows[Engine_Universal.PermanentQualityRandom.Next( 0, FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows.Count )];
                        }
                    }

                    itemToAvoidForBattle2 = initialPlayerBattlestation1;

                    FleetItem centerpiece = initialPlayerBattlestation1.GetCategory( FleetItemDrawBagCategory.Centerpiece ).DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( centerpiece == null || centerpiece.TypeData == null )
                        Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null centerpiece found in InitialPlayerBattlestation " + initialPlayerBattlestation1.InternalName );
                    else
                    {
                        debugIndex = 4500;
                        ArcenPoint entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                        entityPoint = StartingPlanet.GetSafePlacementPoint_SpecificPoint( Context, centerpiece.TypeData, entityPoint, 200, 1000 );
                        GameEntity_Squad actualCenterpiece = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, centerpiece.TypeData, 1, null, 0, entityPoint, Context, "ScourgeInfusedEmpireStartSpawn" );
                        if ( actualCenterpiece == null )
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null actualCenterpiece found in InitialPlayerBattlestation FleetItem " + centerpiece.TypeData.InternalName );
                        else
                        {
                            Fleet centerpieceFleet = actualCenterpiece.GetFleetOrNull_Safe();
                            if ( centerpieceFleet != null )
                            {
                                centerpieceFleet.ClearAllMembershipsExceptForCenterpiece();
                                centerpieceFleet.SetAllMembershipsUpFromDesignTemplates_HostOnly( initialPlayerBattlestation1, FleetDesignLogic.Unused, null );

                                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                                command.RelatedIntegers.Add( centerpieceFleet.FleetID ); //FleetID
                                command.RelatedIntegers.Add( PlayerAccount.Local.PlayerPrimaryKeyID );
                                command.RelatedString = "ToggleIsFleetOnPlayerWatchlist";
                                command.RelatedBool = true;
                                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                            }
                        }
                    }
                }

                debugIndex = 5000;
                //Battlestation2
                if ( TutorialPlanetOrNull == null && !isExpertMode ) //aka "don't give me a second battlestation in expert mode!"
                {
                    FleetDesignTemplate initialPlayerBattlestation2 = FleetDesignTemplateTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( "StartingBattlestation2", false ) );
                    bool wasRandomBattle2 = false;
                    if ( initialPlayerBattlestation2 == null || initialPlayerBattlestation2 == itemToAvoidForBattle2 ) //the "Random" name won't be found, so this will suffice to say "or random!"
                    {
                        wasRandomBattle2 = true;
                        //if was null or random, choose one at random.
                        retry:
                        initialPlayerBattlestation2 = (FleetDesignTemplate)FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows[Engine_Universal.PermanentQualityRandom.Next( 0, FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows.Count )];
                        if ( initialPlayerBattlestation2.WeightInDrawBags == 0 )
                            goto retry;
                    }
                    int loopCount = 0;
                    while ( (wasRandomBattle2 || wasRandomBattle1) && initialPlayerBattlestation2 == itemToAvoidForBattle2 && loopCount++ < 1000 )
                    {
                        //if either was random, make sure that the two are not identical
                        retry:
                        initialPlayerBattlestation2 = (FleetDesignTemplate)FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows[Engine_Universal.PermanentQualityRandom.Next( 0, FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows.Count )];
                        if ( initialPlayerBattlestation2.WeightInDrawBags == 0 )
                            goto retry;
                    }
                    
                    FleetItem centerpiece = initialPlayerBattlestation2.GetCategory( FleetItemDrawBagCategory.Centerpiece ).DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( centerpiece == null || centerpiece.TypeData == null )
                        Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null centerpiece found in InitialPlayerBattlestation " + initialPlayerBattlestation2.InternalName );
                    else
                    {
                        debugIndex = 5500;
                        ArcenPoint entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                        entityPoint = StartingPlanet.GetSafePlacementPoint_SpecificPoint( Context, centerpiece.TypeData, entityPoint, 200, 1000 );
                        GameEntity_Squad actualCenterpiece = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, centerpiece.TypeData, 1, null, 0, entityPoint, Context, "ScourgeInfusedEmpireStartSpawn" );
                        if ( actualCenterpiece == null )
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null actualCenterpiece found in InitialPlayerBattlestation FleetItem " + centerpiece.TypeData.InternalName );
                        else
                        {
                            Fleet centerpieceFleet = actualCenterpiece.GetFleetOrNull_Safe();
                            if ( centerpieceFleet != null )
                            {
                                centerpieceFleet.ClearAllMembershipsExceptForCenterpiece();
                                centerpieceFleet.SetAllMembershipsUpFromDesignTemplates_HostOnly( initialPlayerBattlestation2, FleetDesignLogic.Unused, null );
                            }
                        }
                    }
                }

                debugIndex = 6000;
                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeInitialSupportFleet )
                {
                    FleetDesignTemplate initialPlayerSupportFleet = FleetDesignTemplateTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( "StartingSupportFleet", false ) );
                    if ( initialPlayerSupportFleet == null ) //the "Random" name won't be found, so this will suffice to say "or random!"
                    {
                        //if was null or random, choose one at random.
                        initialPlayerSupportFleet = (FleetDesignTemplate)FleetDesignTemplateTable.Instance.InitialPlayerSupportFleetsAsBaseRows[Engine_Universal.PermanentQualityRandom.Next( 0, FleetDesignTemplateTable.Instance.InitialPlayerSupportFleetsAsBaseRows.Count )];
                    }
                    if ( TutorialPlanetOrNull != null && TutorialPlanetOrNull.InitialPlayerSupportFleet != null )
                        initialPlayerSupportFleet = TutorialPlanetOrNull.InitialPlayerSupportFleet;

                    FleetItem centerpiece = initialPlayerSupportFleet.GetCategory( FleetItemDrawBagCategory.Centerpiece ).DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( centerpiece == null || centerpiece.TypeData == null )
                        Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null centerpiece found in InitialPlayerSupportFleets " + initialPlayerSupportFleet.InternalName );
                    else
                    {
                        debugIndex = 6500;
                        ArcenPoint entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                        entityPoint = StartingPlanet.GetSafePlacementPoint_SpecificPoint( Context, centerpiece.TypeData, entityPoint, 200, 1000 );
                        GameEntity_Squad actualCenterpiece = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, centerpiece.TypeData, 1, null, 0, entityPoint, Context, "ScourgeInfusedEmpireStartSpawn" );
                        if ( actualCenterpiece == null )
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null actualCenterpiece found in InitialPlayerSupportFleets FleetItem " + centerpiece.TypeData.InternalName );
                        else
                        {
                            Fleet centerpieceFleet = actualCenterpiece.GetFleetOrNull_Safe();
                            if ( centerpieceFleet != null )
                            {
                                centerpieceFleet.ClearAllMembershipsExceptForCenterpiece();
                                centerpieceFleet.SetAllMembershipsUpFromDesignTemplates_HostOnly( initialPlayerSupportFleet, FleetDesignLogic.Unused, null );
                            }
                        }
                    }
                }

            }
            catch ( Exception e)
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "HumanScourgeInfusedEmpireSpecificCodeDeepInfo SeedUnitsOnStartingPlanetDuringMapGen error at debugIndex " + debugIndex + ": " + e );
                return false;
            }
            return true;
        }
        public GameEntity_Squad SpawnScourgeStructure(ArcenPoint spawnLocation, Planet planet, string TypeName, Faction faction,
                                                 ArcenHostOnlySimContext Context, bool exactPlacement)
        {
            if (!ArcenNetworkAuthority.GetIsHostMode())
            {
                return null; //only for the host; clients will get this data sync'd to them later
            }

            GameEntityTypeData structureData = GameEntityTypeDataTable.Instance.GetRowByName(TypeName);
            if (structureData == null)
            {
                structureData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, TypeName);
                if (structureData == null)
                    throw new Exception("Unable to find XML with name or tag " + TypeName);
            }

            if ( spawnLocation == ArcenPoint.ZeroZeroPoint )
            {
                spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, structureData, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 300 ) );
            }
            else if ( exactPlacement )
                spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, structureData, spawnLocation, FInt.FromParts( 0, 010 ), FInt.FromParts( 0, 020 ) );
            else
                spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, structureData, spawnLocation, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 250 ) );

            if ( this.VassalFaction == null )
                this.VassalFaction = this.BaseInfo.VassalFaction;

            PlanetFaction pFaction = planet.GetPlanetFactionForFaction(this.VassalFaction);
            GameEntity_Squad structure = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, structureData, 1,
                    null, 0, spawnLocation, Context, "ScourgeVassal-NewStructure" );
            return structure;
        }
    }
}
