using Arcen.Universal;
using System;

using System.Diagnostics;
using System.Threading;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// There's one execution context that is part of the sim that runs after all short-term planning contexts.
    /// It must run to completion in order to process the sim-frame.
    /// After that the planning contexts can run again to plan the next frame.
    /// 
    /// Note that the central control logic is all in SimPlannerImplementation.ProcessCoreLogicForArbitraryFrameOnMainThread
    /// (That's in the Execution subfolder).
    /// </summary>
    public static class ArcenExecutionManager
    {
        private readonly static object contextLock = new object();
        public static ArcenClientOrHostSimPlanningContext SoleContext = null;
        private static readonly Stopwatch LastFrameExecutionStopwatch = new Stopwatch();

        private static ReferenceTracker RefTracker;
        static ArcenExecutionManager()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ArcenExecutionManager" );
            RefTracker.IncrementObjectCount();
        }

        public static void CleanupAll()
        {
            ArcenClientOrHostSimPlanningContext cont = SoleContext;
            if ( cont != null )
                cont.Cleanup();
            SoleContext = null;

            RunAllContextsHits = 0;

            //ArcenDebugging.ArcenDebugLog( "ArcenExecutionManager CleanupAll!  ThreadID: " + Thread.CurrentThread.ManagedThreadId + " AllContexts: " + AllContexts.Count, Verbosity.DoNotShow );
        }

        private static void InitializeIfNeeded()
        {
            lock ( contextLock )
            {
                if ( SoleContext != null  )
                    return;
                SoleContext = SimExecution.GetFromPoolOrCreate();

                //ArcenDebugging.ArcenDebugLog( "ArcenExecutionManager InitializeIfNeeded!  ThreadID: " + Thread.CurrentThread.ManagedThreadId + " AllContexts: " + AllContexts.Count, Verbosity.DoNotShow );
            }
        }

        public static long RunAllContextsHits = 0;

        public static void RunAllContexts()
        {
            RunAllContextsHits++;

            if ( World_AIW2.Instance.IsOutsideOfNormalGameplay )
                return;
            if ( !GetIsSoleExecutionContextDone() )
                return; //oops, don't run us yet!

            LastFrameExecutionStopwatch.Stop();
            InitializeIfNeeded();
            LastFrameExecutionStopwatch.Reset();
            LastFrameExecutionStopwatch.Start();

            //ArcenDebugging.ArcenDebugLogSingleLine( "Start RunAllContexts (" + AllContexts.Count + "): TID" + Thread.CurrentThread.ManagedThreadId +
            //        " Fr: " + Engine_Universal.GlobalUniqueFrameNumber, Verbosity.DoNotShow );

            //ArcenDebugging.ArcenDebugLog( "ArcenExecutionManager Run!  ThreadID: " + Thread.CurrentThread.ManagedThreadId + " AllContexts: " + AllContexts.Count, Verbosity.DoNotShow );

            ArcenClientOrHostSimPlanningContext cont = SoleContext;
            if ( cont != null )
                cont.RunOnBackgroundThread( "_Sim.ArcenExecutionManager.SoleContext", 0.5f, //if this isn't done in half a second, then kill it.
                    false );
            else
                ArcenDebugging.ArcenDebugLog( "ArcenExecutionManager SoleContext missing!", Verbosity.ShowAsError );
        }

        public static bool GetIsSoleExecutionContextDone()
        {
            ArcenClientOrHostSimPlanningContext cont = SoleContext;
            if ( cont != null )
            {
                if ( !cont.GetIsWorkDone() )
                    return false;
            }

            if ( LastFrameExecutionStopwatch.IsRunning )
            {
                LastFrameExecutionStopwatch.Stop();
            }
            return true;
        }
    }
}