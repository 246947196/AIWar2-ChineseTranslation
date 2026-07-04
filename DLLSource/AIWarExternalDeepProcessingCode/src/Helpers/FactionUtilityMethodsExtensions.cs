using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class FactionUtilityMethodsExtensions
    {
        public static bool Helper_RaidAgainstKing( this FactionUtilityMethods Utility, List<SafeSquadWrapper> kingFleet, Planet originPlanet, Faction faction,
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, bool antiHuman, float RequiredTimeBetweenCommands )
        {
            if ( kingFleet.Count <= 0 )
                return false;

            //if any of the ships in the list have been given orders too recently, then don't give orders to any of them right now
            for ( int i = 0; i < kingFleet.Count; i++ )
            {
                if ( ArcenTime.TimeSinceStartF - kingFleet[i].HostOnly_TimeWasLastGivenOrderFromLRP < RequiredTimeBetweenCommands )
                    return false;
            }

            bool debug = false;
            Planet kingPlanet = null;
            //Find the kingPlanet
            if ( antiHuman )
            {

                foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    if ( entity.GetFactionTypeSafe() == FactionType.Player )
                        kingPlanet = entity.Planet;
                }
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Killing the human king on" + kingPlanet.Name, Verbosity.DoNotShow );

            }
            else
            {
                foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    if ( entity.GetFactionTypeSafe() == FactionType.AI )
                        kingPlanet = entity.Planet;
                }
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Killing the AI king on" + kingPlanet.Name, Verbosity.DoNotShow );

            }
            PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "Helper_RaidAgainstKing", originPlanet, kingPlanet, PathingMode.Default, Context, PathCacheData );

            if ( pathCache == null || pathCache.PathToReadOnly.Count <= 0 )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Seems the king is already dead or we are already there?", Verbosity.DoNotShow );
                return false;
            }

            {
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_UtilRaidKing], GameCommandSource.AnythingElse );
                command.RelatedString = "Util_RaidKing";
                for ( int k = 0; k < kingFleet.Count; k++ )
                    command.RelatedEntityIDs.Add( kingFleet[k].PrimaryKeyID );
                for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                    command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                World_AIW2.Instance.QueueGameCommand( faction, command, false );
            }

            for ( int i = 0; i < kingFleet.Count; i++ )
            {
                kingFleet[i].SetHostOnly_TimeWasLastGivenOrderFromLRP( ArcenTime.TimeSinceStartF );
            }
            return true;
        }

        public static bool Helper_DivideShipsAmongPlanets( this FactionUtilityMethods Utility, List<SafeSquadWrapper> ships, Planet originPlanet, Faction faction, List<Planet> targetPlanets, 
            Galaxy galaxy, ArcenLongTermIntermittentPlanningContext Context, float RequiredTimeBetweenCommands )
        {
            if ( ships.Count == 0 || targetPlanets.Count == 0 )
                return false;

            //if any of the ships in the list have been given orders too recently, then don't give orders to any of them right now
            for ( int i = 0; i < ships.Count; i++ )
            {
                if ( ArcenTime.TimeSinceStartF - ships[i].HostOnly_TimeWasLastGivenOrderFromLRP < RequiredTimeBetweenCommands )
                    return false;
            }

            int shipGroupSize = 20;
            if ( ships.Count < shipGroupSize )
                shipGroupSize = ships.Count;
            for ( int i = 0; i < ships.Count / shipGroupSize; i++ )
            {
                int startOfSubList = i * shipGroupSize;
                int endOfSubList = startOfSubList + shipGroupSize - 1;
                if ( i == ships.Count / shipGroupSize - 1 )
                    endOfSubList = ships.Count - 1;
                if ( startOfSubList >= endOfSubList )
                    continue; //nothing to send

                Planet destination = targetPlanets[Context.RandomToUse.Next( 0, targetPlanets.Count )];
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_UtilDivideShips], GameCommandSource.AnythingElse );
                command.RelatedString = "Util_DivideShips";
                command.ToBeQueued = false;
                command.RelatedIntegers.Add( destination.Index );
                for ( int j = startOfSubList; j < endOfSubList; j++ )
                    command.RelatedEntityIDs.Add( ships[j].PrimaryKeyID );
                World_AIW2.Instance.QueueGameCommand( faction, command, false );
            }

            for ( int i = 0; i < ships.Count; i++ )
            {
                ships[i].SetHostOnly_TimeWasLastGivenOrderFromLRP( ArcenTime.TimeSinceStartF );
            }

            return true;
        }

        public static bool SendUnitToRandomWormhole( this FactionUtilityMethods Utility, Faction faction, ArcenLongTermIntermittentPlanningContext Context, GameEntity_Squad entity, float RequiredTimeBetweenCommands )
        {
            if ( entity == null )
                return false;
            if ( ArcenTime.TimeSinceStartF - entity.HostOnly_TimeWasLastGivenOrderFromLRP < RequiredTimeBetweenCommands )
                return false;

            List<ArcenPoint> WorkingWormholeListInner = Mat.GetTemporaryArcenPointList( "FactionUtilityMethods-SendUnitToRandomWormhole-WorkingWormholeListInner", 10f );
            if ( WorkingWormholeListInner == null ) //blocked for teardown/shutdown; bail
                return false;

            ArcenPoint point = Utility.GetRandomWormholeOnPlanet( entity.Planet, Context, WorkingWormholeListInner );
            GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCRandomWormhole], GameCommandSource.AnythingElse );
            moveCommand.PlanetOrderWasIssuedFrom = entity.Planet.Index;
            moveCommand.ToBeQueued = true;
            moveCommand.RelatedPoints.Add( point );
            moveCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );
            World_AIW2.Instance.QueueGameCommand( faction, moveCommand, false );

            entity.HostOnly_TimeWasLastGivenOrderFromLRP = ArcenTime.TimeSinceStartF;

            Mat.ReleaseTemporaryArcenPointList( WorkingWormholeListInner );
            return true;
        }

        public static bool SendUnitToRandomMetalGenerator( this FactionUtilityMethods Utility, Faction faction, ArcenLongTermIntermittentPlanningContext Context, GameEntity_Squad entity, float RequiredTimeBetweenCommands )
        {
            if ( entity == null )
                return false;
            if ( ArcenTime.TimeSinceStartF - entity.HostOnly_TimeWasLastGivenOrderFromLRP < RequiredTimeBetweenCommands )
                return false;

            List<ArcenPoint> WorkingMetalGeneratorListInner = Mat.GetTemporaryArcenPointList( "FactionUtilityMethods-SendUnitToRandomMetalGenerator-WorkingMetalGeneratorListInner", 10f );
            if ( WorkingMetalGeneratorListInner == null ) //blocked for teardown/shutdown; bail
                return false;

            ArcenPoint point = Utility.GetRandomMetalGeneratorOnPlanet( entity.Planet, Context, WorkingMetalGeneratorListInner );
            if ( point == ArcenPoint.ZeroZeroPoint )
            {
                Mat.ReleaseTemporaryArcenPointList( WorkingMetalGeneratorListInner );
                return false;
            }

            GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCRandomMetalSpot], GameCommandSource.AnythingElse );
            moveCommand.PlanetOrderWasIssuedFrom = entity.Planet.Index;
            moveCommand.ToBeQueued = true;
            moveCommand.RelatedPoints.Add( point );
            moveCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );
            World_AIW2.Instance.QueueGameCommand( faction, moveCommand, false );

            entity.HostOnly_TimeWasLastGivenOrderFromLRP = ArcenTime.TimeSinceStartF;

            Mat.ReleaseTemporaryArcenPointList( WorkingMetalGeneratorListInner );
            return true;
        }
        public static bool SendUnitToRandomLocation( this FactionUtilityMethods Utility, Faction faction, ArcenLongTermIntermittentPlanningContext Context, GameEntity_Squad entity, float RequiredTimeBetweenCommands )
        {
            if ( entity == null )
                return false;
            if ( ArcenTime.TimeSinceStartF - entity.HostOnly_TimeWasLastGivenOrderFromLRP < RequiredTimeBetweenCommands )
                return false;

            ArcenPoint randomLocation = entity.Planet.GetSafePlacementPointAroundPlanetCenter( Context, entity.TypeData, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 900 ) );
            GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCRandomMetalSpot], GameCommandSource.AnythingElse );
            moveCommand.PlanetOrderWasIssuedFrom = entity.Planet.Index;
            moveCommand.ToBeQueued = true;
            moveCommand.RelatedPoints.Add( randomLocation );
            moveCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );
            World_AIW2.Instance.QueueGameCommand( faction, moveCommand, false );

            entity.HostOnly_TimeWasLastGivenOrderFromLRP = ArcenTime.TimeSinceStartF;
            return true;
        }
        //Sends a raid against a single specific planet
        //Needs a flag for "Only stage through nanocaut planets if possible" flag
        public static bool Helper_RaidSpecificPlanet( this FactionUtilityMethods Utility, GameEntity_Squad ship, Planet originPlanet, Faction faction, 
            Galaxy galaxy, Planet threatplanet, bool IgnorePathCosts, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, float RequiredTimeBetweenCommands )
        {
            if ( ship == null )
                return false;
            if ( ArcenTime.TimeSinceStartF - ship.HostOnly_TimeWasLastGivenOrderFromLRP < RequiredTimeBetweenCommands )
                return false;

            List<SafeSquadWrapper> workingRaidShips = GameEntity_Squad.GetTemporarySquadList( "FactionUtil-Helper_RaidSpecificPlanet-workingRaidShips", 10f );
            if ( workingRaidShips == null ) //blocked for teardown/shutdown; bail
                return false;

            workingRaidShips.Add( ship );
            bool result = Utility.Helper_RaidSpecificPlanet( workingRaidShips, originPlanet, faction, galaxy, threatplanet, IgnorePathCosts, Context, PathCacheData, RequiredTimeBetweenCommands );

            GameEntity_Squad.ReleaseTemporarySquadList( workingRaidShips );
            return result;
        }

        //sends a raid against a planet chosen at random from a List
        //Not currently used, but it may be helpful someday so hold onto it
        public static bool Helper_RaidPlanetFromList( this FactionUtilityMethods Utility, List<SafeSquadWrapper> threatShipsNotAssignedElsewhere, Planet originPlanet, Faction faction, 
            Galaxy galaxy, List<Planet> threatPlanets, bool IgnorePathCosts, ArcenLongTermIntermittentPlanningContext Context, float RequiredTimeBetweenCommands )
        {
            if ( threatShipsNotAssignedElsewhere.Count <= 0 )
                return false;

            //if any of the ships in the list have been given orders too recently, then don't give orders to any of them right now
            for ( int i = 0; i < threatShipsNotAssignedElsewhere.Count; i++ )
            {
                if ( ArcenTime.TimeSinceStartF - threatShipsNotAssignedElsewhere[i].HostOnly_TimeWasLastGivenOrderFromLRP < RequiredTimeBetweenCommands )
                    return false;
            }

            Planet threatTarget = threatPlanets[Context.RandomToUse.Next( 0, threatPlanets.Count )];
            /* Set up the data to let us get from any planet to any other given planet */
            foreach ( Planet otherPlanet in World_AIW2.Instance.Planets( false ) )
            {
                otherPlanet.FactionPlanning_CheapestRaidPathToHereComesFrom = null;
                otherPlanet.FactionPlanning_CheapestRaidPathToHereCost = 0;
            }

            List<Planet> potentialAttackTargets = Planet.GetTemporaryPlanetList( "FactionUtil-Helper_RaidPlanetFromList-potentialAttackTargets", 10f );
            if ( potentialAttackTargets == null ) //blocked for teardown/shutdown; bail
                return false;
            List<Planet> planetsToCheckInFlood = Planet.GetTemporaryPlanetList( "FactionUtil-Helper_RaidPlanetFromList-planetsToCheckInFlood", 10f );
            if ( planetsToCheckInFlood == null ) //blocked for teardown/shutdown; bail
            {
                Planet.ReleaseTemporaryPlanetList( potentialAttackTargets );
                return false;
            }

            planetsToCheckInFlood.Add( originPlanet );
            originPlanet.FactionPlanning_CheapestRaidPathToHereComesFrom = originPlanet;
            for ( int k = 0; k < planetsToCheckInFlood.Count; k++ )
            {
                Planet floodPlanet = planetsToCheckInFlood[k];
                foreach ( Planet neighbor in floodPlanet.LinkedNeighbors( false ) )
                {
                    int totalCostFromOriginToNeighbor = floodPlanet.FactionPlanning_CheapestRaidPathToHereCost + 1;
                    if ( !potentialAttackTargets.Contains( neighbor ) )
                        potentialAttackTargets.Add( neighbor );
                    if ( neighbor.FactionPlanning_CheapestRaidPathToHereComesFrom != null &&
                         neighbor.FactionPlanning_CheapestRaidPathToHereCost <= totalCostFromOriginToNeighbor )
                        continue;
                    neighbor.FactionPlanning_CheapestRaidPathToHereComesFrom = floodPlanet;
                    neighbor.FactionPlanning_CheapestRaidPathToHereCost = totalCostFromOriginToNeighbor;
                    planetsToCheckInFlood.Add( neighbor );
                }
            }

            List<Planet> workingPath = Planet.GetTemporaryPlanetList( "FactionUtil-Helper_RaidPlanetFromList-workingPath", 10f );
            if ( workingPath == null ) //blocked for teardown/shutdown; bail
            {
                Planet.ReleaseTemporaryPlanetList( potentialAttackTargets );
                Planet.ReleaseTemporaryPlanetList( planetsToCheckInFlood );
                return false;
            }

            Planet workingPlanet = threatTarget;
            int attemptsLeft = 1000;
            while ( workingPlanet != originPlanet && attemptsLeft > 0 )
            {
                attemptsLeft--;
                workingPath.Insert( 0, workingPlanet );
                workingPlanet = workingPlanet.FactionPlanning_CheapestRaidPathToHereComesFrom;
            }
            bool result = false;
            if ( workingPath.Count > 0 )
            {
                result = true;

                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_UtilRaidFromList], GameCommandSource.AnythingElse );
                command.RelatedString = "Util_RaidFList";
                for ( int k = 0; k < threatShipsNotAssignedElsewhere.Count; k++ )
                    command.RelatedEntityIDs.Add( threatShipsNotAssignedElsewhere[k].PrimaryKeyID );
                for ( int k = 0; k < workingPath.Count; k++ )
                    command.RelatedIntegers.Add( workingPath[k].Index );
                World_AIW2.Instance.QueueGameCommand( faction, command, false );

                for ( int i = 0; i < threatShipsNotAssignedElsewhere.Count; i++ )
                {
                    threatShipsNotAssignedElsewhere[i].SetHostOnly_TimeWasLastGivenOrderFromLRP( ArcenTime.TimeSinceStartF );
                }
            }

            Planet.ReleaseTemporaryPlanetList( potentialAttackTargets );
            Planet.ReleaseTemporaryPlanetList( planetsToCheckInFlood );
            Planet.ReleaseTemporaryPlanetList( workingPath );
            return result;
        }

        public static bool FlushUnitsFromReinforcementPointsOnAllRelevantPlanets( this FactionUtilityMethods Utility, Faction faction, 
            ArcenLongTermIntermittentPlanningContext Context, float RequiredTimeBetweenCommands )
        {
            //The AI is causing real problems with reinforcement points that minor factions won't kill like Data Centers (or command stations)
            //Fireteams in particular are having problems. This approach lets minor factions flush out all the units in reinforcements points, and lets
            //fireteams keep neutral planets (with ai command stations) free of enemies.
            //This function should be called by all factions that can be ai-hostile that care about clearing out all the enemies on a planet; make the call from LongRangePlanning

            bool flushedAny = false;
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( ArcenTime.TimeSinceStartF - planet.HostOnly_TimeReinforcementPointsWereLastFlushedFromLRP < RequiredTimeBetweenCommands )
                    continue;

                //if there's anything in reinforcement points and we outnumber the enemy 5 to 1, flush the bastards out
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
                int totalEnemyStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                int enemiesInReinforcementPoint = pFaction.DataByStance[FactionStance.Hostile].StrengthInReinforcementPoints;
                int myAndAlliedStrength = pFaction.DataByStance[FactionStance.Self].TotalStrength + pFaction.DataByStance[FactionStance.Friendly].TotalStrength;
                int myStrength = pFaction.DataByStance[FactionStance.Self].TotalStrength;
                if ( myStrength == 0 )
                    continue;
                if ( enemiesInReinforcementPoint > 0 &&
                     ((enemiesInReinforcementPoint >= totalEnemyStrength - 500 && totalEnemyStrength < myAndAlliedStrength) ||
                     (pFaction.DataByStance[FactionStance.Hostile].StrengthInReinforcementPoints <
                      pFaction.DataByStance[FactionStance.Self].TotalStrength / 2)) )
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine("Flushing reinforcement points on " + planet.Name + ". Enemy strength in reinforcement points: " + enemiesInReinforcementPoint + " my strength: " + myAndAlliedStrength + " totalEnemyStrength " + totalEnemyStrength, Verbosity.DoNotShow );
                    if ( Utility.FlushUnitsFromReinforcementPoints( planet, faction, Context, RequiredTimeBetweenCommands ) )
                        flushedAny = true;
                }
            }
            return flushedAny;
        }

        public static bool FlushUnitsFromReinforcementPoints( this FactionUtilityMethods Utility, Planet targetPlanet, Faction faction, 
            ArcenLongTermIntermittentPlanningContext Context, float RequiredTimeBetweenCommands )
        {
            if ( targetPlanet == null )
                return false;

            if ( ArcenTime.TimeSinceStartF - targetPlanet.HostOnly_TimeReinforcementPointsWereLastFlushedFromLRP < RequiredTimeBetweenCommands )
                return false;
            targetPlanet.HostOnly_TimeReinforcementPointsWereLastFlushedFromLRP = ArcenTime.TimeSinceStartF;

            //helper function for flushing units
            GameCommand flushCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.FlushReinforcementPoints], GameCommandSource.AnythingElse );
            flushCommand.PlanetOrderWasIssuedFrom = targetPlanet.Index;
            World_AIW2.Instance.QueueGameCommand( faction, flushCommand, false );

            return true;
        }
    }
}
