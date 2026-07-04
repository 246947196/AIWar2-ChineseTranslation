using Arcen.AIW2.Core;
using System;
using Arcen.Universal;
using System.Text;


namespace Arcen.AIW2.External
{
    /* Notes for Nasty things a Depot can do
       Reinforce Hunter Fleet ==> Fire on every train
       Reinforce Warden Fleet ==> Fire on every train
       Strengthen next Wave/CPA ==> Fire on every train

       Make a new structure that strengthens incoming waves ==> More strength depending on how many trains arrived savely, then station dies
       Make a new structure that strengthens incoming CPAs ==> More strength depending on how many trains arrived savely, then station dies
       
       Spawn a Superfortress ==> Waits for X trains to arrive, then station dies
       Spawn an Eye ==> Waits for X trains to arrive, then station dies
       Spawn a unit-spawner whose units just go for the Human King ==> More strength depending on how many trains arrived savely, then station dies
       Spawn an "AI progress increaser" ==> Waits for X trains to arrive, then station dies
     */

    //Astro trains mark level goes up based on the number of trains spawned (for that tier)
    //It goes to the Med or High tier spawns based on the number of trains killed by the player
    //Guard strength increases based on trains killed (regardless of who killed the trains)
    
    public sealed class AstroTrainsFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        public AstroTrainsFactionBaseInfo BaseInfo;
        public static AstroTrainsFactionDeepInfo Instance = null;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<AstroTrainsFactionBaseInfo>();
            Instance = this;
        }

        protected override void Cleanup()
        {
            BaseInfo = null;
            Instance = null;

            WorkingAllowedSpawnPlanets.Clear();

            WorkingAllowedSpawnPlanets.Clear();
            factionsWeHaveAlreadyComplainedAboutNotFinding.Clear();

            DifficultyOfStrongestAIFaction = 0;
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 4;

        public readonly List<Planet> WorkingAllowedSpawnPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "AstroTrainsFactionDeepInfo-WorkingAllowedSpawnPlanets" ); //working list
        private int DifficultyOfStrongestAIFaction = 0;
        
        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType)
        {
            bool debug = false;
            int stationsToSeed = 10;

            Tutorial tutorialData = World_AIW2.Instance.TutorialOrNull;
            if ( tutorialData == null || !tutorialData.SkipAstroTrainStations )
            {
                StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, AttachedFaction, SpecialEntityType.None, "AstroTrainStation", SeedingType.HardcodedCount, stationsToSeed - 1,
                    MapGenCountPerPlanet.One, MapGenSeedStyle.SmallBad, 1, 1, PlanetSeedingZone.OuterSystem, SeedingExpansionType.ComplicatedOriginal );

                //force one station to be near a human homeworld so that humans will always have a shot at a train
                int forcedDistanceToHumanHomeworld = 2;
                foreach ( Planet planet in galaxy.Planets( false ) )
                {
                    if ( planet.OriginalHopsToHumanHomeworld == forcedDistanceToHumanHomeworld )
                    {
                        GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "AstroTrainStation" );
                        planet.Mapgen_SeedEntity( Context, AttachedFaction, entityData, PlanetSeedingZone.InnerSystem );
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Seeding bonus astro train station on " + planet.Name, Verbosity.DoNotShow );
                        break;
                    }
                }
            }
        }

        private readonly List<SafeSquadWrapper> lrp_guardsToChangePlanet = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 50, "AstroTrainsFactionDeepInfo-lrp_guardsToChangePlanet" );
        private readonly List<SafeSquadWrapper> lrp_guardsToHeadToTrain = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 50, "AstroTrainsFactionDeepInfo-lrp_guardsToHeadToTrain" );

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            int debugCode = 0;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                bool debug = false;
                if ( debug ) { }
                #region Tracing
                bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.AstroTrains );
                ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AstroTrains-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
                #endregion
                debugCode = 100;

                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "AstroTrain" ) )
                {
                    debugCode = 2010;
                    
                    //So this is an Astro Train
                    AstroTrainsPerTrainBaseInfo data = entity.CreateExternalBaseInfo<AstroTrainsPerTrainBaseInfo>( "AstroTrainsPerTrainBaseInfo" );
                    Planet target = World_AIW2.Instance.GetPlanetByIndex( data.TargetDepotPlanetID );
                    if ( target == null )
                    {
                        debugCode = 2030;
                        ArcenDebugging.ArcenDebugLogSingleLine( "BUG:  " + entity.TypeData.InternalName + " on " + entity.GetPlanetName_Safe() + " has no valid Target Depot Planet ID", Verbosity.DoNotShow );
                        continue;
                    }

                    debugCode = 2040;
                    if ( entity.CalculateFinalDestinationPlanetIndex_Safe() != -1 &&
                         entity.CalculateFinalDestinationPlanetIndex_Safe() != entity.GetPlanetIndexSafe() )
                        continue; // this unit is en route to another planet
                    
                    debugCode = 2050;
                    //This entity was heading to the current planet, and currently
                    //has no orders. Either head to the structure on the planet or
                    //to the next station
                    if ( tracing ) tracingBuffer.Add( "LRP: Handling " + entity.ToStringWithPlanet() ).Add( "\n" );
                    if ( !entity.IsNextOrderMovement() )
                    {
                        debugCode = 2060;
                        if ( tracing ) tracingBuffer.Add( "\n\tTrain " + entity.ToStringWithPlanet() + " has no current movement; either fly to the station/depot on this planet or head to the next planet" );
                        //Find the station on the planet
                        GameEntity_Squad thisStationOrDepotOnPlanet = null;
                        if ( data.TargetDepotPlanetID == entity.Planet.Index )
                        {
                            debugCode = 2070;
                            foreach ( GameEntity_Squad depot in this.BaseInfo.TrainDepots.DisplaySquads() )
                            {
                                if ( depot.Planet == entity.Planet )
                                {
                                    thisStationOrDepotOnPlanet = depot;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            foreach ( GameEntity_Squad station in this.BaseInfo.Stations.DisplaySquads() )
                            {
                                if ( station.Planet == entity.Planet )
                                {
                                    thisStationOrDepotOnPlanet = station;
                                    break;
                                }
                            }
                        }
                        
                        if ( thisStationOrDepotOnPlanet == null || data.hasGottenToStation )
                        {
                            debugCode = 2080;
                            
                            //head to the next station (or to the first station, if this unit has just spawned
                            data.hasGottenToStation = false;
                            GameEntity_Squad nextStation = GetNextTrainDestination( Context, entity, data );
                            if ( data.stationsRemainingBeforeDepot > 0 )
                                data.stationsRemainingBeforeDepot--;
                            
                            debugCode = 2090;
                            PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "AstroTrainLRP", entity.Planet, 
                                nextStation.Planet, PathingMode.Shortest, Context, pathingCacheData );
                            
                            if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                            {
                                debugCode = 2100;
                                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse );
                                command.RelatedString = "TRAIN";
                                command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                                for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                                    command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                            }
                            if ( tracing ) tracingBuffer.Add( "\n\tTrain " + entity.ToStringWithPlanet() + " heading to " + nextStation.ToStringWithPlanet() + ". Stations left to visit: " + data.stationsRemainingBeforeDepot + ". This wormhole move command has " + pathCache.PathToReadOnly.Count + " moves, the first of which is to head to " + pathCache.PathToReadOnly[1].Name );
                        }
                        else
                        {
                            debugCode = 2110;
                            if ( tracing ) tracingBuffer.Add( "\n\tTrain " + entity.ToStringWithPlanet() + " ordered to fly to the station/depot on this planet." );
                            //go to the station on the planet
                            data.hasGottenToStation = true;
                            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse );
                            command.ToBeQueued = true;
                            command.RelatedPoints.Add( thisStationOrDepotOnPlanet.WorldLocation );
                            command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                            bool playAudioEffectForCommand = false;
                            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, playAudioEffectForCommand );
                        }
                    }
                    
                    debugCode = 2120;

                    //Cases: if we are on the Depot destination planet, go to the Depot

                    //This unit either isn't en route to a station yet (ie it has just spawned) or
                    //its station was just destroyed and it now has a new target
                }
                
                debugCode = 300;
                lrp_guardsToChangePlanet.Clear();
                lrp_guardsToHeadToTrain.Clear();
                debugCode = 500;
                foreach ( GameEntity_Squad train in this.BaseInfo.TrainsEnRoute.DisplaySquads() )
                {
                    debugCode = 600;
                    if ( train.TypeData == null || train.Planet == null || train.GetIsHostileTowards_Safe( AttachedFaction ) )
                    {
                        //train is dead; guards just chill.
                        continue;
                    }

                    AstroTrainsPerTrainBaseInfo trainInfo = train.CreateExternalBaseInfo<AstroTrainsPerTrainBaseInfo>( "AstroTrainsPerTrainBaseInfo" );
                    List<SafeSquadWrapper> trainGuards = trainInfo.DeployedGuardsOfThisTrain.GetDisplayList();

                    Planet trainPlanet = train.Planet;
                    //                ArcenDebugging.ArcenDebugLogSingleLine(train.ToStringWithPlanet() + " has " + pair.Value.Count + " guards", Verbosity.DoNotShow );
                    for ( int i = 0; i < trainGuards.Count; i++ )
                    {
                        debugCode = 700;
                        GameEntity_Squad guard = trainGuards[i].GetSquad();
                        if ( guard == null )
                            continue;
                        if ( guard.HasQueuedOrders() )
                        {
                            EntityOrder guardOrder = guard.RemoveInvalidatedOrdersAndReturnFirstValid_IncludingDecollision();
                            if ( guardOrder.TypeData != null && (guardOrder.TypeData.Type == EntityOrderType.Wormhole ||
                                                         guardOrder.TypeData.Type == EntityOrderType.Move_Normal) )
                                continue; //if the guard is going somewhere (ie back to the train), ignore
                        }
                        //If the planet has no enemies, or the train is
                        //close to exiting the planet, head to the train
                        if ( train.Planet == guard.Planet && !ShouldGuardsDeploy( train, Context ) )
                        {
                            //ArcenDebugging.ArcenDebugLogSingleLine("This planet is weak! " + guard.ToStringWithPlanet() + " rejoin train", Verbosity.DoNotShow );
                            //head to the train
                            lrp_guardsToHeadToTrain.Add( guard );
                            continue;
                        }
                        if ( train.Orders.GetNextDestinationOrNull() != null )
                        {
                            EntityOrder currentOrder = train.RemoveInvalidatedOrdersAndReturnFirstValid_IncludingDecollision();
                            if ( currentOrder.TypeData != null &&
                                 currentOrder.TypeData.Type == EntityOrderType.Wormhole )
                            {
                                GameEntity_Other wormhole = currentOrder.GetWormholeToOtherPlanetOrNull( train.Planet );
                                int range = 1000;
                                if ( Mat.DistanceBetweenPointsImprecise( train.WorldLocation, wormhole.WorldLocation ) < range )
                                {
                                    lrp_guardsToHeadToTrain.Add( guard );
                                    continue;
                                }
                            }
                        }

                        //if the train is on a different planet, go to that planet
                        if ( train.Planet != guard.Planet )
                            lrp_guardsToChangePlanet.Add( guard );
                    }
                    debugCode = 800;
                    if ( lrp_guardsToChangePlanet.Count > 0 )
                    {
                        FactionUtilityMethods.Instance.Helper_RaidSpecificPlanet( lrp_guardsToChangePlanet, lrp_guardsToChangePlanet[0].Planet, AttachedFaction,
                            World_AIW2.Instance.CurrentGalaxy, train.Planet, true, Context, pathingCacheData, 5f );
                    }
                    debugCode = 900;
                    if ( lrp_guardsToHeadToTrain.Count > 0 )
                    {
                        debugCode = 1000;
                        GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCFollowGuardedUnit], GameCommandSource.AnythingElse );
                        moveCommand.PlanetOrderWasIssuedFrom = train.Planet.Index;
                        moveCommand.RelatedPoints.Add( train.WorldLocation );
                        for ( int i = 0; i < lrp_guardsToHeadToTrain.Count; i++ )
                            moveCommand.RelatedEntityIDs.Add( lrp_guardsToHeadToTrain[i].PrimaryKeyID );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );
                    }
                }

                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( AttachedFaction.ToString() ).Add( " Long Range Planning trace ends" );
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in astro train LRP debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            finally
            {
                pathingCacheData.ReturnToPool();
            }
        }

        public override void DoPerSimStepLogic_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            

        }


        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context)
        {
            bool debug = false;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.AstroTrains );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AstroTrains-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f ) : null;

            int debugCode = 0;
            try
            {
                int rangeForShipArrival = 600;

                debugCode = 100;

                if ( this.DifficultyOfStrongestAIFaction == 0 )
                {
                    debugCode = 110;
                    int highestDifficulty = FactionUtilityMethods.Instance.GetHighestAIDifficulty();
                    this.DifficultyOfStrongestAIFaction = highestDifficulty;
                }

                debugCode = 200;
                if ( World_AIW2.Instance.GameSecond == 5 && BaseInfo.DebugEarlySpawnDepot )
                {
                    debugCode = 220;
                    GameEntity_Squad newDepot = SpawnNewTrainDepot( Context );
                    if ( newDepot == null )
                    {
                        if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                            ArcenDebugging.ArcenDebugLogSingleLine( "BUG: could not find planet on which to spawn a new train depot. Did you conquer the entire galaxy?", Verbosity.DoNotShow );
                    }
                }

                debugCode = 300;

                if ( this.BaseInfo.TrainDepots.Count < BaseInfo.maxDepots )
                {
                    debugCode = 310;
                    if ( this.BaseInfo.TimeLastDepotSpawned + BaseInfo.DepotSpawnInterval < World_AIW2.Instance.GameSecond )
                    {
                        debugCode = 320;
                        //choose a new planet to spawn a train depot, then spawn it
                        GameEntity_Squad newDepot = SpawnNewTrainDepot( Context );
                        if ( newDepot == null )
                        {
                            if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: could not find planet on which to spawn a new train depot. Did you conquer the entire galaxy?", Verbosity.DoNotShow );
                            return;
                        }
                        if ( tracing ) tracingBuffer.Add( "Spawning an Astro Train depot on " + newDepot.GetPlanetName_Safe() );
                    }
                }

                debugCode = 500;
                //Some train depots will do their thing once they get "enough"
                //trains. Others will do their thing after summoning a certain number
                //of trains, and the thing they do has power associated with the number
                //of trains that get through
                if ( debug )
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine("Processing Depots at " +World_AIW2.Instance.GameSecond, Verbosity.DoNotShow );
                }

                debugCode = 1100;
                foreach ( GameEntity_Squad depot in this.BaseInfo.TrainDepots.DisplaySquads() )
                {
                    debugCode = 1200;
                    //For each depot, check whether to do it's Effect
                    //and whether we need to spawn a new train
                    //Is it time to summon a new train
                    AstroTrainsPerDepotBaseInfo depotData = depot.CreateExternalBaseInfo<AstroTrainsPerDepotBaseInfo>( "AstroTrainsPerDepotBaseInfo" );

                    debugCode = 1210;
                    if ( depotData.LastTrainSpawnTime == -1 )//initialize the data
                    {
                        debugCode = 1220;
                        //For each type of train depot, set its behaviour appropriately.
                        //At the moment this is hard coded in so I don't have to add a field to a GameEntity
                        depotData.data.TrainsAllowedOnMap = BaseInfo.trainsOnMapPerDepot;
                        debugCode = 1230;
                        if ( tracing ) tracingBuffer.Add( "Initializing depot on " + depot.GetPlanetName_Safe() + " to be " + depotData.data );
                        //set the LastTrainSpawnTime to a value so we don't instantly spawn the train
                        //we want the next train to spawn "reasonably soon but not immediately"
                        depotData.LastTrainSpawnTime = World_AIW2.Instance.GameSecond - Context.RandomToUse.Next( BaseInfo.TrainSpawnInterval / 4, BaseInfo.TrainSpawnInterval / 2 );
                    }
                    debugCode = 1300;
                    int trainsEnRouteToDepot = TrainsEnRouteToDepot( depot );
                    debugCode = 1310;
                    if ( depotData.LastTrainSpawnTime + BaseInfo.TrainSpawnInterval < World_AIW2.Instance.GameSecond &&
                        (depotData.data.TrainsToSendBeforeFiring < depotData.TrainsSpawned) &&
                        trainsEnRouteToDepot < depotData.data.TrainsAllowedOnMap )
                    {
                        debugCode = 1400;
                        //If it's been "long enough" since the last time we sent a train to this depot and this
                        //depot hasn't hit the cap on how many trains to send, spawn a new train
                        int trainsToSpawn = 1;
                        if ( Context.RandomToUse.Next( 0, 100 ) < BaseInfo.PercentTrainMultiSpawn )
                        {
                            trainsToSpawn++;
                            if ( Context.RandomToUse.Next( 0, 100 ) < 5 )
                                trainsToSpawn++;
                        }
                        debugCode = 1500;
                        for ( int j = 0; j < trainsToSpawn; j++ )
                        {
                            debugCode = 1600;
                            GameEntity_Squad newTrain = SpawnNewTrain( Context, depot.Planet );
                            debugCode = 1700;
                            if ( newTrain == null )
                            {
                                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "BUG: could not find planet on which to spawn train. Did you conquer the entire galaxy?", Verbosity.DoNotShow );
                                break;
                            }
                            AstroTrainsPerTrainBaseInfo newTrainInfo = newTrain.CreateExternalBaseInfo<AstroTrainsPerTrainBaseInfo>( "AstroTrainsPerTrainBaseInfo" );
                            debugCode = 1800;
                            newTrainInfo.TargetDepotPlanetID = depot.Planet.Index;
                            if ( tracing ) tracingBuffer.Add( "Spawning train " + j + " of " + trainsToSpawn + " on " + newTrain.GetPlanetName_Safe() + " heading to " + depot.GetPlanetName_Safe() + ". " + depotData );
                            newTrainInfo.stationsRemainingBeforeDepot = Context.RandomToUse.Next( BaseInfo.MinStationsBeforeDepot, BaseInfo.MaxStationsBeforeDepot );
                            //Trains may or may not need to go near a player based on how scary the depot effects are
                            if ( depotData.data.TrainsMustGoNearPlayer )
                                newTrainInfo.hasGoneNearPlayer = false;
                            else
                                newTrainInfo.hasGoneNearPlayer = true;
                            newTrainInfo.hasGottenToStation = false;
                            newTrainInfo.DepotDataTableIndex = depotData.DepotTrainBehaviorID;

                            depotData.LastTrainSpawnTime = World_AIW2.Instance.GameSecond;
                            depotData.TrainsSpawned++;
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "depot " + depot.PrimaryKeyID + " on " + depot.GetPlanetName_Safe() + " last generated a train at " + depotData.LastTrainSpawnTime + " so spawn a new one on " + newTrain.GetPlanetName_Safe(), Verbosity.DoNotShow );
                        }
                    }
                    debugCode = 2100;
                    // if(debug)
                    //     ArcenDebugging.ArcenDebugLogSingleLine("Depot " + i + " on " + depot.GetPlanetName_Safe() + " " + depotData.data.ToString());
                    bool depotHasFired = false;
                    debugCode = 2200;
                    if ( shouldDepotFire( depot, depotData ) )
                    {
                        debugCode = 2300;
                        if ( tracing )
                            tracingBuffer.Add( "Proc depot event for " + depot.TypeData.InternalName + "  on " + depot.GetPlanetName_Safe() + " data: " + depotData.data.ToString() + "\n" );
                        depotHasFired = true;
                        debugCode = 2400;
                        DoDepotEvent( Context, depot, depotData );
                    }
                    debugCode = 2500;
                    if ( depotHasFired && depotData.data.SelfDestructOnFiring )
                    {
                        debugCode = 2600;
                        depot.Despawn( Context, true, InstancedRendererDeactivationReason.SelfDestructOnFiring );
                    }
                }
                // if(debug)
                //     ArcenDebugging.ArcenDebugLogSingleLine("Processing trains en route at " +World_AIW2.Instance.GameSecond, Verbosity.DoNotShow );

                debugCode = 4100;

                foreach ( GameEntity_Squad train in this.BaseInfo.TrainsEnRoute.DisplaySquads() )
                {
                    debugCode = 4200;
                    //Update trains en route
                    //If this train's depot is dead, find us a new depot
                    //If we have gotten to the Depot planet and we are close "enough" to the depot,
                    //deliver the cargo
                    train.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, on main thread
                    debugCode = 4300;
                    AstroTrainsPerTrainBaseInfo data = train.CreateExternalBaseInfo<AstroTrainsPerTrainBaseInfo>( "AstroTrainsPerTrainBaseInfo" );
                    debugCode = 4400;
                    GameEntity_Squad DestinationDepot = null;

                    debugCode = 4500;
                    //First we handle depots, then Guards
                    foreach ( GameEntity_Squad depot in this.BaseInfo.TrainDepots.DisplaySquads() )
                    {
                        debugCode = 4600;
                        if ( data.TargetDepotPlanetID == depot.Planet.Index )
                        {
                            DestinationDepot = depot;
                            break;
                        }
                    }

                    debugCode = 5100;

                    if ( DestinationDepot != null && data.stationsRemainingBeforeDepot <= 0 && data.TargetDepotPlanetID == train.Planet.Index )
                    {
                        debugCode = 5200;
                        if ( Mat.DistanceBetweenPointsImprecise( train.WorldLocation, DestinationDepot.WorldLocation ) < rangeForShipArrival )
                        {
                            AstroTrainsPerDepotBaseInfo depotData = DestinationDepot.CreateExternalBaseInfo<AstroTrainsPerDepotBaseInfo>( "AstroTrainsPerDepotBaseInfo" );
                            debugCode = 5200;
                            //train arrived!
                            depotData.TrainsThatArrivedSafely++;
                            //you can later find out how many astro trains arrived with World_AIW2.Instance.History.GetInt( "AT_Arrivals" )
                            debugCode = 5300;
                            World_AIW2.Instance.History.IncrementIntBy( "AT_Arrivals", 1 );
                            debugCode = 5500;
                            data.GuardMetal += 2000; //train gets bonus metal
                            if ( Context.RandomToUse.Next( 0, 100 ) < 50 )
                            {
                                UpdateTrainGuards( train, data, data.GuardMetal, Context ); //sometimes buy new guards
                                data.GuardMetal = 0;
                            }
                            debugCode = 5600;
                            if ( train.HullPointsLost > 0 )
                                train.HullPointsLost -= train.HullPointsLost / 10; //and a bit of healing

                            if ( tracing )
                                tracingBuffer.Add( "FLAGFLAGFLAG Train " + train.PrimaryKeyID + " out of safe arrivals " + depotData.TrainsThatArrivedSafely + "has arrived the depot at " + train.GetPlanetName_Safe() + ". " + depotData );
                            if ( ArcenNetworkAuthority.GetIsHostMode() )
                            {
                                debugCode = 5700;
                                if ( train.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
                                {
                                    debugCode = 5800;
                                    PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                                    if ( chatHandlerOrNull != null )
                                        chatHandlerOrNull.PlanetToView = train.Planet;

                                    World_AIW2.Instance.QueueChatMessageOrCommand( "Astro Train has arrived at the depot on " + train.GetPlanetName_Safe(), ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                                }
                                else
                                    World_AIW2.Instance.QueueChatMessageOrCommand( "Astro Train has arrived at the depot somewhere in the galaxy", ChatType.LogToCentralChat, string.Empty, null );
                            }
                            debugCode = 5900;
                            train.Despawn( Context, true, InstancedRendererDeactivationReason.IFinishedMyJob );
                        }
                    }
                    else if ( DestinationDepot == null )
                    {
                        debugCode = 6100;
                        //the original destionation depot has died, so pick a new depot if possible
                        if ( this.BaseInfo.TrainDepots.Count == 0 )
                        {
                            debugCode = 6200;
                            //no stations alive
                            train.Despawn( Context, true, InstancedRendererDeactivationReason.IGotLost );
                            debugCode = 6300;
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Train " + train.PrimaryKeyID + " was rerouting to a new depot, but there are no depots", Verbosity.DoNotShow );
                            continue;
                        }
                        else
                        {
                            debugCode = 6400;
                            GameEntity_Squad newDestinationDepot = this.BaseInfo.TrainDepots.Display_GetRandomItem( Context.RandomToUse ).GetSquad();
                            if ( newDestinationDepot == null || newDestinationDepot.Planet == null )
                                continue;
                            debugCode = 6500;
                            data.TargetDepotPlanetID = newDestinationDepot.Planet.Index;
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Train " + train.PrimaryKeyID + " is rerouting to new depot on " + newDestinationDepot.GetPlanetName_Safe() + " since the old depot died", Verbosity.DoNotShow );
                        }
                    }

                    debugCode = 7100;
                    DeployGuardsIfNecessary( train, data, Context );
                    debugCode = 7200;
                    CollectDeployedGuardsIfNecessary( train, data, Context );
                    debugCode = 7300;
                }
                debugCode = 8100;
                //Astro Trains don't really need to set Influence;
                //they show the Depots and Train Stations in the Galaxy map,
                //and that's adqequate
                //            setInfluence(faction);
                #region Tracing
                if ( tracing && !tracingBuffer.GetIsEmpty() )
                {
                    tracingBuffer.Add( "\n" ).Add( AttachedFaction.ToString() ).Add( " DoPerSecond trace ends" );
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                }
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in astro train Deep Stage3 debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        public void DeployGuardsIfNecessary( GameEntity_Squad train, AstroTrainsPerTrainBaseInfo data, ArcenHostOnlySimContext Context )
        {
            if ( !ShouldGuardsDeploy( train, Context ) )
                return;
            if ( data.StoredGuardsInsideThisTrain.Count == 0 )
                return;
            int debugCode = 100;
            try
            {
                foreach ( KeyValuePair<GameEntityTypeData, int> pair in data.StoredGuardsInsideThisTrain )
                {
                    //when spawning the guards, they come pre-stacked
                    int totalSquadsToSpawn = pair.Value;
                    int maxSquads = 10;
                    int numStacksPerSquad = 0;
                    int remainder = 0;
                    debugCode = 200;
                    if ( totalSquadsToSpawn > maxSquads )
                    {
                        totalSquadsToSpawn = maxSquads;
                        numStacksPerSquad = pair.Value / maxSquads;
                        remainder = pair.Value % totalSquadsToSpawn;
                    }
                    debugCode = 300;
                    for ( int i = 0; i < totalSquadsToSpawn; i++ )
                    {
                        debugCode = 400;
                        ArcenPoint spawnLocation = train.Planet.GetSafePlacementPoint_AroundEntity( Context, pair.Key, train, FInt.FromParts( 0, 005 ), FInt.FromParts( 0, 030 ) );

                        GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( train.PlanetFaction, pair.Key, train.CurrentMarkLevel,
                            train.GetFactionLooseFleetOrNull_Safe(), 0, spawnLocation, Context, "AstroTrainGuards" );  //is fine, main sim thread
                        if ( entity == null )
                            continue;
                        entity.ShouldNotBeConsideredAsThreatToHumanTeam = true;//this throws off the calculations
                        entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                        AstroTrainsPerTrainGuardUnitBaseInfo guardData = entity.CreateExternalBaseInfo<AstroTrainsPerTrainGuardUnitBaseInfo>( "AstroTrainsPerTrainGuardUnitBaseInfo" );
                        guardData.TrainIGuard = LazyLoadSquadWrapper.Create( train );
                        debugCode = 500;
                        if ( numStacksPerSquad > 0 )
                        {
                            debugCode = 600;
                            entity.AddOrSetExtraStackedSquadsInThis( (Int16)(numStacksPerSquad - 1), true );
                            if ( i == 0 && remainder > 0 )
                                entity.AddOrSetExtraStackedSquadsInThis( (Int16)remainder, false );
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in DeployGuardsIfNecessary debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            //we deployed them all, so clear it
            data.StoredGuardsInsideThisTrain.Clear();
        }
        public int calculateTrainStoredGuardStrength(GameEntity_Squad train)
        {
            if ( train == null )
                return 0;
            AstroTrainsPerTrainBaseInfo data = train.CreateExternalBaseInfo<AstroTrainsPerTrainBaseInfo>( "AstroTrainsPerTrainBaseInfo" );
            int strengthInside = 0;
            foreach ( KeyValuePair<GameEntityTypeData, int> pair in data.StoredGuardsInsideThisTrain )
            {
                GameEntityTypeData.MarkLevelStats markLevelStats = pair.Key.MarkStatsFor( train.CurrentMarkLevel );
                strengthInside += (markLevelStats.StrengthPerSquad_CalculatedWithNullFleetMembership * pair.Value);
            }
            return strengthInside;
        }

        public void CollectDeployedGuardsIfNecessary(GameEntity_Squad train, AstroTrainsPerTrainBaseInfo data,  ArcenHostOnlySimContext Context )
        {
            if ( ShouldGuardsDeploy(train, Context) )
                return;
            List<SafeSquadWrapper> guards = data.DeployedGuardsOfThisTrain.GetDisplayList();
            if ( guards == null || guards.Count == 0 )
                return;
            int range = 1000;
            for ( int i = 0; i < guards.Count; i++ )
            {
                if ( guards[i].Planet != train.Planet )
                    continue;
                if ( Mat.DistanceBetweenPointsImprecise(guards[i].WorldLocation, train.WorldLocation) < range )
                {
                    int currentCount = 0;
                    data.StoredGuardsInsideThisTrain.TryGetValue( guards[i].TypeData, out currentCount );
                    data.StoredGuardsInsideThisTrain[guards[i].TypeData] = currentCount + 1;
                    guards[i].Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut);
                }
            }
        }

        public bool ShouldGuardsDeploy(GameEntity_Squad train,  ArcenHostOnlySimContext Context )
        {
            int hostileStrength = -1;
            int friendlyStrength = -1;
            if ( Context.IsLongRangePlanning )
            {
                EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = train.Planet.GetStanceDataForFaction( AttachedFaction );

                hostileStrength =  myFactionData[FactionStance.Hostile].TotalStrength;
                friendlyStrength =  myFactionData[FactionStance.Self].TotalStrength +
                    myFactionData[FactionStance.Friendly].TotalStrength;
            }
            else
            {
                hostileStrength =  train.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                friendlyStrength =  train.PlanetFaction.DataByStance[FactionStance.Self].TotalStrength +
                    train.PlanetFaction.DataByStance[FactionStance.Friendly].TotalStrength;
            }
            //            ArcenDebugging.ArcenDebugLogSingleLine("For " + train.ToStringWithPlanet() + " hostile strength " + hostileStrength + " friendly " + friendlyStrength, Verbosity.DoNotShow );
            if ( hostileStrength > friendlyStrength / 4 )
                return true;
            return false;
        }

        #region SpawnNewTrainDepot
        private GameEntity_Squad SpawnNewTrainDepot( ArcenHostOnlySimContext Context, int ForceBehaviorTypeByID = -1 )
        {
            Planet targetPlanet = GetRandomPlanetNonKingNonDepotPlanet( Context, true);
            if(targetPlanet == null)
                return null;
            if ( ArcenNetworkAuthority.GetIsHostMode() )
            {
                if ( targetPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                {
                    PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.PlanetToView = targetPlanet;

                    World_AIW2.Instance.QueueChatMessageOrCommand( "<color=#" + AttachedFaction.FactionCenterColor.ColorHexBrighter + ">Astro Train Depot</color> spawning on  " + targetPlanet.Name,
                        ChatType.LogToCentralChat, "ArkChiefOfStaff_AstroTrainDepotSpawned", chatHandlerOrNull );
                }
                else
                    World_AIW2.Instance.QueueChatMessageOrCommand( "<color=#" + AttachedFaction.FactionCenterColor.ColorHexBrighter + ">Astro Train Depot</color> spawning somewhere in the galaxy",
                        ChatType.LogToCentralChat, "ArkChiefOfStaff_AstroTrainDepotSpawned", null );
            }
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "AstroTrainDepot" );
            ArcenPoint spawnLocation = targetPlanet.GetSafePlacementPointAroundPlanetCenter(Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 300 ) );
            PlanetFaction pFaction = targetPlanet.GetPlanetFactionForFaction( AttachedFaction );
            GameEntity_Squad newDepot = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "AstroTrainDepot" );

            //well, we should also go ahead and create this now, since we're actually creating the depot now...
            AstroTrainsPerDepotBaseInfo depotInfo = newDepot.CreateExternalBaseInfo<AstroTrainsPerDepotBaseInfo>( "AstroTrainsPerDepotBaseInfo" );

            AstroTrainBehaviorType trainBehaviorType = AstroTrainBehaviorTypeTable.Instance.GetRandomRowWithIntensity( Context, BaseInfo.Intensity );
            if ( ForceBehaviorTypeByID != -1 )
                trainBehaviorType = AstroTrainBehaviorTypeTable.Instance.GetRowById( ForceBehaviorTypeByID );
            if ( trainBehaviorType == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: No available entry in AstroTrainBehaviorTypeTable", Verbosity.DoNotShow );
            }
            else
            {
                depotInfo.DepotTrainBehaviorID = trainBehaviorType.id;
                depotInfo.data = trainBehaviorType;
                depotInfo.TrainsSpawned = 0;
            }

            //we should do this in HERE, so that it happens consistently, unlike before
            this.BaseInfo.TimeLastDepotSpawned = World_AIW2.Instance.GameSecond;

            //This is a DoubleBufferedConcurrentList thing.  It is threadsafe
            this.BaseInfo.TrainDepots.AddToDisplayList( newDepot );

            return newDepot;
        }
        #endregion

        #region GetRandomPlanetForDepot
        private Planet GetRandomPlanetForDepot( ArcenHostOnlySimContext Context )
        {
            //current restrictions: Make sure we don't spawn on a human planet, a King planet or a planet with a train station or depot already
            //Note that this function is also used to spawn new Stations
            WorkingAllowedSpawnPlanets.Clear();
            List<SafeSquadWrapper> stations = this.BaseInfo.Stations.GetDisplayList();
            List<Planet> planetsWithDepots = this.BaseInfo.PlanetsWithDepots.GetDisplayList();
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                bool skipPlanet = false;
                for ( int j = 0; j < stations.Count; j++ )
                {
                    if ( planet == stations[j].Planet )
                    {
                        skipPlanet = true;
                        break;
                    }
                }
                if ( skipPlanet )
                    continue;
                PlanetFaction controllingFaction = planet.GetPlanetFactionForFaction( planet.GetControllingFaction() );
                if ( controllingFaction.Faction.Type != FactionType.Player )
                {
                    if ( !planet.GetDataByStanceForFaction( AttachedFaction, FactionStance.Hostile ).HasKingUnitPresent &&
                       !planetsWithDepots.Contains( planet ) )
                        WorkingAllowedSpawnPlanets.Add( planet );
                }
            }
            if(WorkingAllowedSpawnPlanets.Count == 0)
                return null;

            return WorkingAllowedSpawnPlanets[Context.RandomToUse.Next( 0, WorkingAllowedSpawnPlanets.Count )];
        }
        #endregion

        private Planet GetRandomPlanetNonKingNonDepotPlanet( ArcenHostOnlySimContext Context, bool requireDefenses)
        {
            //current restrictions: Make sure we don't spawn on a human planet, a King planet or a planet with a train station already
            WorkingAllowedSpawnPlanets.Clear();
            List<Planet> planetsWithDepots = this.BaseInfo.PlanetsWithDepots.GetDisplayList();
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.GetControllingOrInfluencingFaction().GetIsHostileTowards(AttachedFaction) )
                    continue;
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction(AttachedFaction);
                if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength > pFaction.DataByStance[FactionStance.Friendly].TotalStrength )
                    continue;
                if ( requireDefenses && pFaction.DataByStance[FactionStance.Friendly].TotalStrength < 10 * 1000 )
                    continue;
                if ( planet.GetDataByStanceForFaction( AttachedFaction, FactionStance.Hostile ).HasKingUnitPresent ||
                       planetsWithDepots.Contains( planet ) )
                    continue;
                WorkingAllowedSpawnPlanets.Add( planet );
            }
            if(WorkingAllowedSpawnPlanets.Count == 0)
                return null;
            return WorkingAllowedSpawnPlanets[Context.RandomToUse.Next( 0, WorkingAllowedSpawnPlanets.Count )];
        }

        private void SpawnNewUnitFromDepot( ArcenHostOnlySimContext Context, string Tag, Planet targetPlanet)
        {
            bool debug = false;
            if ( debug ) { }
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, Tag );
            if(targetPlanet == null)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("BUG: SpawnNewUnitFromDepot: null planet passed in", Verbosity.DoNotShow );
                return;
            }

            if(entityData == null)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Unable to find an entity with tag " + Tag + " to spawn on " + targetPlanet.Name, Verbosity.DoNotShow );
                return;
            }

            ArcenPoint spawnLocation = targetPlanet.GetSafePlacementPointAroundPlanetCenter(Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 400 ) );
            PlanetFaction pFaction = targetPlanet.GetPlanetFactionForFaction( AttachedFaction );
            GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                                        pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "AstroTrainSFromDepot" );

            if ( ArcenNetworkAuthority.GetIsHostMode() )
            {
                if ( targetPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                {
                    SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( entity );

                    World_AIW2.Instance.QueueChatMessageOrCommand( entityData.InternalName + " spawning on  " + targetPlanet.Name, 
                        ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                }
            }
        }
        
        private void SpawnNewStation( ArcenHostOnlySimContext Context )
        {
            bool debug = false;
            Planet targetPlanet = GetRandomPlanetNonKingNonDepotPlanet( Context, false);
            if(targetPlanet == null)
                return;
            if(debug)
                ArcenDebugging.ArcenDebugLogSingleLine("Astro Train Station spawning on  " + targetPlanet.Name, Verbosity.DoNotShow );

            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "AstroTrainStation" );
            ArcenPoint spawnLocation = targetPlanet.GetSafePlacementPointAroundPlanetCenter(Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 400 ) );
            PlanetFaction pFaction = targetPlanet.GetPlanetFactionForFaction( AttachedFaction );
            GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                                        pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "AstroTrainStation" );

            if ( ArcenNetworkAuthority.GetIsHostMode() )
            {
                if ( targetPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                {
                    SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( entity );

                    World_AIW2.Instance.QueueChatMessageOrCommand( "Astro Train Station spawning on  " + targetPlanet.Name, ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                }
            }

        }
        private GameEntity_Squad SpawnNewTrain( ArcenHostOnlySimContext Context, Planet SpawnFarFromThisPlanet)
        {
            //the planet is passed in to make sure we place the unit some distance from the destination
            Planet targetPlanet = GetRandomPlanetNonKingNonDepotPlanet( Context, false);
            if(targetPlanet == null)
                return null;

            //If enough trains are killed then they start getting harder
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, BaseInfo.TrainToSpawnTag );
            byte markLevel = (byte)(BaseInfo.TrainSpawnCounterForMarkLevel / 8); //every few trains we go up one mark level. This resets when we upgrade to the next tag
            if ( markLevel < 1 )
                markLevel = 1;
            if ( markLevel > 7 )
                markLevel = 7;
            if ( markLevel > BaseInfo.Intensity )
                markLevel = (byte)BaseInfo.Intensity;
            if ( entityData == null )
                throw new Exception( "Could not find astro train definition" );

            ArcenPoint spawnLocation = targetPlanet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 300 ) );
            PlanetFaction pFaction = targetPlanet.GetPlanetFactionForFaction( AttachedFaction );
            GameEntity_Squad newTrain = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, markLevel,
                pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "AstroTrainNewTrain" );
            if ( newTrain == null )
                return null;

            if ( ArcenNetworkAuthority.GetIsHostMode() )
            {
                StringBuilder trainSpawn = new StringBuilder();
                trainSpawn.Append( "<color=#" ).Append( AttachedFaction.FactionCenterColor.ColorHexBrighter ).Append( ">Astro Train</color>" );
                if ( targetPlanet.IntelLevel > PlanetIntelLevel.Unexplored && SpawnFarFromThisPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                {
                    SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( newTrain );

                    World_AIW2.Instance.QueueChatMessageOrCommand( trainSpawn.ToString() + " spawning on  " + targetPlanet.Name + " and traveling to a depot on <color=#dd44dd>" + SpawnFarFromThisPlanet.Name + "</color>",
                        ChatType.LogToCentralChat, "ArkChiefOfStaff_AstroTrainSpawned", chatHandlerOrNull );
                }
                else if ( targetPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                {
                    SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( newTrain );

                    World_AIW2.Instance.QueueChatMessageOrCommand( trainSpawn.ToString() + " spawning on  " + targetPlanet.Name + " and traveling to a depot somewhere in the galaxy",
                        ChatType.LogToCentralChat, "ArkChiefOfStaff_AstroTrainSpawned", chatHandlerOrNull );
                }
                else if ( SpawnFarFromThisPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                {
                    PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.PlanetToView = SpawnFarFromThisPlanet;

                    World_AIW2.Instance.QueueChatMessageOrCommand( trainSpawn.ToString() + " spawning somewhere in the galaxy and travelling to a depot on " + SpawnFarFromThisPlanet.Name,
                        ChatType.LogToCentralChat, "ArkChiefOfStaff_AstroTrainSpawned", chatHandlerOrNull );
                }
                else
                    World_AIW2.Instance.QueueChatMessageOrCommand( trainSpawn.ToString() + " spawning somewhere in the galaxy and travelling to a depot somewhere in the galaxy",
                        ChatType.LogToCentralChat, "ArkChiefOfStaff_AstroTrainSpawned", null );
            }

            AstroTrainsPerTrainBaseInfo data = newTrain.CreateExternalBaseInfo<AstroTrainsPerTrainBaseInfo>( "AstroTrainsPerTrainBaseInfo" );
            newTrain.ShouldNotBeConsideredAsThreatToHumanTeam = true; //trains can become very powerful (100 base strength + 100 guard strength has been seen, and on high intensities there might be 4 trains at once). This can massively throw off the threat calculations 
            int baseGuardIncome = 500 * BaseInfo.Intensity;
            int guardIncome = baseGuardIncome + ( BaseInfo.TotalTrainsKilled * baseGuardIncome ) / 4;
            UpdateTrainGuards(newTrain, data, guardIncome, Context);
            BaseInfo.TotalTrainsSpawned++;
            BaseInfo.TrainSpawnCounterForMarkLevel++;

            //This is a DoubleBufferedConcurrentList thing.  It is threadsafe
            this.BaseInfo.TrainsEnRoute.AddToDisplayList( newTrain );

            return newTrain;
        }
        private void UpdateTrainGuards(GameEntity_Squad train, AstroTrainsPerTrainBaseInfo trainInfo, int income, ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.AstroTrains );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AstroTrains-UpdateTrainGuards-trace", 10f ) : null;
            if ( income > 0 && tracing )
                tracingBuffer.Add("Reinforcing " + train.ToStringWithPlanet() + " with " + income + "'s worth of guards");
            int shipsBought = 0;
            while ( income > 0 )
            {
                GameEntityTypeData typedata = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "AstroTrainGuard");
                int currentCount = 0;
                trainInfo.StoredGuardsInsideThisTrain.TryGetValue( typedata, out currentCount );
                trainInfo.StoredGuardsInsideThisTrain[typedata] = currentCount + 1;

                income -= typedata.CostForAIToPurchase;
                shipsBought++;
            }
            if ( income > 0 && tracing )
                tracingBuffer.Add("\t" + shipsBought + " guards purchased\n");
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }

        public override void UpdatePlanetInfluence_HostOnly( ArcenHostOnlySimContext Context )
        {
            //Set the appropriate influence here
            List<Planet> planetsInfluenced = Planet.GetTemporaryPlanetList( "AstroTrains-UpdatePlanetInfluence_HostOnly-planetsInfluenced", 10f );
            if ( planetsInfluenced == null ) //blocked for teardown/shutdown; bail
                return;

            List<Planet> planetsWithDepots = this.BaseInfo.PlanetsWithDepots.GetDisplayList();
            for (int j = 0; j < planetsWithDepots.Count; j++)
            {
                planetsInfluenced.AddIfNotAlreadyIn( planetsWithDepots[j] );
            }

            AttachedFaction.SetInfluenceForPlanetsToList( planetsInfluenced );
            Planet.ReleaseTemporaryPlanetList( planetsInfluenced );
        }

        //pass in the AstroTrainsPerDepotBaseInfo because we may need to modify fields in it, then save it in the PerSecond functino
        public void DoDepotEvent( ArcenHostOnlySimContext Context, GameEntity_Squad depot, AstroTrainsPerDepotBaseInfo depotData)
        {
            int debugCode = 0;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.AstroTrains );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AstroTrains-DoDepotEvent-trace", 10f ) : null;
            try {
                bool hasHandledEffect = false;
                Planet targetPlanet = null;
                FInt difficultyMultiplier;
                if(this.DifficultyOfStrongestAIFaction <= 3)
                    difficultyMultiplier = depotData.data.EffectMultiplierLowDifficulty;
                else if(this.DifficultyOfStrongestAIFaction <= 7)
                    difficultyMultiplier = depotData.data.EffectMultiplierMediumDifficulty;
                else
                    difficultyMultiplier = depotData.data.EffectMultiplierHighDifficulty;

                if(difficultyMultiplier == FInt.Zero)
                    difficultyMultiplier = FInt.One; //if the multiplier is not set at all, do nothing
                debugCode = 100;
                if(depotData.data.EffectType == "SpawnUnits")
                {
                    debugCode = 101;
                    hasHandledEffect = true;
                    if(depotData.data.SpawnOnLocalPlanet)
                    {
                        if(tracing) tracingBuffer.Add("For Depot on " + depot.GetPlanetName_Safe() + " spawn unit on this planet");
                        targetPlanet = depot.Planet;
                    }
                    else
                    {
                        targetPlanet = GetRandomPlanetNonKingNonDepotPlanet( Context, false);
                        if ( tracing )tracingBuffer.Add("For Depot on " + depot.GetPlanetName_Safe() + " spawn unit remote planet " + targetPlanet.Name + "\n");
                    }
                    Faction targetFaction = targetFactionForDepotEvent( Context, depotData.data.DestinationFaction);
                    if(targetFaction == null)
                    {
                        if ( !factionsWeHaveAlreadyComplainedAboutNotFinding.ContainsKey( depotData.data.DestinationFaction ) )
                        {
                            factionsWeHaveAlreadyComplainedAboutNotFinding[depotData.data.DestinationFaction] = true;
                            ArcenDebugging.ArcenDebugLogSingleLine( "Astro Train Depot Event: Could not find faction for " + depotData.data.DestinationFaction, Verbosity.ShowAsError );
                        }
                        return;
                    }
                    //Pull a new GameEntity each time in case there are multiple entities with that tag
                    PlanetFaction pFaction = targetPlanet.GetPlanetFactionForFaction( targetFaction );
                    debugCode = 102;
                    for(int i = 0; i < depotData.data.NumUnitsToSpawn; i++)
                    {
                        debugCode = 110;
                        GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, depotData.data.TagsToSpawn );
                        debugCode = 111;
                        if(entityData == null)
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine("BUG: Astro Trains depot " + depotData.data.name + " could not find any units with the tag " + depotData.data.TagsToSpawn, Verbosity.DoNotShow );
                            return;
                        }
                        debugCode = 112;
                        if(tracing) tracingBuffer.Add("For Depot on " + depot.GetPlanetName_Safe() + " spawning " + entityData.GetDisplayName() + " " + i + " of " + depotData.data.NumUnitsToSpawn );
                        debugCode = 113;
                        ArcenPoint spawnLocation = targetPlanet.GetSafePlacementPointAroundPlanetCenter(Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 300 ) );
                        debugCode = 114;
                        /*GameEntity entity = */GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                                    pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "AstroTrainDepotEvent" );
                    }
                    debugCode = 115;
                    FInt AIP = FactionUtilityMethods.Instance.GetCurrentAIP();
                    FInt amountToSpawn = depotData.data.StrengthAssociated + depotData.data.BonusStrengthPerAIP*AIP; //temp variable because I use it for the while loop
                    debugCode = 116;
                    amountToSpawn *= difficultyMultiplier;
                    debugCode = 117;
                    while(amountToSpawn > 0)
                    {
                        debugCode = 120;
                        GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, depotData.data.TagsToSpawn );
                        if(entityData == null)
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine("BUG: Astro Trains depot " + depotData.data.name + " could not find any units with the tag " + depotData.data.TagsToSpawn, Verbosity.DoNotShow );
                            return;
                        }

                        ArcenPoint spawnLocation = targetPlanet.GetSafePlacementPointAroundPlanetCenter(Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 300 ) );
                        /*GameEntity entity = */GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                                    pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "AstroTrainDepotEvent" );
                        if(tracing) tracingBuffer.Add("For Depot on " + depot.GetPlanetName_Safe() + " spawning " + entityData.GetDisplayName() +" with strength " + entityData.MarkStatsFor( pFaction.Faction.CurrentGeneralMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership + " out of a strength of " + amountToSpawn );

                        amountToSpawn -= entityData.MarkStatsFor( pFaction.Faction.CurrentGeneralMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership;
                    }
                }
                if(depotData.data.EffectType == "SpawnAIWave")
                {
                    debugCode = 130;
                    hasHandledEffect = true;
                    Faction AIFaction = World_AIW2.GetRandomAIFaction(Context);
                    AISentinelsFactionDeepInfo aiDeepInfo = AIFaction.GetAISentinelsDeepLogic();
                    
                    FInt StrengthAssociated = depotData.data.StrengthAssociated + depotData.data.BonusStrengthPerAIP*(GlobalAIWorldBaseInfo.Instance.AIProgress_Effective ); //temp variable because I use it for the while loop
                    StrengthAssociated *= difficultyMultiplier;

                    PlannedWaveOptions options = PlannedWaveOptions.CreateWithDefaults();
                    PlannedWave wave = aiDeepInfo.BaseInfo.PlanWave_OrGetNull( Context, StrengthAssociated.IntValue, options );
                    options.ReturnToPool();

                    if (wave != null)
                    {
                        wave.IsAstroTrainWave = true;
                        aiDeepInfo.BaseInfo.WaveList.Add(wave);
                    }
                }
                if(depotData.data.EffectType == "BudgetBoost")
                {
                    debugCode = 140;
                    hasHandledEffect = true;
                    if(tracing)
                        tracingBuffer.Add("Depot " + depotData.data.name + " on " + depot.GetPlanetName_Safe() + " buffing budget for " + depotData.data.DestinationFaction + " " + depotData.data.BudgetToBoost + " by " + depotData.data.StrengthAssociated );
                    Faction targetFaction = targetFactionForDepotEvent( Context, depotData.data.DestinationFaction);
                    AISentinelsCoreData factionExternalOrNull = targetFaction.TryGetAISentinelsCoreData()?.SentinelInfo;
                    if ( factionExternalOrNull == null )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Error ib BudgetBoost Astro Train: could not find GetSentinelsExternal factionExternalOrNull for " + depotData.data.DestinationFaction, Verbosity.ShowAsError );
                    FInt StrengthAssociated = depotData.data.StrengthAssociated + depotData.data.BonusStrengthPerAIP* (factionExternalOrNull == null ? FInt.FromParts( 100, 0 ) : GlobalAIWorldBaseInfo.Instance.AIProgress_Effective ); //temp variable because I use it for the while loop
                    StrengthAssociated *= difficultyMultiplier;
                    if(targetFaction == null)
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine("Astro Train Budget Boost: Could not find faction for " + depotData.data.DestinationFaction, Verbosity.DoNotShow );
                        return;
                    }
                    if(depotData.data.BudgetToBoost == "CPA")
                    {
                        if ( factionExternalOrNull != null )
                            factionExternalOrNull.StoredAIPurchaseCostByBudget[AIBudgetType.CPA] += StrengthAssociated;
                    }
                    else if(depotData.data.BudgetToBoost == "Wave")
                    {
                        if ( factionExternalOrNull != null )
                            factionExternalOrNull.StoredAIPurchaseCostByBudget[AIBudgetType.Wave] += StrengthAssociated;
                    }
                    else if(depotData.data.BudgetToBoost == "Reinforcement")
                    {
                        if ( factionExternalOrNull != null )
                            factionExternalOrNull.StoredAIPurchaseCostByBudget[AIBudgetType.Reinforcement] += StrengthAssociated;
                    }
                    else if(depotData.data.BudgetToBoost == "HunterFleet")
                    {
                        if ( factionExternalOrNull != null )
                            factionExternalOrNull.StoredAIPurchaseCostByBudget[AIBudgetType.HunterFleet] += StrengthAssociated;
                    }
                    else if ( depotData.data.BudgetToBoost == "WormholeInvasion")
                    {
                        if ( factionExternalOrNull != null )
                            factionExternalOrNull.StoredAIPurchaseCostByBudget[AIBudgetType.WormholeInvasion] += StrengthAssociated;
                    }
                    else
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine("Depot " + depotData.data.name + " has undefined associated budget boost " + depotData.data.BudgetToBoost, Verbosity.DoNotShow );
                    }
                }
                debugCode = 150;
                if(!hasHandledEffect)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("BUG: No C# definition for how to handle a " + depotData.data.name + " event", Verbosity.DoNotShow );
                }       
                //reset the counter "how many trains have arrived" counter
                depotData.TrainsThatArrivedSafely = 0;
            } 
            catch (Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "AstroTrains DoDepotEvent Hit exception at code " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            finally
            {
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
            }
        }

        private ConcurrentDictionary<string, bool> factionsWeHaveAlreadyComplainedAboutNotFinding = ConcurrentDictionary<string, bool>.Create_WillNeverBeGCed( Engine_Universal.DEFAULT_CONCURRENCY_LEVEL, 900, "AstroTrainsFactionDeepInfo-factionsWeHaveAlreadyComplainedAboutNotFinding" );

        public Faction targetFactionForDepotEvent( ArcenHostOnlySimContext Context, string factionName)
        {
            switch (factionName)
            {
                case "WardenFleet":
                    factionName = "AIWarden";
                    break;
            }
            bool debug = false;
            if(debug)
                ArcenDebugging.ArcenDebugLogSingleLine("getting the Faction object for " + factionName, Verbosity.DoNotShow );
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                //note I probably want to get a list of possible factions if we have multiple AIs or warden fleets
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if(otherFaction.SpecialFactionData.InternalName == factionName)
                {
                    if(debug)
                        ArcenDebugging.ArcenDebugLogSingleLine("found Faction " + i + " to be an instance of " + factionName, Verbosity.DoNotShow );
                    return otherFaction;
                }
                if(debug)
                    ArcenDebugging.ArcenDebugLogSingleLine("Faction " + otherFaction.SpecialFactionData.InternalName + " is not " + factionName, Verbosity.DoNotShow );
            }
            return null;
        }

        #region GetNextTrainDestination
        //either go to the next Station or to the Depot
        public GameEntity_Squad GetNextTrainDestination( ArcenHostOnlySimContext Context, GameEntity_Squad train, AstroTrainsPerTrainBaseInfo trainData )
        {
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.AstroTrains );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AstroTrains-GetNextTrainDestination-trace", 10f ) : null;
            if ( tracing )
                tracingBuffer.Add( "\tFinding a new destination for " + train.ToStringWithPlanet() ).Add( "\n" );
            
            //picks a random planet with a Station to go to
            List<SafeSquadWrapper> stations = this.BaseInfo.Stations.GetDisplayList();
            if ( trainData.stationsRemainingBeforeDepot == 0 || 
                 stations.Count == 0 )
            {
                trainData.stationsRemainingBeforeDepot = 0;
                
                GameEntity_Squad resultingDepot = null;
                foreach ( GameEntity_Squad depot in this.BaseInfo.TrainDepots.DisplaySquads() )
                {
                    if ( depot.Planet.Index == trainData.TargetDepotPlanetID )
                    {
                        resultingDepot = depot;
                        break;
                    }
                }
                
                if ( resultingDepot == null )
                    ArcenDebugging.ArcenDebugLogSingleLine( "BUG: train on " + train.Planet + " could not go to the depot. Did the depot die?", Verbosity.DoNotShow );
                
                return resultingDepot;
            }
            
            GameEntity_Squad target = null;
            if ( stations.Count == 1 )
            {
                target = stations[0].GetSquad();
                return target;
            }
            int retries = 10;
            if ( trainData.hasGoneNearPlayer )
            {
                //if we have already gone near the player, just pick one at random
                if ( tracing )
                    tracingBuffer.Add( "\t\tFinding Dest path A" ).Add( "\n" );

                do
                {
                    target = stations[Context.RandomToUse.Next( 0, stations.Count )].GetSquad();
                } while ( target == null || target.Planet == train.Planet && retries-- > 0 );
            }
            else if ( trainData.stationsRemainingBeforeDepot == 1 )
            {
                trainData.hasGoneNearPlayer = true;
                if ( tracing )
                    tracingBuffer.Add( "\t\tFinding Dest path B" ).Add( "\n" );

                do
                {
                    target = GetStationNearHumansIfPossible( Context, train );
                } while ( target == null || target.Planet == train.Planet && retries-- > 0 );
            }
            else
            {
                if ( tracing )
                    tracingBuffer.Add( "\t\tFinding Dest path C" ).Add( "\n" );

                do
                {
                    if ( Context.RandomToUse.Next( 0, 10 ) % 2 == 0 )
                        target = stations[Context.RandomToUse.Next( 0, stations.Count )].GetSquad();
                    else
                    {
                        trainData.hasGoneNearPlayer = true;
                        target = GetStationNearHumansIfPossible( Context, train );
                    }
                } while ( target == null || target.Planet == train.Planet && retries-- > 0 );
            }

            if ( target == null && stations.Count > 0 )
            {
                //if we failed to find anything good, just pick something at random
                target = stations[Context.RandomToUse.Next( 0, stations.Count )].GetSquad();
            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            return target;
        }
        #endregion end GetNextTrainDestination

        #region GetStationNearHumansIfPossible
        public readonly List<SafeSquadWrapper> workingStations = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "AstroTrainsFactionDeepInfo-workingStations" );
        public GameEntity_Squad GetStationNearHumansIfPossible( ArcenHostOnlySimContext Context, GameEntity_Squad train )
        {
            bool debug = false;
            workingStations.Clear();
            Int16 minHopsFromHumansToCount = 3;
            List<SafeSquadWrapper> stations = this.BaseInfo.Stations.GetDisplayList();
            for (int i = 0; i < stations.Count; i++)
            {
                GameEntity_Squad station = stations[i].GetSquad();
                if(station == null)
                    continue;
                foreach ( Planet.PlanetAtHopDistance _phd in station.Planet.PlanetsWithinXHops_NoFilters( minHopsFromHumansToCount ) )
                {
                    Planet planet = _phd.Planet;
                    PlanetFaction controllingFaction = planet.GetPlanetFactionForFaction( planet.GetControllingFaction() );
                    if(controllingFaction.Faction.Type == FactionType.Player)
                    {
                        workingStations.Add(station);
                        break;
                    }
                }

            }
            if(workingStations.Count == 0)
            {
                if(debug)
                    ArcenDebugging.ArcenDebugLogSingleLine("no stations close enough to humans, pick a random station", Verbosity.DoNotShow );
                return stations[Context.RandomToUse.Next( 0, stations.Count )].GetSquad();
            }
            if(debug)
                ArcenDebugging.ArcenDebugLogSingleLine("return one of " + workingStations.Count + " stations", Verbosity.DoNotShow );
            return workingStations[Context.RandomToUse.Next( 0, workingStations.Count )].GetSquad();
        }
        #endregion

        #region TrainsEnRouteToDepot
        //returns how many trains en route to the depot
        private int TrainsEnRouteToDepot( GameEntity_Squad depot )
        {
            if ( depot == null )
                return 0;
            int enRoute = 0;
            foreach ( GameEntity_Squad train in this.BaseInfo.TrainsEnRoute.DisplaySquads() )
            {
                AstroTrainsPerTrainBaseInfo data = train.CreateExternalBaseInfo<AstroTrainsPerTrainBaseInfo>( "AstroTrainsPerTrainBaseInfo" );
                if ( data.TargetDepotPlanetID == depot.Planet.Index )
                    enRoute++;
            }
            return enRoute;
        }
        #endregion

        private bool shouldDepotFire( GameEntity_Squad depot, AstroTrainsPerDepotBaseInfo depotData )
        {
            //Check if there are any trains en route to this depot
            int trainsStillHeadingToDepot = TrainsEnRouteToDepot( depot );
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.AstroTrains );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AstroTrains-shouldDepotFire-trace", 10f ) : null;


            if ( ((depotData.data.TrainsToSendBeforeFiring >= depotData.TrainsSpawned) && trainsStillHeadingToDepot == 0) )
            {
                //if all the trains we are going to spawn have been spawned and either arrived or died
                if ( tracing ) 
                {
                    tracingBuffer.Add( "depot " + depotData.ToString() + " on " + depot.GetPlanetName_Safe() + " choosing to fire path A\n" );
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return true;
            }
            if ( depotData.data.TrainsNeededBeforeFiring != -1 &&
               (depotData.TrainsThatArrivedSafely >= depotData.data.TrainsNeededBeforeFiring) )
            {
                if ( tracing ) tracingBuffer.Add( "depot " + depotData.ToString() + " on " + depot.GetPlanetName_Safe() + " choosing to fire path B\n" );

                //We needed a specific number of trains to arrive, and we now have enough
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return true;
            }
            if ( depotData.data.FiresOnEveryTrain && depotData.TrainsThatArrivedSafely >= 1 )
            {
                if ( tracing ) tracingBuffer.Add( "depot " + depotData.ToString() + " on " + depot.GetPlanetName_Safe() + " choosing to fire path C\n" );

                //we fire on every train arrival, and a train just arrived
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return true;
            }
//          if(tracing) tracingBuffer.Add("depot " + depotData.ToString() + " on " + depot.GetPlanetName_Safe() + " Not Firing" );
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            return false;
        }

        public override void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context)
        {
            if ( entity == null )
                return;

            int debugStage = 0;
            try
            {
                debugStage = 100;
                try
                {
                    if ( entity.TypeData.GetHasTag( "AstroTrain" ) )
                    {
                        BaseInfo.TotalTrainsKilled++;
                        if ( FiringSystemOrNull != null // the game knows who killed it
                             && FiringSystemOrNull.ParentEntity.GetFactionTypeSafe() == FactionType.Player )
                        {
                            BaseInfo.TotalTrainsKilledByPlayer++;
                        }
                    }
                }
                catch { } //we don't actually care if this errors, the count can be off by a bit and its fine
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in SpecialFaction_AstroTrains.DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly stage " + debugStage + "\n" + e, Verbosity.ShowAsError );
            }
        }
    }
}
