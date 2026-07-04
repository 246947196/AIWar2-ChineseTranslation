using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public class Mapgen_Honeycomb : IMapGenerator
    {
        public Mapgen_Honeycomb()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_Honeycomb : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            this.InnerGenerate( galaxy, Context, mapConfig, PlanetType.Normal, mapType );
        }

        protected void InnerGenerate( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, PlanetType planetType, MapTypeData mapType )
        {
            int minRings = 0;
            int cellsAtRing = 0;
            int numberToSeed = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            for ( ; minRings < this.maxCellsByRingCount.Length; minRings++ )
            {
                cellsAtRing = this.maxCellsByRingCount[minRings];
                if ( cellsAtRing >= numberToSeed )
                    break;
            }

            bool isSolarSnake = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "SolarSnake" ).RelatedIntValue > 0;
            bool isHoneycomb = !isSolarSnake;

            if ( isHoneycomb )
            {
                int extraCellCount = cellsAtRing - numberToSeed;

                FInt extraCellRatio = ((FInt)extraCellCount / (FInt)numberToSeed);
                if ( extraCellRatio < FInt.FromParts( 0, 100 ) )
                {
                    if ( Context.RandomToUse.Next( 0, 2 ) == 0 )
                        minRings++;
                }
                else if ( extraCellRatio < FInt.FromParts( 0, 200 ) )
                {
                    if ( Context.RandomToUse.Next( 0, 3 ) == 0 )
                        minRings++;
                }
                else if ( extraCellRatio < FInt.FromParts( 0, 300 ) )
                {
                    if ( Context.RandomToUse.Next( 0, 4 ) == 0 )
                        minRings++;
                }
            }

            if ( minRings > 8 )
                minRings = 8;

            int numberOfRows = (int)((float)(minRings * BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "Dissonance" ).RelatedIntValue) / 100.0f) - 1;
            //2) Determine number of points on central horizontal (just rings times 2, minus 1)
            int numberOfColumnsOnCentralRow = numberOfRows;

            ArcenPoint[][] pointRows = new ArcenPoint[numberOfRows][];

            int distanceBetweenPoints = planetType.GetData().InterStellarRadius * 4;

            ArcenRectangle seedingArea;
            seedingArea.Width = numberOfRows * distanceBetweenPoints;
            seedingArea.Height = numberOfRows * distanceBetweenPoints;
            seedingArea.X = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter.X - seedingArea.Width / 2;
            seedingArea.Y = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter.Y - seedingArea.Height / 2;

            //int centerX = seedingArea.CalculateCenterPoint().X;
            int centerY = seedingArea.CalculateCenterPoint().Y;

            int centralRowIndex = minRings - 1;
            pointRows[centralRowIndex] = this.Helper_GetHexagonalCoordinatesForRow( numberOfColumnsOnCentralRow, centerY, distanceBetweenPoints, seedingArea.X );

            int rowY = centerY;
            int numberOfCellsOnRow = numberOfColumnsOnCentralRow;
            FInt offset = FInt.Zero;
            for ( int i = centralRowIndex + 1; i < pointRows.Length; i++ )
            {
                numberOfCellsOnRow -= 1;
                rowY += distanceBetweenPoints;
                offset += FInt.FromParts( 0, 500 );
                pointRows[i] = this.Helper_GetHexagonalCoordinatesForRow( numberOfCellsOnRow, rowY, distanceBetweenPoints, seedingArea.X + (offset * distanceBetweenPoints).IntValue );
            }

            rowY = centerY;
            numberOfCellsOnRow = numberOfColumnsOnCentralRow;
            offset = FInt.Zero;
            for ( int i = centralRowIndex - 1; i >= 0; i-- )
            {
                numberOfCellsOnRow -= 1;
                rowY -= distanceBetweenPoints;
                offset += FInt.FromParts( 0, 500 );
                pointRows[i] = this.Helper_GetHexagonalCoordinatesForRow( numberOfCellsOnRow, rowY, distanceBetweenPoints, seedingArea.X + (offset * distanceBetweenPoints).IntValue );
            }

            // remove excess points
            ArcenPoint[] pointRow;
            ArcenPoint point;
            {
                int totalPoints = 0;
                for ( int i = 0; i < pointRows.Length; i++ )
                    totalPoints += pointRows[i].Length;
                if ( isHoneycomb )
                {
                    int randomRowIndex;
                    int randomCellIndex;
                    while ( totalPoints > numberToSeed )
                    {
                        randomRowIndex = Context.RandomToUse.Next( 0, pointRows.Length );
                        pointRow = pointRows[randomRowIndex];
                        randomCellIndex = Context.RandomToUse.Next( 0, pointRow.Length );
                        point = pointRow[randomCellIndex];
                        if ( point.X == 0 && point.Y == 0 )
                            continue;
                        pointRow[randomCellIndex] = ArcenPoint.ZeroZeroPoint;
                        totalPoints--;
                    }
                }
                else
                {
                    int numberToRemove = totalPoints - numberToSeed;
                    int numberToRemoveFromTop = numberToRemove / 2;
                    int numberToRemoveFromBottom = numberToRemoveFromTop;
                    if ( numberToRemoveFromTop + numberToRemoveFromBottom < numberToRemove )
                        numberToRemoveFromTop++;
                    totalPoints -= this.HoneycombHelper_RemovePointsFromTopOrBottom( pointRows, centralRowIndex, numberToRemoveFromTop, true );
                    totalPoints -= this.HoneycombHelper_RemovePointsFromTopOrBottom( pointRows, centralRowIndex, numberToRemoveFromBottom, false );
                }
            }

            //5) place planets at all points
            Planet[][] planetRows = new Planet[numberOfRows][];
            Planet[] planetRow;
            for ( int rowIndex = 0; rowIndex < pointRows.Length; rowIndex++ )
            {
                pointRow = pointRows[rowIndex];
                planetRows[rowIndex] = planetRow = new Planet[pointRow.Length];
                for ( int columnIndex = 0; columnIndex < pointRow.Length; columnIndex++ )
                {
                    point = pointRow[columnIndex];
                    if ( point.X == 0 && point.Y == 0 )
                        continue;
                    planetRow[columnIndex] = galaxy.AddPlanet( planetType, point,
                        World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                }
            }

            //6) for each row:
            Planet cellPlanet;
            Planet[] nextPlanetRow;
            Planet planetToLink;
            if ( isSolarSnake )
            {
                Planet lastPlanet = null;
                bool goingRight = true;
                for ( int rowIndex = 0; rowIndex < planetRows.Length; rowIndex++ )
                {
                    planetRow = planetRows[rowIndex];
                    if ( goingRight )
                    {
                        for ( int columnIndex = 0; columnIndex < planetRow.Length; columnIndex++ )
                        {
                            cellPlanet = planetRow[columnIndex];
                            if ( cellPlanet == null )
                                continue;
                            if ( lastPlanet != null )
                                lastPlanet.AddLinkTo( cellPlanet );
                            lastPlanet = cellPlanet;
                        }
                    }
                    else
                    {
                        for ( int columnIndex = planetRow.Length - 1; columnIndex >= 0; columnIndex-- )
                        {
                            cellPlanet = planetRow[columnIndex];
                            if ( cellPlanet == null )
                                continue;
                            if ( lastPlanet != null )
                                lastPlanet.AddLinkTo( cellPlanet );
                            lastPlanet = cellPlanet;
                        }
                    }
                    goingRight = !goingRight;
                }
            }
            else
            {
                for ( int rowIndex = 0; rowIndex < planetRows.Length; rowIndex++ )
                {
                    planetRow = planetRows[rowIndex];
                    //-- for each cell:
                    for ( int columnIndex = 0; columnIndex < planetRow.Length; columnIndex++ )
                    {
                        cellPlanet = planetRow[columnIndex];
                        if ( cellPlanet == null )
                            continue;
                        //--- connect to the next cell
                        if ( columnIndex + 1 < planetRow.Length )
                        {
                            planetToLink = planetRow[columnIndex + 1];
                            if ( planetToLink != null )
                                cellPlanet.AddLinkTo( planetToLink );
                        }
                        if ( rowIndex + 1 < planetRows.Length )
                        {
                            nextPlanetRow = planetRows[rowIndex + 1];
                            //--- if row below has a cell with the same index, connect to it
                            if ( columnIndex < nextPlanetRow.Length )
                            {
                                planetToLink = nextPlanetRow[columnIndex];
                                if ( planetToLink != null )
                                    cellPlanet.AddLinkTo( planetToLink );
                            }
                            if ( rowIndex < centralRowIndex )
                            {
                                //--- if row below has a cell with the same index + 1, connect to it
                                if ( columnIndex + 1 < nextPlanetRow.Length )
                                {
                                    planetToLink = nextPlanetRow[columnIndex + 1];
                                    if ( planetToLink != null )
                                        cellPlanet.AddLinkTo( planetToLink );
                                }
                            }
                            else
                            {
                                if ( columnIndex > 0 && columnIndex - 1 < nextPlanetRow.Length )
                                {
                                    planetToLink = nextPlanetRow[columnIndex - 1];
                                    if ( planetToLink != null )
                                        cellPlanet.AddLinkTo( planetToLink );
                                }
                            }
                        }
                    }
                }
            }

            Planet firstPlanet = galaxy.GetFirstNonDestroyedPlanet();

            ThrowawayListCanMemLeak<Planet> planetsNotFoundInCurrentSearch = new ThrowawayListCanMemLeak<Planet>( 500 );
            ThrowawayListCanMemLeak<Planet> planetsFoundInCurrentSearch = new ThrowawayListCanMemLeak<Planet>( 500 );
            bool needToCheck = true;
            Planet planetToCheck;
            int lastUnconnected = -1;
            while ( needToCheck )
            {
                needToCheck = false;
                planetsNotFoundInCurrentSearch.Clear();
                planetsFoundInCurrentSearch.Clear();

                planetsNotFoundInCurrentSearch.AddRange( galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs() );

                planetsFoundInCurrentSearch.Add( firstPlanet );
                planetsNotFoundInCurrentSearch.Remove( firstPlanet );

                for ( int i = 0; i < planetsFoundInCurrentSearch.Count; i++ )
                {
                    planetToCheck = planetsFoundInCurrentSearch[i];
                    foreach ( Planet linkedPlanet in planetToCheck.LinkedNeighbors( false ) )
                    {
                        if ( planetsFoundInCurrentSearch.Contains( linkedPlanet ) )
                            continue;
                        planetsFoundInCurrentSearch.Add( linkedPlanet );
                        planetsNotFoundInCurrentSearch.Remove( linkedPlanet );
                    }
                }

                if ( planetsNotFoundInCurrentSearch.Count > 0 )
                {
                    needToCheck = true;
                    bool suppressCrossoverAvoidance = false;
                    if ( lastUnconnected > 0 && lastUnconnected == planetsNotFoundInCurrentSearch.Count )
                        suppressCrossoverAvoidance = true;
                    lastUnconnected = planetsNotFoundInCurrentSearch.Count;
                    UtilityMethods.Helper_ConnectPlanetLists( planetsFoundInCurrentSearch, planetsNotFoundInCurrentSearch, suppressCrossoverAvoidance, false );
                }
            }

            int randomExtraConnections = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "RandomExtraConnections" ).RelatedIntValue;
            BadgerUtilityMethods.RandomlyConnectXPlanetsWithoutIntersectingOthers( galaxy, randomExtraConnections, 40, 20, Context );

            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, galaxy );
        }

        private int HoneycombHelper_RemovePointsFromTopOrBottom( ArcenPoint[][] pointRows, int centralRowIndex, int numberToRemoveFromTop, bool testingTop )
        {
            int numberRemoved = 0;
            ArcenPoint[] pointRow;
            int rowOffset = 0;
            int columnOffset = 0;
            bool testingLeft = true;
            while ( numberToRemoveFromTop > 0 )
            {
                if ( rowOffset >= centralRowIndex )
                    break;
                int rowIndex = testingTop ? rowOffset : pointRows.Length - 1 - rowOffset;
                pointRow = pointRows[rowIndex];
                int length = pointRow.Length;
                int columnIndex = testingLeft ? columnOffset : length - 1 - columnOffset;
                if ( pointRow[columnIndex] != ArcenPoint.ZeroZeroPoint )
                {
                    pointRow[columnIndex] = ArcenPoint.ZeroZeroPoint;
                    numberToRemoveFromTop--;
                    numberRemoved++;
                }
                if ( testingLeft )
                    testingLeft = false;
                else
                {
                    testingLeft = true;
                    columnOffset++;
                    FInt maxOffset = (FInt)length / 2;
                    if ( columnOffset > maxOffset )
                    {
                        rowOffset++;
                        columnOffset = 0;
                        continue;
                    }
                }
            }
            return numberRemoved;
        }

        private readonly int[] maxCellsByRingCount = new int[] { 0, 1, 7, 19, 37, 61, 91, 127 };
        private ArcenPoint[] Helper_GetHexagonalCoordinatesForRow( int CellCount, int RowHeight, int DistanceBetweenPoints, int StartingX )
        {
            ArcenPoint[] result = new ArcenPoint[CellCount];

            int pointX = StartingX;

            for ( int i = 0; i < CellCount; i++ )
            {
                result[i] = ArcenPoint.Create( pointX, RowHeight );
                pointX += DistanceBetweenPoints;
            }

            return result;
        }
    }

    public class RootLatticeTypeGenerator : IMapGenerator
    {
        public RootLatticeTypeGenerator()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: RootLatticeTypeGenerator : " + galaxy.GetTotalPlanetCount() + " planets at start.  RandLast: " + Context.RandomToUse.GetLastGenerated() );

            int noise = 0;
            int numberOfExtraConnections = 0;
            bool interconnectEverything = false;
            bool doAxialConnections = true;
            bool isMaze = false;
            bool maze_EasyMode = false;
            bool maze_Angles = false;
            int numberToSeed = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            string mapName = mapType.InternalName;
            if ( ArcenStrings.Equals( mapName, "Grid" ) )
            {
                int gridType = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "GridType" ).RelatedIntValue;
                if ( gridType == 1 )
                {
                    //this is Lattice
                    noise = 10;
                    numberOfExtraConnections = numberToSeed / 4;
                }
                else if ( gridType == 2 )
                {
                    //crosshatch
                    interconnectEverything = true;
                }
                else
                {
                    //for the Grid, set nothing extra
                }
            }
            else if ( ArcenStrings.Equals( mapName, "Maze" ) )
            {
                int mazeSettings = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "MazeType" ).RelatedIntValue;
                //MazeSettings is 0 == MazeA, 1 == MazeB and so on
                doAxialConnections = false;
                isMaze = true;
                if ( mazeSettings == 0 )
                { }
                else if ( mazeSettings == 1 )
                {
                    maze_EasyMode = true;
                }
                else if ( mazeSettings == 2 )
                {
                    maze_Angles = true;
                }
                else if ( mazeSettings == 3 )
                {
                    maze_Angles = true;
                    maze_EasyMode = true;
                }
            }
            else
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: MapGeneration: invalid Lattice type  ", Verbosity.DoNotShow );
            }

            int spacing = 450;

            int squareLength = (int)Math.Ceiling( (float)Mat.SqrtFastAnyThread( numberToSeed ) );

            ArcenRectangle seedingRect;
            seedingRect.Width = (squareLength - 1) * spacing;
            seedingRect.Height = seedingRect.Width;
            seedingRect.X = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter.X - (seedingRect.Width / 2);
            seedingRect.Y = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter.Y - (seedingRect.Height / 2);

            Planet[,] planets = new Planet[squareLength, squareLength];
            for ( int spotX = 0; spotX < squareLength; spotX++ )
            {
                for ( int spotY = 0; spotY < squareLength; spotY++ )
                {
                    ArcenPoint nextPoint;
                    nextPoint.X = seedingRect.X + (spotX * spacing);
                    nextPoint.Y = seedingRect.Y + (spotY * spacing);

                    if ( noise > 0 )
                    {
                        nextPoint.X += Context.RandomToUse.Next( -noise, noise );
                        nextPoint.Y += Context.RandomToUse.Next( -noise, noise );
                    }

                    if ( MapgenLogger.IsActive )
                        MapgenLogger.Log( "nextPoint:" + nextPoint + " RandLast: " + Context.RandomToUse.GetLastGenerated() );

                    PlanetType planetType = PlanetType.Normal;

                    Planet nextPlanet = galaxy.AddPlanet( planetType, nextPoint,
                        World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );

                    if ( MapgenLogger.IsActive )
                        MapgenLogger.Log( "next Planet:" + nextPlanet.Name + " RandLast: " + Context.RandomToUse.GetLastGenerated() );

                    planets[spotX, spotY] = nextPlanet;
                    nextPlanet.Mapgen_WorkingGridX = spotX;
                    nextPlanet.Mapgen_WorkingGridY = spotY;
                    if ( doAxialConnections )
                    {
                        if ( spotX > 0 )
                        {
                            nextPlanet.AddLinkTo( planets[spotX - 1, spotY] );
                            if ( interconnectEverything )
                            {
                                if ( spotY > 0 )
                                    nextPlanet.AddLinkTo( planets[spotX - 1, spotY - 1] );
                                if ( spotY < squareLength - 1 )
                                    nextPlanet.AddLinkTo( planets[spotX - 1, spotY + 1] );
                            }
                        }
                        if ( spotY > 0 )
                            nextPlanet.AddLinkTo( planets[spotX, spotY - 1] );
                    }
                }
            }

            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "Before any extra connections. RandLast: " + Context.RandomToUse.GetLastGenerated() );

            if ( numberOfExtraConnections > 0 )
            {
                ThrowawayListCanMemLeak<Planet> planetsWithExtraConnection = new ThrowawayListCanMemLeak<Planet>( 500 );
                for ( int i = 0; i < numberOfExtraConnections; i++ )
                {
                    Planet bestPlanet1 = null;
                    Planet bestPlanet2 = null;
                    int bestDistance = 0;
                    foreach ( Planet planet in galaxy.Planets( false ) )
                    {
                        if ( planetsWithExtraConnection.Contains( planet ) )
                            continue;
                        foreach ( Planet otherPlanet in galaxy.Planets( false ) )
                        {
                            if ( planet == otherPlanet )
                                continue;
                            if ( planetsWithExtraConnection.Contains( otherPlanet ) )
                                continue;
                            if ( planet.GetIsDirectlyLinkedTo( false, otherPlanet ) )
                                continue;
                            int distance = planet.GalaxyLocation.GetDistanceTo( otherPlanet.GalaxyLocation, false );
                            if ( bestPlanet1 != null && distance > bestDistance )
                                continue;
                            bestPlanet1 = planet;
                            bestPlanet2 = otherPlanet;
                            bestDistance = distance;
                        }
                    }
                    if ( bestPlanet1 == null )
                        break;
                    bestPlanet1.AddLinkTo( bestPlanet2 );
                    planetsWithExtraConnection.Add( bestPlanet1 );
                    planetsWithExtraConnection.Add( bestPlanet2 );
                }
            }

            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "After any extra connections. RandLast: " + Context.RandomToUse.GetLastGenerated() );

            if ( isMaze )
                RenderMethodMazeRecursiveBacktracker( Context, galaxy, planets, maze_EasyMode, maze_Angles );

            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "After backtracker. RandLast: " + Context.RandomToUse.GetLastGenerated() );

            int randomExtraConnections = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "RandomExtraConnections" ).RelatedIntValue;
            if ( randomExtraConnections > 0 )
            {
                //First we try to add some random stuff, then we just fill in the connections
                ThrowawayListCanMemLeak<Planet> planetsWithExtraConnection = new ThrowawayListCanMemLeak<Planet>( 500 );
                AddRandomConnections( randomExtraConnections, true, planetsWithExtraConnection, galaxy, Context );
                randomExtraConnections -= planetsWithExtraConnection.Count / 2;
                if ( randomExtraConnections > 0 )
                    AddRandomConnections( randomExtraConnections, false, planetsWithExtraConnection, galaxy, Context );
            }

            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "Before make sure galaxy is fully connected. RandLast: " + Context.RandomToUse.GetLastGenerated() );

            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, galaxy );

            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "After make sure galaxy is fully connected. RandLast: " + Context.RandomToUse.GetLastGenerated() );
        }
        public void AddRandomConnections ( int randomExtraConnections, bool avoidAdjacentConnectionsAndBeMoreRandom, ThrowawayListCanMemLeak<Planet> planetsWithExtraConnection, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            for ( int i = 0; i < randomExtraConnections; i++ )
            {
                Planet bestPlanet1 = null;
                Planet bestPlanet2 = null;
                int bestDistance = 0;
                foreach ( Planet planet in galaxy.Planets( false ) )
                {
                    if ( planetsWithExtraConnection.Contains( planet ) )
                        continue;
                    if ( avoidAdjacentConnectionsAndBeMoreRandom && Context.RandomToUse.Next( 0, 100 ) < 50 )
                        continue;
                    foreach ( Planet otherPlanet in galaxy.Planets( false ) )
                    {
                        if ( planet == otherPlanet )
                            continue;
                        if ( planetsWithExtraConnection.Contains( otherPlanet ) )
                            continue;
                        if ( planet.GetIsDirectlyLinkedTo( false, otherPlanet ) )
                            continue;
                        if ( avoidAdjacentConnectionsAndBeMoreRandom )
                        {
                            bool foundAdjacent = false;
                            foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                            {
                                if ( planetsWithExtraConnection.Contains( neighbor ) )
                                {
                                    foundAdjacent = true;
                                    break;
                                }
                            }
                            if ( foundAdjacent )
                                continue;
                        }
                        int distance = planet.GalaxyLocation.GetDistanceTo( otherPlanet.GalaxyLocation, false );
                        if ( bestPlanet1 != null && distance > bestDistance )
                            continue;
                        if ( bestPlanet1 != null &&
                             avoidAdjacentConnectionsAndBeMoreRandom &&
                             distance == bestDistance && Context.RandomToUse.Next( 0, 100 ) < 20 )
                            continue;

                        bestPlanet1 = planet;
                        bestPlanet2 = otherPlanet;
                        bestDistance = distance;
                    }
                }
                if ( bestPlanet1 == null )
                    break;
                bestPlanet1.AddLinkTo( bestPlanet2 );
                planetsWithExtraConnection.Add( bestPlanet1 );
                planetsWithExtraConnection.Add( bestPlanet2 );
            }
        }
        #region RenderMethodMazeRecursiveBacktracker
        private static void RenderMethodMazeRecursiveBacktracker( ArcenHostOnlySimContext Context, Galaxy galaxy, Planet[,] planets, bool IncludeRandomExtras, bool IncludeAngles )
        {
            int numberPerRowCol = planets.GetLength( 0 );
            bool[,] visited = new bool[numberPerRowCol, numberPerRowCol];

            int startingX = Context.RandomToUse.Next( 0, numberPerRowCol );
            int startingY = Context.RandomToUse.Next( 0, numberPerRowCol );

            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "startingX: " + startingX + " startingY: " + startingY + ". RandLast: " + Context.RandomToUse.GetLastGenerated() );

            RenderMethodMazeRecursiveBacktracker_AddCell( Context, startingX, startingY, planets, visited, numberPerRowCol, null, IncludeAngles );

            if ( IncludeRandomExtras )
            {
                int numberRandomExtras = galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() / 10;
                if ( numberRandomExtras < 1 )
                    numberRandomExtras = 1;

                for ( int i = 1; i < numberRandomExtras; i++ )
                {
                    Planet planet = galaxy.GetRandomPlanet( false, Context );
                    FillWorkingAdjacentPlanetsGrid( planet, planets, visited, numberPerRowCol, true, false, IncludeAngles );

                    if ( MapgenLogger.IsActive )
                        MapgenLogger.Log( "RenderMethod workingPlanets.Count: " + workingPlanets.Count + ". RandLast: " + Context.RandomToUse.GetLastGenerated() );

                    if ( workingPlanets.Count > 0 )
                        planet.AddLinkTo( workingPlanets[Context.RandomToUse.Next( 0, workingPlanets.Count )] );
                    else
                        i--;
                }
            }
        }

        private static void RenderMethodMazeRecursiveBacktracker_AddCell( ArcenHostOnlySimContext Context, int CellX, int CellY,
            Planet[,] planets, bool[,] visited, int numberPerRowCol, Planet PriorPlanet, bool IncludeAngles )
        {
            if ( PriorPlanet != null )
                planets[CellX, CellY].AddLinkTo( PriorPlanet );
            visited[CellX, CellY] = true;
            FillWorkingAdjacentPlanetsGrid( planets[CellX, CellY], planets, visited, numberPerRowCol, false, false, IncludeAngles );

            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "AddCell workingPlanets.Count: " + workingPlanets.Count + ". RandLast: " + Context.RandomToUse.GetLastGenerated() );

            if ( workingPlanets.Count > 0 )
            {
                Planet newPlanet = workingPlanets[Context.RandomToUse.Next( 0, workingPlanets.Count )];
                RenderMethodMazeRecursiveBacktracker_AddCell( Context, newPlanet.Mapgen_WorkingGridX, newPlanet.Mapgen_WorkingGridY, planets,
                    visited, numberPerRowCol, planets[CellX, CellY], IncludeAngles );
                //now we backtrack to this method
                RenderMethodMazeRecursiveBacktracker_AddCell( Context, CellX, CellY, planets, visited, numberPerRowCol, null, IncludeAngles );
            }
        }
        #endregion

        #region FillWorkingAdjacentPlanetsGrid
        private static readonly List<Planet> workingPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "RootLatticeTypeGenerator-workingPlanets" );
        private static void FillWorkingAdjacentPlanetsGrid( Planet planet, Planet[,] planets, bool[,] visited, int numberPerRowCol,
            bool CareAboutLinksInsteadOfVisited, bool WantAlreadyVisited, bool IncludeAngles )
        {
            workingPlanets.Clear();
            if ( planet.Mapgen_WorkingGridX > 0 )
            {
                if ( CareAboutLinksInsteadOfVisited )
                {
                    if ( !planet.GetIsDirectlyLinkedTo( false, planets[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY] ) )
                        workingPlanets.Add( planets[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY] );
                }
                else
                {
                    if ( visited[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY] == WantAlreadyVisited )
                        workingPlanets.Add( planets[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY] );
                }
            }
            if ( planet.Mapgen_WorkingGridY > 0 )
            {
                if ( CareAboutLinksInsteadOfVisited )
                {
                    if ( !planet.GetIsDirectlyLinkedTo( false, planets[planet.Mapgen_WorkingGridX, planet.Mapgen_WorkingGridY - 1] ) )
                        workingPlanets.Add( planets[planet.Mapgen_WorkingGridX, planet.Mapgen_WorkingGridY - 1] );
                }
                else
                {
                    if ( visited[planet.Mapgen_WorkingGridX, planet.Mapgen_WorkingGridY - 1] == WantAlreadyVisited )
                        workingPlanets.Add( planets[planet.Mapgen_WorkingGridX, planet.Mapgen_WorkingGridY - 1] );
                }
            }
            if ( planet.Mapgen_WorkingGridX + 1 < numberPerRowCol )
            {
                if ( CareAboutLinksInsteadOfVisited )
                {
                    if ( !planet.GetIsDirectlyLinkedTo( false, planets[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY] ) )
                        workingPlanets.Add( planets[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY] );
                }
                else
                {
                    if ( visited[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY] == WantAlreadyVisited )
                        workingPlanets.Add( planets[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY] );
                }
            }
            if ( planet.Mapgen_WorkingGridY + 1 < numberPerRowCol )
            {
                if ( CareAboutLinksInsteadOfVisited )
                {
                    if ( !planet.GetIsDirectlyLinkedTo( false, planets[planet.Mapgen_WorkingGridX, planet.Mapgen_WorkingGridY + 1] ) )
                        workingPlanets.Add( planets[planet.Mapgen_WorkingGridX, planet.Mapgen_WorkingGridY + 1] );
                }
                else
                {
                    if ( visited[planet.Mapgen_WorkingGridX, planet.Mapgen_WorkingGridY + 1] == WantAlreadyVisited )
                        workingPlanets.Add( planets[planet.Mapgen_WorkingGridX, planet.Mapgen_WorkingGridY + 1] );
                }
            }

            if ( IncludeAngles )
            {
                if ( planet.Mapgen_WorkingGridX > 0 )
                {
                    if ( planet.Mapgen_WorkingGridY > 0 )
                    {
                        if ( CareAboutLinksInsteadOfVisited )
                        {
                            if ( !planet.GetIsDirectlyLinkedTo( false, planets[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY - 1] ) )
                                workingPlanets.Add( planets[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY - 1] );
                        }
                        else
                        {
                            if ( visited[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY - 1] == WantAlreadyVisited )
                                workingPlanets.Add( planets[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY - 1] );
                        }
                    }
                    if ( planet.Mapgen_WorkingGridY + 1 < numberPerRowCol )
                    {
                        if ( CareAboutLinksInsteadOfVisited )
                        {
                            if ( !planet.GetIsDirectlyLinkedTo( false, planets[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY + 1] ) )
                                workingPlanets.Add( planets[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY + 1] );
                        }
                        else
                        {
                            if ( visited[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY + 1] == WantAlreadyVisited )
                                workingPlanets.Add( planets[planet.Mapgen_WorkingGridX - 1, planet.Mapgen_WorkingGridY + 1] );
                        }
                    }
                }

                if ( planet.Mapgen_WorkingGridX + 1 < numberPerRowCol )
                {
                    if ( planet.Mapgen_WorkingGridY > 0 )
                    {
                        if ( CareAboutLinksInsteadOfVisited )
                        {
                            if ( !planet.GetIsDirectlyLinkedTo( false, planets[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY - 1] ) )
                                workingPlanets.Add( planets[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY - 1] );
                        }
                        else
                        {
                            if ( visited[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY - 1] == WantAlreadyVisited )
                                workingPlanets.Add( planets[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY - 1] );
                        }
                    }
                    if ( planet.Mapgen_WorkingGridY + 1 < numberPerRowCol )
                    {
                        if ( CareAboutLinksInsteadOfVisited )
                        {
                            if ( !planet.GetIsDirectlyLinkedTo( false, planets[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY + 1] ) )
                                workingPlanets.Add( planets[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY + 1] );
                        }
                        else
                        {
                            if ( visited[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY + 1] == WantAlreadyVisited )
                                workingPlanets.Add( planets[planet.Mapgen_WorkingGridX + 1, planet.Mapgen_WorkingGridY + 1] );
                        }
                    }
                }
            }
        }
        #endregion
    }

    public class Mapgen_Concentric : IMapGenerator
    {
        public Mapgen_Concentric()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_Concentric : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            int numberOfRings;
            int numberToSeed = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            {
                if ( numberToSeed <= 10 )
                    numberOfRings = 1;
                else if ( numberToSeed <= 20 )
                    numberOfRings = 2;
                else if ( numberToSeed <= 30 )
                    numberOfRings = 3;
                else if ( numberToSeed <= 40 )
                    numberOfRings = 3;
                else if ( numberToSeed <= 50 )
                    numberOfRings = Context.RandomToUse.NextWithInclusiveUpperBound( 3, 4 );
                else if ( numberToSeed <= 60 )
                    numberOfRings = Context.RandomToUse.NextWithInclusiveUpperBound( 4, 5 );
                else if ( numberToSeed <= 70 )
                    numberOfRings = Context.RandomToUse.NextWithInclusiveUpperBound( 5, 6 );
                else if ( numberToSeed <= 80 )
                    numberOfRings = Context.RandomToUse.NextWithInclusiveUpperBound( 5, 7 );
                else if ( numberToSeed <= 90 )
                    numberOfRings = Context.RandomToUse.NextWithInclusiveUpperBound( 5, 7 );
                else if ( numberToSeed <= 100 )
                    numberOfRings = Context.RandomToUse.NextWithInclusiveUpperBound( 6, 8 );
                else if ( numberToSeed <= 110 )
                    numberOfRings = Context.RandomToUse.NextWithInclusiveUpperBound( 6, 8 );
                else //if ( numberToSeed <= 120 )
                    numberOfRings = Context.RandomToUse.NextWithInclusiveUpperBound( 6, 8 );
            }

            int extraRings = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "ExtraRings" ).RelatedIntValue;
            numberOfRings += extraRings;
            bool isSpiral = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "SpiralSnake" ).RelatedIntValue > 0;

            int ringGapMinimum;
            int ringGapMaximum;

            //if ( numberOfRings >= 7 )
            //{
            //    ringGapMinimum = 38;
            //    ringGapMaximum = 46;
            //}
            //else if ( numberOfRings == 6 )
            //{
            //    ringGapMinimum = 46;
            //    ringGapMaximum = 54;
            //}
            //else if ( numberOfRings == 5 )
            //{
            //    ringGapMinimum = 54;
            //    ringGapMaximum = 62;
            //}
            //else 
            if ( numberOfRings >= 4 )
            {
                ringGapMinimum = 70;
                ringGapMaximum = 85;
            }
            else if ( numberOfRings == 3 )
            {
                ringGapMinimum = 95;
                ringGapMaximum = 110;
            }
            else //if ( numberOfRings <= 2 )
            {
                ringGapMinimum = 120;
                ringGapMaximum = 150;
            }

            // determine number of planets for each ring, totalling NumberOfPlanets - 1
            // basically all rings need at least 8 planets to prevent line crossover,
            // and each ring N has a random chance for up to N+1 extra planets if there are more left
            // after that, if there are still unallocated planets, repeat the random add
            int numberOfPlanetsToAllocate = numberToSeed - 1;
            int[] numberOfPlanetsByRing = new int[numberOfRings];
            {
                for ( int i = 0; i < numberOfRings; i++ )
                {
                    numberOfPlanetsByRing[i] = 8;
                    numberOfPlanetsToAllocate -= numberOfPlanetsByRing[i];
                }

                while ( numberOfPlanetsToAllocate > 0 )
                {
                    for ( int i = numberOfRings - 1; i >= 0; i-- )
                    {
                        if ( numberOfPlanetsToAllocate <= 0 )
                            break;

                        int minimumPlanetsToRandomlyAllocate = 0;
                        int maximumPlanetsToRandomlyAllocate = i + 1;
                        if ( maximumPlanetsToRandomlyAllocate > numberOfPlanetsToAllocate )
                            maximumPlanetsToRandomlyAllocate = numberOfPlanetsToAllocate;

                        int additionalPlanetsToAllocate = Context.RandomToUse.NextWithInclusiveUpperBound( minimumPlanetsToRandomlyAllocate, maximumPlanetsToRandomlyAllocate );
                        numberOfPlanetsByRing[i] += additionalPlanetsToAllocate;
                        numberOfPlanetsToAllocate -= additionalPlanetsToAllocate;
                    }
                }
            }

            ArcenPoint originPlanetPoint = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;
            PlanetType planetType = PlanetType.Normal;
            Planet originPlanet = galaxy.AddPlanet( planetType, originPlanetPoint,
                World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );

            // starting from ring 1 (excludes the origin)
            int ringRadiusFromOrigin = 0;
            ThrowawayListCanMemLeak<Planet>[] planetsPlacedByRingThatAreNotConnectedOutsideTheRing = new ThrowawayListCanMemLeak<Planet>[numberOfRings];
            for ( int i = 0; i < numberOfRings; i++ )
            {
                ringRadiusFromOrigin += Context.RandomToUse.NextWithInclusiveUpperBound( ringGapMinimum, ringGapMaximum );

                planetsPlacedByRingThatAreNotConnectedOutsideTheRing[i] = new ThrowawayListCanMemLeak<Planet>( 500 );
                Planet firstPlanetPlacedOnRing = null;
                Planet lastPlanetPlacedOnRing = null;
                int ringPlanetCount = numberOfPlanetsByRing[i];
                for ( int j = 0; j < ringPlanetCount; j++ )
                {
                    AngleDegrees angle = AngleDegrees.Create( ((float)360 / (float)ringPlanetCount) * (float)j );

                    ArcenPoint pointOnRing = originPlanetPoint.GetPointAtAngleAndDistance( angle, ringRadiusFromOrigin );

                    Planet planet = galaxy.AddPlanet( planetType, pointOnRing,
                        World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );

                    planetsPlacedByRingThatAreNotConnectedOutsideTheRing[i].Add( planet );

                    if ( lastPlanetPlacedOnRing != null )
                        lastPlanetPlacedOnRing.AddLinkTo( planet );

                    if ( firstPlanetPlacedOnRing == null )
                        firstPlanetPlacedOnRing = planet;

                    lastPlanetPlacedOnRing = planet;
                }

                if ( lastPlanetPlacedOnRing != null && firstPlanetPlacedOnRing != null )
                {
                    if ( !isSpiral )
                        lastPlanetPlacedOnRing.AddLinkTo( firstPlanetPlacedOnRing );
                    else
                    {
                        if ( i == 0 )
                            firstPlanetPlacedOnRing.AddLinkTo( originPlanet );
                        else
                        {
                            IList<Planet> previousRing = planetsPlacedByRingThatAreNotConnectedOutsideTheRing[i - 1];
                            firstPlanetPlacedOnRing.AddLinkTo( previousRing[previousRing.Count - 1] );
                        }
                    }
                }
            }

            if ( !isSpiral )
            {
                // starting from the origin planet (ring 0) to the second-to-last-ring, for each ring N
                if ( planetsPlacedByRingThatAreNotConnectedOutsideTheRing.Length > 0 &&
                    planetsPlacedByRingThatAreNotConnectedOutsideTheRing[0] != null &&
                    planetsPlacedByRingThatAreNotConnectedOutsideTheRing[0].Count > 0 )
                {
                    int indexWithinRingOfPlanetToConnectToOrigin;
                    if ( planetsPlacedByRingThatAreNotConnectedOutsideTheRing[0].Count == 1 )
                        indexWithinRingOfPlanetToConnectToOrigin = 0;
                    else
                        indexWithinRingOfPlanetToConnectToOrigin = Context.RandomToUse.Next( 0, planetsPlacedByRingThatAreNotConnectedOutsideTheRing[0].Count );
                    Planet planetToConnectToOrigin = planetsPlacedByRingThatAreNotConnectedOutsideTheRing[0][indexWithinRingOfPlanetToConnectToOrigin];
                    if ( originPlanet != null && planetToConnectToOrigin != null )
                    {
                        originPlanet.AddLinkTo( planetToConnectToOrigin );
                        planetsPlacedByRingThatAreNotConnectedOutsideTheRing[0].Remove( planetToConnectToOrigin );
                    }
                }
                for ( int i = 0; i < numberOfRings - 1; i++ )
                {
                    ThrowawayListCanMemLeak<Planet> eligiblePlanetsOnThisRing = planetsPlacedByRingThatAreNotConnectedOutsideTheRing[i];
                    ThrowawayListCanMemLeak<Planet> eligiblePlanetsOnNextRing = planetsPlacedByRingThatAreNotConnectedOutsideTheRing[i + 1];

                    // determine number of connections to ring N+1;
                    // basically random between 1 and (min(number_of_planets_in_ring_with_no_connections_outside_the_ring,same_for_other_ring) / 3)
                    int minimumConnectionsToNextOutermostRing = 2;
                    int maximumConnectionsToNextOutermostRing = ((eligiblePlanetsOnThisRing.Count * 4) / 10);
                    if ( maximumConnectionsToNextOutermostRing > ((eligiblePlanetsOnNextRing.Count * 4) / 10) )
                        maximumConnectionsToNextOutermostRing = ((eligiblePlanetsOnNextRing.Count * 4) / 10);

                    int numberOfConnectionsToNextOutermostRing;
                    if ( minimumConnectionsToNextOutermostRing >= maximumConnectionsToNextOutermostRing )
                        numberOfConnectionsToNextOutermostRing = minimumConnectionsToNextOutermostRing;
                    else
                        numberOfConnectionsToNextOutermostRing = Context.RandomToUse.NextWithInclusiveUpperBound( minimumConnectionsToNextOutermostRing, maximumConnectionsToNextOutermostRing );

                    for ( int j = 0; j < numberOfConnectionsToNextOutermostRing; j++ )
                    {
                        if ( eligiblePlanetsOnThisRing.Count == 0 )
                            break;

                        Planet planetOnThisRing;
                        if ( eligiblePlanetsOnThisRing.Count == 1 )
                        {
                            planetOnThisRing = eligiblePlanetsOnThisRing[0];
                        }
                        else
                        {
                            // pick random planet p1 on ring N that doesn't have any connections outside the ring
                            planetOnThisRing = eligiblePlanetsOnThisRing
                                [Context.RandomToUse.Next( 0, eligiblePlanetsOnThisRing.Count )];
                        }

                        // pick planet p2 on ring N+1 that is closest to p1
                        Planet closestPlanetOnNextRing = null;
                        int currentBestDistance = 0;
                        for ( int k = 0; k < eligiblePlanetsOnNextRing.Count; k++ )
                        {
                            Planet planetOnNextRing = eligiblePlanetsOnNextRing[k];
                            int thisDistance = Mat.ApproxDistanceBetweenPointsFast( planetOnThisRing.GalaxyLocation, planetOnNextRing.GalaxyLocation, -1 );
                            if ( closestPlanetOnNextRing == null || thisDistance < currentBestDistance )
                            {
                                closestPlanetOnNextRing = planetOnNextRing;
                                currentBestDistance = thisDistance;
                            }
                        }

                        if ( closestPlanetOnNextRing != null )
                        {
                            // connect p1 to p2
                            planetOnThisRing.AddLinkTo( closestPlanetOnNextRing );

                            planetsPlacedByRingThatAreNotConnectedOutsideTheRing[i].Remove( planetOnThisRing );
                            eligiblePlanetsOnThisRing.Remove( planetOnThisRing );

                            planetsPlacedByRingThatAreNotConnectedOutsideTheRing[i + 1].Remove( closestPlanetOnNextRing );
                            eligiblePlanetsOnNextRing.Remove( closestPlanetOnNextRing );
                        }
                    }
                }
            }

            int randomExtraConnections = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "RandomExtraConnections" ).RelatedIntValue;
            BadgerUtilityMethods.RandomlyConnectXPlanetsWithoutIntersectingOthers( galaxy, randomExtraConnections, 40, 20, Context );
        }
    }

    public class Mapgen_X : IMapGenerator
    {
        public Mapgen_X()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        private int numPlanetTotalToHave;
        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_X : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            bool goForPerfectSymmetry = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "GoForPerfectSymmetry" ).RelatedIntValue > 0;

            AngleDegrees startingAngleOffset;
            if ( goForPerfectSymmetry )
                startingAngleOffset = AngleDegrees.Create( (float)(-45) );
            else
            {
                switch ( Context.RandomToUse.Next( 0, 4 ) )
                {
                    case 0:
                        startingAngleOffset = AngleDegrees.Create( (float)(-45) );
                        break;
                    case 1:
                        startingAngleOffset = AngleDegrees.Create( (float)(45) );
                        break;
                    case 2:
                        startingAngleOffset = AngleDegrees.Create( (float)(135) );
                        break;
                    default:
                        startingAngleOffset = AngleDegrees.Create( (float)(225) );
                        break;
                }
            }

            FInt distanceToChildren;
            int numPlanets = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            numPlanetTotalToHave = numPlanets;
            if ( numPlanets < 80 )
                distanceToChildren = FInt.FromParts( 400, 000 );
            else if ( numPlanets < 120 )
                distanceToChildren = FInt.FromParts( 500, 000 );
            else if ( numPlanets > 150 )
                distanceToChildren = FInt.FromParts( 550, 000 );
            else if ( numPlanets > 250 )
                distanceToChildren = FInt.FromParts( 600, 000 );
            else
                distanceToChildren = FInt.FromParts( 700, 000 );
            recursionDepth = 0;
            this.InnerGenerate( galaxy, Context, numPlanets, mapType, Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter, (FInt)distanceToChildren, startingAngleOffset, null, goForPerfectSymmetry );

            int randomExtraConnections = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "RandomExtraConnections" ).RelatedIntValue;
            BadgerUtilityMethods.RandomlyConnectXPlanetsWithoutIntersectingOthers( galaxy, randomExtraConnections, 40, 20, Context );

            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, galaxy );
        }

        static int recursionDepth = 0;
        private void InnerGenerate( Galaxy galaxy, ArcenHostOnlySimContext Context, int sizeOfSubTree, MapTypeData mapType, ArcenPoint CenterOfSubTree, FInt distanceToChildren,
            AngleDegrees startingAngleOffset, Planet Parent, bool goForPerfectSymmetry )
        {
            PlanetType planetType = PlanetType.Normal;
            int planetsToPlace = sizeOfSubTree;

            if ( galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() >= numPlanetTotalToHave && !goForPerfectSymmetry )
                return;

            Planet center = galaxy.AddPlanet( planetType, CenterOfSubTree,
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
            planetsToPlace--;

            if ( Parent != null )
                Parent.AddLinkTo( center );

            if ( planetsToPlace <= 0 )
                return;

            AngleDegrees startingAngleOffsetForChildren = startingAngleOffset.Add( AngleDegrees.Create( (float)Context.RandomToUse.Next( 40, 50 ) ) );
            //FInt distanceToChildrenForChildren = distanceToChildren / FInt.FromParts( 2, 250 );
            FInt distanceToChildrenForChildren = distanceToChildren / FInt.FromParts( 2, 150 );

            int defaultNumChildren = 4;
            if ( !goForPerfectSymmetry )
            {
                int rand = Context.RandomToUse.Next( 0, 100 );
                if ( recursionDepth >= 2 && rand < 40 )
                {
                    //sometimes only do 3 instead of 4 spokes. Just to add a bit of visual variety
                    defaultNumChildren--;
                }
            }

            int numberOfChildren = Math.Min( defaultNumChildren, planetsToPlace );
            AngleDegrees degreesBetweenChildren = AngleDegrees.Create( ((float)360) / numberOfChildren );
            int nodesPerChildTree = planetsToPlace / numberOfChildren;
            int extraNodesForChildTrees = planetsToPlace % numberOfChildren;
            if ( goForPerfectSymmetry && recursionDepth < 1 )
            {
                // We want the main branches of a symmetrical map to have the
                // same number of planets, so if the desired number of planets
                // doesn't divide equally, round them up.
                if ( extraNodesForChildTrees > 0 )
                {
                    nodesPerChildTree += 1;
                    extraNodesForChildTrees = 0;
                }
            }

            AngleDegrees angleToNextChild = startingAngleOffset;
            if ( numberOfChildren == 3 )
                angleToNextChild -= 25;
            for ( int i = 0; i < numberOfChildren; i++ )
            {
                int nodesForThisSubTree = nodesPerChildTree;
                Planet extra = null;
                ArcenPoint childPoint;
                //put a bit of visual wobble into the longer lines for aesthetics
                if ( recursionDepth < 2 )
                    childPoint = CenterOfSubTree.GetPointAtAngleAndDistance( angleToNextChild + Context.RandomToUse.Next( -10, 10 ), distanceToChildren.IntValue );
                else
                    childPoint = CenterOfSubTree.GetPointAtAngleAndDistance( angleToNextChild, distanceToChildren.IntValue );
                if ( recursionDepth < 2 )
                {
                    if ( galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() >= numPlanetTotalToHave && !goForPerfectSymmetry )
                        return;

                    //Add some extra hops to the inner planets
                    extra = galaxy.AddPlanet( planetType, childPoint,
                        World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                    extra.AddLinkTo( center );
                    childPoint = CenterOfSubTree.GetPointAtAngleAndDistance( angleToNextChild, distanceToChildren.IntValue * 2 );
                    planetsToPlace--;
                    nodesForThisSubTree--;
                }

                // If the planets don't divide evenly into our children, we
                // want to distribute those extra nodes evenly among the
                // subtrees.
                if ( extraNodesForChildTrees > 0 )
                {
                    nodesForThisSubTree++;
                    extraNodesForChildTrees--;
                }

                recursionDepth++;
                Planet planetToPass = center;
                if ( extra != null )
                    planetToPass = extra; //this is to make sure the extra planet we've added in will be linked correctly
                this.InnerGenerate( galaxy, Context, nodesForThisSubTree, mapType, childPoint, distanceToChildrenForChildren, startingAngleOffsetForChildren, planetToPass, goForPerfectSymmetry );
                angleToNextChild += degreesBetweenChildren;
                recursionDepth--;
            }
        }
    }

    public class Mapgen_ClustersRoot : IMapGenerator
    {
        public Mapgen_ClustersRoot()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        bool UseSimpleForClusterInternalLayout = true;
        int clusterRadius = 200;
        int clusterSpacing = 50;
        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_ClustersRoot : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            clusterRadius = 200;
            clusterSpacing = 50;

            //string mapName = mapType.InternalName;
            //int numberToSeed = mapConfig.NumberOfPlanets;
            if ( ArcenStrings.Equals( mapType.InternalName, "ClustersMicrocosm" ) )
            {
                //clustersMicrocosm
                UseSimpleForClusterInternalLayout = false;
                clusterRadius = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "ClusterRadius" ).RelatedIntValue;
                clusterSpacing = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "ClustersSpacing" ).RelatedIntValue;
                int triesLeft = 100;
                while ( triesLeft > 0 )
                {
                    triesLeft--;
                    if ( this.InnerGenerate( galaxy, Context, mapConfig, mapType ) )
                        break;
                }

                int randomExtraConnections = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "RandomExtraConnections" ).RelatedIntValue;
                BadgerUtilityMethods.RandomlyConnectXPlanetsWithoutIntersectingOthers( galaxy, randomExtraConnections, 40, 20, Context );

                BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, galaxy );

                return;
            }
            else
            {
                ClusterLinkAlgorithm clusterSettings = (ClusterLinkAlgorithm)BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "ClusterStyle" ).RelatedIntValue;
                //clusterSettings of "0" means 'few clusters', aka use the default code
                //catch all the other cases too, just in case

                if ( clusterSettings == ClusterLinkAlgorithm.Large ||
                    clusterSettings == ClusterLinkAlgorithm.Length )
                {
                    Mapgen_Nebula badgerClusters = new Mapgen_Nebula();
                    badgerClusters.isAsteroid = false;
                    badgerClusters.addSomeExtraLinks = true;
                    badgerClusters.numClustersHint = 4;
                    badgerClusters.nebulaConnectivity = 4;
                    badgerClusters.GenerateMapStructureOnly( galaxy, Context, mapConfig, mapType );

                    // UseSimpleForClusterInternalLayout = true;
                    // int triesLeft = 100;
                    // while ( triesLeft > 0 )
                    // {
                    //     triesLeft--;
                    //     if ( this.InnerGenerate( galaxy, Context, mapConfig, mapType ) )
                    //         return;
                    // }
                }
                else if ( clusterSettings == ClusterLinkAlgorithm.Medium ) //medium clusters
                {
                    //this is in Badger's code
                    Mapgen_Nebula badgerClusters = new Mapgen_Nebula();
                    badgerClusters.isAsteroid = true;
                    badgerClusters.addSomeExtraLinks = true;
                    badgerClusters.numClustersHint = 2;
                    badgerClusters.nebulaConnectivity = 3;
                    badgerClusters.GenerateMapStructureOnly( galaxy, Context, mapConfig, mapType );
                }
                else if ( clusterSettings == ClusterLinkAlgorithm.Nebula ) //medium clusters
                {
                    //this is in Badger's code
                    Mapgen_Nebula badgerClusters = new Mapgen_Nebula();
                    badgerClusters.isAsteroid = false;
                    badgerClusters.addSomeExtraLinks = true;
                    badgerClusters.numClustersHint = 2;
                    badgerClusters.nebulaConnectivity = 3;
                    badgerClusters.GenerateMapStructureOnly( galaxy, Context, mapConfig, mapType );
                }
                else if ( clusterSettings == ClusterLinkAlgorithm.Small )
                {
                    Mapgen_Nebula badgerClusters = new Mapgen_Nebula();
                    badgerClusters.isAsteroid = true;
                    badgerClusters.addSomeExtraLinks = true;
                    badgerClusters.numClustersHint = 3;
                    badgerClusters.nebulaConnectivity = 3;
                    badgerClusters.GenerateMapStructureOnly( galaxy, Context, mapConfig, mapType );
                }
                else if ( clusterSettings == ClusterLinkAlgorithm.Butterfly )
                {
                    Mapgen_Nebula badgerClusters = new Mapgen_Nebula();
                    badgerClusters.isAsteroid = false;
                    badgerClusters.addSomeExtraLinks = true;
                    badgerClusters.numClustersHint = 1;
                    badgerClusters.nebulaConnectivity = 3;
                    badgerClusters.GenerateMapStructureOnly( galaxy, Context, mapConfig, mapType );
                }
                else if ( clusterSettings == ClusterLinkAlgorithm.Fractured )
                {
                    Mapgen_Nebula badgerClusters = new Mapgen_Nebula();
                    badgerClusters.isAsteroid = false;
                    badgerClusters.addSomeExtraLinks = true;
                    badgerClusters.numClustersHint = 3;
                    badgerClusters.nebulaConnectivity = 3;
                    badgerClusters.GenerateMapStructureOnly( galaxy, Context, mapConfig, mapType );
                }
                // else if(clusterSettings == ClusterLinkAlgorithm.Unnamed3)
                //   {
                //     Mapgen_Nebula badgerClusters = new Mapgen_Nebula();
                //     badgerClusters.isAsteroid = false;
                //     badgerClusters.addSomeExtraLinks = true;
                //     badgerClusters.numClustersHint = 4;
                //     badgerClusters.nebulaConnectivity = 4;
                //     badgerClusters.GenerateMapStructureOnly(galaxy, Context, mapConfig, mapType);
                //  }
                else
                {
                    throw new Exception( "Unknown cluster setting " + clusterSettings );
                }

                int randomExtraConnections = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "RandomExtraConnections" ).RelatedIntValue;
                BadgerUtilityMethods.RandomlyConnectXPlanetsWithoutIntersectingOthers( galaxy, randomExtraConnections, 40, 20, Context );

                BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, galaxy );
            } //end not-the-microcosm
        }

        private bool InnerGenerate( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            int numberToSeed = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            //string mapName = mapType.InternalName;
            bool iterationSucceeded = false;
            ThrowawayListCanMemLeak<ArcenPoint> clusterCenters = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            ThrowawayListCanMemLeak<MapClusterStyle> clusterStyles = new ThrowawayListCanMemLeak<MapClusterStyle>( 20 );
            ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<ArcenPoint>> planetPointsByCluster = new ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<ArcenPoint>>( 300 );
            PlanetType planetType = PlanetType.Normal;
            while ( !iterationSucceeded )
            {
                iterationSucceeded = true;
                #region determine number of clusters
                int numberOfClusters;
                {
                    if ( numberToSeed <= 10 )
                        numberOfClusters = 2;
                    else if ( numberToSeed <= 20 )
                        numberOfClusters = 2;
                    else if ( numberToSeed <= 30 )
                        numberOfClusters = Context.RandomToUse.NextWithInclusiveUpperBound( 2, 3 );
                    else if ( numberToSeed <= 40 )
                        numberOfClusters = Context.RandomToUse.NextWithInclusiveUpperBound( 2, 3 );
                    else if ( numberToSeed <= 50 )
                        numberOfClusters = Context.RandomToUse.NextWithInclusiveUpperBound( 3, 4 );
                    else if ( numberToSeed <= 60 )
                        numberOfClusters = Context.RandomToUse.NextWithInclusiveUpperBound( 3, 4 );
                    else if ( numberToSeed <= 70 )
                        numberOfClusters = Context.RandomToUse.NextWithInclusiveUpperBound( 4, 5 );
                    else if ( numberToSeed <= 80 )
                        numberOfClusters = Context.RandomToUse.NextWithInclusiveUpperBound( 4, 6 );
                    else if ( numberToSeed <= 90 )
                        numberOfClusters = Context.RandomToUse.NextWithInclusiveUpperBound( 5, 6 );
                    else if ( numberToSeed <= 100 )
                        numberOfClusters = Context.RandomToUse.NextWithInclusiveUpperBound( 5, 6 );
                    else if ( numberToSeed <= 110 )
                        numberOfClusters = 6;
                    else //if ( numberToSeed <= 120 )
                        numberOfClusters = 6;
                }
                #endregion

                int clustersWide = 3;
                int clustersHigh = 2;

                ArcenRectangle overallArea;
                overallArea.Width = (clusterRadius * 2 * clustersWide) + (clusterSpacing * (clustersWide - 1));
                overallArea.Height = (clusterRadius * 2 * clustersHigh) + (clusterSpacing * (clustersHigh - 1));
                overallArea.X = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter.X - overallArea.Width / 2;
                overallArea.Y = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter.Y - overallArea.Height / 2;

                clusterCenters.Clear();
                ArcenPoint galacticCenterPoint;
                #region Pick points for each cluster
                {
                    ArcenPoint topLeftPossiblePoint = ArcenPoint.Create(
                        overallArea.X + clusterRadius,
                        overallArea.Y + clusterRadius );

                    ArcenPoint bottomRightPossiblePoint = ArcenPoint.Create(
                        overallArea.Right - clusterRadius,
                        overallArea.Bottom - clusterRadius );

                    ThrowawayListCanMemLeak<ArcenPoint> possibleClusterSpots = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
                    possibleClusterSpots.Add( topLeftPossiblePoint );
                    possibleClusterSpots.Add( ArcenPoint.Create(
                        topLeftPossiblePoint.X,
                        bottomRightPossiblePoint.Y ) );
                    possibleClusterSpots.Add( ArcenPoint.Create(
                        bottomRightPossiblePoint.X,
                        topLeftPossiblePoint.Y ) );
                    possibleClusterSpots.Add( bottomRightPossiblePoint );

                    galacticCenterPoint = ArcenPoint.Create(
                        (topLeftPossiblePoint.X + bottomRightPossiblePoint.X) / 2,
                        (topLeftPossiblePoint.Y + bottomRightPossiblePoint.Y) / 2 );

                    possibleClusterSpots.Add( ArcenPoint.Create(
                        galacticCenterPoint.X,
                        topLeftPossiblePoint.Y ) );

                    possibleClusterSpots.Add( ArcenPoint.Create(
                        galacticCenterPoint.X,
                        bottomRightPossiblePoint.Y ) );

                    ArcenArrays.Randomize<ArcenPoint>( possibleClusterSpots, Context.RandomToUse, 3 );

                    //int numberFailuresAllowed = 1000;
                    ArcenPoint newClusterCenter;
                    for ( int i = 0; i < numberOfClusters; i++ )
                    {
                        newClusterCenter = possibleClusterSpots[i];
                        //if ( this.HelperDoesPointListContainPointWithinDistance( clusterCenters, newClusterCenter, minimumDistanceBetweenClusters ) )
                        //{
                        //    i--;
                        //    numberFailuresAllowed--;
                        //    if ( numberFailuresAllowed <= 0 )
                        //    {
                        //        return false;
                        //    }
                        //    continue;
                        //}

                        clusterCenters.Add( newClusterCenter );
                    }
                }
                #endregion

                clusterStyles.Clear();
                planetPointsByCluster.Clear();
                #region For each cluster, pick points for planets
                {
                    int averagenumberToSeedPerCluster = numberToSeed / clusterCenters.Count;
                    int remainder = numberToSeed - (averagenumberToSeedPerCluster * clusterCenters.Count);
                    if ( remainder < 0 )
                        remainder = 0;

                    int maxAllowedPlanetsPerCluster = (averagenumberToSeedPerCluster * 4) / 3;
                    if ( maxAllowedPlanetsPerCluster > 20 )
                        maxAllowedPlanetsPerCluster = 20;

                    int maxAdditionalPlanetsPerCluster = maxAllowedPlanetsPerCluster - averagenumberToSeedPerCluster;
                    int additionalPlanetsUsedByLastCluster = 0;

                    for ( int i = 0; i < clusterCenters.Count; i++ )
                    {
                        ArcenPoint clusterCenter = clusterCenters[i];

                        planetPointsByCluster.Add( new ThrowawayListCanMemLeak<ArcenPoint>( 300 ) );

                        int numberToSeedForCluster = averagenumberToSeedPerCluster;
                        if ( additionalPlanetsUsedByLastCluster > 0 )
                        {
                            numberToSeedForCluster -= additionalPlanetsUsedByLastCluster;
                            additionalPlanetsUsedByLastCluster = 0;
                        }
                        else if ( i < (clusterCenters.Count - 1) && maxAdditionalPlanetsPerCluster > 0 )
                        {
                            additionalPlanetsUsedByLastCluster = Context.RandomToUse.NextWithInclusiveUpperBound( 0, maxAdditionalPlanetsPerCluster );
                            numberToSeedForCluster += additionalPlanetsUsedByLastCluster;
                        }

                        if ( i == (clusterCenters.Count - 1) )
                        {
                            numberToSeedForCluster += remainder;
                        }

                        MapClusterStyle clusterStyle;
                        if ( UseSimpleForClusterInternalLayout )
                            clusterStyle = MapClusterStyle.Simple;
                        else
                            clusterStyle = (MapClusterStyle)Context.RandomToUse.Next( (int)MapClusterStyle.None + 1, (int)MapClusterStyle.Length );
                        clusterStyles.Add( clusterStyle );

                        if ( !UtilityMethods.Helper_PickIntraClusterPlanetPoints( Context, planetPointsByCluster[i], clusterCenter, clusterRadius, numberToSeedForCluster, clusterStyle ) )
                        {
                            iterationSucceeded = false;
                            break;
                        }
                    }
                }
                if ( !iterationSucceeded )
                    break;
                #endregion
            }
            if ( !iterationSucceeded )
                return false;

            // CANNOT early-out from this point on, MUST generate a usable map

            ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<Planet>> planetsByCluster = new ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<Planet>>( 300 );
            #region For each cluster, populate with planets
            {
                int totalPlanetsPlaced = 0;
                IList<ArcenPoint> planetPointList;
                MapClusterStyle clusterStyle;
                for ( int i = 0; i < planetPointsByCluster.Count; i++ )
                {
                    planetPointList = planetPointsByCluster[i];
                    planetsByCluster.Add( new ThrowawayListCanMemLeak<Planet>( 500 ) );
                    #region Place Planets
                    {
                        for ( int j = 0; j < planetPointList.Count; j++ )
                        {
                            planetsByCluster[i].Add( galaxy.AddPlanet( planetType, planetPointList[j],
                                World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) ) );
                            totalPlanetsPlaced++;
                        }
                    }
                    #endregion
                    #region Add intra-cluster connections
                    clusterStyle = clusterStyles[i];
                    UtilityMethods.Helper_MakeIntraClusterConnections( planetsByCluster[i], clusterStyle );
                    #endregion
                }
            }
            #endregion

            #region For each cluster, pick up to 3 connections to other nearby clusters
            {
                ThrowawayListCanMemLeak<Pair<int, int>> linkedClusterIndices = new ThrowawayListCanMemLeak<Pair<int, int>>( 300 );
                for ( int i = 0; i < clusterCenters.Count; i++ )
                {
                    ArcenPoint clusterCenter = clusterCenters[i];
                    ThrowawayListCanMemLeak<Planet> clusterPlanetList = planetsByCluster[i];

                    int closestClusterIndex = -1;
                    int distanceToClosestCluster = 0;
                    int secondClosestClusterIndex = -1;
                    int distanceToSecondClosestCluster = 0;

                    for ( int j = 0; j < clusterCenters.Count; j++ )
                    {
                        if ( i == j )
                            continue;
                        if ( UtilityMethods.Helper_GetDoesPairListContainPairInEitherDirection( linkedClusterIndices, i, j ) )
                            continue;
                        ArcenPoint otherClusterCenter = clusterCenters[j];
                        //List<Planet> otherClusterPlanetList = planetsByCluster[j];
                        int distanceToOtherCluster = Mat.DistanceBetweenPointsImprecise( clusterCenter, otherClusterCenter );
                        bool foundHit = false;
                        for ( int k = 0; k < clusterCenters.Count; k++ )
                        {
                            if ( k == i || k == j )
                                continue;
                            ArcenPoint thirdClusterCenter = clusterCenters[k];
                            IList<Planet> thirdClusterPlanets = planetsByCluster[k];
                            for ( int Index = 0; Index < thirdClusterPlanets.Count; Index++ )
                            {
                                Planet planet = thirdClusterPlanets[Index];
                                foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                                {
                                    if ( !Mat.LineSegmentIntersectsLineSegment( clusterCenter, otherClusterCenter, planet.GalaxyLocation, neighbor.GalaxyLocation, planet.TypeData.IntraStellarRadius ) )
                                        continue;
                                    foundHit = true;
                                    break;
                                }
                                if ( foundHit )
                                    break;
                            }
                            if ( foundHit )
                                break;
                        }
                        if ( foundHit )
                            continue;
                        if ( closestClusterIndex == -1 || distanceToOtherCluster < distanceToClosestCluster )
                        {
                            closestClusterIndex = j;
                            distanceToClosestCluster = distanceToOtherCluster;
                        }
                        else if ( secondClosestClusterIndex == -1 || distanceToOtherCluster < distanceToSecondClosestCluster )
                        {
                            secondClosestClusterIndex = j;
                            distanceToSecondClosestCluster = distanceToOtherCluster;
                        }
                    }

                    if ( UtilityMethods.Helper_GetNumberOfPairsInPairListInvolving( linkedClusterIndices, i ) < 3 &&
                         closestClusterIndex != -1 &&
                         (UtilityMethods.Helper_GetNumberOfPairsInPairListInvolving( linkedClusterIndices, i ) < 2 ||
                           UtilityMethods.Helper_GetNumberOfPairsInPairListInvolving( linkedClusterIndices, closestClusterIndex ) < 3) )
                    {
                        UtilityMethods.Helper_ConnectPlanetLists( clusterPlanetList, planetsByCluster[closestClusterIndex], false, true );
                        linkedClusterIndices.Add( Pair<int, int>.Create( i, closestClusterIndex ) );
                    }
                    if ( UtilityMethods.Helper_GetNumberOfPairsInPairListInvolving( linkedClusterIndices, i ) < 2 &&
                         secondClosestClusterIndex != -1 &&
                         UtilityMethods.Helper_GetNumberOfPairsInPairListInvolving( linkedClusterIndices, secondClosestClusterIndex ) < 2 )
                    {
                        UtilityMethods.Helper_ConnectPlanetLists( clusterPlanetList, planetsByCluster[secondClosestClusterIndex], false, true );
                        linkedClusterIndices.Add( Pair<int, int>.Create( i, secondClosestClusterIndex ) );
                    }
                }
            }
            #endregion
            return true;
        }
    }

    public class Mapgen_Wheel : IMapGenerator
    {
        //Set immediately before the planet-link sorts so the comparisons can be non-capturing static
        //delegates.  [ThreadStatic] for safety since map generation may run off the main thread.
        [ThreadStatic] private static Planet cb_wheelSpokeEndPlanet;
        [ThreadStatic] private static Planet cb_wheelEmergencyPlanet;
        public Mapgen_Wheel()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_Wheel : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            //1) Place Center
            int numberToSeed = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            ArcenPoint originPlanetPoint = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;
            PlanetType planetType = PlanetType.Normal;
            Planet originPlanet = galaxy.AddPlanet( planetType, originPlanetPoint,
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );

            //2) Pick Number Of Spokes from 3 to 8
            //3) Pick Radius from 130 to 250
            int numberOfSpokes;
            int radius;
            {
                if ( numberToSeed <= 10 )
                {
                    radius = 300;
                }
                else if ( numberToSeed <= 20 )
                {
                    radius = 310;
                }
                else if ( numberToSeed <= 30 )
                {
                    radius = 320;
                }
                else if ( numberToSeed <= 40 )
                {
                    radius = 330;
                }
                else if ( numberToSeed <= 50 )
                {
                    radius = 340;
                }
                else if ( numberToSeed <= 60 )
                {
                    radius = 360;
                }
                else if ( numberToSeed <= 70 )
                {
                    radius = 380;
                }
                else if ( numberToSeed <= 80 )
                {
                    radius = 400;
                }
                else if ( numberToSeed <= 90 )
                {
                    radius = 420;
                }
                else if ( numberToSeed <= 100 )
                {
                    radius = 440;
                }
                else if ( numberToSeed <= 110 )
                {
                    radius = 460;
                }
                else //if ( numberToSeed <= 120 )
                {
                    radius = 500;
                }
            }

            FInt roughNumberOfPlanetsToExpectOnRing = ((FInt)radius * 6) / 40;
            FInt targetNumberOfPlanetsForSpokes = (numberToSeed - 1) - roughNumberOfPlanetsToExpectOnRing;
            if ( targetNumberOfPlanetsForSpokes < (numberToSeed / 2) )
                targetNumberOfPlanetsForSpokes = (FInt)numberToSeed / 2;

            FInt minimumNumberOfSpokes = targetNumberOfPlanetsForSpokes / 9;
            FInt maximumNumberOfSpokes = targetNumberOfPlanetsForSpokes / 5;
            if ( minimumNumberOfSpokes > 7 )
                numberOfSpokes = 8;
            else
            {
                int minimum = minimumNumberOfSpokes.GetNearestIntPreferringHigher();
                if ( minimum < 4 )
                    minimum = 4;
                int maximum = maximumNumberOfSpokes.GetNearestIntPreferringLower();
                if ( maximum > 8 )
                    maximum = 8;
                if ( minimum > maximum )
                    numberOfSpokes = minimum;
                else
                    numberOfSpokes = Context.RandomToUse.NextWithInclusiveUpperBound( minimum, maximum );
            }

            FInt numberOfPlanetsPerSpoke = targetNumberOfPlanetsForSpokes / numberOfSpokes;
            FInt distanceBetweenPlanetsOnSpoke = (radius - 60) / numberOfPlanetsPerSpoke;

            //4) Pick Random Starting Angle
            AngleDegrees startingAngle = AngleDegrees.Create( (float)Context.RandomToUse.NextWithInclusiveUpperBound( 10, 350 ) );

            AngleDegrees anglePerSpoke = AngleDegrees.Create( (float)360 / (float)numberOfSpokes );

            //5) For each spoke
            ThrowawayListCanMemLeak<Planet> secondToLastPlanetsInEachSpoke = new ThrowawayListCanMemLeak<Planet>( 500 );
            ThrowawayListCanMemLeak<Planet> lastPlanetsInEachSpoke = new ThrowawayListCanMemLeak<Planet>( 500 );
            {
                Planet lastPlanetPlacedInSpoke;
                Planet secondToLastPlanetPlacedInSpoke;
                Planet newPlanetInSpoke;
                AngleDegrees spokeAngle = startingAngle;
                ArcenPoint linePoint;
                ArcenPoint perpendicularLinePoint;
                bool flipLineDisplacement;
                for ( int i = 0; i < numberOfSpokes; i++ )
                {
                    lastPlanetPlacedInSpoke = null;
                    secondToLastPlanetPlacedInSpoke = null;
                    flipLineDisplacement = false;
                    FInt distanceFromOrigin = (FInt)80;

                    for ( int j = 0; j < numberOfPlanetsPerSpoke; j++ )
                    {
                        //-- compute point on line from origin at angle at radius 60
                        linePoint = originPlanetPoint.GetPointAtAngleAndDistance( spokeAngle, distanceFromOrigin.IntValue );

                        //-- compute point on line perpendicular to that, at +20 along that line
                        perpendicularLinePoint = linePoint.GetPointAtAngleAndDistance( originPlanetPoint.GetAngleToDegrees( linePoint ).Add( AngleDegrees.Create( (float)90 ) ),
                             flipLineDisplacement ? -25 : 25 );

                        //-- place planet there
                        newPlanetInSpoke = galaxy.AddPlanet( planetType, perpendicularLinePoint,
                            World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );

                        //--- connect to last two planets placed on this spoke
                        if ( lastPlanetPlacedInSpoke != null )
                            lastPlanetPlacedInSpoke.AddLinkTo( newPlanetInSpoke );
                        if ( secondToLastPlanetPlacedInSpoke != null )
                            secondToLastPlanetPlacedInSpoke.AddLinkTo( newPlanetInSpoke );
                        //--- if fewer than 2 already in spoke, also connect to origin
                        else
                            originPlanet.AddLinkTo( newPlanetInSpoke );
                        secondToLastPlanetPlacedInSpoke = lastPlanetPlacedInSpoke;
                        lastPlanetPlacedInSpoke = newPlanetInSpoke;

                        //-- flip next to be placed at -20 instead of +20
                        flipLineDisplacement = !flipLineDisplacement;

                        //--increment radius
                        distanceFromOrigin += distanceBetweenPlanetsOnSpoke;

                        //---if radius now >= target overall radius, increment angle by 360/spoke_count and go to next spoke
                        if ( distanceFromOrigin >= radius )
                            break;
                    }

                    if ( secondToLastPlanetPlacedInSpoke != null )
                        secondToLastPlanetsInEachSpoke.Add( secondToLastPlanetPlacedInSpoke );
                    if ( lastPlanetPlacedInSpoke != null )
                        lastPlanetsInEachSpoke.Add( lastPlanetPlacedInSpoke );
                    spokeAngle = spokeAngle.Add( anglePerSpoke );
                }
            }

            //6) Compute number of planets left to place
            int numberOfPlanetsForRing = numberToSeed - galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise();

            //7) Compute average angle for next step from 360/planets_left
            AngleDegrees angleBetweenRingPlanet = AngleDegrees.Create( (float)360 / (float)numberOfPlanetsForRing );

            //8) Pick Random Starting Angle from 0 to 359
            AngleDegrees ringAngle = AngleDegrees.Create( (float)Context.RandomToUse.NextWithInclusiveUpperBound( 10, 350 ) );

            //9) for i from 0 to number of planets left to place
            bool useSimpleRingConstruction = numberToSeed < 40;
            ThrowawayListCanMemLeak<Planet> innerRingPlanets = new ThrowawayListCanMemLeak<Planet>( 500 );
            {
                ThrowawayListCanMemLeak<Planet> ringPlanets = new ThrowawayListCanMemLeak<Planet>( 500 );
                ArcenPoint linePoint;
                Planet newPlanetInRing;
                bool flipOffsetForNextRingPlanet = false;
                for ( int i = 0; i < numberOfPlanetsForRing; i++ )
                {
                    //-- compute point on line from origin at angle at target radius +40
                    linePoint = originPlanetPoint.GetPointAtAngleAndDistance( ringAngle, radius + (!useSimpleRingConstruction && flipOffsetForNextRingPlanet ? 30 : 60) );

                    //-- place planet there
                    newPlanetInRing = galaxy.AddPlanet( planetType, linePoint,
                        World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );

                    //--- connect to last two planets placed on ring
                    //--- if this is second-to-last or last planet in ring, connect to first planet in ring
                    //--- if this is last planet in ring, connect to second planet in ring
                    if ( !useSimpleRingConstruction )
                    {
                        if ( ringPlanets.Count >= 1 )
                            ringPlanets[ringPlanets.Count - 1].AddLinkTo( newPlanetInRing );
                        if ( ringPlanets.Count >= 2 )
                            ringPlanets[ringPlanets.Count - 2].AddLinkTo( newPlanetInRing );

                        if ( i >= numberOfPlanetsForRing - 2 && ringPlanets.Count >= 1 )
                            ringPlanets[0].AddLinkTo( newPlanetInRing );
                        if ( i >= numberOfPlanetsForRing - 1 && ringPlanets.Count >= 2 )
                            ringPlanets[1].AddLinkTo( newPlanetInRing );

                        ringPlanets.Add( newPlanetInRing );
                        if ( flipOffsetForNextRingPlanet )
                            innerRingPlanets.Add( newPlanetInRing );
                    }
                    else
                    {
                        if ( ringPlanets.Count >= 1 )
                            ringPlanets[ringPlanets.Count - 1].AddLinkTo( newPlanetInRing );

                        if ( i >= numberOfPlanetsForRing - 1 && ringPlanets.Count >= 1 )
                            ringPlanets[0].AddLinkTo( newPlanetInRing );

                        ringPlanets.Add( newPlanetInRing );
                        innerRingPlanets.Add( newPlanetInRing );
                    }

                    //-- flip offset to +20
                    flipOffsetForNextRingPlanet = !flipOffsetForNextRingPlanet;
                    ringAngle = ringAngle.Add( angleBetweenRingPlanet );
                }
            }

            //10) for each spoke
            //-- for the last planet, connect to the two nearest ring planets
            //-- for the second-to-last planet, connect to the nearest ring planet.
            if ( numberToSeed > 10 )
            {
                Planet spokeEndPlanet;
                if ( !useSimpleRingConstruction )
                {
                    for ( int i = 0; i < secondToLastPlanetsInEachSpoke.Count; i++ )
                    {
                        spokeEndPlanet = secondToLastPlanetsInEachSpoke[i];
                        cb_wheelSpokeEndPlanet = spokeEndPlanet;
                        innerRingPlanets.Sort(
                            static delegate ( Planet Left, Planet Right )
                            {
                                return Mat.DistanceBetweenPointsImprecise( Left.GalaxyLocation, cb_wheelSpokeEndPlanet.GalaxyLocation )
                                    .CompareTo(
                                       Mat.DistanceBetweenPointsImprecise( Right.GalaxyLocation, cb_wheelSpokeEndPlanet.GalaxyLocation )
                                    );
                            } );
                        if ( innerRingPlanets.Count >= 1 )
                            spokeEndPlanet.AddLinkTo( innerRingPlanets[0] );
                    }
                }
                for ( int i = 0; i < lastPlanetsInEachSpoke.Count; i++ )
                {
                    spokeEndPlanet = lastPlanetsInEachSpoke[i];
                    cb_wheelSpokeEndPlanet = spokeEndPlanet;
                    innerRingPlanets.Sort(
                        static delegate ( Planet Left, Planet Right )
                        {
                            return Mat.DistanceBetweenPointsImprecise( Left.GalaxyLocation, cb_wheelSpokeEndPlanet.GalaxyLocation )
                                .CompareTo(
                                   Mat.DistanceBetweenPointsImprecise( Right.GalaxyLocation, cb_wheelSpokeEndPlanet.GalaxyLocation )
                                );
                        } );
                    if ( innerRingPlanets.Count >= 1 )
                        spokeEndPlanet.AddLinkTo( innerRingPlanets[0] );
                    if ( !useSimpleRingConstruction && innerRingPlanets.Count >= 2 )
                        spokeEndPlanet.AddLinkTo( innerRingPlanets[1] );
                }
            }

            // emergency connection test
            {
                foreach ( Planet planet in galaxy.Planets( false ) )
                {
                    if ( planet.GetLinkedNeighborCount() > 0 )
                        continue;
                    cb_wheelEmergencyPlanet = planet;
                    lastPlanetsInEachSpoke.Sort(
                        static delegate ( Planet Left, Planet Right )
                        {
                            return Mat.DistanceBetweenPointsImprecise( Left.GalaxyLocation, cb_wheelEmergencyPlanet.GalaxyLocation )
                                .CompareTo(
                                   Mat.DistanceBetweenPointsImprecise( Right.GalaxyLocation, cb_wheelEmergencyPlanet.GalaxyLocation )
                                );
                        } );
                    if ( lastPlanetsInEachSpoke.Count >= 1 )
                        planet.AddLinkTo( lastPlanetsInEachSpoke[0] );
                }
            }
        }
    }

    public class Mapgen_Encapsulated : IMapGenerator
    {
        public Mapgen_Encapsulated()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_Encapsulated : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            while ( !RenderMethodCookie( Context, galaxy, mapConfig, mapType ) ) ;

            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs() );
        }

        private static bool RenderMethodCookie( ArcenHostOnlySimContext Context, Galaxy galaxy, MapConfiguration mapConfig, MapTypeData mapType )
        {
            #region determine number of clusters
            int NumberOfPlanets = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            int minimumSmallClusterSize = 6;
            int normalLargeClusterSize = 14;
            int numberOfRimPlanets;
            ThrowawayListCanMemLeak<int> smallClusterSizes = new ThrowawayListCanMemLeak<int>( 300 );
            ThrowawayListCanMemLeak<int> largeClusterSizes = new ThrowawayListCanMemLeak<int>( 300 );
            {
                switch ( NumberOfPlanets )
                {
                    case 10:
                        numberOfRimPlanets = 5;
                        smallClusterSizes.Add( 5 );
                        break;
                    case 15:
                        numberOfRimPlanets = 7;
                        smallClusterSizes.Add( 4 );
                        smallClusterSizes.Add( 4 );
                        break;
                    case 20:
                        smallClusterSizes.Add( minimumSmallClusterSize );
                        smallClusterSizes.Add( minimumSmallClusterSize );
                        numberOfRimPlanets = NumberOfPlanets - (minimumSmallClusterSize * 2);
                        break;
                    case 25:
                        if ( Context.RandomToUse.NextBool() )
                        {
                            smallClusterSizes.Add( minimumSmallClusterSize );
                            smallClusterSizes.Add( minimumSmallClusterSize );
                            smallClusterSizes.Add( minimumSmallClusterSize );
                            numberOfRimPlanets = NumberOfPlanets - (minimumSmallClusterSize * 3);
                        }
                        else
                        {
                            largeClusterSizes.Add( normalLargeClusterSize );
                            numberOfRimPlanets = NumberOfPlanets - normalLargeClusterSize;
                        }
                        break;
                    default:
                        int circleSize = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "EncapsulatedCircleSize" ).RelatedIntValue;

                        FInt circleSizeMultiplier = FInt.FromParts( 0, 400 );
                        if ( circleSize == 0 )
                            circleSizeMultiplier = FInt.FromParts( 0, 250 );
                        if ( circleSize == 1 )
                            circleSizeMultiplier = FInt.FromParts( 0, 400 );
                        if ( circleSize == 2 )
                            circleSizeMultiplier = FInt.FromParts( 0, 550 );
                        numberOfRimPlanets = ((FInt)NumberOfPlanets * circleSizeMultiplier).IntValue;
                        int planetsRemaining = NumberOfPlanets - numberOfRimPlanets;
                        while ( planetsRemaining > 10 )
                        {
                            bool pickLarge = false;
                            if ( largeClusterSizes.Count < 4 )
                            {
                                if ( smallClusterSizes.Count >= 5 )
                                    pickLarge = true;
                                else if ( Context.RandomToUse.NextBool() )
                                    pickLarge = true;
                            }

                            if ( pickLarge )
                            {
                                int clusterSize = Math.Min( normalLargeClusterSize, planetsRemaining );
                                largeClusterSizes.Add( clusterSize );
                                planetsRemaining -= clusterSize;
                            }
                            else
                            {
                                int clusterSize = Math.Min( minimumSmallClusterSize, planetsRemaining );
                                smallClusterSizes.Add( clusterSize );
                                planetsRemaining -= clusterSize;
                            }
                        }
                        if ( planetsRemaining >= minimumSmallClusterSize )
                        {
                            int clusterSize = Math.Min( minimumSmallClusterSize, planetsRemaining );
                            smallClusterSizes.Add( clusterSize );
                            planetsRemaining -= clusterSize;
                        }
                        if ( planetsRemaining > 0 )
                        {
                            numberOfRimPlanets += planetsRemaining;
                            planetsRemaining = 0;
                        }
                        break;
                }
            }
            #endregion
            int largeClusterRadius = 175;
            int smallClusterRadius = 100;

            ArcenPoint galacticCenter = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;
            int galacticRadius;
            if ( NumberOfPlanets < 60 )
                galacticRadius = 500;
            else if ( NumberOfPlanets < 100 )
                galacticRadius = 600;
            else if ( NumberOfPlanets < 150 )
                galacticRadius = 700;
            else
                galacticRadius = 800;

            ThrowawayListCanMemLeak<ArcenPoint> largeClusterCenters = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            #region Pick points for each large cluster
            {
                int permittedRadius = galacticRadius - ((FInt)largeClusterRadius * FInt.FromParts( 1, 500 )).IntValue;
                //int lastRadiusToTest = 0;
                //int radiusToTest = largeClusterRadius;
                int numberFailuresAllowedBeforeRadiusIncrease = 1000;
                for ( int i = 0; i < largeClusterSizes.Count; i++ )
                {
                    ArcenPoint newClusterCenter = galacticCenter.GetRandomPointWithinDistance( Context.RandomToUse, 0, permittedRadius );

                    bool foundCollision = false;
                    for ( int j = 0; j < largeClusterCenters.Count; j++ )
                    {
                        ArcenPoint existingClusterCenter = largeClusterCenters[j];
                        int threshold = largeClusterRadius + largeClusterRadius;
                        if ( Mat.ApproxDistanceBetweenPointsFast( newClusterCenter, existingClusterCenter, threshold ) < threshold )
                        {
                            foundCollision = true;
                            break;
                        }
                    }
                    if ( foundCollision )
                    {
                        i--;
                        numberFailuresAllowedBeforeRadiusIncrease--;
                        if ( numberFailuresAllowedBeforeRadiusIncrease <= 0 )
                        {
                            //lastRadiusToTest = radiusToTest;
                            //radiusToTest += largeClusterRadius / 2;
                            //if ( radiusToTest >= galacticRadius )
                            return false;
                        }
                        continue;
                    }

                    largeClusterCenters.Add( newClusterCenter );
                    numberFailuresAllowedBeforeRadiusIncrease = 100;
                }
            }
            #endregion

            ThrowawayListCanMemLeak<ArcenPoint> smallClusterCenters = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            #region Pick points for each small cluster
            {
                int permittedRadius = galacticRadius - ((FInt)smallClusterRadius * FInt.FromParts( 1, 500 )).IntValue;
                //int lastRadiusToTest = 0;
                //int radiusToTest = smallClusterRadius;
                int numberFailuresAllowedBeforeRadiusIncrease = 1000;
                for ( int i = 0; i < smallClusterSizes.Count; i++ )
                {
                    ArcenPoint newClusterCenter = galacticCenter.GetRandomPointWithinDistance( Context.RandomToUse, 0, permittedRadius );

                    bool foundCollision = false;
                    for ( int j = 0; j < largeClusterCenters.Count; j++ )
                    {
                        ArcenPoint existingClusterCenter = largeClusterCenters[j];
                        int threshold = largeClusterRadius + smallClusterRadius;
                        if ( Mat.ApproxDistanceBetweenPointsFast( newClusterCenter, existingClusterCenter, threshold ) < threshold )
                        {
                            foundCollision = true;
                            break;
                        }
                    }
                    if ( !foundCollision )
                    {
                        for ( int j = 0; j < smallClusterCenters.Count; j++ )
                        {
                            ArcenPoint existingClusterCenter = smallClusterCenters[j];
                            int threshold = smallClusterRadius + smallClusterRadius;
                            if ( Mat.ApproxDistanceBetweenPointsFast( newClusterCenter, existingClusterCenter, threshold ) < threshold )
                            {
                                foundCollision = true;
                                break;
                            }
                        }
                    }
                    if ( foundCollision )
                    {
                        i--;
                        numberFailuresAllowedBeforeRadiusIncrease--;
                        if ( numberFailuresAllowedBeforeRadiusIncrease <= 0 )
                        {
                            //lastRadiusToTest = radiusToTest;
                            //radiusToTest += smallClusterRadius / 2;
                            //if ( radiusToTest >= galacticRadius )
                            return false;
                        }
                        continue;
                    }

                    smallClusterCenters.Add( newClusterCenter );
                    numberFailuresAllowedBeforeRadiusIncrease = 100;
                }
            }
            #endregion

            ThrowawayListCanMemLeak<int> clusterSizes = new ThrowawayListCanMemLeak<int>( 300 );
            ThrowawayListCanMemLeak<ArcenPoint> clusterCenters = new ThrowawayListCanMemLeak<ArcenPoint>( 300 );
            for ( int i = 0; i < largeClusterSizes.Count; i++ )
            {
                clusterSizes.Add( largeClusterSizes[i] );
                clusterCenters.Add( largeClusterCenters[i] );
            }
            for ( int i = 0; i < smallClusterSizes.Count; i++ )
            {
                clusterSizes.Add( smallClusterSizes[i] );
                clusterCenters.Add( smallClusterCenters[i] );
            }

            ThrowawayListCanMemLeak<MapClusterStyle> clusterStyles = new ThrowawayListCanMemLeak<MapClusterStyle>( 300 );
            ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<ArcenPoint>> planetPointsByCluster = new ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<ArcenPoint>>( 300 );
            #region For each cluster, pick points for planets
            {
                for ( int i = 0; i < clusterCenters.Count; i++ )
                {
                    ArcenPoint clusterCenter = clusterCenters[i];

                    planetPointsByCluster.Add( new ThrowawayListCanMemLeak<ArcenPoint>( 300 ) );

                    int numberOfPlanetsForCluster = clusterSizes[i];

                    MapClusterStyle clusterStyle = MapClusterStyle.Simple;
                    clusterStyles.Add( clusterStyle );

                    int clusterRadius = numberOfPlanetsForCluster > minimumSmallClusterSize
                        ? largeClusterRadius
                        : smallClusterRadius
                        ;

                    if ( !UtilityMethods.Helper_PickIntraClusterPlanetPoints( Context, planetPointsByCluster[i], clusterCenter, clusterRadius, numberOfPlanetsForCluster, clusterStyle ) )
                        return false;
                }
            }
            #endregion

            // CANNOT early-out from this point on, MUST generate a usable map

            PlanetType planetType = PlanetType.Normal;

            ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<Planet>> planetsByCluster = new ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<Planet>>( 300 );
            #region For each cluster, populate with planets
            {
                int totalPlanetsPlaced = 0;
                ThrowawayListCanMemLeak<ArcenPoint> planetPointList;
                MapClusterStyle clusterStyle;
                for ( int i = 0; i < planetPointsByCluster.Count; i++ )
                {
                    planetPointList = planetPointsByCluster[i];
                    planetsByCluster.Add( new ThrowawayListCanMemLeak<Planet>( 500 ) );
                    #region Place Planets
                    {
                        for ( int j = 0; j < planetPointList.Count; j++ )
                        {
                            planetsByCluster[i].Add( galaxy.AddPlanet( planetType, planetPointList[j],
                                World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) ) );
                            totalPlanetsPlaced++;
                        }
                    }
                    #endregion
                    #region Add intra-cluster connections
                    clusterStyle = clusterStyles[i];
                    UtilityMethods.Helper_MakeIntraClusterConnections( planetsByCluster[i], clusterStyle );
                    #endregion
                }
            }
            #endregion

            #region For each cluster, pick up to 3 connections to other nearby clusters
            {
                ThrowawayListCanMemLeak<Pair<int, int>> linkedClusterIndices = new ThrowawayListCanMemLeak<Pair<int, int>>( 300 );
                ArcenPoint clusterCenter;
                IList<Planet> clusterPlanetList;
                ArcenPoint otherClusterCenter;
                //List<Planet> otherClusterPlanetList;
                int distanceToOtherCluster;
                int closestClusterIndex;
                int distanceToClosestCluster;
                int secondClosestClusterIndex;
                int distanceToSecondClosestCluster;
                for ( int i = 0; i < clusterCenters.Count; i++ )
                {
                    clusterCenter = clusterCenters[i];
                    clusterPlanetList = planetsByCluster[i];

                    closestClusterIndex = -1;
                    distanceToClosestCluster = 0;
                    secondClosestClusterIndex = -1;
                    distanceToSecondClosestCluster = 0;

                    int distanceToClosestClusterIncludingAlreadyConnected = -1;
                    for ( int j = 0; j < clusterCenters.Count; j++ )
                    {
                        if ( i == j )
                            continue;
                        otherClusterCenter = clusterCenters[j];
                        //otherClusterPlanetList = planetsByCluster[j];
                        distanceToOtherCluster = Mat.DistanceBetweenPointsImprecise( clusterCenter, otherClusterCenter );
                        if ( distanceToClosestClusterIncludingAlreadyConnected == -1 || distanceToOtherCluster < distanceToClosestClusterIncludingAlreadyConnected )
                            distanceToClosestClusterIncludingAlreadyConnected = distanceToOtherCluster;
                    }
                    int maxThresholdForConnection = ((FInt)distanceToClosestClusterIncludingAlreadyConnected * FInt.FromParts( 1, 50 )).IntValue;

                    for ( int j = 0; j < clusterCenters.Count; j++ )
                    {
                        if ( i == j )
                            continue;
                        if ( UtilityMethods.Helper_GetDoesPairListContainPairInEitherDirection( linkedClusterIndices, i, j ) )
                            continue;
                        otherClusterCenter = clusterCenters[j];
                        //otherClusterPlanetList = planetsByCluster[j];
                        distanceToOtherCluster = Mat.DistanceBetweenPointsImprecise( clusterCenter, otherClusterCenter );
                        if ( distanceToOtherCluster > maxThresholdForConnection )
                            continue;
                        if ( closestClusterIndex == -1 || distanceToOtherCluster < distanceToClosestCluster )
                        {
                            closestClusterIndex = j;
                            distanceToClosestCluster = distanceToOtherCluster;
                        }
                        else if ( secondClosestClusterIndex == -1 || distanceToOtherCluster < distanceToSecondClosestCluster )
                        {
                            secondClosestClusterIndex = j;
                            distanceToSecondClosestCluster = distanceToOtherCluster;
                        }
                    }

                    if ( UtilityMethods.Helper_GetNumberOfPairsInPairListInvolving( linkedClusterIndices, i ) < 3 &&
                         closestClusterIndex != -1 &&
                         (UtilityMethods.Helper_GetNumberOfPairsInPairListInvolving( linkedClusterIndices, i ) < 2 ||
                           UtilityMethods.Helper_GetNumberOfPairsInPairListInvolving( linkedClusterIndices, closestClusterIndex ) < 3) )
                    {
                        UtilityMethods.Helper_ConnectPlanetLists( clusterPlanetList, planetsByCluster[closestClusterIndex], false, true );
                        linkedClusterIndices.Add( Pair<int, int>.Create( i, closestClusterIndex ) );
                    }
                    if ( UtilityMethods.Helper_GetNumberOfPairsInPairListInvolving( linkedClusterIndices, i ) < 2 &&
                         secondClosestClusterIndex != -1 &&
                         UtilityMethods.Helper_GetNumberOfPairsInPairListInvolving( linkedClusterIndices, secondClosestClusterIndex ) < 2 )
                    {
                        UtilityMethods.Helper_ConnectPlanetLists( clusterPlanetList, planetsByCluster[secondClosestClusterIndex], false, true );
                        linkedClusterIndices.Add( Pair<int, int>.Create( i, secondClosestClusterIndex ) );
                    }
                }
            }
            #endregion

            #region Place Outer Rim
            ThrowawayListCanMemLeak<Planet> rimPlanets = new ThrowawayListCanMemLeak<Planet>( 500 );
            if ( numberOfRimPlanets > 0 ) // it will be, but just making sure
            {
                float startingAngle = Context.RandomToUse.NextFloat( 1, 359 );

                Planet firstPlanetPlacedOnRing = null;
                Planet lastPlanetPlacedOnRing = null;
                for ( int j = 0; j < numberOfRimPlanets; j++ )
                {
                    float angle = (360f / (float)numberOfRimPlanets) * (float)j; // yes, this is theoretically an MP-sync problem, but a satisfactory 360 arc was simply not coming from the FInt approximations and I'm figuring the actual full-sync at the beginning of the game should sync things up before they matter
                    angle += startingAngle;
                    if ( angle >= 360f )
                        angle -= 360f;

                    ArcenPoint pointOnRing = galacticCenter;
                    pointOnRing.X += (int)Math.Round( galacticRadius * (float)Math.Cos( angle * (Math.PI / 180f) ) );
                    pointOnRing.Y += (int)Math.Round( galacticRadius * (float)Math.Sin( angle * (Math.PI / 180f) ) );

                    Planet planet = galaxy.AddPlanet( planetType, pointOnRing,
                        World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );

                    rimPlanets.Add( planet );

                    if ( lastPlanetPlacedOnRing != null )
                        lastPlanetPlacedOnRing.AddLinkTo( planet );

                    if ( firstPlanetPlacedOnRing == null )
                        firstPlanetPlacedOnRing = planet;

                    lastPlanetPlacedOnRing = planet;
                }
                if ( lastPlanetPlacedOnRing != null && firstPlanetPlacedOnRing != null )
                    lastPlanetPlacedOnRing.AddLinkTo( firstPlanetPlacedOnRing );
            }
            #endregion

            #region Connect Outer Rim to clusters
            ThrowawayListCanMemLeak<int> closestClusterIndexByRimPlanet = new ThrowawayListCanMemLeak<int>( 300 );
            for ( int i = 0; i < rimPlanets.Count; i++ )
            {
                Planet rimPlanet = rimPlanets[i];
                int bestDistanceSoFar = -1;
                int bestIndexSoFar = -1;
                for ( int j = 0; j < planetsByCluster.Count; j++ )
                {
                    IList<Planet> cluster = planetsByCluster[j];
                    int distanceToCluster = -1;
                    for ( int k = 0; k < cluster.Count; k++ )
                    {
                        Planet clusterPlanet = cluster[k];
                        int distance = Mat.ApproxDistanceBetweenPointsFast( rimPlanet.GalaxyLocation, clusterPlanet.GalaxyLocation, -1 );
                        if ( distanceToCluster == -1 ||
                             distanceToCluster > distance )
                            distanceToCluster = distance;
                    }

                    if ( bestIndexSoFar == -1 ||
                       bestDistanceSoFar > distanceToCluster )
                    {
                        bestDistanceSoFar = distanceToCluster;
                        bestIndexSoFar = j;
                    }
                }
                closestClusterIndexByRimPlanet.Add( bestIndexSoFar );
            }

            ThrowawayListCanMemLeak<int> closestRimIndexByCluster = new ThrowawayListCanMemLeak<int>( 300 );
            for ( int i = 0; i < planetsByCluster.Count; i++ )
            {
                IList<Planet> cluster = planetsByCluster[i];
                int bestDistanceSoFar = -1;
                int bestIndexSoFar = -1;
                for ( int j = 0; j < rimPlanets.Count; j++ )
                {
                    Planet rimPlanet = rimPlanets[j];
                    int distanceToPlanet = -1;
                    for ( int k = 0; k < cluster.Count; k++ )
                    {
                        Planet clusterPlanet = cluster[k];
                        int distance = Mat.ApproxDistanceBetweenPointsFast( rimPlanet.GalaxyLocation, clusterPlanet.GalaxyLocation, -1 );
                        if ( distanceToPlanet == -1 ||
                             distanceToPlanet > distance )
                            distanceToPlanet = distance;
                    }

                    if ( bestIndexSoFar == -1 ||
                       bestDistanceSoFar > distanceToPlanet )
                    {
                        bestDistanceSoFar = distanceToPlanet;
                        bestIndexSoFar = j;
                    }
                }
                closestRimIndexByCluster.Add( bestIndexSoFar );
            }

            for ( int i = 0; i < rimPlanets.Count; i++ )
            {
                Planet rimPlanet = rimPlanets[i];
                int closestClusterIndex = closestClusterIndexByRimPlanet[i];
                int clustersRimIndex = closestRimIndexByCluster[closestClusterIndex];
                if ( clustersRimIndex != i )
                    continue;
                IList<Planet> cluster = planetsByCluster[closestClusterIndex];
                Planet closestPlanetInCluster = null;
                int distanceToClosest = -1;
                for ( int j = 0; j < cluster.Count; j++ )
                {
                    Planet clusterPlanet = cluster[j];
                    int distance = Mat.ApproxDistanceBetweenPointsFast( rimPlanet.GalaxyLocation, clusterPlanet.GalaxyLocation, -1 );
                    if ( closestPlanetInCluster == null ||
                        distanceToClosest > distance )
                    {
                        closestPlanetInCluster = clusterPlanet;
                        distanceToClosest = distance;
                    }
                }
                rimPlanet.AddLinkTo( closestPlanetInCluster );
            }
            #endregion

            return true;
        }
    }

    public enum MapClusterStyle
    {
        None,
        Simple,
        Concentric,
        //ConcentricWithSimpleLayout,
        Crosshatch,
        CrosshatchWithSimpleLayout,
        Length
    }

    public class Mapgen_TestChamber : IMapGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_TestChamber : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            galaxy.AddPlanet( PlanetType.Normal, Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter,
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
        }


    }

    public class Mapgen_Tutorial_01 : IMapGenerator
    {
        public Mapgen_Tutorial_01()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        private readonly Dictionary<string, Planet> planetsByName = Dictionary<string, Planet>.Create_WillNeverBeGCed( 300, "Mapgen_Tutorial_01-planetsByName" );
        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_Tutorial_01 : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            Tutorial tutorial = World_AIW2.Instance.TutorialOrNull;
            if ( tutorial == null )
                return;

            ArcenPoint centerPoint = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;

            planetsByName.Clear();
            for ( int i = 0; i < tutorial.PlanetSetups.Count; i++ )
            {
                Tutorial.TutorialPlanet planetSetup = tutorial.PlanetSetups[i];
                Planet planet = galaxy.AddPlanet( PlanetType.Normal, centerPoint +
                    ArcenPoint.Create( planetSetup.OffsetXFromGalaxyCenter, planetSetup.OffsetYFromGalaxyCenter ),
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                planet.Name = planetSetup.Name;
                planetsByName[planet.Name] = planet;

                planet.IsBlockedToPlayerTravelViaWormholes = planetSetup.IsBlockedToPlayerTravelViaWormholes;
                planet.IsBlockedToNPCTravelViaWormholes = planetSetup.IsBlockedToNPCTravelViaWormholes;
                planet.DestroyAllMetalHarvesters_NonSim_ForTutorials = planetSetup.DestroyAllMetalHarvesters;
            }

            for ( int i = 0; i < tutorial.PlanetSetups.Count; i++ )
            {
                Tutorial.TutorialPlanet planetSetup = tutorial.PlanetSetups[i];
                for ( int j = 0; j < planetSetup.LinksByName.Count; j++ )
                {
                    Planet planet1 = planetsByName[planetSetup.Name];
                    Planet planet2 = planetsByName[planetSetup.LinksByName[j]];
                    planet1.AddLinkTo( planet2 );
                }
            }
        }
    }

    public enum MapGenSeedStyle
    {
        SmallGood,
        SmallBad,
        BigGood,
        BigBad,
        FullUseByFaction,
        FullUseByFactionOnNomadIfPossible,
        Fuel,
        FactionBeacon,
        NoChecks,
        Other
    }

    public enum MapGenCountPerPlanet
    {
        One = 1,
        Two,
        Three,
        Four,
        Five
    }

    public enum SeedingExpansionType
    {
        ComplicatedOriginal,
        ExpandPlayerMaxOnly,
        DoNoExpansion
    }
}
