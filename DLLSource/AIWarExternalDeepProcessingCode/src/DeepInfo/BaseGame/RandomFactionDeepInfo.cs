using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public sealed class RandomFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        public RandomFactionBaseInfo BaseInfo;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<RandomFactionBaseInfo>();
        }
        protected override void Cleanup()
        {
            BaseInfo = null;
        }
    }
}
