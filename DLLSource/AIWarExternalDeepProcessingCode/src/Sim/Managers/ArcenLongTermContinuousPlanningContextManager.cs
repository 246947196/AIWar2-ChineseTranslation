using Arcen.Universal;
using System;

using System.Diagnostics;
using System.Threading;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public static class ArcenLongTermContinuousPlanningContextManager
    {
        public static readonly List<ArcenLongTermContinuousPlanningContext> NonFactionContexts_ServerAndUnpausedOnly =
            List<ArcenLongTermContinuousPlanningContext>.Create_WillNeverBeGCed( 30, "ArcenLongTermContinuousPlanningContextManager-NonFactionContexts_ServerAndUnpausedOnly" );

        private static ReferenceTracker RefTracker;
        static ArcenLongTermContinuousPlanningContextManager()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ArcenLongTermContinuousPlanningContextManager" );
            RefTracker.IncrementObjectCount();
        }

        public static void CleanupAll()
        {
            //for ( int i = 0; i < NonFactionContexts_ServerAndUnpausedOnly.Count; i++ )
            //    NonFactionContexts_ServerAndUnpausedOnly[i].Cleanup();

            //we can just clear these, no big deal, because they automatically got pulled back into the pool
            //we don't have to actually make them go back into the pool ourselves, or need a protected list, or any of that.
            NonFactionContexts_ServerAndUnpausedOnly.Clear();
        }

        private static void InitializeIfNeeded()
        {
            lock ( NonFactionContexts_ServerAndUnpausedOnly )
            {
                if ( NonFactionContexts_ServerAndUnpausedOnly.Count > 0 )
                    return;

                AddContext( "_Deep.DecollisionPlanning_Player", DecollisionPlanning_Player.GetFromPoolOrCreate() );
                AddContext( "_Deep.DecollisionPlanning_NPC_A", DecollisionPlanning_NPC_A.GetFromPoolOrCreate() );
                AddContext( "_Deep.DecollisionPlanning_NPC_B", DecollisionPlanning_NPC_B.GetFromPoolOrCreate() );
                AddContext( "_Deep.TargetListPlanning_Player", TargetListPlanning_Player.GetFromPoolOrCreate() );
                AddContext( "_Deep.TargetListPlanning_NPC_A", TargetListPlanning_NPC_A.GetFromPoolOrCreate() );
                AddContext( "_Deep.TargetListPlanning_NPC_B", TargetListPlanning_NPC_B.GetFromPoolOrCreate() );
                AddContext( "_Deep.TargetListPlanning_NPC_C", TargetListPlanning_NPC_C.GetFromPoolOrCreate() );
                AddContext( "_Deep.TargetListPlanning_NPC_D", TargetListPlanning_NPC_D.GetFromPoolOrCreate() );
                AddContext( "_Deep.SquadStackPlanning", SquadStackPlanning.GetFromPoolOrCreate() );
            }
        }

        private static void AddContext( string Name, ArcenLongTermContinuousPlanningContext Cont )
        {
            if ( Cont == null )
                return;
            Cont.MyThreadNameThatChangesPerRun = Name;

            //this is the one that matters for execution
            NonFactionContexts_ServerAndUnpausedOnly.Add( Cont );

            //this is just so that the UI can see it.
            ArcenVariousLongTermContextManager.AllContexts_HostContinuous.Add( Cont );
        }

        public static void RunAllContexts_ForUnpausedOnly()
        {
            if ( CentralVars.GetShouldNotRunGameStyleLogic() )
                return;

            bool debugging = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Thread );
            if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "ArcenLongTermContinuousPlanningContextManager.RunAllContexts()", Verbosity.DoNotShow );

            if ( World_AIW2.Instance.IsOutsideOfNormalGameplay )
            {
                if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "\t" + "skipping because !World_AIW2.Instance.HasEverBeenUnpaused", Verbosity.DoNotShow );
                return;
            }
            if ( ArcenNetworkAuthority.GetIsClientMode() )
                return;

            InitializeIfNeeded();

            DoLongTermPlanning( NonFactionContexts_ServerAndUnpausedOnly );
        }

        private static void DoLongTermPlanning( List<ArcenLongTermContinuousPlanningContext> ContextsToProcess )
        {
            try
            {
                bool debugging = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Thread );
                if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "ArcenLongTermContinuousPlanningContextManager.DoLongTermPlanning();", Verbosity.DoNotShow );

                for ( int nonFactionIndex = 0; nonFactionIndex < ContextsToProcess.Count; nonFactionIndex++ )
                {
                    ArcenLongTermContinuousPlanningContext context = null;
                    try
                    {
                        context = ContextsToProcess[nonFactionIndex];
                        Helper_CheckForRunningNonFactionThread( context );
                    }
                    catch ( ArcenPleaseStopThisThreadException )
                    {
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "LongTermContinuousError: " + e, Verbosity.ShowAsError );
                    }
                }
            }
            catch ( ArcenPleaseStopThisThreadException )
            {
                //do nothing -- these are valid out here, and will be reported on the interior if there is a problem!
            }
            finally
            {
            }
        }

        private static void Helper_CheckForRunningNonFactionThread( ArcenLongTermContinuousPlanningContext context )
        {
            if ( context == null )
                return;

            if ( context.GetNeedsToRun() )
                context.RunOnBackgroundThread( context.MyThreadNameThatChangesPerRun, true, null ); //FailSilentlyIfNotFinishedYet = yes!  This can happen and it's no big deal, just wait longer
        }

        //public static float timeUntilNextCheck = 0;
        //public static bool GetAreAllLongRangeContextsNotRunning()
        //{
        //    timeUntilNextCheck -= Engine_Universal.UnscaledDeltaTime;
        //    if ( timeUntilNextCheck > 0 )
        //        return false;
        //    timeUntilNextCheck = 0.05f;
        //    //if ( !MessageFromMeToMainThread_AllPlanningContextsStarted )
        //    //    return false;
        //    if ( World_AIW2.Instance.IsOutsideOfNormalGameplay )
        //        return true;
        //    for ( int i = 0; i < NonFactionContexts_ServerAndUnpausedOnly.Count; i++ )
        //    {
        //        ArcenLongTermContinuousPlanningContext context = NonFactionContexts_ServerAndUnpausedOnly[i];
        //        if ( context.IsRunning )
        //            return false;
        //    }
        //    return true;
        //}
    }
}