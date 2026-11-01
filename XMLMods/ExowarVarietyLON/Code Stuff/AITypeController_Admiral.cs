using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LONAdmiral
{
    public class AITypeController_Admiral : BaseAITypeImplementation
    {


        //To have an AI type spawn units at game start time, define a controllertype class for that ai type.
        //tell that AI type to use the given controllertype, then override the SeedStartingEntitiesForAIType.
        //In that function, figure out which planets are owned by this AI, then on some percentage of them seed whatever units or combinations of
        //units you want
        public override void SeedStartingEntitiesForAIType(Faction faction, ArcenHostOnlySimContext Context)
        {

            //your code here, if you want it to happen first.  Or part of your code
            base.SeedStartingEntitiesForAIType(faction, Context); //this runs the underlying code from us, always fully updated.
                                                                  //more of your code here, if you want it to happen after, or more part of your code.

            AISentinelsCoreData factionExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
            byte difficulty = factionExternal.AIDifficulty.Difficulty; //you will probably want to adjust things based on Difficulty

            //we need to find the planets owned by this AI faction (future-proofing for when the player can start with more AI factions
            List<Planet> ownedPlanets = Planet.GetTemporaryPlanetList( "AITypeController_Admiral-ownedPlanets", 10f );
            if ( ownedPlanets == null ) //blocked for teardown/shutdown; bail
                return;

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if (planet.InitialOwningAIFactionIndex == faction.FactionIndex)
                    ownedPlanets.Add(planet);
            }

            ArcenArrays.Randomize(ownedPlanets, Context.RandomToUse);
            ArcenArrays.Randomize(ownedPlanets, Context.RandomToUse);
            ArcenArrays.Randomize(ownedPlanets, Context.RandomToUse);

            // Figure out how many Lairs and Exos we need to spawn.

            // For Difficulty 1 - 5, No Lairs.
            // For Difficulty 6 and 7, 1 Lair on one random Mk7 Planet. (So Homeworld or Bastion World, unless I've massively misunderstood something.)
            // For Difficulty 8 and 9, 1 Lair on two random Mk7 Planets.
            // For Difficulty 10, 1 lair on all three Mk7 Planets. (Admiral has 2 Bastion Worlds in total.)


            // For the exowar units:
            // For Difficulty 1 - 4, No Extra Units.
            // For Difficulty 4 - 6, 1 Tier 1 Exowar unit on all three Mk7 planets.
            // For Difficulty 7, 1 Tier 2 Exowar unit on all three Mk7 planets.
            // For Difficulty 8, 1 Tier 1 Exowar unit and 1 Extra Tier 2 Exowar unit on all three Mk7 planets.
            // For Difficulty 9 and 10, 1 Tier 1 Exowar unit on all three Mk7 planets.Then, 1 Tier 2 Exowar unit on two of the Mk7 planets, and a Tier 3 Exowar unit on the remaining planet.


            int NumberOfLONRearAdmiralExowarLair = 0;
            int NumberOfTier1Exos = 0;
            int NumberOfTier2Exos = 0;
            int NumberOfTier3Exos = 0;

            switch ( difficulty )
            {
                case 10:
                    NumberOfLONRearAdmiralExowarLair = 3;
                    NumberOfTier1Exos = 3;
                    NumberOfTier2Exos = 2;
                    NumberOfTier3Exos = 1;
                    break;
                case 9:
                    NumberOfLONRearAdmiralExowarLair = 2;
                    NumberOfTier1Exos = 3;
                    NumberOfTier2Exos = 2;
                    NumberOfTier3Exos = 1;
                    break;
                case 8:
                    NumberOfLONRearAdmiralExowarLair = 2;
                    NumberOfTier1Exos = 3;
                    NumberOfTier2Exos = 3;
                    break;
                case 7:
                    NumberOfLONRearAdmiralExowarLair = 1;
                    NumberOfTier2Exos = 3;
                    break;
                case 6:
                    NumberOfLONRearAdmiralExowarLair = 1;
                    NumberOfTier1Exos = 3;
                    break;
                case 5:
                    NumberOfTier1Exos = 3;
                    break;
                case 4:
                    break;
                case 3:
                    break;
                case 2:
                    break;
                case 1:
                    break;
            }

            // Lairs...
            // use the tag appropriate for this difficulty
            // prefering the highest available which isn't above our difficulty
            GameEntityTypeData LONRearAdmiralExowarLairEntityData = null;
            for (int i = difficulty; i >= 1; i--)
            {
                var key = "LONRearAdmiralExowarLair_Diff" + i;
                if (GameEntityTypeDataTable.Instance.RowsByTag.CheckIfAlreadyHasKey(key))
                {
                    LONRearAdmiralExowarLairEntityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, key);

                    break;
                }
            }


            for (int i = 0; i < ownedPlanets.Count; i++)
            {
                Planet planet = ownedPlanets[i];



                // Lair spawning here.

                if ( planet.MarkLevelForAIOnly.Ordinal >= 7 && NumberOfLONRearAdmiralExowarLair > 0 )
                {
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );

                    ArcenPoint LONRearAdmiralExowarLairSpawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, LONRearAdmiralExowarLairEntityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 500 ) );

                    GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, LONRearAdmiralExowarLairEntityData, LONRearAdmiralExowarLairEntityData.MarkFor( pFaction ), pFaction.FleetUsedAtPlanet, 0, LONRearAdmiralExowarLairSpawnLocation, Context, "Admiral-LONRearAdmiralExowarLair" );



                    NumberOfLONRearAdmiralExowarLair--;
                }


                // Exo spawning here.


                // All the requirements spawn a Tier 1 on every Mark 7 planet, so no special cases needed here.

                if ( planet.MarkLevelForAIOnly.Ordinal >= 7 && NumberOfTier1Exos > 0 )
                {
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );



                    GameEntityTypeData LONRearAdmiralExowarTier1EntityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ExtragalacticWarTier1" );

                    ArcenPoint LONRearAdmiralExowarTier1SpawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, LONRearAdmiralExowarTier1EntityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 500 ) );

                    GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, LONRearAdmiralExowarTier1EntityData, LONRearAdmiralExowarTier1EntityData.MarkFor( pFaction ), pFaction.FleetUsedAtPlanet, 0, LONRearAdmiralExowarTier1SpawnLocation, Context, "Admiral-LONRearAdmiralExowarLair" );

                    entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Guard_Guardian_Patrolling ); //works great!  mapgen
                    GameEntity_Squad commandStation = planet.GetCommandStationOrNull();
                    entity.GuardedUnit = LazyLoadSquadWrapper.Create( commandStation );
                    entity.GuardOrPatrolOffsetPoints.Add( entity.WorldLocation - commandStation.WorldLocation );

                    NumberOfTier1Exos--;
                }


                // Note: This one only works under Difficulty 9. This one puts a Tier 2 on all the Mark 7 planets, while the desired setup for Difficulty 9 and 10 is that a Tier 2 is only on the Bastions, and a Tier 3 is on the Homeworld.

                if ( planet.MarkLevelForAIOnly.Ordinal >= 7 && NumberOfTier2Exos > 0 && difficulty < 9 )
                {
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );



                    GameEntityTypeData LONRearAdmiralExowarTier2EntityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ExtragalacticWarTier2" );

                    ArcenPoint LONRearAdmiralExowarTier2SpawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, LONRearAdmiralExowarTier2EntityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 500 ) );

                    GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, LONRearAdmiralExowarTier2EntityData, LONRearAdmiralExowarTier2EntityData.MarkFor( pFaction ), pFaction.FleetUsedAtPlanet, 0, LONRearAdmiralExowarTier2SpawnLocation, Context, "Admiral-LONRearAdmiralExowarLair" );

                    entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Guard_Guardian_Patrolling ); //works great!  mapgen
                    GameEntity_Squad commandStation = planet.GetCommandStationOrNull();
                    entity.GuardedUnit = LazyLoadSquadWrapper.Create( commandStation );
                    entity.GuardOrPatrolOffsetPoints.Add( entity.WorldLocation - commandStation.WorldLocation );

                    NumberOfTier2Exos--;
                }


                // Note: This one only spawns the Tier 2s on the Bastions if on Difficulty 9 or above.

                if ( planet.PopulationType == PlanetPopulationType.AIBastionWorld && NumberOfTier2Exos > 0 && difficulty >= 9 )
                {
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );



                    GameEntityTypeData LONRearAdmiralExowarTier2EntityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ExtragalacticWarTier2" );

                    ArcenPoint LONRearAdmiralExowarTier2SpawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, LONRearAdmiralExowarTier2EntityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 500 ) );

                    GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, LONRearAdmiralExowarTier2EntityData, LONRearAdmiralExowarTier2EntityData.MarkFor( pFaction ), pFaction.FleetUsedAtPlanet, 0, LONRearAdmiralExowarTier2SpawnLocation, Context, "Admiral-LONRearAdmiralExowarLair" );

                    entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Guard_Guardian_Patrolling ); //works great!  mapgen
                    GameEntity_Squad commandStation = planet.GetCommandStationOrNull();
                    entity.GuardedUnit = LazyLoadSquadWrapper.Create( commandStation );
                    entity.GuardOrPatrolOffsetPoints.Add( entity.WorldLocation - commandStation.WorldLocation );

                    NumberOfTier2Exos--;
                }



                // Note: This only spawns a Tier 3 on the Homeworld if on Difficulty 9 or above.

                if ( planet.PopulationType == PlanetPopulationType.AIHomeworld && NumberOfTier3Exos > 0 && difficulty >= 9 )
                {
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );



                    GameEntityTypeData LONRearAdmiralExowarTier3EntityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ExtragalacticWarTier3" );

                    ArcenPoint LONRearAdmiralExowarTier3SpawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, LONRearAdmiralExowarTier3EntityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 500 ) );

                    GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, LONRearAdmiralExowarTier3EntityData, LONRearAdmiralExowarTier3EntityData.MarkFor( pFaction ), pFaction.FleetUsedAtPlanet, 0, LONRearAdmiralExowarTier3SpawnLocation, Context, "Admiral-LONRearAdmiralExowarLair" );

                    entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Guard_Guardian_Patrolling ); //works great!  mapgen
                    GameEntity_Squad commandStation = planet.GetCommandStationOrNull();
                    entity.GuardedUnit = LazyLoadSquadWrapper.Create( commandStation );
                    entity.GuardOrPatrolOffsetPoints.Add( entity.WorldLocation - commandStation.WorldLocation );

                    NumberOfTier3Exos--;
                }

            }
            Planet.ReleaseTemporaryPlanetList( ownedPlanets );
        }
    }
}