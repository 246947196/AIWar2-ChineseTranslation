using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Text;

namespace Arcen.AIW2.External
{
    public class AITypeController_Cruel : BaseAITypeImplementation
    {
        public override void CalculateCoreImportance( ref float importanceWithAdjustment, GameEntity_Squad attackerEntity, EntitySystem attackerSystem, EntitySystemTypeData attackerSystemTypeData, GameEntity_Squad defenderEntity, NonSimTargetPlanningInfo defenderTargetingInfo, ArcenCharacterBuffer traceBuffer )
        {
            // Only long-range ships (AboveAverage1 = 8000) get the flagship priority bonus.
            // Short-range ships would otherwise chase flagships past closer targets, making
            // them trivially baitable with transports parked near the flagship.
            if ( attackerSystem.DataForMark.BaseRange < 8000 )
                return;
            if ( defenderEntity == null )
                return;
            FleetMembership mem = defenderEntity.FleetMembership;
            if ( mem == null )
                return;
            Fleet fleet = mem.Fleet;
            if ( fleet == null )
                return;
            GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
            if ( centerpiece == null )
                return;
            if ( defenderEntity == centerpiece )
            {
                // 1200 sits between BigThreateningWeapon and MajorPlanetLevelObjective.
                // We raise low-priority flagships to this floor rather than multiplying,
                // so flagships that are already highly prioritized are unaffected.
                importanceWithAdjustment = Math.Max( importanceWithAdjustment, 1200f );
            }
        }
    }
}
