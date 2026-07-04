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

    public sealed class FallenSpireFactionDeepInfo : ExternalFactionDeepInfoRoot, IExternalDeepInfo_Singleton
    {
        public FallenSpireFactionBaseInfo BaseInfo;
        public static FallenSpireFactionDeepInfo Instance = null;
        public readonly List<Faction> AIFactionsForDebris = List<Faction>.Create_WillNeverBeGCed( 30, "FallenSpireFactionDeepInfo-AIFactionsForDebris" );
        public readonly List<Faction> OtherFactionsForDebris = List<Faction>.Create_WillNeverBeGCed( 30, "FallenSpireFactionDeepInfo-OtherFactionsForDebris" );
        private static readonly List<int> chokeStrengths = List<int>.Create_WillNeverBeGCed( 300, "FallenSpireFactionDeepInfo-chokeStrengths" );
        //fireteam stuff, for the imperial spire
        public readonly List<SafeSquadWrapper> UnassignedShips = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "FallenSpireFactionDeepInfo-UnassignedShips" );
        public static readonly DictionaryOfLists<Planet, SafeSquadWrapper> KingKillersByPlanet = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 10, "FallenSpireFactionDeepInfo-KingKillersByPlanet" );
        public readonly ProtectedValDictionary<Planet, FireteamRegiment> TeamsAimedAtPlanet = ProtectedValDictionary<Planet, FireteamRegiment>.Create_WillNeverBeGCed( 100, "FallenSpireFactionDeepInfo-TeamsAimedAtPlanet" );
        public static readonly List<SafeSquadWrapper> LRPActiveRelics = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 90, "FallenSpireFactionDeepInfo-LRPActiveRelics" );
        public static readonly List<SafeSquadWrapper> AlliedCommandStations = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 90, "FallenSpireFactionDeepInfo-AlliedCommandStations" );
        private static readonly ArcenLessLinkedList<Fireteam> AvailableFireteams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "FallenSpireFactionDeepInfo-AvailableFireteams" );

        public readonly int MinFireteamStrength = 5000;
        public readonly int MaxFireteamStrength = 20000;

        //this just keeps us from having journal entries submitted every second as a possibility.
        //It doesn't matter that we're not caching this between sessions.
        private int LastNumCitiesSeenForJournal = 0;

        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<FallenSpireFactionBaseInfo>();
            Instance = this;
        }

        protected override void Cleanup()
        {
            Instance = null;
            BaseInfo = null;

            //may or may not matter
            AIFactionsForDebris.Clear();
            OtherFactionsForDebris.Clear();
            chokeStrengths.Clear();
            LRPActiveRelics.Clear();
            AlliedCommandStations.Clear();
            AvailableFireteams.Clear();

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
            FallenSpireSharedDeepInfo.Instance.HandleSelfBuildingCompleteLogic( entity, this.AttachedFaction, Context );
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            Instance = this;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.FallenSpire );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "FallenSpire-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f ) : null;
            int debugCode = 0;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                debugCode = 1000;
                this.AttachedFaction.HasBeenSeenByPlayer = true;
                debugCode = 1010;
                debugCode = 1020;
                debugCode = 1030;
                FallenSpireSharedDeepInfo.Instance.DoInitializationIfNecessary( this.AttachedFaction, this.BaseInfo, Context, tracing );

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
                    BaseInfo.CurrentRelicSpawnPlanetIdx = FallenSpireSharedDeepInfo.Instance.GetNextRelicSpawnPoint( AttachedFaction, Context, BaseInfo, false );
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
                    else if ( tracing )
                        tracingBuffer.Add( "No legal planets for relic spawn; does the player own every watched planet?" );
                }
                debugCode = 2000;
                FallenSpireSharedDeepInfo.Instance.UpdateExoData( AttachedFaction, BaseInfo, Context, tracing );

                List<SafeSquadWrapper> activeRelics = this.BaseInfo.ActiveRelics.GetDisplayList();

                /* Do the relic chase response code */
                for ( int i = 0; i < BaseInfo.ActiveRelics.Count; i++ )
                {
                    debugCode = 2100;
                    //this means the player has found a relic on the map, and the AI is in chase mode
                    GameEntity_Squad relic = activeRelics[i].GetSquad();
                    if ( relic == null )
                        continue;
                    FallenSpirePerUnitBaseInfo relicData = relic.CreateExternalBaseInfo<FallenSpirePerUnitBaseInfo>( "FallenSpirePerUnitBaseInfo" );
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
                        List<SafeSquadWrapper> workingTargets = GameEntity_Squad.GetTemporarySquadList( "FallenSpire-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-workingTargets", 10f );
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
                FallenSpireSharedDeepInfo.Instance.HandleSpireDebris( AIFactionsForDebris, OtherFactionsForDebris, this.AttachedFaction, this.BaseInfo, Context, tracing );
                debugCode = 5050;
                //get any attached fleets set up, if we need to.
                FallenSpireSharedDeepInfo.Instance.RecalculateSpireFleetsAndFlagships_MainThreadSimOnly( this.AttachedFaction, this.BaseInfo, Context );
                debugCode = 5200;
                //and what the contents of each mobile fleet should be, after THAT
                FallenSpireSharedDeepInfo.Instance.RecalculateSpireCityMobileFleetContents_MainThreadSimOnly( this.AttachedFaction, this.BaseInfo, Context );
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
                FallenSpireSharedDeepInfo.Instance.HandleImperialSpire( AttachedFaction, chokeStrengths, BaseInfo, Context, pathingCacheData );
                HandleJournal( AttachedFaction, Context );
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLog( "Hit exception in fallen spire stage3 sim. debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
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
            if ( World_AIW2.Instance.GameSecond % 5 != 0 )
                return; //only do this every so often
            if ( BaseInfo.CurrentRelicSpawnPlanetIdx == -1 &&
                 BaseInfo.NumRelicsCaptured == 0 )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_CampaignStartMessage", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients ); //campaign start
            if ( BaseInfo.CurrentRelicSpawnPlanetIdx != -1 )
            {
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
            foreach ( GameEntity_Squad city in this.BaseInfo.SpireCities.DisplaySquads() )
            {
                if ( highestCity == null ||
                    city.CurrentMarkLevel > highestCity.CurrentMarkLevel )
                    highestCity = city;
                if ( newestCity == null ||
                     city.GetSecondsSinceEnteringThisPlanet() < newestCity.GetSecondsSinceEnteringThisPlanet() )
                    newestCity = city;
            }

            if ( BaseInfo.SpireCities.Count > this.LastNumCitiesSeenForJournal )
            {
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
            if ( highestCity.CurrentMarkLevel > BaseInfo.CityMarkLevelForJournal )
            {
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
        }
        #endregion

        #region ConvertRelicIntoCity
        private void ConvertRelicIntoCity( Faction AttachedFaction, GameEntity_Squad playerKing, GameEntity_Squad relic, FallenSpirePerUnitBaseInfo relicData,
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
                List<SafeSquadWrapper> workingTargets = GameEntity_Squad.GetTemporarySquadList( "FallenSpire-ConvertRelicIntoCity-workingTargets", 10f );
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
                FallenSpireSharedDeepInfo.Instance.SpawnDragons( Context );
                GameEntity_Squad.ReleaseTemporarySquadList( workingTargets );

                debugCode = 800;

                
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
                                                                     pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "FallenSpire-NewRelicTrain" );
            FallenSpirePerUnitBaseInfo data = newEntity.CreateExternalBaseInfo<FallenSpirePerUnitBaseInfo>( "FallenSpirePerUnitBaseInfo" );
            data.HopsLeftForTrain = (Int16)(2 + Context.RandomToUse.Next( 1, 4 ));
            ArcenCharacterBuffer workingBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "FallenSpire-CreateSpireRelicTrain-workingBuffer", 10f );
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
            List<Planet> workingPlanetList = Planet.GetTemporaryPlanetList( "FallenSpire-GetRelicTrainPlanet-workingBuffer", 10f );
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
                        PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "FallenSpireGetRelicTrainPlanet", startPlanetOrNull, workingPlanetList[i], PathingMode.Safest, Context, PathCacheData );
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
            List<Planet> workingPlanetList = Planet.GetTemporaryPlanetList( "FallenSpire-GetNextRelicSpawnPoint-workingBuffer", 10f );
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
                    if ( planet.GetControllingFactionType() == FactionType.Player )
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
                            if ( otherPlanet.GetControllingFactionType() == FactionType.Player )
                                foundPlayerPlanetWithinMinHops = true;
                        }
                        if ( foundPlayerPlanetWithinMinHops )
                        {
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
                            if ( otherPlanet.GetControllingFactionType() == FactionType.Player )
                                foundPlayerPlanetWithinMaxHops = true;
                        }
                        if ( !foundPlayerPlanetWithinMaxHops )
                            continue;
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
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "FallenSpire-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            int debugCode = 0;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                if ( BaseInfo == null || BaseInfo.Teams == null )
                    return;
                LRPActiveRelics.Clear();
                UnassignedShips.Clear();
                KingKillersByPlanet.Clear();
                TeamsAimedAtPlanet.Clear();
                AlliedCommandStations.Clear();
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
                    }
                }
                debugCode = 200;
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    debugCode = 300;
                    if ( entity.TypeData.GetHasTag( "SpireRelic" ) )
                    {
                        LRPActiveRelics.Add( entity );
                        continue;
                    }
                    if ( entity.TypeData.GetHasTag( "SpireDebris" ) )
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
                    FallenSpirePerUnitBaseInfo data = relic.TryGetExternalBaseInfoAs<FallenSpirePerUnitBaseInfo>();
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
                         tracingBuffer.Add( "We have " + pair.Value.Count + " king killers on " + planet.Name ).Add( ".\n" );

                     debugCode = 1200;
                     var factionData = planet.GetStanceDataForFaction( AttachedFaction );
                     if ( planet.GetControllingFaction().GetIsHostileTowards( AttachedFaction ) &&
                          factionData[FactionStance.Hostile].TotalStrength > 100 * 1000 ) //we have the regular imperial spire following us, so don't need to totally clear things out
                     {
                         if ( tracing )
                             tracingBuffer.Add( "\tThere are too many enemies here (strength " + factionData[FactionStance.Hostile].TotalStrength + "); stay and fight\n" );
                         continue; //if there are a reasonable number of enemies, fight them. Smaller numbers can be ignored
                    }
                     debugCode = 1300;
                     Planet dest = FactionUtilityMethods.Instance.GetKingKillerTarget( planet, Context );
                     debugCode = 1500;
                     PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "FallenSpireKingKillersLRP", planet, dest, PathingMode.Shortest, Context, pathingCacheData );
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
                                  factionData[FactionStance.Hostile].TotalStrength > 100 * 000 )
                                 break;
                         }
                         debugCode = 2000;
                         if ( tracing )
                             tracingBuffer.Add( "\tHeading to " + dest.Name + " but next stop, " + nextPlanet.Name );
                         FactionUtilityMethods.Instance.Helper_RaidSpecificPlanet( pair.Value, planet, AttachedFaction,
                                                                               World_AIW2.Instance.CurrentGalaxy, nextPlanet, true, Context, pathingCacheData, 5f );
                     }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in fallen spire LRP, debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
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
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "FallenSpire-GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
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
                bool foundDireGuardPost = false;
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.AIPOnDeath ) )
                {
                    //clear out dire guard posts
                    if ( entity.TypeData.SpecialType != SpecialEntityType.DireGuardPost )
                        continue;
                    ListToFill.Add( new FireteamTarget( entity ) );
                    foundDireGuardPost = true;
                }

                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    if ( foundDireGuardPost )
                        break; //not until the dire guard posts are dead
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
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "FallenSpire-GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
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

            FallenSpirePerUnitBaseInfo relicData = relic.TryGetExternalBaseInfoAs<FallenSpirePerUnitBaseInfo>();
            Faction playerfaction = World_AIW2.Instance.GetFactionByIndex( relicData.FactionThatFoundThisRelic );
            if ( playerfaction == null )
                throw new Exception( "Relic was created w/o knowing who summoned it" );
            GameEntityTypeData cityEntityData = GameEntityTypeDataTable.Instance.GetRowByName( this.BaseInfo.ExpertMode ? "SpireCityHub_Expert" : "SpireCityHub" );
            if ( cityEntityData == null )
                throw new Exception( "No spire city hub defined in XML" );
            if ( TransformIntoGalacticCapitol )
            {
                cityEntityData = GameEntityTypeDataTable.Instance.GetRowByName( this.BaseInfo.ExpertMode ? "SpireGalacticCapitol_Expert" : "SpireGalacticCapitol"  );
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
                    null, 0, spawnLocation, Context, "FallenSpire-TransformRelic" );

            Fleet cityFleet = cityCenter.FleetMembership.Fleet;

            cityFleet.NameRaw = "Spire City '" + FallenSpireSharedDeepInfo.Instance.RandomSpireCityName(Context, cityCenter) + "'";
            cityFleet.CreateExternalBaseInfo<FallenSpireCityFleetBaseInfo>("FallenSpireCityFleetBaseInfo");

            relic.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
            return cityCenter;
        }


        
        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "AISpireResearchLab", SeedingType.CapturableWeightsAndMax,
                                                    1, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 4, 9, 2, 99, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal, null, -1 );
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "AISpireCitadel", SeedingType.CapturableWeightsAndMax,
                                                    1, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 4, 9, 2, 99, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal, null, -1 );

        }
    } 
}
