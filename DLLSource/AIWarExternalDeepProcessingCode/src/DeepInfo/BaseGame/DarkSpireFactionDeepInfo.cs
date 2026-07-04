using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{

    /* Initial thoughts. In some cases superseded by future thoughts 
Here's one thought of how I might implement a simple version of the Dark Spire.
When the Dark Spire has accumulated enough Energy, it flips a coin to see if it should do anything.
         If the coin is Heads then it says "Let's not do anything right now" and sleeps for Y seconds.
         If the coin is tails then it picks a set of Vengeance Generator planets. It then spawns a bunch of ships on those planets based on the amount of Energy.
                 Note if the available energy per planet < threshold then this wouldn't be strong enough to be interesting, so sleep for Y seconds.
Mobile dark spire ships work as follows:
First, kill everything on their current planet.
If everything is dead, pick an AI Overlord or Player King at random. Go to the next planet on the way. Kill everything on it. Repeat.

AI Overlords get a Dark Spire Repeller. If a Dark Spire ship gets to a planet with that, it dies instantly
Dark Spire Repellers can be destroyed by the player
*/

    /*  
        Spawning stuff. We track the energy available for each planet seperately.
                        Each planet has a threshold for "When you hit this threshold, either start a fight or share energy with other vengeance generators with less energy"
                  Each planet also has a conversion ratio for how efficiently it converts energy into ships. The higher the ratio the more ships
                  it generates. Every time a planet attacks, give it an improved ratio. 


*/

    /* TODO:
make a few more tuning knobs in the XML
Give all the VGs a bonus when a new one spawns
Give all the VGs a giant energy boost if you kill a Locus
*/
    public class DarkSpireFactionDeepInfo : ExternalFactionDeepInfoRoot, IExternalDeepInfo_Singleton
    {
        public DarkSpireFactionBaseInfo BaseInfo;
        public static DarkSpireFactionDeepInfo Instance = null;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<DarkSpireFactionBaseInfo>();
            Instance = this;
        }

        protected override void Cleanup()
        {
            Instance = null;
            BaseInfo = null;

            UnassignedShips.Clear();
            shipsPerPlanet.Clear();
            targetPlanets.Clear();
            KingPlanets.Clear();
            possibleLocusLocations.Clear();

            TeamsAimedAtPlanet.Clear();
            AvailableFireteams.Clear();
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 4;

        public readonly ProtectedValDictionary<Planet, FireteamRegiment> TeamsAimedAtPlanet = ProtectedValDictionary<Planet, FireteamRegiment>.Create_WillNeverBeGCed( 100, "DarkSpireFactionDeepInfo-TeamsAimedAtPlanet" );
        public readonly List<Planet> possibleLocusLocations = List<Planet>.Create_WillNeverBeGCed( 500, "DarkSpireFactionDeepInfo-possibleLocusLocations" );

        #region Unit Tags
        public readonly string UnitTag_Weak = "WeakDarkSpireSpawn";
        public readonly string UnitTag_Medium = "MediumDarkSpireSpawn";
        public readonly string UnitTag_Strong = "StrongDarkSpireSpawn";
        #endregion

        public override void ReactToHacking_AsPartOfMainSim_HostOnly( GameEntity_Squad entityBeingHacked, FInt WaveMultiplier, ArcenHostOnlySimContext Context, HackingEvent Event, Faction overrideFaction = null )
        {
            //if ( hackingTarget == null )
            //    return; //if hacking the planet
            //generate energy for this faction
            DarkSpireFactionBaseInfo dsdata = FactionUtilityMethods.Instance.GetDarkSpireFactionBaseInfo();
            DarkSpirePerPlanet perPlanet = dsdata.PerPlanet[entityBeingHacked.Planet.Index];
            perPlanet.NetEnergy += perPlanet.EnergyThresholdForAttack * WaveMultiplier;
        }
        
        public override void SeedStartingEntities_EarlyMajorFactionClaimsOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType)
        {
            Tutorial tutorialData = World_AIW2.Instance.TutorialOrNull;
            if ( tutorialData != null && tutorialData.SkipDarkSpireVengeanceGeneratorsAndWards )
                return;

            int intensity = AttachedFaction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
            int VGToSeed = intensity;
            if(intensity <= 3)
                VGToSeed = 3; //don't seed too few, that would be boring
            int livingPlanetCount = galaxy.GetCountOfNonDestroyedPlanets();
            if( livingPlanetCount > 100)
            {
                int bonusVGForSize = (livingPlanetCount - 100) / 10; //seed some extra VGs on larger galaxies
                VGToSeed += bonusVGForSize;
            }

            if(VGToSeed > intensity * 3) //cap the number of VGs
                VGToSeed = intensity * 3;
            
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, AttachedFaction, SpecialEntityType.None, "VengeanceGeneratorNormalSpawn", SeedingType.HardcodedCount, VGToSeed, 
                MapGenCountPerPlanet.One, MapGenSeedStyle.FullUseByFaction, 3, 1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal, null, -1 );

            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                //AI Overlords are protected by Dark Spire Wards so that the Dark Spire doesn't just win the game for you
                //If you sneak onto the Overlord's planet and blow up the Ward then the Dark Spire might just do your work for you.....
                if ( entity.GetFactionTypeSafe() == FactionType.AI )
                {
                    GameEntityTypeData entityData =  GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "DarkSpireWard");
                    entity.Planet.Mapgen_SeedEntity(Context, AttachedFaction, entityData, PlanetSeedingZone.InnerSystem);
                }
            }
            bool DSNearPlayer = false; //for testing
            if(DSNearPlayer)
            {
                Planet playerPlanet = null;
                GameEntityTypeData entityData;
                foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    //AI Overlords are protected by Dark Spire Wards so that the Dark Spire doesn't just win the game for you
                    //If you sneak onto the Overlord's planet and blow up the Ward then the Dark Spire might just do your work for you.....
                    if ( entity.GetFactionTypeSafe() == FactionType.Player )
                    {
                        entityData =  GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "DarkSpireWard");
                        entity.Planet.Mapgen_SeedEntity(Context, AttachedFaction, entityData, PlanetSeedingZone.MostAnywhere );
                        playerPlanet = entity.Planet;
                    }
                }
                Planet adjacentPlanet = null;
                foreach ( Planet neighbor in playerPlanet.LinkedNeighbors( false ) )
                {
                    adjacentPlanet = neighbor;
                }
                 entityData =  GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "VengeanceGeneratorNormalSpawn");
                adjacentPlanet.Mapgen_SeedEntity(Context, AttachedFaction, entityData, PlanetSeedingZone.MostAnywhere);
            }
        }
        public override void DoOnFirstSightingOfFactionByPlayer( bool IsFromBeacon, GameEntity_Squad SquadSeenOrNull, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return;
            if ( !IsFromBeacon )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DarkSpireFirstDetected", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        }

        public static readonly List<SafeSquadWrapper> UnassignedShips = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DarkSpireFactionDeepInfo-UnassignedShips" );
        public readonly DictionaryOfLists<Planet, SafeSquadWrapper> shipsPerPlanet = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 300, 500, "DarkSpireFactionDeepInfo-shipsPerPlanet" );
        public readonly List<Planet> targetPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "DarkSpireFactionDeepInfo-targetPlanets" );
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            if(BaseInfo == null) //make sure PerSecond has run first
                return;
            #region Tracing
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkSpire );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DarkS-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            #endregion

            ExternalFactionBaseInfoRoot externalRoot = AttachedFaction.BaseInfo as ExternalFactionBaseInfoRoot;
            if ( externalRoot == null )
            {
                ArcenDebugging.ArcenDebugLog( "Error! Could not get BaseInfo as ExternalFactionBaseInfoRoot for faction " + AttachedFaction.GetDisplayName(), Verbosity.ShowAsError );
                return;
            }
            PlanetPathfinder pathfinderConservative = null;

            shipsPerPlanet.Clear();
            UnassignedShips.Clear();
            TeamsAimedAtPlanet.Clear();

            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
                {
                    team.IsAllowedToStack = true; //this is added to help existing saves; it can be removed later
                    team.DeepInfo.Reset(); //reset team count information
                }

                FactionUtilityMethods.Instance.FlushUnitsFromReinforcementPointsOnAllRelevantPlanets( AttachedFaction, Context, 5f );

                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    //Plan: If we are on our way somewhere, do go there
                    //if there are enemies on our current planet, fight them
                    //otherwise pick a random King unit and go to the next planet on the way
                    if ( !BaseInfo.ConquestMode && //this is only for the old, non-conquest mode
                         entity.CalculateFinalDestinationPlanetIndex_Safe() != -1 &&
                         entity.CalculateFinalDestinationPlanetIndex_Safe() != entity.GetPlanetIndexSafe() )
                        continue; // if heading somewhere else, skip

                    if ( entity.TypeData.IsMobile )
                    {
                        if ( BaseInfo.ConquestMode )
                        {
                            if ( entity.FireteamId < 0 )
                                UnassignedShips.Add( entity );
                            else
                            {
                                Fireteam team = FireteamBaseUtility.GetFireteamById( BaseInfo.Teams, entity.FireteamId );
                                if ( team != null )
                                {
                                    team.DeepInfo.AddUnit( entity );
                                }
                                else
                                    entity.FireteamId = -1; //something happened to the fireteam, so lets find a new one next LRP stage
                            }
                            entity.MinorFactionStackingID = -1; //this is added to help existing saves; it can be removed later
                            continue;
                        }

                        shipsPerPlanet[entity.Planet].Add( entity );
                    }
                }
                if ( BaseInfo.ConquestMode )
                {
                    if ( tracing )
                        tracingBuffer.Add( "Tracing dark spire in conquest mode\n" );
                    FInt overkillRequired = FInt.FromParts( 0, 900 );
                    FireteamUtility.UpdateFireteams( AttachedFaction, Context, pathingCacheData, BaseInfo.Teams, TeamsAimedAtPlanet, tracingBuffer, overkillRequired );
                    FireteamUtility.UpdateRegiments( AttachedFaction, Context, pathingCacheData, BaseInfo.Teams, TeamsAimedAtPlanet, tracingBuffer, AttachedFaction.MinFireteamStrength, true );
                    int maxUnitsPerLRPToAdd = 1000;
                    if ( tracing )
                        tracingBuffer.Add( "There are " + UnassignedShips.Count + " ships needing fireteams; limit ourselves to the first " + maxUnitsPerLRPToAdd + ". Max fireteam strength: " + AttachedFaction.MaxFireteamStrength + "\n" );
                    for ( int i = 0; (i < UnassignedShips.Count && i < maxUnitsPerLRPToAdd); i++ )
                        AssignUnitToFireteam( AttachedFaction, UnassignedShips[i].GetSquad(), Context, pathingCacheData );
                    if ( AttachedFaction.NumFireteams != BaseInfo.Teams.GetItemCount() )
                        AttachedFaction.NumFireteams = BaseInfo.Teams.GetItemCount();
                }
                else
                {
                    foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in shipsPerPlanet )
                    {
                        Planet planet = pair.Key;
                        List<SafeSquadWrapper> ships = pair.Value;
                        int enemyStrength = planet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Hostile].TotalStrength;
                        int myStrength = planet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Self].TotalStrength;
                        //int factorForOverrun = 10;
                        if ( tracing )
                            tracingBuffer.Add( "LRP: considering " + ships.Count + " ships with " + myStrength + " strength on " + planet.Name + "\n" );
                        if ( enemyStrength < myStrength / 5 )
                        {
                            if ( !BaseInfo.ConquestMode )
                            {
                                if ( ships.Count <= 0 )
                                    continue; //nothing to send

                                //pick a random king and just wander towards it
                                Planet eventualDestPlanet = findRandomKingPlanet( Context );
                                Planet nextPlanet;

                                if ( pathfinderConservative == null )
                                {
                                    pathfinderConservative = externalRoot.GetConservativePathfinderThatMustBeReleasedOrNull();
                                    if ( pathfinderConservative == null )
                                        pathfinderConservative = externalRoot.GetNormalPathfinderThatMustBeReleased();
                                }

                                PathBetweenPlanetsForFaction pathCache = PathingHelper.InnerFindPath_RawSinglePathfinder( pathfinderConservative, AttachedFaction, "DarkSpireLRP",
                                    planet, eventualDestPlanet, PathingMode.SomeRandomRequest, Context, pathingCacheData );
                                if ( pathCache == null || pathCache.PathToReadOnly.Count == 0 )
                                {
                                    //You must already be on a king planet, or there's no path
                                    continue;
                                }
                                nextPlanet = pathCache.PathToReadOnly[0];
                                GameCommand wormholecommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_OtherRaidKing], GameCommandSource.AnythingElse );
                                wormholecommand.RelatedString = "DARKSPIRE";
                                for ( int j = 0; j < ships.Count; j++ )
                                    wormholecommand.RelatedEntityIDs.Add( ships[j].PrimaryKeyID );
                                wormholecommand.RelatedIntegers.Add( nextPlanet.Index );
                                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, wormholecommand, false );
                                if ( tracing )
                                    tracingBuffer.Add( ships.Count + " units on " + planet.Name + " are now heading toward " + nextPlanet.Name + " on the way toward the king on " + eventualDestPlanet.Name + ". Old (dumb) mode\n" );
                                continue;
                            }
                            //We are winning on this planet, so try to use forces wisely
                            //First thing is to see if there are any Loci nearby, and if so lets go there to help defend it
                            //bool onLocusPlanet = false;
                            GameEntity_Squad destinationLocus = null;
                            int maxLocusDistance = 5;
                            if ( tracing )
                                tracingBuffer.Add( "\tIn conquest mode.\n" );

                            List<SafeSquadWrapper> loci = this.BaseInfo.Loci.GetDisplayList();
                            for ( int j = 0; j < loci.Count; j++ )
                            {
                                GameEntity_Squad locus = loci[j].GetSquad();
                                if ( locus == null )
                                    continue;
                                if ( locus.Planet == planet || locus.Planet.GetHopsTo( planet ) < maxLocusDistance )
                                    destinationLocus = locus;
                            }
                            if ( destinationLocus != null )
                            {
                                if ( tracing )
                                    tracingBuffer.Add( "Ships on " + planet.Name + "  are defending the locus on " + destinationLocus.GetPlanetName_Safe() );
                                if ( destinationLocus.Planet != planet )
                                    FactionUtilityMethods.Instance.Helper_RaidSpecificPlanet( ships, planet, AttachedFaction, World_AIW2.Instance.CurrentGalaxy, 
                                        destinationLocus.Planet, false, Context, pathingCacheData, 5f );
                                continue;
                            }
                            //Okay, there are no loci for us to defend. Lets see if there are any adjacent planets with hostile enemies
                            Planet destination = null;
                            targetPlanets.Clear();
                            foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                            {
                                                             var pFaction = neighbor.GetStanceDataForFaction( AttachedFaction );
                                                             if ( pFaction[FactionStance.Hostile].TotalStrength > 0 )
                                                                 targetPlanets.Add( neighbor );
                            }
                            if ( targetPlanets.Count == 0 )
                                destination = planet.GetRandomNeighbor( false, Context );
                            else
                            {
                                cb_ftAttachedFaction = AttachedFaction;
                                targetPlanets.Sort( static delegate ( Planet Left, Planet Right )
                                                    {
                                                        var lFaction = Left.GetStanceDataForFaction( cb_ftAttachedFaction );
                                                        var rFaction = Right.GetStanceDataForFaction( cb_ftAttachedFaction );
                                                        return lFaction[FactionStance.Hostile].TotalStrength.CompareTo( rFaction[FactionStance.Hostile].TotalStrength );
                                                    } );
                                destination = targetPlanets[0];
                            }
                            if ( tracing )
                                tracingBuffer.Add( "Ships on " + planet.Name + "  are off to attack " + destination.Name );
                            if ( destination != null )
                                FactionUtilityMethods.Instance.Helper_RaidSpecificPlanet( ships, planet, AttachedFaction, 
                                    World_AIW2.Instance.CurrentGalaxy, destination, false, Context, pathingCacheData, 5f );
                        }
                        else
                        {
                            if ( tracing )
                                tracingBuffer.Add( "\t " + planet.Name + " is still a combat zone; fight it out\n" );

                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Dark Spire LRP error: " + e, Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();
                if ( pathfinderConservative != null )
                    pathfinderConservative.ReturnToPool();

                #region Tracing
                if ( tracing && !tracingBuffer.GetIsEmpty() )
                {
                    tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " Long Range Planning with fireteams concludes at " ).Add( Engine_Universal.ToHoursAndMinutesString( World_AIW2.Instance.GameSecond ) + " (" + World_AIW2.Instance.GameSecond + ") \n" );
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                }
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
            }
        }

        /* Begin Fireteam Stuff */

        //prefer close ones that you can get to safely
        private readonly ArcenLessLinkedList<Fireteam> AvailableFireteams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "DarkSpireFactionDeepInfo-AvailableFireteams" );
        private void AssignUnitToFireteam(Faction faction, GameEntity_Squad entity, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( entity == null )
                return;

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DarkS-AssignUnitToFireteam-trace", 10f ) : null;

            AvailableFireteams.Clear();
            //bool debug = false;
            if ( BaseInfo.Teams.GetItemCount() == 0 )
            {
                Fireteam team = Fireteam.CreateNewWithIDFromList( BaseInfo.Teams );
                team.MyStrengthMultiplierForStrengthCalculation = FInt.FromParts(1, 00);
                team.EnemyStrengthMultiplierForStrengthCalculation = FInt.FromParts(1, 500);
                team.StrengthToBringOnline = faction.MinFireteamStrength + Context.RandomToUse.Next( 0, faction.MaxFireteamStrength - faction.MinFireteamStrength );
                BaseInfo.Teams.AddIfNotAlreadyIn( team );
            }
            int maxHops = 10;
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
            {
                if ( team.status == FireteamStatus.Disbanded ||
                        team.status == FireteamStatus.ReadyToAttack ||
                        team.status == FireteamStatus.Attacking )
                    continue; //if a fireteam is ready to fight, don't send more ships to that team
                if ( team.status == FireteamStatus.Staging && //if a fireteam is already "pretty strong", don't give it more
                     team.DeepInfo.TeamStrength > faction.MaxFireteamStrength )
                    continue;
                if ( team.DeepInfo.TeamStrength > faction.MaxFireteamStrength * 2 && team.DeepInfo.ShipsInFireteam.Count > 20 ) //if this is much stronger than usual, don't make it even stronger
                    continue;

                if ( team.DeepInfo.CurrentPlanet == null )
                {
                    //this is a brand new fireteam, so it's totally safe.
                    AvailableFireteams.AddIfNotAlreadyIn( team );
                    continue;
                }
                if ( AvailableFireteams.GetItemCount() > 4 ) //if we already have a lot of possible fireteams to use, don't keep looking
                    break;

                Int16 hops = 0;
                int dangerOfTeam = Fireteam.GetDangerOfPath( faction, Context, PathCacheData, entity.Planet, team.DeepInfo.CurrentPlanet, true, out hops );
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
            {
                #region Tracing
                FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
                #endregion
                return;
            }

            int percentNewTeam = 40;
            if ( AvailableFireteams.GetItemCount() >= 4 )
                percentNewTeam = 0;
            if ( Context.RandomToUse.Next( 0, 100 ) < percentNewTeam || AvailableFireteams.GetItemCount() == 0 )
            {
                Fireteam team = Fireteam.CreateNewWithIDFromList( BaseInfo.Teams );
                team.MyStrengthMultiplierForStrengthCalculation = FInt.FromParts(1, 00);
                team.EnemyStrengthMultiplierForStrengthCalculation = FInt.FromParts(1, 500);
                team.StrengthToBringOnline = faction.MinFireteamStrength + Context.RandomToUse.Next( 0, faction.MaxFireteamStrength - faction.MinFireteamStrength );
                int numShipsForConcentratingEfforts = 5; //before we're too strong, best to concentrate our forces
                if ( BaseInfo.Teams.GetItemCount() < numShipsForConcentratingEfforts )
                    team.PercentBestTarget = 100;
                else if ( this.AttachedFaction.SpecialFactionData.FireteamPercentBestTarget > 0 )
                    team.PercentBestTarget = this.AttachedFaction.SpecialFactionData.FireteamPercentBestTarget;
                else
                    team.PercentBestTarget = 65;
                team.PercentDistanceBestTarget = 45; //marauders often get far-flung empires
                team.PreferredMaxDistance = 5;
                team.DeepInfo.AddUnit(entity);
                entity.FireteamId = team.FireTeamID;

                team.DeepInfo.IdentifyCurrentPlanet(); //in case this unit is the first unit
                BaseInfo.Teams.AddIfNotAlreadyIn( team );
                if ( tracing )
                    tracingBuffer.Add("Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path B: new fireteam. Percent New Fireateam: " + percentNewTeam + "\n");
                #region Tracing
                FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
                #endregion
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
            #region Tracing
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
            #endregion

        }

        public override GameEntity_Squad GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( Planet CurrentPlanetForFireteam,  ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            int currentDanger = -1;
            GameEntity_Squad retreatPoint = null;
            List<SafeSquadWrapper> vgs = this.BaseInfo.VGs.GetDisplayList();
            for ( int i = 0; i < vgs.Count; i++ )
            {
                GameEntity_Squad outpost = vgs[i].GetSquad();
                if ( outpost == null )
                    continue;
                Int16 hops = 0;
                int danger = Fireteam.GetDangerOfPath( this.AttachedFaction, Context, PathCacheData, CurrentPlanetForFireteam, outpost.Planet, true, out hops);
                if ( danger < currentDanger || currentDanger == -1)
                {
                    retreatPoint = outpost;
                    currentDanger = danger;
                }
                if ( danger == 0 )
                    break;
            }
            return retreatPoint;
        }

        //Set immediately before the target sorts so the comparisons can be non-capturing static
        //delegates.  [ThreadStatic] because faction planning runs on background threads.
        [ThreadStatic] private static Planet cb_ftCurrentPlanet;
        [ThreadStatic] private static Faction cb_ftAttachedFaction;
        [ThreadStatic] private static FInt cb_ftFalloff;

        public override void GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass( bool DefenseMode, Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData,
            List<FireteamTarget> PreferredTargets, List<FireteamTarget> FallbackTargets, object TeamObj )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DarkS-GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            FInt falloffForDistance = FInt.FromParts (0, 050);
            GetPreferredDarkSpireTargets( PreferredTargets, AttachedFaction, Context);
            //We do a two-stage check here. First we cull the targets whose defenses are much stronger than usual.
            //then we sort targets by how hard it is to get there
            //sort targets by how hard it is to get there
            for ( int i = 0; i < PreferredTargets.Count; i++ )
            {
                FireteamTarget target = PreferredTargets[i];
                Int16 hops = 0;
                target.dangerOfTarget = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForFireteam, PreferredTargets[i].planet, true, out hops );
                PreferredTargets[i] = target;
            }

            cb_ftCurrentPlanet = CurrentPlanetForFireteam;
            cb_ftAttachedFaction = AttachedFaction;
            cb_ftFalloff = falloffForDistance;
            PreferredTargets.Sort( static delegate ( FireteamTarget Left, FireteamTarget Right )
            {
                int lDistance = Left.planet.GetHopsTo( cb_ftCurrentPlanet );
                int rDistance = Right.planet.GetHopsTo( cb_ftCurrentPlanet );
                int lDanger = Left.dangerOfTarget;
                int rDanger = Right.dangerOfTarget;
                if ( Left.planet.GetControllingOrInfluencingFaction() == cb_ftAttachedFaction )
                    lDanger /= 2;
                if ( Right.planet.GetControllingOrInfluencingFaction() == cb_ftAttachedFaction )
                    rDanger /= 2;

                lDanger = lDanger + (cb_ftFalloff * lDistance).IntValue;
                rDanger = rDanger + (cb_ftFalloff * rDistance).IntValue;

                return lDanger.CompareTo( rDanger );
            } );
            for ( int i = PreferredTargets.Count - 1; i >= 0; i-- )
            {
                FireteamTarget target = PreferredTargets[i];
                if ( Fireteam.IsThisAWinningBattle( AttachedFaction, Context, target.planet, 4 ) || target.dangerOfTarget == -1 )
                    PreferredTargets.Remove( target );
            }

            for ( int i = 0; i < PreferredTargets.Count; i++ )
            {
                //discard things that are enough more dangerous than other things on the list
                if ( i == 0 ) continue;
                FireteamTarget target = PreferredTargets[i];
                FireteamTarget weakestTarget = PreferredTargets[0];
                FInt danger = (FInt)target.dangerOfTarget;
                FInt weakestDanger = (FInt)weakestTarget.dangerOfTarget;
                if ( target.planet.GetControllingOrInfluencingFaction() == AttachedFaction )
                    danger /= 2;
                if ( danger > weakestDanger * FInt.FromParts( 2, 000 ) )
                {
                    //this is way more dangerous than earlier targets on the list, so ignore it
                    PreferredTargets.RemoveRange( i, PreferredTargets.Count - i );
                }

            }
            GetFallbackDarkSpireTargets( FallbackTargets, AttachedFaction, Context);
            //Currently we don't do fallback targets.
            if ( FallbackTargets != null || FallbackTargets.Count == 0 )
            {
                FallbackTargets.Sort( static delegate ( FireteamTarget Left, FireteamTarget Right )
                {
                    int lDifficulty = Left.dangerOfTarget;
                    int rDifficulty = Right.dangerOfTarget;
                    return lDifficulty.CompareTo( rDifficulty );
                } );
            }


            bool debug = true;
            if ( debug && tracing )
            {
                tracingBuffer.Add("Getting lurk/target Preferred Targets\n");
                for ( int i = 0; i < PreferredTargets.Count; i++ )
                    tracingBuffer.Add("\t").Add(PreferredTargets[i].GetPlanetName_Safe()).Add(" difficulty ").Add( PreferredTargets[i].dangerOfPath).Add(" \n");
                tracingBuffer.Add("Getting lurk/target Fallback Targets\n");
                for ( int i = 0; i < FallbackTargets.Count; i++ )
                    tracingBuffer.Add("\t").Add(FallbackTargets[i].GetPlanetName_Safe()).Add("\n");   
            }
        }
        public void GetPreferredDarkSpireTargets( List<FireteamTarget> listToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context)
        {
            listToFill.Clear();
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null )
                    continue;
                if(! otherFaction.GetIsHostileTowards(faction) )
                    continue;
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    listToFill.Add(new FireteamTarget(entity));
                }
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.GrantsMinorFactionPlanetControl ) )
                {
                    //can go after minor factions
                    if ( Fireteam.IsThisAWinningBattle(faction, Context, entity.Planet, 2) )
                        continue;  //if we are already attacking and comfortably winning, don't bother sending more units
                    listToFill.Add(new FireteamTarget(entity));
                }
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.CommandStation ) )
                {
                    //Dark Spire in conquest mode can kill command stations
                    if ( Fireteam.IsThisAWinningBattle(faction, Context, entity.Planet, 2) )
                        continue; //if we are already attacking and comfortably winning, don't bother sending more units
                    listToFill.Add( new FireteamTarget(entity.Planet) );
                }
            }
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( Fireteam.IsThisAWinningBattle( faction, Context, planet, 2 ) )
                    continue;

                if ( planet.GetControllingFactionType() == FactionType.NaturalObject )
                {
                    Faction influencingFaction = World_AIW2.Instance.GetFactionByIndex( planet.PrimaryInfluencingFaction );
                    if ( influencingFaction == null || !influencingFaction.GetIsFriendlyTowards( faction ) )
                        listToFill.Add( new FireteamTarget( planet ) );
                }
                if ( planet.GetControllingOrInfluencingFaction() == faction )
                {
                    //are there enemies attacking us?
                    EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( faction );
                    int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                    int myStrength = myFactionData[FactionStance.Self].TotalStrength;
                    if ( hostileStrength > myStrength / 2 )
                        listToFill.Add( new FireteamTarget( planet ) );
                }
            }
        }
        public void GetFallbackDarkSpireTargets( List<FireteamTarget> listToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context)
        {
            listToFill.Clear();
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.GetControllingFactionType() == FactionType.NaturalObject )
                {
                    if ( Fireteam.IsThisAWinningBattle( faction, Context, planet, 2 ) )
                        continue;
                    EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( faction );
                    int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                    int myStrength = myFactionData[FactionStance.Self].TotalStrength;
                    if ( hostileStrength > 2000 )
                        listToFill.Add( new FireteamTarget( planet ) );
                }
            }
        }
        public override Planet GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass( Planet TargetPlanet, int TeamStrength, Planet CurrentPlanetForTeam, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            Planet bestPlanet = null;
            int dangerOfPathFromBestPlanet = -1;
            //int dangerOfPathToBestPlanet = 999999;
            int distanceFromBestPlanet = 9999;
            Int16 hopsFromBestPlanet = 9999;
            int unused = 0;
            //this logic partially cribbed from IndependentAIFleet.cs::Helper_DoTargetFindingSweep

            if ( TargetPlanet == null )
                throw new Exception ("No target planet set in get lurk planet?!");
            //int debugCode = 0;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DarkS-GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            bool preferUnwatchedPlanets = false;
            if ( tracing )
                tracingBuffer.Add("Getting a lurk planet. Target planet " + TargetPlanet.Name).Add(". ").Add(AttachedFaction.BaseInfo.Allegiance).Add("\n");
            foreach ( Planet.PlanetAtHopDistance _phd in TargetPlanet.PlanetsWithinXHops_NoFilters( -1 ) )
            {
                Planet planet = _phd.Planet;
                Int16 Distance = _phd.Hops;
                if ( planet == TargetPlanet )
                    continue;

                int planetDefensiveStrength = Fireteam.GetPlanetDefensiveStrength( planet, AttachedFaction, true, ref unused,
                                                                          FInt.Zero, FInt.Zero);
                if ( planet.GetControllingFaction().GetIsHostileTowards(AttachedFaction) && planetDefensiveStrength > TeamStrength / 10 )
                    continue;

                //Don't path through any particularly dangerous planets
                Int16 hops = 0;
                int totalDifficultyOfPathToLurkPlanet = Fireteam.GetDangerOfPath(AttachedFaction, Context, PathCacheData, CurrentPlanetForTeam, planet, true, out hops);
                if ( totalDifficultyOfPathToLurkPlanet >= TeamStrength * 5 ) //as long as they only outnumber us 5:1, let's go!
                    continue;

                int totalDifficultyOfPathToTarget = Fireteam.GetDangerOfPath(AttachedFaction, Context, PathCacheData, planet, TargetPlanet, true, out hops);
                if ( preferUnwatchedPlanets && planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    totalDifficultyOfPathToTarget *= 2; //penalized watched planets if desired

                if ( totalDifficultyOfPathToTarget < 0 )
                    totalDifficultyOfPathToTarget = 0; //pathing through allied planets is basically the same

                if ( tracing )
                    tracingBuffer.Add("\tConsidering " + planet.Name + " danger of path " + totalDifficultyOfPathToTarget + " distance " + Distance + " intel " + planet.IntelLevel ).Add("\n");

                if ( dangerOfPathFromBestPlanet > totalDifficultyOfPathToTarget ||
                     dangerOfPathFromBestPlanet == -1 )
                {
                    if ( tracing )
                        tracingBuffer.Add("\t" + planet.Name + " is now the lurk location; path A" ).Add("\n");
                    dangerOfPathFromBestPlanet = totalDifficultyOfPathToTarget;
                    distanceFromBestPlanet = Distance;
                    hopsFromBestPlanet = hops;
                    bestPlanet = planet;
                }

                if ( dangerOfPathFromBestPlanet == totalDifficultyOfPathToTarget &&
                     ( distanceFromBestPlanet > Distance ||
                       hopsFromBestPlanet > hops ) )
                {
                    if ( tracing )
                        tracingBuffer.Add("\t" + planet.Name + " is now the lurk location; path B" ).Add("\n");
                    dangerOfPathFromBestPlanet = totalDifficultyOfPathToTarget;
                    distanceFromBestPlanet = Distance;
                    hopsFromBestPlanet = hops;
                    bestPlanet = planet;
                }

                if ( hopsFromBestPlanet <= 3 && dangerOfPathFromBestPlanet <= TeamStrength / 10 )
                    break; //we found a good lurk within easy striking distance of the planet, so exit now
            }
            return bestPlanet;
        }

        /* End Fireteam Stuff */

        public override void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        {
            //nothing special to do
        }

        #region DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly
        public override void DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly( ref int debugStage, GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull,
            Faction factionThatKilledEntity, Faction entityOwningFaction, int numExtraStacksKilled, ArcenHostOnlySimContext Context )
        {
            if ( entity == null )
                return;

            try
            {
                //Handle Dark Spire gaining energy from ANY ships dying on their planets.
                //Note we do not handle the case where the player ship is being scrapped, since FiringSystemOrNull == null is
                //includes the case where shots die
                {
                    debugStage = 7000;
                    //If the Dark Spire is enabled and there is a Vengeance Generator on the planet, give that VG some energy
                    bool darkSpireDebug = false;
                    if ( this.BaseInfo.DarkSpirePlanets.DisplayContains( entity.Planet ) )
                    {
                        debugStage = 7100;

                        if ( entity.TypeData.IsDrone )
                        {
                            if ( darkSpireDebug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Drones do not give the Dark Spire energy", Verbosity.DoNotShow );
                            return;
                        }
                        if ( entity.TypeData.AlwaysSelfAttritions )
                        {
                            if ( darkSpireDebug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Always self-attritioning units like " + entity.TypeData.GetDisplayName() + " do not give the Dark Spire energy", Verbosity.DoNotShow );
                            return;
                        }
                        debugStage = 7200;
                        if ( darkSpireDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine( entity.TypeData.InternalName + " died on " + entity.GetPlanetName_Safe() + " which has a vengeance generator", Verbosity.DoNotShow );
                        debugStage = 7300;
                        //                    if ( darkSpireBaseInfo == null )
                        debugStage = 7500;
                        Faction killingFaction = factionThatKilledEntity;
                        Faction dyingFaction = entityOwningFaction;

                        debugStage = 7600;
                        DarkSpireHarvestData killingRatios = DarkSpireHarvestDataTable.Instance.GetRowByFaction( killingFaction );
                        DarkSpireHarvestData dyingRatios = DarkSpireHarvestDataTable.Instance.GetRowByFaction( dyingFaction );
                        if ( killingRatios == null || dyingRatios == null )
                        {
                            if ( killingFaction == null || dyingFaction == null )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Could not find ratio for  null faction", Verbosity.DoNotShow );
                            else
                                ArcenDebugging.ArcenDebugLogSingleLine( "Could not find ratio for " + killingFaction.GetDisplayName() + " killing " + dyingFaction.GetDisplayName(), Verbosity.DoNotShow );
                            return;
                        }
                        debugStage = 7900;

                        FInt energyToAdd = entity.GetStrengthPerSquad() * killingRatios.RatioWhenKilling * dyingRatios.RatioWhenDying;
                        energyToAdd += energyToAdd * numExtraStacksKilled;
                        if ( !entity.TypeData.IsMobile )
                        {
                            energyToAdd *= dyingRatios.RatioForStructuresDying;
                        }
                        if ( darkSpireDebug )
                        {
                            string killingFactionName = "unknown";
                            if ( killingFaction != null )
                                killingFactionName = killingFaction.GetDisplayName();
                            string dyingFactionName = "unknown";
                            if ( dyingFaction != null )
                                dyingFactionName = dyingFaction.GetDisplayName();
                            ArcenDebugging.ArcenDebugLogSingleLine( dyingFactionName + " " + entity.TypeData.InternalName + " died due to " + killingFactionName + " and generated " + entity.GetStrengthPerSquad() + " * " + dyingRatios.RatioWhenDying + " * " + killingRatios.RatioWhenKilling + "= " + energyToAdd + "  energy. Ratio for dying structures: " + dyingRatios.RatioForStructuresDying, Verbosity.DoNotShow );
                        }


                        debugStage = 8000;

                        if ( this.BaseInfo.PerPlanet[entity.Planet.Index] != null )
                        {
                            this.BaseInfo.PerPlanet[entity.Planet.Index].TotalEnergy += energyToAdd;
                            this.BaseInfo.PerPlanet[entity.Planet.Index].NetEnergy += energyToAdd;
                            if ( darkSpireDebug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "DarkSpire net energy on " + entity.GetPlanetName_Safe() + " is now " + this.BaseInfo.PerPlanet[entity.Planet.Index].NetEnergy, Verbosity.DoNotShow );
                        }
                        else
                            ArcenDebugging.ArcenDebugLogSingleLine( "DarkSpire net energy could not increase because of null darkSpireBaseInfo.PerPlanet[entity.Planet.Index]", Verbosity.DoNotShow );
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in SpecialFaction_DarkSpire.DoOnAnyDeathLogic_HostOnly_FromCentralLoop_NotJustMyOwnShips stage " + debugStage + "\n" + e, Verbosity.ShowAsError );
            }
        }
        #endregion

        public override void DoPerSimStepLogic_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkSpire );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DarkS-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f ) : null;
            int intensity = AttachedFaction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
            //bool debug = false;
            int debugCode = 0;
            try
            {
                debugCode = 100;
                bool veryVerboseDebug = false;
                if ( veryVerboseDebug ) { }

                debugCode = 200;
                foreach ( KeyValuePair<Int16, DarkSpirePerPlanet> pair in BaseInfo.PerPlanet )
                {
                    debugCode = 300;
                    if ( pair.Value.TimeToAwaken > World_AIW2.Instance.GameSecond ) //if we are idle for some reason, just sit tight
                        continue;
                    if ( pair.Value.VGGeneratesEnergy && Context.RandomToUse.Next( 1, 100 ) <= (intensity * 2) + (pair.Value.numShipDesignsDownloaded * 5) )
                    {
                        int bonusEnergy = GenerateBonusEnergy( AttachedFaction, Context );
                        pair.Value.TotalEnergy += bonusEnergy;
                        pair.Value.NetEnergy += bonusEnergy;

                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Per-VG energy income: VG on planet " + World_AIW2.Instance.GetPlanetByIndex( pair.Key ).Name + " is getting " + bonusEnergy + " energy at " + World_AIW2.Instance.GameSecond );
                    }
                    debugCode = 400;
                    //The fallen spire campaign seems to be allowing VGs to get ridiculous amount of energy. Put in a hard-cap
                    //to prevent far-too-many units from being spawned before we do any unit spawning.
                    if ( pair.Value.NetEnergy >= pair.Value.EnergyThresholdForAttack * 3 )
                        pair.Value.NetEnergy = pair.Value.EnergyThresholdForAttack * 3;
                    if ( pair.Value.NetEnergy > pair.Value.EnergyThresholdForAttack )
                    {
                        debugCode = 500;
                        //either spawn ships or share energy
                        //feel free to update the conversionRatio
                        int randomNumber = Context.RandomToUse.Next( 1, 100 );
                        if ( randomNumber < DarkSpireFactionBaseInfo.percentShareEnergy && !pair.Value.NextAttackMustSpawnUnits )
                        {
                            debugCode = 600;
                            //find a planet or two to share energy with
                            int energyAvailableToShare = DarkSpireFactionBaseInfo.energyToShare;
                            while ( energyAvailableToShare > 0 )
                            {
                                debugCode = 700;
                                int energyNeededToTrigger = 0;
                                Int16 indexOfPreferredPlanet = getIndexOfPlanetToDonateEnergyTo( out energyNeededToTrigger, pair.Key );
                                if ( indexOfPreferredPlanet == -1 )
                                {
                                    debugCode = 800;
                                    //fallback case for if there's nowhere we really want to give energy
                                    Int16 indexOfLowest = getIndexOfPlanetWithLeastEnergy();
                                    BaseInfo.PerPlanet[indexOfLowest].NetEnergy += energyAvailableToShare;
                                    pair.Value.NetEnergy -= energyAvailableToShare;
                                    energyAvailableToShare = 0;
                                    if ( tracing ) tracingBuffer.Add( "Sharing " + energyAvailableToShare + " energy from VG on " + World_AIW2.Instance.GetPlanetByIndex( pair.Key ).Name + " --> " + World_AIW2.Instance.GetPlanetByIndex( indexOfLowest ).Name + ", least path.\n" );
                                    break;
                                }
                                debugCode = 900;
                                int energyToGive = energyNeededToTrigger;
                                if ( energyToGive > energyAvailableToShare )
                                    energyToGive = energyAvailableToShare;
                                if ( tracing ) tracingBuffer.Add( "Sharing " + energyToGive + " energy from VG on " + World_AIW2.Instance.GetPlanetByIndex( pair.Key ).Name + " --> " + World_AIW2.Instance.GetPlanetByIndex( indexOfPreferredPlanet ).Name + ".\n" );
                                BaseInfo.PerPlanet[indexOfPreferredPlanet].NetEnergy += energyToGive;
                                energyAvailableToShare -= energyToGive;
                            }

                            //For balance, lets try not boosting the energy threshold or conversion ration
                            //when sharing energy.
                            // pair.Value.ConversionRatio += BonusConversionRatioPerAttack;
                            // pair.Value.EnergyThresholdForAttack += EnergyThresholdIncreasePerAttack;
                        }
                        else
                        {
                            debugCode = 1000;
                            FInt strengthToSpawn = (FInt)(pair.Value.NetEnergy * pair.Value.ConversionRatio) / 100;
                            if ( FactionUtilityMethods.Instance.ShouldFactionsThrottle() )
                                strengthToSpawn /= 2; //for performance reasons, spawn fewer ships when the sim is slow

                            if ( tracing ) tracingBuffer.Add( "Spawning " + strengthToSpawn + " strength (" + pair.Value.NetEnergy + " at " + pair.Value.ConversionRatio + "%) on " + World_AIW2.Instance.GetPlanetByIndex( pair.Key ).Name + ". Throttled? " + FactionUtilityMethods.Instance.ShouldFactionsThrottle() +"\n" );
                            if ( BaseInfo.UseAlternateSpawningStyle )
                                pair.Value.NetEnergy = SpawnShips_OnMainSimThreadOnly_AlternateLogic( AttachedFaction, Context, strengthToSpawn, World_AIW2.Instance.GetPlanetByIndex( pair.Key ) );
                            else
                                pair.Value.NetEnergy = SpawnShips_OnMainSimThreadOnly( AttachedFaction, Context, strengthToSpawn, World_AIW2.Instance.GetPlanetByIndex( pair.Key ) );
                            pair.Value.ConversionRatio += this.BaseInfo.BonusConversionRatioPerAttack;
                            if ( pair.Value.ConversionRatio > BaseInfo.ConversionRatioCap )
                                pair.Value.ConversionRatio = BaseInfo.ConversionRatioCap;
                            pair.Value.EnergyThresholdForAttack += 100;
                            pair.Value.NextAttackMustSpawnUnits = false;
                        }
                    }
                    else
                    {
                        debugCode = 1100;
                        if ( veryVerboseDebug )
                        {
                            if ( pair.Value.NetEnergy > 0 )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Dark Spire: " + World_AIW2.Instance.GetPlanetByIndex( pair.Key ).Name + " has " + pair.Value.NetEnergy + " energy", Verbosity.DoNotShow );
                        }
                    }
                }
                debugCode = 1200;
                List<SafeSquadWrapper> loci = this.BaseInfo.Loci.GetDisplayList();
                List<SafeSquadWrapper> localVG = this.BaseInfo.VGs.GetDisplayList();
                //Attempt to place a new vengeance generator
                if ( World_AIW2.Instance.GameSecond >= (BaseInfo.LastLocusAttemptTime + this.BaseInfo.LocusAttemptInterval) &&
                   World_AIW2.Instance.GameSecond > (DarkSpireFactionBaseInfo.MinTimeInMinutesBeforeLocusesAreAllowed * 60) &&
                   this.BaseInfo.UseLoci )
                    debugCode = TryToSpawnNewLocus( Context, tracing, tracingBuffer, veryVerboseDebug );

                debugCode = 1500;
                //see if any Loci are done warping in
                for ( int i = 0; i < loci.Count; i++ )
                {
                    debugCode = 1600;
                    GameEntity_Squad Locus = loci[i].GetSquad();
                    if ( Locus == null )
                        continue;
                    if ( Locus.GetSecondsSinceCreation() >= DarkSpireFactionBaseInfo.LocusWarpInTime )
                    {
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Locus " + i + " on " + Locus.GetPlanetName_Safe() + " finished warping in at " + World_AIW2.Instance.GameSecond );
                        GameEntityTypeData entityData = GetNewVGEntityData( Context );
                        ArcenPoint spawnLocation = Locus.WorldLocation;
                        PlanetFaction cFaction = Locus.Planet.GetPlanetFactionForFaction( AttachedFaction );
                        Locus.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                        GameEntity_Squad.CreateNew_ReturnNullIfMPClient( cFaction, entityData, entityData.MarkFor( cFaction ),
                            cFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "DarkSpireVGOther" ); //part of sim, so fine

                    }
                }
                if ( BaseInfo.DarkSpireGeneratesEnergy || BaseInfo.ConquestMode )
                {
                    //if the player has downloaded a ship (or is in the fallen spire quest), the dark spire gets energy income. Each minute,
                    //pick a random vengeance generator and give it energy
                    if ( World_AIW2.Instance.GameSecond % 60 == 0 )
                    {
                        int numTriggers = 1;
                        if ( BaseInfo.ConquestMode )
                            numTriggers += 2;
                        for ( int i = 0; i < numTriggers; i++ )
                        {
                            KeyValuePair<Int16, DarkSpirePerPlanet> pair = BaseInfo.PerPlanet.GetRandomKeyValuePair( Context.RandomToUse );
                            int bonusEnergy = GenerateBonusEnergy( AttachedFaction, Context );
                            if ( FallenSpireFactionBaseInfo.Instance != null )
                            {
                                if ( FallenSpireFactionBaseInfo.Instance.SpireCities.Count >= 7 )
                                    bonusEnergy *= 5;
                                else if ( FallenSpireFactionBaseInfo.Instance.SpireCities.Count >= 3 )
                                    bonusEnergy *= 3;
                            }
                            pair.Value.TotalEnergy += bonusEnergy;
                            pair.Value.NetEnergy += bonusEnergy;

                            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Per-Faction Energy income: VG on planet " + World_AIW2.Instance.GetPlanetByIndex( pair.Key ).Name + " is getting " + bonusEnergy + " energy at " + World_AIW2.Instance.GameSecond );
                        }
                    }

                }
                debugCode = 1700;
                //Check if we need to perform a Vengeance Strike. In a Vengeance Strike, every
                //VG's energy meter gets supercharged (the amount is defined in the XML), and they all need to attack at the next second
                if ( BaseInfo.TimeForNextVengeanceStrike <= World_AIW2.Instance.GameSecond )
                {
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Triggering vengeance strike at " + World_AIW2.Instance.GameSecond );
                    if ( ArcenNetworkAuthority.GetIsHostMode() )
                    {
                        World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "Dark Spire Vengeance Strike</color> under way",
                            ChatType.LogToCentralChat, "ArkChiefOfStaff_VengeanceGeneratorStrike", null );
                    }
                    DarkSpireFactionBaseInfo.Instance.PerformVengeanceStrike();
                    BaseInfo.TimeForNextVengeanceStrike = World_AIW2.Instance.GameSecond + DarkSpireFactionBaseInfo.BaseMinutesBetweenVengeanceStrike * 60 + Context.RandomToUse.Next( 1, 300 );
                    World_AIW2.Instance.OnServer_FactionsToFastBlastToClients.Enqueue( this.AttachedFaction ); //fast blast this so we don't get stale notifications
                }

                //It's been frustrating to people that the Dark Spire Wards don't instantly kill units,
                //so handle that on the Sim thread instead of the Long Range Planning thread
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    if ( !entity.TypeData.IsMobile )
                        continue;
                    entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //part of sim, so fine
                    if ( BaseInfo.WardedPlanets.DisplayContains( entity.Planet ) )
                        entity.Die( Context, true );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception during Dark Spire stage3. debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            //Dark Spire doesn't need to set Influence right now. It may someday
            //            setInfluence(faction);
            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() )
            {
                tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " PerSecond trace ends\n" );
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            }
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion

        }

        protected virtual GameEntityTypeData GetNewVGEntityData( ArcenHostOnlySimContext Context )
        {
            if ( BaseInfo.ConquestMode )
                return GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "VengeanceGeneratorConquestSpawn" );
            else
                return GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "VengeanceGeneratorNormalSpawn" );
        }

        private int TryToSpawnNewLocus( ArcenHostOnlySimContext Context, bool tracing, ArcenCharacterBuffer tracingBuffer, bool veryVerboseDebug )
        {
            possibleLocusLocations.Clear();

            //foreach planet with a vengeance generator, if that planet is
            //clear of any hostiles and there are any adjacent planets that
            //are clear of hostiles, we can try to warp a Locus in
            int debugCode = 1300;
            if ( tracing && World_AIW2.Instance.GameSecond % 60 == 0 )
                tracingBuffer.Add( "Attempt a new Locus. Last attempt time " + BaseInfo.LastLocusAttemptTime + " interval " + this.BaseInfo.LocusAttemptInterval + " current time " + World_AIW2.Instance.GameSecond + ". Next Vengeance Strike at " + BaseInfo.TimeForNextVengeanceStrike + ".\n" );

            List<Planet> darkSpirePlanets = this.BaseInfo.DarkSpirePlanets.GetDisplayList();
            for ( int i = 0; i < darkSpirePlanets.Count; i++ )
            {
                Planet planet = darkSpirePlanets[i];
                if ( planet == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "BUG: planet is null in locus attempt, bizarrely", Verbosity.DoNotShow );
                    continue;
                }
                if ( veryVerboseDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Locus Checking the VG " + planet.Name + " as a potential source for a locus", Verbosity.DoNotShow );
                if ( BaseInfo.WardedPlanets.DisplayContains( planet ) )
                    continue; //this planet has a Dark Spire Ward, so omit it

                if ( planet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Hostile].TotalStrength < 1000 )
                {
                    if ( tracing ) tracingBuffer.Add( "VG on " + planet.Name + " has no hostiles. Check adjacent planets too to see if any of them are free at " + World_AIW2.Instance.GameSecond );
                    foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                    {
                        if ( darkSpirePlanets.Contains( neighbor ) )
                            continue;

                        // Do not allow planets with Loci or VG. Period.
                        if ( neighbor.GetFirstMatching( FactionType.SpecialFaction, "VengeanceGenerator", true, true ) != null )
                            continue; // VG 

                        if ( neighbor.GetFirstMatching( FactionType.SpecialFaction, "VengeanceGeneratorLocus", true, true ) != null )
                            continue; // Locus

                        // Do not allow planets with Dyson Spheres, of any type, since they would forever feed DS infinite hostile ships to eat.
                        if ( neighbor.GetFirstMatching( FactionType.SpecialFaction, "DysonSphere", true, true ) != null )
                            continue;

                        if ( BaseInfo.PerPlanet[neighbor.Index] != null &&
                             BaseInfo.PerPlanet[neighbor.Index].lastTimeAttemptedLocus >= World_AIW2.Instance.GameSecond - 600 )
                            continue;

                        int enemyStrength = neighbor.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Hostile].TotalStrength;
                        int myAndAlliedStrength = neighbor.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Self].TotalStrength + neighbor.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Friendly].TotalStrength;
                        if ( enemyStrength <= 0 ||
                            enemyStrength <= myAndAlliedStrength / 10 )
                        {
                            if ( tracing ) tracingBuffer.Add( neighbor.Name + " is adjacent to a VG planet and has no hostiles (or the enemies are outnumbered by me). Possible locus location. " );
                            possibleLocusLocations.Add( neighbor );
                        }
                    }
                }
            }
            debugCode = 1400;
            if ( possibleLocusLocations.Count > 0 )
            {
                BaseInfo.LastLocusAttemptTime = World_AIW2.Instance.GameSecond;
                Planet planet = possibleLocusLocations[Context.RandomToUse.Next( 0, possibleLocusLocations.Count )];
                if ( tracing ) tracingBuffer.Add( "Spawn new locus on " + planet.Name + " from " + possibleLocusLocations.Count + " possible locus locations." );

                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "VengeanceGeneratorLocus" );
                AngleDegrees angle = AngleDegrees.Create( (float)Context.RandomToUse.Next( 1, 360 ) );
                ArcenPoint center = Engine_AIW2.Instance.CombatCenter;

                ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 200 ), FInt.FromParts( 0, 650 ) );
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );

                GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                                            pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "DarkSpireVGLocus" ); //part of sim, so fine
                if ( ArcenNetworkAuthority.GetIsHostMode() )
                {
                    if ( planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    {
                        PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                        if ( chatHandlerOrNull != null )
                            chatHandlerOrNull.PlanetToView = planet;

                        World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "Dark Spire Locus</color> spawning on " + planet.Name,
                            ChatType.LogToCentralChat, "ArkChiefOfStaff_VengeanceGeneratorSpawn", chatHandlerOrNull );
                    }
                    else
                        World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "Dark Spire Locus</color> spawning somewhere in the galaxy",
                            ChatType.LogToCentralChat, "ArkChiefOfStaff_VengeanceGeneratorSpawn", null );
                }
            }

            return debugCode;
        }

        public int GenerateBonusEnergy(Faction faction, ArcenHostOnlySimContext Context )
        {
            FInt AIP = FactionUtilityMethods.Instance.GetCurrentAIP();
            int bonusIncome = DarkSpireFactionBaseInfo.EnergyIncomePerAIPPerMinute * AIP.IntValue;
            int intensity = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
            if(intensity < 4)
                bonusIncome = (bonusIncome * DarkSpireFactionBaseInfo.EnergyIncomeMultiplierLowIntensity).IntValue;
            else if(intensity < 7)
                bonusIncome = (bonusIncome * DarkSpireFactionBaseInfo.EnergyIncomeMultiplierMediumIntensity).IntValue;
            else
                bonusIncome = (bonusIncome * DarkSpireFactionBaseInfo.EnergyIncomeMultiplierHighIntensity).IntValue;
            return bonusIncome;
        }

        #region Regular Spawning Logic
        FInt SpawnShips_OnMainSimThreadOnly(Faction faction, ArcenHostOnlySimContext Context, FInt strengthToSpawn, Planet planet)
        {
            //A VG has enough energy to create ships; do that now
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkSpire );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DarkS-SpawnShips_OnMainSimThreadOnly-trace", 10f ) : null;
            FInt resultingStrength = strengthToSpawn;
            GameEntity_Squad VG = null;
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
            foreach ( GameEntity_Squad entity in pFaction.Entities.Squads( "VengeanceGenerator" ) )
            {
                VG = entity;
            }
            if(VG == null)
            {
                //Since VGs can be killed, it's possible to have planets with enough energy for a VG spawn
                //but no VG. For this case, do nothing (but leave the energy in case we get a VG here again later
                return FInt.Zero;
            }

            int maxAttempts = 10000; //For paranoia, cap the number of units spawnable at 10K, to make sure this loop always ends
            int numAttempts = 0;
            do{
                numAttempts++;
                GameEntityTypeData entityData = null;
                int rand = Context.RandomToUse.Next( 1, 100 );
                if(rand < 60)
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, UnitTag_Weak);
                else if(rand < 90)
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, UnitTag_Medium);
                else
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, UnitTag_Strong);
                if(entityData == null)
                {
                    throw new Exception("Could not find Dark Spire unit defined in XML. rand " + rand);
                }

                ArcenPoint spawnLocation = Engine_AIW2.Instance.CombatCenter;
                if(VG != null)
                    spawnLocation = planet.GetSafePlacementPoint_AroundEntity( Context, entityData, VG, FInt.FromParts( 0, 010 ), FInt.FromParts( 0, 050 ) );
                
                GameEntity_Squad entityOrNull = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                    pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "DarkSpireSpawnOthers" );
                if ( entityOrNull != null ) //is null on MP clients
                {
                    entityOrNull.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                    entityOrNull.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //part of sim, so fine
                }
                resultingStrength -= entityData.MarkStatsFor( pFaction.Faction.CurrentGeneralMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership;
                if(tracing) tracingBuffer.Add("Spawning " + entityData.InternalName + " on " + planet.Name + " strength of unit: " + entityData.MarkStatsFor( pFaction.Faction.CurrentGeneralMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership + " strength left for spawning: " + resultingStrength + ". \n" );
            }while(resultingStrength > 0 && numAttempts < maxAttempts);
            return FInt.Zero;
        }
        #endregion

        #region Alternate Spawning Logic
        FInt SpawnShips_OnMainSimThreadOnly_AlternateLogic( Faction faction, ArcenHostOnlySimContext Context, FInt strengthToSpawn, Planet planet )
        {
            //A VG has enough energy to create ships; do that now
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkSpire );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DarkS-SpawnShips_OnMainSimThreadOnly-trace", 10f ) : null;
            FInt resultingStrength = strengthToSpawn;
            GameEntity_Squad VG = null;
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
            foreach ( GameEntity_Squad entity in pFaction.Entities.Squads( "VengeanceGenerator" ) )
            {
                VG = entity;
            }
            if ( VG == null )
            {
                //Since VGs can be killed, it's possible to have planets with enough energy for a VG spawn
                //but no VG. For this case, do nothing (but leave the energy in case we get a VG here again later
                return FInt.Zero;
            }

            // Figure out how many of each type we'll want to spawn. Limit based on total strength request.
            FInt strengthToSpawnForThisTag, strengthToSpawnForThisUnit;
            int cost, canAfford, typesOfThisTag;
            ArcenPoint spawnLocation = Engine_AIW2.Instance.CombatCenter;

            #region Strong Spawns
            // Strong Ships - 50% of Budget, limit 1
            strengthToSpawnForThisTag = resultingStrength / 2;
            typesOfThisTag = GameEntityTypeDataTable.Instance.RowsByTag[UnitTag_Strong].Count;

            // Support multiple ships per type.
            GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( UnitTag_Strong ).ForEach( entityData =>
            {
                strengthToSpawnForThisUnit = strengthToSpawnForThisTag / typesOfThisTag;

                cost = entityData.GetForMark( pFaction.Faction.CurrentGeneralMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership;
                canAfford = (strengthToSpawnForThisUnit / cost).GetNearestIntPreferringLower();
                if ( canAfford > 1 )
                    canAfford = 1;
                if ( canAfford >= 1 )
                {
                    SpawnShips_Helper( entityData, canAfford, VG, Context );
                    resultingStrength -= cost * canAfford;
                }
            } );
            #endregion

            #region Medium Spawns
            // Medium Ships - 50% of Remaining Budget
            strengthToSpawnForThisTag = resultingStrength / 2;
            typesOfThisTag = GameEntityTypeDataTable.Instance.RowsByTag[UnitTag_Medium].Count;

            // Support multiple ships per type.
            GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( UnitTag_Medium ).ForEach( entityData =>
            {
                strengthToSpawnForThisUnit = strengthToSpawnForThisTag / typesOfThisTag;

                cost = entityData.GetForMark( pFaction.Faction.CurrentGeneralMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership;
                canAfford = (strengthToSpawnForThisUnit / cost).GetNearestIntPreferringLower();
                if ( canAfford >= 1 )
                {
                    SpawnShips_Helper( entityData, canAfford, VG, Context );
                    resultingStrength -= cost * canAfford;
                }
            } );
            #endregion

            #region Weak Spawns
            // Weak Ships - 100% of Remaining Budget
            strengthToSpawnForThisTag = resultingStrength;
            typesOfThisTag = GameEntityTypeDataTable.Instance.RowsByTag[UnitTag_Weak].Count;

            // Support multiple ships per type.
            GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( UnitTag_Weak ).ForEach( entityData =>
            {
                strengthToSpawnForThisUnit = strengthToSpawnForThisTag / typesOfThisTag;

                cost = entityData.GetForMark( pFaction.Faction.CurrentGeneralMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership;
                canAfford = (strengthToSpawnForThisUnit / cost).GetNearestIntPreferringLower();
                if ( canAfford >= 1 )
                {
                    SpawnShips_Helper( entityData, canAfford, VG, Context );
                    resultingStrength -= cost * canAfford;
                }
            } );
            #endregion

            return resultingStrength;
        }

        private void SpawnShips_Helper( GameEntityTypeData entityData, int amountToSpawn, GameEntity_Squad VG, ArcenHostOnlySimContext Context )
        {
            int squadsToSpawn, stacksPerSquad = 0, extraUnitsToStack = 0;
            if ( amountToSpawn <= 10 )
                squadsToSpawn = amountToSpawn;
            else
            {
                squadsToSpawn = 10;
                stacksPerSquad = (amountToSpawn - 10) / 10;
                extraUnitsToStack = (amountToSpawn - 10) % 10;
            }

            PlanetFaction pFaction = VG.PlanetFaction;

            for ( byte x = 0; x < squadsToSpawn; x++ )
            {
                ArcenPoint spawnLocation = VG.WorldLocation.GetRandomPointWithinDistance( Context.RandomToUse, 0, 1000 );

                GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, pFaction.Faction.GetGlobalMarkLevelForShipLine( entityData ),
                    pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "DarkSpire-Spawn" );

                if ( entity != null )
                {
                    entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );

                    if ( stacksPerSquad > 0 )
                        entity.AddOrSetExtraStackedSquadsInThis( (short)stacksPerSquad, false );
                    if ( extraUnitsToStack > 0 )
                    {
                        extraUnitsToStack--;
                        entity.AddOrSetExtraStackedSquadsInThis( 1, false );
                    }
                }
            }
        }
        #endregion

        private Int16 getIndexOfPlanetWithLeastEnergy()
        {
            //for Sharing, find the planet with the least energy
            Int16 lowestIndex = -1;
            FInt energyOfLowestPlanet = FInt.Zero;
            foreach ( KeyValuePair<Int16, DarkSpirePerPlanet> pair in BaseInfo.PerPlanet )
            {
                if((pair.Value.NetEnergy < energyOfLowestPlanet) || lowestIndex == -1 )
                {
                    lowestIndex = pair.Key;
                    energyOfLowestPlanet = pair.Value.NetEnergy;
                }
            }
            return lowestIndex;
        }
        private Int16 getIndexOfPlanetToDonateEnergyTo(out int energyNeededToTrigger, Int16 planetIdxToSkip)
        {
            //for Sharing, find the planet that's the closest to going off but hasn't gone off yet
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkSpire );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DarkS-getIndexOfPlanetToDonateEnergyTo-trace", 10f ) : null;
            Int16 bestIndex = -1;
            int energyRequiredForBestPlanet = -1;
            energyNeededToTrigger = -1;

            if ( tracing )
            {
                Planet origPlanet = World_AIW2.Instance.GetPlanetByIndex(planetIdxToSkip);
                tracingBuffer.Add("Trying to share energy from planet " + origPlanet.Name +"\n");
            }
            foreach ( KeyValuePair<Int16, DarkSpirePerPlanet> pair in BaseInfo.PerPlanet )
            {
                if ( pair.Key == planetIdxToSkip )
                    continue;
                int energyRequired = (pair.Value.EnergyThresholdForAttack - pair.Value.NetEnergy).IntValue;
                if ( tracing )
                {
                    Planet thisPlanet = World_AIW2.Instance.GetPlanetByIndex(pair.Key);
                    tracingBuffer.Add("\tChecking whether to share energy to " + thisPlanet.Name +". That planet would require " + energyRequired + " energy to create ships\n");
                }
                if ( energyRequired <= 0 )
                    continue;
                if ( energyRequiredForBestPlanet > energyRequired || energyRequiredForBestPlanet == -1 )
                {
                    if ( tracing )
                        tracingBuffer.Add("\tThis is currently our best planet\n");
                    bestIndex = pair.Key;
                    energyRequiredForBestPlanet = energyRequired;
                }
            }
            energyNeededToTrigger = energyRequiredForBestPlanet;
            return bestIndex;
        }
        private readonly List<Planet> KingPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "DarkSpireFactionDeepInfo-KingPlanets" );
        private Planet findRandomKingPlanet( ArcenHostOnlySimContext Context )
        {
            KingPlanets.Clear();
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                                                   KingPlanets.Add(entity.Planet);
                                                  }
            if ( KingPlanets.Count == 0 )
            {
                //if all the kings are dead, just go someplace random. The game is already over,
                //so this just avoids any unsightly errors
                return World_AIW2.Instance.GetRandomPlanet( false, Context );
            }
            return KingPlanets[Context.RandomToUse.Next( 0, KingPlanets.Count )];
        }
    }
}
