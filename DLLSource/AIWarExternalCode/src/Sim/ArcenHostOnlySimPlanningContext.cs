using Arcen.Universal;
using System;

using System.Diagnostics;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// This is the root abstract class for all of the threads that are just run on the host, mainly for long-term planning or similar.
    /// </summary>
    public abstract class ArcenHostOnlySimPlanningContext : ArcenHostOnlySimContextStandalone
    {
        private ThreadingExchanger IsCurrentlyWaitingOnThread = new ThreadingExchanger( "ArcenHostOnlySimPlanningContext", 30f );
        public readonly Stopwatch LastRunStopwatch = new Stopwatch();

        public abstract int GetSecondsToLiveEachCycle();
        public abstract int GetSecondsAfterWhichToWarnInOneCycle();
        public abstract float GetTimeAfterWhichToWarnOfNotRunning();

        /// <summary>
        /// This changes when we're assigning different factions, etc.
        /// </summary>
        public string MyThreadNameThatChangesPerRun;
        public int TimesRun = 0;
        public int TimesRunAttempted = 0;
        public int LastElapsedMSAtCompletion = 0;
        public float LastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective = 0;
        public int LastRunForFactionIndex = -1;

        protected void CleanupBase()
        {
            this.MyThreadNameThatChangesPerRun = "WhoKnows?";

            this.TimesRun = 0;
            this.TimesRunAttempted = 0;
            this.TimesRunAttempted = 0;
            this.LastRunForFactionIndex = -1;
            this.LastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective = 0;

            LastRunStopwatch.Stop();
            LastRunStopwatch.Reset();

            IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
        }

        private static ReferenceTracker RefTracker;
        protected ArcenHostOnlySimPlanningContext( ArcenSimContextType contextType )
            : base( contextType )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ArcenHostOnlySimPlanningContexts" );
            RefTracker.IncrementObjectCount();
        }

        public bool RunOnBackgroundThread( string RequestTypeName, bool FailSilentlyIfNotFinishedYet, ThreadingExchanger ExchangerToMarkAsDoneWhenFinishedOrNull )
        {
            Interlocked.Increment( ref this.TimesRunAttempted );

            if ( !ArcenThreading.RunTaskOnBackgroundThread( RequestTypeName, true, false, () => BackgroundThreadRunHandler( FailSilentlyIfNotFinishedYet, ExchangerToMarkAsDoneWhenFinishedOrNull ) ) )
            {
                ExchangerToMarkAsDoneWhenFinishedOrNull?.MarkAsNoLongerBusy();
                return false;
            }
            return true;
        }

        public int GetCurrentElapsedMiliseconds()
        {
            if ( !IsCurrentlyWaitingOnThread.IsBusy() )
                return -1;
            return (int)LastRunStopwatch.ElapsedMilliseconds;
        }

        public int GetLastElapsedMiliseconds()
        {
            return this.LastElapsedMSAtCompletion;
        }

        public int GetMilisecondsAfterWhichToWarnInOneCycle()
        {
            return this.GetSecondsAfterWhichToWarnInOneCycle() * 1000;
        }

        protected abstract void ThreadWaitingCheckTeardown();
        protected abstract bool ExtraIsRunningCheck();

        private void BackgroundThreadRunHandler( bool FailSilentlyIfNotFinishedYet, ThreadingExchanger ExchangerToMarkAsDoneWhenFinishedOrNull )
        {
            try
            {
                ISpecialFactionPlanningContext specialFactionPlanOrNull = this as ISpecialFactionPlanningContext;
                Faction facThisIsForOrNull = null;
                if ( specialFactionPlanOrNull != null )
                {
                    facThisIsForOrNull = specialFactionPlanOrNull.GetFactionThisIsfor();
                    if ( facThisIsForOrNull == null )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "Null faction on " + this.MyThreadNameThatChangesPerRun + " " + this.ExtraThreadDetails, Verbosity.ShowAsError );
                        ExchangerToMarkAsDoneWhenFinishedOrNull?.MarkAsNoLongerBusy();
                        return;
                    }
                }

                //The below is needed, regardless, in order to tell if we're over-running by thread type
                //But the abvove is a more broad-spectrum blocker.

                //find out ON the task that this is still running, that's fine.  We want to make sure that if the task fails to start, we don't increment IsCurrentlyWaitingOnThread.
                //Byt the time that happens, we're already there.
                if ( this.IsCurrentlyWaitingOnThread.IsBusy() )
                {
                    if ( facThisIsForOrNull != null )
                        facThisIsForOrNull.LastDoLongRangePlanningExtraInfo = "Fail IsCurrentlyWaitingOnThread ";

                    if ( !FailSilentlyIfNotFinishedYet )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Tried to start thread '" + this.MyThreadNameThatChangesPerRun + "', but it was already running (B)!", Verbosity.ShowAsError );
                    if ( this.GetType().ToString().Contains( "SpecialFactionPlann" ) )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Faction IsCurrentlyWaitingOnThread", Verbosity.DoNotShow );
                    ExchangerToMarkAsDoneWhenFinishedOrNull?.MarkAsNoLongerBusy();
                    return;
                }
                if ( !IsCurrentlyWaitingOnThread.DoNextOnlyIfNotAlreadyBusy( this.MyThreadNameThatChangesPerRun ) )
                {
                    if ( facThisIsForOrNull != null )
                        facThisIsForOrNull.LastDoLongRangePlanningExtraInfo = "Fail DoNextOnlyIfNotAlreadyBusy ";
                    ExchangerToMarkAsDoneWhenFinishedOrNull?.MarkAsNoLongerBusy();
                    return;
                }

                LastRunStopwatch.Stop();
                LastRunStopwatch.Reset();
                LastRunStopwatch.Start();

                this.LastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective = ArcenTime.NonSetupNetworkReadyGameTimeSinceLastLoadOrStartF;

                if ( CentralVars.GetShouldNotRunGameStyleLogic() )
                {
                    if ( facThisIsForOrNull != null )
                        facThisIsForOrNull.LastDoLongRangePlanningExtraInfo = "Fail GetShouldNotRunGameStyleLogic ";
                    this.Teardown();
                    ExchangerToMarkAsDoneWhenFinishedOrNull?.MarkAsNoLongerBusy();
                    return;
                }

                Interlocked.Increment( ref this.TimesRun );

                this.IncrementCycle(); //calling this more times than needed won't hurt anything
            
                this.ClearRememberedDataForNewCycle();
                this.RandomToUse.ReinitializeWithSeed( World_AIW2.Instance.Network_CurrentFrameNumber + World_AIW2.Instance.Setup.MapConfig.Seed );

                if ( facThisIsForOrNull != null )
                    facThisIsForOrNull.LastDoLongRangePlanningExtraInfo = "Exectute";

                Execute();
                this.Teardown();
                ExchangerToMarkAsDoneWhenFinishedOrNull?.MarkAsNoLongerBusy();
            }
            catch ( ArcenPleaseStopThisThreadException )//this is normal
            {
                this.Teardown();
                ExchangerToMarkAsDoneWhenFinishedOrNull?.MarkAsNoLongerBusy();
            } 
            catch ( Exception e )
            {
                this.Teardown();
                if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                    return; //we're killing the game, leave us alone
                ArcenDebugging.LogException( e, "Error occurred in h-o sim planning context " + this.MyThreadNameThatChangesPerRun, Verbosity.ShowAsError );
                ExchangerToMarkAsDoneWhenFinishedOrNull?.MarkAsNoLongerBusy();
            }
        }

        protected abstract void Execute();

        protected void Teardown()
        {
            IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
            this.ThreadWaitingCheckTeardown();
            this.LastRunStopwatch.Stop();
            this.LastElapsedMSAtCompletion = (int)this.LastRunStopwatch.ElapsedMilliseconds;
        }

        public bool GetIsWorkDone()
        {
            if ( IsCurrentlyWaitingOnThread.IsBusy() )
                return false;
            if ( this.ExtraIsRunningCheck() )
                return false;
            return true;
        }

        public bool IsRunning
        {
            get { return !GetIsWorkDone(); } //it's running if the work is not yet done.
        }
    }
}