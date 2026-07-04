using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

namespace Arcen.AIW2.External
{
    public static class MapgenTomUtilityMethods
    {
        public static ThrowawayListCanMemLeak<ArcenPoint> addPointsInStartScreen( int numPoints, ArcenHostOnlySimContext Context, int minDistanceBetweenPlanets,
                ThrowawayListCanMemLeak<ArcenPoint> pointsSoFarOrNull, bool allowClustersInPoints, int divisibleByX = 1 )

        {
            int x = 1200;
            int y = 600;
            if ( numPoints > 80 ) {
                x = 1300;
                y = 800;
            }
            if ( numPoints > 180 ) {
                x = 1900;
                y = 1400;
            }
            if ( numPoints > 200 ) {
                x = 2300;
                y = 1900;
            }
            if ( numPoints > 300 ) {
                x = 3000;
                y = 2000;
            }
            if ( numPoints >= 300 ) {
                x = 3500;
                y = 2500;
            }
            //ArcenPoint GalaxyMapOnly_GalaxyCenter = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;
            ArcenPoint topL = ArcenPoint.Create( -x, y );
            ArcenPoint topR = ArcenPoint.Create( x, y );
            ArcenPoint bottomL = ArcenPoint.Create( -x, y );
            ArcenPoint bottomR = ArcenPoint.Create( x, -y );
            return addPointsInRectangle( numPoints, Context, minDistanceBetweenPlanets, pointsSoFarOrNull,
                    divisibleByX, topL, topR, bottomL, bottomR );
        }

        internal static ThrowawayListCanMemLeak<ArcenPoint> addPointsInRectangle( int numPoints, ArcenHostOnlySimContext Context, int minDistanceBetweenPlanets,
                ThrowawayListCanMemLeak<ArcenPoint> pointsSoFarOrNull, int divisibleByX, ArcenPoint topL, ArcenPoint topR,
                ArcenPoint bottomL, ArcenPoint bottomR )
        {
            //keeps track of previously added planets as well
            int numberFailuresAllowed = 1000;
            ThrowawayListCanMemLeak<ArcenPoint> pointsForThisRectangle = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            int workingMinDist = minDistanceBetweenPlanets;
            int minDistForThisPoint = workingMinDist;
            for ( int i = 0; i < numPoints; i++ )
            {
                ArcenPoint testPoint = BadgerUtilityMethods.GetRandomPointWithinRectangle( topL, topR,
                        bottomL, bottomR, Context );
                if ( divisibleByX != 0 )
                {
                    testPoint.X -= testPoint.X % divisibleByX;
                    testPoint.Y -= testPoint.Y % divisibleByX;
                }
                if ( UtilityMethods.HelperDoesPointListContainPointWithinDistance( pointsSoFarOrNull, testPoint, minDistForThisPoint ) )
                {
                    i--;
                    numberFailuresAllowed--;
                    if ( numberFailuresAllowed <= 0 )
                    {
                        numberFailuresAllowed = 1000;
                        minDistForThisPoint -= 10;
                        if ( minDistForThisPoint < 0 )
                        {
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( " NOT GENERATE A PLANET BAD JUJU GURU #253421 (more than 1000 failures trying to add planets in rectangle, so maybe not room?)" );
                            return pointsForThisRectangle;
                        }
                    }
                    continue;
                }
                minDistForThisPoint = workingMinDist;
                pointsForThisRectangle.Add( testPoint );
                if ( pointsSoFarOrNull != null ) {
                    pointsSoFarOrNull.Add( testPoint );
                }
            }
            return pointsForThisRectangle;
        }

#region RandomlyConnectXPlanets
        public static int RandomlyConnectXPlanets( Galaxy galaxy, int CountOfPlanetsToTryToConnect,
                int preferredDistanceFromOtherPlanets, ArcenHostOnlySimContext Context )
        {
            int numberConnected = 0;

            ThrowawayListCanMemLeak<Planet> planetsRandomized = new ThrowawayListCanMemLeak<Planet>( 500 );
            foreach ( Planet p in galaxy.Planets( true ) )
            {
                planetsRandomized.Add( p );
            }

            ArcenArrays.Randomize( planetsRandomized, Context.RandomToUse );
            ArcenArrays.Randomize( planetsRandomized, Context.RandomToUse );
            ArcenArrays.Randomize( planetsRandomized, Context.RandomToUse );

            while ( numberConnected < CountOfPlanetsToTryToConnect && planetsRandomized.Count > 0 ) {
                int planetIndex = Context.RandomToUse.Next( 0, planetsRandomized.Count );
                Planet planetOne = planetsRandomized[planetIndex];
                planetsRandomized.RemoveAt( planetIndex );
                for ( int i = 0; i < planetsRandomized.Count; i++ ) {
                    Planet planetTwo = planetsRandomized[i];
                    if ( GetCanConnectTheseTwoPlanetsWithoutIntersectingPlanets( galaxy, planetOne, planetTwo,
                                preferredDistanceFromOtherPlanets ) ) {
                        planetsRandomized.RemoveAt( i );
                        planetOne.AddLinkTo( planetTwo );
                        numberConnected++;
                        break;
                    }
                }
            }
            return numberConnected;
        }
#endregion

#region GetCanConnectTheseTwoPlanetsWithoutIntersectingPlanets
        public static bool GetCanConnectTheseTwoPlanetsWithoutIntersectingPlanets( Galaxy galaxy, Planet PlanetOne, Planet PlanetTwo,
                int preferredDistanceFromOtherPlanets )
        {
            if ( PlanetOne == PlanetTwo ) {
                return true; //true if the same planet!
            }
            if ( PlanetOne.GetIsDirectlyLinkedTo( true, PlanetTwo ) ) {
                return true; //true if already linked!
            }
            bool foundProblemWithCurrentLocation = false;

            ArcenPoint lineStart = PlanetOne.GalaxyLocation;
            ArcenPoint lineEnd = PlanetTwo.GalaxyLocation;

            foreach ( Planet otherPlanet in galaxy.Planets( false ) )
            {
                if ( otherPlanet == PlanetOne ) {
                    continue; //don't check anything with the planet we are connecting from in terms of our lines
                }
                if ( otherPlanet == PlanetTwo ) {
                    continue; //don't check anything with the planet we are connecting from in terms of our lines
                }
                if ( foundProblemWithCurrentLocation ) {
                    continue; //saves processing if another thread already found a problem
                }

                //does our line pass through the this other planet?
                int boundingRectMinX = otherPlanet.GalaxyLocation.X - preferredDistanceFromOtherPlanets;
                int boundingRectMinY = otherPlanet.GalaxyLocation.Y - preferredDistanceFromOtherPlanets;
                int widthHeight = preferredDistanceFromOtherPlanets + preferredDistanceFromOtherPlanets;
                if ( Mat.LineIntersectsRectangle( boundingRectMinX, boundingRectMinY, widthHeight, widthHeight, lineStart, lineEnd ) ) {
                    //ooh, that other planet is on a line betwen us and the other planet
                    foundProblemWithCurrentLocation = true;
                    break; //Chris notes: this break won't work, but processing them all in parallel is still probably faster than the chance we might hit this
                }
            }

            return !foundProblemWithCurrentLocation;
        }
#endregion

        /* This returns a matrix where matrix[i][j] == 1 means point i and point j should be connected */
        internal static int[,] createWeakGabrielGraphLinks( ThrowawayListCanMemLeak<ArcenPoint> pointsForGraph )
        {
            //Algorithm: for each node
            //                          find midpoint to another node
            //                          Check that no other planets are in the circle connecting the two nodes
            //                              If no other planets, link these two planets
            //see https://en.wikipedia.org/wiki/Gabriel_graph
            int[,] connectionArray;
            connectionArray = new int[pointsForGraph.Count, pointsForGraph.Count];
            for ( int i = 0; i < pointsForGraph.Count; i++ )
            {
                for ( int j = 0; j < pointsForGraph.Count; j++ )
                {
                    connectionArray[i, j] = 0;
                }
            }
            //Here i and j iterate over potential pairs. For each potential pair, iterate over k,
            //which is every other point, to make sure k is not too close to i and j
            for ( int i = 0; i < pointsForGraph.Count; i++ )
            {
                // s = System.String.Format("Outer Loop: Point {0} of {1}", i, pointsForGraph.Count);
                // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
                for ( int j = 0; j < pointsForGraph.Count; j++ )
                {
                    if ( j == i )
                        continue;

                    ArcenPoint pointOne = pointsForGraph[i];
                    ArcenPoint pointTwo = pointsForGraph[j];
                    ArcenPoint midPoint = ArcenPoint.Create(
                            (pointOne.X + pointTwo.X) / 2,
                            (pointOne.Y + pointTwo.Y) / 2 );
                    int radiusOfCircle = Mat.DistanceBetweenPointsImprecise( pointOne, midPoint ) / 3;

                    bool isThereAnotherPoint = false;

                    for ( int k = 0; k < pointsForGraph.Count; k++ )
                    {
                        //Now check each other planet to see if they would fall into the circle
                        //centered on the midpoint between i and j
                        if ( (k == i) || (k == j) )
                            continue; //don't compare to yourself
                        int distanceFromMidpoint;
                        ArcenPoint pointForCircleCheck = pointsForGraph[k];
                        distanceFromMidpoint = Mat.DistanceBetweenPointsImprecise( pointForCircleCheck, midPoint );
                        if ( (distanceFromMidpoint - radiusOfCircle) <= 0 )
                        {
                            //                        s = System.String.Format("Inner Loop: Compare {0}-->{1}. Planet {2} overlaps circle", i, j, k);
                            //                        ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
                            isThereAnotherPoint = true;
                            k = pointsForGraph.Count; //don't bother checking any other planets, since we have one in the circle
                        }

                    }
                    if ( !isThereAnotherPoint )
                    {
                        // s = System.String.Format("Inner Loop: Putting link between {0} --> {1}", i, j);
                        // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);

                        //if there were no other planets, link planetOne and planetTwo
                        connectionArray[i, j] = 1;
                        connectionArray[j, i] = 1;
                    }
                }
            }
            return connectionArray;
        }

        internal static void linkPlanetLists( Galaxy galaxy, IList<Planet> currentPlanets, IList<Planet> newPlanets, int linksToMake = 1)
        {
            int debugStage = 0;
            bool debug = false;
            if ( debug ) {
                ArcenDebugging.ArcenDebugLogSingleLine( " Trying to link planets lists, attempting to make " + linksToMake, Verbosity.DoNotShow );
                ArcenDebugging.ArcenDebugLogSingleLine( " between lists with " + currentPlanets[0].Name + " (" + currentPlanets.Count + ") and " + newPlanets[0].Name + " (" + newPlanets.Count + ")", Verbosity.DoNotShow );
            }

            Dictionary<Planet, Planet> PotentialConnections = Planet.GetTemporaryPlanetDictOfPlanets( "MapgenTomUtilityMethods-linkPlanetLists-PotentialConnections", 10f );
            if ( PotentialConnections == null ) //blocked for teardown/shutdown; bail
                return;
            try
            {
                int retries = 4;
                do
                {
                    PotentialConnections.Clear();
                    bool avoidReusingPlanets = true;
                    if ( retries < 2 )
                        avoidReusingPlanets = false; //give this a good try, but then expand the search
                    for ( int i = 0; i < currentPlanets.Count; i++ )
                    {
                        debugStage = 100;
                        Planet currPlanet = currentPlanets[i];
                        Planet closestPlanet = null;
                        int closestDist = -1;
                        if ( avoidReusingPlanets )
                        {
                            //if possible, try not to reuse the same planet twice.
                            bool avoidThis = false;
                            foreach ( KeyValuePair<Planet, Planet> pair in PotentialConnections )
                            {
                                if ( pair.Key == currPlanet || pair.Value == currPlanet )
                                {
                                    avoidThis = true;
                                    break;
                                }
                            }
                            if ( avoidThis )
                                continue;
                        }
                        for ( int j = 0; j < newPlanets.Count; j++ )
                        {
                            debugStage = 200;
                            Planet newPlanet = newPlanets[j];
                            if ( avoidReusingPlanets )
                            {
                                //if possible, try not to reuse the same planet twice.
                                bool avoidThis = false;
                                foreach ( KeyValuePair<Planet, Planet> pair in PotentialConnections )
                                {
                                    if ( pair.Key == newPlanet || pair.Value == newPlanet )
                                    {
                                        avoidThis = true;
                                        break;
                                    }
                                }
                                if ( avoidThis )
                                    continue;
                            }

                            int distance = Mat.DistanceBetweenPointsImprecise( currPlanet.GalaxyLocation, newPlanet.GalaxyLocation );
                            if ( distance < closestDist || closestDist == -1 )
                            {
                                if ( BadgerUtilityMethods.WouldLinkCrossOtherPlanets( currPlanet, newPlanet, galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs(), 50 ) )
                                    continue; //nothing that crosses other planets
                                closestPlanet = newPlanet;
                                closestDist = distance;
                            }
                        }
                        debugStage = 300;
                        if ( closestPlanet != null && currPlanet != null )
                            PotentialConnections[currPlanet] = closestPlanet;
                    }
                } while ( PotentialConnections.Count < linksToMake && retries-- > 0 );
                if ( PotentialConnections.Count == 0 )
                {
                    Planet.ReleaseTemporaryPlanetDictOfPlanets( PotentialConnections );
                    if ( debug ) {
                        ArcenDebugging.ArcenDebugLogSingleLine( "Found no potential connections between lists with " + currentPlanets[0].Name + " and " + newPlanets[0].Name, Verbosity.DoNotShow );
                    }
                    return;
                }
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Found " + PotentialConnections.Count + " potential connections between lists with " + currentPlanets[0].Name + " and " + newPlanets[0].Name, Verbosity.DoNotShow );
                debugStage = 400;
                var sortedListOfPotentials = PotentialConnections.SortIntoList( delegate ( KeyValuePair<Planet, Planet> L, KeyValuePair<Planet, Planet> R ) {
                    int lDist = Mat.DistanceBetweenPointsImprecise( L.Key.GalaxyLocation, L.Value.GalaxyLocation );
                    int rDist = Mat.DistanceBetweenPointsImprecise( R.Key.GalaxyLocation, R.Value.GalaxyLocation );
                    return rDist.CompareTo( lDist );
                } );
                debugStage = 500;

                Planet.ReleaseTemporaryPlanetDictOfPlanets( PotentialConnections );
                PotentialConnections = null;
                //No more use of PotentialConnections below this point!
                //It should all be using sortedListOfPotentials now.

                if ( sortedListOfPotentials.Count < linksToMake )
                    linksToMake = sortedListOfPotentials.Count;
                if ( debug )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Potential links are ", Verbosity.DoNotShow );
                    for ( int i = 0; i < sortedListOfPotentials.Count; i++ )
                    {
                        KeyValuePair<Planet, Planet> pair = sortedListOfPotentials[i];
                        ArcenDebugging.ArcenDebugLogSingleLine( "\t " + pair.Key.Name + " -> " + pair.Value.Name, Verbosity.DoNotShow );
                    }
                }

                retries = 10;
                debugStage = 600;

                for ( int i = sortedListOfPotentials.Count - 1; i >= 0; i-- )
                {
                    debugStage = 700;
                    KeyValuePair<Planet, Planet> pair = sortedListOfPotentials[i];
                    debugStage = 710;
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Linking " + pair.Key.Name + " --> " + pair.Value.Name, Verbosity.DoNotShow );

                    debugStage = 800;
                    pair.Key.AddLinkTo( pair.Value );
                    linksToMake--;
                    if ( linksToMake <= 0 )
                        break;
                    debugStage = 850;
                }
                debugStage = 900;
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "linkPlanetLists error at debugStage " + debugStage + " \n" + e );
            }
        }

        //This function will return an array of integers to make it easy
        //to divvy up a set of planets into regions
        //onlyOneOfLowestMin exists because sometimes you only want 1 of the
        //smallest group; if this is "true" then it will make sure we only
        //have at most one group of minPlanetsPerGroup
        public static ThrowawayListCanMemLeak<int> allocatePlanetsIntoGroups( int minPlanetsPerGroup, int maxPlanetsPerGroup, int planetsToAllocate, bool onlyOneOfLowestMin, ArcenHostOnlySimContext Context )
        {
            int maxPossibleGroups = planetsToAllocate / minPlanetsPerGroup;
            int planetsLeftToAllocate = planetsToAllocate;
            ThrowawayListCanMemLeak<int> planetsPerGroup = new ThrowawayListCanMemLeak<int>( 300 );

            // s = System.String.Format( "allocatePlanetsIntoGroups min {0} max {1} planetsToAllocate {2} maxPossibleGroups {3}", minPlanetsPerGroup, maxPlanetsPerGroup, planetsToAllocate, maxPlanetsPerGroup);
            // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);

            for ( int i = 0; i <= maxPossibleGroups; i++ )
            {
                //Hand out batches of planets in groups until
                //we run out of planets
                // s = System.String.Format( "Iter {0} of {1}: {2} planets left", i, maxPossibleGroups, planetsLeftToAllocate);
                // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);

                if ( planetsLeftToAllocate == 0 )
                {
                    //                ArcenDebugging.ArcenDebugLogSingleLine("No planets left; break", Verbosity.DoNotShow);
                    break;
                }
                if ( i == maxPossibleGroups - 1 || planetsLeftToAllocate < maxPlanetsPerGroup )
                {
                    //handle case for the last group, or once the number of remaining
                    //planets drops low enough
                    //              ArcenDebugging.ArcenDebugLogSingleLine("This is the last group", Verbosity.DoNotShow);
                    planetsPerGroup.Add( planetsLeftToAllocate );
                }
                else if ( planetsLeftToAllocate < minPlanetsPerGroup + maxPlanetsPerGroup )
                {
                    //handle the case where we are close to the end (so we don't wind up with a really awkward
                    //last case
                    planetsPerGroup.Add( minPlanetsPerGroup );
                }
                else
                {
                    //pick a nice friendly random number of planets
                    int planetsForThisGroup = Context.RandomToUse.NextWithInclusiveUpperBound( minPlanetsPerGroup, maxPlanetsPerGroup );
                    planetsPerGroup.Add( planetsForThisGroup );
                    if ( planetsForThisGroup == minPlanetsPerGroup && onlyOneOfLowestMin ) {
                        minPlanetsPerGroup++;
                        onlyOneOfLowestMin = false;
                    }
                }
                planetsLeftToAllocate -= planetsPerGroup[i];

                // s = System.String.Format( "Iter {0}: {1} planets left", i, planetsLeftToAllocate);
                // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
            }
            return planetsPerGroup;
        }
    }

    public class Mapgen_Chaotic : IMapGenerator
    {
        public Mapgen_Chaotic() {}

        bool debug = false;
        bool veryVerboseDebug = false;

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_Chaotic : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            GenerateMapStructureOnly_Inner( galaxy, Context, mapConfig, mapType );

            int randomExtraConnections = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "RandomExtraConnections" ).RelatedIntValue;
            MapgenTomUtilityMethods.RandomlyConnectXPlanets( galaxy, randomExtraConnections, 40, Context );

            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, galaxy );
        }

        public void GenerateMapStructureOnly_Inner( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;

                int numberToSeed = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
                if ( numberToSeed < 20 )
                    numberToSeed = 20;

                int percentForOneCircle = 20;
                int circleDone = -1;

                int minPlanetsPerRegion = 2;
                int maxPlanetsPerRegion = 6;
                int radiusPerRegion = 100;
                int distanceBetweenRegions = 205;

                debugCode = 200;
                if ( this.debug )
                {
                    string s = String.Format( "minPlanetsPerRegion: " + minPlanetsPerRegion + " maxPlanetsPerRegion " + maxPlanetsPerRegion + " radiusPerRegion " + radiusPerRegion + " distanceBetweenRegions " + distanceBetweenRegions );
                    ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                }

                bool onlyOneOfSmallestRegion = true;

                int alignmentNumber = 10; //align all points on numbers divisible by this value. It makes things look more organized
                var regionsOfPlanets = MapgenTomUtilityMethods.allocatePlanetsIntoGroups( minPlanetsPerRegion,
                        maxPlanetsPerRegion, numberToSeed, onlyOneOfSmallestRegion,
                        Context );
                if ( this.debug )
                {
                    string s = String.Format( "Planets divvied between {0} regions --> ", regionsOfPlanets.Count );
                    for ( int i = 0; i < regionsOfPlanets.Count; i++ )
                    {
                        s += "(" + i + "=" + regionsOfPlanets[i] + ") ";
                    }
                    ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                }
                debugCode = 300;
                //now for each region, find a center point (chosen randomly)
                //then allocate the points
                var regionCenters = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
                bool allowClustersInPoints = false;

                MapgenTomUtilityMethods.addPointsInStartScreen( regionsOfPlanets.Count, Context,
                        distanceBetweenRegions, regionCenters, allowClustersInPoints,
                        alignmentNumber );
                if ( this.veryVerboseDebug )
                {
                    for ( int i = 0; i < regionCenters.Count; i++ )
                    {
                        string s = String.Format( "Region Center: {0}, {1}", regionCenters[i].X, regionCenters[i].Y );
                        ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                    }
                }
                var allPoints = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
                var allPlanetsPerRegion = new ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<Planet>>( 300 );
                var allPlanets = new ThrowawayListCanMemLeak<Planet>( 500 );

                /* Tuning parameters for how to link the various Nebulae/Asteroids */

                //percentages for linking within a region

                int minDistanceBetweenPlanets = 60;
                //settings for the Nebula
                int percentSpanningTreeInRegion = 00;
                int percentSpanningTreeWithConnectionsInRegion = 05;
                int percentGabrielInRegion = 15;
                int percentRNGInRegion = 80;

                debugCode = 400;
                for ( int i = 0; i < regionCenters.Count; i++ )
                {
                    //For each region, add planets and then link the region together
                    ThrowawayListCanMemLeak<ArcenPoint> pointsForThisRegion;
                    if ( circleDone == -1 &&
                            regionsOfPlanets[i] > 4 && regionsOfPlanets[i] < 9 &&
                            percentForOneCircle > Context.RandomToUse.Next( 0, 100 ) ) // chance of a circle for Asteroids
                    {
                        if ( this.debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Adding a circle", Verbosity.DoNotShow );
                        //sometimes we might want to just make a circle
                        circleDone = i;
                        var temp = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
                        pointsForThisRegion = BadgerUtilityMethods.addCircularPoints( regionsOfPlanets[i], Context, regionCenters[i],
                                radiusPerRegion, temp );
                    }
                    else
                        pointsForThisRegion = BadgerUtilityMethods.addPointsInCircleWithExclusion( regionsOfPlanets[i], Context, regionCenters[i], radiusPerRegion,
                                minDistanceBetweenPlanets, allPoints, regionCenters, radiusPerRegion + 20, alignmentNumber );

                    var planetsForThisRegion = BadgerUtilityMethods.convertPointsToPlanets( pointsForThisRegion, galaxy, Context );
                    if ( this.veryVerboseDebug )
                    {
                        string s = String.Format( "Added planets for region {0} of {1}", i, regionCenters.Count );
                        ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                    }
                    //Now pick a random method of linking these planets together

                    LinkMethod method = BadgerUtilityMethods.getRandomLinkMethod( percentSpanningTreeInRegion, percentGabrielInRegion,
                            percentRNGInRegion, percentSpanningTreeWithConnectionsInRegion,
                            Context );
                    if ( this.veryVerboseDebug )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "Using link method " + method, Verbosity.DoNotShow );
                    }

                    if ( method == LinkMethod.Gabriel )
                    {
                        BadgerUtilityMethods.createGabrielGraph( planetsForThisRegion );
                    }
                    else if ( method == LinkMethod.RNG )
                    {
                        //RNG
                        BadgerUtilityMethods.createRNGGraph( planetsForThisRegion );
                        BadgerUtilityMethods.addRandomConnections( planetsForThisRegion, 1, Context, 2 );
                    }
                    else if ( method == LinkMethod.SpanningTree )
                    {
                        //SpanningTree
                        BadgerUtilityMethods.createMinimumSpanningTree( planetsForThisRegion );
                    }
                    else
                    {
                        //SpanningTree + random connections
                        if ( this.veryVerboseDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "creating minimum spanning tree", Verbosity.DoNotShow );

                        BadgerUtilityMethods.createMinimumSpanningTree( planetsForThisRegion );
                        if ( this.veryVerboseDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "adding some random connections", Verbosity.DoNotShow );

                        BadgerUtilityMethods.addRandomConnections( planetsForThisRegion, 1, Context, 2 );
                    }
                    allPlanetsPerRegion.Add( planetsForThisRegion );
                    allPlanets.AddRange( planetsForThisRegion );
                    if ( this.veryVerboseDebug )
                    {
                        string s = String.Format( "Planets for region {0} of {1} are now linked", i, regionCenters.Count );
                        ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                    }
                }

                int[,] connectionMatrix = MapgenTomUtilityMethods.createWeakGabrielGraphLinks( regionCenters );
                for ( int i = 0; i < regionCenters.Count; i++ )
                {
                    for ( int j = i + 1; j < regionCenters.Count; j++ )
                    {
                        if ( connectionMatrix[i, j] == 1 )
                        {
                            int numberOfConnections = 1;
                            if (Context.RandomToUse.NextWithInclusiveUpperBound( 0, 100 ) < 5)
                                numberOfConnections += Context.RandomToUse.NextWithInclusiveUpperBound( 1, 2 );
                            if (numberOfConnections > 0)
                                MapgenTomUtilityMethods.linkPlanetLists( galaxy, allPlanetsPerRegion[i], allPlanetsPerRegion[j], numberOfConnections);
                        }
                    }
                }

                BadgerUtilityMethods.limitLinksForAllPlanets( allPlanets, Context, 4 );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in Chaotic generation. code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
    }
}

