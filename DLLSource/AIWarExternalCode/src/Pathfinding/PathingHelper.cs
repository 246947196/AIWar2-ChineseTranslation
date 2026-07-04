using Arcen.AIW2.Core;
using System;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class PathingHelper
    {
        #region GetPathingModeForLocalPlayer
        public static PathingMode GetPathingModeForLocalPlayer()
        {
            if ( InputCaching.CalculateShouldUseShortestPath() )
                return PathingMode.Shortest;
            if ( InputCaching.CalculateShouldUseSafestPath() )
                return PathingMode.Safest;
            return PathingMode.Default;
        }
        #endregion

        #region NonSim_PopulateCostToPlanet
        public static void NonSim_PopulateCostToPlanet( Faction faction, string DebugAddendum, Dictionary<int, int> lookup, 
            ArcenSimContextAnyStatus Context, bool IsMinimalFogOfWar, PerFactionPathCache PathCacheData )
        {
            //This function is used for the objectives code to figure out how hard it is to get to a given planet
            Planet kingPlanet = FactionUtilityMethods.Instance.findHumanKingForFaction( faction, false );
            if ( kingPlanet == null )
                return;

            ExternalFactionBaseInfoRoot externalRoot = faction.BaseInfo as ExternalFactionBaseInfoRoot;
            if ( externalRoot == null )
            {
                ArcenDebugging.ArcenDebugLog( "Error! Could not get BaseInfo as ExternalFactionBaseInfoRoot for faction " + faction.GetDisplayName(), Verbosity.ShowAsError );
                return;
            }

            PlanetPathfinder pathfinder = externalRoot.GetNormalPathfinderThatMustBeReleased();

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                PathBetweenPlanetsForFaction pathCache = InnerFindPath_RawSinglePathfinder( pathfinder, faction, DebugAddendum, kingPlanet, planet,
                    PathingMode.CostToPlanet, Context, PathCacheData );
                int totalDifficulty = 0;
                if ( pathCache != null )
                {
                    for ( int j = 0; j < pathCache.PathToReadOnly.Count; j++ )
                    {
                        PlanetFaction pFaction = null;
                        try
                        {
                            pFaction = pathCache.PathToReadOnly[j].GetPlanetFactionForFaction( faction );
                        }
                        catch { }
                        if ( pFaction == null )
                            continue;
                        try
                        {
                            int defensiveStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrengthVisible;
                            if ( IsMinimalFogOfWar )
                                defensiveStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                            totalDifficulty += defensiveStrength;
                        }
                        catch { }
                    }
                }
                lookup[planet.Index] = totalDifficulty / 1000;
            }

            pathfinder.ReturnToPool();
        }
        #endregion

        #region NonSim_PopulatePathToCurrentHoverPlanet_LocalPlayerOnly
        public static void NonSim_PopulatePathToCurrentHoverPlanet_LocalPlayerOnly( Faction faction, string DebugAddendum, ProjectedLocalPlayerMultiPathData MultiPathDataLookup, 
            bool additiveSelection, ArcenSimContextAnyStatus Context, PerFactionPathCache PathCacheData )
        {
            Planet currentlyHovered = Planet.CurrentlyHoveredOver;
            if ( currentlyHovered == null )
                return;
            if ( !Engine_AIW2.Instance.GetSelectionThatICanGiveOrdersToContains( EntityRollupType.CanGoThroughWormholes ) )
                return;
            //this list is all the planets that contain a selected unit without wormhole orders, or the planet where
            //a unit with movement orders will wind up
            List<Planet> selectionOrDestinationPlanets = Planet.GetTemporaryPlanetList( "PathingHelper-NonSim_PopulatePathToCurrentHoverPlanet_LocalPlayerOnly-selectionOrDestinationPlanets", 10f );
            if ( selectionOrDestinationPlanets == null ) //blocked for teardown/shutdown; bail
                return;

            foreach ( GameEntity_Squad selected in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
            {
                if ( !selected.TypeData.CanGoThroughWormholes )
                    continue;
                Planet destinationOrCurrent = selected.Planet; //by default use the current planet
                if ( additiveSelection )
                    destinationOrCurrent = selected.GetDestinationPlanet_LocalPlayerOnly( MultiPathDataLookup ); //for an additive selection, we need to also include queued move commands

                if ( !selectionOrDestinationPlanets.Contains( destinationOrCurrent ) )
                    selectionOrDestinationPlanets.Add( destinationOrCurrent );
            }

            //Now path from those planets to the currently hovered planet
            foreach ( Planet planet in selectionOrDestinationPlanets ) // we normally avoid foreach, but this will be a small set, etc
            {
                PathingMode mode = PathingHelper.GetPathingModeForLocalPlayer();
                PathBetweenPlanetsForFaction pathCache = FindPathFreshOrFromCache( faction, DebugAddendum, planet, currentlyHovered, mode, Context, PathCacheData );
                if ( pathCache != null )
                {
                    Planet previousPlanet = planet;
                    for ( int i = 0; i < pathCache.PathToReadOnly.Count; i++ )
                    {
                        Planet nextPlanet = pathCache.PathToReadOnly[i];
                        MultiPathDataLookup.AddConnection( previousPlanet, nextPlanet );
                        previousPlanet = nextPlanet;
                    }
                }
            }
            Planet.ReleaseTemporaryPlanetList( selectionOrDestinationPlanets );
        }
        #endregion

        #region FindPathFreshOrFromCache
        public static PathBetweenPlanetsForFaction FindPathFreshOrFromCache( Faction ForFaction, string DebugAddendum, Planet Origin, Planet Target, PathingMode mode, 
            ArcenSimContextAnyStatus ContextOrNull, PerFactionPathCache PathCacheData )
        {
            if ( ForFaction == null )
                throw new Exception( "Null ForFaction in FindPathFreshOrFromCache!" );

            return InnerFindPath_Raw( ForFaction, DebugAddendum, Origin, Target, mode, ContextOrNull, PathCacheData );
        }
        #endregion

        #region InnerFindPath_Raw
        public static PathBetweenPlanetsForFaction InnerFindPath_Raw( Faction faction, string DebugAddendum, Planet Origin, Planet Target, PathingMode mode, 
            ArcenSimContextAnyStatus ContextOrNull, PerFactionPathCache PathCacheData )
        {
            int debugStage = 0;
            try
            {
                debugStage = 100;
                if ( Origin == Target || Origin == null || Target == null )
                    return null;

                if ( faction == null )
                {
                    ArcenDebugging.ArcenDebugLog( "Error! passed in null faction to InnerFindPath_Raw!", Verbosity.ShowAsError );
                    return null;
                }

                if ( PathCacheData == null )
                {
                    ArcenDebugging.ArcenDebugLog( "Error! passed in null PathCacheData for faction " + faction.GetDisplayName(), Verbosity.ShowAsError );
                    return null;
                }

                debugStage = 200;
                PathCache factionCache = PathCacheData.GetOrAddPathCacheForFactionInThisThread( faction.FactionIndex );
                if ( factionCache == null )
                    return null;
                debugStage = 300;
                PathCache.CacheForPathingMode facPathModeCache = factionCache.GetOrAddCacheForPathingMode( mode );
                if ( facPathModeCache == null )
                    return null;
                debugStage = 400;
                PathBetweenPlanetsForFaction pathCache = facPathModeCache.GetOrAddToFromPlanetDataOrNull( Origin, Target );
                if ( pathCache == null )
                    return null;
                debugStage = 600;
                //if the path cache object has already been calculated, but 
                if ( pathCache.TimeOfLastPathCalculation > 0 && (ArcenTime.TimeSinceStartF - pathCache.TimeOfLastPathCalculation) <=
                    pathCache.RecalculatesOnThisIntervalOfSeconds )
                {
                    debugStage = 700;
                    System.Threading.Interlocked.Add( ref PathBetweenPlanetsForFaction.PathsPulledFromCache, 1 );
                    return pathCache;
                }
                debugStage = 800;
                //otherwise we now need to calculate new things
                //start by clearing the existing path
                pathCache.PathToReadOnly.Clear();
                //and remember when we did it
                pathCache.TimeOfLastPathCalculation = ArcenTime.TimeSinceStartF;

                debugStage = 1000;

                ExternalFactionBaseInfoRoot externalRoot = faction.BaseInfo as ExternalFactionBaseInfoRoot;
                if ( externalRoot == null )
                {
                    ArcenDebugging.ArcenDebugLog( "Error! Could not get BaseInfo as ExternalFactionBaseInfoRoot for faction " + faction.GetDisplayName(), Verbosity.ShowAsError );
                    return null;
                }
                debugStage = 1300;

                debugStage = 1400;
                List<Planet> workingNormalResult = Planet.GetTemporaryPlanetList( "PathingHelper-InnerFindPath_Raw-workingNormalResult", 10f );
                if ( workingNormalResult == null ) //blocked for teardown/shutdown; bail
                    return null;

                PlanetPathfinder pathfinderMain = externalRoot.GetNormalPathfinderThatMustBeReleased();
                pathfinderMain.FindPath( faction, DebugAddendum, workingNormalResult, Origin, Target, 0, 0, false, ContextOrNull );
                debugStage = 1500;
                if ( workingNormalResult.Count <= 0 )
                {
                    pathfinderMain.ReturnToPool();
                    Planet.ReleaseTemporaryPlanetList( workingNormalResult );
                    return pathCache; //early out with empty path
                }

                List<Planet> workingConservativeResult = Planet.GetTemporaryPlanetList( "PathingHelper-InnerFindPath_Raw-workingConservativeResult", 10f );
                if ( workingConservativeResult == null ) //blocked for teardown/shutdown; bail
                {
                    pathfinderMain.ReturnToPool();
                    Planet.ReleaseTemporaryPlanetList( workingNormalResult );
                    return null;
                }
                debugStage = 1600;

                PlanetPathfinder pathfinderConservative = externalRoot.GetConservativePathfinderThatMustBeReleasedOrNull();

                //only do these calcualtions the second time if they are not the same
                if ( pathfinderConservative != pathfinderMain && 
                    pathfinderConservative != null ) //sometimes there is no conservative version, and that's also okay
                {
                    debugStage = 1700;
                    pathfinderConservative.FindPath( faction, DebugAddendum, workingConservativeResult, Origin, Target, 0, 0, false, ContextOrNull );
                }

                if ( pathfinderConservative != null )
                {
                    pathfinderConservative.ReturnToPool();
                    pathfinderConservative = null;
                }

                pathfinderMain.ReturnToPool();
                pathfinderMain = null;

                debugStage = 2000;
                bool safePathExists = false;
                if ( workingConservativeResult.Count > 0 )
                    safePathExists = true;
                debugStage = 2100;
                if ( mode == PathingMode.Shortest )
                {
                    debugStage = 3000;
                    if ( safePathExists && workingConservativeResult.Count <= workingNormalResult.Count )
                    {
                        debugStage = 3100;
                        for ( int i = 0; i < workingConservativeResult.Count; i++ )
                            pathCache.PathToReadOnly.Add( workingConservativeResult[i] );
                    }
                    else
                    {
                        debugStage = 3200;
                        for ( int i = 0; i < workingNormalResult.Count; i++ )
                            pathCache.PathToReadOnly.Add( workingNormalResult[i] );
                    }
                    debugStage = 3300;
                    Planet.ReleaseTemporaryPlanetList( workingNormalResult );
                    Planet.ReleaseTemporaryPlanetList( workingConservativeResult );
                    return pathCache;
                }
                debugStage = 4000;
                if ( workingConservativeResult.Count > 0 )
                {
                    debugStage = 4100;
                    if ( mode == PathingMode.Safest )
                    {
                        debugStage = 4200;
                        for ( int i = 0; i < workingConservativeResult.Count; i++ )
                            pathCache.PathToReadOnly.Add( workingConservativeResult[i] );
                        debugStage = 4300;
                        Planet.ReleaseTemporaryPlanetList( workingNormalResult );
                        Planet.ReleaseTemporaryPlanetList( workingConservativeResult );
                        return pathCache;
                    }
                    debugStage = 4400;
                    //so now we are in "default" mode; if the conservative result isn't too much longer, take it
                    if ( workingConservativeResult.Count <= workingNormalResult.Count + 2 )
                    {
                        debugStage = 4500;
                        for ( int i = 0; i < workingConservativeResult.Count; i++ )
                            pathCache.PathToReadOnly.Add( workingConservativeResult[i] );
                        debugStage = 4600;
                        Planet.ReleaseTemporaryPlanetList( workingNormalResult );
                        Planet.ReleaseTemporaryPlanetList( workingConservativeResult );
                        return pathCache;
                    }
                }
                debugStage = 5000;
                //if we got to this point, and nothing is filled, use the normal result
                if ( pathCache.PathToReadOnly.Count <= 0 )
                {
                    debugStage = 5100;
                    for ( int i = 0; i < workingNormalResult.Count; i++ )
                        pathCache.PathToReadOnly.Add( workingNormalResult[i] );
                }
                debugStage = 5200;
                Planet.ReleaseTemporaryPlanetList( workingNormalResult );
                Planet.ReleaseTemporaryPlanetList( workingConservativeResult );
                return pathCache;
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "InnerFindPath_Raw error at debugStage " + debugStage + ": " + e, Verbosity.ShowAsError );
                return null;
            }
            return null;
        }
        #endregion

        #region InnerFindPath_RawSinglePathfinder
        public static PathBetweenPlanetsForFaction InnerFindPath_RawSinglePathfinder( PlanetPathfinder pathfinder, Faction faction, string DebugAddendum, Planet Origin, 
            Planet Target, PathingMode mode, ArcenSimContextAnyStatus Context, PerFactionPathCache PathCacheData )
        {
            int debugStage = 0;
            try
            {
                debugStage = 100;
                if ( Origin == Target || Origin == null || Target == null )
                    return null;

                if ( faction == null )
                {
                    ArcenDebugging.ArcenDebugLog( "Error! passed in null faction to InnerFindPath_Raw!", Verbosity.ShowAsError );
                    return null;
                }

                if ( pathfinder == null )
                {
                    ArcenDebugging.ArcenDebugLog( "Error! passed in null pathfinder for faction " + faction.GetDisplayName(), Verbosity.ShowAsError );
                    return null;
                }

                if ( PathCacheData == null )
                {
                    ArcenDebugging.ArcenDebugLog( "Error! passed in null PathCacheData for faction " + faction.GetDisplayName(), Verbosity.ShowAsError );
                    return null;
                }

                if ( Context == null )
                {
                    ArcenDebugging.ArcenDebugLog( "Error! passed in null Context for faction " + faction.GetDisplayName(), Verbosity.ShowAsError );
                    return null;
                }

                debugStage = 200;
                PathCache factionCache = PathCacheData.GetOrAddPathCacheForFactionInThisThread( faction.FactionIndex );
                if ( factionCache == null )
                    return null;
                debugStage = 300;
                PathCache.CacheForPathingMode facPathModeCache = factionCache.GetOrAddCacheForPathingMode( mode );
                if ( facPathModeCache == null )
                    return null;
                debugStage = 400;
                PathBetweenPlanetsForFaction pathCache = facPathModeCache.GetOrAddToFromPlanetDataOrNull( Origin, Target );
                if ( pathCache == null )
                    return null;
                debugStage = 500;
                //if the path cache object has already been calculated, but 
                if ( pathCache.TimeOfLastPathCalculation > 0 && (ArcenTime.TimeSinceStartF - pathCache.TimeOfLastPathCalculation) <=
                    pathCache.RecalculatesOnThisIntervalOfSeconds )
                {
                    debugStage = 600;
                    System.Threading.Interlocked.Add( ref PathBetweenPlanetsForFaction.PathsPulledFromCache, 1 );
                    return pathCache;
                }
                debugStage = 700;
                //otherwise we now need to calculate new things
                //start by clearing the existing path
                pathCache.PathToReadOnly.Clear();
                //and remember when we did it
                pathCache.TimeOfLastPathCalculation = ArcenTime.TimeSinceStartF;

                //the code above is the same as InnerFindPath_Raw, except for the extra null check on the pathfinder itself
                debugStage = 1000;

                debugStage = 1100;

                List<Planet> workingPlanetList = Planet.GetTemporaryPlanetList( "PathingHelper-InnerFindPath_RawSinglePathfinder-workingPlanetList", 90f );
                if ( workingPlanetList == null ) //blocked for teardown/shutdown; bail
                    return null;

                debugStage = 1200;
                pathfinder.FindPath( faction, DebugAddendum, workingPlanetList, Origin, Target, 0, 0, false, Context );

                debugStage = 1300;
                for ( int i = 0; i < workingPlanetList.Count; i++ )
                {
                    debugStage = 1400;
                    pathCache.PathToReadOnly.Add( workingPlanetList[i] );
                }

                Planet.ReleaseTemporaryPlanetList( workingPlanetList );

                debugStage = 1500;
                return pathCache;
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "InnerFindPath_RawSinglePathfinder error at debugStage " + debugStage + ": " + e, Verbosity.ShowAsError );
                return null;
            }
            return null;
        }
        #endregion
    }
}