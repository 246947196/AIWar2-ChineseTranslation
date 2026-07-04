using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    //for the dyson sidekick's ai types
    public class AITypeController_Hateful : BaseAITypeImplementation
    {
        //might want to make some unique structures?
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
            topOfStep = FInt.FromParts( 5, 00 ); //100 AIP
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 150 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 950 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 250 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 150 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 000 );

            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = (FInt)( -1 ); 
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 000 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 300 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 100 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            AIBudgetItem.ReleaseTemporaryBudgetRatioArray( aipRatioForStep );
        }
        //Spawn other structures
        public override void SeedStartingEntitiesForAIType( Faction faction, ArcenHostOnlySimContext Context )
        {
            AISentinelsCoreData factionExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
            byte difficulty = factionExternal.AIDifficulty.Difficulty; //you will probably want to adjust things based on Difficulty

            //we need to find the planets owned by this AI faction (future-proofing for when the player can start with more AI factions)
            List<Planet> ownedPlanets = Planet.GetTemporaryPlanetList( "AITypeController_Hateful-ownedPlanets", 10f );
            if ( ownedPlanets == null ) //blocked for teardown/shutdown; bail
                return;

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.InitialOwningAIFactionIndex == faction.FactionIndex )
                    ownedPlanets.Add( planet );
            }

            //Lets spawn just a couple annoying Minor Alarm Posts
            int alarmCount = ownedPlanets.Count;
            alarmCount /= 10;
            if ( alarmCount < 1 )
                alarmCount = 1;

            ArcenArrays.Randomize( ownedPlanets, Context.RandomToUse );
            ArcenArrays.Randomize( ownedPlanets, Context.RandomToUse );
            ArcenArrays.Randomize( ownedPlanets, Context.RandomToUse );
            for ( int i = 0; i < ownedPlanets.Count; i++ )
            {
                Planet planet = ownedPlanets[i];
                if ( planet.MarkLevelForAIOnly.Ordinal > 1 )
                {
                    //some percentage of the planets get extra alarms; 1/10 for low difficulty, 1/5 for medium, 1/4 for high
                    //Don't put any alarmes on mark 1 planets
                    //alarmes have multiple mark levels, so here set it as the mark level of the planet it spawned on if possible
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "HatefulAlarmPost" );
                    ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 500 ) );
                    GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, planet.MarkLevelForAIOnly.Ordinal, pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "AITypeExtraSpawnsOnStart" );

                    alarmCount--;
                    if ( alarmCount <= 0 )
                        break;
                }
            }
            foreach ( GameEntity_Squad king in faction.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                AISentinelsFactionBaseInfo sentinelsBaseInfo = king.PlanetFaction.Faction.GetAISentinelsCoreData();
                Faction praetorian = sentinelsBaseInfo.SubFac_Praetorian;
                if ( praetorian == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "BUG: no praetorian guard was found while spawning flenser for " + king.PlanetFaction.Faction.GetDisplayName(), Verbosity.DoNotShow );
                    continue;
                }
                Planet planet = king.Planet;
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( praetorian );
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( "HatefulFinalBoss" );
                ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 500 ) );
                GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, planet.MarkLevelForAIOnly.Ordinal, pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "AITypeExtraSpawnsOnStart" );
                ArcenDebugging.LogSingleLine("Seeded "+ entity.ToStringWithPlanet(), Verbosity.DoNotShow );

            }

            Planet.ReleaseTemporaryPlanetList( ownedPlanets );
        }
    }
}
