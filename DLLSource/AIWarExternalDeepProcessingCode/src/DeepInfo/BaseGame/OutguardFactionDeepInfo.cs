using Arcen.AIW2.Core;
using Arcen.Universal;
using System;


namespace Arcen.AIW2.External
{
    /* Overall outline of the Outguard. At the beginning of the game,
       there are some Outguard Beacons seeded on various planets. Each beacon
       allows you to access unique Outguard groups, and you need to own the planet to summon them.

       The UI will query the list of available Outguard Groups, check if they can be summoned on a given planet,
       and then do the summoning via helper functions in the SpecialFaction_Outguard class. See the functions under
       "UI Interface Section" below

       Outguard Groups are defined in  OutguardGroup.xml, and are represented in the
       OutguardGroupDataTable below. Units summoned by a given Outguard Group should be specified in Outguard.xml.
       Note these need to seperately defined in the XML for the description and description appenders.

       The beacons are managed via the OutguardFactionBaseInfo class.
    */
    //If a modder wants to add new GameCommands then add it to this enum

    public sealed class OutguardFactionDeepInfo : ExternalFactionDeepInfoRoot, IExternalDeepInfo_Singleton
    {
        public OutguardFactionBaseInfo BaseInfo;
        public static OutguardFactionDeepInfo Instance = null;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<OutguardFactionBaseInfo>();
            Instance = this;
        }

        protected override void Cleanup()
        {
            Instance = null;
            BaseInfo = null;
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 2;                       
        
        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            int debugCode = 0;
            try
            {
                //Note to other modders. If you want a faction to be able to inherit tech upgrades from the player,
                //this boolean needs to be set. It should be set only once at game start time.
                AttachedFaction.InheritsTechUpgradesFromPlayerFactions = true;

                int countOfPlayerFactionsNeedingOutguard = 0;
                foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                {
                    PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                    if ( playerType == null )
                        continue; //only seed if they say to
                    if ( playerType.MapGen_ShouldOutguardSeedForMe )
                        countOfPlayerFactionsNeedingOutguard++;
                }

                debugCode = 2;

                if ( countOfPlayerFactionsNeedingOutguard <= 0 )
                    return;

                Tutorial tutorialData = World_AIW2.Instance.TutorialOrNull;
                if ( tutorialData == null || !tutorialData.SkipOutguardBeacons )
                {
                    debugCode = 3;

                    int numToSeed = 0;
                    AIWar2GalaxySetting settings;
                    settings = AIWar2GalaxySettingTable.Instance.GetRowByName( "OutguardToSeed" );
                    numToSeed = settings.GetRelevantWorldSetup().GetIntBySetting( settings );

                    debugCode = 4;

                    int outguardPerBeacon = 2;
                    settings = AIWar2GalaxySettingTable.Instance.GetRowByName( "OutguardPerBeacon" );
                    outguardPerBeacon = settings.GetRelevantWorldSetup().GetIntBySetting( settings );

                    debugCode = 5;

                    StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, AttachedFaction,
                        SpecialEntityType.None, OutguardBeaconStateForPlanet.OutguardBeaconTag,
                        SeedingType.HardcodedCount, numToSeed,
                        MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 1, 1, PlanetSeedingZone.MostAnywhere,
                        SeedingExpansionType.ComplicatedOriginal );

                    debugCode = 6;


                    OutguardGroupData starting_outguard_group = null;
                    bool starting_planet_beacon = false;
                    {
                        debugCode = 9;
                        AIWar2GalaxySetting setting = AIWar2GalaxySettingTable.Instance.GetRowByNameOrNullIfNotFound( "StartingOutguard" );
                        if ( setting != null )
                        {
                            debugCode = 10;
                            string setting_value_str = setting?.GetRelevantWorldSetup()?.GetStringBySetting( setting );

                            debugCode = 11;
                            starting_outguard_group = OutguardGroupDataTable.Instance.GetRowByNameOrNullIfNotFound(setting_value_str);
                            if (starting_outguard_group != null)
                            {
                                if (starting_outguard_group.InternalName == "None")
                                {
                                    starting_outguard_group = null;
                                }
                                else if (starting_outguard_group.InternalName == "Random")
                                {
                                    starting_outguard_group = null;
                                    starting_planet_beacon = true;
                                }
                                else
                                {
                                    starting_planet_beacon = true;
                                }
                            }
                        }
                    }

                    debugCode = 20;

                    /*
                    ArcenDebugging.ArcenDebugLogSingleLine(
                        string.Format(
                            "spawnBeaconOnHomeworld = {0}, specificBeaconOnHomeworld = {1}, startingOutguad = {2}, numToSeed = {3}, outguardPerBeacon = {4}",
                            spawnBeaconOnHomeworld, specificBeaconOnHomeworld, startingOutguard, numToSeed, outguardPerBeacon ), Verbosity.DoNotShow );
                    */
                    if ( starting_planet_beacon )
                    {
                        Planet playerPlanet = null;

                        debugCode = 21;

                        // note that only the first player gets the starting outguard...
                        // ... but not sure how to handle it anyway, would they get the SAME outguard?
                        foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                        {
                                if ( entity.GetFactionTypeSafe() == FactionType.Player )
                                {
                                    playerPlanet = entity.Planet;
                                }

                            }

                        debugCode = 22;

                        if (playerPlanet == null)
                        {
                            // This can happen if spectating i guess.
                            ArcenDebugging.ArcenDebugLogSingleLine(
                                        string.Format(
                                            "Warning couldn't find a player king unit to identify starting planet so no outguard beacon will be placed there." ), Verbosity.DoNotShow );
                        }
                        else
                        {
                            debugCode = 24;

                            var entityType = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context,
                                OutguardBeaconStateForPlanet.OutguardBeaconTag );

                            debugCode = 25;

                            var seededEntity = playerPlanet.Mapgen_SeedEntity( Context, AttachedFaction, entityType, PlanetSeedingZone.InnerSystem );
                            //ArcenDebugging.ArcenDebugLogSingleLine( string.Format( "Seeded '{0}' at '{1}'.", seededEntity.GetTypeDisplayNameSafe(), playerPlanet.Name), Verbosity.DoNotShow );

                            debugCode = 26;

                            if ( starting_outguard_group != null )
                            {
                                debugCode = 27;
                                this.BaseInfo.AddNewGroupToBeacon_HostOnly( playerPlanet, starting_outguard_group );
                                //ArcenDebugging.ArcenDebugLogSingleLine( string.Format( "Added '{0}' to homeworld Outguard Beacon.", outguardGroup.DisplayName ), Verbosity.DoNotShow );
                            }
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "OutguardFactionDeepInfo.SeedStartingEntities_LaterEverythingElse exception at code '" + debugCode + "' " + e.ToString(), Verbosity.ShowAsError );
            }
        }

        public override void DoOnSpawnsOnDeath_AfterFullDeathOrPartOfStackDeath_HostOnly( GameEntity_Squad dyingEntity, GameEntity_Squad oneOfTheSpawningEntities, ArcenHostOnlySimContext Context )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                if (dyingEntity == null)
                    return;
                //UnityEngine.Debug.Log( "Dying outguard:" + dyingEntity.TypeData.InternalName + " spawning: " + oneOfTheSpawningEntities.TypeData.InternalName );
                debugCode = 200;
                OutguardPerUnitBaseInfo dyingOutguard = dyingEntity.TryGetExternalBaseInfoAs<OutguardPerUnitBaseInfo>();
                if (dyingOutguard != null && oneOfTheSpawningEntities != null )
                {
                    debugCode = 300;
                    //add the dying outguard data to the new spawning entity
                    OutguardPerUnitBaseInfo newOutguardData = oneOfTheSpawningEntities.CreateExternalBaseInfo<OutguardPerUnitBaseInfo>("OutguardPerUnitBaseInfo");
                    debugCode = 310;
                    newOutguardData.OutguardGroup = dyingOutguard.OutguardGroup;
                    oneOfTheSpawningEntities.GameSecondCreated = dyingEntity.GameSecondCreated;
                    //UnityEngine.Debug.Log( "Outguard outguard data was set!" );
                }
            } catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in Outguard DoOnSpawnsOnDeath_AfterFullDeathOrPartOfStackDeath_HostOnly debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }

        //These are static lists for DoLongRangePlanning; all of these are used only in this one method, so that's ok
        private readonly DictionaryOfDictionaryOfLists<int, Planet, SafeSquadWrapper> unitsPerGroup = DictionaryOfDictionaryOfLists<int, Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 30, 30, 30, "OutguardFactionDeepInfo-unitsPerGroup" );
        private readonly List<Planet> WorkingAdjacentValidPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "OutguardFactionDeepInfo-WorkingAdjacentValidPlanets" );
        private readonly List<Planet> PlanetsUnderAttack = List<Planet>.Create_WillNeverBeGCed( 500, "OutguardFactionDeepInfo-PlanetsUnderAttack" );
        private readonly List<Planet> PlanetsUnderAttackWithPaths = List<Planet>.Create_WillNeverBeGCed( 500, "OutguardFactionDeepInfo-PlanetsUnderAttackWithPaths" );
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            int debugCode = 0;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            PlanetPathfinder pathfinderConservative = null;
            try
            {
                //Make a sparseLookup that maps from groupRowIndex to List<SafeSquadWrapper> so we can move each group as a bloc
                //DefendLocalPlanet is easy, just stay there and patrol
                //Warp out is easy, it uses the same mechanism from HRF
                //Attack is easy, just follow the DysonANtagonizer example
                //patrol adjacent is a bit tougher
                // Algorithm for patrol: if we are on a planet with no enemies
                //                       check all Human planets for enemies. Foreach attacked planet, pick one at random and
                //                       if you can get to this planet w/o going through and AI planet, go to that planet
                //            Note that if the patrolling outguards run into enemies on the way they will stop and fight
                bool debug = false;
                unitsPerGroup.Clear();
                debugCode = 100;
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    if ( entity == null )
                        continue;
                    debugCode = 200;
                    // Do not process beacons or drones.
                    if ( entity.TypeData.GetHasTag( OutguardBeaconStateForPlanet.OutguardBeaconTag ) || entity.TypeData.GetHasTag( OutguardBeaconStateForPlanet.HackedOutguardBeaconTag ) || entity.TypeData.IsDrone )
                        continue;
                    debugCode = 250;
                    OutguardPerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<OutguardPerUnitBaseInfo>();
                    debugCode = 260;
                    if ( data == null ||
                         data.OutguardGroup == null )
                    {
                        //Note: the hydral hobbyist outguard has this happen all the time.
                        //ArcenDebugging.ArcenDebugLogSingleLine( "Did not find suitable outguardenary information", Verbosity.DoNotShow );
                        continue;
                    }
                    debugCode = 270;
                    unitsPerGroup[data.OutguardGroup.RowIndexNonSim][entity.Planet].Add( entity );
                }
                debugCode = 300;

                /* This section sets some data that is used by a number of outguard groups behaviours. Finding the AI King,
                   Finding planets under attack, and so on */
                if ( unitsPerGroup.GetCountOfInnerDicts() <= 0 )
                    return; //early exit if there are no outguard groups in the Galaxy right now
                Planet planetToAttackOrNull = FactionUtilityMethods.Instance.findAIKing( false );
                if ( planetToAttackOrNull == null )
                    planetToAttackOrNull = FactionUtilityMethods.Instance.FindStrongestEnemyPlanetOrNull( AttachedFaction );
                debugCode = 400;

                ExternalFactionBaseInfoRoot externalRoot = AttachedFaction.BaseInfo as ExternalFactionBaseInfoRoot;
                if ( externalRoot == null )
                {
                    ArcenDebugging.ArcenDebugLog( "Error! Could not get BaseInfo as ExternalFactionBaseInfoRoot for faction " + AttachedFaction.GetDisplayName(), Verbosity.ShowAsError );
                    return;
                }

                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( planet == null )
                        continue;
                    debugCode = 500;
                    PlanetFaction controllingFaction = planet.GetPlanetFactionForFaction( planet.GetControllingFaction() );
                //if this planets owner is allied to the Outguard and there are enemies on it (note this will not play nicely with the Devourer...)
                if ( controllingFaction.Faction.GetIsFriendlyTowards( AttachedFaction ) )
                    {
                        if ( planet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Hostile].TotalStrength > 0 )
                        {
                            PlanetsUnderAttack.Add( planet );
                        }
                    }
                }
                debugCode = 600;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "We detected " + PlanetsUnderAttack.Count + " planets under attack", Verbosity.DoNotShow );
                //For any outguard groups that will react defensively to other planets under attack, first
                //we find the list of human planets under attack
                foreach ( KeyValuePair<int, DictionaryOfLists<Planet, SafeSquadWrapper>> pair in unitsPerGroup )
                {
                    debugCode = 700;
                    int groupRowIndex = pair.Key;
                    OutguardGroupData group = OutguardGroupDataTable.Instance.Rows[groupRowIndex];
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Figuring out what to do with group " + group.InternalName, Verbosity.DoNotShow );

                    foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> innerPair in pair.Value )
                    {
                        debugCode = 800;
                        Planet currentPlanet = innerPair.Key;
                        if ( innerPair.Value == null )
                            ArcenDebugging.ArcenDebugLogSingleLine( group.InternalName + " on " + currentPlanet.Name + " has no ships and this is confusing", Verbosity.DoNotShow );
                        if ( debug )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( group.InternalName + " on " + currentPlanet.Name + " has " + innerPair.Value.Count + " ships", Verbosity.DoNotShow );

                        }
                        if ( currentPlanet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Hostile].TotalStrength > 0 )
                        {
                            debugCode = 900;
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( group.InternalName + " has found enemies on " + currentPlanet.Name + " and is staying to fight them. Note this overrides any previous orders", Verbosity.DoNotShow );
                            //if there are enemies on this planet, stay to fight them
                            for ( int k = 0; k < innerPair.Value.Count; k++ )
                            {
                                GameEntity_Squad squad = innerPair.Value[k].GetSquad();
                                if ( squad == null )
                                    continue;
                                squad.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //definitely breaks in multiplayer, because nonsim thread
                            }
                        }
                        else
                        {
                            debugCode = 1000;
                            if ( innerPair.Value.Count <= 0 )
                                continue;
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( group.InternalName + " has no enemies on " + currentPlanet.Name + " and is figuring out what to do next", Verbosity.DoNotShow );

                            //Figure out where to go now
                            if ( group.Behaviour == OnSpawnBehaviour.DefendLocalPlanet )
                            {
                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( group.InternalName + " is defending " + currentPlanet.Name, Verbosity.DoNotShow );

                                for ( int k = 0; k < innerPair.Value.Count; k++ )
                                    innerPair.Value[k].Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //definitely breaks in multiplayer, because nonsim thread
                            }
                            debugCode = 1100;
                            if ( group.Behaviour == OnSpawnBehaviour.WarpOutAfterBattle )
                            {
                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( group.InternalName + " is warping away from " + currentPlanet.Name + " to do this we set the SecondsAllowedToExist to right now, then do the despawn in the PerSecond code", Verbosity.DoNotShow );
                                for ( int k = 0; k < innerPair.Value.Count; k++ )
                                    innerPair.Value[k].UilitySetSecondsSinceCreation( group.SecondsAllowedToExist ); //this is a weird hack
                            }
                            debugCode = 1200;
                            if ( group.Behaviour == OnSpawnBehaviour.Attack && planetToAttackOrNull != null )
                            {
                                debugCode = 1300;

                                if ( pathfinderConservative == null )
                                {
                                    pathfinderConservative = externalRoot.GetConservativePathfinderThatMustBeReleasedOrNull();
                                    if ( pathfinderConservative == null )
                                        pathfinderConservative = externalRoot.GetNormalPathfinderThatMustBeReleased();
                                }
                                debugCode = 1310;

                                //take one step toward the AI King
                                PathBetweenPlanetsForFaction pathCache = PathingHelper.InnerFindPath_RawSinglePathfinder( pathfinderConservative, 
                                    AttachedFaction, "OutguardLRP1", currentPlanet, planetToAttackOrNull, PathingMode.SomeRandomRequest, Context, pathingCacheData );

                                if ( pathCache == null || pathCache.PathToReadOnly.Count <= 1 )
                                {
                                    //We must already be on the AU king planet, or there's no way to get there, so just chill I guess
                                    if ( debug )
                                        ArcenDebugging.ArcenDebugLogSingleLine( "Outguard Ships ships on " + currentPlanet.Name + " seems to have no path to the ai king", Verbosity.DoNotShow );
                                    continue;
                                }
                                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_OutguardAttack], GameCommandSource.AnythingElse );
                                command.RelatedString = "Outguard_Attack";
                                for ( int k = 0; k < innerPair.Value.Count; k++ )
                                {
                                    command.RelatedEntityIDs.Add( innerPair.Value[k].PrimaryKeyID );
                                }
                                command.RelatedIntegers.Add( pathCache.PathToReadOnly[0].Index );
                                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );

                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( group.InternalName + " is heading from " + currentPlanet.Name + " to " + pathCache.PathToReadOnly[1].Name + " on its way to the AI King", Verbosity.DoNotShow );

                            }
                            else if ( group.Behaviour == OnSpawnBehaviour.PatrolAdjacentFriendlyPlanets || group.Behaviour == OnSpawnBehaviour.PatrolAdjacentFriendlyAndNeutralPlanets )
                            {
                                debugCode = 1400;
                                if ( innerPair.Value.Count <= 0 )
                                    continue;

                                //first check for any allied planets under attack
                                PlanetsUnderAttackWithPaths.Clear();
                                for ( int n = 0; n < PlanetsUnderAttack.Count; n++ )
                                {
                                    debugCode = 1500;

                                    if ( pathfinderConservative == null )
                                    {
                                        pathfinderConservative = externalRoot.GetConservativePathfinderThatMustBeReleasedOrNull();
                                        if ( pathfinderConservative == null )
                                            pathfinderConservative = externalRoot.GetNormalPathfinderThatMustBeReleased();
                                    }
                                    debugCode = 1510;

                                    //Check if we can get to one of these planets w/o going through an AI planet
                                    PathBetweenPlanetsForFaction pathCache = PathingHelper.InnerFindPath_RawSinglePathfinder( pathfinderConservative, 
                                        AttachedFaction, "OutguardLRP2", currentPlanet, PlanetsUnderAttack[n], PathingMode.SomeRandomRequest, Context, pathingCacheData );
                                    if ( pathCache == null || pathCache.PathToReadOnly.Count <= 0 )
                                        continue; //not a valid path, so skip; no path there, or already there

                                    bool isValidPath = true;
                                    for ( int m = 0; m < pathCache.PathToReadOnly.Count; m++ )
                                    {
                                        Planet plan = pathCache.PathToReadOnly[m];
                                        if ( group.Behaviour == OnSpawnBehaviour.PatrolAdjacentFriendlyPlanets && !plan.GetControllingFaction().GetIsFriendlyTowards( AttachedFaction ) )
                                            isValidPath = false;
                                        if ( group.Behaviour == OnSpawnBehaviour.PatrolAdjacentFriendlyAndNeutralPlanets && plan.GetControllingFaction().GetIsHostileTowards( AttachedFaction ) )
                                            isValidPath = false;
                                        if ( !isValidPath )
                                            break;
                                    }
                                    if ( isValidPath )
                                        PlanetsUnderAttackWithPaths.Add( PlanetsUnderAttack[n] );
                                }
                                debugCode = 1600;
                                Planet destPlanet = null;
                                if ( PlanetsUnderAttackWithPaths.Count > 0 )
                                {
                                    debugCode = 1700;
                                    destPlanet = PlanetsUnderAttackWithPaths[Context.RandomToUse.Next( 0, PlanetsUnderAttackWithPaths.Count )];
                                    if ( debug )
                                        ArcenDebugging.ArcenDebugLogSingleLine( group.InternalName + " is heading from " + currentPlanet.Name + " to " + destPlanet.Name + " to defend against the attack there", Verbosity.DoNotShow );

                                }
                                else
                                {
                                    debugCode = 1800;
                                    WorkingAdjacentValidPlanets.Clear();
                                    //We don't have any planets under attack, so find an adjacent friendly (or neutral) planet and go there
                                    foreach ( Planet neighbor in currentPlanet.LinkedNeighbors( false ) )
                                    {
                                        if ( neighbor.GetControllingFaction().GetIsFriendlyTowards( AttachedFaction ) )
                                        {
                                            WorkingAdjacentValidPlanets.Add( neighbor );
                                            continue;
                                        }
                                        if ( group.Behaviour == OnSpawnBehaviour.PatrolAdjacentFriendlyAndNeutralPlanets &&
                                             !neighbor.GetControllingFaction().GetIsHostileTowards( AttachedFaction ) )
                                            WorkingAdjacentValidPlanets.Add( neighbor );
                                    }
                                    if ( WorkingAdjacentValidPlanets.Count != 0 )
                                    {
                                        destPlanet = WorkingAdjacentValidPlanets[Context.RandomToUse.Next( 0, WorkingAdjacentValidPlanets.Count )];
                                    }
                                    if ( debug )
                                    {
                                        if ( destPlanet != null )
                                            ArcenDebugging.ArcenDebugLogSingleLine( group.InternalName + " is heading from " + currentPlanet.Name + " to " + destPlanet.Name + " because it's patrolling", Verbosity.DoNotShow );
                                        else
                                            ArcenDebugging.ArcenDebugLogSingleLine( group.InternalName + " is just chilling because there are no legal moves", Verbosity.DoNotShow );
                                    }

                                }
                                if ( destPlanet != null )
                                    FactionUtilityMethods.Instance.Helper_RaidSpecificPlanet( innerPair.Value, innerPair.Key, AttachedFaction, World_AIW2.Instance.CurrentGalaxy, destPlanet, false, Context, pathingCacheData, 5f );
                            }
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception during Outguard LRP debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            finally
            {
                pathingCacheData.ReturnToPool();
                if ( pathfinderConservative != null )
                    pathfinderConservative.ReturnToPool();
            }
        }
        //used only at initialization time, don't sync to disk
        private readonly DrawBag<OutguardGroupData> outguardSpawnBag = DrawBag<OutguardGroupData>.Create_WillNeverBeGCed( 40, "OutguardFactionDeepInfo-outguardSpawnBag" );

        private void DoHostInitialization(bool debug, ArcenHostOnlySimContext Context)
        {
            // if we are initializing for the first time, set up the groups for each beacon

            int groupsPerBeacon = 2;
            var settings = AIWar2GalaxySettingTable.Instance.GetRowByName( "OutguardPerBeacon" );
            if (settings != null)
                groupsPerBeacon = settings.GetRelevantWorldSetup().GetIntBySetting( settings );

            // This is an Outguard Party feature...make this a mod-agnostic categorical seeding weight in future if more than just this is needed.
            int favorMultiplier = 1;
            settings = AIWar2GalaxySettingTable.Instance.GetRowByName( "FavorNewOutguard" );
            if ( settings != null )
            {
                var str = settings.GetRelevantWorldSetup().GetStringBySetting( settings );
                if ( str.Equals( "0x", StringComparison.InvariantCultureIgnoreCase ) )
                    favorMultiplier = 0;
                else if ( str.Equals( "1x", StringComparison.InvariantCultureIgnoreCase ) )
                    favorMultiplier = 1;
                else if ( str.Equals( "2x", StringComparison.InvariantCultureIgnoreCase ) )
                    favorMultiplier = 2;
                else if ( str.Equals( "3x", StringComparison.InvariantCultureIgnoreCase ) )
                    favorMultiplier = 3;
            }

            for ( int i = 0; i < OutguardGroupDataTable.Instance.Rows.Count; i++ )
            {
                OutguardGroupData group = OutguardGroupDataTable.Instance.Rows[i];
                // skip any already in the galaxy somehow ( starting outguard.. )

                if ( group.CanBeRandomlySeeded )
                {
                    int count = 1;
                    if ( group.OriginalXmlData.GetBool( "custom_bool_OutguardParty", false, false) )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( string.Format( "'OutguardParty' marked group '{0}' seeded x'{1}'", group.DisplayName, favorMultiplier), Verbosity.DoNotShow );
                        count = favorMultiplier;
                    }
                    
                    if (count > 0)
                        outguardSpawnBag.AddItem( group, count );
                }
            }

            // remove from bag any already somewhere
            // .. this is easier to do after the bag is filled so we don't need a temp list
            foreach ( GameEntity_Squad beacon in AttachedFaction.Squads( OutguardBeaconStateForPlanet.OutguardBeaconTag ) )
            {
                var beaconState = beacon.Planet.OutguardBeaconState;
                if ( beaconState == null )
                    continue;

                foreach (var group in beaconState.GroupsThatCanHire)
                    outguardSpawnBag.RemoveAnyItemsMatching( group );
            }

            int numBeacons = 0;
            DelReturn ProcessOutguardBeacon( GameEntity_Squad entity )
            {
                if ( entity.TypeData.GetHasTag( OutguardBeaconStateForPlanet.HackedOutguardBeaconTag ) )
                    return DelReturn.Continue;
                numBeacons++;

                int i = 0;
                var beaconState = entity.Planet.OutguardBeaconState;
                if ( beaconState != null )
                    i = beaconState.GroupsThatCanHire.Count;
                for ( ; i < groupsPerBeacon; i++ )
                {
                    if ( !outguardSpawnBag.GetHasItems() )
                    {
                        //throw new Exception( "More Outguard Groups were requested than are defined in the XML. Either turn down the number of seeded beacons, the number of outguards per beacon or add new groups. You have  " +
                        //    OutguardGroupDataTable.Instance.Rows.Count + " outguard groups defined in the XML. So far there were " + numBeacons + " beacons and " +
                        //    GroupsPerBeacon + " groups are needed per beacon" );

                        if ( debug )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "More Outguard Groups were requested than are defined in the XML. Either turn down the number of seeded beacons, the number of outguards per beacon or add new groups. You have  " +
                                                                    OutguardGroupDataTable.Instance.Rows.Count + " outguard groups defined in the XML. So far there were " + numBeacons + " beacons and " +
                                                                    groupsPerBeacon + " groups are needed per beacon", Verbosity.DoNotShow );
                        }

                        // only despawn if theres not ANY outguard in this beacon
                        if (i == 0)
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( string.Format( "Despawning outguard beacon on '{0}' because had no groups on it.", entity.Planet.Name ), Verbosity.DoNotShow );
                            
                            entity.Despawn( Context, true, InstancedRendererDeactivationReason.NotEnoughItemsToFillMeInAValidWay );
                        }
                        continue;
                    }
                    OutguardGroupData nextGroup = outguardSpawnBag.PickRandomItemAndDoNotReplace( Context.RandomToUse );
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Planet " + entity.GetPlanetName_Safe() + ":" + entity.Planet.Index + " adding group " + i + " of " + 
                            groupsPerBeacon + ": " + nextGroup.InternalName + " last rand: " + Context.RandomToUse.GetLastGenerated(), Verbosity.DoNotShow );

                    BaseInfo.AddNewGroupToBeacon_HostOnly( entity.Planet, nextGroup );
                }
                return DelReturn.Continue;
            }

            // do the homeworld first
            // so we don't run out of groups before getting to it
            // todo: for some reason this still doesn't prevent it from getting despawned later
            {
                Planet playerPlanet = null;
                foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                        if ( entity.GetFactionTypeSafe() == FactionType.Player )
                        {
                            playerPlanet = entity.Planet;
                        }

                    }

                if ( playerPlanet != null )
                {
                    if ( playerPlanet.OutguardBeaconState != null )
                    {
                        foreach ( GameEntity_Squad e in playerPlanet.Squads( OutguardBeaconStateForPlanet.OutguardBeaconTag ) )
                        {
                            ProcessOutguardBeacon( e );

                            break;
                        }
                    }
                }
            }

            // fill in outguard everywhere
            foreach ( GameEntity_Squad e in AttachedFaction.Squads( OutguardBeaconStateForPlanet.OutguardBeaconTag ) )
            {
                ProcessOutguardBeacon( e );
            }
        }

        public override void DoPerSimStepLogic_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            bool debug = false;

            if ( BaseInfo.hasBeenInitializedHostOnly == false )
            {
                DoHostInitialization(debug, Context);
                
                BaseInfo.hasBeenInitializedHostOnly = true;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Sync'ing global data", Verbosity.DoNotShow );
            }

            // handle queued spawn requests...

            bool veryVerboseDebug = false;
            if ( veryVerboseDebug && BaseInfo.QueuedRequests.Count > 0 )
                ArcenDebugging.ArcenDebugLogSingleLine( "There are " + BaseInfo.QueuedRequests.Count + " queued spawn requests requests", Verbosity.DoNotShow );
            for ( int i = BaseInfo.QueuedRequests.Count - 1; i >= 0; i-- )//For clients this is in the BaseInfo Sim-Step code, minus the spawning ofc!
            {
                OutguardSpawnRequest request = BaseInfo.QueuedRequests[i];
                if ( World_AIW2.Instance.GameSecond >= request.SpawnSecond )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Handling request " + i + " for group " + request.Group.InternalName, Verbosity.DoNotShow );
                    spawnOutguardGroup( Context, AttachedFaction, request );
                    BaseInfo.QueuedRequests.Remove( request, true );
                }
            }
        }

        public override void DoOnFirstSightingOfFactionByPlayer( bool IsFromBeacon, GameEntity_Squad SquadSeenOrNull, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return;
            if ( FactionUtilityMethods.Instance.OnlyNecromancerFactions() )
                return; //don't play the outguard journal if there are no human empires
            World_AIW2.Instance.QueueLogJournalEntryToSidebar( "OutguardFirstDetected", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        }

        public readonly List<OutguardGroupData> DespawningGroups = List<OutguardGroupData>.Create_WillNeverBeGCed( 60, "OutguardFactionDeepInfo-DespawningGroups" );
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            // Update our inherited tech values.
            AttachedFaction.InheritsTechUpgradesFromPlayerFactions = true;
            AttachedFaction.RecalculateMarkLevelsAndInheritedTechUnlocks();

            bool debug = false;
            bool debugSpawnOutguards = false;
            if ( World_AIW2.Instance.GameSecond < 10 * OutguardGroupDataTable.Instance.Rows.Count && (World_AIW2.Instance.GameSecond % 10 == 0) )
            {
                if ( debugSpawnOutguards )
                {
                    OutguardGroupData group = OutguardGroupDataTable.Instance.Rows[World_AIW2.Instance.GameSecond / 10];
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Enqueing test outguard " + group.InternalName, Verbosity.DoNotShow );
                    Faction player = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                    if(player == null)
                        for(int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                            if( World_AIW2.Instance.Factions[i].Type == FactionType.Player)
                            {
                                player = World_AIW2.Instance.Factions[i];
                                break;
                            }
                    if ( player == null )
                        return;
                    Planet kingPlanet = FactionUtilityMethods.Instance.findHumanKingForFaction( player, false );
                    if ( kingPlanet == null )
                        return;
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "spawning on planet index " + kingPlanet.Index, Verbosity.DoNotShow );
                    GameCommand_QueueOutguard.QueueCommand( group, kingPlanet, Engine_AIW2.Instance.CombatCenter, player );
                }
            }

            //despawn old ships
            DespawningGroups.Clear();
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
            {
                // Do not process beacons or drones.
                if ( entity.TypeData.GetHasTag( OutguardBeaconStateForPlanet.OutguardBeaconTag ) || entity.TypeData.GetHasTag( OutguardBeaconStateForPlanet.HackedOutguardBeaconTag ) || entity.TypeData.IsDrone )
                    continue;
                OutguardPerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<OutguardPerUnitBaseInfo>();
                if ( data == null )
                {
                    OutguardGroupData tagBasedGroup = null;
                    // Attempt to load by tag.
                    if ( entity.TypeData.TagsList.Count > 0 )
                    {
                        tagBasedGroup = OutguardGroupDataTable.Instance.GetRowByNameOrNullIfNotFound( entity.TypeData.TagsList[0] );
                    }
                    // Attempt to load by name.
                    if ( tagBasedGroup == null )
                    {
                        tagBasedGroup = OutguardGroupDataTable.Instance.GetRowByNameOrNullIfNotFound( entity.TypeData.InternalName );
                    }
                    if ( tagBasedGroup != null )
                    {
                        data = entity.CreateExternalBaseInfo<OutguardPerUnitBaseInfo>( "OutguardPerUnitBaseInfo" );
                        data.OutguardGroup = tagBasedGroup;
                    }
                    if ( data == null )
                    {
                        //Chris says: you know what?  This is okay.  Probably some sort of drone.
                        //ArcenDebugging.ArcenDebugLog( "Null from GetOutguardPerUnitBaseInfoExt for " + entity.TypeData.InternalName, Verbosity.ShowAsError );
                        continue;
                    }
                }
                OutguardGroupData group = data.OutguardGroup;
                if ( group == null )
                {
                    // Attempt to load by tag.
                    if ( entity.TypeData.TagsList.Count > 0 )
                    {
                        group = OutguardGroupDataTable.Instance.GetRowByNameOrNullIfNotFound( entity.TypeData.TagsList[0] );
                    }
                    // Attempt to load by name.
                    if ( group == null )
                    {
                        group = OutguardGroupDataTable.Instance.GetRowByNameOrNullIfNotFound( entity.TypeData.InternalName );
                    }
                    if ( group != null )
                        data.OutguardGroup = group;
                    if ( group == null )
                    {
                        ArcenDebugging.ArcenDebugLog( "Could not find group data for " + entity.ToStringWithPlanetAndOwner(), Verbosity.ShowAsError );
                        continue;
                    }
                }

                if ( group.SecondsAllowedToExist > 0 && group.SecondsAllowedToExist <= entity.GetSecondsSinceCreation() )
                {
                    AngleDegrees angle = AngleDegrees.Create( Context.RandomToUse.Next( 1, 360 ) );
                    ArcenPoint WarpOutDestination = entity.WorldLocation.GetPointAtAngleAndDistance( angle, entity.Planet.GravWellSize.DistanceScale_GravwellRadius );
                    if ( !DespawningGroups.Contains( group ) )
                        DespawningGroups.Add( group );

                    entity.despawnVis = DespawnVisualization.WarpOut;
                    entity.Despawn( Context, true, InstancedRendererDeactivationReason.MyLifespanRanOut );
                }
            }
            try
            {
                for ( int i = 0; i < DespawningGroups.Count; i++ )
                {
                    // Set repair timers.
                    World_AIW2.Instance.GetOutguardState( DespawningGroups[i]).TimeLeftToRepairInSeconds = DespawningGroups[i].RepairTimeInSeconds;
                    DespawningGroups[i].MarkGroupAsSurvived();
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Error in Outguard DespawningGroups: " + e, Verbosity.ShowAsError );
            }
            AllegianceHelper.AllyThisFactionToHumans( AttachedFaction );

            // Update repair counters, summon cooldowns, and summon capacity where aplicable.
            try
            {
                foreach ( ArcenDynamicTableRow groupDataRaw in OutguardGroupDataTable.Instance.AllRows() )
                {
                    OutguardGroupData groupData = (OutguardGroupData)groupDataRaw;
                    OutguardInfo groupState = World_AIW2.Instance.GetOutguardState( groupData);
                    if ( !groupState.HasBeenContacted )
                        continue; // Skip if we haven't contacted them.

                    #region Old Save Code
                    // Sanity check against saves loaded from previous saves, update some values to vague defaults.
                    if ( groupData.AIPPerAdditionalSummon == 0 )
                        groupData.AIPPerAdditionalSummon = 100;
                    if ( groupData.RepairTimeInSeconds == 0 ) // Set our general default values. May not be accurate to specifically balanced Outguard, but its the best we can do for now.
                        if ( groupData.DonateToPlayer )
                            groupData.RepairTimeInSeconds = 1800;
                        else
                            switch ( groupData.Class )
                            {
                                case OutguardClass.Utility:
                                case OutguardClass.Offense:
                                    groupData.RepairTimeInSeconds = 900;
                                    break;
                                case OutguardClass.Defense:
                                case OutguardClass.Stationary:
                                case OutguardClass.Unset:
                                default:
                                    groupData.RepairTimeInSeconds = 1200;
                                    break;
                            }
                    if ( groupData.TimeBetweenSummonsInSeconds == 0 )
                        groupData.TimeBetweenSummonsInSeconds = 300;
                    #endregion

                    int maxAIP = GlobalAIWorldBaseInfo.Instance.AIProgress_Total.ToInt();

                    groupState.TotalSummonsCapacity = (Int16)(groupData.TimesAllowedToSummon + groupState.SummonsFromAdditionalSources);
                    if ( groupData.AIPPerAdditionalSummon > 0 ) // Only apply AIP increase if the Outguard group defined it.
                        groupState.TotalSummonsCapacity += (Int16)(maxAIP / groupData.AIPPerAdditionalSummon);

                    if ( groupState.TimeLeftUntilNextSummonInSeconds > 0 )
                        groupState.TimeLeftUntilNextSummonInSeconds--;

                    if ( groupState.TimeLeftToRepairInSeconds > 0 )
                    {
                        groupState.TimeLeftToRepairInSeconds--;
                        if ( groupState.TimeLeftToRepairInSeconds == 0 )
                        {
                            groupState.TimesSummoned--;
                            if ( groupState.TimesSummoned > 0 )
                                groupState.TimeLeftToRepairInSeconds = groupData.RepairTimeInSeconds;
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Error in Outguard Update repair counters: " + e, Verbosity.ShowAsError );
            }
        }

        private void spawnOutguardGroup( ArcenHostOnlySimContext Context, Faction faction, OutguardSpawnRequest request )
        {
            ArcenDebugging.SingleLineQuickDebug( "spawnOutguardGroup at " + World_AIW2.Instance.GameSecond + ": " + request.ToString_DebugOnly() );
            bool debug = false;
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null ) //for spectator mode
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "作为旁观者无法召唤外卫部队。", ChatType.ShowLocallyOnly, null );
                return;
            }

            OutguardGroupData group = OutguardGroupDataTable.Instance.GetRowByNameOrNullIfNotFound( request.Group.InternalName );
            OutguardInfo groupState = World_AIW2.Instance.GetOutguardState( group);
            if ( group == null )
                throw new Exception( "spawnOutguardGroup: could not find row with InternalName: " + request.Group.InternalName );
            if ( request.Planet == null )
                throw new Exception( "spawnOutguardGroup: spawn requested with uninitialized planet " );
            if ( request.SpawnPoint == Engine_AIW2.Instance.CombatCenter )
            {
                // Special placement for stationary units.
                if ( group.Class == OutguardClass.Stationary )
                {
                    // If player owned, find a location between the command station and a hostile wormhole to spawn at.
                    if ( request.Planet.GetControllingFactionType() == FactionType.Player )
                    {
                        foreach ( GameEntity_Squad commandStation in request.Planet.Squads( EntityRollupType.CommandStation ) )
                        {
                            if ( commandStation.GetFactionTypeSafe() == FactionType.Player )
                            {
                                ArcenPoint commandPoint = commandStation.WorldLocation;
                                Planet neighbor = request.Planet.GetRandomNeighbor( false, Context );
                                // If our found neighbor isn't hostile, find one manually.
                                // If we're surrounded by friendly planets, we'll just keep our random choice.
                                if ( neighbor.GetControllingFaction().GetIsFriendlyTowards( commandStation.PlanetFaction.Faction ) )
                                {
                                    foreach ( Planet otherPlanet in request.Planet.LinkedNeighbors( false ) )
                                    {
                                        if ( otherPlanet.GetControllingFaction().GetIsHostileTowards( commandStation.PlanetFaction.Faction ) )
                                        {
                                            neighbor = otherPlanet;
                                        }
                                    }
                                }
                                // Get the wormhole to our found neighbor, and store its location.
                                ArcenPoint wormholePoint = request.Planet.GetWormholeTo( neighbor.Index ).WorldLocation;
                                // Get the angle from our command station to the wormhole's point, and put our stuff midway between there.
                                AngleDegrees angleTo = commandPoint.GetAngleToDegrees( wormholePoint );
                                request.SpawnPoint = commandPoint.GetPointAtAngleAndDistance( angleTo, commandPoint.GetDistanceTo( wormholePoint, true ) / 2 );
                            }
                        }
                    }
                    // Otherwise, spawn near the center of the planet.
                    else
                    {
                        AngleDegrees angle = AngleDegrees.Create( Context.RandomToUse.Next( 1, 360 ) );
                        ArcenPoint center = Engine_AIW2.Instance.CombatCenter;
                        //float locationMultiplier = 0.9f;
                        //ArcenPoint spawnLocation = center.GetPointAtAngleAndDistance(angle, (int)(ExternalConstants.Instance.DistanceScale_GravwellRadius * locationMultiplier));
                        request.SpawnPoint = center.GetPointAtAngleAndDistance( angle, (request.Planet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 100 )).IntValue );
                    }
                }
                else
                {
                    // Normal spawning logic, spawn near the edge of the gravity well.
                    AngleDegrees angle = AngleDegrees.Create( Context.RandomToUse.Next( 1, 360 ) );
                    ArcenPoint center = Engine_AIW2.Instance.CombatCenter;
                    //float locationMultiplier = 0.9f;
                    //ArcenPoint spawnLocation = center.GetPointAtAngleAndDistance(angle, (int)(ExternalConstants.Instance.DistanceScale_GravwellRadius * locationMultiplier));
                    request.SpawnPoint = center.GetPointAtAngleAndDistance( angle, (request.Planet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 900 )).IntValue );
                }
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "This request did not specify a point, so choose " + request.SpawnPoint + " at random", Verbosity.DoNotShow );

            }
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Handling request to spawn " + group.InternalName + " on " + request.Planet.Index + " (" + request.Planet.Name + ") at location " + request.SpawnPoint, Verbosity.DoNotShow );

            Faction factionToChargeForOutguards = request.ChargeFaction;
            if ( factionToChargeForOutguards == null )
                throw new Exception( "spawnOutguardGroup: could not find factionToChargeForOutguards" );

            // Double check that we are really allowed to do this...
            // This check was already done on the button
            // but if you spam click it you can get more than one without a cooldown.
            {
                if ( !group.GroupAllowedToSpawnOnPlanet( request.Planet ) )
                    return;

                // Stop if we don't have any fleets available.
                if ( groupState.TotalSummonsCapacity - groupState.TimesSummoned <= 0 )
                    return;

                // Stop if we're still cooling down since our last attack.
                if ( groupState.TimeLeftUntilNextSummonInSeconds > 0 )
                    return;
            }

            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
            if ( chatHandlerOrNull != null )
                chatHandlerOrNull.PlanetToView = request.Planet;

            // Notify the player about their summon.
            string msg = group.DisplayName + " has been hired by " + factionToChargeForOutguards.GetDisplayName() + " at " + request.Planet.Name + ". ";
            // player is looking at the outguard window right now, which shows this...
            //if (group.TimeBetweenSummonsInSeconds >= 0)
                //msg += "They will be willing to send another fleet in " + group.TimeBetweenSummonsInSeconds + " seconds.";
            World_AIW2.Instance.QueueChatMessageOrCommand( msg, ChatType.LogToCentralChat, chatHandlerOrNull );

            /* Now mark the global data that this group is no longer available */
            group.MarkGroupSummoned();

            PlanetFaction spawnFaction = request.Planet.GetPlanetFactionForFaction( faction );
            if ( group.DonateToPlayer )
                spawnFaction = request.Planet.GetPlanetFactionForFaction( localFaction );

            group.AdjustCountsToAIP( FactionUtilityMethods.Instance.GetCurrentAIP() );
            Helper_SpawnUnitsForOutguardGroup( Context, faction, group.UnitBag, request.Planet, request.SpawnPoint, group, spawnFaction );
        }

        private void Helper_SpawnUnitsForOutguardGroup( ArcenHostOnlySimContext Context, Faction faction, EntityTypeDrawingBag entityTypeBag, Planet planet, ArcenPoint point, OutguardGroupData groupData, PlanetFaction spawnFaction )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; 

            // If we have neither number or strength spawning set, skip.
            if ( EntityTypeDrawingBag.IsNullOrInvalid( entityTypeBag ) )
                return;

            Faction localPlayer = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localPlayer == null )
                return;

            ArcenPoint spawnPoint = ArcenPoint.Create( point.X, point.Y );
            faction.InheritsTechUpgradesFromPlayerFactions = true;
            //this is invoked 3 times for the primary, seconday and tertiary unit spawns
            //Spawn the Primary, Secondary and Tertiary units in that order
            bool debug = false;
            if ( planet.Factions.Count == 0 && faction.FactionIndex > 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Helper_SpawnUnitsForOutguardGroup: planet " + planet.Name + " somehow has planet.Factions.Count == " + planet.Factions.Count + ". I have no idea how this can happen", Verbosity.DoNotShow );
                return;
            }

            // Make sure we aren't spawning more units than our stack limit.
            int StackingCutoff = AIWar2GalaxySettingQuickAccess.StackingCutoffNPCs;
            // If we're giving these to a player faction, update our stacking cut off.
            if ( spawnFaction.Faction.Type == FactionType.Player )
                StackingCutoff = AIWar2GalaxySettingQuickAccess.StackingCutoffPlayers;
            // If we, for some reason, failed to load our stacking limit, assign a general one.
            if ( StackingCutoff == 0 )
                StackingCutoff = 60;

            using (var toSpawn = StructList<EntityTypeAndCount>.Get())
            {
                entityTypeBag.Draw( Context, localPlayer, 0, toSpawn, EntityTypeDrawingBag_AlternativeFactionMarkMode.SpecificUnitMarkLevel );
                
                if ( debug )
                {
                    ArcenDebugging.SingleLineQuickDebug( "Spawning Outguard " + groupData.InternalName );
                    for ( int i = 0; i < toSpawn.Count; i++ )
                    {
                        ArcenDebugging.SingleLineQuickDebug( "Index " + i + ": " + toSpawn[i].TypeData + " (" + toSpawn[i].Count + "x)" );
                    }
                }
                
                GameEntityTypeData currentType;
                int squadsToSpawn;
                bool canStack;
                int remainingStacks = 1;
                int currentStackCount;

                for ( int i = 0; i < toSpawn.Count; i++ )
                {
                    currentType = toSpawn[i].TypeData;
                    squadsToSpawn = toSpawn[i].Count;
                    canStack = !currentType.CannotBeStacked && currentType.IsMobile && !currentType.IsFleetLeader;
                    if ( canStack )
                        remainingStacks = Math.Max( 1, StackingCutoff - planet.GetPlanetFactionForFaction( faction ).Entities.GetCountFromListOfEntitiesByEntityType( currentType ) );

                    while ( squadsToSpawn > 0 )
                    {
                        if ( canStack )
                        {
                            currentStackCount = Math.Max( 1, squadsToSpawn / remainingStacks );
                            squadsToSpawn -= currentStackCount;
                            remainingStacks--;
                        } else
                        {
                            currentStackCount = 1;
                            squadsToSpawn--;
                        }

                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Spawning a stack of " + currentStackCount + " " + currentType.InternalName + " (remaining: " + squadsToSpawn + ")", Verbosity.Chat );
                        // Spawn them in a small area instead of on top of each other.
                        spawnPoint = planet.GetSafePlacementPoint_SpecificPoint(Context, currentType, point, 0, 1000);

                        GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( spawnFaction, currentType,
                            localPlayer.GetGlobalMarkLevelForShipLine( currentType ),
                            spawnFaction.Faction.LooseFleet, 0, spawnPoint, Context, "Outguard-FromGroup" );

                        if ( currentStackCount > 1 )
                            entity.AddOrSetExtraStackedSquadsInThis( (short) (currentStackCount - 1), true );
                        OutguardPerUnitBaseInfo data = entity.CreateExternalBaseInfo<OutguardPerUnitBaseInfo>( "OutguardPerUnitBaseInfo" );
                        data.OutguardGroup = groupData;
                    }
                }
            }
        }

        public static Faction GetFactionByName( string name )
        {
            Faction returnFaction = null;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction faction = World_AIW2.Instance.Factions[i];
                if ( faction.SpecialFactionData.InternalName == name )
                    returnFaction = faction;
            }
            return returnFaction;
        }
    }

}
