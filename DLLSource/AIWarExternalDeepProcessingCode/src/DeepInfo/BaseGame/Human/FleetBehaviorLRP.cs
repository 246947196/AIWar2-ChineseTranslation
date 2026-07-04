using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// Handles LRP (Long-Range Planning) for player fleets that have their FleetBehavior set to
    /// WardenMode or HunterMode. Called from each player faction's DoLongRangePlanning_OnBackgroundNonSimThread_Subclass.
    /// DLC4 (4_Forge_Of_Empires_Supporter) must be installed and enabled for this to do anything.
    /// </summary>
    public static class FleetBehaviorLRP
    {
        private static readonly List<Planet> WorkingPlanets = List<Planet>.Create_WillNeverBeGCed( 50, "FleetBehaviorLRP-WorkingPlanets" );
        private static readonly List<Planet> FriendlyPlanets = List<Planet>.Create_WillNeverBeGCed( 200, "FleetBehaviorLRP-FriendlyPlanets" );
        private static readonly List<Planet> HostilePlanets = List<Planet>.Create_WillNeverBeGCed( 200, "FleetBehaviorLRP-HostilePlanets" );
        private static readonly List<ArcenPoint> WorkingAnchors = List<ArcenPoint>.Create_WillNeverBeGCed( 10, "FleetBehaviorLRP-WorkingAnchors" );

        //Reusable per-anchor turret-count scratch for TryGetStrongPoint, parallel to WorkingAnchors.
        //Like WorkingAnchors it is only touched inside LRPLock (TryGetStrongPoint is documented as
        //LRPLock-only), so a plain static grown-on-demand array is safe; it replaces a per-call
        //new int[WorkingAnchors.Count].
        [ThreadStatic]
        private static int[] WorkingNearCounts;

        /// <summary>
        /// Guards the three shared working lists against concurrent access when multiple faction
        /// LRP threads call DoLRP simultaneously (e.g. player faction + sidekick factions).
        /// There are never very many players in a game, and this shouldn't take too long
        /// </summary>
        private static readonly object LRPLock = new object();

        private static bool DebugLogging = false;
        /// <summary>
        /// When true, logs each planet evaluated during HunterMode's hostile planet search,
        /// including why planets are accepted or skipped. Requires DebugLogging to also be true.
        /// </summary>
        private static bool VerboseHunterLogging = false;

        private static Expansion cachedDlc4Expansion = null;
        private static bool dlc4LookupDone = false;

        private static bool GetIsDlc4InstalledAndEnabled()
        {
            if ( !dlc4LookupDone )
            {
                cachedDlc4Expansion = ExpansionTable.Instance.GetRowByNameOrNullIfNotFound( "4_Forge_Of_Empires_Supporter" );
                dlc4LookupDone = true;
            }
            return cachedDlc4Expansion != null && cachedDlc4Expansion.IsInstalledAndEnabled;
        }

        public static void DoLRP( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( !GetIsDlc4InstalledAndEnabled() )
                return;

            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            lock ( LRPLock )
            try
            {
                FriendlyPlanets.Clear();
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( planet.GetControllingFaction().GetIsFriendlyTowards( faction ) )
                        FriendlyPlanets.Add( planet );
                }

                foreach ( Fleet fleet in World_AIW2.Instance.Fleets( faction, FleetStatus.CenterpieceMustLiveOrLooseFleet ) )
                {
                    if ( fleet.Behavior == FleetBehavior.UnderPlayerControl )
                        continue;

                    GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                    if ( centerpiece == null )
                        continue;

                    // Manage load mode before the HasQueuedOrders check so traveling fleets
                    // also get loaded up (same pattern as NecromancerEmpireFactionBaseInfo).
                    HandleLoadMode( fleet, centerpiece, faction );

                    // Retreat check runs BEFORE HasQueuedOrders so a fleet that is actively
                    // fighting (always has queued orders) can still break off when losing.
                    // Player-overridden fleets fight to the death and are exempt.
                    if ( ( fleet.Behavior == FleetBehavior.HunterMode || fleet.Behavior == FleetBehavior.WardenMode )
                         && fleet.PlayerOverridePlanetIndex < 0 )
                    {
                        int earlyCheckStr = centerpiece.GetStrengthPerSquad();
                        if ( AutoDefendUtility.IsLosingBattle( centerpiece.Planet, earlyCheckStr, faction, centerpiece ) )
                        {
                            Planet retreatPlanet = AutoDefendUtility.GetSafestPlanetForRetreat( centerpiece, Context, pathingCacheData );
                            if ( DebugLogging )
                                ArcenDebugging.ArcenDebugLogSingleLine(
                                    $"[FleetBehaviorLRP] {fleet.Behavior} {centerpiece?.TypeData?.InternalName ?? "?"} losing battle on {centerpiece?.Planet?.Name ?? "?"} (pre-order-check), retreating to {retreatPlanet?.Name ?? "none"}",
                                    Verbosity.DoNotShow );
                            if ( retreatPlanet != null )
                                GoToPlanetAndLoad( fleet, centerpiece, faction, retreatPlanet, Context, pathingCacheData, PathingMode.Safest );
                            continue;
                        }
                    }

                    // Critical battle check: fires even if the fleet has queued orders,
                    // but only once the centerpiece has been on this planet for > 30 seconds
                    // (avoids interrupting a fleet that just arrived somewhere).
                    if ( fleet.PlayerOverridePlanetIndex < 0
                         && centerpiece.GetSecondsSinceEnteringThisPlanet() > 30 )
                    {
                        Planet criticalPlanet = AutoDefendUtility.GetCriticalBattlePlanet( centerpiece, faction );
                        if ( criticalPlanet != null && criticalPlanet != centerpiece.Planet )
                        {
                            if ( DebugLogging )
                                ArcenDebugging.ArcenDebugLogSingleLine(
                                    $"[FleetBehaviorLRP] {fleet.Behavior} {centerpiece?.TypeData?.InternalName ?? "?"} on {centerpiece?.Planet?.Name ?? "?"} — critical battle on {criticalPlanet.Name}, redirecting",
                                    Verbosity.DoNotShow );
                            GoToPlanetAndLoad( fleet, centerpiece, faction, criticalPlanet, Context, pathingCacheData, PathingMode.Safest );
                            continue;
                        }
                    }

                    if ( centerpiece.HasQueuedOrders() )
                        continue;
                    if ( centerpiece.GetIsCrippled() )
                        continue;

                    switch ( fleet.Behavior )
                    {
                        case FleetBehavior.WardenMode:
                            RunWardenMode( fleet, centerpiece, faction, Context, pathingCacheData );
                            break;
                        case FleetBehavior.HunterMode:
                            RunHunterMode( fleet, centerpiece, faction, Context, pathingCacheData );
                            break;
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "FleetBehaviorLRP hit exception: " + e.ToString(), Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();
            }
        }

        /// <summary>
        /// Ensures the fleet is loaded when in transit or sitting peacefully (standby).
        /// Called before HasQueuedOrders() so mid-transit fleets are always kept loaded.
        /// Never issues an unload — that is handled by the run methods when the fleet engages.
        /// </summary>
        private static void HandleLoadMode( Fleet fleet, GameEntity_Squad centerpiece, Faction faction )
        {
            if ( fleet.IsFleetInTransportLoadMode )
                return; // already loaded — nothing to do

            Planet flagshipPlanet = centerpiece.Planet;
            if ( flagshipPlanet == null )
                return;

            Planet destPlanet = centerpiece.GetDestinationPlanet();
            bool inTransit = destPlanet != flagshipPlanet;

            bool shouldLoad;
            if ( inTransit )
                shouldLoad = true;
            else
            {
                // Only load on a peaceful planet; if enemies are present the run methods
                // will decide whether to unload and fight or load up and retreat.
                PlanetFaction pFac = flagshipPlanet.GetPlanetFactionForFaction( faction );
                if ( pFac == null )
                    return;
                bool hasEnemies = pFac.DataByStance[FactionStance.Hostile].TotalStrength > 1000 ||
                                  flagshipPlanet.GetControllingOrInfluencingFaction().GetIsHostileTowards( faction );
                shouldLoad = !hasEnemies;
            }

            if ( !shouldLoad )
                return;

            if ( DebugLogging )
                ArcenDebugging.ArcenDebugLogSingleLine(
                    $"[FleetBehaviorLRP] HandleLoadMode issuing LOAD fleet={fleet.NameRaw} flagship={centerpiece.TypeData.InternalName}" +
                    $" currentPlanet={flagshipPlanet.Name} destPlanet={destPlanet?.Name ?? "null"} inTransit={inTransit}",
                    Verbosity.DoNotShow );

            EnsureInLoadMode( fleet, centerpiece, faction );
        }

        /// <summary>
        /// Issues a SetTransportIntoLoadMode command. No-op if already in load mode.
        /// </summary>
        private static void EnsureInLoadMode( Fleet fleet, GameEntity_Squad centerpiece, Faction faction )
        {
            if ( fleet.IsFleetInTransportLoadMode )
                return;

            Planet flagshipPlanet = centerpiece.Planet;
            if ( flagshipPlanet == null )
                return;

            if ( DebugLogging )
                ArcenDebugging.ArcenDebugLogSingleLine(
                    $"[FleetBehaviorLRP] EnsureInLoadMode issuing LOAD for fleet={fleet.NameRaw}",
                    Verbosity.DoNotShow );

            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetTransportIntoLoadMode], GameCommandSource.AnythingElse );
            command.RelatedEntityIDs.Add( centerpiece.PrimaryKeyID );
            command.PlanetOrderWasIssuedFrom = flagshipPlanet.Index;
            command.ToBeQueued = false;
            World_AIW2.Instance.QueueGameCommand( faction, command, false );
        }

        /// <summary>
        /// Issues an UnloadTransports command so fleet members deploy and fight. No-op if already unloaded.
        /// </summary>
        private static void EnsureUnloaded( Fleet fleet, GameEntity_Squad centerpiece, Faction faction )
        {
            if ( !fleet.IsFleetInTransportLoadMode )
                return;

            if ( DebugLogging )
                ArcenDebugging.ArcenDebugLogSingleLine(
                    $"[FleetBehaviorLRP] EnsureUnloaded issuing UNLOAD for fleet={fleet.NameRaw}",
                    Verbosity.DoNotShow );

            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.UnloadTransports], GameCommandSource.AnythingElse );
            command.RelatedEntityIDs.Add( centerpiece.PrimaryKeyID );
            command.RelatedBools.Add( false ); // ToBeQueued
            World_AIW2.Instance.QueueGameCommand( faction, command, false );
        }

        /// <summary>
        /// Two-phase move: if the fleet is not yet loaded, issues the load command this tick and
        /// defers movement to the next LRP tick (by which point the sim will have processed the load).
        /// If already loaded, issues the movement order immediately.
        /// This ensures the flagship never departs before its members have boarded.
        /// </summary>
        private static void GoToPlanetAndLoad( Fleet fleet, GameEntity_Squad centerpiece, Faction faction, Planet dest,
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathingCacheData, PathingMode pathingMode = PathingMode.Default )
        {
            if ( !fleet.IsFleetInTransportLoadMode )
            {
                // Phase 1: load this tick; movement will be issued next LRP tick once confirmed loaded.
                EnsureInLoadMode( fleet, centerpiece, faction );
                return;
            }
            // Phase 2: already loaded — issue the movement order now.
            if ( DebugLogging )
                ArcenDebugging.ArcenDebugLogSingleLine( $"[FleetBehaviorLRP] GoToPlanetAndLoad: {centerpiece?.TypeData?.InternalName ?? "?"} on {centerpiece?.Planet?.Name ?? "?"} → {dest?.Name ?? "?"}", Verbosity.DoNotShow );
            AutoDefendUtility.GoToPlanet( centerpiece, dest, Context, pathingCacheData, pathingMode );
        }

        /// <summary>
        /// Clears the PlayerOverridePlanetIndex on the sim thread via a game command,
        /// so the LRP background thread never writes fleet state directly.
        /// </summary>
        private static void QueueClearPlayerOverride( Fleet fleet, Faction faction )
        {
            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.AnythingElse );
            command.RelatedIntegers.Add( fleet.FleetID );
            command.RelatedString = "ClearPlayerOverride";
            World_AIW2.Instance.QueueGameCommand( faction, command, false );
        }

        /// <summary>
        /// Handles the player-override state: when a player manually sends a Warden/Hunter fleet to a
        /// specific planet, it fights to the death there (no auto-retreat). Returns true if the override
        /// is still active and the caller should skip normal LRP logic for this tick.
        /// Clears the override once the battle on the target planet is won.
        /// </summary>
        private static bool HandlePlayerOverride( Fleet fleet, GameEntity_Squad centerpiece, Faction faction,
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathingCacheData )
        {
            if ( fleet.PlayerOverridePlanetIndex < 0 )
                return false;

            Planet overridePlanet = World_AIW2.Instance.GetPlanetByIndex( fleet.PlayerOverridePlanetIndex );
            if ( overridePlanet == null )
            {
                QueueClearPlayerOverride( fleet, faction );
                return false;
            }

            // Not yet on the override planet. If we have no queued orders the fleet never got
            // its movement command (or it drained) — re-issue it so we aren't stuck forever.
            if ( centerpiece.Planet != overridePlanet )
            {
                if ( !centerpiece.HasQueuedOrders() )
                {
                    if ( DebugLogging )
                        ArcenDebugging.ArcenDebugLogSingleLine(
                            $"[FleetBehaviorLRP] PlayerOverride: {centerpiece?.TypeData?.InternalName ?? "?"} has no orders toward override planet {overridePlanet.Name}, re-issuing move",
                            Verbosity.DoNotShow );
                    GoToPlanetAndLoad( fleet, centerpiece, faction, overridePlanet, Context, pathingCacheData );
                }
                return true; // suppress normal LRP until we arrive
            }

            // Arrived. Check whether the battle is over: no significant hostiles left.
            PlanetFaction pFac = overridePlanet.GetPlanetFactionForFaction( faction );
            if ( pFac != null )
            {
                int hostileStr = pFac.DataByStance[FactionStance.Hostile].TotalStrength;
                if ( hostileStr < 500 )
                {
                    // Battle won — resume autonomous behaviour.
                    QueueClearPlayerOverride( fleet, faction );
                    return false;
                }
            }

            // Still fighting. Stay put; suppress normal LRP (including retreat).
            return true;
        }

        /// <summary>
        /// Scans for a "strong point" on the planet: an AIP-on-death structure that has more than
        /// 50% of the faction's living turrets within 20% of the gravity-well radius.
        /// Returns true and sets strongPoint if one is found; returns false otherwise.
        /// Must be called inside LRPLock.
        /// </summary>
        private static bool TryGetStrongPoint( Planet planet, Faction faction, out ArcenPoint strongPoint )
        {
            strongPoint = ArcenPoint.ZeroZeroPoint;

            if ( planet.GravWellSize == null )
                return false;

            PlanetFaction playerPFac = planet.GetPlanetFactionForFaction( faction );
            if ( playerPFac == null )
                return false;

            long clusterRadius = planet.GravWellSize.DistanceScale_GravwellRadius / 5;
            long clusterRadiusSq = clusterRadius * clusterRadius;

            // Collect candidate anchor points: AIP-on-death structures only.
            // The command station is intentionally excluded — the transport shelter fallback in
            // RunWardenMode handles sending fragile flagships there; Officer flagships should not
            // be drawn to the command station as a strong point.
            WorkingAnchors.Clear();
            foreach ( GameEntity_Squad anchor in playerPFac.Entities.Squads() )
            {
                if ( anchor.TypeData.AIPOnDeath > 0 || anchor.TypeData.AIPOnDeathWhenNoneLeft > 0 )
                    WorkingAnchors.Add( anchor.WorldLocation );
            }

            if ( WorkingAnchors.Count == 0 )
                return false;

            // Single pass over turrets: count how many fall within clusterRadius of each anchor.
            // Includes turrets from allied factions so a well-defended allied command station
            // is recognised as a strong point too.
            int anchorCount = WorkingAnchors.Count;
            if ( WorkingNearCounts == null || WorkingNearCounts.Length < anchorCount )
                WorkingNearCounts = new int[anchorCount];
            else
                System.Array.Clear( WorkingNearCounts, 0, anchorCount );
            int[] nearCounts = WorkingNearCounts;
            int totalTurrets = 0;
            foreach ( Faction otherFaction in World_AIW2.Instance.Factions )
            {
                if ( faction.GetIsHostileTowards( otherFaction ) )
                    continue;
                PlanetFaction otherPFac = planet.GetPlanetFactionForFaction( otherFaction );
                if ( otherPFac == null )
                    continue;
                foreach ( GameEntity_Squad turret in otherPFac.Entities.Squads( EntityRollupType.ForReinforcementType_Turret ) )
                {
                    // Skip infinite-range turrets: they cover the whole planet and don't
                    // indicate a positional cluster worth rallying to.
                    foreach ( EntitySystemTypeData system in turret.TypeData.SystemTypes )
                        if ( system.IsSniperRange )
                            continue;
                    totalTurrets++;
                    ArcenPoint tLoc = turret.WorldLocation;
                    for ( int i = 0; i < WorkingAnchors.Count; i++ )
                    {
                        if ( tLoc.GetSquareDistanceTo( WorkingAnchors[i] ) <= clusterRadiusSq )
                            nearCounts[i]++;
                    }
                }
            }

            if ( totalTurrets == 0 )
                return false;

            // Pick the first anchor where more than half of all turrets are nearby.
            int threshold = totalTurrets / 2 + 1;
            for ( int i = 0; i < WorkingAnchors.Count; i++ )
            {
                if ( nearCounts[i] >= threshold )
                {
                    strongPoint = WorkingAnchors[i];
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// If a strong point exists on the planet, moves the flagship there and switches all mobile
        /// combatants to attack-move so they hold near the defensive cluster rather than pursuing
        /// enemies across the planet.
        /// </summary>
        private static bool RallyIfStrongPoint( Fleet fleet, GameEntity_Squad centerpiece, Faction faction,
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathingCacheData, out bool wasThrottled, bool forceRun = false )
        {
            wasThrottled = false;

            ArcenPoint strongPoint;
            if ( !TryGetStrongPoint( centerpiece.Planet, faction, out strongPoint ) )
            {
                if ( DebugLogging )
                    ArcenDebugging.ArcenDebugLogSingleLine( $"[FleetBehaviorLRP] RallyIfStrongPoint: no strong point found on {centerpiece?.Planet?.Name ?? "?"}", Verbosity.DoNotShow );
                return false;
            }

            // If we're already within 20% of the gravwell radius from the strong point, we've arrived — no need to rally.
            long gravwellThreshold = centerpiece.Planet.GravWellSize.DistanceScale_GravwellRadius / 5;
            if ( centerpiece.WorldLocation.GetSquareDistanceTo( strongPoint ) <= gravwellThreshold * gravwellThreshold )
            {
                if ( DebugLogging )
                    ArcenDebugging.ArcenDebugLogSingleLine( $"[FleetBehaviorLRP] RallyIfStrongPoint: already at strong point on {centerpiece?.Planet?.Name ?? "?"}", Verbosity.DoNotShow );
                return false;
            }

            if ( DebugLogging )
                ArcenDebugging.ArcenDebugLogSingleLine( $"[FleetBehaviorLRP] RallyIfStrongPoint: strong point at {strongPoint} on {centerpiece?.Planet?.Name ?? "?"}, rallying {centerpiece?.TypeData?.InternalName ?? "?"}", Verbosity.DoNotShow );

            // Direct the flagship toward the strong point.
            AutoDefendUtility.GoToLocation( centerpiece, strongPoint, Context, pathingCacheData, faction );

            // Switch mobile combatants to attack-move so they hold near the strong point
            // rather than pursuing enemies across the planet.
            GameCommand behaviorCmd = GameCommand.Create(
                BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetBehavior_FromFaction_SomeOtherReason],
                GameCommandSource.AnythingElse );
            behaviorCmd.RelatedMagnitude = (int)EntityBehaviorType.Attacker_PursueOnlyInRange;
            foreach ( GameEntity_Squad entity in fleet.Entities )
            {
                if ( entity != null && entity.TypeData.IsMobileCombatant )
                    behaviorCmd.RelatedEntityIDs.Add( entity.PrimaryKeyID );
            }
            if ( behaviorCmd.RelatedEntityIDs.Count > 0 )
                World_AIW2.Instance.QueueGameCommand( faction, behaviorCmd, false );
            return true;
        }

        private static void RunWardenMode( Fleet fleet, GameEntity_Squad centerpiece, Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathingCacheData )
        {
            if ( HandlePlayerOverride( fleet, centerpiece, faction, Context, pathingCacheData ) )
                return;

            int myStrength = centerpiece.GetStrengthOfSelfAndContents();
            Planet dest = centerpiece.GetDestinationPlanet();

            if ( AutoDefendUtility.IsLosingBattle( centerpiece.Planet, myStrength, faction, centerpiece ) )
            {
                Planet retreatPlanet = AutoDefendUtility.GetSafestPlanetForRetreat( centerpiece, Context, pathingCacheData );
                if ( retreatPlanet != null )
                    GoToPlanetAndLoad( fleet, centerpiece, faction, retreatPlanet, Context, pathingCacheData, PathingMode.Safest );
                return;
            }

            // Not retreating — if enemies are on our current planet, deploy the fleet to fight.
            {
                PlanetFaction localPFac = centerpiece.Planet.GetPlanetFactionForFaction( faction );
                bool enemiesHere = localPFac != null && localPFac.DataByStance[FactionStance.Hostile].TotalStrength > 2000;
                if ( enemiesHere )
                {
                    int enemyStrength = localPFac.DataByStance[FactionStance.Hostile].TotalStrength;
                    if ( myStrength > enemyStrength / 2 )
                    {
                        // Fleet is strong enough to fight without cover — use Attacker_Full instead of rallying.
                        if ( DebugLogging )
                            ArcenDebugging.ArcenDebugLogSingleLine( $"[FleetBehaviorLRP] Strong enough to skip rally ({myStrength} > {enemyStrength}/2): {centerpiece?.TypeData?.InternalName ?? "?"} on {centerpiece?.Planet?.Name ?? "?"}", Verbosity.DoNotShow );
                        GameCommand behaviorCmd = GameCommand.Create(
                            BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetBehavior_FromFaction_SomeOtherReason],
                            GameCommandSource.AnythingElse );
                        behaviorCmd.RelatedMagnitude = (int)EntityBehaviorType.Attacker_Full;
                        foreach ( GameEntity_Squad entity in fleet.Entities )
                        {
                            if ( entity != null && entity.TypeData.IsMobileCombatant )
                                behaviorCmd.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                        }
                        if ( behaviorCmd.RelatedEntityIDs.Count > 0 )
                            World_AIW2.Instance.QueueGameCommand( faction, behaviorCmd, false );
                        EnsureUnloaded( fleet, centerpiece, faction );
                    }
                    else
                    {
                        bool forceRally = false;
                        if ( World_AIW2.Instance.GameSecond - centerpiece.GameSecondEnteredThisPlanet < 20 )
                            forceRally = true;
                        bool rallied = RallyIfStrongPoint( fleet, centerpiece, faction, Context, pathingCacheData, out bool throttled, forceRun: forceRally );
                        if ( !rallied && !throttled )
                        {
                            // Transport flagships are fragile — shelter at the command station
                            // rather than sitting exposed on the wormhole. Officer flagships
                            // are combat-capable and can hold their ground.
                            if ( centerpiece.TypeData.IsTransport )
                            {
                                GameEntity_Squad commandStation = centerpiece.Planet.GetCommandStationOrNull();
                                if ( commandStation != null && commandStation.PlanetFaction?.Faction == faction )
                                {
                                    // Only issue the move if we're not already near the command station —
                                    // otherwise the flagship oscillates into it every LRP cycle.
                                    bool alreadyNear = centerpiece.Planet.GravWellSize != null &&
                                        centerpiece.WorldLocation.GetSquareDistanceTo( commandStation.WorldLocation ) <=
                                        ( centerpiece.Planet.GravWellSize.DistanceScale_GravwellRadius / 5 ) *
                                        ( centerpiece.Planet.GravWellSize.DistanceScale_GravwellRadius / 5 );
                                    if ( !alreadyNear )
                                    {
                                        if ( DebugLogging )
                                            ArcenDebugging.ArcenDebugLogSingleLine(
                                                $"[FleetBehaviorLRP] Transport flagship sheltering at command station on {centerpiece?.Planet?.Name ?? "?"} (no strong point)",
                                                Verbosity.DoNotShow );
                                        AutoDefendUtility.GoToLocation( centerpiece, commandStation.WorldLocation, Context, pathingCacheData, faction );
                                    }
                                }
                            }
                            EnsureUnloaded( fleet, centerpiece, faction );
                        }
                        if ( DebugLogging )
                            ArcenDebugging.ArcenDebugLogSingleLine( $"[FleetBehaviorLRP] RallyIfStrongPoint? {rallied} (enemies present): {centerpiece?.TypeData?.InternalName ?? "?"} on {centerpiece?.Planet?.Name ?? "?"}", Verbosity.DoNotShow );
                    }
                }
            }

            if ( AutoDefendUtility.IsPlanetUnderAttack( dest, faction ) )
                return; // already in a battle or en route to a battle

            WorkingPlanets.Clear();
            AutoDefendUtility.GetPlanetsUnderAttack( centerpiece, myStrength, centerpiece.Planet, WorkingPlanets, forceWardenMode: true );
            if ( WorkingPlanets.Count > 0 )
            {
                GoToPlanetAndLoad( fleet, centerpiece, faction, WorkingPlanets[0], Context, pathingCacheData );
                return;
            }

            // Warden extension: join active battles on AI planets directly adjacent to a friendly
            // planet, but only if there is already significant friendly presence (>= 5000) fighting
            // there — we want to help finish a fight, not start one.
            {
                Planet adjacentAIBattle = null;
                for ( int i = 0; i < FriendlyPlanets.Count && adjacentAIBattle == null; i++ )
                {
                    foreach ( Planet neighbor in FriendlyPlanets[i].LinkedNeighbors( false ) )
                    {
                        if ( adjacentAIBattle != null )
                            continue;
                        if ( !neighbor.GetControllingOrInfluencingFaction().GetIsHostileTowards( faction ) )
                            continue;
                        if ( FactionUtilityMethods.Instance.DoesPlanetHaveAIEye( neighbor ) )
                            continue;
                        if ( FactionUtilityMethods.Instance.DoesPlanetHaveVengeanceGenerator( neighbor ) )
                            continue;
                        PlanetFaction neighborPFac = neighbor.GetPlanetFactionForFaction( faction );
                        if ( neighborPFac == null )
                            continue;
                        int neighborHostile = neighborPFac.DataByStance[FactionStance.Hostile].TotalStrength;
                        int neighborSelf = neighborPFac.DataByStance[FactionStance.Self].TotalStrength;
                        int neighborFriendly = neighborPFac.DataByStance[FactionStance.Self].TotalStrength
                                             + neighborPFac.DataByStance[FactionStance.Friendly].TotalStrength;
                        if ( neighborSelf <= 3000 )
                            continue; //if we don't have strength, don't go (ie "don't follow minor faction allies, this is the player's actual ships and we don't want to lose them")
                        if ( neighborFriendly < 5000 )
                            continue; // no meaningful fight already in progress
                        if ( neighborHostile < 500 || neighborHostile > myStrength * 2 )
                            continue;
                        adjacentAIBattle = neighbor;
                    }
                }
                if ( adjacentAIBattle != null )
                {
                    if ( DebugLogging )
                        ArcenDebugging.ArcenDebugLogSingleLine( $"[WardenMode] {centerpiece?.TypeData?.InternalName ?? "?"} joining border battle on adjacent AI planet {adjacentAIBattle.Name}", Verbosity.DoNotShow );
                    GoToPlanetAndLoad( fleet, centerpiece, faction, adjacentAIBattle, Context, pathingCacheData );
                    return;
                }
            }

            if ( AutoDefendUtility.IsPlanetThreatened( dest, faction ) )
            {
                if ( World_AIW2.Instance.GameSecond - centerpiece.GameSecondEnteredThisPlanet < 20 )
                {
                    //If we have just arrived, rally to a strong point if there is one
                    if ( DebugLogging )
                        ArcenDebugging.ArcenDebugLogSingleLine( $"[FleetBehaviorLRP] RallyIfStrongPoint (just arrived to threatened planet, forceRun): {centerpiece?.TypeData?.InternalName ?? "?"} on {centerpiece?.Planet?.Name ?? "?"}", Verbosity.DoNotShow );
                    RallyIfStrongPoint( fleet, centerpiece, faction, Context, pathingCacheData, out _, forceRun: true );
                }
                return; // There are no important battles right now, and we're on a planet that is Threatened
            }

            WorkingPlanets.Clear();
            AutoDefendUtility.GetThreatenedPlanets( myStrength, centerpiece.Planet, WorkingPlanets, faction, forceWardenMode: true );
            if ( WorkingPlanets.Count > 0 )
            {
                Planet threatenedPlanet = WorkingPlanets[Context.RandomToUse.Next( 0, WorkingPlanets.Count )];
                GoToPlanetAndLoad( fleet, centerpiece, faction, threatenedPlanet, Context, pathingCacheData );
                return;
            }

            if ( dest == centerpiece.Planet )
            {
                //Nothing is threatened, so either hang out here for a bit, or find a new planet to patrol
                if ( World_AIW2.Instance.GameSecond - centerpiece.GameSecondEnteredThisPlanet < 20 )
                {
                    //If we have just arrived, rally to a strong point if there is one
                    if ( DebugLogging )
                        ArcenDebugging.ArcenDebugLogSingleLine( $"[FleetBehaviorLRP] RallyIfStrongPoint (just arrived patrol path, forceRun): {centerpiece?.TypeData?.InternalName ?? "?"} on {centerpiece?.Planet?.Name ?? "?"}", Verbosity.DoNotShow );
                    RallyIfStrongPoint( fleet, centerpiece, faction, Context, pathingCacheData, out _, forceRun: true );
                    return;
                }

                Planet patrolPlanet = AutoDefendUtility.GetRandomPlanetForPatrol( centerpiece, FriendlyPlanets, WorkingPlanets, Context, pathingCacheData );
                if ( patrolPlanet != null )
                    GoToPlanetAndLoad( fleet, centerpiece, faction, patrolPlanet, Context, pathingCacheData );
            }
        }

        //Set immediately before HostilePlanets.Sort(...) so the comparison can be a non-capturing
        //static delegate.  [ThreadStatic] because long-range planning runs on a background thread.
        [ThreadStatic] private static Faction cb_hunterModeFaction;
        [ThreadStatic] private static ArcenLongTermIntermittentPlanningContext cb_hunterModeContext;
        [ThreadStatic] private static PerFactionPathCache cb_hunterModePathCache;
        [ThreadStatic] private static GameEntity_Squad cb_hunterModeCenterpiece;

        private static void RunHunterMode( Fleet fleet, GameEntity_Squad centerpiece, Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathingCacheData )
        {
            if ( DebugLogging )
                ArcenDebugging.ArcenDebugLogSingleLine( $"[HunterMode] {centerpiece?.TypeData?.InternalName ?? "?"} on {centerpiece?.Planet?.Name ?? "?"} entering hunter logic", Verbosity.DoNotShow );

            if ( HandlePlayerOverride( fleet, centerpiece, faction, Context, pathingCacheData ) )
                return;

            int myStrength = centerpiece.GetStrengthOfSelfAndContents();
            Planet dest = centerpiece.GetDestinationPlanet();

            if ( DebugLogging )
                ArcenDebugging.ArcenDebugLogSingleLine( $"[HunterMode] {centerpiece?.TypeData?.InternalName ?? "?"} on {centerpiece?.Planet?.Name ?? "?"} str={myStrength} dest={dest?.Name ?? "?"}", Verbosity.DoNotShow );

            if ( AutoDefendUtility.IsLosingBattle( centerpiece.Planet, myStrength, faction, centerpiece ) )
            {
                Planet retreatPlanet = AutoDefendUtility.GetSafestPlanetForRetreat( centerpiece, Context, pathingCacheData );
                if ( DebugLogging )
                    ArcenDebugging.ArcenDebugLogSingleLine( $"[HunterMode] {centerpiece?.TypeData?.InternalName ?? "?"} losing battle on {centerpiece?.Planet?.Name ?? "?"}, retreating to {retreatPlanet?.Name ?? "none"}", Verbosity.DoNotShow );
                if ( retreatPlanet != null )
                    GoToPlanetAndLoad( fleet, centerpiece, faction, retreatPlanet, Context, pathingCacheData, PathingMode.Safest );
                return;
            }

            // Not retreating — if enemies are on our current planet, deploy the fleet to fight.
            {
                PlanetFaction localPFac = centerpiece.Planet.GetPlanetFactionForFaction( faction );
                bool enemiesHere = localPFac != null && localPFac.DataByStance[FactionStance.Hostile].TotalStrength > 2000;
                if ( enemiesHere )
                {
                    if ( DebugLogging )
                        ArcenDebugging.ArcenDebugLogSingleLine( $"[HunterMode] {centerpiece?.TypeData?.InternalName ?? "?"} enemies present on {centerpiece?.Planet?.Name ?? "?"} (hostileStr={localPFac.DataByStance[FactionStance.Hostile].TotalStrength}), unloading", Verbosity.DoNotShow );
                    EnsureUnloaded( fleet, centerpiece, faction );
                }
            }

            if ( AutoDefendUtility.IsPlanetUnderAttack( dest, faction ) )
            {
                if ( DebugLogging )
                    ArcenDebugging.ArcenDebugLogSingleLine( $"[HunterMode] {centerpiece?.TypeData?.InternalName ?? "?"} dest {dest?.Name ?? "?"} is under attack, holding course", Verbosity.DoNotShow );
                return;
            }

            // Help with any active battle (including on hostile planets — no warden mode restriction)
            WorkingPlanets.Clear();
            AutoDefendUtility.GetPlanetsUnderAttack( centerpiece, myStrength, centerpiece.Planet, WorkingPlanets, forceWardenMode: false );
            if ( WorkingPlanets.Count > 0 )
            {
                if ( DebugLogging )
                    ArcenDebugging.ArcenDebugLogSingleLine( $"[HunterMode] {centerpiece?.TypeData?.InternalName ?? "?"} responding to battle on {WorkingPlanets[0]?.Name ?? "?"} ({WorkingPlanets.Count} candidates)", Verbosity.DoNotShow );
                GoToPlanetAndLoad( fleet, centerpiece, faction, WorkingPlanets[0], Context, pathingCacheData );
                return;
            }

            // Seek nearest hostile planet to pressure
            HostilePlanets.Clear();
            foreach ( Planet.PlanetAtHopDistance _phd in centerpiece.Planet.PlanetsWithinXHops( -1,
                delegate ( Planet secondaryPlanet )
                {
                    var pFaction = secondaryPlanet.GetStanceDataForFaction( faction );
                    if ( pFaction[FactionStance.Hostile].TotalStrength <= myStrength / 2 )
                        return PropogationEvaluation.Yes;
                    if ( pFaction[FactionStance.Hostile].TotalStrength >=
                         ( pFaction[FactionStance.Self].TotalStrength + pFaction[FactionStance.Friendly].TotalStrength ) * 3 )
                        return PropogationEvaluation.SelfButNotNeighbors;
                    return PropogationEvaluation.Yes;
                } ) )
            {
                Planet otherPlanet = _phd.Planet;
                var pFactionEarly = otherPlanet.GetStanceDataForFaction( faction );
                bool isEnemyControlled = otherPlanet.GetControllingOrInfluencingFaction().GetIsHostileTowards( faction );
                int hostileStrEarly = pFactionEarly[FactionStance.Hostile].TotalStrength;
                bool hasSignificantHostilePresence = hostileStrEarly > 2 * 1000;
                if ( !isEnemyControlled && !hasSignificantHostilePresence )
                {
                    if ( DebugLogging && VerboseHunterLogging )
                        ArcenDebugging.ArcenDebugLogSingleLine( $"[HunterMode] planet search: skipping {otherPlanet.Name} — not enemy-controlled and hostileStr={hostileStrEarly} < 2000", Verbosity.DoNotShow );
                    continue;
                }
                if ( FactionUtilityMethods.Instance.DoesPlanetHaveAIEye( otherPlanet ) )
                {
                    if ( DebugLogging )
                        ArcenDebugging.ArcenDebugLogSingleLine( $"[HunterMode] skipping {otherPlanet.Name}: has AI Eye", Verbosity.DoNotShow );
                    continue;
                }
                if ( FactionUtilityMethods.Instance.DoesPlanetHaveVengeanceGenerator( otherPlanet ) )
                {
                    if ( DebugLogging )
                        ArcenDebugging.ArcenDebugLogSingleLine( $"[HunterMode] skipping {otherPlanet.Name}: has Vengeance Generator", Verbosity.DoNotShow );
                    continue;
                }
                // Skip empty planets and "too strong" of targets
                if ( hostileStrEarly < 5 * 1000 )
                {
                    if ( DebugLogging && VerboseHunterLogging )
                        ArcenDebugging.ArcenDebugLogSingleLine( $"[HunterMode] planet search: skipping {otherPlanet.Name} — hostileStr={hostileStrEarly} below minimum (5000)", Verbosity.DoNotShow );
                    continue;
                }
                //Fundamentally, I want these Hunter fleets to only go after juicy targets
                //The player should be the one starting real battles
                FInt aggressionTuning = FInt.FromParts(0, 800 );
                if ( hostileStrEarly > myStrength * aggressionTuning )
                {
                    if ( DebugLogging && VerboseHunterLogging )
                        ArcenDebugging.ArcenDebugLogSingleLine( $"[HunterMode] planet search: skipping {otherPlanet.Name} — hostileStr={hostileStrEarly} > myStr={myStrength}*{aggressionTuning}, suicidal", Verbosity.DoNotShow );
                    continue;
                }
                if ( DebugLogging && VerboseHunterLogging )
                    ArcenDebugging.ArcenDebugLogSingleLine( $"[HunterMode] planet search: adding {otherPlanet.Name} — enemyControlled={isEnemyControlled} hostileStr={hostileStrEarly} myStr={myStrength}", Verbosity.DoNotShow );
                HostilePlanets.Add( otherPlanet );
            }

            if ( HostilePlanets.Count > 0 )
            {
                // Sort by path danger so we go to the most accessible hostile planet first.
                // A nearby planet through heavily-fortified AI space may be worse than a
                // slightly more distant one on a clear path.
                cb_hunterModeFaction = faction;
                cb_hunterModeContext = Context;
                cb_hunterModePathCache = pathingCacheData;
                cb_hunterModeCenterpiece = centerpiece;
                HostilePlanets.Sort( static delegate ( Planet left, Planet right )
                {
                    short hopsLeft = 0, hopsRight = 0;
                    int dangerLeft  = Fireteam.GetDangerOfPath( cb_hunterModeFaction, cb_hunterModeContext, cb_hunterModePathCache, cb_hunterModeCenterpiece.Planet, left,  true, out hopsLeft );
                    int dangerRight = Fireteam.GetDangerOfPath( cb_hunterModeFaction, cb_hunterModeContext, cb_hunterModePathCache, cb_hunterModeCenterpiece.Planet, right, true, out hopsRight );
                    return dangerLeft.CompareTo( dangerRight );
                } );
                if ( DebugLogging )
                    ArcenDebugging.ArcenDebugLogSingleLine( $"[HunterMode] {centerpiece?.TypeData?.InternalName ?? "?"} pressing nearest hostile {HostilePlanets[0]?.Name ?? "?"} ({HostilePlanets.Count} candidates)", Verbosity.DoNotShow );
                GoToPlanetAndLoad( fleet, centerpiece, faction, HostilePlanets[0], Context, pathingCacheData );
                return;
            }

            // Fallback: patrol friendly planets
            if ( dest == centerpiece.Planet )
            {
                Planet patrolPlanet = AutoDefendUtility.GetRandomPlanetForPatrol( centerpiece, FriendlyPlanets, WorkingPlanets, Context, pathingCacheData );
                if ( DebugLogging )
                    ArcenDebugging.ArcenDebugLogSingleLine( $"[HunterMode] {centerpiece?.TypeData?.InternalName ?? "?"} no hostiles found, patrolling to {patrolPlanet?.Name ?? "none"}", Verbosity.DoNotShow );
                if ( patrolPlanet != null )
                    GoToPlanetAndLoad( fleet, centerpiece, faction, patrolPlanet, Context, pathingCacheData );
            }
        }
    }
}
