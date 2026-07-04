using Arcen.Universal;
using System;

using System.Diagnostics;
using System.Threading;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// These ones run continuously, which you can see an explanation of in the DeepInfo dll 
    /// in the ArcenLongTermContinuousPlanningContext file.
    /// 
    /// Unlike those ones, however, which only run on the host or in single-player, these ones also
    /// run on clients.
    /// </summary>
    public abstract class ArcenLongTermContinuousPlanningClientOrHostContext : ArcenClientOrHostSimPlanningContext
    {
        public override bool IsLongRangePlanning
        {
            get { return true; }
        }

        private static ReferenceTracker RefTracker;
        protected ArcenLongTermContinuousPlanningClientOrHostContext( string MyPermanentThreadName, ArcenSimContextType contextType )
            : base( MyPermanentThreadName, contextType )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ArcenLongTermContinuousPlanningClientOrHostContext" );
            RefTracker.IncrementObjectCount();
        }

        public abstract bool GetNeedsToRun();
    }
}