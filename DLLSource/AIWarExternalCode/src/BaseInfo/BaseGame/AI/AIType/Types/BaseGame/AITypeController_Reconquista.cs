using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class AITypeController_Reconquista : BaseAITypeImplementation
    {
        public override void AllocateSpendingRatios_AnySituation( Faction Faction, EnumIndexedArray<AIBudgetType, FInt> aipToQuasiAllocate, FInt aip )
        {
            FInt bottomOfStep;
            FInt topOfStep;

            EnumIndexedArray<AIBudgetType, FInt> aipRatioForStep = AIBudgetItem.GetTemporaryBudgetRatioArray( "AITypeController_Reconquista-AllocateSpendingRatios_AnySituation-aipRatioForStep", 10f );
            if ( aipRatioForStep == null ) //blocked for teardown/shutdown; bail
                return;

            bottomOfStep = FInt.Zero;
            topOfStep = (FInt)(-1);
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 025 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 025 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 500 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 025 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            AIBudgetItem.ReleaseTemporaryBudgetRatioArray( aipRatioForStep );
        }
    }
}
