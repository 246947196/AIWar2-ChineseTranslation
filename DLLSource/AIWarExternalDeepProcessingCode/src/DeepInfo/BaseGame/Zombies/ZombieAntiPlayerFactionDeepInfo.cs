using Arcen.AIW2.Core;
using Arcen.AIW2.External.BulkPathfinding;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public sealed class ZombieAntiPlayerFactionDeepInfo : ZombieFactionDeepInfoBase, IExternalDeepInfo_Singleton, IBulkPathfinding
    {
        public ZombieAntiPlayerFactionBaseInfo BaseInfo;
        public static ZombieAntiPlayerFactionDeepInfo Instance = null;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<ZombieAntiPlayerFactionBaseInfo>();
            Instance = this;
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            Instance = null;
            BaseInfo = null;
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            this.RebuildConflictPlanetsList( ( planet ) => planet.GetControllingFactionType() != FactionType.AI );
            this.PrepareConflictPlanetMovementLogic( Context );
            this.ExecuteWormholeCommands( Context );
            this.ExecuteMovementCommands( Context );
        }
    }
}
