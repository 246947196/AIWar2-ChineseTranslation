using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

//Proposed rework
//if the player is found to be deepstriking, start an internal timer. At 60 seconds, notification of "AI Reserves incoming with a countdown".
//Once the player is no longer deepstriking, the ai reserves leave

//AI Reserves mechanics:
//Once every 2 minutes the AI reserves spawn a new wormhole.
//Each wormhole has an income per second, growing over time.
//The AI reserves will spawn units from their wormholes from their income
//AI reserve units will use simple logic; find the nearest planet that's "deepstrike-worthy" and with enemies and go there.

//When the player is no longer deepstriking, all AI reserve units go to their nearest wormhole and vanish, then the wormholes vanish.

//The notification warns that the AI reserves are coming, and that they are active.

//TODO: put this in a journal entry


namespace Arcen.AIW2.External
{   
    public sealed class AIReservesFactionDeepInfo : ExternalFactionDeepInfoRoot, IExternalDeepInfo_Singleton
    {
        public AIReservesFactionBaseInfo BaseInfo;
        public static AIReservesFactionDeepInfo Instance = null;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<AIReservesFactionBaseInfo>();
            Instance = this;
        }

        public readonly DoubleBufferedList<Planet> DeepstrikeEligiblePlanets = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 300, "AIReserves-DeepstrikeEligiblePlanets" );
        public readonly DoubleBufferedList<Planet> EligiblePlanetsUnderAttackByPlayer = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 300, "AIReserves-EligiblePlanetsUnderAttackByPlayer" );

        protected override void Cleanup()
        {
            Instance = null;
            BaseInfo = null;

            DeepstrikeEligiblePlanets.Clear();
            EligiblePlanetsUnderAttackByPlayer.Clear();

            //probably not important
            UnassignedShips.Clear();
            UnassignedShipsByPlanet.Clear();
            WorkingPlanetsPreferred.Clear();
            WorkingPlanetsFallback.Clear();
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 5;       
        
        #region DoBackgroundThreadExpensiveEligibilityCalculations
        private void DoBackgroundThreadExpensiveEligibilityCalculations( ArcenLongTermIntermittentPlanningContext Context )
        {
            bool ReservesDisabled = FactionUtilityMethods.Instance.IsImperialSpireActive();
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                //IsEligibleForDeepStrike will get read by the main thread on the host and in single player.
                //IsEligibleForDeepStrike will also get communicated to the UI of clients in MP within 2-3 seconds at the absolute most, if not faster
                if ( ReservesDisabled )
                {
                    planet.IsEligibleForDeepStrike = false;
                    continue;
                }
                planet.IsEligibleForDeepStrike = CalculateIsPlanetEligibleForDeepStrike_VeryExpensiveDoOnBackgroundThreadOnly( planet, Context );
            }
        }
        #endregion

        #region Helper for Deepstriker AI
        private GameEntity_Squad DoesPlanetHaveDeepstrikerKing( Planet planet )
        {
            GameEntity_Squad king = FactionUtilityMethods.Instance.HasAIKing(planet);
            if ( king != null )
            {
                //this is to support a specific AI type that always has the Reserves available where their king is
                AITypeData kingType = king.PlanetFaction.Faction.TryGetAISentinelsCoreData().SentinelInfo.AIType;
                if ( kingType.OverlordInDeepstrike )
                    return king;
            }
            return null;

        }
        #endregion Helper

        #region CalculateIsPlanetEligibleForDeepStrike_VeryExpensiveDoOnBackgroundThreadOnly
        private bool CalculateIsPlanetEligibleForDeepStrike_VeryExpensiveDoOnBackgroundThreadOnly( Planet planet, ArcenHostOnlySimContext Context )
        {
            #region Tracing
            //bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.AIReserves );
            //ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate("AIReserves-CalculateIsPlanetEligibleForDeepStrike_VeryExpensiveDoOnBackgroundThreadOnly-trace", 10f) : null;
            #endregion
            int debugCode = 0;
            try{
                debugCode = 100;
                if ( planet == null || planet.MarkLevelForAIOnly == null )
                    return false;
                debugCode = 200;
                //Mod Support
                if ( DoesPlanetHaveDeepstrikerKing( planet ) != null )
                    return true; //mod override; this AI type will always have the Reserves available for their King
                //End Mod Support

                if ( planet.MarkLevelForAIOnly.Ordinal <= 1 )
                    return false; //mark 1 planets are never eligible for deepstrike
                debugCode = 300;
                Faction controllingFaction = planet.GetControllingFaction();
                if ( controllingFaction == null )
                    return false;
                debugCode = 400;
                if ( controllingFaction.Type != FactionType.AI ) //if this isn't an AI planet, we don't do deepstrike mitigation
                    return false;
                debugCode = 500;
                int deepstrikeDepth = BaseInfo.MinDepthForDeepstrike;
                debugCode = 600;
                AITypeData aiType = controllingFaction.TryGetAISentinelsCoreData().SentinelInfo.AIType;
                debugCode = 700;
                deepstrikeDepth += aiType.DeepstrikeDistanceModifier;
                debugCode = 800;
                Int16 hopsToHumanPlanet = GetHopsToPlayerPlanet_DeepstrikeRules_ExtremelyExpensiveBackgroundThreadOnly( planet, Context, deepstrikeDepth );
                debugCode = 900;
                if ( hopsToHumanPlanet < deepstrikeDepth )
                {
                    return false;
                }
                return true;
            } catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Exception in CalculateIsPlanetEligibleForDeepStrike_VeryExpensiveDoOnBackgroundThreadOnly debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            return false;
        }
        #endregion

        #region GetHopsToPlayerPlanet_DeepstrikeRules_ExtremelyExpensiveBackgroundThreadOnly
        private Int16 GetHopsToPlayerPlanet_DeepstrikeRules_ExtremelyExpensiveBackgroundThreadOnly( Planet planet, ArcenHostOnlySimContext Context, int StopIfLessThanThisNumberOfHops )
        {
            int gracePeriod = BaseInfo.GetEffectiveGracePeriod();
            int gracePeriodAgoInGameTime = World_AIW2.Instance.GameSecond - gracePeriod;

            bool debug = false;
            Int16 shortestDistance = 999;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Checking " + planet.Name, Verbosity.DoNotShow );
            foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops( -1,
                            delegate ( Planet otherPlanet )
                            {
                                //it's a human planet if... it's a human planet
                                bool isValidPlayerPlanet = otherPlanet.GetControllingFactionType() == FactionType.Player;
                                //it's a human planet for our purposes here if it was controlled by the humans within the last twenty minutes
                                if ( !isValidPlayerPlanet )
                                    isValidPlayerPlanet = (otherPlanet.TimeLastControlledByHumans > 0 && otherPlanet.TimeLastControlledByHumans > gracePeriodAgoInGameTime);
                                //oh wait -- regardless of human ownership status, if the AI still has any major structures here, it doesn't count as a human planet
                                if ( isValidPlayerPlanet && otherPlanet.NumberOfMajorAIStructuresCurrentlyHere > 0 )
                                    isValidPlayerPlanet = false;

                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "\t prop for " + otherPlanet.Name + " controller " + otherPlanet.GetControllingFactionType(), Verbosity.DoNotShow );
                                if ( isValidPlayerPlanet ) //once we've hit a player planet, don't evaluate further in this direction
                                    return PropogationEvaluation.SelfButNotNeighbors;

                                return PropogationEvaluation.Yes;
                            } ) )
            {
                Planet otherPlanet = _phd.Planet;
                Int16 distance = _phd.Hops;
                //it's a human planet if... it's a human planet
                bool isValidPlayerPlanet = otherPlanet.GetControllingFactionType() == FactionType.Player;
                if ( otherPlanet.TimeLastControlledByHumans == -1 )
                    isValidPlayerPlanet = true;
                //it's a human planet for our purposes here if it was controlled by the humans within the last twenty minutes
                if ( !isValidPlayerPlanet )
                    isValidPlayerPlanet = (otherPlanet.TimeLastControlledByHumans > 0 && (gracePeriod <= 0 || otherPlanet.TimeLastControlledByHumans > gracePeriodAgoInGameTime ));
                //oh wait -- regardless of human ownership status, if the AI still has any major structures here, it doesn't count as a human planet
                if ( isValidPlayerPlanet && otherPlanet.NumberOfMajorAIStructuresCurrentlyHere > 0 )
                    isValidPlayerPlanet = false;
                if ( !isValidPlayerPlanet )
                {
                    isValidPlayerPlanet = otherPlanet.GetControllingOrInfluencingFaction().Type == FactionType.Player;
                }
                if ( isValidPlayerPlanet && debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( otherPlanet.Name + " counts as a valid player planet", Verbosity.DoNotShow );
                //mark 1 planets never count as deepstrike; the AI doesn't care about them that much. This is needed for Ark Empire as well

                if ( isValidPlayerPlanet && distance < shortestDistance )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Found " + otherPlanet.Name + " at a new shortest distance, " + distance, Verbosity.DoNotShow );
                    shortestDistance = distance;
                    if ( shortestDistance < StopIfLessThanThisNumberOfHops )
                        break; //we found the answer we need
                }
            }
            return shortestDistance;
        }
        #endregion

        #region UpdateDeepstrikeEligiblePlanetsList_CallFromLRPOnly
        private void UpdateDeepstrikeEligiblePlanetsList_CallFromLRPOnly()
        {
            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.AIReserves );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AIReserves-UpdateDeepstrikeEligiblePlanetsList_CallFromLRPOnly-trace", 10f ) : null;
            bool debug = false;
            #endregion

            //TEACHING_MOMENT: Hey, check this out!  Double buffered items can be produced on any thread... but what can background threads safely feed info to?
            //We _could_ have had the DeepstrikeEligiblePlanets be on BaseInfo, and use it from the UI... except wait a second, that would
            //be a terrible idea because clients don't call this DeepInfo code.
            //DeepInfo code can absolutely update lists or variables on BaseInfo, but those need to be serialized or else the client
            //won't hear anything about it.  In this case, the IsEligibleForDeepStrike variable on planets IS serialized, and is used by the UI.
            //That's a single bit, which is very efficient to tack on.  Adding a list of DeepstrikeEligiblePlanets on BaseInfo would be redundant info
            //as well as taking more bandwidth.  So we just calculate it here, use it here, and leave the result for any thread.
            //
            //If you ever are wondering why a given list or variable is in either class, that's the reason: it's meant to be saved or seen by clients and the UI, or it's not.
            //There are plenty of working lists on the BaseInfo classes where they are not saved, but those should ALL be calculated in BaseInfo, not from DeepInfo (or else they're blank for clients).
            //In the event that something doesn't seem like it belongs on BaseInfo based on those rules, then it should be moved to DeepInfo.  If some UI code then complains, you have a problem.
            //It's time at that point to either make something serialize, or to decide that something is okay being host-only (like debug data often is).
            DeepstrikeEligiblePlanets.ClearConstructionListForStartingConstruction();
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( !planet.IsEligibleForDeepStrike )
                {
                    continue;
                }
                DeepstrikeEligiblePlanets.AddToConstructionList( planet );
                Faction controllingFaction = planet.GetControllingFaction();
                if ( tracing && debug && planet.GetPlanetFactionForFaction( controllingFaction ).DataByStance[FactionStance.Hostile].TotalStrength > 0 )
                    tracingBuffer.Add( planet.Name + " is eligible for the reserves. Total strength not in transports: " + planet.GetPlanetFactionForFaction( controllingFaction ).DataByStance[FactionStance.Hostile].TotalPlayerStrengthNotInTransports + " total strength " + planet.GetPlanetFactionForFaction( controllingFaction ).DataByStance[FactionStance.Hostile].TotalStrength ).Add("\n");
            }

            DeepstrikeEligiblePlanets.SwitchConstructionToDisplay();
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        #endregion

        #region UpdateDeepstrikePlanetsUnderAttack_CallFromLRPOnly
        private void UpdateDeepstrikePlanetsUnderAttack_CallFromLRPOnly()
        {
            int debugStage = 0;
            try
            {
                debugStage = 100;
                bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.AIReserves );
                debugStage = 200;
                ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AIReserves-UpdateDeepstrikePlanetsUnderAttack_CallFromLRPOnly-trace", 10f ) : null;
                bool debug = false;
                debugStage = 400;
                List<Planet> listToCheck = DeepstrikeEligiblePlanets.GetDisplayList();
                debugStage = 500;
                EligiblePlanetsUnderAttackByPlayer.ClearConstructionListForStartingConstruction();
                BaseInfo.PlanetsTriggeringAttackHostOnly.Clear();
                debugStage = 900;
                //bool ignoreCrippled = true;
                debugStage = 1000;
                for ( int i = 0; i < listToCheck.Count; i++ )
                {
                    //For each deepstrike eligible planet, check whether the reserves should go there
                    debugStage = 1100;
                    Planet planet = listToCheck[i];
                    debugStage = 1200;
                    Faction aiFaction = planet.GetControllingFaction();
                    
                    {
                        //Mod support

                        GameEntity_Squad deepstrikerKingOrNull = DoesPlanetHaveDeepstrikerKing( planet );
                        if ( deepstrikerKingOrNull != null )
                            aiFaction = deepstrikerKingOrNull.PlanetFaction.Faction;
                    }

                    debugStage = 1300;
                    if ( tracing && debug )
                        tracingBuffer.Add( "checking if " + planet.Name + " is under attack\n" );
                    debugStage = 1400;
                    PlanetFaction aiPFaction = planet.GetPlanetFactionForFaction( aiFaction );
                    if ( aiPFaction.DataByStance[FactionStance.Hostile].MobileStrength == 0 )
                    {
                        if ( tracing && debug )
                            tracingBuffer.Add( "\tThis planet does not count; there is no mobile strength\n" );
                        continue; //don't trigger off of immobile structures (like turrets or necromancer guard posts)
                    }

                    bool isUnderAttack = aiPFaction.DataByStance[FactionStance.Hostile].TotalPlayerStrengthNotInTransports > BaseInfo.MinStrengthToTrigger;
                    if ( isUnderAttack )
                    {
                        //this is the most primary form of alert, so put us on increased alert for the next 10 minutes or so
                        planet.WillBeOnExtraDeepstrikeAlertUntilGameSecond = World_AIW2.Instance.GameSecond + BaseInfo.TimeRemainsOnAlert;
                    }
                    else //we were not on the most primary form alert, so now check for extended alerts
                    {
                        //the first extended alert is all of the player strength on this planet, including those in transports
                        int totalEnemyStrength = aiPFaction.DataByStance[FactionStance.Hostile].TotalPlayerStrength;
                        isUnderAttack = totalEnemyStrength > BaseInfo.MinStrengthToTrigger;

                        if ( !isUnderAttack )
                        {
                            //if that doesn't work, then the second extended check is to also check adjacent planets for player strength in or out of transports
                            //you can't get away so easily!
                            foreach ( Planet adjacent in planet.LinkedNeighbors( false ) )
                            {
                                if ( planet == adjacent ) //don't double-count ourselves!
                                    continue;
                                
                                PlanetFaction adjacentPFaction = adjacent.GetPlanetFactionForFaction( aiFaction );
                                totalEnemyStrength += aiPFaction.DataByStance[FactionStance.Hostile].TotalPlayerStrength;
                                isUnderAttack = totalEnemyStrength > BaseInfo.MinStrengthToTrigger;
                                if ( isUnderAttack )
                                    break;
                            }
                        }
                    }

                    if ( isUnderAttack )
                    {
                        debugStage = 1500;
                        if ( tracing )
                            tracingBuffer.Add( planet.Name + " is under attack!\n" );
                        debugStage = 2000;
                        BaseInfo.PlanetsTriggeringAttackHostOnly.Add( planet );
                        EligiblePlanetsUnderAttackByPlayer.AddToConstructionList( planet );
                    }
                } //end for

                EligiblePlanetsUnderAttackByPlayer.SwitchConstructionToDisplay();
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in AI UpdateDeepstrikePlanetsUnderAttack debugStage " + debugStage + " --> " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        #endregion

        public static readonly List<SafeSquadWrapper> UnassignedShips = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "AIReservesFactionDeepInfo-UnassignedShips" );
        public static readonly DictionaryOfLists<Planet, SafeSquadWrapper> UnassignedShipsByPlanet = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 300, "AIReservesFactionDeepInfo-UnassignedShipsByPlanet" );
        //private bool CheckForOutOfDateOrders = false;
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            this.DoBackgroundThreadExpensiveEligibilityCalculations( Context );

            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.CPA );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AIReserves-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;

            int debugCode = 0;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                debugCode = 100;
                UnassignedShips.Clear();
                UnassignedShipsByPlanet.Clear();

                //TEACHING_MOMENT: hey, this used to be called from both the Stage3 (on the main sim thread) AND from the background thread.
                //This is some code (these next two methods) that can be VERY slow, so that's a horrible idea.  But in the past, we had no choice.
                //Now we can just run this on the LRP thread, and both the simulation thread and the LRP thread can use the results because they are
                //double-buffered lists.  This can save notable CPU time on a thread that impacts sim speed (the simulation thread), while it still
                //gets to use the results.
                //
                //Quick quiz, though: Who can't use the results?
                //Answer: the UI, or any of the code in BaseInfo.  So it's getting the nice IsEligibleForDeepStrike serialized variable that it can work from.
                //The simulation thread, the main/ui thread, and the LRP thread are three different threads, and they can all benefit from the work of each other.
                //You just have to do that... with a bit of care
                UpdateDeepstrikeEligiblePlanetsList_CallFromLRPOnly();
                UpdateDeepstrikePlanetsUnderAttack_CallFromLRPOnly();

                if ( !this.BaseInfo.ReservesActive )
                    return; //we're inactive
                //if ( !this.BaseInfo.AbsorbShipsMode )
                //    CheckForOutOfDateOrders = false;
                //Iterate over all our units to figure out if any need orders
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads() )
                {
                    if ( entity == null )
                        continue;
                    debugCode = 102;
                    if ( entity.PlanetFaction == null ) //I've seen this be null sometimes on game load
                        continue;
                    if ( entity.HasQueuedOrders() ) //entity is doing something already (either attacking a target or en route somewhere)
                    {
                        if ( this.BaseInfo.DropExistingOrders )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "Dropping existing orders\n" );
                            //if this is the first LRP after we started absorbing, give these ships new orders

                            if ( entity.Orders.GetFinalDestinationOrNull() != null )
                            {
                                UnassignedShips.Add( entity );
                                UnassignedShipsByPlanet[entity.Planet].Add( entity );
                            }
                        }
                        continue;
                    }
                    var factionData = entity.Planet.GetStanceDataForFaction( this.AttachedFaction );
                    int friendlyStrength = factionData[FactionStance.Self].TotalStrength + factionData[FactionStance.Friendly].TotalStrength;
                    if ( entity.Planet.GetControllingOrInfluencingFaction().GetIsHostileTowards( this.AttachedFaction ) ||
                         factionData[FactionStance.Hostile].TotalStrength > friendlyStrength / 10 )
                    {
                        continue; //fight enemies here
                    }
                    //these ships aren't on planets with things they should stop and fight
                    UnassignedShips.Add( entity );
                    UnassignedShipsByPlanet[entity.Planet].Add( entity );
                }
                this.BaseInfo.DropExistingOrders = false;

                if ( EligiblePlanetsUnderAttackByPlayer.Count == 0 && !this.BaseInfo.AbsorbShipsMode )
                {
                    return;
                }
                foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in UnassignedShipsByPlanet )
                {
                     Planet startPlanet = pair.Key;
                     if ( this.BaseInfo.AbsorbShipsMode )
                     {
                         //Each ship without orders finds the nearest wormhole and heads there
                         if ( tracing )
                             tracingBuffer.Add( "Ships on " + startPlanet.Name + " are heading back to wormholes\n" );
                         SendShipsToNearestWormhole( startPlanet, pair.Value, this.AttachedFaction, Context, pathingCacheData );
                         continue;
                     }
                     //Else send our ships to attack

                     List<Planet> tempPlanets = Planet.GetTemporaryPlanetList( "AIRes-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-tempPlanets", 10f );
                     if ( tempPlanets == null ) //blocked for teardown/shutdown; bail
                         return;

                     EligiblePlanetsUnderAttackByPlayer.SortThreadsafeCopyOfDisplayList( tempPlanets, delegate ( Planet L, Planet R )
                        {
                            int lhops = startPlanet.GetHopsTo( L );
                            int rhops = startPlanet.GetHopsTo( R );
                            return lhops.CompareTo( rhops );
                        } );
                     Planet targetPlanet = null;
                     for ( int i = 0; i < tempPlanets.Count; i++ )
                     {
                         if ( Context.RandomToUse.Next( 0, 100 ) < 80 )
                             targetPlanet = tempPlanets[i];
                     }
                     if ( targetPlanet == null )
                         targetPlanet = tempPlanets[0];
                     if ( tracing )
                         tracingBuffer.Add( "Ships on " + startPlanet.Name + " are off to attack " + targetPlanet.Name + "\n" );

                     Planet.ReleaseTemporaryPlanetList( tempPlanets );

                     AttackTargetPlanet( startPlanet, targetPlanet, pair.Value, this.AttachedFaction, Context, pathingCacheData );
                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in AIReserves LRP. debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();

                if ( tracing )
                {
                    #region Tracing
                    if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( "AIReserves LRP " + AttachedFaction.FactionIndex + ". " + tracingBuffer.ToString(), Verbosity.DoNotShow );
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                }
            }
        }
        public void SendShipsToNearestWormhole(Planet start, List<SafeSquadWrapper> ships, Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        { 
            if ( ships.Count <= 0 || BaseInfo.Wormholes.Count == 0 )
                return;

            List<SafeSquadWrapper> tempSquads = GameEntity_Squad.GetTemporarySquadList( "AIRes-SendShipsToNearestWormhole-tempSquads", 10f );
            if ( tempSquads == null ) //blocked for teardown/shutdown; bail
                return;

            BaseInfo.Wormholes.SortThreadsafeCopyOfDisplayList( tempSquads, delegate ( SafeSquadWrapper L, SafeSquadWrapper R )
            {
                int lhops = start.GetHopsTo(L.Planet);
                int rhops = start.GetHopsTo(R.Planet);
                return lhops.CompareTo( rhops );
            } );
            GameEntity_Squad wormhole = tempSquads[0].GetSquad();
            if ( wormhole == null )
                return;
            GameEntity_Squad.ReleaseTemporarySquadList( tempSquads );

            if ( wormhole.Planet == start )
            {
                //fly to wormhole
                GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCNearestWormhole], GameCommandSource.AnythingElse );
                moveCommand.PlanetOrderWasIssuedFrom = wormhole.Planet.Index;
                moveCommand.RelatedPoints.Add( wormhole.WorldLocation );
                for ( int i = 0; i < ships.Count; i++ )
                    moveCommand.RelatedEntityIDs.Add( ships[i].PrimaryKeyID );
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );
            }
            else
            {
                AttackTargetPlanet(start, wormhole.Planet, ships, faction, Context, PathCacheData );
            }
        } 
        public void AttackTargetPlanet(Planet start, Planet destination, List<SafeSquadWrapper> ships, Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( ships.Count <= 0 )
                return;

            PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "AttackTargetPlanet", start, destination, PathingMode.Default, Context, PathCacheData );
            if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
            {
                GameCommand command = GameCommand.Create ( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_UtilRaidSpecific], GameCommandSource.AnythingElse );
                command.RelatedString = "AIReserves_Planetary_Movement";
                for ( int k = 0; k < ships.Count; k++ )
                    command.RelatedEntityIDs.Add( ships[k].PrimaryKeyID );
                for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                    command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
            }
        }

        public System.Diagnostics.Stopwatch timeTesterStopwatch = null;// new System.Diagnostics.Stopwatch();

        private readonly Dictionary<GameEntityTypeData, int> waveComposition = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed( 30, "AIReservesFactionDeepInfo-waveComposition" );

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            if ( timeTesterStopwatch != null )
            {
                timeTesterStopwatch.Reset();
                timeTesterStopwatch.Start();
            }

            AIDifficulty highestDifficulty = FactionUtilityMethods.Instance.GetHighestAIDifficulty_AsDifficulty();
            int WormholeSpawnInterval = highestDifficulty.AIReservesWormholeSpawnInterval; //how often will new wormholes spawn
            short WormholeDespawnTime = highestDifficulty.AIReservesWormholeDespawnTime; //after the player has retreated, the wormholes will persist for "A while" to prevent players from just dancing with the reserves
            int InitialWormholeSpawnInterval = highestDifficulty.AIReservesInitialWormholeSpawnDelay; //initial timer for the first wormhole
            int IncomeIncreaseInterval = highestDifficulty.AIReservesIncomeIncreaseInterval; //how often wormhole income increases (this is a major balance lever for the strength of the reserves

            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.AIReserves );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AIReserves-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f ) : null;
            #endregion
            int debugStage = 0;
            try
            {
                debugStage = 1000;
                
                //bool localDebug = false;
                //Check if the Reserves are active
                if ( EligiblePlanetsUnderAttackByPlayer.Count == 0 && 
                     BaseInfo.Wormholes.Count == 0 &&
                     !this.BaseInfo.DespawnOldRegimeShips )
                {
                    this.BaseInfo.AbsorbShipsMode = false;
                    if ( this.BaseInfo.ReservesActive ) //we've just changed state, so fast blast this
                        World_AIW2.Instance.OnServer_FactionsToFastBlastToClients.Enqueue( this.AttachedFaction );
                    
                    this.BaseInfo.ReservesActive = false;
                    this.BaseInfo.TimeReservesActivated = -1;
                    this.BaseInfo.TimePlayerIncursionStarted = -1;
                    this.BaseInfo.TimeForNextWormhole = -1;
                    
                    if ( tracing && World_AIW2.Instance.GameSecond % 20 == 0 )
                    {
                        tracingBuffer.Add( "Reserves are idle" );
                        tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " PerSim trace ends" );
                        ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    
                    if ( timeTesterStopwatch != null )
                    {
                        timeTesterStopwatch.Stop();
                        ArcenDebugging.ArcenDebugLogSingleLine( "AIReserves were not active, and total ms: " +
                            timeTesterStopwatch.ElapsedMilliseconds, Verbosity.DoNotShow );
                    }
                    
                    return; //the reserves are idle
                }

                debugStage = 2000;
                
                if ( EligiblePlanetsUnderAttackByPlayer.Count > 0 && 
                     this.BaseInfo.AbsorbShipsMode )
                {
                    if ( this.BaseInfo.AbsorbShipsMode) //to keep notifications up to date
                        World_AIW2.Instance.OnServer_FactionsToFastBlastToClients.Enqueue( this.AttachedFaction );

                    this.BaseInfo.AbsorbShipsMode = false;
                }
                
                debugStage = 3000;
                
                if ( EligiblePlanetsUnderAttackByPlayer.Count == 0 &&
                     (BaseInfo.Wormholes.Count > 0 || 
                      this.BaseInfo.ReservesActive || 
                      this.BaseInfo.DespawnOldRegimeShips) )
                {
                    if ( tracing )
                        tracingBuffer.Add( "AI Reserves pulling out\n" );
                    if ( !this.BaseInfo.AbsorbShipsMode )
                        this.BaseInfo.DropExistingOrders = true;
                    if ( !this.BaseInfo.AbsorbShipsMode ) //to keep notifications up to date for the clients
                        World_AIW2.Instance.OnServer_FactionsToFastBlastToClients.Enqueue( this.AttachedFaction );
                    
                    this.BaseInfo.AbsorbShipsMode = true;
                    
                    debugStage = 4000;
                    
                    int shipsRemaining = AbsorbShipsIfPossible( AttachedFaction, Context );
                    if ( tracing && World_AIW2.Instance.GameSecond % 20 == 0 )
                        tracingBuffer.Add( "After attempting to absorb ships there are " + shipsRemaining + " ships left" );

                    if ( shipsRemaining == 0 )
                    {
                        if ( this.BaseInfo.DespawnOldRegimeShips )
                        {
                            this.BaseInfo.DespawnOldRegimeShips = false;
                            return; //we've cleaned up all the old ships
                        }
                        if ( tracing && World_AIW2.Instance.GameSecond % 20 == 0 )
                            tracingBuffer.Add( "No ships left; wormholes to despawn soon" );

                        List<SafeSquadWrapper> wormholes = BaseInfo.Wormholes.GetDisplayList();
                        for ( int i = 0; i < wormholes.Count; i++ )
                        {
                            GameEntity_Squad ship = wormholes[i].GetSquad();
                            if ( ship == null )
                                continue;
                            if ( ship.DespawnsInXSeconds <= 0 )
                            {
                                ship.DespawnsInXSeconds = WormholeDespawnTime;
                            }
                        }
                    }

                    debugStage = 5000;
                    
                    if ( tracing && !tracingBuffer.GetIsEmpty() )
                    {
                        tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " PerSim trace ends" );
                        ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    if ( timeTesterStopwatch != null )
                    {
                        timeTesterStopwatch.Stop();
                        ArcenDebugging.ArcenDebugLogSingleLine( "AIReserves were in absorb mode, and total ms: " +
                            timeTesterStopwatch.ElapsedMilliseconds, Verbosity.DoNotShow );
                    }
                    
                    return;
                }
                
                debugStage = 6000;
                
                //
                // At this point, we know the player is deepstriking
                //
                
                if ( tracing && World_AIW2.Instance.GameSecond % 10 == 0 )
                {
                    tracingBuffer.Add( "A player is currently deepstriking against\n" );
                    List<Planet> planets = EligiblePlanetsUnderAttackByPlayer.GetDisplayList();
                    for ( int i = 0; i < planets.Count; i++ )
                        tracingBuffer.Add( "\t" + planets[i].Name );
                    tracingBuffer.Add( "\n" );
                }

                debugStage = 7000;
                
                if ( !this.BaseInfo.ReservesActive )
                {
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Base_Lore_Reserves", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );

                    int gameSecondMostRecentlyControlledByHumans = 0;
                    List<Planet> planets = EligiblePlanetsUnderAttackByPlayer.GetDisplayList();
                    foreach ( Planet planet in planets )
                    {
                        if ( planet.TimeLastControlledByHumans > gameSecondMostRecentlyControlledByHumans )
                            gameSecondMostRecentlyControlledByHumans = planet.TimeLastControlledByHumans;
                    }

                    debugStage = 8000;
                    
                    //the player has just begun to deepstrike
                    if ( !this.BaseInfo.ReservesActive ) //we've just changed state, so fast blast this
                        World_AIW2.Instance.OnServer_FactionsToFastBlastToClients.Enqueue( this.AttachedFaction );
                    this.BaseInfo.ReservesActive = true;

                    //if the player has owned one of the planets within the last 10 minutes, then skip any warning: surprise attack!
                    if ( gameSecondMostRecentlyControlledByHumans > 0 &&
                        World_AIW2.Instance.GameSecond - 600 > gameSecondMostRecentlyControlledByHumans )
                    {
                        debugStage = 9000;
                        
                        this.BaseInfo.TimePlayerIncursionStarted = World_AIW2.Instance.GameSecond;
                        this.BaseInfo.TimeForNextWormhole = World_AIW2.Instance.GameSecond;
                        if ( tracing )
                            tracingBuffer.Add( "Player incursion detected at " + World_AIW2.Instance.GameSecond + " next wormhole spawns now for surprise attack." );
                    }
                    else
                    {
                        this.BaseInfo.TimePlayerIncursionStarted = World_AIW2.Instance.GameSecond;
                        this.BaseInfo.TimeForNextWormhole = World_AIW2.Instance.GameSecond + InitialWormholeSpawnInterval;
                        if ( tracing )
                            tracingBuffer.Add( "Player incursion detected at " + World_AIW2.Instance.GameSecond + " next wormhole spawns at " + this.BaseInfo.TimeForNextWormhole );
                    }
                }
                
                debugStage = 10000;
                
                if ( this.BaseInfo.TimeForNextWormhole <= World_AIW2.Instance.GameSecond )
                {
                    if ( this.BaseInfo.TimeReservesActivated == -1 )
                        this.BaseInfo.TimeReservesActivated = World_AIW2.Instance.GameSecond;
                    //create wormhole
                    if ( tracing )
                        tracingBuffer.Add( "Attempting to create a wormhole\n" );
                    CreateWormhole( AttachedFaction, Context );
                    this.BaseInfo.TimeForNextWormhole = World_AIW2.Instance.GameSecond + WormholeSpawnInterval;
                }

                debugStage = 11000;
                
                //Process the wormholes, updating income and building/absorbing ships
                List<SafeSquadWrapper> wormholesToCheck = BaseInfo.Wormholes.GetDisplayList();
                for ( int i = 0; i < wormholesToCheck.Count; i++ )
                {
                    debugStage = 11000;
                    
                    GameEntity_Squad entity = wormholesToCheck[i].GetSquad();
                    if ( entity == null )
                        continue;
                    if ( entity.DespawnsInXSeconds > 0 && !this.BaseInfo.AbsorbShipsMode )
                        entity.DespawnsInXSeconds = 0;
                    
                    debugStage = 11000;
                    
                    //increase budget per second if necessary
                    AIReservesPerUnitBaseInfo data = entity.GetExternalBaseInfoAs<AIReservesPerUnitBaseInfo>();
                    if ( (World_AIW2.Instance.GameSecond - data.TimeWormholeSpawned) % IncomeIncreaseInterval == 0 )
                    {
                        debugStage = 11000;
                        
                        int increaseAmount = 0;
                        Faction aiFaction = World_AIW2.GetRandomAIFaction( Context );
                        AISentinelsFactionDeepInfo aiDeepInfo = aiFaction.GetAISentinelsDeepLogic();
                        increaseAmount = aiDeepInfo.BaseInfo.GetSpecificBudgetThreshold( AIBudgetType.Wave, GlobalAIWorldBaseInfo.Instance.AIProgress_Effective ) / 100;
                        if ( increaseAmount < 1 )
                            increaseAmount = 1;
                        data.CurrentIncomePerSecond += increaseAmount;
                    }
                    
                    debugStage = 11000;
                    
                    //update wormhole budget
                    int percentUnique = 20;
                    int percentGeneral = 80;
                    data.StoredMetalGeneral += data.CurrentIncomePerSecond * (100 / percentGeneral);
                    data.StoredMetalUniqueShips += data.CurrentIncomePerSecond * (100 / percentUnique);
                    //spend budget
                    if ( tracing )
                        tracingBuffer.Add( entity.ToStringWithPlanet() + " current income " + data.CurrentIncomePerSecond + " stored metal " + data.StoredMetalGeneral + " (" + data.StoredMetalUniqueShips + ") time for next ships spawn " + data.TimeToSpawnShipsNext );

                    debugStage = 11000;
                    
                    if ( data.TimeToSpawnShipsNext == -1 || 
                         data.TimeToSpawnShipsNext <= World_AIW2.Instance.GameSecond )
                    {
                        debugStage = 11000;
                        
                        data.TimeToSpawnShipsNext = data.TimeToSpawnShipsNext + Context.RandomToUse.Next( 5, 45 );
                        
                        int amountSpent = 0;
                        int numShipTypes = 5;
                        int numGuardianTypes = 2;

                        debugStage = 11000;
                        
                        PlannedWaveOptions options = PlannedWaveOptions.CreateWithDefaults();
                        options.allowGuardians = true;
                        
                        debugStage = 11000;
                        
                        WavesHelper.Instance.GetWaveComposition( waveComposition, entity.Planet.GetControllingFaction(), Context, data.StoredMetalGeneral, out amountSpent, 
                            numShipTypes, numGuardianTypes, options, entity.Planet, tracing, tracingBuffer );
                        
                        debugStage = 11000;
                        
                        options.ReturnToPool(); //prevents these from leaking

                        debugStage = 11000;
                        
                        data.StoredMetalGeneral -= amountSpent;
                        
                        debugStage = 11000;
                        
                        // added into that composition, are any 'unique' reserves ships that have a separate budget here
                        int num_scary = 0;
                        while ( data.StoredMetalUniqueShips > 0 )
                        {
                            string tag = "AIReservesRegular";
                            
                            if (num_scary == 0 &&
                                Context.RandomToUse.Next( 0, 100 ) >= 90)
                            {
                                tag = "AIReservesScary";

                                // don't spawn more than a single one of these at a time
                                // from this specific wormhole, from this single spawn event
                                num_scary++;
                            }
                            
                            var uniqueType = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, tag );
                            if (uniqueType == null)
                            {
                                data.StoredMetalUniqueShips = 0;
                                break;
                            }

                            data.StoredMetalUniqueShips -= uniqueType.CostForAIToPurchase;
                            
                            waveComposition[uniqueType] += 1;
                        }
                        
                        // actually spawn everything
                        WavesHelper.Instance.DeployComposition( Context, AttachedFaction, null, -1, waveComposition, null, entity.WorldLocation, entity.Planet, tracing, false );
                    }
                }

                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    //people have been reporting periodic weirdnesses with AI reserves allegiances, so make absolutely sure it's correct
                    //with regards to player and AI
                    if ( otherFaction.Type == FactionType.Player && !AttachedFaction.GetIsHostileTowards( otherFaction ) )
                    {
                        AttachedFaction.MakeHostileTo( otherFaction );
                        otherFaction.MakeHostileTo( AttachedFaction );
                    }
                    if ( otherFaction.Type == FactionType.AI && !AttachedFaction.GetIsFriendlyTowards( otherFaction ) )
                    {
                        AttachedFaction.MakeFriendlyTo( otherFaction );
                        otherFaction.MakeFriendlyTo( AttachedFaction );
                    }
                }
                debugStage = 8000;

                if ( timeTesterStopwatch != null )
                {
                    timeTesterStopwatch.Stop();
                    ArcenDebugging.ArcenDebugLogSingleLine( "AIReserves were in fully active, and total ms: " +
                        timeTesterStopwatch.ElapsedMilliseconds, Verbosity.DoNotShow );
                }
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in AI Reserves debugStage " + debugStage + " --> " + e.ToString(), Verbosity.ShowAsError );
            }
            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() )
            {
                tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " PerSim trace ends" );
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            }
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
        }

        private int AbsorbShipsIfPossible(Faction faction, ArcenHostOnlySimContext Context )
        {
            var wormholes = this.BaseInfo.Wormholes.GetDisplayList();
            
            //returns the number of ships still out.
            //Once "inactive", all AI Reserves ship begin to attrition
            //just in case they can't get home reasonably
            int shipsLeft = 0;
            foreach ( GameEntity_Squad e in faction.Squads() )
            {
                bool processedWormhole = false;
                if ( e.TypeData.GetHasTag("AIReservesSpawnPoint") )
                    continue;

                for ( int i = 0; i < wormholes.Count; i++ )
                {
                    var wormhole = wormholes[i].GetSquad();
                    if ( wormhole == null )
                        continue;

                    if ( wormhole.Planet != e.Planet )
                        continue;

                    if (Mat.DistanceBetweenPointsImprecise( e.WorldLocation, wormhole.WorldLocation ) > 500)
                        continue;

                    e.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut);

                    processedWormhole = true;
                    break;
                }
                if ( processedWormhole )
                    continue;

                if (e.TransformsIntoAfterTime != "$Dies_Paused")
                {
                    e.TransformsIntoAfterTime = "$Dies_Paused";
                    e.SecondsTillTransformation = 100;
                    //e.FlagAsNeedingFullSyncCheckIfInMultiplayerAndWeAreHost();
                }

                e.SecondsTillTransformation--;

                if ( BaseInfo.Wormholes.Count == 0 ||
                     this.BaseInfo.DespawnOldRegimeShips ||
                     e.IsBlackHoledAtMoment.Display ||
                     e.CurrentCountOfTractorsPullingOnThis > 0 )
                {
                    //if we can't get back to a wormhole or we are tractored (or these are old AI Reserves, pre-wormholes),
                    // die faster
                    e.SecondsTillTransformation--;
                    //e.FlagAsNeedingFullSyncCheckIfInMultiplayerAndWeAreHost();
                }

                if (e.SecondsTillTransformation <= 0)
                {
                    e.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut);
                    continue;
                }

                shipsLeft++;
            }
            
            return shipsLeft;
        }

        private void CreateWormhole( Faction faction, ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.AIReserves );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AIReserves-CreateWormhole-trace", 10f ) : null;

            GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "AIReservesSpawnPoint" );
            Planet planetToSpawn = GetPlanetForAIReserves( faction, Context );
            if ( planetToSpawn == null )
            {
                if ( tracing )
                {
                    tracingBuffer.Add( "no eligible planets" );
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return; //no eligible planets
            }
            if ( tracing )
                tracingBuffer.Add("Spawning a new wormhole on " + planetToSpawn.Name);
            ArcenPoint spawnLocation = planetToSpawn.GetSafePlacementPointAroundPlanetCenter(Context, typeData, FInt.FromParts( 0, 650 ), FInt.FromParts( 0, 950 ) );
            PlanetFaction pFaction = planetToSpawn.GetPlanetFactionForFaction(faction);
            GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typeData, 1,
                                                                     faction.LooseFleet, 0, spawnLocation, Context, "AIResrvesCreateWormhole" );  //is fine, main sim thread
            if ( newEntity != null )
            {
                AIReservesPerUnitBaseInfo data = newEntity.CreateExternalBaseInfo<AIReservesPerUnitBaseInfo>( "AIReservesPerUnitBaseInfo" );

                //the initial income is set based on an AI faction's wave income
                Faction aiFaction = World_AIW2.GetRandomAIFaction( Context );
                AISentinelsFactionBaseInfo aiSentinelsInfo = aiFaction.GetAISentinelsCoreData();
                data.CurrentIncomePerSecond = aiSentinelsInfo.GetSpecificBudgetThreshold( AIBudgetType.Wave, GlobalAIWorldBaseInfo.Instance.AIProgress_Effective ) / 100;
                if ( data.CurrentIncomePerSecond < 10 )
                    data.CurrentIncomePerSecond = 10;

                data.StoredMetalGeneral = 0;
                data.StoredMetalUniqueShips = 0;
                data.TimeWormholeSpawned = World_AIW2.Instance.GameSecond;
                data.UnitsToAttackPlayerPlanets = false;
            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }

        private static readonly List<Planet> WorkingPlanetsPreferred = List<Planet>.Create_WillNeverBeGCed( 500, "AIReservesFactionDeepInfo-WorkingPlanetsPreferred" );
        private static readonly List<Planet> WorkingPlanetsFallback = List<Planet>.Create_WillNeverBeGCed( 500, "AIReservesFactionDeepInfo-WorkingPlanetsFallback" );
        private Planet GetPlanetForAIReserves(Faction faction, ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.AIReserves );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AIReserves-GetPlanetForAIReserves-trace", 10f ) : null;

            WorkingPlanetsPreferred.Clear();
            WorkingPlanetsFallback.Clear();
            List<Planet> planetsUnderAttack = this.EligiblePlanetsUnderAttackByPlayer.GetDisplayList();
            List<SafeSquadWrapper> wormholes = this.BaseInfo.Wormholes.GetDisplayList();
            for ( int i = 0; i < planetsUnderAttack.Count; i++ )
            {
                Planet attackedPlanet = planetsUnderAttack[i];
                if ( tracing )
                    tracingBuffer.Add( "Considering a wormhole near " + attackedPlanet.Name );
                foreach ( Planet.PlanetAtHopDistance _phd in attackedPlanet.PlanetsWithinXHops( 4,
                               delegate ( Planet otherPlanet )
                               {
                                   if ( otherPlanet.GetControllingFactionType() != FactionType.AI )
                                       return PropogationEvaluation.No;
                                   if ( otherPlanet.GetHopsTo( attackedPlanet ) > 5 && (WorkingPlanetsPreferred.Count > 0 || WorkingPlanetsFallback.Count > 0) )
                                       return PropogationEvaluation.No;
                                   return PropogationEvaluation.Yes;
                               } ) )
                {
                    Planet otherPlanet = _phd.Planet;
                    if ( otherPlanet.GetControllingFactionType() != FactionType.AI )
                        continue;
                    PlanetFaction pFaction = otherPlanet.GetPlanetFactionForFaction( faction );
                    if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength > 5000 )
                        continue;

                    if ( tracing )
                        tracingBuffer.Add( "\tAdding " + otherPlanet.Name + " as a wormhole option\n" );
                    bool foundAsWormhole = false;
                    for ( int j = 0; j < wormholes.Count; j++ )
                    {
                        if ( wormholes[j].Planet == otherPlanet )
                        {
                            WorkingPlanetsFallback.Add( otherPlanet );
                            foundAsWormhole = true;
                            break;
                        }
                    }
                    if ( foundAsWormhole )
                        continue;
                    if ( !otherPlanet.IsEligibleForDeepStrike )
                    {
                        WorkingPlanetsFallback.Add( otherPlanet );
                        continue;
                    }
                    WorkingPlanetsPreferred.Add( otherPlanet );
                    if ( WorkingPlanetsPreferred.Count > 6 )
                        break;
                }
            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            if ( WorkingPlanetsPreferred.Count == 0 && WorkingPlanetsFallback.Count == 0)
                return null;
            if ( WorkingPlanetsPreferred.Count > 0 )
                return WorkingPlanetsPreferred[ Context.RandomToUse.Next(0, WorkingPlanetsPreferred.Count) ];
            if ( WorkingPlanetsFallback.Count > 0 )
                return WorkingPlanetsFallback[ Context.RandomToUse.Next(0, WorkingPlanetsFallback.Count) ];
            return null;
        }
    }
}
