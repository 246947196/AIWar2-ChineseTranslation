using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public sealed class ZombieAntiEveryoneFactionDeepInfo : ZombieFactionDeepInfoBase, IExternalDeepInfo_Singleton
    {
        public ZombieAntiEveryoneFactionBaseInfo BaseInfo;
        public static ZombieAntiEveryoneFactionDeepInfo Instance = null;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<ZombieAntiEveryoneFactionBaseInfo>();
            Instance = this;
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            Instance = null;
            BaseInfo = null;
        }
    }
}
