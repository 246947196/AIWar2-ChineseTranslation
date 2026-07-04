using Arcen.Universal;
using System;
using System.Diagnostics;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// This is the root abstract class for all of the threads that are part of the sim and run on clients and the host
    /// </summary>
    public abstract class ArcenClientOrHostSimPlanningContext : ArcenClientOrHostSimContextCore, IContextForMonitoring
    {
        private ThreadingExchanger IsCurrentlyWaitingOnThread = new ThreadingExchanger( "ArcenClientOrHostSimPlanningContext", 30f );
        public readonly Stopwatch LastRunStopwatch = new Stopwatch();

        public abstract int GetSecondsToLiveEachCycle();
        public abstract int GetSecondsAfterWhichToWarnInOneCycle();
        public abstract float GetTimeAfterWhichToWarnOfNotRunning();

        public readonly string MyPermanentThreadName;
        public int TimesRun = 0;
        public int TimesRunAttempted = 0;
        public int LastElapsedMSAtCompletion = 0;
        public int TimesSuicided = 0;
        public float LastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective = 0;
        public float SuicidesAtTime = 0;

        protected void CleanupBase()
        {
            this.TimesRun = 0;
            this.TimesRunAttempted = 0;
            this.TimesRunAttempted = 0;
            this.TimesSuicided = 0;
            this.LastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective = 0;

            LastRunStopwatch.Stop();
            LastRunStopwatch.Reset();

            IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
        }

        private static ReferenceTracker RefTracker;
        protected ArcenClientOrHostSimPlanningContext( string MyPermanentThreadName, ArcenSimContextType contextType )
            : base( contextType )
        {
            this.MyPermanentThreadName = MyPermanentThreadName;

            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ArcenClientOrHostSimPlanningContexts" );
            RefTracker.IncrementObjectCount();
        }

        public void RunOnBackgroundThread( string ThreadName, float SuicidesAfterTime, bool FailSilentlyIfNotFinishedYet )
        {
            this.TimesRunAttempted++;

            ArcenThreading.RunTaskOnBackgroundThread( ThreadName, false, false, () => BackgroundThreadRunHandler( SuicidesAfterTime, FailSilentlyIfNotFinishedYet ) );
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

        private void BackgroundThreadRunHandler( float SuicidesAfterTime, bool FailSilentlyIfNotFinishedYet )
        {
            try
            {
                //find out ON the task that this is still running, that's fine.  We want to make sure that if the task fails to start, we don't increment IsCurrentlyWaitingOnThread.
                //Byt the time that happens, we're already there.
                if ( this.IsCurrentlyWaitingOnThread.IsBusy() )
                {
                    if ( !FailSilentlyIfNotFinishedYet )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Tried to start thread '" + this.MyPermanentThreadName + "', but it was already running (B)!", Verbosity.ShowAsError );
                    return;
                }
                if ( !IsCurrentlyWaitingOnThread.DoNextOnlyIfNotAlreadyBusy( this.MyPermanentThreadName ) )
                    return;

                if ( SuicidesAfterTime > 0 )
                    this.SuicidesAtTime = ArcenTime.NonSetupGeneralGameTimeSinceLastLoadOrStartF + SuicidesAfterTime;
                else
                    this.SuicidesAtTime = 0;

                LastRunStopwatch.Stop();
                LastRunStopwatch.Reset();
                LastRunStopwatch.Start();

                this.LastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective = ArcenTime.NonSetupNetworkReadyGameTimeSinceLastLoadOrStartF;

                if ( CentralVars.GetShouldNotRunGameStyleLogic() )
                {
                    this.Teardown();
                    return;
                }

                Interlocked.Increment( ref this.TimesRun );

                this.IncrementCycle(); //calling this more times than needed won't hurt anything

                this.ClearRememberedDataForNewCycle();
                this.RandomToUse.ReinitializeWithSeed( World_AIW2.Instance.Network_CurrentFrameNumber + World_AIW2.Instance.Setup.MapConfig.Seed );

                Execute();
                this.Teardown();
            }
            catch ( ArcenPleaseStopThisThreadException ) //this is normal
            {
                this.Teardown();
            }
            catch ( Exception e )
            {
                this.Teardown();
                if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                    return; //we're killing the game, leave us alone
                ArcenDebugging.LogException( e, "Error occurred in c-o-h sim planning context " + this.MyPermanentThreadName, Verbosity.ShowAsError );
            }
        }

        protected abstract void Execute();

        protected void Teardown()
        {
            IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
            this.LastRunStopwatch.Stop();
            this.LastElapsedMSAtCompletion = (int)this.LastRunStopwatch.ElapsedMilliseconds;
        }

        internal void Cleanup()
        {
            //hopefully the thread stopped on its own before now, but either way we want our stuff to be ready to run again
            this.Teardown();
        }

        public bool GetIsWorkDone()
        {
            if ( IsCurrentlyWaitingOnThread.IsBusy() )
            {
                if ( this.SuicidesAtTime > 0 && this.SuicidesAtTime < ArcenTime.NonSetupGeneralGameTimeSinceLastLoadOrStartF )
                {
                    Interlocked.Increment( ref TimesSuicided );
                    this.Teardown();
                    return true;
                }
                return false;
            }
            return true;
        }

        #region IContextForMonitoring
        public int GetMillisecondsAfterWhichToWarn_OfLongRunning()
        {
            return this.GetMilisecondsAfterWhichToWarnInOneCycle();
        }

        public float GetTimeAfterWhichToWarn_OfNotRunning()
        {
            return this.GetTimeAfterWhichToWarnOfNotRunning();
        }

        public float GetLastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective()
        {
            return this.LastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective;
        }

        public string NameForDisplay => this.MyPermanentThreadName;
        #endregion
    }
}