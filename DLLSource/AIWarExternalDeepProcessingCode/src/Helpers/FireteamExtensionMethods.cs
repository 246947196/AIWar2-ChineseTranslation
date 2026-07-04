using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class FireteamExtensionMethods
    {

        public static void ApplyVassalMissionsToTargets( this Fireteam Me, Faction faction, List<FireteamTarget> preferredTargets, List<FireteamTarget> fallbackTargets, ArcenCharacterBuffer tracingBuffer = null )
        {
            bool fireteamTracing = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            List<VassalMission> activeMissions = VassalMission.GetTemporaryVassalMissionList("RemoveCompletedMissions", 20);
            if ( activeMissions == null ) //blocked for teardown/shutdown; bail
                return;
            int debugCode = 0;
            try{
                debugCode = 100;
                FactionUtilityMethods.Instance.GetActiveVassalMissions( faction, VassalMissionType.Offense, activeMissions );

                for ( int i = 0; i < activeMissions.Count; i++ )
                {
                    debugCode = 200;
                    VassalMission mission = activeMissions[i];
                    if ( mission == null )
                        continue;
                    if ( mission.Type == VassalMissionType.Defense && !Me.DefenseMode )
                        continue;
                    if ( mission.Type == VassalMissionType.Offense && Me.DefenseMode )
                        continue;
                    FireteamTarget target;
                    GameEntity_Squad TargetEntity = mission.TargetEntity.GetSquad();
                    if ( TargetEntity != null && !TargetEntity.GetHasBeenDestroyed() )
                    {
                        target = new FireteamTarget( TargetEntity );
                    }
                    else
                    {
                        Planet planet = mission.Planet;
                        if ( DoesTargetListContainPlanet( preferredTargets, planet ) )
                            target = GetTargetForPlanet( preferredTargets, planet );
                        else
                        {
                            //we did not have a suitable planet, so lets create one
                            target = new FireteamTarget( planet );
                            preferredTargets.Add( target );
                        }
                        target.priority = mission.Priority;
                        if ( mission.Priority == MissionPriority.Override )
                        {
                            fallbackTargets.Clear();
                            preferredTargets.Clear();
                            preferredTargets.Add( target );
                        }

                    }
                    target.priority = mission.Priority;
                    if ( mission.Priority == MissionPriority.Override )
                    {
                        fallbackTargets.Clear();
                        preferredTargets.Clear();
                        preferredTargets.Add( target );
                    }
                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in ApplyVassalMissionsToTargets debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            finally
            {
                VassalMission.ReleaseTemporaryVassalMissionList( activeMissions );
            }
        }

        private static bool DoesTargetListContainPlanet( List<FireteamTarget> targets, Planet planet )
        {
            for ( int i = 0; i < targets.Count; i++ )
            {
                if ( targets[i].planet == planet )
                    return true;
            }
            return false;
        }
        private static FireteamTarget GetTargetForPlanet( List<FireteamTarget> targets, Planet planet )
        {
            for ( int i = 0; i < targets.Count; i++ )
            {
                if ( targets[i].planet == planet )
                    return targets[i];
            }
            return targets[0];
        }

        public static GameEntity_Squad GetRetreatPoint( this Fireteam Me, Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            return faction.DeepInfo.GetFireteamRetreatPoint_OnBackgroundNonSimThread( Me.DeepInfo.CurrentPlanet, Context, PathCacheData );
        }

        private static readonly FInt DangerScaledAcceptFloor = FInt.FromParts( 0, 150 ); //a viable-but-dangerous target never drops below 15% of the base odds

        private static int GetDangerScaledAcceptPercent( int basePercent, int thisDangerOfPath, int baselineDangerOfPath, int teamStrength )
        {
            //The target list is sorted so that baselineDangerOfPath is the least-dangerous (best) target. We keep the
            //best target (and anything at least as safe, e.g. a high-priority target sorted to the front) at the full
            //basePercent odds, and scale the acceptance chance down for targets that are more dangerous to reach. The
            //team's own strength is used as the smoothing term so this self-scales across the game's huge danger range:
            //a strong team barely cares about danger gaps (spreads freely), a weak team is pickier about the safe target.
            if ( basePercent <= 0 )
                return basePercent;
            if ( thisDangerOfPath <= baselineDangerOfPath )
                return basePercent;
            int smoothing = teamStrength;
            if ( smoothing < 1000 )
                smoothing = 1000;
            FInt ratio = (FInt)(baselineDangerOfPath + smoothing) / (FInt)(thisDangerOfPath + smoothing);
            if ( ratio > FInt.One )
                ratio = FInt.One;
            if ( ratio < DangerScaledAcceptFloor )
                ratio = DangerScaledAcceptFloor;
            return ((FInt)basePercent * ratio).IntValue;
        }

        public static void GetTargetAndLurkPlanets( this Fireteam Me, Faction faction, ProtectedValDictionary<Planet, FireteamRegiment> TeamsAimedAtPlanet,
            out int targetsConsidered, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, ArcenCharacterBuffer tracingBuffer = null )
        {
            int debugCode = 0;
            targetsConsidered = 0;
            try{
            List<FireteamTarget> preferredTargets = FireteamTarget.GetTemporaryFireteamTargetList( "FireteamExtensionMethods-GetTargetAndLurkPlanets-preferredTargets", 120f );
            if ( preferredTargets == null ) //blocked for teardown/shutdown; bail
                return;
            List<FireteamTarget> fallbackTargets = FireteamTarget.GetTemporaryFireteamTargetList( "FireteamExtensionMethods-GetTargetAndLurkPlanets-fallbackTargets", 120f );
            if ( fallbackTargets == null ) //blocked for teardown/shutdown; bail
            {
                FireteamTarget.ReleaseTemporaryFireteamTargetList( preferredTargets );
                return;
            }
            debugCode = 100; 
            System.Diagnostics.Stopwatch stopwatch = null;
            bool trackTimings = (tracingBuffer != null);
            long getTargetsTime = 0;
            long getLurkTime = 0;
            long sortPreferredTime = 0;
            if ( trackTimings )
            {
                stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
            }
            debugCode = 200; 
            faction.DeepInfo.GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread( Me.DefenseMode, Me.DeepInfo.CurrentPlanet, Context, PathCacheData, preferredTargets, fallbackTargets, Me );
            if ( trackTimings )
            {
                stopwatch.Stop();
                getTargetsTime = stopwatch.ElapsedMilliseconds;
                stopwatch.Reset();
            }
            debugCode = 300; 
            if ( faction.IsVassal )
                Me.ApplyVassalMissionsToTargets( faction, preferredTargets, fallbackTargets );
            FireteamTarget bestTarget = new FireteamTarget();
            //initializing all the variables on a struct directly, without calling its constructor, is the most
            //efficient way to make sure that it compiles.  Using the constructor can cause "boxing" and thus heap memory usage (whereas structs normally just use the stack memory).
            //a struct that is not DEFINITELY initialized before usage in all cases won't compile with most compilers, and so this is needed to compile on Rosyln and in Visual Studio, both.
            if ( trackTimings )
                stopwatch.Start();
            FireteamUtility.SortTargets( Me, faction, Context, PathCacheData, ref preferredTargets, TeamsAimedAtPlanet, Me.DeepInfo.CurrentPlanet, tracingBuffer, false );
            if ( trackTimings )
            {
                stopwatch.Stop();
                sortPreferredTime = stopwatch.ElapsedMilliseconds;
                stopwatch.Reset();
            }
            debugCode = 400; 
            bestTarget.planet = null;
            bestTarget.targetSquad = null;
            Planet lurkForBestTarget = null; //used later to see if we ever had a good target
            bool fireteamTracing = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam ) && (tracingBuffer != null);
            targetsConsidered = preferredTargets.Count;
            debugCode = 500; 
            if ( fireteamTracing && tracingBuffer != null )
            {
                debugCode = 600; 
                if ( Me.SpecificationOrNull != null && Me.SpecificationOrNull.IsActive() )
                {
                    debugCode = 700; 
                    tracingBuffer.Add( "Finding a target for " + Me.FireTeamID + " (");
                    Me.SpecificationOrNull.ToDebugString( tracingBuffer );
                    tracingBuffer.Add( "), lurk start time " + Me.LurkStartTime + ". There are " + preferredTargets.Count + " preferred targets and " + fallbackTargets.Count + " fallback target. trackTimings " + trackTimings + "s\n" );
                }
                else
                {
                    debugCode = 800; 
                    tracingBuffer.Add( "Finding a target for " + Me.FireTeamID + " (no spec), lurk start time " + Me.LurkStartTime + ". There are " + preferredTargets.Count + " preferred targets and " + fallbackTargets.Count + " fallback targets. trackTimings " + trackTimings + "\n" );
                }
            }
            debugCode = 900;
            //The list is sorted least-dangerous first, so [0] is the baseline we scale other targets' odds against.
            int baselineDangerOfPath = preferredTargets.Count > 0 ? preferredTargets[0].dangerOfPath : 0;
            for ( int i = 0; i < preferredTargets.Count; i++ )
            {
                debugCode = 1000;
                FireteamTarget target = preferredTargets[i];
                if ( trackTimings )
                    stopwatch.Start();

                Planet lurkPlanet = faction.DeepInfo.GetFireteamLurkPlanet_OnBackgroundNonSimThread( target.planet, Me.DeepInfo.TeamStrength, Me.DeepInfo.CurrentPlanet, Context, PathCacheData );
                debugCode = 1100; 
                if ( trackTimings )
                {
                    stopwatch.Stop();
                    getLurkTime += stopwatch.ElapsedMilliseconds;
                    stopwatch.Reset();
                }
                debugCode = 1200; 
                if ( i == 0 && lurkPlanet != null )
                {
                    bestTarget = preferredTargets[i];
                    lurkForBestTarget = lurkPlanet;
                }
                if ( lurkPlanet != null )
                {
                    debugCode = 1300; 
                    //this is a legit target; decide whether to take it
                    if ( fireteamTracing && tracingBuffer != null )
                        tracingBuffer.Add( "\tFound lurk planet " + lurkPlanet.Name + " for preferred target " + i + ": " + target.GetPlanetName_Safe() + "\n" );
                    int percentToUse = Me.PercentBestTarget;
                    if ( Me.PercentDistanceBestTarget != -1 && Me.PreferredMaxDistance != -1 &&
                         target.planet.GetHopsTo( Me.DeepInfo.CurrentPlanet ) > Me.PreferredMaxDistance )
                        percentToUse = Me.PercentDistanceBestTarget;
                    //Scale the acceptance chance down for targets that are more dangerous to reach than the best one,
                    //so a team is far less likely to skip a clearly-safer target in favor of a much riskier one.
                    percentToUse = GetDangerScaledAcceptPercent( percentToUse, target.dangerOfPath, baselineDangerOfPath, Me.DeepInfo.TeamStrength );
                    if ( fireteamTracing && tracingBuffer != null )
                        tracingBuffer.Add( "\t\tdanger-scaled accept chance for " + target.GetPlanetName_Safe() + " is " + percentToUse + "% (path danger " + target.dangerOfPath + " vs baseline " + baselineDangerOfPath + ")\n" );
                    if ( Context.RandomToUse.Next( 0, 100 ) < percentToUse )
                    {
                        debugCode = 1400; 
                        if ( fireteamTracing && tracingBuffer != null )
                            tracingBuffer.Add( "\t\tchoosing preferred target " + i + " " + target.GetPlanetName_Safe() + "\n" );

                        if ( target.targetSquad != null )
                            Me.Target = target.targetSquad;
                        else
                            Me.Target = null;
                        Me.TargetPlanet = target.planet;
                        Me.LurkPlanet = lurkPlanet;
                        debugCode = 1500; 
                        if ( trackTimings )
                        {
                            if ( tracingBuffer != null )
                                tracingBuffer.Add( "Time spent getting targets: " + getTargetsTime + "ms, time spent sorting preferred targets: " + sortPreferredTime + "ms, time spent getting lurk planets: " + getLurkTime + "ms." ).Add( "\n" );
                            else
                                ArcenDebugging.ArcenDebugLogSingleLine( "Time spent getting targets: " + getTargetsTime + "ms, time spent sorting preferred targets: " + sortPreferredTime + "ms, time spent getting lurk planets: " + getLurkTime + "ms.", Verbosity.DoNotShow );
                        }

                        FireteamTarget.ReleaseTemporaryFireteamTargetList( preferredTargets );
                        FireteamTarget.ReleaseTemporaryFireteamTargetList( fallbackTargets );
                        return;
                    }
                }
            }
            debugCode = 1600; 
            if ( fallbackTargets != null && fallbackTargets.Count > 0 && Me.TargetPlanet == null )
            {
                debugCode = 1700; 
                if ( fireteamTracing && tracingBuffer != null )
                    tracingBuffer.Add( "Using a fallback target for " + Me.FireTeamID ).Add( "\n" );
                debugCode = 1710; 
                //we haven't found a target yet
                bool overrideDeathballing = false;
                if ( faction.SpecialFactionData.InternalName == "DarkZenith" )
                    overrideDeathballing = true; //the DZ ignores deathballing restrictions for its fallback targets
                debugCode = 1720; 
                FireteamUtility.SortTargets( Me, faction, Context, PathCacheData, ref fallbackTargets, TeamsAimedAtPlanet, Me.DeepInfo.CurrentPlanet, tracingBuffer, overrideDeathballing );
                targetsConsidered += fallbackTargets.Count;
                for ( int i = 0; i < fallbackTargets.Count; i++ )
                {
                    debugCode = 1800; 
                    FireteamTarget target = fallbackTargets[i];
                    if ( trackTimings )
                        stopwatch.Start();

                    Planet lurkPlanet = faction.DeepInfo.GetFireteamLurkPlanet_OnBackgroundNonSimThread( target.planet, Me.DeepInfo.TeamStrength, Me.DeepInfo.CurrentPlanet, Context, PathCacheData );
                    if ( trackTimings )
                    {
                        stopwatch.Stop();
                        getLurkTime += stopwatch.ElapsedMilliseconds;
                        stopwatch.Reset();
                    }

                    if ( lurkPlanet != null )
                    {
                        debugCode = 1900; 
                        //this is a legit target; choose whether to take it
                        if ( Context.RandomToUse.Next( 0, 100 ) < Me.PercentBestTarget )
                        {
                            if ( fireteamTracing && tracingBuffer != null )
                                tracingBuffer.Add( "\tFallback path. Use lurk planet " + lurkPlanet.Name + " for target planet " + target.GetPlanetName_Safe() + "\n" );

                            if ( target.targetSquad != null )
                                Me.Target = target.targetSquad;
                            else
                                Me.Target = null;
                            Me.TargetPlanet = target.planet;
                            Me.LurkPlanet = lurkPlanet;
                        }
                    }
                }
            }
            debugCode = 2000; 
            if ( Me.TargetPlanet == null && lurkForBestTarget != null && bestTarget.planet != null )
            {
                debugCode = 2100; 
                //if there are no fallbacks and we didn't pick a preferred target (but there are targets on the preferred list)
                //then just pick the best one. Otherwise some marauder fireteams just sit around.

                if ( fireteamTracing && tracingBuffer != null )
                    tracingBuffer.Add( "\tEscape path. Use lurk planet " + lurkForBestTarget.Name + " for target planet " + bestTarget.GetPlanetName_Safe() + "\n" );

                if ( bestTarget.targetSquad != null )
                    Me.Target = bestTarget.targetSquad;
                else
                    Me.Target = null;
                Me.TargetPlanet = bestTarget.planet;
                Me.LurkPlanet = lurkForBestTarget;
            }
            debugCode = 2200; 
            if ( trackTimings )
            {
                if ( tracingBuffer != null )
                    tracingBuffer.Add( "Time spent getting targets: " + getTargetsTime + "ms, time spent sorting preferred targets: " + sortPreferredTime + "ms, time spent getting lurk planets: " + getLurkTime + "ms." ).Add( "\n" );
                else
                {
                    //this is here so I can turn trackTimings on but not enable tracing, which is valuable for debugging sometimes
                    ArcenDebugging.ArcenDebugLogSingleLine( "Time spent getting targets: " + getTargetsTime + "ms, time spent sorting preferred targets: " + sortPreferredTime + "ms, time spent getting lurk planets: " + getLurkTime + "ms.", Verbosity.DoNotShow );
                }
            }
            debugCode = 2300; 
            FireteamTarget.ReleaseTemporaryFireteamTargetList( preferredTargets );
            FireteamTarget.ReleaseTemporaryFireteamTargetList( fallbackTargets );
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in GetTargetAndLurkPlanets for " +  faction.GetDisplayName() + " debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }

        public static void FindEscortTargetIfPossible( this Fireteam Me, List<Fireteam> otherEscortingFireteams, List<SafeSquadWrapper> escortables, 
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( escortables == null || escortables.Count == 0 )
                return;

            Dictionary<SafeSquadWrapper, int> workingEscortingFireteamsPerShip = GameEntity_Squad.GetTemporarySquadIntDict( "FireteamExtensionMethods-FindEscortTargetIfPossible-workingEscortingFireteamsPerShip", 10f );
            if ( workingEscortingFireteamsPerShip == null ) //blocked for teardown/shutdown; bail
                return;

            int fewestEscortingFound = -1;
            for ( int i = 0; i < escortables.Count; i++ )
            {
                GameEntity_Squad escortable = escortables[i].GetSquad();
                if ( escortable == null )
                    continue;
                short unused;
                int pathDanger = Fireteam.GetDangerOfPath( escortable.GetFactionOrNull_Safe(), Context, PathCacheData, escortable.Planet, Me.DeepInfo.CurrentPlanet, true, out unused );
                if ( pathDanger > Me.DeepInfo.TeamStrength * 2 )
                    continue; //we can't get to this escortable, so do nothing
                int fireteamsEscortingThis = 0;
                for ( int j = 0; j < otherEscortingFireteams.Count; j++ )
                {
                    Fireteam otherTeam = otherEscortingFireteams[j];
                    if ( otherTeam.Target == escortable )
                        fireteamsEscortingThis++;
                }
                workingEscortingFireteamsPerShip.Set( escortable, fireteamsEscortingThis );
                if ( fewestEscortingFound == -1 ||
                     fireteamsEscortingThis < fewestEscortingFound )
                    fewestEscortingFound = fireteamsEscortingThis;
            }
            for ( int i = 0; i < escortables.Count; i++ )
            {
                GameEntity_Squad escortable = escortables[i].GetSquad();
                if ( escortable == null )
                    continue;
                if ( workingEscortingFireteamsPerShip.Get( escortable ) == fewestEscortingFound )
                {
                    Me.Target = escortable;
                    GameEntity_Squad.ReleaseTemporarySquadIntDict( workingEscortingFireteamsPerShip );
                    return;
                }
            }

            GameEntity_Squad.ReleaseTemporarySquadIntDict( workingEscortingFireteamsPerShip );
        }

        public static bool HandleEscortDuties( this Fireteam Me, Faction faction, ArcenLongTermIntermittentPlanningContext Context, 
            PerFactionPathCache PathCacheData, ArcenCharacterBuffer tracingBuffer, float RequiredTimeBetweenCommands )
        {
            if ( Me.Target == null )
                return false; //this should be impossible; defensive only
            Planet targetPlanet = Me.Target.Planet;

            //if any of the ships in the list have been given orders too recently, then don't give orders to any of them right now
            for ( int i = 0; i < Me.DeepInfo.ShipsInFireteam.Count; i++ )
            {
                if ( ArcenTime.TimeSinceStartF - Me.DeepInfo.ShipsInFireteam[i].HostOnly_TimeWasLastGivenOrderFromLRP < RequiredTimeBetweenCommands )
                    return false;
            }

            List<SafeSquadWrapper> shipsToMoveToTargetPlanet = GameEntity_Squad.GetTemporarySquadList( "FireteamExts-HandleEscortDuties-shipsToMoveToTargetPlanet", 10f );
            if ( shipsToMoveToTargetPlanet == null ) //blocked for teardown/shutdown; bail
                return false;
            List<SafeSquadWrapper> shipsToMoveToEscortedShip = GameEntity_Squad.GetTemporarySquadList( "FireteamExts-HandleEscortDuties-shipsToMoveToEscortedShip", 10f );
            if ( shipsToMoveToEscortedShip == null ) //blocked for teardown/shutdown; bail
            {
                GameEntity_Squad.ReleaseTemporarySquadList( shipsToMoveToTargetPlanet );
                return false;
            }

            if ( tracingBuffer != null )
                tracingBuffer.Add( "Handling escort of " + Me.Target.ToStringWithPlanet() + " by fireteam " + Me.FireTeamID + " on " + Me.DeepInfo.CurrentPlanet.Name + "\n" );
            int wormholeRange = (ExternalConstants.Instance.MultiplierBase_GravwellRadius_ForUnitsSeedingNearAnother * FInt.FromParts( 0, 250 )).IntValue;
            int goToMeRange = (ExternalConstants.Instance.MultiplierBase_GravwellRadius_ForUnitsSeedingNearAnother * FInt.FromParts( 0, 250 )).IntValue;
            for ( int i = 0; i < Me.DeepInfo.ShipsInFireteam.Count; i++ )
            {
                GameEntity_Squad ship = Me.DeepInfo.ShipsInFireteam[i].GetSquad();
                if ( ship == null )
                    continue; //just in case
                EntityOrder shipOrder = ship.RemoveInvalidatedOrdersAndReturnFirstValid_IncludingDecollision();
                if ( ship.Planet != targetPlanet &&
                     (shipOrder.TypeData == null || shipOrder.TypeData.Type != EntityOrderType.Wormhole) )
                {
                    //if we aren't on a planet with our target and we don't have orders to go through a wormhole, follow the target                    
                    shipsToMoveToTargetPlanet.Add( ship );
                    continue;
                }
                if ( Me.Target.Orders.GetNextDestinationOrNull() != null && !ship.HasQueuedOrders() ) //if we aren't en route somewhere already, go toward the escort ships current location
                {
                    EntityOrder currentOrder = Me.Target.RemoveInvalidatedOrdersAndReturnFirstValid_IncludingDecollision();
                    if ( currentOrder.TypeData != null &&
                         currentOrder.TypeData.Type == EntityOrderType.Wormhole )
                    {
                        GameEntity_Other wormhole = currentOrder.GetWormholeToOtherPlanetOrNull( Me.Target.Planet );
                        if ( Mat.DistanceBetweenPointsImprecise( Me.Target.WorldLocation, wormhole.WorldLocation ) < wormholeRange )
                        {
                            shipsToMoveToEscortedShip.Add( ship );
                            continue;
                        }
                    }
                }

                if ( Mat.DistanceBetweenPointsImprecise( Me.Target.WorldLocation, ship.WorldLocation ) > goToMeRange &&
                     !ship.HasQueuedOrders() &&
                     Me.Target.Planet.GetDataByStanceForFaction( faction, FactionStance.Hostile ).TotalStrength == 0 )
                {
                    //if we are too far away, move us closer!
                    shipsToMoveToEscortedShip.Add( ship );
                    continue;
                }
            }
            if ( shipsToMoveToTargetPlanet.Count > 0 )
            {
                Me.SendFireteamToPlanet( faction, targetPlanet, Context, PathCacheData, tracingBuffer, Me.PreferredSpeed + Me.PreferredSpeed / 20 ); //go extra fast when moving between planets to get to our escort target
                if ( shipsToMoveToEscortedShip.Count == 0 )
                    shipsToMoveToEscortedShip.AddRange( shipsToMoveToTargetPlanet );
            }
            if ( shipsToMoveToEscortedShip.Count > 0 )
            {
                GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_FireteamEscort], GameCommandSource.AnythingElse );
                moveCommand.PlanetOrderWasIssuedFrom = Me.Target.Planet.Index;
                moveCommand.RelatedPoints.Add( Me.Target.WorldLocation );
                for ( int i = 0; i < shipsToMoveToEscortedShip.Count; i++ )
                    moveCommand.RelatedEntityIDs.Add( shipsToMoveToEscortedShip[i].PrimaryKeyID );
                World_AIW2.Instance.QueueGameCommand( faction, moveCommand, false );
            }

            GameEntity_Squad.ReleaseTemporarySquadList( shipsToMoveToTargetPlanet );
            GameEntity_Squad.ReleaseTemporarySquadList( shipsToMoveToEscortedShip );


            for ( int i = 0; i < Me.DeepInfo.ShipsInFireteam.Count; i++ )
                Me.DeepInfo.ShipsInFireteam[i].SetHostOnly_TimeWasLastGivenOrderFromLRP( ArcenTime.TimeSinceStartF );
            return true;
        }

        public static bool CampUnitsOnPlanet( this Fireteam Me, Faction faction, ArcenLongTermIntermittentPlanningContext Context, float RequiredTimeBetweenCommands )
        {
            if ( Context == null )
                return false; //client

            //if any of the ships in the list have been given orders too recently, then don't give orders to any of them right now
            for ( int i = 0; i < Me.DeepInfo.ShipsInFireteam.Count; i++ )
            {
                if ( ArcenTime.TimeSinceStartF - Me.DeepInfo.ShipsInFireteam[i].HostOnly_TimeWasLastGivenOrderFromLRP < RequiredTimeBetweenCommands )
                    return false;
            }

            //This function will make sure the waiting units look nice
            int debugCode = 0;
            try
            {
                ArcenPoint campingSpot = Engine_AIW2.Instance.CombatCenter.GetRandomPointWithinDistance( Context.RandomToUse, Me.DeepInfo.CurrentPlanet.GravWellSize.DistanceScale_GravwellRadius / 11, Me.DeepInfo.CurrentPlanet.GravWellSize.DistanceScale_GravwellRadius / 9 );
                if ( Me.DeepInfo.CurrentPlanet == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Null current planet for " + Me.FireTeamID, Verbosity.DoNotShow );
                }
                Faction controllingFaction = Me.DeepInfo.CurrentPlanet.GetControllingOrInfluencingFaction();
                debugCode = 100;
                if ( controllingFaction != null && controllingFaction.GetIsFriendlyTowards( faction ) && Me.DeepInfo.CurrentPlanet.GetCommandStationOrNull() != null )
                    campingSpot = Me.DeepInfo.CurrentPlanet.GetCommandStationOrNull().WorldLocation;
                debugCode = 200;
                int allowedDistance = Me.DeepInfo.CurrentPlanet.GravWellSize.DistanceScale_GravwellRadius / 5;
                Me.DeepInfo.PurgeDeadUnits();
                debugCode = 300;
                GameCommand campingCommand = null;
                for ( int i = 0; i < Me.DeepInfo.ShipsInFireteam.Count; i++ )
                {
                    debugCode = 400;
                    GameEntity_Squad entity = Me.DeepInfo.ShipsInFireteam[i].GetSquad();
                    if ( entity == null )
                        continue;
                    debugCode = 500;
                    int currentDistance = entity.WorldLocation.GetDistanceTo( campingSpot, false );
                    if ( currentDistance <= allowedDistance )
                        continue;
                    debugCode = 600;
                    if ( campingCommand == null )
                    {
                        campingCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_FireteamCamp], GameCommandSource.AnythingElse );
                        campingCommand.RelatedPoints.Add( campingSpot );
                    }

                    campingCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                }
                if ( campingCommand != null )
                    World_AIW2.Instance.QueueGameCommand( faction, campingCommand, false );
                return true;
            }
            catch ( ArcenPleaseStopThisThreadException )
            {
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "camp units error, debugCode " + debugCode + " exception " + e.ToString(), Verbosity.DoNotShow );
            }
            return false;
        }

        public static bool SendFireteamToPlanet( this Fireteam Me, Faction faction, Planet destination, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, 
            ArcenCharacterBuffer tracingBuffer, float RequiredTimeBetweenCommands, int OverridingSpeed = -1 )
        {
            if ( Context == null ) //client
                return false;

            bool tracing = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            Me.DeepInfo.BuildShipsLookup( !Me.DeepInfo.IncludeShipsInTransit, destination ); //needed to send ships to the lurk planet

            //if any of the ships in the list have been given orders too recently, then don't give orders to any of them right now
            for ( int i = 0; i < Me.DeepInfo.ShipsInFireteam.Count; i++ )
            {
                if ( ArcenTime.TimeSinceStartF - Me.DeepInfo.ShipsInFireteam[i].HostOnly_TimeWasLastGivenOrderFromLRP < RequiredTimeBetweenCommands )
                    return false;
            }

            foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in Me.DeepInfo.shipsByPlanet )
            {
                if ( tracing )
                    tracingBuffer.Add( "\tteam " + Me.FireTeamID + ": " + pair.Key.Name + " --> " + pair.Value.Count + " ships being dispatched\n" );

                FactionUtilityMethods.Instance.Helper_RaidSpecificPlanet( pair.Value, pair.Key, faction, World_AIW2.Instance.CurrentGalaxy, 
                    destination, false, Context, PathCacheData, 0f ); //we checked RequiredTimeBetweenCommands above, so send 0 to the inner method so it always succeeds
            }
            if ( OverridingSpeed > 0 )
            {
                GameCommand speedCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.CreateSpeedGroup_FireteamAttack], GameCommandSource.AnythingElse );
                for ( int j = 0; j < Me.DeepInfo.ShipsInFireteam.Count; j++ )
                    speedCommand.RelatedEntityIDs.Add( Me.DeepInfo.ShipsInFireteam[j].PrimaryKeyID );
                speedCommand.RelatedBool = true;
                speedCommand.RelatedIntegers.Add( OverridingSpeed );
                World_AIW2.Instance.QueueGameCommand( faction, speedCommand, false );
            }

            for ( int i = 0; i < Me.DeepInfo.ShipsInFireteam.Count; i++ )
                Me.DeepInfo.ShipsInFireteam[i].SetHostOnly_TimeWasLastGivenOrderFromLRP( ArcenTime.TimeSinceStartF );

            return true;
        }

        public static bool StageFireteamToLurkPlanet( this Fireteam Me, Faction faction, ArcenLongTermIntermittentPlanningContext Context, 
            PerFactionPathCache PathCacheData, ArcenCharacterBuffer tracingBuffer, float RequiredTimeBetweenCommands )
        {
            if ( Context == null ) //client
                return false;
            //Note if everyone is already en-route then this is a noop
            bool tracing = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge ) && (tracingBuffer != null);
            Me.DeepInfo.BuildShipsLookup( !Me.DeepInfo.IncludeShipsInTransit, Me.LurkPlanet ); //needed to send ships to the lurk planet

            //if any of the ships in the list have been given orders too recently, then don't give orders to any of them right now
            for ( int i = 0; i < Me.DeepInfo.ShipsInFireteam.Count; i++ )
            {
                if ( ArcenTime.TimeSinceStartF - Me.DeepInfo.ShipsInFireteam[i].HostOnly_TimeWasLastGivenOrderFromLRP < RequiredTimeBetweenCommands )
                    return false;
            }

            foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in Me.DeepInfo.shipsByPlanet )
            {
                if ( tracing )
                    tracingBuffer.Add( "\tteam " + Me.FireTeamID + ": " + pair.Key.Name + " --> " + pair.Value.Count + " ships being dispatched\n" );
                if ( pair.Key == Me.LurkPlanet )
                    continue;
                FactionUtilityMethods.Instance.Helper_RaidSpecificPlanet( pair.Value, pair.Key, faction, World_AIW2.Instance.CurrentGalaxy, 
                    Me.LurkPlanet, false, Context, PathCacheData, 0f ); //we checked RequiredTimeBetweenCommands above, so send 0 to the inner method so it always succeeds
            }

            for ( int i = 0; i < Me.DeepInfo.ShipsInFireteam.Count; i++ )
                Me.DeepInfo.ShipsInFireteam[i].SetHostOnly_TimeWasLastGivenOrderFromLRP( ArcenTime.TimeSinceStartF );

            return true;
        }

        public static bool AttackTargetPlanet( this Fireteam Me, Faction faction, ArcenLongTermIntermittentPlanningContext Context, 
            PerFactionPathCache PathCacheData, float RequiredTimeBetweenCommands, int OverridingSpeed = -1 )
        {
            if ( Context == null ) //client
                return false;

            if ( Me.TargetPlanet == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "This fireteam " + Me.FireTeamID + " requested to attack a planet, but have no TargetPlanet.  Faction: " + (faction == null ? "null" : faction.GetDisplayName()), Verbosity.DoNotShow );
                return false;
            }
            Me.DeepInfo.BuildShipsLookup( !Me.DeepInfo.IncludeShipsInTransit, Me.TargetPlanet ); //needed to send ships to the lurk planet

            //if any of the ships in the list have been given orders too recently, then don't give orders to any of them right now
            for ( int i = 0; i < Me.DeepInfo.ShipsInFireteam.Count; i++ )
            {
                if ( ArcenTime.TimeSinceStartF - Me.DeepInfo.ShipsInFireteam[i].HostOnly_TimeWasLastGivenOrderFromLRP < RequiredTimeBetweenCommands )
                    return false;
            }

            foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in Me.DeepInfo.shipsByPlanet )
            {
                if ( pair.Key != Me.TargetPlanet )
                {
                    FactionUtilityMethods.Instance.Helper_RaidSpecificPlanet( pair.Value, pair.Key, faction, World_AIW2.Instance.CurrentGalaxy, 
                        Me.TargetPlanet, false, Context, PathCacheData, 0f ); //we checked RequiredTimeBetweenCommands above, so send 0 to the inner method so it always succeeds
                    if ( OverridingSpeed > 0 )
                    {
                        GameCommand speedCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.CreateSpeedGroup_FireteamAttack], GameCommandSource.AnythingElse );
                        for ( int j = 0; j < Me.DeepInfo.ShipsInFireteam.Count; j++ )
                            speedCommand.RelatedEntityIDs.Add( Me.DeepInfo.ShipsInFireteam[j].PrimaryKeyID );
                        speedCommand.RelatedBool = true;
                        speedCommand.RelatedIntegers.Add( OverridingSpeed );
                        World_AIW2.Instance.QueueGameCommand( faction, speedCommand, false );
                    }
                    continue;
                }
                //We are on the right planet; should we attack a target?
                GameEntity_Squad entity = Me.GetTargetEntityIfValid( faction );
                if ( entity == null || !entity.TypeData.ShipClass.CanBeDamaged || entity.GetHasBeenDestroyed() ||
                    (entity.CountOfEntitiesProvidingExternalInvulnerability > 0 &&
                    entity.CountOfEntitiesProvidingExternalInvulnerability >= entity.TypeData.ExternalInvulnerabilityUnitRequiredCount) )
                    continue;

                GameCommand attackCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.Attack], GameCommandSource.AnythingElse );
                attackCommand.RelatedIntegers4.Add( entity.PrimaryKeyID );
                for ( int j = 0; j < Me.DeepInfo.ShipsInFireteam.Count; j++ )
                {
                    attackCommand.RelatedEntityIDs.Add( Me.DeepInfo.ShipsInFireteam[j].PrimaryKeyID );
                }
                bool playAudioEffectForCommand = false;
                World_AIW2.Instance.QueueGameCommand( faction, attackCommand, playAudioEffectForCommand );
            }

            for ( int i = 0; i < Me.DeepInfo.ShipsInFireteam.Count; i++ )
                Me.DeepInfo.ShipsInFireteam[i].SetHostOnly_TimeWasLastGivenOrderFromLRP( ArcenTime.TimeSinceStartF );

            return true;
        }
    }
}
