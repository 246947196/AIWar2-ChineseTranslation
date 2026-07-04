using Arcen.Universal;
using System;
using System.Diagnostics;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public static class ArcenShortTermPlanningManager
    {
        public static readonly List<ArcenShortTermPlanningContext> AllContexts = List<ArcenShortTermPlanningContext>.Create_WillNeverBeGCed( 250, "ArcenShortTermPlanningContext-AllContexts" );

        private static readonly Stopwatch LastOuterFramePlanningStopwatch = new Stopwatch();

        //only track these when we have the window open to track them
        public static float SubTimeTrackers_LastTimeAskedToTrack = -500;

        private static ReferenceTracker RefTracker;

        private static int CurrentIndex = 0;

        static ArcenShortTermPlanningManager()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ArcenShortTermPlanningManager" );
            RefTracker.IncrementObjectCount();
        }

        public static Action CallMeToInitializeIfPossible = null;

        public static void CleanupAll()
        {
            //we can just clear these, no big deal, because they automatically got pulled back into the pool
            //we don't have to actually make them go back into the pool ourselves, or need a protected list, or any of that.
            AllContexts.Clear();

            SubTimeTrackers_LastTimeAskedToTrack = -500;
            IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();

            RunAllContextsOnBackgroundThreadsFromMainSimThread_Attempts = 0;
            RunAllContextsOnBackgroundThreadsFromMainSimThread_Starts = 0;
            RunAllContextsOnBackgroundThreadsFromMainSimThread_Finishes = 0;
        }

        private static void InitializeIfNeeded()
        {
            lock ( AllContexts )
            {
                if ( AllContexts.Count > 0 )
                    return;

                if ( CallMeToInitializeIfPossible != null )
                {
                    CallMeToInitializeIfPossible();

                    //did we fill it?  If so, don't fill it again.  This was some total conversion overriding us, probably
                    if ( AllContexts.Count > 0 )
                        return;
                }

                AllContexts.Add( MovementPlanning.GetFromPoolOrCreate() );
                AllContexts.Add( MetalFlowPlanning_Player.GetFromPoolOrCreate() );
                AllContexts.Add( MetalFlowPlanning_NPC.GetFromPoolOrCreate() );
                AllContexts.Add( ProtectionPlanning.GetFromPoolOrCreate() );
                AllContexts.Add( TachyonPlanning.GetFromPoolOrCreate() );
                AllContexts.Add( TractorPlanning.GetFromPoolOrCreate() );
                AllContexts.Add( GravityPlanning.GetFromPoolOrCreate() );
                AllContexts.Add( AreaBoostPlanning.GetFromPoolOrCreate() );
            }
        }

        private static ThreadingExchanger IsCurrentlyWaitingOnThread = new ThreadingExchanger( "ArcenShortTermPlanningManager", 30f );

        public static int RunAllContextsOnBackgroundThreadsFromMainSimThread_Attempts = 0;
        public static int RunAllContextsOnBackgroundThreadsFromMainSimThread_Starts = 0;
        public static int RunAllContextsOnBackgroundThreadsFromMainSimThread_Finishes = 0;

        public static bool GetHasFinishedAllShortTermContexts()
        {
            return !IsCurrentlyWaitingOnThread.IsBusy();
        }

        public static void RunAllContextsOnBackgroundThreadsFromMainSimThread()
        {
            Interlocked.Increment( ref RunAllContextsOnBackgroundThreadsFromMainSimThread_Attempts );

            if (AllContexts.Count > 0)
                CurrentIndex = World_AIW2.Instance.Network_CurrentFrameNumber % AllContexts.Count;

            if ( IsCurrentlyWaitingOnThread.IsBusy() )
            {
                //ArcenDebugging.ArcenDebugLogSingleLine( "Tried to ArcenShortTermPlanningManager.RunAllContextsOnBackgroundThreadsFromMainSimThread, but it was already running!", Verbosity.ShowAsError );
                return; //don't complain, just skip it
            }
            
            if ( !IsCurrentlyWaitingOnThread.DoNextOnlyIfNotAlreadyBusy( string.Empty ) )
                return;
            Interlocked.Increment( ref RunAllContextsOnBackgroundThreadsFromMainSimThread_Starts );

            bool shortTermPlanningDebugLog = false;
            bool coarseProcessing = false;
            try
            {
                shortTermPlanningDebugLog = GameSettings.Current.GetBoolBySetting( "ShortTermPlanningDebugLog" );

                bool threadDebugLog = GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.ThreadDebugLog );
                if ( threadDebugLog )
                    ArcenDebugging.ArcenDebugLogSingleLine( "START Thread DoShortTermPlanning", Verbosity.DoNotShow );

                coarseProcessing = AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "CoarseShortTermPlanning" );

                LastOuterFramePlanningStopwatch.Reset();
                LastOuterFramePlanningStopwatch.Start();

                int frameWhenStarted = Engine_Universal.GlobalUniqueFrameNumber;

                InitializeIfNeeded();
                
                //This one we kick off on a BG thread to get some extra speed, and while the current order checks are happening,
                //a lot of this can happen at the same time and likely be pretty ready for the larger parts of the sim below.
                ArcenThreading.RunTaskOnBackgroundThread( "_Sim.DoShipDisabledChecks", false, false, DoShipDisabledChecks_OnBGThread );
                
                //this has to be done before any of the other bits happen, unfortunately.
                //there's not much way to make this faster than it is.
                DoShipCurrentOrderChecks();

                //ArcenDebugging.ArcenDebugLogSingleLine( "Start Task Group: TID" + Thread.CurrentThread.ManagedThreadId +
                //    " Fr: " + Engine_Universal.GlobalUniqueFrameNumber, Verbosity.DoNotShow );

                //These get started as soon as we hit them.
                int index = 0;
                foreach ( ArcenShortTermPlanningContext context in AllContexts )
                {
                    context.myStopwatch.Reset();
                    context.myStopwatch.Start();

                    // new plan
                    // instead of only running some planning contexts per step
                    // if coarse processing is enabled just don't block simstep
                    // advancement (done elsewhere)
                    //if (!coarseProcessing || index == CurrentIndex || context is MovementPlanning)
                    {
                        bool wasStarted = ArcenThreading.RunTaskOnBackgroundThread( context.NameForDisplay, false, false, 
                            () => 
                            {
                                RunAnExecutionPair_AsABackgroundTask( context, shortTermPlanningDebugLog );
                            });
                        
                        if (!wasStarted)
                        {
                            if (Core.Trace.IsOn(ArcenTracingFlags.PlanPerf))
                                LOG.Msg("STP task {0} was still running", context.NameForDisplay);
                        }
                    }

                    index++;
                }

                //ArcenDebugging.ArcenDebugLogSingleLine( "Completed Task Group: TID" + Thread.CurrentThread.ManagedThreadId +
                //    " Fr: " + Engine_Universal.GlobalUniqueFrameNumber, Verbosity.DoNotShow );
                Interlocked.Increment( ref RunAllContextsOnBackgroundThreadsFromMainSimThread_Finishes );
                Teardown( shortTermPlanningDebugLog );
            }
            catch ( ArcenPleaseStopThisThreadException )
            {
                //do nothing -- these are valid out here, and will be reported on the interior if there is a problem!
                Teardown( shortTermPlanningDebugLog );
            }
            catch ( Exception e )
            {
                Teardown( shortTermPlanningDebugLog );
                ArcenDebugging.ArcenDebugLogSingleLine( "ArcenShortTermPlanningManager.RunAllContextsOnBackgroundThreadsFromMainSimThread Error: " + e, Verbosity.ShowAsError );
            }
            finally
            {
                LastOuterFramePlanningStopwatch.Stop();

                if ( shortTermPlanningDebugLog )
                {
                    if ( LastOuterFramePlanningStopwatch.ElapsedMilliseconds > 10 )
                        WriteToShortTermPlanningLog( "WARNING TOTAL PASS took ms: " + LastOuterFramePlanningStopwatch.ElapsedMilliseconds );
                    else
                        WriteToShortTermPlanningLog( "TOTAL PASS took only ms: " + LastOuterFramePlanningStopwatch.ElapsedMilliseconds );
                }
            }
        }

        private static void Teardown( bool shortTermPlanningDebugLog )
        {
            LastOuterFramePlanningStopwatch.Stop();

            if ( shortTermPlanningDebugLog )
            {
                if ( LastOuterFramePlanningStopwatch.ElapsedMilliseconds > 10 )
                    WriteToShortTermPlanningLog( "WARNING TOTAL PASS took ms: " + LastOuterFramePlanningStopwatch.ElapsedMilliseconds );
                else
                    WriteToShortTermPlanningLog( "TOTAL PASS took only ms: " + LastOuterFramePlanningStopwatch.ElapsedMilliseconds );
            }
            IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
        }

        private static void RunAnExecutionPair_AsABackgroundTask( ArcenShortTermPlanningContext context, bool shortTermPlanningDebugLog )
        { 
            try
            {
                //if ( execution.NameForLogs == "StrengthCounting" )
                //    ArcenDebugging.ArcenDebugLogSingleLine( "Start S-T task: " + execution.NameForLogs + ": TID" + Thread.CurrentThread.ManagedThreadId +
                //    " Fr: " + Engine_Universal.GlobalUniqueFrameNumber, Verbosity.DoNotShow );

                try
                {
                    //Engine_Universal.BeginProfilerSample( context.ThreadName );
                    context.Execute();
                    //Engine_Universal.EndProfilerSample( context.ThreadName );
                }
                catch ( ArcenPleaseStopThisThreadException )
                {
                    //do nothing -- these are valid out here, and will be reported on the interior if there is a problem!
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "ArcenShortTermPlanningManager.ParallelRunAllContext error for context " + context.NameForLogs + ": " + e, Verbosity.ShowAsError );
                }

                context.myStopwatch.Stop();
                context.LastMillisecondsTaken = context.myStopwatch.ElapsedMilliseconds;
                context.LastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective = ArcenTime.NonSetupNetworkReadyGameTimeSinceLastLoadOrStartF;
                if ( context.LargestMillisecondsTaken < context.LastMillisecondsTaken )
                    context.LargestMillisecondsTaken = context.LastMillisecondsTaken;
                context.TimesRun++;
                context.LastTimeRun = ArcenTime.TimeSinceStartF;
                if ( shortTermPlanningDebugLog )
                {
                    if ( context.LastMillisecondsTaken > 5 )
                        WriteToShortTermPlanningLog( context.NameForLogs + " Sub-Context took ms: " + context.LastMillisecondsTaken );
                }
                //if ( execution.NameForLogs == "StrengthCounting" )
                //    ArcenDebugging.ArcenDebugLogSingleLine( "Finish S-T task: " + execution.NameForLogs + ": TID" + Thread.CurrentThread.ManagedThreadId +
                //    " Fr: " + Engine_Universal.GlobalUniqueFrameNumber, Verbosity.DoNotShow );
            }
            catch ( ArcenPleaseStopThisThreadException )
            {
                //do nothing -- these are valid out here, and will be reported on the interior if there is a problem!
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "ArcenShortTermPlanningManager.RunAnExecutionPair_AsABackgroundTask Error: " + e, Verbosity.ShowAsError );
            }
        }

        #region DoShipDisabledChecks
        /// <summary>
        /// This is a minor desync in multiplayer, very occasionally, but I'm going to accept that for the sake of performance in general.
        /// Multiplayer can take care of the minor differences that this will cause as-needed.
        /// </summary>
        private static void DoShipDisabledChecks_OnBGThread()
        {
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                foreach ( GameEntity_Squad entity in planet.Squads() )
                {
                    ArcenRejectionReason shipDisabledReason = entity.ComputeDisabledReason( ArcenRejectionReason.Unknown );
                    entity.ForShortTermPlanning_DisabledReason = shipDisabledReason;

                    for ( int i = 0; i < entity.Systems.Count; i++ )
                    {
                        EntitySystem system = entity.Systems[i];
                        if ( shipDisabledReason != ArcenRejectionReason.Unknown )
                        {
                            system.ForShortTermPlanning_DisabledReason = shipDisabledReason;
                            system.ForShortTermPlanning_CannotBeFiredReason = shipDisabledReason;
                        }
                        else
                        {
                            ArcenRejectionReason disabledReason = system.ComputeDisabledReason(CheckEntityStatus:false, CheckReload:false);
                            var cannotFireReason = disabledReason;
                            //ArcenRejectionReason cannotFireReason = system.ComputeDisabledReason(CheckEntityStatus:false, CheckReload:false);

                            system.ForShortTermPlanning_DisabledReason = disabledReason;
                            system.ForShortTermPlanning_CannotBeFiredReason = cannotFireReason;

                            //if ( disabledReason != ArcenRejectionReason.Unknown )
                            //    system.ForShortTermPlanning_CannotBeFiredReason = disabledReason;
                            //else
                            //    system.ForShortTermPlanning_CannotBeFiredReason = system.ComputeCanBeFired_IgnoreDisabling( false );
                        }
                    }

                }
            }
        }
        #endregion

        #region DoShipCurrentOrderChecks
        /// <summary>
        /// This needs to be done on the main sim thread for sure, or else it really causes divergences in the simulation, as well as likely cross-threading issues.
        /// </summary>
        private static void DoShipCurrentOrderChecks()
        {
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( !World.Instance.IsPaused && //only care about specific frames or not frames if the game is unpaused.  If it's paused, hit everything
                    !planet.BattleStatus_ProcessThisSimStep )
                    continue;
                foreach ( GameEntity_Squad entity in planet.Squads() )
                {
                    entity.ForShortTermPlanning_CurrentValidOrder = entity.RemoveInvalidatedOrdersAndReturnFirstValid_IncludingDecollision();
                }
            }
        }
        #endregion

        private static void WriteToShortTermPlanningLog( string Text )
        {
            try
            {
                Engine_Universal.AppendTextToFile( Engine_Universal.CurrentPlayerDataDirectory + "ShortTermPlanningLog.txt", DateTime.Now.ToShortDateString() + " " +
                    DateTime.Now.ToShortTimeString() + ": " + Text + Environment.NewLine, 1024 * 1024 );
            }
            catch ( Exception ) { } //ignore any of these, probably sharing violation IOExceptions
        }

        public static bool SubTimeTrackers_ShouldTrackNow
        {
            get { return ArcenTime.TimeSinceStartF - SubTimeTrackers_LastTimeAskedToTrack < 3f; } //track for up to 3 seconds since we last heard about a request to track them.
        }
    }
}