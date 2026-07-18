using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    //Potential Thought: allow pirates to get un-piratified?
    //More unit types are probably needed
    //
    //        Plan: Use norse-themed names
    //             Have a base type, then variants based on higher Resource costs
    //             Example: A Huscarl unit is the base, with the following possible variants:
    //                Enraged ==> extra red cost, more damage
    //                  Stout ==> extra green cost, more health
    //                  Sinister ==> extra black cost, fancier damage types
    //                  Fortified ==> extra blue cost, more shields
    //                  Spirited ==> extra white cost, utility

    /* Discussion of economics
       Terminus:
          When a terminus finishes warping in, it decides what colour it is (preferring Resource Types we need more of). 
          It gets the default conversion ratio and applies that, followed by any upgrades, to determine its current conversion rate.
          Note that when upgrading, the conversion ratio gets the default for that mark level, then applies upgrades. 

       Transport ships:
          A transport ship works as follows: If it's not doing anything, it checks all the Terminii for "Who has resources for me to pick up where another Transport isn't already en route"?
          Then it goes to a random Terminus and picks up the resources.
          It can also decide to go to another Terminus or to an Epistyle.
          It if decides on an Epistyle then it figures out which epistyle needs its resources most, and goes there.

       Epistyles:
          When an Epistyle is built it builds an DrawBag of all the possible things it can build. It will put in extra copies of some things.
          Note that we can't just use a GameEntityTypeData, so we'll instead use basically a Union of "Upgrade|TypeData"
          If an Epistyle doesn't have something its building, it picks at random from its draw bag; exception: if there are a lot of Terminii but few Transports then it will just build a transport.
          An Epistyle has a flag for "How much it wants each resource type". The longer an Epistyle goes w/o building something the higher priority it gets, so no starvation
          
     */

    public abstract class DarkZenithFactionDeepInfoRoot : ExternalFactionDeepInfoRoot
    {
        //Set immediately before WorkingDestinations.Sort(...) so the comparison can be a non-capturing
        //static delegate.  [ThreadStatic] for safety since this is sim-context code.
        [ThreadStatic] private static DarkZenithPerUnitBaseInfo cb_dzSortData;
        [ThreadStatic] private static List<SafeSquadWrapper> cb_dzSortTransports;
        //We definitely don't want the main DZ and the Svikari running at the same time, that's likely LRP conflicts.

        public abstract bool IsSvikari { get; }
        public abstract bool IsHumanSidekick { get; }

        public DarkZenithFactionBaseInfoRoot BaseInfo;
        public static readonly List<SafeSquadWrapper> WorkingFlagshipsList = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 150, "DarkFactionDeepInfoRoot-WorkingFlagshipsList" );


        protected override void Cleanup() 
        {
            this.BaseInfo = null;
            
            TeamsAimedAtPlanet.Clear();
            WorkingEntityList.Clear();
            WorkingFlagshipsList.Clear();
            //it does not seem like anything is not already cleared on a per-call basis

            this.SubCleanup();
        }
        protected abstract void SubCleanup();

        protected override int MinimumSecondsBetweenLongRangePlannings => 3;
        public readonly ProtectedValDictionary<Planet, FireteamRegiment> TeamsAimedAtPlanet = ProtectedValDictionary<Planet, FireteamRegiment>.Create_WillNeverBeGCed( 100, "DarkZenithFactionDeepInfoRoot-TeamsAimedAtPlanet" );

        public override void SeedStartingEntities_EarlyMajorFactionClaimsOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            //Everything handled later
        }
        public override void DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly( ref int debugStage, GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull,
              Faction factionThatKilledEntity, Faction entityOwningFaction, int numExtraStacksKilled, ArcenHostOnlySimContext Context )
        {
            //We attribute all kills to the flagship
            int debugCode = 0;
            try{
                debugCode = 100;
            GameEntity_Squad killingSquad = null;
            if ( FiringSystemOrNull != null )
            {
                killingSquad = FiringSystemOrNull.ParentEntity;
            }
            debugCode = 200;
            if ( killingSquad == null )
                return;
            debugCode = 300;
            FleetMembership fMem = killingSquad.FleetMembership;
            if ( fMem == null )
                return;
            debugCode = 400;
            Fleet fleet = fMem.Fleet;
            if ( fleet == null )
                return;
            debugCode = 500;
            GameEntity_Squad flagship = fleet.Centerpiece.GetSquad();
            if ( flagship == null )
                return;
            debugCode = 600;
            if ( flagship.TypeData.KillsToTriggerTransformation <= 0 ||
                 flagship.TypeData.TransformAfterKills == null )
                return;
            debugCode = 700;
            DarkZenithPerUnitBaseInfo data = flagship.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
            if ( data == null )
                return;
            data.UnitsKilled++;
            } catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly Dyson Sidekick debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            // if ( entity == null )
            //     return;
            // GameEntityTypeData entityType = entity.TypeData;
            // if ( entityType == null )
            //     return;
            // switch ( entityType.SpecialType )
            // {
            //     case SpecialEntityType.AICommandStationOriginal:
            //       World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Dyson_Stronghold", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            //       break;
            //     case SpecialEntityType.GuardPost:
            //     case SpecialEntityType.DireGuardPost:
            //       World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Dyson_GuardPosts", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            //         this.ConsiderReanimatingGuardPost( entity, entityType, Context );
            //         break;
            // }
        }
        private void HandleInvasionInitialization( ArcenHostOnlySimContext Context )
        {
            if (BaseInfo.HasDoneInvasionInit)
            {
                if ( this.IsSvikari )
                    BaseInfo.JoinAlliedFactions = true; //this is a bit of a hack, because I've seen this sometimes not be set properly
                return;
            }
            BaseInfo.HasDoneInvasionInit = true;
            //pick the initial time, then add some randomness
            string allegiance = AttachedFaction.BaseInfo.Allegiance;
            string invasionTime = this.IsSvikari ? "Join Allied Factions" : AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "InvasionTime", true );
            if ( invasionTime == "Join Allied Factions" && allegiance == "黑暗同盟" )
                invasionTime = "Mid Game"; //override; join allied faction isn't meaningful with Dark Alliance

            if ( invasionTime == "Join Allied Factions" || invasionTime == "Svikari" )
            {
                AttachedFaction.InvasionTime = -1;
                BaseInfo.JoinAlliedFactions = true;
            }
            else if ( invasionTime == "立即" || invasionTime == "Full Invasion")
            {
                if ( this.IsHumanSidekick )
                    AttachedFaction.InvasionTime = 1;
                else
                    AttachedFaction.InvasionTime = 10;
            }
            else if ( invasionTime == "游戏早期" )
                AttachedFaction.InvasionTime = (3 * (60 * 60)) / 2; //1.5 hr in
            else if ( invasionTime == "游戏中期" )
                AttachedFaction.InvasionTime = (7 * (60 * 60)) / 2; //3.5 hr in
            else
                AttachedFaction.InvasionTime = 5 * (60 * 60); //5 hr in
            if ( AttachedFaction.InvasionTime > 1 )
            {
                if ( Context.RandomToUse.Next( 0, 100 ) < 50 )
                    AttachedFaction.InvasionTime += Context.RandomToUse.Next( 0, AttachedFaction.InvasionTime / 10 );
                else
                    AttachedFaction.InvasionTime -= Context.RandomToUse.Next( 0, AttachedFaction.InvasionTime / 10 );
            }


            if ( AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) &&
                 !BaseInfo.PlayerAllied && !BaseInfo.MinorFactionAllied )
            {
                AttachedFaction.InvasionTime = 4;
            }
        }
        public void DeployDragons( ArcenHostOnlySimContext Context )
        {

            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //No client stuff

            int debugCode = 0;
            try{
            ArcenDebugging.ArcenDebugLogSingleLine("Deploying " + BaseInfo.Difficulty.AIDragonsToSpawn + " dragons", Verbosity.DoNotShow );
            foreach ( GameEntity_Squad king in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                Faction kingOwner = king.GetFactionOrNull_Safe();
                if ( kingOwner.Type != FactionType.AI )
                    continue;

                debugCode = 900;
                AISentinelsFactionBaseInfo sentinelsBaseInfo = kingOwner.GetAISentinelsCoreData();
                Faction praetorian = sentinelsBaseInfo.SubFac_Praetorian;
                if ( praetorian == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "BUG: no praetorian guard was found while spawning dragons for " + kingOwner.GetDisplayName(), Verbosity.DoNotShow );
                    continue;
                }
                debugCode = 1000;
                AITypeData aiType = sentinelsBaseInfo.SentinelInfo.AIType;
                int numDragons = BaseInfo.Difficulty.AIDragonsToSpawn;
                if ( aiType.InternalName == "PraetorHard" || aiType.InternalName == "PraetorBrutal" )
                {
                    if ( Context.RandomToUse.Next( 0, 100 ) < 50 )
                        numDragons++;
                }
                debugCode = 1100;
                for ( int j = 0; j < numDragons; j++ )
                {
                    PlanetFaction pFaction = king.Planet.GetPlanetFactionForFaction( praetorian );
                    GameEntityTypeData dragonData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "AIDZDragon" );
                    GameEntity_Squad dragon = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, dragonData,
                                                                                               king.CurrentMarkLevel,
                                                                                               pFaction.FleetUsedAtPlanet, 0,
                                                                                               king.WorldLocation, Context, "DZDragon" );
                    if ( dragon != null )
                    {
                        dragon.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                        dragon.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
                    }
                }
            }
            } catch (Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception during DeployDragons debugcode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }

        public void LinkNewPlanets( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host

            List<Planet> oldPlanets = Planet.GetTemporaryPlanetList( "DZ-LinkNewPlanets-oldPlanets", 10f );
            if ( oldPlanets == null ) //blocked for teardown/shutdown; bail
                return;
            List<Planet> newPlanets = Planet.GetTemporaryPlanetList( "DZ-LinkNewPlanets-newPlanets", 10f );
            if ( newPlanets == null ) //blocked for teardown/shutdown; bail
            {
                Planet.ReleaseTemporaryPlanetList( oldPlanets );
                return;
            }

            foreach ( Planet planet in World_AIW2.Instance.CurrentGalaxy.Planets( false ) )
            {
                if ( planet.PopulationType == PlanetPopulationType.DarkZenith &&
                     (planet.GetControllingOrInfluencingFaction() == AttachedFaction ||
                      planet.UnderInfluenceOfFactionIndex.Contains( AttachedFaction.FactionIndex ) ) )
                    newPlanets.Add(planet);
                else
                    oldPlanets.Add(planet);
            }

            if ( newPlanets.Count == 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "No new planets were available to link for the DZ!", Verbosity.ShowAsError );
                return;
            }
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine("We have " + oldPlanets.Count + " orig planets and " + newPlanets.Count + " new planets", Verbosity.DoNotShow );
            //find the center of the new planets
            ArcenPoint centroid = Galaxy.FindCentroidOfPlanetList ( newPlanets );

            bool successfulLink = ConnectDZPlanets(World_AIW2.Instance.CurrentGalaxy, oldPlanets, newPlanets, centroid, false, 2, false, Context, true);
            if ( !successfulLink )
                return;
            //We've placed wormholes on the new planets, but we need to make sure we have wormholes on the
            //already existing planets that go to the new planets
            IWormholePlacer placer = new WormholePlacer_Default( FInt.FromParts( 0, 250 ), FInt.FromParts( 0, 750 ) );
            //int debugCode = 0;
            foreach ( Planet planet in World_AIW2.Instance.CurrentGalaxy.Planets( false ) )
            {
                if ( newPlanets.Contains(planet) )
                {
                    //if we are a new DZ planet, link us to an existing planet (if necessary)
                    for ( int i = 0; i < oldPlanets.Count; i++ )
                    {
                        Planet oldPlanet = oldPlanets[i];
                        if ( planet.GetIsDirectlyLinkedTo ( false, oldPlanet ) )
                        {
                            //this is a new planet connected to an old planet
                            int largerIndex = Math.Max( oldPlanet.Index, planet.Index );
                            int smallerIndex = Math.Min( oldPlanet.Index, planet.Index );
                            int seed = (largerIndex << 16) + smallerIndex;
                            ArcenPoint wormholePoint = placer.GetPointForWormhole( Context, planet, oldPlanet );
                            PlanetFaction pFaction = planet.GetFirstFactionOfType( FactionType.NaturalObject );
                            GameEntity_Other wormhole = GameEntity_Other.CreateOtherNew_CallFromHostOnly( pFaction, GameEntityTypeDataTable.Instance.DefaultWormholeType, wormholePoint, Context );
                            wormhole.SetLinkedPlanetIndex( oldPlanet.Index );
                        }
                    }
                    continue;
                }
                //we are an old planet, so link us to a new DZ planet (if necessary)
                for ( int i = 0; i < newPlanets.Count; i++ )
                {
                    //debugCode = 5200;
                    Planet newPlanet = newPlanets[i];
                    if ( planet.GetIsDirectlyLinkedTo(false, newPlanet) )
                    {
                        //debugCode = 5300;
                        //if we need a new wormhole, add one
                        int largerIndex = Math.Max( newPlanet.Index, planet.Index );
                        int smallerIndex = Math.Min( newPlanet.Index, planet.Index );
                        int seed = (largerIndex << 16) + smallerIndex;
                        ArcenPoint wormholePoint = placer.GetPointForWormhole( Context, planet, newPlanet );
                        PlanetFaction pFaction = planet.GetFirstFactionOfType( FactionType.NaturalObject );
                        GameEntity_Other wormhole = GameEntity_Other.CreateOtherNew_CallFromHostOnly( pFaction, GameEntityTypeDataTable.Instance.DefaultWormholeType, wormholePoint, Context );
                        if ( this.IsHumanSidekick )
                        {
                            planet.SetIntel( PlanetIntelLevel.PermanentlyWatched );
                            foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                            {
                                if ( neighbor.IntelLevel == PlanetIntelLevel.Unexplored )
                                {
                                    neighbor.SetIntel( PlanetIntelLevel.CurrentlyWatched );
                                }
                            }
                        }
                        wormhole.SetLinkedPlanetIndex( newPlanet.Index );
                    }
                }
            }
            World_AIW2.Instance.CurrentGalaxy.RecomputeDestinationIndexToWormholeMapping();
            World_AIW2.Instance.CurrentGalaxy.RecomputePlanetDistances();
            World_AIW2.Instance.CurrentGalaxy.RecomputeLinkedPathfindables();
            BaseInfo.HasLinkedPlanets = true;

            Planet.ReleaseTemporaryPlanetList( oldPlanets );
            Planet.ReleaseTemporaryPlanetList( newPlanets );
        }

        bool ConnectDZPlanets( Galaxy galaxy, List<Planet> currentPlanets, List<Planet> newPlanets, ArcenPoint centerOfNewPlanets, bool combinePlanets, int linksToMake, bool someRandomness, ArcenHostOnlySimContext Context, bool preferMoreHopsFromHomeworlds = false )
        {
            //this is mostly a copy of linkPlanetLists with some DZ specific modifications
            int debugStage = 0;
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine(" Trying to link planets lists, attempting to make " + linksToMake, Verbosity.DoNotShow );
            bool verboseDebug = false;
            List<Planet> planetsToAvoidIfPossible = Planet.GetTemporaryPlanetList( "DZ-ConnectDZPlanets-planetsToAvoidIfPossible", 10f );
            if ( planetsToAvoidIfPossible == null ) //blocked for teardown/shutdown; bail
                return false;
            Dictionary<Planet, Planet> PotentialConnections = Planet.GetTemporaryPlanetDictOfPlanets( "DZ-ConnectDZPlanets-PotentialConnections", 10f );
            if ( PotentialConnections == null ) //blocked for teardown/shutdown; bail
            {
                Planet.ReleaseTemporaryPlanetList( planetsToAvoidIfPossible );
                return false;
            }
            bool spawnNearPlayer = this.IsHumanSidekick;
            try
            {
                int requiredPlayerHops = 8;
                int requiredAIHops = 8;
                int retries = 100;
                do {
                    PotentialConnections.Clear();
                    bool avoidReusingPlanets = true;
                    bool useLargeRadius = true;
                    bool useExtraSmallRadius = false;
                    if ( retries < 50 )
                    {
                        avoidReusingPlanets = false; //give this a good try, but then expand the search
                        useLargeRadius = false;
                    }
                    if ( retries < 10 )
                    {
                        //Something has gone wrong, so just try to "make it work"
                        useExtraSmallRadius = true;
                    }
                    for ( int i = 0; i < currentPlanets.Count; i++ )
                    {
                        debugStage = 100;
                        Planet currPlanet = currentPlanets[i];
                        Planet closestPlanet = null;
                        int closestDist = -1;
                        if ( currPlanet.GetControllingOrInfluencingFaction().Type == FactionType.Player )
                        {
                            if ( avoidReusingPlanets || !spawnNearPlayer )
                                continue;
                        }
                        if ( currPlanet.PopulationType == PlanetPopulationType.AIBastionWorld ||
                             currPlanet.PopulationType == PlanetPopulationType.AIHomeworld )
                            continue;
                        if ( currPlanet.IsZenithArchitraveTerritory ||
                             currPlanet.IsZenithArchitraveHome )
                            continue;
                       if ( !World_AIW2.Instance.CurrentGalaxy.IsNomadGalaxy &&
                            currPlanet.TypeData.Type == PlanetType.Nomad )
                           continue;

                       if ( preferMoreHopsFromHomeworlds )
                       {
                            if ( currPlanet.OriginalHopsToHumanHomeworld <= requiredPlayerHops && currPlanet.OriginalHopsToHumanHomeworld > -1 && !spawnNearPlayer )
                            {
                                if ( verboseDebug )
                                    ArcenDebugging.ArcenDebugLogSingleLine(currPlanet.Name + " is " + currPlanet.OriginalHopsToHumanHomeworld + " hops from player homeworld, requirement is " + requiredPlayerHops, Verbosity.DoNotShow );
                                continue;
                            }
                            if ( currPlanet.OriginalHopsToAIHomeworld <= requiredAIHops )
                            {
                                if ( verboseDebug )
                                    ArcenDebugging.ArcenDebugLogSingleLine(currPlanet.Name + " is " + currPlanet.OriginalHopsToAIHomeworld + " hops from AI homeworld, requirement is " + requiredAIHops, Verbosity.DoNotShow );
                                continue;
                            }
                            if ( currPlanet.GetControllingFactionType() == FactionType.Player && requiredPlayerHops > 4 && !spawnNearPlayer )
                            {
                                if ( verboseDebug )
                                    ArcenDebugging.ArcenDebugLogSingleLine(currPlanet.Name + " is a player planet ", Verbosity.DoNotShow );
                                continue; //not too close to players untless we feel like we have to
                            }
                            if ( currPlanet.GetControllingFaction().SpecialFactionData.InternalName == "ZenithArchitrave" )
                            {
                                if ( verboseDebug )
                                    ArcenDebugging.ArcenDebugLogSingleLine(currPlanet.Name + " is a ZA planet ", Verbosity.DoNotShow );
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
                            ArcenDebugging.ArcenDebugLogSingleLine("\t" + currPlanet.Name + " is eligible, required player hops " + requiredPlayerHops + " and required AI hops " + requiredAIHops, Verbosity.DoNotShow );
                        //string debugPlanetName = "Lamport";
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
                                        if ( debug )
                                            ArcenDebugging.ArcenDebugLogSingleLine("\tSkipping newPlanet " + newPlanet.Name + " from pair " + pair.Key.Name +", " + pair.Value.Name +" due to reuse", Verbosity.DoNotShow );

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
                                int radius = 60;
                                if ( !useLargeRadius )
                                    radius = newPlanet.TypeData.IntraStellarRadius;
                                if ( useExtraSmallRadius )
                                    radius = 10;
                                if ( BadgerUtilityMethods.WouldLinkCrossOtherPlanets(currPlanet, newPlanet, galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs(), radius ) )
                                //if ( BadgerUtilityMethods.WouldLinkCrossOtherLinks(currPlanet, newPlanet, galaxy.GetRawListOfPlanetsForMapGenAndNotMuchElseOrItBreaksYourLegs(), 40, 1 ) )
                                {
                                    if ( debug ) // && ( currPlanet.Name == debugPlanetName ||
                                               //      newPlanet.Name == debugPlanetName ) )
                                        ArcenDebugging.ArcenDebugLogSingleLine("\tSkipping currPlanet " + currPlanet.Name + " <-> " + newPlanet.Name + " as link would cross others; radius is " + radius, Verbosity.DoNotShow );
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
                    if ( requiredPlayerHops > 0 )
                        requiredPlayerHops--;
                    if ( requiredAIHops > 0 )
                        requiredAIHops--;
                }while ( PotentialConnections.Count < linksToMake && retries-- > 0);
                if ( PotentialConnections.Count == 0 )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("No potential connections found!", Verbosity.DoNotShow );

                    return false;
                }
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("Found " + PotentialConnections.Count + " potential connections, and using hops " + requiredPlayerHops + " between lists with " + currentPlanets[0].Name + " and " + newPlanets[0].Name, Verbosity.DoNotShow );
                planetsToAvoidIfPossible.Clear(); //clear this for reuse
                debugStage = 400;
                int preferredHomeworldHops = 7; //if you are closer than this many hops to a homeworld, we deprioritize this planet (if requested)
                FInt dropoff = FInt.FromParts( 1, 500 ); //higher scores are less important planets
                List<KeyValuePair<Planet, Planet>> sortedListOfPotentials = PotentialConnections.SortIntoList( delegate ( KeyValuePair<Planet, Planet> L, KeyValuePair<Planet, Planet> R)
                {
                    int lDist = Mat.DistanceBetweenPointsImprecise( L.Key.GalaxyLocation, L.Value.GalaxyLocation  );
                    int rDist = Mat.DistanceBetweenPointsImprecise( R.Key.GalaxyLocation, R.Value.GalaxyLocation  );
                    if ( preferMoreHopsFromHomeworlds )
                    {
                        //note we sneakily slightly prefer to be  further from human homeworlds
                        int lHops = Math.Max(L.Key.OriginalHopsToHumanHomeworld + 1, L.Key.OriginalHopsToAIHomeworld); //average the hops to both
                        int rHops = Math.Max(R.Key.OriginalHopsToHumanHomeworld + 1, R.Key.OriginalHopsToAIHomeworld); //average the hops to both
                        for ( int i = 0; i < preferredHomeworldHops - lHops ; i++ )
                            lDist = (lDist * dropoff).IntValue;
                        for ( int i = 0; i < preferredHomeworldHops - rHops ; i++ )
                            rDist = (rDist * dropoff).IntValue;
                    }
                    return rDist.CompareTo(lDist);
                } );
                debugStage = 500;

                //No more use of PotentialConnections below this point!
                //It should all be using sortedListOfPotentials now.

                if ( sortedListOfPotentials.Count < linksToMake )
                    linksToMake = sortedListOfPotentials.Count;
                if ( debug )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Potential linkes are ", Verbosity.DoNotShow );
                    for ( int i = 0; i < sortedListOfPotentials.Count; i++ )
                    {
                        KeyValuePair<Planet, Planet> pair = sortedListOfPotentials[i];
                        ArcenDebugging.ArcenDebugLogSingleLine("\t " + pair.Key.Name + " -> " +pair.Value.Name, Verbosity.DoNotShow );
                    }
                }
                bool avoidDoublesToSamePlanet = true;

                Dictionary<Planet, Planet> workingUsedPairs = Planet.GetTemporaryPlanetDictOfPlanets( "MapgenBadgerUtilityMethods-linkPlanetLists-workingUsedPairs", 10f );
                if ( workingUsedPairs == null ) //blocked for teardown/shutdown; bail
                    return false;

                retries = 10;
                do{
                    //we made an effort to avoid double links above, lets just do it again too here
                    debugStage = 600;

                    for ( int i = sortedListOfPotentials.Count - 1; i >= 0 ; i-- )
                    {
                        debugStage = 700;
                        KeyValuePair<Planet, Planet> pair = sortedListOfPotentials[i];
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("Evaluating " + pair.Key.Name + " -> " + pair.Value.Name +", avoid doubles? " + avoidDoublesToSamePlanet + " currently used pairs: " + workingUsedPairs.Count, Verbosity.DoNotShow );
                        debugStage = 710;
                        bool skipThis = false;
                        if ( avoidDoublesToSamePlanet )
                        {
                            debugStage = 720;
                            foreach ( KeyValuePair<Planet, Planet> usedPair in workingUsedPairs )
                            {
                                debugStage = 730;
                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine("Checking for overlap against " + usedPair.Key.Name + " -> " + usedPair.Value.Name, Verbosity.DoNotShow );
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
                                        ArcenDebugging.ArcenDebugLogSingleLine("Checking for overlap against already used planet " + planetsToAvoidIfPossible[k].Name, Verbosity.DoNotShow );

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
                                ArcenDebugging.ArcenDebugLogSingleLine("Skipping " + pair.Key.Name + " --> " + pair.Value.Name + " reuse", Verbosity.DoNotShow );
                            continue;
                        }
                        if ( Context == null || (Context != null && Context.RandomToUse.Next(0, 100) < 70 ) ||//some randomness if desired
                             (i > linksToMake ) )  //make sure not to skip possible links if we don't have any to spare
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine("Linking " + pair.Key.Name + " --> " + pair.Value.Name, Verbosity.DoNotShow );

                            debugStage = 800;
                            pair.Key.AddLinkTo( pair.Value );
                            workingUsedPairs[pair.Key] = pair.Value;
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine("\tused pairs: " + workingUsedPairs.Count, Verbosity.DoNotShow );
                            linksToMake--;
                            if ( planetsToAvoidIfPossible != null )
                                planetsToAvoidIfPossible.Add( pair.Key );
                        }
                        if ( linksToMake <= 0 )
                            break;
                        debugStage = 850;
                    }
                    if ( retries < 5)
                        avoidDoublesToSamePlanet = false; //we've given it a few tries, lets be more inclusive now
                }while(linksToMake > 0 && retries-- > 0 );
                debugStage = 900;

                Planet.ReleaseTemporaryPlanetDictOfPlanets( workingUsedPairs );
                return true;
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "linkPlanetLists error at debugStage " + debugStage + " \n" + e );
            }
            finally
            {
                Planet.ReleaseTemporaryPlanetList( planetsToAvoidIfPossible );
                Planet.ReleaseTemporaryPlanetDictOfPlanets( PotentialConnections );
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
            return true;
        }
        public void DoInvasion( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //running this on the clients would cause multiple invasions at once

            //There are two major pieces of work. The first is to decide where the planets go, and that happens here.
            //We then create a GameComm`and to do the actual planet/unit creation. We need to do this in a GameCommand for multiplayer.
            int debugCode = 0;
            bool debug = false;
            try
            {
                //ArcenDebugging.ArcenDebugLogSingleLine( "Do DZ invasion!", Verbosity.Chat );

                debugCode = 1000;
                AttachedFaction.InvasionTime = -1;
                BaseInfo.DoInvasionNow = false;
                if ( BaseInfo.PlayerAllied && !this.IsHumanSidekick )
                {
                    //this is done for post-beacon player-allied invasion
                    GameEntityTypeData terminusData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "WarpingInDZTerminus" );
                    foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                    {
                        if ( entity.GetFactionTypeSafe() == FactionType.Player )
                        {
                            GameEntity_Squad terminus = entity.Planet.Mapgen_SeedEntity( Context, AttachedFaction, terminusData, PlanetSeedingZone.MostAnywhere );
                            terminus.TransformsIntoAfterTime = "DZMetalTerminus";
                            terminus.SecondsTillTransformation = (Int16)Context.RandomToUse.Next( 5, 10 );
                        }
                    }
                    
                    return;
                }

                //I need an algorithm for picking the "center" of my DZ planetary group
                //Something like "Pick 8 possible spots in a big circle around the center
                bool hasFoundGoodPoint = false;
                int retries = 400;
                ArcenPoint galaxyCenter = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter;
                int numPlanetsToCheck = 5; //check the 5 closest planets to our potential start location (since we might connect to any of them)
                int minHopsFromPlayerOrAIHomeworld = 20;
                int maxHopsFromPlayerHomeworld = -1; //this is for the DZ Sidekick, and can override the distance requirements
                int hopsFromZA = 12; //we don't want the DZ attacking too close to a zenith architrave
                ArcenPoint potentialPoint = ArcenPoint.ZeroZeroPoint;
                int potentialDistance;
                int spaceForNewPlanets = 250;

                List<Planet> oldPlanets = Planet.GetTemporaryPlanetList( "DZ-DoInvasion-oldPlanets", 10f );
                if ( oldPlanets == null ) //blocked for teardown/shutdown; bail
                    return;
                List<Planet> newPlanets = Planet.GetTemporaryPlanetList( "DZ-DoInvasion-newPlanets", 10f );
                if ( newPlanets == null ) //blocked for teardown/shutdown; bail
                {
                    Planet.ReleaseTemporaryPlanetList( oldPlanets );
                    return;
                }

                AngleDegrees reverserAngle = AngleDegrees.Create( 180 );
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( planet.TypeData.Type == PlanetType.Nomad )
                        continue; //don't link to a nomad
                    oldPlanets.Add( planet );
                }

                if ( FactionUtilityMethods.Instance.GetNumZenithArchitraves() <= 0 )
                    hopsFromZA = -1;
                if ( this.IsHumanSidekick )
                {
                    minHopsFromPlayerOrAIHomeworld = -1;
                    maxHopsFromPlayerHomeworld = 3;
                }
                ArcenDebugging.ArcenDebugLogSingleLine("Doing full invasion logic with difficulty " + this.BaseInfo.Difficulty.InternalName, Verbosity.DoNotShow );
                debugCode = 1010;
                do
                {
                    //Find a good "Central Point" for the DZ planets
                    debugCode = 1100;
                    AngleDegrees potentialAngle = AngleDegrees.Create( Context.RandomToUse.Next( 1, 359 ) );
                    //find the planet furthest from the galaxy center within 30 degrees of my angle
                    int furthestPlanetDistance = -1;
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Finding furthest planet in a direction, starting at angle " + potentialAngle, Verbosity.DoNotShow );
                    foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                    {
                        AngleDegrees planetAngle = planet.GalaxyLocation.GetAngleToDegrees( galaxyCenter );
                        AngleDegrees difference = potentialAngle.GetDeltaNeededToGetToOther( planetAngle );
                        int planetDistance = planet.GalaxyLocation.GetDistanceTo( galaxyCenter, false );
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "\tPlanet " + planet.Name + " at " + planet.GalaxyLocation.ToString() + " is at angle " + planetAngle + " and has difference angle " + difference.ToString() + " and distance " + planet.GalaxyLocation.GetDistanceTo( galaxyCenter, false ), Verbosity.DoNotShow );
                        if ( difference.Tofloat() < 60 && planetDistance > furthestPlanetDistance )
                        {
                            furthestPlanetDistance = planetDistance;
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "\t\tFound new furthest planet " + planet.Name + " at " + furthestPlanetDistance, Verbosity.DoNotShow );
                        }
                    }
                    debugCode = 1200;
                    potentialDistance = furthestPlanetDistance + spaceForNewPlanets;

                    potentialPoint = galaxyCenter.GetPointAtAngleAndDistance( potentialAngle.Add( reverserAngle ), potentialDistance );

                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "\t\tcheckout potential point " + potentialPoint.ToString() + " at distance " + potentialDistance + " and angle " + potentialAngle + " real angle A " + potentialPoint.GetAngleToDegrees( galaxyCenter ).ToString() + " real angle B " + galaxyCenter.GetAngleToDegrees( potentialPoint ).ToString(), Verbosity.DoNotShow );
                    if ( maxHopsFromPlayerHomeworld >= 1 && retries % 20 == 0 )
                    {
                        maxHopsFromPlayerHomeworld++;
                    }
                    if ( minHopsFromPlayerOrAIHomeworld >= 1 && retries % 20 == 0 )
                    {
                        minHopsFromPlayerOrAIHomeworld--;
                        if ( hopsFromZA > 1 )
                            hopsFromZA--;
                    }
                    debugCode = 1300;

                    if ( IsPointValidDZStartLocation( potentialPoint, numPlanetsToCheck, minHopsFromPlayerOrAIHomeworld, maxHopsFromPlayerHomeworld, hopsFromZA, spaceForNewPlanets, Context, debug ) )
                    {
                        debugCode = 1310;
                        hasFoundGoodPoint = true;
                    }
                    if ( !hasFoundGoodPoint && debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "No valid planets, retry", Verbosity.DoNotShow );

                    debugCode = 1400;
                } while ( !hasFoundGoodPoint && retries-- > 0 );

                debugCode = 2000;
                if ( !hasFoundGoodPoint )
                {
                    Planet.ReleaseTemporaryPlanetList( oldPlanets );
                    Planet.ReleaseTemporaryPlanetList( newPlanets );
                    ArcenDebugging.ArcenDebugLogSingleLine( "No suitable Dark Zenith spawning point was found", Verbosity.Chat );
                    return;
                }

                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Found potential point " + potentialPoint.ToString() + ",  remaining retries: " + retries, Verbosity.DoNotShow );
                //Start building the GameCommand. Note that all the unit population code is done in the GameCommand code
                //note BadgerUtilityMethods is the "mapgen" utility methods, not the SpecialFaction utility methods. The name should probably be changed
                debugCode = 2050;
                GameCommand createCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.CreateNewPlanet], GameCommandSource.AnythingElse );
                debugCode = 2070;
                if ( createCommand == null )
                {
                    Planet.ReleaseTemporaryPlanetList( oldPlanets );
                    Planet.ReleaseTemporaryPlanetList( newPlanets );
                    throw new Exception( "Could not create a GameCommand for CreateNewPlanet" );
                }
                createCommand.RelatedFactionIndex = AttachedFaction.FactionIndex;
                debugCode = 2100;
                //this is where we choose where the actual planets will go
                ThrowawayListCanMemLeak<ArcenPoint> pointsForNewPlanets = null;
                if ( Context.RandomToUse.Next( 0, 100 ) < 50 )//spawn different DZ planet arrangements
                    pointsForNewPlanets = BadgerUtilityMethods.addCircularPoints( BaseInfo.Difficulty.PlanetsToSpawn, Context, potentialPoint, spaceForNewPlanets, null );
                else
                    pointsForNewPlanets = BadgerUtilityMethods.addPointsInCircle( BaseInfo.Difficulty.PlanetsToSpawn, Context, potentialPoint, spaceForNewPlanets, 80, null );
                debugCode = 2200;
                createCommand.RelatedPoints.Add( potentialPoint ); //this is the first entry in the list
                for ( int i = 0; i < pointsForNewPlanets.Count; i++ )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "\tCreating a planet at " + pointsForNewPlanets[i].ToString(), Verbosity.DoNotShow );
                    createCommand.RelatedPoints.Add( pointsForNewPlanets[i] ); //I'm sure this is a bit ineffiecient
                }

                //ArcenDebugging.ArcenDebugLogSingleLine( "DZ points: " + pointsForNewPlanets.Count, Verbosity.Chat );

                debugCode = 2300;
                createCommand.RelatedMagnitude = (int)PlanetPopulationType.DarkZenith;
                bool playAudioEffectForCommand = false;
                debugCode = 2400;
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, createCommand, playAudioEffectForCommand );
                AttachedFaction.HasBeenSeenByPlayer = true; //it's immediately obvious...
                if ( this.IsHumanSidekick )
                    BaseInfo.TimeToLinkPlanets = World_AIW2.Instance.GameSecond;
                else
                    BaseInfo.TimeToLinkPlanets = World_AIW2.Instance.GameSecond + BaseInfo.TimeBetweenPlanetSpawnAndLinking;
                if ( AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) )
                    BaseInfo.TimeToLinkPlanets = World_AIW2.Instance.GameSecond + 10;

                Planet.ReleaseTemporaryPlanetList( oldPlanets );
                Planet.ReleaseTemporaryPlanetList( newPlanets );
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Hit an exceprion during DoInvasion, debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }

        private readonly List<KeyValuePair<Planet, int>> PlanetDistanceList = List<KeyValuePair<Planet, int>>.Create_WillNeverBeGCed( 500, "DarkZenithFactionDeepInfoRoot-PlanetDistanceList" );

        private bool IsPointValidDZStartLocation( ArcenPoint potentialPoint, int numPlanetsToCheck, int minHopsFromPlayerOrAIHomeworld, int maxHopsFromPlayerHomeworld, int hopsFromZA, int minDistFromAnyPlanet, ArcenHostOnlySimContext Context, bool inputDebug = false )
        {
            //the rule is "If there are >= numPlanetsThatCantBePlayer player planets near the potential point, or if there are any planets within minDistFromAnyPlanet range
            //of the potential point, this is not a valid point"
            PlanetDistanceList.Clear();
            bool didFail = false;
            bool debug = inputDebug;
            if ( debug )
                ArcenDebugging.LogSingleLine("Checking if " + potentialPoint + " is a good place for the DZ to spawn", Verbosity.DoNotShow );
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                int distance = planet.GalaxyLocation.GetDistanceTo( potentialPoint, false );
                if ( distance < minDistFromAnyPlanet )
                {
                    didFail = true;
                    break;
                }
                PlanetDistanceList.Add( new KeyValuePair<Planet, int>( planet, distance ) );
            }
            if ( didFail )
            {
                if ( debug )
                    ArcenDebugging.LogSingleLine("fail path A", Verbosity.DoNotShow );
                return false;
            }
            PlanetDistanceList.Sort( static delegate ( KeyValuePair<Planet, int> L, KeyValuePair<Planet, int> R )
             {
                 return L.Value.CompareTo( R.Value );
             } );
            //int playerPlanets = 0;
            for ( int i = 0; i < numPlanetsToCheck; i++ )
            {
                KeyValuePair<Planet, int> pair = PlanetDistanceList[i];

                Planet checkPlanet = pair.Key;
                //first check if we are really close to a homeworld
                if ( minHopsFromPlayerOrAIHomeworld > 0 )
                {
                    if ( debug )
                        ArcenDebugging.LogSingleLine("hops from " + checkPlanet.Name + " to a human homeworld: " + checkPlanet.OriginalHopsToAIHomeworld, Verbosity.DoNotShow );
                    //too close to player homeworld
                    if ( checkPlanet.OriginalHopsToHumanHomeworld > 0 && checkPlanet.OriginalHopsToHumanHomeworld < minHopsFromPlayerOrAIHomeworld )
                        return false;
                    //too close to an ai homeworld
                    if ( checkPlanet.OriginalHopsToAIHomeworld > 0 && checkPlanet.OriginalHopsToAIHomeworld < minHopsFromPlayerOrAIHomeworld )
                        return false;
                }

                bool tooCloseToPlayer = false;
                bool tooCloseToZA = false;
                bool closeEnoughToPlayer = false;
                if ( minHopsFromPlayerOrAIHomeworld > 0 )
                {
                    //I think this variable should really be "hopsFromAnyPlayerPlanetOrAIHomeworld"
                    foreach ( Planet.PlanetAtHopDistance _phd in checkPlanet.PlanetsWithinXHops_NoFilters( (Int16)minHopsFromPlayerOrAIHomeworld ) )
                    {
                        Planet planet = _phd.Planet;
                        if ( planet.GetControllingOrInfluencingFaction().Type == FactionType.Player )
                        {
                            tooCloseToPlayer = true;
                            break;
                        }
                    }
                }
                if ( hopsFromZA > 0 && !tooCloseToPlayer && !this.IsHumanSidekick )
                {
                    //if we are checking for the ZA (ie "there are ZAs in the galaxy") and haven't already found a reason to bail out, check now
                    //Note that the ZA check is disabled for human players
                    foreach ( Planet.PlanetAtHopDistance _phd in checkPlanet.PlanetsWithinXHops_NoFilters( (Int16)hopsFromZA ) )
                    {
                        Planet planet = _phd.Planet;
                        if ( ZenithArchitraveFactionBaseInfo.IsPlanetInAnyZATerritory( planet ) )
                        {
                            tooCloseToZA = true;
                            break;
                        }
                    }
                }
                if ( maxHopsFromPlayerHomeworld > 0 && !closeEnoughToPlayer )
                {
                    //maxHopsFromPlayerHomeworld is for the DZ Sidekick, so it's always run at game start time (Immediate Invasion)
                    WorkingFlagshipsList.Clear();
                    FactionUtilityMethods.Instance.findAllHumanKings(WorkingFlagshipsList);
                    if ( WorkingFlagshipsList.Count == 0 )
                    {
                        //It's possible there is no player King (Ark Empire triggers this apparently?), so assume the player can come to us
                        closeEnoughToPlayer = true;
                    }
                    else
                    {

                        foreach ( Planet.PlanetAtHopDistance _phd in checkPlanet.PlanetsWithinXHops_NoFilters( (Int16)maxHopsFromPlayerHomeworld ) )
                        {
                            Planet planet = _phd.Planet;
                            if ( planet.GetControllingOrInfluencingFaction().Type == FactionType.Player ||
                                 FactionUtilityMethods.Instance.HasPlayerKing(planet))
                            {
                                closeEnoughToPlayer = true;
                                break;
                            }
                        }
                    }
                }
                if ( tooCloseToPlayer || tooCloseToZA )
                {
                    if ( debug )
                        ArcenDebugging.LogSingleLine("too close to player? " + tooCloseToPlayer + " too close to ZA? " + tooCloseToZA, Verbosity.DoNotShow );
                    return false;
                }
                if ( maxHopsFromPlayerHomeworld > 0 && !closeEnoughToPlayer)
                {
                    if ( debug )
                        ArcenDebugging.LogSingleLine("Not close enough to player", Verbosity.DoNotShow );
                    return false;
                }
            }
            return true;
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f ) : null;
            #endregion

            //Invasion stuff
            int debugCode = 0;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                debugCode = 100;
                if ( BaseInfo.DZIsSuppressed.Display )
                {
                    BaseInfo.DoInvasionNow = true; //we want to do the invasion "as soon as the beacon is gone"
                    return;
                }
                HandleInvasionInitialization( Context );
                if ( (BaseInfo.DoInvasionNow ||
                       (AttachedFaction.InvasionTime > 0 &&
                        World_AIW2.Instance.GameSecond >= AttachedFaction.InvasionTime)) )
                {
                    DoInvasion( Context );
                    DeployDragons( Context );
                    return;
                }
                if ( !BaseInfo.HasLinkedPlanets &&
                     BaseInfo.TimeToLinkPlanets > 0 &&
                     BaseInfo.TimeToLinkPlanets <= World_AIW2.Instance.GameSecond &&
                     (AttachedFaction.SpecialFactionData.FullInvasionMode || this.IsHumanSidekick ) )
                {
                    LinkNewPlanets( Context );
                }

                debugCode = 200;
                if ( BaseInfo.HasLinkedPlanets &&
                     !BaseInfo.PlayerAllied && !BaseInfo.MinorFactionAllied )
                    UpdateTerritorialSphere( Context );
                HandleInitialInvasionBuffs( Context ); //when we are doing the first invasion, sometimes we need to give the DZ some extra ships...
                HandleSpireBuffs( Context ); //Buffs for when the Fallen Spire is in play
                CheckIfHomeworldsUnderAttack( Context );
                //Economic Stuff
                HandleHarvestersSim( Context );
                HandleTransportsSim( Context, pathingCacheData );
                HandlePrivateersSim( Context );
                HandleConstructorsSim( Context );
                debugCode = 300;
                if ( this.AttachedFaction.IsVassal )
                {
                    //If we have any economic missions, assign epsityles to them if possible
                    UpdateVassalEconomicMissions( Context );
                }

                HandleTerminiiAndEpistyles( Context, pathingCacheData ); //the build paths for terminals and epistyles match right now
                HandleJormuandr( Context ); //This basically just toggles their dormancy
                debugCode = 300;
                HandleUpgrades( Context );
                JoinAlliesIfNecessary( Context );
                ConvertEpistylesToPirate( Context );
                HandleFimbulwinter( Context );
                UpdateJournals( Context );
                HandleExos( AttachedFaction, Context );
                UpdatePlanetBGs();
                UpdateFlagshipsForSidekick( Context );
                UpdateRallyForSidekick(Context);
                HandleNadirForSidekick(Context);
                ConvertMerkismathrForSidekick(Context);
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in stage3 debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();

                #region Tracing
                FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
                #endregion
            }
        }
        #region Sidekicks (mod support)
        private void UpdateRallyForSidekick(ArcenHostOnlySimContext Context)
        {
            if (!this.IsHumanSidekick)
                return;
            if (this.BaseInfo.Flagships.Count == 0)
                return;
            if (!AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame("ShipsRallyToNearestFlagshipDZ"))
                return;
            int interval = 5 + World_AIW2.Instance.GameSecond / 600;
            if (interval > 30)
                interval = 30;
            if ( World_AIW2.Instance.GameSecond % interval == 0)
            {
                List<SafeSquadWrapper> unRallied = BaseInfo.UnRalliedCombatShips.GetDisplayList();
                for ( int i = 0; i < unRallied.Count; i++ )
                {
                    GameEntity_Squad entity = unRallied[i].GetSquad();
                    if ( entity == null )
                        continue;
                    DarkZenithPerUnitBaseInfo data = entity.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    if ( data.HasRalliedToFlagship )
                        continue;
                    Fleet fleet = entity.FleetMembership.Fleet;
                    if ( fleet == null )
                        continue;
                    GameEntity_Squad flagship = fleet.Centerpiece.GetSquad();
                    if ( flagship == null )
                        continue;
                    if ( flagship.Planet == entity.Planet )
                    {
                        data.HasRalliedToFlagship = true;
                    }
                }
            }
        }
        private void UpdateFlagshipsForSidekick( ArcenHostOnlySimContext Context )
        {
            if ( !this.IsHumanSidekick )
                return;
            if ( this.BaseInfo.Flagships.Count == 0 )
            {
                //Spawn the initial flagship
                string startingFlagship = this.AttachedFaction.GetStringValueForCustomFieldOrDefaultValue("DZSidekickStartingFlagship", false);
                if (startingFlagship == "Random")
                {
                    int rand = Context.RandomToUse.Next(0, 100);
                    if (rand < 33)
                        startingFlagship = "Medusa";
                    else if ( rand < 66 )
                        startingFlagship = "Asclepius";
                    else
                        startingFlagship = "Chameleon";
                }
                GameEntityTypeData entityData = null;
                if ( startingFlagship == "Medusa" )
                {
                    entityData = GameEntityTypeDataTable.Instance.GetRowByName( "MedusaFlagship" );
                }
                else if ( startingFlagship == "Asclepius" )
                {
                    entityData = GameEntityTypeDataTable.Instance.GetRowByName( "AsclepiusFlagship" );
                }
                else
                {
                    entityData = GameEntityTypeDataTable.Instance.GetRowByName( "ChameleonFlagship" );
                }

                if ( entityData == null )
                {
                    throw new Exception ("Could not find flagship " + startingFlagship);
                }
                List<SafeSquadWrapper> terminii = this.BaseInfo.Terminii.GetDisplayList();
                for ( int i = 0; i < terminii.Count; i++ )
                {
                    GameEntity_Squad terminus = terminii[i].GetSquad();
                    if ( terminus == null )
                        continue;
                    PlanetFaction pFaction = terminus.PlanetFaction;
                    ArcenPoint spawnLocation = terminus.Planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                    GameEntity_Squad flagship = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                        pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "SvikariFlagship" );
                    Fleet fleet = flagship.GetFleetOrNull_Safe();
                    if ( fleet != null )
                    {
                        GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );

                        command.RelatedIntegers.Add( fleet.FleetID ); //FleetID
                        command.RelatedIntegers.Add( PlayerAccount.Local.PlayerPrimaryKeyID );
                        command.RelatedString = "ToggleIsFleetOnPlayerWatchlist";
                        command.RelatedBool = true;
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                        fleet.IsFleetInTransportLoadMode = false;
                    }

                    return;
                }
            }

            if ( this.BaseInfo.Flagships.Count == 0 )
                return;

            List<SafeSquadWrapper> flagshipsForBaseInfo = this.BaseInfo.Flagships.GetDisplayList();
            for ( int i = 0; i < flagshipsForBaseInfo.Count; i++ )
            {
                GameEntity_Squad flagship = flagshipsForBaseInfo[i].GetSquad();
                if ( flagship == null )
                    continue;
                Fleet fleet = flagship.GetFleetOrNull_Safe();
                if ( fleet != null && !(fleet.BaseInfo is DarkZenithMobileFleetBaseInfo) )
                    fleet.CreateExternalBaseInfo<DarkZenithMobileFleetBaseInfo>( "DarkZenithMobileFleetBaseInfo" );
            }

            //this is costly, since we're iterating over all the units, so only once every 10 seconds after the beginning of the game.
            //For balance reasons We may want to create bonus units early game, so do check regularly early game (also there are fewer units then, so less performance hit)
            if ( World_AIW2.Instance.GameSecond > 600 && World_AIW2.Instance.GameSecond % 10 != 0 )
                return; 
            List<SafeSquadWrapper> flagships = this.BaseInfo.Flagships.GetDisplayList();
            for (int i = 0; i < flagships.Count; i++)
            {
                GameEntity_Squad flagship = flagships[i].GetSquad();
                if (flagship == null)
                    continue;
                DarkZenithPerUnitBaseInfo data = flagship.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                if (data == null)
                    continue;
                //ArcenDebugging.LogSingleLine(flagship.ToStringWithPlanet() + " has killed " + data.UnitsKilled + " and for transformation: " + flagship.TypeData.KillsToTriggerTransformation, Verbosity.DoNotShow );
                if ( flagship.TypeData.KillsToTriggerTransformation <= 0 )
                    continue;
                if ( data.UnitsKilled < flagship.TypeData.KillsToTriggerTransformation)
                    continue;
                if ( flagship.TypeData.TransformAfterKills != null )
                {
                    GameEntityTypeData entityData = flagship.TypeData.TransformAfterKills;
                    flagship.TransformInto(Context, entityData, 1, true);
                }

            }
            List<SafeSquadWrapper> looseShips = BaseInfo.LooseFleetCombatShips.GetDisplayList();
            for ( int i = 0; i < looseShips.Count; i++ )
            {
                GameEntity_Squad entity = looseShips[i].GetSquad();
                if ( entity == null )
                    continue;
                //ArcenDebugging.ArcenDebugLogSingleLine(entity.ToStringWithPlanet() + " needs to be assigned to a flagships", Verbosity.DoNotShow );
                bool omitMoon = true;
                bool omitKing = true;
                GameEntity_Squad closestFlagship = FactionUtilityMethods.Instance.GetNearestFlagshipToPlanet_OrNull( this.AttachedFaction, entity.Planet, Context, WorkingFlagshipsList, omitMoon, omitKing );
                if ( closestFlagship == null || closestFlagship.FleetMembership == null || closestFlagship.FleetMembership.Fleet == null )
                    continue;
                FleetMembership originalMem = entity.FleetMembership;
                if ( originalMem != null )
                    entity.FleetMembership.RemoveEntity( entity, false );
                FleetMembership mem = closestFlagship.FleetMembership.Fleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( entity.TypeData );
                mem.AddEntityToFleetMembership( entity, "DarkZenithSidekickFleet" );
                SquadRegistryInfo registryInfo = entity.GetCentralRegistryInfo_Expensive();
                if ( registryInfo != null )
                    registryInfo.Fleet = closestFlagship.FleetMembership.Fleet;
            }
        }
        private void HandleNadirForSidekick(ArcenHostOnlySimContext Context)
        {
            int debugCode = 0;
            try{
                if ( !this.IsHumanSidekick )
                    return;
                // Per-entity spawn checks use baseLifetime, not GameSecond, so we can only skip
                // on a coarse interval when no bases/spawners exist yet (global intervals are all multiples of 10).
                if ( BaseInfo.NadirBases.Count == 0 && BaseInfo.NadirSpawners.Count == 0 && World_AIW2.Instance.GameSecond % 10 != 0 )
                    return;
                //Base settings, to be modified by DZ Difficulty
                //this way I can avoid the hassle of making a newtable
                debugCode = 100;
                bool debug = false;
                int intervalForNadirBaseSpawn = 1500;
                int intervalForNadirSpawnerSpawn = 600;
                int earlySpawnerSpawnTime = -1; //Only for higher difficulties
                int intervalForNadirTierTwo = 300;
                int intervalForNadirTierOne = 80;
                int markupInterval = 700;

                if (  this.AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "InvasionTime", true ) == "Full Invasion" )
                {
                    //The DZ is much more powerful in full invasion mode
                    intervalForNadirBaseSpawn = 1200;
                    intervalForNadirTierTwo = 240;
                    intervalForNadirTierOne = 60;
                    intervalForNadirSpawnerSpawn = 350;
                    markupInterval = 700;
                    if ( BaseInfo.NadirStrength == 6 )
                        earlySpawnerSpawnTime = 300;
                    else if ( BaseInfo.NadirStrength > 6 )
                        earlySpawnerSpawnTime = 350;
                }
                int timeAdjust = 0;
                
                if ( BaseInfo.NadirStrength < 5 )
                {
                    timeAdjust += (BaseInfo.NadirStrength * 5);
                }
                if ( BaseInfo.NadirStrength > 5 )
                {
                    timeAdjust -= BaseInfo.NadirStrength;
                }
                intervalForNadirTierOne += timeAdjust;
                if ( debug && World_AIW2.Instance.GameSecond % 20 == 0 )
                {
                    ArcenDebugging.LogSingleLine("Pre Modifier: nadir strength: " + BaseInfo.NadirStrength + " Game Second " + World_AIW2.Instance.GameSecond + " spawn interval " + intervalForNadirBaseSpawn + " Svikari? " + this.IsSvikari, Verbosity.DoNotShow );
                }
                //The difficulty can also be adjusted by faction intensity,
                //but lets get a sense of balance right now
                // int difficultyModifier = BaseInfo.Intensity - 5;
                // intervalForNadirBaseSpawn -= 40 * difficultyModifier;
                // intervalForNadirTierTwo -= 6 * difficultyModifier;
                // intervalForNadirTierOne -= 3 * difficultyModifier;
                if ( debug && World_AIW2.Instance.GameSecond % 20 == 0 )
                {
                    ArcenDebugging.LogSingleLine("Post Modifier: Game Second " + World_AIW2.Instance.GameSecond + " spawn interval " + intervalForNadirBaseSpawn + " svikari? " + this.IsSvikari, Verbosity.DoNotShow );
                }
                debugCode = 100;
                if ( (World_AIW2.Instance.GameSecond % intervalForNadirBaseSpawn == 0 ) )
                {
                    debugCode = 200;
                    Faction aiFaction = World_AIW2.GetRandomAIFaction(Context);
                    if ( aiFaction == null )
                        return;
                    debugCode = 300;
                    Planet planet = GetPlanetForNadirBase(aiFaction, Context);
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction(aiFaction);
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "NadirBase" );
                    ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, pFaction.Faction.CurrentGeneralMarkLevel,
                        pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "SpawnNadirBase" );
                    World_AIW2.Instance.QueueChatMessageOrCommand( "A Nadir Base has spawn in the galaxy", ChatType.LogToCentralChat, string.Empty, null );
                    if ( debug )
                        ArcenDebugging.LogSingleLine("spawning " + newEntity.ToStringWithPlanet(), Verbosity.DoNotShow );
                }
                if ( (World_AIW2.Instance.GameSecond % intervalForNadirSpawnerSpawn == 0 ) || 
                    (World_AIW2.Instance.GameSecond == earlySpawnerSpawnTime ) )
                {
                    debugCode = 200;
                    Faction aiFaction = World_AIW2.GetRandomAIFaction(Context);
                    if ( aiFaction == null )
                        return;
                    debugCode = 300;
                    Planet planet = GetPlanetForNadirBase(aiFaction, Context);
                    debugCode = 310;
                    if ( planet == null )
                        throw new Exception("Could not find planet for nadir base");
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction(aiFaction);
                    string spawnerTag = "NadirSpawner";
                    if ( World_AIW2.Instance.GameSecond < 600 )
                    {
                        spawnerTag = "LowTierSpawner";
                    }
                    debugCode = 320;
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, spawnerTag);
                    debugCode = 330;
                    ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, pFaction.Faction.CurrentGeneralMarkLevel,
                        pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "SpawnSpawner" );
                    if ( debug )
                        ArcenDebugging.LogSingleLine("spawning " + newEntity.ToStringWithPlanet(), Verbosity.DoNotShow );
                }
                debugCode = 400;
                List<SafeSquadWrapper> bases = this.BaseInfo.NadirBases.GetDisplayList();
                for ( int i = 0; i < bases.Count; i++ )
                {
                    GameEntity_Squad nadirBase = bases[i].GetSquad();
                    if ( nadirBase == null )
                        continue;
                    debugCode = 500;
                    int baseLifetime = World_AIW2.Instance.GameSecond - nadirBase.GameSecondCreated;
                    if ( baseLifetime % markupInterval == 0 && nadirBase.CurrentMarkLevel < 7 )
                    {
                        nadirBase.SetCurrentMarkLevel( (byte)(nadirBase.CurrentMarkLevel + 1));
                    }
                    byte markLevelForSpawn = nadirBase.CurrentMarkLevel;
                    if ( markLevelForSpawn > 7 )
                        markLevelForSpawn = 7;
                    int unitsToSpawn = nadirBase.CurrentMarkLevel / 3;
                    if ( unitsToSpawn == 0 )
                        unitsToSpawn = 1;
                    debugCode = 600;
                    if ( debug )
                        ArcenDebugging.LogSingleLine("base lifetime for " + nadirBase.ToStringWithPlanet() + " is " + baseLifetime +", one " + intervalForNadirTierOne + ", two " + intervalForNadirTierTwo +". We are spawning " + unitsToSpawn + " units", Verbosity.DoNotShow );
                    if ( baseLifetime % intervalForNadirTierOne == 0 )
                    {
                        for ( int j = 0; j < unitsToSpawn; j++ )
                        {
                            GameEntityTypeData typedata = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "AntiDarkZenithTierOne");    
                            ArcenPoint spawnLocation = nadirBase.Planet.GetSafePlacementPoint_AroundEntity( Context, typedata, nadirBase, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 200 ) );
                            GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( nadirBase.PlanetFaction, typedata, markLevelForSpawn,
                                nadirBase.PlanetFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DarkZenith-NadirTierOne" );
                            if ( debug )
                            {
                                ArcenDebugging.LogSingleLine("TIER ONE spawning " + newEntity.ToStringWithPlanet(), Verbosity.DoNotShow );
                            }
                        }
                    }
                    debugCode = 700;
                    if ( baseLifetime % intervalForNadirTierTwo == 0 )
                    {
                        for ( int j = 0; j < unitsToSpawn; j++ )
                        {
                            GameEntityTypeData typedata = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "AntiDarkZenithTierTwo");
                            ArcenPoint spawnLocation = nadirBase.Planet.GetSafePlacementPoint_AroundEntity( Context, typedata, nadirBase, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 200 ) );
                            GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( nadirBase.PlanetFaction, typedata, markLevelForSpawn,
                                nadirBase.PlanetFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DarkZenith-NadirTierTwo" );
                            if ( debug )
                            {
                                ArcenDebugging.LogSingleLine("TIER TWO spawning " + newEntity.ToStringWithPlanet(), Verbosity.DoNotShow );
                            }

                        }
                    }
                }
                debugCode = 800;
                List<SafeSquadWrapper> spawners = this.BaseInfo.NadirSpawners.GetDisplayList();
                for ( int i = 0; i < spawners.Count; i++ )
                {
                    GameEntity_Squad nadirSpawner = spawners[i].GetSquad();
                    if ( nadirSpawner == null )
                        continue;
                    debugCode = 900;
                    int fortressLifetime = World_AIW2.Instance.GameSecond - nadirSpawner.GameSecondCreated;
                    byte markLevelForSpawn = nadirSpawner.PlanetFaction.Faction.CurrentGeneralMarkLevel;
                    int unitsToSpawn = nadirSpawner.CurrentMarkLevel / 3;
                    if ( unitsToSpawn == 0 )
                        unitsToSpawn = 1;
                    debugCode = 1000;
                    if ( debug )
                        ArcenDebugging.LogSingleLine("spawner lifetime for " + nadirSpawner.ToStringWithPlanet() + " is " + fortressLifetime +", one " + intervalForNadirTierOne + ". We are spawning " + unitsToSpawn + " units", Verbosity.DoNotShow );
                    if ( fortressLifetime % intervalForNadirTierOne == 0 )
                    {
                        for ( int j = 0; j < unitsToSpawn; j++ )
                        {
                            debugCode = 1100;
                            GameEntityTypeData typedata = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "AntiDarkZenithTierOne");
                            ArcenPoint spawnLocation = nadirSpawner.Planet.GetSafePlacementPoint_AroundEntity( Context, typedata, nadirSpawner, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 200 ) );
                            GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( nadirSpawner.PlanetFaction, typedata, markLevelForSpawn,
                                nadirSpawner.PlanetFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DarkZenith-NadirTierOne" );
                            if ( debug )
                            {
                                ArcenDebugging.LogSingleLine("TIER ONE spawning " + newEntity.ToStringWithPlanet(), Verbosity.DoNotShow );
                            }
                        }
                    }
                }
            }catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in HandleNadirForSidekick debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        private void ConvertMerkismathrForSidekick(ArcenHostOnlySimContext Context)
        {
            int debugCode = 0;
            try{
                if ( !this.IsHumanSidekick )
                    return;
                if ( World_AIW2.Instance.GameSecond % 30 != 0 )
                    return;
                //I find the default Merkismathr, with it's phasing, to be very annoying as a player
                //So, infrequently swap them with a non-phasing unit
                foreach ( GameEntity_Squad squad in World_AIW2.Instance.Squads( "DZMerkismathr" ) )
                {
                    if ( squad == null )
                        continue;
                    if ( squad.PlanetFaction.Faction != AttachedFaction )
                        continue;
                    GameEntityTypeData typedata = GameEntityTypeDataTable.Instance.GetRowByName( "DZSidekickMerkismathr" );
                    PlanetFaction pFaction = squad.PlanetFaction;
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typedata, 1,
                        pFaction.Faction.LooseFleet, 0, squad.WorldLocation, Context, "DarkZenith-MerkismathrConv" );
                    newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                    squad.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
                }
            }catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in ConvertMerkismathrForSidekick debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        public Planet GetPlanetForNadirBase( Faction faction, ArcenHostOnlySimContext Context )
        {
            //just stolen from the Instigators
            bool debug = false;
            var WorkingPlanetList = Planet.GetTemporaryPlanetList("DarkZenithFactionDeepInfoRoot-WorkingPlanetList",10.0f);
            if ( WorkingPlanetList == null ) //blocked for teardown/shutdown; bail
                return null;
            var WorkingPlanetListExploredOnly = Planet.GetTemporaryPlanetList("DarkZenithFactionDeepInfoRoot-WorkingPlanetListExploredOnly",10.0f);
            if ( WorkingPlanetListExploredOnly == null ) //blocked for teardown/shutdown; bail
            {
                Planet.ReleaseTemporaryPlanetList( WorkingPlanetList );
                return null;
            }

            FInt AIP = FactionUtilityMethods.Instance.GetCurrentAIP();
            Int16 minHopsFromHumanPlanet = -1;
            Int16 maxHopsFromHumanPlanet = -1;
            byte maxMarkLevel = 3;
            if ( AIP <= 150 )
            {
                minHopsFromHumanPlanet = 2;
                maxHopsFromHumanPlanet = 5;
                maxMarkLevel = 5;
            }
            else if ( AIP <= 300 )
            {
                minHopsFromHumanPlanet = 3;
                maxHopsFromHumanPlanet = 6;
                maxMarkLevel = 6;
            }
            else if ( AIP <= 600 )
            {
                minHopsFromHumanPlanet = 4;
                maxHopsFromHumanPlanet = 7;
                maxMarkLevel = 7;
            }
            else
            {
                minHopsFromHumanPlanet = 5;
                maxHopsFromHumanPlanet = 9;
                maxMarkLevel = 7;
            }
            int allowedRetries = 6; //was 100, and that's likely to break the game in the late game.
            int retries = 0;
            do
            {
                if ( retries > 0 )
                {
                    //the previous attempt was too restrictive
                    minHopsFromHumanPlanet--;
                    maxHopsFromHumanPlanet++;
                    maxMarkLevel++;
                }
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );

                    if ( planet.PopulationType == PlanetPopulationType.AIHomeworld ||
                         planet.PopulationType == PlanetPopulationType.AIBastionWorld )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because it was an AI homeworld at some point.", Verbosity.DoNotShow );
                        continue;
                    }

                    bool adjacentHomeworld = false;
                    foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                    {
                        if ( neighbor.PopulationType == PlanetPopulationType.AIHomeworld ||
                         neighbor.PopulationType == PlanetPopulationType.AIBastionWorld )
                        {
                            adjacentHomeworld = true;
                            break;
                        }
                    }
                    if ( adjacentHomeworld == true )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because its adjacent to n AI homeworld planet", Verbosity.DoNotShow );
                        continue;
                    }

                    //Planet must belong to an allied faction
                    if ( !planet.GetControllingFaction().GetIsFriendlyTowards( faction ) )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because the planet isnt friendly", Verbosity.DoNotShow );
                        continue;
                    }
                    //planet must not be a King planet, or adjacent to a king planet
                    if ( planet.GetDataByStanceForFaction( faction, FactionStance.Friendly ).HasKingUnitPresent )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because its a king planet", Verbosity.DoNotShow );
                        continue;
                    }
                    if ( planet.MarkLevelForAIOnly.Ordinal > maxMarkLevel )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because its mark level " + planet.MarkLevelForAIOnly.Ordinal + " > allowed mark level " + maxMarkLevel, Verbosity.DoNotShow );
                        continue;
                    }
                    bool adjacentKing = false;
                    foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                    {
                        if ( neighbor.GetDataByStanceForFaction( faction, FactionStance.Friendly ).HasKingUnitPresent )
                        {
                            adjacentKing = true;
                            break;
                        }
                    }
                    if ( adjacentKing == true )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because its adjacent to a king planet", Verbosity.DoNotShow );
                        continue;
                    }

                    if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength > FInt.Zero )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because there are enemies on it", Verbosity.DoNotShow );

                        continue;
                    }
                    if ( minHopsFromHumanPlanet > 1 )
                    {
                        //this planet must not be too close to a player planet
                        //to make the bounds listed above inclusive minHops - 1 must be used
                        bool foundPlayerPlanetWithinMinHops = false;
                        foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops_NoFilters( (Int16)(minHopsFromHumanPlanet - 1) ) )
                        {
                            Planet otherPlanet = _phd.Planet;
                            if ( otherPlanet.GetControllingOrInfluencingFaction().Type == FactionType.Player )
                                foundPlayerPlanetWithinMinHops = true;
                        }
                        if ( foundPlayerPlanetWithinMinHops )
                            continue;
                    }
                    if ( maxHopsFromHumanPlanet > 0 )
                    {
                        //this planet can't be too far from a player planet
                        bool foundPlayerPlanetWithinMaxHops = false;
                        
                        foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops( maxHopsFromHumanPlanet,
                            delegate ( Planet source, Planet destination )
                            {
                                /*
                                if ( source.TypeData.Type == PlanetType.Nomad && !World_AIW2.Instance.CurrentGalaxy.IsNomadGalaxy )
                                {
                                    return false;
                                }
                                */

                                var wh = source.GetWormholeTo(destination.Index);
                                if (wh.TypeData.SpecialWormholeLogic != null)
                                {
                                    if (wh.TypeData.SpecialWormholeLogic.WormholeTrafficFilter(wh, destination.GetWormholeTo(source), "InstigatorPlacement_MeasureHopsToHumanPlanet") == WormholeTraffic.Dissallowed)
                                    {
                                        return false;
                                    }
                                }

                                return true;
                            },
                            (Planet.EvaluatorDelegate)null ) )
                        {
                            Planet otherPlanet = _phd.Planet;
                            if ( otherPlanet.GetControllingOrInfluencingFaction().Type == FactionType.Player )
                                foundPlayerPlanetWithinMaxHops = true;
                        }
                        if ( !foundPlayerPlanetWithinMaxHops )
                            continue;
                    }
                    if ( retries > 70 )
                    {
                        if ( planet.GetControllingFaction() != faction )
                        {
                            //try to start on a planet we own
                            continue;
                        }
                    }
                    WorkingPlanetList.Add( planet );
                    if ( planet.IntelLevel > PlanetIntelLevel.Unexplored )
                        WorkingPlanetListExploredOnly.Add( planet );
                }
                retries++;
            } while ( WorkingPlanetList.Count == 0 && retries < allowedRetries );
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Choosing from " + WorkingPlanetList.Count + " planets to spawn instigator base", Verbosity.DoNotShow );

            //if we have a valid planet, first check if we have a valid planet that's also explored. If so, prefer the explored planets
            //if not, use the unexplored planet.

            Planet returnResult = null;
            if ( WorkingPlanetList.Count <= 0 )
            {
                returnResult = null;
            }
            else if ( WorkingPlanetListExploredOnly.Count > 0 )
            {
                int attempts = 5;
                do{
                    returnResult = WorkingPlanetListExploredOnly[Context.RandomToUse.Next( 0, WorkingPlanetListExploredOnly.Count )];
                    PlanetFaction pFaction = returnResult.GetPlanetFactionForFaction( this.BaseInfo.AttachedFaction );
                    if ( attempts-- == 0 )
                        break;
                    if ( pFaction.DataByStance[FactionStance.Self].TotalStrength < 10 )
                    {
                        //prefer planets with a decent amount of defenses to make life harder for the player
                        returnResult = null;
                    }
                }while ( attempts > -1  );
            }
            if (returnResult == null )
            {
                returnResult = WorkingPlanetList[Context.RandomToUse.Next( 0, WorkingPlanetList.Count )];
            }

            Planet.ReleaseTemporaryPlanetList( WorkingPlanetList );
            Planet.ReleaseTemporaryPlanetList( WorkingPlanetListExploredOnly );

            return returnResult;
        }
        #endregion Sidekicks
        #region Vassals
        //Vassals are an unused proto-feature for DLC3 that never was finished
        //Basically, it would be some minor faction that the player could give instructions to
        //This idea was essentially replaced by Sidekicks, which get much of what I wanted from the feature anyway
        private static readonly List<SafeSquadWrapper> WorkingEntityList = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "DarkZenithFactionDeepInfoRoot-WorkingBuilderList" );
        private void UpdateVassalEconomicMissions( ArcenHostOnlySimContext Context )
        {
            if ( this.BaseInfo.EconomicMissions.Count == 0 )
                return;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-UpdateVassalEconomicMissions-trace", 10f ) : null;
            PerFactionPathCache PathCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            List<SafeSquadWrapper> epistyles = this.BaseInfo.Epistyles.GetDisplayList();
            WorkingEntityList.Clear();
            for ( int i = 0; i < epistyles.Count; i++ )
            {
                GameEntity_Squad ship = epistyles[i].GetSquad();
                if ( ship == null )
                    continue;
                DarkZenithPerUnitBaseInfo data = ship.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenthPerUnitBaseInfo" );
                if ( !data.IsAssignedToMission )
                    WorkingEntityList.Add( ship );
            }

            for ( int i = 0; i < this.BaseInfo.EconomicMissions.Count; i++ )
            {
                VassalMission  mission = this.BaseInfo.EconomicMissions[i];
                if ( tracing )
                    tracingBuffer.Add("Trying to find an epistyle for " ).Add( mission.ToStringForDebug() ).Add("\n"); 
                for ( int j = WorkingEntityList.Count - 1; j >= 0; j-- )
                {
                    GameEntity_Squad epistyle = WorkingEntityList[j].GetSquad();
                    if ( epistyle == null )
                        continue;
                    Int16 hops;
                    int danger = Fireteam.GetDangerOfPath( this.AttachedFaction, Context, PathCacheData, mission.Planet, epistyle.Planet, true, out hops );
                    if ( tracing )
                        tracingBuffer.Add("\tChecking whether epistyle ").Add( epistyle.ToStringWithPlanet() ).Add(" is suitable; path danger: " + danger ).Add("\n");

                    if ( danger > 10 * 1000 )
                        continue; //this epistyle can't safely get here
                    DarkZenithPerUnitBaseInfo data = epistyle.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    tracingBuffer.Add("\t\tepistyle assigned!\n");
                    data.MyMission = mission;
                    data.IsAssignedToMission = true;

                    mission.OptionalEntityHandlingMission = LazyLoadSquadWrapper.Create( epistyle );
                    WorkingEntityList.RemoveAt( j );
                }
                if ( WorkingEntityList.Count == 0 )
                    break;
            }
            PathCacheData.ReturnToPool();
            #region Tracing
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
            #endregion
        }
        #endregion
        private void HandleExos( Faction  faction, ArcenHostOnlySimContext Context )
        {
            if ( !faction.SpecialFactionData.FullInvasionMode )
                return; //only for full invasion DZ, not svikari
            if ( !BaseInfo.HasTakenTerritorialSphere )
                return; //only after the territorial sphere has been taken
            if ( BaseInfo.Difficulty.BaseExoStrength <= 0 &&
                 BaseInfo.Difficulty.BaseExoInterval <= 0 )
                return; //looks like we aren't sending exos
            if ( BaseInfo.Jormugandr.Count == 0 ||
                 BaseInfo.Epistyles.Count == 0 )
                return; //the DZ is already doing very poorly....
            if ( BaseInfo.TimeForNextExo != -1 &&
                 BaseInfo.PlanetsControlled.Count < BaseInfo.OriginalPlanets.Count + 2 )
            {
                //                ArcenDebugging.ArcenDebugLogSingleLine("Exo unset; DZ is too weak", Verbosity.DoNotShow );
                BaseInfo.TimeForNextExo = -1; //the DZ is doing poorly, so no more exos
                return;
            }
            
            if ( BaseInfo.TimeForNextExo == -1 )
            {
                BaseInfo.TimeForNextExo = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.BaseExoInterval;
                return;
            }

            if ( World_AIW2.Instance.GameSecond >= BaseInfo.TimeForNextExo )
            {
                ExoOptions options = ExoOptions.CreateWithDefaults( BaseInfo.Jormugandr.GetDisplayList(), BaseInfo.Difficulty.BaseExoStrength, null, faction );
                ExoGalacticAttackManager.SendExoGalacticAttack( options, Context );
                BaseInfo.TimeForNextExo = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.BaseExoInterval;
            }
        }
        private void UpdateJournals( ArcenHostOnlySimContext Context )
        {
            if ( !ArcenNetworkAuthority.GetIsHostMode() )
                return;
            if ( World_AIW2.Instance.GameSecond % 5 != 0 )
                return; //no need for every second
            if ( BaseInfo.CanPlayerSeeEconomy )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_DarkZenith_EconomicOverview", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            if ( BaseInfo.CanPlayerSeeJormugandr )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_DarkZenith_JormugandrOverview", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            if ( BaseInfo.HasBuiltGolem )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_DarkZenith_Svikari_Golem", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            if ( BaseInfo.CanPlayerSeeFimbulwinter )
            {
                if ( BaseInfo.HasAnyPlayerAllies )
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_DarkZenith_Fimbulwinter_PlayerAlly", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                else
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_DarkZenith_Fimbulwinter", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
            if ( BaseInfo.Terminii.Count > 0 )
            {
                //if the DZ is in the galaxy, play some required journals
                if ( AttachedFaction.SpecialFactionData.FullInvasionMode )
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_DarkZenith_Invasion_Hostile", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                else if ( BaseInfo.PlayerAllied )
                {
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_DarkZenith_InitialPlayerAid", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                }
                else if ( BaseInfo.MinorFactionAllied )
                {
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_DarkZenith_JoinMinorFaction", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                }

            }
            if ( BaseInfo.Jormugandr.Count == 0 )
            {
                //The DZ invasion is defeated! Needs the actual XML written
                //World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_DarkZenith_JoinMinorFaction", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
            if ( this.IsHumanSidekick )
            {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_DarkZenithSidekick_Overview", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
        }
        private void UpdatePlanetBGs()
        {
            for ( int i = 0; i < BaseInfo.OriginalPlanets.Count; i++ )
            {
                Planet plan = null;
                try
                {
                    plan = BaseInfo.OriginalPlanets[i];
                }
                catch { continue; }
                if ( plan != null )
                {
                    plan.SpaceBox_TagMustMatch = "DarkZenith";
                    plan.Planet_TagMustMatch = "DarkZenith";
                }
            }
        }

        private void HandleFimbulwinter( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //don't even try this on clients

            if ( !BaseInfo.WinterEnabled )
                return;

            //iterate over all our epistyles. If we have an epistyle and we aren't Wintered and don't have a Hjarn,
            //create a hjarn
            List<SafeSquadWrapper> epistyles = this.BaseInfo.Epistyles.GetDisplayList();
            for ( int i = 0; i < epistyles.Count; i++ )
            {
                if ( BaseInfo.IsPlayer )
                    break; //players fimbulwinter planets via hacks
                GameEntity_Squad entity = epistyles[i].GetSquad();
                if ( entity == null )
                    continue;
                if ( World_AIW2.Instance.GameSecond - entity.GameSecondEnteredThisPlanet < 120 )
                    continue; //don't make a hjarn immediately
                if ( BaseInfo.FimbulwinterEligibleTime[entity.Planet] > World_AIW2.Instance.GameSecond )
                    continue; //we can't respawn a hjarn here for a while
                if ( entity.Planet.IsFimbulwintered )
                    continue;
                bool foundHjarn = false;
                foreach ( GameEntity_Squad hjarn in this.BaseInfo.Hjarnum.DisplaySquads() )
                {
                    if ( hjarn.Planet == entity.Planet )
                    {
                        foundHjarn = true;
                        break;
                    }
                }
                if ( foundHjarn )
                    continue;
                int randomizer = Context.RandomToUse.Next(1, 20);
                if ( World_AIW2.Instance.GameSecond % randomizer != 0)
                {
                    break; //5% of making a hjarn this second
                }
                GameEntityTypeData typedata = GameEntityTypeDataTable.Instance.GetRowByName( "DZHjarn" );
                if ( typedata == null )
                    throw new Exception("Could not find DZHjarn");

                ArcenPoint destinationPoint = entity.Planet.GetSafePlacementPoint_AroundEntity( Context, typedata, entity, FInt.FromParts( 0, 025 ), FInt.FromParts( 0, 250 ) );
                PlanetFaction pFaction = entity.PlanetFaction;

                GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typedata, 1,
                                                                                              pFaction.Faction.LooseFleet, 0, destinationPoint, Context, "DarkZenith-Fimbulwinter" );

                //This is a DoubleBufferedConcurrentList thing.  It is threadsafe
                this.BaseInfo.Hjarnum.AddToDisplayList(newEntity);
                break; //max of one hjarn at a time
            }

            foreach ( GameEntity_Squad hjarn in this.BaseInfo.Hjarnum.DisplaySquads() )
            {
                //this is the delay till we can next attempt to build a Hjarn here
                int baseDelayTime = 1800;
                int delayTime = World_AIW2.Instance.GameSecond + baseDelayTime + Context.RandomToUse.Next( 0, baseDelayTime / 2 );
                BaseInfo.FimbulwinterEligibleTime[hjarn.Planet] = delayTime;

                DarkZenithPerUnitBaseInfo data = hjarn.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( hjarn.GetSecondsSinceEnteringThisPlanet() >= BaseInfo.Difficulty.TimeToConvertPlanet )
                {
                    if ( ArcenNetworkAuthority.GetIsHostMode() && hjarn.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
                        World_AIW2.Instance.QueueChatMessageOrCommand( "Planet " + hjarn.GetPlanetName_Safe() + " has succumbed to the Fimbulwinter.", ChatType.LogToCentralChat, null );
                    hjarn.Planet.IsFimbulwintered = true;
                    hjarn.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                    this.AttachedFaction.StoredScience += BaseInfo.Difficulty.SciencePerPlanetFimbulwintered;
                }
            }

            if ( !BaseInfo.HasCheckedForFimbuledAllies )
            {
                BaseInfo.HasCheckedForFimbuledAllies = true;
                AttachedFaction.BenefitsFromFimbulwinter = true; //we benefit from the fimbulwinter, duh
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    if ( otherFaction == AttachedFaction )
                        continue;
                    if ( AttachedFaction.GetIsFriendlyTowards( otherFaction ) )
                         otherFaction.BenefitsFromFimbulwinter = true;
                }
            }
        }
        private void ConvertEpistylesToPirate( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            //if we have just made an epistyle into a pirate epistyle, swap it for the pirate-specific XML
            List<SafeSquadWrapper> epistyles = this.BaseInfo.Epistyles.GetDisplayList();
            for ( int i = 0; i < epistyles.Count; i++ )
            {
                GameEntity_Squad epistyle = epistyles[i].GetSquad();
                if ( epistyle == null )
                    continue;
                DarkZenithPerUnitBaseInfo data = epistyle.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( !data.IsPirateEpistyle )
                    continue;
                //this is a pirate epistyle
                if ( epistyle.TypeData.GetHasTag("DZPirateEpistyle") )
                    continue;
                //but with the old XML. Swap it
                GameEntityTypeData typedata = GameEntityTypeDataTable.Instance.GetRowByName( "DZPirateEpistyle" );
                PlanetFaction pFaction = epistyle.PlanetFaction;
                GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typedata, 1,
                                                                                              pFaction.Faction.LooseFleet, 0, epistyle.WorldLocation, Context, "DarkZenith-EpiToPirate" );
                newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                epistyle.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
            }
        }
        private void JoinAlliesIfNecessary(ArcenHostOnlySimContext Context)
        {
            //Handle the allegiances by respawning with our allies if necessary
            if (!BaseInfo.JoinAlliedFactions && !this.IsSvikari)
            {
                return;
            }
            if (BaseInfo.AllEconomicStructures.Count > 0)
            {
                return;
            }
            GameEntityTypeData typedata = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DZMetalTerminus" );
            GameEntityTypeData typedataGreen = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DZGreenTerminus" );
            
            if ( BaseInfo.MinorFactionAllied )
            {
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                      Faction influencingFaction = World_AIW2.Instance.GetFactionByIndex( planet.PrimaryInfluencingFaction );
                      if ( influencingFaction == null || !influencingFaction.GetIsFriendlyTowards( AttachedFaction ) )
                          continue;

                      PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                      if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength == 0 )
                      {
                          //ArcenDebugging.ArcenDebugLogSingleLine(string.Format("DZS spawning on {0} for minor faction ally", planet.Name), Verbosity.DoNotShow );

                          if ( ArcenNetworkAuthority.GetIsHostMode() )
                          {
                              if ( planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                              {
                                  PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                                  if ( chatHandlerOrNull != null )
                                      chatHandlerOrNull.PlanetToView = planet;

                                  World_AIW2.Instance.QueueChatMessageOrCommand( "The " + AttachedFaction.StartFactionColourForLog() + "Dark Zenith</color> have joined the " +
                                                                                 influencingFaction.StartFactionColourForLog() + influencingFaction.GetDisplayName() + "</color> invasion on " + planet.Name, ChatType.LogToCentralChat, chatHandlerOrNull );
                              }
                          }
                          SpawnStructureOrReturnNull( planet, typedata, 1, Engine_AIW2.Instance.CombatCenter, Context );

                          break;
                      }
                }
              }
              else
              {
                  foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                  {
                      if ( entity.GetIsFriendlyTowards_Safe( AttachedFaction ) )
                      {
                          if ( entity.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength > 10 * 000 )
                              continue; //don't seed if we're under attack

                          //ArcenDebugging.ArcenDebugLogSingleLine(string.Format("DZS spawning on {0} for friendly king {1}", entity.Planet.Name, entity.GetFactionInternalNameSafe()), Verbosity.DoNotShow );

                          if ( this.IsHumanSidekick )
                          {
                              //for the sidekick
                              SpawnSidekickInitialStructures( entity, Context );
                          }
                          if ( this.BaseInfo.InCivilWar)
                          {
                              if ( FactionUtilityMethods.Instance.DoesPlanetHaveScourgeSpawner(entity.Planet) )
                                  continue;
                              if ( FactionUtilityMethods.Instance.DoesPlanetHaveMetalTerminus(entity.Planet) )
                                  continue;
                              //Civil War starts with more stuff
                              SpawnStructureOrReturnNull( entity.Planet, typedata, 1, Engine_AIW2.Instance.CombatCenter, Context );
                              SpawnStructureOrReturnNull( entity.Planet, GameEntityTypeDataTable.Instance.GetRowByName( "DZHarvester" ), 1, Engine_AIW2.Instance.CombatCenter, Context );
                              break;
                          }
                          else
                          {
                              SpawnStructureOrReturnNull( entity.Planet, typedata, 1, Engine_AIW2.Instance.CombatCenter, Context );
                          }
                        }
                  }
              }
        }
        public override void ReactToHacking_AsPartOfMainSim_HostOnly( GameEntity_Squad entityBeingHacked, FInt WaveMultiplier, ArcenHostOnlySimContext Context, HackingEvent Event, Faction overrideFaction = null )
        {
            ReactToHackingStep_AsPartOfMainSim( entityBeingHacked, WaveMultiplier, Context );
        }

        public static void ReactToHackingStep_AsPartOfMainSim( GameEntity_Squad hackingTarget, FInt multiplier, ArcenHostOnlySimContext Context )
        {
            //if ( hackingTarget == null )
            //    return; //if hacking the planet
            int debugCode = 0;
            try{
                debugCode = 100;
                if ( hackingTarget == null )
                    return;
                PlanetFaction pFaction = hackingTarget.PlanetFaction;
                int retries = 100;
                //Base the wave size off a random AI faction's wave response strength (basically, the response strength now scales with AIP
                Faction aiFaction = World_AIW2.GetRandomAIFaction(Context);
                if ( aiFaction == null )
                    return;
                debugCode = 200;
                AISentinelsCoreData factionExternal = aiFaction.TryGetAISentinelsCoreData()?.SentinelInfo;
                if ( factionExternal == null )
                    return;
                debugCode = 300;
                AISentinelsFactionBaseInfo aiBaseInfo = aiFaction.TryGetAISentinelsCoreData();
                if ( aiBaseInfo == null )
                    return;
                debugCode = 400;
                int WaveSize = aiBaseInfo.GetSpecificBudgetThreshold( AIBudgetType.Wave, GlobalAIWorldBaseInfo.Instance.AIProgress_Effective );
                debugCode = 500;
                int responseStrength = (WaveSize * multiplier).IntValue;
                if ( responseStrength > 50 * 1000 )
                    responseStrength = 50 * 1000; //sometimes I've seen this be enormous
                debugCode = 600;
                while ( responseStrength > 0 && retries > 0 )
                {
                    debugCode = 700;
                    GameEntityTypeData typedata = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DZHackResponse" );
                    if ( typedata == null )
                        throw new Exception("Could not find any entities with tag DZHackResponse");
                    debugCode = 800;
                    if ( responseStrength < typedata.CostForAIToPurchase )
                    {
                        retries--;
                        continue;
                    }
                    debugCode = 900;
                    responseStrength -= typedata.CostForAIToPurchase;
                    GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typedata, 1,
                                                                     pFaction.Faction.LooseFleet, 0, hackingTarget.WorldLocation, Context, "DarkZenith-HackResponse" );
                    debugCode = 1000;
                }
            } catch( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception during DZ ReactToHackingStep_AsPartOfMainSim debugCode " + debugCode + " " + e.ToString() , Verbosity.DoNotShow );
            }
        }
         
        public void UpdateTerritorialSphere( ArcenHostOnlySimContext Context )
        {
            if ( BaseInfo.OriginalPlanets.Count == 0 || BaseInfo.EnemyPlanetsInTerritorialSphereSim == null )
                return; //we haven't invaded yet
            if ( BaseInfo.HasTakenTerritorialSphere ) //we've already taken our whole sphere the first time
            {
                if ( BaseInfo.PlanetsInTerritorialSphere == -1 )
                    BaseInfo.PlanetsInTerritorialSphere = BaseInfo.GetPlanetsInTerritorialSphere( Context );
                return;
            }
            if ( BaseInfo.HopsOfSphereTaken == BaseInfo.Difficulty.TerritorialSphereSize )
                BaseInfo.HasTakenTerritorialSphere = true;
            if ( BaseInfo.EnemyPlanetsInTerritorialSphereSim.Count == 0 )
            {
                List<Planet> workingUnownedForUpdateTerritorialSphere = Planet.GetTemporaryPlanetList( "DZ-UpdateTerritorialSphere-workingUnownedForUpdateTerritorialSphere", 10f );
                if ( workingUnownedForUpdateTerritorialSphere == null ) //blocked for teardown/shutdown; bail
                    return;
                BaseInfo.UnownedPlanetsInSphere( workingUnownedForUpdateTerritorialSphere, Context, BaseInfo.PlanetsControlled.GetDisplayList() );
                if ( workingUnownedForUpdateTerritorialSphere.Count == 0 )
                {
                    BaseInfo.HopsOfSphereTaken++;
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Another hop taken of the sphere; now at " + BaseInfo.HopsOfSphereTaken, Verbosity.DoNotShow );
                }
                Planet.ReleaseTemporaryPlanetList( workingUnownedForUpdateTerritorialSphere );
            }
        }
        public void CheckIfHomeworldsUnderAttack( ArcenHostOnlySimContext Context )
        {
            bool wasOngoingAttack = BaseInfo.IsHomeworldUnderAttack;
            BaseInfo.IsHomeworldUnderAttack = false;
            for ( int i = 0; i < BaseInfo.OriginalPlanets.Count; i++ )
            {
                Planet planet = BaseInfo.OriginalPlanets[i];
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength > 0 )
                {
                    BaseInfo.IsHomeworldUnderAttack = true;
                    break;
                }
            }
            if ( BaseInfo.IsHomeworldUnderAttack && !wasOngoingAttack )
            {
                BaseInfo.TimesHomeworldsAttacked++;
            }
        }
        //Moving resources around! Here harvesters pick up metal from Metal Generators or deposit it at the Terminus
        public void HandleHarvestersSim( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host

            int debugCode = 0;
            try
            {
                debugCode = 100;
                List<SafeSquadWrapper> harvesters = this.BaseInfo.Harvesters.GetDisplayList();
                List<SafeSquadWrapper> terminii = this.BaseInfo.Terminii.GetDisplayList();
                for ( int i = 0; i < harvesters.Count; i++ )
                {
                    debugCode = 200;
                    GameEntity_Squad harvester = harvesters[i].GetSquad();
                    if ( harvester == null )
                        continue;
                    DarkZenithPerUnitBaseInfo harvesterData = harvester.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    bool transferredMetal = false;
                    //If we are near a terminus and have metal, drop it off
                    for ( int j = 0; j < terminii.Count; j++ )
                    {
                        debugCode = 300;
                        GameEntity_Squad terminus = terminii[j].GetSquad();
                        if ( terminus == null )
                            continue;
                        DarkZenithPerUnitBaseInfo terminusData = terminus.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                        if ( harvester.Planet == terminus.Planet &&
                             terminusData.Resource == DZResource.Metal &&
                             Mat.DistanceBetweenPointsImprecise( harvester.WorldLocation, terminus.WorldLocation ) < BaseInfo.ResourceExchangeDistance )
                        {
                            debugCode = 400;
                            if ( !harvesterData.Inventory.ContainsKey( DZResource.Metal ) )
                                harvesterData.Inventory[DZResource.Metal] = 0;
                            if ( terminusData.Resource == DZResource.Metal &&
                                 harvesterData.Inventory[DZResource.Metal] > 0 )
                            {
                                debugCode = 500;
                                string log = ""; //in case we want to log stuff
                                terminusData.TransferFullInventoryFrom( ref harvesterData, ref log );
                                transferredMetal = true;
                                break;
                            }
                        }
                    }
                    if ( transferredMetal )
                        continue;
                    debugCode = 600;
                    if ( !harvesterData.Inventory.ContainsKey( DZResource.Metal ) )
                        harvesterData.Inventory[DZResource.Metal] = 0;
                    if ( harvesterData.Inventory[DZResource.Metal] <= BaseInfo.Difficulty.HarvesterCapacity )
                    {
                        debugCode = 700;
                        bool anyHarvestersFound = false;
                        foreach ( GameEntity_Squad generator in harvester.Planet.Squads( "MetalGenerator" ) )
                        {
                            anyHarvestersFound = true;
                            debugCode = 800;
                            if ( Mat.DistanceBetweenPointsImprecise( harvester.WorldLocation, generator.WorldLocation ) < BaseInfo.ResourceExchangeDistance )
                            {
                                debugCode = 900;
                                harvesterData.Inventory[DZResource.Metal] += BaseInfo.Difficulty.MetalHarvestablePerSecond;

                                break;
                            }
                        }
                        if ( !anyHarvestersFound )
                            harvesterData.Inventory[DZResource.Metal] += BaseInfo.Difficulty.MetalHarvestablePerSecond; //just create some metal magically if  there are no metal generators. This is intended to get the DZ unstuck if for some reason they start on a planet without metal harvesters
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in HandleHarvestersSim code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        public void HandlePrivateersSim( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host

            //Nota Bene: this isn't the most efficient code in the world, but we aren't supposed to ever have too many Privateers
            //If we wind up with too many then we just reduce the number of privateers we have
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-HandlePrivateersSim-trace", 10f ) : null;

            int debugCode = 0;
            try
            {
                List<SafeSquadWrapper> epistyles = this.BaseInfo.Epistyles.GetDisplayList();
                List<SafeSquadWrapper> privateers = this.BaseInfo.Privateers.GetDisplayList();
                List<SafeSquadWrapper> transports = this.BaseInfo.Transports.GetDisplayList();
                for ( int i = 0; i < epistyles.Count; i++ )
                {
                    //First, iterate over all epistyles to make a new Privateer if necessary
                    GameEntity_Squad epistyle = epistyles[i].GetSquad();
                    if ( epistyle == null )
                        continue;
                    DarkZenithPerUnitBaseInfo data = epistyle.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    if ( !data.IsPirateEpistyle )
                        continue;

                    bool foundPrivateerForThisEpistyle = false;
                    for ( int j = 0; j < privateers.Count; j++ )
                    {
                        GameEntity_Squad ship = privateers[j].GetSquad();
                        if ( ship == null )
                            continue;
                        DarkZenithPerUnitBaseInfo privateerData = ship.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                        if ( privateerData.HomeEpistyleId == epistyle.PrimaryKeyID )
                        {
                            foundPrivateerForThisEpistyle = true;
                            break;
                        }
                    }
                    if ( data.TimeForNextPrivateer == -1 )
                        data.TimeForNextPrivateer = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.BaseTimeBetweenPrivateers;
                    if ( data.TimeForNextPrivateer == -1 )
                        data.TimeForNextPrivateer = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.BaseTimeBetweenPrivateers;
                    if ( foundPrivateerForThisEpistyle )
                        continue;
                    if ( World_AIW2.Instance.GameSecond > data.TimeForNextPrivateer )
                    {
                        GameEntityTypeData typedata = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DZPrivateer" );
                        GameEntity_Squad newEntity = SpawnStructureOrReturnNull( epistyle.Planet, typedata, 3, epistyle.WorldLocation, Context );
                        if ( newEntity != null )
                        {
                            DarkZenithPerUnitBaseInfo newData = newEntity.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                            newData.HomeEpistyleId = epistyle.PrimaryKeyID;
                            data.TimeForNextPrivateer = -1;
                        }
                        if ( tracing )
                            tracingBuffer.Add( epistyle.ToStringWithPlanet() + " is creating a new privateer" );
                    }
                }

                for ( int i = 0; i < BaseInfo.Privateers.Count; i++ )
                {
                    //Now handle the actual privateers
                    GameEntity_Squad privateer = privateers[i].GetSquad();
                    if ( privateer == null )
                        continue;
                    DarkZenithPerUnitBaseInfo data = privateer.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );

                    //if our home epistyle is dead, see if there's another pirate epistyle, then donate ourselves to it. Otherwise do nothing
                    GameEntity_Squad homeEpistyle = null;
                    for ( int j = 0; j < BaseInfo.Epistyles.Count; j++ )
                    {
                        if ( epistyles[j].PrimaryKeyID == data.HomeEpistyleId )
                        {
                            homeEpistyle = epistyles[j].GetSquad();
                            if ( homeEpistyle != null )
                                break;
                        }
                    }
                    if ( homeEpistyle == null )
                        continue;

                    if ( data.DestinationId != -1 )
                    {
                        if ( data.Destination == null )
                            data.Destination = World_AIW2.Instance.GetEntityByID_Squad( data.DestinationId );
                        if ( data.Destination == null || data.Destination.TypeData == null || data.Destination.Planet == null ||
                             data.Destination.PlanetFaction.Faction != AttachedFaction )
                        {
                            //our destination is dead, clear it
                            data.DestinationId = -1;
                            data.Destination = null;
                        }
                        if ( data.DestinationId != -1 && data.Destination.TypeData.GetHasTag( "DZTransport" ) )
                        {
                            //we still have a valid destination, which is either our home epistyle or a transport
                            //if it's a transport, if it's run out of resources then discard it
                            DarkZenithPerUnitBaseInfo tData = data.Destination.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                            if ( !tData.HasAnyResourcesAtAll() )
                            {
                                data.DestinationId = -1;
                                data.Destination = null;
                            }
                        }
                    }

                    //Find a new destination
                    if ( data.DestinationId == -1 )
                    {
                        if ( !data.HasAnyResourcesAtAll() )
                        {
                            //if we have no resources, find a tranposrt
                            GameEntity_Squad target = PickRandomTransportWithResources( Context );
                            if ( target != null )
                            {
                                data.Destination = target;
                                data.DestinationId = target.PrimaryKeyID;
                            }
                        }
                        else
                        {
                            //we have resources, so go home
                            data.DestinationId = data.HomeEpistyleId;
                        }
                    }

                    //Resource Transfer if necessary
                    for ( int j = 0; j < transports.Count; j++ )
                    {
                        //If we are close to any transport, loot it
                        GameEntity_Squad transport = transports[j].GetSquad();
                        if ( transport == null )
                            continue;
                        if ( transport.Planet == privateer.Planet &&
                             Mat.DistanceBetweenPointsImprecise( transport.WorldLocation, privateer.WorldLocation ) < BaseInfo.ResourceExchangeDistancePrivateers)
                        {
                            DarkZenithPerUnitBaseInfo transportData = transport.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                            string logString = "";
                            data.TransferFullInventoryFrom( ref transportData, ref logString );
                            if ( data.NumTransportsAttacked <= 0 )
                                data.NumTransportsAttacked = 1;
                            else
                                data.NumTransportsAttacked++;
                            //looting a transport destroys it
                            transport.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
                        }
                    }
                    //if we are close to our home, give resources
                    if ( homeEpistyle.Planet == privateer.Planet &&
                         Mat.DistanceBetweenPointsImprecise( homeEpistyle.WorldLocation, privateer.WorldLocation ) < BaseInfo.ResourceExchangeDistance )
                    {
                        DarkZenithPerUnitBaseInfo homeData = homeEpistyle.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                        string logString = "";
                        homeData.TransferFullInventoryFrom( ref data, ref logString );
                        if ( tracing )
                            tracingBuffer.Add( homeEpistyle.ToStringWithPlanet() + " has just had a privateer drop resources off" );

                        privateer.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
                        homeData.NumTransportsAttacked += data.NumTransportsAttacked;
                        homeData.TimeForNextPrivateer = World_AIW2.Instance.GameSecond + (BaseInfo.Difficulty.BaseTimeBetweenPrivateers / 2); //if successfuly, halve the wait time for the next privateer
                        continue;
                    }

                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception during HandlePrivateersSim code " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            #region Tracing
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
            #endregion
        }
        private GameEntity_Squad PickRandomTransportWithResources( ArcenHostOnlySimContext Context )
        {
            List<SafeSquadWrapper> WorkingTransports = GameEntity_Squad.GetTemporarySquadList( "DZ-PickRandomTransportWithResources-WorkingTransports", 10f );
            if ( WorkingTransports == null ) //blocked for teardown/shutdown; bail
                return null;
            List<SafeSquadWrapper> transports = this.BaseInfo.Transports.GetDisplayList();
            for ( int j = 0; j < transports.Count; j++ )
            {
                //If we are close to any transport, loot it
                GameEntity_Squad transport = transports[j].GetSquad();
                if ( transport == null )
                    continue;
                DarkZenithPerUnitBaseInfo data = transport.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( !data.HasAnyResourcesAtAll() )
                    continue;
                WorkingTransports.Add( transport );
            }
            if ( WorkingTransports.Count == 0 )
            {
                GameEntity_Squad.ReleaseTemporarySquadList( WorkingTransports );
                return null;
            }
            WorkingTransports.Sort( static delegate ( SafeSquadWrapper R, SafeSquadWrapper L )
             {
                 GameEntity_Squad lShip = L.GetSquad();
                 GameEntity_Squad rShip = R.GetSquad();
                 DarkZenithPerUnitBaseInfo lData = lShip == null ? null : lShip.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                 DarkZenithPerUnitBaseInfo rData = rShip == null ? null : rShip.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                 int lScore = lData == null ? 0 : lData.GenerateAvailableResourceScore();
                 int rScore = rData == null ? 0 : rData.GenerateAvailableResourceScore();

                 return lScore.CompareTo( rScore );

             } );
            GameEntity_Squad ret = WorkingTransports[0].GetSquad();
            GameEntity_Squad.ReleaseTemporarySquadList( WorkingTransports );
            return ret;
        }

        //Moving resources around! Here Transports pick up metal from Terminii and deposit it at other Terminii or Epistyles
        public void HandleTransportsSim( ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith ) || BaseInfo.logTransportSim;
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-HandleTransportsSim-trace", 10f ) : null;
            #endregion

            int debugCode = 0;
            try
            {
                if ( tracing && BaseInfo.Transports.Count > 0 )
                    tracingBuffer.Add("Handling " + BaseInfo.Transports.Count + " Transports in the Sim code\n");
                List<SafeSquadWrapper> transports = this.BaseInfo.Transports.GetDisplayList();
                List<SafeSquadWrapper> epistyles = this.BaseInfo.Epistyles.GetDisplayList();
                for ( int i = 0; i < transports.Count; i++ )
                {
                    debugCode = 1000;
                    bool hasCheckedDestination = false; //if we have a destination somewhere
                    bool arrivedAtDestination = false; //if we have arrived at our destination
                    GameEntity_Squad transport = transports[i].GetSquad();
                    if ( transport == null )
                        continue;
                    DarkZenithPerUnitBaseInfo transportData = transport.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    if ( transportData.Destination == null && transportData.DestinationId != -1 )
                    {
                        debugCode = 1010;
                        //if this is shortly after a reload, make sure to initialize the Destination correctly,
                        transportData.Destination = World_AIW2.Instance.GetEntityByID_Squad( transportData.DestinationId );
                    }
                    if ( transportData.SecondaryDestination == null && transportData.SecondaryDestinationId != -1 )
                    {
                        debugCode = 1015;
                        //if this is shortly after a reload, make sure to initialize the Destination correctly,
                        transportData.SecondaryDestination = World_AIW2.Instance.GetEntityByID_Squad( transportData.SecondaryDestinationId );
                    }
                    if ( transportData.SecondaryMostRecentPlanet == null && transportData.SecondaryMostRecentPlanetId != -1 )
                    {
                        debugCode = 1016;
                        //if this is shortly after a reload, make sure to initialize the Destination correctly,
                        transportData.SecondaryMostRecentPlanet = World_AIW2.Instance.GetPlanetByIndex( (short)transportData.SecondaryMostRecentPlanetId );
                    }

                    if ( transportData.Destination != null && tracing && World_AIW2.Instance.GameSecond % 10 == 0 )
                        tracingBuffer.Add(transport.ToStringWithPlanet() + " is en route to " + transportData.Destination.ToStringWithPlanet() + " (id "+transportData.DestinationId +")\n");
                    debugCode = 1020;
                    if ( transportData.Destination == null || transportData.Destination.TypeData == null || transportData.Destination.Planet == null ||
                         transportData.Destination.PlanetFaction.Faction != AttachedFaction )
                    {
                        debugCode = 1030;
                        if ( tracing )
                            tracingBuffer.Add( transport.ToStringWithPlanet() + " is dropping its destination, since it seems to be gone (previous dest ID: " + transportData.DestinationId +")\n" );
                        //Some sanity checking for the Destination
                        transportData.Destination = null;
                        transportData.DestinationId = -1;
                        //If we'd already queued a wormhole hop plus the trailing local move toward this destination
                        //(see GoToPlanetThenLocation in HandleTransportsLRP), cancel the local move so we don't fly
                        //into whatever is now sitting where the destination used to be.
                        AutoDefendUtility.CancelQueuedOrdersIfDestinationGone( transport, null );
                    }
                    if ( transportData.SecondaryDestinationId != -1 && (transportData.SecondaryDestination == null || transportData.SecondaryDestination.TypeData == null || transportData.SecondaryDestination.Planet == null ||
                                                                        transportData.SecondaryDestination.PlanetFaction.Faction != AttachedFaction ) )
                    {
                        debugCode = 10350;
                        if ( tracing )
                            tracingBuffer.Add( transport.ToStringWithPlanet() + " is dropping its secondary destination, since it seems to be gone\n" );
                        //Some sanity checking for the SecondaryDestination
                        transportData.SecondaryDestination = null;
                        transportData.SecondaryDestinationId = -1;
                    }
                    debugCode = 103500;

                    if ( transportData.Destination != null &&
                         transportData.Destination.Planet != transport.Planet && //if we have a destination already and we aren't to that planet yet
                         transportData.SecondaryDestination == null && //and we don't already have a secondary target
                         transportData.SecondaryMostRecentPlanetId != transport.Planet.Index ) //and we haven't looked at this planet before
                    {
                        debugCode = 103510;
                        //if we don't have a secondary destination on this planet, see if there's a good one.
                        //a secondary data is a terminus on the planet with resources we can pick up on our way to another destination
                        //note we can only get one secondary destination per planet
                        if ( tracing )
                            tracingBuffer.Add( "  Finding secondary terminus for " + transport.ToStringWithPlanet() + " if possible before proceeding to  " + transportData.Destination.ToStringWithPlanet() + " \n" );
                        //update the secondary most recent planet to make sure we only check once per planet
                        transportData.SecondaryMostRecentPlanetId = transport.Planet.Index;
                        transportData.SecondaryMostRecentPlanet = World_AIW2.Instance.GetPlanetByIndex( transport.Planet.Index );
                        debugCode = 103520;
                        transportData.SecondaryDestination = FindTerminusForTransport( transport, true, true, true, Context );
                        if ( transportData.SecondaryDestination != null )
                        {
                            transportData.SecondaryDestinationId = transportData.SecondaryDestination.PrimaryKeyID; //we have a new secondary destination
                        }
                    }
                    debugCode = 1040;
                    if ( transportData.SecondaryDestination != null )
                    {
                        debugCode = 1050;
                        bool arrivedAtSecondary = TransportResourcesIfAppropriate( transport, transportData, transportData.SecondaryDestination, tracing, tracingBuffer, ref hasCheckedDestination );
                        if ( arrivedAtSecondary )
                        {
                            transportData.SecondaryDestinationId = -1;
                            transportData.SecondaryDestination = null;
                        }
                        if ( transportData.SecondaryDestination.Planet != transport.Planet )
                        {
                            //                            ArcenDebugging.ArcenDebugLogSingleLine(transport.ToStringWithPlanet() + "  missed its window; cancel secondary dest", Verbosity.DoNotShow );
                            transportData.SecondaryDestinationId = -1;
                            transportData.SecondaryDestination = null;
                        }

                    }
                    if ( transportData.Destination != null )
                    {
                        debugCode = 1055;
                        arrivedAtDestination = TransportResourcesIfAppropriate( transport, transportData, transportData.Destination, tracing, tracingBuffer, ref hasCheckedDestination );
                    }
                    debugCode = 1060;
                    if ( arrivedAtDestination || hasCheckedDestination )
                    {
                        if ( !arrivedAtDestination )
                            continue; //we've checked our destination, but we're not there yet
                        debugCode = 1070;
                        //I have arrived! unset my destination to find a new destination
                        //Otherwise we are heading to an Epistyle or our destination is dead, and we don't know which yet
                        if ( tracing )
                            tracingBuffer.Add( transport.ToStringWithPlanet() + " has just arrived at its destination, " + transportData.Destination.ToString() + " (path A). Now discard its destination.\n" );

                        transportData.DestinationId = -1;
                        transportData.Destination = null;
                        continue;
                    }
                    debugCode = 1080;
                    for ( int j = 0; j < epistyles.Count; j++ )
                    {
                        debugCode = 1090;
                        GameEntity_Squad epistyle = epistyles[j].GetSquad();
                        if ( epistyle == null )
                            continue;
                        arrivedAtDestination = TransportResourcesIfAppropriate( transport, transportData, epistyle, tracing, tracingBuffer, ref hasCheckedDestination );

                        if ( arrivedAtDestination || hasCheckedDestination )
                            break; //we just checked out destination, so bail out of this loop
                    }
                    if ( arrivedAtDestination || !hasCheckedDestination )
                    {
                        if ( tracing )
                        {
                            if ( arrivedAtDestination )
                                tracingBuffer.Add( transport.ToStringWithPlanet() + " has just arrived at its destination (path B)\n" );
                            else if ( !hasCheckedDestination )
                                tracingBuffer.Add( transport.ToStringWithPlanet() + " has no destination, so discard it. Previous destination was " ).Add( transportData.DestinationId ).Add( "\n" );
                        }

                        //I have arrived (or I never found a destination, which means it died), so reset to find a new destination
                        transportData.DestinationId = -1;
                        transportData.Destination = null;
                    }
                    if ( transportData.DestinationId == -1 )
                    {
                        GetNextTransportDestination( transport, transportData, Context, PathCacheData );
                        if ( tracing )
                        {
                            if ( transportData.Destination != null )
                                tracingBuffer.Add( transport.ToStringWithPlanet() + " has just chosen " + transportData.Destination.ToStringWithPlanet() + " as a new destination.\n" );
                            else
                                tracingBuffer.Add( transport.ToStringWithPlanet() + " Couldn't find a destination. Try again later.\n" );
                        }

                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "In HandleTransportsSim debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }

            #region Tracing
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
            #endregion
        }
        //Figure out where this transport goes next. Can't do in LRP since we can race against data structures updated in the Sim
        private void GetNextTransportDestination( GameEntity_Squad entity, DarkZenithPerUnitBaseInfo data, ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData )
        {
            #region Tracing
            bool tracing = this.tracing_shortTerm =  BaseInfo.logTransportSim | (Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith ));
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-GetNextTransportDestination-trace", 10f ) : null;
            #endregion

            int debugCode = 0;

            List<SafeSquadWrapper> WorkingDestinations = GameEntity_Squad.GetTemporarySquadList( "DZ-GetNextTransportDestination-WorkingDestinations", 10f );
            if ( WorkingDestinations == null ) //blocked for teardown/shutdown; bail
                return;
            try
            {
                //First, clear any move orders (if we'd been en-route someplace that died)
                debugCode = 1000;
                if ( entity.HasQueuedOrders() )
                    entity.Orders.ClearOrders( ClearBehavior.AnythingIncludingAttackerMode, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "DZChangeMind" ); //clear my previous orders if I was previously moving
                debugCode = 1010;
                bool mustHaveResourcesAvailable = true;
                bool mustHaveNoOtherTransportsInbound = true;
                bool mustBeOnThisPlanet = true;
                if ( tracing )
                    tracingBuffer.Add( "Looking for a destination for " + entity.ToStringWithPlanet() ).Add( "\n" );
                if ( !data.HasAnyResourcesAtAll() )
                {
                    if ( tracing )
                        tracingBuffer.Add( "\t Looking path for a destination (we had no previous destination path)" ).Add( "\n" );

                    debugCode = 1100;
                    //If we have no resources at all, go pick some up from a terminus
                    data.Destination = FindTerminusForTransport( entity, mustHaveResourcesAvailable, mustHaveNoOtherTransportsInbound, !mustBeOnThisPlanet, Context );
                    debugCode = 1110;
                    if ( data.Destination != null ) //we might not have found a valid place to go
                    {
                        debugCode = 1120;
                        data.DestinationId = data.Destination.PrimaryKeyID;
                        if ( tracing )
                            tracingBuffer.Add( "Chosen " ).Add( data.Destination.ToStringWithPlanet() ).Add( " (we had no previous destination path)\n" );
                        return;
                    }
                }
                debugCode = 1200;
                int percentCheckNearbyTerminus = 30;
                if ( Context.RandomToUse.Next( 0, 100 ) < percentCheckNearbyTerminus )
                {
                    if ( tracing )
                        tracingBuffer.Add( "\t Looking path for a destination (found something convenient path)" ).Add( "\n" );

                    GameEntity_Squad easyNearbyTerminus = FindTerminusForTransport( entity, mustHaveNoOtherTransportsInbound, !mustHaveNoOtherTransportsInbound, mustBeOnThisPlanet, Context );
                    if ( easyNearbyTerminus != null )
                    {
                        //If we have resources but there is a really nearby terminus with resources, feel free to pick them up too
                        data.Destination = easyNearbyTerminus;
                        data.DestinationId = data.Destination.PrimaryKeyID;
                        if ( tracing )
                            tracingBuffer.Add( "Chosen " ).Add( data.Destination.ToStringWithPlanet() ).Add( " path (found something convenient path)\n" );
                        return;
                    }
                }
                //Check the Epistyles and Terminii to see who (if any) needs our resources
                List<SafeSquadWrapper> allEconomicStructures = this.BaseInfo.AllEconomicStructures.GetDisplayList();
                for ( int i = 0; i < allEconomicStructures.Count; i++ )
                {
                    debugCode = 1300;
                    GameEntity_Squad dest = allEconomicStructures[i].GetSquad();
                    if ( dest == null )
                        continue;
                    if ( dest.SecondsTillTransformation > 0 )
                        continue; //this is warping in
                                  //TODO: if there's no safe path, skip
                    DarkZenithPerUnitBaseInfo destData = dest.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    if ( destData.IsPirateEpistyle )
                        continue; //pirates don't get normal resources
                    if ( destData.WantsResourcesFrom( data ) )
                        WorkingDestinations.Add( dest );
                }
                debugCode = 1400;
                List<SafeSquadWrapper> transports = this.BaseInfo.Transports.GetDisplayList();
                debugCode = 1500;
                cb_dzSortData = data;
                cb_dzSortTransports = transports;
                WorkingDestinations.Sort( static delegate ( SafeSquadWrapper R, SafeSquadWrapper L )
                 {
                     GameEntity_Squad lShip = L.GetSquad();
                     GameEntity_Squad rShip = R.GetSquad();
                     DarkZenithPerUnitBaseInfo lData = lShip == null ? null : lShip.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                     DarkZenithPerUnitBaseInfo rData = rShip == null ? null : rShip.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                     int lScore = lData == null ? 0 : lData.HowMuchDoWeNeedResourcesFrom( lShip, cb_dzSortData, cb_dzSortTransports );
                     int rScore = rData == null ? 0 : rData.HowMuchDoWeNeedResourcesFrom( rShip, cb_dzSortData, cb_dzSortTransports );

                     return lScore.CompareTo( rScore );
                 } );
                if ( tracing )
                {
                    tracingBuffer.Add( "\tLooking Path C. There are " + WorkingDestinations.Count + " possible destinations now\n" );
                    for ( int i = 0; i < WorkingDestinations.Count; i++ )
                    {
                        GameEntity_Squad ship = WorkingDestinations[i].GetSquad();
                        if ( ship == null )
                            continue;
                        DarkZenithPerUnitBaseInfo workingPerUnitData = ship.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                        tracingBuffer.Add( i + ": " + WorkingDestinations[i].ToStringWithPlanet() + ", score " +  workingPerUnitData.HowMuchDoWeNeedResourcesFrom( WorkingDestinations[i].GetSquad(), data, transports ) +"\n" );
                    }
                }

                debugCode = 1600;
                if ( WorkingDestinations.Count == 0 )
                {
                    debugCode = 1700;
                    //if there's nothing that needs our resources, go pick some up
                    data.Destination = FindTerminusForTransport( entity, mustHaveResourcesAvailable, mustHaveNoOtherTransportsInbound, !mustBeOnThisPlanet, Context );
                    if ( data.Destination != null ) //we might not have found a valid place to go
                    {
                        data.DestinationId = data.Destination.PrimaryKeyID;
                        if ( tracing )
                            tracingBuffer.Add( "Chosen " ).Add( data.Destination.ToStringWithPlanet() ).Add( " path (no good epistyles, find me a terminus)\n" );
                    }
                    return;
                }
                debugCode = 1800;
                //prefer destinations that are higher priority
                int percentToUseForSelection = 80;
                GameEntity_Squad backup = null;
                for ( int i = 0; i < WorkingDestinations.Count; i++ )
                {
                    debugCode = 1900;
                    GameEntity_Squad potentialDestination = WorkingDestinations[i].GetSquad();
                    if ( potentialDestination == null )
                        continue;
                    if ( !CanTransportGetHereSafely( Context, PathCacheData, entity.Planet, potentialDestination.Planet) )
                    {
                        //ArcenDebugging.ArcenDebugLogSingleLine("i wish I could stop myself from going  " + entity.Planet.Name + " -> " + potentialDestination.Planet.Name, Verbosity.DoNotShow );
                        continue;
                    }
                    if ( backup == null )
                        backup = potentialDestination;
                    if ( Context.RandomToUse.Next( 0, 100 ) > percentToUseForSelection )
                    {
                        data.Destination = potentialDestination;
                        data.DestinationId = data.Destination.PrimaryKeyID;
                        if ( tracing )
                            tracingBuffer.Add( "Chosen " ).Add( data.Destination.ToStringWithPlanet() ).Add( " path D (weighted choice of best options)\n" );

                        break;
                    }
                }
                debugCode = 2000;
                //if we didn't pick one at random, use our backup (most desirable safe target if we have one)
                //if we have no backup then there are no safe places to go, so just wait
                if ( data.Destination == null && backup != null )
                {
                    debugCode = 2100;
                    data.Destination = backup;
                    data.DestinationId = data.Destination.PrimaryKeyID;
                    if ( tracing )
                        tracingBuffer.Add( "Chosen " ).Add( data.Destination.ToStringWithPlanet() ).Add( " path E (just take most desirable)\n" );

                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Hit  exception in GetNextTransportDestination code " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            finally
            {
                GameEntity_Squad.ReleaseTemporarySquadList( WorkingDestinations );
                FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
            }
        }
        private bool CanTransportGetHereSafely( ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData, Planet source, Planet destination )
        {
            if ( source == destination )
                return true;
            PathBetweenPlanetsForFaction pathCacheForDangerCalculation = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "DarkZenithCanTransportGetHereSafely", source, destination, PathingMode.Safest, Context, PathCacheData );
            if ( pathCacheForDangerCalculation == null )
                return false;
            for ( int i = 0; i < pathCacheForDangerCalculation.PathToReadOnly.Count; i++ )
            {
                PlanetFaction pFaction =  pathCacheForDangerCalculation.PathToReadOnly[i].GetPlanetFactionForFaction( AttachedFaction );
                int friendlyStrength = pFaction.DataByStance[FactionStance.Friendly].TotalStrength + pFaction.DataByStance[FactionStance.Self].TotalStrength;
                if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength > friendlyStrength )
                    return false;
            }
            return true;
        }

        private GameEntity_Squad FindTerminusForTransport( GameEntity_Squad transport, bool mustHaveResourcesAvailable, bool mustHaveNoOtherTransportsInbound, bool mustBeOnThisPlanet, ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = BaseInfo.logTransportSim || ( Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith ) );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-FindTerminusForTransport-trace", 10f ) : null;

            List<SafeSquadWrapper> WorkingListForRandomTerminus = GameEntity_Squad.GetTemporarySquadList( "DZ-FindTerminusForTransport-WorkingListForRandomTerminus", 10f );
            if ( WorkingListForRandomTerminus == null ) //blocked for teardown/shutdown; bail
                return null;

            //NOTE: Must not change any values in the PerUnit data
            //Uses the static WorkingDestinations list
            if ( tracing )
                tracingBuffer.Add( "\tFinding terminus for " + transport.ToStringWithPlanet() + " to get resources from\n" );
            List<SafeSquadWrapper> terminii = this.BaseInfo.Terminii.GetDisplayList();
            List<SafeSquadWrapper> transports = this.BaseInfo.Transports.GetDisplayList();
            for ( int i = 0; i < terminii.Count; i++ )
            {
                GameEntity_Squad terminus = terminii[i].GetSquad();
                if ( terminus == null )
                    continue;
                DarkZenithPerUnitBaseInfo tData = terminus.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( tracing ) tracingBuffer.Add( "\t\tChecking " + terminus.ToStringWithPlanet() ).Add( "\n" );
                if ( mustBeOnThisPlanet && transport.Planet != terminus.Planet )
                {
                    if ( tracing ) tracingBuffer.Add( "\t\t\tfail, path A; looking only at the local planet" ).Add( "\n" );
                    continue;
                }
                if ( mustHaveResourcesAvailable && !tData.HasResourcesAvailable( tracing, tracingBuffer, terminus ) )
                {
                    if ( tracing ) tracingBuffer.Add( "\t\t\tfail, path B; no resources available here" ).Add( "\n" );
                    continue;
                }
                GameEntity_Squad otherTransportInbound = null;
                for ( int j = 0; j < transports.Count; j++ )
                {
                    GameEntity_Squad otherTransport = transports[j].GetSquad();
                    if ( otherTransport == null )
                        continue;
                    DarkZenithPerUnitBaseInfo otherTData = otherTransport.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    if ( otherTData.Destination == terminus || otherTData.SecondaryDestination == terminus )
                    {
                        otherTransportInbound = otherTransport;
                        break;
                    }
                }
                if ( mustHaveNoOtherTransportsInbound && otherTransportInbound != null )
                {
                    if ( tracing ) tracingBuffer.Add( "\t\t\tfail, path C; transport " + otherTransportInbound.ToStringWithPlanet() + " is inbound" ).Add( "\n" );
                    continue;
                }
                WorkingListForRandomTerminus.Add( terminus );
            }

            if ( tracing ) tracingBuffer.Add( "\t\tfound " + WorkingListForRandomTerminus.Count + " possibilities" ).Add( "\n" );
            if ( WorkingListForRandomTerminus.Count == 0 )
            {
                FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
                GameEntity_Squad.ReleaseTemporarySquadList( WorkingListForRandomTerminus );
                return null;
            }
            //sort the working list to find the terminus we most want to go to
            WorkingListForRandomTerminus.Sort( static delegate ( SafeSquadWrapper R, SafeSquadWrapper L )
             {
                 GameEntity_Squad lShip = L.GetSquad();
                 GameEntity_Squad rShip = R.GetSquad();
                 DarkZenithPerUnitBaseInfo lData = lShip == null ? null : lShip.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                 DarkZenithPerUnitBaseInfo rData = rShip == null ? null : rShip.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                 int lScore = lData == null ? 0 : lData.GenerateAvailableResourceScore();
                 int rScore = rData == null ? 0 : rData.GenerateAvailableResourceScore();
                 return lScore.CompareTo( rScore );
             } );
            if ( tracing )
            {
                for ( int i = 0; i < WorkingListForRandomTerminus.Count; i++ )
                {
                    GameEntity_Squad ship = WorkingListForRandomTerminus[i].GetSquad();
                    if ( ship == null )
                        continue;
                    DarkZenithPerUnitBaseInfo wData = ship.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    tracingBuffer.Add("\t\t\t" + i + ": " + WorkingListForRandomTerminus[i].ToStringWithPlanet() ).Add( " (" + wData.GenerateAvailableResourceScore() + ")" ).Add( "\n" );
                }
            }

            if ( tracing ) tracingBuffer.Add( "\t\tidentified " + WorkingListForRandomTerminus[0].ToStringWithPlanet() ).Add( " as the next terminus\n" );
            
            GameEntity_Squad ret = WorkingListForRandomTerminus[0].GetSquad();
            GameEntity_Squad.ReleaseTemporarySquadList( WorkingListForRandomTerminus );

            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );

            return ret;
        }
        //A constructor ship is created to build a new structure someplace
        public void HandleConstructorsSim( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host

            List<SafeSquadWrapper> constructors = this.BaseInfo.Constructors.GetDisplayList();
            for ( int i = 0; i < constructors.Count; i++ )
            {
                GameEntity_Squad constructor = constructors[i].GetSquad();
                if ( constructor == null )
                    continue;
                //if the constructor is close to its destination, build it's Unit
                DarkZenithPerUnitBaseInfo data = constructor.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );

                if ( data.DZConstructorTargetPlanetIndex == -1 && data.Unit == null )
                {
                    //previously this was possibly because DZ Constructors could stack
                    ArcenDebugging.ArcenDebugLogSingleLine(constructor.ToStringWithPlanet() + " had no unit to build or planet to build on. Cleaning this up.", Verbosity.DoNotShow );
                    constructor.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                }

                if ( constructor.Planet.Index != data.DZConstructorTargetPlanetIndex )
                    continue; //we aren't on our planet
                if ( Mat.DistanceBetweenPointsImprecise( constructor.WorldLocation, data.DestinationPoint ) > BaseInfo.ResourceExchangeDistance )
                    continue; //we aren't close enough

                PlanetFaction pFaction = constructor.Planet.GetPlanetFactionForFaction( AttachedFaction );
                for ( int j = 0; j < data.NumberOfUnits; j++ )
                {
                    //We've already chosen a pretty precise place
                    GameEntity_Squad newEntity = SpawnStructureOrReturnNull( constructor.Planet, data.Unit, data.MarkLevel, data.DestinationPoint, Context );
                    if ( newEntity != null )
                    {
                        bool spawnsEnemiesForSidekick = false;
                        if ( data.Unit.GetHasTag( "WarpingInDZEpistyle" ) )
                        {
                            newEntity.TransformsIntoAfterTime = "DZEpistyle";
                            newEntity.SecondsTillTransformation = (Int16)Context.RandomToUse.Next( 25, 45 );
                            spawnsEnemiesForSidekick = true;
                        }
                        if ( data.Unit.GetHasTag( "WarpingInDZTerminus" ) )
                        {
                            newEntity.TransformsIntoAfterTime = DarkZenithFactionBaseInfo.GetTerminusTagFromResource( data.Resource );
                            newEntity.SecondsTillTransformation = (Int16)Context.RandomToUse.Next( 15, 30 );
                            spawnsEnemiesForSidekick = true;
                        }
                        if ( spawnsEnemiesForSidekick && this.IsHumanSidekick )
                            SpawnSidekickEnemies(Context);
                    }
                }
                constructor.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
            }
        }
        #region Sidekicks
        private void SpawnSidekickEnemies( ArcenHostOnlySimContext Context )
        {
            if (!this.IsHumanSidekick)
                return;
            Faction aiFaction = World_AIW2.GetRandomAIFaction ( Context );
            if ( aiFaction == null )
                return;
            GameEntity_Squad king = aiFaction.GetFactionKing();
            if ( king == null )
                return;
            int aip = GlobalAIWorldBaseInfo.Instance.AIProgress_Effective.IntValue;
            int shipsToSpawn = aip / 80 + 1;
            for ( int i = 0; i < shipsToSpawn; i++)
            {
                PlanetFaction pFaction = king.PlanetFaction;
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "AntiDarkZenithTierOne" );
                ArcenPoint spawnLocation = king.Planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, pFaction.Faction.CurrentGeneralMarkLevel,
                    pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "SpawnSidekickEnemies" );
            }

        }
        private void SpawnSidekickInitialStructures( GameEntity_Squad kingToSpawnWith, ArcenHostOnlySimContext Context )
        {
            //We want to make sure the DZ Sidekick spawns with some reasonable structures in Svikari mode
            FInt minRange = FInt.FromParts(0, 100);
            FInt maxRange = FInt.FromParts(0, 250);
            //spawn two metal Terminii
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DZMetalTerminus" );
            GameEntityTypeData harvesterData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DZHarvester" );
            int numTerminii = 2;
            for ( int i = 0; i < numTerminii; i++ )
            {
                SpawnStructureOrReturnNull( kingToSpawnWith.Planet, entityData, 1, kingToSpawnWith.WorldLocation, Context, minRange, maxRange  );
                SpawnStructureOrReturnNull( kingToSpawnWith.Planet, harvesterData, 1, kingToSpawnWith.WorldLocation, Context, minRange, maxRange );
            }

            //Spawn an epistyle 
            entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DZEpistyle" );
            SpawnStructureOrReturnNull( kingToSpawnWith.Planet, entityData, 1, Engine_AIW2.Instance.CombatCenter, Context, minRange, maxRange );
            //Spawn a transport
            entityData = GameEntityTypeDataTable.Instance.GetRowByName( "DZTransport" );
            SpawnStructureOrReturnNull( kingToSpawnWith.Planet, entityData, 1, Engine_AIW2.Instance.CombatCenter, Context, minRange, maxRange );
            //Then use the Difficulty invasion settings to figure out how much combat power to spawn
            FInt strength = BaseInfo.Difficulty.BaseInvasionStrengthPerPlanet;
            int percentTierZero = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkZenith_PercentTierZeroForInvasion" );
            int percentTierOne = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkZenith_PercentTierOneForInvasion" );
            int percentTierTwo = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkZenith_PercentTierTwoForInvasion" );
            int percentTierThree = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkZenith_PercentTierThreeForInvasion" );
            if ( percentTierZero + percentTierOne + percentTierTwo + percentTierThree != 100 )
               throw new Exception( "Problem with DZ invasion XML (sidekick svikari code path); the percent tiers do not add to 100" );
            int retries = 100;

            while ( strength > 0 && retries > 0 )
            {
                int random = Context.RandomToUse.Next( 0, 100 );
                if ( random < percentTierZero )
                {
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DZTierZeroBase" );
                }
                else if ( random + percentTierZero < percentTierOne )
                {
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DZTierOneBase" );
                }
                else if ( random + percentTierZero + percentTierOne < percentTierTwo )
                {
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DZTierTwoBase" );
                }
                else
                {
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DZTierThreeBase" );
                }
                if ( strength < entityData.CostForAIToPurchase )
                {
                    retries--;
                    continue;
                }
                strength -= entityData.CostForAIToPurchase;
                ArcenPoint spawnLocation = kingToSpawnWith.Planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                PlanetFaction pFaction = kingToSpawnWith.Planet.GetPlanetFactionForFaction( this.AttachedFaction );
                GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                                                             pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DZSidekickPopulatePlanet" );
                newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
            }
        }
        #endregion Sidekicks
        private GameEntity_Squad SpawnStructureOrReturnNull( Planet planet, GameEntityTypeData TypeData, byte markLevel, ArcenPoint nearPoint, ArcenHostOnlySimContext Context )
        {
            return SpawnStructureOrReturnNull( planet, TypeData, markLevel, nearPoint, Context, FInt.FromParts(0, 050), FInt.FromParts(0, 100) );
        }
        private GameEntity_Squad SpawnStructureOrReturnNull( Planet planet, GameEntityTypeData TypeData, byte markLevel, ArcenPoint nearPoint, ArcenHostOnlySimContext Context, FInt minRange , FInt maxRange )
        {
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
            ArcenPoint spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, TypeData, nearPoint, minRange, maxRange );

            GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, TypeData, markLevel,
                                                                     pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DarkZenith-SpawnStructure" );
            if ( newEntity == null )
                return null;
            newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;
            if ( newEntity.TypeData.IsMobileCombatant )
                newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
            return newEntity;
        }

        //If this transport is close enough to its destination, move resources unless someone has already handled the problem
        //returns true if we were within transport range
        private bool TransportResourcesIfAppropriate( GameEntity_Squad transport, DarkZenithPerUnitBaseInfo transportData, GameEntity_Squad potentialDestination, bool tracing, ArcenCharacterBuffer tracingBuffer, ref bool hasCheckedDestination )
        {
            if ( transport == null || potentialDestination == null )
                return false;
            DarkZenithPerUnitBaseInfo destinationData = potentialDestination.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
            bool isMySecondaryDestination = false;
            if ( transportData.DestinationId == potentialDestination.PrimaryKeyID )
                hasCheckedDestination = true;
            else if ( transportData.SecondaryDestinationId == potentialDestination.PrimaryKeyID )
                isMySecondaryDestination = true;
            else
            {
                hasCheckedDestination = false;
                return false;
            }
            if ( transport.Planet != potentialDestination.Planet )
                return false; //quick bailout
            int distance = Mat.DistanceBetweenPointsImprecise( transport.WorldLocation, potentialDestination.WorldLocation );
            if ( transport.Planet == potentialDestination.Planet &&
                 distance < BaseInfo.ResourceExchangeDistance )
            {
                if ( tracing )
                    tracingBuffer.Add( transport.ToStringWithPlanet() ).Add( " is in range " + distance + " to trade with " ).Add( potentialDestination.ToString() ).Add( "\n" );
                //we are in range; get resources if they are available
                if ( potentialDestination.TypeData.GetHasTag( "DZTerminus" ) )
                {
                    //Take no longer needed resources from this terminus
                    //note we might want to do this for epistyles too?
                    string log = "";
                    destinationData.TransferUnneededResourcesTo( tracing, tracingBuffer, ref transportData, ref log );
                    if ( tracing )
                        tracingBuffer.Add( "Transferred unneeded resources from " + potentialDestination.ToString() + " to " + transport.ToString() + ": " + log ).Add( "\n" );
                    if ( isMySecondaryDestination )
                        return false; //secondary only picks up resources
                }

                if ( destinationData.WantsResourcesFrom( transportData ) && hasCheckedDestination)
                {
                    string log = "";
                    //destinationData.TransferFullInventoryFrom( ref transportData, ref log ); //                    
                    if ( potentialDestination.TypeData.GetHasTag("DZEpistyle" ) )
                        destinationData.TransferRequestedInventoryFrom( ref transportData, ref log);
                    else if ( potentialDestination.TypeData.GetHasTag("DZTerminus" ) )
                    {
                        //For a terminus, transfer enough resources to allow up to 10 conversions
                        int maxConversionsOfResources = 10;
                        destinationData.TransferRequiredResourcesWithConversionMax( ref transportData, maxConversionsOfResources, ref log);
                    }
                    else
                        destinationData.TransferFullInventoryFrom( ref transportData,  ref log);
                    if ( tracing )
                        tracingBuffer.Add( "Transferring requested inventory from " + transport.ToString() + " to " + potentialDestination.ToString() + ": " + log );
                }

                return true;
            }
            return false;
        }
        public void HandleJormuandr( ArcenHostOnlySimContext Context )
        {

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-HandleJormuandr-trace", 10f ) : null;
            int debugCode = 0;
            try
            {
                List<SafeSquadWrapper> jormugandr = this.BaseInfo.Jormugandr.GetDisplayList();
                for ( int i = 0; i < jormugandr.Count; i++ )
                {
                    GameEntity_Squad entity = jormugandr[i].GetSquad();
                    if ( entity == null )
                        continue;
                    DarkZenithPerUnitBaseInfo data = entity.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    if ( tracing )
                    {
                        tracingBuffer.Add( "Handling " + entity.ToStringWithPlanet() + " . isJormugandr  " + data.IsJormugandr ).Add( ". " );
                        tracingBuffer.Add( " isDormant " + data.IsDormant + " untilDorm " + data.SecondsUntilDormancyMove ).Add( ". " );
                        tracingBuffer.Add( " active " + data.SecondsRemaingActive + ".\n" );
                    }
                    if ( data.IsDormant )
                    {
                        data.SecondsUntilDormancyMove--;
                        if ( data.SecondsUntilDormancyMove < -40 )
                        {
                            //the LRP sim can't update this value without a GameCommand, and I don't feel like making another.
                            //Plus, this is intended to be semi-random behaviour, so if there are fun races it's fine
                            //Note that the 
                            data.SecondsUntilDormancyMove = BaseInfo.Difficulty.TimeBetweenJormugandrDormantMoves + Context.RandomToUse.Next( 0, 5 );
                        }
                        if ( BaseInfo.IsHomeworldUnderAttack )
                        {
                            if ( tracing )
                                tracingBuffer.Add( entity.ToStringWithPlanet() + " is awakening.\n" );
                            data.IsDormant = false;
                            data.SecondsUntilDormancyMove = -1;
                            data.SecondsRemaingActive = BaseInfo.TimesHomeworldsAttacked * BaseInfo.Difficulty.TimeJorumugandrActivePerDZHomeworldAssault;
                        }

                    }
                    else if ( !data.IsDormant && !BaseInfo.IsHomeworldUnderAttack )
                    {
                        //we only do this countdown when the homeworlds aren't under attack
                        data.SecondsRemaingActive--;
                        if ( data.SecondsRemaingActive <= 0 )
                        {
                            if ( tracing )
                                tracingBuffer.Add( entity.ToStringWithPlanet() + " is becoming dormant.\n" );
                            entity.Orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "DZGoDormant" );
                            data.IsDormant = true;
                            data.SecondsUntilDormancyMove = BaseInfo.Difficulty.TimeBetweenJormugandrDormantMoves;
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in HandleJormuandr " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            #region Tracing
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
            #endregion
        }

        //Terminii do their Resource Conversion if they have enough resources
        public void HandleTerminiiAndEpistyles( ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host

            //The Terminus and Epistyle handling logic is basically identical
            int debugCode = 0;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-HandleTerminiiAndEpistyles-trace", 10f ) : null;

            try
            {
                List<SafeSquadWrapper> allEconomicStructures = this.BaseInfo.AllEconomicStructures.GetDisplayList();
                for ( int i = 0; i < allEconomicStructures.Count; i++ )
                {
                    debugCode = 100;
                    GameEntity_Squad entity = allEconomicStructures[i].GetSquad();
                    if ( entity == null )
                        continue;
                    DarkZenithPerUnitBaseInfo dzPerUnitInfo = entity.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );

                    if ( entity.SecondsTillTransformation > 0 )
                        continue; //this is still warping in
                    if ( entity.CurrentMarkLevel < AttachedFaction.CurrentGeneralMarkLevel )
                    {
                        //we've leveled up! Increase our mark level
                        entity.SetCurrentMarkLevel( AttachedFaction.CurrentGeneralMarkLevel );
                    }
                    debugCode = 200;
                    //For newly created structures, set its initial turrets and conversions
                    AddEconomicDefensiveStructures( entity, dzPerUnitInfo, Context );
                    debugCode = 205;
                    InitializeResourceConversionsIfNecessary( entity, dzPerUnitInfo, Context );
                    //If we don't have a conversion, choose a conversion.
                    //this includes "Build an Epistyle/Harvester if we don't have any" for metal terminii
                    debugCode = 210;

                    if ( entity.TypeData.GetHasTag( "DZMetalTerminus" ) && dzPerUnitInfo.NextConversion != null )
                    {
                        //bail out of bootstrapping things if we already have "enough" to be bootstrapped
                        debugCode = 220;
                        if ( dzPerUnitInfo.NextConversion.DisplayName == "Build Transport" && BaseInfo.Transports.Count >= 1 )
                            dzPerUnitInfo.NextConversion = null;
                        else if ( dzPerUnitInfo.NextConversion.DisplayName == "Build Epistyle" && (HasAnyInfrastructureEpistyles( false ) || BaseInfo.WarpingInEpistyles.Count > 0) )
                            dzPerUnitInfo.NextConversion = null;
                    }
                    debugCode = 230;
                    if ( dzPerUnitInfo.NextConversion == null )
                    {
                        debugCode = 300;
                        if ( entity.TypeData.GetHasTag( "DZTerminus" ) )
                        {
                            debugCode = 310;
                            if ( !dzPerUnitInfo.HasList )
                                throw new Exception( entity.ToStringWithPlanet() + " doesn't have HasList set." );
                            if ( dzPerUnitInfo.ConversionList.Count == 0 )
                                throw new Exception( entity.ToStringWithPlanet() + " doesn't have a list. Resource " + dzPerUnitInfo.Resource + " time enterted planet " + entity.GameSecondEnteredThisPlanet + " current time " + World_AIW2.Instance.GameSecond );
                            debugCode = 315;
                            if ( entity.TypeData.GetHasTag( "DZMetalTerminus" ) )
                            {
                                //metal terminii only build things for bootstrapping; the default is to have a null NextConversion
                                debugCode = 320;
                                if ( BaseInfo.CanPlanetBuildHarvesters(entity.Planet) )
                                {
                                    dzPerUnitInfo.NextConversion = dzPerUnitInfo.GetConversionFromListByNameIfPossible( "Build Harvester" );
                                    if ( dzPerUnitInfo.NextConversion == null )
                                        throw new Exception( "No build harvester conversion found on " + entity.ToStringWithPlanet() );
                                }
                                else if ( !HasAnyInfrastructureEpistyles( false ) && (BaseInfo.WarpingInEpistyles.Count + NumConstructorsGoingToBuild("WarpingInDZEpistyle" ) ) == 0 )
                                {
                                    //if there is a warping in epistyle but no infrastructure epistyles then the warping one should become
                                    //infrastructure
                                    dzPerUnitInfo.NextConversion = dzPerUnitInfo.GetConversionFromListByNameIfPossible( "Build Epistyle" );
                                    if ( dzPerUnitInfo.NextConversion == null )
                                        throw new Exception( "No build epistyle conversion found on " + entity.ToStringWithPlanet() );
                                }
                                else if ( BaseInfo.Transports.Count == 0 )
                                {
                                    dzPerUnitInfo.NextConversion = dzPerUnitInfo.GetConversionFromListByNameIfPossible( "Build Transport" );
                                    if ( dzPerUnitInfo.NextConversion == null )
                                        throw new Exception( "No build transport conversion found on " + entity.ToStringWithPlanet() );
                                }
                                dzPerUnitInfo.UpdateNeededResources( dzPerUnitInfo.NextConversion, entity, tracing, tracingBuffer );
                                if ( tracing && dzPerUnitInfo.NextConversion != null )
                                    tracingBuffer.Add( entity.ToString() + " has chosen conversion " + dzPerUnitInfo.NextConversion );
                                continue;
                            }
                            debugCode = 325;
                            //for all non-metal terminii, pick one at random (this is pretty much exclusively "the next conversion is 'convert resources'")
                            dzPerUnitInfo.NextConversion = GetNextConversion( entity, dzPerUnitInfo, Context, PathCacheData );
                        }
                        debugCode = 330;
                        if ( entity.TypeData.GetHasTag( "DZEpistyle" ) )
                        {
                            debugCode = 350;
                            //For epistyles
                            if ( !dzPerUnitInfo.HasBag )
                                throw new Exception( entity.ToStringWithPlanet() + " doesn't have HasBag set." );
                            //Some code for bootstrapping. Lets make sure our infrastructure epistyles are doing what is necessary
                            debugCode = 352;
                            if ( dzPerUnitInfo.CanBuildUtility )
                            {
                                //we have too few transports for our planets; high priority
                                if ( BaseInfo.Transports.Count + NumConstructorsGoingToBuild( "DZTransport" ) + NumEpistylesWithConversion( "Build Transport" ) < BaseInfo.PlanetsControlled.Count )
                                    dzPerUnitInfo.NextConversion = dzPerUnitInfo.GetConversionFromListByNameIfPossible( "Build Transport" );
                            }
                            if ( dzPerUnitInfo.CanBuildInfrastructure && dzPerUnitInfo.NextConversion == null )
                            {
                                debugCode = 354;
                                //This is bootstrapping code to make sure we have a few critical structures.
                                //Make sure to build at least one epistyle early as long as we have any metal terminii
                                if ( tracing )
                                    tracingBuffer.Add("Considering bootstrap. numEpistyles " + BaseInfo.Epistyles.Count + " and num metal terminii " + NumTerminiiOrConstructorsForResource( DZResource.Metal ) + " + " + NumEpistylesWithConversion( "Build Metal Terminus" )  ).Add(" \n");


                                if ( BaseInfo.Epistyles.Count <= 2 && NumTerminiiOrConstructorsForResource( DZResource.Metal ) >= 2 )
                                    dzPerUnitInfo.NextConversion = dzPerUnitInfo.GetConversionFromListByNameIfPossible( "Build Epistyle" );
                                else if ( NumTerminiiOrConstructorsForResource( DZResource.Metal ) + NumEpistylesWithConversion( "Build Metal Terminus" ) < BaseInfo.BaseMetalTerminii )
                                {
                                    dzPerUnitInfo.NextConversion = dzPerUnitInfo.GetConversionFromListByNameIfPossible( "Build Metal Terminus" );
                                    if ( dzPerUnitInfo.NextConversion == null )
                                        throw new Exception("could not find conversion 'build metal terminus'" );
                                }
                                else if ( NumTerminiiOrConstructorsForResource( DZResource.Green ) + NumEpistylesWithConversion( "Build Green Terminus" ) < BaseInfo.BaseGreenTerminii )
                                    dzPerUnitInfo.NextConversion = dzPerUnitInfo.GetConversionFromListByNameIfPossible( "Build Green Terminus" );
                                else if ( NumTerminiiOrConstructorsForResource( DZResource.White ) + NumEpistylesWithConversion( "Build White Terminus" ) < BaseInfo.BaseWhiteTerminii )
                                    dzPerUnitInfo.NextConversion = dzPerUnitInfo.GetConversionFromListByNameIfPossible( "Build White Terminus" );
                                else if ( NumTerminiiOrConstructorsForResource( DZResource.Blue ) + NumEpistylesWithConversion( "Build Blue Terminus" ) < BaseInfo.BaseBlueTerminii )
                                    dzPerUnitInfo.NextConversion = dzPerUnitInfo.GetConversionFromListByNameIfPossible( "Build Blue Terminus" );

                                if ( tracing )
                                {
                                    if ( dzPerUnitInfo.NextConversion != null )
                                        tracingBuffer.Add( "Selected " + dzPerUnitInfo.NextConversion.InternalName + " for " + entity.ToStringWithPlanet() + " bootstrap path\n" );
                                    else
                                        tracingBuffer.Add( "No bootstrap chosen. Metal A " + NumTerminiiOrConstructorsForResource( DZResource.Metal ) + " and B " + NumEpistylesWithConversion( "Build Metal Terminus" ) + "\n" );
                                }
                            }
                            debugCode = 356;
                            //if that didn't find us something specific to build, just get something at random
                            if ( dzPerUnitInfo.NextConversion == null )
                            {
                                debugCode = 357;
                                dzPerUnitInfo.NextConversion = this.GetNextConversion( entity, dzPerUnitInfo, Context, PathCacheData );
                            }
                        }
                        // if ( data.NextConversion == null )
                        //     entity.ToStringWithPlanet() + " couldn't find next conversion");

                    }
                    //check if we can still afford a conversion (for example, some enemy might have killed all our Thaumite production)
                    string reason = "";
                    if ( dzPerUnitInfo.NextConversion != null &&
                         !CanGetResourcesForConversion( dzPerUnitInfo.NextConversion, out reason) )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "GetNextConversion: \t\tD'oh! We can no longer afford; " + dzPerUnitInfo.NextConversion ).Add( " due to " + reason +". Discard it\n" );
                        dzPerUnitInfo.NextConversion = null;
                        dzPerUnitInfo.UpdateNeededResources( dzPerUnitInfo.NextConversion, entity, tracing, tracingBuffer );
                        continue;
                    }
                    //If we have enough resources, do the conversion then pick the next conversion
                    debugCode = 400;
                    if ( dzPerUnitInfo.CanWeDoResourceConversion( dzPerUnitInfo.NextConversion ) &&
                         (World_AIW2.Instance.GameSecond - dzPerUnitInfo.TimeWeLastDidConversion) > BaseInfo.Difficulty.TimeBetweenResourceConversions )
                    {
                        debugCode = 500;
                        // if ( tracing )
                        //     tracingBuffer.Add( entity.ToStringWithPlanet() +  " is attempting to do conversion " + data.NextConversion ).Add(". There are " + AllEconomicStructures.Count + " economic structures here\n");
                        dzPerUnitInfo.ExecuteResourceConversion( entity, dzPerUnitInfo.NextConversion, BaseInfo, BaseInfo.Difficulty,
                            BaseInfo.AllEconomicStructures.GetDisplayList(), BaseInfo.Constructors.GetDisplayList(), BaseInfo.Utilities.GetDisplayList(), tracing, tracingBuffer, Context, PathCacheData );
                        if ( !dzPerUnitInfo.KeepConversion )
                        {
                            dzPerUnitInfo.NextConversion = null;
                            dzPerUnitInfo.HighPriority = false;
                        }
                        dzPerUnitInfo.UpdateNeededResources( dzPerUnitInfo.NextConversion, entity, tracing, tracingBuffer );
                        continue; //we'll pick a new conversion next SimStep
                    }
                    debugCode = 600;
                    //Metal Terminii have some bonus income
                    if ( entity.TypeData.GetHasTag( "DZMetalTerminus" ) )
                    {
                        if ( BaseInfo.HarvestersOnPlanet( entity.Planet, false ) == 0 )
                        {
                            debugCode = 610;
                            if ( !dzPerUnitInfo.Inventory.ContainsKey( DZResource.Metal ) )
                                dzPerUnitInfo.Inventory[DZResource.Metal] = 0;
                            debugCode = 620;
                            dzPerUnitInfo.Inventory[DZResource.Metal] += BaseInfo.Difficulty.MetalIncomeWithoutHarvesters;
                        }
                    }

                    if ( World_AIW2.Instance.GameSecond % BaseInfo.PermanentBonusIncomeInterval == 0 )
                    {
                        //Permanent Bonus Income only runs every few seconds, since the DZ was starting to make
                        //all its income with bonus income
                        foreach ( KeyValuePair<DZResource,int> kv in dzPerUnitInfo.PermanentBonusIncome )
                        {
                            if ( kv.Value > 0 )
                                dzPerUnitInfo.Inventory[kv.Key] += kv.Value;
                        }
                    }

                    //Update our NeedForResources.
                    //In this case we have a ResourceConversion but can't build it yet
                    // if ( tracing )
                    //     tracingBuffer.Add("Updating needed resources for " + entity.ToStringWithPlanet() + " with conversion " + data.NextConversion.ToString() + " isResourceCreation " + data.NextConversion.IsResourceCreation + " isOffensive " + data.NextConversion.IsOffensive + " upgradeidx " + data.NextConversion.UpgradeIndex + " conversion index " + data.NextConversion.ConversionIndex +"\n");

                    dzPerUnitInfo.UpdateNeededResources( dzPerUnitInfo.NextConversion, entity, tracing, tracingBuffer );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in HandleTerminiiAndEpistyles code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            #region Tracing
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
            #endregion
        }
        private static DZResourceConversion GetTerminusConversionForResource( DarkZenithPerUnitBaseInfo data, DZResource resource )
        {
            switch ( resource )
            {
                case DZResource.Metal: return data.GetConversionFromListByNameIfPossible( "Build Metal Terminus" );
                case DZResource.Green: return data.GetConversionFromListByNameIfPossible( "Build Green Terminus" );
                case DZResource.Blue: return data.GetConversionFromListByNameIfPossible( "Build Blue Terminus" );
                case DZResource.White: return data.GetConversionFromListByNameIfPossible( "Build White Terminus" );
                case DZResource.Red: return data.GetConversionFromListByNameIfPossible( "Build Red Terminus" );
                case DZResource.Black: return data.GetConversionFromListByNameIfPossible( "Build Black Terminus" );
                default: return null;
            }
        }
        private static List<DZResourceConversion> UpgradeChoices = List<DZResourceConversion>.Create_WillNeverBeGCed( 30, "DarkZenithFactionDeepInfoRoot-UpgradeChoices" );
        private DZResourceConversion GetNextConversion( GameEntity_Squad structure, DarkZenithPerUnitBaseInfo data, ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData )
        {
            //this is a critical function for the DZ economy, since it decides what each epistyle will do next
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-GetNextConversion-trace", 10f ) : null;
            DZResourceConversion conversion = null;
            int numPotentialPlanetsForTerminus = 0;
            int numPotentialPlanetsForEpistyles = 0;
            int debugCode = 0;

            List<Planet> PotentialPlanets = Planet.GetTemporaryPlanetList( "DZ-GetNextConversion-PotentialPlanets", 10f );
            if ( PotentialPlanets == null ) //blocked for teardown/shutdown; bail
                return null;
            DictionaryOfLists<Planet, SafeSquadWrapper> WorkingStructuresOnThisPlanet = GameEntity_Squad.GetTemporarySquadsPerPlanetDictOfLists( "DZ-GetNextConversion-WorkingStructuresOnThisPlanet", 10f );
            if ( WorkingStructuresOnThisPlanet == null ) //blocked for teardown/shutdown; bail
            {
                Planet.ReleaseTemporaryPlanetList( PotentialPlanets );
                return null;
            }

            try
            {
                int retries = 6;
                debugCode = 100;
                if ( tracing )
                    tracingBuffer.Add( "GetNextConversion: finding conversion for " + structure.ToStringWithPlanet() ).Add( "\n" ); ;
                if ( data.IsPirateEpistyle )
                    retries = 2; //pirates often don't have enough resources (and have few options), so don't retry constantly
                int numVariantUpgrades = BaseInfo.GetVariantUpgrades();
                do
                {
                    debugCode = 200;
                    if ( data.CanBuildInfrastructure )
                    {
                        //First, some bonus intelligence for infrastructure; try to quickly build epistyles and terminii if possible
                        debugCode = 300;
                        //prioritize building terminii if possible for economic reasons
                        int maxTerminiiForInitialPreference = 10;
                        int maxEpistylesForInitialPreference = 7;
                        GameEntityTypeData typedata = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "WarpingInDZTerminus" );
                        DarkZenithPerUnitBaseInfo.UpdatePotentialPlanetsToBuildOn( PotentialPlanets, WorkingStructuresOnThisPlanet, structure, typedata,
                            BaseInfo.AllEconomicStructures.GetDisplayList(), BaseInfo.Constructors.GetDisplayList(), BaseInfo.Utilities.GetDisplayList(), 
                            BaseInfo.Difficulty, BaseInfo, tracing, tracingBuffer, Context, PathCacheData );
                        Planet planetNeedingTerminus = null;
                        Planet planetNeedingEpistyle = null;
                        numPotentialPlanetsForTerminus = PotentialPlanets.Count;
                        debugCode = 400;
                        if ( tracing )
                            tracingBuffer.Add( "GetNextConversion: found " + numPotentialPlanetsForTerminus + " planets we can build a terminus on").Add("\n" ); ;

                        debugCode = 410;
                        if ( conversion == null && numPotentialPlanetsForTerminus > 0 && BaseInfo.RequestedTerminusResource != DZResource.None )
                        {
                            //the player asked (via the DZ Logistics popout) for the next Terminus built to be of a specific resource type
                            DZResourceConversion requestedConversion = GetTerminusConversionForResource( data, BaseInfo.RequestedTerminusResource );
                            string requestReason;
                            if ( requestedConversion != null && CanGetResourcesForConversion( requestedConversion, out requestReason ) )
                            {
                                if ( tracing )
                                    tracingBuffer.Add( "GetNextConversion: \tFulfilling player request to build " + requestedConversion.ToString() + " next.\n" );
                                conversion = requestedConversion;
                                BaseInfo.RequestedTerminusResource = DZResource.None; //one-shot; consumed
                            }
                        }

                        if ( conversion == null && PotentialPlanets.Count > 0 && BaseInfo.Terminii.Count < maxTerminiiForInitialPreference )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "GetNextConversion: considering bootstrapping a terminus " + structure.ToStringWithPlanet() ).Add( "\n" ); ;

                            debugCode = 410;
                            //we can build a Terminus
                            for ( int i = 0; i < PotentialPlanets.Count; i++ )
                            {
                                debugCode = 420;
                                Planet planet = PotentialPlanets[i];
                                if ( WorkingStructuresOnThisPlanet[planet] == null ||
                                     WorkingStructuresOnThisPlanet[planet].Count == 0 )
                                {
                                    planetNeedingTerminus = planet;
                                    break;
                                }
                            }

                            debugCode = 500;
                            if ( planetNeedingTerminus != null )
                            {
                                debugCode = 510;
                                //lets pick a random terminus
                                DZResource randomResource = DarkZenithFactionBaseInfo.GetRandomResourceType( Context );
                                if ( tracing ) 
                                    tracingBuffer.Add( "GetNextConversion: \t we found planet " + planetNeedingTerminus.Name + " needs a terminus (we have only " + BaseInfo.Terminii.Count + " of " + maxTerminiiForInitialPreference + ". Randomly chose " + randomResource.ToString() + " as a terminus for that planet" ).Add( "\n" );
                                if ( randomResource == DZResource.Metal )
                                    conversion = data.GetConversionFromListByNameIfPossible( "Build Metal Terminus" );
                                else if ( randomResource == DZResource.Green )
                                    conversion = data.GetConversionFromListByNameIfPossible( "Build Green Terminus" );
                                else if ( randomResource == DZResource.Blue )
                                    conversion = data.GetConversionFromListByNameIfPossible( "Build Blue Terminus" );
                                else if ( randomResource == DZResource.White )
                                    conversion = data.GetConversionFromListByNameIfPossible( "Build White Terminus" );
                                else if ( randomResource == DZResource.Red )
                                    conversion = data.GetConversionFromListByNameIfPossible( "Build Red Terminus" );
                                else if ( randomResource == DZResource.Black )
                                    conversion = data.GetConversionFromListByNameIfPossible( "Build Black Terminus" );
                            }
                        }
                        //now see about creating a quick epistyle or two
                        if ( conversion == null )
                        {
                            typedata = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "WarpingInDZEpistyle" );
                            DarkZenithPerUnitBaseInfo.UpdatePotentialPlanetsToBuildOn( PotentialPlanets, WorkingStructuresOnThisPlanet, structure, typedata,
                                BaseInfo.AllEconomicStructures.GetDisplayList(), BaseInfo.Constructors.GetDisplayList(), BaseInfo.Utilities.GetDisplayList(), 
                                BaseInfo.Difficulty, BaseInfo, tracing, tracingBuffer, Context, PathCacheData );
                            numPotentialPlanetsForEpistyles = PotentialPlanets.Count;
                            if ( PotentialPlanets.Count > 0 && (BaseInfo.Epistyles.Count < maxEpistylesForInitialPreference ||
                                                                                      BaseInfo.UnownedPlanetsInTerritorialSphereSim.Count > 0 ) )
                            {
                                debugCode = 410;
                                //we can build a Epistyle
                                for ( int i = 0; i < PotentialPlanets.Count; i++ )
                                {
                                    debugCode = 420;
                                    Planet planet = PotentialPlanets[i];
                                    if ( WorkingStructuresOnThisPlanet[planet] == null ||
                                         WorkingStructuresOnThisPlanet[planet].Count == 0 )
                                    {
                                        planetNeedingEpistyle = planet;
                                        break;
                                    }
                                }

                                debugCode = 500;
                                if ( planetNeedingEpistyle != null )
                                {
                                    debugCode = 510;
                                    if ( tracing )
                                        tracingBuffer.Add( "GetNextConversion: \t we found planet " + planetNeedingEpistyle.Name + " needs a epistyle (we have only " + BaseInfo.Epistyles.Count + " of the preferred " + maxEpistylesForInitialPreference + "." ).Add( "\n" );
                                    conversion = data.GetConversionFromListByNameIfPossible( "Build Epistyle" );
                                }
                            }
                        }
                    }
                    debugCode = 600;
                    if ( data.CanBuildUpgrades )
                    {
                        int upgradeRetries = 20;
                        //allow a couple retries for upgrades
                        UpgradeChoices.Clear();
                        do{
                            if ( data.HasBag )
                            {
                                debugCode = 620;
                                conversion = data.ConversionBag.PickRandomItemAndReplace( Context.RandomToUse );
                                if ( tracing )
                                    tracingBuffer.Add( "GetNextConversion: \tUpdate path, has bag, picking from " + data.ConversionBag.InternalListSize + " options and got " ).Add( conversion.ToString() ).Add( "\n" );

                                if ( conversion._UpgradeIndex <= 0 )
                                {
                                    conversion = null;
                                    if ( tracing )
                                        tracingBuffer.Add( "GetNextConversion: \tUpdate path, dropping because not an upgrade\n" );

                                    continue;
                                }
                                DZUpgrade upgrade = conversion.Upgrade;
                                if ( !BaseInfo.IsUpgradeAllowed( upgrade, numVariantUpgrades ) )
                                     conversion = null;

                                debugCode = 660;
                            }
                            if ( conversion != null )
                                UpgradeChoices.Add(conversion);
                        }while ( UpgradeChoices.Count < 10 && upgradeRetries-- > 0 );
                        int percentMilitaryUpgrade = 75;
                        if ( Context.RandomToUse.Next(0, 100) < percentMilitaryUpgrade )
                        {
                            //try to find a military upgrade
                            for ( int i = 0; i < UpgradeChoices.Count; i++ )
                            {
                                DZUpgrade upgrade = UpgradeChoices[i].Upgrade;
                                if ( upgrade.UnlockShipVariant ||
                                     upgrade.UnlockShipTier ||
                                     upgrade.UnlockMarkLevel )
                                {
                                    conversion = UpgradeChoices[i];
                                    if ( tracing )
                                        tracingBuffer.Add( "GetNextConversion: \tUpdate path, choosing military choice\n" );

                                    break;
                                }
                            }
                        }
                        if ( conversion == null && UpgradeChoices.Count > 0 )
                            conversion = UpgradeChoices[0];
                    }

                    if ( conversion == null )
                    {
                        debugCode = 6100;
                        if ( data.HasBag )
                        {
                            debugCode = 6200;
                            if ( tracing )
                                tracingBuffer.Add( "GetNextConversion: \thas bag, picking from " + data.ConversionBag.InternalListSize + " options" ).Add( "\n" );
                            conversion = data.ConversionBag.PickRandomItemAndReplace( Context.RandomToUse );
                            debugCode = 6300;
                            if ( conversion == null )
                                throw new Exception(structure.ToStringWithPlanetAndOwner() + " could not find a conversion. It has " + data.ConversionBag.InternalListSize + " items in its bag and " + data.ConversionList.Count + " items on its list");
                            if ( conversion.Upgrade != null )
                            {
                                debugCode = 6400;
                                //make sure its okay for us to get this upgrade
                                DZUpgrade upgrade = conversion.Upgrade;
                                if ( !BaseInfo.IsUpgradeAllowed( upgrade, numVariantUpgrades ) )
                                    conversion = null;
                            }
                            debugCode = 6450;
                            if ( conversion != null && data.MaxOffenseTier > 0 && conversion.IsOffensive )
                            {
                                int conversionUITier = 1; //DZTierZero
                                if ( conversion.TagForUnit != null )
                                {
                                    if ( conversion.TagForUnit.StartsWith( "DZTierThree" ) )
                                        conversionUITier = 4;
                                    else if ( conversion.TagForUnit.StartsWith( "DZTierTwo" ) )
                                        conversionUITier = 3;
                                    else if ( conversion.TagForUnit.StartsWith( "DZTierOne" ) )
                                        conversionUITier = 2;
                                    else if ( conversion.TagForUnit.StartsWith( "DZTierZero" ) )
                                        conversionUITier = 1;
                                }
                                if ( conversionUITier > data.MaxOffenseTier )
                                {
                                    if ( tracing )
                                        tracingBuffer.Add( "GetNextConversion: \t\tD'oh! " + conversion + " is above the offense tier cap (" + data.MaxOffenseTier + ") for this Epistyle. Discard and pick again\n" );
                                    conversion = null;
                                }
                            }
                        }
                        else
                        {
                            conversion = data.ConversionList[Context.RandomToUse.Next( 0, data.ConversionList.Count )];
                        }
                        if ( conversion != null && conversion.TagForUnit == "DZHarvester" &&
                             AttachedFaction.Type == FactionType.Player)
                        {
                            //Check if we are over the metal harvester cap for this planet
                            //There's a longstanding bug for the DZ that gave them more metal harvesters.
                            //I don't want to change balance in the base game, so we're only checking the cap for the Player

                            if ( !BaseInfo.CanPlanetBuildHarvesters(structure.Planet) )
                            {
                                conversion = null;
                            }
                        }
                        if ( tracing )
                        {
                            if ( conversion != null )
                                tracingBuffer.Add( "GetNextConversion: \tRandom choice: " + conversion ).Add( "\n" );
                            else
                                tracingBuffer.Add( "GetNextConversion: \tRandom choice: null (better luck next time?)" ).Add( "\n" );
                        }
                    }
                    if ( conversion == null )
                        continue; //this can happen if we randomly picked something we weren't allowed to use
                    debugCode = 700;
                    string reason = "";
                    if ( !CanGetResourcesForConversion( conversion, out reason ) )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "GetNextConversion: \t\tD'oh! We can't afford " + conversion ).Add( ", we are missing <" + reason + ">. Discard the previous choice and pick again\n" );
                        conversion = null; //we can't afford this one; pick another at random
                        continue;
                    }
                    debugCode = 800;
                    if ( conversion.Unit == null && !String.IsNullOrEmpty(conversion.NameForUnit) )
                        conversion.Unit = GameEntityTypeDataTable.Instance.GetRowByName( conversion.NameForUnit );
                    if ( conversion.Unit != null )
                    {
                        //NOTE: for these checks, we must have a UnitNameToBuild not a UnitTag (we don't figure out which tag we are using till the conversion is being executed)
                        if ( numPotentialPlanetsForEpistyles == 0 && conversion.Unit.GetHasTag( "WarpingInDZEpistyle" ) )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "GetNextConversion: \t\tD'oh! We can't build any more epistyles; " + conversion ).Add( ". Discard the previous choice and pick again\n" );
                            conversion = null; //we can't afford this one; pick another at random
                            continue;
                        }
                        debugCode = 900;
                        if ( numPotentialPlanetsForTerminus == 0 && conversion.Unit.GetHasTag( "WarpingInDZTerminus" ) )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "GetNextConversion: \t\tD'oh! We can't build any more terminii; " + conversion ).Add( ". Discard the previous choice and pick again\n" );
                            conversion = null; //we can't afford this one; pick another at random
                            continue;
                        }
                        debugCode = 950;
                        if (  conversion.Unit.GetHasTag( "DZTransport" ) &&
                              (BaseInfo.Transports.Count + NumConstructorsGoingToBuild( "DZTransport" ) + NumEpistylesWithConversion( "Build Transport" ) ) > BaseInfo.PlanetsControlled.Count )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "GetNextConversion: \t\tD'oh! We can't build any more transports; " + conversion ).Add( ". Discard the previous choice and pick again\n" );
                            conversion = null; //we can't afford this one; pick another at random
                            continue;
                        }
                    }
                    else
                    {
                        if ( tracing )
                            tracingBuffer.Add("Conversion " + conversion.ToString() + " does not have a Unit\n");
                    }

                    debugCode = 1000;
                } while ( conversion == null && retries-- > 0 );
                debugCode = 1100;
                if ( conversion != null &&
                     (conversion.TagForUnit == "WarpingInDZTerminus" || conversion.NameForUnit == "WarpingInDZTerminus") &&
                     conversion.RelatedResource == DZResource.None )
                    throw new Exception( "Found bad conversion <" + conversion.ToString() + "> in GetNextConversion; it looks like the related resource is DZResource.None" );
                if ( tracing && conversion != null )
                {
                    tracingBuffer.Add(" numPotentialPlanetsForTerminus " + numPotentialPlanetsForTerminus + " numPotentialPlanetsForEpistyles " + numPotentialPlanetsForEpistyles ).Add("\n");
                    tracingBuffer.Add( "GetNextConversion: \tFinal choice for " + structure.ToStringWithPlanet() + ": " + conversion ).Add( "\n" );
                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in GetNextConversion for " + structure.ToString() + " code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            finally
            {
                FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );

                Planet.ReleaseTemporaryPlanetList( PotentialPlanets );
                GameEntity_Squad.ReleaseTemporarySquadsPerPlanetDictOfLists( WorkingStructuresOnThisPlanet );
            }
            return conversion;
        }
        
        private bool CanGetResourcesForConversion( DZResourceConversion conversion, out string reason )
        {
            if ( conversion == null )
                throw new Exception( "Null conversion in CanGetResourcesForConversion" );
            //foreach resource in the cost, make sure we are producing that resource
            List<SafeSquadWrapper> terminii = this.BaseInfo.Terminii.GetDisplayList();
            foreach ( KeyValuePair<DZResource, int> kv in conversion.Cost )
            {
                if ( kv.Value > 0 )
                {
                    bool foundProduction = false;
                    for ( int i = 0; i < terminii.Count; i++ )
                    {
                        GameEntity_Squad ship = terminii[i].GetSquad();
                        if ( ship == null )
                            continue;
                        DarkZenithPerUnitBaseInfo data = ship.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                        if ( data.Resource == kv.Key )
                        {
                            foundProduction = true;
                            continue;
                        }
                    }
                    if ( !foundProduction )
                    {
                        reason = "at least " + kv.Key;
                        return false;
                    }
                }
            }
            reason = "";
            return true;
        }
        private bool HasAnyInfrastructureEpistyles( bool ignoreOffensive )
        {
            List<SafeSquadWrapper> epistyles = this.BaseInfo.Epistyles.GetDisplayList();
            for ( int i = 0; i < epistyles.Count; i++ )
            {
                GameEntity_Squad epistyle = epistyles[i].GetSquad();
                if ( epistyle == null )
                    continue;
                DarkZenithPerUnitBaseInfo data = epistyle.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( ignoreOffensive && data.CanBuildOffensiveUnits )
                    continue;
                if ( data.CanBuildInfrastructure )
                    return true;
            }
            return false;
        }
        private bool HasUnlockedTierTwoUnits()
        {
            //Pirates bring high tier units, so these are more for late game
            for ( int i = 0; i < BaseInfo.CompletedUpgrades.Count; i++ )
            {
                DZUpgrade upgrade = BaseInfo.CompletedUpgrades[i];
                if ( upgrade.UnlockShipTier && upgrade.RelatedInteger1 >= 2 )
                    return true;
            }
            return false;
        }
        private void AddEconomicDefensiveStructures( GameEntity_Squad structureToDefend, DarkZenithPerUnitBaseInfo data, ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host
            if ( !AttachedFaction.SpecialFactionData.FullInvasionMode )
                return; //only for invading DZ; svikari have allies

            //When an Epistyle or Terminus is created, it doesn't have ResourceConversion initialized. Use that as the check
            if ( data.ConversionList.Count != 0 || data.ConversionBag.InternalListSize != 0 )
                return;
            //Spawn some defensive structures
            Planet planet = structureToDefend.Planet;
            int numWeakDefensiveTurrets = Context.RandomToUse.Next(3, 4);
            int numStrongDefensiveTurrets = 1;
            PlanetFaction pFaction = structureToDefend.PlanetFaction;
            for ( int i = 0; i < numWeakDefensiveTurrets; i++ )
            {
                GameEntityTypeData typedata = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "DZDefensiveStructureWeak");
                ArcenPoint spawnLocation = planet.GetSafePlacementPoint_AroundEntity( Context, typedata, structureToDefend, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 200 ) );
                GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typedata, 1,
                                                                                              pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DarkZenith-AddEconDef" );
            }
            for ( int i = 0; i < numStrongDefensiveTurrets; i++ )
            {
                GameEntityTypeData typedata = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "DZDefensiveStructureStrong");
                ArcenPoint spawnLocation = planet.GetSafePlacementPoint_AroundEntity( Context, typedata, structureToDefend, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 200 ) );
                GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typedata, 1,
                                                                                              pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DarkZenith-AddEconDef" );
            }


        }
        private void InitializeResourceConversionsIfNecessary( GameEntity_Squad squad, DarkZenithPerUnitBaseInfo data, ArcenHostOnlySimContext Context )
        {
            //When an Epistyle or Terminus is created, it doesn't have ResourceConversion initialized
            if ( data.ConversionList.Count != 0 || data.ConversionBag.InternalListSize != 0 )
                return;

            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-InitializeResourceConversionsIfNecessary-trace", 10f ) : null;
            #endregion

            int debugCode = 0;
            try
            {
                tracingBuffer?.Add( "Initializing resource conversions for " + squad.ToStringWithPlanet() ).Add( "\n" );
                debugCode = 100;

                if ( squad.TypeData.GetHasTag( "DZTerminus" ) &&
                     !squad.TypeData.GetHasTag( "WarpingInDZTerminus" ) )
                {
                    data.HasList = true;
                    debugCode = 200;
                    if ( squad.TypeData.GetHasTag( "DZMetalTerminus" ) )
                    {
                        debugCode = 300;
                        data.Resource = DZResource.Metal;
                        //Metal Terminii can magically create Epistyles or Harvesters if there are no others around
                        //This allows for bootstrapping
                        DarkZenithResourceConversionTable.Instance.AddMetalHarvesterBuildablesToList( data.ConversionList, AttachedFaction );
                    }
                    else if ( squad.TypeData.GetHasTag( "DZRedTerminus" ) )
                    {
                        debugCode = 400;
                        data.Resource = DZResource.Red;
                        DarkZenithResourceConversionTable.Instance.AddConversionToListByName( data.ConversionList, "Make Izumite", AttachedFaction );
                    }
                    else if ( squad.TypeData.GetHasTag( "DZGreenTerminus" ) )
                    {
                        debugCode = 500;
                        data.Resource = DZResource.Green;
                        DarkZenithResourceConversionTable.Instance.AddConversionToListByName( data.ConversionList, "Make Thaumite", AttachedFaction );
                    }
                    else if ( squad.TypeData.GetHasTag( "DZWhiteTerminus" ) )
                    {
                        debugCode = 600;
                        data.Resource = DZResource.White;
                        DarkZenithResourceConversionTable.Instance.AddConversionToListByName( data.ConversionList, "Make Alkahest", AttachedFaction );

                        tracingBuffer?.Add( "Hopefully adding white terminus, conversion list length  " + data.ConversionList.Count );
                    }
                    else if ( squad.TypeData.GetHasTag( "DZBlackTerminus" ) )
                    {
                        debugCode = 700;
                        data.Resource = DZResource.Black;
                        DarkZenithResourceConversionTable.Instance.AddConversionToListByName( data.ConversionList, "Make Skrith", AttachedFaction );
                    }
                    else if ( squad.TypeData.GetHasTag( "DZBlueTerminus" ) )
                    {
                        debugCode = 800;
                        data.Resource = DZResource.Blue;
                        DarkZenithResourceConversionTable.Instance.AddConversionToListByName( data.ConversionList, "Make Chelonium", AttachedFaction );

                        tracingBuffer?.Add( "Hopefully adding blue terminus, conversion list length  " + data.ConversionList.Count );
                    }
                    else
                    {
                        throw new Exception( "Confusion for " + squad.ToStringWithPlanet() );
                    }
                    debugCode = 900;
                    if ( data.ConversionList.Count == 0 )
                        throw new Exception( "Failed to initialize conversion list for " + squad.ToStringWithPlanet() + " " + data.Resource );
                    for ( int i = 0; i < data.ConversionList.Count; i++ )
                    {
                        if ( data.ConversionList[i] == null )
                            throw new Exception( "Failed to initialize some elements of conversion list for " + squad.ToStringWithPlanet() + " " + data.Resource );
                    }
                }
                debugCode = 1000;
                if ( squad.TypeData.GetHasTag( "DZEpistyle" ) &&
                     !squad.TypeData.GetHasTag( "WarpingInDZEpistyle" ) )
                {
                    debugCode = 1100;
                    data.HasBag = true;
                    //We will fill this Epistyle with options. There are three categories;
                    //Upgrades, defensive units, and offensive units.
                    //Most of the time we want Offensive units
                    //60% offensive, 20% defensive, 10% upgrades
                    int percentOffense = 50;
                    int percentMisc = 35;
                    int percentPirate = 5;
                    //int percentUpgradedOnly = 10;
                    int value = Context.RandomToUse.Next( 0, 100 );

                    int numOffensive = 0;
                    int numMisc = 0;
                    int numUpgraders = 0;
                    int numPirates = 0;
                    List<SafeSquadWrapper> epistyles = this.BaseInfo.Epistyles.GetDisplayList();
                    debugCode = 1200;
                    for ( int i = 0; i < epistyles.Count; i++ )
                    {
                        debugCode = 1300;
                        GameEntity_Squad epistyle = epistyles[i].GetSquad();
                        if ( epistyle == null )
                            continue;
                        DarkZenithPerUnitBaseInfo eData = epistyle.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                        if ( eData.CanBuildOffensiveUnits )
                            numOffensive++;
                        if ( eData.CanBuildInfrastructure )
                            numMisc++;
                        if ( eData.CanBuildUpgrades )
                            numUpgraders++;
                        if ( eData.IsPirateEpistyle )
                            numPirates++;
                    }
                    debugCode = 1400;
                    //First some overrides for bootstrapping
                    if ( numMisc == 0 && (BaseInfo.PlayerAllied || BaseInfo.JoinAlliedFactions) )
                    {
                        debugCode = 1500;
                        //the "regular" numMisc is handled below; this is for non-invading DZs that need to get their economy going early
                        data.CanBuildInfrastructure = true;
                        DarkZenithResourceConversionTable.Instance.AddBaseBuildablesToList( data.ConversionList, AttachedFaction );

                        DarkZenithResourceConversionTable.Instance.AddAllEconomicConversionsToBag( data.ConversionBag, AttachedFaction );
                        DarkZenithResourceConversionTable.Instance.AddAllDefensiveConversionsToBag( data.ConversionBag, AttachedFaction );
                    }
                    else if ( numOffensive == 0 )
                    {
                        debugCode = 1600;
                        data.CanBuildOffensiveUnits = true;
                        data.CanBuildUtility = true;
                        //DarkZenithResourceConversionTable.Instance.AddAllOffensiveConversionsToBag( data.ConversionBag, data.IsPirateEpistyle, 3, faction );
                        DarkZenithResourceConversionTable.Instance.AddUnlockedOffensiveConversionsToBag( data.ConversionBag, BaseInfo.CompletedUpgrades, data.IsPirateEpistyle, 3, AttachedFaction );
                        DarkZenithResourceConversionTable.Instance.AddAllUtilityConversionsToBag( data.ConversionBag, 1, AttachedFaction );
                    }
                    else if ( numMisc == 0 )
                    {
                        debugCode = 1700;
                        data.CanBuildInfrastructure = true;
                        data.CanBuildUtility = true;
                        DarkZenithResourceConversionTable.Instance.AddBaseBuildablesToList( data.ConversionList, AttachedFaction );

                        DarkZenithResourceConversionTable.Instance.AddAllEconomicConversionsToBag( data.ConversionBag, AttachedFaction );
                        DarkZenithResourceConversionTable.Instance.AddAllDefensiveConversionsToBag( data.ConversionBag, AttachedFaction );
                        DarkZenithResourceConversionTable.Instance.AddAllUtilityConversionsToBag( data.ConversionBag, 3, AttachedFaction ); //also build lots of utilitiy; there's a cap on how many you can have, so prioritizing them isn't terrible
                    }
                    else if ( numUpgraders == 0 )
                    {
                        debugCode = 1800;
                        data.CanBuildUpgrades = true;
                        data.CanBuildUtility = true;
                        DarkZenithResourceConversionTable.Instance.AddAllUpgradeConversionsToBag( data.ConversionBag, AttachedFaction );
                        DarkZenithResourceConversionTable.Instance.AddAllUtilityConversionsToBag( data.ConversionBag, 3, AttachedFaction );
                    }
                    else if ( numPirates == 0 && BaseInfo.Epistyles.Count >= 8 && HasUnlockedTierTwoUnits() )
                    {
                        debugCode = 1900;
                        data.IsPirateEpistyle = true;
                        data.CanBuildOffensiveUnits = true;
                        DarkZenithResourceConversionTable.Instance.AddUnlockedOffensiveConversionsToBag( data.ConversionBag, BaseInfo.CompletedUpgrades, data.IsPirateEpistyle, 1, AttachedFaction );
                    }
                    else
                    {
                        debugCode = 2000;
                        //we have at least one of each type of epistyle that matters
                        //Lets first get a second offensive epistyle
                        if ( numOffensive == 1 )
                            percentOffense = 100;

                        if ( value < percentPirate && HasUnlockedTierTwoUnits() && BaseInfo.Epistyles.Count > 10 )
                            data.IsPirateEpistyle = true;

                        if ( value < (percentOffense + percentPirate) || data.IsPirateEpistyle )
                        {
                            //pirate and offensive epistyles can build offensive units; the IsPirateEpistyle modifier
                            //changes which resources
                            data.CanBuildOffensiveUnits = true;
                            if ( !data.IsPirateEpistyle )
                            {
                                data.CanBuildUtility = true;
                                DarkZenithResourceConversionTable.Instance.AddAllUtilityConversionsToBag( data.ConversionBag, 3, AttachedFaction );
                            }

                            DarkZenithResourceConversionTable.Instance.AddUnlockedOffensiveConversionsToBag( data.ConversionBag, BaseInfo.CompletedUpgrades, data.IsPirateEpistyle, 1, AttachedFaction );
                        }
                        else if ( value < percentPirate + percentOffense + percentMisc )
                        {
                            data.CanBuildInfrastructure = true;
                            data.CanBuildUtility = true;
                            DarkZenithResourceConversionTable.Instance.AddBaseBuildablesToList( data.ConversionList, AttachedFaction );
                            DarkZenithResourceConversionTable.Instance.AddAllDefensiveConversionsToBag( data.ConversionBag, AttachedFaction );
                            DarkZenithResourceConversionTable.Instance.AddAllUtilityConversionsToBag( data.ConversionBag, 2, AttachedFaction );
                            DarkZenithResourceConversionTable.Instance.AddAllEconomicConversionsToBag( data.ConversionBag, AttachedFaction );
                        }
                        else
                        {
                            data.CanBuildUpgrades = true;
                            data.CanBuildUtility = true;
                            DarkZenithResourceConversionTable.Instance.AddAllUpgradeConversionsToBag( data.ConversionBag, AttachedFaction );
                            DarkZenithResourceConversionTable.Instance.AddAllUtilityConversionsToBag( data.ConversionBag, 3, AttachedFaction ); //there are a lot fewer utility conversions, and we'd like to prioritize them. Upgrades should be fewer
                        }
                    }
                    debugCode = 2100;
                }
                if ( data.ConversionBag.InternalListSize == 0 && data.ConversionList.Count == 0 )
                    throw new Exception( "Failed to initialize conversions for " + squad.ToStringWithPlanet() + ", we got no conversions" );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in InitializeResourceConversionsIfNecessary debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }

            #region Tracing
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
            #endregion
        }
        public void HandleUpgrades( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-HandleUpgrades-trace", 10f ) : null;
            bool incrementalMode = false;
            if ( AttachedFaction.SpecialFactionData.FullInvasionMode )
            {
                if ( World_AIW2.Instance.GameSecond < AttachedFaction.InvasionTime )
                {
                    #region Tracing
                    FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
                    #endregion

                    return; //In full invasion mode, we delay granting the Upgrades until the invasion time
                }

                //Incremental mode is only for full invasion
                ConfigurationForFaction cfg = this.AttachedFaction.Config;
                incrementalMode = cfg.GetBoolValueForCustomFieldOrDefaultValue( "IncrementalUpgrade", false );
            }
            //handle starting upgrades which DZ should begin with
            if ( BaseInfo.CompletedUpgrades.Count == 0 )
            {
                //All DZs start with "Unlock Tier Zero"
                //DZs doing the full invasion start with all the ship tiers and variants unlocked (or some subset based on the gametime)
                //DZs that join their allies do not start with any additional upgrades

                int intensity = BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
                tracingBuffer?.Add( "Applying starting upgrades" );
                for ( int i = 0; i < DarkZenithUpgradeTable.Instance.Rows.Count; i++ )
                {
                    DZUpgrade upgrade = DarkZenithUpgradeTable.Instance.Rows[i];
                    if ( upgrade.AlwaysUnlocked )
                    {
                        tracingBuffer?.Add( "Applying always unlocked upgrade ").Add( upgrade.InternalName );
                        BaseInfo.ApplyThisUpgrade(upgrade);
                        BaseInfo.CompletedUpgrades.Add(upgrade);
                    }

                    if ( intensity < upgrade.RequiredIntensity &&
                         !IsHumanSidekick ) //players get all the goodies, its more fun that way
                        continue;
                    if  ( AttachedFaction.SpecialFactionData.FullInvasionMode )
                    {
                        if ( incrementalMode )
                        {
                            //The starting upgrades are based on the GameTime
                            if ( upgrade.UnlocksIfInvasionAfter != -1 &&
                                 upgrade.UnlocksIfInvasionAfter < World_AIW2.Instance.GameSecond )
                            {
                                tracingBuffer?.Add( "Applying upgrade do to full invasion (incremental mode) ").Add( upgrade.InternalName );
                                BaseInfo.ApplyThisUpgrade(upgrade);
                                BaseInfo.CompletedUpgrades.Add(upgrade);
                            }
                        }
                        else
                        {
                            //This is the original mode, they just get everything
                            if (upgrade.UnlockShipVariant || upgrade.UnlockShipTier)
                            {
                                tracingBuffer?.Add( "Applying upgrade do to full invasion (regular mode) ").Add( upgrade.InternalName );

                                BaseInfo.ApplyThisUpgrade(upgrade);
                                BaseInfo.CompletedUpgrades.Add(upgrade);
                            }
                        }
                    }
                }
            }

            //handle unusual upgrades; they trigger on some other factors
            for ( int i = 0; i < DarkZenithUpgradeTable.Instance.UnusualUpgrades.Count; i++ )
            {
                DZUpgrade upgrade = DarkZenithUpgradeTable.Instance.UnusualUpgrades[i];

                if ( upgrade.SingleUpgradeOnly && (BaseInfo.HasUpgradeBeenDone( upgrade ) || BaseInfo.IsAnotherEpistyleUpgradingForMe( upgrade ) ) )
                {
                    continue;
                }

                if ( upgrade.EnemyPowerLevelToTriggerOn > FInt.Zero )
                {
                    FInt enemyPowerLevel = FactionUtilityMethods.Instance.GetOverallPowerLevelOfEnemies( AttachedFaction );
                    if ( enemyPowerLevel > upgrade.EnemyPowerLevelToTriggerOn )
                    {
                        tracingBuffer?.Add( "Applying triggered upgrade " ).Add( upgrade.InternalName );

                        BaseInfo.ApplyThisUpgrade(upgrade);
                        BaseInfo.CompletedUpgrades.Add( upgrade );
                    }
                }
            }
            
            #region Tracing
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
            #endregion
        }

        public override void MinorFactionAIPEquivalentIncrease( FInt AIPEquivalent )
        {
            //TODO? Or could just rely on OverallPowerLevel
        }

        
        //Long Range Planning (LRP) starts here
        public readonly List<SafeSquadWrapper> LRPHarvesters = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DarkZenithFactionDeepInfoRoot-LRPHarvesters" );
        public readonly List<SafeSquadWrapper> LRPConstructors = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DarkZenithFactionDeepInfoRoot-LRPConstructors" );
        public readonly List<SafeSquadWrapper> LRPTransports = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DarkZenithFactionDeepInfoRoot-LRPTransports" );
        public readonly List<SafeSquadWrapper> LRPPrivateers = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DarkZenithFactionDeepInfoRoot-LRPPrivateers" );
        public readonly List<SafeSquadWrapper> LRPTerminii = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DarkZenithFactionDeepInfoRoot-LRPTerminii" );
        public readonly List<SafeSquadWrapper> LRPEpistyles = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DarkZenithFactionDeepInfoRoot-LRPEpistyles" );
        public readonly List<SafeSquadWrapper> LRPJormugandr = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DarkZenithFactionDeepInfoRoot-LRPJormugandr" );
        public readonly List<SafeSquadWrapper> UnassignedShips = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DarkZenithFactionDeepInfoRoot-UnassignedShips" );
        public readonly List<SafeSquadWrapper> ShipsToRally = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DarkZenithFactionDeepInfoRoot-ShipsToRally" );
        public readonly List<Planet> LRPPlanetsToDefend = List<Planet>.Create_WillNeverBeGCed( 500, "DarkZenithFactionDeepInfoRoot-LRPPlanetsToDefend" );
        public readonly List<Planet> PlanetsControlled_LRP = List<Planet>.Create_WillNeverBeGCed( 500, "DarkZenithFactionDeepInfoRoot-PlanetsControlled_LRP" );

        public readonly List<SafeSquadWrapper> NonAttackers = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DarkZenithFactionDeepInfoRoot-NonAttackers" );
        GameCommand AttackCommand = null;

        public readonly List<Planet> EnemyPlanetsInTerritorialSphereLRP = List<Planet>.Create_WillNeverBeGCed( 500, "DarkZenithFactionDeepInfoRoot-EnemyPlanetsInTerritorialSphereLRP" );
        public readonly List<Planet> UnownedPlanetsInTerritorialSphereLRP = List<Planet>.Create_WillNeverBeGCed( 500, "DarkZenithFactionDeepInfoRoot-UnownedPlanetsInTerritorialSphereLRP" );
        public readonly DictionaryOfLists<Planet, SafeSquadWrapper> UnassignedShipsByPlanet = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 60, "DarkZenithFactionDeepInfoRoot-UnassignedShipsByPlanet" );
        public static int numDefensiveFleets_LRP;
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            //if in war footing, use fireateams. If not, go back to a spawner with space
            if ( this.BaseInfo == null )
                return;
            #region Tracing
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            #endregion
            int debugCode = 0;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                //first iterate over and clean up stale data (ships list and strength)
                debugCode = 1000;
                UnassignedShips.Clear();
                UnassignedShipsByPlanet.Clear();
                LRPHarvesters.Clear();
                LRPConstructors.Clear();
                LRPTransports.Clear();
                LRPPrivateers.Clear();
                LRPTerminii.Clear();
                LRPEpistyles.Clear();
                LRPJormugandr.Clear();
                PlanetsControlled_LRP.Clear();
                TeamsAimedAtPlanet.Clear();
                ShipsToRally.Clear();
                numDefensiveFleets_LRP = 0;
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
                {
                    team.DeepInfo.Reset();
                    if ( team.DefenseMode && team.StepsUntilBecomesOffensive == -1 )
                        numDefensiveFleets_LRP++;
                }
                debugCode = 1010;
                //If we are not in pace, iterate over all our units and make some lists
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    debugCode = 1100;
                    if ( entity.TypeData.GetHasTag( "WarpingInDZTerminus" ) || entity.TypeData.GetHasTag( "DZTerminus" ) )
                    {
                        if ( !PlanetsControlled_LRP.Contains( entity.Planet ) )
                            PlanetsControlled_LRP.Add( entity.Planet );

                        LRPTerminii.Add( entity );
                        continue;
                    }
                    if ( entity.TypeData.GetHasTag( "WarpingInDZEpistyle" ) || entity.TypeData.GetHasTag( "DZEpistyle" ) )
                    {
                        if ( !PlanetsControlled_LRP.Contains( entity.Planet ) )
                            PlanetsControlled_LRP.Add( entity.Planet );

                        LRPEpistyles.Add( entity );
                        continue;
                    }
                    if ( entity.TypeData.GetHasTag( "DZHarvester" ) )
                    {
                        LRPHarvesters.Add( entity );
                        continue;
                    }
                    if ( entity.TypeData.GetHasTag( "DZTransport" ) )
                    {
                        LRPTransports.Add( entity );
                        continue;
                    }
                    if ( entity.TypeData.GetHasTag( "DZConstructor" ) )
                    {
                        LRPConstructors.Add( entity );
                        continue;
                    }


                    if ( entity.TypeData.IsMobileCombatant &&
                         entity.GetEffectiveOrders().Behavior != EntityBehaviorType.Attacker_Full )
                    {
                        if ( AttackCommand == null )
                        {
                            AttackCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetBehavior_FromFaction_NoPraetorianGuard], GameCommandSource.AnythingElse );
                            AttackCommand.RelatedMagnitude = (int)EntityBehaviorType.Attacker_Full;
                        }
                        AttackCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                    }
                    DarkZenithPerUnitBaseInfo data = entity.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    if ( entity.TypeData.GetHasTag( "DZJormugandr" ) )
                    {
                        LRPJormugandr.Add( entity );
                        if ( data.IsDormant ) //dormant jormugandr are not in fireteams
                            continue;
                    }

                    if ( entity.TypeData.GetHasTag( "DZPrivateer" ) )
                    {
                        LRPPrivateers.Add( entity );
                        continue;
                    }
                    if ( entity.TypeData.GetHasTag( "DZDefensiveStructure" ) || !entity.TypeData.IsMobile )
                        continue; //ignore these
                    debugCode = 1110;
                    if ( !entity.TypeData.CanGoThroughWormholes )
                        continue; //these can't go through wormholes

                    if (this.IsHumanSidekick)
                    {
                        if (AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame("PlayerControllingDZ"))
                        {
                            if (AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame("ShipsRallyToNearestFlagshipDZ") &&
                                !data.HasRalliedToFlagship)
                            {
                                Fleet fleet = entity.FleetMembership.Fleet;
                                if (fleet == null)
                                    continue;
                                GameEntity_Squad flagship = fleet.Centerpiece.GetSquad();
                                if (flagship == null)
                                    continue;
                                Planet dest  = entity.GetDestinationPlanet();
                                if (dest != flagship.Planet)
                                {
                                    ShipsToRally.Add(entity); //rally this ship to its flagship
                                }
                            }
                            continue; //player must give all orders
                        }
                    }
                    if ( entity.FireteamId < 0 )
                    {
                        UnassignedShips.Add( entity );
                        UnassignedShipsByPlanet[entity.Planet].Add( entity );
                    }
                    else
                    {
                        Fireteam team = FireteamBaseUtility.GetFireteamById( BaseInfo.Teams, entity.FireteamId );
                        if ( team == null )
                            entity.FireteamId = -1; //something happened to the fireteam, so lets find a new one next LRP stage
                        else
                            team.DeepInfo.AddUnit( entity );
                    }
                }
                debugCode = 1200;

                if ( AttackCommand != null )
                {
                    //sometimes units can wind up in "not attacker_full behaviour and I'm not sure why, so correct it
                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, AttackCommand, false );
                    AttackCommand = null;
                }
                if ( ShipsToRally.Count > 0 )
                {
                    int rallyCap = 10; //this isn't the most efficient code, so only give orders to up a cap
                    for (int i = 0; i < rallyCap; i++)
                    {
                        if (i == ShipsToRally.Count)
                        {
                            break; //everyone is rallied!
                        }
                        GameEntity_Squad entity = ShipsToRally[i].GetSquad();
                        if ( entity == null )
                            continue;
                        Fleet fleet = entity.FleetMembership.Fleet;
                        if (fleet == null)
                            continue;

                        GameEntity_Squad flagship = fleet.Centerpiece.GetSquad();
                        if (flagship == null)
                            continue;
                        GameCommand rallyCommand = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleSpecialMission], GameCommandSource.AnythingElse);

                        rallyCommand.RelatedString = "DZ_RallyToFlagship";
                        rallyCommand.RelatedEntityIDs.Add(entity.PrimaryKeyID);
                        PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache(AttachedFaction, "DarkZenithHandleRallyLRP",
                                        entity.Planet, flagship.Planet, PathingMode.Safest, Context, pathingCacheData);
                        if (pathCache != null && pathCache.PathToReadOnly.Count > 0)
                        {
                            for (int k = 0; k < pathCache.PathToReadOnly.Count; k++)
                                rallyCommand.RelatedIntegers.Add(pathCache.PathToReadOnly[k].Index);
                        }

                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, rallyCommand, false );
                    }
                }

                //Handle Harvesters
                //If we are full of metal, go to the nearest metal terminus
                //If we aren't full of metal and have no move orders, go to a random metal generator
                HandleHarvestersLRP( Context, pathingCacheData );
                //Handle Transports
                //If we have a destination, make sure we are going to that destination
                HandleTransportsLRP( Context, pathingCacheData );
                //Handle Constructors
                HandleConstructorsLRP( Context, pathingCacheData );
                //for pirates
                HandlePrivateersLRP( Context, pathingCacheData );
                //for jorumugandr
                HandleJormugandrLRP( Context, pathingCacheData );

                BaseInfo.GetEnemiesInTerritorialSphere( EnemyPlanetsInTerritorialSphereLRP, Context );
                if ( !BaseInfo.HasTakenTerritorialSphere )
                {
                    BaseInfo.UnownedPlanetsInSphere( UnownedPlanetsInTerritorialSphereLRP, Context, PlanetsControlled_LRP );
                }
                debugCode = 1300;
                //Fireteam stuff is below
                FactionUtilityMethods.Instance.FlushUnitsFromReinforcementPointsOnAllRelevantPlanets( AttachedFaction, Context, 5f );
                debugCode = 1400;
                LRPPlanetsToDefend.Clear();
                for ( int i = 0; i < LRPTerminii.Count; i++ )
                    LRPPlanetsToDefend.Add( LRPTerminii[i].Planet );
                debugCode = 1500;
                for ( int i = 0; i < LRPEpistyles.Count; i++ )
                    LRPPlanetsToDefend.Add( LRPEpistyles[i].Planet );
                debugCode = 1600;
                for ( int i = 0; i < UnownedPlanetsInTerritorialSphereLRP.Count; i++ )
                    LRPPlanetsToDefend.Add( UnownedPlanetsInTerritorialSphereLRP[i] );
                debugCode = 1700;

                FireteamUtility.UpdateFireteams( AttachedFaction, Context, pathingCacheData, BaseInfo.Teams, TeamsAimedAtPlanet, tracingBuffer, FInt.One, LRPPlanetsToDefend );
                FireteamUtility.UpdateRegiments( AttachedFaction, Context, pathingCacheData, BaseInfo.Teams, TeamsAimedAtPlanet, tracingBuffer, AttachedFaction.MinFireteamStrength, true );
                debugCode = 2000;
                for ( int i = 0; i < UnassignedShips.Count; i++ )
                    AssignUnitToFireteam( UnassignedShips[i].GetSquad(), Context, pathingCacheData );
                if ( AttachedFaction.NumFireteams != BaseInfo.Teams.GetItemCount() )
                    AttachedFaction.NumFireteams = BaseInfo.Teams.GetItemCount();
                if ( !HaveEnoughDefensiveFleets_LRPOnly( AttachedFaction ) )
                {
                    debugCode = 2200;
                    //if we need more defensive fleets and also have no enemies right now, sometimes pick a staging fireteam and disband it
                    //the goal is to get out on the offensive reasonably quickly, but not so quick that we don't consolidate our gains a bit
                    if ( BaseInfo.HasTakenTerritorialSphere && UnownedPlanetsInTerritorialSphereLRP.Count == 0 &&
                         EnemyPlanetsInTerritorialSphereLRP.Count == 0 &&
                         Context.RandomToUse.Next( 0, 100 ) < 50 )
                    {
                        foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
                        {
                            if ( !team.DefenseMode &&
                                 team.status == FireteamStatus.Staging )
                            {
                                team.Disband( this.AttachedFaction, Context );
                                break;
                            }
                        }
                    }
                }

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Dark Zenith LRP debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            finally
            {
                pathingCacheData.ReturnToPool();

                #region Tracing
                if ( tracingBuffer != null && !tracingBuffer.GetIsEmpty() )
                    tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " " + AttachedFaction.FactionIndex ).Add( " LongRangePlanning trace ends at " ).Add( World_AIW2.Instance.GameSecond ).Add( "\n" );
                FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
                #endregion
            }
        }

        /* General DZ, non-military stuff */
        public void HandleHarvestersLRP( ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-HandleHarvestersLRP-trace", 10f ) : null;

            bool harvesterLRPLogging = false;
            int debugCode = 0;
            List<SafeSquadWrapper> WorkingListForHarvesters = GameEntity_Squad.GetTemporarySquadList( "DZ-HandleHarvestersLRP-WorkingListForHarvesters", 10f );
            if ( WorkingListForHarvesters == null ) //blocked for teardown/shutdown; bail
                return;
            try
            {
                debugCode = 100;
                for ( int i = 0; i < LRPHarvesters.Count; i++ )
                {
                    debugCode = 200;
                    GameEntity_Squad harvester = LRPHarvesters[i].GetSquad();
                    if ( harvester == null )
                        continue;
                    DarkZenithPerUnitBaseInfo data = harvester.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    debugCode = 210;
                    //If we have move orders, just keep moving
                    if ( harvester.HasQueuedOrders() )
                        continue;
                    int currentValue = 0;
                    WorkingListForHarvesters.Clear();
                    data.Inventory.TryGetValue( DZResource.Metal, out currentValue );
                    debugCode = 220;
                    bool harvesterSentToOtherPlanet = false;
                    if ( currentValue >= BaseInfo.Difficulty.HarvesterCapacity )
                    {
                        debugCode = 230;
                        if ( LRPTerminii.Count == 0 )
                            continue; //no more terminii; this DZ is probly dead
                        //Find the nearest metal terminal and go there
                        GameEntity_Squad dest = null;
                        debugCode = 240;
                        for ( int j = 0; j < LRPTerminii.Count; j++ )
                        {
                            debugCode = 250;
                            GameEntity_Squad terminus = LRPTerminii[j].GetSquad();
                            if ( terminus == null )
                                continue;
                            if ( terminus.Planet != harvester.Planet )
                                continue;
                            DarkZenithPerUnitBaseInfo tData = terminus.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                            if ( tData.Resource == DZResource.Metal )
                                WorkingListForHarvesters.Add( terminus );
                        }
                        debugCode = 260;
                        if ( WorkingListForHarvesters.Count == 0 )
                        {
                            debugCode = 262;
                            //we have no metal terminii here; go to another planet!
                            ArcenArrays.Randomize( LRPTerminii, Context.RandomToUse );
                            for ( int j = 0; j < LRPTerminii.Count; j++ )
                            {
                                debugCode = 2630;
                                GameEntity_Squad terminus = LRPTerminii[j].GetSquad();
                                if ( terminus == null )
                                    continue;
                                DarkZenithPerUnitBaseInfo tData = terminus.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                                if ( tData.Resource == DZResource.Metal )
                                {
                                    debugCode = 264;
                                    PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "DarkZenithHandleHarvestersLRP", 
                                        harvester.Planet, terminus.Planet, PathingMode.Safest, Context, PathCacheData );
                                    if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                                    {
                                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleSpecialMission], GameCommandSource.AnythingElse );
                                        command.RelatedString = "DZ_HarvesterToDest";
                                        command.RelatedEntityIDs.Add( harvester.PrimaryKeyID );
                                        for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                                            command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                                        harvesterSentToOtherPlanet = true;
                                    }
                                    break;
                                }
                            }
                        }
                        else
                        {
                            debugCode = 265;
                            //cross threading errors can happen here!
                            try
                            {
                                if ( WorkingListForHarvesters.Count > 1 )
                                    dest = WorkingListForHarvesters[Context.RandomToUse.Next( 0, WorkingListForHarvesters.Count )].GetSquad();
                                else
                                    dest = WorkingListForHarvesters[0].GetSquad();
                            }
                            catch
                            {
                                try
                                {
                                    if ( WorkingListForHarvesters.Count > 1 )
                                        dest = WorkingListForHarvesters[Context.RandomToUse.Next( 0, WorkingListForHarvesters.Count )].GetSquad();
                                    else
                                        dest = WorkingListForHarvesters[0].GetSquad();
                                }
                                catch
                                {
                                    dest = null;
                                }
                            }
                        }
                        debugCode = 266;
                        if ( harvesterSentToOtherPlanet )
                        {
                            if ( tracing && harvesterLRPLogging )
                                tracingBuffer.Add( "LRP: " + harvester.ToStringWithPlanet() + " is en route to another planet, since there were no metal terminii here.\n" );
                            continue;
                        }
                        if ( tracing && harvesterLRPLogging )
                            tracingBuffer.Add( "LRP: " + harvester.ToStringWithPlanet() + " is en route to its terminus, " + dest?.ToStringWithPlanet() + ".\n" );
                        if ( dest == null )
                        {
                            continue; //this unit is probably going to die soon
                            //throw new Exception( "Could not find any metal terminii for " + harvester.ToStringWithPlanetAndOwner() );
                        }
                        debugCode = 270;
                        GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse );
                        moveCommand.PlanetOrderWasIssuedFrom = harvester.Planet.Index;
                        moveCommand.RelatedPoints.Add( dest.WorldLocation );
                        moveCommand.RelatedEntityIDs.Add( harvester.PrimaryKeyID );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );


                        continue;
                    }
                    debugCode = 300;
                    if ( tracing && harvesterLRPLogging )
                        tracingBuffer.Add( harvester.ToStringWithPlanet() + " is en route to a new metal generator\n" );

                    //We have no move orders and not enough metal, so go to a randomly chosen metal generator
                    FactionUtilityMethods.Instance.SendUnitToRandomMetalGenerator( AttachedFaction, Context, harvester, 10f );
                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception during HandleHarvestersLRP code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            finally
            {
                GameEntity_Squad.ReleaseTemporarySquadList( WorkingListForHarvesters );
                if ( tracing )
                    FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
            }
        }
        public void HandleTransportsLRP( ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            bool logTransportLRP = false;
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith ) && logTransportLRP;
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-HandleTransportsLRP-trace", 10f ) : null;

            bool TransportLRPDebug = false;
            for ( int i = 0; i < LRPTransports.Count; i++ )
            {
                GameEntity_Squad transport = LRPTransports[i].GetSquad();
                if ( transport == null )
                    continue;
                DarkZenithPerUnitBaseInfo data = transport.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( transport.HasQueuedOrders() )
                    continue;
                if ( data.Destination == null )
                    continue; //no destination (this is set in the Transport Sim code)
                if ( data.SecondaryDestination != null &&
                     !transport.IsNextOrderMovementType( EntityOrderType.Move_Normal ) )
                {
                    if ( transport.Planet != data.SecondaryDestination.Planet )
                    {
                        continue;
                    }
                    //Wipe the current orders; we are going to the secondary now
                    if ( transport.HasQueuedOrders() )
                        transport.Orders.ClearOrders( ClearBehavior.AnythingIncludingAttackerMode, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "DZChangeMind" ); //clear my previous orders if I was previously moving
                    GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse );
                    moveCommand.PlanetOrderWasIssuedFrom = transport.Planet.Index;
                    moveCommand.RelatedPoints.Add( data.SecondaryDestination.WorldLocation );
                    moveCommand.RelatedEntityIDs.Add( transport.PrimaryKeyID );
                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );
                }

                //Dispatch toward the Destination. If it's on another planet, this queues the wormhole hop(s)
                //and the local move to the Destination's exact location together, so the transport doesn't
                //sit idle for a cycle once it lands (see AutoDefendUtility.GoToPlanetThenLocation).
                if ( tracing && TransportLRPDebug )
                    tracingBuffer.Add( "We have ordered " + transport.ToStringWithPlanet() + " to find a path to " + data.Destination.ToStringWithPlanetAndOwner() + "\n" );
                AutoDefendUtility.GoToPlanetThenLocation( transport, data.Destination.Planet, data.Destination.WorldLocation, Context, PathCacheData, PathingMode.Safest );
            }

            if ( tracing )
                FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
        }

        public void HandleConstructorsLRP( ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-HandleConstructorsLRP-trace", 10f ) : null;
            int debugCode = 0;
            try
            {
                debugCode = 100;
                for ( int i = 0; i < LRPConstructors.Count; i++ )
                {
                    debugCode = 200;
                    GameEntity_Squad constructor = LRPConstructors[i].GetSquad();
                    if ( constructor == null )
                        continue;
                    DarkZenithPerUnitBaseInfo data = constructor.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    Planet destinationPlanet = World_AIW2.Instance.GetPlanetByIndex( data.DZConstructorTargetPlanetIndex );
                    if ( destinationPlanet == null )
                    {
                        //ArcenDebugging.ArcenDebugLogSingleLine( "HandleConstructorsLRP: " + constructor.ToStringWithPlanet() + " has no destination ", Verbosity.ShowAsError );
                        continue;
                    }
                    if ( constructor.HasQueuedOrders() )
                        continue;
                    if ( tracing )
                        tracingBuffer.Add( "LRP: Handling constructor " ).Add( constructor.ToStringWithPlanet() ).Add( "\n" );
                    debugCode = 300;
                    if ( constructor.Planet != destinationPlanet )
                    {
                        debugCode = 310;
                        if ( tracing )
                            tracingBuffer.Add( "\t Constructor is being sent to planet  " ).Add( destinationPlanet.Name );
                        PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "DarkZenithHandleConstructorsLRP", 
                            constructor.Planet, destinationPlanet, PathingMode.Safest, Context, PathCacheData );
                        if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                        {
                            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleSpecialMission], GameCommandSource.AnythingElse );
                            debugCode = 320;
                            command.RelatedString = "DZ_CnstrctrToDest";
                            command.RelatedEntityIDs.Add( constructor.PrimaryKeyID );
                            for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                                command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                        }
                        continue;
                    }
                    debugCode = 400;
                    if ( tracing )
                        tracingBuffer.Add( "\t Constructor is being moved to  " ).Add( data.DestinationPoint );
                    //We are on the right planet, so go to the point
                    GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse );
                    moveCommand.PlanetOrderWasIssuedFrom = constructor.Planet.Index;
                    debugCode = 410;
                    moveCommand.RelatedPoints.Add( data.DestinationPoint );
                    debugCode = 420;
                    moveCommand.RelatedEntityIDs.Add( constructor.PrimaryKeyID );
                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception during HandleConstructorsLRP code " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }

            if ( tracing )
                FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
        }
        public void HandlePrivateersLRP( ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-HandlePrivateersLRP-trace", 10f ) : null;
            int debugCode = 0;
            try
            {
                debugCode = 100;
                for ( int i = 0; i < LRPPrivateers.Count; i++ )
                {
                    debugCode = 200;
                    GameEntity_Squad privateer = LRPPrivateers[i].GetSquad();
                    if ( privateer == null )
                        continue;
                    DarkZenithPerUnitBaseInfo pData = privateer.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );

                    //If we have a target, fly towards it. The target is either a Transport (to steal from)
                    //or our home epistyle

                    GameEntity_Squad destination = pData.Destination;
                    if ( destination == null )
                        continue; //no destination, do nothing
                    debugCode = 300;
                    if ( privateer.Planet != destination.Planet )
                    {
                        debugCode = 400;
                        if ( tracing )
                            tracingBuffer.Add( "\t" ).Add( privateer.ToStringWithPlanet() + " sent to  " ).Add( destination.ToStringWithPlanet() ).Add( " on another planet" );
                        PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "DarkZenithHandlePrivateersLRP", 
                            privateer.Planet, destination.Planet, PathingMode.Safest, Context, PathCacheData );
                        if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                        {
                            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleSpecialMission], GameCommandSource.AnythingElse );
                            debugCode = 320;
                            command.RelatedString = "DZ_PrivateerToDest";
                            command.RelatedEntityIDs.Add( privateer.PrimaryKeyID );
                            for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                                command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                        }
                        continue;
                    }
                    debugCode = 400;
                    if ( tracing )
                        tracingBuffer.Add( "\t" ).Add( privateer.ToString() ).Add( " is being moved toward  " ).Add( destination.ToString() );
                    //We are on the right planet, so go to the point
                    GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse );
                    moveCommand.PlanetOrderWasIssuedFrom = privateer.Planet.Index;
                    debugCode = 410;
                    moveCommand.RelatedPoints.Add( destination.WorldLocation );
                    debugCode = 420;
                    moveCommand.RelatedEntityIDs.Add( privateer.PrimaryKeyID );
                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in HandlePrivateersLRP code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }

            if ( tracing )
                FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
        }
        public void HandleJormugandrLRP( ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            //this moves Jormugandr around in Dormant mode.
            //Active Jormugandr use fireteam code
            if ( BaseInfo.OriginalPlanets.Count == 0 )
                return; //some sort of race condition
            if ( this.IsHumanSidekick && AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "PlayerControllingDZ"  ))
                return; //no restrictions on Jormugandr
            for ( int i = 0; i < this.LRPJormugandr.Count; i++ )
            {
                GameEntity_Squad entity = this.LRPJormugandr[i].GetSquad();
                if ( entity == null )
                    continue;
                DarkZenithPerUnitBaseInfo data = entity.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( entity.HasQueuedOrders() )
                    continue; //let the entity finish what it's doing
                if ( !data.IsDormant )
                    continue; //we are in combat mode, so none of that dormancy moving stuff. We are doing fireteam things
                bool onHomeworld = false;
                if ( BaseInfo.OriginalPlanets.Contains( entity.Planet ) )
                    onHomeworld = true;
                if ( !onHomeworld || data.SecondsUntilDormancyMove < 0 )
                {
                    //Note we can't update data without desyncs, so we might actually move a couple times
                    //(or not at all) depending on how we race with the Sim thread. All of that is fine and will just
                    //make us look a bit less predictable

                    //move jormugandr to a random home planet
                    Planet dest = BaseInfo.OriginalPlanets[Context.RandomToUse.Next( 0, BaseInfo.OriginalPlanets.Count )];
                    PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "DarkZenithHandleJormugandrLRP", entity.Planet, dest, PathingMode.Safest, Context, PathCacheData );
                    if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                    {
                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleSpecialMission], GameCommandSource.AnythingElse );
                        command.RelatedString = "DZ_JrmgdrToDest";
                        command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                        for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                            command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                    }
                }
                //move around the planet a bit
                else if ( Context.RandomToUse.Next( 0, 100 ) < 10 )
                    FactionUtilityMethods.Instance.SendUnitToRandomWormhole( AttachedFaction, Context, entity, 10f );
                else if ( Context.RandomToUse.Next( 0, 100 ) < 30 )
                    FactionUtilityMethods.Instance.SendUnitToRandomMetalGenerator( AttachedFaction, Context, entity, 10f );

            }
        }
        /* Fireteam code below */

        //prefer close ones that you can get to safely
        private ArcenLessLinkedList<Fireteam> AvailableFireteams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "DarkZenithFactionDeepInfoRoot-AvailableFireteams" ); 
        private void AssignUnitToFireteam( GameEntity_Squad entity, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( entity == null )
                return;

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam ) && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-AssignUnitToFireteam-trace", 10f ) : null;

            AvailableFireteams.Clear();
            //bool debug = false;

            if ( this.BaseInfo.Teams.GetItemCount() == 0 )
            {
                Fireteam team = Fireteam.CreateNewWithIDFromList( BaseInfo.Teams );
                team.DefenseMode = true;
                team.StepsUntilBecomesOffensive = 10;
                team.StrengthToBringOnline = AttachedFaction.MinFireteamStrength + Context.RandomToUse.Next( 0, AttachedFaction.MaxFireteamStrength - AttachedFaction.MinFireteamStrength );
                this.BaseInfo.Teams.AddIfNotAlreadyIn( team );
            }
            int maxHops = 10;
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
            {
                if ( team.status == FireteamStatus.Disbanded ||
                        team.status == FireteamStatus.ReadyToAttack ||
                        team.status == FireteamStatus.Attacking )
                    continue; //if a fireteam is ready to fight, don't send more ships to that team
                if ( team.status == FireteamStatus.Staging && //if a fireteam is already "pretty strong", don't give it more
                     team.DeepInfo.TeamStrength > AttachedFaction.MaxFireteamStrength )
                    continue;
                if ( team.DeepInfo.TeamStrength > AttachedFaction.MaxFireteamStrength * 2 && team.DeepInfo.ShipsInFireteam.Count > 10 ) //if this is much stronger than usual, don't make it even stronger. Note the much lower number of required ships, since the DZ can have some massive golems
                    continue;
                if ( (team.DefenseMode && team.StepsUntilBecomesOffensive == -1) && (entity.TypeData.GetHasTag( "DZTierThree" ) ||
                                                                                       entity.TypeData.GetHasTag( " DZJormugandr " )) )
                    continue; //no wasting  golems on defense
                if ( team.DeepInfo.CurrentPlanet == null )
                {
                    //this is a brand new fireteam, so it's totally safe.
                    AvailableFireteams.AddIfNotAlreadyIn( team );
                    continue;
                }
                if ( AvailableFireteams.GetItemCount() >= 4 ) //if we already have a lot of possible fireteams to use, don't keep looking
                    break;

                Int16 hops = 0;
                int dangerOfTeam = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, entity.Planet, team.DeepInfo.CurrentPlanet, true, out hops );

                if ( dangerOfTeam < 40000 ) //let units wander through pretty dangerous spots (40 strength)
                {
                    if ( hops < maxHops )
                        AvailableFireteams.AddIfNotAlreadyIn( team );
                }
            }
            //Base algorithm: if any of our "safe" fireteams are "below strength" then just add to one of those fireteams.
            //If all our fireteams are "Strong Enough" then randomly choose to reinforce an existing one or create a new one
            //We require "safe teams" for the case where it's an octopus map with a Spawner cut off from the rest of the galaxy

            bool stopProcesssing = false;
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( AvailableFireteams ) )
            {
                if ( team.DeepInfo.TeamStrength < team.StrengthToBringOnline )
                {
                    team.DeepInfo.AddUnit( entity );
                    team.DeepInfo.IdentifyCurrentPlanet(); //just in case this unit is the first unit or something
                    entity.FireteamId = team.FireTeamID;
                    if ( tracing )
                        tracingBuffer.Add( "Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path A\n" );
                    stopProcesssing = true;
                    break;
                }
            }
            if ( stopProcesssing )
                return;

            int percentNewTeam = 40;
            if ( AvailableFireteams.GetItemCount() >= 3 )
                percentNewTeam = 0;
            if ( Context.RandomToUse.Next( 0, 100 ) < percentNewTeam || AvailableFireteams.GetItemCount() == 0 )
            {
                Fireteam team = Fireteam.CreateNewWithIDFromList( BaseInfo.Teams );
                team.DefenseMode = true; //all fireteams start in defense mode, then usually they will go attack more
                if ( entity.TypeData.GetHasTag( "DZJormugandr" ) )
                    team.DefenseMode = false;
                else if ( !HaveEnoughDefensiveFleets_LRPOnly(AttachedFaction) && //if we don't have enough defense fleets
                          EnemyPlanetsInTerritorialSphereLRP.Count == 0 && //and we don't have immediate urgent targets to conquer
                          !entity.TypeData.GetHasTag( "DZTierThree" ) ) //no wasting golems on defense
                {
                    team.StepsUntilBecomesOffensive = -1; //we will stay defense
                    numDefensiveFleets_LRP++;
                }
                else
                    team.StepsUntilBecomesOffensive = 4;
                team.StrengthToBringOnline = AttachedFaction.MinFireteamStrength + Context.RandomToUse.Next( 0, AttachedFaction.MaxFireteamStrength - AttachedFaction.MinFireteamStrength );
                int numShipsForConcentratingEfforts = 5; //before we're too strong, best to concentrate our forces
                if ( BaseInfo.Teams.GetItemCount() < numShipsForConcentratingEfforts )
                    team.PercentBestTarget = 100;
                else if ( this.AttachedFaction.SpecialFactionData.FireteamPercentBestTarget > 0 )
                    team.PercentBestTarget = this.AttachedFaction.SpecialFactionData.FireteamPercentBestTarget;
                else
                    team.PercentBestTarget = 65;
                team.PercentDistanceBestTarget = 45; //marauders often get far-flung empires
                team.PreferredMaxDistance = 5;
                team.DeepInfo.AddUnit( entity );
                entity.FireteamId = team.FireTeamID;

                team.DeepInfo.IdentifyCurrentPlanet(); //in case this unit is the first unit
                BaseInfo.Teams.AddIfNotAlreadyIn( team );
                if ( tracing )
                    tracingBuffer.Add( "Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path B: new fireteam. Percent New Fireateam: " + percentNewTeam + "\n" );
                return;
            }

            {
                Fireteam team = AvailableFireteams.GetRandom( Context.RandomToUse );
                team.DeepInfo.AddUnit( entity );
                entity.FireteamId = team.FireTeamID;
                if ( tracing )
                    tracingBuffer.Add( "Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path C\n" );
                team.DeepInfo.IdentifyCurrentPlanet(); //just in case this unit is the first unit or something
            }

            if ( tracing )
                FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
        }
        private bool HaveEnoughDefensiveFleets_LRPOnly(Faction faction)
        {
            if ( faction.IsVassal && FactionUtilityMethods.Instance.GetActiveVassalMissionCount(faction, VassalMissionType.Combat) > 0 )
         { 
              //if we have no assigned orders as a faction then we allocate our fireteams as "normal"
              //If we have any orders then we only allocate as much defense as the player requests
              if ( FactionUtilityMethods.Instance.GetActiveVassalMissionCount( faction, VassalMissionType.Defense ) > numDefensiveFleets_LRP )
              {
                  //if we are a vassal and we've requested more defense, do it
                  return true;
              }
              else
                  return false;
          }
            if ( faction.SpecialFactionData.FullInvasionMode &&
                 numDefensiveFleets_LRP < PlanetsControlled_LRP.Count )
                return false;
            if ( !faction.SpecialFactionData.FullInvasionMode &&
                 numDefensiveFleets_LRP < PlanetsControlled_LRP.Count / 3  )
                return false; //player allied DZ spend fewer resources on defense
            return true;
        }
        private readonly List<SafeSquadWrapper> WorkingRetreatList = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DarkZenithFactionDeepInfoRoot-WorkingRetreatList" );
        public override GameEntity_Squad GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            //int currentDanger = -1;
            WorkingRetreatList.Clear();
            int debugCode = 0;
            GameEntity_Squad safestRetreatPoint = null;
            try
            {
                debugCode = 100;
                WorkingRetreatList.AddRange( LRPTerminii );
                debugCode = 200;
                WorkingRetreatList.AddRange( LRPEpistyles );
                debugCode = 300;
                int dangerOfSafest = 99999999;
                for ( int i = 0; i < WorkingRetreatList.Count; i++ )
                {
                    debugCode = 400;
                    Int16 hops = 0;
                    int danger = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForFireteam, WorkingRetreatList[i].Planet, true, out hops );
                    if ( safestRetreatPoint == null || dangerOfSafest > danger )
                    {
                        safestRetreatPoint = WorkingRetreatList[i].GetSquad();
                        dangerOfSafest = danger;
                    }
                    if ( danger == 0 )
                        break;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            return safestRetreatPoint;
        }

        public override void GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass( bool DefenseMode, Planet CurrentPlanetForFireteam, 
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, List<FireteamTarget> PreferredTargets, List<FireteamTarget> FallbackTargets, object TeamObj )
        {
            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam ) && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            #endregion

            Fireteam team = (Fireteam)TeamObj;

            if ( team.DefenseMode )
                AlliedPlanetsUnderAttack( PreferredTargets, Context );
            else
            {
                GetPreferredDarkZenithTargets( PreferredTargets, Context );
                GetFallbackDarkZenithTargets( FallbackTargets, Context );
            }
            //We do a two-stage check here. First we cull the targets whose defenses are much stronger than usual.
            //then we sort targets by how hard it is to get there


            bool debug = true;
            if ( debug && tracing )
            {
                tracingBuffer.Add( "Getting lurk/target Preferred Targets\n" );
                for ( int i = 0; i < PreferredTargets.Count; i++ )
                    tracingBuffer.Add( "\t" ).Add( PreferredTargets[i].GetPlanetName_Safe() ).Add( " difficulty " ).Add( PreferredTargets[i].dangerOfPath ).Add( " \n" );
                tracingBuffer.Add( "Getting lurk/target Fallback Targets\n" );
                for ( int i = 0; i < FallbackTargets.Count; i++ )
                    tracingBuffer.Add( "\t" ).Add( FallbackTargets[i].GetPlanetName_Safe() ).Add( "\n" );
            }

            if ( tracing )
                FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
        }

        public void GetPreferredDarkZenithTargets( List<FireteamTarget> ListToFill, ArcenLongTermIntermittentPlanningContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam ) && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-GetPreferredDarkZenithTargets-trace", 10f ) : null;
            if ( tracing )
                tracingBuffer.Add( "\tFinding some preferred targets. Has Taken Sphere: " + BaseInfo.HasTakenTerritorialSphere + " UnownedPlanetsInTerritorialSphereLRP " + UnownedPlanetsInTerritorialSphereLRP.Count + " enemy planets " + EnemyPlanetsInTerritorialSphereLRP.Count + "\n" );

            ListToFill.Clear();
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null )
                    continue;
                if ( !otherFaction.GetIsHostileTowards( AttachedFaction ) )
                    continue;

                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.GrantsMinorFactionPlanetControl ) )
                {
                     if ( entity.Planet.IsZenithArchitraveTerritory )
                         continue; //scourge don't generally like to tackle the ZA

                     if ( EnemyPlanetsInTerritorialSphereLRP.Count > 0 && !EnemyPlanetsInTerritorialSphereLRP.Contains( entity.Planet ) )
                         continue; //too far from our territory while we have elements of the territory not controlled yet

                    if ( !BaseInfo.HasTakenTerritorialSphere && UnownedPlanetsInTerritorialSphereLRP.Count > 0
                          && EnemyPlanetsInTerritorialSphereLRP.Count == 0 )
                     {
                         continue; //no attacking further away enemy planets if we still need to consolidate
                    }
                     if ( !HaveEnoughDefensiveFleets_LRPOnly(AttachedFaction) && EnemyPlanetsInTerritorialSphereLRP.Count == 0 )
                         continue; //no attacking if we still need to build a defensive reserve
                     if ( entity.Planet.IsZenithArchitraveTerritory )
                         continue; //don't go into ZA territory; its a deathtrap
                    //can go after minor factions
                    if ( Fireteam.IsThisAWinningBattle( AttachedFaction, Context, entity.Planet, 2 ) )
                         continue;  //if we are already attacking and comfortably winning, don't bother sending more units
                    if ( tracing )
                         tracingBuffer.Add( "\tAdding " + entity.ToStringWithPlanet() + ", minor faction control path\n" );
                     ListToFill.Add( new FireteamTarget( entity ) );
                 }
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.CommandStation ) )
                {
                     if ( EnemyPlanetsInTerritorialSphereLRP.Count > 0 && !EnemyPlanetsInTerritorialSphereLRP.Contains( entity.Planet ) )
                     {
                         continue;  //too far from our territory while we have elements of the territory not controlled yet
                    }

                     if ( !BaseInfo.HasTakenTerritorialSphere && UnownedPlanetsInTerritorialSphereLRP.Count > 0
                          && EnemyPlanetsInTerritorialSphereLRP.Count == 0 )
                     {
                         continue; //no attacking further away enemy planets if we still need to consolidate
                    }
                     if ( !HaveEnoughDefensiveFleets_LRPOnly(AttachedFaction) && EnemyPlanetsInTerritorialSphereLRP.Count == 0 )
                     {
                         continue; //no attacking if we still need to build a defensive reserve
                    }
                     if ( Fireteam.IsThisAWinningBattle( AttachedFaction, Context, entity.Planet, 2 ) )
                     {
                         continue; //if we are already attacking and comfortably winning, don't bother sending more units
                    }

                     if ( entity.SecondsSpentAsRemains > 0 )
                     {
                         continue;
                     }
                     if ( tracing )
                         tracingBuffer.Add( "\tAdding " + entity.ToStringWithPlanet() + ", command station path\n" );

                     ListToFill.Add( new FireteamTarget( entity.Planet ) );
                 }
            }
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( Fireteam.IsThisAWinningBattle( AttachedFaction, Context, planet, 2 ) )
                    continue;
                if ( !HaveEnoughDefensiveFleets_LRPOnly(AttachedFaction) && EnemyPlanetsInTerritorialSphereLRP.Count == 0 )
                         break; //no attacking if we still need to build a defensive reserve

                if ( UnownedPlanetsInTerritorialSphereLRP.Contains( planet ) )
                {
                    if ( tracing )
                        tracingBuffer.Add( "\tAdding " + planet.Name + ", unowned\n" );

                    ListToFill.Add( new FireteamTarget( planet ) );
                }
                if ( planet.GetControllingOrInfluencingFaction() == AttachedFaction ||
                     UnownedPlanetsInTerritorialSphereLRP.Contains( planet ) )
                {
                    //are there enemies attacking us?
                    EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( AttachedFaction );
                    int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                    int myStrength = myFactionData[FactionStance.Self].TotalStrength + myFactionData[FactionStance.Friendly].TotalStrength;
                    if ( hostileStrength > myStrength / 2 )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\tAdding " + planet.Name + ", Defend our planet\n" );
                        ListToFill.Add( new FireteamTarget( planet ) );
                    }
                }
                if ( UnownedPlanetsInTerritorialSphereLRP.Count > 0 )
                    continue; //if we have unowned planets in our sphere, don't attack neutral or enemy faction owned planets
                if ( planet.GetControllingFactionType() == FactionType.NaturalObject )
                {
                    if ( EnemyPlanetsInTerritorialSphereLRP.Count > 0 && !EnemyPlanetsInTerritorialSphereLRP.Contains( planet ) )
                        continue; //don't go for far-away minor factions until after we've taken our territorial sphere

                    Faction influencingFaction = World_AIW2.Instance.GetFactionByIndex( planet.PrimaryInfluencingFaction );
                    if ( influencingFaction == null || !influencingFaction.GetIsFriendlyTowards( AttachedFaction ) )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\tAdding " + planet.Name + ", this planet is influenced by an enemy faction\n" );

                        ListToFill.Add( new FireteamTarget( planet ) );
                    }
                }
            }
            if ( tracing )
                FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
        }

        public void GetFallbackDarkZenithTargets( List<FireteamTarget> ListToFill, ArcenLongTermIntermittentPlanningContext Context )
        {
            //we use "planets in our sphere without enemies but that haven't had anything built yet", to encourage units to move toward the front lines
            ListToFill.Clear();
            if ( BaseInfo.HasTakenTerritorialSphere )
                return; //no targets

            List<Planet> workingUnownedForFallbackDarkZenithTargets = Planet.GetTemporaryPlanetList( "DZ-GetFallbackDarkZenithTargets-workingUnownedForFallbackDarkZenithTargets", 10f );
            if ( workingUnownedForFallbackDarkZenithTargets == null ) //blocked for teardown/shutdown; bail
                return;

            BaseInfo.UnownedPlanetsInSphere( workingUnownedForFallbackDarkZenithTargets, Context, BaseInfo.PlanetsControlled.GetDisplayList() );
            for ( int i = 0; i < workingUnownedForFallbackDarkZenithTargets.Count; i++ )
            {
                ListToFill.Add( new FireteamTarget( workingUnownedForFallbackDarkZenithTargets[i] ) );
            }

            Planet.ReleaseTemporaryPlanetList( workingUnownedForFallbackDarkZenithTargets );
        }

        public void AlliedPlanetsUnderAttack( List<FireteamTarget> output, ArcenLongTermIntermittentPlanningContext Context )
        {
            //note that the Nanocaust should probably also support this
            for ( int i = 0; i < LRPEpistyles.Count; i++ )
            {
                GameEntity_Squad entity = LRPEpistyles[i].GetSquad();
                if ( entity == null )
                    continue;
                var pFaction = entity.Planet.GetStanceDataForFaction( AttachedFaction );
                if ( pFaction[FactionStance.Hostile].TotalStrength > 0 )
                    output.Add( new FireteamTarget( entity.Planet ) );
            }
            for ( int i = 0; i < LRPTerminii.Count; i++ )
            {
                GameEntity_Squad entity = LRPTerminii[i].GetSquad();
                if ( entity == null )
                    continue;
                var pFaction = entity.Planet.GetStanceDataForFaction( AttachedFaction );
                if ( pFaction[FactionStance.Hostile].TotalStrength > 0 )
                    output.Add( new FireteamTarget( entity.Planet ) );
            }
        }
        public override Planet GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass( Planet TargetPlanet, int TeamStrength, Planet CurrentPlanetForTeam, 
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            Planet bestPlanet = null;
            int dangerOfPathFromBestPlanet = -1;
            int distanceFromBestPlanet = 9999;
            Int16 hopsFromBestPlanet = 9999;
            int unused = 0;
            //this logic partially cribbed from IndependentAIFleet.cs::Helper_DoTargetFindingSweep

            if ( TargetPlanet == null )
                throw new Exception( "No target planet set in get lurk planet?!" );
            //int debugCode = 0;

            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam ) && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            #endregion

            bool preferUnwatchedPlanets = false;
            if ( tracing )
                tracingBuffer.Add( "Getting a lurk planet. Target planet " + TargetPlanet.Name ).Add( ". " ).Add( AttachedFaction.BaseInfo.Allegiance ).Add( "\n" );

            foreach ( Planet.PlanetAtHopDistance _phd in TargetPlanet.PlanetsWithinXHops_NoFilters( -1 ) )
            {
                Planet planet = _phd.Planet;
                Int16 Distance = _phd.Hops;
                if ( planet == TargetPlanet )
                    continue;

                int planetDefensiveStrength = Fireteam.GetPlanetDefensiveStrength( planet, AttachedFaction, true, ref unused,
                                                                          FInt.Zero, FInt.Zero );
                if ( planet.GetControllingFaction().GetIsHostileTowards( AttachedFaction ) && planetDefensiveStrength > TeamStrength / 10 )
                    continue;

                //Don't path through any particularly dangerous planets
                Int16 hops = 0;
                int totalDifficultyOfPathToLurkPlanet = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForTeam, planet, true, out hops );
                if ( totalDifficultyOfPathToLurkPlanet >= TeamStrength * 5 ) //as long as they only outnumber us 5:1, let's go!
                    continue;

                int totalDifficultyOfPathToTarget = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, planet, TargetPlanet, true, out hops );
                if ( preferUnwatchedPlanets && planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    totalDifficultyOfPathToTarget *= 2; //penalized watched planets if desired

                if ( totalDifficultyOfPathToTarget < 0 )
                    totalDifficultyOfPathToTarget = 0; //pathing through allied planets is basically the same

                if ( tracing )
                    tracingBuffer.Add( "\tConsidering " + planet.Name + " danger of path " + totalDifficultyOfPathToTarget + " distance " + Distance + " intel " + planet.IntelLevel ).Add( "\n" );

                if ( dangerOfPathFromBestPlanet > totalDifficultyOfPathToTarget ||
                     dangerOfPathFromBestPlanet == -1 )
                {
                    if ( tracing )
                        tracingBuffer.Add( "\t" + planet.Name + " is now the lurk location; path A" ).Add( "\n" );
                    dangerOfPathFromBestPlanet = totalDifficultyOfPathToTarget;
                    distanceFromBestPlanet = Distance;
                    hopsFromBestPlanet = hops;
                    bestPlanet = planet;
                }

                if ( dangerOfPathFromBestPlanet == totalDifficultyOfPathToTarget &&
                     (distanceFromBestPlanet > Distance ||
                       hopsFromBestPlanet > hops) )
                {
                    if ( tracing )
                        tracingBuffer.Add( "\t" + planet.Name + " is now the lurk location; path B" ).Add( "\n" );
                    dangerOfPathFromBestPlanet = totalDifficultyOfPathToTarget;
                    distanceFromBestPlanet = Distance;
                    hopsFromBestPlanet = hops;
                    bestPlanet = planet;
                }

                if ( hopsFromBestPlanet <= 3 && dangerOfPathFromBestPlanet <= TeamStrength / 10 )
                    break; //we found a good lurk within easy striking distance of the planet, so exit now
            }

            #region Tracing
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
            #endregion

            return bestPlanet;
        }

        //Other miscellaneous stuff

        public int NumEpistylesWithConversion( string nameForConversion )
        {
            DZResourceConversion conversion = DarkZenithResourceConversionTable.Instance.GetRowByName( nameForConversion );
            if ( conversion == null )
                throw new Exception( "Could not find " + nameForConversion + " in DZResourceConversion table" );
            return NumEpistylesWithConversion( conversion );
        }

        public int NumEpistylesWithConversion( DZResourceConversion conversion )
        {
            int output = 0;
            List<SafeSquadWrapper> epistyles = this.BaseInfo.Epistyles.GetDisplayList();
            for ( int i = 0; i < epistyles.Count; i++ )
            {
                GameEntity_Squad ship = epistyles[i].GetSquad();
                if ( ship == null )
                    continue;
                DarkZenithPerUnitBaseInfo data = ship.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( data.NextConversion == null )
                    continue;
                if ( data.NextConversion == conversion )
                    output++;
            }
            return output;
        }
        public int NumConstructorsGoingToBuild( string tag )
        {
            int output = 0;
            List<SafeSquadWrapper> constructors = this.BaseInfo.Constructors.GetDisplayList();
            for ( int i = 0; i < constructors.Count; i++ )
            {
                GameEntity_Squad ship = constructors[i].GetSquad();
                if ( ship == null )
                    continue;
                DarkZenithPerUnitBaseInfo data = ship.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( data.Unit == null )
                    continue;
                if ( data.Unit.GetHasTag( tag ) )
                    output++;
            }
            return output;
        }
        public int NumTerminiiOrConstructorsForResource( DZResource resource )
        {
            int numTerminii = 0;
            List<SafeSquadWrapper> terminii = this.BaseInfo.Terminii.GetDisplayList();
            for ( int i = 0; i < terminii.Count; i++ )
            {
                GameEntity_Squad ship = terminii[i].GetSquad();
                if ( ship == null )
                    continue;
                DarkZenithPerUnitBaseInfo data = ship.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( data.Resource == resource )
                    numTerminii++;
            }
            List<SafeSquadWrapper> constructors = this.BaseInfo.Constructors.GetDisplayList();
            for ( int i = 0; i < constructors.Count; i++ )
            {
                GameEntity_Squad ship = constructors[i].GetSquad();
                if ( ship == null )
                    continue;
                DarkZenithPerUnitBaseInfo data = ship.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( data.Unit != null &&
                     data.Unit.GetHasTag( "WarpingInDZTerminus" ) &&
                     data.Resource == resource )
                    numTerminii++;
            }
            return numTerminii;
        }
        private void HandleSpireBuffs( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host

            int debugCode = 0;
            try
            {
                debugCode = 100;
                if ( World_AIW2.Instance.GameSecond % BaseInfo.Difficulty.SpireRelatedIncomeInterval != 0 )
                    return; //not time!
                debugCode = 200;
                Faction faction = FactionUtilityMethods.Instance.GetFallenSpireFaction();
                if ( faction == null )
                    return; //no spire faction
                if ( faction.GetIsFriendlyTowards( this.AttachedFaction ) )
                    return; //if we are allied to the Spire, do nothing
                bool foundSpireCities = false;
                debugCode = 300;
                PlayerTypeData playerTypeData = faction.PlayerTypeDataOrNull_ModeratelyExpensive;
                if ( playerTypeData != null &&  playerTypeData.GetHasTag("SpireSidekick"))
                {
                    debugCode = 400;
                    SpireSidekickFactionBaseInfo sideInfo = faction.GetExternalBaseInfoAs<SpireSidekickFactionBaseInfo>();
                    if ( sideInfo == null || sideInfo.SpireCities.Count == 0 )
                        return; //no buffs till you actually have cities
                    foundSpireCities = true;
                }
                else
                {
                    debugCode = 500;
                    FallenSpireFactionBaseInfo info = faction.GetExternalBaseInfoAs<FallenSpireFactionBaseInfo>();
                    if ( info == null || info.SpireCities.Count == 0 )
                        return; //no buffs till you actually have cities
                    foundSpireCities = true;
                }
                if ( !foundSpireCities )
                    return;
                debugCode = 600;
                List<SafeSquadWrapper> epistyles = this.BaseInfo.Epistyles.GetDisplayList();
                for ( int i = 0; i < epistyles.Count; i++ )
                {
                    debugCode = 700;
                    GameEntity_Squad epistyle = epistyles[i].GetSquad();
                    if ( epistyle == null )
                        continue;
                    DarkZenithPerUnitBaseInfo data = epistyle.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    for ( int j = (int)DZResource.Metal; j < (int)DZResource.End; j++ )
                    {
                        if ( j == (int)DZResource.Metal )
                        {
                            data.AddResources( (DZResource)j, BaseInfo.Difficulty.SpireRelatedMetalIncome );
                        }
                        else
                            data.AddResources( (DZResource)j, BaseInfo.Difficulty.SpireRelatedOtherIncome );
                    }

                }
            } catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in HandleSpireBuffs debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        private void HandleInitialInvasionBuffs( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host

            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-HandleInitialInvasionBuffs-trace", 10f ) : null;
            #endregion

            int debugCode = 0;
            try
            {
                debugCode = 100;
                //If we have just started our invasion but are below our "Planets for initial conquest" count
                //then give the DZ some bonus ships and resources; this is to make sure we don't get stuck early
                if ( !AttachedFaction.SpecialFactionData.FullInvasionMode ) //only for full invasion
                    return;
                if ( BaseInfo.HasTakenTerritorialSphere )
                    return; //once we've taken the sphere the first time, we're done with the early invasion

                if ( World_AIW2.Instance.GameSecond % BaseInfo.Difficulty.InitialInvasionInterval != 0 )
                    return; //on a time interval

                if ( BaseInfo.Jormugandr.Count == 0 || BaseInfo.Epistyles.Count == 0)
                    return; //if the jormugandr are all dead (or the DZ is dead) then we are pretty much toast already

                debugCode = 200;

                BaseInfo.InInitialInvasionMode = false;
                if ( BaseInfo.EnemyPlanetsInTerritorialSphereSim == null )
                    return;

                if ( BaseInfo.EnemyPlanetsInTerritorialSphereSim.Count > 0 )
                    BaseInfo.InInitialInvasionMode = true;

                tracingBuffer?.Add("The DZ is getting some early-game invasion bonuses\n");

                debugCode = 300;
                int hours = World_AIW2.Instance.GameSecond / 3600;
                int bonusShips = BaseInfo.Difficulty.InitialInvasionFreeShipsPerHour * hours;
                List<SafeSquadWrapper> epistyles = this.BaseInfo.Epistyles.GetDisplayList();
                for ( int i = 0; i < BaseInfo.Difficulty.InitialInvasionFreeShips + bonusShips; i++ )
                {
                    debugCode = 400;
                    //create a random unit every so often
                    GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "InitialInvasionReinforcements" );
                    GameEntity_Squad epistyle = epistyles[Context.RandomToUse.Next( 0, epistyles.Count )].GetSquad();
                    if ( epistyle != null )
                        SpawnStructureOrReturnNull( epistyle.Planet, typeData, (byte)Context.RandomToUse.Next( 1, 7 ), epistyle.WorldLocation, Context );
                }
                int terminusBonusInterval = 2;
                int terminalBonusIncome = 1;
                if ( World_AIW2.Instance.GameSecond % terminusBonusInterval == 0 )
                {
                    List<SafeSquadWrapper> terminii = this.BaseInfo.Terminii.GetDisplayList();
                    for ( int i = 0; i < terminii.Count; i++ )
                    {
                        GameEntity_Squad terminus = terminii[i].GetSquad();
                        if ( terminus == null )
                            continue;
                        DarkZenithPerUnitBaseInfo data = terminus.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                        if ( data.NextConversion == null )
                            continue;
                        foreach ( KeyValuePair<DZResource, int> kv in data.NextConversion.Cost )
                        {
                            if ( kv.Value > 0 )
                                data.AddResources( kv.Key, terminalBonusIncome );
                        }
                    }
                }
                for ( int i = 0; i < epistyles.Count; i++ )
                {
                    debugCode = 500;
                    GameEntity_Squad epistyle = epistyles[i].GetSquad();
                    if ( epistyle == null )
                        continue;
                    DarkZenithPerUnitBaseInfo data = epistyle.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    //data.AddResources( DZResource.Metal, BaseInfo.Difficulty.InitialInvasionMetalIncome );
                    int bonusMetal = BaseInfo.Difficulty.InitialInvasionExtraMetalPerHour * hours;
                    int bonusOther = BaseInfo.Difficulty.InitialInvasionOtherIncomePerHour * hours;
                    for ( int j = (int)DZResource.Metal; j < (int)DZResource.End; j++ )
                    {
                        if ( j == (int)DZResource.Metal )
                            data.AddResources( (DZResource)j, BaseInfo.Difficulty.InitialInvasionMetalIncome + bonusMetal );
                        else
                            data.AddResources( (DZResource)j, BaseInfo.Difficulty.InitialInvasionOtherIncome + bonusOther );
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in HandleInitialInvasionBuffs code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            finally
            {
                FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
            }
        }
        public override void UpdatePlanetInfluence_HostOnly( ArcenHostOnlySimContext Context )
        {
            List<Planet> planetsInfluenced = Planet.GetTemporaryPlanetList( "DZ-UpdatePlanetInfluence_HostOnly-planetsInfluenced", 10f );
            if ( planetsInfluenced == null ) //blocked for teardown/shutdown; bail
                return;

            List<SafeSquadWrapper> allEconomicStructures = this.BaseInfo.AllEconomicStructures.GetDisplayList();
            for ( int j = 0; j < allEconomicStructures.Count; j++ )
            {
                GameEntity_Squad entity = allEconomicStructures[j].GetSquad();
                if ( entity == null )
                    continue;
                if ( entity.SecondsTillTransformation > 0 )
                    continue; //this is warping in

                planetsInfluenced.AddIfNotAlreadyIn( entity.Planet );
                PlanetFaction pFaction = entity.PlanetFaction;
                //Only convert command-station/warp-gate AIP into a minor-faction-AIP-equivalent (and clear it) when the
                //DZ is a hostile minor faction. When the DZ is the player empire, normal player AIP mechanics apply, so
                //leave these values alone rather than zeroing them out.
                if ( AttachedFaction.Type != FactionType.Player && pFaction.AIPLeftFromCommandStation != 0 )
                {
                    MinorFactionAIPEquivalentIncrease( (FInt)pFaction.AIPLeftFromCommandStation + pFaction.AIPLeftFromWarpGate );
                    pFaction.AIPLeftFromCommandStation = 0;
                    pFaction.AIPLeftFromWarpGate = 0;
                }
                if ( !BaseInfo.AllPlanetsEverTakenIdxs.Contains( entity.Planet.Index ) )
                    BaseInfo.AllPlanetsEverTakenIdxs.Add( entity.Planet.Index );
            }

            AttachedFaction.SetInfluenceForPlanetsToList( planetsInfluenced );
            Planet.ReleaseTemporaryPlanetList( planetsInfluenced );
        }

    }
}
