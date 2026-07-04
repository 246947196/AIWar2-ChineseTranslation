using Arcen.Universal;
using System;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// These items are run on various threads, and help us wire together various dlls into being able to call to each other
    /// </summary>
    public class SimPlannerImplementation : SimPlanner
    {
        public override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() 
        {
            //none of these really matter to clear, but why not
            waitingBuffer.Clear();
            hasBeenWaitingSinceTime = -1;
            nextTimeCanRunPausedLogic = 0;
        }

        public SimPlannerImplementation()
        {
            SimPlanner.Instance = this;
        }

        public override void ReinitializeDuringStartOrLoad()
        {
            ArcenShortTermPlanningManager.CleanupAll();
            ArcenExecutionManager.CleanupAll();
            ArcenVariousLongTermContextManager.CleanupAll();

            //other cleanup
            PathBetweenPlanetsForFaction.PathRecalculations = 0;
            PathBetweenPlanetsForFaction.PathsPulledFromCache = 0;

            ProcessCoreLogicForArbitraryFrameOnMainThreadHits = 0;
            AllowedToRunSimStep1 = 0;
            AllowedToRunSimStep2 = 0;
            AllowedToRunSimStep3 = 0;
        }

        #region QueueChatMessageOrCommand
        public override void QueueChatMessageOrCommand( string Message, ChatType Type, string SFXItemInternalName, IChatClickHandler ClickHandlerOrNull, float expire )
        {
            if ( CentralVars.GetShouldNotRunGameStyleLogic() )
                return;
            if ( Message == null )
                Message = string.Empty;

            #region ShowLocallyOnly
            if ( Type == ChatType.ShowLocallyOnly )
            {
                if ( Message.Length > 0 )
                    Engine_Universal.WriteToLocalMomentaryDisplayLog( Message, null );

                if ( SFXItemInternalName != null && SFXItemInternalName.Length > 0 )
                {
                    SFXItem sfxItem = SFXItemTable.Instance.GetRowByNameOrNullIfNotFound( SFXItemInternalName );
                    if ( sfxItem != null )
                        Engine_AIW2.Instance.PresentationLayer.PlayVoiceAtCamera( SoundPropagation.PlayLocallyOnly, sfxItem );
                }
                return;
            }
            #endregion

            Faction localFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            PlayerAccount localPlayer = PlayerAccount.Local;

            string lowercase = Message.ToLower();
            GameCommand command;
            if ( lowercase.Contains( "cmd:" ) && localFactionOrNull != null && localPlayer != null )
            {
                //this is a cheat or command!
                command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.ChatCommand], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedIntegers2.Add( Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed() );
                command.RelatedIntegers3.Add( localFactionOrNull.FactionIndex );
                command.RelatedIntegers3.Add( localPlayer.PlayerPrimaryKeyID );
            }
            else
            {
                //this is a chat message!
                command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.Chat], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.ClickHandlerOrNull = ClickHandlerOrNull;
                command.RelatedFInts.Add(FInt.CreateFromDoubleNonSim(expire));
            }

            command.RelatedString = Message;
            if ( SFXItemInternalName != null && SFXItemInternalName.Length > 0 )
                command.RelatedString2 = SFXItemInternalName;
            
            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, false );
        }
        #endregion

        #region QueueLogJournalEntryToSidebar
        /// <summary>
        /// Returns true if the entry was actually played/added.
        /// </summary>
        public override bool QueueLogJournalEntryToSidebar( 
            string UniqueID_ToUseIfGroupIDNotPresent, string OptionalGroupID, Faction RelatedFactionOrNull, GameEntityTypeData RelatedUnitTypeOrNull, Planet RelatedPlanetOrNull, OnClient Client )
        {
            if ( CentralVars.GetShouldNotRunGameStyleLogic() )
                return false;
            
            if ( Client == OnClient.DoThisOnHostOnly_WillBeSentToClients )
            {
                if ( !ArcenNetworkAuthority.GetIsHostMode() )
                    return false;
            }

            // Sometimes blank stuff gets submitted here, especially from voice lines.
            if (string.IsNullOrEmpty(UniqueID_ToUseIfGroupIDNotPresent) && 
                string.IsNullOrEmpty(OptionalGroupID))
            {
                return false;
            }

            // if we are logging a very specific one
            if (!string.IsNullOrEmpty(UniqueID_ToUseIfGroupIDNotPresent))
            {
                JournalEntry entry = JournalEntryTable.Instance.GetRowByNameOrNullIfNotFound( UniqueID_ToUseIfGroupIDNotPresent );
                if ( entry != null )
                {
                    if ( entry.IsBlockedFromTriggering )
                        return false;
                }
                else
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "QueueLogJournalEntryToSidebar: journal entry not found: '" + UniqueID_ToUseIfGroupIDNotPresent + "'", Verbosity.ShowAsError );
                    return false;
                }

                if (!entry.CanRecordAnotherCopyIfAlreadyRecordedInThisCampaign)
                {
                    for ( int i = 0; i < World_AIW2.Instance.JournalHistory.Count; i++ )
                    {
                        try
                        {
                            if (World_AIW2.Instance.JournalHistory[i]?.UniqueID == UniqueID_ToUseIfGroupIDNotPresent)
                                return false;
                        }
                        catch { }
                    }
                }
            }
            
            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.JournalEntry], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
            command.RelatedString = UniqueID_ToUseIfGroupIDNotPresent;
            command.RelatedString2 = OptionalGroupID;
            command.RelatedString3 = RelatedUnitTypeOrNull == null ? string.Empty : RelatedUnitTypeOrNull.InternalName;
            command.RelatedIntegers.Add( RelatedFactionOrNull == null ? -1 : RelatedFactionOrNull.FactionIndex );
            command.RelatedIntegers2.Add( RelatedPlanetOrNull == null ? -1 : RelatedPlanetOrNull.Index );

            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, false );
            
            return true;
        }
        #endregion

        public override void FixAnyBrokenSimBitsOnHost()
        {
            //actually, nothing to do here for now.  We'll fix most things on a load of a savegame or handle local world start.
        }
        private static ArcenDoubleCharacterBuffer waitingBuffer = new ArcenDoubleCharacterBuffer( "SimPlannerImplementation-waitingBuffer" );

        private float hasBeenWaitingSinceTime = -1;
        private float nextTimeCanRunPausedLogic = 0;

        private static string _lastDidNotRunReason = string.Empty;
        private static float _lastDidNotRunTime = 0;
        
        private static void SetDidNotRun(string reason, bool norVisualUpdate=false)
        {
            _lastDidNotRunTime = ArcenTime.TimeSinceStartF;
            _lastDidNotRunReason = reason;
        }
        
        private static void SetDidRun()
        {
            var time = ArcenTime.TimeSinceStartF;
            if (!string.IsNullOrEmpty(_lastDidNotRunReason) && (time - _lastDidNotRunTime) > 2.0f)
            {
                _lastDidNotRunReason = null;
            }
        }
        
        public static bool GetDidNotRun(out string reason, out float elapsed)
        {
            if (!string.IsNullOrEmpty(_lastDidNotRunReason))
            {
                reason = _lastDidNotRunReason;
                elapsed = ArcenTime.TimeSinceStartF - _lastDidNotRunTime;
                
                return true;
            }
            
            reason = null;
            elapsed = 0;
            
            return false;
        }
        
        private int doLRPChecksAfterXFrames = -1;

        public static long ProcessCoreLogicForArbitraryFrameOnMainThreadHits = 0;
        public static long AllowedToRunSimStep1 = 0;
        public static long AllowedToRunSimStep2 = 0;
        public static long AllowedToRunSimStep3 = 0;

        //Phase 2 adaptive frame-budget controller state (host/SP-only; per-peer & non-deterministic — it only DECIDES when to
        //emit the synced, frame-bound GameCommand_ChangeFrameSize, so the actual sim scaling stays in lockstep across peers).
        private static float _autoBudget_lastDecisionRealtime = -1f;
        private static int _autoBudget_overBudgetTicks = 0;
        private static int _autoBudget_keepingPaceTicks = 0;
        private const double AUTO_BUDGET_UP_THRESHOLD = 0.80;   //sim-step compute using >80% of the per-step budget (sustained) => grow (matches the in-game ">80ms -> +1 speed" guidance)
        private const double AUTO_BUDGET_DOWN_THRESHOLD = 0.55; //using <55% of budget (sustained, only if engaged) => shrink back toward intent; the 0.55..0.80 gap is the hysteresis band
        private const int AUTO_BUDGET_UP_TICKS_REQUIRED = 2;    //~2s sustained over budget before stepping up (escalate fairly fast)
        private const int AUTO_BUDGET_DOWN_TICKS_REQUIRED = 12; //~12s sustained headroom before stepping back down (de-escalate slow)

        //Phase 2: the host's (or single-player's) once-per-second decision on whether to grow/shrink the synced FrameSizeMultiplier
        //so that sustained over-budget load is absorbed by fewer/bigger sim steps while game-speed (the player's intent) is held.
        private static void ConsiderAutoFrameBudget_HostOnly()
        {
            World_AIW2 w = World_AIW2.Instance;
            if ( w == null )
                return;
            //Clients never drive the synced multiplier (they would race the host's intended value); only the host decides.
            //Single-player counts as host (DesiredStatus is not Client), so the auto-budget also helps heavy single-player.
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;
            if ( w.InSetupPhase || World.Instance.IsPaused )
            {
                //Reset the hysteresis so a pause / setup phase / new game in the same process doesn't carry stale escalation state.
                _autoBudget_lastDecisionRealtime = -1f;
                _autoBudget_overBudgetTicks = 0;
                _autoBudget_keepingPaceTicks = 0;
                return;
            }

            float now = ArcenTime.TimeSinceStartF;
            if ( _autoBudget_lastDecisionRealtime > 0f && now - _autoBudget_lastDecisionRealtime < 1f )
                return; //at most one decision per real second
            _autoBudget_lastDecisionRealtime = now;

            FInt F = w.FrameSizeMultiplier;
            FInt PTS = w.PlayerTargetSpeedMultiplier;
            Faction faction = w.GetLocalPlayerFactionOrNaturalObjectsNeverNull();

            if ( !GameSettings.Current.GetBoolBySetting( "Network_HostAdaptiveFrameBudget" ) )
            {
                //Feature off: behave exactly as before, but if a prior on-period left the frame-size auto-inflated above the
                //player's intent, ease it back down (one step/sec) so turning the toggle off cleanly reverts.
                _autoBudget_lastDecisionRealtime = -1f;
                _autoBudget_overBudgetTicks = 0;
                _autoBudget_keepingPaceTicks = 0;
                if ( F > PTS )
                    EndpointFunctions.IncreaseOrDecreaseFrameSize( faction, GameCommandSource.AnythingElse, false, false ); //last arg false: silent (no manual-tweak click)
                return;
            }

            //load = the worst peer's ACTUAL sim-step compute time as a fraction of its per-step budget (1.0 == exactly on budget),
            //from the real measured sim-step ms — so we can de-escalate on genuine headroom, no probing needed. Budget for the
            //slowest peer.
            double load = w.GetWorstPeerLoadRatio_ServerOnly();

            if ( load > AUTO_BUDGET_UP_THRESHOLD )
            {
                _autoBudget_keepingPaceTicks = 0;
                _autoBudget_overBudgetTicks++;
                if ( _autoBudget_overBudgetTicks >= AUTO_BUDGET_UP_TICKS_REQUIRED )
                {
                    _autoBudget_overBudgetTicks = 0;
                    //Only grow within the conservative auto fidelity cap (manual fast-forward can still go higher than this).
                    if ( F < (FInt)World_AIW2.AUTO_MAX_FRAME_SIZE_MULTIPLIER )
                        EndpointFunctions.IncreaseOrDecreaseFrameSize( faction, GameCommandSource.AnythingElse, true, false ); //last arg false: silent (no manual-tweak click)
                }
            }
            else if ( load < AUTO_BUDGET_DOWN_THRESHOLD && F > PTS )
            {
                //Genuine headroom AND the auto-budget is currently engaged (F above the player's intent): ease back down. Slow,
                //so a brief lull does not immediately undo a needed adjustment; if we drop too far we simply re-detect the load.
                _autoBudget_overBudgetTicks = 0;
                _autoBudget_keepingPaceTicks++;
                if ( _autoBudget_keepingPaceTicks >= AUTO_BUDGET_DOWN_TICKS_REQUIRED )
                {
                    _autoBudget_keepingPaceTicks = 0;
                    EndpointFunctions.IncreaseOrDecreaseFrameSize( faction, GameCommandSource.AnythingElse, false, false ); //last arg false: silent (no manual-tweak click)
                }
            }
            else
            {
                //In the hysteresis band (0.55..0.80) or already at the player's intended speed: hold steady.
                _autoBudget_overBudgetTicks = 0;
                _autoBudget_keepingPaceTicks = 0;
            }
        }

        #region ProcessCoreLogicForArbitraryFrameOnMainThread
        public override void ProcessCoreLogicForArbitraryFrameOnMainThread( ref bool stalledWaitingOnConnections, ref bool mayUpdateVisuals, ref string reasonForNoVisualUpdates )
        {
            ProcessCoreLogicForArbitraryFrameOnMainThreadHits++;

            if ( World_AIW2.Instance == null )
            {
                reasonForNoVisualUpdates = "World_AIW2.Instance is null";
                SetDidNotRun("World_AIW2.Instance is null");
                
                return;
            }

            if ( CentralVars.GetShouldNotRunGameStyleLogic() )
            {
                reasonForNoVisualUpdates = "!CentralVars.GetShouldNotRunGameStyleLogic()";
                SetDidNotRun("!CentralVars.GetShouldNotRunGameStyleLogic()");
                
                return;
            }
            int debugStage = 0;
            try
            {
                #region actual sim step processing
                debugStage = 1000;

                this.DoInitialViewCenteringOnClientOrHostIfNeeded();

                if ( World_AIW2.Instance.InSetupPhase )
                {
                    #region The Subset Of Logic For The Lobby
                    Engine_AIW2.Instance.CurrentSimFrameVisualOnly++;
                    debugStage = 1200;
                    if ( !Mapgen.IsMapCurrentlyGenerating &&
                         ArcenNetworkAuthority.GetIsHostMode() )
                    {
                        if ( World_AIW2.Instance.Setup.ChangedSinceLastMapGenCall && !Mapgen.IsMapCurrentlyGenerating )
                        {
                            debugStage = 1300;
                            //ArcenDebugging.ArcenDebugLogSingleLine( "World_AIW2.Instance.SetupStoredLongTerm.ChangedSinceLastMapGenCall: '" + 
                            //    World_AIW2.Instance.SetupStoredLongTerm.ChangedSinceLastMapGenCall + "'", Verbosity.DoNotShow );
                            Mapgen.GenerateMap( null, delegate
                            {
                                World_AIW2.Instance.Setup.ChangedSinceLastMapGenCall = false;
                            } );
                            debugStage = 1400;
                        }
                    }

                    World_AIW2.Instance.OnClientOrHost_ExecuteGameCommandsThatWereScheduled( Engine_AIW2.Instance.MainThreadContext_ClientOrHost );
                    #endregion

                    //reasonForNoVisualUpdates = "InSetupPhase";
                    mayUpdateVisuals = true;
                    hasBeenWaitingSinceTime = -1;
                    SetDidNotRun("World_AIW2.Instance.InSetupPhase");
                    
                    return;
                }
                else
                {
                    debugStage = 2000;
                    ArcenVariousLongTermContextManager.RunAllContexts_ForPausedOrUnpaused();
                    debugStage = 2100;
                    if ( !World.Instance.IsPaused ) //do all the non-paused stuff
                        EntitySimLogicDeepInfo.Instance.ArcenLongTermContinuousPlanningContext_RunAllContexts_ForUnpausedOnly();
                    else //oh, we're paused!
                    {
                        #region Logic To Run Periodically While Paused
                        ArcenClientOrHostSimContextCore context = Engine_AIW2.Instance.MainThreadContext_ClientOrHost;

                        if ( nextTimeCanRunPausedLogic <= ArcenTime.TimeSinceStartF )
                        {
                            nextTimeCanRunPausedLogic = ArcenTime.TimeSinceStartF + 1f; //wait one second until next time

                            foreach ( GameEntity_Squad squad in World_AIW2.Instance.Squads() )
                            {
                                if ( squad != null )
                                    squad.DoPerSecondWhenPaused( context );
                            }
                        }

                        EntitySimLogicDeepInfo.Instance.LongTermIntermittent_HandleFactionLogicWhilePaused();
                        #endregion
                    }

                    //we are not in setup, so add any accumulated time;
                    float accumulation = ArcenTime.AccumulatedPre_NonSetupGeneralGameTimeWorkingF;
                    if ( accumulation > 0 )
                        ArcenTime.NonSetupGeneralGameTimeSinceLastLoadOrStartF += accumulation;
                    ArcenTime.AccumulatedPre_NonSetupGeneralGameTimeWorkingF = 0;

                }
                //ArcenDebugging.ArcenDebugLogSingleLine( "ArcenExecutionManager.MessageFromMainThreadToMe_ExecutionRequested: " +
                //    ArcenExecutionManager.MessageFromMainThreadToMe_ExecutionRequested +
                //    " IsPaused: " + World.Instance.IsPaused, Verbosity.DoNotShow ); //PAUSE_2_TODO

                debugStage = 3000;
                {
                    mayUpdateVisuals = true;
                    //if ( ArcenShortTermPlanningContext.MessageFromMainThreadToMe_PlanningRequested )
                    //    reasonForNoVisualUpdates = "!ArcenExecutionManager.MessageFromMainThreadToMe_ExecutionRequested and ArcenShortTermPlanningContext.MessageFromMainThreadToMe_PlanningRequested";
                    //else if ( !ArcenShortTermPlanningContext.GetAreAllPlanningContextsDone() )
                    //    reasonForNoVisualUpdates = "!ArcenExecutionManager.MessageFromMainThreadToMe_ExecutionRequested and !ArcenShortTermPlanningContext.GetAreAllPlanningContextsDone()";
                    //else
                    //    reasonForNoVisualUpdates = "!ArcenExecutionManager.MessageFromMainThreadToMe_ExecutionRequested and ??? internal1";
                    debugStage = 3100;
                    //Engine_Universal.BeginProfilerSample( "Branch2" );
                    //Engine_Universal.BeginProfilerSample( "Short.GetAreAllPlanningContextsDone" );
                    bool isAllowedToRunSimStep = true;

                    //second part of halting the sim
                    if ( CentralVars.GetShouldNotRunGameStyleLogic() )
                    {
                        SetDidNotRun("GetShouldNotRunGameStyleLogic()");
                        isAllowedToRunSimStep = false;
                    }
                    if ( CentralVars.DEBUG_TURN_OFF_MAIN_SIM_EXECUTION )
                    {
                        isAllowedToRunSimStep = false;
                        SetDidNotRun("DEBUG_TURN_OFF_MAIN_SIM_EXECUTION");
                    }
                    if ( World_AIW2.Instance.IsSimulationArtificiallyStopped )
                    {
                        isAllowedToRunSimStep = false;
                        SetDidNotRun("IsSimulationArtificiallyStopped");
                    }
                    if ( !ArcenExecutionManager.GetIsSoleExecutionContextDone() )
                    {
                        isAllowedToRunSimStep = false;
                        SetDidNotRun("!GetIsSoleExecutionContextDone()");
                    }
                    if ( !ArcenShortTermPlanningManager.GetHasFinishedAllShortTermContexts() )
                    {
                        if (!AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "CoarseShortTermPlanning" ))
                        {
                            isAllowedToRunSimStep = false;
                            SetDidNotRun("!ArcenShortTermPlanningManager.GetHasFinishedAllShortTermContexts()");
                        }
                    }

                    #region Calculate Our Recent Frame Times From Here To Here
                    //yes we can know our general framerate from elsewhere, but the big thing is that we want
                    //to know how long it is taking between intervals of getting HERE, specifically.
                    //down below is where Network_CurrentFrameNumber++ happens, so that's what is relevant
                    //for calculating our sim speed including all outside factors like network lag and uneven frames

                    //if this is the first frame since the last reset, so make sure not to use any other recent time intervals
                    if ( World_AIW2.Instance.OnClientOrHost_LastFrameIntervalMarker <= 0 )
                    {
                        World_AIW2.Instance.OnClientOrHost_LastFrameIntervalMarker = ArcenTime.TimeSinceStartF;
                        World_AIW2.Instance.OnClientOrHost_RecentTimeIntervalsBetweenSimFrames.Clear();
                    }
                    else
                    {
                        //how long has it been since the last frame?
                        float currentFrameInterval = ArcenTime.TimeSinceStartF - World_AIW2.Instance.OnClientOrHost_LastFrameIntervalMarker;
                        World_AIW2.Instance.OnClientOrHost_LastFrameIntervalMarker = ArcenTime.TimeSinceStartF;

                        World_AIW2.Instance.OnClientOrHost_RecentTimeIntervalsBetweenSimFrames.Enqueue( currentFrameInterval );
                        //only keep the most recent 30 frames to average, no matter the framerate
                        while ( World_AIW2.Instance.OnClientOrHost_RecentTimeIntervalsBetweenSimFrames.Count > 30 )
                            World_AIW2.Instance.OnClientOrHost_RecentTimeIntervalsBetweenSimFrames.TryDequeue( out float unused );
                    }
                    #endregion

                    if ( isAllowedToRunSimStep )
                    {
                        AllowedToRunSimStep1++;

                        //only stop us from running based on time if we know when the last run time was
                        if ( World_AIW2.Instance.OnClientOrHost_TimeStartedLastSimFrame > 0 ) 
                        {
                            //has it been long enough since we last STARTED the last sim step?
                            //we measure from start to start, not from end to start.
                            float currentSimStepInterval = ArcenTime.TimeSinceStartF - World_AIW2.Instance.OnClientOrHost_TimeStartedLastSimFrame;
                            bool isClient = ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client;
                            if ( isClient && World_AIW2.Instance.Network_AuthorizedToExecuteThroughFrameNumber >= World_AIW2.Instance.Network_CurrentFrameNumber + 2 )
                            {
                                //on a client that is at least 2 frames behind, don't block anything ever!  Because holy cow there also
                            }
                            else if ( currentSimStepInterval > 2 )
                            {} //don' stop us from running if it's been more than 2 seconds since we last run, because holy cow
                            else
                            {
                                float currentFrameFrequencyMultiplier = World_AIW2.Instance.GetCurrentFrameFrequencyMulitplier();
                                float targetIntervalBetweenSimSteps = (World_AIW2.Instance.SimulationProfile.SecondsPerFrameNonSim * currentFrameFrequencyMultiplier);
                                if ( currentSimStepInterval < targetIntervalBetweenSimSteps )
                                {
                                    //if it has not been long enough between the last im step and the next one, then
                                    //let's consider something a bit more.  Frames are uneven, and the next frame may be
                                    //annoyingly far into the future for this to kick off.  So let's see if that is the case.
                                    float totalFrameTime = 0;
                                    int frameCount = 0;
                                    foreach ( float time in World_AIW2.Instance.OnClientOrHost_RecentTimeIntervalsBetweenSimFrames )
                                    {
                                        totalFrameTime += time;
                                        frameCount++;
                                    }
                                    if ( frameCount <= 0 )
                                    {
                                        //nevermind, we have no recent frame data, so just block it
                                        isAllowedToRunSimStep = false;
                                    }
                                    else
                                    {
                                        //on average over the last 30 frames, extra long frames and all, this is our average
                                        float averageFrameTimeLately = totalFrameTime / frameCount;
                                        //is 55% of our average going to put us past our target time?
                                        float fiftyFivePercentOfAverage = averageFrameTimeLately * 0.55f;
                                        if ( currentSimStepInterval + fiftyFivePercentOfAverage >= targetIntervalBetweenSimSteps )
                                        {
                                            //yeah, we'd be past it.  You know what, then?  Let's run the frame now, a little early
                                            //it will be smoother that way.
                                        }
                                        else
                                        {
                                            //nah, that won't push us past it.  Still block the sim step, we'll check again next frame.
                                            isAllowedToRunSimStep = false;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if ( isAllowedToRunSimStep )
                    {
                        AllowedToRunSimStep2++;

                        //if we don't have permission to run the next sim step yet, then don't!
                        if ( World_AIW2.Instance.Network_CurrentFrameNumber >= World_AIW2.Instance.Network_AuthorizedToExecuteThroughFrameNumber )
                        {
                            isAllowedToRunSimStep = false;
                            SetDidNotRun("Network_CurrentFrameNumber >= Network_AuthorizedToExecuteThroughFrameNumber");

                            #region Inform the UI about having to wait on sim steps
                            //ArcenDebugging.ArcenDebugLogSingleLine( "World_AIW2.Instance.Network_CurrentFrameNumber " + World_AIW2.Instance.Network_CurrentFrameNumber
                            //    + " " + World_AIW2.Instance.Network_AuthorizedToExecuteThroughFrameNumber, Verbosity.DoNotShow );  //PAUSE_3_TODO
                            debugStage = 4100;
                            //ArcenDebugging.ArcenDebugLogSingleLine( "Waiting On Server, at frame " + World_AIW2.Instance.CurrentFrameNumber + " client mode: " + ArcenNetworkAuthority.GetIsClientMode() +
                            //    " SocketMode: " + (ArcenNetworkAuthority.ActiveSocket == null ? "nullsocket" : ArcenNetworkAuthority.GetArcenSocketModeAsString()), Verbosity.Chat );

                            waitingBuffer.EnsureResetForNextUpdate();
                            if ( ArcenNetworkAuthority.GetIsHostMode() )
                            {
                                debugStage = 4200;
                                if ( hasBeenWaitingSinceTime < 0 )
                                    hasBeenWaitingSinceTime = ArcenTime.TimeSinceStartF;
                                int countWaitingOn = ArcenNetworkAuthority.GetCountOfClientsNeedingWorldData();
                                debugStage = 4300;
                                if ( countWaitingOn > 0 )
                                {
                                    debugStage = 4400;
                                    waitingBuffer.Add( string.Format( "Waiting on {0} connection{1}.", countWaitingOn, countWaitingOn > 1 ? "s" : "" ) );
                                    for ( int i = 0; i < ArcenNetworkAuthority.ClientConnections.Count; i++ )
                                    {
                                        ArcenNetworkClientConnection player = ArcenNetworkAuthority.ClientConnections[i];
                                        if ( !player.HasWorldDataRecieptBeenVerified )
                                        {
                                            if ( player.ProfileName.Length > 0 )
                                            {
                                                if ( !player.HasBeenSentWorldData )
                                                    waitingBuffer.Add( "\n" ).Add( "Sending world data to " ).Add( player.ProfileName );
                                                else
                                                    waitingBuffer.Add( "\n" ).Add( "Waiting for " ).Add( player.ProfileName ).Add( " to build world from our data." );
                                            }
                                            else
                                                waitingBuffer.Add( "\n" ).Add( "Waiting for " ).Add( player.ConnectionUniqueIdentifier )
                                                    .Add( " (connection " ).Add( player.ConnectionIndex ).Add( ") to send their name, expansions, and mods status." );
                                        }
                                    }
                                }
                                else
                                {
                                    debugStage = 4800;
                                    countWaitingOn = World_AIW2.Instance.GetCountOfPlayerAccountsNeedingToCatchUp_ServerOnly();
                                    if ( countWaitingOn > 0 )
                                    {
                                        waitingBuffer.Add( string.Format( "Waiting on {0} connection{1}.", countWaitingOn, countWaitingOn > 1 ? "s" : "" ) );
                                        int minFrameMustBeAt = World_AIW2.Instance.Network_AuthorizedToExecuteThroughFrameNumber - World_AIW2.Instance.SimulationProfile.MaxNumberOfFramesServerIsAllowedToGetAheadOfClient;
                                        for ( int i = 0; i < World.Instance.AllPlayerAccounts.Count; i++ )
                                        {
                                            PlayerAccount acc = World.Instance.AllPlayerAccounts[i];
                                            if ( acc.Network_IsHost )
                                                continue; //the host can't get ahead of itself
                                            if ( !acc.OnServer_GetIsConnected() )
                                                continue; //don't worry about ones not conected

                                            if ( acc.Network_HasFinishedThroughFrame < minFrameMustBeAt )
                                                waitingBuffer.Add( "\n" ).Add( acc.Username ).Add( " is behind." );
                                        }
                                    }
                                    else
                                        waitingBuffer.Add( string.Format( "Waiting on host (ourselves) at frame {0}, but not sure why.", World_AIW2.Instance.Network_CurrentFrameNumber ) );
                                }
                            }
                            else //is client
                            {
                                debugStage = 5100;
                                if ( hasBeenWaitingSinceTime < 0 )
                                    hasBeenWaitingSinceTime = ArcenTime.TimeSinceStartF;
                                waitingBuffer.Add( string.Format( "Waiting on host at frame {0}, not sure why yet.", World_AIW2.Instance.Network_CurrentFrameNumber ) );
                            }
                            debugStage = 5500;
                            if ( ArcenTime.TimeSinceStartF - hasBeenWaitingSinceTime > 1f )
                                CenterScreenPopupData.CreateAndLogNewOrExtendExistingAndReplaceText( waitingBuffer.GetStringAndResetForNextUpdate(), "MPSTALL", 0.2f );
                            stalledWaitingOnConnections = true;

                            //we are waiting, so accumulate no time
                            ArcenTime.AccumulatedPre_NonSetupNetworkGameTimeWorkingF = 0;
                            #endregion end Inform the UI about having to wait on sim steps
                        }
                        else
                        {
                            debugStage = 6100;
                            //we are not waiting, so add any accumulated time;
                            float accumulation = ArcenTime.AccumulatedPre_NonSetupNetworkGameTimeWorkingF;
                            if ( accumulation > 0 )
                                ArcenTime.NonSetupNetworkReadyGameTimeSinceLastLoadOrStartF += accumulation;
                            ArcenTime.AccumulatedPre_NonSetupNetworkGameTimeWorkingF = 0;

                            hasBeenWaitingSinceTime = -1;
                        }
                    }

                    debugStage = 7000;
                    //ArcenDebugging.ArcenDebugLogSingleLine( "HEY!  IsSimulationArtificiallyStopped: " +
                    //    World_AIW2.Instance.IsSimulationArtificiallyStopped +
                    //    " contextsDone: " + contextsDone +
                    //    " GetShouldNotRunGameStyleLogic: " + CentralVars.GetShouldNotRunGameStyleLogic() +
                    //    " Short.GetAreAllPlanningContextsDone: " + ArcenShortTermPlanningContext.GetAreAllPlanningContextsDone() +
                    //    " IsPaused: " + World.Instance.IsPaused, Verbosity.DoNotShow );  //PAUSE_2_TODO

                    //Engine_Universal.EndProfilerSample( "Short.GetAreAllPlanningContextsDone" );
                    debugStage = 7100;
                    if ( isAllowedToRunSimStep )
                    {
                        AllowedToRunSimStep3++;

                        debugStage = 7200;
                        SetDidRun();

                        World_AIW2.Instance.OnClientOrHost_TimeStartedLastSimFrame = ArcenTime.TimeSinceStartF;
                        if ( ArcenFramerateTracker.CurrentFramesPerSecond >= 100 )
                            doLRPChecksAfterXFrames = 5;
                        else if ( ArcenFramerateTracker.CurrentFramesPerSecond >= 70 )
                            doLRPChecksAfterXFrames = 4;
                        else if ( ArcenFramerateTracker.CurrentFramesPerSecond >= 60 )
                            doLRPChecksAfterXFrames = 3;
                        else if ( ArcenFramerateTracker.CurrentFramesPerSecond >= 30 )
                            doLRPChecksAfterXFrames = 2;
                        else
                            doLRPChecksAfterXFrames = 1;

                        #region Execute any gamecommands for the current sim step
                        World_AIW2.Instance.OnClientOrHost_ExecuteGameCommandsThatWereScheduled( Engine_AIW2.Instance.MainThreadContext_ClientOrHost );
                        #endregion

                        #region Now actually do a sim step (it will be on a bg thread)
                        debugStage = 7300;
                        //Engine_Universal.BeginProfilerSample( "starting execution contexts" );
                        World_AIW2.Instance.Network_CurrentFrameNumber++;
                        debugStage = 7400;
                        World_AIW2.Instance.IncrementCurrentSimCycles();
                        ConsiderAutoFrameBudget_HostOnly(); //Phase 2 adaptive frame-budget (host/SP-only; self-rate-limited to once/sec)
                        debugStage = 7500;
                        ArcenExecutionManager.RunAllContexts();
                        //Engine_Universal.EndProfilerSample( "starting execution contexts" );                        
                        #endregion

                        //ArcenDebugging.ArcenDebugLogSingleLine( "ArcenExecutionManager.RunAllContexts()", Verbosity.DoNotShow );  //PAUSE_2_TODO
                    }
                    debugStage = 9100;

                    #region Checks For Triggering Long Range Planning
                    if ( doLRPChecksAfterXFrames > 0 && !World_AIW2.Instance.InSetupPhase )
                    {
                        debugStage = 9200;
                        doLRPChecksAfterXFrames--;
                        //ArcenDebugging.ArcenDebugLogSingleLine( "executionContextsDone: " +
                        //    executionContextsDone +
                        //    " MessageFromMainThreadToMe_PlanningRequested: " + ArcenShortTermPlanningContext.MessageFromMainThreadToMe_PlanningRequested +
                        //    " ShortTermAllDone: " + ArcenShortTermPlanningContext.GetAreAllPlanningContextsDone() +
                        //    " IsPaused: " + World.Instance.IsPaused, Verbosity.DoNotShow );  //PAUSE_2_TODO
                        //Engine_Universal.EndProfilerSample( "GetAreAllExecutionContextsDone" );
                        if ( doLRPChecksAfterXFrames == 0 )
                        {
                            doLRPChecksAfterXFrames = -1;
                            //Engine_Universal.BeginProfilerSample( "Responding to completed execution contexts" );
                            Engine_AIW2.Instance.CurrentSimFrameVisualOnly++;

                            {
                                debugStage = 9300;
                                mayUpdateVisuals = true;
                                debugStage = 9400;
                                #region keep long-range planning going (and apply changes, if they're ready)
                                //Engine_Universal.BeginProfilerSample( "Keep Long Range Planning Going" );
                                bool debugging = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Thread );
                                if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "checking whether should run ArcenLongTermPlanningContext.RunAllContexts()"
                                    + "\n\t" + "World_AIW2.Instance.Network_CurrentFrameNumber" + "=" + (World_AIW2.Instance.Network_CurrentFrameNumber)
                                    + "\n\t" + "World_AIW2.Instance.SimulationProfile.LongTermPlanningOnServerInterval_Frames" + "=" + (World_AIW2.Instance.SimulationProfile.LongTermPlanningOnServerInterval_Frames)
                                    , Verbosity.DoNotShow );
                                debugStage = 9500;

                                EntitySimLogicDeepInfo.Instance.LongTermIntermittent_RunAllContexts();
                                //Engine_Universal.EndProfilerSample( "Keep Long Range Planning Going" );
                                #endregion

                                debugStage = 9600;
                                if ( Engine_Universal.DebugSimStepsToRun > 0 )
                                {
                                    Engine_Universal.DebugSimStepsToRun--;
                                    if ( Engine_Universal.DebugSimStepsToRun <= 0 )
                                        Engine_Universal.DebugTimeMode = DebugTimeMode.StopSim;
                                }
                            }
                            //Engine_Universal.EndProfilerSample( "Responding to completed execution contexts" );
                        }
                    }
                    #endregion end Checks For Triggering Long Range Planning

                    debugStage = 11200;
                }
                #endregion
                debugStage = 12200;
                #region increment CampaignRealSecondPartial, CampaignRealSecondsPlayed, and profile.TotalGameSecondsPlayed if not paused
                World.Instance.CampaignRealSecondPartial_NonSim += Engine_Universal.GameDeltaTimeWithSimSpeed;
                debugStage = 12300;
                while ( World.Instance.CampaignRealSecondPartial_NonSim >= 1f )
                {
                    World.Instance.CampaignRealSecondsPlayed_NonSim += 1;
                    if ( !Engine_Universal.InUnitTestMode )
                    {
                        debugStage = 12400;
                        PlayerProfile.Local.TotalGameSecondsPlayed += 1;
                    }
                    World.Instance.CampaignRealSecondPartial_NonSim -= 1f;
                }
                //    ArcenDebugging.StopStopwatch();
                //    ArcenDebugging.WriteStopwatchTime( true );
                //    ArcenDebugging.ResetStopwatch();
                #endregion
            }
            catch ( ArcenThreadsAreShutDownRightNowSoYouCannotHaveThatException ) { } //whoops, we tried to do a no-no
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "ProcessCoreLogicForArbitraryFrameOnMainThread exception at debugStage " + debugStage + ", exception: " + e, Verbosity.ShowAsError );
            }
        }
        #endregion end ProcessCoreLogicForArbitraryFrameOnMainThread

        #region DoInitialViewCenteringOnClientOrHostIfNeeded
        private void DoInitialViewCenteringOnClientOrHostIfNeeded()
        {
            if ( !World_AIW2.Instance.BeforeStartOnly_HaveDoneInitialViewCentering && !World_AIW2.Instance.IsOutsideOfNormalGameplay )
            {
                Planet planet = null;
                bool switchToPlanetView = false;
                if ( World_AIW2.Instance.GameSecond <= 0 )
                {
                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    if ( localFaction == null )
                    {
                        GameEntity_Squad entity = World_AIW2.Instance.GetFirstEntityMatching( SpecialEntityType.HumanHomeCommand, EntityRollupType.KingUnitsOnly, null, false, false );
                        if ( entity != null ) planet = entity.Planet;
                    }
                    else
                    {
                        GameEntity_Squad entity = localFaction.GetFirstMatching( SpecialEntityType.HumanHomeCommand, false, false );
                        if ( entity != null ) planet = entity.Planet;
                    }
                }
                else
                {
                    switchToPlanetView = false;
                    PlayerAccount_AIW2 local = PlayerAccount_AIW2.LocalOrNull;
                    if ( local != null )
                        planet = World_AIW2.Instance.GetPlanetByIndex( local.ViewingPlanetIndex );
                }

                //Chris says: do not do this!  It's not needed, I don't think, and generally overwrites the better logic
                //if ( planet == null )
                //    planet = World_AIW2.Instance.CurrentGalaxy.GetFirstNonDestroyedPlanet();

                if ( planet != null )
                {
                    Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( planet, true );
                    if ( switchToPlanetView )
                    {
                        World_AIW2.Instance.SwitchViewToPlanet( planet );
                        Engine_AIW2.Instance.PresentationLayer.ReactToEnteringPlanetView( planet );
                    }
                }
                World_AIW2.Instance.BeforeStartOnly_HaveDoneInitialViewCentering = true;
            }
        }
        #endregion end DoInitialViewCenteringOnClientOrHostIfNeeded

        public override void AllegianceHelper_SetDefaultStartingFactionRelationships( Faction faction )
        {
            AllegianceHelper.SetDefaultStartingFactionRelationships( faction );
        }
    }
}
