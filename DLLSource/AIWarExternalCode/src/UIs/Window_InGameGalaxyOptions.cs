using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameGalaxyOptions : ToggleableWindowController, IInputActionHandler
    {
        protected float topBuffer = 3;
        protected float leftBuffer = 2;
        protected float rowHeight = 24;
        protected float rowBuffer = 1.5f;
        
        #region CalculateBoundsTriple
        protected void CalculateBoundsTriple( out Rect nameBounds, out Rect valueSettingControlBounds, out Rect valueDescriptionBounds, ref float runningY, float NameWidth, float ValueWidth, float DescriptionWidth )
        {
            nameBounds = ArcenRectangle.CreateUnityRect( leftBuffer, runningY, NameWidth, rowHeight );

            valueSettingControlBounds = ArcenRectangle.CreateUnityRect( nameBounds.xMax, runningY, ValueWidth, rowHeight );

            valueDescriptionBounds = ArcenRectangle.CreateUnityRect( valueSettingControlBounds.xMax, runningY, DescriptionWidth, rowHeight );

            runningY += rowHeight + rowBuffer;
        }
        #endregion

        public AIWar2GalaxySettingCategory CurrentCategory;
        public static Window_InGameGalaxyOptions Instance;
        public Window_InGameGalaxyOptions()
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
            AIWar2GalaxySettingTable.Instance.CopyCurrentValuesToTemp( World_AIW2.Instance.Setup );
            this.lastWasShowingAdvanced = CalculateShouldShowAdvancedSettings();

            RecalculateCategories();
            
            if ( Window_InGameGalaxyOptions.Instance.categories.Count > 0 )
                this.CurrentCategory = Window_InGameGalaxyOptions.Instance.categories[0];
            else
                this.CurrentCategory = null;
            
            this.isTempShowingAdvanced = false;

            World_AIW2.Instance.DoOnSpecialEvent_OnMainThread_ClientOrHost_ForAllFactionsAndExternalWorldBaseInfo( SpecialEventType.IngameGalaxyOptionsOpened, Engine_AIW2.Instance.MainThreadContext_ClientOrHost );
        }

        public bool isTempShowingAdvanced = false;
        public bool CalculateShouldShowAdvancedSettings()
        {
            //this is different from the lobby way!  It won't work in-game
            return AIWar2GalaxySettingTable.Instance.GetRowByName( "ShowAdvancedGalaxyAndFactionOptions" ).GetTempValue_Int() > 0 || isTempShowingAdvanced;
        }

        #region RecalculateCategories
        
        public readonly List<AIWar2GalaxySettingCategory> categories = List<AIWar2GalaxySettingCategory>.Create_WillNeverBeGCed( 300, "Window_InGameGalaxyOptions-categories" );
        
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

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "银河系选项" );
            }
        }
        #endregion
        private bool lastWasShowingAdvanced = false;

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

        public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            if ( bMainContentParent.ParentT == null )
                return;
            
            this.Window.SetOverridingTransformToWhichToAddChildren( bMainContentParent.ParentT );

            bool shouldShowAdvancedSettings = CalculateShouldShowAdvancedSettings();

            if ( this.categories.Count == 0 || lastWasShowingAdvanced != shouldShowAdvancedSettings )
                RecalculateCategories();

            lastWasShowingAdvanced = shouldShowAdvancedSettings;

            if ( this.CurrentCategory == null && this.categories.Count > 0 )
            {
                this.CurrentCategory = this.categories[0];
                this.isTempShowingAdvanced = false;
            }
            
            if ( this.CurrentCategory == null )
                return;

            float runningY = topBuffer;
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
                Rect nameBounds = ArcenRectangle.CreateUnityRect( 5, runningY, 400, this.rowHeight );
                AddText( Set, type_tAdvancedHiddenOverall, string.Empty, numberAdvancedHidden, numberAdvancedHidden, nameBounds, 10f );
                runningY += this.rowHeight + this.rowBuffer;
            }

            bMainContentParent.ParentRT.UI_SetHeight( runningY );
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
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, "Null subcategory!" );
                else
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, subCat.DisplayName + "\n" + subCat.Description );
            }
        }

        #region RenderSettingsFromSubList
        private void RenderSettingsFromSubList( List<AIWar2GalaxySetting> listToShow, ref float runningY, ArcenUI_SetOfCreateElementDirectives Set,
            bool shouldShowAdvancedSettings, AIWar2GalaxySettingSubcategory subCat, ref int numberAdvancedHidden )
        {
            Rect nameBounds;
            Rect valueBounds;
            Rect explainBounds;

            bool isFirst = true;
            int numberAdvancedHiddenHere = 0;
            bool isInSandboxMode = World_AIW2.Instance.CampaignType.IsConsideredSandbox;

            for ( int i = 0; i < listToShow.Count; i++ )
            {
                AIWar2GalaxySetting setting = listToShow[i];
                if ( setting.Deprecated ) //don't show deprecated settings
                    continue;
                if ( setting.IsAdvancedSetting && !shouldShowAdvancedSettings )
                {
                    if ( setting.GetIsTempValueMatchingDefault() ) //only hide advanced rows that match
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

                this.CalculateBoundsTriple( out nameBounds, out valueBounds, out explainBounds, ref runningY, Window_SetupOptionsTab.nameWidth, Window_SetupOptionsTab.valueWidth, Window_SetupOptionsTab.explainWidth );

                AddText( Set, type_tSettingName, setting.InternalName, setting.RowIndexNonSim, -1, nameBounds, 11f );//okay to use here, as it will be consistent per run

                bool editable = setting.ChangeableDuringGameplay && setting.GetShouldBeEditableAtThisHarshness();
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
                buffer.StartColor( "ff7e64" ).Add( "<i>" ).Add( numberHidden ).Add( " Advanced Fields Are Hidden</i>" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.isTempShowingAdvanced = !Instance.isTempShowingAdvanced;
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                int numberHidden = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "To view the " + numberHidden +
                    " advanced fields that are presently hidden, click here to temporarily see them.\n\nFor longer term viewing, choose the General tab on the left, and click the 'Show Advanced Galaxy And Faction Options' button." );
            }
        }

        public class tAdvancedHiddenSubCat : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                int numberHidden = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                buffer.StartColor( "ff7e64" ).Add( "<voffset=0.2em><i>" ).Add( numberHidden ).Add( " Hidden Fields In This Subcategory</i>" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.isTempShowingAdvanced = !Instance.isTempShowingAdvanced;
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                int numberHidden = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, "To view the " + numberHidden +
                    " advanced fields that are presently hidden in this subcategory, click here to temporarily see them (and all the others on this tab).\n\nFor longer term viewing, choose the General tab on the left, and click the 'Show Advanced Galaxy And Faction Options' button." );
            }
        }

        public static AIWar2GalaxySetting GetSettingForController( ElementAbstractBase controller )
        {
            int tableIndex = controller.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            if ( tableIndex >= AIWar2GalaxySettingTable.Instance.Rows.Count )
                return null;
            return AIWar2GalaxySettingTable.Instance.Rows[tableIndex];
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
                if ( Window_InGameGalaxyOptions.Instance != null )
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

                List<AIWar2GalaxySettingCategory> categories = Window_InGameGalaxyOptions.Instance.categories;
                for ( int i = 0; i < categories.Count; i++ )
                {
                    bCategory item = btnCategoryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; //time slicing, too many added right now
                    item.Assign( categories[i] );
                }

                #region Positioning Logic 1
                btnCategoryPool.ApplyItemsInRows( 10, ref currentY, 30, 218, 28);
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
                buffer.AddDlcMod(this.Category);
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.Category == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                Instance.CurrentCategory = this.Category;
                Instance.isTempShowingAdvanced = false;
                return MouseHandlingResult.None;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_InGameGalaxyOptions-bCategory-tooltipBuffer" );
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

        public class tSettingName : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                
                
                bool isfixed = setting.GetIsDefaultValueAlteredByHarshness();
                bool isdefault = setting.GetIsTempValueMatchingDefault();
                
                string col = "ffffff";
                if (isfixed)
                    col = "ffde85";
                else if (isdefault)
                    col = "ffffff";
                else
                    col = "ffde85";
                
                buffer.Add( setting.DisplayName, col );

                buffer.AddDlcMod(setting);

                if ( isfixed )
                {
                    buffer
                        .NewLine()
                        .StartColor( "85ffa2" )
                        .Add( "<size=50%>" )
                        .Add( "（被战役类型或AI难度修改）" )
                        .Add( "</size>" )
                        .EndColor();
                }
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_InGameGalaxyOptions-tSettingName-tooltipBuffer" );
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
                            int tempValue = setting.GetTempValue_Int();
                            buffer.AddNumberMoreReadable( tempValue );
                            buffer.Add( "   " ).Add( FontSizes.SLIGHTLY_SMALLER_SIZE_V2_STRING ).Add("<color=#999999>(").Add( setting.GetMinValue_Int().ToString() );
                            if ( setting.GetMaxValue_Int() > 0 && setting.GetMaxValue_Int() > setting.GetMinValue_Int() )
                                buffer.Add( " to " ).AddNumberMoreReadable( setting.GetMaxValue_Int() );
                            buffer.Add( ")" );
                        }
                        break;
                    case AIWar2GalaxySettingType.IntTextbox:
                        int valueAsInt = setting.GetTempValue_Int();
                        if ( valueAsInt < setting.GetMinValue_Int() )
                buffer.Add( "  必须至少为 " ).Add( setting.GetMinValue_Int() );
                            else if ( setting.GetMaxValue_Int() > 0 && setting.GetMaxValue_Int() > setting.GetMinValue_Int() && valueAsInt > setting.GetMaxValue_Int() )
                                buffer.Add( "  必须至多为 " ).Add( setting.GetMaxValue_Int() );
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

                buffer.Add( setting.GetTempValue_Int() > 0 ? "On" : "<color=#666666>Off" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return MouseHandlingResult.None;

                if ( setting.GetTempValue_Int() > 0 )
                    setting.SetTempValue_Int( 0 );
                else
                    setting.SetTempValue_Int( 1 );
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

                buffer.Add( setting.GetTempValue_Int() > 0 ? "On" : "<color=#666666>Off" );
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
                if ( newValueAsInt < setting.GetMinValue_Int() )
                    return;
                if ( setting.GetMaxValue_Int() > setting.GetMinValue_Int() && newValueAsInt > setting.GetMaxValue_Int() )
                    return;

                setting.SetTempValue_Int( newValueAsInt );
            }

            public override void OnUpdate()
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                //only update to the current value if we're not editing this field right now
                if ( !this.GetIsCurrentlyBeingEdited() )
                {
                    ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;
                    elementAsType.SetText( setting.GetTempValue_Int().ToString() );
                }
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

                buffer.Add( setting.GetTempValue_Int().ToString() );
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
            public override void OnUpdate()
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                int currentValue = setting.GetTempValue_Int();
                int range = setting.GetMaxValue_Int() - setting.GetMinValue_Int();
                if ( range == 0 ) range = 1;
                float currentPortion = (float)( currentValue - setting.GetMinValue_Int() ) / (float)range;

                ArcenUI_Slider elementAsType = (ArcenUI_Slider)this.Element;
                elementAsType.ReferenceSlider.value = currentPortion;
            }

            public override void OnChange( float NewValue )
            {
                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                int range = setting.GetMaxValue_Int() - setting.GetMinValue_Int();
                int adjustedNewValue = setting.GetMinValue_Int() + Mathf.RoundToInt( range * NewValue );

                setting.SetTempValue_Int( adjustedNewValue );
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

                buffer.Add( setting.GetTempValue_Int().ToString() );
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

                    string currentValue = setting.GetTempValue_String();
                    return currentValue;
                }
            }

            private bool shouldIgnoreChanges = false;
            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                if ( Item == null || this.shouldIgnoreChanges )
                    return;
                string ItemAsType = (Item.GetItem() as IOption).GetInternalName();

                AIWar2GalaxySetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                string oldValue = setting.GetTempValue_String();
                if ( oldValue == ItemAsType )
                    return; //no change!
                    
                setting.SetTempValue_String( ItemAsType );
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

                    string currentValue = setting.GetTempValue_String();
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

        public class bCancel : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();
                World_AIW2.Instance.DoOnSpecialEvent_OnMainThread_ClientOrHost_ForAllFactionsAndExternalWorldBaseInfo( SpecialEventType.IngameGalaxyOptionsCanceled, Engine_AIW2.Instance.MainThreadContext_ClientOrHost );
                return MouseHandlingResult.None;
            }
        }

        public class bSave : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                AIWar2GalaxySettingTable.Instance.CopyTempValuesToCurrent( World_AIW2.Instance.Setup );
                GameSettings.SaveToDisk();
                GameSettings.Current.DoGraphicsSettingsNeedARefresh = true;
                Instance.Close();
                World_AIW2.Instance.DoOnSpecialEvent_OnMainThread_ClientOrHost_ForAllFactionsAndExternalWorldBaseInfo( SpecialEventType.IngameGalaxyOptionsSavedAndApplied, Engine_AIW2.Instance.MainThreadContext_ClientOrHost );
                return MouseHandlingResult.None;
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
            AIWar2GalaxySettingTable.Instance.CopyCurrentValuesToTemp( World_AIW2.Instance.Setup ); // shouldn't matter, but tidies things up
            base.Close();
        }

        //from IInputActionHandler
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
