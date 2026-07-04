using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    //for the dyson sidekick's ai types
    public class AITypeController_Bandersnatch : BaseAITypeImplementation
    {
        //might want to make some unique structures?
        public override void AllocateSpendingRatios_AnySituation(Faction faction, EnumIndexedArray<AIBudgetType, FInt> aipToQuasiAllocate, FInt aip)
        {
            EnumIndexedArray<AIBudgetType, FInt> aipRatioForStep = AIBudgetItem.GetTemporaryBudgetRatioArray("AITypeController_Aggressor-AllocateSpendingRatios_AnySituation-aipRatioForStep", 10f);
            if ( aipRatioForStep == null ) //blocked for teardown/shutdown; bail
                return;

            FInt bottomOfStep;
            FInt topOfStep;

            /* The bottom/top of steps code here allows the spending to have different ratios depending on how much
               total AI Progress there is. The bottom/stop of steps refer to how many planets you've taken.
               So the AIP from the first 2.5 planets is spent in one way, then the AIP for the next 7.5 planets is spent differently, and so on. */
            bottomOfStep = FInt.Zero;
            topOfStep = FInt.FromParts(2, 00); //40 AIP
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts(0, 100);
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts(0, 100);
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts(0, 100);
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts(0, 250);
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts(0, 100);
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts(0, 100);
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts(0, 250);
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts(0, 000);
            AllocateAIPWithinStep(aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip);

            bottomOfStep = topOfStep;
            topOfStep = FInt.FromParts(5, 00); //100 AIP
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts(0, 150);
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts(0, 150);
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts(0, 100);
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts(0, 300);
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts(0, 100);
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts(0, 100);
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts(0, 050);
            aipRatioForStep[AIBudgetType.HunterFleet] = FInt.FromParts(0, 050);

            AllocateAIPWithinStep(aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip);

            bottomOfStep = topOfStep;
            topOfStep = (FInt)(-1);
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts(0, 350);
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts(0, 200);
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts(0, 100);
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts(0, 050);
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts(0, 100);
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts(0, 050);
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts(0, 050);
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts(0, 100);
            AllocateAIPWithinStep(aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip);

            AIBudgetItem.ReleaseTemporaryBudgetRatioArray(aipRatioForStep);
        }
        //Spawn a bunch of Fortresses
        public override void SeedStartingEntitiesForAIType(Faction faction, ArcenHostOnlySimContext Context)
        {
            AISentinelsCoreData factionExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
            byte difficulty = factionExternal.AIDifficulty.Difficulty; //you will probably want to adjust things based on Difficulty

            //we need to find the planets owned by this AI faction (future-proofing for when the player can start with more AI factions
            List<Planet> ownedPlanets = Planet.GetTemporaryPlanetList("AITypeController_Bandersnatch-ownedPlanets", 10f);
            if ( ownedPlanets == null ) //blocked for teardown/shutdown; bail
                return;

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if (planet.InitialOwningAIFactionIndex == faction.FactionIndex)
                    ownedPlanets.Add(planet);
            }

            int fortressCount = ownedPlanets.Count;
            if (difficulty < 3)
                fortressCount /= 10;
            else if (difficulty < 7)
                fortressCount /= 5;
            else
                fortressCount /= 2;
            if (fortressCount < 1)
                fortressCount = 1;
            fortressCount += (World_AIW2.Instance.EmpireStylePlayerFactions.Count * 4);

            ArcenArrays.Randomize(ownedPlanets, Context.RandomToUse);
            ArcenArrays.Randomize(ownedPlanets, Context.RandomToUse);
            ArcenArrays.Randomize(ownedPlanets, Context.RandomToUse);
            for (int i = 0; i < ownedPlanets.Count; i++)
            {
                Planet planet = ownedPlanets[i];
                if (planet.MarkLevelForAIOnly.Ordinal > 1)
                {
                    //some percentage of the planets get extra fortresses; 1/10 for low difficulty, 1/5 for medium, 1/4 for high
                    //Don't put any fortresses on mark 1 planets
                    //fortresses have multiple mark levels, so here set it as the mark level of the planet it spawned on if possible
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction(faction);
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "BandersnatchFortress");
                    ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter(Context, entityData, FInt.FromParts(0, 150), FInt.FromParts(0, 500));
                    GameEntity_Squad fortress = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(pFaction, entityData, planet.MarkLevelForAIOnly.Ordinal, pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "AITypeExtraSpawnsOnStart");
                    fortressCount--;
                    int turretCount = Context.RandomToUse.Next(2, 6);
                    for (int j = 0; j < turretCount; j++)
                    {
                        GameEntityTypeData turretData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "WormholeInvasionDefense");
                        spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity(Context, turretData, fortress.WorldLocation, FInt.FromParts(0, 010), FInt.FromParts(0, 020));
                        GameEntity_Squad.CreateNew_ReturnNullIfMPClient(pFaction, turretData, planet.MarkLevelForAIOnly.Ordinal, pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "AITypeExtraSpawnsOnStart");
                    }
                    if (fortressCount <= 0)
                        break;
                }
            }

            foreach ( GameEntity_Squad king in faction.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                AISentinelsFactionBaseInfo sentinelsBaseInfo = king.PlanetFaction.Faction.GetAISentinelsCoreData();
                Faction praetorian = sentinelsBaseInfo.SubFac_Praetorian;
                if (praetorian == null)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("BUG: no praetorian guard was found while spawning flenser for " + king.PlanetFaction.Faction.GetDisplayName(), Verbosity.DoNotShow);
                    continue;
                }
                Planet planet = king.Planet;
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction(praetorian);
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName("Bandersnatch");
                int numToSpawn = 3;
                for (int i = 0; i < numToSpawn; i++)
                {
                    ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter(Context, entityData, FInt.FromParts(0, 150), FInt.FromParts(0, 500));
                    GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(pFaction, entityData, planet.MarkLevelForAIOnly.Ordinal, pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "AITypeExtraSpawnsOnStart");
                }

            }

            Planet.ReleaseTemporaryPlanetList(ownedPlanets);
        }
        public override void CalculateCoreImportance(ref float importanceWithAdjustment, GameEntity_Squad attackerEntity, EntitySystem attackerSystem, EntitySystemTypeData attackerSystemTypeData, GameEntity_Squad defenderEntity, NonSimTargetPlanningInfo defenderTargetingInfo, ArcenCharacterBuffer traceBuffer)
        {
            if (defenderEntity == null)
                return;
            if (defenderEntity.TypeData.SpecialType == SpecialEntityType.NormalHumanCommandStation)
            {
                importanceWithAdjustment *= 2;
            }
        }
    }

}
