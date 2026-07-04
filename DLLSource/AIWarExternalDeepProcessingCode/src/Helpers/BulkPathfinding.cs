using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

// Notice: It is in its own namespace so that its extension methods do not appear everywhere.
namespace Arcen.AIW2.External.BulkPathfinding
{
    public interface IBulkPathfinding
    {
        Faction FactionForBulkPathfinding { get; }
        DictionaryOfDictionaryOfLists<Planet, Planet, SafeSquadWrapper> WormholeCommands { get; set; }
        DictionaryOfDictionaryOfLists<Planet, ArcenPoint, SafeSquadWrapper> MovementCommands { get; set; }
        List<Planet> ConflictPlanets { get; set; }
    }

    public static class BulkPathfinding
    {
        #region Wormhole Commands
        /// <summary>
        /// Queues up a wormhole command in BulkPathfinding. Does not actually queue the command in sim at this time.
        /// To execute commands queued in this way, call this.ExecuteWormholeCommands at the end of your Movement Planning.
        /// </summary>
        /// <returns>The next planet the entity will move to.</returns>
        public static Planet QueueWormholeCommand( this IBulkPathfinding faction, GameEntity_Squad entity, Planet destination,
            ArcenLongTermIntermittentPlanningContext ContextForSmartPathfindingOrNullForDumb = null, PathingMode pathingMode = PathingMode.Safest, bool forResultOnlyDoNotActuallyQueue = false )
        {
            if ( faction.WormholeCommands == null )
                faction.WormholeCommands = DictionaryOfDictionaryOfLists<Planet, Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 10, 10, 10, "IBulkPathfinding-WormholeCommands" );

            Planet origin = entity.Planet;
            Planet nextPlanet = null;
            if ( ContextForSmartPathfindingOrNullForDumb != null )
            {
                PerFactionPathCache pathCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
                List<Planet> path = PathingHelper.FindPathFreshOrFromCache( entity.PlanetFaction.Faction, "IBulkPathfinding-QueueWormholeCommand", entity.Planet, destination, pathingMode,
                    ContextForSmartPathfindingOrNullForDumb, pathCacheData ).PathToReadOnly;
                if (path.Count > 0) {
                    nextPlanet = path[0];
                }
                pathCacheData.ReturnToPool();
            }

            if ( nextPlanet == null )
                foreach ( Planet planet in origin.LinkedNeighbors( false ) )
                {
                    if ( nextPlanet == null )
                        nextPlanet = planet;
                    else if ( planet.GetHopsTo( destination ) < nextPlanet.GetHopsTo( destination ) || (planet.GetHopsTo( destination ) == nextPlanet.GetHopsTo( destination ) && planet.Index < nextPlanet.Index) )
                        nextPlanet = planet;
                }

            if ( !forResultOnlyDoNotActuallyQueue && nextPlanet != null )
                faction.WormholeCommands[origin][nextPlanet].Add( entity );

            return nextPlanet;
        }
        public static void ExecuteWormholeCommands( this IBulkPathfinding faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( faction.WormholeCommands == null )
                return;

            PerFactionPathCache pathCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            foreach ( KeyValuePair<Planet, DictionaryOfLists<Planet, SafeSquadWrapper>> originPair in faction.WormholeCommands )
            {
                Planet origin = originPair.Key;
                DictionaryOfLists<Planet, SafeSquadWrapper> destinations = originPair.Value;
                foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> kv in destinations )
                {
                    Planet destination = kv.Key;
                    List<SafeSquadWrapper> entities = kv.Value;
                    if ( entities == null )
                        continue;

                    List<Planet> path = PathingHelper.FindPathFreshOrFromCache( faction.FactionForBulkPathfinding, "IBulkPathfinding-ExecuteWormholeCommands", origin, destination, PathingMode.Safest, Context, pathCacheData ).PathToReadOnly;
                    GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCDirectedMob], GameCommandSource.AnythingElse );
                    for ( int p = 0; p < path.Count; p++ )
                        command.RelatedIntegers.Add( path[p].Index );
                    for ( int z = 0; z < entities.Count; z++ )
                    {
                        GameEntity_Squad entity = entities[z].GetSquad();
                        if ( entity != null && entity.CalculateFinalDestinationPlanetIndex_Safe() != destination.Index )
                            command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                    }
                    if ( command.RelatedEntityIDs.Count > 0 )
                        World_AIW2.Instance.QueueGameCommand( faction.FactionForBulkPathfinding, command, false );
                    else
                        command.ReturnToPool(); //avoid memory leak
                }
            }
            pathCacheData.ReturnToPool();

            faction.WormholeCommands.Clear();
        }
        #endregion

        #region Movement Commands
        /// <summary>
        /// Queues up a movement command in BulkPathfinding. Does not actually queue the command in sim at this time.
        /// To execute commands queued in this way, call this.ExecuteMovementCommands at the end of your Movement Planning.
        /// </summary>
        /// <param name="faction"></param>
        /// <param name="entity"></param>
        /// <param name="destination"></param>
        public static void QueueMovementCommand( this IBulkPathfinding faction, GameEntity_Squad entity, ArcenPoint destination )
        {
            if ( faction.MovementCommands == null )
                faction.MovementCommands = DictionaryOfDictionaryOfLists<Planet, ArcenPoint, SafeSquadWrapper>.Create_WillNeverBeGCed( 10, 10, 10, "IBulkPathfinding-MovementCommands" );

            Planet planet = entity.Planet;
            faction.MovementCommands[planet][destination].Add( entity );
        }
        public static void ExecuteMovementCommands( this IBulkPathfinding faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( faction.MovementCommands == null )
                return;

            foreach ( KeyValuePair<Planet, DictionaryOfLists<ArcenPoint, SafeSquadWrapper>> planetPair in faction.MovementCommands )
            {
                Planet planet = planetPair.Key;
                DictionaryOfLists<ArcenPoint, SafeSquadWrapper> destinations = planetPair.Value;
                foreach ( KeyValuePair<ArcenPoint, List<SafeSquadWrapper>> destinationPair in destinations )
                {
                    ArcenPoint destination = destinationPair.Key;
                    List<SafeSquadWrapper> entities = destinationPair.Value;
                    if ( entities == null )
                        continue;
                    GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCBulkPathfind], GameCommandSource.AnythingElse );
                    command.RelatedPoints.Add( destination );
                    for ( int z = 0; z < entities.Count; z++ )
                    {
                        GameEntity_Squad entity = entities[z].GetSquad();
                        if ( entity != null && entity.CalculateDestinationPoint_Safe() != destination )
                            command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                    }
                    if ( command.RelatedEntityIDs.Count > 0 )
                        World_AIW2.Instance.QueueGameCommand( faction.FactionForBulkPathfinding, command, false );
                    else
                        command.ReturnToPool(); //avoid memory leak
                }
            }

            faction.MovementCommands.Clear();
        }
        #endregion

        #region Conflict Planets
        /// <summary>
        /// Builds up a list of all planets that this faction should consider in conflict.
        /// Conflict refers to planets that they have units on, and enemies are on.
        /// Ignores cloaking and reinforcement point enemies by default.
        /// </summary>
        /// <param name="faction"></param>
        /// <param name="skipPlanetIfTrue">Optional function. If true, a planet will be skipped over even if it would otherwise be a conflict planet.</param>
        /// <param name="minimumStrengthOfOursForConflict"></param>
        /// <param name="minimumStrengthOfEnemiesForConflict"></param>
        /// <param name="ignoreCloaking"></param>
        /// <param name="ignoreUnitsInReinforcementPoints"></param>
        public static void RebuildConflictPlanetsList( this IBulkPathfinding faction, Func<Planet, bool> skipPlanetIfTrue = null, int minimumStrengthOfOursForConflict = 100, int minimumStrengthOfEnemiesForConflict = 100, bool ignoreCloaking = true, bool ignoreUnitsInReinforcementPoints = true )
        {
            if ( faction.ConflictPlanets == null )
                faction.ConflictPlanets = List<Planet>.Create_WillNeverBeGCed( 10, "IBulkPathfinding-ConflictPlanets" );

            faction.ConflictPlanets.Clear();

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.GetPlanetFactionForFaction( faction.FactionForBulkPathfinding ).DataByStance[FactionStance.Self].TotalStrength < minimumStrengthOfOursForConflict )
                    continue; // We're not here. We don't care.

                if ( skipPlanetIfTrue != null && skipPlanetIfTrue( planet ) )
                    continue;

                var strengthData = planet.GetPlanetFactionForFaction( faction.FactionForBulkPathfinding ).DataByStance;

                int hostileStrength = strengthData[FactionStance.Hostile].TotalStrength;
                if ( ignoreCloaking )
                    hostileStrength -= strengthData[FactionStance.Hostile].CloakedStrength;
                if ( ignoreUnitsInReinforcementPoints )
                    hostileStrength -= strengthData[FactionStance.Hostile].StrengthInReinforcementPoints;
                hostileStrength -= strengthData[FactionStance.Hostile].NonTargetableStrength;

                if ( hostileStrength >= minimumStrengthOfEnemiesForConflict )
                    faction.ConflictPlanets.Add( planet );
            }
        }
        /// <summary>
        /// Moves all units towards their nearest Conflict Planet.
        /// By default, units will stop and patrol any planets they go on for a minute before moving on.
        /// Make sure you run this.ExecuteWormholeCommands and this.ExecuteMovementCommands after this, or its queue will not be applied.
        /// Warning: RebuildConflictPlanetsList must be ran before this.
        /// </summary>
        /// <param name="faction"></param>
        /// <param name="Context"></param>
        /// <param name="minHostileStrengthToStayAndFight"></param>
        /// <param name="secondsToPatrolOnEachPlanet"></param>
        /// <param name="skipEntityIfTrue"></param>
        public static void PrepareConflictPlanetMovementLogic( this IBulkPathfinding faction, ArcenLongTermIntermittentPlanningContext Context, int minHostileStrengthToStayAndFight = 100, int secondsToPatrolOnEachPlanet = 60, Func<GameEntity_Squad, bool> skipEntityIfTrue = null )
        {
            foreach ( GameEntity_Squad entity in faction.FactionForBulkPathfinding.Squads() )
            {
                if ( entity.TypeData.IsDrone || !entity.TypeData.IsMobile )
                    continue;

                if ( skipEntityIfTrue != null && skipEntityIfTrue( entity ) )
                    continue; // Skip anything we're told to skip.

                PlanetFaction pFac = entity.PlanetFaction;
                if ( pFac == null )
                    continue; // this happens when things die while our thread is thinking about things.

                Planet planet = entity.Planet;
                if ( planet == null )
                    continue; // this happens when things die while our thread is thinking about things.

                var strengthData = pFac.DataByStance;

                int hostileStrength = strengthData[FactionStance.Hostile].TotalStrength;
                hostileStrength -= strengthData[FactionStance.Hostile].CloakedStrength;
                hostileStrength -= strengthData[FactionStance.Hostile].StrengthInReinforcementPoints;
                hostileStrength -= strengthData[FactionStance.Hostile].NonTargetableStrength;

                if ( hostileStrength > minHostileStrengthToStayAndFight )
                    continue; // Don't move on until we've killed everything we can see.

                if ( entity.GetSecondsSinceEnteringThisPlanet() < secondsToPatrolOnEachPlanet )
                {
                    // Patrol around a bit. Look lively.
                    if ( entity.Orders.GetHasAnyOrdersOfType( EntityOrderType.Move_Normal ) )
                        continue;
                    faction.QueueMovementCommand( entity, planet.GetRandomPointWithinCircleAndAlsoWithinGravWell( Engine_AIW2.Instance.CombatCenter, 10000, Context.RandomToUse ) );
                }
                else
                {
                    // Move on to another planet.
                    if ( entity.Orders.GetHasAnyOrdersOfType( EntityOrderType.Wormhole ) )
                        continue;
                    faction.QueueWormholeCommand( entity, faction.GetNearestConflictPlanet( planet, Context ), Context );
                }
            }
        }
        /// <summary>
        /// Warning: RebuildConflictPlanetsList must be ran before this.
        /// </summary>
        /// <param name="faction"></param>
        /// <param name="planet"></param>
        /// <param name="Context"></param>
        /// <returns></returns>
        public static Planet GetNearestConflictPlanet( this IBulkPathfinding faction, Planet planet, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( faction.ConflictPlanets == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( $"A faction attempted to call GetNearestConflictPlanet without first calling RebuildConflictPlanetsList. This is not supported.", Verbosity.ShowAsError );
                return null;
            }

            Planet planetToReturn = null;
            List<Planet> potentialPlanets = Planet.GetTemporaryPlanetList( "IBulkPathfinding-GetNearestConflictPlanet-potentialPlanets", 10f );
            if ( potentialPlanets == null ) //blocked for teardown/shutdown; bail
                return null;
            int lowestHopsToConflict = 9999;
            faction.ConflictPlanets.ForEach( conflictPlanet =>
            {
                if ( planet != conflictPlanet )
                {
                    int hops = planet.GetHopsTo( conflictPlanet );
                    if ( hops < lowestHopsToConflict )
                    {
                        lowestHopsToConflict = hops;
                        potentialPlanets.Clear();
                    }
                    if ( hops == lowestHopsToConflict )
                        potentialPlanets.AddIfNotAlreadyIn( conflictPlanet );
                }
            } );

            // Pick a random valid planet.
            if ( potentialPlanets.Count > 0 )
                planetToReturn = potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];
            else
                planetToReturn = planet.GetRandomNeighbor( false, Context );

            Planet.ReleaseTemporaryPlanetList( potentialPlanets );

            return planetToReturn;
        }
        #endregion
    }
}
