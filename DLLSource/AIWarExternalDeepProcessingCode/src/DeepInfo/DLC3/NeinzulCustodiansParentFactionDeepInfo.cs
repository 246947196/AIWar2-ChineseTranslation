using System;

using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public sealed class NeinzulCustodiansParentFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        public NeinzulCustodiansParentFactionBaseInfo BaseInfo;

        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<NeinzulCustodiansParentFactionBaseInfo>();
        }

        protected override void Cleanup()
        {
            BaseInfo = null;
        }

        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            // At this point, all other seeding logic is completed, so do cleanup.
            BaseInfo.enclaveTypes.Clear();
        }
    }
}
