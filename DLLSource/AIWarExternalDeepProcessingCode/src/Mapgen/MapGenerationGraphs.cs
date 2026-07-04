using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

namespace Arcen.AIW2.External
{
    // n-prism: two concentric n-cycles (inner and outer rings) connected by n radial rungs.
    // Every planet has exactly 3 connections.
    public class Mapgen_Prism : IMapGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            int n = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "PrismSize" ).RelatedIntValue;
            if ( n < 4 ) n = 4;

            // Scale so arc spacing on the inner ring is ~160 units: 2π*r/n = 160 → r = 160n/(2π).
            // Outer ring is ~1.8× the inner. Cap outer at 900 to stay on the galaxy map.
            double innerRadius = Math.Max( 280.0, 160.0 * n / (2.0 * Math.PI) );
            double outerRadius = innerRadius * 1.8;
            if ( outerRadius > 900.0 )
            {
                outerRadius = 900.0;
                innerRadius = 900.0 / 1.8;
            }

            ArcenPoint center = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;
            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( 2 * n );

            // Inner ring: indices 0..n-1. Start at the top (-π/2) and go clockwise.
            for ( int i = 0; i < n; i++ )
            {
                double angle = 2.0 * Math.PI * i / n - Math.PI / 2.0;
                allPlanets.Add( galaxy.AddPlanet( PlanetType.Normal,
                    ArcenPoint.Create( center.X + (int)(innerRadius * Math.Cos( angle )),
                                       center.Y + (int)(innerRadius * Math.Sin( angle )) ),
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) ) );
            }

            // Outer ring: indices n..2n-1. Same angles as inner.
            for ( int i = 0; i < n; i++ )
            {
                double angle = 2.0 * Math.PI * i / n - Math.PI / 2.0;
                allPlanets.Add( galaxy.AddPlanet( PlanetType.Normal,
                    ArcenPoint.Create( center.X + (int)(outerRadius * Math.Cos( angle )),
                                       center.Y + (int)(outerRadius * Math.Sin( angle )) ),
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) ) );
            }

            // Inner ring cycle
            for ( int i = 0; i < n; i++ )
                allPlanets[i].AddLinkTo( allPlanets[(i + 1) % n] );

            // Outer ring cycle
            for ( int i = 0; i < n; i++ )
                allPlanets[n + i].AddLinkTo( allPlanets[n + (i + 1) % n] );

            // Radial rungs
            for ( int i = 0; i < n; i++ )
                allPlanets[i].AddLinkTo( allPlanets[n + i] );

            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }
    }

    // Random mix of famous graphs arranged as spatially separated clusters,
    // connected to each other via a minimum spanning tree of inter-cluster wormholes.
    // The graph types and cluster count are chosen automatically to approximate the
    // requested planet count.
    public class Mapgen_FamousGraphs : IMapGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            int targetPlanets = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            if ( targetPlanets < 20 ) targetPlanets = 20;

            int[] graphChoices = PickGraphMix( targetPlanets, Context );
            int clusterCount = graphChoices.Length;

            double maxRadius = 0;
            for ( int i = 0; i < clusterCount; i++ )
            {
                double r = GraphRadius( graphChoices[i] );
                if ( r > maxRadius ) maxRadius = r;
            }
            int distanceBetweenCenters = (int)(maxRadius * 2.3 + 120);

            ThrowawayListCanMemLeak<ArcenPoint> centers = new ThrowawayListCanMemLeak<ArcenPoint>( clusterCount );
            BadgerUtilityMethods.addPointsInStartScreen( clusterCount, Context, distanceBetweenCenters, centers, false, false );

            int totalPlanets = 0;
            for ( int i = 0; i < clusterCount; i++ ) totalPlanets += GraphNodeCount( graphChoices[i] );

            ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<Planet>> allClusters = new ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<Planet>>( clusterCount );
            ThrowawayListCanMemLeak<Planet> allPlanets = new ThrowawayListCanMemLeak<Planet>( totalPlanets );

            for ( int i = 0; i < clusterCount; i++ )
            {
                ThrowawayListCanMemLeak<Planet> cluster = SpawnGraph( graphChoices[i], centers[i], GraphRadius( graphChoices[i] ), galaxy, Context );
                allClusters.Add( cluster );
                allPlanets.AddRange( cluster );
            }

            int[,] conn = BadgerUtilityMethods.createMinimumSpanningTreeLinks( centers );
            for ( int i = 0; i < clusterCount; i++ )
                for ( int j = i + 1; j < clusterCount; j++ )
                    if ( conn[i, j] == 1 )
                        BadgerUtilityMethods.linkPlanetLists( galaxy, allClusters[i], allClusters[j], centers[j] );

            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, allPlanets );
        }

        // Local search: pick a random mix of graphs whose total planet count is as close
        // as possible to targetPlanets, preferring all-distinct graph types.
        // Phase 1 (first 75% of iterations) enforces variety — rejects swaps that would
        // introduce a duplicate. Phase 2 relaxes that so we still converge on a good count.
        private static int[] PickGraphMix( int targetPlanets, ArcenHostOnlySimContext Context )
        {
            // Sizes indexed by graph choice constant (0=Petersen .. 14=Tietze)
            int[] sizes = { 10, 14, 18, 20, 24, 7, 12, 6, 4, 8, 6, 20, 12, 8, 12, 16, 12, 11 };
            int graphCount = sizes.Length;

            // Average graph size ≈ 11; keep cluster count between 2 and 8.
            int k = (int)Math.Round( targetPlanets / 11.0 );
            if ( k < 2 ) k = 2;
            if ( k > 8 ) k = 8;
            if ( k > graphCount ) k = graphCount;

            // Start with k distinct random choices.
            int[] choices = new int[k];
            bool[] used   = new bool[graphCount];
            for ( int i = 0; i < k; i++ )
            {
                int c;
                do { c = Context.RandomToUse.Next( 0, graphCount ); } while ( used[c] );
                choices[i] = c;
                used[c]    = true;
            }

            int currentSum = 0;
            for ( int i = 0; i < k; i++ ) currentSum += sizes[choices[i]];

            const int totalIters  = 2000;
            const int strictIters = 1500; // enforce variety for first 75%

            for ( int iter = 0; iter < totalIters; iter++ )
            {
                int slot      = Context.RandomToUse.Next( 0, k );
                int newChoice = Context.RandomToUse.Next( 0, graphCount );
                int newSum    = currentSum - sizes[choices[slot]] + sizes[newChoice];

                if ( Math.Abs( newSum - targetPlanets ) > Math.Abs( currentSum - targetPlanets ) )
                    continue;

                if ( iter < strictIters )
                {
                    // Reject if another slot already uses this graph type.
                    bool duplicate = false;
                    for ( int j = 0; j < k; j++ )
                        if ( j != slot && choices[j] == newChoice ) { duplicate = true; break; }
                    if ( duplicate ) continue;
                }

                choices[slot] = newChoice;
                currentSum    = newSum;
            }

            return choices;
        }

        private static double GraphRadius( int graphChoice )
        {
            switch ( graphChoice )
            {
                case 1: return 165; // Heawood 14
                case 2: return 185; // Pappus 18
                case 3: return 195; // Desargues 20
                case 4: return 215; // McGee 24
                case 5: return 120; // Moser spindle 7
                case 6: return 155; // Frucht 12
                case 7:  return 120; // K33 6
                case 8:  return  90; // Tetrahedron 4
                case 9:  return 120; // Cube 8
                case 10: return 100; // Octahedron 6
                case 11: return 195; // Dodecahedron 20
                case 12: return 150; // Icosahedron 12
                case 13: return 120; // Wagner 8
                case 14: return 155; // Tietze 12
                case 15: return 170; // Möbius-Kantor 16
                case 16: return 155; // Franklin 12
                case 17: return 145; // Herschel 11
                default: return 140; // Petersen 10
            }
        }

        private static int GraphNodeCount( int graphChoice )
        {
            switch ( graphChoice )
            {
                case 1: return 14;
                case 2: return 18;
                case 3: return 20;
                case 4: return 24;
                case 5: return 7;
                case 6: return 12;
                case 7:  return 6;
                case 8:  return 4;
                case 9:  return 8;
                case 10: return 6;
                case 11: return 20;
                case 12: return 12;
                case 13: return 8;
                case 14: return 12;
                case 15: return 16;
                case 16: return 12;
                case 17: return 11;
                default: return 10;
            }
        }

        private static ThrowawayListCanMemLeak<Planet> SpawnGraph( int graphChoice, ArcenPoint center, double radius, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            switch ( graphChoice )
            {
                case 1: return SpawnHeawood( center, radius, galaxy, Context );
                case 2: return SpawnPappus( center, radius, galaxy, Context );
                case 3: return SpawnDesargues( center, radius, galaxy, Context );
                case 4: return SpawnMcGee( center, radius, galaxy, Context );
                case 5: return SpawnMoserSpindle( center, radius, galaxy, Context );
                case 6: return SpawnFrucht( center, radius, galaxy, Context );
                case 7:  return SpawnK33( center, radius, galaxy, Context );
                case 8:  return SpawnTetrahedron( center, radius, galaxy, Context );
                case 9:  return SpawnCube( center, radius, galaxy, Context );
                case 10: return SpawnOctahedron( center, radius, galaxy, Context );
                case 11: return SpawnDodecahedron( center, radius, galaxy, Context );
                case 12: return SpawnIcosahedron( center, radius, galaxy, Context );
                case 13: return SpawnWagner( center, radius, galaxy, Context );
                case 14: return SpawnTietze( center, radius, galaxy, Context );
                case 15: return SpawnMobiusKantor( center, radius, galaxy, Context );
                case 16: return SpawnFranklin( center, radius, galaxy, Context );
                case 17: return SpawnHerschel( center, radius, galaxy, Context );
                default: return SpawnPetersen( center, radius, galaxy, Context );
            }
        }

        private static Planet MakePlanet( ArcenPoint center, double radius, double angle, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            return galaxy.AddPlanet( PlanetType.Normal,
                ArcenPoint.Create( center.X + (int)(radius * Math.Cos( angle )),
                                   center.Y + (int)(radius * Math.Sin( angle )) ),
                World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
        }

        private static ThrowawayListCanMemLeak<Planet> OnCircle( int n, ArcenPoint center, double radius, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            var p = new ThrowawayListCanMemLeak<Planet>( n );
            for ( int i = 0; i < n; i++ )
                p.Add( MakePlanet( center, radius, 2.0 * Math.PI * i / n - Math.PI / 2.0, galaxy, Context ) );
            return p;
        }

        private static void Ring( ThrowawayListCanMemLeak<Planet> p, int n )
        {
            for ( int i = 0; i < n; i++ ) p[i].AddLinkTo( p[(i + 1) % n] );
        }

        private static void Link( ThrowawayListCanMemLeak<Planet> p, int[,] pairs )
        {
            for ( int i = 0; i < pairs.GetLength( 0 ); i++ ) p[pairs[i, 0]].AddLinkTo( p[pairs[i, 1]] );
        }

        // Petersen: outer pentagon (0–4) + inner pentagram (5–9). LCF implicit.
        private static ThrowawayListCanMemLeak<Planet> SpawnPetersen( ArcenPoint center, double r, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            var p = new ThrowawayListCanMemLeak<Planet>( 10 );
            for ( int i = 0; i < 5; i++ ) p.Add( MakePlanet( center, r,       2.0 * Math.PI * i / 5 - Math.PI / 2.0, galaxy, Context ) );
            for ( int i = 0; i < 5; i++ ) p.Add( MakePlanet( center, r * 0.5, 2.0 * Math.PI * i / 5 - Math.PI / 2.0, galaxy, Context ) );
            for ( int i = 0; i < 5; i++ ) p[i].AddLinkTo( p[(i + 1) % 5] );
            int[] star = { 5, 7, 9, 6, 8 };
            for ( int i = 0; i < 5; i++ ) p[star[i]].AddLinkTo( p[star[(i + 1) % 5]] );
            for ( int i = 0; i < 5; i++ ) p[i].AddLinkTo( p[5 + i] );
            return p;
        }

        // Heawood (14, girth 6). LCF: [5,−5]^7
        private static ThrowawayListCanMemLeak<Planet> SpawnHeawood( ArcenPoint center, double r, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            var p = OnCircle( 14, center, r, galaxy, Context );
            Ring( p, 14 );
            Link( p, new int[,] { {0,5},{1,10},{2,7},{3,12},{4,9},{6,11},{8,13} } );
            return p;
        }

        // Pappus (18, bipartite). LCF: [5,7,−7,7,−7,−5]^3
        private static ThrowawayListCanMemLeak<Planet> SpawnPappus( ArcenPoint center, double r, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            var p = OnCircle( 18, center, r, galaxy, Context );
            Ring( p, 18 );
            Link( p, new int[,] { {0,5},{1,8},{2,13},{3,10},{4,15},{6,11},{7,14},{9,16},{12,17} } );
            return p;
        }

        // Desargues (20). LCF: [5,−5,9,−9]^5
        private static ThrowawayListCanMemLeak<Planet> SpawnDesargues( ArcenPoint center, double r, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            var p = OnCircle( 20, center, r, galaxy, Context );
            Ring( p, 20 );
            Link( p, new int[,] { {0,5},{1,16},{2,11},{3,14},{4,9},{6,15},{7,18},{8,13},{10,19},{12,17} } );
            return p;
        }

        // McGee (24, girth 7 — smallest such cubic graph). LCF: [12,7,−7]^8
        private static ThrowawayListCanMemLeak<Planet> SpawnMcGee( ArcenPoint center, double r, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            var p = OnCircle( 24, center, r, galaxy, Context );
            Ring( p, 24 );
            Link( p, new int[,] { {0,12},{1,8},{2,19},{3,15},{4,11},{5,22},{6,18},{7,14},{9,21},{10,17},{13,20},{16,23} } );
            return p;
        }

        // Tetrahedron / K₄ (4 vertices, 6 edges, 3-regular). Every planet connects to every other.
        private static ThrowawayListCanMemLeak<Planet> SpawnTetrahedron( ArcenPoint center, double r, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            var p = OnCircle( 4, center, r, galaxy, Context );
            Link( p, new int[,] { {0,1},{0,2},{0,3},{1,2},{1,3},{2,3} } );
            return p;
        }

        // Cube / Q₃ (8 vertices, 12 edges, 3-regular). The 3-dimensional hypercube.
        // Vertices i and j are connected iff i XOR j is a power of 2.
        private static ThrowawayListCanMemLeak<Planet> SpawnCube( ArcenPoint center, double r, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            var p = OnCircle( 8, center, r, galaxy, Context );
            Link( p, new int[,] { {0,1},{0,2},{0,4},{1,3},{1,5},{2,3},{2,6},{3,7},{4,5},{4,6},{5,7},{6,7} } );
            return p;
        }

        // Octahedron (6 vertices, 12 edges, 4-regular). Every pair connected except the three antipodal pairs.
        private static ThrowawayListCanMemLeak<Planet> SpawnOctahedron( ArcenPoint center, double r, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            var p = OnCircle( 6, center, r, galaxy, Context );
            // Missing: {0,5},{1,4},{2,3} (antipodal pairs)
            Link( p, new int[,] {
                {0,1},{0,2},{0,3},{0,4},
                {1,2},{1,3},{1,5},
                {2,4},{2,5},
                {3,4},{3,5},
                {4,5}
            } );
            return p;
        }

        // Dodecahedron (20 vertices, 30 edges, 3-regular, Hamiltonian, girth 5). Platonic solid.
        // Schlegel layout: outer pentagon → belt → inner pentagon.
        private static ThrowawayListCanMemLeak<Planet> SpawnDodecahedron( ArcenPoint center, double r, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            var p = new ThrowawayListCanMemLeak<Planet>( 20 );
            double half = Math.PI / 5.0; // 36° offset for alternating rings
            for ( int i = 0; i < 5; i++ ) p.Add( MakePlanet( center, r,        2*Math.PI*i/5 - Math.PI/2,        galaxy, Context ) ); // outer (0-4)
            for ( int i = 0; i < 5; i++ ) p.Add( MakePlanet( center, r * 0.65, 2*Math.PI*i/5 - Math.PI/2 + half, galaxy, Context ) ); // belt outer (5-9)
            for ( int i = 0; i < 5; i++ ) p.Add( MakePlanet( center, r * 0.35, 2*Math.PI*i/5 - Math.PI/2,        galaxy, Context ) ); // belt inner (10-14)
            for ( int i = 0; i < 5; i++ ) p.Add( MakePlanet( center, r * 0.15, 2*Math.PI*i/5 - Math.PI/2 + half, galaxy, Context ) ); // inner (15-19)
            Link( p, new int[,] {
                {0,1},{1,2},{2,3},{3,4},{4,0},                            // outer pentagon
                {0,5},{1,6},{2,7},{3,8},{4,9},                            // outer → belt outer
                {5,10},{5,14},{6,10},{6,11},{7,11},{7,12},{8,12},{8,13},{9,13},{9,14}, // belt outer → belt inner
                {10,15},{11,16},{12,17},{13,18},{14,19},                  // belt inner → inner
                {15,16},{16,17},{17,18},{18,19},{19,15}                   // inner pentagon
            } );
            return p;
        }

        // Icosahedron (12 vertices, 30 edges, 5-regular). Two apex planets plus two 5-rings.
        private static ThrowawayListCanMemLeak<Planet> SpawnIcosahedron( ArcenPoint center, double r, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            var p = new ThrowawayListCanMemLeak<Planet>( 12 );
            double half = Math.PI / 5.0;
            for ( int i = 0; i < 5; i++ ) p.Add( MakePlanet( center, r * 0.5, 2*Math.PI*i/5 - Math.PI/2,        galaxy, Context ) ); // upper ring (0-4)
            for ( int i = 0; i < 5; i++ ) p.Add( MakePlanet( center, r * 0.5, 2*Math.PI*i/5 - Math.PI/2 + half, galaxy, Context ) ); // lower ring (5-9)
            p.Add( MakePlanet( center, r, -Math.PI/2, galaxy, Context ) ); // top apex (10)
            p.Add( MakePlanet( center, r,  Math.PI/2, galaxy, Context ) ); // bottom apex (11)
            Link( p, new int[,] {
                {0,1},{1,2},{2,3},{3,4},{4,0},                // upper ring
                {5,6},{6,7},{7,8},{8,9},{9,5},                // lower ring
                {10,0},{10,1},{10,2},{10,3},{10,4},           // top apex
                {11,5},{11,6},{11,7},{11,8},{11,9},           // bottom apex
                {0,5},{0,9},{1,5},{1,6},{2,6},{2,7},{3,7},{3,8},{4,8},{4,9} // cross
            } );
            return p;
        }

        // Wagner graph (8 vertices, 12 edges, 3-regular). Octagon with 4-step chords — Möbius-strip topology.
        private static ThrowawayListCanMemLeak<Planet> SpawnWagner( ArcenPoint center, double r, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            var p = OnCircle( 8, center, r, galaxy, Context );
            Ring( p, 8 );
            Link( p, new int[,] { {0,4},{1,5},{2,6},{3,7} } );
            return p;
        }

        // Tietze's graph (12 vertices, 18 edges, cubic, non-Hamiltonian, girth 3).
        // Derived from the Petersen graph by replacing one vertex with a triangle.
        // Triangle: {9,10,11}. The rest is the Petersen graph minus one vertex.
        private static ThrowawayListCanMemLeak<Planet> SpawnTietze( ArcenPoint center, double r, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            var p = OnCircle( 12, center, r, galaxy, Context );
            Link( p, new int[,] {
                {0,1},{0,5},{0,9},
                {1,2},{1,6},
                {2,3},{2,7},
                {3,8},{3,10},
                {4,6},{4,7},{4,11},
                {5,7},{5,8},
                {6,8},
                {9,10},{9,11},
                {10,11}
            } );
            return p;
        }

        // Frucht graph (12 vertices, 18 edges). The smallest cubic graph with trivial
        // automorphism group — every planet is topologically unique. Girth 3 (triangle: 7-8-11).
        private static ThrowawayListCanMemLeak<Planet> SpawnFrucht( ArcenPoint center, double r, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            var p = OnCircle( 12, center, r, galaxy, Context );
            Link( p, new int[,] {
                {0,1},{0,6},{0,7},
                {1,2},{1,11},
                {2,3},{2,8},
                {3,4},{3,9},
                {4,5},{4,10},
                {5,6},{5,9},
                {6,10},
                {7,8},{7,11},
                {8,11},
                {9,10}
            } );
            return p;
        }

        // K_{3,3}: complete bipartite graph (6 vertices, 9 edges, 3-regular, bipartite).
        // The utility graph — the smallest non-planar graph.
        // Left set {0,1,2} each connects to every vertex in right set {3,4,5}.
        private static ThrowawayListCanMemLeak<Planet> SpawnK33( ArcenPoint center, double r, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            int cx = center.X, cy = center.Y;
            int ri = (int)r, rs = (int)(r * 0.6);
            ArcenPoint[] pos = {
                ArcenPoint.Create( cx - ri, cy + rs ), // 0 left top
                ArcenPoint.Create( cx - ri, cy      ), // 1 left middle
                ArcenPoint.Create( cx - ri, cy - rs ), // 2 left bottom
                ArcenPoint.Create( cx + ri, cy + rs ), // 3 right top
                ArcenPoint.Create( cx + ri, cy      ), // 4 right middle
                ArcenPoint.Create( cx + ri, cy - rs ), // 5 right bottom
            };
            var p = new ThrowawayListCanMemLeak<Planet>( 6 );
            for ( int i = 0; i < 6; i++ )
                p.Add( galaxy.AddPlanet( PlanetType.Normal, pos[i],
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) ) );
            for ( int i = 0; i < 3; i++ )
                for ( int j = 0; j < 3; j++ )
                    p[i].AddLinkTo( p[3 + j] );
            return p;
        }

        // Moser spindle (7 vertices, 11 edges, chromatic number 4).
        // Famous unit-distance graph proving the plane requires ≥4 colors.
        // Layout: left tip (0), inner-left pair (1,2), center hub (3, degree 4),
        //         inner-right pair (4,5), right tip (6).
        private static ThrowawayListCanMemLeak<Planet> SpawnMoserSpindle( ArcenPoint center, double r, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            int cx = center.X, cy = center.Y;
            int ri = (int)r, rh = (int)(r * 0.5);
            ArcenPoint[] pos = {
                ArcenPoint.Create( cx - ri, cy      ), // 0 left tip
                ArcenPoint.Create( cx - rh, cy + rh ), // 1 inner-left top
                ArcenPoint.Create( cx - rh, cy - rh ), // 2 inner-left bottom
                ArcenPoint.Create( cx,      cy      ), // 3 center hub
                ArcenPoint.Create( cx + rh, cy + rh ), // 4 inner-right top
                ArcenPoint.Create( cx + rh, cy - rh ), // 5 inner-right bottom
                ArcenPoint.Create( cx + ri, cy      ), // 6 right tip
            };
            var p = new ThrowawayListCanMemLeak<Planet>( 7 );
            for ( int i = 0; i < 7; i++ )
                p.Add( galaxy.AddPlanet( PlanetType.Normal, pos[i],
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) ) );
            Link( p, new int[,] {
                {0,1},{0,2},{0,6},
                {1,3},{1,5},
                {2,3},{2,4},
                {3,4},{3,5},
                {4,6},{5,6}
            } );
            return p;
        }

        // Möbius-Kantor: generalized Petersen GP(8,3). Outer 8-ring + inner 8-star (skip-3) + 8 spokes.
        private static ThrowawayListCanMemLeak<Planet> SpawnMobiusKantor( ArcenPoint center, double r, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            var p = new ThrowawayListCanMemLeak<Planet>( 16 );
            for ( int i = 0; i < 8; i++ )
                p.Add( MakePlanet( center, r,       2.0 * Math.PI * i / 8 - Math.PI / 2.0,                    galaxy, Context ) );
            for ( int i = 0; i < 8; i++ )
                p.Add( MakePlanet( center, r * 0.5, 2.0 * Math.PI * i / 8 - Math.PI / 2.0 + Math.PI / 8.0,   galaxy, Context ) );
            for ( int i = 0; i < 8; i++ ) p[i].AddLinkTo( p[(i + 1) % 8] );         // outer ring
            for ( int i = 0; i < 8; i++ ) p[8 + i].AddLinkTo( p[8 + (i + 3) % 8] ); // inner star (skip 3)
            for ( int i = 0; i < 8; i++ ) p[i].AddLinkTo( p[8 + i] );                // spokes
            return p;
        }

        // Franklin: two 6-cycles (outer 0-5, inner 6-11) + cross edges swapping adjacent pairs (i → 6+(i^1)).
        // Bipartite, girth 4, non-isomorphic to the hexagonal prism.
        private static ThrowawayListCanMemLeak<Planet> SpawnFranklin( ArcenPoint center, double r, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            var p = new ThrowawayListCanMemLeak<Planet>( 12 );
            for ( int i = 0; i < 6; i++ )
                p.Add( MakePlanet( center, r,       2.0 * Math.PI * i / 6 - Math.PI / 2.0, galaxy, Context ) );
            for ( int i = 0; i < 6; i++ )
                p.Add( MakePlanet( center, r * 0.5, 2.0 * Math.PI * i / 6 - Math.PI / 2.0, galaxy, Context ) );
            for ( int i = 0; i < 6; i++ ) p[i].AddLinkTo( p[(i + 1) % 6] );          // outer hexagon
            for ( int i = 0; i < 6; i++ ) p[6 + i].AddLinkTo( p[6 + (i + 1) % 6] ); // inner hexagon
            for ( int i = 0; i < 6; i++ ) p[i].AddLinkTo( p[6 + (i ^ 1)] );          // cross: swap adjacent pairs
            return p;
        }

        // Herschel: smallest non-Hamiltonian polyhedral graph. Bipartite: outer hexagon (A, all deg-3)
        // + inner pentagon (B, three deg-4 and two deg-3). Non-Hamiltonian because |A|≠|B|.
        private static ThrowawayListCanMemLeak<Planet> SpawnHerschel( ArcenPoint center, double r, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            var p = new ThrowawayListCanMemLeak<Planet>( 11 );
            // A-part: 6 vertices on outer circle (p[0..5])
            for ( int i = 0; i < 6; i++ )
                p.Add( MakePlanet( center, r,       2.0 * Math.PI * i / 6 - Math.PI / 2.0, galaxy, Context ) );
            // B-part: 5 vertices on inner circle (p[6..10])
            for ( int i = 0; i < 5; i++ )
                p.Add( MakePlanet( center, r * 0.5, 2.0 * Math.PI * i / 5 - Math.PI / 2.0, galaxy, Context ) );
            // p[6],p[7],p[8] are degree-4 B-vertices; p[9],p[10] are degree-3 B-vertices
            Link( p, new int[,] {
                {6,0},{6,1},{6,3},{6,4},
                {7,1},{7,2},{7,3},{7,4},
                {8,0},{8,2},{8,4},{8,5},
                {9,0},{9,3},{9,5},
                {10,1},{10,2},{10,5}
            } );
            return p;
        }
    }

    public class Mapgen_Graphs : IMapGenerator
    {
        private static readonly IMapGenerator[] Delegates = new IMapGenerator[]
        {
            new Mapgen_Prism(),             // 0
            new Mapgen_FamousGraphs(), // 1
        };

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            for ( int i = 0; i < Delegates.Length; i++ )
                Delegates[i].ClearAllMyDataForQuitToMainMenuOrBeforeNewMap();
        }

        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            int choice = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "GraphType" ).RelatedIntValue;
            if ( choice < 0 || choice >= Delegates.Length )
                choice = 0;
            Delegates[choice].GenerateMapStructureOnly( galaxy, Context, mapConfig, mapType );
        }
    }
}
