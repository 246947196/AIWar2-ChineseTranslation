using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class AITypeController_Tutorial_01 : BaseAITypeImplementation
    {
        private static AIDefensePlacer _OnlyDefensePlacer;
        private static AIDefensePlacer OnlyDefensePlacer
        {
            get
            {
                if ( _OnlyDefensePlacer == null )
                    _OnlyDefensePlacer = AIDefensePlacerTable.Instance.GetRowByName( "SoloStrongPoint_Central" );
                return _OnlyDefensePlacer;
            }
        }

        public override AIDefensePlacer GetAIDefensePlacerForPlanet( Faction faction, Planet planet, ArcenHostOnlySimContext Context )
        {
            return OnlyDefensePlacer;
        }

        public override void AssignDefenseValuesTo_HostOnly( List<Planet> planets, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return;
            int debugStage = 0;
            try
            {
                debugStage = 100;
                int max = 5;
                int min = 1;
                for ( int i = 0; i < planets.Count; i++ )
                {
                    debugStage = 200;
                    Planet planet = planets[i];
                    debugStage = 400;
                    switch ( planet.PopulationType )
                    {
                        case PlanetPopulationType.HumanHomeworld:
                        case PlanetPopulationType.NonHomeworld:
                            planet.MarkLevelForAIOnly = Balance_MarkLevelTable.Instance.RowsByOrdinal[min];
                            break;
                        case PlanetPopulationType.AIHomeworld:
                        case PlanetPopulationType.AIBastionWorld:
                            planet.MarkLevelForAIOnly = Balance_MarkLevelTable.Instance.RowsByOrdinal[max];
                            break;
                    }
                    debugStage = 500;
                    //there are 3 planets in this tutorial. Make sure the last planet is Mark 2
                    if ( i == 2 )
                        planet.MarkLevelForAIOnly = Balance_MarkLevelTable.Instance.RowsByOrdinal[2];

                    debugStage = 600;
                    //Make Sure All The GuardPostAndCommandPlacers Are Set
                    planet.SetGuardPostAndCommandPlacerFromPlanetStats( 1, Context );
                }
            }
            catch ( Exception e )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "AIType.AssignDefenseValuesTo_HostOnly exception at debugStage " + debugStage + ", exception: " + e );
            }
        }

        public override void AllocateSpendingRatios_AnySituation( Faction faction, EnumIndexedArray<AIBudgetType, FInt> aipToQuasiAllocate, FInt ataip )
        {
            FInt bottomOfStep;
            FInt topOfStep;

            EnumIndexedArray<AIBudgetType, FInt> aipRatioForStep = AIBudgetItem.GetTemporaryBudgetRatioArray( "AITypeController_Tutorial_01-AllocateSpendingRatios_AnySituation-aipRatioForStep", 10f );
            if ( aipRatioForStep == null ) //blocked for teardown/shutdown; bail
                return;

            bottomOfStep = FInt.Zero;
            topOfStep = (FInt)(-1);
            //            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 1, 000 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 000 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, ataip );

            AIBudgetItem.ReleaseTemporaryBudgetRatioArray( aipRatioForStep );
        }

    }
}
