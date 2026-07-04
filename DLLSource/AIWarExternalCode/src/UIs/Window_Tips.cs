using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_Tips : ToggleableWindowController, IInputActionHandler
    {
        protected float topBuffer = 3;
        protected float leftBuffer = 2;
        protected float rowHeight = 24;
        protected float rowBuffer = 1.5f;

        #region CalculateBoundsSingle
        protected void CalculateBoundsSingle( out Rect soleBounds, ref float runningY, float SoleWidth )
        {
            soleBounds = ArcenRectangle.CreateUnityRect( leftBuffer, runningY, SoleWidth, rowHeight );

            runningY += rowHeight + rowBuffer;
        }
        #endregion

        TipCategory CurrentCategory;
        public static Window_Tips Instance;
        public Window_Tips()
        {
            Instance = this;
            this.topBuffer = 3;
            this.leftBuffer = 5;
            this.rowHeight = 24;
            this.rowBuffer = 1.5f;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
            this.PreventsNormalInputHandlers = true;
            this.SuppressesUIScaling = true;
            this.CurrentCategory = null;
        }

        public override void OnOpen()
        {
        }

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "如何游玩：提示与策略" );
            }
        }
        #endregion

        private ArcenCachedExternalTypeDirect type_bTipDisplay = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bTipDisplay ) );

        public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            if ( bMainContentParent.ParentT == null )
                return;
            this.Window.SetOverridingTransformToWhichToAddChildren( bMainContentParent.ParentT );
                        
            float runningY = topBuffer;

            Rect onlyBounds;
            
            if ( TipCategoryTable.Instance.SortedCategories.Count <= 0 )
                return;

            if ( this.CurrentCategory == null )
                this.CurrentCategory = TipCategoryTable.Instance.SortedCategories[0];

            List<Tip> listToShow = this.CurrentCategory.Tips;

            for ( int i = 0; i < listToShow.Count; i++ )
            {
                Tip tip = listToShow[i];

                this.CalculateBoundsSingle( out onlyBounds, ref runningY, 800 );
                
                AddButton( Set, type_bTipDisplay, tip.InternalName, tip.RowIndexNonSim, -1, onlyBounds, -1f );
            }

            bMainContentParent.ParentRT.UI_SetHeight( runningY );
        }
        
        public static Tip GetTipForController( ElementAbstractBase controller )
        {
            int tableIndex = controller.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            if ( tableIndex >= TipTable.Instance.Rows.Count )
                return null;
            return TipTable.Instance.Rows[tableIndex];
        }

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

        private static ButtonAbstractBase.ButtonPool<bCategory> btnCategoryPool;

        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_Tips.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( bCategory.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnCategoryPool = new ButtonAbstractBase.ButtonPool<bCategory>( bCategory.Original, 10 );
                        }
                    }
                    #endregion
                }

                this.OnUpdateCategories();
            }

            public void OnUpdateCategories()
            {
                float currentY = -5;

                if ( !hasGlobalInitialized )
                    return;

                btnCategoryPool.Clear( 10 );

                List<TipCategory> categories = TipCategoryTable.Instance.SortedCategories;

                for ( int i = 0; i < categories.Count; i++ )
                {
                    TipCategory category = categories[i];
                    bCategory item = btnCategoryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break;
                    item.Assign( category );
                }

                #region Positioning Logic 1
                btnCategoryPool.ApplyItemsInRows( 10, ref currentY, 29, 218, 27 );
                #endregion

                #region Positioning Logic
                RectTransform rTran = (RectTransform)bCategory.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }
        }

        #region bCategory
        public class bCategory : ButtonAbstractBase
        {
            public static bCategory Original;
            public bCategory() { if ( Original == null ) Original = this; }

            private TipCategory tipCat = null;

            public void Assign( TipCategory tipCategory )
            {
                this.tipCat = tipCategory;
            }

            public override bool GetShouldBeHidden()
            {
                return this.tipCat == null;
            }

            public override void Clear()
            {
                this.tipCat = null;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( this.tipCat == null )
                    return;

                if ( Instance.CurrentCategory == this.tipCat )
                    buffer.Add( "<color=#6fafff>" );

                buffer.Add( this.tipCat.DisplayName );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.tipCat == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                Instance.CurrentCategory = this.tipCat;
                return MouseHandlingResult.None;
            }
            
            public override void HandleMouseover()
            {
                if ( this.tipCat == null )
                    return;
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, tipCat.Description );
            }
        }
        #endregion
                
        public class bTipDisplay : ButtonAbstractBase
        {
            private bool hasDonAlignment = false;
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                Tip tip = GetTipForController( this );
                if ( tip == null ) return;

                if ( !hasDonAlignment )
                {
                    hasDonAlignment = true;
                    ( (ArcenUI_Button)this.Element).ReferenceText.alignment = TMPro.TextAlignmentOptions.Left;
                }

                buffer.Add( "  " ).Add( tip.DisplayName );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Tip tip = GetTipForController( this );
                if ( tip == null ) return MouseHandlingResult.None;

                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.TallWide, null, tip.DisplayName, tip.FullText + "\n\n\n\n\n", "确定" );
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
            }
        }
        
        public class bCancel : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }
        }

        public class bSave : ButtonAbstractBase
        {
            public override bool GetShouldBeHidden()
            {
                return true;
            }
        }

        public class bSetDefaults : ButtonAbstractBase
        {
            public override bool GetShouldBeHidden()
            {
                return true;
            }
        }

        public override void Close()
        {
            base.Close();
        }

        //from IInputActionHandler
        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.InternalName )
            {
                case "OpenSystemMenu":
                    if (Engine_Universal.CurrentPopups.Count > 0)
                    {
                        Engine_Universal.CurrentPopups[0].OnNoOrClose?.Invoke();
                        Engine_Universal.CurrentPopups.RemoveAt(0);
                    }
                    else
                        this.Close();
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
            }
        }
    }
}
