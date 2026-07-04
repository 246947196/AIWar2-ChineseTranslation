using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public sealed class DysonSphereAntagonizedFactionDeepInfo : ExternalFactionDeepInfoRoot, IExternalDeepInfo_Singleton
    {
        // Deprecated. Everything here is for old save compatibility.

        public DysonSphereAntagonizedFactionBaseInfo BaseInfo;
        public static DysonSphereAntagonizedFactionDeepInfo Instance = null;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<DysonSphereAntagonizedFactionBaseInfo>();
            Instance = this;
        }

        protected override void Cleanup()
        {
            Instance = null;
            BaseInfo = null;
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 2;

    }
}
