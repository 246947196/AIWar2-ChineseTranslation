using Arcen.Universal;
using System;

using System.Diagnostics;
using System.Threading;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public abstract class ArcenLongTermIntermittentPlanningContextBase : ArcenHostOnlySimPlanningContext, ILongRangePlanningHostContext
    {
        private static ReferenceTracker RefTracker;
        protected ArcenLongTermIntermittentPlanningContextBase( ArcenSimContextType contextType )
            : base( contextType )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ArcenLongTermIntermittentPlanningContextBase" );
            RefTracker.IncrementObjectCount();
        }

        public abstract string NameForDisplay { get; }

        public abstract float GetLastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective();
        public abstract int GetMillisecondsAfterWhichToWarn_OfLongRunning();
        public abstract float GetTimeAfterWhichToWarn_OfNotRunning();
    }
}