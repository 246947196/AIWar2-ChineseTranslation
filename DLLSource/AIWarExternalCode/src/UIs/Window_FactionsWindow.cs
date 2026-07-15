using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;
using System.Linq;

namespace Arcen.AIW2.External
{
    public class Window_FactionsWindow : ToggleableWindowController, IInputActionHandler
    {
        protected float topBuffer = 3;
        protected float leftBuffer = 2;
        protected float rowHeight = 24;
        protected float rowBuffer = 1.5f;

        public const Int16 OTHER_FACTIONS_INDEX = 9999;

        public int CurrentFactionIndex
        {
            get
            {
                return CurrentFactionArrayIndexNonliteral;
            }
        }
        public new bool IsOpen { get; private set; }
        public override void OnShowAfterNotShowing()
        {
            base.OnShowAfterNotShowing();
            IsOpen = true;
        }

        public override void OnHideAfterShowing()
        {
            base.OnHideAfterShowing();
            IsOpen = false;
        }

        #region CalculateBoundsSingle
        protected void CalculateBoundsSingle( out Rect soleBounds, ref float runningY, float SoleWidth )
        {
            soleBounds = ArcenRectangle.CreateUnityRect( leftBuffer, runningY, SoleWidth, rowHeight );

            runningY += rowHeight + rowBuffer;
        }
        #endregion

        #region CalculateBoundsDual
        protected void CalculateBoundsDual( out Rect nameBounds, out Rect valueSettingControlBounds, ref float runningY, float NameWidth, float ValueWidth )
        {
            nameBounds = ArcenRectangle.CreateUnityRect( leftBuffer, runningY, NameWidth, rowHeight );

            valueSettingControlBounds = ArcenRectangle.CreateUnityRect( nameBounds.xMax, runningY, ValueWidth, rowHeight );

            runningY += rowHeight + rowBuffer;
        }
        #endregion

        #region CalculateBoundsTriple
        protected void CalculateBoundsTriple( out Rect nameBounds, out Rect valueSettingControlBounds, out Rect valueDescriptionBounds, ref float runningY, float NameWidth, float ValueWidth, float DescriptionWidth )
        {
            nameBounds = ArcenRectangle.CreateUnityRect( leftBuffer, runningY, NameWidth, rowHeight );

            valueSettingControlBounds = ArcenRectangle.CreateUnityRect( nameBounds.xMax, runningY, ValueWidth, rowHeight );

            valueDescriptionBounds = ArcenRectangle.CreateUnityRect( valueSettingControlBounds.xMax, runningY, DescriptionWidth, rowHeight );

            runningY += rowHeight + rowBuffer;
        }
        #endregion
        
        private int _currentFactionIndex;
        private bool isSkippingDrawingAnythingThisFrameWhileDataFieldsSwitchOver = false;

        public int CurrentFactionArrayIndexNonliteral
        {
            get
            {
                return _currentFactionIndex;
            }
            set
            {
                _currentFactionIndex = value;
                Engine_AIW2.Instance.CurrentlyFocusedFactionInLobby = GetCurrentFactionBeingViewedOrNull()?.Config;
            }
        }

        public static Window_FactionsWindow Instance;
        public Window_FactionsWindow()
        {
            Instance = this;
            //this.ShouldCauseAllOtherWindowsToNotShow = true;
            this.PreventsNormalInputHandlers = true;
            //this.SuppressesUIScaling = true;

            this.topBuffer = 3;
            this.leftBuffer = 5;
            this.rowHeight = 24;
            this.rowBuffer = 1.5f;
            this.CurrentFactionArrayIndexNonliteral = -1;
            this.isSkippingDrawingAnythingThisFrameWhileDataFieldsSwitchOver = true;
        }

        public override void OnOpen()
        {
            //make sure it shows nothing at the start.  0 is always the naturalobject faction
            if (this.CurrentFactionArrayIndexNonliteral == 0)
            {
                int i = World_AIW2.Instance.GetLocalPlayerFactionOrNull()?.FactionIndex ?? -1;
                CurrentFactionArrayIndexNonliteral = i;
            }
            this.isTempShowingAdvanced = false;
            this.isSkippingDrawingAnythingThisFrameWhileDataFieldsSwitchOver = true;
        }

        public bool isTempShowingAdvanced = false;
        public bool CalculateShouldShowAdvancedSettings()
        {
            //this style only works in the main game!
            return World_AIW2.Instance.Setup.GetBoolBySetting( AIWar2GalaxySettingTable.Instance.GetRowByName( "ShowAdvancedGalaxyAndFactionOptions" ) ) || isTempShowingAdvanced;
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
        private ArcenCachedExternalTypeDirect type_tAltFor_dCustomDropdown = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tAltFor_dCustomDropdown ) );
        private ArcenCachedExternalTypeDirect type_tSettingValueDescription = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tSettingValueDescription ) );

        private ArcenCachedExternalTypeDirect type_tSettingNameDirect = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tSettingNameDirect ) );
        private ArcenCachedExternalTypeDirect type_iFactionNameInput = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( iFactionNameInput ) );
        private ArcenCachedExternalTypeDirect type_bTeamColor = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bTeamColor ) );
        private ArcenCachedExternalTypeDirect type_dPlayerGiftType = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( dPlayerGiftType ) );
        private ArcenCachedExternalTypeDirect type_dPlayerGiftTarget = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( dPlayerGiftTarget ) );
        private ArcenCachedExternalTypeDirect type_iPlayerGiftAmount = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( iPlayerGiftAmount ) );
        private ArcenCachedExternalTypeDirect type_bPlayerGiftSelection = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bPlayerGiftSelection ) );
        private ArcenCachedExternalTypeDirect type_tMetalGiftDisplay = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tMetalGiftDisplay ) );
        private ArcenCachedExternalTypeDirect type_bMetalGiftRemoval = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bMetalGiftRemoval ) );
        private ArcenCachedExternalTypeDirect type_tEnergyGiftDisplay = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tEnergyGiftDisplay ) );
        private ArcenCachedExternalTypeDirect type_bEnergyGiftRemoval = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bEnergyGiftRemoval ) );
        private ArcenCachedExternalTypeDirect type_tPlayerAccountName = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tPlayerAccountName ) );
        private ArcenCachedExternalTypeDirect type_bPlayerAccountToggle = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bPlayerAccountToggle ) );
        private ArcenCachedExternalTypeDirect type_bTeamColor_TrimOnly = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bTeamColor_TrimOnly ) );
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

        private static ButtonAbstractBase.ButtonPool<bCategory> bCategoryPool;

        #region getFactionsToShowInOtherFactionsList
        public void getFactionsToShowInOtherFactionsList( List<Faction> ListToFill, bool SortResults )
        {
            ListToFill.Clear();

            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction faction = World_AIW2.Instance.Factions[i];

                if ( !Helper_ShouldShowFactionInOtherList( faction, i ) )
                    continue;
                ListToFill.Add( faction );
            }

            if ( SortResults )
            {
                ListToFill.Sort( static delegate ( Faction Left, Faction Right )
                {
                    //normal sorting
                    int value = Left.SpecialFactionData.SortGroup.CompareTo( Right.SpecialFactionData.SortGroup );
                    if ( value != 0 ) return value;
                    if ( Left.SpecialFactionData.SortGroup >= 1000 )
                    {
                        value = Left.GetDisplayNameWithoutPlayerNames().CompareTo( Right.GetDisplayNameWithoutPlayerNames() );
                        if ( value != 0 ) return value;
                    }
                    return Left.FactionIndex.CompareTo( Right.FactionIndex );
                } );
            }
        }
        #endregion

        #region getFactionsToShow
        public void getFactionsToShow( List<Faction> ListToFill, bool SortResults )
        {
            ListToFill.Clear();
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction faction = World_AIW2.Instance.Factions[i];
                //                ArcenDebugging.ArcenDebugLogSingleLine("Checking whether to show " + faction.GetDisplayName(), Verbosity.DoNotShow );
                if ( !Helper_ShouldShowFaction( faction, i ) )
                    continue;
                //                ArcenDebugging.ArcenDebugLogSingleLine("\tdecided to show " + faction.GetDisplayName(), Verbosity.DoNotShow );
                ListToFill.Add( faction );
            }

            if ( SortResults )
            {
                ListToFill.Sort( static delegate ( Faction Left, Faction Right )
                {
                    //normal sorting
                    int value = Left.SpecialFactionData.SortGroup.CompareTo( Right.SpecialFactionData.SortGroup );
                    if ( value != 0 ) return value;
                    if ( Left.SpecialFactionData.SortGroup >= 1000 )
                    {
                        value = Left.GetDisplayNameWithoutPlayerNames().CompareTo( Right.GetDisplayNameWithoutPlayerNames() );
                        if ( value != 0 ) return value;
                    }
                    return Left.FactionIndex.CompareTo( Right.FactionIndex );
                } );
            }
        }
        #endregion

        #region Helper_ShouldShowFactionInOtherList
        public static bool Helper_ShouldShowFactionInOtherList( Faction faction, int Index )
        {
            if ( !faction.SpecialFactionData.ShouldNotBeShown )
                return false;
            if ( faction.SpecialFactionData.ShouldNotBeShownEvenAsSeparateLineItemsInGameFactionsMenu )
                return false;

            if ( faction.SpecialFactionData.ShowOnlyIfFactionWithThisNameIsPresent.Length > 0 )
            {
                //ArcenDebugging.ArcenDebugLog( faction.SpecialFactionData.InternalName + " looking for faction with name '" + faction.SpecialFactionData.ShowOnlyIfFactionWithThisNameIsPresent + "'", Verbosity.DoNotShow );
                bool foundOtherFactionWeNeed = false;
                for ( int j = 0; j < World_AIW2.Instance.Factions.Count; j++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[j];
                    if ( faction == null )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "Warning: Helper_GetShouldExclude found Factions[" + j + "]==null", Verbosity.Chat );
                        continue;
                    }
                    if ( otherFaction.SpecialFactionData.InternalName != faction.SpecialFactionData.ShowOnlyIfFactionWithThisNameIsPresent )
                        continue;
                    //ArcenDebugging.ArcenDebugLog( "Found faction " + otherFaction.SpecialFactionData.InternalName + " for " + faction.SpecialFactionData.InternalName + " at index " + otherFaction.FactionIndex +
                    //    " ", Verbosity.DoNotShow );
                    foundOtherFactionWeNeed = true;
                    break;
                }
                if ( !foundOtherFactionWeNeed )
                {
                    //ArcenDebugging.ArcenDebugLog( faction.SpecialFactionData.InternalName + " NOT FOUND '" + faction.SpecialFactionData.ShowOnlyIfFactionWithThisNameIsPresent + "'", Verbosity.DoNotShow );
                    return false;
                }
                //ArcenDebugging.ArcenDebugLog( faction.SpecialFactionData.InternalName + " FOUND '" + faction.SpecialFactionData.ShowOnlyIfFactionWithThisNameIsPresent + "'", Verbosity.DoNotShow );
            }
            return true;
        }
        #endregion

        #region Helper_ShouldShowFaction
        public static bool Helper_ShouldShowFaction( Faction faction, int Index )
        {
            if ( faction.SpecialFactionData.ShouldNotBeShownUnlessZombiesEnabled &&
                 !AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "ZombiesControlledByPlayer" ) )
                return false;

            if ( faction.Type == FactionType.Player )
                return true;
            if ( faction.SpecialFactionData.ShouldNotBeShown )
                return false;
            if ( !faction.HasBeenSeenByPlayer &&
                 !AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "AlwaysShowFactions" ) )
                return false;

            if ( World.Instance.ConclusionType == CampaignConclusionType.NotConcluded &&
                 AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "FactionDetailsAreSecret" ))
            {
                return false;
            }

            //if ( Helper_GetShouldExcludeFaction( faction.SpecialFactionData, Index ) )
            //    return false;
            return true;
        }
        #endregion

        #region Helper_GetShouldExcludeFaction
        public static bool Helper_GetShouldExcludeFaction( SpecialFactionData currentFactionData, int Index, out bool shouldStillShowAtAll, out string Reason )
        {
            if ( currentFactionData == null )
            {
                shouldStillShowAtAll = false;
                Reason = string.Empty;
                return true;
            }
            if ( currentFactionData.ShouldNotBeShown )
            {
                shouldStillShowAtAll = false;
                Reason = string.Empty;
                return true;
            }
            if ( currentFactionData.MustBeAtMostOne )
            {
                bool foundOtherFactionWithThis = false;
                //only look BEFORE this entry in the list of factions
                for ( int j = 0; j < World_AIW2.Instance.Factions.Count && j < Index; j++ )
                {
                    Faction faction = World_AIW2.Instance.Factions[j];
                    if ( faction == null )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "Warning: Helper_GetShouldExclude found Factions[" + j + "]==null", Verbosity.Chat );
                        continue;
                    }
                    if ( faction.SpecialFactionData != currentFactionData )
                        continue;
                    foundOtherFactionWithThis = true;
                    break;
                }
                if ( foundOtherFactionWithThis )
                {
                    shouldStillShowAtAll = true;
                    Reason = "您只能在银河中添加一个此阵营，且已存在一个。";
                    return true;
                }
            }
            if ( currentFactionData.MustBeAtMostX > 0 )
            {
                int countFoundOtherFactionWithThis = 0;
                //only look BEFORE this entry in the list of factions
                for ( int j = 0; j < World_AIW2.Instance.Factions.Count && j < Index; j++ )
                {
                    Faction faction = World_AIW2.Instance.Factions[j];
                    if ( faction == null )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "Warning: Helper_GetShouldExclude found Factions[" + j + "]==null", Verbosity.Chat );
                        continue;
                    }
                    if ( faction.SpecialFactionData != currentFactionData )
                        continue;
                    countFoundOtherFactionWithThis++;
                }
                if ( countFoundOtherFactionWithThis >= currentFactionData.MustBeAtMostX )
                {
                    shouldStillShowAtAll = true;
                    Reason = "您只能在银河中添加 " + currentFactionData.MustBeAtMostX + " 个此阵营，且已存在 " + countFoundOtherFactionWithThis + " 个。";
                    return true;
                }
            }
            if ( World_AIW2.Instance.Factions.Count > 200 )
            {
                shouldStillShowAtAll = true;
                Reason = "游戏中阵营过多，无法再添加更多。";
                return true;
            }

            if ( currentFactionData.ShouldNotBeShownUnlessZombiesEnabled &&
                 !AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "ZombiesControlledByPlayer" ) )
            {
                shouldStillShowAtAll = false;
                Reason = string.Empty;
                return true;
            }

            shouldStillShowAtAll = true;
            Reason = string.Empty;
            return false;
        }
        #endregion

        #region customParent
        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_SettingsMenu.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( bCategory.Original != null )
                        {
                            hasGlobalInitialized = true;
                            bCategoryPool = new ButtonAbstractBase.ButtonPool<bCategory>( bCategory.Original, 10 );
                        }
                    }
                    #endregion
                }

                this.OnUpdateCategories();
            }

            public static List<Faction> factionsToShow = List<Faction>.Create_WillNeverBeGCed( 30, "Window_FactionsWindow-factionsToShow" );

            public void OnUpdateCategories()
            {
                float currentY = -5; //the position of the first entry

                if ( !hasGlobalInitialized )
                    return;

                bCategoryPool.Clear( 5 );

                Instance.getFactionsToShow( factionsToShow, true );
                for ( int i = 0; i < factionsToShow.Count; i++ )
                {
                    Faction faction = factionsToShow[i];
                    if ( Instance.CurrentFactionArrayIndexNonliteral < 0 && faction.GetIsLocalFaction() )
                    {
                        Instance.CurrentFactionArrayIndexNonliteral = faction.FactionIndex;
                        Instance.isTempShowingAdvanced = false;
                    }
                    bCategory item = bCategoryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; //time slicing, too many added right now
                    item.AssignFactionIndex( faction.FactionIndex );
                }

                {
                    bCategory item = bCategoryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item != null )
                        item.AssignFactionIndex( OTHER_FACTIONS_INDEX );
                }

                #region Positioning Logic 1
                bCategoryPool.ApplyItemsInRows( 10, ref currentY, 36, 218, 30 );
                #endregion

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)bCategory.Original.Element.RelevantRect.parent;
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
                Buffer.Add( "查看/编辑阵营（更改立即生效）" );
            }
        }
        #endregion

        #region bCategory
        public class bCategory : ButtonAbstractBase
        {
            public static bCategory Original;
            public bCategory() { if ( Original == null ) Original = this; }

            private Int16 FactionIndex = -1;

            public void AssignFactionIndex( Int16 FactionIndex )
            {
                this.FactionIndex = FactionIndex;
            }

            public override bool GetShouldBeHidden()
            {
                return this.FactionIndex < 0;
            }

            public override void Clear()
            {
                this.FactionIndex = -1;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( this.FactionIndex == OTHER_FACTIONS_INDEX )
                {
                    buffer.Add( "其他阵营" );
                    return;
                }

                if ( this.FactionIndex < 0 || this.FactionIndex >= World_AIW2.Instance.Factions.Count )
                    return;

                Faction fac = World_AIW2.Instance.Factions[this.FactionIndex];
                if ( fac == null )
                    return;

                var facinfo = fac.BaseInfo;
                facinfo.WriteFactionSlotText( buffer );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.CurrentFactionArrayIndexNonliteral = this.FactionIndex;
                Instance.isTempShowingAdvanced = false;
                Instance.isSkippingDrawingAnythingThisFrameWhileDataFieldsSwitchOver = true;
                return MouseHandlingResult.None;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_FactionsWindow-bCategory-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( this.FactionIndex == OTHER_FACTIONS_INDEX )
                {
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "有许多阵营除了颜色之外没有其他可自定义的内容。您可以在此调整所有这些阵营的设置。" );
                    return;
                }

                if ( this.FactionIndex < 0 || this.FactionIndex >= World_AIW2.Instance.Factions.Count )
                    return;

                var fac = World_AIW2.Instance.Factions[this.FactionIndex];
                if ( fac == null )
                    return;

                var baseInfo = World_AIW2.Instance.GetFactionByIndex( fac.FactionIndex )?.BaseInfo;
                if ( baseInfo != null )
                {
                    if ( baseInfo.AttachedFaction != null )
                        baseInfo.WriteFactionTooltipForSidebarInLobby( tooltipBuffer );
                }

                tooltipBuffer.AddDlcMod(fac.SpecialFactionData, "此阵营由以下内容添加：" );

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

        protected void PopulateSubclassControls( ArcenUI_SetOfCreateElementDirectives Set, ref float runningY )
        {
            if ( this.isSkippingDrawingAnythingThisFrameWhileDataFieldsSwitchOver )
            {
                this.isSkippingDrawingAnythingThisFrameWhileDataFieldsSwitchOver = false;
                return;
            }
            if ( this.CurrentFactionArrayIndexNonliteral == OTHER_FACTIONS_INDEX )
            {
                this.PopulateSubclassControls_ForOtherFactionsList( Set, ref runningY );
                return;
            }

            Rect nameBounds;
            Rect valueBounds;
            Rect explainBounds = new Rect();
            Rect fourthBounds;

            if ( Mapgen.IsMapCurrentlyGenerating )
                return; //hide while regenerating, to make it refresh

            Faction factionToDisplay = GetCurrentFaction( -1 );
            if ( factionToDisplay == null || factionToDisplay.SpecialFactionData.Type == FactionType.NaturalObject )
                return;
            if ( !Helper_ShouldShowFaction( factionToDisplay, this.CurrentFactionArrayIndexNonliteral ) )
                return;

            bool shouldHideAllFieldsExceptPlayerType = false;
            string playerTypeName = string.Empty;
            PlayerTypeData playerType = factionToDisplay.PlayerTypeDataOrNull_ModeratelyExpensive;
            if ( playerType != null )
                playerTypeName = playerType.InternalName;
            if ( playerType != null && playerType.HideAllFactionFieldsOtherThanPlayerType )
                shouldHideAllFieldsExceptPlayerType = true;

            if ( !shouldHideAllFieldsExceptPlayerType )
            {
                if ( factionToDisplay.SpecialFactionData.Type == FactionType.Player )
                {
                    //name for player factions
                    this.CalculateBoundsDual( out nameBounds, out valueBounds, ref runningY, nameWidth, valueWidth + explainWidth );
                    AddText( Set, type_tSettingNameDirect, factionToDisplay.GetDisplayNameWithoutPlayerNames() + " 阵营名称", -1, -1, nameBounds, 12f );
                    AddInput( Set, type_iFactionNameInput, "FactionName", -1, -1, valueBounds );
                }
                else
                {
                    //nicknames for factions
                    this.CalculateBoundsDual( out nameBounds, out valueBounds, ref runningY, nameWidth, valueWidth + explainWidth );
                    AddText( Set, type_tSettingNameDirect, factionToDisplay.GetDisplayNameWithoutPlayerNames() + " 阵营昵称", -1, -1, nameBounds, 12f );
                    AddInput( Set, type_iFactionNameInput, "FactionName", -1, -1, valueBounds );
                }

                //TeamColor for all factions here
                this.CalculateBoundsTriple( out nameBounds, out valueBounds, out explainBounds, ref runningY, nameWidth, valueWidth, explainWidth );
                AddText( Set, type_tSettingNameDirect, factionToDisplay.GetDisplayNameWithoutPlayerNames() + " 阵营颜色", -1, -1, nameBounds, 12f );
                valueBounds.width = valueBounds.height;
                AddButtonCustom( "ColorPickerButton", Set, type_bTeamColor, "TeamColor", -1, -1, valueBounds, -1f );
            }

            if ( factionToDisplay.SpecialFactionData.Type == FactionType.Player )
            {
                if ( World_AIW2.Instance.EmpireStylePlayerFactions.Count > 1 &&
                    World_AIW2.Instance.EmpireStylePlayerFactions.Contains( factionToDisplay ) && 
                    !shouldHideAllFieldsExceptPlayerType )
                {
                    //Multiple Human Empires Subsection
                    {
                        runningY += ((this.rowHeight + rowBuffer) * 0.5f);
                        nameBounds = ArcenRectangle.CreateUnityRect( 5, runningY, 400, this.rowHeight );
                        AddText( Set, type_tSubSectionHeader, "MultipleHumanEmpires", -1, -1, nameBounds, 12f );
                        runningY += this.rowHeight + rowBuffer;
                    }

                    //Select Actions For Gifting
                    {
                        nameBounds = ArcenRectangle.CreateUnityRect( 5, runningY, 200, this.rowHeight );
                        AddDropdown( Set, type_dPlayerGiftType, string.Empty, factionToDisplay.FactionIndex, -1, nameBounds );

                        valueBounds = ArcenRectangle.CreateUnityRect( nameBounds.xMax + 5, runningY, 200, this.rowHeight );
                        AddDropdown( Set, type_dPlayerGiftTarget, string.Empty, factionToDisplay.FactionIndex, -1, valueBounds );

                        explainBounds = ArcenRectangle.CreateUnityRect( valueBounds.xMax + 5, runningY, 100, this.rowHeight );
                        AddInput( Set, type_iPlayerGiftAmount, string.Empty, factionToDisplay.FactionIndex, -1, explainBounds );

                        fourthBounds = ArcenRectangle.CreateUnityRect( explainBounds.xMax + 5, runningY, 100, this.rowHeight );
                        AddButton( Set, type_bPlayerGiftSelection, string.Empty, factionToDisplay.FactionIndex, -1, fourthBounds, -1f );

                        runningY += this.rowHeight + rowBuffer;
                    }
                    
                    //Ongoing Metal Gifts
                    for ( int i = 0; i < factionToDisplay.MetalGiftsFromThisPlayer.Count; i++ )
                    {
                        nameBounds = ArcenRectangle.CreateUnityRect( 5, runningY, 510, this.rowHeight );
                        AddText( Set, type_tMetalGiftDisplay, string.Empty, i, i, nameBounds, 12f );

                        valueBounds = ArcenRectangle.CreateUnityRect( nameBounds.xMax + 5, runningY, 100, this.rowHeight );
                        AddButton( Set, type_bMetalGiftRemoval, string.Empty, i, i, valueBounds, -1f );

                        runningY += this.rowHeight + rowBuffer;
                    }

                    //Ongoing Energy Gifts
                    for ( int i = 0; i < factionToDisplay.EnergyGiftsFromThisPlayer.Count; i++ )
                    {
                        nameBounds = ArcenRectangle.CreateUnityRect( 5, runningY, 510, this.rowHeight );
                        AddText( Set, type_tEnergyGiftDisplay, string.Empty, i, i, nameBounds, 12f );

                        valueBounds = ArcenRectangle.CreateUnityRect( nameBounds.xMax + 5, runningY, 100, this.rowHeight );
                        AddButton( Set, type_bEnergyGiftRemoval, string.Empty, i, i, valueBounds, -1f );

                        runningY += this.rowHeight + rowBuffer;
                    }
                }

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

                //PlayersOtherSettings
                //if ( !shouldHideAllFieldsExceptPlayerType ) //we do show this here, because the player type itself will be the sole option in this category
                {
                    runningY += ((this.rowHeight + rowBuffer) * 0.5f);
                    nameBounds = ArcenRectangle.CreateUnityRect( 5, runningY, 400, this.rowHeight );
                    AddText( Set, type_tSubSectionHeader, "PlayersOtherSettings", -1, -1, nameBounds, 12f );
                    runningY += this.rowHeight + rowBuffer;
                }
            } //endif Player faction

            bool canEditThisFactionAtAllOtherThanColor = false; //this once was letting you do things for beacons, but no longer.  Those are handled differently now
            bool shouldShowAdvancedSettings = CalculateShouldShowAdvancedSettings();

            bool isCurrentlyVassal = factionToDisplay.GetBoolValueForCustomFieldOrDefaultValue( "Vassal", false );

            int numberAdvancedHidden = 0;
            for ( int i = 0; i < factionToDisplay.SpecialFactionData.CustomFields.Count; i++ )
            {
                DataForFaction_CustomFieldDefinition customField = factionToDisplay.SpecialFactionData.CustomFields[i];
                if ( isCurrentlyVassal )
                {
                    switch ( customField.InternalName ) //these are not relevant options for vassals
                    {
                        case "Allegiance":
                        case "InvasionTime":
                            continue;
                    }
                }
                if ( shouldHideAllFieldsExceptPlayerType )
                {
                    if ( customField.InternalName != "PlayerType" )
                        continue;
                }
                if ( playerTypeName != null && playerTypeName.Length > 0 )
                {
                    if ( customField.RequiresPlayerTypes.Count > 0 && !customField.RequiresPlayerTypes.Contains( playerTypeName ) )
                        continue; //don't show for this player type
                }
                if ( customField.IsHidden )
                    continue;

                ConfigurationForFaction configForFac = GetConfigurationForFactionInUse( -1 ); //not for "other factions"
                if ( customField.IsAdvancedSetting && !shouldShowAdvancedSettings )
                {
                    if ( customField.GetIsTempValueMatchingDefault( configForFac ) ) //only hide advanced rows that match
                    {
                        //if ( !isInSandboxMode || setting.GetIsDefaultValueAlteredByHarshness() )
                        //{ } //DO draw these!
                        //else
                        {
                            numberAdvancedHidden++;
                            continue;
                        }
                    }
                }

                bool canEditField = canEditThisFactionAtAllOtherThanColor && customField.GetShouldBeVisibleAtThisHarshness();

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
                        else
                            AddText( Set, type_tAltFor_dCustomDropdown, customField.InternalName, i, -1, valueBounds, 12f );
                        break;
                    case DataForFaction_SettingType.TeamColorPopup_TrimOnly:
                        //these are all still ok!
                        valueBounds.width = valueBounds.height;
                        AddButtonCustom( "ColorPickerButton", Set, type_bTeamColor_TrimOnly, customField.InternalName, i, -1, valueBounds, -1f );
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
            if ( Instance.isSkippingDrawingAnythingThisFrameWhileDataFieldsSwitchOver )
                return null;
            //even though this is "CurrentFactionArrayIndexNonliteral" it can still work for our playe factions, so no worries
            if ( Instance.CurrentFactionArrayIndexNonliteral < 0 || Instance.CurrentFactionArrayIndexNonliteral >= World_AIW2.Instance.Factions.Count )
                return null;
            return World_AIW2.Instance.Factions[Instance.CurrentFactionArrayIndexNonliteral];
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
                else if ( PlayerAccount.Local == player )
                    buffer.Add( "  （你）" );

                buffer.EndColor();
            }

            public static void MouseoverDetails( ArcenUI_Element Element, PlayerAccount player )
            {
                Faction currentFactionBeingViewed = GetCurrentFactionBeingViewedOrNull();

                Faction controllingFactionCurrently = World_AIW2.Instance.GetFirstFactionControlledByPlayerOrNull( player.PlayerPrimaryKeyID );
                if ( controllingFactionCurrently == null )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, GetNameForPlayerAccount( player ) + " 目前只是旁观者。他们可以观看游戏，但无法控制任何单位。这可以在之后的任何时候更改。" );
                else if ( controllingFactionCurrently == currentFactionBeingViewed && currentFactionBeingViewed != null )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, GetNameForPlayerAccount( player ) + " 目前正在控制此阵营（" + currentFactionBeingViewed.GetDisplayName() + "）。在多人游戏中，此阵营的控制权可以共享给任意数量的玩家，包括在战役过程中添加或移除额外的控制玩家。" );
                else if ( controllingFactionCurrently != null )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, GetNameForPlayerAccount( player ) + " 目前正在控制另一个阵营（" + controllingFactionCurrently.GetDisplayName() + "）。多个玩家可以控制同一个阵营，但每个玩家只能控制一个阵营。将此玩家分配到控制此阵营将取消其对其他阵营的控制。" );
            }

            public override void HandleMouseover()
            {
                PlayerAccount player = GetPlayerAccountForController( this );
                if ( player == null ) return;
                MouseoverDetails( this.Element, player );
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

                buffer.Add( currentFactionBeingViewed != null && controllingFactionCurrently == currentFactionBeingViewed ? "控制中" : "<color=#666666>未控制" );
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
                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.AfterGameStartOnly_RequestFactionChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
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
                tPlayerAccountName.MouseoverDetails( this.Element, player );
            }
        }
        #endregion

        #region Player Gifting Actions
        public class dPlayerGiftType : dCustomDropdown
        {
            public static dPlayerGiftType MyInstance;
            public dPlayerGiftType()
            {
                MyInstance = this;
            }
            protected override StringTrioBasedDropdownOption ConstructOptionFor( IOption value )
            {
                return new CustomDropdownOption( value );
            }

            private List<IOption> options = null;
            public override List<IOption> GetOptions()
            {
                if ( options == null )
                {
                    options = List<IOption>.Create_WillNeverBeGCed( 6, "dPlayerGiftType-options" );
                    options.Add( ArbitaryArcenOption.Create(
                        "Metal_OneTime", "一次性金属赠礼",
                        "立即向另一位玩家赠送一定数量的金属。", null, null ) );

                    options.Add( ArbitaryArcenOption.Create(
                        "Metal_PerSecond", "每秒金属赠礼",
                        "开始以每秒为基础向另一位玩家赠送一定数量的金属。如果您生产的金属远超所需，这是确保金属得到充分利用的好方法。您可以稍后取消这些指令。每个帝国每秒最多只能接受5,000金属。", null, null ) );

                    options.Add( ArbitaryArcenOption.Create(
                        "Energy_Ongoing", "持续能源供应",
                        "能源在生产的同时不断被消耗。如果另一位玩家短缺而您有盈余，您可以将一部分能源发送给他们。您可以稍后取消这些指令。每个帝国最多只能额外接受200万能源。", null, null ) );

                    options.Add( ArbitaryArcenOption.Create(
                        "Hacking_OneTime", "一次性入侵赠礼",
                        "立即向另一位玩家赠送一定数量的入侵点数。入侵点数是有限的，但有时您需要集中资源才能共同执行您想要的入侵。这与科学完全不同，科学是每个帝国独有的，永远无法赠送。", null, null ) );
                }
                return options;
            }
        }

        public class dPlayerGiftTarget : dCustomDropdown
        {
            public static dPlayerGiftTarget MyInstance;
            public dPlayerGiftTarget()
            {
                MyInstance = this;
            }
            protected override StringTrioBasedDropdownOption ConstructOptionFor( IOption value )
            {
                return new CustomDropdownOption( value );
            }

            private List<IOption> options = List<IOption>.Create_WillNeverBeGCed( 40, "dPlayerGiftTarget-options" );
            private int lastFactionIndex = -2;
            public override List<IOption> GetOptions()
            {
                if ( options.Count != World_AIW2.Instance.EmpireStylePlayerFactions.Count - 1 ||
                    lastFactionIndex != Instance.CurrentFactionArrayIndexNonliteral )
                {
                    lastFactionIndex = Instance.CurrentFactionArrayIndexNonliteral;
                    options.Clear( true );
                    foreach ( Faction fac in World_AIW2.Instance.EmpireStylePlayerFactions )
                    {
                        if ( fac.FactionIndex == Instance.CurrentFactionArrayIndexNonliteral )
                            continue; //don't list self
                        options.Add( ArbitaryArcenOption.Create(
                            fac.FactionIndex.ToString(), fac.GetDisplayName(), fac.GetDisplayName() + " 将收到赠礼。", null, null ) );
                    }
                }

                return options;
            }
        }

        public class iPlayerGiftAmount : InputAbstractBase
        {
            public static iPlayerGiftAmount MyInstance;
            public iPlayerGiftAmount()
            {
                MyInstance = this;
            }
            public int lastValue = 0;
            public override void HandleChangeInValue( string NewValue )
            {
                int newValueAsInt;
                if ( !Int32.TryParse( NewValue, out newValueAsInt ) )
                    return;
                if ( newValueAsInt < 0 )
                    return;
                lastValue = newValueAsInt;
            }

            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                if ( input.Length >= 10 )
                    return '\0';
                if ( !char.IsDigit( addedChar ) )
                    return '\0';

                string testInput = input.Insert( charIndex, addedChar.ToString() );
                int testInputAsType;
                if ( !Int32.TryParse( testInput, out testInputAsType ) )
                    return '\0';

                return addedChar;
            }

            public override void OnUpdate()
            {
                if ( !this.GetIsCurrentlyBeingEdited() )
                {
                    ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;
                    elementAsType.SetText( this.lastValue.ToString() );
                }
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

            public override void HandleMouseover()
            {
                
            }
        }

        public class bPlayerGiftSelection : ButtonAbstractBase
        {
            
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "赠礼" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                IOption giftType = dPlayerGiftType.MyInstance.GetSelected();
                if ( giftType == null )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "无法赠礼", "未选择赠礼类型。", "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                IOption giftTarget = dPlayerGiftTarget.MyInstance.GetSelected();
                if ( giftTarget == null || giftTarget.GetInternalName() == null || giftTarget.GetInternalName().Length == 0 )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "无法赠礼", "未选择目标阵营。", "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                int giftTargetFactionIndex = Int32.Parse( giftTarget.GetInternalName() );

                int giftAmount = iPlayerGiftAmount.MyInstance.lastValue;
                if ( giftAmount <= 0 )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "无法赠礼", "赠礼数量必须大于零。", "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                switch (giftType.GetInternalName() )
                {
                    case "Metal_PerSecond":
                        if ( giftAmount > 25000 )
                        {
                            ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "无法赠礼", "每个帝国每秒最多只能接受25,000金属。", "确定" );
                            return MouseHandlingResult.PlayClickDeniedSound;
                        }
                        break;
                    case "Energy_Ongoing":
                        if ( giftAmount > 2000000 )
                        {
                            ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "无法赠礼", "每个帝国最多只能额外接受200万能源。", "确定" );
                            return MouseHandlingResult.PlayClickDeniedSound;
                        }
                        break;
                }

                ModalPopupData.CreateAndLogYesNoStyle( delegate
                {
                    GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.AfterGameStartOnly_RequestFactionChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    command.RelatedString = "FactionGift";
                    command.RelatedString2 = giftType.GetInternalName();
                    //command.RelatedString3 = giftType.SecondItem;
                    command.RelatedIntegers.Add( Instance.CurrentFactionArrayIndexNonliteral );
                    command.RelatedIntegers2.Add( giftAmount );
                    command.RelatedIntegers3.Add( giftTargetFactionIndex );
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                    //shut the window so that we can see the results
                    Instance.Close();

                }, null, giftType.GetDisplayName(), "向 " + giftTarget.GetDisplayName() + " 赠送 <color=#e8ff50>" +
                    giftType.GetDisplayName() + "</color>，数量为 " + giftAmount.ToString( "#,##0" ) + "？", "确认赠送", "取消" );

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, "点击此处执行您指定的赠礼。" );
            }
        }
        #endregion

        #region Player Metal Ongoing Gifts
        public class tMetalGiftDisplay : TextAbstractBase
        {
            public int GetIndex()
            {
                return this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                Faction faction = World_AIW2.Instance.GetFactionByIndex( Instance.CurrentFactionArrayIndexNonliteral );
                if ( faction == null )
                    buffer.Add( "源阵营为空 " ).Add( Instance.CurrentFactionArrayIndexNonliteral );
                else
                {
                    int index = GetIndex();
                    if ( index < 0 || index >= faction.MetalGiftsFromThisPlayer.Count )
                        buffer.Add( "金属赠礼索引超出范围：" ).Add( index ).Add( "/" ).Add( faction.MetalGiftsFromThisPlayer.Count );
                    else
                    {
                        var metalGift = faction.MetalGiftsFromThisPlayer[index];
                        Faction targetFaction = World_AIW2.Instance.GetFactionByIndex( metalGift.Key );
                        if ( targetFaction == null )
                            buffer.Add( "目标阵营为空 " ).Add( metalGift.Key );
                        else
                            buffer.Add( "正在向 " ).Add( targetFaction.GetDisplayName() ).Add( " 发送每秒 " ).AddNumberMoreReadable( metalGift.Value ).Add( " 金属" );
                    }
                }
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, "这些金属会在您所有其他支出之后发送给目标阵营，因此如果您当前负担不起，只会发送较少的数量。您的金属支出将始终优先于发送给其他玩家的额外金属。" );
            }
        }

        public class bMetalGiftRemoval : ButtonAbstractBase
        {
            public int GetIndex()
            {
                return this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "清除" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Faction faction = World_AIW2.Instance.GetFactionByIndex( Instance.CurrentFactionArrayIndexNonliteral );
                if ( faction == null )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "无法清除", "源阵营为空 " + Instance.CurrentFactionArrayIndexNonliteral, "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                else
                {
                    int index = GetIndex();
                    if ( index < 0 || index >= faction.MetalGiftsFromThisPlayer.Count )
                    {
                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "无法清除", "金属赠礼索引超出范围：" + index + "/" + faction.MetalGiftsFromThisPlayer.Count, "确定" );
                        return MouseHandlingResult.PlayClickDeniedSound;
                    }
                    else
                    {
                        var metalGift = faction.MetalGiftsFromThisPlayer[index];
                        Faction targetFaction = World_AIW2.Instance.GetFactionByIndex( metalGift.Key );
                        if ( targetFaction == null )
                        {
                            ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "无法清除", "目标阵营为空 " + metalGift.Key, "确定" );
                            return MouseHandlingResult.PlayClickDeniedSound;
                        }
                        else
                        {
                            //success
                            ModalPopupData.CreateAndLogYesNoStyle( delegate
                            {
                                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.AfterGameStartOnly_RequestFactionChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                                command.RelatedString = "FactionGift";
                                command.RelatedString2 = "ClearOngoingMetalGift";
                                command.RelatedIntegers.Add( Instance.CurrentFactionArrayIndexNonliteral );
                                command.RelatedIntegers2.Add( 1 );
                                command.RelatedIntegers3.Add( targetFaction.FactionIndex );
                                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                                //shut the window so that we can see the results
                                Instance.Close();

                            }, null, "清除持续金属赠礼？", "清除当前正在向 " + targetFaction.GetDisplayName() + " 发送的<color=#e8ff50>每秒 " + metalGift.Value.ToString( "#,##0" ) + 
                                " 金属的持续赠礼</color>？", "确认清除", "取消" );
                        }
                    }
                }

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, "点击此处清除此持续赠礼。" );
            }
        }
        #endregion

        #region Player Energy Ongoing Gifts
        public class tEnergyGiftDisplay : TextAbstractBase
        {
            public int GetIndex()
            {
                return this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                Faction faction = World_AIW2.Instance.GetFactionByIndex( Instance.CurrentFactionArrayIndexNonliteral );
                if ( faction == null )
                    buffer.Add( "源阵营为空 " ).Add( Instance.CurrentFactionArrayIndexNonliteral );
                else
                {
                    int index = GetIndex();
                    if ( index < 0 || index >= faction.EnergyGiftsFromThisPlayer.Count )
                        buffer.Add( "能源赠礼索引超出范围：" ).Add( index ).Add( "/" ).Add( faction.EnergyGiftsFromThisPlayer.Count );
                    else
                    {
                        var EnergyGift = faction.EnergyGiftsFromThisPlayer[index];
                        Faction targetFaction = World_AIW2.Instance.GetFactionByIndex( EnergyGift.Key );
                        if ( targetFaction == null )
                            buffer.Add( "目标阵营为空 " ).Add( EnergyGift.Key );
                        else
                            buffer.Add( "正在向 " ).Add( targetFaction.GetDisplayName() ).Add( " 持续提供 " ).AddNumberMoreReadable( EnergyGift.Value ).Add( " 能源" );
                    }
                }
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, "这些能源会在您现有的能源需求得到满足后发送给目标阵营。如果您没有足够的剩余能源可以发送而不变为负数，将会发送较少的数量。您的能源需求将始终优先于发送给其他玩家的额外能源。" );
            }
        }

        public class bEnergyGiftRemoval : ButtonAbstractBase
        {
            public int GetIndex()
            {
                return this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "清除" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Faction faction = World_AIW2.Instance.GetFactionByIndex( Instance.CurrentFactionArrayIndexNonliteral );
                if ( faction == null )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "无法清除", "源阵营为空 " + Instance.CurrentFactionArrayIndexNonliteral, "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                else
                {
                    int index = GetIndex();
                    if ( index < 0 || index >= faction.EnergyGiftsFromThisPlayer.Count )
                    {
                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "无法清除", "能源赠礼索引超出范围：" + index + "/" + faction.EnergyGiftsFromThisPlayer.Count, "确定" );
                        return MouseHandlingResult.PlayClickDeniedSound;
                    }
                    else
                    {
                        var energyGift = faction.EnergyGiftsFromThisPlayer[index];
                        Faction targetFaction = World_AIW2.Instance.GetFactionByIndex( energyGift.Key );
                        if ( targetFaction == null )
                        {
                            ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "无法清除", "目标阵营为空 " + energyGift.Key, "确定" );
                            return MouseHandlingResult.PlayClickDeniedSound;
                        }
                        else
                        {
                            //success
                            ModalPopupData.CreateAndLogYesNoStyle( delegate
                            {
                                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.AfterGameStartOnly_RequestFactionChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                                command.RelatedString = "FactionGift";
                                command.RelatedString2 = "ClearOngoingEnergyGift";
                                command.RelatedIntegers.Add( Instance.CurrentFactionArrayIndexNonliteral );
                                command.RelatedIntegers2.Add( 1 );
                                command.RelatedIntegers3.Add( targetFaction.FactionIndex );
                                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                                //shut the window so that we can see the results
                                Instance.Close();

                            }, null, "清除持续能源赠礼？", "清除当前正在向 " + targetFaction.GetDisplayName() + " 提供的<color=#e8ff50>每秒 " + energyGift.Value.ToString( "#,##0" ) +
                                " 能源的持续赠礼</color>？", "确认清除", "取消" );
                        }
                    }
                }

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, "点击此处清除此持续赠礼。" );
            }
        }
        #endregion

        private readonly List<Faction> lastOtherFactionsToShow = List<Faction>.Create_WillNeverBeGCed( 30, "Window_FactionsWindow-lastOtherFactionsToShow" );

        #region PopulateSubclassControls_ForOtherFactionsList
        protected void PopulateSubclassControls_ForOtherFactionsList( ArcenUI_SetOfCreateElementDirectives Set, ref float runningY )
        {
            Rect nameBounds;
            Rect valueBounds;
            Rect explainBounds;
            
            if ( Mapgen.IsMapCurrentlyGenerating )
                return; //hide while regenerating, to make it refresh

            Instance.getFactionsToShowInOtherFactionsList( lastOtherFactionsToShow, true );
            for ( int i = 0; i < lastOtherFactionsToShow.Count; i++ )
            {
                Faction factionToDisplay = lastOtherFactionsToShow[i];
                if ( factionToDisplay == null || factionToDisplay.SpecialFactionData.Type == FactionType.NaturalObject )
                    continue;

                //don't do any of this here
                //ConfigurationForFaction configForFac = GetConfigurationForFactionInUse( -1 ); //not for "other factions"
                //if ( customField.IsAdvancedSetting && !shouldShowAdvancedSettings )
                //{
                //    if ( customField.GetIsTempValueMatchingDefault( configForFac ) ) //only hide advanced rows that match
                //    {
                //        //if ( !isInSandboxMode || setting.GetIsDefaultValueAlteredByHarshness() )
                //        //{ } //DO draw these!
                //        //else
                //        {
                //            numberAdvancedHidden++;
                //            continue;
                //        }
                //    }
                //}

                //TeamColor for all factions here
                this.CalculateBoundsTriple( out nameBounds, out valueBounds, out explainBounds, ref runningY, nameWidth, valueWidth, explainWidth );
                AddText( Set, type_tSettingNameDirect, factionToDisplay.GetDisplayNameWithoutPlayerNames() + " 阵营颜色", i, i, nameBounds, 12f );
                valueBounds.width = valueBounds.height;
                AddButtonCustom( "ColorPickerButton", Set, type_bTeamColor, "TeamColor", i, i, valueBounds, -1f );
            }

            //bMainContentParent.ParentRT.UI_SetHeight( runningY );
        }
        #endregion

        #region GetCurrentFaction
        public static Faction GetCurrentFaction( ArcenUI_Element Element )
        {
            return GetCurrentFaction( Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag2 );
        }

        public static Faction GetCurrentFaction( int OptionalIndexInOtherFactionsArray )
        {
            if ( Instance == null )
                return null;
            if ( Instance.CurrentFactionArrayIndexNonliteral == OTHER_FACTIONS_INDEX ) //if we're not showing a specific faction on the sidebar, find the faction from our list
            {
                if ( Instance.lastOtherFactionsToShow == null || OptionalIndexInOtherFactionsArray < 0 ||
                    OptionalIndexInOtherFactionsArray >= Instance.lastOtherFactionsToShow.Count )
                    return null;
                return Instance.lastOtherFactionsToShow[OptionalIndexInOtherFactionsArray];
            }
            if ( Instance.CurrentFactionArrayIndexNonliteral < 0 || Instance.CurrentFactionArrayIndexNonliteral >= World_AIW2.Instance.Factions.Count )
                return null;

            Faction factionToDisplay = World_AIW2.Instance.Factions[Instance.CurrentFactionArrayIndexNonliteral];
            return factionToDisplay;
        }
        #endregion

        #region GetCurrentFactionConfig
        public static ConfigurationForFaction GetCurrentFactionConfig( ArcenUI_Element Element )
        {
            return GetCurrentFactionConfig( Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag2 );
        }

        public static ConfigurationForFaction GetCurrentFactionConfig( int OptionalIndexInOtherFactionsArray )
        {
            if ( Instance == null )
                return null;
            if ( Instance.CurrentFactionArrayIndexNonliteral == OTHER_FACTIONS_INDEX ) //if we're not showing a specific faction on the sidebar, find the faction from our list
            {
                if ( Instance.lastOtherFactionsToShow == null || OptionalIndexInOtherFactionsArray < 0 ||
                    OptionalIndexInOtherFactionsArray >= Instance.lastOtherFactionsToShow.Count )
                {
                    Faction faction = Instance.lastOtherFactionsToShow[OptionalIndexInOtherFactionsArray];
                    return World_AIW2.Instance.Setup.FactionConfigurations[faction.FactionIndex];
                }
            }
            if ( Instance.CurrentFactionArrayIndexNonliteral < 0 || Instance.CurrentFactionArrayIndexNonliteral >= World_AIW2.Instance.Factions.Count )
                return null;

            return World_AIW2.Instance.Setup.FactionConfigurations[Instance.CurrentFactionArrayIndexNonliteral];
        }
        #endregion

        #region GetCurrentFactionIndex
        public static int GetCurrentFactionIndex( ArcenUI_Element Element )
        {
            return GetCurrentFactionIndex( Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag2 );
        }

        public static int GetCurrentFactionIndex( int OptionalIndexInOtherFactionsArray )
        {
            Faction factionToDisplay = GetCurrentFaction( OptionalIndexInOtherFactionsArray );
            if ( factionToDisplay == null )
                return -1;
            return factionToDisplay.FactionIndex;
        }
        #endregion

        #region GetCustomFieldForController
        public static DataForFaction_CustomFieldDefinition GetCustomFieldForController( ElementAbstractBase controller )
        {
            if ( Instance == null )
                return null;

            Faction factionToDisplay = GetCurrentFaction( controller.Element );
            if ( factionToDisplay == null )
                return null;
            int fieldIndex = controller.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            if ( fieldIndex < 0 || fieldIndex >= factionToDisplay.SpecialFactionData.CustomFields.Count )
                return null;

            return factionToDisplay.SpecialFactionData.CustomFields[fieldIndex];
        }
        #endregion

        #region GetConfigurationForFactionInUse
        public static ConfigurationForFaction GetConfigurationForFactionInUse( int OptionalIndexInOtherFactionsArray )
        {
            if ( Instance == null )
                return null;
            if ( Instance.isSkippingDrawingAnythingThisFrameWhileDataFieldsSwitchOver )
                return null;

            return GetCurrentFactionConfig( OptionalIndexInOtherFactionsArray );
        }
        #endregion

        #region GetCurrentStringForCustomField
        public static string GetCurrentStringForCustomField( DataForFaction_CustomFieldDefinition ForField, ArcenUI_Element Element )
        {
            return GetCurrentStringForCustomField( ForField, Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag2 );
        }

        public static string GetCurrentStringForCustomField( DataForFaction_CustomFieldDefinition ForField, int OptionalIndexInOtherFactionsArray )
        {
            if ( Instance == null || ForField == null )
                return string.Empty;
            if ( Instance.isSkippingDrawingAnythingThisFrameWhileDataFieldsSwitchOver )
                return string.Empty;

            ConfigurationForFaction factionToDisplay = GetCurrentFactionConfig( OptionalIndexInOtherFactionsArray );
            if ( factionToDisplay == null )
                return string.Empty;
            return factionToDisplay.GetStringValueForCustomFieldOrDefaultValue( ForField.InternalName, false );
        }
        #endregion

        #region GetCurrentIntForCustomField
        public static int GetCurrentIntForCustomField( DataForFaction_CustomFieldDefinition ForField, ArcenUI_Element Element )
        {
            return GetCurrentIntForCustomField( ForField, Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag2 );
        }

        public static int GetCurrentIntForCustomField( DataForFaction_CustomFieldDefinition ForField, int OptionalIndexInOtherFactionsArray )
        {
            if ( Instance == null || ForField == null )
                return -1;
            if ( Instance.isSkippingDrawingAnythingThisFrameWhileDataFieldsSwitchOver )
                return -1;
            ConfigurationForFaction factionToDisplay = GetCurrentFactionConfig( OptionalIndexInOtherFactionsArray );
            if ( factionToDisplay == null )
                return -1;
            return factionToDisplay.GetIntValueForCustomFieldOrDefaultValue( ForField.InternalName, false );
        }
        #endregion

        #region SetCurrentStringForCustomField
        public static void SetCurrentStringForCustomField( DataForFaction_CustomFieldDefinition ForField, ArcenUI_Element Element, string Value )
        {
            SetCurrentStringForCustomField( ForField, Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag2, Value );
        }

        public static void SetCurrentStringForCustomField( DataForFaction_CustomFieldDefinition ForField, int OptionalIndexInOtherFactionsArray, string Value )
        {
            if ( Instance == null || ForField == null )
                return;
            if ( Instance.isSkippingDrawingAnythingThisFrameWhileDataFieldsSwitchOver )
                return;
            ConfigurationForFaction factionToDisplay = GetCurrentFactionConfig( OptionalIndexInOtherFactionsArray );
            if ( factionToDisplay == null )
                return;
            factionToDisplay.SetCustomFieldValue(ForField.InternalName, Value );
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
                    buffer.Add( "<size=125%>空子分类！" );
                else
                    buffer.Add( "<size=125%>" ).StartColor( subCat.Color ).Add( subCat.DisplayName );
            }

            public override void HandleMouseover()
            {
                ArcenSettingSubcategory subCat = this.GetSubcategory();
                if ( subCat == null )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, "空子分类！" );
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

                ConfigurationForFaction configForFac = GetConfigurationForFactionInUse( Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag2 );

                bool fixedByCampaign = field.GetIsDefaultValueAlteredByHarshness();
                if ( !field.GetIsTempValueMatchingDefault( configForFac ) )
                    buffer.StartColor( "ffde85" );
                else if ( fixedByCampaign )
                    buffer.StartColor( "b7ffbc" );

                buffer.Add( field.DisplayName );
                
                var len = buffer.Builder.Length;
                buffer.AddDlcMod(field);
                var wroteAnotherLine = buffer.Builder.Length > len;
                
                if ( fixedByCampaign )
                    buffer.EndColor().StartColor( "85ffa2" ).Add( wroteAnotherLine ? "  " : "\n" ).Add( "<size=50%>(ALTERED BY CAMPAIGN TYPE OR AI DIFFICULTY)" ).EndColor();
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_FactionsWindow-tSettingName-tooltipBuffer" );
            public override void HandleMouseover()
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;

                tooltipBuffer.Add( field.DisplayName ).Add( "\n" ).Add( field.Description );
                tooltipBuffer.AddDlcMod(field, "此阵营设置由以下内容添加：" );
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
                if ( Instance.CurrentFactionArrayIndexNonliteral == OTHER_FACTIONS_INDEX )
                {
                    Faction faction = GetCurrentFaction( this.Element );
                    if ( faction == null )
                        return;
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, faction.GetDisplayNameWithoutPlayerNames() + "\n" + faction.SpecialFactionData.Description );
                }
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
                            int tempValue = GetCurrentIntForCustomField( field, this.Element );
                            buffer.AddNumberMoreReadable( tempValue );
                            //buffer.Add( "       " ).Add( FontSizes.SLIGHTLY_SMALLER_SIZE_V2_STRING ).Add( "<color=#999999>(" ).Add( field.GetMinValue_Int() );
                            //if ( field.GetMaxValue_Int() > 0 && field.GetMaxValue_Int() > field.GetMinValue_Int() )
                            //    buffer.Add( " to " ).AddNumberMoreReadable( field.GetMaxValue_Int() );
                            //buffer.Add( ")" );
                        }
                        break;
                    case DataForFaction_SettingType.IntTextbox:
                        {
                            int valueAsInt = GetCurrentIntForCustomField( field, this.Element );
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
                    " 个高级字段，请点击此处临时显示它们。\n\n如需长期查看，请切换到ESC菜单中的银河选项屏幕，点击'显示高级银河和阵营选项'按钮。" );
            }
        }

        #region bToggle and related
        public class bToggle : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;

                buffer.Add( GetCurrentIntForCustomField( field, this.Element ) > 0 ? "开" : "<color=#666666>关" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return MouseHandlingResult.None;

                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.AfterGameStartOnly_RequestFactionChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "DataForFaction_CustomFieldDefinitionChanged";
                command.RelatedString2 = field.InternalName;
                command.RelatedString3 = (GetCurrentIntForCustomField( field, this.Element ) > 0 ? 0 : 1).ToString();
                command.RelatedIntegers.Add( GetCurrentFactionIndex( this.Element ) );
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

                buffer.Add( GetCurrentIntForCustomField( field, this.Element ) > 0 ? "开" : "<color=#666666>关" );
            }

            public override void HandleMouseover()
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                if ( field.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, field.Description + field.GetAddedTextForCampaignType() );
            }
        }
        #endregion

        public class bTeamColor : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Faction faction = GetCurrentFaction( this.Element );

                Window_TeamColorPicker.Instance.Open( faction.Config,
                    delegate ( TeamColorDefinition CenterColor, TeamColorDefinition TrimColor )
                    {
                        if ( faction.FactionCenterColor != CenterColor )
                        {
                            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.AfterGameStartOnly_RequestFactionChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                            command.RelatedString = "bTeamColor";
                            command.RelatedString2 = "FactionCenterColor";
                            command.RelatedString3 = CenterColor.InternalName;
                            command.RelatedIntegers.Add( GetCurrentFactionIndex( this.Element ) );
                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                        }
                        if ( faction.FactionTrimColor != TrimColor )
                        {
                            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.AfterGameStartOnly_RequestFactionChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                            command.RelatedString = "bTeamColor";
                            command.RelatedString2 = "FactionTrimColor";
                            command.RelatedString3 = TrimColor.InternalName;
                            command.RelatedIntegers.Add( GetCurrentFactionIndex( this.Element ) );
                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                        }
                    }, true );
                return MouseHandlingResult.None;
            }

            public override void DoAnyCustomButtonStuffFromVolatile( ArcenUI_Button Button )
            {
                Faction faction = GetCurrentFaction( this.Element );
                if ( faction != null && faction.FactionCenterColor != null && Button.RelatedImages.Length >= 1 )
                    Button.RelatedImages[0].color = faction.FactionCenterColor.TeamColor;
                if ( faction != null && faction.FactionTrimColor != null && Button.RelatedImages.Length >= 2 )
                    Button.RelatedImages[1].color = faction.FactionTrimColor.TeamColor;
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "点击编辑此阵营的中心和边框颜色。" );
            }
        }

        public class bTeamColor_TrimOnly : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( Instance.isSkippingDrawingAnythingThisFrameWhileDataFieldsSwitchOver )
                    return MouseHandlingResult.None;
                Faction faction = GetCurrentFaction( this.Element );

                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                string currentValue = GetCurrentStringForCustomField( field, this.Element );
                if ( currentValue == null || currentValue.Length == 0 )
                    currentValue = field.GetDefaultValue_String();
                TeamColorDefinition colorDef = TeamColorDefinitionTable.Instance.GetRowByNameOrNullIfNotFound( currentValue );

                Window_TeamColorPickerTrimOnly.Instance.Open( faction.FactionCenterColor, colorDef,
                    delegate ( TeamColorDefinition TrimColor )
                    {
                        if ( colorDef != TrimColor )
                        {
                            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.AfterGameStartOnly_RequestFactionChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                            command.RelatedString = "DataForFaction_CustomFieldDefinitionChangedValidForDuringGame";
                            command.RelatedString2 = field.InternalName;
                            command.RelatedString3 = TrimColor.InternalName;
                            command.RelatedIntegers.Add( GetCurrentFactionIndex( this.Element ) );
                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                        }
                    } );
                return MouseHandlingResult.None;
            }

            public override void DoAnyCustomButtonStuffFromVolatile( ArcenUI_Button Button )
            {
                if ( Instance.isSkippingDrawingAnythingThisFrameWhileDataFieldsSwitchOver )
                    return;
                Faction faction = GetCurrentFaction( this.Element );
                if ( faction != null && faction.FactionCenterColor != null && Button.RelatedImages.Length >= 1 )
                    Button.RelatedImages[0].color = faction.FactionCenterColor.TeamColor;

                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                string currentValue = GetCurrentStringForCustomField( field, this.Element );
                if ( currentValue == null || currentValue.Length == 0 )
                    currentValue = field.GetDefaultValue_String();
                TeamColorDefinition colorDef = TeamColorDefinitionTable.Instance.GetRowByNameOrNullIfNotFound( currentValue );
                if ( colorDef != null && Button.RelatedImages.Length >= 2 )
                    Button.RelatedImages[1].color = colorDef.TeamColor;
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "点击编辑此子阵营的边框颜色。它将继续与主阵营共享相同的主色调。" );
            }
        }

        public class iFactionNameInput : InputAbstractBase
        {
            public override void OnEndEdit()
            {
                ConfigurationForFaction config = GetCurrentFactionConfig( this.Element );
                if ( config == null ) return;

                ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;
                string NewValue = elementAsType.GetText();

                ArcenDebugging.ArcenDebugLogSingleLine( "New: " + NewValue, Verbosity.DoNotShow );

                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.AfterGameStartOnly_RequestFactionChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "FactionNameChanged";
                command.RelatedString3 = NewValue;
                command.RelatedIntegers.Add( GetCurrentFactionIndex( this.Element ) );
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
                ConfigurationForFaction config = GetCurrentFactionConfig( this.Element );
                if ( config == null ) return;

                if ( !ArcenInput.CalculateIsInputFieldFocused() )
                {
                    ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;
                    elementAsType.SetText( config.FactionNameOrEmpty == null ? string.Empty : config.FactionNameOrEmpty );
                }
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "如果留空，将使用第一个控制此阵营的玩家名称。如果您希望您的帝国拥有与个人名称不同的名称，可以在此处设置。如果多个玩家共享一个阵营，他们可以为其取一个代表共同利益的名称。" );
            }
        }

        #region iIntInput and alt
        public class iIntInput : InputAbstractBase
        {
            public override void HandleChangeInValue( string NewValue )
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;

                int newValueAsInt;
                if ( !Int32.TryParse( NewValue, out newValueAsInt ) )
                    return;
                if ( newValueAsInt == GetCurrentIntForCustomField( field, this.Element ) )
                    return;
                if ( newValueAsInt < field.GetMinValue_Int() )
                    return;
                if ( field.GetMaxValue_Int() > field.GetMinValue_Int() && newValueAsInt > field.GetMaxValue_Int() )
                    return;

                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.AfterGameStartOnly_RequestFactionChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "DataForFaction_CustomFieldDefinitionChanged";
                command.RelatedString2 = field.InternalName;
                command.RelatedString3 = newValueAsInt.ToString();
                command.RelatedIntegers.Add( GetCurrentFactionIndex( this.Element ) );
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

                //only update to the current value if we're not editing this field right now
                if ( !this.GetIsCurrentlyBeingEdited() )
                {
                    ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;
                    elementAsType.SetText( GetCurrentIntForCustomField( field, this.Element ).ToString() );
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

                buffer.Add( GetCurrentIntForCustomField( field, this.Element ) );
            }

            public override void HandleMouseover()
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                if ( field.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, field.Description + field.GetAddedTextForCampaignType() );
            }
        }
        #endregion

        #region sIntSlider and alt
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

                int currentValue = GetCurrentIntForCustomField( field, this.Element );
                int range = field.GetMaxValue_Int() - field.GetMinValue_Int();
                if ( range == 0 ) range = 1;
                float currentPortion = (float)(currentValue - field.GetMinValue_Int()) / (float)range;

                ArcenUI_Slider elementAsType = (ArcenUI_Slider)this.Element;
                elementAsType.ReferenceSlider.value = currentPortion;

                if ( hasNewValueBeenSetButNotSent && Engine_Universal.CumulativeUnscaledTime - lastTimeOfOnChanged >= MIN_TIME_BETWEEN_SENDS )
                {
                    hasNewValueBeenSetButNotSent = false;
                    GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.AfterGameStartOnly_RequestFactionChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    command.RelatedString = "DataForFaction_CustomFieldDefinitionChanged";
                    command.RelatedString2 = field.InternalName;
                    command.RelatedString3 = newValueIs.ToString();
                    command.RelatedIntegers.Add( GetCurrentFactionIndex( this.Element ) );
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                }
            }

            public override void OnChange( float NewValue )
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;

                lastTimeOfOnChanged = Engine_Universal.CumulativeUnscaledTime;

                int range = field.GetMaxValue_Int() - field.GetMinValue_Int();
                int adjustedNewValue = field.GetMinValue_Int() + Mathf.RoundToInt( range * NewValue );

                if ( adjustedNewValue == GetCurrentIntForCustomField( field, this.Element ) )
                    return;

                newValueIs = adjustedNewValue;
                hasNewValueBeenSetButNotSent = true;
                //this is an MP desync, but keeps the interface responsive.  It will be synced up within a part of a second.
                SetCurrentStringForCustomField( field, this.Element, adjustedNewValue.ToString() );
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

                buffer.Add( GetCurrentIntForCustomField( field, this.Element ) );
                buffer.Add( "    <color=#999999>(" ).Add( field.GetMinValue_Int() ).Add( " 至 " ).Add( field.GetMaxValue_Int() ).Add( ")" );
            }

            public override void HandleMouseover()
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                if ( field.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, field.Description + field.GetAddedTextForCampaignType() );
            }
        }
        #endregion

        public abstract class StringBasedCustomDropdown : DropdownAbstractBase
        {
            private string SelectedItemAsType
            {
                get
                {
                    DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                    if ( field == null ) return string.Empty;

                    string currentValue = GetCurrentStringForCustomField( field, this.Element );
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

                if ( this is dPlayerGiftType )
                    return;
                if ( this is dPlayerGiftTarget )
                    return;

                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) return;
                string oldValue = GetCurrentStringForCustomField( field, this.Element );
                if ( oldValue == Item.OptionValue.GetInternalName() )
                    return; //no change!

                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.AfterGameStartOnly_RequestFactionChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "DataForFaction_CustomFieldDefinitionChanged";
                command.RelatedString2 = field.InternalName;
                command.RelatedString3 = Item.OptionValue.GetInternalName();
                command.RelatedIntegers.Add( GetCurrentFactionIndex( this.Element ) );
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
            }

            public override void OnUpdate()
            {
                this.shouldIgnoreChanges = true;
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                int newFactionIndex = GetCurrentFactionIndex( this.Element );
                if ( lastShownFactionIndex != newFactionIndex )
                {
                    lastShownFactionIndex = newFactionIndex;
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

            public IOption GetSelected()
            {
                int selectedIndex = (this.Element as ArcenUI_Dropdown).GetSelectedIndex();
                List<IOption> currentOptions = this.GetOptions();
                if ( selectedIndex <= 0 || selectedIndex >= currentOptions.Count )
                    selectedIndex = 0;
                if ( currentOptions.Count <= 0 )
                    return null;
                return currentOptions[selectedIndex];
            }

            public string GetSelectedFirstItemOrEmptyString()
            {
                string result = this.GetSelected()?.GetInternalName();
                if ( result == null )
                    return string.Empty;
                return result;
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

        public class tAltFor_dCustomDropdown : TextAbstractBase
        {
            private string SelectedItemAsType
            {
                get
                {
                    DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                    if ( field == null ) return string.Empty;

                    string currentValue = GetCurrentStringForCustomField( field, this.Element );
                    return currentValue;
                }
            }

            private static List<IOption> emptyList = List<IOption>.Create_WillNeverBeGCed( 1, "tAltFor_dCustomDropdown-emptyList" );
            public List<IOption> GetOptions()
            {
                DataForFaction_CustomFieldDefinition field = GetCustomFieldForController( this );
                if ( field == null ) 
                    return emptyList;
                return field.GetOptions();
            }

            private string lastDisplayStringAsType = string.Empty;
            private IOption lastDisplayStringForDisplay = null;
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                int debugStage = 0;
                try
                {
                    string selectedString = this.SelectedItemAsType;
                    debugStage = 100;
                    if ( this.lastDisplayStringAsType != selectedString )
                    {
                        this.lastDisplayStringAsType = selectedString;

                        debugStage = 200;
                        List<IOption> options = this.GetOptions();
                        debugStage = 300;
                        if ( options != null )
                        {
                            for ( int i = 0; i < options.Count; i++ )
                            {
                                IOption optionValue = options[i];
                                debugStage = 400;
                                if ( optionValue.GetInternalName() == selectedString )
                                {
                                    debugStage = 500;
                                    this.lastDisplayStringForDisplay = optionValue;
                                    break;
                                }
                            }
                        }
                        debugStage = 600;
                    }

                    debugStage = 700;
                    if ( this.lastDisplayStringForDisplay != null )
                        buffer.Add( this.lastDisplayStringForDisplay.GetDisplayName() );
                    else
                        buffer.Add( selectedString );
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "tAltFor_dCustomDropdown GetTextToShowFromVolatile: Error at debug stage " + debugStage +
                                                  "\n" + e, Verbosity.ShowAsError );
                }
            }

            public override void HandleMouseover()
            {
                DataForFaction_CustomFieldDefinition setting = GetCustomFieldForController( this );
                if ( setting == null ) return;
                tooltipBuffer.Add( setting.DisplayName ).Add( "\n" );
                tooltipBuffer.Add( setting.Description );
                if ( this.lastDisplayStringForDisplay != null )
                {
                    if ( this.FillMouseoverPartsForItem( setting.Description.Length > 0 ) )
                        tooltipBuffer.Add( setting.GetAddedTextForCampaignType() );
                    //now draw it
                    this.FinishAndDisplayTooltipFromBuffer( this.Element );
                }
            }

            public static readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_FactionsWindow-tAltFor_dCustomDropdown-tooltipBuffer" );

            public bool FillMouseoverPartsForItem( bool AddDoubleNewline )
            {
                IOption optionValue = this.lastDisplayStringForDisplay;
                if ( optionValue == null )
                    return false;

                if ( AddDoubleNewline )
                    tooltipBuffer.Add( "\n\n" );

                tooltipBuffer.Add( "<b>" ).Add( optionValue.GetDisplayName() ).Add( "</b>\n" ).Add( optionValue.AddDescription );
                tooltipBuffer.AddDlcMod(optionValue, "此选项由以下内容添加：" );
                return true;
            }

            public void FinishAndDisplayTooltipFromBuffer( IArcenUIElementForSizing Element )
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }
        #endregion

        public class bClose : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }
        }

        public override void Close()
        {
            base.Close();
        }

        #region bAddPlayerSlot
        public class bAddPlayerSlot : ButtonAbstractBase
        {
            public static bAddPlayerSlot Instance;

            public bAddPlayerSlot()
            {
                Instance = this;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "添加阵营" );
            }

            private static ProtectedList<CustomPopupData> factionOptions = ProtectedList<CustomPopupData>.Create_WillNeverBeGCed( 20, "Window_FactionsWindow-bAddPlayerSlot-factionOptions" );

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                #region Fill factionOptions
                for ( int i = 0; i < SpecialFactionDataTable.Instance.Rows.Count; i++ )
                {
                    SpecialFactionData row = SpecialFactionDataTable.Instance.Rows[i];
                    CustomPopupData option = CustomPopupData.GetFromPoolOrCreate();

                    bool shouldStillShowAtAll;
                    string Reason;

                    if ( Helper_GetShouldExcludeFaction( row, 999999999, out shouldStillShowAtAll, out Reason ) ) //by having an index that is higher than any faction, it will check them all for "must be only one" and similar
                    {
                        if ( !shouldStillShowAtAll )
                            continue;
                        option.CanBeSelected = false;
                        option.CannotBeSelectedReason = Reason;
                    }

                    option.DisplayName = row.GetDisplayName();
                    
                    if ( row.Type == FactionType.Player )
                        option.DisplayName = "额外人类玩家槽位";
                    else if ( row.Type == FactionType.AI )
                        option.DisplayName = "额外AI阵营";

                    option.InternalName = row.InternalName;
                    option.Tooltiptext = row.Description;

                    LOG.Msg("option {0} description:\n{1}", option.InternalName, option.Tooltiptext);
                    
                    factionOptions.Add( option );
                }
                #endregion

                factionOptions.Sort( static delegate ( CustomPopupData Left, CustomPopupData Right )
                {
                    return Left.DisplayName.CompareTo( Right.DisplayName );
                } );

                Window_PopupScrollingColumnButtonList.Instance.Open( "选择要添加的阵营", null, factionOptions,
                    delegate ( CustomPopupData Option )
                    {
                        if ( Option == null || !Option.CanBeSelected )
                            return;
                        GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.AfterGameStartOnly_RequestFactionChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                        command.RelatedString = "dFactionType_Add";
                        command.RelatedString2 = Option.InternalName;
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                    }, null );

                return MouseHandlingResult.None;
            }
            public override bool GetShouldBeHidden()
            {
                return true; //don't show for now!  CHRIS_TODO later when we want to add more players in mid-game
            }
        }
        #endregion

        //from IInputActionHandler
        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.InternalName )
            {
                case "OpenSystemMenu":
                    this.Close();
                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
            }
        }
    }
}
