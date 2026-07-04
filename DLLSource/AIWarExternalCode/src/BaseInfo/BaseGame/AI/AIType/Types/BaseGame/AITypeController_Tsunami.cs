using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class AITypeController_Tsunami : BaseAITypeImplementation
    {
        public override void AllocateSpendingRatios_AnySituation( Faction faction, EnumIndexedArray<AIBudgetType, FInt> aipToQuasiAllocate, FInt aip )
        {
            FInt bottomOfStep;
            FInt topOfStep;

            EnumIndexedArray<AIBudgetType, FInt> aipRatioForStep = AIBudgetItem.GetTemporaryBudgetRatioArray( "AITypeController_Tsunami-AllocateSpendingRatios_AnySituation-aipRatioForStep", 10f );
            if ( aipRatioForStep == null ) //blocked for teardown/shutdown; bail
                return;

            bottomOfStep = FInt.Zero;
            topOfStep = FInt.FromParts( 2, 500 );
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 400 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 150 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 125 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = FInt.FromParts( 7, 500 );
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 400 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 90 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 83 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 160 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 067 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = FInt.FromParts( 12, 000 );
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 350 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 000 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 250 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 075 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 025 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 075 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = FInt.FromParts( 30, 000 );
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 400 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 000 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 025 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 075 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = (FInt)(-1);
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 400 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 000 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 250 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 025 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 075 );
            AllocateAIPWithinStep(  aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            AIBudgetItem.ReleaseTemporaryBudgetRatioArray( aipRatioForStep );
        }
    }
}
