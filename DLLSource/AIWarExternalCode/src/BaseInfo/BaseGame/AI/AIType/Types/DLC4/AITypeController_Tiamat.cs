using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class AITypeController_Tiamat : BaseAITypeImplementation
    {
        public override void AllocateSpendingRatios_AnySituation( Faction faction, EnumIndexedArray<AIBudgetType, FInt> aipToQuasiAllocate, FInt aip )
        {
            EnumIndexedArray<AIBudgetType, FInt> aipRatioForStep = AIBudgetItem.GetTemporaryBudgetRatioArray( "AITypeController_Tiamat-AllocateSpendingRatios_AnySituation-aipRatioForStep", 10f );

            FInt bottomOfStep;
            FInt topOfStep;

            bottomOfStep = FInt.Zero;
            topOfStep = FInt.FromParts( 1, 00 );
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 400 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 000 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = FInt.FromParts( 2, 00 );
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.HunterFleet] = FInt.FromParts( 0, 100 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = FInt.FromParts( 4, 00 );
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 150 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 250 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.HunterFleet] = FInt.FromParts( 0, 100 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = (FInt)( -1 );
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 250 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 100 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            AIBudgetItem.ReleaseTemporaryBudgetRatioArray( aipRatioForStep );
        }
    }
}
