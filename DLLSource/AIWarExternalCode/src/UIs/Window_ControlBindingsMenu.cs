using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_ControlBindingsMenu : ToggleableWindowController, IInputActionHandler
    {
        private readonly float rowHeight = 24;
        private readonly float rowBuffer = 1.5f;
        string CurrentCategory;
        public static Window_ControlBindingsMenu Instance;
        public Window_ControlBindingsMenu()
        {
            Instance = this;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
            this.PreventsNormalInputHandlers = true;
            this.SuppressesUIScaling = true;
            this.CurrentCategory = "Critical";
        }

        public override void OnOpen()
        {
            InputActionTypeDataTable.Instance.CopyCurrentValuesToTemp();
        }

        private ArcenCachedExternalTypeDirect type_bModifier1 = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bModifier1 ) );
        private ArcenCachedExternalTypeDirect type_tPlus = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tPlus ) );
        private ArcenCachedExternalTypeDirect type_bKeyCode = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bKeyCode ) );
        private ArcenCachedExternalTypeDirect type_tSettingName = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tSettingName ) );

        public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            if ( bMainContentParent.ParentT == null )
                return;
            this.Window.SetOverridingTransformToWhichToAddChildren( bMainContentParent.ParentT );

            //arguments to createunityrectagle: X, Y, width, height
            
            DictionaryOfLists<string, InputActionTypeData> controlsPerCategory = InputActionTypeDataTable.ActionsByCategory;

            // Add header buttons (close/save/reset)
            float runningY = 3; 

            //Here we check for which Category is active, then display the slider if there are "enough" settings to require a slider to display
            List<InputActionTypeData> listToShow = controlsPerCategory[this.CurrentCategory];
                        
            for ( int i = 0; i < listToShow.Count; i++ )
            {
                InputActionTypeData inputAction = listToShow[i];

                Rect mod1Bounds = ArcenRectangle.CreateUnityRect( 5, runningY, 100, this.rowHeight );
                AddButton( Set, type_bModifier1, inputAction.InternalName, inputAction.RowIndexNonSim, -1, mod1Bounds, -1f );//okay to use here, as it will be consistent per run

                Rect textPlusBounds = ArcenRectangle.CreateUnityRect( mod1Bounds.xMax, runningY, 25, this.rowHeight );
                AddText( Set, type_tPlus, inputAction.InternalName, inputAction.RowIndexNonSim, -1, textPlusBounds, 14f );

                Rect keyCodeBounds = ArcenRectangle.CreateUnityRect( textPlusBounds.xMax, runningY, 100, this.rowHeight );
                AddButton( Set, type_bKeyCode, inputAction.InternalName, inputAction.RowIndexNonSim, -1, keyCodeBounds, -1f );

                Rect nameBounds = ArcenRectangle.CreateUnityRect( keyCodeBounds.xMax + 10, runningY, 400, this.rowHeight );
                AddText( Set, type_tSettingName, inputAction.InternalName, inputAction.RowIndexNonSim, -1, nameBounds, 12f );

                runningY += this.rowHeight + rowBuffer;
            }
            
            bMainContentParent.ParentRT.UI_SetHeight( runningY );
        }

        public static InputActionTypeData GetSettingForController( ElementAbstractBase controller )
        {
            int tableIndex = controller.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            if ( tableIndex >= InputActionTypeDataTable.Instance.Rows.Count )
                return null;
            return InputActionTypeDataTable.Instance.Rows[tableIndex];
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

        public class tPlus : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "  +" );
            }
        }

        private static ButtonAbstractBase.ButtonPool<bCategory> btnCategoryPool;

        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_ControlBindingsMenu.Instance != null )
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
                float currentY = -5; //the position of the first entry

                if ( !hasGlobalInitialized )
                    return;

                btnCategoryPool.Clear( 10 );

                DictionaryOfLists<string, InputActionTypeData> controlsPerCategory = InputActionTypeDataTable.ActionsByCategory;
                foreach ( KeyValuePair<string, List<InputActionTypeData>> pair in controlsPerCategory )
                {
                    if ( pair.Key == "Unused" || pair.Key == "Hidden" )
                        continue;
                    bCategory item = btnCategoryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; //time slicing, too many added right now
                    item.Assign( pair.Key );
                }

                #region Positioning Logic 1
                btnCategoryPool.ApplyItemsInRows( 10, ref currentY, 36, 218, 30 );
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

        #region bCategory
        public class bCategory : ButtonAbstractBase
        {
            public static bCategory Original;
            public bCategory() { if ( Original == null ) Original = this; }

            private string categoryName = string.Empty;

            public void Assign( string categoryName )
            {
                this.categoryName = categoryName;
            }

            public override bool GetShouldBeHidden()
            {
                return this.categoryName == null || this.categoryName.Length <= 0;
            }

            public override void Clear()
            {
                this.categoryName = string.Empty;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( this.categoryName == null )
                    return;

                if ( Instance.CurrentCategory == this.categoryName )
                    buffer.Add( "<color=#6fafff>" );

                buffer.Add( this.categoryName );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.categoryName == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                Instance.CurrentCategory = this.categoryName;
                return MouseHandlingResult.None;
            }
            
            public override void HandleMouseover()
            {
                if ( this.categoryName == null )
                    return;
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "查看分类为" + this.categoryName + "的按键绑定" );
            }
        }
        #endregion

        public class tSettingName : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                InputActionTypeData setting = GetSettingForController( this );
                if ( setting == null ) return;

                buffer.Add( setting.DisplayName );
            }

            public override void HandleMouseover()
            {
                InputActionTypeData setting = GetSettingForController( this );
                if ( setting == null ) return;
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, setting.GetTEMPHumanReadableKeyComboForAction() + ":  " + setting.Description );
            }
        }

        public class bModifier1 : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                InputActionTypeData setting = GetSettingForController( this );
                if ( setting == null ) return;

                if ( this.amIWaitingOnKeypress && ArcenTime.TimeSinceStartF > this.doNothingUntil )
                {
                    if ( Input.anyKeyDown )
                    {
                        for ( int i = 0; i < EnumInput.Lookup.Length; i++ )
                        {
                            if ( Input.GetKeyDown( EnumInput.Lookup[i] ) )
                            {
                                this.amIWaitingOnKeypress = false;
                                InputManager.IgnoreAllInput = false;
                                setting.TempModifier1KeyCode = (ArcenInputCode)i;
                                this.doNothingUntil = ArcenTime.TimeSinceStartF + 0.5f;
                                break;
                            }
                        }
                    }
                }

                if ( this.amIWaitingOnKeypress )
                    buffer.Add( "???" );
                else
                {
                    if ( setting.TempKeyCode == ArcenInputCode.None && setting.TempModifier1KeyCode != ArcenInputCode.None )
                        buffer.Add( "<color=#ff6666>请先设置其他键！" );
                    else
                        buffer.Add( setting.TempModifier1KeyCode != ArcenInputCode.None ? InputActionTypeDataTable.GetHumanReadableStringForKeyCode( setting.TempModifier1KeyCode ) : "<color=#666666>无" );
                }
            }

            private bool amIWaitingOnKeypress = false;
            private float doNothingUntil;

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                InputActionTypeData setting = GetSettingForController( this );
                if ( setting == null ) return MouseHandlingResult.None;
                if ( ArcenTime.TimeSinceStartF < this.doNothingUntil )
                    return MouseHandlingResult.DoNotPlayClickSound;

                if ( input.RightButtonClicked )
                {
                    setting.TempModifier1KeyCode = ArcenInputCode.None;
                }
                else
                {
                    InputManager.IgnoreAllInput = true;
                    this.amIWaitingOnKeypress = true;
                    this.doNothingUntil = ArcenTime.TimeSinceStartF + 0.25f;
                }
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                InputActionTypeData setting = GetSettingForController( this );
                if ( setting == null ) return;
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "修饰键 - 可选。如果设置了此键，则需要按住此键并按下下一个键来触发操作。\n左键点击后按任意键设置。\n右键点击清除。" );
            }
        }

        public class bKeyCode : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                InputActionTypeData setting = GetSettingForController( this );
                if ( setting == null ) return;

                if ( this.amIWaitingOnKeypress && ArcenTime.TimeSinceStartF > this.doNothingUntil )
                {
                    if ( Input.anyKeyDown )
                    {
                        for( int i = 0; i < EnumInput.Lookup.Length; i++ )
                        {
                            if ( Input.GetKeyDown( EnumInput.Lookup[i] ) )
                            {
                                this.amIWaitingOnKeypress = false;
                                InputManager.IgnoreAllInput = false;
                                setting.TempKeyCode = (ArcenInputCode)i;
                                this.doNothingUntil = ArcenTime.TimeSinceStartF + 0.5f;
                                break;
                            }
                        }
                    }
                }

                if ( this.amIWaitingOnKeypress )
                    buffer.Add( "???" );
                else
                    buffer.Add( setting.TempKeyCode != ArcenInputCode.None ? InputActionTypeDataTable.GetHumanReadableStringForKeyCode( setting.TempKeyCode ) : "<color=#cc4444>未绑定" );
            }

            private bool amIWaitingOnKeypress = false;
            private float doNothingUntil;

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                InputActionTypeData setting = GetSettingForController( this );
                if ( setting == null ) return MouseHandlingResult.None;
                if ( ArcenTime.TimeSinceStartF < this.doNothingUntil )
                    return MouseHandlingResult.DoNotPlayClickSound;

                if ( input.RightButtonClicked )
                {
                    setting.TempKeyCode = ArcenInputCode.None;
                }
                else
                {
                    InputManager.IgnoreAllInput = true;
                    this.amIWaitingOnKeypress = true;
                    this.doNothingUntil = ArcenTime.TimeSinceStartF + 0.25f;
                }
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                InputActionTypeData setting = GetSettingForController( this );
                if ( setting == null ) return;
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "主键 - 如果要使用此操作则必填。\n对于不打算使用的操作，可以跳过。\n多个操作可以分配相同的按键，通常也是如此。\n左键点击后按任意键设置。\n右键点击清除。" );
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
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                InputActionTypeDataTable.Instance.CopyTempValuesToCurrent();
                ArcenInput.SaveInputMappingsToDisk();
                Instance.Close();
                return MouseHandlingResult.None;
            }
        }

        public class bSetDefaults : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                int rowCount = InputActionTypeDataTable.Instance.Rows.Count;
                for ( int i = 0; i < rowCount; i++ )
                {
                    InputActionTypeData inputData = InputActionTypeDataTable.Instance.Rows[i];
                    inputData.TempModifier1KeyCode = inputData.DefaultModifier1KeyCode;
                    inputData.TempModifier2KeyCode = inputData.DefaultModifier2KeyCode;
                    inputData.TempModifier3KeyCode = inputData.DefaultModifier3KeyCode;
                    inputData.TempKeyCode = inputData.DefaultKeyCode;
                }
                return MouseHandlingResult.None;
            }
        }

        public override void Close()
        {
            InputManager.IgnoreAllInput = false;
            InputActionTypeDataTable.Instance.CopyCurrentValuesToTemp(); // shouldn't matter, but tidies things up
            base.Close();
        }

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
