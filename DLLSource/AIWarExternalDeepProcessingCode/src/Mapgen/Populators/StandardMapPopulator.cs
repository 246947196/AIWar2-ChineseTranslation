using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using UnityEngine;
using System.Text;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// Return true if the passed entity type should be seeded and specify values for weight and max in galaxy.
    /// These will be used instead of the normal tags on the entity type.
    /// </summary>
    public delegate bool GetSeedingWeight(GameEntityTypeData type, out int weight, out int max);
    
    public delegate void OnSeeded(GameEntity_Squad squad);
    
    /// <summary>
    /// Return true if the passed planet already has an entity similar to to the passed type.
    /// This is so seeding can avoid duplicates near one another.
    /// </summary>
    public delegate bool GetHasSimilar(GameEntityTypeData type, Planet planet);
    
    public struct SeedArgs
    {
        public static readonly SeedArgs Default = new SeedArgs()
        {
            CountPer = MapGenCountPerPlanet.One,
        };
        
        public Faction FactionOrNull;
        public FactionType FactionType;
        public SpecialEntityType SpecialTypeOrNone;
        public string TagOrEmpty;
        public SeedingType SeedType;
        public int Count;
        public MapGenCountPerPlanet CountPer;
        public MapGenSeedStyle SeedStyle;
        public int MinDistanceFromHumanHomeworld;
        public int MaxDistanceFromHumanHomeworld;
        public int MinDistanceFromAIHomeworld;
        public int MaxDistanceFromAIHomeworld;
        public PlanetSeedingZone SeedingZone;
        public SeedingExpansionType ExpansionStyle;
        public Faction SpecificFactionToTryToBeCloseTo;
        public Int16 SpecificFactionTryToBeAtMost;
        public GetSeedingWeight GetWeight;
        public GetHasSimilar GetHasSimilar;
        public OnSeeded Callback;
        public ThrowawayListCanMemLeak<Planet> SeededPlanets;
        public List<GameEntity_Squad> SeededEntities;
        
        /// <summary>
        /// The improved seeder will consider the min/max distance values as relative
        /// to a normal 80 planet realistic galaxy. If the galaxy is significantly
        /// more or less connected, based on num of hops from player start to fartherst
        /// corner of galaxy, it rescales the values relative to that.
        ///
        /// Sometimes though you *really* want stuff to be precisely at the distance specified.
        /// So this allows you to disable that.
        /// </summary>
        public bool DisableDistanceRescaling;
    }
    
    public class StandardMapPopulator : IMapPopulator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        public static StandardMapPopulator Instance;

        private static ReferenceTracker RefTracker;

        private readonly DrawBag<IWormholePlacer> WormholePlacers_General = DrawBag<IWormholePlacer>.Create_WillNeverBeGCed( 100, "StandardMapPopulator-WormholePlacers_General" );
        private readonly DrawBag<IWormholePlacer> WormholePlacers_PlayerHome = DrawBag<IWormholePlacer>.Create_WillNeverBeGCed( 100, "StandardMapPopulator-WormholePlacers_PlayerHome" );
        private readonly DrawBag<AIDefensePlacer> DefensePlacers = DrawBag<AIDefensePlacer>.Create_WillNeverBeGCed( 100, "StandardMapPopulator-DefensePlacers" );

        public StandardMapPopulator()
        {
            Instance = this;

            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "StandardMapPopulators" );
            RefTracker.IncrementObjectCount();

            {
                // All in a ring at one of the predefined distances
                this.WormholePlacers_General.AddItem( new WormholePlacer_Default( FInt.FromParts( 0, 750 ), FInt.FromParts( 0, 750 ) ), 3 );
                this.WormholePlacers_General.AddItem( new WormholePlacer_Default( FInt.FromParts( 0, 250 ), FInt.FromParts( 0, 250 ) ), 3 );
                this.WormholePlacers_General.AddItem( new WormholePlacer_Default( FInt.FromParts( 0, 400 ), FInt.FromParts( 0, 400 ) ), 3 );
                this.WormholePlacers_General.AddItem( new WormholePlacer_Default( FInt.FromParts( 0, 600 ), FInt.FromParts( 0, 600 ) ), 3 );

                // Each at randomly selected distances in the predefined ranges; less common than the rings
                this.WormholePlacers_General.AddItem( new WormholePlacer_Default( FInt.FromParts( 0, 250 ), FInt.FromParts( 0, 750 ) ), 1 );
                this.WormholePlacers_General.AddItem( new WormholePlacer_Default( FInt.FromParts( 0, 400 ), FInt.FromParts( 0, 750 ) ), 1 );
                this.WormholePlacers_General.AddItem( new WormholePlacer_Default( FInt.FromParts( 0, 600 ), FInt.FromParts( 0, 750 ) ), 1 );
                this.WormholePlacers_General.AddItem( new WormholePlacer_Default( FInt.FromParts( 0, 250 ), FInt.FromParts( 0, 600 ) ), 1 );
                this.WormholePlacers_General.AddItem( new WormholePlacer_Default( FInt.FromParts( 0, 400 ), FInt.FromParts( 0, 600 ) ), 1 );

                // All in a ring at one of the predefined distances, for the player home
                this.WormholePlacers_PlayerHome.AddItem( new WormholePlacer_Default( FInt.FromParts( 0, 850 ), FInt.FromParts( 0, 850 ) ), 3 );
                //this.WormholePlacers_PlayerHome.AddItem( new WormholePlacer_Default( FInt.FromParts( 0, 200 ), FInt.FromParts( 0, 200 ) ), 3 );
            }

            {
                foreach ( AIDefensePlacer row in AIDefensePlacerTable.Instance.Rows )
                    this.DefensePlacers.AddItem( row, 1 );
            }
        }

        //Set immediately before PlanetsNearNomads.Sort(...) so the comparison can be a non-capturing
        //static delegate.  [ThreadStatic] for safety since map generation may run off the main thread.
        [ThreadStatic] private static Planet cb_nomadCullSortPlanet;

        public void AddNomadPlanetsIfNecessary( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( NomadPlanetsFactionBaseInfo.Instance == null )
                return;

            int nomadsToMake = NomadPlanetsFactionBaseInfo.Instance.Intensity; //this is shown as "Number of Planets" on the settings screen
            if ( nomadsToMake == 0 )
                return;
            int radiusOfGalaxy = 0;
            int preferredDistanceFromOtherPlanets = 40;
            int preferredDistanceFromPoint = 10;

            //we use the centroid of the galaxy rather than Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter because
            //map generation algorithms might generate "off-centered" galaxies.
            //Modders in particular might make a new map type that is centered "somewhere else"
            ArcenPoint centroid = galaxy.FindCentroidOfGalaxy();
            foreach ( Planet planet in galaxy.Planets( false ) )
            {
                int distance = Mat.ApproxDistanceBetweenPointsFast( centroid, planet.GalaxyLocation, -1 );
                if ( distance > radiusOfGalaxy )
                    radiusOfGalaxy = distance;
            }

            List<Planet> PlanetsNearNomads = Planet.GetTemporaryPlanetList( "StandardMapPop-AddNomadPlanetsIfNecessary-PlanetsNearNomads", 10f );
            if ( PlanetsNearNomads == null ) //blocked for teardown/shutdown; bail
                return;

            //find furthest out planet, then go in units of distance / nomadsToMake
            for ( int i = 0; i < nomadsToMake; i++ )
            {
                int distance = (radiusOfGalaxy / (nomadsToMake + 1)) * (i + 1);
                AngleDegrees angle = AngleDegrees.Create( (float)Context.RandomToUse.Next( 0, 360 ) );
                ArcenPoint newLocation = Mat.GetPointFromCircleCenter( centroid, distance, angle );
                newLocation = BadgerUtilityMethods.GetSafePointNearPoint( null, newLocation, galaxy, preferredDistanceFromOtherPlanets, 
                    preferredDistanceFromPoint, Context );
                Planet planet = galaxy.AddPlanet( PlanetType.Nomad, newLocation,
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                planet.Name = "Nomad " + (i + 1);
                planet.OriginalNomadDistance = distance;
                //find nearby planets, then update links. Note this isn't the most efficient code, but nomads never link to more than
                //4 planets, so shouldn't be too bad.
                int radiusForNearNeighbors = 100;
                int minPlanetsForNomadConnection = 2;
                int maxPlanetsForNomadConnection = 4;
                do
                {
                    PlanetsNearNomads.Clear();
                    foreach ( Planet otherPlanet in galaxy.Planets( false ) )
                    {
                        if ( planet == otherPlanet )
                            continue;
                        if ( Mat.DistanceBetweenPointsImprecise( planet.GalaxyLocation, otherPlanet.GalaxyLocation ) < radiusForNearNeighbors )
                            PlanetsNearNomads.Add( otherPlanet );
                    }
                    radiusForNearNeighbors += 100;
                } while ( PlanetsNearNomads.Count <= minPlanetsForNomadConnection );
                if ( PlanetsNearNomads.Count > maxPlanetsForNomadConnection )
                {
                    //if we have too many, cull the weak
                    cb_nomadCullSortPlanet = planet;
                    PlanetsNearNomads.Sort( static delegate ( Planet Left, Planet Right )
                    {
                        int lDistance = Mat.DistanceBetweenPointsImprecise( cb_nomadCullSortPlanet.GalaxyLocation, Left.GalaxyLocation );
                        int rDistance = Mat.DistanceBetweenPointsImprecise( cb_nomadCullSortPlanet.GalaxyLocation, Left.GalaxyLocation );
                        return lDistance.CompareTo( rDistance );
                    } );
                    PlanetsNearNomads.RemoveRange( maxPlanetsForNomadConnection - 1, PlanetsNearNomads.Count - maxPlanetsForNomadConnection );
                }
                for ( int j = 0; j < PlanetsNearNomads.Count; j++ )
                {
                    //set the appropriate links
                    Planet otherPlanet = PlanetsNearNomads[j];
                    if ( !planet.GetIsDirectlyLinkedTo( false, otherPlanet ) )
                    {
                        planet.AddLinkTo( otherPlanet );
                    }
                }
            }

            Planet.ReleaseTemporaryPlanetList( PlanetsNearNomads );
        }

        public void CalculateAllTheAsteroidCountsAndScienceAndHackingPerPlanet( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            int humanHomeworldCount = 0;
            int assignedEarlyCount = 0;
            int countByLoop = 0;
            foreach ( Planet planet in galaxy.Planets( false ) )
            {
                countByLoop++;
                switch (planet.PopulationType)
                {
                    case PlanetPopulationType.HumanHomeworld:
                        planet.Mapgen_WorkingAsteroidCount = 6;
                        humanHomeworldCount++;
                        assignedEarlyCount++;
                        break;
                    case PlanetPopulationType.ArkEmpireHumanHomeworld:
                        planet.Mapgen_WorkingAsteroidCount = 6;
                        humanHomeworldCount++;
                        assignedEarlyCount++;
                        break;
                    case PlanetPopulationType.DarkZenith:
                        planet.Mapgen_WorkingAsteroidCount = Context.RandomToUse.NextInclus( 6, 10 ); //Note that Dark Zenith planets do not exist at the time this routine is run
                        assignedEarlyCount++;
                        break;
                    case PlanetPopulationType.AIHomeworld:
                        planet.Mapgen_WorkingAsteroidCount = 10;
                        assignedEarlyCount++;
                        break;
                    case PlanetPopulationType.AIBastionWorld:
                        planet.Mapgen_WorkingAsteroidCount = Context.RandomToUse.NextInclus( 9, 12 );
                        assignedEarlyCount++;
                        break;
                    default:
                        planet.Mapgen_WorkingAsteroidCount = 0;
                        break;
                }
            }

            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "CalculateAllTheAsteroidCountsPerPlanet: planet.Count: " + countByLoop +
                    " assigned-early: " + assignedEarlyCount + " human-home: " + humanHomeworldCount );

            int highTarget = 10;
            if ( humanHomeworldCount > 4 )
                highTarget = 3;
            else if ( humanHomeworldCount > 3 )
                highTarget = 4;
            else if ( humanHomeworldCount > 2 )
                highTarget = 6;
            else if ( humanHomeworldCount > 1 )
                highTarget = 7;

            ThrowawayListCanMemLeak<Planet> workingPlanets = new ThrowawayListCanMemLeak<Planet>( 500 );
            foreach ( Planet planet in galaxy.Planets( false ) )
            {
                switch ( planet.PopulationType )
                {
                    case PlanetPopulationType.HumanHomeworld:
                    case PlanetPopulationType.ArkEmpireHumanHomeworld:

                        #region Make A List Of The Nearest highTarget Planets Not Already Defined
                        Int16 currentDistance = 2;
                        while ( workingPlanets.Count < highTarget && currentDistance < 10 )
                        {
                            foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops_NoFilters( currentDistance ) )
                            {
                                Planet other = _phd.Planet;
                                if ( other.Mapgen_WorkingAsteroidCount <= 0 && !workingPlanets.Contains( other ) )
                                {
                                    workingPlanets.Add( other );
                                    if ( workingPlanets.Count >= highTarget )
                                        break;
                                }
                            }
                            currentDistance++;
                        }
                        #endregion

                        if ( MapgenLogger.IsActive )
                            MapgenLogger.Log( "Number To Assign Near Homeworld: " + planet.Name +
                                " count: " + workingPlanets.Count );

                        //int originalCount = workingPlanets.Count;
                        Helper_AssignAndRemoveRandomPlanets( workingPlanets, 1, 9, 9, Context );
                        Helper_AssignAndRemoveRandomPlanets( workingPlanets, 1, 2, 2, Context );
                        Helper_AssignAndRemoveRandomPlanets( workingPlanets, 2, 6, 6, Context );
                        Helper_AssignAndRemoveRandomPlanets( workingPlanets, 1, 7, 7, Context );
                        Helper_AssignAndRemoveRandomPlanets( workingPlanets, 1, 5, 5, Context );
                        Helper_AssignAndRemoveRandomPlanets( workingPlanets, 1, 8, 8, Context );
                        Helper_AssignAndRemoveRandomPlanets( workingPlanets, 1, 4, 4, Context );
                        Helper_AssignAndRemoveRandomPlanets( workingPlanets, 1, 10, 10, Context );
                        Helper_AssignAndRemoveRandomPlanets( workingPlanets, 1, 3, 3, Context );
                        Helper_AssignAndRemoveRandomPlanets( workingPlanets, 1, 11, 11, Context );
                        Helper_AssignAndRemoveRandomPlanets( workingPlanets, 1, 2, 2, Context );
                        break;
                }
            }

            workingPlanets.Clear();
            #region Fill workingPlanets with all remaining ones
            foreach ( Planet planet in galaxy.Planets( false ) )
            {
                if ( planet.Mapgen_WorkingAsteroidCount <= 0 )
                    workingPlanets.Add( planet );
            }
            #endregion

            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "Number To Assign Out In The Middle: " + workingPlanets.Count );

            int originalCount = workingPlanets.Count;
            int sixCount = originalCount / 3;
            if ( sixCount > 10 )
            {
                int sixOverage = sixCount - 10;
                int sevenCount = sixOverage / 2;
                int fiveCount = sixOverage - sevenCount;
            }
            Helper_AssignAndRemoveRandomPlanets( workingPlanets, sixCount, 6, 6, Context );
            int eightCount = sixCount / 2;
            int fourCount = sixCount - eightCount;
            Helper_AssignAndRemoveRandomPlanets( workingPlanets, eightCount, 8, 8, Context );
            Helper_AssignAndRemoveRandomPlanets( workingPlanets, fourCount, 4, 4, Context );

            int nineCount = sixCount / 3;
            int threeCount = nineCount;
            int tenCount = nineCount / 2;
            int twoCount = tenCount;
            int twelveCount = tenCount;

            Helper_AssignAndRemoveRandomPlanets( workingPlanets, nineCount, 9, 9, Context );
            Helper_AssignAndRemoveRandomPlanets( workingPlanets, threeCount, 3, 3, Context );
            Helper_AssignAndRemoveRandomPlanets( workingPlanets, tenCount, 10, 10, Context );
            Helper_AssignAndRemoveRandomPlanets( workingPlanets, twoCount, 2, 2, Context );
            Helper_AssignAndRemoveRandomPlanets( workingPlanets, twelveCount, 12, 12, Context );

            if ( workingPlanets.Count > 0 )
                Helper_AssignAndRemoveRandomPlanets( workingPlanets, workingPlanets.Count, 7, 11, Context );

            if ( AIWar2GalaxySettingQuickAccess.HackingPoints_Variance > 0 )
            {
                int min = (AIWar2GalaxySettingQuickAccess.HackingPoints_Amount * (100 - AIWar2GalaxySettingQuickAccess.HackingPoints_Variance)) / 100;
                int max = (AIWar2GalaxySettingQuickAccess.HackingPoints_Amount * (100 + AIWar2GalaxySettingQuickAccess.HackingPoints_Variance)) / 100;
                foreach ( Planet planet in galaxy.Planets( false ) )
                {
                    planet.OverrideHackingAmount = (short) Context.RandomToUse.NextInclus( min, max );
                }
            }

            if ( AIWar2GalaxySettingQuickAccess.SciencePoints_Variance > 0 )
            {
                int min = (AIWar2GalaxySettingQuickAccess.SciencePoints_Amount * (100 - AIWar2GalaxySettingQuickAccess.SciencePoints_Variance)) / 100;
                int max = (AIWar2GalaxySettingQuickAccess.SciencePoints_Amount * (100 + AIWar2GalaxySettingQuickAccess.SciencePoints_Variance)) / 100;
                foreach ( Planet planet in galaxy.Planets( false ) )
                {
                    planet.OverrideScienceAmount = (short) Context.RandomToUse.NextInclus( min, max );
                }
            }
        }

        #region Helper_AssignAndRemoveRandomPlanets
        private static void Helper_AssignAndRemoveRandomPlanets( IList<Planet> list, int CountToAssign, int MinAsteroidSpotsToGiveEach, int MaxAsteroidSpotsToGiveEach, ArcenHostOnlySimContext Context )
        {
            int toAssign = CountToAssign;
            int numerAssigned = 0;
            while ( list.Count > 0 && CountToAssign > 0 )
            {
                int index = Context.RandomToUse.Next( 0, list.Count );
                Planet plan = list[index];
                list.RemoveAt( index );
                CountToAssign--;
                numerAssigned++;
                if ( MinAsteroidSpotsToGiveEach >= MaxAsteroidSpotsToGiveEach )
                    plan.Mapgen_WorkingAsteroidCount = MinAsteroidSpotsToGiveEach;
                else
                    plan.Mapgen_WorkingAsteroidCount = Context.RandomToUse.NextInclus( MinAsteroidSpotsToGiveEach, MaxAsteroidSpotsToGiveEach );
            }


            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "Helper_AssignAndRemoveRandomPlanets: planet.Count: " + list.Count + " toAssign: " + toAssign + " numerAssigned: " + numerAssigned +
                    " Min-As: " + MinAsteroidSpotsToGiveEach + " Max-As: " + MaxAsteroidSpotsToGiveEach );
        }
        #endregion

        public void SeedNormalEntities( Planet planet, ArcenHostOnlySimContext Context, MapTypeData mapType, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( planet.Name + " SeedNormalEntities, planet.PopulationType " + planet.PopulationType + " planet.Factions.Count: " + planet.Factions.Count );

            int debugStage = 0;
            try
            {
                debugStage = 1;
                int wormholeRadius = (planet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 900 )).IntValue;

                int innerSystemMinimumRadius = (planet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 150 )).IntValue;
                int innerSystemMaximumRadius = (planet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 300 )).IntValue;

                ArcenPoint center = Engine_AIW2.Instance.CombatCenter;

                IWormholePlacer wormholePlacer;
                if ( (planet.PopulationType == PlanetPopulationType.HumanHomeworld || (planet.PopulationType == PlanetPopulationType.AIHomeworld)) ) //don't worry about this on AIBastionWorld
                    wormholePlacer = this.WormholePlacers_PlayerHome.PickRandomItemAndReplace( Context.RandomToUse );
                else
                    wormholePlacer = this.WormholePlacers_General.PickRandomItemAndReplace( Context.RandomToUse );
                if ( wormholePlacer == null )
                {
                    string wormholePlacerErrorText = "wormholePlacer is null at planet " + planet.Name;
                    if ( MapgenLogger.IsActive )
                        MapgenLogger.Log( wormholePlacerErrorText );
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( wormholePlacerErrorText );
                    return;
                }

                bool stillNeedsToSeedAIHomeworldStuff = planet.PopulationType == PlanetPopulationType.AIHomeworld; //don't do this on AIBastionWorld
                bool stillNeedsToSeedHumanHomeworldStuff = ( planet.PopulationType == PlanetPopulationType.HumanHomeworld || planet.PopulationType == PlanetPopulationType.ArkEmpireHumanHomeworld );

                debugStage = 2;
                switch ( planet.PopulationType )
                {
                    case PlanetPopulationType.DarkZenith:
                        //Can seed some stuff here if desired, instead of the DarkZenith faction
                        break;
                    case PlanetPopulationType.NonHomeworld:
                    case PlanetPopulationType.AIHomeworld:
                    case PlanetPopulationType.AIBastionWorld:
                    case PlanetPopulationType.HumanHomeworld:
                    case PlanetPopulationType.ArkEmpireHumanHomeworld:
                        {
                            debugStage = 3;

                            debugStage = 4;
                            AIDefensePlacer placer = planet.GetCurrentDefensePlacer( Context );
                            if ( placer == null )
                            {
                                string aiDefensePlacerErrorText = "AIDefensePlacer is null at planet " + planet.Name;
                                if ( MapgenLogger.IsActive )
                                    MapgenLogger.Log( aiDefensePlacerErrorText );
                                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( aiDefensePlacerErrorText );
                                break;
                            }
                            if ( placer.Implementation == null )
                            {
                                string aiDefensePlacerErrorText = "AIDefensePlacer Implementation is null for placer type " + placer.InternalName + " at planet " + planet.Name;
                                if ( MapgenLogger.IsActive )
                                    MapgenLogger.Log( aiDefensePlacerErrorText );
                                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( aiDefensePlacerErrorText );
                                break;
                            }
                            debugStage = 41;
                            ArcenPoint commandStationPoint;
                            if ( planet.PopulationType == PlanetPopulationType.HumanHomeworld  )
                                commandStationPoint = AIGuardPostAndCommandPlacers_Base.GetPointForCommandStation_HumanHome( Context, planet );
                            else
                            {
                                debugStage = 411;
                                if ( planet.GuardPostAndCommandPlacer == null || planet.GuardPostAndCommandPlacer?.Implementation == null )
                                {
                                    debugStage = 412;
                                    byte strongestAI = (byte)FactionUtilityMethods.Instance.GetHighestAIDifficulty();
                                    debugStage = 413;
                                    planet.SetGuardPostAndCommandPlacerFromPlanetStats( strongestAI, Context );
                                }
                                debugStage = 414;
                                AIGuardPostAndCommandPlacer guardPlacer = planet.GuardPostAndCommandPlacer;
                                if ( guardPlacer == null )
                                {
                                    string aiDefensePlacerErrorText = "AIGuardPostAndCommandPlacer is null at planet " + planet.Name;
                                    if ( MapgenLogger.IsActive )
                                        MapgenLogger.Log( aiDefensePlacerErrorText );
                                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( aiDefensePlacerErrorText );
                                    break;
                                }
                                if ( guardPlacer.Implementation == null )
                                {
                                    string aiDefensePlacerErrorText = "AIGuardPostAndCommandPlacer Implementation is null at planet " + planet.Name;
                                    if ( MapgenLogger.IsActive )
                                        MapgenLogger.Log( aiDefensePlacerErrorText );
                                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( aiDefensePlacerErrorText );
                                    break;
                                }
                                debugStage = 415;
                                commandStationPoint = guardPlacer.Implementation.GetPointForCommandStation( Context, planet );
                            }
                            debugStage = 416;
                            //if in a tutorial and we're overriding the command station point, then... well, use that point!
                            if ( TutorialPlanetOrNull != null && TutorialPlanetOrNull.CommandStationPoint != ArcenPoint.ZeroZeroPoint )
                                commandStationPoint = Engine_AIW2.Instance.CombatCenter + TutorialPlanetOrNull.CommandStationPoint;

                            debugStage = 5;
                            for ( int fIdx = 0; fIdx < planet.Factions.Count; fIdx++ )
                            {
                                debugStage = 6;
                                PlanetFaction pFaction = planet.Factions[fIdx];
                                debugStage = 61;

                                if ( pFaction.Faction.FactionIndex < 0 )
                                    continue;
                                debugStage = 610100;
                                if ( pFaction.Faction.FactionIndex >= World_AIW2.Instance.Setup.FactionConfigurations.Count )
                                    continue;

                                debugStage = 610200;
                                //ConfigurationForFaction factionConfig = World_AIW2.Instance.SetupStoredLongTerm.FactionConfigurations[i];
                                ConfigurationForFaction factionConfig = World_AIW2.Instance.Setup.FactionConfigurations[pFaction.Faction.FactionIndex];

                                debugStage = 610300;
                                if ( TutorialPlanetOrNull != null && TutorialPlanetOrNull.Ships.Count > 0 )
                                {
                                    Tutorial.TutorialShipPlacement tutShip;
                                    debugStage = 610400;
                                    for ( int j = 0; j < TutorialPlanetOrNull.Ships.Count; j++ )
                                    {
                                        tutShip = TutorialPlanetOrNull.Ships[j];
                                        debugStage = 610500;
                                        //must be for the correct faction
                                        if ( tutShip.FactionName == factionConfig.LookupNameForTutorialsAndSuch )
                                        {
                                            ArcenPoint entityPoint = tutShip.PlacementPointOrZeroForRandom;
                                            debugStage = 610510;
                                            if ( entityPoint == ArcenPoint.ZeroZeroPoint )
                                                entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                                            else
                                                entityPoint += Engine_AIW2.Instance.CombatCenter;

                                            debugStage = 610520;
                                            GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, tutShip.TypeDataToSeed, tutShip.TypeDataToSeed.MarkFor( pFaction ),
                                                pFaction.FleetUsedAtPlanet, 0, entityPoint, Context, "Mapgen_Tutorial" );
                                        }
                                    }
                                }

                                debugStage = 610600;
                                switch ( pFaction.Faction.Type )
                                {
                                    case FactionType.AI:
                                        {
                                            debugStage = 7;
                                            switch ( planet.PopulationType )
                                            {
                                                case PlanetPopulationType.DarkZenith:
                                                    //the Dark Zenith Sidekick/empire does its unit population elsewhere
                                                    break;
                                                case PlanetPopulationType.HumanHomeworld:
                                                {
                                                        debugStage = 76;
                                                        if ( !World_AIW2.Instance.GetIsTutorial() ) //test ships only seed on the human homeworld now in non-tutorials.
                                                        {
                                                            IList<GameEntityTypeData> testShipDatas = GameEntityTypeDataTable.Instance.RowsByRollup[EntityRollupType.AITestShip];
                                                            for ( int j = 0; j < testShipDatas.Count; j++ )
                                                            {
                                                                GameEntityTypeData testShipData = testShipDatas[j];
                                                                ArcenPoint entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                                                                GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, testShipData, testShipData.MarkFor( pFaction ),
                                                                                                                 pFaction.FleetUsedAtPlanet, 0, entityPoint, Context, "Mapgen_TestShipsHumanHome" );
                                                            }
                                                            if ( MapgenLogger.IsActive )
                                                                MapgenLogger.Log( "FactionType.AI: HumanHomeworld: testShipDatas.Count( " + testShipDatas.Count + " )" );
                                                        }
                                                    }
                                                    break;
                                                case PlanetPopulationType.ArkEmpireHumanHomeworld:
                                                  {
                                                        debugStage = 765;
                                                        if ( planet.InitialOwningAIFactionIndex == -1 )
                                                            planet.InitialOwningAIFactionIndex = pFaction.Faction.FactionIndex;
                                                        if ( planet.InitialOwningAIFactionIndex != pFaction.Faction.FactionIndex )
                                                        {
                                                            break;
                                                        }
                                                        if ( !World_AIW2.Instance.GetIsTutorial() ) //test ships only seed on the human homeworld now in non-tutorials.
                                                        {
                                                            ArcenPoint entityPoint;
                                                            IList<GameEntityTypeData> testShipDatas = GameEntityTypeDataTable.Instance.RowsByRollup[EntityRollupType.AITestShip];
                                                            for ( int j = 0; j < testShipDatas.Count; j++ )
                                                            {
                                                                GameEntityTypeData testShipData = testShipDatas[j];
                                                                entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                                                                GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, testShipData, testShipData.MarkFor( pFaction ),
                                                                                                                 pFaction.FleetUsedAtPlanet, 0, entityPoint, Context, "Mapgen_TestShipsHumanHome" );
                                                            }
                                                            if ( MapgenLogger.IsActive )
                                                                MapgenLogger.Log( "FactionType.AI: HumanHomeworld: testShipDatas.Count( " + testShipDatas.Count + " )" );
                                                            //Also see the AI Command station and Warp Gate
                                                            commandStationPoint = planet.GuardPostAndCommandPlacer.Implementation.GetPointForCommandStation( Context, planet );
       
                                                            GameEntityTypeData commandStationType = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "AICommandStationOriginal" );

                                                            debugStage = 766;
                                                            GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, commandStationType, (byte) 1,
                                                                                                             pFaction.FleetUsedAtPlanet, 0, commandStationPoint, Context, "Mapgen_AIWorld" );
                                                            debugStage = 767;
                                                            GameEntityTypeData warpGateData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "WarpGate" );
                                                            int distanceToGate = (warpGateData.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius + commandStationType.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius) * 2;
                                                            entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, distanceToGate, distanceToGate );
                                                            debugStage = 768;
                                                            GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, warpGateData, planet.MarkLevelForAIOnly.Ordinal,
                                                                                                                 pFaction.FleetUsedAtPlanet, 0, entityPoint, Context, "Mapgen_AIWorld" );
                                                        }
                                                    }
                                                    break;
                                                case PlanetPopulationType.AIHomeworld:
                                                    {
                                                        debugStage = 77;
                                                        if ( planet.InitialOwningAIFactionIndex != pFaction.Faction.FactionIndex )
                                                        {
                                                            //if ( MapgenLogger.IsActive )
                                                            //    MapgenLogger.Log( "FactionType.AI: AIHomeworld: skipped because planet.InitialOwningAIFactionIndex " + planet.InitialOwningAIFactionIndex +
                                                            //        " != faction.Faction.FactionIndex " + faction.Faction.FactionIndex );
                                                            break;
                                                        }
                                                        stillNeedsToSeedAIHomeworldStuff = false;
                                                        debugStage = 771;
                                                        string overlordTypeToUse = "AIOverlordSeed_Difficulty" + pFaction.Faction.GetAISentinelsCoreData().SentinelInfo.AIDifficulty.Difficulty;
                                                        GameEntityTypeData commandStationType = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, overlordTypeToUse );
                                                        if ( commandStationType == null )
                                                        {
                                                            string errorText = "FactionType.AI: Could not find overlord with tag '" + overlordTypeToUse + "'";
                                                            if ( MapgenLogger.IsActive )
                                                                MapgenLogger.Log( errorText );
                                                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( errorText );
                                                            break;
                                                        }
                                                        debugStage = 7711;
                                                        GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, commandStationType, planet.MarkLevelForAIOnly.Ordinal,
                                                            pFaction.FleetUsedAtPlanet, 0, commandStationPoint, Context, "Mapgen_AIWorld" );
                                                        debugStage = 7713;
                                                        GameEntityTypeData warpGateData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "WarpGate" );
                                                        debugStage = 7714;
                                                        int distanceToGate = (warpGateData.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius + commandStationType.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius) * 2;
                                                        debugStage = 7715;
                                                        ArcenPoint entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, distanceToGate, distanceToGate );
                                                        debugStage = 7716;
                                                        GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, warpGateData, planet.MarkLevelForAIOnly.Ordinal,
                                                            pFaction.FleetUsedAtPlanet, 0, entityPoint, Context, "Mapgen_AIWorld" );
                                                        //GameEntityTypeData warheadSupressorData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "WarheadSuppressor" );
                                                        //for ( int j = 0; j < 4; j++ )
                                                        //{
                                                        //    entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                                                        //    GameEntity_Squad.CreateNew_ReturnNullIfMPClient( faction, warheadSupressorData, entityPoint, Context );
                                                        //}
                                                        if ( MapgenLogger.IsActive )
                                                            MapgenLogger.Log( "FactionType.AI: AIHomeworld: probably a success." );
                                                    }
                                                    break;
                                                case PlanetPopulationType.NonHomeworld:
                                                case PlanetPopulationType.AIBastionWorld:
                                                    {
                                                        debugStage = 78;
                                                        if ( planet.InitialOwningAIFactionIndex != pFaction.Faction.FactionIndex )
                                                        {
                                                            //if ( MapgenLogger.IsActive )
                                                            //    MapgenLogger.Log( "FactionType.AI: NonHomeworld: skipped because planet.InitialOwningAIFactionIndex " + planet.InitialOwningAIFactionIndex +
                                                            //        " != faction.Faction.FactionIndex " + faction.Faction.FactionIndex );
                                                            break;
                                                        }

                                                        string commandStationPickTag = "AICommandStationOriginal";
                                                        var sentinalData = pFaction.Faction.GetAISentinelsCoreData();
                                                        if (sentinalData != null)
                                                        {
                                                            var sentinalInfo = sentinalData.SentinelInfo;
                                                            var tag = sentinalInfo.AIType.CommandStationPickTag;
                                                            if (!string.IsNullOrEmpty(tag))
                                                                commandStationPickTag = tag;
                                                        }

                                                        debugStage = 781;
                                                        GameEntityTypeData commandStationType = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, commandStationPickTag );
                                                        debugStage = 782;
                                                        GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, commandStationType, planet.MarkLevelForAIOnly.Ordinal,
                                                            pFaction.FleetUsedAtPlanet, 0, commandStationPoint, Context, "Mapgen_AIWorld" );
                                                        debugStage = 783;
                                                        GameEntityTypeData warpGateData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "WarpGate" );
                                                        int distanceToGate = (warpGateData.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius + commandStationType.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius) * 2;
                                                        ArcenPoint entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, distanceToGate, distanceToGate );
                                                        debugStage = 784;
                                                        GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, warpGateData, planet.MarkLevelForAIOnly.Ordinal,
                                                            pFaction.FleetUsedAtPlanet, 0, entityPoint, Context, "Mapgen_AIWorld" );
                                                        //if ( planet.MarkLevel.Ordinal >= 4 )
                                                        //{
                                                        //GameEntityTypeData warheadSupressorData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "WarheadSuppressor" );
                                                        //    debugStage = 781;
                                                        //    for ( int j = 0; j < 2; j++ )
                                                        //    {
                                                        //        debugStage = 782;
                                                        //        entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                                                        //        debugStage = 783;
                                                        //        GameEntity_Squad.CreateNew_ReturnNullIfMPClient( faction, warheadSupressorData, entityPoint, Context );
                                                        //        debugStage = 784;
                                                        //    }
                                                        //}
                                                        if ( MapgenLogger.IsActive )
                                                            MapgenLogger.Log( "FactionType.AI: NonHomeworld: probably a success." );
                                                    }
                                                    break;
                                                default:
                                                    {
                                                        string errorText = "FactionType.AI: planet.PopulationType: " + planet.PopulationType + " skipped because it has no case ready.";
                                                        if ( MapgenLogger.IsActive )
                                                            MapgenLogger.Log( errorText );
                                                        Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( errorText );
                                                    }
                                                    break;
                                            }
                                            debugStage = 79;
                                        }
                                        break;
                                    //case FactionType.Player:
                                    default:
                                        {
                                            debugStage = 8;
                                            if ( factionConfig.StartingIndex != planet.Index )
                                            {
                                                //if ( MapgenLogger.IsActive )
                                                //    MapgenLogger.Log( "FactionType.Player: Whatever planet: skipped because planet.Index " + planet.Index +
                                                //        " != factionConfig.StartingIndex " + factionConfig.StartingIndex );
                                                break; //don't seed any player stuff on player homeworlds if this player doesn't start here.
                                            }
                                            debugStage = 81;

                                            if ( !pFaction.Faction.DeepInfo.SeedUnitsOnStartingPlanetDuringMapGen( 
                                                        planet, factionConfig, pFaction, ref commandStationPoint, ref stillNeedsToSeedHumanHomeworldStuff, 
                                                        TutorialPlanetOrNull, Context ) )
                                            {
                                                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( string.Format( "Error trying to seed units for {0} on its starting planet {1}", pFaction.Faction.GetDisplayNameWithoutPlayerNames(), planet.Name) ); 
                                                break;
                                            }
                                            
                                            // test ships we spawn on homeworld
                                            // skipped on tutorial
                                            if ( pFaction.Faction.Type == FactionType.Player && !World_AIW2.Instance.GetIsTutorial() )
                                            {
                                                IList<GameEntityTypeData> testShipDatas = GameEntityTypeDataTable.Instance.RowsByRollup[EntityRollupType.PlayerTestShip];
                                                for ( int j = 0; j < testShipDatas.Count; j++ )
                                                {
                                                    GameEntityTypeData testShipData = testShipDatas[j];
                                                    //int distanceToTestUnit = Context.RandomToUse.Next( innerSystemMinimumRadius, innerSystemMaximumRadius );
                                                    ArcenPoint otherPoint = Engine_AIW2.Instance.CombatCenter.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                                                    GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, testShipData, testShipData.MarkFor( pFaction ),
                                                        pFaction.FleetUsedAtPlanet, 0, otherPoint, Context, "Mapgen_TestShips" );
                                                }
                                            }

                                            if ( MapgenLogger.IsActive )
                                                MapgenLogger.Log( string.Format("{0} : planet {1} : probably a success.", pFaction.Faction.Type, planet.Name ) );
                                        }
                                        break;
                                    //default:
                                        //if ( MapgenLogger.IsActive )
                                        //    MapgenLogger.Log( faction.Faction.Type + " faction skipped 'normal seeding' because no case for it.  Probably not a bug." );
                                        //break;
                                }
                                debugStage = 62;
                            }
                        }
                        break;
                }
                debugStage = 9;

                if ( stillNeedsToSeedHumanHomeworldStuff )
                {
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "No human players found to seed homeworld structures for on planet " + planet.Name );
                    return;
                }
                if ( stillNeedsToSeedAIHomeworldStuff )
                {
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "No AIs found to seedhomeworld structures for on planet " + planet.Name );
                    return;
                }

                debugStage = 10;
                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( planet.Name + " SeedWormholesForPlanet" );
                UtilityMethods.SeedWormholesForPlanet( planet, wormholePlacer, Context );

                debugStage = 13;
                int numberOfMetalSpots = planet.Mapgen_WorkingAsteroidCount;
                if ( TutorialPlanetOrNull != null && TutorialPlanetOrNull.NumberOfMetalSpots > 0 )
                    numberOfMetalSpots = TutorialPlanetOrNull.NumberOfMetalSpots;
                if ( numberOfMetalSpots < 2 )
                    numberOfMetalSpots = 2;

                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( planet.Name + " numberOfMetalSpots: " + numberOfMetalSpots );
                for ( int i = 0; i < numberOfMetalSpots; i++ )
                {
                    if ( World_AIW2.Instance.PlayersAreInDistributedResourceGenerationMode )
                        UtilityMethods.SeedResourceSpot( planet, Context, wormholeRadius, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MineAndPowerplanet" ) );
                    else
                        UtilityMethods.SeedResourceSpot( planet, Context, wormholeRadius, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MetalHarvester" ) );
                }
                //UtilityMethods.SeedResourceSpot( planet, Context, wormholeRadius, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "EnergyGenerator" ) );
                //UtilityMethods.SeedResourceSpot( planet, Context, wormholeRadius, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ScienceGenerator" ) );

                //int numberOfPowerDistributionNodes = ( (FInt)4 * (FInt)planet.ResourceOutputs[ResourceType.Power] / (FInt)ExternalConstants.Instance.Balance_BasePowerScale ).IntValue;
                //for(int i = 0; i < numberOfPowerDistributionNodes;i++ )
                //    UtilityMethods.SeedResourceSpot( planet, Context, wormholeRadius, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "PowerDistributionNode" ) );
                //if ( planet.ResourceOutputs[ResourceType.Hacking] > 0 )
                //    UtilityMethods.SeedResourceSpot( planet, Context, wormholeRadius, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "HackingGenerator" ) );
                debugStage = 14;
                #region Defense ships
                switch ( planet.PopulationType )
                {
                    case PlanetPopulationType.AIHomeworld:
                    case PlanetPopulationType.AIBastionWorld:
                    case PlanetPopulationType.NonHomeworld:
                    case PlanetPopulationType.ArkEmpireHumanHomeworld: //note the suzerain does not want to seed enemy units
                        {
                            AIDefensePlacer placer = planet.GetCurrentDefensePlacer( Context );
                            if ( placer == null || placer.Implementation == null )
                                break;
                            placer.Implementation.DoInitialDefenseSeeding( Context, planet, TutorialPlanetOrNull, World_AIW2.Instance.GetFactionByIndex( planet.InitialOwningAIFactionIndex ) );
                        }
                        break;
                    case PlanetPopulationType.DarkZenith:
                        //could seed some ships here if desired
                        break;
                }
                #endregion
            }
            catch ( Exception e )
            {
                string errorText = "Exception thrown in SeedNormalEntities in stage " + debugStage + "\n" + e.ToString();
                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( errorText );
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( errorText );
            }
        }

        public void SeedFirstReinforcementsAtVeryEnd( Planet planet, ArcenHostOnlySimContext Context, MapTypeData mapType, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( planet.Name + "SeedFirstReinforcementsAtVeryEnd, planet.PopulationType " + planet.PopulationType + " planet.Factions.Count: " + planet.Factions.Count );

            switch ( planet.PopulationType )
            {
                case PlanetPopulationType.AIHomeworld:
                case PlanetPopulationType.AIBastionWorld:
                case PlanetPopulationType.NonHomeworld:
                case PlanetPopulationType.ArkEmpireHumanHomeworld:
                {
                        int debugStage = 1;
                        try
                        {
                            debugStage = 2000;
                            Faction faction1 = planet.GetControllingFaction();
                            debugStage = 3000;
                            if ( faction1 != null )
                                Helper_SeedFirstReinforcementsForFaction( planet, Context, mapType, TutorialPlanetOrNull, faction1 );
                            debugStage = 4000;
                            Faction faction2 = World_AIW2.Instance.GetFactionByIndex( planet.InitialOwningAIFactionIndex );
                            debugStage = 5000;
                            if ( faction2 != null && faction1 != faction2 )
                                Helper_SeedFirstReinforcementsForFaction( planet, Context, mapType, TutorialPlanetOrNull, faction2 );
                        }
                        catch ( Exception e )
                        {
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Exception thrown in SeedFirstReinforcementsAtVeryEnd in stage " + debugStage + "\n" + e.ToString() );
                        }
                    }
                    break;
            }
        }

        #region Helper_SeedFirstReinforcementsForFaction
        public static void Helper_SeedFirstReinforcementsForFaction( Planet planet, ArcenHostOnlySimContext Context, MapTypeData mapType, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull, Faction faction )
        {
            int debugStage = 1;
            try
            {
                AIDefensePlacer placer = planet.GetCurrentDefensePlacer( Context );
                debugStage = 1000;
                if ( placer == null || placer.Implementation == null )
                {
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null AIDefensePlacer on planet " + planet.Name + " (" + planet.Index + ") in Helper_SeedFirstReinforcementsForFaction. This should not be possible" );
                    return;
                }

                debugStage = 3000;
                if ( faction == null )
                {
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null faction passed in for planet " + planet.Name + " (" + planet.Index + ") in Helper_SeedFirstReinforcementsForFaction. This should not be possible" );
                    return;
                }
                debugStage = 4000;

                AISentinelsCoreData sentinelsExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
                debugStage = 5000;
                if ( sentinelsExternal == null && (faction.Type == FactionType.SpecialFaction || faction.Type == FactionType.NaturalObject) )
                {
                    faction = planet.GetFirstFactionOfType( FactionType.AI ).Faction;
                    sentinelsExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
                }
                debugStage = 6000;

                if ( sentinelsExternal == null )
                {
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null sentinelsExternal on faction of type " + faction.Type + " on planet " + planet.Name + " (" + planet.Index + ") in Helper_SeedFirstReinforcementsForFaction. This should not be possible" );
                    return;
                }
                debugStage = 7000;

                FInt turretAIToPurchaseCostBudget = AIUtilityMethods.GetAICostPurchaseCapForBudgetType( planet, faction, ReinforcementType.Turret, true, false, sentinelsExternal ) *
                        sentinelsExternal.AIType.MultiplierForGameStartingTurretsFromTotalBudget;
                FInt nonTurretDefenseAIToPurchaseCostBudget = AIUtilityMethods.GetAICostPurchaseCapForBudgetType( planet, faction, ReinforcementType.NonTurretDefense, true, false, sentinelsExternal ) *
                        sentinelsExternal.AIType.MultiplierForGameStartingNonTurretDefensesFromTotalBudget;
                FInt fleetShipAIToPurchaseCostBudget = AIUtilityMethods.GetAICostPurchaseCapForBudgetType( planet, faction, ReinforcementType.Strikecraft, true, false, sentinelsExternal ) *
                        sentinelsExternal.AIType.MultiplierForGameStartingStrikecraftFromTotalBudget;

                int addedStrength = 0;

                debugStage = 8000;
                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipAIInitialTurrets )
                    placer.Implementation.Reinforce( Context, planet, faction, turretAIToPurchaseCostBudget.GetNearestIntPreferringHigher(), ReinforcementType.Turret, true, true, ref addedStrength );
                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipAIInitialNonTurretDefenses )
                    placer.Implementation.Reinforce( Context, planet, faction, nonTurretDefenseAIToPurchaseCostBudget.GetNearestIntPreferringHigher(), ReinforcementType.NonTurretDefense, true, true, ref addedStrength );
                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipAIInitialStrikecraft )
                    placer.Implementation.Reinforce( Context, planet, faction, fleetShipAIToPurchaseCostBudget.GetNearestIntPreferringHigher(), ReinforcementType.Strikecraft, true, true, ref addedStrength );

                if ( addedStrength > 0 ) { } //we don't care
            }
            catch ( Exception e )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Exception thrown in Helper_SeedFirstReinforcementsForFaction in stage " + debugStage + "\n" + e.ToString() );
            }
        }
        #endregion

        #region EnsureFarFromWormholesIfPossible
        public static ArcenPoint EnsureFarFromWormholesIfPossible( int offsetDistanceMin, int offsetDistanceMax, int minDistance, int MaxLoopCount,
            ArcenPoint center, Planet planet, ArcenHostOnlySimContext Context )
        {
            ArcenPoint finalPoint;
            int loop = 0;
            bool areAnyTooClose = false;
            do
            {
                finalPoint = center.GetRandomPointWithinDistance( Context.RandomToUse, offsetDistanceMin, offsetDistanceMax );

                areAnyTooClose = false;
                foreach ( GameEntity_Other wormhole in planet.Others() )
                {
                    if ( wormhole.TypeData.OtherSpecialType != OtherSpecialEntityType.Wormhole )
                        continue;

                    if ( wormhole.WorldLocation.GetDistanceTo( finalPoint, true ) < minDistance )
                    {
                        areAnyTooClose = true;
                        break;
                    }
                }

                if ( !areAnyTooClose )
                    return finalPoint;
            }
            while ( loop++ < MaxLoopCount );

            return ArcenPoint.OutOfRange;
        }
        #endregion

        public static void Helper_SeedStartingBaseUnit( ArcenHostOnlySimContext Context, ArcenPoint commandStationPoint, PlanetFaction faction, FInt placementOffsetScale, string tag, int xOffset, int yOffset )
        {
            GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, tag );
            if ( typeData == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Helper_SeedStartingBaseUnit: Error: No unit available with tag " + tag, Verbosity.DoNotShow );
                return;
            }
            ArcenPoint point = commandStationPoint;
            point.X += (xOffset * placementOffsetScale).IntValue;
            point.Y += (yOffset * placementOffsetScale).IntValue;

            byte markLevel = faction.Faction.Type == FactionType.AI ? faction.Planet.MarkLevelForAIOnly.Ordinal : typeData.MarkFor( faction );

            GameEntity_Squad.CreateNew_ReturnNullIfMPClient( faction, typeData, markLevel,
                faction.FleetUsedAtPlanet, 0, point, Context, "Mapgen_SeedStartingBaseUnit" );
        }

        public static void Helper_SeedStartingBaseUnit( ArcenHostOnlySimContext Context, ArcenPoint commandStationPoint, PlanetFaction faction, Fleet FleetToSeedFor, FInt placementOffsetScale, string tag, int xOffset, int yOffset )
        {
            GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, tag );
            if ( typeData == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Helper_SeedStartingBaseUnit: Error: No unit available with tag " + tag, Verbosity.DoNotShow );
                return;
            }
            Helper_SeedStartingBaseUnit( Context, commandStationPoint, faction, FleetToSeedFor, placementOffsetScale, typeData, xOffset, yOffset );
        }

        public static void Helper_SeedStartingBaseUnit( ArcenHostOnlySimContext Context, ArcenPoint commandStationPoint, PlanetFaction faction, Fleet FleetToSeedFor, FInt placementOffsetScale, GameEntityTypeData typeData, int xOffset, int yOffset )
        {
            ArcenPoint point = commandStationPoint;
            point.X += (xOffset * placementOffsetScale).IntValue;
            point.Y += (yOffset * placementOffsetScale).IntValue;

            byte markLevel = faction.Faction.Type == FactionType.AI ? faction.Planet.MarkLevelForAIOnly.Ordinal : typeData.MarkFor( faction );

            GameEntity_Squad.CreateNew_ReturnNullIfMPClient( faction, typeData, markLevel,
                FleetToSeedFor, 0, point, Context, "Mapgen_SeedStartingBaseUnit" );
        }

        public void SeedSpecialEntities_EarlyPreMajorFactionClaims( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData MapData )
        {
        }

        public void SeedSpecialEntities_MiddlePostMajorFactionClaims( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData MapData )
        {
        }

        private Dictionary<Planet, bool> planetsRandomizedAtLeastOnce = Dictionary<Planet, bool>.Create_WillNeverBeGCed( 300, "StandardMapPopulator-planetsRandomizedAtLeastOnce" );

        public void SeedSpecialEntities_LateAfterAllFactionSeeding( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData MapData )
        {
            //there are cases where we just don't have any factions yet, and that's ok!  Don't seed anything here, then.
            if ( World_AIW2.Instance.Factions.Count == 0 )
                return;
            //if not seeding the details yet, then... skip all this!
            if ( !World_AIW2.Instance.Setup.ShouldSeedDetailsYet )
                return;

            Tutorial tutorialData = World_AIW2.Instance.TutorialOrNull;

            if ( tutorialData != null && tutorialData.SkipAllGalaxyWideCapturablesAndObstacles )
                return;

            MapgenLogger.WriteHeaderStringIfActive( "SeedSpecialEntities" );

            ThrowawayListCanMemLeak<Planet> baseListPlanetsToSeedOn = new ThrowawayListCanMemLeak<Planet>( 500 );
            int bonusStructuresForBigMaps = 0;
            foreach ( Planet planet in galaxy.Planets( false ) )
            {
                if ( planet.PopulationType == PlanetPopulationType.HumanHomeworld )
                    continue;
                baseListPlanetsToSeedOn.Add( planet );
            }
            if ( galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() < 140 )
                bonusStructuresForBigMaps = 0;
            else if ( galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() <= 170 )
                bonusStructuresForBigMaps = 1;
            else if ( galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() <= 200 )
                bonusStructuresForBigMaps = 2;
            else
                bonusStructuresForBigMaps = 3;

            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "bonusStructuresForBigMaps " + bonusStructuresForBigMaps + " planetCount: " + galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() );

            int numPlanets = galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise();
            int targetToSeed = 0;
            int seedThisMany = 0;
            int numFails = 0;
            float seedMult = 1.0f;
            
            //we place different things based on the AI 
            AIDifficulty highestDifficulty = FactionUtilityMethods.Instance.GetHighestAIDifficulty_AsDifficulty();

            int closestPlayerAndAIHomeworld = GetClosestDistanceFromPlayerToAIHomeworld( galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs() );

            if ( MapgenLogger.IsActive )
            {
                MapgenLogger.Log( "highestDifficulty " + highestDifficulty.InternalName + " min hops between any AI and player homeworld: " + closestPlayerAndAIHomeworld );
            }


            int extraDistanceForAdajentSeededItems = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "ExtraDistanceForAdajentSeededItems" );
            int extraDistanceForMiddleDistanceItems = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "ExtraDistanceForMiddleDistanceItems" );
            int reducedDistanceRestrictionForAnyItems = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "ReducedDistanceRestrictionForAnyItems" );

            #region The Big Fleet Capturables For Players
            IList<Planet> planetsSeeded;

            #region Regular/Strike Fleets

            if ( tutorialData == null || !tutorialData.SkipMobileStrikeCombatFleetFlagships )
            {
                string mobileStrikeFleetTag = "RegularStrike";
                int countOfPlayerFactionsNeedingMobileStrikeFleets = 0;
                foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                {
                    PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                    if ( playerType == null )
                        continue; //only seed if they say to
                    if ( playerType.MapGen_ShouldMobileStrikeFleetsSeedForMe )
                        countOfPlayerFactionsNeedingMobileStrikeFleets++;
                    if ( playerType.MapGen_RequireMobileStrikeFleetTag != null && playerType.MapGen_RequireMobileStrikeFleetTag.Length > 0 )
                        mobileStrikeFleetTag = playerType.MapGen_RequireMobileStrikeFleetTag;
                }

                numFails = 0;
                seedMult = AIWar2GalaxySettingTable.GetFauxFloatValueFromSettingByName_DuringGame( "RegularFleetsToSeed", 1.0f );
                if ( seedMult < 0 ) seedMult = 1.0f;
                
                if (seedMult > 0)
                {
                    //one per human faction that is supposed to have these
                    foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                    {
                        PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                        if ( playerType == null )
                            continue; //only seed if they say to
                        if ( !playerType.MapGen_ShouldMobileStrikeFleetsSeedForMe )
                            continue;

                        //put one RegularStrike on an adjacent planet to a player homeworld if possible
                        seedThisMany = 1;
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, mobileStrikeFleetTag, SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 1, 1 + extraDistanceForAdajentSeededItems, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                            player, (Int16)(1 + extraDistanceForAdajentSeededItems) );
                        seedThisMany -= planetsSeeded.Count;
                        if ( seedThisMany < 0 ) seedThisMany = 0;
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "Regular/Strike Fleets - on an adjacent planet to player homeworld " + player.GetDisplayName(), seedThisMany );

                        //put another RegularStrike two hops out from the player homeworld if possible
                        seedThisMany++;
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, mobileStrikeFleetTag, SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 2 - reducedDistanceRestrictionForAnyItems, 2 + extraDistanceForAdajentSeededItems, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                            player, (Int16)(2 + extraDistanceForAdajentSeededItems) );
                        seedThisMany -= planetsSeeded.Count;
                        if ( seedThisMany < 0 ) seedThisMany = 0;
                        if ( seedThisMany > 0 ) numFails += seedThisMany;
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "Regular/Strike Fleets - two hops out from " + player.GetDisplayName(), seedThisMany );
                    }

                    if ( countOfPlayerFactionsNeedingMobileStrikeFleets > 0 )
                    {
                        targetToSeed = (int)Math.Ceiling( (Math.Max( 8, numPlanets / 30 ) * seedMult) / 2f );
                        
                        //an extra two for each human empire beyond the first
                        targetToSeed += ((countOfPlayerFactionsNeedingMobileStrikeFleets - 1) * 2);

                        //then put half the rest of the RegularStrike in the middle distance
                        seedThisMany = targetToSeed + numFails;
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, mobileStrikeFleetTag, SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 3 - reducedDistanceRestrictionForAnyItems, 8 + extraDistanceForMiddleDistanceItems, 2, 999, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
                        seedThisMany -= planetsSeeded.Count;
                        if ( seedThisMany < 0 ) seedThisMany = 0;
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "Regular/Strike Fleets - middle distance", seedThisMany );

                        //then put half the rest of the RegularStrike kind of wherever
                        seedThisMany += targetToSeed;
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, FactionType.NaturalObject, SpecialEntityType.None, mobileStrikeFleetTag, SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 3 - reducedDistanceRestrictionForAnyItems, 2, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.DoNoExpansion, null, -1 );
                        seedThisMany -= planetsSeeded.Count;
                        if ( seedThisMany < 0 ) seedThisMany = 0;

                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "Regular/Strike Fleets - wherever", -1 );
                    }
                }
            }
            #endregion

            targetToSeed = 0;
            seedThisMany = 0;
            numFails = 0;
            seedMult = 1.0f;

            #region TurretSchematicServer
            if ( tutorialData == null || !tutorialData.SkipTurretSchematicServers )
            {
                seedMult = AIWar2GalaxySettingTable.GetFauxFloatValueFromSettingByName_DuringGame( "TurretSchematicServersToSeed", 1.0f );
                
                if (seedMult > 0)
                {
                    int countOfPlayerFactionsNeedingTSSes = 0;
                    foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                    {
                        PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                        if ( playerType == null )
                            continue; //only seed if they say to
                        if ( playerType.MapGen_ShouldTSSesSeedForMe )
                            countOfPlayerFactionsNeedingTSSes++;
                    }
                    
                    if ( countOfPlayerFactionsNeedingTSSes > 0 )
                    {
                        targetToSeed = (int)Math.Ceiling( (Math.Max( 6, galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() / 40 ) * seedMult) / 2f );
                        
                        string TSStag = "TurretSchematicServer";
                        if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "FewerARSOptions" ) )
                            TSStag = "TurretSchematicServerExpert";
                        if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "MoreARSOptions" ) )
                            TSStag = "TurretSchematicServerEasy";
                    
                        //one per human empire homeworld
                        foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                        {
                            PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                            if ( playerType == null )
                                continue; //only seed if they say to
                            if ( !playerType.MapGen_ShouldTSSesSeedForMe )
                                continue;
                            seedThisMany = 1;
                            planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, TSStag, SeedingType.CapturableWeightsAndMax,
                                seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 1, 1 + extraDistanceForAdajentSeededItems, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                                player, (Int16)(1 + extraDistanceForAdajentSeededItems) );
                            seedThisMany -= planetsSeeded.Count;
                            if ( seedThisMany < 0 ) seedThisMany = 0;
                            if ( seedThisMany > 0 ) numFails += seedThisMany;
                            Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "TurretSchematicServer - within a few hops from player homeworld " + player.GetDisplayName(), seedThisMany );
                        }

                        //put one more TurretSchematicServer with 3-99 hops of a player homeworld if possible per each player empire
                        seedThisMany = countOfPlayerFactionsNeedingTSSes;
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, TSStag, SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 3 - reducedDistanceRestrictionForAnyItems, 99, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
                        seedThisMany -= planetsSeeded.Count;
                        if ( seedThisMany < 0 ) seedThisMany = 0;
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "TurretSchematicServer - within 3-6 hops of player homeworlds", seedThisMany );

                        //then put half the rest of the TurretSchematicServers in the middle distance
                        seedThisMany += targetToSeed + numFails;
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, TSStag, SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 5 - reducedDistanceRestrictionForAnyItems, 99, 2, 999, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
                        seedThisMany -= planetsSeeded.Count;
                        if ( seedThisMany < 0 ) seedThisMany = 0;
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "TurretSchematicServer - middle distance", seedThisMany );
                    }
                }
            }
            #endregion

            targetToSeed = 0;
            seedThisMany = 0;
            numFails = 0;
            seedMult = 1.0f;
            
            #region OtherDefensiveSchematicServer
            if ( tutorialData == null || !tutorialData.SkipTurretSchematicServers )
            {
                seedMult = AIWar2GalaxySettingTable.GetFauxFloatValueFromSettingByName_DuringGame( "OtherDefensiveSchematicServersToSeed", 1.0f );
                
                if (seedMult > 0)
                {
                    int countOfPlayerFactionsNeedingODSSes = 0;
                    foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                    {
                        PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                        if ( playerType == null )
                            continue; //only seed if they say to
                        if ( playerType.MapGen_ShouldODSSesSeedForMe )
                            countOfPlayerFactionsNeedingODSSes++;
                    }

                    if ( countOfPlayerFactionsNeedingODSSes > 0 )
                    {
                        numFails = 0;
                        //one per human empire homeworld
                        foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                        {
                            PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                            if ( playerType == null )
                                continue; //only seed if they say to
                            if ( !playerType.MapGen_ShouldODSSesSeedForMe )
                                continue;

                            seedThisMany = 1;
                            planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "OtherDefensiveSchematicServer", SeedingType.CapturableWeightsAndMax,
                                seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 2, 99, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                                player, 1 );
                            seedThisMany -= planetsSeeded.Count;
                            if ( seedThisMany < 0 ) seedThisMany = 0;
                            if ( seedThisMany > 0 ) numFails += seedThisMany;
                            Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "OtherDefensiveSchematicServer - within a few hops from player homeworld " + player.GetDisplayName(), seedThisMany );
                        }
                        
                        targetToSeed = (int)Math.Ceiling( (Math.Max( 3, galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() / 60 ) * seedMult) / 2f );
                        //an extra one for each human empire beyond the first
                        seedThisMany += (countOfPlayerFactionsNeedingODSSes - 1);

                        //then put half the rest of the OtherDefensiveSchematicServers in the middle distance
                        seedThisMany += targetToSeed + numFails;
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "OtherDefensiveSchematicServer", SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 3 - reducedDistanceRestrictionForAnyItems, 11, 2, 999, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
                        seedThisMany -= planetsSeeded.Count;
                        if ( seedThisMany < 0 ) seedThisMany = 0;
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "OtherDefensiveSchematicServer - middle distance", seedThisMany );
                    }
                }
            }
            #endregion

            targetToSeed = 0;
            seedThisMany = 0;
            numFails = 0;
            seedMult = 1.0f;
            
            #region ARS
            
            seedMult = AIWar2GalaxySettingTable.GetFauxFloatValueFromSettingByName_DuringGame( "AdvancedResearchStationsToSeed", 1.0f );
            if (seedMult > 0)
            {
                int countOfPlayerFactionsNeedingARSes = 0;
                foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                {
                    PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                    if ( playerType == null || !playerType.MapGen_ShouldARSesSeedForMe )
                        continue; //only seed ARS if they say to
                    countOfPlayerFactionsNeedingARSes++;
                }

                if ( ( tutorialData == null || !tutorialData.SkipAdvancedResearchStations ) && countOfPlayerFactionsNeedingARSes > 0 )
                {
                    //one per human empire homeworld
                    if ( MapgenLogger.IsActive)
                        MapgenLogger.Log( "Goal: ARS try seed one next to each of " + countOfPlayerFactionsNeedingARSes + " relevant players." );
                    
                    string ARStag = "ARS";
                    if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "FewerARSOptions" ) )
                        ARStag = "ARSExpert";
                    if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "MoreARSOptions" ) )
                        ARStag = "ARSEasy";
                    
                    foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                    {
                        PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                        if ( playerType == null || !playerType.MapGen_ShouldARSesSeedForMe )
                            continue; //only seed ARS if they say to

                        seedThisMany = 1;

                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, ARStag, SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 1, 1 + extraDistanceForAdajentSeededItems, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                            player, (Int16)(1 + extraDistanceForAdajentSeededItems) );
                        seedThisMany -= planetsSeeded.Count;
                        if ( seedThisMany < 0 ) seedThisMany = 0;
                        if ( seedThisMany > 0 ) numFails += seedThisMany;
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "ARS - on an adjacent planet to player homeworld " + player.GetDisplayName(), seedThisMany );
                    }

                    //an extra one per each human player empire, anywhere
                    seedThisMany = countOfPlayerFactionsNeedingARSes;
                    if ( MapgenLogger.IsActive )
                        MapgenLogger.Log( "Goal: ARS try seed one in general plus one for each of " + countOfPlayerFactionsNeedingARSes + " relevant players." );
                    planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, ARStag, SeedingType.CapturableWeightsAndMax,
                        seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 2 - reducedDistanceRestrictionForAnyItems, 99, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
                    seedThisMany -= planetsSeeded.Count;
                    if ( seedThisMany < 0 ) seedThisMany = 0;
                    Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "ARS - within a few hops from player homeworld", seedThisMany );

                    targetToSeed = (int)Math.Ceiling( (Math.Max( 4, numPlanets / 50 ) * seedMult) / 2f );

                    //then put half the rest of the ARS in the middle distance
                    seedThisMany += targetToSeed + numFails;
                    if ( highestDifficulty.Difficulty < 5 )
                        seedThisMany += 1; //a few more for easy games
                    //an extra one for each relevant player beyond the first
                    seedThisMany += (countOfPlayerFactionsNeedingARSes - 1);

                    if ( MapgenLogger.IsActive )
                        MapgenLogger.Log( "Goal: ARS try seed " + targetToSeed + " from targetToSeed, " + numFails + " from earlier fails, and " + seedThisMany + " in all from all factors." );

                    planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, ARStag, SeedingType.CapturableWeightsAndMax,
                        seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 3 - reducedDistanceRestrictionForAnyItems, 8 + extraDistanceForMiddleDistanceItems, 2, 999, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
                    seedThisMany -= planetsSeeded.Count;
                    if ( seedThisMany < 0 ) seedThisMany = 0;
                    Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "ARS - middle distance", seedThisMany );

                    //then put half the rest of the ARS kind of wherever
                    seedThisMany += targetToSeed;
                    planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, FactionType.AI, SpecialEntityType.None, ARStag, SeedingType.CapturableWeightsAndMax,
                        seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 3 - reducedDistanceRestrictionForAnyItems, 2, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.DoNoExpansion, null, -1 );
                    seedThisMany -= planetsSeeded.Count;
                    if ( seedThisMany < 0 ) seedThisMany = 0;
                    Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "ARS - wherever", seedThisMany );
                }
            }
            #endregion

            targetToSeed = 0;
            seedThisMany = 0;
            numFails = 0;
            seedMult = 1.0f;
            
            #region Dire CPAs
            if ( tutorialData == null &&
                 AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "DireCPA" ) )
            {
                Mapgen_SeedSpecialEntities( Context, galaxy, FactionType.AI, SpecialEntityType.None, "CPABunker", SeedingType.HardcodedCount, 1,
                    MapGenCountPerPlanet.One, MapGenSeedStyle.SmallBad, 3 - reducedDistanceRestrictionForAnyItems, 2, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal, null, -1 );

            }
            #endregion
            
            #region Early Officers
            
            seedMult = AIWar2GalaxySettingTable.GetFauxFloatValueFromSettingByName_DuringGame( "OfficerFleetsToSeed", 1.0f );
            if (seedMult > 0)
            {
                if ( tutorialData == null )
                {
                    string earlyOfficerTag = "AnyEarlyOfficer";
                    int countOfPlayerFactionsNeedingEarlyOfficers = 0;
                    foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                    {
                        PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                        if ( playerType == null )
                            continue; //only seed if they say to
                        if ( playerType.MapGen_ShouldEarlyOfficersSeedForMe )
                            countOfPlayerFactionsNeedingEarlyOfficers++;
                        if ( playerType.MapGen_RequireEarlyOfficerTag != null && playerType.MapGen_RequireEarlyOfficerTag.Length > 0 )
                            earlyOfficerTag = playerType.MapGen_RequireEarlyOfficerTag;
                    }

                    if (AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame("ExpertOfficers"))
                        earlyOfficerTag += "_Expert";

                    if ( countOfPlayerFactionsNeedingEarlyOfficers > 0 )
                    {
                        if ( highestDifficulty.Difficulty <= 6 )
                        {
                            //put one EarlyOfficers with 1-4 hops of a player homeworld
                            seedThisMany = 1;
                            planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, earlyOfficerTag, SeedingType.CapturableWeightsAndMax,
                                seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 1, 4 + extraDistanceForMiddleDistanceItems, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
                            seedThisMany -= planetsSeeded.Count;
                            if ( seedThisMany < 0 ) seedThisMany = 0;
                            Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "Early Officers - within 1-4 hops of player homeworlds", seedThisMany );
                        }
                        else
                        {
                            //put one EarlyOfficers with 3-8 hops of a player homeworld
                            seedThisMany = 1;
                            planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, earlyOfficerTag, SeedingType.CapturableWeightsAndMax,
                                seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 3, 8 + extraDistanceForMiddleDistanceItems, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
                            seedThisMany -= planetsSeeded.Count;
                            if ( seedThisMany < 0 ) seedThisMany = 0;
                            Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "Early Officers - within 6-16 hops of player homeworlds", seedThisMany );
                        }
                    }
                }
            }
            #endregion

            targetToSeed = 0;
            seedThisMany = 0;
            numFails = 0;
            seedMult = 1.0f;
            
            #region TechVault
            if ( tutorialData == null || !tutorialData.SkipTechVaults )
            {
                seedThisMany = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "BaseTechVaultsToSeed" );

                if ( seedThisMany > 0 )
                {
                    int countOfPlayerFactionsNeedingTechVaults = 0;
                    foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                    {
                        PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                        if ( playerType == null )
                            continue; //only seed if they say to
                        if ( playerType.MapGen_ShouldTechVaultsSeedForMe )
                            countOfPlayerFactionsNeedingTechVaults++;
                    }

                    if ( countOfPlayerFactionsNeedingTechVaults > 0 )
                    {
                        if ( countOfPlayerFactionsNeedingTechVaults > 1 )
                        {
                             //one more for each extra human empire beyond the first?
                            if (AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "SeedExtraTechVaultsInMultiplayer" ))
                                seedThisMany += (countOfPlayerFactionsNeedingTechVaults - 1);
                        }
                        
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "TechVault", SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 3 - reducedDistanceRestrictionForAnyItems, 999, 2, 999, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
                        seedThisMany -= planetsSeeded.Count;
                        if ( seedThisMany < 0 ) seedThisMany = 0;
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "TechVault - middle to really far distance", seedThisMany );
                    }
                }
            }
            #endregion

            #region Fortresses and Ultimate Fortress
            if ( tutorialData == null )
            {
                seedThisMany = 3;

                planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "AIFortress", SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigBad, 3 - reducedDistanceRestrictionForAnyItems, 999, 2, 999, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
                Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "AI Fortress - middle to really far distance", seedThisMany );
                seedThisMany = 1;

                if ( World_AIW2.Instance.Setup.GetBoolBySetting( "UltimateFortress" ) )
                {
                    planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "AIUltimateFortress", SeedingType.CapturableWeightsAndMax,
                                seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigBad, 3 - reducedDistanceRestrictionForAnyItems, 999, 2, 999, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
                    Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "AI Ultimate Fortress - middle to really far distance", seedThisMany );
                }
            }
            #endregion

            targetToSeed = 0;
            seedThisMany = 0;
            numFails = 0;
            seedMult = 1.0f;
            
            #region FRS
            // reuse skip ars for this
            if ( tutorialData == null || !tutorialData.SkipAdvancedResearchStations ) 
            {
                seedMult = AIWar2GalaxySettingTable.GetFauxFloatValueFromSettingByName_DuringGame( "FleetResearchStationsToSeed", 1.0f );
                
                if (seedMult > 0)
                {
                    int countOfPlayerFactionsNeedingFRSes = 0;
                    foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                    {
                        PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                        if ( playerType == null )
                            continue; //only seed if they say to
                        if ( playerType.MapGen_ShouldFRSesSeedForMe )
                            countOfPlayerFactionsNeedingFRSes++;
                    }

                    if ( countOfPlayerFactionsNeedingFRSes > 0 )
                    {
                        //don't seed any THAT nearby, these need to be more rare.
                        
                        targetToSeed = (int)Math.Ceiling( (Math.Max( 4, galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() / 50 ) * seedMult) / 2f );

                        //put half the rest of the FRS in the middle distance
                        seedThisMany += targetToSeed;
                        if ( highestDifficulty.Difficulty < 5 )
                            seedThisMany += 1; //a few more for easy games
                                               //an extra one for each human empire beyond the first
                        seedThisMany += (countOfPlayerFactionsNeedingFRSes - 1);

                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "FRS", SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 5 - reducedDistanceRestrictionForAnyItems, 10 + extraDistanceForMiddleDistanceItems, 2, 999, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
                        seedThisMany -= planetsSeeded.Count;
                        if ( seedThisMany < 0 ) seedThisMany = 0;
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "FRS - middle distance", seedThisMany );

                        //then put half the rest of the FRS kind of wherever
                        seedThisMany += targetToSeed;
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, FactionType.AI, SpecialEntityType.None, "FRS", SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 5 - reducedDistanceRestrictionForAnyItems, 2, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.DoNoExpansion, null, -1 );
                        seedThisMany -= planetsSeeded.Count;
                        if ( seedThisMany < 0 ) seedThisMany = 0;
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "FRS - wherever", seedThisMany );
                    }
                }
            }
            #endregion FRS

            targetToSeed = 0;
            seedThisMany = 0;
            numFails = 0;
            seedMult = 1.0f;
            
            #region CruiserConstructionFacility
            // reusing skip ars for this
            if ( !(tutorialData?.SkipAdvancedResearchStations == true) && 
                FleetDesignTemplateTable.Instance.CountOfCruisers > 0 )
            {
                // reusing ars multiplier for this
                seedMult = AIWar2GalaxySettingTable.GetFauxFloatValueFromSettingByName_DuringGame( "AdvancedResearchStationsToSeed", 1.0f );
                
                if (seedMult > 0)
                {
                    int countOfPlayerFactionsNeedingCruisers = 0;
                    foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                    {
                        if ( player.PlayerTypeDataOrNull_ModeratelyExpensive?.MapGen_ShouldCruisersSeedForMe == true)
                            countOfPlayerFactionsNeedingCruisers++;
                    }

                    if ( countOfPlayerFactionsNeedingCruisers > 0 )
                    {
                        //only three per galaxy, normally
                        seedThisMany = (int)Math.Ceiling(3 * seedMult);
                        
                        //an extra one for each two human empires beyond the first
                        seedThisMany += ((countOfPlayerFactionsNeedingCruisers - 1) / 2);

                        int max = FleetDesignTemplateTable.Instance.CountOfCruisers;
                        if (max > 2)
                            max -= 2;
                        
                        if (seedThisMany > max)
                            seedThisMany = max;
                        
                        //then put them CruiserConstructionFacility kind of wherever
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, FactionType.AI, SpecialEntityType.None, "CruiserConstructionFacility", SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 4 - reducedDistanceRestrictionForAnyItems, 2, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.DoNoExpansion, null, -1 );
                        seedThisMany -= planetsSeeded.Count;
                        if ( seedThisMany < 0 ) seedThisMany = 0;
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "CruiserConstructionFacility - wherever", seedThisMany );
                    }
                }
            }
            #endregion

            targetToSeed = 0;
            seedThisMany = 0;
            numFails = 0;
            seedMult = 1.0f;
            
            #region DestroyerConstructionFacility
            // reusing skip ars for this
            if ( !(tutorialData?.SkipAdvancedResearchStations == true) && 
                FleetDesignTemplateTable.Instance.CountOfDestroyers > 0 )
            {
                // reusing ars multiplier for this
                seedMult = AIWar2GalaxySettingTable.GetFauxFloatValueFromSettingByName_DuringGame( "AdvancedResearchStationsToSeed", 1.0f );
                
                if (seedMult > 0)
                {   
                    int countOfPlayerFactionsNeedingDestroyers = 0;
                    foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                    {
                        if ( player.PlayerTypeDataOrNull_ModeratelyExpensive?.MapGen_ShouldDestroyersSeedForMe == true)
                            countOfPlayerFactionsNeedingDestroyers++;
                    }

                    if ( countOfPlayerFactionsNeedingDestroyers > 0 )
                    {
                        //only three per galaxy, normally
                        seedThisMany = (int)Math.Ceiling(3 * seedMult);
                        
                        //an extra one for each two human empires beyond the first
                        seedThisMany += ((countOfPlayerFactionsNeedingDestroyers - 1) / 2);

                        int max = FleetDesignTemplateTable.Instance.CountOfCruisers;
                        if (max > 2)
                            max -= 2;
                        
                        if (seedThisMany > max)
                            seedThisMany = max;
                        
                        //then put them DestroyerConstructionFacility kind of wherever
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, FactionType.AI, SpecialEntityType.None, "DestroyerConstructionFacility", SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 4 - reducedDistanceRestrictionForAnyItems, 2, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.DoNoExpansion, null, -1 );
                        seedThisMany -= planetsSeeded.Count;
                        if ( seedThisMany < 0 ) seedThisMany = 0;
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "DestroyerConstructionFacility - wherever", seedThisMany );
                    }
                }
            }
            #endregion

            targetToSeed = 0;
            seedThisMany = 0;
            numFails = 0;
            seedMult = 1.0f;
            
            #region Support Fleets
            if ( !(tutorialData?.SkipMobileSupportFleetFlagships == true) )
            {
                seedMult = AIWar2GalaxySettingTable.GetFauxFloatValueFromSettingByName_DuringGame( "MobileSupportFleetsToSeed", 1.0f );
                if (seedMult > 0)
                {
                    string supportFleetTag = "SupportFleet";
                    int countOfPlayerFactionsNeedingSupportFleets = 0;
                    foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                    {
                        PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                        if ( playerType == null )
                            continue; //only seed if they say to
                        if ( playerType.MapGen_ShouldSupportFleetsSeedForMe )
                            countOfPlayerFactionsNeedingSupportFleets++;
                        if ( playerType.MapGen_RequireSupportFleetTag != null && playerType.MapGen_RequireSupportFleetTag.Length > 0 )
                            supportFleetTag = playerType.MapGen_RequireSupportFleetTag;
                    }

                    if ( countOfPlayerFactionsNeedingSupportFleets > 0 )
                    {
                        numFails = 0;
                        //put another SupportFleet two hops out from each player homeworld if possible for those who need it
                        foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                        {
                            PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                            if ( playerType == null )
                                continue; //only seed if they say to
                            if ( !playerType.MapGen_ShouldSupportFleetsSeedForMe )
                                continue;

                            seedThisMany = 1;
                            planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, supportFleetTag, SeedingType.CapturableWeightsAndMax,
                                seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 2 - reducedDistanceRestrictionForAnyItems, 2 + extraDistanceForAdajentSeededItems, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                                player, (Int16)(3 + extraDistanceForAdajentSeededItems) );
                            seedThisMany -= planetsSeeded.Count;
                            if ( seedThisMany < 0 ) seedThisMany = 0;
                            if ( seedThisMany > 0 ) numFails += seedThisMany;
                            Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "Support Fleets - two hops out from " + player.GetDisplayName(), seedThisMany );
                        }

                        targetToSeed = (int)Math.Ceiling( Math.Max( 2, galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() / 150 ) * seedMult );
                        //an extra one for each human empire beyond the first
                        targetToSeed += (countOfPlayerFactionsNeedingSupportFleets - 1);

                        //then put the rest of the SupportFleet kind of wherever
                        seedThisMany = targetToSeed + numFails;
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, FactionType.NaturalObject, SpecialEntityType.None, supportFleetTag, SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 3 - reducedDistanceRestrictionForAnyItems, 2, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.DoNoExpansion, null, -1 );
                        seedThisMany -= planetsSeeded.Count;
                        if ( seedThisMany < 0 ) seedThisMany = 0;
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "Support Fleets - wherever", seedThisMany );
                    }
                }
            }
            #endregion

            targetToSeed = 0;
            seedThisMany = 0;

            #region Citadels
            if ( !(tutorialData?.SkipBattlestationCitadels == true) )
            {
                seedMult = AIWar2GalaxySettingTable.GetFauxFloatValueFromSettingByName_DuringGame( "BattlestationCitadelsToSeed", 1.0f );
                if (seedMult > 0)
                {
                    string citadelTag = "SeededCitadel";
                    int countOfPlayerFactionsNeedingCitadels = 0;
                    foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                    {
                        PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                        if ( playerType == null )
                            continue; //only seed if they say to
                        if ( playerType.MapGen_ShouldCitadelsSeedForMe )
                            countOfPlayerFactionsNeedingCitadels++;
                        if ( playerType.MapGen_RequireCitadelTag != null && playerType.MapGen_RequireCitadelTag.Length > 0 )
                            citadelTag = playerType.MapGen_RequireCitadelTag;
                    }

                    if ( countOfPlayerFactionsNeedingCitadels > 0 )
                    {
                        targetToSeed = (int)Math.Ceiling( (Math.Max( 1, galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() / 200 ) * seedMult) / 2f );

                        //then put half the rest of the SeededCitadels in the middle distance
                        seedThisMany += targetToSeed;
                        //an extra one for each human empire beyond the first, only in the first half of the galaxy
                        seedThisMany += (countOfPlayerFactionsNeedingCitadels - 1);
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, citadelTag, SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 3, 8 + extraDistanceForMiddleDistanceItems, 2, 999, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
                        seedThisMany -= planetsSeeded.Count;
                        if ( seedThisMany < 0 ) seedThisMany = 0;
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "Citadels - middle distance", seedThisMany );

                        //then put half the rest of the SeededCitadels kind of wherever
                        seedThisMany += targetToSeed;
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, FactionType.NaturalObject, SpecialEntityType.None, citadelTag, SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 3 - reducedDistanceRestrictionForAnyItems, 2, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.DoNoExpansion, null, -1 );
                        seedThisMany -= planetsSeeded.Count;
                        if ( seedThisMany < 0 ) seedThisMany = 0;
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "Citadels - wherever", seedThisMany );
                    }
                }
            }
            #endregion Citadels

            targetToSeed = 0;
            seedThisMany = 0;

            #region Officers
            if ( !(tutorialData?.SkipMobileOfficerCombatFleets == true) )
            {
                seedMult = AIWar2GalaxySettingTable.GetFauxFloatValueFromSettingByName_DuringGame( "OfficerFleetsToSeed", 1.0f );
                
                if (seedMult > 0)
                {
                    string officerTag = "AnyOfficer";
                    int countOfPlayerFactionsNeedingOfficerFleets = 0;
                    foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                    {
                        PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                        if ( playerType == null )
                            continue; //only seed if they say to
                        if ( playerType.MapGen_ShouldOfficerFleetsSeedForMe )
                            countOfPlayerFactionsNeedingOfficerFleets++;
                        if ( playerType.MapGen_RequireOfficerFleetTag != null && playerType.MapGen_RequireOfficerFleetTag.Length > 0 )
                            officerTag = playerType.MapGen_RequireOfficerFleetTag;
                    }
                    
                    if (AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame("ExpertOfficers"))
                        officerTag += "_Expert";

                    if ( countOfPlayerFactionsNeedingOfficerFleets > 0 )
                    {
                        
                        targetToSeed = (int)(Math.Max( 3, galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() / 120 ) * seedMult);
                        //an extra one in general
                        targetToSeed++;
                        //an extra two for each human empire beyond the first
                        targetToSeed += ((countOfPlayerFactionsNeedingOfficerFleets - 1) * 2);

                        //then put all of the MobileOfficerCombatFleetFlagship pretty darn far away!
                        seedThisMany += targetToSeed;
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, officerTag, SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 6 - reducedDistanceRestrictionForAnyItems, 999, 3, 999, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal, null, -1 );
                        seedThisMany -= planetsSeeded.Count;
                        if ( seedThisMany < 0 ) seedThisMany = 0;

                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "Officers - wherever", seedThisMany );
                    }
                }
            }
            #endregion Officers

            #region LoneSpire
            if ( tutorialData == null || !tutorialData.SkipMobileOfficerCombatFleets )
            {
                seedMult = AIWar2GalaxySettingTable.GetFauxFloatValueFromSettingByName_DuringGame( "OfficerFleetsToSeed", 1.0f );
                
                if (seedMult > 0)
                {
                    int countOfPlayerFactionsNeedingLoneSpire = 0;
                    foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                    {
                        PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                        if ( playerType == null )
                            continue; //only seed if they say to
                        if ( playerType.MapGen_ShouldLoneSpireSeedForMe )
                            countOfPlayerFactionsNeedingLoneSpire++;
                    }

                    if ( countOfPlayerFactionsNeedingLoneSpire > 0 )
                    {
                        seedThisMany = 1;
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "LoneSpire", SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 3, 999, 4, 999, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal, null, -1 );
                        seedThisMany -= planetsSeeded.Count;
                        if ( seedThisMany < 0 ) seedThisMany = 0;

                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "LoneSpire - wherever", seedThisMany );
                    }
                }
            }
            #endregion

            targetToSeed = 0;
            seedThisMany = 0;

            #region BrutalGuardianLairs
            //Turn normal brutal guardian lairs that were spawned earlier into royal ones
            if ( (tutorialData == null || !tutorialData.SkipAllGalaxyWideCapturablesAndObstacles) && ExpansionTable.Instance.GetRowByName( "3_The_Neinzul_Abyss" ).IsInstalledAndEnabled &&
                World_AIW2.Instance.Setup.GetIntBySetting( "BrutalGuardianLairs" ) > 0 )
            {
                targetToSeed = World_AIW2.Instance.Setup.GetIntBySetting( "BrutalGuardianLairs" );
                seedThisMany = targetToSeed;
                planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "BrutalGuardianLair", SeedingType.HardcodedCount,
                    seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigBad, 5, 999, 2, 999, PlanetSeedingZone.GravityWell, SeedingExpansionType.ComplicatedOriginal, null, -1 );
                Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "BrutalGuardianLairs - wherever", seedThisMany );

                foreach ( GameEntity_Squad squad in World_AIW2.Instance.Squads( "BrutalGuardianLair" ) )
                {
                    Faction owner = World_AIW2.Instance.Factions[squad.Planet.InitialOwningAIFactionIndex];
                    if ( owner.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>().SentinelInfo.AIType.InternalName.Equals( "Royal" ) )
                    {
                        owner.SpawnNewUnit_ReturnNullIfMPClient( Context, squad.Planet, squad.WorldLocation, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "RoyalBrutalGuardianLair" ),
                            0, squad.FleetMembership.Fleet, 0, EntityBehaviorType.Stationary, -1, null, "BGL_to_Royal_Switch" );
                        if ( true )
                            MapgenLogger.Log( "Swapped a Brutal Guardian Lair to a Royal variant on " + squad.Planet.Name );

                        squad.Despawn( Context, true, InstancedRendererDeactivationReason.SelfDestructOnTooHighOfCap );
                    }
                }
            }
            #endregion

            targetToSeed = 0;
            seedThisMany = 0;
            
            #endregion

            #region The bulk of the interesting strategic targets (and a few other things)
            if ( galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() >= 20 )
            {
                if ( tutorialData == null || !tutorialData.SkipZenithGoodies )
                {
                    int countOfPlayerFactionsNeedingZenithPowerGenerators = 0;
                    int countOfPlayerFactionsNeedingZenithMatterConverters = 0;
                    foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                    {
                        PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                        if ( playerType == null )
                            continue; //only seed if they say to
                        if ( playerType.MapGen_ShouldZenithPowerGeneratorsSeedForMe )
                            countOfPlayerFactionsNeedingZenithPowerGenerators++;
                        if ( playerType.MapGen_ShouldZenithMatterConvertersSeedForMe )
                            countOfPlayerFactionsNeedingZenithMatterConverters++;
                    }

                    if ( countOfPlayerFactionsNeedingZenithPowerGenerators > 0 )
                    {
                        int numZenithPowerGenerators = 1;
                        if ( galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() > 60 )
                            numZenithPowerGenerators++;

                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, SpecialEntityType.None, "ZenithPowerGenerator", SeedingType.HardcodedCount, numZenithPowerGenerators,
                            MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 3 + countOfPlayerFactionsNeedingZenithPowerGenerators - reducedDistanceRestrictionForAnyItems, 2, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal, null, -1 );
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "ZenithPowerGenerator", -1 );
                    }

                    if ( countOfPlayerFactionsNeedingZenithMatterConverters > 0 )
                    {
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, SpecialEntityType.None, "ZenithMatterConverter", SeedingType.HardcodedCount, 1,
                        MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 3 + countOfPlayerFactionsNeedingZenithMatterConverters - reducedDistanceRestrictionForAnyItems, 2, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal, null, -1 );
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "ZenithMatterConverter", -1 );
                    }
                }
                if ( tutorialData == null || !tutorialData.SkipCoprocessors )
                {
                    if ( World_AIW2.Instance.AllPlayerFactions.Count > 0 )
                    {
                        Faction firstPlayer = World_AIW2.Instance.AllPlayerFactions[0];
                        int hostileAIFactionCount = 0;
                        foreach ( Faction aiFac in World_AIW2.Instance.AIFactions )
                        {
                            if ( aiFac != null && aiFac.GetIsHostileTowards( firstPlayer ) )
                                hostileAIFactionCount++;
                        }

                        //only seed if at least two hostile AI factions
                        if ( hostileAIFactionCount >= 2 )
                        {
                            int countOfPlayerFactionsNeedingCoprocessors = 0;
                            foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                            {
                                PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                                if ( playerType == null || !playerType.MapGen_ShouldCoprocessorsSeedForMe )
                                    continue; //only seed if they say to
                                countOfPlayerFactionsNeedingCoprocessors++;
                            }

                            if ( countOfPlayerFactionsNeedingCoprocessors > 0 )
                            {
                                planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, FactionType.AI, SpecialEntityType.None, "Coprocessor", SeedingType.HardcodedCount, 4,
                                    MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 3 - reducedDistanceRestrictionForAnyItems, 2, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal, null, -1 );
                                Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "Coprocessor", -1 );
                            }
                        }
                    }
                }
            }
            if ( tutorialData == null || !tutorialData.SkipDistributionNodes )
            {
                string minorCapturableTag = "MinorCapturable";
                int countOfPlayerFactionsNeedingDistributionNodes = 0;
                int countOfPlayerFactionsNeedingMinorCapturables = 0;
                foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                {
                    PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                    if ( playerType == null )
                        continue; //only seed if they say to
                    if ( playerType.MapGen_ShouldDistributionNodesSeedForMe )
                        countOfPlayerFactionsNeedingDistributionNodes++;
                    if ( playerType.MapGen_ShouldMinorCapturablesSeedForMe )
                        countOfPlayerFactionsNeedingMinorCapturables++;
                    if ( playerType.MapGen_RequireMinorCapturableTag != null && playerType.MapGen_RequireMinorCapturableTag.Length > 0 )
                        minorCapturableTag = playerType.MapGen_RequireMinorCapturableTag;
                }

                if ( countOfPlayerFactionsNeedingDistributionNodes > 0 )
                {
                    int distributionNodesToSeed = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "DistributionNodesToSeed" );
                    planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, FactionType.AI, SpecialEntityType.None, "DistributionNode", SeedingType.HardcodedCount, distributionNodesToSeed,
                        MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 1, 1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal, null, -1 );
                    Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "DistributionNode", -1 );
                }

                if ( countOfPlayerFactionsNeedingMinorCapturables > 0 )
                {
                    if ( GameEntityTypeDataTable.Instance.RowsByTag.CheckIfAlreadyHasKey( minorCapturableTag ) )
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, FactionType.NaturalObject, SpecialEntityType.None, minorCapturableTag,
                            SeedingType.HardcodedCount, 4 + countOfPlayerFactionsNeedingMinorCapturables,
                            MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, countOfPlayerFactionsNeedingMinorCapturables - reducedDistanceRestrictionForAnyItems, 1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal, null, -1 );
                }
            }

            #region Fuel Seeding
            if ( tutorialData == null && World_AIW2.Instance.IsFuelEnabled )
            {
                #region Basic Seeding All Over The Place
                int countOfPlayerFactionsNeedingFuel = 0;
                foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                {
                    PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                    if ( playerType == null || !playerType.MapGen_ShouldFuelSeedForMeIfFuelIsEnabled )
                        continue; //only seed fuel if they need it
                    countOfPlayerFactionsNeedingFuel++;
                }

                if ( countOfPlayerFactionsNeedingFuel > 0 )
                {
                    int planetCount = galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise();
                    int argonDivisor = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame("Fuel_MajorArgonPlanetDivisor");
                    int argonMultiplier = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame("Fuel_MajorArgonPlanetMultiplier");
                    int argonCount = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame("Fuel_MajorArgonExtra");
                    int argonMinHops = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame("Fuel_MajorArgonRandomMinHop");
                    int radonDivisor = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame("Fuel_MajorRadonPlanetDivisor");
                    int radonMultiplier = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame("Fuel_MajorRadonPlanetMultiplier");
                    int radonCount = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame("Fuel_MajorRadonExtra");
                    int radonMinHops = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame("Fuel_MajorRadonRandomMinHop");
                    int xenonDivisor = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame("Fuel_MajorXenonPlanetDivisor");
                    int xenonMultiplier = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame("Fuel_MajorXenonPlanetMultiplier");
                    int xenonCount = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame("Fuel_MajorXenonExtra");
                    int xenonMinHops = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame("Fuel_MajorXenonRandomMinHop");
                    int balancedDivisor = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame("Fuel_MajorBalancedPlanetDivisor");
                    int balancedMultiplier = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame("Fuel_MajorBalancedPlanetMultiplier");
                    int balancedCount = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame("Fuel_MajorBalancedExtra");
                    int balancedMinHops = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame("Fuel_MajorBalancedRandomMinHop");
                    if (argonDivisor > 0 && argonMultiplier > 0) {
                        argonCount += argonMultiplier * planetCount / argonDivisor;
                    }
                    if (radonDivisor > 0 && radonMultiplier > 0) {
                        radonCount += radonMultiplier * planetCount / radonDivisor;
                    }
                    if (xenonDivisor > 0 && xenonMultiplier > 0) {
                        xenonCount += xenonMultiplier * planetCount / xenonDivisor;
                    }
                    if (balancedDivisor > 0 && balancedMultiplier > 0) {
                        balancedCount += balancedMultiplier * planetCount / balancedDivisor;
                    }

                    ThrowawayListCanMemLeak<KeyValuePair<GameEntityTypeData, int>> typesToSeed = new ThrowawayListCanMemLeak<KeyValuePair<GameEntityTypeData, int>>( 300 );
                    for ( int i = 0; i < argonCount; i++ )
                        typesToSeed.Add( new KeyValuePair<GameEntityTypeData, int>(GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MajorArgon" ), argonMinHops) );
                    for ( int i = 0; i < radonCount; i++ )
                        typesToSeed.Add( new KeyValuePair<GameEntityTypeData, int>(GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MajorRadon" ), radonMinHops) );
                    for ( int i = 0; i < xenonCount; i++ )
                        typesToSeed.Add( new KeyValuePair<GameEntityTypeData, int>(GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MajorXenon" ), xenonMinHops) );
                    for ( int i = 0; i < balancedCount; i++ )
                        typesToSeed.Add( new KeyValuePair<GameEntityTypeData, int>(GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "BalancedFuel" ), balancedMinHops) );

                    ThrowawayListCanMemLeak<Planet> planetList = galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs()
                        .ToThrowawayListCanMemLeak_HugeWasteAndExpensive_AvoidIfAtAllPossible();
                    planetsSeeded = new ThrowawayListCanMemLeak<Planet>( 30 );

                    int percentTwoFuelStations = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame("Fuel_PercentageTwoFuelStations");
                    while ( planetList.Count > 0 && typesToSeed.Count > 0 )
                    {
                        int index = Context.RandomToUse.Next( 0, planetList.Count );
                        Planet planet = planetList[index];
                        planetList.RemoveAt( index );

                        switch ( planet.PopulationType )
                        {
                            case PlanetPopulationType.AIBastionWorld:
                            case PlanetPopulationType.AIHomeworld:
                            case PlanetPopulationType.DarkZenith:
                            case PlanetPopulationType.HumanHomeworld:
                                continue; //don't seed on any of those...
                        }

                        if ( typesToSeed.Count == 0 )
                            break;

                        // Try to find a type that can be seeded this close.
                        // We assume that Stations that can be seeded closer to
                        // homeworlds were added first.
                        index = Context.RandomToUse.Next( 0, typesToSeed.Count );
                        while (index >= 0) {
                            int minHops = typesToSeed[index].Value;
                            if (planet.OriginalHopsToHumanHomeworld >= minHops) {
                                break;
                            };
                            index--;
                        }
                        if ( index < 0 ) {
                            continue;
                        }
                        GameEntityTypeData typeToSeed = typesToSeed[index].Key;
                        typesToSeed.RemoveAt( index );

                        Helper_DoInnermostSeed_ByType( Context, planet, typeToSeed, /* naturalobject */ planet.Factions[0].Faction,
                                PlanetSeedingZone.MostAnywhere );
                        planet.MapGen_NumberOfFuelThingsHere++;
                        planetsSeeded.Add(planet);

                        if ( Context.RandomToUse.Next( 0, 100 ) < percentTwoFuelStations )
                        {
                            //10% chance of adding a second one!

                            if ( typesToSeed.Count == 0 )
                                break;
                            //otherwise, let's seed something!

                            // Try to find a type that can be seeded this close.
                            // We assume that Stations that can be seeded closer to
                            // homeworlds were added first.
                            index = Context.RandomToUse.Next( 0, typesToSeed.Count );
                            while (index >= 0) {
                                int minHops = typesToSeed[index].Value;
                                if (planet.OriginalHopsToHumanHomeworld >= minHops) {
                                    break;
                                };
                                index--;
                            }
                            if ( index < 0 ) {
                                continue;
                            }
                            typeToSeed = typesToSeed[index].Key;
                            typesToSeed.RemoveAt( index );

                            Helper_DoInnermostSeed_ByType( Context, planet, typeToSeed, /* naturalobject */ planet.Factions[0].Faction,
                                    PlanetSeedingZone.MostAnywhere );
                            planet.MapGen_NumberOfFuelThingsHere++;
                        }
                    }
                    Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "Fuel Stations - wherever", -1 );
                }
                #endregion Basic Seeding All Over The Place

                #region Extras Near Players
                if ( countOfPlayerFactionsNeedingFuel > 0 ) {
                    //one per player starting world
                    foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                    {
                        PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                        if ( !(playerType?.MapGen_ShouldFuelSeedForMeIfFuelIsEnabled ?? false) ) {
                            continue; //only seed fuel near factions that need it
                        }

                        //put one extra MajorArgon within 2-4 hops of each human homeworld
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "MajorArgon", SeedingType.HardcodedCount,
                                1, MapGenCountPerPlanet.One, MapGenSeedStyle.Fuel, 2, -1, 3, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                                player, 4 );
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "Argon Fuel - 2-4 hops out from " + player.GetDisplayName(), seedThisMany );

                        //put one extra MajorRadon within 2-4 hops of each human homeworld
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "MajorRadon", SeedingType.HardcodedCount,
                                1, MapGenCountPerPlanet.One, MapGenSeedStyle.Fuel, 2, -1, 3, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                                player, 4 );
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "Radon Fuel - 2-4 hops out from " + player.GetDisplayName(), seedThisMany );

                        //put one extra MajorRadon within 3-5 hops of each human homeworld
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "MajorRadon", SeedingType.HardcodedCount,
                                1, MapGenCountPerPlanet.One, MapGenSeedStyle.Fuel, 3, -1, 3, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                                player, 5 );
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "Radon Fuel - 3-5 hops out from " + player.GetDisplayName(), seedThisMany );

                        //put one extra MajorXenon within 2-4 hops of each human homeworld
                        planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "MajorXenon", SeedingType.HardcodedCount,
                                1, MapGenCountPerPlanet.One, MapGenSeedStyle.Fuel, 2, -1, 3, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                                player, 4 );
                        Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "Xenon Fuel - 2-4 hops out from " + player.GetDisplayName(), seedThisMany );
                    }
                }
                #endregion Extras Near AI Homes
            }
            #endregion Fuel Seeding

            int countOfPlayerFactionsNeedingDataCenters = 0;
            int countOfPlayerFactionsNeedingMajorDataCenters = 0;
            int countOfPlayerFactionsNeedingSuperTerminals = 0;
            int countOfPlayerFactionsNeedingSocketIncreasers = 0;
            foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
            {
                PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                if ( playerType == null )
                    continue; //only seed if they say to
                if ( playerType.MapGen_ShouldDataCentersSeedForMe )
                    countOfPlayerFactionsNeedingDataCenters++;
                if ( playerType.MapGen_ShouldMajorDataCentersSeedForMe )
                    countOfPlayerFactionsNeedingMajorDataCenters++;
                if ( playerType.MapGen_ShouldSuperTerminalsSeedForMe )
                    countOfPlayerFactionsNeedingSuperTerminals++;
                if ( playerType.MapGen_ShouldSocketIncreasersSeedForMe )
                    countOfPlayerFactionsNeedingSocketIncreasers++;

            }

            if ( ( tutorialData == null || !tutorialData.SkipNormalDataCenters ) && countOfPlayerFactionsNeedingDataCenters > 0 )
            {
                int normalDataCentersToSeed = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "NormalDataCentersToSeed" );
                if (normalDataCentersToSeed > 0)
                {
                    int planetCount = galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise();
                    if ( planetCount > 80 )
                    {
                        float multiplier = (float)planetCount / 80f;
                        if ( multiplier > 1f)
                        {
                            multiplier -= 1f;
                            multiplier /= 2f;
                            multiplier += 1f;
                            if ( multiplier > 1f )
                                normalDataCentersToSeed = (int)Mathf.RoundToInt( normalDataCentersToSeed * multiplier );
                        }
                    }
                }

                int minHopsFromPlayerHomeworld = 5;
                if ( highestDifficulty.Difficulty >= 8 )
                    minHopsFromPlayerHomeworld = 7;

                //only one close to the player
                if (normalDataCentersToSeed > 0)
                {
                    planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "DataCenter", SeedingType.HardcodedCount,
                                                                1, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 3 - reducedDistanceRestrictionForAnyItems, 5 + extraDistanceForMiddleDistanceItems, 2, 999,
                                                                PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal, null, -1 );
                    Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "DataCenter - close", -1 );

                    normalDataCentersToSeed--;
                }

                //the rest of them
                if (normalDataCentersToSeed > 0)
                {
                    planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, FactionType.AI, SpecialEntityType.None, "DataCenter", SeedingType.HardcodedCount, normalDataCentersToSeed,
                        MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, minHopsFromPlayerHomeworld, 2, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal, null, -1 );
                    Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "DataCenter - the rest", -1 );
                }
            }

            if ( ( tutorialData == null || !tutorialData.SkipMajorDataCenters ) && countOfPlayerFactionsNeedingMajorDataCenters > 0 )
            {
                int majorDataCentersToSeed = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "MajorDataCentersToSeed" );
                if ( majorDataCentersToSeed > 0 )
                {
                    planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, FactionType.NaturalObject, SpecialEntityType.None, "MajorDataCenter", SeedingType.HardcodedCount, majorDataCentersToSeed,
                        MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 3 - reducedDistanceRestrictionForAnyItems, 2, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal, null, -1 );
                    Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "MajorDataCenter", -1 );
                }
            }

            if ( (tutorialData == null || !tutorialData.SkipSuperTerminals) && countOfPlayerFactionsNeedingSuperTerminals > 0 )
            {
                planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, FactionType.AI, SpecialEntityType.None, "SuperTerminal", SeedingType.HardcodedCount, 1,
                    MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 3 - reducedDistanceRestrictionForAnyItems, 2, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal, null, -1 );
                Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, "SuperTerminal", -1 );
            }
            #endregion

            #region Socket Increasers (Mod Support)
            if (tutorialData == null && countOfPlayerFactionsNeedingSocketIncreasers > 0 )
            {
                //These are desirable targets for any faction that uses sockets. We seed a minor one near every player that wants to use sockets
                foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                {
                    seedThisMany = 1;
                    PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                    if ( playerType == null || !playerType.MapGen_ShouldSocketIncreasersSeedForMe )
                        continue; //only seed socket increasers if they say to

                    //Seed two minor increasers close to every player using sockets, and a major "not too far away"
                    planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "MinorSocketIncreaser", SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 1, 2, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                        null, (Int16)(1 + extraDistanceForAdajentSeededItems) );
                    planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "MinorSocketIncreaser", SeedingType.CapturableWeightsAndMax,
                        seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 2, 4, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                        null, (Int16)(1 + extraDistanceForAdajentSeededItems) );

                    planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "MajorSocketIncreaser", SeedingType.CapturableWeightsAndMax,
                            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 2, 5, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                            null, (Int16)(1 + extraDistanceForAdajentSeededItems) );
                }
                //Minor increasers everywhere else
                seedThisMany = 6 + countOfPlayerFactionsNeedingSocketIncreasers;
                planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "MinorSocketIncreaser", SeedingType.CapturableWeightsAndMax,
                    seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 4, 99, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                    null, (Int16)(1 + extraDistanceForAdajentSeededItems) );
                //Major increasers everywhere else
                seedThisMany = 2 + countOfPlayerFactionsNeedingSocketIncreasers;
                planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "MajorSocketIncreaser", SeedingType.CapturableWeightsAndMax,
                    seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 4, 99, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                    null, (Int16)(1 + extraDistanceForAdajentSeededItems) );
            }
            #endregion

            #region Do Any Custom Galaxy-Wide Seeding Per Faction Type
            foreach ( var fac in World_AIW2.Instance.Factions )
            {
                fac.DeepInfo.SeedSpecialEntities_LateAfterAllFactionSeeding_CustomForPlayerType( galaxy, Context, MapData );
            }
            #endregion

            planetsRandomizedAtLeastOnce.Clear();

            #region Mark Level Randomization
            {
                int numberOfPlanetsToRandomizeMarks2Through6 = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "NumberOfPlanetsToRandomizeMarks2Through6" );
                if ( numberOfPlanetsToRandomizeMarks2Through6 > 0 )
                {
                    ThrowawayListCanMemLeak<Planet> workingPlanets = new ThrowawayListCanMemLeak<Planet>( 30 );
                    #region First get the planets
                    foreach ( Planet p in galaxy.Planets( false ) )
                    {
                        switch ( p.PopulationType )
                        {
                            case PlanetPopulationType.None:
                            case PlanetPopulationType.NonHomeworld:
                                break;
                            default:
                                continue;
                        }
                        if ( p.MarkLevelForAIOnly != null )
                        {
                            if ( p.MarkLevelForAIOnly.Ordinal >= 2 && p.MarkLevelForAIOnly.Ordinal <= 6 )
                                workingPlanets.Add( p );
                        }
                    }
                    #endregion
                    if ( workingPlanets.Count > 1 )
                    {
                        for ( int index = 0; index < numberOfPlanetsToRandomizeMarks2Through6; index++ )
                        {
                            int p1 = Context.RandomToUse.Next( 0, workingPlanets.Count );
                            int p2 = Context.RandomToUse.Next( 0, workingPlanets.Count );
                            while ( p1 == p2 )
                                p2 = Context.RandomToUse.Next( 0, workingPlanets.Count );

                            //do the swap
                            Planet planet1 = workingPlanets[p1];
                            Balance_MarkLevel planet1Mark = planet1.MarkLevelForAIOnly;
                            Planet planet2 = workingPlanets[p2];
                            planet1.MarkLevelForAIOnly = planet2.MarkLevelForAIOnly;
                            planet2.MarkLevelForAIOnly = planet1Mark;

                            planetsRandomizedAtLeastOnce[planet1] = true;
                            planetsRandomizedAtLeastOnce[planet2] = true;
                        }
                    }
                }
            }
            {
                int numberOfPlanetsToRandomizeMarks1Through4 = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "NumberOfPlanetsToRandomizeMarks1Through4" );
                if ( numberOfPlanetsToRandomizeMarks1Through4 > 1 )
                {
                    ThrowawayListCanMemLeak<Planet> workingPlanets = new ThrowawayListCanMemLeak<Planet>( 500 );
                    #region First get the planets
                    foreach ( Planet p in galaxy.Planets( false ) )
                    {
                        switch ( p.PopulationType )
                        {
                            case PlanetPopulationType.None:
                            case PlanetPopulationType.NonHomeworld:
                                break;
                            default:
                                continue;
                        }
                        if ( p.MarkLevelForAIOnly != null )
                        {
                            if ( p.MarkLevelForAIOnly.Ordinal >= 1 && p.MarkLevelForAIOnly.Ordinal <= 4 )
                                workingPlanets.Add( p );
                        }
                    }
                    #endregion
                    if ( workingPlanets.Count > 1 )
                    {
                        for ( int index = 0; index < numberOfPlanetsToRandomizeMarks1Through4; index++ )
                        {
                            int p1 = Context.RandomToUse.Next( 0, workingPlanets.Count );
                            int p2 = Context.RandomToUse.Next( 0, workingPlanets.Count );
                            while ( p1 == p2 )
                                p2 = Context.RandomToUse.Next( 0, workingPlanets.Count );

                            //do the swap
                            Planet planet1 = workingPlanets[p1];
                            Balance_MarkLevel planet1Mark = planet1.MarkLevelForAIOnly;
                            Planet planet2 = workingPlanets[p2];
                            planet1.MarkLevelForAIOnly = planet2.MarkLevelForAIOnly;
                            planet2.MarkLevelForAIOnly = planet1Mark;

                            planetsRandomizedAtLeastOnce[planet1] = true;
                            planetsRandomizedAtLeastOnce[planet2] = true;
                        }
                    }
                }
            }
            #endregion

            #region fix the mark levels to match on swapped planets
            foreach ( KeyValuePair<Planet, bool> kv in planetsRandomizedAtLeastOnce )
            {
                byte markLevelToTrySetting = kv.Key.MarkLevelForAIOnly.Ordinal;
                foreach ( GameEntity_Squad entity in kv.Key.Squads() )
                {
                        if ( entity.GetFactionTypeSafe() == FactionType.AI )
                            entity.SetCurrentMarkLevel( markLevelToTrySetting );
                    }
            }
            #endregion

            //All the planet mark levels are finalized!  So time to seed via RowsByPlanetSeedingType/AutoSeedingOnPlanets
            //===========================================================================================================

            #region Seed By PlanetSeedingType Per Planet
            foreach ( Planet planet in galaxy.Planets( false ) )
            {
                PlanetSeedingType seedingType = PlanetSeedingType.None;
                switch (planet.PopulationType)
                {
                    case PlanetPopulationType.AIHomeworld:
                        seedingType = PlanetSeedingType.AIHomeworld;
                        break;
                    case PlanetPopulationType.AIBastionWorld:
                        seedingType = PlanetSeedingType.AIBastion;
                        break;
                    case PlanetPopulationType.ArkEmpireHumanHomeworld:
                        seedingType = PlanetSeedingType.Mark1;
                        break;
                    case PlanetPopulationType.HumanHomeworld:
                        seedingType = PlanetSeedingType.PlayerHomeworld;
                        break;
                    default:
                        if ( planet.MarkLevelForAIOnly == null )
                            continue;
                        switch (planet.MarkLevelForAIOnly.Ordinal )
                        {
                            case 1:
                                seedingType = PlanetSeedingType.Mark1;
                                break;
                            case 2:
                                seedingType = PlanetSeedingType.Mark2;
                                break;
                            case 3:
                                seedingType = PlanetSeedingType.Mark3;
                                break;
                            case 4:
                                seedingType = PlanetSeedingType.Mark4;
                                break;
                            case 5:
                                seedingType = PlanetSeedingType.Mark5;
                                break;
                            case 6:
                                seedingType = PlanetSeedingType.Mark6NonBastion;
                                break;
                            case 7:
                                seedingType = PlanetSeedingType.Mark7NonHomeworld;
                                break;
                        }
                        break;
                }
                if ( seedingType == PlanetSeedingType.None )
                    continue;
                if ( !GameEntityTypeDataTable.Instance.RowsByPlanetSeedingType.CheckIfAlreadyHasKey( seedingType ) )
                    continue;

                List<GameEntityTypeData> rowsToConsider = GameEntityTypeDataTable.Instance.RowsByPlanetSeedingType[seedingType];
                if ( rowsToConsider == null )
                    continue;

                //Note that auto-seeded units are always seeded to belong to the AI if possible
                Faction planetOwnerFaction = planet.GetInitialControllingAIFactionOrNull();
                if ( planetOwnerFaction == null )
                    continue;

                foreach ( GameEntityTypeData typeData in rowsToConsider )
                {
                    if ( !typeData.AutoSeedingOnPlanets.ContainsKey( seedingType ) )
                        continue;
                    int chanceOfSeeding = typeData.AutoSeedingOnPlanets[seedingType];
                    if ( chanceOfSeeding <= 0 )
                        continue;
                    if ( Context.RandomToUse.Next( 0, 100 ) > chanceOfSeeding )
                        continue;

                    //ArcenDebugging.ArcenDebugLogSingleLine( "Seed " + typeData.DisplayName + " on " + planet.Name + ", belonging to " + 
                    //                                        planetOwnerFaction.GetDisplayName(), Verbosity.DoNotShow );
                    Helper_DoInnermostSeed_ByType( Context, planet, typeData, planetOwnerFaction, PlanetSeedingZone.MostAnywhere );
                }
            }
            #endregion Seed By PlanetSeedingType Per Planet

            // Pretty much all else is done... so what about sprinkling in some beacons for meeting new friends?
            //===========================================================================================================

            AddDiscoverableFactionBeacons( galaxy, Context );
        }

        #region AddDiscoverableFactionBeacons
        public void AddDiscoverableFactionBeacons( Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            if ( World_AIW2.Instance.GetIsTutorial() )
                return; //don't do this for tutorials
            if ( !World_AIW2.Instance.Setup.GetBoolBySetting( "BeaconsEnabled" ) )
                return; //if beacons are not enabled, then also don't do this

            ThrowawayListCanMemLeak<BeaconFactionOption> minorBeacons = new ThrowawayListCanMemLeak<BeaconFactionOption>( 10 );
            ThrowawayListCanMemLeak<BeaconFactionOption> mediumBeacons = new ThrowawayListCanMemLeak<BeaconFactionOption>( 10 );
            ThrowawayListCanMemLeak<BeaconFactionOption> majorBeacons = new ThrowawayListCanMemLeak<BeaconFactionOption>( 10 );
            byte minorToSeed = 1;
            byte mediumToSeed = 1;
            byte majorToSeed = 1;

            for ( int i = 0; i < BeaconFactionOptionTable.Instance.Rows.Count; i++ )
            {
                BeaconFactionOption option = BeaconFactionOptionTable.Instance.Rows[i];
                if ( option == null )
                    continue;
                switch ( option.BeaconTierForSeeding )
                {
                    case "Minor":
                        minorBeacons.Add( option );
                        break;
                    case "Medium":
                        mediumBeacons.Add( option );
                        break;
                    case "Major":
                        majorBeacons.Add( option );
                        break;
                    case "AlwaysSeed":
                        AddDiscoverableFactionBeacons_Inner( galaxy, Context, option );
                        break;
                    default:
                        ArcenDebugging.ArcenDebugLogSingleLine( "Unknown BeaconTypeForSeeding '" + option.BeaconTierForSeeding + "' passed to AddDiscoverableFactionBeacons()!", Verbosity.ShowAsError );
                        break;
                }
            }

            //Iterate over all the beacon faction options.  For any that are not blocked by existing factions, add their beacon somewhere random
            RandomGenerator rand = Context.RandomToUse;
            int amountSeeded = 0;
            while ( amountSeeded < minorToSeed && minorBeacons.Count > 0)
            {
                int randomizedIndex = rand.Next( 0, minorBeacons.Count );
                if ( AddDiscoverableFactionBeacons_Inner( galaxy, Context, minorBeacons[randomizedIndex] ) )
                    minorBeacons.RemoveAt( randomizedIndex );
                else
                    amountSeeded++;
            }
            amountSeeded = 0;
            while ( amountSeeded < mediumToSeed && mediumBeacons.Count > 0 )
            {
                int randomizedIndex = rand.Next( 0, mediumBeacons.Count );
                if ( AddDiscoverableFactionBeacons_Inner( galaxy, Context, mediumBeacons[randomizedIndex] ) )
                    mediumBeacons.RemoveAt( randomizedIndex );
                else
                    amountSeeded++;
            }
            amountSeeded = 0;
            while ( amountSeeded < majorToSeed && majorBeacons.Count > 0 )
            {
                int randomizedIndex = rand.Next( 0, majorBeacons.Count );
                if ( AddDiscoverableFactionBeacons_Inner( galaxy, Context, majorBeacons[randomizedIndex] ) )
                    majorBeacons.RemoveAt( randomizedIndex );
                else
                    amountSeeded++;
            }
        }

        private bool AddDiscoverableFactionBeacons_Inner( Galaxy galaxy, ArcenHostOnlySimContext Context, BeaconFactionOption option )
        {
            bool debug = false;
            IList<Planet> planetsSeeded;

            bool hasBeenBlockedYet = false;
            if ( option.BlockedFromSeedingIfAnyOfTheseFactionsIncluded.Count > 0 )
            {
                for ( int k = 0; k < option.BlockedFromSeedingIfAnyOfTheseFactionsIncluded.Count; k++ )
                {
                    string factionThatBlocksMe = option.BlockedFromSeedingIfAnyOfTheseFactionsIncluded[k];

                    for ( int j = 0; j < World_AIW2.Instance.Factions.Count; j++ )
                    {
                        Faction existingFac = World_AIW2.Instance.Factions[j];

                        if ( existingFac.SpecialFactionData.InternalName == factionThatBlocksMe )
                        {
                            hasBeenBlockedYet = true;
                            if ( debug)
                                ArcenDebugging.ArcenDebugLogSingleLine( "\tWe've found a pre-existing instance of faction " + existingFac.SpecialFactionData.InternalName +
                                    ", so are skipping beacon option " + option.InternalName, Verbosity.DoNotShow );
                            break;
                        }
                    }
                    if ( hasBeenBlockedYet )
                        break;
                }
            }

            if ( !hasBeenBlockedYet )
            {
                if ( option.BlockedFromSeedingIfAnyOfThesePlayerTypesIncluded.Count > 0 )
                {
                    for ( int k = 0; k < option.BlockedFromSeedingIfAnyOfThesePlayerTypesIncluded.Count; k++ )
                    {
                        string playerTypeThatBlocksMe = option.BlockedFromSeedingIfAnyOfThesePlayerTypesIncluded[k];

                        for ( int j = 0; j < World_AIW2.Instance.Factions.Count; j++ )
                        {
                            Faction existingFac = World_AIW2.Instance.Factions[j];
                            PlayerTypeData playerType = existingFac.PlayerTypeDataOrNull_ModeratelyExpensive;
                            if ( playerType == null )
                                continue;

                            if ( playerType.InternalName == playerTypeThatBlocksMe )
                            {
                                hasBeenBlockedYet = true;
                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "\tWe've found a pre-existing instance of player type " + playerType.InternalName +
                                        ", so are skipping beacon option " + option.InternalName, Verbosity.DoNotShow );
                                break;
                            }
                        }
                        if ( hasBeenBlockedYet )
                            break;
                    }
                }
            }

            if ( !hasBeenBlockedYet )
            {
                if ( option.RequiresAtLeastOneMetalUsingPlayer )
                {
                    bool foundAMetalUser = false;
                    bool foundAnyNonSpectators = false;
                    for ( int j = 0; j < World_AIW2.Instance.Factions.Count; j++ )
                    {
                        Faction existingFac = World_AIW2.Instance.Factions[j];
                        PlayerTypeData playerType = existingFac.PlayerTypeDataOrNull_ModeratelyExpensive;
                        if ( playerType == null )
                            continue;

                        if ( !playerType.IsSpectator )
                        {
                            foundAnyNonSpectators = true;
                            if ( playerType.UsesMetal )
                                foundAMetalUser = true;
                        }
                    }
                    if ( !foundAMetalUser && foundAnyNonSpectators )
                    {
                        hasBeenBlockedYet = true;
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "\tWe did not find a metal user player, and a non-spectator player, so are skipping beacon option " + 
                                option.InternalName, Verbosity.DoNotShow );
                    }
                }
            }

            if ( !hasBeenBlockedYet )
            {
                if ( option.BlockedFromSeedingUnlessAllOfTheseFactionsAreIncluded.Count > 0 )
                {
                    for ( int k = 0; k < option.BlockedFromSeedingUnlessAllOfTheseFactionsAreIncluded.Count; k++ )
                    {
                        string factionTypeThatAllowsMe = option.BlockedFromSeedingUnlessAllOfTheseFactionsAreIncluded[k];
                        bool foundWhatWeNeeded = false;
                        for ( int j = 0; j < World_AIW2.Instance.Factions.Count; j++ )
                        {
                            Faction existingFac = World_AIW2.Instance.Factions[j];
                            if ( existingFac.SpecialFactionData.InternalName == factionTypeThatAllowsMe )
                            {
                                foundWhatWeNeeded = true;                                       
                                break;
                            }
                        }
                        if ( !foundWhatWeNeeded )
                        {
                            hasBeenBlockedYet = true;
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "\tWe've NOT found any instance of faction " + factionTypeThatAllowsMe +
                                    ", so are skipping beacon option " + option.InternalName, Verbosity.DoNotShow );
                        }
                    }
                }
            }

            if ( hasBeenBlockedYet )
                return hasBeenBlockedYet; //existing presence of something else blocked this, so nevermind.  Do not seed.

            //we have decided to seed the beacon, so go ahead and do that now
            if ( option.TagFromWhichToSeedOneBeacon != null && option.TagFromWhichToSeedOneBeacon.Length > 0 )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "\tSeed Beacon with tag '" + option.TagFromWhichToSeedOneBeacon +
                        "' for " + option.InternalName, Verbosity.DoNotShow );

                planetsSeeded = Mapgen_SeedSpecialEntities( Context, galaxy, FactionType.NaturalObject, SpecialEntityType.None, option.TagFromWhichToSeedOneBeacon, 
                    SeedingType.HardcodedCount, 1,
                    MapGenCountPerPlanet.One, MapGenSeedStyle.FactionBeacon, 1, 1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal, null, -1 );
                Helper_MapgenLogger_PlanetsSeeder( planetsSeeded, option.TagFromWhichToSeedOneBeacon, -1 );
            }
            return hasBeenBlockedYet;
        }
        #endregion AddDiscoverableFactionBeacons

        private static int[] BASIC_EMPIRE_STAIRCASE_COUNT = new int[] { 0, 1, 1, 2, 2, 3 };

        public static int GetBasicEmpireStaircaseCountForCurrentHumanEmpireCount()
        {
            int humanEmpireCount = World_AIW2.Instance.EmpireStylePlayerFactions.Count;
            if ( humanEmpireCount >= BASIC_EMPIRE_STAIRCASE_COUNT.Length )
                return BASIC_EMPIRE_STAIRCASE_COUNT[BASIC_EMPIRE_STAIRCASE_COUNT.Length - 1];
            return BASIC_EMPIRE_STAIRCASE_COUNT[humanEmpireCount];
        }

        public void SetTurretAllowancesOnAIPlanets( Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            //if not seeding the details yet, then... skip all this!
            if ( !World_AIW2.Instance.Setup.ShouldSeedDetailsYet )
                return;

            MapgenLogger.Log( "*********SetTurretAllowancesOnAIPlanets********* For Faction Count: " + World_AIW2.Instance.AIFactions.Count );

            for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
                this.SetTurretAllowancesOnPlanetsOfSpecificAIFaction( World_AIW2.Instance.AIFactions[i], galaxy, Context );
        }

        private void SetTurretAllowancesOnPlanetsOfSpecificAIFaction( Faction ForAIFaction, Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            if ( ForAIFaction == null )
                return;

            AISentinelsCoreData sentinelsExternal = ForAIFaction.TryGetAISentinelsCoreData()?.SentinelInfo;
            if ( sentinelsExternal == null )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null sentinelsExternal on faction of type " + ForAIFaction.Type + " in SetTurretAllowancesOnPlanetsOfSpecificAIFaction. This should not be possible" );
                return;
            }

            AITypeData aiTypeData = sentinelsExternal.AIType;
            if ( sentinelsExternal == null )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null aiTypeData on sentinelsExternal of faction of type " + ForAIFaction.Type + " in SetTurretAllowancesOnPlanetsOfSpecificAIFaction. This should not be possible" );
                return;
            }

            //use floating point math, because this is host-only anyway and that is simpler
            float halfTurretPercent = aiTypeData.PercentageOutOf100OfPlanetsToHaveHalfCapTurretsAndNonTurretDefenses / 100f;
            float fullTurretPercent = aiTypeData.PercentageOutOf100OfPlanetsToHaveFullCapTurretsAndNonTurretDefenses / 100f;

            MapgenLogger.Log( "*********SetTurretAllowancesOnPlanetsOfSpecificAIFaction********* Faction: " + ForAIFaction.FactionIndex + " " + ForAIFaction.GetDisplayName() );

            ThrowawayListCanMemLeak<Planet> totalPlanetsOfThisFactionForSeeding = new ThrowawayListCanMemLeak<Planet>( 500 );

            #region totalPlanetsOfThisFaction
            foreach ( Planet planet in galaxy.Planets( false ) )
            {
                Faction controllingFaction = planet.GetControllingFaction();
                if ( controllingFaction != ForAIFaction && planet.InitialOwningAIFactionIndex != ForAIFaction.FactionIndex )
                    continue;
                //if ( planet.TypeData.Type == PlanetType.Nomad )
                //    return DelReturn.Continue; //blocked
                //if ( planet.MapGen_IsFullyUsedByAFaction )
                //    return DelReturn.Continue; //blocked
                if ( planet.PopulationType == PlanetPopulationType.AIHomeworld ) //fine for AIBastionWorld
                    continue; //don't allow turrets and such on the AI homeworld, that's just mean

                totalPlanetsOfThisFactionForSeeding.Add( planet );
            }
            #endregion

            if ( totalPlanetsOfThisFactionForSeeding.Count <= 0 )
            {
                MapgenLogger.Log( "No planets on which to set turret status." );
                return;
            }

            //use floating point math, because this is host-only anyway and that is simpler
            int planetsToHaveHalfTurretCaps = Mathf.CeilToInt( totalPlanetsOfThisFactionForSeeding.Count * halfTurretPercent );
            int planetsToHaveFullTurretCaps = Mathf.CeilToInt( totalPlanetsOfThisFactionForSeeding.Count * fullTurretPercent );
            if ( planetsToHaveHalfTurretCaps < 1 )
                planetsToHaveHalfTurretCaps = 1;
            if ( planetsToHaveFullTurretCaps < 1 )
                planetsToHaveFullTurretCaps = 1;

            MapgenLogger.Log( totalPlanetsOfThisFactionForSeeding.Count + " planets on which to set turret status. " + 
                aiTypeData.PercentageOutOf100OfPlanetsToHaveHalfCapTurretsAndNonTurretDefenses + "% to have half cap. " +
                aiTypeData.PercentageOutOf100OfPlanetsToHaveFullCapTurretsAndNonTurretDefenses + "% to have full cap. " +
                planetsToHaveHalfTurretCaps + " actual planets to have half cap. " +
                planetsToHaveFullTurretCaps + " actual planets to have full cap. " );

            ThrowawayListCanMemLeak<Planet>[] planetsByTierOfInterest = new ThrowawayListCanMemLeak<Planet>[6];

            #region Sort Into Tiers Of Interest
            for ( int i = 0; i < planetsByTierOfInterest.Length; i++ )
                planetsByTierOfInterest[i] = new ThrowawayListCanMemLeak<Planet>( 500 );

            foreach ( Planet p in totalPlanetsOfThisFactionForSeeding )
            {
                int tierOfInterest = 5;
                if ( p.MapGen_NumberOfBigGoodThingsHere > 1 )
                    tierOfInterest = 0;
                else if ( p.MapGen_NumberOfBigBadThingsHere > 0 && ( p.MapGen_NumberOfSmallGoodThingsHere + p.MapGen_NumberOfBigGoodThingsHere ) > 1 )
                    tierOfInterest = 1;
                else if ( ( p.MapGen_NumberOfBigBadThingsHere + p.MapGen_NumberOfBigGoodThingsHere ) > 1 )
                    tierOfInterest = 2;
                else if ( ( p.MapGen_NumberOfSmallGoodThingsHere + p.MapGen_NumberOfSmallBadThingsHere + p.MapGen_NumberOfBigGoodThingsHere) > 1 )
                    tierOfInterest = 3;
                else if ( p.MapGen_NumberOfBigGoodThingsHere > 0 )
                    tierOfInterest = 4;

                planetsByTierOfInterest[tierOfInterest].Add( p );
            }

            MapgenLogger.Log( totalPlanetsOfThisFactionForSeeding.Count + " planets on which to set turret status. By tier of interest: " +
                " Tier 0: " + planetsByTierOfInterest[0].Count +
                " Tier 1: " + planetsByTierOfInterest[1].Count +
                " Tier 2: " + planetsByTierOfInterest[2].Count +
                " Tier 3: " + planetsByTierOfInterest[3].Count +
                " Tier 4: " + planetsByTierOfInterest[4].Count +
                " Tier 5: " + planetsByTierOfInterest[5].Count );
            #endregion

            //seed those of the full cap
            Helper_SetTurretAllowancesOnPlanetsFromList( planetsByTierOfInterest, planetsToHaveFullTurretCaps, 
                PlanetAITurretAnyNonTurretDefensesCap.FullCap, Context );
            //seed those of the half cap
            Helper_SetTurretAllowancesOnPlanetsFromList( planetsByTierOfInterest, planetsToHaveHalfTurretCaps,
                PlanetAITurretAnyNonTurretDefensesCap.HalfCap, Context );
        }

        private void Helper_SetTurretAllowancesOnPlanetsFromList( IList<Planet>[] planetsByTierOfInterest, int PlanetsToSeed, 
            PlanetAITurretAnyNonTurretDefensesCap CapType, ArcenHostOnlySimContext Context )
        {
            if ( PlanetsToSeed <= 0 )
                return;

            for ( int seedCounter = 0; seedCounter < PlanetsToSeed; seedCounter++ )
            {
                for ( int tier = 0; tier < planetsByTierOfInterest.Length; tier++ )
                {
                    IList<Planet> planetsAtTier = planetsByTierOfInterest[tier];
                    if ( planetsAtTier.Count <= 0 )
                        continue;
                    if ( planetsAtTier.Count > 1 )
                    {
                        int indexToUse = Context.RandomToUse.Next( 0, planetsAtTier.Count );
                        planetsAtTier[indexToUse].PlanetAITurretSeedingCap = CapType;
                        MapgenLogger.Log( planetsAtTier[indexToUse].Name + " at interest tier " + tier +
                            " set to " + CapType );
                        planetsAtTier.RemoveAt( indexToUse );
                    }
                    else
                    {
                        planetsAtTier[0].PlanetAITurretSeedingCap = CapType;
                        MapgenLogger.Log( planetsAtTier[0].Name + " at interest tier " + tier +
                            " set to " + CapType );
                        planetsAtTier.Clear();
                    }
                    break; //we seeded something, so move to the next one to seed
                }
            }
        }

        #region Helper_MapgenLogger_PlanetsSeeder
        private static void Helper_MapgenLogger_PlanetsSeeder( IList<Planet> planetsSeeded, string NameForCategory, int morePlanetsToSeed )
        {
            if ( MapgenLogger.IsActive )
            {
                if ( planetsSeeded == null )
                {
                    MapgenLogger.Log( "*********" + NameForCategory + "********* planetsSeeded: NULL                            morePlanetsToSeed: " + morePlanetsToSeed );
                    return;
                }

                string planetList = string.Empty;
                for ( int i = 0; i < planetsSeeded.Count; i++ )
                {
                    if ( planetList.Length > 0 )
                        planetList += ", ";
                    planetList += planetsSeeded[i].Name;
                }
                MapgenLogger.Log( "*********" + NameForCategory + "********* planetsSeeded: " + planetsSeeded.Count + " (" + planetList + ")                            morePlanetsToSeed: " + morePlanetsToSeed );
            }
        }
        #endregion

        public static int GetClosestDistanceFromPlayerToAIHomeworld( IList<Planet> planets )
        {
            int closestDistance = 9999;
            for ( int i = 0; i < planets.Count; i++ )
            {
                Planet planet = planets[i];
                if ( planet.OriginalHopsToHumanHomeworld + planet.OriginalHopsToAIHomeworld < closestDistance )
                    closestDistance = planet.OriginalHopsToHumanHomeworld + planet.OriginalHopsToAIHomeworld;
            }
            return closestDistance;
        }

        public static void Mapgen_RemoveEntitiesIfNotEnclosingAnyPockets( ArcenHostOnlySimContext Context, Galaxy galaxy, int SkipUpToThisFarFromHumanHomeworld, EntityRollupType entityRollup, GameEntityTypeData entityData )
        {
            bool debugging = false;
            ArcenCharacterBuffer debugBuffer = null;
            if ( debugging ) debugBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "StandardMapPop-Mapgen_RemoveEntitiesIfNotEnclosingAnyPockets-trace", 10f );
            if ( debugging ) debugBuffer.Add( "Mapgen_RemoveEntitiesIfNotEnclosingAnyPockets" );
            foreach ( Planet planet in galaxy.Planets( false ) )
            {
                if ( !planet.GetHasAtLeast( entityRollup, 1, null, false, false ) )
                    continue;
                if ( planet.OriginalHopsToHumanHomeworld <= SkipUpToThisFarFromHumanHomeworld )
                    continue;
                ThrowawayListCanMemLeak<Planet> unblockedNeighbors = new ThrowawayListCanMemLeak<Planet>( 500 );
                foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                {
                    if ( neighbor.GetHasAtLeast( entityRollup, 1, null, false, false ) )
                        continue;
                    unblockedNeighbors.Add( neighbor );
                }
                bool foundThatIAmEnclosingAPocket = false;
                if ( unblockedNeighbors.Count <= 1 ) // if it's 1, the answer is automatically that this is not enclosing a pocket
                {
                    if ( debugging ) debugBuffer.Add( "\n" ).Add( "Removing scrambler from " ).Add( planet.Name ).Add( " because only " ).Add( unblockedNeighbors.Count ).Add( " unblocked neighbors" );
                }
                else
                {
                    if ( debugging )
                    {
                        debugBuffer.Add( "\n" ).Add( "Checking for removing scrambler from " );
                        debugBuffer.Add( planet.Name );
                        debugBuffer.Add( " with " ).Add( unblockedNeighbors.Count ).Add( " unblocked neighbors:" );
                    }
                    for ( int j = 0; j < unblockedNeighbors.Count; j++ )
                    {
                        Planet neighbor = unblockedNeighbors[j];
                        if ( debugging ) debugBuffer.Add( "\n\t" ).Add( neighbor.Name ).Add( " Can Reach " );
                        ThrowawayListCanMemLeak<Planet> planetsIAmTryingToReach = new ThrowawayListCanMemLeak<Planet>( 500 );
                        planetsIAmTryingToReach.AddRange( unblockedNeighbors );
                        planetsIAmTryingToReach.Remove( neighbor );
                        bool isFirst = false;
                        foreach ( Planet.PlanetAtHopDistance _phd in neighbor.PlanetsWithinXHops( -1,
                            delegate ( Planet otherPlanet )
                            {
                                if ( !unblockedNeighbors.Contains( otherPlanet ) )
                                    return PropogationEvaluation.No;
                                return PropogationEvaluation.Yes;
                            } ) )
                        {
                            Planet otherPlanet = _phd.Planet;
                            if ( planetsIAmTryingToReach.Contains( otherPlanet ) )
                            {
                                if ( isFirst )
                                    isFirst = false;
                                else if ( debugging )
                                    debugBuffer.Add( "," );
                                if ( debugging ) debugBuffer.Add( otherPlanet.Name );
                                planetsIAmTryingToReach.Remove( otherPlanet );
                                if ( planetsIAmTryingToReach.Count <= 0 )
                                    break;
                            }
                        }
                        if ( planetsIAmTryingToReach.Count > 0 )
                        {
                            foundThatIAmEnclosingAPocket = true;
                            if ( debugging ) debugBuffer.Add( " could not reach all!" );
                            break;
                        }
                    }
                }
                if ( debugging ) debugBuffer.Add( "\n\t***Result= " );
                if ( foundThatIAmEnclosingAPocket )
                {
                    if ( debugging ) debugBuffer.Add( "Not Removed" );
                    continue;
                }
                if ( debugging ) debugBuffer.Add( "Removed" );
                planet.Mapgen_ImmediatelyRemoveAllTheseFromSim( entityRollup, entityData, InstancedRendererDeactivationReason.MapGen_NotEnclosingAnyPockets );
            }
            if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( debugBuffer.ToStringAndReturnToPool(), Verbosity.DoNotShow );
        }

        /// <summary>
        /// returns true if it removes a unit from some planet
        /// </summary>
        public static bool Mapgen_ExpandSmallestPocketNotContainingThis( ArcenHostOnlySimContext Context, Galaxy galaxy, GameEntityTypeData entityData, EntityRollupType entityRollup, int SkipUpToThisFarFromHumanHomeworld, int AllowablePocketSize )
        {
            Planet bestPlanet = null;
            int bestPocketSize = -1;
            foreach ( Planet planet in galaxy.Planets( false ) )
            {
                if ( !planet.GetHasAtLeast( entityRollup, 1, null, false, false ) )
                    continue;
                if ( planet.OriginalHopsToHumanHomeworld <= SkipUpToThisFarFromHumanHomeworld )
                    continue;
                int resultingPocketSize = 0;
                foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops( -1,
                    delegate ( Planet otherPlanet )
                    {
                        if ( otherPlanet == planet )
                            return PropogationEvaluation.Yes;
                        if ( otherPlanet.GetHasAtLeast( entityRollup, 1, null, false, false ) )
                            return PropogationEvaluation.No;
                        return PropogationEvaluation.Yes;
                    } ) )
                {
                    resultingPocketSize++;
                }
                if ( bestPlanet != null && bestPocketSize >= resultingPocketSize )
                    continue;
                bestPlanet = planet;
                bestPocketSize = resultingPocketSize;
            }
            if ( bestPlanet == null || bestPocketSize > AllowablePocketSize )
                return false;
            bestPlanet.Mapgen_ImmediatelyRemoveAllTheseFromSim( entityRollup, entityData, InstancedRendererDeactivationReason.Mapgen_ExpandSmallestPocketNotContainingThis );
            return true;
        }

        /// <summary>
        /// returns true if it adds a unit to some planet
        /// </summary>
        public static bool Mapgen_SplitUpBiggestPocketNotContainingThis( ArcenHostOnlySimContext Context, Galaxy galaxy, GameEntityTypeData entityData, EntityRollupType entityRollup, int SkipUpToThisFarFromHumanHomeworld, int AllowablePocketSize )
        {
            ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<Planet>> emptyPockets = new ThrowawayListCanMemLeak<ThrowawayListCanMemLeak<Planet>>( 300 );
            foreach ( Planet planet in galaxy.Planets( false ) )
            {
                if ( planet.GetHasAtLeast( entityRollup, 1, null, false, false ) )
                    continue;
                if ( planet.OriginalHopsToHumanHomeworld <= SkipUpToThisFarFromHumanHomeworld )
                    continue;
                bool foundBlock = false;
                for ( int j = 0; j < emptyPockets.Count; j++ )
                {
                    if ( !emptyPockets[j].Contains( planet ) )
                        continue;
                    foundBlock = true;
                    break;
                }
                if ( foundBlock )
                    continue;
                ThrowawayListCanMemLeak<Planet> newPocket = new ThrowawayListCanMemLeak<Planet>( 500 );
                emptyPockets.Add( newPocket );
                newPocket.Add( planet );
                foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops( -1,
                    delegate ( Planet otherPlanet )
                    {
                        if ( otherPlanet.GetHasAtLeast( entityRollup, 1, null, false, false ) )
                            return PropogationEvaluation.No;
                        return PropogationEvaluation.Yes;
                    } ) )
                {
                    Planet otherPlanet = _phd.Planet;
                    if ( newPocket.Contains( otherPlanet ) )
                        continue;
                    newPocket.Add( otherPlanet );
                }
            }
            IList<Planet> largestPocket = null;
            for ( int i = 0; i < emptyPockets.Count; i++ )
            {
                IList<Planet> pocket = emptyPockets[i];
                if ( largestPocket != null && largestPocket.Count >= pocket.Count )
                    continue;
                largestPocket = pocket;
            }
            if ( largestPocket == null || largestPocket.Count <= AllowablePocketSize )
                return false;
            Planet planetFurthestFromScrambler = null;
            int furthestDistance = -1;
            for ( int i = 0; i < largestPocket.Count; i++ )
            {
                Planet planet = largestPocket[i];
                int thisPlanetDistance = -1;
                foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops( -1,
                    delegate ( Planet otherPlanet )
                    {
                        if ( otherPlanet.GetHasAtLeast( entityRollup, 1, null, false, false ) )
                            return PropogationEvaluation.SelfButNotNeighbors;
                        return PropogationEvaluation.Yes;
                    } ) )
                {
                    Planet otherPlanet = _phd.Planet;
                    Int16 distance = _phd.Hops;
                    if ( thisPlanetDistance != -1 && thisPlanetDistance <= distance )
                        continue;
                    if ( otherPlanet.GetHasAtLeast( entityRollup, 1, null, false, false ) )
                        thisPlanetDistance = distance;
                }
                if ( planetFurthestFromScrambler != null && furthestDistance >= thisPlanetDistance )
                    continue;
                planetFurthestFromScrambler = planet;
                furthestDistance = thisPlanetDistance;
            }
            if ( planetFurthestFromScrambler == null )
                return false;
            planetFurthestFromScrambler.Mapgen_SeedAIEntity( Context, entityData, PlanetSeedingZone.InnerSystem );
            return true;
        }

        public static ThrowawayListCanMemLeak<Planet> Mapgen_SeedSpecialEntities( ArcenHostOnlySimContext Context, Galaxy galaxy, SpecialEntityType SpecialTypeOrNone, string TagOrEmpty, SeedingType SeedType, int Count,
            MapGenCountPerPlanet CountPer, MapGenSeedStyle SeedStyle, int minDistanceFromHumanHomeworld, int minDistanceFromAIHomeworld, PlanetSeedingZone SeedingZone, SeedingExpansionType ExpansionStyle,
            Faction SpecificFactionToTryToBeCloseTo, Int16 SpecificFactionTryToBeAtMost )
        {
            return Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialTypeOrNone, TagOrEmpty, SeedType, Count, 
                CountPer, SeedStyle, minDistanceFromHumanHomeworld, 999, minDistanceFromAIHomeworld, 999, SeedingZone, ExpansionStyle, SpecificFactionToTryToBeCloseTo, SpecificFactionTryToBeAtMost, null );
        }

        public static ThrowawayListCanMemLeak<Planet> Mapgen_SeedSpecialEntities( ArcenHostOnlySimContext Context, Galaxy galaxy, SpecialEntityType SpecialTypeOrNone, string TagOrEmpty, SeedingType SeedType, int Count,
            MapGenCountPerPlanet CountPer, MapGenSeedStyle SeedStyle, int minDistanceFromHumanHomeworld, int minDistanceFromAIHomeworld, PlanetSeedingZone SeedingZone, SeedingExpansionType ExpansionStyle )
        {
            return Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialTypeOrNone, TagOrEmpty, SeedType, Count,
                CountPer, SeedStyle, minDistanceFromHumanHomeworld, 999, minDistanceFromAIHomeworld, 999, SeedingZone, ExpansionStyle, null, -1, null );
        }

        public static ThrowawayListCanMemLeak<Planet> Mapgen_SeedSpecialEntities( ArcenHostOnlySimContext Context, Galaxy galaxy, Faction Faction, SpecialEntityType SpecialTypeOrNone, string TagOrEmpty, SeedingType SeedType, int Count,
            MapGenCountPerPlanet CountPer, MapGenSeedStyle SeedStyle, int minDistanceFromHumanHomeworld, int minDistanceFromAIHomeworld, PlanetSeedingZone SeedingZone, SeedingExpansionType ExpansionStyle,
            Faction SpecificFactionToTryToBeCloseTo, Int16 SpecificFactionTryToBeAtMost )
        {
            return Mapgen_SeedSpecialEntities( Context, galaxy, Faction, FactionType.NaturalObject, SpecialTypeOrNone, TagOrEmpty, SeedType, Count, 
                CountPer, SeedStyle, minDistanceFromHumanHomeworld, 999, minDistanceFromAIHomeworld, 999, SeedingZone, ExpansionStyle, SpecificFactionToTryToBeCloseTo, SpecificFactionTryToBeAtMost, null );
        }

        public static ThrowawayListCanMemLeak<Planet> Mapgen_SeedSpecialEntities( ArcenHostOnlySimContext Context, Galaxy galaxy, Faction Faction, SpecialEntityType SpecialTypeOrNone, string TagOrEmpty, SeedingType SeedType, int Count,
            MapGenCountPerPlanet CountPer, MapGenSeedStyle SeedStyle, int minDistanceFromHumanHomeworld, int minDistanceFromAIHomeworld, PlanetSeedingZone SeedingZone, SeedingExpansionType ExpansionStyle )
        {
            return Mapgen_SeedSpecialEntities( Context, galaxy, Faction, FactionType.NaturalObject, SpecialTypeOrNone, TagOrEmpty, SeedType, Count,
                CountPer, SeedStyle, minDistanceFromHumanHomeworld, 999, minDistanceFromAIHomeworld, 999, SeedingZone, ExpansionStyle, null, -1, null );
        }

        public static ThrowawayListCanMemLeak<Planet> Mapgen_SeedSpecialEntities( ArcenHostOnlySimContext Context, Galaxy galaxy, FactionType factionType, SpecialEntityType SpecialTypeOrNone, string TagOrEmpty, SeedingType SeedType, int Count,
            MapGenCountPerPlanet CountPer, MapGenSeedStyle SeedStyle, int minDistanceFromHumanHomeworld, int minDistanceFromAIHomeworld, PlanetSeedingZone SeedingZone, SeedingExpansionType ExpansionStyle,
            Faction SpecificFactionToTryToBeCloseTo, Int16 SpecificFactionTryToBeAtMost )
        {
            return Mapgen_SeedSpecialEntities( Context, galaxy, null, factionType, SpecialTypeOrNone, TagOrEmpty, SeedType, Count, 
                CountPer, SeedStyle, minDistanceFromHumanHomeworld, 999, minDistanceFromAIHomeworld, 999, SeedingZone, ExpansionStyle, SpecificFactionToTryToBeCloseTo, SpecificFactionTryToBeAtMost, null );
        }

        public static ThrowawayListCanMemLeak<Planet> Mapgen_SeedSpecialEntities( ArcenHostOnlySimContext Context, Galaxy galaxy, FactionType factionType, SpecialEntityType SpecialTypeOrNone, string TagOrEmpty, SeedingType SeedType, int Count,
            MapGenCountPerPlanet CountPer, MapGenSeedStyle SeedStyle, int minDistanceFromHumanHomeworld, int minDistanceFromAIHomeworld, PlanetSeedingZone SeedingZone, SeedingExpansionType ExpansionStyle )
        {
            return Mapgen_SeedSpecialEntities( Context, galaxy, null, factionType, SpecialTypeOrNone, TagOrEmpty, SeedType, Count,
                CountPer, SeedStyle, minDistanceFromHumanHomeworld, 999, minDistanceFromAIHomeworld, 999, SeedingZone, ExpansionStyle, null, -1, null );
        }

        public static ThrowawayListCanMemLeak<Planet> Mapgen_SeedSpecialEntities( ArcenHostOnlySimContext Context, Galaxy galaxy, Faction FactionOrNull, FactionType factionType,
            SpecialEntityType SpecialTypeOrNone, string TagOrEmpty, SeedingType SeedType, int Count, MapGenCountPerPlanet CountPer, MapGenSeedStyle SeedStyle,
            int minDistanceFromHumanHomeworld, int maxDistanceFromHumanHomeworld,
            int minDistanceFromAIHomeworld, int maxDistanceFromAIHomeworld, PlanetSeedingZone SeedingZone, SeedingExpansionType ExpansionStyle )
        { 
            return Mapgen_SeedSpecialEntities( Context, galaxy, FactionOrNull, factionType, SpecialTypeOrNone, TagOrEmpty, SeedType, Count,
                CountPer, SeedStyle, minDistanceFromHumanHomeworld, maxDistanceFromHumanHomeworld, minDistanceFromAIHomeworld, maxDistanceFromAIHomeworld, SeedingZone, ExpansionStyle, null, -1, null );
        }

        public static ThrowawayListCanMemLeak<Planet> Mapgen_SeedSpecialEntities( ArcenHostOnlySimContext Context, Galaxy galaxy, Faction FactionOrNull, FactionType factionType,
            SpecialEntityType SpecialTypeOrNone, string TagOrEmpty, SeedingType SeedType, int Count, MapGenCountPerPlanet CountPer, MapGenSeedStyle SeedStyle,
            int minDistanceFromHumanHomeworld, int maxDistanceFromHumanHomeworld,
            int minDistanceFromAIHomeworld, int maxDistanceFromAIHomeworld, PlanetSeedingZone SeedingZone, SeedingExpansionType ExpansionStyle, 
            Faction SpecificFactionToTryToBeCloseTo, Int16 SpecificFactionTryToBeAtMost )
        {
            return Mapgen_SeedSpecialEntities( Context, galaxy, FactionOrNull, factionType, SpecialTypeOrNone, TagOrEmpty, SeedType, Count,
                CountPer, SeedStyle, minDistanceFromHumanHomeworld, maxDistanceFromHumanHomeworld, minDistanceFromAIHomeworld, maxDistanceFromAIHomeworld, SeedingZone, ExpansionStyle, SpecificFactionToTryToBeCloseTo, SpecificFactionTryToBeAtMost, null );
        }

        /// <summary>
        /// returns a list of planets that the unit(s) were seeded on
        /// </summary>
        public static ThrowawayListCanMemLeak<Planet> Mapgen_SeedSpecialEntities( ArcenHostOnlySimContext Context, Galaxy galaxy, Faction FactionOrNull, FactionType factionType,
            SpecialEntityType SpecialTypeOrNone, string TagOrEmpty, SeedingType SeedType, int Count, MapGenCountPerPlanet CountPer, MapGenSeedStyle SeedStyle,
            int minDistanceFromHumanHomeworld, int maxDistanceFromHumanHomeworld,
            int minDistanceFromAIHomeworld, int maxDistanceFromAIHomeworld, PlanetSeedingZone SeedingZone, SeedingExpansionType ExpansionStyle, 
            Faction SpecificFactionToTryToBeCloseTo, Int16 SpecificFactionTryToBeAtMost, GetSeedingWeight GetWeight )
        {
            var planetsSeeded = new ThrowawayListCanMemLeak<Planet>();
            
            var args = new SeedArgs()
                {
                    FactionOrNull = FactionOrNull, 
                    FactionType = factionType,
                    SpecialTypeOrNone = SpecialTypeOrNone,
                    TagOrEmpty = TagOrEmpty, 
                    SeedType = SeedType, 
                    Count = Count, 
                    CountPer = CountPer, 
                    SeedStyle = SeedStyle,
                    MinDistanceFromHumanHomeworld = minDistanceFromHumanHomeworld, 
                    MaxDistanceFromHumanHomeworld = maxDistanceFromHumanHomeworld,
                    MinDistanceFromAIHomeworld = minDistanceFromAIHomeworld, 
                    MaxDistanceFromAIHomeworld = maxDistanceFromAIHomeworld, 
                    SeedingZone = SeedingZone, 
                    ExpansionStyle = ExpansionStyle, 
                    SpecificFactionToTryToBeCloseTo = SpecificFactionToTryToBeCloseTo, 
                    SpecificFactionTryToBeAtMost = SpecificFactionTryToBeAtMost, 
                    GetWeight = GetWeight, 
                    SeededPlanets = planetsSeeded,
                };
                                
            Mapgen_SeedSpecialEntities(Context, galaxy, args);
            
            return planetsSeeded;
        }
        
        public static void Mapgen_SeedSpecialEntities( ArcenHostOnlySimContext Context, Galaxy Galaxy, SeedArgs Args )
        {
            var link = ExternalDeepLinkTable.Instance.GetRowByName("SeedEntityAlgorithmLink");
            if (link != null)
            {
                var alg = link.Singleton as SeedEntityAlgorithm;
                if (alg != null && alg.Enabled)
                {
                    alg.Seed( Context, Galaxy, Args );
                    return;
                }
            }

            var galaxy = Galaxy;
            var FactionOrNull = Args.FactionOrNull;
            var factionType = Args.FactionType;
            var SpecialTypeOrNone = Args.SpecialTypeOrNone;
            var TagOrEmpty = Args.TagOrEmpty;
            var SeedType = Args.SeedType; 
            var Count = Args.Count; 
            var CountPer = Args.CountPer; 
            var SeedStyle = Args.SeedStyle;
            var minDistanceFromHumanHomeworld = Args.MinDistanceFromHumanHomeworld;
            var maxDistanceFromHumanHomeworld = Args.MaxDistanceFromHumanHomeworld;
            var minDistanceFromAIHomeworld = Args.MinDistanceFromAIHomeworld; 
            var maxDistanceFromAIHomeworld = Args.MaxDistanceFromAIHomeworld; 
            var SeedingZone = Args.SeedingZone; 
            var ExpansionStyle = Args.ExpansionStyle; 
            var SpecificFactionToTryToBeCloseTo = Args.SpecificFactionToTryToBeCloseTo; 
            var SpecificFactionTryToBeAtMost = Args.SpecificFactionTryToBeAtMost; 
            var GetWeight = Args.GetWeight; 
            var Callback = Args.Callback; 
            var GetHasSimilar = Args.GetHasSimilar;
            var seededPlanets = Args.SeededPlanets;
            var seededEntities = Args.SeededEntities;

            //cap these two, so that minimum restrictions don't put these things on those.
            if ( minDistanceFromHumanHomeworld < 1 )
                minDistanceFromHumanHomeworld = 1;
            if ( minDistanceFromAIHomeworld < 1 )
                minDistanceFromAIHomeworld = 1;

            //there are cases where we just don't have any factions yet, and that's ok!  Don't seed anything here, then.
            if ( World_AIW2.Instance.Factions.Count == 0 )
                return;
            //if not seeding the details yet, then... skip all this!
            if ( !World_AIW2.Instance.Setup.ShouldSeedDetailsYet )
                return;

            ThrowawayListCanMemLeak<Planet>  potentialPlanets = new ThrowawayListCanMemLeak<Planet>( 500 );

            IList<GameEntityTypeData> eligibleEntityDatas = null;
            if ( !string.IsNullOrEmpty(TagOrEmpty) )
                eligibleEntityDatas = GameEntityTypeDataTable.Instance.RowsByTag[TagOrEmpty];
            if ( SpecialTypeOrNone != SpecialEntityType.None && SpecialTypeOrNone != SpecialEntityType.Length )
                eligibleEntityDatas = GameEntityTypeDataTable.Instance.RowsBySpecialType[SpecialTypeOrNone];

            if ( eligibleEntityDatas == null || eligibleEntityDatas.Count <= 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Harmless warning: Mapgen_SeedSpecialEntities called with tag '" + (TagOrEmpty == null ? "null" : TagOrEmpty) +
                    "' and SpecialEntityType '" + SpecialTypeOrNone + "' but none of those exist.  This may be entirely be design, based on mods or expansions.", Verbosity.DoNotShow );
                return;
            }

            bool isItOkayIfAllPlanetsNextToHumanHomeworld = maxDistanceFromAIHomeworld <= 1;

            #region Fill potentialPlanets based on distance and MapGenSeedStyle
            int matchingSeedTypeCounter = 0;
            Planet tryToSeedVeryNearSpecificPlanet = null;

            #region Find tryToSeedVeryNearSpecificPlanet based off of the SpecificFactionToTryToBeCloseTo
            if ( SpecificFactionToTryToBeCloseTo != null )
            {
                foreach ( Planet plan in galaxy.Planets( false ) )
                {
                    if ( plan.GetControllingFaction() == SpecificFactionToTryToBeCloseTo )
                    {
                        if ( tryToSeedVeryNearSpecificPlanet == null )
                            tryToSeedVeryNearSpecificPlanet = plan;
                        if ( plan.PopulationType == PlanetPopulationType.HumanHomeworld ||
                             plan.PopulationType == PlanetPopulationType.AIHomeworld ) //ignore AIBastionWorld
                        {
                            tryToSeedVeryNearSpecificPlanet = plan;
                            break;
                        }
                    }
                }
                
                if ( tryToSeedVeryNearSpecificPlanet == null )
                {
                    foreach ( Planet plan in galaxy.Planets( false ) )
                    {
                        if ( plan.GetControllingFaction() == SpecificFactionToTryToBeCloseTo )
                        {
                            tryToSeedVeryNearSpecificPlanet = plan;
                            break;
                        }
                    }
                }
                
                if ( tryToSeedVeryNearSpecificPlanet == null && 
                     SpecificFactionToTryToBeCloseTo.Type == FactionType.Player )
                {
                    try
                    {
                        var planetIdx = World_AIW2.Instance.Setup.GetConfigurationForFaction(SpecificFactionToTryToBeCloseTo.FactionIndex)?.StartingIndex??-1;
                        var plan = galaxy.GetPlanetByIndex( planetIdx );
                        if ( plan != null && ( plan.PopulationType == PlanetPopulationType.HumanHomeworld || plan.PopulationType == PlanetPopulationType.ArkEmpireHumanHomeworld ) )
                            tryToSeedVeryNearSpecificPlanet = plan;
                        else
                        {
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( 
                                "Please correct the configuration so that the faction: " + SpecificFactionToTryToBeCloseTo.GetDisplayName()
                                + " is sharing the homeworld of an empire-style human player.   If this is a 'sidekick' style faction, it must be on the same starting planet as an empire-style faction." +
                                "\n\nIf you want to play this faction more independently, please choose the 'empire' style version of this, if there is one." );
                            
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( 
                            string.Format("Exception in {0}() trying to seed near {1}.\n{2}", LOG.MethodName(), SpecificFactionToTryToBeCloseTo.OrNull(), e));
                        
                        return;
                    }
                }
            }
            #endregion

            PlanetSeederHelper seederHelper = new PlanetSeederHelper();
            seederHelper.potentialPlanets = potentialPlanets;
            seederHelper.SeedStyle = SeedStyle;
            seederHelper.FactionOrNull = FactionOrNull;
            seederHelper.minDistanceFromAIHomeworld = minDistanceFromAIHomeworld;
            seederHelper.maxDistanceFromAIHomeworld = maxDistanceFromAIHomeworld;
            seederHelper.minDistanceFromHumanHomeworld = minDistanceFromHumanHomeworld;
            seederHelper.maxDistanceFromHumanHomeworld = maxDistanceFromHumanHomeworld;
            seederHelper.Context = Context;
            seederHelper.hopsToAvoidWhenSeedingMultipleFactionCopies = 10;
            seederHelper.maxMatchingSeedTypes = 0;

            for ( int loopCount = 0; loopCount < 1000; loopCount++ )
            {
                if ( SeedStyle == MapGenSeedStyle.FullUseByFactionOnNomadIfPossible && loopCount > 400 )
                    SeedStyle = MapGenSeedStyle.FullUseByFaction; //if there aren't enough nomads, just use the regular style

                if ( tryToSeedVeryNearSpecificPlanet != null && loopCount < 10 )
                {
                    foreach ( Planet.PlanetAtHopDistance _phd in tryToSeedVeryNearSpecificPlanet.PlanetsWithinXHops_NoFilters( SpecificFactionTryToBeAtMost ) )
                    {
                        if ( seederHelper.Helper_DistanceBasePlanetLoop( _phd.Planet, _phd.Hops ) == DelReturn.Break )
                            break;
                    }
                }
                else
                    foreach ( Planet planet in galaxy.Planets( false ) )
                        if ( seederHelper.Helper_PlanetLoop( planet ) == DelReturn.Break )
                            break;

                matchingSeedTypeCounter++;
                if ( matchingSeedTypeCounter > 5 )
                {
                    matchingSeedTypeCounter = 0;
                    seederHelper.maxMatchingSeedTypes++;
                }

                if ( potentialPlanets.Count <= 0 )
                {
                    bool outerBreak = false;
                    if ( seederHelper.hopsToAvoidWhenSeedingMultipleFactionCopies >= 3 )
                        seederHelper.hopsToAvoidWhenSeedingMultipleFactionCopies--; //this starts high but can decrease to a min of 2
                    switch ( ExpansionStyle )
                    {
                        case SeedingExpansionType.ComplicatedOriginal:
                            {
                                if ( minDistanceFromHumanHomeworld <= 1 && minDistanceFromAIHomeworld <= 1 )
                                {
                                    //too close!
                                    outerBreak = true;
                                    break;
                                }
                                if ( minDistanceFromHumanHomeworld <= 1 )
                                {
                                    if ( minDistanceFromHumanHomeworld < maxDistanceFromAIHomeworld )
                                        minDistanceFromHumanHomeworld++;
                                    else
                                        maxDistanceFromAIHomeworld++;
                                }
                                else
                                {
                                    if ( minDistanceFromHumanHomeworld > minDistanceFromAIHomeworld )
                                        minDistanceFromHumanHomeworld--;
                                    else
                                        minDistanceFromAIHomeworld--;
                                }
                            }
                            break;
                        case SeedingExpansionType.ExpandPlayerMaxOnly:
                            maxDistanceFromHumanHomeworld++;
                            break;
                        case SeedingExpansionType.DoNoExpansion:
                            //nothing to expand!
                            outerBreak = true;
                            break;
                    }
                    if ( outerBreak )
                        break;
                    continue;
                }
                break;
            }
            #endregion

            if ( !isItOkayIfAllPlanetsNextToHumanHomeworld )
            {
                #region If all the planets are right next to the human homeworld, something went wrong; use the other method, then
                bool foundAPlanetGreaterThanOne = false;
                for ( int i = 0; i < potentialPlanets.Count; i++ )
                {
                    Planet planet = potentialPlanets[i];
                    if ( planet.OriginalHopsToHumanHomeworld > 1 )
                    {
                        foundAPlanetGreaterThanOne = true;
                        break;
                    }
                }
                if ( !foundAPlanetGreaterThanOne )
                    potentialPlanets.Clear();
                #endregion
            }

            #region If failed to get potential planets based on distance, try not via distance
            if ( potentialPlanets.Count <= 0 )
            {
                for ( int i = 0; i < 10; i++ )
                {
                    //this retries a few times to allow hopsToAvoidWhenSeedingMultipleFactionCopies to count down
                    foreach ( Planet planet in galaxy.Planets( false ) )
                    {
                        if ( planet.PopulationType == PlanetPopulationType.AIHomeworld || planet.PopulationType == PlanetPopulationType.HumanHomeworld ) //ignore AIBastionWorld
                            continue;
                        if ( planet.MapGen_IsFullyUsedByAFaction )
                            continue; //blocked
                        bool skipForDistance = false;

                        foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops_NoFilters( seederHelper.hopsToAvoidWhenSeedingMultipleFactionCopies ) )
                        {
                            Planet neighbor = _phd.Planet;
                            Int16 Distance = _phd.Hops;
                            if ( FactionOrNull == null )
                                break;
                            if ( Distance > seederHelper.hopsToAvoidWhenSeedingMultipleFactionCopies )
                                continue;
                            if ( neighbor.MapGen_IsFullyUsedByAFaction &&
                                 neighbor.MapGen_FullyUsingFaction != FactionOrNull &&
                                 neighbor.MapGen_FullyUsingFaction.GetDisplayName() == FactionOrNull.GetDisplayName() )
                            {
                                skipForDistance = true;
                                break;
                            }
                        }
                        if ( skipForDistance )
                            continue;

                        potentialPlanets.Add( planet );
                    }
                    if ( potentialPlanets.Count > 0 )
                        break;
                    if ( seederHelper.hopsToAvoidWhenSeedingMultipleFactionCopies >= 3 )
                        seederHelper.hopsToAvoidWhenSeedingMultipleFactionCopies--;
                }
            }

            if ( potentialPlanets.Count <= 0 )
            {
                if ( World_AIW2.Instance.GetIsTutorial() )
                    return; //in tutorials, we don't care

                if ( SeedStyle == MapGenSeedStyle.FullUseByFaction && FactionOrNull != null )
                {
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Galaxy has too few planets for seeding this many factions!  There's not enough room to spread them out." );
                }

                ArcenCharacterBuffer debugBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "StandardMapPop-Mapgen_SeedSpecialEntities-trace", 10f);
                debugBuffer.Add( "Failed to find planet to seed tag '" ).Add( TagOrEmpty == null ? "null" : TagOrEmpty ).Add( "' / SpecialEntityType '" ).Add( SpecialTypeOrNone.ToString() ).Add( "'" );
                debugBuffer.Add( "\n" ).Add( "Count" ).Add( "=" ).Add( Count );
                debugBuffer.Add( "\n" ).Add( "GalaxyPlanetCount" ).Add( "=" ).Add( galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() );

                int AIHomeworldCount = 0;
                int HumanHomeworldCount = 0;
                int BlockedPlanetCount = 0;
                int OpenPlanetCount = 0;
                StringBuilder blockedPlanetsBy = new StringBuilder();
                foreach ( Planet planet in galaxy.Planets( false ) )
                {
                    if ( planet.PopulationType == PlanetPopulationType.AIHomeworld ) //ignore AIBastionWorld
                    {
                        AIHomeworldCount++;
                        continue;
                    }
                    if ( planet.PopulationType == PlanetPopulationType.HumanHomeworld )
                    {
                        HumanHomeworldCount++;
                        continue;
                    }
                    if ( planet.MapGen_IsFullyUsedByAFaction )
                    {
                        BlockedPlanetCount++;
                        if ( blockedPlanetsBy.Length > 0 )
                            blockedPlanetsBy.Append( ", " ); 
                        else
                            blockedPlanetsBy.Append( planet.MapGen_MajorFactionsBlockedHereReason == null ? "NULL" : planet.MapGen_MajorFactionsBlockedHereReason );
                        continue;
                    }
                    OpenPlanetCount++;
                }

                debugBuffer.Add( "\n" ).Add( "AIHomeworldCount" ).Add( "=" ).Add( AIHomeworldCount );
                debugBuffer.Add( "\n" ).Add( "HumanHomeworldCount" ).Add( "=" ).Add( HumanHomeworldCount );
                debugBuffer.Add( "\n" ).Add( "OpenPlanetCount" ).Add( "=" ).Add( OpenPlanetCount );
                debugBuffer.Add( "\n" ).Add( "BlockedPlanetCount" ).Add( "=" ).Add( BlockedPlanetCount );
                debugBuffer.Add( "\n" ).Add( "PlanetsBlockedBy:" ).Add( "=" ).Add( blockedPlanetsBy.ToString() );
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( debugBuffer.ToStringAndReturnToPool() );
                debugBuffer = null;
            }
            #endregion

            ThrowawayDrawBagCanMemLeak<GameEntityTypeData> workingSeedDrawBag = ThrowawayDrawBagCanMemLeak<GameEntityTypeData>.Create_WillActuallyBeGCed( 30 );

            int numberToSeed = Count;
            while ( numberToSeed > 0 && potentialPlanets.Count > 0 )
            {
                numberToSeed--;
                Planet planet = potentialPlanets[Context.RandomToUse.Next( 0, potentialPlanets.Count )];
                potentialPlanets.Remove( planet );

                if ( !workingSeedDrawBag.GetHasItems() )
                    Helper_RefillWorkingSeedDrawBag( Context, eligibleEntityDatas, SeedType, galaxy, workingSeedDrawBag, GetWeight );
                
                if ( !workingSeedDrawBag.GetHasItems() )
                    break;
                
                var faction = FactionOrNull;
                if ( faction == null )
                {
                    if ( factionType == FactionType.AI )
                        faction = World_AIW2.Instance.GetFactionByIndex( planet.InitialOwningAIFactionIndex );
                    if ( faction == null ) //this is the "AI didn't have an initial owning faction sent or ANYTHING ELSE" condition
                        faction = planet.GetFirstFactionOfType( factionType ).Faction;
                }
                
                GameEntity_Squad squad = Helper_DoInnermostSeed_FromWorkingSeedDrawBag( Context, planet, SeedType, faction, SeedingZone, workingSeedDrawBag );
                if ( squad != null )
                {
                    if ( seededEntities != null )
                        seededEntities.Add( squad );
                    
                    if ( seededPlanets != null && !seededPlanets.Contains( planet ) )
                        seededPlanets.Add( planet );
                    
                    if (Callback != null)
                        Callback(squad);

                    switch ( SeedStyle )
                    {
                        case MapGenSeedStyle.FullUseByFactionOnNomadIfPossible:
                        case MapGenSeedStyle.FullUseByFaction:
                            planet.MapGen_IsFullyUsedByAFaction = true;
                            planet.MapGen_FullyUsingFaction = faction;
                            planet.MapGen_MajorFactionsBlockedHereReason = squad.TypeData.DisplayName;
                            break;
                        case MapGenSeedStyle.SmallGood:
                            planet.MapGen_NumberOfSmallGoodThingsHere++;
                            break;
                        case MapGenSeedStyle.SmallBad:
                            planet.MapGen_NumberOfSmallBadThingsHere++;
                            break;
                        case MapGenSeedStyle.BigGood:
                            planet.MapGen_NumberOfBigGoodThingsHere++;
                            break;
                        case MapGenSeedStyle.BigBad:
                            planet.MapGen_NumberOfBigBadThingsHere++;
                            break;
                        case MapGenSeedStyle.Fuel:
                            planet.MapGen_NumberOfFuelThingsHere++;
                            break;
                        case MapGenSeedStyle.FactionBeacon:
                            planet.MapGen_NumberOfFactionBeaconsHere++;
                            break;
                        case MapGenSeedStyle.NoChecks:
                            break;
                    }

                    int countPerPlanet = (int)CountPer;
                    if ( countPerPlanet > 1 )
                    {
                        for ( int i = 1; i < countPerPlanet; i++ )
                        {
                            squad = Helper_DoInnermostSeed_FromWorkingSeedDrawBag( Context, planet, SeedType, faction, SeedingZone, workingSeedDrawBag );
                            
                            if (squad != null && Callback != null)
                                Callback(squad);
                        }
                    }
                }
            }
        }

        #region PlanetSeederHelper
        private class PlanetSeederHelper
        {
            public IList<Planet> potentialPlanets;
            public MapGenSeedStyle SeedStyle;
            public Faction FactionOrNull;
            public int minDistanceFromAIHomeworld;
            public int maxDistanceFromAIHomeworld;
            public int minDistanceFromHumanHomeworld;
            public int maxDistanceFromHumanHomeworld;
            public ArcenHostOnlySimContext Context;
            public Int16 hopsToAvoidWhenSeedingMultipleFactionCopies;
            public int maxMatchingSeedTypes;

            public DelReturn Helper_DistanceBasePlanetLoop( Planet Item, short Distance )
            {
                if ( Distance <= 0 ) //cannot be ON the planet in question
                    return DelReturn.Continue;
                return Helper_PlanetLoop( Item );
            }

            public DelReturn Helper_PlanetLoop( Planet planet )
            {
                if ( planet.PopulationType == PlanetPopulationType.AIHomeworld || planet.PopulationType == PlanetPopulationType.HumanHomeworld ) //ignore AIBastionWorld
                    return DelReturn.Continue;

                if ( SeedStyle == MapGenSeedStyle.FullUseByFactionOnNomadIfPossible )
                {
                    //The nomad-specific path is quite different, but basically things that want to be seeded on Nomads ignore the minDistance stuff because the distance is variable.
                    //NOTA BENE: if other things are added to the main path in this loop that are important, mirror them here
                    if ( planet.TypeData.Type != PlanetType.Nomad )
                        return DelReturn.Continue;
                    if ( planet.MapGen_IsFullyUsedByAFaction )
                        return DelReturn.Continue; //blocked

                    potentialPlanets.Add( planet );
                    return DelReturn.Continue;
                }

                if ( (FactionOrNull != null && FactionOrNull.Type != FactionType.NaturalObject) &&
                     planet.TypeData.Type == PlanetType.Nomad ) //faction-specific stuff shouldn't be seeded on nomads unless requested
                    return DelReturn.Continue;
                if ( minDistanceFromAIHomeworld > 0 && planet.OriginalHopsToAIHomeworld < minDistanceFromAIHomeworld )
                    return DelReturn.Continue;
                if ( maxDistanceFromAIHomeworld > 0 && planet.OriginalHopsToAIHomeworld > maxDistanceFromAIHomeworld )
                    return DelReturn.Continue;
                if ( minDistanceFromHumanHomeworld > 0 && planet.OriginalHopsToHumanHomeworld < minDistanceFromHumanHomeworld )
                    return DelReturn.Continue;
                if ( maxDistanceFromHumanHomeworld > 0 && planet.OriginalHopsToHumanHomeworld > maxDistanceFromHumanHomeworld )
                    return DelReturn.Continue;
                if ( planet.MapGen_IsFullyUsedByAFaction )
                    return DelReturn.Continue; //blocked
                                               //Make sure we don't seed faction structures that fully block a planet next to other
                                               //instances of the same faction. Ie if faction X would seed two structures, they can be on adjacent planets.
                                               //But if you have two copies of faction X, they can't seed their structures near the other faction. Needed for things like the Nanocaust or ZA
                bool skipForDistance = false;
                foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops_NoFilters( hopsToAvoidWhenSeedingMultipleFactionCopies ) )
                {
                    Planet neighbor = _phd.Planet;
                    Int16 Distance = _phd.Hops;
                    if ( FactionOrNull == null )
                        break;
                    if ( Distance > hopsToAvoidWhenSeedingMultipleFactionCopies )
                        continue;
                    if ( neighbor.MapGen_IsFullyUsedByAFaction &&
                         neighbor.MapGen_FullyUsingFaction != FactionOrNull &&
                         neighbor.MapGen_FullyUsingFaction.GetDisplayName() == FactionOrNull.GetDisplayName() )
                    {
                        skipForDistance = true;
                        break;
                    }
                }
                if ( skipForDistance )
                    return DelReturn.Continue;

                switch ( SeedStyle )
                {
                    case MapGenSeedStyle.SmallGood:
                        if ( planet.MapGen_NumberOfSmallGoodThingsHere > maxMatchingSeedTypes )
                            return DelReturn.Continue;
                        break;
                    case MapGenSeedStyle.SmallBad:
                        if ( planet.MapGen_NumberOfSmallBadThingsHere > maxMatchingSeedTypes )
                            return DelReturn.Continue;
                        break;
                    case MapGenSeedStyle.BigGood:
                        if ( planet.MapGen_NumberOfBigGoodThingsHere > maxMatchingSeedTypes )
                            return DelReturn.Continue;
                        break;
                    case MapGenSeedStyle.BigBad:
                        if ( planet.MapGen_NumberOfBigBadThingsHere > maxMatchingSeedTypes )
                            return DelReturn.Continue;
                        break;
                    case MapGenSeedStyle.Fuel:
                        if ( planet.MapGen_NumberOfFuelThingsHere > maxMatchingSeedTypes )
                            return DelReturn.Continue;
                        break;
                    case MapGenSeedStyle.FactionBeacon:
                        if ( planet.MapGen_NumberOfFactionBeaconsHere > 0 )
                            return DelReturn.Continue; //one beacon per planet only!
                        if ( planet.MapGen_IsFullyUsedByAFaction )
                            return DelReturn.Continue; //if a planet is fully owned by a faction already, skip that too!
                        switch (planet.PopulationType )
                        {
                            case PlanetPopulationType.AIBastionWorld:
                            case PlanetPopulationType.AIHomeworld:
                            case PlanetPopulationType.ArkEmpireHumanHomeworld:
                            case PlanetPopulationType.DarkZenith:
                            case PlanetPopulationType.HumanHomeworld:
                                return DelReturn.Continue; //if one of these special planets, then skip!
                        }
                        break;
                    case MapGenSeedStyle.NoChecks:
                        break;
                }
                potentialPlanets.Add( planet );
                return DelReturn.Continue;
            }
        }
        #endregion

        private static GameEntity_Squad Helper_DoInnermostSeed_ByType( ArcenHostOnlySimContext Context, Planet planet, GameEntityTypeData TypeData, Faction FactionOrNull, PlanetSeedingZone SeedingZone )
        {
            GameEntity_Squad squad = planet.Mapgen_SeedEntity( Context, FactionOrNull == null ? planet.GetControllingFaction() : FactionOrNull,
                    TypeData, SeedingZone );
            if ( squad != null )
            {
                Helper_DoInnermostSeed_Afterward( Context, planet, squad );
            }
            return squad;
        }

        private static GameEntity_Squad Helper_DoInnermostSeed_FromWorkingSeedDrawBag( ArcenHostOnlySimContext Context, Planet planet, SeedingType SeedType, 
            Faction FactionOrNull, PlanetSeedingZone SeedingZone, ThrowawayDrawBagCanMemLeak<GameEntityTypeData> workingSeedDrawBag )
        {
            GameEntity_Squad squad = planet.Mapgen_SeedEntity( Context, FactionOrNull == null ? planet.GetControllingFaction() : FactionOrNull,
                    SeedType == SeedingType.HardcodedCount ? workingSeedDrawBag.PickRandomItemAndDoNotReplace( Context.RandomToUse ) :
                    workingSeedDrawBag.PickRandomItemAndReplace( Context.RandomToUse ),
                    SeedingZone );
            if ( squad != null )
            {
                if ( SeedType == SeedingType.CapturableWeightsAndMax )
                {
                    if ( !squad.TypeData.CapturableCanSeedAtAll )
                        workingSeedDrawBag.RemoveAnyItemsMatching( squad.TypeData );
                    else if ( squad.TypeData.CapturableMaxPerGalaxy > 0 )
                    {
                        int countNowSeeded = planet.ParentGalaxy.GetCountFromListOfEntitiesByEntityType( squad.TypeData );
                        countNowSeeded++; //for the one we're just adding

                        if ( countNowSeeded >= squad.TypeData.CapturableMaxPerGalaxy )
                            workingSeedDrawBag.RemoveAnyItemsMatching( squad.TypeData );
                    }
                }
                Helper_DoInnermostSeed_Afterward( Context, planet, squad );
            }
            return squad;
        }

        private static void Helper_DoInnermostSeed_Afterward( ArcenHostOnlySimContext Context, Planet planet, GameEntity_Squad squad )
        {
            if ( squad != null )
            {
                Fleet squadFleet = squad.GetFleetOrNull_Safe();
                if ( squadFleet == null )
                    return;
                switch ( squad.TypeData.SpecialType )
                {
                    case SpecialEntityType.MobileOfficerCombatFleetFlagship:
                    case SpecialEntityType.MobileStrikeCombatFleetFlagship:
                    case SpecialEntityType.MobileCustomUnattachedFleetFlagship:
                    case SpecialEntityType.MobileSupportFleetFlagship:
                    //case SpecialEntityType.MobileLoneWolfFleetFlagship:
                    case SpecialEntityType.BattlestationBasic:
                    case SpecialEntityType.BattlestationCitadel:
                    case SpecialEntityType.CityCenter:
                        squadFleet.SetAllMembershipsUpFromDesignTemplates_HostOnly( null, squad.TypeData.FleetDesignLogicIGrantOneOf,
                            squad.TypeData.FleetDesignTemplatesIAlwaysGrant );
                        break;
                    case SpecialEntityType.ThirdPartySellerToPlayers:
                        squadFleet.SetAllMembershipsUpFromDesignTemplates_HostOnly( null, FleetDesignLogic.Unused,
                            squad.TypeData.FleetDesignTemplatesIAlwaysGrant );
                        break;
                }

                if ( MapgenLogger.IsActive )
                {
                    MapgenLogger.Log( squad.TypeData.InternalName + "     seeded on " + squad.GetPlanetName_Safe() +
                        "   (dist:" + planet.OriginalHopsToHumanHomeworld + ")" +
                        "                        IsFullyUsedByAFaction: " + planet.MapGen_IsFullyUsedByAFaction +
                        "   NumberOfSmallGoodThingsHere: " + planet.MapGen_NumberOfSmallGoodThingsHere +
                        "   NumberOfSmallBadThingsHere: " + planet.MapGen_NumberOfSmallBadThingsHere +
                        "   NumberOfBigGoodThingsHere: " + planet.MapGen_NumberOfBigGoodThingsHere +
                        "   NumberOfBigBadThingsHere: " + planet.MapGen_NumberOfBigBadThingsHere +
                        "   NumberOfFuelThingsHere: " + planet.MapGen_NumberOfFuelThingsHere );
                }
            }
        }

        #region Helper_RefillWorkingSeedDrawBag
        private static void Helper_RefillWorkingSeedDrawBag( ArcenHostOnlySimContext Context, IList<GameEntityTypeData> eligibleEntityDatas, 
            SeedingType SeedType, Galaxy galaxy, ThrowawayDrawBagCanMemLeak<GameEntityTypeData> workingSeedDrawBag, GetSeedingWeight callback )
        {
            if ( eligibleEntityDatas == null )
                return;

            workingSeedDrawBag.Clear();
            switch ( SeedType )
            {
                case SeedingType.HardcodedCount:
                    {
                        GameEntityTypeData typeData;
                        for ( int i = 0; i < eligibleEntityDatas.Count; i++ )
                        {
                            typeData = eligibleEntityDatas[i];
                            if ( !typeData.CapturableCanSeedAtAll )
                                continue;
                            workingSeedDrawBag.AddItem( typeData, 1 );
                        }
                    }
                    break;
                case SeedingType.CapturableWeightsAndMax:
                    {
                        GameEntityTypeData typeData;
                        for ( int i = 0; i < eligibleEntityDatas.Count; i++ )
                        {
                            typeData = eligibleEntityDatas[i];
                            if ( !typeData.CapturableCanSeedAtAll )
                                continue;

                            int max = typeData.CapturableMaxPerGalaxy;
                            int weight = typeData.CapturableSeedWeight;
                            if ( callback != null )
                            { 
                                if (!callback(typeData, out weight, out max))
                                    continue;
                            }

                            if (max > 0)
                            {
                                int countAlreadySeeded = galaxy.GetCountFromListOfEntitiesByEntityType( typeData );
                                if ( countAlreadySeeded >= max )
                                    continue;
                            }

                            if ( weight <= 0 )
                                workingSeedDrawBag.AddItem( typeData, 100 );
                            else
                                workingSeedDrawBag.AddItem( typeData, weight );
                        }
                    }
                    break;
            }
        }
        #endregion

        public static void ClearAllUnitsNotBelongingToThisFaction( IList<Planet> planetsSeededOn, Faction faction, bool AlsoAssignOwnership )
        {
            for ( int i = 0; i < planetsSeededOn.Count; i++ )
            {
                Planet planet = planetsSeededOn[i];
                foreach ( GameEntity_Squad entity in planet.Squads() )
                {
                    if ( entity.PlanetFaction.Faction == faction )
                        continue;
                    if ( entity.GetFactionTypeSafe() == FactionType.NaturalObject )
                        continue;
                    if ( entity.GetFactionTypeSafe() == FactionType.Player )
                        continue; //should not happen, but just in case!
                    if ( entity.TypeData.GetMatches_SemiSlow( EntityRollupType.Claimables ) )
                        continue;
                    if ( entity.TypeData.GetMatches_SemiSlow( EntityRollupType.KingUnitsOnly ) )
                        continue;
                    if ( entity.TypeData.GetMatches_SemiSlow( EntityRollupType.EnergyProducers ) )
                        continue;
                    if ( entity.TypeData.GetMatches_SemiSlow( EntityRollupType.MetalProducers ) )
                        continue;
                    if ( entity.TypeData.GetMatches_SemiSlow( EntityRollupType.Battlestation ) )
                        continue;
                    if ( entity.TypeData.GetMatches_SemiSlow( EntityRollupType.MobileFleetFlagships ) )
                        continue;

                    entity.OnlyInMapgenOrInActuallyGettingRidOfEntities_ImmediatelyRemoveFromSim( InstancedRendererDeactivationReason.MapGen_RemoveAllEntitiesNotFromThisFaction );
                }

                planet.UnderInfluenceOfFactionIndex.Add( faction.FactionIndex );
                planet.InitialOwningAIFactionIndex = faction.FactionIndex;
                planet.OverrridingSetOwnership( faction );
            }
        }
        public void SetInitialPlayerVision( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration MapConfig, MapTypeData mapType )
        {
            foreach ( Planet planet in galaxy.Planets( false ) )
            {
                if ( planet.PopulationType == PlanetPopulationType.HumanHomeworld || planet.PopulationType == PlanetPopulationType.ArkEmpireHumanHomeworld )
                {
                    foreach ( var ph in planet.PlanetsWithinXHops_NoFilters( 3 ) )
                    {
                        Planet neighbor = ph.Planet;
                        Int16 distance = ph.Hops;
                        if ( distance == 1 )
                        {
                            neighbor.IntelLevel = PlanetIntelLevel.PermanentlyWatched;
                            neighbor.GameSecondLastHadVisionBase = World_AIW2.Instance.GameSecond + 2;

                            if ( MapgenLogger.IsActive )
                                MapgenLogger.Log( "PermanentlyWatched: " + neighbor.Name );
                        }
                        else if ( distance <= 2 )
                        {
                            neighbor.IntelLevel = PlanetIntelLevel.ExploredByNaturalMeans;
                            neighbor.GameSecondLastHadVisionBase = World_AIW2.Instance.GameSecond + 2;
                            if ( MapgenLogger.IsActive )
                                MapgenLogger.Log( "ExploredByNaturalMeans: " + neighbor.Name );
                        }
                    }
                    planet.IntelLevel = PlanetIntelLevel.PermanentlyWatched;
                    planet.GameSecondLastHadVisionBase = World_AIW2.Instance.GameSecond + 2;
                    if ( MapgenLogger.IsActive )
                        MapgenLogger.Log( "PermanentlyWatched (HumanHomeworld): " + planet.Name );
                }
            }
        }

        private static readonly Dictionary<Faction, AIStartSize> startSizes = Dictionary<Faction, AIStartSize>.Create_WillNeverBeGCed( World_AIW2.Instance.AIFactions.Count + 2, "StandardMapPopulator-startSizes" );
        public void DetermineAiOwnership( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration MapConfig, MapTypeData mapType )
        {
            if ( World_AIW2.Instance.GetIsTutorial() )
            {
                if ( World_AIW2.Instance.TutorialOrNull.SkipDetermineAiOwnership )
                {
                    if ( MapgenLogger.IsActive )
                        MapgenLogger.Log( "SkipDetermineAiOwnership Because Tutorial" );
                    return;
                }
                else
                {
                    if ( MapgenLogger.IsActive )
                        MapgenLogger.Log( "Tutorial But Not SkipDetermineAiOwnership" );
                }
            }
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "DetermineAiOwnership In New Style" );

            int debugStage = 0;
            try
            {
                bool debug = false;
                debugStage = 100;

                //we need this list later
                ThrowawayListCanMemLeak<Planet> AIHomeworlds = new ThrowawayListCanMemLeak<Planet>( 500 );
                foreach ( Planet planet in galaxy.Planets( false ) )
                {
                    debugStage = 101;
                    if ( planet.PopulationType == PlanetPopulationType.AIHomeworld ) //ignore AIBastionWorld
                    {
                        debugStage = 102;
                        AIHomeworlds.Add( planet );
                    }
                }

                debugStage = 150;
                //int aiIndex = 0;

                if ( World_AIW2.Instance.AIFactions.Count <= 0 )
                {
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "There are no AI factions in the current game!  Please add one to continue." );
                    if ( MapgenLogger.IsActive )
                        MapgenLogger.Log( "There are no AI factions in the current game!  Please add one to continue." );
                    return;
                }

                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "AI Empire Size Calculation" );

                //Set up the logic for allowing AIs to have different numbers of planets and layout
                int allAvailablePlanets = World_AIW2.Instance.CurrentGalaxy.GetCountOfNonDestroyedPlanets() - (World_AIW2.Instance.AIFactions.Count + World_AIW2.Instance.EmpireStylePlayerFactions.Count);
                int averagePlanets = allAvailablePlanets / World_AIW2.Instance.AIFactions.Count;
                int numHuge = 0;
                int totalEmpiresSize;
                startSizes.Clear();
                for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
                {
                    Faction faction = World_AIW2.Instance.AIFactions[i];
                    AISentinelsCoreData factionExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
                    startSizes[faction] = faction.CustomData_StartingAIPlanetRatio();
                }

                do
                {
                    totalEmpiresSize = 0;
                    //We iterate over the AI Factions several times; first we take a pass to scale up all the requested empire sizes
                    //until we have at least one at Huge. We handle the case of (say) 3 very smalls and 1 normal and 3 normals and one huge identically;
                    //it's about the proportional empire sizes
                    for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
                    {
                        Faction faction = World_AIW2.Instance.AIFactions[i];
                        totalEmpiresSize += (int)startSizes[faction];
                        if ( startSizes[faction] >= AIStartSize.Huge )
                            numHuge++;
                    }
                    if ( MapgenLogger.IsActive )
                        MapgenLogger.Log( "totalEmpiresSize: " + totalEmpiresSize );
                    if ( numHuge == 0 )
                    {
                        //we found no Huges, so bump all the sizes requested up; we need at least one AI to be at "Huge"
                        for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
                        {
                            Faction faction = World_AIW2.Instance.AIFactions[i];
                            startSizes[faction]++;
                        }
                    }
                }while ( numHuge == 0 );
                int radiusOfPlanetsAroundHomeworldsUntilOverlap = GetRadiusOfOverlapBetweenHomeworlds(AIHomeworlds);
                int highestRadius = 0;
                int planetsPerSizeUnits = allAvailablePlanets / totalEmpiresSize;
                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "planetsPerSizeUnits: " + planetsPerSizeUnits );
                for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
                {
                    //TODO: adjust this to use totalEmpireSize
                    debugStage = 200;
                    Faction faction = World_AIW2.Instance.AIFactions[i];
                    AISentinelsCoreData factionExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
                    //first how many planets to assign to each AI
                    if ( startSizes[faction] < AIStartSize.Huge )
                        faction.Mapgen_MaxPlanetsToAssign = planetsPerSizeUnits * (int)startSizes[faction];
                    else
                        faction.Mapgen_MaxPlanetsToAssign = allAvailablePlanets; //can take as many planets as it wants

                    if ( faction.Mapgen_MaxPlanetsToAssign < 5 )
                        faction.Mapgen_MaxPlanetsToAssign = 5; //never fewer than 5 planets
                    debugStage = 300;
                    //Now figure out how large of clusters of planets around a given homeworld to use
                    AIOwnershipLayout startingLayout = faction.CustomData_StartingAILayout();
                    if ( startingLayout == AIOwnershipLayout.Random )
                        faction.Mapgen_RadiusAroundHomeworld = 0;
                    else if ( startingLayout == AIOwnershipLayout.SmallCluster )
                        faction.Mapgen_RadiusAroundHomeworld = ExternalConstants.Instance.SmallAIHomeworldClusterRadius; //ai homeworlds really should never be that close to eachother so this is safe
                    else if ( startingLayout == AIOwnershipLayout.RandomCluster )
                    {
                        if ( radiusOfPlanetsAroundHomeworldsUntilOverlap >= 7 )
                            faction.Mapgen_RadiusAroundHomeworld = Context.RandomToUse.Next(4, radiusOfPlanetsAroundHomeworldsUntilOverlap + 3);
                        else
                            faction.Mapgen_RadiusAroundHomeworld = Context.RandomToUse.Next(3, radiusOfPlanetsAroundHomeworldsUntilOverlap + 1);
                    }
                    else if ( startingLayout == AIOwnershipLayout.LargeCluster )
                        faction.Mapgen_RadiusAroundHomeworld = radiusOfPlanetsAroundHomeworldsUntilOverlap + 1;
                    if ( highestRadius < faction.Mapgen_RadiusAroundHomeworld )
                        highestRadius = faction.Mapgen_RadiusAroundHomeworld;
                    if ( MapgenLogger.IsActive )
                        MapgenLogger.Log( "AI Faction " + faction.FactionIndex + " has empire size " + startSizes[faction] + " and preffered number of planets " + faction.Mapgen_MaxPlanetsToAssign + " in layout " + startingLayout + " has radius " + faction.Mapgen_RadiusAroundHomeworld );
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "AI Faction " + faction.FactionIndex + " has empire size " + startSizes[faction] + " and preffered number of planets " + faction.Mapgen_MaxPlanetsToAssign + " (" + planetsPerSizeUnits +") in layout " + startingLayout + " has radius " + faction.Mapgen_RadiusAroundHomeworld , Verbosity.DoNotShow );
                }
                debugStage = 400;


                /* First assign all the clusters (or as much of a cluster as we can if we are constrained in planets) */
                for ( int i = 0; i < highestRadius; i++ )
                {
                    if ( !World_AIW2.Instance.Setup.ShouldSeedDetailsYet )
                        break;
                    //we assign planets one ring at a time in case of overlaps
                    for ( int j = 0; j < AIHomeworlds.Count; j++ )
                    {
                        debugStage = 3101;
                        Planet homeworld = AIHomeworlds[j];
                        Faction chosenFaction = World_AIW2.Instance.GetFactionByIndex(homeworld.InitialOwningAIFactionIndex);
                        AISentinelsCoreData factionExternal = chosenFaction.TryGetAISentinelsCoreData()?.SentinelInfo;
                        if ( i > chosenFaction.Mapgen_RadiusAroundHomeworld )
                            continue; //we have already allocated our entire radius, so skip us
                        if ( chosenFaction.Mapgen_NumPlanetsAssigned > chosenFaction.Mapgen_MaxPlanetsToAssign )
                            continue; //early bail out if we have assigned all our planets. We also check this again in the "assign planets" loop below
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Assigning homeworld ring " + i + " for faction " + chosenFaction.FactionIndex, Verbosity.DoNotShow );

                        foreach ( var ph in homeworld.PlanetsWithinXHops_NoFilters( (Int16)i ) )
                        {
                            Planet planet = ph.Planet;
                            Int16 distance = ph.Hops;
                            debugStage = 3110;
                            if ( planet.PopulationType == PlanetPopulationType.HumanHomeworld || planet.PopulationType == PlanetPopulationType.ArkEmpireHumanHomeworld ||planet.PopulationType == PlanetPopulationType.AIHomeworld ) //ignoring bastion-ness seems to be fine? might need adjusting later
                                continue;

                            if ( chosenFaction.Mapgen_NumPlanetsAssigned > chosenFaction.Mapgen_MaxPlanetsToAssign )
                            {
                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine("We have already assigned " + chosenFaction.Mapgen_NumPlanetsAssigned + " to " + chosenFaction.FactionIndex + ", and the max was " + chosenFaction.Mapgen_MaxPlanetsToAssign + ", so break out of the homeworld radius loop", Verbosity.DoNotShow );
                                break; //if we have limited the number of planets we will asign, honor that
                            }

                            if (planet.InitialOwningAIFactionIndex > 0) //only should be possible for LargeClusters; sometimes we can overlap, in which case 'first one there wines', but we'll make it up in the 'random' section
                                continue;
                            debugStage = 3111;
                            if ( planet.InitialOwningAIFactionIndex <= 0 ) //set this to be our planet
                            {
                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "Found planet " + planet.Index + " " + planet.Name + " at distance " + distance + " ai faction with initial owning faction " + homeworld.InitialOwningAIFactionIndex, Verbosity.DoNotShow );
                                debugStage = 3112;
                                chosenFaction.Mapgen_NumPlanetsAssigned++;
                                planet.InitialOwningAIFactionIndex = chosenFaction.FactionIndex;
                                continue;
                            }
                        }
                    }
                }
                debugStage = 3300;
                int numRandomPlanets = 0;
                //Now that we've done a ring of suitable size around each homeworld, assign the remainder at random
                int unassignedPlanets = GetUnassignedPlanetCount(galaxy);
                foreach ( Planet planet in galaxy.Planets( false ) )
                {
                    if ( planet.InitialOwningAIFactionIndex > 0 )
                        continue;
                    if ( planet.PopulationType == PlanetPopulationType.HumanHomeworld 
                        || planet.PopulationType == PlanetPopulationType.ArkEmpireHumanHomeworld 
                        || planet.PopulationType == PlanetPopulationType.AIHomeworld )  //AIBastionWorld ignore good?
                    {
                        continue;
                    }
                    debugStage = 3320;
                    if ( World_AIW2.Instance.AIFactions.Count == 0 )
                        continue;
                    //if not seeding the details yet, then... skip all this!
                    if ( !World_AIW2.Instance.Setup.ShouldSeedDetailsYet )
                        break;
                    //decide who gets the next random planet
                    Faction fewestPlanetsFaction = GetNextFactionForPlanetAssignment(unassignedPlanets, galaxy, Context);
                    debugStage = 3330;
                    Faction chosenFaction = World_AIW2.Instance.GetFactionByIndex(fewestPlanetsFaction.FactionIndex);
                    debugStage = 3340;
                    unassignedPlanets--;
                    chosenFaction.Mapgen_NumPlanetsAssigned++;
                    planet.InitialOwningAIFactionIndex = chosenFaction.FactionIndex;
                    debugStage = 3350;
                    if ( MapgenLogger.IsActive )
                        MapgenLogger.Log( "New AI owning planet " + planet.Name + ", faction " + planet.InitialOwningAIFactionIndex );
                    numRandomPlanets++;
                }
                if ( debug )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Randomly assigned " + numRandomPlanets + " planets", Verbosity.DoNotShow );
                    for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine("\tAssigned " + World_AIW2.Instance.AIFactions[i].Mapgen_NumPlanetsAssigned + " planets total to " + World_AIW2.Instance.AIFactions[i].FactionIndex, Verbosity.DoNotShow );
                    }
                }

                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "Randomly assigned " + numRandomPlanets + " planets" );
            }
            catch ( Exception e )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "DetermineAiOwnership error at stage " + debugStage + "\n\n" + e );
                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "DetermineAiOwnership error at stage " + debugStage + "\n\n" + e );
            }
        }
        private int GetUnassignedPlanetCount (Galaxy galaxy)
        {
            int count = 0;
            foreach ( Planet planet in galaxy.Planets( false ) )
            {
                if ( planet.InitialOwningAIFactionIndex > 0 )
                    continue;
                if ( planet.PopulationType == PlanetPopulationType.HumanHomeworld || planet.PopulationType == PlanetPopulationType.ArkEmpireHumanHomeworld ||
                     planet.PopulationType == PlanetPopulationType.ArkEmpireHumanHomeworld || planet.PopulationType == PlanetPopulationType.AIHomeworld )  //AIBastionWorld ignore good?
                    continue;
                count++;
            }
            return count;
        }
        private Faction GetNextFactionForPlanetAssignment(int unassignedPlanets, Galaxy galaxy, ArcenHostOnlySimContext Context)
        {
            Faction faction = null;
            int retries = 100;
            if ( World_AIW2.Instance.AIFactions.Count == 1 )
                return World_AIW2.Instance.AIFactions[0]; //if we have only one AI faction, always assign the planets to it
            do{
                faction = World_AIW2.Instance.AIFactions[ Context.RandomToUse.Next(0, World_AIW2.Instance.AIFactions.Count) ];
                if ( faction.Mapgen_NumPlanetsAssigned >= faction.Mapgen_MaxPlanetsToAssign )
                    faction = null;
            } while ( faction == null && retries-- > 0 );
            return faction;
        }
        private int GetRadiusOfOverlapBetweenHomeworlds(IList<Planet> AIHomeworlds)
        {
            //Figure out how far apart the closest homeworlds are
            int debugStage = 0;
            Int16 radius = 0;
            bool debug = false;
            bool foundEdge = false;
            int retries = 100;
            
            try{
            debugStage = 1000;
            if ( AIHomeworlds.Count == 1 )
                return 5; //if we have only one homeworld, return any number, it will all just get handled in the wash

            while ( !foundEdge && retries-- > 0 )
            {
                radius++;
                debugStage = 3100;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("Checking radius " + radius, Verbosity.DoNotShow );
                for ( int j = 0; j < AIHomeworlds.Count; j++ )
                {
                    debugStage = 3101;
                    Planet homeworld = AIHomeworlds[j];
                    debugStage = 3102;
                    foreach ( var ph in homeworld.PlanetsWithinXHops_NoFilters( radius ) )
                    {
                        Planet planet = ph.Planet;
                        Int16 distance = ph.Hops;
                        debugStage = 3110;
                        if ( planet.PopulationType == PlanetPopulationType.HumanHomeworld || planet.PopulationType == PlanetPopulationType.ArkEmpireHumanHomeworld ||
                             planet.PopulationType == PlanetPopulationType.AIHomeworld ) //Ignoring bastion worlds is fine here
                            continue;

                        debugStage = 3111;
                        if ( (planet.InitialOwningAIFactionIndex > 0) && planet.InitialOwningAIFactionIndex != homeworld.InitialOwningAIFactionIndex )
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Found planet " + planet.Index + " " + planet.Name + " at distance " + distance + " with owning faction " + planet.InitialOwningAIFactionIndex + " for homeworld with initial owning faction " + homeworld.InitialOwningAIFactionIndex, Verbosity.DoNotShow );
                            foundEdge = true;
                            break;
                        }
                        planet.InitialOwningAIFactionIndex = homeworld.InitialOwningAIFactionIndex;
                    }
                }
                debugStage = 3200;
                if ( foundEdge )
                {
                    //now reset the Initial Owning AI Faction Indices
                    //note this logic sucks and should probably be reworked someday
                    for ( int j = 0; j < AIHomeworlds.Count; j++ )
                    {
                        debugStage = 3101;
                        Planet homeworld = AIHomeworlds[j];
                        foreach ( var ph in homeworld.PlanetsWithinXHops_NoFilters( radius ) )
                        {
                            Planet planet = ph.Planet;
                            Int16 distance = ph.Hops;
                            if ( planet.PopulationType == PlanetPopulationType.HumanHomeworld || planet.PopulationType == PlanetPopulationType.ArkEmpireHumanHomeworld ||
                                 planet.PopulationType == PlanetPopulationType.AIHomeworld ) //AIBastionWorld ignore good?
                                continue;
                            planet.InitialOwningAIFactionIndex = -1;
                        }

                    }
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Found the edge at radius " + radius, Verbosity.DoNotShow );
                }
            }
            }catch(Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in GetLargeClusterHomworldRadius code " + debugStage + " " + e.ToString(), Verbosity.ShowAsError );
            }
            if ( foundEdge )
            {
                return radius;
            }
            else
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Bug: failed to find the radius", Verbosity.DoNotShow );
                return 5;
            }
        }

        private void RecomputeDistancesToHomeworlds( Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
            List<Planet> WorkingHomeworldsList = Planet.GetTemporaryPlanetList( "StandardMapPop-RecomputeDistancesToHomeworlds-WorkingHomeworldsList", 10f );
            if ( WorkingHomeworldsList == null ) //blocked for teardown/shutdown; bail
                return;
            foreach ( Planet planet in galaxy.Planets( false ) )
            {
                if ( planet.PopulationType == PlanetPopulationType.AIHomeworld || planet.PopulationType == PlanetPopulationType.ArkEmpireHumanHomeworld ||
                     planet.PopulationType == PlanetPopulationType.HumanHomeworld ) //AIBastionWorld ignore
                {
                    WorkingHomeworldsList.Add( planet );
                    planet.OriginalHopsToAnyHomeworld = 0;
                }
            }
            for ( int i = 0; i < WorkingHomeworldsList.Count; i++ )
            {
                Planet homeworld = WorkingHomeworldsList[i];
                foreach ( var ph in homeworld.PlanetsWithinXHops_NoFilters( -1 ) )
                {
                    Planet planet = ph.Planet;
                    Int16 distance = ph.Hops;
                    if ( distance < planet.OriginalHopsToAnyHomeworld || planet.OriginalHopsToAnyHomeworld == -1 )
                        planet.OriginalHopsToAnyHomeworld = distance;
                }
            }

            Planet.ReleaseTemporaryPlanetList( WorkingHomeworldsList );
        }
        
        public void DetermineAiHomeworlds( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration MapConfig, MapTypeData mapType )
        {
            if ( World_AIW2.Instance.GetIsTutorial() )
            {
                if ( World_AIW2.Instance.TutorialOrNull.SkipDetermineAiHomeworlds )
                    return;
            }

            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "DetermineAiHomeworlds start!" );

            //Proposed new algorithm. First identify player homeworlds.
            //Write a function that says "Identify reasonable planets that avoid the following planets"
            //call that function to identify possible AI homeworlds, then choose one at random.
            //Then call the "Identify reasonable planets" function again but include the new AI homeworlds. Repeat

            //Also make sure planet.OriginalHopsTo<AI|Human>Homeworld is updated for each planet
            bool debug = false;
            ThrowawayListCanMemLeak<Planet> possibleAIHomeworlds = new ThrowawayListCanMemLeak<Planet>( 500 );
            List<Planet> WorkingHomeworldsList = Planet.GetTemporaryPlanetList( "StandardMapPop-DetermineAiHomeworlds-WorkingHomeworldsList", 10f );
            if ( WorkingHomeworldsList == null ) //blocked for teardown/shutdown; bail
                return;
            for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
            {
                WorkingHomeworldsList.Clear();
                int furthestDistanceFromAnyHomeworld = 0;
                RecomputeDistancesToHomeworlds( galaxy, Context );
                foreach ( Planet planet in galaxy.Planets( false ) )
                {
                    furthestDistanceFromAnyHomeworld = Math.Max( furthestDistanceFromAnyHomeworld, planet.OriginalHopsToAnyHomeworld );
                }

                int acceptableDistance = furthestDistanceFromAnyHomeworld - 2;
                acceptableDistance = Math.Max( acceptableDistance, 5 );
                acceptableDistance = Math.Min( acceptableDistance, furthestDistanceFromAnyHomeworld );
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "For AI faction " + i + " furthestDistance " + furthestDistanceFromAnyHomeworld + " acceptableDistance " + acceptableDistance, Verbosity.DoNotShow );
                possibleAIHomeworlds.Clear();

                foreach ( Planet planet in galaxy.Planets( false ) )
                {
                    if ( planet.OriginalHopsToAnyHomeworld < acceptableDistance )
                        continue;
                    if ( planet.TypeData.Type == PlanetType.Nomad )
                        continue;
                    possibleAIHomeworlds.Add( planet );
                }

                Planet proposedAIHomePlanet = null;

                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "CreateGalaxies: Finding homeworld " + i + " of " + World_AIW2.Instance.AIFactions.Count + " Currently choosing between " + possibleAIHomeworlds.Count + " possible homeworlds based on distance from all homeworlds, aka < " + acceptableDistance, Verbosity.DoNotShow );
                if ( possibleAIHomeworlds.Count <= 0 )
                {
                    //if there are no good choices, pick at random. Hopefully this won't happen much?
                    proposedAIHomePlanet = galaxy.GetRandomPlanet( false, Context ); //this WAS just picking the last planet, but now it's actually a random one

                    if ( MapgenLogger.IsActive )
                        MapgenLogger.Log( "No good choices, so choose a random one" );
                }
                else
                {
                    proposedAIHomePlanet = possibleAIHomeworlds[Context.RandomToUse.Next( 0, possibleAIHomeworlds.Count )];
                    if ( MapgenLogger.IsActive )
                        MapgenLogger.Log( "Choose one of the " + possibleAIHomeworlds.Count + " options" );
                }
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Using planet " + proposedAIHomePlanet.Index + " for ai faction " + World_AIW2.Instance.AIFactions[i].FactionIndex, Verbosity.DoNotShow );
                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "Using planet " + proposedAIHomePlanet.Index + " for ai faction " + World_AIW2.Instance.AIFactions[i].FactionIndex );
                
                TryAssignAIHomeworld( galaxy, proposedAIHomePlanet.Index, World_AIW2.Instance.AIFactions[i], "StandardMapPopulator.DetermineAiHomeworlds", true ); //this should already be to the point where a failed attempt is unlikely
            }
            Planet.ReleaseTemporaryPlanetList( WorkingHomeworldsList );
        }
        
        public void DeterminePlayerHomeworlds( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration MapConfig, MapTypeData mapType )
        { 
            //LOG.Msg("{0}() called.\n{1}", this.TypeNameAndMethod(), LOG.StackTrace(10));
            
            int debugstage = 0;
            List<Faction> players = null;
            try
            {
                players = Faction.GetTemporaryFactionList("DeterminePlayerHomeworlds", 5.0f);
                if ( players == null ) //blocked for teardown/shutdown; bail
                    return;
                players.AddRange(World_AIW2.Instance.AllPlayerFactions);
                
                for (int pass = 0; pass < 2; pass++)
                {
                    if (players.Count == 0)
                        break;
                    
                    for (int i = 0; i < players.Count; i++)
                    {
                        var faction = players[i];
                        var player = faction.PlayerTypeDataOrNull_ModeratelyExpensive;
                        var config = faction.Config;
                        if (config != World_AIW2.Instance.Setup.GetConfigurationForFaction(faction.FactionIndex))
                        {
                            LOG.Err("Logic error in {0}()", this.TypeNameAndMethod());
                            goto remove_and_continue;
                        }
                        
                        if (player.StartsWithAlliedEmpire && pass == 0)
                            continue;
                        
                        Int16 index = config.StartingIndex;
                        Int16 prevIndex = config.StartingIndex;

                        int maxTries = player.StartsWithAlliedEmpire ? 2 : 30;
                        for ( int attemptCount = 0; attemptCount < maxTries; attemptCount++ )
                        {
                            if (index != -1)
                            {
                                if ( TryAssignHumanHomeworld( galaxy, index, faction, "StandardMapPopulator.DeterminePlayerHomeworlds", false ) )
                                {
                                    //if this is not set, then things won't seed correctly later
                                    //
                                    // jcf: I have not reproduced this issue (or tried to) but it makes some sense.
                                    //      Because, as I just learned, a queued game command is not actually executed 
                                    //      for the host immediately at the QueueGameCommand call. It gets queued like
                                    //      it would for a client, then handled some time later when all those would
                                    //      be sent. *Probably* this is a good thing, so you can reproduce bugs in a
                                    //      singleplayer situation that otherwise would only occur in multiplayer.
                                    //
                                    //      But it is also rather annoying when already within host-only code and
                                    //      you want to make a change right now and inform clients of it at the same time.
                                    //
                                    // jcf: Not that I can explain it, but without doing this you get error messages about player
                                    //      homeworlds not being seeded, when trying to start a quickstart. 
                                    config.StartingIndex = index;

                                    GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                                    command.RelatedString = "StartingIndex_FromAutoAssignInMapgen";
                                    command.RelatedIntegers.Add( faction.FactionIndex ); 
                                    command.RelatedIntegers2.Add( index );
                                    command.RelatedIntegers3.Add( prevIndex );
                                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                                    //ArcenDebugging.ArcenDebugLogSingleLine(string.Format("Human homeworld assigned to planet #{0}", index), Verbosity.DoNotShow);

                                    goto remove_and_continue;
                                }
                            }

                            if (player.StartsWithAlliedEmpire)
                            {
                                bool RatePlanet( Planet p, out Galaxy.PlanetRating r )
                                {
                                    r = new Galaxy.PlanetRating();
                                    return p.MapGen_FullyUsingFaction?.IsConsideredAFullEmpirePlayerType_Safe()??false;
                                }
                                
                                var trace = MapgenLogger.IsActive ? ArcenCharacterBuffer.GetFromPoolOrCreate("temp") : null;
                                var planets = galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs();
                                var candidatePlanet = World_AIW2.Instance.CurrentGalaxy.GetRandomPlanet(Engine_Universal.PermanentQualityRandom, planets, RatePlanet, trace);
                                if (candidatePlanet != null)
                                    index = candidatePlanet.Index;
                                if (MapgenLogger.IsActive) MapgenLogger.Log(trace.ToStringAndReturnToPool());
                            }
                            else
                            {
                                var candidatePlanet = World_AIW2.Instance.CurrentGalaxy.GetRandomPlanet(false, Engine_Universal.PermanentQualityRandom);
                                index = candidatePlanet.Index;
                            }
                        }

                        remove_and_continue:
                        {
                            players.Remove(faction);
                            i--;
                        }
                    }
                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch (Exception e)
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    LOG.Err("Exception in {0}() at debugstage {1}.\n{2}", this.TypeNameAndMethod(), debugstage, e);
            }
            finally
            {
                Faction.ReleaseTemporaryFactionList(players);
            }
        }

        public bool TryAssignHumanHomeworld( Galaxy galaxy, Int16 Index, Faction faction, string Reason, bool ErrorOutIfFails )
        {
            //LOG.Msg("{0}() called. Index={1}, faction={2}, Reason={3}", this.TypeNameAndMethod(), Index, faction.OrNull(), Reason.OrNull());
            
            bool Try()
            {
                Planet planet = null;
                int debugStage = 0;
                try
                {
                    debugStage = 100;
                    planet = galaxy.GetPlanetByIndex( Index );
                    
                    if ( MapgenLogger.IsActive )
                    {
                        MapgenLogger.Log( 
                            string.Format("{0}() called for planet index {1} ({2}) and faction {3} with reason \"{4}\".", 
                                          this.TypeNameAndMethod(), Index, planet.OrNull(), faction.OrNull(), Reason.OrNull()));
                    }
                    
                    debugStage = 200;
                    if (planet == null)
                    {
                        if ( ErrorOutIfFails ) 
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen(string.Format("Error trying to assign homeworld for {0}. Planet index {1} does not exist.", faction.OrNull(), Index));
                        return false;
                    }
                    
                    debugStage = 300;
                    var player = faction.PlayerTypeDataOrNull_ModeratelyExpensive;
                    if (player == null)
                    {
                        if ( ErrorOutIfFails ) 
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen(string.Format("Error trying to assign homeworld for {0}. This faction does not appear to be a player.", faction.OrNull()));
                        return false;
                    }

                    if (player.StartsWithAlliedEmpire)
                    {
                        if (!(planet.MapGen_FullyUsingFaction?.PlayerTypeDataOrNull_ModeratelyExpensive?.IsConsideredAFullEmpire ?? false))
                        {
                            if ( ErrorOutIfFails ) 
                            {
                                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen(
                                    string.Format("Error trying to assign homeworld for {0}. Planet {1} expected to be the homeworld of an empire style player and is not.", 
                                                  faction.OrNull(), planet.OrNull()));
                            }
                            
                            return false;
                        }
                    }
                    
                    if (player.TakesOwnershipOfStartingPlanet)
                    {
                        if ( planet.PopulationType != PlanetPopulationType.None )
                        {
                            if ( ErrorOutIfFails ) 
                            {
                                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen(
                                    string.Format("Error trying to assign homeworld for {0}. Planet {1} already assigned to someone as {2}.", 
                                                  faction.OrNull(), planet.OrNull(), Extensions.ToString(planet.PopulationType)));
                            }
                            return false;
                        }
                        
                        debugStage = 410;
                        
                        if (planet.PopulationType != PlanetPopulationType.HumanHomeworld && 
                            (player != null && player.DoesNotClaimPlanetFromAI))
                        {
                            planet.PopulationType = PlanetPopulationType.ArkEmpireHumanHomeworld;
                        }
                        else
                        {
                            planet.PopulationType = PlanetPopulationType.HumanHomeworld;
                        }

                        debugStage = 420;
                        
                        planet.GravWellSize = World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Engine_Universal.PermanentQualityRandom, planet.PopulationType );

                        debugStage = 500;
                    
                        planet.OriginalHopsToHumanHomeworld = 0;
                        foreach ( var ph in planet.PlanetsWithinXHops_NoFilters( -1 ) )
                        {
                            Planet otherPlanet = ph.Planet;
                            Int16 distance = ph.Hops;
                            if ( otherPlanet == null )
                                continue;

                            if ( otherPlanet.PopulationType == PlanetPopulationType.HumanHomeworld )
                                continue;

                            if ( otherPlanet.OriginalHopsToHumanHomeworld > 0 &&
                                 otherPlanet.OriginalHopsToHumanHomeworld <= distance )
                                continue;

                            otherPlanet.OriginalHopsToHumanHomeworld = distance;
                        }
                        
                        debugStage = 600;
                        
                        planet.MapGen_FullyUsingFaction = faction;
                        planet.MapGen_IsFullyUsedByAFaction = true;
                    }
                    
                    debugStage = 700;
                }
                catch (Exception e)
                {
                    LOG.Err("Exception in {0}() at debugstage={1}.\n{2}", this.TypeNameAndMethod(), debugStage, e);
                    return false;
                }

                //LOG.Msg("Player {0} assigned starting planet {1}#{2}", faction.OrNull(), planet.OrNull(), planet?.Index.OrNull());
                return true;
            }
            
            bool result = Try();
            //LOG.Msg("Result={0}", result);
            
            return result;
        }

        public bool TryAssignAIHomeworld( Galaxy galaxy, Int16 Index, Faction faction, string Reason, bool ErrorOutIfFails )
        {
            Planet planet = galaxy.GetPlanetByIndex( Index );
            if ( planet.PopulationType != PlanetPopulationType.None )
            {
                if ( ErrorOutIfFails ) //this prevents us from loading into the game
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Yikes!  Tried to make planet " + Index + " into an AI homeworld, but it was already " + planet.PopulationType );
                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "FAIL Helper_TryAssignAIHomeworld for factionIndex: " + faction.FactionIndex + " planet: " + planet.Name );
                return false;
            }
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "Helper_TryAssignAIHomeworld for factionIndex: " + faction.FactionIndex + " planet: " + planet.Name );

            planet.PopulationType = PlanetPopulationType.AIHomeworld;
            planet.GravWellSize = World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Engine_Universal.PermanentQualityRandom, planet.PopulationType );
            planet.OriginalHopsToAIHomeworld = 0;
            planet.InitialOwningAIFactionIndex = faction.FactionIndex;
            foreach ( var ph in planet.PlanetsWithinXHops_NoFilters( -1 ) )
            {
                Planet otherPlanet = ph.Planet;
                Int16 distance = ph.Hops;
                if ( otherPlanet == null )
                    continue;
                if ( otherPlanet.PopulationType == PlanetPopulationType.AIHomeworld )
                {
                    otherPlanet.OriginalHopsToAIHomeworld = 0;
                    continue;
                }
                if ( otherPlanet.OriginalHopsToAIHomeworld > 0 &&
                     otherPlanet.OriginalHopsToAIHomeworld <= distance )
                    continue;
                otherPlanet.OriginalHopsToAIHomeworld = distance;
            }

            planet.MapGen_FullyUsingFaction = faction;
            planet.MapGen_IsFullyUsedByAFaction = true;

            return true;
        }
    }
}
