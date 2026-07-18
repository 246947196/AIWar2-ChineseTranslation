using Arcen.AIW2.Core;
using System;
using System.Text;
using Arcen.Universal;

/*
  A helper class for auto-defend code
 */

namespace Arcen.AIW2.External
{
    public static class AutoDefendUtility
    {
        //Reusable weighted-selection scratch for the weighted GetRandomPlanetForPatrol overload.
        //[ThreadStatic] because AutoDefendUtility is a shared static helper that several factions'
        //LRP background threads can call concurrently (unlike FleetBehaviorLRP there is no shared
        //lock here), so a single static array would race. Each worker thread keeps its own buffer,
        //reused across calls; it replaces a per-call new int[workingList.Count].
        [ThreadStatic]
        private static int[] _patrolWeightScratch;
        public static void GoToLocation( GameEntity_Squad squad, ArcenPoint location, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathCacheData, Faction faction )
        {
            //We have the metal and the structure has the experience, so fly to it so we can upgrade it
            GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse );
            moveCommand.PlanetOrderWasIssuedFrom = squad.Planet.Index;
            moveCommand.RelatedPoints.Add( location );
            moveCommand.RelatedEntityIDs.Add( squad.PrimaryKeyID );
            World_AIW2.Instance.QueueGameCommand( faction, moveCommand, false );
        }
        public static void GoToPlanet( GameEntity_Squad squad, Planet planet, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathCacheData, PathingMode pathingMode = PathingMode.Default )
        {
            Faction faction = squad.PlanetFaction.Faction;
            PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( squad.PlanetFaction.Faction, "DysonSidekickLRP", squad.Planet, planet, pathingMode, Context, pathCacheData );
            if (pathCache != null && pathCache.PathToReadOnly.Count > 0)
            {
                GameCommand command = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleSpecialMission], GameCommandSource.AnythingElse);
                command.RelatedString = "AutoDefendUtility_MoveFlagship";
                command.RelatedEntityIDs.Add(squad.PrimaryKeyID);
                command.ToBeQueued = false;
                for (int k = 0; k < pathCache.PathToReadOnly.Count; k++)
                    command.RelatedIntegers.Add(pathCache.PathToReadOnly[k].Index);
                World_AIW2.Instance.QueueGameCommand( faction, command, false);
            }

        }

        //Dispatches a squad to a specific point on a (possibly distant) planet without the usual one-LRP-cycle
        //pause on arrival: if a cross-planet hop is needed, the SetWormholePath order and the local
        //MoveManyToOnePoint order are queued together in the same call (the local move is queued with
        //ToBeQueued = true and PlanetOrderWasIssuedFrom = targetPlanet.Index, which GameCommand_MoveManyToOnePoint
        //allows because GetDestinationPlanet() already reflects the just-queued wormhole order).
        //This mirrors how the player's right-click-to-a-remote-planet order is built in
        //PlanetViewSelector.HandleSecondaryClickOnEmptySpacePoint / EndpointFunctions.BringRemoteShipsToPlanet.
        public static void GoToPlanetThenLocation( GameEntity_Squad squad, Planet targetPlanet, ArcenPoint location, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathCacheData, PathingMode pathingMode = PathingMode.Default )
        {
            Faction faction = squad.PlanetFaction.Faction;
            if ( squad.Planet == targetPlanet )
            {
                GoToLocation( squad, location, Context, pathCacheData, faction );
                return;
            }

            PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "AutoDefendUtility_GoToPlanetThenLocation", squad.Planet, targetPlanet, pathingMode, Context, pathCacheData );
            if ( pathCache == null || pathCache.PathToReadOnly.Count <= 0 )
                return;

            GameCommand wormholeCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleSpecialMission], GameCommandSource.AnythingElse );
            wormholeCommand.RelatedString = "AutoDefendUtility_GoToPlanetThenLocation";
            wormholeCommand.RelatedEntityIDs.Add( squad.PrimaryKeyID );
            wormholeCommand.ToBeQueued = false;
            for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                wormholeCommand.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
            World_AIW2.Instance.QueueGameCommand( faction, wormholeCommand, false );

            GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse );
            moveCommand.PlanetOrderWasIssuedFrom = targetPlanet.Index;
            moveCommand.ToBeQueued = true;
            moveCommand.RelatedPoints.Add( location );
            moveCommand.RelatedEntityIDs.Add( squad.PrimaryKeyID );
            World_AIW2.Instance.QueueGameCommand( faction, moveCommand, false );
        }

        //Companion to GoToPlanetThenLocation: call this from a per-tick (Sim) validity check once the
        //caller has determined that the entity it was dispatching toward is gone (destinationOrNull == null).
        //If the squad still has queued orders from a prior GoToPlanetThenLocation dispatch (wormhole hop(s)
        //plus the trailing local move), this clears them so it doesn't blindly fly to the dead target's
        //last known location. A wormhole hop already in progress can't be undone, but the trailing local
        //move order - which is what would otherwise carry it into whatever is now sitting on that spot -
        //gets cancelled, and the faction's normal destination-selection logic can pick a new target next cycle.
        public static void CancelQueuedOrdersIfDestinationGone( GameEntity_Squad squad, GameEntity_Squad destinationOrNull )
        {
            if ( destinationOrNull != null )
                return;
            if ( !squad.HasQueuedOrders() )
                return;
            squad.Orders.ClearOrders( ClearBehavior.AnythingIncludingAttackerMode, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders,
                ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "DestinationGone" );
        }


        public static Planet GetRandomPlanetForPatrol( GameEntity_Squad squad, List<SafeSquadWrapper> strongholds, List<Planet> workingList, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathingCacheData)
        {
            workingList.Clear();
            Planet planet = squad.Planet;
            int myStrength = squad.GetStrengthOfSelfAndContents();
            //strongholds can also be "necropolises"
            for (int i = 0; i < strongholds.Count; i++ )
            {
                GameEntity_Squad stronghold = strongholds[i].GetSquad();
                if ( stronghold == null )
                    continue;
                if ( stronghold.Planet == squad.Planet )
                    continue;
                short hops = 0;
                int danger = Fireteam.GetDangerOfPath( squad.PlanetFaction.Faction, Context, pathingCacheData, planet, stronghold.Planet, true, out hops );
                if ( danger > myStrength )
                    continue;
                workingList.Add( stronghold.Planet );
            }
            if ( workingList.Count == 0 )
                return null;
            return workingList[Context.RandomToUse.Next(0, workingList.Count)];
        }
        public static Planet GetRandomPlanetForPatrol( GameEntity_Squad squad, List<Planet> planetsToDefend, List<Planet> workingList, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathingCacheData)
        {
            //this version just takes a list of planets
            workingList.Clear();
            Planet planet = squad.Planet;
            int myStrength = squad.GetStrengthOfSelfAndContents();
            for (int i = 0; i < planetsToDefend.Count; i++ )
            {
                Planet potentialPlanet = planetsToDefend[i];
                if ( potentialPlanet == null )
                    continue;
                if ( potentialPlanet == squad.Planet )
                    continue;
                short hops = 0;
                int danger = Fireteam.GetDangerOfPath( squad.PlanetFaction.Faction, Context, pathingCacheData, squad.Planet, potentialPlanet, true, out hops );
                if ( danger > myStrength )
                    continue;
                workingList.Add( potentialPlanet );
            }
            if ( workingList.Count == 0 )
                return null;

            // Weight each candidate by the highest hostile strength on any adjacent planet.
            // A planet bordering heavy enemy presence is more attractive to patrol toward.
            // Minimum weight of 1 so every candidate retains a non-zero chance.
            Faction squadFaction = squad.PlanetFaction.Faction;
            //Reuse the per-thread scratch buffer instead of allocating new int[workingList.Count]
            //each call. Every entry in [0, workingList.Count) is written below before it is read,
            //so no clear of the (possibly larger) buffer is needed.
            int candidateCount = workingList.Count;
            int[] weights = _patrolWeightScratch;
            if ( weights == null || weights.Length < candidateCount )
                weights = _patrolWeightScratch = new int[candidateCount < 16 ? 16 : candidateCount];
            int totalWeight = 0;
            for ( int i = 0; i < workingList.Count; i++ )
            {
                int maxAdjacentHostile = 0;
                foreach ( Planet neighbor in workingList[i].LinkedNeighbors( false ) )
                {
                    PlanetFaction neighborPFac = neighbor.GetPlanetFactionForFaction( squadFaction );
                    if ( neighborPFac == null )
                        continue;
                    int hostileStr = neighborPFac.DataByStance[FactionStance.Hostile].TotalStrength;
                    if ( hostileStr > maxAdjacentHostile )
                        maxAdjacentHostile = hostileStr;
                }
                weights[i] = Math.Max( 1, maxAdjacentHostile );
                totalWeight += weights[i];
            }

            int roll = Context.RandomToUse.Next( 0, totalWeight );
            int cumulative = 0;
            for ( int i = 0; i < workingList.Count; i++ )
            {
                cumulative += weights[i];
                if ( roll < cumulative )
                    return workingList[i];
            }
            return workingList[workingList.Count - 1];
        }
        // Finds the safest reachable planet to retreat to: must be friendly or allied, have > 10
        // friendly/self strength, and have less enemy strength than our own strength.
        // Among valid candidates picks the one with the lowest path danger.
        // Returns null if no safe retreat destination exists.
        public static Planet GetSafestPlanetForRetreat( GameEntity_Squad squad, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathingCacheData )
        {
            Faction faction = squad.PlanetFaction.Faction;
            int myStrength = squad.GetStrengthOfSelfAndContents();
            Planet bestPlanet = null;
            int bestDanger = int.MaxValue;

            foreach ( Planet otherPlanet in World_AIW2.Instance.Planets( false ) )
            {
                if ( otherPlanet == squad.Planet )
                    continue;

                Faction controlling = otherPlanet.GetControllingOrInfluencingFaction();
                if ( controlling == null || !controlling.GetIsFriendlyTowards( faction ) )
                    continue;

                PlanetFaction pFac = otherPlanet.GetPlanetFactionForFaction( faction );
                if ( pFac == null )
                    continue;

                int friendlyStr = pFac.DataByStance[FactionStance.Self].TotalStrength + pFac.DataByStance[FactionStance.Friendly].TotalStrength;
                if ( friendlyStr <= 10 )
                    continue;

                int hostileStr = pFac.DataByStance[FactionStance.Hostile].TotalStrength;
                if ( hostileStr >= myStrength )
                    continue;

                short hops = 0;
                int danger = Fireteam.GetDangerOfPath( faction, Context, pathingCacheData, squad.Planet, otherPlanet, true, out hops );
                if ( danger < bestDanger )
                {
                    bestDanger = danger;
                    bestPlanet = otherPlanet;
                }
            }

            return bestPlanet;
        }

        public static Planet GetNearestShipyardToMe( Planet planet, List<SafeSquadWrapper> shipyards, Faction factionOrNull = null,
            ArcenLongTermIntermittentPlanningContext Context = null, PerFactionPathCache pathingCacheData = null )
        {
            Planet bestPlanet = null;
            if ( factionOrNull != null && Context != null && pathingCacheData != null )
            {
                // Pick the shipyard reachable via the safest path
                int bestDanger = int.MaxValue;
                for ( int i = 0; i < shipyards.Count; i++ )
                {
                    GameEntity_Squad shipyard = shipyards[i].GetSquad();
                    if ( shipyard == null )
                        continue;
                    short hops = 0;
                    int danger = Fireteam.GetDangerOfPath( factionOrNull, Context, pathingCacheData, planet, shipyard.Planet, true, out hops );
                    if ( danger < bestDanger )
                    {
                        bestDanger = danger;
                        bestPlanet = shipyard.Planet;
                    }
                }
            }
            else
            {
                // Fallback: nearest by hop count
                int hops = -1;
                for ( int i = 0; i < shipyards.Count; i++ )
                {
                    GameEntity_Squad shipyard = shipyards[i].GetSquad();
                    if ( shipyard == null )
                        continue;
                    if ( shipyard.Planet.GetHopsTo( planet ) < hops || hops == -1 )
                    {
                        hops = shipyard.Planet.GetHopsTo( planet );
                        bestPlanet = shipyard.Planet;
                    }
                }
            }
            return bestPlanet;
        }

        public static bool DoesPlanetHaveShipyard( Planet planet, List<SafeSquadWrapper> shipyards)
        {
            for (int i = 0; i < shipyards.Count; i++ )
            {
                if ( shipyards[i].Planet == planet ||
                     shipyards[i].Planet.GetIsDirectlyLinkedTo( false, planet ) )
                    return true;
            }
            return false;
        }
        public static void GetPlanetsUnderAttack( GameEntity_Squad squad, int myStrength, Planet startPlanet, List<Planet> underAttack, bool forceWardenMode = false )
        {
            //we sort the underAttack at the end for priority.
            //If some faction wants a custom sort then they are free to resort it when they get the list
            Faction faction = squad.PlanetFaction.Faction;
            bool inWardenMode = false;
            bool inTiberiumMode = false;
            if ( World_AIW2.Instance.Setup.GetStringBySetting("DysonAutoDefend") == "仅防御")
                inWardenMode = true;
            if ( forceWardenMode )
                inWardenMode = true;
            bool totalOffense = false;
            if ( squad.TypeData.GetHasTag("DysonSphereVictoryProduction" ) )
            {
                inWardenMode = false; //this is for the Dyson Sphere win condition
            }
            if ( squad.TypeData.GetHasTag("TiberiumAutoDefenseShip" ) )
            {
                inTiberiumMode = true; //this is for the tiberium defense ships
            }

            bool debug = false;
            if ( debug )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("\tGetting planets under attack for a " + faction.ToString() + " battle coordinator on " + startPlanet.Name, Verbosity.DoNotShow );
                if ( inTiberiumMode )
                    ArcenDebugging.LogSingleLine("\t\tTiberium Mode", Verbosity.DoNotShow );
                if ( inWardenMode )
                    ArcenDebugging.LogSingleLine("\t\tWarden Mode", Verbosity.DoNotShow );

            }
            
            short hopsToCheck = squad.TypeData.AllowedHopsFromCenterpiece;
            foreach ( Planet.PlanetAtHopDistance _phd in startPlanet.PlanetsWithinXHops( hopsToCheck,
                delegate (Planet secondaryPlanet)
                {
                    if ( (inWardenMode || inTiberiumMode) &&
                         secondaryPlanet.GetControllingOrInfluencingFaction().GetIsHostileTowards(faction))
                        return PropogationEvaluation.No;
                    if ( inTiberiumMode && !FactionUtilityMethods.Instance.DoesPlanetHaveTiberiumVein(secondaryPlanet) )
                    {
                        return PropogationEvaluation.SelfButNotNeighbors;
                    }
                    var pFaction = secondaryPlanet.GetStanceDataForFaction(faction);
                    if ( pFaction[FactionStance.Hostile].TotalStrength <= myStrength / 2 )
                        return PropogationEvaluation.Yes; //am I strong enough to just go regardless? This lets me pass through weak enemy planets

                    if (pFaction[FactionStance.Hostile].TotalStrength >=
                        ( pFaction[FactionStance.Self].TotalStrength + pFaction[FactionStance.Friendly].TotalStrength  ) )
                        return PropogationEvaluation.SelfButNotNeighbors; //this is too strong for me to just head through casually, and we don't have strong allies or friends
                    return PropogationEvaluation.Yes;
                }) )
            {
                Planet otherPlanet = _phd.Planet;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("\t\tChecking whether " + otherPlanet.Name + " is under attack", Verbosity.DoNotShow );
                if (FactionUtilityMethods.Instance.DoesPlanetHaveAIEye(otherPlanet) )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\t\t\tNo, there is an eye", Verbosity.DoNotShow );

                    continue; //don't go to planets with Eyes
                }
                if ( inTiberiumMode && otherPlanet.GetControllingOrInfluencingFaction().GetIsHostileTowards(faction) )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\t\t\tNo, this is a hostile planet", Verbosity.DoNotShow );

                    continue; //Tiberium ships can't go to hostile planets
                }

                if (FactionUtilityMethods.Instance.DoesPlanetHaveVengeanceGenerator(otherPlanet) )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\t\t\tNo, there is a VG", Verbosity.DoNotShow );

                    continue; //don't go to planets with Eyes
                }
                if ( FactionUtilityMethods.Instance.GetDysonSphereOnPlanetOrNull( otherPlanet ) != null )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\t\t\tNo, there is a Dyson Sphere", Verbosity.DoNotShow );
                    continue;
                }

                var pFaction = otherPlanet.GetStanceDataForFaction(faction);
                if ( totalOffense )
                {
                    //secondary path for dyson sphere produced ships
                    if ( pFaction[FactionStance.Hostile].TotalStrength > 5 * 1000 )
                        underAttack.Add(otherPlanet);
                    continue;
                }
                if ( otherPlanet.IsEligibleForDeepStrike && !inTiberiumMode )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\t\t\tNo, this is deepstrike", Verbosity.DoNotShow );

                    continue; //we are too badly outnumbered to go for it. Note this is a more generous check than the secondary delegate
                }

                if (pFaction[FactionStance.Hostile].TotalStrength >=
                    (pFaction[FactionStance.Self].TotalStrength + pFaction[FactionStance.Friendly].TotalStrength) * 3)
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\t\t\tNo, the enemy is too strong", Verbosity.DoNotShow );

                    continue; //we are too badly outnumbered to go for it. Note this is a more generous check than the secondary delegate
                }
                if (otherPlanet.GetControllingOrInfluencingFaction().GetIsHostileTowards(faction) && inWardenMode)
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\t\t\tNo, we are in Warden mode", Verbosity.DoNotShow );
                    continue;
                }
                if (AutoDefendUtility.IsPlanetUnderAttack(otherPlanet, faction))
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\t\t\tYes, that planet is under attack", Verbosity.DoNotShow );
                    underAttack.Add(otherPlanet);
                }
                else if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("\t\t\tNo, that planet is not under attack", Verbosity.DoNotShow );
            }

            if (underAttack.Count > 0)
            {
                //sort to prioritize the planets with the most enemy strength
                //since that's probably the hardest battle. Still more we can do (prioritizing defending the king, perhaps?),
                //but lets get feedback on this
                cb_underAttackSortFaction = faction;
                underAttack.Sort(static delegate (Planet Left, Planet Right)
                {
                    PlanetFaction lPFaction = Left.GetPlanetFactionForFaction(cb_underAttackSortFaction);
                    PlanetFaction rPFaction = Right.GetPlanetFactionForFaction(cb_underAttackSortFaction);

                    int lHostileStr = lPFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                    int rHostileStr = rPFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                    return lHostileStr.CompareTo(rHostileStr);
                });
            }
        }
        // Returns the planet that needs urgent help, or null if no critical battle is happening.
        // Two conditions qualify:
        //   1. Enemy forces on the king planet exceed 20% of total friendly+self strength there.
        //   2. Any friendly planet within 2 hops has enemy strength > our strength / 2.
        // King planet pressure takes priority; otherwise returns the first nearby critical planet found.
        public static Planet GetCriticalBattlePlanet( GameEntity_Squad centerpiece, Faction faction )
        {
            // Check 1: king planet under significant pressure
            GameEntity_Squad king = faction.GetFactionKing();
            if ( king != null )
            {
                PlanetFaction kingPFac = king.Planet.GetPlanetFactionForFaction( faction );
                if ( kingPFac != null )
                {
                    int kingHostile = kingPFac.DataByStance[FactionStance.Hostile].TotalStrength;
                    int kingFriendly = kingPFac.DataByStance[FactionStance.Self].TotalStrength
                                     + kingPFac.DataByStance[FactionStance.Friendly].TotalStrength;
                    if ( kingHostile > kingFriendly / 5 ) // > 20%
                        return king.Planet;
                }
            }

            // Check 2: friendly planet within 2 hops has enemies > our strength / 2
            int myStrength = centerpiece.GetStrengthOfSelfAndContents();
            Planet criticalPlanet = null;
            foreach ( Planet.PlanetAtHopDistance _phd in centerpiece.Planet.PlanetsWithinXHops_NoFilters( 2 ) )
            {
                Planet otherPlanet = _phd.Planet;
                if ( criticalPlanet != null )
                    continue;
                Faction controlling = otherPlanet.GetControllingOrInfluencingFaction();
                if ( controlling == null || !controlling.GetIsFriendlyTowards( faction ) )
                    continue;
                PlanetFaction pFac = otherPlanet.GetPlanetFactionForFaction( faction );
                if ( pFac == null )
                    continue;
                int enemyStrength = pFac.DataByStance[FactionStance.Hostile].TotalStrength;
                int friendlyStrength = pFac.DataByStance[FactionStance.Self].TotalStrength +
                                       pFac.DataByStance[FactionStance.Friendly].TotalStrength;
                if ( pFac.DataByStance[FactionStance.Hostile].TotalStrength > friendlyStrength / 2 )
                    criticalPlanet = otherPlanet;
            }
            return criticalPlanet;
        }

        public static bool IsLosingBattle( Planet planet, int myStrength, Faction faction, GameEntity_Squad flagshipOrNull = null )
        {
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction);
            int hostileStr = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;
            int friendlyStr = pFaction.DataByStance[FactionStance.Friendly].TotalStrength + pFaction.DataByStance[FactionStance.Self].TotalStrength;

            // If a flagship is provided, replace its nominal strength contribution with a
            // health-scaled version: a flagship at 50% hull counts for 50% of its strength.
            if ( flagshipOrNull != null )
            {
                int maxHull = flagshipOrNull.GetMaxHullPoints();
                if ( maxHull > 0 )
                {
                    int flagshipStr = flagshipOrNull.GetStrengthPerSquad();
                    int curHull = flagshipOrNull.GetCurrentHullPoints();
                    int scaledStr = (int)( (FInt)flagshipStr * (FInt)curHull / (FInt)maxHull );
                    friendlyStr = friendlyStr - flagshipStr + scaledStr;
                }
            }

            //possibly we should retreat if our buddies are too weak?
            if ( hostileStr * 2 > friendlyStr * 3 ) // retreat when outnumbered 1.5 to 1
                return true;

            // Retreat if on an AI planet with an active counterattack force that is not being
            // stalled — staying only generates more budget without preventing the launch.
            // Exception: if a human player has a fleet here that is NOT in Warden/Hunter mode,
            // they deliberately chose to attack this planet; stay and support them.
            if ( planet.GetIsControlledByFactionType( FactionType.AI ) &&
                 planet.PrecalculatedAICounterattackForcesStrength > 0 &&
                 !planet.AICounterAttacksNotSufficientToTryToSend )
            {
                bool nonWardenFleetPresent = false;
                foreach ( Faction otherFaction in World_AIW2.Instance.Factions )
                {
                    if ( nonWardenFleetPresent )
                        break;
                    if ( otherFaction.Type != FactionType.Player )
                        continue;
                    foreach ( Fleet fleet in World_AIW2.Instance.Fleets( otherFaction, FleetStatus.CenterpieceMustLiveOrLooseFleet ) )
                    {
                        if ( fleet.Behavior == FleetBehavior.WardenMode || fleet.Behavior == FleetBehavior.HunterMode )
                            continue;
                        GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                        if ( centerpiece != null && centerpiece.Planet == planet )
                        {
                            nonWardenFleetPresent = true;
                            break;
                        }
                    }
                }
                if ( !nonWardenFleetPresent )
                    return true;
            }

            return false;
        }
        public static bool CanISafelyLeavePlanet( GameEntity_Squad squad, int myStrength, Faction faction )
        {
            Planet planet = squad.Planet;
            if ( squad.GetIsCrippled() )
                return true;
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction(faction);
            int hostileStr = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;
            if ( hostileStr == 0 ) //no enemies, fine to leave
                return true;
            int friendlyStr = pFaction.DataByStance[FactionStance.Friendly].TotalStrength + pFaction.DataByStance[FactionStance.Self].TotalStrength;
            if ( friendlyStr < hostileStr / 10 )
                return true; //we are outnumbered very badly
            if ( myStrength == 0 )
                return true; //our fleet/ship has 0 strength (this probably means we are crippled and have no other ships left)

            if ( hostileStr < (friendlyStr - (myStrength * FInt.FromParts(2, 200)).IntValue ) ) //we are in a fight, but our allies have this covered if we leave
                return true;
            return false;
        }
        //Set immediately before underThreat.Sort(...) so the comparison can be a non-capturing
        //static delegate.  [ThreadStatic] because this is called from background planning threads.
        [ThreadStatic] private static Planet cb_threatSortStartPlanet;
        [ThreadStatic] private static Faction cb_underAttackSortFaction;

        public static void GetThreatenedPlanets( int myStrength, Planet startPlanet, List<Planet> underThreat, Faction faction, bool forceWardenMode = false )
        {
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine("\tGetting threatened planets for a flagship on " + startPlanet.Name, Verbosity.DoNotShow );
            foreach ( Planet.PlanetAtHopDistance _phd in startPlanet.PlanetsWithinXHops( -1,
                delegate (Planet secondaryPlanet)
                {
                    // Don't be so strict about following the AI's warden fleet rules
                    // if ( inWardenMode &&
                    //      secondaryPlanet.GetControllingOrInfluencingFaction().GetIsHostileTowards(faction))
                    //     return PropogationEvaluation.No;

                    var pFaction = secondaryPlanet.GetStanceDataForFaction(faction);
                    if ( pFaction[FactionStance.Hostile].TotalStrength <= myStrength / 2 )
                        return PropogationEvaluation.Yes; //am I strong enough to just go regardless? This lets me pass through weak enemy planets
                    if (pFaction[FactionStance.Hostile].TotalStrength >=
                         (pFaction[FactionStance.Self].TotalStrength + pFaction[FactionStance.Friendly].TotalStrength) * 3)
                        return PropogationEvaluation.SelfButNotNeighbors; //consider fights were we are outnumbered
                    return PropogationEvaluation.Yes;
                }) )
            {
                Planet otherPlanet = _phd.Planet;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("\t\tChecking whether " + otherPlanet.Name + " is threatened", Verbosity.DoNotShow );

                if (FactionUtilityMethods.Instance.DoesPlanetHaveAIEye(otherPlanet))
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\t\t\tNo, there is an eye", Verbosity.DoNotShow );

                    continue; //don't go to planets with Eyes
                }
                var pFaction = otherPlanet.GetStanceDataForFaction(faction);
                if (pFaction[FactionStance.Hostile].TotalStrength >= 1000 )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\t\t\tNo, there are enemies", Verbosity.DoNotShow );

                    continue; //we are too badly outnumbered to go for it. Note this is a more generous check than the secondary delegate
                }

                if (!otherPlanet.GetControllingOrInfluencingFaction().GetIsFriendlyTowards(faction))
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\t\t\tNo, this isn't one of our planets", Verbosity.DoNotShow );
                    continue;
                }
                if (AutoDefendUtility.IsPlanetThreatened(otherPlanet, faction))
                    underThreat.Add(otherPlanet);
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("\t\t\tNo, there is no threat", Verbosity.DoNotShow );
            }
            if (underThreat.Count > 0)
            {
                cb_threatSortStartPlanet = startPlanet;
                underThreat.Sort(static delegate (Planet Left, Planet Right)
                {
                    int leftHops = Left.GetHopsTo(cb_threatSortStartPlanet);
                    int rightHops = Right.GetHopsTo(cb_threatSortStartPlanet);
                    return leftHops.CompareTo(rightHops);
                });
            }
        }
        public static bool IsPlanetUnderAttack(Planet planet, Faction faction)
        {
            //If there is a battle going on here
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction(faction);
            StrengthData_PlanetFaction_Stance hostileData = pFaction.DataByStance[FactionStance.Hostile];
            int hostileStr = hostileData.TotalStrength;
            int friendlyStr = pFaction.DataByStance[FactionStance.Friendly].TotalStrength + pFaction.DataByStance[FactionStance.Self].TotalStrength;
            if ( hostileStr < 2000 )
                return false; //no enemies worth noticing
            // small pockets of mostly-cloaked enemies can strand fleets indefinitely; ignore them
            if ( hostileStr < 5000 && hostileData.CloakedStrength > hostileStr / 2 )
                return false;
            if ( hostileStr > friendlyStr * 3 )
                return false; //too many enemies!
            return true;
        }
        public static bool IsPlanetThreatened(Planet planet, Faction faction )
        {
            //Are there hostile mobile forces nearby? Not implemented
            if ( planet.IsWaveIncoming )
                return true;
            int nearbyUnengagedHostileMobileStrength = 0;
            FInt divisor = FInt.FromParts(1, 500);
            FInt divisorIncreaseRate = FInt.FromParts(2, 000);
            var factionData = planet.GetStanceDataForFaction( faction );
            StrengthData_PlanetFaction_Stance hostileStrengthData = factionData[FactionStance.Hostile];

            for ( int j = 0; j < hostileStrengthData.UnengagedMobileStrengthByHopCount.Length; j++, divisor *= divisorIncreaseRate )
            {
                int initialValue = (hostileStrengthData.UnengagedMobileStrengthByHopCount[j] / divisor).IntValue;
                nearbyUnengagedHostileMobileStrength += initialValue;
            }
            int friendlyStrengh = factionData[FactionStance.Self].TotalStrength + factionData[FactionStance.Friendly].TotalStrength;
            if ( nearbyUnengagedHostileMobileStrength > friendlyStrengh / 2)
                return true;
            return false;
        }

    }
}
