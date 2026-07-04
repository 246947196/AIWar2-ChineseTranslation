using Arcen.Universal;
using System;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// This is the last secondary-thread step of the sim 
    /// before SimPlannerImplementation (on the main thread) can flip to the next sim-frame.
    /// 
    /// All of those short-term planning threads have to run, then this can run.
    /// And then once this is done, then SimPlannerImplementation can do its flip.
    /// </summary>
    public sealed class SimExecution : ArcenClientOrHostSimPlanningContext, IBetweenMapGenPoolable<SimExecution>
    {
        public override int GetSecondsToLiveEachCycle()
        {
            return 10;
        }

        public override int GetSecondsAfterWhichToWarnInOneCycle()
        {
            return 2;
        }

        public override float GetTimeAfterWhichToWarnOfNotRunning()
        {
            return 5f;
        }

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            CleanupBase();

            WorldSimClientOrHost_Hits = 0;
            WorldSimHostOnly_Hits = 0;

            //Phase 2: don't carry a prior game's sim-step timing into the next one (these are static/shared across worlds).
            MostRecentFrameTimes.Clear();
            _recentSimStepTimeSumMs = 0;
        }

        private static readonly BetweenMapGenPool<SimExecution> Pool = BetweenMapGenPool<SimExecution>.Create_WillNeverBeGCed( "SimExecution", 10, 5,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new SimExecution(); } );

        public static SimExecution GetFromPoolOrCreate()
        {
            SimExecution context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion

        public static int WorldSimClientOrHost_Hits = 0;
        public static int WorldSimHostOnly_Hits = 0;

        private ThreadingExchanger IsCurrentlyWaitingOnThread = new ThreadingExchanger( "SimExecution", 30f );

        public static readonly ConcurrentQueue<long> MostRecentFrameTimes = ConcurrentQueue<long>.Create_WillNeverBeGCed( "SimExecution-MostRecentFrameTimes" );
        private static long _recentSimStepTimeSumMs = 0; //Phase 2: running sum of MostRecentFrameTimes so the per-step average needs no enumeration (keeps this sim-hot-loop path allocation-free)

        private SimExecution()
            : base( "SimExecution", ArcenSimContextType.ExecutionContextThread_Aka_MainThreadBGIndirect )
        {
            //ArcenDebugging.ArcenDebugLog( "Starting SimExecution!  ThreadID: " + Thread.CurrentThread.ManagedThreadId, Verbosity.DoNotShow );
        }

        protected override void Execute()
        {
            if ( CentralVars.GetShouldNotRunGameStyleLogic() )
                return;
            if ( CentralVars.DEBUG_TURN_OFF_MAIN_SIM_EXECUTION )
                return;
            if ( World_AIW2.Instance.IsSimulationArtificiallyStopped )
                return;
            if ( World_AIW2.Instance.IsOutsideOfNormalGameplay )
            {
                // in this specific case, this function is executing on the main thread
                if ( World_AIW2.Instance.OnClient_GameCommandsThatHaveNotYetBeenSentToServer.Count > 0 )
                    return; // wait until the mapgen command has gone through before doing anything else; notably before refreshing the UI
            }
            if ( IsCurrentlyWaitingOnThread.IsBusy() )
                return;

            if ( !IsCurrentlyWaitingOnThread.DoNextOnlyIfNotAlreadyBusy( this.MyPermanentThreadName ) )
                return;

            //ArcenDebugging.ArcenDebugLogSingleLine( "Start SimExecution: TID" + Thread.CurrentThread.ManagedThreadId +
            //        " Fr: " + Engine_Universal.GlobalUniqueFrameNumber, Verbosity.DoNotShow );

            try
            {
                this.RandomToUse.ReinitializeWithSeed( World_AIW2.Instance.Network_CurrentFrameNumber + World_AIW2.Instance.Setup.MapConfig.Seed );

                //ArcenDebugging.ArcenDebugLogSingleLine( "executionContext RUN!", Verbosity.DoNotShow );  //PAUSE_3_TODO

                try
                {
                    EntitySimLogicBaseInfo.Instance.DoWorldStepLogic_ClientOrHost_FromSimBGThread( this );
                    Interlocked.Increment( ref WorldSimClientOrHost_Hits );
                    EntitySimLogicDeepInfo.Instance.DoWorldStepLogic_HostOnly_FromSimBGThread( this.GetHostOnlyContext() );
                    Interlocked.Increment( ref WorldSimHostOnly_Hits );
                }
                catch ( ArcenPleaseStopThisThreadException ) //this is normal
                {}
                catch ( Exception e ) //ideally prevent crashes on linux when errors are thrown on the non-main thread.
                {
                    ArcenDebugging.LogException( e, "Error in thread for SimExecution context '" + this.MyPermanentThreadName + "'", Verbosity.ShowAsError );
                }

                long thisStepMs = this.LastRunStopwatch.ElapsedMilliseconds;
                MostRecentFrameTimes.Enqueue( thisStepMs );
                _recentSimStepTimeSumMs += thisStepMs;
                while ( MostRecentFrameTimes.Count > 20 )
                {
                    if ( MostRecentFrameTimes.TryDequeue( out long removed ) )
                        _recentSimStepTimeSumMs -= removed;
                }

                //Phase 2 adaptive frame-budget: mirror the rolling-average sim-step compute time (ms) onto World_AIW2 (Core) so the
                //host controller and the per-frame client load-report can read it without reaching into this External assembly.
                //Uses the running sum (NOT a per-step foreach over the queue) to keep this sim-hot-loop path allocation-free.
                World_AIW2 worldForTiming = World_AIW2.Instance;
                if ( worldForTiming != null )
                {
                    int simStepTimeCount = MostRecentFrameTimes.Count;
                    worldForTiming.NonSim_RecentAverageSimStepMilliseconds = simStepTimeCount > 0 ? (float)_recentSimStepTimeSumMs / simStepTimeCount : 0f;
                }
                IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
            }
            catch ( ArcenPleaseStopThisThreadException ) //this is normal
            {
                IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
            }
            catch ( Exception e )
            {
                IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
                ArcenDebugging.ArcenDebugLogSingleLine( "SimExecution Error for context context '" + this.MyPermanentThreadName + "': " + e, Verbosity.ShowAsError );
            }
        }
    }
}