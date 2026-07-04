using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Linq;
using System.Text;

namespace Arcen.AIW2.External
{
    public class AIDefensePlacer_ShieldNinny : AIDefensePlacer_Default
    {
        public AIDefensePlacer_ShieldNinny()
        {
            this.OnlyOneShieldPerReinforceablePerPass = false;
        }
        
        //public override void DoInitialOrReconquestDefenseSeeding( ArcenSimContext Context, Planet ThisPlanet )
        //{
        //    GameEntity_Squad controller = ThisPlanet.GetController();

        //    this.DoInitialOrReconquest_DefenseSeeding_Guardians( Context, ThisPlanet, List<ArcenPoint>.Create_WillNeverBeGCed() );

        //    Faction faction = controller.GetFactionOrNull_Safe();

        //    FInt turretAIToPurchaseCostBudget = this.GetAICostPurchaseCapForBudgetType( ThisPlanet, faction, ReinforcementType.Turret ) / 2;
        //    FInt shieldAIToPurchaseCostBudget = this.GetAICostPurchaseCapForBudgetType( ThisPlanet, faction, ReinforcementType.Shield ) / 2;
        //    FInt fleetShipAIToPurchaseCostBudget = this.GetAICostPurchaseCapForBudgetType( ThisPlanet, faction, ReinforcementType.Strikecraft ) / 2;

        //    turretAIToPurchaseCostBudget *= FInt.FromParts( 0, 667 );
        //    fleetShipAIToPurchaseCostBudget *= FInt.FromParts( 0, 667 );
        //    shieldAIToPurchaseCostBudget *= 2;

        //    this.Reinforce( Context, ThisPlanet, faction, shieldAIToPurchaseCostBudget, ReinforcementType.Shield );
        //    this.Reinforce( Context, ThisPlanet, faction, turretAIToPurchaseCostBudget, ReinforcementType.Turret );
        //    this.Reinforce( Context, ThisPlanet, faction, fleetShipAIToPurchaseCostBudget, ReinforcementType.Strikecraft );
        //}

        //protected override FInt GetAICostPurchaseCapForBudgetType(Planet planet, Faction faction, ReinforcementType reinforcementType)
        //{
        //    FInt baseValue = base.GetAICostPurchaseCapForBudgetType( planet, faction, reinforcementType );
        //    switch ( reinforcementType )
        //    {
        //        case ReinforcementType.Strikecraft:
        //            baseValue *= FInt.FromParts( 0, 667 );
        //            break;
        //        case ReinforcementType.Turret:
        //            baseValue *= FInt.FromParts( 0, 667 );
        //            break;
        //        case ReinforcementType.Shield:
        //            baseValue *= 2;
        //            break;
        //    }
        //    return baseValue;
        //}
    }
}
