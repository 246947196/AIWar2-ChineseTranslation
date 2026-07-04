using System;

using System.Linq;
using System.Text;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    // Tamed Macrophage
    // Pretty much the exact same logic as the normal Macrophage, except always allied to the player.
    public sealed class MacrophageTamedFactionDeepInfo : MacrophageFactionDeepInfo, IExternalDeepInfo_Singleton
    {
        public static MacrophageTamedFactionDeepInfo Instance = null;
        public override void SubDoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<MacrophageTamedFactionBaseInfo>();
            Instance = this;
        }

        protected override void SubCleanup()
        {
            Instance = null;
            base.SubCleanup();
        }

        public override void SeedStartingEntities_EarlyMajorFactionClaimsOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            // Do not seed starting entities.
        }
    }
}
