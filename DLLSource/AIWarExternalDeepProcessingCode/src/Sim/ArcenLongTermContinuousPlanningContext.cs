using Arcen.Universal;
using System;

using System.Threading;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// Long Term Planning contexts may run any number of frames, but must be completed before 
    /// the simulation can drop any dead ships or otherwise change data the long-term planning needs
    /// Currently these are only supported on the host.
    /// 
    /// The big thing for these is decollisions and targeting.
    /// 
    /// These are not things that are actually directly blocking the sim, per se.  They aren't part of the sim
    /// loop, anyhow.  It's possible that we have an issue in here that is causing these to hold up the sim
    /// in some way, because of the need to not remove ships or other data that these would use.
    /// But surely we thought of that...
    /// 
    /// These ones in particular are "continuous," which means they run and then get restarted
    /// almost as soon as they stop.  These things are slightly more time-sensitive and central.
    /// </summary>
    public abstract class ArcenLongTermContinuousPlanningContext : ArcenHostOnlySimPlanningContext, ILongRangePlanningHostContext
    {
        public override bool IsLongRangePlanning
        {
            get { return true; }
        }

        private static ReferenceTracker RefTracker;
        protected ArcenLongTermContinuousPlanningContext( ArcenSimContextType contextType )
            : base( contextType )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ArcenLongTermContinuousPlanningContexts" );
            RefTracker.IncrementObjectCount();
        }

        public abstract bool GetNeedsToRun();

        protected override void ThreadWaitingCheckTeardown()
        {
            //no extra blocking
        }

        protected override bool ExtraIsRunningCheck()
        {
            return false; //no extra blocking
        }

        #region IContextForMonitoring
        public int GetMillisecondsAfterWhichToWarn_OfLongRunning()
        {
            return this.GetSecondsAfterWhichToWarnInOneCycle() * 1000;
        }

        public float GetTimeAfterWhichToWarn_OfNotRunning()
        {
            return this.GetTimeAfterWhichToWarnOfNotRunning();
        }

        public float GetLastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective()
        {
            return this.LastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective;
        }

        public string NameForDisplay => this.MyThreadNameThatChangesPerRun;
        #endregion
    }
}
