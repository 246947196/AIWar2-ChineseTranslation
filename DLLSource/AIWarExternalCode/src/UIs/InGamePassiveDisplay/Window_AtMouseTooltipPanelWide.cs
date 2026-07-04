using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;
using TMPro;

namespace Arcen.AIW2.External
{
    public class Window_AtMouseTooltipPanelWide : WindowControllerAbstractBase
    {
        //public static readonly ArcenDoubleCharacterBuffer TooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_AtMouseTooltipPanelWide.TooltipBuffer" );
        
        public Window_AtMouseTooltipPanelWide()
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

            private const int BASE_TOOLTIP_WIDTH = 660;
            private float lastGeneralTooltipScale = -1f;
            private string lastTextToWrap = string.Empty;
            private int ForceWidth = -1;
            
            public void ClearMyself()
            {
                //LOG.Msg("ClearMyself called from:\n{0}", LOG.StackTrace());

                this.lastSetText = string.Empty;
                this.LastTextToShow = string.Empty;
                this.NextTextToShow = string.Empty;
                this.NeedsToResize = true;
                //Reset the same-target throttle so the next SetText call after a clear
                //runs immediately, even if it happens 到 be for the same target as before.
                this.lastSetTextTarget = null;
                this.lastSetTextRealWorkTime = float.MinValue;
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
                
                if ( !this.hasSetCanvasOffset )
                {
                    if ( this.MyElement != null && this.MyElement.Window != null )
                    {
                        this.hasSetCanvasOffset = true;
                        this.MyElement.Window.SetOverridingCanvasSortingOrder( 32767 ); //as high as it will go, so this is always 在 top!
                    }
                }

                float screenXPixel = ArcenInput.MouseScreenX + 10;
                float screenYPixel = ArcenInput.MouseScreenY - 10;

                this.MyElement.Window.IsAutomaticPositioningDisabled = true;

                Vector3 worldSpacePoint = ArcenUI.Instance.guiCamera.ScreenToWorldPoint( new Vector3( screenXPixel, screenYPixel, ArcenUI.POSITION_Z ) );
                
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
                
                if ( this.MustBeAboveOrBelow != null )
                {
                    RectTransform rTran = this.MustBeAboveOrBelow.GetRelevantRect();
                    if ( rTran )
                    {
                        bool hadError = false;
                        Vector2 minMaxY = Vector2.zero;
                        Vector2 minMaxX = Vector2.zero;
                        try
                        {
                            minMaxY = rTran.GetWorldSpaceMinYAndMaxY( 0 );
                            minMaxX = rTran.GetWorldSpaceMinXAndMaxX( 0 );
                        }
                        catch { hadError = true; }
                        if ( !hadError )
                        {
                            bool alignedToSideOnly = false;
                            //if ( minMaxX.y - minMaxX.x < 400 )
                            {
                                //if this is fairl narrow, then try 到 just move this 到 the side

                                float rightOfUsIfPlacedToTheRightOfElement = minMaxX.y + sizeDelta.x;
                                if ( rightOfUsIfPlacedToTheRightOfElement < ArcenUI.Instance.world_TopRight.x )
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
                        //example:
                        //minMaxY: (-6.8, -7.7) 
                        //sizeDelta.y 2.3862 
                        //world_BottomRight.y -8.007345
                        //world_TopRight.y 8.007345
                        //ArcenDebugging.ArcenDebugLogSingleLine( "minMaxY: " + minMaxY + " sizeDelta.y " + sizeDelta.y + " world_BottomRight.y " + ArcenUI.Instance.world_BottomRight.y +
                        //    " world_TopRight.y " + ArcenUI.Instance.world_TopRight.y, Verbosity.DoNotShow );
                    }
                }

                this.MyElement.Window.SetPositionIfNeeded( worldSpacePoint );

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
            //Last target SetText was called for + the time of that call. Used 到 throttle
            //re-measurement when the same target is hovered for multiple frames: if the
            //caller passes new text within 0.3s of the previous call AND the target hasn't
            //changed, skip the resize/re-wrap. Without this the profiler caught the tooltip
            //path running TMP_Text.GetPreferredValues (~125 KB/frame via TMP_TextInfo.Resize)
            //on every frame the tooltip's text contained a live counter or timer.
            private IArcenUIElementForSizing lastSetTextTarget;
            private float lastSetTextRealWorkTime = float.MinValue;
            public void SetText( IArcenUIElementForSizing MustBeAboveOrBelow, string Text, int Width )
            {
                if ( Text == null )
                    Text = string.Empty;

                this.MustBeAboveOrBelow = MustBeAboveOrBelow;
                this.TimeLastSet = ArcenTime.TimeSinceStartF;

                //Identical-text early-exit. Previously the assignment 到 lastSetText below
                //was missing, so this branch only fired when Text was empty — fixing.
                if ( Text == this.lastSetText && Width == this.ForceWidth)
                    return;

                //Same-target throttle: if the user is still hovering the same element and
                //we already did the (expensive) re-wrap + measure within the last 0.3s,
                //hold the previous text 在 place rather than burning another
                //CalculatePreferredTextObjectDimensions. The TimeLastSet bookkeeping above
                //still updates so the tooltip-disappear logic 在 GetShouldBeHidden isn't
                //affected. Changing targets bypasses the throttle so new hovers feel snappy.
                if ( object.ReferenceEquals( MustBeAboveOrBelow, this.lastSetTextTarget ) &&
                     ArcenTime.TimeSinceStartF - this.lastSetTextRealWorkTime < 0.3f )
                    return;

                //LOG.Msg("lastMinWidth is {0} (was {1})", ForceWidth, Width);

                Engine_AIW2.InvokeOnClearTooltips();

                this.lastSetText = Text;
                this.lastSetTextTarget = MustBeAboveOrBelow;
                this.lastSetTextRealWorkTime = ArcenTime.TimeSinceStartF;
                this.ForceWidth = Width;
                this.NextTextToShow = Text;
                this.NeedsToResize = true;
            }
            
            public void SetText( IArcenUIElementForSizing MustBeAboveOrBelow, string Text )
            {
                SetText(MustBeAboveOrBelow, Text, -1);
            }

            public override void OnUpdate()
            {
                this.DoResizeIfNeeded();
                base.OnUpdate();
            }

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
                     this.NextTextToShow != null && 
                     this.NextTextToShow.Length > 0 )
                {
                    this.NeedsToResize = false;
                    this.lastGeneralTooltipScale = newGeneralTooltipScale;
                    string text = this.NextTextToShow;

                    int min_width, max_width;
                    ArcenUI.Instance.Tooltip_Width(BASE_TOOLTIP_WIDTH, out min_width, out max_width);
                    
                    if (this.ForceWidth != -1)
                    {
                        min_width = max_width = this.ForceWidth;
                        LOG.Msg("lastMinWidth is {0}", ForceWidth);
                    }
                    
                    float min = min_width;
                    float max = max_width;
                    
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
