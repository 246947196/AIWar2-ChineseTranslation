using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class AITypeController_Turtle : BaseAITypeImplementation
    {
        public override void AllocateSpendingRatios_AnySituation( Faction faction, EnumIndexedArray<AIBudgetType, FInt> aipToQuasiAllocate, FInt aip )
        {
            FInt bottomOfStep;
            FInt topOfStep;
            EnumIndexedArray<AIBudgetType, FInt> aipRatioForStep = AIBudgetItem.GetTemporaryBudgetRatioArray( "AITypeController_Turtle-AllocateSpendingRatios_AnySituation-aipRatioForStep", 10f );
            if ( aipRatioForStep == null ) //blocked for teardown/shutdown; bail
                return;

            bottomOfStep = FInt.Zero;
            topOfStep = FInt.FromParts( 2, 500 );
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 175 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 425 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 150 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 100 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = FInt.FromParts( 7, 500 );
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 225 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 83 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 067 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = FInt.FromParts( 12, 000 );
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 325 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 025 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 050 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = FInt.FromParts( 30, 000 );
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 400 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 150 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 050 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = (FInt)(-1);
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 000 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 400 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 250 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 050 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            AIBudgetItem.ReleaseTemporaryBudgetRatioArray( aipRatioForStep );
        }

        public override void SeedStartingEntitiesForAIType( Faction faction, ArcenHostOnlySimContext Context )
        {
            AISentinelsCoreData factionExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
            byte difficulty = factionExternal.AIDifficulty.Difficulty; //you will probably want to adjust things based on Difficulty

            //we need to find the planets owned by this AI faction (future-proofing for when the player can start with more AI factions
            List<Planet> ownedPlanets = Planet.GetTemporaryPlanetList( "AITypeController_Turtle-ownedPlanets", 10f );
            if ( ownedPlanets == null ) //blocked for teardown/shutdown; bail
                return;

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.InitialOwningAIFactionIndex == faction.FactionIndex )
                    ownedPlanets.Add( planet );
            }

            int specialTurtleSpawnCount = ownedPlanets.Count;
            if ( difficulty < 3 )
                specialTurtleSpawnCount /= 10;
            else if ( difficulty < 7 )
                specialTurtleSpawnCount /= 5;
            else
                specialTurtleSpawnCount /= 2;
            if ( specialTurtleSpawnCount < 1 )
                specialTurtleSpawnCount = 1;
            specialTurtleSpawnCount += (World_AIW2.Instance.EmpireStylePlayerFactions.Count * 3);

            ArcenArrays.Randomize( ownedPlanets, Context.RandomToUse );
            ArcenArrays.Randomize( ownedPlanets, Context.RandomToUse );
            ArcenArrays.Randomize( ownedPlanets, Context.RandomToUse );

            for ( int i = 0; i < ownedPlanets.Count; i++ )
            {
                Planet planet = ownedPlanets[i];
                if ( planet.MarkLevelForAIOnly.Ordinal > 1 )
                {
                    //some percentage of the planets get troop accelerators; 1/10 for low difficulty, 1/5 for medium, 1/4 for high
                    //Don't put any accelerators on mark 1 planets
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "TurtleSpecialSpawn" );
                    ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 500 ) );
                    GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ), pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "AITypeExtraSpawnsOnStart" );

                    specialTurtleSpawnCount--;
                    if ( specialTurtleSpawnCount <= 0 )
                        break;
                }
            }

            Planet.ReleaseTemporaryPlanetList( ownedPlanets );
        }
    }
}
