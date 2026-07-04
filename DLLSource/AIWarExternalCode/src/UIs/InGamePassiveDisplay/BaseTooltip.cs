using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class BaseTooltip : ImageButtonAbstractBase
    {
        public WindowControllerAbstractBase WindowController;

        public ArcenUI_ImageButton MyElement;
        private ArcenUI_Image.SubImageGroup SubImages;
        private SubTextGroup SubTexts;
        public float TimeLastSet;
        private bool isInitialized = false;
        private bool IsSkippingDrawing = false;
        private string lastSetText = string.Empty;

        private const int BASE_TOOLTIP_WIDTH = 660;
        private float tooltipScale = 1.0f;
        private IArcenUIElementForSizing mustBeAboveOrBelow;

        public void ClearMyself()
        {
            this.lastSetText = string.Empty;
            this.lastSetTextTarget = null;
            this.lastSetScaleType = string.Empty;
            this.lastSetTextRealWorkTime = float.MinValue;
            isInitialized = false;
            IsSkippingDrawing = true;
        }

        public override void SetElement( ArcenUI_Element Element )
        {
            this.MyElement = (ArcenUI_ImageButton)Element;
            this.MyElement.Window.MaxDeltaTimeBeforeUpdates = 0;
        }

        public override bool GetShouldBeHidden()
        {
            if ( InputCaching.CalculateShouldTooltipsBeSuppressed() )
                return true;
            //if ( this.NextTextToShow.Length <= 0 )
            //    return true;
            if ( ArcenTime.TimeSinceStartF - this.TimeLastSet > ArcenUI.TimespanAfterWhichTooltipsDisappear )
                return true;

            return false;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
        }

        public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup _SubImages, SubTextGroup _SubTexts )
        {
            int debugstage = 0;
            try
            {
                if ( this.isInitialized)
                    return;

                debugstage = 100;
                if (this.MyElement == null)
                    return;
                debugstage = 110;
                if (this.MyElement.Window == null)
                    return;
                debugstage = 120;
                this.WindowController = (this.MyElement.Window.Controller as WindowControllerAbstractBase);
                if ( this.WindowController == null )
                    return;

                debugstage = 130;
                this.MyElement.Window.IsAutomaticPositioningDisabled = true;
                debugstage = 140;
                this.MyElement.Window.SetOverridingCanvasSortingOrder( 32767 ); //as high as it will go, so this is always 在 top!
                debugstage = 150;
                this.WindowController.GetType().GetField("Panel", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.GetField).SetValue(this.WindowController, this);
                debugstage = 160;
                WindowController.myXPositionScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" );
                debugstage = 170;
                SubImages = _SubImages;
                SubTexts = _SubTexts;
                debugstage = 180;

                this.isInitialized = true;
                debugstage = 190;
            }
            catch( Exception e)
            {
                ArcenDebugging.ArcenDebugLogNoDateOrAnything(
                    string.Format("exception 在 BaseTooltip.UpdateContentFromVolatile at debugstage {0}\n{1}", debugstage, e), 
                    DebugLogDestination.ArcenDebugLog, Verbosity.DoNotShow);
            }
        }

        public override void OnMainThreadUpdate()
        {
            base.OnMainThreadUpdate();

            if (!isInitialized)
                return;
            if (IsSkippingDrawing)
                return;
            if ( string.IsNullOrEmpty(this.lastSetText) )
                return;

            var ui_group_0 = this.SubTexts[0];
            var ui_group_1 = this.SubTexts[1];
            var ui_text_0 = this.SubTexts[0].Text;
            var ui_text_1 = this.SubTexts[1].Text;
            var ui_text_ref_0 = this.SubTexts[0].ReferenceText;
            var ui_text_ref_1 = this.SubTexts[1].ReferenceText;

            #region Update Text, Height

            //string text = this.lastSetText;

            try
            {
                //lastSetText = ExternalConstants.Instance.GetCustomString_Slow("tooltip_text_loremipsum");

                //if (this.SubTexts[0].Text.GetCurrentText() != lastSetText)
                {
                    ui_text_0.StartWritingToBuffer().Add( this.lastSetText );
                    ui_text_0.FinishWritingToBuffer();
                    //ui_text_1.StartWritingToBuffer().Add( Text );
                    //ui_text_1.FinishWritingToBuffer();
                    ui_text_ref_0.SetText(this.lastSetText);
                    ui_text_ref_1.SetText(this.lastSetText);
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception 在 UpdateContentFromVolatile for the single text element:" + e.ToString(), Verbosity.ShowAsError );
            }
            

            this.SubTexts[0].Obj.transform.parent.localScale = new Vector3( tooltipScale, tooltipScale, tooltipScale );

            int min_width, max_width;
            ArcenUI.Instance.Tooltip_Width(BASE_TOOLTIP_WIDTH, out min_width, out max_width);
                
            float min = min_width;// / newGeneralTooltipScale;
            float max = max_width;// / newGeneralTooltipScale;
             
            
            //float ui_width = max;//ArcenUI.Instance.Tooltip_Width(BASE_TOOLTIP_WIDTH);// / tooltipScale;
            //float ui_height = 2000;

            //float fixed_width = ExternalConstants.Instance.GetCustomFloat_Slow("tooltip_fixed_width");
            //float fixed_height = ExternalConstants.Instance.GetCustomFloat_Slow("tooltip_fixed_height");
            //ui_width = fixed_width;
            //ui_height = fixed_height;

            ui_text_ref_0.rectTransform.UI_SetWidth(max);
            ui_text_ref_1.rectTransform.UI_SetWidth(max);

            //if ( text != null && text.Length > 0 )
            var ui_height_0 = ArcenUI.Instance.CalculatePreferredTextObjectHeight( ui_text_ref_0, lastSetText, min, max );
            var ui_height_1 = ArcenUI.Instance.CalculatePreferredTextObjectHeight( ui_text_ref_1, lastSetText, min, max );

            ui_text_ref_0.rectTransform.UI_SetHeight( ui_height_0 );
            ui_text_ref_1.rectTransform.UI_SetHeight( ui_height_1 );


            //this.WrappedNextTextToShow = this.NextTextToShow;

            //float text_hmargin, text_vmargin;
            //ArcenUI.Instance.GetTextMargins(out text_hmargin, out text_vmargin);

            //var bg_width = text_hmargin*2 + ui_width;
            //var bg_height = text_vmargin + ui_height_0;

            //ArcenUI_Image.SubImage subImage = this.MyElement.SubImages[0];
            //subImage.Img.rectTransform.UI_SetWidth( bg_width + space_after);
            //subImage.Img.rectTransform.UI_SetHeight( bg_height );

            #endregion

            //UpdatePositionAndSize();
            /*
            ArcenDebugging.ArcenDebugLogNoDateOrAnything( string.Format("width={1} height={2} ui_height_0={3} ui_height_1={4}\ntext={0}\nui_text_0.GetCurrentText()={5}\nui_text_ref_0.text={6}\nui_text_1.GetCurrentText()={7}\nui_text_ref_1.text={8}", 
                lastSetText, ui_width, ui_height, ui_height_0, ui_height_1, lastSetText, ui_text_0.GetCurrentText(), ui_text_ref_0.text, ui_text_1.GetCurrentText(), ui_text_ref_1.text), 
                DebugLogDestination.ArcenDebugLog, Verbosity.DoNotShow );
            */

            return;

            #region Set World-Space Pos/Size

            float screenXPixel = 0;
            float screenYPixel = ArcenInput.MouseScreenY - 10;

            Vector3 worldSpacePoint = ArcenUI.Instance.guiCamera.ScreenToWorldPoint( new Vector3( screenXPixel, screenYPixel, ArcenUI.POSITION_Z ) );
            worldSpacePoint.x = Window_InGameSidebarShips.Instance.GetWorldSpaceMaxX( 5f * WindowController.myXPositionScale, false );

            SubText groupZero = this.SubTexts[0];
            if ( groupZero == null || groupZero.Obj == null || groupZero.Obj.transform == null || groupZero.Obj.transform.parent == null )
                return;

            Vector2 sizeDelta = ( (RectTransform)groupZero.Obj.transform.parent ).GetWorldSpaceSize();
            float width = sizeDelta.x;
            float height = sizeDelta.y;
                
            float maxXPixel = ArcenUI.Instance.world_BottomRight.x - width;
            float maxYPixel = ArcenUI.Instance.world_BottomRight.y + height;
                
            worldSpacePoint.x = Mathf.Min( worldSpacePoint.x, maxXPixel );
            worldSpacePoint.y = Mathf.Max( worldSpacePoint.y, maxYPixel );

            this.MyElement.Window.SetPositionIfNeeded( worldSpacePoint );

            #endregion
        }

        public void UpdatePositionAndSize()
        {
            //if ( this.GetShouldBeHidden() )
            //    return;
            if ( this.SubTexts == null )
                return;
            int debugStage = 0;
            try
            {
                debugStage = 1000;
                float screenXPixel = ArcenInput.MouseScreenX;
                float screenYPixel = ArcenInput.MouseScreenY;

                debugStage = 1100;
                this.MyElement.Window.IsAutomaticPositioningDisabled = true;

                debugStage = 1200;
                Vector3 worldSpacePoint = ArcenUI.Instance.guiCamera.ScreenToWorldPoint( new Vector3( screenXPixel, screenYPixel, ArcenUI.POSITION_Z ) );

                debugStage = 1300;
                SubText groupZero = this.SubTexts[0];
                debugStage = 1400;
                if ( groupZero == null || groupZero.Obj == null || groupZero.Obj.transform == null || groupZero.Obj.transform.parent == null )
                    return;

                debugStage = 2000;
                Vector2 sizeDelta = ((RectTransform)groupZero.Obj.transform.parent).GetWorldSpaceSize();
                debugStage = 2100;
                float width = sizeDelta.x;
                float height = sizeDelta.y;

                debugStage = 2200;
                float maxXPixel = ArcenUI.Instance.world_BottomRight.x - width;
                float maxYPixel = ArcenUI.Instance.world_BottomRight.y + height;

                debugStage = 2300;
                float centerOfScreenXInWorldCoordinates = (ArcenUI.Instance.world_BottomRight.x - ArcenUI.Instance.world_BottomLeft.x) / 2f;
                centerOfScreenXInWorldCoordinates += ArcenUI.Instance.world_BottomLeft.x;

                debugStage = 2400;
                bool isMouseOnLeftHalfOfScreen = worldSpacePoint.x < centerOfScreenXInWorldCoordinates;

                //ArcenDebugging.ArcenDebugLogSingleLine( "centerOfScreenXInWorldCoordinates: " + centerOfScreenXInWorldCoordinates + " screenXPixel: " + screenXPixel +
                //    " worldSpacePoint.x: " + worldSpacePoint.x + " botRight.x: " + ArcenUI.Instance.world_BottomRight.x +
                //    " botLeft.x: " + ArcenUI.Instance.world_BottomLeft.x + " isMouseOnLeftHalfOfScreen: " + isMouseOnLeftHalfOfScreen, Verbosity.DoNotShow );

                debugStage = 2500;
                worldSpacePoint.x = Mathf.Min( worldSpacePoint.x, maxXPixel );
                worldSpacePoint.y = Mathf.Max( worldSpacePoint.y, maxYPixel );

                //ignore all MustBeAboveOrBelow code, since we are moving 到 the side of the screen opposite the mouse instead
                //mustBeAboveOrBelow = null;
                debugStage = 3000;
                if ( !isMouseOnLeftHalfOfScreen )
                {
                    debugStage = 3100;
                    //snap 到 left of screen
                    worldSpacePoint.x = ArcenUI.Instance.world_TopLeft.x;

                    debugStage = 3200;
                    if ( this.mustBeAboveOrBelow != null )
                    {
                        debugStage = 3300;
                        RectTransform rTran = this.mustBeAboveOrBelow.GetRelevantRect();
                        debugStage = 3400;
                        if ( rTran )
                        {
                            debugStage = 3500;
                            bool hadError = false;
                            Vector2 minMaxX = Vector2.zero;
                            debugStage = 3600;
                            try
                            {
                                minMaxX = rTran.GetWorldSpaceMinXAndMaxX( 0 );
                            }
                            catch { hadError = true; }
                            debugStage = 3700;
                            if ( !hadError )
                            {
                                debugStage = 4000;
                                float rightOfUsIfPlacedToRightOfElement = minMaxX.x
                                    - sizeDelta.x; //we must subtracy the size, because we are going left

                                debugStage = 4100;
                                if ( rightOfUsIfPlacedToRightOfElement > worldSpacePoint.x )
                                    worldSpacePoint.x = rightOfUsIfPlacedToRightOfElement; //snap 到 the right of the element if we can
                            }
                        }
                    }
                }
                else
                {
                    debugStage = 7000;
                    //snap 到 right of screen
                    worldSpacePoint.x = maxXPixel;

                    debugStage = 7100;
                    if ( this.mustBeAboveOrBelow != null )
                    {
                        debugStage = 7200;
                        RectTransform rTran = this.mustBeAboveOrBelow.GetRelevantRect();
                        debugStage = 7300;
                        if ( rTran )
                        {
                            debugStage = 7400;
                            bool hadError = false;
                            Vector2 minMaxX = Vector2.zero;
                            try
                            {
                                minMaxX = rTran.GetWorldSpaceMinXAndMaxX( 0 );
                            }
                            catch { hadError = true; }
                            debugStage = 7500;
                            if ( !hadError )
                            {
                                debugStage = 7600;
                                if ( minMaxX.y < worldSpacePoint.x )
                                    worldSpacePoint.x = minMaxX.y; //snap 到 the left of the element if we can
                            }
                        }
                    }
                }

                debugStage = 8000;
                //okay, DO snap the Y, after all
                if ( this.mustBeAboveOrBelow != null )
                {
                    debugStage = 8100;
                    RectTransform rTran = this.mustBeAboveOrBelow.GetRelevantRect();
                    debugStage = 8200;
                    if ( rTran )
                    {
                        debugStage = 8300;
                        bool hadError = false;
                        Vector2 minMaxY = Vector2.zero;
                        Vector2 minMaxX = Vector2.zero;
                        try
                        {
                            minMaxY = rTran.GetWorldSpaceMinYAndMaxY( 0 );
                            minMaxX = rTran.GetWorldSpaceMinXAndMaxX( 0 );
                        }
                        catch { hadError = true; }
                        debugStage = 8400;
                        if ( !hadError )
                        {
                            bool alignedToSideOnly = false;
                            //if ( minMaxX.y - minMaxX.x < 400 )
                            {
                                //if this is fairl narrow, then try 到 just move this 到 the side

                                float rightOfUsIfPlacedToTheRightOfElement = minMaxX.y + sizeDelta.x;
                                if ( rightOfUsIfPlacedToTheRightOfElement < ArcenUI.Instance.world_TopRight.x &&
                                    ( this.mustBeAboveOrBelow == null || !(this.mustBeAboveOrBelow is ArcenUI_Slider) ) ) //if a slider, never put it 到 the right
                                {
                                    //if there IS room 到 the right, then place us there:
                                    worldSpacePoint.x = minMaxX.y;
                                    worldSpacePoint.y = minMaxY.x; //and align our top with the top of this
                                    alignedToSideOnly = true;
                                }
                                else
                                {
                                    float leftOfUsIfPlacedToLeftOfElement = minMaxX.x - sizeDelta.x;

                                    if ( leftOfUsIfPlacedToLeftOfElement >= ArcenUI.Instance.world_TopLeft.x )
                                    {
                                        //if there IS room 到 the left, then place us there:
                                        worldSpacePoint.x = leftOfUsIfPlacedToLeftOfElement;
                                        worldSpacePoint.y = minMaxY.x; //and align our top with the top of this
                                        alignedToSideOnly = true;
                                    }
                                }
                            }

                            if ( !alignedToSideOnly )
                            {
                                float bottomOfUsIfPlacedBelowElement = minMaxY.y //"max y" is a smaller number, further down the screen
                                        - sizeDelta.y; //we must subtract the size, because we are going down not up

                                if ( bottomOfUsIfPlacedBelowElement > ArcenUI.Instance.world_BottomRight.y )
                                {
                                    //if there IS room below, then place us there:
                                    worldSpacePoint.y = minMaxY.y; //the top of our tooltip goes at the bottom of our element we are aligning to
                                }
                                else
                                {
                                    float topOfUsIfPlacedAboveElement = minMaxY.x //"min y" is a larger number, higher up the screen
                                        + sizeDelta.y; //we must add the size, because we are going up instead of down

                                    if ( topOfUsIfPlacedAboveElement < ArcenUI.Instance.world_TopRight.y )
                                    {
                                        //if there IS room above, then place us there:
                                        worldSpacePoint.y = topOfUsIfPlacedAboveElement;
                                    }
                                }
                            }

                            worldSpacePoint.x = Mathf.Min( worldSpacePoint.x, maxXPixel );
                            worldSpacePoint.y = Mathf.Max( worldSpacePoint.y, maxYPixel );
                        }
                    }
                }

                debugStage = 9000;
                //this.MyElement.Window.SetPositionIfNeeded( worldSpacePoint );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Tooltip Position Error at debugStage " + debugStage + ": " + e, Verbosity.ShowAsError );
            }
        }

        public void SetText( string Text, string NewTooltipScaleType)
        {
            SetText( Text, NewTooltipScaleType, null );
        }

        public void SetText( IArcenUIElementForSizing MustBeAboveOrBelow, string Text)
        {
            SetText( Text, string.Empty, MustBeAboveOrBelow );
        }

        //Same-target throttle: see Window_AtMouseTooltipPanelWide.cs SetText for the
        //rationale (skip re-measure within 0.3s when hovering the same target).
        private IArcenUIElementForSizing lastSetTextTarget;
        private string lastSetScaleType = string.Empty;
        private float lastSetTextRealWorkTime = float.MinValue;
        public void SetText( string Text, string NewTooltipScaleType, IArcenUIElementForSizing MustBeAboveOrBelow )
        {
            if ( Text == null )
                Text = string.Empty;

            this.TimeLastSet = ArcenTime.TimeSinceStartF;
            if ( Text == this.lastSetText )
                return;
            if ( object.ReferenceEquals( MustBeAboveOrBelow, this.lastSetTextTarget ) &&
                 NewTooltipScaleType == this.lastSetScaleType &&
                 ArcenTime.TimeSinceStartF - this.lastSetTextRealWorkTime < 0.3f )
                return;

            //Text = ExternalConstants.Instance.GetCustomString_Slow("tooltip_text_loremipsum");
            this.lastSetText = Text;
            this.lastSetTextTarget = MustBeAboveOrBelow;
            this.lastSetScaleType = NewTooltipScaleType;
            this.lastSetTextRealWorkTime = ArcenTime.TimeSinceStartF;

            Engine_AIW2.InvokeOnClearTooltips();
                
            //this.NextTextToShow = Text;
            //this.NeedsToResize = true;
            //if (!string.IsNullOrEmpty(NewTooltipScaleType))
            //{
            //    var temp = GameSettings.Current.GetFloatBySetting( NewTooltipScaleType );
            //    this.tooltipScale = temp;
            //}
            //else
            //{
            //    this.tooltipScale = 1.0f;
            //}

            tooltipScale = GameSettings.Current.GetFloatBySetting( "ShipTooltipScale" );

            IsSkippingDrawing = string.IsNullOrEmpty(this.lastSetText);

            //mustBeAboveOrBelow = MustBeAboveOrBelow;

            
        }
    }
}
