using Arcen.Universal;
using System;

using System.Diagnostics;
using System.Threading;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public static class ArcenVariousLongTermContextManager
    {
        public static readonly List<ArcenLongTermContinuousPlanningClientOrHostContext> AllContexts_ClientOrHost = List<ArcenLongTermContinuousPlanningClientOrHostContext>.Create_WillNeverBeGCed( 4,
            "ArcenVariousLongTermContextManager-AllContexts_ClientOrHost", 2 );

        public static readonly List<IContextForMonitoring> AllContexts_HostContinuous = List<IContextForMonitoring>.Create_WillNeverBeGCed( 10,
            "ArcenVariousLongTermContextManager-AllContexts_HostContinuous", 5 );

        private static ReferenceTracker RefTracker;
        static ArcenVariousLongTermContextManager()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ArcenVariousLongTermContextManager" );
            RefTracker.IncrementObjectCount();
        }

        public static void CleanupAll()
        {
            for ( int i = 0; i < AllContexts_ClientOrHost.Count; i++ )
                AllContexts_ClientOrHost[i].Cleanup();

            //we can just clear these, no big deal, because they automatically got pulled back into the pool
            //we don't have to actually make them go back into the pool ourselves, or need a protected list, or any of that.
            AllContexts_ClientOrHost.Clear();

            //These are just from deepinfo to tell us about stuff, so we REALLY can clear these without doing anything
            AllContexts_HostContinuous.Clear();
        }

        private static void InitializeIfNeeded()
        {
            lock ( AllContexts_ClientOrHost )
            {
                if ( AllContexts_ClientOrHost.Count > 0 )
                    return;
                AllContexts_ClientOrHost.Add( PerSecondNonSimPlanning.GetFromPoolOrCreate() );
                AllContexts_ClientOrHost.Add( StrengthCounting.GetFromPoolOrCreate() );
                AllContexts_ClientOrHost.Add( PerSecondSimRepair.GetFromPoolOrCreate() );
            }            
        }

        public static void RunAllContexts_ForPausedOrUnpaused()
        {
            if ( CentralVars.DEBUG_TURN_OFF_PER_SEC_NON_SIM_PLAN )
                return;
            if ( World_AIW2.Instance.IsOutsideOfNormalGameplay )
                return;
            if ( CentralVars.GetShouldNotRunGameStyleLogic() )
                return;

            InitializeIfNeeded();

            for ( int i = 0; i < AllContexts_ClientOrHost.Count; i++ )
            {
                ArcenLongTermContinuousPlanningClientOrHostContext context = AllContexts_ClientOrHost[i];
                try
                {
                    if ( context != null && context.GetNeedsToRun() )
                        context.RunOnBackgroundThread( context.NameForDisplay, 10f, //die if it's not done in 10 seconds
                            true ); //FailSilentlyIfNotFinishedYet = yes!  This can happen and it's no big deal, just wait longer
                }
                catch ( ArcenPleaseStopThisThreadException )
                {
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "LongTermContinuousError in '" + ( context == null ? "[null]" : context.MyPermanentThreadName ) + "': " + e, Verbosity.ShowAsError );
                }
            }
        }
    }
}