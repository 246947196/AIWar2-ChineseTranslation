using Arcen.Universal;
using System;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public sealed class SpecialFactionPlanning : ArcenLongTermIntermittentPlanningContext, ISpecialFactionPlanningContext, IBetweenMapGenPoolable<SpecialFactionPlanning>, IContextForMonitoring
    {
        #region Pooling
        private static ReferenceTracker RefTracker;
        public SpecialFactionPlanning()
            : base( ArcenSimContextType.FactionPlanning )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "SpecialFactionPlanning" );
            RefTracker.IncrementObjectCount();
        }

        private static readonly BetweenMapGenPool<SpecialFactionPlanning> Pool = BetweenMapGenPool<SpecialFactionPlanning>.Create_WillNeverBeGCed( "SpecialFactionPlanning", 40, 40,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new SpecialFactionPlanning(); } );

        public static int NumberRawCreated
        {
            get { return Pool.NumberRawCreated; }
        }
        public static Int64 NumberCreatedOrPoolRequested
        {
            get { return Pool.NumberCreatedOrPoolRequested; }
        }

        public SpecialFactionPlanning CreateNewForPool()
        {
            return new SpecialFactionPlanning();
        }

        public void WipeForReuseAsNewObject()
        {
            this.factionToRunFor = null;
            this.MyThreadNameThatChangesPerRun = string.Empty;
            this.TimesRun = 0;
        }
        #endregion

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
            return 120f;
        }

        private Faction factionToRunFor;

        public static SpecialFactionPlanning GetFromPoolOrCreate()
        {
            SpecialFactionPlanning plan = Pool.GetFromPoolOrCreate();
            return plan;
        }

        public void UpdateToBeForFaction( Faction faction )
        {
            if ( faction == null )
                return;

            if ( factionToRunFor != faction )
            {
                factionToRunFor = faction;
                //Note that this WILL be the same thread for different iterations of the same faction!
                //This is by design.  I want them to share the same thread, since they don't use it at the same time anyhow.
                //It's much more efficient for them to share it.
                this.MyThreadNameThatChangesPerRun = "LRP_" + faction.SpecialFactionData.InternalName;
            }
        }

        public Faction GetFactionThisIsfor()
        {
            return this.factionToRunFor;
        }

        public static bool tracing;

        protected override void ThreadWaitingCheckTeardown()
        {
            Faction fac = this.factionToRunFor;
            if ( fac == null )
                return;
            SpecialFactionData factionData = fac.SpecialFactionData;
            if ( factionData == null )
                return;
            SpecialFactionProcessingGroup processingGroup = factionData.ProcessingGroup;
            if ( processingGroup == null )
                return;
            processingGroup.IsCurrentlyWaitingOnLRPThread.MarkAsNoLongerBusy();
        }
        protected override bool ExtraIsRunningCheck()
        {
            Faction fac = this.factionToRunFor;
            if ( fac == null )
                return false;
            SpecialFactionData factionData = fac.SpecialFactionData;
            if ( factionData == null )
                return false;
            SpecialFactionProcessingGroup processingGroup = factionData.ProcessingGroup;
            if ( processingGroup == null )
                return false;

            //find out ON the task that this is still running, that's fine.  We want to make sure that if the task fails to start, we don't increment IsCurrentlyWaitingOnThread.
            //Byt the time that happens, we're already there.
            if ( processingGroup.IsCurrentlyWaitingOnLRPThread.IsBusy() )
                return true;
            return false;
        }

        protected override void Execute()
        {
            Faction fac = this.factionToRunFor;
            if ( fac != null )
                fac.LastDoLongRangePlanningExtraInfo = "Exec A ";
            else
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Null faction on thread " + this.MyThreadNameThatChangesPerRun + " " + this.ExtraThreadDetails, Verbosity.ShowAsError );
                InnerTeardown( fac );
                return;
            }

            try
            {                

                if ( CentralVars.GetShouldNotRunGameStyleLogic() )
                {
                    fac.LastDoLongRangePlanningExtraInfo = "NO RUN ";
                    InnerTeardown( fac );
                    return;
                }
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                {
                    fac.LastDoLongRangePlanningExtraInfo = "CLIENT ";
                    System.Threading.Interlocked.Exchange( ref fac.LastDoLongRangePlanningEndedAtUnpausedTimeSinceLastRestart_NonSim, ArcenTime.NonSetupNetworkReadyGameTimeSinceLastLoadOrStartF );
                    InnerTeardown( fac );
                    return;
                }

                //if ( World_AIW2.CurrentSimCycleFast != GameEntity_Base.CalculateSimCycleFromInt_Fast( this.fac.FactionIndex ) )
                //    return; //catch you next time!
                if ( World.Instance.IsPaused )
                {
                    fac.LastDoLongRangePlanningExtraInfo = "PAUSED ";
                    InnerTeardown( fac );
                    return;
                }

                //Engine_Universal.NewTimingsBeingBuilt.StartRememberingFrame( FramePartTimings.TimingType.BackgroundSimThreadEntry, this.MyThreadNameThatChangesPerRun );

                fac.LastDoLongRangePlanningExtraInfo = "Exec B ";
                //ArcenDebugging.ArcenDebugLogSingleLine( "Faction start: " + fac.SpecialFactionData.InternalName + " " + fac.FactionIndex, Verbosity.DoNotShow );
                Interlocked.Exchange( ref LastRunForFactionIndex, fac.FactionIndex );
                fac.Safe_DeepInfo_DoLongRangePlanning_OnBackgroundNonSimThread_HostOnly( this );
                //ArcenDebugging.ArcenDebugLogSingleLine( "Faction done: " + fac.SpecialFactionData.InternalName + " " + fac.FactionIndex, Verbosity.DoNotShow );

                fac.LastDoLongRangePlanningExtraInfo = "Exec C ";
            }
            catch ( ArcenPleaseStopThisThreadException )
            {
                InnerTeardown( fac );
            }
            catch ( Exception e )
            {
                InnerTeardown( fac );
                ArcenDebugging.ArcenDebugLogSingleLine( this.MyThreadNameThatChangesPerRun + " Error: " + e, Verbosity.ShowAsError );
            }

            InnerTeardown( fac );
        }

        private void InnerTeardown( Faction fac )
        {
            Interlocked.Exchange( ref LastRunForFactionIndex, -1 );
            this.ThreadWaitingCheckTeardown();

            if ( fac != null )
            {
                //ArcenDebugging.ArcenDebugLogSingleLine( "Faction teardown 1: " + faction.SpecialFactionData.InternalName + " " + faction.FactionIndex, Verbosity.DoNotShow );

                System.Threading.Interlocked.Exchange( ref fac.LastDoLongRangePlanningEndedAtUnpausedTimeSinceLastRestart_NonSim, ArcenTime.NonSetupNetworkReadyGameTimeSinceLastLoadOrStartF );
                System.Threading.Interlocked.Exchange( ref fac.LastDoLongRangePlanningReason, "Normal Finish Execute" );
                //Engine_Universal.NewTimingsBeingBuilt.FinishRememberingFrame( FramePartTimings.TimingType.BackgroundSimThreadEntry, this.MyThreadNameThatChangesPerRun );

                //ArcenDebugging.ArcenDebugLogSingleLine( "Faction teardown 2: " + faction.SpecialFactionData.InternalName + " " + faction.FactionIndex, Verbosity.DoNotShow );
            }
        }

        public override bool GetNeedsToRun()
        {
            Faction fac = this.factionToRunFor;
            if ( fac == null )
                return false;
            ExternalFactionDeepInfo deepInfo = fac.DeepInfo;
            if ( deepInfo == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Null DeepInfo on faction " + fac.GetDisplayName() + ", so of course it can't run LRP threads!", Verbosity.ShowAsError );
                return false;
            }

            return deepInfo.GetNeedsToRunLongRangePlanning( fac.LastIntermittentLongRangePlanningStartedTime_Nonsim, this );
        }

        #region IContextForMonitoring
        public override string NameForDisplay => this.factionToRunFor == null ? "[NullFaction]" : this.factionToRunFor.GetDisplayName();
        #endregion
    }
}
