using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class AITypeController_Lernean : BaseAITypeImplementation
    {
        public override void SeedStartingEntitiesForAIType( Faction faction, ArcenHostOnlySimContext Context )
        {
            AISentinelsCoreData factionExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
            byte difficulty = factionExternal.AIDifficulty.Difficulty; //you will probably want to adjust things based on Difficulty

            //we need to find the planets owned by this AI faction (future-proofing for when the player can start with more AI factions
            List<Planet> ownedPlanets = Planet.GetTemporaryPlanetList( "AITypeController_PeaceMaker-ownedPlanets", 10f );
            if ( ownedPlanets == null ) //blocked for teardown/shutdown; bail
                return;

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.InitialOwningAIFactionIndex == faction.FactionIndex )
                    ownedPlanets.Add( planet );
            }

            int omdCount = ownedPlanets.Count;
            int ionCannonCount = ownedPlanets.Count;
            if ( difficulty < 3 )
                omdCount /= 10;
            else if ( difficulty < 7 )
                omdCount /= 5;
            else
                omdCount /= 2;
            if ( omdCount < 1 )
                omdCount = 1;
            omdCount += (World_AIW2.Instance.EmpireStylePlayerFactions.Count * 3);
            omdCount /= 4; //some extra but not too many
            if ( difficulty < 3 )
                ionCannonCount /= 30;
            else if ( difficulty < 7 )
                ionCannonCount /= 15;
            else
                ionCannonCount /= 12;

            ionCannonCount /= 2;

            if ( ionCannonCount < World_AIW2.Instance.EmpireStylePlayerFactions.Count )
                ionCannonCount = World_AIW2.Instance.EmpireStylePlayerFactions.Count;

            ArcenArrays.Randomize( ownedPlanets, Context.RandomToUse );
            ArcenArrays.Randomize( ownedPlanets, Context.RandomToUse );
            ArcenArrays.Randomize( ownedPlanets, Context.RandomToUse );

            for ( int i = 0; i < ownedPlanets.Count; i++ )
            {
                Planet planet = ownedPlanets[i];
                if ( planet.MarkLevelForAIOnly.Ordinal > 1 )
                {
                    //some percentage of the planets get orbital mass drivers; 1/8 for low difficulty, 1/4 for medium, 1/3 for high
                    //Don't put any drivers on mark 1 planets
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "OrbitalMassDriverPeacemaker" );
                    ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 500 ) );
                    GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ), pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "AITypeExtraSpawnsOnStart" );

                    omdCount--;
                    if ( omdCount <= 0 )
                        break;
                }
            }

            ArcenArrays.Randomize( ownedPlanets, Context.RandomToUse );
            ArcenArrays.Randomize( ownedPlanets, Context.RandomToUse );
            ArcenArrays.Randomize( ownedPlanets, Context.RandomToUse );

            for ( int i = 0; i < ownedPlanets.Count; i++ )
            {
                Planet planet = ownedPlanets[i];
                if ( planet.MarkLevelForAIOnly.Ordinal > 1 )
                {
                    //some percentage of the planets get ion cannons; 1/24 for low difficulty, 1/12 for medium, 1/9 for high
                    //Don't put any cannons on mark 1 planets
                    //ion cannons have multiple mark levels, so here set it as the mark level of the planet it spawned on if possible
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "IonCannon" );
                    ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 500 ) );
                    GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, planet.MarkLevelForAIOnly.Ordinal, pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "AITypeExtraSpawnsOnStart" );

                    ionCannonCount--;
                    if ( ionCannonCount <= 0 )
                        break;
                }
            }

            Planet.ReleaseTemporaryPlanetList( ownedPlanets );
        }
    }
}
