using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameHoverEntityInfo : WindowControllerAbstractBase
    {
        public enum Mode
        {
            None,
            Build,
            SingleUnit,
            UnitGroupOnSidebar
        }

        public static Window_InGameHoverEntityInfo Instance;
        public Window_InGameHoverEntityInfo()
        {
            this.OnlyShowInGame = true;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true;
            Instance = this;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( InputCaching.CalculateShouldTooltipsBeSuppressed() )
                return false;
            if ( bPanel.Instance == null )
                return false;
            //if ( bPanel.Instance.GetShouldBeHidden() )
            //    return false;
            return true;
        }

        public static bool IsDrawing = false;

        public class bPanel : ImageButtonAbstractBase
        {
            public static bPanel Instance;
            public bPanel() { Instance = this; }

            public ArcenUI_ImageButton MyElement;
            private SubTextGroup SubTexts;
            private string NextTextToShow = string.Empty;
            private string WrappedNextTextToShow = string.Empty;
            private string LastTextToShow = string.Empty;
            private bool NeedsToResize = true;
            private float LastRequestedWidth;
            private float LastRequestedHeight;
            private bool hasSetCanvasOffset = false;

            public void ClearMyself()
            {
                this.LastTextToShow = string.Empty;
                this.NextTextToShow = string.Empty;
                this.NeedsToResize = true;
            }

            private ArcenDoubleCharacterBuffer textBuffer = new ArcenDoubleCharacterBuffer( "Window_InGameHoverEntityInfo-bPanel-textBuffer" );

            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup _SubImages, SubTextGroup _SubTexts )
            {
                if ( !this.GetShouldBeHidden() )
                {
                    textBuffer.EnsureResetForNextUpdate();
                    this.GetTextToRender( textBuffer );
                    string newText = textBuffer.GetStringAndResetForNextUpdate();

                    if ( newText == null )
                        newText = string.Empty;
                    if ( newText != this.LastTextToShow )
                    {
                        this.NextTextToShow = newText;
                        this.NeedsToResize = true;
                    }
                }

                Window_InGameHoverEntityInfo.Instance.myXPositionScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" );

                this.SubTexts = _SubTexts;

                bool isFromSidebar = false;
                switch ( GameEntity_Base.CurrentlyHoveredOverIsFromSidebarType )
                {
                    case FromSidebarType.Sidebar_MultipleUnits:
                    case FromSidebarType.Sidebar_SingleUnit:
                        isFromSidebar = true;
                        break;
                }

                if ( isFromSidebar )
                { }//    this.HandleMouseoverOfSidebarEntity( GameEntity_Base.CurrentlyHoveredOverIsFromSidebarType );
                else
                {
                    this.DoResizeIfNeeded();
                }

            }

            //because of... complicated factors... the delay is actually amplified more than we would expect based on actual time.
            //this lets us adjust back down to closer to the correct timing expectation, but it's still not perfect
            private const float OVERALL_CORRECTION_MULITPLIER = 0.2f;

            private float lastTimeWasHiddenByMerits = 0;

            private GameEntity_Squad lastEntityHoveredOver = null;
            private FleetMembership lastFleetHoveredOver = null;

            public override bool GetShouldBeHidden()
            {
                bool isOverallSuppressed = false;
                if ( InputCaching.CalculateShouldTooltipsBeSuppressed() )
                    isOverallSuppressed = true; //this is done this way rather than just returning "true" so that it doesn't mess up lastTimeWasHiddenByMerits

                bool isFromSidebar = false;
                switch ( GameEntity_Base.CurrentlyHoveredOverIsFromSidebarType )
                {
                    case FromSidebarType.Sidebar_MultipleUnits:
                    case FromSidebarType.Sidebar_SingleUnit:
                        isFromSidebar = true;
                        break;
                }

                float delayTime = GameSettings.Current.GetFloatBySetting( "MainViewTooltipDelay" ) * OVERALL_CORRECTION_MULITPLIER;
                if ( ArcenTime.TimeSinceStartF - lastTimeWasHiddenByMerits < delayTime )
                    isOverallSuppressed = true; //show delay

                if ( !isFromSidebar )
                {
                    GameEntity_Squad squadCurrentlyHoveredOver = GameEntity_Base.CurrentlyHoveredOver as GameEntity_Squad;
                    if ( squadCurrentlyHoveredOver != null )
                    {
                        lastFleetHoveredOver = null;
                        if ( lastEntityHoveredOver != squadCurrentlyHoveredOver )
                        {
                            lastEntityHoveredOver = squadCurrentlyHoveredOver;
                            if ( delayTime > 0 )
                            {
                                lastTimeWasHiddenByMerits = ArcenTime.TimeSinceStartF;
                                return true; //something different is hovered-over now, so hide!
                            }
                        }
                        return isOverallSuppressed;
                    }
                    else
                        lastEntityHoveredOver = null;

                    if ( GameEntityTypeData.CurrentlyHoveredOver != null )
                    {
                        if ( lastFleetHoveredOver != GameEntityTypeData.CurrentlyHoveredOver )
                        {
                            lastFleetHoveredOver = GameEntityTypeData.CurrentlyHoveredOver;
                            if ( delayTime > 0 )
                            {
                                lastTimeWasHiddenByMerits = ArcenTime.TimeSinceStartF;
                                return true; //something different is hovered-over now, so hide!
                            }
                        }
                        return isOverallSuppressed;
                    }
                }

                lastEntityHoveredOver = null;
                lastFleetHoveredOver = null;
                lastTimeWasHiddenByMerits = ArcenTime.TimeSinceStartF;
                return true;
            }

            public override bool GetShouldRunUpdatesEvenWhenHidden()
            {
                return true;
            }

            public void UpdateTextIfNeeded()
            {
                string nextText = this.WrappedNextTextToShow;
                if ( this.LastTextToShow.Length <= 0 && nextText.Length <= 0 )
                {
                    this.ClearMyself();
                    return;
                }

                try
                {
                    if ( this.LastTextToShow != nextText )
                    {
                        this.LastTextToShow = nextText;
                        ArcenDoubleCharacterBuffer buffer = this.SubTexts[0].Text.StartWritingToBuffer();
                        buffer.Add( this.LastTextToShow );
                        this.SubTexts[0].Text.FinishWritingToBuffer();
                        this.NeedsToResize = true;
                        this.DoResizeIfNeeded();
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "Exception in UpdateContentFromVolatile for the single text element:" + e.ToString(), Verbosity.ShowAsError );
                }
            }

            #region GetTextToRender
            public void GetTextToRender( ArcenDoubleCharacterBuffer buffer )
            {
		GameEntity_Squad squadCurrentlyHoveredOver = GameEntity_Base.CurrentlyHoveredOver as GameEntity_Squad;
                if ( squadCurrentlyHoveredOver == null )
                    return;
                if ( InputCaching.CalculateShouldTooltipsBeSuppressed() )
                    return;
                bool isFromSidebar = false;
                switch ( GameEntity_Base.CurrentlyHoveredOverIsFromSidebarType )
                {
                    case FromSidebarType.Sidebar_MultipleUnits:
                    case FromSidebarType.Sidebar_SingleUnit:
                        isFromSidebar = true;
                        break;
                }

                if ( !isFromSidebar )
                {
                    EntityText.GetTooltip( buffer, squadCurrentlyHoveredOver, GameEntityTypeData.CurrentlyHoveredOver,
                        null, -1, null, 0, GameEntity_Base.CurrentlyHoveredOverIsFromSidebarType, ShipExtraDetailFlags.None, 1f, false );
                    EntityText.Write_Tooltip_Hotkeys_Footer( buffer, true, true, EntityText.GetHasContentsToView( squadCurrentlyHoveredOver ) );
                }
                switch ( GameEntity_Base.CurrentlyHoveredOverIsFromSidebarType )
                {
                    case FromSidebarType.SelectionWindow_MultipleUnits:
                        buffer.Add( FontSizes.MUCH_SMALLER_SIZE_PLUS_A_TAD_STRING ).Add( "\n<color=#996f4c>点击仅选择此类型单位。按住 <color=#d18444>" )
                            .Add( InputCaching.inputRemoveFromSelection.GetHumanReadableKeyCombo() ).Add( "</color> 并点击以从此选择中移除此类型单位。</color></size>" );
                        break;
                    case FromSidebarType.SelectionWindow_SingleUnit:
                        buffer.Add( FontSizes.MUCH_SMALLER_SIZE_PLUS_A_TAD_STRING ).Add( "\n<color=#996f4c>点击仅选择此单位。按住 <color=#d18444>" )
                            .Add( InputCaching.inputRemoveFromSelection.GetHumanReadableKeyCombo() ).Add( "</color> 并点击以从此选择中移除此单位。</color></size>" );
                        break;
                    case FromSidebarType.SelectionWindow_FromFleet:
                        buffer.Add( FontSizes.MUCH_SMALLER_SIZE_PLUS_A_TAD_STRING ).Add( "\n<color=#996f4c>此单位作为舰队的一部分被选中。" );
                        break;
                }
            }
            #endregion

            //private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer();
            //public void HandleMouseoverOfSidebarEntity( FromSidebarType Type )
            //{
            //    if ( InputCaching.CalculateShouldTooltipsBeSuppressed() )
            //        return;
            //    EntityText.GetTooltip( tooltipBuffer, GameEntity_Base.CurrentlyHoveredOver, null, Type, false );
            //    Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
            //}

            public override void OnMainThreadUpdate()
            {
                this.UpdatePositionAndSize();
            }

            public void UpdatePositionAndSize()
            {
                //if ( this.GetShouldBeHidden() )
                //    return;
                if ( this.SubTexts == null )
                    return;
                if ( !this.hasSetCanvasOffset )
                {
                    if ( this.MyElement != null && this.MyElement.Window != null )
                    {
                        this.hasSetCanvasOffset = true;
                        this.MyElement.Window.SetOverridingCanvasSortingOrder( 32767 ); //as high as it will go, so this is always on top!
                    }
                }

                try
                {
                    float screenXPixel = 230 * Window_InGameHoverEntityInfo.Instance.myXPositionScale;
                    float screenYPixel = 0;

                    this.MyElement.Window.IsAutomaticPositioningDisabled = true;

                    Vector3 worldSpacePoint = ArcenUI.Instance.guiCamera.ScreenToWorldPoint( new Vector3( screenXPixel, screenYPixel, ArcenUI.POSITION_Z ) );
                    worldSpacePoint.x = Window_InGameSidebarShips.Instance.GetWorldSpaceMaxX( 5f * Window_InGameHoverEntityInfo.Instance.myXPositionScale, false );

                    SubText groupZero = this.SubTexts[0];
                    if ( groupZero == null || groupZero.Obj == null || groupZero.Obj.transform == null || groupZero.Obj.transform.parent == null )
                        return;

                    Vector2 sizeDelta = ((RectTransform)groupZero.Obj.transform.parent).GetWorldSpaceSize();
                    float width = sizeDelta.x;
                    float height = sizeDelta.y;

                    float maxXPixel = ArcenUI.Instance.world_BottomRight.x - width;
                    float maxYPixel = ArcenUI.Instance.world_BottomRight.y + height;

                    worldSpacePoint.x = Mathf.Min( worldSpacePoint.x, maxXPixel );
                    worldSpacePoint.y = Mathf.Max( worldSpacePoint.y, maxYPixel );

                    this.MyElement.Window.SetPositionIfNeeded( worldSpacePoint );
                }
                catch { } //silence this
            }

            public override void SetElement( ArcenUI_Element Element )
            {
                this.MyElement = (ArcenUI_ImageButton)Element;
                this.MyElement.Window.MaxDeltaTimeBeforeUpdates = 0;
            }

            public override void OnUpdate()
            {
                this.DoResizeIfNeeded();
                base.OnUpdate();
            }

            private float _lastScale = -1f;
            private const int BASE_TOOLTIP_WIDTH = 660;

            public void DoResizeIfNeeded()
            {
                float scale = ArcenUI.Instance.Tooltip_Scale("ShipTooltipScale");
                bool scale_changed = scale != _lastScale;
                if ( scale_changed )
                    this.NeedsToResize = true;

                if ( this.MyElement != null && 
                     this.NeedsToResize && 
                     this.SubTexts != null && 
                     this.SubTexts[1].Obj.activeInHierarchy && 
                     this.NextTextToShow != null && 
                     this.NextTextToShow.Length > 0 )
                {
                    this.NeedsToResize = false;
                    this._lastScale = scale;
                    string text = this.NextTextToShow;

                    int min_width, max_width;
                    ArcenUI.Instance.Tooltip_Width(BASE_TOOLTIP_WIDTH, out min_width, out max_width);
                    
                    float min = min_width;
                    float max = max_width;
                        
                    if ( scale_changed )
                    {
                        this.SubTexts[1].ReferenceText.rectTransform.UI_SetWidth( max_width );
                        //this.SubTexts[0].Obj.transform.parent.localScale = new Vector3( newGeneralTooltipScale, newGeneralTooltipScale, newGeneralTooltipScale );
                    }
                    
                    this.MyElement.gameObject.transform.localScale = new Vector3( scale, scale, scale );

                    var messageSize = ArcenUI.Instance.CalculatePreferredTextObjectDimensions( this.SubTexts[1].ReferenceText, text, min, max );
                    this.WrappedNextTextToShow = this.NextTextToShow;
                    this.LastRequestedWidth = messageSize.x;
                    this.LastRequestedHeight = messageSize.y;

                    float text_margin_h, text_margin_v;
                    ArcenUI.Instance.Tooltip_Margin(out text_margin_h, out text_margin_v);

                    var bg_width = text_margin_h*2 + this.LastRequestedWidth;
                    var bg_height = text_margin_v*2 + this.LastRequestedHeight;
                    
                    var ui_width = text_margin_h*1 + this.LastRequestedWidth;
                    var ui_height = text_margin_v*1 + this.LastRequestedHeight;

                    ArcenUI_Image.SubImage subImage = this.MyElement.SubImages[0];
                    subImage.Img.rectTransform.UI_SetWidth( bg_width );
                    subImage.Img.rectTransform.UI_SetHeight( bg_height );
                    
                    var ui_text = this.SubTexts[0].ReferenceText;
                    ui_text.rectTransform.UI_SetWidth(ui_width);
                    ui_text.rectTransform.UI_SetHeight(ui_height);
                    
                    this.UpdateTextIfNeeded();
                    this.UpdatePositionAndSize();
                }
            }
        }

        // todo: remove
        public static TooltipDetail _CalculateTooltipDetailLevel()
        {
            return EntityText.Setup.Detail;
        }
        
        public static bool _CalculateIsShowingStrengthsAndWeaknesses()
        {
            return EntityText.Setup.ShowingCounters;
        }

        public static void _WriteTooltipDetailHotkeysFooter( ArcenCharacterBufferBase buffer, bool ShowSuppressionText, bool ShowDetailLevelText, string ShowClickForMoreInfoText )
        {
            TooltipDetail detailLevel = EntityText.Detail;

            if (ShowSuppressionText)
            {
                if (!InputActionTypeDataTable.Instance.IsActionBound("SuppressTooltips"))
                    ShowSuppressionText = false;
            }

            buffer.Add( "\n" ).Add( FontSizes.MUCH_SMALLER_SIZE_PLUS_A_TAD_STRING );

            if ( ShowDetailLevelText )
            {
                switch ( detailLevel )
                {
                    case TooltipDetail.SuperShortBecauseShowingOtherStuff:
                        //don't show those particular hotkeys right now, I guess
                        break;
                    case TooltipDetail.SuperShort:
                        //dim blue
                        buffer.StartColor( "7486d1" ).Add( "简要 " ).Add( "</color> <color=#996f4c>工具提示" );

                        buffer.Add( ": 按住 </color><color=#d18444>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key1" ) );
                        buffer.Add( "</color> <color=#996f4c>或 </color><color=#d18444>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key2" ) );
                        buffer.Add( "</color> <color=#996f4c>以查看中等详情，或同时按住以查看完整详情。" );

                        break;
                    case TooltipDetail.Medium:
                        //dim orange
                        buffer.StartColor( "dd9f45" ).Add( "中等 " ).Add( "</color> <color=#996f4c>工具提示" );

                        if ( GameSettings.Current.GetBoolBySetting( "ShowShipAndPlanetMediumModeByDefault" ) )
                        {
                            buffer.Add( ": 按住 </color><color=#d18444>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key1" ) );
                            buffer.Add( "</color> <color=#996f4c>或 </color><color=#d18444>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key2" ) );
                            buffer.Add( "</color> <color=#996f4c>以查看完整详情。" );
                        }
                        else
                        {
                            buffer.Add( ": 按住 </color><color=#d18444>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key1" ) );
                            buffer.Add( "</color> <color=#996f4c>并且 </color><color=#d18444>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key2" ) );
                            buffer.Add( "</color> <color=#996f4c>以查看完整详情。" );
                        }
                        break;
                    case TooltipDetail.Full:
                        //pretty bright purple
                        buffer.StartColor( "dd44eb" ).Add( "详细 " ).Add( "</color> <color=#996f4c>工具提示" );

                        //nothing to show about those hotkeys, since they don't show less info
                        break;
                    default:
                        buffer.Add( "??? 模式" );
                        break;
                }
                //if we're showing both, put a space between them
                if ( ShowSuppressionText )
                    buffer.Add( "  " );
            }
            if ( ShowSuppressionText )
                buffer.Add( "按住" ).Add( " </color><color=#d18444>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "SuppressTooltips" ) ).Add( "</color> <color=#996f4c>以隐藏所有工具提示。  " );

            if ( ShowClickForMoreInfoText != null && ShowClickForMoreInfoText.Length > 0 )
            {
                //if we're showing either of the things above, put a space between them and this
                if ( ShowSuppressionText || ShowDetailLevelText )
                    buffer.Add( "  " );

                buffer.Add( "<color=#3f6c9e>按住 </color><color=#4486d1>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "HoldAndClickToViewDetailsOfContents" ) )
                    .Add( "</color> <color=#3f6c9e>并点击此处以查看 " ).Add( ShowClickForMoreInfoText ).Add( "。</color>  " );
            }

            buffer.Add( "</color></size>" );
        }

        [ThreadStatic] //this one use of ThreadStatic is ok - it's highly unlikely to be on multiple threads, but if it is, then this is important
        private static GameEntity_Squad fakeSquadForBuildMode;

        public static bool _GetTextForEntity( ArcenCharacterBufferBase buffer, GameEntity_Squad relatedSquadOrNull, FleetMembership MembershipBase,
            GameEntityTypeData TypeDataOrNull, int OptionalCountToShow, Faction ForFactionOrNull, byte OptionalForMarkLevel, FromSidebarType IsFromSidebarType,
            ShipExtraDetailFlags DetailFlags, float PositionScaleMultiplier, bool IsBeingDrawnInPopupWindowRatherThanTooltip )
        {
            return _GetTextForEntity( buffer, relatedSquadOrNull, MembershipBase,
                TypeDataOrNull, null, "ffffff", string.Empty, OptionalCountToShow, ForFactionOrNull, OptionalForMarkLevel, IsFromSidebarType,
                DetailFlags, PositionScaleMultiplier, IsBeingDrawnInPopupWindowRatherThanTooltip );
        }

        public static bool _GetTextForEntity( ArcenCharacterBufferBase buffer, GameEntity_Squad relatedSquadOrNull, FleetMembership MembershipBase,
            GameEntityTypeData TypeDataOrNull, Fleet FleetToUseOrNull, string AltTextColorIfUsed, string AltTextInPlaceOfFleetAndOwnerOrBlank, int OptionalCountToShow, 
            Faction ForFactionOrNull, byte OptionalForMarkLevel, FromSidebarType IsFromSidebarType, 
            ShipExtraDetailFlags DetailFlags, float PositionScaleMultiplier, bool IsBeingDrawnInPopupWindowRatherThanTooltip )
        {
            int debugStage = 0;
            try
            {
                IsDrawing = false;
                debugStage = 1;

                if ( PositionScaleMultiplier != 1f )
                    PositionScaleMultiplier = 1f; //from Chris: now that we have ultrawide displays, this does not seem to be needed.

                TooltipDetail detailLevel = EntityText.Detail;
                bool isShowingStrengthsAndWeaknesses = _CalculateIsShowingStrengthsAndWeaknesses();

                FleetMembership relatedMembershipOrNull = relatedSquadOrNull == null ? MembershipBase : relatedSquadOrNull.FleetMembership;
                GameEntityTypeData relatedEntityTypeData = relatedSquadOrNull == null ? (MembershipBase == null ? TypeDataOrNull : MembershipBase.TypeData) : relatedSquadOrNull.TypeData;
                if ( relatedEntityTypeData == null )
                {
                    buffer.Add( "null relatedEntityData" );
                    return false;
                }
                if ( relatedEntityTypeData.Category != GameEntityCategory.Ship )
                {
                    buffer.Add( "relatedEntityData not a ship" );
                    return false;
                }

                bool showDebugInfoInTooltip = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" );

                if ( relatedSquadOrNull != null && ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                {
                    relatedSquadOrNull.FlagForRequestedForcedFullSyncToAllClients_FromAnyClient();
                }

                debugStage = 2;

                Mode panelMode;
                
                switch ( IsFromSidebarType )
                {
                    default:
                    {
                        panelMode = Mode.SingleUnit;
                        break;
                    }
                    case FromSidebarType.Sidebar_MultipleUnits:
                    case FromSidebarType.NonSidebar_MultipleUnits:
                    case FromSidebarType.SelectionWindow_MultipleUnits:
                    {
                        panelMode = Mode.UnitGroupOnSidebar;
                        break;
                    }
                }

                if (DetailFlags.HasFlag(ShipExtraDetailFlags.BuildInfo))
                    panelMode = Mode.Build;
                
                bool modeHasSpecificUnitOrFaction = relatedSquadOrNull != null;
                bool isForMultipleUnits = panelMode == Mode.UnitGroupOnSidebar;
                    
                bool isFromHackSidebarPopoutWindow = DetailFlags.HasFlag( ShipExtraDetailFlags.WindowHackChoicesSidebarPopoutForData );
                
                HackingType hackBeingDoneAgainstUs = null;
                GameEntity_Squad hackerBeingUsedAgainstUs = null;
                if (isFromHackSidebarPopoutWindow)
                {
                    hackBeingDoneAgainstUs = Window_HackChoicesSidebarPopout.Instance.HackTypeToChooseFor;
                    hackerBeingUsedAgainstUs = Window_HackChoicesSidebarPopout.Instance.HackerToUseOrNullIfNoneHere;
                }
                
                if ( hackBeingDoneAgainstUs != null && hackBeingDoneAgainstUs.IsAGrantShipStyleHack )
                {
                    if ( relatedEntityTypeData.IsBlockedFromDSSGrantToCommandStations )
                        buffer.Add( "不会授予指挥站；仅限战斗站和堡垒。\n" );
                    if ( relatedEntityTypeData.IsBlockedFromDSSGrantToBattlestationsAndCitadels )
                        buffer.Add( "不会授予战斗站或堡垒；仅限指挥站。\n" );
                }

                debugStage = 3;

                debugStage = 4;
                Faction localPlayerFactionOrNull = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                debugStage = 5;
                Faction owningFactionOrNull = (ForFactionOrNull != null ? ForFactionOrNull : (modeHasSpecificUnitOrFaction ? relatedSquadOrNull?.PlanetFaction?.Faction : localPlayerFactionOrNull));
                debugStage = 6;
                Planet thisPlanetOrNull = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                debugStage = 7;
                PlanetFaction localPlayerPlanetFactionOrNull = (thisPlanetOrNull == null || localPlayerFactionOrNull == null ? null : thisPlanetOrNull.GetPlanetFactionForFaction( localPlayerFactionOrNull ));
                debugStage = 8;

                //ArcenDebugging.ArcenDebugLogNoDateOrAnything(string.Format("TypeData={0} ForFactionOrNull={1} owningFactionOrNull={2}", TypeDataOrNull?.InternalName??"null", ForFactionOrNull?.GetDisplayName()??"null", owningFactionOrNull?.GetDisplayName()??"null"), DebugLogDestination.ArcenDebugLog, Verbosity.DoNotShow);
                PlayerTypeData localPlayerTypeOrNull = localPlayerFactionOrNull == null ? null : localPlayerFactionOrNull.PlayerTypeDataOrNull_ModeratelyExpensive;
                PlayerTypeData owningPlayerTypeOrNull = owningFactionOrNull == null ? null : owningFactionOrNull.PlayerTypeDataOrNull_ModeratelyExpensive;

                bool showMetalCost = true;
                bool showMetalOther = true;
                bool showAnyFuel = true;
                bool showEnergyProduction = true;
                bool showEnergyConsumption = true;

                if ( owningPlayerTypeOrNull != null )
                {
                    if ( !owningPlayerTypeOrNull.UsesMetal )
                    {
                        showMetalOther = false;
                    }

                    if ( !owningPlayerTypeOrNull.UsesEnergyAndFuel )
                    {
                        showEnergyProduction = false;
                    }
                }
                if ( localPlayerFactionOrNull == owningFactionOrNull )
                {
                    if ( localPlayerTypeOrNull != null && !localPlayerTypeOrNull.UsesMetal )
                    {
                        showMetalCost = false;
                    }
                }

                if ( localPlayerTypeOrNull != null && !localPlayerTypeOrNull.UsesEnergyAndFuel )
                {
                    showAnyFuel = false;
                }

                Fleet relatedMemFleetOrNull = null;
                if ( relatedMembershipOrNull != null )
                    relatedMemFleetOrNull = relatedMembershipOrNull.Fleet;
                if ( FleetToUseOrNull != null )
                    relatedMemFleetOrNull = FleetToUseOrNull;

                Fleet fedFromCityFleetOrNull = relatedMembershipOrNull?.GetFedHereFromCityFleetOrNull();

                bool isToBeClaimed = false;
                if ( relatedSquadOrNull != null && 
                     relatedSquadOrNull.HasNotYetBeenFullyClaimed &&
                     (relatedSquadOrNull.GetFactionTypeSafe() == FactionType.NaturalObject || relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player ) )
                {
                    isToBeClaimed = true;
                }

                //buffer.Add( "\nowningFaction: " ).Add( owningFaction.GetDisplayName() )
                //    .Add( " ForFactionOrNull: " ).Add( ForFactionOrNull == null ? "[null]" : ForFactionOrNull.GetDisplayName() )
                //    .Add( " Fleet: " ).Add( relatedMemFleetOrNull == null ? "[null]" : relatedMemFleetOrNull.GetName() )
                //    .Add( "\n" );

                debugStage = 9;
                //if ( relatedMembership == null )
                //{
                //    buffer.Add( "NULL relatedMembership" ).Add( "\n" );
                //    buffer.Add( "relatedEntity: " ).Add( relatedEntity == null ? "null" : relatedEntity.TypeData.InternalName ).Add( "\n" );
                //    buffer.Add( "localPlayerFaction: " ).Add( localPlayerFaction == null ? "null" : localPlayerFaction.GetDisplayName() ).Add( "\n" );
                //    buffer.Add( "owningFaction: " ).Add( owningFaction == null ? "null" : owningFaction.GetDisplayName() ).Add( "\n" );
                //    buffer.Add( "thisPlanet: " ).Add( thisPlanet == null ? "null" : thisPlanet.Name ).Add( "\n" );
                //    buffer.Add( "localPlayerPlanetFaction: " ).Add( localPlayerPlanetFaction == null ? "null" : localPlayerGetFactionDisplayNameSafe() ).Add( "\n" );
                //    return false;
                //}

                PlanetFaction relatedSquadPlanetFactionOrNull = null;
                Faction relatedSquadFactionOrNull = null;

                debugStage = 10;
                byte effectiveMarkLevel = OptionalForMarkLevel;
                if ( effectiveMarkLevel <= 0 )
                {
                    debugStage = 11;
                    if ( relatedMembershipOrNull != null )
                    {
                        debugStage = 12;
                        effectiveMarkLevel = relatedMembershipOrNull.EffectiveMark;
                    }
                    else
                    {
                        debugStage = 13;
                        effectiveMarkLevel = relatedEntityTypeData.StartingMarkLevel.Ordinal;
                    }
                    
                    debugStage = 14;
                }
                
                debugStage = 15;
                if ( effectiveMarkLevel > relatedEntityTypeData.MaxMarkLevel )
                {
                    debugStage = 16;
                    effectiveMarkLevel = relatedEntityTypeData.MaxMarkLevel;
                }
                
                debugStage = 17;
                
                GameEntityTypeData.MarkLevelStats relatedMarkLevelData = relatedEntityTypeData.MarkStatsFor( effectiveMarkLevel );
                
                debugStage = 18;
                
                //This little section is required for AI ships to show their correct values.  Same with other non-player ships.
                if ( relatedSquadOrNull != null )
                {
                    debugStage = 19;
                    relatedMarkLevelData = relatedSquadOrNull.DataForMark;
                    debugStage = 20;
                    effectiveMarkLevel = relatedMarkLevelData.MarkLevel.Ordinal;
                    debugStage = 21;
                    relatedSquadPlanetFactionOrNull = relatedSquadOrNull.PlanetFaction;
                    debugStage = 22;
                    relatedSquadFactionOrNull = relatedSquadPlanetFactionOrNull?.Faction ?? null;
                }

                debugStage = 23;
                
                IsDrawing = true;

                float baseOffsetBeforeSteps = 0;
                if ( relatedEntityTypeData.TexEmbedSprite_Icon != null )
                {
                    debugStage = 24;
                    
                    var default_faction = SpecialFactionDataTable.Instance.DefaultRow;
                    var centerColor = default_faction.DefaultFactionCenterColor;
                    var trimColor = default_faction.DefaultFactionTrimColor;
                    
                    debugStage = 25;
                    if (PlayerProfile_AIW2.Local != null)
                    {
                        centerColor = PlayerProfile_AIW2.Local.DefaultFactionCenterColor;
                        trimColor = PlayerProfile_AIW2.Local.DefaultFactionTrimColor;
                    }
                    
                    debugStage = 26;
                    
                    if (owningFactionOrNull != null)
                    {
                        centerColor = owningFactionOrNull.FactionCenterColor;
                        trimColor = owningFactionOrNull.FactionTrimColor;
                    }
                    
                    if (relatedEntityTypeData.OverrideFactionColor_Center != null)
                        centerColor = relatedEntityTypeData.OverrideFactionColor_Center;
                    
                    if (relatedEntityTypeData.OverrideFactionColor_Trim != null)
                        trimColor = relatedEntityTypeData.OverrideFactionColor_Trim;

                    debugStage = 27;
                    
                    //note: mspace has been recoded by Chris to be a "do not advance the x offset" function.  It is not monospace like it otherwise would be.
                    //      this allows for drawing sprites on top of one another in the stacked fashion, and then having other stuff come after it
                    buffer.Add( "<mspace><sprite=\"" ).Add( relatedEntityTypeData.TexEmbedSprite_Icon.InternalName )
                        .Add( "\" color=\"#" ).Add( centerColor.ColorHex ).Add( "\">" );
                    
                    debugStage = 28;
                    
                    if ( relatedEntityTypeData.TexEmbedSprite_IconBorder != null )
                    {
                        buffer.Add( "<sprite=\"" ).Add( relatedEntityTypeData.TexEmbedSprite_IconBorder.InternalName )
                            .Add( "\" color=\"#" ).Add( trimColor.ColorHex ).Add( "\">" );
                    }
                    
                    debugStage = 29;
                    
                    if ( relatedEntityTypeData.TexEmbedSprite_IconOverlay != null )
                    {
                        buffer.Add( "<sprite=\"" ).Add( relatedEntityTypeData.TexEmbedSprite_IconOverlay.InternalName ).Add( "\">" );
                    }
                    
                    debugStage = 30;
                    
                    buffer.Add( "</mspace><pos=50>" );
                    baseOffsetBeforeSteps = 50 * PositionScaleMultiplier;
                    buffer.SetPositionOffset( baseOffsetBeforeSteps );
                }
                
                debugStage = 50;

                //========Start Chris adapter for SirLimbo
                GameEntityTypeData entityType = relatedEntityTypeData;
                Balance_MarkLevel markLevel = Balance_MarkLevelTable.Instance.RowsByOrdinal[effectiveMarkLevel];
                FleetMembership fleetMembershipOrNull = relatedMembershipOrNull;
                debugStage = 101;
                bool isUsingFakeEntityAsStandin = false;

                debugStage = 102;
                //=========fakeSquadForBuildMode is set up as an adapter from Chris to make life easier for all of us, but specifically for SirLimbo's stuff.
                if ( fakeSquadForBuildMode == null )
                    fakeSquadForBuildMode = GameEntity_Squad.CreateNew_ForFakeInUITooltipsOnlyDoNotUseThisAnywhereElse();

                debugStage = 103;
                if ( relatedSquadOrNull == null )
                {
                    debugStage = 104;
                    isUsingFakeEntityAsStandin = true; //this keeps entity from being null and lets it work properly, but is probably just for build menus and wave contents tooltips, etc
                    relatedSquadOrNull = fakeSquadForBuildMode;
                    
                    debugStage = 105;
                    fakeSquadForBuildMode.SetInfoForFakeInUITooltipsOnlyDoNotUseThisAnywhereElse( 
                        entityType, effectiveMarkLevel, thisPlanetOrNull, owningFactionOrNull );
                }

                debugStage = 106;
                //these are conveniences
                PlanetFaction entityPFaction = relatedSquadOrNull == null ? null : relatedSquadOrNull.PlanetFaction;
                Faction entityFaction = entityPFaction == null ? null : entityPFaction.Faction;

                debugStage = 107;
                //========Start SirLimbo Bits
                float posSteps;
                if ( detailLevel == TooltipDetail.Full )
                    posSteps = 200;
                else
                    posSteps = 100;
                posSteps = (float) Math.Round( posSteps * PositionScaleMultiplier );
                CharacterPosInfo cpi = new CharacterPosInfo( posSteps );

                GameEntityTypeData.MarkLevelStats markStats = relatedMarkLevelData;

                #region FirstRow: Entity Name, Faction, etc
                debugStage = 13;
                buffer.Add( "<b>" );

                if ( DetailFlags.HasFlag( ShipExtraDetailFlags.InGuardPost ) )
                    buffer.Add( "内部守卫哨所: ", "ff622b" );
                else if ( DetailFlags.HasFlag( ShipExtraDetailFlags.BeingTransported ) )
                    buffer.Add( "内部运输: ", "59d2ff" );
                else if ( DetailFlags.HasFlag( ShipExtraDetailFlags.LoadedDrone ) )
                    buffer.Add( "已装载无人机: ", "59d2ff" );

                if ( OptionalCountToShow > 1 )
                {
                    buffer.Add( OptionalCountToShow, "dbef21" ).Add( "x " );
                } else if ( relatedSquadOrNull.ExtraStackedSquadsInThis > 0 )
                {
                    if ( detailLevel == TooltipDetail.Full )
                    {
                        buffer.Add( "堆叠 " ).Add( relatedSquadOrNull.ExtraStackedSquadsInThis + 1, "dbef21" ).Add( " " );
                    } else
                    {
                        buffer.Add( relatedSquadOrNull.ExtraStackedSquadsInThis + 1, "dbef21" ).Add( "x " );
                    }
                }
                debugStage = 14;

                buffer.AddSize_Large();

                if ( relatedSquadOrNull.SecondsSpentAsRemains >= 0 )
                {
                    buffer.Add( "残骸 ", "ff4b21" );
                }
                if ( entityType.IsModular && relatedMembershipOrNull != null )
                {
                    foreach ( EntitySystemTypeData systemData in entityType.SystemTypes )
                    {
                        if ( !systemData.IsModule || systemData.ModularFullNamePrefix == null || systemData.ModularFullNamePrefix.Length <= 0 )
                            continue; //skip any non-modules, or things that don't add to the prefix
                        if ( !systemData.IsModuleOn( relatedMembershipOrNull ) )
                            continue; //skip any that are not enabled
                        if ( !systemData.ForMark[effectiveMarkLevel].IsFunctionalAtThisMarkLevel )
                            continue;

                        buffer.Add( systemData.ModularFullNamePrefix );
                    }
                }
                bool shrinkShipName = false;
                if ( fedFromCityFleetOrNull != null && owningFactionOrNull != null &&
                     detailLevel > TooltipDetail.Medium)
                    shrinkShipName = true;
                if ( shrinkShipName )
                    buffer.Add("<size=80%>");
                buffer.Add( entityType.DisplayName );
                if ( markLevel.Ordinal != 0 )
                {
                    buffer.Add( " " ).AddMarkLevelFormated( markLevel );
                }
                debugStage = 15;
                if ( AltTextInPlaceOfFleetAndOwnerOrBlank != null && AltTextInPlaceOfFleetAndOwnerOrBlank.Length > 0 )
                {
                    buffer.StartColor( AltTextColorIfUsed ).Add( AltTextInPlaceOfFleetAndOwnerOrBlank ).EndColor();
                }
                else if ( owningFactionOrNull != null )
                {
                    {
                        buffer.Add( " of " ).StartColor( owningFactionOrNull.FactionCenterColor.ColorHexBrighter );
                        if ( owningFactionOrNull.Type == FactionType.AI || FactionUtilityMethods.Instance.IsACoreAISubFaction( owningFactionOrNull ) )
                        {
                            AISentinelsCoreData sentinelData = owningFactionOrNull.TryGetAISentinelsCoreData()?.SentinelInfo;
                            if ( sentinelData != null )
                            {
                                bool secretFactionDetails = AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "FactionDetailsAreSecret" ) && World.Instance.ConclusionType == CampaignConclusionType.NotConcluded;
                                var showRandomAiType = GameSettings.Current.GetBoolBySetting( "ShowRandomAIType" );

                                if (!secretFactionDetails)
                                {
                                    if ( sentinelData.WasRandomAIType )
                                    {
                                        if ( showRandomAiType )
                                        {
                                            buffer.Add( sentinelData.AIType.DisplayName ).Add( " " );
                                        } 
                                        else
                                        {
                                            buffer.Add( "随机 " );
                                        }
                                    } 
                                    else if ( sentinelData.AdaptiveAIDifficulty != TypeDifficulty.Unset )
                                    {
                                        if ( showRandomAiType )
                                        {
                                            buffer.Add( sentinelData.AIType.DisplayName ).Add( " " );
                                        } 
                                        else
                                        {
                                            buffer.Add( "自适应 " );
                                        }
                                    } 
                                    else
                                    {
                                        buffer.Add( sentinelData.AIType.DisplayName ).Add( " " );
                                    }
                                }
                            }
                        }
                        debugStage = 16;
                        if ( entityType.HideFactionName )
                        { }
                        else if ( entityType.OverrideFactionName.Length > 0 )
                        {
                            buffer.Add( entityType.OverrideFactionName );
                        }
                        else
                        {
                            buffer.Add( owningFactionOrNull.GetDisplayName() );
                        }
                        if ( owningFactionOrNull.Type == FactionType.Player )
                        {
                            if ( relatedMemFleetOrNull != null )
                                buffer.EndColor().Add( " 舰队 " ).AddFactionColoredString( relatedMemFleetOrNull.GetName(), owningFactionOrNull );
                            if ( fedFromCityFleetOrNull != null && detailLevel > TooltipDetail.Medium) {
                                buffer.EndColor().Add( " 来自 " ).AddFactionColoredString( fedFromCityFleetOrNull.GetName(), owningFactionOrNull );
                            }
                        }
                        if ( relatedSquadOrNull.ExoGalacticAttackPlanetIdx != -1 )
                            buffer.Add( " (星系外打击部队)" );
                        buffer.EndColor();
                    }
                }
                if ( shrinkShipName )
                    buffer.Add("</size>");
                debugStage = 17;
                if ( GameSettings.Current.GetBoolBySetting( "ShowEntityIDInHovertext" ) )
                {
                    buffer.Add( FontSizes.BASE_SIZE_STRING ).StartColor( "33dd33" ).Add( " ID-" ).Add( relatedSquadOrNull.PrimaryKeyID ).EndColor().EndSize();
                }
                if ( GameSettings.Current.GetBoolBySetting( "Debug_WriteFleetIDInTooltips" ) )
                {
                    buffer.Add( FontSizes.BASE_SIZE_STRING ).StartColor( "3333dd" ).Add( " FLEET-" ).Add( relatedSquadOrNull.GetFleetID_Safe() ).EndColor().EndSize();
                }

                if ( GameSettings.Current.GetBoolBySetting( "ShowEntityLocationInHovertext" ) )
                {
                    buffer.Add( FontSizes.SLIGHTLY_SMALLER_SIZE_STRING ).StartColor( "87dd33" ).Add( " Loc: " ).Add( relatedSquadOrNull.WorldLocation.X - 400000 )
                        .Add( "," ).Add( relatedSquadOrNull.WorldLocation.Y - 400000 ).EndColor().EndSize();
                }
                #endregion

                debugStage = 18;
                buffer.Add( "</b>" ).EndSize();
                
                // this is really problematic to actually fit in
                /*
                buffer.Add( "<line-height=0>" );
                buffer.NewLine();
                
                if ( detailLevel < TooltipDetail.Medium )
                {
                    buffer.Add( "<align=\"right\">" );
                    buffer.Add( "<size=80%><color=#525d66>Hold <color=#577287>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key1" ) );
                    buffer.Add( "</color> + <color=#577287>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key2" ) );
                    buffer.Add( "</color> to see field labels.  </color></size>" );
                    buffer.Add( "<line-height=0>");
                    buffer.NewLine();
                    buffer.Add( "</align>");
                    buffer.Add( "</line-height>");
                }
                
                buffer.Add( "</line-height>");
                */
                buffer.NewLine();

                if ( isShowingStrengthsAndWeaknesses && owningFactionOrNull != null && localPlayerFactionOrNull != null )
                {
                    buffer.Add( "\n" );

                    if ( owningFactionOrNull.GetIsHostileTowards( localPlayerFactionOrNull ) )
                    {
                        WriteWeakAgainst( buffer, relatedEntityTypeData, markStats );
                    } else
                    {
                        WriteStrongAgainst( buffer, relatedEntityTypeData, thisPlanetOrNull );
                    }
                    return true;
                }
                debugStage = 1801;

                cpi.ResetPos();
                buffer.ToPos( cpi.AddCustomFloat( baseOffsetBeforeSteps ) );

                #region SecondRow: Useful Stats
                int hullMax;
                int shieldMax;
                int hullCurr;
                int shieldCurr;
                FInt hullPercent;
                FInt shieldPercent;
                int metalCurr;
                int energyCurr;
                int storedMetal = int.MaxValue;
                int storedEnergy = int.MaxValue;
                int storedArgon = int.MaxValue;
                int storedRadon = int.MaxValue;
                int storedXenon = int.MaxValue;
                bool entityCanBeClaimed;
                bool isUnderConstruction;
                bool isCenterpiece;
                debugStage = 1802;
                entityCanBeClaimed = relatedSquadOrNull.HasNotYetBeenFullyClaimed && (owningFactionOrNull == null || owningFactionOrNull.Type == FactionType.NaturalObject || owningFactionOrNull.Type == FactionType.Player);
                debugStage = 1803;
                isUnderConstruction = relatedSquadOrNull.SelfBuildingMetalRemaining > FInt.Zero;
                debugStage = 1804;
                isCenterpiece = fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.Centerpiece.GetSquad() == relatedSquadOrNull;
                debugStage = 1805;
                hullCurr = relatedSquadOrNull.GetCurrentHullPoints();
                shieldCurr = relatedSquadOrNull.GetCurrentShieldPoints();
                
                if ( IsBeingDrawnInPopupWindowRatherThanTooltip )
                {
                    hullMax = markStats.BaseHullPoints;
                    shieldMax = markStats.BaseShieldPoints;
                } else
                {
                    hullMax = relatedSquadOrNull.GetMaxHullPoints();
                    shieldMax = relatedSquadOrNull.GetMaxShieldPoints();
                }
                hullPercent = FInt.Create( hullCurr, true ).ToPercent( hullMax );
                shieldPercent = FInt.Create( shieldCurr, true ).ToPercent( shieldMax );
                if ( entityCanBeClaimed )
                {
                    metalCurr = relatedSquadOrNull.DataForMark.MetalCostToClaim;
                } else
                {
                    metalCurr = relatedSquadOrNull.GetMetalCost();
                }

                energyCurr = relatedSquadOrNull.GetEnergyUsage();
                if ( localPlayerFactionOrNull != null && ( entityCanBeClaimed || localPlayerFactionOrNull.NetEnergy < 0 || panelMode == Mode.Build ) )
                {
                    storedMetal = localPlayerFactionOrNull.StoredMetal.IntValue;
                    storedEnergy = localPlayerFactionOrNull.NetEnergy;
                    storedArgon = localPlayerFactionOrNull.NetFuelArgon;
                    storedRadon = localPlayerFactionOrNull.NetFuelRadon;
                    storedXenon = localPlayerFactionOrNull.NetFuelXenon;
                }

                debugStage = 5000;

                #region Hull
                if ( detailLevel == TooltipDetail.Full )
                {
                    buffer.Add( "船体: " );
                }
                buffer.StartHull( detailLevel != TooltipDetail.Full ).AddNumberTruncated( hullCurr ).EndColor();
                if ( detailLevel == TooltipDetail.Full )
                {
                    buffer.Add( " / " ).StartHull( false ).AddNumberTruncated( hullMax ).EndColor();
                }
                if ( detailLevel >= TooltipDetail.Medium && !isUsingFakeEntityAsStandin && !ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() )
                {
                    buffer.Add( " " ).AddSize_Tiny().AddPercentageInColor( hullPercent, true, false ).EndSize();
                }
                #endregion

                debugStage = 6000;

                if(detailLevel == TooltipDetail.Full)
                    buffer.ToPos( cpi.AddStep_NinetyPercent() );
                else
                    buffer.ToPos( cpi.AddStep_HundredFiftyPercent() );

                #region Shield
                if ( detailLevel == TooltipDetail.Full )
                {
                    buffer.Add( "护盾: " );
                }
                buffer.StartShield( detailLevel != TooltipDetail.Full ).AddNumberTruncated( shieldCurr ).EndColor();
                if ( detailLevel == TooltipDetail.Full )
                {
                    buffer.Add( " / " ).StartShield( false ).AddNumberTruncated( shieldMax ).EndColor();
                }
                if ( detailLevel >= TooltipDetail.Medium && !isUsingFakeEntityAsStandin && !ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() )
                {
                    buffer.Add( " " ).AddSize_Tiny().AddPercentageInColor( shieldPercent, true, true ).EndSize();
                }
                #endregion

                debugStage = 7000;

                if ( detailLevel == TooltipDetail.Full )
                    buffer.ToPos( cpi.AddStep() );
                else
                    buffer.ToPos( cpi.AddStep_HundredFiftyPercent() );

                if ( IsBeingDrawnInPopupWindowRatherThanTooltip || entityCanBeClaimed || panelMode == Mode.Build )
                {
                    #region Popup Display Mass/Armor
                    buffer.AddSize_Small();

                    if ( detailLevel == TooltipDetail.Full )
                    {
                        buffer.Add( "质量: " );
                    }
                    buffer.StartMass( detailLevel != TooltipDetail.Full ).Add( entityType.Mass_tX ).Add( " tX" ).EndColor();

                    if ( detailLevel == TooltipDetail.Full )
                        buffer.ToPos( cpi.AddStep_Half() );
                    else if ( IsBeingDrawnInPopupWindowRatherThanTooltip )
                        buffer.ToPos( cpi.AddStep_SeventyPercent() );
                    else
                        buffer.ToPos( cpi.AddStep_NinetyPercent() );

                    if ( detailLevel == TooltipDetail.Full )
                    {
                        buffer.Add( "装甲: " );
                    }
                    buffer.StartArmor( detailLevel != TooltipDetail.Full ).Add( entityType.Armor_mm ).Add( " mm" ).EndColor();

                    #endregion

                    buffer.EndSize();
                } else
                {
                    #region Metal
                    if ( showMetalCost )
                    {
                        if ( detailLevel == TooltipDetail.Full )
                        {
                            buffer.Add( "金属：" );
                        }
                        buffer.StartMetal( detailLevel != TooltipDetail.Full );
                        if ( metalCurr == 0 )
                        {
                            buffer.Add( "-" );
                        }
                        else
                        {
                            buffer.AddNumberTruncated( metalCurr );
                        }
                        buffer.EndColor();
                    }
                    #endregion

                    debugStage = 8000;

                    if(detailLevel == TooltipDetail.Full )
                        buffer.ToPos( cpi.AddStep_SixtyPercent() );
                    else
                        buffer.ToPos( cpi.AddStep_EightyPercent() );

                    #region Energy
                    if ( showEnergyConsumption )
                    {
                        if ( detailLevel == TooltipDetail.Full )
                        {
                            buffer.Add( "能量: " );
                        }
                        buffer.StartEnergy( detailLevel != TooltipDetail.Full );
                        if ( energyCurr == 0 )
                        {
                            buffer.Add( "-" );
                        } else
                        {
                            buffer.AddNumberTruncated( energyCurr );
                        }
                        buffer.EndColor();
                    }
                    #endregion

                    debugStage = 8500;

                    if ( detailLevel == TooltipDetail.Full )
                        buffer.ToPos( cpi.AddStep_SixtyPercent() );
                    else
                        buffer.ToPos( cpi.AddStep_EightyPercent() );

                    #region Fuel
                    if ( World_AIW2.Instance.IsFuelEnabled && relatedEntityTypeData.FuelUse > 0 )
                    {
                        if ( detailLevel == TooltipDetail.Full )
                        {
                            if ( relatedEntityTypeData.FuelUseType == ResourceType.FuelArgon )
                            {
                                buffer.Add( "氩气: " );
                            } else if ( relatedEntityTypeData.FuelUseType == ResourceType.FuelRadon)
                            {
                                buffer.Add( "氡气: " );
                            } else if ( relatedEntityTypeData.FuelUseType == ResourceType.FuelXenon )
                            {
                                buffer.Add( "氙气: " );
                            }
                        }
                        if ( relatedEntityTypeData.FuelUseType == ResourceType.FuelArgon )
                        {
                            buffer.StartArgon( detailLevel != TooltipDetail.Full );
                        } else if ( relatedEntityTypeData.FuelUseType == ResourceType.FuelRadon )
                        {
                            buffer.StartRadon( detailLevel != TooltipDetail.Full );
                        } else if ( relatedEntityTypeData.FuelUseType == ResourceType.FuelXenon )
                        {
                            buffer.StartXenon( detailLevel != TooltipDetail.Full );
                        }
                        if ( energyCurr == 0 )
                        {
                            buffer.Add( "-" );
                        } else
                        {
                            buffer.AddNumberTruncated( relatedEntityTypeData.FuelUse );
                        }
                        buffer.EndColor();
                    }
                    #endregion
                }
                
                #endregion

                debugStage = 9000;

                if ( ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() && GameSettings.Current.GetBoolBySetting( "ShowTooltipProgressBars" ) && !isUsingFakeEntityAsStandin )
                {
                    buffer.Add( "\n" );
                    cpi.ResetPos( baseOffsetBeforeSteps );
                    buffer.ToPos( cpi );
                    ArcenExternalUIUtilities.AppendBar( buffer, hullPercent.IntValue, ColorMath.LightGreen, 35, 35, ColorMath.DarkRed, hullMax );
                    float shieldStart = baseOffsetBeforeSteps + ( detailLevel == TooltipDetail.Full
                        ? cpi.Config.PosPerStep_NinetyPercent
                        : cpi.Config.PosPerStep_HundredFiftyPercent );
                    cpi.ResetPos( shieldStart );
                    buffer.ToPos( cpi );
                    ArcenExternalUIUtilities.AppendBar( buffer, shieldPercent.IntValue, ColorMath.IceBlue, 35, 35, ColorMath.DarkRed, shieldMax );
                }

                buffer.NewLine();
                cpi.ResetPos();
                buffer.ToPos( cpi.AddCustomFloat( baseOffsetBeforeSteps ) );

                #region ThridRow: Strength, Speed but also the 4 Useless Stats
                int strengthCurr;
                int strengthTotal = 0;
                string strengthMode = null;
                string strengthModeColorHex = null;
                strengthCurr = relatedSquadOrNull.GetStrengthPerSquad( false );

                #region AIP/Strength
                if ( panelMode == Mode.Build || entityCanBeClaimed )
                {
                    #region AIP
                    if ( entityCanBeClaimed )
                    {
                        if ( entityType.AIPToClaim != FInt.Zero )
                        {
                            if ( detailLevel == TooltipDetail.Full )
                            {
                                buffer.Add( "占领AIP: " );
                            }
                            if ( entityType.AIPToClaim > FInt.Zero )
                            {
                                buffer.AddAIP_MoreReadable( entityType.AIPToClaim, detailLevel != TooltipDetail.Full );
                            } else
                            {
                                buffer.AddAIPReduction_MoreReadable( entityType.AIPToClaim, detailLevel != TooltipDetail.Full );
                            }
                        }
                    } else if ( entityType.AIPWhenGrantedByHack != FInt.Zero )
                    {
                        if ( detailLevel == TooltipDetail.Full )
                        {
                            buffer.Add( "黑客AIP: " );
                        }
                        if ( entityType.AIPWhenGrantedByHack > FInt.Zero )
                        {
                            buffer.AddAIP_MoreReadable( entityType.AIPWhenGrantedByHack, detailLevel != TooltipDetail.Full );
                        } else
                        {
                            buffer.AddAIPReduction_MoreReadable( entityType.AIPWhenGrantedByHack, detailLevel != TooltipDetail.Full );
                        }
                    }
                    #endregion
                } else
                {
                    debugStage = 11000;
                    #region Strength
                    if ( !isUsingFakeEntityAsStandin && !IsBeingDrawnInPopupWindowRatherThanTooltip )
                    {
                        if ( OptionalCountToShow > 1 ) {
                            strengthTotal = strengthCurr;
                            strengthCurr *= OptionalCountToShow;
                            strengthMode = "1x";
                            strengthModeColorHex = "ffffff";
                        } else if ( relatedSquadOrNull.ExtraStackedSquadsInThis > 0 )
                        {
                            strengthTotal = strengthCurr;
                            strengthCurr *= 1 + relatedSquadOrNull.ExtraStackedSquadsInThis;
                            strengthMode = "1x";
                            strengthModeColorHex = "ffffff";
                        } else
                        {
                            strengthTotal = relatedSquadOrNull.GetStrengthOfContentsIfAny();
                            if ( strengthTotal > 0 )
                            {
                                strengthMode = "+T";
                                strengthModeColorHex = ColorMath.IceBlue.GetHexCode();
                            }
                        }
                    }
                    if ( detailLevel == TooltipDetail.Full )
                    {
                            buffer.Add( "战斗力: " );
                    }
                    buffer.AddStrength_Truncated( strengthCurr, detailLevel != TooltipDetail.Full );
                    if ( strengthMode != null )
                    {
                        buffer.Add( " " ).AddSize_Tiny().Add( "(" ).StartStrength( detailLevel != TooltipDetail.Full ).AddNumberTruncated( FInt.Create( strengthTotal, false ) ).EndColor().Add( " " ).Add( strengthMode ).Add( ")" ).EndSize();
                    }
                    #endregion
                }
                #endregion

                if ( detailLevel == TooltipDetail.Full )
                    buffer.ToPos( cpi.AddStep_NinetyPercent() );
                else
                    buffer.ToPos( cpi.AddStep_HundredFiftyPercent() );

                debugStage = 12000;

                #region Speed
                int speedCurr;
                int speedBase;
                FInt speedPercent;
                if ( isUsingFakeEntityAsStandin )
                {
                    speedCurr = relatedSquadOrNull.CalculateSpeed( false );//since these ships don't calculate speed on their own
                } else
                {
                    speedCurr = relatedSquadOrNull.CalculatedSpeed;
                }
                speedBase = markStats.Speed;
                if ( speedBase > 0 )
                {
                    if ( detailLevel == TooltipDetail.Full )
                        buffer.Add( "速度: " );
                    buffer.StartSpeed( detailLevel != TooltipDetail.Full ).AddNumberMoreReadable( speedCurr );
                    if ( speedCurr != speedBase )
                    {
                        speedPercent = FInt.Create( speedCurr, true ).ToPercent( speedBase );
                        if ( detailLevel == TooltipDetail.Full )
                        {
                            buffer.EndColor().Add( " / " ).StartSpeed( false ).AddNumberMoreReadable( speedBase );
                        }
                        if ( detailLevel >= TooltipDetail.Medium )
                        {
                            buffer.Add( " " ).AddSize_Tiny().AddPercentageInColor( speedPercent, true, false ).EndSize();
                        }
                    }
                    buffer.EndColor();
                } else
                {
                    if ( relatedEntityTypeData.OrbitsGravityWellCenterAtItsCurrentRadius ||
                        relatedEntityTypeData.OrbitsParentAtRange > 0 || relatedEntityTypeData.OrbitsFlagshipAtRange > 0 )
                    {
                        if ( detailLevel == TooltipDetail.Full )
                            buffer.Add( "轨道: " );
                        buffer.AddSpeed( relatedEntityTypeData.DegreesToOrbitPerSecond.ToFloatNonSim().ToString(
                            detailLevel != TooltipDetail.Full ? "0.0' d'" : "0.0' deg'" ), detailLevel != TooltipDetail.Full ); ;
                    } else
                    {
                        if ( detailLevel == TooltipDetail.Full )
                            buffer.Add( "速度: " );
                        buffer.AddSpeed( "-", detailLevel != TooltipDetail.Full );
                    }
                }
                #endregion

                debugStage = 13000;

                if ( detailLevel == TooltipDetail.Full )
                    buffer.ToPos( cpi.AddStep() );
                else
                    buffer.ToPos( cpi.AddStep_HundredFiftyPercent() );

                #region Useless Stats
                if ( IsBeingDrawnInPopupWindowRatherThanTooltip || entityCanBeClaimed || panelMode == Mode.Build )
                {
                    buffer.AddSize_Small();
                    if ( detailLevel == TooltipDetail.Full )
                    {
                        buffer.Add( "引擎: " );
                    }
                    buffer.StartEngine( detailLevel != TooltipDetail.Full );
                    if ( entityType.Engine_gx > 0 )
                    {
                        buffer.Add( entityType.Engine_gx ).Add( " gX" );
                    } else
                    {
                        buffer.Add( " -" );
                    }
                    buffer.EndColor();

                    if ( detailLevel == TooltipDetail.Full )
                        buffer.ToPos( cpi.AddStep_Half() );
                    else if ( IsBeingDrawnInPopupWindowRatherThanTooltip )
                        buffer.ToPos( cpi.AddStep_SeventyPercent() );
                    else
                        buffer.ToPos( cpi.AddStep_NinetyPercent() );

                    if ( detailLevel == TooltipDetail.Full )
                    {
                        buffer.Add( "反照率: " );
                    }
                    buffer.StartAlbedo( detailLevel != TooltipDetail.Full ).Add( markStats.Albedo ).EndColor();
                } else
                {
                    buffer.AddSize_Small();
                    if ( detailLevel == TooltipDetail.Full )
                    {
                        buffer.Add( "引擎: " );
                    }
                    buffer.StartEngine( detailLevel != TooltipDetail.Full );
                    if ( entityType.Engine_gx > 0 )
                    {
                        buffer.Add( entityType.Engine_gx ).Add( " gX" );
                    } else
                    {
                        buffer.Add( " -" );
                    }
                    buffer.EndColor();

                    if(detailLevel == TooltipDetail.Full )
                        buffer.ToPos( cpi.AddStep_Half() );
                    else
                        buffer.ToPos( cpi.AddStep_EightyPercent() );

                    if ( detailLevel == TooltipDetail.Full )
                    {
                        buffer.Add( "质量: " );
                    }
                    buffer.StartMass( detailLevel != TooltipDetail.Full ).Add( entityType.Mass_tX ).Add( " tX" ).EndColor();

                    if ( detailLevel == TooltipDetail.Full )
                        buffer.ToPos( cpi.AddStep_FourtyPercent().AddStep_Twentieth() );
                    else
                        buffer.ToPos( cpi.AddStep_EightyPercent() );

                    if ( detailLevel == TooltipDetail.Full )
                    {
                        buffer.Add( "反照率: " );
                    }
                    buffer.StartAlbedo( detailLevel != TooltipDetail.Full ).Add( markStats.Albedo ).EndColor();

                    if ( detailLevel == TooltipDetail.Full )
                        buffer.ToPos( cpi.AddStep_Half() );
                    else
                        buffer.ToPos( cpi.AddStep_EightyPercent() );

                    if ( detailLevel == TooltipDetail.Full )
                    {
                        buffer.Add( "装甲: " );
                    }
                    buffer.StartArmor( detailLevel != TooltipDetail.Full ).Add( entityType.Armor_mm ).Add( " mm" ).EndColor();
                }
                #endregion

                buffer.EndSize();
                #endregion

                debugStage = 14001;

                #region FourthRow: Claim/Construction printouts, orders
                //if ( detailLevel >= TooltipDetail.Medium )
                {
                    #region Claiming
                    if ( !isUsingFakeEntityAsStandin ) //only do this for real entities.  The build menu stuff happens here for fake ones
                    {
                        if ( entityCanBeClaimed && owningFactionOrNull != null )
                        {
                            buffer.EndColor().NewLine();
                            cpi.ResetPos();
                            buffer.Add( "占领: " ).AddSize_Small();
                            if ( relatedSquadOrNull.IsInHoldFireMode )
                            {
                                buffer.Add( "已暂停", "cccccc" );
                            } else
                            {
                                Faction forFaction;
                                if ( owningFactionOrNull.Type != FactionType.Player )
                                {
                                    forFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                                } else
                                {
                                    forFaction = owningFactionOrNull;
                                }
                                if ( forFaction != null )
                                {
                                    FactionType planetController = relatedSquadOrNull.Planet == null ? FactionType.NaturalObject : relatedSquadOrNull.Planet.GetControllingFactionType();
                                    if ( planetController == FactionType.NaturalObject )
                                    {
                                        buffer.Add( "需要星球控制", "bbbbbb" );
                                    } else if ( planetController != FactionType.Player )
                                    {
                                        buffer.Add( "被敌人阻挡", "aa3333" );
                                    } else if ( forFaction.SecondsSinceBrownout > 0 )
                                    {
                                        buffer.Add( "已停止（停电）", "ff2222" );
                                    } else if ( forFaction.NetEnergy < entityType.EnergyUsage )
                                    {
                                        buffer.Add( "已停止（能量不足）", "cc2222" );
                                    } else if ( World_AIW2.Instance.IsFuelEnabled && relatedEntityTypeData.FuelUseType == ResourceType.FuelArgon && forFaction.NetFuelArgon < entityType.FuelUse )
                                    {
                                        buffer.Add( "已停止（氩气不足）", "cc2222" );
                                    } else if ( World_AIW2.Instance.IsFuelEnabled && relatedEntityTypeData.FuelUseType == ResourceType.FuelRadon && forFaction.FuelRadonConsumption < entityType.FuelUse )
                                    {
                                        buffer.Add( "已停止（氡气不足）", "cc2222" );
                                    } else if ( World_AIW2.Instance.IsFuelEnabled && relatedEntityTypeData.FuelUseType == ResourceType.FuelXenon && forFaction.FuelXenonConsumption < entityType.FuelUse )
                                    {
                                        buffer.Add( "已停止（氙气不足）", "cc2222" );
                                    } else if ( hullCurr == 1 )
                                    {
                                        buffer.Add( "就绪", "55dd55" );
                                    } else
                                    {
                                        buffer.AddPercentageInColor( hullPercent.IntValue.ToString( "00" ) + "%", hullPercent, true, false ).Add( " 完成" );
                                        if ( ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() && GameSettings.Current.GetBoolBySetting( "ShowTooltipProgressBars" ) )
                                            ArcenExternalUIUtilities.AppendBar( buffer, hullPercent.IntValue, EntityText.GetProportionalStrengthColor( hullPercent.ToFloat() / 100f ), 20, 60, ColorMath.DarkRed );
                                        if ( detailLevel >= TooltipDetail.Medium )
                                        {
                                            buffer.Add( " (" );
                                            if ( detailLevel == TooltipDetail.Full )
                            buffer.Add( "金属: " );
                                            buffer.StartMetal( detailLevel != TooltipDetail.Full )
                                                .AddNumberTruncated( metalCurr * hullPercent / 100 ).Add( " / " ).AddNumberTruncated( metalCurr ).EndColor().Add( ")" );
                                        }
                                    }
                                }
                            }
                            buffer.EndSize();
                        }
                        #endregion
                        #region Construction
                        else if ( isUnderConstruction )
                        {
                            buffer.EndColor().NewLine();
                            cpi.ResetPos();
                            buffer.Add( "建造中: " ).AddSize_Small();
                            if ( relatedSquadOrNull.IsInHoldFireMode )
                            {
                                buffer.Add( "已暂停", "cccccc" );
                            } else
                            {
                                buffer.AddPercentageInColor( ( metalCurr - relatedSquadOrNull.SelfBuildingMetalRemaining ).ToPercent( metalCurr ), true, false ).Add( " 完成" );
                            }
                            if(detailLevel >= TooltipDetail.Medium)
                            {
                                buffer.Add( " (" );
                                if ( detailLevel == TooltipDetail.Full )
                                    buffer.Add( "金属: " );
                                buffer.StartMetal( detailLevel != TooltipDetail.Full )
                                    .AddNumberTruncated( metalCurr - relatedSquadOrNull.SelfBuildingMetalRemaining ).Add( " / " ).AddNumberTruncated( metalCurr ).EndColor().Add( ")" );
                            }
                            buffer.EndSize();
                        }
                        #endregion
                        #region Behavior
                        else
                        {
                            buffer.EndColor().NewLine();
                            cpi.ResetPos();
                            buffer.Add( "行为: " ).AddSize_Small();
                            if ( relatedSquadOrNull.IsInHoldFireMode )
                            {
                                buffer.Add( "待命", "444499" );
                            } 
                            else
                            {
                                switch ( relatedSquadOrNull.Orders.Behavior )
                                {
                                    case EntityBehaviorType.Attacker_Full:
                                        if ( markStats.Computed_BaseLongestWeaponRange <= 0 )
                                        {
                                            if ( isCenterpiece )
                                            {
                                                buffer.Add( "指挥中", "eeee88" );
                                            } 
                                            else
                                            {
                                                if ( entityFaction != null && entityFaction.Type == FactionType.Player )
                                                    buffer.Add( "追击模式", Window_InGameSelectionInfo.color_Pursuit );
                                                else
                                                    buffer.Add( "游荡", "eeee88" );
                                            }
                                        } 
                                        else
                                        {
                                            if ( entityFaction != null && entityFaction.Type == FactionType.Player )
                                                buffer.Add( "追击模式", Window_InGameSelectionInfo.color_Pursuit );
                                            else
                                                buffer.Add( "攻击所有", "ee8888" );
                                        }
                                        break;
                                    case EntityBehaviorType.Attacker_PursueOnlyInRange:
                                        buffer.Add( "攻击移动", Window_InGameSelectionInfo.color_AttackMove );
                                        break;
                                    case EntityBehaviorType.Guard_FleetShip:
                                        buffer.Add( "保护盟友", "88ee88" );
                                        break;
                                    case EntityBehaviorType.Guard_Guardian_Patrolling:
                                        buffer.Add( "巡逻区域", "88ee88" );
                                        break;
                                    case EntityBehaviorType.Guard_Guardian_Anchored:
                                        buffer.Add( "防御阵地", "88ee88" );
                                        break;
                                    case EntityBehaviorType.Stationary:
                                    case EntityBehaviorType.None:
                                        if ( relatedSquadOrNull.TypeData.IsMobile == false )
                                        {
                                            buffer.Add( "固定不动", "777777" );
                                        }
                                        else
                                        {
                                            buffer.Add( "防御中", "88ee88" );
                                        }
                                        break;
                                }
                            }
                            buffer.EndSize();
                            //if (detailLevel == TooltipDetail.Full )
                                //buffer.ToPos( cpi.AddStep_HundredFiftyPercent() );
                            //else
                                buffer.ToPos( cpi.AddStep().AddStep() );
                            #endregion
                            #region Orders
                            //if ( detailLevel >= TooltipDetail.Medium )
                            {
                                EntityOrder order = relatedSquadOrNull.Orders.GetQueuedOrderAtIndex_OrNull( 0 );
                                if ( order.TypeData != null )
                                {
                                    buffer.Add( "命令: " ).AddSize_Small();
                                    int queuedOrderCount = relatedSquadOrNull.Orders.GetQueuedOrderCount();
                                    Window_PrototypeInGameHoverEntityInfoUtils.WriteEntityOrder( relatedSquadOrNull, order, buffer );

                                    int count = GameSettings.Current.GetIntBySetting( "MaxDisplayedOrdersInTooltips" );
                                    int consecutivePathing = 0;
                                    short lastCyclePlanetIndex = -1;
                                    if ( order.TypeData.Type == EntityOrderType.Wormhole )
                                    {
                                        consecutivePathing = 1;
                                    }
                                    int i = 1;
                                    for ( ; i < count || consecutivePathing > 0; i++ )
                                    {
                                        order = relatedSquadOrNull.Orders.GetQueuedOrderAtIndex_OrNull( i );
                                        if ( order.TypeData == null )
                                        {
                                            if ( consecutivePathing > 1 )
                                            {
                                                buffer.Add( " (" ).Add( consecutivePathing ).Add( " 跳跃)" );
                                            }
                                            break;
                                        }

                                        if ( order.TypeData.Type == EntityOrderType.Wormhole )
                                        {
                                            if ( lastCyclePlanetIndex == order.RelatedPlanetIndex )//apparently this can happen when moving to a planet, and setting a new move command to a different planet...
                                            {
                                                count++;
                                                continue;
                                            }
                                            if ( consecutivePathing == 0 )
                                            {
                                                buffer.Add( ", " );
                                                Window_PrototypeInGameHoverEntityInfoUtils.WriteEntityOrder( relatedSquadOrNull, order, buffer );
                                            }
                                            consecutivePathing++;
                                            count++;
                                            lastCyclePlanetIndex = order.RelatedPlanetIndex;
                                            continue;//skip ahead, no need to show every planet individually!
                                        } 
                                        else
                                        {
                                            if ( consecutivePathing > 1 )
                                            {
                                                buffer.Add( " (" ).Add( consecutivePathing ).Add( " 跳跃)" );
                                            }
                                            consecutivePathing = 0;
                                        }
                                        if ( i >= count )
                                        {
                                            break;
                                        }
                                        buffer.Add( ", " );
                                        Window_PrototypeInGameHoverEntityInfoUtils.WriteEntityOrder( relatedSquadOrNull, order, buffer );
                                    }
                                    if ( queuedOrderCount - i > 0 )
                                    {
                                        buffer.Add( " + " ).Add( queuedOrderCount - i ).Add( " 更多" );
                                    }
                                    buffer.EndSize();
                                }
                            }
                            #endregion
                        }
                    }
                }
                #endregion

                debugStage = 15001;

                #region FithRow: Buffs
                int entityTimeOnPlanet = relatedSquadOrNull.GetSecondsSinceEnteringThisPlanet();//reuse in later stages for weapon damage modifiers, if needed
                FInt speedMultiplierWhileHacking = ExternalConstants.Instance.SpeedMultiplierWhileHacking;
                if ( detailLevel >= TooltipDetail.Medium )
                {
                    debugStage = 15001801;
                    bool wroteBuffStart = false;
                    #region Lye
                    //Lye = negative damage amplification, which is called Acid here
                    if ( relatedSquadOrNull.IncomingDamageAmplifiedDuration_Max15 > 0 &&
                        (relatedSquadOrNull.IncomingDamageAmplifiedByFlat < 0 || relatedSquadOrNull.IncomingDamageAmplifiedByMult > FInt.Zero && relatedSquadOrNull.IncomingDamageAmplifiedByMult < FInt.One) )
                    {
                        debugStage = 15001811;
                        Window_PrototypeInGameHoverEntityInfoUtils.WriteBuffsStartIfNeeded( buffer, ref wroteBuffStart );
                        buffer.Add( "酸蚀: (" ).Add( relatedSquadOrNull.IncomingDamageAmplifiedDuration_Max15 ).Add( "s, " );
                        if ( relatedSquadOrNull.IncomingDamageAmplifiedByFlat < 0 )
                        {
                            buffer.AddNumberMoreReadable( relatedSquadOrNull.IncomingDamageAmplifiedByFlat );
                        }
                        if ( relatedSquadOrNull.IncomingDamageAmplifiedByMult > FInt.Zero && relatedSquadOrNull.IncomingDamageAmplifiedByMult < FInt.One )
                        {
                            if ( relatedSquadOrNull.IncomingDamageAmplifiedByFlat < 0 )
                            {
                                buffer.Add( " / " );
                            }
                            buffer.AddPercentRoundedDynamically( (relatedSquadOrNull.IncomingDamageAmplifiedByMult - 1) * 100 );
                        }
                        buffer.Add( ")" );
                    }
                    #endregion

                    debugStage = 15001821;

                    #region Hull
                    bool wroteHullBuffStart = false;
                    if ( markStats.BaseHullPoints > 0 )
                    {
                        debugStage = 15001831;
                        if ( fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.HullMultiplier > FInt.One && !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteHullBuffsStartIfNeeded( buffer, false, ref wroteBuffStart, ref wroteHullBuffStart );
                            buffer.Add( "舰队增益: +" ).AddPercentRoundedDynamically( (fleetMembershipOrNull.Fleet.HullMultiplier - 1) * 100 );
                        }
                    }
                    #endregion

                    debugStage = 15001841;

                    #region Shield
                    if ( markStats.BaseShieldPoints > 0 )
                    {
                        debugStage = 15001851;

                        bool wroteShieldBuffStart = false;
                        if ( fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.ShieldsMultiplier > FInt.One && !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteShieldBuffsStartIfNeeded( buffer, false, ref wroteBuffStart, ref wroteShieldBuffStart );
                            buffer.Add( "舰队增益: +" ).AddPercentRoundedDynamically( (fleetMembershipOrNull.Fleet.ShieldsMultiplier - 1) * 100 );
                        }
                    }
                    #endregion

                    debugStage = 15001861;

                    #region Damage
                    if ( markStats.Computed_BaseLongestWeaponRange > 0 || markStats.AttritionDamagePreFleetModifiers > 0 )
                    {
                        debugStage = 15001871;

                        bool wroteDamageBuffStart = false;
                        if ( fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.AttackPowerMultiplier > FInt.One && !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteDamageBuffsStartIfNeeded( buffer, false, ref wroteBuffStart, ref wroteDamageBuffStart );
                            buffer.Add( "舰队增益: +" ).AddPercentRoundedDynamically( (fleetMembershipOrNull.Fleet.AttackPowerMultiplier - 1) * 100 );
                        }

                        if ( markStats.Computed_BaseLongestWeaponRange > 0 && relatedSquadOrNull.PlanetFaction != null && relatedSquadOrNull.PlanetFaction.CurrentAttackMultiplier > FInt.One )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteDamageBuffsStartIfNeeded( buffer, false, ref wroteBuffStart, ref wroteDamageBuffStart );
                            buffer.Add( "阵营星球增益: +" ).AddPercentRoundedDynamically( (relatedSquadOrNull.PlanetFaction.CurrentAttackMultiplier - 1) * 100 );
                        }

                        if ( relatedSquadOrNull.TypeData.AmountAddedToDamagePerShipOfThisTypeOnPlanet > 0 && relatedSquadOrNull.CalculatedAddedDamage > 0 )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteDamageBuffsStartIfNeeded( buffer, false, ref wroteBuffStart, ref wroteDamageBuffStart );
                            buffer.Add( "网络: +" ).AddNumberTruncated( relatedSquadOrNull.CalculatedAddedDamage );
                        }

                        if ( relatedSquadOrNull.NumberOfWeaponPoints > 0 )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteDamageBuffsStartIfNeeded( buffer, false, ref wroteBuffStart, ref wroteDamageBuffStart );
                            buffer.Add( "武器点数: " ).AddNumberMoreReadable( relatedSquadOrNull.NumberOfWeaponPoints );
                        }
                    }
                    #endregion

                    debugStage = 15001881;

                    #region Speed
                    if ( entityType.IsMobile )
                    {
                        debugStage = 15001891;

                        bool wroteSpeedBuffStart = false;
                        if ( relatedSquadOrNull.SpeedLimitFromGroupMove > 0 )
                        {
                            if ( relatedSquadOrNull.SpeedLimitFromGroupMove > markStats.Speed )
                            {
                                Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedBuffsStartIfNeeded( buffer, false, ref wroteBuffStart, ref wroteSpeedBuffStart );
                                if ( owningFactionOrNull != null && owningFactionOrNull.Type == FactionType.Player )
                                {
                                    buffer.Add( "编队移动: +" );
                                } else
                                {
                                    buffer.Add( "速度编组: +" );
                                }
                                buffer.AddNumberMoreReadable( relatedSquadOrNull.SpeedLimitFromGroupMove - markStats.Speed );
                            }
                        } else if ( fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.OverridingMinSpeed > markStats.Speed &&
                            !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet && !isCenterpiece )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedBuffsStartIfNeeded( buffer, false, ref wroteBuffStart, ref wroteSpeedBuffStart );
                            buffer.Add( "舰队增益: +" ).AddNumberMoreReadable( fleetMembershipOrNull.Fleet.OverridingMinSpeed - markStats.Speed );
                        }

                        if ( relatedSquadOrNull.PlanetFaction != null && ( relatedSquadOrNull.PlanetFaction.CurrentSpeedMultiplier > FInt.One || relatedSquadOrNull.PlanetFaction.CurrentSpeedFlatBonus > 0 ) )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedBuffsStartIfNeeded( buffer, false, ref wroteBuffStart, ref wroteSpeedBuffStart );
                            buffer.Add( "阵营星球增益: " );
                            if ( relatedSquadOrNull.PlanetFaction.CurrentSpeedMultiplier > FInt.One )
                            {
                                buffer.Add( "+ " ).AddPercentRoundedDynamically( (relatedSquadOrNull.PlanetFaction.CurrentSpeedMultiplier - 1) * 100 );
                                if ( relatedSquadOrNull.PlanetFaction.CurrentSpeedFlatBonus > 0 )
                                    buffer.Add( ", " );
                            }
                            if ( relatedSquadOrNull.PlanetFaction.CurrentSpeedFlatBonus > 0 )
                                buffer.Add( "+ " ).Add( relatedSquadOrNull.PlanetFaction.CurrentSpeedFlatBonus );
                        }

                        if ( relatedSquadOrNull.Planet != null && relatedSquadOrNull.Planet.UnitSpeedupPercentage > 0 )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedBuffsStartIfNeeded( buffer, false, ref wroteBuffStart, ref wroteSpeedBuffStart );
                            buffer.Add( "星球增益: " ).AddPercentRoundedDynamically( relatedSquadOrNull.Planet.UnitSpeedupPercentage, 100 );
                        }

                        if ( entityType.SpeedMultiplierFirst5SecondsOnPlanet > FInt.One && entityTimeOnPlanet <= 5 )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedBuffsStartIfNeeded( buffer, false, ref wroteBuffStart, ref wroteSpeedBuffStart );
                            buffer.Add( "快速部署: +" ).AddPercentRoundedDynamically( (entityType.SpeedMultiplierFirst5SecondsOnPlanet - 1) * 100 );
                            if ( detailLevel == TooltipDetail.Full )
                            {
                                buffer.Add( " (" ).Add( 6 - entityTimeOnPlanet ).Add( "s)" );
                            }
                        }

                        if ( !isCenterpiece && !relatedEntityTypeData.IsDrone )
                        {
                            EntityOrder order = relatedSquadOrNull.Orders.GetQueuedOrderAtIndex_OrNull( 0 );
                            if ( order.TypeData != null && order.TypeData.Type == EntityOrderType.GetIntoTransport )
                            {
                                Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedBuffsStartIfNeeded( buffer, false, ref wroteBuffStart, ref wroteSpeedBuffStart );
                                buffer.Add( "装载中: +" ).AddPercentRoundedDynamically( 2, 1 );
                            }
                        }

                        if ( isCenterpiece && relatedSquadOrNull.ActiveHack != null && speedMultiplierWhileHacking > FInt.One )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedBuffsStartIfNeeded( buffer, false, ref wroteBuffStart, ref wroteSpeedBuffStart );
                            buffer.Add( "黑客入侵: " ).AddPercentRoundedDynamically( (speedMultiplierWhileHacking - 1) * 100 );
                        }

                        if ( owningFactionOrNull != null && relatedSquadOrNull.Planet != null && relatedSquadOrNull.Planet.IsFimbulwintered && owningFactionOrNull.BenefitsFromFimbulwinter )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedBuffsStartIfNeeded( buffer, false, ref wroteBuffStart, ref wroteSpeedBuffStart );
                            buffer.Add( "芬布尔之冬: +" ).AddPercentRoundedDynamically( ExternalConstants.Instance.FimbulwinterSpeedupPercent, 100 );
                        }
                    }

                    #endregion

                    debugStage = 15001901;

                    #region Range
                    if ( relatedSquadOrNull.TypeData.AmountAddedToRangePerShipOfThisTypeOnPlanet > 0 )
                    {
                        debugStage = 15001911;
                        bool wroteRangeBuffStart = false;
                        if ( fleetMembershipOrNull != null && relatedSquadOrNull.CalculatedAddedRange > 0 )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteRangeBuffsStartIfNeeded( buffer, false, ref wroteBuffStart, ref wroteRangeBuffStart );
                            buffer.Add( "网络: +" ).AddNumberTruncated( relatedSquadOrNull.CalculatedAddedRange );
                        }
                    }
                    #endregion

                    debugStage = 15001921;

                    #region Cloak
                    if ( relatedSquadOrNull.GetMightPossiblyBeCloaked() )
                    {
                        debugStage = 15001931;

                        bool wroteCloakBuffStart = false;
                        int cloak = relatedSquadOrNull.GetCurrentCloakingPoints();
                        int cloakMax = relatedSquadOrNull.GetMaxCloakingPoints();
                        if ( cloak > 0 )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteCloakBuffsStartIfNeeded( buffer, false, ref wroteBuffStart, ref wroteCloakBuffStart );
                            buffer.AddNumberTruncated( cloak ).Add( " / " ).AddNumberTruncated( cloakMax );
                        }
                    }
                    #endregion

                    if ( wroteBuffStart )
                    {
                        buffer.EndSize();
                    }
                }
                #endregion

                debugStage = 16001;

                #region SixthRow: Debuffs
                if ( detailLevel >= TooltipDetail.Medium )
                {
                    bool wroteDebuffStart = false;
                    #region Acid
                    if ( relatedSquadOrNull.IncomingDamageAmplifiedDuration_Max15 > 0 && (relatedSquadOrNull.IncomingDamageAmplifiedByFlat > 0 || relatedSquadOrNull.IncomingDamageAmplifiedByMult > FInt.One) )
                    {
                        Window_PrototypeInGameHoverEntityInfoUtils.WriteDebuffsStartIfNeeded( buffer, ref wroteDebuffStart );
                        buffer.Add( "酸蚀: (" ).Add( relatedSquadOrNull.IncomingDamageAmplifiedDuration_Max15 ).Add( "s, +" );
                        if ( relatedSquadOrNull.IncomingDamageAmplifiedByFlat > 0 )
                        {
                            buffer.AddNumberMoreReadable( relatedSquadOrNull.IncomingDamageAmplifiedByFlat );
                        }
                        if ( relatedSquadOrNull.IncomingDamageAmplifiedByMult > FInt.One )
                        {
                            if ( relatedSquadOrNull.IncomingDamageAmplifiedByFlat > 0 )
                            {
                                buffer.Add( " / +" );
                            }
                            buffer.AddPercentRoundedDynamically( (relatedSquadOrNull.IncomingDamageAmplifiedByMult * 100) - 100 );
                        }
                        buffer.Add( ")" );
                    }
                    #endregion

                    #region Corrosion
                    if ( relatedSquadOrNull.CorrosionDamageToBeAppliedToMe > 0 )
                    {
                        Window_PrototypeInGameHoverEntityInfoUtils.WriteDebuffsStartIfNeeded( buffer, ref wroteDebuffStart );
                        buffer.Add( "腐蚀: " ).Add( relatedSquadOrNull.CorrosionDamageToBeAppliedToMe );
                    }
                    #endregion

                    #region Paralysis
                    if ( relatedSquadOrNull.CurrentParalysisSeconds > 0 )
                    {
                        Window_PrototypeInGameHoverEntityInfoUtils.WriteDebuffsStartIfNeeded( buffer, ref wroteDebuffStart );
                        buffer.Add( "麻痹: (" ).Add( relatedSquadOrNull.CurrentParalysisSeconds ).Add( "s)" );
                    }
                    #endregion

                    #region ReloadSlow
                    if ( relatedSquadOrNull.CurrentWeaponAddedReloadSeconds > 0 )
                    {
                        Window_PrototypeInGameHoverEntityInfoUtils.WriteDebuffsStartIfNeeded( buffer, ref wroteDebuffStart );
                        buffer.Add( "装填减速: (" ).Add( relatedSquadOrNull.CurrentWeaponAddedReloadSeconds ).Add( "s)" );
                    }
                    #endregion


                    #region Hull
                    bool wroteHullDebuffStart = false;
                    if ( markStats.BaseHullPoints > 0 )
                    {
                        if ( fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.HullMultiplier < FInt.One && fleetMembershipOrNull.Fleet.HullMultiplier != FInt.Zero &&
                            !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteHullDebuffsStartIfNeeded( buffer, false, ref wroteDebuffStart, ref wroteHullDebuffStart );
                            buffer.Add( "舰队压制: " ).AddPercentRoundedDynamically( (fleetMembershipOrNull.Fleet.HullMultiplier * 100) - 100 );
                        }
                    }
                    #endregion

                    #region Shield
                    if ( markStats.BaseShieldPoints > 0 )
                    {
                        bool wroteShieldDebuffStart = false;
                        if ( fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.ShieldsMultiplier < FInt.One && fleetMembershipOrNull.Fleet.ShieldsMultiplier != FInt.Zero &&
                            !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteShieldDebuffsStartIfNeeded( buffer, false, ref wroteDebuffStart, ref wroteShieldDebuffStart );
                            buffer.Add( "舰队压制: " ).AddPercentRoundedDynamically( (fleetMembershipOrNull.Fleet.ShieldsMultiplier * 100) - 100 );
                        }
                    }
                    #endregion

                    #region Damage
                    if ( markStats.Computed_BaseLongestWeaponRange > 0 || markStats.AttritionDamagePreFleetModifiers > 0 )
                    {
                        bool wroteDamageDebuffStart = false;
                        if ( fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.AttackPowerMultiplier < FInt.One && fleetMembershipOrNull.Fleet.AttackPowerMultiplier != FInt.Zero &&
                            !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteDamageDebuffsStartIfNeeded( buffer, false, ref wroteDebuffStart, ref wroteDamageDebuffStart );
                            buffer.Add( "舰队压制: " ).AddPercentRoundedDynamically( (fleetMembershipOrNull.Fleet.AttackPowerMultiplier * 100) - 100 );
                        }

                        if ( markStats.Computed_BaseLongestWeaponRange > 0 && relatedSquadOrNull.PlanetFaction != null && relatedSquadOrNull.PlanetFaction.CurrentAttackMultiplier < FInt.One && relatedSquadOrNull.PlanetFaction.CurrentAttackMultiplier != FInt.Zero &&
                            !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteDamageDebuffsStartIfNeeded( buffer, false, ref wroteDebuffStart, ref wroteDamageDebuffStart );
                            buffer.Add( "阵营星球压制: " ).AddPercentRoundedDynamically( (relatedSquadOrNull.PlanetFaction.CurrentAttackMultiplier * 100) - 100 );
                        }

                        if ( !isUsingFakeEntityAsStandin && markStats.Computed_BaseLongestWeaponRange > 0 &&
                            (relatedSquadOrNull.IsUnderDamageReducingShield.Display || 
                             relatedSquadOrNull.GetIsSelfEmittingProtectingShield_ReduceDamage()) )
                        {
                            EntitySystemTypeData systemType;
                            bool foundAny = false;
                            bool toEveryWeapon = true;

                            List<string> SystemsAffected = ArcenStrings.GetTemporaryStringList( "Window_InGameHoverEntityInfo-SystemsAffected", 10f );
                            if ( SystemsAffected == null ) //blocked for teardown/shutdown; bail
                                return false;

                            for ( int i = 0; i < entityType.SystemTypes.Count; i++ )
                            {
                                systemType = entityType.SystemTypes[i];
                                if ( systemType.DamageModifierWhileUnderForcefield > FInt.One )//early out ASAP
                                {
                                    toEveryWeapon = false;
                                    continue;
                                }
                                if ( systemType.Category != EntitySystemCategory.Weapon )
                                {
                                    continue;
                                }
                                if ( systemType.MaxMarkLevelToFunction < markLevel.Ordinal || systemType.MinMarkLevelToFunction > markLevel.Ordinal )
                                {
                                    continue;
                                }
                                if ( systemType.IsModule && !systemType.IsModuleOn( relatedMembershipOrNull ) )
                                {
                                    continue;
                                }
                                if ( systemType.MustBeThisStateOfMatterToBeEnabled != null && systemType.MustBeThisStateOfMatterToBeEnabled != relatedSquadOrNull.CurrentStateOfMatter )
                                {
                                    continue;
                                }
                                if ( systemType.ShotTypeData.Category != GameEntityCategory.Shot )
                                {
                                    continue;
                                }
                                if ( systemType.SystemIsHiddenForUI )
                                {
                                    continue;
                                }
                                if ( detailLevel == TooltipDetail.Full )
                                {
                                    SystemsAffected.Add( systemType.DisplayName );
                                }
                                foundAny = true;
                            }
                            if ( foundAny )
                            {
                                Window_PrototypeInGameHoverEntityInfoUtils.WriteDamageDebuffsStartIfNeeded( buffer, false, ref wroteDebuffStart, ref wroteDamageDebuffStart );
                                buffer.Add( "护盾力场下: -50%" );
                                if ( !toEveryWeapon )
                                {
                                    buffer.Add( " 至 " );
                                    if ( detailLevel == TooltipDetail.Full )
                                    {
                                        bool isFirst = true;
                                        for ( int i = 0; i < SystemsAffected.Count; i++ )
                                        {
                                            if ( !isFirst )
                                            {
                                                buffer.Add( ", " );
                                                isFirst = false;
                                            }
                                            buffer.Add( SystemsAffected[i] );
                                        }
                                    } else
                                    {
                                        buffer.Add( "某些武器" );
                                    }
                                }
                            }

                            ArcenStrings.ReleaseTemporaryStringList( SystemsAffected );
                        }
                    }
                    #endregion

                    #region Speed
                    if ( entityType.IsMobile )
                    {
                        bool wroteSpeedDebuffStart = false;

                        #region EngineSlow
                        if ( relatedSquadOrNull.CurrentEngineStunSeconds > 0 )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedDebuffsStartIfNeeded( buffer, false, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                            int index = relatedSquadOrNull.CurrentEngineStunSeconds;
                            if ( index >= ExternalConstants.Instance.EngineStunMultipliersByStunSeconds.Count )
                            {
                                index = ExternalConstants.Instance.EngineStunMultipliersByStunSeconds.Count - 1;
                            }
                            FInt slowFactor = ExternalConstants.Instance.EngineStunMultipliersByStunSeconds[index];
                            if ( slowFactor <= FInt.Zero )
                            {
                                buffer.Add( "引擎眩晕: (" ).Add( relatedSquadOrNull.CurrentEngineStunSeconds ).Add( "s, -100%)" );
                            } else
                            {
                                buffer.Add( "引擎减速: (" ).Add( relatedSquadOrNull.CurrentEngineStunSeconds ).Add( "s, " ).AddPercentRoundedDynamically( (slowFactor * 100) - 100 ).Add( ")" );
                            }
                        }
                        #endregion

                        if ( relatedSquadOrNull.CurrentCountOfTractorsPullingOnThis > 0 )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedDebuffsStartIfNeeded( buffer, false, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                            buffer.Add( "被牵引光束捕获: -100%" );
                        } else
                        {
                            if ( relatedSquadOrNull.SpeedLimitFromGroupMove > 0 && relatedSquadOrNull.SpeedLimitFromGroupMove < markStats.Speed )
                            {
                                Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedDebuffsStartIfNeeded( buffer, false, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                                if ( owningFactionOrNull != null && owningFactionOrNull.Type == FactionType.Player )
                                {
                                    buffer.Add( "编队移动: " );
                                } else
                                {
                                    buffer.Add( "速度编组: " );
                                }
                                buffer.AddNumberMoreReadable( relatedSquadOrNull.SpeedLimitFromGroupMove - markStats.Speed );
                            }

                            if ( relatedSquadOrNull.PlanetFaction != null && ( relatedSquadOrNull.PlanetFaction.CurrentSpeedMultiplier < FInt.One || relatedSquadOrNull.PlanetFaction.CurrentSpeedFlatBonus < 0 ) )
                            {
                                Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedDebuffsStartIfNeeded( buffer, false, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                                buffer.Add( "阵营星球压制: " );
                                if ( relatedSquadOrNull.PlanetFaction.CurrentSpeedMultiplier < FInt.One )
                                {
                                    buffer.Add( " " ).AddPercentRoundedDynamically( (relatedSquadOrNull.PlanetFaction.CurrentSpeedMultiplier * 100) - 100 );
                                    if ( relatedSquadOrNull.PlanetFaction.CurrentSpeedFlatBonus > 0 )
                                        buffer.Add( ", " );
                                }
                                if ( relatedSquadOrNull.PlanetFaction.CurrentSpeedFlatBonus > 0 )
                                    buffer.Add( " " ).Add( relatedSquadOrNull.PlanetFaction.CurrentSpeedFlatBonus );
                            }

                            if ( relatedSquadOrNull.Planet != null && relatedSquadOrNull.Planet.UnitSlowPercentage > 0 )
                            {
                                Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedDebuffsStartIfNeeded( buffer, false, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                                buffer.Add( "星球压制: " ).AddPercentRoundedDynamically( -relatedSquadOrNull.Planet.UnitSlowPercentage, 100 );
                            }

                            if ( entityType.SpeedMultiplierFirst5SecondsOnPlanet < FInt.One && entityType.SpeedMultiplierFirst5SecondsOnPlanet != FInt.Zero && entityTimeOnPlanet <= 5 )
                            {
                                Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedDebuffsStartIfNeeded( buffer, false, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                                buffer.Add( "抵达: " ).AddPercentRoundedDynamically( (entityType.SpeedMultiplierFirst5SecondsOnPlanet * 100) - 100 );
                                if ( detailLevel == TooltipDetail.Full )
                                {
                                    buffer.Add( " (for " ).Add( 6 - entityTimeOnPlanet ).Add( "s)" );
                                }
                            }

                            if ( isCenterpiece && relatedSquadOrNull.ActiveHack != null && speedMultiplierWhileHacking < FInt.One )
                            {
                                Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedDebuffsStartIfNeeded( buffer, false, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                                buffer.Add( "黑客入侵: " ).AddPercentRoundedDynamically( (speedMultiplierWhileHacking - 1) * 100 );
                            }

                            if ( owningFactionOrNull != null && relatedSquadOrNull.Planet != null && relatedSquadOrNull.Planet.IsFimbulwintered && !owningFactionOrNull.BenefitsFromFimbulwinter )
                            {
                                Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedDebuffsStartIfNeeded( buffer, false, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                                buffer.Add( "芬布尔之冬: " ).AddPercentRoundedDynamically( -ExternalConstants.Instance.FimbulwinterSlowdownPercent, 100 );
                            }

                            FInt grav = relatedSquadOrNull.CurrentGravitySpeedMultiplier.Display;
                            if ( grav < FInt.One && grav != FInt.Zero )
                            {
                                Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedDebuffsStartIfNeeded( buffer, false, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                                buffer.Add( "重力: " ).AddPercentRoundedDynamically( grav * 100 );
                            }
                        }
                    }

                    #endregion

                    #region Cloak
                    if ( relatedSquadOrNull.IsFakeEntity == false && 
                         relatedSquadOrNull.GetMightPossiblyBeCloaked() )
                    {
                        bool wroteCloakBuffStart = false;
                        int cloak = relatedSquadOrNull.GetCurrentCloakingPoints();
                        int cloakMax = relatedSquadOrNull.GetMaxCloakingPoints();
                        if ( cloak == 0 && cloakMax > 0 )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteDecloakDebuffsStartIfNeeded( buffer, false, true, ref wroteDebuffStart, ref wroteCloakBuffStart );
                            if ( relatedSquadOrNull.ActiveHack != null )
                            {
                                buffer.Add( ", 因黑客入侵而禁用" );
                            } else if ( relatedSquadOrNull.GetIsCrippled() )
                            {
                                buffer.Add( ", 已致残" );
                            } else if ( detailLevel == TooltipDetail.Full )
                            {
                                buffer.Add( " (Regen: " ).Add( 1 + ExternalConstants.Instance.SecondsToWaitBeforeRecloaking - (World_AIW2.Instance.GameSecond - relatedSquadOrNull.GameSecondOfLastCloakingPointLoss) )
                                    .Add( "s)" );
                            }
                        } 
                        else if ( World_AIW2.Instance.GameSecond - relatedSquadOrNull.GameSecondOfLastCloakingPointLoss < 2 )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteDecloakDebuffsStartIfNeeded( buffer, false, false, ref wroteDebuffStart, ref wroteCloakBuffStart );
                        }
                    }
                    #endregion

                    if (relatedEntityTypeData.IsTurret && AIWar2GalaxySettingQuickAccess.UseHarshTurretRangesOnNonHumanPlanets ) {
                        if ( owningFactionOrNull?.Type == FactionType.Player && thisPlanetOrNull?.GetControllingFactionType() != FactionType.Player  ) {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteDebuffsStartIfNeeded( buffer, ref wroteDebuffStart );
                            buffer.WrapSpeed("射程: ", false, false).Add("在敌方星球上受限");
                        }
                    }

                    if ( wroteDebuffStart )
                    {
                        buffer.EndSize();
                    }
                }
                #endregion

                /*if ( buffer is ArcenDoubleCharacterBuffer )
                    ArcenDebugging.SingleLineQuickDebug( ((ArcenDoubleCharacterBuffer) buffer).GetStringAndResetForNextUpdate() );
                else
                    ArcenDebugging.SingleLineQuickDebug( ((ArcenCharacterBuffer) buffer).ToStringAndReturnToPool() );
                */  
                debugStage = 17001;

                #region SeventhRow: Build Stats
                if ( panelMode == Mode.Build || entityCanBeClaimed )
                {
                    int fuelUse = entityType.FuelUse;
                    int fuelMax = 0;
                    if ( World_AIW2.Instance.IsFuelEnabled )
                    {
                        if ( entityType.FuelUseType == ResourceType.FuelArgon )
                            fuelMax = storedArgon;
                        else if ( entityType.FuelUseType == ResourceType.FuelRadon )
                            fuelMax = storedRadon;
                        else if ( entityType.FuelUseType == ResourceType.FuelXenon )
                            fuelMax = storedXenon;
                    }
                    bool isForNecromancer = false;
                    if(localPlayerFactionOrNull != null)
                    {
                        string typeName = localPlayerFactionOrNull.PlayerTypeName_ModeratelyExpensive;
                        if ( typeName.Equals( "NecromancerEmpire" ) || typeName.Equals( "NecromancerSidekick" ) )
                            isForNecromancer = true;
                    }
                    WriteBuildStatRow( buffer, isForNecromancer, cpi, detailLevel, 1, strengthCurr, hullMax, shieldMax, metalCurr, storedMetal, storedEnergy, energyCurr,
                        entityType.FuelUseType, fuelUse, fuelMax );

                    int forCount = 0;
                    if ( OptionalCountToShow > 1 )
                    {
                        forCount = OptionalCountToShow;
                    } else if ( relatedSquadOrNull.ExtraStackedSquadsInThis > 0 )
                    {
                        forCount = relatedSquadOrNull.ExtraStackedSquadsInThis + 1;
                    } else if ( fleetMembershipOrNull?.EffectiveSquadCap > 1 )
                    {
                        forCount = fleetMembershipOrNull.EffectiveSquadCap;
                    }

                    if ( forCount > 1 )
                    {
                        WriteBuildStatRow( buffer, isForNecromancer, cpi, detailLevel, forCount, strengthCurr, hullMax, shieldMax, metalCurr, storedMetal, storedEnergy, energyCurr,
                            entityType.FuelUseType, fuelUse, fuelMax );
                    }
                }
                #endregion

                debugStage = 18001;

                #region EightRow: Ship Class
                if ( !relatedEntityTypeData.ShipClass.IsDefault && relatedEntityTypeData.ShipClass.WriteDescriptionSoonerInTooltip )
                {
                    FactionType fType = FactionType.Player;
                    if ( entityFaction != null )
                        fType = entityFaction.Type;
                    Window_PrototypeInGameHoverEntityInfoUtils.WriteShipClass_Complete( buffer, relatedEntityTypeData.ShipClass, detailLevel, fType, markStats.Speed );
                }
                #endregion
                
                debugStage = 19001;

                //========End SirLimbo Bits

                //note: The code expects you to be missing a newline here, and it will add one after it.
                //      If you end on a newline, you'll get an extra space.

                //note: This DebugText is meant for programmers to be able to add random text in as they need to.
                //      It isn't meant to ever be shown to end users, but it still needs to be here.
                if ( !relatedSquadOrNull.IsFakeEntity && relatedSquadOrNull.DebugText.Length > 0 )
                    buffer.Add( "\n" ).Add( relatedSquadOrNull.DebugText );

                Color healthColor = ColorMath.LightGreen;
                Color shieldsColor = ColorMath.LightCyan;
                //Color squadShipCountColor = ColorMath.LightBlue;
                Color cloakColor = ColorMath.PaleVioletRed;
                Color armorColor = ColorMath.LighterRed;
                Color massColor = ColorMath.LightOrange;
                Color albedoColor = ColorMath.LightPurple;
                Color speedColor = ColorMath.LightSkyBlue;
                Color engineColor = ColorMath.LightWhipBlue;

                /*
                debugStage = 50;
                if ( panelMode == Mode.Build )
                {
                    buffer.NewLine();
                    cpi.ResetPos();
                    if ( OptionalCountToShow > 0 )
                    {
                        buffer.Add( "<b>Count</b>:  " );
                        buffer.Add( " " ).AddNumberMoreReadable( OptionalCountToShow ).Add( "     " );
                    }

                    buffer.Add( "     " );
                    ArcenExternalUIUtilities.AddSizeAndVOssetToText( buffer, ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon, 12, 2 );
                    AddSingleValueStrengthOnly( buffer, relatedMembershipOrNull != null && relatedMemFleetOrNull.IsPlayerStyleFleet ? relatedMembershipOrNull.GetStrengthPerSquad_PlayerFleetsOnly() : markStats.StrengthPerSquad_CalculatedWithNullFleetMembership );
                    buffer.EndColor();

                    if ( relatedMembershipOrNull != null && localPlayerFaction.NetEnergy < relatedMembershipOrNull.GetEnergyUsage() && relatedMembershipOrNull.GetEnergyUsage() > 0 )
                        buffer.Add( "\n" ).StartColor( QuickColors.Danger ).Add( "Ship requires " ).Add( relatedMembershipOrNull.GetEnergyUsage() )
                            .Add( " energy to function, but you only have " ).Add( localPlayerFaction.NetEnergy ).Add( " available." ).EndColor();
                }*/

                debugStage = 60;

                debugStage = 80;

                debugStage = 97;

                bool showWeaponActivityDetails = GameSettings.Current.GetBoolBySetting( "ShowWeaponActivityDetails" );
                if (relatedSquadOrNull.IsFakeEntity)
                    showWeaponActivityDetails = false;
                
                debugStage = 140;
                int cloakSystem = -1;
                int tachyonSystem = -1;
                int tractorSystem = -1;
                int gravitySystem = -1;
                int attractantSystem = -1;
                for ( int i = 0; i < relatedEntityTypeData.SystemTypes.Count; i++ )
                {
                    EntitySystemTypeData systemData = relatedEntityTypeData.SystemTypes[i];
                    if ( systemData.MaxMarkLevelToFunction < effectiveMarkLevel || systemData.MinMarkLevelToFunction > effectiveMarkLevel || systemData.SystemIsHiddenForUI )
                        continue;

                    if ( systemData.CareAboutStateOfMatterToBeEnabled && systemData.IsInvisibleInTooltipsIfNotAMatchByStateOfMatter )
                    {
                        if ( !relatedSquadOrNull.IsFakeEntity && relatedSquadOrNull.CurrentStateOfMatter != systemData.MustBeThisStateOfMatterToBeEnabled )
                            continue; //skip completely, since this will be invisible AND disabled
                    }
                    if ( systemData.IsModule && !systemData.IsModuleOn( relatedMembershipOrNull ) )
                        continue;
                    if ( !isUsingFakeEntityAsStandin && !string.IsNullOrEmpty( systemData.ModuleForSummonerTag ) && !relatedSquadOrNull.HasSummonerTagEnabled( systemData.ModuleForSummonerTag ) )
                        continue;
                    EntitySystemTypeData.MarkLevelStats systemForMarkLevel = systemData.ForMark[effectiveMarkLevel];
                    if ( cloakSystem < 0 && systemData.ForMark[effectiveMarkLevel].CloakingPoints > 0 )
                        cloakSystem = i;
                    if ( tachyonSystem < 0 && (systemData.TachyonHitsAlbedoLessThan > FInt.Zero || systemData.TachyonHitsAlbedoMoreThan > FInt.Zero) )
                        tachyonSystem = i;
                    if ( tractorSystem < 0 && systemData.ForMark[effectiveMarkLevel].TractorCount > 0 )
                        tractorSystem = i;
                    if ( gravitySystem < 0 && systemData.GravityHitsEngine_gxLessThan > 0 )
                        gravitySystem = i;
                    if ( attractantSystem < 0 && systemData.ForMark[effectiveMarkLevel].AttractRangeForShotsAgainstAllies > 0 )
                        attractantSystem = i;
                }

                bool hasSystemWithBonusFromAttackingUnderForcefields = false;

                debugStage = 180;
                for ( int i = 0; i < relatedEntityTypeData.SystemTypes.Count; i++ )
                {
                    debugStage = 182;
                    EntitySystemTypeData systemData = relatedEntityTypeData.SystemTypes[i];
                    if ( systemData.MaxMarkLevelToFunction < effectiveMarkLevel || systemData.MinMarkLevelToFunction > effectiveMarkLevel || systemData.SystemIsHiddenForUI )
                        continue;
                    if ( systemData.CareAboutStateOfMatterToBeEnabled && systemData.IsInvisibleInTooltipsIfNotAMatchByStateOfMatter )
                    {
                        if ( !relatedSquadOrNull.IsFakeEntity && relatedSquadOrNull.CurrentStateOfMatter != systemData.MustBeThisStateOfMatterToBeEnabled )
                            continue; //skip completely, since this will be invisible AND disabled
                    }
                    if (!isUsingFakeEntityAsStandin)
                    {
                        if ( systemData.IsModule && !systemData.IsModuleOn( relatedMembershipOrNull ) )
                            continue;
                        if ( !string.IsNullOrEmpty( systemData.ModuleForSummonerTag ) && !relatedSquadOrNull.HasSummonerTagEnabled( systemData.ModuleForSummonerTag ) )
                            continue;
                    }

                    _WriteSystemInfo( buffer, i, relatedSquadOrNull, relatedMembershipOrNull, entityFaction, thisPlanetOrNull, systemData, effectiveMarkLevel, true, false, detailLevel, isForMultipleUnits, showWeaponActivityDetails,
                        ref hasSystemWithBonusFromAttackingUnderForcefields );
                }
                    
                debugStage = 300;
                buffer.Add( FontSizes.SLIGHTLY_SMALLER_SIZE_STRING ).Add( "\n" );
                if ( !relatedSquadOrNull.IsFakeEntity && relatedSquadOrNull.SecondsSpentAsRemains >= 0 )
                {
                    if ( detailLevel < TooltipDetail.Full )
                        buffer.Add( "<color=#ff4b21>这仅是单位的破损残骸。</color>" );
                    else
                        buffer.Add( "<color=#ff4b21>这仅是单位的破损残骸。残骸本身不产生作用，但可由重建单位进行重建。</color>" );
                }
                debugStage = 305;
                if ( detailLevel < TooltipDetail.Full && relatedEntityTypeData.DescriptionShort.Length > 0 && 
                    ( relatedEntityTypeData.DescriptionShort.Length != 3 || relatedEntityTypeData.DescriptionShort != "~*~" ) )
                {
                    buffer.StartColor( "aaaaaa" ).Add( relatedEntityTypeData.DescriptionShort ).Add( "</color>\n" );
                }
                else
                {
                    if ( relatedEntityTypeData.Description.Length > 0 && ( relatedEntityTypeData.Description.Length != 3 || relatedEntityTypeData.Description != "~*~" ) )
                        buffer.StartColor( "aaaaaa" ).Add( relatedEntityTypeData.Description ).Add( "</color>\n" );
                }
                debugStage = 30510;

                relatedEntityTypeData.ForAnyDataExtensions_AddToTooltip_MidSection_ForEntity( buffer, relatedSquadOrNull, relatedMembershipOrNull, relatedEntityTypeData, detailLevel );

                if ( (GameSettings.Current.GetBoolBySetting( "ShowEntityIDInHovertext" ) || showDebugInfoInTooltip )
                    && !relatedSquadOrNull.IsFakeEntity)
                {
                    buffer.Add("PrimaryKeyID " ).Add( relatedSquadOrNull.PrimaryKeyID, "22ff22" ).Add( ". ");
                    if ( relatedSquadOrNull.FireteamId > 0 )
                        buffer.Add("FireteamId <color=#22ff22>" ).Add( relatedSquadOrNull.FireteamId ).Add( "</color>. ");
                    if ( relatedSquadOrNull.FireteamSpecificationOrNull != null && relatedSquadOrNull.FireteamSpecificationOrNull.IsActive() )
                    {
                        buffer.Add("<color=#22ff22>");
                        relatedSquadOrNull.FireteamSpecificationOrNull.ToDisplayString(buffer);
                        buffer.Add( "</color>. ");
                    }
                }
                if ( relatedEntityTypeData.AllowedHopsFromCenterpiece > 0 )
                {
                    buffer.StartColor( "999999" ).Add( "此单位无法飞离其中心舰超过 " ).Add( relatedEntityTypeData.AllowedHopsFromCenterpiece, "a45e5e" ).Add( " 次跳跃。" ).EndColor();
                }

                debugStage = 30530;
                if ( !relatedSquadOrNull.IsFakeEntity && relatedSquadOrNull.FireteamId > 0 )
                {
                    debugStage = 30531;
                    if ( ( relatedSquadOrNull.GetIsFriendlyToLocalFaction_Safe() && detailLevel >= TooltipDetail.Full)
                       || GameSettings.Current.GetBoolBySetting( "ShowFireteamHistory" ) )
                    {
                        debugStage = 30532;
                        Fireteam team = null;
                        ExternalFactionBaseInfo baseInfo = relatedSquadOrNull.GetFactionBaseInfoOrNull_Safe();
                        if ( baseInfo != null )
                            team = (Fireteam)baseInfo.GetFireteamBaseById( relatedSquadOrNull.FireteamId );

                        if ( team != null )
                        {
                            //team can be null if we are racing with the long range planning code that clears/rebuilds the team list
                            debugStage = 30533;
                            buffer.Add( "\n火力队 " ).Add( team.FireTeamID, "10ffdd" ).Add( " 状态: " );
                            team.GetStatusForDisplay( buffer );
                            buffer.Add(". ");
                            if ( team.Target != null && showDebugInfoInTooltip )
                                buffer.Add("目标是 " + team.Target.ToStringWithPlanetAndOwner() ).Add("\n");
                            team.GetSpecificationForDisplay( buffer );
                            if ( team != null && team.History != null && team.History.Count > 0  && team.FireTeamID > 0 )
                            {
                                buffer.Add( "单位火力队历史:\n ");
                                for ( int i = team.History.Count - 1; i >= 0; i-- )
                                {
                                    buffer.Add("\t" + team.History[i].ToDisplayString( team ) +"\n");
                                }
                            }
                        }
                        else
                        {
                            buffer.Add("<color=#d57aff>Null fireteam despite fireteam ID " + relatedSquadOrNull.FireteamId + "?</color>  ");
                        }
                    }
                }
                debugStage = 30539;

                if ( showDebugInfoInTooltip && !relatedSquadOrNull.IsFakeEntity )
                {
                    debugStage = 305391;
                    buffer.Add( "舰队类别: " ).Add( relatedMemFleetOrNull == null ? "null" : EnumNameCache.GetName( relatedMemFleetOrNull.Category ) ).Add(".  ");
                    debugStage = 305392;
                    bool isThisSelectedVisually = relatedSquadOrNull.GetIsSelected();
                    debugStage = 305393;
                    bool isSelectedInListOfSquads = World_AIW2.Instance.LocalPlayerSelectedSquads_CanGiveOrders.Contains( relatedSquadOrNull ) ||
                        World_AIW2.Instance.LocalPlayerSelectedSquads_CannotGiveOrders.Contains( relatedSquadOrNull ); ;
                    debugStage = 305394;
                    if ( relatedMemFleetOrNull != null )
                    {
                        bool isSelectedByFleet = relatedMemFleetOrNull.IsConsideredSelected_NonSim;
                        debugStage = 305395;
                        buffer.Add( "已选择: " ).Add( !isThisSelectedVisually ? "视觉_否" : "视觉_是" ).Add( ", " )
                            .Add( !isSelectedInListOfSquads ? "列表_否" : "列表_是" ).Add( ", " )
                            .Add( !isSelectedByFleet ? "舰队_否" : "舰队_是" ).Add( ".  " );
                    }
                    debugStage = 305396;
                    ArcenRejectionReason disabledReason = relatedSquadOrNull.ComputeDisabledReason();
                    if ( disabledReason != ArcenRejectionReason.Unknown ) //unknown is "not disabled", so don't bother showing that!
                        buffer.Add( "禁用原因: " ).Add( EnumNameCache.GetName( disabledReason ) ).Add( ".  " );
                }
                debugStage = 30540;

                if ( detailLevel >= TooltipDetail.Full )
                {
                    if ( relatedEntityTypeData.IsCrippledInsteadOfDying && ( relatedSquadOrNull == null || relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player ) )
                    {
                        FInt extraCost = relatedEntityTypeData.GetExtraCostWhileCrippled();
                        buffer.StartColor( "999999" ).Add( "如果此单位变为致残状态，其修复费用将增加 " ).AddFixedDecimal( extraCost.ToFloatNonSim(), 2 ).Add( " 倍，直到它不再致残（恢复满血）。  " );

                        int hackingPointsLost = relatedEntityTypeData.GetHackingPointsLostWhenCrippled();
                        if ( hackingPointsLost > 0 )
                        {
                            buffer.Add( "每次致残时还将损失 " ).Add(
                                hackingPointsLost ).Add( " 个黑客点数。  " );
                        }
                        buffer.EndColor();
                    }
                    {
                        int time = relatedEntityTypeData.CustomRepairImpossibleForSecondsAfterDamagedByEnemy_Max15 > 0 ? relatedEntityTypeData.CustomRepairImpossibleForSecondsAfterDamagedByEnemy_Max15 : ExternalConstants.Instance.Balance_RepairImpossibleForSecondsAfterDamagedByEnemy;
                        buffer.StartColor( "999999" ).Add( "此单位在被敌人伤害后无法恢复生命值或被修复，直到 " ).Add( time, "a45e5e" ).Add( " 秒过去。  " ).EndColor();
                    }
                }
                
                if ( !relatedSquadOrNull.IsFakeEntity && relatedSquadOrNull.GetIsPlayerUnit())
                {
                    if ( relatedSquadOrNull.TypeData.SelfAttritionsXPercentPerSecondIfParentShipNotOnPlanet > 0 )
                    {
                        if ( detailLevel >= TooltipDetail.Full )
                            buffer.StartColor( "aaaaaa" ).Add( "此单位因自我损耗而永远无法修复。  " ).EndColor();
                        else
                            buffer.StartColor( "aaaaaa" ).Add( "此单位永远无法修复。  " );
                    }
                    else if ( relatedSquadOrNull.RepairImpossibleForSeconds > 0 )
                    {
                        if ( relatedSquadOrNull.TypeData.ImmuneToRepairs )
                        {
                            if ( relatedSquadOrNull.TypeData.SecondsToFullyRegenerateHull > FInt.Zero )
                                buffer.StartColor( "aaaaaa" ).Add( "此单位将在 " ).Add( relatedSquadOrNull.RepairImpossibleForSeconds, "aa3434" ).Add( " 秒后可恢复生命值。  " ).EndColor();
                        }
                        else
                        {
                            if ( relatedSquadOrNull.TypeData.SecondsToFullyRegenerateHull > FInt.Zero )
                                buffer.StartColor( "aaaaaa" ).Add( "此单位将在 " ).Add( relatedSquadOrNull.RepairImpossibleForSeconds, "aa3434" ).Add( " 秒后可恢复生命值或被修复。  " ).EndColor();
                            else
                                buffer.StartColor( "aaaaaa" ).Add( "此单位将在 " ).Add( relatedSquadOrNull.RepairImpossibleForSeconds, "aa3434" ).Add( " 秒后可被修复。  " ).EndColor();
                        }
                    }
                    //if ( relatedSquadOrNull.RepairExtraCostForSeconds > 0 )
                    //{
                    //    if ( !relatedSquadOrNull.TypeData.ImmuneToRepairs )
                    //    {
                    //        buffer.StartColor( "aaaaaa" ).Add( "Repairs to this unit will cost " ).Add(
                    //        ExternalConstants.Instance.Balance_MultiplierForShipsThatHaveExtraRepairCostTemporarily.ToFloatNonSim().ToString( "0.##" )
                    //        ).Add( "x more until " ).Add( relatedSquadOrNull.RepairExtraCostForSeconds, "aa3434" ).Add( "s pass.  " ).EndColor();
                    //    }
                    //}
                    //if ( relatedSquadOrNull.GetIsCrippled() )
                    //{
                    //    if ( !relatedSquadOrNull.TypeData.ImmuneToRepairs )
                    //    {
                    //        FInt extraCost = relatedEntityTypeData.GetExtraCostWhileCrippled();
                    //        buffer.StartColor( "aaaaaa" ).Add( "Repairs to this unit cost " ).Add( extraCost.ToFloatNonSim().ToString( "0.##" )
                    //        ).Add( "x more until it is no longer crippled (when it reaches full hull health).  " ).EndColor();
                    //    }
                    //}
                }

                    float secondsPerSimFrame = World_AIW2.Instance.SimulationProfile.SecondsPerFrameNonSim;
                float simFrameMultiplier = 1f / secondsPerSimFrame;

                //Main description items
                //--------------------------------------
                debugStage = 310;
                WriteTransformsInto(buffer, relatedSquadOrNull);

                #region Assistance Items
                if ( detailLevel >= TooltipDetail.Medium ) //general stuff only in medium mode
                {
                    debugStage = 320;
                    bool didTheRepairEntry = false;
                    EntityMetalFlowEntry metalFlow;

                    PlannedMetalFlow flow_AssistSelfConstruction = PlannedMetalFlow.Create( null, MetalFlowPurpose.None );
                    PlannedMetalFlow flow_AssistFactoryConstruction = PlannedMetalFlow.Create( null, MetalFlowPurpose.None );
                    PlannedMetalFlow flow_ClaimingNeutrals = PlannedMetalFlow.Create( null, MetalFlowPurpose.None );
                    PlannedMetalFlow flow_RebuildingRemains = PlannedMetalFlow.Create( null, MetalFlowPurpose.None );
                    PlannedMetalFlow flow_RepairingEnginesOfFriendlies = PlannedMetalFlow.Create( null, MetalFlowPurpose.None );
                    PlannedMetalFlow flow_RepairingHullsOfFriendlies = PlannedMetalFlow.Create( null, MetalFlowPurpose.None );
                    PlannedMetalFlow flow_RepairingShieldsOfFriendlies = PlannedMetalFlow.Create( null, MetalFlowPurpose.None );

                    debugStage = 32001;
                    if ( !relatedSquadOrNull.IsFakeEntity && !relatedSquadOrNull.GetIsCrippled() )
                    {
                        debugStage = 32002;
                        List<PlannedMetalFlow> flows = relatedSquadOrNull.SquadPlannedFlows.GetDisplayList();
                        for ( int flowIndex = 0; flowIndex < flows.Count; flowIndex++ )
                        {
                            debugStage = 32003;
                            PlannedMetalFlow plannedFlow = flows[flowIndex];
                            if ( plannedFlow.FromEntity == null || !plannedFlow.IsInRangeAtTheMoment )
                                continue;
                            switch ( plannedFlow.Purpose )
                            {
                                case MetalFlowPurpose.AssistSelfConstruction:
                                    flow_AssistSelfConstruction = plannedFlow;
                                    break;
                                case MetalFlowPurpose.AssistFactoryConstruction:
                                    flow_AssistFactoryConstruction = plannedFlow;
                                    break;
                                case MetalFlowPurpose.ClaimingNeutrals:
                                    flow_ClaimingNeutrals = plannedFlow;
                                    break;
                                case MetalFlowPurpose.RebuildingRemains:
                                    flow_RebuildingRemains = plannedFlow;
                                    break;
                                case MetalFlowPurpose.RepairingEnginesOfFriendlies:
                                    flow_RepairingEnginesOfFriendlies = plannedFlow;
                                    break;
                                case MetalFlowPurpose.RepairingHullsOfFriendlies:
                                    flow_RepairingHullsOfFriendlies = plannedFlow;
                                    break;
                                case MetalFlowPurpose.RepairingShieldsOfFriendlies:
                                    flow_RepairingShieldsOfFriendlies = plannedFlow;
                                    break;
                                case MetalFlowPurpose.BuildingDronesInternally: //don't care about showing
                                case MetalFlowPurpose.FactoryConstructionForPlayerMobileFleets: //don't care about showing
                                case MetalFlowPurpose.SelfConstruction: //don't care about showing
                                    break;
                            }
                        }
                    }

                    debugStage = 32101;
                    for ( MetalFlowPurpose purpose = MetalFlowPurpose.None + 1; purpose < MetalFlowPurpose.Length; purpose++ )
                    {
                        metalFlow = markStats.GetMetalFlow( purpose );
                        if ( !metalFlow.HasRealData )
                            continue;
                        if ( metalFlow.EffectiveThroughput == FInt.Zero )
                            continue;

                        if ( detailLevel < TooltipDetail.Full && (purpose == MetalFlowPurpose.ClaimingNeutrals || purpose == MetalFlowPurpose.RebuildingRemains) )
                            continue;  //claim neutral units and rebuild remains only shown in full mode

                        debugStage = 32102;
                        switch ( purpose )
                        {
                            case MetalFlowPurpose.AssistSelfConstruction:
                                debugStage = 32131;
                                buffer.Add( "辅助建造" ).Add( " (射程 <color=#ffdf72>" ).Add( markStats.AssistRange ).Add( "</color>, 速度 <color=#ffdf72>" )
                                    .Add( metalFlow.EffectiveThroughput.ReadableString ).Add( "</color>)" );
                                if ( flow_AssistSelfConstruction.FromEntity != null )
                                    WritePlannedMetalFlowBriefTargetInfo( buffer, flow_AssistSelfConstruction, "Build", detailLevel >= TooltipDetail.Full );
                                buffer.Add( ".  " );
                                break;
                            case MetalFlowPurpose.AssistFactoryConstruction:
                                debugStage = 32141;
                                buffer.Add( "增强工厂" ).Add( " (射程 <color=#ffdf72>" ).Add( markStats.AssistRange ).Add( "</color>, 速度 <color=#ffdf72>" )
                                    .Add( metalFlow.EffectiveThroughput.ReadableString ).Add( "</color>)" );
                                if ( flow_AssistFactoryConstruction.FromEntity != null )
                                    WritePlannedMetalFlowBriefTargetInfo( buffer, flow_AssistFactoryConstruction, "Boost", detailLevel >= TooltipDetail.Full );
                                buffer.Add( ".  " );
                                break;
                            case MetalFlowPurpose.ClaimingNeutrals:
                                debugStage = 32151;
                                buffer.Add( "占领中立单位" ).Add( " (射程 <color=#ffdf72>" ).Add( markStats.AssistRange ).Add( "</color>, 速度 <color=#ffdf72>" )
                                    .Add( metalFlow.EffectiveThroughput.ReadableString ).Add( "</color>)" );
                                if ( flow_ClaimingNeutrals.FromEntity != null )
                                    WritePlannedMetalFlowBriefTargetInfo( buffer, flow_ClaimingNeutrals, "Claim", detailLevel >= TooltipDetail.Full );
                                buffer.Add( ".  " );
                                break;
                            case MetalFlowPurpose.RebuildingRemains:
                                debugStage = 32161;
                                buffer.Add( "重建残骸" ).Add( " (射程 <color=#ffdf72>无限</color>, 速度 <color=#ffdf72>" )
                                    .Add( metalFlow.EffectiveThroughput.ReadableString ).Add( "</color>)" );
                                if ( flow_RebuildingRemains.FromEntity != null )
                                    WritePlannedMetalFlowBriefTargetInfo( buffer, flow_RebuildingRemains, "Rebuild", detailLevel >= TooltipDetail.Full );
                                buffer.Add( ".  " );
                                break;
                            case MetalFlowPurpose.FactoryConstructionForPlayerMobileFleets:
                                debugStage = 32171;
                                buffer.Add( "建造舰队单位" ).Add( " (速度 <color=#ffdf72>" )
                                    .Add( metalFlow.EffectiveThroughput.ReadableString ).Add( "</color>).  " );
                                break;
                            case MetalFlowPurpose.RepairingHullsOfFriendlies:
                            case MetalFlowPurpose.RepairingShieldsOfFriendlies:
                            case MetalFlowPurpose.RepairingEnginesOfFriendlies:
                                debugStage = 32181;
                                if ( didTheRepairEntry )
                                    continue;
                                //asssume that the 3 repairs always are together at the same quality and location.
                                //this could be incorrect someday, but the interface is so much more brief if we assume not.
                                didTheRepairEntry = true;
                                buffer.Add( "修复盟友" ).Add( " (射程 <color=#ffdf72>" ).Add( markStats.AssistRange ).Add( "</color>, 速度 <color=#ffdf72>" )
                                    .Add( metalFlow.EffectiveThroughput.ReadableString ).Add( "</color>)" );
                                if ( flow_RepairingHullsOfFriendlies.FromEntity != null )
                                    WritePlannedMetalFlowBriefTargetInfo( buffer, flow_RepairingHullsOfFriendlies, "Hull", detailLevel >= TooltipDetail.Full );
                                if ( flow_RepairingShieldsOfFriendlies.FromEntity != null )
                                    WritePlannedMetalFlowBriefTargetInfo( buffer, flow_RepairingShieldsOfFriendlies, "Shields", detailLevel >= TooltipDetail.Full );
                                if ( flow_RepairingEnginesOfFriendlies.FromEntity != null )
                                    WritePlannedMetalFlowBriefTargetInfo( buffer, flow_RepairingEnginesOfFriendlies, "Engines", detailLevel >= TooltipDetail.Full );
                                buffer.Add( ".  " );
                                break;
                            default:
                                continue;
                        }
                        debugStage = 32231;

                    }
                }
                #endregion

                debugStage = 330;

                #region Drones
                if ( relatedEntityTypeData.FleetDesignTemplateIUseForDrones != null && 
                    ( relatedEntityTypeData.FleetDesignTemplateIUseForDrones.Strikecraft.DrawBag.GetHasItems() ||
                    relatedEntityTypeData.FleetDesignTemplateIUseForDrones.Frigates.DrawBag.GetHasItems()  ) )
                {
                    debugStage = 340;
                    if ( detailLevel < TooltipDetail.Full )
                        buffer.Add( "自动建造以下类型的无人机: <color=#ffdf72>" );
                    else
                        buffer.Add( "自动建造以下类型的无人机，并在受到威胁时释放: <color=#ffdf72>" );
                    bool isFirst = true;

                    debugStage = 350;
                    DrawBag<FleetItem> drones = relatedEntityTypeData.FleetDesignTemplateIUseForDrones.Strikecraft.DrawBag;
                    if ( drones != null )
                    {
                        for ( int j = 0; j < drones.InternalListSize; j++ )
                        {
                            FleetItem droneItem = drones.GetInternalListItemAtIndex( j );
                            if ( droneItem == null )
                                continue;
                            if ( isFirst )
                                isFirst = false;
                            else
                                buffer.Add( ", " );
                            buffer.Add( droneItem.TypeData.DisplayName );
                            buffer.Add( " x" );
                            int cap = droneItem.Cap;
                            if ( relatedEntityTypeData.MultipliedNonFrigateShipCapForDrones > FInt.Zero )
                                cap = ( cap * relatedEntityTypeData.MultipliedNonFrigateShipCapForDrones ).GetNearestIntPreferringHigher();
                            buffer.Add( cap );
                        }
                    }
                    drones = relatedEntityTypeData.FleetDesignTemplateIUseForDrones.Frigates.DrawBag;
                    if ( drones != null )
                    {
                        for ( int j = 0; j < drones.InternalListSize; j++ )
                        {
                            FleetItem droneItem = drones.GetInternalListItemAtIndex( j );
                            if ( droneItem == null )
                                continue;
                            if ( isFirst )
                                isFirst = false;
                            else
                                buffer.Add( ", " );
                            buffer.Add( droneItem.TypeData.DisplayName );
                            buffer.Add( " x" );
                            int cap = droneItem.Cap;
                            if ( relatedEntityTypeData.MultipliedFrigateShipCapForDrones > FInt.Zero )
                                cap = ( cap * relatedEntityTypeData.MultipliedFrigateShipCapForDrones ).GetNearestIntPreferringHigher();
                            buffer.Add( cap );
                        }
                    }
                    buffer.Add( "</color>.  " );
                }
                #endregion

                debugStage = 360;

                #region Resource Generation
                {
                    bool handledMetalStorage = false;
                    FInt resourceProductionPreBonuses;
                    for ( ResourceType resource = ResourceType.None + 1; resource < ResourceType.Length; resource++ )
                    {
                        resourceProductionPreBonuses = markStats.GetResourceProductionBeforeAnyBonuses( resource );
                        if ( resourceProductionPreBonuses == FInt.Zero )
                            continue;

                        switch (resource)
                        {
                            case ResourceType.Metal:
                                if ( !showMetalOther )
                                    continue;
                                break;
                            case ResourceType.Energy:
                                if ( !showEnergyProduction )
                                    continue;
                                break;
                            case ResourceType.FuelArgon:
                            case ResourceType.FuelRadon:
                            case ResourceType.FuelXenon:
                                if ( !World_AIW2.Instance.IsFuelEnabled || !showAnyFuel )
                                    continue;
                                break;
                        }

                        if ( detailLevel < TooltipDetail.Full && (resource == ResourceType.Science || resource == ResourceType.Hacking) )
                            continue;  //science and hacking only shown in verbose mode

                        FInt productionFinal = resourceProductionPreBonuses;
                        switch ( resource )
                        {
                            case ResourceType.Metal:
                                if ( !relatedSquadOrNull.IsFakeEntity )
                                    productionFinal = relatedSquadOrNull.GetFullyMultipliedMetalToProduce();
                                else if ( thisPlanetOrNull != null ) //make it correct in build mode!
                                    productionFinal = GameEntity_Squad.DoMultiplierOfMetalAtPlanet( relatedEntityTypeData, productionFinal, thisPlanetOrNull );
                                break;
                            case ResourceType.Energy:
                                if ( !relatedSquadOrNull.IsFakeEntity )
                                    productionFinal = relatedSquadOrNull.GetFullyMultipliedEnergyToProduce();
                                else if ( thisPlanetOrNull != null ) //make it correct in build mode!
                                    productionFinal = GameEntity_Squad.DoMultiplierOfEnergyAtPlanet( relatedEntityTypeData, productionFinal, thisPlanetOrNull );
                                break;
                        }

                        switch ( resource )
                        {
                            case ResourceType.Metal:
                            case ResourceType.FuelArgon:
                            case ResourceType.FuelRadon:
                            case ResourceType.FuelXenon:
                                buffer.Add( "产出 " );
                                break;
                            case ResourceType.Energy:
                                buffer.Add( "生成 " );
                                break;
                            case ResourceType.Hacking:
                            case ResourceType.Science:
                                buffer.Add( "收集 " );
                                break;
                            default:
                                continue;
                        }

                        buffer.Add( "<color=#ffdf72>" ).AddNumberMoreReadable( productionFinal.IntValue ).Add( "</color> " );

                        switch ( resource )
                        {
                            case ResourceType.Metal:
                                buffer.Add( " 金属/秒。  " );
                                if ( markStats.MetalStorage > 0 )
                                {
                                    handledMetalStorage = true;
                                    buffer.Add( "额外金属存储: <color=#ffd272>" ).AddNumberMoreReadable( markStats.MetalStorage ).Add( "</color>  " );
                                }
                                break;
                            case ResourceType.Energy:
                                buffer.Add( " 能量。  " );
                                break;
                            case ResourceType.Hacking:
                                buffer.Add( " 黑客点数/秒（在有剩余黑客点数的星球上）。  " );
                                break;
                            case ResourceType.Science:
                                buffer.Add( " 科学点数/秒（在有剩余科学点数的星球上）。  " );
                                break;
                            case ResourceType.FuelArgon:
                                buffer.Add( " 氩气燃料（用于主力战舰）。  " );
                                break;
                            case ResourceType.FuelRadon:
                                buffer.Add( " 氡气燃料（用于炮塔和力场）。  " );
                                break;
                            case ResourceType.FuelXenon:
                                buffer.Add( " 氙气燃料（用于军官和精英单位）。  " );
                                break;
                            default:
                                continue;
                        }
                    }
                    if ( !handledMetalStorage )
                    {
                        if ( markStats.MetalStorage > 0 )
                        {
                            handledMetalStorage = true;
                            buffer.Add( "额外金属存储: <color=#ffd272>" ).AddNumberMoreReadable( markStats.MetalStorage ).Add( "</color>  " );
                        }
                    }
                }
                #endregion

                debugStage = 370;

                if ( World_AIW2.Instance.PlayersAreInDistributedResourceGenerationMode )
                {
                    if ( showMetalOther )
                    {
                        #region Distributed Mode Metal Multipliers
                        {
                            debugStage = 370100;
                            if ( relatedEntityTypeData.MetalProductionMultiplierToAsteroids_DistributedModeOnly > FInt.Zero &&
                                relatedEntityTypeData.MetalProductionMultiplierToAsteroids_DistributedModeOnly != FInt.One )
                            {
                                debugStage = 370110;
                                buffer.Add( "<color=#ffdf72>" ).Add( relatedEntityTypeData.MetalProductionMultiplierToAsteroids_DistributedModeOnly.ReadableString ).Add( "x</color> " );
                                buffer.Add( " 金属/秒由本星球小行星生产。  " );
                            }
                        }
                        #endregion
                    }

                    if ( showEnergyProduction )
                    {
                        #region Distributed Mode Energy Multipliers
                        {
                            debugStage = 370100;
                            if ( relatedEntityTypeData.EnergyProductionMultiplierToAsteroids_DistributedModeOnly > FInt.Zero &&
                                relatedEntityTypeData.EnergyProductionMultiplierToAsteroids_DistributedModeOnly != FInt.One )
                            {
                                debugStage = 370110;
                                buffer.Add( "<color=#ffdf72>" ).Add( relatedEntityTypeData.EnergyProductionMultiplierToAsteroids_DistributedModeOnly.ReadableString ).Add( "x</color> " );
                                buffer.Add( " 能量由本星球小行星生成。  " );
                            }
                        }
                        #endregion
                    }
                }

                #region Resource Multipliers
                {
                    FInt resourceMultiplier;
                    for ( ResourceType resource = ResourceType.None + 1; resource < ResourceType.Length; resource++ )
                    {
                        resourceMultiplier = markStats.GetResourceProductionMultiplier( resource );
                        if ( resourceMultiplier == FInt.Zero )
                            continue;
                        switch ( resource )
                        {
                            case ResourceType.Metal:
                                if ( !showMetalOther )
                                    continue;
                                break;
                            case ResourceType.Energy:
                                if ( !showEnergyProduction )
                                    continue;
                                break;
                            case ResourceType.FuelArgon:
                            case ResourceType.FuelRadon:
                            case ResourceType.FuelXenon:
                                if ( !World_AIW2.Instance.IsFuelEnabled || !showAnyFuel )
                                    continue;
                                break;
                        }

                        debugStage = 372;

                        buffer.Add( "<color=#ffdf72>" ).Add( resourceMultiplier.ReadableString ).Add( "x</color> " );

                        switch ( resource )
                        {
                            case ResourceType.Metal:
                                buffer.Add( " 金属/秒在本星球生产。  " );
                                break;
                            case ResourceType.Energy:
                                buffer.Add( " 能量在本星球生成。  " );
                                break;
                            case ResourceType.FuelArgon:
                                buffer.Add( " 氩气燃料在本星球生产。  " );
                                break;
                            case ResourceType.FuelRadon:
                                buffer.Add( " 氡气燃料在本星球生产。  " );
                                break;
                            case ResourceType.FuelXenon:
                                buffer.Add( " 氙气燃料在本星球生产。  " );
                                break;
                            case ResourceType.Hacking:
                                buffer.Add( " 黑客点数/秒在本星球收集（如果有剩余黑客点数）。  " );
                                break;
                            case ResourceType.Science:
                                buffer.Add( " 科学点数/秒在本星球收集（如果有剩余科学点数）。  " );
                                break;
                            default:
                                continue;
                        }
                    }
                }
                #endregion

                debugStage = 375;

                #region Resource Multipliers After Time Being Here And Not Crippled
                if ( relatedEntityTypeData.TimeRequiredToBeHereAndNonCrippledToBoostMetalOrEnergyProduction > 0)
                {
                    if ( relatedSquadOrNull == null || relatedSquadOrNull.GetFactionTypeSafe() == FactionType.NaturalObject || relatedSquadOrNull.HasNotYetBeenFullyClaimed )
                    {
                        #region When no entity or an unclaimed ship
                        if ( relatedEntityTypeData.BoostToPlanetaryMetalProductionIfHaveBeenHereAndNonCrippledForXTime > FInt.One )
                        {
                            if ( relatedEntityTypeData.BoostToPlanetaryEnergyProductionIfHaveBeenHereAndNonCrippledForXTime > FInt.One )
                            {
                                buffer.Add( "<color=#ffdf72>" ).AddFixedDecimalThousands( relatedEntityTypeData.BoostToPlanetaryMetalProductionIfHaveBeenHereAndNonCrippledForXTime.ToFloatNonSim(), 2 )
                                    .Add( "x</color> 金属/秒，和 <color=#ffdf72>" )
                                    .AddFixedDecimalThousands( relatedEntityTypeData.BoostToPlanetaryEnergyProductionIfHaveBeenHereAndNonCrippledForXTime.ToFloatNonSim(), 2 )
                                    .Add( "x</color> 能量在该飞船所在星球产出（需在同一星球且非残废至少 " )
                                    .AddHoursAndMinutes( relatedEntityTypeData.TimeRequiredToBeHereAndNonCrippledToBoostMetalOrEnergyProduction ).Add( "）。若同星球有多个此类单位，仅最大加成生效。  " );
                            }
                            else
                            {
                                buffer.Add( "<color=#ffdf72>" ).AddFixedDecimalThousands( relatedEntityTypeData.BoostToPlanetaryMetalProductionIfHaveBeenHereAndNonCrippledForXTime.ToFloatNonSim(), 2 )
                                    .Add( "x</color> 金属/秒在该飞船所在星球产出（需在同一星球且非残废至少 " )
                                    .AddHoursAndMinutes( relatedEntityTypeData.TimeRequiredToBeHereAndNonCrippledToBoostMetalOrEnergyProduction ).Add( "）。若同星球有多个此类单位，仅最大加成生效。  " );
                            }
                        }
                        else
                        {
                            if ( relatedEntityTypeData.BoostToPlanetaryEnergyProductionIfHaveBeenHereAndNonCrippledForXTime > FInt.One )
                            {
                                buffer.Add( "<color=#ffdf72>" )
                                    .AddFixedDecimalThousands( relatedEntityTypeData.BoostToPlanetaryEnergyProductionIfHaveBeenHereAndNonCrippledForXTime.ToFloatNonSim(), 2 )
                                    .Add( "x</color> 能量在该飞船所在星球产出（需在同一星球且非残废至少 " )
                                    .AddHoursAndMinutes( relatedEntityTypeData.TimeRequiredToBeHereAndNonCrippledToBoostMetalOrEnergyProduction ).Add( "）。若同星球有多个此类单位，仅最大加成生效。  " );
                            }
                        }
                        #endregion
                    }
                    else if ( relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player )
                    {
                        int timeHasBeenHereAndNotCrippled = relatedSquadOrNull.GetSecondsSinceEnteringThisPlanetOrLastCrippled();
                        bool areBonusesOn = timeHasBeenHereAndNotCrippled >= relatedEntityTypeData.TimeRequiredToBeHereAndNonCrippledToBoostMetalOrEnergyProduction;
                        string prefix = areBonusesOn ? "<color=#ffed52>ON: </color>" : "<color=#ff8f52>OFF: </color>";
                        int timeRemaining = relatedEntityTypeData.TimeRequiredToBeHereAndNonCrippledToBoostMetalOrEnergyProduction - timeHasBeenHereAndNotCrippled;

                        #region When an entity exists
                        bool doesABoostBeingAtPlanetForTime = false;
                        if ( relatedEntityTypeData.BoostToPlanetaryMetalProductionIfHaveBeenHereAndNonCrippledForXTime > FInt.One )
                        {
                            if ( relatedEntityTypeData.BoostToPlanetaryEnergyProductionIfHaveBeenHereAndNonCrippledForXTime > FInt.One )
                            {
                                doesABoostBeingAtPlanetForTime = true;
                                buffer.Add( prefix ).Add( "<color=#ffdf72>" ).AddFixedDecimalThousands( relatedEntityTypeData.BoostToPlanetaryMetalProductionIfHaveBeenHereAndNonCrippledForXTime.ToFloatNonSim(), 2 )
                                    .Add( "x</color> 金属/秒，和 <color=#ffdf72>" )
                                    .AddFixedDecimalThousands( relatedEntityTypeData.BoostToPlanetaryEnergyProductionIfHaveBeenHereAndNonCrippledForXTime.ToFloatNonSim(), 2 )
                                    .Add( "x</color> 能量在此星球产出 " );
                            }
                            else
                            {
                                doesABoostBeingAtPlanetForTime = true;
                                buffer.Add( prefix ).Add( "<color=#ffdf72>" ).AddFixedDecimalThousands( relatedEntityTypeData.BoostToPlanetaryMetalProductionIfHaveBeenHereAndNonCrippledForXTime.ToFloatNonSim(), 2 )
                                    .Add( "x</color> 金属/秒在此飞船所在星球 " );
                            }
                        }
                        else
                        {
                            if ( relatedEntityTypeData.BoostToPlanetaryEnergyProductionIfHaveBeenHereAndNonCrippledForXTime > FInt.One )
                            {
                                doesABoostBeingAtPlanetForTime = true;
                                buffer.Add( prefix ).Add( "<color=#ffdf72>" )
                                    .AddFixedDecimalThousands( relatedEntityTypeData.BoostToPlanetaryEnergyProductionIfHaveBeenHereAndNonCrippledForXTime.ToFloatNonSim(), 2 )
                                    .Add( "x</color> 能量在此飞船所在星球 " );
                            }
                        }

                        if ( doesABoostBeingAtPlanetForTime )
                        {
                            if ( areBonusesOn )
                                buffer.Add( "因已在此星球且未受损超过 " )
                                    .AddHoursAndMinutes( relatedEntityTypeData.TimeRequiredToBeHereAndNonCrippledToBoostMetalOrEnergyProduction ).Add( ".  " );
                            else
                                buffer.Add( "一旦在此星球且未受损至少 " ).AddHoursAndMinutes( timeRemaining ).Add( " 后。  " );

                            if ( relatedSquadOrNull.NonSim_PlanetaryMetalBoostFailedFromOthersBeingPresent )
                            {
                                if ( relatedSquadOrNull.NonSim_PlanetaryEnergyBoostFailedFromOthersBeingPresent )
                                    buffer.StartColor( "ffae72" )
                                        .Add( "说明：这些金属和能量加成未生效，因为该星球存在多个同类加成单位且另一个加成更高。  " )
                                        .EndColor();
                                else
                                    buffer.StartColor( "ffae72" )
                                        .Add( "说明：此金属加成未生效，因为该星球存在多个同类加成单位且另一个加成更高。  " )
                                        .EndColor();
                            }
                            else if ( relatedSquadOrNull.NonSim_PlanetaryEnergyBoostFailedFromOthersBeingPresent )
                                buffer.StartColor( "ffae72" )
                                    .Add( "说明：此能量加成未生效，因为该星球存在多个同类加成单位且另一个加成更高。  " )
                                    .EndColor();
                        }
                        #endregion
                    }
                }
                #endregion

                debugStage = 380;
                if ( relatedEntityTypeData.HackableForCommandStationsAndBattleStations_DSSStyle && !relatedSquadOrNull.IsFakeEntity )
                {
                    debugStage = 3800001;
                    HackingType grantHack = null;
                    foreach ( HackingType hack in relatedEntityTypeData.GetListOfHacks() )
                    {
                        if ( hack == null )
                            continue;
                        debugStage = 3800101;
                        if ( hack.IsAGrantShipStyleHack )
                        {
                            grantHack = hack;
                            break;
                        }
                    }
                    debugStage = 3800201;
                    if ( grantHack != null && grantHack.NumberOfTimesIndividualUnitCanBeHacked > 1 )
                    {
                        debugStage = 3800301;
                        int timesHacked = relatedSquadOrNull == null ? 0 : relatedSquadOrNull.GetNumberOfTimesHacked( grantHack );
                        int remaining = grantHack.NumberOfTimesIndividualUnitCanBeHacked - timesHacked;

                        debugStage = 3800401;
                        buffer.Add( "<color=#f25e1c>可黑入 (剩余 " ).Add( remaining ).Add( "/" ).Add( grantHack.NumberOfTimesIndividualUnitCanBeHacked ).Add( " 次)</color>: " );
                    }
                    else
                        buffer.Add( "<color=#f25e1c>可黑入（一次）</color>: " );

                    debugStage = 3800501;
                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    debugStage = 3800501;
                    if ( relatedSquadOrNull.ShipGrantsList.Count <= 0 )
                        buffer.Add( "由于某种原因，此单位没有可授予的舰船！（这是一个BUG，请附带存档报告。）  " );
                    else
                    {
                        debugStage = 3800601;
                        buffer.Add( "  可授予的舰船编制（选择其一）: " );
                        ShipLineEntry entry = null;
                        for ( int i = 0; i < relatedSquadOrNull.ShipGrantsList.Count; i++ )
                        {
                            debugStage = 3800701;
                            entry = relatedSquadOrNull.ShipGrantsList[i];
                            if ( i > 0 )
                                buffer.Add( ", " );

                            debugStage = 3800801;
                            byte markLevelAddedAt = localFaction.GetGlobalMarkLevelForShipLine( entry.TypeData );
                            GameEntityTypeData.MarkLevelStats markStatsForDisplay = entry.TypeData.MarkStatsFor( markLevelAddedAt );
                            if ( markStatsForDisplay == null )
                                buffer.Add( "<color=#ffa1a1>" );
                            else
                                buffer.StartColor( markStatsForDisplay.MarkLevel.ColorHex );

                            debugStage = 3800901;
                            int numShips = entry.GetNumShipsForHackAndHacker( hackerBeingUsedAgainstUs, hackBeingDoneAgainstUs == null ? grantHack : hackBeingDoneAgainstUs );
                            buffer.Add( entry.TypeData == null ? "null" : entry.TypeData.GetDisplayName() ).Add( "</color>" ).Add( " x" )
                                //Chris says: these are theoretical adds, so this is the base cap and that gets marked up.  This is okay.
                                .Add( entry.TypeData.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( entry.TypeData, numShips, numShips, markStatsForDisplay.MarkLevel ), entry.GetColorForShipLineScore() );

                            debugStage = 3801101;
                            if ( detailLevel == TooltipDetail.Full &&
                                 entry.TypeData.TechUpgradesThatBenefitMe.Count > 0 )
                            {
                                debugStage = 3801201;
                                buffer.StartColor( Color.grey ).Add("<size=60%>");
                                for ( int j = 0; j < entry.TypeData.TechUpgradesThatBenefitMe.Count; j++ )
                                {
                                    debugStage = 3801301;
                                    buffer.Add(" ").Add(entry.TypeData.TechUpgradesThatBenefitMe[j].DisplayName);
                                }
                                buffer.Add("</size>").EndColor();
                            }
                            debugStage = 3801401;
                            if ( entry.TypeData.AIPWhenGrantedByHack > 0 )
                                buffer.Add( " <color=#ff9072>(AIP Cost " ).Add( entry.TypeData.AIPWhenGrantedByHack.ReadableString ).Add( ")</color>" );
                        }
                        buffer.Add( ". " );
                    }
                }
                debugStage = 3850;
                if ( relatedEntityTypeData.GrantsStuffToBeAddedToPlayerFleets && !relatedSquadOrNull.IsFakeEntity )
                {
                    debugStage = 3851;
                    HackingType grantHack = null;
                    foreach ( HackingType hack in relatedEntityTypeData.GetListOfHacks())
                    {
                        debugStage = 3852;
                        if ( hack.IsAGrantShipStyleHack )
                        {
                            grantHack = hack;
                            break;
                        }
                    }
                    debugStage = 3853;
                    if ( grantHack != null && grantHack.NumberOfTimesIndividualUnitCanBeHacked > 1 )
                    {
                        debugStage = 3854;
                        int timesHacked = relatedSquadOrNull == null ? 0 : relatedSquadOrNull.GetNumberOfTimesHacked( grantHack );
                        int remaining = grantHack.NumberOfTimesIndividualUnitCanBeHacked - timesHacked;

                        buffer.Add( "<color=#f25e1c>可黑入 (剩余 " ).Add( remaining ).Add( "/" ).Add( grantHack.NumberOfTimesIndividualUnitCanBeHacked ).Add( " 次)</color>: " );
                    }
                    else
                        buffer.Add( "<color=#f25e1c>可黑入（一次）</color>: " );
                    debugStage = 3855;
                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    if ( relatedSquadOrNull.ShipGrantsList.Count <= 0 )
                        buffer.Add( "由于某种原因，此单位没有可授予的舰船！（这是一个BUG，请附带存档报告。）  " );
                    else
                    {
                        debugStage = 3856;
                        buffer.Add( "  可授予的舰船编制（选择其一）: " );
                        ShipLineEntry entry = null;
                        for ( int i = 0; i < relatedSquadOrNull.ShipGrantsList.Count; i++ )
                        {
                            debugStage = 3857;
                            entry = relatedSquadOrNull.ShipGrantsList[i];

                            GameEntityTypeData entryTypeData = entry.TypeData;
                            if ( entryTypeData == null )
                                continue;
                            byte markLevelAddedAt = localFaction.GetGlobalMarkLevelForShipLine( entryTypeData );
                            GameEntityTypeData.MarkLevelStats markStatsForDisplay = entryTypeData.MarkStatsFor( markLevelAddedAt );
                            if ( markStatsForDisplay == null )
                                continue;

                            if ( i > 0 )
                                buffer.Add( ", " );

                            buffer.StartColor( markStatsForDisplay.MarkLevel.ColorHex );
                            debugStage = 3858;
                            int numShips = entry.GetNumShipsForHackAndHacker( hackerBeingUsedAgainstUs, hackBeingDoneAgainstUs == null ? grantHack : hackBeingDoneAgainstUs );
                            buffer.Add( entryTypeData.GetDisplayName() ).Add("</color>").Add(" x" )
                                //Chris says: these are theoretical adds, so this is the base cap and that gets marked up.  This is okay.
                                .Add( entryTypeData.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( entryTypeData, numShips, numShips, markStatsForDisplay.MarkLevel ), entry.GetColorForShipLineScore() ) ;
                            debugStage = 3859;
                            if ( detailLevel == TooltipDetail.Full &&
                                 entryTypeData.TechUpgradesThatBenefitMe.Count > 0 )
                            {
                                buffer.StartColor( Color.grey ).Add("<size=60%>");
                                for ( int j = 0; j < entryTypeData.TechUpgradesThatBenefitMe.Count; j++ )
                                {
                                    buffer.Add(" ").Add( entryTypeData.TechUpgradesThatBenefitMe[j].DisplayName);
                                }
                                buffer.Add("</size>").EndColor();
                            }

                            if ( entryTypeData.AIPWhenGrantedByHack > 0 )
                                buffer.Add( " <color=#ff9072>(AIP Cost " ).Add( entryTypeData.AIPWhenGrantedByHack.ReadableString ).Add( ")</color>" );
                        }
                        buffer.Add(". ");
                    }
                }
                debugStage = 386;
                if ( relatedEntityTypeData.GetIsEligibleForAnyHack() && !relatedSquadOrNull.IsFakeEntity && localPlayerFactionOrNull != null )
                {
                    List<HackingType> hacksAgainst = relatedEntityTypeData.GetListOfHacks();
                    foreach ( HackingType hack in hacksAgainst )
                    {
                        if ( !hack.GetIsHackValidAgainst( relatedSquadOrNull, false ) )
                            continue;
                        try
                        {
                            hack.Implementation.WriteAnySpecialDisplayCodeForHackedShipTooltip( relatedSquadOrNull, localPlayerFactionOrNull, buffer, (BaseTooltipDetail)detailLevel );
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "Error in WriteAnySpecialDisplayCodeForHackedShipTooltip for '" + 
                                hack.InternalName + "': " + e, Verbosity.ShowAsError );
                        }
                    }
                }
                debugStage = 390;
                if ( relatedEntityTypeData.RegeneratesDyingShipsAtThisHealthCostRatio > FInt.Zero )
                    buffer.Add( "当此星球上的友方单位将要死亡时，此舰船改为减少自身生命值来复活它们。效率为每 <color=#ffdf72>" )
                        .Add( relatedEntityTypeData.RegeneratesDyingShipsAtThisHealthCostRatio.ReadableString ).Add( " health regenerated</color>.  " );

                debugStage = 395;

                //Later ship class tooltip
                if ( !relatedEntityTypeData.ShipClass.IsDefault && !relatedEntityTypeData.ShipClass.WriteDescriptionSoonerInTooltip )
                {
                    FactionType fType = FactionType.Player;
                    if ( entityFaction != null )
                        fType = entityFaction.Type;
                    Window_PrototypeInGameHoverEntityInfoUtils.WriteShipClass_Complete( buffer, relatedEntityTypeData.ShipClass, detailLevel, fType, markStats.Speed );
                }

                if ( detailLevel >= TooltipDetail.Full )
                {
                    if ( relatedSquadOrNull.GetImmuneToCapture() )
                        buffer.Add( "无法被其他阵营俘获。 ", "999999" );
                }

                if ( !relatedEntityTypeData.ShipClass.CanBeFleetSupercharged && !AIWar2GalaxySettingQuickAccess.DisableFleetWideBonuses )
                    buffer.Add( "无法被超级充能。 ", "999999" );

                debugStage = 400;

                if ( relatedEntityTypeData.SpeedMultiplierFirst5SecondsOnPlanet > FInt.One )
                    buffer.Add( "<color=#f25e1c>快速部署</color>: 此单位在通过虫洞或生成后前5秒内拥有 " ).Add( relatedEntityTypeData.SpeedMultiplierFirst5SecondsOnPlanet ).Add( " 倍速度加成  " );

                if ( relatedEntityTypeData.BuildPointsPerDamageDealt > FInt.Zero )
                {
                    buffer.Add( "<color=#f25e1c>冯·诺依曼</color>: 建造额外的 " );
                    if ( relatedEntityTypeData.UnitToMakeWithBuildPoints_TypeData.DisplayName.Equals( relatedEntityTypeData.DisplayName ) )
                    {
                        buffer.Add( "自身副本； " );
                    } else
                    {
                        buffer.Add( relatedEntityTypeData.UnitToMakeWithBuildPoints_TypeData.DisplayName ).Add( " 单位" );
                    }
                    buffer.Add( " 一旦对敌人造成足够伤害，等于其默认舰队编制数量。  " );
                }

                debugStage = 410;

                if ( relatedEntityTypeData.AIReinforcementMultiplier > FInt.One )
                    buffer.Add( "<color=#f25e1c>AI加速器</color>: 将本星球的AI增援力量增强 <color=#ffdf72>" ).Add( 
                        relatedEntityTypeData.AIReinforcementMultiplier.ReadableString ).Add( "倍</color>。  " );

                debugStage = 420;

                if ( relatedEntityTypeData.AddsBlackHoleEffectForEntitiesWithEngine_gxLessThan > 0 )
                    buffer.Add( "<color=#f25e1c>黑洞效应</color>: 引擎功率低于 <color=#ffdf72>" ).Add(
                        relatedEntityTypeData.AddsBlackHoleEffectForEntitiesWithEngine_gxLessThan ).Add( "gx</color> 的敌舰无法离开此星球。致残单位无论引擎功率如何均可离开。 " );
                else if ( relatedEntityTypeData.AddsBlackHoleEffectForAllEntitiesPeriod )
                    buffer.Add( "<color=#f25e1c>超级黑洞效应</color>: 任何舰船都无法离开此星球。  无论友军、敌军、致残单位，均不例外。 " );

                debugStage = 425;

                if ( relatedEntityTypeData.CannotTargetOrAlertAIReinforcementSpots )
                {
                    buffer.Add( "<color=#f25e1c>仇恨隐形</color>: 包含守卫的敌方目标舰船和建筑（主要是守卫哨所）无法探测此舰船，此舰船也无法向那些目标开火。 " );
                    if ( detailLevel >= TooltipDetail.Full )
                        buffer.Add( "一旦那些目标被警觉并释放其守卫进行战斗，此舰船即可攻击那些目标。  " );
                }

                debugStage = 430;

                if ( relatedEntityTypeData.PeriodicSpawn_InitialDelay > 0 )
                {
                    buffer.Add( "<color=#f25e1c>" );
                    if( relatedEntityTypeData.PeriodicallySpawnsUnits )
                    {
                        buffer.Add( "巢穴:</color> ");
                        relatedEntityTypeData.PeriodicSpawn_EntityTypeDrawingBag.Value.WriteToBuffer( buffer, relatedEntityTypeData, false );
                        buffer.Add( " for its " ).Add( EnumNameCache.GetName( relatedEntityTypeData.Periodic_SpawnFactionForUnit ) ).Add( " faction" );
                    } else
                    {
                        if ( relatedEntityTypeData.PeriodicSpawn_CreatesWave && relatedEntityTypeData.PeriodicSpawn_CreatesExoStrike )
                        {
                            buffer.Add( "星系外/突袭引擎:</color> 生成波次和星系外打击" );
                        } else if( relatedEntityTypeData.PeriodicSpawn_CreatesExoStrike )
                        {
                            buffer.Add( "星系外引擎:</color> 生成星系外打击" );
                        } else
                        {
                            buffer.Add( "突袭引擎:</color> 生成波次" );
                        }
                        buffer.Add( " of <color=#ffdf72>" ).Add( relatedEntityTypeData.PeriodicSpawn_WaveOrExoSizeMultiplier.ReadableString ).Add( "倍 </color>正常强度" );
                    }

                    buffer.Add( " <color=#ffdf72>每 " ).Add( relatedEntityTypeData.PeriodicSpawn_DelayBetweenSpawns ).Add( " 秒</color>" );
                    if ( relatedEntityTypeData.PeriodicSpawn_InitialDelay > 0 )
                        buffer.Add( " 在 <color=#ffdf72>" ).Add( relatedEntityTypeData.PeriodicSpawn_InitialDelay ).Add( " 秒</color>后" );
                    else
                        buffer.Add( " 立即" );

                    buffer.Add( " 当" );
                    if ( relatedEntityTypeData.PeriodicSpawn_MinHostileStrengthToTrigger > 0 )
                        buffer.Add( " 至少有 " ).AddStrength_Truncated( relatedEntityTypeData.PeriodicSpawn_MinHostileStrengthToTrigger, false ).Add( " 强度的敌军" );
                    if ( relatedEntityTypeData.PeriodicSpawn_OnlyTriggerAgainstPlayer )
                        buffer.Add( " 玩家" );
                    buffer.Add( " 存在被探测到" );
                    if (relatedEntityTypeData.PeriodicSpawn_OnlyTriggerOnOccupation)
                    {
                        buffer.Add( " 占领星球" );
                    } else
                    {
                        buffer.Add( " 在本星球上" );
                        if( relatedEntityTypeData.PeriodicSpawn_MaxHopsToTrigger > 0)
                        {
                            buffer.Add( " 并且" );
                        }
                    }
                    if(relatedEntityTypeData.PeriodicSpawn_MaxHopsToTrigger > 0)
                    {
                        buffer.Add( " 在 <color=#ffdf72>" ).Add( relatedEntityTypeData.PeriodicSpawn_MaxHopsToTrigger ).Add( "</color> 次跳跃范围内" );
                    }
                    if ( relatedEntityTypeData.PeriodicSpawn_NeverStopOnceTriggered )
                        buffer.Add( ". 一旦首次生成，只要银河系中存在有效敌人，它将不会停止" );
                    buffer.Add( ".  " );
                }

                debugStage = 440;

                if ( relatedEntityTypeData.FreesGuardsWhenLocalForceRatioIsWorseThan > FInt.Zero )
                {
                    debugStage = 450;
                    if ( relatedEntityTypeData.NumberOfHopsOutToFreeGuards > 0 )
                        buffer.Add( "<color=#f25e1c>求援</color>: 本星球 <color=#ffdf72>" ).Add(
                            relatedEntityTypeData.NumberOfHopsOutToFreeGuards ).Add( " 次虫洞跳跃</color>范围内的所有守卫单位将变成威胁（并可能加入猎杀舰队），如果攻击的敌军强度超过本星球AI部队的 <color=#ffdf72> " ).Add(
                            relatedEntityTypeData.FreesGuardsWhenLocalForceRatioIsWorseThan.ReadableString ).Add( "倍</color>。  " );
                    else
                        buffer.Add( "<color=#f25e1c>分散守卫</color>: 本星球上所有守卫单位将变成威胁（并可能加入猎杀舰队），如果本星球AI强度低于敌军的 <color=#ffdf72> " ).Add(
                            relatedEntityTypeData.FreesGuardsWhenLocalForceRatioIsWorseThan.ReadableString ).Add( "倍</color>。  " );
                }
                debugStage = 451;
                if ( relatedEntityTypeData.WatchPlanetsAtXHops > 0 && ( relatedSquadOrNull == null || relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player ) )
                {
                    debugStage = 455;

                    int hopCount = relatedEntityTypeData.WatchPlanetsAtXHops + (relatedMembershipOrNull == null ? 0 : relatedMembershipOrNull.Hacked_ExtraWatchPlanetsAtXHops );
                    buffer.Add( "<color=#f25e1c>侦察</color>: 监视 " ).Add( hopCount, "ffdf72");
                    if ( hopCount > 1 )
                        buffer.Add(" 次跳跃范围内的所有星球。  " );
                    else
                        buffer.Add(" 次跳跃范围内的所有星球。  " );
                }

                debugStage = 460;

                if ( relatedEntityTypeData.PushesEnemyShields )
                    buffer.Add( "<color=#f25e1c>诺里斯效应</color>: 移入敌方时会位移敌方泡状力场发生器。  " );

                debugStage = 465;

                if ( relatedEntityTypeData.DisallowKiting && detailLevel >= TooltipDetail.Full )
                    buffer.Add( "永不被允许风筝。  " );

                debugStage = 470;

                bool shouldShowDefensiveStructureCapMultiple = relatedEntityTypeData.IsCommandStation && (relatedSquadOrNull == null || relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player);
                string addedBy = "TSSes";
                string postExplanation = " 为TSS本身所声明数量的倍数。  不同类型的指挥站、战斗站和堡垒会根据其性质获得增益或减益。  ";
                if ( World.Instance.GetWasWorldStartedOnGameVersionAtLeastThisVersionOrNewer( 2, 739 ) ) //2739_DSSHacks
                {
                    if ( relatedEntityTypeData.HackableForCommandStationsAndBattleStations_TurretCount <= 0  )
                    {
                        addedBy = "ODSSes";
                        postExplanation = " 为ODSS本身所声明数量的倍数。  不同类型的指挥站、战斗站和堡垒会根据其性质获得增益或减益。  ";
                    }
                    if ( !shouldShowDefensiveStructureCapMultiple )
                    {
                        //this is not a player-owned command station.  So... is it a battlestation or a citadel owned by anyone?
                        if ( relatedEntityTypeData.IsBattlestation )
                            shouldShowDefensiveStructureCapMultiple = true;
                    }
                }
                else //pre-DSS
                {
                    addedBy = "GCAs";
                    postExplanation = " 为GCA本身所声明数量的倍数。  不同类型的指挥站会根据其性质获得增益或减益。  ";
                }

                if ( shouldShowDefensiveStructureCapMultiple )
                {
                    string colorForDefensiveCap = "72ffbe"; //very green
                    if ( relatedEntityTypeData.DefensiveStructureCap_Multiplier < FInt.One )
                        colorForDefensiveCap = "ffc872"; //bright orange

                    if ( detailLevel < TooltipDetail.Full )
                    {
                        buffer.StartColor( colorForDefensiveCap ).Add( "防御建筑上限: " );
                        buffer.Add( (relatedEntityTypeData.DefensiveStructureCap_Multiplier * 100 ).GetNearestIntPreferringHigher() );
                        buffer.Add( "%  " ).EndColor();
                    }
                    else
                    {
                        buffer.StartColor( colorForDefensiveCap ).Add( "防御建筑上限: " ).EndColor().Add( "由 " )
                            .Add( addedBy ).Add( " 添加的炮塔和其他防御选项数量为 " ).StartColor( colorForDefensiveCap );
                        buffer.Add( (relatedEntityTypeData.DefensiveStructureCap_Multiplier * 100).GetNearestIntPreferringHigher() );
                        buffer.Add( "%" ).EndColor().Add( postExplanation );
                    }
                }
                if ( !relatedSquadOrNull.IsFakeEntity && relatedSquadOrNull.SpeedLimitFromGroupMove > 0 && detailLevel == TooltipDetail.Full )
                { 
                    SpeedGroup groupOnHost = relatedSquadOrNull.GroupMoveSpeed_HostOnly;
                    if ( groupOnHost != null )
                        buffer.Add( " 在速度组 " ).Add( groupOnHost.SpeedGroupID );
                    else
                        buffer.Add( " 在速度组" );
                    buffer.Add("，编队速度 " ).AddNumberMoreReadable( relatedSquadOrNull.SpeedLimitFromGroupMove ).Add("，计算速度 " ).AddNumberMoreReadable( relatedSquadOrNull.CalculateSpeed( true ) );
                    if ( groupOnHost != null && groupOnHost.OverrideSpeedLimit > 0 )
                        buffer.Add( "。覆写速度 " ).AddNumberMoreReadable( groupOnHost.OverrideSpeedLimit );
                    buffer.Add( "，相比原始速度 " ).AddNumberMoreReadable( relatedMarkLevelData.Speed );
                    buffer.Add("。  ");
                }

                debugStage = 480;

                if ( relatedEntityTypeData.OrbitsGravityWellCenterAtItsCurrentRadius )
                {
                    buffer.Add( "绕重力井轨道 " ).Add( relatedEntityTypeData.DegreesToOrbitPerSecond.ToFloatNonSim().ToString( "0.0' degrees'" ) ).Add( "/秒" );
                    if ( detailLevel < TooltipDetail.Full )
                        buffer.Add( ".  " );
                    else
                        buffer.Add( " at its current range from the center of the gravity well.  " );
                }
                else if ( relatedEntityTypeData.OrbitsParentAtRange > 0 )
                {
                    buffer.Add( "绕祖先单位轨道 " ).Add( relatedEntityTypeData.DegreesToOrbitPerSecond.ToFloatNonSim().ToString( "0.0' degrees'" ) ).Add( "/秒" );
                    if ( detailLevel < TooltipDetail.Full )
                        buffer.Add( ".  " );
                    else
                        buffer.Add( " at a range of " ).AddNumberMoreReadable( relatedEntityTypeData.OrbitsParentAtRange ).Add( ".  " );
                }
                else if ( relatedEntityTypeData.OrbitsFlagshipAtRange > 0 )
                {
                    buffer.Add( "绕旗舰轨道 " ).Add( relatedEntityTypeData.DegreesToOrbitPerSecond.ToFloatNonSim().ToString( "0.0' degrees'" ) ).Add( "/秒" );
                    if ( detailLevel < TooltipDetail.Full )
                        buffer.Add( ".  " );
                    else
                        buffer.Add( " at a range of " ).AddNumberMoreReadable( relatedEntityTypeData.OrbitsFlagshipAtRange ).Add( ".  " );
                }
                
                debugStage = 490;

                int effectiveShieldRadius = (!relatedSquadOrNull.IsFakeEntity ? relatedSquadOrNull.GetEffectiveMaxForcefieldRadius() : markStats.ShieldRadius );
                if ( effectiveShieldRadius > 0 )
                {
                    #region Forcefield
                    if ( detailLevel < TooltipDetail.Full )
                    {
                        buffer.Add( "<color=#f25e1c>" );
                        if ( relatedEntityTypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack > FInt.Zero )
                        {
                            buffer.Add( ( relatedEntityTypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack * 100 ).GetNearestIntPreferringHigher() );
                            buffer.Add( "% 电毒素 " );
                        }
                        if ( relatedEntityTypeData.MyForcefieldDoesNotShrink )
                            buffer.Add( "硬化 " );
                        if ( relatedEntityTypeData.MyForcefieldHasNoPenaltyForEnemiesFiringOutOfIt )
                            buffer.Add( "优质-" );
                        buffer.Add( "力场半径</color> <color=#ffdf72> " );
                        buffer.AddNumberMoreReadable( effectiveShieldRadius );
                        buffer.Add( "</color>z.  " );
                    }
                    else if ( detailLevel >= TooltipDetail.Medium )
                    {
                        buffer.Add( "<color=#f25e1c>" );
                        if ( relatedEntityTypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack > FInt.Zero )
                        {
                            buffer.Add( ( relatedEntityTypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack * 100 ).GetNearestIntPreferringHigher() );
                            buffer.Add( "% 电毒素 " );
                        }
                        if ( relatedEntityTypeData.MyForcefieldDoesNotShrink )
                            buffer.Add( "硬化 " );
                        if ( relatedEntityTypeData.MyForcefieldHasNoPenaltyForEnemiesFiringOutOfIt )
                            buffer.Add( "优质-" );
                        buffer.Add( "力场发生器</color>: 投射半径为 <color=#ffdf72> " );
                        buffer.AddNumberMoreReadable( effectiveShieldRadius );
                        buffer.Add( "</color> 的泡状力场，总强度为 " );
                        buffer.AddNumberMoreReadable( (relatedMembershipOrNull != null && relatedMemFleetOrNull != null && relatedMemFleetOrNull.IsPlayerStyleFleet ? relatedMembershipOrNull.GetMaxShieldPoints_PlayerFleetsOnly() : markStats.BaseShieldPoints) );
                        buffer.Add( ".  力场内的友舰不会受到伤害，敌舰也无法穿透力场。  " );
                        if ( relatedEntityTypeData.MyForcefieldDoesNotShrink )
                            buffer.Add( "硬化力场不会随护盾生命值降低而缩小。  " );
                        if ( relatedEntityTypeData.MyForcefieldHasNoPenaltyForEnemiesFiringOutOfIt )
                            buffer.Add( "优质力场不会导致从力场内向外射击的友军受到伤害惩罚。  " );
                        else
                            buffer.Add( "从力场内向外射击的友军只能造成一半伤害。  " );
                        if ( relatedEntityTypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack > FInt.Zero )
                        {
                            buffer.Add( "此电毒素力场将 " )
                                .Add( (relatedEntityTypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack * 100).GetNearestIntPreferringHigher() )
                                .Add( "% 接受到的伤害反弹给射击它的单位。这被视为 <color=#dfff72>特殊伤害</color>。  " );
                        }
                    }
                    #endregion
                }
                else
                {
                    #region Non-Forcefield
                    if ( relatedEntityTypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack > FInt.Zero )
                    {
                        buffer.Add( "<color=#f25e1c>" );
                        buffer.Add( (relatedEntityTypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack * 100).GetNearestIntPreferringHigher() );
                        buffer.Add( "% 电毒素船体:</color> " );
                        if ( detailLevel >= TooltipDetail.Medium )
                        {
                            if ( relatedEntityTypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack > FInt.Zero )
                            {
                                buffer.Add( "此单位的电毒素船体将 " ).Add(
                                    (relatedEntityTypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack * 100).GetNearestIntPreferringHigher() )
                                    .Add( "% 接受到的伤害反弹给射击它的单位。这被视为 <color=#dfff72>特殊伤害</color>。 " );
                            }
                        }
                        else
                            buffer.Add( "反弹受到的伤害。  " );
                    }
                    #endregion
                }

                debugStage = 500;

                #region AlliedAttackMultiplier
                if ( markStats.AlliedAttackMultiplier != FInt.One )
                {
                    if ( detailLevel < TooltipDetail.Full )
                    {
                        buffer.Add( "<color=#f25e1c>PLANETARY ATTACK AMPLIFIER</color>: <color=#ffdf72> " );
                        buffer.Add( markStats.AlliedAttackMultiplier.ReadableString );
                        buffer.Add( "x</color>.  " );
                    }
                    else
                    {
                        buffer.Add( "<color=#f25e1c>行星攻击增幅器</color>: 本星球所有友舰获得 <color=#ffdf72> " );
                        buffer.Add( markStats.AlliedAttackMultiplier.ReadableString );
                        buffer.Add( "倍</color> 攻击加成。  " );
                    }
                }
                #endregion

                debugStage = 505;

                #region HostileAttackMultiplier
                if ( markStats.HostileAttackMultiplier != FInt.One )
                {
                    if ( detailLevel < TooltipDetail.Full )
                    {
                        buffer.Add( "<color=#f25e1c>PLANETARY ATTACK INHIBITOR</color>: <color=#ffdf72> " );
                        buffer.Add( markStats.HostileAttackMultiplier.ReadableString );
                        buffer.Add( "x</color>.  " );
                    }
                    else
                    {
                        buffer.Add( "<color=#f25e1c>行星攻击抑制器</color>: 本星球所有敌舰受到 <color=#ffdf72> " );
                        buffer.Add( markStats.HostileAttackMultiplier.ReadableString );
                        buffer.Add( "倍</color> 攻击削弱。  " );
                    }
                }
                #endregion

                debugStage = 510;

                #region AlliedSpeedMultiplier
                if ( markStats.AlliedSpeedMultiplier != FInt.One || markStats.AlliedSpeedFlatBonus != 0 )
                {
                    if ( detailLevel < TooltipDetail.Full )
                    {
                        buffer.Add( "<color=#f25e1c>PLANETARY SPEED BOOSTER</color>: <color=#ffdf72> " );
                        if ( markStats.AlliedSpeedMultiplier != FInt.One )
                        {
                            buffer.Add( markStats.AlliedSpeedMultiplier.ReadableString ).Add("x");
                            if ( markStats.AlliedSpeedFlatBonus != 0 )
                                buffer.Add( ", " );
                        }
                        if ( markStats.AlliedSpeedFlatBonus != 0 )
                            buffer.Add("+ ").Add( markStats.AlliedSpeedFlatBonus );
                        buffer.Add( "</color>.  " );
                    }
                    else
                    {
                        buffer.Add( "<color=#f25e1c>行星速度增幅器</color>: 本星球所有友舰获得 <color=#ffdf72> " );
                        if ( markStats.AlliedSpeedMultiplier != FInt.One )
                        {
                            buffer.Add( markStats.AlliedSpeedMultiplier.ReadableString ).Add( "x" );
                            if ( markStats.AlliedSpeedFlatBonus != 0 )
                                buffer.Add( ", " );
                        }
                        if ( markStats.AlliedSpeedFlatBonus != 0 )
                            buffer.Add( "+ " ).Add( markStats.AlliedSpeedFlatBonus );
                        buffer.Add( "</color> 速度加成。  " );
                    }
                }
                #endregion

                debugStage = 515;

                #region HostileSpeedMultiplier
                if ( markStats.HostileSpeedMultiplier != FInt.One || markStats.HostileSpeedFlatBonus != 0 )
                {
                    if ( detailLevel < TooltipDetail.Full )
                    {
                        buffer.Add( "<color=#f25e1c>PLANETARY SPEED INHIBITOR</color>: <color=#ffdf72> " );
                        if ( markStats.HostileSpeedMultiplier != FInt.One )
                        {
                            buffer.Add( markStats.HostileSpeedMultiplier.ReadableString ).Add( "x" );
                            if ( markStats.HostileSpeedFlatBonus != 0 )
                                buffer.Add( ", " );
                        }
                        if ( markStats.HostileSpeedFlatBonus != 0 )
                            buffer.Add( "+ " ).Add( markStats.HostileSpeedFlatBonus );
                        buffer.Add( "</color>.  " );
                    }
                    else
                    {
                        buffer.Add( "<color=#f25e1c>行星速度抑制器</color>: 本星球所有敌舰受到 <color=#ffdf72> " );
                        if ( markStats.HostileSpeedMultiplier != FInt.One )
                        {
                            buffer.Add( markStats.HostileSpeedMultiplier.ReadableString ).Add( "x" );
                            if ( markStats.HostileSpeedFlatBonus != 0 )
                                buffer.Add( ", " );
                        }
                        if ( markStats.HostileSpeedFlatBonus != 0 )
                            buffer.Add( "+ " ).Add( markStats.HostileSpeedFlatBonus );
                        buffer.Add( "</color> 速度削弱。  " );
                    }
                }
                #endregion

                debugStage = 520;

                if ( tachyonSystem >= 0 )
                    _WriteSystemInfo( buffer, tachyonSystem, relatedSquadOrNull, relatedMembershipOrNull, entityFaction, thisPlanetOrNull, relatedEntityTypeData, effectiveMarkLevel, false, false, detailLevel,
                        isForMultipleUnits, showWeaponActivityDetails );

                debugStage = 530;

                #region tractorSystem
                if ( tractorSystem >= 0 )
                    _WriteSystemInfo( buffer, tractorSystem, relatedSquadOrNull, relatedMembershipOrNull, entityFaction, thisPlanetOrNull, relatedEntityTypeData, effectiveMarkLevel, false, false, detailLevel,
                        isForMultipleUnits, showWeaponActivityDetails );
                #endregion

                debugStage = 540;

                #region gravitySystem
                if ( gravitySystem >= 0 )
                    _WriteSystemInfo( buffer, gravitySystem, relatedSquadOrNull, relatedMembershipOrNull, entityFaction, thisPlanetOrNull, relatedEntityTypeData, effectiveMarkLevel, false, false, detailLevel,
                        isForMultipleUnits, showWeaponActivityDetails );
                #endregion

                debugStage = 545;

                #region attractantSystem
                if ( attractantSystem >= 0 )
                    _WriteSystemInfo( buffer, attractantSystem, relatedSquadOrNull, relatedMembershipOrNull, entityFaction, thisPlanetOrNull, relatedEntityTypeData, effectiveMarkLevel, false, false, detailLevel,
                        isForMultipleUnits, showWeaponActivityDetails );
                #endregion

                debugStage = 550;

                #region cloakSystem
                /*if ( relatedEntityTypeData != null )
                {
                    int cloakingPoints = relatedSquadOrNull.GetCurrentCloakingPoints();
                    if ( cloakingPoints > 0 || cloakSystem >= 0 )
                    {
                        buffer.Add( "Current Cloaking Points: " ).StartColor( cloakColor ).AddNumberMoreReadable( cloakingPoints ).EndColor().Add( "  " );
                    }
                }*/

                if ( cloakSystem >= 0 )
                    _WriteSystemInfo( buffer, cloakSystem, relatedSquadOrNull, relatedMembershipOrNull, entityFaction, thisPlanetOrNull, relatedEntityTypeData, effectiveMarkLevel, false, false, detailLevel,
                        isForMultipleUnits, showWeaponActivityDetails );
                #endregion

                debugStage = 555;

                #region Attrition
                if ( relatedMarkLevelData != null && relatedMarkLevelData.AttritionDamagePreFleetModifiers > 0 )
                {
                    debugStage = 55501;
                    int attritionDamage = relatedMarkLevelData.AttritionDamagePreFleetModifiers;
                    if ( !relatedSquadOrNull.IsFakeEntity )
                        attritionDamage = relatedSquadOrNull.GetAttritionDamage();
                    debugStage = 55502;
                    if ( detailLevel < TooltipDetail.Full )
                        buffer.Add( "<color=#f25e1c>消耗器:</color>: 每秒 " ).Add( attritionDamage, "a1ffa1" ).Add( "/s " );
                    else
                    {
                        debugStage = 55503;
                        buffer.Add( "<color=#f25e1c>消耗器:</color>: 对移动中的敌方单位每秒造成 " ).Add( attritionDamage, "a1ffa1" )
                            .Add( " 伤害。这被视为 <color=#dfff72>特殊伤害</color>。 " );
                        if (relatedMarkLevelData.MaxAttritionDamagePreFleetModifiers > 0)
                        {
                            debugStage = 55504;
                            int maxAttritionDamage;
                            if(relatedSquadOrNull == null)
                                maxAttritionDamage = relatedMarkLevelData.MaxAttritionDamagePreFleetModifiers;
                            else
                                maxAttritionDamage = relatedSquadOrNull.GetMaxAttritionDamageOrZeroForUnlimited();
                            buffer.Add( " 最高每秒 " ).AddNumberMoreReadable( maxAttritionDamage ).Add( "。 " );
                        } else
                        {
                            buffer.Add( ". " );
                        }
                    }
                }
                #endregion

                debugStage = 560;

                #region Hardened
                if ( relatedEntityTypeData.DamageTakenCannotBeMoreThanThisPercentageOfMaxHealth > FInt.Zero &&
                    relatedEntityTypeData.DamageTakenCannotBeMoreThanThisPercentageOfMaxHealth < FInt.One )
                {
                    int percentageAsInteger = ( relatedEntityTypeData.DamageTakenCannotBeMoreThanThisPercentageOfMaxHealth * 100 ).GetNearestIntPreferringHigher();
                    if ( detailLevel < TooltipDetail.Full )
                        buffer.Add( "<color=#f25e1c>硬化:</color>: 单次承受伤害从不超过最大船体生命值的 " ).Add( percentageAsInteger ).Add( "%。 " );
                    else
                        buffer.Add( "<color=#f25e1c>硬化:</color>: 来自单一来源的任何伤害（爆炸、射击、消耗等），如果超过此单位最大船体生命值的 " )
                            .Add( percentageAsInteger ).Add( "%，将被降低至该值。  可抵御大型火炮、离子炮、质量驱动器等。 " );
                }
                #endregion

                debugStage = 562;

                #region CreatesCeasefireOnPlanet
                if ( relatedEntityTypeData.CreatesCeasefireOnPlanet )
                {
                    buffer.Add( "停火: 在与单位同处一个星球时阻止所有单位开火。 " );
                }
                #endregion

                #region BlocksCeasefireOnPlanet
                if ( relatedEntityTypeData.BlocksCeasefireOnPlanet )
                {
                    buffer.Add( "停火破坏者: 如果此单位在某个星球上，则不可能停火。 " );
                }
                #endregion

                debugStage = 564;

                #region Harmonic
                if ( relatedMarkLevelData.AmountAddedToDamagePerShipOfThisTypeOnPlanet > 0  )
                {
                    if ( detailLevel < TooltipDetail.Full )
                    {
                        buffer.Add( "<color=#f25e1c>和谐</color>: <color=#ffdf72>" );
                        buffer.Add( relatedMarkLevelData.AmountAddedToDamagePerShipOfThisTypeOnPlanet );
                        buffer.Add( "</color> 额外伤害，每存在一个此类型单位。  " );
                    }
                    else if ( relatedEntityTypeData.MaxAmountAddedToDamagePerShipOfThisTypeOnPlanet > 0 )
                    {
                        buffer.Add( "<color=#f25e1c>和谐</color>: 此舰船对本星球上每个此类型单位造成 <color=#ffdf72>" );
                        buffer.Add( relatedMarkLevelData.AmountAddedToDamagePerShipOfThisTypeOnPlanet );
                        buffer.Add( "</color> 额外伤害，每发子弹最高 <color=#ffdf72>" );
                        buffer.Add( relatedEntityTypeData.MaxAmountAddedToDamagePerShipOfThisTypeOnPlanet );
                        buffer.Add( "</color> 额外伤害。 " );
                    }
                    else
                    {
                        buffer.Add( "<color=#f25e1c>和谐</color>: 此舰船对本星球上每个此类型单位造成 <color=#ffdf72>" );
                        buffer.Add( relatedMarkLevelData.AmountAddedToDamagePerShipOfThisTypeOnPlanet );
                        buffer.Add( "</color> 额外伤害。  " );
                    }
                }
                #endregion

                debugStage = 570;

                //any modular stat boosts
                if ( relatedEntityTypeData.IsModular )
                {
                    for ( int i = 0; i < relatedEntityTypeData.SystemTypes.Count; i++ )
                    {
                        debugStage = 571;
                        EntitySystemTypeData systemData = relatedEntityTypeData.SystemTypes[i];
                        if ( systemData.MaxMarkLevelToFunction < effectiveMarkLevel || systemData.MinMarkLevelToFunction > effectiveMarkLevel || systemData.SystemIsHiddenForUI )
                            continue;
                        if ( systemData.CareAboutStateOfMatterToBeEnabled && systemData.IsInvisibleInTooltipsIfNotAMatchByStateOfMatter )
                        {
                            if ( !relatedSquadOrNull.IsFakeEntity && relatedSquadOrNull.CurrentStateOfMatter != systemData.MustBeThisStateOfMatterToBeEnabled )
                                continue; //skip completely, since this will be invisible AND disabled
                        }
                        if ( systemData.IsModule && !systemData.IsModuleOn( relatedMembershipOrNull ) )
                            continue;

                        if (systemData.ModuleStatAdjuster == ModularStatAdjustment.None)
                            continue;
                        
                        _WriteSystemInfo( buffer, i, relatedSquadOrNull, relatedMembershipOrNull, entityFaction, thisPlanetOrNull, systemData, effectiveMarkLevel, false, true, detailLevel, isForMultipleUnits, false,
                            ref hasSystemWithBonusFromAttackingUnderForcefields );
                    }
                }

                //Player Fleet bonus items
                //--------------------------------------
                debugStage = 700;
                //if ( relatedMembershipOrNull != null && relatedMemFleetOrNull != null )
                //{
                //    switch ( relatedMemFleetOrNull.Category )
                //    {
                //        case FleetCategory.PlayerMobile:
                //        case FleetCategory.PlayerPlanetaryCommand:
                //        case FleetCategory.PlayerBattlestation:
                //            {
                //                debugStage = 710;

                //            }
                //            break;
                //    }
                //}
                if ( relatedEntityTypeData.HackingEffectMultiplier != FInt.One )
                    buffer.Add( "<color=#f25e1c>黑客加成</color>: 此单位进行的所有黑客入侵的AI反应乘以 <color=#ffdf72>" ).Add(relatedEntityTypeData.HackingEffectMultiplier.ReadableString).Add("倍</color>。  " );

                if ( !AIWar2GalaxySettingQuickAccess.DisableFleetWideBonuses )
                {
                    int superchargeBonusLimiter = 0;
                    bool isSuperchargeLimited = false;
                    string fleetBonusPrefix = "<color=#7cf21c>舰队加成:</color> <color=#9ce066>";
                    if ( relatedEntityTypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess > 0 && relatedMembershipOrNull != null && relatedMemFleetOrNull != null )
                    {
                        superchargeBonusLimiter = relatedMemFleetOrNull.GetCountOfShipLinesForSuperchargePurposes( null );
                        if ( superchargeBonusLimiter > relatedEntityTypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess )
                        {
                            isSuperchargeLimited = true;
                            fleetBonusPrefix = "<color=#f2ae1c>舰队加成关闭:</color> <color=#e0c266>";
                        }
                        else
                        {
                            fleetBonusPrefix = "<color=#7cf21c>舰队加成开启:</color> <color=#9ce066>";
                        }
                    }

                    if ( relatedEntityTypeData.SuperchargesMinSpeedOfRestOfPlayerFleet )
                    {
                        buffer.Add( fleetBonusPrefix ).Add( "使舰队中所有非旗舰成员的速度至少与自身相同。</color>  " );
                    }
                    if ( relatedEntityTypeData.SuperchargesHullOfRestOfPlayerFleet > FInt.One )
                    {
                        buffer.Add( fleetBonusPrefix ).Add( "为舰队中所有成员（包括旗舰）的船体强度提供 " ).AddFixedDecimal( relatedEntityTypeData.SuperchargesHullOfRestOfPlayerFleet.ToDouble(), 2 ).Add( " 倍加成。</color>  " );
                    }
                    if ( relatedEntityTypeData.SuperchargesShieldsOfRestOfPlayerFleet > FInt.One )
                    {
                        buffer.Add( fleetBonusPrefix ).Add( "为舰队中所有成员（包括旗舰）的护盾强度提供 " ).AddFixedDecimal( relatedEntityTypeData.SuperchargesShieldsOfRestOfPlayerFleet.ToDouble(), 2 ).Add( " 倍加成。</color>  " );
                    }
                    if ( relatedEntityTypeData.SuperchargesAttackPowerOfRestOfPlayerFleet > FInt.One )
                    {
                        buffer.Add( fleetBonusPrefix ).Add( "为舰队中所有成员（包括旗舰）的攻击强度提供 " ).AddFixedDecimal( relatedEntityTypeData.SuperchargesAttackPowerOfRestOfPlayerFleet.ToDouble(), 2 ).Add( " 倍加成。</color>  " );
                    }
                    if ( relatedEntityTypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess > 0 )
                    {
                        if ( isSuperchargeLimited )
                            buffer.Add( "<color=#f2ae1c>舰队加成限制已超:</color> <color=#e0c266>舰队范围加成仅在拥有 " ).Add(
                                relatedEntityTypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess )
                                .Add( " 条或更少非旗舰、非精英舰船编制的舰队中生效，但此舰队有 " ).Add( superchargeBonusLimiter ).Add( " 条。</color>  " );
                        else if ( relatedMembershipOrNull == null || detailLevel >= TooltipDetail.Full )
                        {
                            buffer.Add( "<color=#7cf21c>舰队加成限制:</color> <color=#9ce066>舰队范围加成仅在拥有 " ).Add(
                                    relatedEntityTypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess );

                            if ( relatedMembershipOrNull != null )
                                buffer.Add( " 条或更少非旗舰、非精英舰船编制的舰队中生效，此舰队仅有 " ).Add( superchargeBonusLimiter ).Add( " 条。</color>  " );
                            else
                                buffer.Add( " 条或更少非旗舰、非精英舰船编制的舰队中生效。</color>  " );
                        }
                    }
                    if ( relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet && detailLevel >= TooltipDetail.Full )
                    {
                        buffer.Add( "<color=#f2ae1c>舰队加成不适用:</color> <color=#e0c266>无论何种情况，舰队范围加成对此特定单位无帮助。</color>  " );
                    }
                }
                else
                {
                    bool shouldDrawFleetWideBonusNote = false;
                    if ( relatedEntityTypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess > 0 && relatedMembershipOrNull != null && relatedMemFleetOrNull != null )
                        shouldDrawFleetWideBonusNote = true;
                    else if ( relatedEntityTypeData.SuperchargesMinSpeedOfRestOfPlayerFleet )
                        shouldDrawFleetWideBonusNote = true;
                    else if ( relatedEntityTypeData.SuperchargesHullOfRestOfPlayerFleet > FInt.One )
                        shouldDrawFleetWideBonusNote = true;
                    else if ( relatedEntityTypeData.SuperchargesShieldsOfRestOfPlayerFleet > FInt.One )
                        shouldDrawFleetWideBonusNote = true;
                    else if ( relatedEntityTypeData.SuperchargesAttackPowerOfRestOfPlayerFleet > FInt.One )
                        shouldDrawFleetWideBonusNote = true;
                    else if ( relatedEntityTypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess > 0 )
                        shouldDrawFleetWideBonusNote = true;

                    if ( shouldDrawFleetWideBonusNote )
                        buffer.Add( "<color=#f2ae1c>舰队范围加成已禁用:</color> <color=#e0c266>由于星系选项（可能与您的战役类型有关），不允许舰队范围加成。</color>  " );
                }
                // && !this.TypeData.CannotBeSuperchargedWhenInSuperchargedFleet

                //Pretty late note items
                //--------------------------------------

                //int cloakingPoints = relatedEntity.GetCurrentCloakingPoints();
                //if ( cloakingPoints > 0 )
                //    buffer.StartColor( cloakColor ).Add( "Cloaking Points: " ).Add( cloakingPoints ).EndColor();

                debugStage = 900;

                if ( relatedEntityTypeData.IncomingDamageModifiers_FullList.Count > 0 )
                {
                    for ( int k = 0; k < relatedEntityTypeData.IncomingDamageModifiers_FullList.Count; k++ )
                    {
                        Window_PrototypeInGameHoverEntityInfo.WriteDamageModifierData( buffer, markStats.MarkLevel.Ordinal,
                            relatedEntityTypeData.IncomingDamageModifiers_FullList[k], null );
                    }
                }

                debugStage = 950;

                if ( !relatedSquadOrNull.IsFakeEntity && relatedSquadOrNull.CountOfEntitiesProvidingExternalInvulnerability > 0 &&
                    relatedSquadOrNull.CountOfEntitiesProvidingExternalInvulnerability >= relatedSquadOrNull.TypeData.ExternalInvulnerabilityUnitRequiredCount )
                {
                    string protectorStringCurrent = GetProtectorString( relatedSquadOrNull.TypeData.ExternalInvulnerabilityUnitType,
                        relatedSquadOrNull.TypeData.ExternalInvulnerabilityUnitTag, relatedSquadOrNull.CountOfEntitiesProvidingExternalInvulnerability );

                    buffer.Add( "<color=#f25e1c>无敌</color>: 此单位的无敌由 " )
                        .Add( relatedSquadOrNull.CountOfEntitiesProvidingExternalInvulnerability, "cfd988" ).Add( " " ).Add( protectorStringCurrent );
                    if ( relatedSquadOrNull.TypeData.InvulnerabilityRegion == ExternalInvulnerabilityRegion.ThisPlanet )
                    {
                        buffer.Add( " 在本星球上。  " );
                        //RelatedEntityTypeData.ExternalInvulnerabilityUnitRequiredCount
                    }
                    else
                        buffer.Add( " 在银河系中。  " );
                    if ( relatedSquadOrNull.TypeData.ExternalInvulnerabilityUnitRequiredCount > 0 )
                    {
                        string protectorStringMax = GetProtectorString( relatedSquadOrNull.TypeData.ExternalInvulnerabilityUnitType,
                            relatedSquadOrNull.TypeData.ExternalInvulnerabilityUnitTag, relatedSquadOrNull.TypeData.ExternalInvulnerabilityUnitRequiredCount );

                        buffer.Add( "获得无敌需要至少 " ).Add( relatedSquadOrNull.TypeData.ExternalInvulnerabilityUnitRequiredCount, "d9bd88" )
                            .Add( " " ).Add( protectorStringMax ).Add( ".  " );
                    }
                }
                else if ( !relatedSquadOrNull.IsFakeEntity && relatedSquadOrNull.TypeData.ExternalInvulnerabilityUnitRequiredCount > 0 )
                {
                    string protectorStringMax = GetProtectorString( relatedSquadOrNull.TypeData.ExternalInvulnerabilityUnitType,
                            relatedSquadOrNull.TypeData.ExternalInvulnerabilityUnitTag, relatedSquadOrNull.TypeData.ExternalInvulnerabilityUnitRequiredCount );

                    buffer.Add( "<color=#f25e1c>脆弱</color>: 获得无敌需要至少 " ).Add( relatedSquadOrNull.TypeData.ExternalInvulnerabilityUnitRequiredCount, "d9bd88" )
                        .Add( " " ).Add( protectorStringMax ).Add( "。它仅有 " ).Add( relatedSquadOrNull.CountOfEntitiesProvidingExternalInvulnerability, "cfd988" ).Add( "。  " );
                }

                debugStage = 1001;

                if ( !EntityTypeDrawingBag.IsNullOrInvalid( relatedEntityTypeData.SpawnOnDeath_EntityTypeDrawingBag.Value ) )
                {
                    buffer.Add( "<color=#f25e1c>死亡时</color>: ");
                    relatedEntityTypeData.SpawnOnDeath_EntityTypeDrawingBag.Value.WriteToBuffer( buffer, relatedEntityTypeData, false );
                    buffer.Add( ".  " );
                }

                // Puffin Note Hydra Regeneration
                if ( relatedEntityTypeData.SecondsToFullyRegenerateHull > FInt.Zero )
                {
                    if ( !relatedSquadOrNull.IsFakeEntity )
                    {
                        buffer.Add( "<color=#f25e1c>再生</color>: 在 <color=#ffdf72>" ).Add( relatedEntityTypeData.SecondsToFullyRegenerateHull )
                        .Add( "</color> 秒内恢复船体，前提是不受攻击。 " );
                    }
                }

                debugStage = 1010;

                if ( relatedEntityTypeData.SpecialType == SpecialEntityType.AIKingCommandStation || relatedEntityTypeData.SpecialType == SpecialEntityType.AIKingMobile )
                {
                    buffer.Add( "AI 进程 (AIP) 将<color=#ffdf72>上升 " ).Add( relatedEntityTypeData.AIPOnDeath ).Add( "</color> 如果此单位死亡。  " ); //this is the normal case
                } else if ( (relatedEntityTypeData.SpecialType == SpecialEntityType.AICommandStationReconquest && localPlayerPlanetFactionOrNull != null && localPlayerPlanetFactionOrNull.AIPLeftFromCommandStation > 0) )
                {
                    debugStage = 1011;
                    //special case: reconquest command station that you haven't paid the AIP price for yet (ie some minor faction non-allied to you killed the first command station, so you still get charged)
                    buffer.Add( "由于你尚未为此星球支付 AI 进程 (AIP) 代价，AIP 将<color=#ffdf72>上升 " ).Add( localPlayerPlanetFactionOrNull.AIPLeftFromCommandStation ).Add( "</color> 如果此单位死亡。  " ); //this is the normal case
                }
                else if ( relatedEntityTypeData.AIPOnDeath > 0 )
                {
                    debugStage = 1012;
                    bool wasPriceAlreadyPaid = false;
                    bool noPriceToPay = false;
                    if ( !relatedSquadOrNull.IsFakeEntity )
                    {
                        debugStage = 1013;
                        if ( relatedEntityTypeData.AIPOnDeathOnlyWhenOwnedByAI &&
                             relatedSquadOrNull.PlanetFaction.Faction.Type != FactionType.AI )
                            noPriceToPay = true;
                        Faction factionToUse = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                        if ( factionToUse != null )
                        {
                            Planet relatedPlanet = relatedSquadOrNull == null ? null : relatedSquadOrNull.Planet;
                            PlanetFaction localPlanetFaction = relatedPlanet == null ? null : relatedPlanet.GetPlanetFactionForFaction( factionToUse );
                            if ( localPlanetFaction != null && relatedSquadOrNull.GetMatches_SemiSlow( EntityRollupType.WarpEntryPoints ) && localPlanetFaction.AIPLeftFromWarpGate == 0 )
                                wasPriceAlreadyPaid = true; //the AIP price has already been paid for destroying this structure, so AIP won't increase
                        }
                    }

                    if ( !wasPriceAlreadyPaid && !noPriceToPay )
                    buffer.Add( "AI 进程 (AIP) 将<color=#ffdf72>上升 " ).Add( relatedEntityTypeData.AIPOnDeath ).Add( "</color> 如果此单位死亡。  " ); //this is the normal case
                }
                else if ( relatedEntityTypeData.AIPOnDeath < 0 )
                    buffer.Add( "AI 进程 (AIP) 将<color=#ffdf72>减少 " ).Add( -relatedEntityTypeData.AIPOnDeath ).Add( "</color> 如果此单位死亡。  " );
                debugStage = 1015;

                relatedEntityTypeData.ForAnyDataExtensions_AddToTooltip_GainsSection_ForEntity( buffer, relatedSquadOrNull, relatedMembershipOrNull, relatedEntityTypeData, detailLevel );
                string playerClarification = " ";
                if ( FactionUtilityMethods.Instance.AnyNecromancerFactions() )
                    playerClarification = " human empire ";
                if ( relatedEntityTypeData.MetalToGrantOnDeath > 0 && showMetalOther )
                {
                    buffer.Add( "如果").Add( playerClarification).Add("玩家击杀此单位，获得<color=#ffdf72>" ).Add ( relatedEntityTypeData.MetalToGrantOnDeath ).Add(" 金属。</color>");
                }
                if ( relatedEntityTypeData.ScienceToGrantOnDeath > 0 && relatedEntityTypeData.HackingToGrantOnDeath > 0 )
                {
                    buffer.Add( "如果").Add( playerClarification).Add("玩家击杀此单位，获得<color=#ffdf72>" ).Add ( relatedEntityTypeData.ScienceToGrantOnDeath ).Add(" 科学</color> 和 <color=#ffdf72>").Add(relatedEntityTypeData.HackingToGrantOnDeath).Add(" 黑客点数。</color>");
                }
                else if(relatedEntityTypeData.ScienceToGrantOnDeath > 0 )
                {
                    buffer.Add( "如果").Add( playerClarification ).Add("玩家击杀此单位，获得<color=#ffdf72>" ).Add ( relatedEntityTypeData.ScienceToGrantOnDeath ).Add(" 科学。</color>");
                }
                else if(relatedEntityTypeData.HackingToGrantOnDeath > 0 )
                {
                    buffer.Add( "如果").Add( playerClarification ).Add("玩家击杀此单位，获得<color=#ffdf72>" ).Add ( relatedEntityTypeData.HackingToGrantOnDeath ).Add(" 黑客点数。</color>");
                }
                debugStage = 1020;

                if ( relatedEntityTypeData.AIPOnDeathWhenNoneLeft > 0 )
                    buffer.Add( "AI 进程 (AIP) 将<color=#ffdf72>上升 " ).Add( relatedEntityTypeData.AIPOnDeathWhenNoneLeft ).Add( "</color> 如果所有剩余的 <color=#ffdf72>" )
                        .Add( BaseScenario.GetCountOfMatchingAIPOnDeathWhenNoneLeft( relatedEntityTypeData ) ).Add( "</color> 死亡。  " );
                else if ( relatedEntityTypeData.AIPOnDeathWhenNoneLeft < 0 )
                    buffer.Add( "AI 进程 (AIP) 将<color=#ffdf72>减少 " ).Add( -relatedEntityTypeData.AIPOnDeathWhenNoneLeft ).Add( "</color> 如果所有剩余的 <color=#ffdf72>" )
                        .Add( BaseScenario.GetCountOfMatchingAIPOnDeathWhenNoneLeft( relatedEntityTypeData ) ).Add( "</color> 死亡。  " );

                debugStage = 1030;

                if ( DetailFlags.HasFlag( ShipExtraDetailFlags.AIPCostOnGrant ) || DetailFlags.HasFlag( ShipExtraDetailFlags.AnyGrantHackInfo ) )
                {
                    if ( relatedEntityTypeData.AIPWhenGrantedByHack > 0 )
                        buffer.Add( "\n<color=#ff9072>如果通过黑客入侵获取此单位，AI 进程 (AIP) 将增加 " ).Add( relatedEntityTypeData.AIPWhenGrantedByHack.ReadableString ).Add( "。</color>\n" );
                }

                debugStage = 1500;

                #region Tech Upgrades!

                if ( detailLevel >= TooltipDetail.Medium )
                {
                    Faction factionToUse = null;
                    if ( !relatedSquadOrNull.IsFakeEntity )
                        factionToUse = relatedSquadOrNull.GetFactionOrNull_Safe();

                    if ( factionToUse == null )
                        factionToUse = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    
                    // only show this on ships owned/ownable by the player
                    if (factionToUse == null || 
                        factionToUse.Type == FactionType.Player || 
                        factionToUse.Type == FactionType.NaturalObject)
                    {
                        if ( markStats.MarkLevel.Ordinal <= 0 )
                        {
                            // This is redundant. If you don't see the techs that upgrade it, it isn't upgradable.
                            //buffer.Add( "Cannot be upgraded.  " );
                        }
                        else
                        {
                            buffer.NewLineIfNeeded();
                            
                            if ( relatedEntityTypeData.StartingMarkLevel.Ordinal > 1 )
                                buffer.Add( "起始等级 " ).StartColor( relatedEntityTypeData.StartingMarkLevel.ColorHex )
                                    .Add( "等级 " ).Add( relatedEntityTypeData.StartingMarkLevel.Ordinal ).EndColor().Add( "。  " );

                            if ( relatedEntityTypeData.TechUpgradesThatBenefitMe == null || relatedEntityTypeData.TechUpgradesThatBenefitMe.Count == 0 )
                            {
                                // This is redundant. If you don't see the techs that upgrade it, it isn't upgradable.
                                //buffer.Add( "Not upgraded by any techs.  " );
                            }
                            else
                            {
                                debugStage = 1510;
                                bool alsoShowShipLineCountWithSameTech = false;
                                if ( IsFromSidebarType == FromSidebarType.Sidebar_MultipleUnits )
                                    alsoShowShipLineCountWithSameTech = true;
                                
                                if ( relatedEntityTypeData.TechUpgradesThatBenefitMe.Count == 1 )
                                {
                                    debugStage = 1520;
                                    TechUpgrade upgrade = relatedEntityTypeData.TechUpgradesThatBenefitMe[0];
                                    //this is for Unused or UpgradeMeFromFleetOnly
                                    if ( !upgrade.IsVisibleOnMenus )
                                    {
                                        debugStage = 1530;
                                        WriteTechThatBenefits( buffer, upgrade, factionToUse, alsoShowShipLineCountWithSameTech, ref debugStage );
                                        buffer.Add( "  " );
                                    } 
                                    else
                                    {
                                        debugStage = 1540;
                                        if ( detailLevel == TooltipDetail.Full )
                                            buffer.Add( "科技升级：" );
                                        else
                                            buffer.Add( "科技：" );
                                        
                                        WriteTechThatBenefits( buffer, upgrade, factionToUse, alsoShowShipLineCountWithSameTech, ref debugStage );
                                        buffer.Add( "  " );
                                    }
                                } 
                                else
                                {
                                    debugStage = 1550;

                                    if ( detailLevel == TooltipDetail.Full )
                                        buffer.Add( "科技升级：" );
                                    else
                                        buffer.Add( "科技：" );
                                    
                                    for ( int i = 0; i < relatedEntityTypeData.TechUpgradesThatBenefitMe.Count; i++ )
                                    {
                                        if ( i > 0 )
                                            buffer.Add( ", " );
                                        WriteTechThatBenefits( buffer, relatedEntityTypeData.TechUpgradesThatBenefitMe[i], factionToUse, alsoShowShipLineCountWithSameTech, ref debugStage );
                                    }
                                    buffer.Add( "  " );
                                }
                            }
                        }
                    }
                }
                #endregion

                //Very late note items
                //--------------------------------------

                debugStage = 2000;

                //only show this for non-player ships, or ships that are not yet fully claimed
                if ( isToBeClaimed )
                {
                    debugStage = 2010;
                    if ( relatedEntityTypeData.MarkStatsFor( effectiveMarkLevel ).MetalCostToClaim > 0 )
                    {
                        debugStage = 2020;
                        if ( relatedEntityTypeData.AIPToClaim > 0 )
                        {
                            if ( detailLevel >= TooltipDetail.Medium )
                                buffer.Add( "<color=#ff5bf2>占领成本：</color>要占领此单位，你必须控制该星球，消耗 <color=#ffdf72>" ).AddNumberMoreReadable( relatedEntityTypeData.MarkStatsFor( effectiveMarkLevel ).MetalCostToClaim )
                                    .Add( " 金属</color>，同时 AI 进程 (AIP) 将增加 <color=#ff620c>" ).Add( relatedEntityTypeData.AIPToClaim.ReadableString ).Add( "</color>。  " );
                            else
                                buffer.Add( "<color=#ff5bf2>占领成本：</color> <color=#ffdf72>" ).AddNumberMoreReadable( relatedEntityTypeData.MarkStatsFor( effectiveMarkLevel ).MetalCostToClaim )
                                    .Add( " 金属</color> 和 <color=#ff620c>" ).Add( relatedEntityTypeData.AIPToClaim.ReadableString ).Add( " AIP</color>。  " );
                        }
                        else
                        {
                            if ( detailLevel >= TooltipDetail.Medium )
                                buffer.Add( "<color=#ff5bf2>占领成本：</color>要占领此单位，你必须控制该星球，消耗 <color=#ffdf72>" ).AddNumberMoreReadable( relatedEntityTypeData.MarkStatsFor( effectiveMarkLevel ).MetalCostToClaim ).Add( " 金属</color>。  " );
                            else
                                buffer.Add( "<color=#ff5bf2>占领成本：</color> <color=#ffdf72>" ).AddNumberMoreReadable( relatedEntityTypeData.MarkStatsFor( effectiveMarkLevel ).MetalCostToClaim ).Add( " 金属</color>。  " );
                        }
                    }
                    else if ( relatedEntityTypeData.AIPToClaim > 0 )
                    {
                        if ( detailLevel >= TooltipDetail.Medium )
                            buffer.Add( "<color=#ff5bf2>占领成本：</color>要占领此单位，你必须控制该星球，AI 进程 (AIP) 将增加 <color=#ff620c>" ).Add( relatedEntityTypeData.AIPToClaim.ReadableString ).Add( "</color>。  " );
                        else
                            buffer.Add( "<color=#ff5bf2>占领成本：</color> <color=#ff620c>" ).Add( relatedEntityTypeData.AIPToClaim.ReadableString ).Add( " AIP</color>。  " );
                    }
                    else if ( relatedEntityTypeData.AIPToClaim < 0 )
                    {
                        if ( detailLevel >= TooltipDetail.Medium )
                            buffer.Add( "<color=#5be6ff>占领奖励：</color>要占领此单位，你必须控制该星球，AI 进程 (AIP) 将减少 <color=#72fff7>" ).Add( relatedEntityTypeData.AIPToClaim.ReadableString ).Add( "</color>（非常好！）。  " );
                        else
                            buffer.Add( "<color=#5be6ff>占领奖励：</color> <color=#72fff7>-" ).Add( relatedEntityTypeData.AIPToClaim.ReadableString ).Add( " AIP</color>。  " );
                    }
                }

                //only show this for actual ships
                if ( !relatedSquadOrNull.IsFakeEntity )
                {
                    if ( detailLevel >= TooltipDetail.Medium )
                    {
                        List<HackingType> hacksAgainst = relatedEntityTypeData.GetListOfHacks();
                        if ( hacksAgainst.Count > 0 )
                        {
                            bool hasDoneHeader = false;
                            foreach ( HackingType hack in hacksAgainst )
                            {
                                if ( hack.GetShouldSkipHackInEntityTooltip( relatedSquadOrNull ) )
                                    continue;
                                if ( !hasDoneHeader )
                                {
                                    hasDoneHeader = true;
                                    buffer.Add( "<color=#f25e1c>You Can Hack:</color> " );
                                }
                                else
                                {
                                    buffer.Add( ", " );
                                }
                                buffer.Add( hack.GetDisplayName( relatedSquadOrNull.TypeData, true ) );
                            }
                            if ( hasDoneHeader )
                                buffer.Add( "  " );
                        }
                    }
                }

                //parent stuff
                if ( !relatedSquadOrNull.IsFakeEntity )
                {
                    GameEntity_Squad parent = relatedSquadOrNull.ParentGameEntity.GetSquad();
                    if ( parent != null )
                    {
                        buffer.Add( "<color=#f25e1c>祖先</color>: " );
                        buffer.Add( "祖先单位：" ).Add( parent.GetTypeDisplayNameSafe() );
                        if ( relatedEntityTypeData.DiesIfParentDies )
                            buffer.Add( "  (如果祖先死亡则此单位也会死亡)" );
                        buffer.Add( ".  " );
                    }
                }
                else
                {
                    if ( relatedEntityTypeData.DiesIfParentDies )
                    {
                        buffer.Add( "<color=#f25e1c>祖先</color>: " );
                        buffer.Add( "通常有祖先单位，如果祖先死亡则此单位也会死亡。  " );
                    }
                }

                //child stuff
                if ( relatedEntityTypeData.BuildPointsPerSecond > 0 )
                {
                    buffer.Add( "<color=#f25e1c>祖代</color>: " );
                    float perSecondRate = relatedEntityTypeData.BuildPointCostForPerSecondConstruction <= 0 ? -1 :
                        ((float)relatedEntityTypeData.BuildPointCostForPerSecondConstruction / (float)relatedEntityTypeData.BuildPointsPerSecond);
                    buffer.Add( "最多建造 " ).Add( relatedEntityTypeData.PersonalShipCapForPerSecondBuildPointConstruction )
                        .Add( " 个后代" );

                    if ( !relatedSquadOrNull.IsFakeEntity )
                    {
                        buffer.Add( "（当前：" ).Add( relatedSquadOrNull.ChildSquads.Count ).Add( "）" );
                    }

                    buffer.Add( "，每 " ).AddFixedDecimal( perSecondRate, 2 ).Add( "秒建造一个。" );

                    List<GameEntityTypeData> list = GameEntityTypeDataTable.Instance.RowsByTag[relatedEntityTypeData.TagToSpawnFromForBuildPointForPerSecondConstruction];
                    if ( list == null || list.Count == 0 )
                        buffer.Add( "错误！没有可建造的后代！" );
                    else if ( list.Count == 1 )
                        buffer.Add( "后代为：" ).Add( list[0].GetDisplayName() ).Add( "  " );
                    else
                    {
                        buffer.Add( "后代混合了：" );
                        for ( int i = 0; i < list.Count; i++ )
                        {
                            if ( i > 0 )
                            {
                                if ( i == list.Count - 1 )
                                {
                                    if ( i == 1 )
                                        buffer.Add( " 和 " );
                                    else
                                        buffer.Add( "，和 " );
                                }
                                else
                                    buffer.Add( ", " );
                            }
                            GameEntityTypeData childType = list[i];
                            buffer.Add( childType.GetDisplayName() );

                            int currentCount = 0;
                            if ( !relatedSquadOrNull.IsFakeEntity )
                            {
                                foreach ( LazyLoadSquadWrapper child in relatedSquadOrNull.ChildSquads )
                                {
                                    GameEntity_Squad childSquad = child.GetSquad();
                                    if ( childSquad != null && childSquad.TypeData == childType )
                                        currentCount++;
                                }
                            }
                            if ( currentCount > 0 )
                                buffer.Add( " (" ).Add( currentCount ).Add( detailLevel >= TooltipDetail.Full ? " 当前)" : ")" );
                        }
                        buffer.Add( ".  " );
                    }

                    if ( !relatedSquadOrNull.IsFakeEntity && detailLevel >= TooltipDetail.Full )
                    {
                        buffer.Add( "(Progress: " ).AddNumberMoreReadable( relatedSquadOrNull.BuildPoints ).Add( " / " )
                            .AddNumberMoreReadable( relatedEntityTypeData.BuildPointCostForPerSecondConstruction )
                            .Add( ")  " ); ;
                    }
                }

                //Last note items
                //--------------------------------------
                debugStage = 3000;

                buffer.StartColor( "888888" );

                debugStage = 3010;

                if ( (relatedEntityTypeData.IsElite) && detailLevel >= TooltipDetail.Medium )
                    buffer.Add( "精英：每支舰队只能添加一条精英舰船线。  " );

                debugStage = 3020;

                if ( ( relatedEntityTypeData.ProvidesAIWarpEntryPoint || relatedEntityTypeData.IsWarpBeacon ) && detailLevel >= TooltipDetail.Medium )
                    buffer.Add( "允许 AI 舰船跃迁至此。  " );

                debugStage = 3030;

                if ( relatedEntityTypeData.IsMobile && relatedEntityTypeData.FleetMembershipStyle == FleetMembershipStyle.Planetary && detailLevel >= TooltipDetail.Medium )
                    buffer.Add( "无法穿越虫洞。  " );

                debugStage = 3030;

                if ( relatedEntityTypeData.AutomaticallyDiesWithCommandStation )
                    buffer.Add( "如果指挥站被摧毁则自毁。  " );

                debugStage = 3040;

                if ( relatedEntityTypeData.SelfAttritionsXPercentPerSecondIfParentShipNotOnPlanet > FInt.Zero && !relatedEntityTypeData.AlwaysSelfAttritions && detailLevel >= TooltipDetail.Full )
                    buffer.Add( "每秒损失 <color=#ffdf72>" ).Add( relatedEntityTypeData.SelfAttritionsXPercentPerSecondIfParentShipNotOnPlanet.ReadableString )
                        .Add( "%</color> 船体生命值，当它不与母船在同一星球上时。  " );

                debugStage = 3045;

                if ( relatedEntityTypeData.AlwaysSelfAttritions && detailLevel >= TooltipDetail.Full )
                    buffer.Add( "每秒损失 <color=#ffdf72>" ).Add( relatedEntityTypeData.SelfAttritionsXPercentPerSecondIfParentShipNotOnPlanet.ReadableString )
                        .Add( "%</color> 船体生命值。  " );

                debugStage = 3050;

                if ( !relatedEntityTypeData.ShipClass.CanBeDamaged )
                    buffer.Add( "免疫所有伤害。  " );

                debugStage = 3051;

                if ( relatedEntityTypeData.ImmuneToRepairs )
                {
                    if ( detailLevel >= TooltipDetail.Full )
                        buffer.Add( "无法修复 -- 我们不知道这东西怎么修。  " );
                    else
                        buffer.Add( "无法修复。  " );
                }

                debugStage = 3052;

                if ( detailLevel >= TooltipDetail.Full && !relatedEntityTypeData.IsScrappingByPlayerDisallowed ) {
                    int percentageToReturnOfMetalAsInt = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "ScrapRefundsOnFriendlyPlanets" );
                    FInt percentageToReturnOfMetalAsFIntMult = (FInt)percentageToReturnOfMetalAsInt / FInt.OneHundred;
                    if (percentageToReturnOfMetalAsInt > 0) {
                        int metalCostForScrapping = (markStats.MetalCost * relatedEntityTypeData.MetalCostMultiplierForScrapping * percentageToReturnOfMetalAsFIntMult).GetNearestIntPreferringHigher();
                        if ( relatedEntityTypeData.MetalCostMultiplierForScrapping == FInt.Zero ) {
                            buffer.Add( "拆解此单位不获得金属。  ");
                        } else {
                            buffer.Add( "在友方星球拆解此单位返还 ").AddMetal_MoreReadable(metalCostForScrapping, true).Add("。  ");
                        }
                    }
                }

                debugStage = 3053;

                if ( relatedEntityTypeData.ImmuneToProtectionByForcefields )
                {
                    if ( detailLevel >= TooltipDetail.Full )
                        buffer.Add( "由于与现实结构产生奇怪交互，无法被力场保护。  " );
                    else
                        buffer.Add( "无法被力场保护。  " );
                }

                if ( relatedEntityTypeData.ImmuneToBonusDamage )
                {
                    buffer.Add( "免疫敌方武器系统加成伤害。  " );
                }

                if ( relatedEntityTypeData.CanPassThroughEnemyForcefields )
                {
                    if ( detailLevel >= TooltipDetail.Full )
                        buffer.Add( "<color=#f25e1c>力场谐波</color>: 可匹配护盾谐波以穿过敌方力场，但其武器射击仍会击中力场本身。  " );
                    else
                        buffer.Add( "<color=#f25e1c>力场谐波</color>: 可穿过敌方力场。  " );
                }

                debugStage = 30531100;

                if ( !relatedSquadOrNull.IsFakeEntity && relatedSquadOrNull.CurrentStateOfMatter.ShouldShowDescriptionInTooltips )
                {
                    buffer.Add( "<color=#ff5bf2>" ).Add( relatedSquadOrNull.CurrentStateOfMatter.DisplayName ).Add( " 物质状态" );
                    if ( detailLevel >= TooltipDetail.Full )
                    {
                        buffer.Add( ": " );
                        buffer.Add( relatedSquadOrNull.CurrentStateOfMatter.DescriptionFull );
                        buffer.Add( "</color>  " );
                    }
                    else if ( detailLevel >= TooltipDetail.Medium )
                    {
                        buffer.Add( ": " );
                        buffer.Add( relatedSquadOrNull.CurrentStateOfMatter.DescriptionShort );
                        buffer.Add( "</color>  " );
                    }
                    else
                        buffer.Add( "</color>  " );
                }

                debugStage = 30531200;

                if ( relatedEntityTypeData.AlternativeStateOfMatter != null && relatedEntityTypeData.PhasesToOtherAlternativeStateOfMatterAfterSeconds > 0 )
                {
                    int returnTime = relatedEntityTypeData.PhasesBackToDefaultStateOfMatterAfterSeconds;
                    if ( returnTime <= 0 )
                        returnTime = relatedEntityTypeData.PhasesToOtherAlternativeStateOfMatterAfterSeconds;

                    if ( detailLevel >= TooltipDetail.Full )
                    {
                        buffer.Add( "与时空结构的奇怪交互导致此单位仅部分时间存在于正常位面" );
                        if ( relatedEntityTypeData.AlternativeStateOfMatter.CanTargetOtherUnitsInThisState )
                            buffer.Add( "，使其在另一种状态下无敌且隐形。  " );
                        else
                            buffer.Add( "，使其在另一种状态下无敌且隐形，但对于同样处于该状态的其他单位除外。  " );
                    }
                    
                    buffer.Add( "相位切换至 " ).Add( relatedEntityTypeData.AlternativeStateOfMatter.DisplayName ).Add( " 每 " )
                        .Add( relatedEntityTypeData.PhasesToOtherAlternativeStateOfMatterAfterSeconds )
                        .Add( " 秒，然后 " )
                        .Add( returnTime ).Add( " 秒后切回。  " );
                }

                debugStage = 30531300;

                if ( relatedEntityTypeData.StateOfMatterToBecomeOnWormholeExit != null )
                {
                    int returnTime = relatedEntityTypeData.ReturnsToDefaultStateOfMatterAfterSecondsFromWormholeExit;
                    buffer.Add( "<color=#f25e1c>相位跃迁</color>: 每当通过虫洞时，相位切换至 " ).Add( relatedEntityTypeData.StateOfMatterToBecomeOnWormholeExit.DisplayName );
                    if ( returnTime > 0 )
                        buffer.Add( "，然后在 " ).Add( returnTime ).Add( " 秒后恢复正常。  " );
                    else
                        buffer.Add( "，且不计划恢复正常。  " );
                }

                debugStage = 30531400;

                if ( relatedEntityTypeData.ForcedToBeThisStateOfMatterWhenOnFormerOrCurrentAIHomeworldOrBastionWorldsUnlessEnemyKingPresent != null )
                {
                    buffer.Add( "<color=#f25e1c>AI 核心相位</color>: 当处于当前或前 AI 母星或 AI 堡垒世界时，相位切换至 " ).Add( relatedEntityTypeData.ForcedToBeThisStateOfMatterWhenOnFormerOrCurrentAIHomeworldOrBastionWorldsUnlessEnemyKingPresent.DisplayName )
                        .Add( "（除非有敌方首领在场）。  " );
                }

                debugStage = 3060;

                if ( relatedEntityTypeData.ImmuneToAllDamageForSecondsAfterCreation > 0 && detailLevel >= TooltipDetail.Medium )
                {
                    if ( !relatedSquadOrNull.IsFakeEntity )
                    {
                        if ( relatedEntityTypeData.ImmuneToAllDamageForSecondsAfterCreation > relatedSquadOrNull.GetSecondsSinceCreation() )
                            buffer.StartColor( QuickColors.NewValue ).Add( "免疫所有伤害，持续 " ).Add( relatedEntityTypeData.ImmuneToAllDamageForSecondsAfterCreation - 
                                relatedSquadOrNull.GetSecondsSinceCreation() ).Add( " 更多秒。  " ).EndColor();
                    }
                    else
                        buffer.Add( "免疫所有伤害，持续 " ).StartColor( QuickColors.NewValue )
                            .Add( relatedEntityTypeData.ImmuneToAllDamageForSecondsAfterCreation ).Add( "秒</color>（创建后）。  " );
                }

                debugStage = 3061;
                // Puffin Note Hydra Heads
                if ( relatedEntityTypeData.BuildPointsPerDamageTaken > FInt.Zero )
                {
                    if ( !relatedSquadOrNull.IsFakeEntity )
                    {
                        buffer.Add( "当前拥有 <color=#ffdf72>" ).Add( relatedSquadOrNull.BuildPoints )
                        .Add( "</color> 来自承受伤害的构建点。" );
                    }
                }
                debugStage = 3062;
                // Puffin Note Neinzul Fireflies
                if ( !relatedSquadOrNull.IsFakeEntity && relatedSquadOrNull.NumberOfWeaponPoints > FInt.Zero )
                {
                    if ( !relatedSquadOrNull.IsFakeEntity )
                    {
                        buffer.Add( "当前拥有 <color=#ffdf72>" ).Add( relatedSquadOrNull.NumberOfWeaponPoints )
                        .Add( "</color> 武器点数。 " );
                    }
                }

                debugStage = 3063;
                // For Death Effects. Lists each one a unit has been hit by, the current amount, and the amount required.
                // pair.Value = current amount, pair.Key.Scale = amount required (scale being from the XML for each death effect).
                if ( detailLevel >= TooltipDetail.Medium )
                {
                    if ( !relatedSquadOrNull.IsFakeEntity && relatedSquadOrNull.DeathEffectCausingDamageReceivedToEntity.Count > 0 )
                    {
                        int pairCount = relatedSquadOrNull.DeathEffectCausingDamageReceivedToEntity.Count;
                        foreach ( KeyValuePair<DeathEffectType, int> pair in relatedSquadOrNull.DeathEffectCausingDamageReceivedToEntity )
                        {
                            buffer.Add( "\n当前有 <color=#ffdf72>" ).Add( pair.Value ).Add( " " )
                                .Add( pair.Key.DescriptionDamageName ).Add( "</color> 伤害，需要 " ).Add( pair.Key.Scale ).Add( " 才能触发效果。 " );
                            continue;
                        }
                    }
                }

                debugStage = 3070;

                if ( World_AIW2.Instance.IsFuelEnabled && !relatedSquadOrNull.IsFakeEntity )
                {
                    if ( relatedSquadOrNull.GetIsOutguardUnit() )
                    {
                        if ( AIWar2GalaxySettingQuickAccess.OutguardAlsoCostXenon )
                        {
                            FInt worstXenonRatio = FInt.One;
                            foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                            {
                                if ( worstXenonRatio > player.FuelXenonOveruseRatio )
                                    worstXenonRatio = player.FuelXenonOveruseRatio;
                            }
                            if ( worstXenonRatio < FInt.One )
                                buffer.Add( "\n<color=#ff6935>当前生命值和护盾因最差的玩家氙气比率而降低，为 " )
                                    .AddFixedDecimal( worstXenonRatio.ToFloatNonSim(), 2 ).Add( "x 正常值。</color>  " );
                        }
                    }
                    else
                    {
                        switch ( relatedSquadOrNull.TypeData.FuelUseType )
                        {
                            case ResourceType.FuelArgon:
                                {
                                    if ( relatedSquadFactionOrNull != null && relatedSquadFactionOrNull.FuelArgonOveruseRatio < FInt.One )
                                        buffer.Add( "\n<color=#ff6935>当前生命值和护盾因你的氩气比率而降低，为 " )
                                            .AddFixedDecimal( relatedSquadFactionOrNull.FuelArgonOveruseRatio.ToFloatNonSim(), 2 ).Add( "x 正常值。</color>  " );
                                }
                                break;
                            case ResourceType.FuelRadon:
                                {
                                    if ( relatedSquadFactionOrNull != null && relatedSquadFactionOrNull.FuelRadonOveruseRatio < FInt.One )
                                        buffer.Add( "\n<color=#ff6935>当前生命值和护盾因你的氡气比率而降低，为 " )
                                            .AddFixedDecimal( relatedSquadFactionOrNull.FuelRadonOveruseRatio.ToFloatNonSim(), 2 ).Add( "x 正常值。</color>  " );
                                }
                                break;
                            case ResourceType.FuelXenon:
                                {
                                    if ( relatedSquadFactionOrNull != null && relatedSquadFactionOrNull.FuelXenonOveruseRatio < FInt.One )
                                        buffer.Add( "\n<color=#ff6935>当前生命值和护盾因你的氙气比率而降低，为 " )
                                            .AddFixedDecimal( relatedSquadFactionOrNull.FuelXenonOveruseRatio.ToFloatNonSim(), 2 ).Add( "x 正常值。</color>  " );
                                }
                                break;
                        }
                    }
                }

                if ( detailLevel >= TooltipDetail.Full )
                {
                    if ( relatedEntityTypeData.IsCrippledInsteadOfDying && (relatedSquadOrNull == null || relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player || 
                        relatedSquadOrNull.GetFactionTypeSafe() == FactionType.NaturalObject ) )
                    {
                        buffer.Add( "不会死亡，而是在 1 HP 时变为重创状态。  " );
                        if ( relatedEntityTypeData.ForcedToBailOutOnCripple_Any )
                            buffer.Add( "重创时将使用弹射功能前往友方星球。  " );
                        else if ( relatedEntityTypeData.ForcedToBailOutOnCripple_DeepstrikeOnly )
                            buffer.Add( "在深袭区域重创时将使用弹射功能前往友方星球。  " );
                    }
                    else if ( relatedEntityTypeData.DiesToRemains && (relatedSquadOrNull == null || relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player) )
                        buffer.Add( "由人类控制时，死亡变为可重建的残骸。  " );
                    else if ( relatedEntityTypeData.RevertsToNeutralOnDeathIfPermadeathSettingIsFalse != null && relatedEntityTypeData.RevertsToNeutralOnDeathIfPermadeathSettingIsFalse.Length > 0 )
                    {
                        if ( !World_AIW2.Instance.Setup.GetBoolBySetting( relatedEntityTypeData.RevertsToNeutralOnDeathIfPermadeathSettingIsFalse ) )
                            buffer.Add( "死亡时恢复为中立状态，而非真正死亡。  " );
                    }
                }
                debugStage = 30710;
                EntityOrderCollection orders = relatedSquadOrNull?.Orders;
                debugStage = 30711;
                EntityOrder firstOrder = orders == null ? new EntityOrder( -1 ) : orders.GetQueuedOrderAtIndex_OrNull( 0 );
                EntityOrder lastOrder = orders == null ? new EntityOrder( -1 ) : orders.GetLastQueuedOrder_OrNull();
                debugStage = 30712;

                if ( !relatedSquadOrNull.IsFakeEntity && relatedSquadOrNull.IsAfterANonHumanTeam_NonSim )
                {
                    buffer.Add( "\n<color=#ff5bf2>不是追你" );
                    if ( detailLevel >= TooltipDetail.Full )
                    {
                        buffer.Add( "：</color>如果你靠近该舰船它会向你开火，但除此之外它会忽略你。它正忙于追捕 " );
                        buffer.Add( relatedSquadOrNull.CalculateFactionIsChasingInsteadOfHumans() );
                        buffer.Add( ".  " );
                        if (showDebugInfoInTooltip) {
                            buffer.Add(" Debug: ");
                            buffer.Add("\n- ShouldNotBeConsideredAsThreatToHumanTeam: ").Add(relatedSquadOrNull.ShouldNotBeConsideredAsThreatToHumanTeam);
                            buffer.Add("\n- Orders.BehaviorRelatedFaction (index ").Add(relatedSquadOrNull.Orders.BehaviorRelatedFactionIndex).Add("): ");
                            if (relatedSquadOrNull.Orders.BehaviorRelatedFactionIndex < 0) {
                                buffer.Add("none");
                            } else {
                                Faction behaviorRelatedFaction = World_AIW2.Instance.GetFactionByIndex(relatedSquadOrNull.Orders.BehaviorRelatedFactionIndex);
                                buffer.AddFactionNameInItsColor(behaviorRelatedFaction);
                                if (behaviorRelatedFaction != null) {
                                    buffer.Add(" (type: ").Add(Extensions.ToString(behaviorRelatedFaction.Type)).Add(")");
                                }
                            }
                            buffer.Add("\n- Fireteam (id ").Add(relatedSquadOrNull.FireteamId).Add("): ");
                            if (relatedSquadOrNull.FireteamId == -1) {
                                buffer.Add("none");
                            } else {
                                Fireteam team = (Fireteam)relatedSquadOrNull.GetFactionBaseInfoOrNull_Safe()?.GetFireteamBaseById( relatedSquadOrNull.FireteamId );
                                if ( team == null ) {
                                    buffer.Add("(null team)");
                                }
                            }
                            buffer.Add("\n - Required Target:");
                            FireteamRequiredTarget fireteamTarget = relatedSquadOrNull.FireteamSpecificationOrNull;
                            if (fireteamTarget == null || !fireteamTarget.IsActive()) {
                                buffer.Add("(inactive) ");
                            } else {
                                buffer.Add("\n   - AgainstFaction: ");
                                Faction AgainstFaction = fireteamTarget.AgainstFaction;
                                if (AgainstFaction == null) {
                                    buffer.Add("null");
                                } else {
                                    buffer.Add(AgainstFaction.GetDisplayNameWithoutPlayerNames(), AgainstFaction.FactionCenterColor.ColorHexBrighter);
                                    buffer.Add(" (type: ").Add(Extensions.ToString(AgainstFaction.Type)).Add(")");
                                }
                                buffer.Add("\n   - AgainstFactionAllegiance: ").Add(fireteamTarget.AgainstFactionAllegiance, "4649a9");
                                buffer.Add("\n   - RequiredTag: ").Add(fireteamTarget.RequiredTag);
                                buffer.Add("\n   - AgainstGameEntity: ");
                                GameEntity_Squad AgainstGameEntity = fireteamTarget.AgainstGameEntity.GetSquad();
                                if (AgainstGameEntity == null) {
                                    buffer.Add("null");
                                } else {
                                    buffer.Add(AgainstGameEntity.TypeData.GetDisplayName(), AgainstGameEntity.GetFactionCenterColorHexBrighter_Safe());
                                    buffer.Add(" on ").AddPlanetNameFormated(AgainstGameEntity.Planet, false);
                                    buffer.Add(" faction ").AddFactionNameInItsColor(AgainstGameEntity.GetFactionOrNull_Safe());
                                    buffer.Add(" (type: ").Add(Extensions.ToString(AgainstGameEntity.GetFactionTypeSafe())).Add(")");
                                }
                            }
                            buffer.Add("\nEnd NOT AFTER YOU debug");
                        }
                    }
                    else if ( detailLevel >= TooltipDetail.Medium )
                    {
                        buffer.Add( ":</color> Hunting " );
                        buffer.Add( relatedSquadOrNull.CalculateFactionIsChasingInsteadOfHumans() );
                        buffer.Add( ".  " );
                    }
                    else
                        buffer.Add( "</color>  " );
                }

                if ( showDebugInfoInTooltip && !relatedSquadOrNull.IsFakeEntity )
                {
                    debugStage = 3072;
                    if (orders != null)
                    {
                        debugStage = 3073;
                        buffer.Add("行为: " + orders.Behavior + "。 ");
                        if (orders.GetQueuedOrderCount() == 0 || firstOrder.TypeData == null )
                            buffer.Add("无排队命令\n");
                        else
                        {
                            debugStage = 3074;
                            if (firstOrder.TypeData.Type == EntityOrderType.Wormhole && detailLevel >= TooltipDetail.Full )
                            {
                                buffer.Add("此单位有 " + orders.GetQueuedOrderCount() + " 个排队命令， " + firstOrder.TypeData.Type + "。 " + World_AIW2.Instance.GetPlanetByIndex( firstOrder.RelatedPlanetIndex).Name);
                                debugStage = 30741;
                                if(orders.GetQueuedOrderCount() > 1 && lastOrder.TypeData != null )
                                {
                                    Planet planet = World_AIW2.Instance.GetPlanetByIndex( lastOrder.RelatedPlanetIndex );
                                    if ( planet != null )
                                        buffer.Add(" --> ").Add( planet.Name) ;
                                }
                                buffer.Add(". ");
                            }
                            else
                                buffer.Add("此单位有 " + orders.GetQueuedOrderCount() + " 个排队命令，第一个为 " + firstOrder.TypeData.Type + "。 ");
                        }
                    }
                    debugStage = 3075;
                    if(relatedSquadOrNull.GetFactionTypeSafe() == FactionType.AI &&
                       relatedSquadOrNull.TypeData.IsMobile && relatedSquadOrNull.TypeData.IsCombatant)
                    {
//                        bool isAttacker = ( relatedEntity.GetEffectiveOrders().Behavior == EntityBehaviorType.Attacker_Full );
//                        if ( isAttacker && relatedEntity.Guarding.GetSquad() == null)
                        debugStage = 3076;
                        if(relatedSquadOrNull.GuardedUnit.GetSquad() == null)
                        {
                            debugStage = 3077;
                            buffer.Add("威胁 " );
                            Faction againstFaction = null;
                            if (orders != null)
                            {
                                buffer.Add(" against ").Add(orders.BehaviorRelatedFactionIndex);
                                againstFaction = World_AIW2.Instance.GetFactionByIndex(orders.BehaviorRelatedFactionIndex);
                                if(againstFaction != null)
                                {
                                    if(againstFaction.Type == FactionType.SpecialFaction)
                                    {
                                        buffer.Add(": ").Add(againstFaction.GetDisplayName() );
                                    }
                                    else
                                        buffer.Add(": ").Add( EnumNameCache.GetName( againstFaction.Type ) );
                                } else {
                                    buffer.Add(": players");
                                }
                                buffer.Add(". ");
                            }
                            if(relatedSquadOrNull.WaitingAgainstPlanetIndex != -1)
                            {
                                debugStage = 3078;
                                int secondsIHaveBeenWaiting = World_AIW2.Instance.GameSecond - relatedSquadOrNull.StartedWaitingAtGameSecond;
                                buffer.Add("正在等待对抗 " + World_AIW2.Instance.GetPlanetByIndex(relatedSquadOrNull.WaitingAgainstPlanetIndex).Name + 
                                    " for " + secondsIHaveBeenWaiting + " seconds. ");
                            }
                            if ( relatedSquadOrNull.TypeData.NotEligibleToJoinHunterFleet || relatedSquadOrNull.TypeData.IsDrone )
                                buffer.Add( "此类舰船将留在威胁舰队中，而不会加入猎手。" );
                            else if (againstFaction != null && againstFaction.Type!= FactionType.Player )
                                buffer.Add( "此舰船将留在威胁舰队中，因为它不针对人类。" );
                            else
                            {
                                Faction facOrNull = relatedSquadOrNull.GetFactionOrNull_Safe();
                                AISentinelsCoreData factionExternal = facOrNull == null ? null : facOrNull.TryGetAISentinelsCoreData()?.SentinelInfo;
                                if ( factionExternal == null )
                                    buffer.Add( "此舰船将留在威胁舰队中，因其未链接到哨兵蜂巢思维。" );
                                else
                                {
                                    int secondsIHaveBeenWaiting = World_AIW2.Instance.GameSecond - relatedSquadOrNull.StartedWaitingAtGameSecond;
                                    if ( secondsIHaveBeenWaiting < 0 )
                                        secondsIHaveBeenWaiting = 0;

                                    int hunterFleetWaitThreshold = factionExternal.AIDifficulty.SecondsThreatWaitsBeforeJoiningHunterFleet;

                                    int secondsIHaveBeenThreatfleet = relatedSquadOrNull.BecameThreatfleetAtGameSecond <= 0 ? 0 : World_AIW2.Instance.GameSecond - relatedSquadOrNull.BecameThreatfleetAtGameSecond;
                                    int hunterFleetExistsThreshold = factionExternal.AIDifficulty.SecondsThreatExistsAsThreatBeforeJoiningHunterFleet;

                                    buffer.Add( "此舰船将在 " ).Add( hunterFleetWaitThreshold - secondsIHaveBeenWaiting )
                                        .Add( " 秒的待机攻击时间后离开威胁舰队加入猎手，或在经过 " ).Add( hunterFleetExistsThreshold - secondsIHaveBeenThreatfleet )
                                        .Add( " 秒的存在时间后离开。" );
                                }
                            }

                        }
                        debugStage = 3079;
                        if( relatedSquadOrNull.ExoGalacticAttackPlanetIdx != -1)
                        {
                            GameEntity_Squad target = relatedSquadOrNull.ExoGalacticAttackTarget.GetSquad();
                            if ( target != null )
                                buffer.Add("属于外银河打击部队，目标为 " + target.TypeData.GetDisplayName() + " 位于 " + target.GetPlanetName_Safe() + "。");
                            else
                                buffer.Add("属于外银河打击部队，目标为 " + World_AIW2.Instance.GetPlanetByIndex(relatedSquadOrNull.ExoGalacticAttackPlanetIdx).Name + " 但无明确目标。");
                        }
                        GameEntity_Squad guarded = relatedSquadOrNull.GuardedUnit.GetSquad();
                        if(guarded != null)
                            buffer.Add("守卫 " + guarded.TypeData.InternalName);
                        
                    }
                }
                debugStage = 30712;
                if ( !relatedSquadOrNull.IsFakeEntity && relatedSquadOrNull.DespawnsInXSeconds > 0 &&
                     detailLevel >= TooltipDetail.Medium )
                    buffer.Add("此实体将在 " + relatedSquadOrNull.DespawnsInXSeconds + " 秒后消失。", "7486d1");

                debugStage = 3080;
                
                buffer.EndColor();

                //Now to the appender
                //--------------------------------------

                debugStage = 5000;
                if ( relatedEntityTypeData.DescriptionAppender != null && !relatedSquadOrNull.IsFakeEntity && !relatedSquadOrNull.IsFakeEntity )
                    relatedEntityTypeData.DescriptionAppender.AddToDescriptionBuffer( relatedSquadOrNull, relatedEntityTypeData, buffer );

                //And then the "what am I doing right now" appender
                //--------------------------------------

                debugStage = 5400;

                if ( owningFactionOrNull?.Type == FactionType.Player )
                {
                    var effectiveGalaxyCap = relatedEntityTypeData.CalculateEffectiveGalaxyWideCapForPlayersConstructing(owningFactionOrNull);
                    if (effectiveGalaxyCap > 0)
                    {
                        int countOfExisting = 0;
                        foreach ( GameEntity_Squad squad in owningFactionOrNull.Squads( EntityRollupType.HasGalaxyWideCapForPlayersConstructing ) )
                        {
                            if ( squad.TypeData.GalaxyWideCapMatchString == relatedEntityTypeData.GalaxyWideCapMatchString )
                                countOfExisting++;
                        }

                        buffer.Add( "     全银河上限：" ).Add( countOfExisting );
                        buffer.Add( "/" ).Add( effectiveGalaxyCap );

                        if ( detailLevel >= TooltipDetail.Full )
                        {
                            if ( relatedEntityTypeData.GalaxyWideCapForPlayersConstructingXPlanetVariable > 0 &&
                                 relatedEntityTypeData.AddedGalaxyWideCapForPlayersConstructingPerXPlanets > 0 )
                            {
                                buffer.Add( "<size=80%>" );
                                buffer.Add( " (" ).Add( relatedEntityTypeData.BaseGalaxyWideCapForPlayersConstructing );
                                buffer.Add( " + " ).Add( relatedEntityTypeData.AddedGalaxyWideCapForPlayersConstructingPerXPlanets );
                                buffer.Add( " 每 " ).Add( relatedEntityTypeData.GalaxyWideCapForPlayersConstructingXPlanetVariable );
                                buffer.Add( " 个玩家拥有的星球。").Add(World_AIW2.Instance.PlayerOwnedPlanets, "a1ffa1" ).Add(" 玩家星球)" );
                                buffer.Add( "</size>  " );
                            }
                        }
                        else
                            buffer.Add( "  " );
                    }
                }

                if ( !relatedSquadOrNull.IsFakeEntity)
                {
                    #region Fleet Centerpiece Things
                    debugStage = 5460;
                    if ( relatedMembershipOrNull == null || relatedMemFleetOrNull == null )
                    {
                        debugStage = 546100;
                        if ( relatedSquadOrNull.HasBeenRemovedFromSim || relatedSquadOrNull.ToBeRemovedAtEndOfThisFrame)
                            buffer.Add( "<color=#ff5842>此舰船已死亡。</color>  " );
                        else
                            buffer.Add( "<color=#ff5842>错误：我的舰队成员资格为空！</color>  " );
                    }
                    else if ( relatedMemFleetOrNull.Centerpiece.GetSquad() == relatedSquadOrNull )
                    {
                        bool isCity = relatedSquadOrNull.TypeData.SpecialType == SpecialEntityType.CityCenter;

                        debugStage = 546200;
                        if ( detailLevel < TooltipDetail.Full )
                            buffer.Add( "是 " ).Add( isCity ? relatedSquadOrNull.TypeData.NameForCityCenter : "旗舰" ).Add( " 的舰队 " )
                                .Add( relatedMemFleetOrNull.GetName() )
                                .Add( "，总强度 " );
                        else
                            buffer.Add( "我是 " ).Add( isCity ? relatedSquadOrNull.TypeData.NameForCityCenter : "旗舰" ).Add( " 的舰队 " )
                                .Add( relatedMemFleetOrNull.GetName() )
                                .Add( "，总强度 " );

                        debugStage = 546300;
                        //useLocalUIValues is calculated inside here.
                        {
                            int currentStrength = relatedMemFleetOrNull.GetCurrentStrengthOfFleet_ForUIOnly( relatedMemFleetOrNull.GetFactionType_Safe() == FactionType.NaturalObject );
                            int maxStrength = relatedMemFleetOrNull.GetMaxStrengthOfFleet_ForUIOnly( relatedMemFleetOrNull.GetFactionType_Safe() == FactionType.NaturalObject );
                            float ratio = (float)currentStrength/maxStrength;
                            Color fleetColor = EntityText.GetProportionalStrengthColor(ratio);
                            buffer.StartColor( fleetColor );
                            EntityText.AddSingleValueStrengthOnly( buffer, maxStrength );
                            buffer.EndColor();
                        }
                        buffer.Add( ".  " );
                        if ( relatedMembershipOrNull != null && relatedMemFleetOrNull != null &&
                             relatedMemFleetOrNull.TimesCrippled_UIOnly > 0 &&
                             detailLevel >= TooltipDetail.Full )
                        {
                            buffer.Add( "此旗舰已被重创 " ).Add( relatedMemFleetOrNull.TimesCrippled_UIOnly, "a1ffa1" ).Add( " 次。  " );
                        }

                        debugStage = 546400;

                        if ( isCity && relatedMemFleetOrNull != null )
                        {
                            int totalCityPoints = relatedMemFleetOrNull.CalculateTotalCitySockets();
                            if ( totalCityPoints > 0 )
                            {
                                int spentCityPoints = relatedMemFleetOrNull.CalculateSpentCitySockets();
                                buffer.Add( relatedEntityTypeData.NameForCitySockets_Plural ).Add( " 已用: " ).Add( spentCityPoints ).Add( "/" ).Add( totalCityPoints ).Add( "。  " );
                            }
                        }
                        if ( relatedSquadOrNull == null || relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player )
                        {
                            if ( relatedEntityTypeData.FiringDelayForTransportedShips > FInt.Zero && !isCity )
                            {
                                if ( detailLevel < TooltipDetail.Full )
                                    buffer.Add( "卸载的舰船有 " ).Add( relatedEntityTypeData.FiringDelayForTransportedShips, "a1ffa1" ).Add( " 秒的开火延迟。" );
                                else if ( detailLevel >= TooltipDetail.Medium )
                                    buffer.Add( "从此旗舰卸载的舰船有 " ).Add( relatedEntityTypeData.FiringDelayForTransportedShips, "a1ffa1" ).Add( " 秒后才能开火。" );
                            }
                        }
                        if ( detailLevel >= TooltipDetail.Full && relatedMemFleetOrNull != null && relatedMemFleetOrNull.FleetOnFriendlyPlanet && !isCity )
                        {
                            buffer.Add( "此舰队位于友方星球，可以更快重建舰船。" );
                        }

                        if ( relatedMemFleetOrNull != null && relatedMemFleetOrNull.IsFleetInTransportLoadMode && !isCity )
                        {
                            if ( detailLevel < TooltipDetail.Full )
                                buffer.Add( "运输就绪模式。" );
                            else
                                buffer.Add( "我的舰队处于运输就绪模式，所有舰船正试图进入舱位。" );
                        }

                        debugStage = 5465;
                        bool isForCapture = relatedMemFleetOrNull != null && relatedMemFleetOrNull.GetFactionType_Safe() == FactionType.NaturalObject;
                        //buffer.Add( " useLocalUIValues = " + useLocalUIValues + " " + relatedEntity.GetFleetFactionType_Safe() );

                        debugStage = 5462;
                        //if ( detailLevel >= TooltipDetail.Medium )
                        if ( relatedMemFleetOrNull != null && localPlayerFactionOrNull != null )
                        {
                            ExternalFleetBaseInfo fleetBaseInfoOrNull = relatedMemFleetOrNull.BaseInfo;
                            if ( fleetBaseInfoOrNull != null )
                                fleetBaseInfoOrNull.AddToTooltipForFleet( buffer, detailLevel );

                            foreach ( FleetMembership mem in relatedMemFleetOrNull.MemberGroupsSorted_NonSim_ForUIOnly_SeriouslyDoNotCallFromBGCode() )
                            {
                                // Special check for Cities; we only want to display whats been built in the minimal detail tooltip.
                                int effectiveHere = mem.GetCountPresent( true, ExtraFromStacks.IncludePrecalc );
                                if ( detailLevel < TooltipDetail.Medium && isCity && effectiveHere <= 0 )
                                    continue;

                                bool isExcludedSpecialType = false;
                                switch ( mem.TypeData.SpecialType )
                                {
                                    case SpecialEntityType.AICommandStationOriginal:
                                    case SpecialEntityType.AICommandStationReconquest:
                                    case SpecialEntityType.BattlestationBasic:
                                    case SpecialEntityType.BattlestationCitadel:
                                    case SpecialEntityType.CityCenter:
                                    case SpecialEntityType.HumanHomeCommand:
                                    case SpecialEntityType.MobileOfficerCombatFleetFlagship:
                                    case SpecialEntityType.MobileStrikeCombatFleetFlagship:
                                    case SpecialEntityType.MobileCustomUnattachedFleetFlagship:
                                    case SpecialEntityType.MobileSupportFleetFlagship:
                                    case SpecialEntityType.NormalHumanCommandStation:
                                    case SpecialEntityType.NPCFactionCenterpiece:
                                    case SpecialEntityType.WardenSecretNinjaHideout:
                                        isExcludedSpecialType = true;
                                        break;
                                }
                                if ( isExcludedSpecialType )
                                    continue;

                                GameEntityTypeData.MarkLevelStats markStatsForUse = mem.ForMark;
                                int squadCapToUse = mem.EffectiveSquadCap;
                                if ( isForCapture )
                                {
                                    byte markLevelToUse = localPlayerFactionOrNull.GetGlobalMarkLevelForShipLine( mem.TypeData );
                                    markStatsForUse = mem.TypeData.GetForMark( markLevelToUse );

                                    squadCapToUse = mem.TypeData.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap(
                                        mem.TypeData, mem.ExplicitBaseSquadCap, mem.ExplicitBaseSquadCap, markStatsForUse.MarkLevel );
                                }

                                if ( squadCapToUse <= 0 && mem.TransportContents.Count <= 0 )
                                    continue;

                                if ( markStatsForUse != null )
                                    buffer.StartColor( markStatsForUse.MarkLevel.ColorHex );
                                buffer.Add( mem.TypeData == null ? "nulltype" : mem.TypeData.DisplayName ).Add( " " )
                                    .Add( markStatsForUse == null ? "nullmark" : markStatsForUse.MarkLevel.Abbreviation );
                                if ( markStatsForUse != null )
                                    buffer.EndColor();

                                // If we don't have a cap, or we're a City in minimal detail, don't list our capacity.
                                if ( squadCapToUse <= 0 || (detailLevel < TooltipDetail.Medium && isCity) )
                                {
                                    if ( effectiveHere > mem.CalculateTransportedContentsCount() )
                                    {
                                        if ( effectiveHere > 0 )
                                            buffer.StartColor( QuickColors.NewValue );
                                        else
                                            buffer.StartColor( QuickColors.OldValue );
                                        buffer.Add( " x" ).Add( effectiveHere );
                                        buffer.EndColor();
                                    }
                                }
                                else
                                {
                                    if ( squadCapToUse > mem.CalculateTransportedContentsCount() )
                                    {
                                        if ( isForCapture )
                                            buffer.StartColor( ShipLineEntry.GetColorForShipLineScore(mem.Score) );
                                        else if ( effectiveHere >= squadCapToUse )
                                            buffer.StartColor( QuickColors.NewValue );
                                        else 
                                            buffer.StartColor( ColorMath.Orange );

                                        buffer.Add( " x" ).Add( effectiveHere );
                                        if ( !mem.TypeData.NoExplicitCap )
                                            buffer.Add( "/" ).Add( squadCapToUse ); //we almost always want to print the cap, but some unit types (necromancer in particular) use a different sort of cap
                                        buffer.EndColor();
                                        if ( relatedMemFleetOrNull != null && relatedMemFleetOrNull.Faction != null &&
                                            relatedMemFleetOrNull.Faction.Type == FactionType.NaturalObject &&
                                             detailLevel == TooltipDetail.Full &&
                                             mem.TypeData.TechUpgradesThatBenefitMe.Count > 0 )
                                        {
                                            buffer.StartColor( Color.grey ).Add("<size=60%>");
                                            for ( int i = 0; i < mem.TypeData.TechUpgradesThatBenefitMe.Count; i++ )
                                            {
                                                buffer.Add(" ").Add(mem.TypeData.TechUpgradesThatBenefitMe[i].DisplayName);
                                            }
                                            buffer.Add("</size>").EndColor();
                                        }
                                    }
                                }

                                if ( mem.TypeData.IsDrone )
                                {
                                    buffer.StartColor( ColorMath.LightLeafGreen ).Add( " (" );
                                    buffer.Add( (((float)mem.MetalSpentConstructingCurrentReplacement / (float)mem.ForMark.MetalCost) * 100f).ToString( "00.0" ) );
                                    buffer.Add( "%" ).Add( ")" ).EndColor();
                                }

                                if ( mem.NumberCreatedButNotDeployed > 0 )
                                    buffer.StartColor( ColorMath.LessLightGreen ).Add( " (" ).Add( mem.NumberCreatedButNotDeployed ).Add( " Undeployed Drones)" ).EndColor();

                                if ( mem.TransportContents.Count > 0 )
                                    buffer.StartColor( ColorMath.IceBlue ).Add( " (Transporting " ).Add( mem.CalculateTransportedContentsCount() ).Add( ")" ).EndColor();

                                buffer.Add( "  " );
                            }
                        }
                    }

                    debugStage = 87394000;
                    if ( relatedSquadOrNull.ActiveHack != null )
                    {
                        debugStage = 87394010;
                        buffer.Add("此单位正在黑客入侵；它将引擎能量转移到黑客矩阵，因此速度降低且无法离开星球。");
                        if ( relatedSquadOrNull.GetMaxCloakingPoints() > 0 )
                            buffer.Add("黑客入侵会禁用舰船的隐形系统。");
                    }
                    #endregion

                    debugStage = 87396000;
                    #region Error Checks For Strange Things
                    if ( showDebugInfoInTooltip )
                    {
                        if ( relatedMembershipOrNull != null && !relatedMembershipOrNull.DoesShipLineContainThisExactShip( relatedSquadOrNull ) )
                            buffer.Add( "<color=#ff731e>ERROR!  The fleet membership for this ship does not actually contain it!</color>  " );
                        if ( relatedSquadOrNull.ToBeRemovedAtEndOfThisFrame )
                            buffer.Add( "<color=#ff731e>ERROR!  ToBeRemovedAtEndOfThisFrame = true!</color>  " );
                        if ( relatedSquadOrNull.HasBeenRemovedFromSim )
                            buffer.Add( "<color=#ff731e>ERROR!  HasBeenRemovedFromSim = true!</color>  " );
                        if ( relatedSquadOrNull.IHaveDiedSoPleaseDisregardAnyRequestForANewVisualObject )
                            buffer.Add( "<color=#ff731e>ERROR!  IHaveDiedSoPleaseDisregardAnyRequestForANewVisualObject = true!</color>  " );
                        if ( relatedSquadOrNull.HasDoneOnDeathSinceLastClaimed )
                            buffer.Add( "<color=#ff731e>ERROR!  HasDoneOnDeathSinceLastClaimed = true!</color>  " );
                        if ( Engine_AIW2.Instance.CurrentGameViewMode != GameViewMode.GalaxyMapView )
                        {
                            if ( relatedSquadOrNull.InstancedRenderer == null )
                                buffer.Add( "<color=#ff731e>ERROR!  InstancedRenderer is null!</color>  " );
                            if ( relatedSquadOrNull.Planet != Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() )
                                buffer.Add( "<color=#ff731e>ERROR!  Planet of this ship is " ).Add( relatedSquadOrNull.Planet == null ? "null" : relatedSquadOrNull.GetPlanetName_Safe() ).Add( " instead of planet being viewed.</color>  " );
                        }

                        List<PlannedMetalFlow> flows = relatedSquadOrNull.SquadPlannedFlows.GetDisplayList();
                        if ( flows.Count > 0 )
                        {
                            buffer.Add( "<color=#6bffec>Metal Flows: " ).Add( flows.Count );
                            if ( detailLevel >= TooltipDetail.Full )
                            {
                                for ( int flowIndex = 0; flowIndex < flows.Count; flowIndex++ )
                                {
                                    if ( flowIndex > 0 )
                                        buffer.Add( ", " );
                                    else
                                        buffer.Add( ": " );
                                    PlannedMetalFlow plannedFlow = flows[flowIndex];
                                    if ( plannedFlow.FromEntity == null )
                                    {
                                        buffer.Add( "[null flow]" );
                                        continue;
                                    }
                                    if ( !plannedFlow.IsInRangeAtTheMoment )
                                        buffer.Add( "(Out of Range) " );
                                    switch ( plannedFlow.Purpose )
                                    {
                                        case MetalFlowPurpose.AssistSelfConstruction:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, "Help Build", true );
                                            break;
                                        case MetalFlowPurpose.AssistFactoryConstruction:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, "Assist Factory", true );
                                            break;
                                        case MetalFlowPurpose.ClaimingNeutrals:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, "Claim", true );
                                            break;
                                        case MetalFlowPurpose.RebuildingRemains:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, "Rebuild", true );
                                            break;
                                        case MetalFlowPurpose.RepairingEnginesOfFriendlies:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, "Rep Eng", true );
                                            break;
                                        case MetalFlowPurpose.RepairingHullsOfFriendlies:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, "Rep Hull", true );
                                            break;
                                        case MetalFlowPurpose.RepairingShieldsOfFriendlies:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, "Rep Shld", true );
                                            break;
                                        case MetalFlowPurpose.BuildingDronesInternally:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, "Build Drones", true );
                                            break;
                                        case MetalFlowPurpose.FactoryConstructionForPlayerMobileFleets:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, "Factory Work", true );
                                            break;
                                        case MetalFlowPurpose.SelfConstruction:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, "Self Build", true );
                                            break;
                                        default:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, EnumNameCache.GetName( plannedFlow.Purpose ), true );
                                            break;
                                    }
                                }
                            }
                            buffer.Add( "</color>  " );
                        }
                    }
                    #endregion

                    
                    debugStage = 87398000;

                    #region Codehooks
                    {
                        var codeHook = ArcenExternalCodeHookTable.Instance.GetRowByNameOrNullIfNotFound( "GlobalEntityDescriptionAppender" );
                        if ( codeHook != null )
                            codeHook.HandleAllSubscribedHooks( buffer, relatedSquadOrNull, null, Engine_AIW2.Instance.MainThreadContext_ClientOrHost );
                        //else
                            //ArcenDebugging.ArcenDebugLog( "Could not find GlobalEntityDescriptionAppender", Verbosity.ShowAsError );
                    }
                    #endregion

                    debugStage = 87399000;

                    #region Exo stuff
                    if ( relatedEntityTypeData.ExoGenerationDifficulty > 0 )
                    {
                        AIDifficulty highestDifficulty = FactionUtilityMethods.Instance.GetHighestAIDifficulty_AsDifficulty();
                        if ( highestDifficulty.Difficulty >= relatedEntityTypeData.ExoGenerationDifficulty )
                        {
                            buffer.Add( "如果你占领此建筑，AI 将派出外银河打击部队对付你。" );
                        }
                    }
                    #endregion

                    debugStage = 87401000;

                    if ( relatedEntityTypeData.OnlyCloakedWhenOwningPlanet &&  detailLevel > TooltipDetail.Medium )
                        buffer.Add("此单位仅在其阵营控制该星球时保持隐形。");

                    debugStage = 87402000;

                    #region AIReinforcementPointContents
                    if ( relatedSquadOrNull.AIReinforcementPointContents != null && relatedSquadOrNull.AIReinforcementPointContents.Count > 0 )
                    {
                        int countOfItemTypes = 0;
                        int countOfItems = 0;
                        int strengthOfItems = 0;
                        RefPair<GameEntityTypeData, int> content;
                        for ( int i = 0; i < relatedSquadOrNull.AIReinforcementPointContents.Count; i++ )
                        {
                            content = relatedSquadOrNull.AIReinforcementPointContents[i];
                            if ( content.RightItem > 0 )
                            {
                                countOfItemTypes++;
                                countOfItems += content.RightItem;
                                strengthOfItems += ( content.RightItem * content.LeftItem.MarkStatsFor( relatedSquadOrNull.CurrentMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership );
                            }
                        }
                        if ( countOfItemTypes > 0 )
                        {
                            if ( detailLevel >= TooltipDetail.Medium )
                                buffer.Add( "此 AI 增援点包含 " ).StartColor( QuickColors.NewValue )
                                     .Add( countOfItems ).Add( " 艘舰船</color>，总强度 " );
                            else

                                buffer.Add( "包含 " ).StartColor( QuickColors.NewValue )
                                     .Add( countOfItems ).Add( " 艘舰船</color>，强度 " );
                            buffer.Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon );
                            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, strengthOfItems, true, true );
                            buffer.Add( "</color>." );

                            if ( detailLevel >= TooltipDetail.Medium )
                            {
                                buffer.Add( "  其类型为：" );
                                bool isFirst = true;
                                for ( int i = 0; i < relatedSquadOrNull.AIReinforcementPointContents.Count; i++ )
                                {
                                    content = relatedSquadOrNull.AIReinforcementPointContents[i];
                                    if ( content.RightItem > 0 )
                                    {
                                        if ( isFirst )
                                            isFirst = false;
                                        else
                                            buffer.Add( ", " );
                                        buffer.Add( content.RightItem ).Add( "x " ).StartColor( QuickColors.NewValue ).Add( content.LeftItem.DisplayName ).Add( "</color>" );
                                    }
                                }
                                buffer.Add( "  如果受到干扰，可能会释放它们。  " );
                            }
                        }
                    }
                    #endregion

                    debugStage = 87403000;

                    #region AIReinforcementPointReasonCodesForDebugging
                    if ( relatedSquadOrNull.AIReinforcementPointReasonCodesForDebugging != null && relatedSquadOrNull.AIReinforcementPointReasonCodesForDebugging.Count > 0 )
                    {
                        if ( detailLevel >= TooltipDetail.Medium )
                        {
                            buffer.Add( "增援调试原因代码：" );
                            bool isFirst = true;
                            RefPair<string, int> content;
                            for ( int i = 0; i < relatedSquadOrNull.AIReinforcementPointReasonCodesForDebugging.Count; i++ )
                            {
                                content = relatedSquadOrNull.AIReinforcementPointReasonCodesForDebugging[i];
                                if ( content.RightItem > 0 )
                                {
                                    if ( isFirst )
                                        isFirst = false;
                                    else
                                        buffer.Add( ", " );
                                    buffer.Add( content.RightItem ).Add( "x " ).StartColor( QuickColors.NewValue ).Add( content.LeftItem ).Add( "</color>" );
                                }
                            }
                            buffer.Add( ".  " );
                        }
                    }
                    #endregion

                    #region Factory Things
                    debugStage = 5900;
                    if ( relatedSquadOrNull.TypeData.HasFactoryFlows )
                    {                        
                        if ( relatedSquadOrNull.GetIsCrippled() )
                            buffer.Add( "受损工厂在修复前无法消耗金属。" );
                        else if ( relatedSquadOrNull.GetIsNonFunctional() )
                            buffer.Add( "失效工厂在功能恢复前无法消耗金属。" );
                        else if ( relatedSquadOrNull.ComputeDisabledReason( ArcenRejectionReason.Unknown ) != ArcenRejectionReason.Unknown )
                            buffer.Add( "已禁用的工厂在重新启用前无法消耗金属。" );
                        else
                        {
                            debugStage = 5901;
                            bool foundAnyFleets = false;
                            bool wroteAboutAnyFleets = false;
                            bool hasSupportingFactoriesInRangeEverBeenSet = false;
                            foreach ( Fleet fleet in World_AIW2.Instance.Fleets( FleetStatus.AnyStatus ) )
                            {
                                debugStage = 5910;
                                if ( fleet == null )
                                    break;
                                if ( fleet.IsFleetConstructionPaused )
                                    continue; //are these turned off?
                                if ( fleet.Faction == null || fleet.Faction.Type != FactionType.Player )
                                    continue;
                                //debugStage = 59102;
                                //if ( fleet.Category != FleetCategory.PlayerMobile && fleet.Category != FleetCategory.PlayerCustomMobile )
                                //    continue;
                                debugStage = 59103;
                                if ( fleet.SupportingFactoriesInRange.Count <= 0 )
                                    continue;
                                //no need to check for special type match, because supporting factories in range already does

                                debugStage = 59104;
                                bool foundMyself = false;
                                List<SafeSquadWrapper> supportingFactories = fleet.SupportingFactoriesInRange.GetDisplayList();
                                for ( int i = 0; i < supportingFactories.Count; i++ )
                                {
                                    try
                                    {
                                        debugStage = 59105;
                                        if ( supportingFactories[i].GetPrimaryKeyID() == relatedSquadOrNull.PrimaryKeyID )
                                        {
                                            foundMyself = true;
                                            break;
                                        }
                                    }
                                    catch ( Exception ) { } //cross-threading issues
                                }
                                hasSupportingFactoriesInRangeEverBeenSet = fleet.HasSupportingFactoriesInRangeEverBeenSet;
                                debugStage = 59106;
                                if ( !foundMyself )
                                    continue;

                                foundAnyFleets = true;
                                debugStage = 5911;
                                bool isFirst = true;
                                bool areAllAtShipCap = true;
                                foreach ( FleetMembership mem in fleet.MemberGroupsSorted_NonSim_ForUIOnly_SeriouslyDoNotCallFromBGCode() )
                                {
                                    debugStage = 5912;
                                    if ( mem.TypeData.IsDrone || mem.TypeData.SelfConstructs )
                                        continue;
                                    if ( mem.EffectiveSquadCap <= mem.EntitiesOfFMem.Count )
                                        continue; //skip us if we are at or above cap

                                    if ( mem.GetCanBuildAnother( true, -1, ExtraFromStacks.IncludePrecalc ) != ArcenRejectionReason.Unknown )
                                        continue; //something is blocking us other than cap!

                                    areAllAtShipCap = false;
                                    if ( isFirst )
                                    {
                                        wroteAboutAnyFleets = true;
                                        isFirst = false;
                                        //When display the fleet's name, colour code it so a player can see at a glance how "healthy" that fleet is through this interface
                                        int currentStrength = relatedMemFleetOrNull.GetCurrentStrengthOfFleet_ForUIOnly( relatedMemFleetOrNull.GetFactionType_Safe() == FactionType.NaturalObject );
                                        int maxStrength = relatedMemFleetOrNull.GetMaxStrengthOfFleet_ForUIOnly( relatedMemFleetOrNull.GetFactionType_Safe() == FactionType.NaturalObject );
                                        float ratio = (float)currentStrength/maxStrength;
                                        Color fleetColor = EntityText.GetProportionalStrengthColor(ratio);
                                        buffer.Add( "\n为舰队建造 " ).StartColor( fleetColor ).Add( fleet.GetName() ).EndColor().Add("：  ");
                                    }
                                    else
                                        buffer.Add( ", " );
                                    buffer.Add( mem.TypeData.DisplayName, "c0c0c0" );
                                    buffer.Add( " (x" ).Add( mem.GetRemainingCap( true, -1, ExtraFromStacks.IncludePrecalc ) ).Add( "  " ).StartColor( ColorMath.LightLeafGreen );
                                    int buildPct = (int)((float)mem.MetalSpentConstructingCurrentReplacement / (float)mem.GetMetalCost() * 100f);
                                    buffer.AddPaddedInt( buildPct, 2 );
                                    buffer.Add( "%" ).EndColor();
                                    if ( ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() && GameSettings.Current.GetBoolBySetting( "ShowTooltipProgressBars" ) )
                                        ArcenExternalUIUtilities.AppendBar( buffer, buildPct, ColorMath.LightLeafGreen, 16, 60, ColorMath.DarkRed );
                                    buffer.Add( ")" );

                                    if ( mem.Fleet.Faction.Type == FactionType.Player )
                                    {
                                        if ( mem.TypeData.EnergyUsage > 0 && mem.Fleet.Faction.NetEnergy - mem.GetEnergyUsage() < 0 && mem.GetEnergyUsage() > 0 )
                                            buffer.StartColor( ColorMath.LightRed ).Add( " 能量不足" ).EndColor();
                                    }
                                }

                                if ( isFirst )
                                {
                                    wroteAboutAnyFleets = true;
                                    if ( areAllAtShipCap )
                                        buffer.Add( "\n舰队 " ).StartColor( EntityText.GetProportionalStrengthColor( 1 ) ).Add( fleet.GetName() ).EndColor().Add( " 的所有建造已完成。  " );
                                    else
                                    {
                                        Faction fleetFaction = fleet.Faction;
                                        if ( fleetFaction != null && fleetFaction.NetEnergy <= 0 )
                                            buffer.Add( "\n无法为舰队 " ).StartColor( EntityText.GetProportionalStrengthColor( 1 ) ).Add( fleet.GetName() ).EndColor()
                                            .Add( " 建造，能量不足。  " );
                                        else
                                            buffer.Add( "\n无法为舰队 " ).StartColor( EntityText.GetProportionalStrengthColor( 1 ) ).Add( fleet.GetName() ).EndColor().Add( " 建造，某些舰船线被阻挡。请检查能量可用性。  " );
                                    }
                                }

                            }
                            if ( foundAnyFleets && !wroteAboutAnyFleets )
                                buffer.Add( "一个或多个相关舰队在此工厂范围内，但已达舰船上限，无需行动。" );
                            else if ( !foundAnyFleets )
                            {
                                if ( !hasSupportingFactoriesInRangeEverBeenSet )
                                {
                                    buffer.Add( "你必须短暂取消暂停才能查看支持工厂的信息。" );
                                }
                                else
                                {
                                    if ( relatedEntityTypeData.SpecialFactoryType != null && relatedEntityTypeData.SpecialFactoryType.Length > 0 )
                                        buffer.Add( "没有（未受损的）" ).Add( relatedEntityTypeData.SpecialFactoryType ).Add( " 类型的我方舰队在此星球或邻近星球上，因此无法为任何人建造。" );
                                    else
                                        buffer.Add( "没有（未受损的）我方机动舰队在此或邻近星球上，因此无法为任何人建造。" );
                                }
                            }
                        }               
                    }
                    #endregion

                    debugStage = 5950;
                    var constructionBlockedReason = relatedSquadOrNull.GetIsSelfConstructionBlocked();
                    if ( constructionBlockedReason != ArcenRejectionReason.Unknown )
                    {
                        debugStage = 5951;
                        if ( relatedMemFleetOrNull != null && relatedMemFleetOrNull.Category == FleetCategory.PlayerPlanetaryCommand )
                            buffer.Add( "<color=#ff5842>此星球无完全建造的指挥站则无法建造！</color>  " );
                        else
                            buffer.Add( "<color=#ff5842>旗舰必须在此且未受重创才能建造。</color>  " );

                        if ( GameSettings.Current.GetBoolBySetting( "Debug_ShowConstructionBlockedReason" ) )
                            buffer.Add( "调试：建造被阻止：" ).Add( Extensions.ToString(constructionBlockedReason) ).Add( "  " );
                    }
                    debugStage = 5952;
                }
                else //relatedEntity IS NULL
                {
                    #region Fleet Centerpiece Things For Not-Yet-Existing Fleets
                    debugStage = 6460;
                    if ( relatedEntityTypeData.FleetDesignTemplatesIAlwaysGrant != null && relatedEntityTypeData.FleetDesignTemplatesIAlwaysGrant.Count > 0 )
                    {
                        debugStage = 6461;
                        buffer.Add( "我是将成为以下内容舰队的旗舰：" );

                        debugStage = 6462;
                        for ( int j = 0; j < relatedEntityTypeData.FleetDesignTemplatesIAlwaysGrant.Count; j++ )
                        {
                            FleetDesignTemplate template = relatedEntityTypeData.FleetDesignTemplatesIAlwaysGrant[j];
                            foreach ( FleetItem Item in template.FleetItems() )
                            {
                                if ( Item.TypeData == relatedEntityTypeData )
                                    continue;
                                bool isExcludedSpecialType = false;
                                switch ( Item.TypeData.SpecialType )
                                {
                                    case SpecialEntityType.AICommandStationOriginal:
                                    case SpecialEntityType.AICommandStationReconquest:
                                    case SpecialEntityType.BattlestationBasic:
                                    case SpecialEntityType.BattlestationCitadel:
                                    case SpecialEntityType.CityCenter:
                                    case SpecialEntityType.HumanHomeCommand:
                                    case SpecialEntityType.MobileOfficerCombatFleetFlagship:
                                    case SpecialEntityType.MobileStrikeCombatFleetFlagship:
                                    case SpecialEntityType.MobileCustomUnattachedFleetFlagship:
                                    case SpecialEntityType.MobileSupportFleetFlagship:
                                    case SpecialEntityType.NormalHumanCommandStation:
                                    case SpecialEntityType.NPCFactionCenterpiece:
                                    case SpecialEntityType.WardenSecretNinjaHideout:
                                        isExcludedSpecialType = true;
                                        break;
                                }
                                if ( isExcludedSpecialType )
                                    continue;


                                buffer.Add( Item.TypeData == null ? "nulltype" : Item.TypeData.DisplayName ).Add( " " );
                                //Chris says: these are theoretical adds, so this is the base cap and that gets marked up.  This is okay.
                                int CapToShow = Item.TypeData.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( Item.TypeData, Item.Cap, Item.Cap, markStats.MarkLevel );

                                buffer.StartColor( QuickColors.NewValue );
                                buffer.Add( " x" ).Add( CapToShow );
                                buffer.EndColor();

                                buffer.Add( "  " );
                            }
                        }
                    }
                    #endregion
                }
                debugStage = 6470;
                if ( relatedEntityTypeData.ExtraSocketsGranted > 0 )
                {
                    buffer.Add("提供 ").Add( relatedEntityTypeData.ExtraSocketsGranted, "dd33dd" ).Add( " 个额外建造插槽（如果你拥有此建筑）。");
                }

                if ( relatedEntityTypeData.CitySocketCost > 0 && panelMode == Mode.Build )
                {
                    debugStage = 6480;

                    buffer.Add( relatedEntityTypeData.NameForCitySockets_Plural ).Add( " 需要：" ).Add( relatedEntityTypeData.CitySocketCost );
                    if (relatedMemFleetOrNull != null) {
                        buffer.Add( "（共 " ).Add( relatedMemFleetOrNull.CalculateRemainingCitySockets() ).Add( " 可用）");
                    }
                    buffer.Add(".  " );
                    if ( relatedEntityTypeData.MinimumRequiredCityLevelForConstruction > 1 )
                    {
                        debugStage = 6490;
                        buffer.Add("必须达到标记等级 " ).Add( relatedEntityTypeData.MinimumRequiredCityLevelForConstruction, "a1ffa1").Add(" 才能建造此建筑。");
                    }

                }
                debugStage = 6500;
                //only tell about added points if they come from not-the-hub
                if ( relatedMarkLevelData.CitySockets > 0 )
                {
                    debugStage = 6510;
                    buffer.Add( relatedEntityTypeData.NameForCitySockets_Plural ).Add( " 提供：" ).Add( relatedMarkLevelData.CitySockets ).Add( "。  " );
                }

                if ( relatedEntityTypeData != null &&
                     relatedEntityTypeData.ShipTypeNameToGrantMoreOfInCustomFleet != string.Empty )
                {
                    debugStage = 6520;
                    GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRowByName( relatedEntityTypeData.ShipTypeNameToGrantMoreOfInCustomFleet );
                    if ( typeData == null )
                        throw new Exception("Could not find row with name " + relatedEntityTypeData.ShipTypeNameToGrantMoreOfInCustomFleet );
                    buffer.Add("</color>此建筑将授予你 ").Add( relatedEntityTypeData.ShipTypeCountToGrantMoreOfInCustomFleet, "ffa1a1" ).Add(" ").Add(typeData.GetDisplayName(), "a1ffa1");
                    if ( detailLevel >= TooltipDetail.Medium || DetailFlags.HasFlag(ShipExtraDetailFlags.AnyGrantHackInfo) )
                    {
                        debugStage = 6530;
                        buffer.Add(": ").NewLine();
                        byte markLevelToUse = localPlayerFactionOrNull?.GetGlobalMarkLevelForShipLine( typeData ) ?? 1;
                        EntityText.GetTooltip( buffer, null, null,
                             typeData, -1, null, markLevelToUse, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None, PositionScaleMultiplier, IsBeingDrawnInPopupWindowRatherThanTooltip );
                    }
                    else
                        buffer.Add(". ");

                }
                
                buffer.Add(FontSizes.MUCH_SMALLER_SIZE_STRING);

                if ( !relatedSquadOrNull.IsFakeEntity )
                {
                    debugStage = 5450;
                    if ( relatedSquadOrNull.ExtraStackedSquadsInThis > 0 && detailLevel >= TooltipDetail.Medium )
                    {
                        buffer.StartColor( QuickColors.HeaderDark ).Add( "这是堆叠 " ).StartColor( QuickColors.NewValue )
                            .Add( relatedSquadOrNull.ExtraStackedSquadsInThis + 1 ).Add( " 艘舰船</color>。当前单位死亡后，计数减少并弹出下一单位。  " );
                        if ( detailLevel >= TooltipDetail.Full )
                        {
                            int extraCount = 1 + relatedSquadOrNull.ExtraStackedSquadsInThis;
                            buffer.Add( "此堆叠正常承受伤害，但发射 " ).StartColor( QuickColors.OldValue ).Add( extraCount ).Add( "倍</color> 的正常射击量。" );
                        }
                        buffer.EndColor();

                    }
                }

                debugStage = 5500;

                if ( !relatedSquadOrNull.IsFakeEntity && detailLevel >= TooltipDetail.SuperShort )
                {
                    debugStage = 5501;
                    if ( relatedSquadOrNull.TypeData.IsDefault &&
                        relatedSquadOrNull.CalculateShouldOrderBeIgnoredByFlagshipAsIfWasStationaryMode( false ) && Engine_AIW2.Instance.CurrentGameViewMode != GameViewMode.GalaxyMapView )
                    {
                        debugStage = 5502;
                        buffer.StartColor( "ee3198" ).Add( "固定旗舰模式！  " );
                        if ( detailLevel >= TooltipDetail.Medium )
                        {
                            debugStage = 5503;
                            buffer.Add( "按住 " ).Add( InputActionTypeDataTable.GetActionByName_FairlySlow( "HoldToGiveOrdersToStationaryFlagships" ).GetHumanReadableKeyCombo() )
                                .Add( " 使此旗舰听取命令。否则，舰队其余单位执行命令，旗舰持有命令，从旗舰出现的新舰船继承这些命令。  " );
                            if ( detailLevel >= TooltipDetail.Full )
                            {
                                buffer.Add( "你可以在舰队侧边栏页签中了解更多信息并更改其模式。找到此旗舰的舰队并点击它。" );
                            }
                        }
                        buffer.EndColor();
                    }
                }
                debugStage = 5504;
                if ( !relatedSquadOrNull.IsFakeEntity && orders != null && relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player && detailLevel >= TooltipDetail.Medium )
                {
                    debugStage = 5505;
                    if ( relatedSquadOrNull.StopToShootAnySeenTargets )
                        buffer.StartColor( Window_InGameSelectionInfo.color_StopToShoot ).Add( "停射模式！  " ).EndColor();
                    if ( relatedSquadOrNull.SpeedLimitFromGroupMove > 0 )
                        buffer.StartColor( Window_InGameSelectionInfo.color_GroupMove ).Add( "编队移动模式！速度：" ).Add( relatedSquadOrNull.SpeedLimitFromGroupMove ).Add( "  " ).EndColor();
                    if ( relatedSquadOrNull.PreferredEntityTypeDataForTargeting != null )
                        buffer.StartColor( Window_InGameSelectionInfo.color_AttackMove ).Add( "优先目标：" ).Add( relatedSquadOrNull.PreferredEntityTypeDataForTargeting.GetDisplayName() ).Add( "  " ).EndColor();
                }
                debugStage = 5510;
                if ( showDebugInfoInTooltip && !relatedSquadOrNull.IsFakeEntity && relatedSquadOrNull.DecollisionMoveTarget != ArcenPoint.ZeroZeroPoint && !isForMultipleUnits && detailLevel >= TooltipDetail.Medium )
                {
                    #region Write Decollision Move Logic
                    buffer.StartColor( QuickColors.HeaderDull ).Add( "Decollision Detour To: " );
                    if ( relatedSquadOrNull.DecollisionMoveTarget == ArcenPoint.OutOfRange )
                        buffer.Add( "BUG!  OutOfRange!" );
                    else
                    {
                        ArcenPoint targetPoint = relatedSquadOrNull.DecollisionMoveTarget;
                        targetPoint -= Engine_AIW2.Instance.CombatCenter;
                        buffer.Add( targetPoint.X ).Add( "," ).Add( targetPoint.Y );
                    }
                    buffer.EndColor();
                    buffer.Add( "  " );
                    #endregion
                }
                debugStage = 5520;

                debugStage = 5530;
                if ( showDebugInfoInTooltip && !relatedSquadOrNull.IsFakeEntity )
                {
                    EntitySystem system;
                    GameEntity_Squad targetEntity;
                    for ( int i = 0; i < relatedSquadOrNull.Systems.Count; i++ )
                    {
                        system = relatedSquadOrNull.Systems[i];
                        if ( system == null || system.TypeData.Category != EntitySystemCategory.Weapon )
                            continue;
                        buffer.StartColor( system.CountOfPotentialTargetsByPriorityForSystem() == 0 ? 
                            QuickColors.OldValue : QuickColors.NewValue ).Add( "targetPriorityList for " ).Add( system.TypeData.DisplayName ).Add( ": " );
                        if ( system.CountOfPotentialTargetsByPriorityForSystem() == 0 )
                        {
                            buffer.Add( "targetPriorityList is empty!" );
                            buffer.EndColor();
                            continue;
                        }
                        buffer.Add( "targetPriorityList.Count: " ).Add( system.CountOfPotentialTargetsByPriorityForSystem() ).Add(". ");
                        foreach ( LazyLoadSquadWrapper Wrapper in system.PotentialTargetsByPriorityForSystem() )
                        {
                            targetEntity = Wrapper.GetSquad();
                            if ( targetEntity == null )
                                continue;
                            buffer.Add( targetEntity.TypeData.GetDisplayName() );

                            //buffer.Add( " (#" ).Add( targetEntity.TargetingInfo_Player.Importance ).Add( ") ");
                            if ( targetEntity.Planet != relatedSquadOrNull.Planet )
                                buffer.Add( " (Planet: " ).Add( targetEntity.Planet == null ? "null" : targetEntity.GetPlanetName_Safe() ).Add( ")" );
                            buffer.Add( "   " );
                        }
                        buffer.Add( "  " );
                        buffer.EndColor();
                    }
                    if ( Engine_Universal.DoDeepLoggingOnObjectHistories ) {
                        buffer.NewLine();
                        buffer.Add("Squad Faction ReasonCode: ").Add(relatedSquadOrNull.LastReasonsForPlanetFactionChange.GetAllContentsAsCommaSeparatedStringForDebugging());
                        buffer.Add("\nSquad Planet ReasonCode: ").Add(relatedSquadOrNull.LastReasonsForPlanetChange.GetAllContentsAsCommaSeparatedStringForDebugging());
                        buffer.Add("\nSquad Fleet Squad Fleet ReasonCode List: ").Add(relatedSquadOrNull.LastReasonsForFleetMembershipChange.GetAllContentsAsCommaSeparatedStringForDebugging());
                        buffer.Add("\nSquad Pool status history: ").Add(relatedSquadOrNull.Debug_GetCondensedHistoryOfPoolStatus());
                        buffer.NewLine();
                    }
                
                    debugStage = 5540;
                    //buffer.Add( "ShowShipOrders:" ).Add( ArcenInput_AIW2.ShouldShowShipOrders );
                    if ( detailLevel >= TooltipDetail.Full && !isForMultipleUnits )//&& ArcenInput_AIW2.ShouldShowShipOrders )
                    {
                        int autoTargetListedSoFar = 0;
                        bool hasDoneFirstWeaponAutotargeting = false;
                        for ( int i = 0; i < relatedSquadOrNull.Systems.Count; i++ )
                        {
                            system = relatedSquadOrNull.Systems[i];
                            if ( system == null || system.TypeData.Category != EntitySystemCategory.Weapon )
                                continue;
                            if ( system.TypeData.IsModule && !system.TypeData.IsModuleOn( relatedSquadOrNull.FleetMembership ) )
                                continue; //skip any that are not enabled
                            if ( !system.TypeData.ForMark[effectiveMarkLevel].IsFunctionalAtThisMarkLevel )
                                continue;

                            if ( !hasDoneFirstWeaponAutotargeting )
                            {
                                hasDoneFirstWeaponAutotargeting = true;
                                char lastChar = buffer.GetLastWrittenCharacterIfNotPartOfInt_DoNotCallRepeatedly();
                                if ( lastChar != '\n' )
                                    buffer.Add( "\n" );
                                buffer.StartColor( QuickColors.OldValue ).Add( "Autotargeting Per Weapon (" ).Add( Extensions.ToString(relatedSquadOrNull.GetFactionTargetPlanningGroupSafe()) ).Add( "):" ).EndColor();
                            }

                            buffer.StartColor( QuickColors.OldValue ).Add( "\n" ).Add( system.TypeData.DisplayName );
                            buffer.Add( " (" ).Add( system.CountOfPotentialTargetsByPriorityForSystem() ).Add( "): " );

                            if ( system.CountOfPotentialTargetsByPriorityForSystem() == 0 )
                            {
                                buffer.EndColor();
                                continue;
                            }

                            bool hasHadAnyInRangeYet = false;
                            bool needsToWriteEtc = false;
                            foreach ( LazyLoadSquadWrapper Wrapper in system.PotentialTargetsByPriorityForSystem() )
                            {
                                targetEntity = Wrapper.GetSquad();
                                if ( targetEntity == null )
                                    continue;
                                autoTargetListedSoFar++;
                                bool shouldDrawNoMatterWhat = true;
                                if ( autoTargetListedSoFar >= 6 )
                                {
                                    needsToWriteEtc = true;
                                    shouldDrawNoMatterWhat = false;
                                    if ( hasHadAnyInRangeYet )
                                        break;
                                }
                                bool isOnCorrectPlanet = targetEntity.Planet == relatedSquadOrNull.Planet;
                                bool isInRange = !isOnCorrectPlanet ? false : system.GetIsTargetInRange( targetEntity, RangeCheckType.ForActualFiring );
                                if ( isInRange )
                                    hasHadAnyInRangeYet = true;

                                if ( shouldDrawNoMatterWhat || isInRange )
                                {
                                    if ( autoTargetListedSoFar > 1 )
                                        buffer.Add( ", " );
                                    buffer.Add( targetEntity.TypeData.DisplayNameForSidebar );

                                    if ( !isOnCorrectPlanet )
                                        buffer.Add( " (Planet: " ).Add( targetEntity.Planet == null ? "null" : targetEntity.GetPlanetName_Safe() ).Add( ")" );
                                    else if ( isInRange )
                                        buffer.Add( " (In Range)" );
                                }
                            }

                            if ( needsToWriteEtc )
                            {
                                buffer.Add( ", etc." );
                            }
                            if ( !hasHadAnyInRangeYet )
                                buffer.Add( " (None In Range)" );
                        }
                        buffer.EndColor();
                    }

                    
                    {
                        int estimatedRemainingDurability = relatedSquadOrNull.EstimateRemainingDurabilityAfterAllShots( 0 );
                        int originalDurability = relatedSquadOrNull.GetAbsoluteDurabilityOfMyselfAndStack();
                        if ( estimatedRemainingDurability < originalDurability )
                        {
                            int damage = (originalDurability - estimatedRemainingDurability);
                            buffer.StartColor( QuickColors.OldValue ).Add( "\n预期受到伤害：" ).AddNumberMoreReadable( damage ).EndColor().Add( "  " );
                            if ( estimatedRemainingDurability <= 0 )
                                buffer.StartColor( QuickColors.OldValue ).Add( "（预期死亡）" ).EndColor().Add( "  " );
                        }
                        else
                        {
                            int incomingShotCount = relatedSquadOrNull.GetIncomingShotCount();
                            if ( incomingShotCount > 0 )
                                buffer.StartColor( QuickColors.OldValue ).Add( "\nIncoming Shots: " ).AddNumberMoreReadable( incomingShotCount ).Add( " (But Their Calculated Damage Is Zero??)  " ).EndColor();
                        }
                    }
                }

                //Now to any status effects
                //--------------------------------------

                debugStage = 6000;
                
                
                //we must be talking about a specific entity, now
                if ( !relatedSquadOrNull.IsFakeEntity && !isForMultipleUnits )
                {
                    /*
                    //bool haveDoneNewLine = false;
                    if ( relatedSquadOrNull.CurrentEngineStunSeconds > 0 )
                    {
                        //HandleNewline( buffer, ref haveDoneNewLine );

                        if ( detailLevel < TooltipDetail.Full )
                            buffer.StartColor( QuickColors.OldValue ).Add( "Engine-stunned for " ).Add( 
                            relatedSquadOrNull.CurrentEngineStunSeconds ).Add( " more seconds" ).EndColor().Add( ", speed reduced to " )
                            .StartColor( QuickColors.OldValue ).AddNumberMoreReadable( relatedSquadOrNull.CalculatedSpeed ).EndColor().Add( " for now.  " );
                        else
                            buffer.StartColor( QuickColors.OldValue ).Add( "This ship is engine-stunned for " ).Add(
                            relatedSquadOrNull.CurrentEngineStunSeconds ).Add( " more seconds" ).EndColor().Add( ", thus having its speed reduced to " )
                            .StartColor( QuickColors.OldValue ).AddNumberMoreReadable( relatedSquadOrNull.CalculatedSpeed ).EndColor().Add( " for now.  " );
                    }
                    debugStage = 6010;

                    FInt grav = relatedSquadOrNull.CurrentGravitySpeedMultiplier.Display;
                    if ( grav > FInt.Zero && grav != FInt.One )
                    {
                        //HandleNewline( buffer, ref haveDoneNewLine );

                        if ( detailLevel < TooltipDetail.Full )
                            buffer.StartColor( QuickColors.OldValue ).Add( "In gravity field(s), speed reduced to " ).Add(
                            grav.ReadableString ).Add( "x normal" ).EndColor().Add( ", which is " )
                            .StartColor( QuickColors.OldValue ).AddNumberMoreReadable( relatedSquadOrNull.CalculatedSpeed ).EndColor().Add( " for now.  " );
                        else
                            buffer.StartColor( QuickColors.OldValue ).Add( "This ship is in one or more gravity fields, which combine to reduce its speed to " ).Add(
                            grav.ReadableString ).Add( "x normal" ).EndColor().Add( ", thus having its total speed reduced to " )
                            .StartColor( QuickColors.OldValue ).AddNumberMoreReadable( relatedSquadOrNull.CalculatedSpeed ).EndColor().Add( " for now.  " );
                    }*/
                    
                    debugStage = 6015;
                        
                    if ( relatedSquadOrNull.IsBlackHoledAtMoment.Display )
                    {
                    if ( detailLevel < TooltipDetail.Full )
                        buffer.Add( " 因黑洞机器而无法离开星球。 " );
                    else
                        buffer.Add( " 此舰船受到黑洞机器影响而无法离开星球。你必须摧毁黑洞机器才能逃脱。 " );
                    }

                    debugStage = 6020;

                    /*
                    if ( relatedSquadOrNull.CurrentParalysisSeconds > 0 )
                    {
                        //HandleNewline( buffer, ref haveDoneNewLine );
                        if ( detailLevel < TooltipDetail.Full )
                            buffer.StartColor( QuickColors.OldValue ).Add( "Paralyzed for " ).Add(
                            relatedSquadOrNull.CurrentParalysisSeconds ).Add( " more seconds" ).Add( ".  " ).EndColor();
                        else
                            buffer.StartColor( QuickColors.OldValue ).Add( "This ship is completely paralyzed for " ).Add(
                            relatedSquadOrNull.CurrentParalysisSeconds ).Add( " more seconds" ).Add( ", making it completely useless for now.  " ).EndColor();
                    }*/

                    debugStage = 6022;

                    if ( relatedSquadOrNull.IsBlackHoledAtMoment.Display )
                    {
                        buffer.StartColor( QuickColors.OldValue ).Add( "黑洞化：无法离开此星球" ).EndColor();
                    }
                    debugStage = 6030;

                    /*
                    if ( relatedSquadOrNull.CurrentCountOfTractorsPullingOnThis > 0 )
                    {
                        //HandleNewline( buffer, ref haveDoneNewLine );
                        if ( detailLevel < TooltipDetail.Full )
                            buffer.StartColor( QuickColors.OldValue ).Add( "Caught in tractor field and unable to move.  " ).EndColor();
                        else
                        {
                            buffer.StartColor( QuickColors.OldValue ).Add( "This ship is currently caught in a tractor field that is preventing it from moving.  " );

                            buffer.EndColor();
                        }
                    }

                    debugStage = 6040;

                    if ( relatedSquadOrNull.CurrentWeaponAddedReloadSeconds > 0 )
                    {
                        //HandleNewline( buffer, ref haveDoneNewLine );

                        buffer.StartColor( QuickColors.OldValue ).Add( "All weapons on this ship currently take an extra " ).Add(
                            relatedSquadOrNull.CurrentWeaponAddedReloadSeconds ).Add( " seconds to reload per salvo.  " ).EndColor();
                    }

                    debugStage = 6045;

                    if ( (relatedSquadOrNull.ProtectingShields_ReduceDamage.Count > 0 || relatedSquadOrNull.GetIsSelfEmittingProtectingShield_ReduceDamage()) &&
                        !hasSystemWithBonusFromAttackingUnderForcefields)
                    {
                        //HandleNewline( buffer, ref haveDoneNewLine );

                        if ( detailLevel >= TooltipDetail.Medium )
                            buffer.StartColor( QuickColors.OldValue ).Add( "Deals half normal damage due to firing from under forcefield.  " ).EndColor();
                    }
                    else
                    {
                        if ( relatedSquadOrNull.GetIsProtectedByAnyForcefield() || 
                            relatedSquadOrNull.GetIsSelfEmittingProtectingShieldNoDamageReduction() )
                        {
                            if ( detailLevel >= TooltipDetail.Medium )
                                buffer.StartColor( QuickColors.NewValue ).Add( "Under forcefield.  " ).EndColor();
                        }
                    }*/

                    if ( relatedSquadOrNull.AreaBoosting_CurrentCount.Display > 0 )
                        buffer.StartColor( QuickColors.NewValue ).Add( "正在增强/保护 " ).Add( relatedSquadOrNull.AreaBoosting_CurrentCount.Display ).Add( " 个单位。  " ).EndColor();

                    if ( relatedSquadOrNull.AreaBoosters_ShotEvaluated.Count > 0 /*|| relatedSquadOrNull.AreaBoosters_NotShotEvaluated.Count > 0*/ )
                    {
                        buffer.StartColor( QuickColors.NewValue ).Add( "受到以下单位的增强/保护：" );
                        List<GameEntity_Squad> protectors = relatedSquadOrNull.AreaBoosters_ShotEvaluated.GetDisplayList();
                        bool isFirst = true;
                        foreach ( GameEntity_Squad squad in protectors )
                        {
                            if ( isFirst )
                                isFirst = false;
                            else
                                buffer.Add( ", " );
                            buffer.Add( squad.TypeData.DisplayName );
                        }

                        // jcf: unused, but could be
                        /*
                        protectors = relatedSquadOrNull.AreaBoosters_NotShotEvaluated.GetDisplayList();
                        foreach ( GameEntity_Squad squad in protectors )
                        {
                            if ( isFirst )
                                isFirst = false;
                            else
                                buffer.Add( ", " );
                            buffer.Add( squad.TypeData.DisplayName );
                        }
                        */
                        buffer.Add( "  " ).EndColor();
                    }

                    debugStage = 6046;
                    if ( GameSettings.Current.GetBoolBySetting( "Debug_ShowShipCoordinates" ) && !relatedSquadOrNull.IsFakeEntity)
                    {
                        buffer.Add("Current ship coordinates: ").Add( relatedSquadOrNull.WorldLocation ).Add(".");
                        if ( relatedSquadOrNull.GuardOrPatrolOffsetPoints.Count == 0 )
                            buffer.Add(" No Guarding Offsets.");
                        else
                            buffer.Add(" The first of ").Add( relatedSquadOrNull.GuardOrPatrolOffsetPoints.Count ).Add(" offsets is ").Add( relatedSquadOrNull.GuardOrPatrolOffsetPoints[0] );
                    }
                    debugStage = 6050;

                    if ( detailLevel >= TooltipDetail.Full && relatedSquadPlanetFactionOrNull != null && relatedSquadPlanetFactionOrNull.GetIsLocalFaction() && !relatedSquadOrNull.TypeData.IsFleetLeader && 
                        relatedSquadOrNull.TypeData.FleetMembershipStyle != FleetMembershipStyle.Planetary )
                    {
                        CannotTransportReason noTransportBase = relatedSquadOrNull.TypeData.GetCanBeTransported();
                        if ( noTransportBase != CannotTransportReason.TranportingIsFine )
                        {
                            //Don't bother telling me about that, actually.  buffer.StartColor( QuickColors.HeaderDull ).Add( "This ship can never be transported.  " ).EndColor();
                        }
                        else
                        {
                            CannotTransportReason noTransport = relatedSquadOrNull.GetCanBeTransportedRightNow();
                            if ( noTransport != CannotTransportReason.TranportingIsFine )
                            {
                                //HandleNewline( buffer, ref haveDoneNewLine );

                                buffer.StartColor( QuickColors.OldValue ).Add( "此舰船通常可被运输，但目前无法装入任何运输船，因为 " ).Add(
                                    EnumNameCache.GetName( noTransport ) ).Add( "。  " ).EndColor();
                            }
                        }
                    }

                    debugStage = 8000;

                    ArcenRejectionReason rejectionReason = relatedSquadOrNull.ComputeDisabledReason( ArcenRejectionReason.Unknown );
                    bool alreadyWroteCrippledInfo = false;
                    if ( rejectionReason != ArcenRejectionReason.Unknown )
                    {
                        bool skipBecauseWrittenElsewhere = false;
                        switch ( rejectionReason )
                        {
                            case ArcenRejectionReason.EntityIsParalyzed:
                            case ArcenRejectionReason.EntityIsUnrebuiltRemains:
                                skipBecauseWrittenElsewhere = true;
                                break;
                        }
                        if ( !skipBecauseWrittenElsewhere )
                        {
                            //HandleNewline( buffer, ref haveDoneNewLine );
                            buffer.StartColor( QuickColors.OldValue ).Add( "舰船已禁用：" );

                            switch ( rejectionReason )
                            {
                                case ArcenRejectionReason.CrippledInseadOfDead:
                                    WriteCrippledInfo( buffer, relatedSquadOrNull );
                                    alreadyWroteCrippledInfo = true;
                                    break;
                                case ArcenRejectionReason.NonFunctionalWhenNotOnPlanetOwnedByMyFaction:
                                    buffer.Add( "失效 - 所属阵营必须控制此星球！" );
                                    break;
                                case ArcenRejectionReason.InUnexploredSpace:
                                    buffer.Add( "在未探索空间 - 没有侦察兵到过此地则无法运作！" );
                                    break;
                                //case ArcenRejectionReason.EntityConsideredOutOfSupplyOfParentFleetCenterpiece:
                                //    buffer.Add( "No supply - not on same or adjacent parent to the fleet centerpiece." );
                                //    break;
                                case ArcenRejectionReason.EntityHasNotYetBeenFullyClaimed:
                                    if ( relatedSquadOrNull.IsInHoldFireMode )
                                        buffer.Add( "尚未完全占领，且已暂停，因此不会被占领。" );
                                    else
                                        buffer.Add( "尚未完全占领。" );
                                    break;
                                case ArcenRejectionReason.EntityIsInHoldFireMode:
                                    if ( relatedEntityTypeData.IsCombatant )
                                        buffer.Add( "停火模式。" );
                                    else
                                        buffer.Add( "功能暂停模式。" );
                                    break;
                                case ArcenRejectionReason.EntityIsSelfBuilding:
                                    {
                                        float percent = ( 1f - ( (float)relatedSquadOrNull.SelfBuildingMetalRemaining / (float)relatedSquadOrNull.GetMetalCost() ) ) * 100;
                                        buffer.Add( "仍在建造中（" );
                                        buffer.AddFixedDecimalThousands( percent, 1 );
                                        //buffer.Add( " " ).AddFixedDecimalThousands( relatedSquadOrNull.SelfBuildingMetalRemaining.ToFloatNonSim(), 1 ).Add( " metal left" );
                                        buffer.Add( "%)" );
                                        if ( relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player )
                                        {
                                            Faction facOrNull = relatedSquadOrNull.GetFactionOrNull_Safe();
                                            if ( relatedSquadOrNull.TypeData.EnergyUsage > 0 && (facOrNull != null && facOrNull.NetEnergy < 0 ) &&
                                                 relatedSquadOrNull.GetEnergyUsage() > 0 )
                                                buffer.StartColor( ColorMath.LightRed ).Add( " ENERGY SHORTAGE" ).EndColor();
                                        }
                                        buffer.Add( "." );
                                    }
                                    break;
                                //case ArcenRejectionReason.EntityIsUnrebuiltRemains:
                                //    buffer.Add( "Was destroyed and not yet rebuilt." );
                                //    break;
                                case ArcenRejectionReason.FactionDoesNotControlThisPlanet:
                                    buffer.Add( "阵营未控制此星球。" );
                                    break;
                                case ArcenRejectionReason.FactionDoesNotHaveEnoughEnergy:
                                    buffer.Add( "阵营能量不足。" );
                                    break;
                                case ArcenRejectionReason.NotEnoughCitySockets:
                                    buffer.Add( "不足" ).Add( relatedSquadOrNull.TypeData.NameForCitySockets_Plural ).Add( "，在此星球。" );
                                    break;
                                case ArcenRejectionReason.MetalIsZero:
                                    buffer.Add( "存储金属为零。" );
                                    break;
                                default:
                                    buffer.Add( "ERROR_WRITE_NICE_TEXT: " ).Add( EnumNameCache.GetName( rejectionReason ) );
                                    break;
                            }

                            buffer.EndColor();
                            buffer.Add( "  " );
                        }
                        else
                        {
                        }
                    }

                    if ( !alreadyWroteCrippledInfo && relatedSquadOrNull.GetIsCrippled() )
                        WriteCrippledInfo( buffer, relatedSquadOrNull );

                    debugStage = 8050;

                    if ( !relatedSquadOrNull.IsFakeEntity && relatedSquadOrNull.SecondsSpentAsRemains >= 0 )
                    {
                        rejectionReason = relatedSquadOrNull.GetCannotRebuildRemainsReason();
                        if ( rejectionReason != ArcenRejectionReason.Unknown )
                        {//HandleNewline( buffer, ref haveDoneNewLine );
                            buffer.StartColor( QuickColors.OldValue ).Add( "无法重建残骸：" );

                            switch ( rejectionReason )
                            {
                                case ArcenRejectionReason.CannotRebuild_IsNotremains:
                                    buffer.Add( "不是残骸！" );
                                    break;
                                case ArcenRejectionReason.CannotRebuild_YesEnemiesHereAndNotBeenLongEnough:
                                    buffer.Add( "必须等待 " )
                                        .AddHoursAndMinutes( ExternalConstants.Instance.Balance_SecondsAfterDeathBeforeRebuilding - relatedSquadOrNull.SecondsSpentAsRemains )
                                        .Add( "，因为敌人仍在此星球。" );
                                    break;
                                case ArcenRejectionReason.CannotRebuild_NoEnemiesHereAndNotBeenLongEnough:
                                    buffer.Add( "只需再等待 " )
                                        .AddHoursAndMinutes( ExternalConstants.Instance.Balance_SecondsAfterDeathBeforeRebuildingNoEnemies - relatedSquadOrNull.SecondsSpentAsRemains )
                                        .Add( "，因为无敌人存在。" );
                                    break;
                                case ArcenRejectionReason.CannotRebuild_CommandStationOnAIPlanet:
                                    buffer.Add( "指挥站不能在敌方星球重建。" );
                                    break;
                                case ArcenRejectionReason.CannotRebuild_WouldPutUsIntoBrownout:
                                    buffer.Add( "重建会导致能量为负。" );
                                    break;
                                default:
                                    buffer.Add( "ERROR_WRITE_NICE_TEXT: " ).Add( EnumNameCache.GetName( rejectionReason ) );
                                    break;
                            }
                            buffer.EndColor();
                            buffer.Add( "  " );
                        }
                    }

                    debugStage = 8100;

                    {
                        Faction facOrNull = relatedSquadOrNull.GetFactionOrNull_Safe();
                        if ( relatedSquadOrNull.GetHasBubbleForcefieldRightNow() && relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player &&
                            facOrNull != null && facOrNull.SecondsSinceBrownout >= 0 )
                        {
                            buffer.StartColor( QuickColors.OldValue );
                            if ( detailLevel < TooltipDetail.Full )
                                buffer.Add( "电力不足：气泡力场将在 " ).Add(
                                            ExternalConstants.Instance.SecondsToWaitBeforeBrownoutEnds - facOrNull.SecondsSinceBrownout ).Add( "秒。" );
                            else
                                buffer.Add( "电力不足：能量平衡为负！你的气泡力场将在 " ).Add(
                                        ExternalConstants.Instance.SecondsToWaitBeforeBrownoutEnds - facOrNull.SecondsSinceBrownout ).Add( " seconds.  " );
                            buffer.EndColor();
                        }
                    }

                    debugStage = 8200;

                    int secondsUntilClaim = localPlayerPlanetFactionOrNull == null ? 0 : relatedSquadOrNull.GetRemainingSecondsBeforeCanClaim( localPlayerPlanetFactionOrNull );
                    if ( secondsUntilClaim > 0 )
                    {
                        buffer.StartColor( QuickColors.OldValue )
                            .Add( "无法被占领，还需要 " ).AddHoursAndMinutes( secondsUntilClaim ).Add( "。这取决于敌方存在以及上次被摧毁后经过的时间。  " );
                        buffer.EndColor();
                    }

                    debugStage = 8300;

                    if ( relatedSquadOrNull.HasNotYetBeenFullyClaimed && localPlayerFactionOrNull != null &&
                         relatedSquadOrNull.TypeData.EnergyUsage > 0 && localPlayerFactionOrNull.NetEnergy - relatedSquadOrNull.TypeData.EnergyUsage < 0 )
                    {
                        buffer.StartColor( QuickColors.OldValue )
                            .Add( "无法认领，因为你需要 " ).Add( -(localPlayerFactionOrNull.NetEnergy - relatedSquadOrNull.TypeData.EnergyUsage ) ).Add( " 更多能量来运行此实体。  " );
                        buffer.EndColor();
                    }

                    debugStage = 8400;

                    if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.MainGameView && GameSettings.Current.GetBoolBySetting( "Debug_ShowLODInfoInShipTooltips" ) )
                    {
                        Planet planet = relatedSquadOrNull.Planet;
                        if ( relatedSquadOrNull.InstancedRenderer == null )
                        {
                            buffer.StartColor( QuickColors.OldValue )
                            .Add( "No InstancedRenderer on this ship.  " );
                            buffer.EndColor();
                        }
                        else if ( planet == null )
                        {
                            buffer.StartColor( QuickColors.OldValue )
                              .Add( "No planet related to this ship?  " );
                            buffer.EndColor();
                        }
                        else
                        {
                            float distanceFromCamera;
                            int currentLOD = relatedSquadOrNull.InstancedRenderer.GetCurrentLODAndLastDistanceFromCamera( out distanceFromCamera );

                            float lodDivisor = planet.GravWellSize.GeneralMultiplier;
                            bool hasLODDivisor = false;
                            if ( Math.Round( lodDivisor, 1 ) != 1f )
                            {
                                hasLODDivisor = true;
                                buffer.Add( "\n<pos=40><color=#c79462>Current Visual Scale Is Offset:</color> " ).Add( 1 / lodDivisor ).Add( "x<pos=350><color=#c79462>Do NOT set LOD values in xml from this planet!</color>" );
                            }
                            else
                                lodDivisor = 1f;

                            buffer.Add( "\n<pos=40><color=#99aa99>Current LOD:</color> " ).Add( currentLOD ).Add( "<pos=350><color=#99aa99>Distance From Camera:</color> " ).AddFixedDecimalThousands( distanceFromCamera, 1 );
                            if ( relatedEntityTypeData.LODDistancesFromVis == null )
                            {
                                buffer.Add( "\n<pos=40>LODDistancesFromVis Not Set For Some Reason!" );
                            }
                            else if ( relatedEntityTypeData.LODMeshesFromVis == null )
                            {
                                buffer.Add( "\n<pos=40>LODMeshesFromVis Not Set For Some Reason!" );
                            }
                            else
                            {
                                if ( relatedEntityTypeData.LODDistancesFromVis.Length != relatedEntityTypeData.LODMeshesFromVis.Length )
                                    buffer.Add( "\n<pos=40>LODDistancesFromVis.Length != LODMeshesFromVis.Length!" );

                                for ( int i = 0; i < relatedEntityTypeData.LODDistancesFromVis.Length; i++ )
                                {
                                    buffer.Add( "\n<pos=40><color=#aaaaaa>LOD" ).Add( i ).Add( " Lasts Until Dist:</color> " );
                                    if ( i < relatedEntityTypeData.LODDistanceOverrides.Count ) //use the override if present
                                    {
                                        buffer.AddNumberMoreReadable( ( ( relatedEntityTypeData.LODDistanceOverrides[i] * relatedEntityTypeData.LODDistanceMultiplier) / lodDivisor) );
                                        buffer.Add( "   <color=#aaaaaa>(Overriding: " ).AddNumberMoreReadable( ( relatedEntityTypeData.LODDistancesFromVis[i] / lodDivisor ) ).Add( ")</color>" );
                                        if ( hasLODDivisor )
                                        {
                                            buffer.Add( "   <color=#c79462>(Orig: " )
                                                .AddNumberMoreReadable( (relatedEntityTypeData.LODDistanceOverrides[i] * relatedEntityTypeData.LODDistanceMultiplier) )
                                                .Add( " / " )
                                                .AddNumberMoreReadable( relatedEntityTypeData.LODDistancesFromVis[i] )
                                                .Add( ")</color>" );
                                        }
                                    }
                                    else
                                    {
                                        buffer.AddNumberMoreReadable( ( ( relatedEntityTypeData.LODDistancesFromVis[i] * relatedEntityTypeData.LODDistanceMultiplier) / lodDivisor) );
                                        if ( hasLODDivisor )
                                        {
                                            buffer.Add( "   <color=#c79462>(Orig: " )
                                                .AddNumberMoreReadable( (relatedEntityTypeData.LODDistancesFromVis[i] * relatedEntityTypeData.LODDistanceMultiplier) )
                                                .Add( ")</color>" );
                                        }
                                    }
                                    buffer.Add( "<pos=350><color=#aaaaaa>Mesh Vertex Count:</color> " ).AddNumberMoreReadable( relatedEntityTypeData.LODMeshesFromVis[i].vertexCount );
                                }
                            }
                        }
                    }
                } //end if ( relatedEntity != null )


                buffer.Add( "</size>" );
                buffer.Add( "</size>" );

                //truly the last thing before network stuff
                debugStage = 9005;

                buffer.Add( "<size=50%>" );
                if ( detailLevel < TooltipDetail.Full )
                    buffer.AddDlcMod(relatedEntityTypeData);
                else
                    buffer.AddDlcMod(relatedEntityTypeData, "This unit was added by");
                buffer.Add( "</size>" );

                debugStage = 9010;

                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client && !relatedSquadOrNull.IsFakeEntity )
                {
                    if ( relatedSquadOrNull.Network_ClientOnly_SimCyclesSinceLastHostUpdate > 50 )
                    {
                        buffer.Add( "\n<pos=40><color=#ff6767>It has been </color> " ).AddFixedDecimalThousands( (relatedSquadOrNull.Network_ClientOnly_SimCyclesSinceLastHostUpdate / 10f), 1 )
                            .Add( " seconds since this unit has been checked by the host!</color>  "  );
                    }
                    else
                    {
                        if ( detailLevel == TooltipDetail.Full )
                            buffer.Add( "\n<pos=40><color=#aaaaaa></color> " ).AddFixedDecimalThousands( (relatedSquadOrNull.Network_ClientOnly_SimCyclesSinceLastHostUpdate / 10f), 1 )
                                .Add( "s since unit was checked by host.</color>  " );
                    }
                }

                debugStage = 9020;
            } 
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLog( "Exception in entity tooltip text generation at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                buffer.Add( "Exception during generation of tooltip!" );
                return false;
            }
            return true;
        }

        #region WriteSystemInfo
        public static void _WriteSystemInfo( ArcenCharacterBufferBase buffer, int systemIndex, GameEntity_Squad relatedSquadOrNull, FleetMembership relatedMembershipOrNull, Faction facOrNull, Planet relatedPlanet, GameEntityTypeData relatedEntityTypeData,
            byte effectiveMarkLevel, bool OnlyWriteIfWeapon, bool OnlyWriteIfModuleStatBooster, TooltipDetail detailLevel, bool isForMultipleUnits, bool showWeaponActivityDetails )
        {
            bool unused = false;
            _WriteSystemInfo( buffer, systemIndex, relatedSquadOrNull, relatedMembershipOrNull, facOrNull, relatedPlanet, relatedEntityTypeData.SystemTypes[systemIndex],
                effectiveMarkLevel, OnlyWriteIfWeapon, OnlyWriteIfModuleStatBooster, detailLevel, isForMultipleUnits, showWeaponActivityDetails, ref unused );
            if ( unused ) { }
        }

        public static void _WriteSystemInfo( ArcenCharacterBufferBase buffer, int systemIndex, GameEntity_Squad relatedSquadOrNull, FleetMembership relatedMembershipOrNull, Faction facOrNull, Planet relatedPlanet, EntitySystemTypeData systemData, 
            byte effectiveMarkLevel, bool OnlyWriteIfWeapon, bool OnlyWriteIfModuleStatBooster, TooltipDetail detailLevel, bool isForMultipleUnits, bool showWeaponActivityDetails, ref bool hasSystemWithBonusFromAttackingUnderForcefields )
        {
            Color damageColor = ColorMath.LightRed;
            int debugStage = 0;
            try
            {
                EntitySystemTypeData.MarkLevelStats systemStats = systemData.ForMark[effectiveMarkLevel];

                EntitySystem actualEntitySystemOrNull = null;
                if ( !relatedSquadOrNull.IsFakeEntity )
                    actualEntitySystemOrNull = relatedSquadOrNull.Systems[systemIndex];

                string display_name = systemData.DisplayName;
                if (systemData.CustomType != null)
                    display_name = systemData.CustomType.DisplayName;
                
                int display_shotdamage = systemStats.DamagePerShot;
                if (actualEntitySystemOrNull != null)
                    display_shotdamage = actualEntitySystemOrNull.GetShotDamage();
                
                int display_salvodps = systemStats.DamagePerSecond.GetNearestIntPreferringHigher();
                
                #region If A Weapon
                if ( systemStats.DamagePerShot > 0 && systemData.Category == EntitySystemCategory.Weapon )
                {
                    if ( OnlyWriteIfModuleStatBooster )
                    {
                        //buffer.Add( "OnlyWriteIfModuleStatBooster" );
                        return;
                    }
                    //EntitySystemTypeData systemData = systemSubEntry.SystemData;
                    
                    debugStage = 183;
                    bool isRetaliatory = systemData.FiringTiming == FiringTiming.WhenParentEntityHit;
                    bool isMirrorShot = isRetaliatory && systemData.ReturnsThisPercentageOfDamageWhenFiringRetaliatoryShot > FInt.Zero;
                    bool isDroneGun = systemData.ShotTypeData?.Category == GameEntityCategory.Ship;
                    bool isOnDeath = systemData.OnlyFiresOnDeath;
                    
                    buffer.Add( "\n" ).Add( "<b>" ).Add( display_name ).Add( "</b> " );
                    
                    debugStage = 185;
                    if ( systemData.IonDamageToAlbedoLessThan > FInt.Zero || isMirrorShot )
                    { } //don't draw damage amount for ion or mirror shot
                    else if ( systemData.OverdrivesShields )
                        buffer.Add( "100% Shield Damage, " );
                    else
                    {
                        if ( isOnDeath )
                        {
                            buffer.Add( "" );
                        }
                        else 
                        if ( isDroneGun )
                        {
                            buffer.Add( "发射 " );
                        }
                        else 
                        if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot > 0 && systemData.NumberBeamsToFire > 0 )
                        {
                            debugStage = 185100;
                            int effectiveMaxTargets = systemStats.AOEMaximumNumberOfTargetsHitPerShot;
                            if ( systemData.AOESpreadsDamageAmongAvailableTargets )
                                effectiveMaxTargets = 1; //it's divided up, so just show it once

                            buffer.StartColor( damageColor ).AddNumberMoreReadable( display_salvodps ).EndColor()
                                .Add( " DPS to " ).StartColor( damageColor ).AddNumberMoreReadable( (display_salvodps * effectiveMaxTargets * systemData.NumberBeamsToFire) ).EndColor().Add( " DPS, " );
                        }
                        else 
                        if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot > 0 && !systemData.AOESpreadsDamageAmongAvailableTargets )
                        {
                            debugStage = 185200;
                            buffer.StartColor( damageColor ).AddNumberMoreReadable( display_salvodps ).EndColor()
                                .Add( " DPS to " ).StartColor( damageColor ).AddNumberMoreReadable( (display_salvodps * systemStats.AOEMaximumNumberOfTargetsHitPerShot) ).EndColor().Add( " DPS, " );
                        }
                        else
                        {
                            debugStage = 185300;
                            buffer.StartColor( damageColor ).AddNumberMoreReadable( display_salvodps ).EndColor()
                                .Add( " DPS, " );
                        }
                        // else buffer.Add( "DPS " ).StartColor( damageColor ).Add( systemStats.BaseDPS.GetNearestIntPreferringHigher() ).EndColor().Add( ", " );
                        // MaximumNumberOfTargetsHitPerShot;
                        if ( systemStats.ShotsPerSalvo > 1 )
                            buffer.Add( "<color=#ffdf72>" ).Add( systemStats.ShotsPerSalvo ).Add( "x</color> " );

                        debugStage = 185400;
                        if (isDroneGun) 
                        {
                            buffer.Add( systemData.ShotTypeData.DisplayName ).Add(", ");
                        } 
                        else 
                        {
                            buffer.StartColor( damageColor ).AddNumberMoreReadable( display_shotdamage ).EndColor();
                            if ( isRetaliatory )
                                buffer.Add( " <color=#dfff72>Exotic Damage</color>, " );
                            else
                                buffer.Add( " Damage, " );
                        }
                    }
                    
                    debugStage = 200;
                    if ( isOnDeath )
                        buffer.StartColor( damageColor ).Add( "Fires On Ship Death" ).EndColor().Add( ", " );
                    else 
                    if ( isRetaliatory )
                        buffer.StartColor( damageColor ).Add( "Fires When Ship Hit" ).EndColor().Add( ", " );
                    else
                    {
                        if ( detailLevel >= TooltipDetail.Medium )
                        {
                            int secondsPerSalvoRightNow = systemStats.SecondsPerSalvo;
                            int forAnotherXSalvos = -1;
                            if ( actualEntitySystemOrNull != null )
                            {
                                if ( actualEntitySystemOrNull.IsInAltSalvoFiringMode )
                                    secondsPerSalvoRightNow = systemStats.AltSecondsPerSalvo;

                                if ( systemStats.AltSecondsPerSalvo > 0 )
                                {
                                    if ( actualEntitySystemOrNull.IsInAltSalvoFiringMode )
                                        forAnotherXSalvos = systemData.UseAlternateRateOfFireForXShotsBeforeReverting - actualEntitySystemOrNull.ShotsSinceLastSwitchingSalvoFiringMode;
                                    else
                                        forAnotherXSalvos = systemData.UseAlternateRateOfFireAfterXShots - actualEntitySystemOrNull.ShotsSinceLastSwitchingSalvoFiringMode;
                                }
                            }

                            if ( detailLevel >= TooltipDetail.Full || isForMultipleUnits )
                            {
                                buffer.Add( "装弹速度 " ).StartColor( damageColor ).Add( secondsPerSalvoRightNow ).Add( "s" );
                                if ( forAnotherXSalvos > 0 )
                                {
                                    buffer.Add( " for another " ).Add( forAnotherXSalvos ).Add( " salvos" );
                                }
                                buffer.EndColor().Add( ", " );
                            }
                            else
                                buffer.Add( "装弹 " ).StartColor( damageColor ).Add( secondsPerSalvoRightNow ).Add( "s" ).EndColor().Add( ", " );
                        }
                        if ( systemStats.AltSecondsPerSalvo > 0 )
                        {
                            buffer.Add( "爆发射击" );
                            if ( detailLevel >= TooltipDetail.Medium )
                            {
                                buffer.Add( ": " );
                                if ( detailLevel >= TooltipDetail.Full )
                                    buffer.Add( systemData.UseAlternateRateOfFireAfterXShots ).Add( " salvos fired at " )
                                        .Add( systemStats.SecondsPerSalvo ).EndColor().Add( "s per salvo before switching to " )
                                        .Add( systemData.UseAlternateRateOfFireForXShotsBeforeReverting ).Add( " salvos fired at " )
                                        .Add( systemStats.AltSecondsPerSalvo ).EndColor().Add( "s per salvo (and then switching back), " );
                                else
                                    buffer.Add( systemData.UseAlternateRateOfFireAfterXShots ).Add( " at " )
                                        .Add( systemStats.SecondsPerSalvo ).EndColor().Add( "s before " )
                                        .Add( systemData.UseAlternateRateOfFireForXShotsBeforeReverting ).Add( " at " )
                                        .Add( systemStats.AltSecondsPerSalvo ).EndColor().Add( "s, " );
                            }
                            else
                                buffer.Add( ", " );
                        }
                    }
                    if ( systemStats.CorrosionDamage > 0 )
                        buffer.Add("将额外造成 ").Add( systemStats.CorrosionDamage, "a1ffa1" ).Add(" 腐蚀伤害。" );
                    if ( systemData.AllDamageIsCorrosive )
                        buffer.Add( "所有伤害均为腐蚀伤害。", "69ff69" );
                    if ( detailLevel == TooltipDetail.Full && (systemStats.CorrosionDamage > 0 || systemData.AllDamageIsCorrosive) )
                        buffer.AddSize_Small().Add( "(Corrosive damage is dealt over time directly to the hull of the target.) " ).EndSize();

                    debugStage = 205;

                    if ( isRetaliatory )
                        buffer.StartColor( damageColor ).Add( "Any Range Required To Retaliate" ).EndColor();
                    else if ( systemData.ShotsDetonateImmediately &&
                        //beam weapons detonate immediately but still have a range
                        systemData.BeamLengthMultiplier <= FInt.Zero )
                        buffer.StartColor( damageColor ).Add( "Detonates From Ship" ).EndColor();
                    else if ( systemData.IsMelee )
                        buffer.StartColor( damageColor ).Add( "Melee Range" ).EndColor();
                    else
                    {
                        int actualRange;
                        if ( !relatedSquadOrNull.IsFakeEntity ) {
                            actualRange = systemStats.CalculateActualRange( relatedSquadOrNull );
                        } else {
                            actualRange = systemStats.CalculateActualRange( facOrNull, relatedPlanet, 0 );
                        }
                        if (actualRange > 99999) {
                            //FiresFromAnyRange actually just has to do with targeting logic, and doesn't seem
                            //like something that we should be surfacing to players here.  Just doesn't seem relevant.
                            buffer.StartColor( damageColor ).Add( "Infinite Range" ).EndColor();
                        } else {
                            buffer.Add( "范围 " ).StartColor( damageColor ).AddNumberMoreReadable( actualRange ).EndColor();
                        }
                    }

                    buffer.Add( "  " );

                    if ( systemData.InflictsStateOfMatterOnTargetForSeconds > 0 && systemData.StateOfMatterForTargetToBecome != null )
                    {
                        if ( systemData.CannotInflictStateOfMatterIfTargetHasAnyShieldsUp )
                        {
                            if ( systemData.CannotInflictStateOfMatterIfTargetHasEnergyUsageOfAtLeast > 0 )
                                buffer.Add( "当击中无个人护盾且使用少于 " )
                                    .AddNumberMoreReadable( systemData.CannotInflictStateOfMatterIfTargetHasEnergyUsageOfAtLeast ).Add( " energy, causes it to change to " )
                                    .Add( systemData.StateOfMatterForTargetToBecome.DisplayName ).Add( " for " )
                                    .AddNumberMoreReadable( systemData.InflictsStateOfMatterOnTargetForSeconds ).Add( "秒。" );
                            else
                                buffer.Add( "当击中无个人护盾的目标船体时，使其改变为 " )
                                    .Add( systemData.StateOfMatterForTargetToBecome.DisplayName ).Add( " for " )
                                    .AddNumberMoreReadable( systemData.InflictsStateOfMatterOnTargetForSeconds ).Add( "秒。" );

                        }
                        else //don't care about shields
                        {
                            if ( systemData.CannotInflictStateOfMatterIfTargetHasEnergyUsageOfAtLeast > 0 )
                                buffer.Add( "当击中任何使用少于 " )
                                    .AddNumberMoreReadable( systemData.CannotInflictStateOfMatterIfTargetHasEnergyUsageOfAtLeast ).Add( " energy, causes it to change to " )
                                    .Add( systemData.StateOfMatterForTargetToBecome.DisplayName ).Add( " for " )
                                    .AddNumberMoreReadable( systemData.InflictsStateOfMatterOnTargetForSeconds ).Add( "秒。" );
                            else
                                buffer.Add( "当击中任何目标时，使其改变为 " )
                                    .Add( systemData.StateOfMatterForTargetToBecome.DisplayName ).Add( " for " )
                                    .AddNumberMoreReadable( systemData.InflictsStateOfMatterOnTargetForSeconds ).Add( "秒。" );
                        }
                    }

                    if ( systemData.CareAboutStateOfMatterToBeEnabled )
                        WriteSystemStateOfMatterSuffix( buffer, systemData, relatedSquadOrNull );



                    debugStage = 210;

                    #region ShowWeaponActivityDetails
                    if ( !relatedSquadOrNull.IsFakeEntity && 
                         !isForMultipleUnits && 
                         showWeaponActivityDetails &&
                         detailLevel >= TooltipDetail.Medium && 
                         !isRetaliatory )
                    {
                        EntitySystem relatedSystem = relatedSquadOrNull.Systems[systemIndex];
                        if ( relatedSystem != null && (
                            relatedSystem.TimeUntilNextShot > FInt.Zero ||
                            relatedSystem.LastGameSecondIFiredShot > 0 ||
                            relatedSystem.LastGameSecondMyShotHit > 0
                           ) )
                        {
                            if ( detailLevel <= TooltipDetail.Medium )
                                buffer.Add( ", " ); //no newline for abbreviated version
                            else
                                buffer.Add( "\n" ).Add( FontSizes.MUCH_SMALLER_SIZE_STRING );

                            if ( relatedSystem.TimeUntilNextShot > FInt.Zero )
                                buffer.StartColor( damageColor ).AddFixedDecimal( relatedSystem.TimeUntilNextShot.ToFloatNonSim(), 1 )
                                    .Add( "s" ).EndColor().Add( " Until Reload  " );
                            if ( relatedSystem.LastGameSecondIFiredShot > 0 && detailLevel >= TooltipDetail.Full )
                                buffer.StartColor( QuickColors.OldValue ).AddNumberMoreReadable( (World_AIW2.Instance.GameSecond - relatedSystem.LastGameSecondIFiredShot) )
                                    .Add( "s" ).EndColor().Add( " Since Shot Fired  " );
                            if ( relatedSystem.LastGameSecondMyShotHit > 0 )
                                WriteSystemLastDamageInfo( buffer, relatedSystem );
                            WriteSystemLastFireAbortCode( buffer, relatedSystem );
                            buffer.Add( "</size>" );
                        }
                    }
                    #endregion ShowWeaponActivityDetails

                    //if there is a description for the weapon
                    {
                        bool haveDoneNewLineAndSize = false;

                        debugStage = 215;

                        #region AOE
                        if ( systemStats.ShotAreaOfEffect > 0 )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            //lighting-style burst out of ship
                            if ( systemData.ShotsDetonateImmediately )
                            {
                                if ( detailLevel < TooltipDetail.Full )
                                {
                                    buffer.Add( "<color=#f25e1c>DIRECT AOE</color>: radius <color=#ffdf72>" );
                                    buffer.AddNumberMoreReadable( systemStats.ShotAreaOfEffect );
                                    buffer.Add( "</color>, " );
                                    if ( systemData.AOESpreadsDamageAmongAvailableTargets )
                                        buffer.Add( "伤害在目标间分摊，" );
                                    if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot > 0 )
                                        buffer.Add( "<= <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> 个目标" );
                                    else
                                        buffer.Add( "范围内所有目标" );

                                    if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                    {
                                        buffer.Add( " and " );
                                        buffer.Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                        buffer.Add( "% damage to non-primary targets" );
                                    }

                                    if ( systemData.AOEHitsFriendlyTargets )
                                        buffer.Add( ", friendly fire.  " );
                                    else
                                        buffer.Add( ".  " );
                                }
                                else
                                {
                                    buffer.Add( "<color=#f25e1c>AOE</color>: A blast emanates directly out of this ship with a radius of <color=#ffdf72>" );
                                    buffer.AddNumberMoreReadable( systemStats.ShotAreaOfEffect );
                                    buffer.Add( "</color>, " );
                                    if ( systemData.AOESpreadsDamageAmongAvailableTargets )
                                    {
                                        buffer.Add( "在其间分摊伤害 " );
                                        if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot > 0 )
                                            buffer.Add( "最多 <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> 个目标" );
                                        else
                                            buffer.Add( "范围内所有目标" );
                                        if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                        {
                                            buffer.Add( ", with " );
                                            buffer.Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                            buffer.Add( "% damage to targets other than the primary" );
                                        }
                                    }
                                    else
                                    {
                                        if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                        {
                                            buffer.Add( "对主要目标造成全额伤害，并" );
                                            buffer.Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                            buffer.Add( "% damage to " );
                                        }
                                        else
                                            buffer.Add( "对其造成全额伤害" );
                                        if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot > 0 )
                                            buffer.Add( "最多 <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> 个目标" );
                                        else
                                            buffer.Add( "范围内所有目标" );
                                    }
                                    if ( systemData.AOEHitsFriendlyTargets )
                                        buffer.Add( " -- including any friendlies caught in the blast.  " );
                                    else
                                        buffer.Add( ".  " );
                                }
                            }
                            else //AOE at destination
                            {
                                if ( detailLevel < TooltipDetail.Full )
                                {
                                    buffer.Add( "<color=#f25e1c>AOE AT TARGET</color>: radius <color=#ffdf72>" );
                                    buffer.AddNumberMoreReadable( systemStats.ShotAreaOfEffect );
                                    buffer.Add( "</color>, " );
                                    if ( systemData.AOESpreadsDamageAmongAvailableTargets )
                                        buffer.Add( "伤害在目标间分摊，" );
                                    if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot > 0 )
                                        buffer.Add( "<= <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> 个目标" );
                                    else
                                        buffer.Add( "范围内所有目标" );

                                    if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                    {
                                        buffer.Add( " and " );
                                        buffer.Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                        buffer.Add( "% damage to non-primary targets" );
                                    }

                                    if ( systemData.AOEHitsFriendlyTargets )
                                        buffer.Add( ", friendly fire.  " );
                                    else
                                        buffer.Add( ".  " );
                                }
                                else
                                {
                                    buffer.Add( "<color=#f25e1c>AOE</color>: Shots from the above weapon explode with a radius of <color=#ffdf72>" );
                                    buffer.AddNumberMoreReadable( systemStats.ShotAreaOfEffect );
                                    buffer.Add( "</color> on impact, " );
                                    if ( systemData.AOESpreadsDamageAmongAvailableTargets )
                                    {
                                        buffer.Add( "在其间分摊伤害" );
                                        if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot > 0 )
                                            buffer.Add( "最多 <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> 个目标" );
                                        else
                                            buffer.Add( "范围内所有目标" );
                                        if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                        {
                                            buffer.Add( ", with " );
                                            buffer.Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                            buffer.Add( "% damage to targets other than the primary" );
                                        }
                                    }
                                    else
                                    {
                                        if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                        {
                                            buffer.Add( "对主要目标造成全额伤害，并" );
                                            buffer.Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                            buffer.Add( "% damage to " );
                                        }
                                        else
                                            buffer.Add( "对其造成全额伤害" );
                                        if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot > 0 )
                                            buffer.Add( "最多 <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> 个目标" );
                                        else
                                            buffer.Add( "范围内所有目标" );
                                    }
                                    if ( systemData.AOEHitsFriendlyTargets )
                                        buffer.Add( " -- including any friendlies caught in the blast.  " );
                                    else
                                        buffer.Add( ".  " );
                                }
                            }
                        }
                        #endregion

                        debugStage = 220;

                        #region Beam Weapon
                        if ( systemData.BeamLengthMultiplier > FInt.Zero )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( systemData.IsCoilbeam )
                            {
                                if ( detailLevel < TooltipDetail.Full )
                                {
                                    if ( systemData.NumberBeamsToFire == 1 )
                                        buffer.Add( "<color=#f25e1c>COIL-BEAM WEAPON</color>: length <color=#ffdf72>" );
                                    else
                                    {
                                        buffer.Add( "<color=#f25e1c>COIL-BEAM-ARRAY WEAPON</color>: <color=#ffdf72>" );
                                        buffer.Add( systemData.NumberBeamsToFire );
                                        buffer.Add( "</color> beams, length <color=#ffdf72>" );
                                    }
                                    buffer.AddNumberMoreReadable( (systemStats.CalculateActualRange( relatedSquadOrNull ) * systemData.BeamLengthMultiplier).IntValue );


                                    if ( systemData.NumberBeamsToFire == 1 )
                                    {
                                        buffer.Add( "</color>" );
                                    }
                                    else
                                        buffer.Add( "</color> with 1/" ).Add( systemData.NumberBeamsToFire ).Add( " DMG/beam" );
                                    if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot >= 1 )
                                    {
                                        buffer.Add( ", hitting main target for full damage, then <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot );
                                        buffer.Add( "</color> targets with " );
                                        if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                            buffer.Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() )
                                                .Add( "% damage" );
                                        else
                                            buffer.Add( "伤害" );
                                        buffer.Add( " divided evenly, max damage per beam <color=#ffdf72>" ).Add( (systemData.ForMark[effectiveMarkLevel].CalculateShipOrFleetMemDamagePerShot( relatedSquadOrNull, relatedMembershipOrNull ) * 2) ).Add( "</color>" );
                                    }
                                    else
                                    {
                                        buffer.Add( ", hitting main target for full damage, hits everything else with " );
                                        if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                            buffer.Add( "a second copy of " ).Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() )
                                                .Add( "% of the damage" );
                                        else
                                            buffer.Add( "a second copy of the damage" );
                                        buffer.Add( " divided evenly, max damage per beam <color=#ffdf72>" )
                                            .Add( (systemData.ForMark[effectiveMarkLevel].CalculateShipOrFleetMemDamagePerShot( relatedSquadOrNull, relatedMembershipOrNull ) * 2) ).Add( "</color>" );
                                    }

                                    if ( systemData.AOEHitsFriendlyTargets )
                                        buffer.Add( ", friendly fire.  " );
                                    else
                                        buffer.Add( ".  " );
                                }
                                else
                                {
                                    if ( systemData.NumberBeamsToFire == 1 )
                                        buffer.Add( "<color=#f25e1c>COIL-BEAM WEAPON</color>: Attacks from the above weapon are in the form of a fan of a beam of length <color=#ffdf72>" );
                                    else
                                    {
                                        buffer.Add( "<color=#f25e1c>COIL-BEAM-ARRAY WEAPON</color>: Attacks from the above weapon are in the form of a fan of <color=#ffdf72>" );
                                        buffer.Add( systemData.NumberBeamsToFire );
                                        buffer.Add( "</color> beams of length <color=#ffdf72>" );
                                    }
                                    buffer.AddNumberMoreReadable( (systemStats.CalculateActualRange( relatedSquadOrNull ) * systemData.BeamLengthMultiplier).IntValue );
                                    if ( systemData.NumberBeamsToFire == 1 )
                                    {
                                        buffer.Add( "</color>, " );
                                    }
                                    else
                                    {
                                        buffer.Add( "</color> with <color=#ffdf72>" );
                                        buffer.AddFixedDecimal( systemData.DegreesOffsetPerBeam, 1 );
                                        buffer.Add( " degrees</color> between each beam, " );
                                    }
                                    if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot >= 1 )
                                    {
                                        buffer.Add( "击中主要目标造成全额伤害，然后击中 <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> 个目标，造成第二份伤害并在所有目标间平均分摊，每束最大伤害 <color=#ffdf72>" ).Add( (systemData.ForMark[effectiveMarkLevel].CalculateShipOrFleetMemDamagePerShot( relatedSquadOrNull, relatedMembershipOrNull ) * 2) ).Add( "</color>. " );
                                    }
                                    else
                                        buffer.Add( "击中主要目标造成全额伤害，然后对所有其他目标造成第二份伤害，平均分摊，每束最大伤害 <color=#ffdf72>" ).Add( (systemData.ForMark[effectiveMarkLevel].CalculateShipOrFleetMemDamagePerShot( relatedSquadOrNull, relatedMembershipOrNull ) * 2) ).Add( "</color>. " );
                                    if ( systemData.NumberBeamsToFire > 1 )
                                        buffer.Add( "注意：由于多束光束可能击中同一目标，更近或更大的目标会承受更多伤害。" );
                                }
                            }
                            else 
                            if ( !systemData.HitsAllIntersectingTargets )
                            {
                                if ( systemData.BeamChainsOutToTargetsXTimes > 0 && systemData.BeamChainsOutToTargetsXRange > 0 )
                                {
                                    if ( systemData.DistanceFromCenterForBeamEmission > 0 )
                                        buffer.Add( "<color=#f25e1c>RADIAL CHAIN-LIGHTNING WEAPON</color>: <color=#ffdf72>" );
                                    else
                                        buffer.Add( "<color=#f25e1c>CHAIN-LIGHTNING WEAPON</color>: Hits " );

                                    if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot >= 1 )
                                    {
                                        buffer.Add( "最多 <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> 个总目标 " );
                                    }
                                    else
                                        buffer.Add( "任意数量的总目标 " );

                                    buffer.Add( "链式闪电攻击，跳跃 <color=#ffdf72>" ).AddNumberMoreReadable( systemData.BeamChainsOutToTargetsXTimes ).Add( "</color> 次 " );
                                    buffer.Add( "链式范围 <color=#ffdf72>" ).AddNumberMoreReadable( systemData.BeamChainsOutToTargetsXRange ).Add( "</color>.  " );

                                    if ( detailLevel < TooltipDetail.Full )
                                    { }
                                    else
                                    {
                                        buffer.Add( "每次闪电跳跃可击中 " );
                                        if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot >= 1 )
                                        {
                                            buffer.Add( "最多 <color=#ffdf72>" ).Add( systemData.BeamChainsOutToMaxTargetsFromEachSource ).Add( "</color> targets in the next cycle.  " );
                                        }
                                        else
                                            buffer.Add( "任意数量目标在下一周期。" );
                                        if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                        {
                                            buffer.Add( "主要目标后的每个目标承受 " ).Add(
                                                systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() ).Add( "% of the usual damage.  " );
                                        }
                                        else
                                            buffer.Add( "主要目标后的每个目标承受全额伤害。" );
                                    }
                                }
                                else
                                {
                                    if ( detailLevel < TooltipDetail.Full )
                                        buffer.Add( "<color=#f25e1c>POINT-BEAM WEAPON</color>: Hits one target. " );
                                    else
                                        buffer.Add( "<color=#f25e1c>POINT-BEAM WEAPON</color>: Attacks from the above weapon are in the form of beam that hits only the target it was aimed at. " );
                                }
                            }
                            else
                            {
                                if ( detailLevel < TooltipDetail.Full )
                                {
                                    if ( systemData.NumberBeamsToFire == 1 )
                                        buffer.Add( "<color=#f25e1c>BEAM WEAPON</color>: length <color=#ffdf72>" );
                                    else
                                    {
                                        if ( systemData.DistanceFromCenterForBeamEmission > 0 )
                                            buffer.Add( "<color=#f25e1c>RADIAL BEAM-ARRAY WEAPON</color>: <color=#ffdf72>" );
                                        else
                                            buffer.Add( "<color=#f25e1c>BEAM-ARRAY WEAPON</color>: <color=#ffdf72>" );
                                        buffer.Add( systemData.NumberBeamsToFire );
                                        buffer.Add( "</color> beams, length <color=#ffdf72>" );
                                    }
                                    buffer.AddNumberMoreReadable( (systemStats.CalculateActualRange( relatedSquadOrNull ) * systemData.BeamLengthMultiplier).IntValue );

                                    if ( systemData.NumberBeamsToFire == 1 )
                                    {
                                        buffer.Add( "</color>" );
                                    }
                                    else
                                        buffer.Add( "</color> with 1/" ).Add( systemData.NumberBeamsToFire ).Add( " DMG/beam" );
                                    if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot >= 1 )
                                        buffer.Add( "，击中 <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> 个目标，每束最大伤害 <color=#ffdf72>" ).Add( (systemData.ForMark[effectiveMarkLevel].CalculateShipOrFleetMemDamagePerShot( relatedSquadOrNull, relatedMembershipOrNull ) * systemStats.AOEMaximumNumberOfTargetsHitPerShot / systemData.NumberBeamsToFire) ).Add( "</color>" );
                                    if ( systemData.AOEHitsFriendlyTargets )
                                        buffer.Add( ", friendly fire" );

                                    if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                    {
                                        buffer.Add( ", " );
                                        buffer.Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                        buffer.Add( "% damage to non-primary targets" );
                                    }
                                    buffer.Add( ".  " );
                                }
                                else
                                {
                                    if ( systemData.NumberBeamsToFire == 1 )
                                        buffer.Add( "<color=#f25e1c>BEAM WEAPON</color>: Attacks from the above weapon are in the form of a fan of a beam of length <color=#ffdf72>" );
                                    else
                                    {
                                        if ( systemData.DistanceFromCenterForBeamEmission > 0 )
                                            buffer.Add( "<color=#f25e1c>RADIAL BEAM-ARRAY WEAPON</color>: <color=#ffdf72>" );
                                        else
                                            buffer.Add( "<color=#f25e1c>BEAM-ARRAY WEAPON</color>: Attacks from the above weapon are in the form of a fan of <color=#ffdf72>" );
                                        buffer.Add( systemData.NumberBeamsToFire );
                                        buffer.Add( "</color> beams of length <color=#ffdf72>" );
                                    }
                                    buffer.AddNumberMoreReadable( (systemStats.CalculateActualRange( relatedSquadOrNull ) * systemData.BeamLengthMultiplier).IntValue );
                                    if ( systemData.NumberBeamsToFire == 1 )
                                    {
                                        buffer.Add( "</color>, " );
                                    }
                                    else
                                    {
                                        buffer.Add( "</color> with <color=#ffdf72>" );
                                        buffer.AddFixedDecimal( systemData.DegreesOffsetPerBeam, 1 );
                                        buffer.Add( " degrees</color> between each beam, " );
                                    }
                                    if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot >= 1 )
                                    {
                                        buffer.Add( "击中 <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> 个目标" );
                                    }
                                    else
                                        buffer.Add( "击中光束路径上的所有目标" );
                                    if ( systemData.NumberBeamsToFire == 1 )
                                    {
                                        if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                        {
                                            buffer.Add( " with " );
                                            buffer.Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                            buffer.Add( "% damage to targets other than the primary" );
                                        }
                                        else
                                            buffer.Add( " with its full damage" );
                                    }
                                    else
                                    {
                                        buffer.Add( " with 1/" ).Add( systemData.NumberBeamsToFire ).Add( " its damage per beam that strikes them" );
                                        if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                        {
                                            buffer.Add( " and " );
                                            buffer.Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                            buffer.Add( "% damage to targets other than the primary" );
                                        }
                                    }
                                    if ( systemData.AOEHitsFriendlyTargets )
                                        buffer.Add( " -- including any friendlies caught in the blast.  " );
                                    else
                                        buffer.Add( ".  " );
                                    if ( systemData.NumberBeamsToFire > 1 )
                                        buffer.Add( "注意：由于多束光束可能击中同一目标，更近或更大的目标会承受更多伤害。" );
                                }
                            }
                        }
                        #endregion

                        debugStage = 223;

                        #region ShotsPerSalvo
                        if ( systemData.ForMark[effectiveMarkLevel].ShotsPerSalvo > 1 && !isMirrorShot && !isDroneGun )
                        {
                            if ( systemData.BeamLengthMultiplier > FInt.Zero )
                            {
                                HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );
                                if ( detailLevel < TooltipDetail.Full )
                                {
                                    buffer.Add( "<color=#f25e1c>MULTI-BEAM</color>: up to <color=#ffdf72>" ).Add( systemData.ForMark[effectiveMarkLevel].ShotsPerSalvo ).Add( "</color> beams" );
                                    if ( !systemData.HitsAllIntersectingTargets )
                                        buffer.Add( ", one target per beam.  " );
                                    else if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot >= 1 )
                                        buffer.Add( ", full DMG to intersected targets.  " );
                                }
                                else
                                {
                                    buffer.Add( "<color=#f25e1c>MULTI-BEAM</color>: Up to <color=#ffdf72>" ).Add( systemData.ForMark[effectiveMarkLevel].ShotsPerSalvo )
                                        .Add( "</color> beams can be fired at a time. " );
                                    if ( systemData.ForMark[effectiveMarkLevel].ShotsPerSalvo > systemData.ForMark[effectiveMarkLevel].ShotsPerTarget )
                                        buffer.Add( "仅 " ).Add( systemData.ForMark[effectiveMarkLevel].ShotsPerTarget ).Add( " 可对每个目标和堆叠发射。" );
                                    else
                                        buffer.Add( "均可瞄准同一目标。" );
                                    if ( !systemData.HitsAllIntersectingTargets )
                                        buffer.Add( "单个目标可能被多束相交光束击中。每束光束造成全额伤害。" );
                                    else if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot >= 1 )
                                        buffer.Add( "每束光束造成全额伤害。" );
                                }
                            }
                            else
                            {
                                if ( detailLevel >= TooltipDetail.Full )
                                {
                                    HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );
                                    buffer.Add( "<color=#f25e1c>MULTI-SHOT</color>: Up to <color=#ffdf72>" ).Add( systemData.ForMark[effectiveMarkLevel].ShotsPerSalvo )
                                        .Add( "</color> shots can be fired at a time. " );
                                    if ( systemData.ForMark[effectiveMarkLevel].ShotsPerSalvo > systemData.ForMark[effectiveMarkLevel].ShotsPerTarget )
                                        buffer.Add( "仅 " ).Add( systemData.ForMark[effectiveMarkLevel].ShotsPerTarget ).Add( " 可对每个目标和堆叠发射" );
                                    else
                                        buffer.Add( "均可瞄准同一目标" );
                                    buffer.Add( "，每发造成全额伤害。" );
                                }
                            }
                        }
                        #endregion

                        debugStage = 220;
                        #region isMirrorShot
                        if ( isMirrorShot )
                        {
                            buffer.Add( "<color=#f25e1c>" );
                            buffer.Add( (systemData.ReturnsThisPercentageOfDamageWhenFiringRetaliatoryShot * 100).GetNearestIntPreferringHigher() );
                            buffer.Add( "% MIRROR DAMAGE:</color> " );
                            if ( detailLevel >= TooltipDetail.Medium )
                            {
                                buffer.Add( "此镜面武器反射一枚射弹，" ).Add(
                                    (systemData.ReturnsThisPercentageOfDamageWhenFiringRetaliatoryShot * 100).GetNearestIntPreferringHigher() )
                                    .Add( "% of the power of any shots that hit its parent, back to the unit that shot it. This is considered <color=#dfff72>exotic damage</color>. " );
                            }
                            else
                                buffer.Add( "基于击中母舰的冲击力反射一枚射弹。" );
                        }
                        #endregion
                        debugStage = 221;
                        #region MobileImmobile
                        if ( systemData.OnlyTargetsStaticUnits )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            buffer.Add( "<color=#f25e1c>LIMITED TARGETS</color>: Can only target static structures.  " );
                        }
                        if ( systemData.OnlyTargetsMobileUnits )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            buffer.Add( "<color=#f25e1c>LIMITED TARGETS</color>: Can only target mobile units.  " );
                        }
                        if ( systemData.OnlyTargetsStrikecraftAndFrigates )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            buffer.Add( "<color=#f25e1c>LIMITED TARGETS</color>: Can only target strikecraft and frigates.  " );
                        }
                        #endregion
                        debugStage = 225;


                        #region EngineStun
                        if ( systemData.EngineStunToEngine_gxLessThan > 0 )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                if ( systemData.MaxEngineStunSeconds > 0 )
                                    buffer.Add( "<color=#f25e1c>ENGINE-SLOWER</color>: <color=#ffdf72>" );
                                else
                                    buffer.Add( "<color=#f25e1c>ENGINE-STUNNER</color>: <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.EngineStunPerShot );
                                buffer.Add( "秒</color> 如果目标引擎 < <color=#ffdf72>" );
                                buffer.Add( systemData.EngineStunToEngine_gxLessThan );
                                buffer.Add( " gx</color>" );
                                if ( systemData.MaxEngineStunSeconds > 0 )
                                {
                                    if ( systemData.MaxEngineStunSeconds >= ExternalConstants.Instance.EngineStunMultipliersByStunSeconds.Count )
                                        buffer.Add( ".  " ); //fully stunned
                                    else //partially stunned
                                        buffer.Add( ", max " ).AddNumberMoreReadable( systemData.MaxEngineStunSeconds ).Add( "秒。" );
                                }
                                else
                                    buffer.Add( ".  " );
                            }
                            else
                            {
                                if ( systemData.MaxEngineStunSeconds > 0 )
                                    buffer.Add( "<color=#f25e1c>ENGINE-SLOWER</color>: Shots from the above weapon slow enemy engines for <color=#ffdf72>" );
                                else
                                    buffer.Add( "<color=#f25e1c>ENGINE-STUNNER</color>: Shots from the above weapon stun enemy engines for <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.EngineStunPerShot );
                                buffer.Add( "秒</color> 如果目标引擎动力低于 <color=#ffdf72>" );
                                buffer.Add( systemData.EngineStunToEngine_gxLessThan );
                                buffer.Add( " gx</color>.  " );
                                if ( systemData.MaxEngineStunSeconds > 0 )
                                {
                                    FInt engineSpeed = FInt.Zero;
                                    if ( systemData.MaxEngineStunSeconds >= ExternalConstants.Instance.EngineStunMultipliersByStunSeconds.Count )
                                    { } //fully stunned
                                    else //partially stunned
                                        engineSpeed = ExternalConstants.Instance.EngineStunMultipliersByStunSeconds[systemData.MaxEngineStunSeconds];

                                    buffer.Add( "目标可被减速最多 " ).AddNumberMoreReadable( systemData.MaxEngineStunSeconds )
                                        .Add( "s, at which point its movement speed will only be  <color=#ffdf72>" ).Add( engineSpeed.ReadableString ).Add( "x</color> normal.  " );
                                }
                                else
                                    buffer.Add( "目标累积的眩晕秒数越多，速度越慢。4秒 = 50%移动速度，7秒以上 = 无法移动。" );
                            }
                        }
                        #endregion

                        debugStage = 235;

                        #region Paralysis
                        if ( systemData.ParalysisToShipsMass_tXLessThan > 0 )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#f25e1c>PARALYZER</color>: <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.ParalysisSecondsPerShot );
                                buffer.Add( "秒</color> 如果目标质量 < <color=#ffdf72>" );
                                buffer.Add( systemData.ParalysisToShipsMass_tXLessThan.ReadableString );
                                buffer.Add( " tX</color>.  " );
                            }
                            else
                            {
                                buffer.Add( "<color=#f25e1c>PARALYZER</color>: Shots from the above weapon completely shut down enemy ships for <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.ParalysisSecondsPerShot );
                                buffer.Add( "秒</color> 如果目标质量低于 <color=#ffdf72>" );
                                buffer.Add( systemData.ParalysisToShipsMass_tXLessThan.ReadableString );
                                buffer.Add( " tX</color>.  " );
                            }
                        }
                        #endregion
                        debugStage = 236;

                        #region FiresThroughEnemyShields
                        if ( systemData.FiresThroughEnemyShields )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#f25e1c>FIRES THROUGH FORCEFIELDS</color>  " );
                            }
                            else
                            {
                                buffer.Add( "<color=#f25e1c>FIRES THROUGH FORCEFIELDS</color>: Shots from the above weapon completely ignore forcefields as if they were not there, damaging ships under them like normal.  " );
                            }
                        }
                        #endregion

                        #region SetsTargetToRandomLocation
                        if ( systemData.SetsTargetToRandomLocation )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#f25e1c>TARGET WARP</color>  " );
                            }
                            else
                            {
                                buffer.Add( "<color=#f25e1c>TARGET WARP</color>: Shots from the above weapon cause enemies that are hit to be moved to a completely random spot in the planet's gravity well.  " );
                            }
                        }
                        #endregion

                        debugStage = 237;
                        #region StackingDamage
                        if ( systemData.MaxStacksToKill != 1 )
                        {
                            buffer.Add( " <color=#f25e1c>MULTI KILL</color>: Allowed to kill <color=#ffdf72>" + systemData.MaxStacksToKill + "</color> stacks at a time from a squad with each shot. " );
                        }
                        #endregion

                        debugStage = 238;

                        #region TractorDamage
                        if ( systemStats.DamageMultiplierToTractoredUnits > FInt.Zero )

                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );
                            buffer.Add( "<color=#f25e1c>ATTACK BONUS</color>: Shots from the above weapon do <color=#ffdf72>" ).Add( systemStats.DamageMultiplierToTractoredUnits.ReadableString, "a1ffa1" ).Add( "x</color> damage to units in tractor beams. " );
                        }
                        #endregion

                        debugStage = 239;

                        #region ForcefieldDamageMultiplier
                        if ( systemData.DamageModifierWhileUnderForcefield != FInt.FromParts( 0, 500 ) )
                        {
                            if ( systemData.DamageModifierWhileUnderForcefield > FInt.One )
                                hasSystemWithBonusFromAttackingUnderForcefields = true;

                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );
                            buffer.Add( "<color=#f25e1c>ATTACK BONUS</color>: Shots from the above weapon do <color=#ffdf72>" ).Add( systemData.DamageModifierWhileUnderForcefield.ReadableString, "a1ffa1" ).Add( "x</color> damage if this unit is under a forcefield, ignoring the normal penalty. " );
                        }
                        #endregion

                        debugStage = 240;

                        #region WeaponPoints
                        // The case where it doesn't consume Weapon Points on firing, i.e Neinzul Firefly style.
                        if ( systemData.AdditionalDamageModifierPerWeaponPoint != FInt.Zero && systemData.NumberOfWeaponPointsToConsumeOnFiring == 0 )

                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                                buffer.Add( "<color=#f25e1c>ATTACK BONUS</color>: Additional <color=#ffdf72>" ).Add( systemData.AdditionalDamageModifierPerWeaponPoint.ReadableString, "a1ffa1" ).Add( "x</color> per current Weapon Point. " );
                            else
                                buffer.Add( "<color=#f25e1c>ATTACK BONUS</color>: Shots from the above weapon do an additional <color=#ffdf72>" ).Add( systemData.AdditionalDamageModifierPerWeaponPoint.ReadableString, "a1ffa1" ).Add( "x</color> damage for every Weapon Point it currently has. " );
                        }

                        // The case where it DOES consume Weapon Points on firing, i.e Powerslaver style.
                        if ( systemData.AdditionalDamageModifierPerWeaponPoint != FInt.Zero && systemData.NumberOfWeaponPointsToConsumeOnFiring != 0 )

                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                                buffer.Add( "<color=#f25e1c>ATTACK BONUS</color>: Consumes <color=#ffdf72>" + systemData.NumberOfWeaponPointsToConsumeOnFiring + "</color> Weapon Points, additional <color=#ffdf72>" ).Add( systemData.AdditionalDamageModifierPerWeaponPoint.ReadableString, "a1ffa1" ).Add( "x</color> damage per consumed Weapon Point. " );
                            else
                                buffer.Add( "<color=#f25e1c>ATTACK BONUS</color>: Shots from the above weapon consume <color=#ffdf72>" + systemData.NumberOfWeaponPointsToConsumeOnFiring + "</color> Weapon Points per salvo, doing an additional <color=#ffdf72>" ).Add( systemData.AdditionalDamageModifierPerWeaponPoint.ReadableString, "a1ffa1" ).Add( "x</color> damage for every Weapon Point it consumed. " );
                        }

                        if ( systemData.NumberOfWeaponPointsToGainOnFiring != FInt.Zero )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            buffer.Add( "<color=#f25e1c>GAINS WEAPON POINTS</color>: Shots from the above weapon increase this units Weapon Points by <color=#ffdf72>" + systemData.NumberOfWeaponPointsToGainOnFiring + "</color>, up to max of <color=#ffdf72>" + systemData.ParentEntityTypeData.MaxNumberOfWeaponPoints + "</color>. " );
                        }
                        #endregion

                        debugStage = 241;

                        #region Devour
                        if ( systemData.CanDevour )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( systemData.DevourFunctionNameUpper.Length <= 0 )
                                systemData.DevourFunctionNameUpper = systemData.DevourFunctionName.ToUpper();

                            buffer.Add( "<color=#f25e1c>" ).Add( systemData.DevourFunctionNameUpper ).Add( "</color>: If the target ship is the same or lower mark, and has mass lower than <color=#ffdf72>" )
                                .Add( systemData.DevourMassLowerThan.ReadableString ).Add( "tX</color>, then instakill the target ship. " );
                        }
                        #endregion

                        debugStage = 242;

                        #region Infestation
                        if ( systemData.UnitToSpawnOnInfestation.Length > 0 )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( systemData.InfestationFunctionNameUpper.Length <= 0 )
                                systemData.InfestationFunctionNameUpper = systemData.InfestationFunctionName.ToUpper();

                            GameEntityTypeData thingToSpawnFromInfestedUnit = GameEntityTypeDataTable.Instance.GetRowByName( systemData.UnitToSpawnOnInfestation );

                            buffer.Add( "<color=#f25e1c>" ).Add( systemData.InfestationFunctionNameUpper ).Add( ":</color> Whenever this unit deals the killing blow to a foe, it will spawn a " + (thingToSpawnFromInfestedUnit?.DisplayName ?? systemData.UnitToSpawnOnInfestation) + " at the foe's location. " );
                        }
                        #endregion

                        debugStage = 243;

                        #region OverdrivesShields
                        if ( systemData.OverdrivesShields )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                                buffer.Add( "<color=#f25e1c>OVERDRIVES SHIELDS</color>: Completely destroy shields, but cannot damage hulls.  " );
                            else
                                buffer.Add( "<color=#f25e1c>OVERDRIVES SHIELDS</color>: Shots from the above weapon completely destroy the shields on target ships -- including entire bubble forcefields.  However, this weapon cannot damage hulls.  " );
                        }
                        #endregion

                        debugStage = 245;

                        #region IonDamage
                        if ( systemData.IonDamageToAlbedoLessThan > FInt.Zero )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#f25e1c>IONIZING RADIATION</color>: <color=#ffdf72>" );
                                buffer.Add( (systemData.IonPercentagePerMarkLevelLower * FInt.FromParts( 100, 0 )).IntValue );
                                buffer.Add( "%</color> target max health, if target <color=#ffdf72>Mark " );
                                buffer.AddNumberMoreReadable( systemStats.MarkLevel.Ordinal );
                                buffer.Add( "</color> and albedo <color=#ffdf72>" );
                                buffer.Add( systemData.IonDamageToAlbedoLessThan.ReadableString );
                                buffer.Add( "</color>. This is considered <color=#dfff72>exotic damage</color>. " );
                            }
                            else
                            {
                                buffer.Add( "<color=#f25e1c>IONIZING RADIATION</color>: Shots from the above weapon do <color=#ffdf72>" );
                                buffer.Add( (systemData.IonPercentagePerMarkLevelLower * FInt.FromParts( 100, 0 )).IntValue );
                                buffer.Add( "%</color> of the target's max health (shield if present, then hull) to any targets whose Mark is lower than <color=#ffdf72>Mark " );
                                buffer.AddNumberMoreReadable( systemStats.MarkLevel.Ordinal );
                                buffer.Add( "</color> (not Markless) and which have an albedo less than <color=#ffdf72>" );
                                buffer.Add( systemData.IonDamageToAlbedoLessThan.ReadableString );
                                buffer.Add( "</color>. This is considered <color=#dfff72>exotic damage</color> damage.  But wait!  That damage percent is actually multiplied by how many Marks lower the target is.  Two Marks lower and the percentage is doubled, three Marks lower is tripled, etc.  " );
                            }
                        }
                        #endregion

                        debugStage = 250;

                        #region Death Effects
                        if ( systemData.HasAnyDeathEffectOffenses )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            for ( int k = 0; k < DeathEffectTypeTable.Instance.Rows.Count; k++ )
                            {
                                DeathEffectType row = DeathEffectTypeTable.Instance.Rows[k];
                                int damageAmount = systemStats.DeathEffectDamagePerShotByType[row];
                                if ( damageAmount <= 0 )
                                    continue;

                                if ( detailLevel < TooltipDetail.Full )
                                {
                                    buffer.Add( "<color=#f25e1c> " ).Add( row.DescriptionPrefix ).Add( "</color>: <color=#ffdf72>" );
                                    buffer.AddNumberMoreReadable( damageAmount );
                                    buffer.Add( "</color> " ).Add( row.DescriptionDamageName ).Add( " damage" )
                                        .Add( row.DescriptionConditions != null && row.DescriptionConditions.Length > 0 ? " " : "" ).Add( row.DescriptionConditions )
                                        .Add( ".  " ).AddNumberMoreReadable( row.Scale ).Add( " total damage required for effect.  " );
                                    //only for player ships or things that are being contemplated for construction
                                    if ( relatedSquadOrNull == null || relatedSquadOrNull.GetFactionTypeSafe() != FactionType.Player )
                                    {
                                        if ( row.DescriptionAddedNoteForPlayers != null && row.DescriptionAddedNoteForPlayers.Length > 0 )
                                            buffer.Add( row.DescriptionAddedNoteForPlayers ).Add( "  " );
                                    }
                                }
                                else
                                {
                                    buffer.Add( "<color=#f25e1c> " ).Add( row.DescriptionPrefix ).Add( "</color>: The above does <color=#ffdf72>" );
                                    buffer.AddNumberMoreReadable( damageAmount );
                                    buffer.Add( "</color> " ).Add( row.DescriptionDamageName ).Add( " damage" )
                                        .Add( row.DescriptionConditions != null && row.DescriptionConditions.Length > 0 ? " " : "" ).Add( row.DescriptionConditions )
                                        .Add( ".  If at least " ).AddNumberMoreReadable( row.Scale ).Add( " " ).Add( row.DescriptionDamageName )
                                        .Add( " damage has been done to the target when it dies, then " ).Add( row.DescriptionOfEffects ).Add( ".  " );
                                    //only for player ships or things that are being contemplated for construction
                                    if ( relatedSquadOrNull == null || relatedSquadOrNull.GetFactionTypeSafe() != FactionType.Player )
                                    {
                                        if ( row.DescriptionAddedNoteForPlayers != null && row.DescriptionAddedNoteForPlayers.Length > 0 )
                                            buffer.Add( row.DescriptionAddedNoteForPlayers ).Add( "  " );
                                    }
                                }
                            }
                        }
                        #endregion

                        debugStage = 260;

                        #region IsMelee
                        if ( systemData.IsMelee && detailLevel >= TooltipDetail.Full )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            buffer.Add( "<color=#f25e1c>MELEE WEAPON</color>: This ship must get within touching range of its target to hit it.  " );
                        }
                        #endregion

                        debugStage = 265;

                        #region EnemyWeaponReloadSlowing
                        if ( systemData.EnemyWeaponReloadSlowingSecondsArmor_mmLessThan > 0 )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#f25e1c>WEAPON JAMMER</color>: target reload +<color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.EnemyWeaponReloadSlowingSecondsPerShot );
                                buffer.Add( "秒</color> 如果护甲 < <color=#ffdf72>" );
                                buffer.Add( systemData.EnemyWeaponReloadSlowingSecondsArmor_mmLessThan );
                                buffer.Add( "毫米</color>，最大 " );
                                if ( systemData.MaxEnemyWeaponReloadSlowingSeconds > 0 )
                                    buffer.Add( systemData.MaxEnemyWeaponReloadSlowingSeconds );
                                else
                                    buffer.Add( ExternalConstants.Instance.MaxWeaponAddedReloadSeconds );
                                buffer.Add( "秒。" );
                            }
                            else
                            {
                                buffer.Add( "<color=#f25e1c>WEAPON JAMMER</color>: Shots from the above weapon add to the reload times of enemies they hit by <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.EnemyWeaponReloadSlowingSecondsPerShot );
                                buffer.Add( "秒</color> 如果目标护甲厚度低于 <color=#ffdf72>" );
                                buffer.Add( systemData.EnemyWeaponReloadSlowingSecondsArmor_mmLessThan );
                                buffer.Add( "毫米</color>。每个目标可施加的额外装弹时间总量为 " );
                                if ( systemData.MaxEnemyWeaponReloadSlowingSeconds > 0 )
                                    buffer.Add( systemData.MaxEnemyWeaponReloadSlowingSeconds );
                                else
                                    buffer.Add( ExternalConstants.Instance.MaxWeaponAddedReloadSeconds );
                                buffer.Add( "秒。" );
                            }
                        }
                        #endregion

                        debugStage = 270;

                        #region PercentDamageBypassesPersonalShields
                        if ( systemStats.PercentDamageBypassesPersonalShields > FInt.Zero )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#f25e1c>FUSION REACTION</color>: <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemStats.PercentDamageBypassesPersonalShields.ToFloatNonSim() * 100f ) );
                                buffer.Add( "%</color> direct to target hull.  " );
                            }
                            else if ( !systemData.FiresThroughEnemyShields )
                            {
                                buffer.Add( "<color=#f25e1c>FUSION REACTION</color>: Shots from the above weapon do <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemStats.PercentDamageBypassesPersonalShields.ToFloatNonSim() * 100f ) );
                                buffer.Add( "%</color> of their damage directly to the hull of their target, bypassing any personal shields (NOT bubble forcefields).  " );
                            }
                            else
                            {
                                buffer.Add( "<color=#f25e1c>FUSION REACTION</color>: Shots from the above weapon do <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemStats.PercentDamageBypassesPersonalShields.ToFloatNonSim() * 100f ) );
                                buffer.Add( "%</color> of their damage directly to the hull of their target, bypassing any personal shields.  " );
                            }
                        }
                        #endregion

                        debugStage = 280;

                        #region KnockbackToTarget
                        if ( systemData.KnockbackPerShotToShipsMass_tXLessThan > 0 )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );
                            #region Abbreviated
                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#f25e1c>KNOCKBACK</color>: " );
                                if ( systemStats.KnockbackPerShot > 0 )
                                {
                                    if ( !systemData.KnockbackAtTargetLocation )
                                    {
                                        buffer.Add( "目标被推开 <color=#ffdf72>" );
                                        buffer.AddNumberMoreReadable( systemStats.KnockbackPerShot );
                                        buffer.Add( "</color> away from this ship if mass <= <color=#ffdf72>" );
                                        buffer.Add( systemData.KnockbackPerShotToShipsMass_tXLessThan );
                                        buffer.Add( " tX</color>. Knockback decreases with higher mass.  " );
                                    }
                                    else
                                    {
                                        buffer.Add( "被AOE击中的目标被推开 <color=#ffdf72>" );
                                        buffer.AddNumberMoreReadable( systemStats.KnockbackPerShot );
                                        buffer.Add( "</color> away from the AoE center if mass <= <color=#ffdf72>" );
                                        buffer.Add( systemData.KnockbackPerShotToShipsMass_tXLessThan );
                                        buffer.Add( " tX</color>. Knockback decreases with higher mass.  " );
                                    }
                                }
                                else
                                {
                                    if ( !systemData.KnockbackAtTargetLocation )
                                    {
                                        buffer.Add( "目标被拉向 <color=#ffdf72>" );
                                        buffer.AddNumberMoreReadable( systemStats.KnockbackPerShot );
                                        buffer.Add( "</color> towards this ship if mass <= <color=#ffdf72>" );
                                        buffer.Add( systemData.KnockbackPerShotToShipsMass_tXLessThan );
                                        buffer.Add( " tX</color>. Knockback decreases with higher mass.  " );
                                    }
                                    else
                                    {
                                        buffer.Add( "被AOE击中的目标被拉向 <color=#ffdf72>" );
                                        buffer.AddNumberMoreReadable( systemStats.KnockbackPerShot );
                                        buffer.Add( "</color> towards the AoE center if mass <= <color=#ffdf72>" );
                                        buffer.Add( systemData.KnockbackPerShotToShipsMass_tXLessThan );
                                        buffer.Add( " tX</color>. Knockback decreases with higher mass.  " );
                                    }
                                }
                            }
                            #endregion

                            #region Non-Abbreviated
                            else
                            {
                                buffer.Add( "<color=#f25e1c>KNOCKBACK</color>: Enemies hit by this weapon are " );
                                if ( systemStats.KnockbackPerShot > 0 )
                                    buffer.Add( "被推开远离 " );
                                else
                                    buffer.Add( "被拉向 " );
                                if ( !systemData.KnockbackAtTargetLocation )
                                    buffer.Add( "此舰船。" );
                                else
                                    buffer.Add( "此舰船射击的AOE中心。" );
                                if ( systemStats.KnockbackPerShot > 0 )
                                    buffer.Add( "舰船可被推开的距离上限为 <color=#ffdf72>" );
                                else
                                    buffer.Add( "舰船可被拉近的距离上限为 <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.KnockbackPerShot );
                                buffer.Add( "</color>, decreasing as the target's mass approaches the max mass of <color=#ffdf72>" );
                                buffer.Add( systemData.KnockbackPerShotToShipsMass_tXLessThan );
                                buffer.Add( "倍</color>。" );
                            }
                            #endregion
                        }
                        #endregion

                        debugStage = 285;

                        #region DamageAmplification
                        if ( systemData.DamageAmplification && systemData.DamageAmplificationDuration_Max15 > 0 && systemStats.DamageAmplificationFlat > 0 )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#f25e1c>DAMAGE AMPLIFICATION</color>: <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemStats.DamageAmplificationFlat ) );
                                buffer.Add( "</color> extra damage taken by target for <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemData.DamageAmplificationDuration_Max15 ) );
                                buffer.Add( "</color>s.  " );
                            }
                            else
                            {
                                buffer.Add( "<color=#f25e1c>DAMAGE AMPLIFICATION</color>: Shots from the above weapon cause the target to take <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemStats.DamageAmplificationFlat ) );
                                buffer.Add( "</color> more damage from every shot that hits it for <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemData.DamageAmplificationDuration_Max15 ) );
                                buffer.Add( "</color> seconds.  " );
                            }
                        }

                        if ( systemData.DamageAmplification && systemData.DamageAmplificationDuration_Max15 > 0 && systemData.DamageAmplificationMult != 1 )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#f25e1c>DAMAGE AMPLIFICATION</color>: Target takes <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemData.DamageAmplificationMult.ToFloatNonSim() * 100f ) );
                                buffer.Add( "%</color> of normal damage for <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemData.DamageAmplificationDuration_Max15 ) );
                                buffer.Add( "</color>s.  " );
                            }
                            else
                            {
                                buffer.Add( "<color=#f25e1c>DAMAGE AMPLIFICATION</color>: Shots from the above weapon cause the target to take <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemData.DamageAmplificationMult.ToFloatNonSim() * 100f ) );
                                buffer.Add( "%</color> of normal damage from every shot that hits it for <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemData.DamageAmplificationDuration_Max15 ) );
                                buffer.Add( "</color> seconds.  " );
                            }
                        }
                        #endregion

                        debugStage = 290;

                        #region Vampirism/SelfDamage //Damage-based Vampirism/Self-Damage
                        if ( systemData.HealthChangePerDamageDealt != FInt.Zero )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                if ( systemData.HealthChangePerDamageDealt > FInt.Zero )
                                {
                                    buffer.Add( "<color=#f25e1c>VAMPIRISM</color>: repairs <color=#ffdf72>" );
                                }
                                else
                                {
                                    buffer.Add( "<color=#f25e1c>SELF DAMAGE</color>: damages itself by <color=#ffdf72>" );
                                }
                                buffer.AddNumberMoreReadable( systemData.HealthChangePerDamageDealt );
                                buffer.Add( " health</color> per damage dealt.  " );
                            }
                            else
                            {
                                if ( systemData.HealthChangePerDamageDealt > FInt.Zero )
                                {
                                    buffer.Add( "<color=#f25e1c>VAMPIRISM</color>: repairs itself by <color=#ffdf72>" );
                                }
                                else
                                {
                                    buffer.Add( "<color=#f25e1c>SELF DAMAGE</color>: damages itself by <color=#ffdf72>" );
                                }
                                buffer.AddNumberMoreReadable( systemData.HealthChangePerDamageDealt );
                                buffer.Add( " health</color> for every 1 damage it has dealt.  " );
                            }
                        }

                        //Max-Health-based Vampirism/Self-Damage
                        if ( systemData.HealthChangeByMaxHealthDividedByThisPerAttack != FInt.Zero  && !isOnDeath )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                if ( systemData.HealthChangeByMaxHealthDividedByThisPerAttack >= FInt.One * -1 && systemData.HealthChangeByMaxHealthDividedByThisPerAttack < FInt.Zero )
                                {
                                    buffer.Add( "<color=#f25e1c>SELF-DESTRUCT</color>: kills itself to attack.  " );
                                }
                                else
                                {
                                    if ( systemData.HealthChangeByMaxHealthDividedByThisPerAttack > FInt.Zero )
                                    {
                                        buffer.Add( "<color=#f25e1c>SELF-ASSEMBLY</color>: repairs <color=#ffdf72>" );
                                    }
                                    else
                                    {
                                        buffer.Add( "<color=#f25e1c>DISASSEMBLY</color>: damages itself by <color=#ffdf72>" );
                                    }
                                    buffer.AddPercentFormated( ( 1 / systemData.HealthChangeByMaxHealthDividedByThisPerAttack).ToPercent( 1 ) );
                                    buffer.Add( " health</color> each attack.  " );
                                }
                            }
                            else
                            {
                                if ( systemData.HealthChangeByMaxHealthDividedByThisPerAttack >= FInt.One * -1 && systemData.HealthChangeByMaxHealthDividedByThisPerAttack < FInt.Zero )
                                {
                                    buffer.Add( "<color=#f25e1c>SELF-DESTRUCT</color>: kills itself to attack.  " );
                                }
                                else
                                {
                                    if ( systemData.HealthChangeByMaxHealthDividedByThisPerAttack > FInt.Zero )
                                    {
                                        buffer.Add( "<color=#f25e1c>SELF-ASSEMBLY</color>: repairs itself by <color=#ffdf72>" );
                                    }
                                    else
                                    {
                                        buffer.Add( "<color=#f25e1c>DISASSEMBLY</color>: damages itself by <color=#ffdf72>" );
                                    }
                                    buffer.AddPercentFormated( (1 / systemData.HealthChangeByMaxHealthDividedByThisPerAttack).ToPercent( 1 ) );
                                    buffer.Add( " health</color> each attack.  " );
                                }
                            }
                        }
                        #endregion

                        debugStage = 300;

                        for ( int k = 0; k < systemData.OutgoingDamageModifiers_FullList.Count; k++ )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );
                            Window_PrototypeInGameHoverEntityInfo.WriteDamageModifierData( buffer, effectiveMarkLevel,
                                systemData.OutgoingDamageModifiers_FullList[k], systemStats );
                        }

                        if ( haveDoneNewLineAndSize )
                            buffer.Add( "</size>" );
                    }
                } //end "must be weapon" IF statement
                #endregion If A Weapon
                else //if not a weapon
                {
                    if ( OnlyWriteIfWeapon )
                    {
                        //buffer.Add( "OnlyWriteIfWeapon" );
                        return;
                    }

                    if ( systemData.ModuleStatAdjuster != ModularStatAdjustment.None )
                    {
                        buffer.Add( "<color=#ffee63>" ).Add( systemData.DisplayName ).Add( ":</color> <color=#eee8b2>" );
                        switch ( systemData.ModuleStatAdjuster )
                        {
                            case ModularStatAdjustment.HullHealth:
                                if ( detailLevel < TooltipDetail.Full )
                                    buffer.AddFixedDecimal( systemData.ModuleStatAdjusterMultiplier.ToFloatNonSim(), 3 ).Add( "倍" );
                                else
                                    buffer.Add( "船体生命值倍率 ").AddFixedDecimal( systemData.ModuleStatAdjusterMultiplier.ToFloatNonSim(), 3 ).Add( "倍" );
                                break;
                            case ModularStatAdjustment.ShieldHealth:
                                if ( detailLevel < TooltipDetail.Full )
                                    buffer.AddFixedDecimal( systemData.ModuleStatAdjusterMultiplier.ToFloatNonSim(), 3 ).Add( "倍" );
                                else
                                    buffer.Add( "护盾生命值倍率 " ).AddFixedDecimal( systemData.ModuleStatAdjusterMultiplier.ToFloatNonSim(), 3 ).Add( "倍" );
                                break;
                            case ModularStatAdjustment.BubbleForcefield:
                                if ( systemData.ModuleStatAdjusterMultiplier != FInt.One )
                                {
                                    if ( detailLevel < TooltipDetail.Full )
                                        buffer.AddFixedDecimal( systemData.ModuleStatAdjusterMultiplier.ToFloatNonSim(), 3 ).Add( "倍" );
                                    else
                                        buffer.Add( "气泡力场添加，" ).AddFixedDecimal( systemData.ModuleStatAdjusterMultiplier.ToFloatNonSim(), 3 ).Add( "倍正常个人护盾评级。" );
                                }
                                else
                                {
                                    if ( detailLevel >= TooltipDetail.Full )
                                        buffer.Add( "气泡力场替换个人护盾；护盾强度不变。" );
                                }
                                break;
                        }
                        buffer.Add( "</color>  " );
                    }
                    else
                    {
                        //not a module stat adjuster
                        if ( OnlyWriteIfModuleStatBooster )
                        {
                            //buffer.Add( "OnlyWriteIfModuleStatBooster 2" );
                            return;
                        }
                    }

                    #region Tachyon System
                    if ( systemData.TachyonHitsAlbedoLessThan > FInt.Zero || systemData.TachyonHitsAlbedoMoreThan > FInt.Zero )
                    {
                        float secondsPerSimFrame = World_AIW2.Instance.SimulationProfile.SecondsPerFrameNonSim;
                        float simFrameMultiplier = 1f / secondsPerSimFrame;

                        if ( detailLevel < TooltipDetail.Full )
                        {
                            buffer.Add( "<color=#f25e1c>DE-CLOAKER</color>: range <color=#ffdf72>" );
                            buffer.AddNumberMoreReadable( systemStats.TachyonRange );
                            buffer.Add( "</color>, strength <color=#ffdf72>" );
                            //we use simFrameMultiplier to get the actual number per second, rather than the number per frame
                            buffer.AddNumberMoreReadable( Mathf.RoundToInt( systemStats.TachyonPoints * simFrameMultiplier ) );
                            buffer.Add( "</color>, " );

                            WriteMinMaxOrVariant( buffer, "any albedo", "only albedo", true, "ffdf72", systemData.TachyonHitsAlbedoMoreThan, systemData.TachyonHitsAlbedoLessThan );
                        }
                        else
                        {
                            buffer.Add( "<color=#f25e1c>DE-CLOAKER</color>: Cloaked enemy ships within a range of <color=#ffdf72>" );
                            buffer.AddNumberMoreReadable( systemStats.TachyonRange );
                            buffer.Add( "</color> are saturated with a tachyon field that drains <color=#ffdf72>" );
                            //we use simFrameMultiplier to get the actual number per second, rather than the number per frame
                            buffer.AddNumberMoreReadable( Mathf.RoundToInt( systemStats.TachyonPoints * simFrameMultiplier ) );
                            buffer.Add( "</color> cloaking points per second from enemy ships with " );

                            WriteMinMaxOrVariant( buffer, "any albedo", "an albedo", true, "ffdf72", systemData.TachyonHitsAlbedoMoreThan, systemData.TachyonHitsAlbedoLessThan );
                        }
                        if ( systemData.CareAboutStateOfMatterToBeEnabled )
                            WriteSystemStateOfMatterSuffix( buffer, systemData, relatedSquadOrNull );
                    }
                    #endregion Tachyon System
                    #region Cloaking System
                    else if ( systemStats.CloakingPoints > 0 )
                    {
                        FInt maxWeaponCloakingReductionCost = FInt.Zero;
                        for ( int i = 0; i < systemData.ParentEntityTypeData.SystemTypes.Count; i++ )
                        {
                            EntitySystemTypeData otherSystem = systemData.ParentEntityTypeData.SystemTypes[i];
                            if ( otherSystem.MaxMarkLevelToFunction < effectiveMarkLevel || otherSystem.MinMarkLevelToFunction > effectiveMarkLevel || otherSystem.SystemIsHiddenForUI )
                                continue;
                            if ( otherSystem.CareAboutStateOfMatterToBeEnabled && otherSystem.IsInvisibleInTooltipsIfNotAMatchByStateOfMatter )
                            {
                                if ( !relatedSquadOrNull.IsFakeEntity && relatedSquadOrNull.CurrentStateOfMatter != otherSystem.MustBeThisStateOfMatterToBeEnabled )
                                    continue; //skip completely, since this will be invisible AND disabled
                            }
                            if ( otherSystem.IsModule && !otherSystem.IsModuleOn( relatedMembershipOrNull ) )
                                continue;
                            if ( otherSystem.Category == EntitySystemCategory.Weapon )
                            {
                                if ( maxWeaponCloakingReductionCost < otherSystem.CloakingPercentLossFromFiring )
                                    maxWeaponCloakingReductionCost = otherSystem.CloakingPercentLossFromFiring;
                            }
                        }

                        Color cloakColor = ColorMath.PaleVioletRed;
                        if ( detailLevel < TooltipDetail.Full )
                        {
                            buffer.Add( "最大隐形点数：<color=#ffdf72>" );
                            buffer.AddNumberMoreReadable( systemStats.CloakingPoints );
                            buffer.Add( "</color>.  " );
                        }
                        else
                        {
                            buffer.Add( "隐形", cloakColor ).Add( "：此舰船有 <color=#ffdf72>" );
                            buffer.AddNumberMoreReadable( systemStats.CloakingPoints );
                            buffer.Add( "</color> max cloaking points.  As long as its current cloaking points are above zero, it will be invisible to enemies (although detected).  " );
                            if ( systemData.ParentEntityTypeData.IsCombatant )
                                buffer.Add( "此舰船每次开火将消耗 " )
                                    .Add( Mathf.RoundToInt( maxWeaponCloakingReductionCost.ToFloatNonSim() * 100f ) )
                                    .Add( "% 的隐形点数。" );
                            buffer.Add( "在 " ).Add( ExternalConstants.Instance.SecondsToWaitBeforeRecloaking ).Add( " 秒未损失隐形点数后，此舰船将恢复所有损失的点数。" );
                        }
                        if ( systemData.CareAboutStateOfMatterToBeEnabled )
                            WriteSystemStateOfMatterSuffix( buffer, systemData, relatedSquadOrNull );
                    }
                    #endregion Cloaking System
                    #region Tractor System
                    else if ( systemStats.TractorCount > 0 )
                    {
                        if ( systemData.IsReverseTractorBeam )
                        {
                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#ffdf72>" );
                                buffer.Add( systemStats.TractorCount );
                                buffer.Add( "</color>x <color=#f25e1c>REVERSE TRACTOR BEAMS</color>: range <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.TractorRange );
                                buffer.Add( "</color>" );
                                WriteTractorRangeInfo( buffer, systemData, false );
                            }
                            else
                            {
                                buffer.Add( "<color=#f25e1c>REVERSE TRACTOR BEAMS</color>: A maximum of <color=#ffdf72>" );
                                buffer.Add( systemStats.TractorCount );
                                buffer.Add( "</color> enemy units can be grappled, pulling this unit with them, if they are within <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.TractorRange );
                                buffer.Add( "</color> range" );
                                WriteTractorRangeInfo( buffer, systemData, true );
                                buffer.Add( "它们仍可自由移动，拖着此单位，但无法离开当前星球。" );
                            }
                        }
                        else
                        {
                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#ffdf72>" );
                                buffer.Add( systemStats.TractorCount );
                                buffer.Add( "</color>x <color=#f25e1c>TRACTOR BEAMS</color>: range <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.TractorRange );
                                buffer.Add( "</color>" );
                                WriteTractorRangeInfo( buffer, systemData, false );
                            }
                            else
                            {
                                buffer.Add( "<color=#f25e1c>TRACTOR BEAMS</color>: A maximum of <color=#ffdf72>" );
                                buffer.Add( systemStats.TractorCount );
                                buffer.Add( "</color> enemy squads can be frozen in place if they are within <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.TractorRange );
                                buffer.Add( "</color> range" );
                                WriteTractorRangeInfo( buffer, systemData, true );
                            }
                        }
                        if ( systemData.CareAboutStateOfMatterToBeEnabled )
                            WriteSystemStateOfMatterSuffix( buffer, systemData, relatedSquadOrNull );
                    }
                    #endregion Tractor System
                    #region Gravity System
                    else if ( systemData.GravityHitsEngine_gxLessThan > 0 )
                    {
                        buffer.Add( "<color=#f25e1c>GRAVITY FIELD</color>: ");
                        if ( detailLevel < TooltipDetail.Full )
                        {
                            if (systemStats.GravityRange > 99999) {
                                buffer.Add("无限范围" );
                            } else {
                                buffer.Add("范围 " );
                                buffer.AddNumberMoreReadable( systemStats.GravityRange, "ffdf72" );
                            }
                            buffer.Add( ", 减速至 <color=#ffdf72>" );
                            buffer.Add( systemStats.GravitySpeedMultiplier.ReadableString );
                            buffer.Add( "倍</color>，仅目标引擎 < <color=#ffdf72>" );
                            buffer.Add( systemData.GravityHitsEngine_gxLessThan );
                            buffer.Add( " gx</color>.  " );
                        }
                        else
                        {
                            if (systemStats.GravityRange > 99999) {
                                buffer.Add( "星球上所有敌方小队" );
                            } else {
                                buffer.Add( "范围内所有敌方小队 " );
                                buffer.AddNumberMoreReadable( systemStats.GravityRange, "ffdf72" );
                            }
                            buffer.Add( " 被减速至 <color=#ffdf72>" );
                            buffer.Add( systemStats.GravitySpeedMultiplier.ReadableString );
                            buffer.Add( "倍</color> 正常速度，如果引擎动力低于 <color=#ffdf72>" );
                            buffer.Add( systemData.GravityHitsEngine_gxLessThan );
                            buffer.Add( " gx</color>.  " );
                        }
                        if ( systemData.CareAboutStateOfMatterToBeEnabled )
                            WriteSystemStateOfMatterSuffix( buffer, systemData, relatedSquadOrNull );
                    }
                    #endregion Gravity System
                    #region Attractant System
                    else if ( systemStats.AttractRangeForShotsAgainstAllies > 0 )
                    {
                        buffer.Add( "<color=#f25e1c>ATTRACTANT FIELD</color>: All shots fired against friendly units within range <color=#ffdf72>" );
                        buffer.AddNumberMoreReadable( systemStats.AttractRangeForShotsAgainstAllies );
                        buffer.Add( "</color> are automatically redirected at this unit, instead.  " );
                        if ( systemData.CareAboutStateOfMatterToBeEnabled )
                            WriteSystemStateOfMatterSuffix( buffer, systemData, relatedSquadOrNull );
                    }
                    #endregion Attractant System
                }
            }
            catch ( Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in WriteSystemInfo at debugStage: " + debugStage  + "\n Exception: " + e, Verbosity.ShowAsError );
            }
        } //end WriteSystemInfo
        #endregion WriteSystemInfo

        private static void WriteSystemLastDamageInfo( ArcenCharacterBufferBase buffer, EntitySystem relatedSystem )
        {
            buffer.StartColor( QuickColors.OldValue ).AddNumberMoreReadable( (World_AIW2.Instance.GameSecond - relatedSystem.LastGameSecondIFiredShot) )
                .Add( "s" ).EndColor().Add( " Since Shot Hit (" ).StartColor( QuickColors.OldValue )
                .AddNumberMoreReadable( relatedSystem.LastTotalDamageMyShotDidCaused ).Add( " DMG" ).EndColor().Add( ")  " );
            if ( relatedSystem.LastDamageAbortCode != 0 || relatedSystem.LastTotalDamageMyShotDidCaused == 0 )
            {
                buffer.StartColor( QuickColors.OldValue ).Add( "DMG ABORT: " );
                switch ( (int)relatedSystem.LastDamageAbortCode )
                {
                    case 1:
                        buffer.Add( "Immune to All Damage" );
                        break;
                    case 2:
                        buffer.Add( "Newly-Created Immunity To Damage" );
                        break;
                    case 3:
                        buffer.Add( "External Invulnerability" );
                        break;
                    case 4:
                        buffer.Add( "foundProtectorButCouldNotHitDueToFiniteHitCountAOE" );
                        break;
                    case 5:
                        buffer.Add( "Debug_IgnoresDamage" );
                        break;
                    case 6:
                        buffer.Add( "HonorFiniteHitCountAOE and not in list" );
                        break;
                    case 7:
                        buffer.Add( "Calculated Zero Damage!" );
                        break;
                    case 8:
                        buffer.Add( "Damage-Drop-During-Hit" );
                        break;
                    case 9:
                        buffer.Add( "Health-Of-Target-Zero" );
                        break;
                    case 10:
                        buffer.Add( "Overdrives-Shields-No-Shields" );
                        break;
                    case 11:
                        buffer.Add( "Shooting Dead Target" );
                        break;
                    default:
                        buffer.Add( "CODE " ).Add( (int)relatedSystem.LastDamageAbortCode );
                        break;
                }
                buffer.EndColor().Add( "  " );
            }
        }

        private static void WriteSystemLastFireAbortCode( ArcenCharacterBufferBase buffer, EntitySystem relatedSystem )
        {
            if ( relatedSystem.LastShotFireAbortCode > 0 )
            {
                buffer.StartColor( QuickColors.OldValue ).Add( "FIRE ABORT: " );
                switch ( (int)relatedSystem.LastShotFireAbortCode )
                {
                    case 1:
                        buffer.Add( "Only Fires On Death" );
                        break;
                    case 2:
                        buffer.Add( "Maintain Cloak When No Direct Target" );
                        break;
                    case 3:
                        buffer.Add( "Empty Target List" );
                        break;
                    case 4:
                        buffer.Add( "All Targets Out Of Range" );
                        break;
                    case 5:
                        buffer.Add( "No Viable Targets" );
                        break;
                    default:
                        buffer.Add( "CODE " ).Add( (int)relatedSystem.LastShotFireAbortCode );
                        break;
                }
                buffer.EndColor().Add( "  " );
            }
        }

        private static void WriteCrippledInfo( ArcenCharacterBufferBase buffer, GameEntity_Squad relatedSquadOrNull )
        {
            buffer.StartColor( QuickColors.OldValue );
            if ( !relatedSquadOrNull.TypeData.ImmuneToRepairs )
            {
                FInt extraCost = relatedSquadOrNull.TypeData.GetExtraCostWhileCrippled();
                if ( extraCost > FInt.One )
                    buffer.Add( "重创 - 不会死亡，但需要修复（" ).AddFixedDecimal( extraCost.ToFloatNonSim(), 2 ).Add( "倍正常费用）至满血才能恢复功能！" );
                else
                    buffer.Add( "重创 - 不会死亡，但需要修复至满血才能恢复功能！" );
            }
            else
                buffer.Add( "重创 - 不会死亡，但需要修复至满血才能恢复功能！" );
            buffer.EndColor();
        }

        private static bool WritePlannedMetalFlowBriefTargetInfo( ArcenCharacterBufferBase buffer, PlannedMetalFlow flow, string Prefix, bool WriteFull )
        {
            if ( flow.FromEntity == null )
                return false;
            GameEntity_Squad squad = flow.SquadRecipient;
            if ( squad == null )
                return false;

            buffer.StartColor( "70ff59" ); //bright green
            buffer.Add( " (" );
            if ( Prefix != null && Prefix.Length > 0 )
                buffer.Add( Prefix ).Add( ": " );
            buffer.StartColor( squad.GetFactionCenterColorHexBrighter_Safe() );
            buffer.Add( WriteFull ? squad.TypeData.DisplayName : squad.FleetMembership?.GetDisplayNameForSidebar() ).EndColor();
            if ( WriteFull && squad.CurrentMarkLevel > 0 )
                buffer.Add( " " ).StartColor( squad.DataForMark.MarkLevel.ColorHex ).Add( squad.DataForMark.MarkLevel.Abbreviation ).EndColor();
            buffer.Add( ")" ).EndColor();
            return true;
        }

        private static bool WritePlannedMetalFlowFullTargetInfo( ArcenCharacterBufferBase buffer, PlannedMetalFlow flow, GameEntity_Squad entity, string Prefix, bool WriteFull )
        {
            if ( flow.FromEntity == null )
            {
                buffer.Add( "[null flow]" );
                return false;
            }
            GameEntity_Squad squad = flow.SquadRecipient;
            if ( Prefix != null && Prefix.Length > 0 )
                buffer.Add( Prefix ).Add( ": " );

            bool addedRecipient = false;
            if ( squad != null )
            {
                buffer.StartColor( squad.GetFactionCenterColorHexBrighter_Safe() );
                buffer.Add( WriteFull ? squad.TypeData.DisplayName : squad.FleetMembership?.GetDisplayNameForSidebar() ).EndColor();
                if ( WriteFull && squad.CurrentMarkLevel > 0 ) {
                    buffer.Add( " " ).StartColor( squad.DataForMark.MarkLevel.ColorHex ).Add( squad.DataForMark.MarkLevel.Abbreviation ).EndColor();
                }
                if ( WriteFull && GameSettings.Current.GetBoolBySetting("Debug_Tooltip") ) {
                    buffer.Add(" (dist ").Add( flow.FromEntity.GetDistanceTo_ExpensiveAccurate(squad, RadiusCheck.SubtractRadiiFromDistance, true) ).Add(")");
                }
                addedRecipient = true;
            }
            switch ( flow.Purpose )
            {
                case MetalFlowPurpose.FactoryConstructionForPlayerMobileFleets:
                    {
                        if ( entity == null )
                            break;
                        PlanetFaction pFaction = entity.PlanetFaction;
                        if ( pFaction == null )
                            break;
                        DoubleBufferedList<FleetMembership> factoryTargets = pFaction.FactoryBoostsBySpecialFactoryType.GetListForOrNull( entity.TypeData.SpecialFactoryType );
                        if ( factoryTargets == null || factoryTargets.Count == 0 )
                            break;
                        List<FleetMembership> factoryTargetsFinal = factoryTargets.GetDisplayList();
                        if ( factoryTargetsFinal == null || factoryTargetsFinal.Count == 0 )
                            break;

                        for ( int i = 0; i < factoryTargetsFinal.Count; i++ )
                        {
                            FleetMembership fMem = factoryTargetsFinal[i];
                            if ( i > 0 )
                                buffer.Add( ", " );
                            buffer.StartColor( fMem.Fleet.Faction.FactionCenterColor.ColorHexBrighter );
                            buffer.Add( WriteFull ? fMem.TypeData.DisplayName : fMem.GetDisplayNameForSidebar() ).EndColor();
                            addedRecipient = true;
                        }
                    }
                    break;
            }
            if ( !addedRecipient )
                buffer.Add( "[no recipients]" );
            return true;
        }

        private static void WriteTractorRangeInfo( ArcenCharacterBufferBase buffer, EntitySystemTypeData tractorSystem, bool WriteFull )
        {
            bool haveWrittenFirst = false;

            if ( WriteSingleStatPairInfo( buffer, tractorSystem.TractorHitsAlbedoLessThan, tractorSystem.TractorHitsAlbedoGreaterThan, FInt.One, WriteFull,
                WriteFull ? "an albedo" : "albedo", string.Empty, haveWrittenFirst ) )
                haveWrittenFirst = true;

            if ( WriteSingleStatPairInfo( buffer, (FInt)tractorSystem.TractorHitsEngine_gxLessThan, (FInt)tractorSystem.TractorHitsEngine_gxGreaterThan, (FInt)999, WriteFull,
                WriteFull ? "an engine power" : "engine", " gx", haveWrittenFirst ) )
                haveWrittenFirst = true;

            if ( WriteSingleStatPairInfo( buffer, tractorSystem.TractorHitsMassLessThan, tractorSystem.TractorHitsMassGreaterThan, (FInt)999, WriteFull,
                WriteFull ? "a mass" : "mass", " tX", haveWrittenFirst ) )
                haveWrittenFirst = true;

            buffer.Add( ".  " );
        }

        private static bool WriteSingleStatPairInfo( ArcenCharacterBufferBase buffer, FInt lessThanVal, FInt greaterThanVal, FInt OutOfRangeValue, bool WriteFull, 
            string UnitPrefix, string UnitSuffix, bool WriteAndInsteadOfComma )
        {
            bool writeLessThan = lessThanVal > FInt.Zero && lessThanVal < OutOfRangeValue;
            bool writeGreaterThan = greaterThanVal > FInt.Zero && greaterThanVal < OutOfRangeValue;
            if ( !writeLessThan && !writeGreaterThan )
                return false;
            if ( WriteFull )
            {
                if ( WriteAndInsteadOfComma )
                    buffer.Add( " and " );
                else
                    buffer.Add( ", if they have " );
            }
            else
            {
                if ( WriteAndInsteadOfComma )
                    buffer.Add( " and " );
                else
                    buffer.Add( ", only target " );
            }
            buffer.Add( UnitPrefix );

            if ( writeLessThan )
            {
                if ( writeGreaterThan )
                {
                    if ( WriteFull )
                    {
                        buffer.Add( " between " );
                    }
                    else
                    {

                    }
                    buffer.Add( "<color=#ffdf72>" );
                    buffer.AddFixedDecimal( greaterThanVal.ToFloatNonSim(), 2 );
                    buffer.Add( "-" );
                    buffer.AddFixedDecimal( lessThanVal.ToFloatNonSim(), 2 );
                    buffer.Add( UnitSuffix );
                    buffer.Add( "</color>" );
                }
                else //no range, just less than
                {
                    if ( WriteFull )
                    {
                        buffer.Add( " < " );

                    }
                    else
                    {
                        buffer.Add( " less than " );
                    }
                    buffer.Add( "<color=#ffdf72>" );
                    buffer.AddFixedDecimal( lessThanVal.ToFloatNonSim(), 2 );
                    buffer.Add( UnitSuffix );
                    buffer.Add( "</color>" );
                }
            }
            else //no range, just greater than
            {
                if ( WriteFull )
                {
                    buffer.Add( " > " );
                }
                else
                {
                    buffer.Add( " greater than " );
                }
                buffer.Add( "<color=#ffdf72>" );
                buffer.AddFixedDecimal( greaterThanVal.ToFloatNonSim(), 2 );
                buffer.Add( UnitSuffix );
                buffer.Add( "</color>" );
            }
            return true;
        }

        private static string GetProtectorString( SpecialEntityType Type, string Tag, int Count )
        {
            bool shouldBePlural = Count > 1;
            string protectorString = "";
            if ( Type != SpecialEntityType.None )
            {
                switch ( Type )
                {
                    case SpecialEntityType.GuardPost:
                        if ( shouldBePlural )
                            protectorString = "guard posts";
                        else
                            protectorString = "guard post";
                        break;
                    case SpecialEntityType.DireGuardPost:
                        if ( shouldBePlural )
                            protectorString = "dire guard posts";
                        else
                            protectorString = "dire guard post";
                        break;
                    default:
                        if ( shouldBePlural )
                            protectorString = Type.ToString() + "s";
                        else
                            protectorString = Type.ToString();
                        break;
                }
            }
            else
            {
                if ( Tag == "AICommandStationOriginal" )
                {
                    protectorString = "AI Command Station";
                }
                else
                {
                    if ( shouldBePlural )
                        protectorString = Tag + "s";
                    else
                        protectorString = Tag;
                }
            }
            return protectorString;
        }

        private static void WriteSystemStateOfMatterSuffix( ArcenCharacterBufferBase buffer, EntitySystemTypeData systemData, GameEntity_Squad relatedSquadOrNull )
        {
            if ( systemData.CareAboutStateOfMatterToBeEnabled )
            {
                buffer.Add( "必须为 " ).Add( systemData.MustBeThisStateOfMatterToBeEnabled.DisplayName ).Add( " 物质状态才能运作。" );
                if ( !relatedSquadOrNull.IsFakeEntity && !systemData.IsInvisibleInTooltipsIfNotAMatchByStateOfMatter )
                {
                    if ( relatedSquadOrNull.CurrentStateOfMatter != systemData.MustBeThisStateOfMatterToBeEnabled )
                        buffer.Add( "(Disabled)" );
                    else
                        buffer.Add( "(Enabled)" );
                }
            }
        }

        private static void WriteTechThatBenefits( ArcenCharacterBufferBase buffer, TechUpgrade upgrade, Faction localFactionOrNull, bool alsoShowShipLineCountWithSameTech, ref int debugStage )
        {
            debugStage = 15010101;
            TooltipDetail detailLevel = EntityText.Detail;
            int upgradesSoFar = localFactionOrNull == null ? 0 : localFactionOrNull.TechUnlocks[upgrade.RowIndexNonSim] + localFactionOrNull.FreeTechUnlocks[upgrade.RowIndexNonSim];//okay to use here, as it will be consistent per run
            debugStage = 15010102;
            int upgradeIndex = upgradesSoFar < 0 ? 0 : upgradesSoFar + 1;
            if ( upgradeIndex < 0 )
                upgradeIndex = 0;
            if ( upgradeIndex >= Balance_MarkLevelTable.Instance.RowsByOrdinal.Length )
                upgradeIndex = Balance_MarkLevelTable.Instance.RowsByOrdinal.Length - 1;

            List<int> upgradeCosts = upgrade.GetScienceOrOtherResourceCostsPerTimeUnlocked();

            Balance_MarkLevel markByOrdinal = Balance_MarkLevelTable.Instance.RowsByOrdinal[upgradeIndex];
            debugStage = 15010103;
            buffer.Add( upgrade.DisplayName ).Add( markByOrdinal.ColorHexStart ).Add( " (" ).Add(
              upgradesSoFar ).Add( "/" ).Add( upgradeCosts.Count );
            if ( alsoShowShipLineCountWithSameTech && (upgrade.UIOnly_Tech_ShipLinesAffected > 0 || upgrade.UIOnly_Tech_DefensiveLinesAffected > 0 ) )
            {
                buffer.Add( " - ");
                if ( detailLevel < TooltipDetail.Medium )
                    buffer.Add( upgrade.UIOnly_Tech_ShipLinesAffected.ToString(), ArcenExternalUIUtilities.ShipLineIncreaseColor ).Add(", ").Add( upgrade.UIOnly_Tech_DefensiveLinesAffected.ToString(),  ArcenExternalUIUtilities.DefenseLineIncreaseColor ).Add("");
                else if ( detailLevel < TooltipDetail.Medium )
                    buffer.Add( upgrade.UIOnly_Tech_ShipLinesAffected.ToString(), ArcenExternalUIUtilities.ShipLineIncreaseColor ).Add(" lines, ").Add( upgrade.UIOnly_Tech_DefensiveLinesAffected.ToString(),  ArcenExternalUIUtilities.DefenseLineIncreaseColor ).Add(" lines");
                else
                    buffer.Add( upgrade.UIOnly_Tech_ShipLinesAffected.ToString(), ArcenExternalUIUtilities.ShipLineIncreaseColor ).Add(" ship lines and ").Add( upgrade.UIOnly_Tech_DefensiveLinesAffected.ToString(),  ArcenExternalUIUtilities.DefenseLineIncreaseColor ).Add(" defensive lines");
            }
            buffer.Add( ")" ).EndColor();
        }

        private static readonly SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark> weakAgainst_Ships_ThatYouHave = 
            SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark>.Create_WillNeverBeGCed( 30, delegate { return new ShipDataByMark(); }, "Window_InGameHoverEntityInfo-weakAgainst_Ships_ThatYouHave" );
        
        private static readonly SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataForSingleMark> weakAgainst_Ships_ThatYouCanCapture = 
            SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataForSingleMark>.Create_WillNeverBeGCed( 30, delegate { return new ShipDataForSingleMark(); }, "Window_InGameHoverEntityInfo-weakAgainst_Ships_ThatYouCanCapture" );

        private static readonly SortedDictionary<GameEntityTypeData, RefThreeTuple<string, int, FInt>> weakAgainst_multipliersDealtByShipName = 
            SortedDictionary<GameEntityTypeData, RefThreeTuple<string, int, FInt>>.Create_WillNeverBeGCed( 300, "Window_InGameHoverEntityInfo-weakAgainst_multipliersDealtByShipName" );

        private static FInt CalculateMultipleOfMultiplier( DamageModifier playerShipDamageModifier, GameEntityTypeData ShipTypeData, byte mark )
        {
            if ( playerShipDamageModifier.NeedsToGetMultiples )
            {
                FInt multiples;
                multiples = playerShipDamageModifier.GetMultiplierForResourceForType( ShipTypeData, mark ) * playerShipDamageModifier.MultiplierForMark[mark];
                if ( playerShipDamageModifier.MaxMultiplier != int.MaxValue && multiples > playerShipDamageModifier.MaxMultiplier )
                    multiples = FInt.Create( playerShipDamageModifier.MaxMultiplier, true );
                if ( playerShipDamageModifier.MultiplierIsAdditive )
                    multiples += FInt.One; //add 1x base damage

                return multiples;
            }
            return FInt.One;
        }

        private static void WriteWeakAgainst( ArcenCharacterBufferBase buffer, GameEntityTypeData enemyShipTypeData, GameEntityTypeData.MarkLevelStats enemyShipMarkStats )
        {
            int debugStage = 0;
            try
            {
                debugStage = 100;
                #region Weak against
                debugStage = 200;

                ShipListerUtils.CalculateShipsThatYouHave( x => true, weakAgainst_Ships_ThatYouHave, false, true );
                debugStage = 300;
                ShipListerUtils.CalculateShipsThatYouCanCapture( x => true, weakAgainst_Ships_ThatYouCanCapture, null, false );

                int comparisonInt = 0;
                FInt comparisonFInt = FInt.Zero;
                bool forceDoesNotMeetCriteria = false;

                debugStage = 400;
                buffer.Add( "<color=#ff7150>\nYour ships have the following damage multipliers against this unit:</color>\n" );

                bool wroteAny = false;
                foreach ( KeyValuePair<GameEntityTypeData, ShipDataByMark> pair in weakAgainst_Ships_ThatYouHave )
                {
                    debugStage = 500;
                    GameEntityTypeData playerShipType = pair.Key;
                    byte maxMarkLevel = 0;
                    int countAcrossAllMarks = 0;
                    int singleLineCount = 0;
                    for ( byte i = 0; i < pair.Value.CountsByMarkLength(); i++ )
                    {
                        singleLineCount = pair.Value.GetCountByMark( i );
                        if ( singleLineCount > 0 )
                        {
                            maxMarkLevel = i;
                            countAcrossAllMarks += singleLineCount;
                        }
                    }
                    if ( countAcrossAllMarks <= 0 )
                        continue;

                    debugStage = 600;
                    for ( int j = 0; j < playerShipType.SystemTypes.Count; j++ )
                    {
                        debugStage = 700;
                        EntitySystemTypeData systemData = playerShipType.SystemTypes[j];
                        for ( int k = 0; k < systemData.OutgoingDamageModifiers_FullList.Count; k++ )
                        {
                            debugStage = 800;
                            DamageModifier playerShipDamageModifier = systemData.OutgoingDamageModifiers_FullList[k];
                            debugStage = 900;
                            if ( playerShipDamageModifier.CalculateDoesMeetCriteriaStatic( enemyShipTypeData, enemyShipMarkStats, ref comparisonInt, ref comparisonFInt, ref forceDoesNotMeetCriteria )
                                || playerShipDamageModifier.CalculateDoesMeetCriteriaTypeOnly( enemyShipTypeData, enemyShipMarkStats, ref comparisonInt, ref comparisonFInt, ref forceDoesNotMeetCriteria ) )
                            {
                                debugStage = 1000;
                                if ( !forceDoesNotMeetCriteria && playerShipDamageModifier.CompareWithValues( comparisonInt, comparisonFInt ) )
                                {
                                    debugStage = 1100;
                                    wroteAny = true;
                                    debugStage = 1200;
                                    GameEntityTypeData.MarkLevelStats shipMarkStats = playerShipType.MarkStatsFor( maxMarkLevel );
                                    RefThreeTuple<string, int, FInt> multiplier = null;
                                    debugStage = 1300;
                                    if ( !weakAgainst_multipliersDealtByShipName.TryGetValue( playerShipType, out multiplier ) )
                                    {
                                        debugStage = 1400;
                                        multiplier = RefThreeTuple<string, int, FInt>.Create(
                                            $"{playerShipType.DisplayName} <color=#{shipMarkStats.MarkLevel.ColorHex}>{shipMarkStats.MarkLevel.Abbreviation}</color>",
                                            0, FInt.Zero );
                                        weakAgainst_multipliersDealtByShipName[playerShipType] = multiplier;
                                    }
                                    debugStage = 1500;

                                    multiplier.SecondItem += countAcrossAllMarks;

                                    if ( multiplier.ThirdItem == FInt.Zero )
                                        multiplier.ThirdItem = playerShipDamageModifier.MultiplierForMark[maxMarkLevel];
                                    else
                                        multiplier.ThirdItem *= playerShipDamageModifier.MultiplierForMark[maxMarkLevel];
                                    multiplier.ThirdItem *= CalculateMultipleOfMultiplier( playerShipDamageModifier, enemyShipTypeData, maxMarkLevel );
                                }
                            }
                        }
                    }
                }

                debugStage = 3000;
                if ( !wroteAny )
                    buffer.Add( "无" );

                debugStage = 3100;
                WriteMultipliersDealtByShipName( buffer, null, weakAgainst_multipliersDealtByShipName );
                buffer.Add( "\n" );

                debugStage = 3200;
                buffer.Add( "<color=#ffe450>\nThese ships you could capture have the following damage multipliers against this unit:</color>\n" );

                wroteAny = false;

                debugStage = 4000;
                weakAgainst_multipliersDealtByShipName.Clear();
                debugStage = 4100;
                foreach ( KeyValuePair<GameEntityTypeData, ShipDataForSingleMark> pair in weakAgainst_Ships_ThatYouCanCapture )
                {
                    GameEntityTypeData shipType = pair.Key;
                    bool usesCaps = ShipListerUtils.GetUsesShipCaps( shipType );
                    debugStage = 4200;
                    for ( int j = 0; j < shipType.SystemTypes.Count; j++ )
                    {
                        debugStage = 4300;
                        EntitySystemTypeData systemData = shipType.SystemTypes[j];
                        for ( int k = 0; k < systemData.OutgoingDamageModifiers_FullList.Count; k++ )
                        {
                            debugStage = 4400;
                            DamageModifier damageModifier = systemData.OutgoingDamageModifiers_FullList[k];
                            debugStage = 4500;
                            if ( damageModifier.CalculateDoesMeetCriteriaStatic( enemyShipTypeData, enemyShipMarkStats, ref comparisonInt, ref comparisonFInt, ref forceDoesNotMeetCriteria )
                                || damageModifier.CalculateDoesMeetCriteriaTypeOnly( enemyShipTypeData, enemyShipMarkStats, ref comparisonInt, ref comparisonFInt, ref forceDoesNotMeetCriteria ) )
                            {
                                debugStage = 4600;
                                if ( !forceDoesNotMeetCriteria && damageModifier.CompareWithValues( comparisonInt, comparisonFInt ) )
                                {
                                    debugStage = 4700;
                                    wroteAny = true;    
                                    byte maxMarkLevel = pair.Value.MarkLevel;
                                    debugStage = 4800;
                                    GameEntityTypeData.MarkLevelStats shipMarkStats = shipType.MarkStatsFor( maxMarkLevel );
                                    debugStage = 4900;
                                    RefThreeTuple<string, int, FInt> multiplier = null;
                                    debugStage = 5000;
                                    if ( !weakAgainst_multipliersDealtByShipName.TryGetValue( shipType, out multiplier ) )
                                    {
                                        debugStage = 5100;  
                                        multiplier = RefThreeTuple<string, int, FInt>.Create(
                                            $"{shipType.DisplayName} <color=#{shipMarkStats.MarkLevel.ColorHex}>{shipMarkStats.MarkLevel.Abbreviation}</color>",
                                            0, FInt.Zero );
                                        debugStage = 5200;
                                        weakAgainst_multipliersDealtByShipName[shipType] = multiplier;
                                    }
                                    debugStage = 5300;

                                    int shipCount = pair.Value.GetCountToCapture();
                                    if ( shipCount > 0 && usesCaps )
                                        shipCount = shipType.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( shipType, shipCount, shipCount, maxMarkLevel );

                                    multiplier.SecondItem += shipCount;

                                    if ( multiplier.ThirdItem == FInt.Zero )
                                        multiplier.ThirdItem = damageModifier.MultiplierForMark[maxMarkLevel];
                                    else
                                        multiplier.ThirdItem *= damageModifier.MultiplierForMark[maxMarkLevel];
                                    multiplier.ThirdItem *= CalculateMultipleOfMultiplier( damageModifier, enemyShipTypeData, maxMarkLevel );
                                }
                            }
                        }
                    }
                }

                debugStage = 6000;
                if ( !wroteAny )
                    buffer.Add( "无" );

                debugStage = 6100;
                WriteMultipliersDealtByShipName( buffer, null, weakAgainst_multipliersDealtByShipName );
                buffer.Add( "\n" );

                #endregion
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "WriteWeakAgainst exception at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }
        }

        private static readonly SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark> strongAgainst_Ships_EnemiesHave = 
            SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark>.Create_WillNeverBeGCed( 30, delegate { return new ShipDataByMark(); }, "Window_InGameHoverEntityInfo-strongAgainst_Ships_EnemiesHave" );
        
        private static readonly SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark> strongAgainst_Ships_EnemiesAtThisPlanet = 
            SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark>.Create_WillNeverBeGCed( 30, delegate { return new ShipDataByMark(); }, "Window_InGameHoverEntityInfo-strongAgainst_Ships_EnemiesAtThisPlanet" );
        
        private static readonly SortedDictionary<GameEntityTypeData, RefThreeTuple<string, int, FInt>> strong_multipliersDealtByShipName = 
            SortedDictionary<GameEntityTypeData, RefThreeTuple<string, int, FInt>>.Create_WillNeverBeGCed( 300, "Window_InGameHoverEntityInfo-strong_multipliersDealtByShipName" );

        private static void WriteStrongAgainst( ArcenCharacterBufferBase buffer, GameEntityTypeData playerShipTypeData, Planet planetBeingViewedOrNull )
        {
            ShipListerUtils.CalculateKnownShipsThatEnemiesHave( strongAgainst_Ships_EnemiesHave, x => true, 
                x => true, x => true, false );
            //ShipListerUtils.CalculateKnownShipsThatEnemiesHave( strongAgainst_Ships_EnemiesAtThisPlanet, x => (planetBeingViewedOrNull == null || x.Planet == planetBeingViewedOrNull),
            //    x => true, x => true, false, strongAgainst_Counters_EnemiesAtThisPlanet );
            strong_multipliersDealtByShipName.Clear();

            Faction playerFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();

            int comparisonInt = 0;
            FInt comparisonFInt = FInt.Zero;
            bool forceDoesNotMeetCriteria = false;

            buffer.Add( "<color=#50abff>\nHas the following damage multipliers against enemy ships:</color>\n" );

            bool wroteAny = false;
            foreach ( KeyValuePair<GameEntityTypeData, ShipDataByMark> pair in strongAgainst_Ships_EnemiesHave )
            {
                GameEntityTypeData enemyShipType = pair.Key;

                byte enemyShipMaxMarkLevelFound = 0;
                int countAcrossAllMarks = 0;
                int singleLineCount = 0;
                for ( byte i = 0; i < pair.Value.CountsByMarkLength(); i++ )
                {
                    singleLineCount = pair.Value.GetCountByMark( i );
                    if ( singleLineCount > 0 )
                    {
                        enemyShipMaxMarkLevelFound = i;
                        countAcrossAllMarks += singleLineCount;
                    }
                }
                if ( countAcrossAllMarks <= 0 )
                    continue;

                GameEntityTypeData.MarkLevelStats enemyShipMarkStats = enemyShipType.MarkStatsFor( enemyShipMaxMarkLevelFound );
                for ( int j = 0; j < playerShipTypeData.SystemTypes.Count; j++ )
                {
                    //Debug.Log($"Strong against: comparing human {playerShipTypeData.DisplayName} with enemy {enemyShipType}");
                    EntitySystemTypeData playerShipSystemData = playerShipTypeData.SystemTypes[j];
                    for ( int k = 0; k < playerShipSystemData.OutgoingDamageModifiers_FullList.Count; k++ )
                    {
                        DamageModifier playerShipDamageModifier = playerShipSystemData.OutgoingDamageModifiers_FullList[k];
                        if ( playerShipDamageModifier.CalculateDoesMeetCriteriaStatic( enemyShipType, enemyShipMarkStats, ref comparisonInt, ref comparisonFInt, ref forceDoesNotMeetCriteria )
                            || playerShipDamageModifier.CalculateDoesMeetCriteriaTypeOnly( enemyShipType, enemyShipMarkStats, ref comparisonInt, ref comparisonFInt, ref forceDoesNotMeetCriteria ) )
                        {
                            if ( !forceDoesNotMeetCriteria &&
                                playerShipDamageModifier.CompareWithValues( comparisonInt, comparisonFInt ) )
                            {
                                wroteAny = true;

                                RefThreeTuple<string, int, FInt> multiplier = null;
                                if ( !strong_multipliersDealtByShipName.TryGetValue( enemyShipType, out multiplier ) )
                                {
                                    multiplier = RefThreeTuple<string, int, FInt>.Create(
                                        $"{enemyShipType.DisplayName} <color=#{enemyShipMarkStats.MarkLevel.ColorHex}>{enemyShipMarkStats.MarkLevel.Abbreviation}</color>",
                                        0, FInt.Zero );
                                    strong_multipliersDealtByShipName[enemyShipType] = multiplier;
                                }

                                multiplier.SecondItem += countAcrossAllMarks;

                                byte playerMark = 0;
                                if ( playerFaction != null )
                                    playerMark = playerFaction.GetGlobalMarkLevelForShipLine( playerShipTypeData );

                                if ( multiplier.ThirdItem == FInt.Zero )
                                    multiplier.ThirdItem = playerShipDamageModifier.MultiplierForMark[playerMark];
                                else
                                    multiplier.ThirdItem *= playerShipDamageModifier.MultiplierForMark[playerMark];
                                multiplier.ThirdItem *= CalculateMultipleOfMultiplier( playerShipDamageModifier, enemyShipType, playerMark );
                            }
                        }
                    }
                }
            }

            if ( !wroteAny )
                buffer.Add( "无" );

            WriteMultipliersDealtByShipName( buffer, null, strong_multipliersDealtByShipName );
            buffer.Add( "\n" );
        }

        private static void WriteMultipliersDealtByShipName( ArcenCharacterBufferBase buffer,
            SortedDictionary<GameEntityTypeData, RefThreeTuple<string, int, FInt>> multipliersDealtByShip_LocalPlanetOrNull,
            SortedDictionary<GameEntityTypeData, RefThreeTuple<string, int, FInt>> multipliersDealtByShip_GeneralOrNull )
        {
            if ( multipliersDealtByShip_LocalPlanetOrNull != null )
            {
                multipliersDealtByShip_LocalPlanetOrNull.SortIntoList( delegate ( KeyValuePair<GameEntityTypeData, RefThreeTuple<string, int, FInt>> Left, 
                    KeyValuePair<GameEntityTypeData, RefThreeTuple<string, int, FInt>> Right )
                {
                    //desceding by bonus amount
                    int val = Right.Value.ThirdItem.CompareTo( Left.Value.ThirdItem );
                    if ( val != 0 )
                        return val;
                    //if same bonus amount, then by name
                    return Left.Key.DisplayName.CompareTo( Right.Key.DisplayName );
                } );

                int count = 0;
                foreach ( KeyValuePair<GameEntityTypeData, RefThreeTuple<string, int, FInt>> kv in multipliersDealtByShip_LocalPlanetOrNull )
                {
                    string name = kv.Value.SecondItem + " " + kv.Value.FirstItem;
                    FInt value = kv.Value.ThirdItem;
                    buffer.Add( $"<b><size=120%>{name} (<color=#ffdf72>{value.ToFloatNonSim():#.#}x</color>)</size></b>" );

                    count++;
                    if ( count < multipliersDealtByShip_LocalPlanetOrNull.Count )
                    {
                        buffer.Add( ", " );
                    }
                    else
                    {
                        buffer.Add( ". " );
                    }
                }
            }

            if ( multipliersDealtByShip_GeneralOrNull != null )
            {
                multipliersDealtByShip_GeneralOrNull.SortIntoList( delegate ( KeyValuePair<GameEntityTypeData, RefThreeTuple<string, int, FInt>> Left, 
                    KeyValuePair<GameEntityTypeData, RefThreeTuple<string, int, FInt>> Right )
                {
                    //desceding by bonus amount
                    int val = Right.Value.ThirdItem.CompareTo( Left.Value.ThirdItem );
                    if ( val != 0 )
                        return val;
                    //if same bonus amount, then by name
                    return Left.Key.DisplayName.CompareTo( Right.Key.DisplayName );
                } );

                int count = 0;
                foreach ( KeyValuePair<GameEntityTypeData, RefThreeTuple<string, int, FInt>> kv in multipliersDealtByShip_GeneralOrNull )
                {
                    string name = kv.Value.SecondItem + " " + kv.Value.FirstItem;
                    FInt value = kv.Value.ThirdItem;
                    buffer.Add( $"{name} (<color=#ffdf72>{value.ToFloatNonSim():#.#}x</color>)" );

                    count++;
                    if ( count < multipliersDealtByShip_GeneralOrNull.Count )
                    {
                        buffer.Add( ", " );
                    }
                    else
                    {
                        buffer.Add( ". " );
                    }
                }
            }
        }

        #region GetEntityContainsSomethingToSeeMoreOf
        public static string _GetEntityContainsSomethingToSeeMoreOf( GameEntity_Squad squad )
        {
            if ( squad == null )
                return null;
            //show TSS contents
            if ( squad.TypeData.HackableForCommandStationsAndBattleStations_DSSStyle )
            {
                if ( squad.ShipGrantsList != null && squad.ShipGrantsList.Count > 0 )
                    return "details of the defensive lines available here";
            }
            //show ARS contents
            if ( squad.TypeData.GrantsStuffToBeAddedToPlayerFleets )
            {
                if ( squad.ShipGrantsList != null && squad.ShipGrantsList.Count > 0 )
                    return "details of the ships available here";
            }
            if ( squad.TypeData.IsFleetLeader )
            {
                //show any fleets -- what they have in them
                //if ( squad.HasNotYetBeenFullyClaimed || squad.GetFactionTypeSafe() != FactionType.Player )
                return "details of the ships that are part of this fleet";
            }
            if ( squad.AIReinforcementPointContents != null && squad.AIReinforcementPointContents.Count > 0 )
            {
                //show ai reinforcement point contents
                return "AI reinforcement point contents";
            }
            if (squad.TypeData.GetHasTag( OutguardBeaconStateForPlanet.OutguardBeaconTag))
            {
                //show Outguard unit details
                return "details for each Outguard group that you can contact from here";
            }
            return null;
        }
        #endregion

        #region WriteDetailsOfAnOutguardGroupContents
        public static void _WriteDetailsOfAnOutguardGroupContents( ArcenCharacterBufferBase buffer, OutguardInfo OutguardInfo )
        {
            EntityText.Write_Tooltip_Hotkeys_Footer( buffer, false, true, null );
            buffer.Add( "\n\n" );

            buffer.Add( "<u><b><size=17>" + OutguardInfo.GroupData.GetDisplayName() + " Outguard Group:</size></b></u>" );

            OutguardInfo.GroupData.WriteTooltipInfo( buffer );

            buffer.Add( "\n\n" );

            try
            {
                EntityTypeDrawingBag unitBag = OutguardInfo.GroupData.UnitBag;
                if ( !EntityTypeDrawingBag.IsNullOrInvalid( unitBag ) )
                {
                    // units can be by tag or by name, if tag, list every possible outcome
                    ListOfLists<GameEntityTypeData> unitData = GameEntityTypeData.GetTemporaryGameEntityTypeDataListOfLists( "Window_InGameHoverEntityInfo-unitData", 10f );
                    if ( unitData == null ) //blocked for teardown/shutdown; bail
                        return;
                    unitBag.FillAllPossibleEntityTypes( unitData, World_AIW2.Instance.AIFactions[0] );

                    for(int i = 0; i < unitData.OuterListCount; i++ )
                    {
                        buffer.AddSize_Large();
                        switch(i)
                        {
                            case 0:
                                buffer.Add( "主要：" );
                                break;
                            case 1:
                                buffer.Add( "次要：" );
                                break;
                            case 2:
                                buffer.Add( "第三：" );
                                break;
                            case 3:
                                buffer.Add( "第四：" );
                                break;
                            case 4:
                                buffer.Add( "第五：" );
                                break;
                            case 5:
                                buffer.Add( "第六：" );
                                break;
                            case 6:
                                buffer.Add( "第七：" );
                                break;
                            case 7:
                                buffer.Add( "第八：" );
                                break;
                            case 8:
                                buffer.Add( "第九：" );
                                break;
                            case 9:
                                buffer.Add( "第十：" );
                                break;
                            default:
                                buffer.Add( "组 " ).Add( i - 1 ).Add("：");
                                break;
                        }
                        EntityTypeDrawingBag_SpawnMode spawnMode = unitBag.CountTypeList[i];
                        bool variableAmount = unitBag.SpawnValue_Min[i] != unitBag.SpawnValue_Max[i];
                        if ( variableAmount )
                        {
                            switch(spawnMode)
                            {
                                case EntityTypeDrawingBag_SpawnMode.RawCount:
                                    buffer.Add( "介于 " ).AddNumberMoreReadable( unitBag.SpawnValue_Min[i] ).Add( " 和 " ).AddNumberMoreReadable( unitBag.SpawnValue_Max[i] ).Add( "x" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.AIBudget:
                                    buffer.Add( "介于 " ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " 和 " ).AddNumberTruncated( unitBag.SpawnValue_Max[i] )
                                        .Add( " AI budget worth" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.Strength_BaseMark:
                                    buffer.Add( "介于 " ).StartStrength( false ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " 和 " )
                                        .AddNumberTruncated( unitBag.SpawnValue_Max[i] ).Add(" 基础战力（= Mk1级）").EndColor().Add( " 价值" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.Strength_CurrentMark:
                                    buffer.Add( "介于 " ).StartStrength( false ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " 和 " )
                                        .AddNumberTruncated( unitBag.SpawnValue_Max[i] ).Add( " 战力 " ).EndColor().Add( " 价值" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.MetalCost:
                                    buffer.Add( "介于 " ).StartMetal( false ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " 和 " )
                                        .AddNumberTruncated( unitBag.SpawnValue_Max[i] ).Add( " 金属 " ).EndColor().Add( " 价值" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.EnergyCost:
                                    buffer.Add( "介于 " ).StartMetal( false ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " 和 " )
                                        .AddNumberTruncated( unitBag.SpawnValue_Max[i] ).Add( " 能量 " ).EndColor().Add( " 价值" );
                                    break;
                            }
                        } else
                        {
                            switch ( spawnMode )
                            {
                                case EntityTypeDrawingBag_SpawnMode.RawCount:
                                    buffer.AddNumberMoreReadable( unitBag.SpawnValue_Min[i] ).Add( "x" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.AIBudget:
                                    buffer.AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " AI budget worth" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.Strength_BaseMark:
                                    buffer.StartStrength( false ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " base strength (= at Mk1)" ).EndColor().Add( " worth of" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.Strength_CurrentMark:
                                    buffer.StartStrength( false ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " strength " ).EndColor().Add( " worth of" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.MetalCost:
                                    buffer.StartMetal( false ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " metal " ).EndColor().Add( " worth of" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.EnergyCost:
                                    buffer.StartMetal( false ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " energy " ).EndColor().Add( " worth of" );
                                    break;
                            }
                        }
                        if(unitData[i].Count > 1)
                            buffer.Add( " the following potential units:");
                        buffer.Add( "\n\n" ).EndSize();
                        var faction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                        for ( int j = 0; j < unitData[i].Count; j++ )
                        {
                            EntityText.GetTooltip( buffer, null, null, unitData[i][j], 0, null, faction?.GetGlobalMarkLevelForShipLine( unitData[i][j] )??0,
                                FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None, 1.0f, true );
                            buffer.Add( "\n\n" );
                        }
                    }
                    buffer.Add( "\n" );

                    GameEntityTypeData.ReleaseTemporaryGameEntityTypeDataListOfLists( unitData );
                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLog( "Error in Window_InGameHoverEntityInfo, outguard unit display: " + e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region WriteDetailsOfAllShipContents
        public static void _WriteDetailsOfAllShipContents( ArcenCharacterBufferBase buffer, GameEntity_Squad squad )
        {
            //also update GetEntityContainsSomethingToSeeMoreOf with anything from here!!
            EntityText.Write_Tooltip_Hotkeys_Footer( buffer, false, true, null );
            buffer.Add( "\n\n" );

            buffer.Add( "<u>Main Ship:</u>\n" );
            EntityText.GetTooltip( buffer, squad, squad.FleetMembership,
                null, -1, null, 0, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None, 1.0f, true );
            buffer.Add( "\n\n" );

            #region show DSS contents
            if ( squad.TypeData.HackableForCommandStationsAndBattleStations_DSSStyle )
            {
                if ( squad.ShipGrantsList != null && squad.ShipGrantsList.Count > 0 )
                {
                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    ShipLineEntry entry = null;
                    buffer.Add( "<u>Defensive Lines For Theft:</u>\n" );
                    for ( int i = 0; i < squad.ShipGrantsList.Count; i++ )
                    {
                        entry = squad.ShipGrantsList[i];
                        byte markLevel = localFaction.GetGlobalMarkLevelForShipLine( entry.TypeData );

                        GameEntityTypeData.MarkLevelStats markStatsForDisplay = entry.TypeData.MarkStatsFor( markLevel );
                        int cap = entry.TypeData.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( entry.TypeData, entry.BaseNumShips, entry.BaseNumShips, markStatsForDisplay.MarkLevel );

                        EntityText.GetTooltip( buffer, null, null,
                                entry.TypeData, null, localFaction.FactionCenterColor.ColorHexBrighter, " - Hack To Choose One Option", cap,
                                localFaction, markLevel, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.AIPCostOnGrant | ShipExtraDetailFlags.BuildInfo, 
                                1.0f, true );
                        buffer.Add( "\n\n" );
                    }
                }
            }
            #endregion show DSS contents

            #region show ARS or FRS contents
            if ( squad.TypeData.GrantsStuffToBeAddedToPlayerFleets )
            {
                if ( squad.ShipGrantsList != null && squad.ShipGrantsList.Count > 0 )
                {
                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    ShipLineEntry entry = null;
                    buffer.Add( "<u>Ships Available For Theft:</u>\n" );
                    for ( int i = 0; i < squad.ShipGrantsList.Count; i++ )
                    {
                        entry = squad.ShipGrantsList[i];
                        byte markLevel = localFaction.GetGlobalMarkLevelForShipLine(entry.TypeData);

                        GameEntityTypeData.MarkLevelStats markStatsForDisplay = entry.TypeData.MarkStatsFor( markLevel );
                        int cap = entry.TypeData.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( entry.TypeData, entry.BaseNumShips, entry.BaseNumShips, markStatsForDisplay.MarkLevel );

                        EntityText.GetTooltip( buffer, null, null,
                                entry.TypeData, null, localFaction.FactionCenterColor.ColorHexBrighter, " - Hack To Choose One Option", cap, 
                                localFaction, markLevel, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.AIPCostOnGrant | ShipExtraDetailFlags.BuildInfo, 
                                1.0f, true );
                        buffer.Add( "\n\n" );
                    }
                }
            }
            #endregion show ARS or ARS contents

            if ( squad.TypeData.IsFleetLeader )
            {
                #region show any fleets -- what they have in them
                Fleet squadFleet = squad.GetFleetOrNull_Safe();
                //if ( squad.HasNotYetBeenFullyClaimed || squad.GetFactionTypeSafe() != FactionType.Player )
                if ( squadFleet != null )
                {
                    bool isForCapture = squadFleet.GetFactionType_Safe() == FactionType.NaturalObject;
                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();

                    buffer.Add( "<u>Ship Lines In Fleet:</u>\n" );
                    foreach ( FleetMembership mem in squadFleet.MemberGroupsSorted_NonSim_ForUIOnly_SeriouslyDoNotCallFromBGCode() )
                    {
                        if ( mem.TypeData.IsFleetLeader )
                            continue;

                        if ( isForCapture )
                        {
                            if ( mem.ExplicitBaseSquadCap <= 0 )
                                continue;
                            byte markLevel = localFaction.GetGlobalMarkLevelForShipLine( mem.TypeData );

                            GameEntityTypeData.MarkLevelStats markStatsForDisplay = mem.TypeData.MarkStatsFor( markLevel );
                            int cap = mem.TypeData.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( mem.TypeData, mem.ExplicitBaseSquadCap, mem.ExplicitBaseSquadCap, markStatsForDisplay.MarkLevel );

                            EntityText.GetTooltip( buffer, null, null,
                                mem.TypeData, null, localFaction.FactionCenterColor.ColorHexBrighter, " - Captured When Fleet Leader Is Claimed", cap, null,
                                markStatsForDisplay.MarkLevel.Ordinal, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None,
                                1.0f, true );
                        }
                        else //an actual one!
                        {
                            if ( mem.EffectiveSquadCap <= 0 )
                                continue;

                            EntityText.GetTooltip( buffer, null, mem,
                                null, mem.EffectiveSquadCap, null, mem.ForMark.MarkLevel.Ordinal, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None,
                                1.0f, true );
                        }
                        buffer.Add( "\n\n" );
                    }
                }
                #endregion show any fleets -- what they have in them

                #region show the contents of fleet leaders that are transports
                if ( squadFleet != null && squadFleet.GetHasAnyMobileFleetTransportContents() )
                {
                    buffer.Add( "<u>Ships Transported In Flagship:</u>\n" );
                    foreach ( FleetMembership mem in squadFleet.MemberGroupsSorted_NonSim_ForUIOnly_SeriouslyDoNotCallFromBGCode() )
                    {
                        if ( mem.TypeData.IsFleetLeader )
                            continue;
                        if ( mem.TransportContents.Count == 0 )
                            continue;

                        EntityText.GetTooltip( buffer, null, mem,
                            null, mem.CalculateTransportedContentsCount(), null, 0, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None,
                            1.0f, true );
                        buffer.Add( "\n\n" );
                    }
                }
                #endregion show the contents of fleet leaders that are transports
            }

            if ( squad.AIReinforcementPointContents != null && squad.AIReinforcementPointContents.Count > 0 )
            {
                byte markLevelOfContents = squad.GetMarkLevelOfContents();

                #region show ai reinforcement point contents
                buffer.Add( "<u>Ships Contained In This AI Reinforcement Point:</u>\n" );
                RefPair<GameEntityTypeData, int> pair;
                for ( int i = 0; i < squad.AIReinforcementPointContents.Count; i++ )
                {
                    pair = squad.AIReinforcementPointContents[i];
                    if ( pair != null && pair.RightItem > 0 )
                    {
                        EntityText.GetTooltip( buffer, null, null,
                            pair.LeftItem, pair.RightItem, squad.GetFactionOrNull_Safe(), markLevelOfContents, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None, 
                            1.0f, true );
                        buffer.Add( "\n\n" );
                    }
                }
                #endregion show ai reinforcement point contents
            }

            #region show outguard unit details
            if ( squad.TypeData.GetHasTag( OutguardBeaconStateForPlanet.OutguardBeaconTag ) )
            {
                List<OutguardGroupData> givenGroups = OutguardGroupData.GetTemporaryOutguardGroupDataList( "Window_InGameHoverEntityInfo-givenGroups", 10f );
                if ( givenGroups == null ) //blocked for teardown/shutdown; bail
                    return;

                // list out details for each Outguard group

                bool hacked = false; // unused here, but needed as the function requires it
                OutguardBeaconStateForPlanet.GetAvailableGroupsForBeaconOnPlanet( squad.Planet, givenGroups, ref hacked );

                for ( int x = 0; x < givenGroups.Count; x++ )
                {
                    OutguardGroupData groupData = givenGroups[x];
                    _WriteDetailsOfAnOutguardGroupContents( buffer, World_AIW2.Instance.GetOutguardState( groupData ) );
                }

                OutguardGroupData.ReleaseTemporaryOutguardGroupDataList( givenGroups );
            }
            #endregion
        }
        #endregion

        public static void WriteTransformsInto( ArcenCharacterBufferBase buffer, GameEntity_Squad e )
        {
            if ( e == null || e.SecondsTillTransformation <= 0 )
                return;

            if ( e.TransformsIntoAfterTime == "$Dies" ||
                 e.TransformsIntoAfterTime == "$Dies_Paused" )
            {
                if ( e.TypeData.TransformationCountdownOnlyDuringCombat )
                    buffer.Add( "此将在 " )
                        .AddMinutesAndSeconds( e.SecondsTillTransformation, "ffa1a1" ).Add( " 后于战斗中消失。" );
                else
                    buffer.Add( "此将在 " ).AddMinutesAndSeconds( e.SecondsTillTransformation, "ffa1a1" )
                        .Add( " 后消失。" );
            }
            else
            {
                //a bit of a performance hit to look up the entity here; if this is too much then
                //we should do this lookup earlier
                var intoType =
                    GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( e.TransformsIntoAfterTime );

                if ( intoType == null )
                {
                    buffer.Add( "找不到 " + e.TransformsIntoAfterTime +
                                " 在 XML 中。这是一个 BUG。请报告。" );
                }
                else
                {
                    if ( e.TypeData.TransformationCountdownOnlyDuringCombat )
                        buffer
                            .Add( "This will transform into a " )
                            .Add( intoType.GetDisplayName(), "a1ffa1" )
                            .Add( " after " )
                            .AddMinutesAndSeconds( e.SecondsTillTransformation, "ffa1a1" )
                            .Add( " in combat." );
                    else
                        buffer
                            .Add( "This " )
                            .Add( intoType.GetDisplayName(), "a1ffa1" )
                            .Add( " is warping in and will be fully created in " )
                            .AddMinutesAndSeconds( e.SecondsTillTransformation, "ffa1a1" )
                            .Add( "." );
                }
            }

            buffer.Add( " " );
        }
        
        private static void WriteMinMaxOrVariant( ArcenCharacterBufferBase buffer, string AnyName, string OnlyName, bool IsBrief, string Color, FInt min, FInt max )
        {
            if ( min <= FInt.Zero && max <= FInt.Zero )
            { 
                buffer.Add( AnyName );
                buffer.Add( ".  " );
                return;
            }
            else
            {
                buffer.Add( OnlyName );
                if ( min > FInt.Zero )
                {
                    if ( IsBrief )
                        buffer.Add( " > " );
                    else
                        buffer.Add( " greater than " );
                    buffer.StartColor( Color );
                    buffer.AddFixedDecimal( min.ToFloatNonSim(), 2 );
                    buffer.EndColor();

                    if ( max > FInt.Zero )
                    {
                        if ( IsBrief )
                            buffer.Add( " and < " );
                        else
                            buffer.Add( " and less than " );
                        buffer.StartColor( Color );
                        buffer.AddFixedDecimal( max.ToFloatNonSim(), 2 );
                    }
                    buffer.Add( "</color>.  " );
                }
                else //no min
                {
                    if ( IsBrief )
                        buffer.Add( " < " );
                    else
                        buffer.Add( " less than " );
                    buffer.StartColor( Color );
                    buffer.AddFixedDecimal( max.ToFloatNonSim(), 2 );
                    buffer.Add( "</color>.  " );
                }
            }
        }

        private static void WriteBuildStatRow(ArcenCharacterBufferBase buffer, bool isforNecromancer, CharacterPosInfo cpi, TooltipDetail detailLevel, int forCount,
            int strengthCurr, int hullMax, int shieldMax, int metalCurr, int storedMetal, int storedEnergy, int energyCurr, ResourceType fuelType, int fuelCurr, int storedFuel )
        {
            buffer.NewLine();
            cpi.ResetPos();
            if ( detailLevel == TooltipDetail.Full )//for full tooltips we have to basically go small size all the way...
                buffer.AddSize_Small();
            buffer.Add( "<color=#dbef21>" ).Add( forCount ).Add( "</color> Units:" );
            if ( detailLevel < TooltipDetail.Full )
                buffer.ToPos( cpi.AddStep().AddStep_TwentyPercent() );
            else
                buffer.ToPos( cpi.AddStep_Half() );

            if ( detailLevel == TooltipDetail.Full )
                buffer.Add( "战力：" );
            else
                buffer.AddSize_Small();
            buffer.AddStrength_Truncated( strengthCurr * forCount, detailLevel != TooltipDetail.Full );
            if(detailLevel != TooltipDetail.Full)
                buffer.EndSize();

            if ( detailLevel < TooltipDetail.Full )
                buffer.ToPos( cpi.AddStep().AddStep_Tenth() );
            else
                buffer.ToPos( cpi.AddStep_SeventyPercent() );

            if ( detailLevel == TooltipDetail.Full )
                buffer.Add( "船体：" );
            else
                buffer.AddSize_Small();
            buffer.AddHull_Truncated( hullMax * forCount, detailLevel != TooltipDetail.Full );
            if ( detailLevel != TooltipDetail.Full )
                buffer.EndSize();

            if ( detailLevel < TooltipDetail.Full )
                buffer.ToPos( cpi.AddStep_NinetyPercent() );
            else
                buffer.ToPos( cpi.AddStep_Half() );

            if ( detailLevel == TooltipDetail.Full )
                buffer.Add( "护盾：" );
            else
                buffer.AddSize_Small();
            buffer.AddShield_Truncated( shieldMax * forCount, detailLevel != TooltipDetail.Full );
            if ( detailLevel != TooltipDetail.Full )
                buffer.EndSize();

            if ( !isforNecromancer )
            {
                if ( detailLevel < TooltipDetail.Full )
                    buffer.ToPos( cpi.AddStep_NinetyPercent() );
                else
                    buffer.ToPos( cpi.AddStep_Half().AddStep_Twentieth() );

                if ( detailLevel == TooltipDetail.Full )
                    buffer.Add( "金属：" );
                if ( metalCurr == 0 )
                {
                    buffer.StartMetal( detailLevel != TooltipDetail.Full );
                    if ( detailLevel != TooltipDetail.Full )
                        buffer.AddSize_Small();
                    buffer.Add( "-" );
                } else if ( storedMetal < metalCurr * forCount )
                {
                    buffer.StartMetal( detailLevel != TooltipDetail.Full, "ef3219" );
                    if ( detailLevel != TooltipDetail.Full )
                        buffer.AddSize_Small();
                    buffer.AddNumberTruncated( metalCurr * forCount );
                } else
                {
                    buffer.StartMetal( detailLevel != TooltipDetail.Full );
                    if ( detailLevel != TooltipDetail.Full )
                        buffer.AddSize_Small();
                    buffer.AddNumberTruncated( metalCurr * forCount );
                }
                if ( detailLevel == TooltipDetail.Full )
                    buffer.Add( " U" );
                else
                    buffer.EndSize();
                buffer.EndColor();

                if ( detailLevel < TooltipDetail.Full )
                    buffer.ToPos( cpi.AddStep_EightyPercent() );
                else
                    buffer.ToPos( cpi.AddStep_Half().AddStep_Twentieth() );

                if ( detailLevel == TooltipDetail.Full )
                    buffer.Add( "能量：" );
                if ( energyCurr == 0 )
                {
                    buffer.StartEnergy( detailLevel != TooltipDetail.Full );
                    if ( detailLevel != TooltipDetail.Full )
                        buffer.AddSize_Small();
                    buffer.Add( "-" );
                } else if ( storedEnergy < energyCurr * forCount )
                {
                    buffer.StartEnergy( detailLevel != TooltipDetail.Full, "ef3219" );
                    if ( detailLevel != TooltipDetail.Full )
                        buffer.AddSize_Small();
                    buffer.AddNumberTruncated( energyCurr * forCount );
                } else
                {
                    buffer.StartEnergy( detailLevel != TooltipDetail.Full );
                    if ( detailLevel != TooltipDetail.Full )
                        buffer.AddSize_Small();
                    buffer.AddNumberTruncated( energyCurr * forCount );
                }
                if ( detailLevel == TooltipDetail.Full )
                    buffer.Add( " GW" );
                else
                    buffer.EndSize();
                buffer.EndColor();

                if ( !World_AIW2.Instance.IsFuelEnabled || fuelCurr <= 0 )
                {
                    if ( detailLevel == TooltipDetail.Full )
                        buffer.EndSize();
                    return;
                }

                if ( detailLevel < TooltipDetail.Full )
                    buffer.ToPos( cpi.AddStep_NinetyPercent().AddStep_Twentieth() );
                else
                    buffer.ToPos( cpi.AddStep_SeventyPercent() );

                if ( detailLevel == TooltipDetail.Full )
                {
                    if ( fuelType == ResourceType.FuelArgon )
                        buffer.Add( "氩气：" );
                    else if ( fuelType == ResourceType.FuelRadon )
                        buffer.Add( "氡气：" );
                    else if ( fuelType == ResourceType.FuelXenon )
                        buffer.Add( "氙气：" );
                }
                if ( fuelCurr == 0 )
                {
                    if ( fuelType == ResourceType.FuelArgon )
                        buffer.StartArgon( detailLevel != TooltipDetail.Full );
                    else if ( fuelType == ResourceType.FuelRadon )
                        buffer.StartRadon( detailLevel != TooltipDetail.Full );
                    else if ( fuelType == ResourceType.FuelXenon )
                        buffer.StartXenon( detailLevel != TooltipDetail.Full );
                    if ( detailLevel != TooltipDetail.Full )
                        buffer.AddSize_Small();
                    buffer.Add( "-" );
                } else if ( storedFuel < fuelCurr * forCount )
                {
                    if ( fuelType == ResourceType.FuelArgon )
                        buffer.StartArgon( detailLevel != TooltipDetail.Full, "ef3219" );
                    else if ( fuelType == ResourceType.FuelRadon )
                        buffer.StartRadon( detailLevel != TooltipDetail.Full, "ef3219" );
                    else if ( fuelType == ResourceType.FuelXenon )
                        buffer.StartXenon( detailLevel != TooltipDetail.Full, "ef3219" );
                    if ( detailLevel != TooltipDetail.Full )
                        buffer.AddSize_Small();
                    buffer.AddNumberTruncated( fuelCurr * forCount );
                } else
                {
                    if ( fuelType == ResourceType.FuelArgon )
                        buffer.StartArgon( detailLevel != TooltipDetail.Full );
                    else if ( fuelType == ResourceType.FuelRadon )
                        buffer.StartRadon( detailLevel != TooltipDetail.Full );
                    else if ( fuelType == ResourceType.FuelXenon )
                        buffer.StartXenon( detailLevel != TooltipDetail.Full );
                    if ( detailLevel != TooltipDetail.Full )
                        buffer.AddSize_Small();
                    buffer.AddNumberTruncated( fuelCurr * forCount );
                }
                if ( detailLevel != TooltipDetail.Full )
                    buffer.EndSize();
            }
            buffer.EndColor().EndSize();
        }

        private static void HandleNewlineAndSize( ArcenCharacterBufferBase buffer, ref bool haveDoneNewLineAndSize )
        {
            if ( haveDoneNewLineAndSize )
                return;
            haveDoneNewLineAndSize = true;
            buffer.Add( "\n" ).Add( FontSizes.SLIGHTLY_SMALLER_SIZE_V2_STRING );
        }
    }

    public class AnyUnitExampleAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                Buffer.Add( "嘿，这是侧边栏或建造菜单的提示！当前没有悬停于具体单位。" );
            else
                Buffer.Add( "嘿，当前悬停于位置：" ).Add( RelatedEntityOrNull.WorldLocation.X ).Add( "," ).Add( RelatedEntityOrNull.WorldLocation.Y );
        }
    }

    [Flags]
    public enum ShipExtraDetailFlags
    {
        None                                    = 0,
        BuildInfo                               = 1 << 0,
        AIPCostOnGrant                          = 1 << 1,
        AnyGrantHackInfo                        = 1 << 2,
        WindowHackChoicesSidebarPopoutForData   = 1 << 3,
        InGuardPost                             = 1 << 4,
        BeingTransported                        = 1 << 5,
        LoadedDrone                             = 1 << 6,
        IsMultiple                              = 1 << 7,
        Encyclopedia                            = 1 << 8,
        PlainText                               = 1 << 9,
        HighestDetail                           = 1 << 10,
        ShownInsideAnother                      = 1 << 11,
        ShowTechLineCount                       = 1 << 12,
    }
}
