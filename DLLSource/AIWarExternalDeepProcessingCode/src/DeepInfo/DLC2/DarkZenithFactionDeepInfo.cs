using System;

using System.Linq;
using System.Text;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public sealed class DarkZenithFactionDeepInfo : DarkZenithFactionDeepInfoRoot, IExternalDeepInfo_Singleton
    {
        public override bool IsSvikari => false;
        public override bool IsHumanSidekick => false;

        public static DarkZenithFactionDeepInfo Instance = null;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<DarkZenithFactionBaseInfoRoot>();
            Instance = this;
        }

        protected override void SubCleanup()
        {
            Instance = null;
        }
    }
}
