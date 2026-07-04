using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public abstract class WindowControllerAbstractBase : IArcenUI_Window_Controller
    {
        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "WindowControllerAbstractBases" );
        public WindowControllerAbstractBase()
        {
            RefTracker.IncrementObjectCount();
        }

        public ArcenUI_Window Window;
        public bool ShouldCauseAllOtherWindowsToNotShow;
        public bool ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow;
        public bool IsAtMouseTooltip;
        public bool ShouldShowEvenWhenGUIHidden;
        public bool OnlyShowInGame;
        public bool PreventsNormalInputHandlers;
        public bool IsPassiveWindowThatDoesNotAffectDropdowns = false;
        private static readonly List<WindowControllerAbstractBase> CurrentlyShownWindowsWith_ShouldCauseAllOtherWindowsToNotShow = 
            List<WindowControllerAbstractBase>.Create_WillNeverBeGCed( 80, "WindowControllerAbstractBase-CurrentlyShownWindowsWith_ShouldCauseAllOtherWindowsToNotShow" );

        public float myScale = 1f;
        public float myXPositionScale = 1f;
        public float myYPositionScale = 1f;

        private bool wasShowingLastFrame = false;

        public bool GetShouldDrawThisFrame()
        {
            bool result = true;
            
            if ( !this.ShouldCauseAllOtherWindowsToNotShow && !this.IsAtMouseTooltip && !this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow &&
                CurrentlyShownWindowsWith_ShouldCauseAllOtherWindowsToNotShow.Count > 0 )
                result = false;
            
            if ( ArcenUI.Instance.InHideGUIMode && !this.ShouldShowEvenWhenGUIHidden )
                result = false;
            
            if ( this.OnlyShowInGame )
            {
                if ( !World.Instance.IsLoaded )
                    result = false;
                if ( World_AIW2.Instance.IsOutsideOfNormalGameplay )
                    result = false;
            }
            
            if ( result )
                result = this.GetShouldDrawThisFrame_Subclass();

            if ( wasShowingLastFrame != result )
            {
                wasShowingLastFrame = result;
                if ( result )
                {
                    this.OnShowAfterNotShowing();
                    if ( !this.IsPassiveWindowThatDoesNotAffectDropdowns )
                        ArcenUI.HideAnyOpenDropdowns(); //make sure if a new window opens, all the dropdowns get hidden
                }
                else
                {
                    this.OnHideAfterShowing();
                }
            }

            if ( this.ShouldCauseAllOtherWindowsToNotShow )
            {
                if ( result )
                {
                    if ( !CurrentlyShownWindowsWith_ShouldCauseAllOtherWindowsToNotShow.Contains( this ) )
                        CurrentlyShownWindowsWith_ShouldCauseAllOtherWindowsToNotShow.Add( this );
                }
                else
                {
                    if ( CurrentlyShownWindowsWith_ShouldCauseAllOtherWindowsToNotShow.Contains( this ) )
                        CurrentlyShownWindowsWith_ShouldCauseAllOtherWindowsToNotShow.Remove( this );
                }
            }
            
            if ( !result )
                this.OnShowingRefused();
            
            return result;
        }

        public virtual void OnShowAfterNotShowing() { }
        public virtual void OnHideAfterShowing() { }

        public virtual bool GetShouldDrawThisFrame_Subclass()
        {
            return true;
        }

        public virtual void SetWindow( ArcenUI_Window Window )
        {
            this.Window = Window;
        }

        public virtual void OnShowingRefused() { }

        public virtual void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set ) { }

        bool IArcenUI_Window_Controller.PreventsNormalInputHandlers => this.PreventsNormalInputHandlers;

        public static ArcenUI_CreateElementDirective _AddBase( string prefabName, ArcenUI_SetOfCreateElementDirectives Set, ArcenCachedExternalTypeDirect ControllerType,
            string CodeDirectiveTagString, int CodeDirectiveTag1, int CodeDirectiveTag2, string ParentTabName, Rect rect, float FontSize )
        {
            // slightly shrink the dimensions of everything so that there aren't weird edge problems with adjacent buttons, etc
            if ( rect.width >= 0.2f )
            {
                rect.x += 0.05f;
                rect.width -= 0.1f;
            }

            if ( rect.height >= 0.2f )
            {
                rect.y += 0.05f;
                rect.height -= 0.1f;
            }

            ArcenUI_CreateElementDirective directive = Set.GetNextFree( prefabName, ControllerType, CodeDirectiveTag1, CodeDirectiveTag2, CodeDirectiveTagString, rect.x, rect.y, rect.width, rect.height );
            directive.ParentTabName = ParentTabName;
            directive.FontSize = FontSize;
            return directive;
        }

        public RectTransform PrimeRect;

        private Vector3[] fourCorners = new Vector3[4];
        public float GetWorldSpaceMaxX( float Buffer, bool CrashIfPrimeRectNull )
        {
            if ( PrimeRect == null )
            {
                if ( CrashIfPrimeRectNull )
                    throw new Exception( "PrimeRect not set on " + this.Window.InternalName + "!" );
                else
                    return 0;
            }
            Buffer *= ArcenUI.ratioFromScreenSize;
            //The returned array of 4 vertices is clockwise. It starts bottom left and rotates to top left, 
            //then top right, and finally bottom right. 
            //Note that bottom left, for example, is an (x, y, z) vector with x being left and y being bottom.
            this.PrimeRect.GetWorldCorners( fourCorners );
            return fourCorners[1].x + Buffer;
        }

        public float GetWorldSpaceTopY( float Buffer, bool CrashIfPrimeRectNull )
        {
            if ( PrimeRect == null )
            {
                if ( CrashIfPrimeRectNull )
                    throw new Exception( "PrimeRect not set on " + this.Window.InternalName + "!" );
                else
                    return 0;
            }
            Buffer *= ArcenUI.ratioFromScreenSize;
            //The returned array of 4 vertices is clockwise. It starts bottom left and rotates to top left, 
            //then top right, and finally bottom right. 
            //Note that bottom left, for example, is an (x, y, z) vector with x being left and y being bottom.
            this.PrimeRect.GetWorldCorners( fourCorners );
            return fourCorners[1].y + Buffer;
        }

        private static readonly string BACKGROUND_PREFAB_NAME = "SimpleWindowBackground";
        private static readonly string TEXT_PREFAB_NAME = "HoverableText";
        private static readonly string BUTTON_PREFAB_NAME = "ButtonBlue";
        private static readonly string INPUT_PREFAB_NAME = "BasicTextbox";
        private static readonly string VERTICAL_SLIDER_PREFAB_NAME = "BasicVerticalSlider";
        private static readonly string HORIZONTAL_SLIDER_PREFAB_NAME = "BasicHorizontalSlider";
        private static readonly string DROPDOWN_PREFAB_NAME = "BasicDropdown";

        protected static ArcenUI_CreateElementDirective AddBackground( ArcenUI_SetOfCreateElementDirectives Set, ArcenCachedExternalTypeDirect ControllerType, string CodeDirectiveTagString, int CodeDirectiveTag1, int CodeDirectiveTag2, Rect rect )
        {
            return AddBackground( Set, ControllerType, CodeDirectiveTagString, CodeDirectiveTag1, CodeDirectiveTag2, string.Empty, rect );
        }
        protected static ArcenUI_CreateElementDirective AddBackground( ArcenUI_SetOfCreateElementDirectives Set, ArcenCachedExternalTypeDirect ControllerType, string CodeDirectiveTagString, int CodeDirectiveTag1, int CodeDirectiveTag2, string ParentTabName, Rect rect )
        {
            return _AddBase( BACKGROUND_PREFAB_NAME, Set, ControllerType, CodeDirectiveTagString, CodeDirectiveTag1, CodeDirectiveTag2, ParentTabName, rect, -1f );
        }

        protected static ArcenUI_CreateElementDirective AddText( ArcenUI_SetOfCreateElementDirectives Set, ArcenCachedExternalTypeDirect ControllerType, string CodeDirectiveTagString, int CodeDirectiveTag1, int CodeDirectiveTag2, Rect rect, float FontSize )
        {
            return AddText( Set, ControllerType, CodeDirectiveTagString, CodeDirectiveTag1, CodeDirectiveTag2, string.Empty, rect, FontSize );
        }
        protected static ArcenUI_CreateElementDirective AddText( ArcenUI_SetOfCreateElementDirectives Set, ArcenCachedExternalTypeDirect ControllerType, string CodeDirectiveTagString, int CodeDirectiveTag1, int CodeDirectiveTag2, string ParentTabName, Rect rect, float FontSize )
        {
            return _AddBase( TEXT_PREFAB_NAME, Set, ControllerType, CodeDirectiveTagString, CodeDirectiveTag1, CodeDirectiveTag2, ParentTabName, rect, FontSize );
        }

        protected static ArcenUI_CreateElementDirective AddButton( ArcenUI_SetOfCreateElementDirectives Set, ArcenCachedExternalTypeDirect ControllerType, string CodeDirectiveTagString, int CodeDirectiveTag1, int CodeDirectiveTag2, Rect rect, float FontSize )
        {
            return AddButton( Set, ControllerType, CodeDirectiveTagString, CodeDirectiveTag1, CodeDirectiveTag2, string.Empty, rect, FontSize );
        }
        protected static ArcenUI_CreateElementDirective AddButton( ArcenUI_SetOfCreateElementDirectives Set, ArcenCachedExternalTypeDirect ControllerType, string CodeDirectiveTagString, int CodeDirectiveTag1, int CodeDirectiveTag2, string ParentTabName, Rect rect, float FontSize )
        {
            return _AddBase( BUTTON_PREFAB_NAME, Set, ControllerType, CodeDirectiveTagString, CodeDirectiveTag1, CodeDirectiveTag2, ParentTabName, rect, FontSize );
        }

        protected static ArcenUI_CreateElementDirective AddButtonCustom( string PrefabName, ArcenUI_SetOfCreateElementDirectives Set, ArcenCachedExternalTypeDirect ControllerType, string CodeDirectiveTagString, int CodeDirectiveTag1, int CodeDirectiveTag2, Rect rect, float FontSize )
        {
            return AddButtonCustom( PrefabName, Set, ControllerType, CodeDirectiveTagString, CodeDirectiveTag1, CodeDirectiveTag2, string.Empty, rect, FontSize );
        }
        protected static ArcenUI_CreateElementDirective AddButtonCustom( string PrefabName, ArcenUI_SetOfCreateElementDirectives Set, ArcenCachedExternalTypeDirect ControllerType, string CodeDirectiveTagString, int CodeDirectiveTag1, int CodeDirectiveTag2, string ParentTabName, Rect rect, float FontSize )
        {
            return _AddBase( PrefabName, Set, ControllerType, CodeDirectiveTagString, CodeDirectiveTag1, CodeDirectiveTag2, ParentTabName, rect, FontSize );
        }

        protected static ArcenUI_CreateElementDirective AddInput( ArcenUI_SetOfCreateElementDirectives Set, ArcenCachedExternalTypeDirect ControllerType, string CodeDirectiveTagString, int CodeDirectiveTag1, int CodeDirectiveTag2, Rect rect )
        {
            return AddInput( Set, ControllerType, CodeDirectiveTagString, CodeDirectiveTag1, CodeDirectiveTag2, string.Empty, rect );
        }
        protected static ArcenUI_CreateElementDirective AddInput( ArcenUI_SetOfCreateElementDirectives Set, ArcenCachedExternalTypeDirect ControllerType, string CodeDirectiveTagString, int CodeDirectiveTag1, int CodeDirectiveTag2, string ParentTabName, Rect rect )
        {
            return _AddBase( INPUT_PREFAB_NAME, Set, ControllerType, CodeDirectiveTagString, CodeDirectiveTag1, CodeDirectiveTag2, ParentTabName, rect, -1f );
        }

        protected static ArcenUI_CreateElementDirective AddVerticalSlider( ArcenUI_SetOfCreateElementDirectives Set, ArcenCachedExternalTypeDirect ControllerType, string CodeDirectiveTagString, int CodeDirectiveTag1, int CodeDirectiveTag2, Rect rect )
        {
            return AddVerticalSlider( Set, ControllerType, CodeDirectiveTagString, CodeDirectiveTag1, CodeDirectiveTag2, string.Empty, rect );
        }
        protected static ArcenUI_CreateElementDirective AddVerticalSlider( ArcenUI_SetOfCreateElementDirectives Set, ArcenCachedExternalTypeDirect ControllerType, string CodeDirectiveTagString, int CodeDirectiveTag1, int CodeDirectiveTag2, string ParentTabName, Rect rect )
        {
            return _AddBase( VERTICAL_SLIDER_PREFAB_NAME, Set, ControllerType, CodeDirectiveTagString, CodeDirectiveTag1, CodeDirectiveTag2, ParentTabName, rect, -1f );
        }

        protected static ArcenUI_CreateElementDirective AddHorizontalSlider( ArcenUI_SetOfCreateElementDirectives Set, ArcenCachedExternalTypeDirect ControllerType, string CodeDirectiveTagString, int CodeDirectiveTag1, int CodeDirectiveTag2, Rect rect )
        {
            return AddHorizontalSlider( Set, ControllerType, CodeDirectiveTagString, CodeDirectiveTag1, CodeDirectiveTag2, string.Empty, rect );
        }
        protected static ArcenUI_CreateElementDirective AddHorizontalSlider( ArcenUI_SetOfCreateElementDirectives Set, ArcenCachedExternalTypeDirect ControllerType, string CodeDirectiveTagString, int CodeDirectiveTag1, int CodeDirectiveTag2, string ParentTabName, Rect rect )
        {
            return _AddBase( HORIZONTAL_SLIDER_PREFAB_NAME, Set, ControllerType, CodeDirectiveTagString, CodeDirectiveTag1, CodeDirectiveTag2, ParentTabName, rect, -1f );
        }

        protected static ArcenUI_CreateElementDirective AddDropdown( ArcenUI_SetOfCreateElementDirectives Set, ArcenCachedExternalTypeDirect ControllerType, string CodeDirectiveTagString, int CodeDirectiveTag1, int CodeDirectiveTag2, Rect rect )
        {
            return AddDropdown( Set, ControllerType, CodeDirectiveTagString, CodeDirectiveTag1, CodeDirectiveTag2, string.Empty, rect, -1 );
        }
        protected static ArcenUI_CreateElementDirective AddDropdown( ArcenUI_SetOfCreateElementDirectives Set, ArcenCachedExternalTypeDirect ControllerType, string CodeDirectiveTagString, int CodeDirectiveTag1, int CodeDirectiveTag2, Rect rect, float FontSize )
        {
            return AddDropdown( Set, ControllerType, CodeDirectiveTagString, CodeDirectiveTag1, CodeDirectiveTag2, string.Empty, rect, FontSize );
        }
        protected static ArcenUI_CreateElementDirective AddDropdown( ArcenUI_SetOfCreateElementDirectives Set, ArcenCachedExternalTypeDirect ControllerType, string CodeDirectiveTagString, int CodeDirectiveTag1, int CodeDirectiveTag2, string ParentTabName, Rect rect, float FontSize )
        {
            return _AddBase( DROPDOWN_PREFAB_NAME, Set, ControllerType, CodeDirectiveTagString, CodeDirectiveTag1, CodeDirectiveTag2, ParentTabName, rect, FontSize );
        }

        protected static void LayOutButtonSetAndSizeCanvasAndSelfHeight( ArcenUI_ButtonSet elementAsType, List<IArcenUI_Button_Controller> buttonControllers )
        {
            var x_max = elementAsType.Window.UISpaceWidth;
            var y_max = elementAsType.Window.UISpaceHeight;

            Vector2 buttonSize;
            buttonSize.x = elementAsType.ButtonWidth;
            buttonSize.y = elementAsType.ButtonHeight;
            
            int num_columns = 1;
            while (buttonControllers.Count / num_columns * buttonSize.y > y_max)
                num_columns++;
            
            int num_rows = (buttonControllers.Count / (float)num_columns).RoundUp();
            
            Vector2 cur_pos = new Vector2( 0, 0 );

            elementAsType.UISpaceWidth = elementAsType.ButtonWidth * num_columns;
            elementAsType.UISpaceHeight = elementAsType.ButtonHeight * num_rows;

            
            //LOG.Msg("LayOutButtonSetAndSizeCanvasAndSelfHeight; x_max={0} y_max={1} button.x={2} button.y={3} num_columns={4} num_rows={5} cur_pos={6} uiwidth={7} uiheight={8}",
            //    x_max, y_max, buttonSize.x, buttonSize.y, num_columns, num_rows, cur_pos, elementAsType.UISpaceWidth, elementAsType.UISpaceHeight);
            
            int itr = 0;
            for (int x = 0; x < num_columns; x++)
            {
                cur_pos.y = 0;
                
                for (int y = 0; y < num_rows; y++)
                {
                    //LOG.Msg("button#{0} x={1} y={2}", itr, cur_pos.x, cur_pos.y);
                    
                    elementAsType.AddButton( buttonControllers[itr], buttonSize, cur_pos, -1 );
                    
                    itr++;
                    if (itr >= buttonControllers.Count)
                        goto done;
                    
                    cur_pos.y += buttonSize.y;
                }
                
                cur_pos.x += buttonSize.x;
            }
            
            done:
            return;
        }

        protected static void LayOutButtonSet( ArcenUI_ButtonSet elementAsType, List<IArcenUI_Button_Controller> buttonControllers )
        {
            Vector2 buttonSize;
            buttonSize.x = elementAsType.ButtonWidth;
            buttonSize.y = elementAsType.ButtonHeight;

            float buttonSetHeight = elementAsType.Window.UISpaceHeight;
            int buttonSlots = Mathf.FloorToInt( buttonSetHeight / buttonSize.y );
            int emptySlots = buttonSlots - buttonControllers.Count;
            Vector2 runningOffset = Mat.V2_Zero;
            runningOffset.y = emptySlots * buttonSize.y;
            for ( int i = 0; i < buttonControllers.Count; i++ )
            {
                elementAsType.AddButton( buttonControllers[i], buttonSize, runningOffset, -1 );
                runningOffset.y += buttonSize.y;
            }
        }

        private float lastScale = -1f;
        public void SetAdjustedWorldSpaceSize()
        {
            float scaleToUse = ArcenUI.uiScaleToUse * this.myScale;
            if ( scaleToUse == lastScale )
                return;
            lastScale = scaleToUse;

            Vector3 finalUIScaleV3 = new Vector3( scaleToUse, scaleToUse, scaleToUse );
            this.Window.SetLocalScaleIfNeeded( finalUIScaleV3 );
        }

        public float ExtraOffsetX = 0;
        public float ExtraOffsetY = 0;

        //private Vector3 localPosWorldSpace;
        private bool hasPositionEverBeenSet = false;

        public void PositionWindow()
        {
            if ( this.Window.IsAutomaticPositioningDisabled )
                return;
            if ( !this.Window.IsXOrYDirty )
                return;
            if ( !this.Window.GetIsConsideredActive() && this.hasPositionEverBeenSet )
                return;

            this.hasPositionEverBeenSet = true;
            this.Window.IsXOrYDirty = false;
            float unitsPerUIPixelX = ArcenUI.unitsPerUIPixelX * this.myXPositionScale;
            float unitsPerUIPixelY = ArcenUI.unitsPerUIPixelY * this.myYPositionScale;

            //ArcenDebugging.ArcenDebugLogWrapper( "A:" + window.CanvasGO.name + " "  + window.CanvasGO.transform.position + " " + rt.rect + " " + rt.pivot );

            Vector2 pivot = Mat.V2_Zero;
            Vector3 localPos = Mat.V3_Zero;
            localPos.z = ArcenUI.POSITION_Z;
            switch ( this.Window.XAlignment.Type )
            {
                default:
                case ArcenUI_AxisAlignmentType.MinimumOfScreen:
                    localPos.x = -ArcenUI.worldHalfSize.x;
                    pivot.x = 0;
                    break;
                case ArcenUI_AxisAlignmentType.MiddleOfScreen:
                    {
                        pivot.x = 0.5f;
                        localPos.x = 0f;
                    }
                    break;
                case ArcenUI_AxisAlignmentType.MaximumOfScreen:
                    pivot.x = 1f;
                    localPos.x = ArcenUI.worldHalfSize.x;
                    break;
            }
            switch ( this.Window.YAlignment.Type )
            {
                default:
                case ArcenUI_AxisAlignmentType.MinimumOfScreen:
                    localPos.y = ArcenUI.worldHalfSize.y;
                    pivot.y = 1;
                    break;
                case ArcenUI_AxisAlignmentType.MiddleOfScreen:
                    {
                        localPos.y = 0;
                        pivot.y = 0.5f;
                    }
                    break;
                case ArcenUI_AxisAlignmentType.MaximumOfScreen:
                    localPos.y = -ArcenUI.worldHalfSize.y;
                    pivot.y = 0f;
                    break;
            }

            localPos.x += (unitsPerUIPixelX * (this.Window.UISpaceX + this.ExtraOffsetX));
            //y is negative because unity is handling the coordinate system from the bottom-up here, and we want to switch that.
            localPos.y -= (unitsPerUIPixelY * (this.Window.UISpaceY + this.ExtraOffsetY));

            this.Window.SetPivotIfNeeded( pivot );
            this.Window.SetLocalPositionIfNeeded( localPos );
            //if ( this.InternalName.StartsWith( "Window_MainMenu" ) )
            //    ArcenDebugging.ArcenDebugLogWrapper( ArcenTime.Now + "   "  + this.localPosWorldSpace + "    x" + uiSpaceX + "      y" + uiSpaceY + "     u" + unitsPerUIPixel );
        }

        public void FlagAsDirty()
        {
            this.Window.IsXOrYDirty = true;
        }
    }

    public abstract class ToggleableWindowController : WindowControllerAbstractBase
    {
        public bool IsOpen;
        public bool SuppressesUIScaling;

        public void Open()
        {
            if ( this.IsOpen )
                return;
            if ( this.SuppressesUIScaling )
                ArcenUI.Instance.SuppressUIScaling = true;
            this.IsOpen = true;
            this.OnOpen();
        }

        public virtual void Close()
        {
            if ( !this.IsOpen )
                return;
            if ( this.SuppressesUIScaling )
                ArcenUI.Instance.SuppressUIScaling = false;
            this.IsOpen = false;
            if ( this.Window != null )
            {
                for ( int i = 0; i < this.Window.Elements.Count; i++ )
                {
                    ArcenUI_Element element = this.Window.Elements[i];
                    if ( !(element.Controller is WindowTogglingButtonController) )
                        continue;
                    WindowTogglingButtonController otherControllerAsType = (WindowTogglingButtonController)element.Controller;
                    ToggleableWindowController otherRelatedController = otherControllerAsType.GetRelatedController();
                    if ( !otherRelatedController.IsOpen )
                        continue;
                    otherRelatedController.Close();
                }
            }
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( !this.IsOpen )
                return false;
            return true;
        }

        public override void OnShowingRefused()
        {
            if ( !this.IsOpen )
                return;
            this.Close();
        }

        public virtual void OnOpen() { }
    }

    public abstract class StackMenuWindowController : WindowControllerAbstractBase
    {
        public bool IsPermanentBaseOfStack;

        private static readonly List<StackMenuWindowController> Stack = List<StackMenuWindowController>.Create_WillNeverBeGCed( 40, "StackMenuWindowController-Stack" );

        public static int StackCount => StackMenuWindowController.Stack.Count;

        public static StackMenuWindowController GetCurrentTopOfStack()
        {
            if ( Stack.Count <= 0 )
                return null;
            return Stack[Stack.Count - 1];
        }

        /// <summary>
        /// returns true if it found something to close
        /// </summary>
        public static bool CloseEntireStack( bool AlsoCloseAnyOptionalWindows )
        {
            bool result = false;
            while ( GetCurrentTopOfStack() != null && !StackMenuWindowController.GetCurrentTopOfStack().IsPermanentBaseOfStack )
            {
                GetCurrentTopOfStack().Close();
                result = true;
            }
            return result;
        }

        public static void CloseTopItemOnStack()
        {
            if ( GetCurrentTopOfStack() == null || StackMenuWindowController.GetCurrentTopOfStack().IsPermanentBaseOfStack )
            {
                if ( !CloseEntireStack( true ) ) // if didn't find anything to close, proceed to clear selection
                    Engine_AIW2.Instance.ClearSelection( true, true );
                return;
            }
            GetCurrentTopOfStack().Close();
        }

        public bool GetIsOnStack()
        {
            return Stack.Contains( this );
        }

        public void Open()
        {
            if ( this.GetIsOnStack() )
                return;
            Stack.Add( this );
            this.OnOpen();
        }

        public void Close()
        {
            if ( this.IsPermanentBaseOfStack )
                return;
            if ( !this.GetIsOnStack() )
                return;
            if ( this != GetCurrentTopOfStack() )
                ArcenDebugging.ArcenDebugLog( "Warning: tried to close StackMenuWindowController " + this.GetBriefName() + " when it was not on top of the stack", Verbosity.ShowAsError );
            Stack.Remove( this );
            this.OnClose();
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( StackMenuWindowController.GetCurrentTopOfStack() == null && this.IsPermanentBaseOfStack )
                this.Open();
            if ( this != StackMenuWindowController.GetCurrentTopOfStack() )
                return false;
            if ( !this.GetIsAllowedInCurrentContext() )
            {
                this.Close();
                return false;
            }
            if ( Window_FactionsWindow.Instance.IsOpen )
                return false;
            return true;
        }

        public virtual bool GetIsAllowedInCurrentContext() { return true; }

        public virtual void OnOpen() { }
        public virtual void OnClose() { }
        public abstract string GetBriefName();
    }

    public abstract class ElementAbstractBase : IArcenUI_Element_Controller
    {
        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "ElementAbstractBases" );
        public ElementAbstractBase()
        {
            RefTracker.IncrementObjectCount();
        }

        public ArcenUI_Element Element;
        public WindowControllerAbstractBase WindowController;

        public float AlternativeHeightToUseInAutoSizing = 0;
        public float ExtraSpaceBeforeInAutoSizing = 0;
        public float ExtraSpaceAfterInAutoSizing = 0;

        public virtual void OnUpdate() { }
        public virtual bool GetShouldBeHidden() { return false; }
        public virtual bool GetShouldRunUpdatesEvenWhenHidden() { return false; }
        public virtual void HandleMouseover() { }
        public virtual void OnMainThreadUpdate() { }

        public void SetElement( ArcenUI_Element Element )
        {
            if ( this.Element == Element )
                return; //if same one, don't bother doing the other bits

            this.Element = Element;
            this.WindowController = (WindowControllerAbstractBase)Element.Window.Controller;
        }

        public void SetSkipGetTextFor( float TimeSpan )
        {
            if ( this.Element == null )
                return;
            this.Element.SkipGetTextUntilTime = ArcenTime.TimeSinceStartF + TimeSpan;
        }
    }

    public abstract class TextAbstractBase : ElementAbstractBase, IArcenUI_Text_Controller
    {
        public abstract void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer );

        public MouseHandlingResult HandleClick( MouseHandlingInput input )
        {
            return this.HandleClick_Subclass( input );
        }

        public virtual MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input ) { return MouseHandlingResult.None; }

        public virtual MouseHandlingResult HandleHyperlinkClick( MouseHandlingInput Input, string LinkID )
        {
            return MouseHandlingResult.None;
        }
        public virtual void HandleHyperlinkHover( string LinkID )
        {
            
        }
    }

    public abstract class CustomUIAbstractBase : ElementAbstractBase, IArcenUI_CustomUI_Controller
    {
        private static Vector3[] fourCorners = new Vector3[4];

        public void TalkToControllingWindow()
        {
            this.WindowController.PrimeRect = this.Element.RelevantRect;
        }

        private float timeOfLastSize = 0;
        public void AdjustHeightToScreenMax( float Buffer, string BufferScaleMultiplierSetting, string WindowControllerMyScale, string WindowControllerMyYPosScale, params CustomUIAbstractBase[] OtherWindowsToMatchSize )
        {
            if ( ArcenTime.TimeSinceStartF - timeOfLastSize < 0.4f )
                return;
            timeOfLastSize = ArcenTime.TimeSinceStartF;

            this.WindowController.myYPositionScale = GameSettings.Current.GetFloatBySetting( WindowControllerMyYPosScale );
            this.WindowController.myScale = GameSettings.Current.GetFloatBySetting( WindowControllerMyScale );
            this.WindowController.SetAdjustedWorldSpaceSize();
            this.WindowController.PositionWindow();

            Buffer *= ArcenUI.ratioFromScreenSize;
            Buffer *= Mathf.Max( 1f, Screen.height / 460 );
            Buffer *= GameSettings.Current.GetFloatBySetting( BufferScaleMultiplierSetting );
            this.WindowController.PrimeRect = this.Element.RelevantRect;

            Vector2 lastSize = Mat.V2_Zero;
            bool setAnySizes = false;
            float diff;
            for ( int i = 0; i < 3; i++ )
            {
                this.Element.RelevantRect.GetWorldCorners( fourCorners );
                //float worldPointY = ArcenUI.Instance.guiCamera.WorldToScreenPoint( fourCorners[0] ).y;
                Vector2 size = Element.RelevantRect.sizeDelta;

                float percentage = Buffer / (float)Screen.height;
                float worldHeight = Mathf.Abs( ArcenUI.Instance.world_BottomLeft.y - ArcenUI.Instance.world_TopLeft.y );
                float desiredY = worldHeight * percentage;
                desiredY = ArcenUI.Instance.world_BottomLeft.y + desiredY;
                //Debug.Log( "   world_BottomLeft.y: " + ArcenUI.Instance.world_BottomLeft.y + "   world_TopLeft.y: " + ArcenUI.Instance.world_TopLeft.y +
                //    "  desiredY: " + desiredY + "  " + DateTime.Now );
                diff = (fourCorners[0].y - desiredY) / this.WindowController.myScale;

                float currentHeight = Mathf.Abs( fourCorners[0].y - fourCorners[1].y );
                //Debug.Log( "   diff: " + diff + "  currentHeight: " + currentHeight + "   " + DateTime.Now );
                if ( Mathf.Abs( diff ) < 0.01f )
                    break;
                float multiplier = (currentHeight + diff) / currentHeight;
                size.y *= multiplier;
                if ( size.y < 0 )
                    break;

                setAnySizes = true;
                lastSize = size;
                Element.RelevantRect.sizeDelta = size;
            }
            //Debug.Log( "setAnySizes: " + setAnySizes + " " + DateTime.Now );
            if ( !setAnySizes )
                return;
            //Debug.Log( "finalSize: " + size + " " + DateTime.Now );

            //Engine_Universal.DebugText = fourCorners[0] + " " + fourCorners[1] + " " + fourCorners[2] + " " + fourCorners[3] +
            //    "\n" + ArcenInput.MouseScreenX + "," + ArcenInput.MouseScreenY +
            //    "\n" + ArcenUI.Instance.guiCamera.WorldToScreenPoint( fourCorners[0] ) + " " +
            //    ArcenUI.Instance.guiCamera.WorldToScreenPoint( fourCorners[1] ) + " " +
            //    ArcenUI.Instance.guiCamera.WorldToScreenPoint( fourCorners[2] ) + " " +
            //    ArcenUI.Instance.guiCamera.WorldToScreenPoint( fourCorners[3] ) + " " +
            //    "\n" + ArcenUI.Instance.LastScreenHeight;

            CustomUIAbstractBase otherBase;
            for ( int i = 0; i < OtherWindowsToMatchSize.Length; i++ )
            {
                otherBase = OtherWindowsToMatchSize[i];
                if ( otherBase == null || otherBase == this )
                    continue;
                otherBase.Element.RelevantRect.sizeDelta = lastSize;
            }
        }
    }

    public abstract class DisappearingPanelAbstractBase : ElementAbstractBase, IArcenUI_DisappearingPanel_Controller
    {
        //private ArcenUI_DisappearingPanel ElementPanel;

        public override bool GetShouldBeHidden()
        {
            return !this.GetShouldShowThisFrameFromMainThread();
        }

        public abstract bool GetShouldShowThisFrameFromMainThread();
    }

    public abstract class ButtonAbstractBase : ElementAbstractBase, IArcenUI_Button_Controller
    {
        public virtual void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
        {
            bool grayedOut = this.GetShouldBeGrayedOut();

            if ( grayedOut )
                Buffer.Add( "<color=#999999>" );
        }

        public MouseHandlingResult HandleClick( MouseHandlingInput input )
        {
            if ( this.GetShouldBeGrayedOut() )
                return MouseHandlingResult.PlayClickDeniedSound;
            return this.HandleClick_Subclass( input );
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            StackMenuWindowController controllerAsType = this.WindowController as StackMenuWindowController;
            if ( controllerAsType != null && controllerAsType == StackMenuWindowController.GetCurrentTopOfStack() )
            {
                Color targetColor = ColorMath.White;
                int stackCount = StackMenuWindowController.StackCount;
                if ( stackCount > 1 ) targetColor = stackCount % 2 == 1 ? ColorMath.LightRed : ColorMath.LightGreen;
                ((ArcenUI_Button)this.Element).SetColor( targetColor );
            }
        }

        public virtual MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input ) { return MouseHandlingResult.None; }
        public virtual void DoAnyCustomButtonStuffFromVolatile( ArcenUI_Button Button ) { }
        public virtual bool GetShouldBeGrayedOut() { return false; }

        #region ButtonPool
        public virtual void Clear() { }
        public virtual ButtonAbstractBase DuplicateSelf()
        {
            ArcenUI_Button button = (ArcenUI_Button)this.Element.DuplicateSelf();
            ButtonAbstractBase controller = (ButtonAbstractBase)button.Controller;
            return controller;
        }
        public virtual int CompareSelfWithOther( ButtonAbstractBase Other )
        {
            return PREFER_NEITHER;
        }

        public const int PREFER_LEFT = -1;
        public const int PREFER_RIGHT = 1;
        public const int PREFER_NEITHER = 0;

        public class ButtonPool<T> where T : ButtonAbstractBase
        {
            private T original;
            private List<T> poolList = List<T>.Create_WillNeverBeGCed( 90, "ButtonPool-poolList", 100 );
            private List<T> inUseList = List<T>.Create_WillNeverBeGCed( 90, "ButtonPool-inUseList", 100 );
            private int currentIndex = -1;

            public ButtonPool( T Original, int MinEntries )
            {
                this.original = Original;

                maxRemainingAllowedToAddBeforeNextClear = MinEntries;
                for ( int i = 0; i < MinEntries; i++ )
                    GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
            }

            private int maxRemainingAllowedToAddBeforeNextClear = 0;
            public void Clear( int MaxToAddAtASingleTime )
            {
                maxRemainingAllowedToAddBeforeNextClear = MaxToAddAtASingleTime;
                for ( int i = 0; i < inUseList.Count; i++ )
                    inUseList[i].Clear();
                currentIndex = 0;// -1; SKIP THE FIRST ONE!  It should always be unused, because... reasons?  Its sizing goes nuts, at any rate
                inUseList.Clear();
            }

            public int GetRemainingAllowedToAddBeforeNextClear()
            {
                return maxRemainingAllowedToAddBeforeNextClear;
            }

            public List<T> GetInUseList()
            {
                return this.inUseList;
            }

            public int GetInUseCount()
            {
                return this.inUseList.Count;
            }

            public T GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds()
            {
                T item;

                currentIndex++;
                if ( currentIndex < poolList.Count )
                {
                    //had enough items in pool to just grab one
                    item = poolList[currentIndex];
                    inUseList.Add( item );
                    return item;
                }

                maxRemainingAllowedToAddBeforeNextClear--;
                if ( maxRemainingAllowedToAddBeforeNextClear <= 0 )
                    return null;

                //did NOT have enough items in pool, so creating a duplicate now
                item = (T)this.original.DuplicateSelf();
                this.poolList.Add( item );
                this.inUseList.Add( item );
                return item;
            }

            public void Sort()
            {
                if ( this.inUseList.Count <= 1 )
                    return;

                this.inUseList.Sort( static delegate ( T Left, T Right )
                {
                    return Left.CompareSelfWithOther( Right );
                } );
            }

            #region ApplyItemsInRows
            public void ApplyItemsInRows( float currentX, ref float currentY, float ROW_ADVANCE, float WIDTH, float HEIGHT )
            {
                T item;
                RectTransform rTran;
                for ( int i = 0; i < inUseList.Count; i++ )
                {
                    item = inUseList[i];
                    if ( item.ExtraSpaceBeforeInAutoSizing != 0 )
                    {
                        currentY -= item.ExtraSpaceBeforeInAutoSizing;
                        item.ExtraSpaceBeforeInAutoSizing = 0;
                    }

                    rTran = item.Element.RelevantRect;
                    rTran.anchoredPosition = new Vector2( currentX, currentY );
                    rTran.localScale = Mat.V3_One;
                    rTran.localRotation = Mat.Quater_Ident;
                    if ( item.AlternativeHeightToUseInAutoSizing > 0 )
                    {
                        rTran.sizeDelta = new Vector2( WIDTH, item.AlternativeHeightToUseInAutoSizing );
                        currentY -= (ROW_ADVANCE - (HEIGHT - item.AlternativeHeightToUseInAutoSizing));
                        item.AlternativeHeightToUseInAutoSizing = 0;
                    }
                    else
                    {
                        rTran.sizeDelta = new Vector2( WIDTH, HEIGHT );
                        currentY -= ROW_ADVANCE;
                    }
                    if ( item.ExtraSpaceAfterInAutoSizing != 0 )
                    {
                        currentY -= item.ExtraSpaceAfterInAutoSizing;
                        item.ExtraSpaceAfterInAutoSizing = 0;
                    }
                }
            }
            #endregion

            #region ApplyItemsInGrid
            public void ApplyItemsInGrid( ref float currentY, int COLUMN_COUNT, float COLUMN_ADVANCE, float ROW_ADVANCE, float WIDTH, float HEIGHT )
            {
                T item;
                int currentColumn = 0;
                RectTransform rTran;
                for ( int i = 0; i < inUseList.Count; i++ )
                {
                    item = inUseList[i];

                    if ( currentColumn >= COLUMN_COUNT )
                    {
                        currentColumn = 0;
                        currentY -= ROW_ADVANCE;
                    }

                    rTran = item.Element.RelevantRect;
                    rTran.anchoredPosition = new Vector2( currentColumn * COLUMN_ADVANCE, currentY );
                    rTran.localScale = Mat.V3_One;
                    rTran.localRotation = Mat.Quater_Ident;
                    rTran.sizeDelta = new Vector2( WIDTH, HEIGHT );
                    currentColumn++;
                }

                currentY -= ROW_ADVANCE;
            }
            #endregion
        }
        #endregion
    }

    public abstract class ColorSettableButtonBase : ButtonAbstractBase
    {
        private TMPro.TextMeshProUGUI Text;
        private UnityEngine.UI.Image Image;

        private Color lastColor = new Color( 0, 1, 0, 0 );

        public void SetColor( Color c )
        {
            if ( c == this.lastColor )
                return;
            this.InitIfNeeded();
            if ( this.Text != null )
            {
                this.Text.color = c;
                if ( this.Image != null )
                {
                    this.Image.color = c;
                    this.lastColor = c;
                }
            }
        }

        private void InitIfNeeded()
        {
            if ( Text != null )
                return;
            if ( this.Element == null )
                return;
            ArcenUI_Button button = this.Element as ArcenUI_Button;

            Text = button.ReferenceText;
            Image = button.GetComponentInChildren<UnityEngine.UI.Image>();
        }
    }

    public abstract class HighlightSettableButtonBase : ButtonAbstractBase
    {
        private bool wasPreviouslyHighlighting = false;
        private ArcenUI_Button button = null;

        public void SetHighlightOn( bool On )
        {
            if ( On == this.wasPreviouslyHighlighting )
                return;
            this.wasPreviouslyHighlighting = On;

            this.InitIfNeeded();
            if ( this.button != null )
                this.button.ShouldDoMouseoverVisualsRightNowAsWayOfHighlightingSelf = On;
        }

        private void InitIfNeeded()
        {
            if ( button != null )
                return;
            if ( this.Element == null )
                return;
            this.button = this.Element as ArcenUI_Button;
        }
    }

    #region PoolableGUIGroup
    public abstract class PoolableGUIGroup
    {
        public int IndexInPool = 0;

        public abstract void Clear();
        public abstract void Update();
        public abstract PoolableGUIGroup DuplicateSelf();
        public abstract void PostInit();

        public virtual int CompareSelfWithOther( PoolableGUIGroup Other )
        {
            return PREFER_NEITHER;
        }

        public const int PREFER_LEFT = -1;
        public const int PREFER_RIGHT = 1;
        public const int PREFER_NEITHER = 0;

        public class GroupPool<T> where T : PoolableGUIGroup
        {
            private T original;
            private List<T> poolList = List<T>.Create_WillNeverBeGCed( 90, "GroupPool-poolList" );
            private List<T> inUseList = List<T>.Create_WillNeverBeGCed( 90, "GroupPool-inUseList" );
            private int currentIndex = -1;

            public GroupPool( T Original, int MinEntries )
            {
                this.original = Original;
                this.original.IndexInPool = -1;

                maxRemainingAllowedToAddBeforeNextClear = MinEntries;
                for ( int i = 0; i < MinEntries; i++ )
                    GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
            }

            private int maxRemainingAllowedToAddBeforeNextClear = 0;
            public void Clear( int MaxToAddAtASingleTime )
            {
                maxRemainingAllowedToAddBeforeNextClear = MaxToAddAtASingleTime;
                for ( int i = 0; i < inUseList.Count; i++ )
                    inUseList[i].Clear();
                currentIndex = 0;// -1; SKIP THE FIRST ONE!  It should always be unused, because... reasons?  Its sizing goes nuts, at any rate
                inUseList.Clear();
            }

            public int GetRemainingAllowedToAddBeforeNextClear()
            {
                return maxRemainingAllowedToAddBeforeNextClear;
            }

            public List<T> GetInUseList()
            {
                return this.inUseList;
            }

            public int GetInUseCount()
            {
                return this.inUseList.Count;
            }

            public T GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds()
            {
                T item;

                currentIndex++;
                if ( currentIndex < poolList.Count )
                {
                    //had enough items in pool to just grab one
                    item = poolList[currentIndex];
                    inUseList.Add( item );
                    return item;
                }

                maxRemainingAllowedToAddBeforeNextClear--;
                if ( maxRemainingAllowedToAddBeforeNextClear <= 0 )
                    return null;

                //did NOT have enough items in pool, so creating a duplicate now
                item = (T)this.original.DuplicateSelf();
                item.PostInit();
                item.IndexInPool = this.poolList.Count;
                this.poolList.Add( item );
                this.inUseList.Add( item );
                return item;
            }

            public void Sort()
            {
                if ( this.inUseList.Count <= 1 )
                    return;

                this.inUseList.Sort( static delegate ( T Left, T Right )
                {
                    return Left.CompareSelfWithOther( Right );
                } );
            }
        }
    }
    #endregion

    public abstract class ImageButtonAbstractBase : IArcenUI_ImageButton_Controller
    {
        public abstract void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts );

        public virtual MouseHandlingResult HandleClick( MouseHandlingInput input ) { return MouseHandlingResult.None; }

        public virtual void HandleMouseover() { }
        public virtual void OnMainThreadUpdate() { }

        public ArcenUI_Element Element;

        public float AlternativeHeightToUseInAutoSizing = 0;
        public float ExtraSpaceBeforeInAutoSizing = 0;
        public float ExtraSpaceAfterInAutoSizing = 0;

        public virtual void OnUpdate() { }
        public virtual bool GetShouldBeHidden() { return false; }
        public virtual bool GetShouldRunUpdatesEvenWhenHidden() { return false; }
        public virtual void SetElement( ArcenUI_Element Element ) { this.Element = Element; }
        public virtual void HandleSubImageMouseover( ArcenUI_Image.SubImage SubImage ) { }
        public virtual void HandleSubTextMouseover( SubText SubText ) { }

        #region ImageButtonPool
        public virtual void Clear() { }
        public virtual ImageButtonAbstractBase DuplicateSelf()
        {
            ArcenUI_ImageButton button = (ArcenUI_ImageButton)this.Element.DuplicateSelf();
            ImageButtonAbstractBase controller = (ImageButtonAbstractBase)button.Controller;
            return controller;
        }
        public virtual int CompareSelfWithOther( ImageButtonAbstractBase Other )
        {
            return PREFER_NEITHER;
        }

        public const int PREFER_LEFT = -1;
        public const int PREFER_RIGHT = 1;
        public const int PREFER_NEITHER = 0;

        public class ImageButtonPool<T> where T : ImageButtonAbstractBase
        {
            private T original;
            private List<T> poolList = List<T>.Create_WillNeverBeGCed( 90, "ImageButtonPool-poolList" );
            private List<T> inUseList = List<T>.Create_WillNeverBeGCed( 90, "ImageButtonPool-inUseList" );
            private int currentIndex = -1;

            public ImageButtonPool( T Original, int MinEntries )
            {
                this.original = Original;

                maxRemainingAllowedToAddBeforeNextClear = MinEntries;
                for ( int i = 0; i < MinEntries; i++ )
                    GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
            }

            private int maxRemainingAllowedToAddBeforeNextClear = 0;
            public void Clear( int MaxToAddAtASingleTime )
            {
                maxRemainingAllowedToAddBeforeNextClear = MaxToAddAtASingleTime;
                for ( int i = 0; i < inUseList.Count; i++ )
                    inUseList[i].Clear();
                currentIndex = 0;// -1; SKIP THE FIRST ONE!  It should always be unused, because... reasons?  Its sizing goes nuts, at any rate
                inUseList.Clear();
            }

            public int GetRemainingAllowedToAddBeforeNextClear()
            {
                return maxRemainingAllowedToAddBeforeNextClear;
            }

            public List<T> GetInUseList()
            {
                return this.inUseList;
            }

            public int GetInUseCount()
            {
                return this.inUseList.Count;
            }

            public T GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds()
            {
                T item;

                currentIndex++;
                if ( currentIndex < poolList.Count )
                {
                    //had enough items in pool to just grab one
                    item = poolList[currentIndex];
                    inUseList.Add( item );
                    return item;
                }

                maxRemainingAllowedToAddBeforeNextClear--;
                if ( maxRemainingAllowedToAddBeforeNextClear <= 0 )
                    return null;

                //did NOT have enough items in pool, so creating a duplicate now
                item = (T)this.original.DuplicateSelf();
                this.poolList.Add( item );
                this.inUseList.Add( item );
                return item;
            }

            public void Sort()
            {
                if ( this.inUseList.Count <= 1 )
                    return;

                this.inUseList.Sort( static delegate ( T Left, T Right )
                {
                    return Left.CompareSelfWithOther( Right );
                } );
            }

            #region ApplyItemsInRows
            public void ApplyItemsInRows( float currentX, ref float currentY, float ROW_ADVANCE, float WIDTH, float HEIGHT )
            {
                T item;
                RectTransform rTran;
                for ( int i = 0; i < inUseList.Count; i++ )
                {
                    item = inUseList[i];
                    if ( item.ExtraSpaceBeforeInAutoSizing != 0 )
                    {
                        currentY -= item.ExtraSpaceBeforeInAutoSizing;
                        item.ExtraSpaceBeforeInAutoSizing = 0;
                    }

                    rTran = item.Element.RelevantRect;
                    rTran.anchoredPosition = new Vector2( currentX, currentY );
                    rTran.localScale = Mat.V3_One;
                    rTran.localRotation = Mat.Quater_Ident;
                    if ( item.AlternativeHeightToUseInAutoSizing > 0 )
                    {
                        rTran.sizeDelta = new Vector2( WIDTH, item.AlternativeHeightToUseInAutoSizing );
                        currentY -= ( ROW_ADVANCE - ( HEIGHT - item.AlternativeHeightToUseInAutoSizing ) );
                        item.AlternativeHeightToUseInAutoSizing = 0;
                    }
                    else
                    {
                        rTran.sizeDelta = new Vector2( WIDTH, HEIGHT );
                        currentY -= ROW_ADVANCE;
                    }
                    if ( item.ExtraSpaceAfterInAutoSizing != 0 )
                    {
                        currentY -= item.ExtraSpaceAfterInAutoSizing;
                        item.ExtraSpaceAfterInAutoSizing = 0;
                    }
                }
            }
            #endregion

            #region ApplyItemsInGrid
            public void ApplyItemsInGrid( ref float currentY, int COLUMN_COUNT, float COLUMN_ADVANCE, float ROW_ADVANCE, float WIDTH, float HEIGHT, float AddedXOffset, float AddedYOffset )
            {
                T item;
                int currentColumn = 0;
                RectTransform rTran;
                for ( int i = 0; i < inUseList.Count; i++ )
                {
                    item = inUseList[i];

                    if ( currentColumn >= COLUMN_COUNT )
                    {
                        currentColumn = 0;
                        currentY -= ROW_ADVANCE;
                    }

                    rTran = item.Element.RelevantRect;
                    rTran.anchoredPosition = new Vector2( (currentColumn * COLUMN_ADVANCE) + AddedXOffset, currentY + AddedYOffset );
                    rTran.localScale = Mat.V3_One;
                    rTran.localRotation = Mat.Quater_Ident;
                    rTran.sizeDelta = new Vector2( WIDTH, HEIGHT );
                    currentColumn++;
                }

                currentY -= ROW_ADVANCE;
            }
            #endregion
        }
        #endregion
    }

    public abstract class SliderAbstractBase : ElementAbstractBase, IArcenUI_Slider_Controller
    {
        public virtual void DoAnyCustomSliderStuffFromVolatile( ArcenUI_Slider Slider ) { }
        public virtual MouseHandlingResult HandleClick( MouseHandlingInput input ) { return MouseHandlingResult.None; }
        public virtual void OnChange( float NewValue ) { }
    }

    public abstract class ButtonSetAbstractBase : ElementAbstractBase, IArcenUI_ButtonSet_Controller
    {

    }

    public abstract class DropdownAbstractBase : ElementAbstractBase, IArcenUI_Dropdown_Controller
    {
        public virtual void GetMainTextToShowPrefixFromVolatile( ArcenDoubleCharacterBuffer Buffer ) { }

        public virtual void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenUI_Dropdown_Option Item ) { }

        public virtual void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType ) { }
    }

    public abstract class InputAbstractBase : ElementAbstractBase, IArcenUI_Input_Controller
    {
        public void SetText( string NewText )
        {
            ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;
            elementAsType.SetText( NewText );
        }

        public string GetText()
        {
            ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;
            return elementAsType.ReferenceInputField.text;
        }

        public bool GetIsCurrentlyBeingEdited()
        {
            ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;
            return elementAsType.ReferenceInputField.isFocused;
        }

        public virtual void HandleChangeInValue( string NewValue ) { }

        public virtual char ValidateInput( string input, int charIndex, char addedChar ) { return addedChar; }
        public virtual void OnEndEdit() { }
        public abstract InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action );
    }

    public abstract class ImageSetAbstractBase : IArcenUI_ImageSet_Controller
    {
        public virtual void OnUpdate() { }
        public virtual bool GetShouldBeHidden() { return false; }
        public virtual bool GetShouldRunUpdatesEvenWhenHidden() { return false; }
        public virtual void SetElement( ArcenUI_Element Element ) { }
        public virtual void HandleMouseover() { }
        public virtual void OnMainThreadUpdate() { }
    }

    public abstract class ImageButtonSetAbstractBase : IArcenUI_ImageButtonSet_Controller
    {
        public virtual void OnUpdate() { }
        public virtual bool GetShouldBeHidden() { return false; }
        public virtual bool GetShouldRunUpdatesEvenWhenHidden() { return false; }
        public virtual void SetElement( ArcenUI_Element Element ) { }
        public virtual void HandleMouseover() { }
        public virtual void OnMainThreadUpdate() { }
    }

    public abstract class ImageAbstractBase : IArcenUI_Image_Controller
    {
        public virtual void OnUpdate() { }
        public virtual bool GetShouldBeHidden() { return false; }
        public virtual bool GetShouldRunUpdatesEvenWhenHidden() { return false; }
        public virtual void SetElement( ArcenUI_Element Element ) { }
        public virtual void HandleClick() { }
        public virtual void HandleMouseover() { }
        public virtual void OnMainThreadUpdate() { }
        public virtual void UpdateImagesFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages ) { }
    }

    public abstract class WindowTogglingButtonController : ButtonAbstractBase
    {
        private ArcenUI_Window Window;
        private readonly string TextWhenClosed = string.Empty;
        private readonly string TextWhenOpen = string.Empty;

        public WindowTogglingButtonController( string TextWhenClosed, string TextWhenOpen )
        {
            this.TextWhenClosed = TextWhenClosed;
            this.TextWhenOpen = TextWhenOpen;
        }

        public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
        {
            bool toggledWindowIsShown = this.GetRelatedController().IsOpen;
            if ( this.GetShouldSuppressOpenIndicatorEvenIfToggledWindowIsShown() )
                toggledWindowIsShown = false;
            base.GetTextToShowFromVolatile( Buffer );
            Buffer.Add( toggledWindowIsShown ? this.TextWhenOpen : this.TextWhenClosed );
        }

        public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
        {
            ToggleableWindowController controller = this.GetRelatedController();
            if ( controller.IsOpen )
                controller.Close();
            else
                controller.Open();
            return MouseHandlingResult.None;
        }

        public override void HandleMouseover() { }
        public override void OnUpdate()
        {
            if ( this.Window == null )
                this.Window = this.Element.Window;
        }

        public abstract ToggleableWindowController GetRelatedController();

        public virtual bool GetShouldSuppressOpenIndicatorEvenIfToggledWindowIsShown() { return false; }
    }

    public abstract class StackWindowOpeningButtonController : ButtonAbstractBase
    {
        public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
        {
            base.GetTextToShowFromVolatile( Buffer );
            if ( this.RelatedController != null )
                Buffer.Add( this.RelatedController.GetBriefName() );
        }

        public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
        {
            if ( this.RelatedController != null )
                this.RelatedController.Open();
            return MouseHandlingResult.None;
        }

        protected abstract StackMenuWindowController RelatedController { get; }

        public override bool GetShouldBeGrayedOut() { return !this.RelatedController.GetIsAllowedInCurrentContext(); }
    }

    public class IOptionIntBasedDropdownOption : IArcenUI_Dropdown_Option
    {
        public readonly IOption Option;
        public readonly int OptionValue;

        public IOptionIntBasedDropdownOption( IOption option, int Value )
        {
            this.Option = option;
            this.OptionValue = Value;
        }

        public virtual string GetOptionNameFromVolatile()
        {
            return this.Option.GetDisplayName();
        }

        public Sprite GetOptionSprite()
        {
            return null;
        }

        public object GetItem()
        {
            return OptionValue;
        }

        public static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "StringTrioBasedDropdownOption-tooltipBuffer" );

        public bool FillMouseoverPartsForItem( bool AddDoubleNewline)
        {
            if ( this.OptionValue == -1 ) 
                return false;

            if ( AddDoubleNewline )
                tooltipBuffer.Add( "\n\n" );

            tooltipBuffer.Add( "<b>" ).Add( this.Option.GetDisplayName() ).Add( "</b>\n" ).Add( this.Option.AddDescription );
            tooltipBuffer.AddDlcMod(this.Option, "This option was added by" );

            return true;
        }

        public void FinishAndDisplayTooltipFromBuffer( IArcenUIElementForSizing Element )
        {
            Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
        }
    }

    public class IntBasedDropdownOption : IArcenUI_Dropdown_Option
    {
        public readonly int OptionValue;
        private readonly string OptionName;

        public IntBasedDropdownOption( int Value, string Name )
        {
            this.OptionValue = Value;
            this.OptionName = Name;
        }

        public virtual string GetOptionNameFromVolatile()
        {
            return OptionName;
        }

        public Sprite GetOptionSprite()
        {
            return null;
        }

        public object GetItem()
        {
            return OptionValue;
        }
    }

    public class StringTrioBasedDropdownOption : IArcenUI_Dropdown_Option
    {
        public IOption OptionValue;

        public StringTrioBasedDropdownOption( IOption Value )
        {
            this.OptionValue = Value;
        }

        private static ArcenDoubleCharacterBuffer displayNameBuffer = new ArcenDoubleCharacterBuffer( "StringTrioBasedDropdownOption-displayNameBuffer" );
        public virtual string GetOptionNameFromVolatile()
        {
            displayNameBuffer.Add( this.OptionValue.GetShortDisplayName() );

            displayNameBuffer.AddDlcMod( this.OptionValue );

            return displayNameBuffer.GetStringAndResetForNextUpdate();
        }

        public Sprite GetOptionSprite()
        {
            return null;
        }

        public static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "StringTrioBasedDropdownOption-tooltipBuffer" );

        public bool FillMouseoverPartsForItem( bool AddDoubleNewline)
        {
            if ( this.OptionValue == null ) 
                return false;

            if ( AddDoubleNewline )
                tooltipBuffer.Add( "\n\n" );

            tooltipBuffer.Add( "<b>" ).Add( this.OptionValue.GetDisplayName() ).Add( "</b>\n" ).Add( this.OptionValue.AddDescription );
            
            tooltipBuffer.AddDlcMod(OptionValue, "This option was added by" );

            return true;
        }

        public void FinishAndDisplayTooltipFromBuffer( IArcenUIElementForSizing Element )
        {
            Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
        }

        public object GetItem()
        {
            return this.OptionValue;
        }
    }

    public class RowBasedDropdownOption<T> : IArcenUI_Dropdown_Option
        where T : ArcenDynamicTableRow
    {
        public T Row;

        public RowBasedDropdownOption( T Row )
        {
            this.Row = Row;
        }

        public virtual object GetItem()
        {
            return this.Row;
        }

        private static ArcenDoubleCharacterBuffer displayNameBuffer = new ArcenDoubleCharacterBuffer( "RowBasedDropdownOption-displayNameBuffer" );
        public virtual string GetOptionNameFromVolatile()
        {
            if ( this.Row == null )
                displayNameBuffer.Add( "None" );
            else
            {
                displayNameBuffer.Add( this.Row.GetDisplayName() );
                displayNameBuffer.AddDlcMod(this.Row );
            }

            return displayNameBuffer.GetStringAndResetForNextUpdate();
        }

        public virtual Sprite GetOptionSprite()
        {
            return null;
        }
    }
}
