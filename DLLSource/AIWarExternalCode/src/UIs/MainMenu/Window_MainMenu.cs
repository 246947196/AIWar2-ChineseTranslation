using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.IO;
using System.Diagnostics;
using UnityEngine;
using JetBrains.Annotations;

namespace Arcen.AIW2.External
{
    public class Window_MainMenu : WindowControllerAbstractBase
    {
        public enum ModeDrawn
        {
            AllClosed = 0,
            SinglePlayer,
            Multiplayer,
            More
        }

        public static Window_MainMenu Instance;
        public float LastStartGameTime = ArcenTime.TimeSinceStartF;
        public ModeDrawn currentModeDrawn = ModeDrawn.AllClosed;

        public Window_MainMenu()
        {
            Instance = this;
        }

        public class customParent : CustomUIAbstractBase
        {
            private GameObject spaceRotationOfMainScene = null;
            private ArchanorAssets.BetterRotationScript spaceRotator = null;

            private GameObject otherSceneBitsParent = null;
            private Light[] lightsInOtherSceneBits = null;
            private readonly List<Light> pointLights = List<Light>.Create_WillNeverBeGCed( 300, "Window_MainMenu-customParent-pointLights" );
            private ReflectionProbe[] reflectionProbes = null;
            //private GameObject cutsceneGroup = null;

            private bool hasInitialized = false;
            public override void OnUpdate()
            {
                if ( !hasInitialized )
                {
                    hasInitialized = true;
                    DisableCanvasByName( "AbsoluteResolution" ); //SLATE Director canvas 1
                    DisableCanvasByName( "ReferenceResolution" ); //SLATE Director canvas 2
                    DisableCanvasByName( "LogoCanvas" ); //AI War and Arcen logo during initial load
                }
                if ( PlayerProfile.Local == null || !PlayerProfile.Local.WasCreatedByPlayer )
                {
                    if ( bViewProfile.Instance != null )
                        bViewProfile.Instance.HandleClick( new MouseHandlingInput() );
                }

                //make sure no planet cache carries over!
                World_AIW2.Instance.Setup.MapConfig.ClearAnyCache();

                #region Adjust The Speed Of The Background Scene Rotation
                if ( spaceRotationOfMainScene == null )
                    spaceRotationOfMainScene = GameObject.Find( "/ModalMenuCamera/MainMenuCutscene/Loc_03_Space" );
                if ( spaceRotationOfMainScene != null && spaceRotator == null )
                {
                    spaceRotator = spaceRotationOfMainScene.GetComponent<ArchanorAssets.BetterRotationScript>();
                    if ( spaceRotator != null )
                    {
                        Vector3 vec = spaceRotator.rotateVector;
                        vec.x = Engine_Universal.PermanentQualityRandom.NextFloat( 0.1f, 1.1f ) * ( Engine_Universal.PermanentQualityRandom.NextBool() ? -1 : 1 );
                        vec.y = Engine_Universal.PermanentQualityRandom.NextFloat( 0.1f, 1.1f ) * (Engine_Universal.PermanentQualityRandom.NextBool() ? -1 : 1);
                        vec.z = Engine_Universal.PermanentQualityRandom.NextFloat( 0.1f, 1.1f ) * (Engine_Universal.PermanentQualityRandom.NextBool() ? -1 : 1);
                        spaceRotator.rotateVector = vec;
                        //ArcenDebugging.ArcenDebugLogSingleLine( "Set rotation to " + vec, Verbosity.DoNotShow );
                    }
                }
                #endregion

                #region Lights On The Main Menu Adjustment
                if ( otherSceneBitsParent == null )
                    otherSceneBitsParent = GameObject.Find( "/ModalMenuCamera/MainMenuCutscene/OtherSceneBits" );
                if ( otherSceneBitsParent != null && lightsInOtherSceneBits == null )
                {
                    // Bit shift the index of the layer (26) to get a bit mask
                    int layerMaskOfScene = 1 << 26; //"Scene" layer

                    lightsInOtherSceneBits = otherSceneBitsParent.GetComponentsInChildren<Light>( true );
                    foreach ( Light lgt in lightsInOtherSceneBits )
                    {
                        //for every light in the scene, start off by making sure that they are only affecting the proper layers
                        lgt.cullingMask = layerMaskOfScene;
                        lgt.shadows = LightShadows.None;
                        lgt.bounceIntensity = 0;
                        //there is only one spot light, and we should disable it
                        if ( lgt.type == LightType.Spot )
                            lgt.enabled = false;
                        if ( lgt.type == LightType.Point )
                        {
                            pointLights.Add( lgt );
                            //lgt.enabled = false; //this adds only 2fps on my super old mac, from 31 to 33fps, so that's pointless.
                        }
                    }

                    reflectionProbes = otherSceneBitsParent.GetComponentsInChildren<ReflectionProbe>( true );
                    //foreach ( var probe in reflectionProbes )
                    //{
                    //    //probe.enabled = false; //this gets no extra performance boost on my super old mac
                    //}

                    //if ( cutsceneGroup == null )
                    //    cutsceneGroup = GameObject.Find( "/ModalMenuCamera/MainMenuCutscene/OtherSceneBits/Cutscenes" );
                    //else
                    //    cutsceneGroup.SetActive( false );
                }
                #endregion

                if ( InputActionTypeDataTable.IsInitialized )
                    InputCaching.UpdateInputCacheIfNeeded();
            }

            private void DisableCanvasByName( string SearchName )
            {
                GameObject obj = GameObject.Find( SearchName );
                if ( !obj )
                    return;
                //make sure the canvas to copy isn't using extra resources
                Canvas canv = obj.GetComponent<Canvas>();
                if ( canv )
                    canv.enabled = false;
                UnityEngine.UI.GraphicRaycaster raycaster = obj.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if ( raycaster )
                    raycaster.enabled = false;
            }
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( World.Instance != null && World.Instance.IsLoaded )
                return false;
            if ( World_AIW2.Instance != null && World_AIW2.Instance.InSetupPhase )
                return false;
            if ( Window_SettingsMenu.Instance != null && Window_SettingsMenu.Instance.IsOpen )
                return false;
            if ( Window_ControlBindingsMenu.Instance != null && Window_ControlBindingsMenu.Instance.IsOpen )
                return false;
            if ( Window_JoinMultiplayerGameByIPMenu.Instance != null && Window_JoinMultiplayerGameByIPMenu.Instance.IsOpen )
                return false;
            if ( Window_JoinMultiplayerGameByListMenu.Instance != null && Window_JoinMultiplayerGameByListMenu.Instance.GetIsOpen() )
                return false;
            if ( Window_Credits.Instance != null && Window_Credits.Instance.IsOpen )
                return false;
            if ( Window_CreditsKickstarter.Instance != null && Window_CreditsKickstarter.Instance.IsOpen )
                return false;
            if ( Window_BackgroundStory.Instance != null && Window_BackgroundStory.Instance.IsOpen)
                return false;
            if ( Window_AddEditProfileWindow.Instance != null && Window_AddEditProfileWindow.Instance.IsOpen )
                return false;
            if ( Window_ProfileList.Instance != null && Window_ProfileList.Instance.IsOpen )
                return false;            
            return true;
        }

        public class dpSinglePlayer : DisappearingPanelAbstractBase
        {
            public override bool GetShouldShowThisFrameFromMainThread()
            {
                if ( Window_MainMenu.Instance == null )
                    return false;
                return Window_MainMenu.Instance.currentModeDrawn == ModeDrawn.SinglePlayer;
            }
        }

        public class dpMultiplayer : DisappearingPanelAbstractBase
        {
            public override bool GetShouldShowThisFrameFromMainThread()
            {
                if ( Window_MainMenu.Instance == null )
                    return false;
                return Window_MainMenu.Instance.currentModeDrawn == ModeDrawn.Multiplayer;
            }
        }

        public class dpMore : DisappearingPanelAbstractBase
        {
            public override bool GetShouldShowThisFrameFromMainThread()
            {
                if ( Window_MainMenu.Instance == null )
                    return false;
                return Window_MainMenu.Instance.currentModeDrawn == ModeDrawn.More;
            }
        }

        public class dpTipsSubwindow : DisappearingPanelAbstractBase
        {
            public override bool GetShouldShowThisFrameFromMainThread()
            {
                if ( Window_MainMenu.Instance == null )
                    return false;
                return Window_MainMenu.Instance.currentModeDrawn != ModeDrawn.More;
            }
        }

        public class bToggleModeSinglePlayer : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( Window_MainMenu.Instance.currentModeDrawn == ModeDrawn.SinglePlayer )
                    Window_MainMenu.Instance.currentModeDrawn = ModeDrawn.AllClosed;
                else
                {
                    if ( PlayerProfile_AIW2.Local != null && !GameSettings.Current.GetBoolBySetting( "HasBeenShownTutorialPrompt" ) )
                    {
                        GameSettings.Current.SetBoolBySetting( "HasBeenShownTutorialPrompt", true );
                        GameSettings.SaveToDisk();

                        ModalPopupData.CreateAndLogYesNoStyle( delegate
                        {
                            ArcenNetworkAuthority.HandleNetworkBeforeGameStartup( DesiredMultiplayerStatus.SinglePlayerOnly );
                            Window_LoadTutorialsMenu.Instance.Open();
                            Window_MainMenu.Instance.currentModeDrawn = ModeDrawn.AllClosed;
                        },
                        delegate
                        {
                            Window_MainMenu.Instance.currentModeDrawn = ModeDrawn.SinglePlayer;
                        }, 
                        "查看教程吗？",
                            "你好！游戏内有维基百科（主菜单和游戏内ESC菜单上的<color=#ffcd36>游戏指南</color>）以及可玩的教程（主菜单右侧的<color=#ffcd36>教程</color>）。\n\n如果你是新手，先做教程或查看游戏维基基础知识会让你有更好的体验。\n\n你想查看教程列表吗？我们只问一次，但你可以随时访问。", "查看教程", "不，现在就玩" );
                    }
                    else
                        Window_MainMenu.Instance.currentModeDrawn = ModeDrawn.SinglePlayer;
                }
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bToggleModeMultiPlayer : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( Window_MainMenu.Instance.currentModeDrawn == ModeDrawn.Multiplayer )
                    Window_MainMenu.Instance.currentModeDrawn = ModeDrawn.AllClosed;
                else
                {
                    Window_MainMenu.Instance.currentModeDrawn = ModeDrawn.Multiplayer;
                    //If No Network Type Set, Have It Set One
                    EnsureNetworkTypeSetToSensibleValue();
                }

                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() {}
            public override void OnUpdate() { }

            #region  EnsureNetworkTypeSetToSensibleValue
            public static void EnsureNetworkTypeSetToSensibleValue()
            {
                string lastNetworkFramework = GameSettings.Current.GetStringBySetting( "LastChosenNetworkType" );
                if ( NetworkingFrameworkTable.Instance.GetRowByNameOrNullIfNotFound( lastNetworkFramework ) == null )
                {
                    if ( ArcenSteamWrapper.Instance.SteamClientIsValid )
                    {
                        NetworkingFramework frame = NetworkingFrameworkTable.Instance.FindFirstFrameworkOfType_OrNull( SpecialNetworkType.SteamMultiSocketsRelay );
                        if ( frame != null )
                        {
                            GameSettings.Current.SetStringBySetting( "LastChosenNetworkType", frame.InternalName );
                            return;
                        }
                        frame = NetworkingFrameworkTable.Instance.FindFirstFrameworkOfType_OrNull( SpecialNetworkType.SteamMultiSocketsDirect );
                        if ( frame != null )
                        {
                            GameSettings.Current.SetStringBySetting( "LastChosenNetworkType", frame.InternalName );
                            return;
                        }
                    }
                    if ( ArcenGOGWrapper.Instance.GOGClientIsValid )
                    {
                        NetworkingFramework frame = NetworkingFrameworkTable.Instance.FindFirstFrameworkOfType_OrNull( SpecialNetworkType.GOG );
                        if ( frame != null )
                        {
                            GameSettings.Current.SetStringBySetting( "LastChosenNetworkType", frame.InternalName );
                            return;
                        }
                    }
                    {
                        NetworkingFramework frame = NetworkingFrameworkTable.Instance.FindFirstFrameworkOfType_OrNull( SpecialNetworkType.None );
                        if ( frame != null )
                            GameSettings.Current.SetStringBySetting( "LastChosenNetworkType", frame.InternalName );
                    }
                }
            }
            #endregion
        }

        public class bToggleModeMore : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( Window_MainMenu.Instance.currentModeDrawn == ModeDrawn.More )
                    Window_MainMenu.Instance.currentModeDrawn = ModeDrawn.AllClosed;
                else
                    Window_MainMenu.Instance.currentModeDrawn = ModeDrawn.More;
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bCustomStartGame : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( EndpointFunctions.GetShouldButtonClickFailBasedOnGameStatusCycling() )
                    return MouseHandlingResult.PlayClickDeniedSound;
                return bCustomStartGame.StartCustomGame( true );
            }
            public override void HandleMouseover() 
            {
                if ( EndpointFunctions.DoDisabledButtonBasedOnGameStatusCyclingIfNeeded( this.Element ) )
                    return;

            }
            public override void OnUpdate() { }

            public static MouseHandlingResult StartCustomGame( bool StartInSinglePlayer )
            {
                if ( (ArcenTime.TimeSinceStartF - Instance.LastStartGameTime) < 1f )
                    return MouseHandlingResult.None;
                if ( World.Instance.IsLoaded )
                    return MouseHandlingResult.None;

                Engine_AIW2.Instance.CurrentlyFocusedFactionInLobby = null;
                ArcenNetworkAuthority.HandleNetworkBeforeGameStartup( StartInSinglePlayer ? DesiredMultiplayerStatus.SinglePlayerOnly : DesiredMultiplayerStatus.Host );
                ArcenSettingTable.Instance.CopyCurrentValuesToTemp();

                string saveDirectoryPath = Engine_Universal.CurrentPlayerDataDirectory + "Save/_Internal/";
                string saveFilename = saveDirectoryPath + "LastLobbySettings.save";

                Action<bool> PostAction = (bool success)=> { if (!success) EndpointFunctions.SetDefaultsForLobby(); };
                
                if ( !File.Exists( saveFilename ) )
                    EndpointFunctions.SetDefaultsForLobby();
                else
                    Engine_AIW2.LoadGameNoCampaignNameSet( saveFilename, StartWorldSource1.StartingTheLobbyFromPrior, StartWorldSource2.LoadingLastLobbySettings, true, PostAction );
                
                Instance.LastStartGameTime = ArcenTime.TimeSinceStartF;
                
                return MouseHandlingResult.None;
            }
        }

        public class bLoadGame : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( EndpointFunctions.GetShouldButtonClickFailBasedOnGameStatusCycling() )
                    return MouseHandlingResult.PlayClickDeniedSound;
                ArcenNetworkAuthority.HandleNetworkBeforeGameStartup( DesiredMultiplayerStatus.SinglePlayerOnly );
                Window_LoadGameMenu.Instance.Open( false );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() 
            {
                if ( EndpointFunctions.DoDisabledButtonBasedOnGameStatusCyclingIfNeeded( this.Element ) )
                    return;
            }
            public override void OnUpdate() { }
        }

        public class bSettings : WindowTogglingButtonController
        {
            public static bSettings Instance;
            public bSettings() : base( "设置", ">" ) { Instance = this; }
            public override ToggleableWindowController GetRelatedController() { return Window_SettingsMenu.Instance; }
        }

        public class bControls : WindowTogglingButtonController
        {
            public static bControls Instance;
            public bControls() : base( "控制", ">" ) { Instance = this; }
            public override ToggleableWindowController GetRelatedController() { return Window_ControlBindingsMenu.Instance; }
        }

        public class bReportBug : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Process.Start( "https://bugtracker.arcengames.com" );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() {  }
            public override void OnUpdate() { }
        }

        public class bContinueGame : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( EndpointFunctions.GetShouldButtonClickFailBasedOnGameStatusCycling() )
                    return MouseHandlingResult.PlayClickDeniedSound;
                
                string campaignName = GameSettings.Current.LastSavegameCampaign;
                
                if ( string.IsNullOrEmpty(campaignName) )
                {
                    LOG.Err("Could not load most recent save, because the campaign name is empty!");
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                ArcenNetworkAuthority.HandleNetworkBeforeGameStartup( DesiredMultiplayerStatus.SinglePlayerOnly );
                
                SFXItemTable.TryPlayItemByName_GUIOnly( "ButtonStartGame", null );
                
                var path = Engine_Universal.CurrentPlayerDataDirectory + "Save/" + campaignName + "/" + GameSettings.Current.LastSavegameFile;
                string fullSaveName = path + Engine_Universal.SaveMainExtension;
                string metaDataSaveName = path + Engine_Universal.SaveMetadataExtension;

                if ( !File.Exists( fullSaveName ) )
                {
                    GameSettings.Current.LastSavegameCampaign = null;
                    GameSettings.Current.LastSavegameFile = null;
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "存档不存在！", "此存档似乎不存在。请验证文件，然后从载入游戏菜单载入。", "确定" );
                    
                    return MouseHandlingResult.None;
                }
                
                Action<bool> PostAction = 
                    (bool success)=> 
                    { 
                        if (!success) 
                            return;
                        
                        World.Instance.CampaignName = campaignName;

                        var save = SaveGameData.Load(fullSaveName);
                        save.numTimesLoaded++;
                        save.SaveMetaData();
                    };
                    
                Engine_AIW2.LoadGameNoCampaignNameSet( fullSaveName, StartWorldSource1.AnythingElse, StartWorldSource2.LoadingSaveGame, false, PostAction );
                
                return MouseHandlingResult.None;
            }
            
            public override void HandleMouseover()
            {
                if ( EndpointFunctions.DoDisabledButtonBasedOnGameStatusCyclingIfNeeded( this.Element ) )
                    return;
                
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( 
                    this.Element, 
                    "上次战役：" + GameSettings.Current.LastSavegameCampaign +
                    "\n上次存档：" + GameSettings.Current.LastSavegameFile );
            }
            
            public override void OnUpdate() { }

            public override bool GetShouldBeHidden()
            {
                if ( GameSettings.Current == null )
                    return false;
                return ArcenStrings.IsEmpty( GameSettings.Current.LastSavegameCampaign ) ||
                    ArcenStrings.IsEmpty( GameSettings.Current.LastSavegameFile );
            }
        }

        public class bQuickStart : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( EndpointFunctions.GetShouldButtonClickFailBasedOnGameStatusCycling() )
                    return MouseHandlingResult.PlayClickDeniedSound;
                ArcenNetworkAuthority.HandleNetworkBeforeGameStartup( DesiredMultiplayerStatus.SinglePlayerOnly );
                Engine_AIW2.Instance.CurrentlyFocusedFactionInLobby = null;
                Window_LoadQuickStartMenu.Instance.Open();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                if ( EndpointFunctions.DoDisabledButtonBasedOnGameStatusCyclingIfNeeded( this.Element ) )
                    return;
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "选择预制场景，而不是手动进行战役设置。" );
            }
            public override void OnUpdate() { }
        }

        public class bConnectToServer : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( EndpointFunctions.GetShouldButtonClickFailBasedOnGameStatusCycling() )
                    return MouseHandlingResult.PlayClickDeniedSound;
                ArcenNetworkAuthority.HandleNetworkBeforeGameStartup( DesiredMultiplayerStatus.Client );
                NetworkingFramework frame = NetworkingFrameworkTable.Instance.GetRowByNameOrNullIfNotFound( GameSettings.Current.GetStringBySetting( "LastChosenNetworkType" ) );
                Engine_AIW2.Instance.CurrentlyFocusedFactionInLobby = null;
                if ( frame == null )
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "无法连接", "尚未选择网络框架。", "确定" );
                else if ( frame.SocketImplementation.AreConnectionsByIPAddress() )
                    Window_JoinMultiplayerGameByIPMenu.Instance.Open();
                else
                    Window_JoinMultiplayerGameByListMenu.Instance.Open();
                
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                if ( EndpointFunctions.DoDisabledButtonBasedOnGameStatusCyclingIfNeeded( this.Element ) )
                    return;
                NetworkingFramework frame = NetworkingFrameworkTable.Instance.GetRowByNameOrNullIfNotFound( GameSettings.Current.GetStringBySetting( "LastChosenNetworkType" ) );
                if ( frame == null )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "尚未选择网络框架。" );
                else
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, frame.ClientConnectTooltip );
            }
            public override void OnUpdate() { }
        }

        public class bHostSavedGame : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( EndpointFunctions.GetShouldButtonClickFailBasedOnGameStatusCycling() )
                    return MouseHandlingResult.PlayClickDeniedSound;
                ArcenNetworkAuthority.HandleNetworkBeforeGameStartup( DesiredMultiplayerStatus.Host );
                Engine_AIW2.Instance.CurrentlyFocusedFactionInLobby = null;
                Window_LoadGameMenu.Instance.Open( true );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                if ( EndpointFunctions.DoDisabledButtonBasedOnGameStatusCyclingIfNeeded( this.Element ) )
                    return;
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "其他玩家可以加入游戏，条件是游戏中有一个与其配置文件名称匹配的玩家槽位。\n\n其他玩家可以在你将存档载入大厅（作为新游戏的起点）或直接载入存档后连接。在你还在下一个菜单时，他们无法连接。" );
            }
            public override void OnUpdate() { }
        }

        public class bHostCustomStart : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( EndpointFunctions.GetShouldButtonClickFailBasedOnGameStatusCycling() )
                    return MouseHandlingResult.PlayClickDeniedSound;
                Engine_AIW2.Instance.CurrentlyFocusedFactionInLobby = null;
                return bCustomStartGame.StartCustomGame( false );
            }
            public override void HandleMouseover()
            {
                if ( EndpointFunctions.DoDisabledButtonBasedOnGameStatusCyclingIfNeeded( this.Element ) )
                    return;
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "其他玩家可以加入大厅并与你一起进行设置。\n\n其他玩家可以在你进入下一个屏幕后立即连接。" );
            }
            public override void OnUpdate() { }
        }

        public class bHostQuickStart : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( EndpointFunctions.GetShouldButtonClickFailBasedOnGameStatusCycling() )
                    return MouseHandlingResult.PlayClickDeniedSound;
                ArcenNetworkAuthority.HandleNetworkBeforeGameStartup( DesiredMultiplayerStatus.Host );
                Engine_AIW2.Instance.CurrentlyFocusedFactionInLobby = null;
                Window_LoadQuickStartMenu.Instance.Open();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                if ( EndpointFunctions.DoDisabledButtonBasedOnGameStatusCyclingIfNeeded( this.Element ) )
                    return;
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "选择预制场景，而不是手动进行战役设置。\n\n警告：大多数快速开始默认只有一个玩家槽位。要进行多人游戏，只需在你的选择上点击'在大厅中编辑'，让其他玩家在你开始前连接。\n\n其他玩家可以在你将快速开始载入大厅或完全载入游戏后连接。在你还在下一个菜单时，他们无法连接。" );
            }
            public override void OnUpdate() { }
        }

        public class bAboutMultiplayer : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "多人游戏问题？维基有答案！" );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Process.Start( "https://wiki.arcengames.com/index.php?title=AI_War_2:Multiplayer_Alpha_And_Beta#What_Does_Multiplayer_Alpha_Mean.3F" );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() 
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "点击打开我们的维基页面，其中解释了各种多人游戏主题。这不是必须的，但如果你有问题，这是一个有用的资源。" );
            }
            public override void OnUpdate() { }
        }

        public class bChooseNetwork : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "<size=75%>" );
                if ( ArcenTime.IsOn_EveryHalfSecondToggle )
                    Buffer.StartColor( "ffcd36" );
                else
                    Buffer.StartColor( "ffba36" );

                Buffer.Add( "网络：" );
                NetworkingFramework frame = NetworkingFrameworkTable.Instance.GetRowByNameOrNullIfNotFound( GameSettings.Current.GetStringBySetting( "LastChosenNetworkType" ) );
                if ( frame == null )
                    Buffer.Add( "???" );
                else
                    Buffer.Add( frame.Abbreviation );
            }

            private static ProtectedList<CustomPopupData> networkOptions = ProtectedList<CustomPopupData>.Create_WillNeverBeGCed( 30, "Window_MainMenu-bChooseNetwork-networkOptions" );
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                networkOptions.Clear( true );
                #region Fill networkOptions
                for ( int i = 0; i < NetworkingFrameworkTable.Instance.Rows.Count; i++ )
                {
                    NetworkingFramework row = NetworkingFrameworkTable.Instance.Rows[i];
                    if ( row.SpecialType == SpecialNetworkType.NullHandler )
                        continue;

                    CustomPopupData option = CustomPopupData.GetFromPoolOrCreate();
                    option.InternalName = row.InternalName;
                    option.DisplayName = row.DisplayName;
                    option.CanBeSelected = row.SocketImplementation.GetIsAvailableOnThisSystemForMultiplayer();
                    option.CannotBeSelectedReason = row.SocketImplementation.GetExpalantionForNotAvailableOnThisSystemForMultiplayer();
                    option.Tooltiptext = row.Description;
                    networkOptions.Add( option );
                }
                #endregion

                //networkOptions.Sort( static delegate ( RefThreeTuple<string, string, string> Left, RefThreeTuple<string, string, string> Right )
                //{
                //    return Left.SecondItem.CompareTo( Right.SecondItem );
                //} );

                Window_PopupScrollingColumnButtonList.Instance.Open( "选择网络框架", null, networkOptions,
                    delegate ( CustomPopupData Option )
                    {
                        if ( Option == null || !Option.CanBeSelected )
                            return;
                        GameSettings.Current.SetStringBySetting( "LastChosenNetworkType", Option.InternalName );
                        GameSettings.SaveToDisk();
                    }, null );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                NetworkingFramework frame = NetworkingFrameworkTable.Instance.GetRowByNameOrNullIfNotFound( GameSettings.Current.GetStringBySetting( "LastChosenNetworkType" ) );
                string tooltip = string.Empty;
                if ( frame == null )
                    tooltip = "尚未选择网络框架。\n\n";
                else
                    tooltip = frame.DisplayName + "\n" + frame.Description + "\n\n";

                tooltip += "<color=#999999>本游戏支持多种底层网络连接方式。也支持模组作者添加更多类型的网络连接，如果由于某些原因有此需求。\n\n游戏本身在所有模式下工作方式相同，但你与一起玩的人如何连接，以及数据如何在你们之间来回传递，可以根据你偏好的服务或网络状况以几种不同的方式进行。";

                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, tooltip ); 
            }
            public override void OnUpdate() { }
        }

        public class bChangeProfile : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_ProfileList.Instance.Open();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "切换要使用的配置文件，或编辑或创建新配置文件。连接多人游戏时，你的配置文件名称必须与游戏中玩家槽位的名称匹配。" ); }
            public override void OnUpdate() { }
        }

        public class bViewProfile : WindowTogglingButtonController
        {
            public static bViewProfile Instance;
            public bViewProfile() : base( "AddEditProfile", ">" ) { Instance = this; }
            public override ToggleableWindowController GetRelatedController() { return Window_ProfileList.Instance; }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( PlayerProfile.Local == null ? "???" : PlayerProfile.Local.DisplayName );
            }
            public override void HandleMouseover() { Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "切换要使用的配置文件，或编辑或创建新配置文件。连接多人游戏时，你的配置文件名称必须与游戏中玩家槽位的名称匹配。" ); }

        }

        public class bViewCredits : WindowTogglingButtonController
        {
            public static bViewCredits Instance;
            public bViewCredits() : base( "开发人员致谢", ">" ) { Instance = this; }
            public override ToggleableWindowController GetRelatedController() { return Window_Credits.Instance; }
        }

        public class bViewCreditsKickstarter : WindowTogglingButtonController
        {
            public static bViewCreditsKickstarter Instance;
            public bViewCreditsKickstarter() : base( "众筹致谢", ">" ) { Instance = this; }
            public override ToggleableWindowController GetRelatedController() { return Window_CreditsKickstarter.Instance; }
        }

        public class bViewBackgroundStory : WindowTogglingButtonController
        {
            public static bViewBackgroundStory Instance;
            public bViewBackgroundStory() : base("背景故事", ">") { Instance = this; }
            public override ToggleableWindowController GetRelatedController() { return Window_BackgroundStory.Instance; }
        }
        
        public class bViewForum : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Process.Start( "https://forums.arcengames.com/ai-war-ii/" );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class bViewDiscord : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Process.Start( "https://discord.com/invite/5g9ETKn" );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() 
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element,
                    "Discord已经成为本游戏社区的主要聚集地，也是分享或寻求技巧、胜利、困境等的好地方。有时候，如果你有一个看似无法获胜的特别困难的存档，其他人会很感兴趣去玩它，看看能否找到获胜的方法。我们的旧论坛仍然存在，但大多作为过时的媒介而尘封。" );
            }
            public override void OnUpdate() { }
        }

        public class bViewWiki : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Process.Start( "https://wiki.arcengames.com/index.php?title=AI_War_2:AI_War_2" );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() {  }
            public override void OnUpdate() { }
        }

        public class bMailingList : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Process.Start( "http://arcengames.us14.list-manage.com/subscribe?u=0f2f4d49ecd5967ae28acf0bf&id=468c8ac0f4" );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "我们只偶尔发送关于重大产品公告的邮件。我们不想打扰你！\n<color=#999999>\n请注意，如果你不注册我们的邮件列表，我们将无法联系你，所以如果你不想收到邮件，请务必定期查看我们的网站。" ); }
            public override void OnUpdate() { }
        }

        public class bOpenReleaseNotes : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Process.Start( "https://wiki.arcengames.com/index.php?title=AI_War_2:AI_War_2#Release_History" );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { /* Window_AtMouseTooltipPanel.bPanel.Instance.SetText( "Our release notes are long and detailed.  Blahahahaso asldkksdkf sdk fkjsdfnkjsdnfkjdsnfkjsd nkjd kjxcn ;kn;kcn kjxf nkjfdnkjdf gjkdf kjfd jkdf gjkdf mnds gdfgjk dkj" );*/ }
            public override void OnUpdate() { }
        }

        public class bExitApplication : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Engine_Universal.ForceClose( false );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public class tVersion : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "v " ).Add( GameVersionTable.Instance.CurrentVersion.GetAsString() ).Add( ", 最后更新 " ).Add( GameVersionTable.Instance.CurrentVersion.ReleaseDateText );
            }

            public override void OnUpdate() { }
        }

        public class tSteamUser : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( FrontEndBaseLink.Instance == null )
                    return;

                string userNameGOG = ArcenGOGWrapper.Instance.GOGUsername;

                if ( Engine_Universal.IsSteamVersionOfGame )
                {
                    string userNameSteam = ArcenSteamWrapper.Instance.GetSteamUsername();
                    if ( userNameSteam != null && userNameSteam.Length > 0 )
                        Buffer.Add( "已登录Steam用户：" ).Add( userNameSteam );
                    else if ( userNameGOG != null && userNameGOG.Length > 0 )
                        Buffer.Add( "已登录GOG用户：" ).Add( userNameGOG );
                    else
                        Buffer.Add( "未登录Steam。" );
                }
                else
                {
                    if ( userNameGOG != null && userNameGOG.Length > 0 )
                        Buffer.Add( "已登录GOG用户：" ).Add( userNameGOG );
                }
            }

            public override void OnUpdate() { }
        }

        public class bLoadTime : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "载入：" ).Add( Engine_Universal.SecondsToLoad ).Add( "秒" );
                Buffer.Add( " 帧率：" ).Add( (int)ArcenFramerateTracker.CurrentFramesPerSecond );
            }

            public override void HandleMouseover() { Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element,
                "载入时间：" + Engine_Universal.SecondsToLoad + "秒\n" + Engine_Universal.permanentInitStagesPartial.ToString() + "\n" +
                Engine_Universal.GetExpansionsString() + Engine_Universal.GetXmlModsString() ); }

            public override void OnUpdate() { }
        }

        public class bUnitEncyclopedia : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_UnitEncyclopedia.Instance.Open();
                return MouseHandlingResult.None;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "单位\n百科" );
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element,
                    "一个通用参考，你可以在这里找到游戏中每个单位的信息。任何已启用的模组也会自动显示在这里。" );
            }
            public override void OnUpdate() { }
        }

        public class bMoreTips : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_Tips.Instance.Open();
                return MouseHandlingResult.None;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "游戏指南" );
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element,
                    "从基础主题到详细策略的书面解释。" );
            }
            public override void OnUpdate() { }
        }

        public class bTutorials : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( EndpointFunctions.GetShouldButtonClickFailBasedOnGameStatusCycling() )
                    return MouseHandlingResult.PlayClickDeniedSound;
                ArcenNetworkAuthority.HandleNetworkBeforeGameStartup( DesiredMultiplayerStatus.SinglePlayerOnly );
                Engine_AIW2.Instance.CurrentlyFocusedFactionInLobby = null;
                Window_LoadTutorialsMenu.Instance.Open();
                return MouseHandlingResult.None;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( ArcenTime.IsOn_EveryHalfSecondToggle )
                    Buffer.StartColor( "ffcd36" );
                else
                    Buffer.StartColor( "ffba36" );

                Buffer.Add( "教程" );
            }

            public override void HandleMouseover()
            {
                if ( EndpointFunctions.DoDisabledButtonBasedOnGameStatusCyclingIfNeeded( this.Element ) )
                    return;
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element,
                    "可玩的场景，引导你完成游戏的一个或多个部分，让你实际操作（并且可能会输！）。" );
            }
            public override void OnUpdate() { }
        }

        public class bTutorialsSPMenu : bTutorials
        {
            //same as the other one
        }

        public class tTipHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( tTipHeaderText.currentRandomTip != null )
                    Buffer.Add( tTipHeaderText.currentRandomTip.DisplayName );
            }

            private double lastTimeWasShown = -100;
            public static Tip currentRandomTip = null;
            public override void OnUpdate()
            {
                if ( lastTimeWasShown < Engine_Universal.CumulativeUnscaledTime - 1 || currentRandomTip == null )
                {
                    if ( TipCategoryTable.Instance.MainMenuTips.Count > 0 )
                        currentRandomTip = TipCategoryTable.Instance.MainMenuTips[Engine_Universal.PermanentQualityRandom.Next( 0, TipCategoryTable.Instance.MainMenuTips.Count )];
                }
                lastTimeWasShown = Engine_Universal.CumulativeUnscaledTime;
            }
        }

        public class tTipBodyText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( tTipHeaderText.currentRandomTip != null )
                    Buffer.Add( tTipHeaderText.currentRandomTip.FullText ).Add( "\n\n" );
            }

            public override void OnUpdate() { }
        }
    }
}
