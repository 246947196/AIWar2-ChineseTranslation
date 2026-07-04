
using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    /* TODO: Utility functions

       AllowedToBuildLevel2City (ish)
       Change what information is stored for Exogalactic Strikeforces per-GameEntity_Squad, and then make that able to chase a relic properly <=== needs testing
       Seed 2 bonus spire things on the map: the AI Spire Research Lab (gotta build here) and AI Spire Citadel (weak exos)
       Astro-Train style relic chase
       Spire Debris; logic for each minor faction to know that it's eligible to build spire equivalent
       Relic death trigger (AIP? A lot of people will just savescum, so I think having a 5 AIP cost is "bad enough to hurt but not bad enough to force savescum )
       (Needs a Spire Scourge. Needs a Spire Marauder Raider. Needs a Spire Nanocaust unit. Needs an AI equivalent version of all the spire units)

       The logic should handle multiple simultaneous relic chases, so in theory a "Rescue to Relics at once" achievement is possible
       */

    public sealed class SpireSidekickFactionDeepInfo : ExternalFactionDeepInfoRoot, IExternalDeepInfo_Singleton
    {
        public SpireSidekickFactionBaseInfo BaseInfo;
        public static SpireSidekickFactionDeepInfo Instance = null;
        public readonly List<Faction> AIFactionsForDebris = List<Faction>.Create_WillNeverBeGCed( 30, "SpireSidekickFactionDeepInfo-AIFactionsForDebris" );
        public readonly List<Faction> OtherFactionsForDebris = List<Faction>.Create_WillNeverBeGCed( 30, "SpireSidekickFactionDeepInfo-OtherFactionsForDebris" );
        private static readonly List<int> chokeStrengths = List<int>.Create_WillNeverBeGCed( 300, "SpireSidekickFactionDeepInfo-chokeStrengths" );
        //fireteam stuff, for the imperial spire
        public readonly List<SafeSquadWrapper> UnassignedShips = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "SpireSidekickFactionDeepInfo-UnassignedShips" );
        public static readonly DictionaryOfLists<Planet, SafeSquadWrapper> KingKillersByPlanet = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 10, "SpireSidekickFactionDeepInfo-KingKillersByPlanet" );
        public readonly ProtectedValDictionary<Planet, FireteamRegiment> TeamsAimedAtPlanet = ProtectedValDictionary<Planet, FireteamRegiment>.Create_WillNeverBeGCed( 100, "SpireSidekickFactionDeepInfo-TeamsAimedAtPlanet" );
        public static readonly List<SafeSquadWrapper> LRPActiveRelics = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 90, "SpireSidekickFactionDeepInfo-LRPActiveRelics" );
        public static readonly List<SafeSquadWrapper> AlliedCommandStations = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 90, "SpireSidekickFactionDeepInfo-AlliedCommandStations" );
        public static readonly List<Planet> FriendlyPlanets = List<Planet>.Create_WillNeverBeGCed( 90, "SpireSidekickFactionDeepInfo-FriendlyPlanets" );
        private static readonly ArcenLessLinkedList<Fireteam> AvailableFireteams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "SpireSidekickFactionDeepInfo-AvailableFireteams" );

        //Sidekick specific stuff
        private static readonly List<SafeSquadWrapper> AutoDefendingShipsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "SpireSidekickFactionDeepInfo-AutoDefendingShipsLRP" );
        private static readonly List<Planet> WorkingPlanetsLRP = List<Planet>.Create_WillNeverBeGCed( 500, "SpireSidekickFactionDeepInfo-WorkingPlanetsLRP" );
        private static readonly Dictionary<GameEntityTypeData, int> AttackComposition = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed(500, "DysonFactionDeepInfo-AttackComposition");
        public readonly int MinFireteamStrength = 5000;
        public readonly int MaxFireteamStrength = 20000;

        //this just keeps us from having journal entries submitted every second as a possibility.
        //It doesn't matter that we're not caching this between sessions.
        private int LastNumCitiesSeenForJournal = 0;

        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<SpireSidekickFactionBaseInfo>();
            Instance = this;
        }

        protected override void Cleanup()
        {
            Instance = null;
            BaseInfo = null;

            //may or may not matter
            AIFactionsForDebris.Clear();
            AttackComposition.Clear();
            OtherFactionsForDebris.Clear();
            chokeStrengths.Clear();
            LRPActiveRelics.Clear();
            AlliedCommandStations.Clear();
            FriendlyPlanets.Clear();
            AvailableFireteams.Clear();
            AutoDefendingShipsLRP.Clear();
            WorkingPlanetsLRP.Clear();
            //matters some, probably
            UnassignedShips.Clear();
            KingKillersByPlanet.Clear();
            TeamsAimedAtPlanet.Clear();

            LastNumCitiesSeenForJournal = 0;
        }
        protected override int MinimumSecondsBetweenLongRangePlannings => 3;

        public override void DoOnSelfBuildingCompleteLogic_HostOnly( GameEntity_Squad entity, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return;
            HandleSelfBuildingCompleteLogic( entity, this.AttachedFaction, Context );
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            Instance = this;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.FallenSpire );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "SpireSidekick-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f ) : null;
            int debugCode = 0;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                debugCode = 1000;
                this.AttachedFaction.HasBeenSeenByPlayer = true;
                debugCode = 1010;
                debugCode = 1020;
                DropMetalGenerators(Context);
                debugCode = 1030;
                DoInitializationIfNecessary( this.AttachedFaction, this.BaseInfo, Context, tracing );
                if ( World_AIW2.Instance.GameSecond == 2 )
                {
                    //Spawn a little later so we can play this alongside the DZ Empire, which appears at the beginning of the game
                    Planet start = FactionUtilityMethods.Instance.findHumanKing(true);
                
                    if ( start != null )
                    {
                        PlanetFaction pFaction = start.GetPlanetFactionForFaction( this.AttachedFaction );
                        GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRowByName( "SpireSidekickCommandVessel" );
                        GameEntity_Squad startShip = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typeData, 1,
                            null, 0, Engine_AIW2.Instance.CombatCenter, Context, "SpireSidekick-SidekickStart" );
                    }
                }

                debugCode = 1100;
                if ( AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) &&
                     BaseInfo.CurrentRelicSpawnPlanetIdx == -1 &&
                     BaseInfo.TimeForNextRelicSpawn > World_AIW2.Instance.GameSecond + 10 ) //in debug mode, relics spawn very quickly
                {
                    debugCode = 1110;
                    BaseInfo.TimeForNextRelicSpawn = World_AIW2.Instance.GameSecond + 10;
                    if ( tracing )
                        tracingBuffer.Add( "Debug mode relic spawn override: " + BaseInfo.TimeForNextRelicSpawn );
                }

                debugCode = 1130;
                Planet relicPlanet = World_AIW2.Instance.GetPlanetByIndex( BaseInfo.CurrentRelicSpawnPlanetIdx );
                debugCode = 1140;
                if ( relicPlanet != null && (relicPlanet.HasPlanetBeenDestroyed || relicPlanet.IsPlanetToBeDestroyed) )
                {
                    debugCode = 1150;
                    //there was an unclaimed relic on a planet, but something has destroyed it (presumably a zenith miner). Try to spawn a new relic immediately
                    BaseInfo.CurrentRelicSpawnPlanetIdx = -1;
                    BaseInfo.TimeForNextRelicSpawn = World_AIW2.Instance.GameSecond;
                }

                debugCode = 1200;
                if ( BaseInfo.TimeForNextRelicSpawn >= World_AIW2.Instance.GameSecond && tracing &&
                     World_AIW2.Instance.GameSecond % 10 == 0 )
                    tracingBuffer.Add( "The next relic will spawn in " ).Add( BaseInfo.TimeForNextRelicSpawn - World_AIW2.Instance.GameSecond ).Add( " seconds (aka game time  " + (World_AIW2.Instance.GameSecond + BaseInfo.TimeForNextRelicSpawn) + ")" );
                if ( BaseInfo.TimeForNextRelicSpawn <= World_AIW2.Instance.GameSecond && BaseInfo.CurrentRelicSpawnPlanetIdx == -1 )
                {
                    debugCode = 1400;
                    BaseInfo.CurrentRelicSpawnPlanetIdx = GetNextRelicSpawnPoint( AttachedFaction, Context, false );
                    debugCode = 1410;
                    if ( BaseInfo.CurrentRelicSpawnPlanetIdx != -1 )
                    {
                        debugCode = 1420;
                        BaseInfo.CurrentRelicInSearchMode = false;
                        if ( Context.RandomToUse.Next( 0, 100 ) < this.BaseInfo.Difficulty.SearchModePercent &&
                             BaseInfo.NumRelicsCaptured > 0 ) //first relic is always easy
                            BaseInfo.CurrentRelicInSearchMode = true;
                        debugCode = 1430;
                        BaseInfo.RelicOnMap = false;
                        BaseInfo.PlanetsSearchedForCurrentRelic.Clear();
                        debugCode = 1440;
                        Planet spawnPlanet = World_AIW2.Instance.GetPlanetByIndex( BaseInfo.CurrentRelicSpawnPlanetIdx );
                        debugCode = 1450;
                        if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                        {
                            debugCode = 1460;
                            if ( BaseInfo.CurrentRelicInSearchMode || spawnPlanet.IntelLevel == PlanetIntelLevel.Unexplored )
                            {
                                debugCode = 1470;
                                World_AIW2.Instance.QueueChatMessageOrCommand( "A Spire Relic has spawned somewhere in the galaxy.", ChatType.LogToCentralChat, string.Empty, null );
                            }
                            else
                            {
                                debugCode = 1480;
                                PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                                if ( chatHandlerOrNull != null )
                                    chatHandlerOrNull.PlanetToView = World_AIW2.Instance.GetPlanetByIndex( BaseInfo.CurrentRelicSpawnPlanetIdx );

                                World_AIW2.Instance.QueueChatMessageOrCommand( "A Spire Relic has spawned on planet " + World_AIW2.Instance.GetPlanetByIndex( BaseInfo.CurrentRelicSpawnPlanetIdx ).Name + ".",
                                    ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                            }
                        }
                        debugCode = 1490;
                        if ( tracing )
                            tracingBuffer.Add( "The next spire relic will spawn at " + BaseInfo.TimeForNextRelicSpawn + " on " + spawnPlanet.Name );
                    }
                    else
                    {
                        //PERF FIX (2026-05-29): GetNextRelicSpawnPoint is an expensive galaxy scan called from
                        //per-second sim logic. Previously a failed search (-1) left TimeForNextRelicSpawn unchanged
                        //and CurrentRelicSpawnPlanetIdx == -1, so the scan re-ran every single sim-second. Push the
                        //retry out so a failed search costs at most one scan per retry interval, not one per second.
                        BaseInfo.TimeForNextRelicSpawn = World_AIW2.Instance.GameSecond + 30;
                        if ( tracing )
                            tracingBuffer.Add( "No legal planets for relic spawn; does the player own every watched planet?" );
                    }
                }
                debugCode = 2000;
                UpdateExoData( AttachedFaction, BaseInfo, Context, tracing );
                SpawnRangers( Context );
                SpawnNadir(Context);
                List<SafeSquadWrapper> activeRelics = this.BaseInfo.ActiveRelics.GetDisplayList();

                /* Do the relic chase response code */
                for ( int i = 0; i < BaseInfo.ActiveRelics.Count; i++ )
                {
                    debugCode = 2100;
                    //this means the player has found a relic on the map, and the AI is in chase mode
                    GameEntity_Squad relic = activeRelics[i].GetSquad();
                    if ( relic == null )
                        continue;
                    SpireSidekickPerUnitBaseInfo relicData = relic.CreateExternalBaseInfo<SpireSidekickPerUnitBaseInfo>( "SpireSidekickPerUnitBaseInfo" );
                    GameEntity_Squad playerKing = ExoGalacticAttackManager.GetHumanHomeCommandStation( relicData.FactionThatFoundThisRelic );
                    if ( playerKing == null )
                        continue;
                    debugCode = 2150;
                    Faction aiFactionForCalculations = World_AIW2.GetRandomAIFaction( Context );
                    AISentinelsFactionBaseInfo aiBaseIfno = aiFactionForCalculations.GetAISentinelsCoreData();
                    int WaveSize = aiBaseIfno.GetSpecificBudgetThreshold( AIBudgetType.Wave, GlobalAIWorldBaseInfo.Instance.AIProgress_Effective );

                    FInt baseResponseStrength = (BaseInfo.Difficulty.BaseRelicResponseStrength + BaseInfo.Difficulty.ResponseIncreasePerRelic * BaseInfo.NumRelicsCaptured + WaveSize * BaseInfo.Difficulty.RelicResponseWaveMultiplier) * relicData.RelicResponseMultiplier;
                    FInt responseStrength = baseResponseStrength + baseResponseStrength * (BaseInfo.Difficulty.RelicResponseIncreasePerPlanetChecked * (BaseInfo.PlanetsSearchedForCurrentRelic.Count + 1)) * (BaseInfo.Difficulty.RelicResponseMultiplierPerCity * BaseInfo.NumRelicsCaptured);

                    debugCode = 2175;
                    if ( relicData.TimeForNextRelicResponse == -1 )
                    {
                        debugCode = 2200;
                        //this is the first response to this relic, so only go for the relic
                        if ( DarkSpireFactionBaseInfo.Instance != null )
                        {
                            if ( ArcenNetworkAuthority.GetIsHostMode() )
                                World_AIW2.Instance.QueueChatMessageOrCommand( "The Dark Spire has detected a surge in Fallen Spire energy, and is performing a Vengeance Strike as a result",
                                    ChatType.LogToCentralChat, string.Empty, null );
                            DarkSpireFactionBaseInfo.Instance.PerformVengeanceStrike();
                        }
                        if ( AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) ) //debug mode is easy so you can play through it quickly for testing
                            responseStrength /= 15;
                        debugCode = 2250;
                        ExoOptions options = ExoOptions.CreateWithDefaults( relic, responseStrength.IntValue, null, AttachedFaction );
                        if ( BaseInfo.SpireCities.Count > 3 ) //this number could be in the BaseInfo.Difficulty
                            options.newExoLeaderTag = "ExtragalacticWar";
                        options.distanceFromTargetOverride = 1;
                        options.exoText = "The AI has detected the Relic's energy signature and is attacking!";

                        ExoGalacticAttackManager.SendExoGalacticAttack( options, Context );
                        relicData.TimeForNextRelicResponse = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.RelicResponseInterval;
                        if ( tracing )
                            tracingBuffer.Add( "AI sending first relic response against " + relic.ToStringWithPlanet() + " at " + World_AIW2.Instance.GameSecond + ". Total strength: " + responseStrength + ". (BaseRelicResponseStrength " + BaseInfo.Difficulty.BaseRelicResponseStrength + " + ResponseIncreasePerRelic " + BaseInfo.Difficulty.ResponseIncreasePerRelic + " * NumRelicsCaptured " + BaseInfo.NumRelicsCaptured + " + WaveSize " + WaveSize + " * waveMult " + BaseInfo.Difficulty.RelicResponseWaveMultiplier + ") * responseMult " + relicData.RelicResponseMultiplier + " with relic response multiplier RelicResponseIncreasePerPlanetChecked " + BaseInfo.Difficulty.RelicResponseIncreasePerPlanetChecked + " * numPlanetsSearched " + BaseInfo.PlanetsSearchedForCurrentRelic.Count );

                    }
                    else if ( relicData.TimeForNextRelicResponse < World_AIW2.Instance.GameSecond )
                    {
                        debugCode = 2300;
                        //trigger "regular" relic response, which splits strength between the relic and the player king
                        //Could add more targets if  necessary
                        List<SafeSquadWrapper> workingTargets = GameEntity_Squad.GetTemporarySquadList( "SpireSidekick-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-workingTargets", 10f );
                        if ( workingTargets == null ) //blocked for teardown/shutdown; bail
                            return;
                        workingTargets.Add( playerKing );
                        workingTargets.Add( relic );

                        int citySnipePercentage = 10;
                        if ( BaseInfo.Intensity >= 7 && Context.RandomToUse.Next( 0, 100 ) < citySnipePercentage &&
                             BaseInfo.SpireCities.Count >= 3 )
                        {
                            //also throw a bonus assault against one of the player cities. Only for higher difficulties
                            //and once you're a ways into the game). TODO: put this percentage in the BaseInfo.Difficulty if needed
                            workingTargets.Add( BaseInfo.SpireCities.Display_GetRandomItem( Context.RandomToUse ) );
                        }
                        debugCode = 2350;
                        if ( AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) ) //debug mode is easy so you can play through it quickly for testing
                            responseStrength /= 15;
                        ExoOptions options1 = ExoOptions.CreateWithDefaults( relic, responseStrength.IntValue, null, AttachedFaction );
                        if ( BaseInfo.SpireCities.Count > 3 ) //this number could be in the BaseInfo.Difficulty
                            options1.newExoLeaderTag = "ExtragalacticWar";
                        options1.distanceFromTargetOverride = 2;
                        options1.exoText = "The AI has detected the Relic's energy signature and is attacking!";

                        ExoGalacticAttackManager.SendExoGalacticAttack( options1, Context );

                        ExoOptions options2 = ExoOptions.CreateWithDefaults( workingTargets, responseStrength.IntValue, null, AttachedFaction );
                        if ( BaseInfo.SpireCities.Count > 3 ) //this number could be in the BaseInfo.Difficulty
                            options2.newExoLeaderTag = "ExtragalacticWar";
                        options2.distanceFromTargetOverride = 2;
                        options2.exoText = "The AI has detected the Relic's energy signature and is attacking!";

                        GameEntity_Squad.ReleaseTemporarySquadList( workingTargets );

                        ExoGalacticAttackManager.SendExoGalacticAttack( options2, Context );

                        relicData.TimeForNextRelicResponse = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.RelicResponseInterval;
                        if ( tracing )
                            tracingBuffer.Add( "AI sending subsequent exo response against " + relic.ToStringWithPlanet() + " at " + World_AIW2.Instance.GameSecond + ". Total strength: " + responseStrength + ". (BaseRelicResponseStrength " + BaseInfo.Difficulty.BaseRelicResponseStrength + " + ResponseIncreasePerRelic " + BaseInfo.Difficulty.ResponseIncreasePerRelic + " * NumRelicsCaptured " + BaseInfo.NumRelicsCaptured + " + WaveSize " + WaveSize + " * waveMult " + BaseInfo.Difficulty.RelicResponseWaveMultiplier + ") * responseMult " + relicData.RelicResponseMultiplier + " with relic response multiplier RelicResponseIncreasePerPlanetChecked " + BaseInfo.Difficulty.RelicResponseIncreasePerPlanetChecked + " * numPlanetsSearched " + BaseInfo.PlanetsSearchedForCurrentRelic.Count + " city based increase multiplier: " + BaseInfo.Difficulty.RelicResponseMultiplierPerCity + " * " + BaseInfo.NumRelicsCaptured );

                    }
                    debugCode = 2400;

                    bool reachedDestination = false;
                    if ( relicData.DestinationPlanet != null )
                    {
                        if ( relic.Planet == relicData.DestinationPlanet &&
                             Mat.DistanceBetweenPointsImprecise( relicData.DestinationPoint, relic.WorldLocation ) < 1000 )
                        {
                            reachedDestination = true;
                        }
                    }

                    if ( reachedDestination )
                    {
                        debugCode = 2500;
                        ConvertRelicIntoCity( AttachedFaction, playerKing, relic, relicData, Context, ref responseStrength );
                    }
                }
                /* Spire debris comes in a few mechanisms. First, spawn it. We will need some specific spawning code to place it "close enough" to the player, Then see whether it's been long enough to despawn it. If despawning
                   then it gives buffs to non-player-allied minor factions. Priority: minor faction, then AI. */
                debugCode = 3000;
                HandleSpireDebris( AIFactionsForDebris, OtherFactionsForDebris, this.AttachedFaction, this.BaseInfo, Context, tracing );
                debugCode = 5050;
                //get any attached fleets set up, if we need to.
                RecalculateSpireFleetsAndFlagships_MainThreadSimOnly( this.AttachedFaction, this.BaseInfo, Context );
                debugCode = 5200;
                //and what the contents of each mobile fleet should be, after THAT
                RecalculateSpireCityMobileFleetContents_MainThreadSimOnly( this.AttachedFaction, this.BaseInfo, Context );
                debugCode = 6000;
                //whether to send a spire relic train
                if ( BaseInfo.NumRelicsCaptured >= BaseInfo.NumRelicsRequiredForTrains )
                {
                    debugCode = 6100;
                    //only start the timer once enough relics are collected
                    if ( BaseInfo.TimeForNextSpireRelicTrain == -1 ) //initialization
                        BaseInfo.TimeForNextSpireRelicTrain = World_AIW2.Instance.GameSecond + BaseInfo.TrainIntervalFirstTrain + Context.RandomToUse.Next( 0, BaseInfo.TrainIntervalRandomness );
                    if ( World_AIW2.Instance.GameSecond > BaseInfo.TimeForNextSpireRelicTrain )
                    {
                        CreateSpireRelicTrain( AttachedFaction, Context, pathingCacheData );
                        BaseInfo.TimeForNextSpireRelicTrain = World_AIW2.Instance.GameSecond + BaseInfo.TrainInterval + Context.RandomToUse.Next( 0, BaseInfo.TrainIntervalRandomness );
                    }
                }
                debugCode = 7000;
                HandleImperialSpire( AttachedFaction, chokeStrengths, BaseInfo, Context, pathingCacheData );
                HandleJournal( AttachedFaction, Context );
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLog( "Hit exception in spire sidekick stage3 sim. debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();

                #region Tracing
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
            }
        }
        #region SpireJournal
        private void HandleJournal ( Faction faction, ArcenHostOnlySimContext Context )
        {
            int debugCode = 0;
            try{
            if ( World_AIW2.Instance.GameSecond % 5 != 0 )
                return; //only do this every so often
                debugCode = 10;
            if ( BaseInfo.CurrentRelicSpawnPlanetIdx == -1 &&
                 BaseInfo.NumRelicsCaptured == 0 )
            {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_CampaignStartMessage", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients ); //campaign start
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DS_SpireSidekick_Introduction", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients ); //campaign start
                debugCode = 20;
            }
            if ( BaseInfo.CurrentRelicSpawnPlanetIdx != -1 )
            {
                debugCode = 30;
                //there is a relic we can capture; show relevant journals
                Planet planet = World_AIW2.Instance.GetPlanetByIndex (BaseInfo.CurrentRelicSpawnPlanetIdx);
                if ( BaseInfo.NumRelicsCaptured == 0 )
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_RelicSpawn_1", string.Empty, faction, null, planet, OnClient.DoThisOnHostOnly_WillBeSentToClients ); //campaign start
                if ( BaseInfo.NumRelicsCaptured == 1 )
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_RelicSpawn_2", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                if ( BaseInfo.NumRelicsCaptured == 2 )
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_RelicSpawn_3", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );

                //if more journals are needed for more relics, add support here
            }
            if ( BaseInfo.NumRelicsCaptured == 0 )
                return; //the rest of these journal entries require a relic
                GameEntity_Squad highestCity = null;
                GameEntity_Squad newestCity = null;
                debugCode = 40;
            foreach ( GameEntity_Squad city in this.BaseInfo.SpireCities.DisplaySquads() )
            {
                debugCode = 50;
                if ( highestCity == null ||
                    city.CurrentMarkLevel > highestCity.CurrentMarkLevel )
                    highestCity = city;
                if ( newestCity == null ||
                     city.GetSecondsSinceEnteringThisPlanet() < newestCity.GetSecondsSinceEnteringThisPlanet() )
                    newestCity = city;
            }
                debugCode = 60;
            if ( BaseInfo.SpireCities.Count > this.LastNumCitiesSeenForJournal )
            {
                debugCode = 70;
                this.LastNumCitiesSeenForJournal = BaseInfo.SpireCities.Count;
                //update the num cities and play the right journal entry
                string journalString = "TSR_Spire_NumCities_" + this.LastNumCitiesSeenForJournal;
                if ( this.LastNumCitiesSeenForJournal == 1 )
                {
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( journalString, string.Empty, faction, null, newestCity.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_SimCityOverview", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_FallenSpireModules", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                }
            }
                debugCode = 80;
            if ( highestCity != null && highestCity.CurrentMarkLevel > BaseInfo.CityMarkLevelForJournal )
            {
                debugCode = 90;
                BaseInfo.CityMarkLevelForJournal = highestCity.CurrentMarkLevel;
                string journalString = "TSR_Spire_MarkLevel_" + BaseInfo.CityMarkLevelForJournal;
                if ( highestCity.CurrentMarkLevel == 2 )
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( journalString, string.Empty, faction, null, highestCity.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                if ( highestCity.CurrentMarkLevel == 3 )
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( journalString, string.Empty, faction, null, highestCity.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                if ( highestCity.CurrentMarkLevel == 4 )
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( journalString, string.Empty, faction, null, highestCity.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                if ( highestCity.CurrentMarkLevel == 5 )
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( journalString, string.Empty, faction, null, highestCity.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );

            }
            }catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in HandleJournal debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion
        #region Rangers
        private void SpawnRangers(ArcenHostOnlySimContext Context)
        {
            if (BaseInfo.SpireCities.Count == 0)
                return;
            bool debug = false;
            foreach ( GameEntity_Squad city in this.BaseInfo.SpireCities.DisplaySquads() )
            {
                if (city == null)
                    continue;
                SpireSidekickPerUnitBaseInfo data = city.TryGetExternalBaseInfoAs<SpireSidekickPerUnitBaseInfo>();
                if (data == null)
                {
                    continue;
                }
                if ( debug )
                    ArcenDebugging.LogSingleLine("checking if we can build rangers for " + city.ToStringWithPlanet(), Verbosity.DoNotShow );
                if ( city.GetIsCrippled())
                    continue;
                int rangerCapIncrease = 0;
                int direRangerCapIncrease = 0;
                int rangerIncome = 26;
                int direRangerIncome = 111;
                int baseRangerCap = 3;
                int baseDireRangerCap = 1;
                foreach ( GameEntity_Squad outpost in this.BaseInfo.RangerOutposts.DisplaySquads() )
                {
                    if ( outpost.Planet != city.Planet )
                        continue;

                    rangerCapIncrease += baseRangerCap;
                }
                foreach ( GameEntity_Squad outpost in this.BaseInfo.DireRangerOutposts.DisplaySquads() )
                {
                    if ( outpost.Planet != city.Planet )
                        continue;
                    direRangerCapIncrease += baseDireRangerCap;
                }
                //we only get income if we've specc'ed into these
                if ( rangerCapIncrease > 0 )
                {
                    data.RangerMetal += rangerIncome;
                }
                if ( direRangerCapIncrease > 0 )
                {
                    data.DireRangerMetal += direRangerIncome;
                }

                if (data.RangerMetal <= 0 &&
                     data.DireRangerMetal <= 0)
                    continue;

                data.RangerCap = 5;
                data.DireRangerCap = 2;
                data.DireRangerCap += direRangerCapIncrease;
                data.RangerCap += rangerCapIncrease;

                string tag = "";
                GameEntityTypeData typeData;
                if (data.RangerMetal > 0)
                {
                    //try to build some rangers
                    tag = "SpireBaseRanger";

                    typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, tag);
                    if (typeData == null)
                        throw new Exception("Could not find ranger with tag " + tag + ".");
                    if (data.RangerMetal >= typeData.CostForAIToPurchase)
                    {
                        data.RangerMetal -= typeData.CostForAIToPurchase;
                        if (CanBuildRanger(city, data.RangerCap))
                        {
                            ArcenPoint spawnLocation = city.Planet.GetSafePlacementPoint_AroundEntity(Context, typeData, city, FInt.FromParts(0, 005), FInt.FromParts(0, 010));
                            GameEntity_Squad ranger = this.AttachedFaction.SpawnNewUnit_ReturnNullIfMPClient(
                              Context, city.Planet, spawnLocation, typeData, city.CurrentMarkLevel,
                              this.AttachedFaction.LooseFleet, 0,
                              EntityBehaviorType.Attacker_Full, -1, null, "SpireSidekick-Ranger");
                            SpireSidekickPerUnitBaseInfo newData = ranger.CreateExternalBaseInfo<SpireSidekickPerUnitBaseInfo>("SpireSidekickPerUnitBaseInfo");
                            newData.HomeCity = LazyLoadSquadWrapper.Create(city);
                        }
                        else
                        {
                            //ArcenDebugging.LogSingleLine("Skipping building a ranger from " + city.ToString() + " because we already have " + this.BaseInfo.RangerCount.Display + "  updated metal: " + data.RangerMetal, Verbosity.DoNotShow);
                        }
                    }
                }
                if (data.DireRangerMetal > 0)
                {
                    tag = "SpireDireRanger";
                    typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, tag);
                    if (typeData == null)
                        throw new Exception("Could not find dire ranger with tag " + tag + " when considering " + city.ToStringWithPlanet());
                    if (data.DireRangerMetal >= typeData.CostForAIToPurchase)
                    {
                        data.DireRangerMetal -= typeData.CostForAIToPurchase;
                        if (CanBuildDireRanger(city, data.DireRangerCap))
                        {
                            ArcenPoint spawnLocation = city.Planet.GetSafePlacementPoint_AroundEntity(Context, typeData, city, FInt.FromParts(0, 030), FInt.FromParts(0, 050));
                            GameEntity_Squad ranger = this.AttachedFaction.SpawnNewUnit_ReturnNullIfMPClient(
                              Context, city.Planet, spawnLocation, typeData, city.CurrentMarkLevel,
                              this.AttachedFaction.LooseFleet, 0,
                              EntityBehaviorType.Attacker_Full, -1, null, "SpireSidekick-DireRanger");
                            SpireSidekickPerUnitBaseInfo newData = ranger.CreateExternalBaseInfo<SpireSidekickPerUnitBaseInfo>("SpireSidekickPerUnitBaseInfo");
                            newData.HomeCity = LazyLoadSquadWrapper.Create(city);
                            if ( debug && ranger != null )
                                ArcenDebugging.LogSingleLine("spawningRanger " + ranger.ToStringWithPlanet() + " tag " + tag, Verbosity.DoNotShow );

                        }
                    }
                }
            }
        }
        private bool CanBuildRanger(GameEntity_Squad city, int cap)
        {
            Dictionary<SafeSquadWrapper, int> rangers = BaseInfo.RangersPerCity.GetDisplayDict();
            //ArcenDebugging.LogSingleLine("How many rangers does " + city.ToStringWithPlanet() + " have? We see " + rangers.GetPairCount() + " possibilities", Verbosity.DoNotShow );
            int guardCount = 0;
            foreach ( KeyValuePair<SafeSquadWrapper, int> guards in rangers )
            {
                //ArcenDebugging.LogSingleLine("Checking rangers to see if they match " + city.ToStringWithPlanet() + " checking against " + guards.Key.GetSquad().ToString() , Verbosity.DoNotShow );
                if ( guards.Key.GetSquad() == city)
                {
                    guardCount = guards.Value;
                    break;
                }
            }

            if ( guardCount >= cap )
                return false;
            return true;
        }
        private bool CanBuildDireRanger(GameEntity_Squad city, int cap)
        {
            Dictionary<SafeSquadWrapper, int> rangers = BaseInfo.DireRangersPerCity.GetDisplayDict();
            //ArcenDebugging.LogSingleLine("How many rangers does " + city.ToStringWithPlanet() + " have? We see " + rangers.GetPairCount() + " possibilities", Verbosity.DoNotShow );
            int guardCount = 0;
            foreach ( KeyValuePair<SafeSquadWrapper, int> guards in rangers )
            {
                //ArcenDebugging.LogSingleLine("Checking rangers to see if they match " + city.ToStringWithPlanet() + " checking against " + guards.Key.GetSquad().ToString() , Verbosity.DoNotShow );
                if ( guards.Key.GetSquad() == city)
                {
                    guardCount = guards.Value;
                    break;
                }
            }

            if ( guardCount >= cap )
                return false;
            return true;
        }
        #endregion
        #region Nadir
        private void SpawnNadir(ArcenHostOnlySimContext Context)
        {
            int debugCode = 0;
            try{
                int intervalForNadirTierTwo = 280;
                int intervalForNadirTierOne = 120;
                int numTierOneToSpawn = 3;
                int timeAdjust = 0;
                int WaveInterval = 1290;
                if ( BaseInfo.Difficulty.Intensity < 5 )
                {
                    timeAdjust += (BaseInfo.Difficulty.Intensity * 5);
                }
                if ( BaseInfo.Difficulty.Intensity > 5 )
                {
                    timeAdjust -= BaseInfo.Difficulty.Intensity * 5;
                }
                intervalForNadirTierOne += timeAdjust;
                intervalForNadirTierTwo += timeAdjust;

                if ( World_AIW2.Instance.GameSecond % intervalForNadirTierOne == 0 )
                {
                    Faction aiFaction = World_AIW2.GetRandomAIFaction( Context );
                    if ( aiFaction == null )
                        return;
                    GameEntity_Squad king = FactionUtilityMethods.Instance.findKing( aiFaction );
                    if ( king == null )
                        return;
                    AISentinelsFactionBaseInfo aiBaseInfo = aiFaction.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
                    PlanetFaction pFaction = king.PlanetFaction;
                    for ( int i = 0; i < numTierOneToSpawn; i++ )
                    {
                        int rand = Context.RandomToUse.Next(0, 100 );

                        if ( rand < 20)
                        {
                            pFaction = king.Planet.GetPlanetFactionForFaction(aiBaseInfo.SubFac_Warden );
                        }
                        else if ( rand < 40)
                            pFaction = king.Planet.GetPlanetFactionForFaction(aiBaseInfo.SubFac_BorderAggression );
                        else if ( rand < 60)
                            pFaction = king.Planet.GetPlanetFactionForFaction(aiBaseInfo.SubFac_Hunter );
                        else if ( rand < 80)
                            pFaction = king.Planet.GetPlanetFactionForFaction(aiBaseInfo.SubFac_Praetorian );
                        GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "AntiDarkZenithTierOne" );
                        ArcenPoint spawnLocation = king.Planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                        GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, pFaction.Faction.CurrentGeneralMarkLevel,
                            pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "SpawnAntiSpireOne" );
                        if ( newEntity == null )
                            return;
                        newEntity.HullPointsLost = 0;
                        newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                    }
                }
                if ( World_AIW2.Instance.GameSecond % intervalForNadirTierTwo == 0 )
                {
                    Faction aiFaction = World_AIW2.GetRandomAIFaction( Context );
                    if ( aiFaction == null )
                        return;
                    GameEntity_Squad king = FactionUtilityMethods.Instance.findKing( aiFaction );
                    if ( king == null )
                        return;
                    AISentinelsFactionBaseInfo aiBaseInfo = aiFaction.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
                    PlanetFaction pFaction = king.PlanetFaction;
                    int rand = Context.RandomToUse.Next(0, 100 );
                    if ( rand < 20)
                        pFaction = king.Planet.GetPlanetFactionForFaction(aiBaseInfo.SubFac_Warden );
                    else if ( rand < 40)
                        pFaction = king.Planet.GetPlanetFactionForFaction(aiBaseInfo.SubFac_BorderAggression );
                    else if ( rand < 60)
                        pFaction = king.Planet.GetPlanetFactionForFaction(aiBaseInfo.SubFac_Hunter );
                    else if ( rand < 80)
                        pFaction = king.Planet.GetPlanetFactionForFaction(aiBaseInfo.SubFac_Praetorian );

                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "AntiDarkZenithTierTwo" );
                    ArcenPoint spawnLocation = king.Planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, pFaction.Faction.CurrentGeneralMarkLevel,
                        pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "SpawnAntiSpireTwo" );
                    if ( newEntity == null )
                        return;
                    newEntity.HullPointsLost = 0;
                    newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread

                }
                if ( World_AIW2.Instance.GameSecond % WaveInterval == 0 )
                {
                    Faction aiFaction = World_AIW2.GetRandomAIFaction( Context );
                    if ( aiFaction == null )
                        return;
                    GameEntity_Squad king = FactionUtilityMethods.Instance.findKing( aiFaction );
                    if ( king == null )
                        return;
                    AISentinelsFactionBaseInfo aiBaseInfo = aiFaction.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
                    int WaveSize = aiBaseInfo.GetSpecificBudgetThreshold( AIBudgetType.Wave, GlobalAIWorldBaseInfo.Instance.AIProgress_Effective );
                    SpawnNadirForce( king.Planet, aiBaseInfo.SubFac_BorderAggression, WaveSize, Context, AttackComposition);
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Hit exception in scourge empire spawn nadir. debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        private void SpawnNadirForce( Planet planet, Faction faction, int strength, ArcenHostOnlySimContext Context, Dictionary<GameEntityTypeData, int> AttackCompositionToFill )
        {
            AttackCompositionToFill.Clear();
            int debugCode = 0;
            try{
                bool debug = false;
                string tagOne = "AntiDarkZenithTierOne";
                string tagTwo = "AntiDarkZenithTierTwo";
                int ratio = 8;
                int origStr = strength;
                int attempts = 100;
                debugCode = 100;
                if ( debug )
                    ArcenDebugging.LogSingleLine("spawning a nadir force with strength " + strength, Verbosity.DoNotShow );
                while ( strength > 0 && attempts > 0)
                {
                    debugCode = 200;
                    string tag = tagOne;
                    GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, tagOne );
                    if ( attempts % ratio == 0 )
                        tag = tagTwo;
                    typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, tag );
                    if ( typeData == null )
                        throw new Exception("Could not find entity with tag " + tag + " in the SpawnRavagerForces code path");
                    if ( strength - typeData.CostForAIToPurchase < 0 )
                    {
                        attempts--;
                        continue;
                    }
                    strength -= typeData.CostForAIToPurchase;
                    AttackComposition[typeData]++;
                }
                foreach ( KeyValuePair<GameEntityTypeData, int> pair in AttackComposition )
                {
                    debugCode = 800;
                    int totalSquadsToSpawn = pair.Value;
                    GameEntityTypeData entityType = pair.Key;
                    int numStacksPerSquad = 0;
                    int separateSquadsToSpawn = totalSquadsToSpawn;
                    int remainder = 0;
                    int StackingCutoff = AIWar2GalaxySettingQuickAccess.StackingCutoffNPCs;
                    if (debug)
                        ArcenDebugging.LogSingleLine("\tSpawning " + totalSquadsToSpawn + " of " + pair.Key.GetDisplayName(), Verbosity.DoNotShow );
                    
                    if (StackingCutoff <= 0)
                        throw new Exception("Undefined StackingCutoffNPCs; this means that waves won't spawn");
                    if (totalSquadsToSpawn > StackingCutoff)
                    {
                        separateSquadsToSpawn = StackingCutoff;
                        numStacksPerSquad = totalSquadsToSpawn / separateSquadsToSpawn;
                        remainder = totalSquadsToSpawn % separateSquadsToSpawn;
                    }
                    debugCode = 900;
                    for (int j = 0; j < separateSquadsToSpawn; j++)
                    {
                        debugCode = 1000;
                        FInt minRadius = FInt.FromParts(0, 600);
                        FInt maxRadius = FInt.FromParts(0, 800);
                        
                        ArcenPoint spawnLocation = Engine_AIW2.Instance.CombatCenter;
                        spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity(Context, entityType, spawnLocation, minRadius, maxRadius);
                        byte markLevel = (byte)(FactionUtilityMethods.Instance.GetCurrentAIP() / 60);
                        if (markLevel < 1)
                            markLevel = 1;
                        if (markLevel > 7)
                            markLevel = 7;
                        GameEntity_Squad entity = faction.SpawnNewUnit_ReturnNullIfMPClient(
                            Context, planet, spawnLocation, entityType, markLevel,
                            this.AttachedFaction.LooseFleet, 0,
                            EntityBehaviorType.Attacker_Full, -1, null, "NadirAttackDeployment");
                        
                        if (entity == null)
                            continue;
                        
                        if (numStacksPerSquad > 0)
                        {
                            if (totalSquadsToSpawn < numStacksPerSquad)
                            {
                                entity.AddOrSetExtraStackedSquadsInThis((Int16)totalSquadsToSpawn, true);
                            }
                            else
                                entity.AddOrSetExtraStackedSquadsInThis((Int16)(numStacksPerSquad - 1), true); //don't count the original unit
                            if (remainder > 0)
                            {
                                entity.AddOrSetExtraStackedSquadsInThis(1, false);
                                remainder--;
                            }
                            totalSquadsToSpawn -= entity.ExtraStackedSquadsInThis + 1;
                        }
                    }
                };
            }catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in SpawnNadir debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion Nadir
        #region ConvertRelicIntoCity
        private void ConvertRelicIntoCity( Faction AttachedFaction, GameEntity_Squad playerKing, GameEntity_Squad relic, SpireSidekickPerUnitBaseInfo relicData,
            ArcenHostOnlySimContext Context, ref FInt responseStrength )
        {
            int debugCode = 0;
            try
            {
                //The spire relic is turning into a spire city
                //set the time for the next relic spawn and other globals

                debugCode = 100;
                //If we already have a spire city here, abort
                bool foundExistingCity = false;
                foreach ( GameEntity_Squad city in this.BaseInfo.SpireCities.DisplaySquads() )
                {
                    if ( city.Planet == relic.Planet )
                    {
                        relicData.DestinationPoint = ArcenPoint.ZeroZeroPoint;
                        relicData.DestinationPlanet = null;
                        foundExistingCity = true;
                        break;
                    }
                }

                debugCode = 200;
                if ( foundExistingCity )
                {
                    if ( ArcenNetworkAuthority.GetIsHostMode() )
                    {
                        PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                        if ( chatHandlerOrNull != null )
                            chatHandlerOrNull.PlanetToView = relic.Planet;

                        World_AIW2.Instance.QueueChatMessageOrCommand( "A new city can't be built on " + relic.GetPlanetName_Safe() + " since there is already a city there.", 
                            ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                    }
                    return;
                }

                debugCode = 300;
                //We can legally place a city here
                BaseInfo.TimeForNextRelicSpawn = World_AIW2.Instance.GameSecond + BaseInfo.RelicSpawnInterval + Context.RandomToUse.Next( 0, BaseInfo.RelicSpawnIntervalRandomness );
                BaseInfo.NumRelicsCaptured++;
                //Now spawn the Spire City
                if ( ArcenNetworkAuthority.GetIsHostMode() )
                {
                    PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.PlanetToView = relic.Planet;

                    World_AIW2.Instance.QueueChatMessageOrCommand( "The Spire Relic has arrived safely on " + relic.GetPlanetName_Safe(), ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                }
                bool useGalacticCapitol = false;
                if ( BaseInfo.NumRelicsCaptured == 1 )
                    useGalacticCapitol = true;
                GameEntity_Squad newCity = TransformRelicToSpireStructure_OrNull( relic, Context, useGalacticCapitol );
                if ( newCity != null )
                    BaseInfo.SpireCities.AddToDisplayList( newCity );
                debugCode = 500;
                //Set up debris spawning
                for ( int k = 0; k < BaseInfo.Difficulty.DebrisToSpawnPerRelic; k++ )
                    BaseInfo.TimesForNextSpireDebris.Add( BaseInfo.DebrisSpawnDelay + Context.RandomToUse.Next( BaseInfo.DebrisSpawnDelayRandomness / 10, BaseInfo.DebrisSpawnDelayRandomness ) );

                debugCode = 600;
                //Spawn a new exo based on the relic response strength
                List<SafeSquadWrapper> workingTargets = GameEntity_Squad.GetTemporarySquadList( "SpireSidekick-ConvertRelicIntoCity-workingTargets", 10f );
                if ( workingTargets == null ) //blocked for teardown/shutdown; bail
                    return;

                workingTargets.Add( playerKing );
                foreach ( GameEntity_Squad city in this.BaseInfo.SpireCities.DisplaySquads() )
                {
                    if ( Context.RandomToUse.Next( 0, 100 ) < 20 && !city.GetIsCrippled() )
                        workingTargets.Add( city );
                }

                debugCode = 700;
                int finalExoStrengthMultiplier = 3;
                ExoOptions options = ExoOptions.CreateWithDefaults( workingTargets, responseStrength.IntValue * finalExoStrengthMultiplier, null, AttachedFaction );
                if ( BaseInfo.SpireCities.Count > 3 ) //this number could be in the BaseInfo.Difficulty
                    options.newExoLeaderTag = "ExtragalacticWar";
                options.distanceFromTargetOverride = 2;
                options.exoText = "The AI is sending a powerful strike against you and your new city.";
                ExoGalacticAttackManager.SendExoGalacticAttack( options, Context );
                SpawnDragons( Context );
                GameEntity_Squad.ReleaseTemporarySquadList( workingTargets );

                debugCode = 800;
                //And make sure the AIP is adjusted properly
                FInt aipAdjustment = FInt.FromParts(0, 000);
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction localFaction = World_AIW2.Instance.Factions[i];

                    if ( localFaction.Type == FactionType.Player )
                    {
                        PlanetFaction localPlanetFaction = relic.Planet.GetPlanetFactionForFaction( localFaction );
                        if ( localPlanetFaction.AIPLeftFromWarpGate != 0 )
                        {
                            if ( localFaction == this.AttachedFaction )
                                aipAdjustment += localPlanetFaction.AIPLeftFromWarpGate;
                            localPlanetFaction.AIPLeftFromWarpGate = 0;
                        }
                        if ( localPlanetFaction.AIPLeftFromCommandStation != 0 )
                        {
                            if ( localFaction == this.AttachedFaction )
                                aipAdjustment += localPlanetFaction.AIPLeftFromCommandStation;

                            localPlanetFaction.AIPLeftFromCommandStation = 0;
                        }
                    }
                }
                if ( aipAdjustment != FInt.FromParts(0, 000))
                GlobalAIWorldBaseInfo.Instance.ChangeAIP( aipAdjustment, AIPChangeReason.PlanetCapture, null, this.AttachedFaction.FactionIndex, relic.Planet.Index,  this.AttachedFaction.FactionIndex );
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in ConvertRelicIntoCity. debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        #endregion

        void CreateSpireRelicTrain( Faction faction, ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //don't even try this on clients

            //pick an AI faction
            //pick a good place to spawn the relic train
            //pick a number of hops to make and set the destination to be the AI King
            //create the relic train and give it to that AI faction
            //in AI long range planning, handle ship movement
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "SpireRelicTrain" );
            Faction AIFaction = World_AIW2.GetRandomAIFaction( Context );
            Planet startPlanet = GetRelicTrainPlanet( AIFaction, null, 2, 5, Context, PathCacheData );
            if ( startPlanet == null )
            {
                return;
            }
            PlanetFaction pFaction = startPlanet.GetPlanetFactionForFaction( AIFaction );
            int minRadius = (startPlanet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 150 )).IntValue;
            int maxRadius = (startPlanet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 300 )).IntValue;
            ArcenPoint spawnLocation = startPlanet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 300 ) );
            GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                                                     pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "SpireSidekick-NewRelicTrain" );
            SpireSidekickPerUnitBaseInfo data = newEntity.CreateExternalBaseInfo<SpireSidekickPerUnitBaseInfo>( "SpireSidekickPerUnitBaseInfo" );
            data.HopsLeftForTrain = (Int16)(2 + Context.RandomToUse.Next( 1, 4 ));
            ArcenCharacterBuffer workingBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "SpireSidekick-CreateSpireRelicTrain-workingBuffer", 10f );
            workingBuffer.Add( "A " ).Add( newEntity.TypeData.GetDisplayName(), AIFaction.FactionCenterColor.ColorHexBrighter ).Add( " has spawned on " );
            if ( startPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                workingBuffer.Add( startPlanet.Name );
            else
                workingBuffer.Add( "a nearby planet" );
            workingBuffer.Add( " and will travel between " ).Add( data.HopsLeftForTrain, "a1ffa1" ).Add( " various destinations until going to the AI Homeworld." );
            World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_SpireRelicTrain", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            if ( ArcenNetworkAuthority.GetIsHostMode() )
            {
                SquadViewChatHandlerBase chatHandlerOrNull = null;
                if ( startPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                {
                    chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( newEntity );
                }
                World_AIW2.Instance.QueueChatMessageOrCommand( workingBuffer.ToStringAndReturnToPool(), ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
            }
            else
                workingBuffer.ReturnToPool();
        }

        public Planet GetRelicTrainPlanet( Faction faction, Planet startPlanetOrNull, Int16 minHopsToPlayerWorld, Int16 maxHopsFromPlayerWorld, ArcenSimContextAnyStatus Context, PerFactionPathCache PathCacheData )
        {
            List<Planet> workingPlanetList = Planet.GetTemporaryPlanetList( "SpireSidekick-GetRelicTrainPlanet-workingBuffer", 10f );
            if ( workingPlanetList == null ) //blocked for teardown/shutdown; bail
                return null;

            //note this can be called from either the Sim thread or the Long Range Planning thread
            Planet planetToReturn = null;
            //called by the Relic Train routing in the AI faction
            bool debug = false;
            int retries = 0;
            int maxRetries = 6; //was 100, and that's likely to break the game in the late game.
            if ( startPlanetOrNull != null && debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Finding the next stop for the relic train on  " + startPlanetOrNull.Name, Verbosity.DoNotShow );

            int preferredHopsFromCurrent = 6;
            if ( startPlanetOrNull == null ) //if we know our starting planet, try to choose the next planet some ways away
                preferredHopsFromCurrent = -1;
            bool restrictOnlyMyPlanets = true;
            do
            {
                int numAIPlanets = 0;
                int numallowedAIPlanets = 0;
                if ( retries > 0 )
                {
                    //the previous attempt was too restrictive
                    minHopsToPlayerWorld--;
                    maxHopsFromPlayerWorld++;
                    preferredHopsFromCurrent--;
                    if ( minHopsToPlayerWorld <= 1 )
                        minHopsToPlayerWorld = 1;
                    if ( retries > 3 )
                        restrictOnlyMyPlanets = false;
                }
                retries++;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Checking for a planet owned by " + faction.GetDisplayName() + " " + minHopsToPlayerWorld + " min hops from a player world and " + maxHopsFromPlayerWorld + " max hops from player world, retries " + retries, Verbosity.DoNotShow );
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
                    if ( planet.GetControllingFactionType() == FactionType.AI )
                        numAIPlanets++;
                    if ( planet.GetControllingFaction() != faction && restrictOnlyMyPlanets ) //the planet must be owned by this faction
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( planet.Name + " is not owned by my faction " + faction.GetDisplayName() + " idx " + faction.FactionIndex + ". Are we restricted to only my planets? " + restrictOnlyMyPlanets, Verbosity.DoNotShow );
                        continue;
                    }
                    numallowedAIPlanets++;
                    if ( preferredHopsFromCurrent > 0 &&
                         planet.GetHopsTo(startPlanetOrNull) < preferredHopsFromCurrent )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( planet.Name + " is within " + preferredHopsFromCurrent + " hops of the current planet", Verbosity.DoNotShow );
                            continue;
                    }
                    if ( minHopsToPlayerWorld > 1 )
                    {
                        //this planet must not be too close to a player planet
                        //to make the bounds listed above inclusive minHops - 1 must be used
                        bool foundPlayerPlanetWithinMinHops = false;
                        foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops_NoFilters( (Int16)(minHopsToPlayerWorld - 1) ) )
                        {
                            Planet otherPlanet = _phd.Planet;
                            if ( otherPlanet.GetControllingFactionType() == FactionType.Player )
                                foundPlayerPlanetWithinMinHops = true;
                        }
                        if ( foundPlayerPlanetWithinMinHops )
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( planet.Name + " is within " + minHopsToPlayerWorld + " hops of a player world ", Verbosity.DoNotShow );
                            continue;
                        }
                    }
                    if ( maxHopsFromPlayerWorld > 0 )
                    {
                        //this planet can't be too far from a player planet
                        bool foundPlayerPlanetWithinMaxHops = false;
                        foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops_NoFilters( maxHopsFromPlayerWorld ) )
                        {
                            Planet otherPlanet = _phd.Planet;
                            if ( otherPlanet.GetControllingFactionType() == FactionType.Player )
                                foundPlayerPlanetWithinMaxHops = true;
                        }
                        if ( !foundPlayerPlanetWithinMaxHops )
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( planet.Name + " is not within " + maxHopsFromPlayerWorld + " hops of a player world ", Verbosity.DoNotShow );

                            continue;
                        }
                    }
                    workingPlanetList.Add( planet );
                }
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "After scanning all planets, there are " + workingPlanetList.Count + " possible planets, and " + numAIPlanets + " ai planets (of these, there were " + numallowedAIPlanets + " allowed to me", Verbosity.DoNotShow );
                if ( numAIPlanets <= 5 )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Too few AI planets; don't spawn a train", Verbosity.DoNotShow );

                    Planet.ReleaseTemporaryPlanetList( workingPlanetList );
                    return null;
                }
                if ( startPlanetOrNull != null && workingPlanetList.Count > 0 )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "culling the planets list to make sure there's a safe path there from " + startPlanetOrNull.Name, Verbosity.DoNotShow );
                    for ( int i = workingPlanetList.Count - 1; i >= 0; i-- )
                    {
                        PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "SpireSidekickGetRelicTrainPlanet", startPlanetOrNull, workingPlanetList[i], PathingMode.Safest, Context, PathCacheData );
                        if ( pathCache != null )
                        {
                            for ( int j = 0; j < pathCache.PathToReadOnly.Count; j++ )
                            {
                                if ( pathCache.PathToReadOnly[j].GetControllingOrInfluencingFaction().GetIsHostileTowards( faction ) )
                                {
                                    //if this path doesn't work, remove the planet from the main list
                                    if ( debug )
                                        ArcenDebugging.ArcenDebugLogSingleLine( "removing " + workingPlanetList[i].Name + " since there best path to that goes through " + pathCache.PathToReadOnly[j].Name + " which is hostile", Verbosity.DoNotShow );

                                    workingPlanetList.RemoveAt( i );
                                    break;
                                }
                            }
                        }
                    }
                }
                if ( workingPlanetList.Count > 0 )
                    planetToReturn = workingPlanetList[Context.RandomToUse.Next( 0, workingPlanetList.Count )];

            } while ( planetToReturn == null && retries < maxRetries );
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Chose " + planetToReturn.Name + " for the relic train\n", Verbosity.DoNotShow );

            Planet.ReleaseTemporaryPlanetList( workingPlanetList );
            return planetToReturn;
        }

        private Int16 GetNextRelicSpawnPoint( Faction AttachedFaction, ArcenHostOnlySimContext Context, bool isDebris )
        {
            List<Planet> workingPlanetList = Planet.GetTemporaryPlanetList( "SpireSidekick-GetNextRelicSpawnPoint-workingBuffer", 10f );
            if ( workingPlanetList == null ) //blocked for teardown/shutdown; bail
                return -1;

            //returns the planet index of where the relic will go. Also currently reused for the spire debris
            bool debug = false;
            Int16 minHopsFromHumanPlanet = -1;
            Int16 maxHopsFromHumanPlanet = -1;
            byte maxMarkLevel = 2;
            if ( isDebris )
            {
                //debris has slightly different rules
                minHopsFromHumanPlanet = 2;
                maxHopsFromHumanPlanet = 4;
                maxMarkLevel = 4;
            }
            else if ( BaseInfo.NumRelicsCaptured < 1 )
            {
                minHopsFromHumanPlanet = 1;
                maxHopsFromHumanPlanet = 2;
                maxMarkLevel = 2;
            }
            else if ( BaseInfo.NumRelicsCaptured < 3 )
            {
                minHopsFromHumanPlanet = 2;
                maxHopsFromHumanPlanet = 4;
                maxMarkLevel = 3;
            }
            else if ( BaseInfo.NumRelicsCaptured < 5 )
            {
                minHopsFromHumanPlanet = 4;
                maxHopsFromHumanPlanet = 5;
                maxMarkLevel = 5;
            }
            else
            {
                minHopsFromHumanPlanet = 6;
                maxMarkLevel = 7;
            }
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "minHopsFromHumanPlanet " + minHopsFromHumanPlanet + " max hops " + maxHopsFromHumanPlanet + " mark level " + maxMarkLevel, Verbosity.DoNotShow );
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
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                    if ( planet.IntelLevel == PlanetIntelLevel.Unexplored )
                        continue; //don't put anything on unexplored planets
                    Faction controllingFaction = planet.GetControllingOrInfluencingFaction();
                    if ( controllingFaction.Type == FactionType.Player )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because the planet is player owned", Verbosity.DoNotShow );
                        continue;
                    }
                    if ( planet.MarkLevelForAIOnly.Ordinal > maxMarkLevel )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because its mark level " + planet.MarkLevelForAIOnly.Ordinal + " > allowed mark level " + maxMarkLevel, Verbosity.DoNotShow );
                        continue;
                    }
                    bool adjacentKing = false;
                    if ( planet.GetDataByStanceForFaction( AttachedFaction, FactionStance.Friendly ).HasKingUnitPresent ||
                         planet.GetDataByStanceForFaction( AttachedFaction, FactionStance.Hostile ).HasKingUnitPresent )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because it is a king planet", Verbosity.DoNotShow );
                        continue;
                    }
                    foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                    {
                        if ( neighbor.GetDataByStanceForFaction( AttachedFaction, FactionStance.Friendly ).HasKingUnitPresent ||
                             neighbor.GetDataByStanceForFaction( AttachedFaction, FactionStance.Hostile ).HasKingUnitPresent )
                            adjacentKing = true;
                    }
                    if ( adjacentKing == true )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because its adjacent to a king planet", Verbosity.DoNotShow );
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
                            Faction otherFaction = otherPlanet.GetControllingOrInfluencingFaction();
                            if ( otherFaction.Type == FactionType.Player )
                                foundPlayerPlanetWithinMinHops = true;
                        }
                        if ( foundPlayerPlanetWithinMinHops )
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because too close to player", Verbosity.DoNotShow );

                            continue;
                        }
                    }
                    if ( maxHopsFromHumanPlanet > 0 )
                    {
                        //this planet can't be too far from a player planet
                        bool foundPlayerPlanetWithinMaxHops = false;
                        foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops_NoFilters( maxHopsFromHumanPlanet ) )
                        {
                            Planet otherPlanet = _phd.Planet;
                            Faction otherFaction = otherPlanet.GetControllingOrInfluencingFaction();
                            if ( otherFaction.Type == FactionType.Player )
                                foundPlayerPlanetWithinMaxHops = true;
                        }
                        if ( !foundPlayerPlanetWithinMaxHops )
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because too far from player (maxHops " + maxHopsFromHumanPlanet + ")", Verbosity.DoNotShow );
                            
                            continue;
                        }
                    }
                    workingPlanetList.Add( planet );
                }
                retries++;
            } while ( workingPlanetList.Count == 0 && retries < allowedRetries );
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Choosing from " + workingPlanetList.Count + " planets to spawn relic", Verbosity.DoNotShow );
            if ( workingPlanetList.Count <= 0 )
            {
                Planet.ReleaseTemporaryPlanetList( workingPlanetList );
                return -1;
            }
            Int16 ret = workingPlanetList[Context.RandomToUse.Next( 0, workingPlanetList.Count )].Index;
            Planet.ReleaseTemporaryPlanetList( workingPlanetList );
            return ret;
        }
        
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.FallenSpire );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "SpireSidekick-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            int debugCode = 0;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                if ( BaseInfo == null || BaseInfo.Teams == null )
                    return;
                LRPActiveRelics.Clear();
                AutoDefendingShipsLRP.Clear();
                WorkingPlanetsLRP.Clear();
                UnassignedShips.Clear();
                KingKillersByPlanet.Clear();
                TeamsAimedAtPlanet.Clear();
                AlliedCommandStations.Clear();
                FriendlyPlanets.Clear();
                debugCode = 20;
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
                {
                    debugCode = 40;
                    if ( team != null )
                        team.DeepInfo.Reset(); //reset team count information (I think this can be null right after game load?)
                }
                debugCode = 100;
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( planet.GetControllingFaction().GetIsFriendlyTowards( AttachedFaction ) )
                    {
                        GameEntity_Squad commandStation = planet.GetCommandStationOrNull();
                        if ( commandStation != null )
                            AlliedCommandStations.Add( commandStation );
                        FriendlyPlanets.Add( planet );
                    }
                }
                debugCode = 200;
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    debugCode = 300;
                    if ( entity.TypeData.GetHasTag( "SpireSidekickRelic" ) )
                    {
                        LRPActiveRelics.Add( entity );
                        continue;
                    }
                    if ( entity.TypeData.GetHasTag("AutoDefenseShip") )
                    {
                        AutoDefendingShipsLRP.Add(entity);
                        continue;
                    }

                    if ( entity.TypeData.GetHasTag( "SpireSidekickDebris" ) )
                        continue;
                    if ( entity.TypeData.GetHasTag( "KingKiller" ) )
                    {
                        //TODO: remove this and check the actual fireteam logic, since they shouldn't be so cowardly.
                        //I might need to let factions set their aggressiveness tunables, or to make factions
                        //happier to attack non-players.
                        //Then add this back in to test the new logic
                        if ( entity.HasQueuedOrders() )
                            continue; //if we are going somewhere, don't get new orders
                        KingKillersByPlanet[entity.Planet].Add( entity );
                        continue;
                    }
                    if ( !entity.TypeData.GetHasTag( "ImperialSpire" ) )
                        continue;

                    if ( entity.FireteamId < 0 )
                        UnassignedShips.Add( entity );
                    else
                    {
                        Fireteam team = this.BaseInfo.GetFireteamById( entity.FireteamId );
                        if ( team != null )
                            team.DeepInfo.AddUnit( entity );
                        else
                            entity.FireteamId = -1; //something happened to the fireteam, so lets find a new one next LRP stage
                    }

                }
                debugCode = 400;
                for ( int i = 0; i < LRPActiveRelics.Count; i++ )
                {
                    debugCode = 500;
                    //Handle any active relics
                    GameEntity_Squad relic = LRPActiveRelics[i].GetSquad();
                    if ( relic == null )
                        continue;
                    SpireSidekickPerUnitBaseInfo data = relic.TryGetExternalBaseInfoAs<SpireSidekickPerUnitBaseInfo>();
                    if ( data.FactionThatFoundThisRelic == -1 )
                        throw new Exception( relic.ToString() + " does not know which faction found it!\n" );
                    Faction destFaction = World_AIW2.Instance.GetFactionByIndex( data.FactionThatFoundThisRelic );
                    Planet destPlanetLocal = data.DestinationPlanet;
                    ArcenPoint destPointLocal = data.DestinationPoint;
                    if ( destPlanetLocal == null )
                        continue; //if the destination has not been set yet, do nothing
                    if ( destPointLocal == ArcenPoint.ZeroZeroPoint )
                        continue; //if the destination has not been set yet, do nothing
                    debugCode = 600;
                    if ( relic.Planet != destPlanetLocal )
                        FactionUtilityMethods.Instance.Helper_RaidSpecificPlanet( relic, relic.Planet, AttachedFaction, World_AIW2.Instance.CurrentGalaxy, destPlanetLocal, false, Context, pathingCacheData, 5f );
                    else
                    {
                        GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCWander], GameCommandSource.AnythingElse );
                        moveCommand.PlanetOrderWasIssuedFrom = relic.Planet.Index;
                        moveCommand.RelatedPoints.Add( destPointLocal );
                        moveCommand.RelatedEntityIDs.Add( relic.PrimaryKeyID );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );
                    }
                }
                if ( BaseInfo.ImperialFleetActive )
                    FactionUtilityMethods.Instance.FlushUnitsFromReinforcementPointsOnAllRelevantPlanets( AttachedFaction, Context, 5f );
                debugCode = 700;
                FInt overkillRequired = FInt.FromParts( 0, 850 );
                FireteamUtility.UpdateFireteams( AttachedFaction, Context, pathingCacheData, BaseInfo.Teams, TeamsAimedAtPlanet, tracingBuffer, overkillRequired );
                debugCode = 800;
                FireteamUtility.UpdateRegiments( AttachedFaction, Context, pathingCacheData, BaseInfo.Teams, TeamsAimedAtPlanet, tracingBuffer, this.MinFireteamStrength, true );
                debugCode = 900;
                for ( int i = 0; i < UnassignedShips.Count; i++ )
                    AssignUnitToFireteam( AttachedFaction, UnassignedShips[i].GetSquad(), Context, pathingCacheData );
                debugCode = 1000;
                //And now handle KingKillers; these units will just try to kill an AI king
                //so we'll have one set of units spreading out and generally attacking, and others going directly for a king. Should be interesting
                foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in KingKillersByPlanet )
                {
                     debugCode = 1100;
                     Planet planet = pair.Key;
                     if ( tracing )
                         tracingBuffer.Add( "We have " + pair.Value.Count + " king killers on " + planet.Name ).Add( "\n" );

                     debugCode = 1200;
                     var factionData = planet.GetStanceDataForFaction( AttachedFaction );
                     if ( factionData[FactionStance.Hostile].TotalStrength > 100 * 1000 )
                     {
                         if ( tracing )
                             tracingBuffer.Add( "\tThere are too many enemies here; stay and fight\n" );
                         continue; //if there are a reasonable number of enemies, fight them. Smaller numbers can be ignored
                    }
                     debugCode = 1300;
                     Planet dest = FactionUtilityMethods.Instance.GetKingKillerTarget( planet, Context );
                     if ( dest == null)
                     {
                         if ( tracing )
                             tracingBuffer.Add( "\tno ai factions found\n" );

                         continue;
                     }

                     debugCode = 1500;
                     PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "SpireSidekickKingKillersLRP", planet, dest, PathingMode.Shortest, Context, pathingCacheData );
                     debugCode = 1600;
                     if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                     {
                         if ( tracing )
                         {
                             if ( pathCache.PathToReadOnly.Count > 1 )
                                 tracingBuffer.Add( "\tfinding a path to " + dest.Name + ". 1 " + pathCache.PathToReadOnly[0].Name + " 2 " + pathCache.PathToReadOnly[1].Name + "\n" );
                             else
                                 tracingBuffer.Add( "\tfinding a path to " + dest.Name + ". 1 " + pathCache.PathToReadOnly[0].Name + "\n" );
                         }
                         debugCode = 1700;
                         Planet nextPlanet = pathCache.PathToReadOnly[1];
                         debugCode = 1800;
                         for ( int i = 0; i < pathCache.PathToReadOnly.Count; i++ )
                         {
                             debugCode = 1900;
                            //find the next enemy planet on the way to this king, then go there
                            nextPlanet = pathCache.PathToReadOnly[i];
                             factionData = nextPlanet.GetStanceDataForFaction( AttachedFaction );
                             if ( nextPlanet.GetControllingFaction().GetIsHostileTowards( AttachedFaction ) ||
                                  factionData[FactionStance.Hostile].TotalStrength > 100 * 1000 )
                                 break;
                         }
                         debugCode = 2000;
                         if ( tracing )
                             tracingBuffer.Add( "\tHeading to " + dest.Name + " but next stop, " + nextPlanet.Name );
                         FactionUtilityMethods.Instance.Helper_RaidSpecificPlanet( pair.Value, planet, AttachedFaction,
                                                                               World_AIW2.Instance.CurrentGalaxy, nextPlanet, true, Context, pathingCacheData, 5f );
                     }
                }

                //Below is the spire auto-defend mode
                for (int i = 0; i < AutoDefendingShipsLRP.Count; i++ )
                {
                    /* This is a very simple implementation. There's a ton of fancy things we could do, like 
                    being able to do multiple passes to find categories of battles (defensive/offensive), and prioritizing defense (or offense).
                    Another good improvement would be being able to make more informed choices that aren't "Just go to the closest battle".
                    Another good improvement would be letting fleets go to "threatened" planets (ie planets with incoming waves, or lots of hostile mobile forces nearby)

                   For that matter, there's no principled reason we couln't give a "Offensive" mode, where we could kill Instigator bases,
                   AIP reducers, or just neutering enemy planets.
                     */
                    GameEntity_Squad defenseShip = AutoDefendingShipsLRP[i].GetSquad();
                    if ( defenseShip == null )
                        continue;
                    Fleet fleet = defenseShip.FleetMembership.Fleet;
                    if (fleet == null)
                    {
                        continue;
                    }
                    SpireSidekickPerUnitBaseInfo unitData = defenseShip.TryGetExternalBaseInfoAs<SpireSidekickPerUnitBaseInfo>();
                    GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                    
                    Planet dest = defenseShip.GetDestinationPlanet();
                    //bool goToShipyard = false; //these double as "retreat"
                    bool goToHomeCity = false;
                    debugCode = 1500;
                    int myStrength;
                    myStrength = defenseShip.GetStrengthPerSquad();

                    debugCode = 1400;

                    bool debug = false;

                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("Doing LRP update for " + defenseShip.ToStringWithPlanet(), Verbosity.DoNotShow );

                    debugCode = 1600;
                    if (AutoDefendUtility.IsLosingBattle(defenseShip.Planet, myStrength, this.AttachedFaction ))
                    {
                        debugCode = 1700;
                        goToHomeCity = true;
                    }
                    GameEntity_Squad homeCity = null;
                    if ( unitData != null &&
                         defenseShip.TypeData.AllowedHopsFromCenterpiece > -1 )
                    {
                        homeCity = unitData.HomeCity.GetSquad();
                        if ( homeCity.Planet.GetHopsTo( defenseShip.Planet ) > defenseShip.TypeData.AllowedHopsFromCenterpiece )
                            goToHomeCity = true;
                    }
                    debugCode = 1750;
                    if (goToHomeCity && homeCity != null )
                    {
                        Planet cityPlanet = homeCity.Planet;
                        if (cityPlanet != null)
                        {
                            if (debug)
                                ArcenDebugging.ArcenDebugLogSingleLine("\tHeading to " + cityPlanet.Name + ", its home city", Verbosity.DoNotShow);

                            AutoDefendUtility.GoToPlanet(defenseShip, cityPlanet, Context, pathingCacheData);
                        }
                    }

                    if ( defenseShip.GetIsCrippled() )
                        continue;
                    debugCode = 2100;
                    if ( AutoDefendUtility.IsPlanetUnderAttack(dest, this.AttachedFaction) )
                    {
                        debugCode = 2200;
                        if (debug)
                        {
                            if ( dest == defenseShip.Planet )
                                ArcenDebugging.ArcenDebugLogSingleLine("\tWe are already in a battle!", Verbosity.DoNotShow);
                            else
                                ArcenDebugging.ArcenDebugLogSingleLine("\tWe are en route to a battle at " + dest.Name, Verbosity.DoNotShow);
                        }

                        continue;
                    }
                    debugCode = 2300;
                    //Check if we have any battles to go to!
                    WorkingPlanetsLRP.Clear();
                    if ( homeCity != null ) 
                        AutoDefendUtility.GetPlanetsUnderAttack( defenseShip, myStrength, homeCity.Planet, WorkingPlanetsLRP); //we are defending around a specific city; Guardians
                    else
                        AutoDefendUtility.GetPlanetsUnderAttack(defenseShip, myStrength, defenseShip.Planet, WorkingPlanetsLRP); //we are "generic defense"; defensive flagships
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\tWe have found " + WorkingPlanetsLRP.Count + " planets under attack", Verbosity.DoNotShow );
                    debugCode = 2400;
                    if ( WorkingPlanetsLRP.Count > 0 )
                    {
                        debugCode = 2500;
                        //The target's priority is chosen in GetPlanetsUnderAttack (so technically we could just pass back a single planet), but we use a List for future-proofing
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tWe are heading to " + WorkingPlanetsLRP[0].Name + " to help defend", Verbosity.DoNotShow );

                        AutoDefendUtility.GoToPlanet( defenseShip, WorkingPlanetsLRP[0], Context, pathingCacheData);
                        continue;
                    }
                    debugCode = 2600;
                    //We haven't found an ongoing battle to assist in, so see if we have a threatened planet we can defend
                    WorkingPlanetsLRP.Clear();
                    if ( AutoDefendUtility.IsPlanetThreatened( dest, this.AttachedFaction ))
                    {
                        debugCode = 2700;
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tWe are already on a threatened planet!", Verbosity.DoNotShow );
                        continue;
                    }
                    AutoDefendUtility.GetThreatenedPlanets( myStrength, defenseShip.Planet, WorkingPlanetsLRP, this.AttachedFaction);
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\tWe have found " + WorkingPlanetsLRP.Count + " planets threatened", Verbosity.DoNotShow );
                    debugCode = 2800;
                    if ( WorkingPlanetsLRP.Count > 0 )
                    {
                        Planet threatenedPlanet = WorkingPlanetsLRP[Context.RandomToUse.Next(0, WorkingPlanetsLRP.Count)];
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tWe are heading to " + threatenedPlanet.Name + " because the planet is threatened", Verbosity.DoNotShow );
                    
                        AutoDefendUtility.GoToPlanet( defenseShip, threatenedPlanet, Context, pathingCacheData);
                        continue;
                    }
                    debugCode = 2900;
                    if ( dest != defenseShip.Planet )
                        continue; //we don't have any more urgent objectives, and we are en route someplace
                    if ( dest == defenseShip.Planet )
                    {
                        if ( defenseShip.HasQueuedOrders() )
                            continue;
                        debugCode = 3000;
                        //Patrolling: head to a randomly chosen other shipyard, or someplace on this planet
                        if ( Context.RandomToUse.Next(0, 100) < 20 )
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine("\tWe are heading to part of of " + defenseShip.Planet.Name + " to patrol", Verbosity.DoNotShow );
                            FInt minRadius = FInt.FromParts( 0, 100 );
                            FInt maxRadius = FInt.FromParts( 0, 900 );

                            ArcenPoint patrolPoint = defenseShip.Planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, defenseShip.TypeData, Engine_AIW2.Instance.CombatCenter, minRadius, maxRadius );
                            GoToLocation(defenseShip, patrolPoint, Context, pathingCacheData);
                            continue;
                        }
                        Planet friendlyPlanet = AutoDefendUtility.GetRandomPlanetForPatrol(defenseShip, FriendlyPlanets, WorkingPlanetsLRP, Context, pathingCacheData);
                        if (friendlyPlanet != null)
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine("\tWe are heading to " + friendlyPlanet.Name + " to patrol", Verbosity.DoNotShow );

                            AutoDefendUtility.GoToPlanet(defenseShip, friendlyPlanet, Context, pathingCacheData);
                            continue;
                        }
                    }
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\tNothing to do?", Verbosity.DoNotShow );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in spire sidekick LRP, debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();

                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " " + AttachedFaction.FactionIndex + " DoLongRangePlanning trace ends" );
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
            }
        }
        private void GoToLocation( GameEntity_Squad squad, ArcenPoint location, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathCacheData )
        {
            //We have the metal and the structure has the experience, so fly to it so we can upgrade it
            GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse );
            moveCommand.PlanetOrderWasIssuedFrom = squad.Planet.Index;
            moveCommand.RelatedPoints.Add( location );
            moveCommand.RelatedEntityIDs.Add( squad.PrimaryKeyID );
            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );
        }

        //prefer close ones that you can get to safely
        private void AssignUnitToFireteam( Faction faction, GameEntity_Squad entity, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( entity == null )
                return;

            AvailableFireteams.Clear();
            bool debug = false;
            if ( BaseInfo.Teams.GetItemCount() == 0 )
            {
                Fireteam team = Fireteam.CreateNewWithIDFromList( BaseInfo.Teams );
                team.StrengthToBringOnline = this.MinFireteamStrength + Context.RandomToUse.Next( 0, MaxFireteamStrength - MinFireteamStrength );
                team.MyStrengthMultiplierForStrengthCalculation = FInt.FromParts(1, 100);
                team.EnemyStrengthMultiplierForStrengthCalculation = FInt.FromParts(1, 000);
                BaseInfo.Teams.AddIfNotAlreadyIn( team );
            }
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
            {
                if ( team.status == FireteamStatus.Disbanded ||
                        team.status == FireteamStatus.ReadyToAttack ||
                        team.status == FireteamStatus.Attacking )
                    continue; //if a fireteam is ready to fight, don't send more ships to that team

                if ( team.DeepInfo.CurrentPlanet == null )
                {
                    //this is a brand new fireteam, so it's totally safe.
                    AvailableFireteams.AddIfNotAlreadyIn( team );
                    continue;
                }
                Int16 hops = 0;
                int dangerOfTeam = Fireteam.GetDangerOfPath( faction, Context, PathCacheData, entity.Planet, team.DeepInfo.CurrentPlanet, true, out hops );
                int maxHops = 5;
                if ( dangerOfTeam < 20000 ) //let units wander through pretty dangerous spots (20 strength)
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
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path A", Verbosity.DoNotShow );
                    stopProcesssing = true;
                    break;
                }
            }
            if ( stopProcesssing )
                return;

            int percentNewTeam = 40;
            if ( AvailableFireteams.GetItemCount() > 5 )
                percentNewTeam = 0;

            if ( Context.RandomToUse.Next( 0, 100 ) < percentNewTeam || AvailableFireteams.GetItemCount() == 0 )
            {
                Fireteam team = Fireteam.CreateNewWithIDFromList( BaseInfo.Teams );
                team.MyStrengthMultiplierForStrengthCalculation = FInt.FromParts(1, 100);
                team.EnemyStrengthMultiplierForStrengthCalculation = FInt.FromParts(1, 000);

                team.StrengthToBringOnline = this.MinFireteamStrength + Context.RandomToUse.Next( 0, MaxFireteamStrength - MinFireteamStrength );
                team.PercentDistanceBestTarget = 45; //split up a lot
                team.PreferredMaxDistance = 5;
                team.DeepInfo.AddUnit( entity );
                entity.FireteamId = team.FireTeamID;

                team.DeepInfo.IdentifyCurrentPlanet(); //in case this unit is the first unit
                BaseInfo.Teams.AddIfNotAlreadyIn( team );
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path B", Verbosity.DoNotShow );
                return;
            }

            {
                Fireteam team = AvailableFireteams.GetRandom( Context.RandomToUse );
                team.DeepInfo.AddUnit( entity );
                entity.FireteamId = team.FireTeamID;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path C", Verbosity.DoNotShow );
                team.DeepInfo.IdentifyCurrentPlanet(); //just in case this unit is the first unit or something
            }
        }

        public override GameEntity_Squad GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            int currentDanger = -1;
            GameEntity_Squad retreatPoint = null;
            for ( int i = 0; i < AlliedCommandStations.Count; i++ )
            {
                GameEntity_Squad outpost = AlliedCommandStations[i].GetSquad();
                if ( outpost == null )
                    continue;
                if ( outpost.Planet == CurrentPlanetForFireteam )
                    continue;
                Int16 hops = 0;
                int danger = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForFireteam, outpost.Planet, true, out hops );
                if ( danger < currentDanger || currentDanger == -1 )
                {
                    retreatPoint = outpost;
                    currentDanger = danger;
                }
            }
            return retreatPoint;
        }

        //Set immediately before PreferredTargets.Sort(...) so the comparison can be a non-capturing
        //static delegate.  [ThreadStatic] because this runs on a background non-sim thread.
        [ThreadStatic] private static Planet cb_ftCurrentPlanet;
        [ThreadStatic] private static Faction cb_ftAttachedFaction;
        [ThreadStatic] private static FInt cb_ftFalloff;

        public override void GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass( bool DefenseMode, Planet CurrentPlanetForFireteam,
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, List<FireteamTarget> PreferredTargets, List<FireteamTarget> FallbackTargets, object TeamObj )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "SpireSidekick-GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            FInt falloffForDistance = FInt.FromParts( 0, 050 );
            GetPreferredImperialSpireTargets( PreferredTargets, AttachedFaction, Context );
            GetFallbackImperialSpireTargets( FallbackTargets, AttachedFaction, Context ); //this is used so the imperial spire can also go after minor factions
            //sort targets by how hard it is to get there
            for ( int i = 0; i < PreferredTargets.Count; i++ )
            {
                FireteamTarget target = PreferredTargets[i];
                Int16 hops = 0;
                target.dangerOfTarget = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForFireteam, PreferredTargets[i].planet, true, out hops );
                PreferredTargets[i] = target;
            }
            for ( int i = PreferredTargets.Count - 1; i >= 0; i-- )
            {
                FireteamTarget target = PreferredTargets[i];
                if ( Fireteam.IsThisAWinningBattle( AttachedFaction, Context, target.planet, 4 ) || target.dangerOfTarget == -1)
                    PreferredTargets.Remove( target );
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

                return rDanger.CompareTo( lDanger ); //prefer stronger targets
            } );


            
            // for ( int i = 0; i < PreferredTargets.Count; i++ )
            // {
            //     //discard things that are enough more dangerous than other things on the list
            //     if ( i == 0 ) continue;
            //     FireteamTarget target = PreferredTargets[i];
            //     FireteamTarget weakestTarget = PreferredTargets[0];
            //     FInt danger = (FInt)target.dangerOfTarget;
            //     FInt weakestDanger = (FInt)weakestTarget.dangerOfTarget;
            //     if ( target.planet.GetControllingOrInfluencingFaction() == faction )
            //         danger /= 2;
            //     if ( danger > weakestDanger * FInt.FromParts( 2, 000 ) )
            //     {
            //         //this is way more dangerous than earlier targets on the list, so ignore it
            //         PreferredTargets.RemoveRange( i, PreferredTargets.Count - i );
            //     }

            // }
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


            bool debug = false;
            if ( debug && tracing )
            {
                tracingBuffer.Add( "Getting lurk/target Preferred Targets\n" );
                for ( int i = 0; i < PreferredTargets.Count; i++ )
                    tracingBuffer.Add( "\t" ).Add( PreferredTargets[i].GetPlanetName_Safe() ).Add( " difficulty " ).Add( PreferredTargets[i].dangerOfTarget ).Add( " \n" );
                tracingBuffer.Add( "Getting lurk/target Fallback Targets\n" );
                for ( int i = 0; i < FallbackTargets.Count; i++ )
                    tracingBuffer.Add( "\t" ).Add( FallbackTargets[i].GetPlanetName_Safe() ).Add( "\n" );
            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        public void GetPreferredImperialSpireTargets( List<FireteamTarget> ListToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            ListToFill.Clear();
            bool foundKingUnderAttack = false;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null )
                    continue;
                if ( otherFaction.GetIsFriendlyTowards( faction ) )
                {
                    foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.KingUnitsOnly ) )
                    {
                        //Check if the king here is under attack; if so, help it
                        var pFaction = entity.Planet.GetStanceDataForFaction( faction );
                        if ( pFaction[FactionStance.Hostile].TotalStrength > 0 )
                        {
                            ListToFill.Add( new FireteamTarget(entity.Planet) );
                            foundKingUnderAttack = true;
                        }
                    }
                }
                if ( foundKingUnderAttack )
                    continue;
                if ( !otherFaction.GetIsHostileTowards( faction ) )
                    continue;
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( faction );
                    int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                    int myStrength = myFactionData[FactionStance.Self].TotalStrength + myFactionData[FactionStance.Friendly].TotalStrength;
                    if ( hostileStrength > 10000 )
                        ListToFill.Add( new FireteamTarget( planet ) );
                }

                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    ListToFill.Add( new FireteamTarget( entity ) );
                }
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.AIPOnDeath ) )
                {
                    //clear out dire guard posts
                    if ( entity.TypeData.SpecialType != SpecialEntityType.DireGuardPost )
                        continue;
                    ListToFill.Add( new FireteamTarget( entity ) );
                }

            }
            for ( int i = 0; i < ListToFill.Count; i++ )
            {
                FireteamTarget target = ListToFill[i];
                int ignored = 0;
                target.dangerOfTarget = Fireteam.GetPlanetDefensiveStrength(target.planet, faction, true, ref ignored, FInt.Zero, FInt.Zero);
            }
        }
        public void GetFallbackImperialSpireTargets( List<FireteamTarget> ListToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            ListToFill.Clear();
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null )
                    continue;

                if ( !otherFaction.GetIsHostileTowards( faction ) )
                    continue;
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.GrantsMinorFactionPlanetControl ) )
                {
                    //allows the imperial spire to go after hostile minor factions
                    ListToFill.Add( new FireteamTarget( entity ) );
                }
            }

            for ( int i = 0; i < ListToFill.Count; i++ )
            {
                FireteamTarget target = ListToFill[i];
                int ignored = 0;
                target.dangerOfTarget = Fireteam.GetPlanetDefensiveStrength(target.planet, faction, true, ref ignored, FInt.Zero, FInt.Zero);
            }
        } 
        public override Planet GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass( Planet TargetPlanet, int TeamStrength, Planet CurrentPlanetForTeam, 
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            Planet bestPlanet = null;
            int dangerOfPathFromBestPlanet = -1;
            //int dangerOfPathToBestPlanet = 999999;
            int distanceFromBestPlanet = 9999;
            Int16 hopsFromBestPlanet = 9999;
            int unused = 0;
            //this logic partially cribbed from IndependentAIFleet.cs::Helper_DoTargetFindingSweep

            if ( TargetPlanet == null )
                throw new Exception( "No target planet set in get lurk planet?!" );
            if ( TargetPlanet == CurrentPlanetForTeam )
                return TargetPlanet;
            //int debugCode = 0;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "SpireSidekick-GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
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
                //the Imperial spire doesn't care about the difficulty of getting to a target
//                int totalDifficultyOfPathToLurkPlanet = Fireteam.GetDangerOfPath( faction, Context, CurrentPlanetForTeam, planet, true, out hops );
//                if ( totalDifficultyOfPathToLurkPlanet >= TeamStrength * 5 ) //as long as they only outnumber us 5:1, let's go!
//                    continue;
                int totalDifficultyOfPathToTarget = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, planet, TargetPlanet, true, out hops );
                if ( preferUnwatchedPlanets && planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    totalDifficultyOfPathToTarget *= 2; //penalized watched planets if desired

                if ( totalDifficultyOfPathToTarget < 0 )
                    totalDifficultyOfPathToTarget = 0; //pathing through allied planets is basically the same

                if ( tracing )
                    tracingBuffer.Add("\tConsidering " + planet.Name + " danger of path " + totalDifficultyOfPathToTarget + " distance " + Distance + " intel " + planet.IntelLevel ).Add("\n");

                if ( dangerOfPathFromBestPlanet == -1 || dangerOfPathFromBestPlanet > totalDifficultyOfPathToTarget )
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
                    bestPlanet = planet;
                    hopsFromBestPlanet = hops;
                }
            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            return bestPlanet;
        }

        

        //Static functions intended for use by Chris' code below
        public GameEntity_Squad TransformRelicToSpireStructure_OrNull( GameEntity_Squad relic, ArcenHostOnlySimContext Context, bool TransformIntoGalacticCapitol )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return null; //don't even try this on clients
            if ( relic == null )
                return null;

            SpireSidekickPerUnitBaseInfo relicData = relic.TryGetExternalBaseInfoAs<SpireSidekickPerUnitBaseInfo>();
            Faction playerfaction = AttachedFaction;
            if ( playerfaction == null )
                throw new Exception( "Relic was created w/o knowing who summoned it" );
            GameEntityTypeData cityEntityData = GameEntityTypeDataTable.Instance.GetRowByName( "SpireSidekickCityHub" );
            if ( cityEntityData == null )
                throw new Exception( "No spire city hub defined in XML" );
            if ( TransformIntoGalacticCapitol )
            {
                cityEntityData = GameEntityTypeDataTable.Instance.GetRowByName(  "SpireSidekickGalacticCapitol"  );
                if ( cityEntityData == null )
                    throw new Exception( "No spire galacticCapitol defined in XML" );
            }
            if ( relic == null )
                throw new Exception( "No relic?" );
            PlanetFaction pFaction = relic.Planet.GetPlanetFactionForFaction( playerfaction );

            //I'd like to say "If this is a safe placement point, put the unit here. Otherwise place it "very close by"  This is the best way.
            ArcenPoint spawnLocation = relic.Planet.GetSafePlacementPoint_AroundEntity( Context, cityEntityData, relic, FInt.FromParts( 0, 005 ), FInt.FromParts( 0, 010 ) );

            //put the city enter in first, so it gets to stay where it is.  But it's not the centerpiece
            GameEntity_Squad cityCenter = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, cityEntityData, 1,
                    null, 0, spawnLocation, Context, "SpireSidekick-TransformRelic" );

            Fleet cityFleet = cityCenter.FleetMembership.Fleet;

            cityFleet.NameRaw = "Spire City '" + RandomSpireCityName(Context, cityCenter) + "'";
            cityFleet.CreateExternalBaseInfo<SpireSidekickCityFleetBaseInfo>("SpireSidekickCityFleetBaseInfo");

            relic.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
            return cityCenter;
        }

        private void DropMetalGenerators(ArcenHostOnlySimContext Context)
        {
            //at game start time, the armada will get the metal generators on its home planet
            //this is not ideal, since we don't use them. So either give them to the human empire we started with, or
            //make them neutral
            if ( World_AIW2.Instance.GameSecond % 10 != 1 )
                return;
            GameCommand transferCommand = null;
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "MetalGenerator" ) )
            {
                if (transferCommand == null)
                {
                    transferCommand = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransferEntitiesToFaction], GameCommandSource.AnythingElse);
                }
                int destFactionId = 0;
                foreach ( GameEntity_Squad king in entity.Planet.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    if ( king.PlanetFaction.Faction.Type != FactionType.Player )
                        continue;
                    PlayerTypeData playerTypeData = king.PlanetFaction.Faction.PlayerTypeDataOrNull_ModeratelyExpensive;
                    if ( playerTypeData == null )
                        continue;
                    if ( !playerTypeData.UsesMetal )
                        continue;
                    if ( king.PlanetFaction.Faction == this.AttachedFaction)
                        continue;
                    destFactionId = king.PlanetFaction.Faction.FactionIndex;
                    break;
                }
                transferCommand.RelatedEntityIDs.Add(entity.PrimaryKeyID);
                transferCommand.RelatedFactionIndex = (short)destFactionId; //either neutral or a player who uses metal (this only seems to happen on homeworlds where the sidekick steals the metal generators)
            }
            if ( transferCommand != null )
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, transferCommand, false );
        }
        
        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "AISpireResearchLab", SeedingType.CapturableWeightsAndMax,
                1, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 4, 9, 2, 99, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal, null, -1 );
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "AISpireCitadel", SeedingType.CapturableWeightsAndMax,
                1, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 4, 9, 2, 99, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal, null, -1 );

            //Set our economy
            AttachedFaction.StoredScience = FInt.FromParts( 7500, 000 );
            AttachedFaction.StoredHacking = FInt.FromParts( 40, 000 )
;

            
            //Seed some Golems for the Spire
            int nearbyGolems = 1;
            int farGolems = 2;
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "SpireGolem", SeedingType.CapturableWeightsAndMax,
                nearbyGolems, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 2, 4, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "SpireGolem", SeedingType.CapturableWeightsAndMax,
                farGolems, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 5, 99, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );

            ArcenDebugging.LogSingleLine("spawning lone frigates?", Verbosity.DoNotShow );
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "SpireSidekickLoneFrigate", SeedingType.CapturableWeightsAndMax,
                2, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 3, 99, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );

             foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
             {
                 if ( planet.GetControllingFaction().Type == FactionType.AI)
                 {
                     PlanetFaction pFaction = planet.GetPlanetFactionForFaction( this.AttachedFaction );
                     pFaction.AIPLeftFromWarpGate = 5;
                     pFaction.AIPLeftFromCommandStation = 15;
                 }
             }
        }
        #region HandleSpireCitadelBuiltLogic
        public void HandleSelfBuildingCompleteLogic( GameEntity_Squad entity, Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( entity.TypeData.InternalName == "SpireFrigateNeuralNet") {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_FrigateNeuralNet", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            } else if ( entity.TypeData.InternalName == "SpireDestroyerNeuralNet" ) {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_DestroyerNeuralNet", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            } else if ( entity.TypeData.GetHasTag( "SpireCitadel" ) ) {
                if ( entity.GetIsFactionControlledByAnyPlayerAccount_Safe()
                    && entity.GetNumberInFaction( entity.TypeData ) == 1 )//okay to use here, as it will be consistent per run
                {
                    //At the moment I think the AI should taunt all the players once one gets Spire allies, since
                    //I assume that the AI's counterstrikes will hit all the players
                    //TODO: Once the Spire Citadel is defined in XML, give it a tag and also check for the tag
                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.PlayerBuildsFirstSpireCitadel );
                }
                if ( entity.GetIsFactionControlledByAnyPlayerAccount_Safe()
                    && entity.GetNumberInFaction( entity.TypeData ) > 1 )//okay to use here, as it will be consistent per run
                {
                    //At the moment I think the AI should taunt all the players once one gets Spire allies, since
                    //I assume that the AI's counterstrikes will hit all the players
                    //TODO: Once the Spire Citadel is defined in XML, give it a tag and also check for the tag
                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.PlayerBuildsSubsequentSpireCitadel );
                }

                entity.FlagForForcedFullSyncToClients_FromHost();
            }
        }
        #endregion
        public void DoInitializationIfNecessary( Faction faction, SpireSidekickFactionBaseInfo BaseInfo, ArcenHostOnlySimContext Context, bool tracing )
        {
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "SpireSidekick-DoInitializationIfNecessary-trace", 10f ) : null;

            if ( BaseInfo.exoData.StrengthRequiredForNextExo == FInt.Zero )
            {
                BaseInfo.exoData.StrengthRequiredForNextExo = (FInt)BaseInfo.Difficulty.BaseExoStrength;
                BaseInfo.exoData.CurrentExoStrength = FInt.Zero;
                BaseInfo.exoData.NumExosSoFar = 0;
                BaseInfo.exoData.FactionIndexOfExoSpawnFaction = World_AIW2.GetRandomAIFaction( Context ).FactionIndex; //send from a random ai faction each time
                BaseInfo.exoData.ExoReasonOverride = "Spire Sidekick";
                BaseInfo.exoData.PercentToStartWarning = 75;
            }
            //            ArcenDebugging.ArcenDebugLogSingleLine("pre: Time for next relic spawn " + BaseInfo.TimeForNextRelicSpawn + " debug mode " + faction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ), Verbosity.DoNotShow );
            if ( BaseInfo.TimeForNextRelicSpawn == -1 )
            {
                if ( faction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) )
                {
                    BaseInfo.TimesForNextSpireDebris.Add( BaseInfo.DebrisSpawnDelay + Context.RandomToUse.Next( BaseInfo.DebrisSpawnDelayRandomness / 10, BaseInfo.DebrisSpawnDelayRandomness ) ); //immediate debris
                    BaseInfo.TimesForNextSpireDebris.Add( BaseInfo.DebrisSpawnDelay + Context.RandomToUse.Next( BaseInfo.DebrisSpawnDelayRandomness / 10, BaseInfo.DebrisSpawnDelayRandomness ) ); //immediate debris
                }

                BaseInfo.TimeForNextRelicSpawn = BaseInfo.FirstRelicSpawnTime;
                if ( tracing )
                    tracingBuffer.Add( "Time for next relic spawn " + BaseInfo.TimeForNextRelicSpawn ).Add( "\n" );
            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        public void UpdateExoData( Faction faction, SpireSidekickFactionBaseInfo BaseInfo, ArcenHostOnlySimContext Context, bool tracing )
        {
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "SpireSidekick-UpdateExoData-trace", 10f ) : null;

            int exoStrengthModifier = BaseInfo.NumRelicsCaptured; //this is either the number of relics captured (fallen spire) or the number of cities (infused empire). Used to gauge how large the exo should be

            if ( exoStrengthModifier < BaseInfo.SpireCities.Count )
                exoStrengthModifier = BaseInfo.SpireCities.Count; //for the spire infused empire; it doesn't use relics

            if ( exoStrengthModifier > 0 )
            {
                //the Exo is allowed to charge
                FInt aipAdjustedIncome = (FactionUtilityMethods.Instance.GetCurrentAIP() / 10) * BaseInfo.Difficulty.ExoIncomePer10AIP;
                FInt multiplier = BaseInfo.Difficulty.ExoIncomeMultiplierPerCity * exoStrengthModifier;
                FInt change = (BaseInfo.Difficulty.BaseExoIncome + aipAdjustedIncome) * (multiplier);
                BaseInfo.exoData.UpdateExoStrength (change);
                if ( tracing )
                    tracingBuffer.Add( "Exo strength additions: (base income: " + BaseInfo.Difficulty.BaseExoIncome + " aip income " + aipAdjustedIncome + ") * ( city-based income multiplier " + BaseInfo.Difficulty.ExoIncomeMultiplierPerCity + " * " + BaseInfo.NumRelicsCaptured + ") = " + change + ". exo strength " + BaseInfo.exoData.CurrentExoStrength + " target strength " + BaseInfo.exoData.StrengthRequiredForNextExo ).Add( "\n" );

            }
            BaseInfo.exoData.SyncExoToCPAIfAllowed( faction, Context );
            if ( BaseInfo.exoData.ShouldLaunchExo() )
            {
                List<SafeSquadWrapper> workingTargets = GameEntity_Squad.GetTemporarySquadList( "SpireSidekick-UpdateExoData-workingTargets", 10f );
                if ( workingTargets == null ) //blocked for teardown/shutdown; bail
                    return;
                FactionUtilityMethods.Instance.findAllHumanKings( workingTargets );
                int ExoStrengthToSend = BaseInfo.exoData.CurrentExoStrength.IntValue;
                if ( faction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) ) //debug mode is easy so you can play through it quickly for testing
                    ExoStrengthToSend /= 15;
                BaseInfo.exoData.ResetSync();
                //Sometimes we add some extra targets to make the exo a bit more exciting for the player
                foreach ( GameEntity_Squad city in BaseInfo.SpireCities.DisplaySquads() )
                {
                    //For each city, there is a chance of targeting it
                    int percentAdditionalTarget = 20;
                    if ( Context.RandomToUse.Next( 0, 100 ) < percentAdditionalTarget )
                        workingTargets.Add( city );
                }
                ExoOptions options = ExoOptions.CreateWithDefaults( workingTargets, ExoStrengthToSend, World_AIW2.Instance.GetFactionByIndex( BaseInfo.exoData.FactionIndexOfExoSpawnFaction ), faction );
                if ( BaseInfo.SpireCities.Count > 3 )
                    options.newExoLeaderTag="ExtragalacticWar";

                GameEntity_Squad.ReleaseTemporarySquadList( workingTargets );

                ExoGalacticAttackManager.SendExoGalacticAttack( options, Context );
                BaseInfo.exoData.CurrentExoStrength = FInt.Zero;
                BaseInfo.exoData.NumExosSoFar++;
                BaseInfo.NumExosSent++;
                FInt multiplicativeBasedIncrease = BaseInfo.Difficulty.BaseExoStrength * BaseInfo.Difficulty.MultiplicativeExoStrengthIncreasePerExo * BaseInfo.exoData.NumExosSoFar;
                FInt additiveBaseIncrease = (FInt)BaseInfo.Difficulty.AdditiveExoStrengthIncreasePerExo * BaseInfo.exoData.NumExosSoFar;
                FInt cityBasedMultiplier = BaseInfo.Difficulty.ExoStrengthIncreaseMultiplierPerCity * exoStrengthModifier;
                BaseInfo.exoData.StrengthRequiredForNextExo = (FInt)(BaseInfo.Difficulty.BaseExoStrength +  multiplicativeBasedIncrease + additiveBaseIncrease ) * cityBasedMultiplier;
                
                if ( tracing )
                    tracingBuffer.Add( "Sending exo now.  Next Exo strength will be (" + BaseInfo.Difficulty.BaseExoStrength + " + " + multiplicativeBasedIncrease + " + " + additiveBaseIncrease + " ) * " + cityBasedMultiplier + " = " + BaseInfo.exoData.StrengthRequiredForNextExo + ". NumExosSoFar " + BaseInfo.exoData.NumExosSoFar + " cities: " + BaseInfo.NumRelicsCaptured).Add( "\n" );
                BaseInfo.exoData.FactionIndexOfExoSpawnFaction = World_AIW2.GetRandomAIFaction( Context ).FactionIndex; //send from a random ai faction each time

            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        public void HandleSpireDebris( List<Faction> AIFactionsForDebris, List<Faction> OtherFactionsForDebris, Faction faction, SpireSidekickFactionBaseInfo BaseInfo, ArcenHostOnlySimContext Context, bool tracing )
        {
            int debugCode = 0;
            try
            {
                ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "SpireSidekick-HandleSpireDebris-trace", 10f ) : null;
                for ( int j = BaseInfo.TimesForNextSpireDebris.Count - 1; j >= 0; j-- )
                {
                    debugCode = 3100;
                    BaseInfo.TimesForNextSpireDebris[j]--;
                    if ( BaseInfo.TimesForNextSpireDebris[j] == 0 )
                    {
                        debugCode = 3200;
                        BaseInfo.TimesForNextSpireDebris.RemoveAt( j );
                        GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "SpireSidekickDebris" );
                        if ( entityData == null )
                            ArcenDebugging.ArcenDebugLogSingleLine( "No Spire Debris defined in XML", Verbosity.DoNotShow );
                        Planet spawnPlanet = GetDebrisSpawnPlanet( faction, BaseInfo, Context );
                        if ( spawnPlanet == null )
                        {
                            continue;
                        }
                        PlanetFaction pFaction = spawnPlanet.GetPlanetFactionForFaction( faction );

                        ArcenPoint spawnLocation = spawnPlanet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 300 ) );

                        GameEntity_Squad debris = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                                                        pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "SpireSidekick-NewDebris" );
                        if ( debris == null )
                            continue;
                        SpireSidekickPerUnitBaseInfo debrisData = debris.CreateExternalBaseInfo<SpireSidekickPerUnitBaseInfo>( "SpireSidekickPerUnitBaseInfo" );
                        int debrisTime = BaseInfo.Difficulty.DebrisDuration + Context.RandomToUse.Next( 0, 60 );
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_SpireDebris", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                        if ( BaseInfo.SpireCities.Count == 1 )
                            debrisTime *= 2; //the first set of debris gives you extra time to get
                        debrisData.TimeUntilDebrisVanishes = World_AIW2.Instance.GameSecond + debrisTime;
                        Faction destinationFactionForDebris = GetAvailableFactionForDebris( AIFactionsForDebris, OtherFactionsForDebris, faction, BaseInfo, Context );
                        if ( destinationFactionForDebris == null )
                            debrisData.FactionIndexForDebris = -1;
                        else
                            debrisData.FactionIndexForDebris = destinationFactionForDebris.FactionIndex;
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( debris );

                            World_AIW2.Instance.QueueChatMessageOrCommand( "Spire Debris is spawning on " + debris.GetPlanetName_Safe() + ". You have " + debrisTime +
                                " seconds to retrieve it before someone else does. Some Spire Debris is generated shortly after a new Spire City is built.", ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                        }
                        BaseInfo.SpireDebris.AddToDisplayList( debris );
                    }
                }
                debugCode = 4000;
                foreach ( GameEntity_Squad debris in BaseInfo.SpireDebris.DisplaySquads() )
                {
                    debugCode = 4100;
                    SpireSidekickPerUnitBaseInfo debrisData = debris.TryGetExternalBaseInfoAs<SpireSidekickPerUnitBaseInfo>();
                    //if we are actively hacking this debris, ignore
                    if ( tracing )
                        tracingBuffer.Add("Processing " + debris.ToStringWithPlanet() + " TimeUntilDebrisVanishes " + (debrisData.TimeUntilDebrisVanishes - World_AIW2.Instance.GameSecond) ).Add("\n");
                    if ( debrisData.TimeUntilDebrisVanishes == -1 )
                        continue;

                    if ( World_AIW2.Instance.GameSecond >= debrisData.TimeUntilDebrisVanishes )
                    {
                        if ( tracing )
                            tracingBuffer.Add("\tTime to resolve this debris\n");

                        debugCode = 4200;
                        //Handle AI or minor factions getting spire debris
                        Faction factionToUse = World_AIW2.Instance.GetFactionByIndex( debrisData.FactionIndexForDebris );
                        if ( factionToUse == null )
                            factionToUse = GetAvailableFactionForDebris( AIFactionsForDebris, OtherFactionsForDebris, faction, BaseInfo, Context );
                        debugCode = 4210;
                        if ( factionToUse == null )
                        {
                            debugCode = 4220;
                            ArcenDebugging.ArcenDebugLogSingleLine( "Somehow there are no actual factions available for debris. This is a BUG (or you have won the game)\n", Verbosity.DoNotShow );
                            debris.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                            continue;
                        }
                        if ( factionToUse.FactionIsDefeated ) //in the time between this debris having a faction selected and now, the faction has died. Do nothing
                        {
                            debris.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                            continue;
                        }
                        if ( debris.AmIBeingHacked() ) //this won't be used anymore
                        {
                            //if a player was hacking this debris when it ran out of time, the other faction does not get it
                            debris.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                            continue;
                        }

                        debugCode = 4230;
                        //bool multipleFactionsFlagged = false;

                        Planet debrisPlanet = debris.Planet;
                        ArcenCharacterBuffer workingBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "SpireSidekick-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-workingBuffer", 10f );
                        if ( factionToUse.Type != FactionType.AI )
                        {
                            debugCode = 4300;
                            factionToUse.HasObtainedSpireDebris = true;
                            if ( faction.RandomImpact != TypeDifficulty.Unset &&
                                 !faction.HasBeenSeenByPlayer &&
                                 !GameSettings.Current.GetBoolBySetting( "ShowRandomAIType" ) )
                                workingBuffer.Add( "A " ).Add( "Random Faction", factionToUse.FactionCenterColor.ColorHexBrighter ).Add( " has retrieved the Spire Debris on " + debris.GetPlanetName_Safe() + ". They will use this to build new ship types with Spire technology" );
                            else
                                workingBuffer.Add( factionToUse.GetDisplayName(), factionToUse.FactionCenterColor.ColorHexBrighter ).Add( " has retrieved the Spire Debris on " + debris.GetPlanetName_Safe() + ". They will use this to build new ship types with Spire technology" );
                        }
                        else
                        {
                            debugCode = 4400;
                            factionToUse.HasObtainedSpireDebris = true;

                            workingBuffer.Add( "The " ).Add( factionToUse.GetDisplayName(), factionToUse.FactionCenterColor.ColorHexBrighter ).Add( " has retrieved the Spire Debris on " + debris.GetPlanetName_Safe() + ". They will repair it to build a single powerful ship to use against their enemies." );
                            //TODO: Do we want an AI-specific spire unit type? If so, we should use that here
                            debugCode = 4410;
                            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "AIShipFromDebris" );
                            GameEntity_Squad king = FactionUtilityMethods.Instance.findKing( factionToUse );
                            PlanetFaction pFaction = king.Planet.GetPlanetFactionForFaction( factionToUse );
                            debugCode = 4420;
                            GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData,
                                                                                  factionToUse.CurrentGeneralMarkLevel,
                                                                                  pFaction.FleetUsedAtPlanet, 0,
                                                                                  king.WorldLocation, Context, "SpireSidekick-AIShipFromDebris" );
                            debugCode = 4430;
                            if ( entity != null )
                                entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread

                        }
                        debugCode = 4500;
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = debrisPlanet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( workingBuffer.ToStringAndReturnToPool(), ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                        }
                        else
                            workingBuffer.ReturnToPool();
                        debris.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                    }
                }
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in HandleSpireDebris at debugCode " + debugCode + ". Exception: " + e, Verbosity.DoNotShow );
            }
        }
        public Planet GetDebrisSpawnPlanet( Faction faction, SpireSidekickFactionBaseInfo BaseInfo, ArcenHostOnlySimContext Context )
        {
            //reuses the relic spawn point interface, for convenience
            Planet planet = null;
            int retries = 100;
            do
            {
                Int16 planetIdx = GetNextRelicSpawnPoint( faction, Context, true );
                planet = World_AIW2.Instance.GetPlanetByIndex( planetIdx );
                //don't allow duplication
                foreach ( GameEntity_Squad debris in BaseInfo.SpireDebris.DisplaySquads() )
                {
                    if ( debris.Planet == planet )
                    {
                        planet = null;
                        break;
                    }
                }
            } while ( retries-- > 0 && planet == null );
            return planet;
        }
        #region RecalculateSpireFleetsAndFlagships_MainThreadSimOnly
        public void RecalculateSpireFleetsAndFlagships_MainThreadSimOnly( Faction faction, SpireSidekickFactionBaseInfo BaseInfo, ArcenHostOnlySimContext Context )
        {
            //goal: three spire fleets, period, at most.  Regardless of how many players.
            //first fleet comes when you have a single city.
            //second on first city to mark 3, third on city to mark 4

            int intendedNumberOfFleets;
            switch ( BaseInfo.SpireCities.Count )
            {
                case 0:
                    return; //if we have no spire cities, then do nothing
                case 1:
                case 2:
                    intendedNumberOfFleets = 1;
                    break;
                case 3:
                case 4:
                    intendedNumberOfFleets = 2;
                    break;
                default:
                    intendedNumberOfFleets = 3;
                    break;
            }

            int countOfFleetsActuallyHere = 0;
            foreach ( Fleet cityFedFleet in World_AIW2.Instance.Fleets_CityFedMobile( null, FleetStatus.AnyStatus ) )
            {
                if ( cityFedFleet.FleetQualifier == "FallSpi" )
                {
                    countOfFleetsActuallyHere++;
                }
            }
            
            if ( intendedNumberOfFleets > countOfFleetsActuallyHere )
            {
                //if we have too-few fleets, let's create some then!  Only do one at a time, just in case.

                //We spawn this new flagship at the new city
                if ( BaseInfo.SortedSpireCities.Count <= 0 )
                    return; //no cities
                GameEntity_Squad cityToUse = BaseInfo.SortedSpireCities.GetDisplayList()[BaseInfo.SortedSpireCities.Count - 1].GetSquad();
                if ( cityToUse == null )
                    return;

                //here's our new flagship for our new fleet
                GameEntityTypeData fleetLeaderType = GameEntityTypeDataTable.Instance.GetRowByName(
                    BaseInfo.ExpertMode ? "SpireCruiserFlagship_Expert" : "SpireCruiserFlagship"
                );
                GameEntity_Squad newFleetLeader = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( cityToUse.PlanetFaction, fleetLeaderType, 1,
                    null, 0, cityToUse.WorldLocation, Context, "SpireSidekick-NewFleet" );

                newFleetLeader.FleetMembership.Fleet.NameRaw = "Spire Fleet '" + RandomSpireCityName(Context, cityToUse) + "'";
                newFleetLeader.FleetMembership.Fleet.FleetQualifier = "FallSpi";

                ArcenDebugging.ArcenDebugLogSingleLine( "Added new fleet: countOfFleetsActuallyHere: " + countOfFleetsActuallyHere + " intendedNumberOfFleets: " + intendedNumberOfFleets +
                    " " + newFleetLeader.FleetMembership.Fleet.Category, Verbosity.DoNotShow );
                newFleetLeader.OnClaim(); //run the "on claim" logic since we have just obtained a new flagship. This will trigger achievements
                //any cities that are not bolstering at all will now bolster this
                foreach ( GameEntity_Squad city in BaseInfo.SpireCities.DisplaySquads() )
                {
                    if ( city.FleetMembership.Fleet.CityBolstersFleetID <= 0 )
                        city.FleetMembership.Fleet.CityBolstersFleetID = newFleetLeader.FleetMembership.Fleet.FleetID;
                }
                return; //catch up next time around, if we added one
            }
        }
        #endregion
        #region spireCityNames
        //https://ingesanagram.appspot.com/
        //All anagrams or partial anagrams of crystal names
        private static readonly string[] spireCityNames = new string[] {
            "ca Erl", "crt ezra", "az lucre", "nib sod", "bi Ian", "cir ie", "ctr en", "iq or", "iq rue", "qui re", "sire yeg", "eer gi", "et ey",
            "gi ret", "sri ye", "eg eyre", "ems ha", "et ha", "ma set", "st ta" };
        #endregion

        public string RandomSpireCityName( ArcenHostOnlySimContext Context, GameEntity_Squad city )
        {
            if ( World_AIW2.Instance.Setup.GetBoolBySetting("BoringSpireNames") && city != null )
            {
                return city.Planet.Name;
            }
            int num = Context.RandomToUse.Next( 0, 26 ); // Zero to 25
            char firsLet = (char)('a' + num);
            num = Context.RandomToUse.Next( 0, 26 ); // Zero to 25
            char lastLet = (char)('a' + num);
            return firsLet + spireCityNames[Context.RandomToUse.Next( 0, spireCityNames.Length )] + lastLet;
        }

        #region RecalculateSpireCityMobileFleetContents_MainThreadSimOnly
        public BolsteringManager bolsteringManger = new BolsteringManager();
        public void RecalculateSpireCityMobileFleetContents_MainThreadSimOnly( Faction faction, SpireSidekickFactionBaseInfo BaseInfo, ArcenHostOnlySimContext Context )
        {
            int debugStage = 0;
            try
            {
                debugStage = 100;
                List<GameEntityTypeData> spireCityBuildings = GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( "SpireFeedsFleet" );

                int maxMarkLevelOfAnyCity = 0;

                debugStage = 200;

                #region First, Find The Max Mark Level Of Any City
                foreach ( GameEntity_Squad city in BaseInfo.SpireCities.DisplaySquads() )
                {
                    debugStage = 300;
                    Fleet fleetForCity = city.GetFleetOrNull_Safe();
                    if ( fleetForCity == null )
                        continue;
                    debugStage = 400;
                    if ( fleetForCity.MaxMarkLevelOfAnyInFleet > maxMarkLevelOfAnyCity )
                        maxMarkLevelOfAnyCity = fleetForCity.MaxMarkLevelOfAnyInFleet;
                }
                #endregion

                debugStage = 1000;

                GameEntityTypeData desiredFlagshipData = null;
                int minMarkLevelOfAllFlagships = maxMarkLevelOfAnyCity;

                debugStage = 1100;

                #region Find The desiredFlagshipData
                if ( !BaseInfo.ExpertMode )
                {
                    debugStage = 1200;
                    if ( maxMarkLevelOfAnyCity >= 6 ) //any city mark 6 or higher
                        desiredFlagshipData = GameEntityTypeDataTable.Instance.GetRowByName( "SpireDreadnoughtFlagship" );
                    else if ( maxMarkLevelOfAnyCity >= 3 ) //any city mark 3 or higher
                        desiredFlagshipData = GameEntityTypeDataTable.Instance.GetRowByName( "SpireBattleshipFlagship" );
                    else
                        desiredFlagshipData = GameEntityTypeDataTable.Instance.GetRowByName( "SpireCruiserFlagship" );
                }
                else
                {
                    debugStage = 1300;
                    if ( maxMarkLevelOfAnyCity >= 6 ) //any city mark 6 or higher
                        desiredFlagshipData = GameEntityTypeDataTable.Instance.GetRowByName( "SpireDreadnoughtFlagship_Expert" );
                    else if ( maxMarkLevelOfAnyCity >= 3 ) //any city mark 3 or higher
                        desiredFlagshipData = GameEntityTypeDataTable.Instance.GetRowByName( "SpireBattleshipFlagship_Expert" );
                    else
                        desiredFlagshipData = GameEntityTypeDataTable.Instance.GetRowByName( "SpireCruiserFlagship_Expert" );
                }
                #endregion

                debugStage = 2000;

                //Commented out per Badger's notes: https://bugtracker.arcengames.com/view.php?id=26511
                //#region Missing Centerpieces
                ////look for any missing centerpieces, and fix them
                //World_AIW2.Instance.DoForCityFedMobileFleets( null, FleetStatus.AnyStatus, delegate ( Fleet cityFedFleet )
                //{
                //    debugStage = 2100;
                //    if ( cityFedFleet.FleetQualifier == "FallSpi" )
                //    {
                //        debugStage = 2200;
                //        GameEntity_Squad mobileFlagship = cityFedFleet.Centerpiece.GetSquad();

                //        debugStage = 2300;
                //        if ( mobileFlagship == null ) //missing flagship!
                //        {
                //            debugStage = 2400;
                //            //here's our new flagship
                //            mobileFlagship = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( mobileFlagship.PlanetFaction, desiredFlagshipData, (byte)minMarkLevelOfAllFlagships,
                //                    cityFedFleet, 0, Engine_AIW2.Instance.CombatCenter, Context, "SpireSidekick-MissingFlagship" );
                //        }
                //    }
                //    return DelReturn.Continue;
                //} );
                //#endregion

                debugStage = 3000;

                this.bolsteringManger.HandleBolstering( "SpireFeedsFleet", BaseInfo.SpireCities.GetDisplayList(), BaseInfo.SpirePlayerFleets.GetDisplayList(), Context );

                debugStage = 3100;

                //next loop over all the cities
                foreach ( GameEntity_Squad city in BaseInfo.SpireCities.DisplaySquads() )
                {
                    debugStage = 3200;
                    Fleet fleetForCity = city.GetFleetOrNull_Safe();
                    if ( fleetForCity == null )
                        continue;

                    debugStage = 3300;

                    //what fleet is this city bolstering?  Might be null, that's okay
                    Fleet fleetForMobile = fleetForCity.GetFleetBolsteredByThisCity();

                    debugStage = 3400;
                    //if the city is crippled, buildings it contains still provide the mobile fleet stuff anyway
                    //sort of.  They won't reduce cap, but won't raise it either
                    bool isCityItselfDisabled = city.GetIsCrippled() || city.GetIsNonFunctional();

                    if ( isCityItselfDisabled )
                    {
                        continue;
                    }

                    debugStage = 3500;

                    int cruisersToHave = 0;
                    int battleshipsToHave = 0;

                    if ( fleetForCity.MaxMarkLevelOfAnyInFleet >= 6 ) //city mark 6 or higher
                    {
                        battleshipsToHave = (fleetForCity.MaxMarkLevelOfAnyInFleet - 5);
                        cruisersToHave = (fleetForCity.MaxMarkLevelOfAnyInFleet - 2);
                    }
                    else if ( fleetForCity.MaxMarkLevelOfAnyInFleet >= 3 ) //city mark 3 or higher
                        cruisersToHave = (fleetForCity.MaxMarkLevelOfAnyInFleet - 2);

                    debugStage = 3600;

                    string crusierTypeName = "SpireCruiser";
                    string battleshipTypeName = "SpireBattleship";
                    if ( BaseInfo.ExpertMode )
                    {
                        crusierTypeName = "SpireCruiser_Expert";
                        battleshipTypeName = "SpireBattleship_Expert";
                    }
                    debugStage = 3700;

                    if ( cruisersToHave > 0 ) //increase the cap only if the city is not disabled
                    {
                        debugStage = 3800;
                        FleetMembership cruiserMem = fleetForMobile.GetOrAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates( GameEntityTypeDataTable.Instance.GetRowByName( crusierTypeName ), fleetForCity.FleetID );
                        debugStage = 3900;
                        if ( cruiserMem.ExplicitBaseSquadCap < cruisersToHave )
                            cruiserMem.ExplicitBaseSquadCap = cruisersToHave;
                    }

                    if ( battleshipsToHave > 0 ) //increase the cap only if the city is not disabled
                    {
                        debugStage = 4100;
                        FleetMembership battleshipMem = fleetForMobile.GetOrAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates( GameEntityTypeDataTable.Instance.GetRowByName( battleshipTypeName ), fleetForCity.FleetID );
                        debugStage = 4200; 
                        if ( battleshipMem.ExplicitBaseSquadCap < battleshipsToHave )
                            battleshipMem.ExplicitBaseSquadCap = battleshipsToHave;
                    }
                }

                debugStage = 5100;

                //now loop over all the mobile fleets for the spire
                foreach ( GameEntity_Squad mobileFlagship in BaseInfo.SpirePlayerFleets.DisplaySquads() )
                {
                    debugStage = 5200;
                    Fleet mobileFleet = mobileFlagship.GetFleetOrNull_Safe();
                    if ( mobileFleet == null )
                        continue;

                    debugStage = 5300;

                    int markLevelForFlagshipsInThisFleet = minMarkLevelOfAllFlagships;

                    if ( mobileFleet.AddedMarkLevelsForFleet_FromScience < markLevelForFlagshipsInThisFleet - 1 )
                        mobileFleet.AddedMarkLevelsForFleet_FromScience = (byte)(markLevelForFlagshipsInThisFleet - 1);

                    debugStage = 5400;

                    #region Find Flagship Types In This Fleet
                    //Backwards iterate because we remove memberships (DF's RemoveAndContinue semantics).
                    for ( int memIdx = mobileFleet.MemberGroupCount - 1; memIdx >= 0; memIdx-- )
                    {
                        FleetMembership mem = mobileFleet.GetMemberGroupAt( memIdx );
                        if ( mem == null )
                            continue;
                        debugStage = 5500;
                        //if this is a flagship type, make sure we don't have extras
                        if ( mem.TypeData.SpecialType == SpecialEntityType.MobileCustomCityFedFleetFlagship )
                        {
                            if ( mem != mobileFlagship.FleetMembership )
                            {
                                mem.DespawnAllContentsFromNoLongerBolstering( Context );
                                mobileFleet.RemoveMemGroupExplicit( mem ); //if it's not the correct type, then kill it
                            }
                        }
                    }
                    #endregion

                    debugStage = 5600;

                    if ( mobileFlagship != null )
                    {
                        debugStage = 5700;

                        //wrong type!
                        if ( mobileFlagship.TypeData != desiredFlagshipData )
                        {
                            debugStage = 5800;
                            GameEntity_Squad newFlagship = mobileFlagship.TransformInto( Context, desiredFlagshipData, 1, mobileFlagship.TypeData.KeepDamageAndDebuffsOnTransformation );
                            newFlagship.OnClaim(); //run the "on claim" logic since we have just obtained a new flagship. This will trigger achievements
                        }
                        else //right type!
                        {
                            debugStage = 5900;
                            mobileFlagship.FleetMembership.ExplicitBaseSquadCap = 1;
                            mobileFlagship.FleetMembership.SetEffectiveSquadCap( 1 );
                        }
                    }

                }

                debugStage = 7200;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "RecalculateSpireCityMobileFleetContents_MainThreadSimOnly at debugStage " + debugStage + ", Exception: " + e, Verbosity.ShowAsError );
            }
        }
        #endregion
        private readonly List<SafeSquadWrapper> imperialSpirePossibleTargets = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 30, "SpireSidekickFactionDeepInfo-imperialSpirePossibleTargets" );
                                                                                                                                                                                            
        public void HandleImperialSpire( Faction faction, List<int> chokeStrengths, SpireSidekickFactionBaseInfo BaseInfo, ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData )
        {
            GameEntity_Squad transciever = BaseInfo.Transceiver.Display.GetSquad();
            if ( transciever != null ) {
                if (transciever.SelfBuildingMetalRemaining > 0 ) {
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_TranscieverStartedBuilding", string.Empty, faction, null, transciever.PlanetFaction.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                } else if ( !transciever.HasBeenRemovedFromSim || transciever.SecondsSpentAsRemains == 0) {
                    transciever.OnClaim(); //trigger the achievements for the transciever
                    if ( !BaseInfo.ImperialFleetActive && BaseInfo.TimeUntilImperialFleetArrives < 0 )
                    {
                        BaseInfo.TimeUntilImperialFleetArrives = BaseInfo.Difficulty.ImperialSpireWaitTime;
                        BaseInfo.TimeImperialFleetSummoned = World_AIW2.Instance.GameSecond;
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_ImperialFleetInbound", string.Empty, faction, null, transciever.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    }
                }
            }
            if ( BaseInfo.TimeUntilImperialFleetArrives == -1 && !BaseInfo.ImperialFleetActive )
                return;
            // Until the Imperial spire is active, keep counting down and give the AI a response.
            if ( !BaseInfo.ImperialFleetActive )
            {
                //spawn ai exo in response to imperial spire threat
                if ( (BaseInfo.TimeImperialFleetSummoned - World_AIW2.Instance.GameSecond) % BaseInfo.Difficulty.ImperialSpireAttackInterval == 0 )
                {
                    //every "interval" seconds, spawn an exo the size of the last main exo that was sent (first pass; may need its own balance later)
                    List<SafeSquadWrapper> workingTargets = GameEntity_Squad.GetTemporarySquadList( "SpireSidekick-HandleImperialSpire1-workingTargets", 10f );
                    if ( workingTargets == null ) //blocked for teardown/shutdown; bail
                        return;
                    FactionUtilityMethods.Instance.findAllHumanKings( workingTargets );
                    int strengthToSend = BaseInfo.exoData.StrengthRequiredForNextExo.IntValue;

                    if ( BaseInfo.Transceiver.Display.GetSquad() != null )
                        workingTargets.Add( BaseInfo.Transceiver.Display ); //also send some ships against the Transceiver, since that might not be on a player homeworld

                    int enemyStrengthOnStrongestChokePoint;
                    int enemyStrengthOnWeakestChokePoint;
                    GetStrongestPlanetEnRouteToHomeworld(World_AIW2.Instance.GetFactionByIndex( BaseInfo.exoData.FactionIndexOfExoSpawnFaction ), chokeStrengths, Context, PathCacheData
                        , out enemyStrengthOnStrongestChokePoint, out enemyStrengthOnWeakestChokePoint); 

                    if ( enemyStrengthOnWeakestChokePoint > ( strengthToSend * FInt.FromParts(2, 500) ).IntValue )
                    {
                        strengthToSend += (strengthToSend * FInt.FromParts(0, 050)).IntValue;
                    }
                    if ( faction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) ) //debug mode is easy so you can play through it quickly for testing
                        strengthToSend /= 15;
                    ExoOptions options = ExoOptions.CreateWithDefaults( workingTargets, strengthToSend, World_AIW2.Instance.GetFactionByIndex( BaseInfo.exoData.FactionIndexOfExoSpawnFaction ), faction );
                    if ( Context.RandomToUse.Next(0, 100 ) < 25 )
                    {
                        //sometimes send just heavy hitters
                        options.UnitBlocksToUse.Clear();
                        options.UnitBlocksToUse.Add(ExoUnitType.Guardians);
                        options.UnitBlocksToUse.Add(ExoUnitType.ExoLeaders);
                    }
                    options.newExoLeaderTag="ExtragalacticWar";
                    options.exoText = "The AI has detected the Imperial Spire!";

                    ExoGalacticAttackManager.SendExoGalacticAttack( options, Context );

                    GameEntity_Squad.ReleaseTemporarySquadList( workingTargets );
                }
                if ( (BaseInfo.TimeImperialFleetSummoned - World_AIW2.Instance.GameSecond) % BaseInfo.Difficulty.ImperialSpireSecondaryAttackInterval == 0 )
                {
                    //AI gets some bonus strikes against tasty player targets (maybe we can knock out some GCAs or economic command stations to force a brownout?)
                    int strengthToSend = (BaseInfo.exoData.StrengthRequiredForNextExo.IntValue * BaseInfo.Difficulty.ImperialSpireSecondaryMultiplier).IntValue;
                    if ( faction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) ) //debug mode is easy so you can play through it quickly for testing
                        strengthToSend /= 15;

                    bool omitHomeworlds = true;
                    ExoGalacticAttackManager.GetTastyPlayerTargets( imperialSpirePossibleTargets, omitHomeworlds );
                    if ( Context.RandomToUse.Next(0, 100 ) < 50 ) //sometimes go in really random
                        ArcenArrays.Randomize( imperialSpirePossibleTargets, Context.RandomToUse, 5);

                    int targetsToAttack = 3;
                    if ( strengthToSend > 50000 )
                        targetsToAttack = 5;
                    if ( targetsToAttack < imperialSpirePossibleTargets.Count )
                        imperialSpirePossibleTargets.RemoveRange(targetsToAttack, imperialSpirePossibleTargets.Count - targetsToAttack );

                    ExoOptions options = ExoOptions.CreateWithDefaults( imperialSpirePossibleTargets, strengthToSend, World_AIW2.Instance.GetFactionByIndex( BaseInfo.exoData.FactionIndexOfExoSpawnFaction ), faction );
                    options.newExoLeaderTag="ExtragalacticWar";
                    options.exoText = "";

                    ExoGalacticAttackManager.SendExoGalacticAttack( options, Context );
                }
                BaseInfo.TimeUntilImperialFleetArrives--;

                if ( BaseInfo.TimeUntilImperialFleetArrives <= 0 )
                {
                    // The spire is here. Inform the player.
                    BaseInfo.ImperialFleetActive = true;

                    BaseInfo.TimeUntilImperialFleetArrives = -1;
                    if ( ArcenNetworkAuthority.GetIsHostMode() )
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_ImperialFleetArrived", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                }
                return;
            }
            //The Imperial Spire Fleet is here!
            //spawn imperial spire ships to attack
            if ( World_AIW2.Instance.GameSecond % 5 == 0 )
            {
                List<SafeSquadWrapper> workingTargets = GameEntity_Squad.GetTemporarySquadList( "SpireSidekick-HandleImperialSpire2-workingTargets", 10f );
                if ( workingTargets == null ) //blocked for teardown/shutdown; bail
                    return;

                FactionUtilityMethods.Instance.findAllHumanKings( workingTargets );
                if ( BaseInfo.Transceiver.Display.GetSquad() != null )
                    workingTargets.Add(BaseInfo.Transceiver.Display);
                for ( int i = 0; i < workingTargets.Count; i++ )
                {
                    GameEntity_Squad king = workingTargets[i].GetSquad();
                    if ( king == null )
                        continue;
                    PlanetFaction pFaction = king.Planet.GetPlanetFactionForFaction( faction );
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ImperialSpireDreadnought" );
                    if ( entityData == null )
                        ArcenDebugging.ArcenDebugLogSingleLine( "No spire dreadnaught defined?", Verbosity.DoNotShow );
                    else
                    {
                        GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData,
                                                                              king.CurrentMarkLevel,
                                                                              pFaction.Faction.LooseFleet, 0,
                                                                              king.WorldLocation, Context, "SpireSidekick-ImperialSpire" );
                        if ( entity != null )
                            entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
                    }
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ImperialSpireBattleship" );
                    if ( entityData == null )
                        ArcenDebugging.ArcenDebugLogSingleLine( "No spire battleship defined?", Verbosity.DoNotShow );
                    else
                    {
                        GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData,
                                                                              king.CurrentMarkLevel,
                                                                              pFaction.Faction.LooseFleet, 0,
                                                                              king.WorldLocation, Context, "SpireSidekick-ImperialSpire" );
                        if ( entity != null )
                            entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
                    }
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ImperialSpireCruiser" );
                    if ( entityData == null )
                        ArcenDebugging.ArcenDebugLogSingleLine( "No spire cruiser defined?", Verbosity.DoNotShow );
                    else
                    {
                        GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData,
                                                                              king.CurrentMarkLevel,
                                                                              pFaction.Faction.LooseFleet, 0,
                                                                              king.WorldLocation, Context, "SpireSidekick-ImperialSpire" );
                        if ( entity != null )
                            entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
                    }
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ImperialSpireDestroyer" );
                    if ( entityData == null )
                        ArcenDebugging.ArcenDebugLogSingleLine( "No spire destroyer defined?", Verbosity.DoNotShow );
                    else
                    {
                        GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData,
                                                                              king.CurrentMarkLevel,
                                                                              pFaction.Faction.LooseFleet, 0,
                                                                              king.WorldLocation, Context, "SpireSidekick-ImperialSpire" );
                        if ( entity != null )
                            entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
                    }

                }

                GameEntity_Squad.ReleaseTemporarySquadList( workingTargets );
            }
        }
        private void GetStrongestPlanetEnRouteToHomeworld(Faction faction, List<int> chokeStrengths, ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData, out int strongestChoke, out int weakestChoke )
        {
            //find this faction's king, then all the player kings
            List<SafeSquadWrapper> workingTargets = GameEntity_Squad.GetTemporarySquadList( "SpireSidekick-GetStrongestPlanetEnRouteToHomeworld-workingTargets", 10f );
            if ( workingTargets == null ) //blocked for teardown/shutdown; bail
            {
                strongestChoke = weakestChoke = 0;
                return;
            }

            FactionUtilityMethods.Instance.findAllHumanKings( workingTargets );
            GameEntity_Squad king = FactionUtilityMethods.Instance.findKing(faction);
            chokeStrengths.Clear();

            for ( int i = 0; i < workingTargets.Count; i++ )
            {
                int strongestChokeForThisFaction = -1;
                PathBetweenPlanetsForFaction pathCacheForDangerCalculation = PathingHelper.FindPathFreshOrFromCache( faction, "SpireSidekickGetStrongestPlanetEnRouteToHomeworld", 
                    king.Planet, workingTargets[i].Planet, PathingMode.Default, Context, PathCacheData );
                if ( pathCacheForDangerCalculation != null )
                {
                    for ( int j = 0; j < pathCacheForDangerCalculation.PathToReadOnly.Count; j++ )
                    {
                        Planet planet = pathCacheForDangerCalculation.PathToReadOnly[j];
                        int enemystrength = planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Hostile].TotalStrength;
                        int mystrength = planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Self].TotalStrength + planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Friendly].TotalStrength;
                        if ( enemystrength - mystrength > strongestChokeForThisFaction )
                            strongestChokeForThisFaction = enemystrength - mystrength;
                    }
                }
                chokeStrengths.Add(strongestChokeForThisFaction);
            }

            GameEntity_Squad.ReleaseTemporarySquadList( workingTargets );

            if ( chokeStrengths.Count == 1 )
            {
                //single enemy case is easy
                strongestChoke = weakestChoke = chokeStrengths[0];
                return;
            }
            chokeStrengths.Sort(static delegate(int L, int R)
            {
                return L.CompareTo(R);
            } );
            strongestChoke = chokeStrengths[0];
            weakestChoke = chokeStrengths[chokeStrengths.Count - 1];
        }
        public void SpawnDragons( ArcenHostOnlySimContext Context )
        {
            int debugCode = 0;
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
                int numDragons = 1;
                if ( aiType.InternalName == "PraetorHard" || aiType.InternalName == "PraetorBrutal" )
                {
                    if ( Context.RandomToUse.Next( 0, 100 ) < 50 )
                        numDragons++;
                }
                debugCode = 1100;
                for ( int j = 0; j < numDragons; j++ )
                {
                    PlanetFaction pFaction = king.Planet.GetPlanetFactionForFaction( praetorian );
                    GameEntityTypeData dragonData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "AIDragon" );
                    GameEntity_Squad dragon = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, dragonData,
                                                                                               king.CurrentMarkLevel,
                                                                                               pFaction.FleetUsedAtPlanet, 0,
                                                                                               king.WorldLocation, Context, "SpireSidekick-AIDragon" );
                    if ( dragon != null )
                    {
                        dragon.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                        dragon.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
                    }
                }
            }
            if ( debugCode > 0 )
            { }
        }
        public Faction GetAvailableFactionForDebris( List<Faction> AIFactionsForDebris, List<Faction> OtherFactionsForDebris, Faction faction, SpireSidekickFactionBaseInfo BaseInfo, ArcenHostOnlySimContext Context )
        {
            for ( int j = 0; j < World_AIW2.Instance.Factions.Count; j++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[j];
                if ( otherFaction.Type == FactionType.Player )
                    continue;
                if ( !otherFaction.SpecialFactionData.CanUseSpireDebris )
                    continue;
                if ( otherFaction.FactionIsDefeated )
                    continue;
                if ( otherFaction.InvasionTime > World_AIW2.Instance.GameSecond )
                    continue; //faction hasn't invaded yet
                if ( otherFaction.Type == FactionType.AI )
                    AIFactionsForDebris.Add( otherFaction );
                else if ( !otherFaction.HasObtainedSpireDebris )
                    OtherFactionsForDebris.Add( otherFaction );
            }
            //Now I have the lists of eligible factions. Now remove all minor factions that are already on an existing piece of debris
            foreach ( GameEntity_Squad debris in BaseInfo.SpireDebris.DisplaySquads() )
            {
                SpireSidekickPerUnitBaseInfo debrisData = debris.TryGetExternalBaseInfoAs<SpireSidekickPerUnitBaseInfo>();
                Faction debrisFaction = World_AIW2.Instance.GetFactionByIndex( debrisData.FactionIndexForDebris );
                if ( debrisFaction == null )
                    continue;
                for ( int j = OtherFactionsForDebris.Count - 1; j >= 0; j-- )
                    if ( OtherFactionsForDebris[j] == debrisFaction )
                        OtherFactionsForDebris.RemoveAt( j );
            }

            //I've now removed all the minor factions currently in use.
            if ( OtherFactionsForDebris.Count > 0 && Context.RandomToUse.Next(0, 80) < 100 )
            {
                //80% chance of picking an available minor faction
                return OtherFactionsForDebris[Context.RandomToUse.Next(0, OtherFactionsForDebris.Count)];
            }
            if ( AIFactionsForDebris.Count > 0 )
                return AIFactionsForDebris[Context.RandomToUse.Next(0, AIFactionsForDebris.Count)];
            return null;
        }

    } 
}
