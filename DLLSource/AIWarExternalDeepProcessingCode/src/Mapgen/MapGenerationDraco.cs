using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Text;
using System.Linq;
using System.Collections.ObjectModel;
using System.IO;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public static class DracoUtilities
    {
        public static bool debug = false;
        public interface IHasArcenPoint
        {
            ArcenPoint galaxyLocation { get; set; }
        }

        public static Planet FindNearestPlanetInList( ThrowawayListCanMemLeak<Planet> outerRing, Planet plnt )
        {
            int dist = int.MaxValue;
            Planet ret = null;
            foreach ( Planet pl in outerRing )
            {
                int d = pl.GalaxyLocation.GetDistanceTo( plnt.GalaxyLocation, false );
                if ( d < dist )
                {
                    dist = d;
                    ret = pl;
                }
            }
            return ret;
        }

        //adds an ellipse of points
        public static ThrowawayListCanMemLeak<ArcenPoint> addElipticalPoints( int numPoints, ArcenHostOnlySimContext Context, ArcenPoint ellipseCenter, int ellipseMajorAxis,
            int ellipseMinorAxis, double rotationRad, ref ThrowawayListCanMemLeak<ArcenPoint> pointsSoFar )
        {
            float startingAngle = Context.RandomToUse.NextFloat( 1, 359 );
            ThrowawayListCanMemLeak<ArcenPoint> pointsForThisCircle = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            for ( int i = 0; i < numPoints; i++ )
            {
                float angle = (360f / (float)numPoints) * (float)i; // yes, this is theoretically an MP-sync problem, but a satisfactory 360 arc was simply not coming from the FInt approximations and I'm figuring the actual full-sync at the beginning of the game should sync things up before they matter
                angle += startingAngle;
                if ( angle >= 360f )
                    angle -= 360f;
                double angleRad = angle / 180 * Math.PI;
                ArcenPoint pointOnRing = ellipseCenter;
                double tan = Math.Sin( angleRad ) / Math.Cos( angleRad );
                double x = ellipseMajorAxis * ellipseMinorAxis / Math.Sqrt( ellipseMinorAxis * ellipseMinorAxis + ellipseMajorAxis * ellipseMajorAxis * tan * tan ); //not using Mat.SqrtFast because of need for double precision
                if ( angle >= 90 ) x = -x;
                if ( angle >= 270 ) x = -x;
                double y = x * Math.Sin( angleRad ) / Math.Cos( angleRad );
                double xn = x * Math.Cos( rotationRad ) - y * Math.Sin( rotationRad );
                double yn = x * Math.Sin( rotationRad ) + y * Math.Cos( rotationRad );
                pointOnRing.X += (int)xn;
                pointOnRing.Y += (int)yn;
                pointsForThisCircle.Add( pointOnRing );
                pointsSoFar.Add( pointOnRing );
            }
            return pointsForThisCircle;
        }

        //adds a circle of points
        public static ThrowawayListCanMemLeak<ArcenPoint> addCircularPoints( int numPoints, ArcenHostOnlySimContext Context, ArcenPoint circleCenter, int circleRadius,
                                                           ref ThrowawayListCanMemLeak<ArcenPoint> pointsSoFar )
        {
            float startingAngle = Context.RandomToUse.NextFloat( 1, 359 );
            ThrowawayListCanMemLeak<ArcenPoint> pointsForThisCircle = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            for ( int i = 0; i < numPoints; i++ )
            {
                float angle = (360f / (float)numPoints) * (float)i; // yes, this is theoretically an MP-sync problem, but a satisfactory 360 arc was simply not coming from the FInt approximations and I'm figuring the actual full-sync at the beginning of the game should sync things up before they matter
                angle += startingAngle;
                if ( angle >= 360f )
                    angle -= 360f;
                ArcenPoint pointOnRing = circleCenter;
                pointOnRing.X += (int)Math.Round( circleRadius * (float)Math.Cos( angle * (Math.PI / 180f) ) );
                pointOnRing.Y += (int)Math.Round( circleRadius * (float)Math.Sin( angle * (Math.PI / 180f) ) );
                pointsForThisCircle.Add( pointOnRing );
                pointsSoFar.Add( pointOnRing );
            }
            return pointsForThisCircle;
        }

        /* This returns a matrix where matrix[i][j] == 1 means point i and point j should be connected 
		   Has the same algorithm as createMinimumSpanningTree, but a seperate implementation */
        public static int[,] createMinimumSpanningTreeLinks( ReadOnlyCollection<IHasArcenPoint> pointsForGraph )
        {
            int[,] connectionArray;
            connectionArray = new int[pointsForGraph.Count, pointsForGraph.Count];
            if ( pointsForGraph.Count < 1 ) return connectionArray;
            for ( int i = 0; i < pointsForGraph.Count; i++ )
            {
                for ( int j = 0; j < pointsForGraph.Count; j++ )
                {
                    connectionArray[i, j] = 0;
                }
            }
            ThrowawayListCanMemLeak<int> verticesNotInTree = new ThrowawayListCanMemLeak<int>( 300 );
            ThrowawayListCanMemLeak<int> verticesInTree = new ThrowawayListCanMemLeak<int>( 300 );
            // ArcenDebugging.ArcenDebugLogSingleLine("Creating minimum spanning tree now", Verbosity.DoNotShow);
            for ( int i = 0; i < pointsForGraph.Count; i++ )
                verticesNotInTree.Add( i );
            //Pick first element, then remove it from the list
            int pointIdx = verticesNotInTree[0];
            verticesNotInTree.RemoveAt( 0 );
            verticesInTree.Add( pointIdx );

            //initialize adjacency matrix for Prim's algorithm
            //the adjacency matrix contains entries as follows
            //pointIdxNotInTree <closest point in tree> <distance to closest point>
            //In the body of the algorithm we look at this matrix to figure out
            //which point to add to the tree next, then update it for the next iteration
            int[,] spanningAdjacencyMatrix;
            spanningAdjacencyMatrix = new int[pointsForGraph.Count, 3];
            for ( int i = 0; i < pointsForGraph.Count; i++ )
            {
                spanningAdjacencyMatrix[i, 0] = i;
                spanningAdjacencyMatrix[i, 1] = -1;
                spanningAdjacencyMatrix[i, 1] = 9999;
            }
            //loop until all vertices are in the tree
            while ( verticesNotInTree.Count > 0 )
            {
                //update the adjacency matrix
                //for each element NOT in the tree, find the closest
                //element in the tree
                for ( int i = 0; i < verticesNotInTree.Count; i++ )
                {
                    int minDistance = 9999;
                    for ( int j = 0; j < verticesInTree.Count; j++ )
                    {
                        int idxNotInTree = verticesNotInTree[i];
                        int idxInTree = verticesInTree[j];
                        ArcenPoint pointNotInTree = ((IHasArcenPoint)pointsForGraph[idxNotInTree]).galaxyLocation;
                        ArcenPoint pointInTree = ((IHasArcenPoint)pointsForGraph[idxInTree]).galaxyLocation;
                        int distance = Mat.DistanceBetweenPointsImprecise( pointNotInTree, pointInTree );
                        if ( distance < minDistance )
                        {
                            spanningAdjacencyMatrix[idxNotInTree, 1] = idxInTree;
                            spanningAdjacencyMatrix[idxNotInTree, 2] = distance;
                            minDistance = distance;
                        }
                    }
                }

                //now pick the closest edge
                // s = System.String.Format("Examine the remaining {0} vertices to find which to add",
                //                          verticesNotInTree.Count);
                // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
                int minDistanceFound = 9999;
                int closestPointIdx = -1;
                int pointToAdd = -1;
                for ( int i = 0; i < verticesNotInTree.Count; i++ )
                {
                    pointIdx = verticesNotInTree[i];
                    // s = System.String.Format( "To find closest edge, examine {0} of {1} (idx {4}), minDistance {2} dist for this point {3}",
                    //                           i, verticesNotInTree.Count , minDistanceFound, spanningAdjacencyMatrix[pointIdx, 2], pointIdx);
                    // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
                    if ( spanningAdjacencyMatrix[pointIdx, 2] == 0 )
                    {
                        //don't try to link a point to itself
                        continue;
                    }
                    if ( spanningAdjacencyMatrix[pointIdx, 2] < minDistanceFound )
                    {
                        minDistanceFound = spanningAdjacencyMatrix[pointIdx, 2];
                        closestPointIdx = spanningAdjacencyMatrix[pointIdx, 1];
                        pointToAdd = pointIdx;
                    }
                }
                // s = System.String.Format( "Adding point idx {0} closest neighbor ({1}. distance {2} to tree", pointToAdd,
                //                           closestPointIdx, minDistanceFound);
                // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
                //Now let's add this point to the Tree
                verticesNotInTree.Remove( pointToAdd );
                verticesInTree.Add( pointToAdd );
                spanningAdjacencyMatrix[pointToAdd, 2] = 9999;
                connectionArray[pointToAdd, closestPointIdx] = 1;
                connectionArray[closestPointIdx, pointToAdd] = 1;
            }
            return connectionArray;
        }

        public static bool DoesPointOverlapPlanet( ArcenPoint pt, int v, ReadOnlyCollection<IHasArcenPoint> currentlist )
        {
            foreach ( IHasArcenPoint ap in currentlist )
            {
                if ( pt.GetDistanceTo( ap.galaxyLocation, false ) <= v )
                {
                    return true;
                }
            }
            return false;
        }

        public static byte[] ReadBitmapFile( string path, out int width, out int height )
        {
            bool debug = false;
            if ( !File.Exists( path ) )
            {
                string origPath = path;
                path = "AIWar2/" + path;
                if ( !File.Exists( path ) )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Warning: " + origPath + " does not exist", Verbosity.DoNotShow );
                    width = -1;
                    height = -1;
                    return null;
                }
            }
            FileStream f = File.OpenRead( path );
            byte[] info = new byte[54];
            f.Read( info, 0, 54 );
            width = info[19] * 256 + info[18];
            height = info[23] * 256 + info[22];
            int w = (int)(Math.Ceiling( width * 3 / 4f ) * 4);
            int size = w * height;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "size of image: " + width + "*" + height, Verbosity.DoNotShow );
            byte[] input = new byte[size];
            byte[] colors = new byte[size];
            f.Read( input, 0, size );
            f.Close();
            for ( int c = 0; c < size; c++ )
            {
                colors[c] = input[c];
            }
            return colors;
        }

        public static int GetProbabilityAt( byte[] colors, int x, int y, int w, int h )
        {
            w = (int)(Math.Ceiling( w * 3 / 4f ) * 4);
            return Math.Max( Math.Max( colors[(y * w + x * 3)], colors[(y * w + x * 3) + 1] ), colors[(y * w + x * 3) + 2] );
        }

        public static Color GetColorAt( byte[] colors, int x, int y, int w, int h )
        {
            w = (int)(Math.Ceiling( w * 3 / 4f ) * 4);
            return new Color( colors[(y * w + x * 3) + 2], colors[(y * w + x * 3) + 1], colors[(y * w + x * 3)] );
        }
    }
    public class Mapgen_D18_Mesh : IMapGenerator
    {
        public Mapgen_D18_Mesh()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        private bool debug = false;
        //Galaxy and Context are always passed in. numberToSeed is the number of planets to create
        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_D18_Mesh : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            //numberToSeed = 30;  //let's override the number of planets desired to something small and manageable
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Welcome to the D18 Mesh generator\n", Verbosity.DoNotShow );
            //AngleDegrees thirty = AngleDegrees.Create(FInt.FromParts(30, 0));
            //AngleDegrees threethirty = AngleDegrees.Create(FInt.FromParts(330, 0));
            int numberToSeed = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            PlanetType planetType = PlanetType.Normal;
            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( 500 );
            ThrowawayListCanMemLeak<Planet> openPlanets = new ThrowawayListCanMemLeak<Planet>( 500 );
            ArcenPoint originPlanetPoint = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;
            Planet originPlanet = galaxy.AddPlanet( planetType, originPlanetPoint,
                World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
            openPlanets.Add( originPlanet );
            //ArcenDebugging.ArcenDebugLogSingleLine( "populate PlanetPoints list\n", Verbosity.DoNotShow );
            for ( int i = 0; i < numberToSeed - 1 && openPlanets.Count > 0; i++ )
            {
                Planet extendFrom = openPlanets[Context.RandomToUse.Next( 0, openPlanets.Count )];
                Planet newplanet = null;
                ArcenPoint newpos;
                bool recheck = false;
                int maxCheck = 30;
                do
                {
                    recheck = false;
                    newpos = extendFrom.GalaxyLocation.GetRandomPointWithinDistance( Context.RandomToUse, 30, 150 );
                    foreach ( Planet item in extendFrom.LinkedNeighbors( false ) )
                    {
                        float deg = extendFrom.GalaxyLocation.GetAngleToDegrees( item.GalaxyLocation ).GetAbsoluteDeltaNeededToGetToOther( extendFrom.GalaxyLocation.GetAngleToDegrees( newpos ) );
                        recheck = recheck || (deg < 30);
                    }
                    int minDistSeen = int.MaxValue;
                    recheck |= this.wouldCollideWithLinks( allPlanets, extendFrom, newpos ); //GetWouldLinkCrossOverOtherPlanets(pl, newpos);
                    if ( !recheck )
                    {
                        foreach ( Planet p in allPlanets )
                        {
                            int dist = p.GalaxyLocation.GetDistanceTo( newpos, false );
                            recheck |= dist < 30;
                            if ( dist < minDistSeen )
                            {
                                minDistSeen = dist;
                            }
                        }
                    }
                    maxCheck--;
                } while ( recheck && maxCheck > 0 );
                if ( recheck && maxCheck <= 0 )
                {
                    openPlanets.Remove( extendFrom );
                    i--;
                    continue;
                }
                planetType = Context.RandomToUse.NextFloat( 0, 1 ) > 0.2 ? PlanetType.Normal : PlanetType.Star;
                newplanet = galaxy.AddPlanet( planetType, newpos,
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                newplanet.AddLinkTo( extendFrom );
                allPlanets.Add( newplanet );
                openPlanets.Add( newplanet );

                foreach ( Planet plnt in openPlanets )
                {
                    if ( newplanet.GetLinkedNeighborCount() <= 4 )
                    {
                        int dist = plnt.GalaxyLocation.GetDistanceTo( newplanet.GalaxyLocation, false );
                        if ( dist >= 30 && dist <= 150 )
                        {
                            bool canConnect = true;
                            float deg = newplanet.GalaxyLocation.GetAngleToDegrees( extendFrom.GalaxyLocation ).GetAbsoluteDeltaNeededToGetToOther( newplanet.GalaxyLocation.GetAngleToDegrees( plnt.GalaxyLocation ) );
                            canConnect = canConnect && deg >= 30;

                            foreach ( Planet item in plnt.LinkedNeighbors( false ) )
                            {
                                deg = plnt.GalaxyLocation.GetAngleToDegrees( item.GalaxyLocation ).GetAbsoluteDeltaNeededToGetToOther( plnt.GalaxyLocation.GetAngleToDegrees( newplanet.GalaxyLocation ) );
                                canConnect = canConnect && deg >= 30;
                            }
                            bool wouldCollide = this.wouldCollideWithLinks( allPlanets, plnt, newplanet );
                            if ( canConnect && !wouldCollide )
                            {
                                newplanet.AddLinkTo( plnt );
                            }
                        }
                    }
                }
                if ( extendFrom.GetLinkedNeighborCount() > 4 )
                {
                    openPlanets.Remove( extendFrom );
                }
                if ( newplanet.GetLinkedNeighborCount() > 4 )
                {
                    openPlanets.Remove( newplanet );
                }
                openPlanets.RemoveAll( x => x.GetLinkedNeighborCount() > 4 );
            }
            galaxy.AddPlanet( planetType, ArcenPoint.Create( 1000, 1000 ),
                World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
            return;
        }

        private bool wouldCollideWithLinks( ThrowawayListCanMemLeak<Planet> toCheck, Planet one, Planet two )
        {
            bool ret = false;
            foreach ( Planet plnt in toCheck )
            {
                foreach ( Planet item in plnt.LinkedNeighbors( false ) )
                {
                    if ( plnt == one || plnt == two || item == one || item == two ) continue;
                    ret |= Mat.LineSegmentIntersectsLineSegment( one.GalaxyLocation, two.GalaxyLocation, plnt.GalaxyLocation, item.GalaxyLocation, 10 );
                    if ( ret ) break;
                }
                if ( ret ) return true;
            }
            return false;
        }

        private bool wouldCollideWithLinks( ThrowawayListCanMemLeak<Planet> toCheck, Planet one, ArcenPoint two )
        {
            bool ret = false;
            foreach ( Planet plnt in toCheck )
            {
                foreach ( Planet item in plnt.LinkedNeighbors( false ) )
                {
                    if ( plnt == one || item == one ) continue;
                    ret |= Mat.LineSegmentIntersectsLineSegment( one.GalaxyLocation, two, plnt.GalaxyLocation, item.GalaxyLocation, 10 );
                    if ( ret ) break;
                }
                if ( ret ) return true;
            }
            return false;
        }
    }

    public class Mapgen_D18_LinkedRings : IMapGenerator
    {
        public Mapgen_D18_LinkedRings()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        private bool debug = false;
        //Galaxy and Context are always passed in. numberToSeed is the number of planets to create
        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_D18_LinkedRings : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Welcome to the D18 Linked Rings v2 generator\n", Verbosity.DoNotShow );
            int numberToSeed = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Seed: " + galaxy.RandomSeedBase + "\nNum planets: " + numberToSeed, Verbosity.DoNotShow );
            int trueNumberToSeed = numberToSeed;
            int numBigRing = (int)Math.Max( numberToSeed / 8, 10 );

            ThrowawayListCanMemLeak<Planet> mainRing = GenerateConnectedRing( galaxy, Context, numBigRing, (int)(Math.Max( numBigRing, 15 ) * 2 * Math.PI * 10), ArcenPoint.ZeroZeroPoint );
            if ( numBigRing >= trueNumberToSeed )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Number of planets in main ring is sufficient. Returning early.\n", Verbosity.DoNotShow );
                return;
            }

            int numSmallRings = numberToSeed / (Context.RandomToUse.NextInclus( 18, 24 ));
            int numMoonRings = numBigRing / 3;
            int numMoons = 4;
            int numPlanetsInMoonRings = numMoonRings * numMoons;
            int numPlanetsInRings = numberToSeed - numBigRing - numPlanetsInMoonRings;
            int numPlanetsPerRing = numPlanetsInRings / numSmallRings - 1;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Small rings: " + numSmallRings + ", Number in main ring: " + numBigRing + "\n", Verbosity.DoNotShow );
            ThrowawayListCanMemLeak<Planet> allSmallRings = new ThrowawayListCanMemLeak<Planet>( 500 );
            for ( float q = 0; Math.Ceiling( q ) < mainRing.Count; q += (float)numBigRing / numSmallRings )
            {
                //ArcenDebugging.ArcenDebugLogSingleLine("    " + q + "/" + mainRing.Count, Verbosity.DoNotShow);
                int pIndex = (int)Math.Ceiling( q );
                ThrowawayListCanMemLeak<Planet> smallRing = GenerateConnectedRing( galaxy, Context, numPlanetsPerRing, (int)(numPlanetsPerRing * 2 * Math.PI * 5), mainRing[pIndex].GalaxyLocation );
                allSmallRings.AddRange( smallRing );
            }
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Build moon rings", Verbosity.DoNotShow );
            ThrowawayListCanMemLeak<Planet> allMoons = new ThrowawayListCanMemLeak<Planet>( 500 );
            for ( int m = 0; m < numMoonRings; m++ )
            {
                int pIndex = Context.RandomToUse.Next( allSmallRings.Count );
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "    " + m + ": " + pIndex, Verbosity.DoNotShow );
                Planet mCenter = allSmallRings[pIndex];
                allMoons.Add( mCenter );
                ThrowawayListCanMemLeak<Planet> moonRing = GenerateConnectedRing( galaxy, Context, numMoons + 2, 120, mCenter.GalaxyLocation );
                allMoons.AddRange( moonRing );
                foreach ( Planet planet1 in mCenter.LinkedNeighbors( false ) )
                {
                    foreach ( Planet planet2 in planet1.LinkedNeighborsAndSelf( false ) )
                    {
                        allSmallRings.Remove( planet2 );
                    }
                }
                //allSmallRings.RemoveAt(pIndex);
            }

            bool extraSatellitesToHitTarget = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "ExtraSatellitesToHitTarget" ).RelatedIntValue > 0;
            if ( extraSatellitesToHitTarget )
            {
                int maxAttempts = 1000;
                while ( galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() < numberToSeed && maxAttempts-- > 0 && allMoons.Count > 0 )
                {
                    Planet moon = allMoons[Context.RandomToUse.Next( 0, allMoons.Count )];
                    ArcenPoint safePoint;
                    if ( BadgerUtilityMethods.GetExtremelySafePointNearPoint( null, moon.GalaxyLocation, galaxy, 40, 80, moon, true, 20, Context, out safePoint ) )
                    {
                        Planet addon = galaxy.AddPlanet( PlanetType.Normal, safePoint,
                            World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                        addon.AddLinkTo( moon );
                    }
                }
            }

            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Total number of planets generated: " + galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise(), Verbosity.DoNotShow );
            foreach ( Planet planet in allMoons )
            {
                ThrowawayListCanMemLeak<Planet> nearby = galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs()
                    .ToThrowawayListCanMemLeak_HugeWasteAndExpensive_AvoidIfAtAllPossible().FindAll( x => x.GalaxyLocation.GetDistanceTo( planet.GalaxyLocation, false ) < 230 );
                System.Collections.Generic.IEnumerable<Planet> planetsWhere = nearby.Where(planet2 => !allMoons.Contains( planet2 ));

                foreach ( Planet planet2 in planetsWhere )
                {
                    //BadgerUtilityMethods.wouldLinkCrossOtherPlanets(planet,planet2,galaxy.Planets)
                    if ( !galaxy.CheckForOverlapWithExistingLines( planet, planet.GalaxyLocation, planet2, planet2.GalaxyLocation, false, true ) )
                    {
                        planet.AddLinkTo( planet2 );
                    }
                }
            }
            foreach ( Planet planet in galaxy.Planets( false ) )
            {
                ThrowawayListCanMemLeak<Planet> nearby = galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs()
                    .ToThrowawayListCanMemLeak_HugeWasteAndExpensive_AvoidIfAtAllPossible().FindAll( x => x.GalaxyLocation.GetDistanceTo( planet.GalaxyLocation, false ) < 130 );
                System.Collections.Generic.IEnumerable<Planet> planetsWhere = nearby.Where(planet2 => !allMoons.Contains( planet2 ));
                foreach ( Planet planet2 in planetsWhere )
                {
                    //BadgerUtilityMethods.wouldLinkCrossOtherPlanets(planet,planet2,galaxy.Planets)
                    if ( !galaxy.CheckForOverlapWithExistingLines( planet, planet.GalaxyLocation, planet2, planet2.GalaxyLocation, false, true ) )
                    {
                        planet.AddLinkTo( planet2 );
                    }
                }
            }

            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs() );
        }

        private ThrowawayListCanMemLeak<Planet> GenerateConnectedRing( Galaxy galaxy, ArcenHostOnlySimContext Context, int numBigRing, int radius, ArcenPoint center )
        {
            ThrowawayListCanMemLeak<ArcenPoint> newPlanets = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            ThrowawayListCanMemLeak<Planet> planets = new ThrowawayListCanMemLeak<Planet>( 500 );
            newPlanets = DracoUtilities.addCircularPoints( numBigRing, Context, center, radius, ref newPlanets );
            Planet origin = null;
            Planet prev = null;
            //ArcenDebugging.ArcenDebugLogSingleLine("Planets in ring: " + newPlanets.Count, Verbosity.DoNotShow);
            int i = 0;
            foreach ( ArcenPoint pt in newPlanets )
            {
                //ArcenDebugging.ArcenDebugLogSingleLine("    " + i, Verbosity.DoNotShow);
                i++;
                if ( galaxy.CheckForTooCloseToExistingNodes( pt, PlanetType.Normal, false ) || galaxy.CheckForTooCloseToExistingLines( pt, PlanetType.Normal, false ) )
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine("    C: " + pt, Verbosity.DoNotShow);
                    int best = int.MaxValue;
                    Planet p = null;
                    foreach ( Planet pl in galaxy.Planets( false ) )
                    {
                        int dist = pl.GalaxyLocation.GetDistanceTo( pt, false );
                        if ( dist >= best )
                            continue;
                        best = dist;
                        p = pl;
                    }
                    //ArcenDebugging.ArcenDebugLogSingleLine("      Best closest: " + best + ", " + p, Verbosity.DoNotShow);
                    if ( origin == null ) origin = p;
                    prev?.AddLinkTo( p );
                    prev = p;
                    planets.Add( p );
                    //ArcenDebugging.ArcenDebugLogSingleLine("    D: " + p, Verbosity.DoNotShow);
                }
                else
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine("    A: " + pt, Verbosity.DoNotShow);
                    Planet p = galaxy.AddPlanet( PlanetType.Normal, pt,
                        World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                    if ( origin == null ) origin = p;
                    prev?.AddLinkTo( p );
                    prev = p;
                    planets.Add( p );
                    //ArcenDebugging.ArcenDebugLogSingleLine("    B: " + p, Verbosity.DoNotShow);
                }
            }
            prev?.AddLinkTo( origin );
            //ArcenDebugging.ArcenDebugLogSingleLine("    E", Verbosity.DoNotShow);
            return planets;
        }

        private bool DoesPlanetLieOnRing( ThrowawayListCanMemLeak<Planet> outerRing, Planet plnt, out Planet conflictEndpoint1, out Planet conflictEndpoint2 )
        {
            bool ret = false;
            Planet o1 = null;
            Planet o2 = null;
            foreach ( Planet rng in outerRing )
            {
                foreach ( Planet item in rng.LinkedNeighbors( false ) )
                {
                    if ( Mat.LineIntersectsRectangleContainingCircle( rng.GalaxyLocation, item.GalaxyLocation, plnt.GalaxyLocation, 10 ) )
                    {
                        o1 = rng;
                        o2 = item;
                        ret = true;
                        break;
                    }
                }
            }
            conflictEndpoint1 = o1;
            conflictEndpoint2 = o2;
            return ret;
        }
    }

    public class Mapgen_D18_Ellipses : IMapGenerator
    {
        public Mapgen_D18_Ellipses()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        private bool debug = false;
        //Galaxy and Context are always passed in. numberToSeed is the number of planets to create
        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_D18_Ellipses : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Welcome to the D18 Swirl generator\n", Verbosity.DoNotShow );
            int numberToSeed = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            float angleOffset = Context.RandomToUse.NextFloat( (float)Math.PI );
            int dir = Context.RandomToUse.NextBool() ? 1 : -1;
            int overPopulation = 0;// BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive(mapConfig, mapType, "OverpopAmount").RelatedIntValue;
            int numRings = Math.Max( ((int)Math.Sqrt( numberToSeed )), 3 ) + 1;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Number of rings " + numRings + "\n", Verbosity.DoNotShow );
            int numPointsPerRing = numberToSeed / numRings;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Points per ring " + numPointsPerRing + "\n", Verbosity.DoNotShow );
            int sizeOfBigLoop = (numRings * (numRings + 1) / 2) * 5 + (numRings) * 36 + 50;
            ThrowawayListCanMemLeak<ArcenPoint> planetPoints = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            Planet origin = null;
            Planet prev = null;
            int angleMod = 0;
            int planetsPlaced = 0;
            int lastNum = -1;
            for ( int j = numRings; j >= 0; j-- )
            {
                bool oncePerRing = true;
                angleMod += 2;
                float angle = j * 10 * dir + angleMod;
                sizeOfBigLoop -= 38 + j * 5 + (j * j) / 3;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Ring " + j + " major axis: " + sizeOfBigLoop + "\n", Verbosity.DoNotShow );
                int numpts = (int)Math.Ceiling( sizeOfBigLoop * .9f * Math.PI * 2 / (48 * 2 + 4) ) - 1;
                if ( numpts < 5 )
                {
                    Planet q = galaxy.AddPlanet( PlanetType.Normal, Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter,
                        World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                    break;
                }
                if ( numpts > 15 )
                    numpts -= Context.RandomToUse.Next( 2 ) + 1;
                lastNum = numpts;
                planetsPlaced += numpts;
                DracoUtilities.addElipticalPoints( numpts, Context, Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter, sizeOfBigLoop + 100, (int)(sizeOfBigLoop * .725) + 72, angle / 180f * Math.PI + angleOffset, ref planetPoints );
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Ring " + j + ": " + numpts + "\n", Verbosity.DoNotShow );
                foreach ( ArcenPoint p in planetPoints )
                {
                    ThrowawayListCanMemLeak<Planet> nearby = galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs()
                        .ToThrowawayListCanMemLeak_HugeWasteAndExpensive_AvoidIfAtAllPossible().FindAll( x => x.GalaxyLocation.GetDistanceTo( p, false ) < 48 );
                    if ( nearby.Count <= 1 )
                    {
                        Planet q = galaxy.AddPlanet( PlanetType.Normal, p,
                            World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                        if ( origin == null ) origin = q;
                        if ( prev != null ) prev.AddLinkTo( q );
                        prev = q;
                    }
                    else
                    {
                        if ( debug && oncePerRing )
                        {
                            oncePerRing = false;
                            ArcenDebugging.ArcenDebugLogSingleLine( "    Planets were too close" + "\n", Verbosity.DoNotShow );
                        }
                        planetsPlaced--;
                    }
                }
                origin?.AddLinkTo( prev );
                origin = prev = null;
                planetPoints = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
                if ( Math.Abs( planetsPlaced - numberToSeed ) < lastNum / 2 )
                {
                    if ( !(lastNum >= overPopulation && (numberToSeed - planetsPlaced) <= overPopulation) )
                    {
                        break;
                    }
                }
            }
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Total planets: " + galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() + "\n", Verbosity.DoNotShow );
            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( 500 );
            allPlanets.AddRange( galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs() );
            foreach ( Planet p in allPlanets )
            {
                ThrowawayListCanMemLeak<Planet> nearby = allPlanets.FindAll( x => x.GalaxyLocation.GetDistanceTo( p.GalaxyLocation, false ) < 144 );
                foreach ( Planet q in nearby )
                {
                    bool linkValid = true;
                    foreach ( Planet plChk in allPlanets )
                    {
                        if ( plChk != p && plChk != q )
                        {
                            foreach ( Planet item in plChk.LinkedNeighbors( false ) )
                            {
                                if ( item != p && item != q )
                                {
                                    linkValid &= !Mat.LineSegmentIntersectsLineSegment( p.GalaxyLocation, q.GalaxyLocation, plChk.GalaxyLocation, item.GalaxyLocation, 5 );
                                }
                                if ( !linkValid ) break;
                            }
                        }
                        if ( !linkValid ) break;
                    }
                    if ( linkValid && p != q )
                        p.AddLinkTo( q );
                }
            }

            /* There seem to be some instances where things don't get fully connected */
            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }

    public class Mapgen_D18_Bubbles : IMapGenerator
    {
        public Mapgen_D18_Bubbles()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        private bool debug = false;
        private class BubbleArrangement : DracoUtilities.IHasArcenPoint
        {
            public static bool debug = false;
            public int radius;
            public ArcenPoint center;
            public int numPlanets;
            public ThrowawayListCanMemLeak<Planet> region;

            public ArcenPoint galaxyLocation
            {
                get
                {
                    return this.center;
                }

                set
                {
                    this.center = value;
                }
            }

            public BubbleArrangement( ArcenPoint _center, int _radius )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "    " + (_radius * 2) + "@" + _center, Verbosity.DoNotShow );
                this.radius = _radius * 2;
                this.center = _center;
                this.numPlanets = Math.Max( (int)(this.radius / (20f)), 5 );
            }
        }
        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_D18_Bubbles : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            BubbleArrangement.debug = debug;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Welcome to the D18 Bubbles generator\n", Verbosity.DoNotShow );
            int numberToSeed = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            int planetsPerBubble = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "PlanetsPerBubble" ).RelatedIntValue;
            FInt percentageOfNormal = (FInt)planetsPerBubble / (FInt)10;
            int numBlobs = numberToSeed / planetsPerBubble;
            int minSize = ( (int)Math.Sqrt( numberToSeed ) * percentageOfNormal ).IntValue;
            int maxSize = ( ( (int)(Math.Sqrt( numberToSeed * 2 ) * 2) + 75 ) * percentageOfNormal ).IntValue;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Creating " + numBlobs + " blobs", Verbosity.DoNotShow );
            ThrowawayListCanMemLeak<BubbleArrangement> regionCenters = new ThrowawayListCanMemLeak<BubbleArrangement>( 300 );
            int seededPlanets = 0;
            for ( int s = 0; s < numBlobs; s++ )
            {
                ArcenPoint pt = ArcenPoint.Create( Context.RandomToUse.Next( -300, 300 ), Context.RandomToUse.Next( -300, 300 ) );
                BubbleArrangement bub = new BubbleArrangement( pt, Context.RandomToUse.Next( minSize, maxSize ) + Context.RandomToUse.Next( minSize, maxSize ) );
                regionCenters.Add( bub );
                seededPlanets += bub.numPlanets;
                if ( seededPlanets >= numberToSeed )
                    break;
            }
            if ( seededPlanets < numberToSeed )
            {
                ArcenPoint pt = ArcenPoint.Create( Context.RandomToUse.Next( -500, 500 ), Context.RandomToUse.Next( -500, 500 ) );
                BubbleArrangement bub = new BubbleArrangement( pt, 0 );
                bub.numPlanets = Math.Max( numberToSeed - seededPlanets, 3 );
                bub.radius = bub.numPlanets * 9 * 4;
                regionCenters.Add( bub );
            }
            bool needsRecheck = false;
            int maxDepth = 150;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Spreading blobs out... ", Verbosity.DoNotShow );
            do
            {
                needsRecheck = false;
                //ArcenDebugging.ArcenDebugLogSingleLine("  Spreading groups out... " + maxDepth, Verbosity.DoNotShow);
                for ( int i = 0; i < regionCenters.Count; i++ )
                {
                    for ( int j = i + 1; j < regionCenters.Count; j++ )
                    {
                        ArcenPoint pt1 = regionCenters[i].center;
                        ArcenPoint pt2 = regionCenters[j].center;
                        if ( pt1 != pt2 && pt1.GetDistanceTo( pt2, false ) <= regionCenters[i].radius + regionCenters[j].radius + 120 )
                        {
                            if ( pt1.GetDistanceTo( pt2, false ) <= regionCenters[i].radius + regionCenters[j].radius + 90 )
                            {
                                needsRecheck = true;
                            }
                            ArcenPoint rel = ArcenPoint.Create( pt1.X - pt2.X, pt1.Y - pt2.Y );
                            double d = 1;// Mat.SqrtFast(rel.X * rel.X + rel.Y * rel.Y);
                                         //ArcenDebugging.ArcenDebugLogSingleLine("    Shifting " + pt1 + " & " + pt2 + " by " + rel, Verbosity.DoNotShow);
                            pt1.X += (int)(20 * Math.Sign( Math.Floor( rel.X / d ) ));
                            pt2.X += -(int)(20 * Math.Sign( Math.Floor( rel.X / d ) ));
                            pt1.Y += (int)(20 * Math.Sign( Math.Floor( rel.Y / d ) ));
                            pt2.Y += -(int)(20 * Math.Sign( Math.Floor( rel.Y / d ) ));
                            regionCenters[i].center = pt1;
                            regionCenters[j].center = pt2;
                        }
                    }
                }
                maxDepth--;
            } while ( needsRecheck && maxDepth > 0 );
            //ArcenDebugging.ArcenDebugLogSingleLine("Spreading Complete", Verbosity.DoNotShow);
            //Dictionary<ArcenPoint, ThrowawayListCanMemLeak<Planet>> allPlanetsMap = Dictionary<ArcenPoint, ThrowawayListCanMemLeak<Planet>>.Create_WillNeverBeGCed();
            Planet origin, prev;
            foreach ( BubbleArrangement pt in regionCenters )
            {
                origin = prev = null;
                ThrowawayListCanMemLeak<ArcenPoint> cirlce = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
                int rad = pt.radius;
                int planetsNeeded = pt.numPlanets;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "  Blob " + pt + " has radius " + rad + " and " + planetsNeeded + " planets", Verbosity.DoNotShow );
                DracoUtilities.addCircularPoints( planetsNeeded, Context, pt.center, rad, ref cirlce );
                pt.region = new ThrowawayListCanMemLeak<Planet>( 500 );
                foreach ( ArcenPoint pl in cirlce )
                {
                    Planet plt = galaxy.AddPlanet( PlanetType.Normal, pl,
                        World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                    pt.region.Add( plt );
                    if ( origin == null ) origin = plt;
                    if ( prev != null ) prev.AddLinkTo( plt );
                    prev = plt;
                }
                origin?.AddLinkTo( prev );
                //allPlanetsMap.Add(pt, circPl);
            }
            //ArcenDebugging.ArcenDebugLogSingleLine("  Circles added", Verbosity.DoNotShow);

            ThrowawayListCanMemLeak<DracoUtilities.IHasArcenPoint> pointsList = new ThrowawayListCanMemLeak<DracoUtilities.IHasArcenPoint>( regionCenters.Count );
            foreach ( BubbleArrangement arr in regionCenters )
                pointsList.Add( (DracoUtilities.IHasArcenPoint)arr );

            int[,] connectionMatrix = DracoUtilities.createMinimumSpanningTreeLinks( new ReadOnlyCollection<DracoUtilities.IHasArcenPoint>( pointsList.ToBuiltInGenericList_HugeWasteAndExpensive_AvoidIfAtAllPossible() ) );

            for ( int i = 0; i < regionCenters.Count; i++ )
            {
                for ( int j = i + 1; j < regionCenters.Count; j++ )
                {
                    //ThrowawayListCanMemLeak<Planet> region1, region2;
                    //allPlanetsMap.TryGetValue(regionCenters[i], out region1);
                    //allPlanetsMap.TryGetValue(regionCenters[j], out region2);
                    Planet a = DracoUtilities.FindNearestPlanetInList( regionCenters[i].region, regionCenters[j].region[0] );
                    Planet b = DracoUtilities.FindNearestPlanetInList( regionCenters[j].region, a );
                    a = DracoUtilities.FindNearestPlanetInList( regionCenters[i].region, b );
                    if ( j >= regionCenters.Count )
                        ArcenDebugging.ArcenDebugLogSingleLine( "BUG! FIXME", Verbosity.DoNotShow );
                    if ( connectionMatrix[i, j] == 1 )
                    {
                        if ( regionCenters[i] == null | regionCenters[j] == null )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "BUG! FIXME", Verbosity.DoNotShow );
                        }
                        else
                        {
                            a.AddLinkTo( b );
                        }
                    }
                    else if ( regionCenters[i].center.GetDistanceTo( regionCenters[j].center, false ) < 150 + regionCenters[i].radius + regionCenters[j].radius )
                    {
                        //a.AddLinkTo(b);
                        bool isvalid = true;
                        foreach ( Planet plChk in galaxy.Planets( false ) )
                        {
                            if ( plChk != a && plChk != b )
                            {
                                foreach ( Planet item in plChk.LinkedNeighbors( false ) )
                                {
                                    if ( item.Index > plChk.Index && item != a && item != b )
                                    {
                                        isvalid &= !Mat.LineSegmentIntersectsLineSegment( a.GalaxyLocation, b.GalaxyLocation, plChk.GalaxyLocation, item.GalaxyLocation, 5 );
                                    }
                                    if ( !isvalid ) break;
                                }
                            }
                        }
                        if ( isvalid )
                            a.AddLinkTo( b );
                    }
                }
            }

            int randomExtraConnections = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "RandomExtraConnections" ).RelatedIntValue;
            BadgerUtilityMethods.RandomlyConnectXPlanetsWithoutIntersectingOthers( galaxy, randomExtraConnections, 40, 20, Context );

            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, galaxy );

            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Galaxy complete! Galaxy has " + galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() + " planets.", Verbosity.DoNotShow );
        }
    }
    public class Mapgen_D18_DensityFile : IMapGenerator
    {
        public Mapgen_D18_DensityFile()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        private bool debug = false;
        private class MSTArrangement : DracoUtilities.IHasArcenPoint
        {
            public readonly Planet planet;
            public readonly int bitmapVal;

            public ArcenPoint galaxyLocation
            {
                get
                {
                    return this.planet.GalaxyLocation;
                }

                set
                {
                    //Planet. = value;
                }
            }

            public MSTArrangement( Planet p, int v )
            {
                this.planet = p;
                this.bitmapVal = v;
            }
        }
        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_D18_DensityFile : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            int numberToSeed = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            int mapExtents = 300;
            if ( numberToSeed > 80 )
            {
                mapExtents = (int)Math.Round( (Mat.SqrtFastAnyThread( numberToSeed - 80 ) / 15 + 1) * mapExtents );
            }
            string path = "./GameData/Configuration/MapType/map_c.bmp";
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Welcome to the D18 Density Map generator\n", Verbosity.DoNotShow );

            int width;
            int height;
            byte[] colors = DracoUtilities.ReadBitmapFile( path, out width, out height );
            if ( colors == null )
                return;
            ThrowawayListCanMemLeak<MSTArrangement> allPoints = new ThrowawayListCanMemLeak<MSTArrangement>( 300 );
            float xDiv = mapExtents * 2f / width;
            float yDiv = mapExtents * 2f / height;
            for ( int n = 0; n < numberToSeed; )
            {
                ArcenPoint pt = ArcenPoint.Create( Context.RandomToUse.Next( -mapExtents, mapExtents ), Context.RandomToUse.Next( -mapExtents, mapExtents ) );
                ThrowawayListCanMemLeak<DracoUtilities.IHasArcenPoint> pointsList = new ThrowawayListCanMemLeak<DracoUtilities.IHasArcenPoint>( allPoints.Count );
                foreach ( MSTArrangement arr in allPoints )
                    pointsList.Add( (DracoUtilities.IHasArcenPoint)arr );

                if ( DracoUtilities.DoesPointOverlapPlanet( pt, 20, new ReadOnlyCollection<DracoUtilities.IHasArcenPoint>( pointsList.ToBuiltInGenericList_HugeWasteAndExpensive_AvoidIfAtAllPossible() ) ) )
                {
                    continue;
                }
                int y = DracoUtilities.GetProbabilityAt( colors, (int)Math.Floor( (pt.X + mapExtents) / xDiv ), (int)Math.Floor( (pt.Y + mapExtents) / yDiv ), width, height );
                int r = Context.RandomToUse.Next( 256 );
                //ArcenDebugging.ArcenDebugLogSingleLine("    " + pt + ": " + r + " > " + y, Verbosity.DoNotShow);
                if ( r < y )
                {
                    allPoints.Add( new MSTArrangement( galaxy.AddPlanet( PlanetType.Normal, pt,
                        World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) ), y ) );
                    n++;
                }
            }

            ThrowawayListCanMemLeak<DracoUtilities.IHasArcenPoint> pointsListOuter = new ThrowawayListCanMemLeak<DracoUtilities.IHasArcenPoint>( allPoints.Count );
            foreach ( MSTArrangement arr in allPoints )
                pointsListOuter.Add( (DracoUtilities.IHasArcenPoint)arr );

            int[,] connectionMatrix = DracoUtilities.createMinimumSpanningTreeLinks( new ReadOnlyCollection<DracoUtilities.IHasArcenPoint>( pointsListOuter.ToBuiltInGenericList_HugeWasteAndExpensive_AvoidIfAtAllPossible() ) );
            for ( int i = 0; i < allPoints.Count; i++ )
            {
                for ( int j = i + 1; j < allPoints.Count; j++ )
                {
                    Planet a = allPoints[i].planet;
                    Planet b = allPoints[j].planet;
                    if ( j >= allPoints.Count )
                        ArcenDebugging.ArcenDebugLogSingleLine( "BUG! FIXME", Verbosity.DoNotShow );
                    if ( connectionMatrix[i, j] == 1 )
                    {
                        if ( allPoints[i] == null | allPoints[j] == null )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "BUG! FIXME", Verbosity.DoNotShow );
                        }
                        else
                        {
                            a.AddLinkTo( b );
                        }
                    }
                    else if ( allPoints[i].galaxyLocation.GetDistanceTo( allPoints[j].galaxyLocation, false ) < (330 - Math.Max( allPoints[i].bitmapVal, allPoints[j].bitmapVal )) / 3 || (allPoints[i].galaxyLocation.GetDistanceTo( allPoints[j].galaxyLocation, false ) > (70 + Math.Min( allPoints[i].bitmapVal, allPoints[j].bitmapVal )) / 6 && allPoints[i].galaxyLocation.GetDistanceTo( allPoints[j].galaxyLocation, false ) < (16 + Math.Min( allPoints[i].bitmapVal, allPoints[j].bitmapVal )) / 4) )
                    {
                        Color ci = DracoUtilities.GetColorAt( colors, (int)Math.Floor( (allPoints[i].galaxyLocation.X + mapExtents) / xDiv ), (int)Math.Floor( (allPoints[i].galaxyLocation.Y + mapExtents) / yDiv ), width, height );
                        Color cj = DracoUtilities.GetColorAt( colors, (int)Math.Floor( (allPoints[j].galaxyLocation.X + mapExtents) / xDiv ), (int)Math.Floor( (allPoints[j].galaxyLocation.Y + mapExtents) / yDiv ), width, height );
                        float H, S, V;
                        Color.RGBToHSV( ci, out H, out S, out V );
                        ci.r = (float)Math.Round( H * 32 ) / 32;
                        ci.g = 0;
                        ci.b = 0;
                        Color.RGBToHSV( cj, out H, out S, out V );
                        cj.r = (float)Math.Round( H * 32 ) / 32;
                        cj.g = 0;
                        cj.b = 0;
                        //ArcenDebugging.ArcenDebugLogSingleLine("    a: " + allPoints[i].galaxyLocation + ": " + ci, Verbosity.DoNotShow);
                        //ArcenDebugging.ArcenDebugLogSingleLine("    b: " + allPoints[j].galaxyLocation + ": " + cj, Verbosity.DoNotShow);
                        if ( ci == cj && allPoints[i].galaxyLocation.GetDistanceTo( allPoints[j].galaxyLocation, false ) < 70 )
                        {
                            bool isvalid = true;
                            foreach ( Planet plChk in galaxy.Planets( false ) )
                            {
                                if ( plChk != a && plChk != b )
                                {
                                    isvalid &= !Mat.LineIntersectsRectangleContainingCircle( a.GalaxyLocation, b.GalaxyLocation, plChk.GalaxyLocation, 10 );
                                    foreach ( Planet item in plChk.LinkedNeighbors( false ) )
                                    {
                                        if ( item.Index > plChk.Index && item != a && item != b )
                                        {
                                            isvalid &= !Mat.LineSegmentIntersectsLineSegment( a.GalaxyLocation, b.GalaxyLocation, plChk.GalaxyLocation, item.GalaxyLocation, 5 );

                                        }
                                        if ( !isvalid ) break;
                                    }
                                }
                            }
                            if ( isvalid )
                                a.AddLinkTo( b );
                        }
                    }
                }
            }
            ThrowawayListCanMemLeak<Planet> lowConnectivity = new ThrowawayListCanMemLeak<Planet>( 500 );
            ThrowawayListCanMemLeak<Planet> semiLowConnectivity = new ThrowawayListCanMemLeak<Planet>( 500 );
            foreach ( Planet p in galaxy.Planets( false ) )
            {
                if ( p.GetLinkedNeighborCount() == 1 )
                {
                    lowConnectivity.Add( p );
                }
                if ( p.GetLinkedNeighborCount() <= 2 )
                {
                    semiLowConnectivity.Add( p );
                }
            }
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( lowConnectivity.Count + " low connectivity nodes, attempting outbound linkages...", Verbosity.DoNotShow );
            //ArcenDebugging.ArcenDebugLogSingleLine("semi-low con: " + semiLowConnectivity.Count, Verbosity.DoNotShow);
            foreach ( Planet p in lowConnectivity )
            {
                if ( p.GetLinkedNeighborCount() > 1 ) continue;
                //ArcenDebugging.ArcenDebugLogSingleLine("  checking " + p.GalaxyLocation, Verbosity.DoNotShow);
                int dist = int.MaxValue;
                Planet ret = null;
                Color ci = DracoUtilities.GetColorAt( colors, (int)Math.Floor( (p.GalaxyLocation.X + mapExtents) / xDiv ), (int)Math.Floor( (p.GalaxyLocation.Y + mapExtents) / yDiv ), width, height );
                float H, S, V;
                Color.RGBToHSV( ci, out H, out S, out V );
                ci.r = (float)Math.Round( H * 32 ) / 32;
                ci.g = 0;
                ci.b = 0;
                //find closest in same color region without overlapping other planets
                foreach ( Planet pl in semiLowConnectivity )
                {
                    if ( p == pl || p.GetIsDirectlyLinkedTo( false, pl ) ) continue;
                    int d = pl.GalaxyLocation.GetDistanceTo( p.GalaxyLocation, false );
                    if ( d < dist )
                    {
                        Color cj = DracoUtilities.GetColorAt( colors, (int)Math.Floor( (pl.GalaxyLocation.X + mapExtents) / xDiv ), (int)Math.Floor( (pl.GalaxyLocation.Y + mapExtents) / yDiv ), width, height );
                        Color.RGBToHSV( cj, out H, out S, out V );
                        cj.r = (float)Math.Round( H * 32 ) / 32;
                        cj.g = 0;
                        cj.b = 0;
                        if ( ci == cj )
                        {
                            bool isValid = true;
                            foreach ( Planet plChk in galaxy.Planets( false ) )
                            {
                                if ( plChk != p && plChk != pl && Mat.LineIntersectsRectangleContainingCircle( p.GalaxyLocation, pl.GalaxyLocation, plChk.GalaxyLocation, 15 ) )
                                {
                                    isValid = false;
                                    break;
                                }
                            }
                            if ( isValid )
                            {
                                dist = d;
                                ret = pl;
                            }
                        }
                    }
                }
                if ( ret != null )
                {
                    p.AddLinkTo( ret );
                }
            }
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Galaxy complete! Galaxy has " + galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() + " planets.", Verbosity.DoNotShow );
        }
    }
}
