using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class AITypeController_OneWayDoormaster : BaseAITypeImplementation
    {
        //To have an AI type spawn units at game start time, define a controllertype class for that ai type.
        //tell that AI type to use the given controllertype, then override the SeedStartingEntitiesForAIType.
        //In that function, figure out which planets are owned by this AI, then on some percentage of them seed whatever units or combinations of
        //units you want
        public override void SeedStartingEntitiesForAIType( Faction faction, ArcenHostOnlySimContext Context )
        {
            AISentinelsCoreData factionExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
            byte difficulty = factionExternal.AIDifficulty.Difficulty; //you will probably want to adjust things based on Difficulty

            //we need to find the planets owned by this AI faction (future-proofing for when the player can start with more AI factions
            List<Planet> ownedPlanets = Planet.GetTemporaryPlanetList( "AITypeController_OneWayDoormaster-ownedPlanets", 10f );
            if ( ownedPlanets == null ) //blocked for teardown/shutdown; bail
                return;

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.InitialOwningAIFactionIndex == faction.FactionIndex )
                    ownedPlanets.Add( planet );
            }

            int fortressCount = ownedPlanets.Count;
            if ( difficulty < 3 )
                fortressCount /= 10;
            else if ( difficulty < 7 )
                fortressCount /= 5;
            else
                fortressCount /= 2;
            if ( fortressCount < 1 )
                fortressCount = 1;
            fortressCount += (World_AIW2.Instance.EmpireStylePlayerFactions.Count * 3);

            ArcenArrays.Randomize( ownedPlanets, Context.RandomToUse );
            ArcenArrays.Randomize( ownedPlanets, Context.RandomToUse );
            ArcenArrays.Randomize( ownedPlanets, Context.RandomToUse );

            for ( int i = 0; i < ownedPlanets.Count; i++ )
            {
                Planet planet = ownedPlanets[i];
                if ( planet.MarkLevelForAIOnly.Ordinal > 1 )
                {
                    //some percentage of the planets get black hole machines; 1/10 for low difficulty, 1/5 for medium, 1/4 for high
                    //Don't put any machines on mark 1 planets
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "OneWayDoormasterSpecialSpawn" );
                    ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 500 ) );
                    GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ), pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "AITypeExtraSpawnsOnStart" );
                    //ArcenDebugging.ArcenDebugLogSingleLine( entityData.InternalName + " seeding on " + planet.Name, Verbosity.DoNotShow );

                    fortressCount--;
                    if ( fortressCount <= 0 )
                        break;
                }
            }

            Planet.ReleaseTemporaryPlanetList( ownedPlanets );
        }
    }
}
