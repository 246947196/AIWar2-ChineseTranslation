using Arcen.AIW2.Core;
using System;


using System.Text;
using Arcen.Universal;

/*
  Overall Structure: The Fireteam is the basic unit; they contain a list of ships (calculated each LRP based on a fireteamID set on the entity)
  and a bunch of information about what they should be doing.

  The FireteamRegiment is an organization of Fireteams that all have the same target; this allows for coordination between fireteams

  The FireteamUtility class is called by non-Scourge factions that want to use Fireteams; for those factions they need to
  build Fireteam List (by iterating over all units in LRP and then adding them to the Fireteams in the List),
  then implement a number of faction-specific things like "How do I pick a target" and "How do I decide to retreat"? as other function.
  Then they call the a FireteamUtility method to update their fireteams and build the FireteamRegiments, then they
  call another FireteamUtility method to have the fireteams decide to attack or retreat

  The Scourge have their own version of the FireteamUtility methods, since they have a bunch of extra stuff they are doing.
   */

namespace Arcen.AIW2.External
{
    public static class FireteamUtility
    {
        //Set immediately before the sorts so the comparisons can be non-capturing static delegates.
        //[ThreadStatic] because fireteam planning runs on background threads.
        [ThreadStatic] private static Faction cb_fuFaction;
        [ThreadStatic] private static Planet cb_fuTargetCurrentPlanet;
        [ThreadStatic] private static FInt cb_fuTargetFalloff;

        //Loop-invariant ratio constants used by UpdateRegiments; hoisted out of the per-planet loop so we
        //don't rebuild them on every iteration.
        private static readonly FInt advantageRatioRequiredForAttackOther = FInt.FromParts( 1, 250 );
        private static readonly FInt advantageRatioRequiredForAttackPlayer = FInt.FromParts( 1, 750 );
        private static readonly FInt advantageRatioRequiredForAttackPlayerJoiningIn = FInt.FromParts( 1, 400 );
        private static readonly FInt outnumberRatioRequiredForRetreatNormal = FInt.FromParts( 0, 750 );
        private static readonly FInt outnumberRatioRequiredForRetreatPlayer = FInt.FromParts( 0, 800 );

        //Deathball cap (B): we allow a regiment to gather up to (win ratio * this margin) times the enemy strength
        //on a target, then redirect surplus fireteams to other targets to open more fronts. The win ratios reused
        //here are the same advantageRatioRequiredForAttack* values the actual attack decision uses.
        private static readonly FInt DeathballOvercommitMargin = FInt.FromParts( 2, 000 );

        //Composition matchup (E) is always computed (for tracing/UI/future use), but only feeds into fireteam
        //decisions when DLC4 is installed and enabled. Same cached lookup the DLC4 LRP/UI code uses.
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
        public static void DisbandFireteam( Faction faction, ArcenLessLinkedList<Fireteam> Teams, Fireteam team, ArcenLongTermIntermittentPlanningContext Context )
        {
            team.Disband( faction, Context );
            Teams.Remove( team );
        }
        public static void DisbandAndRetreatFireteam( ArcenLessLinkedList<Fireteam> Teams, Fireteam team, GameEntity_Squad retreatPoint, Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            team.DeepInfo.DisbandAndRetreat( faction, Context, PathCacheData, retreatPoint );
            Teams.Remove( team );
        }

        public static void CleanUpDisbandedFireteams( ArcenLessLinkedList<Fireteam> Teams )
        {
            //See the Scourge for a more complex implementation of fireteams.
            //Iterating LiveTeamsIn prunes any null/Disbanded teams in place (returning them to the pool) as it walks — that pruning is the entire job here.
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( Teams ) ) { }
        }

        //for generic fireteam implementations (like marauders, etc), lets do all the long range planning sort of logic here
        //The Scourge needs its own implementation because it's more complex
        public static void UpdateFireteams( Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData,
            ArcenLessLinkedList<Fireteam> Teams, ProtectedValDictionary<Planet, FireteamRegiment> TeamsAimedAtPlanet, 
            ArcenCharacterBuffer tracingBuffer, FInt attackingStrengthMultiplier, List<Planet> PlanetsToDefendInput = null, List<SafeSquadWrapper> ShipsThatNeedEscorting = null )
        {
            //this is called after a given faction has iterated over all its relevant units and assigned units to Fireteams
            //It covers updating Fireteam state and then the Fireteam Regiment code
            int debugCode = 0;
            bool tracing = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam ) && tracingBuffer != null;
            bool trackPerformanceTicks = GameSettings.Current.GetBoolBySetting( "PerformanceTicksDebug" );
            if ( trackPerformanceTicks )
            {
                faction.Fireteamstopwatch.Start();
            }

            debugCode = 100;
            Dictionary<FireteamStatus, int> totalStrengthPerState = null;
            List<Fireteam> teamsThatNeedTargets = null;
            List<Fireteam> escortingFireteams = null;
            List<Planet> planetsToDefend = null;
            List<Planet> friendlyPlanetsUnderAttack = null;
            
            try
            {               
                debugCode = 110;
                totalStrengthPerState = Fireteam.GetTemporaryFireteamStatusDictOfInts( "FireteamUtil-UpdateFireteams-totalStrengthPerState", 10f );
                if ( totalStrengthPerState == null ) //blocked for teardown/shutdown; bail
                    return;
                debugCode = 111;
                teamsThatNeedTargets = Fireteam.GetTemporaryFireteamList( "FireteamUtil-UpdateFireteams-teamsThatNeedTargets", 10f );
                if ( teamsThatNeedTargets == null ) //blocked for teardown/shutdown; bail
                    return;
                debugCode = 112;
                escortingFireteams = Fireteam.GetTemporaryFireteamList( "FireteamUtil-UpdateFireteams-escortingFireteams", 10f );
                if ( escortingFireteams == null ) //blocked for teardown/shutdown; bail
                    return;
                debugCode = 113;
                planetsToDefend = Planet.GetTemporaryPlanetList( "FireteamUtil-UpdateFireteams-planetsToDefend", 10f );
                if ( planetsToDefend == null ) //blocked for teardown/shutdown; bail
                    return;
                debugCode = 114;
                friendlyPlanetsUnderAttack = Planet.GetTemporaryPlanetList( "FireteamUtil-UpdateFireteams-friendlyPlanetsUnderAttack", 10f );
                if ( friendlyPlanetsUnderAttack == null ) //blocked for teardown/shutdown; bail
                    return;

                debugCode = 120;
                
                //See the Scourge for a more complex implementation of fireteams
                CleanUpDisbandedFireteams( Teams );

                if ( PlanetsToDefendInput != null && PlanetsToDefendInput.Count > 0 )
                {
                    debugCode = 200;
                    //remove duplicate copies from the input list and put them into the regular list
                    //from now on, we only use planetsToDefend
                    planetsToDefend.Add( PlanetsToDefendInput[0] );
                    for ( int i = 1; i < PlanetsToDefendInput.Count; i++ )
                    {
                        debugCode = 210;
                        Planet planetToCheck = PlanetsToDefendInput[i];
                        if ( !planetsToDefend.Contains( planetToCheck ) )
                             planetsToDefend.Add( planetToCheck );
                    }
                }
                debugCode = 230;
                if ( planetsToDefend != null && planetsToDefend.Count > 0 )
                {
                    debugCode = 240;
                    //The Faction tells us which planets need to be defended (by which we mean, "try to leave a defense mode fireteam lurking on them")
                    foreach ( Fireteam team in Fireteam.LiveTeamsIn( Teams ) )
                    {
                        debugCode = 250;
                        if ( !team.DefenseMode )
                            continue;
                        if ( team.LurkPlanet == null )
                            continue;

                        if ( planetsToDefend.Contains( team.LurkPlanet ) )
                            planetsToDefend.Remove( team.LurkPlanet );
                    }
                    debugCode = 260;
                    if ( planetsToDefend.Count > 0 )
                    {
                        debugCode = 270;
                        //Now we have a list of planets that the faction would like defended, and that don't have
                        //a fireteam already there.
                        //Sort by "how weak is our strength on this planet". Ideally we'd also prefer planets next to adjacent enemies, but I think that will be too slow
                        //I think we might keep some of that info too
                        cb_fuFaction = faction;
                        planetsToDefend.Sort( static delegate ( Planet Left, Planet Right )
                        {
                            var leftData = Left.GetStanceDataForFaction( cb_fuFaction );
                            var rightData = Right.GetStanceDataForFaction( cb_fuFaction );
                            int lStrength = leftData[FactionStance.Self].TotalStrength + leftData[FactionStance.Friendly].TotalStrength;
                            int rStrength = rightData[FactionStance.Self].TotalStrength + rightData[FactionStance.Friendly].TotalStrength;
                            return lStrength.CompareTo( rStrength );
                        } );
                    }
                }

                debugCode = 990;
                System.Diagnostics.Stopwatch overallStopwatch = null;
                System.Diagnostics.Stopwatch incrementalStopwatch = null;
                bool trackTimings = tracing; //timings is its own variable so we can enable only timing code
                long targetAndLurkTime = 0;
                if ( trackTimings )
                {
                    overallStopwatch = new System.Diagnostics.Stopwatch();
                    incrementalStopwatch = new System.Diagnostics.Stopwatch();
                }

                if ( trackTimings )
                    overallStopwatch.Start();
                escortingFireteams.Clear();
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( Teams ) )
                {
                    if ( team.status == FireteamStatus.Escorting )
                        escortingFireteams.Add( team );
                }
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( Teams ) )
                {
                    debugCode = 1000;

                    team.DeepInfo.UpdateNonSerializedFields_LRP( faction, tracingBuffer, Context ); //we pass in global data for some debug loggin
                    if ( tracing )
                    {
                        tracingBuffer.Add( "Checking fireteam " );
                        team.GetDebugString(tracingBuffer);
                        tracingBuffer.NewLine();
                        totalStrengthPerState[team.status] += team.DeepInfo.TeamStrength;
                    }
                    if ( team.DeepInfo.ShipsInFireteam.Count == 0 ) //all our ships are dead :-(. We probably got ambushed or something
                    {
                        debugCode = 1050;
                        if ( tracing )
                            tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is dead" + "\n" );
                        team.Disband( faction, Context );
                        continue;
                    }

                    if ( team.DeepInfo.FireteamShouldDisband )
                    {
                        debugCode = 1051;
                        if ( tracing )
                            tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is should disband, something is wrong\n" );
                        team.Disband( faction, Context );
                        continue;
                    }
                    if ( team.DefenseMode && team.StepsUntilBecomesOffensive > 0 )
                    {
                        //Defensive Fleets are allowed to become offensive
                        if ( --team.StepsUntilBecomesOffensive <= 0 )
                        {
                            team.DefenseMode = false;
                        }
                    }

                    if ( team.status == FireteamStatus.Assembling )
                    {
                        debugCode = 1100;
                        //Check if the current planet is no longer safe; if it's not safe anymore, flee and regroup
                        if ( !team.DeepInfo.IsCurrentPlanetSafe( faction ) )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "Fireteam " + team.FireTeamID + " no longer has a safe planet to assemble on; retreat!" );
                            GameEntity_Squad retreatPoint = team.GetRetreatPoint( faction, Context, PathCacheData );
                            if ( retreatPoint != null )
                            {
                                team.History.Add( Fireteam.HistoryItem.Create_DoublePlanetRelated( Fireteam.HistoryItemType.RetreatFromLurkLocation, team.DefenseMode, team.LurkPlanet, retreatPoint.Planet ) );
                                team.DeepInfo.DisbandAndRetreat( faction, Context, PathCacheData, retreatPoint );
                            }
                            else
                                team.Disband( faction, Context );
                            continue;
                        }

                        if ( team.DeepInfo.TeamStrength >= team.StrengthToBringOnline )
                            team.status = FireteamStatus.Staging;
                        else
                        {
                            //we're not strong enough to do anything yet
                            //Warden fleet ships should just go to a warden fleet base if possible (if not, join the hunter)
                            if ( team.MustCampOnWardenFleetBase )
                            {
                                GameEntity_Squad wardenbase = GetRandomWardenFleetBaseIfNecessary( team, Context, PathCacheData );
                                if ( wardenbase != null )
                                {
                                    debugCode = 1110;
                                    if ( tracing )
                                        tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is going to begin by assembling at warden fleet base on " + wardenbase.GetPlanetName_Safe() + "\n" );
                                    team.LurkPlanet = wardenbase.Planet;
                                    team.History.Add( Fireteam.HistoryItem.Create_SinglePlanetRelated( Fireteam.HistoryItemType.LurkWithWardenFleetBase, team.DefenseMode, wardenbase.Planet ) );
                                    team.StageFireteamToLurkPlanet( faction, Context, PathCacheData, tracingBuffer, 5f );
                                }
                                else
                                {
                                    //we can't get to a warden fleet base, so join the hunter fleet
                                    //ArcenDebugging.ArcenDebugLogSingleLine( "Donating team " + team.ToString() + " to hunter, path A", Verbosity.DoNotShow );
                                    DonateTeamToAlliedHunter( team, Context );
                                    team.Disband( faction, Context );
                                    continue;
                                }
                            }
                            continue;
                        }
                    }
                    if ( team.status == FireteamStatus.Escorting )
                    {
                        if ( team.Target == null )
                        {
                            team.FindEscortTargetIfPossible( escortingFireteams, ShipsThatNeedEscorting, Context, PathCacheData ); //attempt to find a target
                            if ( team.Target != null )
                            {
                                team.History.Add( Fireteam.HistoryItem.Create_EscortRelated( Fireteam.HistoryItemType.EscortShip, team.Target.TypeData.GetDisplayName() ) );
                                if ( tracing )
                                    tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is now escorting " + team.Target.ToStringWithPlanet() + "\n" );
                            }
                        }

                        if ( team.Target == null )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "Fireteam " + team.FireTeamID + " could not find anything to escort\n" );
                            continue; //nothing to escort right now, just chill
                        }
                        if ( !team.DeepInfo.CanISafelyGetToEscortPlanet( faction, Context, PathCacheData ) )
                        {
                            //I can no longer get to my escort target. Find new orders
                            GameEntity_Squad retreatPoint = team.GetRetreatPoint( faction, Context, PathCacheData );
                            team.DeepInfo.DisbandAndRetreat( faction, Context, PathCacheData, retreatPoint );
                            continue;
                        }
                        team.HandleEscortDuties( faction, Context, PathCacheData, tracingBuffer, 5f ); //either go to your target or fight enemies on your target's planet
                        continue;
                    }
                    debugCode = 1160;
                    if ( team.TargetPlanet == null || (team.LurkStartTime > 0 && ((World_AIW2.Instance.GameSecond - team.LurkStartTime) > 180)) )
                    {
                        debugCode = 1200;
                        if ( tracing )
                            tracingBuffer.Add( "Team " + team.FireTeamID + " is picking a new target. Time started lurking: " + team.LurkStartTime ).Add( "\n" );
                        teamsThatNeedTargets.Add( team );
                        continue;
                    }
                    else if ( tracing )
                        tracingBuffer.Add( "Team " + team.FireTeamID + " has a target and its fine. Lurk Start: " + team.LurkStartTime + " and current time " + World_AIW2.Instance.GameSecond +"\n");

                    if ( team.Target != null && !team.Target.GetIsHostileTowards_Safe( faction ) )
                    {
                        //if we have a target, but we've become friendly to that faction, abandon the target
                        team.DiscardCurrentObjectives();
                        continue;
                    }
                    if ( team.status == FireteamStatus.Staging && team.LurkPlanet != null )
                    {
                        debugCode = 1300;
                        int unused = 0;
                        int defensiveStrengthOfTarget = Fireteam.GetPlanetDefensiveStrength( team.TargetPlanet, faction, true, ref unused, FInt.Zero, FInt.Zero );
                        if ( tracing )
                            tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is staging toward  " + team.LurkPlanet.Name + "; that planet's defensive strength is " + defensiveStrengthOfTarget + ". It has " + team.DeepInfo.ShipsInFireteam.Count + " ships\n" );
                        debugCode = 1310;
                        //Check if the lurk planet is no longer safe; if it's not safe anymore, flee and regroup
                        if ( !team.DeepInfo.IsLurkPlanetSafe( faction ) )
                        {
                            debugCode = 1320;
                            if ( tracing )
                                tracingBuffer.Add( "Fireteam " + team.FireTeamID + " no longer has a safe lurk planet; retreat to an outpost" );
                            GameEntity_Squad retreatPoint = team.GetRetreatPoint( faction, Context, PathCacheData );
                            if ( retreatPoint != null )
                            {
                                team.History.Add( Fireteam.HistoryItem.Create_DoublePlanetRelated( Fireteam.HistoryItemType.RetreatFromLurkLocation, team.DefenseMode, team.LurkPlanet, retreatPoint.Planet ) );
                                team.DeepInfo.DisbandAndRetreat( faction, Context, PathCacheData, retreatPoint );
                            }
                            else
                                team.Disband( faction, Context );
                            continue;
                        }
                        debugCode = 1330;
                        if ( !team.DeepInfo.CanISafelyGetToLurkPlanet( faction, Context, PathCacheData ) )
                        {
                            debugCode = 1340;
                            if ( tracing )
                                tracingBuffer.Add( "Fireteam " + team.FireTeamID + " can't safely get to the lurk planet; retreat " );
                            GameEntity_Squad retreatPoint = team.GetRetreatPoint( faction, Context, PathCacheData );
                            if ( retreatPoint != null )
                            {
                                team.History.Add( Fireteam.HistoryItem.Create_DoublePlanetRelated( Fireteam.HistoryItemType.RetreatFromTravelToLurkLocation, team.DefenseMode, team.LurkPlanet, retreatPoint.Planet ) );
                                team.DeepInfo.DisbandAndRetreat( faction, Context, PathCacheData, retreatPoint );
                                continue;
                            }
                        }
                        debugCode = 1350;
                        team.StageFireteamToLurkPlanet( faction, Context, PathCacheData, tracingBuffer, 5f );
                        if ( team.DeepInfo.CheckIfEnoughUnitsAreLurking() && team.TargetPlanet != null )
                        {
                            debugCode = 1360;
                            if ( team.LurkStartTime == -1 )
                                team.LurkStartTime = World_AIW2.Instance.GameSecond;
                            team.status = FireteamStatus.ReadyToAttack;
                            if ( tracing )
                                tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is ready to attack. Now camp in a nice looking spot" + "\n" );
                            team.History.Add( Fireteam.HistoryItem.Create_DoublePlanetRelated( Fireteam.HistoryItemType.LurkingAgainstAnotherPlanet, team.DefenseMode, team.LurkPlanet, team.TargetPlanet ) );
                            team.CampUnitsOnPlanet( faction, Context, 10f );
                        }
                        debugCode = 1370;
                        if ( Fireteam.IsThisAWinningBattle( faction, Context, team.TargetPlanet, 4 ) ) //if we are already winning
                        {
                            debugCode = 1380;
                            if ( team.TargetPlanet.GetHopsTo( team.DeepInfo.CurrentPlanet ) > 5 )
                            {
                                if ( tracing )
                                    tracingBuffer.Add( "Fireteam " + team.FireTeamID + " was going to a planet that's already been defeated, and we're still a ways away, so find a new target.\n" );
                                team.DiscardCurrentObjectives();
                                continue;
                            }
                        }
                    }
                    debugCode = 1390;
                    if ( team.status == FireteamStatus.Staging || team.status == FireteamStatus.ReadyToAttack )
                    {
                        debugCode = 1400;
                        if ( team.LurkStartTime == -1 )
                            team.LurkStartTime = World_AIW2.Instance.GameSecond;
                        //This code is used for both regular fleets and for DefenseMode (in fact, this is how Defense Mode fleets find a planet to defend)
                        int distanceToScan = 10;
                        if ( Teams.GetItemCount() > 7 )
                            distanceToScan = 5; //if we have lots of fleets, don't bother chasing stuff far away
                        if ( team.DefenseMode )
                            distanceToScan = 10; //defensive fleets look further afield
                        friendlyPlanetsUnderAttack.Clear();
                        GetNearbyAlliedPlanetsUnderAttackThatWeCanHelp( friendlyPlanetsUnderAttack, faction, Context, PathCacheData, team.DeepInfo.CurrentPlanet, team, distanceToScan, Teams );
                        debugCode = 1410;
                        if ( friendlyPlanetsUnderAttack.Count > 0 && !friendlyPlanetsUnderAttack.Contains( team.TargetPlanet ) )
                        {
                            Planet planetToHelp = null;
                            Planet lurkPlanetForHelping = null;
                            debugCode = 1420;
                            for ( int j = 0; j < friendlyPlanetsUnderAttack.Count; j++ )
                            {
                                planetToHelp = friendlyPlanetsUnderAttack[j];
                                lurkPlanetForHelping = faction.DeepInfo.GetFireteamLurkPlanet_OnBackgroundNonSimThread( planetToHelp, 
                                    team.DeepInfo.TeamStrength, team.DeepInfo.CurrentPlanet, Context, PathCacheData );
                                if ( lurkPlanetForHelping != null )
                                {
                                    if ( tracing )
                                        tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is being re-purposed to help out " + friendlyPlanetsUnderAttack[j].Name + "\n" );
                                    break;
                                }
                            }
                            debugCode = 1430;
                            if ( planetToHelp != null && lurkPlanetForHelping != null )
                            {
                                team.DiscardCurrentObjectives();
                                team.TargetPlanet = planetToHelp;
                                team.LurkPlanet = lurkPlanetForHelping;
                                team.status = FireteamStatus.Staging;
                                continue;
                            }
                        }

                        debugCode = 1460;
                        if ( team.DeepInfo.ShouldFireteamRetreatFromCurrentPlanet( faction ) )
                        {
                            debugCode = 1470;
                            GameEntity_Squad retreatPoint = team.GetRetreatPoint( faction, Context, PathCacheData );
                            if ( tracing && team.DeepInfo.CurrentPlanet != null )
                                tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is disbanding to run away from its too-dangerous planet " + team.DeepInfo.CurrentPlanet.Name + " A.\n" );
                            if ( tracing && team.DeepInfo.CurrentPlanet == null )
                                tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is disbanding to run away from its too-dangerous planet, but we have already cleared the planet name A.\n" );
                            debugCode = 1480;
                            if ( retreatPoint != null )
                                team.DeepInfo.DisbandAndRetreat( faction, Context, PathCacheData, retreatPoint );
                            else
                                team.Disband( faction, Context );
                            continue;
                        }
                        debugCode = 1490;
                        if ( team.TargetPlanet.GetControllingFaction().GetIsHostileTowards( faction ) &&
                             team.MustCampOnWardenFleetBase )
                        {
                            debugCode = 1495;
                            if ( tracing )
                                tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is no longer allowed to go for " + team.TargetPlanet.Name + " A.\n" );

                            team.DiscardCurrentObjectives();
                            continue;
                        }
                        debugCode = 1496;
                        if ( tracing )
                            tracingBuffer.Add( "\tpath A.\n" );
                        if ( team.status == FireteamStatus.Staging )
                        {
                            debugCode = 1497;
                            if ( team.DefenseMode && team.TargetPlanet == null )
                                continue; //we can have a lurking defense mode fleet w/o a target
                            //we handle adding the "ready to attack/attacking" teams to regiments below
                            if ( TeamsAimedAtPlanet[team.TargetPlanet] == null )
                            {
                                debugCode = 1498;
                                if ( tracing )
                                    tracingBuffer.Add( "creating a new regiment for this staging team whose target is " + team.TargetPlanet.Name + "\n" );
                                TeamsAimedAtPlanet[team.TargetPlanet] = FireteamRegiment.GetFromPoolOrCreate();
                                TeamsAimedAtPlanet[team.TargetPlanet].TargetPlanet = team.TargetPlanet;
                            }
                            debugCode = 1499;
                            TeamsAimedAtPlanet[team.TargetPlanet].Add( team );
                            TeamsAimedAtPlanet[team.TargetPlanet].TargetPlanet = team.TargetPlanet;
                        }
                    }
                    debugCode = 1500;
                    if ( team.status == FireteamStatus.ReadyToAttack )
                    {
                        if ( team.DeepInfo.ShouldFireteamRetreatFromCurrentPlanet( faction ) )
                        {
                            debugCode = 1510;
                            GameEntity_Squad retreatPoint = team.GetRetreatPoint( faction, Context, PathCacheData );
                            if ( retreatPoint != null )
                            {
                                team.DeepInfo.DisbandAndRetreat( faction, Context, PathCacheData, retreatPoint );
                                continue;
                            }
                        }
                        team.StageFireteamToLurkPlanet( faction, Context, PathCacheData, tracingBuffer, 5f );
                    }
                    debugCode = 1520;
                    if ( team.status == FireteamStatus.ReadyToAttack ||
                         team.status == FireteamStatus.Attacking )
                    {
                        debugCode = 1530;
                        if ( team.DeepInfo.ShipsInFireteam.Count == 0 ) //all our ships are dead :-(
                        {
                            if ( tracing )
                                tracingBuffer.Add( "Fireteam " + team.FireTeamID + " has no more ships. Disband.\n" );

                            team.Disband( faction, Context );
                            continue;
                        }
                        debugCode = 1540;
                        if ( TeamsAimedAtPlanet[team.TargetPlanet] == null )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "creating a new regiment for this Attacking team whose target is " + team.TargetPlanet.Name + "\n" );
                            TeamsAimedAtPlanet[team.TargetPlanet] = FireteamRegiment.GetFromPoolOrCreate();
                            TeamsAimedAtPlanet[team.TargetPlanet].TargetPlanet = team.TargetPlanet;
                        }
                        TeamsAimedAtPlanet[team.TargetPlanet].Add( team );
                        TeamsAimedAtPlanet[team.TargetPlanet].TargetPlanet = team.TargetPlanet;
                        if ( team.SuicideMission )
                            TeamsAimedAtPlanet[team.TargetPlanet].HasSuicidalFireteams = true;
                        debugCode = 1550;
                        if ( TeamsAimedAtPlanet[team.TargetPlanet].HighestMarkUnit < team.DeepInfo.HighestMarkUnit )
                            TeamsAimedAtPlanet[team.TargetPlanet].HighestMarkUnit = team.DeepInfo.HighestMarkUnit;
                    }
                }
                debugCode = 200;
                foreach ( KeyValuePair<Planet, FireteamRegiment> pair in TeamsAimedAtPlanet )
                {
                    debugCode = 2100;
                    //set up the strength calculations
                    FireteamRegiment regiment = pair.Value;
                    Planet planet = pair.Key;
                    debugCode = 2200;
                    regiment.calculateAvailableStrength( faction, attackingStrengthMultiplier );
                    debugCode = 2300;
                    regiment.calculateEnemyStrength( faction, planet, Context, PathCacheData, tracing, tracingBuffer );
                    regiment.CalculateCapabilityFactor( faction, planet, tracing, tracingBuffer );
                    regiment.calculateAttackingSpeed( faction );
                }
                int maxTargetsToConsider = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "FireteamTargetsToScan" ); //checking a target can be expensive, so cap the number of targets
                                                                                                                                          //we are allowed to examine in an iteration
                int targetsConsidered = 0;

                //randomize the list of teams that need targets to make starvation much less likely
                ArcenArrays.Randomize( teamsThatNeedTargets, Context.RandomToUse, 3 );
                debugCode = 3000;
                for ( int i = 0; i < teamsThatNeedTargets.Count; i++ )
                {
                    debugCode = 3100;
                    int targetsForThisTeam = 0;
                    //also have the fireteam regiment track fireteams staging toward that target, and then also update that value each time we add a new fireteam to a target
                    //In the meantime, GetTargetAndLurkPlanets also has the FireteamRegiments passed in, so we can throw out all the targets we are overkilling
                    Fireteam team = null;
                    try
                    {
                        team = teamsThatNeedTargets[i];
                    }
                    catch { continue; } //this can be changed and cause an argument out of range exception
                    Planet originalTargetPlanet = team.TargetPlanet;
                    debugCode = 3110;
                    if ( tracing )
                    {
                        debugCode = 3120;
                        if ( i == 0 )
                            tracingBuffer.Add( "There are " + teamsThatNeedTargets.Count + " teams looking for targets\n" );
                        debugCode = 3130;
                        tracingBuffer.Add( " Fireteam " + team.FireTeamID + " is looking for a target.\n" );
                    }
                    debugCode = 3140;
                    if ( trackTimings )
                        incrementalStopwatch.Start();
                    debugCode = 3150;
                    team.GetTargetAndLurkPlanets( faction, TeamsAimedAtPlanet, out targetsForThisTeam, Context, PathCacheData, tracingBuffer );
                    if ( trackTimings )
                    {
                        incrementalStopwatch.Stop();
                        targetAndLurkTime += incrementalStopwatch.ElapsedMilliseconds;
                        incrementalStopwatch.Reset();
                    }
                    targetsConsidered += targetsForThisTeam;
                    debugCode = 3160;
                    team.LurkStartTime = -1;
                    if ( team.TargetPlanet != null && team.TargetPlanet != originalTargetPlanet )
                    {
                        debugCode = 3200;
                        team.status = FireteamStatus.Staging;
                        team.History.Add( Fireteam.HistoryItem.Create_TriplePlanetRelated( Fireteam.HistoryItemType.StagingToLurkPlanet, team.DefenseMode, team.DeepInfo.CurrentPlanet, team.TargetPlanet, team.LurkPlanet ) );
                        if ( TeamsAimedAtPlanet[team.TargetPlanet] == null )
                        {
                            TeamsAimedAtPlanet[team.TargetPlanet] = FireteamRegiment.GetFromPoolOrCreate();
                            TeamsAimedAtPlanet[team.TargetPlanet].TargetPlanet = team.TargetPlanet;
                        }
                        TeamsAimedAtPlanet[team.TargetPlanet].Add( team );
                        TeamsAimedAtPlanet[team.TargetPlanet].calculateAvailableStrength( faction, FInt.One );
                        if ( tracing )
                        {
                            if ( team.Target != null )
                                tracingBuffer.Add( "Fireteam " + team.FireTeamID + " now has target " + team.Target.ToStringWithPlanetAndOwner() + " and lurk on " + team.LurkPlanet.Name + ". We have considered " + targetsConsidered + " total targets for all teams so far.\n" );
                        }

                    }
                    debugCode = 3300;
                    if ( team.TargetPlanet == null )
                    {
                        debugCode = 3400;
                        if ( team.LurkPlanet == null )
                        {
                            debugCode = 3410;
                            if ( team.DefenseMode && !team.MustCampOnWardenFleetBase )
                            {
                                debugCode = 3420;
                                if ( planetsToDefend != null && planetsToDefend.Count > 0 )
                                {
                                    debugCode = 3430;
                                    for ( int j = planetsToDefend.Count - 1; j >= 0; j-- )
                                    {
                                        debugCode = 3440;
                                        //we've already removed the planets that already have defensive fireteams lurking there.
                                        //We have also sorted from "weakest" to "strongest" planets
                                        Planet potentialLurkPlanet = planetsToDefend[j];
                                        if ( potentialLurkPlanet == null )
                                            continue;
                                        debugCode = 3450;
                                        short hops = 0;
                                        int danger = Fireteam.GetDangerOfPath( faction, Context, PathCacheData, potentialLurkPlanet, team.DeepInfo.CurrentPlanet, true, out hops );

                                        debugCode = 3460;
                                        if ( danger > team.DeepInfo.TeamStrength )
                                            continue;
                                        team.LurkPlanet = potentialLurkPlanet;
                                        debugCode = 3470;
                                        planetsToDefend.Remove( potentialLurkPlanet );
                                        break;
                                    }
                                }

                                debugCode = 3480;
                                if ( team.LurkPlanet == null )
                                {
                                    debugCode = 3490;
                                    //this is the case where we have nothing specifically requested to defend,
                                    //or none of those is a good idea
                                    if ( team.DeepInfo.CurrentPlanet != null )
                                    {
                                        debugCode = 3495;
                                        team.LurkPlanet = team.DeepInfo.CurrentPlanet;
                                        team.SendFireteamToPlanet( faction, team.DeepInfo.CurrentPlanet, Context, PathCacheData, tracingBuffer, 5f );
                                    }
                                }
                                debugCode = 3498;
                                team.History.Add( Fireteam.HistoryItem.Create_SinglePlanetRelated( Fireteam.HistoryItemType.DefensiveFleetWaitingInGeneral, team.DefenseMode, team.DeepInfo.CurrentPlanet ) );
                            }
                            debugCode = 3500;
                            if ( team.MustCampOnWardenFleetBase )
                            {
                                debugCode = 3510;
                                //find a warden fleet base to lurk on
                                GameEntity_Squad wardenbase = GetRandomWardenFleetBaseIfNecessary( team, Context, PathCacheData );
                                debugCode = 3520;
                                if ( wardenbase != null )
                                {
                                    debugCode = 3530;
                                    if ( tracing )
                                        tracingBuffer.Add( "Fireteam " + team.FireTeamID + " could not find a good target, but it's off to lurk on a warden fleet base on " + wardenbase.GetPlanetName_Safe() + "\n" );
                                    team.LurkPlanet = wardenbase.Planet;
                                    team.History.Add( Fireteam.HistoryItem.Create_SinglePlanetRelated( Fireteam.HistoryItemType.LurkWithWardenFleetBase, team.DefenseMode, wardenbase.Planet ) );
                                    team.SendFireteamToPlanet( faction, wardenbase.Planet, Context, PathCacheData, tracingBuffer, 5f );
                                }
                                else
                                {
                                    debugCode = 3580;
                                    //team could not find a wardenbase to lurk on; join hunter fleet
                                    //ArcenDebugging.ArcenDebugLogSingleLine( "Donating team " + team.ToString() + " to hunter, path B", Verbosity.DoNotShow );
                                    DonateTeamToAlliedHunter( team, Context );
                                    team.Disband( faction, Context );
                                    continue;
                                }
                            }
                        }
                        else if ( tracing )
                            tracingBuffer.Add( "Fireteam " + team.FireTeamID + " could not find a good target. Try again next iteration, path B\n" );
                    }
                    if ( targetsConsidered >= maxTargetsToConsider )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "By fireteam " + i + " of " + teamsThatNeedTargets.Count + " we have examined " + targetsConsidered + " targets, which is >= " + maxTargetsToConsider + ". We'll get to the others next LRP\n" );
                        break;
                    }
                }
                if ( tracing )
                {
                    tracingBuffer.Add( "Total strength for the " + Teams.GetItemCount() + " fireteams in each of the states (at the beginning of this, so doesn't have any changes made this UpdateFireteams run, see the next run) is" ).Add( "\n" );
                    foreach ( KeyValuePair<FireteamStatus, int> pair in totalStrengthPerState )
                    {
                        tracingBuffer.Add( pair.Key.ToString() + ": " + pair.Value ).Add( "\n" );
                    }
                }

                FireteamUtility.RemoveCompletedMissions( faction, Context );

                if ( trackTimings )
                {
                    overallStopwatch.Stop();
                    if ( tracing )
                        tracingBuffer.Add( "UpdateFireteams for " + faction.GetDisplayName() + " " + faction.FactionIndex + " took " + overallStopwatch.ElapsedMilliseconds + " milliseconds. Time spend doing target and lurk code: " + targetAndLurkTime + "ms" ).Add( "\n" );
                    else
                        ArcenDebugging.ArcenDebugLogSingleLine( "UpdateFireteams for " + faction.GetDisplayName() + " " + faction.FactionIndex + " took " + overallStopwatch.ElapsedMilliseconds + " milliseconds. Time spend doing target and lurk code: " + targetAndLurkTime + "ms", Verbosity.DoNotShow );
                }
            }
            catch ( ArcenPleaseStopThisThreadException )
            {
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                string errorString = e.ToString();
                if ( errorString.Contains( "ThreadAbortException" ) )
                    return; //this comes from when it happens during a sort
                if ( errorString.Contains( "ArcenPleaseStopThisThreadException" ) )
                    return; //this comes from when it happens during a sort
                if ( errorString.Contains( "compare two elements" ) )
                    return; //this comes from when it happens during a sort
                //
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    LOG.Msg("FireteamUtility-UpdateFireteams Error: faction={0} debugcode={1} during update fireteams.\n{2}", faction?.GetDisplayName()??"[null]", debugCode, e);
            }
            finally
            {
                Fireteam.ReleaseTemporaryFireteamStatusDictOfInts( totalStrengthPerState );
                Fireteam.ReleaseTemporaryFireteamList( teamsThatNeedTargets );
                Fireteam.ReleaseTemporaryFireteamList( escortingFireteams );
                Planet.ReleaseTemporaryPlanetList( planetsToDefend );
                Planet.ReleaseTemporaryPlanetList( friendlyPlanetsUnderAttack );
                if ( trackPerformanceTicks )
                {
                    faction.UpdateFireteamsTicks = faction.Fireteamstopwatch.ElapsedTicks;
                    faction.Fireteamstopwatch.Reset();
                }

                #region Tracing
                if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
            }
        }

        public static void UpdateRegiments( Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData,
            ArcenLessLinkedList<Fireteam> AllFactionTeams, ProtectedValDictionary<Planet, FireteamRegiment> TeamsAimedAtPlanet, ArcenCharacterBuffer tracingBuffer, int MinFireteamStrength,
            bool allowedToDecloakEnemies )
        {
            int debugCode = 0;
            bool tracing = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam ) && tracingBuffer != null;
            bool trackPerformanceTicks = GameSettings.Current.GetBoolBySetting( "PerformanceTicksDebug" );
            try
            {
                debugCode = 100;
                bool trackTimings = tracing;
                System.Diagnostics.Stopwatch overallStopwatch = null;
                System.Diagnostics.Stopwatch incrementalStopwatch = null;

                if ( trackTimings )
                {
                    overallStopwatch = new System.Diagnostics.Stopwatch();
                    incrementalStopwatch = new System.Diagnostics.Stopwatch();
                }
                if ( trackPerformanceTicks )
                {
                    faction.Fireteamstopwatch.Start();
                }

                if ( trackTimings )
                    overallStopwatch.Start();
                debugCode = 200;
                int totalRegimentalStrength = 0;
                foreach ( KeyValuePair<Planet, FireteamRegiment> pair in TeamsAimedAtPlanet )
                {
                    debugCode = 300;
                    Planet targetPlanet = pair.Key;
                    FireteamRegiment regiment = pair.Value;
                    debugCode = 305;
                    if ( regiment.teams == null || regiment.stagingteams == null )
                        throw new Exception("Confused regiments");

                    ArcenLessLinkedList<Fireteam> teamsOfRegiment = regiment.teams;
                    debugCode = 310;
                    if ( regiment.teams.GetItemCount() == 0 )
                    {
                        debugCode = 400;
                        if ( regiment.stagingteams != null && regiment.stagingteams.GetItemCount() > 0 && tracing )
                            tracingBuffer.Add("Regimental Status overview for fireteams targeting " + pair.Key.Name + ". There are " + regiment.stagingteams.GetItemCount() + " staging teams and no teams ready to strike.\n");
                        continue; //we can have a fireteam regiment with only staging fleets, and for that case don't bother processing them
                    }
                    debugCode = 320;
                    int defensiveEnemyStrength = regiment.mobileEnemyStrength;
                    Fireteam firstTeam = teamsOfRegiment.GetFirst().Contained;
                    debugCode = 500;

                    GameEntity_Squad targetOrNull = firstTeam.Target;
                    if ( targetOrNull != null && regiment.TargetEntity == null )
                    {
                        regiment.TargetEntity = targetOrNull;
                    }
                    bool targetHasForcefield = firstTeam.DeepInfo.TargetProtectedByForcefield;
                    bool MustCampOnWardenFleetBase = firstTeam.MustCampOnWardenFleetBase;
                    int availableStrength = regiment.availableStrength;
                    int enemyStrength = regiment.netEnemyStrength;
                    FInt advantageRatioRequiredForAttack;
                    debugCode = 3010;
                    debugCode = 3010;
                    VassalMission mission = targetPlanet.GetMissionForFaction( faction, VassalMissionType.Combat );
                    string missionString = "No mission";

                    if ( FactionUtilityMethods.Instance.IsFactionAlliedToAnyPlayer( faction) )
                    {
                        //For player allied fireteams, double-count our friendly strength.
                        //This will make player-allied fireteams much more willing to enter a close fight
                        //to help the player
                        var factionData = targetPlanet.GetStanceDataForFaction( faction );
                        int friendlyStrength = factionData[FactionStance.Friendly].TotalStrength;
                        availableStrength += friendlyStrength;
                        //missionString = " We have a " + mission.Priority + " mission here.";
                        //if ( friendlyStrength > 0 )
                        //missionString += " Our allies have " + friendlyStrength + " strength here.";
                    }
                    //Counter-awareness (E): a regiment whose composition counters these defenders punches above its raw
                    //strength, so it can commit sooner (and is less eager to retreat). Apply the matchup multiplier to a
                    //derived "effective" strength used in the attack/retreat decisions; the raw value stays in the trace
                    //and on the path-danger gate. CapabilityFactor is 1.0 (no change) for a neutral matchup. The factor is
                    //still computed for everyone, but only fed into the decision when DLC4 is enabled.
                    int effectiveAvailableStrength = GetIsDlc4InstalledAndEnabled()
                        ? (regiment.CapabilityFactor * availableStrength).IntValue
                        : availableStrength;
                    if ( targetPlanet.GetControllingFactionType() == FactionType.Player &&
                         targetPlanet.GetControllingFaction().GetIsHostileTowards(faction ) )
                    {
                        //If we are currently already attacking this faction, everyone else is more willing to go in too.
                        //The goal is "The AI hits planet X because its strong enough to win, but also will go after planet Y in a close battle to make sure the player is distracted"
                        advantageRatioRequiredForAttack = advantageRatioRequiredForAttackPlayer;
                        if ( CurrentlyAttackingFactionFromPlanet( targetPlanet, TeamsAimedAtPlanet) )
                            advantageRatioRequiredForAttack = advantageRatioRequiredForAttackPlayerJoiningIn;
                    }
                    else
                        advantageRatioRequiredForAttack = advantageRatioRequiredForAttackOther;

                    if ( tracing )
                        tracingBuffer.Add( "Regimental Status overview for fireteams targeting " + pair.Key.Name + ". We have " + teamsOfRegiment.GetItemCount() + " teams of available strength " + availableStrength + " (effective " + effectiveAvailableStrength + ", total " + regiment.myTotalStrength +") and net enemy strength " + enemyStrength + " total enemy strength " + regiment.totalEnemyStrength + ". danger of path only: " + regiment.dangerOfPathOnly + ". advantage ratio for attack: " + advantageRatioRequiredForAttack + ". Team 0 is in state " + firstTeam.status + " " + missionString + ". capability factor " + regiment.CapabilityFactor + "\n" );
                    totalRegimentalStrength += regiment.myTotalStrength;
                    if ( regiment.availableStrength < regiment.dangerOfPathOnly )
                    {
                        if ( tracing )
                            tracingBuffer.Add("\tSkipped; it's too dangerous to go there\n");
                        continue;
                    }
                    if ( regiment.HasSuicidalFireteams )
                    {
                        foreach ( Fireteam team in Fireteam.LiveTeamsIn( teamsOfRegiment ) )
                        {
                            if ( team.SuicideMission && team.status == FireteamStatus.ReadyToAttack )
                            {
                                if ( team.TargetPlanet == null )
                                    team.TargetPlanet = targetPlanet;
                                if ( team.TargetPlanet != null )
                                    team.AttackTargetPlanet( faction, Context, PathCacheData, 2f );
                            }
                        }
                    }
                    if ( MustCampOnWardenFleetBase && targetPlanet.GetControllingFaction().GetIsHostileTowards( faction ) )
                    {
                        //Warden fleet fireteams will abandon a target if its no longer valid
                        if ( tracing )
                            tracingBuffer.Add( "\t" + pair.Key.Name + " is no longer a valid target (player captured it). 10% chance of abandoning" );
                        if ( Context.RandomToUse.Next( 0, 100 ) < 10 )
                        {
                            foreach ( Fireteam team in Fireteam.LiveTeamsIn( teamsOfRegiment ) )
                            {
                                if ( team.DeepInfo.TeamStrength < MinFireteamStrength )
                                {
                                    team.Disband( faction, Context );
                                }
                                else
                                {
                                    //this fireteam can be reused for new duties
                                    if ( tracing )
                                        tracingBuffer.Add( "\t\tFireteam " + team.FireTeamID + " is being returned to active duty since its target is no longer valid\n" );
                                    team.DiscardCurrentObjectives();
                                }
                            }
                        }
                    }

                    if ( regiment.HasWonBattle( faction ) &&
                         (regiment.GetTotalEnemyStrengthForRegiment( faction ) <= 0 || //either no enemies
                          (regiment.declareVictoryEarly && regiment.GetTotalEnemyStrengthForRegiment( faction ) <= 20 * 10000 ) ) ) //or we shouldn't bother trying to kill everything
                    {
                        debugCode = 3100;
                        //We won! disband so we can upgrade/attack other things
                        if ( tracing )
                            tracingBuffer.Add( "\tFireteams available " + pair.Key.Name + " are victorious!" );
                        foreach ( Fireteam team in Fireteam.LiveTeamsIn( teamsOfRegiment ) )
                        {
                            if ( team.DeepInfo.TeamStrength < MinFireteamStrength )
                            {
                                team.Disband( faction, Context );
                            }
                            else
                            {
                                //this fireteam can be reused for new duties
                                if ( tracing )
                                    tracingBuffer.Add( "\t\tFireteam " + team.FireTeamID + " is being returned to active duty because we won\n" );
                                team.DiscardCurrentObjectives();
                            }
                        }
                    }
                    else if ( effectiveAvailableStrength >= (enemyStrength * advantageRatioRequiredForAttack).IntValue + regiment.dangerOfPathOnly / 10)
                    {
                        debugCode = 3200;
                        //Generic attack path
                        foreach ( Fireteam team in Fireteam.LiveTeamsIn( teamsOfRegiment ) )
                        {
                            debugCode = 3210;
                            bool attackStarted = false;
                            if ( team.TargetPlanet == null )
                                team.TargetPlanet = targetPlanet;
                            if ( team.TargetPlanet != null )
                                team.AttackTargetPlanet( faction, Context, PathCacheData, 2f, regiment.AttackingSpeed );
                            if ( team.status != FireteamStatus.Attacking )
                            {
                                debugCode = 3220;
                                attackStarted = true;
                                team.status = FireteamStatus.Attacking;
                                team.History.Add( Fireteam.HistoryItem.Create_DoublePlanetRelated( Fireteam.HistoryItemType.AttackFromPlanetToPlanet, team.DefenseMode, team.DeepInfo.CurrentPlanet, team.TargetPlanet ) );
                                team.LurkStartTime = -1;
                            }
                            if ( tracing && attackStarted )
                                tracingBuffer.Add( "\tFireteam " + team.FireTeamID + " start attack on " + team.TargetPlanet.Name +"! Speed " + regiment.AttackingSpeed );
                        }
                        if ( regiment.totalEnemyStrengthLiteral < regiment.strengthOnTarget / 4 && allowedToDecloakEnemies )
                        {
                            debugCode = 3300;
                            if ( tracing )
                            {
                                if ( targetOrNull == null )
                                    tracingBuffer.Add( "\ttachyon blast " + pair.Key.Name + " if necessary. target is null. defensive strength " + regiment.totalEnemyStrengthLiteral + " available strength " + availableStrength + "\n" );
                                else
                                    tracingBuffer.Add( "\ttachyon blast " + pair.Key.Name + " if necessary. target is " + targetOrNull.ToStringWithPlanet() + "\n" );
                            }
                            //if we are already massively winning, decloak cloaked enemies
                            FactionUtilityMethods.Instance.TachyonBlastPlanet( targetPlanet, faction, Context );
                        }
                    }
                    else
                    {
                        debugCode = 3400;
                        foreach ( Fireteam team in Fireteam.LiveTeamsIn( teamsOfRegiment ) )
                        {
                            debugCode = 3500;
                            if ( team == null ) //a dead fireteam might have already been disbanded
                                continue;

                            
                            bool doesPlanetHaveAlliedKing( Planet p )
                            {
                                bool result = false;
                                foreach ( GameEntity_Squad e in p.Squads( EntityRollupType.KingUnitsOnly ) )
                                {
                                    if ( e.GetIsFriendlyTowards_Safe( faction ) && e.FireteamId != team.FireTeamID )
                                    {
                                        result = true;
                                        break;
                                    }
                                }

                                return result;
                            }
                            
                            if ( doesPlanetHaveAlliedKing( team.DeepInfo.CurrentPlanet ) ||
                                 doesPlanetHaveAlliedKing( targetPlanet ) )
                            {
                                if ( tracing )
                                    tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is defending its king; fight to the death\n" );
                                continue;
                            }
                            FInt ratioForRetreat = outnumberRatioRequiredForRetreatNormal;
                            if ( targetPlanet.GetControllingFactionType() == FactionType.Player &&
                                 targetPlanet.GetControllingFaction().GetIsHostileTowards(faction ) )
                            {
                                debugCode = 3600;
                                ratioForRetreat = outnumberRatioRequiredForRetreatPlayer;
                                if ( regiment.ExtraCautiousAgainstPlayers )
                                    ratioForRetreat = outnumberRatioRequiredForRetreatPlayer + FInt.FromParts(0, 100);
                            }
                            debugCode = 3700;
                            if ( (effectiveAvailableStrength * ratioForRetreat).IntValue < enemyStrength &&
                                team.status == FireteamStatus.Attacking )
                            {
                                debugCode = 3800;
                                GameEntity_Squad retreatPoint = team.GetRetreatPoint( faction, Context, PathCacheData );

                                if ( retreatPoint != null )
                                {
                                    if ( tracing )
                                        tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is has been defeated in its attack of " + team.DeepInfo.CurrentPlanet.Name + ". Retreat to " + retreatPoint.ToStringWithPlanet() + " \n" );
                                    team.DeepInfo.DisbandAndRetreat( faction, Context, PathCacheData, retreatPoint );
                                }
                                else
                                {
                                    if ( tracing )
                                        tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is has been defeated in its attack of " + team.DeepInfo.CurrentPlanet.Name + ". Fight to the death \n" );
                                    team.Disband( faction, Context );
                                }
                                continue;
                            }
                            debugCode = 3900;
                        }
                        debugCode = 4000;
                    }
                    debugCode = 5000;
                }
                if ( tracing )
                    tracingBuffer.Add("Total strength of all regiments: " + totalRegimentalStrength).Add("\n");
                if ( trackTimings )
                {
                    overallStopwatch.Stop();
                    if ( tracing )
                        tracingBuffer.Add("UpdateRegiments for " + faction.GetDisplayName() + " " + faction.FactionIndex + " took " + overallStopwatch.ElapsedMilliseconds + " milliseconds").Add("\n");
                    else
                        ArcenDebugging.ArcenDebugLogSingleLine("UpdateRegiments for " + faction.GetDisplayName() + " " + faction.FactionIndex + " took " + overallStopwatch.ElapsedMilliseconds + " milliseconds", Verbosity.DoNotShow );
                }

            }
            catch ( ArcenPleaseStopThisThreadException )
            {
                //this is the main thread ending us.  Guess we'll skip reporting it
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "debug code " + debugCode + " during update fireteam regiments for " + faction.GetDisplayName() + " " + e.ToString(), Verbosity.DoNotShow );
            }
            if ( trackPerformanceTicks )
            {
                faction.UpdateRegimentsTicks = faction.Fireteamstopwatch.ElapsedTicks;
                faction.Fireteamstopwatch.Reset();
            }

            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion

        }
        private static bool CurrentlyAttackingFactionFromPlanet( Planet playerPlanet, ProtectedValDictionary<Planet, FireteamRegiment> TeamsAimedAtPlanet )
        {
            //So we are considering whether to attack playerPlanet. If we are also attacking a different
            //planet currently owned by this player then we are more likely to just dive in right now
            int debugCode = 0;
            try{
                foreach ( KeyValuePair<Planet, FireteamRegiment> pair in TeamsAimedAtPlanet )
                {
                debugCode = 300;
                if ( pair.Key == playerPlanet )
                    continue; //just in case

                if ( pair.Key.GetControllingFaction() != playerPlanet.GetControllingFaction() )
                    continue;
                debugCode = 400;
                ArcenLessLinkedList<Fireteam> teamsOfRegiment = pair.Value.teams;
                if ( teamsOfRegiment == null ||
                     teamsOfRegiment.GetItemCount() == 0 )
                    continue;
                debugCode = 500;
                Fireteam firstTeam = teamsOfRegiment.GetFirst().Contained;
                if ( firstTeam != null &&
                     firstTeam.status == FireteamStatus.Attacking )
                    return true;
            }
            }catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in CurrentlyAttackingFactionFromPlanet " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            return false;
        }

        public static void GetRegimentsForDisplay( ProtectedValDictionary<Planet, FireteamRegiment> DictToFill, Faction faction, ArcenLessLinkedList<Fireteam> Teams, PerFactionPathCache PathCacheData )
        {
            //only for display; must be called after UpdateFireteams
            DictToFill.Clear();

            int debugCode = 0;
            try
            {
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( Teams ) )
                {
                    debugCode = 100;
                    if ( team == null || team.TargetPlanet == null )
                        continue;
                    if ( team.status == FireteamStatus.Staging )
                    {
                        debugCode = 200;
                        if ( DictToFill[team.TargetPlanet] == null )
                        {
                            debugCode = 220;
                            DictToFill[team.TargetPlanet] = FireteamRegiment.GetFromPoolOrCreate();
                            debugCode = 220;
                            DictToFill[team.TargetPlanet].TargetPlanet = team.TargetPlanet;
                        }
                        debugCode = 300;
                        DictToFill[team.TargetPlanet].Add( team );
                        DictToFill[team.TargetPlanet].TargetPlanet = team.TargetPlanet;
                        DictToFill[team.TargetPlanet].ExtraCautiousAgainstPlayers = team.ExtraCautiousAgainstPlayers;
                    }
                    if ( team.status == FireteamStatus.ReadyToAttack ||
                         team.status == FireteamStatus.Attacking )
                    {
                        debugCode = 400;
                        if ( DictToFill[team.TargetPlanet] == null )
                        {
                            DictToFill[team.TargetPlanet] = FireteamRegiment.GetFromPoolOrCreate();
                            DictToFill[team.TargetPlanet].TargetPlanet = team.TargetPlanet;
                        }
                        debugCode = 500;
                        DictToFill[team.TargetPlanet].Add( team );
                        DictToFill[team.TargetPlanet].TargetPlanet = team.TargetPlanet;
                        DictToFill[team.TargetPlanet].ExtraCautiousAgainstPlayers = team.ExtraCautiousAgainstPlayers;
                    }
                }
                foreach ( KeyValuePair<Planet, FireteamRegiment> pair in DictToFill )
                {
                    debugCode = 600;
                    //set up the strength calculations
                    FireteamRegiment regiment = pair.Value;
                    Planet planet = pair.Key;
                    regiment.calculateAvailableStrength( faction, FInt.One );
                    regiment.calculateEnemyStrength( faction, planet, null, PathCacheData, false, null );
                }
                debugCode = 700;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "hit debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }

        public static void SortTargets(Fireteam team, Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, ref List<FireteamTarget> TargetList,
            ProtectedValDictionary<Planet, FireteamRegiment> TeamsAimedAtPlanet, Planet CurrentPlanetForFireteam, ArcenCharacterBuffer tracingBuffer, 
            bool overrideIgnoreDeathballing)
        {
            bool tracing = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam ) && tracingBuffer != null;
            FInt falloffForDistance = FInt.FromParts (0, 050);
            if ( tracing )
                tracingBuffer.Add("\tSorting ").Add(TargetList.Count ).Add( " targets\n");

            List<Planet> workingCheckedPlanets = Planet.GetTemporaryPlanetList( "FireteamUtil-SortTargets-workingCheckedPlanets", 10f );
            if ( workingCheckedPlanets == null ) //blocked for teardown/shutdown; bail
                return;

            //Lets go over all the targets and quickly remove ones that we don't need to go after
            for ( int i = TargetList.Count - 1; i >= 0; i-- )
            {
                FireteamTarget target = TargetList[i];
                if ( workingCheckedPlanets.Contains(target.planet ) )
                {
                    if ( tracing )
                        tracingBuffer.Add("\tFor planet " + target.GetPlanetName_Safe() + " there is currently already an Target on that planet, so skip\n");
                    TargetList.RemoveAt(i);
                    continue;
                }
                if ( Fireteam.IsThisAWinningBattle(faction, Context, target.planet) )
                {
                    //remove any battles we are currently winning
                    VassalMission mission = target.planet.GetMissionForFaction( faction, VassalMissionType.Combat );
                    if ( !team.DefenseMode && //defense mode fleets don't care if they've won; they need to stick around anway
                         mission != null  ) //TODO: make sure this is the right sort of mission
                    {
                        //Remove our mission and send a message to our Leige
                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.RemoveVassalMission], GameCommandSource.AnythingElse );
                        command.RelatedIntegers.Add( target.planet.Index ); //which planet
                        command.RelatedIntegers2.Add( faction.FactionIndex );
                        command.RelatedString = mission.OrderType.InternalName;
                        World_AIW2.Instance.QueueGameCommand( faction, command, false );

                        ArcenCharacterBuffer output = ArcenCharacterBuffer.GetFromPoolOrCreate( "FireteamUtility-SortTargets-output" );
                        output.Add( faction.GetDisplayName(), faction.FactionCenterColor.ColorHexBrighter ).Add( " has dealt with " ).Add( target.GetPlanetName_Safe(), "a1ffa1" );

                        PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                        if ( chatHandlerOrNull != null )
                            chatHandlerOrNull.PlanetToView = target.planet;

                        World_AIW2.Instance.QueueChatMessageOrCommand( output.ToStringAndReturnToPool(), ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                    }
                    if ( tracing )
                        tracingBuffer.Add("\tFor planet " + target.GetPlanetName_Safe() + " we have already won\n");

                    TargetList.Remove(target);
                    continue;
                }

                if ( team.NoDeathballing && !overrideIgnoreDeathballing)
                {
                    //to reduce deathballing, if a target is being overkilled then we won't go for it
                    int unused = 0;
                    target.dangerOfTarget = Fireteam.GetPlanetDefensiveStrength(target.planet, faction, false, ref unused, FInt.Zero, FInt.Zero);
                    FireteamRegiment regiment = TeamsAimedAtPlanet[target.planet];

                    if (regiment != null )
                    {
                        //Cap committed strength at roughly "enough to win + margin" and redirect any surplus to other
                        //targets (proportional allocation). We anchor the cap to netEnemyStrength when we have it --
                        //that's the same remote-inclusive quantity the attack decision uses, so we never redirect teams
                        //away from a target before it could actually be taken -- and fall back to on-planet defensive
                        //strength for freshly-created regiments that have not had their enemy strength computed yet.
                        int enemyStrengthForCap = regiment.netEnemyStrength > 0 ? regiment.netEnemyStrength : target.dangerOfTarget;
                        int cap;
                        if ( team.DeathballingThreshold > 0 )
                        {
                            //explicit per-faction tuning (e.g. Hunter, Custodians): keep the original flat multiple of on-planet defense
                            cap = target.dangerOfTarget * team.DeathballingThreshold;
                        }
                        else
                        {
                            //target-aware default: allow more concentration against the player (harder to finish, and
                            //multi-front pressure is desirable), redirect surplus away from softer targets sooner.
                            FInt winRatio = ( target.planet.GetControllingFactionType() == FactionType.Player &&
                                              target.planet.GetControllingFaction().GetIsHostileTowards( faction ) )
                                            ? advantageRatioRequiredForAttackPlayer
                                            : advantageRatioRequiredForAttackOther;
                            cap = (winRatio * DeathballOvercommitMargin * (FInt)enemyStrengthForCap).IntValue;
                        }
                        if ( regiment.myTotalStrength > cap ) //we've already committed enough to win this with margin
                        {
                            //skip targets we have already allocated a lot of ships toward (even if we haven't started to attack yet)
                            if ( tracing )
                                tracingBuffer.Add("\tFor planet " + target.GetPlanetName_Safe() + " there is currently " + regiment.availableStrength + " available and " + regiment.stagingStrength + " staging strength against it (total " + regiment.myTotalStrength + ", cap " + cap + ", enemy " + enemyStrengthForCap + ")").Add("; discard this target (no deathballing)").Add("\n");
                            TargetList.RemoveAt(i);
                            continue;
                        }
                        else
                        {
                            if ( tracing )
                                tracingBuffer.Add("\tFor planet " + target.GetPlanetName_Safe() + " there is currently " + regiment.availableStrength + " available and " + regiment.stagingStrength + " staging strength against it (total " + regiment.myTotalStrength + ", cap " + cap + ", enemy " + enemyStrengthForCap + ")").Add("; keep this target (no deathballing path)").Add("\n");
                        }
                    }
                }

                workingCheckedPlanets.Add(target.planet);
            }

            Planet.ReleaseTemporaryPlanetList( workingCheckedPlanets );
            if ( tracing )
                tracingBuffer.Add("\tAfter pre-processing we now have ").Add(TargetList.Count ).Add( " targets\n");

            //After that pre-processing, calculate the path danger for each (this is a performance hit)
            for ( int i = 0; i < TargetList.Count; i++ )
            {
                FireteamTarget target = TargetList[i];
                Int16 ignored = 0;
                bool includeDestination = true;
                target.dangerOfPath = Fireteam.GetDangerOfPath(faction, Context, PathCacheData, CurrentPlanetForFireteam, target.planet, includeDestination, out ignored);
                TargetList[i] = target;
            }
            cb_fuFaction = faction;
            cb_fuTargetCurrentPlanet = CurrentPlanetForFireteam;
            cb_fuTargetFalloff = falloffForDistance;
            TargetList.Sort( static delegate ( FireteamTarget Left, FireteamTarget Right )
            {
                //sort based on danger of the specific target
                int lDistance = Left.planet.GetHopsTo(cb_fuTargetCurrentPlanet);
                int rDistance = Right.planet.GetHopsTo(cb_fuTargetCurrentPlanet);
                int lDanger = Left.dangerOfPath;
                int rDanger = Right.dangerOfPath;

                //Sort based on priority if the priority >= Medium
                if ( (Left.priority >= MissionPriority.Medium ||
                     Right.priority >= MissionPriority.Medium) &&
                     Left.priority != Right.priority)
                    return Left.priority.CompareTo( Right.priority );
                //For lower-priority cases, prefer weaker targets.
                //Note that Low priority targets will look tastier
                if ( Left.priority == MissionPriority.Low )
                    lDanger /= 2;
                if ( Right.priority == MissionPriority.Low )
                    rDanger /= 2;

                //Be willing to defend our own planets
                if ( Left.planet.GetControllingOrInfluencingFaction() == cb_fuFaction )
                    lDanger /= 2;
                if ( Right.planet.GetControllingOrInfluencingFaction() == cb_fuFaction )
                    rDanger /= 2;

                lDanger = lDanger + (cb_fuTargetFalloff * lDistance).IntValue;
                rDanger = rDanger + (cb_fuTargetFalloff * rDistance).IntValue;

                return lDanger.CompareTo(rDanger);
            } );

            for ( int i = 0; i < TargetList.Count; i++ )
            {
                //discard things that are enough more dangerous than other things on the list
                //First find a target we aren't comfortably winning (if we are winning all of them, we don't discard anything
                if ( i == 0 )
                    continue;
                FireteamTarget target = TargetList[i];
                FireteamTarget weakestTarget = TargetList[0];

                FInt danger = (FInt)target.dangerOfPath;
                FInt weakestDanger = (FInt)weakestTarget.dangerOfPath;
                if ( target.planet.GetControllingOrInfluencingFaction() == faction )
                    danger /= 2;
                int maxTargets = 30; //since these are sorted by difficulty, don't bother with more than 30 targets
                if ( danger > weakestDanger * FInt.FromParts(3, 000) || i > maxTargets )
                {
                    if ( tracing )
                        tracingBuffer.Add("\t\t Discarding targets starting at index " + i +": " +  target.GetPlanetName_Safe() + ". Danger of this was " + danger + " and danger of the weakest target (" + weakestTarget.GetPlanetName_Safe() + ") was " + weakestDanger + ", maxTargets " + maxTargets +"\n");

                    //this is way more dangerous than earlier targets on the list, so ignore it
                    TargetList.RemoveRange(i, TargetList.Count - i);
                }
            }

            if ( tracing )
            {
                tracingBuffer.Add("\tAfter sorting the " + TargetList.Count + " preferred targets, we have\n");
                for ( int i = 0; i < TargetList.Count; i++ )
                {
                    if ( i > 20 )
                    {
                        tracingBuffer.Add("\t").Add("....\n");
                        break;
                    }
                    if ( TargetList[i].targetSquad != null )
                        tracingBuffer.Add("\t").Add(TargetList[i].targetSquad.ToStringWithPlanetAndOwner()).Add(" path difficulty ").Add(TargetList[i].dangerOfPath).Add(" target difficulty ").Add(TargetList[i].dangerOfTarget).Add("\n");
                    else
                        tracingBuffer.Add("\t").Add(TargetList[i].GetPlanetName_Safe()).Add(" path difficulty ").Add(TargetList[i].dangerOfPath).Add(" target difficulty ").Add(TargetList[i].dangerOfTarget).Add("\n");
                }
            }
            for ( int i = TargetList.Count - 1; i >= 0; i-- )
            {
                FireteamTarget target = TargetList[i];
                if ( Fireteam.IsThisAWinningBattle(faction, Context,  target.planet) )
                    TargetList.Remove(target);
                PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "FireteamUtilitySortTargets", CurrentPlanetForFireteam, target.planet, PathingMode.Default, Context, PathCacheData );
                if ( pathCache != null )
                {
                    for ( int j = 0; j < pathCache.PathToReadOnly.Count; j++ )
                    {
                        //check the path and make sure we aren't walking into anything problematic; for the warden fleet, no crossing enemy planets
                        //for everyone else, no suiciding please
                        if ( (faction.SpecialFactionData.InternalName == "AIWarden") && pathCache.PathToReadOnly[j].GetControllingFaction().GetIsHostileTowards( faction ) )
                        {
                            TargetList.Remove( target );
                            break;
                        }
                        if ( team.DefenseMode && pathCache.PathToReadOnly[j].GetControllingOrInfluencingFaction().GetIsHostileTowards( faction ) )
                        {
                            int unused = 0;
                            int danger = Fireteam.GetPlanetDefensiveStrength( pathCache.PathToReadOnly[j], faction, true, ref unused, FInt.Zero, FInt.Zero );
                            if ( danger > team.DeepInfo.TeamStrength * 2 )
                            {
                                //if we are a defensive fleet, don't fly through enemy strongholds
                                TargetList.Remove( target );
                                break;
                            }
                        }
                    }
                }
            }
        }

        public static void RemoveCompletedMissions( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            //Go through the existing Missions and see if any need to be removed due to victory
            int debugCode = 0;
            List<VassalMission> activeMissions = VassalMission.GetTemporaryVassalMissionList("RemoveCompletedMissions", 20);
            if ( activeMissions == null ) //blocked for teardown/shutdown; bail
                return;
            try{
                if ( !faction.IsVassal )
                    return; //nothing to do if we are not a vassal
                debugCode = 100;


                FactionUtilityMethods.Instance.GetActiveVassalMissions( faction, VassalMissionType.Offense, activeMissions );
                if ( activeMissions.Count == 0 )
                {
                    VassalMission.ReleaseTemporaryVassalMissionList( activeMissions );
                    return;
                }
                for ( int i = 0; i < activeMissions.Count; i++ )
                {
                    debugCode = 200;
                    VassalMission mission = activeMissions[i];
                    if ( mission == null )
                        continue;
                    GameEntity_Squad TargetEntity = mission.TargetEntity.GetSquad();
                    Planet planet = mission.Planet;
                    bool planetVictory = false;
                    bool specificTargetVictory = false;
                    if ( TargetEntity != null )
                    {
                        if ( TargetEntity.GetHasBeenDestroyed() )
                        {
                            specificTargetVictory = true;
                        }
                    }
                    else if ( Fireteam.IsThisAWinningBattle(faction, Context, planet, 10, false) )
                        planetVictory = true;

                    if ( planetVictory || specificTargetVictory )
                    {
                        debugCode = 300;
                        //We've won the battle! Remove our mission and send a message to our Leige
                        //TODO: make sure we remove missions attached to the faction
                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.AddVassalMission], GameCommandSource.AnythingElse );
                        command.RelatedBool = false; //disable this mission
                        command.RelatedIntegers.Add( mission.Planet.Index ); //which planet
                        command.RelatedIntegers2.Add( faction.FactionIndex );
                        if ( mission.OrderType != null )
                            command.RelatedString =  mission.OrderType.InternalName ;
                        World_AIW2.Instance.QueueGameCommand( faction, command, false );

                        //some variation of text here
                        string chosenText = "";
                        int random = Context.RandomToUse.Next(100);
                        if ( planetVictory )
                        {
                            if ( random < 5 )
                                chosenText = "begs to inform you that they have resolved the insignificant problem on";
                            else if ( random < 15 )
                                chosenText = "has dealt with";
                            else if ( random < 25 )
                                chosenText = "has crushed";
                            else if ( random < 45 )
                                chosenText = "has defeated";
                            else if ( random < 55 )
                                chosenText = "is victorious on";
                            else if ( random < 65 )
                                chosenText = "is triumphant on";
                            else if ( random < 75 )
                                chosenText = "declares victory on";
                            else if ( random < 85 )
                                chosenText = "now controls";
                            else if ( random < 95 )
                                chosenText = "has smitten the enemies on";
                            else
                                chosenText = "dominates";
                        }
                        else
                        {
                            if ( random < 5 )
                                chosenText = "begs to inform you that they have resolved an insignificant problem with a";
                            else if ( random < 15 )
                                chosenText = "has dealt with";
                            else if ( random < 25 )
                                chosenText = "has crushed a terrifying";
                            else if ( random < 45 )
                                chosenText = "has defeated";
                            else if ( random < 55 )
                                chosenText = "is victorious over";
                            else if ( random < 65 )
                                chosenText = "is triumphant over";
                            else if ( random < 75 )
                                chosenText = "declares victory over";
                            else if ( random < 85 )
                                chosenText = "has eliminated";
                            else if ( random < 95 )
                                chosenText = "has smitten a dreadful";
                            else
                                chosenText = "has slain a vile";
                        }
                        ArcenCharacterBuffer output = ArcenCharacterBuffer.GetFromPoolOrCreate( "FireteamUtility-RemoveCompletedMissions-output" );
                        if ( planetVictory )
                            output.Add("My leige, your vassal ").Add( faction.GetDisplayName(), faction.FactionCenterColor.ColorHexBrighter ).Add( " " + chosenText + " " ).Add( planet.Name, "a1ffa1" );
                        else
                            output.Add("My leige, your vassal ").Add( faction.GetDisplayName(), faction.FactionCenterColor.ColorHexBrighter ).Add( " " + chosenText + " " ).Add( TargetEntity.TypeData.GetDisplayName(), "a1ffa1" );

                        PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                        if ( chatHandlerOrNull != null )
                            chatHandlerOrNull.PlanetToView = planet;

                        World_AIW2.Instance.QueueChatMessageOrCommand( output.ToStringAndReturnToPool(), ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                    }
                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in RemoveCompletedMissions " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            finally
            {
               VassalMission.ReleaseTemporaryVassalMissionList( activeMissions );
            }
        }
        public static GameEntity_Squad GetRandomWardenFleetBaseIfNecessary(Fireteam team, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            GameEntity_Squad choice = null;
            if ( team.DeepInfo.ShipsInFireteam.Count == 0 )
                ArcenDebugging.ArcenDebugLogSingleLine("should be impossible?", Verbosity.DoNotShow );
            GameEntity_Squad shipFromFireteam = team.DeepInfo.ShipsInFireteam[0].GetSquad();
            if ( shipFromFireteam == null )
                return null;
            //first see if we are already at a warden fleet base
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.WardenFleetLurkLocations ) )
            {
                if ( team.DeepInfo.CurrentPlanet == entity.Planet )
                {
                    choice = entity;
                }
            }
            if ( choice != null )
            {
                return choice;
            }
            Faction faction = shipFromFireteam.GetFactionOrNull_Safe();
            int attempts = 5;
            int percentOfGivenBase = 45;
            //if we aren't at a warden fleet base, try to find a warden fleet base to wait at
            //first we choose at random; if we can't decide, pick the closest base
            GameEntity_Squad closestBase = null;
            Int16 hopsToClosest = 999;
            do {
                foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.WardenFleetLurkLocations ) )
                {
                    if ( entity.GetIsHostileTowards_Safe(faction ) )
                         continue;
                    Int16 hops = 0;
                    int danger = Fireteam.GetDangerOfPath(faction, Context, PathCacheData, team.DeepInfo.CurrentPlanet, entity.Planet, false, out hops);
                    if ( danger > team.DeepInfo.TeamStrength )
                        continue;
                    if ( closestBase == null || hops < hopsToClosest )
                    {
                        closestBase = entity;
                        hopsToClosest = hops;
                    }
                    if ( Context.RandomToUse.Next(0, 100 ) < percentOfGivenBase )
                    {
                        choice = entity;
                        break;
                    }
                }
            } while ( choice == null && attempts-- > 0);
            if ( choice == null && closestBase != null )
                return closestBase;
            return choice;
        }
        public static void DonateTeamToAlliedHunter(Fireteam team, ArcenLongTermIntermittentPlanningContext Context)
        {
            if ( team.DeepInfo.ShipsInFireteam.Count == 0 )
                return;
            Faction myfaction = team.DeepInfo.ShipsInFireteam[0].GetFactionOrNull_Safe();
            int myAlliedAIFaction = myfaction.FactionIndexOfMyParentIfIHaveOne;
            Faction hunterFaction = null;
            for ( Int16 factionIndex = 0; factionIndex < World_AIW2.Instance.Factions.Count; factionIndex++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[factionIndex];
                if ( !otherFaction.GetIsFriendlyTowards( myfaction ) )
                    continue;
                if ( !( otherFaction.SpecialFactionData.InternalName == "HunterFleet" ) )
                    continue;
                if( otherFaction.FactionIndexOfMyParentIfIHaveOne == myAlliedAIFaction )
                {
                    hunterFaction = otherFaction;
                    break;
                }
            }
            if ( hunterFaction == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("BUG: no hunter fleet found", Verbosity.DoNotShow );
                return;
            }
            GameCommand pendingTransferToHunterFleetCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransferEntitiesToFaction], GameCommandSource.AnythingElse );
            pendingTransferToHunterFleetCommand.RelatedFactionIndex = hunterFaction.FactionIndex;
            for ( int i = 0; i < team.DeepInfo.ShipsInFireteam.Count; i++ )
                pendingTransferToHunterFleetCommand.RelatedEntityIDs.Add( team.DeepInfo.ShipsInFireteam[i].PrimaryKeyID );
            World_AIW2.Instance.QueueGameCommand( hunterFaction, pendingTransferToHunterFleetCommand, false );
        }

        public static void GetNearbyAlliedPlanetsUnderAttackThatWeCanHelp( List<Planet> result, Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, 
            Planet startplanet, Fireteam team, int maxDistanceToScan, ArcenLessLinkedList<Fireteam> Teams )
        {
            result.Clear();
            if ( Context == null ) //client
                return;

            foreach ( Planet.PlanetAtHopDistance _phd in startplanet.PlanetsWithinXHops( -1,
                delegate ( Planet secondaryPlanet )
                {
                    if ( secondaryPlanet.GetControllingFaction().GetIsHostileTowards( faction ) )
                        return PropogationEvaluation.No;
                    return PropogationEvaluation.Yes;
                } ) )
            {
                Planet planet = _phd.Planet;
                Int16 Distance = _phd.Hops;
                EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( faction );
                StrengthData_PlanetFaction_Stance hostileData = myFactionData[FactionStance.Hostile];
                StrengthData_PlanetFaction_Stance selfData = myFactionData[FactionStance.Self];
                StrengthData_PlanetFaction_Stance friendlyData = myFactionData[FactionStance.Friendly];
                int hostileStrength = hostileData.TotalStrength;
                int friendlyStrength = selfData.TotalStrength + friendlyData.TotalStrength;
                if ( friendlyStrength == 0 || hostileStrength == 0 || Distance > maxDistanceToScan )
                    continue;
                if ( hostileStrength < friendlyStrength / 2 )
                    continue;

                //see if any other fireteams are also on their way to help
                int strengthEnRoute = 0;
                foreach ( Fireteam fTeam in Fireteam.LiveTeamsIn( Teams ) )
                {
                    if ( fTeam.TargetPlanet != null && fTeam.TargetPlanet == planet )
                        strengthEnRoute += fTeam.DeepInfo.TeamStrength;
                }

                if ( friendlyStrength + strengthEnRoute > hostileStrength ) //we've already sent enough help
                    continue;
                Int16 hops = 0;
                int danger = Fireteam.GetDangerOfPath( faction, Context, PathCacheData, planet, team.DeepInfo.CurrentPlanet, true, out hops );

                if ( danger > team.DeepInfo.TeamStrength )
                    continue;

                if ( planet.GetControllingOrInfluencingFaction() == faction && hostileStrength > friendlyStrength / 2 )
                {
                    result.Add( planet );
                }
            }
        }
    }    
}
