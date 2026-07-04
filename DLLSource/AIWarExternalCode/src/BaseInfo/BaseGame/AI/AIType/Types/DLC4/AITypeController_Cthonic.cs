using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class AITypeController_Cthonic : BaseAITypeImplementation
    {
        public override void AllocateSpendingRatios_AnySituation( Faction faction, EnumIndexedArray<AIBudgetType, FInt> aipToQuasiAllocate, FInt aip )
        {
            EnumIndexedArray<AIBudgetType, FInt> aipRatioForStep = AIBudgetItem.GetTemporaryBudgetRatioArray( "AITypeController_Cthonic-AllocateSpendingRatios_AnySituation-aipRatioForStep", 10f );
            if ( aipRatioForStep == null ) //blocked for teardown/shutdown; bail
                return;

            FInt bottomOfStep;
            FInt topOfStep;

            bottomOfStep = FInt.Zero;
            topOfStep = FInt.FromParts( 1, 00 ); //20 AIP
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 500 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 300 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 000 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 000 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = FInt.FromParts( 2, 00 ); //40 AIP
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.HunterFleet] = FInt.FromParts( 0, 100 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = FInt.FromParts( 4, 00 ); //80 AIP
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.HunterFleet] = FInt.FromParts( 0, 100 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = (FInt)( -1 );
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 250 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 150 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 100 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            AIBudgetItem.ReleaseTemporaryBudgetRatioArray( aipRatioForStep );
        }

        public override void SeedStartingEntitiesForAIType( Faction faction, ArcenHostOnlySimContext Context )
        {
            foreach ( GameEntity_Squad king in faction.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                AISentinelsFactionBaseInfo sentinelsBaseInfo = king.PlanetFaction.Faction.GetAISentinelsCoreData();
                Faction praetorian = sentinelsBaseInfo.SubFac_Praetorian;
                if ( praetorian == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "BUG: no praetorian guard found while spawning cthonic praetorian for " + king.PlanetFaction.Faction.GetDisplayName(), Verbosity.DoNotShow );
                    continue;
                }
                Planet planet = king.Planet;
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( praetorian );
                foreach ( string praetorianTypeName in new[] { "CthonicPraetorianLeader", "CthonicAegis", "CthonicHarbinger" } )
                {
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( praetorianTypeName );
                    if ( entityData == null )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "BUG: Cthonic praetorian seed could not find type " + praetorianTypeName, Verbosity.DoNotShow );
                        continue;
                    }
                    ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 500 ) );
                    GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, planet.MarkLevelForAIOnly.Ordinal, pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "AITypeExtraSpawnsOnStart" );
                    if ( entity != null )
                    {
                        entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                        ArcenDebugging.LogSingleLine( "Seeded " + entity.ToStringWithPlanet(), Verbosity.DoNotShow );
                    }
                }
            }
        }
    }
}
