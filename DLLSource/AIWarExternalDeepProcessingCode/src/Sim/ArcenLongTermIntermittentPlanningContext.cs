using Arcen.Universal;
using System;
using System.Threading;
using System.Threading.Tasks;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// Long Term Planning contexts may run any number of frames, but must be completed before 
    /// the simulation can drop any dead ships or otherwise change data the long-term planning needs
    /// Currently these are only supported on the host.
    /// 
    /// The big thing for these is "special faction" logic, which includes logic for the AI and humans
    /// and any other faction that is running in the current game.
    /// 
    /// These are not things that are actually directly blocking the sim, per se.  They aren't part of the sim
    /// loop, anyhow.  It's possible that we have an issue in here that is causing these to hold up the sim
    /// in some way, because of the need to not remove ships or other data that these would use.
    /// But surely we thought of that...
    /// 
    /// These ones in particular are "intermittent," which means they only get run every so many frames, 
    /// and not every sub-thread gets run every time, either.
    /// </summary>
    public abstract class ArcenLongTermIntermittentPlanningContext : ArcenLongTermIntermittentPlanningContextBase
    {
        public override bool IsLongRangePlanning
        {
            get { return true; }
        }

        private static ReferenceTracker RefTracker;
        protected ArcenLongTermIntermittentPlanningContext( ArcenSimContextType contextType )
            : base( contextType )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ArcenLongTermIntermittentPlanningContext" );
            RefTracker.IncrementObjectCount();
        }

        static ArcenLongTermIntermittentPlanningContext()
        {
        }
                
        //Note: if long-range-planning is ever done on not-the-host, the sky will fall in general
        private static int CurrentParallelFactionCycle = 0;
        private static Dictionary<SpecialFactionProcessingGroup, bool> SpecialFactionProcessingGroupsRunningThisCycle = Dictionary<SpecialFactionProcessingGroup, bool>.Create_WillNeverBeGCed( 300, "ArcenLongTermIntermittentPlanningContext-SpecialFactionProcessingGroupsRunningThisCycle" );

        //Interned literals shared across factions for the !planningContext.GetNeedsToRun() skip path.
        //See Helper_CheckForRunningFactionThread_Parallel for the reason this matters.
        private const string SkipReason_DoesNotNeedToRun = "Skip Because Does Not Need To Run";
        private const string SkipReason_DoesNotNeedToRun_HasNotRunYet = "Skip Because Does Not Need To Run - Has Not Run Yet";

        public static void RunAllContexts()
        {
            bool debugging = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Thread );
            if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "ArcenLongTermIntermittentPlanningContext.RunAllContexts()", Verbosity.DoNotShow );
            if (ArcenNetworkAuthority.GetIsClientMode())
                return;
            if ( CentralVars.DEBUG_TURN_OFF_LONG_TERM_INTERMITTENT )
                return;

            ArcenThreading.RunTaskOnBackgroundThread( "_Deep.DoLongTermPlanning", false, false, () => DoLongTermPlanning() );
        }

        private static void DoLongTermPlanning()
        {
            try
            {
                if ( !World.Instance.IsPaused )
                    DoLongTermPlanning_MassivelyParallelThreads();
            }
            catch ( ArcenPleaseStopThisThreadException )
            {
                //do nothing -- these are valid out here, and will be reported on the interior if there is a problem!
                OuterThreadTeardown();
            } 
            catch ( Exception e )
            {
                OuterThreadTeardown();
                ArcenDebugging.ArcenDebugLogSingleLine( "DoLongTermPlanning IntermittentError: " + e, Verbosity.ShowAsError );
            }
            finally
            {
                OuterThreadTeardown();
            }
        }

        #region DoLongTermPlanning_MassivelyParallelThreads
        private static void DoLongTermPlanning_MassivelyParallelThreads()
        {
            try
            {
                bool mayLaunchAnyThreads = World_AIW2.Instance.GameSecond >= 5;
                bool debugging = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Thread );
                if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "ArcenLongTermIntermittentPlanningContext.DoLongTermPlanningMassivelyParalle();mayLaunchAnyThreads=" + mayLaunchAnyThreads, Verbosity.DoNotShow );

                //this will let us make sure we don't run any of the same type, as that would be Really Bad
                SpecialFactionProcessingGroupsRunningThisCycle.Clear();

                //first of all, find any that are still running and block those off
                for ( Int16 factionIndex = 0; factionIndex < World_AIW2.Instance.Factions.Count; factionIndex++ )
                {
                    Faction fac = World_AIW2.Instance.Factions[factionIndex];
                    if ( fac == null )
                        continue;
                    SpecialFactionProcessingGroup processingGroup = fac.SpecialFactionData.ProcessingGroup;
                    if ( processingGroup.IsCurrentlyWaitingOnLRPThread.IsBusy() ) //if this is still running compared to last time (very reasonable to happen), make sure we don't run it again
                        SpecialFactionProcessingGroupsRunningThisCycle[processingGroup] = true;
                }

                //now try to run any that haven't run in the current batch yet.  A batch is "all the factions except any duplicates."
                for ( Int16 factionIndex = 0; factionIndex < World_AIW2.Instance.Factions.Count; factionIndex++ )
                    ArcenLongTermIntermittentPlanningContext.Helper_CheckForRunningFactionThread_Parallel( mayLaunchAnyThreads, factionIndex );

                //nothing was able to run (Probably because it's time to increment the cycle),
                //and nothing was already running (probably because they all finished their work)!
                //Yay!  Only now is it time for the next full cycle!
                if ( SpecialFactionProcessingGroupsRunningThisCycle.Count <= 0 )
                {
                    //since nothing ran, increment the cycle and start anew
                    CurrentParallelFactionCycle++;

                    World_AIW2.Instance.CheckForActuallyGettingRidOfRemovedEntities();

                    //this time some should run, and the cycle continues
                    for ( Int16 factionIndex = 0; factionIndex < World_AIW2.Instance.Factions.Count; factionIndex++ )
                        ArcenLongTermIntermittentPlanningContext.Helper_CheckForRunningFactionThread_Parallel( mayLaunchAnyThreads, factionIndex );
                }
            }
            catch ( ArcenPleaseStopThisThreadException ) //do nothing -- these are valid out here, and will be reported on the interior if there is a problem!
            {
                OuterThreadTeardown();
            } 
            catch ( Exception e )
            {
                OuterThreadTeardown();
                ArcenDebugging.ArcenDebugLogSingleLine( "DoLongTermPlanning_MassivelyParallelThreads: " + e, Verbosity.ShowAsError );
            }
            finally
            {
                OuterThreadTeardown();
            }
        }
        #endregion

        #region InitFactionLongRangePlanningContextIfNeedBe
        public static void InitFactionLongRangePlanningContextIfNeedBe( Faction faction )
        {
            if ( faction == null )
                return;
            if ( faction.SpecialFactionData == null ) //this can happen when trying to load a good savegame after a failed one
                return;
            //faction.SpecialFactionData.ProcessingGroup.IsCurrentlyWaitingOnLRPThread = 0;

            SpecialFactionPlanning plan = null;
            if ( faction.LongRangePlanningContext == null )
            {
                plan = SpecialFactionPlanning.GetFromPoolOrCreate();
                faction.LongRangePlanningContext = plan;
            }
            else
                plan = faction.LongRangePlanningContext as SpecialFactionPlanning;

            plan.UpdateToBeForFaction( faction );
        }
        #endregion

        #region Helper_CheckForRunningFactionThread_Parallel
        private static void Helper_CheckForRunningFactionThread_Parallel( bool mayLaunchAnyThread, Int16 factionIndex )
        {
            Faction faction = World_AIW2.Instance.Factions[factionIndex];
            ExternalFactionDeepInfoRoot deepInfoRoot = faction.GetExternalDeepInfoAs<ExternalFactionDeepInfoRoot>();

            bool debugging = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Thread );
            if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "ArcenLongTermIntermittentPlanningContext.Helper_CheckForRunningFactionThread(" + mayLaunchAnyThread + "," + faction.SpecialFactionData.InternalName + ")", Verbosity.DoNotShow );
            //CurrentParallelFactionCycle
            InitFactionLongRangePlanningContextIfNeedBe( faction );
            SpecialFactionPlanning planningContext = (SpecialFactionPlanning)faction.LongRangePlanningContext;
            if ( planningContext.IsRunning && planningContext.LastRunForFactionIndex == faction.FactionIndex ) //if this is still running (very reasonable to happen), just say that
            {
                if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "\t" + "Still Running", Verbosity.DoNotShow );
                System.Threading.Interlocked.Exchange( ref faction.LastDoLongRangePlanningReason, "Still Running" );
            }
            else if ( !mayLaunchAnyThread )
            {
                if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "\t" + "skipping because !mayLaunchAnyThread", Verbosity.DoNotShow );
                System.Threading.Interlocked.Exchange( ref faction.LastDoLongRangePlanningReason, "Skip Because MayNotLaunchAny" );
            }
            else if ( faction.LastLongTermPlanningCycle == CurrentParallelFactionCycle )
            {
                if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "\t" + "skipping because faction already ran this cycle", Verbosity.DoNotShow );
                System.Threading.Interlocked.Exchange( ref faction.LastDoLongRangePlanningReason, "Skip Because Already Ran This Cycle" );
            }
            else if ( !faction.BaseInfo.HasPerSecondSimRunSinceGameLoad ) //don't run any LRP until a sim step has run!
            {                
                if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "\t" + "skipping because faction has not yet run a per-second sim step", Verbosity.DoNotShow );
                System.Threading.Interlocked.Exchange( ref faction.LastDoLongRangePlanningReason, "Skip Because Has Not Yet Run A Per-Second Sim Step" );
            }
            else if ( !planningContext.GetNeedsToRun() ) //just didn't need to run right this moment!
            {
                if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "\t" + "skipping because !planningContext.GetNeedsToRun()", Verbosity.DoNotShow );
                //Static literal reasons -- the previous form built the time-since-run into the message
                //via concat + Single.ToString every frame for every faction in this branch (7+ per frame
                //in the profile), allocating ~1.6KB of garbage just to tell the UI text that hadn't
                //meaningfully changed. The UI already displays time-since-last-run separately via
                //LastDoLongRangePlanningEndedAtUnpausedTimeSinceLastRestart_NonSim, so the embedded
                //time was redundant. ReferenceEquals guards the Exchange too -- once the reason is
                //the interned literal, subsequent calls don't touch the field at all.
                string newReason = faction.LastIntermittentLongRangePlanningStartedTime_Nonsim <= 0
                    ? SkipReason_DoesNotNeedToRun_HasNotRunYet
                    : SkipReason_DoesNotNeedToRun;
                if ( !ReferenceEquals( faction.LastDoLongRangePlanningReason, newReason ) )
                    System.Threading.Interlocked.Exchange( ref faction.LastDoLongRangePlanningReason, newReason );
            }
            else if ( World_AIW2.Instance.IsBrainDeathCurrentlyEnabled )
            {
                if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "\t" + "skipping because IsBrainDeath", Verbosity.DoNotShow );
                System.Threading.Interlocked.Exchange( ref faction.LastDoLongRangePlanningReason, "Skip Because IsBrainDeath" );
            }
            else if ( SpecialFactionProcessingGroupsRunningThisCycle.ContainsKey( faction.SpecialFactionData.ProcessingGroup ) )
            {
                //ArcenDebugging.ArcenDebugLogSingleLine( "Skip " + faction.Implementation.GetType() + " for factionindex " + faction.FactionIndex, Verbosity.DoNotShow );
                if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "\t" + "skipping because another faction of this processing group is running this cycle", Verbosity.DoNotShow );

                System.Threading.Interlocked.Exchange( ref faction.LastDoLongRangePlanningReason, "Skip Because Another Faction Active In Same Processing Group (Loc A)!" );
            }
            else
            {
                //ArcenDebugging.ArcenDebugLogSingleLine( "Faction RunOnBackgroundThread: " + faction.SpecialFactionData.InternalName + " " + faction.FactionIndex, Verbosity.DoNotShow );

                if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "\t" + "calling planningContext.Run()", Verbosity.DoNotShow );

                planningContext.UpdateToBeForFaction( faction );

                SpecialFactionData factionData = faction.SpecialFactionData;
                if ( factionData == null )
                {
                    if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "\t" + "skipping because faction.SpecialFactionData is null", Verbosity.DoNotShow );

                    System.Threading.Interlocked.Exchange( ref faction.LastDoLongRangePlanningReason, "Skip faction.SpecialFactionData null!" );
                    return;
                }
                SpecialFactionProcessingGroup processingGroup = factionData.ProcessingGroup;
                if ( processingGroup == null )
                {
                    if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "\t" + "skipping because faction.SpecialFactionData.ProcessingGroup is null", Verbosity.DoNotShow );

                    System.Threading.Interlocked.Exchange( ref faction.LastDoLongRangePlanningReason, "Skip faction.SpecialFactionData.ProcessingGroup null!" );
                    return;
                }

                //find out ON the task that this is still running, that's fine.  We want to make sure that if the task fails to start, we don't increment IsCurrentlyWaitingOnThread.
                //Byt the time that happens, we're already there.
                if ( processingGroup.IsCurrentlyWaitingOnLRPThread.IsBusy() )
                {
                    if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "\t" + "skipping because another faction of this processing group is running this cycle B", Verbosity.DoNotShow );

                    System.Threading.Interlocked.Exchange( ref faction.LastDoLongRangePlanningReason, "Skip Because Another Faction Active In Same Processing Group (Loc B)!" );
                    return;
                }
                if ( !processingGroup.IsCurrentlyWaitingOnLRPThread.DoNextOnlyIfNotAlreadyBusy( factionData.InternalName ) )
                {
                    if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "\t" + "skipping because another faction of this processing group is running this cycle C", Verbosity.DoNotShow );

                    System.Threading.Interlocked.Exchange( ref faction.LastDoLongRangePlanningReason, "Skip Because Another Faction Active In Same Processing Group (Loc C)!" );
                    return;
                }

                if ( !planningContext.RunOnBackgroundThread( faction.SpecialFactionData.ProcessingGroup.InternalName, false, processingGroup.IsCurrentlyWaitingOnLRPThread ) )
                {
                    processingGroup.IsCurrentlyWaitingOnLRPThread.MarkAsNoLongerBusy();

                    if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "\t" + "skipping because another faction of this processing group is running this cycle", Verbosity.DoNotShow );
                    System.Threading.Interlocked.Exchange( ref faction.LastDoLongRangePlanningReason, "Skip Likely was Still Running" );
                    return;
                }
                faction.LastIntermittentLongRangePlanningStartedTime_Nonsim = ArcenTime.TimeSinceStartF;
                World_AIW2.Instance.CountOfLongTermPlanningThreadsStarted++;
                faction.LastLongTermPlanningCycle = CurrentParallelFactionCycle;
                SpecialFactionProcessingGroupsRunningThisCycle[faction.SpecialFactionData.ProcessingGroup] = true;
                //ArcenDebugging.ArcenDebugLogSingleLine( "Run " + faction.Implementation.GetType() + " for factionindex " + faction.FactionIndex, Verbosity.DoNotShow );
                System.Threading.Interlocked.Exchange( ref faction.LastDoLongRangePlanningReason, "Started Run" );
            }
        }
        #endregion

        public abstract bool GetNeedsToRun();

        private static void OuterThreadTeardown()
        {
        }

        public static void ReinitializeDuringStartOrLoad()
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction faction = World_AIW2.Instance.Factions[i];
                InitFactionLongRangePlanningContextIfNeedBe( faction );
            }
        }

        #region IContextForMonitoring
        public sealed override int GetMillisecondsAfterWhichToWarn_OfLongRunning()
        {
            return this.GetSecondsAfterWhichToWarnInOneCycle() * 1000;
        }

        public sealed override float GetTimeAfterWhichToWarn_OfNotRunning()
        {
            return this.GetTimeAfterWhichToWarnOfNotRunning();
        }

        public sealed override float GetLastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective()
        {
            return this.LastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective;
        }
        #endregion
    }
}
