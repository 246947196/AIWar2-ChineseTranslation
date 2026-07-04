using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    /// <summary>
    //Handles the logic for a CPA; when the AI creates CPA units they are donated to the corresponding CPA faction. Each AI has a unique
    //CPA faction so they can have appropriate colours when a CPA.

    //When a CPA has units that aren't on enemy planets, it takes all of its units on a given planet and picks a weighted random target planet to go to (since there are usually a lot of Planets, we hit a lot of targets).
    //When a CPA has units on an enemy planet without a target, they pick a specific random valuable structure on a planet and attack it. Since CPA ships will trickle in, this means they will distribute well across the targets in a system.
    /// </summary>

    public sealed class AICrossPlanetAttackerDeepInfo : ExternalFactionDeepInfoRoot
    {
        public AICrossPlanetAttackerBaseInfo BaseInfo;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<AICrossPlanetAttackerBaseInfo>();
        }

        protected override void Cleanup()
        {
            BaseInfo = null;

            UnassignedShips.Clear();
            UnassignedShipsByPlanet.Clear();
            InCombatShipsNeedingOrders.Clear();
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 5;
        public static readonly List<SafeSquadWrapper> UnassignedShips = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "AICrossPlanetAttackerDeepInfo-UnassignedShips" );
        public static readonly DictionaryOfLists<Planet, SafeSquadWrapper> UnassignedShipsByPlanet = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 500, "AICrossPlanetAttackerDeepInfo-UnassignedShipsByPlanet" );
        public static readonly DictionaryOfLists<Planet, SafeSquadWrapper> InCombatShipsNeedingOrders = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 500, "AICrossPlanetAttackerDeepInfo-InCombatShipsNeedingOrders" );
        //Set immediately before the target sorts so the comparisons can be non-capturing static
        //delegates (no per-call closure allocation).  [ThreadStatic] because this runs on a
        //background non-sim thread.
        [ThreadStatic] private static Faction cb_apaAttachedFaction;
        [ThreadStatic] private static Planet cb_apaStartPlanet;
        [ThreadStatic] private static int cb_apaHighestDifficulty;

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.CPA );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AICPA-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            int debugCode = 0;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                int highestDifficulty = FactionUtilityMethods.Instance.GetHighestAIDifficulty();
                debugCode = 100;
                UnassignedShips.Clear();
                UnassignedShipsByPlanet.Clear();
                InCombatShipsNeedingOrders.Clear();
                int totalShips = 0;
                int totalStrength = 0;
                //Iterate over all our units to figure out if any need orders
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    if ( entity == null )
                        continue;
                    debugCode = 102;
                    totalShips++;
                    totalStrength += entity.GetStrengthOfSelfAndContents();
                    if ( entity.HasQueuedOrders() )
                    {
                        //entity is doing something already (either attacking a target or en route somewhere)
                        Planet eventualDestinationOrNull = entity.Orders.GetFinalDestinationOrNull();
                        if ( eventualDestinationOrNull == null )
                            continue; //we are going somewhere on this planet, so keep the same orders
                        var factionData = eventualDestinationOrNull.GetStanceDataForFaction( AttachedFaction );
                        if ( factionData[FactionStance.Hostile].TotalStrength > 0 ||
                             eventualDestinationOrNull.GetControllingFaction().GetIsHostileTowards( AttachedFaction ) )
                            continue; //if the planet we are going has enemies, keep going there
                    }

                    debugCode = 105;

                    if ( entity.Planet.GetControllingOrInfluencingFaction().GetIsHostileTowards( AttachedFaction ) )
                    {
                        //This entity is on an enemy planet but doesn't have orders to attack a specific valuable target
                        //This means we will always kill a command stations before moving on
                        debugCode = 110;
                        InCombatShipsNeedingOrders[entity.Planet].Add( entity );

                        continue;
                    }
                    //This unit doesn't have any active orders and isn't on an enemy planet. Find an enemy planet
                    debugCode = 120;
                    UnassignedShips.Add( entity );
                    UnassignedShipsByPlanet[entity.Planet].Add( entity );
                }
                debugCode = 130;
                if ( UnassignedShips.Count == 0 && World_AIW2.Instance.GameSecond % 60 == 0 )
                {
                    debugCode = 130;
                    if ( tracing )
                        tracingBuffer.Add( "No ships in CPA.\n" );
                    return;
                }
                debugCode = 150;
                if ( tracing && totalShips > 0 )
                    tracingBuffer.Add( totalShips + " with strength " + (totalStrength / 1000) + " in the CPA right now.\n" );

                //First, handle the ships not in combat
                //Here's the rule. First we pick our preferred planets (player or civil war AI planets), then we sort them
                //We prefer close and weak player planets.

                List<Planet> preferredTargets = Planet.GetTemporaryPlanetList( "AICPA-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-preferredTargets", 10f );
                if ( preferredTargets == null ) //blocked for teardown/shutdown; bail
                    return;
                //ie minor faction are minor faction targets
                List<Planet> fallbackTargets = Planet.GetTemporaryPlanetList( "AICPA-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-fallbackTargets", 10f );
                if ( fallbackTargets == null ) //blocked for teardown/shutdown; bail
                {
                    Planet.ReleaseTemporaryPlanetList( preferredTargets );
                    return;
                }

                foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in UnassignedShipsByPlanet )
                {
                     debugCode = 200;
                     Planet startPlanet = pair.Key;
                     if ( tracing )
                         tracingBuffer.Add( pair.Value.Count + " ships on " + startPlanet.Name + " are looking for a target\n" );
                     preferredTargets.Clear();
                     fallbackTargets.Clear();
                     debugCode = 210;
                     foreach ( Planet.PlanetAtHopDistance _phd in startPlanet.PlanetsWithinXHops( -1, delegate ( Planet secondaryPlanet )
                     {
                         debugCode = 230;
                         //don't path through hostile planets.
                         if ( secondaryPlanet.GetControllingOrInfluencingFaction().GetIsHostileTowards( AttachedFaction ) )
                             return PropogationEvaluation.SelfButNotNeighbors;
                         return PropogationEvaluation.Yes;
                     } ) )
                     {
                         Planet planet = _phd.Planet;
                         debugCode = 220;
                         if ( IsPreferredTarget( planet, AttachedFaction, Context ) )
                         {
                             preferredTargets.Add( planet );
                             continue;
                         }

                         if ( IsFallbackTarget( planet, AttachedFaction, Context ) )
                             fallbackTargets.Add( planet );
                     }
                     debugCode = 240;
                     if ( tracing )
                     {
                         tracingBuffer.Add( "\tWe have " + preferredTargets.Count + " preferred targets\n" );
                         for ( int i = 0; i < preferredTargets.Count; i++ )
                             tracingBuffer.Add( "\t\t" + preferredTargets[i].Name ).Add( "\n" );
                         if ( fallbackTargets.Count > 0 )
                         {
                             tracingBuffer.Add( "\tWe have " + fallbackTargets.Count + " fallback targets\n" );
                             for ( int i = 0; i < fallbackTargets.Count; i++ )
                                 tracingBuffer.Add( "\t\t" + fallbackTargets[i].Name ).Add( "\n" );
                         }
                     }
                     debugCode = 250;
                     //choose the target. First try to get a preferred planet
                     Planet target = null;
                     if ( preferredTargets.Count > 0 )
                     {
                         debugCode = 260;
                         if ( tracing )
                             tracingBuffer.Add( "\tAttempting to pick a preferredTarget\n" );
                         cb_apaAttachedFaction = AttachedFaction;
                         cb_apaStartPlanet = startPlanet;
                         cb_apaHighestDifficulty = highestDifficulty;
                         debugCode = 270;
                         preferredTargets.Sort( static delegate ( Planet L, Planet R )
                         {
                             //To sort the planets, we factor how scary a planet is and how far it is away. We prefer nearer and weaker targets
                             //TODO if desired: also say "if this has an AIP increaser, want to attack it a bit more"
                             //that would make it a tad more evil
                             var lFactionData = L.GetStanceDataForFaction( cb_apaAttachedFaction );
                             var rFactionData = R.GetStanceDataForFaction( cb_apaAttachedFaction );
                             int lhops = cb_apaStartPlanet.GetHopsTo( L );
                             int rhops = cb_apaStartPlanet.GetHopsTo( R );
                             int lEnemyStrength = lFactionData[FactionStance.Hostile].TotalStrength;
                             int rEnemyStrength = rFactionData[FactionStance.Hostile].TotalStrength;
                             int lFriendlyStrength = lFactionData[FactionStance.Friendly].TotalStrength +
                                 lFactionData[FactionStance.Self].TotalStrength;
                             int rFriendlyStrength = rFactionData[FactionStance.Friendly].TotalStrength +
                                 rFactionData[FactionStance.Self].TotalStrength;

                             int lstrength = lEnemyStrength - lFriendlyStrength;
                             int rstrength = rEnemyStrength - rFriendlyStrength;
                             FInt factorPerHop = FInt.FromParts( 5, 000 );
                             if ( cb_apaHighestDifficulty > 7 )
                                 factorPerHop = FInt.One; //on higher difficulties, the AI prefers to focus exclusively on weaker targets
                             int lVal = (lhops * factorPerHop).IntValue * lstrength;
                             int rVal = (rhops * factorPerHop).IntValue * rstrength;

                             return lVal.CompareTo( rVal );
                         } );
                         debugCode = 280;
                         for ( int i = 0; i < preferredTargets.Count; i++ )
                         {
                             int percentToUse = 50;
                             if ( highestDifficulty > 7 )
                                 percentToUse = 80; //more focus on higher difficulties
                             if ( Context.RandomToUse.Next( 0, 100 ) < percentToUse )
                             {
                                 //weighted choice
                                 target = preferredTargets[i];
                                 if ( target != null )
                                     break;
                             }
                         }
                         if ( target == null ) //if we didn't pick one randomly, just take the best
                             target = preferredTargets[0];
                     }
                     debugCode = 300;
                     if ( target == null && fallbackTargets.Count > 0 )
                     {
                         debugCode = 310;
                         //this is very similar to the preferredTargets code above
                         if ( tracing )
                             tracingBuffer.Add( "\tAttempting to pick a fallbackTarget\n" );
                         cb_apaAttachedFaction = AttachedFaction;
                         cb_apaStartPlanet = startPlanet;
                         fallbackTargets.Sort( static delegate ( Planet L, Planet R )
                         {
                             var lFactionData = L.GetStanceDataForFaction( cb_apaAttachedFaction );
                             var rFactionData = R.GetStanceDataForFaction( cb_apaAttachedFaction );
                             int lhops = cb_apaStartPlanet.GetHopsTo( L );
                             int rhops = cb_apaStartPlanet.GetHopsTo( R );
                             int lEnemyStrength = lFactionData[FactionStance.Hostile].TotalStrength;
                             int rEnemyStrength = rFactionData[FactionStance.Hostile].TotalStrength;
                             int lFriendlyStrength = lFactionData[FactionStance.Friendly].TotalStrength +
                                 lFactionData[FactionStance.Self].TotalStrength;
                             int rFriendlyStrength = rFactionData[FactionStance.Friendly].TotalStrength +
                                 rFactionData[FactionStance.Self].TotalStrength;

                             int lstrength = lEnemyStrength - lFriendlyStrength;
                             int rstrength = rEnemyStrength - rFriendlyStrength;
                             FInt factorPerHop = FInt.FromParts( 0, 500 );

                             int lVal = (lhops / factorPerHop).IntValue * lstrength;
                             int rVal = (rhops / factorPerHop).IntValue * rstrength;

                             return lVal.CompareTo( rVal );
                         } );
                         for ( int i = 0; i < fallbackTargets.Count; i++ )
                         {
                             if ( Context.RandomToUse.Next( 0, 100 ) < 50 )
                             {
                                 target = fallbackTargets[i];
                                 if ( target != null )
                                    break;
                             }
                         }
                         if ( target == null ) //if we didn't pick one randomly, just take the best
                             target = fallbackTargets[0];
                     }
                     debugCode = 400;
                     if ( tracing )
                         tracingBuffer.Add( "\tSending " + pair.Value.Count + " ships from " + startPlanet.Name + " to attack " + target.Name ).Add( "\n" );
                     debugCode = 410;
                     AttackTargetPlanet( startPlanet, target, pair.Value, AttachedFaction, Context, pathingCacheData, tracing, tracingBuffer );
                }

                Planet.ReleaseTemporaryPlanetList( preferredTargets );
                Planet.ReleaseTemporaryPlanetList( fallbackTargets );

                debugCode = 500;
                //This is a ship on a planet controlled by our enemies. Pick a target and go after it.
                List<SafeSquadWrapper> targetSquads = GameEntity_Squad.GetTemporarySquadList( "AICPA-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-targetSquads", 10f );
                if ( targetSquads == null ) //blocked for teardown/shutdown; bail
                    return;

                foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in InCombatShipsNeedingOrders )
                {
                     debugCode = 600;
                     PlanetFaction pFaction = pair.Key.GetControllingPlanetFaction();
                     targetSquads.Clear();
                     if ( tracing )
                         tracingBuffer.Add( "Finding a target for " + pair.Value.Count + " ships on " + pair.Key.Name ).Add( "\n" );

                     //note that this approach duplicates entries in targets, but that's good; if something both creates energy and
                     //grants aip on death then it's good enough that we want to prioritize it over just something that creates energy

                     foreach ( GameEntity_Squad entity in pFaction.Entities.Squads( EntityRollupType.KingUnitsOnly ) )
                     {
                          targetSquads.Add( entity );
                      }

                     foreach ( GameEntity_Squad entity in pFaction.Entities.Squads( EntityRollupType.CommandStation ) )
                     {
                         if ( entity.SecondsSpentAsRemains > 0 )
                             continue;

                         targetSquads.Add( entity );
                     }

                     foreach ( GameEntity_Squad entity in pFaction.Entities.Squads( EntityRollupType.EnergyProducers ) )
                     {
                         targetSquads.Add( entity );
                     }
                     foreach ( GameEntity_Squad entity in pFaction.Entities.Squads( EntityRollupType.AIPOnDeath ) )
                     {
                         targetSquads.Add( entity );
                     }
                     if ( targetSquads.Count == 0 )
                     {
                         if ( tracing )
                             tracingBuffer.Add( "No targets on " + pair.Key.Name + "\n" );
                         continue;
                     }
                     //pick a random target. The intent is that we should call this whenever new units arrive,
                     //and since units are just streaming in we should call it a lot and should get a good split for units
                     debugCode = 610;
                     int targetIndex = Context.RandomToUse.Next( 0, targetSquads.Count );
                     GameEntity_Squad target = targetSquads[targetIndex].GetSquad();
                     while ( target == null )
                     {
                         targetSquads.RemoveAt( targetIndex );
                         if ( targetSquads.Count <= 0 )
                         {
                             if ( tracing )
                                 tracingBuffer.Add( "All targets died on " + pair.Key.Name + "\n" );
                             goto next_pair;
                         }
                         targetIndex = Context.RandomToUse.Next( 0, targetSquads.Count );
                         target = targetSquads[targetIndex].GetSquad();
                     }
                     if ( tracing )
                     {
                         tracingBuffer.Add( "We are attacking " + target.ToStringWithPlanet() + ", one of " + targetSquads.Count + " targets.\n" );
                         bool debug = false;
                         if ( tracing && debug )
                         {
                             for ( int i = 0; i < targetSquads.Count; i++ )
                             {
                                 tracingBuffer.Add( i + ": " + targetSquads[i].ToStringWithPlanet() + ".\n" );
                             }
                         }
                     }

                     GameCommand attackCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.Attack], GameCommandSource.AnythingElse );
                     attackCommand.RelatedIntegers4.Add( target.PrimaryKeyID );
                     for ( int j = 0; j < pair.Value.Count; j++ )
                     {
                         attackCommand.RelatedEntityIDs.Add( pair.Value[j].PrimaryKeyID );
                     }

                     World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, attackCommand, false );
                     next_pair:;
                }

                GameEntity_Squad.ReleaseTemporarySquadList( targetSquads );
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in CPALogic LRP. debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();

                if ( tracing )
                {
                    #region Tracing
                    if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( "CPALogic " + AttachedFaction.FactionIndex + ". " + tracingBuffer.ToString(), Verbosity.DoNotShow );
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                }
            }
        }

        public bool IsPreferredTarget(Planet planet, Faction faction, ArcenLongTermIntermittentPlanningContext Context)
        {
            //Whether this is an enemy controlled planet. Note that we try not to overkill planets too badly
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.CPA );
            bool debug = false;
            ArcenCharacterBuffer tracingBuffer = tracing && debug ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AICPA-IsPreferredTarget-trace", 10f ) : null;
            if ( tracing && debug )
                tracingBuffer.Add("\tchecking " + planet.Name + " for preferredness\n");
            Faction controllingFaction = planet.GetControllingFaction();
            if ( controllingFaction.Type != FactionType.Player ||
                 (faction.InCivilWarMode && controllingFaction.Type != FactionType.AI) )
            {
                if ( tracing && debug )
                    tracingBuffer.Add("\t\tNot owned by primary enemy (ie player or AI on civil war). Owned by " + controllingFaction.GetDisplayName() +"\n");
                return false;
            }

            if ( !controllingFaction.GetIsHostileTowards(faction) )
            {
                if ( tracing && debug )
                    tracingBuffer.Add("\t\t" + controllingFaction.GetDisplayName() + " is friendly\n");
                return false;
            }
            var factionData = planet.GetStanceDataForFaction( faction );
            int enemyStrength = factionData[FactionStance.Hostile].TotalStrength;
            int friendlyStrength = factionData[FactionStance.Friendly].TotalStrength + factionData[FactionStance.Self].TotalStrength ;
            if ( enemyStrength * 5 < friendlyStrength )
            {
                if ( tracing && debug )
                    tracingBuffer.Add("\t\t" + enemyStrength + " < " + (friendlyStrength * 5)+ ", so discard\n");

                return false; //if we outnumber 7 to 1, don't bother attacking
            }
            return true;
        }
        public bool IsFallbackTarget(Planet planet, Faction faction, ArcenLongTermIntermittentPlanningContext Context)
        {
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.CPA );
            Faction controllingFaction = planet.GetControllingOrInfluencingFaction();

            if ( !controllingFaction.GetIsHostileTowards(faction) )
                return false;

            var factionData = planet.GetStanceDataForFaction( faction );
            int enemyStrength = factionData[FactionStance.Hostile].TotalStrength;
            int friendlyStrength = factionData[FactionStance.Friendly].TotalStrength + factionData[FactionStance.Self].TotalStrength ;
            if ( enemyStrength < friendlyStrength * 7 )
            {
                return false; //if we outnumber 7 to 1, don't bother attacking
            }

            return true;
        }
        public int CalculateSpeed(List<SafeSquadWrapper> ships, ArcenLongTermIntermittentPlanningContext Context)
        {
            //Return the speed we want these ships to use. It's "a bit faster than the average speed, and at least 500".
            //We make the speeds all a little bit different to make it less obvious to the player that we are using speed groups (since
            //ships from multiple planets will be going by at the same time, and moving at different speeds)
            if ( ships.Count == 0 )
                return 0;
            int maxSpeed = 0;
            Int64 totalSpeed = 0;
            GameEntity_Squad ship;
            int effectiveShipsCount = 0;
            for ( int i = 0; i < ships.Count; i++ )
            {
                ship = ships[i].GetSquad();
                if ( ship == null )
                    continue;
                GameEntityTypeData.MarkLevelStats dataForMark = ship.DataForMark;
                if ( dataForMark == null )
                    continue;
                effectiveShipsCount++;
                int newSpeed = dataForMark.Speed;
                totalSpeed += newSpeed;
                if ( maxSpeed < newSpeed )
                    maxSpeed = newSpeed;

            }
            int average = effectiveShipsCount <= 0 ? 300 : (Int32)( totalSpeed / (Int64)effectiveShipsCount);

            average += average / 9; //a bit faster than average
            if ( average < 500 )
                average = 500;
            if ( BaseInfo.TimeForCPAToAttack < World_AIW2.Instance.GameSecond )
                average += 200; //get into position quickly
            average += Context.RandomToUse.Next(0, 50); //a little more randomness

            //if we only have one ship type, then they just go the speed the go, for example
            if ( average > maxSpeed )
                average = maxSpeed;

            return average;
        }
        public void AttackTargetPlanet(Planet start, Planet destination, List<SafeSquadWrapper> ships, Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, bool tracing, ArcenCharacterBuffer tracingBuffer )
        {
            if ( ships.Count <= 0 )
                return;
            if ( BaseInfo == null )
                return; //Sim code hasn't run yet
            int debugCode = 0;
            try{
                debugCode = 100;
                if ( !BaseInfo.CPATimeInitialized )
                {
                    // if ( tracing )
                    // {
                    //     ArcenDebugging.ArcenDebugLogSingleLine("Skipping LRP since we haven't run sim to determing when to fully strike", Verbosity.DoNotShow );
                    // }
                    return;
                }
                debugCode = 110;
                PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "AICPAAttackTargetPlanet", start, destination, PathingMode.Safest, Context, PathCacheData );
                int gatheringDistance = -1;
                debugCode = 200;
                if ( BaseInfo.TimeForCPAToAttack > World_AIW2.Instance.GameSecond )
                {
                    gatheringDistance = 2;
                    if ( tracing )
                        tracingBuffer.Add("\tHolding back our strike a bit; gatheringDistance " + gatheringDistance +"\n");
                }
                debugCode = 300;
                if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                {
                    debugCode = 400;
                    if ( gatheringDistance > -1 && pathCache.PathToReadOnly.Count <= gatheringDistance && BaseInfo.TimeForCPAToAttack > World_AIW2.Instance.GameSecond )
                    {
                        if ( tracing ) {
                            tracingBuffer.Add("\tWe are only going " + pathCache.PathToReadOnly.Count + " hops, so no movement commands issued\n");
                        }
                        return; //no attacking. we're note ready yet
                    }
                    if ( tracing )
                        tracingBuffer.Add("\tpath count " + pathCache.PathToReadOnly.Count + " and gatheringDistance " + gatheringDistance );
                    debugCode = 500;
                    if ( pathCache.PathToReadOnly.Count > gatheringDistance || gatheringDistance == -1)
                    {
                        debugCode = 600;
                        //if we are coming from a long ways away, fly a little faster so the CPA is less spread out
                        int speedToUse = CalculateSpeed(ships, Context);
                        GameCommand speedCommand = GameCommand.Create ( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.CreateSpeedGroup_FireteamAttack],  GameCommandSource.AnythingElse);
                        for (int j = 0; j < ships.Count; j++)
                            speedCommand.RelatedEntityIDs.Add(ships[j].PrimaryKeyID);
                        int exoGroupSpeed = speedToUse;
                        speedCommand.RelatedBool = true;
                        speedCommand.RelatedIntegers.Add(exoGroupSpeed);
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, speedCommand, false );
                    }
                    debugCode = 700;
                    GameCommand command = GameCommand.Create ( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_UtilRaidSpecific], GameCommandSource.AnythingElse );
                    command.RelatedString = "CPA_Planetary_Movement";
                    for ( int k = 0; k < ships.Count; k++ )
                        command.RelatedEntityIDs.Add( ships[k].PrimaryKeyID );
                    //determine whether we are actually going to the target, or just getting close so we are ready to strike
                    debugCode = 800;
                    if ( gatheringDistance == -1 )
                    {
                        debugCode = 900;
                        if ( tracing )
                            tracingBuffer.Add("\tJust attack please\n");

                        for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                            command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                    }
                    else
                    {
                        debugCode = 1000;
                        int hopsForwardToMove = pathCache.PathToReadOnly.Count - gatheringDistance;
                        if ( hopsForwardToMove >= pathCache.PathToReadOnly.Count || hopsForwardToMove < 0 )
                            throw new Exception("huh?");
                        if ( tracing )
                            tracingBuffer.Add("\tMove forward only " + hopsForwardToMove + " hops from " + start.Name + " -> " + pathCache.PathToReadOnly[hopsForwardToMove - 1].Name + " en route to " + destination.Name + "\n");
                        debugCode = 1100;
                        for ( int k = 0; k < hopsForwardToMove; k++ )
                            command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                    }

                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                }
            } catch(Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in CPA AttackTargetPlanet debugCode " + debugCode + " " + e.ToString() , Verbosity.ShowAsError );
            }
        }        
    }
}
