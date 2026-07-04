using Arcen.Universal;
using System;

using System.Diagnostics;
using System.Threading;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public class SquadStackPlanning : ArcenLongTermContinuousPlanningContext, IBetweenMapGenPoolable<SquadStackPlanning>
    {
        public override int GetSecondsToLiveEachCycle()
        {
            return 30;
        }

        public override int GetSecondsAfterWhichToWarnInOneCycle()
        {
            return 10;
        }

        public override float GetTimeAfterWhichToWarnOfNotRunning()
        {
            return 5f;
        }

        private SquadStackPlanning()
            : base( ArcenSimContextType.LongTermContinuous )
        {
        }

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            this.DoNotRunAgainUntilTime = 0;
            CleanupBase();
        }

        private static readonly BetweenMapGenPool<SquadStackPlanning> Pool = BetweenMapGenPool<SquadStackPlanning>.Create_WillNeverBeGCed( "SquadStackPlanning", 30, 10,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new SquadStackPlanning(); } );

        public static SquadStackPlanning GetFromPoolOrCreate()
        {
            SquadStackPlanning context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion

        private float DoNotRunAgainUntilTime;
        public override bool GetNeedsToRun()
        {
            return !this.IsRunning && this.DoNotRunAgainUntilTime < ArcenTime.TimeSinceStartF;
        }

        public int newUnitsStacked = 0;

        private bool StackingDebug = false;
                
        protected override void Execute()
        {
            //UnityEngine.Debug.Log( "DECOL Thread:" + Thread.CurrentThread.ManagedThreadId );
            try
            {
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                {
                    //don't let that timer get strange
                    World_AIW2.Instance.TimeOfCurrentUnitStackPlanningCycleStart = 0;
                    return;
                }
                if ( World.Instance.IsPaused )
                {
                    //don't let that timer get strange
                    World_AIW2.Instance.TimeOfCurrentUnitStackPlanningCycleStart = 0;
                    return;
                }
                if ( World_AIW2.Instance.TimeOfCurrentUnitStackPlanningCycleStart == 0 )
                    World_AIW2.Instance.TimeOfCurrentUnitStackPlanningCycleStart = ArcenTime.TimeSinceStartF;

                if ( CentralVars.DEBUG_TURN_OFF_STACK_PLAN )
                    return;

                if ( AIWar2GalaxySettingQuickAccess.ExperimentalStackingAlgorithm_Enabled )
                {
                    newUnitsStacked = 0;
                    
                    World_AIW2.Instance.TotalUnitsStacked = 0;
                    
                    var planets = Planet.GetTemporaryPlanetList("SquadStackPlanning.planets", 10.0f);
                    if ( planets == null ) //blocked for teardown/shutdown; bail
                        return;
                    var cur = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                    foreach ( Planet p in World_AIW2.Instance.CurrentGalaxy.Planets( false ) )
                    {
                                if (p == cur)
                                    continue;    
                                
                                planets.Add(p);
                    }
                    int compareTimeLastRestacked(Planet a, Planet b)
                    {
                        return a.TimeLastRestacked.CompareTo(b.TimeLastRestacked);
                    }
                    planets.StableSort(compareTimeLastRestacked);
                    
                    int loops = 0;
                    while ( planets.Count > 0)
                    {
                        var p = planets[planets.Count-1];
                        planets.RemoveAt(planets.Count-1);
                        
                        using (var data = Core.Stacking.Data.Get(p))
                            data.Restack();
                        
                        loops++;
                            
                        var elapsed = (ArcenTime.TimeSinceStartF - World_AIW2.Instance.TimeOfCurrentUnitStackPlanningCycleStart);
                        if (elapsed > 5.0)
                            break;
                    }
                    
                    newUnitsStacked = loops;
                    
                    Planet.ReleaseTemporaryPlanetList(planets);
                }
                else
                {
                    newUnitsStacked = 0;
                    #region Central Loop
                    foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                    {
                        for ( int j = 0; j < planet.Factions.Count; j++ )
                        {
                            PlanetFaction faction = planet.Factions[j];
                            this.DoCentralLoopForPlanetFaction( faction );
                            if ( newUnitsStacked > MAX_TO_STACK_AT_ONCE )
                                break;
                        }
                        if ( newUnitsStacked > MAX_TO_STACK_AT_ONCE )
                            break;
                    }
                    #endregion
                }
                
                //hey, we finished; we do that every time, actually
                {
                    World_AIW2.Instance.CurrentUnitStackPlanningCycle++;
                    float currentTime = ArcenTime.TimeSinceStartF;
                    //only set that timer if we have real data for it
                    if ( World_AIW2.Instance.TimeOfCurrentUnitStackPlanningCycleStart > 0 )
                    {
                        World_AIW2.Instance.TotalTimeOfLastUnitStackPlanningCycle = currentTime - World_AIW2.Instance.TimeOfCurrentUnitStackPlanningCycleStart;
                        if ( World_AIW2.Instance.TotalTimeOfLastUnitStackPlanningCycle < 0.01f )
                            World_AIW2.Instance.TotalTimeOfLastUnitStackPlanningCycle = 0.01f;
                    }
                    //don't directly set the timer for the next cycle, instead set it to 0 so that it will get properly set when the next timer starts if there is a gap
                    World_AIW2.Instance.TimeOfCurrentUnitStackPlanningCycleStart = 0;

                    if ( newUnitsStacked > 0 )
                        World_AIW2.Instance.TotalUnitsStacked += newUnitsStacked;

                    //try to run this every 4 seconds, or with 1 second gaps, whichever is slower
                    float timeToWaitBeforeNextStart = 4f - World_AIW2.Instance.TotalTimeOfLastUnitStackPlanningCycle;
                    if ( timeToWaitBeforeNextStart < 1f )
                        timeToWaitBeforeNextStart = 1f;
                    this.DoNotRunAgainUntilTime = ArcenTime.TimeSinceStartF + timeToWaitBeforeNextStart;
                }
            }
            catch ( ArcenPleaseStopThisThreadException ) { }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "SquadStackPlanning Exception: " + e, Verbosity.ShowAsError );
            }
        }

        private void DoCentralLoopForPlanetFaction( PlanetFaction PFaction )
        {
            if ( PFaction == null )
                return;

            //no stacking of un-owned or player objects!
            switch ( PFaction.Faction.Type )
            {
                case FactionType.NaturalObject:
                    return;
                case FactionType.Player:
                    {
                        currentPlanet = PFaction.Planet;
                        #region prepare all of the local fleets for recieving squad counts!
                        for ( int i = 0; i < currentPlanet.PlayerFleetsAtPlanet_Current.Count; i++ )
                        {
                            Fleet fleet = null;
                            try
                            {
                                fleet = currentPlanet.PlayerFleetsAtPlanet_Current[i];
                            }
                            catch { }
                            if ( fleet == null )
                                continue;
                            fleet.UnitCountForStacking.Clear();
                            switch ( fleet.Category )
                            {
                                case FleetCategory.PlayerMobile:
                                case FleetCategory.PlayerCustomCityFedMobile:
                                case FleetCategory.PlayerCustomUnattachedMobile:
                                    foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                                    {
                                        if ( mem.NonSim_PlayerOnly_UnitStacking_WorkingSquadsOrNull != null )
                                            mem.NonSim_PlayerOnly_UnitStacking_WorkingSquadsOrNull.Clear();
                                    }
                                    break;
                            }
                        }
                        #endregion

                        //tally them
                        foreach ( GameEntity_Squad Squad in PFaction.Entities.Squads() )
                            if ( TallyEntitiesForPossibleStacking_PlayerOnly( Squad ) == DelReturn.Break )
                                break;

                        //now combine any that have too many!
                        CheckForTooManyUnitsOfAType_PlayerOnly();
                    }
                    break;
                default:
                    {
                        //prepare all of the rows for recieving squad counts!
                        for ( int k = 0; k < GameEntityTypeDataTable.Instance.Rows.Count; k++ )
                            GameEntityTypeDataTable.Instance.Rows[k].NonSim_UnitStacking_WorkingSquads.Clear();

                        //tally them
                        foreach ( GameEntity_Squad Squad in PFaction.Entities.Squads() )
                            if ( TallyEntitiesForPossibleStacking_NonPlayer( Squad ) == DelReturn.Break )
                                break;

                        //now combine any that have too many!
                        CheckForTooManyUnitsOfAType_NonPlayer( PFaction );
                    }
                    break;
            }
        }

        private Planet currentPlanet = null;

        #region TallyEntitiesForPossibleStacking_NonPlayer
        private DelReturn TallyEntitiesForPossibleStacking_NonPlayer( GameEntity_Squad Squad )
        {
            if ( Squad == null )
                return DelReturn.Continue;
            if ( Squad.PrimaryKeyID == -17 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Oi!  Tried to run TallyEntitiesForPossibleStacking_NonPlayer on the FakeEntity!  How did this even get into the game?  Faction: " +
                    Squad.GetFactionDisplayNameSafe() + " typeData: " + Squad.GetTypeDisplayNameSafe() + " planet: " + Squad.GetPlanetNameSafe(), Verbosity.ShowAsError );
                return DelReturn.Continue;
            }
            //must be alive and claimed
            if ( Squad.TypeData == null || Squad.HasBeenRemovedFromSim || Squad.HasNotYetBeenFullyClaimed ||
                 Squad.SecondsSpentAsRemains > 0 || Squad.ToBeRemovedAtEndOfThisFrame )
                return DelReturn.Continue;
            //must be mobile in order to combine, or a turret
            if ( !Squad.TypeData.IsMobile && !Squad.TypeData.NPCShipCap.DefaultFor_Turrets && !Squad.TypeData.IsTurret )
                return DelReturn.Continue;
            //some types of ships are explicitly forbidden from stacking
            if ( Squad.TypeData.CannotBeStacked )
                return DelReturn.Continue;
            //can't stack units with a parent for now.
            if ( Squad.ParentGameEntity.GetSquad() != null )
                return DelReturn.Continue;
            //if ( Squad.HasBadStatusEffectForSplitting() ) //don't care about this for NPCs
            //{
            //    return DelReturn.Continue; //don't put things with bad status effects into a stack
            //}

            Squad.TypeData.NonSim_UnitStacking_WorkingSquads.Add( Squad );
            return DelReturn.Continue;
        }
        #endregion

        #region TallyEntitiesForPossibleStacking_PlayerOnly
        private DelReturn TallyEntitiesForPossibleStacking_PlayerOnly( GameEntity_Squad Squad )
        {
            if ( Squad == null )
                return DelReturn.Continue;
            if ( Squad.PrimaryKeyID == -17 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Oi!  Tried to run TallyEntitiesForPossibleStacking_PlayerOnly on the FakeEntity!  How did this even get into the game?  Faction: " +
                    Squad.GetFactionDisplayNameSafe() + " typeData: " + Squad.GetTypeDisplayNameSafe() + " planet: " + Squad.GetPlanetNameSafe(), Verbosity.ShowAsError );
                return DelReturn.Continue;
            }
            FleetMembership fleetMem = Squad.FleetMembership;
            if ( fleetMem == null )
                return DelReturn.Continue;
            Fleet fleet = Squad.FleetMembership.Fleet;
            if ( fleet == null )
                return DelReturn.Continue;
            //if this type doesn't ever get stacked for players, then stop thinking about it
            if ( !fleetMem.GetShouldPlayerShipsConsiderBeingStacked() )
                return DelReturn.Continue;
            //must be part of this planet
            if ( currentPlanet == null || Squad.Planet != currentPlanet )
                return DelReturn.Continue;
            //must be alive and claimed
            if ( Squad.TypeData == null || Squad.HasBeenRemovedFromSim || Squad.HasNotYetBeenFullyClaimed ||
                 Squad.SecondsSpentAsRemains > 0 || Squad.ToBeRemovedAtEndOfThisFrame )
                return DelReturn.Continue;
            //can't stack units with a parent for now.
            if ( Squad.ParentGameEntity.GetSquad() != null )
                return DelReturn.Continue;

            FleetMembership squadtMem = Squad.FleetMembership;
            if ( squadtMem != null )
            {
                if ( squadtMem.NonSim_PlayerOnly_UnitStacking_WorkingSquadsOrNull == null )
                    squadtMem.NonSim_PlayerOnly_UnitStacking_WorkingSquadsOrNull = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "Squad-NonSim_PlayerOnly_UnitStacking_WorkingSquadsOrNull" );
            }

            Squad.FleetMembership.NonSim_PlayerOnly_UnitStacking_WorkingSquadsOrNull.Add( Squad );
            fleet.UnitCountForStacking[Squad.TypeData]++;
            return DelReturn.Continue;
        }
        #endregion

        private const int MAX_TO_STACK_AT_ONCE = 500;

        #region CheckForTooManyUnitsOfAType_NonPlayer
        private void CheckForTooManyUnitsOfAType_NonPlayer( PlanetFaction PFaction )
        {
            int[] npcShipCapsByType = null;
            int[] npcShipCountsByCapType = null;

            if ( PFaction != null )
            {
                Faction fac = PFaction.Faction;
                if ( fac != null )
                {
                    SpecialFactionData specialFac = fac.SpecialFactionData;
                    if ( specialFac != null )
                    {
                        npcShipCapsByType = specialFac.NPCShipCapsByType;
                        npcShipCountsByCapType = fac.NPCShipCountsByCapType;
                    }
                }
            }

            int globalMax = AIWar2GalaxySettingQuickAccess.StackingCutoffNPCs;
            GameEntityTypeData typeData;
            List<SafeSquadWrapper> squads;
            for ( int k = 0; k < GameEntityTypeDataTable.Instance.Rows.Count; k++ )
            {
                typeData = GameEntityTypeDataTable.Instance.Rows[k];
                squads = typeData.NonSim_UnitStacking_WorkingSquads;
                if ( squads.Count <= 0 )
                    continue;
                int effectiveMax = globalMax;
                if ( typeData.MaxCountPerNPCFactionBeforeStack > 1 )
                    effectiveMax = typeData.MaxCountPerNPCFactionBeforeStack;

                int capByNPCCapType = npcShipCapsByType == null || typeData.NPCShipCap == null ? 0 : npcShipCapsByType[typeData.NPCShipCap.RowIndexNonSim];
                int currentCountByNPCCapType = npcShipCountsByCapType == null || typeData.NPCShipCap == null ? 0 : npcShipCountsByCapType[typeData.NPCShipCap.RowIndexNonSim];

                if ( squads.Count > 3 ) //don't do NPC-count-overage check counts when the squads are too small in number
                {
                    if ( capByNPCCapType > 0 && currentCountByNPCCapType > capByNPCCapType - 10 )
                    {
                        int newEffectiveMax = squads.Count * 3 / 4;
                        if ( newEffectiveMax < 2 )
                            newEffectiveMax = 2;
                        this.DoStackCombine_NonPlayer( newEffectiveMax, squads );
                        continue;
                    }
                }

                if ( squads.Count <= effectiveMax )
                {
                    //if we have at most half the max, then split the largest stack out into two stacks
                    if ( squads.Count <= effectiveMax/2 &&
                        currentCountByNPCCapType <= currentCountByNPCCapType/2 //AND if we have less than half of the overall ship cap for this NPC type, then do that
                        //we don't want splitting of stacks to happen too easily.
                        )
                    {
                        if ( StackingDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\twe have " + squads.Count + " squads of " + typeData.GetDisplayName() + " and effective max is " + effectiveMax, Verbosity.DoNotShow );
                        this.TryToSplitLargestStack( squads );
                    }
                    continue;
                }

                this.TryToSplitOutlierSquads( squads );

                //uh-oh, we need to stack!
                this.DoStackCombine_NonPlayer( effectiveMax, squads );
                if ( newUnitsStacked > MAX_TO_STACK_AT_ONCE )
                    return;
            }
        }
        #endregion

        #region CheckForTooManyUnitsOfAType_PlayerOnly
        private void CheckForTooManyUnitsOfAType_PlayerOnly()
        {
            if ( currentPlanet == null )
                return;
            int effectiveMax = AIWar2GalaxySettingQuickAccess.StackingCutoffPlayers;
            List<SafeSquadWrapper> squads;
            for ( int i = 0; i < currentPlanet.PlayerFleetsAtPlanet_Current.Count; i++ )
            {
                Fleet fleet = null;
                try
                {
                    fleet = currentPlanet.PlayerFleetsAtPlanet_Current[i];
                }
                catch { }
                if ( fleet == null )
                    continue;
                switch ( fleet.Category )
                {
                    case FleetCategory.PlayerMobile:
                    case FleetCategory.PlayerCustomCityFedMobile:
                    case FleetCategory.PlayerCustomUnattachedMobile:
                        {
                            bool iAmDone = false;
                            foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                            {
                                if ( mem.NonSim_PlayerOnly_UnitStacking_WorkingSquadsOrNull != null )
                                {
                                    squads = mem.NonSim_PlayerOnly_UnitStacking_WorkingSquadsOrNull;
                                    if ( squads.Count <= 0 )
                                        continue;

                                    //Note: some fleet type (necromancer, dyson sidekick)
                                    //have multiple FleetMemberships with the same unit type;
                                    //having 10 memberships of 10 squads each puts us over the stacking limit, but won't trigger
                                    //any of the stacking mechanisms. So now we check the total number of ships of that type in the fleet
                                    //as well as each fleetmembership specifically
                                    if ( squads.Count <= effectiveMax &&
                                         fleet.UnitCountForStacking[mem.TypeData] <= effectiveMax )
                                    {
                                        //if we have at most half the max, then split the largest stack out into two stacks
                                        if ( squads.Count <= effectiveMax / 2 &&
                                             fleet.UnitCountForStacking[mem.TypeData] <= effectiveMax / 2 )
                                            this.TryToSplitLargestStack( squads );
                                        continue;
                                    }

                                    this.TryToSplitOutlierSquads( squads );

                                    //uh-oh, we need to stack!
                                    this.DoStackCombine_PlayerOnly( effectiveMax, squads, fleet.UnitCountForStacking[mem.TypeData] );
                                    if ( newUnitsStacked > MAX_TO_STACK_AT_ONCE )
                                    {
                                        iAmDone = true;
                                        break;
                                    }
                                }
                            }
                            if ( iAmDone )
                                return;
                        }
                        break;
                }
                
            }
        }
        #endregion

        #region DoStackCombine_PlayerOnly
        private void DoStackCombine_PlayerOnly( int effectiveMax, List<SafeSquadWrapper> squads, int totalShipsOfType )
        {
            int numberToStack = squads.Count - effectiveMax;
            if ( numberToStack < 0 && totalShipsOfType > squads.Count )
            {
                numberToStack = squads.Count / 2;
            }
            ArcenArrays.Randomize( squads, this.RandomForHostOnly );
            ArcenArrays.Randomize( squads, this.RandomForHostOnly );
            ArcenArrays.Randomize( squads, this.RandomForHostOnly );

            // If we have 110 stacks, and we want to combine them
            // some of them so we have less than 100, we need to
            // combine 11 of them into a new stack. We will have
            // 99 untouched stacks, plus the new stack.
            int numberOfStacks = numberToStack / 4;
            if ( numberOfStacks < 1 )
                numberOfStacks = 1;
            int numberPerStack = numberToStack / numberOfStacks + 1;
            if ( numberPerStack < 2 )
                numberPerStack = 2;

            numberToStack += numberOfStacks;

            //UnityEngine.Debug.Log( squads[0].GetPlanetName_Safe() + " Try to stack: " + typeData.DisplayName + " " + squads.Count + " numberToStack: " + numberToStack +
            //    " numberPerStack: " + numberPerStack );
            if ( StackingDebug && squads[0].Planet.Index == Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed() )
                ArcenDebugging.ArcenDebugLogSingleLine( squads[0].GetPlanetName_Safe() + " Try to stack: " + squads[0].TypeData.InternalName + " " + squads.Count + " numberToStack: " + numberToStack + " numberPerStack: " + numberPerStack + " on " + squads[0].GetPlanetName_Safe(), Verbosity.DoNotShow );
            int numberRemainingOverall = numberToStack;
            this.StackBySquadType_PlayerOnly( squads, ref numberRemainingOverall, numberPerStack );

        }

        private readonly List<int> plannedStackIDs_Player = List<int>.Create_WillNeverBeGCed( 300, "SquadStackPlanning-plannedStackIDs_Player" );
        private void StackBySquadType_PlayerOnly( List<SafeSquadWrapper> squads, ref int numberRemainingOverall, int numberPerStack )
        {
            int numberRemainingThisStack = numberPerStack;
            GameEntity_Squad squad;
            int incrementAmount = 1;
            int lastK = -1;
            bool hadAnyPotentials = false;
            plannedStackIDs_Player.Clear();
            //int timeSinceLastTakenDamageForStack = 20; //don't let units in the middle of a battle start stacking
            //k is "how many units in a stack are we considering"
            for ( int k = 0; k < 100000; k += incrementAmount )
            {
                hadAnyPotentials = false;
                if ( StackingDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Iteration for " + squads[0].TypeData.InternalName + " k " + k, Verbosity.DoNotShow );
                for ( int i = 0; i < squads.Count; i++ )
                {
                    if ( numberRemainingOverall <= 0 )
                        break;//done!
                    squad = squads[i].GetSquad();
                    if ( squad == null )
                        continue;
                    if ( squad.ExtraStackedSquadsInThis <= lastK )
                    {    
                        if ( StackingDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Ignoring a squad of " + squad.TypeData.InternalName + " because extra squads " + squad.ExtraStackedSquadsInThis + " lastK " + lastK + " A", Verbosity.DoNotShow );
                        continue; //already looked at it
                    }
                    hadAnyPotentials = true;
                    if ( squad.ExtraStackedSquadsInThis > k )
                    {
                        if ( StackingDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Ignoring a squad of " + squad.TypeData.InternalName + " because extra squads " + squad.ExtraStackedSquadsInThis + " > " + k + " B", Verbosity.DoNotShow );
                        continue; //have not yet looked at it
                    }
                    // if ( World_AIW2.Instance.GameSecond - squad.LastTimeTakenDamageFromBeingShotByAnyone < timeSinceLastTakenDamageForStack)
                    // {
                    //     ArcenDebugging.ArcenDebugLogSingleLine("Ignoring a squad of " + squad.TypeData.InternalName + " with extra squads " + squad.ExtraStackedSquadsInThis + " because it has recently taken damage", Verbosity.DoNotShow );
                    //     continue; //don't let things joing stacks in the middle of a battle, it looks weird
                    // }
                    if ( StackingDebug && squads[0].Planet.Index == Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed() )
                    {
                        if ( StackingDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Choosing to add " + squad.TypeData.InternalName + " with stacks " + squad.ExtraStackedSquadsInThis + " to a new stack. k " + k + " lastK " + lastK, Verbosity.DoNotShow );
                    }
                    plannedStackIDs_Player.Add( squad.PrimaryKeyID );
                    numberRemainingOverall--;
                    numberRemainingThisStack--;

                    if ( numberRemainingThisStack <= 0 &&
                        //don't leave an extra sitting around!
                        numberRemainingOverall > 1 &&
                        //don't send just a single unit to combine with itself!
                        plannedStackIDs_Player.Count >= 2 )
                    {
                        GameCommand cmdStackUnits = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.StackUnits], GameCommandSource.AnythingElse );
                        cmdStackUnits.RelatedEntityIDs.AddRange( plannedStackIDs_Player );
                        cmdStackUnits.ToBeQueued = false;
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetNaturalObjectFactionNeverNull(), cmdStackUnits, false );
                        newUnitsStacked += cmdStackUnits.RelatedEntityIDs.Count;
                        plannedStackIDs_Player.Clear();
                        //start the next stack, if there is to be one
                        if ( numberRemainingOverall > 0 )
                            numberRemainingThisStack = numberPerStack;
                    }
                }
                if ( !hadAnyPotentials )
                    break;
                if ( k >= 2000 )
                    incrementAmount = 100000;
                else if ( k >= 500 )
                    incrementAmount = 500;
                else if ( k >= 200 )
                    incrementAmount = 100;
                else if ( k >= 100 )
                    incrementAmount = 50;
                else if ( k >= 60 )
                    incrementAmount = 20;
                else if ( k >= 40 )
                    incrementAmount = 15;
                else if ( k >= 20 )
                    incrementAmount = 10;
                else if ( k >= 10 )
                    incrementAmount = 8;
                else if ( k >= 5 )
                    incrementAmount = 5;
                lastK = k;
            }

            if ( plannedStackIDs_Player.Count >= 2 )
            {
                GameCommand cmdStackUnits = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.StackUnits], GameCommandSource.AnythingElse );
                cmdStackUnits.RelatedEntityIDs.AddRange( plannedStackIDs_Player );
                cmdStackUnits.ToBeQueued = false;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetNaturalObjectFactionNeverNull(), cmdStackUnits, false );
                newUnitsStacked += cmdStackUnits.RelatedEntityIDs.Count;
                plannedStackIDs_Player.Clear();
            }
        }
        #endregion

        #region DoStackCombine_NonPlayer
        private void DoStackCombine_NonPlayer( int effectiveMax, List<SafeSquadWrapper> squads )
        {
            int numberToStack = squads.Count - effectiveMax;
            ArcenArrays.Randomize( squads, this.RandomForHostOnly );
            ArcenArrays.Randomize( squads, this.RandomForHostOnly );
            ArcenArrays.Randomize( squads, this.RandomForHostOnly );

            // If we have 110 stacks, and we want to combine them
            // some of them so we have less than 100, we need to
            // combine 11 of them into a new stack. We will have
            // 99 untouched stacks, plus the new stack.
            int numberOfStacks = numberToStack / 10;
            if ( numberOfStacks < 1 )
                numberOfStacks = 1;
            int numberPerStack = numberToStack / numberOfStacks + 1;
            if ( numberPerStack < 2 )
                numberPerStack = 2;

            numberToStack += numberOfStacks;

            //UnityEngine.Debug.Log( squads[0].GetPlanetName_Safe() + " Try to stack: " + typeData.DisplayName + " " + squads.Count + " numberToStack: " + numberToStack +
            //    " numberPerStack: " + numberPerStack );
            if ( StackingDebug && squads[0].Planet.Index == Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed())
                ArcenDebugging.ArcenDebugLogSingleLine(squads[0].GetPlanetName_Safe() + " Try to stack: " + squads[0].TypeData.InternalName + " " + squads.Count + " numberToStack: " + numberToStack + " numberPerStack: " + numberPerStack + " on " + squads[0].GetPlanetName_Safe(), Verbosity.DoNotShow );
            int numberRemainingOverall = numberToStack;
            for ( byte i = 0; i <= 7; i++ )
            {
                this.StackByMarkLevel_NonPlayer( i, squads, ref numberRemainingOverall, numberPerStack, false );
                if ( numberRemainingOverall <= 0 )
                    break;
            }

            if ( numberRemainingOverall > 6 ) //only do this if there's still a fair number to stack
            {
                //for NPCs, allow stacking them into bad status effects if there's nothing better
                for ( byte i = 0; i <= 7; i++ )
                {
                    this.StackByMarkLevel_NonPlayer( i, squads, ref numberRemainingOverall, numberPerStack, true );
                    if ( numberRemainingOverall <= 0 )
                        break;
                }
            }
        }

        private readonly List<int> plannedStackIDs_NPC = List<int>.Create_WillNeverBeGCed( 300, "SquadStackPlanning-plannedStackIDs_NPC" );
        private void StackByMarkLevel_NonPlayer( byte MarkLevel, List<SafeSquadWrapper> squads, ref int numberRemainingOverall, int numberPerStack, 
            bool AllowBadStatusEffectsToCombine )
        {            
            int numberRemainingThisStack = numberPerStack;
            GameEntity_Squad squad;
            int incrementAmount = 1;
            int lastK = -1;
            bool hadAnyPotentials = false;

            plannedStackIDs_NPC.Clear();
            //int timeSinceLastTakenDamageForStack = 20; //don't let units in the middle of a battle start stacking
            //k is "how many units in a stack are we considering"
            for ( int k = 0; k < 100000; k += incrementAmount )
            {
                int stackingId = -1; //used to limit how ships for a minor faction are stacking

                hadAnyPotentials = false;
                if ( StackingDebug)
                    ArcenDebugging.ArcenDebugLogSingleLine("Iteration for " + squads[0].TypeData.InternalName + " k " + k + " Mark Level " + MarkLevel + " total ships regardless of mark level " + squads.Count, Verbosity.DoNotShow );
                int totalSeen = 0;
                for ( int i = 0; i < squads.Count; i++ )
                {
                    if ( numberRemainingOverall <= 0 )
                        break;//done!
                    squad = squads[i].GetSquad();
                    if ( squad == null )
                        continue;
                    if ( squad.CurrentMarkLevel != MarkLevel )
                        continue; //must match mark level!
                    totalSeen++;
                    if ( squad.ExtraStackedSquadsInThis <= lastK )
                    {
                        if(StackingDebug)
                            ArcenDebugging.ArcenDebugLogSingleLine("Ignoring " + squad.ToStringWithPlanet() + " because extra squads " + squad.ExtraStackedSquadsInThis + " lastK " + lastK + " A", Verbosity.DoNotShow );
                        continue; //already looked at it
                    }
                    hadAnyPotentials = true;
                    if ( squad.ExtraStackedSquadsInThis > k )
                    {
                        if(StackingDebug)
                            ArcenDebugging.ArcenDebugLogSingleLine("Ignoring " + squad.ToStringWithPlanet() + " because extra squads " + squad.ExtraStackedSquadsInThis + " > " + k + " B", Verbosity.DoNotShow );
                        continue; //have not yet looked at it
                    }
                    if ( !AllowBadStatusEffectsToCombine && squad.HasBadStatusEffectForSplitting() )
                    {
                        if(StackingDebug)
                            ArcenDebugging.ArcenDebugLogSingleLine("Ignoring " + squad.ToStringWithPlanet() + " with extra squads " + squad.ExtraStackedSquadsInThis + " because it has a bad status effect. D", Verbosity.DoNotShow );
                        continue; //don't put things with bad status effects into a stack
                    }
                    if ( plannedStackIDs_NPC.Count >= 1 &&
                         stackingId != squad.MinorFactionStackingID )
                    {
                        if(StackingDebug)
                            ArcenDebugging.ArcenDebugLogSingleLine("Ignoring " + squad.ToStringWithPlanet() + " with extra squads " + squad.ExtraStackedSquadsInThis + " because it has stacking id " + squad.MinorFactionStackingID + " and the rest of the list is " + stackingId, Verbosity.DoNotShow );
                        continue; //minor faction stacking ids must match (for stuff like Nanocaust
                    }
                    //units with bad status effects are now filtered out before they get here

                    // if ( World_AIW2.Instance.GameSecond - squad.LastTimeTakenDamageFromBeingShotByAnyone < timeSinceLastTakenDamageForStack)
                    // {
                    //     ArcenDebugging.ArcenDebugLogSingleLine("Ignoring " + squad.ToStringWithPlanet() + " with extra squads " + squad.ExtraStackedSquadsInThis + " because it has recently taken damage", Verbosity.DoNotShow );
                    //     continue; //don't let things joing stacks in the middle of a battle, it looks weird
                    // }
                    if ( StackingDebug && squads[0].Planet.Index == Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed())
                    {
                        if(StackingDebug)
                            ArcenDebugging.ArcenDebugLogSingleLine("Choosing to add " + squad.ToStringWithPlanet() + " with stacks " + squad.ExtraStackedSquadsInThis + " to a new stack. k " + k + " lastK " + lastK + ". squad.MinorFactionStackingID " + squad.MinorFactionStackingID + " stack id " + stackingId + ". There are currently " + plannedStackIDs_NPC.Count + " ships", Verbosity.DoNotShow );
                    }

                    if ( stackingId != -1 && stackingId != squad.MinorFactionStackingID )
                        throw new Exception ("Squad Stack minor faction mismatch for " + squad.ToStringWithPlanetAndOwner() + ". ID for the to-be-generated stack  " + stackingId + " != id of squad " + squad.MinorFactionStackingID + ". So far there were " + plannedStackIDs_NPC.Count + " units"); //this shouldn't happen due to the above check, but paranoia is good for new code

                    plannedStackIDs_NPC.Add( squad.PrimaryKeyID );

                    if ( squad.MinorFactionStackingID != -1 )
                        stackingId = squad.MinorFactionStackingID;

                    numberRemainingOverall--;
                    numberRemainingThisStack--;

                    if ( numberRemainingThisStack <= 0 &&
                        //don't leave an extra sitting around!
                        numberRemainingOverall > 1 &&
                        //don't send just a single unit to combine with itself!
                        plannedStackIDs_NPC.Count >= 2 )
                    {
                        GameCommand cmdStackUnits = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.StackUnits], GameCommandSource.AnythingElse );
                        cmdStackUnits.RelatedEntityIDs.AddRange( plannedStackIDs_NPC );
                        cmdStackUnits.ToBeQueued = false;
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetNaturalObjectFactionNeverNull(), cmdStackUnits, false );
                        newUnitsStacked += cmdStackUnits.RelatedEntityIDs.Count;
                        plannedStackIDs_NPC.Clear();
                        stackingId = -1; //we've finished this command; reset the stack ID for the next command
                        //start the next stack, if there is to be one
                        if ( numberRemainingOverall > 0 )
                            numberRemainingThisStack = numberPerStack;
                    }
                }
                if ( StackingDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine("There were " + totalSeen + " total ships seen in this step", Verbosity.DoNotShow );
                if ( !hadAnyPotentials )
                    break;
                if ( k >= 2000 )
                    incrementAmount = 100000;
                else if ( k >= 500 )
                    incrementAmount = 500;
                else if ( k >= 200 )
                    incrementAmount = 100;
                else if ( k >= 100 )
                    incrementAmount = 50;
                else if ( k >= 60 )
                    incrementAmount = 20;
                else if ( k >= 20 )
                    incrementAmount = 10;
                else if ( k >= 5 )
                    incrementAmount = 5;
                lastK = k;
            }

            if ( plannedStackIDs_NPC.Count >= 2 )
            {
                GameCommand cmdStackUnits = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.StackUnits], GameCommandSource.AnythingElse );
                cmdStackUnits.RelatedEntityIDs.AddRange( plannedStackIDs_NPC );
                cmdStackUnits.ToBeQueued = false;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetNaturalObjectFactionNeverNull(), cmdStackUnits, false );
                newUnitsStacked += cmdStackUnits.RelatedEntityIDs.Count;
                plannedStackIDs_NPC.Clear();
            }
        }
        #endregion

        #region TryToSplitOutlierSquads
        private void TryToSplitOutlierSquads( List<SafeSquadWrapper> squads )
        {
            //If some squad has way more stacks in it than other squads, split it.
            for ( byte i = 1; i <= 7; i++ )
            {
                int totalStacks = 0;
                int squadsCounted = 0;
                GameEntity_Squad squad;
                int mostRecentGameSecond = 0;
                for ( int j = 0; j < squads.Count; j++ )
                {
                    squad = squads[j].GetSquad();
                    if ( squad == null )
                        continue;
                    if ( squad.CurrentMarkLevel != i )
                        continue;
                    squadsCounted++;
                    totalStacks += squad.ExtraStackedSquadsInThis;
                    if ( squad.GameSecondEnteredThisPlanet > mostRecentGameSecond )
                        mostRecentGameSecond = squad.GameSecondEnteredThisPlanet;
                }
                if ( totalStacks <= 5 )
                    continue; //don't bother if we have only a few stacks

                if ( mostRecentGameSecond >= World_AIW2.Instance.GameSecond - 10 )
                    continue; //if any of the ships of this type and mark level have been on the planet for less than 10 seconds, don't try to split

                if ( StackingDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine("There were " + squadsCounted + " mark " + i + " " + squads[0].TypeData.GetDisplayName() +  " out of a  total of " + squads.Count, Verbosity.DoNotShow );
                int averageStacks = totalStacks/squadsCounted;
                int amountAboveAverageAllowed = averageStacks + 20;
                for ( int j = 0; j < squads.Count; j++ )
                {
                    squad = squads[j].GetSquad();
                    if ( squad == null )
                        continue;
                    if ( squad.CurrentMarkLevel == i &&
                         squad.ExtraStackedSquadsInThis > averageStacks + amountAboveAverageAllowed )
                    {
                        if ( StackingDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tSplitting " + squad.ToString() + " mark " + squad.CurrentMarkLevel + " since it is an outlier (" + squad.ExtraStackedSquadsInThis + " > " + averageStacks +"). There were " + squadsCounted + " squads counted", Verbosity.DoNotShow );
                        squad.SplitStack_ReturnNewSquadOrNullIfNoneSplit( Engine_AIW2.Instance.MainThreadContext_ClientOrHost.GetHostOnlyContext(), (1 + squad.ExtraStackedSquadsInThis) / 2 );
                    }
                }
            }
            
        }
        #endregion
        
        #region TryToSplitLargestStack
        private void TryToSplitLargestStack( List<SafeSquadWrapper> squads )
        {
            GameEntity_Squad bestSquad = null;
            GameEntity_Squad squad;
            for ( int i = 0; i < squads.Count; i++ )
            {
                squad = squads[i].GetSquad();
                if ( squad == null )
                    continue;
                if ( squad.ExtraStackedSquadsInThis > 0 )
                {
                    if ( bestSquad == null )
                        bestSquad = squad;
                    else if ( squad.ExtraStackedSquadsInThis > bestSquad.ExtraStackedSquadsInThis )
                        bestSquad = squad;
                }
            }
            if ( bestSquad != null && bestSquad.ExtraStackedSquadsInThis > 0 )
            {
                if ( StackingDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine("Splitting " + bestSquad.ToString() + " since its the largest stack (" + bestSquad.ExtraStackedSquadsInThis + " stacked)", Verbosity.DoNotShow );
                bestSquad.SplitStack_ReturnNewSquadOrNullIfNoneSplit( Engine_AIW2.Instance.MainThreadContext_ClientOrHost.GetHostOnlyContext(), (1 + bestSquad.ExtraStackedSquadsInThis) / 2 );
            }
        }
        #endregion
    }
}
