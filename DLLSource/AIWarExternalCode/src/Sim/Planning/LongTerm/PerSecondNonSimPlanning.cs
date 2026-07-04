using Arcen.Universal;
using System;

using System.Diagnostics;
using System.Threading;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public sealed class PerSecondNonSimPlanning : ArcenLongTermContinuousPlanningClientOrHostContext, IBetweenMapGenPoolable<PerSecondNonSimPlanning>
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

        private PerSecondNonSimPlanning()
            : base( "_PSec.PerSecondNonSimPlanning", ArcenSimContextType.LongTermContinuous )
        {
        }

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            this.DoNotRunAgainUntilTime = 0;
            CleanupBase();
        }

        private static readonly BetweenMapGenPool<PerSecondNonSimPlanning> Pool = BetweenMapGenPool<PerSecondNonSimPlanning>.Create_WillNeverBeGCed( "PerSecondNonSimPlanning", 30, 10,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new PerSecondNonSimPlanning(); } );

        public static PerSecondNonSimPlanning GetFromPoolOrCreate()
        {
            PerSecondNonSimPlanning context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion

        private float DoNotRunAgainUntilTime;
        public override bool GetNeedsToRun()
        {
            return this.GetIsWorkDone() && this.DoNotRunAgainUntilTime < ArcenTime.TimeSinceStartF;
        }

        protected override void Execute()
        {
            float startTime = ArcenTime.TimeSinceStartF;
            try
            {
                this.DoCentralLoop_HostOnly();
                this.DoCentralLoop_ClientAndHost();
            }
            catch ( ArcenPleaseStopThisThreadException )
            {
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "PerSecondNonSimPlanning Error: " + e, Verbosity.ShowAsError );
            }
            finally
            {
                //hey, we finished; we do that every time, actually
                {
                    float finishedTime = ArcenTime.TimeSinceStartF;

                    //try to run this every 1 seconds, or with 0.5 second gaps, whichever is slower
                    float timeToWaitBeforeNextStart = 1f - (finishedTime - startTime);
                    if ( timeToWaitBeforeNextStart < 0.5f )
                        timeToWaitBeforeNextStart = 0.5f;
                    this.DoNotRunAgainUntilTime = ArcenTime.TimeSinceStartF + timeToWaitBeforeNextStart;
                }
            }
        }

        /// <summary>
        /// This is happening on a background thread that is not sim-blocking in any way.
        /// </summary>
        private void DoCentralLoop_HostOnly()
        {
            //this whole thing is now host-only!
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            //nothing to do only on the host right now, but we could add something later if we feel like it!
        }

        /// <summary>
        /// This is happening on a background thread that is not sim-blocking in any way.
        /// </summary>
        private void DoCentralLoop_ClientAndHost()
        {
            foreach ( Faction faction in World_AIW2.Instance.Factions )
            {
                //cleans things up and gets them ready before the next set of per-second loops
                faction.ResetAllForPerSecondNonSimUpdate_OnBackgroundNonSimThread_NonBlocking_ClientOrHost();
            }

            //we may not have much to do in here, now
            foreach ( Faction faction in World_AIW2.Instance.Factions )
            {
                faction.Safe_BaseInfo_DoPerSecondNonSimUpdate_OnBackgroundNonSimThread_NonBlocking_ClientOrHost( this );
            }

            //any faction-related notifications should be added in here.  This happens after ALL of the other per-second stuff
            foreach ( Faction faction in World_AIW2.Instance.Factions )
            {
                faction.DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost( this );
            }

            //any non-faction-related notifications, or ones that apply to many factions, should be in here:
            foreach ( ExternalWorldBaseInfoSource worldInfo in ExternalWorldBaseInfoSourceTable.Instance.Rows )
            {
                if ( worldInfo.Singleton.GetShouldIBeInUse() )
                    worldInfo.Singleton.Safe_DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost( this );
            }

            //this happens on clients and the host
            PersonalNotificationGenerator.GenerateAllPersonalNotifications_FromBGThread( this );

            //This cascades down to any and all child lists.
            //What was under construction is now constructed, and what was constructed is now cleared
            //this happens on the host AND the client!
            Notification.FlipConstructingAndComplete();

            ObjectiveGeneratorTable.Instance.RegenerateAllObjectives_ClientOrHost( this );
        }
    }
}