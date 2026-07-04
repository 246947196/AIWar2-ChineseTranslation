using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class AITypeController_Geneticist : BaseAITypeImplementation
    {
        //Spawn a bunch of ScourgeFortresses
        public override void SeedStartingEntitiesForAIType( Faction faction, ArcenHostOnlySimContext Context )
        {
            AISentinelsCoreData factionExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
            byte difficulty = factionExternal.AIDifficulty.Difficulty; //you will probably want to adjust things based on Difficulty

            //we need to find the planets owned by this AI faction (future-proofing for when the player can start with more AI factions
            List<Planet> ownedPlanets = Planet.GetTemporaryPlanetList( "AITypeController_Geneticist-ownedPlanets", 10f );
            if ( ownedPlanets == null ) //blocked for teardown/shutdown; bail
                return;

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.InitialOwningAIFactionIndex == faction.FactionIndex )
                    ownedPlanets.Add( planet );
            }

            int scourgeFortressCount = ownedPlanets.Count;
            if ( difficulty < 3 )
                scourgeFortressCount /= 10;
            else if ( difficulty < 7 )
                scourgeFortressCount /= 5;
            else
                scourgeFortressCount /= 2;
            if ( scourgeFortressCount < 1 )
                scourgeFortressCount = 1;
            scourgeFortressCount += (World_AIW2.Instance.EmpireStylePlayerFactions.Count * 3);

            ArcenArrays.Randomize( ownedPlanets, Context.RandomToUse );
            ArcenArrays.Randomize( ownedPlanets, Context.RandomToUse );
            ArcenArrays.Randomize( ownedPlanets, Context.RandomToUse );

            for ( int i = 0; i < ownedPlanets.Count; i++ )
            {
                Planet planet = ownedPlanets[i];
                if ( planet.MarkLevelForAIOnly.Ordinal > 2 )
                {
                    //some percentage of the planets get extra fortresses; 1/10 for low difficulty, 1/5 for medium, 1/4 for high
                    //Don't put any fortresses on mark 1 planets
                    //fortresses have multiple mark levels, so here set it as the mark level of the planet it spawned on if possible
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ScourgeFortress" );
                    ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 500 ) );
                    GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, (byte)(planet.MarkLevelForAIOnly.Ordinal - 1), pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "AITypeExtraSpawnsOnStart" );

                    scourgeFortressCount--;
                    if ( scourgeFortressCount <= 0 )
                        break;
                }
            }

            Planet.ReleaseTemporaryPlanetList( ownedPlanets );
        }
    }
}
