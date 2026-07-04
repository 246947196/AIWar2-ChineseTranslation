using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class LocalPlayerWorldBaseInfo : ExternalWorldBaseInfo
    {
        public readonly ProjectedLocalPlayerMultiPathData NonSim_PlanetsInvolvedInPathToCurrentHoverPlanet_LocalFactionOnly = new ProjectedLocalPlayerMultiPathData();
        public readonly ProjectedLocalPlayerMultiPathData NonSim_PlanetsInvolvedInPathToCurrentHoverPlanet_LocalFactionOnly_AdditiveSelection = new ProjectedLocalPlayerMultiPathData();
        public readonly Dictionary<int, int> NonSim_CostToPlanet = Dictionary<int, int>.Create_WillNeverBeGCed( 500, "LocalPlayerWorldBaseInfo-NonSim_CostToPlanet" );
        public int NonSim_LastHoverIndex_LocalFactionOnly = -1;
        public int NonSim_LastUISecondRefreshedCurrentHoverPath_LocalFactionOnly;
        private readonly Dictionary<int, int> workingCostToPlanet = Dictionary<int, int>.Create_WillNeverBeGCed( 500, "LocalPlayerWorldBaseInfo-workingCostToPlanet" );

        public static LocalPlayerWorldBaseInfo Instance;

        private static ReferenceTracker RefTracker;
        private static readonly List<string> availableTagsScratch = List<string>.Create_WillNeverBeGCed( 4, "LocalPlayerWorldBaseInfo-availableTagsScratch" );
        public LocalPlayerWorldBaseInfo()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "LocalPlayerWorldBaseInfos" );
            RefTracker.IncrementObjectCount();

            Instance = this;
            if ( EntityTypeDrawingBag_DrawLink.Instance == null )//I found no other way to put this in - but it can't be self-contained it seems because then it wouldn't load!
                EntityTypeDrawingBag_DrawLink.Instance = new EntityTypeDrawingBag_DrawLinkImplementation();
        }

        public override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            //probably does not matter
            NonSim_PlanetsInvolvedInPathToCurrentHoverPlanet_LocalFactionOnly.Reset();
            NonSim_PlanetsInvolvedInPathToCurrentHoverPlanet_LocalFactionOnly_AdditiveSelection.Reset();
            NonSim_CostToPlanet.Clear();
            NonSim_LastHoverIndex_LocalFactionOnly = -1;
            NonSim_LastUISecondRefreshedCurrentHoverPath_LocalFactionOnly = 0;

            workingCostToPlanet.Clear();
        }

        public override string GetIdentifierForErrorMessages()
        {
            return "LocalPlayerWorldBaseInfo";
        }

        public override bool GetShouldIBeInUse()
        {
            return true; //always in use!
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType ) { }
        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType ) { }

        protected override void DoPerSimStepLogic_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            Planet currentlyHovered = Planet.CurrentlyHoveredOver;
            int currentHoverIndex = currentlyHovered?.Index ?? -1;
            int currentUISecond = ArcenUI.Instance.CurrentSecond;
            if ( currentHoverIndex != NonSim_LastHoverIndex_LocalFactionOnly ||
                 NonSim_LastUISecondRefreshedCurrentHoverPath_LocalFactionOnly != currentUISecond )
            {
                NonSim_LastHoverIndex_LocalFactionOnly = currentHoverIndex;
                NonSim_LastUISecondRefreshedCurrentHoverPath_LocalFactionOnly = currentUISecond;

                NonSim_PlanetsInvolvedInPathToCurrentHoverPlanet_LocalFactionOnly.Reset();
                NonSim_PlanetsInvolvedInPathToCurrentHoverPlanet_LocalFactionOnly_AdditiveSelection.Reset();

                ProjectedLocalPlayerMultiPathData newLookup = NonSim_PlanetsInvolvedInPathToCurrentHoverPlanet_LocalFactionOnly;
                ProjectedLocalPlayerMultiPathData newLookupAdditive = NonSim_PlanetsInvolvedInPathToCurrentHoverPlanet_LocalFactionOnly_AdditiveSelection;

                Faction faction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();

                PerFactionPathCache cacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();

                PathingHelper.NonSim_PopulatePathToCurrentHoverPlanet_LocalPlayerOnly( faction, "LocalPlayerWorldBaseInfo-Normal", newLookup, false, Context, cacheData );
                PathingHelper.NonSim_PopulatePathToCurrentHoverPlanet_LocalPlayerOnly( faction, "LocalPlayerWorldBaseInfo-Additive", newLookupAdditive, true, Context, cacheData );
                PathingHelper.NonSim_PopulateCostToPlanet( faction, "LocalPlayerWorldBaseInfo-CostToPlanet", workingCostToPlanet, Context, AIWar2GalaxySettingQuickAccess.GalaxyMinimalFogOfWar, cacheData );
                NonSim_CostToPlanet.ClearAndCopyFrom( workingCostToPlanet );

                cacheData.ReturnToPool(); //must happen at the end, or we get a leak
            }
        }

        protected override void DoPerSecondLogic_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Host )
                return;
            HandleHostSyncIfNecessary( );
            FlagObjectsForSyncIfNecessary( );
        }
        #region FlagObjectsForSyncIfNecessary
        private int counter = 0;
        private void FlagObjectsForSyncIfNecessary()
        {
            //periodically gameplay elements need to by quickly sync'd to clients. For example, when a counterattack is building on a planet we don't want the client to
            //need to wait for potentially several minutes for that planet to be sync'd
            int debugCode = 0;
            int syncCheckIntervalForPlanets = 10;
            int flagshipSyncSpread = 10;
            try
            {
                debugCode = 100;
                if (counter > flagshipSyncSpread)
                    counter = 0;
                foreach ( Fleet fleet in World_AIW2.Instance.Fleets( FleetStatus.CenterpieceMustLive ) )
                {
                    //sync mobile flagship locations more regularly
                    debugCode = 200;
                    GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                    if (centerpiece == null ||
                         centerpiece.PlanetFaction.Faction.Type != FactionType.Player)
                        continue;
                    if (!centerpiece.TypeData.IsMobile)
                        continue;
                    debugCode = 300;
                    if (centerpiece.PrimaryKeyID % flagshipSyncSpread == counter)
                    {
                        centerpiece.FlagForForcedFullSyncToClients_FromHost();
                    }
                }
                counter++;

                debugCode = 400;
                if (World_AIW2.Instance.GameSecond % syncCheckIntervalForPlanets != 0)
                    return;
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    if (planet.IntelLevel <= PlanetIntelLevel.Unexplored)
                        continue;
                    if (planet.PrecalculatedAICounterattackForcesStrength <= 0 || planet.AICounterAttacksNotSufficientToTryToSend)
                        continue;
                    World_AIW2.Instance.OnServer_PlanetsToFastBlastToClients.Enqueue(planet); //make sure to immediately remove stale waves
                }
            } catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exceptions in FlagObjectsForSyncIfNecessary debugCode " + debugCode + " " + e.ToString() + " counter " + counter, Verbosity.DoNotShow );
            }
        }
        #endregion
        #region Host Sync
        private static int NextSyncTime = -1; //this isn't serialized
        private static int NextSoftSyncTime = -1; //this isn't serialized
        private void HandleHostSyncIfNecessary( )
        {
            if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Host )
                return;

            int syncInterval = GameSettings.Current.GetIntBySetting( "NetworkSyncInterval" ) * 60;
            if ( syncInterval > 0 )
            {
                if ( NextSyncTime == -1 )
                    NextSyncTime = World_AIW2.Instance.GameSecond + syncInterval;
                else if ( NextSyncTime <= World_AIW2.Instance.GameSecond )
                {
                    if ( World_AIW2.Instance.GetCountOfPlayerAccountsNeedingToCatchUp_ServerOnly( 2 ) > 0 )
                    {
                        //Do not let the automatic hard-sync interval repeatedly knock an already-behind client back into a
                        //full world reload. Give the catch-up path a chance to close the gap, then try the interval again.
                        NextSyncTime = World_AIW2.Instance.GameSecond + Math.Min( syncInterval, 10 );
                    }
                    else
                    {
                        LocalPlayerWorldBaseInfo.ForceSyncWorldToClients();
                        NextSyncTime = World_AIW2.Instance.GameSecond + syncInterval;
                    }
                }
            }

            int softSyncInterval = GameSettings.Current.GetIntBySetting( "NetworkSoftSyncInterval" ) * 60;
            if ( softSyncInterval > 0 )
            {
                if ( NextSoftSyncTime == -1 )
                    NextSoftSyncTime = World_AIW2.Instance.GameSecond + softSyncInterval;
                else if ( NextSoftSyncTime <= World_AIW2.Instance.GameSecond )
                {
                    LocalPlayerWorldBaseInfo.ForceSoftSyncToClients();
                    NextSoftSyncTime = World_AIW2.Instance.GameSecond + softSyncInterval;
                }
            }
        }
        public static void ForceSyncWorldToClients()
        {
            Engine_Universal.WriteToLocalMomentaryDisplayLog( "Forcing sync to all clients; this will cause a performance hitch", null );
            foreach ( ArcenNetworkClientConnection client in ArcenNetworkAuthority.ClientConnections )
            {
                //now send the world
                string errorText;
                if ( !ArcenNetworkAuthority.SendTheWorldFromTheHostToClients( (int)client.ConnectionIndex, out errorText ) )
                    ArcenDebugging.ArcenDebugLog( "MP Error: " + errorText, Verbosity.ShowAsError );
            }
        }
        public static void ForceSoftSyncToClients()
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;
            Engine_Universal.WriteToLocalMomentaryDisplayLog( "Forcing soft sync of all squads to clients (no world rebuild)", null );
            foreach ( GameEntity_Squad squad in World_AIW2.Instance.Squads() )
            {
                squad.FlagForForcedFullSyncToClients_FromHost();
            }
        }
        #endregion

        public override void DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost( ArcenClientOrHostSimContextCore Context )
        {
            if ( World_AIW2.Instance.GameSecond < 3 )
                return;

            int debugStage = 1;
            try
            {
                debugStage = 20000;
                #region Fill NonSim Notifications List Relating To Incoming Exogalactic Strikeforces
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    ExoData exodata = null;
                    if ( otherFaction.BaseInfo.GetDoesThisImplementInterface( typeof(IExoDataHolder) ) )
                        exodata = ((IExoDataHolder)otherFaction.BaseInfo).GetExoData();
                    if ( exodata == null )
                        continue;
                    if ( exodata.PercentToStartWarning == -1 || exodata.CurrentExoStrength == FInt.Zero ) //skip uninitialized exos
                        continue;
                    FInt exoChargePercent = exodata.GetPercentageCharged();
                    if ( exoChargePercent.IntValue < exodata.PercentToStartWarning &&
                         !(exodata.IsSyncingWithCPA || exodata.IsSyncingWithWormholeInvasion) )
                        continue;

                    //We need to generate a warning now
                    NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                    fillData.OtherImportantData = exodata;
                    NotificationNonSim notification = new NotificationNonSim();
                    SortedNotificationPriorityLevel priority = SortedNotificationPriorityLevel.Major;
                    if ( exodata.GetPercentageCharged().IntValue >= 95 )
                        priority = SortedNotificationPriorityLevel.OMG;
                    notification.Assign( ExoNotifier.Instance, fillData, "", 0, "Incoming Exogalactic Strikeforce", priority );
                }
                #endregion

                debugStage = 40000;
                #region Fill NonSim Notifications List Relating to Wormhole Invasion
                Faction wormholeInvasionFaction = FactionUtilityMethods.Instance.GetWormholeInvasionFaction();
                if ( wormholeInvasionFaction != null )
                {
                    WormholeInvasionFactionBaseInfo wormholeBase = wormholeInvasionFaction.GetExternalBaseInfoAs<WormholeInvasionFactionBaseInfo>();
                    for ( int i = 0; i < wormholeBase.IncomingInvasionList.Count; i++ )
                    {
                        WormholeInvasionData invasionData = wormholeBase.IncomingInvasionList[i];
                        NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                        fillData.Faction = invasionData.ResponsibleAIFaction;
                        fillData.Planet = invasionData.InvasionStartPlanet;
                        fillData.PlanetList.Add( invasionData.InvasionDestinationPlanet );
                        fillData.Int64List.Add( invasionData.ProjectorAppearanceTime );
                        fillData.Int64List.Add( invasionData.PlanetLinkTime );
                        if ( invasionData.WaveData != null && invasionData.WaveData.Count > 0 )
                        {
                            fillData.Int64List.Add( invasionData.WaveData[0].TimeForWave );
                            fillData.ShipDictionary.ClearAndCopyFrom( invasionData.WaveData[0].ShipsInWave );
                        }
                        else
                        {
                            fillData.Int64List.Add( -1 );
                            fillData.ShipDictionary.Clear();
                        }
                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( WormholeInvasionNotifier.Instance, fillData, "", 0, "Wormhole Invasion", SortedNotificationPriorityLevel.OMG );
                    }
                }
                #endregion

                debugStage = 50000;
                #region Fill NonSim Notifications List Relating to Instigators
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    if ( otherFaction.SpecialFactionData.InternalName != "Instigators" )
                        continue;
                    foreach ( GameEntity_Squad entity in otherFaction.Squads( "InstigatorBase" ) )
                    {
                        NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                        fillData.Entity = SafeSquadWrapper.Create( entity );

                        SortedNotificationPriorityLevel priority = SortedNotificationPriorityLevel.Medium;

                        InstigatorPerUnitBaseInfo localData = entity.TryGetExternalBaseInfoAs<InstigatorPerUnitBaseInfo>();
                        if ( localData != null )
                        {
                            if ( localData.NumTimesEffectHappened > 4 )
                                priority = SortedNotificationPriorityLevel.Major;
                        }

                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( InstigatorNotifier.Instance, fillData, "", 0, "Instigators", priority );
                    }
                }
                #endregion

                debugStage = 130000;
                #region Fill NonSim Notifications List Relating to astro trains
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    if ( otherFaction.SpecialFactionData.InternalName != "AstroTrains" )
                        continue;
                    debugStage = 131000;
                    AstroTrainsFactionBaseInfo astroTrainFactionData = otherFaction.GetExternalBaseInfoAs<AstroTrainsFactionBaseInfo>();
                    debugStage = 131100;
                    if ( astroTrainFactionData == null )
                        continue;
                    debugStage = 132000;
                    foreach ( GameEntity_Squad entity in otherFaction.Squads( "AstroTrain" ) )
                    {
                        NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                        fillData.Entity = SafeSquadWrapper.Create( entity );

                        debugStage = 133000;
                        SortedNotificationPriorityLevel priority = SortedNotificationPriorityLevel.Minor;

                        debugStage = 133100;
                        AstroTrainsPerTrainBaseInfo perTrainDataOrNull = entity.TryGetExternalBaseInfoAs<AstroTrainsPerTrainBaseInfo>();
                        debugStage = 133200;
                        if ( perTrainDataOrNull != null )
                        {
                            GameEntity_Squad targetDepot = null;
                            debugStage = 133300;
                            foreach ( GameEntity_Squad depot in astroTrainFactionData.TrainDepots.DisplaySquads() )
                            {
                                debugStage = 133500;
                                if ( depot.Planet == null )
                                    continue;
                                debugStage = 133600;
                                if ( depot.Planet.Index == perTrainDataOrNull.TargetDepotPlanetID )
                                {
                                    targetDepot = depot;
                                    break;
                                }
                            }
                            debugStage = 134000;
                            if ( targetDepot != null )
                            {
                                debugStage = 134100;
                                AstroTrainsPerDepotBaseInfo depotData = targetDepot.TryGetExternalBaseInfoAs<AstroTrainsPerDepotBaseInfo>();
                                debugStage = 134200;
                                if ( depotData != null && depotData.data != null && depotData.data.TrainsNeededBeforeFiring > 0 &&
                                    depotData.TrainsThatArrivedSafely >= depotData.data.TrainsNeededBeforeFiring - 1 )
                                {
                                    priority = SortedNotificationPriorityLevel.Major;
                                }
                                debugStage = 134300;
                            }
                        }
                        debugStage = 135000;

                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( AstroTrainNotifier.Instance, fillData, "", 0, "astro trains", priority );
                    }
                }
                #endregion
                debugStage = 140000;
                #region Fill NonSim Notifications List Relating to showdown devices
                if ( GlobalAIWorldBaseInfo.Instance.SecondsUntilCrisis > 0 )
                {
                    NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                    fillData.eventTimeRemaining = GlobalAIWorldBaseInfo.Instance.SecondsUntilCrisis;
                    fillData.Int64List.Add( GlobalAIWorldBaseInfo.Instance.TimeForNextExo );
                    fillData.Int64List.Add( GlobalAIWorldBaseInfo.Instance.StrengthForNextExo );
                    fillData.Int64List.Add( GlobalAIWorldBaseInfo.Instance.TimeForNextWormholeInvasion );
                    fillData.Int64List.Add( GlobalAIWorldBaseInfo.Instance.StrengthForNextWormholeInvasion );

                    NotificationNonSim notification = new NotificationNonSim();
                    if ( GlobalAIWorldBaseInfo.Instance.SecondsUntilCrisis < 600 )
                        notification.Assign( ShowdownDevicesNotifier.Instance, fillData, "", 0, "ShowdownDevices", SortedNotificationPriorityLevel.OMG );
                    else
                        notification.Assign( ShowdownDevicesNotifier.Instance, fillData, "", 0, "ShowdownDevices", SortedNotificationPriorityLevel.Major );
                }
                #endregion

                debugStage = 170000;
                #region Fill NonSim Notifications List Relating To Risk Analyzers
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    if ( otherFaction.SpecialFactionData.InternalName != "AIRiskAnalyzers" )
                        continue;
                    RiskAnalyzerFactionBaseInfo riskAnalyzer = otherFaction.GetExternalBaseInfoAs<RiskAnalyzerFactionBaseInfo>();
                    int nextFiringTime = riskAnalyzer.TimeForNextFiring;
                    if ( nextFiringTime <= 0 )
                        continue;
                    int WarningTime = 300;
                    if ( World_AIW2.Instance.GameSecond + WarningTime > nextFiringTime )
                    {
                        NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                        fillData.eventTimeRemaining = nextFiringTime - World_AIW2.Instance.GameSecond;
                        fillData.NetAIPChange = riskAnalyzer.GetNetAIPChangeForThisFiring();
                        fillData.Faction = otherFaction;
                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( RiskAnalyzerNotifier.Instance, fillData, "", 0, "Risk Analyzers", SortedNotificationPriorityLevel.Informational );
                        break;
                    }
                }
                #endregion

                debugStage = 230000;
                #region Fill NonSim Notifications List Relating To Nemesis Spawning
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    if ( otherFaction.SpecialFactionData.InternalName != "Scourge" )
                        continue;
                    ScourgeFactionBaseInfo scourgeData = otherFaction.GetExternalBaseInfoAs<ScourgeFactionBaseInfo>();
                    if ( scourgeData == null )
                        continue;

                    if ( scourgeData.CountdownTimerForNemesis != -1 )
                    {
                        //there's a nemesis spawning
                        NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                        fillData.Faction = otherFaction;
                        NotificationNonSim notification = new NotificationNonSim();

                        SortedNotificationPriorityLevel priority = SortedNotificationPriorityLevel.OMG;
                        if ( ArcenStrings.Equals( otherFaction.BaseInfo.Allegiance, "Allied To Players" ) )
                            priority = SortedNotificationPriorityLevel.Informational;

                        notification.Assign( NemesisNotifier.Instance, fillData, "", 0, "Nemesis", priority );
                    }
                }
                #endregion

                #region Fill NonSim Notifications List Relating To  Wormhole Borers
                debugStage = 270000;
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    debugStage = 270010;
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    if ( otherFaction.Type != FactionType.AI )
                        continue;
                    if ( otherFaction.FactionIsDefeated )
                        continue;
                    NotifierFillData fillData = null;
                    debugStage = 270020;
                    bool hasAnyNotInFlight = false;
                    foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.WormholeBorer ) )
                    {
                        debugStage = 270030;
                        if ( fillData == null )
                            fillData = NotifierFillData.GetFromPoolOrCreate();
                        debugStage = 270035;
                        fillData.EntityList.Add( entity );
                        if ( !hasAnyNotInFlight )
                        {
                            if ( !entity.TypeData.GetHasTag( "MobileWormholeBorer" ) )
                                hasAnyNotInFlight = true;
                        }
                        debugStage = 270036;
                        fillData.Faction = otherFaction;
                    }
                    debugStage = 270050;
                    if ( fillData != null )
                    {
                        debugStage = 270090;
                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( WormholeBorerNotifier.Instance, fillData, "", 0, "Wormhole Borers", hasAnyNotInFlight ?
                            SortedNotificationPriorityLevel.Medium : SortedNotificationPriorityLevel.Informational );
                    }
                }
                #endregion

                #region Fill NonSim Notifications List Relating to Planet Pings
                {
                    int timeToShowPings = 120;
                    NotifierFillData fillData = null;
                    foreach ( Planet planet in World_AIW2.Instance.CurrentGalaxy.Planets( false ) )
                    {
                        if ( planet == null || planet.GameSecondLastPinged == -1 )
                            continue;
                        if ( World_AIW2.Instance.GameSecond <= planet.GameSecondLastPinged + timeToShowPings )
                        {
                            if ( fillData == null )
                            {
                                fillData = NotifierFillData.GetFromPoolOrCreate();
                            }
                            fillData.PlanetList.Add( planet );
                        }
                    }
                    if ( fillData != null )
                    {
                        fillData.PlanetList.Sort( static delegate ( Planet L, Planet R )
                        {
                            return L.GameSecondLastPinged.CompareTo( R.GameSecondLastPinged );
                        } );
                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( PlanetPingNotifier.Instance, fillData, "", 0, "Pings", SortedNotificationPriorityLevel.Informational );
                    }
                }
                #endregion
                #region Fill NonSim Notifications List Relating To Necromancer
                {
                    debugStage = 400000000;
                    Faction templarFaction = FactionUtilityMethods.Instance.GetTemplarFaction();
                    if (templarFaction != null)
                    {
                        NotifierFillData fillData = null;
                        int visibleLeaders = 0;
                        foreach ( GameEntity_Squad entity in templarFaction.Squads( "TemplarWaveLeader" ) )
                        {
                            if ( fillData == null )
                                fillData = NotifierFillData.GetFromPoolOrCreate();
                            if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                                visibleLeaders++;
                            fillData.EntityList.Add(entity);
                        }
                        if ( fillData != null )
                        {
                            NotificationNonSim notification = new NotificationNonSim();
                            fillData.Int64List.Add( visibleLeaders );
                            notification.Assign( PublicTemplarWaveNotifier.Instance, fillData, "", 0, "Templar Wave",
                                                 SortedNotificationPriorityLevel.Medium );

                        }
                        fillData = null;
                        foreach ( GameEntity_Squad entity in templarFaction.Squads( "TemplarConstructor" ) )
                        {
                            if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                                continue;
                            if ( fillData == null )
                                fillData = NotifierFillData.GetFromPoolOrCreate();
                            fillData.EntityList.Add(entity);
                        }
                        if ( fillData != null )
                        {
                            NotificationNonSim notification = new NotificationNonSim();
                            notification.Assign( PublicTemplarConstructorNotifier.Instance, fillData, "", 0, "Templar Constructors",
                                                 SortedNotificationPriorityLevel.Medium );

                        }
                    }
                }

                #endregion
                #region Fill NonSim Notifications List Relating To Dyson Sidekick
                debugStage = 270000;
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    debugStage = 270010;
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    if (otherFaction.Type == FactionType.AI)
                    {
                        //do the AI drill notifications
                        foreach ( GameEntity_Squad drill in otherFaction.Squads( "AICuendillarDrill" ) )
                        {
                            if ( FactionUtilityMethods.Instance.GetReaperChrysalisOnPlanetOrNull( drill.Planet ) != null )
                                continue; //this is noted in the Chrysalis notification
                            NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                            fillData.EntityList.Add(drill);
                            NotificationNonSim notification = new NotificationNonSim();
                            notification.Assign( PublicAIDrillNotifier.Instance, fillData, "", 0, "AI Drill", SortedNotificationPriorityLevel.Medium );
                        }
                    }
                    if ( !DysonSidekickFactionBaseInfo.GetIsThisADysonFaction( otherFaction ) )
                        continue;
                    DysonSidekickFactionBaseInfo dysonData = otherFaction.GetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
                    if ( dysonData == null )
                        continue;
                    //The dyson sidekick has a couple notifications.

                    //Here check for the next ravager assault
                    int notificationTime = 240;
                    if ( dysonData.TimeForNextEnemyAttack > 0 &&
                         (dysonData.TimeForNextEnemyAttack - World_AIW2.Instance.GameSecond) < notificationTime )
                    {
                        NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                        fillData.Int64List.Add( (dysonData.TimeForNextEnemyAttack - World_AIW2.Instance.GameSecond) );
                        NotificationNonSim notification = new NotificationNonSim();
                        SortedNotificationPriorityLevel priority = SortedNotificationPriorityLevel.Medium;
                        if ( dysonData.TimeForNextEnemyAttack - World_AIW2.Instance.GameSecond < 30 )
                            priority = SortedNotificationPriorityLevel.OMG;
                        notification.Assign( PublicDysonRavagerAssaultNotifier.Instance, fillData, "", 0, "Ravager Assault", priority );
                        
                    }
                    //Here check for a notification associated with the Dyson Sphere construction win condition
                    debugStage = 270020;
                    List<SafeSquadWrapper> spheres = dysonData.Spheres.GetDisplayList();
                    for (int j = 0; j < spheres.Count; j++ )
                    {
                        GameEntity_Squad sphere = spheres[j].GetSquad();
                        if ( sphere == null )
                            continue;
                        DysonSidekickPerUnitBaseInfo bInfo = sphere.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                        if ( bInfo == null )
                            continue;
                        if ( bInfo.TimeTillDysonSphereWin <= 0 )
                            continue;
                        
                        debugStage = 270030;
                        NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                        fillData.EntityList.Add(sphere);
                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( PublicDysonBuildingNotifier.Instance, fillData, "", 0, "Dyson Building", SortedNotificationPriorityLevel.Medium );
                        break;
                    }
                    debugStage = 270020;
                    //Here check for a notification associated with any active Drilling
                    List<SafeSquadWrapper> drills = dysonData.DrillsAndOverloaders.GetDisplayList();
                    for (int j = 0; j < drills.Count; j++ )
                    {
                        GameEntity_Squad drill = drills[j].GetSquad();
                        if ( drill == null )
                            continue;
                        DysonSidekickPerUnitBaseInfo bInfo = drill.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                        if ( bInfo == null )
                            continue;
                        if ( bInfo.TimeTillPlanetOverloaded > 0 || bInfo.TimeTillPlanetDrilled > 0)
                        {
                            debugStage = 270030;
                            NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                            fillData.EntityList.Add(drill);
                            int remainingCuendillar = 0;
                            if ( drill.TypeData.GetHasTag("DysonAsteroidDrill") )
                            {
                                GameEntity_Squad asteroid = FactionUtilityMethods.Instance.GetAsteroidOnPlanetOrNull( drill.Planet );
                                GameEntity_Squad chrysalis = FactionUtilityMethods.Instance.GetReaperChrysalisOnPlanetOrNull( drill.Planet );
                                GameEntity_Squad planetoid = FactionUtilityMethods.Instance.GetPlanetoidOnPlanetOrNull( drill.Planet );
                                
                                if ( chrysalis != null )
                                {
                                    ReapersPerUnitBaseInfo chrData = chrysalis.TryGetExternalBaseInfoAs<ReapersPerUnitBaseInfo>();
                                    remainingCuendillar = chrData.CuendillarRemaining;
                                    fillData.EntityList.Add(chrysalis);
                                }

                                if ( asteroid != null )
                                {
                                    DysonSidekickPerUnitBaseInfo aInfo = asteroid.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                                    remainingCuendillar = aInfo.CuendillarRemaining;
                                    fillData.EntityList.Add(asteroid);
                                }
                                if ( planetoid != null )
                                {
                                    DysonSidekickPerUnitBaseInfo aInfo = planetoid.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                                    remainingCuendillar = aInfo.CuendillarRemaining;
                                    fillData.EntityList.Add(planetoid);
                                }

                            }
                            else
                                remainingCuendillar = drill.Planet.ResourceOneRemainingForAnyPlayer.IntValue;
                            fillData.Int64List.Add( remainingCuendillar );
                            NotificationNonSim notification = new NotificationNonSim();
                            notification.Assign( PublicDysonDrillNotifier.Instance, fillData, "", 0, "Dyson Drill", SortedNotificationPriorityLevel.Medium );
                        }
                    }
                }
                #region Fill NonSim Notifications List Relating To Malware Nexuses
                debugStage = 275000;
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    MalwareFactionBaseInfo malwareData = otherFaction.TryGetExternalBaseInfoAs<MalwareFactionBaseInfo>();
                    if ( malwareData == null )
                        continue;
                    if ( malwareData.Nexuses.Count > 0 )
                    {
                        NotifierFillData fillDataNexus = NotifierFillData.GetFromPoolOrCreate();
                        foreach ( GameEntity_Squad nexus in malwareData.Nexuses.DisplaySquads() )
                        {
                            if ( nexus.GetShouldBeVisibleBasedOnPlanetIntel() )
                                fillDataNexus.EntityList.Add( nexus ); //no notifiers if we can't see it!
                        }
                        if ( fillDataNexus.EntityList.Count > 0 )
                        {
                            NotificationNonSim notification = new NotificationNonSim();
                            notification.Assign( MalwareNexusNotifier.Instance, fillDataNexus, "", 0, "Malware Nexus", SortedNotificationPriorityLevel.Medium );
                        }
                    }
                    if ( malwareData.Rifts.Count > 0 )
                    {
                        NotifierFillData fillDataRift = NotifierFillData.GetFromPoolOrCreate();
                        foreach ( GameEntity_Squad rift in malwareData.Rifts.DisplaySquads() )
                        {
                            if ( rift.GetShouldBeVisibleBasedOnPlanetIntel() )
                                fillDataRift.EntityList.Add( rift ); //no notifiers if we can't see it!
                        }
                        if ( fillDataRift.EntityList.Count > 0 )
                        {
                            NotificationNonSim notification = new NotificationNonSim();
                            notification.Assign( MalwareConduitNotifier.Instance, fillDataRift, "", 0, "Malware Conduit", SortedNotificationPriorityLevel.Medium );
                        }
                    }
                    if ( malwareData.ActiveBreachesByPlanetIndex.Count > 0 )
                    {
                        NotifierFillData fillDataBreach = NotifierFillData.GetFromPoolOrCreate();
                        foreach ( Arcen.Universal.KeyValuePair<int, MalwareBreach> kvp in malwareData.ActiveBreachesByPlanetIndex )
                        {
                            Planet breachPlanet = World_AIW2.Instance.GetPlanetByIndex( (short)kvp.Key );
                            if ( breachPlanet == null )
                                continue;
                            if ( breachPlanet.IntelLevel < PlanetIntelLevel.CurrentlyWatched )
                                continue;
                            fillDataBreach.PlanetList.Add( breachPlanet );
                            fillDataBreach.StringList.Add( kvp.Value?.DisplayName ?? "Unknown Breach" );
                        }
                        if ( fillDataBreach.PlanetList.Count > 0 )
                        {
                            NotificationNonSim notification = new NotificationNonSim();
                            notification.Assign( MalwareBreachNotifier.Instance, fillDataBreach, "", 0, "Malware Breach", SortedNotificationPriorityLevel.Medium );
                        }
                    }
                    if ( malwareData.ConvergenceCountdownEndTime != -1 )
                    {
                        NotifierFillData fillDataConvergence = NotifierFillData.GetFromPoolOrCreate();
                        long secondsRemaining = malwareData.ConvergenceCountdownEndTime - World_AIW2.Instance.GameSecond;
                        fillDataConvergence.Int64List.Add( secondsRemaining );
                        foreach ( GameEntity_Squad gen in malwareData.ConvergenceGenerators.DisplaySquads() )
                        {
                            if ( gen.GetShouldBeVisibleBasedOnPlanetIntel() )
                                fillDataConvergence.EntityList.Add( gen );
                        }
                        NotificationNonSim notificationConvergence = new NotificationNonSim();
                        SortedNotificationPriorityLevel convergencePriority = secondsRemaining < 60
                            ? SortedNotificationPriorityLevel.OMG
                            : SortedNotificationPriorityLevel.Medium;
                        notificationConvergence.Assign( MalwareConvergenceNotifier.Instance, fillDataConvergence, "", 0, "Malware Convergence", convergencePriority );
                    }
                    // Phase 1: Disruptor active, Malware still allied to AI, betrayal countdown running.
                    if ( malwareData.BetrayalCountdownEndTime > 0 && malwareData.InvasionTime == -1 )
                    {
                        long secondsRemaining = malwareData.BetrayalCountdownEndTime - World_AIW2.Instance.GameSecond;
                        if ( secondsRemaining > 0 )
                        {
                            NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                            fillData.Int64List.Add( secondsRemaining );
                            NotificationNonSim notification = new NotificationNonSim();
                            SortedNotificationPriorityLevel priority = secondsRemaining < 60
                                ? SortedNotificationPriorityLevel.OMG
                                : SortedNotificationPriorityLevel.Medium;
                            notification.Assign( MalwareBetrayalCountdownNotifier.Instance, fillData, "", 0, "Malware Betrayal Countdown", priority );
                        }
                    }
                    // Phase 2: Malware betrayed the AI; planets have not yet appeared. No countdown shown.
                    else if ( malwareData.InvasionTime > 0 && !malwareData.HasDoneInvasion )
                    {
                        NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( MalwareBetrayedNotifier.Instance, fillData, "", 0, "Malware Betrayed", SortedNotificationPriorityLevel.Medium );
                    }
                    // Phase 3: Malware planets have appeared; countdown to wormholes opening.
                    else if ( malwareData.HasDoneInvasion && !malwareData.HasLinkedPlanets && malwareData.TimeToLinkPlanets > 0 )
                    {
                        long secondsRemaining = malwareData.TimeToLinkPlanets - World_AIW2.Instance.GameSecond;
                        if ( secondsRemaining > 0 )
                        {
                            NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                            fillData.Int64List.Add( secondsRemaining );
                            NotificationNonSim notification = new NotificationNonSim();
                            SortedNotificationPriorityLevel priority = secondsRemaining < 60
                                ? SortedNotificationPriorityLevel.OMG
                                : SortedNotificationPriorityLevel.Medium;
                            notification.Assign( MalwareInvasionCountdownNotifier.Instance, fillData, "", 0, "Malware Invasion Countdown", priority );
                        }
                    }
                }
                #endregion
                #region Fill NonSim Notifications List Relating To Apkallu Lamassu Module Desync
                debugStage = 277000;
                {
                    for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                    {
                        Faction otherFaction = World_AIW2.Instance.Factions[i];
                        ApkalluFactionBaseInfo apkalluData = otherFaction.TryGetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
                        if ( apkalluData == null )
                            continue;
                        NotifierFillData fillData = null;
                        foreach ( GameEntity_Squad lamassu in apkalluData.Lamassus.DisplaySquads() )
                        {
                            List<string> activeTags = lamassu.GetEnabledSummonerTagsDirect();
                            // Find the Ziggurat that owns this Lamassu
                            GameEntity_Squad ownerZiggurat = null;
                            foreach ( GameEntity_Squad ziggurat in apkalluData.Ziggurats.DisplaySquads() )
                            {
                                ApkalluPerUnitBaseInfo zigUnit = ziggurat.TryGetExternalBaseInfoAs<ApkalluPerUnitBaseInfo>();
                                if ( zigUnit != null && zigUnit.LamassuEntityPrimaryKeyID == lamassu.PrimaryKeyID )
                                {
                                    ownerZiggurat = ziggurat;
                                    break;
                                }
                            }
                            if ( ownerZiggurat == null || ownerZiggurat.Planet == null )
                                continue;
                            // Collect summoner tags that the Ziggurat's current structures provide
                            availableTagsScratch.Clear();
                            foreach ( GameEntity_Squad entity in ownerZiggurat.Planet.Squads() )
                            {
                                if ( entity.GetFactionOrNull_Safe() != otherFaction )
                                    continue;
                                if ( entity.TypeData.GetHasTag( "ApkalluZigguratSummoner" ) )
                                {
                                    List<string> tags = entity.TypeData.TagsList;
                                    if ( tags != null )
                                        for ( int t = 0; t < tags.Count; t++ )
                                            if ( tags[t].StartsWith( "ApkalluSummoner" ) && !availableTagsScratch.Contains( tags[t] ) )
                                                availableTagsScratch.Add( tags[t] );
                                }
                            }
                            // Check for drift between active tags and what the Ziggurat now provides
                            bool differs = false;
                            int activeCount = activeTags == null ? 0 : activeTags.Count;
                            if ( activeCount != availableTagsScratch.Count )
                                differs = true;
                            else
                            {
                                for ( int t = 0; t < availableTagsScratch.Count && !differs; t++ )
                                    if ( activeTags == null || !activeTags.Contains( availableTagsScratch[t] ) )
                                        differs = true;
                                for ( int t = 0; t < activeCount && !differs; t++ )
                                    if ( !availableTagsScratch.Contains( activeTags[t] ) )
                                        differs = true;
                            }
                            if ( differs )
                            {
                                if ( fillData == null )
                                    fillData = NotifierFillData.GetFromPoolOrCreate();
                                fillData.EntityList.Add( lamassu );
                            }
                        }
                        if ( fillData != null )
                        {
                            NotificationNonSim notification = new NotificationNonSim();
                            notification.Assign( LamassuModuleDesyncNotifier.Instance, fillData, "", 0, "Lamassu Resync", SortedNotificationPriorityLevel.Informational );
                        }
                    }
                }
                #endregion
                debugStage = 280000;
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    if ( otherFaction.SpecialFactionData.InternalName != "Reapers" &&
                         otherFaction.SpecialFactionData.InternalName != "ReapersWithoutDyson") 
                        continue;
                    ReapersFactionBaseInfo reaperData = otherFaction.GetExternalBaseInfoAs<ReapersFactionBaseInfo>();
                    if ( reaperData == null )
                        continue;
                    int notificationTime = 240;
                    if ( reaperData.TimeForNextLunarInvasion > 0 &&
                         (reaperData.TimeForNextLunarInvasion - World_AIW2.Instance.GameSecond) < notificationTime )
                    {
                        NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                        fillData.Int64List.Add( (reaperData.TimeForNextLunarInvasion - World_AIW2.Instance.GameSecond) );
                        NotificationNonSim notification = new NotificationNonSim();
                        SortedNotificationPriorityLevel priority = SortedNotificationPriorityLevel.Medium;
                        if ( reaperData.TimeForNextLunarInvasion - World_AIW2.Instance.GameSecond < 30 )
                            priority = SortedNotificationPriorityLevel.OMG;
                        notification.Assign( PublicDysonLunarInvasionNotifier.Instance, fillData, "", 0, "Lunar Invasion", priority );
                    }

                    if (reaperData.MobileRavagers.Count > 0 || reaperData.ImmobileRavagers.Count > 0)
                    {
                        NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                        List<SafeSquadWrapper> ravagers = reaperData.MobileRavagers.GetDisplayList();
                        for (int j = 0; j < ravagers.Count; j++)
                        {
                            GameEntity_Squad entity = ravagers[j].GetSquad();
                            if (entity == null)
                                continue;
                            fillData.EntityList.Add(entity);
                        }
                        ravagers = reaperData.ImmobileRavagers.GetDisplayList();
                        for (int j = 0; j < ravagers.Count; j++)
                        {
                            GameEntity_Squad entity = ravagers[j].GetSquad();
                            if (entity == null)
                                continue;
                            fillData.EntityList.Add(entity);
                        }
                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( PublicRavagerNotifier.Instance, fillData, "", 0, "Ravager", SortedNotificationPriorityLevel.Medium );
                    }
                    if (reaperData.Larvae.Count > 0 || reaperData.Larvae.Count > 0)
                    {
                        NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                        List<SafeSquadWrapper> larvae = reaperData.Larvae.GetDisplayList();
                        for (int j = 0; j < larvae.Count; j++)
                        {
                            GameEntity_Squad entity = larvae[j].GetSquad();
                            if (entity == null)
                                continue;
                            fillData.EntityList.Add(entity);
                        }
                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( PublicLarvaNotifier.Instance, fillData, "", 0, "Larvae", SortedNotificationPriorityLevel.Medium );
                    }
                    if (reaperData.Chrysalises.Count > 0)
                    {
                        NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                        List<SafeSquadWrapper> chrysalises = reaperData.Chrysalises.GetDisplayList();
                        for (int j = 0; j < chrysalises.Count; j++)
                        {
                            GameEntity_Squad entity = chrysalises[j].GetSquad();
                            if (entity == null)
                                continue;
                            fillData.EntityList.Add(entity);
                            Faction controllingFaction = entity.Planet.GetControllingOrInfluencingFaction();
                            if (controllingFaction.Type == FactionType.AI)
                            {
                                foreach ( GameEntity_Squad drill in controllingFaction.Squads( "AICuendillarDrill" ) )
                                {
                                    if ( drill.Planet == entity.Planet )
                                    {
                                        fillData.BoolList.Add(true);
                                    }
                                }
                            }

                        }

                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign(PublicReaperChrysalisNotifier.Instance, fillData, "", 0, "Chrysalis", SortedNotificationPriorityLevel.Medium);
                    }
                }
                #endregion
                #region Fill NonSim Notifications List Relating To Scourge Infused Empire
                debugStage = 290000;
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    debugStage = 270010;
                    Faction otherFaction = World_AIW2.Instance.Factions[i];

                     if ( otherFaction.SpecialFactionData.InternalName != "ScourgeVassal")
                        continue;
                    ScourgeVassalFactionBaseInfo baseInfo = otherFaction.GetExternalBaseInfoAs<ScourgeVassalFactionBaseInfo>();
                    if ( baseInfo == null )
                        continue;
                    debugStage = 290020;
                    //Here check for a notification associated with flowers
                    List<SafeSquadWrapper> flowers = baseInfo.Flowers.GetDisplayList();
                    NotifierFillData fillData = null;
                    for (int j = 0; j < flowers.Count; j++ )
                    {
                        GameEntity_Squad flower = flowers[j].GetSquad();
                        if ( flower == null )
                            continue;
                        if ( fillData == null )
                            fillData = NotifierFillData.GetFromPoolOrCreate();
                        fillData.EntityList.Add(flower);
                    }
                    if ( fillData != null )
                    {
                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( ScourgeFlowerNotifier.Instance, fillData, "", 0, "ScourgeFlower", SortedNotificationPriorityLevel.Medium );
                    }
                    //Here check for a notification associated with any structures needing upgrading
                    List<SafeSquadWrapper> structures = baseInfo.UpgradableInfrastructure.GetDisplayList();
                    fillData = null;
                    for (int j = 0; j < structures.Count; j++ )
                    {
                        GameEntity_Squad structure = structures[j].GetSquad();
                        if ( structure == null )
                            continue;
                        if ( fillData == null )
                            fillData = NotifierFillData.GetFromPoolOrCreate();
                        fillData.EntityList.Add(structure);
                    }
                    if ( fillData != null )
                    {
                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( ScourgeUpgradableStructureNotifier.Instance, fillData, "", 0, "ScourgeUpgrade", SortedNotificationPriorityLevel.Medium );
                    }

                }
                #endregion
                #region Fill NonSim Notifications List Relating To Armada Empire
                debugStage = 290000;
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    debugStage = 270010;
                    Faction otherFaction = World_AIW2.Instance.Factions[i];

                    if ( !ArmadaFactionBaseInfo.GetIsThisAnArmadaFaction ( otherFaction ))
                        continue;
                    ArmadaFactionBaseInfo baseInfo = otherFaction.GetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
                    if ( baseInfo == null )
                        continue;
                    debugStage = 290020;
                    int timeTillNextAttack = baseInfo.TimeForNextEnemyAttack - World_AIW2.Instance.GameSecond;
                    if ( timeTillNextAttack > 180 )
                        continue;

                    NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                    fillData.Int64List.Add( timeTillNextAttack );
                    NotificationNonSim notification = new NotificationNonSim();
                    SortedNotificationPriorityLevel priority = SortedNotificationPriorityLevel.Medium;
                    notification.Assign( PublicArmadaCPANotifier.Instance, fillData, "", 0, "Armada CPA", priority );

                }
                #endregion
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "LocalPlayerWorldBaseInfo.DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost Error at debug stage " + debugStage + ":\n" + e, Verbosity.ShowAsError );
            }
            
        }
    }
}
