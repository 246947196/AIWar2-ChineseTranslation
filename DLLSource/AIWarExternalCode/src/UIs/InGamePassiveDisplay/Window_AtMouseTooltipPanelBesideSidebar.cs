using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;
using TMPro;

namespace Arcen.AIW2.External
{
    public class Window_AtMouseTooltipPanelBesideSidebar : WindowControllerAbstractBase
    {
        public static Window_AtMouseTooltipPanelBesideSidebar WindowControllerInstance;
        public static readonly ArcenDoubleCharacterBuffer TooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_AtMouseTooltipPanelBesideSidebar.TooltipBuffer" );

        public Window_AtMouseTooltipPanelBesideSidebar()
        {
            WindowControllerInstance= this;
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
            //if ( bPanel.Instance.IsSkippingTextDraw )
            //    return false;
            //if ( ArcenTime.TimeSinceStartF - bPanel.Instance.TimeLastSet > ArcenUI.TimespanAfterWhichTooltipsDisappear )
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
            public float TimeLastSet;
            private float LastRequestedWidth;
            private float LastRequestedHeight;
            private bool hasSetCanvasOffset = false;

            public void ClearMyself()
            {
                this.lastSetText = string.Empty;
                this.lastSetScaleType = string.Empty;
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

                WindowControllerInstance.myXPositionScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" );
            }

            public bool IsSkippingTextDraw = false;
            public void UpdateTextIfNeeded()
            {
                string nextText = this.WrappedNextTextToShow;
                if ( this.LastTextToShow.Length <= 0 && nextText.Length <= 0 )
                {
                    //this.ClearMyself();
                    this.IsSkippingTextDraw = true;
                    return;
                }
                IsSkippingTextDraw = false;

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

                this.DoResizeIfNeeded();
                if ( !this.hasSetCanvasOffset )
                {
                    if ( this.MyElement != null && this.MyElement.Window != null )
                    {
                        this.hasSetCanvasOffset = true;
                        this.MyElement.Window.SetOverridingCanvasSortingOrder( 32767 ); //as high as it will go, so this is always 在 top!
                    }
                }

                float screenXPixel = 0;
                float screenYPixel = ArcenInput.MouseScreenY - 10;

                this.MyElement.Window.IsAutomaticPositioningDisabled = true;

                Vector3 worldSpacePoint = ArcenUI.Instance.guiCamera.ScreenToWorldPoint( new Vector3( screenXPixel, screenYPixel, ArcenUI.POSITION_Z ) );
                worldSpacePoint.x = Window_InGameSidebarShips.Instance.GetWorldSpaceMaxX( 5f * WindowControllerInstance.myXPositionScale, false );

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

            private string lastSetText = "T";
            //Same-target throttle: see Window_AtMouseTooltipPanelWide.cs SetText for the
            //rationale. BesideSidebar has no element target, so we use the scale-type
            //string as the proxy for "same tooltip context".
            private string lastSetScaleType = string.Empty;
            private float lastSetTextRealWorkTime = float.MinValue;
            public void SetText( string Text, string NewTooltipScaleType )
            {
                if ( Text == null )
                    Text = string.Empty;

                this.TimeLastSet = ArcenTime.TimeSinceStartF;
                if ( Text == this.lastSetText )
                    return;
                if ( NewTooltipScaleType == this.lastSetScaleType &&
                     ArcenTime.TimeSinceStartF - this.lastSetTextRealWorkTime < 0.3f )
                    return;
                this.lastSetText = Text;
                this.lastSetScaleType = NewTooltipScaleType;
                this.lastSetTextRealWorkTime = ArcenTime.TimeSinceStartF;

                Engine_AIW2.InvokeOnClearTooltips();

                this.NextTextToShow = Text;
                this.NeedsToResize = true;
                this.tooltipScaleType = NewTooltipScaleType;
            }

            public override void OnUpdate()
            {
                this.DoResizeIfNeeded();
                base.OnUpdate();
            }

            private float lastTooltipScale = 1f;
            private const int BASE_TOOLTIP_WIDTH = 660;
            private string tooltipScaleType = "GeneralTooltipScale";

            private string lastTextToWrap = string.Empty;
            public void DoResizeIfNeeded()
            {
                float scale = ArcenUI.Instance.Tooltip_Scale(tooltipScaleType);

                bool hasScaleMismatch = scale != lastTooltipScale;
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
                    this.lastTooltipScale = scale;
                    string text = this.NextTextToShow;

                    int min_width = 0;
                    int max_width = BASE_TOOLTIP_WIDTH;
                    
                    if (tooltipScaleType == "ShipTooltipScale")
                        ArcenUI.Instance.Tooltip_Width(BASE_TOOLTIP_WIDTH, out min_width, out max_width);

                    if ( hasScaleMismatch )
                    {
                        this.SubTexts[1].ReferenceText.rectTransform.UI_SetWidth( max_width );
                        //this.SubTexts[0].Obj.transform.parent.localScale = new Vector3( newGeneralTooltipScale, newGeneralTooltipScale, newGeneralTooltipScale );
                    }
                    
                    this.MyElement.gameObject.transform.localScale = new Vector3( lastTooltipScale, lastTooltipScale, lastTooltipScale );

                    if ( this.lastTextToWrap != text || hasScaleMismatch )
                    {
                        this.lastTextToWrap = text;
                        var messageSize = ArcenUI.Instance.CalculatePreferredTextObjectDimensions( this.SubTexts[1].ReferenceText, text, min_width, max_width );
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
