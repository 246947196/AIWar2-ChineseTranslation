using System;

using System.Linq;
using System.Text;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public sealed class DarkZenithSvikariFactionDeepInfo : DarkZenithFactionDeepInfoRoot
    {
        public override bool IsSvikari => true;
        public override bool IsHumanSidekick => false;

        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<DarkZenithFactionBaseInfoRoot>();
        }

        protected override void SubCleanup()
        {
        }
    }
}
