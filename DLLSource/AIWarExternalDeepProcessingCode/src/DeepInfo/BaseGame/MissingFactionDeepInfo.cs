using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// This is used for factions that can't be found for some reason to keep them from erroring.
    /// Usually they would be something in a quick start that doesn't exist in the local installation.
    /// An expansion discoverable faction or faction in general would be one good example.
    /// </summary>
    public sealed class MissingFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        //public MissingFactionBaseInfo BaseInfo;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            //this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<MissingFactionBaseInfo>();
        }

        protected override void Cleanup()
        {
            //BaseInfo = null;
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 20;
    }
}
