using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;
using System.Diagnostics;

namespace Arcen.AIW2.External
{
    public class Window_InGameEscapeMenu : StackMenuWindowController
    {
        public static Window_InGameEscapeMenu Instance;
        public Window_InGameEscapeMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
        }
        public override string GetBriefName() { return "系统"; }

        public static bool WasPaused;
        public static bool DidPause = false;

        public static void HandleOpeningAMenuThatMightPause()
        {
            //This pauses the game when the escape menu is opened, if the 
            WasPaused = World.Instance.IsPaused;
            DidPause = false;
            if ( !WasPaused && GameSettings.Current.GetBoolBySetting( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.SinglePlayerOnly ? "PauseGameEnteringEscMenu_SP" : "PauseGameEnteringEscMenu_MP" ) )
            {
                DidPause = true;
                if ( !Engine_Universal.IsShuttingDown )
                {
                    try
                    {
                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.PauseOnly], GameCommandSource.AnythingElse );
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                    }
                    catch { } //generally happens when exiting the game
                }
            }
        }

        public static void HandleClosingAMenuThatMightPause()
        {
            if ( DidPause && World.Instance.IsPaused && World_AIW2.Instance.GameSecond > 0 )
            {
                if ( !Engine_Universal.IsShuttingDown )
                {
                    try
                    {
                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.UnpauseOnly], GameCommandSource.AnythingElse );
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                    }
                    catch { } //generally happens when exiting the game
                }
            }
            DidPause = false;
        }

        public override void OnOpen()
        {
            HandleOpeningAMenuThatMightPause();
        }

        public override void OnClose()
        {
            HandleClosingAMenuThatMightPause();
        }

        public class customParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.TimeSinceStartF;
            }
            
            public override void OnUpdate()
            {
                
            }            
        }
        
        public class bResume : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_InGameEscapeMenu.Instance.Close();
                return MouseHandlingResult.None;
            }
        }

        public class bUnitEncyclopedia : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_UnitEncyclopedia.Instance.Open();
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element,
                    "一个通用参考手册，你可以在此找到游戏中每个单位的信息。任何已启用的模组也会自动显示在这里。" );
            }
            public override void OnUpdate() { }
        }

        public class bHowToPlay : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_Tips.Instance.Open();
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element,
                    "从基础主题到详细策略的文字说明。" );
            }
        }

        public class bSettings : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_SettingsMenu.Instance.Open();
                return MouseHandlingResult.None;
            }
        }

        public class bGalaxyOptions : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_InGameGalaxyOptions.Instance.Open();
                return MouseHandlingResult.None;
            }
        }

        public class bControls : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_ControlBindingsMenu.Instance.Open();
                return MouseHandlingResult.None;
            }
        }

        public class bSaveGame : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( !ArcenNetworkAuthority.GetIsClientMode() && ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    Buffer.Add( "保存游戏" );
                else
                    Buffer.StartColor( ColorMath.DarkGray ).Add( "客户端无法保存游戏" );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( !ArcenNetworkAuthority.GetIsClientMode() && ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    Window_SaveGameMenu.Instance.Open();
                else
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "多人游戏客户端无法保存游戏",
                    "很遗憾，在多人游戏中只有主机才能保存游戏。为了节省带宽和机器间的处理能力，有大量数据仅在主机上计算。如果你在客户端保存了游戏副本，加载时将导致大量错误和缺失数据。",
                    "确定" );

                return MouseHandlingResult.None;
            }
        }

        public class bDebugMenu : StackWindowOpeningButtonController
        {
            protected override StackMenuWindowController RelatedController { get { return Window_InGameDeveloperToolsMenu.Instance; } }
        }

        public class bEditFactions : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_FactionsWindow.Instance.Open();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, 
                    "允许你调整阵营昵称、查看阵营设置，在多人游戏中甚至可以在玩家之间赠送某些资源。按<color=#4486d1>" +
                    InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "OpenFactionWindowWithoutPausing" ) + "</color>可以轻松打开阵营窗口而无需暂停或进入此菜单。" );
            }
        }
        public class bDiscord : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Process.Start( "https://discord.com/invite/5g9ETKn" );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element,
                    "Discord已成为本游戏社区的事实聚集地，也是分享或寻求技巧、胜利、磨难等的好地方。有时候你遇到一个看似不可能获胜的特别困难的存档，其他人会有兴趣尝试看看能否找到通关方法。我们的旧论坛虽然还在，但大多已落满灰尘，属于过时的媒介了。" );
            }
        }

        public class bQuit : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                {
                    StackMenuWindowController.CloseEntireStack( true );
                    Engine_AIW2.Instance.LogNotesOnObjectCountsRightBeforeQuit();
                    //quitting to the main menu
                    ArcenThreading.InitiateThreadingBlockForShutdown( NextThreadingBlockPhase.QuitToMainMenu );
                    return MouseHandlingResult.None;
                }
                if ( World_AIW2.Instance.CalculateIsIronmanMode() )
                {
                    ModalPopupData.CreateAndLogYesNoStyle( delegate
                    {
                        SaveLoadMethods.TakeIronmanSave(true, false);
                    }, null, "你确定吗？", "在铁人模式下，退出游戏时会自动保存。", "是，退出", "不，返回" );

                    return MouseHandlingResult.None;
                }
                if ( GameSettings.Current.GetBoolBySetting( "TakeQuitSave" ) )
                {
                    //player has chosen to take a save when quitting
                    ModalPopupData.CreateAndLogYesNoStyle( delegate
                    {
                        InterfaceHelper.TakeQuitSave(true, false);
                    }, null, "你确定吗？", "退出游戏时会自动保存。", "是，退出", "不，返回" );

                    return MouseHandlingResult.None;
                }
                //normal quit path
                ModalPopupData.CreateAndLogYesNoStyle( delegate
                {
                    StackMenuWindowController.CloseEntireStack( true );
                    Engine_AIW2.Instance.LogNotesOnObjectCountsRightBeforeQuit();
                    //quitting to the main menu
                    ArcenThreading.InitiateThreadingBlockForShutdown( NextThreadingBlockPhase.QuitToMainMenu );
                }, null, "你确定吗？", "你已保存所有数据，准备退出到主菜单吗？", "是，退出", "不，返回" );

                return MouseHandlingResult.None;
            }

        }

        public class bExitToOS : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                {
                    Engine_Universal.IsShuttingDown = true;
                    StackMenuWindowController.CloseEntireStack( true );
                    Engine_AIW2.Instance.LogNotesOnObjectCountsRightBeforeQuit();
                    Engine_Universal.ForceClose( false );
                    return MouseHandlingResult.None;
                }
                if ( World_AIW2.Instance.CalculateIsIronmanMode() )
                {
                    ModalPopupData.CreateAndLogYesNoStyle( delegate
                    {
                        Engine_Universal.ForceSoundOff();
                        SaveLoadMethods.TakeIronmanSave(false, true);
                    }, null, "你确定吗？", "在铁人模式下，退出游戏时会自动保存。", "是，退出", "不，返回" );

                    return MouseHandlingResult.None;
                }
                if ( GameSettings.Current.GetBoolBySetting( "TakeQuitSave" ) )
                {
                    ModalPopupData.CreateAndLogYesNoStyle( delegate
                    {
                        Engine_Universal.ForceSoundOff();
                        InterfaceHelper.TakeQuitSave(true, true );
                    }, null, "你确定吗？", "退出游戏时会自动保存。", "是，退出", "不，返回" );

                    return MouseHandlingResult.None;
                }
                ModalPopupData.CreateAndLogYesNoStyle( delegate
                {
                    Engine_Universal.ForceSoundOff();
                    Engine_Universal.IsShuttingDown = true;
                    StackMenuWindowController.CloseEntireStack( true );
                    Engine_AIW2.Instance.LogNotesOnObjectCountsRightBeforeQuit();
                    Engine_Universal.ForceClose( false );
                }, null, "你确定吗？", "你已保存所有数据，准备退出到桌面吗？", "是，退出", "不，返回" );

                return MouseHandlingResult.None;
            }
        }

        public class tAboutContent : TextAbstractBase
        {
            //Section toggles — modeled on HotM's VisCommands.ShowPerformance(). Default ALL
            //sections to closed so opening the escape menu doesn't immediately allocate a
            //multi-thousand-character TMP string per frame (the profiler caught this at
            //~2.8 MB/frame via TMP_Text.GetPreferredValues → SetArraySizes — TMP keeps
            //per-character arrays sized to the text length). Click a section header to
            //toggle it open; only that section's content is generated on subsequent frames.
            //SetSkipGetTextFor(0.3f) below still throttles per-section refresh to ~3 Hz.
            private static bool RenderFactions = false;
            private static bool RenderMultiplayer = false;
            private static bool RenderCurrentPlanet = false;
            private static bool RenderGamePerf = false;
            private static bool RenderPools = false;

            public override MouseHandlingResult HandleHyperlinkClick( MouseHandlingInput Input, string LinkID )
            {
                switch ( LinkID )
                {
                    case "RenderFactions":      RenderFactions      = !RenderFactions;      break;
                    case "RenderMultiplayer":   RenderMultiplayer   = !RenderMultiplayer;   break;
                    case "RenderCurrentPlanet": RenderCurrentPlanet = !RenderCurrentPlanet; break;
                    case "RenderGamePerf":      RenderGamePerf      = !RenderGamePerf;      break;
                    case "RenderPools":         RenderPools         = !RenderPools;         break;
                    default: return MouseHandlingResult.None;
                }
                //Force the text element to refresh immediately on the next frame instead of
                //waiting up to 0.3s for the throttled re-render — toggle should feel snappy.
                this.SetSkipGetTextFor( 0f );
                return MouseHandlingResult.None;
            }

            private static void WriteSectionToggle( ArcenDoubleCharacterBuffer Buffer, string LinkID, string Label, bool IsOpen )
            {
                //TMP <link="ID">text</link> markup. ArcenUI_Text routes the click to
                //HandleHyperlinkClick(Input, LinkID) via the FindIntersectingLink path
                //already wired up in the framework. Colored to match open/closed state so
                //the user can see which sections are expanded at a glance.
                string color = IsOpen ? "9be1ff" : "8d8d8d";
                Buffer.Add( "<link=\"" ).Add( LinkID ).Add( "\"><color=#" ).Add( color ).Add( "><u>" );
                Buffer.Add( IsOpen ? "[-] " : "[+] " ).Add( Label );
                Buffer.Add( "</u></color></link>  " );
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( !Window_InGameEscapeMenu.Instance.GetIsOnStack() )
                    return;

                this.SetSkipGetTextFor( 0.3f );

                int debugStage = 0;
                try
                {
                    if ( World_AIW2.Instance == null ||
                        World_AIW2.Instance.Setup == null ||
                        World_AIW2.Instance.CurrentGalaxy == null )
                    {
                        Buffer.StartColor( QuickColors.HeaderBright ).Add( "战役数据错误\n" );
                        return;
                    }

                    Planet localPlanet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                    bool isTutorial = World_AIW2.Instance.TutorialOrNull != null;


                    if ( isTutorial )
                        Buffer.StartColor( QuickColors.HeaderBright ).Add( "<b>教程：</b> " ).EndColor().Add( World_AIW2.Instance.TutorialOrNull.DisplayName ).Add( "\n" );
                    else
                    {
                        Buffer.StartColor( QuickColors.HeaderBright ).Add( "<b>战役：</b> " ).EndColor().Add( World.Instance.CampaignName ).Add( "\n" );

                        if ( World_AIW2.Instance.CampaignType == null )
                            World_AIW2.Instance.CampaignType = CampaignTypeDataTable.EasiestNonSandboxType;

                        if ( Engine_AIW2.Instance.IsTestChamber )
                            Buffer.StartColor( QuickColors.HeaderBright ).Add( "<b>战役类型：</b> " ).EndColor().Add( "测试室\n" );
                        else
                        {
                            CampaignTypeData typeDataToSelect = World_AIW2.Instance.CampaignType;
                            if ( typeDataToSelect != null )
                            {
                                Buffer.StartColor( QuickColors.HeaderBright ).Add( "<b>战役类型：</b> " ).EndColor().Add( typeDataToSelect.DisplayName ).Add( "\n" );
                            }
                        }
                    }

                    Buffer.StartColor( QuickColors.HeaderMid ).Add( "<b>开始时间：</b> " ).EndColor().Add( World.Instance.CampaignStartTime.ToShortDateString() )
                        .Add( " " ).Add( World.Instance.CampaignStartTime.ToShortTimeString() ).Add( "，版本 " ).EndColor().Add( World.Instance.OriginalGameVersionOfCampaign ).Add( "\n" );

                    #region ExpansionsInUse
                    int expansionCount = World.Instance.GetCountOfExpansionsInUse();
                    if ( expansionCount > 0 )
                    {
                        Buffer.StartColor( QuickColors.HeaderMid ).Add( "<b>使用的扩展包：</b> " ).EndColor();
                        for ( int i = 0; i < expansionCount; i++ )
                        {
                            Expansion e = World.Instance.GetExpansionsInUseAtIndex( i );
                            if ( i > 0 )
                                Buffer.Add( ", " );
                            Buffer.Add( e.DisplayName, e.ColorForDisplay );
                        }
                        Buffer.Add( "\n" );
                    }
                    #endregion

                    #region XmlModsInUse
                    int modCount = World.Instance.GetCountOfXmlModsInUse();
                    if ( modCount > 0 )
                    {
                        Buffer.StartColor( QuickColors.HeaderMid ).Add( "<b>使用的模组：</b> " ).EndColor();
                        for ( int i = 0; i < modCount; i++ )
                        {
                            XmlMod mod = World.Instance.GetXmlModsInUseAtIndex( i );
                            if ( i > 0 )
                                Buffer.Add( ", " );
                            Buffer.Add( mod.DisplayName );
                        }
                        Buffer.Add( "\n\n" );
                    }
                    #endregion

                    if ( World.Instance.HaveDoneAnyCheatingThatBlocksAchievemets )
                    {
                        Buffer.StartColor( "de5817" ).Add( "<b>成就：</b> " ).EndColor();
                        Buffer.Add( "因沙盒模式或作弊而禁用", "ff7e00" );
                        Buffer.Add( "\n" );
                    }
                    else
                    {
                        Buffer.StartColor( "1783de" ).Add( "<b>成就：</b> " ).EndColor();
                        Buffer.Add( "已启用", "00a2ff" );
                        Buffer.Add( "\n" );
                    }
                    if ( World_AIW2.Instance.PlayersAreInDistributedResourceGenerationMode )
                    {
                        Buffer.StartColor( "37e26b" ).Add( "<b>经济模式：</b> " ).EndColor();
                        Buffer.Add( "分配到小行星矿反应堆中。", "6ffa99" );
                        Buffer.Add( "\n" );
                    }
                    else
                    {
                        Buffer.StartColor( "afe128" ).Add( "<b>经济模式：</b> " ).EndColor();
                        Buffer.Add( "集中在指挥站中。", "c4e569" );
                        Buffer.Add( "\n" );
                    }

                    if ( World_AIW2.Instance.CalculateIsIronmanMode() )
                    {
                        Buffer.StartColor( QuickColors.HeaderMid ).Add( "<b>铁人模式：</b> " ).EndColor();
                        Buffer.Add( "已启用", "ffbe00" );
                        Buffer.Add( "\n\n" );
                    }
                    Buffer.Add( "\n" );

                    {
                        Buffer.StartColor( QuickColors.HeaderMid ).Add( "<b>模拟速度：</b> " ).EndColor();
                        debugStage = 410;
                        double ratioInMilliseconds = World_AIW2.Instance.GetPerformanceRatio();
                        if ( ratioInMilliseconds > 0 )
                        {
                            double ratioInSpeed = 1f / ratioInMilliseconds;
                            Color ratioColor;
                            if ( ratioInSpeed < 0.7f )
                                ratioColor = ColorMath.LightRed;
                            else
                                ratioColor = ColorMath.LightGreen;
                            Buffer.Add( " <color=#" ).Add( ratioColor.GetHexCode() ).Add( ">" ).Add( (int)Math.Round( ratioInSpeed * 100 ) ).Add( "%</color>" );
                        }
                        if ( World.Instance.IsPaused )
                            Buffer.StartColor( QuickColors.HeaderBright ).Add( " - 已暂停" ).EndColor();
                        else
                            Buffer.StartColor( QuickColors.Danger ).Add( " - 运行中" ).EndColor();

                        Buffer.Add( " (" ).Add( Mathf.RoundToInt( ArcenFramerateTracker.CurrentFramesPerSecond ) ).Add( " FPS)" );

                        Buffer.Add( "\n" );
                    }

                    {
                        Buffer.StartColor( QuickColors.HeaderMid ).Add( "<b>内存：</b> " ).EndColor();
                        Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( GC.CollectionCount( 0 ) ).EndColor().Add( " GC调用     " );
                        Buffer.StartColor( QuickColors.HeaderMid ).AddFixedDecimalThousands( (GC.GetTotalMemory( false ) / 1024f / 1024f), 1 ).EndColor().Add( " MB GC内存" );
                        Buffer.Add( "\n" );
                    }

                    debugStage = 10;

                    if ( World_AIW2.Instance == null )
                    {
                        Buffer.StartColor( "Null world!" );
                        return;
                    }

                    //Section toggle bar — see field comments at top of class for rationale.
                    //Each link toggles its corresponding RenderX bool via HandleHyperlinkClick.
                    bool isMP = ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.SinglePlayerOnly;
                    Buffer.Add( "\n" );
                    if ( !isTutorial )
                        WriteSectionToggle( Buffer, "RenderFactions", "阵营", RenderFactions );
                    if ( isMP )
                        WriteSectionToggle( Buffer, "RenderMultiplayer", "多人游戏", RenderMultiplayer );
                    if ( localPlanet != null )
                        WriteSectionToggle( Buffer, "RenderCurrentPlanet", "当前星球", RenderCurrentPlanet );
                    Buffer.Add( "\n" );
                    WriteSectionToggle( Buffer, "RenderGamePerf", "性能", RenderGamePerf );
                    WriteSectionToggle( Buffer, "RenderPools", "内存", RenderPools );
                    Buffer.Add( "\n\n" );

                    debugStage = 100;
                    if ( !isTutorial )
                    {
                        if ( RenderFactions )
                        {
                            Buffer.StartColor( QuickColors.HeaderBright ).Add( "<b>阵营：</b>\n" ).EndColor();
                            InterfaceHelper.WriteFactionsToBuffer( Buffer );
                            Buffer.Add( "\n" );
                        }
                    }
                    else
                    {
                        int mostRecentTutorialStage = Math.Min( World_AIW2.Instance.TutorialOrNull.Steps.Count - 1, World_AIW2.Instance.TutorialStageIndex );
                        for ( int i = mostRecentTutorialStage; i >= 0; i-- )
                        {
                            Tutorial.TutorialStep step = World_AIW2.Instance.TutorialOrNull.Steps[i];
                            BaseScenario.WriteTutorialHeader( Buffer, i, World_AIW2.Instance.TutorialOrNull.Steps.Count, i < World_AIW2.Instance.TutorialStageIndex );
                            Buffer.Add( "\n" );
                            Buffer.Add( step.CalculateTextToShow() );
                            Buffer.Add( "\n" );
                        }
                    }

                    debugStage = 200;
                    if ( !isTutorial )
                    {
                        Buffer.StartColor( QuickColors.HeaderBright ).Add( "<b>地图：</b> " ).EndColor();
                        WorldSetup setup = World_AIW2.Instance.Setup;
                        if ( setup == null )
                            Buffer.Add( "NULL WORLD SETUP???" );
                        else if ( setup.MapConfig.UseRandomMapType )
                            Buffer.Add( "随机地图类型" );
                        else
                        {
                            MapTypeData mapType = setup.MapConfig.MapType;
                            Buffer.Add( mapType == null ? "MAPTYPE NOT FOUND" : mapType.DisplayName );
                        }
                    }

                    Buffer.Add( "\n<pos=20>" );
                    if ( !isTutorial )
                    {
                        Buffer.StartColor( QuickColors.HeaderMid ).Add( World_AIW2.Instance.Setup.MapConfig.Seed ).EndColor().Add( " 种子<pos=200>" );
                        Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.CurrentGalaxy.GetCountOfNonDestroyedPlanets() ).EndColor().Add( " 颗星球" );
                        Buffer.Add( "\n" );
                    }

                    debugStage = 350;

                    switch ( ArcenNetworkAuthority.DesiredStatus )
                    {
                        case DesiredMultiplayerStatus.SinglePlayerOnly:
                            Buffer.Add( "\n" ).StartColor( QuickColors.HeaderBright ).Add( "<b>当前模式：</b> " ).EndColor().Add( "单人游戏" );
                            break;
                        case DesiredMultiplayerStatus.Client:
                            Buffer.Add( "\n" ).StartColor( QuickColors.HeaderBright ).Add( "<b>当前模式：</b> " ).EndColor().Add( "多人游戏客户端" );
                            Buffer.Add( "\n" ).Add( "<b>网络框架：</b> " ).EndColor().Add( ArcenNetworkAuthority.ActiveSocket.DisplayName );
                            break;
                        case DesiredMultiplayerStatus.Host:
                            Buffer.Add( "\n" ).StartColor( QuickColors.HeaderBright ).Add( "<b>当前模式：</b> " ).EndColor().Add( "多人游戏主机" );
                            Buffer.Add( "\n" ).Add( "<b>网络框架：</b> " ).EndColor().Add( ArcenNetworkAuthority.ActiveSocket.DisplayName );
                            if ( ArcenNetworkAuthority.ActiveSocket.AreConnectionsByIPAddress() &&
                                !GameSettings.Current.GetBoolBySetting( "HideIPAddressInLobbyAndEscMenu" ) )
                            {
                                Buffer.Add( "\n<b>提供给客户端的公网IP：</b> " ).StartColor( "ff974b" ).Add( ArcenNetworkAuthority.GetMyPublicIPAddress() ).EndColor();
                                List<string> localIPs = ArcenNetworkAuthority.GetListOfLocalIPAddresses();
                                if ( localIPs.Count > 0 )
                                {
                                    Buffer.Add( "\n<b>或局域网内的本地IP选项：</b> " ).StartColor( "acff4b" );
                                    if ( localIPs.Count > 1 )
                                        Buffer.Add( "\n" );
                                    for ( int i = 0; i < localIPs.Count; i++ )
                                    {
                                        Buffer.Add( localIPs[i] ).Add( "\n" );
                                    }
                                    Buffer.EndColor();
                                }
                            }
                            break;
                    }
                    Buffer.Add( "\n" );

                    debugStage = 350;
                    if ( ArcenMusicPlayer.Instance.GetIsMusicPlaying() )
                    {
                        Buffer.Add( "\n" ).StartColor( QuickColors.HeaderMid ).Add( "当前曲目：" ).EndColor().Add( ArcenMusicPlayer.Instance.GetCurrentlyPlayingMusicTrack() );
                        if ( GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.AdaptiveMusic ) )
                        {
                            if ( World_AIW2.Instance.ExcitingGameState )
                                Buffer.Add( "激烈游戏状态", "ffa1a1" );
                            else
                                Buffer.Add( "平静游戏状态", "a1ffa1" );
                            MusicTrack track = ArcenMusicPlayer.Instance.GetCurrentlyPlayingTrack();
                            if ( track == null )
                                Buffer.Add( "<size=60%> 曲目为空</size>", "a1a1ff" );
                            else if ( track.GetHasTag("Exciting") && track.GetHasTag("Peaceful") )
                                Buffer.Add( "<size=60%> 曲目可变</size>", "a1a1ff" );
                            else if ( track.GetHasTag("Exciting") )
                                Buffer.Add( "<size=60%> 曲目为激烈风格</size>", "a1a1ff" );
                            else if ( track.GetHasTag("Peaceful") )
                                Buffer.Add( "<size=60%> 曲目为平静风格</size>", "a1a1ff" );
                            else
                                Buffer.Add( "<size=60%> 曲目风格未定", "a1a1ff" );
                        }
                        Buffer.Add( "\n\n" );
                    }

                    debugStage = 400;

                    #region Multiplayer Stats
                    if ( RenderMultiplayer && ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.SinglePlayerOnly )
                    {
                        Buffer.StartColor( QuickColors.HeaderBright ).Add( "<b>多人游戏统计：</b> " ).EndColor();
                        
                        if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Host )
                        {
                            var temp = AIWar2NetworkSync.OnHost_SquadNotSyncedForLongestTime_GameSecond;
                            //if (temp.Squad != null)
                            {
                                Buffer.Add( "\n" ).SetPositionOffset( 20 );
                                Buffer.Add("最久未同步小队 ", QuickColors.HeaderWarning).Add(World_AIW2.Instance.GameSecond - temp.Value).Add(" 秒 ").Add(temp.Squad.OrNull()).Add("");
                            }
                        }
                        else
                        {
                            var temp = AIWar2NetworkSync.OnClient_SquadNotSyncedForLongestTime_Cycles;
                            //if (temp.Squad != null)
                            {
                                Buffer.Add( "\n" ).SetPositionOffset( 20 );
                                Buffer.Add("最久未同步小队 ", QuickColors.HeaderWarning).Add(temp.Value).Add(" 周期 ").Add(temp.Squad.OrNull()).Add("");
                            }
                        }
                        
                        //ROW 1
                        Buffer.Add( "\n" ).Add( "<pos=20>" );
                        Buffer.StartColor( QuickColors.HeaderMid ).AddFormat( ArcenNetworkLogging.OverallMessagesReceived, "{0:#,##0}" ).EndColor().Add( " 总接收<pos=200>" );
                        Buffer.AddBytesWithFormatAndColor( QuickColors.HeaderMid, ArcenNetworkLogging.OverallBytesReceived ).Add( " 总接收字节" );
                        //ROW 2
                        Buffer.Add( "\n" ).Add( "<pos=20>" );
                        Buffer.StartColor( QuickColors.HeaderMid ).AddFormat( ArcenNetworkLogging.Overall.MessagesSent, "{0:#,##0}" ).EndColor().Add( " 总发送<pos=200>" );
                        Buffer.AddBytesWithFormatAndColor( QuickColors.HeaderMid, ArcenNetworkLogging.Overall.BytesSent ).Add( " 总发送字节" );
                        for ( NetChannel c = NetChannel.Main; c < NetChannel.Length; c++ )
                        {
                            NetDataVol vol = ArcenNetworkLogging.GetNetDataVol( c );
                            if ( vol.MessagesSent <= 0 )
                                continue;
                            //row 3+
                            Buffer.Add( "\n" ).Add( "<pos=20>" );
                            string name = string.Empty;
                            switch ( c )
                            {
                                case NetChannel.Main:
                                    name = "主通道";
                                    break;
                                case NetChannel.Frequent:
                                    name = "频繁";
                                    break;
                                case NetChannel.Bulky1:
                                    name = "大包-1";
                                    break;
                                case NetChannel.Bulky2:
                                    name = "大包-2";
                                    break;
                                case NetChannel.Bulky3:
                                    name = "大包-3";
                                    break;
                            }
                            Buffer.StartColor( QuickColors.HeaderMid ).AddFormat( vol.MessagesSent, "{0:#,##0}" ).EndColor().Add( " 发送 " ).Add( name ).Add( "<pos=200>" );
                            Buffer.AddBytesWithFormatAndColor( QuickColors.HeaderMid, vol.BytesSent ).Add( " 发送 " ).Add( name );
                        }

                        Engine_AIW2.WriteNetworkingDetailedStats( Buffer, false );

                        Buffer.Add( "\n" );
                    }
                    #endregion
                    
                    #region Current Planet

                    if ( RenderCurrentPlanet && localPlanet != null )
                    {
                        Buffer.StartColor( QuickColors.HeaderBright ).Add( "\n<b>当前星球性能：</b>\n" ).EndColor();

                        #region local methods
                        int numItems = 0;
                        void AddNum( int val, string label )
                        {
                            numItems++;
                            bool endline = numItems % 2 == 0;

                            if (endline)
                                Buffer.Add( "<pos=20>" );
                            else
                                Buffer.Add( "<pos=200>" );
                            
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( val ).EndColor().Add( " " ).Add( label );
                            
                            if ( endline )
                                Buffer.Add( "\n" );
                        }
                        void NewRow()
                        {
                            if (numItems%2 > 0)
                                Buffer.NewLine();
                        }
                        #endregion

                        //AddNum( EntitySystem.num_range_to_shoot_calls, "#range_checks");
                        //Buffer.NewLine();

                        if (AIWar2GalaxySettingQuickAccess.ExperimentalStackingAlgorithm_Enabled)
                        {
                            var data = Core.Stacking.Data.Get( localPlanet );
                            
                            AddNum( data.ShipCount, "#舰船" );
                            AddNum( data.SquadCount, "#小队" );
                            AddNum( data.ShipsNotStackable, "#不可堆叠舰船" );
                            AddNum( data.SquadsNotStackable, "#不可堆叠小队" );
                            //AddNum( data.ShipsStackable, "#ShipsCanStck" );
                            //AddNum( data.SquadsStackable, "#SquadsCanStck" );
                            AddNum( data.GroupCount, "#组" );
                            AddNum( data.GroupsOfOne, "#单组" );
                            AddNum( data.ClusterCount, "#群集" );
                            AddNum( data.ClustersOfOne, "#单群集" );
                            //NewRow();
                            AddNum( data.Setting_MaxSquads, "最大小队数" );
                            //NewRow();
                            AddNum( data.Setting_MaxDistanceToMergeSquads, "最大合并距离" );
                            //NewRow();
                            //AddNum( data.Setting_MinDistanceBetweenSquads, "Setting_MinDistanceBetweenSquads" );
                            NewRow();

                            Buffer.NewLine();

                            Core.Stacking.Data.Return( data );
                        }
                    }

                    #endregion

                    #region Current Game Performance

                    if ( RenderGamePerf )
                    {
                    Buffer.StartColor( QuickColors.HeaderBright ).Add( "<b>当前游戏性能：</b> " ).EndColor();
                    debugStage = 420;

                    #region Entity Counts
                    //ROW 1B
                    Buffer.Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( AudioAndSimilarHandler.TotalShips ).EndColor().Add( " 舰船<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( AudioAndSimilarHandler.TotalStandaloneShips ).EndColor().Add( " 小队" );
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( AudioAndSimilarHandler.TotalUnstackableSquads ).EndColor().Add( " 不可堆叠" );
                    //ROW 2C
                    //Buffer.Add( "\n" ).Add( "<pos=20>" );
                    //Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( AudioAndSimilarHandler.TotalStackedShips ).EndColor().Add( " Stacked<pos=200>" );
                    //Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( AudioAndSimilarHandler.TotalStacksOfAtLeastTwoUnits ).EndColor().Add( " Stacks" );
                    //ROW 2B
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( AudioAndSimilarHandler.TotalContainedShips ).EndColor().Add( " 已收纳<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( AudioAndSimilarHandler.TotalContainers ).EndColor().Add( " 容器" );
                    //ROW 2C
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( AudioAndSimilarHandler.TotalDeathRegistryEntries ).EndColor().Add( " 死亡记录<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.AllFleets.Count ).EndColor().Add( " 舰队" );
                    //ROW 3
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    int shotCount = AudioAndSimilarHandler.TotalShots;
                    Buffer.StartColor( shotCount < 50000 ? QuickColors.HeaderMid : QuickColors.HeaderWarning ).AddNumberMoreReadable( shotCount ).EndColor().Add( " 射击<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( AudioAndSimilarHandler.TotalOtherEntities ).EndColor().Add( " 虫洞" );
                    //ROW 3A
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.TotalShotsHit ).EndColor().Add( " 命中<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.TotalShipsKilled ).EndColor().Add( " 击杀舰船" );
                    //ROW 3B
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( TimeBasedPoolBase.GetCountOfQuarantinedItemsInAllPools() ).EndColor().Add( " 时间隔离<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( GameEntity_Squad.TotalShipsQuarantined ).EndColor().Add( " 隔离舰船" );
                    #endregion
                    
                    if ( !ArcenNetworkAuthority.IsClient ) //these do not run on the client
                    {
                        #region Target Planning
                        //ROW 4
                        Buffer.Add( "\n" ).Add( "<pos=20>" );
                        Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( (World_AIW2.Instance.Targeting_Player.CurrentTargetListPlanningCycle - 1) ).EndColor().Add( " P目标周期<pos=200>" );
                        Buffer.StartColor( World_AIW2.Instance.Targeting_Player.TotalTimeOfLastTargetListPlanningCycle < 5 ? QuickColors.HeaderMid : QuickColors.HeaderWarning )
                            .Add( World_AIW2.Instance.Targeting_Player.TotalTimeOfLastTargetListPlanningCycle > 0 ?
                            World_AIW2.Instance.Targeting_Player.TotalTimeOfLastTargetListPlanningCycle.ToString( "#,##0.00" ) : "??" ).Add( "s" ).EndColor().Add( " 上一P周期" );

                        //ROW 4A
                        Buffer.Add( "\n" ).Add( "<pos=20>" );
                        if ( World_AIW2.Instance.Targeting_Player.TotalTimeOfLastTargetListPlanningCycle > 0 )
                        {
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Targeting_Player.LastTargetListPlanningCycleAssignments_Systems ).EndColor().Add( " P攻击系统<pos=200>" );
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Targeting_Player.LastTargetListPlanningCycleAssignments_Targets ).EndColor().Add( " P目标" );
                        }
                        else
                        {
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Targeting_Player.CurrentSoFarTargetListPlanningCycleAssignments_Systems ).EndColor().Add( " P攻击系统<pos=200>" );
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Targeting_Player.CurrentSoFarTargetListPlanningCycleAssignments_Targets ).EndColor().Add( " P目标" );
                        }

                        //ROW 4B
                        Buffer.Add( "\n" ).Add( "<pos=20>" );
                        Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( (World_AIW2.Instance.Targeting_NPC_A.CurrentTargetListPlanningCycle - 1) ).EndColor().Add( " NP-A目标周期<pos=200>" );
                        Buffer.StartColor( World_AIW2.Instance.Targeting_NPC_A.TotalTimeOfLastTargetListPlanningCycle < 5 ? QuickColors.HeaderMid : QuickColors.HeaderWarning )
                            .Add( World_AIW2.Instance.Targeting_NPC_A.TotalTimeOfLastTargetListPlanningCycle > 0 ?
                            World_AIW2.Instance.Targeting_NPC_A.TotalTimeOfLastTargetListPlanningCycle.ToString( "#,##0.00" ) : "??" ).Add( "s" ).EndColor().Add( " 上一NP-A周期" );

                        //ROW 4C
                        Buffer.Add( "\n" ).Add( "<pos=20>" );
                        if ( World_AIW2.Instance.Targeting_NPC_A.TotalTimeOfLastTargetListPlanningCycle > 0 )
                        {
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Targeting_NPC_A.LastTargetListPlanningCycleAssignments_Systems ).EndColor().Add( " NP-A攻击系统<pos=200>" );
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Targeting_NPC_A.LastTargetListPlanningCycleAssignments_Targets ).EndColor().Add( " NP-A目标" );
                        }
                        else
                        {
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Targeting_NPC_A.CurrentSoFarTargetListPlanningCycleAssignments_Systems ).EndColor().Add( " NP-A攻击系统<pos=200>" );
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Targeting_NPC_A.CurrentSoFarTargetListPlanningCycleAssignments_Targets ).EndColor().Add( " NP-A目标" );
                        }

                        //ROW 4D
                        Buffer.Add( "\n" ).Add( "<pos=20>" );
                        Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( (World_AIW2.Instance.Targeting_NPC_B.CurrentTargetListPlanningCycle - 1) ).EndColor().Add( " NP-B目标周期<pos=200>" );
                        Buffer.StartColor( World_AIW2.Instance.Targeting_NPC_B.TotalTimeOfLastTargetListPlanningCycle < 5 ? QuickColors.HeaderMid : QuickColors.HeaderWarning )
                            .Add( World_AIW2.Instance.Targeting_NPC_B.TotalTimeOfLastTargetListPlanningCycle > 0 ?
                            World_AIW2.Instance.Targeting_NPC_B.TotalTimeOfLastTargetListPlanningCycle.ToString( "#,##0.00" ) : "??" ).Add( "s" ).EndColor().Add( " 上一NP-B周期" );

                        //ROW 4E
                        Buffer.Add( "\n" ).Add( "<pos=20>" );
                        if ( World_AIW2.Instance.Targeting_NPC_B.TotalTimeOfLastTargetListPlanningCycle > 0 )
                        {
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Targeting_NPC_B.LastTargetListPlanningCycleAssignments_Systems ).EndColor().Add( " NP-B攻击系统<pos=200>" );
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Targeting_NPC_B.LastTargetListPlanningCycleAssignments_Targets ).EndColor().Add( " NP-B目标" );
                        }
                        else
                        {
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Targeting_NPC_B.CurrentSoFarTargetListPlanningCycleAssignments_Systems ).EndColor().Add( " NP-B攻击系统<pos=200>" );
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Targeting_NPC_B.CurrentSoFarTargetListPlanningCycleAssignments_Targets ).EndColor().Add( " NP-B目标" );
                        }

                        //ROW 4F
                        Buffer.Add( "\n" ).Add( "<pos=20>" );
                        Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( (World_AIW2.Instance.Targeting_NPC_C.CurrentTargetListPlanningCycle - 1) ).EndColor().Add( " NP-C目标周期<pos=200>" );
                        Buffer.StartColor( World_AIW2.Instance.Targeting_NPC_C.TotalTimeOfLastTargetListPlanningCycle < 5 ? QuickColors.HeaderMid : QuickColors.HeaderWarning )
                            .Add( World_AIW2.Instance.Targeting_NPC_C.TotalTimeOfLastTargetListPlanningCycle > 0 ?
                            World_AIW2.Instance.Targeting_NPC_C.TotalTimeOfLastTargetListPlanningCycle.ToString( "#,##0.00" ) : "??" ).Add( "s" ).EndColor().Add( " 上一NP-C周期" );

                        //ROW 4G
                        Buffer.Add( "\n" ).Add( "<pos=20>" );
                        if ( World_AIW2.Instance.Targeting_NPC_C.TotalTimeOfLastTargetListPlanningCycle > 0 )
                        {
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Targeting_NPC_C.LastTargetListPlanningCycleAssignments_Systems ).EndColor().Add( " NP-C攻击系统<pos=200>" );
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Targeting_NPC_C.LastTargetListPlanningCycleAssignments_Targets ).EndColor().Add( " NP-C目标" );
                        }
                        else
                        {
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Targeting_NPC_C.CurrentSoFarTargetListPlanningCycleAssignments_Systems ).EndColor().Add( " NP-C攻击系统<pos=200>" );
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Targeting_NPC_C.CurrentSoFarTargetListPlanningCycleAssignments_Targets ).EndColor().Add( " NP-C目标" );
                        }

                        //ROW 4H
                        Buffer.Add( "\n" ).Add( "<pos=20>" );
                        Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( (World_AIW2.Instance.Targeting_NPC_D.CurrentTargetListPlanningCycle - 1) ).EndColor().Add( " NP-D目标周期<pos=200>" );
                        Buffer.StartColor( World_AIW2.Instance.Targeting_NPC_D.TotalTimeOfLastTargetListPlanningCycle < 5 ? QuickColors.HeaderMid : QuickColors.HeaderWarning )
                            .Add( World_AIW2.Instance.Targeting_NPC_D.TotalTimeOfLastTargetListPlanningCycle > 0 ?
                            World_AIW2.Instance.Targeting_NPC_D.TotalTimeOfLastTargetListPlanningCycle.ToString( "#,##0.00" ) : "??" ).Add( "s" ).EndColor().Add( " 上一NP-D周期" );

                        //ROW 4I
                        Buffer.Add( "\n" ).Add( "<pos=20>" );
                        if ( World_AIW2.Instance.Targeting_NPC_D.TotalTimeOfLastTargetListPlanningCycle > 0 )
                        {
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Targeting_NPC_D.LastTargetListPlanningCycleAssignments_Systems ).EndColor().Add( " NP-D攻击系统<pos=200>" );
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Targeting_NPC_D.LastTargetListPlanningCycleAssignments_Targets ).EndColor().Add( " NP-D目标" );
                        }
                        else
                        {
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Targeting_NPC_D.CurrentSoFarTargetListPlanningCycleAssignments_Systems ).EndColor().Add( " NP-D攻击系统<pos=200>" );
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Targeting_NPC_D.CurrentSoFarTargetListPlanningCycleAssignments_Targets ).EndColor().Add( " NP-D目标" );
                        }
                        #endregion

                        #region Decollision
                        //ROW 5
                        Buffer.Add( "\n" ).Add( "<pos=20>" );
                        Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( (World_AIW2.Instance.Decollision_Player.CurrentDecollisionCycle - 1) ).EndColor().Add( " P去碰撞周期<pos=200>" );
                        Buffer.StartColor( World_AIW2.Instance.Decollision_Player.TotalTimeOfLastDecollisionCycle < 5 ? QuickColors.HeaderMid : QuickColors.HeaderWarning )
                            .Add( World_AIW2.Instance.Decollision_Player.TotalTimeOfLastDecollisionCycle > 0 ?
                            World_AIW2.Instance.Decollision_Player.TotalTimeOfLastDecollisionCycle.ToString( "#,##0.00" ) : "??" ).Add( "s" ).EndColor().Add( " P上一周期" );
                        //ROW 5A
                        Buffer.Add( "\n" ).Add( "<pos=20>" );
                        if ( World_AIW2.Instance.Decollision_Player.TotalTimeOfLastDecollisionCycle > 0 )
                        {
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Decollision_Player.LastDecollisionCycleAssignments ).EndColor().Add( " P上次去碰撞<pos=200>" );
                        }
                        else
                        {
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Decollision_Player.CurrentSoFarDecollisionCycleAssignments ).EndColor().Add( " P当前去碰撞<pos=200>" );
                        }
                        //blank half row

                        //ROW 5B
                        Buffer.Add( "\n" ).Add( "<pos=20>" );
                        Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( (World_AIW2.Instance.Decollision_NPC_A.CurrentDecollisionCycle - 1) ).EndColor().Add( " NPC A去碰撞周期<pos=200>" );
                        Buffer.StartColor( World_AIW2.Instance.Decollision_NPC_A.TotalTimeOfLastDecollisionCycle < 5 ? QuickColors.HeaderMid : QuickColors.HeaderWarning )
                            .Add( World_AIW2.Instance.Decollision_NPC_A.TotalTimeOfLastDecollisionCycle > 0 ?
                            World_AIW2.Instance.Decollision_NPC_A.TotalTimeOfLastDecollisionCycle.ToString( "#,##0.00" ) : "??" ).Add( "s" ).EndColor().Add( " NPC A上一周期" );
                        //ROW 5C
                        Buffer.Add( "\n" ).Add( "<pos=20>" );
                        if ( World_AIW2.Instance.Decollision_NPC_A.TotalTimeOfLastDecollisionCycle > 0 )
                        {
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Decollision_NPC_A.LastDecollisionCycleAssignments ).EndColor().Add( " NPC A上次去碰撞<pos=200>" );
                        }
                        else
                        {
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Decollision_NPC_A.CurrentSoFarDecollisionCycleAssignments ).EndColor().Add( " NPC A当前去碰撞<pos=200>" );
                        }
                        //blank half row

                        //ROW 5D
                        Buffer.Add( "\n" ).Add( "<pos=20>" );
                        Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( (World_AIW2.Instance.Decollision_NPC_B.CurrentDecollisionCycle - 1) ).EndColor().Add( " NPC B去碰撞周期<pos=200>" );
                        Buffer.StartColor( World_AIW2.Instance.Decollision_NPC_B.TotalTimeOfLastDecollisionCycle < 5 ? QuickColors.HeaderMid : QuickColors.HeaderWarning )
                            .Add( World_AIW2.Instance.Decollision_NPC_B.TotalTimeOfLastDecollisionCycle > 0 ?
                            World_AIW2.Instance.Decollision_NPC_B.TotalTimeOfLastDecollisionCycle.ToString( "#,##0.00" ) : "??" ).Add( "s" ).EndColor().Add( " NPC B上一周期" );
                        //ROW 5E
                        Buffer.Add( "\n" ).Add( "<pos=20>" );
                        if ( World_AIW2.Instance.Decollision_NPC_B.TotalTimeOfLastDecollisionCycle > 0 )
                        {
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Decollision_NPC_B.LastDecollisionCycleAssignments ).EndColor().Add( " NPC B上次去碰撞<pos=200>" );
                        }
                        else
                        {
                            Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Decollision_NPC_B.CurrentSoFarDecollisionCycleAssignments ).EndColor().Add( " NPC B当前去碰撞<pos=200>" );
                        }
                        //blank half row
                        #endregion
                    }
                    //ROW 5F
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.TotalDecollisionsScheduled ).EndColor().Add( " 去碰撞计划<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.TotalDecollisionsRun ).EndColor().Add( " 去碰撞运行" );
                    if ( !ArcenNetworkAuthority.IsClient ) //these do not run on the client
                    {
                        //ROW 6
                        Buffer.Add( "\n" ).Add( "<pos=20>" );
                        Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( (World_AIW2.Instance.CurrentUnitStackPlanningCycle - 1) ).EndColor().Add( " 堆叠周期<pos=200>" );
                        Buffer.StartColor( QuickColors.HeaderMid ).Add( World_AIW2.Instance.TotalTimeOfLastUnitStackPlanningCycle > 0 ?
                            World_AIW2.Instance.TotalTimeOfLastUnitStackPlanningCycle.ToString( "#,##0.00" ) : "??" ).Add( "s" ).EndColor().Add( " 上一周期" );
                        //ROW 7
                        Buffer.Add( "\n" ).Add( "<pos=20>" );
                        Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.TotalUnitsStacked ).EndColor().Add( " 已堆叠<pos=200>" );
                        Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.TotalStacksSplit ).EndColor().Add( " 已拆分" );
                    }
                    //ROW 7A
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( AudioAndSimilarHandler.TotalPlanetsOn ).EndColor().Add( " 活跃星球<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( AudioAndSimilarHandler.TotalPlanetsOff ).EndColor().Add( " 等待星球" );
                    //ROW 7B
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( AudioAndSimilarHandler.TotalPlanetsTier1 ).EndColor().Add( " 1级星球<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( AudioAndSimilarHandler.TotalPlanetsTier2 ).EndColor().Add( " 2级星球" );
                    //ROW 7C
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( AudioAndSimilarHandler.TotalPlanetsTier3 ).EndColor().Add( " 3级星球<pos=200>" );
                    int waveCount = WaveUtils.GetCountOfAllWaves();
                    Buffer.StartColor( waveCount < 10 ? QuickColors.HeaderMid : QuickColors.HeaderWarning ).AddNumberMoreReadable( waveCount ).EndColor().Add( " AI波次" );
                    //ROW 8
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.CountOfLongTermPlanningThreadsStarted ).EndColor().Add( " 长期规划调用<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.CountOfEntityRemovalChecks ).EndColor().Add( " 移除检查" );
                    //ROW 9
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( AudioAndSimilarHandler.TotalSpeedGroups ).EndColor().Add( " 速度组<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.GlobalLastSpeedGroupPrimaryKeyID_HostOnly ).EndColor().Add( " 上一速度组ID" );
                    //ROW 10
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.CountOfDronesDeployed ).EndColor().Add( " 已部署无人机<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.CountOfDronesRecalled ).EndColor().Add( " 已召回无人机" );
                    //ROW 11
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.CountOfEntitiesAddedToFleets ).EndColor().Add( " 加入舰队<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.CountOfEntitiesAddedToFleetsSkippedBecauseDuplicate ).EndColor().Add( " 重复跳过" );
                    //ROW 12
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.CountOfEntitiesRemovedFromFleets ).EndColor().Add( " 离开舰队<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.CountOfEntitiesRemovedFromFleetsFailed ).EndColor().Add( " 离开失败" );
                    //ROW 13
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.CountOfIInstancedRendererRemovalRequests ).EndColor().Add( " 实例渲染移除<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.CountOfNullFleetMembershipsFixed ).EndColor().Add( " 空成员修复" );
                    //ROW 14
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( MetalFlowPlanning_Player.LastCountOfRepairingEnginesOfFriendlies ).EndColor().Add( " P船体修复<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( MetalFlowPlanning_Player.LastCountOfRepairingShieldsOfFriendlies ).EndColor().Add( " P护盾修复" );
                    //ROW 14
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( MetalFlowPlanning_NPC.LastCountOfRepairingEnginesOfFriendlies ).EndColor().Add( " N船体修复<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( MetalFlowPlanning_NPC.LastCountOfRepairingShieldsOfFriendlies ).EndColor().Add( " N护盾修复" );
                    //ROW 15
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    //Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( MetalFlowPlanning.LastCountOfRepairingHullsOfFriendlies ).EndColor().Add( " Engn Repairs<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( MetalFlowPlanning_Player.LastCountOfSelfAssistConstructions ).EndColor().Add( " P建造协助<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( MetalFlowPlanning_Player.LastCountOfFactoryAssistConstructions ).EndColor().Add( " P工厂协助" );
                    //ROW 15
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    //Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( MetalFlowPlanning.LastCountOfRepairingHullsOfFriendlies ).EndColor().Add( " Engn Repairs<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( MetalFlowPlanning_NPC.LastCountOfSelfAssistConstructions ).EndColor().Add( " N建造协助<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( MetalFlowPlanning_NPC.LastCountOfFactoryAssistConstructions ).EndColor().Add( " N工厂协助" );
                    //ROW 16
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( PathBetweenPlanetsForFaction.PathRecalculations ).EndColor().Add( " 路径重算<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( PathBetweenPlanetsForFaction.PathsPulledFromCache ).EndColor().Add( " 缓存路径" );
                    //LAST ROW
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    int commandCount = World_AIW2.Instance.OnClient_GameCommandsThatHaveBeenReceivedFromServerButNotYetExecuted.Count +
                        World_AIW2.Instance.OnClient_GameCommandsThatHaveNotYetBeenSentToServer.Count +
                        World_AIW2.Instance.OnServer_GameCommandsThatHaveNotYetBeenSentToClients.Count;
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( commandCount ).EndColor().Add( " 当前命令<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.Network_CurrentFrameNumber ).EndColor().Add( " 帧" );
                    debugStage = 440;

                    Buffer.Add( "\n\n" );
                    } //end of if ( RenderGamePerf )

                    debugStage = 600;

                    if ( RenderPools )
                    {
                    Buffer.StartColor( QuickColors.HeaderBright ).Add( "<b>内存池性能：</b> " ).EndColor();
                    //ROW 1
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( GC.CollectionCount( 0 ) ).EndColor().Add( " GC调用<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddFixedDecimalThousands( ( GC.GetTotalMemory( false ) / 1024f / 1024f), 1 ).EndColor().Add( " MB GC内存" );
                    //Chris note: the per-thread allocation checks don't really work, because of how they have to be calculated. But a future version of mono or .net could resolve this for us
                    ////ROW 1-0B: per-thread GC allocation -- which background thread is the memory-churn culprit.
                    ////Populated by ArcenThreading after every background work item (see ThreadAllocTracker).
                    //Buffer.Add( "\n" ).Add( "<pos=20>" ).StartColor( QuickColors.HeaderBright ).Add( "Per-Thread GC Alloc (last " ).Add( ThreadAllocTracker.WindowSeconds ).Add( "s):" ).EndColor();
                    //if ( !ArcenThreading.IsPerThreadAllocTrackingSupported )
                    //{
                    //    Buffer.Add( "\n" ).Add( "<pos=40>" ).StartColor( QuickColors.HeaderWarning ).Add( "(no allocation counter available on this runtime)" ).EndColor();
                    //}
                    //else
                    //{
                    //    if ( ArcenThreading.AllocMode != ArcenThreading.PerThreadAllocMode.PerThreadAccurate )
                    //        Buffer.Add( "\n" ).Add( "<pos=40>" ).StartColor( "8d8d8d" ).Add(
                    //            ArcenThreading.AllocMode == ArcenThreading.PerThreadAllocMode.ProcessCumulative
                    //                ? "(approx: process-wide alloc charged to each thread's run window)"
                    //                : "(rough: heap-size delta, GC-affected)" ).EndColor();
                    //    float allocNow = ArcenTime.TimeSinceStartF;
                    //    threadAllocScratch.Clear();
                    //    foreach ( ArcenThreadingRequestType rt in ArcenThreadingRequestTypeTable.Instance.Rows )
                    //    {
                    //        if ( rt == null )
                    //            continue;
                    //        if ( rt.AllocTracker.GetBytesInWindow( allocNow ) > 0 )
                    //            threadAllocScratch.Add( rt );
                    //    }
                    //    cb_threadAllocSortNow = allocNow;
                    //    threadAllocScratch.Sort( cb_compareThreadAllocDescending );

                    //    float totalMBPerSec = 0f;
                    //    for ( int i = 0; i < threadAllocScratch.Count; i++ )
                    //    {
                    //        ArcenThreadingRequestType rt = threadAllocScratch[i];
                    //        float mbPerSec = rt.AllocTracker.GetBytesPerSecondInWindow( allocNow ) / 1048576f;
                    //        totalMBPerSec += mbPerSec;
                    //        if ( i < 12 ) //list is sorted worst-first; cap the rows so a busy run can't flood the panel
                    //        {
                    //            Buffer.Add( "\n" ).Add( "<pos=40>" );
                    //            Buffer.StartColor( mbPerSec >= 5f ? QuickColors.HeaderWarning : QuickColors.HeaderMid ).AddFixedDecimalThousands( mbPerSec, 2 ).EndColor().Add( " MB/s<pos=170>" );
                    //            Buffer.Add( rt.InternalName );
                    //        }
                    //    }
                    //    if ( threadAllocScratch.Count == 0 )
                    //        Buffer.Add( "\n" ).Add( "<pos=40>" ).Add( "(no tracked background-thread allocation in the window)" );
                    //    else
                    //    {
                    //        Buffer.Add( "\n" ).Add( "<pos=20>" );
                    //        Buffer.StartColor( QuickColors.HeaderMid ).AddFixedDecimalThousands( totalMBPerSec, 2 ).EndColor().Add( " MB/s total across tracked threads" );
                    //    }
                    //}
                    //ROW 1-1
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).Add( " " ).EndColor().Add( " <pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( ArcenPathfinder<Planet>.TotalPathfindersEver ).EndColor().Add( " 寻路器" );
                    //ROW 1-2
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( PathCache.TotalPathCachesEver ).EndColor().Add( " 路径缓存<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( ProjectedLocalPlayerMultiPathData.TotalocalPlayerMultiPathDatasEver ).EndColor().Add( " 多路径数据" );
                    //ROW 1A
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( ArcenDoubleCharacterBuffer.MatchesSavedAndReturned ).EndColor().Add( " 已保存文本<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( ArcenDoubleCharacterBuffer.Mismatches ).EndColor().Add( " 新文本" );
                    //ROW 2
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( GameCommand.TotalRealGameCommandsCreated ).EndColor().Add( " 原始命令<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( GameCommand.TotalGameCommandsPooledOrCreated ).EndColor().Add( " 总命令" );
                    //ROW 2B
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.OnClient_GameCommandsThatHaveBeenReceivedFromServerButNotYetExecuted.Count ).EndColor().Add( " 待执行命令<pos=200>" );
                    //Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.OnClient_GameCommandsThatHaveBeenReceivedFromServerButNotYetExecuted_Backup.Count ).EndColor().Add( " N-Ex Backup Cmds" );
                    //ROW 2C
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.OnClient_GameCommandsThatHaveNotYetBeenSentToServer.Count ).EndColor().Add( " 客户端命令<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( World_AIW2.Instance.OnServer_GameCommandsThatHaveNotYetBeenSentToClients.Count ).EndColor().Add( " 主机命令" );
                    //ROW 2D
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( GameEntity_Shot.GetNumberRawCreated() ).EndColor().Add( " 原始射击<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( GameEntity_Shot.GetNumberCreatedOrPoolRequested() ).EndColor().Add( " 总射击" );
                    
                    #region unused
                    //Planet localPlanet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                    //if ( localPlanet != null )
                    //{
                    //    for ( int index = 0; index < localPlanet.Factions.Count; index++ )
                    //    {
                    //        PlanetFaction localPlanetFaction = localPlanet.Factions[index];
                    //        if(!localPlanetFaction.GetIsLocalFaction())
                    //            continue;
                    //        ArcenMob<GameEntity_Shot> mob = localPlanetFaction.Entities.Entities_Shot;
                    //        int nominalCount = mob.NominalCount;
                    //        GameEntity_Shot[] shotArray = mob.Items;
                    //        int totalArraySize = shotArray.Length;
                    //        int nullCount = 0;
                    //        int nonNullCount = 0;
                    //        int highestNonNullIndex = 0;
                    //        for ( int i = 0; i < shotArray.Length; i++ )
                    //        {
                    //            if ( shotArray[i] == null )
                    //                nullCount++;
                    //            else
                    //            {
                    //                nonNullCount++;
                    //                highestNonNullIndex = i;
                    //            }
                    //        }
                    //        Buffer.Add( "\n" ).Add( "<pos=20>" );
                    //        Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( nominalCount ).EndColor().Add( " Shot Mob Nominal Count" );
                    //        Buffer.Add( "\n" ).Add( "<pos=20>" );
                    //        Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( totalArraySize ).EndColor().Add( " Shot Mob Total Array Size Count" );
                    //        Buffer.Add( "\n" ).Add( "<pos=20>" );
                    //        Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( nonNullCount ).EndColor().Add( " Shot Mob Non-Null Count" );
                    //        Buffer.Add( "\n" ).Add( "<pos=20>" );
                    //        Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( nullCount ).EndColor().Add( " Shot Mob Null Count" );
                    //        Buffer.Add( "\n" ).Add( "<pos=20>" );
                    //        Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( highestNonNullIndex ).EndColor().Add( " Shot Mob Highest Non-Null Index" );
                    //    }
                    //}
                    #endregion
                    
                    //ROW 3
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( GameEntity_Squad.NumberRawSquadsCreated ).EndColor().Add( " 已创建舰船<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( WrapperedAddonObjectOrParticleField.TotalAddonObjectsInstantiated ).EndColor().Add( " 附件/粒子" );
                    //ROW 4
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( GameEntity_Other.GetNumberRawCreated() ).EndColor().Add( " 原始虫洞<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( GameEntity_Other.GetNumberCreatedOrPoolRequested() ).EndColor().Add( " 总虫洞" );
                    //ROW 6
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( ShipToShipLineRenderer.CountOfLineTypesToRenderThisFrame ).EndColor().Add( " 线类型<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( ShipToShipLineRenderer.LastCountOfLinesToRenderThisFrame ).EndColor().Add( " 总线条" );
                    //ROW 6-A
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( ShipToShipLineRenderer.WorkingCountOfLinesToGetNumericsThisFrame ).EndColor().Add( " 线数值<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( ShipToShipLineRenderer.WorkingCountOfLinesToCalculateFinalsThisFrame ).EndColor().Add( " 已计算线" );
                    //ROW 7
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( PlanetFaction.NumberRawCreated ).EndColor().Add( " 原始星球阵营<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( PlanetFaction.NumberCreatedOrPoolRequested ).EndColor().Add( " 总星球阵营" );
                    //ROW 8D
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( Faction.NumberFlowsForUI_Cycles ).EndColor().Add( " UI预算周期<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( Faction.NumberFlowsForUI_TotalAdded ).EndColor().Add( " UI预算已添加" );
                    //ROW 8E
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( GalaxyMapPlanet.NumberRawCreated ).EndColor().Add( " 原始银河星球<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( GalaxyMapPlanet.NumberCreatedOrPoolRequested ).EndColor().Add( " 请求银河星球" );
                    //ROW 8F
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( GalaxyMapPlanetLink.NumberRawCreated ).EndColor().Add( " 原始银河链接<pos=200>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( GalaxyMapPlanetLink.NumberCreatedOrPoolRequested ).EndColor().Add( " 请求银河链接" );
                    //ROW 8G
                    Buffer.Add( "\n" ).Add( "<pos=20>" );
                    Buffer.StartColor( QuickColors.HeaderMid ).AddNumberMoreReadable( Engine_AIW2.NumberOfGimbalMaterialCombinations ).EndColor().Add( " 万向节材质组合<pos=200>" );
                    int queuedSFX = World_AIW2.Instance.NumberOfQueuedSoundEffects;
                    Buffer.StartColor( queuedSFX > 10 ? QuickColors.HeaderWarning : QuickColors.HeaderMid ).AddNumberMoreReadable( queuedSFX ).EndColor().Add( " 队列音效" );

                    //ROWS FROM BATTLEFIELD SINGLETON
                    Engine_AIW2.Instance.PresentationLayer.WriteBattlefieldVisualHandelerDetailsToEscapeMenu( Buffer );
                    
                    //ROW 10
                    if ( GameSettings.Current.GetBoolBySetting( "Debug_ShowPoolCountsInEscapeMenu" ) )
                    {
                        CountedPoolBase.SortAllPoolsInList( false );
                        Buffer.Add( "\n" ).Add( "<pos=40>" ).StartColor( ColorMath.LightBlue ).Add( "详细池计数" ).EndColor();
                        foreach ( CountedPoolBase pool in CountedPoolBase.AllPoolsForSorting )
                        {
                            Buffer.Add( "\n" ).Add( "<pos=40>" ).Add( pool.PoolNameOrig ).Add( ": " ).AddNumberMoreReadable( pool.ItemsCreated );
                        }
                    }
                    //ROW 11
                    if ( GameSettings.Current.GetBoolBySetting( "Debug_ShowDetailsOfTimeBasedPoolsInEscapeMenu" ) )
                    {
                        Buffer.Add( "\n" ).Add( "<pos=40>" ).StartColor( ColorMath.LightBlue ).Add( "基于时间的池详情" ).EndColor();
                        foreach ( TimeBasedPoolBase pool in TimeBasedPoolBase.AllTimeBasedPools )
                        {
                            Buffer.Add( "\n" ).StartColor( ColorMath.LightGreen ).Add( "<pos=40>" ).Add( pool.PoolNameOrig ).Add( ": " ).AddNumberMoreReadable( pool.ItemsCreated ).EndColor();
                            Buffer.Add( "\n" ).Add( "<pos=100>放回池中的物品：" ).AddNumberMoreReadable( pool.TotalItemsPutBackInPools );
                            Buffer.Add( "\n" ).Add( "<pos=100>原始创建数量：" ).AddNumberMoreReadable( pool.NumberRawCreated );
                            Buffer.Add( "\n" ).Add( "<pos=100>创建或池请求数量：" ).AddNumberMoreReadable( pool.NumberCreatedOrPoolRequested );
                            Buffer.Add( "\n" ).Add( "<pos=100>当前所有子池中的总物品数：" ).AddNumberMoreReadable( pool.CalculateTotalItemsInAllPoolsRightNow() );
                        }
                    }

                    //ChainList Pool Stats: per-bucket Rents / Misses / Evictions for the shared array pools that back
                    //ChainList<T>. Misses and Evictions should stay at zero in normal play once the pool warms up. If
                    //either climbs over time, report the numbers - they tell us which bucket size needs more headroom.
                    Buffer.Add( "\n\n" ).StartColor( QuickColors.HeaderBright ).Add( "<b>链表池统计：</b> " ).EndColor();
                    ChainListSharedPools.WriteAllDiagnostics( Buffer );

                    Buffer.Add( "\n\n" );
                    } //end of if ( RenderPools )
                    #endregion

                    debugStage = 1000;                    

                    #region Write Player Faction(s) Stats
                    //for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                    //{
                    //    Faction faction = World_AIW2.Instance.Factions[i];
                    //    if ( faction == null || faction.SpecialFactionData == null )
                    //        continue;
                    //    if ( faction.SpecialFactionData.ShouldNotBeShown )
                    //        continue;
                    //    if ( faction.Type != FactionType.Player )
                    //        continue;
                    //    debugStage = 1010;

                    //    Buffer.StartColor( faction.FactionCenterColor.ColorHexBrighter ).Add( "<b>" ).Add( faction.GetDisplayName() ).Add( " Stats:</b>\n" ).EndColor();
                    //    Buffer.Add( "<size=10>" );
                    //    debugStage = 1020;

                    //    debugStage = 1030;
                    //    ShipStats stats = prims.stats;
                    //    if ( stats == null )
                    //        continue;
                    //    debugStage = 1040;
                    //    stats.WriteCasualtiesToBuffer( Buffer );
                    //    debugStage = 1050;
                    //    stats.WriteLossesToBuffer( Buffer );
                    //    Buffer.Add( "</size>\n\n" );
                    //}
                    #endregion
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "Error at about campaign stage: " + debugStage + "\n" + e, Verbosity.ShowAsError );
                }
            }

            public override void OnUpdate()
            {
                ArcenUI_Text myText = (this.Element as ArcenUI_Text);
                myText.SupportsHyperlinks = true;
            }
        }

        public class bCPULoad : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "CPU负载：" );

                int totalLoad = EndpointFunctions.CalculatePredictedGameLoad( null, false );
                if ( totalLoad <= 0 )
                {
                    Buffer.Add( "正在计算负载...</size>" );
                }
                else
                {
                    string loadColor = EndpointFunctions.CalculateLoadColorAndCategory( totalLoad, out string LoadName, out _ );
                    Buffer.StartColor( loadColor ).Add( LoadName ).Add( "  " ).AddNumberMoreReadable( totalLoad ).EndColor();
                }
            }

            private static readonly ArcenDoubleCharacterBuffer summaryBuffer = new ArcenDoubleCharacterBuffer( "Window_SetupFooterControls-btnStartGame-summaryBuffer" );
            private static readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_SetupFooterControls-btnStartGame-tooltipBuffer" );
            private static readonly ArcenDoubleCharacterBuffer detailsBuffer = new ArcenDoubleCharacterBuffer( "Window_SetupFooterControls-btnStartGame-detailsBuffer" );
            private static readonly ArcenDoubleCharacterBuffer popupBuffer = new ArcenDoubleCharacterBuffer( "Window_SetupFooterControls-btnStartGame-popupBuffer" );
            public override void HandleMouseover()
            {
                int totalLoad = EndpointFunctions.CalculatePredictedGameLoad( summaryBuffer, true );
                if ( totalLoad <= 0 )
                    tooltipBuffer.Add( "" ).Add( "正在计算负载..." );
                else
                {
                    string loadColor = EndpointFunctions.CalculateLoadColorAndCategory( totalLoad, out string LoadName, out string Explantion );
                    tooltipBuffer.StartColor( loadColor ).Add( LoadName ).Add( " / 负载分数 " ).AddNumberMoreReadable( totalLoad ).EndColor();
                    tooltipBuffer.Add( "\n" ).Add( Explantion );
                }
                tooltipBuffer.Add( "\n\n请注意，负载估算仅为估算值；如果你在游戏中通过信标黑客添加了更多阵营，负载可能会上升。你可以在下方的帧率和模拟速度计数器中看到实际性能，那些才是真实数据。此提示信息旨在预测你在长期游戏中可能遇到的情况。" );
                tooltipBuffer.Add( "\n负载等级原因：\n" ).Add( summaryBuffer.GetStringAndResetForNextUpdate() );
                tooltipBuffer.Add( "\n\n点击此按钮查看详情。" );

                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }

            private static ArcenDoubleCharacterBuffer errorTextBuffer = new ArcenDoubleCharacterBuffer( "btnStartGame-errorTextBuffer" );

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                int totalLoad = EndpointFunctions.CalculatePredictedGameLoad( detailsBuffer, false );
                if ( totalLoad <= 0 )
                    popupBuffer.Add( "" ).Add( "正在计算负载 - 关闭并重新打开以查看" );
                else
                {
                    string loadColor = EndpointFunctions.CalculateLoadColorAndCategory( totalLoad, out string LoadName, out string Explantion );
                    popupBuffer.StartColor( loadColor ).Add( LoadName ).Add( " / 负载分数 " ).AddNumberMoreReadable( totalLoad ).EndColor();
                    popupBuffer.Add( "\n" ).Add( Explantion );
                }
                popupBuffer.Add( "\n\n负载等级详细原因：\n" ).Add( detailsBuffer.GetStringAndResetForNextUpdate() );

                popupBuffer.Add( "\n\n请注意，负载估算仅为估算值；如果你在游戏中通过信标黑客添加了更多阵营，负载可能会上升。你可以在下方的帧率和模拟速度计数器中看到实际性能，那些才是真实数据。此提示信息旨在预测你在长期游戏中可能遇到的情况。" );

                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.TallWide, null, "预测的游戏全程CPU负载详情", popupBuffer.GetStringAndResetForNextUpdate(), "确定" );

                return MouseHandlingResult.None;
            }
        }
    }
}
