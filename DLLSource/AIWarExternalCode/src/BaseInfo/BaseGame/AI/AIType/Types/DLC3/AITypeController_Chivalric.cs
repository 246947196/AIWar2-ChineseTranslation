using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    //for the dyson sidekick's ai types
    public class AITypeController_Chivalric : BaseAITypeImplementation
    {
        public override void CalculateCoreImportance(ref float importanceWithAdjustment, GameEntity_Squad attackerEntity, EntitySystem attackerSystem, EntitySystemTypeData attackerSystemTypeData, GameEntity_Squad defenderEntity, NonSimTargetPlanningInfo defenderTargetingInfo, ArcenCharacterBuffer traceBuffer)
        {
            if (defenderEntity == null)
                return;
            FleetMembership mem = defenderEntity.FleetMembership;
            if (mem == null)
                return;
            Fleet fleet = mem.Fleet;
            if (fleet == null)
                return;
            GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
            if (centerpiece == null)
                return;
            if (defenderEntity == centerpiece)
            {
                importanceWithAdjustment /= 100;
            }
        }
    }
}
