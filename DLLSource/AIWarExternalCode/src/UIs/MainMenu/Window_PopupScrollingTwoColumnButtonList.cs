using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_PopupScrollingTwoColumnButtonList : ToggleableWindowController, IInputActionHandler
    {
        public CustomPopupData CurrentSelection = null;
        public ProtectedList<CustomPopupData> AllLeftOptions = null;
        public ProtectedList<CustomPopupData> AllRightOptions = null;
        public Window_PopupScrollingTwoColumnButtonListClickHandler OnOkClick;
        public Window_PopupScrollingTwoColumnButtonListTooltipHandler TooltipHandler;
        public string HeaderString;
        public string LeftHeaderString;
        public string RightHeaderString;
        public string OptionAddedByText;

        private bool hasThisTimeInitialized = false;
        /// <summary>
        /// This window will NOT close when you double click a button
        /// </summary>
        public void Open( string Header,string leftHeader, string RightHeader, CustomPopupData CurrentSelect, ProtectedList<CustomPopupData> LeftOptions, ProtectedList<CustomPopupData> RightOptions, Window_PopupScrollingTwoColumnButtonListClickHandler OnOk,
            Window_PopupScrollingTwoColumnButtonListTooltipHandler tooltipHandler, string OptionAddedBy = "此选项由以下内容添加：" )
        {
            hasThisTimeInitialized = true;
            HeaderString = Header;
            LeftHeaderString = leftHeader;
            RightHeaderString = RightHeader;
            CurrentSelection = CurrentSelect;
            AllLeftOptions = LeftOptions;
            AllRightOptions = RightOptions;
            OnOkClick = OnOk;
            TooltipHandler = tooltipHandler;
            OptionAddedByText = OptionAddedBy;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !this.hasThisTimeInitialized )
                return false;
            return true;
        }

        public static Window_PopupScrollingTwoColumnButtonList Instance;
        public Window_PopupScrollingTwoColumnButtonList()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
        }
        
        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public static ButtonAbstractBase.ButtonPool<bLeft> btnLeftColumnButtonPool;
            public static ButtonAbstractBase.ButtonPool<bRight> btnRightColumnButtonPool;
            public override void OnUpdate()
            {
                if ( Window_PopupScrollingTwoColumnButtonList.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( bLeft.Original != null && bRight.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnLeftColumnButtonPool = new ButtonAbstractBase.ButtonPool<bLeft>( bLeft.Original, 10 );
                            btnRightColumnButtonPool = new ButtonAbstractBase.ButtonPool<bRight>( bRight.Original, 10 );
                        }
                    }
                    #endregion
                }
                this.OnUpdateLeft();
                this.OnUpdateRight();
            
            }

            public void OnUpdateRight()
            {
                float currentY = -10; //the position of the first entry

                btnRightColumnButtonPool.Clear( 10 );

                if ( Instance.AllRightOptions != null )
                {

                    for ( int i = 0; i < Instance.AllRightOptions.Count; i++ )
                    {
                        CustomPopupData RightOption = Instance.AllRightOptions[i];
                        bRight RightItem = btnRightColumnButtonPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( RightItem == null )
                            break; //time slicing, too many added right now
                        RightItem.Assign( RightOption );
                    }
                }

                #region Positioning Logic 1
                btnRightColumnButtonPool.ApplyItemsInRows( 10, ref currentY, 36, 278, 30 );
                #endregion

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)bRight.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }
            public void OnUpdateLeft()
            {
                float currentY = -10; //the position of the first entry

                btnLeftColumnButtonPool.Clear( 10 );

                if ( Instance.AllLeftOptions != null )
                {

                    for ( int i = 0; i < Instance.AllLeftOptions.Count; i++ )
                    {
                        CustomPopupData LeftOption = Instance.AllLeftOptions[i];
                        bLeft LeftItem = btnLeftColumnButtonPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( LeftItem == null )
                            break; //time slicing, too many added right now
                        LeftItem.Assign( LeftOption );
                    }
                }

                #region Positioning Logic 1
                btnLeftColumnButtonPool.ApplyItemsInRows( 10, ref currentY, 36, 278, 30 );
                #endregion

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)bLeft.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }
        }
        #region bLeftColumnButton
        public class bLeft : ButtonAbstractBase
        {
            public static bLeft Original;
            public bLeft() { if ( Original == null ) Original = this; }

            private CustomPopupData ButtonData = null;

            public void Assign( CustomPopupData ButtonData )
            {
                this.ButtonData = ButtonData;
            }

            public override bool GetShouldBeHidden()
            {
                return this.ButtonData == null;
            }

            public override void Clear()
            {
                this.ButtonData = null;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( this.ButtonData == null )
                    return;

                if ( !this.ButtonData.CanBeSelected )
                    buffer.Add( "<color=#e9493d>" );
                else if ( Instance.CurrentSelection == this.ButtonData )
                    buffer.Add( "<color=#6fafff>" );

                buffer.Add( this.ButtonData.DisplayName );
                buffer.EndColor();
                buffer.AddDlcMod(this.ButtonData, Instance.OptionAddedByText );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.ButtonData == null || !this.ButtonData.CanBeSelected )
                    return MouseHandlingResult.PlayClickDeniedSound;
                Instance.CurrentSelection = this.ButtonData;

                if ( input.LeftButtonDoubleClicked )
                {
                       Window_PopupScrollingTwoColumnButtonList.Instance.OnOkClick?.Invoke( Window_PopupScrollingTwoColumnButtonList.Instance.CurrentSelection );                    
                }

                return MouseHandlingResult.None;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_PopupScrollingTwoColumnButtonList-bLeft-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( this.ButtonData == null )
                    return;
                if ( !this.ButtonData.CanBeSelected )
                    tooltipBuffer.StartColor( "e9493d" );
                tooltipBuffer.Add( this.ButtonData.DisplayName );
                if ( !this.ButtonData.CanBeSelected )
                    tooltipBuffer.Add( "（无法选择）</color>" );

                if ( this.ButtonData.Tooltiptext != null && this.ButtonData.Tooltiptext.Length > 0 )
                    tooltipBuffer.Add( "\n" ).Add( this.ButtonData.Tooltiptext );

                if ( Instance.TooltipHandler != null )
                    Instance.TooltipHandler( tooltipBuffer, this.ButtonData );
                if ( !this.ButtonData.CanBeSelected && this.ButtonData.CannotBeSelectedReason.Length > 0 )
                    tooltipBuffer.Add( "\n<color=#e9493d>无法选择的原因：" ).Add( this.ButtonData.CannotBeSelectedReason );

                tooltipBuffer.AddDlcMod(this.ButtonData, Instance.OptionAddedByText );

                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }
        #endregion
        #region bRightColumnButton
        public class bRight : ButtonAbstractBase
        {
            public static bRight Original;
            public bRight() { if ( Original == null ) Original = this; }

            private CustomPopupData ButtonData = null;

            public void Assign( CustomPopupData ButtonData )
            {
                this.ButtonData = ButtonData;
            }

            public override bool GetShouldBeHidden()
            {
                return this.ButtonData == null;
            }

            public override void Clear()
            {
                this.ButtonData = null;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( this.ButtonData == null )
                    return;

                if ( !this.ButtonData.CanBeSelected )
                    buffer.Add( "<color=#e9493d>" );
                else if ( Instance.CurrentSelection == this.ButtonData )
                    buffer.Add( "<color=#6fafff>" );

                buffer.Add( this.ButtonData.DisplayName );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.ButtonData == null || !this.ButtonData.CanBeSelected )
                    return MouseHandlingResult.PlayClickDeniedSound;
                Instance.CurrentSelection = this.ButtonData;

                if ( input.LeftButtonDoubleClicked )
                {
                    Window_PopupScrollingTwoColumnButtonList.Instance.OnOkClick?.Invoke( Window_PopupScrollingTwoColumnButtonList.Instance.CurrentSelection );
                }

                return MouseHandlingResult.None;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_PopupScrollingTwoColumnButtonList-bRight-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( this.ButtonData == null )
                    return;
                if ( !this.ButtonData.CanBeSelected )
                    tooltipBuffer.StartColor( "e9493d" );
                tooltipBuffer.Add( this.ButtonData.DisplayName );
                if ( !this.ButtonData.CanBeSelected )
                    tooltipBuffer.Add( "（无法选择）</color>" );

                if ( this.ButtonData.Tooltiptext != null && this.ButtonData.Tooltiptext.Length > 0 )
                    tooltipBuffer.Add( "\n" ).Add( this.ButtonData.Tooltiptext );

                if ( Instance.TooltipHandler != null )
                    Instance.TooltipHandler( tooltipBuffer, this.ButtonData );
                if ( !this.ButtonData.CanBeSelected && this.ButtonData.CannotBeSelectedReason.Length > 0 )
                    tooltipBuffer.Add( "\n<color=#e9493d>无法选择的原因：" ).Add( this.ButtonData.CannotBeSelectedReason );

                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }
        #endregion
        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( Instance.HeaderString);
            }
        }
        #endregion

        #region tHeaderTextRight
        public class tHeaderTextRight : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( Instance.RightHeaderString );
            }
        }
        #endregion

        #region tHeaderTextLeft
        public class tHeaderTextLeft : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( Instance.LeftHeaderString );
            }
        }
        #endregion

        public class bDone : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( Window_PopupScrollingTwoColumnButtonList.Instance.CurrentSelection == null )
                {
                    Window_PopupScrollingTwoColumnButtonList.Instance.hasThisTimeInitialized = false;
                    return MouseHandlingResult.None;
                }

                Window_PopupScrollingTwoColumnButtonList.Instance.OnOkClick?.Invoke( Window_PopupScrollingTwoColumnButtonList.Instance.CurrentSelection );
                Window_PopupScrollingTwoColumnButtonList.Instance.hasThisTimeInitialized = false;
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "完成" );
            }
        }

        public class bCancel : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_PopupScrollingTwoColumnButtonList.Instance.hasThisTimeInitialized = false;
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "取消" );
            }
        }

        //from IInputActionHandler
        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.InternalName )
            {
                case "OpenSystemMenu":
                    Window_PopupScrollingTwoColumnButtonList.Instance.hasThisTimeInitialized = false;
                    Instance.Close();
                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
            }
        }
    }

    public delegate void Window_PopupScrollingTwoColumnButtonListClickHandler( CustomPopupData SelectedOption );
    public delegate void Window_PopupScrollingTwoColumnButtonListTooltipHandler( ArcenDoubleCharacterBuffer Buffer, CustomPopupData HoveredOption );
}
