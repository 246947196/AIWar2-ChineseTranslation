using Arcen.Universal;
using Arcen.AIW2.Core;
using System;


namespace Arcen.AIW2.External
{
    public class Window_InGameDeveloperToolsMenu : StackMenuWindowController
    {
        public static Window_InGameDeveloperToolsMenu Instance;
        public Window_InGameDeveloperToolsMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
        }

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "调试菜单" );
            }

            public override void OnUpdate() { }
        }

        public override string GetBriefName() { return "调试"; }

        public class bViewCheatsAndCommands : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                Buffer.Add( "<color=#40c2ff>Wiki:</color> 秘籍与命令列表" );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                System.Diagnostics.Process.Start( "https://wiki.arcengames.com/index.php?title=AI_War_2:_Cheats" );
                return MouseHandlingResult.None;
            }
        }

        public class bRevealAll : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                Buffer.Add( "观察全部" );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                EndpointFunctions.Debug_ScoutAll( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                if ( World_AIW2.Instance.Setup.GetBoolBySetting( "RevealingMapDetailsIsConsideredCheating" ) )
                    Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "根据你当前的银河选项设置（侦察 -> 战争迷雾），这被视为作弊。这将永久观察所有星球" );
                else
                    Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "根据你当前的银河选项设置（侦察 -> 战争迷雾），这不被视为作弊。这将永久观察所有星球" );
            }
        }

        public class bExploreAll : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                Buffer.Add( "探索全部" );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                EndpointFunctions.Debug_ExploreAll( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                if ( World_AIW2.Instance.Setup.GetBoolBySetting( "RevealingMapDetailsIsConsideredCheating" ) )
                    Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "根据你当前的银河选项设置（侦察 -> 战争迷雾），这被视为作弊。这是观察全部的弱化版本；它授予你所有星球的最新探索知识，但不会将它们设置为永久观察。" );
                else
                    Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "根据你当前的银河选项设置（侦察 -> 战争迷雾），这不被视为作弊。这是观察全部的弱化版本；它授予你所有星球的最新探索知识，但不会将它们设置为永久观察。" );
            }
        }

        public class bCreatePreset : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                Buffer.Add( "保存预设" );
            }
            
            private static string _lastName = "QuickName";
            //private static string _lastDesc = "QuickDescription";
            
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_ModalDualTextboxWindow.Instance.Open( 
                    "保存为快速开始？", 
                    "名称:", 
                    "描述:", 
                    true, true, _lastName, "[unused]", 30, 3000, 
                    (name, desc)=>
                    {
                        //if (!string.IsNullOrEmpty(desc))
                            //_lastDesc = desc;
                        if (!string.IsNullOrEmpty(name))
                            _lastName = name;
                        
                        if (string.IsNullOrEmpty(name))
                        {
                            LOG.Err("必须为你的快速开始输入名称。");
                            return;
                        }
                        
                        SaveLoadMethods.SaveWorldToDisk_AsQuickStart(name);
                    } );
                
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "将当前游戏保存为社区文件夹中的预设" );
            }
        }
        public class bListMusic : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                Buffer.Add( "更换音乐" );
            }

            private static ProtectedList<CustomPopupData> musicTrackList = ProtectedList<CustomPopupData>.Create_WillNeverBeGCed( 500, "Window_InGameDeveloperToolsMenu-bListMusic-musicTrackList" );

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                MusicTypeData.currentMusicTypeData.FillWorkingList();
                if ( MusicTypeData.currentMusicTypeData == null || MusicTypeData.currentMusicTypeData.MusicListWorking.Count == 0 )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null,
                        "没有播放音乐！", "当前没有播放音乐——可能音乐在设置中被关闭了。", "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                musicTrackList.Clear( true );
                MusicTrack currentTrack = MusicTypeData.currentMusicTypeData.MusicListWorking[0];
                List<MusicTrack> allMusicTracks = MusicTypeData.currentMusicTypeData.MusicListFull;

                for ( int i = 0; i < MusicTypeData.currentMusicTypeData.MusicListFull.Count; i++ )
                {
                    MusicTrack musicTrack = MusicTypeData.currentMusicTypeData.MusicListFull[i];
                    string tooltip;

                    if ( musicTrack.RowFromExpansion != null )
                        tooltip = "此曲目来自 " + musicTrack.RowFromExpansion.DisplayName + " 资料片";
                    else if ( !String.IsNullOrEmpty(musicTrack.Description) )
                        tooltip = musicTrack.Description;
                    else
                        tooltip = "此曲目来自基础游戏";
                    if ( musicTrack.GetHasTag("Exciting") )
                        tooltip = "\n这是一首节奏快、令人兴奋的曲目。";

                    CustomPopupData option = CustomPopupData.GetFromPoolOrCreate();
                    option.InternalName = musicTrack.InternalName;
                    option.DisplayName = musicTrack.DisplayName;
                    option.Tooltiptext = tooltip;

                    if ( musicTrack.DisplayName == ArcenMusicPlayer.Instance.GetCurrentlyPlayingMusicTrack() )
                    {
                        option.CanBeSelected = false;
                        option.CannotBeSelectedReason = "此曲目正在播放。";
                    }
                    if ( !GameSettings.Current.DisabledMusicTracks.Contains( musicTrack.InternalName ) )
                    {
                        musicTrackList.Add( option );
                    }
                    else
                        option.ReturnToPool();
                }
                if ( ArcenMusicPlayer.Instance.GetIsMusicPlaying() )
                {
                    MusicTrack currentlyPlaying = MusicTrackTable.Instance.GetRowByNameOrNullIfNotFound( ArcenMusicPlayer.Instance.GetCurrentlyPlayingMusicTrack() );
                    if (currentlyPlaying != null )
                    {
                        CustomPopupData option = CustomPopupData.GetFromPoolOrCreate();
                        option.InternalName = currentlyPlaying.InternalName;
                        option.DisplayName = currentlyPlaying.DisplayName;
                        option.Tooltiptext = currentlyPlaying.Description;
                        musicTrackList.Add( option );
                    }
                }
                if ( musicTrackList.Count > 1 )
                {
                    musicTrackList.Sort( static delegate ( CustomPopupData Left, CustomPopupData Right )
                    {
                        int val = Left.CanBeSelected.CompareTo( Right.CanBeSelected ); //asc
                        if ( val != 0 )
                            return val;
                        return Left.DisplayName.CompareTo( Right.DisplayName );
                    } );

                    Window_PopupScrollingColumnButtonList.Instance.Open( "选择音乐曲目", null, musicTrackList,
                    delegate ( CustomPopupData selectedTrack )
                    {
                        if ( selectedTrack == null )
                            return;

                        MusicTrack newMusicTrack = MusicTrackTable.Instance.GetRowByNameOrNullIfNotFound( selectedTrack.InternalName );
                        if ( newMusicTrack != currentTrack && newMusicTrack != null )
                        {                       
                            if ( MusicTypeData.currentMusicTypeData.MusicListWorking.Contains( newMusicTrack ) )
                                MusicTypeData.currentMusicTypeData.MusicListWorking.Remove( newMusicTrack );
                            ArcenMusicPlayer.Instance.SetMusicTrack( newMusicTrack, MusicTypeData.currentMusicTypeData.Type );
                            ArcenMusicPlayer.Instance.PlayMusic();
                        }
                    }, null );
                }
                else
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, 
                        "没有其他音乐！", "没有其他可播放的音乐曲目！请检查音乐文件是否存在于你的电脑上。", "确定" );

                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "列出所有音乐曲目并允许更换" );
            }
        }
        public class bDisableMusic : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                Buffer.Add( "禁用音乐" );
            }
            public static readonly ProtectedList<CustomPopupData> musicTrackList = ProtectedList<CustomPopupData>.Create_WillNeverBeGCed( 400, "Window_InGameDeveloperToolsMenu-bDisableMusic-musicTrackList" );
            public static readonly ProtectedList<CustomPopupData> disabledMusicTrackList = ProtectedList<CustomPopupData>.Create_WillNeverBeGCed( 400, "Window_InGameDeveloperToolsMenu-bDisableMusic-disabledMusicTrackList" );

            public void updateLists()
            {
                string currentTrack = ArcenMusicPlayer.Instance.GetCurrentlyPlayingMusicTrack();
                musicTrackList.Clear( true );
                disabledMusicTrackList.Clear( true );
                for ( int i = 0; i < MusicTypeData.currentMusicTypeData.MusicListFull.Count; i++ )
                {
                    MusicTrack musicTrack = MusicTypeData.currentMusicTypeData.MusicListFull[i];
                    string tooltip;
                    if ( GameSettings.Current.DisabledMusicTracks.Contains( musicTrack.InternalName ) )
                        tooltip = "此曲目已禁用";
                    else
                        tooltip = "此曲目已启用";

                    CustomPopupData option = CustomPopupData.GetFromPoolOrCreate();
                    option.InternalName = musicTrack.InternalName;
                    option.DisplayName = musicTrack.DisplayName;
                    option.Tooltiptext = tooltip;

                    if ( musicTrack.DisplayName == ArcenMusicPlayer.Instance.GetCurrentlyPlayingMusicTrack() )
                    {
                        option.CanBeSelected = false;
                        option.CannotBeSelectedReason = "此曲目正在播放。";
                    }

                    if ( GameSettings.Current.DisabledMusicTracks.Contains( musicTrack.InternalName )){
                        disabledMusicTrackList.Add( option );
                    }
                    else
                        musicTrackList.Add( option );
                    
                }
                if (musicTrackList.Count > 1 )
                {
                    musicTrackList.Sort( static delegate ( CustomPopupData Left, CustomPopupData Right )
                    {
                        int val = Left.CanBeSelected.CompareTo( Right.CanBeSelected ); //asc
                        if ( val != 0 )
                            return val;
                        return Left.DisplayName.CompareTo( Right.DisplayName );
                    } );
                }
                if ( disabledMusicTrackList.Count > 1 )
                {
                    disabledMusicTrackList.Sort( static delegate ( CustomPopupData Left, CustomPopupData Right )
                    {
                        int val = Left.CanBeSelected.CompareTo( Right.CanBeSelected ); //asc
                        if ( val != 0 )
                            return val;
                        return Left.DisplayName.CompareTo( Right.DisplayName );
                    } );
                }
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( MusicTypeData.currentMusicTypeData == null || MusicTypeData.currentMusicTypeData.MusicListWorking.Count == 0 )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null,
                        "没有播放音乐！", "当前没有播放音乐——可能音乐在设置中被关闭了。", "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                this.updateLists();
                MusicTrack currentTrack = MusicTypeData.currentMusicTypeData.MusicListWorking[0];

                if ( musicTrackList.Count > 1 )
                {
                    Window_PopupScrollingTwoColumnButtonList.Instance.Open( "禁用音乐曲目", "启用的曲目", "禁用的曲目", null, musicTrackList, disabledMusicTrackList,
                    delegate ( CustomPopupData selectedTrack )
                    {
                        if ( selectedTrack == null )
                            return;

                        MusicTrack newMusicTrack = MusicTrackTable.Instance.GetRowByNameOrNullIfNotFound( selectedTrack.InternalName );
                        if ( newMusicTrack != currentTrack && newMusicTrack != null )
                        {
                            if ( MusicTypeData.currentMusicTypeData.MusicListWorking.Contains( newMusicTrack ) )
                            {
                                MusicTypeData.currentMusicTypeData.MusicListDisabled.Add( newMusicTrack );
                                GameSettings.Current.DisabledMusicTracks.Add( newMusicTrack.InternalName );
                                MusicTypeData.currentMusicTypeData.MusicListWorking.Remove( newMusicTrack );
                            }
                            else if ( MusicTypeData.currentMusicTypeData.MusicListDisabled.Contains( newMusicTrack ) )
                            {
                                MusicTypeData.currentMusicTypeData.MusicListWorking.Add( newMusicTrack );
                                MusicTypeData.currentMusicTypeData.MusicListDisabled.Remove( newMusicTrack );
                                GameSettings.Current.DisabledMusicTracks.Remove( newMusicTrack.InternalName );
                                
                            }
                            else
                                ArcenDebugging.ArcenDebugLogSingleLine( "Music track isn't listed in either group!", Verbosity.ShowAsError );
                        }
                        this.updateLists();
                    }, null );
                }
                else
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null,
                        "没有其他音乐！", "没有其他可播放的音乐曲目！请检查音乐文件是否存在于你的电脑上。", "确定" );

                return MouseHandlingResult.None;
            }
            
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "列出所有音乐曲目并允许你逐个禁用曲目。如果你想完全关闭音乐，请进入个人设置 -> 音频！" );
            }
        }

        public class bListAllThreads : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                Buffer.Add( "记录所有线程" );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "正在记录所有当前线程...", Verbosity.Chat );
                try
                {
                    ArcenThreading.ReportStacksOfAllThreads();
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( e );
                }
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "游戏莫名其妙地卡住了吗？这将转储所有当前正在运行的线程列表，以及它们运行了多长时间。如果有某个不应该运行太久的线程运行了很长时间，这是一种检测方法。结果将出现在你的 ArcenDebugLog.txt 文件中。按一次按钮，等待片刻再按一次可能会有帮助。" );
            }
        }

        public class bAbortAllThreads : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                Buffer.Add( "终止所有线程" );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "正在终止所有当前线程...", Verbosity.Chat );
                try
                {
                    ArcenThreading.AbortAllThreads();
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( e );
                }
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "游戏莫名其妙地卡住了吗？这将终止游戏启动的所有线程，这可能会损坏游戏或使其崩溃。但是，它将为我们提供堆栈跟踪（代码中的位置），确切地显示游戏卡在哪里。因此这对我们要说非常有用，你可以在 ArcenDebuggingLog.txt 中找到结果。游戏可能会崩溃，也可能会继续正常运行，但无论哪种方式我们都需要日志。如果它恢复正常运行，那么保存你的游戏、退出并从操作系统重新启动游戏可能是个好主意。" );
            }
        }

        public class bToggleTracingMenu : StackWindowOpeningButtonController
        {
            protected override StackMenuWindowController RelatedController { get { return Window_InGameTracingMenu.Instance; } }
        }

        public class bDebugSettings : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                Buffer.Add( "调试设置" );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_SettingsMenu.Instance.Open();
                Window_SettingsMenu.Instance.CurrentCategory = ArcenSettingCategoryTable.Instance.GetRowByNameOrNullIfNotFound("Debug");
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "打开调试设置。" );
            }
        }
        public class bForceSync : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                    Buffer.Add( "请求主机完整同步" );
                else
                    Buffer.Add( "强制多人同步" );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                {
                    Engine_Universal.WriteToLocalMomentaryDisplayLog( "正在请求主机完整同步...", null );
                    AIWar2NetworkSync.Client_RequestFullSyncFromHost();
                }
                else if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Host )
                {
                    Engine_Universal.WriteToLocalMomentaryDisplayLog( "正在强制多人同步", null );
                    foreach ( ArcenNetworkClientConnection client in ArcenNetworkAuthority.ClientConnections )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "向客户端索引 " + client.ConnectionIndex + " 发送世界数据。", Verbosity.DoNotShow );
                        string errorText;
                        if ( !ArcenNetworkAuthority.SendTheWorldFromTheHostToClients( (int)client.ConnectionIndex, out errorText ) )
                            ArcenDebugging.ArcenDebugLog( "多人错误: " + errorText, Verbosity.ShowAsError );
                    }
                }
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                    Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "请求主机向你发送完整的世界同步。你的视角将短暂重置到银河地图。" );
                else
                    Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "强制将多人数据同步到所有客户端。仅在主机上有效。" );
            }
        }

        public class bOpenLog : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                Buffer.Add( "打开日志" );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                System.Diagnostics.Process.Start( Engine_Universal.CurrentPlayerDataDirectory + "ArcenDebugLog.txt" );

                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "打开包含所有游戏日志的文本文件。" );
            }
        }
    }
}
