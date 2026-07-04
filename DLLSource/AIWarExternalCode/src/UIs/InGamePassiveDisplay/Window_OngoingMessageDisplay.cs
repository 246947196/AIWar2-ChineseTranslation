using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_OngoingMessageDisplay : WindowControllerAbstractBase
    {
        public static Window_OngoingMessageDisplay Instance = new Window_OngoingMessageDisplay();
        public Window_OngoingMessageDisplay()
        {
            //this.OnlyShowInGame = true;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true;
            Instance = this;
        }

        public class tText : TextAbstractBase
        {
            public override bool GetShouldBeHidden()
            {
                if ( InputCaching.CalculateShouldTooltipsBeSuppressed() )
                    return true;
                return string.IsNullOrEmpty(updateBufferResult);
            }

            public override bool GetShouldRunUpdatesEvenWhenHidden()
            {
                return true;
            }

            #region HandleHyperlinkClick
            public override MouseHandlingResult HandleHyperlinkClick( MouseHandlingInput Input, string LinkIDString )
            {
                if ( LinkIDString == string.Empty )
                    return MouseHandlingResult.None;
                int linkID = Convert.ToInt32( LinkIDString );

                List<MomentaryChatItem> logEntries = Engine_Universal.LocalMomentaryDisplayLog;
                int currentCount = logEntries.Count;
                for ( int i = currentCount - 1; i >= 0; i-- )
                {
                    MomentaryChatItem entry = logEntries[i];
                    if ( entry.UniqueID == linkID )
                    {
                        if ( entry.ChatClickHandlerOrNull == null )
                        {
                            //open the chat log for regular chat messages
                            if ( !Window_ChatboxWindow.Instance.IsOpen )
                                Window_ChatboxWindow.Instance.Open();
                        }
                        else
                            entry.ChatClickHandlerOrNull.DoOnClick( Input );
                        return MouseHandlingResult.None;
                    }
                }
                return MouseHandlingResult.None;
            }
            #endregion

            #region HandleHyperlinkHover
            private static readonly ArcenDoubleCharacterBuffer linkTooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_OngoingMessageDisplay-tText-linkTooltipBuffer" );

            public override void HandleHyperlinkHover( string LinkIDString )
            {
                if ( LinkIDString == string.Empty )
                    return;
                int linkID = Convert.ToInt32( LinkIDString );

                List<MomentaryChatItem> logEntries = Engine_Universal.LocalMomentaryDisplayLog;
                int currentCount = logEntries.Count;
                for ( int i = currentCount - 1; i >= 0; i-- )
                {
                    MomentaryChatItem entry = logEntries[i];
                    if ( entry.UniqueID == linkID )
                    {
                        if ( entry.ChatClickHandlerOrNull == null )
                        {
                            Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( Element, "点击打开消息日志。" );
                        }
                        else
                        {
                            entry.ChatClickHandlerOrNull.DoOnTooltip( linkTooltipBuffer );
                            Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( Element, linkTooltipBuffer.GetStringAndResetForNextUpdate() );
                        }
                        return;
                    }
                }
            }
            #endregion

            private Int64 lastDisplayedErrorCount = 0;
            private DateTime lastUpdateErrorTime = DateTime.Now;

            private static readonly ArcenDoubleCharacterBuffer updateBuffer = new ArcenDoubleCharacterBuffer( "Window_OngoingMessageDisplay-tText-updateBuffer" );
            private string updateBufferResult = string.Empty;

            //Throttle text rebuild to ~3 Hz. Without this, OnUpdate ran every frame; the
            //buffer content often included continuously-changing values (mapgen elapsed
            //time, log messages with timestamps, etc.), which busted the
            //ArcenUI_Text.lastTextForSizing identity-check and forced
            //TMP_Text.GetPreferredValues to recompute — profiler caught that path at
            //~125 KB / frame via TMP_TextInfo.Resize. 0.3s matches the same throttle the
            //rest of the codebase uses via SetSkipGetTextFor and is fast enough that
            //user-visible latency (tutorial-step messages etc.) is imperceptible.
            private float lastBufferRebuildTime = 0;

            public override void OnUpdate()
            {
                //this.Element.Window.MaxDeltaTimeBeforeUpdates = 0;
                ArcenUI_Text myText = (this.Element as ArcenUI_Text);
                myText.SupportsHyperlinks = true;

                //this.WindowController.myYPositionScale = GameSettings.Current.GetFloatBySetting( "ResourceBarScale" );
                this.WindowController.myScale = GameSettings.Current.GetFloatBySetting( "GeneralTooltipScale" );

                Instance.ExtraOffsetY = Window_NotificationsDisplay.CurrentRowOffsetInView;

                //Bail out before rebuilding the buffer if we did so very recently.
                //Everything above is per-frame setup that must keep running; the heavy
                //text-building below is what we're throttling.
                if ( ArcenTime.TimeSinceStartF - lastBufferRebuildTime < 0.3f )
                    return;
                lastBufferRebuildTime = ArcenTime.TimeSinceStartF;

                updateBuffer.EnsureResetForNextUpdate();
                if ( World_AIW2.Instance.IsOutsideOfNormalGameplay )
                {
                    if ( World_AIW2.Instance.InSetupPhase )
                    {
                        bool wroteTutorialText = false;
                        if ( Mapgen.IsMapCurrentlyGenerating )
                        {
                            updateBuffer.Add( FontSizes.SLIGHTLY_SMALLER_SIZE_STRING ).Add( "<color=#fecd49><b>正在生成地图 " );
                            updateBuffer.AddFixedDecimal( ( ArcenTime.TimeSinceStartF - Mapgen.LastMapgenStartTime ), 1 );
                            updateBuffer.Add( "秒...</b></color>\n" );
                        }
                        //if ( !World.Instance.IsLoaded )
                        //{
                        //    updateBuffer.Add( FontSizes.SLIGHTLY_SMALLER_SIZE_STRING ).Add( "<color=#fe6f49><b>Loading...</b></color>\n" );
                        //}
                        if ( !Mapgen.IsMapCurrentlyGenerating )
                            EndpointFunctions.GetHasAnyErrorsPreventingStart( true, updateBuffer );

                        bool wroteAnything = false;
                        int debugStageOngoingOnly = 0;
                        try
                        {
                            Helper_WriteMomentaryLogMessages( ref debugStageOngoingOnly, ref wroteTutorialText, ref wroteAnything );
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "Setup-Only Ongoing message error at stage " + debugStageOngoingOnly + ": " + e, Verbosity.ShowAsError );
                        }
                        
                        updateBufferResult = updateBuffer.GetStringAndResetForNextUpdate();

                        return;
                    }
                    else //not in the setup phase
                    {
                        updateBufferResult = string.Empty;
                        return;
                    }
                }

                int debugStage = 1;
                try
                {
                    debugStage = 100;
                    bool wroteTutorialText = false;
                    IScenarioImplementation scenarioImp = World_AIW2.Instance.GetScenarioImplementationSafe_OrNull();
                    if ( scenarioImp != null )                        
                        wroteTutorialText = scenarioImp.WriteCurrentOngoingMessageToDisplay( updateBuffer );

                    bool wroteAnything = false;
                    Helper_WriteMomentaryLogMessages( ref debugStage, ref wroteTutorialText, ref wroteAnything );

                    if ( !Engine_Universal.HasFocus )
                    {
                        if ( !updateBuffer.GetIsEmpty() )
                            updateBuffer.Add( "\n" );
                        updateBuffer.Add( "<color=#ff5c1c><b>游戏未处于焦点状态</b>\n<size=60%>键盘和鼠标输入已被忽略。</size></color>" );
                    }

                    //for ( int i = 0; i < EnumInput.Lookup.Length; i++ )
                    //{
                    //    InputControlState controlState = InputManager.mState.Controls[i];
                    //}

                    debugStage = 4000;
                    bool isFirst = true;
                    //All the various kinds of slow threads possible
                    ArcenClientOrHostSimPlanningContext soleContext = ArcenExecutionManager.SoleContext;
                    if ( soleContext != null && World_AIW2.Instance.GameSecond >= 5 )
                    {
                        Helper_WriteAnyContextLongRunningWarning( soleContext, ref wroteAnything, ref isFirst );
                    }
                    for ( int contextIndex = 0; contextIndex < ArcenShortTermPlanningManager.AllContexts.Count; contextIndex++ )
                    {
                        if ( World_AIW2.Instance.GameSecond < 5 )
                            break; //if we just entered from the lobby, ignore all this

                        ArcenShortTermPlanningContext context = ArcenShortTermPlanningManager.AllContexts[contextIndex];
                        Helper_WriteAnyContextLongRunningWarning( context, ref wroteAnything, ref isFirst );
                    }
                    for ( int contextIndex = 0; contextIndex < ArcenVariousLongTermContextManager.AllContexts_ClientOrHost.Count; contextIndex++ )
                    {
                        if ( World_AIW2.Instance.GameSecond < 5 )
                            break; //if we just entered from the lobby, ignore all this

                        ArcenLongTermContinuousPlanningClientOrHostContext context = ArcenVariousLongTermContextManager.AllContexts_ClientOrHost[contextIndex];
                        Helper_WriteAnyContextLongRunningWarning( context, ref wroteAnything, ref isFirst );
                    }
                    for ( int contextIndex = 0; contextIndex < ArcenVariousLongTermContextManager.AllContexts_HostContinuous.Count; contextIndex++ )
                    {
                        if ( World_AIW2.Instance.GameSecond < 5 )
                            break; //if we just entered from the lobby, ignore all this

                        IContextForMonitoring context = ArcenVariousLongTermContextManager.AllContexts_HostContinuous[contextIndex];
                        Helper_WriteAnyContextLongRunningWarning( context, ref wroteAnything, ref isFirst );
                    }
                    for ( int contextIndex = 0; contextIndex < World_AIW2.Instance.Factions.Count; contextIndex++ )
                    {
                        if ( World_AIW2.Instance.GameSecond < 5 )
                            break; //if we just entered from the lobby, ignore all this

                        Faction fac = World_AIW2.Instance.Factions[contextIndex];
                        if ( fac == null )
                            continue;
                        Helper_WriteAnyContextLongRunningWarning( fac.LongRangePlanningContext, ref wroteAnything, ref isFirst );
                    }

                    isFirst = true;
                    //All the various kinds of inactive threads possible
                    if ( soleContext != null && World_AIW2.Instance.GameSecond >= 5 )
                    {
                        Helper_WriteAnyContextNotRunningWarning( soleContext, ref wroteAnything, ref isFirst );
                    }
                    for ( int contextIndex = 0; contextIndex < ArcenShortTermPlanningManager.AllContexts.Count; contextIndex++ )
                    {
                        if ( World_AIW2.Instance.GameSecond < 5 )
                            break; //if we just entered from the lobby, ignore all this

                        ArcenShortTermPlanningContext context = ArcenShortTermPlanningManager.AllContexts[contextIndex];
                        Helper_WriteAnyContextNotRunningWarning( context, ref wroteAnything, ref isFirst );
                    }
                    for ( int contextIndex = 0; contextIndex < ArcenVariousLongTermContextManager.AllContexts_ClientOrHost.Count; contextIndex++ )
                    {
                        if ( World_AIW2.Instance.GameSecond < 5 )
                            break; //if we just entered from the lobby, ignore all this

                        ArcenLongTermContinuousPlanningClientOrHostContext context = ArcenVariousLongTermContextManager.AllContexts_ClientOrHost[contextIndex];
                        Helper_WriteAnyContextNotRunningWarning( context, ref wroteAnything, ref isFirst );
                    }
                    for ( int contextIndex = 0; contextIndex < ArcenVariousLongTermContextManager.AllContexts_HostContinuous.Count; contextIndex++ )
                    {
                        if ( World_AIW2.Instance.GameSecond < 5 )
                            break; //if we just entered from the lobby, ignore all this

                        IContextForMonitoring context = ArcenVariousLongTermContextManager.AllContexts_HostContinuous[contextIndex];
                        Helper_WriteAnyContextNotRunningWarning( context, ref wroteAnything, ref isFirst );
                    }
                    for ( int contextIndex = 0; contextIndex < World_AIW2.Instance.Factions.Count; contextIndex++ )
                    {
                        if ( World_AIW2.Instance.GameSecond < 5 )
                            break; //if we just entered from the lobby, ignore all this

                        Faction fac = World_AIW2.Instance.Factions[contextIndex];
                        if ( fac == null )
                            continue;
                        Helper_WriteAnyContextNotRunningWarning( fac.LongRangePlanningContext, ref wroteAnything, ref isFirst );
                    }

                    isFirst = true;
                    for ( int contextIndex = 0; contextIndex < World_AIW2.Instance.Factions.Count; contextIndex++ )
                    {
                        if ( World_AIW2.Instance.GameSecond < 5 )
                            break; //if we just entered from the lobby, ignore all this
                        if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                            break; //clients don't run these threads.

                        debugStage = 4100;
                        Faction fac = World_AIW2.Instance.Factions[contextIndex];
                        debugStage = 4200;
                        if ( fac == null )
                            continue; // if this faction is null, that's fine
                        debugStage = 4300;
                        debugStage = 4500;
                        if ( ArcenTime.NonSetupNetworkReadyGameTimeSinceLastLoadOrStartF - fac.LastDoLongRangePlanningEndedAtUnpausedTimeSinceLastRestart_NonSim < 30f ) //if it's been less than 30 seconds since the game last ran this thread, don't complain
                            continue;
                        debugStage = 4600;
                        if ( wroteAnything )
                        {
                            debugStage = 4700;
                            updateBuffer.Add( "\n\n" );
                            wroteAnything = false;
                        }
                        if ( isFirst )
                        {
                            isFirst = false;
                            if ( !updateBuffer.GetIsEmpty() )
                                updateBuffer.Add( "\n" );
                            updateBuffer.Add( FontSizes.SLIGHTLY_SMALLER_SIZE_STRING ).Add( "<color=#f87433><b>未活跃的阵营线程：</b></color>" );
                        }
                        if ( !updateBuffer.GetIsEmpty() )
                            updateBuffer.Add( "\n" );
                        updateBuffer.Add( fac.GetDisplayName() ).Add( "：未运行 " ).AddFixedDecimal( (ArcenTime.NonSetupNetworkReadyGameTimeSinceLastLoadOrStartF - fac.LastDoLongRangePlanningEndedAtUnpausedTimeSinceLastRestart_NonSim), 1 ).Add( "秒" );
                    }

                    bool hadAnyOverCap = false;
                    //now tell me if any NPC factions are over ship cap!
                    foreach ( Faction fac in World_AIW2.Instance.Factions )
                    {
                        switch ( fac.Type )
                        {
                            case FactionType.Player:
                            case FactionType.NaturalObject:
                                continue; //skip these two
                        }
                        foreach ( NPCShipCapType row in NPCShipCapTypeTable.Instance.Rows )
                        {
                            if ( !row.VisiblyShowWhenOverCap )
                                continue; //skip certain types of caps

                            int currentCount = fac.NPCShipCountsByCapType == null ? 0 : fac.NPCShipCountsByCapType[row.RowIndexNonSim];
                            int cap = fac.SpecialFactionData.NPCShipCapsByType[row.RowIndexNonSim];
                            if ( currentCount > cap + (cap/2 ) ) //make sure it is a fair bit over before we report it here
                            {
                                updateBuffer.StartColor( "ff814a" );
                                updateBuffer.Add( "\n<size=80%>阵营超出上限：" ).StartColor( fac.FactionCenterColor.ColorHexBrighter ).Add( fac.GetDisplayName() ).EndColor();
                                updateBuffer.Add( " " );
                                updateBuffer.Add( row.InternalName ).Add( ": " ).Add( currentCount ).Add( "/" ).Add( cap );
                                updateBuffer.Add( "</size>" );
                                updateBuffer.EndColor();
                                hadAnyOverCap = true;
                            }
                        }
                    }

                    if ( hadAnyOverCap )
                    {
                        updateBuffer.StartColor( "ff814a" );
                        updateBuffer.Add( "\n<size=60%>请将阵营超出上限的情况作为Bug报告，并附上存档，因为这可能会影响游戏性能。</size>" );
                        updateBuffer.EndColor();
                    }

                    if ( ArcenDebugging.ErrorSinceStart > 0 )
                    {
                        if ( ArcenDebugging.ErrorSinceStart != lastDisplayedErrorCount )
                        {
                            lastDisplayedErrorCount = ArcenDebugging.ErrorSinceStart;
                            lastUpdateErrorTime = DateTime.Now;
                        }
                        if ( ( DateTime.Now - lastUpdateErrorTime ).TotalSeconds < 5 )
                        {
                            if ( !updateBuffer.GetIsEmpty() )
                                updateBuffer.Add( "\n" );
                            updateBuffer.Add( FontSizes.SLIGHTLY_SMALLER_SIZE_STRING ).Add( "<color=#f86133><b>启动以来的错误：</b> " )
                                .AddNumberMoreReadable( lastDisplayedErrorCount ).Add( "</color>" );
                        }

                        int factionsWithFatalErrors = 0;
                        if ( World_AIW2.Instance != null )
                        {
                            List<Faction> factions = World_AIW2.Instance.Factions;
                            foreach ( Faction fac in factions )
                            {
                                if ( fac.GetHasHadSomeException() )
                                    factionsWithFatalErrors++;
                            }
                        }
                        if ( factionsWithFatalErrors > 0 )
                        {
                            if ( !updateBuffer.GetIsEmpty() )
                                updateBuffer.Add( "\n" );
                            updateBuffer.Add( FontSizes.SLIGHTLY_SMALLER_SIZE_STRING ).Add( "<color=#f84633><b>因致命错误而关闭的阵营：</b> " )
                                .AddNumberMoreReadable( factionsWithFatalErrors ).Add( "</color>" );
                        }
                    }
                    if ( World_AIW2.Instance.IsSimulationArtificiallyStopped )
                    {
                        if ( !updateBuffer.GetIsEmpty() )
                            updateBuffer.Add( "\n" );
                        updateBuffer.Add( FontSizes.SLIGHTLY_SMALLER_SIZE_STRING ).Add( "<color=#f84633><b>游戏模拟已暂停！</b></color> " );
                    }
                    switch ( ArcenNetworkAuthority.DesiredStatus )
                    {
                        case DesiredMultiplayerStatus.Client:
                            if ( GameSettings.Current.GetBoolBySetting( "Debug_ShowNetworkStatsInUpperRight" ) )
                            {
                                updateBuffer.Add( "\n<size=80%>" ).AddFixedDecimalThousands( ArcenNetworkAuthority.AverageMillisecondHeartbeatRoundTripsOnClient, 1 )
                                            .Add( "ms 到主机的延迟 " )
                                            .Add( World_AIW2.Instance.Network_AuthorizedToExecuteThroughFrameNumber - World_AIW2.Instance.Network_CurrentFrameNumber )
                                            .Add( " 帧已授权 " );
                                updateBuffer.Add( "</size>" );
                            }
                            if ( World_AIW2.Instance.Network_ServerCurrentFrameNumber > World_AIW2.Instance.Network_CurrentFrameNumber + 4 ) //if we KNOW that we are more than 400ms behind.
                            {
                                int serverFramesAhead = World_AIW2.Instance.Network_ServerCurrentFrameNumber - World_AIW2.Instance.Network_CurrentFrameNumber;
                                float timeAhead = ((float)serverFramesAhead / 10f);
                                if ( !updateBuffer.GetIsEmpty() )
                                    updateBuffer.Add( "\n" );
                                updateBuffer.Add( "<size=90%>" ).AddFixedDecimalThousands( timeAhead, 1 ).Add( "秒 落后于主机模拟</size> " );
                            }
                            break;
                        case DesiredMultiplayerStatus.Host:
                            {
                                if ( GameSettings.Current.GetBoolBySetting( "Debug_ShowNetworkStatsInUpperRight" ) )
                                {
                                    foreach ( ArcenNetworkClientConnection conn in ArcenNetworkAuthority.AllClientConnections() )
                                    {
                                        updateBuffer.Add( "\n<size=80%>" ).Add( conn.ProfileName ).Add( " - " )
                                            .AddFixedDecimalThousands( conn.AverageMillisecondHeartbeatRoundTripsToThisConnectionFromHost, 1 )
                                            .Add( "ms 延迟 " )
                                            .Add( World_AIW2.Instance.Network_CurrentFrameNumber - conn.Network_HasFinishedThroughFrame )
                                            .Add( " 帧落后 " );
                                        updateBuffer.Add( "</size>" );
                                    }
                                }
                                int furthestBackClient = World_AIW2.Instance.Network_CurrentFrameNumber;
                                string slowestClient = string.Empty;
                                foreach ( ArcenNetworkClientConnection conn in ArcenNetworkAuthority.AllClientConnections() )
                                {
                                    if ( conn.Network_HasFinishedThroughFrame < furthestBackClient )
                                    {
                                        furthestBackClient = conn.Network_HasFinishedThroughFrame;
                                        slowestClient = conn.ProfileName;
                                    }
                                }
                                if ( World_AIW2.Instance.Network_CurrentFrameNumber > furthestBackClient + 10 ) //if more than 1 second ahead!
                                {
                                    int serverFramesAhead = World_AIW2.Instance.Network_CurrentFrameNumber - furthestBackClient;
                                    float timeAhead = ((float)serverFramesAhead / 10f);
                                    if ( !updateBuffer.GetIsEmpty() )
                                        updateBuffer.Add( "\n" );
                                    updateBuffer.Add( "<size=90%>" ).AddFixedDecimalThousands( timeAhead, 1 ).Add( "秒 领先于 " ).Add( slowestClient );
                                    updateBuffer.Add( "</size>" );
                                }
                                //version 2 for testing:
                                {
                                    int minFrameMustBeAt = World_AIW2.Instance.Network_CurrentFrameNumber - World_AIW2.Instance.SimulationProfile.MaxNumberOfFramesServerIsAllowedToGetAheadOfClient;
                                    bool isFirstWaiter = true;
                                    for ( int i = 0; i < World.Instance.AllPlayerAccounts.Count; i++ )
                                    {
                                        PlayerAccount acc = World.Instance.AllPlayerAccounts[i];
                                        if ( acc.Network_IsHost )
                                            continue; //the host can't get ahead of itself
                                        if ( !acc.OnServer_GetIsConnected() )
                                            continue; //don't worry about ones not conected

                                        //this one is lagging behind!
                                        if ( acc.Network_HasFinishedThroughFrame < minFrameMustBeAt )
                                        {
                                            if ( isFirstWaiter )
                                            {
                                                if ( !updateBuffer.GetIsEmpty() )
                                                    updateBuffer.Add( "\n" );
                                                updateBuffer.Add( "<size=90%>" ).Add( "等待：" );
                                                isFirstWaiter = false;
                                            }
                                            else
                                                updateBuffer.Add( ", " );
                                            updateBuffer.Add( acc.Username );
                                        }
                                    }
                                    if ( !isFirstWaiter )
                                        updateBuffer.Add( "</size>" );
                                }
                            }
                            break;
                    }

                    if ( GameSettings.Current.GetBoolBySetting( "Debug_ShowGameCommandStatsInUpperRight" ) )
                    {
                        updateBuffer.Add( "\n<size=80%>所有命令：" ).AddNumberMoreReadable( World_AIW2.Instance.OnClient_GameCommandsThatHaveBeenReceivedFromServerButNotYetExecuted.Count )
                                    .Add( " 等待中   " )
                                    .AddNumberMoreReadable( World_AIW2.Instance.OnClient_GameCommandsThatHaveNotYetBeenSentToServer.Count )
                                    .Add( " 已排队   " )
                                    .AddNumberMoreReadable( World_AIW2.Instance.OnClient_CommandsExecuted )
                                    .Add( " 已执行" )
                                    .Add( "\n我的命令：" )
                                    .AddNumberMoreReadable( World_AIW2.Instance.OnClient_LocalPlayerCommandsExecuted )
                                    .Add( " 已执行   " )
                                    .AddNumberMoreReadable( World_AIW2.Instance.OnClient_LocalPlayerCommandsQueued )
                                    .Add( " 已排队   " )
                                    .AddNumberMoreReadable( World_AIW2.Instance.OnClient_LocalPlayerCommandsConsidered )
                                    .Add( " 已考虑 " )
                                    .Add( "\n+" )
                                    .AddNumberMoreReadable( World_AIW2.Instance.OnClient_MaxNumberOfFramesAheadForAnyExecution )
                                    .Add( " 最大帧数" );
                        updateBuffer.Add( "</size>" );
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Ongoing message error at stage " + debugStage + ": " + e, Verbosity.ShowAsError );
                }

                updateBufferResult = updateBuffer.GetStringAndResetForNextUpdate();
            }

            #region Helper_WriteMomentaryLogMessages
            private void Helper_WriteMomentaryLogMessages( ref int debugStage, ref bool wroteTutorialText, ref bool wroteAnything )
            {
                debugStage = 300;
                List<MomentaryChatItem> logEntries = Engine_Universal.LocalMomentaryDisplayLog;
                debugStage = 310;
                int currentCount = logEntries.Count;
                for ( int i = currentCount - 1; i >= 0; i-- )
                {
                    debugStage = 320;
                    MomentaryChatItem entry = logEntries[i];
                    if ( entry.Text == null || entry.Text.Length == 0 )
                    {
                        debugStage = 330;
                        logEntries.RemoveAt( i );
                        continue;
                    }
                    debugStage = 34;
                    if ( entry.TimeToExpire > ArcenTime.TimeSinceStartF )
                        continue;
                    debugStage = 350;
                    //this is an older one, stop showing it
                    logEntries.RemoveAt( i );
                }

                //now display everything that is left, since it should all be okay
                debugStage = 400;
                currentCount = logEntries.Count;
                wroteAnything = wroteTutorialText;
                for ( int i = currentCount - 1; i >= 0; i-- )
                {
                    debugStage = 500;
                    MomentaryChatItem entry = logEntries[i];
                    debugStage = 650;
                    if ( entry.Text == null || entry.Text.Length == 0 )
                        continue;
                    debugStage = 700;
                    if ( wroteTutorialText )
                    {
                        debugStage = 800;
                        updateBuffer.Add( "\n\n" ).Add( FontSizes.SLIGHTLY_SMALLER_SIZE_STRING ).Add( "<color=#bebebe><b>其他备注：</b></color>" );
                        wroteTutorialText = false;
                    }
                    debugStage = 900;
                    if ( !updateBuffer.GetIsEmpty() )
                        updateBuffer.Add( "\n" );
                    debugStage = 1000;
                    updateBuffer.Add( "<link=" ).Add( entry.UniqueID );
                    updateBuffer.Add( ">" );
                    updateBuffer.Add( entry.Text );
                    updateBuffer.Add( "</link>" );
                    wroteAnything = true;
                }
            }
            #endregion

            #region Helper_WriteAnyContextLongRunningWarning
            private void Helper_WriteAnyContextLongRunningWarning( IContextForMonitoring context, ref bool wroteAnything, ref bool isFirst )
            {
                if ( context == null )
                    return; //should not happen, but whatever
                if ( context.GetIsWorkDone() )
                    return; //we must be running right now!
                int elapsedMs = context.GetCurrentElapsedMiliseconds();
                if ( elapsedMs <= 0 )
                    return;
                if ( elapsedMs < context.GetMillisecondsAfterWhichToWarn_OfLongRunning() )
                    return;
                if ( wroteAnything )
                {
                    updateBuffer.Add( "\n\n" );
                    wroteAnything = false;
                }
                if ( isFirst )
                {
                    isFirst = false;
                    if ( !updateBuffer.GetIsEmpty() )
                        updateBuffer.Add( "\n" );
                    updateBuffer.Add( FontSizes.SLIGHTLY_SMALLER_SIZE_STRING ).Add( "<color=#f87433><b>缓慢的后台线程：</b></color>" );
                }
                if ( !updateBuffer.GetIsEmpty() )
                    updateBuffer.Add( "\n" );
                updateBuffer.Add( context.NameForDisplay ).Add( ": " ).AddFixedDecimal( (elapsedMs / 1000f), 1 ).Add( "s" );
            }
            #endregion

            #region Helper_WriteAnyContextNotRunningWarning
            private void Helper_WriteAnyContextNotRunningWarning( IContextForMonitoring context, ref bool wroteAnything, ref bool isFirst )
            {
                if ( context == null )
                    return; //should not happen, but whatever
                if ( context.GetIsWorkDone() )
                    return; //if we are running right now, then cool!

                float timeAfterWarning = context.GetTimeAfterWhichToWarn_OfNotRunning();
                if ( timeAfterWarning >= 99999 )
                    return; //if there is such a gap that we never complain, then also cool!

                float lastRunEffective = context.GetLastNonSetupGameTimeSinceLastLoadOrStartFinishedProperlyRunning_Effective();
                if ( ArcenTime.NonSetupNetworkReadyGameTimeSinceLastLoadOrStartF - lastRunEffective < timeAfterWarning )
                    return;

                if ( wroteAnything )
                {
                    updateBuffer.Add( "\n\n" );
                    wroteAnything = false;
                }
                if ( isFirst )
                {
                    isFirst = false;
                    if ( !updateBuffer.GetIsEmpty() )
                        updateBuffer.Add( "\n" );
                    updateBuffer.Add( FontSizes.SLIGHTLY_SMALLER_SIZE_STRING ).Add( "<color=#f87433><b>未活跃的后台线程：</b></color>" );
                }
                if ( !updateBuffer.GetIsEmpty() )
                    updateBuffer.Add( "\n" );
                updateBuffer.Add( context.NameForDisplay ).Add( "：未运行 " ).AddFixedDecimal( (ArcenTime.NonSetupNetworkReadyGameTimeSinceLastLoadOrStartF - lastRunEffective), 1 ).Add( "秒" );
            }
            #endregion

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( string.IsNullOrEmpty(updateBufferResult) == false )
                    buffer.Add( updateBufferResult );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( World_AIW2.Instance.TutorialOrNull != null )
                {
                    //if in the tutorial, advance to the next tutorial step
                    if ( World_AIW2.Instance.TutorialStageIndex < World_AIW2.Instance.TutorialOrNull.Steps.Count )
                    {
                        Tutorial.TutorialStep step = World_AIW2.Instance.TutorialOrNull.Steps[World_AIW2.Instance.TutorialStageIndex];

                        bool areAllConditionsComplete = true;
                        for ( int i = 0; i < step.Conditions.Count; i++ )
                        {
                            if ( !step.Conditions[i].HasConditionBeenMet() )
                            {
                                areAllConditionsComplete = false;
                                break;
                            }
                        }
                        if ( areAllConditionsComplete )
                        {
                            World_AIW2.Instance.TutorialStageIndex++;
                            if ( World_AIW2.Instance.TutorialStageIndex < World_AIW2.Instance.TutorialOrNull.Steps.Count )
                                World_AIW2.Instance.TutorialOrNull.Steps[World_AIW2.Instance.TutorialStageIndex].ResetAnythingForThisNewStep();
                        }
                    }
                    else //completed the tutorial!
                    {
                        StackMenuWindowController.CloseEntireStack( true );
                        //quitting to the main menu
                        ArcenThreading.InitiateThreadingBlockForShutdown( NextThreadingBlockPhase.QuitToMainMenu );
                        Window_LoadTutorialsMenu.Instance.Open();
                    }
                }
                return base.HandleClick_Subclass( input );
            }
        }
    }
}
