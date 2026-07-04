using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public static class UtilityMethods
    {
        public static void Helper_ConnectPlanetLists( IList<Planet> First, IList<Planet> Second, bool SuppressCrossoverAvoidance, bool ReCallWithSuppressCrossoverAvoidanceIfThatIsTheOnlyWayToConnect )
        {
            if ( First.Count <= 0 || Second.Count <= 0 )
                return;

            Planet closestPlanetOfFirst = null;
            Planet closestPlanetOfSecond = null;
            int closestDistance = 0;
            for ( int i = 0; i < First.Count; i++ )
            {
                Planet planetFromFirst = First[i];
                for ( int j = 0; j < Second.Count; j++ )
                {
                    Planet planetFromSecond = Second[j];
                    if (planetFromFirst == planetFromSecond)
                        continue;
                    if (planetFromFirst.GetIsDirectlyLinkedTo(true, planetFromSecond))
                        continue;
                    int distance = Mat.DistanceBetweenPointsImprecise( planetFromFirst.GalaxyLocation, planetFromSecond.GalaxyLocation );
                    if ( closestPlanetOfFirst == null || distance < closestDistance )
                    {
                        if ( SuppressCrossoverAvoidance || !GetWouldLinkCrossOverOtherPlanets( planetFromFirst, planetFromSecond ) )
                        {
                            closestPlanetOfFirst = planetFromFirst;
                            closestPlanetOfSecond = planetFromSecond;
                            closestDistance = distance;
                        }
                    }
                }
            }

            if ( closestPlanetOfFirst != null && closestPlanetOfSecond != null )
                closestPlanetOfFirst.AddLinkTo( closestPlanetOfSecond );
            else
            if ( ReCallWithSuppressCrossoverAvoidanceIfThatIsTheOnlyWayToConnect && !SuppressCrossoverAvoidance )
                Helper_ConnectPlanetLists( First, Second, true, false );
        }

        public static bool GetWouldLinkCrossOverOtherPlanets( Planet First, Planet Second )
        {
            IList<Planet> PlanetsToNoNotCrossOver = First.ParentGalaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs();
            for ( int k = 0; k < PlanetsToNoNotCrossOver.Count; k++ )
            {
                Planet planetToNotHit = PlanetsToNoNotCrossOver[k];
                if ( planetToNotHit == First || planetToNotHit == Second )
                    continue;
                if ( Mat.LineIntersectsRectangleContainingCircle( First.GalaxyLocation, Second.GalaxyLocation, planetToNotHit.GalaxyLocation, planetToNotHit.TypeData.IntraStellarRadius ) )
                    return true;
            }
            return false;
        }

        public static int Helper_GetNumberOfPairsInPairListInvolving( IList<Pair<int, int>> PairList, int First )
        {
            int result = 0;

            Pair<int, int> item;
            for ( int i = 0; i < PairList.Count; i++ )
            {
                item = PairList[i];
                if ( item.LeftItem != First && item.RightItem != First )
                    continue;
                result++;
            }

            return result;
        }

        public static bool Helper_GetDoesPairListContainPairInEitherDirection( IList<Pair<int, int>> PairList, int First, int Second )
        {
            Pair<int, int> item;
            for ( int i = 0; i < PairList.Count; i++ )
            {
                item = PairList[i];
                if ( item.LeftItem == First && item.RightItem == Second )
                    return true;
                if ( item.RightItem == First && item.LeftItem == Second )
                    return true;
            }
            return false;
        }

        public static bool HelperDoesPointListContainPointWithinDistance( IList<ArcenPoint> PointListOrNull, ArcenPoint Point, int Distance )
        {
            if ( PointListOrNull == null )
                return false;
            for ( int i = 0; i < PointListOrNull.Count; i++ )
            {
                if ( Mat.ApproxDistanceBetweenPointsFast( PointListOrNull[i], Point, Distance ) > Distance )
                    continue;
                if ( Mat.DistanceBetweenPointsImprecise( PointListOrNull[i], Point ) > Distance )
                    continue;
                return true;
            }
            return false;
        }

        public static void Helper_AddConnectionsWithinCluster( IList<Planet> PlanetsInCluster, int HoldOffAtThisManyLinksPerPlanet, bool SuppressCrossoverAvoidance )
        {
            for ( int j = 0; j < PlanetsInCluster.Count; j++ )
            {
                Planet planetInCluster = PlanetsInCluster[j];
                for ( int k = 0; k < PlanetsInCluster.Count; k++ )
                {
                    if ( k == j )
                        continue;
                    Planet otherPlanetInCluster = PlanetsInCluster[k];
                    if ( otherPlanetInCluster.GetIsDirectlyLinkedTo( false, planetInCluster ) )
                        continue;
                    if ( otherPlanetInCluster.GetLinkedNeighborCount() >= HoldOffAtThisManyLinksPerPlanet )
                        continue;
                    if ( !SuppressCrossoverAvoidance && GetWouldLinkCrossOverOtherPlanets( planetInCluster, otherPlanetInCluster ) )
                        continue;
                    int distanceToOtherPlanetInCluster = Mat.DistanceBetweenPointsImprecise( planetInCluster.GalaxyLocation, otherPlanetInCluster.GalaxyLocation );

                    bool foundNearerPlanet = false;
                    for ( int l = 0; l < PlanetsInCluster.Count; l++ )
                    {
                        Planet yetAnotherPlanetInCluster = PlanetsInCluster[l];
                        if ( l == j || l == k )
                            continue;
                        if ( otherPlanetInCluster.GetIsDirectlyLinkedTo( false, yetAnotherPlanetInCluster ) )
                            continue;
                        int distanceFromOtherToYetAnother = Mat.DistanceBetweenPointsImprecise( otherPlanetInCluster.GalaxyLocation, yetAnotherPlanetInCluster.GalaxyLocation );
                        if ( distanceFromOtherToYetAnother >= distanceToOtherPlanetInCluster )
                            continue;
                        if ( !SuppressCrossoverAvoidance && GetWouldLinkCrossOverOtherPlanets( yetAnotherPlanetInCluster, otherPlanetInCluster ) )
                            continue;
                        foundNearerPlanet = true;
                        break;
                    }

                    if ( foundNearerPlanet )
                        continue;
                    planetInCluster.AddLinkTo( otherPlanetInCluster );
                }
            }
        }

        public static void Helper_MakeIntraClusterConnections( IList<Planet> ClusterPlanets, MapClusterStyle ClusterStyle )
        {
            switch ( ClusterStyle )
            {
                #region Simple
                case MapClusterStyle.Simple:
                //case MapClusterStyle.ConcentricWithSimpleLayout:
                case MapClusterStyle.CrosshatchWithSimpleLayout:
                    {
                        Helper_AddConnectionsWithinCluster( ClusterPlanets, 1, false );
                        int lastTotalDisconnectionCount = -1;
                        for ( int loopCount = 0; loopCount < 10 - 0; loopCount++ )
                        {
                            #region Next, find the planet in the cluster connected to the most other planets in the cluster
                            IList<Planet> bestConnectedPlanets = null;
                            IList<Planet> bestDisconnectedPlanets = null;
                            int totalDisconnectionCount = 0;
                            {
                                Planet planetInCluster;
                                Planet otherPlanetInCluster;
                                ThrowawayListCanMemLeak<Planet> planetsConnectedToThisPlanet;
                                ThrowawayListCanMemLeak<Planet> planetsNotConnectedToThisPlanet;
                                for ( int j = 0; j < ClusterPlanets.Count; j++ )
                                {
                                    planetInCluster = ClusterPlanets[j];
                                    planetsConnectedToThisPlanet = new ThrowawayListCanMemLeak<Planet>( 500 );
                                    planetsConnectedToThisPlanet.Add( planetInCluster );
                                    planetsNotConnectedToThisPlanet = new ThrowawayListCanMemLeak<Planet>( 500 );
                                    for ( int k = 0; k < ClusterPlanets.Count; k++ )
                                    {
                                        if ( k == j )
                                            continue;
                                        otherPlanetInCluster = ClusterPlanets[k];
                                        if ( planetInCluster.MapGenOnly_GetIsConnectedByAnyLinksTo( otherPlanetInCluster ) )
                                            planetsConnectedToThisPlanet.Add( otherPlanetInCluster );
                                        else
                                        {
                                            planetsNotConnectedToThisPlanet.Add( otherPlanetInCluster );
                                            totalDisconnectionCount++;
                                        }
                                    }
                                    if ( bestConnectedPlanets == null || planetsConnectedToThisPlanet.Count > bestConnectedPlanets.Count )
                                    {
                                        bestConnectedPlanets = planetsConnectedToThisPlanet;
                                        bestDisconnectedPlanets = planetsNotConnectedToThisPlanet;
                                    }
                                    if ( bestDisconnectedPlanets.Count <= 0 )
                                        break;
                                }
                            }
                            #endregion
                            if ( bestDisconnectedPlanets.Count <= 0 )
                                break;
                            bool suppressCrossoverAvoidance = false;
                            if ( lastTotalDisconnectionCount > 0 && lastTotalDisconnectionCount == totalDisconnectionCount )
                                suppressCrossoverAvoidance = true;
                            lastTotalDisconnectionCount = totalDisconnectionCount;
                            Helper_ConnectPlanetLists( bestConnectedPlanets, bestDisconnectedPlanets, suppressCrossoverAvoidance, false );
                        }
                        Helper_AddConnectionsWithinCluster( ClusterPlanets, 2, false );
                        Helper_AddConnectionsWithinCluster( ClusterPlanets, 3, false );
                    }
                    break;
                #endregion
                #region Concentric
                case MapClusterStyle.Concentric:
                    {
                        ThrowawayListCanMemLeak<Planet> planetsInPreviousLayer = new ThrowawayListCanMemLeak<Planet>( 500 );
                        planetsInPreviousLayer.Add( ClusterPlanets[0] );
                        ArcenPoint clusterCenter = ClusterPlanets[0].GalaxyLocation;
                        int nextLayerIsNoMoreThanThisFarOut = (60 + 30);
                        ThrowawayListCanMemLeak<Planet> planetsInNextLayer = new ThrowawayListCanMemLeak<Planet>( 500 );
                        Planet planet;
                        int distanceToClusterCenter;
                        for ( int i = 1; i < ClusterPlanets.Count; i++ )
                        {
                            planet = ClusterPlanets[i];
                            distanceToClusterCenter = Mat.DistanceBetweenPointsImprecise( planet.GalaxyLocation, clusterCenter );
                            if ( distanceToClusterCenter < nextLayerIsNoMoreThanThisFarOut )
                            {
                                // just another one for the layer
                                planetsInNextLayer.Add( planet );
                                if ( planetsInNextLayer.Count > 1 )
                                    planetsInNextLayer[planetsInNextLayer.Count - 2].AddLinkTo( planetsInNextLayer[planetsInNextLayer.Count - 1] );
                            }
                            else
                            {
                                // starting new layer
                                if ( planetsInNextLayer.Count > 2 )
                                {
                                    if ( Mat.DistanceBetweenPointsImprecise( planetsInNextLayer[planetsInNextLayer.Count - 1].GalaxyLocation,
                                        planetsInNextLayer[0].GalaxyLocation ) < 70 )
                                        planetsInNextLayer[planetsInNextLayer.Count - 1].AddLinkTo( planetsInNextLayer[0] );
                                }
                                Helper_ConnectPlanetLists( planetsInPreviousLayer, planetsInNextLayer, false, true );
                                planetsInPreviousLayer.Clear();
                                planetsInPreviousLayer.AddRange( planetsInNextLayer );
                                planetsInNextLayer.Clear();
                                planetsInNextLayer.Add( planet );
                                nextLayerIsNoMoreThanThisFarOut += 60;
                            }
                        }
                        if ( planetsInNextLayer.Count > 0 )
                        {
                            if ( planetsInNextLayer.Count > 2 )
                            {
                                if ( Mat.DistanceBetweenPointsImprecise( planetsInNextLayer[planetsInNextLayer.Count - 1].GalaxyLocation,
                                    planetsInNextLayer[0].GalaxyLocation ) < 70 )
                                    planetsInNextLayer[planetsInNextLayer.Count - 1].AddLinkTo( planetsInNextLayer[0] );
                            }
                            // connect last layer
                            Helper_ConnectPlanetLists( planetsInPreviousLayer, planetsInNextLayer, false, true );
                            ThrowawayListCanMemLeak<Planet> tempList = new ThrowawayListCanMemLeak<Planet>( 500 );
                            for ( int i = 0; i < planetsInNextLayer.Count; i++ )
                            {
                                planet = planetsInNextLayer[i];
                                if ( planet.GetLinkedNeighborCount() > 1 )
                                    continue;
                                tempList.Clear();
                                tempList.Add( planet );
                                Helper_ConnectPlanetLists( planetsInPreviousLayer, tempList, false, true );
                                tempList.Clear();
                            }

                            planetsInPreviousLayer.Clear();
                            planetsInNextLayer.Clear();
                        }
                    }
                    break;
                #endregion
                #region Crosshatch
                case MapClusterStyle.Crosshatch:
                    {
                        int connectIfDistanceLessThan = 80;
                        Planet planet;
                        Planet otherPlanet;
                        for ( int i = 0; i < ClusterPlanets.Count; i++ )
                        {
                            planet = ClusterPlanets[i];
                            for ( int j = 0; j < ClusterPlanets.Count; j++ )
                            {
                                if ( i == j )
                                    continue;
                                otherPlanet = ClusterPlanets[j];
                                if ( planet.GetIsDirectlyLinkedTo( false, otherPlanet ) )
                                    continue;
                                if ( Mat.DistanceBetweenPointsImprecise( planet.GalaxyLocation, otherPlanet.GalaxyLocation ) > connectIfDistanceLessThan )
                                    continue;
                                planet.AddLinkTo( otherPlanet );
                            }
                        }
                    }
                    break;
                    #endregion
            }
        }

        public static bool Helper_PickIntraClusterPlanetPoints( ArcenHostOnlySimContext Context, IList<ArcenPoint> PlanetPointListToFill, ArcenPoint ClusterCenter, int ClusterRadius, int ClusterPlanetCount, MapClusterStyle ClusterStyle )
        {
            switch ( ClusterStyle )
            {
                #region Simple
                case MapClusterStyle.Simple:
                    {
                        int minimumDistanceBetweenPlanets = 50;

                        if ( ClusterRadius < 120 )
                            minimumDistanceBetweenPlanets = 40;
                        if ( ClusterRadius < 100 )
                            minimumDistanceBetweenPlanets = 25;

                        int numberFailuresAllowed = 1000;
                        for ( int j = 0; j < ClusterPlanetCount; j++ )
                        {
                            ArcenPoint newPlanetPoint = ClusterCenter.GetRandomPointWithinDistance( Context.RandomToUse, 0, ClusterRadius );

                            if ( HelperDoesPointListContainPointWithinDistance( PlanetPointListToFill, newPlanetPoint, minimumDistanceBetweenPlanets ) )
                            {
                                j--;
                                numberFailuresAllowed--;
                                if ( numberFailuresAllowed <= 0 )
                                    return false;
                                continue;
                            }

                            PlanetPointListToFill.Add( newPlanetPoint );
                        }
                    }
                    break;
                #endregion
                #region Concentric
                case MapClusterStyle.Concentric:
                    //case MapClusterStyle.ConcentricWithSimpleLayout:
                    {
                        int minimumDistanceBetweenPlanets = 50;

                        PlanetPointListToFill.Add( ClusterCenter );

                        int distanceFromCenter = 0;
                        while ( PlanetPointListToFill.Count < ClusterPlanetCount )
                        {
                            distanceFromCenter += (minimumDistanceBetweenPlanets + 10);
                            if ( distanceFromCenter > ClusterRadius )
                                return false;
                            AngleDegrees angleToStartAt = AngleDegrees.Create( (float)Context.RandomToUse.Next( 1, 360 ) );
                            for ( int i = 0; i < 360; i++ )
                            {
                                AngleDegrees angle = angleToStartAt.Add( AngleDegrees.Create( (float)i ) );
                                ArcenPoint potentialPlanetPoint = ClusterCenter.GetPointAtAngleAndDistance( angle, distanceFromCenter );
                                if ( HelperDoesPointListContainPointWithinDistance( PlanetPointListToFill, potentialPlanetPoint, minimumDistanceBetweenPlanets ) )
                                    continue;
                                PlanetPointListToFill.Add( potentialPlanetPoint );
                                if ( PlanetPointListToFill.Count >= ClusterPlanetCount )
                                    break;
                            }
                        }
                    }
                    break;
                #endregion
                #region Crosshatch
                case MapClusterStyle.Crosshatch:
                case MapClusterStyle.CrosshatchWithSimpleLayout:
                    {
                        int minimumDistanceBetweenPlanets = 50;

                        int sideLengthInPlanets;
                        if ( ClusterPlanetCount >= 26 )
                            return false;
                        else if ( ClusterPlanetCount >= 17 )
                            sideLengthInPlanets = 5;
                        else if ( ClusterPlanetCount >= 10 )
                            sideLengthInPlanets = 4;
                        else if ( ClusterPlanetCount >= 5 )
                            sideLengthInPlanets = 3;
                        else
                            sideLengthInPlanets = 2;

                        int sideLengthInPixels = sideLengthInPlanets * minimumDistanceBetweenPlanets;

                        ArcenPoint topLeftCorner = ClusterCenter;
                        topLeftCorner.X -= (sideLengthInPixels >> 1);
                        topLeftCorner.Y -= (sideLengthInPixels >> 1);

                        ArcenPoint proposedPoint;
                        for ( int i = 0; i < sideLengthInPlanets; i++ )
                        {
                            for ( int j = 0; j < sideLengthInPlanets; j++ )
                            {
                                proposedPoint = topLeftCorner;
                                proposedPoint.X += (i * minimumDistanceBetweenPlanets);
                                proposedPoint.Y += (j * minimumDistanceBetweenPlanets);
                                PlanetPointListToFill.Add( proposedPoint );
                                if ( PlanetPointListToFill.Count >= ClusterPlanetCount )
                                    break;
                            }
                            if ( PlanetPointListToFill.Count >= ClusterPlanetCount )
                                break;
                        }

                        AngleDegrees rotationAngle = AngleDegrees.Create( (float)(Context.RandomToUse.Next( 10, 20 ) * (Context.RandomToUse.NextBool() ? -1 : 1)) );
                        for ( int i = 0; i < PlanetPointListToFill.Count; i++ )
                        {
                            proposedPoint = PlanetPointListToFill[i];
                            int currentPointDistanceToClusterCenter = Mat.DistanceBetweenPointsImprecise( proposedPoint, ClusterCenter );
                            AngleDegrees currentPointAngle = ClusterCenter.GetAngleToDegrees( proposedPoint ).Add( rotationAngle );
                            ArcenPoint rotatedPoint = ClusterCenter.GetPointAtAngleAndDistance( currentPointAngle, currentPointDistanceToClusterCenter );
                            PlanetPointListToFill[i] = rotatedPoint;
                        }
                    }
                    break;
                    #endregion
            }

            return true;
        }

        internal static void SeedResourceSpot( Planet planet, ArcenHostOnlySimContext Context, Int32 wormholeRadius, GameEntityTypeData resourceSpotData )
        {
            if ( resourceSpotData == null )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( planet.Name + " could not find a valid resource spot typeData to seed!" );
                return;
            }
            //there are cases where we just don't have any factions yet, and that's ok!  Don't seed anything here, then.
            if ( World_AIW2.Instance.Factions.Count == 0 )
                return;
            //if not seeding the details yet, then... skip all this!
            if ( !World_AIW2.Instance.Setup.ShouldSeedDetailsYet )
                return;

            if ( MapgenLogger.IsActive && World_AIW2.Instance.GetIsTutorial() )
                MapgenLogger.Log( planet.Name + " SeedResourceSpot: " + resourceSpotData.DisplayName );

            PlanetFaction owningFaction = planet.GetFirstFactionOfType( FactionType.NaturalObject );

            if ( planet.PopulationType == PlanetPopulationType.HumanHomeworld )
            {
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count && i < World_AIW2.Instance.Setup.FactionConfigurations.Count; i++ )
                {
                    ConfigurationForFaction config = World_AIW2.Instance.Setup.FactionConfigurations[i];
                    if ( config.StartingIndex != planet.Index )
                        continue;
                    Faction faction = World_AIW2.Instance.Factions[i];
                    PlayerTypeData playerType = faction.PlayerTypeDataOrNull_ModeratelyExpensive;
                    if ( ! playerType?.UsesMetal ?? false ) {
                        continue;
                    }
                    owningFaction = planet.GetPlanetFactionForFaction( faction );
                }
            }

            int minimumSeparation = resourceSpotData.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius * 4;

            ArcenPoint spotPoint = ArcenPoint.ZeroZeroPoint;
            bool foundIt = false;
            int outerLoopIterations = 10;
            while ( !foundIt && outerLoopIterations > 0 )
            {
                outerLoopIterations--;
                int rechecksPerOuterLoop = 10;
                while ( !foundIt && rechecksPerOuterLoop > 0 )
                {
                    spotPoint = Mat.GetRandomPointFromCircleCenter( Context.RandomToUse, Engine_AIW2.Instance.CombatCenter, minimumSeparation, wormholeRadius );
                    rechecksPerOuterLoop--;
                    foundIt = true;
                    foreach ( GameEntity_Squad entity in planet.Squads() )
                    {
                        if ( !entity.GetIsWithinRangeOf( spotPoint, minimumSeparation ) )
                            continue;
                        foundIt = false;
                        break;
                    }
                    foreach ( GameEntity_Other entity in planet.Others() )
                    {
                        if ( !entity.GetIsWithinRangeOf( spotPoint, minimumSeparation ) )
                            continue;
                        foundIt = false;
                        break;
                    }
                }
                minimumSeparation = (minimumSeparation * 9) / 10;
            }
            if ( owningFaction == null )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "No faction found for SeedResourceSpot type '" + resourceSpotData.InternalName + "' on planet " + planet.Name + " (" + planet.Index + ")" );
                return;
            }
            //if ( MapgenLogger.IsActive && World_AIW2.Instance.GetIsTutorial() )
            //    MapgenLogger.Log( planet.Name + " SeedResourceSpot: " + owningFaction.Faction.GetDisplayName() + " "  + spotPoint );
            if ( GameEntity_Squad.CreateNew_ReturnNullIfMPClient( owningFaction, resourceSpotData, resourceSpotData.MarkFor( owningFaction ),
                owningFaction.FleetUsedAtPlanet, 0, spotPoint, Context, "Mapgen-ResourceSpot" ) == null )
            {
                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "FAIL TO SEED: " + planet.Name + " SeedResourceSpot: " + owningFaction.Faction.GetDisplayName() + " " + spotPoint );
            }
        }

        public static void SeedWormholesForPlanet( Planet planet, IWormholePlacer wormholePlacer, ArcenHostOnlySimContext Context )
        {
            //there are cases where we just don't have any factions yet, and that's ok!  Don't seed anything here, then.
            if ( World_AIW2.Instance.Factions.Count == 0 )
                return;
            //if not seeding the details yet, then... skip all this!
            if ( !World_AIW2.Instance.Setup.ShouldSeedDetailsYet )
                return;

            //ArcenDebugging.ArcenDebugLog( "SeedWormholesForPlanet from planet " + planet.Index, Verbosity.Chat );

            PlanetFaction faction = planet.GetFirstFactionOfType( FactionType.NaturalObject );
            if ( faction != null )
            {
                if ( planet.GetLinkedNeighborCount() == 0 )
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "No links to any neighbors on planet " + planet.Name + " (" + planet.Index + ")!!" );
                else
                {
                    foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                    {
                        if ( neighbor == null )
                        {
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Error during creation of wormhole to planet null neighbor from planet " + planet.Index );
                            continue;
                        }
                        //ArcenDebugging.ArcenDebugLog( "p1 create wormhole to planet " + neighbor.Index + " from planet " + planet.Index, Verbosity.Chat );
                        try
                        {
                            int largerIndex = Math.Max( planet.Index, neighbor.Index );
                            int smallerIndex = Math.Min( planet.Index, neighbor.Index );
                            int seed = (largerIndex << 16) + smallerIndex;
                            ArcenPoint wormholePoint = wormholePlacer.GetPointForWormhole( Context, planet, neighbor );
                            GameEntity_Other wormhole = GameEntity_Other.CreateOtherNew_CallFromHostOnly( faction, GameEntityTypeDataTable.Instance.DefaultWormholeType, wormholePoint, Context );
                            if ( wormhole != null )
                                wormhole.SetLinkedPlanetIndex( neighbor.Index );
                            //ArcenDebugging.ArcenDebugLog( "create wormhole to planet " + neighbor.Index + " from planet " + planet.Index, Verbosity.Chat );
                        }
                        catch ( Exception e )
                        {
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "p2 Error during creation of wormhole to planet " + neighbor.Index + " from planet " + planet.Index +
                                "\n" + e );
                        }
                    }
                }
            }
            else
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "No naturalObjectFaction found for wormhole placement on planet " + planet.Name + " (" + planet.Index + ")!!" );

            planet.RecomputeDestinationIndexToWormholeMapping();
        }
    }
}
