using Arcen.Universal;
using System;

using System.Threading;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public class EntitySimLogicImplementation_DeepInfo : EntitySimLogicDeepInfo
    {
        //Set immediately before PlanetsNearNomads.Sort(...) so the comparison can be a non-capturing
        //static delegate.  [ThreadStatic] for safety since this is sim-context code.
        [ThreadStatic] private static Planet cb_esldNomadSortPlanet;
        public override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            //these two don't matter much
            PlanetsNearNomads.Clear();
            //this may matter for data set inside
            HomeworldPlacer = null;
            NomadPlacers.Clear();
            NomadPlanetList.Clear();
            unconnectedPlanets.Clear();
        }

        public EntitySimLogicImplementation_DeepInfo()
        {
            EntitySimLogicDeepInfo.Instance = this;
        }

        public override void ReinitializeDuringStartOrLoad()
        {
            ArcenLongTermIntermittentPlanningContext.ReinitializeDuringStartOrLoad();
            ArcenLongTermContinuousPlanningContextManager.CleanupAll();
        }

        public override void LongTermIntermittent_RunAllContexts()
        {
            ArcenLongTermIntermittentPlanningContext.RunAllContexts();
        }
        public override void LongTermIntermittent_HandleFactionLogicWhilePaused()
        {
            //avoid warnings about faction threads when paused
            try
            {
                foreach ( Faction fac in World_AIW2.Instance.Factions )
                {
                    //mark as effectively run now, to not trigger warnings
                    fac.LastDoLongRangePlanningEndedAtUnpausedTimeSinceLastRestart_NonSim = ArcenTime.NonSetupNetworkReadyGameTimeSinceLastLoadOrStartF;
                    //but at the same time, be sure to make it clear why it's skipped
                    System.Threading.Interlocked.Exchange( ref fac.LastDoLongRangePlanningReason, "Skip - Does Not Run While Game Paused " +
                        (fac.LastIntermittentLongRangePlanningStartedTime_Nonsim <= 0 ? "- Has Not Run Yet" :
                        (ArcenTime.TimeSinceStartF - fac.LastIntermittentLongRangePlanningStartedTime_Nonsim).ToString( "0.0" ) + "s game time since run") );

                    SpecialFactionPlanning planningContext = (SpecialFactionPlanning)fac.LongRangePlanningContext;
                }
            }
            catch { } //this happens when exiting at just the wrong time
        }

        #region DoWorldStepLogic_HostOnly_FromSimBGThread
        public override void DoWorldStepLogic_HostOnly_FromSimBGThread( ArcenHostOnlySimContext Context )
        {
            if ( Context == null )
                return; //client
            if ( CentralVars.DEBUG_TURN_OFF_WORLD_STEP_LOGIC )
                return;
            if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                return;
            if ( World_AIW2.Instance == null )
                return;

            if ( World_AIW2.Instance.InSetupPhase )
            {
                //if it's the setup phase, what to do?
            }
            else
            {
                DoPlanetMovementAndDestructionLogic( Context );
            }
        }
        #endregion

        public override void ArcenLongTermContinuousPlanningContext_RunAllContexts_ForUnpausedOnly()
        {
            ArcenLongTermContinuousPlanningContextManager.RunAllContexts_ForUnpausedOnly();
        }

        public void DoPlanetMovementAndDestructionLogic( ArcenHostOnlySimContext Context )
        {
            #region Planet Movement and Destruction
            //Note that this must be done here so it won't race with any other threads
            UpdateNomadPlanets( Context );
            DestroyPlanetsIfNecessary( Context );
            #endregion
        }

        #region Nomad Planets
        public static readonly List<Planet> PlanetsNearNomads = List<Planet>.Create_WillNeverBeGCed( 500, "EntitySimLogicImplementation_DeepInfo-PlanetsNearNomads" );
        public static IWormholePlacer HomeworldPlacer = null;
        public static readonly List<IWormholePlacer> NomadPlacers = List<IWormholePlacer>.Create_WillNeverBeGCed( 5, "EntitySimLogicImplementation_DeepInfo-NomadPlacers" );
        public static readonly List<Planet> NomadPlanetList = List<Planet>.Create_WillNeverBeGCed( 500, "EntitySimLogicImplementation_DeepInfo-NomadPlanetList" );
        private static float lastTimeRequestedNomadFaction = 0;
        public void UpdateNomadPlanets( ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return;

            //Note code needs to run here  because Galaxy Links will change, which will break things if it's run as a sim stage 3
            int debugCode = 0;
            try
            {
                if ( World_AIW2.Instance.IsOutsideOfNormalGameplay )
                    return;
                NomadPlanetList.Clear();
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( planet.TypeData.Type == PlanetType.Nomad )
                        NomadPlanetList.Add( planet );
                }

                if ( HomeworldPlacer == null || NomadPlacers == null )
                {
                    debugCode = 100;
                    //just copied from MapGeneration.cs
                    if ( HomeworldPlacer == null )
                        HomeworldPlacer = new WormholePlacer_Default( FInt.FromParts( 0, 750 ), FInt.FromParts( 0, 750 ) );
                    if ( NomadPlacers.Count == 0 )
                    {                        
                        NomadPlacers.Add( new WormholePlacer_Default( FInt.FromParts( 0, 400 ), FInt.FromParts( 0, 400 ) ) );
                        NomadPlacers.Add( new WormholePlacer_Default( FInt.FromParts( 0, 400 ), FInt.FromParts( 0, 750 ) ) );
                        NomadPlacers.Add( new WormholePlacer_Default( FInt.FromParts( 0, 400 ), FInt.FromParts( 0, 600 ) ) );
                    }
                }

                bool nomadHasMoved = false;
                if ( NomadPlanetList.Count > 0 )
                {
                    Faction nomadFaction = FactionUtilityMethods.Instance.GetNomadPlanetFaction();
                    if ( nomadFaction == null && lastTimeRequestedNomadFaction < ArcenTime.TimeSinceStartF - 5f ) //don't request more frequently than every 5 seconds.  This has to happen by game command, but we really want it to happen once...
                    {
                        lastTimeRequestedNomadFaction = ArcenTime.TimeSinceStartF;

                        GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.BelatedlyCreateFaction], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                        command.RelatedString = "NomadPlanets";
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                    }

                    debugCode = 200;
                    for ( int i = 0; i < NomadPlanetList.Count; i++ )
                    {
                        if ( nomadFaction == null || 
                            NomadPlanetsFactionBaseInfo.Instance == null ) //this will be true on the first sim second after the new faction is added, so just be patient
                            break;
                        debugCode = 300;
                        Planet planet = NomadPlanetList[i];
                        debugCode = 301;
                        if ( planet.IsDisabledNomad )
                            continue;

                        debugCode = 310;
                        if ( planet.TimeForNextMove == -1 )
                        {
                            debugCode = 320;
                            planet.TimeForNextMove = World_AIW2.Instance.GameSecond + NomadPlanetsFactionBaseInfo.Instance.InitialMoveTime + Context.RandomToUse.Next( 0, NomadPlanetsFactionBaseInfo.Instance.VarianceBetweenMoveTimes );
                            if ( World_AIW2.Instance.CurrentGalaxy.IsNomadGalaxy )
                                planet.TimeForNextMove += 400 + Context.RandomToUse.Next( 0, 800 ); //nomad planets now take a bit longer to start moving

                            debugCode = 340;
                            if ( nomadFaction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) )
                                planet.TimeForNextMove = World_AIW2.Instance.GameSecond + 20;
                        }
                        Planet targetPlanet = World_AIW2.Instance.GetPlanetByIndex( planet.NomadTargetPlanetIdx );
                        //Update this planet if necessary
                        if ( planet.TimeForNextMove <= World_AIW2.Instance.GameSecond || //if it's time to move
                             (targetPlanet != null && planet.SecondsTillNomadCrashes <= 0) ) //or if this planet is crashing now
                        {
                            debugCode = 400;
                            if ( targetPlanet != null )
                            {
                                debugCode = 410;
                                
                                //we are en route to crash into another planet
                                if ( planet.SecondsTillNomadCrashes <= 0 )
                                {
                                    //We have just hit the planet
                                    planet.IsPlanetToBeDestroyed = true;
                                    targetPlanet.IsPlanetToBeDestroyed = true;
                                    
                                    // there isn't a nomad planet flag, and they both destroy planets so ..
                                    Trace.For(ArcenTracingFlags.ZenithMiners)?.Msg("Nomad planet {0} just hit {1}. Marking both for destruction.", planet, targetPlanet);
                                    
                                    debugCode = 420;
                                    continue;
                                }
                            }
                            
                            debugCode = 430;
                            //move this planet in the galaxy
                            ArcenPoint newLocation = FindNextNomadPoint( planet, Context, NomadPlanetList.Count );
                            //now find an actual safe spot
                            planet.GalaxyLocation = newLocation;
                            planet.Network_HostOnly_NeedToSyncWormholesToClients = true;
                            planet.Network_HostOnly_NeedToSyncPlanetPositionToClients = true;
                            //find nearby planets, then update links. Note this isn't the most efficient code, but nomads never link to more than
                            //4 planets, so shouldn't be too bad.
                            int radiusForNearNeighbors = 100;
                            int minPlanetsForNomadConnection = 2;
                            int maxPlanetsForNomadConnection = 4;
                            do
                            {
                                debugCode = 440;
                                PlanetsNearNomads.Clear();
                                foreach ( Planet otherPlanet in World_AIW2.Instance.Planets( false ) )
                                {
                                    debugCode = 450;
                                    if ( otherPlanet == planet )
                                        continue;
                                    if ( otherPlanet.PopulationType == PlanetPopulationType.DarkZenith && otherPlanet.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "DarkZenith" )
                                    {
                                    //this is a DZ planet that warped in; make sure they are ready to link to the galaxy before linking them
                                    DarkZenithFactionBaseInfo dzData = otherPlanet.GetControllingOrInfluencingFaction().GetExternalBaseInfoAs<DarkZenithFactionBaseInfo>();
                                        if ( !dzData.HasLinkedPlanets )
                                            continue;
                                    }
                                    if ( Mat.DistanceBetweenPointsImprecise( planet.GalaxyLocation, otherPlanet.GalaxyLocation ) < radiusForNearNeighbors )
                                        PlanetsNearNomads.Add( otherPlanet );
                                }
                                radiusForNearNeighbors += 100;
                            } while ( PlanetsNearNomads.Count <= minPlanetsForNomadConnection );
                            debugCode = 500;
                            if ( PlanetsNearNomads.Count > maxPlanetsForNomadConnection )
                            {
                                //if we have too many, cull the weak
                                cb_esldNomadSortPlanet = planet;
                                PlanetsNearNomads.Sort( static delegate ( Planet Left, Planet Right )
                                {
                                    int lDistance = Mat.DistanceBetweenPointsImprecise( cb_esldNomadSortPlanet.GalaxyLocation, Left.GalaxyLocation );
                                    int rDistance = Mat.DistanceBetweenPointsImprecise( cb_esldNomadSortPlanet.GalaxyLocation, Left.GalaxyLocation );
                                    return lDistance.CompareTo( rDistance );
                                } );
                                PlanetsNearNomads.RemoveRange( maxPlanetsForNomadConnection - 1, PlanetsNearNomads.Count - maxPlanetsForNomadConnection );
                            }
                            bool planetLinksUpdated = false;
                            debugCode = 600;
                            //remove unecessary old links, then add new links
                            foreach ( Planet otherPlanet in planet.LinkedNeighbors( false ) )
                            {
                                debugCode = 700;
                                if ( !PlanetsNearNomads.Contains( otherPlanet ) )
                                {
                                    debugCode = 710;

                                    planet.Network_HostOnly_NeedToSyncWormholesToClients = true;
                                    planet.Network_HostOnly_NeedToSyncPlanetPositionToClients = true;
                                    otherPlanet.Network_HostOnly_NeedToSyncWormholesToClients = true;
                                    otherPlanet.Network_HostOnly_NeedToSyncPlanetPositionToClients = true;
                                    planet.RemoveLinkTo( otherPlanet );
                                    if ( World_AIW2.Instance.GetIsHostAnyShouldPrepareToSendNewEntitiesToClients() && World_AIW2.Instance.GameSecond > 0 )
                                        World_AIW2.Instance.OnServer_PlanetsToFastBlastToClients.Enqueue( otherPlanet );
                                //also destroy wormhole objects
                                GameEntity_Other myWormhole = otherPlanet.GetWormholeTo( planet );
                                    GameEntity_Other otherWormhole = planet.GetWormholeTo( otherPlanet );
                                    debugCode = 720;
                                //these null checks are defensive code against what seems to be an MP-specific bug where we aren't
                                //creating the right wormholes when a nomad moves
                                //Note that this definitely has been observed with two adjacent nomad planets
                                if ( myWormhole != null )
                                        myWormhole.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.RemoveWormholes );
                                    if ( otherWormhole != null )
                                        otherWormhole.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.RemoveWormholes );
                                    planetLinksUpdated = true;
                                }
                            }
                            debugCode = 800;
                            IWormholePlacer wormholePlacer = NomadPlacers[Context.RandomToUse.Next( 0, NomadPlacers.Count )];
                            for ( int j = 0; j < PlanetsNearNomads.Count; j++ )
                            {
                                debugCode = 900;
                                //set the appropriate links
                                Planet otherPlanet = PlanetsNearNomads[j];
                                if ( !planet.GetIsDirectlyLinkedTo( false, otherPlanet ) )
                                {
                                    debugCode = 1000;
                                    if ( otherPlanet.PopulationType == PlanetPopulationType.HumanHomeworld || otherPlanet.PopulationType == PlanetPopulationType.ArkEmpireHumanHomeworld ||
                                         otherPlanet.PopulationType == PlanetPopulationType.AIHomeworld ) //AIBastionWorld ignore
                                        wormholePlacer = HomeworldPlacer;
                                    LinkPlanetsAndAddWormholes( planet, otherPlanet, wormholePlacer, Context );

                                    planetLinksUpdated = true;
                                }
                            }
                            debugCode = 1200;
                            if ( planetLinksUpdated )
                            {
                                //note this doesn't update "Original Hops to Player/Human Homeworld"; unsure if that should be updated
                                World_AIW2.Instance.CurrentGalaxy.RecomputeLinkedPathfindables();
                            }
                            debugCode = 1300;
                            World_AIW2.Instance.CurrentGalaxy.RecomputePlanetDistances();

                            if ( nomadFaction != null )
                            {
                                //We are a nomad planet happily puttering along
                                NomadPlanetsFactionBaseInfo gData = nomadFaction.TryGetExternalBaseInfoAs<NomadPlanetsFactionBaseInfo>();
                                if ( planet.NomadTargetPlanetIdx != -1 ) //we are en route to crash
                                {
                                    int interval = -1; //a default for when this code runs before the Nomad Sim code that sets the interval
                                    if ( !gData.MoveIntervalForCrash.ContainsKey( planet.Index ) )
                                    {
                                        if ( gData.TimeNomadCrashStarted > 0 )
                                        {
                                            //the move interval is set at the same time the time nomad crash started
                                            throw new Exception( "We don't know the move interval for this nomad planet " + planet.Index );
                                        }
                                        interval = 20; //a default; the Nomad Sim code will correct this later
                                    }
                                    else
                                    {
                                        //we are en route to our target; if we are already really close, don't move any more
                                        int totalDistance = Mat.DistanceBetweenPointsImprecise( planet.GalaxyLocation, targetPlanet.GalaxyLocation );
                                        if ( totalDistance < 40 )
                                            interval = planet.SecondsTillNomadCrashes;
                                        else
                                            interval = gData.MoveIntervalForCrash[planet.Index]; //this is the normal path
                                    }

                                    planet.TimeForNextMove = World_AIW2.Instance.GameSecond + interval + Context.RandomToUse.Next( 0, NomadPlanetsFactionBaseInfo.Instance.VarianceBetweenMoveTimesCrash );
                                }
                                else if ( NomadPlanetsFactionBaseInfo.Instance.BaseMoveTime <= 0 ) //at game start time we haven't loaded the XML constants, so pick something pretty random
                                {
                                    planet.TimeForNextMove = World_AIW2.Instance.GameSecond + 600 + Context.RandomToUse.Next( 0, 200 * 8 );
                                }
                                else //this is the normal case
                                    planet.TimeForNextMove = World_AIW2.Instance.GameSecond + NomadPlanetsFactionBaseInfo.Instance.BaseMoveTime + Context.RandomToUse.Next( 0, NomadPlanetsFactionBaseInfo.Instance.VarianceBetweenMoveTimes );
                            }
                            else
                            {
                                //we are a nomad planet w/o nomad planets enabled; this is probably because a Miner has Nomadified a planet
                                planet.TimeForNextMove = World_AIW2.Instance.GameSecond + 600 + Context.RandomToUse.Next( 0, 200 * 8 );
                            }
                            nomadHasMoved = true; //this gets set on both the Planet and Galaxy objects, for simplicity
                            planet.NomadHasMoved = true;
                        }
                    }
                }
                
                debugCode = 2000;
                if ( World_AIW2.Instance.CurrentGalaxy == null )
                    return; //this seems to be possible immediately after game load?
                
                if ( nomadHasMoved )
                {
                    debugCode = 2100;
                    bool reconnectionNecessary = ReconnectGalaxyIfNecessary( Context );
                    if ( reconnectionNecessary )
                    {
                        World_AIW2.Instance.CurrentGalaxy.RecomputeLinkedPathfindables();
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in UpdateNomadPlanets. debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        private readonly List<Planet> unconnectedPlanets = List<Planet>.Create_WillNeverBeGCed( 30, "EntitySimLogicImplementation_DeepInfo-unconnectedPlanets" );
        private bool ReconnectGalaxyIfNecessary( ArcenHostOnlySimContext Context )
        {
            int debugCode = 0;
            bool wasUnconnected = false;
            try
            {
                //ArcenDebugging.ArcenDebugLogSingleLine("Checking if fully connected ", Verbosity.DoNotShow );
                while ( !World_AIW2.Instance.CurrentGalaxy.IsGalaxyFullyConnected( unconnectedPlanets ) )
                {
                    wasUnconnected = true;
                    debugCode = 410;
                    if ( unconnectedPlanets.Count == 0 )
                        throw new Exception( "The galaxy isn't fully connected, but there are no unconnected planets" );
                    Planet unlinkedPlanet = GetPlanetToLinkOrNull( unconnectedPlanets );
                    if ( unlinkedPlanet == null )
                        break; //our only unlinked planets are DZ waiting to warp in
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Galaxy is not fully connected. First, reconnect " + unlinkedPlanet.Name, Verbosity.DoNotShow );
                    //link this planet to its nearest unlinked neighbor
                    Planet nearestUnlinkedNeighbor = null;
                    int distanceToNeighbor = -1;
                    debugCode = 420;
                    foreach ( Planet potentialLink in World_AIW2.Instance.Planets( false ) )
                    {
                            debugCode = 430;
                            //the planet about to be destroyed is in the ToBeDestroyed state
                            if ( potentialLink.IsPlanetToBeDestroyed || 
                                 potentialLink.HasPlanetBeenDestroyed ) 
                            {
                                continue;
                            }
                            
                            if ( ShouldDeferLinkingPlanets( potentialLink ) )
                                continue;

                            if ( unlinkedPlanet == potentialLink || 
                                 unlinkedPlanet.GetIsDirectlyLinkedTo( false, potentialLink ) ||
                                 unconnectedPlanets.Contains( potentialLink ) )
                            {
                                continue;
                            }
                            
                            debugCode = 440;
                            if ( nearestUnlinkedNeighbor == null ||
                                 distanceToNeighbor > unlinkedPlanet.GetDistanceTo( potentialLink ) )
                            {
                                nearestUnlinkedNeighbor = potentialLink;
                                distanceToNeighbor = unlinkedPlanet.GetDistanceTo( nearestUnlinkedNeighbor );
                                continue;
                            }
                    }
                    
                    ArcenDebugging.ArcenDebugLogSingleLine( "Link and add wormholes between " + unlinkedPlanet.Name + " and " + nearestUnlinkedNeighbor.Name, Verbosity.DoNotShow );
                    
                    debugCode = 500;
                    IWormholePlacer placer = NomadPlacers[Context.RandomToUse.Next( 0, NomadPlacers.Count )];
                    LinkPlanetsAndAddWormholes( unlinkedPlanet, nearestUnlinkedNeighbor, placer, Context );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in ReconnectGalaxyIfNecessary debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            
            return wasUnconnected;
        }
        private static bool ShouldDeferLinkingPlanets( Planet planet )
        {
            Faction f = planet.GetControllingOrInfluencingFaction();
            if ( planet.PopulationType == PlanetPopulationType.DarkZenith &&
                 f.SpecialFactionData.InternalName == "DarkZenith" )
                return !f.GetExternalBaseInfoAs<DarkZenithFactionBaseInfo>().HasLinkedPlanets;
            if ( planet.PopulationType == PlanetPopulationType.Malware )
            {
                // Use TryGet rather than requiring the InternalName match: when Malware planets are
                // first created via QueueGameCommand the controlling faction may not be MalwareForApkallu
                // yet.  In that case (mData == null) we still defer — the planet just appeared and is
                // not ready to be wired into the galaxy.
                MalwareFactionBaseInfo mData = f.TryGetExternalBaseInfoAs<MalwareFactionBaseInfo>();
                return mData == null || !mData.HasLinkedPlanets;
            }
            return false;
        }
        private Planet GetPlanetToLinkOrNull( List<Planet> planets )
        {
            for ( int i = 0; i < planets.Count; i++ )
            {
                Planet planet = planets[i];
                if ( ShouldDeferLinkingPlanets( planet ) )
                    continue;
                return planet;
            }
            return null;
        }
        private void DestroyPlanetsIfNecessary( ArcenHostOnlySimContext Context )
        {
            //This function removes planets from the galaxy (from nomads crashing, from miners, etc...)
            int debugCode = 0;
            try
            {
                debugCode = 100;
                bool anyPlanetsKilled = false;
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    debugCode = 200;
                    if ( planet.HasPlanetBeenDestroyed )
                    {
                        //ArcenDebugging.ArcenDebugLogSingleLine( planet.Name + " has already been destroyed already", Verbosity.DoNotShow );
                        continue; // this planet has already been destroyed
                    }
                    
                    if ( !planet.IsPlanetToBeDestroyed )
                        continue;
                    
                    anyPlanetsKilled = true;
                    debugCode = 210;
                    planet.Network_HostOnly_NeedToSyncWormholesToClients = true;
                    planet.Network_HostOnly_NeedToSyncPlanetPositionToClients = true;

                    planet.DestroyPlanet( Context, true );
                }
                debugCode = 300;
            
                if ( anyPlanetsKilled || World_AIW2.Instance.GameSecond % 60 == 0 )
                {
                    debugCode = 400;
                    bool reconnectionNecessary = ReconnectGalaxyIfNecessary( Context );
                    debugCode = 600;
                    if ( reconnectionNecessary )
                    {
                        World_AIW2.Instance.CurrentGalaxy.RecomputeLinkedPathfindables();
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in DestroyPlanetsIfNecessary debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        private void LinkPlanetsAndAddWormholes( Planet planet, Planet otherPlanet, IWormholePlacer wormholePlacer, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return;
            planet.AddLinkTo( otherPlanet );
            //also add wormhole objects

            int largerIndex = Math.Max( planet.Index, otherPlanet.Index );
            int smallerIndex = Math.Min( planet.Index, otherPlanet.Index );
            int seed = (largerIndex << 16) + smallerIndex;
            Context.RandomToUse.ReinitializeWithSeed( seed );

            ArcenPoint wormholePoint = wormholePlacer.GetPointForWormhole( Context, planet, otherPlanet );
            PlanetFaction pFaction = planet.GetFirstFactionOfType( FactionType.NaturalObject );
            GameEntity_Other wormholeOrNull = GameEntity_Other.CreateOtherNew_CallFromHostOnly( pFaction, GameEntityTypeDataTable.Instance.DefaultWormholeType, wormholePoint, Context );
            if ( wormholeOrNull != null )
                wormholeOrNull.SetLinkedPlanetIndex( otherPlanet.Index );
            planet.RecomputeDestinationIndexToWormholeMapping();

            pFaction = otherPlanet.GetFirstFactionOfType( FactionType.NaturalObject );
            ArcenPoint otherWormholePoint = wormholePlacer.GetPointForWormhole( Context, otherPlanet, planet );
            GameEntity_Other otherWormholeOrNull = GameEntity_Other.CreateOtherNew_CallFromHostOnly( pFaction, GameEntityTypeDataTable.Instance.DefaultWormholeType, otherWormholePoint, Context );
            if ( otherWormholeOrNull != null )
                otherWormholeOrNull.SetLinkedPlanetIndex( planet.Index );
            otherPlanet.RecomputeDestinationIndexToWormholeMapping();

            ArcenDebugging.ArcenDebugLogSingleLine("We are moving " + planet.Name, Verbosity.DoNotShow );
            planet.Network_HostOnly_NeedToSyncWormholesToClients = true;
            planet.Network_HostOnly_NeedToSyncPlanetPositionToClients = true;
            otherPlanet.Network_HostOnly_NeedToSyncWormholesToClients = true;
            otherPlanet.Network_HostOnly_NeedToSyncPlanetPositionToClients = true;
            if ( World_AIW2.Instance.GetIsHostAnyShouldPrepareToSendNewEntitiesToClients() && World_AIW2.Instance.GameSecond > 0 )
            {
                World_AIW2.Instance.OnServer_PlanetsToFastBlastToClients.Enqueue( planet );
                World_AIW2.Instance.OnServer_PlanetsToFastBlastToClients.Enqueue( otherPlanet );
            }
        }

        private ArcenPoint FindNextNomadPoint( Planet nomad, ArcenHostOnlySimContext Context, int totalNomads )
        {
            ArcenPoint preferredLocation, output;
            Planet targetPlanet = World_AIW2.Instance.GetPlanetByIndex( nomad.NomadTargetPlanetIdx );
            ArcenPoint previousLocation = nomad.GalaxyLocation;
            AngleDegrees currentAngle = Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter.GetAngleToDegrees( previousLocation );
            int distance = Mat.DistanceBetweenPointsImprecise( nomad.GalaxyLocation, Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter );
            int preferredDistanceFromOtherPlanets = 40;
            int preferredDistanceFromPoint = 10;

            if ( targetPlanet != null )
            {
                //go toward a target and try to crash
                ArcenPoint finalDestination = targetPlanet.GalaxyLocation;
                currentAngle = finalDestination.GetAngleToDegrees( previousLocation );
                int totalDistance = Mat.DistanceBetweenPointsImprecise( previousLocation, finalDestination );
                Faction nomadFaction = FactionUtilityMethods.Instance.GetNomadPlanetFaction();
                int movementDistance = NomadPlanetsFactionBaseInfo.Instance.DistanceToMoveForCrash; //default value; will probably be overridden below
                if ( totalDistance < 40 )
                    movementDistance = 0; //we're already really close; just chill
                else if ( nomadFaction != null )
                {
                    NomadPlanetsFactionBaseInfo gData = nomadFaction.TryGetExternalBaseInfoAs<NomadPlanetsFactionBaseInfo>();
                    if ( gData != null && gData.MoveIntervalForCrash.ContainsKey( nomad.Index ) && nomad.SecondsTillNomadCrashes > 0 )
                    {
                        int interval = gData.MoveIntervalForCrash[nomad.Index];
                        int numHopsLeft = nomad.SecondsTillNomadCrashes / interval;
                        if ( numHopsLeft <= 0 )
                            movementDistance = 0;
                        else
                            movementDistance = totalDistance / numHopsLeft;
                        ArcenDebugging.ArcenDebugLogSingleLine( "Moving a crashing planet; there are " + numHopsLeft + " hops left and we are moving " + movementDistance, Verbosity.DoNotShow );
                    }
                    else
                        movementDistance = 2;
                }
                preferredLocation = previousLocation.GetPointTowardsOther( finalDestination, movementDistance, totalDistance );
                output = BadgerUtilityMethods.GetSafePointNearPoint( nomad, preferredLocation, World_AIW2.Instance.CurrentGalaxy,
                    preferredDistanceFromOtherPlanets, preferredDistanceFromPoint, Context );
                //ArcenDebugging.ArcenDebugLogSingleLine("Crash Path: Moving nomad planet "  + nomad.Name + " units " + movementDistance + "  previous galaxy location was " +nomad.GalaxyLocation.ToString() + ", preferredLocation " + preferredLocation.ToString() + " but actually using " + output.ToString() + ". totalDistance to target " + totalDistance + " and target location " + targetPlanet.GalaxyLocation + ".", Verbosity.DoNotShow );
                return output;
            }

            int attempts = 0;
            int distMoved = 0;
            AngleDegrees change;
            AngleDegrees newAngle;
            int minDistanceForMove = 50; //make sure we move at least a decent amount
            do
            {
                int angleChangePerMove = Context.RandomToUse.Next( 5, 8 );
                angleChangePerMove += attempts;
                if ( totalNomads < 3 )
                    angleChangePerMove += 3;
                else if ( totalNomads < 5 )
                    angleChangePerMove += 2;
                if ( nomad.Index % 2 == 0 ) //even and odd nomads move opposite directions
                    angleChangePerMove *= -1;

                change = AngleDegrees.Create( angleChangePerMove );
                newAngle = currentAngle.Add( change ); //if we were at 180 degrees from galaxy center, go to 180 + angleChangePerMove
                int origDistance = nomad.OriginalNomadDistance;

                //TODO: If we have a rectangular map and wind up too far away from other planets it looks weird. I'm including the original distance we were in case we want to change the distance
                //when on the short side of the rectangle. 
                //int ActualDistanceToMove = 30;
                preferredLocation = Mat.GetPointFromCircleCenter( Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter, distance, newAngle );
                distMoved = Mat.DistanceBetweenPointsImprecise( previousLocation, preferredLocation );
                attempts++;
            } while ( distMoved < minDistanceForMove && attempts < 20 );

            output = BadgerUtilityMethods.GetSafePointNearPoint( nomad, preferredLocation, World_AIW2.Instance.CurrentGalaxy,
                preferredDistanceFromOtherPlanets, preferredDistanceFromPoint, Context.GetHostOnlyContext() );
            //ArcenDebugging.ArcenDebugLogSingleLine("Moving nomad planet " + nomad.Name + " previous galaxy location was " +nomad.GalaxyLocation.ToString() + " and new location is " + output.ToString() + ". old angle: " + currentAngle + " new angle: " + newAngle + " angle change " + change + " distance moved " + distMoved + " attempts " + attempts, Verbosity.DoNotShow );

            return output;
        }
        #endregion
    }
}
