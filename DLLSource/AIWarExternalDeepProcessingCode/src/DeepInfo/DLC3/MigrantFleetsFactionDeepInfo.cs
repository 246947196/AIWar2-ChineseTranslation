using System;

using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public sealed class MigrantFleetsFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        public MigrantFleetsFactionBaseInfo BaseInfo;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<MigrantFleetsFactionBaseInfo>();
        }

        protected override void Cleanup()
        {
            BaseInfo = null;
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 2;

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            // Start our first timer a minute in.
            if ( World_AIW2.Instance.GameSecond < 60 )
                return;
            else if ( World_AIW2.Instance.GameSecond == 60 )
            {
                BaseInfo.Trigger( MigrantFleetEscortSize.Large );
                BaseInfo.Trigger( MigrantFleetEscortSize.Medium );
                BaseInfo.Trigger( MigrantFleetEscortSize.Small );
            }

            bool shouldSpawn = DetermineMigrantSpawns( out MigrantFleetEscortSize typeToSpawn, out bool returningMigrants );

            if ( shouldSpawn )
                SpawnMigrantFleet( typeToSpawn, returningMigrants, AttachedFaction, Context );

            TransformMigrantWormholes( AttachedFaction, Context );

            RepairOrWarpOutMigrants( AttachedFaction, Context );

            if ( BaseInfo.clanlingsWithoutParent.Count > 0 )
                AttachClanlingUnits( AttachedFaction, Context );
        }

        private bool DetermineMigrantSpawns( out MigrantFleetEscortSize typeToSpawn, out bool returningMigrants )
        {
            typeToSpawn = MigrantFleetEscortSize.Small;
            returningMigrants = false;

            // Figure out what size, if any, of migrant fleet to spawn. Prioritze using saved Migrants on larger ones first.
            if ( BaseInfo.CanTrigger_New( MigrantFleetEscortSize.Large, BaseInfo.Difficulty ) )
                typeToSpawn = MigrantFleetEscortSize.Large;
            else if ( BaseInfo.CanTrigger_Saved( MigrantFleetEscortSize.Large, BaseInfo.Difficulty ) )
            {
                typeToSpawn = MigrantFleetEscortSize.Large;
                returningMigrants = true;
            }
            else if ( BaseInfo.CanTrigger_New( MigrantFleetEscortSize.Medium, BaseInfo.Difficulty ) )
                typeToSpawn = MigrantFleetEscortSize.Medium;
            else if ( BaseInfo.CanTrigger_Saved( MigrantFleetEscortSize.Medium, BaseInfo.Difficulty ) )
            {
                typeToSpawn = MigrantFleetEscortSize.Medium;
                returningMigrants = true;
            }
            else if ( BaseInfo.CanTrigger_New( MigrantFleetEscortSize.Small, BaseInfo.Difficulty ) )
                typeToSpawn = MigrantFleetEscortSize.Small;
            else if ( BaseInfo.CanTrigger_Saved( MigrantFleetEscortSize.Small, BaseInfo.Difficulty ) )
            {
                typeToSpawn = MigrantFleetEscortSize.Small;
                returningMigrants = true;
            }
            else
                return false;

            return true;
        }

        //Set immediately before spawn_planets.Sort(...) so the comparison can be a non-capturing
        //static delegate.  [ThreadStatic] for safety since this is sim-context code.
        [ThreadStatic] private static Faction cb_migrantSortFaction;

        private void SpawnMigrantFleet( MigrantFleetEscortSize typeToSpawn, bool returningMigrants, Faction faction, ArcenHostOnlySimContext context )
        {
            // Build a list of valid planets.
            int hopsFromFriendliesMin = 3, hopsFromFriendliesMax = 5;
            int hopsFromFriendliesSmallType = 2;
            int numberOfBestPlanetsToCheck = 5; // Pick from the easiest planets based on this value.

            List<Planet> friendlyTerritory = Planet.GetTemporaryPlanetList( "Migrants-SpawnMigrantFleet-friendlyTerritory", 10f );
            if ( friendlyTerritory == null ) //blocked for teardown/shutdown; bail
                return;
            List<Planet> spawn_planets = Planet.GetTemporaryPlanetList( "Migrants-SpawnMigrantFleet-spawn_planets", 10f );
            if ( spawn_planets == null ) //blocked for teardown/shutdown; bail
            {
                Planet.ReleaseTemporaryPlanetList( friendlyTerritory );
                return;
            }

            BaseInfo.GetFriendlyTerritory( friendlyTerritory );

            foreach ( Planet workingPlanet in World_AIW2.Instance.Planets( false ) )
            {
                if ( friendlyTerritory.Contains( workingPlanet ) )
                    continue; // Is part of friendly territory. Too easy.

                if ( workingPlanet.GetFirstMatching( EntityRollupType.KingUnitsOnly, null, true, true ) != null )
                    continue; // Homeworld.

                if ( BaseInfo.humanAlly && typeToSpawn == MigrantFleetEscortSize.Small && workingPlanet.IntelLevel < PlanetIntelLevel.ExploredByDistantHacking )
                    continue; // Small ones should never be un unknown planets.

                int hopsToNearestFriendlyPlanet = 99;

                friendlyTerritory.ForEach( friendlyPlanet =>
                {
                    int workingHops = workingPlanet.GetHopsTo( friendlyPlanet );
                    if ( workingHops < hopsToNearestFriendlyPlanet )
                        hopsToNearestFriendlyPlanet = workingHops;
                } );

                // Stop if too close or far from a friendly planet. Let small spawns spawn super close.
                switch ( typeToSpawn )
                {
                    case MigrantFleetEscortSize.Small:
                        if ( hopsToNearestFriendlyPlanet != hopsFromFriendliesSmallType )
                            continue; // Not good enough for a small wave.
                        break;
                    default:
                        if ( hopsToNearestFriendlyPlanet < hopsFromFriendliesMin || hopsToNearestFriendlyPlanet > hopsFromFriendliesMax )
                            continue; // Not within our range bounds.
                        break;
                }

                spawn_planets.Add( workingPlanet );
            }

            // Sort our list by hostile strength, putting the weaker ones in front.
            cb_migrantSortFaction = faction;
            spawn_planets.Sort( static ( thisPlanet, otherPlanet ) => { return thisPlanet.GetPlanetFactionForFaction( cb_migrantSortFaction ).DataByStance[FactionStance.Hostile].TotalStrength.CompareTo( otherPlanet.GetPlanetFactionForFaction( cb_migrantSortFaction ).DataByStance[FactionStance.Hostile].TotalStrength ); } );

            int planetsToSpawnOn, migrantsToSpawnPerPlanet;
            switch ( typeToSpawn )
            {
                case MigrantFleetEscortSize.Large:
                    planetsToSpawnOn = 2;
                    migrantsToSpawnPerPlanet = BaseInfo.Difficulty.MigrantsPerMigration_Large / 2;
                    break;
                case MigrantFleetEscortSize.Medium:
                    planetsToSpawnOn = 1;
                    migrantsToSpawnPerPlanet = BaseInfo.Difficulty.MigrantsPerMigration_Medium;
                    break;
                default:
                    planetsToSpawnOn = 1;
                    migrantsToSpawnPerPlanet = BaseInfo.Difficulty.MigrantsPerMigration_Small;
                    break;
            }

            if ( spawn_planets.Count < planetsToSpawnOn )
            {
                Planet.ReleaseTemporaryPlanetList( friendlyTerritory );
                Planet.ReleaseTemporaryPlanetList( spawn_planets );
                //ArcenDebugging.SingleLineQuickDebug( "Not enough planets, stopping." );
                return; // No planets we can spawn at. Await a chance.
            }

            // Pick one of our best options.
            // Try to pick one of the best options we have, at least in the top half of preferred targets if we're in a small galaxy.
            int options = Math.Min( spawn_planets.Count, Math.Max( numberOfBestPlanetsToCheck, spawn_planets.Count / 2 ) );

            int spawnCount = 0;
            for ( int x = 0; x < planetsToSpawnOn; x++ )
                if ( SpawnMigrantFleet_Helper( spawn_planets[context.RandomToUse.Next( options )], migrantsToSpawnPerPlanet, returningMigrants, context ) )
                    spawnCount++;

            if ( spawnCount > 0 )
            {
                // Restart their timer.
                BaseInfo.Trigger( typeToSpawn );
            }

            Planet.ReleaseTemporaryPlanetList( friendlyTerritory );
            Planet.ReleaseTemporaryPlanetList( spawn_planets );
        }
        private bool SpawnMigrantFleet_Helper( Planet spawnPlanet, int toSpawn, bool returningMigrants, ArcenHostOnlySimContext Context )
        {
            if ( spawnPlanet == null )
                return false;

            bool spawned = false;
            for ( int x = 0; x < toSpawn; x++ )
            {
                GameEntity_Squad wormhole = spawnPlanet.Mapgen_SeedEntity( Context, AttachedFaction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, BaseInfo.NeinzulMigrantWormholeTag ), PlanetSeedingZone.OuterSystem );
                if ( wormhole != null )
                {
                    spawned = true;
                    if ( returningMigrants )
                    {
                        // If they've already been here before, they know where you live.
                        BaseInfo.SetMigrantHasBeenHereBefore( wormhole );
                        BaseInfo.KnownMigrantsOutsideOurGalaxy--; // They're back~
                    }
                    wormhole.SetCurrentMarkLevelIfHigherThanCurrent( BaseInfo.Difficulty.GetMarkLevel( BaseInfo ) );
                    wormhole.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                }
            }
            if ( spawned )
                // Send a journal.
                if ( BaseInfo.humanAlly )
                    if ( returningMigrants )
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( BaseInfo.Journal_Wormhole_Known, string.Empty, AttachedFaction, null, spawnPlanet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    else
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( BaseInfo.Journal_Wormhole_Unknown, string.Empty, AttachedFaction, null, spawnPlanet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            return spawned;
        }

        private void TransformMigrantWormholes( Faction faction, ArcenHostOnlySimContext context )
        {
            bool transformationOccured = false;
            BaseInfo.migrantWormholePlanets.GetDisplayList().ForEach( planet =>
            {
                if ( BaseInfo.GetOrStartWormholeTimer( planet ) >= BaseInfo.Migrant_WormholeWarpInTimer )
                {
                    transformationOccured = true;
                    foreach ( GameEntity_Squad wormhole in planet.GetPlanetFactionForFaction( faction ).Entities.Squads( BaseInfo.NeinzulMigrantWormholeTag ) )
                    {
                        bool shouldKnowYou = BaseInfo.GetMigrantHasBeenHereBefore( wormhole ); // Carry over from the wormhole.

                        GameEntity_Squad migrant = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( wormhole.PlanetFaction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( context, BaseInfo.NeinzulMigrantTag ), BaseInfo.Difficulty.GetMarkLevel( BaseInfo ), wormhole.PlanetFaction.FleetUsedAtPlanet, 0, wormhole.WorldLocation, context, "MigrantFleet-WormholeTransformIntoMigrant" );

                        if ( migrant != null )
                        {
                            migrant.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                            migrant.SetCurrentMarkLevelIfHigherThanCurrent( BaseInfo.Difficulty.GetMarkLevel( BaseInfo ) );

                            BaseInfo.SetMigrantOriginPlanet( migrant, migrant.Planet );

                            if ( shouldKnowYou )
                            {
                                if ( BaseInfo.humanAlly )
                                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( BaseInfo.Journal_MigrantArrives_Known, string.Empty, AttachedFaction, null, planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );

                                BaseInfo.SetMigrantHasBeenHereBefore( migrant );

                                // If they know us; they brought some of their children along.
                                int toSpawn = context.RandomToUse.Next( 0, 5 );
                                for ( int x = 0; x < toSpawn; x++ )
                                {
                                    GameEntity_Squad clanling = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( migrant.PlanetFaction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( context,
                                        BaseInfo.NeinzulClanlingTag ), BaseInfo.Difficulty.GetMarkLevel( BaseInfo ), migrant.PlanetFaction.FleetUsedAtPlanet, 0, migrant.WorldLocation, context, "MigrantFleets-TransformWH" );
                                    if ( clanling != null )
                                        clanling.MinorFactionStackingID = migrant.PrimaryKeyID;
                                }
                            }
                            else if ( BaseInfo.humanAlly )
                                World_AIW2.Instance.QueueLogJournalEntryToSidebar( BaseInfo.Journal_MigrantArrives_Unknown, string.Empty, AttachedFaction, null, planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                        }

                        wormhole.Despawn( context, true, InstancedRendererDeactivationReason.IAmTransforming );
                    }

                    BaseInfo.StopWormholeTimer( planet );
                }
            } );
            if ( transformationOccured )
                BaseInfo.PurgeOldEntries();
        }

        private void RepairOrWarpOutMigrants( Faction faction, ArcenHostOnlySimContext context )
        {
            List<Planet> friendlyTerritory = Planet.GetTemporaryPlanetList( "Migrants-RepairOrWarpOutMigrants-friendlyTerritory", 10f );
            if ( friendlyTerritory == null ) //blocked for teardown/shutdown; bail
                return;

            BaseInfo.GetFriendlyTerritory( friendlyTerritory );

            BaseInfo.migrants.GetDisplayList().ForEach( migrantWrap =>
            {
                GameEntity_Squad migrant = migrantWrap.GetSquad();
                if ( migrant == null )
                    return;
                if ( !friendlyTerritory.Contains( migrant.Planet ) || migrant.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength > 1000 )
                {
                    // Not safe. Stop.
                    BaseInfo.StopMigrantSafetyTimer( migrant );
                    return;
                }

                if ( migrant.GetCurrentHullPoints() < migrant.GetMaxHullPoints() )
                {
                    // We're hurt, but safe. Heal a bit.
                    BaseInfo.StopMigrantSafetyTimer( migrant );
                    migrant.TakeHullRepair( 1000 );
                }
                else if ( BaseInfo.MigrantSafeLongEnoughToWarpOutOfGalaxy( migrant, BaseInfo.GetOrStartMigrantSafetyTimer( migrant ) ) )
                {
                    // We've been safe for long enough. Warp out and leave goodies.
                    for ( int x = 0; x < BaseInfo.Difficulty.GetChambersPerEnclave( BaseInfo ); x++ )
                    {
                        GameEntityTypeData chamberData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( context, BaseInfo.NeinzulChamberTag );
                        GameEntity_Squad chamber = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( migrant.PlanetFaction, chamberData, BaseInfo.Difficulty.GetMarkLevel( BaseInfo ),
                            migrant.PlanetFaction.FleetUsedAtPlanet, 0, migrant.Planet.GetSafePlacementPoint_SpecificPoint( context, chamberData, migrant.WorldLocation, 0, 2500 ), context, "MigrantFleets-LeaveGoodies" );

                        if ( chamber != null )
                        {
                            chamber.TransformsIntoAfterTime = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( context, BaseInfo.NeinzulClanlingTag ).InternalName;
                            chamber.SecondsTillTransformation = BaseInfo.Migrant_ChamberTimer;
                        }
                    }

                    if ( BaseInfo.humanAlly )
                    {
                        if ( BaseInfo.GetMigrantHasBeenHereBefore( migrant ) )
                            World_AIW2.Instance.QueueLogJournalEntryToSidebar( BaseInfo.Journal_MigrantLeaves_Known, string.Empty, AttachedFaction, null, migrant.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                        else
                            World_AIW2.Instance.QueueLogJournalEntryToSidebar( BaseInfo.Journal_MigrantLeaves_Unknown, string.Empty, AttachedFaction, null, migrant.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    }

                    // Get the Migrant out of here.
                    migrant.Despawn( context, true, InstancedRendererDeactivationReason.IFinishedMyJob );
                    BaseInfo.KnownMigrantsOutsideOurGalaxy++;
                }
            } );

            Planet.ReleaseTemporaryPlanetList( friendlyTerritory );
        }

        private void AttachClanlingUnits( Faction faction, ArcenHostOnlySimContext context )
        {
            List<SafeSquadWrapper> potentialLeaders = GameEntity_Squad.GetTemporarySquadList( "MigrantFleetsFactionDeepInfo-AttachClanlingUnits-potentialLeaders", 10f );
            if ( potentialLeaders == null ) //blocked for teardown/shutdown; bail
                return;

            // Find entities that we deem 'leaders'.
            if ( BaseInfo.humanAlly )
            {
                // As the ally of a human, we simply find fleet leaders.
                foreach ( Faction workingFaction in World_AIW2.Instance.Factions )
                {
                    if ( workingFaction.Type != FactionType.Player || !faction.GetIsFriendlyTowards( workingFaction ) )
                        continue;

                    foreach ( GameEntity_Squad leader in workingFaction.Squads( EntityRollupType.FleetLeaders ) )
                    {
                        int fleetLines = 0;
                        foreach ( FleetMembership mem in leader.FleetMembership.Fleet.MemberGroupsUnsorted_Sim )
                        {
                            fleetLines++;
                            if ( fleetLines > 1 )
                                break;

                        }
                        if ( fleetLines > 1 )
                            potentialLeaders.Add( leader );
                    }
                }
            }
            else
            {
                // Otherwise, we find their strong units.
                int maxStrengthFound = 0;
                foreach ( Faction workingFaction in World_AIW2.Instance.Factions )
                {
                    if ( !faction.GetIsFriendlyTowards( workingFaction ) )
                        continue;

                    foreach ( GameEntity_Squad leader in workingFaction.Squads( EntityRollupType.MobileCombatants ) )
                    {
                        if ( leader.GetStrengthPerSquad() > maxStrengthFound )
                            maxStrengthFound = leader.GetStrengthPerSquad();

                        if ( leader.GetStrengthPerSquad() >= maxStrengthFound / 2 )
                            potentialLeaders.Add( leader );
                    }
                }
                // Trim any that no longer make the cut.
                potentialLeaders.RemoveAll( leader => leader.GetStrengthPerSquad() < maxStrengthFound / 2 );
            }
            // Attach a clanling to each leader at random.
            // Right now this is entirely random. Completly possible to have that one Battlestation you control become the hub of Clanling life.
            // I personally enjoy this random aspect, but testing should determine if people want it to be more uniformly distributed.
            if ( potentialLeaders.Count > 0 )
            {
                BaseInfo.clanlingsWithoutParent.GetDisplayList().ForEach( clanlingWrap =>
                {
                    GameEntity_Squad clanling = clanlingWrap.GetSquad();
                    if ( clanling == null )
                        return;
                    clanling.MinorFactionStackingID = potentialLeaders[context.RandomToUse.Next( potentialLeaders.Count )].PrimaryKeyID;
                } );
                if ( BaseInfo.humanAlly )
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( BaseInfo.Journal_ClanlingBond, string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }


            GameEntity_Squad.ReleaseTemporarySquadList( potentialLeaders );
        }

        public override void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        {
            if ( entity.TypeData.GetHasTag( BaseInfo.NeinzulMigrantTag ) )
                BaseInfo.PurgeOldEntries();
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            BaseInfo.cachedMovementTargets.Clear();

            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();

            try
            {
                MoveMigrants( AttachedFaction, Context );

                MoveClanlings_LRP( AttachedFaction, Context, pathingCacheData );
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Migrant Fleets LRP error: " + e, Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();
            }
        }

        private void MoveMigrants( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            List<Planet> friendlyTerritory = Planet.GetTemporaryPlanetList( "Migrants-MoveMigrants-friendlyTerritory", 10f );
            if ( friendlyTerritory == null ) //blocked for teardown/shutdown; bail
                return;

            BaseInfo.GetFriendlyTerritory( friendlyTerritory );

            foreach ( GameEntity_Squad migrant in faction.Squads( BaseInfo.NeinzulMigrantTag ) )
            {
                if ( UnitHasWormholeCommand( migrant ) )
                    continue; // Skip if we're already moving.

                if ( BaseInfo.MigrantShouldStayOnHostilePlanet( migrant ) )
                    continue; // Wait a bit on each planet.

                if ( friendlyTerritory.Contains( migrant.Planet ) )
                {
                    // We're currently on friendly territory.  Move around so we don't look static.
                    if ( !migrant.Orders.GetHasAnyOrdersOfType( EntityOrderType.Move_Normal ) && migrant.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength < 1000 )
                    {
                        // No hostiles here, move around and stretch our shippy appendages.
                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCPatrol], GameCommandSource.AnythingElse );
                        command.RelatedString = "MigrantFleets_MigrantPatrolOnPlanet";
                        command.RelatedEntityIDs.Add( migrant.PrimaryKeyID );
                        command.ToBeQueued = false;
                        command.RelatedPoints.Add( migrant.Planet.GetRandomPointWithinCircleAndAlsoWithinGravWell( Engine_AIW2.Instance.CombatCenter, migrant.Planet.GravWellSize.DistanceScale_GravwellRadius / 2, Context.RandomToUse ) );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                    }
                }
                else if ( BaseInfo.MigrantKnowsWhereToGoNext( migrant ) )
                {
                    // We know where our ally is as we've been here before, or we're in a safe enough location for our allies to have informed us. Head to them.
                    Planet next = BaseInfo.GetNextPlanetToMoveToToReachFriendlies( migrant, friendlyTerritory );

                    // If nowhere is safe, we are either on our allies one and only planet, or they have no planets. Either way, stop moving.
                    if ( next == null )
                        continue;

                    GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleSpecialMission], GameCommandSource.AnythingElse );
                    command.RelatedString = "MigrantFleets_KnownPath";
                    command.RelatedEntityIDs.Add( migrant.PrimaryKeyID );
                    command.ToBeQueued = false;
                    command.RelatedIntegers.Add( next.Index );
                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                }
                else
                {
                    // We have no idea what we're doing.
                    GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleSpecialMission], GameCommandSource.AnythingElse );
                    command.RelatedString = "MigrantFleets_PanicPath";
                    command.RelatedEntityIDs.Add( migrant.PrimaryKeyID );
                    command.ToBeQueued = false;
                    command.RelatedIntegers.Add( migrant.Planet.GetRandomNeighbor( false, Context ).Index );
                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                }
            }

            Planet.ReleaseTemporaryPlanetList( friendlyTerritory );
        }

        private void MoveClanlings_LRP( Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            //no sense using parallel on the LRP thread, we'll just take up more room on the CPU.  Anything LRP should almost always stick to its thread
            foreach ( GameEntity_Squad clanling in faction.Squads( BaseInfo.NeinzulClanlingTag ) )
            {
                if ( World_AIW2.Instance.GetEntityByID_Squad( clanling.MinorFactionStackingID ) == null )
                    continue; // Skip if not attached.

                if ( UnitHasWormholeCommand( clanling ) )
                    continue; // Skip if moving.

                GameEntity_Squad leader = World_AIW2.Instance.GetEntityByID_Squad( clanling.MinorFactionStackingID );

                if ( clanling.Planet != leader.Planet )
                {
                    // follow the leader
                    PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "MigrantFleetsMoveClanlings", clanling.Planet, leader.Planet, PathingMode.Default, Context, PathCacheData );
                    if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                    {
                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse );
                        command.RelatedString = "MigrantClanlings_FollowLeaderToNewPlanet";
                        command.RelatedEntityIDs.Add( clanling.PrimaryKeyID );
                        for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                            command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                    }
                }
                else if ( clanling.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength < 1000 )
                {
                    // hug the leader
                    if ( clanling.GetDistanceTo_VeryCheapButExtremelyRough( leader.WorldLocation, RadiusCheck.SubtractRadiiFromDistance ) > 3000 )
                    {
                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCFollowGuardedUnit], GameCommandSource.AnythingElse );
                        command.RelatedString = "MigrantClanlings_HugLeader";
                        command.RelatedEntityIDs.Add( clanling.PrimaryKeyID );
                        command.RelatedPoints.Add( leader.WorldLocation );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                    }
                }
            }
        }

        private bool UnitHasWormholeCommand( GameEntity_Squad unit )
        {
            return unit.CalculateFinalDestinationPlanetIndex_Safe() != -1 && unit.CalculateFinalDestinationPlanetIndex_Safe() != unit.Planet.Index;
        }
    }
}
