using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

namespace Arcen.AIW2.External
{
    public class Mapgen_USCities : IMapGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        // Cities: { xOffset, yOffset } relative to galaxy center
        // Positions derived from lat/lon: xOffset = (lon + 95) * 30, yOffset = (lat - 37) * 28
        // (Pacific coast cities have large negative xOffset, Atlantic coast small positive xOffset)
        private static readonly int[,] CityPositions = new int[,]
        {
            // 0  Seattle
            { -1260, 238 },
            // 1  Portland
            { -1290, 182 },
            // 2  Spokane
            { -1200, 238 },
            // 3  Boise
            { -1170, 154 },
            // 4  Eugene
            { -1320, 140 },
            // 5  Sacramento
            { -1380, 56 },
            // 6  San Francisco
            { -1410, 28 },
            // 7  San Jose
            { -1410, 0 },
            // 8  Fresno
            { -1380, -28 },
            // 9  Los Angeles
            { -1410, -84 },
            // 10 San Diego
            { -1410, -140 },
            // 11 Las Vegas
            { -1320, -112 },
            // 12 Phoenix
            { -1260, -196 },
            // 13 Tucson
            { -1230, -252 },
            // 14 Albuquerque
            { -1110, -168 },
            // 15 El Paso
            { -1080, -308 },
            // 16 Denver
            { -1050, 0 },
            // 17 Colorado Springs
            { -1050, -56 },
            // 18 Salt Lake City
            { -1200, 84 },
            // 19 Great Falls
            { -1110, 308 },
            // 20 Billings
            { -1050, 280 },
            // 21 Rapid City
            { -900, 238 },
            // 22 Cheyenne
            { -990, 56 },
            // 23 Omaha
            { -750, 84 },
            // 24 Sioux Falls
            { -810, 196 },
            // 25 Fargo
            { -780, 308 },
            // 26 Minneapolis
            { -660, 280 },
            // 27 Kansas City
            { -690, 0 },
            // 28 Wichita
            { -750, -84 },
            // 29 Oklahoma City
            { -750, -196 },
            // 30 Amarillo
            { -870, -224 },
            // 31 Lubbock
            { -870, -308 },
            // 32 Dallas
            { -720, -336 },
            // 33 Fort Worth
            { -750, -336 },
            // 34 Austin
            { -720, -420 },
            // 35 San Antonio
            { -720, -476 },
            // 36 Houston
            { -630, -476 },
            // 37 Shreveport
            { -570, -392 },
            // 38 New Orleans
            { -480, -504 },
            // 39 Memphis
            { -480, -280 },
            // 40 Little Rock
            { -570, -308 },
            // 41 Nashville
            { -330, -224 },
            // 42 Birmingham
            { -300, -364 },
            // 43 Atlanta
            { -240, -364 },
            // 44 Jacksonville
            { -120, -476 },
            // 45 Orlando
            { -150, -560 },
            // 46 Miami
            { -90, -644 },
            // 47 Tampa
            { -180, -588 },
            // 48 Louisville
            { -330, -140 },
            // 49 Columbus
            { -210, -56 },
            // 50 Cincinnati
            { -270, -112 },
            // 51 Cleveland
            { -180, 28 },
            // 52 Detroit
            { -150, 84 },
            // 53 Indianapolis
            { -300, -56 },
            // 54 Chicago
            { -360, 84 },
            // 55 Milwaukee
            { -360, 140 },
            // 56 St. Louis
            { -450, -84 },
            // 57 Springfield IL
            { -420, -28 },
            // 58 Pittsburgh
            { -120, -56 },
            // 59 Buffalo
            { -60, 84 },
            // 60 Philadelphia
            { 0, -28 },
            // 61 New York City
            { 30, 28 },
            // 62 Boston
            { 90, 84 },
            // 63 Providence
            { 60, 56 },
            // 64 Hartford
            { 60, 28 },
            // 65 Baltimore
            { 0, -56 },
            // 66 Washington DC
            { 0, -84 },
            // 67 Charlotte
            { -90, -252 },
            // 68 Raleigh
            { -30, -224 },
            // 69 Richmond
            { -30, -140 },
            // 70 Norfolk
            { 30, -168 },
            // 71 Knoxville
            { -240, -196 },
            // 72 Chattanooga
            { -270, -252 },
            // 73 Huntsville
            { -330, -308 },
        };

        // Edges: { cityA, cityB }
        private static readonly int[,] CityEdges = new int[,]
        {
            // Pacific Northwest
            { 0, 1 }, { 0, 2 }, { 1, 4 }, { 2, 3 }, { 3, 18 }, { 4, 5 },
            // West Coast spine
            { 5, 6 }, { 6, 7 }, { 7, 8 }, { 8, 9 }, { 9, 10 },
            // West Coast to interior
            { 5, 18 }, { 9, 11 }, { 10, 11 }, { 11, 12 }, { 12, 13 },
            { 12, 14 }, { 13, 15 }, { 14, 15 },
            // Mountain West
            { 18, 19 }, { 18, 16 }, { 3, 16 }, { 11, 16 }, { 16, 17 },
            { 17, 14 }, { 15, 32 },
            // Northern Rockies / Great Plains
            { 19, 20 }, { 20, 2 }, { 20, 21 }, { 21, 24 }, { 21, 22 },
            { 22, 16 }, { 22, 23 }, { 24, 25 }, { 25, 26 },
            // Central Plains
            { 23, 26 }, { 23, 27 }, { 23, 24 }, { 26, 55 }, { 26, 54 },
            { 27, 28 }, { 27, 56 }, { 27, 53 },
            // Southern Plains
            { 28, 29 }, { 28, 30 }, { 29, 30 }, { 29, 32 }, { 29, 40 },
            { 30, 31 }, { 30, 14 }, { 31, 32 }, { 31, 15 },
            // Texas
            { 32, 33 }, { 33, 34 }, { 34, 35 }, { 35, 36 }, { 36, 37 },
            { 36, 38 }, { 32, 37 }, { 37, 40 }, { 37, 39 },
            // South Central
            { 38, 39 }, { 39, 40 }, { 39, 41 }, { 40, 56 }, { 40, 73 },
            // Midwest
            { 54, 55 }, { 54, 57 }, { 54, 53 }, { 55, 52 }, { 53, 48 },
            { 53, 50 }, { 57, 56 }, { 56, 48 }, { 56, 39 },
            // Ohio Valley
            { 48, 41 }, { 48, 50 }, { 50, 49 }, { 50, 71 }, { 49, 51 },
            { 49, 58 }, { 51, 52 }, { 51, 59 }, { 52, 59 },
            // Southeast
            { 41, 71 }, { 41, 48 }, { 71, 72 }, { 72, 43 }, { 72, 73 },
            { 73, 42 }, { 42, 43 }, { 43, 67 }, { 43, 44 }, { 44, 45 },
            { 45, 47 }, { 45, 46 }, { 47, 46 }, { 42, 38 },
            // Mid-Atlantic
            { 58, 60 }, { 58, 66 }, { 59, 61 }, { 60, 65 }, { 60, 61 },
            { 61, 63 }, { 61, 64 }, { 63, 62 }, { 64, 62 }, { 65, 66 },
            { 65, 69 }, { 66, 69 }, { 69, 70 }, { 69, 68 }, { 68, 67 },
            { 68, 70 }, { 67, 41 },
        };

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            ArcenPoint center = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;

            int cityCount = CityPositions.GetLength( 0 );
            int edgeCount = CityEdges.GetLength( 0 );

            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( cityCount );

            for ( int i = 0; i < cityCount; i++ )
            {
                int x = center.X + CityPositions[i, 0];
                int y = center.Y + CityPositions[i, 1];
                Planet p = galaxy.AddPlanet( PlanetType.Normal, ArcenPoint.Create( x, y ),
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                allPlanets.Add( p );
            }

            BadgerUtilityMethods.EnforcePlanetMinimumSpacing( allPlanets, 50 );

            for ( int i = 0; i < edgeCount; i++ )
            {
                int a = CityEdges[i, 0];
                int b = CityEdges[i, 1];
                allPlanets[a].AddLinkTo( allPlanets[b] );
            }

            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }

    public class Mapgen_EuropeanCities : IMapGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        // Cities: { xOffset, yOffset } relative to galaxy center
        // Positions derived from lat/lon: xOffset = (lon - 15) * 38, yOffset = (lat - 48) * 35
        // (Iberian Peninsula has large negative xOffset, Eastern Europe/Turkey has positive xOffset)
        // North Africa extends south from Lisbon via Tangier; Middle East extends south-east from Istanbul.
        private static readonly int[,] CityPositions = new int[,]
        {
            // 0  Lisbon
            { -915, -325 },
            // 1  Oporto
            { -900, -240 },
            // 2  Madrid
            { -710, -265 },
            // 3  Seville
            { -795, -370 },
            // 4  Barcelona
            { -485, -230 },
            // 5  Valencia
            { -585, -300 },
            // 6  Bilbao
            { -680, -165 },
            // 7  Zaragoza
            { -605, -220 },
            // 8  Paris
            { -485, 30 },
            // 9  Lyon
            { -390, -75 },
            // 10 Marseille
            { -365, -165 },
            // 11 Toulouse
            { -515, -155 },
            // 12 London
            { -575, 125 },
            // 13 Dublin
            { -810, 185 },
            // 14 Manchester
            { -655, 195 },
            // 15 Glasgow
            { -735, 275 },
            // 16 Cardiff
            { -690, 125 },
            // 17 Brussels
            { -405, 100 },
            // 18 Amsterdam
            { -385, 155 },
            // 19 Luxembourg
            { -340, 55 },
            // 20 Bern
            { -290, -40 },
            // 21 Zurich
            { -245, -20 },
            // 22 Frankfurt
            { -240, 75 },
            // 23 Munich
            { -130, 5 },
            // 24 Berlin
            { -60, 160 },
            // 25 Hamburg
            { -190, 195 },
            // 26 Copenhagen
            { -90, 270 },
            // 27 Stockholm
            { 120, 395 },
            // 28 Oslo
            { -165, 415 },
            // 29 Helsinki
            { 380, 425 },
            // 30 Riga
            { 345, 310 },
            // 31 Vilnius
            { 390, 235 },
            // 32 Tallinn
            { 370, 400 },
            // 33 Gdansk
            { 135, 225 },
            // 34 Warsaw
            { 230, 145 },
            // 35 Krakow
            { 190, 75 },
            // 36 Prague
            { -25, 75 },
            // 37 Vienna
            { 55, 5 },
            // 38 Bratislava
            { 80, 5 },
            // 39 Budapest
            { 150, -20 },
            // 40 Zagreb
            { 35, -75 },
            // 41 Ljubljana
            { -20, -65 },
            // 42 Sarajevo
            { 130, -145 },
            // 43 Belgrade
            { 210, -110 },
            // 44 Bucharest
            { 420, -125 },
            // 45 Sofia
            { 315, -185 },
            // 46 Skopje
            { 245, -210 },
            // 47 Tirana
            { 180, -235 },
            // 48 Podgorica
            { 165, -195 },
            // 49 Athens
            { 330, -355 },
            // 50 Thessaloniki
            { 305, -260 },
            // 51 Istanbul
            { 530, -245 },
            // 52 Ankara
            { 680, -285 },
            // 53 Rome
            { -95, -215 },
            // 54 Milan
            { -220, -90 },
            // 55 Naples
            { -25, -250 },
            // 56 Palermo
            { -60, -345 },
            // 57 Turin
            { -275, -100 },
            // 58 Kiev
            { 590, 90 },
            // 59 Minsk
            { 480, 205 },
            // 60 Lviv
            { 340, 65 },
            // 61 Kharkiv
            { 805, 70 },
            // 62 Odessa
            { 595, -55 },
            // --- North Africa ---
            // 63 Tangier
            { -790, -425 },
            // 64 Casablanca
            { -860, -505 },
            // 65 Marrakech
            { -875, -575 },
            // 66 Fez
            { -760, -490 },
            // 67 Algiers
            { -450, -395 },
            // 68 Oran
            { -595, -430 },
            // 69 Constantine
            { -320, -405 },
            // 70 Tunis
            { -180, -390 },
            // 71 Tripoli
            { -70, -530 },
            // 72 Benghazi
            { 195, -555 },
            // 73 Alexandria
            { 565, -590 },
            // 74 Cairo
            { 615, -630 },
            // --- Middle East ---
            // 75 Beirut
            { 780, -495 },
            // 76 Damascus
            { 810, -510 },
            // 77 Aleppo
            { 845, -415 },
            // 78 Amman
            { 795, -565 },
            // 79 Tel Aviv
            { 750, -555 },
            // 80 Baghdad
            { 1120, -515 },
            // 81 Riyadh
            { 1205, -815 },
            // 82 Kuwait
            { 1250, -650 },
        };

        // Edges: { cityA, cityB }
        private static readonly int[,] CityEdges = new int[,]
        {
            // Iberian Peninsula
            { 0, 1 }, { 0, 3 }, { 1, 2 }, { 2, 3 }, { 2, 5 }, { 2, 6 }, { 2, 7 },
            { 3, 5 }, { 4, 5 }, { 4, 7 }, { 4, 10 }, { 6, 7 }, { 6, 11 }, { 7, 11 },
            // France
            { 8, 9 }, { 8, 11 }, { 8, 12 }, { 8, 17 }, { 8, 19 },
            { 9, 10 }, { 9, 11 }, { 9, 54 }, { 9, 57 },
            // British Isles
            { 12, 14 }, { 12, 16 }, { 12, 17 }, { 13, 14 }, { 13, 15 },
            { 14, 15 }, { 14, 16 }, { 15, 28 },
            // Benelux / Western Germany
            { 17, 18 }, { 17, 19 }, { 17, 22 }, { 18, 25 }, { 19, 20 }, { 19, 22 },
            { 20, 21 }, { 20, 57 }, { 21, 22 }, { 21, 23 }, { 21, 54 },
            { 22, 23 }, { 22, 24 }, { 22, 25 },
            // Italy
            { 54, 57 }, { 54, 53 }, { 54, 41 }, { 53, 55 }, { 55, 56 },
            // Central Europe
            { 23, 37 }, { 23, 41 }, { 24, 25 }, { 24, 33 }, { 24, 36 },
            { 25, 26 }, { 26, 27 }, { 26, 28 },
            // Scandinavia
            { 27, 28 }, { 27, 29 }, { 27, 32 },
            // Baltic States
            { 29, 30 }, { 29, 32 }, { 30, 31 }, { 30, 32 }, { 30, 33 },
            // Eastern Europe
            { 33, 34 }, { 34, 31 }, { 34, 35 }, { 34, 59 }, { 35, 36 },
            { 35, 39 }, { 35, 60 }, { 36, 37 }, { 37, 38 }, { 37, 40 },
            { 37, 41 }, { 38, 39 },
            // Balkans
            { 39, 42 }, { 39, 43 }, { 40, 41 }, { 40, 42 }, { 42, 43 },
            { 42, 47 }, { 42, 48 }, { 43, 44 }, { 43, 45 }, { 43, 46 },
            // Greece / Turkey / Black Sea
            { 44, 45 }, { 44, 51 }, { 44, 62 }, { 45, 46 }, { 45, 50 },
            { 46, 47 }, { 47, 48 }, { 49, 50 }, { 49, 51 }, { 50, 51 }, { 51, 52 },
            // Ukraine / East
            { 58, 59 }, { 58, 60 }, { 58, 61 }, { 58, 62 }, { 59, 31 }, { 60, 35 }, { 62, 51 },
            // North Africa spine (Lisbon to Cairo)
            { 0, 63 },   // Lisbon-Tangier (Strait of Gibraltar ferry)
            { 63, 64 }, { 63, 66 },           // Tangier hub
            { 64, 65 }, { 64, 68 },           // Casablanca hub
            { 66, 68 },                        // Fez-Oran
            { 68, 67 }, { 67, 69 },           // Oran-Algiers-Constantine
            { 69, 70 }, { 70, 71 },           // Constantine-Tunis-Tripoli
            { 71, 72 }, { 72, 73 }, { 73, 74 }, // Tripoli-Benghazi-Alexandria-Cairo
            // Mediterranean ferry shortcuts into North Africa
            { 10, 70 },  // Marseille-Tunis
            { 56, 71 },  // Palermo-Tripoli
            { 49, 73 },  // Athens-Alexandria
            // Middle East (connecting from Istanbul / Ankara)
            { 51, 77 },  // Istanbul-Aleppo
            { 52, 77 },  // Ankara-Aleppo
            { 77, 76 }, { 76, 75 }, { 76, 78 }, // Aleppo-Damascus hub
            { 75, 79 }, { 78, 79 },             // Beirut/Amman-Tel Aviv
            { 74, 79 },                          // Cairo-Tel Aviv (Sinai coast)
            { 77, 80 },                          // Aleppo-Baghdad
            { 80, 82 }, { 80, 81 }, { 81, 82 }, // Baghdad-Kuwait-Riyadh triangle
        };

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            ArcenPoint center = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;

            int cityCount = CityPositions.GetLength( 0 );
            int edgeCount = CityEdges.GetLength( 0 );

            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( cityCount );

            for ( int i = 0; i < cityCount; i++ )
            {
                int x = center.X + CityPositions[i, 0];
                int y = center.Y + CityPositions[i, 1];
                Planet p = galaxy.AddPlanet( PlanetType.Normal, ArcenPoint.Create( x, y ),
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                allPlanets.Add( p );
            }

            BadgerUtilityMethods.EnforcePlanetMinimumSpacing( allPlanets, 50 );

            for ( int i = 0; i < edgeCount; i++ )
            {
                int a = CityEdges[i, 0];
                int b = CityEdges[i, 1];
                allPlanets[a].AddLinkTo( allPlanets[b] );
            }

            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }

    public class Mapgen_JapanCities : IMapGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        // Cities: { xOffset, yOffset } relative to galaxy center
        // Positions derived from lat/lon: xOffset = (lon - 137) * 70, yOffset = (lat - 36) * 55
        // Japan runs SW to NE; Okinawa/Ryukyus are far south, Hokkaido far north.
        // Korean cities (59-77) sit to the west, connected via two cross-strait ferry links.
        // Removed: Saitama (too close to Tokyo), Saga (too close to Fukuoka),
        //          Ishinomaki (too close to Sendai), Gifu (too close to Nagoya),
        //          Nara (too close to Kyoto).
        private static readonly int[,] CityPositions = new int[,]
        {
            // --- Japan ---
            // 0  Sapporo
            { 300, 390 },
            // 1  Asahikawa
            { 380, 430 },
            // 2  Hakodate
            { 260, 320 },
            // 3  Kushiro
            { 520, 385 },
            // 4  Obihiro
            { 435, 380 },
            // 5  Aomori
            { 261, 266 },
            // 6  Akita
            { 215, 205 },
            // 7  Morioka
            { 295, 205 },
            // 8  Sendai
            { 280, 125 },
            // 9  Yamagata
            { 230, 120 },
            // 10 Fukushima
            { 245, 100 },
            // 11 Niigata
            { 140, 105 },
            // 12 Utsunomiya
            { 200, 35 },
            // 13 Maebashi
            { 145, 20 },
            // 14 Tokyo
            { 191, -19 },
            // 15 Yokohama
            { 179, -36 },
            // 16 Chiba
            { 215, -20 },
            // 17 Nagano
            { 85, 40 },
            // 18 Toyama
            { 15, 40 },
            // 19 Kanazawa
            { -30, 35 },
            // 20 Shizuoka
            { 100, -60 },
            // 21 Hamamatsu
            { 50, -70 },
            // 22 Nagoya
            { -5, -45 },
            // 23 Kyoto
            { -85, -55 },
            // 24 Osaka
            { -105, -70 },
            // 25 Kobe
            { -125, -70 },
            // 26 Wakayama
            { -125, -100 },
            // 27 Okayama
            { -215, -70 },
            // 28 Hiroshima
            { -315, -90 },
            // 29 Matsue
            { -275, -30 },
            // 30 Tottori
            { -195, -30 },
            // 31 Takamatsu
            { -210, -95 },
            // 32 Kochi
            { -240, -130 },
            // 33 Matsuyama
            { -295, -120 },
            // 34 Tokushima
            { -170, -105 },
            // 35 Fukuoka
            { -460, -130 },
            // 36 Nagasaki
            { -495, -180 },
            // 37 Kumamoto
            { -440, -175 },
            // 38 Oita
            { -375, -160 },
            // 39 Miyazaki
            { -390, -225 },
            // 40 Kagoshima
            { -450, -240 },
            // 41 Naha (Okinawa)
            { -650, -540 },
            // 42 Wakkanai (northernmost Hokkaido)
            { 330, 515 },
            // 43 Abashiri (NE Hokkaido)
            { 510, 440 },
            // 44 Hirosaki
            { 244, 254 },
            // 45 Hachinohe
            { 315, 250 },
            // 46 Mito
            { 245, 20 },
            // 47 Kofu
            { 110, -15 },
            // 48 Matsumoto
            { 65, 10 },
            // 49 Tsu
            { -35, -70 },
            // 50 Himeji
            { -160, -65 },
            // 51 Yamaguchi
            { -385, -100 },
            // 52 Sasebo
            { -510, -155 },
            // 53 Beppu
            { -390, -145 },
            // 54 Yatsushiro
            { -450, -195 },
            // 55 Miyakonojo
            { -415, -235 },
            // 56 Amami Oshima (Ryukyu chain)
            { -525, -420 },
            // 57 Ishigaki (far southern Ryukyus)
            { -895, -645 },
            // 58 Iwaki (Pacific coast, Tohoku)
            { 275, 60 },
            // --- Korea ---
            // 59 Seoul
            { -700, 91 },
            // 60 Incheon
            { -730, 83 },
            // 61 Suwon
            { -700, 70 },
            // 62 Chuncheon
            { -650, 105 },
            // 63 Gangneung (east coast)
            { -565, 99 },
            // 64 Wonju
            { -635, 72 },
            // 65 Cheongju
            { -663, 37 },
            // 66 Cheonan
            { -685, 44 },
            // 67 Daejeon
            { -672, 18 },
            // 68 Daegu
            { -590, -5 },
            // 69 Pohang (east coast)
            { -530, 0 },
            // 70 Ulsan
            { -540, -22 },
            // 71 Busan
            { -555, -50 },
            // 72 Changwon
            { -580, -44 },
            // 73 Jeonju
            { -695, -11 },
            // 74 Gwangju
            { -705, -44 },
            // 75 Mokpo (southwest tip)
            { -740, -66 },
            // 76 Yeosu
            { -650, -72 },
            // 77 Jinju
            { -620, -44 },
        };

        // Edges: { cityA, cityB }
        private static readonly int[,] CityEdges = new int[,]
        {
            // Hokkaido internal
            { 0, 1 }, { 0, 2 }, { 0, 4 }, { 1, 3 }, { 1, 4 }, { 3, 4 },
            // Hokkaido to Honshu (Seikan Tunnel / ferry)
            { 2, 5 },
            // Tohoku
            { 5, 6 }, { 5, 7 }, { 6, 9 }, { 7, 8 }, { 8, 9 }, { 8, 10 },
            { 9, 11 }, { 10, 11 }, { 10, 12 },
            // Kanto
            { 12, 13 }, { 12, 14 }, // Utsunomiya -> Maebashi, Utsunomiya -> Tokyo
            { 13, 17 },              // Maebashi-Nagano
            { 14, 15 }, { 14, 16 }, { 14, 20 }, // Tokyo hub
            // Chubu
            { 11, 18 }, { 17, 18 }, { 17, 20 }, { 18, 19 }, { 19, 23 },
            { 19, 30 }, { 20, 21 }, { 21, 22 },
            // Kansai
            { 22, 23 }, { 23, 24 }, { 24, 25 }, { 24, 26 }, { 25, 26 }, { 25, 27 },
            // Chugoku (San'yo and San'in coasts)
            { 27, 28 }, { 27, 31 }, { 28, 29 }, { 28, 33 }, { 28, 35 }, { 29, 30 },
            // Shikoku
            { 30, 27 }, { 31, 32 }, { 31, 34 }, { 32, 33 }, { 34, 25 },
            // Kyushu
            { 35, 37 }, { 35, 38 }, { 37, 40 }, { 38, 39 }, { 39, 40 },
            // Ryukyu chain (Kagoshima -> Amami -> Naha -> Ishigaki)
            { 40, 56 }, { 56, 41 }, { 41, 57 },
            // Hokkaido extended
            { 42, 1 }, { 43, 1 }, { 43, 3 }, { 43, 4 },
            // Tohoku extended
            { 44, 5 }, { 44, 6 }, { 45, 5 }, { 45, 7 },
            // Kanto / Tohoku coast extended
            { 46, 12 }, { 46, 58 }, { 58, 10 }, { 47, 14 }, { 47, 17 },
            // Chubu extended
            { 48, 17 }, { 48, 18 }, { 49, 22 }, { 49, 26 },
            // Chugoku / Kansai extended
            { 50, 25 }, { 50, 27 }, { 51, 28 }, { 51, 35 },
            // Kyushu extended
            { 52, 35 }, { 52, 36 }, // Sasebo-Fukuoka, Sasebo-Nagasaki
            { 53, 38 }, { 54, 37 }, { 54, 40 }, { 55, 39 }, { 55, 40 },
            // Korea internal
            { 59, 60 }, { 59, 61 }, { 59, 62 }, { 59, 66 },
            { 61, 64 }, { 61, 66 },
            { 62, 63 }, { 62, 64 },
            { 63, 69 },
            { 64, 67 },
            { 65, 66 }, { 65, 67 }, { 66, 67 },
            { 67, 68 }, { 67, 73 },
            { 68, 69 }, { 68, 70 }, { 68, 72 },
            { 69, 70 }, { 70, 71 }, { 71, 72 }, { 72, 77 },
            { 73, 74 }, { 74, 75 }, { 74, 76 }, { 76, 77 },
            // Cross-strait links (Korea <-> Japan)
            { 71, 35 }, // Busan-Fukuoka (Korea Strait ferry)
            { 63, 11 }, // Gangneung-Niigata (Sea of Japan link)
        };

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            ArcenPoint center = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;

            int cityCount = CityPositions.GetLength( 0 );
            int edgeCount = CityEdges.GetLength( 0 );

            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( cityCount );

            for ( int i = 0; i < cityCount; i++ )
            {
                int x = center.X + CityPositions[i, 0];
                int y = center.Y + CityPositions[i, 1];
                Planet p = galaxy.AddPlanet( PlanetType.Normal, ArcenPoint.Create( x, y ),
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                allPlanets.Add( p );
            }

            BadgerUtilityMethods.EnforcePlanetMinimumSpacing( allPlanets, 50 );

            for ( int i = 0; i < edgeCount; i++ )
            {
                int a = CityEdges[i, 0];
                int b = CityEdges[i, 1];
                allPlanets[a].AddLinkTo( allPlanets[b] );
            }

            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }

    public class Mapgen_RomanEmpire : IMapGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        // Cities: { xOffset, yOffset } relative to galaxy center
        // Positions derived from lat/lon: xOffset = (lon - 15) * 38, yOffset = (lat - 48) * 35
        // Covers the full extent of the Roman Empire from Britannia to Mesopotamia.
        private static readonly int[,] CityPositions = new int[,]
        {
            // --- Britannia ---
            // 0  Londinium (London): lon=0, lat=51.5
            { -570, 122 },
            // 1  Eburacum (York): lon=-1, lat=54
            { -608, 210 },
            // 2  Deva (Chester): lon=-2.9, lat=53.2
            { -681, 182 },
            // --- Gaul / Rhine Frontier ---
            // 3  Lutetia (Paris): lon=2.35, lat=48.85
            { -481, 30 },
            // 4  Lugdunum (Lyon): lon=4.83, lat=45.75
            { -386, -79 },
            // 5  Massilia (Marseille): lon=5.37, lat=43.3
            { -366, -165 },
            // 6  Narbo (Narbonne): lon=3.0, lat=43.18
            { -456, -169 },
            // 7  Burdigala (Bordeaux): lon=-0.57, lat=44.84
            { -591, -111 },
            // 8  Colonia (Cologne): lon=6.96, lat=50.94
            { -305, 103 },
            // 9  Moguntiacum (Mainz): lon=8.27, lat=50.0
            { -256, 70 },
            // 10 Augusta Treverorum (Trier): lon=6.64, lat=49.75
            { -318, 61 },
            // --- Hispania ---
            // 11 Olisipo (Lisbon): lon=-9.14, lat=38.72
            { -918, -325 },
            // 12 Emerita Augusta (Merida): lon=-6.34, lat=38.92
            { -812, -318 },
            // 13 Hispalis (Seville): lon=-5.99, lat=37.39
            { -799, -371 },
            // 14 Caesaraugusta (Zaragoza): lon=-0.89, lat=41.65
            { -604, -222 },
            // 15 Barcino (Barcelona): lon=2.15, lat=41.39
            { -488, -231 },
            // 16 Carthago Nova (Cartagena): lon=-1.0, lat=37.6
            { -608, -364 },
            // --- Italia ---
            // 17 Mediolanum (Milan): lon=9.19, lat=45.46
            { -220, -89 },
            // 18 Aquileia: lon=13.37, lat=45.77
            { -62, -78 },
            // 19 Roma: lon=12.5, lat=41.9
            { -95, -214 },
            // 20 Neapolis (Naples): lon=14.27, lat=40.85
            { -27, -256 },
            // 21 Brundisium (Brindisi): lon=17.94, lat=40.64
            { 112, -258 },
            // 22 Panormus (Palermo): lon=13.36, lat=38.12
            { -62, -346 },
            // 23 Syracusae (Syracuse): lon=15.29, lat=37.07
            { 11, -383 },
            // 24 Capua: lon=14.21, lat=41.11
            { -31, -235 },
            // 25 Genua (Genoa): lon=8.93, lat=44.41
            { -231, -126 },
            // --- Africa (North) ---
            // 26 Carthago (Tunis): lon=10.17, lat=36.82
            { -184, -391 },
            // 27 Cirta (Constantine): lon=6.61, lat=36.37
            { -319, -407 },
            // 28 Caesarea Mauretaniae (Cherchell): lon=2.22, lat=36.61
            { -486, -399 },
            // 29 Tingis (Tangier): lon=-5.8, lat=35.78
            { -791, -428 },
            // 30 Leptis Magna: lon=14.29, lat=32.64
            { -27, -538 },
            // 31 Cyrene: lon=21.86, lat=32.83
            { 261, -531 },
            // 32 Alexandria: lon=29.92, lat=31.2
            { 567, -588 },
            // 33 Oea (Tripoli): lon=13.19, lat=32.9
            { -69, -529 },
            // --- Danube / Balkans ---
            // 34 Vindobona (Vienna): lon=16.37, lat=48.21
            { 52, 7 },
            // 35 Sirmium (Sremska Mitrovica): lon=19.62, lat=44.97
            { 176, -106 },
            // 36 Singidunum (Belgrade): lon=20.46, lat=44.82
            { 208, -112 },
            // 37 Naissus (Nis): lon=21.89, lat=43.32
            { 262, -164 },
            // 38 Thessalonica: lon=22.94, lat=40.64
            { 302, -258 },
            // 39 Philippopolis (Plovdiv): lon=24.75, lat=42.15
            { 371, -205 },
            // 40 Byzantium (Istanbul): lon=28.98, lat=41.01
            { 531, -244 },
            // 41 Athenae (Athens): lon=23.72, lat=37.98
            { 331, -351 },
            // --- Asia Minor ---
            // 42 Nicomedia (Izmit): lon=29.94, lat=40.77
            { 568, -253 },
            // 43 Ephesus (Selcuk): lon=27.35, lat=37.95
            { 469, -352 },
            // 44 Ancyra (Ankara): lon=32.86, lat=39.93
            { 679, -282 },
            // 45 Caesarea Mazaca (Kayseri): lon=35.49, lat=38.72
            { 779, -325 },
            // 46 Trapezus (Trabzon): lon=39.73, lat=41.0
            { 940, -245 },
            // --- East (Syria / Levant / Egypt) ---
            // 47 Antiochia (Antakya): lon=36.16, lat=36.2
            { 804, -413 },
            // 48 Berytus (Beirut): lon=35.5, lat=33.89
            { 779, -494 },
            // 49 Damascus: lon=36.3, lat=33.51
            { 809, -507 },
            // 50 Hierosolyma (Jerusalem): lon=35.22, lat=31.78
            { 768, -567 },
            // 51 Petra: lon=35.44, lat=30.33
            { 777, -618 },
            // 52 Memphis (Egypt): lon=31.25, lat=29.85
            { 618, -636 },
            // --- Additional nodes for connectivity ---
            // 53 Gessoriacum (Boulogne): lon=1.6, lat=50.7
            { -509, 95 },
            // 54 Augusta Raurica (Basel area): lon=7.72, lat=47.54
            { -277, -16 },
            // 55 Aquincum (Budapest): lon=19.05, lat=47.5
            { 154, -18 },
            // 56 Sinope: lon=35.15, lat=42.03
            { 766, -209 },
            // 57 Corinthus (Corinth): lon=22.93, lat=37.91
            { 302, -353 },
            // 58 Volubilis (Morocco): lon=-5.56, lat=34.07
            { -782, -487 },
            // 59 Caesarea Philippi (Banias): lon=35.69, lat=33.25
            { 790, -515 },
        };

        // Edges: { cityA, cityB }
        private static readonly int[,] CityEdges = new int[,]
        {
            // Britannia
            { 0, 1 }, { 0, 2 }, { 1, 2 },
            // Britannia to Gaul (Channel crossing)
            { 0, 53 }, { 53, 3 },
            // Gaul interior
            { 3, 7 }, { 3, 4 }, { 3, 10 }, { 7, 6 }, { 6, 5 }, { 4, 5 }, { 4, 25 },
            // Rhine frontier
            { 3, 8 }, { 8, 9 }, { 8, 10 }, { 9, 10 }, { 9, 54 }, { 9, 17 },
            // Hispania
            { 7, 14 }, { 6, 14 }, { 6, 15 }, { 15, 14 }, { 14, 12 }, { 14, 16 },
            { 12, 11 }, { 12, 13 }, { 11, 13 }, { 13, 29 }, { 28, 29 }, { 28, 58 },
            { 29, 58 }, { 16, 26 },
            // Italia spine (Via Appia etc.)
            { 25, 17 }, { 25, 5 }, { 17, 18 }, { 17, 34 }, { 17, 54 },
            { 18, 19 }, { 18, 35 }, { 19, 24 }, { 24, 20 }, { 20, 19 }, { 20, 21 },
            { 21, 35 }, { 22, 23 }, { 22, 26 }, { 23, 26 }, { 23, 21 },
            // Africa coastal road (west to east)
            { 29, 28 }, { 28, 27 }, { 27, 26 }, { 26, 33 }, { 33, 30 }, { 30, 31 }, { 31, 32 },
            // Africa cross links
            { 26, 5 },  // Carthago-Massilia (sea)
            { 22, 33 }, // Panormus-Oea (sea)
            { 32, 52 }, { 52, 50 }, { 52, 51 },
            // Danube frontier
            { 34, 55 }, { 34, 18 }, { 34, 36 }, { 55, 35 }, { 55, 36 },
            { 35, 36 }, { 36, 37 }, { 37, 39 }, { 37, 38 },
            // Balkans to Greece
            { 38, 57 }, { 38, 41 }, { 57, 41 }, { 39, 40 }, { 39, 38 }, { 40, 42 },
            { 41, 43 }, { 41, 31 }, // Athens-Cyrene (sea)
            // Asia Minor
            { 40, 42 }, { 42, 44 }, { 42, 43 }, { 43, 45 }, { 44, 45 }, { 44, 56 },
            { 45, 46 }, { 45, 47 }, { 46, 56 }, { 46, 44 },
            // East / Levant
            { 47, 48 }, { 47, 49 }, { 47, 45 }, { 48, 49 }, { 48, 50 }, { 49, 50 },
            { 49, 59 }, { 48, 59 }, { 50, 51 }, { 50, 59 },
            // Egypt connection
            { 32, 40 }, // Alexandria-Byzantium (sea)
            { 32, 41 }, // Alexandria-Athens (sea)
        };

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            ArcenPoint center = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;

            int cityCount = CityPositions.GetLength( 0 );
            int edgeCount = CityEdges.GetLength( 0 );

            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( cityCount );

            for ( int i = 0; i < cityCount; i++ )
            {
                int x = center.X + CityPositions[i, 0];
                int y = center.Y + CityPositions[i, 1];
                Planet p = galaxy.AddPlanet( PlanetType.Normal, ArcenPoint.Create( x, y ),
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                allPlanets.Add( p );
            }

            BadgerUtilityMethods.EnforcePlanetMinimumSpacing( allPlanets, 50 );

            for ( int i = 0; i < edgeCount; i++ )
            {
                int a = CityEdges[i, 0];
                int b = CityEdges[i, 1];
                allPlanets[a].AddLinkTo( allPlanets[b] );
            }

            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }

    public class Mapgen_IndiaCities : IMapGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        // Cities: { xOffset, yOffset } relative to galaxy center
        // Positions derived from lat/lon: xOffset = (lon - 78) * 60, yOffset = (lat - 22) * 55
        private static readonly int[,] CityPositions = new int[,]
        {
            // --- North (Punjab / J&K / Himachal / Uttarakhand) ---
            // 0  Amritsar: lon=74.87, lat=31.63
            { -188, 530 },
            // 1  Ludhiana: lon=75.85, lat=30.9
            { -129, 490 },
            // 2  Chandigarh: lon=76.78, lat=30.73
            { -73, 480 },
            // 3  Shimla: lon=77.17, lat=31.1
            { -50, 501 },
            // 4  Dehradun: lon=78.03, lat=30.32
            { 2, 458 },
            // 5  Srinagar: lon=74.8, lat=34.08
            { -192, 664 },
            // 6  Jammu: lon=74.86, lat=32.73
            { -188, 590 },
            // --- NCR / Uttar Pradesh ---
            // 7  Delhi: lon=77.21, lat=28.66
            { -47, 366 },
            // 8  Meerut: lon=77.7, lat=28.98
            { -18, 384 },
            // 9  Agra: lon=78.01, lat=27.18
            { 1, 285 },
            // 10 Bareilly: lon=79.43, lat=28.36
            { 86, 350 },
            // 11 Lucknow: lon=80.95, lat=26.85
            { 177, 267 },
            // 12 Kanpur: lon=80.35, lat=26.45
            { 141, 245 },
            // 13 Allahabad: lon=81.84, lat=25.45
            { 230, 190 },
            // 14 Varanasi: lon=82.97, lat=25.32
            { 298, 183 },
            // 15 Gorakhpur: lon=83.37, lat=26.76
            { 322, 262 },
            // --- Bihar / Bengal ---
            // 16 Patna: lon=85.14, lat=25.6
            { 428, 198 },
            // 17 Gaya: lon=85.0, lat=24.79
            { 420, 153 },
            // 18 Muzaffarpur: lon=85.39, lat=26.12
            { 443, 226 },
            // 19 Kolkata: lon=88.37, lat=22.57
            { 622, 31 },
            // 20 Asansol: lon=86.98, lat=23.68
            { 539, 92 },
            // 21 Siliguri: lon=88.43, lat=26.72
            { 626, 260 },
            // --- Northeast ---
            // 22 Guwahati: lon=91.74, lat=26.14
            { 824, 228 },
            // 23 Shillong: lon=91.88, lat=25.57
            { 833, 192 },
            // 24 Agartala: lon=91.28, lat=23.84
            { 797, 101 },
            // --- Rajasthan ---
            // 25 Jaipur: lon=75.79, lat=26.91
            { -133, 270 },
            // 26 Jodhpur: lon=73.02, lat=26.3
            { -299, 237 },
            // 27 Bikaner: lon=73.32, lat=28.02
            { -281, 331 },
            // 28 Ajmer: lon=74.64, lat=26.45
            { -202, 245 },
            // 29 Kota: lon=75.85, lat=25.18
            { -129, 175 },
            // 30 Udaipur: lon=73.72, lat=24.58
            { -257, 142 },
            // --- Gujarat ---
            // 31 Ahmedabad: lon=72.59, lat=23.03
            { -325, 57 },
            // 32 Surat: lon=72.83, lat=21.17
            { -310, -46 },
            // 33 Vadodara: lon=73.19, lat=22.31
            { -289, 17 },
            // 34 Rajkot: lon=70.8, lat=22.3
            { -432, 17 },
            // 35 Bhavnagar: lon=72.15, lat=21.77
            { -351, -13 },
            // --- Madhya Pradesh / Chhattisgarh ---
            // 36 Bhopal: lon=77.41, lat=23.26
            { -35, 69 },
            // 37 Indore: lon=75.86, lat=22.72
            { -128, 40 },
            // 38 Jabalpur: lon=79.94, lat=23.17
            { 116, 64 },
            // 39 Gwalior: lon=78.17, lat=26.22
            { 10, 232 },
            // 40 Jhansi: lon=78.57, lat=25.45
            { 34, 190 },
            // 41 Nagpur: lon=79.09, lat=21.15
            { 65, -47 },
            // 42 Raipur: lon=81.63, lat=21.25
            { 218, -41 },
            // --- Jharkhand / Odisha ---
            // 43 Ranchi: lon=85.33, lat=23.36
            { 440, 75 },
            // 44 Bhubaneswar: lon=85.82, lat=20.3
            { 467, -99 },
            // --- Maharashtra ---
            // 45 Mumbai: lon=72.88, lat=19.08
            { -307, -161 },
            // 46 Pune: lon=73.86, lat=18.52
            { -249, -191 },
            // 47 Nashik: lon=73.79, lat=20.0
            { -253, -110 },
            // 48 Aurangabad: lon=75.35, lat=19.88
            { -159, -116 },
            // 49 Kolhapur: lon=74.24, lat=16.7
            { -226, -292 },
            // 50 Amravati: lon=77.75, lat=20.93
            { -15, -59 },
            // 51 Nanded: lon=77.32, lat=19.15
            { -41, -157 },
            // 52 Solapur: lon=75.9, lat=17.68
            { -126, -238 },
            // --- Goa / Karnataka ---
            // 53 Goa (Panaji): lon=73.83, lat=15.49
            { -250, -358 },
            // 54 Mangalore: lon=74.86, lat=12.87
            { -188, -503 },
            // 55 Hubli: lon=75.12, lat=15.36
            { -173, -365 },
            // 56 Belgaum: lon=74.49, lat=15.86
            { -211, -337 },
            // 57 Bangalore: lon=77.6, lat=12.97
            { -24, -497 },
            // 58 Mysore: lon=76.64, lat=12.3
            { -82, -534 },
            // 59 Gulbarga: lon=76.82, lat=17.33
            { -71, -257 },
            // --- Andhra Pradesh / Telangana ---
            // 60 Hyderabad: lon=78.47, lat=17.38
            { 28, -254 },
            // 61 Warangal: lon=79.59, lat=18.0
            { 95, -220 },
            // 62 Vijayawada: lon=80.62, lat=16.52
            { 159, -300 },
            // 63 Visakhapatnam: lon=83.22, lat=17.69
            { 313, -237 },
            // 64 Guntur: lon=80.45, lat=16.3
            { 145, -316 },
            // 65 Tirupati: lon=79.42, lat=13.65
            { 85, -459 },
            // 66 Nellore: lon=79.98, lat=14.44
            { 119, -416 },
            // --- Kerala ---
            // 67 Kochi: lon=76.26, lat=9.93
            { -104, -664 },
            // 68 Calicut (Kozhikode): lon=75.78, lat=11.25
            { -133, -591 },
            // 69 Thrissur: lon=76.21, lat=10.52
            { -107, -629 },
            // 70 Trivandrum: lon=76.95, lat=8.49
            { -63, -743 },
            // --- Tamil Nadu ---
            // 71 Chennai: lon=80.28, lat=13.08
            { 137, -490 },
            // 72 Coimbatore: lon=76.97, lat=11.02
            { -62, -604 },
            // 73 Madurai: lon=78.12, lat=9.92
            { 7, -665 },
            // 74 Salem: lon=78.15, lat=11.66
            { 9, -569 },
            // 75 Trichy: lon=78.7, lat=10.79
            { 42, -616 },
            // 76 Tirunelveli: lon=77.7, lat=8.73
            { -18, -727 },
            // --- Additional Northeast / East ---
            // 77 Imphal: lon=93.95, lat=24.82
            { 957, 155 },
            // 78 Dhanbad: lon=86.43, lat=23.8
            { 506, 99 },
            // 79 Cuttack: lon=85.88, lat=20.46
            { 475, -80 },
        };

        // Edges: { cityA, cityB }
        private static readonly int[,] CityEdges = new int[,]
        {
            // J&K spine
            { 5, 6 }, { 6, 0 }, { 0, 1 },
            // Punjab / Himachal
            { 1, 2 }, { 2, 3 }, { 3, 4 }, { 2, 7 }, { 1, 7 }, { 0, 7 },
            // Dehradun to Delhi / UP
            { 4, 7 }, { 4, 8 }, { 7, 8 }, { 7, 9 }, { 7, 25 },
            // UP spine (Grand Trunk Road / NH)
            { 8, 10 }, { 9, 12 }, { 9, 40 }, { 10, 11 }, { 11, 12 }, { 11, 15 },
            { 12, 13 }, { 13, 14 }, { 14, 15 }, { 15, 16 },
            // Bihar / Bengal
            { 16, 17 }, { 16, 18 }, { 17, 20 }, { 18, 21 }, { 20, 19 }, { 20, 43 },
            { 19, 21 }, { 19, 24 }, { 19, 20 },
            // Northeast
            { 21, 22 }, { 22, 23 }, { 22, 77 }, { 23, 24 }, { 23, 77 },
            // Rajasthan links
            { 27, 0 }, { 27, 26 }, { 27, 28 }, { 26, 28 }, { 26, 30 }, { 26, 31 },
            { 28, 25 }, { 25, 7 }, { 25, 29 }, { 25, 9 }, { 29, 30 }, { 29, 37 },
            // Gujarat
            { 27, 34 }, { 34, 31 }, { 31, 33 }, { 31, 35 }, { 33, 32 }, { 35, 32 },
            { 32, 45 }, { 31, 37 },
            // MP / Chhattisgarh
            { 39, 7 }, { 39, 40 }, { 40, 12 }, { 40, 36 }, { 36, 37 }, { 36, 38 },
            { 36, 41 }, { 37, 30 }, { 38, 41 }, { 38, 43 }, { 41, 42 }, { 42, 38 },
            // Jharkhand / Odisha
            { 43, 17 }, { 43, 44 }, { 43, 78 }, { 78, 20 }, { 44, 79 }, { 79, 19 },
            // Maharashtra
            { 47, 33 }, { 47, 45 }, { 47, 48 }, { 45, 46 }, { 46, 48 }, { 46, 49 },
            { 48, 41 }, { 48, 51 }, { 48, 60 }, { 50, 41 }, { 50, 61 }, { 50, 51 },
            { 51, 60 }, { 51, 52 }, { 52, 49 }, { 52, 59 },
            // Goa / Karnataka
            { 53, 49 }, { 53, 56 }, { 53, 55 }, { 55, 56 }, { 55, 59 }, { 56, 49 },
            { 57, 58 }, { 57, 65 }, { 57, 54 }, { 58, 54 }, { 58, 72 }, { 59, 60 },
            // AP / Telangana
            { 60, 61 }, { 61, 62 }, { 61, 63 }, { 62, 64 }, { 64, 66 }, { 66, 71 },
            { 65, 66 }, { 65, 71 }, { 63, 44 },
            // Tamil Nadu
            { 71, 74 }, { 74, 57 }, { 74, 75 }, { 74, 65 }, { 75, 73 }, { 75, 72 },
            { 72, 68 }, { 68, 67 }, { 67, 69 }, { 69, 70 }, { 70, 73 }, { 73, 76 },
            { 76, 70 },
        };

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            ArcenPoint center = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;

            int cityCount = CityPositions.GetLength( 0 );
            int edgeCount = CityEdges.GetLength( 0 );

            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( cityCount );

            for ( int i = 0; i < cityCount; i++ )
            {
                int x = center.X + CityPositions[i, 0];
                int y = center.Y + CityPositions[i, 1];
                Planet p = galaxy.AddPlanet( PlanetType.Normal, ArcenPoint.Create( x, y ),
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                allPlanets.Add( p );
            }

            BadgerUtilityMethods.EnforcePlanetMinimumSpacing( allPlanets, 50 );

            for ( int i = 0; i < edgeCount; i++ )
            {
                int a = CityEdges[i, 0];
                int b = CityEdges[i, 1];
                allPlanets[a].AddLinkTo( allPlanets[b] );
            }

            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }

    public class Mapgen_AfricaCities : IMapGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        // Cities: { xOffset, yOffset } relative to galaxy center
        // Positions derived from lat/lon: xOffset = (lon - 17) * 40, yOffset = (lat - 5) * 45
        // Covers all of Africa from the Mediterranean to the Cape.
        private static readonly int[,] CityPositions = new int[,]
        {
            // --- North Africa (Morocco) ---
            // 0  Casablanca: lon=-7.59, lat=33.57
            { -982, 1286 },
            // 1  Rabat: lon=-6.83, lat=34.02
            { -953, 1351 },
            // 2  Tangier: lon=-5.8, lat=35.78
            { -912, 1385 },
            // 3  Fez: lon=-5.0, lat=34.05
            { -880, 1307 },
            // 4  Marrakech: lon=-8.01, lat=31.63
            { -1000, 1198 },
            // 5  Agadir: lon=-9.6, lat=30.42
            { -1064, 1144 },
            // 6  Oujda: lon=-1.9, lat=34.68
            { -756, 1336 },
            // --- Algeria ---
            // 7  Tlemcen: lon=-1.32, lat=34.88
            { -733, 1345 },
            // 8  Oran: lon=-0.64, lat=35.7
            { -706, 1382 },
            // 9  Algiers: lon=3.06, lat=36.73
            { -558, 1428 },
            // 10 Annaba: lon=7.76, lat=36.9
            { -370, 1436 },
            // 11 Constantine: lon=6.61, lat=36.37
            { -416, 1412 },
            // --- Tunisia ---
            // 12 Tunis: lon=10.18, lat=36.82
            { -273, 1432 },
            // 13 Sfax: lon=10.76, lat=34.74
            { -250, 1338 },
            // --- Libya ---
            // 14 Tripoli: lon=13.19, lat=32.9
            { -152, 1256 },
            // 15 Benghazi: lon=20.07, lat=32.12
            { 123, 1219 },
            // 16 Derna: lon=22.64, lat=32.77
            { 226, 1247 },
            // --- Egypt ---
            // 17 Alexandria: lon=29.92, lat=31.2
            { 517, 1179 },
            // 18 Cairo: lon=31.25, lat=30.06
            { 570, 1128 },
            // 19 Luxor: lon=32.64, lat=25.7
            { 626, 932 },
            // 20 Aswan: lon=32.9, lat=24.09
            { 636, 859 },
            // 21 Suez: lon=32.54, lat=29.97
            { 622, 1124 },
            // --- Sudan / Horn of Africa ---
            // 22 Khartoum: lon=32.56, lat=15.55
            { 622, 475 },
            // 23 Asmara: lon=38.93, lat=15.34
            { 877, 465 },
            // 24 Addis Ababa: lon=38.74, lat=9.02
            { 870, 181 },
            // 25 Dire Dawa: lon=41.87, lat=9.59
            { 995, 206 },
            // 26 Djibouti: lon=43.15, lat=11.59
            { 1046, 297 },
            // 27 Mogadishu: lon=45.34, lat=2.05
            { 1134, -133 },
            // --- East Africa ---
            // 28 Nairobi: lon=36.82, lat=-1.29
            { 793, -283 },
            // 29 Mombasa: lon=39.67, lat=-4.05
            { 907, -407 },
            // 30 Kampala: lon=32.58, lat=0.32
            { 623, -211 },
            // 31 Dar es Salaam: lon=39.27, lat=-6.79
            { 891, -530 },
            // 32 Dodoma: lon=35.74, lat=-6.17
            { 750, -503 },
            // 33 Zanzibar: lon=39.19, lat=-6.17
            { 888, -503 },
            // 34 Mwanza: lon=32.9, lat=-2.52
            { 636, -338 },
            // --- West Africa ---
            // 35 Dakar: lon=-17.44, lat=14.72
            { -1378, 437 },
            // 36 Banjul: lon=-16.58, lat=13.45
            { -1343, 380 },
            // 37 Conakry: lon=-13.71, lat=9.54
            { -1228, 204 },
            // 38 Freetown: lon=-13.23, lat=8.49
            { -1209, 157 },
            // 39 Monrovia: lon=-10.8, lat=6.3
            { -1112, 59 },
            // 40 Abidjan: lon=-4.03, lat=5.34
            { -841, 15 },
            // 41 Accra: lon=-0.19, lat=5.56
            { -688, 25 },
            // 42 Lome: lon=1.22, lat=6.14
            { -632, 51 },
            // 43 Cotonou: lon=2.42, lat=6.35
            { -583, 61 },
            // 44 Lagos: lon=3.38, lat=6.45
            { -545, 65 },
            // 45 Ibadan: lon=3.9, lat=7.39
            { -524, 108 },
            // 46 Kano: lon=8.52, lat=12.0
            { -339, 315 },
            // 47 Kaduna: lon=7.44, lat=10.52
            { -382, 248 },
            // 48 Abuja: lon=7.49, lat=9.06
            { -380, 183 },
            // 49 Enugu: lon=7.49, lat=6.46
            { -380, 66 },
            // 50 Port Harcourt: lon=7.0, lat=4.82
            { -400, -8 },
            // 51 Niamey: lon=2.12, lat=13.51
            { -596, 383 },
            // 52 Ouagadougou: lon=-1.52, lat=12.36
            { -741, 331 },
            // 53 Bamako: lon=-8.0, lat=12.65
            { -1000, 344 },
            // 54 Nouakchott: lon=-15.97, lat=18.08
            { -1319, 589 },
            // 55 Nouadhibou: lon=-17.04, lat=20.94
            { -1362, 717 },
            // --- Central Africa ---
            // 56 N'Djamena: lon=15.04, lat=12.12
            { -78, 321 },
            // 57 Abéché: lon=20.83, lat=13.83
            { 153, 397 },
            // 58 Bangui: lon=18.56, lat=4.36
            { 62, -29 },
            // 59 Douala: lon=9.7, lat=4.05
            { -292, -43 },
            // 60 Yaounde: lon=11.52, lat=3.87
            { -219, -51 },
            // 61 Libreville: lon=9.45, lat=0.39
            { -302, -208 },
            // 62 Brazzaville: lon=15.28, lat=-4.27
            { -73, -409 },
            // 63 Kinshasa: lon=15.31, lat=-4.32
            { -64, -427 },
            // 64 Kisangani: lon=25.19, lat=0.52
            { 328, -202 },
            // 65 Lubumbashi: lon=27.47, lat=-11.68
            { 419, -750 },
            // 66 Luanda: lon=13.24, lat=-8.84
            { -150, -623 },
            // --- South Sudan ---
            // 67 Juba: lon=31.58, lat=4.85
            { 583, -7 },
            // --- Southern Africa ---
            // 68 Lusaka: lon=28.29, lat=-15.42
            { 452, -920 },
            // 69 Harare: lon=31.05, lat=-17.83
            { 562, -1027 },
            // 70 Bulawayo: lon=28.58, lat=-20.15
            { 463, -1132 },
            // 71 Lilongwe: lon=33.79, lat=-13.97
            { 672, -854 },
            // 72 Blantyre: lon=35.0, lat=-15.79
            { 720, -938 },
            // 73 Maputo: lon=32.59, lat=-25.97
            { 624, -1394 },
            // 74 Beira: lon=34.84, lat=-19.84
            { 714, -1118 },
            // 75 Antananarivo: lon=47.54, lat=-18.91
            { 1222, -1076 },
            // 76 Johannesburg: lon=28.04, lat=-26.2
            { 442, -1404 },
            // 77 Pretoria: lon=28.19, lat=-25.75
            { 448, -1384 },
            // 78 Durban: lon=30.99, lat=-29.86
            { 560, -1569 },
            // 79 Cape Town: lon=18.42, lat=-33.93
            { 57, -1752 },
            // 80 Port Elizabeth: lon=25.57, lat=-33.96
            { 343, -1753 },
            // 81 East London: lon=27.87, lat=-32.98
            { 435, -1709 },
            // 82 Windhoek: lon=17.08, lat=-22.56
            { 3, -1240 },
            // 83 Gaborone: lon=25.91, lat=-24.65
            { 356, -1349 },
            // 84 Pointe-Noire: lon=11.86, lat=-4.78
            { -206, -440 },
        };

        // Edges: { cityA, cityB }
        private static readonly int[,] CityEdges = new int[,]
        {
            // Morocco coastal / interior
            { 0, 1 }, { 0, 4 }, { 1, 2 }, { 1, 3 }, { 2, 3 }, { 3, 6 }, { 4, 5 },
            { 4, 0 }, { 5, 4 },
            // Morocco-Algeria border
            { 6, 7 }, { 7, 8 }, { 8, 9 },
            // Algeria spine
            { 9, 11 }, { 9, 10 }, { 11, 10 }, { 11, 12 },
            // Tunisia
            { 12, 13 }, { 13, 14 },
            // Libya
            { 14, 15 }, { 15, 16 }, { 16, 17 },
            // Egypt
            { 17, 18 }, { 17, 21 }, { 18, 21 }, { 18, 19 }, { 19, 20 }, { 19, 22 },
            // Nile Valley to Sudan
            { 20, 22 }, { 22, 23 },
            // Horn of Africa
            { 23, 24 }, { 24, 25 }, { 25, 26 }, { 26, 27 }, { 24, 27 },
            // Sudan to East Africa
            { 22, 67 }, { 67, 30 }, { 67, 24 },
            // East Africa spine
            { 30, 34 }, { 34, 28 }, { 28, 29 }, { 28, 32 }, { 29, 31 }, { 31, 33 },
            { 33, 32 }, { 32, 34 },
            // East Africa southern links
            { 31, 72 }, { 32, 71 }, { 28, 30 },
            // West Africa: Senegal/Gambia
            { 35, 36 }, { 35, 54 }, { 35, 55 }, { 36, 37 }, { 54, 55 }, { 54, 53 },
            // West Africa: Guinea coast
            { 37, 38 }, { 38, 39 }, { 39, 40 }, { 40, 41 }, { 41, 42 }, { 42, 43 },
            { 43, 44 }, { 44, 45 },
            // Sahel: west to east
            { 53, 52 }, { 52, 51 }, { 51, 46 }, { 51, 56 }, { 52, 37 },
            // Nigeria / Niger
            { 45, 47 }, { 45, 48 }, { 45, 49 }, { 46, 47 }, { 47, 48 }, { 48, 49 },
            { 49, 50 }, { 50, 44 }, { 48, 56 },
            // Chad / Central Africa
            { 56, 57 }, { 56, 58 }, { 57, 23 }, { 57, 67 },
            // Gulf of Guinea coast
            { 50, 59 }, { 59, 60 }, { 60, 58 }, { 60, 61 }, { 61, 84 }, { 84, 62 },
            { 62, 63 }, { 63, 66 },
            // Congo Basin
            { 58, 64 }, { 64, 30 }, { 64, 65 }, { 63, 64 }, { 62, 63 },
            // Southern Congo / Angola
            { 66, 84 }, { 66, 82 }, { 65, 68 }, { 65, 71 },
            // Southern Africa: Zambia / Zimbabwe / Malawi
            { 68, 69 }, { 68, 70 }, { 68, 71 }, { 69, 74 }, { 69, 70 }, { 70, 83 },
            { 71, 72 }, { 72, 74 },
            // Mozambique / South Africa coast
            { 74, 73 }, { 73, 78 }, { 74, 75 },
            // South Africa
            { 76, 77 }, { 76, 78 }, { 76, 83 }, { 77, 83 }, { 78, 81 }, { 81, 80 },
            { 80, 79 }, { 79, 82 }, { 82, 76 },
            // Namibia to Zimbabwe
            { 82, 70 },
        };

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            ArcenPoint center = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;

            int cityCount = CityPositions.GetLength( 0 );
            int edgeCount = CityEdges.GetLength( 0 );

            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( cityCount );

            for ( int i = 0; i < cityCount; i++ )
            {
                int x = center.X + CityPositions[i, 0];
                int y = center.Y + CityPositions[i, 1];
                Planet p = galaxy.AddPlanet( PlanetType.Normal, ArcenPoint.Create( x, y ),
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                allPlanets.Add( p );
            }

            BadgerUtilityMethods.EnforcePlanetMinimumSpacing( allPlanets, 50 );

            for ( int i = 0; i < edgeCount; i++ )
            {
                int a = CityEdges[i, 0];
                int b = CityEdges[i, 1];
                allPlanets[a].AddLinkTo( allPlanets[b] );
            }

            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }

    public class Mapgen_BalkansMap : IMapGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }

        // xOffset = (lon - 22) * 65, yOffset = (lat - 42) * 70
        private static readonly int[,] CityPositions = new int[,]
        {
            // --- Pannonian Plain / Hungary ---
            // 0  Budapest
            { -257, 385 },
            // 1  Pecs
            { -245, 285 },
            // 2  Debrecen
            { -24, 387 },
            // 3  Miskolc
            { -79, 427 },
            // 4  Gyor (western gateway)
            { -284, 398 },
            // 5  Bucharest
            { 267, 170 },
            // 6  Craiova
            { 116, 162 },
            // 7  Galati
            { 393, 241 },
            // 8  Braila
            { 388, 229 },
            // 9  Constanta (Black Sea)
            { 432, 153 },
            // 10 Brasov
            { 233, 256 },
            // 11 Cluj-Napoca
            { 104, 334 },
            // 12 Ruse (Danube crossing)
            { 257, 130 },
            // 13 Pleven
            { 170, 99 },
            // 14 Belgrade (major hub)
            { -100, 196 },
            // 15 Novi Sad
            { -140, 228 },
            // 16 Subotica
            { -152, 287 },
            // --- Croatia / Slovenia ---
            // 17 Ljubljana (western entry)
            { -487, 284 },
            // 18 Maribor
            { -413, 319 },
            // 19 Zagreb
            { -391, 267 },
            // 20 Rijeka
            { -491, 233 },
            // 21 Zadar
            { -440, 148 },
            // 22 Split
            { -361, 106 },
            // 23 Dubrovnik
            { -254, 46 },
            // 24 Osijek
            { -215, 249 },
            // --- Bosnia-Herzegovina ---
            // 25 Banja Luka
            { -313, 194 },
            // 26 Sarajevo
            { -233, 130 },
            // 27 Mostar
            { -273, 94 },
            // 28 Tuzla
            { -217, 179 },
            // --- Serbia interior ---
            // 29 Nis (major junction)
            { -7, 92 },
            // 30 Kragujevac
            { -71, 141 },
            // 31 Vranje
            { -7, 39 },
            // --- Montenegro ---
            // 32 Podgorica
            { -178, 31 },
            // 33 Kotor
            { -210, 29 },
            // 34 Bar
            { -189, 6 },
            // --- Kosovo ---
            // 35 Pristina
            { -54, 47 },
            // 36 Prizren
            { -82, 15 },
            // --- North Macedonia ---
            // 37 Skopje
            { -37, 0 },
            // 38 Bitola
            { -43, -68 },
            // 39 Ohrid
            { -78, -62 },
            // --- Albania ---
            // 40 Shkoder
            { -162, 5 },
            // 41 Tirana
            { -142, -47 },
            // 42 Durres (Adriatic port)
            { -166, -48 },
            // 43 Vlore
            { -163, -107 },
            // 44 Gjirokaster (Greek border)
            { -121, -134 },
            // --- Bulgaria ---
            // 45 Sofia (major hub)
            { 86, 49 },
            // 46 Plovdiv
            { 179, 10 },
            // 47 Stara Zagora
            { 236, 30 },
            // 48 Varna (Black Sea)
            { 384, 84 },
            // 49 Burgas (Black Sea)
            { 356, 36 },
            // --- Greece mainland ---
            // 50 Thessaloniki (major hub)
            { 61, -95 },
            // 51 Kavala
            { 157, -61 },
            // 52 Alexandroupoli (Turkish border)
            { 252, -81 },
            // 53 Ioannina
            { -75, -163 },
            // 54 Larissa
            { 27, -165 },
            // 55 Volos
            { 62, -185 },
            // 56 Lamia
            { 28, -217 },
            // 57 Athens (major hub)
            { 112, -281 },
            // 58 Patras
            { -18, -263 },
            // 59 Tripoli (Peloponnese)
            { 25, -314 },
            // 60 Sparta
            { 45, -360 },
            // 61 Kalamata
            { -10, -360 },
            // --- Greek islands ---
            // 62 Corfu
            { -135, -167 },
            // 63 Kefalonia
            { -98, -267 },
            // 64 Zakynthos
            { -72, -295 },
            // 65 Crete / Heraklion
            { 204, -466 },
            // 66 Rhodes
            { 404, -390 },
            // 67 Lesbos
            { 296, -203 },
            // 68 Chios
            { 270, -254 },
            // 69 Samos
            { 324, -294 },
            // 70 Cyclades cluster
            { 208, -344 },
            // --- Turkey (European) ---
            // 71 Istanbul (bridge node)
            { 455, -70 },
            // 72 Edirne
            { 296, -23 },
            // 73 Tekirdag
            { 358, -71 },
            // 74 Canakkale (Dardanelles chokepoint)
            { 286, -130 },
            // --- Choke / crossing nodes ---
            // 75 Corinth (Peloponnese gateway - 2 connections)
            { 60, -284 },
            // 76 Adriatic crossing (Durres-Bari ferry, external edge)
            { -360, -91 },
            // 77 Bosphorus crossing (Istanbul - external edge to Asia)
            { 520, -63 },
        };

        // Edges: { cityA, cityB }
        private static readonly int[,] CityEdges = new int[,]
        {
            // Northwest entry (Gyor gateway)
            { 4, 17 }, { 4, 18 }, { 4, 0 },
            // Pannonian / Hungary
            { 0, 3 }, { 0, 2 }, { 3, 2 }, { 0, 1 }, { 0, 15 }, { 15, 16 },
            // Belgrade hub
            { 15, 14 }, { 1, 24 }, { 24, 14 }, { 24, 15 }, { 14, 25 }, { 14, 30 }, { 14, 29 }, { 14, 26 },
            // Romania
            { 2, 11 }, { 11, 10 }, { 6, 5 }, { 6, 10 }, { 6, 13 },
            { 10, 5 }, { 5, 12 }, { 5, 8 }, { 8, 7 }, { 8, 9 },
            { 9, 48 }, { 12, 13 }, { 12, 48 },
            // Bulgaria
            { 13, 45 }, { 13, 46 }, { 45, 29 }, { 45, 46 }, { 45, 50 },
            { 46, 47 }, { 47, 49 }, { 47, 72 }, { 48, 49 }, { 49, 71 },
            // Croatia / Slovenia
            { 17, 18 }, { 18, 19 }, { 19, 24 }, { 17, 20 }, { 19, 20 },
            { 20, 21 }, { 21, 22 }, { 22, 23 },
            // Bosnia-Herzegovina
            { 24, 28 }, { 24, 25 }, { 25, 26 }, { 25, 28 }, { 28, 26 }, { 26, 27 },
            { 26, 32 }, { 22, 26 }, { 22, 27 }, { 27, 23 }, { 30, 26 },
            // Serbia
            { 30, 29 }, { 29, 35 }, { 29, 31 }, { 31, 37 },
            // Montenegro
            { 23, 33 }, { 33, 32 }, { 32, 34 }, { 34, 40 }, { 32, 40 }, { 32, 35 },
            // Kosovo
            { 35, 36 }, { 36, 37 }, { 35, 37 },
            // North Macedonia
            { 37, 38 }, { 37, 39 }, { 38, 39 }, { 37, 50 }, { 38, 53 },
            // Albania
            { 40, 41 }, { 41, 42 }, { 42, 76 }, { 42, 43 }, { 44, 42 }, { 43, 44 },
            { 44, 53 }, { 39, 41 },
            // Thrace (Bulgaria to Greece)
            { 50, 51 }, { 51, 52 }, { 52, 72 },
            // Greece mainland
            { 50, 54 }, { 53, 54 }, { 53, 58 }, { 53, 62 },
            { 54, 55 }, { 55, 56 }, { 55, 57 }, { 56, 57 }, { 56, 58 },
            { 58, 75 }, { 75, 57 }, { 75, 59 }, { 58, 59 },
            { 57, 65 }, { 57, 70 }, { 57, 67 },
            { 59, 60 }, { 59, 61 }, { 60, 61 },
            // Greek islands
            { 62, 63 }, { 63, 64 }, { 64, 61 }, { 62, 43 },
            { 65, 66 }, { 65, 70 }, { 66, 69 }, { 67, 68 }, { 68, 69 }, { 68, 74 }, { 58, 63 },
            // Turkey European
            { 71, 73 }, { 72, 73 }, { 73, 74 }, { 71, 77 },
        };

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            ArcenPoint center = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;
            int cityCount = CityPositions.GetLength( 0 );
            int edgeCount = CityEdges.GetLength( 0 );
            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( cityCount );
            for ( int i = 0; i < cityCount; i++ )
            {
                int x = center.X + CityPositions[i, 0];
                int y = center.Y + CityPositions[i, 1];
                Planet p = galaxy.AddPlanet( PlanetType.Normal, ArcenPoint.Create( x, y ),
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                allPlanets.Add( p );
            }
            BadgerUtilityMethods.EnforcePlanetMinimumSpacing( allPlanets, 50 );
            for ( int i = 0; i < edgeCount; i++ )
            {
                int a = CityEdges[i, 0];
                int b = CityEdges[i, 1];
                allPlanets[a].AddLinkTo( allPlanets[b] );
            }
            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }

    public class Mapgen_PhilippinesMap : IMapGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }

        // xOffset = (lon - 122) * 110, yOffset = (lat - 9) * 120
        private static readonly int[,] CityPositions = new int[,]
        {
            // --- Metro Manila / Central Luzon (dense hub) ---
            // 0  Metro Manila
            { -112, 672 },
            // 1  Quezon City / northern Metro
            { -95, 700 },
            // 2  Cavite / southern Metro
            { -121, 646 },
            // 3  Bulacan / northern approach
            { -113, 730 },
            // 4  Pampanga
            { -143, 769 },
            // 5  Bataan Peninsula (chokepoint)
            { -202, 724 },
            // 6  Batangas
            { -103, 571 },
            // 7  Laguna
            { -91, 634 },
            // 8  Rizal / eastern Metro
            { -78, 672 },
            // --- Northern Luzon ---
            // 9  Nueva Ecija
            { -102, 782 },
            // 10 Pangasinan
            { -195, 827 },
            // 11 La Union
            { -184, 913 },
            // 12 Baguio (mountain hub)
            { -154, 889 },
            // 13 Ilocos Sur
            { -179, 1024 },
            // 14 Ilocos Norte (northern edge)
            { -160, 1102 },
            // 15 Cagayan (northeastern)
            { -24, 1033 },
            // 16 Isabela
            { -20, 930 },
            // 17 Aurora (Pacific coast)
            { -37, 816 },
            // 18 Mountain Province / Ifugao
            { -116, 936 },
            // --- Southern Luzon ---
            // 19 Quezon Province
            { -12, 590 },
            // 20 Camarines Sur / Naga
            { 129, 554 },
            // 21 Albay / Legazpi
            { 188, 497 },
            // 22 Sorsogon (tip - Samar crossing)
            { 218, 476 },
            // 23 Masbate (island - Luzon/Visayas bridge)
            { 178, 404 },
            // 24 Romblon (island)
            { 25, 430 },
            // 25 Mindoro / Calapan
            { -90, 529 },
            // --- Palawan (linear chain) ---
            // 26 Puerto Princesa (hub)
            { -360, 89 },
            // 27 Coron / Busuanga
            { -220, 360 },
            // 28 El Nido (northern tip)
            { -284, 263 },
            // 29 Southern Palawan / Balabac (edge)
            { -393, -32 },
            // --- Visayas - Cebu cluster ---
            // 30 Cebu City (hub)
            { 209, 160 },
            // 31 Lapu-Lapu / Mactan
            { 232, 148 },
            // 32 Bohol / Tagbilaran
            { 205, 78 },
            // 33 Siquijor (edge island)
            { 165, 25 },
            // 34 Dumaguete / Negros Oriental
            { 143, 37 },
            // 35 Bacolod / Negros Occidental
            { 105, 202 },
            // 36 Guimaras
            { 66, 192 },
            // 37 Iloilo City (hub)
            { 62, 244 },
            // 38 Capiz
            { 69, 346 },
            // 39 Aklan / Boracay
            { 7, 348 },
            // 40 Antique (western Panay)
            { 19, 284 },
            // --- Visayas - Eastern (Samar / Leyte) ---
            // 41 Tacloban / Leyte (hub)
            { 329, 269 },
            // 42 Samar / Catbalogan
            { 317, 334 },
            // 43 Eastern Samar / Borongan (Pacific coast)
            { 387, 292 },
            // 44 Northern Samar / Catarman
            { 292, 418 },
            // 45 Southern Leyte / Maasin
            { 330, 137 },
            // 46 Biliran (small island)
            { 281, 312 },
            // --- Mindanao - Northern / Western ---
            // 47 Cagayan de Oro (hub)
            { 292, -62 },
            // 48 Iligan
            { 245, -92 },
            // 49 Lanao del Sur / Marawi
            { 220, -132 },
            // 50 Misamis Oriental
            { 330, -48 },
            // 51 Misamis Occidental
            { 204, -77 },
            // 52 Zamboanga City (western hub)
            { 7, -252 },
            // 53 Basilan (island)
            { 20, -321 },
            // --- Mindanao - Central ---
            // 54 Cotabato City / Maguindanao
            { 246, -216 },
            // 55 Sultan Kudarat
            { 255, -275 },
            // 56 General Santos (southern hub)
            { 348, -347 },
            // 57 Sarangani (southern coast edge)
            { 379, -376 },
            // 58 South Cotabato
            { 300, -319 },
            // --- Mindanao - Davao ---
            // 59 Davao City (hub)
            { 397, -232 },
            // 60 Davao del Norte
            { 436, -175 },
            // 61 Davao Occidental (edge)
            { 393, -305 },
            // 62 Compostela Valley
            { 446, -154 },
            // --- Mindanao - Northeast ---
            // 63 Surigao City
            { 385, 94 },
            // 64 Surigao del Sur / Tandag
            { 440, -16 },
            // 65 Agusan del Norte / Butuan
            { 388, -6 },
            // 66 Agusan del Sur (interior)
            { 376, -144 },
            // --- Sulu Archipelago (trailing chain) ---
            // 67 Jolo / Sulu
            { -111, -354 },
            // 68 Tawi-Tawi
            { -250, -470 },
            // 69 Turtle Islands (southern edge)
            { -412, -590 },
            // --- Sea strait / crossing nodes ---
            // 70 San Bernardino Strait (Luzon-Samar crossing)
            { 280, 440 },
            // 71 Surigao Strait (Leyte-Mindanao crossing)
            { 374, 100 },
        };

        // Edges: { cityA, cityB }
        private static readonly int[,] CityEdges = new int[,]
        {
            // Metro Manila dense cluster
            { 0, 1 }, { 0, 2 }, { 0, 3 }, { 0, 7 }, { 0, 8 }, { 1, 3 }, { 1, 4 },
            { 2, 6 }, { 2, 7 }, { 3, 4 }, { 4, 5 }, { 4, 9 }, { 4, 10 }, { 5, 10 },
            { 6, 7 }, { 7, 8 }, { 7, 19 }, { 8, 9 },
            // Northern Luzon chain
            { 9, 10 }, { 9, 16 },
            { 10, 11 }, { 10, 12 }, { 11, 13 }, { 12, 18 },
            { 13, 14 }, { 14, 15 }, { 15, 16 }, { 16, 17 }, { 17, 19 },
            // Southern Luzon chain
            { 8, 19 }, { 19, 20 }, { 20, 21 }, { 21, 22 },
            // Sorsogon to Samar (San Bernardino Strait - main Luzon/Visayas gap)
            { 22, 70 }, { 70, 42 }, { 70, 44 },
            // Masbate and Romblon as island bridges
            { 20, 23 }, { 23, 24 }, { 23, 30 }, { 24, 37 }, { 24, 35 },
            // Mindoro to Manila and Coron
            { 25, 0 }, { 25, 19 }, { 25, 27 }, { 27, 28 }, { 27, 26 },
            // Palawan chain
            { 26, 29 }, { 26, 37 },
            // Southern Palawan to Sulu Sea
            { 29, 68 },
            // Visayas Cebu cluster (internal)
            { 30, 31 }, { 30, 32 }, { 30, 34 }, { 30, 35 }, { 30, 45 },
            { 32, 33 }, { 33, 34 }, { 35, 36 }, { 35, 37 }, { 36, 37 },
            { 37, 38 }, { 37, 40 }, { 38, 39 }, { 38, 40 }, { 39, 40 },
            // Cebu to Leyte / Eastern Visayas
            { 32, 41 },
            // Eastern Visayas internal
            { 41, 42 }, { 41, 45 }, { 41, 46 }, { 42, 43 }, { 42, 44 }, { 42, 46 },
            // Northern Samar to Capiz sea crossing
            { 44, 38 },
            // Eastern Samar to Surigao del Sur (Pacific coast)
            { 43, 64 },
            // Leyte to Mindanao (Surigao Strait)
            { 45, 71 }, { 71, 63 }, { 71, 47 },
            // Mindanao north coast
            { 47, 48 }, { 47, 50 }, { 47, 65 },
            { 48, 49 }, { 48, 51 }, { 49, 51 }, { 49, 52 },
            // Zamboanga corridor
            { 51, 52 }, { 52, 53 }, { 53, 67 },
            // Mindanao central
            { 54, 49 }, { 54, 51 }, { 54, 55 }, { 55, 58 }, { 56, 58 }, { 56, 57 },
            { 57, 61 }, { 58, 61 }, { 59, 56 }, { 59, 61 }, { 59, 60 },
            // Mindanao Davao cluster
            { 60, 62 }, { 60, 47 }, { 62, 63 }, { 62, 64 },
            // Mindanao northeast
            { 63, 64 }, { 64, 65 }, { 65, 66 }, { 66, 54 }, { 66, 55 },
            // Sulu chain
            { 67, 68 }, { 68, 69 },
        };

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            ArcenPoint center = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;
            int cityCount = CityPositions.GetLength( 0 );
            int edgeCount = CityEdges.GetLength( 0 );
            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( cityCount );
            for ( int i = 0; i < cityCount; i++ )
            {
                int x = center.X + CityPositions[i, 0];
                int y = center.Y + CityPositions[i, 1];
                Planet p = galaxy.AddPlanet( PlanetType.Normal, ArcenPoint.Create( x, y ),
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                allPlanets.Add( p );
            }
            BadgerUtilityMethods.EnforcePlanetMinimumSpacing( allPlanets, 50 );
            for ( int i = 0; i < edgeCount; i++ )
            {
                int a = CityEdges[i, 0];
                int b = CityEdges[i, 1];
                allPlanets[a].AddLinkTo( allPlanets[b] );
            }
            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }

    public class Mapgen_WarringStatesMap : IMapGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }

        // xOffset = (lon - 112) * 60, yOffset = (lat - 32) * 65
        // Qin exits only at Hangu, Wuguan, and Xiaoguan Passes — the hard chokepoints.
        private static readonly int[,] CityPositions = new int[,]
        {
            // --- Qin (northwest fortress) ---
            // 0  Xianyang (capital)
            { -186, 150 },
            // 1  Yong (old capital, western interior)
            { -270, 176 },
            // 2  Longxi (western frontier edge)
            { -504, 195 },
            // 3  Guanzhong Valley interior
            { -240, 130 },
            // 4  Hanzhong (Qin-Chu border pass)
            { -300, 71 },
            // 5  Ba / Chongqing (Sichuan gateway)
            { -324, -156 },
            // 6  Shu / Chengdu (isolated Sichuan basin)
            { -474, -85 },
            // 7  Bashu northern gateway
            { -360, -33 },
            // --- Chu (south - largest, sparse) ---
            // 8  Ying / Jiangling (original capital)
            { 12, -104 },
            // 9  Wan / Nanyang (northern Chu)
            { 30, 65 },
            // 10 Chen (eastern capital)
            { 180, 117 },
            // 11 Shouchun (final capital)
            { 270, 39 },
            // 12 Jiangnan / lower Yangtze
            { 450, -13 },
            // 13 Changsha (southern Chu)
            { 60, -247 },
            // 14 Hangzhou / Yue coast
            { 492, -110 },
            // 15 Eastern Chu lowlands
            { 330, 13 },
            // --- Zhao (north-central) ---
            // 16 Handan (capital - central hub)
            { 150, 299 },
            // 17 Jinyang / Taiyuan (northern stronghold)
            { 36, 383 },
            // 18 Dai (far north edge)
            { 120, 553 },
            // 19 Zhongshan (buffer state)
            { 174, 409 },
            // 20 Julu
            { 180, 338 },
            // --- Wei (central - most contested) ---
            // 21 Daliang / Kaifeng (capital - central hub)
            { 138, 182 },
            // 22 Anyi (old capital, west of Yellow River)
            { -60, 195 },
            // 23 Ye (northern Wei)
            { 150, 279 },
            // 24 Puyang (eastern)
            { 180, 240 },
            // 25 Henei / Yellow River bend
            { 78, 208 },
            // --- Han (smallest major kingdom) ---
            // 26 Xinzheng (capital)
            { 102, 156 },
            // 27 Yangzhai (old capital)
            { 60, 137 },
            // 28 Yiyang
            { 30, 104 },
            // 29 Shangdang Plateau (strategic highland - chokepoint)
            { 48, 253 },
            // 30 Yingyang (Yellow River crossing)
            { 84, 188 },
            // --- Qi (east - coastal, wealthiest) ---
            // 31 Linzi (capital - eastern hub)
            { 384, 312 },
            // 32 Jimo (eastern coast edge)
            { 504, 286 },
            // 33 Ju (southern Qi)
            { 408, 234 },
            // 34 Pingyuan (northern Qi)
            { 270, 338 },
            // 35 Langya (southeastern coast edge)
            { 450, 253 },
            // --- Yan (northeast - isolated) ---
            // 36 Ji / Yanjing (capital - future Beijing)
            { 264, 512 },
            // 37 Xiadu (southern capital)
            { 222, 448 },
            // 38 Liaodong (eastern frontier edge)
            { 600, 630 },
            // 39 Shanggu (northern frontier)
            { 210, 553 },
            // 40 Yuyang
            { 300, 540 },
            // --- Minor / Buffer States ---
            // 41 Song (between Qi, Wei, Chu)
            { 222, 163 },
            // 42 Lu (between Qi and Chu)
            { 300, 234 },
            // 43 Wey (tiny surviving state)
            { 180, 222 },
            // 44 Bohai coast (Yan eastern territory)
            { 420, 488 },
            // --- River Crossing / Pass Nodes (chokepoints) ---
            // 45 Hangu Pass (Qin's eastern gate - THE critical chokepoint, 2 conn)
            { -73, 163 },
            // 46 Wuguan Pass (Qin's southern gate to Chu, 2 conn)
            { -60, 104 },
            // 47 Xiaoguan Pass (Qin's northern gate, 2 conn)
            { -330, 293 },
            // 48 Mengjin Ford (Yellow River - Wei/Han crossing)
            { 72, 189 },
            // 49 Puban Ford (Yellow River - Qin/Wei crossing)
            { -132, 189 },
            // 50 Xiling Gorge (Yangtze - Qin/Chu crossing)
            { -60, -79 },
            // 51 Jing-Xing Pass (Zhao-Yan border)
            { 108, 403 },
            // --- Central Plains Battleground (dense) ---
            // 52 Zhongyuan / Central Plains
            { 240, 98 },
            // 53 Changping (famous battle site - Zhao/Qin confrontation)
            { -12, 293 },
            // 54 Huaibei
            { 294, 104 },
            // 55 Pengcheng / Xuzhou (5 connections - strategic junction)
            { 330, 163 },
            // 56 Xiangyang corridor (Qin-Chu approach)
            { 6, 7 },
            // --- Northern Frontier (Xiongnu-facing, linear) ---
            // 57 Yunzhong (Zhao frontier fort)
            { -60, 520 },
            // 58 Jiuyuan (steppe edge)
            { -120, 565 },
            // 59 Yanmen Pass (Xiongnu chokepoint)
            { 48, 494 },
            // 60 Yan northern wall
            { 330, 585 },
            // 61 Shanhai corridor (Yan coast)
            { 462, 520 },
            // --- Yangtze / Southern Zones ---
            // 62 Jiangling crossing (Yangtze corridor)
            { -30, -130 },
            // 63 Huai River corridor
            { 318, 59 },
            // 64 Hefei region (Chu/Wei border)
            { 300, -33 },
            // 65 Yue heartland (southeast)
            { 480, -195 },
            // 66 Minyue coast (far southeast edge)
            { 438, -384 },
            // 67 Nanhai / Canton (far south edge)
            { 0, -618 },
            // 68 Bashu south (Yunnan gateway)
            { -450, -390 },
            // --- Frontier / Tribal Zones (edge nodes) ---
            // 69 Rong tribal territories (northwest edge)
            { -540, 260 },
            // 70 Di tribal territories (north edge)
            { 0, 585 },
            // 71 Xiongnu steppe (far north dead end)
            { 0, 715 },
            // 72 Baiyue territories (deep south edge)
            { 90, -553 },
            // 73 Gojoseon border (Yan far east edge)
            { 720, 618 },
            // --- Late-Period Strategic Nodes ---
            // 74 Nanyang corridor (Chu/Han/Wei junction)
            { 78, 78 },
            // 75 Yanzhao border area
            { 210, 370 },
            // 76 Jibei (Qi/Yan border)
            { 300, 390 },
            // 77 Runan (Chu/Wei/Han junction)
            { 120, 39 },
            // 78 Chenliu (central Wei road hub)
            { 192, 189 },
            // 79 Tao (Song/Wei junction - frequently contested)
            { 252, 215 },
        };

        // Edges: { cityA, cityB }
        private static readonly int[,] CityEdges = new int[,]
        {
            // Qin interior (fortress topology)
            { 0, 1 }, { 0, 3 }, { 0, 4 }, { 1, 2 }, { 1, 4 }, { 3, 1 },
            { 4, 7 }, { 7, 5 }, { 5, 6 }, { 7, 6 }, { 2, 69 },
            // Qin's three exits (THE CHOKEPOINTS)
            { 0, 45 }, { 3, 45 }, // → Hangu Pass (eastern)
            { 4, 46 },             // → Wuguan Pass (southern)
            { 1, 47 }, { 0, 47 }, // → Xiaoguan Pass (northern)
            { 5, 50 },             // → Xiling Gorge (Yangtze)
            { 0, 49 }, { 22, 49 }, // Puban Ford (Qin/Wei Yellow River)
            // Pass exits into other kingdoms
            { 45, 27 }, { 45, 74 },  // Hangu → Han territory
            { 46, 9 }, { 46, 28 },   // Wuguan → Chu / Han
            { 47, 58 },              // Xiaoguan → steppe frontier
            { 50, 8 }, { 50, 62 },   // Xiling → Chu
            { 49, 22 }, { 49, 25 },  // Puban Ford → Wei
            // Chu internal
            { 8, 9 }, { 8, 13 }, { 8, 56 }, { 8, 62 },
            { 9, 10 }, { 9, 28 }, { 9, 74 }, { 9, 77 },
            { 10, 11 }, { 10, 52 }, { 10, 74 },
            { 11, 12 }, { 11, 15 }, { 11, 63 }, { 11, 77 },
            { 12, 14 }, { 12, 63 },
            { 13, 56 }, { 13, 67 }, { 13, 72 },
            { 14, 65 }, { 65, 66 }, { 66, 67 }, { 67, 72 },
            { 63, 64 }, { 64, 15 }, { 77, 56 },
            // Wei (dense central plains)
            { 21, 22 }, { 21, 23 }, { 21, 24 }, { 21, 25 }, { 21, 26 }, { 21, 27 },
            { 21, 30 }, { 21, 78 }, { 22, 25 }, { 22, 29 }, { 22, 53 },
            { 23, 16 }, { 23, 24 }, { 23, 20 },
            { 24, 43 }, { 24, 79 }, { 25, 27 },
            { 78, 30 }, { 78, 41 }, { 79, 43 }, { 79, 55 },
            { 48, 26 },
            // Han
            { 26, 27 }, { 26, 28 }, { 26, 30 },
            { 27, 28 }, { 28, 9 }, { 28, 74 },
            { 74, 26 }, { 74, 77 },
            // Shangdang strategic highland
            { 29, 22 }, { 29, 53 }, { 29, 16 }, { 29, 17 }, { 29, 51 },
            // Jing-Xing Pass
            { 51, 17 }, { 51, 19 }, { 51, 37 },
            // Zhao
            { 16, 20 }, { 16, 17 }, { 16, 34 }, { 16, 23 },
            { 17, 53 }, { 17, 57 }, { 17, 59 },
            { 19, 37 }, { 19, 51 }, { 19, 75 },
            { 20, 24 }, { 20, 43 }, { 37, 75 },
            // Qi
            { 31, 32 }, { 31, 33 }, { 31, 34 }, { 31, 35 }, { 31, 42 }, { 31, 55 },
            { 32, 35 }, { 33, 42 }, { 33, 52 }, { 34, 76 }, { 35, 55 },
            { 42, 55 }, { 76, 37 },
            // Yan
            { 36, 37 }, { 36, 39 }, { 36, 40 }, { 36, 75 },
            { 37, 44 }, { 39, 57 }, { 39, 59 }, { 39, 70 },
            { 40, 60 }, { 40, 75 }, { 60, 61 }, { 60, 75 },
            { 44, 61 }, { 38, 61 }, { 38, 73 }, { 61, 73 },
            // Buffer states
            { 41, 78 }, { 41, 79 }, { 42, 33 }, { 42, 54 }, { 43, 79 },
            { 52, 54 }, { 52, 77 }, { 52, 41 },
            { 54, 55 }, { 55, 15 }, { 55, 31 },
            // Northern frontier (linear)
            { 57, 58 }, { 57, 59 }, { 58, 69 },
            { 59, 18 }, { 59, 70 }, { 18, 39 }, { 18, 36 }, { 70, 71 },
            // Southern periphery
            { 68, 5 }, { 68, 6 }, { 68, 67 },
            // Changping crosslinks
            { 53, 29 },
        };

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            ArcenPoint center = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;
            int cityCount = CityPositions.GetLength( 0 );
            int edgeCount = CityEdges.GetLength( 0 );
            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( cityCount );
            for ( int i = 0; i < cityCount; i++ )
            {
                int x = center.X + CityPositions[i, 0];
                int y = center.Y + CityPositions[i, 1];
                Planet p = galaxy.AddPlanet( PlanetType.Normal, ArcenPoint.Create( x, y ),
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                allPlanets.Add( p );
            }
            BadgerUtilityMethods.EnforcePlanetMinimumSpacing( allPlanets, 50 );
            for ( int i = 0; i < edgeCount; i++ )
            {
                int a = CityEdges[i, 0];
                int b = CityEdges[i, 1];
                allPlanets[a].AddLinkTo( allPlanets[b] );
            }
            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }

    public class Mapgen_IndonesiaMap : IMapGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }

        // xOffset = (lon - 118) * 40, yOffset = (lat + 3) * 75
        // Java is the dense hub; Palu in Sulawesi links all 4 peninsulas; Papua is vast and sparse.
        private static readonly int[,] CityPositions = new int[,]
        {
            // --- Java (dense internal chain) ---
            // 0  Jakarta / Batavia (western hub)
            { -446, -241 },
            // 1  Serang / Banten (northwestern tip — Sunda crossing)
            { -474, -234 },
            // 2  Bogor
            { -448, -270 },
            // 3  Bandung
            { -415, -293 },
            // 4  Cirebon (north coast corridor)
            { -378, -281 },
            // 5  Tegal
            { -355, -290 },
            // 6  Semarang (central hub)
            { -303, -298 },
            // 7  Yogyakarta
            { -305, -360 },
            // 8  Solo / Surakarta
            { -286, -342 },
            // 9  Magelang
            { -311, -335 },
            // 10 Surabaya (eastern hub)
            { -210, -319 },
            // 11 Malang
            { -215, -373 },
            // 12 Jember
            { -172, -388 },
            // 13 Blitar
            { -233, -383 },
            // 14 Banyuwangi (eastern tip — Bali crossing)
            { -145, -392 },
            // 15 Madura Island
            { -187, -307 },
            // --- Bali ---
            // 16 Denpasar / South Bali (gateway hub)
            { -111, -424 },
            // 17 Singaraja / North Bali
            { -116, -383 },
            // --- Lombok / Sumbawa ---
            // 18 Mataram / Lombok
            { -75, -419 },
            // 19 Bima / Sumbawa
            { 29, -409 },
            // --- Flores / Timor / Nusa Tenggara ---
            // 20 Labuan Bajo / West Flores
            { 76, -410 },
            // 21 Ende / Central Flores
            { 146, -438 },
            // 22 Kupang / West Timor (hub for eastern chain)
            { 223, -539 },
            // 23 East Timor / Dili (edge node)
            { 303, -417 },
            // 24 Sumba / Waingapu (isolated)
            { 90, -500 },
            // --- Sumatra (north-south corridor) ---
            // 25 Banda Aceh (northwestern tip edge)
            { -908, 641 },
            // 26 Medan (northern hub)
            { -773, 494 },
            // 27 Padang Sidempuan
            { -749, 329 },
            // 28 Padang (western coast hub)
            { -705, 154 },
            // 29 Bukittinggi
            { -705, 202 },
            // 30 Pekanbaru (central hub)
            { -662, 263 },
            // 31 Batam / Riau Islands (Singapore gateway)
            { -559, 309 },
            // 32 Tanjung Pinang
            { -542, 294 },
            // 33 Jambi
            { -576, 105 },
            // 34 Palembang (southern hub)
            { -530, 1 },
            // 35 Bengkulu (isolated west coast)
            { -630, -60 },
            // 36 Bandar Lampung (southern tip — Java crossing)
            { -509, -184 },
            // --- Kalimantan / Borneo (sparse) ---
            // 37 Pontianak (western coast)
            { -347, 225 },
            // 38 Ketapang
            { -320, 90 },
            // 39 Palangkaraya (central river hub)
            { -162, 60 },
            // 40 Banjarmasin (southern hub)
            { -137, -24 },
            // 41 Samarinda (eastern hub)
            { -34, 188 },
            // 42 Balikpapan (eastern port)
            { -47, 131 },
            // 43 Tarakan (northern coast)
            { -15, 473 },
            // 44 Nunukan (Malaysia border edge)
            { -13, 536 },
            // --- Sulawesi (spider shape) ---
            // 45 Makassar / Ujung Pandang (southern hub)
            { 57, -161 },
            // 46 Pare-Pare (western peninsula)
            { 65, -76 },
            // 47 Palopo (eastern peninsula south)
            { 88, 1 },
            // 48 Kendari (southeastern peninsula)
            { 181, -73 },
            // 49 Kolaka
            { 143, -80 },
            // 50 Palu (THE critical hub — all 4 peninsulas meet here)
            { 75, 158 },
            // 51 Mamuju (western peninsula coast)
            { 36, 24 },
            // 52 Gorontalo (northern peninsula)
            { 202, 266 },
            // 53 Manado (northern tip hub)
            { 274, 338 },
            // 54 Bitung (northeastern port — Maluku gateway)
            { 288, 333 },
            // --- Maluku / Spice Islands (scattered) ---
            // 55 Ternate (northern Maluku hub)
            { 375, 284 },
            // 56 Tidore
            { 378, 277 },
            // 57 Halmahera (large island)
            { 407, 270 },
            // 58 Morotai (northern edge)
            { 413, 399 },
            // 59 Ambon (central Maluku hub)
            { 407, -53 },
            // 60 Seram
            { 459, -15 },
            // 61 Banda Islands (isolated)
            { 476, -114 },
            // 62 Tual / Kei Islands (eastern edge)
            { 589, -197 },
            // --- Papua / West Papua (vast, sparse) ---
            // 63 Sorong (western gateway)
            { 530, 159 },
            // 64 Manokwari
            { 643, 160 },
            // 65 Fakfak (isolated coast)
            { 572, 6 },
            // 66 Biak Island (air hub)
            { 724, 135 },
            // 67 Nabire
            { 700, -27 },
            // 68 Jayapura (eastern hub — PNG border)
            { 909, 35 },
            // 69 Wamena (highland — extremely isolated)
            { 838, -82 },
        };

        // Edges: { cityA, cityB }
        private static readonly int[,] CityEdges = new int[,]
        {
            // Java internal
            { 0, 1 }, { 0, 2 }, { 2, 3 }, { 3, 4 }, { 3, 7 }, { 4, 5 }, { 5, 6 },
            { 6, 7 }, { 6, 8 }, { 6, 9 }, { 7, 8 }, { 7, 9 }, { 8, 9 }, { 8, 10 }, { 9, 10 },
            { 10, 11 }, { 10, 13 }, { 10, 15 }, { 11, 12 }, { 11, 13 }, { 12, 14 }, { 13, 14 },
            // Sunda crossing (Java to Sumatra — the key western sea link)
            { 1, 36 },
            // Java east tip to Bali (Bali Strait)
            { 14, 16 },
            // Bali and eastern island chain
            { 16, 17 }, { 17, 18 }, { 18, 19 }, { 19, 20 }, { 20, 21 }, { 21, 22 }, { 21, 24 },
            { 22, 23 },
            // Timor to Banda Sea (south Maluku approach)
            { 22, 61 },
            // Sumba isolated link
            { 24, 62 },
            // Sumatra north-south spine
            { 25, 26 }, { 26, 27 }, { 27, 28 }, { 28, 29 }, { 29, 30 }, { 30, 31 }, { 31, 32 },
            { 30, 33 }, { 33, 34 }, { 34, 35 }, { 34, 36 },
            // Sumatra cross-links
            { 28, 35 }, { 26, 30 },
            // Sumatra to Kalimantan (Karimata Strait)
            { 34, 37 }, { 31, 37 },
            // Kalimantan internal
            { 37, 38 }, { 38, 39 }, { 39, 40 }, { 40, 41 }, { 40, 42 }, { 41, 42 }, { 43, 44 },
            // Kalimantan north to south
            { 41, 43 }, { 42, 40 },
            // Kalimantan to Sulawesi (Makassar Strait crossing)
            { 41, 45 }, { 42, 46 },
            // Sulawesi — all peninsulas converge on Palu (50)
            { 45, 46 }, { 46, 47 }, { 47, 50 },
            { 48, 49 }, { 49, 47 }, { 48, 50 },
            { 50, 51 }, { 51, 46 },
            { 50, 52 }, { 52, 53 }, { 53, 54 },
            // Sulawesi to Maluku
            { 54, 55 },
            // Maluku northern cluster
            { 55, 56 }, { 55, 57 }, { 56, 57 }, { 57, 58 },
            // Maluku central / southern
            { 55, 59 }, { 59, 60 }, { 60, 61 }, { 61, 62 }, { 60, 62 },
            // Maluku to Papua
            { 57, 63 }, { 62, 63 },
            // Papua chain
            { 63, 64 }, { 63, 65 }, { 64, 66 }, { 64, 67 }, { 65, 67 },
            { 66, 68 }, { 67, 68 }, { 68, 69 },
        };

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            ArcenPoint center = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;
            int cityCount = CityPositions.GetLength( 0 );
            int edgeCount = CityEdges.GetLength( 0 );
            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( cityCount );
            for ( int i = 0; i < cityCount; i++ )
            {
                int x = center.X + CityPositions[i, 0];
                int y = center.Y + CityPositions[i, 1];
                Planet p = galaxy.AddPlanet( PlanetType.Normal, ArcenPoint.Create( x, y ),
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                allPlanets.Add( p );
            }
            BadgerUtilityMethods.EnforcePlanetMinimumSpacing( allPlanets, 50 );
            for ( int i = 0; i < edgeCount; i++ )
            {
                int a = CityEdges[i, 0];
                int b = CityEdges[i, 1];
                allPlanets[a].AddLinkTo( allPlanets[b] );
            }
            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }

    public class Mapgen_ScandinaviaMap : IMapGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }

        // xOffset = (lon - 15) * 75, yOffset = (lat - 62) * 80
        // Norway is a linear spine (fjords = defense). Øresund (48/34) is the main N-S chokepoint.
        // Turku (61) to Stockholm (22) is the critical Finland-Sweden sea crossing.
        private static readonly int[,] CityPositions = new int[,]
        {
            // --- Norway (mountainous spine — mostly linear) ---
            // 0  Oslo (hub)
            { -319, -167 },
            // 1  Drammen
            { -360, -181 },
            // 2  Fredrikstad (Swedish border)
            { -304, -223 },
            // 3  Kongsberg
            { -401, -186 },
            // 4  Skien / Porsgrunn
            { -405, -224 },
            // 5  Kristiansand (southern coast)
            { -525, -308 },
            // 6  Stavanger (western hub)
            { -695, -242 },
            // 7  Bergen (fjord gateway)
            { -725, -129 },
            // 8  Alesund
            { -663, 38 },
            // 9  Molde
            { -588, 59 },
            // 10 Kristiansund
            { -545, 89 },
            // 11 Trondheim (central hub)
            { -345, 114 },
            // 12 Roros (mountain pass)
            { -270, 46 },
            // 13 Lillehammer
            { -340, -70 },
            // 14 Hamar
            { -295, -97 },
            // 15 Gjovik
            { -323, -96 },
            // 16 Bodo (Arctic Circle gateway)
            { -44, 422 },
            // 17 Narvik (iron ore port — rail link to Kiruna)
            { 182, 515 },
            // 18 Tromso (northern hub)
            { 296, 612 },
            // 19 Alta
            { 620, 638 },
            // 20 Hammerfest (edge)
            { 651, 693 },
            // 21 Kirkenes (Norwegian-Russian border edge)
            { 1129, 618 },
            // --- Sweden (grid-like interior — best connected) ---
            // 22 Stockholm (eastern hub)
            { 230, -214 },
            // 23 Uppsala
            { 197, -171 },
            // 24 Vasteras
            { 116, -191 },
            // 25 Orebro
            { 17, -218 },
            // 26 Eskilstuna
            { 113, -210 },
            // 27 Norrkoping
            { 89, -272 },
            // 28 Linkoping
            { 47, -287 },
            // 29 Jonkoping (hub linking north and south Sweden)
            { -63, -338 },
            // 30 Vaxjo
            { -14, -410 },
            // 31 Kalmar (Baltic coast)
            { 103, -427 },
            // 32 Karlskrona (naval base)
            { 44, -467 },
            // 33 Malmo (Oresund crossing — critical chokepoint)
            { -150, -511 },
            // 34 Helsingborg (ferry to Helsingør)
            { -173, -476 },
            // 35 Gothenburg / Goteborg (western hub)
            { -228, -343 },
            // 36 Boras
            { -156, -342 },
            // 37 Karlstad (Norway corridor)
            { -113, -210 },
            // 38 Falun
            { 47, -112 },
            // 39 Gavle (Gulf of Bothnia coast)
            { 161, -106 },
            // 40 Sundsvall
            { 173, 31 },
            // 41 Harnosand
            { 221, 51 },
            // 42 Ostersund (mountain hub — Norwegian border)
            { -27, 94 },
            // 43 Umea (northern hub)
            { 394, 146 },
            // 44 Lulea (Baltic port, northern)
            { 537, 286 },
            // 45 Kiruna (iron ore — far north)
            { 392, 469 },
            // 46 Haparanda (Finnish border crossing)
            { 684, 307 },
            // --- Denmark (compact, high connectivity) ---
            // 47 Copenhagen (hub — Oresund bridge to Malmo)
            { -182, -506 },
            // 48 Helsingor (ferry to Helsingborg — chokepoint)
            { -179, -477 },
            // 49 Roskilde
            { -219, -509 },
            // 50 Odense (Funen island hub)
            { -346, -528 },
            // 51 Esbjerg (North Sea port)
            { -491, -522 },
            // 52 Aarhus (Jutland hub)
            { -360, -467 },
            // 53 Aalborg (northern Jutland)
            { -381, -396 },
            // 54 Frederikshavn (ferry to Gothenburg and Oslo)
            { -336, -365 },
            // 55 Kolding
            { -413, -520 },
            // 56 Vejle
            { -410, -503 },
            // 57 Viborg
            { -420, -444 },
            // 58 Bornholm (Baltic island — isolated)
            { 0, -552 },
            // --- Finland (lake district creates internal barriers) ---
            // 59 Helsinki (hub)
            { 750, -146 },
            // 60 Espoo / Vantaa
            { 745, -138 },
            // 61 Turku (western hub — ferry to Stockholm)
            { 543, -124 },
            // 62 Tampere (inland hub)
            { 657, -40 },
            // 63 Lahti
            { 800, -82 },
            // 64 Jyvaskyla (lake district hub)
            { 806, 19 },
            // 65 Kuopio
            { 951, 72 },
            // 66 Joensuu (Russian border)
            { 1107, 48 },
            // 67 Lappeenranta (Russian border edge)
            { 989, -75 },
            // 68 Vaasa (western coast)
            { 496, 88 },
            // 69 Seinajoki
            { 587, 63 },
            // 70 Oulu (northern hub)
            { 785, 241 },
            // 71 Rovaniemi (Arctic Circle hub)
            { 803, 360 },
            // 72 Sodankyla
            { 869, 434 },
            // 73 Ivalo / Inari (far north edge)
            { 941, 533 },
            // --- Edge / Border nodes ---
            // 74 Flensburg / German border (entry from Central Europe)
            { -416, -577 },
            // 75 Murmansk region (Russian Arctic — edge from Kirkenes)
            { 1354, 558 },
        };

        // Edges: { cityA, cityB }
        private static readonly int[,] CityEdges = new int[,]
        {
            // Oslo hub
            { 0, 1 }, { 0, 2 }, { 0, 13 }, { 0, 14 }, { 0, 15 },
            // Norway south coast
            { 1, 3 }, { 1, 5 }, { 3, 4 }, { 4, 5 }, { 5, 6 }, { 6, 7 },
            // Norway west coast (fjords northward)
            { 7, 8 }, { 7, 10 }, { 8, 9 }, { 9, 10 }, { 10, 11 },
            // Trondheim area
            { 11, 12 }, { 11, 42 }, { 11, 16 },
            { 12, 14 }, { 13, 14 }, { 13, 15 },
            // Norwegian arctic spine (linear)
            { 16, 17 }, { 17, 18 }, { 18, 19 }, { 19, 20 }, { 19, 21 },
            // Kirkenes to Murmansk (external)
            { 21, 75 },
            // Norway to Sweden crossings
            { 0, 37 },   // Oslo to Karlstad (E18 corridor)
            { 2, 35 },   // Fredrikstad to Gothenburg (coast road)
            { 11, 42 },  // Trondheim to Ostersund (already added)
            { 17, 45 },  // Narvik to Kiruna (iron ore rail)
            // Stockholm hub
            { 22, 23 }, { 22, 24 }, { 22, 26 }, { 22, 39 },
            { 23, 24 }, { 23, 38 }, { 23, 39 },
            // Sweden central grid
            { 24, 25 }, { 24, 26 }, { 25, 28 }, { 25, 37 },
            { 26, 27 }, { 27, 28 }, { 27, 31 },
            { 28, 29 }, { 29, 30 }, { 29, 35 }, { 29, 36 }, { 29, 37 },
            { 30, 31 }, { 30, 36 }, { 31, 32 },
            { 32, 33 }, { 33, 34 }, { 33, 47 }, // Malmo to Copenhagen (Oresund Bridge!)
            { 34, 48 },                           // Helsingborg to Helsingor (ferry!)
            { 35, 36 }, { 35, 33 },
            { 36, 29 }, // Boras to Jonkoping
            { 37, 38 }, { 38, 24 }, { 38, 39 },
            { 39, 40 }, { 39, 23 },
            { 40, 41 }, { 40, 42 }, { 40, 43 },
            { 41, 43 }, { 42, 11 }, // Ostersund back to Trondheim
            { 43, 44 }, { 43, 45 },
            { 44, 46 }, // Lulea to Haparanda (Finnish border)
            // Denmark internal
            { 47, 48 }, { 47, 49 }, { 47, 50 },
            { 48, 49 }, { 49, 50 }, { 50, 51 }, { 50, 52 }, { 50, 55 },
            { 51, 55 }, { 52, 53 }, { 52, 56 }, { 52, 57 },
            { 53, 54 }, { 53, 57 }, { 54, 35 }, // Frederikshavn to Gothenburg (ferry)
            { 55, 56 }, { 56, 57 },
            // German entry
            { 74, 55 }, { 74, 51 },
            // Bornholm (isolated Baltic island)
            { 58, 47 },
            // Finland hub
            { 59, 60 }, { 59, 63 }, { 59, 67 },
            { 60, 61 }, { 60, 62 },
            { 61, 62 }, { 61, 68 }, { 61, 22 }, // Turku to Stockholm (ferry!)
            { 62, 63 }, { 62, 64 }, { 62, 69 },
            { 63, 64 }, { 64, 65 }, { 64, 70 },
            { 65, 66 }, { 65, 70 },
            { 66, 67 },
            { 68, 69 }, { 69, 62 },
            { 70, 71 }, { 71, 72 }, { 71, 73 },
            { 72, 73 }, { 73, 75 }, // Inari to Murmansk
            // Sweden-Finland north crossing
            { 46, 70 }, // Haparanda to Oulu (via Tornio land border)
            { 44, 68 }, // Lulea to Vaasa (Kvarken ferry)
        };

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            ArcenPoint center = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;
            int cityCount = CityPositions.GetLength( 0 );
            int edgeCount = CityEdges.GetLength( 0 );
            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( cityCount );
            for ( int i = 0; i < cityCount; i++ )
            {
                int x = center.X + CityPositions[i, 0];
                int y = center.Y + CityPositions[i, 1];
                Planet p = galaxy.AddPlanet( PlanetType.Normal, ArcenPoint.Create( x, y ),
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                allPlanets.Add( p );
            }
            BadgerUtilityMethods.EnforcePlanetMinimumSpacing( allPlanets, 50 );
            for ( int i = 0; i < edgeCount; i++ )
            {
                int a = CityEdges[i, 0];
                int b = CityEdges[i, 1];
                allPlanets[a].AddLinkTo( allPlanets[b] );
            }
            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }

    public class Mapgen_LevantMap : IMapGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }

        // xOffset = (lon - 42) * 30, yOffset = (lat - 25) * 40
        // Center: ~42°E / ~25°N. Map spans Antioch south to Aden, coast to Muscat.
        // Dead Sea Rift is a hard N-S divide (Jordan Valley nodes = chokepoint chain).
        // Damascus is THE interior hub; Jerusalem controls the coast-to-Transjordan crossing.
        private static readonly int[,] CityPositions = new int[,]
        {
            // --- Northern Levant ---
            // 0  Antioch / Antakya (northern entry — gateway from Anatolia)
            { -168, 503 },
            // 1  Aleppo (northern interior hub)
            { -126, 503 },
            // 2  Latakia (northern coastal port)
            { -180, 464 },
            // 3  Hama
            { -147, 442 },
            // 4  Homs / Emesa (interior junction)
            { -147, 419 },
            // 5  Palmyra (desert shortcut — 2 connections)
            { -79, 414 },
            // --- Levant Coast ---
            // 6  Tripoli
            { -184, 402 },
            // 7  Byblos
            { -193, 386 },
            // 8  Beirut (coastal hub)
            { -197, 374 },
            // 9  Sidon
            { -201, 358 },
            // 10 Tyre
            { -210, 341 },
            // 11 Acre (northern Israel port)
            { -214, 318 },
            // 12 Haifa
            { -218, 313 },
            // 13 Caesarea
            { -222, 296 },
            // 14 Jaffa / Tel Aviv (central coast hub)
            { -226, 274 },
            // 15 Ashkelon
            { -235, 251 },
            // 16 Gaza (land bridge to Egypt — 3 connections)
            { -239, 240 },
            // --- Jezreel Valley / Galilee ---
            // 17 Nazareth
            { -205, 307 },
            // 18 Tiberias / Sea of Galilee
            { -197, 313 },
            // 19 Megiddo (valley chokepoint)
            { -210, 302 },
            // 20 Beth-Shean (Galilee eastern spur)
            { -197, 296 },
            // --- Jordan Valley / Dead Sea Rift ---
            // 21 Dan / Caesarea Philippi (Damascus northern spur)
            { -189, 341 },
            // 22 Jericho (Jordan Valley hub — lowest city on earth)
            { -197, 262 },
            // 23 Masada / Dead Sea south
            { -205, 223 },
            // 24 Zoar / Safi (Dead Sea southern tip)
            { -197, 212 },
            // --- Jerusalem / Judean Hills ---
            // 25 Jerusalem (major hub — 3 connections; controls coast-to-Jordan crossing)
            { -210, 257 },
            // 26 Bethlehem
            { -210, 251 },
            // 27 Hebron
            { -214, 240 },
            // --- Transjordan ---
            // 28 Amman / Philadelphia (Transjordanian hub)
            { -180, 262 },
            // 29 Jerash / Gerasa
            { -180, 285 },
            // 30 Madaba
            { -184, 251 },
            // 31 Kerak / Kir Moab
            { -189, 223 },
            // 32 Petra (Nabataean hub — controls desert trade)
            { -197, 173 },
            // 33 Aqaba / Aila (Red Sea terminus)
            { -210, 128 },
            // --- Negev / Sinai ---
            // 34 Beersheba (Negev gateway)
            { -226, 229 },
            // 35 Eilat (Red Sea — twin to Aqaba)
            { -222, 134 },
            // 36 El-Arish (Sinai coast — Egypt border)
            { -268, 218 },
            // 37 Sinai crossing (extreme chokepoint — 2 connections; lone land bridge)
            { -306, 128 },
            // --- Damascus Basin ---
            // 38 Damascus (THE map hub — 4 connections)
            { -163, 352 },
            // 39 Baalbek / Heliopolis (Bekaa Valley — Damascus spur)
            { -168, 380 },
            // 40 Bosra / Busra (Hauran hub — southern Syria gateway)
            { -155, 296 },
            // 41 Deraa / Adhriat
            { -172, 302 },
            // 42 Dumayr (eastern desert approach to Damascus)
            { -151, 363 },
            // 43 Suweida / Jabal al-Arab (Druze highlands)
            { -151, 307 },
            // --- Eastern Desert / Arabia approach ---
            // 44 Wadi Sirhan (caravan route — junction between Syria and Arabia)
            { -135, 240 },
            // 45 Tayma (Arabian oasis — caravan hub)
            { -105, 104 },
            // 46 Tabuk (northwestern Arabia)
            { -162, 140 },
            // --- Hejaz ---
            // 47 Yanbu (Red Sea port — northern Hejaz)
            { -117, -36 },
            // 48 Medina / Yathrib
            { -72, -20 },
            // 49 Jeddah (port and gateway to Mecca)
            { -84, -140 },
            // 50 Mecca (Hejaz hub — 3 connections)
            { -66, -144 },
            // 51 Taif (highland — southern Hejaz)
            { -48, -144 },
            // --- Yemen ---
            // 52 Najran (Hejaz–Yemen junction)
            { 63, -300 },
            // 53 Sana'a
            { 66, -384 },
            // 54 Taiz
            { 60, -456 },
            // 55 Aden (southern terminus — edge)
            { 90, -488 },
            // 56 Mukalla (eastern Yemen coast — edge)
            { 213, -420 },
            // --- Arabian Interior ---
            // 57 Riyadh (central Arabian hub)
            { 141, -12 },
            // 58 Hofuf / Al-Hasa (eastern Arabian oasis)
            { 228, 16 },
            // --- Gulf Coast ---
            // 59 Basra (Gulf hub)
            { 174, 220 },
            // 60 Kuwait
            { 177, 172 },
            // 61 Bahrain (island — sea crossroads)
            { 258, 48 },
            // 62 Muscat / Oman (southeastern edge)
            { 498, -56 },
        };

        private static readonly int[,] CityEdges = new int[,]
        {
            // Northern Levant
            { 0, 1 }, { 0, 2 }, { 0, 3 },
            { 1, 5 },
            { 2, 6 },
            { 3, 4 },
            { 4, 6 },
            // Levant Coast (linear chain, north to south)
            { 6, 7 }, { 7, 8 }, { 8, 9 }, { 9, 10 }, { 10, 11 },
            { 11, 12 }, { 12, 13 }, { 13, 14 }, { 14, 15 }, { 15, 16 },
            // Coast-to-interior links (Acre is the sole coast–Galilee gateway)
            { 11, 17 },
            // Galilee / Jezreel Valley
            { 17, 18 }, { 17, 19 },
            { 18, 20 },
            // Jordan Valley chain
            { 21, 38 },
            { 22, 23 }, { 23, 24 },
            // Jerusalem
            { 25, 14 }, { 25, 22 },
            { 25, 26 }, { 26, 27 },
            { 27, 34 },
            // Transjordan — King's Highway
            { 28, 22 }, { 28, 29 }, { 28, 30 },
            { 30, 31 }, { 31, 32 }, { 32, 33 }, { 32, 44 },
            // Negev / Sinai — Beersheba and Eilat are separate; no Negev shortcut
            { 16, 34 }, { 16, 36 },
            { 33, 35 },
            { 36, 37 }, { 35, 37 },
            // Damascus Basin — Palmyra is Aleppo↔Damascus bridge; Baalbek is a Damascus spur
            { 5, 38 },
            { 39, 38 },
            { 38, 40 }, { 40, 41 }, { 41, 28 },
            { 43, 40 },
            // Eastern Desert
            { 42, 44 },
            { 44, 45 }, { 45, 46 },
            // Hejaz
            { 46, 47 },
            { 47, 48 },
            { 48, 50 },
            { 50, 49 }, { 50, 51 },
            { 51, 52 },
            // Yemen
            { 52, 53 }, { 53, 54 }, { 54, 55 },
            { 55, 56 },
            // Arabia
            { 52, 57 },
            { 57, 58 }, { 57, 59 },
            // Gulf Coast
            { 59, 60 }, { 60, 61 }, { 61, 58 }, { 61, 62 },
        };

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            ArcenPoint center = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;
            int cityCount = CityPositions.GetLength( 0 );
            int edgeCount = CityEdges.GetLength( 0 );
            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( cityCount );
            for ( int i = 0; i < cityCount; i++ )
            {
                int x = center.X + CityPositions[i, 0];
                int y = center.Y + CityPositions[i, 1];
                Planet p = galaxy.AddPlanet( PlanetType.Normal, ArcenPoint.Create( x, y ),
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                allPlanets.Add( p );
            }
            BadgerUtilityMethods.EnforcePlanetMinimumSpacing( allPlanets, 50 );
            for ( int i = 0; i < edgeCount; i++ )
            {
                int a = CityEdges[i, 0];
                int b = CityEdges[i, 1];
                allPlanets[a].AddLinkTo( allPlanets[b] );
            }
            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }

    public class Mapgen_SilkRoadMap : IMapGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }

        // xOffset = (lon - 69) * 18, yOffset = (lat - 38) * 30
        // Center: ~69°E / ~38°N (Samarkand / Tashkent). Map spans Xi'an to Constantinople.
        // Two routes cross the Tarim Basin (north/south), reuniting at Kashgar.
        // Samarkand and Damascus are the key interior hubs; Derbent and Jade Gate are extreme chokepoints.
        private static readonly int[,] CityPositions = new int[,]
        {
            // --- Hexi Corridor ---
            // 0  Chang'an / Xi'an (eastern terminus)
            { 720, -111 },
            // 1  Lanzhou
            { 625, -57 },
            // 2  Zhangye
            { 565, 27 },
            // 3  Wuwei
            { 605, -3 },
            // 4  Dunhuang (fork node: northern and southern routes split here)
            { 463, 63 },
            // --- Tarim Basin — Northern Route ---
            // 5  Hami / Kumul
            { 441, 144 },
            // 6  Turfan
            { 364, 150 },
            // 7  Kucha
            { 250, 111 },
            // 8  Aksu
            { 203, 93 },
            // --- Tarim Basin — Southern Route ---
            // 9  Yumenguan / Jade Gate (extreme chokepoint — 2 connections)
            { 432, 72 },
            // 10 Khotan
            { 196, -27 },
            // 11 Yarkand
            { 150, 12 },
            // --- Routes converge ---
            // 12 Kashgar (all Tarim routes converge — 4 connections)
            { 124, 45 },
            // --- Ferghana Valley ---
            // 13 Osh (mountain pass gateway from Kashgar)
            { 68, 75 },
            // 14 Ferghana / Kokand
            { 34, 75 },
            // 15 Tashkent (northern hub)
            { 4, 99 },
            // 16 Jaxartes / Syr Darya steppe (edge)
            { -72, 150 },
            // --- Transoxiana ---
            // 17 Samarkand (major hub — 5+ connections; controls the central spine)
            { -38, 51 },
            // 18 Bukhara
            { -83, 54 },
            // 19 Merv (southern hub — gateway to Khorasan and Persia)
            { -130, -12 },
            // 20 Khiva / Urgench (northern steppe bypass node)
            { -155, 102 },
            // 21 Termez (Oxus river crossing — chokepoint)
            { -32, -24 },
            // 22 Amul / Charjew (Oxus crossing, Bukhara side)
            { -97, 33 },
            // --- Bactria / Afghanistan ---
            // 23 Balkh (ancient hub — Silk Road to India branch)
            { -38, -36 },
            // 24 Herat (gateway west)
            { -122, -111 },
            // 25 Kabul (India route edge)
            { 4, -105 },
            // 26 Bamyan / Hindu Kush pass (mountain crossing between Balkh and Kabul)
            { -22, -96 },
            // --- Khorasan ---
            // 27 Nishapur
            { -184, -54 },
            // 28 Mashhad / Tus
            { -169, -51 },
            // 29 Ray / Tehran
            { -317, -69 },
            // 30 Damghan
            { -265, -54 },
            // 31 Semnan
            { -281, -72 },
            // 32 Qazvin
            { -342, -51 },
            // --- Persia ---
            // 33 Hamadan / Ecbatana
            { -369, -96 },
            // 34 Isfahan (rich interior hub)
            { -312, -159 },
            // 35 Shiraz (southern edge)
            { -297, -252 },
            // 36 Siraf / Hormuz (Persian Gulf trade port — edge)
            { -225, -315 },
            // 37 Baghdad / Ctesiphon (Mesopotamian hub)
            { -443, -141 },
            // 38 Tabriz (northwestern junction)
            { -408, 3 },
            // --- Caucasus Branch ---
            // 39 Tbilisi (Caucasus hub)
            { -435, 111 },
            // 40 Derbent (Caspian coastal pass — extreme chokepoint, 2 connections)
            { -372, 123 },
            // 41 Trebizond (Black Sea port)
            { -527, 90 },
            // --- Anatolia ---
            // 42 Erzurum
            { -498, 57 },
            // 43 Diyarbakir (upper Euphrates junction)
            { -518, -3 },
            // 44 Sivas
            { -576, 51 },
            // 45 Ankara / Ancyra (western hub)
            { -649, 57 },
            // --- Mesopotamia / Syria ---
            // 46 Mosul
            { -467, -48 },
            // 47 Aleppo (Syrian crossroads — 4 connections)
            { -572, -54 },
            // 48 Palmyra (desert shortcut — 3 connections)
            { -553, -102 },
            // 49 Damascus (THE map hub — 5+ connections)
            { -588, -135 },
            // 50 Antioch / Antakya
            { -590, -54 },
            // --- Mediterranean ---
            // 51 Beirut
            { -603, -123 },
            // 52 Gaza (land bridge to Egypt — 3 connections)
            { -621, -195 },
            // 53 Aqaba / Red Sea terminus (edge)
            { -612, -255 },
            // 54 Alexandria (Egyptian terminus)
            { -704, -204 },
            // 55 Constantinople (THE western prize)
            { -720, 90 },
            // 56 Sinope (Black Sea port)
            { -608, 120 },
            // --- Northern Steppe Bypass ---
            // 57 Itil / Astrakhan (Caspian steppe hub)
            { -378, 249 },
            // 58 Sarai / Lower Volga (steppe edge)
            { -432, 315 },
            // 59 Crimea / Caffa (steppe-to-Constantinople link)
            { -629, 207 },
        };

        private static readonly int[,] CityEdges = new int[,]
        {
            // Hexi Corridor (linear funnel, east to west)
            { 0, 1 }, { 1, 3 }, { 3, 2 }, { 2, 4 },
            // Northern Tarim route: Dunhuang → Hami → Turfan → Kucha → Aksu → Kashgar
            { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 8 }, { 8, 12 },
            // Southern Tarim route: Dunhuang → Jade Gate → Khotan → Yarkand → Kashgar
            { 4, 9 }, { 9, 10 }, { 10, 11 }, { 11, 12 },
            // Kashgar exits: Ferghana Valley and Bactria via mountain pass
            { 12, 13 }, { 12, 23 },
            // Ferghana Valley
            { 13, 14 }, { 14, 15 }, { 14, 17 }, { 15, 16 }, { 15, 17 },
            // Transoxiana (Samarkand as hub)
            { 17, 18 }, { 17, 21 }, { 17, 22 },
            { 18, 19 }, { 18, 20 }, { 18, 22 },
            { 21, 22 }, { 21, 23 },
            // Bactria / Afghanistan
            { 23, 24 }, { 23, 25 }, { 23, 26 },
            { 24, 19 }, { 24, 26 }, { 24, 28 },
            { 25, 26 },
            // Khorasan road (Merv → Mashhad → Nishapur → Damghan → Semnan → Ray)
            { 19, 28 }, { 28, 27 }, { 27, 30 }, { 30, 31 }, { 31, 29 },
            // Western Persia
            { 29, 32 }, { 29, 33 }, { 29, 34 },
            { 32, 38 }, { 32, 33 },
            { 33, 34 }, { 33, 37 },
            { 34, 35 }, { 34, 37 },
            { 35, 36 },
            { 38, 39 }, { 38, 37 }, { 38, 33 },
            // Caucasus branch
            { 39, 40 }, { 39, 41 },
            { 40, 57 },
            { 41, 42 }, { 41, 56 },
            // Anatolia
            { 42, 43 }, { 42, 44 },
            { 43, 46 },
            { 44, 45 }, { 44, 50 },
            { 45, 55 },
            // Mesopotamia / Syria
            { 37, 46 }, { 37, 48 },
            { 46, 47 },
            { 47, 48 }, { 47, 49 }, { 47, 50 },
            { 48, 49 },
            { 49, 50 }, { 49, 51 }, { 49, 52 },
            { 50, 51 },
            // Mediterranean terminals
            { 51, 52 }, { 52, 53 }, { 52, 54 }, { 54, 55 },
            // Black Sea coast and steppe
            { 56, 55 }, { 56, 59 },
            { 57, 58 }, { 57, 59 },
            { 58, 59 },
            { 59, 55 },
            // Northern steppe connections (Khiva and Jaxartes → Itil bypass)
            { 16, 57 }, { 20, 57 },
        };

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            ArcenPoint center = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;

            int cityCount = CityPositions.GetLength( 0 );
            int edgeCount = CityEdges.GetLength( 0 );

            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( cityCount );

            for ( int i = 0; i < cityCount; i++ )
            {
                int x = center.X + CityPositions[i, 0];
                int y = center.Y + CityPositions[i, 1];
                Planet p = galaxy.AddPlanet( PlanetType.Normal, ArcenPoint.Create( x, y ),
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                allPlanets.Add( p );
            }

            BadgerUtilityMethods.EnforcePlanetMinimumSpacing( allPlanets, 50 );

            for ( int i = 0; i < edgeCount; i++ )
            {
                int a = CityEdges[i, 0];
                int b = CityEdges[i, 1];
                allPlanets[a].AddLinkTo( allPlanets[b] );
            }

            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }

    public class Mapgen_MekongMap : IMapGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }

        // xOffset = (lon - 100) * 72, yOffset = (lat - 13) * 40
        private static readonly int[,] CityPositions = new int[,]
        {
            // --- Myanmar — Irrawaddy Corridor ---
            // 0  Mandalay
            { -281, 360 },
            // 1  Bagan
            { -367, 328 },
            // 2  Magway
            { -367, 288 },
            // 3  Naypyidaw
            { -281, 268 },
            // 4  Pyinmana
            { -274, 260 },
            // 5  Toungoo
            { -259, 236 },
            // 6  Pegu / Bago
            { -252, 172 },
            // 7  Yangon / Rangoon
            { -274, 152 },
            // 8  Mawlamyine / Moulmein
            { -173, 140 },
            // 9  Pathein / Bassein
            { -382, 152 },
            // --- Myanmar — Shan State ---
            // 10 Taunggyi
            { -216, 312 },
            // 11 Kengtung (Golden Triangle)
            { -29, 332 },
            // 12 Lashio (Burma Road)
            { -166, 396 },
            // 13 Hsipaw
            { -151, 384 },
            // --- Myanmar — Chin Hills ---
            // 14 Monywa
            { -353, 364 },
            // 15 Kalewa (India border — edge node)
            { -410, 408 },
            // --- Northern Thailand ---
            // 16 Chiang Rai
            { -14, 276 },
            // 17 Chiang Mai
            { -79, 232 },
            // 18 Lampang
            { -36, 212 },
            // 19 Phrae
            { 14, 204 },
            // 20 Nan
            { 58, 232 },
            // 21 Mae Sot (Moei River border — Myanmar crossing)
            { -101, 148 },
            // --- Central Thailand (Chao Phraya Valley) ---
            // 22 Phitsanulok
            { 22, 152 },
            // 23 Nakhon Sawan
            { 14, 108 },
            // 24 Ayutthaya
            { 43, 56 },
            // 25 Bangkok
            { 36, 32 },
            // 26 Nonthaburi / Greater Bangkok
            { 29, 40 },
            // 27 Samut Prakan
            { 43, 24 },
            // 28 Ratchaburi
            { -14, 20 },
            // 29 Hua Hin (peninsula gateway)
            { -7, -16 },
            // --- Eastern Thailand / Isan ---
            // 30 Udon Thani
            { 202, 176 },
            // 31 Nong Khai (Mekong / Friendship Bridge crossing)
            { 194, 196 },
            // 32 Khon Kaen
            { 202, 136 },
            // 33 Ubon Ratchathani
            { 353, 88 },
            // 34 Nakhon Ratchasima / Korat (plateau chokepoint)
            { 151, 76 },
            // 35 Buriram
            { 223, 80 },
            // 36 Surin
            { 252, 76 },
            // 37 Si Saket
            { 310, 84 },
            // --- Laos ---
            // 38 Luang Prabang
            { 151, 276 },
            // 39 Xieng Khouang / Plain of Jars
            { 245, 252 },
            // 40 Vientiane
            { 187, 200 },
            // 41 Thakhek
            { 346, 176 },
            // 42 Savannakhet (Ho Chi Minh trail hub)
            { 346, 144 },
            // 43 Pakse / Champasak
            { 418, 84 },
            // 44 Si Phan Don / 4,000 Islands
            { 418, 48 },
            // --- Cambodia ---
            // 45 Siem Reap
            { 281, 16 },
            // 46 Battambang
            { 216, 0 },
            // 47 Phnom Penh
            { 353, -56 },
            // 48 Kampot / Kep
            { 302, -96 },
            // 49 Sihanoukville
            { 252, -96 },
            // 50 Kompong Cham
            { 396, -40 },
            // --- Vietnam — North ---
            // 51 Dien Bien Phu
            { 216, 336 },
            // 52 Hanoi
            { 418, 320 },
            // 53 Haiphong
            { 482, 312 },
            // 54 Nam Dinh
            { 446, 296 },
            // 55 Vinh
            { 410, 228 },
            // --- Vietnam — Central ---
            // 56 Hue
            { 547, 140 },
            // 57 Da Nang
            { 590, 124 },
            // 58 Hoi An
            { 597, 116 },
            // 59 Qui Nhon
            { 662, 32 },
            // 60 Nha Trang
            { 662, -32 },
            // --- Vietnam — Central Highlands ---
            // 61 Pleiku
            { 576, 40 },
            // 62 Buon Ma Thuot
            { 583, -12 },
            // 63 Da Lat
            { 605, -44 },
            // --- Vietnam — South ---
            // 64 Ho Chi Minh City / Saigon
            { 482, -88 },
            // 65 Can Tho
            { 418, -116 },
            // 66 Vung Tau
            { 511, -104 },
            // 67 Ca Mau (delta tip)
            { 374, -152 },
            // --- Peninsula — Southern Chain ---
            // 68 Hat Yai (peninsula chokepoint)
            { 36, -240 },
            // 69 Penang / Georgetown
            { 29, -304 },
            // 70 Kuala Lumpur
            { 122, -396 },
            // 71 Singapore (terminal)
            { 274, -464 },
        };

        private static readonly int[,] CityEdges = new int[,]
        {
            // Myanmar — Irrawaddy spine
            { 15, 14 }, { 14, 0 }, { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 4 }, { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 8 }, { 7, 9 },
            // Myanmar — Shan State
            { 0, 12 }, { 0, 13 }, { 12, 13 }, { 13, 10 }, { 10, 11 }, { 11, 16 }, { 5, 10 },
            // Myanmar — Mae Sot crossing (Myawaddy border)
            { 8, 21 },
            // Northern Thailand
            { 16, 17 }, { 17, 18 }, { 18, 16 }, { 18, 19 }, { 19, 20 }, { 19, 22 }, { 17, 21 },
            // Chiang Rai and Nan — northern Laos connections (Chiang Khong / Mekong)
            { 16, 38 }, { 20, 38 },
            // Central Thailand (Chao Phraya corridor)
            { 21, 22 }, { 22, 23 }, { 23, 24 }, { 24, 25 }, { 24, 26 }, { 25, 26 }, { 25, 27 }, { 25, 28 }, { 28, 29 },
            // Bangkok hub to Korat (northeast gateway)
            { 25, 34 },
            // Peninsula approach
            { 29, 68 },
            // Eastern Thailand / Isan
            { 34, 32 }, { 34, 35 }, { 30, 31 }, { 30, 32 }, { 31, 40 }, { 32, 35 }, { 35, 36 }, { 36, 37 }, { 37, 33 }, { 33, 43 },
            // Battambang — Thailand border crossing (Poipet / Aranyaprathet)
            { 46, 34 },
            // Laos — Mekong corridor
            { 38, 39 }, { 38, 40 }, { 38, 51 }, { 39, 40 }, { 40, 41 }, { 41, 42 }, { 42, 43 }, { 43, 44 },
            // Laos — Ho Chi Minh trail crossings into Vietnam
            { 39, 52 }, { 39, 55 }, { 41, 55 }, { 42, 57 },
            // Si Phan Don — Cambodia entry
            { 44, 45 },
            // Cambodia
            { 45, 46 }, { 45, 47 }, { 46, 47 }, { 47, 50 }, { 47, 48 }, { 48, 49 }, { 50, 64 }, { 47, 64 },
            // Vietnam — North
            { 51, 52 }, { 52, 53 }, { 52, 54 }, { 52, 55 }, { 53, 54 }, { 55, 56 },
            // Vietnam — Central coast
            { 56, 57 }, { 57, 58 }, { 58, 59 }, { 59, 60 },
            // Vietnam — Highlands (lateral bypasses)
            { 57, 61 }, { 61, 59 }, { 61, 62 }, { 62, 60 }, { 62, 63 }, { 63, 60 }, { 63, 64 },
            // Vietnam — South
            { 60, 64 }, { 64, 65 }, { 64, 66 }, { 65, 67 },
            // Peninsula — southern chain
            { 68, 69 }, { 69, 70 }, { 70, 71 },
        };

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            ArcenPoint center = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;
            int cityCount = CityPositions.GetLength( 0 );
            int edgeCount = CityEdges.GetLength( 0 );
            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( cityCount );
            for ( int i = 0; i < cityCount; i++ )
            {
                int x = center.X + CityPositions[i, 0];
                int y = center.Y + CityPositions[i, 1];
                Planet p = galaxy.AddPlanet( PlanetType.Normal, ArcenPoint.Create( x, y ),
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                allPlanets.Add( p );
            }
            BadgerUtilityMethods.EnforcePlanetMinimumSpacing( allPlanets, 50 );
            for ( int i = 0; i < edgeCount; i++ )
            {
                int a = CityEdges[i, 0];
                int b = CityEdges[i, 1];
                allPlanets[a].AddLinkTo( allPlanets[b] );
            }
            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }

    public class Mapgen_AndesMap : IMapGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }

        // xOffset = (lon + 69) * 25, yOffset = (lat + 22) * 20
        private static readonly int[,] CityPositions = new int[,]
        {
            // --- Colombia / Venezuela ---
            // 0  Bogotá
            { -128, 534 },
            // 1  Medellín
            { -165, 564 },
            // 2  Cali
            { -188, 508 },
            // 3  Bucaramanga
            { -103, 582 },
            // 4  Manizales
            { -163, 542 },
            // 5  Pasto (southern Colombia — Ecuador border)
            { -208, 464 },
            // 6  Cartagena (Caribbean port — northern entry)
            { -163, 648 },
            // 7  Barranquilla
            { -145, 660 },
            // 8  Caracas
            { 53, 650 },
            // 9  Maracaibo
            { -65, 654 },
            // 10 Cúcuta (Colombia–Venezuela border pass)
            { -88, 598 },
            // --- Ecuador ---
            // 11 Quito
            { -238, 436 },
            // 12 Guayaquil (Pacific port)
            { -273, 396 },
            // 13 Cuenca
            { -250, 382 },
            // 14 Ibarra (northern — Colombia border approach)
            { -228, 448 },
            // 15 Loja (southern — Peru border)
            { -255, 360 },
            // --- Peru — Coast ---
            // 16 Piura (northern coast — Ecuador crossing)
            { -290, 336 },
            // 17 Chiclayo
            { -270, 304 },
            // 18 Trujillo
            { -250, 278 },
            // 19 Lima (THE Peruvian hub)
            { -200, 200 },
            // 20 Callao (Lima's port — dead-end branch)
            { -250, 185 },
            // 21 Ica
            { -168, 158 },
            // 22 Arequipa (southern hub)
            { -63, 112 },
            // 23 Tacna (Chile border crossing)
            { -30, 80 },
            // --- Peru — Highlands ---
            // 24 Cusco (Andean highland hub — Inca crossroads)
            { -73, 170 },
            // 25 Puno (Lake Titicaca — Peru–Bolivia crossing)
            { -25, 124 },
            // 26 Huancayo (central highland)
            { -155, 198 },
            // 27 Ayacucho
            { -130, 176 },
            // 28 Huaraz (Cordillera Blanca)
            { -213, 250 },
            // 29 Iquitos (upper Amazon — remote, 2 connections)
            { -105, 366 },
            // 30 Puerto Maldonado (Amazon slope — Trans-Oceanic Highway)
            { -5, 188 },
            // --- Bolivia ---
            // 31 La Paz / El Alto (capital hub)
            { 20, 110 },
            // 32 Oruro (THE Altiplano crossroads)
            { 48, 82 },
            // 33 Potosí
            { 83, 48 },
            // 34 Sucre
            { 93, 60 },
            // 35 Cochabamba
            { 70, 92 },
            // 36 Santa Cruz (eastern lowlands — Bolivia exit east)
            { 145, 84 },
            // 37 Uyuni (salt flat — Antofagasta rail link)
            { 55, 30 },
            // --- Chile — North / Atacama ---
            // 38 Arica (Peru–Chile coastal crossing)
            { -33, 70 },
            // 39 Iquique
            { -28, 36 },
            // 40 Antofagasta (major mining hub)
            { -35, -34 },
            // 41 Calama / San Pedro de Atacama
            { 20, -18 },
            // --- Chile — Central ---
            // 42 Copiapó
            { -33, -108 },
            // 43 La Serena
            { -58, -158 },
            // 44 Valparaíso / Viña del Mar (main Pacific port)
            { -65, -220 },
            // 45 Santiago (THE Chile hub)
            { -43, -230 },
            // 46 Rancagua
            { -43, -244 },
            // 47 Talca
            { -68, -268 },
            // 48 Chillán
            { -78, -292 },
            // --- Chile — South ---
            // 49 Concepción
            { -103, -296 },
            // 50 Temuco
            { -90, -334 },
            // 51 Valdivia
            { -105, -356 },
            // 52 Osorno
            { -103, -372 },
            // 53 Puerto Montt (Patagonia gateway)
            { -98, -390 },
            // 54 Castro / Chiloé Island (maritime threshold)
            { -120, -410 },
            // --- Patagonia — Chile ---
            // 55 Coyhaique
            { -78, -472 },
            // 56 Punta Arenas (Magellan Strait hub)
            { -48, -624 },
            // 57 Tierra del Fuego / Ushuaia (terminal dead-end)
            { 18, -656 },
            // --- Argentina — Andean Foothills / Cuyo ---
            // 58 Mendoza (trans-Andean hub — Paso de los Libertadores)
            { 5, -218 },
            // 59 San Juan
            { 13, -190 },
            // 60 Salta
            { 90, -56 },
            // 61 Jujuy (far north — Bolivia/Chile tripoint)
            { 93, -44 },
            // 62 Tucumán
            { 95, -96 },
            // 63 San Luis (Pampas gateway)
            { 65, -226 },
            // --- Argentina — Pampas / Core ---
            // 64 Buenos Aires (THE southern hub)
            { 265, -252 },
            // 65 Rosario (Paraná river hub)
            { 208, -218 },
            // 66 Córdoba (central Argentine hub)
            { 120, -188 },
            // 67 Santa Fe
            { 208, -192 },
            // 68 Bahía Blanca (Atlantic coast — Patagonia gateway)
            { 168, -334 },
            // --- Argentina — Patagonia ---
            // 69 Neuquén (northern Patagonia hub)
            { 23, -338 },
            // 70 Bariloche (lake district — Chilean pass crossing)
            { -58, -382 },
            // 71 Comodoro Rivadavia (Atlantic coast)
            { 38, -478 },
            // 72 Río Gallegos (far south)
            { -5, -592 },
            // --- Uruguay / Paraguay ---
            // 73 Montevideo (Río de la Plata — Buenos Aires neighbor)
            { 320, -258 },
            // 74 Asunción (Paraguay — Paraná river hub)
            { 285, -66 },
            // --- Pass / Crossing Nodes ---
            // 75 Paso de los Libertadores (Santiago–Mendoza — THE primary crossing, 2 connections)
            { -25, -216 },
            // 76 Jama Pass (Calama–Salta — northern Andean crossing, 2 connections)
            { 48, -24 },
            // 77 Paso Cardenal Samoré (Puerto Montt–Bariloche — southern crossing, 2 connections)
            { -73, -374 },
        };

        private static readonly int[,] CityEdges = new int[,]
        {
            // Colombia — northern Caribbean coast
            { 6, 7 }, { 7, 1 }, { 6, 1 },
            // Colombia — Venezuela connections
            { 7, 9 }, { 9, 8 }, { 9, 10 }, { 10, 3 }, { 10, 8 },
            // Colombia — three-cordillera network
            { 3, 0 }, { 0, 1 }, { 0, 2 }, { 0, 4 }, { 1, 4 }, { 1, 2 }, { 4, 2 },
            // Colombia–Ecuador border (Cali → Pasto → Ibarra)
            { 2, 5 }, { 5, 14 },
            // Ecuador highlands and coast
            { 14, 11 }, { 11, 13 }, { 11, 12 }, { 13, 12 }, { 13, 15 }, { 12, 15 },
            // Ecuador–Peru crossings (highland and coastal Pan-American)
            { 15, 16 }, { 12, 16 },
            // Peru — Panamerican coastal highway
            { 16, 17 }, { 17, 18 }, { 18, 19 }, { 19, 20 }, { 19, 21 }, { 21, 22 }, { 22, 23 },
            // Lima — Central Highway to highlands
            { 19, 26 },
            // Ica — Ayacucho highland spur
            { 21, 27 },
            // Peru — northern and central sierra (Cordillera Blanca corridor)
            { 17, 28 }, { 18, 28 }, { 28, 26 }, { 26, 27 }, { 27, 24 },
            // Cusco — Puno — Arequipa highland triangle
            { 24, 25 }, { 24, 22 },
            // Iquitos — Amazon connections (Napo/Coca to Ecuador; road west to Chiclayo)
            { 29, 11 }, { 29, 17 },
            // Puerto Maldonado — Trans-Oceanic Highway
            { 30, 24 }, { 30, 31 },
            // Peru–Chile coastal crossing (Tacna–Arica)
            { 23, 38 },
            // Peru–Bolivia (Puno–La Paz Titicaca shore road)
            { 25, 31 },
            // Bolivia — Altiplano core (Oruro is the crossroads)
            { 31, 32 }, { 32, 35 }, { 32, 33 }, { 32, 37 }, { 33, 34 }, { 34, 35 }, { 35, 36 },
            // Bolivia–Chile (Uyuni–Antofagasta railway)
            { 37, 40 },
            // Bolivia–Argentina border (Jujuy/La Quiaca–Potosí/Villazón)
            { 61, 33 },
            // Bolivia–Paraguay (Santa Cruz–Asunción lowland route)
            { 36, 74 },
            // Chile — Atacama coastal strip
            { 38, 39 }, { 39, 40 }, { 40, 41 }, { 40, 42 },
            // Calama — Jama Pass (cross-Andean exit to Salta)
            { 41, 76 },
            // Chile — Central Valley (Panamerican spine)
            { 42, 43 }, { 43, 44 }, { 44, 45 }, { 45, 46 }, { 46, 47 }, { 47, 48 }, { 48, 49 },
            // Chile — South (Bío-Bío to Puerto Montt)
            { 49, 50 }, { 50, 51 }, { 51, 52 }, { 52, 53 }, { 53, 54 },
            // Patagonian Chilean chain
            { 54, 55 }, { 55, 56 }, { 56, 57 },
            // Punta Arenas–Río Gallegos cross-border (southern Patagonia)
            { 56, 72 },
            // Paso de los Libertadores (Santiago–Mendoza — THE primary trans-Andean crossing)
            { 75, 45 }, { 75, 58 },
            // Jama Pass (Calama–Salta)
            { 76, 60 },
            // Paso Cardenal Samoré (Puerto Montt–Bariloche)
            { 77, 53 }, { 77, 70 },
            // Argentina — Cuyo / Andean foothills
            { 58, 59 }, { 58, 63 }, { 59, 62 }, { 61, 60 }, { 60, 62 },
            // Argentina — Pampas highway network
            { 62, 66 }, { 63, 66 }, { 64, 65 }, { 64, 73 }, { 64, 68 }, { 65, 66 }, { 65, 67 }, { 66, 67 }, { 67, 74 },
            // Bahía Blanca — Patagonian gateway
            { 68, 69 },
            // Argentina — Patagonian chain (Atlantic side)
            { 69, 70 }, { 69, 71 }, { 71, 72 },
        };

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            ArcenPoint center = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;
            int cityCount = CityPositions.GetLength( 0 );
            int edgeCount = CityEdges.GetLength( 0 );
            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( cityCount );
            for ( int i = 0; i < cityCount; i++ )
            {
                int x = center.X + CityPositions[i, 0];
                int y = center.Y + CityPositions[i, 1];
                Planet p = galaxy.AddPlanet( PlanetType.Normal, ArcenPoint.Create( x, y ),
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                allPlanets.Add( p );
            }
            BadgerUtilityMethods.EnforcePlanetMinimumSpacing( allPlanets, 50 );
            for ( int i = 0; i < edgeCount; i++ )
            {
                int a = CityEdges[i, 0];
                int b = CityEdges[i, 1];
                allPlanets[a].AddLinkTo( allPlanets[b] );
            }
            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }

    public class Mapgen_CentralAmericaCaribbean : IMapGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        // Positions derived from lat/lon: xOffset = (lon + 80) * 30, yOffset = (lat - 15) * 60
        private static readonly int[,] CityPositions = new int[,]
        {
            // --- Southern Mexico ---
            // 0  Veracruz: lon=-96.13, lat=19.18
            { -484, 251 },
            // 1  Oaxaca: lon=-96.73, lat=17.07
            { -502, 124 },
            // 2  Villahermosa: lon=-92.92, lat=17.99
            { -388, 179 },
            // 3  Tuxtla Gutiérrez: lon=-93.10, lat=16.75
            { -393, 105 },
            // 4  San Cristóbal de las Casas: lon=-92.64, lat=16.74
            { -379, 104 },
            // 5  Tapachula: lon=-92.27, lat=14.90
            { -368, -6 },
            // 6  Campeche: lon=-90.53, lat=19.84
            { -316, 290 },
            // 7  Mérida: lon=-89.62, lat=20.97
            { -289, 358 },
            // 8  Chetumal: lon=-88.30, lat=18.50
            { -249, 210 },
            // 9  Cancún: lon=-86.85, lat=21.17
            { -206, 370 },
            // --- Guatemala ---
            // 10 Flores (Petén): lon=-89.89, lat=16.92
            { -297, 115 },
            // 11 Puerto Barrios: lon=-88.60, lat=15.71
            { -258, 43 },
            // 12 Guatemala City: lon=-90.52, lat=14.64
            { -316, -22 },
            // 13 Quetzaltenango: lon=-91.52, lat=14.84
            { -346, -10 },
            // --- Belize ---
            // 14 Belize City: lon=-88.20, lat=17.25
            { -246, 135 },
            // 15 Orange Walk: lon=-88.57, lat=18.10
            { -257, 186 },
            // --- Honduras ---
            // 16 San Pedro Sula: lon=-88.03, lat=15.47
            { -241, 28 },
            // 17 La Ceiba: lon=-86.87, lat=15.76
            { -206, 46 },
            // 18 Tegucigalpa: lon=-87.22, lat=14.09
            { -217, -55 },
            // 19 Choluteca: lon=-87.19, lat=13.30
            { -216, -102 },
            // --- El Salvador ---
            // 20 Santa Ana: lon=-89.56, lat=13.99
            { -287, -61 },
            // 21 San Salvador: lon=-89.20, lat=13.70
            { -276, -78 },
            // 22 San Miguel: lon=-88.18, lat=13.48
            { -245, -91 },
            // --- Nicaragua ---
            // 23 Matagalpa: lon=-85.92, lat=12.93
            { -178, -124 },
            // 24 Managua: lon=-86.29, lat=12.14
            { -189, -172 },
            // 25 León: lon=-86.88, lat=12.43
            { -206, -154 },
            // 26 Granada: lon=-85.96, lat=11.93
            { -179, -184 },
            // --- Costa Rica ---
            // 27 Liberia: lon=-85.44, lat=10.63
            { -163, -262 },
            // 28 San José: lon=-84.09, lat=9.93
            { -123, -304 },
            // 29 Puerto Limón: lon=-83.04, lat=10.00
            { -91, -300 },
            // --- Panama ---
            // 30 David: lon=-82.43, lat=8.43
            { -73, -394 },
            // 31 Santiago (Veraguas): lon=-80.99, lat=8.10
            { -30, -414 },
            // 32 Colón: lon=-79.90, lat=9.36
            { 3, -338 },
            // 33 Panama City: lon=-79.52, lat=8.99
            { 14, -361 },
            // --- Cuba ---
            // 34 Havana: lon=-82.38, lat=23.13
            { -71, 488 },
            // 35 Matanzas: lon=-81.58, lat=23.05
            { -47, 483 },
            // 36 Santa Clara: lon=-79.97, lat=22.40
            { 1, 444 },
            // 37 Camagüey: lon=-77.92, lat=21.38
            { 62, 383 },
            // 38 Holguín: lon=-76.26, lat=20.89
            { 112, 353 },
            // 39 Santiago de Cuba: lon=-75.83, lat=20.02
            { 125, 301 },
            // 40 Guantánamo: lon=-75.21, lat=20.14
            { 144, 308 },
            // --- Bahamas ---
            // 41 Nassau: lon=-77.35, lat=25.04
            { 80, 602 },
            // --- Jamaica ---
            // 42 Montego Bay: lon=-77.92, lat=18.47
            { 62, 208 },
            // 43 Kingston: lon=-76.79, lat=17.99
            { 96, 179 },
            // --- Haiti ---
            // 44 Cap-Haïtien: lon=-72.19, lat=19.76
            { 234, 286 },
            // 45 Port-au-Prince: lon=-72.34, lat=18.54
            { 230, 212 },
            // --- Dominican Republic ---
            // 46 Santiago de los Caballeros: lon=-70.70, lat=19.45
            { 279, 267 },
            // 47 Santo Domingo: lon=-69.90, lat=18.48
            { 303, 209 },
            // --- Puerto Rico ---
            // 48 San Juan: lon=-66.10, lat=18.47
            { 417, 208 },
            // 49 Ponce: lon=-66.61, lat=18.01
            { 402, 181 },
            // --- Lesser Antilles ---
            // 50 Basseterre (St. Kitts): lon=-62.72, lat=17.30
            { 518, 138 },
            // 51 Roseau (Dominica): lon=-61.39, lat=15.30
            { 558, 18 },
            // 52 Fort-de-France (Martinique): lon=-61.06, lat=14.61
            { 568, -23 },
            // 53 Castries (St. Lucia): lon=-61.00, lat=14.02
            { 570, -59 },
            // 54 Bridgetown (Barbados): lon=-59.62, lat=13.10
            { 611, -114 },
            // 55 Port of Spain (Trinidad): lon=-61.52, lat=10.65
            { 554, -261 },
            // --- ABC Islands ---
            // 56 Willemstad (Curaçao): lon=-68.93, lat=12.10
            { 332, -174 },
            // 57 Oranjestad (Aruba): lon=-70.03, lat=12.52
            { 299, -149 },
            // --- Cayman Islands ---
            // 58 George Town: lon=-81.38, lat=19.30
            { -41, 258 },
            // --- Guadeloupe ---
            // 59 Pointe-à-Pitre: lon=-61.54, lat=16.24
            { 554, 74 },
            // --- Mainland Mexico bridge ---
            // 60 Mexico City: lon=-99.13, lat=19.43
            { -574, 266 },
            // 61 Guadalajara: lon=-103.35, lat=20.66
            { -701, 340 },
            // 62 Mazatlán: lon=-106.41, lat=23.23
            { -792, 494 },
            // --- Baja California ---
            // 63 Tijuana: lon=-117.04, lat=32.53
            { -1111, 1052 },
            // 64 Ensenada: lon=-116.63, lat=31.87
            { -1099, 1012 },
            // 65 Guerrero Negro: lon=-113.99, lat=27.97
            { -1020, 778 },
            // 66 La Paz: lon=-110.31, lat=24.14
            { -909, 548 },
            // 67 Cabo San Lucas: lon=-109.92, lat=22.89
            { -898, 473 },
            // --- Central America additions ---
            // 68 Roatán (Honduras Bay Islands): lon=-86.56, lat=16.32
            { -197, 79 },
            // 69 Bluefields (Nicaragua): lon=-83.77, lat=12.00
            { -113, -180 },
            // 70 Bocas del Toro (Panama): lon=-82.24, lat=9.34
            { -67, -340 },
            // --- Northern Mexico mainland (Pacific corridor) ---
            // 71 Culiacán: lon=-107.39, lat=24.80
            { -822, 588 },
            // 72 Los Mochis: lon=-108.99, lat=25.79
            { -870, 647 },
            // 73 Guaymas: lon=-110.89, lat=27.92
            { -927, 775 },
            // 74 Hermosillo: lon=-110.96, lat=29.07
            { -929, 844 },
            // 75 Nogales: lon=-110.94, lat=31.32
            { -928, 979 },
            // 76 Mexicali: lon=-115.45, lat=32.66
            { -1064, 1060 },
        };

        // Edges: { cityA, cityB }
        private static readonly int[,] CityEdges = new int[,]
        {
            // Southern Mexico internal
            { 0, 2 }, { 0, 1 },
            { 1, 3 },
            { 2, 6 }, { 2, 3 },
            { 3, 4 }, { 3, 5 },
            { 4, 5 },
            { 6, 7 }, { 6, 8 },
            { 7, 8 }, { 7, 9 },
            { 8, 9 },
            // Cancún — Havana (Yucatán Channel)
            { 9, 34 },
            // Chetumal — Belize border
            { 8, 15 },
            // Guatemala
            { 5, 12 }, { 5, 13 },
            { 10, 15 }, { 10, 11 }, { 10, 12 },
            { 11, 14 }, { 11, 16 },
            { 12, 13 }, { 12, 21 },
            // Belize
            { 14, 15 },
            // Honduras
            { 16, 17 }, { 16, 18 },
            { 17, 18 },
            { 18, 19 }, { 18, 21 },
            // El Salvador
            { 20, 13 }, { 20, 21 },
            { 21, 22 },
            { 22, 19 },
            // Nicaragua
            { 19, 25 }, { 19, 24 },
            { 24, 25 }, { 24, 23 }, { 24, 26 },
            // Costa Rica
            { 25, 27 }, { 26, 27 },
            { 27, 28 },
            { 28, 29 }, { 28, 30 },
            { 29, 32 },
            // Panama
            { 30, 31 },
            { 31, 32 }, { 31, 33 },
            { 32, 33 },
            // Cuba spine
            { 34, 35 }, { 35, 36 }, { 36, 37 }, { 37, 38 }, { 38, 39 }, { 39, 40 },
            // Cuba — sea connections
            { 34, 41 }, { 34, 42 }, { 34, 58 },
            { 39, 43 },
            { 40, 44 },
            // Bahamas — eastern Cuba
            { 41, 38 },
            // Cayman Islands
            { 58, 42 },
            // Jamaica
            { 42, 43 }, { 43, 45 },
            // Hispaniola
            { 44, 45 }, { 44, 46 },
            { 45, 47 },
            { 46, 47 },
            // Dominican Republic — Puerto Rico (Mona Passage)
            { 47, 48 },
            // Puerto Rico
            { 48, 49 }, { 48, 50 }, { 49, 50 },
            // Lesser Antilles island chain
            { 50, 59 }, { 59, 51 }, { 51, 52 }, { 52, 53 }, { 53, 54 }, { 54, 55 },
            // Trinidad — ABC Islands
            { 55, 56 }, { 56, 57 },
            // Mainland Mexico bridge (Veracruz/Oaxaca → Mexico City → Guadalajara → Mazatlán)
            { 0, 60 }, { 1, 60 },
            { 60, 61 },
            { 61, 62 },
            // Mazatlán — La Paz ferry, then Transpeninsular Highway to Tijuana
            { 62, 66 },
            { 65, 66 }, { 64, 65 }, { 63, 64 },
            { 66, 67 },
            // Roatán — La Ceiba ferry
            { 17, 68 },
            // Bluefields — Nicaragua road links
            { 24, 69 }, { 26, 69 },
            // Bocas del Toro — western Panama and Caribbean coast
            { 30, 70 }, { 32, 70 },
            // --- Northern Mexico mainland corridor (Pacific coast highway) ---
            // Mazatlán to Culiacán (MEX-15)
            { 62, 71 },
            // Culiacán to Los Mochis
            { 71, 72 },
            // Los Mochis to Guaymas
            { 72, 73 },
            // Guaymas to Hermosillo
            { 73, 74 },
            // Hermosillo to Nogales
            { 74, 75 },
            // Nogales to Mexicali (MEX-2 border highway)
            { 75, 76 },
            // Mexicali to Tijuana
            { 76, 63 },
            // Los Mochis to La Paz (Topolobampo ferry — real ferry route)
            { 72, 66 },
            // Guaymas to Guerrero Negro (cross-gulf link)
            { 73, 65 },
        };

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            ArcenPoint center = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;
            int cityCount = CityPositions.GetLength( 0 );
            int edgeCount = CityEdges.GetLength( 0 );
            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( cityCount );
            for ( int i = 0; i < cityCount; i++ )
            {
                int x = center.X + CityPositions[i, 0];
                int y = center.Y + CityPositions[i, 1];
                Planet p = galaxy.AddPlanet( PlanetType.Normal, ArcenPoint.Create( x, y ),
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                allPlanets.Add( p );
            }
            BadgerUtilityMethods.EnforcePlanetMinimumSpacing( allPlanets, 50 );
            for ( int i = 0; i < edgeCount; i++ )
            {
                int a = CityEdges[i, 0];
                int b = CityEdges[i, 1];
                allPlanets[a].AddLinkTo( allPlanets[b] );
            }
            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }

    public class Mapgen_RealWorldMaps : IMapGenerator
    {
        // Ordered alphabetically by display name to match the XML choice_value ids.
        private static readonly IMapGenerator[] Delegates = new IMapGenerator[]
        {
            new Mapgen_AfricaCities(),      // 0
            new Mapgen_BalkansMap(),        // 1
            new Mapgen_EuropeanCities(),    // 2
            new Mapgen_IndiaCities(),       // 3
            new Mapgen_IndonesiaMap(),      // 4
            new Mapgen_JapanCities(),       // 5
            new Mapgen_PhilippinesMap(),    // 6
            new Mapgen_RomanEmpire(),       // 7
            new Mapgen_ScandinaviaMap(),    // 8
            new Mapgen_USCities(),          // 9
            new Mapgen_WarringStatesMap(),  // 10
            new Mapgen_SilkRoadMap(),       // 11
            new Mapgen_LevantMap(),         // 12
            new Mapgen_MekongMap(),         // 13
            new Mapgen_AndesMap(),                      // 14
            new Mapgen_CentralAmericaCaribbean(),       // 15
        };

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            for ( int i = 0; i < Delegates.Length; i++ )
                Delegates[i].ClearAllMyDataForQuitToMainMenuOrBeforeNewMap();
        }

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            int choice = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "RealWorldMap" ).RelatedIntValue;
            if ( choice < 0 || choice >= Delegates.Length )
                choice = 0;
            Delegates[choice].GenerateMapStructureOnly( galaxy, Context, mapConfig, mapType );
        }
    }
}
