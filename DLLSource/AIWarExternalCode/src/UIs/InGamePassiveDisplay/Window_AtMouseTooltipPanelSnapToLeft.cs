using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;
using TMPro;

namespace Arcen.AIW2.External
{
    public class Window_AtMouseTooltipPanelSnapToLeft : WindowControllerAbstractBase
    {
        //public static readonly ArcenDoubleCharacterBuffer TooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_AtMouseTooltipPanelSnapToLeft.TooltipBuffer" );
        
        public Window_AtMouseTooltipPanelSnapToLeft()
        {
            this.IsAtMouseTooltip = true;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true;
            Engine_AIW2.OnClearTooltips += new ClearTooltipsHandler( ClearMyself );
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( InputCaching.CalculateShouldTooltipsBeSuppressed() )
                return false;
            if ( bPanel.Instance == null )
                return true;
            //if ( bPanel.Instance.GetShouldBeHidden() )
            //    return false;
            return true;
        }

        public void ClearMyself()
        {
            bPanel.Instance?.ClearMyself();
        }

        public class bPanel : ImageButtonAbstractBase
        {
            public static bPanel Instance;
            public bPanel() { Instance = this; }

            public ArcenUI_ImageButton MyElement;
            //private ArcenUI_Image.SubImageGroup SubImages;
            private SubTextGroup SubTexts;
            private string NextTextToShow = string.Empty;
            private string WrappedNextTextToShow = string.Empty;
            private string LastTextToShow = string.Empty;
            private bool NeedsToResize = true;
            private float TimeLastSet;
            private float LastRequestedWidth;
            private float LastRequestedHeight;
            private bool hasSetCanvasOffset = false;

            public void ClearMyself()
            {
                this.lastSetText = string.Empty;
                this.lastSetTextTarget = null;
                this.lastSetTextRealWorkTime = float.MinValue;
                this.LastTextToShow = string.Empty;
                this.NextTextToShow = string.Empty;
                this.NeedsToResize = true;
            }

            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup _SubImages, SubTextGroup _SubTexts )
            {
                //this.SubImages = _SubImages;
                this.SubTexts = _SubTexts;
                this.DoResizeIfNeeded();
            }

            public void UpdateTextIfNeeded()
            {
                string nextText = this.WrappedNextTextToShow;
                if ( this.LastTextToShow.Length <= 0 && nextText.Length <= 0 || this.SubTexts == null )
                {
                    this.ClearMyself();
                    return;
                }

                try
                {
                    if ( this.LastTextToShow != nextText )
                    {
                        this.LastTextToShow = nextText;
                        this.SubTexts[0].Text.DirectlySetNextText( this.LastTextToShow );
                        this.NeedsToResize = true;
                        this.DoResizeIfNeeded();
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "Exception 在 UpdateContentFromVolatile for the single text element:" + e.ToString(), Verbosity.ShowAsError );
                }
            }

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
                int debugStage = 0;
                try
                {
                    debugStage = 100;
                    if ( !this.hasSetCanvasOffset )
                    {
                        debugStage = 200;
                        if ( this.MyElement != null && this.MyElement.Window != null )
                        {
                            debugStage = 300;
                            this.hasSetCanvasOffset = true;
                            this.MyElement.Window.SetOverridingCanvasSortingOrder( 32767 ); //as high as it will go, so this is always 在 top!
                        }
                    }

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

                    debugStage = 3000;
                    if ( !isMouseOnLeftHalfOfScreen )
                    {
                        debugStage = 3100;
                        //snap 到 left of screen
                        worldSpacePoint.x = ArcenUI.Instance.world_TopLeft.x;

                        debugStage = 3200;
                        if ( this.MustBeAboveOrBelow != null )
                        {
                            debugStage = 3300;
                            RectTransform rTran = this.MustBeAboveOrBelow.GetRelevantRect();
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
                        if ( this.MustBeAboveOrBelow != null )
                        {
                            debugStage = 7200;
                            RectTransform rTran = this.MustBeAboveOrBelow.GetRelevantRect();
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
                    if ( this.MustBeAboveOrBelow != null )
                    {
                        debugStage = 8100;
                        RectTransform rTran = this.MustBeAboveOrBelow.GetRelevantRect();
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
                                        ( this.MustBeAboveOrBelow == null || !(this.MustBeAboveOrBelow is ArcenUI_Slider) ) ) //if a slider, never put it 到 the right
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
                    this.MyElement.Window.SetPositionIfNeeded( worldSpacePoint );
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Tooltip Position Error at debugStage " + debugStage + ": " + e, Verbosity.ShowAsError );
                }
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

            private IArcenUIElementForSizing MustBeAboveOrBelow = null;

            private string lastSetText = string.Empty;
            //Same-target throttle: see Window_AtMouseTooltipPanelWide.cs SetText for the
            //rationale (skip re-measure within 0.3s when hovering the same target).
            private IArcenUIElementForSizing lastSetTextTarget;
            private float lastSetTextRealWorkTime = float.MinValue;
            public void SetText( IArcenUIElementForSizing MustBeAboveOrBelow, string Text )
            {
                if ( Text == null || Text.Length <= 0 )
                    return;

                this.MustBeAboveOrBelow = MustBeAboveOrBelow;
                this.TimeLastSet = ArcenTime.TimeSinceStartF;
                if ( Text == this.lastSetText )
                    return;
                if ( object.ReferenceEquals( MustBeAboveOrBelow, this.lastSetTextTarget ) &&
                     ArcenTime.TimeSinceStartF - this.lastSetTextRealWorkTime < 0.3f )
                    return;
                this.lastSetText = Text;
                this.lastSetTextTarget = MustBeAboveOrBelow;
                this.lastSetTextRealWorkTime = ArcenTime.TimeSinceStartF;

                Engine_AIW2.InvokeOnClearTooltips();

                this.NextTextToShow = Text;
                this.NeedsToResize = true;
            }

            public override void OnUpdate()
            {
                this.DoResizeIfNeeded();
                base.OnUpdate();
            }

            private float lastGeneralTooltipScale = -1f;
            private const int BASE_TOOLTIP_WIDTH = 540;

            private string lastTextToWrap = string.Empty;
            public void DoResizeIfNeeded()
            {
                float newGeneralTooltipScale = GameSettings.Current.GetFloatBySetting( "GeneralTooltipScale" );
                bool hasScaleMismatch = newGeneralTooltipScale != lastGeneralTooltipScale;
                if ( hasScaleMismatch )
                    this.NeedsToResize = true;

                if ( this.MyElement != null && 
                     this.NeedsToResize && 
                     this.SubTexts != null && 
                     this.SubTexts[1].Obj.activeInHierarchy && 
                     !string.IsNullOrEmpty(this.NextTextToShow))
                {
                    this.NeedsToResize = false;
                    this.lastGeneralTooltipScale = newGeneralTooltipScale;
                    string text = this.NextTextToShow;

                    // this tooltip is used for a lot of things that are not entities
                    // so use the original sizing, which allows it 到 become LESS wide than the base width.
                    
                    //int min_width, max_width;
                    //ArcenUI.Instance.Tooltip_Width(BASE_TOOLTIP_WIDTH, out min_width, out max_width);
                    
                    int min_width = 0;
                    int max_width = BASE_TOOLTIP_WIDTH;
                    float min = min_width;// * newGeneralTooltipScale;
                    float max = max_width;// * newGeneralTooltipScale;
                        
                    if ( hasScaleMismatch )
                    {
                        this.SubTexts[1].ReferenceText.rectTransform.UI_SetWidth( max_width );
                        //this.SubTexts[0].Obj.transform.parent.localScale = new Vector3( newGeneralTooltipScale, newGeneralTooltipScale, newGeneralTooltipScale );
                    }
                    
                    this.MyElement.gameObject.transform.localScale = new Vector3( newGeneralTooltipScale, newGeneralTooltipScale, newGeneralTooltipScale );

                    if ( this.lastTextToWrap != text || hasScaleMismatch )
                    {
                        this.lastTextToWrap = text;
                        var messageSize = ArcenUI.Instance.CalculatePreferredTextObjectDimensions( this.SubTexts[1].ReferenceText, text, min, max );
                        this.WrappedNextTextToShow = this.NextTextToShow;
                        this.LastRequestedWidth = messageSize.x;
                        this.LastRequestedHeight = messageSize.y;
                    }

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
    }
}
