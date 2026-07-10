using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_SetupOptionsTab : Window_SetupTabWindowBase
    {
        public static Window_SetupOptionsTab Instance;
        
        public AIWar2GalaxySettingCategory CurrentCategory;
        
        public Window_SetupOptionsTab()
        {
            Instance = this;
            this.topBuffer = 3;
            this.leftBuffer = 5;
            this.rowHeight = 24;
            this.rowBuffer = 1.5f;

            this.PreventsNormalInputHandlers = true;
            //this.SuppressesUIScaling = true;
            this.CurrentCategory = null;
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

        private ArcenCachedExternalTypeDirect type_tAdvancedHiddenOverall = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tAdvancedHiddenOverall ) );
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
        private ArcenCachedExternalTypeDirect type_tAdvancedHiddenSubCat = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tAdvancedHiddenSubCat ) );

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

        #region GetShouldDrawThisFrame_Subclass
        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( Window_SetupTopTabs.Current != LobbyTabType.Options )
                return false;
            return true;
        }
        #endregion

        public override void OnShowAfterNotShowing()
        {
            this.lastWasShowingAdvanced = CalculateShouldShowAdvancedSettings();
            this.RecalculateCategories();
            if ( this.categories.Count > 0 )
                this.CurrentCategory = this.categories[0];
            else
                this.CurrentCategory = null;
            this.isTempShowingAdvanced = false;

            World_AIW2.Instance.DoOnSpecialEvent_OnMainThread_ClientOrHost_ForAllFactionsAndExternalWorldBaseInfo( SpecialEventType.LobbyGalaxyOptionsTabOpened, Engine_AIW2.Instance.MainThreadContext_ClientOrHost );
        }
        public override void OnHideAfterShowing()
        {
            World_AIW2.Instance.DoOnSpecialEvent_OnMainThread_ClientOrHost_ForAllFactionsAndExternalWorldBaseInfo( SpecialEventType.LobbyGalaxyOptionsTabClosed, Engine_AIW2.Instance.MainThreadContext_ClientOrHost );
        }

        public bool isTempShowingAdvanced = false;
        public bool CalculateShouldShowAdvancedSettings()
        {
            //this style only works in the lobby!
            return World_AIW2.Instance.Setup.GetBoolBySetting( AIWar2GalaxySettingTable.Instance.GetRowByName( "ShowAdvancedGalaxyAndFactionOptions" ) ) || isTempShowingAdvanced;
        }

        #region RecalculateCategories
        
        public readonly List<AIWar2GalaxySettingCategory> categories = List<AIWar2GalaxySettingCategory>.Create_WillNeverBeGCed( 60, "Window_SetupOptionsTab-categories" );
        public void RecalculateCategories()
        {
            bool shouldShowAdvancedSettings = CalculateShouldShowAdvancedSettings();

            this.categories.Clear();
            foreach ( AIWar2GalaxySettingCategory cat in AIWar2GalaxySettingCategoryTable.Instance.Rows )
            {
                if ( cat.IsHidden ) //if hidden, skip
                    continue;

                if ( !cat.ShowEvenWhenEmpty )
                {
                    //if literally empty, skip
                    if ( cat.VisibleRows_All.Count <= 0 )
                        continue;

                    //otherwise check to see if all the settings within are hidden
                    foreach ( AIWar2GalaxySetting row in cat.VisibleRows_All )
                    {
                        if ( row.IsAdvancedSetting && !shouldShowAdvancedSettings )
                        {
                            if ( row.GetIsTempValueMatchingDefault() )
                            {
                                continue;
                            }
                        }
                            
                        var impl = row.OptionalImplementation;
                        if (impl != null && !impl.ShouldShow)
                            continue;
                        
                        goto show;
                    }
                    
                    continue;
                }
                
                show:
                this.categories.Add( cat );
            }
            
            if (this.CurrentCategory != null && !categories.Contains(this.CurrentCategory))
                this.CurrentCategory = this.categories.Count > 0 ? this.categories[0] : null;
        }
        #endregion

        private static ButtonAbstractBase.ButtonPool<bLeftColumnButton> btnLeftColumnButtonPool;

        #region custSetupWindowSectional
        public class custSetupWindowSectional : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_SettingsMenu.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( bLeftColumnButton.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnLeftColumnButtonPool = new ButtonAbstractBase.ButtonPool<bLeftColumnButton>( bLeftColumnButton.Original, 60 );
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

                btnLeftColumnButtonPool.Clear( 10 );

                List<AIWar2GalaxySettingCategory> categories = Instance.categories;
                for ( int i = 0; i < categories.Count; i++ )
                {
                    bLeftColumnButton item = btnLeftColumnButtonPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; //time slicing, too many added right now
                    item.Assign( categories[i] );
                }

                #region Positioning Logic 1
                btnLeftColumnButtonPool.ApplyItemsInRows( 10, ref currentY, 30, 218, 28 );
                #endregion

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)bLeftColumnButton.Original.Element.RelevantRect.parent;
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
                Buffer.Add( "游戏设置 - 其他选项" );
            }
        }
        #endregion

        #region bLeftColumnButton
        public class bLeftColumnButton : ButtonAbstractBase
        {
            public static bLeftColumnButton Original;
            public bLeftColumnButton() { if ( Original == null ) Original = this; }

            private AIWar2GalaxySettingCategory Category = null;

            public void Assign( AIWar2GalaxySettingCategory Category )
            {
                this.Category = Category;
            }

            public override bool GetShouldBeHidden()
            {
                return this.Category == null;
            }

            public override void Clear()
            {
                this.Category = null;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( this.Category == null )
                    return;

                if ( Instance.CurrentCategory == this.Category )
                    buffer.Add( "<color=#6fafff>" );

                buffer.Add( this.Category.DisplayName );
                buffer.AddDlcMod(this.Category, "Added by");
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.Category == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                Instance.CurrentCategory = this.Category;
                Instance.isTempShowingAdvanced = false;
                return MouseHandlingResult.None;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_SetupOptionsTab-bLeftColumnButton-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( this.Category == null )
                    return;

                tooltipBuffer.Add( this.Category.DisplayName ).Add( "\n" ).Add( this.Category.Description );
                tooltipBuffer.AddDlcMod(this.Category, "This Galaxy Options category was added by" );
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }
        #endregion

        #region PopulateSubclassControls
        public static float nameWidth = 300;
        public static float valueWidth = 185;
        public static float explainWidth = 300; //yes this goes over, but it prevents wrapping
        //private float fullWidth = 585;
        //private float spacerSize = 40f;

        private bool lastWasShowingAdvanced = false;

        protected override void PopulateSubclassControls( ArcenUI_SetOfCreateElementDirectives Set, ref float runningY )
        {
            bool shouldShowAdvancedSettings = CalculateShouldShowAdvancedSettings();

            if ( this.categories.Count == 0 || lastWasShowingAdvanced != shouldShowAdvancedSettings )
                this.RecalculateCategories();

            lastWasShowingAdvanced = shouldShowAdvancedSettings;

            if ( this.CurrentCategory == null && this.categories.Count > 0 )
            {
                this.CurrentCategory = this.categories[0];
                this.isTempShowingAdvanced = false;
            }

            if ( this.CurrentCategory == null )
                return;

            int numberAdvancedHidden = 0;

            //render the main stuff for this category
            this.RenderSettingsFromSubList( this.CurrentCategory.VisibleRows_NotInSubCategory, ref runningY, Set, shouldShowAdvancedSettings, null, ref numberAdvancedHidden );

            foreach ( AIWar2GalaxySettingSubcategory subCat in this.CurrentCategory.Subcategories )
            {
                if ( subCat.VisibleRows.Count <= 0 )
                    continue; //skip any subcategories that would be empty                

                //render the main stuff for this subcategory
                this.RenderSettingsFromSubList( subCat.VisibleRows, ref runningY, Set, shouldShowAdvancedSettings, subCat, ref numberAdvancedHidden );
            }

            if ( numberAdvancedHidden > 0 )
            {
                Rect nameBounds;
                this.CalculateBoundsSingle( out nameBounds, ref runningY, nameWidth + valueWidth + explainWidth );
                AddText( Set, type_tAdvancedHiddenOverall, string.Empty, numberAdvancedHidden, numberAdvancedHidden, nameBounds, 10f );
                runningY += this.rowHeight + this.rowBuffer;
            }

            bMainContentParent.ParentRT.UI_SetHeight( runningY );

        }
        #endregion

        #region RenderSettingsFromSubList
        private void RenderSettingsFromSubList( List<AIWar2GalaxySetting> listToShow, ref float runningY, ArcenUI_SetOfCreateElementDirectives Set, 
            bool shouldShowAdvancedSettings, AIWar2GalaxySettingSubcategory subCat, ref int numberAdvancedHidden )
        {
            Rect nameBounds;
            Rect valueBounds;
            Rect explainBounds;

            bool isFirst = true;
            int numberAdvancedHiddenHere = 0;

            for ( int i = 0; i < listToShow.Count; i++ )
            {
                var setting = listToShow[i];
                if ( setting.Deprecated ) //don't show deprecated settings
                    continue;
                if ( setting.IsAdvancedSetting && !shouldShowAdvancedSettings )
                {
                    if ( setting.GetIsSetupValueMatchingDefault(World_AIW2.Instance.Setup) ) //only hide advanced rows that match
                    {
                        //if ( !isInSandboxMode || setting.GetIsDefaultValueAlteredByHarshness() )
                        //{ } //DO draw these!
                        //else
                        {
                            numberAdvancedHidden++;
                            numberAdvancedHiddenHere++;
                            continue;
                        }
                    }
                }

                if ( isFirst && subCat != null )
                {
                    isFirst = false;
                    //New Section Header
                    {
                        runningY += ((this.rowHeight + rowBuffer) * 0.5f);
                        Rect nameBoundsHeader = ArcenRectangle.CreateUnityRect( 5, runningY, 400, this.rowHeight );
                        AddText( Set, type_tSubSectionHeader, subCat.InternalName, -1, -1, nameBoundsHeader, 12f );
                        runningY += this.rowHeight + rowBuffer;
                    }
                }

                this.CalculateBoundsTriple( out nameBounds, out valueBounds, out explainBounds, ref runningY, nameWidth, valueWidth, explainWidth );

                AddText( Set, type_tSettingName, setting.InternalName, setting.RowIndexNonSim, -1, nameBounds, 11f );//okay to use here, as it will be consistent per run

                bool editable = setting.GetShouldBeEditableAtThisHarshness();
                switch ( setting.Type )
                {
                    case AIWar2GalaxySettingType.BoolToggle:
                        if ( editable )
                            AddButton( Set, type_bToggle, setting.InternalName, setting.RowIndexNonSim, -1, valueBounds, -1f );
                        else
                            AddText( Set, type_tAltFor_bToggle, setting.InternalName, setting.RowIndexNonSim, -1, valueBounds, 12f );
                        break;
                    case AIWar2GalaxySettingType.IntTextbox:
                        if ( editable )
                            AddInput( Set, type_iIntInput, setting.InternalName, setting.RowIndexNonSim, -1, valueBounds );
                        else
                            AddText( Set, type_tAltFor_iIntInput, setting.InternalName, setting.RowIndexNonSim, -1, valueBounds, 12f );
                        break;
                    case AIWar2GalaxySettingType.IntSlider:
                        if ( editable )
                            AddHorizontalSlider( Set, type_sIntSlider, setting.InternalName, setting.RowIndexNonSim, -1, valueBounds );
                        else
                            AddText( Set, type_tAltFor_sIntSlider, setting.InternalName, setting.RowIndexNonSim, -1, valueBounds, 12f );
                        break;
                    case AIWar2GalaxySettingType.CustomDropdownArbitraryOptions:
                    case AIWar2GalaxySettingType.CustomDropdownSurrogateTable:
                    case AIWar2GalaxySettingType.CustomDropdownCoreTable:
                        if ( editable )
                            AddDropdown( Set, type_dCustomDropdown, setting.InternalName, setting.RowIndexNonSim, -1, valueBounds );
                        else
                            AddText( Set, type_tAltFor_dCustomDropdown, setting.InternalName, setting.RowIndexNonSim, -1, valueBounds, 12f );
                        break;
                }

                if ( editable )
                    AddText( Set, type_tSettingValueDescription, setting.InternalName, setting.RowIndexNonSim, -1, explainBounds, 11f );

                //runningY += this.rowHeight + rowBuffer;
            }

            if ( numberAdvancedHiddenHere > 0 && !isFirst )
            {
                nameBounds = ArcenRectangle.CreateUnityRect( 5, runningY, 400, this.rowHeight );
                AddText( Set, type_tAdvancedHiddenSubCat, subCat?.InternalName, numberAdvancedHiddenHere, -1, nameBounds, 10f );
                runningY += this.rowHeight + this.rowBuffer;
            }
        }
        #endregion

        public class tAdvancedHiddenOverall : TextAbstractBase
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
                    " 个高级字段，请点击此处临时显示它们。\n\n如需长期查看，请在顶部选择'常规'分类，点击'显示高级星系和派系选项'按钮。" );
            }
        }

        public class tAdvancedHiddenSubCat : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                int numberHidden = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                buffer.StartColor( "ff7e64" ).Add( "<voffset=0.2em><i>" ).Add( numberHidden ).Add( " 个子分类中的字段已隐藏</i>" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.isTempShowingAdvanced = !Instance.isTempShowingAdvanced;
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                int numberHidden = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "要查看当前在此子分类中隐藏的 " + numberHidden +
                    " 个高级字段，请点击此处临时显示它们（以及此选项卡上的所有其他字段）。\n\n如需长期查看，请在顶部选择'常规'分类，点击'显示高级星系和派系选项'按钮。" );
            }
        }

        public static AIWar2GalaxySetting GetSettingForController( ElementAbstractBase controller )
        {
            int tableIndex = controller.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            if ( tableIndex >= AIWar2GalaxySettingTable.Instance.Rows.Count )
                return null;
            return AIWar2GalaxySettingTable.Instance.Rows[tableIndex];
        }

        public class tSubSectionHeader : TextAbstractBase
        {
            public AIWar2GalaxySettingSubcategory GetSubcategory()
            {
                string subCatName = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString;
                if ( subCatName != null ) {
                    return AIWar2GalaxySettingSubcategoryTable.Instance.GetRowByName(subCatName);
                }
                return null;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                AIWar2GalaxySettingSubcategory subCat = this.GetSubcategory();
                if ( subCat == null )
                    buffer.Add( "<size=125%>Null subcategory!" );
                else
                    buffer.Add( "<size=125%>" ).StartColor( subCat.Color ).Add( subCat.DisplayName );
            }

            public override void HandleMouseover()
            {
                AIWar2GalaxySettingSubcategory subCat = this.GetSubcategory();
                if ( subCat == null )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, "空子类别！" );
                else
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, subCat.DisplayName + "\n" + subCat.Description );
            }
        }

        #region Custom Controls For In-Game Settings
        public class tSettingName : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                bool fixedByCampaign = setting.GetIsDefaultValueAlteredByHarshness();
                if ( !setting.GetIsSetupValueMatchingDefault(World_AIW2.Instance.Setup) )
                    buffer.StartColor( "ffde85" );
                else if ( fixedByCampaign )
                    buffer.StartColor( "b7ffbc" );
                buffer.Add( setting.DisplayName );
                
                var len = buffer.Builder.Length;
                buffer.AddDlcMod(setting, "由以下添加");
                var wroteAnotherLine = buffer.Builder.Length > len;
                
                if ( fixedByCampaign )
                    buffer.EndColor().StartColor( "85ffa2" ).Add( wroteAnotherLine ? "  " : "\n" ).Add( "<size=50%>（被战役类型或AI难度修改）" ).EndColor();
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_SetupOptionsTab-tSettingName-tooltipBuffer" );
            public override void HandleMouseover()
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                tooltipBuffer.Add( setting.DisplayName ).Add( "\n" ).Add( setting.Description );
                tooltipBuffer.AddDlcMod(setting, "This galaxy option was added by" );

                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }

        public class tSettingValueDescription : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                switch ( setting.Type )
                {
                    case AIWar2GalaxySettingType.BoolToggle:
                        break;
                    case AIWar2GalaxySettingType.IntSlider:
                        {
                            buffer.Add( "  " );
                            int tempValue = World_AIW2.Instance.Setup.GetIntBySetting( setting );
                            buffer.AddNumberMoreReadable( tempValue );
                            buffer.Add( "   " ).Add( FontSizes.SLIGHTLY_SMALLER_SIZE_V2_STRING ).Add( "<color=#999999>(" ).Add( setting.GetMinValue_Int().ToString() );
                            if ( setting.GetMaxValue_Int() > 0 && setting.GetMaxValue_Int() > setting.GetMinValue_Int() )
                                buffer.Add( " 到 " ).AddNumberMoreReadable( setting.GetMaxValue_Int() );
                            buffer.Add( ")" );
                        }
                        break;
                    case AIWar2GalaxySettingType.IntTextbox:
                        {
                            int valueAsInt = World_AIW2.Instance.Setup.GetIntBySetting( setting );
                            if ( valueAsInt < setting.GetMinValue_Int() )
                                buffer.Add( "  必须至少为 " ).Add( setting.GetMinValue_Int() );
                            else if ( setting.GetMaxValue_Int() > 0 && setting.GetMaxValue_Int() > setting.GetMinValue_Int() && valueAsInt > setting.GetMaxValue_Int() )
                                buffer.Add( "  必须至多为 " ).Add( setting.GetMaxValue_Int() );
                        }
                        break;
                }
            }

            public override void HandleMouseover()
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, setting.Description + setting.GetAddedTextForCampaignType() );
            }
        }

        public class bToggle : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                buffer.Add( World_AIW2.Instance.Setup.GetBoolBySetting( setting ) ? "开启" : "<color=#666666>关闭" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return MouseHandlingResult.None;

                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "AIWar2GalaxySettingChanged_Int";
                command.RelatedString2 = setting.InternalName;
                command.RelatedIntegers.Add( World_AIW2.Instance.Setup.GetBoolBySetting( setting ) ? 0 : 1 );
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, setting.Description + setting.GetAddedTextForCampaignType() );
            }
        }

        public class tAltFor_bToggle : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                buffer.Add( World_AIW2.Instance.Setup.GetIntBySetting(setting) > 0 ? "开启" : "<color=#666666>关闭" );
            }

            public override void HandleMouseover()
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, setting.Description + setting.GetAddedTextForCampaignType() );
            }
        }

        public class iIntInput : InputAbstractBase
        {
            public override void HandleChangeInValue( string NewValue )
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                int newValueAsInt;
                if ( !Int32.TryParse( NewValue, out newValueAsInt ) )
                    return;
                if ( newValueAsInt == World_AIW2.Instance.Setup.GetIntBySetting( setting ) )
                    return;
                if ( newValueAsInt < setting.GetMinValue_Int() )
                    return;
                if ( setting.GetMaxValue_Int() > setting.GetMinValue_Int() && newValueAsInt > setting.GetMaxValue_Int() )
                    return;

                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "AIWar2GalaxySettingChanged_Int";
                command.RelatedString2 = setting.InternalName;
                command.RelatedIntegers.Add( newValueAsInt );
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

                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return '\0';

                string testInput = input.Insert( charIndex, addedChar.ToString() );
                int testInputAsType;
                if ( !Int32.TryParse( testInput, out testInputAsType ) )
                    return '\0';

                return addedChar;
            }

            public override void OnUpdate()
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                //only update to the current value if we're not editing this field right now
                if ( !this.GetIsCurrentlyBeingEdited() )
                {
                    ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;
                    elementAsType.SetText( World_AIW2.Instance.Setup.GetIntBySetting( setting ).ToString() );
                }
            }

            public override void HandleMouseover()
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, setting.Description + setting.GetAddedTextForCampaignType() );
            }
        }

        public class tAltFor_iIntInput : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                buffer.Add( World_AIW2.Instance.Setup.GetIntBySetting( setting ).ToString() );
            }

            public override void HandleMouseover()
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, setting.Description + setting.GetAddedTextForCampaignType() );
            }
        }

        public class sIntSlider : SliderAbstractBase
        {
            private bool hasNewValueBeenSetButNotSent = false;
            private int newValueIs = 0;
            private double lastTimeOfOnChanged = 0;
            private const float MIN_TIME_BETWEEN_SENDS = 0.2f;

            public override void OnUpdate()
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                int currentValue = World_AIW2.Instance.Setup.GetIntBySetting( setting );
                int range = setting.GetMaxValue_Int() - setting.GetMinValue_Int();
                if ( range == 0 ) range = 1;
                float currentPortion = (float)(currentValue - setting.GetMinValue_Int()) / (float)range;

                ArcenUI_Slider elementAsType = (ArcenUI_Slider)this.Element;
                elementAsType.ReferenceSlider.value = currentPortion;

                if ( hasNewValueBeenSetButNotSent && Engine_Universal.CumulativeUnscaledTime - lastTimeOfOnChanged >= MIN_TIME_BETWEEN_SENDS )
                {
                    hasNewValueBeenSetButNotSent = false;
                    GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    command.RelatedString = "AIWar2GalaxySettingChanged_Int";
                    command.RelatedString2 = setting.InternalName;
                    command.RelatedIntegers.Add( newValueIs );
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                }
            }

            public override void OnChange( float NewValue )
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                lastTimeOfOnChanged = Engine_Universal.CumulativeUnscaledTime;

                int range = setting.GetMaxValue_Int() - setting.GetMinValue_Int();
                int adjustedNewValue = setting.GetMinValue_Int() + Mathf.RoundToInt( range * NewValue );

                if ( adjustedNewValue == World_AIW2.Instance.Setup.GetIntBySetting( setting ) )
                    return;

                newValueIs = adjustedNewValue;
                hasNewValueBeenSetButNotSent = true;
                //this is an MP desync, but keeps the interface responsive.  It will be synced up within a part of a second.
                World_AIW2.Instance.Setup.SetIntBySetting( setting, newValueIs );
            }

            public override void HandleMouseover()
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, setting.Description + setting.GetAddedTextForCampaignType() );
            }
        }

        public class tAltFor_sIntSlider : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                buffer.Add(World_AIW2.Instance.Setup.GetIntBySetting( setting ));
                buffer.Add( "    <color=#999999>(" ).Add( setting.GetMinValue_Int() ).Add( " 到 " ).Add( setting.GetMaxValue_Int() ).Add( ")" );
            }

            public override void HandleMouseover()
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, setting.Description + setting.GetAddedTextForCampaignType() );
            }
        }

        public abstract class StringBasedCustomDropdown : DropdownAbstractBase
        {
            private string SelectedItemAsType
            {
                get
                {
                    AIWar2GalaxySetting setting = GetSettingForController( this );
                    if ( setting == null ) return string.Empty;

                    string currentValue = World_AIW2.Instance.Setup.GetStringBySetting( setting );
                    return currentValue;
                }
            }

            private bool shouldIgnoreChanges = false;
            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                if ( Item == null || this.shouldIgnoreChanges )
                    return;
                string ItemAsType = ( Item.GetItem() as IOption).GetInternalName();

                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                string oldValue = World_AIW2.Instance.Setup.GetStringBySetting( setting );
                if ( oldValue == ItemAsType )
                    return; //no change!

                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "AIWar2GalaxySettingChanged_String";
                command.RelatedString2 = setting.InternalName;
                command.RelatedString3 = ItemAsType;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
            }

            public override void OnUpdate()
            {
                this.shouldIgnoreChanges = true;
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                List<IOption> options = this.GetOptions();
                if ( elementAsType.GetItemCount() != options.Count )
                {
                    elementAsType.ClearItems();
                    string selectedString = this.SelectedItemAsType;
                    for ( int i = 0; i < options.Count; i++ )
                    {
                        string optionValue = options[i].GetInternalName();
                        StringTrioBasedDropdownOption option = this.ConstructOptionFor( options[i] );
                        elementAsType.AddItem( option, optionValue == selectedString );
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
                AIWar2GalaxySetting setting = GetSettingForController( this );
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
                AIWar2GalaxySetting setting = GetSettingForController( this );
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
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) 
                    return emptyList;
                return setting.GetOptions();
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
                    AIWar2GalaxySetting setting = GetSettingForController( this );
                    if ( setting == null ) return string.Empty;

                    string currentValue = World_AIW2.Instance.Setup.GetStringBySetting(setting);
                    return currentValue;
                }
            }

            private static List<IOption> emptyList = List<IOption>.Create_WillNeverBeGCed( 1, "tAltFor_dCustomDropdown-emptyList" );
            public List<IOption> GetOptions()
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) 
                    return emptyList;
                return setting.GetOptions();
            }

            private string lastDisplayStringAsType = string.Empty;
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
                    }

                    debugStage = 700;
                    buffer.Add( this.lastDisplayStringAsType );
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "tAltFor_dCustomDropdown GetTextToShowFromVolatile: Error at debug stage " + debugStage +
                                                  "\n" + e, Verbosity.ShowAsError );
                }
            }

            public override void HandleMouseover()
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, setting.Description + setting.GetAddedTextForCampaignType() );
            }
        }
        #endregion

        #region dCenterOptions
        public class dCenterOptions : DropdownAbstractBase
        {
            public static dCenterOptions Instance;
            public dCenterOptions()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                
            }

            public override void OnUpdate()
            {
                
            }
            public override void HandleMouseover()
            {
                
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenUI_Dropdown_Option Item )
            {
            }
            public override bool GetShouldBeHidden()
            {
                return true; //not used on this screen
            }
        }
        #endregion

        #region btnLeftColumn1
        public class btnLeftColumn1 : ButtonAbstractBase
        {
            public static btnLeftColumn1 Instance;

            public btnLeftColumn1()
            {
                Instance = this;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }
            public override bool GetShouldBeHidden()
            {
                return true; //not used on this screen
            }
        }
        #endregion

        #region btnLeftColumn2
        public class btnLeftColumn2 : ButtonAbstractBase
        {
            public static btnLeftColumn2 Instance;

            public btnLeftColumn2()
            {
                Instance = this;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }
            public override bool GetShouldBeHidden()
            {
                return true; //not used on this screen
            }
        }
        #endregion

        #region Inherited Stuff
        public class tChatText : tChatText_Base
        {
        }

        public class tPlayerInfoText : tPlayerInfoText_Base
        { }

        public class tChatHeaderText : tChatHeaderText_Base
        { }

        public class btnSendChat : btnSendChat_Base
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                iChatTextbox.Instance.DoSend();
                return MouseHandlingResult.None;
            }
        }

        public class iChatTextbox : iChatTextbox_Base
        {
            public static iChatTextbox Instance;
            public iChatTextbox() { Instance = this; }

            public override void DoSend()
            {
                DoSend_Inner( this );
            }

            public override void ClearTextbox()
            {
                this.SetText( string.Empty );
            }
        }
        #endregion
    }
}
