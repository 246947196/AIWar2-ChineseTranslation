using Arcen.Universal;
using System;

using System.Diagnostics;
using System.Threading;
using Arcen.AIW2.Core;
using System.Threading.Tasks;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// Short Term Planning code executions must be completed before the next sim-frame can be processed.
    /// Basically these are parts of the sim, directly, that are split off into a variety of threads.
    /// This is not the implementation, but rather those are in classes that implement this class.
    /// 
    /// If you look under the static constructor for ArcenShortTermPlanningContext, then you can
    /// see the main actual threads that run.  They are things like keeping track of how ships move,
    /// or what they want to autotarget-shoot, or the spending of metal, etc.
    /// 
    /// These classes are basically the heavy lifting of the sim.  These all happening at once, as
    /// fast as possible, and then flagging themselves as done, is the biggest barrier to the sim
    /// being able to proceed.
    /// </summary>
    public abstract class ArcenShortTermPlanningContext : ArcenClientOrHostSimContextCore, IContextForMonitoring
    {
        internal readonly Stopwatch myStopwatch = new Stopwatch();
        public readonly string NameForLogs;

        private static ReferenceTracker RefTracker;
        protected ArcenShortTermPlanningContext( string NameForLogs )
            : base( ArcenSimContextType.ShortTermPlanning )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ArcenShortTermPlanningContexts" );
            RefTracker.IncrementObjectCount();

            this.NameForLogs = NameForLogs;
        }

        private void CleanupAll()
        {
            this.LastMillisecondsTaken = -1;
            this.LargestMillisecondsTaken = -1;
            this.TimesRun = 0;
            this.LastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective = 0;
            this.LastTimeRun = 0;

            foreach ( SubTimeTracker tracker in this.SubTimeTrackers )
            {
                tracker.LastMillisecondsTaken = -1;
                tracker.LargestMillisecondsTaken = -1;
                tracker.TimesRun = 0;
            }
        }

        public abstract void Execute();

        #region IContextForMonitoring
        public int GetMillisecondsAfterWhichToWarn_OfLongRunning()
        {
            return 1000; //for all of these, go ahead
        }

        public int GetCurrentElapsedMiliseconds()
        {
            return (int)myStopwatch.ElapsedMilliseconds;
        }

        public float GetTimeAfterWhichToWarn_OfNotRunning()
        {
            return 2f;
        }

        public float GetLastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective()
        {
            return this.LastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective;
        }

        public bool GetIsWorkDone()
        {
            return World.Instance.IsPaused;
        }

        public int GetLastElapsedMiliseconds()
        {
            return (int)this.LastMillisecondsTaken;
        }

        public string NameForDisplay => this.NameForLogs;
        #endregion

        public long LastMillisecondsTaken = -1;
        public long LargestMillisecondsTaken = -1;
        public long TimesRun = 0;
        public float LastTimeRun = 0;
        public float LastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective = 0;
        public readonly List<SubTimeTracker> SubTimeTrackers = List<SubTimeTracker>.Create_WillNeverBeGCed( 250, "ArcenShortTermPlanningContext-SubTimeTrackers" );

        public class SubTimeTracker
        {
            public Stopwatch Stopwatch;
            public string SubTimeTrackerName;
            public bool IgnoreIfNeverRun = false;
            public long LastMillisecondsTaken = -1;
            public long LargestMillisecondsTaken = -1;
            public long TimesRun = 0;
            public bool LargestTimeTakenIsTimestampInstead = false;
        }
    }
}