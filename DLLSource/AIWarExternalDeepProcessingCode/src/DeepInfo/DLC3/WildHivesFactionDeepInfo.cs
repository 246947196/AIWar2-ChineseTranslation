
using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public class WildHivesFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        #region Required Data
        public WildHivesFactionBaseInfo BaseInfo;
        public WildHivesFactionDeepInfo() => Cleanup();
        protected override void Cleanup()
        {
            BaseInfo = null;
        }
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            BaseInfo = AttachedFaction.GetExternalBaseInfoAs<WildHivesFactionBaseInfo>();
        }
        protected override int MinimumSecondsBetweenLongRangePlannings => 3;
        #endregion

        AntiMinorFactionWaveData WaveData => BaseInfo.WaveData;

        private readonly List<SafeSquadWrapper> potentialGenerators = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 30, "WildHivesFactionDeepInfo-potentialGenerators" );

        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            potentialGenerators.Clear();
            // Seed on planets that are at least 3 hops away from all human/ai homeworlds.
            foreach ( Planet planet in galaxy.Planets( false ) )
            {
                if ( planet.OriginalHopsToAnyHomeworld >= 3 )
                    foreach ( GameEntity_Squad generator in planet.Squads( WildHivesFactionBaseInfo.MetalGeneratorTag ) )
                    {
                        potentialGenerators.Add( generator );
                    }
            }

            for ( int x = 0; x < 1 + BaseInfo.Intensity / 5; x++ )
            {
                if ( potentialGenerators.Count <= 0 )
                    break; //exception inevitable without this

                int index = Context.RandomToUse.Next( potentialGenerators.Count );
                GameEntity_Squad generator = potentialGenerators[index].GetSquad();
                potentialGenerators.RemoveAt( index );
                if ( generator == null )
                {
                    x--;
                    continue;
                }
                ConvertEntityToHive( generator.Planet.GetPlanetFactionForFaction( BaseInfo.AttachedFaction ), generator, false, Context );
            }
        }

        public override void UpdatePlanetInfluence_HostOnly( ArcenHostOnlySimContext Context )
        {
            List<Planet> planetsInfluenced = Planet.GetTemporaryPlanetList( "WildHives-UpdatePlanetInfluence_HostOnly-planetsInfluenced", 10f );
            if ( planetsInfluenced == null ) //blocked for teardown/shutdown; bail
                return;

            foreach ( GameEntity_Squad hive in BaseInfo.hives.DisplaySquads() )
            {
                switch ( hive.Planet.GetControllingFactionType() )
                {
                    case FactionType.Player:
                        break;
                    case FactionType.AI:
                        break;
                    case FactionType.SpecialFaction:
                        break;
                    default:
                        planetsInfluenced.AddIfNotAlreadyIn( hive.Planet );
                        break;
                }

            }

            AttachedFaction.SetInfluenceForPlanetsToList( planetsInfluenced );
            Planet.ReleaseTemporaryPlanetList( planetsInfluenced );
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            if ( BaseInfo.workers.GetDisplayList().Count == 0 && BaseInfo.hives.GetDisplayList().Count == 0 )
                return; // Faction is defeated.

            UpdateAllegiance();

            HandleUnitProductionAndStateToggle( Context );

            HandleNeutralObjectConversion( Context );

            HandleAIResponse( Context );
        }

        #region UpdateAllegiance
        public void UpdateAllegiance()
        {
            AllegianceHelper.EnemyThisFactionToAll( BaseInfo.AttachedFaction );
        }
        #endregion
        #region HandleUnitProduction
        public void HandleUnitProductionAndStateToggle( ArcenHostOnlySimContext Context )
        {
            foreach ( GameEntity_Squad hive in BaseInfo.hives.DisplaySquads() )
            {
                WildHivesPerUnitBaseInfo hiveInfo = hive.GetExternalBaseInfoAs<WildHivesPerUnitBaseInfo>();
                AddBuildPoints( hiveInfo, BaseInfo.Difficulty.GetPerSecondBudgetForHive( hive ) );
                DeploySoldiersIfNeeded( hive, hiveInfo, Context );
                BuildWorkersAndMarkUpIfAble( hive, hiveInfo, Context );

                WildHivesFactionBaseInfo.HasStoredSoldiers( hive, BaseInfo, hiveInfo, out int stored );
                hive.AdditionalStrengthFromFactions = WildHivesFactionBaseInfo.GetAverageStrengthOfSpawntSoldiers( stored, hive.CurrentMarkLevel );

                // If we've been attacked, or are contesting on a non player planet, become targetable by everything.
                if ( BaseInfo.attackedPlanets.DisplayContains( hive.Planet ) || (hive.Planet.GetControllingFactionType() != FactionType.Player && BaseInfo.contestedPlanets.DisplayContains( hive.Planet )) )
                {
                    if ( hive.TypeData.InternalName == WildHivesFactionBaseInfo.NeinzulHiveName_Basic )
                    {
                        hive.TransformInto( Context, GameEntityTypeDataTable.Instance.GetRowByName( WildHivesFactionBaseInfo.NeinzulHiveName_Basic_Agitated ), 1, true ).GetExternalBaseInfoAs<WildHivesPerUnitBaseInfo>().CopyFrom( hiveInfo );
                    }
                    else if ( hive.TypeData.InternalName == WildHivesFactionBaseInfo.NeinzulHiveName_Advanced )
                        hive.TransformInto( Context, GameEntityTypeDataTable.Instance.GetRowByName( WildHivesFactionBaseInfo.NeinzulHiveName_Advanced_Agitated ), 1, true ).GetExternalBaseInfoAs<WildHivesPerUnitBaseInfo>().CopyFrom( hiveInfo );
                }
                else
                {
                    if ( hive.TypeData.InternalName == WildHivesFactionBaseInfo.NeinzulHiveName_Basic_Agitated )
                        hive.TransformInto( Context, GameEntityTypeDataTable.Instance.GetRowByName( WildHivesFactionBaseInfo.NeinzulHiveName_Basic ), 1, true ).GetExternalBaseInfoAs<WildHivesPerUnitBaseInfo>().CopyFrom( hiveInfo );
                    else if ( hive.TypeData.InternalName == WildHivesFactionBaseInfo.NeinzulHiveName_Advanced_Agitated )
                        hive.TransformInto( Context, GameEntityTypeDataTable.Instance.GetRowByName( WildHivesFactionBaseInfo.NeinzulHiveName_Advanced ), 1, true ).GetExternalBaseInfoAs<WildHivesPerUnitBaseInfo>().CopyFrom( hiveInfo );
                }
            }
            foreach ( GameEntity_Squad worker in BaseInfo.workers.DisplaySquads() )
            {
                WildHivesPerUnitBaseInfo workerInfo = worker.GetExternalBaseInfoAs<WildHivesPerUnitBaseInfo>();
                AddBuildPoints( workerInfo, BaseInfo.Difficulty.perSecondWorker );
                DeploySoldiersIfNeeded( worker, workerInfo, Context );

                WildHivesFactionBaseInfo.HasStoredSoldiers( worker, BaseInfo, workerInfo, out int stored );
                worker.AdditionalStrengthFromFactions = WildHivesFactionBaseInfo.GetAverageStrengthOfSpawntSoldiers( stored, worker.CurrentMarkLevel );

                if ( BaseInfo.attackedPlanets.DisplayContains( worker.Planet ) || (worker.Planet.GetControllingFactionType() != FactionType.Player && BaseInfo.contestedPlanets.DisplayContains( worker.Planet )) )
                {
                    if ( worker.TypeData.InternalName == WildHivesFactionBaseInfo.WorkerName_Basic )
                        worker.TransformInto( Context, GameEntityTypeDataTable.Instance.GetRowByName( WildHivesFactionBaseInfo.WorkerName_Agitated ), 1, true ).GetExternalBaseInfoAs<WildHivesPerUnitBaseInfo>().CopyFrom( workerInfo );
                }
                else
                {
                    if ( worker.TypeData.InternalName == WildHivesFactionBaseInfo.WorkerName_Agitated )
                        worker.TransformInto( Context, GameEntityTypeDataTable.Instance.GetRowByName( WildHivesFactionBaseInfo.WorkerName_Basic ), 1, true ).GetExternalBaseInfoAs<WildHivesPerUnitBaseInfo>().CopyFrom( workerInfo );
                }

            }
        }
        public void AddBuildPoints( WildHivesPerUnitBaseInfo entityInfo, int toAdd )
        {
            entityInfo.PrimaryBuildPoints += toAdd;
            entityInfo.SoldierBuildPoints += toAdd;
        }
        #endregion
        #region BuildOrDeploySoldiersAsNeeded
        public void DeploySoldiersIfNeeded( GameEntity_Squad entity, WildHivesPerUnitBaseInfo entityInfo, ArcenHostOnlySimContext Context )
        {
            if ( WildHivesFactionBaseInfo.HasStoredSoldiers( entity, BaseInfo, entityInfo, out int soldiersToSpawn ) )
            {
                if ( entity.Planet.GetControllingFactionType() == FactionType.Player )
                {
                    if ( BaseInfo.attackedPlanets.DisplayContains( entity.Planet ) )
                    {
                        // We've been attacked! Betrayal! Deploy to destroy.
                        DeploySoldiers( entity, soldiersToSpawn, false, Context );
                    }
                    else if ( WildHivesFriendlyFactionBaseInfo.Instance.attackedPlanets.DisplayContains( entity.Planet ) )
                    {
                        // Allies under attack. Defend them.
                        if ( ArcenNetworkAuthority.GetIsHostMode() && entity.Planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                            World_AIW2.Instance.QueueLogJournalEntryToSidebar( WildHivesFactionBaseInfo.JournalFriendlyDefense, string.Empty, entity.PlanetFaction.Faction, null, entity.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                        DeploySoldiers( entity, soldiersToSpawn, true, Context );
                    }
                } // Otherwise, if something on our planet is under attack, deploy to kill.
                else if ( BaseInfo.attackedPlanets.DisplayContains( entity.Planet ) )
                {
                    DeploySoldiers( entity, soldiersToSpawn, false, Context );
                }
                else if ( BaseInfo.contestedPlanets.DisplayContains( entity.Planet ) )
                {
                    if ( ArcenNetworkAuthority.GetIsHostMode() && entity.Planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( WildHivesFactionBaseInfo.JournalContested, string.Empty, entity.PlanetFaction.Faction, null, entity.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    DeploySoldiers( entity, soldiersToSpawn, false, Context );
                }
            }
        }
        public static void DeploySoldiers( GameEntity_Squad entity, int toDeploy, bool asFriendlyToPlayer, ArcenHostOnlySimContext Context )
        {
            if ( entity == null )
                return;
            for ( int x = 0; x < toDeploy; x++ )
            {
                PlanetFaction pFaction = entity.PlanetFaction;
                if ( asFriendlyToPlayer )
                    pFaction = entity.Planet.GetPlanetFactionForFaction( WildHivesFriendlyFactionBaseInfo.Instance.AttachedFaction );
                GameEntity_Squad soldier = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, WildHivesFactionBaseInfo.SoldierTag ),
                    entity.CurrentMarkLevel, pFaction.FleetUsedAtPlanet, 0, entity.WorldLocation, Context, "WildHives-DeploySoldier" );
                if ( soldier != null )
                {
                    soldier.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                    soldier.MinorFactionStackingID = entity.PrimaryKeyID;
                }
            }
            entity.GetExternalBaseInfoAs<WildHivesPerUnitBaseInfo>().SoldierBuildPoints = 0;
        }
        #endregion
        #region BuildWorkersIfAble
        public void BuildWorkersAndMarkUpIfAble( GameEntity_Squad entity, WildHivesPerUnitBaseInfo entityInfo, ArcenHostOnlySimContext Context )
        {
            if ( entityInfo.PrimaryBuildPoints >= BaseInfo.Difficulty.workerCostBase + (BaseInfo.workers.GetDisplayList().Count * BaseInfo.Difficulty.workerCostIncreasePerAlreadyBuilt) )
            {
                GameEntity_Squad.CreateNew_ReturnNullIfMPClient( entity.PlanetFaction, GameEntityTypeDataTable.Instance.GetRowByName( WildHivesFactionBaseInfo.WorkerName_Basic ), entity.CurrentMarkLevel,
                    entity.PlanetFaction.FleetUsedAtPlanet, 0, entity.WorldLocation, Context, "WildHives-BuildWorker" );
                entityInfo.PrimaryBuildPoints = 0;
                if ( entity.CurrentMarkLevel < 7 )
                    entity.SetCurrentMarkLevel( (byte)(entity.CurrentMarkLevel + 1) );
            }
        }
        #endregion
        #region HandleNeutralObjectConversion
        public bool CanConvertEntity( GameEntity_Squad entity, int buildPoints, out bool CheckForFleetLines )
        {
            CheckForFleetLines = false;
            if ( entity.TypeData.GetHasTag( WildHivesFactionBaseInfo.MetalGeneratorTag ) )
            {
                if ( buildPoints >= BaseInfo.Difficulty.claimMineCost )
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if ( entity.TypeData.FleetDesignLogicIGrantOneOf == FleetDesignLogic.MobileCombatFleet || entity.TypeData.FleetDesignLogicIGrantOneOf == FleetDesignLogic.Planetary )
            {
                if ( BaseInfo.Difficulty.claimCenterpieceCost > 0 && buildPoints >= BaseInfo.Difficulty.claimCenterpieceCost )
                {
                    CheckForFleetLines = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if ( BaseInfo.Difficulty.claimOtherNeutralCost > 0 && buildPoints >= BaseInfo.Difficulty.claimOtherNeutralCost )
                {
                    CheckForFleetLines = true;
                    return true;
                }
                else
                    return false;
            }
        }
        public void HandleNeutralObjectConversion( ArcenHostOnlySimContext Context )
        {
            foreach ( GameEntity_Squad worker in BaseInfo.workers.DisplaySquads() )
            {
                // Conversion logic for neutral entities.
                foreach ( GameEntity_Squad entity in worker.Planet.GetPlanetFactionForFaction( World_AIW2.Instance.GetNeutralFaction() ).Entities.Squads() )
                {
                    if ( worker.GetDistanceTo_VeryCheapButExtremelyRough( entity.WorldLocation, RadiusCheck.SubtractRadiiFromDistance ) > 1500 )
                        continue; // Not near.

                    WildHivesPerUnitBaseInfo workerInfo = worker.GetExternalBaseInfoAs<WildHivesPerUnitBaseInfo>();
                    bool checkForFleetLines = false;
                    bool canConvert = CanConvertEntity( entity, workerInfo.PrimaryBuildPoints, out checkForFleetLines );

                    if ( !canConvert )
                        continue;

                    ConvertEntityToHive( worker.PlanetFaction, entity, checkForFleetLines, Context );
                    workerInfo.PrimaryBuildPoints = 0;

                }

            }
        }
        public void ConvertEntityToHive( PlanetFaction pFaction, GameEntity_Squad entity, bool checkForFleetLines, ArcenHostOnlySimContext Context )
        {
            if ( entity == null )
                return;

            // If we're converting a metal generator, convert to a basic hive, otherwise, big hive.
            GameEntityTypeData entityData = entity.TypeData.GetHasTag( WildHivesFactionBaseInfo.MetalGeneratorTag ) ? GameEntityTypeDataTable.Instance.GetRowByName( WildHivesFactionBaseInfo.NeinzulHiveName_Basic ) : GameEntityTypeDataTable.Instance.GetRowByName( WildHivesFactionBaseInfo.NeinzulHiveName_Advanced );
            GameEntity_Squad hive = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1, pFaction.FleetUsedAtPlanet, 0, entity.WorldLocation, Context, "WildHives-ConvertToHive" );
            WildHivesPerUnitBaseInfo hiveInfo = hive.GetExternalBaseInfoAs<WildHivesPerUnitBaseInfo>();
            hiveInfo.BaseEntityName = entity.TypeData.InternalName;

            if ( checkForFleetLines )
            {
                foreach ( FleetMembership mem in entity.FleetMembership.Fleet.MemberGroupsUnsorted_Sim )
                {
                    if ( mem.TypeData == entity.TypeData )
                        continue; // Skip self.

                    short cap = (short)mem.GetBaseSquadCapWithAdditions();

                    if ( cap > 0 )
                        hiveInfo.FleetLines.Add( mem.TypeData.InternalName, cap );

                }
            }

            entity.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );
        }
        #endregion
        #region HandleAIResponse
        private void HandleAIResponse( ArcenHostOnlySimContext Context )
        {
            if ( World_AIW2.Instance.GameSecond % 60 != 0 )
                return;

            // Add budget for reconquest waves. Budget amount increases based on how many planets the Wild Hives own.
            short owned = 0;
            int minStrReq = 0;
            foreach ( Planet influencedPlanet in BaseInfo.influencedPlanets.GetDisplayList() )
            {
                if ( influencedPlanet.GetControllingOrInfluencingFaction() == AttachedFaction )
                {
                    owned++;
                    int strength = influencedPlanet.GetDataByStanceForFaction( AttachedFaction, FactionStance.Self ).TotalStrengthIncludingNonMilitary / 2 * 3;
                    if ( strength > minStrReq )
                        minStrReq = strength;
                }
            }

            if ( owned == 0 )
            {
                // No budget.
                WaveData.currentWaveBudget = FInt.Zero;
                WaveData.timeForNextWave = 0;
                return;
            }

            // Add budget. Scales based on ownership and intensity.
            WaveData.currentWaveBudget += BaseInfo.Intensity * owned * 1000;

            // Set timer if first time.
            if ( WaveData.timeForNextWave == 0 )
                WaveData.timeForNextWave = World_AIW2.Instance.GameSecond + 600;
            else if ( WaveData.timeForNextWave <= World_AIW2.Instance.GameSecond )
            {
                int budgetSpent = AntiMinorFactionWaveData.QueueWave( AttachedFaction, Context, WaveData.currentWaveBudget.GetNearestIntPreferringHigher(), true );
                if ( budgetSpent == FInt.Zero )
                {
                    //the wave wasn't actually sent; presumably there are no valid targets
                    //start checking every minute
                    this.BaseInfo.WaveData.timeForNextWave = World_AIW2.Instance.GameSecond + 60;
                }
                else
                {
                    this.BaseInfo.WaveData.timeForNextWave = World_AIW2.Instance.GameSecond + 600;
                    this.BaseInfo.WaveData.currentWaveBudget = FInt.Zero;
                }
            }
        }
        #endregion

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( BaseInfo.workers.GetDisplayList().Count == 0 && BaseInfo.hives.GetDisplayList().Count == 0 )
                return; // Faction is defeated.

            MoveWorkers( Context );
        }

        #region MoveWorkers
        private readonly List<ArcenPoint> moveWorkers_workingList = List<ArcenPoint>.Create_WillNeverBeGCed( 50, "WildHivesFactionDeepInfo-moveWorkers_workingList" );
        public void MoveWorkers( ArcenLongTermIntermittentPlanningContext Context )
        {
            foreach ( GameEntity_Squad worker in BaseInfo.workers.DisplaySquads() )
            {
                if ( worker.Planet.HasPlanetBeenDestroyed )
                    continue;
                if ( worker.GetSecondsSinceEnteringThisPlanet() < 350 )
                {
                    if ( worker.HasQueuedOrders() )
                        continue; // Already have order, skip.
                    #region Moving Around Our Planet
                    moveWorkers_workingList.Clear();

                    // Stick our shippy appendenges on any neutral objects we can find.
                    foreach ( GameEntity_Squad entity in worker.Planet.GetPlanetFactionForFaction( World_AIW2.Instance.GetNeutralFaction() ).Entities.Squads() )
                    {
                        // Very rare case of a neutral entity not having a location, for some strange reason. Sanity check.
                        if ( entity.WorldLocation != ArcenPoint.ZeroZeroPoint )
                            moveWorkers_workingList.Add( entity.WorldLocation );

                    }

                    // Also visit any Metal Generators. Makes them more likely to visit neutral metal generators due to appearing twice.
                    foreach ( GameEntity_Squad generator in worker.Planet.Squads( WildHivesFactionBaseInfo.MetalGeneratorTag ) )
                    {
                        // Stick this generator into our movement queue at random.
                        moveWorkers_workingList.Add( generator.WorldLocation );
                    }

                    GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCPatrol], GameCommandSource.AnythingElse );
                    command.RelatedString = "WildHives_WorkerWanderOnPlanet";
                    command.RelatedEntityIDs.Add( worker.PrimaryKeyID );
                    command.RelatedPoints.Add( moveWorkers_workingList[Context.RandomToUse.Next( moveWorkers_workingList.Count )] );
                    command.ToBeQueued = true;
                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );

                    #endregion
                }
                else if ( worker.GetSecondsSinceEnteringThisPlanet() % 120 == 0 || !worker.Orders.GetHasAnyOrdersOfType( EntityOrderType.Wormhole ) )
                {
                    // Unlike movement commands, potentially recalculate this every now and than, in case we're stuck on a bubble.
                    #region Moving To New Planets
                    // We really don't care where we go, just get us outta here.
                    GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleSpecialMission], GameCommandSource.AnythingElse );
                    command.RelatedString = "WildHives_WorkerWanderToOtherPlanet";
                    command.RelatedEntityIDs.Add( worker.PrimaryKeyID );
                    command.RelatedIntegers.Add( worker.Planet.GetRandomNeighbor( false, Context ).Index );
                    command.ToBeQueued = false;
                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                    #endregion
                }


            }
        }
        #endregion

        public override void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        {
            if ( entity.TypeData.AlwaysSelfAttritions )
                return; // Skip younglings.

            // Get angry.
            BaseInfo.AttackedOn( entity.Planet );

            WildHivesPerUnitBaseInfo entityInfo = entity.TryGetExternalBaseInfoAs<WildHivesPerUnitBaseInfo>();
            if ( entityInfo == null )
                return; // Skip if we're not supported.

            // If we have any Soldiers stored, no matter what we are, spit them out.
            if ( WildHivesFactionBaseInfo.HasStoredSoldiers( entity, BaseInfo, entityInfo, out int soldersToSpawn ) )
                DeploySoldiers( entity, soldersToSpawn, false, Context );

            // If we're a Hive, put a neutral Metal Generator back where we found it.
            if ( entity.TypeData.GetHasTag( WildHivesFactionBaseInfo.NeinzulHiveTag ) )
            {
                PlanetFaction pFaction = entity.Planet.GetPlanetFactionForFaction( World_AIW2.Instance.GetNeutralFaction() );
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( entityInfo.BaseEntityName );
                GameEntity_Squad neutralEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1, pFaction.FleetUsedAtPlanet, 0, entity.WorldLocation, Context, "WildHives-MetalFromDeath" );

                if ( entityInfo.FleetLines.GetPairCount() > 0 )
                {
                    // We have saved fleet data. Put it back.
                    // First, remove existing memberships.
                    List<FleetMembership> toRemove = FleetMembership.GetTemporaryFleetMembershipList( "WildHivesFactionDeepInfo-OnAnyDeathLogic-toRemove", 10f );
                    if ( toRemove == null ) //blocked for teardown/shutdown; bail
                        return;
                    foreach ( FleetMembership mem in neutralEntity.FleetMembership.Fleet.MemberGroupsUnsorted_Sim )
                    {
                        if ( mem.TypeData == neutralEntity.TypeData )
                            continue; // Skip centerpiece.

                        toRemove.Add( mem );

                    }
                    toRemove.ForEach( mem => { neutralEntity.FleetMembership.Fleet.RemoveMemGroupExplicit( mem ); } );
                    FleetMembership.ReleaseTemporaryFleetMembershipList( toRemove );

                    // Now, add in our old data.
                    foreach ( KeyValuePair<string, short> pair in entityInfo.FleetLines )
                    {
                        var mem = neutralEntity.FleetMembership.Fleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( GameEntityTypeDataTable.Instance.GetRowByName( pair.Key ) );
                        mem.ExplicitBaseSquadCap = pair.Value;
                    }
                }
            }
        }
    }
}
