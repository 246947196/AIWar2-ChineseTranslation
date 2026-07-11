using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using System.Text;

namespace Arcen.AIW2.External
{
    public class Window_SetupFooterControls : WindowControllerAbstractBase, IInputActionHandler
    {
        public static LobbyTabType Current = LobbyTabType.Factions;

        public static Window_SetupFooterControls Instance;
        public Window_SetupFooterControls()
        {
            Instance = this;
        }

        #region GetShouldDrawThisFrame_Subclass
        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( Window_SetupTopTabs.Instance == null )
                return false;
            if ( !Window_SetupTopTabs.Instance.GetShouldDrawThisFrame_Subclass() )
                return false;
            return true;
        }
        #endregion

        public static float ScreenSpaceTop;

        #region custSetupFooterControls
        public class custSetupFooterControls : CustomUIAbstractBase
        {
            public override void OnUpdate()
            {
                ScreenSpaceTop = ArcenUI.Instance.guiCamera.WorldToScreenPoint( this.Element.RelevantRect.GetWorldSpaceTopRightCorner() ).y;
                //ArcenDebugging.ArcenDebugLogSingleLine( this.Element.RelevantRect + " rect " +  this.Element.RelevantRect.GetWorldSpaceTopRightCorner() + " top " + ScreenSpaceTop, Verbosity.DoNotShow );
            }
        }
        #endregion

        #region tCampaignText
        public class tCampaignText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "战役名称：" );
            }
            public override bool GetShouldBeHidden()
            {
                return ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client;
            }
        }
        #endregion

        #region iCampaignName
        public class iCampaignName : InputAbstractBase
        {
            //this is the box where one types in the campaign name
            public int maxCampaignLen = 20;
            public static iCampaignName Instance;
            public iCampaignName() { Instance = this; }
            public override void HandleChangeInValue( string NewValue )
            {
                World.Instance.CampaignName = NewValue;
            }
            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                if ( input.Length >= this.maxCampaignLen )
                    return '\0';
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                    return '\0';
                //use a whitelist of approved characters only
                if ( Char.IsLetterOrDigit( addedChar ) ) //must be alphanumeric
                    return addedChar;
                if ( addedChar == '_' || addedChar == ' ' )
                    return addedChar;
                //block everything except alphanumerics and _
                //for right now
                return '\0';
            }

            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                switch ( Action.InternalName )
                {
                    case "OpenSystemMenu": //escape key
                        return InputActionTextboxResult.UnfocusMe;
                    case "Return": //enter key
                        if ( btnStartGame.StartInstance != null )
                            btnStartGame.StartInstance.HandleClick_Subclass( new MouseHandlingInput() );
                        return InputActionTextboxResult.UnfocusMe;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }

            public override void OnUpdate()
            {
                ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                {
                    elementAsType.SetText( "将由主机设置" );
                }
                else
                {
                    //only update to the current value if we're not editing this field right now
                    if ( !this.GetIsCurrentlyBeingEdited() )
                    {
                        elementAsType.SetText( World.Instance.CampaignName );
                    }
                }
            }
            public override bool GetShouldBeHidden()
            {
                return ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client;
            }
        }
        #endregion

        #region btnStartGame
        public class btnStartGame : ButtonAbstractBase
        {
            public static btnStartGame StartInstance;
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                StartInstance = this;

                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    Buffer.Add( "开始游戏\n" );
                else
                    Buffer.Add( "查看加载详情\n" );

                int totalLoad = EndpointFunctions.CalculatePredictedGameLoad( null, false );
                if ( totalLoad <= 0 )
                    Buffer.Add( "<size=60%>" ).Add( "计算负载中…</size>" );
                else
                {
                    string loadColor = EndpointFunctions.CalculateLoadColorAndCategory( totalLoad, out string LoadName, out _ );
                    Buffer.Add( "<size=60%>" ).StartColor( loadColor ).Add( LoadName ).Add( " 负载</size> <size=50%> " ).AddNumberMoreReadable( totalLoad ).Add( "</size>" ).EndColor();
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
                    tooltipBuffer.Add( "" ).Add( "计算负载中…" );
                else
                {
                    string loadColor = EndpointFunctions.CalculateLoadColorAndCategory( totalLoad, out string LoadName, out string Explantion );
                    tooltipBuffer.StartColor( loadColor ).Add( LoadName ).Add( " / 负载分数 " ).AddNumberMoreReadable( totalLoad ).EndColor();
                    tooltipBuffer.Add( "\n" ).Add( Explantion );
                }
                tooltipBuffer.Add( "\n\n请注意，负载估算仅供参考；如果在游戏中通过信标黑客添加更多派系，负载可能会上升。" );
                tooltipBuffer.Add( "\n负载等级原因：\n" ).Add( summaryBuffer.GetStringAndResetForNextUpdate() );
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    tooltipBuffer.Add( "\n\n要查看更详细信息，请右键点击此按钮。" );
                else
                    tooltipBuffer.Add( "\n\n要查看更详细信息，请点击此按钮。" );

                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }

            private static ArcenDoubleCharacterBuffer errorTextBuffer = new ArcenDoubleCharacterBuffer( "btnStartGame-errorTextBuffer" );

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client || input.RightButtonClicked )
                {
                    int totalLoad = EndpointFunctions.CalculatePredictedGameLoad( detailsBuffer, false );
                    if ( totalLoad <= 0 )
                        popupBuffer.Add( "" ).Add( "计算负载中 - 关闭并重新打开以查看" );
                    else
                    {
                        string loadColor = EndpointFunctions.CalculateLoadColorAndCategory( totalLoad, out string LoadName, out string Explantion );
                        popupBuffer.StartColor( loadColor ).Add( LoadName ).Add( " / 负载分数 " ).AddNumberMoreReadable( totalLoad ).EndColor();
                        popupBuffer.Add( "\n" ).Add( Explantion );
                    }
                    popupBuffer.Add( "\n\n负载等级详细原因：\n" ).Add( detailsBuffer.GetStringAndResetForNextUpdate() );

                    popupBuffer.Add( "\n\n请注意，负载估算仅供参考；如果在游戏中通过信标黑客添加更多派系，负载可能会上升。" );

                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.TallWide, null, "预计CPU负载详情", popupBuffer.GetStringAndResetForNextUpdate(), "确定" );

                    return MouseHandlingResult.None;
                }
        
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "只能由主机启动", "抱歉，您必须等待主机启动游戏。", "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                if ( Mapgen.IsMapCurrentlyGenerating )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "仍在生成上一张地图", "抱歉，需要完成上一张地图的生成后才能尝试生成新的。", "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                errorTextBuffer.EnsureResetForNextUpdate();
                errorTextBuffer.Add( "无法启动此战役，原因如下：\n" );
                if ( EndpointFunctions.GetHasAnyErrorsPreventingStart( false, errorTextBuffer ) )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "战役尚无法启动！", errorTextBuffer.GetStringAndResetForNextUpdate(), "返回" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                if ( World.Instance.CampaignName == null || World.Instance.CampaignName.Length <= 0 )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "需要战役名称！", "请为您的战役选择一个名称。这可以使您的存档更加井然有序。", "返回" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }


                (this.Element as ArcenUI_Button).ClickSoundEffect = "ButtonStartGame";

                //This should propagate over to the real in-game lobby settings after 0.1 秒后完全生成。
                //That should then cause the REAL map to generate with all the details on it.
                //That in turn should then also cause the game to unpause for the first time (DoFirstUnpauseIfNotDoneYet).
                World_AIW2.Instance.Setup.ShouldSeedDetailsYet = true;
                World_AIW2.Instance.Setup.LobbyWorking_MarkAsChanged( StartWorldSource1.StartingMainGameAfterLobby, StartWorldSource2.NotLoadingAnything );

                return MouseHandlingResult.None;
            }
        }
        #endregion

        #region btnQuit
        public class btnQuit : ButtonAbstractBase
        {
            public static btnQuit Instance;

            public btnQuit()
            {
                Instance = this;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                Buffer.Add( "退出" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                //quitting to the main menu
                Window_SetupFooterControls.SaveLobbySettings();
                ArcenThreading.InitiateThreadingBlockForShutdown( NextThreadingBlockPhase.QuitToMainMenu );
                ArcenUI.HideAnyOpenDropdowns();
                return MouseHandlingResult.None;
            }
        }
        #endregion

        public static void SaveLobbySettings()
        {
            if ( Engine_AIW2.Instance.IsTestChamber ||
                 World_AIW2.Instance.GetIsTutorial() ||
                 ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client)
            {
                return;
            }
            
            try
            {
                World.Instance.SaveWorldToDiskAsMostRecentSettingsForLobby();
            }
            catch { } //if it fails, it fails
        }

        //from IInputActionHandler
        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.InternalName )
            {
                case "OpenSystemMenu":
                    //quitting to the main menu
                    Window_SetupFooterControls.SaveLobbySettings();
                    ArcenThreading.InitiateThreadingBlockForShutdown( NextThreadingBlockPhase.QuitToMainMenu );
                    break;
            }
        }

        #region btnResetToDefaults
        public class btnResetToDefaults : ButtonAbstractBase
        {
            public static btnResetToDefaults Instance;

            public btnResetToDefaults()
            {
                Instance = this;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                Buffer.Add( "恢复默认设置" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                EndpointFunctions.SetDefaultsForLobby();
                return MouseHandlingResult.None;
            }
            public override bool GetShouldBeHidden()
            {
                return ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client;
            }
        }
        #endregion

        #region btnDebugDetails
        public class btnDebugDetails : ButtonAbstractBase
        {
            public static btnDebugDetails Instance;

            public btnDebugDetails()
            {
                Instance = this;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                Buffer.Add( "调试详情" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                System.Text.StringBuilder builder = new System.Text.StringBuilder();

                List<ConfigurationForFaction> factionConfigs = World_AIW2.Instance.Setup.FactionConfigurations;
                ConfigurationForFaction selectingPlanetForFaction = Engine_AIW2.Instance.CurrentlyFocusedFactionInLobby;
                if ( selectingPlanetForFaction == null || selectingPlanetForFaction.SpecialFactionData.Type != FactionType.Player )
                {
                    selectingPlanetForFaction = null;
                    for ( int i = 0; i < factionConfigs.Count; i++ )
                    {
                        ConfigurationForFaction factionConfig = factionConfigs[i];
                        if ( factionConfig.SpecialFactionData.Type == FactionType.Player && factionConfig.IsFactionControlledByLocalPlayer )
                        {
                            selectingPlanetForFaction = factionConfig;
                            break;
                        }
                    }
                }

                #region General
                builder.Append( "<color=#fff799>******** 常规：********</color>" ).Append( "\n" );
                builder.Append( "PlayerAccount.Local.PlayerPrimaryKeyID: " ).Append( PlayerAccount.Local.PlayerPrimaryKeyID ).Append( "\n" );
                builder.Append( "World_AIW2.Instance.Setup.Seed: " ).Append( World_AIW2.Instance.Setup.MapConfig.Seed ).Append( "\n" );
                builder.Append( "selectingPlanetForFaction: " ).Append( selectingPlanetForFaction == null ? "[null]" : selectingPlanetForFaction.FactionIndex.ToString() ).Append( "\n" );
                builder.Append( "Engine_AIW2.Instance.CurrentlyFocusedFactionInLobby: " ).Append( Engine_AIW2.Instance.CurrentlyFocusedFactionInLobby == null ? "[null]" : 
                    Engine_AIW2.Instance.CurrentlyFocusedFactionInLobby.FactionIndex.ToString() ).Append( "\n" );
                #endregion

                builder.Append( "\n" );

                #region In-Game Factions
                builder.Append( "<color=#fff799>******** 游戏内派系：********</color>" ).Append( "\n" );
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction fac = World_AIW2.Instance.Factions[i];
                    if ( fac != null )
                    {
                        builder.Append( fac.FactionIndex ).Append( ": <color=#dcff99>" ).Append( fac.GetDisplayName() ).Append( "</color> (" ).Append(
                            !fac.Config.IsFactionControlledByAnyPlayer ? "NPC" : "玩家 " + fac.Config.GetPKIDOfFirstControllingPlayer() )
                            .Append( ")\n" );
                    }
                }
                #endregion

                builder.Append( "\n" );

                #region Player Accounts
                builder.Append( "<color=#fff799>******** 玩家账户：********</color>" ).Append( "\n" );
                for ( int i = 0; i < World.Instance.AllPlayerAccounts.Count; i++ )
                {
                    PlayerAccount playerAccount = World.Instance.AllPlayerAccounts[i];
                    if ( playerAccount != null )
                    {
                        builder.Append( "索引 " ).Append( i ).Append( " PKID: " ).Append( playerAccount.PlayerPrimaryKeyID ).Append( ": <color=#dcff99>" ).Append( playerAccount.Username ).Append( "</color> " )
                            .Append( playerAccount.Network_IsHost ? "(主机)" : string.Empty )
                            .Append( playerAccount.HasAutoCreatedFactionForThisAccountBefore ? "(已自动创建派系)" : "(未自动创建派系)" )
                            .Append( "\n" );
                    }
                    else
                        builder.Append( "索引 " ).Append( i ).Append( " 处的 PlayerAccount 为空\n" );
                }
                #endregion

                builder.Append( "\n" );

                #region actually calculate the text for the long-term and working stringbuilders
                StringBuilder builderLongTerm = new StringBuilder();
                World_AIW2.Instance.Setup.WriteDetailsToStringBuilder( builderLongTerm, true );
                #endregion

                builder.Append( "\n\n" );

                #region Setup (Stored Long Term)
                builder.Append( "<color=#fff799>******** 设置（长期存储）：********</color>" ).Append( "\n" );
                builder.Append( builderLongTerm.ToString() );
                #endregion

                builder.Append( "\n\n" );

                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.TallUltraWide, null, "当前设置的调试详情", builder.ToString(), "确定" );

                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                if ( GameSettings.Current == null || !GameSettings.Current.GetSettingsProperlyLoaded() )
                    return true;
                return !GameSettings.Current.GetBoolBySetting( "Debug_ShowDebugDetailsButtonInLobby" );
            }
        }
        #endregion

        #region dCampaignType
        public class dCampaignType : DropdownAbstractBase
        {
            public static dCampaignType Instance;
            public dCampaignType()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;
                CampaignTypeData ItemAsType = (CampaignTypeData)Item.GetItem();
                //don't set this again if it was already the same
                if ( World_AIW2.Instance.CampaignType == ItemAsType )
                    return;

                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "CampaignType";
                command.RelatedString2 = ItemAsType.InternalName;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
            }

            public override void OnUpdate()
            {
                if ( CampaignTypeDataTable.Instance.AvailableCampaignTypes.Count <= 0 )
                    return;

                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                if ( World_AIW2.Instance.CampaignType == null )
                    World_AIW2.Instance.CampaignType = CampaignTypeDataTable.EasiestNonSandboxType;
                CampaignTypeData typeDataToSelect = World_AIW2.Instance.CampaignType;

                bool foundMismatch = false;
                if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (CampaignTypeData)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
                {
                    foundMismatch = true;
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.InternalName, Verbosity.DoNotShow );
                }
                else
                {
                    for ( int i = 0; i < CampaignTypeDataTable.Instance.AvailableCampaignTypes.Count; i++ )
                    {
                        CampaignTypeData row = CampaignTypeDataTable.Instance.AvailableCampaignTypes[i];
                        if ( elementAsType.GetItemCount() <= i )
                        {
                            foundMismatch = true;
                            break;
                        }
                        IArcenUI_Dropdown_Option option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        CampaignTypeData optionItemAsType = (CampaignTypeData)option.GetItem();
                        if ( row == optionItemAsType )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( int i = 0; i < CampaignTypeDataTable.Instance.AvailableCampaignTypes.Count; i++ )
                    {
                        CampaignTypeData row = CampaignTypeDataTable.Instance.AvailableCampaignTypes[i];
                        DropdownOptionCampaignTypeData option = new DropdownOptionCampaignTypeData( row );
                        elementAsType.AddItem( option, row == typeDataToSelect );
                    }
                }
            }
            public override void HandleMouseover()
            {
                string mouseoverText = "战役类型决定游戏的一般规则集。";
                if ( World_AIW2.Instance.CampaignType == null )
                    World_AIW2.Instance.CampaignType = CampaignTypeDataTable.EasiestNonSandboxType;
                CampaignTypeData typeDataToSelect = World_AIW2.Instance.CampaignType;
                if ( typeDataToSelect != null )
                {
                    mouseoverText += "\n\n当前：<color=#7ab9ff>" + typeDataToSelect.DisplayName + "</color>\n" + typeDataToSelect.Description;
                }
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, mouseoverText );
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenUI_Dropdown_Option Item )
            {
                CampaignTypeData ItemAsType = (CampaignTypeData)Item.GetItem();
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( ItemElement, "战役类型决定游戏的一般规则集。\n\n<color=#ffc87a>" +
                    ItemAsType.DisplayName + "</color>：\n" + ItemAsType.Description );
            }
        }
        #endregion
    }

    public class DropdownOptionCampaignTypeData : RowBasedDropdownOption<CampaignTypeData>
    {
        public DropdownOptionCampaignTypeData( CampaignTypeData Row ) : base( Row )
        {
        }

        private static readonly ArcenDoubleCharacterBuffer displayNameBuffer = new ArcenDoubleCharacterBuffer( "Window_SetupFooterControls-DropdownOptionCampaignTypeData-displayNameBuffer" );
        public override string GetOptionNameFromVolatile()
        {
            displayNameBuffer.Add( "<size=85%>" );
            if ( this.Row == null )
                displayNameBuffer.Add( "无" );
            else
            {
                displayNameBuffer.Add( this.Row.GetDisplayName() );
                displayNameBuffer.AddDlcMod(this.Row );
            }

            return displayNameBuffer.GetStringAndResetForNextUpdate();
        }
    }
}
