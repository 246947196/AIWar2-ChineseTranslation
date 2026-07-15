using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;
using System.Linq;

namespace Arcen.AIW2.External
{
    public class Window_SetupFactionsTab : Window_SetupTabWindowBase
    {
        public bool IsOpen { get; private set; }
        public override void OnShowAfterNotShowing()
        {
            if (CurrentFaction == null)
            {
                if (World_AIW2.Instance.InSetupPhase)
                    CurrentFaction = World_AIW2.Instance.Setup.GetConfigurationForFaction( World_AIW2.Instance.GetLocalPlayerFactionOrNull()?.FactionIndex??-1 );
            }
            base.OnShowAfterNotShowing();
            IsOpen = true;
        }

        public override void OnHideAfterShowing()
        {
            base.OnHideAfterShowing();
            IsOpen = false;
        }

        public static Window_SetupFactionsTab Instance;
        public ConfigurationForFaction CurrentFaction
        {
            get
            {
                return Engine_AIW2.Instance.CurrentlyFocusedFactionInLobby;
            }
            set
            {
                Engine_AIW2.Instance.CurrentlyFocusedFactionInLobby = value;
            }
        }

        public Window_SetupFactionsTab()
        {
            Instance = this;
            this.topBuffer = 3;
            this.leftBuffer = 5;
            this.rowHeight = 24;
            this.rowBuffer = 1.5f;

            this.PreventsNormalInputHandlers = false;
            //this.SuppressesUIScaling = true;
            this.CurrentFaction = null;
        }

        #region bMainContentParent
        public class bMainContentParent : CustomUIAbstractBase
        {
            public static Transform ParentT;
            public static RectTransform ParentRT;
            public override void OnUpdate()
            {
                if ( ParentT == null )
                {
                    ParentT = this.Element.transform;
                    ParentRT = (RectTransform)ParentT;
                }
            }
        }
        #endregion

        private ArcenCachedExternalTypeDirect type_tSubSectionHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tSubSectionHeader ) );
        private ArcenCachedExternalTypeDirect type_tSettingName = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tSettingName ) );
        private ArcenCachedExternalTypeDirect type_bToggle = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bToggle ) );
        private ArcenCachedExternalTypeDirect type_tAltFor_bToggle = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tAltFor_bToggle ) );
        private ArcenCachedExternalTypeDirect type_iIntInput = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( iIntInput ) );
        private ArcenCachedExternalTypeDirect type_tAltFor_iIntInput = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tAltFor_iIntInput ) );
        private ArcenCachedExternalTypeDirect type_sIntSlider = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( sIntSlider ) );
        private ArcenCachedExternalTypeDirect type_tAltFor_sIntSlider = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tAltFor_sIntSlider ) );
        private ArcenCachedExternalTypeDirect type_dCustomDropdown = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( dCustomDropdown ) );
        private ArcenCachedExternalTypeDirect type_tSettingValueDescription = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tSettingValueDescription ) );

        private ArcenCachedExternalTypeDirect type_tSettingNameDirect = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tSettingNameDirect ) );
        private ArcenCachedExternalTypeDirect type_iFactionNameInput = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( iFactionNameInput ) );
        private ArcenCachedExternalTypeDirect type_bTeamColor = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bTeamColor ) );
        private ArcenCachedExternalTypeDirect type_tPlayerAccountName = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tPlayerAccountName ) );
        private ArcenCachedExternalTypeDirect type_bPlayerAccountToggle = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bPlayerAccountToggle ) );
        private ArcenCachedExternalTypeDirect type_bTeamColor_TrimOnly = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bTeamColor_TrimOnly ) );
        private ArcenCachedExternalTypeDirect type_bTeamColor_ForSubsidiaryFaction = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bTeamColor_ForSubsidiaryFaction) );

        private ArcenCachedExternalTypeDirect type_tAdvancedHidden = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tAdvancedHidden ) );

        #region PopulateFreeFormControls
        public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            if ( bMainContentParent.ParentT == null )
                return;
            this.Window.SetOverridingTransformToWhichToAddChildren( bMainContentParent.ParentT );

            float runningY = topBuffer;
            this.PopulateSubclassControls( Set, ref runningY );

            bMainContentParent.ParentRT.UI_SetHeight( runningY );
        }
        #endregion

        #region GetShouldDrawThisFrame_Subclass
        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( Window_SetupTopTabs.Current != LobbyTabType.Factions )
                return false;
            return true;
        }
        #endregion

        private static ButtonAbstractBase.ButtonPool<bLeftColumnButton> btnLeftColumnButtonPool;

        public bool isTempShowingAdvanced = false;
        public bool CalculateShouldShowAdvancedSettings()
        {
            //this style only works in the lobby!
            return World_AIW2.Instance.Setup.GetBoolBySetting( AIWar2GalaxySettingTable.Instance.GetRowByName( "ShowAdvancedGalaxyAndFactionOptions" ) ) || isTempShowingAdvanced;
        }

        #region getFactionsToShow
        public void getFactionsToShow( List<ConfigurationForFaction> ListToFill, bool SortResults )
        {
            WorldSetup setupToViewOnly = World_AIW2.Instance.Setup;
            ListToFill.Clear();
            for ( int i = 0; i < setupToViewOnly.FactionConfigurations.Count; i++ )
            {
                ConfigurationForFaction factionConfig = setupToViewOnly.FactionConfigurations[i];
                factionConfig.LobbyOnly_SortIndex = i;

                if ( !Helper_ShouldShowFaction( factionConfig ) )
                    continue;

                ListToFill.Add( factionConfig );
            }

            if ( SortResults )
            {
                ListToFill.Sort( static delegate ( ConfigurationForFaction Left, ConfigurationForFaction Right )
                {
                    int value = Left.SpecialFactionData.SortGroup.CompareTo( Right.SpecialFactionData.SortGroup );
                    if ( value != 0 ) return value;
                    if ( Left.SpecialFactionData.SortGroup >= 1000 )
                    {
                        value = Left.GetDisplayNameWithoutPlayerNames().CompareTo( Right.GetDisplayNameWithoutPlayerNames() );
                        if ( value != 0 ) return value;
                    }
                    return Left.LobbyOnly_SortIndex.CompareTo( Right.LobbyOnly_SortIndex );
                } );
            }
        }
        #endregion

        public static bool Helper_ShouldShowFaction( ConfigurationForFaction factionConfig )
        {
            if ( factionConfig.SpecialFactionData.ShouldNotBeShown || factionConfig.SpecialFactionData.ShouldNotBeShownInGameLobby)
                return false;

            //if ( Helper_GetShouldExcludeFaction( factionconfig.SpecialFactionData, Index ) )
            //    return false;
            return true;
        }

        #region Helper_GetShouldExcludeFaction
        public static bool Helper_GetShouldExcludeFaction( SpecialFactionData currentFactionData, int Index, out bool shouldStillShowAtAll, out string Reason )
        {
            if ( currentFactionData == null )
            {
                shouldStillShowAtAll = false;
                Reason = string.Empty;
                return true;
            }
            if ( currentFactionData.ShouldNotBeShown || currentFactionData.ShouldNotBeShownInGameLobby )
            {
                shouldStillShowAtAll = false;
                Reason = string.Empty;
                return true;
            }

            if ( currentFactionData.ShouldNotBeShownUnlessZombiesEnabled &&
                 !AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "ZombiesControlledByPlayer" ) )
            {
                shouldStillShowAtAll = false;
                Reason = string.Empty;
                return true;
            }

            if ( currentFactionData.MustBeAtMostOne )
            {
                bool foundOtherFactionWithThis = false;
                //only look BEFORE this entry in the list of factions
                for ( int j = 0; j < World_AIW2.Instance.Setup.FactionConfigurations.Count && j < Index; j++ )
                {
                    ConfigurationForFaction factionConfig = World_AIW2.Instance.Setup.FactionConfigurations[j];
                    if ( factionConfig == null )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "Warning: Helper_GetShouldExclude found Factions[" + j + "]==null", Verbosity.Chat );
                        continue;
                    }
                    if ( factionConfig.SpecialFactionData != currentFactionData )
                        continue;
                    foundOtherFactionWithThis = true;
                    break;
                }

                if ( foundOtherFactionWithThis )
                {
                    shouldStillShowAtAll = true;
                    Reason = "您只能向银河系添加一个此派系，且已存在一个。";
                    return true;
                }
            }
            if ( currentFactionData.MustBeAtMostX > 0 )
            {
                int countFoundOtherFactionWithThis = 0;
                //only look BEFORE this entry in the list of factions
                for ( int j = 0; j < World_AIW2.Instance.Setup.FactionConfigurations.Count && j < Index; j++ )
                {
                    ConfigurationForFaction factionConfig = World_AIW2.Instance.Setup.FactionConfigurations[j];
                    if ( factionConfig == null )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "Warning: Helper_GetShouldExclude found Factions[" + j + "]==null", Verbosity.Chat );
                        continue;
                    }
                    if ( factionConfig.SpecialFactionData != currentFactionData )
                        continue;
                    countFoundOtherFactionWithThis++;
                }

                if ( countFoundOtherFactionWithThis >= currentFactionData.MustBeAtMostX )
                {
                    shouldStillShowAtAll = true;
                    Reason = "您只能向银河系添加" + currentFactionData.MustBeAtMostX + "个此派系，且已存在" + countFoundOtherFactionWithThis + "个。";
                    return true;
                }
            }
            if ( World_AIW2.Instance.Setup.FactionConfigurations.Count > 200 )
            {
                shouldStillShowAtAll = true;
                    Reason = "游戏中派系太多，无法再添加更多。";
                return true;
            }

            shouldStillShowAtAll = true;
            Reason = string.Empty;
            return false;
        }
        #endregion

        #region custSetupWindowSectional
        public class custSetupWindowSectional : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_SettingsMenu.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( bLeftColumnButton.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnLeftColumnButtonPool = new ButtonAbstractBase.ButtonPool<bLeftColumnButton>( bLeftColumnButton.Original, 10 );
                        }
                    }
                    #endregion
                }

                this.OnUpdateCategories();
            }

            public static List<ConfigurationForFaction> lastFactionsToShow = List<ConfigurationForFaction>.Create_WillNeverBeGCed( 30, "Window_SetupFactionsTab-custSetupWindowSectional-lastFactionsToShow" );

            public void OnUpdateCategories()
            {
                float currentY = -5; //the position of the first entry

                if ( !hasGlobalInitialized )
                    return;

                btnLeftColumnButtonPool.Clear( 10 );

                Instance.getFactionsToShow( lastFactionsToShow, true );
                for ( int i = 0; i < lastFactionsToShow.Count; i++ )
                {
                    ConfigurationForFaction factionConfig = lastFactionsToShow[i];
                    bLeftColumnButton item = btnLeftColumnButtonPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; //time slicing, too many added right now
                    item.Assign( factionConfig );
                }

                #region Positioning Logic 1
                btnLeftColumnButtonPool.ApplyItemsInRows( 10, ref currentY, 36, 218, 30 );
                #endregion

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)bLeftColumnButton.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }
        }
        #endregion

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "游戏设置 - 派系" );
            }
        }
        #endregion

        #region bLeftColumnButton
        public class bLeftColumnButton : ButtonAbstractBase
        {
            public static bLeftColumnButton Original;
            public bLeftColumnButton() { if ( Original == null ) Original = this; }

            private ConfigurationForFaction Faction = null;

            public void Assign( ConfigurationForFaction Faction )
            {
                this.Faction = Faction;
            }

            public override bool GetShouldBeHidden()
            {
                return this.Faction == null;
            }

            public override void Clear()
            {
                this.Faction = null;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( this.Faction == null )
                    return;

                var fcon = this.Faction;
                if (fcon == null)
                    return;
                var fac = World_AIW2.Instance.GetFactionByIndex( this.Faction.FactionIndex );
                if (fac == null)
                    return;
                var finfo = fac.BaseInfo;
                if (finfo == null)
                    return;
                
                finfo.WriteFactionSlotText( buffer );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.Faction == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                Instance.CurrentFaction = this.Faction;
                Instance.isTempShowingAdvanced = false;
                return MouseHandlingResult.None;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_SetupFactionsTab-bLeftColumnButton-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( this.Faction == null )
                    return;

                ExternalFactionBaseInfo baseInfo = World_AIW2.Instance.GetFactionByIndex( this.Faction.FactionIndex )?.BaseInfo;
                if ( baseInfo != null )
                {
                    if ( baseInfo.AttachedFaction != null )
                        baseInfo.WriteFactionTooltipForSidebarInLobby( tooltipBuffer );
                }

                tooltipBuffer.AddDlcMod(this.Faction.SpecialFactionData, "This faction was added by" );

                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }
        #endregion

        #region PopulateSubclassControls
        private float nameWidth = 320;
        private float valueWidth = 185;
        private float explainWidth = 80;
        //private float fullWidth = 585;
        //private float spacerSize = 40f;

        protected override void PopulateSubclassControls( ArcenUI_SetOfCreateElementDirectives Set, ref float runningY )
        {
            Rect nameBounds;
            Rect valueBounds;
            Rect explainBounds = new Rect();

            //make sure the current selected faction is in bounds and still there, basically.
            if ( this.CurrentFaction == null )
                return;
            if ( this.CurrentFaction.GetInPoolStatus() || !World_AIW2.Instance.Setup.FactionConfigurations.Contains( this.CurrentFaction ) )
            {
                this.CurrentFaction = null;
                return;
            }
            if ( Mapgen.IsMapCurrentlyGenerating )
                return; //hide while regenerating, to make it refresh

            if ( this.CurrentFaction.SpecialFactionData.Type == FactionType.NaturalObject )
                return;
            if ( !Helper_ShouldShowFaction( this.CurrentFaction ) )
                return;

            bool shouldAlwaysShowVassalFields = false;
            bool shouldHideAllFieldsExceptPlayerType = false;
            string playerTypeName = string.Empty;
            PlayerTypeData playerType = this.CurrentFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
            if ( playerType != null )
                playerTypeName = playerType.InternalName;
            if ( playerType != null && playerType.HideAllFactionFieldsOtherThanPlayerType )
                shouldHideAllFieldsExceptPlayerType = true;

            List<ConfigurationForFaction> factionConfigs = World_AIW2.Instance.Setup.FactionConfigurations;
            for ( int i = 0; i < factionConfigs.Count; i++ )
            {
                ConfigurationForFaction factionConfig = factionConfigs[i];
                if ( factionConfig.SpecialFactionData.Type == FactionType.Player )
                {
                    PlayerTypeData otherPlayerType = factionConfig.PlayerTypeDataOrNull_ModeratelyExpensive;
                    if ( otherPlayerType.RequiredVassalCount > 0 )
                        shouldAlwaysShowVassalFields = true;
                }
            }

            if ( !shouldHideAllFieldsExceptPlayerType )
            {
                if ( this.CurrentFaction.SpecialFactionData.Type == FactionType.Player )
                {
                    //name for player factions
                    this.CalculateBoundsDual( out nameBounds, out valueBounds, ref runningY, nameWidth, valueWidth + explainWidth );
                    AddText( Set, type_tSettingNameDirect, this.CurrentFaction.GetDisplayNameWithoutPlayerNames() + " 派系名称", -1, -1, nameBounds, 12f );
                    AddInput( Set, type_iFactionNameInput, "FactionName", -1, -1, valueBounds );
                }
                else
                {
                    //nicknames for NPC factions
                    this.CalculateBoundsDual( out nameBounds, out valueBounds, ref runningY, nameWidth, valueWidth + explainWidth );
                    AddText( Set, type_tSettingNameDirect, this.CurrentFaction.GetDisplayNameWithoutPlayerNames() + " 派系昵称", -1, -1, nameBounds, 12f );
                    AddInput( Set, type_iFactionNameInput, "FactionName", -1, -1, valueBounds );
                }

                //TeamColor for all factions here
                this.CalculateBoundsTriple( out nameBounds, out valueBounds, out explainBounds, ref runningY, nameWidth, valueWidth, explainWidth );
                AddText( Set, type_tSettingNameDirect, this.CurrentFaction.GetDisplayNameWithoutPlayerNames() + " 派系颜色", -1, -1, nameBounds, 12f );
                valueBounds.width = valueBounds.height;
                AddButtonCustom( "ColorPickerButton", Set, type_bTeamColor, "TeamColor", -1, -1, valueBounds, -1f );
            }

            bool shouldShowAdvancedSettings = CalculateShouldShowAdvancedSettings();
            ConfigurationForFaction configForFac = GetConfigurationForFactionInUse();

            bool isCurrentlyVassal = this.CurrentFaction.GetBoolValueForCustomFieldOrDefaultValue( "Vassal", false );

            int numberAdvancedHidden = 0;
            for ( int i = 0; i < this.CurrentFaction.SpecialFactionData.CustomFields.Count; i++ )
            {
                DataForFaction_CustomFieldDefinition customField = this.CurrentFaction.SpecialFactionData.CustomFields[i];
                
                if ( customField.IsHidden )
                    continue;

                if ( shouldHideAllFieldsExceptPlayerType )
                {
                    if ( customField.InternalName != "PlayerType" )
                        continue;
                }
                if ( isCurrentlyVassal )
                {
                    switch ( customField.InternalName ) //these are not relevant options for vassals
                    {
                        case "Allegiance":
                        case "InvasionTime":
                            continue;
                    }
                }
                if ( playerTypeName != null && playerTypeName.Length > 0 )
                {
                    if ( customField.RequiresPlayerTypes.Count > 0 && !customField.RequiresPlayerTypes.Contains( playerTypeName ) )
                        continue; //don't show for this player type
                }
                if ( customField.IsAdvancedSetting && !shouldShowAdvancedSettings )
                {                    
                    if ( customField.GetIsTempValueMatchingDefault( configForFac ) ) //only hide advanced rows that match
                    {
                        bool showAnyway = false;
                        if ( shouldAlwaysShowVassalFields )
                        {
                            switch ( customField.InternalName )
                            {
                                case "Vassal":
                                    showAnyway = true;
                                    break;
                            }
                        }
                        
                        if ( !showAnyway )
                        {
                            numberAdvancedHidden++;
                            continue;
                        }
                    }
                }

                bool canEditField = customField.GetShouldBeVisibleAtThisHarshness();

                bool hasExplain = false;
                switch ( customField.SettingType )
                {
                    case DataForFaction_SettingType.IntSlider:
                        if ( canEditField )
                        {
                            hasExplain = true;
                            this.CalculateBoundsTriple( out nameBounds, out valueBounds, out explainBounds, ref runningY, nameWidth, valueWidth, explainWidth );
                        }
                        else
                            this.CalculateBoundsDual( out nameBounds, out valueBounds, ref runningY, nameWidth, valueWidth + explainWidth );
                        break;
                    default:
                        this.CalculateBoundsDual( out nameBounds, out valueBounds, ref runningY, nameWidth, valueWidth + explainWidth );
                        break;
                }

                AddText( Set, type_tSettingName, customField.InternalName, i, -1, nameBounds, 12f );

                switch ( customField.SettingType )
                {
                    case DataForFaction_SettingType.BoolToggle:
                        if ( canEditField )
                            AddButton( Set, type_bToggle, customField.InternalName, i, -1, valueBounds, -1f );
                        else
                            AddText( Set, type_tAltFor_bToggle, customField.InternalName, i, -1, valueBounds, 12f );
                        break;
                    case DataForFaction_SettingType.IntTextbox:
                        if ( canEditField )
                            AddInput( Set, type_iIntInput, customField.InternalName, i, -1, valueBounds );
                        else
                            AddText( Set, type_tAltFor_iIntInput, customField.InternalName, i, -1, valueBounds, 12f );
                        break;
                    case DataForFaction_SettingType.IntSlider:
                        if ( canEditField )
                            AddHorizontalSlider( Set, type_sIntSlider, customField.InternalName, i, -1, valueBounds );
                        else
                            AddText( Set, type_tAltFor_sIntSlider, customField.InternalName, i, -1, valueBounds, 12f );
                        break;
                    case DataForFaction_SettingType.CustomDropdownArbitraryOptions:
                    case DataForFaction_SettingType.CustomDropdownSurrogateTable:
                    case DataForFaction_SettingType.CustomDropdownCoreTable:
                    case DataForFaction_SettingType.CustomDropdownCoreTableSubset:
                        if ( canEditField )
                            AddDropdown( Set, type_dCustomDropdown, customField.InternalName, i, -1, valueBounds );
                        break;
                    case DataForFaction_SettingType.TeamColorPopup_TrimOnly:
                        valueBounds.width = valueBounds.height;
                        AddButtonCustom( "ColorPickerButton", Set, type_bTeamColor_TrimOnly, customField.InternalName, i, -1, valueBounds, -1f );
                        break;
                    case DataForFaction_SettingType.TeamColorPopup_SubsidiaryFactionColor:
                        valueBounds.width = valueBounds.height;
                        AddButtonCustom( "ColorPickerButton", Set, type_bTeamColor_ForSubsidiaryFaction, customField.InternalName, i, -1, valueBounds, -1f );
                        break;

                }

                if ( hasExplain )
                    AddText( Set, type_tSettingValueDescription, customField.InternalName, i, -1, explainBounds, 14f );
            }

            if ( numberAdvancedHidden > 0 )
            {
                this.CalculateBoundsSingle( out nameBounds, ref runningY, nameWidth + valueWidth + explainWidth );
                AddText( Set, type_tAdvancedHidden, string.Empty, numberAdvancedHidden, numberAdvancedHidden, nameBounds, 10f );
            }

            if ( this.CurrentFaction.SpecialFactionData.Type == FactionType.Player && ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.SinglePlayerOnly )
            {
                //Players Controlling Subsection
                {
                    runningY += ((this.rowHeight + rowBuffer) * 0.5f);
                    nameBounds = ArcenRectangle.CreateUnityRect( 5, runningY, 400, this.rowHeight );
                    AddText( Set, type_tSubSectionHeader, "PlayersControlling", -1, -1, nameBounds, 12f );
                    runningY += this.rowHeight + rowBuffer;
                }

                //which players are assigned to control this faction.
                for ( int i = 0; i < World.Instance.AllPlayerAccounts.Count; i++ )
                {
                    PlayerAccount player = World.Instance.AllPlayerAccounts[i];
                    nameBounds = ArcenRectangle.CreateUnityRect( 5, runningY, 400, this.rowHeight );
                    AddText( Set, type_tPlayerAccountName, string.Empty, player.PlayerPrimaryKeyID, i, nameBounds, 12f );

                    Rect valueSettingControlBounds = ArcenRectangle.CreateUnityRect( nameBounds.xMax, runningY, 170, this.rowHeight );
                    AddButton( Set, type_bPlayerAccountToggle, string.Empty, player.PlayerPrimaryKeyID, i, valueSettingControlBounds, -1f );

                    runningY += this.rowHeight + rowBuffer;
                }
            }

            //bMainContentParent.ParentRT.UI_SetHeight( runningY );
        }
        #endregion

        #region Player Faction Helper Methods
        public static PlayerAccount GetPlayerAccountForController( ElementAbstractBase controller )
        {
            int pkID = controller.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            return World.Instance.GetPlayerAccountByPrimaryID( pkID );
        }

        public static string GetNameForPlayerAccount( PlayerAccount player )
        {
            string name = player.Username;
            if ( name == null || name.Length <= 0 )
                name = "未知玩家账号 " + player.PlayerPrimaryKeyID;
            return name;
        }

        public static Faction GetCurrentFactionBeingViewedOrNull()
        {
            if ( Instance == null )
                return null;
            ConfigurationForFaction cfg = Instance.CurrentFaction;
            if ( cfg == null )
                return null;
            if ( cfg.FactionIndex < 0 || cfg.FactionIndex >= World_AIW2.Instance.Factions.Count )
                return null;
            return World_AIW2.Instance.Factions[cfg.FactionIndex];
        }
        #endregion

        #region Player Faction Types
        public class tPlayerAccountName : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                PlayerAccount player = GetPlayerAccountForController( this );
                if ( player == null ) return;

                buffer.StartColor( player.GetFactionCenterColor().ColorHexBrighter );
                buffer.Add( GetNameForPlayerAccount( player ) );

                if ( player.Network_IsHost )
                    buffer.Add( "  （主机）" );
                if ( PlayerAccount.Local == player )
                    buffer.Add( "  （你）" );

                buffer.EndColor();
            }

            public static void MouseoverDetails( PlayerAccount player, ArcenUI_Element Element )
            {
                Faction currentFactionBeingViewed = GetCurrentFactionBeingViewedOrNull();

                Faction controllingFactionCurrently = World_AIW2.Instance.GetFirstFactionControlledByPlayerOrNull( player.PlayerPrimaryKeyID );
                if ( controllingFactionCurrently == null )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, GetNameForPlayerAccount( player ) + " 目前只是观察者。他们可以观看游戏，但无法控制任何单位。这可以在游戏中的任何时候更改。" );
                else if ( controllingFactionCurrently == currentFactionBeingViewed && currentFactionBeingViewed != null )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, GetNameForPlayerAccount( player ) + " 目前正在控制此派系（" + currentFactionBeingViewed.GetDisplayName() + "）。在多人游戏中，此派系的控制权可以在任意数量的玩家之间共享，包括在战役过程中添加和移除额外的控制玩家。" );
                else if ( controllingFactionCurrently != null )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, GetNameForPlayerAccount( player ) + " 目前正在控制另一个派系（" + controllingFactionCurrently.GetDisplayName() + "）。多个玩家可以控制一个派系，但每个玩家只能控制一个派系。将此玩家分配控制此派系将使其停止控制另一个派系。" );
            }

            public override void HandleMouseover()
            {
                PlayerAccount player = GetPlayerAccountForController( this );
                if ( player == null ) return;
                MouseoverDetails( player, this.Element );
            }
        }

        public class bPlayerAccountToggle : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                PlayerAccount player = GetPlayerAccountForController( this );
                if ( player == null ) return;

                Faction currentFactionBeingViewed = GetCurrentFactionBeingViewedOrNull();
                Faction controllingFactionCurrently = World_AIW2.Instance.GetFirstFactionControlledByPlayerOrNull( player.PlayerPrimaryKeyID );

                buffer.Add( currentFactionBeingViewed != null && controllingFactionCurrently == currentFactionBeingViewed ? "正在控制" : "<color=#666666>未控制" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                PlayerAccount player = GetPlayerAccountForController( this );
                if ( player == null ) return MouseHandlingResult.PlayClickDeniedSound;

                Faction currentFactionBeingViewed = GetCurrentFactionBeingViewedOrNull();
                if ( currentFactionBeingViewed == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                Faction controllingFactionCurrently = World_AIW2.Instance.GetFirstFactionControlledByPlayerOrNull( player.PlayerPrimaryKeyID );

                //do the change
                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "FactionOwnershipChanged";
                command.RelatedIntegers.Add( currentFactionBeingViewed.FactionIndex );
                command.RelatedIntegers.Add( player.PlayerPrimaryKeyID );
                //set if true, otherwise just remove.  Only set to true if not controlling a faction or controlling a different one.
                command.RelatedBool = (controllingFactionCurrently == null || controllingFactionCurrently != currentFactionBeingViewed);
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                PlayerAccount player = GetPlayerAccountForController( this );
                if ( player == null ) return;
                tPlayerAccountName.MouseoverDetails( player, this.Element );
            }
        }
        #endregion

        #region GetCustomFieldForController
        public static DataForFaction_CustomFieldDefinition GetCustomFieldForController( ElementAbstractBase controller )
        {
            if ( Instance == null )
                return null;

            ConfigurationForFaction factionConfigToDisplay = Instance.CurrentFaction;
            if ( factionConfigToDisplay == null )
                return null;
            int fieldIndex = controller.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            if ( fieldIndex < 0 || fieldIndex >= factionConfigToDisplay.SpecialFactionData.CustomFields.Count )
                return null;

            return factionConfigToDisplay.SpecialFactionData.CustomFields[fieldIndex];
        }
        #endregion

        #region GetConfigurationForFactionInUse
        public static ConfigurationForFaction GetConfigurationForFactionInUse()
        {
            if ( Instance == null )
                return null;

            ConfigurationForFaction factionConfigToDisplay = Instance.CurrentFaction;
            return factionConfigToDisplay;
        }
        #endregion

        #region GetCurrentStringForCustomField
        public static string GetCurrentStringForCustomField( DataForFaction_CustomFieldDefinition ForField )
        {
            if ( Instance == null || ForField == null )
                return string.Empty;
            ConfigurationForFaction factionConfigToDisplay = Instance.CurrentFaction;
            if ( factionConfigToDisplay == null )
                return string.Empty;
            if ( !factionConfigToDisplay.SpecialFactionData.CustomFields.Contains( ForField ) )
                return string.Empty; //if we switched factions
            return factionConfigToDisplay.GetStringValueForCustomFieldOrDefaultValue( ForField.InternalName, false );
        }
        #endregion

        #region GetCurrentIntForCustomField
        public static int GetCurrentIntForCustomField( DataForFaction_CustomFieldDefinition ForField )
        {
            if ( Instance == null || ForField == null )
                return -1;

            ConfigurationForFaction factionConfigToDisplay = Instance.CurrentFaction;
            if ( factionConfigToDisplay == null )
                return -1;
            if ( !factionConfigToDisplay.SpecialFactionData.CustomFields.Contains( ForField ) )
                return -1; //if we switched factions
            return factionConfigToDisplay.GetIntValueForCustomFieldOrDefaultValue( ForField.InternalName, false );
        }
        #endregion

        #region SetCurrentStringForCustomField
        public static void SetCurrentStringForCustomField( DataForFaction_CustomFieldDefinition ForField, string Value )
        {
            if ( Instance == null || ForField == null )
                return;

            ConfigurationForFaction factionConfigToDisplay = Instance.CurrentFaction;
            if ( factionConfigToDisplay == null )
                return;
            if ( !factionConfigToDisplay.SpecialFactionData.CustomFields.Contains( ForField ) )
                return; //if we switched factions
            factionConfigToDisplay.SetCustomFieldValue( ForField.InternalName, Value );
        }
        #endregion

        public class tSubSectionHeader : TextAbstractBase
        {
            public ArcenSettingSubcategory GetSubcategory()
            {
                string subCategoryName = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString;
                return ArcenSettingSubcategoryTable.Instance.GetRowByNameOrNullIfNotFound( subCategoryName );
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                ArcenSettingSubcategory subCat = this.GetSubcategory();
                if ( subCat == null )
                    buffer.Add( "<size=125%>Null subcategory!" );
                else
                    buffer.Add( "<size=125%>" ).StartColor( subCat.Color ).Add( subCat.DisplayName );
            }

            public override void HandleMouseover()
            {
                ArcenSettingSubcategory subCat = this.GetSubcategory();
                if ( subCat == null )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, "空子类别！" );
                else
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, subCat.DisplayName + "\n" + subCat.Description );
            }
        }

        #region Custom Controls For In-Game Settings
        public class tSettingName : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;

                ConfigurationForFaction configForFac = GetConfigurationForFactionInUse();

                bool fixedByCampaign = field.GetIsDefaultValueAlteredByHarshness();
                if ( !field.GetIsTempValueMatchingDefault( configForFac ) )
                    buffer.StartColor( "ffde85" );
                else if ( fixedByCampaign )
                    buffer.StartColor( "b7ffbc" );

                buffer.Add( field.DisplayName );

                var len = buffer.Builder.Length;
                buffer.AddDlcMod(field, "由以下添加");
                var wroteAnotherLine = buffer.Builder.Length > len;
                
                if ( fixedByCampaign )
                    buffer.EndColor().StartColor( "85ffa2" ).Add( wroteAnotherLine ? "  " : "\n" ).Add( "<size=50%>（被战役类型或AI难度修改）" ).EndColor();
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_SetupFactionsTab-tSettingName-tooltipBuffer" );
            public override void HandleMouseover()
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;

                tooltipBuffer.Add( field.DisplayName ).Add( "\n" ).Add( field.Description );
                tooltipBuffer.AddDlcMod( field, "This faction setting was added by", TextStyle.DlcMod_Full );
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }

        public class tSettingNameDirect : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString );
            }

            public override void HandleMouseover()
            {
            }
        }

        public class tSettingValueDescription : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;

                switch ( field.SettingType )
                {
                    case DataForFaction_SettingType.BoolToggle:
                        break;
                    case DataForFaction_SettingType.IntSlider:
                        {
                            buffer.Add( "  " );
                            int tempValue = GetCurrentIntForCustomField( field );
                            buffer.AddNumberMoreReadable( tempValue );
                            //buffer.Add( "       " ).Add( FontSizes.SLIGHTLY_SMALLER_SIZE_V2_STRING ).Add( "<color=#999999>(" ).Add( field.GetMinValue_Int().ToString() );
                            //if ( field.GetMaxValue_Int() > 0 && field.GetMaxValue_Int() > field.GetMinValue_Int() )
                            //    buffer.Add( " to " ).AddNumberMoreReadable( field.GetMaxValue_Int() );
                            //buffer.Add( ")" );
                        }
                        break;
                    case DataForFaction_SettingType.IntTextbox:
                        {
                            int valueAsInt = GetCurrentIntForCustomField( field );
                            if ( valueAsInt < field.GetMinValue_Int() )
                                buffer.Add( "  >= " ).Add( field.GetMinValue_Int() );
                            else if ( field.GetMaxValue_Int() > 0 && field.GetMaxValue_Int() > field.GetMinValue_Int() && valueAsInt > field.GetMaxValue_Int() )
                                buffer.Add( "  <= " ).Add( field.GetMaxValue_Int() );
                        }
                        break;
                }
            }

            public override void HandleMouseover()
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                if ( field.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, field.Description + field.GetAddedTextForCampaignType() );
            }
        }

        public class tAdvancedHidden : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                int numberHidden = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                buffer.StartColor( "ff7e64" ).Add( "<i>" ).Add( numberHidden ).Add( " 个高级字段已隐藏</i>" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.isTempShowingAdvanced = !Instance.isTempShowingAdvanced;
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                int numberHidden = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "要查看当前隐藏的 " + numberHidden +
                    " 个高级字段，请点击此处临时显示它们。\n\n如需长期查看，请切换到'选项'标签页，点击'显示高级星系和派系选项'按钮。" );
            }
        }

        public class bToggle : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                switch (field.SettingType)
                {
                    case DataForFaction_SettingType.BoolToggle:
                        break;
                    default:
                        return;
                }

                buffer.Add( GetCurrentIntForCustomField( field ) > 0 ? "开启" : "<color=#666666>关闭" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return MouseHandlingResult.None;
                switch ( field.SettingType )
                {
                    case DataForFaction_SettingType.BoolToggle:
                        break;
                    default:
                        return MouseHandlingResult.PlayClickDeniedSound;
                }

                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "DataForFaction_CustomFieldDefinitionChanged";
                command.RelatedString2 = field.InternalName;
                command.RelatedString3 = (GetCurrentIntForCustomField( field ) > 0 ? 0 : 1).ToString();
                command.RelatedIntegers.Add( Instance.CurrentFaction.FactionIndex );
                command.RelatedBool = field.RequiresMarkAsChanged;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                if ( field.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, field.Description + field.GetAddedTextForCampaignType() );
            }
        }

        public class tAltFor_bToggle : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                switch ( field.SettingType )
                {
                    case DataForFaction_SettingType.BoolToggle:
                        break;
                    default:
                        return;
                }

                buffer.Add( GetCurrentIntForCustomField( field ) > 0 ? "开启" : "<color=#666666>关闭" );
            }

            public override void HandleMouseover()
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                if ( field.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, field.Description + field.GetAddedTextForCampaignType() );
            }
        }

        public class bTeamColor : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                ConfigurationForFaction factionConfig = Instance.CurrentFaction;

                Window_TeamColorPicker.Instance.Open( factionConfig.FactionCenterColor, factionConfig.FactionTrimColor,
                    delegate ( TeamColorDefinition CenterColor, TeamColorDefinition TrimColor )
                    {
                        bool madeChanges = false;
                        if ( factionConfig.FactionCenterColor != CenterColor )
                        {
                            madeChanges = true;
                            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                            command.RelatedString = "bTeamColor";
                            command.RelatedString2 = "FactionCenterColor";
                            command.RelatedString3 = CenterColor.InternalName;
                            command.RelatedIntegers.Add( Instance.CurrentFaction.FactionIndex );
                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                        }
                        if ( factionConfig.FactionTrimColor != TrimColor )
                        {
                            madeChanges = true;
                            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                            command.RelatedString = "bTeamColor";
                            command.RelatedString2 = "FactionTrimColor";
                            command.RelatedString3 = TrimColor.InternalName;
                            command.RelatedIntegers.Add( Instance.CurrentFaction.FactionIndex );
                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                        }
                        if ( madeChanges )
                        {
                            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                            command.RelatedString = "SimplyRegenerate";
                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                        }
                    }, true );
                return MouseHandlingResult.None;
            }

            public override void DoAnyCustomButtonStuffFromVolatile( ArcenUI_Button Button )
            {
                ConfigurationForFaction factionConfig = Instance.CurrentFaction;
                if ( factionConfig != null && factionConfig.FactionCenterColor != null && Button.RelatedImages.Length >= 1 )
                    Button.RelatedImages[0].color = factionConfig.FactionCenterColor.TeamColor;
                if ( factionConfig != null && factionConfig.FactionTrimColor != null && Button.RelatedImages.Length >= 2 )
                    Button.RelatedImages[1].color = factionConfig.FactionTrimColor.TeamColor;
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "点击编辑此阵营的中心和边界颜色。" );
            }
        }
        public class bTeamColor_ForSubsidiaryFaction : ButtonAbstractBase
        {
            ConfigurationForFaction subsidiaryFactionConfig = null;
            private void getSubsidiaryFaction()
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null )
                    return;
                for ( int i = 0; i < World_AIW2.Instance.Setup.FactionConfigurations.Count; i++ )
                {
                    if ( World_AIW2.Instance.Setup.FactionConfigurations[i] == null
                         || World_AIW2.Instance.Setup.FactionConfigurations[i].SpecialFactionData == null )
                        continue;
                    if ( World_AIW2.Instance.Setup.FactionConfigurations[i].SpecialFactionData.InternalName == field.RelatedFactionName )
                    {
                        subsidiaryFactionConfig = World_AIW2.Instance.Setup.FactionConfigurations[i];
                        return;
                    }
                }
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                getSubsidiaryFaction();
                if ( subsidiaryFactionConfig == null )
                    return MouseHandlingResult.None;
                //ArcenDebugging.ArcenDebugLogSingleLine("Found subsidiary " + subsidiaryFactionConfig.SpecialFactionData.InternalName + " idx " + subsidiaryFactionConfig.FactionIndex + " prev center color: " + subsidiaryFactionConfig.FactionCenterColor, Verbosity.DoNotShow );
                Window_TeamColorPicker.Instance.Open( subsidiaryFactionConfig.FactionCenterColor, subsidiaryFactionConfig.FactionTrimColor,
                    delegate ( TeamColorDefinition CenterColor, TeamColorDefinition TrimColor )
                    {
                        //bool madeChanges = false;
                        if ( subsidiaryFactionConfig.FactionCenterColor != CenterColor )
                        {
                            //madeChanges = true;
                            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                            command.RelatedString = "bTeamColor";
                            command.RelatedString2 = "FactionCenterColor";
                            command.RelatedString3 = CenterColor.InternalName;
                            command.RelatedIntegers.Add( subsidiaryFactionConfig.FactionIndex );
                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                        }
                        if ( subsidiaryFactionConfig.FactionTrimColor != TrimColor )
                        {
                            //madeChanges = true;
                            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                            command.RelatedString = "bTeamColor";
                            command.RelatedString2 = "FactionTrimColor";
                            command.RelatedString3 = TrimColor.InternalName;
                            command.RelatedIntegers.Add( subsidiaryFactionConfig.FactionIndex );
                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                        }
                        // if ( madeChanges )
                        // {
                        //     GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                        //     command.RelatedString = "SimplyRegenerate";
                        //     World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                        // }
                    }, true );
                return MouseHandlingResult.None;
            }

            public override void DoAnyCustomButtonStuffFromVolatile( ArcenUI_Button Button )
            {
                getSubsidiaryFaction();
                if ( subsidiaryFactionConfig == null )
                    return;
                if ( subsidiaryFactionConfig != null && subsidiaryFactionConfig.FactionCenterColor != null && Button.RelatedImages.Length >= 1 )
                    Button.RelatedImages[0].color = subsidiaryFactionConfig.FactionCenterColor.TeamColor;
                if ( subsidiaryFactionConfig != null && subsidiaryFactionConfig.FactionTrimColor != null && Button.RelatedImages.Length >= 2 )
                    Button.RelatedImages[1].color = subsidiaryFactionConfig.FactionTrimColor.TeamColor;
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "点击编辑此阵营的中心和边界颜色。" );
            }
        }

        public class bTeamColor_TrimOnly : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                ConfigurationForFaction factionConfig = Instance.CurrentFaction;

                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                string currentValue = GetCurrentStringForCustomField( field );
                if ( currentValue == null || currentValue.Length == 0 )
                    currentValue = field.GetDefaultValue_String();
                TeamColorDefinition colorDef = TeamColorDefinitionTable.Instance.GetRowByNameOrNullIfNotFound( currentValue );

                Window_TeamColorPickerTrimOnly.Instance.Open( factionConfig.FactionCenterColor, colorDef,
                    delegate ( TeamColorDefinition TrimColor )
                    {
                        if ( colorDef != TrimColor )
                        {
                            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                            command.RelatedString = "DataForFaction_CustomFieldDefinitionChangedValidForDuringGame";
                            command.RelatedString2 = field.InternalName;
                            command.RelatedString3 = TrimColor.InternalName;
                            command.RelatedIntegers.Add( Instance.CurrentFaction.FactionIndex );
                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                        }
                    } );
                return MouseHandlingResult.None;
            }

            public override void DoAnyCustomButtonStuffFromVolatile( ArcenUI_Button Button )
            {
                ConfigurationForFaction factionConfig = Instance.CurrentFaction;
                if ( factionConfig != null && factionConfig.FactionCenterColor != null && Button.RelatedImages.Length >= 1 )
                    Button.RelatedImages[0].color = factionConfig.FactionCenterColor.TeamColor;
                
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                string currentValue = GetCurrentStringForCustomField( field );
                if ( currentValue == null || currentValue.Length == 0 )
                    currentValue = field.GetDefaultValue_String();
                TeamColorDefinition colorDef = TeamColorDefinitionTable.Instance.GetRowByNameOrNullIfNotFound( currentValue );
                if ( colorDef != null && Button.RelatedImages.Length >= 2 )
                    Button.RelatedImages[1].color = colorDef.TeamColor;
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "点击编辑此子阵营的边界颜色。它将保持与主阵营相同的主色。" );
            }
        }

        public class iFactionNameInput : InputAbstractBase
        {
            public override void OnEndEdit()
            { 
                ConfigurationForFaction config = Instance.CurrentFaction;
                if ( config == null ) return;

                ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;
                string NewValue = elementAsType.GetText();

                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "FactionNameChanged";
                command.RelatedString3 = NewValue;
                command.RelatedIntegers.Add( Instance.CurrentFaction.FactionIndex );
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
            }

            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                if ( input.Length >= 30 )
                    return '\0';
                //use a whitelist of approved characters only
                if ( Char.IsLetterOrDigit( addedChar ) ) //must be alphanumeric
                    return addedChar;
                if ( ArcenSerializationBuffer.CharMapping.Contains( addedChar ) ) //block everything except alphanumerics allowed chars
                    return addedChar;
                return '\0';
            }

            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                switch ( Action.InternalName )
                {
                    case "OpenSystemMenu": //escape key
                    case "Return": //enter key
                        return InputActionTextboxResult.UnfocusMe;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }

            public override void OnUpdate()
            {
                ConfigurationForFaction config = Instance.CurrentFaction;
                if ( config == null ) return;

                if ( !ArcenInput.CalculateIsInputFieldFocused() )
                {
                    ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;
                    elementAsType.SetText( config.FactionNameOrEmpty == null ? string.Empty : config.FactionNameOrEmpty );
                }
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "如果留空，将使用控制此阵营的第一位玩家的名称。如果你希望你的帝国拥有与你个人名称不同的名字，可以在此设置。如果多个玩家共享一个阵营，他们可以给它取一个代表共同利益的名字。" );
            }
        }

        public class iIntInput : InputAbstractBase
        {
            public override void HandleChangeInValue( string NewValue )
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                switch ( field.SettingType )
                {
                    case DataForFaction_SettingType.IntSlider:
                    case DataForFaction_SettingType.IntTextbox:
                        break;
                    default:
                        return;
                }

                int newValueAsInt;
                if ( !Int32.TryParse( NewValue, out newValueAsInt ) )
                    return;
                if ( newValueAsInt == GetCurrentIntForCustomField( field ) )
                    return;
                if ( newValueAsInt < field.GetMinValue_Int() )
                    return;
                if ( field.GetMaxValue_Int() > field.GetMinValue_Int() && newValueAsInt > field.GetMaxValue_Int() )
                    return;

                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "DataForFaction_CustomFieldDefinitionChanged";
                command.RelatedString2 = field.InternalName;
                command.RelatedString3 = newValueAsInt.ToString();
                command.RelatedIntegers.Add( Instance.CurrentFaction.FactionIndex );
                command.RelatedBool = field.RequiresMarkAsChanged;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
            }

            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                switch ( Action.InternalName )
                {
                    case "OpenSystemMenu": //escape key
                    case "Return": //enter key
                        return InputActionTextboxResult.UnfocusMe;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }

            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                if ( input.Length >= 10 )
                    return '\0';
                if ( !char.IsDigit( addedChar ) )
                    return '\0';

                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return '\0';

                string testInput = input.Insert( charIndex, addedChar.ToString() );
                int testInputAsType;
                if ( !Int32.TryParse( testInput, out testInputAsType ) )
                    return '\0';

                return addedChar;
            }

            public override void OnUpdate()
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                switch ( field.SettingType )
                {
                    case DataForFaction_SettingType.IntSlider:
                    case DataForFaction_SettingType.IntTextbox:
                        break;
                    default:
                        return;
                }

                //only update to the current value if we're not editing this field right now
                if ( !this.GetIsCurrentlyBeingEdited() )
                {
                    ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;
                    elementAsType.SetText( GetCurrentIntForCustomField( field ).ToString() );
                }
            }

            public override void HandleMouseover()
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                if ( field.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, field.Description + field.GetAddedTextForCampaignType() );
            }
        }

        public class tAltFor_iIntInput : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                switch ( field.SettingType )
                {
                    case DataForFaction_SettingType.IntSlider:
                    case DataForFaction_SettingType.IntTextbox:
                        break;
                    default:
                        return;
                }

                buffer.Add( GetCurrentIntForCustomField( field ).ToString() );
            }

            public override void HandleMouseover()
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                if ( field.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, field.Description + field.GetAddedTextForCampaignType() );
            }
        }

        public class sIntSlider : SliderAbstractBase
        {
            private bool hasNewValueBeenSetButNotSent = false;
            private int newValueIs = 0;
            private double lastTimeOfOnChanged = 0;
            private const float MIN_TIME_BETWEEN_SENDS = 0.2f;

            public override void OnUpdate()
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                switch ( field.SettingType )
                {
                    case DataForFaction_SettingType.IntSlider:
                    case DataForFaction_SettingType.IntTextbox:
                        break;
                    default:
                        return;
                }

                int currentValue = GetCurrentIntForCustomField( field );
                int range = field.GetMaxValue_Int() - field.GetMinValue_Int();
                if ( range == 0 ) range = 1;
                float currentPortion = (float)(currentValue - field.GetMinValue_Int()) / (float)range;

                ArcenUI_Slider elementAsType = (ArcenUI_Slider)this.Element;
                elementAsType.ReferenceSlider.value = currentPortion;

                if ( hasNewValueBeenSetButNotSent && Engine_Universal.CumulativeUnscaledTime - lastTimeOfOnChanged >= MIN_TIME_BETWEEN_SENDS )
                {
                    hasNewValueBeenSetButNotSent = false;
                    GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    command.RelatedString = "DataForFaction_CustomFieldDefinitionChanged";
                    command.RelatedString2 = field.InternalName;
                    command.RelatedString3 = newValueIs.ToString();
                    command.RelatedIntegers.Add( Instance.CurrentFaction.FactionIndex );
                    command.RelatedBool = field.RequiresMarkAsChanged;
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                }
            }

            public override void OnChange( float NewValue )
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                switch ( field.SettingType )
                {
                    case DataForFaction_SettingType.IntSlider:
                    case DataForFaction_SettingType.IntTextbox:
                        break;
                    default:
                        return;
                }

                lastTimeOfOnChanged = Engine_Universal.CumulativeUnscaledTime;

                int range = field.GetMaxValue_Int() - field.GetMinValue_Int();
                int adjustedNewValue = field.GetMinValue_Int() + Mathf.RoundToInt( range * NewValue );

                if ( adjustedNewValue == GetCurrentIntForCustomField( field ) )
                    return;

                newValueIs = adjustedNewValue;
                hasNewValueBeenSetButNotSent = true;
                //this is an MP desync, but keeps the interface responsive.  It will be synced up within a part of a second.
                SetCurrentStringForCustomField( field, adjustedNewValue.ToString() );
            }

            public override void HandleMouseover()
            {
                DataForFaction_CustomFieldDefinition setting = GetCustomFieldForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, setting.Description );
            }
        }

        public class tAltFor_sIntSlider : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                switch ( field.SettingType )
                {
                    case DataForFaction_SettingType.IntSlider:
                    case DataForFaction_SettingType.IntTextbox:
                        break;
                    default:
                        return;
                }

                buffer.Add( GetCurrentIntForCustomField( field ).ToString() );
                buffer.Add( "    <color=#999999>(" ).Add( field.GetMinValue_Int() ).Add( " 到 " ).Add( field.GetMaxValue_Int() ).Add( ")" );
            }

            public override void HandleMouseover()
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                if ( field.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, field.Description + field.GetAddedTextForCampaignType() );
            }
        }

        public abstract class StringBasedCustomDropdown : DropdownAbstractBase
        {
            private string SelectedItemAsType
            {
                get
                {
                    DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                    if ( field == null ) return string.Empty;

                    string currentValue = GetCurrentStringForCustomField( field );
                    return currentValue;
                }
            }

            private bool shouldIgnoreChanges = false;
            private int lastShownFactionIndex = -1;
            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option BaseItem, DropdownSetType SetType )
            {
                if ( BaseItem == null || this.shouldIgnoreChanges )
                    return;
                StringTrioBasedDropdownOption Item = BaseItem as StringTrioBasedDropdownOption;
                if ( Item == null )
                    return;

                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                string oldValue = GetCurrentStringForCustomField( field );
                if ( oldValue == Item.OptionValue.GetInternalName() )
                    return; //no change!

                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "DataForFaction_CustomFieldDefinitionChanged";
                command.RelatedString2 = field.InternalName;
                command.RelatedString3 = Item.OptionValue.GetInternalName();
                command.RelatedIntegers.Add( Instance.CurrentFaction.FactionIndex );
                command.RelatedBool = field.RequiresMarkAsChanged;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
            }

            public override void OnUpdate()
            {
                this.shouldIgnoreChanges = true;
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                if ( lastShownFactionIndex != Instance.CurrentFaction.FactionIndex || Instance.CurrentFaction == null  )
                {
                    lastShownFactionIndex = Instance.CurrentFaction == null ? -1 : Instance.CurrentFaction.FactionIndex;
                    elementAsType.ClearItems();
                }

                List<IOption> options = this.GetOptions();
                if ( elementAsType.GetItemCount() != options.Count )
                {
                    elementAsType.ClearItems();
                    string selectedString = this.SelectedItemAsType;
                    for ( int i = 0; i < options.Count; i++ )
                    {
                        IOption optionValue = options[i];
                        StringTrioBasedDropdownOption option = this.ConstructOptionFor( optionValue );
                        elementAsType.AddItem( option, optionValue.GetInternalName() == selectedString );
                    }
                }
                this.shouldIgnoreChanges = false;
            }

            public abstract List<IOption> GetOptions();

            protected virtual StringTrioBasedDropdownOption ConstructOptionFor( IOption value )
            {
                return new StringTrioBasedDropdownOption( value );
            }
            public override void HandleMouseover()
            {
                DataForFaction_CustomFieldDefinition setting = GetCustomFieldForController( this );
                if ( setting == null ) return;

                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;
                IArcenUI_Dropdown_Option selectedItem = elementAsType == null ? null : elementAsType.CurrentlySelectedOption;
                StringTrioBasedDropdownOption selectedItemAsTrio = selectedItem as StringTrioBasedDropdownOption;

                if ( selectedItemAsTrio != null )
                {
                    //main setting
                    StringTrioBasedDropdownOption.tooltipBuffer.Add( setting.DisplayName ).Add( "\n" );
                    StringTrioBasedDropdownOption.tooltipBuffer.Add( setting.Description );
                    //the specific option chosen
                    if ( selectedItemAsTrio.FillMouseoverPartsForItem( setting.Description.Length > 0 ) )
                        StringTrioBasedDropdownOption.tooltipBuffer.Add( setting.GetAddedTextForCampaignType() );
                    //now draw it
                    selectedItemAsTrio.FinishAndDisplayTooltipFromBuffer( this.Element );
                }
                else
                {
                    //main setting only
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, setting.Description );
                }
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenUI_Dropdown_Option Item )
            {
                DataForFaction_CustomFieldDefinition setting = GetCustomFieldForController( this );
                if ( setting == null ) return;

                StringTrioBasedDropdownOption itemAsTrio = Item as StringTrioBasedDropdownOption;

                if ( itemAsTrio != null )
                {
                    //the specific option under mouse
                    if ( itemAsTrio.FillMouseoverPartsForItem( false ) )
                        StringTrioBasedDropdownOption.tooltipBuffer.Add( setting.GetAddedTextForCampaignType() );
                    //now draw it
                    itemAsTrio.FinishAndDisplayTooltipFromBuffer( this.Element );
                }
            }
        }

        public class dCustomDropdown : StringBasedCustomDropdown
        {
            protected override StringTrioBasedDropdownOption ConstructOptionFor( IOption value )
            {
                return new CustomDropdownOption( value );
            }

            private static List<IOption> emptyList = List<IOption>.Create_WillNeverBeGCed( 1, "dCustomDropdown-emptyList" );
            public override List<IOption> GetOptions()
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) 
                    return emptyList;
                return field.GetOptions();
            }
        }
        public class CustomDropdownOption : StringTrioBasedDropdownOption
        {
            public CustomDropdownOption( IOption Value ) : base( Value )
            {
            }
            //public override string GetOptionNameFromVolatile()
            //{
            //    return " " + base.GetOptionNameFromVolatile();
            //}
        }
        #endregion

        #region dCenterOptions
        public class dCenterOptions : DropdownAbstractBase
        {
            public static dCenterOptions Instance;
            public dCenterOptions()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                
            }

            public override void OnUpdate()
            {
                
            }
            public override void HandleMouseover()
            {
                
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenUI_Dropdown_Option Item )
            {
            }
            public override bool GetShouldBeHidden()
            {
                return true; //not used on this screen
            }
        }
        #endregion

        #region btnLeftColumn1 (Add Faction)
        public class btnLeftColumn1 : ButtonAbstractBase
        {
            public static btnLeftColumn1 Instance;

            public btnLeftColumn1()
            {
                Instance = this;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "添加阵营" );
            }

            private ProtectedList<CustomPopupData> factionOptions = ProtectedList<CustomPopupData>.Create_WillNeverBeGCed( 200, "Window_SetupFactionsTab-btnLeftColumn1-factionOptions" );

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                factionOptions.Clear( true );

                bool shouldCallOutVassalCapableFactions = false;
                List<ConfigurationForFaction> factionConfigs = World_AIW2.Instance.Setup.FactionConfigurations;
                for ( int i = 0; i < factionConfigs.Count; i++ )
                {
                    ConfigurationForFaction factionConfig = factionConfigs[i];
                    if ( factionConfig.SpecialFactionData.Type == FactionType.Player )
                    {
                        PlayerTypeData otherPlayerType = factionConfig.PlayerTypeDataOrNull_ModeratelyExpensive;
                        if ( otherPlayerType.RequiredVassalCount > 0 )
                            shouldCallOutVassalCapableFactions = true;
                    }
                }

                #region Fill factionOptions
                for ( int i = 0; i < SpecialFactionDataTable.Instance.Rows.Count; i++ )
                {
                    SpecialFactionData row = SpecialFactionDataTable.Instance.Rows[i];
                    CustomPopupData option = CustomPopupData.GetFromPoolOrCreate();

                    bool shouldStillShowAtAll;
                    string Reason;
                    if ( row.ShouldNotBeShownInGameLobbyAddFactionsList ){
                        ArcenDebugging.ArcenDebugLogSingleLine("skipping " + row.InternalName, Verbosity.DoNotShow );
                        continue;
                    }
                    if ( Helper_GetShouldExcludeFaction( row, 999999999, out shouldStillShowAtAll, out Reason ) ) //by having an index that is higher than any faction, it will check them all for "must be only one" and similar
                    {
                        if ( !shouldStillShowAtAll )
                            continue;
                        option.CanBeSelected = false;
                        option.CannotBeSelectedReason = Reason;
                    }
                    if ( row.TexEmbedSprite_Icon != null )
                    {
                        //Provide an icon along with faction name
                        option.TexEmbedSprite_Icon = row.TexEmbedSprite_Icon;
                        option.TexEmbedSprite_IconBorder = row.TexEmbedSprite_IconBorder;
                        option.TexEmbedSprite_IconOverlay = row.TexEmbedSprite_IconOverlay;
                        option.IconColor = row.DefaultFactionCenterColor.ColorHexBrighter;
                        option.IconBorderColor = row.DefaultFactionTrimColor.ColorHexBrighter;
                        option.IconOverlayColor = row.TexEmbedSprite_IconOverlay_HexColor;
                    }

                    option.DisplayName = row.GetDisplayName();
                    if ( row.Type == FactionType.Player )
                        option.DisplayName = "额外人类玩家槽位";
                    else if ( row.Type == FactionType.AI )
                        option.DisplayName = "额外AI阵营";

                    var lobbyName = row.OriginalXmlData.GetString( "custom_NameForLobby", string.Empty, false );
                    var sortName = row.OriginalXmlData.GetString( "custom_NameForSorting", string.Empty, false );
                    if (!string.IsNullOrEmpty(lobbyName))
                        option.DisplayName = lobbyName;
                    if (!string.IsNullOrEmpty(sortName))
                        option.SortingName = sortName;
                    else
                        option.SortingName = option.DisplayName;

                    if ( shouldCallOutVassalCapableFactions )
                    {
                        if ( row.CanBeAVassal )
                        {
                            option.DisplayName += " <size=80%><color=#ff5aee>可成为附庸</color></size>";
                        }
                    }

                    option.InternalName = row.InternalName;
                    option.Tooltiptext = row.Description;
                    option.RowFromExpansion = row.RowFromExpansion;
                    option.RowFromXmlMod = row.RowFromXmlMod;

                    factionOptions.Add( option );
                }
                #endregion

                factionOptions.Sort( static delegate ( CustomPopupData Left, CustomPopupData Right )
                {
                    return Left.SortingName.CompareTo( Right.SortingName );
                } );

                Window_PopupScrollingColumnButtonList.Instance.Open( "选择要添加的阵营", null, factionOptions,
                    delegate ( CustomPopupData Option )
                    {
                        if ( Option == null || !Option.CanBeSelected )
                            return;
                        GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                        command.RelatedString = "dFactionType_Add";
                        command.RelatedString2 = Option.InternalName;
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                    }, null, "This faction was added by" );

                return MouseHandlingResult.None;
            }
            public override bool GetShouldBeHidden()
            {
                return false; //always valid, I suppose
            }
        }
        #endregion

        #region btnLeftColumn2 (Remove Faction)
        public class btnLeftColumn2 : ButtonAbstractBase
        {
            public static btnLeftColumn2 Instance;

            public btnLeftColumn2()
            {
                Instance = this;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "移除阵营" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                ConfigurationForFaction faction = Window_SetupFactionsTab.Instance.CurrentFaction;
                if ( faction == null || ( !faction.AllowRemoval && faction.SpecialFactionData.Type != FactionType.Player ) )
                    return MouseHandlingResult.PlayClickDeniedSound;

                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "dFactionType_Remove";
                command.RelatedIntegers.Add( faction.FactionIndex ); //remove
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                return MouseHandlingResult.None;
            }
            public override bool GetShouldBeHidden()
            {
                ConfigurationForFaction faction = Window_SetupFactionsTab.Instance.CurrentFaction;
                return faction == null || (!faction.AllowRemoval && faction.SpecialFactionData.Type != FactionType.Player);
            }
        }
        #endregion

        #region Inherited Stuff
        public class tChatText : tChatText_Base
        {
        }

        public class tPlayerInfoText : tPlayerInfoText_Base
        { }

        public class tChatHeaderText : tChatHeaderText_Base
        { }

        public class btnSendChat : btnSendChat_Base
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                iChatTextbox.Instance.DoSend();
                return MouseHandlingResult.None;
            }
        }

        public class iChatTextbox : iChatTextbox_Base
        {
            public static iChatTextbox Instance;
            public iChatTextbox() { Instance = this; }

            public override void DoSend()
            {
                DoSend_Inner( this );
            }

            public override void ClearTextbox()
            {
                this.SetText( string.Empty );
            }
        }
        #endregion
    }
}
