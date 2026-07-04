using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    //Nota Bene: most of this code is old and idiosyncratic. If you want to use similar functions for your own code I would recommend copying it and modifying it to suit your needs
    public enum ClusterLinkAlgorithm
    {
        Large,
        //         Microcosm,
        Medium,
        Nebula,
        Small,
        Butterfly,
        Fractured,
        //         Unnamed3,
        Length
    }

    public enum LinkMethod
    {
        None,
        SpanningTree,
        Gabriel,
        RNG,
        SpanningTreeWithConnections
    }

    /* This is a helper class. It mostly contains routines for "Connect a list of planets
       according to a given algorithm" and "Place planets in an interesting but
       pleasing fashion", with a few others */

    public static class BadgerUtilityMethods
    {
        public static ArcenPoint GetSafePointNearPoint( Planet nomadPlanetOrNull, ArcenPoint origDesiredLocation, Galaxy galaxy, int preferredDistanceFromOtherPlanets, int preferredDistanceFromPoint, ArcenHostOnlySimContext Context )
        {
            //used for the initial nomad seeding, and also for whenever a specific nomad is moving
            int retries = 200;
            ArcenPoint desiredLocation = origDesiredLocation;
            bool foundTooClosePlanet = false;
            do
            {
                foundTooClosePlanet = false;
                foreach ( Planet otherPlanet in galaxy.Planets( false ) )
                {
                    if ( nomadPlanetOrNull != null && nomadPlanetOrNull == otherPlanet )
                        continue;
                    if ( foundTooClosePlanet )
                        continue; //saves processing if another thread already found a problem

                    int distanceBetween = Mat.ApproxDistanceBetweenPointsFast( desiredLocation, otherPlanet.GalaxyLocation, preferredDistanceFromOtherPlanets );
                    if ( distanceBetween < preferredDistanceFromOtherPlanets )
                    {
                        foundTooClosePlanet = true;
                        break; //Chris notes: this break won't work, but processing them all in parallel is still probably faster than the chance we might hit this
                    }
                }

                if ( foundTooClosePlanet )
                {
                    //expand the search radius
                    preferredDistanceFromPoint += preferredDistanceFromPoint / 10;
                    preferredDistanceFromOtherPlanets -= preferredDistanceFromOtherPlanets / 20;
                    desiredLocation = origDesiredLocation.GetRandomPointWithinDistance( Context.RandomToUse, 0, preferredDistanceFromPoint );
                }
            } while ( retries-- > 0 && foundTooClosePlanet );
            return desiredLocation;
        }

        #region GetExtremelySafePointNearPoint
        public static bool GetExtremelySafePointNearPoint( Planet nomadPlanetOrNull, ArcenPoint origDesiredLocation, Galaxy galaxy,
            int preferredDistanceFromOtherPlanets, int preferredDistanceFromPoint, Planet planetWeAreConnectingFromOrNull,
            bool checkLineStatusOfPlanetWeAreConnectingFrom,
            int preferredDistanceFromOtherLinesIfCheckingThose, ArcenHostOnlySimContext Context, out ArcenPoint result )
        {
            int retries = 200;
            ArcenPoint desiredLocation = origDesiredLocation;
            int distanceFromPoint = preferredDistanceFromPoint;

            bool foundProblemWithCurrentLocation = false;
            do
            {
                foundProblemWithCurrentLocation = false;
                foreach ( Planet otherPlanet in galaxy.Planets( false ) )
                {
                    if ( nomadPlanetOrNull != null && nomadPlanetOrNull == otherPlanet )
                        continue;
                    if ( foundProblemWithCurrentLocation )
                        continue; //saves processing if another thread already found a problem

                    int distanceBetween = Mat.ApproxDistanceBetweenPointsFast( desiredLocation, otherPlanet.GalaxyLocation, preferredDistanceFromOtherPlanets );
                    if ( distanceBetween < preferredDistanceFromOtherPlanets )
                    {
                        foundProblemWithCurrentLocation = true;
                        break; //Chris notes: this break won't work, but processing them all in parallel is still probably faster than the chance we might hit this
                    }
                }

                if ( !foundProblemWithCurrentLocation )
                {
                    //we were not too close to any other planets, so find out if we are on any lines between other planets
                    int boundingRectMinX = desiredLocation.X - preferredDistanceFromOtherPlanets;
                    int boundingRectMinY = desiredLocation.Y - preferredDistanceFromOtherPlanets;
                    int widthHeight = preferredDistanceFromOtherPlanets + preferredDistanceFromOtherPlanets;

                    foreach ( Planet otherPlanet in galaxy.Planets( false ) )
                    {
                        if ( otherPlanet == planetWeAreConnectingFromOrNull )
                            continue; //don't check anything with the planet we are connecting from in terms of our lines
                        if ( nomadPlanetOrNull != null && nomadPlanetOrNull == otherPlanet )
                            continue;
                        if ( foundProblemWithCurrentLocation )
                            continue; //saves processing if another thread already found a problem

                        foreach ( Planet neighbor in otherPlanet.LinkedNeighborsWithIndexHigherThanSelf( true ) )
                        {
                            if ( Mat.LineIntersectsRectangle( boundingRectMinX, boundingRectMinY, widthHeight, widthHeight, otherPlanet.GalaxyLocation, neighbor.GalaxyLocation ) )
                            {
                                //ooh, we're on a line between other planets.  Don't do that.
                                foundProblemWithCurrentLocation = true;
                                break; //Chris notes: this break will work, since it's not parallel on this inner loop
                            }
                        }
                    }
                }

                if ( planetWeAreConnectingFromOrNull != null && checkLineStatusOfPlanetWeAreConnectingFrom ) //now we know the point is okay, but does the line connecting this point to its parent also pass muster?
                {
                    if ( !foundProblemWithCurrentLocation )
                    {
                        ArcenPoint lineStart = planetWeAreConnectingFromOrNull.GalaxyLocation;
                        ArcenPoint lineEnd = desiredLocation;

                        foreach ( Planet otherPlanet in galaxy.Planets( false ) )
                        {
                            if ( otherPlanet == planetWeAreConnectingFromOrNull )
                                continue; //don't check anything with the planet we are connecting from in terms of our lines
                            if ( nomadPlanetOrNull != null && nomadPlanetOrNull == otherPlanet )
                                continue;
                            if ( foundProblemWithCurrentLocation )
                                continue; //saves processing if another thread already found a problem

                            //does our line pass through the this other planet?
                            int boundingRectMinX = otherPlanet.GalaxyLocation.X - preferredDistanceFromOtherPlanets;
                            int boundingRectMinY = otherPlanet.GalaxyLocation.Y - preferredDistanceFromOtherPlanets;
                            int widthHeight = preferredDistanceFromOtherPlanets + preferredDistanceFromOtherPlanets;
                            if ( Mat.LineIntersectsRectangle( boundingRectMinX, boundingRectMinY, widthHeight, widthHeight, lineStart, lineEnd ) )
                            {
                                //ooh, that other planet is on a line betwen us and the other planet
                                foundProblemWithCurrentLocation = true;
                                break; //Chris notes: this break won't work, but processing them all in parallel is still probably faster than the chance we might hit this
                            }

                            //okay, we didn't intersect this other planet... so does our line intersect any of the other lines?
                            foreach ( Planet neighbor in otherPlanet.LinkedNeighborsWithIndexHigherThanSelf( true ) )
                            {
                                if ( neighbor == planetWeAreConnectingFromOrNull )
                                    continue; //don't check anything with the planet we are connecting from in terms of our lines
                                if ( Mat.LineSegmentIntersectsLineSegment( lineStart, lineEnd, otherPlanet.GalaxyLocation, neighbor.GalaxyLocation, preferredDistanceFromOtherLinesIfCheckingThose ) )
                                {
                                    //ooh, our line goes through a line between other planets.  Don't do that.
                                    foundProblemWithCurrentLocation = true;
                                    break; //Chris notes: this break will work, since it's not parallel on this inner loop
                                }
                            }
                        }
                    }
                }

                if ( foundProblemWithCurrentLocation )
                {
                    //expand the search radius
                    distanceFromPoint += distanceFromPoint / 10;
                    if (distanceFromPoint > 5 * preferredDistanceFromPoint) {
                        distanceFromPoint = 5 * preferredDistanceFromPoint;
                    }
                    preferredDistanceFromOtherPlanets -= preferredDistanceFromOtherPlanets / 20;
                    desiredLocation = origDesiredLocation.GetRandomPointWithinDistance( Context.RandomToUse, 0, distanceFromPoint );
                }
            } while ( retries-- > 0 && foundProblemWithCurrentLocation );

            result = desiredLocation;

            if ( foundProblemWithCurrentLocation )
                return false; //so don't use that result!
            return true;
        }
        #endregion

        #region RandomlyConnectXPlanetsWithoutIntersectingOthers
        public static int RandomlyConnectXPlanetsWithoutIntersectingOthers( Galaxy galaxy, int CountOfPlanetsToTryToConnect,
            int preferredDistanceFromOtherPlanets, int preferredDistanceFromOtherLines, ArcenHostOnlySimContext Context )
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

            while ( numberConnected < CountOfPlanetsToTryToConnect && planetsRandomized.Count > 0 )
            {
                int planetIndex = Context.RandomToUse.Next( 0, planetsRandomized.Count );
                Planet planetOne = planetsRandomized[planetIndex];
                planetsRandomized.RemoveAt( planetIndex );
                for ( int i = 0; i < planetsRandomized.Count; i++ )
                {
                    Planet planetTwo = planetsRandomized[i];
                    if ( GetCanConnectTheseTwoPlanetsWithoutIntersectingOthers( galaxy, planetOne, planetTwo,
                        preferredDistanceFromOtherPlanets, preferredDistanceFromOtherLines ) )
                    {
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

        #region GetCanConnectTheseTwoPlanetsWithoutIntersectingOthers
        public static bool GetCanConnectTheseTwoPlanetsWithoutIntersectingOthers( Galaxy galaxy, Planet PlanetOne, Planet PlanetTwo,
            int preferredDistanceFromOtherPlanets, int preferredDistanceFromOtherLines )
        {
            if ( PlanetOne == PlanetTwo )
                return true; //true if the same planet!
            if ( PlanetOne.GetIsDirectlyLinkedTo( true, PlanetTwo ) )
                return true; //true if already linked!
            bool foundProblemWithCurrentLocation = false;

            ArcenPoint lineStart = PlanetOne.GalaxyLocation;
            ArcenPoint lineEnd = PlanetTwo.GalaxyLocation;

            foreach ( Planet otherPlanet in galaxy.Planets( false ) )
            {
                if ( otherPlanet == PlanetOne )
                    continue; //don't check anything with the planet we are connecting from in terms of our lines
                if ( otherPlanet == PlanetTwo )
                    continue; //don't check anything with the planet we are connecting from in terms of our lines
                if ( foundProblemWithCurrentLocation )
                    continue; //saves processing if another thread already found a problem

                //does our line pass through the this other planet?
                int boundingRectMinX = otherPlanet.GalaxyLocation.X - preferredDistanceFromOtherPlanets;
                int boundingRectMinY = otherPlanet.GalaxyLocation.Y - preferredDistanceFromOtherPlanets;
                int widthHeight = preferredDistanceFromOtherPlanets + preferredDistanceFromOtherPlanets;
                if ( Mat.LineIntersectsRectangle( boundingRectMinX, boundingRectMinY, widthHeight, widthHeight, lineStart, lineEnd ) )
                {
                    //ooh, that other planet is on a line betwen us and the other planet
                    foundProblemWithCurrentLocation = true;
                    break; //Chris notes: this break won't work, but processing them all in parallel is still probably faster than the chance we might hit this
                }

                //okay, we didn't intersect this other planet... so does our line intersect any of the other lines?
                foreach ( Planet neighbor in otherPlanet.LinkedNeighborsWithIndexHigherThanSelf( true ) )
                {
                    if ( neighbor == PlanetOne )
                        continue; //don't check anything with the planet we are connecting from in terms of our lines
                    if ( neighbor == PlanetTwo )
                        continue; //don't check anything with the planet we are connecting from in terms of our lines

                    if ( Mat.LineSegmentIntersectsLineSegment( lineStart, lineEnd, otherPlanet.GalaxyLocation, neighbor.GalaxyLocation, preferredDistanceFromOtherLines ) )
                    {
                        //ooh, our line goes through a line between other planets.  Don't do that.
                        foundProblemWithCurrentLocation = true;
                        break; //Chris notes: this break will work, since it's not parallel on this inner loop
                    }
                }
            }

            return !foundProblemWithCurrentLocation;
        }
        #endregion

        public static MapSettingOptionChoice getSettingValueMapSettingOptionChoice_Expensive( MapConfiguration mapConfig, string settingName )
        {
            if ( mapConfig == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Null mapConfig passed into getSettingValueMapSettingOptionChoice_Expensive!", Verbosity.ShowAsError );
                return null;
            }

            MapSettingOption option = mapConfig.MapType.GetOptionByName( settingName );
            if (option == null)
            {
                ArcenDebugging.ArcenDebugLogSingleLine( string.Format("Null option found asked for name {0} in getSettingValueMapSettingOptionChoice_Expensive!", settingName??"null"), Verbosity.ShowAsError );
                return null;
            }

            retry:

            var val = mapConfig.GetCustomInt(option.ID);

            for (int i = 0; i < option.Choices.Count; i++)
            {
                var c = option.Choices[i];
                if (c.RelatedIntValue == val)
                {
                    return c;
                }
            }

            ArcenDebugging.ArcenDebugLogSingleLine( string.Format( "Option {0} seems to have the value of {1} set but there is no choice that matches that, reset to defaults.", option.DisplayName, val), Verbosity.DoNotShow);
            mapConfig.SetCustomInt(option.ID, option.DefaultValue);
            goto retry;
        }

        public static bool getSettingValueBool_Expensive( MapConfiguration mapConfig, string settingName )
        {
            //they are all stored as ints internally
            int intValueForSetting = getSettingValueMapSettingOptionChoice_Expensive( mapConfig, settingName ).RelatedIntValue;
            return intValueForSetting > 0;
        }

        public static void limitLinksForAllPlanets( IList<Planet> planetsForMap, ArcenHostOnlySimContext Context, int maxLinks )
        {
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Capping links for " + planetsForMap.Count + " planets at " + maxLinks + " links", Verbosity.DoNotShow );
            Planet planet;
            for ( int i = 0; i < planetsForMap.Count; i++ )
            {
                planet = planetsForMap[i];
                ThrowawayListCanMemLeak<Planet> neighbors = getNeighborList( planet );
                int numAttempts = 0;
                while ( neighbors.Count > maxLinks && numAttempts < 20 )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Planet " + i + " has " + neighbors.Count + " neighbors. Attempt to remove one", Verbosity.DoNotShow );
                    int neighborIdx = Context.RandomToUse.NextWithInclusiveUpperBound( 0, neighbors.Count - 1 );
                    Planet neighbor = neighbors[neighborIdx];
                    planet.RemoveLinkTo( neighbor );
                    //ThrowawayListCanMemLeak<Planet> connectedPlanets = null;
                    numAttempts++;
                    neighbors = getNeighborList( planet );
                }
            }
        }

        public static void removeSomeLinksBetweenPlanets( int maxToRemove, IList<Planet> planetsForMap, ArcenHostOnlySimContext Context )
        {
            //attempts to remove up to maxToRemove links at random
            int linksToRemove = maxToRemove;
            int linksRemovedSoFar = 0;
            int numAttempts = 0;
            int maxAttempts = 100;
            while ( linksRemovedSoFar < linksToRemove )
            {
                numAttempts++;
                if ( numAttempts > maxAttempts )
                    break;
                Int16 planetIdxForDel = (Int16)Context.RandomToUse.NextWithInclusiveUpperBound( 0, planetsForMap.Count - 1 );
                Planet planetToDelLink = planetsForMap[planetIdxForDel];
                ThrowawayListCanMemLeak<Planet> neighbors = getNeighborList( planetToDelLink );
                if ( neighbors.Count < 2 ) //skip planets with only one neighbor
                    continue;
                int neighborIdx = Context.RandomToUse.NextWithInclusiveUpperBound( 0, neighbors.Count - 1 );
                Planet neighbor = neighbors[neighborIdx];
                planetToDelLink.RemoveLinkTo( neighbor );
                if ( isGalaxyFullyConnected( planetsForMap ) )
                {
                    linksRemovedSoFar++;
                }
                else
                    planetToDelLink.AddLinkTo( neighbor ); //since this unconnected the galaxy, put the link back
            }
        }

        internal static void makeOneDeadEndPlanet( IList<Planet> planetsForMap, ArcenHostOnlySimContext Context )
        {
            //attempts to remove up to maxToRemove links at random
            int linksToRemove = 1;
            int linksRemovedSoFar = 0;
            int numAttempts = 0;
            int maxAttempts = 100;
            while ( linksRemovedSoFar < linksToRemove )
            {
                numAttempts++;
                if ( numAttempts > maxAttempts )
                    break;
                Int16 planetIdxForDel = (Int16)Context.RandomToUse.NextWithInclusiveUpperBound( 0, planetsForMap.Count - 1 );
                Planet planetToDelLink = planetsForMap[planetIdxForDel];
                ThrowawayListCanMemLeak<Planet> neighbors = getNeighborList( planetToDelLink );
                if ( neighbors.Count != 2 ) //we want exactly one link
                    continue;
                int neighborIdx = Context.RandomToUse.NextWithInclusiveUpperBound( 0, neighbors.Count - 1 );
                Planet neighbor = neighbors[neighborIdx];
                planetToDelLink.RemoveLinkTo( neighbor );
                if ( isGalaxyFullyConnected( planetsForMap ) )
                {
                    linksRemovedSoFar++;
                }
                else
                    planetToDelLink.AddLinkTo( neighbor ); //since this unconnected the galaxy, put the link back
            }
        }

        internal static ThrowawayListCanMemLeak<Planet> getNeighborList( Planet planet )
        {
            ThrowawayListCanMemLeak<Planet> neighbors = new ThrowawayListCanMemLeak<Planet>( 500 );
            foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
            {
                if ( neighbor == null )
                    continue;
                neighbors.Add( neighbor );
            }
            return neighbors;
        }

        public static void makeSureGalaxyIsFullyConnected( bool UnDestroyAnyDestroyedPlanetsFirst, Galaxy galaxy )
        {
            makeSureGalaxyIsFullyConnected( UnDestroyAnyDestroyedPlanetsFirst, galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs() );
        }

        //this is similar to isGalaxyFullyConnected
        public static void makeSureGalaxyIsFullyConnected( bool UnDestroyAnyDestroyedPlanetsFirst, IList<Planet> planetsForMap )
        {
            if ( UnDestroyAnyDestroyedPlanetsFirst )
            {
                for ( int i = 0; i < planetsForMap.Count; i++ )
                    planetsForMap[i].HasPlanetBeenDestroyed = false;
            }

            if ( MapgenLogger.IsActive )
            {
                for ( int i = 0; i < planetsForMap.Count; i++ )
                {
                    MapgenLogger.Log( "Planet " + i + ": " + planetsForMap[i].Name + " links: " + planetsForMap[i].LinkedIndices.Count );
                    for ( int j = 0; j < planetsForMap[i].LinkedIndices.Count; j++ )
                        MapgenLogger.Log( "Link to: " + planetsForMap[i].LinkedIndices[j] );
                }
            }

            bool debug = false;
            IList<Planet> connectedPlanets = null;
            int lastConnectedCountStyle1 = -1;
            //int lastConnectedCountStyle2 = -1;
            int lastConnectedCountStyle3 = -1;
            int style3Misses = 0;
            int style1Misses = 0;
            while ( !isGalaxyFullyConnected( planetsForMap, out connectedPlanets ) )
            {
                if ( lastConnectedCountStyle1 == connectedPlanets.Count && style1Misses++ > 100 )
                {
                    //if ( lastConnectedCountStyle2 == connectedPlanets.Count )
                    //{
                        if ( lastConnectedCountStyle3 == connectedPlanets.Count && style3Misses++ > 100 )
                        {
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Unable to make sure galaxy is connected!" );
                            return;
                        }
                        lastConnectedCountStyle3 = connectedPlanets.Count;
                        ConnectPlanetsStyle3( planetsForMap, connectedPlanets, debug );
                        continue;
                    //}
                    //lastConnectedCountStyle2 = connectedPlanets.Count;
                    //ConnectPlanetsStyle2( planetsForMap, connectedPlanets, debug );
                    //continue;
                }
                if ( lastConnectedCountStyle1 != connectedPlanets.Count )
                    style1Misses = 0;
                lastConnectedCountStyle1 = connectedPlanets.Count;
                //lastConnectedCountStyle2 = -1;
                lastConnectedCountStyle3 = -1;
                style3Misses = 0;
                ConnectPlanetsStyle1( planetsForMap, connectedPlanets, debug );
            }
        }

        #region ConnectPlanetsStyle1
        private static void ConnectPlanetsStyle1( IList<Planet> planetsForMap, IList<Planet> connectedPlanets, bool debug )
        {
            //after the inner loop runs, we will link
            //the closest connected and unconnected planets
            int previousMinDistance = -1;
            Planet closestUnconnectedPlanet = null;
            Planet closestConnectedPlanet = null;
            //find the closest planet not in connectedPlanets
            for ( int i = 0; i < planetsForMap.Count; i++ )
            {
                Planet test = planetsForMap[i];
                if ( test.HasPlanetBeenDestroyed )
                    continue;
                if ( connectedPlanets.Contains( test ) )
                    continue;
                //now we have an unconnected planet
                for ( int j = 0; j < connectedPlanets.Count; j++ )
                {
                    Planet connected = connectedPlanets[j];
                    if ( connected == null || connected == test )
                        continue;
                    if ( connected.HasPlanetBeenDestroyed )
                        continue;
                    if ( connected.GetIsDirectlyLinkedTo( false, test ) )
                        continue;
                    if ( closestUnconnectedPlanet == null && closestConnectedPlanet == null )
                    {
                        closestUnconnectedPlanet = test;
                        closestConnectedPlanet = connected;
                        previousMinDistance = Mat.DistanceBetweenPointsImprecise( test.GalaxyLocation, connected.GalaxyLocation );
                        continue;
                    }
                    int distance = Mat.DistanceBetweenPointsImprecise( test.GalaxyLocation, connected.GalaxyLocation );
                    if ( distance < previousMinDistance )
                    {
                        closestConnectedPlanet = connected;
                        closestUnconnectedPlanet = test;
                        previousMinDistance = distance;
                    }
                }
            }
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "Style 1: Adding extra link between " + closestConnectedPlanet.Name + " and " + closestUnconnectedPlanet.Name );
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Style 1: Adding extra link between " + closestConnectedPlanet.Name + " and " + closestUnconnectedPlanet.Name, Verbosity.DoNotShow );
            closestConnectedPlanet.AddLinkTo( closestUnconnectedPlanet );
        }
        #endregion

        //#region ConnectPlanetsStyle2
        //private static void ConnectPlanetsStyle2( IList<Planet> planetsForMap, ThrowawayListCanMemLeak<Planet> connectedPlanets, bool debug )
        //{
        //    //find the first planet not in connectedPlanets
        //    for ( int i = 0; i < planetsForMap.Count; i++ )
        //    {
        //        Planet test = planetsForMap[i];
        //        if ( test.HasBeenDestroyed )
        //            continue;
        //        if ( connectedPlanets.Contains( test ) )
        //            continue;
        //        //now we have an unconnected planet
        //        for ( int j = 0; j < connectedPlanets.Count; j++ )
        //        {
        //            Planet connected = connectedPlanets[j];
        //            if ( connected == null || connected == test )
        //                continue;
        //            if ( connected.HasBeenDestroyed )
        //                continue;
        //            if ( test.GetIsDirectlyLinkedTo( false, connected ) )
        //                continue;

        //            if ( MapgenLogger.IsActive )
        //                MapgenLogger.Log( "Style 2: Adding extra link between " + test.Name + " and " + connected.Name );
        //            if ( debug )
        //                ArcenDebugging.ArcenDebugLogSingleLine( "Style 2: Adding extra link between " + test.Name + " and " + connected.Name, Verbosity.DoNotShow );
        //            test.AddLinkTo( connected );
        //            return;
        //        }
        //    }
        //}
        //#endregion

        #region ConnectPlanetsStyle3
        private static void ConnectPlanetsStyle3( IList<Planet> planetsForMap, IList<Planet> connectedPlanets, bool debug )
        {
            //after the inner loop runs, we will link
            //the closest connected and unconnected planets
            int previousMinDistance = -1;
            Planet closestUnconnected1Planet = null;
            Planet closestUnconnected2Planet = null;
            //find the closest planet not in connectedPlanets
            for ( int i = 0; i < planetsForMap.Count; i++ )
            {
                Planet unconnected1 = planetsForMap[i];
                if ( unconnected1.HasPlanetBeenDestroyed )
                    continue;
                if ( connectedPlanets.Contains( unconnected1 ) )
                    continue;
                //now we have an unconnected planet
                for ( int j = 0; j < planetsForMap.Count; j++ )
                {
                    Planet unconnected2 = planetsForMap[j];
                    if ( unconnected2.HasPlanetBeenDestroyed )
                        continue;
                    if ( connectedPlanets.Contains( unconnected2 ) )
                        continue;
                    if ( unconnected2 == null || unconnected1 == unconnected2 )
                        continue;
                    if ( unconnected1.GetIsDirectlyLinkedTo( false, unconnected2 ) )
                        continue;
                    if ( closestUnconnected1Planet == null && closestUnconnected2Planet == null )
                    {
                        closestUnconnected1Planet = unconnected1;
                        closestUnconnected2Planet = unconnected2;
                        previousMinDistance = Mat.DistanceBetweenPointsImprecise( unconnected1.GalaxyLocation, unconnected2.GalaxyLocation );
                        continue;
                    }
                    int distance = Mat.DistanceBetweenPointsImprecise( unconnected1.GalaxyLocation, unconnected2.GalaxyLocation );
                    if ( distance < previousMinDistance )
                    {
                        closestUnconnected1Planet = unconnected1;
                        closestUnconnected2Planet = unconnected2;
                        previousMinDistance = distance;
                    }
                }
            }
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "Style 3: Adding extra link between " + closestUnconnected1Planet.Name + " and " + closestUnconnected2Planet.Name );
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Style 3: Adding extra link between " + closestUnconnected1Planet.Name + " and " + closestUnconnected2Planet.Name, Verbosity.DoNotShow );
            closestUnconnected1Planet.AddLinkTo( closestUnconnected2Planet );
        }
        #endregion

        internal static bool isGalaxyFullyConnected( IList<Planet> planetsForMap )
        {
            IList<Planet> connectedPlanets = null;
            return isGalaxyFullyConnected( planetsForMap, out connectedPlanets );
        }

        internal static bool isGalaxyFullyConnected( IList<Planet> planetsForMap, out IList<Planet> connectedPlanets )
        {
            //check for map connectivity (which will be done after stripping a few connections out)
            ThrowawayListCanMemLeak<Planet> localConnectedPlanets = new ThrowawayListCanMemLeak<Planet>();
            if ( planetsForMap == null || planetsForMap.Count == 0 )
            {
                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "isGalaxyFullyConnected is empty" );
                connectedPlanets = localConnectedPlanets;
                return true; //well, there's nothing here, so
            }

            int nonDestroyedCount = 0;
            for ( int i = 0; i < planetsForMap.Count; i++ )
            {
                Planet p = planetsForMap[i];
                if ( !p.HasPlanetBeenDestroyed )
                    nonDestroyedCount++;
            }

            Planet firstPlanet = planetsForMap[0];
            if ( firstPlanet.LinkedIndices.Count <= 0 || firstPlanet.HasPlanetBeenDestroyed )
            {
                firstPlanet = null;
                //if the first planet is not linked, find one that is
                for ( int i = 0; i < planetsForMap.Count; i++ )
                {
                    Planet p = planetsForMap[i];
                    if ( p.LinkedIndices.Count > 0 && !p.HasPlanetBeenDestroyed )
                    {
                        firstPlanet = p;
                        break;
                    }
                }
            }
            if ( firstPlanet == null )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "No non-destroyed planets available to link!" );
                connectedPlanets = localConnectedPlanets;
                return true; //well, there's nothing here, so
            }
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "isGalaxyFullyConnected firstPlanet: " + firstPlanet.Name );
            localConnectedPlanets.Add( firstPlanet );

            //if ( MapgenLogger.IsActive )
            //    MapgenLogger.Log( "isGalaxyFullyConnected: " + nonDestroyedCount );

            for ( int i = 0; i < localConnectedPlanets.Count; i++ )
            {
                Planet planetToCheck = localConnectedPlanets[i];
                if ( planetToCheck == null )
                    continue;

                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "isGalaxyFullyConnected planetToCheck: " + planetToCheck.Name + " links: " + planetToCheck.LinkedIndices.Count );

                //if ( MapgenLogger.IsActive )
                //    MapgenLogger.Log( "isGalaxyFullyConnected: localConnectedPlanets Count: " + localConnectedPlanets.Count );

                foreach ( Planet neighbor in planetToCheck.LinkedNeighbors( false ) )
                {
                    if ( neighbor == null )
                        continue;
                    if ( !localConnectedPlanets.Contains( neighbor ) )
                    {
                        if ( MapgenLogger.IsActive )
                            MapgenLogger.Log( "isGalaxyFullyConnected neighbor: " + neighbor.Name + " added" );
                        localConnectedPlanets.Add( neighbor );
                    }
                }
            }
            connectedPlanets = localConnectedPlanets;
            if ( connectedPlanets.Count >= nonDestroyedCount )
            {
                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "galaxy fully connected!" );
                return true;
            }
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "galaxy not fully connected: connected planets: " + connectedPlanets.Count + " out of " + nonDestroyedCount + " (non-destroyed) total." );
            return false;
        }

        //Note this needs to take a method of giving probabilities
        internal static LinkMethod getRandomLinkMethod( int percentSpanningTree, int percentGabriel,
                                                       int percentRNG, int percentSpanningTreeWithConnections,
                                                       ArcenHostOnlySimContext Context )
        {
            if ( percentSpanningTreeWithConnections + percentRNG + percentGabriel + percentSpanningTree != 100 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "BUG! percentages given to getRandomLinkMethod do not add up to 100", Verbosity.DoNotShow );
            }
            LinkMethod val = LinkMethod.None;
            int linkingMethodRand = Context.RandomToUse.NextWithInclusiveUpperBound( 0, 100 );

            if ( linkingMethodRand < percentGabriel )
                val = LinkMethod.Gabriel;
            else if ( linkingMethodRand < percentGabriel + percentRNG )
                val = LinkMethod.RNG;
            else if ( linkingMethodRand < percentGabriel + percentRNG + percentSpanningTree )
                val = LinkMethod.SpanningTree;
            else if ( linkingMethodRand <= percentGabriel + percentRNG + percentSpanningTree + percentSpanningTreeWithConnections )
                val = LinkMethod.SpanningTreeWithConnections;
            else
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Using random linking method number " + linkingMethodRand, Verbosity.DoNotShow );
            }
            return val;
        }

        //adds a circle of points
        public static ThrowawayListCanMemLeak<ArcenPoint> addCircularPoints( int numPoints, ArcenHostOnlySimContext Context, ArcenPoint circleCenter, int circleRadius,
                                                           ThrowawayListCanMemLeak<ArcenPoint> pointsSoFarOrNull )
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
                if ( pointsSoFarOrNull != null )
                    pointsSoFarOrNull.Add( pointOnRing );
            }
            return pointsForThisCircle;
        }
        //adds an ellipse of points
        internal static ThrowawayListCanMemLeak<ArcenPoint> addElipticalPoints( int numPoints, ArcenHostOnlySimContext Context, ArcenPoint ellipseCenter, int ellipseMajorAxis,
                                                            int ellipseMinorAxis, double rotationRad, ThrowawayListCanMemLeak<ArcenPoint> pointsSoFarOrNull )
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
                                                                                                                                                                     //heck if I know why this has to be done, but the ellipse gets twisted without it
                if ( angle >= 90 ) x = -x;
                if ( angle >= 270 ) x = -x;
                double y = x * Math.Sin( angleRad ) / Math.Cos( angleRad );
                double xn = x * Math.Cos( rotationRad ) - y * Math.Sin( rotationRad );
                double yn = x * Math.Sin( rotationRad ) + y * Math.Cos( rotationRad );
                pointOnRing.X += (int)xn;
                pointOnRing.Y += (int)yn;
                pointsForThisCircle.Add( pointOnRing );
                if ( pointsSoFarOrNull != null )
                    pointsSoFarOrNull.Add( pointOnRing );
            }
            return pointsForThisCircle;
        }


        //this version of AddPointsInCircle can provide some other points that must be avoided
        internal static ThrowawayListCanMemLeak<ArcenPoint> addPointsInCircleWithExclusion( int numPoints, ArcenHostOnlySimContext Context, ArcenPoint circleCenter, int circleRadius,
                                                 int minDistanceBetweenPlanets, ThrowawayListCanMemLeak<ArcenPoint> pointsSoFarOrNull, ThrowawayListCanMemLeak<ArcenPoint> pointsToAvoid, int distanceFromAvoidance, int divisibleByX = 0 )
        {
            //keeps track of previously added planets as well
            int numberFailuresAllowed = 1000;
            ThrowawayListCanMemLeak<ArcenPoint> pointsForThisCircle = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            ThrowawayListCanMemLeak<ArcenPoint> pointsToAvoidWithoutThisCenter = new ThrowawayListCanMemLeak<ArcenPoint>( pointsToAvoid.Count - 1 );
            foreach ( ArcenPoint point in pointsToAvoid )
            {
                if ( point != circleCenter )
                    pointsToAvoidWithoutThisCenter.Add( point );
            }

            for ( int i = 0; i < numPoints; i++ )
            {
                ArcenPoint testPoint = circleCenter.GetRandomPointWithinDistance( Context.RandomToUse, 0, circleRadius );
                if ( divisibleByX != 0 )
                {
                    testPoint.X -= testPoint.X % divisibleByX;
                    testPoint.Y -= testPoint.Y % divisibleByX;
                }
                if ( UtilityMethods.HelperDoesPointListContainPointWithinDistance( pointsSoFarOrNull, testPoint, minDistanceBetweenPlanets ) )
                {
                    i--;
                    numberFailuresAllowed--;
                    if ( numberFailuresAllowed <= 0 )
                    {
                        numberFailuresAllowed = 1000;
                        minDistanceBetweenPlanets -= 10;
                    }
                    continue;
                }
                if ( UtilityMethods.HelperDoesPointListContainPointWithinDistance( pointsToAvoidWithoutThisCenter, testPoint, distanceFromAvoidance ) )
                {
                    i--;
                    numberFailuresAllowed--;
                    if ( numberFailuresAllowed <= 0 )
                    {
                        numberFailuresAllowed = 1000;
                        distanceFromAvoidance -= 10;
                    }
                    continue;
                }

                pointsForThisCircle.Add( testPoint );
                if ( pointsSoFarOrNull != null )
                    pointsSoFarOrNull.Add( testPoint );
            }
            return pointsForThisCircle;
        }

        internal static ArcenPoint GetRandomPointWithinRectangle( ArcenPoint topL, ArcenPoint topR,
                                                                              ArcenPoint bottomL, ArcenPoint bottomR,
                                                                              ArcenHostOnlySimContext Context )
        {
            int minLegalX = Math.Min( topL.X, bottomL.X );
            int maxLegalX = Math.Min( topR.X, bottomR.X );
            int minLegalY = Math.Min( bottomL.Y, bottomR.Y );
            int maxLegalY = Math.Min( topL.Y, topR.Y );
            int newX = Context.RandomToUse.Next( minLegalX, maxLegalX );
            int newY = Context.RandomToUse.Next( minLegalY, maxLegalY );
            ArcenPoint newPoint = ArcenPoint.Create( newX, newY );
            return newPoint;
        }


        //adds in a rectangle to roughly cover the screen
        //The X and Y values here were arrived at by crude trial and error
        internal static ThrowawayListCanMemLeak<ArcenPoint> addPointsInStartScreen( int numPoints, ArcenHostOnlySimContext Context, int minDistanceBetweenPlanets,
                                                                 ThrowawayListCanMemLeak<ArcenPoint> pointsSoFarOrNull, bool allowClustersInPoints, bool isGridShape, int divisibleByX = 1,
                                                                                    Dictionary<ArcenPoint, int> AreasToAvoid = null, bool bonusClusters = false )

        {
            int x = 1200;
            int y = 600;
            if ( numPoints > 80 )
            {
                x = 1300;
                y = 800;
            }
            if ( numPoints > 180 )
            {
                x = 1900;
                y = 1400;
            }
            if ( numPoints > 200 )
            {
                x = 2300;
                y = 1900;
            }
            if ( numPoints > 300 )
            {
                x = 3000;
                y = 2000;
            }
            if ( numPoints >= 300 )
            {
                x = 3500;
                y = 2500;
            }
            if ( AreasToAvoid != null )
            {
                x += 500;
                y += 500;
            }
            //ArcenPoint GalaxyMapOnly_GalaxyCenter = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;
            ArcenPoint topL = ArcenPoint.Create( -x, y );
            ArcenPoint topR = ArcenPoint.Create( x, y );
            ArcenPoint bottomL = ArcenPoint.Create( -x, y );
            ArcenPoint bottomR = ArcenPoint.Create( x, -y );
            if ( isGridShape )
            {
                return addGridOfPoints( numPoints, Context, minDistanceBetweenPlanets, pointsSoFarOrNull,
                                        divisibleByX, topL, topR, bottomL, bottomR );
            }
            if ( !allowClustersInPoints )
            {
                return addPointsInRectangle( numPoints, Context, minDistanceBetweenPlanets, pointsSoFarOrNull,
                                             divisibleByX, topL, topR, bottomL, bottomR, AreasToAvoid );
            }
            else
            {
                
                return addPointsInRectangleWithClusters( numPoints, Context, minDistanceBetweenPlanets, bonusClusters, pointsSoFarOrNull,
                                                         divisibleByX, topL, topR, bottomL, bottomR, AreasToAvoid );
            }
        }

        //we add numPlanetsPerChunk planets at a time (so you can have a big spiral of variable thickness). This is used by FatSnake/Cubular
        internal static ThrowawayListCanMemLeak<ArcenPoint> addCenteredGridOfPlanets( int numPoints, ArcenHostOnlySimContext Context, ArcenPoint gridCenter,
                                                                   int numPlanetsPerChunk, int chunkDistance, int horizontalDistance, int verticalDistance, ThrowawayListCanMemLeak<ArcenPoint> pointsSoFarOrNull,
                                                                   ref ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<ArcenPoint>> pointsByChunk, bool angledChunk, int angledY, bool daliStyle )
        {
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "creating centered grid with " + numPoints + " planets", Verbosity.DoNotShow );
            //starting at gridCenter we build a grid of planets
            ThrowawayListCanMemLeak<ArcenPoint> pointsInGrid = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            ThrowawayListCanMemLeak<ArcenPoint> chunksAdded = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            ArcenPoint nextPoint = gridCenter;
            int pointsLeft = numPoints;
            int iter = 0;
            bool clockwise = true;
            if ( Context.RandomToUse.Next( 0, 100 ) < 50 )
                clockwise = false;
            while ( pointsLeft > 0 )
            {
                iter++;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( pointsLeft + " points left; the next point is: " + nextPoint.ToString(), Verbosity.DoNotShow );
                //first, add a chunks worth of planets here
                ThrowawayListCanMemLeak<ArcenPoint> chunkPoints = null;
                if ( pointsByChunk != null )
                    chunkPoints = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );

                int planetsForThisChunk = numPlanetsPerChunk;
                if ( numPlanetsPerChunk == -1 )
                    planetsForThisChunk = Context.RandomToUse.Next( 2, 6 );

                for ( int i = 0; i < planetsForThisChunk; i++ )
                {
                    ArcenPoint tmp = ArcenPoint.Create( nextPoint.X + i * chunkDistance, nextPoint.Y );
                    if ( angledChunk && angledY > 0 )
                    {
                        // if ( iter % 2 == 0 )
                        //     tmp.Y -= i * angledY;
                        // else
                        tmp.Y += i * angledY;
                    }
                    if ( angledChunk && daliStyle )
                    {
                        //a bit more wiggle in both X and Y directions
                        int randomIncrease = Context.RandomToUse.Next( 6, 10 );
                        if ( Context.RandomToUse.Next( 0, 100 ) < 50 )
                            randomIncrease *= -1;
                        tmp.Y += randomIncrease;
                        randomIncrease = Context.RandomToUse.Next( 6, 10 );
                        if ( Context.RandomToUse.Next( 0, 100 ) < 50 )
                            randomIncrease *= -1;

                        tmp.X += randomIncrease;
                    }

                    pointsInGrid.Add( tmp );
                    if ( pointsSoFarOrNull != null )
                        pointsSoFarOrNull.Add( tmp );
                    if ( pointsByChunk != null )
                        chunkPoints.Add( tmp );

                    if ( pointsLeft-- <= 0 )
                        break;
                }
                if ( pointsByChunk != null )
                    pointsByChunk.Add( chunkPoints );
                if ( pointsLeft == 0 )
                    break;
                chunksAdded.Add( nextPoint );
                if ( clockwise )
                {
                    //find the next point (aka the beginning of the next chunk), but we are going clockwise
                    if ( isPointBelow( nextPoint, chunksAdded ) && isPointRight( nextPoint, chunksAdded ) ) //if there's a point below and to the right, we're going up on the left side of the square, so keep going up
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "A", Verbosity.DoNotShow );
                        nextPoint = ArcenPoint.Create( nextPoint.X, nextPoint.Y + verticalDistance );
                    }
                    else if ( isPointBelow( nextPoint, chunksAdded ) ) //there's a point below me and not to my right, so we're on top of the square going right
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "B", Verbosity.DoNotShow );
                        nextPoint = ArcenPoint.Create( nextPoint.X + horizontalDistance, nextPoint.Y );
                    }
                    else if ( isPointLeft( nextPoint, chunksAdded ) ) //there's a point to my left but not below me, so we're on the right side going down
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "C", Verbosity.DoNotShow );
                        nextPoint = ArcenPoint.Create( nextPoint.X, nextPoint.Y - verticalDistance );
                    }
                    else if ( isPointAbove( nextPoint, chunksAdded ) ) //there's a point above me but not to my left, so I'm on the bottom going left
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "D", Verbosity.DoNotShow );
                        nextPoint = ArcenPoint.Create( nextPoint.X - horizontalDistance, nextPoint.Y );
                    }
                    else if ( isPointRight( nextPoint, chunksAdded ) || chunksAdded.Count == 1 ) //This was my first point, so go up
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "E, vertical distance", Verbosity.DoNotShow );
                        nextPoint = ArcenPoint.Create( nextPoint.X, nextPoint.Y + verticalDistance );
                    }
                    else
                    {
                        throw new Exception( "Problem in addCenteredGridOfPlanets; algorithm doesnt know where to put next planet" );
                    }
                }
                else
                {
                    //find the next point (aka the beginning of the next chunk), but we are going counterclockwise
                    if ( chunksAdded.Count == 1 )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "initial point, go up", Verbosity.DoNotShow );
                        nextPoint = ArcenPoint.Create( nextPoint.X, nextPoint.Y + verticalDistance );
                    }
                    else if ( isPointBelow( nextPoint, chunksAdded ) && isPointLeft( nextPoint, chunksAdded ) ) //if there's a point below and to the right, we're going up on the right side of the square
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "A1", Verbosity.DoNotShow );
                        nextPoint = ArcenPoint.Create( nextPoint.X, nextPoint.Y + verticalDistance );
                    }
                    else if ( isPointBelow( nextPoint, chunksAdded ) ) //there's a point below me and not to my right, so we're on top of the square going left
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "B1", Verbosity.DoNotShow );
                        nextPoint = ArcenPoint.Create( nextPoint.X - horizontalDistance, nextPoint.Y );
                    }
                    else if ( isPointRight( nextPoint, chunksAdded ) ) //we are on the left side, heading down
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "E1, vertical distance", Verbosity.DoNotShow );
                        nextPoint = ArcenPoint.Create( nextPoint.X, nextPoint.Y - verticalDistance );
                    }
                    else if ( isPointAbove( nextPoint, chunksAdded ) ) //there's a point above me but not to my right, so I'm on the bottom going right
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "D1", Verbosity.DoNotShow );
                        nextPoint = ArcenPoint.Create( nextPoint.X + horizontalDistance, nextPoint.Y );
                    }
                    else if ( isPointLeft( nextPoint, chunksAdded ) ) //there's a point to my left but not above me, so we're on the right side going up
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "C1", Verbosity.DoNotShow );
                        nextPoint = ArcenPoint.Create( nextPoint.X, nextPoint.Y + verticalDistance );
                    }
                    else
                    {
                        throw new Exception( "Problem in addCenteredGridOfPlanets, counterclockwise; algorithm doesnt know where to put next planet" );
                    }
                }
            }
            if ( debug )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "returning " + pointsInGrid.Count + " planets", Verbosity.DoNotShow );
                for ( int i = 0; i < pointsInGrid.Count; i++ )
                    ArcenDebugging.ArcenDebugLogSingleLine( "\t" + pointsInGrid[i].ToString(), Verbosity.DoNotShow );
            }
            return pointsInGrid;
        }
        //Thise isPointInDirection functions are helper functions for addCenteredGridOfPlanets
        private static bool isPointAbove( ArcenPoint pointForCheck, ThrowawayListCanMemLeak<ArcenPoint> otherPoints )
        {
            for ( int i = 0; i < otherPoints.Count; i++ )
            {
                ArcenPoint otherPoint = otherPoints[i];
                if ( otherPoint.Y > pointForCheck.Y && otherPoint.X == pointForCheck.X )
                {
                    return true;
                }
            }
            return false;
        }
        private static bool isPointBelow( ArcenPoint pointForCheck, ThrowawayListCanMemLeak<ArcenPoint> otherPoints )
        {
            for ( int i = 0; i < otherPoints.Count; i++ )
            {
                ArcenPoint otherPoint = otherPoints[i];
                if ( otherPoint.Y < pointForCheck.Y && otherPoint.X == pointForCheck.X )
                {
                    return true;
                }
            }
            return false;
        }
        private static bool isPointLeft( ArcenPoint pointForCheck, ThrowawayListCanMemLeak<ArcenPoint> otherPoints )
        {
            for ( int i = 0; i < otherPoints.Count; i++ )
            {
                ArcenPoint otherPoint = otherPoints[i];
                if ( otherPoint.X < pointForCheck.X && otherPoint.Y == pointForCheck.Y )
                    return true;
            }
            return false;
        }
        private static bool isPointRight( ArcenPoint pointForCheck, ThrowawayListCanMemLeak<ArcenPoint> otherPoints )
        {
            for ( int i = 0; i < otherPoints.Count; i++ )
            {
                ArcenPoint otherPoint = otherPoints[i];
                if ( otherPoint.X > pointForCheck.X && otherPoint.Y == pointForCheck.Y )
                    return true;
            }
            return false;
        }


        internal static ThrowawayListCanMemLeak<ArcenPoint> addGridOfPoints( int numPoints, ArcenHostOnlySimContext Context, int minDistanceBetweenPlanets,
                                                          ThrowawayListCanMemLeak<ArcenPoint> pointsSoFarOrNull, int divisibleByX, ArcenPoint topL, ArcenPoint topR,
                                                          ArcenPoint bottomL, ArcenPoint bottomR )
        {
            ThrowawayListCanMemLeak<ArcenPoint> PointsInGrid = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            int numRows = 1;
            int numColumns = 1;


            //int distBetweenPointsInRow;
            //int distBetweenPointsInColumn;

            int pointsLeft = numPoints - 1;
            int chanceToIncreaseRows = 50;
            while ( pointsLeft > 0 )
            {
                //Figure out how many rows/columns to use; incorporate some randomness
                bool updateRows = ((numColumns > numRows + 2) && Context.RandomToUse.Next( 0, 100 ) < chanceToIncreaseRows);

                if ( updateRows && pointsLeft > numColumns )
                {
                    numRows++;
                    pointsLeft -= numColumns;
                }
                else if ( !updateRows && pointsLeft > numRows )
                {
                    numColumns++;
                    pointsLeft -= numRows;
                }
                else if ( pointsLeft < numColumns )
                {
                    numRows++;
                    pointsLeft -= numColumns;
                }
            }
            int height = topR.Y - bottomR.Y;
            int width = topR.X - bottomL.X;
            int distanceBetweenRows = height / numRows;
            int distanceBetweenColumns = width / numColumns;
            ArcenPoint startPoint = topL;
            if ( Context.RandomToUse.Next( 0, 100 ) % 2 == 0 )
                startPoint = bottomL;
            pointsLeft = numPoints;
            for ( int i = 0; i < numColumns; i++ )
            {
                for ( int j = 0; j < numRows; j++ )
                {
                    ArcenPoint nextPoint = ArcenPoint.Create( startPoint.X + i * distanceBetweenColumns, startPoint.Y + j * distanceBetweenRows );
                    if ( pointsSoFarOrNull != null )
                        pointsSoFarOrNull.Add( nextPoint );
                    PointsInGrid.Add( nextPoint );
                    pointsLeft--;
                    if ( pointsLeft == 0 )
                        return PointsInGrid;
                }

            }
            return PointsInGrid;
        }

        internal static ThrowawayListCanMemLeak<ArcenPoint> addPointsInRectangle( int numPoints, ArcenHostOnlySimContext Context, int minDistanceBetweenPlanets,
                                                            ThrowawayListCanMemLeak<ArcenPoint> pointsSoFarOrNull, int divisibleByX, ArcenPoint topL, ArcenPoint topR,
                                                               ArcenPoint bottomL, ArcenPoint bottomR, Dictionary<ArcenPoint, int> AreasToAvoid = null )
        {
            //keeps track of previously added planets as well
            int numberFailuresAllowed = 1000;
            ThrowawayListCanMemLeak<ArcenPoint> pointsForThisRectangle = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            int workingMinDist = minDistanceBetweenPlanets;
            for ( int i = 0; i < numPoints; i++ )
            {
                int minDistForThisPoint = workingMinDist;
                // if ( i % 10 == 0 )
                //     minDistForThisPoint *= 3;
                // if ( i % 5 == 0 )
                // {
                //     minDistForThisPoint *= 3;
                //     minDistForThisPoint /= 2;
                // }
                // if ( i % 11 == 0 )
                //     minDistForThisPoint *= 7;


                ArcenPoint testPoint = GetRandomPointWithinRectangle( topL, topR,
                                                                     bottomL, bottomR, Context );
                if ( divisibleByX != 0 )
                {
                    testPoint.X -= testPoint.X % divisibleByX;
                    testPoint.Y -= testPoint.Y % divisibleByX;
                }
                if ( UtilityMethods.HelperDoesPointListContainPointWithinDistance( pointsSoFarOrNull, testPoint, minDistForThisPoint ) ||
                     ShouldPointBeAvoided( testPoint, AreasToAvoid ) )
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
                pointsForThisRectangle.Add( testPoint );
                if ( pointsSoFarOrNull != null )
                    pointsSoFarOrNull.Add( testPoint );
            }
            return pointsForThisRectangle;
        }

        internal static bool ShouldPointBeAvoided( ArcenPoint newPoint, Dictionary<ArcenPoint, int> AreasToAvoid )
        {
            if ( AreasToAvoid == null )
                return false; //we're not doing any omissions, so it's okay
            bool avoidMe = false;
            //            ArcenDebugging.ArcenDebugLogSingleLine("Checking if " + newPoint.ToString() + " should be avoided", Verbosity.DoNotShow );
            foreach ( KeyValuePair<ArcenPoint, int> pair in AreasToAvoid )
            {
                int dist = Mat.DistanceBetweenPointsImprecise( pair.Key, newPoint );
                if ( dist < pair.Value )
                {
                    //                    ArcenDebugging.ArcenDebugLogSingleLine("\tis too close (" + dist + " < " + pair.Value + ") to " + pair.Key.ToString(), Verbosity.DoNotShow );
                    avoidMe = true;
                    break;
                }
            }
            return avoidMe;
        }

        internal static ThrowawayListCanMemLeak<ArcenPoint> addPointsInRectangleWithClusters( int numPoints, ArcenHostOnlySimContext Context, int minDistanceBetweenPlanets, bool bonusClusters, 
                                                                          ThrowawayListCanMemLeak<ArcenPoint> pointsSoFarOrNull, int divisibleByX, ArcenPoint topL, ArcenPoint topR,
                                                                           ArcenPoint bottomL, ArcenPoint bottomR, Dictionary<ArcenPoint, int> AreasToAvoid = null )
        {
            //keeps track of previously added planets as well
            int numberFailuresAllowed = 1000;
            ThrowawayListCanMemLeak<ArcenPoint> pointsForThisRectangle = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            //int pointsToAdd = numPoints;
            //int currPoint = 0;
            int interClusterDist = minDistanceBetweenPlanets * 4;
            bool debug = false;
            int intraClusterDist = minDistanceBetweenPlanets * 2;

            ThrowawayListCanMemLeak<ArcenPoint> clusterCenters = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            int minClusters = 0;
            int maxClusters = 0;
            if ( numPoints > 100 )
                maxClusters = 5;

            else if ( numPoints > 70 )
                maxClusters = 4;
            else if ( numPoints > 40 )
                maxClusters = 3;
            else if ( numPoints < 30 )
                maxClusters = 1;
            else
                maxClusters = 0;
            if ( bonusClusters )
            {
                maxClusters += 3;
                minClusters += 4;
            }
            
            int numClusters = Context.RandomToUse.NextWithInclusiveUpperBound( minClusters, maxClusters );
            int minClusterSize = 5;
            int maxClusterSize = 10;
            if ( bonusClusters )
            {
                minClusterSize = Context.RandomToUse.Next(3, 6);
                minClusterSize = Context.RandomToUse.Next(5, 9);

            }
            //First let's choose our cluster centers
            for ( int i = 0; i < numClusters; i++ )
            {
                ArcenPoint testPoint = GetRandomPointWithinRectangle( topL, topR,
                                                                     bottomL, bottomR, Context );
                if ( numberFailuresAllowed <= 0 )
                {
                    numberFailuresAllowed = 1000;
                    break;
                }
                if ( UtilityMethods.HelperDoesPointListContainPointWithinDistance( clusterCenters, testPoint, interClusterDist ) ||
                     ShouldPointBeAvoided( testPoint, AreasToAvoid ) )
                {
                    numberFailuresAllowed--;
                    continue;
                }
                clusterCenters.Add( testPoint );
            }
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Got " + clusterCenters.Count + " clusters", Verbosity.DoNotShow );
            //now let's allocate points for the clusters:
            for ( int i = 0; i < clusterCenters.Count; i++ )
            {
                int clusterSize = Context.RandomToUse.NextWithInclusiveUpperBound( minClusterSize, maxClusterSize );
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Cluster " + i + " adding " + clusterSize + " points", Verbosity.DoNotShow );
                ThrowawayListCanMemLeak<ArcenPoint> newPoints = addPointsInCircle( clusterSize, Context, clusterCenters[i], intraClusterDist,
                                                                minDistanceBetweenPlanets, pointsForThisRectangle, divisibleByX, AreasToAvoid );
                if ( pointsSoFarOrNull != null )
                    pointsSoFarOrNull.AddRange( newPoints );

            }
            //now let's allocate the rest of the points
            int numP = 0;
            while ( pointsForThisRectangle.Count < numPoints )
            {
                int tempMinDist = minDistanceBetweenPlanets;
                // if ( numP % 10 == 0 )
                //     tempMinDist *= 3;
                // if ( numP % 5 == 0 )
                // {
                //     tempMinDist *= 3;
                //     tempMinDist /= 2;
                // }
                // if ( numP % 3 == 0 )
                //     tempMinDist *= 7;

                ArcenPoint testPoint = GetRandomPointWithinRectangle( topL, topR,
                                                                     bottomL, bottomR, Context );
                if ( divisibleByX != 0 )
                {
                    testPoint.X -= testPoint.X % divisibleByX;
                    testPoint.Y -= testPoint.Y % divisibleByX;
                }

                if ( UtilityMethods.HelperDoesPointListContainPointWithinDistance( pointsSoFarOrNull, testPoint, tempMinDist ) )
                //                UtilityMethods.HelperDoesPointListContainPointWithinDistance(clusterCenters, testPoint, interClusterDist))
                {
                    numberFailuresAllowed--;
                    if ( numberFailuresAllowed <= 0 )
                    {
                        numberFailuresAllowed = 1000;
                        tempMinDist -= 10;
                        minDistanceBetweenPlanets -= 10;
                        if ( tempMinDist < 0 )
                        {
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( " NOT GENERATE A PLANET BAD JUJU GURU #253421 (more than 1000 failures trying to add planets in rectangle with clusters, so maybe not room?)" );
                            return pointsForThisRectangle;
                        }
                    }
                    continue;

                }
                pointsForThisRectangle.Add( testPoint );
                if ( pointsSoFarOrNull != null )
                    pointsSoFarOrNull.Add( testPoint );
                numP++;
            }
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "forRect points " + pointsForThisRectangle.Count + " so far points" + (pointsSoFarOrNull == null ? 0 : pointsSoFarOrNull.Count ), Verbosity.DoNotShow );
            return pointsForThisRectangle;
        }

        //Add points within a circle defined by circleCenter and circleRadius
        //It gives a more ordered appearance when the points are on "more reasonable" numbers, so include the
        //divisible
        internal static ThrowawayListCanMemLeak<ArcenPoint> addPointsInCircle( int numPoints, ArcenHostOnlySimContext Context, ArcenPoint circleCenter, int circleRadius,
                                                            int minDistanceBetweenPlanets, ThrowawayListCanMemLeak<ArcenPoint> pointsSoFarOrNull, int divisibleByX = 0, Dictionary<ArcenPoint, int> AreasToAvoid = null )
        {
            //keeps track of previously added planets as well
            int numberFailuresAllowed = 1000;
            ThrowawayListCanMemLeak<ArcenPoint> pointsForThisCircle = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "generating " + numPoints + " with minDistance " + minDistanceBetweenPlanets + "and radius " + circleRadius + " centered on " + circleCenter.X + ", " + circleCenter.Y, Verbosity.Chat );
            int workingMinDist = minDistanceBetweenPlanets;
            for ( int i = 0; i < numPoints; i++ )
            {
                int minDistForThisPlanet = workingMinDist;
                // if ( i % 10 == 0 )
                //     minDistForThisPlanet *= 3;
                // if ( i % 5 == 0 )
                // {
                //     minDistForThisPlanet *= 3;
                //     minDistForThisPlanet /= 2;
                // }
                // if ( i % 3 == 0 )
                //     minDistForThisPlanet *= 7;

                ArcenPoint testPoint = circleCenter.GetRandomPointWithinDistance( Context.RandomToUse, 0, circleRadius );
                if ( divisibleByX != 0 )
                {
                    if ( debug )
                    {
                        string s = String.Format( "addPointsInCircle: previous {0},{1}",
                                                 testPoint.X, testPoint.Y );
                        ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                    }
                    testPoint.X -= testPoint.X % divisibleByX;
                    testPoint.Y -= testPoint.Y % divisibleByX;
                    if ( debug )
                    {
                        string s = String.Format( "addPointsInCircle: Adjusting planet to {0},{1}",
                                                 testPoint.X, testPoint.Y );
                        ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                    }
                }
                if ( UtilityMethods.HelperDoesPointListContainPointWithinDistance( pointsSoFarOrNull, testPoint, minDistForThisPlanet ) ||
                     ShouldPointBeAvoided( testPoint, AreasToAvoid ) )
                {
                    i--;
                    numberFailuresAllowed--;
                    if ( numberFailuresAllowed <= 0 )
                    {
                        //If we have exceeded the number of allowed failures,
                        //decrease the minDistance and retry
                        numberFailuresAllowed = 1000;
                        workingMinDist -= 10;
                        minDistForThisPlanet -= 10;
                        if ( workingMinDist < 10 )
                        {

                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( " NOT GENERATE A PLANET BAD JUJU GURU #253421 (more than 1000 failures trying to add planets in circle, so maybe not room?) workingMinDist " + workingMinDist + " minDistForThisPlanet " + minDistForThisPlanet + " we placed " + pointsForThisCircle.Count + " planets." );
                            return pointsForThisCircle;
                        }
                    }
                    continue;
                }
                pointsForThisCircle.Add( testPoint );
                if ( pointsSoFarOrNull != null )
                    pointsSoFarOrNull.Add( testPoint );
                if ( debug )
                {
                    string s = String.Format( "addPointsInCircle: Adding planet {0} at location {1},{2}", i,
                                             testPoint.X, testPoint.Y + " so far " + numberFailuresAllowed + " failures." );
                    ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                }
            }
            return pointsForThisCircle;
        }
        public static bool WouldLinkCrossOtherPlanets( Planet First, Planet Second, IList<Planet> planetsForMap, int planetWidth = 40, bool debug = false )
        {
            for ( int i = 0; i < planetsForMap.Count; i++ )
            {
                Planet otherPlanet = planetsForMap[i];
                if ( otherPlanet == First || otherPlanet == Second )
                    continue;
                if ( Mat.LineIntersectsRectangleContainingCircle( First.GalaxyLocation, Second.GalaxyLocation, otherPlanet.GalaxyLocation, planetWidth ) )
                {
                    if ( debug )
                    {
                        ArcenDebugging.LogSingleLine(First.Name + " <-> " + Second.Name + " too close to " + otherPlanet.Name, Verbosity.DoNotShow );
                    }
                    return true;
                }
            }
            return false;
        }
        public static int CountOfCrossedLinksBetween (  Planet firstPlanet, Planet secondPlanet, IList<Planet> planetsForMap)
        {
            //count how many links would be crossed by a link between these two planets
            int firstPlanetIdx = firstPlanet.Index;
            int secondPlanetIdx = secondPlanet.Index;

            ArcenPoint p1 = firstPlanet.GalaxyLocation;
            ArcenPoint p2 = secondPlanet.GalaxyLocation;
            int detectedCrosses = 0;
            for ( int crossIdx1 = 0; crossIdx1 < planetsForMap.Count; crossIdx1++ )
            {
                if ( (crossIdx1 == firstPlanetIdx) || (crossIdx1 == secondPlanetIdx) )
                    continue;
                ArcenPoint crossP1 = planetsForMap[crossIdx1].GalaxyLocation;
                for ( int crossIdx2 = 0; crossIdx2 < planetsForMap.Count; crossIdx2++ )
                {
                    if ( crossIdx1 == crossIdx2 )
                        continue;
                    if ( (crossIdx2 == firstPlanetIdx) || (crossIdx2 == secondPlanetIdx) )
                        continue;
                    if ( !planetsForMap[crossIdx1].GetIsDirectlyLinkedTo( false, planetsForMap[crossIdx2] ) )
                        continue;
                    ArcenPoint crossP2 = planetsForMap[crossIdx2].GalaxyLocation;

                    if ( Mat.LineSegmentIntersectsLineSegment( p1, p2, crossP1, crossP2, 50 ) )
                        detectedCrosses++;
                }
            }
            return detectedCrosses;
        }
        public static bool WouldLinkCrossOtherLinks( Planet firstPlanet, Planet secondPlanet, IList<Planet> planetsForMap, int planetWidth = 40, int maxCrosses = 1 )
        {
            //figure out if the line crossing these two planets would hit another planet
            bool debug = false;
            int firstPlanetIdx = firstPlanet.Index;
            int secondPlanetIdx = secondPlanet.Index;

            ArcenPoint p1 = firstPlanet.GalaxyLocation;
            ArcenPoint p2 = secondPlanet.GalaxyLocation;
            int detectedCrosses = 0;
            for ( int crossIdx1 = 0; crossIdx1 < planetsForMap.Count; crossIdx1++ )
            {
                if ( (crossIdx1 == firstPlanetIdx) || (crossIdx1 == secondPlanetIdx) )
                    continue;
                ArcenPoint crossP1 = planetsForMap[crossIdx1].GalaxyLocation;
                for ( int crossIdx2 = 0; crossIdx2 < planetsForMap.Count; crossIdx2++ )
                {
                    if ( crossIdx1 == crossIdx2 )
                        continue;
                    if ( (crossIdx2 == firstPlanetIdx) || (crossIdx2 == secondPlanetIdx) )
                        continue;
                    if ( !planetsForMap[crossIdx1].GetIsDirectlyLinkedTo( false, planetsForMap[crossIdx2] ) )
                        continue;
                    ArcenPoint crossP2 = planetsForMap[crossIdx2].GalaxyLocation;

                    if ( Mat.LineSegmentIntersectsLineSegment( p1, p2, crossP1, crossP2, planetWidth ) )
                    {
                        detectedCrosses++;
                        if ( detectedCrosses > maxCrosses )
                        {
                            if ( debug )
                            {
                                string s = String.Format( "Planets {0} ({4},{5}) and {1} ({6},{7}) connection overlaps with {2}-{3} ({8},{9})-({10},{11}), and we now have all the allowed crosses" ,
                                                          firstPlanetIdx, secondPlanetIdx, crossIdx1, crossIdx2,
                                                          p1.X, p1.Y, p2.X, p2.Y, crossP1.X, crossP1.Y, crossP2.X, crossP2.Y );
                                ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                            }
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        //the random connections can't be too close; they must be at least
        //minumum hops apart
        //if numRandomConnections == -1 then "use an appropriate number"
        internal static void addRandomConnections( IList<Planet> planetsForMap, int numRandomConnections, ArcenHostOnlySimContext Context, int minimumHops )
        {
            Int16 firstPlanetIdx = 0;
            Int16 secondPlanetIdx = 0;
            ThrowawayListCanMemLeak<int> usedPlanetsForConnections = new ThrowawayListCanMemLeak<int>( 300 );
            int maxRetries = 1000;
            int numRetries = 0;
            bool debug = false;
            if ( numRandomConnections == -1 )
            {
                if ( planetsForMap.Count < 20 )
                    numRandomConnections = 1;
                else if ( planetsForMap.Count < 40 )
                    numRandomConnections = 2;
                else if ( planetsForMap.Count < 50 )
                    numRandomConnections = 3;
                else if ( planetsForMap.Count < 60 )
                    numRandomConnections = 4;
                else if ( planetsForMap.Count < 80 )
                    numRandomConnections = 5;
                else if ( planetsForMap.Count < 100 )
                    numRandomConnections = 6;
                else
                    numRandomConnections = 8;
            }
            if ( debug )
            {
                string s = String.Format( "Adding {0} random connections", numRandomConnections );
                ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
            }
            for ( int i = 0; i < numRandomConnections; i++ )
            {
                int firstPlanetRetries = 0;
                do
                {
                    firstPlanetIdx = (Int16)Context.RandomToUse.Next( 0, planetsForMap.Count );
                    //make sure we don't use this planet twice
                    if ( debug )
                    {
                        string s = String.Format( "Attempt at first planet: {0} ", firstPlanetIdx );
                        ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                    }

                    for ( int j = 0; j < usedPlanetsForConnections.Count; j++ )
                    {
                        if ( firstPlanetIdx == usedPlanetsForConnections[j] )
                            firstPlanetIdx = -1;
                    }
                    firstPlanetRetries++;
                } while ( firstPlanetIdx == -1 && firstPlanetRetries < maxRetries );
                if ( firstPlanetIdx == -1 )
                {
                    numRetries++;
                    continue;
                }
                if ( debug )
                {
                    string s = String.Format( "First random planet: {0}", firstPlanetIdx );
                    ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                }

                //let's get all the planets within X hops of the first planet,
                //to make sure we get an interesting link
                Int16[] neighbors;
                minimumHops++; //increment this now to make the following loop easy
                do
                {
                    minimumHops--; //decrease the hops until we get enough planets to work with
                    neighbors = getNeighbors( firstPlanetIdx, minimumHops, planetsForMap );
                } while ( planetsForMap.Count - neighbors.Length > numRandomConnections - i );

                //use the neighborhood generated above to find a potential planet
                //to link to the first planet
                int maxSecondPlanetRetries = 1000;
                int secondPlanetRetries = 0;
                do
                {
                    secondPlanetIdx = (Int16)Context.RandomToUse.Next( 0, planetsForMap.Count );
                    if ( secondPlanetIdx == firstPlanetIdx )
                        secondPlanetIdx = -1;
                    for ( int j = 0; j < usedPlanetsForConnections.Count; j++ )
                    {
                        if ( secondPlanetIdx == usedPlanetsForConnections[j] )
                            secondPlanetIdx = -1;
                    }
                    if ( isNeighborAlready( neighbors, (Int16)planetsForMap.Count, secondPlanetIdx ) )
                        secondPlanetIdx = -1;
                    secondPlanetRetries++;
                } while ( (secondPlanetIdx == -1) && (secondPlanetRetries < maxSecondPlanetRetries) );
                if ( secondPlanetIdx == -1 )
                {
                    //this can happen if we have a very small number of planets we are working with
                    numRetries++;
                    continue;
                }
                //Two potential planets are selected for random connections (neither has
                //a random connection yet)
                //Let's make sure a link between t hem does not cause any overlap in lines
                Planet firstPlanet = planetsForMap[firstPlanetIdx];
                Planet secondPlanet = planetsForMap[secondPlanetIdx];


                if ( WouldLinkCrossOtherPlanets( firstPlanet, secondPlanet, planetsForMap ) ||
                     WouldLinkCrossOtherLinks( firstPlanet, secondPlanet, planetsForMap ) )
                {
                    i--; //discard this pair, since there's an overlap
                    neighbors = null; //let's not leak memory
                }
                else
                {
                    //Found a valid link, create it
                    firstPlanet.AddLinkTo( secondPlanet );
                    usedPlanetsForConnections.Add( firstPlanetIdx );
                    usedPlanetsForConnections.Add( secondPlanetIdx );
                }
                numRetries++;
                if ( numRetries > maxRetries )
                {
                    numRetries = 0;
                    if ( debug )
                    {
                        string s = String.Format( "Exceeded retry limit with {0} hop minimum; retry", minimumHops );
                        ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                    }
                    if ( minimumHops > 2 )
                    {
                        minimumHops--; //make it easier to find matches
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }
        //this code is for addRandomConnections; we want to not link
        //planets already close to eachother
        internal static Int16[] getNeighbors( Int16 planetIdx, int degreeOfNeighbors, IList<Planet> planetsForMap )
        {
            bool debug = false;
            if ( debug )
            {
                string s = String.Format( "returning list of all planets {0} or fewer hops from {1}", degreeOfNeighbors, planetIdx );
                ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
            }
            Planet testPlanet = planetsForMap[planetIdx];
            Int16[] neighbors = new Int16[planetsForMap.Count];
            int neighborsSoFar = 0;
            for ( int i = 0; i < planetsForMap.Count; i++ )
                neighbors[i] = -1;
            //test for all immediate neighbors
            for ( Int16 i = 0; i < planetsForMap.Count; i++ )
            {
                if ( i == planetIdx )
                    continue;
                Planet potentialNewNeighbor = planetsForMap[i];
                if ( testPlanet.GetIsDirectlyLinkedTo( false, potentialNewNeighbor ) )
                {
                    neighbors[neighborsSoFar] = i;
                    neighborsSoFar++;
                    if ( debug )
                    {
                        string s = String.Format( "{0} --> one hop {1} ({2} neighbors so far)", planetIdx, i, neighborsSoFar );
                        ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                    }
                }
            }
            //now count down remaining degrees
            //note that for a large number of hops and a small number of planets, we might
            //get all the planets before we run out of hops
            while ( degreeOfNeighbors > 1 && neighborsSoFar < planetsForMap.Count )
            {
                int newNeighbors = 0;
                for ( int i = 0; i < neighborsSoFar; i++ )
                {
                    //now we check all the current neighbors to see who their neighbors are,
                    //which will give us the next degree of neighborness

                    Int16 planetIdxForNeighbor = neighbors[i];
                    if ( debug )
                    {
                        string s = String.Format( "Checking for connections to {0}", planetIdxForNeighbor );
                        ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                    }
                    for ( Int16 j = 0; j < planetsForMap.Count - 1; j++ )
                    {
                        //check this planet (a neighbor) for all connections that
                        //are not itself and are also not on the list
                        if ( j == planetIdx )
                            continue;
                        if ( isNeighborAlready( neighbors, (Int16)(neighborsSoFar + newNeighbors), j ) )
                        {
                            if ( debug )
                            {
                                string s = String.Format( " {0} is on the list already, skip it", j );
                                ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                            }
                            continue;
                        }

                        if ( debug )
                        {
                            string s = String.Format( "Checking {0} against {1} (out of {2} total planets). We have {3} neighbors so far", planetIdxForNeighbor, j, planetsForMap.Count, neighborsSoFar );
                            ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                        }

                        Planet currentNeighbor = planetsForMap[planetIdxForNeighbor];
                        Planet potentialNewNeighbor = planetsForMap[j];
                        if ( currentNeighbor.GetIsDirectlyLinkedTo( false, potentialNewNeighbor ) )
                        {
                            if ( debug )
                            {
                                string s = String.Format( "{0} is directly linked to {1} (neighborsSoFar {2} newNeighbors {3}", planetIdxForNeighbor, j, neighborsSoFar, newNeighbors );
                                ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                            }

                            neighbors[neighborsSoFar + newNeighbors] = j;
                            newNeighbors++;
                            if ( debug )
                            {
                                string s = String.Format( "{0} --> {1} hop {2}", planetIdxForNeighbor, degreeOfNeighbors, j );
                                ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                            }
                        }
                    }
                }
                neighborsSoFar += newNeighbors;
                degreeOfNeighbors--;
            }
            return neighbors;
        }

        //checks if element is already in array. I don't always want to look at every
        //element
        internal static bool isNeighborAlready( Int16[] neighborList, Int16 numElemToCheck, int element )
        {
            for ( int i = 0; i < numElemToCheck; i++ )
            {
                if ( neighborList[i] == element )
                    return true;
            }
            return false;
        }

        internal static ThrowawayListCanMemLeak<Planet> convertPointsToPlanets( ThrowawayListCanMemLeak<ArcenPoint> vertices, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            ThrowawayListCanMemLeak<Planet> planetsForMap = new ThrowawayListCanMemLeak<Planet>( 500 );
            for ( int i = 0; i < vertices.Count; i++ )
            {
                Planet planet = galaxy.AddPlanet( PlanetType.Normal, vertices[i],
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                planet.HasPlanetBeenDestroyed = false;
                planet.LinkedIndices.Clear();
                planet.LinkedPathfindables.Clear();
                planetsForMap.Add( planet );
            }
            return planetsForMap;
        }
        /* This returns a matrix where matrix[i][j] == 1 means point i and point j should be connected 
           Has the same algorithm as createMinimumSpanningTree, but it doesn't do the linking. */
        internal static int[,] createMinimumSpanningTreeLinks( ThrowawayListCanMemLeak<ArcenPoint> pointsForGraph )
        {
            int[,] connectionArray;
            connectionArray = new int[pointsForGraph.Count, pointsForGraph.Count];
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
                        ArcenPoint pointNotInTree = pointsForGraph[idxNotInTree];
                        ArcenPoint pointInTree = pointsForGraph[idxInTree];
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
        /* This returns a matrix where matrix[i][j] == 1 means point i and point j should be connected 
           Has the same algorithm as createGabrielGraph, but a seperate implementation */
        internal static int[,] createGabrielGraphLinks( ThrowawayListCanMemLeak<ArcenPoint> pointsForGraph )
        {
            //Algorithm: for each node
            //                          find midpoint to another node
            //                          Check that no other planets are in the circle connecting the two nodes
            //                              If no other planets, link these two planets
            //see htts://en.wikipedia.org/wiki/Gabriel_graph
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
                    int radiusOfCircle = Mat.DistanceBetweenPointsImprecise( pointOne, midPoint );

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
        /* This returns a matrix where matrix[i][j] == 1 means point i and point j should be connected 
           Has the same algorithm as createGabrielGraph, but a seperate implementation */
        internal static int[,] createRNGGraphLinks( ThrowawayListCanMemLeak<ArcenPoint> pointsForGraph )
        {
            //Algorithm: for each pair of nodes i and j
            //           check if any other node k is closer to both i and j than they are to eachother
            //           if no such k exists, link i and j
            int[,] connectionArray;
            connectionArray = new int[pointsForGraph.Count, pointsForGraph.Count];
            for ( int i = 0; i < pointsForGraph.Count; i++ )
            {
                for ( int j = 0; j < pointsForGraph.Count; j++ )
                {
                    connectionArray[i, j] = 0;
                }
            }

            for ( int i = 0; i < pointsForGraph.Count; i++ )
            {
                //the minus one is because the last planet in the last can't compare itself to itself
                // s = System.String.Format("Outer Loop: point {0} of {1}", i, pointsForGraph.Count);
                // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
                for ( int j = 0; j < pointsForGraph.Count; j++ )
                {
                    if ( i == j )
                        continue;
                    // s = System.String.Format("  Middle Loop: Point {0} of {1}", j, pointsForGraph.Count);
                    // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);

                    ArcenPoint pointOne = pointsForGraph[i];
                    ArcenPoint pointTwo = pointsForGraph[j];
                    int distanceBetweenPoints = Mat.DistanceBetweenPointsImprecise( pointOne, pointTwo );
                    bool isThereAnotherPoint = false;
                    for ( int k = 0; k < pointsForGraph.Count; k++ )
                    {
                        if ( (k == i) || (k == j) )
                            continue;

                        ArcenPoint pointForCheck = pointsForGraph[k];
                        int distanceFromOne = Mat.DistanceBetweenPointsImprecise( pointForCheck, pointOne );
                        int distanceFromTwo = Mat.DistanceBetweenPointsImprecise( pointForCheck, pointTwo );
                        if ( (distanceFromOne < distanceBetweenPoints) && (distanceFromTwo < distanceBetweenPoints) )
                        {
                            // s = System.String.Format("    Inner Loop: Compare {0}-->{1}. Point {2} is close enough to prevent a link", i, j, k);
                            // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
                            isThereAnotherPoint = true;
                            k = pointsForGraph.Count; //don't bother checking any other planets
                        }
                    }
                    if ( !isThereAnotherPoint )
                    {
                        // s = System.String.Format("    Inner Loop: Putting link between {0} --> {1}", i, j);
                        // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);

                        //if there were no other planets, link planetOne and planetTwo
                        connectionArray[i, j] = 1;
                        connectionArray[j, i] = 1;

                        // ArcenDebugging.ArcenDebugLogSingleLine("    link added sucessfully", Verbosity.DoNotShow);
                    }
                }
            }
            return connectionArray;
        }
        public static void createMinimumSpanningTree( IList<Planet> planetsForMap )
        {
            ThrowawayListCanMemLeak<Int16> verticesNotInTree = new ThrowawayListCanMemLeak<Int16>( 300 );
            ThrowawayListCanMemLeak<Int16> verticesInTree = new ThrowawayListCanMemLeak<Int16>( 300 );
            // ArcenDebugging.ArcenDebugLogSingleLine("Creating minimum spanning tree now", Verbosity.DoNotShow);
            if ( planetsForMap.Count == 0 )
                throw new Exception( "Asked to create a minimum spanning tree with 0 planets\n" );
            //ArcenDebugging.ArcenDebugLogSingleLine( "Trying to create a minimum spanning tree with " + planetsForMap.Count + " planets", Verbosity.DoNotShow );
            for ( Int16 i = 0; i < planetsForMap.Count; i++ )
                verticesNotInTree.Add( i );
            //Pick first element, then remove it from the list
            Int16 planetIdx = verticesNotInTree[0];
            verticesNotInTree.RemoveAt( 0 );
            verticesInTree.Add( planetIdx );

            //initialize adjacency matrix for Prim's algorithm
            //the adjacency matrix contains entries as follows
            //planetIdxNotInTree <closest planet in tree> <distance to closest planet>
            //In the body of the algorithm we look at this matrix to figure out
            //which planet to add to the tree next, then update it for the next iteration
            int[,] spanningAdjacencyMatrix;
            spanningAdjacencyMatrix = new int[planetsForMap.Count, 3];
            for ( Int16 i = 0; i < planetsForMap.Count; i++ )
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
                        Int16 idxInTree = verticesInTree[j];
                        Planet planetNotInTree = planetsForMap[idxNotInTree];
                        Planet planetInTree = planetsForMap[idxInTree];
                        int distance = Mat.DistanceBetweenPointsImprecise( planetNotInTree.GalaxyLocation, planetInTree.GalaxyLocation );
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
                Int16 closestPlanetIdx = -1;
                Int16 planetToAdd = -1;
                for ( Int16 i = 0; i < verticesNotInTree.Count; i++ )
                {
                    planetIdx = verticesNotInTree[i];
                    // s = System.String.Format( "To find closest edge, examine {0} of {1} (idx {4}), minDistance {2} dist for this planet {3}",
                    //                           i, verticesNotInTree.Count , minDistanceFound, spanningAdjacencyMatrix[planetIdx, 2], planetIdx);
                    // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
                    if ( spanningAdjacencyMatrix[planetIdx, 2] == 0 )
                    {
                        //don't try to link a planet to itself
                        continue;
                    }
                    if ( spanningAdjacencyMatrix[planetIdx, 2] < minDistanceFound )
                    {
                        minDistanceFound = spanningAdjacencyMatrix[planetIdx, 2];
                        closestPlanetIdx = (Int16)spanningAdjacencyMatrix[planetIdx, 1];
                        planetToAdd = planetIdx;
                    }
                }
                // s = System.String.Format( "Adding planet idx {0} closest neighbor ({1}. distance {2} to tree", planetToAdd,
                //                           closestPlanetIdx, minDistanceFound);
                // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
                //Now let's add this planet to the Tree
                verticesNotInTree.Remove( planetToAdd );
                verticesInTree.Add( planetToAdd );
                spanningAdjacencyMatrix[planetToAdd, 2] = 9999;
                planetsForMap[closestPlanetIdx].AddLinkTo( planetsForMap[planetToAdd] );
            }
        }


        public static void createGabrielGraph( IList<Planet> planetsForMap )
        {
            //Algorithm: for each node
            //                          find midpoint to another node
            //                          Check that no other planets are in the circle connecting the two nodes
            //                              If no other planets, link these two planets
            //see htts://en.wikipedia.org/wiki/Gabriel_graph
            //Here i and j iterate over potential pairs. For each potential pair, iterate over k,
            //which is every other planet, to make sure k is not too close to i and j
            for ( int i = 0; i < planetsForMap.Count; i++ )
            {
                // s = System.String.Format("Outer Loop: Planet {0} of {1}", i, planetsForMap.Count);
                // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
                for ( int j = 0; j < planetsForMap.Count; j++ )
                {
                    if ( j == i )
                        continue;
                    Planet planetOne = planetsForMap[i];
                    Planet planetTwo = planetsForMap[j];
                    ArcenPoint pointOne = planetOne.GalaxyLocation;
                    ArcenPoint pointTwo = planetTwo.GalaxyLocation;
                    ArcenPoint midPoint = ArcenPoint.Create(
                                                            (pointOne.X + pointTwo.X) / 2,
                                                            (pointOne.Y + pointTwo.Y) / 2 );
                    int radiusOfCircle = Mat.DistanceBetweenPointsImprecise( pointOne, midPoint );

                    bool isThereAnotherPlanet = false;

                    for ( int k = 0; k < planetsForMap.Count; k++ )
                    {
                        //Now check each other planet to see if they would fall into the circle
                        //centered on the midpoint between i and j
                        if ( (k == i) || (k == j) )
                            continue; //don't compare to yourself
                        int distanceFromMidpoint;
                        Planet planetForCircleCheck = planetsForMap[k];
                        distanceFromMidpoint = Mat.DistanceBetweenPointsImprecise( planetForCircleCheck.GalaxyLocation, midPoint );
                        if ( (distanceFromMidpoint - radiusOfCircle) <= 0 )
                        {
                            //                        s = System.String.Format("Inner Loop: Compare {0}-->{1}. Planet {2} overlaps circle", i, j, k);
                            //                        ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
                            isThereAnotherPlanet = true;
                            k = planetsForMap.Count; //don't bother checking any other planets, since we have one in the circle
                        }

                    }
                    if ( !isThereAnotherPlanet )
                    {
                        // s = System.String.Format("Inner Loop: Putting link between {0} --> {1}", i, j);
                        // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);

                        //if there were no other planets, link planetOne and planetTwo
                        planetOne.AddLinkTo( planetTwo );
                    }
                }
            }
        }

        public static void nearestNeighborGraph( IList<Planet> planetsForMap, ArcenHostOnlySimContext Context, int maxLinksPerPlanet, 
                                                  int percentNearestLink, int percentSecondNearestLink, int percentThirdNearestLink, int linkOverlapsToAllow )
        {
            //this is intended to recapture some more of the old Simple or Realistic maps
            //from AIWC. This will be allowed to have wormhole lines that cross
            /* Algorithm:
               every planet links to its nearest neighbor with chance percentNearestLink
               percentSecondNearestLink chance it links to the second nearest
               percentThirdNearestLink chance it links to the third nearest

               Additionally, if there are any other planets within radiusForExtraConnections of it (up to maxLinksPerPlanet)
               link those too */
            bool veryVerboseDebug = false;
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "stage 1 nearest neighbor" );

            for ( int i = 0; i < planetsForMap.Count; i++ )
            {
                //if( planet.GetIsDirectlyLinkedTo( false, otherPlanet) )
                Planet thisPlanet = planetsForMap[i];
                Planet closestPlanet = null;
                Planet secondClosestPlanet = null;
                Planet thirdClosestPlanet = null;

                for ( int j = i; j < planetsForMap.Count; j++ )
                {
                    if ( veryVerboseDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( i + ", " + j, Verbosity.DoNotShow );
                    if ( j == i )
                        continue;
                    bool updateClosest = false;
                    bool updateSecondClosest = false;
                    bool updateThirdClosest = false;
                    Planet otherPlanet = planetsForMap[j];
                    if ( thisPlanet.GetIsDirectlyLinkedTo( false, otherPlanet ) )
                        continue;
                    int distanceBetweenPlanets = Mat.DistanceBetweenPointsImprecise( thisPlanet.GalaxyLocation, otherPlanet.GalaxyLocation );
                    otherPlanet.Mapgen_WorkingDistance = distanceBetweenPlanets;
                    if ( closestPlanet == null ||
                         otherPlanet.Mapgen_WorkingDistance < closestPlanet.Mapgen_WorkingDistance )
                        updateClosest = true;
                    else if ( secondClosestPlanet == null ||
                              otherPlanet.Mapgen_WorkingDistance < secondClosestPlanet.Mapgen_WorkingDistance )
                        updateSecondClosest = true;
                    else if ( thirdClosestPlanet == null ||
                              otherPlanet.Mapgen_WorkingDistance < thirdClosestPlanet.Mapgen_WorkingDistance )
                        updateThirdClosest = true;

                    if ( updateClosest )
                    {
                        thirdClosestPlanet = secondClosestPlanet;
                        secondClosestPlanet = closestPlanet;
                        closestPlanet = otherPlanet;
                    }
                    else if ( updateSecondClosest )
                    {
                        thirdClosestPlanet = secondClosestPlanet;
                        secondClosestPlanet = otherPlanet;
                    }
                    else if ( updateThirdClosest )
                    {
                        thirdClosestPlanet = otherPlanet;
                    }
                }
                //see if we can connect any of the nearby planets.
                //TODO: overlapsAllowed = Context.RandomToUse.Next(0, 3);
                if ( Context.RandomToUse.NextWithInclusiveUpperBound( 0, 100 ) < percentNearestLink &&
                     closestPlanet != null && closestPlanet.GetLinkedNeighborCount() < maxLinksPerPlanet )
                {
                    if ( !WouldLinkCrossOtherLinks( thisPlanet, closestPlanet, planetsForMap, 40, linkOverlapsToAllow ) ) 
                    {
                        if ( MapgenLogger.IsActive )
                            MapgenLogger.Log( "Linking " + thisPlanet.Index + " (" + thisPlanet.Name + ") to " + closestPlanet.Index + " (" + closestPlanet.Name + ") path A" );
                        thisPlanet.AddLinkTo( closestPlanet );
                    }
                    bool secondAdded = false;
                    if ( Context.RandomToUse.NextWithInclusiveUpperBound( 0, 100 ) < percentSecondNearestLink &&
                         secondClosestPlanet != null && secondClosestPlanet.GetLinkedNeighborCount() < maxLinksPerPlanet &&
                         !WouldLinkCrossOtherPlanets( thisPlanet, secondClosestPlanet, planetsForMap, 40 ) )
                    {
                        if ( !WouldLinkCrossOtherLinks( thisPlanet, secondClosestPlanet, planetsForMap, 40, linkOverlapsToAllow ) )
                        {
                            if ( MapgenLogger.IsActive )
                                MapgenLogger.Log( "Linking " + thisPlanet.Index + " (" + thisPlanet.Name + ") to " + secondClosestPlanet.Index + " (" + secondClosestPlanet.Name + ") path B" );
                            thisPlanet.AddLinkTo( secondClosestPlanet );
                            secondAdded = true;
                        }
                    }
                    if ( secondAdded &&
                         Context.RandomToUse.NextWithInclusiveUpperBound( 0, 100 ) < percentThirdNearestLink &&
                         thirdClosestPlanet != null && thirdClosestPlanet.GetLinkedNeighborCount() < maxLinksPerPlanet &&
                         !WouldLinkCrossOtherPlanets( thisPlanet, thirdClosestPlanet, planetsForMap ) )
                    {
                        if ( !WouldLinkCrossOtherLinks( thisPlanet, thirdClosestPlanet, planetsForMap, 40, linkOverlapsToAllow ) )
                        {
                            if ( MapgenLogger.IsActive )
                                MapgenLogger.Log( "Linking " + thisPlanet.Index + " (" + thisPlanet.Name + ") to " + thirdClosestPlanet.Index + " (" + thirdClosestPlanet.Name + ") path C" );

                            thisPlanet.AddLinkTo( thirdClosestPlanet );
                        }
                    }
                }
            }
            MapgenLogger.WriteRandomStatus( Context.RandomToUse );
            //Now that the three closest planets may or may not be linked, potentially add some extra links
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "stage 2" );
            for ( int i = 0; i < planetsForMap.Count; i++ )
            {
                Planet thisPlanet = planetsForMap[i];
                for ( int j = 0; j < planetsForMap.Count; j++ )
                {
                    if ( i == j )
                        continue;
                    Planet otherPlanet = planetsForMap[j];
                    if ( WouldLinkCrossOtherPlanets( thisPlanet, otherPlanet, planetsForMap ) )
                        continue;
                    if ( thisPlanet.GetLinkedNeighborCount() >= maxLinksPerPlanet || otherPlanet.GetLinkedNeighborCount() >= maxLinksPerPlanet )
                    {
                        // if ( debug )
                        //     ArcenDebugging.ArcenDebugLogSingleLine( "Stage 2: planet " + thisPlanet.Name + " has " + thisPlanet.GetLinkedNeighborCount() + " links." + " " + otherPlanet.Name + " has " + otherPlanet.GetLinkedNeighborCount() + " already, so no more", Verbosity.DoNotShow );
                        break;
                    }

                    if ( thisPlanet.GetIsDirectlyLinkedTo( false, otherPlanet ) )
                        continue;
                    int distanceBetweenPlanets = Mat.DistanceBetweenPointsImprecise( thisPlanet.GalaxyLocation, otherPlanet.GalaxyLocation );
                    //if ( distanceBetweenPlanets < radiusForExtraConnections && !WouldLinkCrossOtherLinks( thisPlanet, otherPlanet, planetsForMap, overlapsAllowed ) )
                    if ( !WouldLinkCrossOtherLinks( thisPlanet, otherPlanet, planetsForMap, 40, linkOverlapsToAllow ) ) //don't allow too many connections, it can feel really messy
                    {
                        if ( MapgenLogger.IsActive )
                            MapgenLogger.Log( "stage 2: add bonus link between " + thisPlanet.Name + " and " + otherPlanet.Name + " at dist " + distanceBetweenPlanets + " max overlaps " + linkOverlapsToAllow + " and current overlaps: " + CountOfCrossedLinksBetween(thisPlanet, otherPlanet, planetsForMap) );
                        thisPlanet.AddLinkTo( otherPlanet );
                    }
                    //else
                    {
                        // if(debug)
                        //     ArcenDebugging.ArcenDebugLogSingleLine("planet " + otherPlanet + " is too far away", Verbosity.DoNotShow );
                    }
                }
            }
            //Now we make sure most planets are connected to at least 2 other planets
            //So if we have planets with only one link, give them another link to another one-link planet
            for ( int i = 0; i < planetsForMap.Count; i++ )
            {
                Planet oneLinkedPlanet = planetsForMap[i];
                if ( oneLinkedPlanet.GetLinkedNeighborCount() > 1 )
                    continue;
                //one planets with one link
                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( oneLinkedPlanet.Name + " has only " + oneLinkedPlanet.GetLinkedNeighborCount() + " connections, so try to add a new one" );
                for ( int j = i; j < planetsForMap.Count; j++ )
                {
                    if ( j == i )
                        continue;
                    Planet otherOneLinkedPlanet = planetsForMap[j];
                    
                    //include planets without any links just for paranoia's sake, though there should not be any
                    if ( otherOneLinkedPlanet.GetLinkedNeighborCount() > 1 )
                        continue;
                    if ( WouldLinkCrossOtherPlanets( oneLinkedPlanet, otherOneLinkedPlanet, planetsForMap ) )
                        continue;
                    if ( MapgenLogger.IsActive )
                        MapgenLogger.Log( "\tAdding link from one linked planet " + oneLinkedPlanet.Name + " to " + otherOneLinkedPlanet.Name );
                    oneLinkedPlanet.AddLinkTo( otherOneLinkedPlanet );
                }
            }
            bool foundLinksToRemove = true;
            while ( foundLinksToRemove )
            {
                foundLinksToRemove = false;
                ThrowawayListCanMemLeak<Planet> otherHeavyPlanets = new ThrowawayListCanMemLeak<Planet>( 300 );
                for ( int i = 0; i < planetsForMap.Count; i++ )
                {
                    Planet heavilyConnectedPlanet = planetsForMap[i];
                    if ( heavilyConnectedPlanet.GetLinkedNeighborCount() <= 4 )
                        continue;
                    otherHeavyPlanets.Clear();
                    for ( int j = 0; j < planetsForMap.Count; j++ )
                    {
                        Planet otherHeavilyConnectedPlanet = planetsForMap[j];
                        if ( otherHeavilyConnectedPlanet.GetIsDirectlyLinkedTo( false, heavilyConnectedPlanet ) &&
                             otherHeavilyConnectedPlanet.GetLinkedNeighborCount() >= 4 )
                        {
                            otherHeavyPlanets.Add(otherHeavilyConnectedPlanet);
                        }
                    }
                    if ( otherHeavyPlanets.Count > 0 )
                    {
                        Planet removePlanet = otherHeavyPlanets[ Context.RandomToUse.Next(0, otherHeavyPlanets.Count) ];
                        if ( MapgenLogger.IsActive )
                            MapgenLogger.Log( "removing heavy link " + heavilyConnectedPlanet.Name + " -> " + removePlanet.Name );
                        removePlanet.RemoveLinkTo( heavilyConnectedPlanet );
                            foundLinksToRemove = true;
                    }
                }
            }
        }


        public static void createRNGGraph( IList<Planet> planetsForMap )
        {
            //Algorithm: for each pair of nodes i and j
            //           check if any other node k is closer to both i and j than they are to eachother
            //           if no such k exists, link i and j
            for ( int i = 0; i < planetsForMap.Count; i++ )
            {
                //the minus one is because the last planet in the last can't compare itself to itself
                // s = System.String.Format("Outer Loop: Planet {0} of {1}", i, planetsForMap.Count);
                // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
                for ( int j = 0; j < planetsForMap.Count; j++ )
                {
                    if ( i == j )
                        continue;
                    // s = System.String.Format("  Middle Loop: Planet {0} of {1}", j, planetsForMap.Count);
                    // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);

                    Planet planetOne = planetsForMap[i];
                    Planet planetTwo = planetsForMap[j];
                    ArcenPoint pointOne = planetOne.GalaxyLocation;
                    ArcenPoint pointTwo = planetTwo.GalaxyLocation;
                    int distanceBetweenPoints = Mat.DistanceBetweenPointsImprecise( pointOne, pointTwo );
                    bool isThereAnotherPlanet = false;
                    for ( int k = 0; k < planetsForMap.Count; k++ )
                    {
                        if ( (k == i) || (k == j) )
                            continue;

                        Planet planetForCheck = planetsForMap[k];
                        int distanceFromOne = Mat.DistanceBetweenPointsImprecise( planetForCheck.GalaxyLocation, pointOne );
                        int distanceFromTwo = Mat.DistanceBetweenPointsImprecise( planetForCheck.GalaxyLocation, pointTwo );
                        if ( (distanceFromOne < distanceBetweenPoints) && (distanceFromTwo < distanceBetweenPoints) )
                        {
                            // s = System.String.Format("    Inner Loop: Compare {0}-->{1}. Planet {2} is close enough to prevent a link", i, j, k);
                            // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
                            isThereAnotherPlanet = true;
                            k = planetsForMap.Count; //don't bother checking any other planets
                        }
                    }
                    if ( !isThereAnotherPlanet )
                    {
                        // s = System.String.Format("    Inner Loop: Putting link between {0} --> {1}", i, j);
                        // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);

                        //if there were no other planets, link planetOne and planetTwo
                        planetOne.AddLinkTo( planetTwo );
                        // ArcenDebugging.ArcenDebugLogSingleLine("    link added sucessfully", Verbosity.DoNotShow);
                    }
                }
            }
            //        ArcenDebugging.ArcenDebugLogSingleLine("Finished adding links", Verbosity.DoNotShow);
        }

        //This function will return an array of integers to make it easy
        //to divvy up a set of planets into regions
        //onlyOneOfLowestMin exists because sometimes you only want 1 of the
        //smallest group; if this is "true" then it will make sure we only
        //have at most one group of minPlanetsPerGroup
        internal static ThrowawayListCanMemLeak<int> allocatePlanetsIntoGroups( int minPlanetsPerGroup, int maxPlanetsPerGroup, int planetsToAllocate, bool onlyOneOfLowestMin, ArcenHostOnlySimContext Context )
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
                    if ( planetsForThisGroup == minPlanetsPerGroup && onlyOneOfLowestMin )
                        minPlanetsPerGroup++;
                }
                planetsLeftToAllocate -= planetsPerGroup[i];

                // s = System.String.Format( "Iter {0}: {1} planets left", i, planetsLeftToAllocate);
                // ArcenDebugging.ArcenDebugLogSingleLine(s, Verbosity.DoNotShow);
            }
            return planetsPerGroup;
        }

        //links currentPlanets and newPlanets
        //We will eventually have a couple styles of links (link nearest planets, link
        //nearest planet in one two nearest 2 in the other, link two different but nearby planets,
        //etc, but right now just link the two closest)
        internal static void linkPlanetLists( Galaxy galaxy, IList<Planet> currentPlanets, IList<Planet> newPlanets, ArcenPoint centerOfNewPlanets, bool combinePlanets = true, int linksToMake = 1, bool someRandomness = false, ArcenHostOnlySimContext Context = null, bool preferMoreHopsFromHomeworlds = false, IList<Planet> planetsToAvoidIfPossible = null )
        {
            int debugStage = 0;
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "linkPlanetLists: Trying to link planets lists, attempting to make " + linksToMake + " connection", Verbosity.DoNotShow );

            Dictionary<Planet, Planet> PotentialConnections = Planet.GetTemporaryPlanetDictOfPlanets( "MapgenBadgerUtilityMethods-linkPlanetLists-PotentialConnections", 10f );
            if ( PotentialConnections == null ) //blocked for teardown/shutdown; bail
                return;
            try
            {
                int requiredPlayerHops = 8;
                int requiredAIHops = 8;
                int retries = 100;
                do
                {
                    PotentialConnections.Clear();
                    bool avoidReusingPlanets = true;
                    if ( retries < 10 )
                        avoidReusingPlanets = false; //give this a good try, but then expand the search
                    for ( int i = 0; i < currentPlanets.Count; i++ )
                    {
                        debugStage = 100;
                        Planet currPlanet = currentPlanets[i];
                        Planet closestPlanet = null;
                        int closestDist = -1;
                        if ( preferMoreHopsFromHomeworlds )
                        {
                            if ( currPlanet.OriginalHopsToHumanHomeworld <= requiredPlayerHops )
                            {
                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( currPlanet.Name + " is " + currPlanet.OriginalHopsToHumanHomeworld + " hops from player homeworld, requirement is " + requiredPlayerHops, Verbosity.DoNotShow );
                                continue;
                            }
                            if ( currPlanet.OriginalHopsToAIHomeworld <= requiredAIHops )
                            {
                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( currPlanet.Name + " is " + currPlanet.OriginalHopsToAIHomeworld + " hops from AI homeworld, requirement is " + requiredAIHops, Verbosity.DoNotShow );
                                continue;
                            }
                            if ( currPlanet.GetControllingFactionType() == FactionType.Player && requiredPlayerHops > 4 )
                            {
                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( currPlanet.Name + " is a player planet ", Verbosity.DoNotShow );
                                continue; //not too close to players untless we feel like we have to
                            }
                            if ( currPlanet.GetControllingFaction().SpecialFactionData.InternalName == "ZenithArchitrave" )
                            {
                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( currPlanet.Name + " is a ZA planet ", Verbosity.DoNotShow );
                                continue; //or ZAs
                            }
                        }
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
                        if ( preferMoreHopsFromHomeworlds && debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "\t" + currPlanet.Name + " is eligible, hops " + requiredPlayerHops, Verbosity.DoNotShow );
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "\t" + currPlanet.Name + " is eligible, hops " + requiredPlayerHops + " preferMoreHopsFromHomeworlds " + preferMoreHopsFromHomeworlds, Verbosity.DoNotShow );
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
                                if ( WouldLinkCrossOtherPlanets( currPlanet, newPlanet, galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs(), 30 ) )
                                {
                                    if ( debug )
                                        ArcenDebugging.ArcenDebugLogSingleLine("Can't link between " + currPlanet.Name + " and " + newPlanet.Name + " due to crossing other planets", Verbosity.DoNotShow );
                                    continue; //nothing that crosses other planets
                                }
                                closestPlanet = newPlanet;
                                closestDist = distance;
                            }
                        }
                        debugStage = 300;
                        if ( closestPlanet != null && currPlanet != null )
                            PotentialConnections[currPlanet] = closestPlanet;
                    }
                    requiredPlayerHops--;
                    requiredAIHops--;
                } while ( PotentialConnections.Count < linksToMake && retries-- > 0 );
                if ( PotentialConnections.Count == 0 )
                {
                    Planet.ReleaseTemporaryPlanetDictOfPlanets( PotentialConnections );
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\tno potential connections found", Verbosity.DoNotShow );
                    return;
                }
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Found " + PotentialConnections.Count + " potential connections, and using hops " + requiredPlayerHops + " between lists with " + currentPlanets[0].Name + " and " + newPlanets[0].Name, Verbosity.DoNotShow );
                debugStage = 400;
                int preferredHomeworldHops = 7; //if you are closer than this many hops to a homeworld, we deprioritize this planet (if requested)
                FInt dropoff = FInt.FromParts( 1, 500 ); //higher scores are less important planets
                ThrowawayListCanMemLeak<KeyValuePair<Planet, Planet>> sortedListOfPotentials = PotentialConnections.SortIntoThrowawayListCanMemLeak( delegate ( KeyValuePair<Planet, Planet> L, KeyValuePair<Planet, Planet> R )
                {
                    int lDist = Mat.DistanceBetweenPointsImprecise( L.Key.GalaxyLocation, L.Value.GalaxyLocation );
                    int rDist = Mat.DistanceBetweenPointsImprecise( R.Key.GalaxyLocation, R.Value.GalaxyLocation );
                    if ( preferMoreHopsFromHomeworlds )
                    {
                        //note we sneakily slightly prefer to be  further from human homeworlds
                        int lHops = Math.Max( L.Key.OriginalHopsToHumanHomeworld + 1, L.Key.OriginalHopsToAIHomeworld ); //average the hops to both
                        int rHops = Math.Max( R.Key.OriginalHopsToHumanHomeworld + 1, R.Key.OriginalHopsToAIHomeworld ); //average the hops to both
                        for ( int i = 0; i < preferredHomeworldHops - lHops; i++ )
                            lDist = (lDist * dropoff).IntValue;
                        for ( int i = 0; i < preferredHomeworldHops - rHops; i++ )
                            rDist = (rDist * dropoff).IntValue;
                    }
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
                    ArcenDebugging.ArcenDebugLogSingleLine( "Potential linkes are ", Verbosity.DoNotShow );
                    for ( int i = 0; i < sortedListOfPotentials.Count; i++ )
                    {
                        KeyValuePair<Planet, Planet> pair = sortedListOfPotentials[i];
                        ArcenDebugging.ArcenDebugLogSingleLine( "\t " + pair.Key.Name + " -> " + pair.Value.Name, Verbosity.DoNotShow );
                    }
                }

                Dictionary<Planet, Planet> workingUsedPairs = Planet.GetTemporaryPlanetDictOfPlanets( "MapgenBadgerUtilityMethods-linkPlanetLists-workingUsedPairs", 10f );
                if ( workingUsedPairs == null ) //blocked for teardown/shutdown; bail
                    return;

                bool avoidDoublesToSamePlanet = true;
                retries = 10;
                do
                {
                    //we made an effort to avoid double links above, lets just do it again too here
                    debugStage = 600;

                    for ( int i = sortedListOfPotentials.Count - 1; i >= 0; i-- )
                    {
                        debugStage = 700;
                        KeyValuePair<Planet, Planet> pair = sortedListOfPotentials[i];
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Evaluating " + pair.Key.Name + " -> " + pair.Value.Name + ", avoid doubles? " + avoidDoublesToSamePlanet + " currently used pairs: " + workingUsedPairs.Count, Verbosity.DoNotShow );
                        debugStage = 710;
                        bool skipThis = false;
                        if ( avoidDoublesToSamePlanet )
                        {
                            debugStage = 720;
                            foreach ( KeyValuePair<Planet, Planet> usedPair in workingUsedPairs )
                            {
                                debugStage = 730;
                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "Checking for overlap against " + usedPair.Key.Name + " -> " + usedPair.Value.Name, Verbosity.DoNotShow );
                                debugStage = 740;
                                if ( pair.Key == usedPair.Key || pair.Value == usedPair.Value ||
                                     pair.Key == usedPair.Value || pair.Value == usedPair.Key )
                                {
                                    skipThis = true;
                                    break;
                                }
                            }
                            if ( planetsToAvoidIfPossible != null && planetsToAvoidIfPossible.Count > 0 )
                            {
                                for ( int k = 0; k < planetsToAvoidIfPossible.Count; k++ )
                                {
                                    if ( debug )
                                        ArcenDebugging.ArcenDebugLogSingleLine( "Checking for overlap against already used planet " + planetsToAvoidIfPossible[k].Name, Verbosity.DoNotShow );

                                    if ( planetsToAvoidIfPossible[k] == pair.Key ||
                                         planetsToAvoidIfPossible[k] == pair.Value )
                                    {
                                        skipThis = true;
                                        break;
                                    }
                                }
                            }

                        }
                        if ( skipThis )
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Skipping " + pair.Key.Name + " --> " + pair.Value.Name + " reuse", Verbosity.DoNotShow );
                            continue;
                        }
                        if ( Context == null || (Context != null && Context.RandomToUse.Next( 0, 100 ) < 70) ||//some randomness if desired
                             (i > linksToMake) )  //make sure not to skip possible links if we don't have any to spare
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Linking " + pair.Key.Name + " --> " + pair.Value.Name, Verbosity.DoNotShow );

                            debugStage = 800;
                            pair.Key.AddLinkTo( pair.Value );
                            workingUsedPairs[pair.Key] = pair.Value;
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "\tused pairs: " + workingUsedPairs.Count, Verbosity.DoNotShow );
                            linksToMake--;
                            if ( planetsToAvoidIfPossible != null )
                                planetsToAvoidIfPossible.Add( pair.Key );
                        }
                        if ( linksToMake <= 0 )
                            break;
                        debugStage = 850;
                    }
                    if ( retries < 5 )
                        avoidDoublesToSamePlanet = false; //we've given it a few tries, lets be more inclusive now
                } while ( linksToMake > 0 && retries-- > 0 );
                debugStage = 900;

                Planet.ReleaseTemporaryPlanetDictOfPlanets( workingUsedPairs );
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "linkPlanetLists error at debugStage " + debugStage + " \n" + e );
            }

            //        closestToNewPlanets.AddLinkTo(newPlanets[secondClosestNewIdx]);
            //secondClosestToNewPlanets.AddLinkTo(newPlanets[secondClosestNewIdx]);
            if ( combinePlanets )
            {
                for ( int i = 0; i < newPlanets.Count; i++ )
                {
                    currentPlanets.Add( newPlanets[i] );
                }
            }
        }
        internal static void EnforcePlanetMinimumSpacing( ThrowawayListCanMemLeak<Planet> planets, int minDist )
        {
            for ( int pass = 0; pass < 200; pass++ )
            {
                bool anyViolation = false;
                for ( int i = 0; i < planets.Count; i++ )
                {
                    for ( int j = i + 1; j < planets.Count; j++ )
                    {
                        int dx = planets[j].GalaxyLocation.X - planets[i].GalaxyLocation.X;
                        int dy = planets[j].GalaxyLocation.Y - planets[i].GalaxyLocation.Y;
                        double distSq = (double)dx * dx + (double)dy * dy;
                        if ( distSq < (double)minDist * minDist )
                        {
                            anyViolation = true;
                            double dist = Math.Sqrt( distSq );
                            if ( dist < 0.5 ) { dx = 1; dy = 0; dist = 1.0; }
                            double push = (minDist - dist) / 2.0 + 1.0;
                            int px = (int)Math.Round( (dx / dist) * push );
                            int py = (int)Math.Round( (dy / dist) * push );
                            planets[i].GalaxyLocation = ArcenPoint.Create(
                                planets[i].GalaxyLocation.X - px, planets[i].GalaxyLocation.Y - py );
                            planets[j].GalaxyLocation = ArcenPoint.Create(
                                planets[j].GalaxyLocation.X + px, planets[j].GalaxyLocation.Y + py );
                        }
                    }
                }
                if ( !anyViolation )
                    break;
            }
        }

        //returns the index of the smallest distance that's larger than smallestDistanceSoFar
        //ie if our distances are 4, 5, 6, 7 and smallestDistanceSoFar == 5
        //then it would return 6
        internal static int findNextValueInList( int[] distanceFromCenter, int smallestDistanceSoFar, int numRegions )
        {
            int idx = -1;
            int bestFit = 9999;
            for ( int i = 0; i < numRegions; i++ )
            {
                //note must be >= smallestDistanceSoFar in case two regions are equidistant
                //this is allowable because we delete the entry for each match after it is made
                if ( i == 0 || (distanceFromCenter[i] >= smallestDistanceSoFar && distanceFromCenter[i] < bestFit) )
                {
                    idx = i;
                    bestFit = distanceFromCenter[i];
                }
            }
            return idx;
        }
    }


    //Graph generators work as follows:
    //Select a random set of vertices (planets)
    //Then for use a chosen method to link the planets
    public class Mapgen_Graph : IMapGenerator
    {
        public Mapgen_Graph()
        {

        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        private ThrowawayListCanMemLeak<ArcenPoint> vertices;

        private bool gabriel;
        private bool spanning;
        private bool rng;
        private bool nearestNeighbor;
        private readonly bool debug = false;
        private int radiusForPlanetPlacement;
        //private int numPlanetsDesired;
        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_Graph : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            MapgenLogger.WriteRandomStatus( Context.RandomToUse );

            bool spanningRandomConnections = true;
            this.gabriel = false;
            this.spanning = false;
            this.rng = false;
            this.nearestNeighbor = false;
            int numberToSeed = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            //string mapName = mapType.InternalName;
            //numberToSeed =  BadgerUtilityMethods.getSettingValueInt("NumPlanets");
            //if(numberToSeed == 0)
            //  numberToSeed = 80;
            //  this.numPlanetsDesired = numberToSeed;
            int planetLayoutVal = -1;
            int realisticLinkingMethodNum = -1;
            //int maxLinks = 4;
            if ( ArcenStrings.Equals( mapType.InternalName, "Simple" ) )
            {
                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "Variant: Simple" );

                int simpleLinkingMethodNum = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "SimpleLinkMethod" ).RelatedIntValue;
                if ( this.debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Value from simpleLinkMethod was " + simpleLinkingMethodNum, Verbosity.DoNotShow );
                simpleLinkingMethodNum++;
                if ( simpleLinkingMethodNum == 1 )
                {
                    if ( this.debug )
                    {
                        string s = String.Format( "Using a random neighborhood graph for Simple,  {0} planets", numberToSeed );
                        ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                    }
                    this.rng = true;
                }

                if ( simpleLinkingMethodNum == 2 )
                {
                    if ( this.debug )
                    {
                        string s = String.Format( "Using generating a gabriel graph for Dreamcatcher,  {0} planets", numberToSeed );
                        ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                    }
                    this.gabriel = true;
                }
                if ( simpleLinkingMethodNum == 3 )
                {
                    if ( this.debug )
                    {

                        string s = String.Format( "Using a spanning tree for Constellation,  {0} planets", numberToSeed );
                        ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                    }
                    this.spanning = true;
                }
                planetLayoutVal = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "SimplePointDistribution" ).RelatedIntValue;
            }
            else if ( ArcenStrings.Equals( mapType.InternalName, "Realistic" ) )
            {
                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "Variant: Realistic" );

                realisticLinkingMethodNum = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "RealisticLinkMethod" ).RelatedIntValue;
                realisticLinkingMethodNum++;
                this.nearestNeighbor = true;
                planetLayoutVal = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "RealisticPointDistribution" ).RelatedIntValue;
            }
            else
            {
                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "Variant: Missing!!" );
            }
            this.vertices = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );

            //Set up points. Do points only first
            //because this way if I want to use a later algorithm
            //that might delete certain entries, I don't have to try
            //to remove a planet from the Galaxy because I'm not sure
            //how to do that.
            if ( this.debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "using planet layout " + planetLayoutVal, Verbosity.DoNotShow );
            this.setUpPoints( numberToSeed, Context, planetLayoutVal );
            //Now we have matching lists of points and planets
            ThrowawayListCanMemLeak<Planet> planetsForMap = BadgerUtilityMethods.convertPointsToPlanets( this.vertices, galaxy, Context );

            if ( this.gabriel )
            {
                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "Method: gabriel" );

                BadgerUtilityMethods.createGabrielGraph( planetsForMap );

                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "planetsForMap: " + planetsForMap.Count );

                //remove a few links at random
                BadgerUtilityMethods.removeSomeLinksBetweenPlanets( 10, planetsForMap, Context );

                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "planetsForMap After removing some: " + planetsForMap.Count );
            }
            if ( this.rng )
            {
                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "Method: rng" );

                BadgerUtilityMethods.createRNGGraph( planetsForMap );

                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "planetsForMap: " + planetsForMap.Count );

                //remove a few links at random
                BadgerUtilityMethods.removeSomeLinksBetweenPlanets( 2, planetsForMap, Context );

                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "planetsForMap After removing some: " + planetsForMap.Count );
            }
            if ( this.nearestNeighbor )
            {
                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "Method: nearestNeighbor" );

                MapgenLogger.WriteRandomStatus( Context.RandomToUse );

                //this is "Original"
                if ( this.debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Linking nearest neighbnord with method " + realisticLinkingMethodNum, Verbosity.DoNotShow );
                int maxLinksPerPlanet = 5;
                int percentNearestLink = 100;
                int percentSecondNearestLink = 50;
                int percentThirdNearestLink = 30;
                int overlappingLinkConnectionsAllowed = 0;
                if ( realisticLinkingMethodNum == 2 )
                {
                    //Cyclone
                    maxLinksPerPlanet = 8;
                    overlappingLinkConnectionsAllowed = 4;
                }
                if ( realisticLinkingMethodNum == 3 )
                {
                    //Amnesia
                    maxLinksPerPlanet = 5;
                    percentSecondNearestLink = 10;
                    percentThirdNearestLink = 10;
                    overlappingLinkConnectionsAllowed = 1;
                }
                BadgerUtilityMethods.nearestNeighborGraph( planetsForMap, Context, maxLinksPerPlanet, 
                                                          percentNearestLink, percentSecondNearestLink, percentThirdNearestLink, overlappingLinkConnectionsAllowed );

                MapgenLogger.WriteRandomStatus( Context.RandomToUse );
                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "planetsForMap: " + planetsForMap.Count );

                BadgerUtilityMethods.limitLinksForAllPlanets( planetsForMap, Context, 4 );
                //ThrowawayListCanMemLeak<Planet> connectedPlanets = null;

                MapgenLogger.WriteRandomStatus( Context.RandomToUse );
                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "planetsForMap After removing some: " + planetsForMap.Count );
            }
            if ( this.spanning )
            {
                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "Method: spanning" );

                //create a minimal spanning tree using Prim's algorithm https://en.wikipedia.org/wiki/Prim%27s_algorithm
                BadgerUtilityMethods.createMinimumSpanningTree( planetsForMap );

                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "planetsForMap: " + planetsForMap.Count );

                if ( spanningRandomConnections )
                {
                    int numRandomConnections;
                    if ( numberToSeed < 40 )
                        numRandomConnections = 2;
                    else if ( numberToSeed < 50 )
                        numRandomConnections = 3;
                    else if ( numberToSeed < 60 )
                        numRandomConnections = 4;
                    else if ( numberToSeed < 80 )
                        numRandomConnections = 5;
                    else if ( numberToSeed < 100 )
                        numRandomConnections = 6;
                    else
                        numRandomConnections = 7;
                    int minimumHops = 3;
                    if ( numberToSeed < 40 )
                        minimumHops = 2;
                    BadgerUtilityMethods.addRandomConnections( planetsForMap, numRandomConnections, Context, minimumHops );
                }
            }

            MapgenLogger.WriteRandomStatus( Context.RandomToUse );

            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "planetsForMap Before limitLinksForAllPlanets: " + planetsForMap.Count );

            BadgerUtilityMethods.limitLinksForAllPlanets( planetsForMap, Context, 4 );
            if ( this.debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Making sure planets are fully connected A", Verbosity.DoNotShow );

            MapgenLogger.WriteRandomStatus( Context.RandomToUse );

            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, planetsForMap );

        }

        //this function is a great place for tuning the eventual map
        //There's a lot of worthwhile experimentation to do here
        public void setUpPoints( int numberToSeed, ArcenHostOnlySimContext Context, int placementType )
        {
            bool circlePlacement = false;
            bool oneBigCircle = false;
            if ( placementType == 0 )
            {
                //distribute the points in a rectangle
                circlePlacement = false;
                oneBigCircle = false;
            }
            else if ( placementType == 1 )
            {
                circlePlacement = true;
            }
            else
            {
                circlePlacement = true;
                oneBigCircle = true;
            }
            //My first pass for this just steals the code from intraClusterPlanetPoints
            if ( this.debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Generating poitns in setUpPoints. CirclePlacement " + circlePlacement + " one big circle " + oneBigCircle, Verbosity.DoNotShow );
            int minimumDistanceBetweenPlanets = 140;
            //        bool circlePlacement = true; //we can distribute in either a circle or a rectangle
            if ( circlePlacement )
            {
                if ( numberToSeed < 40 )
                    this.radiusForPlanetPlacement = 500;
                else if ( numberToSeed <= 60 )
                    this.radiusForPlanetPlacement = 550;
                else if ( numberToSeed <= 80 )
                    this.radiusForPlanetPlacement = 650;
                else if ( numberToSeed <= 100 )
                    this.radiusForPlanetPlacement = 700;
                else if ( numberToSeed <= 200 )
                    this.radiusForPlanetPlacement = 850;
                else if ( numberToSeed <= 300 )
                    this.radiusForPlanetPlacement = 950;
                else if ( numberToSeed <= 400 )
                    this.radiusForPlanetPlacement = 1050;
                else
                    this.radiusForPlanetPlacement = 1150;

                if ( this.gabriel || this.rng )
                {
                    //It looks better for these maps if things are more spread out
                    this.radiusForPlanetPlacement += 100;
                    this.radiusForPlanetPlacement += numberToSeed / 90 * 20;
                }
                //Here is my first pass (oneBigCircle) and a second attempt
                //where I spread things out a bit more
                if ( oneBigCircle )
                {
                    if ( this.debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "generating points in a big circle", Verbosity.DoNotShow );

                    this.radiusForPlanetPlacement += 2000;
                    ArcenPoint Center = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;
                    BadgerUtilityMethods.addPointsInCircle( numberToSeed, Context, Center, this.radiusForPlanetPlacement,
                                                           minimumDistanceBetweenPlanets, this.vertices );
                }
                else
                {
                    if ( this.debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "points generated from multiple circles", Verbosity.DoNotShow );
                    int xStart = 900;
                    int yStart = 750;
                    xStart += (numberToSeed / 90) * 200;
                    yStart += (numberToSeed / 90) * 180;
                    ThrowawayListCanMemLeak<ArcenPoint> centersForCircles = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
                    centersForCircles.Add( ArcenPoint.Create( xStart, yStart ) );
                    centersForCircles.Add( ArcenPoint.Create( xStart, -yStart ) );
                    centersForCircles.Add( ArcenPoint.Create( 0, 0 ) );
                    centersForCircles.Add( ArcenPoint.Create( -xStart, yStart ) );
                    centersForCircles.Add( ArcenPoint.Create( -xStart, -yStart ) );

                    centersForCircles.Add( ArcenPoint.Create( xStart, 0 ) );
                    centersForCircles.Add( ArcenPoint.Create( -xStart, 0 ) );
                    centersForCircles.Add( ArcenPoint.Create( 0, yStart ) );
                    centersForCircles.Add( ArcenPoint.Create( 0, -yStart ) );


                    //int seedsPerCircle = numberToSeed /centersForCircles.Count;
                    //int extraPoints = numberToSeed % centersForCircles.Count;
                    for ( int i = 0; i < centersForCircles.Count; i++ )
                    {
                        int seedsForThisCircle = numberToSeed / centersForCircles.Count;
                        if ( i == 0 )
                            seedsForThisCircle += numberToSeed % centersForCircles.Count;
                        if ( this.debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Seeding " + seedsForThisCircle + " for circle " + i + " centered on " + centersForCircles[i].X + ", " + centersForCircles[i].Y, Verbosity.DoNotShow );
                        BadgerUtilityMethods.addPointsInCircle( seedsForThisCircle, Context, centersForCircles[i], this.radiusForPlanetPlacement,
                                                               minimumDistanceBetweenPlanets, this.vertices );
                    }
                }
            }
            else
            {
                bool allowClustersInPoints = true;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Adding points in start screen", Verbosity.DoNotShow );
                BadgerUtilityMethods.addPointsInStartScreen( numberToSeed, Context, minimumDistanceBetweenPlanets, this.vertices,
                                                             allowClustersInPoints, false );
                if ( this.vertices.Count == 0 )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "BUG: could not generate enough points with rectancle and clusgers", Verbosity.DoNotShow );

                    allowClustersInPoints = false;
                    BadgerUtilityMethods.addPointsInStartScreen( numberToSeed, Context, minimumDistanceBetweenPlanets, this.vertices,
                                                                 allowClustersInPoints, false );

                }
            }
        }
    }


    public class Mapgen_Circles : IMapGenerator
    {
        public Mapgen_Circles()
        { 
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_Circles : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            bool linkNormally = true;
            bool linkRNG = false;
            bool linkSpanning = false;
            bool linkGabriel = false;
            bool spanOnly = false;
            int numberToSeed = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            //this map type is vaguely like solar systems, but it's intended to be a different
            //sort of style. The layout is
            //One central circle, surrounded by a bunch of smaller circles. Every point in the center connects to its
            //outer circle. So in a Solar System POV, the central circle are the Suns, the outer circle are its planets orbiting
            //but we have this layout (instead of the suns at the center of the orbiting planets) because it's much more readable
            //numberToSeed =  BadgerUtilityMethods.getSettingValueInt("NumPlanets");
            //if(numberToSeed == 0)
            //  numberToSeed = 80;
            //string mapName = mapType.InternalName;
            int linkingMethodNum = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "SolarSystemsLinkMethod" ).RelatedIntValue;
            ArcenDebugging.LogSingleLine("got method " + linkingMethodNum, Verbosity.DoNotShow );
            if ( linkingMethodNum == 0 )
            {
                linkNormally = true;//the default
            }
            if ( linkingMethodNum == 1 ) 
            {
                linkNormally = false;
                spanOnly = true;
            }
            if ( linkingMethodNum == 2 )
            {
                linkNormally = false;
                linkRNG = true;
            }
            if ( linkingMethodNum == 3 )
            {
                linkNormally = false;
                linkGabriel = true;
            }

            if ( linkingMethodNum == 4 )
            {
                linkNormally = false;
                linkSpanning = true;
            }

            //int planetsLeftToAllocate = numberToSeed;

            int minPlanetsPerCircle = 4; //it's kinda nice if there's a single circle of 4 planets,
                                         //but more than one such is boring. Once we have
                                         // one such circle, we bump this value up
            int maxPlanetsPerCircle = 9;
            //figure out how many circles there are, and how many planets are in each circle
            ThrowawayListCanMemLeak<int> planetsPerCircle = BadgerUtilityMethods.allocatePlanetsIntoGroups( minPlanetsPerCircle,
                                                                                     maxPlanetsPerCircle, numberToSeed, true,
                                                                                     Context );

            int numberOfCircles = planetsPerCircle.Count;
            //Now create each circle

            int outerCircleRadius = this.getRadiusOfOuterCircles( numberOfCircles );
            int innerCircleRadius = this.getRadiusOfSmallCircle( numberOfCircles );
            ThrowawayListCanMemLeak<ArcenPoint> innerCirclePlanetPoints;
            //note that this function returns the locations of the inner circle planets
            //as an out parameter (otherwise it was hard to get the center of the outer circle to be on the same
            //line as the Sun)
            ThrowawayListCanMemLeak<ArcenPoint> centerOfOuterCircles = this.getCenterOfOuterCircles( galaxy, Context,
                                                                            numberOfCircles, outerCircleRadius,
                                                                            Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter, out innerCirclePlanetPoints,
                                                                            innerCircleRadius );

            //create the inner circle (aka Suns)
            ThrowawayListCanMemLeak<Planet> innerCirclePlanets = this.makeCircle( galaxy, Context, innerCirclePlanetPoints );
            if ( linkNormally )
            {
                BadgerUtilityMethods.createRNGGraph( innerCirclePlanets );
            }
            for ( int i = 0; i < centerOfOuterCircles.Count; i++ )
            {
                //create all the outer planets for this circle
                ThrowawayListCanMemLeak<Planet> planetsForThisCircle = this.makeCircle( galaxy, Context,
                                                                         planetsPerCircle[i], this.getRadiusOfSmallCircle( planetsPerCircle[i] ), centerOfOuterCircles[i] );
                if ( linkNormally )
                {
                    //link the planets of the solar system (aka outer circle)
                    BadgerUtilityMethods.createRNGGraph( planetsForThisCircle );
                    //link the inner planet (Sun) to the nearest member of its orbiting planet
                    this.linkPlanetToNearestCircle( galaxy, innerCirclePlanets[i], planetsForThisCircle );
                }
            }
            if ( spanOnly )
            {
                BadgerUtilityMethods.createMinimumSpanningTree( galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs() );
            }
            if ( linkSpanning )
            {
                BadgerUtilityMethods.createMinimumSpanningTree( galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs() );
                BadgerUtilityMethods.addRandomConnections( galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs(), 5, Context, 5 );
            }
            if ( linkGabriel )
            {
                BadgerUtilityMethods.createGabrielGraph( galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs() );
            }
            if ( linkRNG )
            {
                BadgerUtilityMethods.createRNGGraph( galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs() );
            }

            return;
        }

        //Note that the center of the outer circle and its matching Sun
        //need to be on the same line, so we return the points on the inner circle as an out parameter
        private ThrowawayListCanMemLeak<ArcenPoint> getCenterOfOuterCircles( Galaxy galaxy, ArcenHostOnlySimContext Context, int outerCircles, int outerRadius, ArcenPoint circleCenter,
                                                         out ThrowawayListCanMemLeak<ArcenPoint> innerCirclePoints, int innerRadius )
        {
            innerCirclePoints = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            //shamelessly stolen ;-)
            AngleDegrees angleBetweenRingPlanet = AngleDegrees.Create( (float)360 / (float)outerCircles );
            AngleDegrees ringAngle = AngleDegrees.Create( (float)Context.RandomToUse.NextWithInclusiveUpperBound( 10, 350 ) );

            ThrowawayListCanMemLeak<ArcenPoint> outerCircleCenters = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            bool flipOffsetForNextRingPlanet = false;
            int offsetOuterFlip = 10;
            //We need to stagger the outer planet radii once there are enough of them
            if ( outerCircles > 7 )
                offsetOuterFlip = 100;
            if ( outerCircles > 12 )
                offsetOuterFlip = 200;

            for ( int i = 0; i < outerCircles; i++ )
            {
                ArcenPoint innerPoint = circleCenter.GetPointAtAngleAndDistance( ringAngle, innerRadius + (flipOffsetForNextRingPlanet ? 15 : 30) );
                ArcenPoint outerPoint = circleCenter.GetPointAtAngleAndDistance( ringAngle, outerRadius + (flipOffsetForNextRingPlanet ? offsetOuterFlip : 0) );
                outerCircleCenters.Add( outerPoint );
                innerCirclePoints.Add( innerPoint );
                flipOffsetForNextRingPlanet = !flipOffsetForNextRingPlanet;
                ringAngle = ringAngle.Add( angleBetweenRingPlanet );
            }
            return outerCircleCenters;
        }
        //maps the Sun to its corresponding planets
        private void linkPlanetToNearestCircle( Galaxy galaxy, Planet innerCirclePlanet, ThrowawayListCanMemLeak<Planet> outerCircle )
        {
            //takes a planet in the inner circle and an outer circle. Link the inner planet and the closest outer planet
            int minDistanceSoFar = 9999;
            int indexOfMinDistance = -1;
            for ( int i = 0; i < outerCircle.Count; i++ )
            {
                Planet outerPlanet = outerCircle[i];
                int distance = Mat.DistanceBetweenPointsImprecise( innerCirclePlanet.GalaxyLocation,
                                                         outerPlanet.GalaxyLocation );
                if ( distance < minDistanceSoFar )
                {
                    minDistanceSoFar = distance;
                    indexOfMinDistance = i;
                }
            }
            innerCirclePlanet.AddLinkTo( outerCircle[indexOfMinDistance] );
        }

        //This function is overloaded; one version takes a circleCenter, finds all the planets and then
        //creates/links them
        //the other version starts knowing all the points, then only has to create the planets and link them
        private ThrowawayListCanMemLeak<Planet> makeCircle( Galaxy galaxy, ArcenHostOnlySimContext Context, int planetsOnCircle, int radius, ArcenPoint circleCenter )
        {
            //Generate a connected circle of planets around the circleCenter with a given radius
            PlanetType planetType = PlanetType.Normal;
            //7) Compute average angle for next step from 360/planets_left
            AngleDegrees angleBetweenRingPlanet = AngleDegrees.Create( (float)360 / (float)planetsOnCircle );

            //8) Pick Random Starting Angle from 0 to 359
            AngleDegrees ringAngle = AngleDegrees.Create( (float)Context.RandomToUse.NextWithInclusiveUpperBound( 10, 350 ) );

            ThrowawayListCanMemLeak<Planet> ringPlanets = new ThrowawayListCanMemLeak<Planet>( 500 );
            ArcenPoint linePoint;
            Planet newPlanetInRing;
            bool flipOffsetForNextRingPlanet = false;
            bool debug = false;
            if ( debug )
            {
                string s = String.Format( "Creating Circle of {0}  planets centered on {1},{2}",//centered on ({1},{2}) at radius {3}",
                                           planetsOnCircle, circleCenter.X, circleCenter.Y );
                ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
            }

            for ( int i = 0; i < planetsOnCircle; i++ )
            {
                //-- compute point on line from origin at angle at target radius +40
                linePoint = circleCenter.GetPointAtAngleAndDistance( ringAngle, radius + (flipOffsetForNextRingPlanet ? 15 : 30) );

                //-- place planet there
                newPlanetInRing = galaxy.AddPlanet( planetType, linePoint,
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );

                ringPlanets.Add( newPlanetInRing );


                flipOffsetForNextRingPlanet = !flipOffsetForNextRingPlanet;
                ringAngle = ringAngle.Add( angleBetweenRingPlanet );
            }
            return ringPlanets;
        }
        //overloaded version of makeAndConnectCircle for when we already know all the points
        private ThrowawayListCanMemLeak<Planet> makeCircle( Galaxy galaxy, ArcenHostOnlySimContext Context, ThrowawayListCanMemLeak<ArcenPoint> planetPoints )
        {
            //Generate a connected circle of planets around the circleCenter with a given radius
            PlanetType planetType = PlanetType.Normal;
            ThrowawayListCanMemLeak<Planet> ringPlanets = new ThrowawayListCanMemLeak<Planet>( 500 );

            for ( int i = 0; i < planetPoints.Count; i++ )
            {
                //-- place planet there
                Planet newPlanetInRing = galaxy.AddPlanet( planetType, planetPoints[i],
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                ringPlanets.Add( newPlanetInRing );
            }
            return ringPlanets;
        }

        private int getRadiusOfSmallCircle( int smallCircle )
        {
            int radius;
            if ( smallCircle < 4 )
                radius = 45;
            else if ( smallCircle < 5 )
                radius = 55;
            else if ( smallCircle < 6 )
                radius = 65;
            else if ( smallCircle < 7 )
                radius = 75;
            else
                radius = 100;
            return radius;

        }
        private int getRadiusOfOuterCircles( int numberOfSmallCircles )
        {
            int radius;
            //stolen from Wheel
            if ( numberOfSmallCircles < 3 )
            {
                radius = 300;
            }
            else if ( numberOfSmallCircles < 4 )
                radius = 310;
            else if ( numberOfSmallCircles < 5 )
                radius = 320;
            else if ( numberOfSmallCircles < 6 )
                radius = 370;
            else if ( numberOfSmallCircles < 7 )
                radius = 390;
            else if ( numberOfSmallCircles < 8 )
                radius = 400;
            else if ( numberOfSmallCircles < 8 )
                radius = 410;
            else if ( numberOfSmallCircles < 9 )
                radius = 430;
            else if ( numberOfSmallCircles < 10 )
                radius = 440;
            else if ( numberOfSmallCircles < 11 )
                radius = 460;
            else
                radius = 490;

            return radius;
        }
    }

    public class Mapgen_Tutorial : IMapGenerator
    {
        public Mapgen_Tutorial()
        { 
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }
        /*This class is intended as a teaching exercise. To create a new map generator,
          you must declase a class that implements IMapGenerator. It has a "Generate" function that is
          called by the main AI War 2 code that actually creates the map. The galaxy map is laid out on a giant grid.
          The center of the galaxy is (0,0) and you can have both positive and negative coordinates.

          The input to the Generate function are as follows. The "Galaxy" object is what you populate with Planets
          to create the map for your game. The Context is used to generate random numbers (and I'm sure many other things).
          The numberToSeed is the number of planets, and the mapType is something you (the coder)
          can use if you want to have multiple map types sharing a similar codebase.
          You can look at the RootLatticeGenerator for an example of one IMapGenerator sharing
          multiple mapTypes.

        This example code generates a bunch of random planets simply connected

        One critical thing to make sure that all your planets are connected. If they are not all connected then you will get obscure bugs. 
         See Mantis bug 19086

        To have this entry appear as a selection option in the Game Start Screen, add an entry like 
         <map_type name="Tutorial" 
                  display_name="ExampleForAspiringModders" <=== this will be the name that appears in Game Select
                  description="~*~" <==== hovertext for the map type
                  dll_name="AIWarExternalCode" <== must be External Code
                  type_name="Arcen.AIW2.External.Mapgen_Tutorial" <=== the name of this class
      >
      </map_type>

    to GameData/Configuration/MapType/YourFile_MapTypes.xml

    */
    public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_Tutorial : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            string s; //this is used for debugging printouts later in this function
            ArcenDebugging.ArcenDebugLogSingleLine( "Welcome to the test generator\n", Verbosity.DoNotShow ); //this message will go in PlayerData/ArcenDebugLog.txt
                                                                                                              //this is invaluable for debugging purposes

            //An ArcenPoint is a data structure containing (at least) an X,Y coordinate pair
            //creating a new planet requires an ArcenPoint
            //This test generator will hard code in some ArcenPoints,
            //then put planets at those points

            //The map itself is a cartesian coordinate plane centered on 0,0

            PlanetType planetType = PlanetType.Normal; //Normal is to say "not a Nomad".
                                                       //unless you know otherwise, always use PlanetType.Normal
            ThrowawayListCanMemLeak<ArcenPoint> planetPoints = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            ArcenPoint originPlanetPoint = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter; //this is (0,0) in tha aforementioned coordinate plane
            Planet originPlanet = galaxy.AddPlanet( planetType, originPlanetPoint,
                World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );

            //First we create a list of points, then we will create Planets at those points
            ArcenDebugging.ArcenDebugLogSingleLine( "populate PlanetPoints list\n", Verbosity.DoNotShow );
            planetPoints.Add( ArcenPoint.Create( 0, 100 ) ); //so this point has X=0 and Y=100
            planetPoints.Add( ArcenPoint.Create( 500, 0 ) ); // X=500, Y=0
            planetPoints.Add( ArcenPoint.Create( 600, 100 ) ); //etc
            planetPoints.Add( ArcenPoint.Create( -100, -100 ) );
            planetPoints.Add( ArcenPoint.Create( 0, -600 ) );
            planetPoints.Add( ArcenPoint.Create( -100, 0 ) );

            int numberToSeed = planetPoints.Count;//reset numberToSeed for this example
                                                  //int distance = Mat.DistanceBetweenPointsImprecise(planetPoints[0], planetPoints[1]); //This is how to check the distance between Points
            ArcenDebugging.ArcenDebugLogSingleLine( "I have created my points, now let's make the planets\n", Verbosity.DoNotShow );
            Planet previousPlanet = null;
            for ( int i = 0; i < numberToSeed - 1; i++ )
            {
                //this uses a printf-style formatting string to generate a more elaborate debugging message
                s = String.Format( "Adding planet {0} at location {1},{2}\n", i,
                                         planetPoints[i].X, planetPoints[i].Y );
                ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                //calling galaxy.AddPlanet adds a planet to the galaxy; it takes a planetType (probably "Normal"),
                //an ArcenPoint and the Context passed into the Generate functino
                Planet planet = galaxy.AddPlanet( planetType, planetPoints[i],
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                if ( previousPlanet == null )
                    planet.AddLinkTo( originPlanet ); //if we have no previous planet, link this planet to the origin planet
                else
                    planet.AddLinkTo( previousPlanet );
                previousPlanet = planet;

            }
            //int numLinkedToOrigin = originPlanet.GetLinkedNeighborCount(); //count how many planets are connected to originPlanet
            //bool isLinkedToOrigin = originPlanet.GetIsDirectlyLinkedTo(galaxy.Planets[2]); //checks if this is linked to origin planet
            s = String.Format( "My galaxy has {0} planets. Return now\n", galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() );
            ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
            return;
        }
    }
    public class Mapgen_Compass : IMapGenerator
    {
        public Mapgen_Compass()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        public static ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<Planet>> allRings = new ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<Planet>>( 300 );
        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_Compass : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            int numberToSeed = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            int centerSizeParm = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "CompassCenterSize" ).RelatedIntValue;
            int connectTheRings = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "CompassConnectTheRings" ).RelatedIntValue;
            int numRings = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "CompassNumRings" ).RelatedIntValue + 2; //this setting starts at 0. Shrug
            int connections = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "CompassConnectionsToRing" ).RelatedIntValue + 1; //this setting starts at 0. Shrug
            allRings.Clear();
            //First create a cluster at the center of the map
            int centerClusterSize = 6 + Context.RandomToUse.Next( 4, 8 ) * centerSizeParm;
            ArcenDebugging.ArcenDebugLogSingleLine( "Compass. centerSizeParm " + centerSizeParm + " (" + centerClusterSize + ") numRings " + numRings + " connections " + connections, Verbosity.DoNotShow );

            int radiusForCentralCluster = 250 + (centerClusterSize / 5) * 100;

            int distBetweenPoints = 40 + (centerClusterSize / 5) * 10;
            ThrowawayListCanMemLeak<ArcenPoint> allPoints = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            ThrowawayListCanMemLeak<ArcenPoint> centerClusterPoints = BadgerUtilityMethods.addPointsInCircle( centerClusterSize, Context, Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter,
                                                                                           radiusForCentralCluster, distBetweenPoints, allPoints );
            ThrowawayListCanMemLeak<Planet> centerClusterPlanets = BadgerUtilityMethods.convertPointsToPlanets( centerClusterPoints, galaxy, Context );
            BadgerUtilityMethods.createGabrielGraph( centerClusterPlanets );
            //Add Circular Points is for the rings
            ThrowawayListCanMemLeak<int> planetsPerRing = new ThrowawayListCanMemLeak<int>( 300 );
            int remainingRingPoints = numberToSeed - centerClusterSize;
            for ( int i = 0; i < numRings; i++ )
            {
                if ( i == numRings - 1 )
                {
                    planetsPerRing.Add( remainingRingPoints );
                    break;
                }
                int pointsForThisRing = remainingRingPoints / (numRings + 2 - i);
                planetsPerRing.Add( pointsForThisRing );
                remainingRingPoints -= pointsForThisRing;
            }

            int distBetweenRings = 200;
            if ( numRings == 0 )
                numRings = 1;
            ThrowawayListCanMemLeak<Planet> planetsToAvoid = new ThrowawayListCanMemLeak<Planet>( 500 );
            for ( int i = numRings - 1; i >= 0; i-- )
            {
                //note I need to do this backwards so we don't wind up saying "We can't link to the outer ring since it would cross over links between inner rings"
                int numPlanets = planetsPerRing[i];
                int radiusForThisRing = (radiusForCentralCluster + distBetweenRings) + distBetweenRings * i;
                //ArcenDebugging.ArcenDebugLogSingleLine("For ring " + i + " using radius " + radiusForThisRing +" and allocation " + numPlanets + " planets." , Verbosity.DoNotShow );
                ThrowawayListCanMemLeak<ArcenPoint> pointsForThisRing = BadgerUtilityMethods.addCircularPoints( numPlanets, Context, Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter,
                                                                                             radiusForThisRing, allPoints );
                ThrowawayListCanMemLeak<Planet> planetsForThisRing = BadgerUtilityMethods.convertPointsToPlanets( pointsForThisRing, galaxy, Context );
                allRings.Add( planetsForThisRing );
                BadgerUtilityMethods.createRNGGraph( planetsForThisRing );
                BadgerUtilityMethods.linkPlanetLists( galaxy, centerClusterPlanets, planetsForThisRing, Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter, false, connections, true, Context, false, planetsToAvoid );
            }
            if ( connectTheRings > 0 && numRings > 1 )
            {
                for ( int i = numRings - 1; i >= 1; i-- ) //don't do the last ring
                {
                    BadgerUtilityMethods.linkPlanetLists( galaxy, allRings[i], allRings[i - 1], Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter, false, Context.RandomToUse.Next( 1, 4 ), true, Context );
                }
            }
        }
    }
    public class Mapgen_Dissonance : IMapGenerator
    {
        public Mapgen_Dissonance()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        private Dictionary<ArcenPoint, int> AreasToAvoid = Dictionary<ArcenPoint, int>.Create_WillNeverBeGCed( 100, "Mapgen_Dissonance-AreasToAvoid" ); //no new planets near here and no links through here
        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_Dissonance : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            //Here's how this works. We do something roughly like simple/realistic (handing out planets at random in a big area, then linking
            //What we also do is define some "Don't put planets here or let links run through here" sections of the map"
            int numberToSeed = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            int numBigAreas = 1 + Context.RandomToUse.Next( 0, 3 );
            int numMediumAreas = 1 + Context.RandomToUse.Next( 1, 4 );
            int numSmallAreas = 3 + Context.RandomToUse.Next( 1, 4 );
            if ( numberToSeed >= 100 )
            {
                numBigAreas += 1;
                numMediumAreas += 2;
                numSmallAreas += 3;
            }
            int totalAvoidanceAreas = numBigAreas + numMediumAreas + numSmallAreas;
            int baseDistance = 100;
            int distBigArea = 400;
            int distMedArea = 320;
            int distSmallArea = 180;
            ThrowawayListCanMemLeak<ArcenPoint> avoidancePoints = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            AreasToAvoid.Clear();
            BadgerUtilityMethods.addPointsInStartScreen( totalAvoidanceAreas, Context, baseDistance, avoidancePoints, false, false );
            for ( int i = 0; i < avoidancePoints.Count; i++ )
            {
                ArcenPoint point = avoidancePoints[i];
                if ( i < numBigAreas )
                    AreasToAvoid[point] = distBigArea;
                else if ( i < numBigAreas + numMediumAreas )
                    AreasToAvoid[point] = distMedArea;
                else
                    AreasToAvoid[point] = distSmallArea;
            }
            ThrowawayListCanMemLeak<ArcenPoint> vertices = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            BadgerUtilityMethods.addPointsInStartScreen( numberToSeed, Context, baseDistance, vertices, false, false, 5, AreasToAvoid );
            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( 500 );
            for ( int i = 0; i < vertices.Count; i++ )
            {
                Planet planet = galaxy.AddPlanet( PlanetType.Normal, vertices[i],
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                allPlanets.Add( planet );
            }
            int dissonanceConnectionStyle = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "DissonanceConnectivity" ).RelatedIntValue;
            //now link with a reasonable algorithm
            if ( dissonanceConnectionStyle == 0 )
                BadgerUtilityMethods.nearestNeighborGraph( allPlanets, Context, 5, 100, 50, 35, 4 );
            else if ( dissonanceConnectionStyle == 1 )
                BadgerUtilityMethods.createGabrielGraph( galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs() );
            else
                BadgerUtilityMethods.createRNGGraph( galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs() );
            // int linksToRemove = Context.RandomToUse.Next(2, 10);
            // BadgerUtilityMethods.removeSomeLinksBetweenPlanets( linksToRemove, allPlanets, Context );
            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }
    public class Mapgen_Classic : IMapGenerator
    {
        public Mapgen_Classic()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        private Dictionary<ArcenPoint, int> AreasToAvoid = Dictionary<ArcenPoint, int>.Create_WillNeverBeGCed( 100, "Mapgen_Classic-AreasToAvoid" ); //no new planets near here and no links through here
        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_Classic : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            //Here's how this works. We do something roughly like simple/realistic (handing out planets at random in a big area, then linking
            //What we also do is define some "Don't put planets here or let links run through here" sections of the map"
            int numberToSeed = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            int numBigAreas = 0;
            int numMediumAreas = Context.RandomToUse.Next( 0, 1 );
            int numSmallAreas = Context.RandomToUse.Next( 0, 5 );
            if ( numberToSeed >= 100 )
            {
                numMediumAreas += 1;
                numSmallAreas += 2;
            }
            int totalAvoidanceAreas = numBigAreas + numMediumAreas + numSmallAreas;
            int baseDistance = 100;
            int distBigArea = 400;
            int distMedArea = 320;
            int distSmallArea = 180;
            ThrowawayListCanMemLeak<ArcenPoint> avoidancePoints = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            AreasToAvoid.Clear();
            BadgerUtilityMethods.addPointsInStartScreen( totalAvoidanceAreas, Context, baseDistance, avoidancePoints, false, false );
            for ( int i = 0; i < avoidancePoints.Count; i++ )
            {
                ArcenPoint point = avoidancePoints[i];
                if ( i < numBigAreas )
                    AreasToAvoid[point] = distBigArea;
                else if ( i < numBigAreas + numMediumAreas )
                    AreasToAvoid[point] = distMedArea;
                else
                    AreasToAvoid[point] = distSmallArea;
            }
            ThrowawayListCanMemLeak<ArcenPoint> vertices = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            BadgerUtilityMethods.addPointsInStartScreen( numberToSeed, Context, baseDistance, vertices, true, false, 5, AreasToAvoid, true );
            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( 500 );
            for ( int i = 0; i < vertices.Count; i++ )
            {
                Planet planet = galaxy.AddPlanet( PlanetType.Normal, vertices[i],
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                allPlanets.Add( planet );
            }
            int classicConnectionStyle = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "ClassicConnectivity" ).RelatedIntValue;
            //now link with a reasonable algorithm
            if ( classicConnectionStyle == 0 )
            {
                int percentNearestLink = 100;
                int percentSecondLink = 60;
                int percentThirdLink = 50;
                int numCrossingLinks = Context.RandomToUse.Next( 3, 5);
                BadgerUtilityMethods.nearestNeighborGraph( allPlanets, Context, 5, percentNearestLink, percentSecondLink, percentThirdLink, numCrossingLinks );
            }
            else if ( classicConnectionStyle == 1 )
                BadgerUtilityMethods.createGabrielGraph( galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs() );
            else
                BadgerUtilityMethods.createRNGGraph( galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs() );
            // int linksToRemove = Context.RandomToUse.Next(2, 10);
            // BadgerUtilityMethods.removeSomeLinksBetweenPlanets( linksToRemove, allPlanets, Context );
            BadgerUtilityMethods.makeOneDeadEndPlanet( allPlanets, Context ); //I like this map a lot, I just want there to be a nice defensible homeworld I can pick if I want
            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }
    public class Mapgen_Test1 : IMapGenerator
    {
        public Mapgen_Test1()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }
        bool debug = false;
        private Dictionary<ArcenPoint, int> AreasToAvoid = Dictionary<ArcenPoint, int>.Create_WillNeverBeGCed( 100, "Mapgen_Classic-AreasToAvoid" ); //no new planets near here and no links through here
        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_Test1 : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            //Here's how this works. We do something roughly like simple/realistic (handing out planets at random in a big area, then linking
            //What we also do is define some "Don't put planets here or let links run through here" sections of the map"
            int numberToSeed = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            int minPlanetsPerRegion = 4;
            int maxPlanetsPerRegion = 8;
            bool onlyOneOfSmallestRegion = true;
            int distanceBetweenRegions = 200;
            bool allowClustersInPoints = true;
            bool isSquare = false;
            int alignmentNumber = 10;
            int rand = Context.RandomToUse.Next(1, 5);
            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( 500 );
            ThrowawayListCanMemLeak<ArcenPoint>vertices = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            if ( rand % 2 == 0 )
            {                
                //now for each region, find a center point (chosen randomly)
                //then allocate the points
                int baseDistance = 70;
                BadgerUtilityMethods.addPointsInStartScreen( numberToSeed, Context, baseDistance, vertices, true, false, 5, AreasToAvoid, true );
                for ( int i = 0; i < vertices.Count; i++ )
                {
                    Planet planet = galaxy.AddPlanet( PlanetType.Normal, vertices[i],
                                                      World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                    allPlanets.Add( planet );
                }
            }
            else
            {
                
                ThrowawayListCanMemLeak<int> regionsOfPlanets = BadgerUtilityMethods.allocatePlanetsIntoGroups( minPlanetsPerRegion,
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

                ThrowawayListCanMemLeak<ArcenPoint> regionCenters = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
                BadgerUtilityMethods.addPointsInStartScreen( regionsOfPlanets.Count, Context,
                                                             distanceBetweenRegions, regionCenters, allowClustersInPoints,
                                                             isSquare, alignmentNumber );
                ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<Planet>> allPlanetsPerRegion = new ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<Planet>>( 300 );

                for ( int i = 0; i < regionCenters.Count; i++ )
                {
                    ThrowawayListCanMemLeak<ArcenPoint> pointsForThisRegion = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );;
                    int radiusPerRegion = 250;
                    int minDistanceBetweenPlanets = 40;
                    pointsForThisRegion = BadgerUtilityMethods.addPointsInCircleWithExclusion( regionsOfPlanets[i], Context, regionCenters[i], radiusPerRegion,
                                                                                               minDistanceBetweenPlanets, pointsForThisRegion, regionCenters, radiusPerRegion + 20, alignmentNumber );
                    ThrowawayListCanMemLeak<Planet> planetsForThisRegion = BadgerUtilityMethods.convertPointsToPlanets( pointsForThisRegion, galaxy, Context );
                    allPlanets.AddRange( planetsForThisRegion );
                }
            }
            
            //int classicConnectionStyle = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "ClassicConnectivity" ).RelatedIntValue;
            //now link with a reasonable algorithm
            int classicConnectionStyle = 0;
            if ( classicConnectionStyle == 0 )
            {
                int percentNearestLink = 100;
                int percentSecondLink = 60;
                int percentThirdLink = 50;
                int numCrossingLinks = Context.RandomToUse.Next( 3, 5);
                BadgerUtilityMethods.nearestNeighborGraph( allPlanets, Context, 5, percentNearestLink, percentSecondLink, percentThirdLink, numCrossingLinks );
            }
            else if ( classicConnectionStyle == 1 )
                BadgerUtilityMethods.createGabrielGraph( galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs() );
            else
                BadgerUtilityMethods.createRNGGraph( galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs() );
            int linksToRemove = Context.RandomToUse.Next(12, 20);
            BadgerUtilityMethods.removeSomeLinksBetweenPlanets( linksToRemove, allPlanets, Context );
            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }
    public class Mapgen_Pairs : IMapGenerator
    {

        public Mapgen_Pairs()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }
        private Dictionary<ArcenPoint, int> AreasToAvoid = Dictionary<ArcenPoint, int>.Create_WillNeverBeGCed( 100, "Mapgen_Classic-AreasToAvoid" ); //no new planets near here and no links through here
        private ThrowawayListCanMemLeak<ArcenPoint> verticesL;
        private ThrowawayListCanMemLeak<ArcenPoint> verticesR;
        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_Pairs : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            //Here's how this works. We do something roughly like simple/realistic (handing out planets at random in a big area, then linking
            //What we also do is define some "Don't put planets here or let links run through here" sections of the map"
            int numPoints = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            int x = 1200;
            int y = 600;
            if ( numPoints > 80 )
            {
                x = 1300;
                y = 800;
            }
            if ( numPoints > 180 )
            {
                x = 1900;
                y = 1400;
            }
            if ( numPoints > 200 )
            {
                x = 2300;
                y = 1900;
            }
            if ( numPoints > 300 )
            {
                x = 3000;
                y = 2000;
            }
            if ( numPoints >= 300 )
            {
                x = 3500;
                y = 2500;
            }

            ArcenPoint topL = ArcenPoint.Create( -x, y );
            ArcenPoint topLM = ArcenPoint.Create( -5, y );
            ArcenPoint topRM = ArcenPoint.Create( 5, y );
            ArcenPoint topR = ArcenPoint.Create( x, y );
            ArcenPoint bottomL = ArcenPoint.Create( -x, -y );
            ArcenPoint bottomLM = ArcenPoint.Create( -5, -y );
            ArcenPoint bottomRM = ArcenPoint.Create( 5, -y );
            ArcenPoint bottomR = ArcenPoint.Create( x, -y );

            
            int minDistanceBetweenPlanets = 45; // planets will be packed more densely than this if needed.
            this.verticesL = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            this.verticesR = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            int divisibleByX = 2;

            BadgerUtilityMethods.addPointsInRectangle( numPoints / 2, Context, minDistanceBetweenPlanets, verticesL,
                                                         divisibleByX, topL, topLM, bottomL, bottomLM);

            BadgerUtilityMethods.addPointsInRectangle( numPoints / 2, Context, minDistanceBetweenPlanets, verticesR,
                                                         divisibleByX, topRM, topR, bottomRM, bottomR );

            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( 500 );

            ThrowawayListCanMemLeak<Planet> planetsForMapL = BadgerUtilityMethods.convertPointsToPlanets( this.verticesL, galaxy, Context );
            ThrowawayListCanMemLeak<Planet> planetsForMapR = BadgerUtilityMethods.convertPointsToPlanets( this.verticesR, galaxy, Context );
            allPlanets.AddRange( planetsForMapL );
            allPlanets.AddRange( planetsForMapR );

            BadgerUtilityMethods.createRNGGraph( planetsForMapL );
            BadgerUtilityMethods.createGabrielGraph( planetsForMapR );

            BadgerUtilityMethods.makeOneDeadEndPlanet( planetsForMapL, Context ); //I like this map a lot, I just want there to be a nice defensible homeworld I can pick if I want
            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }
    public class Mapgen_FatSnake : IMapGenerator
    {
        public Mapgen_FatSnake()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_FatSnake : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            int thickness = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "SnakeThickness" ).RelatedIntValue;
            ArcenDebugging.ArcenDebugLogSingleLine( "got thickness " + thickness + " and increase it by 2", Verbosity.DoNotShow );
            thickness += 2;

            int extraConnections = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "SnakeConnectivity" ).RelatedIntValue;
            bool fewerConnections = false;
            bool moreConnections = false;
            bool mixedConnections = false;
            if ( extraConnections == 1 )
                fewerConnections = true;
            else if ( extraConnections == 2 )
                moreConnections = true;
            else if ( extraConnections == 3 )
                mixedConnections = true;
            int numberToSeed = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            ThrowawayListCanMemLeak<ArcenPoint> allPoints = new ThrowawayListCanMemLeak<ArcenPoint>( 300 ); //not really used
            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( 500 );
            ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<ArcenPoint>> pointsByChunk = new ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<ArcenPoint>>( 300 );
            ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<Planet>> planetsByChunk = new ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<Planet>>( 300 );
            int chunkDistance = 45;
            int horizontalDistance = 105 * thickness;
            int angledY = 40;
            int verticalDistance = 75 + angledY * thickness;
            if ( thickness == 7 )
                thickness = -1; //this is "varied"; change this after the horizontal/vertical settings

            bool daliStyle = BadgerUtilityMethods.getSettingValueBool_Expensive( mapConfig, "DaliStyle" );
            ThrowawayListCanMemLeak<ArcenPoint> pointsForThisRegion = BadgerUtilityMethods.addCenteredGridOfPlanets( numberToSeed, Context, Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter, thickness, chunkDistance, horizontalDistance, verticalDistance, allPoints, ref pointsByChunk, true, angledY, daliStyle );
            for ( int i = 0; i < pointsByChunk.Count; i++ )
            {
                ThrowawayListCanMemLeak<ArcenPoint> chunk = pointsByChunk[i];
                ThrowawayListCanMemLeak<Planet> chunkPlanets = new ThrowawayListCanMemLeak<Planet>( 500 );
                for ( int j = 0; j < chunk.Count; j++ )
                {
                    Planet planet = galaxy.AddPlanet( PlanetType.Normal, chunk[j],
                        World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                    chunkPlanets.Add( planet );
                    allPlanets.Add( planet );
                }
                planetsByChunk.Add( chunkPlanets );
            }

            //now connect things
            for ( int i = 0; i < planetsByChunk.Count; i++ )
            {
                ThrowawayListCanMemLeak<Planet> thisChunk = planetsByChunk[i];
                BadgerUtilityMethods.createGabrielGraph( thisChunk ); //link each chunk
                if ( i < planetsByChunk.Count - 1 )
                {
                    ThrowawayListCanMemLeak<Planet> nextChunk = planetsByChunk[i + 1];
                    //now link this chunk to the next chunk
                    int numLinksMade = 0;
                    for ( int j = 0; j < thisChunk.Count; j++ )
                    {
                        //go through this chunk. Randomly choose whether to
                        //link a planet with the corresponding planet in the next chunk
                        if ( fewerConnections || mixedConnections )
                        {
                            if ( Context.RandomToUse.Next( 0, 100 ) < 50 )
                                continue;
                        }
                        numLinksMade++;
                        if ( j < nextChunk.Count )
                            thisChunk[j].AddLinkTo( nextChunk[j] ); //in case there are fewer planets in the next chunk
                    }
                    if ( numLinksMade == 0 )
                    {
                        //if we haven't made a link, add a single random connection which issafe
                        thisChunk[Context.RandomToUse.Next( 0, thisChunk.Count )].AddLinkTo( nextChunk[Context.RandomToUse.Next( 0, nextChunk.Count )] );
                    }
                }
            }
            if ( moreConnections || mixedConnections )
            {
                int connectionsToAdd = numberToSeed / 16;
                BadgerUtilityMethods.addRandomConnections( galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs(), connectionsToAdd, Context, 5 );
            }
            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }

    }
    public class Mapgen_Nebula : IMapGenerator
    {
        public Mapgen_Nebula()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        /* The goal of this map type is to have a varierty of "regions" of planets,
           with a different layout of planets and a randomly chosen linking algorithm for each
           region. This gives a really organic feel to things.

           TODO: add some additional "seeding" algorithms to the initial planet placements (for example, maybe a circle?
           maybe a small grid?)
           Also add some "Link regions differently" code, and maybe consider linking adjacent regions (so it's not just
           always linked in a spanning tree, but maybe in a gabriel or something)
           Also add Spanning + random connections to the way of linking inside a region

           This galaxy type also implements ClustersMini, wherein the regions are tightly packed
        */

        bool debug = false;
        bool veryVerboseDebug = false;
        //bool checkSettings = false; //the values will be fed in from MapGeneration.cs' Mapgen_ClustersRoot for false
        //otherwise we will check the settings directly

        public bool isAsteroid = false;
        public bool isSquare = true;
        public int numClustersHint = 2;
        public int nebulaConnectivity = 2; //tunes the connectivity algorithms for Nebula
        public bool addSomeExtraLinks = false; //for Asteroid,
                                               //which is normally linked via spanning tree
        public bool removeSomeLinks = false; //sometimes for the square

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_Nebula : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            this.addSomeExtraLinks = false;
            int debugCode = 0;
            try
            {
                debugCode = 100;
                //numberToSeed =  BadgerUtilityMethods.getSettingValueInt("NumPlanets");
                //if(numberToSeed == 0)
                //  numberToSeed = 80;

                isSquare = false;
                if ( mapType.Name == "TheSquare" )
                    isSquare = true;

                int numberToSeed = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
                if ( numberToSeed < 20 )
                    numberToSeed = 20;

                int percentForOneCircle = 20;
                int circleDone = -1;

                //string mapName = mapType.InternalName;
                // if( this.checkSettings)
                //   {
                //     if ( ArcenStrings.Equals( mapName, "Asteroid") )
                //         this.isAsteroid = true;
                //     else
                //         this.isAsteroid = false;
                //   }

                //These parameters are tuned based on the numClustersHint
                int minPlanetsPerRegion = 6;
                int maxPlanetsPerRegion = 17;
                int radiusPerRegion = 130;
                int distanceBetweenRegions = 360;
                int minDistanceBetweenPlanets = 60;

                //get the user requested settings
                // if( this.checkSettings)
                //     this.nebulaSettingUpdates(out this.numClustersHint, out this.nebulaConnectivity, this.isAsteroid);
                if ( this.isAsteroid )
                {
                    if ( this.numClustersHint == 1 )
                    {
                        //Few clusters, so each one is larger
                        minPlanetsPerRegion = 7;
                        maxPlanetsPerRegion = 12;
                        if ( numberToSeed > 120 )
                        {
                            minPlanetsPerRegion += 2;
                            maxPlanetsPerRegion += 2;
                        }
                        radiusPerRegion = 140;
                        distanceBetweenRegions = 380;
                    }
                    if ( this.numClustersHint == 2 )
                    {
                        minPlanetsPerRegion = 5;
                        maxPlanetsPerRegion = 10;
                        if ( numberToSeed > 120 )
                        {
                            minPlanetsPerRegion += 1;
                            maxPlanetsPerRegion += 1;
                        }

                        radiusPerRegion = 120;
                        distanceBetweenRegions = 260;
                    }
                    if ( this.numClustersHint == 3 )
                    {
                        minPlanetsPerRegion = 4;
                        maxPlanetsPerRegion = 7;
                        radiusPerRegion = 110;
                        distanceBetweenRegions = 240;
                    }
                }
                else if ( isSquare )
                {
                    int squareSizes = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "SquareSize" ).RelatedIntValue;
                    if ( squareSizes == 0 )
                    {
                        minPlanetsPerRegion = 3;
                        maxPlanetsPerRegion = 5;
                        radiusPerRegion = 110;
                        distanceBetweenRegions = 240;
                    }
                    else if ( squareSizes == 1 )
                    {
                        minPlanetsPerRegion = 7;
                        maxPlanetsPerRegion = 10;
                        radiusPerRegion = 150;
                        distanceBetweenRegions = 220;
                    }
                    else if ( squareSizes == 2 )
                    {
                        minPlanetsPerRegion = 10;
                        maxPlanetsPerRegion = 12;
                        radiusPerRegion = 200;
                        distanceBetweenRegions = 180;
                    }
                    else
                    {
                        minPlanetsPerRegion = 5;
                        maxPlanetsPerRegion = 12;
                        radiusPerRegion = 200;
                        distanceBetweenRegions = 200;
                    }
                }
                else
                {
                    /* Nebula map mode */
                    if ( this.numClustersHint == 1 )
                    {
                        //Few clusters, so each one is larger
                        minPlanetsPerRegion = 11;
                        maxPlanetsPerRegion = 21;
                        radiusPerRegion = 220;
                        if ( numberToSeed > 120 )
                        {
                            minPlanetsPerRegion += numberToSeed / 80;
                            maxPlanetsPerRegion += numberToSeed / 80;
                            radiusPerRegion += numberToSeed / 200;
                        }
                        distanceBetweenRegions = 340;
                    }
                    if ( this.numClustersHint == 2 )
                    {
                        minPlanetsPerRegion = 6;
                        maxPlanetsPerRegion = 18;
                        radiusPerRegion = 210;
                        if ( numberToSeed > 120 )
                        {
                            minPlanetsPerRegion += numberToSeed / 80;
                            maxPlanetsPerRegion += numberToSeed / 80;
                            radiusPerRegion += numberToSeed / 100;
                        }
                        distanceBetweenRegions = 180;
                    }
                    if ( this.numClustersHint == 3 )
                    {
                        minPlanetsPerRegion = 5;
                        maxPlanetsPerRegion = 13;
                        radiusPerRegion = 190;
                        distanceBetweenRegions = 170;
                    }
                    if ( this.numClustersHint == 4 )
                    {
                        minPlanetsPerRegion = 16;
                        maxPlanetsPerRegion = 21;
                        radiusPerRegion = 220;
                        distanceBetweenRegions = 170;
                        if ( numberToSeed > 160 )
                        {
                            minPlanetsPerRegion += numberToSeed / 50;
                            maxPlanetsPerRegion += numberToSeed / 50;
                            radiusPerRegion += numberToSeed / 70;
                        }
                    }


                }
                // if( this.isAsteroid && this.checkSettings)
                //     this.addSomeExtraLinks = BadgerUtilityMethods.getSettingValueBool("addBonusLinksAsteroid");
                debugCode = 200;
                if ( this.debug )
                {
                    string s = String.Format( "minPlanetsPerRegion: " + minPlanetsPerRegion + " maxPlanetsPerRegion " + maxPlanetsPerRegion + " radiusPerRegion " + radiusPerRegion + " distanceBetweenRegions " + distanceBetweenRegions + " isAsteroid " + isAsteroid + " connectivity " + this.nebulaConnectivity + " numClustersHint " + this.numClustersHint );
                    ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                }

                bool onlyOneOfSmallestRegion = false;

                int alignmentNumber = 10; //align all points on numbers divisible by this value. It makes things look more organized
                ThrowawayListCanMemLeak<int> regionsOfPlanets = BadgerUtilityMethods.allocatePlanetsIntoGroups( minPlanetsPerRegion,
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
                ThrowawayListCanMemLeak<ArcenPoint> regionCenters = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
                bool allowClustersInPoints = false;

                BadgerUtilityMethods.addPointsInStartScreen( regionsOfPlanets.Count, Context,
                                                             distanceBetweenRegions, regionCenters, allowClustersInPoints,
                                                             isSquare, alignmentNumber );
                if ( this.veryVerboseDebug )
                {
                    for ( int i = 0; i < regionCenters.Count; i++ )
                    {
                        string s = String.Format( "Region Center: {0}, {1}", regionCenters[i].X, regionCenters[i].Y );
                        ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                    }
                }
                ThrowawayListCanMemLeak<ArcenPoint> allPoints = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
                ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<Planet>> allPlanetsPerRegion = new ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<Planet>>( 300 );
                ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( 500 );

                /* Tuning parameters for how to link the various Nebulae/Asteroids */

                //percentages for linking within a region

                //settings for the Nebula
                int percentSpanningTreeInRegion;
                int percentSpanningTreeWithConnectionsInRegion;
                int percentGabrielInRegion;
                int percentRNGInRegion;
                //percentages for linking the different regions (inter-region)
                LinkMethod regionLinkMethod = LinkMethod.Gabriel;
                this.getNebulaLinkingPercentages( Context, this.isAsteroid, this.nebulaConnectivity,
                                                  out percentSpanningTreeInRegion, out percentSpanningTreeWithConnectionsInRegion,
                                                  out percentGabrielInRegion, out percentRNGInRegion,
                                                  out regionLinkMethod, mapConfig, mapType );
                debugCode = 400;
                for ( int i = 0; i < regionCenters.Count; i++ )
                {
                    //For each region, add planets and then link the region together
                    ThrowawayListCanMemLeak<ArcenPoint> pointsForThisRegion;
                    if ( this.isAsteroid && circleDone == -1 &&
                       regionsOfPlanets[i] > 4 && regionsOfPlanets[i] < 9 &&
                       percentForOneCircle > Context.RandomToUse.Next( 0, 100 ) ) // chance of a circle for Asteroids
                    {
                        if ( this.debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Adding a circle", Verbosity.DoNotShow );
                        //sometimes we might want to just make a circle
                        circleDone = i;
                        pointsForThisRegion = BadgerUtilityMethods.addCircularPoints( regionsOfPlanets[i], Context, regionCenters[i],
                                                                radiusPerRegion, null );
                    }
                    else if ( isSquare && BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "SquareIntraClusterLinkMethod" ).RelatedIntValue == 0 )
                    {
                        //planets in each region are laid out in squares
                        int planetsPerChunk = 1;
                        int horizontalDistance = 70;
                        int verticalDistance = 70;
                        int chunkDistance = 30;
                        ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<ArcenPoint>> pointsByChunk = new ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<ArcenPoint>>( 300 ); //unused here, used for Fat Snake
                        pointsForThisRegion = BadgerUtilityMethods.addCenteredGridOfPlanets( regionsOfPlanets[i], Context, regionCenters[i], planetsPerChunk, chunkDistance, horizontalDistance, verticalDistance, allPoints, ref pointsByChunk, false, -1, false );
                    }
                    else
                        pointsForThisRegion = BadgerUtilityMethods.addPointsInCircleWithExclusion( regionsOfPlanets[i], Context, regionCenters[i], radiusPerRegion,
                                                                                                  minDistanceBetweenPlanets, allPoints, regionCenters, radiusPerRegion + 20, alignmentNumber );

                    ThrowawayListCanMemLeak<Planet> planetsForThisRegion = BadgerUtilityMethods.convertPointsToPlanets( pointsForThisRegion, galaxy, Context );
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
                        if ( removeSomeLinks )
                        {
                            int maxToRemove = 2;
                            if ( regionsOfPlanets[i] < 8 )
                                maxToRemove = 1;
                            if ( circleDone == i )
                                maxToRemove = 0;
                            BadgerUtilityMethods.removeSomeLinksBetweenPlanets( maxToRemove, planetsForThisRegion, Context );
                        }
                    }
                    else if ( method == LinkMethod.RNG )
                    {
                        //RNG
                        BadgerUtilityMethods.createRNGGraph( planetsForThisRegion );
                        if ( removeSomeLinks )
                        {
                            int maxToRemove = 1;
                            if ( regionsOfPlanets[i] < 8 )
                                maxToRemove = 0;
                            if ( circleDone == i )
                                maxToRemove = 0;
                            BadgerUtilityMethods.removeSomeLinksBetweenPlanets( maxToRemove, planetsForThisRegion, Context );
                        }


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
                if ( this.veryVerboseDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "choosing inter-region link method " + regionLinkMethod, Verbosity.DoNotShow );


                if ( regionLinkMethod == LinkMethod.SpanningTree )
                {
                    int[,] connectionMatrix = BadgerUtilityMethods.createMinimumSpanningTreeLinks( regionCenters );
                    for ( int i = 0; i < regionCenters.Count; i++ )
                    {
                        for ( int j = i + 1; j < regionCenters.Count; j++ )
                        {
                            if ( j >= regionCenters.Count )
                                ArcenDebugging.ArcenDebugLogSingleLine( "BUG! FIXME", Verbosity.DoNotShow );
                            if ( connectionMatrix[i, j] == 1 )
                            {
                                BadgerUtilityMethods.linkPlanetLists( galaxy, allPlanetsPerRegion[i], allPlanetsPerRegion[j], regionCenters[j] );
                            }
                        }
                    }
                }
                else if ( regionLinkMethod == LinkMethod.Gabriel )
                {
                    int[,] connectionMatrix = BadgerUtilityMethods.createGabrielGraphLinks( regionCenters );
                    for ( int i = 0; i < regionCenters.Count; i++ )
                    {
                        for ( int j = i + 1; j < regionCenters.Count; j++ )
                        {
                            if ( j >= regionCenters.Count )
                                ArcenDebugging.ArcenDebugLogSingleLine( "BUG! FIXME", Verbosity.DoNotShow );
                            if ( connectionMatrix[i, j] == 1 )
                            {
                                if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine("linking resgions " + i + " and " + j, Verbosity.DoNotShow );
                                BadgerUtilityMethods.linkPlanetLists( galaxy, allPlanetsPerRegion[i], allPlanetsPerRegion[j], regionCenters[j] );
                            }
                            else if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine("choosing not to link resgions " + i + " and " + j, Verbosity.DoNotShow );
                        }
                    }
                }
                else if ( regionLinkMethod == LinkMethod.RNG )
                {
                    int[,] connectionMatrix = BadgerUtilityMethods.createRNGGraphLinks( regionCenters );
                    for ( int i = 0; i < regionCenters.Count; i++ )
                    {
                        for ( int j = i + 1; j < regionCenters.Count; j++ )
                        {
                            if ( j >= regionCenters.Count )
                                ArcenDebugging.ArcenDebugLogSingleLine( "BUG! FIXME", Verbosity.DoNotShow );
                            if ( connectionMatrix[i, j] == 1 )
                            {
                                BadgerUtilityMethods.linkPlanetLists( galaxy, allPlanetsPerRegion[i], allPlanetsPerRegion[j], regionCenters[j] );
                            }
                        }
                    }
                }
                else if ( regionLinkMethod == LinkMethod.SpanningTreeWithConnections )
                {
                    int[,] connectionMatrix = BadgerUtilityMethods.createMinimumSpanningTreeLinks( regionCenters );
                    for ( int i = 0; i < regionCenters.Count; i++ )
                    {
                        for ( int j = i + 1; j < regionCenters.Count; j++ )
                        {
                            if ( j >= regionCenters.Count )
                                ArcenDebugging.ArcenDebugLogSingleLine( "BUG! FIXME", Verbosity.DoNotShow );
                            if ( connectionMatrix[i, j] == 1 )
                            {
                                BadgerUtilityMethods.linkPlanetLists( galaxy, allPlanetsPerRegion[i], allPlanetsPerRegion[j], regionCenters[j] );
                            }
                        }
                    }
                    //now let's link everything together at the end a bit better
                    if ( this.debug )
                    {
                        string s = String.Format( "Adding a few random connections at the end" );
                        ArcenDebugging.ArcenDebugLogSingleLine( s, Verbosity.DoNotShow );
                    }
                    int connectionsToAdd = 2;
                    if ( isSquare )
                    {
                        if ( addSomeExtraLinks )
                            connectionsToAdd = -1;
                        else
                            connectionsToAdd = 0;
                    }

                    BadgerUtilityMethods.addRandomConnections( allPlanets, connectionsToAdd, Context, 5 );
                }
                else
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "BUG: linking regions with unknown algorithm", Verbosity.DoNotShow );
                }
                //BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( false, allPlanets );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in Nebula generation. code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }

        private void getNebulaLinkingPercentages( ArcenHostOnlySimContext Context, bool isAsteroid, int nebulaConnectivity,
                                        out int percentSpanningTreeInRegion, out int percentSpanningTreeWithConnectionsInRegion,
                                        out int percentGabrielInRegion, out int percentRNGInRegion,
                                                  out LinkMethod InterRegionLinkingMethod, MapConfiguration mapConfig, MapTypeData mapType )
        {
            percentSpanningTreeInRegion = 10;
            percentSpanningTreeWithConnectionsInRegion = 10;
            percentGabrielInRegion = 40;
            percentRNGInRegion = 40;
            InterRegionLinkingMethod = LinkMethod.Gabriel;
            if ( isSquare )
            {
                int intraLinkingVal = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "SquareIntraClusterLinkMethod" ).RelatedIntValue;
                if ( intraLinkingVal == 0 )
                {
                    //random!
                    if ( Context.RandomToUse.Next( 0, 100 ) < 50 )
                    {
                        percentSpanningTreeInRegion = 30;
                        percentSpanningTreeWithConnectionsInRegion = 30;
                        percentGabrielInRegion = 30;
                        percentRNGInRegion = 10;
                    }
                    else
                    {
                        percentSpanningTreeInRegion = 10;
                        percentSpanningTreeWithConnectionsInRegion = 40;
                        percentGabrielInRegion = 40;
                        percentRNGInRegion = 10;
                    }
                }
                else if ( intraLinkingVal == 1 )
                {
                    percentSpanningTreeInRegion = 0;
                    percentSpanningTreeWithConnectionsInRegion = 0;
                    percentGabrielInRegion = 0;
                    percentRNGInRegion = 100;
                }
                else if ( intraLinkingVal == 2 )
                {
                    percentSpanningTreeInRegion = 0;
                    percentSpanningTreeWithConnectionsInRegion = 0;
                    percentGabrielInRegion = 100;
                    percentRNGInRegion = 0;
                }
                else if ( intraLinkingVal == 3 )
                {
                    percentSpanningTreeInRegion = Context.RandomToUse.Next( 0, 100 );
                    percentSpanningTreeWithConnectionsInRegion = 100 - percentSpanningTreeInRegion;
                    percentGabrielInRegion = 0;
                    percentRNGInRegion = 0;
                }
                else if ( intraLinkingVal == 4 )
                {
                    percentSpanningTreeInRegion = 0;
                    percentSpanningTreeWithConnectionsInRegion = 10;
                    percentGabrielInRegion = 45;
                    percentRNGInRegion = 45;
                }

                int rand = Context.RandomToUse.Next( 0, 100 );
                int overallLinkingVal = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "SquareLinkingStyle" ).RelatedIntValue;
                if ( overallLinkingVal == 0 )
                {
                    InterRegionLinkingMethod = LinkMethod.Gabriel;
                }
                else if ( overallLinkingVal == 1 )
                {
                    InterRegionLinkingMethod = LinkMethod.SpanningTreeWithConnections;
                }
                else
                {
                    InterRegionLinkingMethod = LinkMethod.SpanningTreeWithConnections;
                    addSomeExtraLinks = true;
                }
            }
            else if ( !isAsteroid )
            {
                if ( nebulaConnectivity == 1 )
                {
                    percentSpanningTreeInRegion = 20;
                    percentSpanningTreeWithConnectionsInRegion = 30;
                    percentGabrielInRegion = 20;
                    percentRNGInRegion = 30;
                    InterRegionLinkingMethod = LinkMethod.RNG;
                }
                if ( nebulaConnectivity == 2 )
                {
                    percentSpanningTreeInRegion = 10;
                    percentSpanningTreeWithConnectionsInRegion = 10;
                    percentGabrielInRegion = 40;
                    percentRNGInRegion = 40;
                    InterRegionLinkingMethod = LinkMethod.Gabriel;
                }
                if ( nebulaConnectivity == 3 )
                {
                    percentSpanningTreeInRegion = 5;
                    percentSpanningTreeWithConnectionsInRegion = 5;
                    percentGabrielInRegion = 45;
                    percentRNGInRegion = 45;
                    InterRegionLinkingMethod = LinkMethod.Gabriel;
                }
                if ( nebulaConnectivity == 4 )
                {
                    percentSpanningTreeInRegion = 0;
                    percentSpanningTreeWithConnectionsInRegion = 0;
                    percentGabrielInRegion = 90;
                    percentRNGInRegion = 10;
                    InterRegionLinkingMethod = LinkMethod.Gabriel;
                }
            }
            if ( isAsteroid )
            {
                percentSpanningTreeInRegion = 0;
                percentSpanningTreeWithConnectionsInRegion = 0;
                percentGabrielInRegion = 100;
                percentRNGInRegion = 0;
                int randomNumber = Context.RandomToUse.NextWithInclusiveUpperBound( 0, 2 );
                if ( randomNumber == 0 )
                    InterRegionLinkingMethod = LinkMethod.Gabriel;
                else if ( randomNumber == 1 )
                    InterRegionLinkingMethod = LinkMethod.RNG;
                else
                {
                    InterRegionLinkingMethod = LinkMethod.SpanningTree;
                    if ( addSomeExtraLinks )
                    {
                        InterRegionLinkingMethod = LinkMethod.SpanningTreeWithConnections;
                    }
                }
            }

        }

        private void nebulaSettingUpdates( MapConfiguration mapConfig, MapTypeData mapType, out int numClustersHint, out int nebulaConnectivity, bool isAsteroid )
        {
            numClustersHint = 2;
            if ( isAsteroid )
                numClustersHint = 1;
            nebulaConnectivity = 2;
            int settingValue;

            if ( isAsteroid )
                settingValue = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "NumberOfAsteroids" ).RelatedIntValue;
            else
                settingValue = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "NumberOfNebulae" ).RelatedIntValue;

            numClustersHint = settingValue;
            if ( !isAsteroid )
            {
                settingValue = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "NebulaeConnectivity" ).RelatedIntValue;
                nebulaConnectivity = settingValue;
            }
            return;
        }

        // void nebulaSettingUpdates( out int minPlanetsPerRegion, out int maxPlanetsPerRegion,
        //                            out int radiusPerRegion, out int distanceBetweenRegions, bool isAsteroid)
        //   {

        //For this version, we passed inthe explicit region sizes. Was my original attempt
        //     //set defaults in case Settings aren't there
        //      minPlanetsPerRegion = 6;
        //      maxPlanetsPerRegion = 17;
        //      radiusPerRegion = 210;
        //      distanceBetweenRegions = 210;
        //      if(isAsteroid)
        //        {
        //          minPlanetsPerRegion = 4;
        //          maxPlanetsPerRegion = 10;
        //          radiusPerRegion = 130;
        //          distanceBetweenRegions = 360;
        //        }

        //     ArcenSetting setting;
        //     if(isAsteroid)
        //       setting = BadgerUtilityMethods.getSettingByName("MinPlanetsPerAsteroidRegion");
        //     else
        //       setting = BadgerUtilityMethods.getSettingByName("MinPlanetsPerNebulaRegion");

        //     if(setting != null && setting.TempValue_Int != 0)
        //       {
        //         minPlanetsPerRegion = setting.TempValue_Int;
        //       }

        //     if(isAsteroid)
        //       setting = BadgerUtilityMethods.getSettingByName("MaxPlanetsPerAsteroidRegion");
        //     else
        //       setting = BadgerUtilityMethods.getSettingByName("MaxPlanetsPerNebulaRegion");

        //     if(setting != null  && setting.TempValue_Int != 0)
        //       {
        //         maxPlanetsPerRegion = setting.TempValue_Int;
        //       }
        //     if(isAsteroid)
        //       setting = BadgerUtilityMethods.getSettingByName("radiusPerRegionAsteroid");
        //     else
        //       setting = BadgerUtilityMethods.getSettingByName("radiusPerRegionNebula");

        //     if(setting != null  && setting.TempValue_Int != 0)
        //       {
        //         radiusPerRegion = setting.TempValue_Int;
        //       }
        //     if(isAsteroid)
        //       setting = BadgerUtilityMethods.getSettingByName("distanceBetweenAsteroidRegion");
        //     else
        //       setting = BadgerUtilityMethods.getSettingByName("distanceBetweenNebulaRegion");
        //     if(setting != null  && setting.TempValue_Int != 0)
        //       {
        //         distanceBetweenRegions = setting.TempValue_Int;
        //       }
        //   }
        //returns the index of the smallest distance that's larger than smallestDistanceSoFar
        //ie if our distances are 4, 5, 6, 7 and smallestDistanceSoFar == 5
        //then it would return 6
        public int findNextValue( int[] distanceFromCenter, int smallestDistanceSoFar, int numRegions )
        {
            int idx = -1;
            int bestFit = 9999;
            for ( int i = 0; i < numRegions; i++ )
            {
                //note must be >= smallestDistanceSoFar in case two regions are equidistant
                //this is allowable because we delete the entry for each match after it is made
                if ( distanceFromCenter[i] >= smallestDistanceSoFar && distanceFromCenter[i] < bestFit )
                {
                    idx = i;
                    bestFit = distanceFromCenter[i];
                }
            }
            return idx;
        }
    }

    public class Mapgen_Octopus : IMapGenerator
    {
        public Mapgen_Octopus()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        /* This map type was suggested by Tadrinth on the forums. He couched it as 
           "Spiral galaxy: large cluster in the middle (Body), 8 arms coming off, each arm is a series of linked small clusters.  "
           which made me think of an octopus. So variables are named like it's an octopus

           It was initially coded by BadgerBadger, and then Tadrinth made some welcome tweaks.

           Original modification notes for Tadrinth:
           We figure out how many planets belong in each arm and how many planets go in the body.
           Planets are allocated via the "addPointsInCircle" because that's easily implemented (and because Keith
           had already written most of the pieces, so I could steal it readily).

           If you want two clusters per arm and a bit of a spiral then I suggest
           you allocate more planets per Arm (note the minPlanetsPerArm and maxPlanetsPerArm variables at the top),
           then allocate a second armCenter that's a bit further away from the body and at a slightly different angle

           You can connect gruops of planets via the linkPlanetLists function, so just call that first to link the two
           clusters in each arm

           Tadrinth update notes:
           We seed the core of the galaxy as either one big center cluster, or a ring of smaller clusters.
           Then we seed pairs of arms; each arm is made up of an middle cluster and an outer cluster.   
           Then we link everything together. 
           */

        private readonly bool debug = false;
        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_Octopus : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            int numberToSeed = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );

            int symmetryFactor = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "OctopusNumArms" ).RelatedIntValue;

            int radius = 100; // base radius for each cluster (center is twice as large)
            int distForThisArm = 105; // base distance for how far out each arm should be placed
            int minDistanceBetweenPlanets = 45; // planets will be packed more densely than this if needed.
            int alignmentNumber = 10; //align all planets on points divisible by this value. It makes things look more organized
            if ( numberToSeed < 20 )
            {
                radius = 70;
                distForThisArm = 80;
            }
            else if ( numberToSeed < 60 )
            {
                radius = 90;
                distForThisArm = 100;
            }
            else if ( numberToSeed < 80 )
            {
                radius = 100;
                distForThisArm = 120;
            }
            else if ( numberToSeed < 110 )
            {
                radius = 130;
                distForThisArm = 145;
            }
            else if ( numberToSeed < 200 )
            {
                radius = 200;
                distForThisArm = 205;
            }
            else if ( numberToSeed < 300 )
            {
                radius = 220;
                distForThisArm = 270;
            }
            else if ( numberToSeed < 400 )
            {
                radius = 250;
                distForThisArm = 315;
            }
            else
            {
                radius = 350;
                distForThisArm = 415;
            }

            // need at least symmetry three for multi-cluster method to look decent
            bool singleLargeCentralCluster = (symmetryFactor < 3) || Context.RandomToUse.NextBool();

            if ( this.debug )
                ArcenDebugging.ArcenDebugLogSingleLine( string.Format( "Generating a spiral galaxy with symmetry {0} and {1}", symmetryFactor, singleLargeCentralCluster ? "one large central cluster" : "a ring of small central clusters" ), Verbosity.Chat );


            ArcenPoint galacticCenter = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;

            ThrowawayListCanMemLeak<int> centerClusterSizes = new ThrowawayListCanMemLeak<int>( 300 );
            ThrowawayListCanMemLeak<int> innerArmClusterSizes = new ThrowawayListCanMemLeak<int>( 300 );
            ThrowawayListCanMemLeak<int> outerArmClusterSizes = new ThrowawayListCanMemLeak<int>( 300 );

            // spread half the planets evenly across the clusters
            int minPlanetsPerCluster = Math.Max( numberToSeed / (symmetryFactor * 5) / 2, 2 );
            for ( int i = 0; i < symmetryFactor; i++ )
            {
                centerClusterSizes.Add( minPlanetsPerCluster );
                innerArmClusterSizes.Add( minPlanetsPerCluster );
                innerArmClusterSizes.Add( minPlanetsPerCluster );
                outerArmClusterSizes.Add( minPlanetsPerCluster );
                outerArmClusterSizes.Add( minPlanetsPerCluster );
            }


            int planetsRemaining = numberToSeed - symmetryFactor * minPlanetsPerCluster * 5;

            // spread rest of planets randomly across the clusters
            // have to adjust ratios a bit depending on number of arms; the middle feels a little small with few arms and too big with many, otherwise
            int outerClusterRatio = 25;
            int innerClusterRatio = 30;
            if ( symmetryFactor > 2 )
            {
                outerClusterRatio = 35;
                innerClusterRatio = 30;
            }
            else if ( symmetryFactor > 4 )
            {
                outerClusterRatio = 50;
                innerClusterRatio = 40;
            }

            while ( planetsRemaining > 0 )
            {
                int percent = Context.RandomToUse.NextWithInclusiveUpperBound( 1, 100 );
                ThrowawayListCanMemLeak<int> clusterSizesListToAddTo;
                if ( percent > 100 - outerClusterRatio )
                {
                    clusterSizesListToAddTo = outerArmClusterSizes;
                }
                else if ( percent > 100 - outerClusterRatio - innerClusterRatio )
                {
                    clusterSizesListToAddTo = innerArmClusterSizes;
                }
                else
                {
                    clusterSizesListToAddTo = centerClusterSizes;
                }

                int i = Context.RandomToUse.Next( 0, clusterSizesListToAddTo.Count );
                clusterSizesListToAddTo[i] += 1;
                planetsRemaining -= 1;
            }

            if ( this.debug )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "center " + String.Join( ", ", centerClusterSizes ), Verbosity.DoNotShow );
                ArcenDebugging.ArcenDebugLogSingleLine( " inner " + String.Join( ", ", innerArmClusterSizes ), Verbosity.DoNotShow );
                ArcenDebugging.ArcenDebugLogSingleLine( " outer " + String.Join( ", ", outerArmClusterSizes ), Verbosity.DoNotShow );
            }


            //allocate the points for the body
            ThrowawayListCanMemLeak<ArcenPoint> allPoints = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            ThrowawayListCanMemLeak<ArcenPoint> bodyCenters = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<Planet>> bodyPlanets = new ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<Planet>>( 300 );

            //Figure out where to place the Arms; we split them evenly around the body
            //note that we update the armAngle for each iteration.

            AngleDegrees startingAngle = AngleDegrees.Create( (float)Context.RandomToUse.NextWithInclusiveUpperBound( 10, 350 ) ); // randomly spin before we start
            AngleDegrees anglePerArm = AngleDegrees.Create( (float)360 / (float)symmetryFactor );
            AngleDegrees subAnglePerArm = AngleDegrees.Create( (float)360 / (float)symmetryFactor / (float)3 );
            AngleDegrees spiralizeAngle = AngleDegrees.Create( (float)360 / (float)symmetryFactor / (float)6 ); // spin things slightly more as we go outward so it looks a little like a spiral galaxy

            AngleDegrees armAngle = startingAngle;

            ThrowawayListCanMemLeak<Planet> bodyCluster = new ThrowawayListCanMemLeak<Planet>( 500 );
            ArcenPoint center;

            // randomize linking method for large central cluster; usually densely connected since arms are sparse
            int percentGabriel = 80;
            int percentRNG = 10;
            int percentSpanningTree = 10;
            int percentSpanningTreeWithConnections = 0;

            LinkMethod linkingMethod = BadgerUtilityMethods.getRandomLinkMethod( percentSpanningTree, percentGabriel,
                                                                        percentRNG, percentSpanningTreeWithConnections, Context );

            if ( singleLargeCentralCluster )
            {
                int totalCentralPlanets = 0;
                for ( int i = 0; i < centerClusterSizes.Count; i++ )
                {
                    totalCentralPlanets += centerClusterSizes[i];
                }
                CreateClusterOfPlanets( bodyCluster, galaxy, Context, radius * 2, galacticCenter, minDistanceBetweenPlanets, alignmentNumber, totalCentralPlanets, ref allPoints, armAngle, linkingMethod, 0 );
                if ( true )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "total central planets: " + totalCentralPlanets, Verbosity.DoNotShow );
                    ArcenDebugging.ArcenDebugLogSingleLine( "created central planets: " + bodyCluster.Count, Verbosity.DoNotShow );
                }

            }

            for ( int i = 0; i < symmetryFactor; i++ )
            {
                if ( this.debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( string.Format( "creating cluster {0}", i ), Verbosity.DoNotShow );

                armAngle = armAngle.Add( anglePerArm );

                AngleDegrees firstArmAngle = armAngle.Add( spiralizeAngle );
                AngleDegrees secondArmAngle = firstArmAngle.Add( subAnglePerArm );
                if ( this.debug )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( string.Format( "armAngle {0}", armAngle ), Verbosity.DoNotShow );
                    ArcenDebugging.ArcenDebugLogSingleLine( string.Format( "first arm angle {0}", firstArmAngle ), Verbosity.DoNotShow );
                    ArcenDebugging.ArcenDebugLogSingleLine( string.Format( "second arm angle {0}", secondArmAngle ), Verbosity.DoNotShow );
                }


                //pick random method for linking clusters making up the central ring, again usually dense
                percentGabriel = 75;
                percentRNG = 15;
                percentSpanningTree = 10;
                percentSpanningTreeWithConnections = 0;
                linkingMethod = BadgerUtilityMethods.getRandomLinkMethod( percentSpanningTree, percentGabriel,
                                                                          percentRNG, percentSpanningTreeWithConnections,
                                                                          Context );
                if ( !singleLargeCentralCluster )
                {
                    bodyCluster = new ThrowawayListCanMemLeak<Planet>( 500 );
                    center = CreateClusterOfPlanets( bodyCluster, galaxy, Context, radius, galacticCenter, minDistanceBetweenPlanets, alignmentNumber, centerClusterSizes[i], ref allPoints, armAngle, linkingMethod, distForThisArm );
                    bodyPlanets.Add( bodyCluster );
                    bodyCenters.Add( center );
                }

                if ( this.debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( string.Format( "creating inner arm clusters {0}", i ), Verbosity.DoNotShow );

                // random link method for inner parts of the arms; these can be anything.
                percentGabriel = 50;
                percentRNG = 35;
                percentSpanningTree = 10;
                percentSpanningTreeWithConnections = 5;
                linkingMethod = BadgerUtilityMethods.getRandomLinkMethod( percentSpanningTree, percentGabriel,
                                                                          percentRNG, percentSpanningTreeWithConnections,
                                                                          Context );
                ThrowawayListCanMemLeak<Planet> innerArm1 = new ThrowawayListCanMemLeak<Planet>( 500 );
                ArcenPoint innerArm1Center = CreateClusterOfPlanets( innerArm1, galaxy, Context, radius, galacticCenter, minDistanceBetweenPlanets + 15, alignmentNumber, innerArmClusterSizes[2 * i], ref allPoints, firstArmAngle, linkingMethod, distForThisArm * 2 + 20 );

                if ( this.debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( string.Format( "creating second inner arm clusters {0}", i ), Verbosity.DoNotShow );
                ThrowawayListCanMemLeak<Planet> innerArm2 = new ThrowawayListCanMemLeak<Planet>( 500 );
                ArcenPoint innerArm2Center = CreateClusterOfPlanets( innerArm2, galaxy, Context, radius, galacticCenter, minDistanceBetweenPlanets + 15, alignmentNumber, innerArmClusterSizes[2 * i + 1], ref allPoints, secondArmAngle, linkingMethod, distForThisArm * 2 + 35 );

                // random link method for outer parts of arms; prefer sparse for good defense
                percentGabriel = 15;
                percentRNG = 15;
                percentSpanningTree = 60;
                percentSpanningTreeWithConnections = 10;
                linkingMethod = BadgerUtilityMethods.getRandomLinkMethod( percentSpanningTree, percentGabriel,
                                                                          percentRNG, percentSpanningTreeWithConnections,
                                                                          Context );

                if ( this.debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( string.Format( "creating outer arm clusters {0}", i ), Verbosity.DoNotShow );

                linkingMethod = BadgerUtilityMethods.getRandomLinkMethod( percentSpanningTree, percentGabriel,
                                                                          percentRNG, percentSpanningTreeWithConnections,
                                                                          Context );
                ThrowawayListCanMemLeak<Planet> outerArm1 = new ThrowawayListCanMemLeak<Planet>( 500 );
                ArcenPoint outerArm1Center = CreateClusterOfPlanets( outerArm1, galaxy, Context, radius + 30, galacticCenter, minDistanceBetweenPlanets + 40, alignmentNumber, outerArmClusterSizes[2 * i], ref allPoints, firstArmAngle.Add( spiralizeAngle ), LinkMethod.SpanningTreeWithConnections, distForThisArm * 4 );

                if ( this.debug )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( string.Format( "linking outer arm clusters {0}", i ), Verbosity.DoNotShow );
                    ArcenDebugging.ArcenDebugLogSingleLine( string.Format( "creating second outer arm clusters {0}", i ), Verbosity.DoNotShow );
                }

                linkingMethod = BadgerUtilityMethods.getRandomLinkMethod( percentSpanningTree, percentGabriel,
                                                                          percentRNG, percentSpanningTreeWithConnections,
                                                                          Context );
                ThrowawayListCanMemLeak<Planet> outerArm2 = new ThrowawayListCanMemLeak<Planet>( 500 );
                ArcenPoint outerArm2Center = CreateClusterOfPlanets( outerArm2, galaxy, Context, radius + 30, galacticCenter, minDistanceBetweenPlanets + 40, alignmentNumber, outerArmClusterSizes[2 * i + 1], ref allPoints, secondArmAngle.Add( spiralizeAngle ), linkingMethod, distForThisArm * 4 + 30 );

                // Link clusters together - inner to outer, body to inner
                BadgerUtilityMethods.linkPlanetLists( galaxy, innerArm1, outerArm1, outerArm1Center, false );
                BadgerUtilityMethods.linkPlanetLists( galaxy, bodyCluster, innerArm1, innerArm1Center, false );
                BadgerUtilityMethods.linkPlanetLists( galaxy, innerArm2, outerArm2, outerArm2Center, false );
                BadgerUtilityMethods.linkPlanetLists( galaxy, bodyCluster, innerArm2, innerArm2Center, false );
            }

            if ( !singleLargeCentralCluster )
            {
                if ( this.debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "linking central clusters together", Verbosity.DoNotShow );
                for ( int i = 0; i < symmetryFactor - 1; i++ )
                {
                    BadgerUtilityMethods.linkPlanetLists( galaxy, bodyPlanets[i], bodyPlanets[i + 1], bodyCenters[i + 1], false );

                }
                if ( Context.RandomToUse.NextBool() || Context.RandomToUse.NextBool() ) // occasionally skip completing the ring for spice
                {
                    if ( this.debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "linking last two central clusters together to make a ring", Verbosity.DoNotShow );
                    BadgerUtilityMethods.linkPlanetLists( galaxy, bodyPlanets[0], bodyPlanets[bodyPlanets.Count - 1], bodyCenters[bodyPlanets.Count - 1], false );
                }
            }

            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( false, galaxy );
        }

        private static ArcenPoint CreateClusterOfPlanets( ThrowawayListCanMemLeak<Planet> cluster, Galaxy galaxy, ArcenHostOnlySimContext Context, int radius, ArcenPoint galacticCenter, int minDistanceBetweenPlanets, int alignmentNumber, int clusterSize, ref ThrowawayListCanMemLeak<ArcenPoint> allPoints, AngleDegrees armAngle, LinkMethod linkingMethod, int distForThisArm )
        {
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( string.Format( "CreateClusterOfPlanets - creating cluster\n size: {0}\nangle: {1}\n dist: {2}", clusterSize, armAngle, distForThisArm, linkingMethod ), Verbosity.DoNotShow );

            ArcenPoint bodyCenter = galacticCenter.GetPointAtAngleAndDistance( armAngle, distForThisArm );
            ThrowawayListCanMemLeak<ArcenPoint> pointsForArm = BadgerUtilityMethods.addPointsInCircle( clusterSize, Context, bodyCenter, radius,
                                              minDistanceBetweenPlanets, allPoints, alignmentNumber );
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( string.Format( "CreateClusterOfPlanets - converting to planets", clusterSize, armAngle, distForThisArm ), Verbosity.DoNotShow );

            ThrowawayListCanMemLeak<Planet> planetsForThisArm = BadgerUtilityMethods.convertPointsToPlanets( pointsForArm, galaxy, Context );
            cluster.AddRange( planetsForThisArm );
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( string.Format( "CreateClusterOfPlanets - linking\n link: {0}", linkingMethod ), Verbosity.DoNotShow );

            if ( linkingMethod == LinkMethod.Gabriel )
                BadgerUtilityMethods.createGabrielGraph( planetsForThisArm );
            else if ( linkingMethod == LinkMethod.RNG )
                BadgerUtilityMethods.createRNGGraph( planetsForThisArm );
            else if ( linkingMethod == LinkMethod.SpanningTreeWithConnections )
            {
                BadgerUtilityMethods.createMinimumSpanningTree( planetsForThisArm );
            }
            else
            {
                BadgerUtilityMethods.createMinimumSpanningTree( planetsForThisArm );
            }

            return bodyCenter;
        }
    }

    /* Two lobes of star systems connected at a central junction, forming a figure-eight.
       The junction is the strategic chokepoint of the map; whoever holds it controls
       traffic between the two halves of the galaxy.

       Each lobe is internally connected with a configurable graph algorithm.
       The junction is a small cluster of 1-3 bridge planets linking both lobes. */
    public class Mapgen_FigureEight : IMapGenerator
    {
        public Mapgen_FigureEight() { }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_FigureEight : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            int numPlanets = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );

            int linkingMethodInt = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "FigureEightLinking" ).RelatedIntValue;
            int middleClusterOpt = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "FigureEightJunction" ).RelatedIntValue;
            bool asymmetric = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "FigureEightShape" ).RelatedIntValue == 1;

            int minDist = 45;
            int alignmentNumber = 10;

            // Scale the outer lobe radius with planet count, same as before.
            int lobeRadius;
            if ( numPlanets < 40 )
                lobeRadius = 90;
            else if ( numPlanets < 60 )
                lobeRadius = 120;
            else if ( numPlanets < 80 )
                lobeRadius = 145;
            else if ( numPlanets < 120 )
                lobeRadius = 170;
            else if ( numPlanets < 200 )
                lobeRadius = 220;
            else
                lobeRadius = 280;

            // The central cluster is smaller than the outer lobes — about 35% of the lobe radius.
            // lobeDistance is derived so that the lobe circles and the central cluster circle don't
            // overlap: lobe edge to cluster edge gap of ~55 units.
            int middleRadius = lobeRadius * 35 / 100;
            int lobeDistance = lobeRadius + middleRadius + 55;

            // Middle cluster size is 15%, 20%, or 25% of total planets depending on the option.
            int middlePct = 15 + middleClusterOpt * 5;
            int middlePlanetCount = Math.Max( 4, numPlanets * middlePct / 100 );
            int remainingPlanets = Math.Max( 10, numPlanets - middlePlanetCount );

            // Split remaining planets between the two outer lobes.
            int leftLobePlanets, rightLobePlanets;
            if ( asymmetric )
            {
                int leftPercent = Context.RandomToUse.Next( 35, 66 );
                leftLobePlanets = Math.Max( 5, remainingPlanets * leftPercent / 100 );
                rightLobePlanets = Math.Max( 5, remainingPlanets - leftLobePlanets );
            }
            else
            {
                leftLobePlanets = remainingPlanets / 2;
                rightLobePlanets = remainingPlanets - leftLobePlanets;
            }

            ArcenPoint galacticCenter = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;
            ArcenPoint leftCenter  = ArcenPoint.Create( galacticCenter.X - lobeDistance, galacticCenter.Y );
            ArcenPoint rightCenter = ArcenPoint.Create( galacticCenter.X + lobeDistance, galacticCenter.Y );

            // Place the central cluster first since it sits between the two outer lobes and is the most
            // spatially constrained. Placing it first ensures the outer lobe placement routine avoids
            // putting planets too close to the central cluster planets.
            ThrowawayListCanMemLeak<ArcenPoint> allPoints = new ThrowawayListCanMemLeak<ArcenPoint>( 500 );

            ThrowawayListCanMemLeak<ArcenPoint> middlePoints = BadgerUtilityMethods.addPointsInCircle( middlePlanetCount, Context, galacticCenter, middleRadius, minDist, allPoints, alignmentNumber );
            ThrowawayListCanMemLeak<Planet>     middlePlanets = BadgerUtilityMethods.convertPointsToPlanets( middlePoints, galaxy, Context );

            ThrowawayListCanMemLeak<ArcenPoint> leftPoints  = BadgerUtilityMethods.addPointsInCircle( leftLobePlanets,  Context, leftCenter,  lobeRadius, minDist, allPoints, alignmentNumber );
            ThrowawayListCanMemLeak<Planet>     leftPlanets  = BadgerUtilityMethods.convertPointsToPlanets( leftPoints,  galaxy, Context );

            ThrowawayListCanMemLeak<ArcenPoint> rightPoints = BadgerUtilityMethods.addPointsInCircle( rightLobePlanets, Context, rightCenter, lobeRadius, minDist, allPoints, alignmentNumber );
            ThrowawayListCanMemLeak<Planet>     rightPlanets = BadgerUtilityMethods.convertPointsToPlanets( rightPoints, galaxy, Context );

            // Internally link all three regions using the chosen method.
            if ( linkingMethodInt == 1 ) // Sparse (RNG)
            {
                BadgerUtilityMethods.createRNGGraph( leftPlanets );
                BadgerUtilityMethods.createRNGGraph( rightPlanets );
                BadgerUtilityMethods.createRNGGraph( middlePlanets );
            }
            else if ( linkingMethodInt == 2 ) // Minimal (Spanning Tree)
            {
                BadgerUtilityMethods.createMinimumSpanningTree( leftPlanets );
                BadgerUtilityMethods.createMinimumSpanningTree( rightPlanets );
                BadgerUtilityMethods.createMinimumSpanningTree( middlePlanets );
            }
            else // Spiderweb (Gabriel, default)
            {
                BadgerUtilityMethods.createGabrielGraph( leftPlanets );
                BadgerUtilityMethods.createGabrielGraph( rightPlanets );
                BadgerUtilityMethods.createGabrielGraph( middlePlanets );
            }

            // Connect the central cluster to each outer lobe. The number of cross-links scales with
            // the middle cluster size option so that a larger cluster has more entry points.
            int linksToLobes = middleClusterOpt + 1; // 1, 2, or 3
            BadgerUtilityMethods.linkPlanetLists( galaxy, middlePlanets, leftPlanets,  leftCenter,  false, linksToLobes, false, Context );
            BadgerUtilityMethods.linkPlanetLists( galaxy, middlePlanets, rightPlanets, rightCenter, false, linksToLobes, false, Context );

            // Guarantee full connectivity across all three regions.
            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( 500 );
            allPlanets.AddRange( leftPlanets );
            allPlanets.AddRange( rightPlanets );
            allPlanets.AddRange( middlePlanets );

            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }
}
