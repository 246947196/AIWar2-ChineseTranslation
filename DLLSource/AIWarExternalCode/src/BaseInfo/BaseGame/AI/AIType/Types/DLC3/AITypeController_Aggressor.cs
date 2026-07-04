using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class AITypeController_Aggressor : BaseAITypeImplementation
    {
        public override void AllocateSpendingRatios_AnySituation( Faction faction, EnumIndexedArray<AIBudgetType, FInt> aipToQuasiAllocate, FInt aip )
        {
            EnumIndexedArray<AIBudgetType, FInt> aipRatioForStep = AIBudgetItem.GetTemporaryBudgetRatioArray( "AITypeController_Aggressor-AllocateSpendingRatios_AnySituation-aipRatioForStep", 10f );
            if ( aipRatioForStep == null ) //blocked for teardown/shutdown; bail
                return;

            FInt bottomOfStep;
            FInt topOfStep;

            /* The bottom/top of steps code here allows the spending to have different ratios depending on how much
               total AI Progress there is. The bottom/stop of steps refer to how many planets you've taken.
               So the AIP from the first 2.5 planets is spent in one way, then the AIP for the next 7.5 planets is spent differently, and so on. */
            bottomOfStep = FInt.Zero;
            topOfStep = FInt.FromParts( 2, 500 );
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 325 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 50 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 150 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 250 );
            aipRatioForStep[AIBudgetType.HunterFleet] = FInt.FromParts( 0, 025 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 150 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = FInt.FromParts( 7, 500 );
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 280 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 045 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 075 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = FInt.FromParts( 12, 000 );
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 175 );
            aipRatioForStep[AIBudgetType.HunterFleet] = FInt.FromParts( 0, 025 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 025 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 350 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 050 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = FInt.FromParts( 30, 000 );
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 150 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 150 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 125 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = (FInt)(-1);
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 075 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 300 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 150 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 050 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            AIBudgetItem.ReleaseTemporaryBudgetRatioArray( aipRatioForStep );
        }
    }
}
