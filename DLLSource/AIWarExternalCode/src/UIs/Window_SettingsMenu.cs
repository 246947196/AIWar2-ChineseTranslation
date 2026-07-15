using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using UnityEngine;
using System.Text;

namespace Arcen.AIW2.External
{
    public class Window_SettingsMenu : ToggleableWindowController, IInputActionHandler
    {
        private readonly float rowHeight = 24;
        private readonly float rowBuffer = 1.5f;
        public ArcenSettingCategory CurrentCategory;
        public static Window_SettingsMenu Instance;
        public Window_SettingsMenu()
        {
            Instance = this;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
            this.PreventsNormalInputHandlers = true;
            this.SuppressesUIScaling = true;
        }

        public override void OnOpen()
        {
            ArcenSettingTable.Instance.CopyCurrentValuesToTemp();

			// Lets just not... its nicer to retain the prior view
            /*
            this.lastWasShowingAdvanced = CalculateShouldShowAdvancedSettings();
            this.RecalculateCategories();
            if ( this.categories.Count > 0 )
                this.CurrentCategory = this.categories[0];
            else
                this.CurrentCategory = null;
            this.isTempShowingAdvanced = false;

            if ( bCategory.Original != null ) //scroll left panel back to top when opening
                bCategory.Original.Element.TryScrollToTop();
            */

            World_AIW2.Instance.DoOnSpecialEvent_OnMainThread_ClientOrHost_ForAllFactionsAndExternalWorldBaseInfo( SpecialEventType.PersonalSettingsOpened, Engine_AIW2.Instance.MainThreadContext_ClientOrHost );
        }

        public bool isTempShowingAdvanced = false;
        public bool CalculateShouldShowAdvancedSettings()
        {
            return ArcenSettingTable.Instance.GetRowByName( "ShowAdvancedSettings" ).TempValue_Bool || isTempShowingAdvanced;
        }

        #region RecalculateCategories
        public readonly List<ArcenSettingCategory> categories = List<ArcenSettingCategory>.Create_WillNeverBeGCed( 400, "Window_SettingsMenu-categories" );
        public void RecalculateCategories()
        {
            bool shouldShowAdvancedSettings = CalculateShouldShowAdvancedSettings();
            categories.Clear();
            foreach ( ArcenSettingCategory cat in ArcenSettingCategoryTable.Instance.Rows )
            {
                if ( cat.IsHidden ) //if hidden, skip
                    continue;
                
                if ( !cat.ShowEvenWhenEmpty )
                {
                    //if literally empty, skip
                    if ( cat.VisibleRows_All.Count <= 0 )
                        continue;

                    //otherwise check to see if all the settings within are hidden
                    bool foundAtLeastOneVisible = false;
                    foreach ( ArcenSetting row in cat.VisibleRows_All )
                    {
                        if ( row.GetShouldShow() )
                        {
                            if ( row.IsAdvancedSetting && !shouldShowAdvancedSettings )
                            {
                                if ( row.GetIsTempValueMatchingDefault() )
                                    continue; //only hide advanced rows that match
                            }
                            foundAtLeastOneVisible = true;
                            break;
                        }
                    }
                    if ( !foundAtLeastOneVisible )
                        continue;
                }
                categories.Add( cat );
            }
        }
        #endregion

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "个人设置" );
            }
        }
        #endregion

        private bool lastWasShowingAdvanced = false;


        private ArcenCachedExternalTypeDirect type_tExpansionName = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tExpansionName ) );
        private ArcenCachedExternalTypeDirect type_bExpansionToggle = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bExpansionToggle ) );
        private ArcenCachedExternalTypeDirect type_tExpansionToggleDisabledDuringGame = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tExpansionToggleDisabledDuringGame ) );
        private ArcenCachedExternalTypeDirect type_tExpansionNotInstalled = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tExpansionNotInstalled ) );
        private ArcenCachedExternalTypeDirect type_tXmlModName = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tXmlModName ) );
        private ArcenCachedExternalTypeDirect type_bXmlModToggle = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bXmlModToggle ) );
        private ArcenCachedExternalTypeDirect type_tPublicIPHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tPublicIPHeader ) );
        private ArcenCachedExternalTypeDirect type_iPublicIPBody = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( iPublicIPBody ) );
        private ArcenCachedExternalTypeDirect type_tLocalIPHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tLocalIPHeader ) );
        private ArcenCachedExternalTypeDirect type_iLocalIPBody = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( iLocalIPBody ) );
        private ArcenCachedExternalTypeDirect type_tAdvancedHiddenOverall = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tAdvancedHiddenOverall ) );
        private ArcenCachedExternalTypeDirect type_tSubSectionHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tSubSectionHeader ) );
        private ArcenCachedExternalTypeDirect type_tSettingName = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tSettingName ) );
        private ArcenCachedExternalTypeDirect type_bToggle = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bToggle ) );
        private ArcenCachedExternalTypeDirect type_iIntInput = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( iIntInput ) );
        private ArcenCachedExternalTypeDirect type_sFloatSlider = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( sFloatSlider ) );
        private ArcenCachedExternalTypeDirect type_sIntSlider = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( sIntSlider ) );
        private ArcenCachedExternalTypeDirect type_dIntDropdown = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( dIntDropdown ) );
        private ArcenCachedExternalTypeDirect type_tSettingValueDescription = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tSettingValueDescription ) );
        private ArcenCachedExternalTypeDirect type_tAdvancedHiddenSubCat = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tAdvancedHiddenSubCat ) );

        public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            if ( bMainContentParent.ParentT == null )
                return;
            
            this.Window.SetOverridingTransformToWhichToAddChildren( bMainContentParent.ParentT );

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

            //arguments to createunityrectagle: X, Y, width, height

            // Add header buttons (close/save/reset)
            float runningY = 3;

            int numberAdvancedHidden = 0;

            //render the main stuff for this category
            this.RenderSettingsFromSubList( this.CurrentCategory.VisibleRows_NotInSubCategory, ref runningY, Set, shouldShowAdvancedSettings, null, ref numberAdvancedHidden );

            #region ShowExpansionsList
            if ( this.CurrentCategory.ShowExpansionsList )
            {
                for ( int i = 0; i < ExpansionTable.Instance.Rows.Count; i++ )
                {
                    Expansion expansion = ExpansionTable.Instance.Rows[i];
                    Rect nameBounds = ArcenRectangle.CreateUnityRect( 5, runningY, 400, this.rowHeight );
                    AddText( Set, type_tExpansionName, expansion.InternalName, expansion.RowIndexNonSim, -1, nameBounds, 12f );

                    Rect valueSettingControlBounds = ArcenRectangle.CreateUnityRect( nameBounds.xMax, runningY, 170, this.rowHeight );
                    if ( expansion.IsInstalledAtAll )
                    {
                        if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                            AddButton( Set, type_bExpansionToggle, expansion.InternalName, expansion.RowIndexNonSim, -1, valueSettingControlBounds, -1f );
                        else
                            AddText( Set, type_tExpansionToggleDisabledDuringGame, expansion.InternalName, expansion.RowIndexNonSim, -1, valueSettingControlBounds, -1f );
                    }
                    else
                        AddText( Set, type_tExpansionNotInstalled, expansion.InternalName, expansion.RowIndexNonSim, -1, valueSettingControlBounds, 12f );

                    runningY += this.rowHeight + rowBuffer;
                }
            }
            #endregion

            void AddControlFor( XmlMod mod )
            {
                Rect nameBounds = ArcenRectangle.CreateUnityRect( 5, runningY, 400, this.rowHeight );
                AddText( Set, type_tXmlModName, mod.InternalName, mod.RowIndexNonSim, -1, nameBounds, 12f );

                Rect valueSettingControlBounds = ArcenRectangle.CreateUnityRect( nameBounds.xMax, runningY, 170, this.rowHeight );
                {
                    _AddBase( "ButtonBlue", Set, type_bXmlModToggle, mod.InternalName, mod.RowIndexNonSim, -1, string.Empty, valueSettingControlBounds, -1f );
                }

                runningY += this.rowHeight + rowBuffer;
            }

            #region ShowMainModsList
            if ( this.CurrentCategory.ShowMainModsList )
            {
                var modlist = XmlModTable.ModsInDisplayOrder;
                for ( int i = 0; i < modlist.Count; i++ )
                {
                    XmlMod mod = modlist[i] as XmlMod;
                    AddControlFor(mod);
                }
            }
            #endregion

            foreach ( ArcenSettingSubcategory subCat in this.CurrentCategory.Subcategories )
            {
                //skip any subcategories that would be empty
                if ( subCat.VisibleRows.Count <= 0 && !subCat.ShowNetworkExtras && !subCat.ShowFrameworkModsList )
                    continue; 

                //render the main stuff for this subcategory
                this.RenderSettingsFromSubList( subCat.VisibleRows, ref runningY, Set, shouldShowAdvancedSettings, subCat, ref numberAdvancedHidden );

                #region ShowNetworkExtras
                if ( subCat.ShowNetworkExtras )
                {
                    //Public IP Address
                    {
                        string publicIP = ArcenNetworkAuthority.GetMyPublicIPAddress();
                        Rect nameBounds = ArcenRectangle.CreateUnityRect( 5, runningY, 400, this.rowHeight );
                        AddText( Set, type_tPublicIPHeader, publicIP, -1, -1, nameBounds, 12f );

                        Rect valueSettingControlBounds = ArcenRectangle.CreateUnityRect( nameBounds.xMax, runningY, 280, this.rowHeight );
                        AddInput( Set, type_iPublicIPBody, publicIP, -1, -1, valueSettingControlBounds );

                        runningY += this.rowHeight + rowBuffer;
                    }

                    //Local IP Addresses
                    List<string> localIPs = ArcenNetworkAuthority.GetListOfLocalIPAddresses();
                    for ( int i = 0; i < localIPs.Count; i++ )
                    {
                        string localIP = localIPs[i];
                        Rect nameBounds = ArcenRectangle.CreateUnityRect( 5, runningY, 400, this.rowHeight );
                        AddText( Set, type_tLocalIPHeader, localIP, i, i, nameBounds, 12f );

                        Rect valueSettingControlBounds = ArcenRectangle.CreateUnityRect( nameBounds.xMax, runningY, 280, this.rowHeight );
                        AddInput( Set, type_iLocalIPBody, localIP, i, i, valueSettingControlBounds );

                        runningY += this.rowHeight + rowBuffer;
                    }
                }
                #endregion
            }

            if ( numberAdvancedHidden > 0 )
            {
                Rect nameBounds = ArcenRectangle.CreateUnityRect( 5, runningY, 400, this.rowHeight );
                AddText( Set, type_tAdvancedHiddenOverall, string.Empty, numberAdvancedHidden, numberAdvancedHidden, nameBounds, 10f );
                runningY += this.rowHeight + this.rowBuffer;
            }

            bMainContentParent.ParentRT.UI_SetHeight( runningY );
        }

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
                    " 个高级字段，请点击此处临时显示它们。\n\n如需长期查看，请在顶部选择'游戏'选项卡，点击'显示高级设置'按钮。" );
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
                    " 个高级字段，请点击此处临时显示它们（以及此选项卡上的所有其他字段）。\n\n如需长期查看，请在顶部选择'游戏'选项卡，点击'显示高级设置'按钮。" );
            }
        }

        #region RenderSettingsFromSubList
        private void RenderSettingsFromSubList( List<ArcenSetting> listToShow, ref float runningY, ArcenUI_SetOfCreateElementDirectives Set, 
            bool shouldShowAdvancedSettings, ArcenSettingSubcategory subCat, ref int numberAdvancedHidden )
        {
            if ( listToShow.Count <= 0 )
                return;

            bool isFirst = true;
            int numberAdvancedHiddenHere = 0;

            for ( int i = 0; i < listToShow.Count; i++ )
            {
                ArcenSetting setting = listToShow[i];
                
                //don't show deprecated settings
                if ( setting.Deprecated ) 
                    continue;
                
                //don't show things hidden by mods or expansions not being installed
                //or ones with <show_if> conditions not met
                if ( !setting.GetShouldShow() )
                    continue; 
                
                if ( setting.IsAdvancedSetting && !shouldShowAdvancedSettings )
                {
                    //only hide advanced rows that match
                    if ( setting.GetIsTempValueMatchingDefault() ) 
                    { 
                        numberAdvancedHidden++;
                        numberAdvancedHiddenHere++;
                        continue;
                    }
                }

                if ( isFirst && subCat != null )
                {
                    isFirst = false;
                    //New Section Header
                    {
                        runningY += ((this.rowHeight + rowBuffer) * 0.5f);
                        Rect nameHeaderBounds = ArcenRectangle.CreateUnityRect( 5, runningY, 400, this.rowHeight );
                        AddText( Set, type_tSubSectionHeader, subCat.InternalName, -1, -1, nameHeaderBounds, 12f );
                        runningY += this.rowHeight + rowBuffer;
                    }
                }

                Rect nameBounds = ArcenRectangle.CreateUnityRect( 5, runningY, 400, this.rowHeight );
                AddText( Set, type_tSettingName, setting.InternalName, setting.RowIndexNonSim, -1, nameBounds, 12f );//okay to use here, as it will be consistent per run

                Rect valueSettingControlBounds = ArcenRectangle.CreateUnityRect( nameBounds.xMax, runningY, 170, this.rowHeight );
                switch ( setting.Type )
                {
                    case ArcenSettingType.BoolToggle:
                        AddButton( Set, type_bToggle, setting.InternalName, setting.RowIndexNonSim, -1, valueSettingControlBounds, -1f );
                        break;
                    case ArcenSettingType.IntTextbox:
                        AddInput( Set, type_iIntInput, setting.InternalName, setting.RowIndexNonSim, -1, valueSettingControlBounds );
                        break;
                    case ArcenSettingType.FloatSlider:
                        AddHorizontalSlider( Set, type_sFloatSlider, setting.InternalName, setting.RowIndexNonSim, -1, valueSettingControlBounds );
                        break;
                    case ArcenSettingType.IntSlider:
                        AddHorizontalSlider( Set, type_sIntSlider, setting.InternalName, setting.RowIndexNonSim, -1, valueSettingControlBounds );
                        break;
                    case ArcenSettingType.IntDropdown:
                        AddDropdown( Set, type_dIntDropdown, setting.InternalName, setting.RowIndexNonSim, -1, valueSettingControlBounds, -1f );
                        break;
                }

                Rect valueDescriptionBounds = ArcenRectangle.CreateUnityRect( valueSettingControlBounds.xMax, runningY, 320, this.rowHeight );
                AddText( Set, type_tSettingValueDescription, setting.InternalName, setting.RowIndexNonSim, -1, valueDescriptionBounds, 14f );

                runningY += this.rowHeight + rowBuffer;
            }

            if ( numberAdvancedHiddenHere > 0 && !isFirst )
            {
                Rect nameBounds = ArcenRectangle.CreateUnityRect( 5, runningY, 400, this.rowHeight );
                AddText( Set, type_tAdvancedHiddenSubCat, subCat?.InternalName, numberAdvancedHiddenHere, -1, nameBounds, 10f );
                runningY += this.rowHeight + this.rowBuffer;
            }
        }
        #endregion

        public static ArcenSetting GetSettingForController( ElementAbstractBase controller )
        {
            int tableIndex = controller.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            if ( tableIndex >= ArcenSettingTable.Instance.Rows.Count )
                return null;
            return ArcenSettingTable.Instance.Rows[tableIndex];
        }

        public static Expansion GetExpansionForController( ElementAbstractBase controller )
        {
            int tableIndex = controller.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            if ( tableIndex >= ExpansionTable.Instance.Rows.Count )
                return null;
            return ExpansionTable.Instance.Rows[tableIndex];
        }

        public static XmlMod GetXmlModForController( ElementAbstractBase controller )
        {
            int tableIndex = controller.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;

            if ( tableIndex >= XmlModTable.Instance.Rows.Count )
                return null;

            return XmlModTable.Instance.Rows[tableIndex];
        }

        public class bMainContentParent : CustomUIAbstractBase
        {
            public static Transform ParentT;
            public static RectTransform ParentRT;
            public static bMainContentParent Instance;
            public override void OnUpdate()
            {
                if ( ParentT == null )
                {
                    Instance = this;
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
                if ( Window_SettingsMenu.Instance != null )
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

                btnCategoryPool.Clear( 5 );

                List<ArcenSettingCategory> categories = Instance.categories;
                foreach ( ArcenSettingCategory cat in categories )
                {
                    int countCurrent = 0;
                    int countMax = 0;
                    if ( cat.ShowExpansionsList )
                    {
                        foreach ( Expansion exp in ExpansionTable.Instance.Rows )
                        {
                            if ( !exp.IsDisabledBasedOnSettings && exp.IsInstalledAndEnabled && exp.IsInstalledAtAll )
                                countCurrent++;
                            countMax++;
                        }
                    }
                    if ( cat.ShowMainModsList )
                    {
                        foreach ( XmlMod mod in XmlModTable.Instance.Rows )
                        {
                            if ( mod.IsOn() )
                                countCurrent++;
                            countMax++;
                        }
                    }

                    bCategory item = btnCategoryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; //time slicing, too many added right now
                    item.Assign( cat, countCurrent, countMax );
                }

                #region Positioning Logic 1
                btnCategoryPool.ApplyItemsInRows( 10, ref currentY, 36, 218, 30);
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

            private ArcenSettingCategory Category = null;
            private int CountCurrent;
            private int CountOutOf;

            public void Assign( ArcenSettingCategory Cat, int CountCurrent, int CountOutOf )
            {
                this.Category = Cat;
                this.CountCurrent = CountCurrent;
                this.CountOutOf = CountOutOf;
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
                if ( this.CountOutOf > 0 )
                {
                    buffer.Add( " <size=90%>(" );
                    buffer.Add( this.CountCurrent );
                    buffer.Add( "/" );
                    buffer.Add( this.CountOutOf );
                    buffer.Add( ")</size>" );
                }
                buffer.AddDlcMod(this.Category, "由以下内容添加：");
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.Category == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                Instance.CurrentCategory = this.Category;
                Instance.isTempShowingAdvanced = false;
                //scroll right panel back to top when change category
                if ( bMainContentParent.Instance != null )
                    bMainContentParent.Instance.Element.TryScrollToTop();
                return MouseHandlingResult.None;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_SettingsMenu-bCategory-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( this.Category == null )
                    return;

                tooltipBuffer.Add( this.Category.DisplayName ).Add( "\n" ).Add( this.Category.Description );
                tooltipBuffer.AddDlcMod( this.Category,
                    "此个人设置分类由以下内容添加：" );
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }
        #endregion

        public class tSettingName : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                if ( !setting.GetIsTempValueMatchingDefault() )
                    buffer.StartColor( "ffde85" );
                buffer.Add( setting.DisplayName );
                buffer.EndColor();
                buffer.AddDlcMod(setting, "由以下内容添加：");
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_SettingsMenu-tSettingName-tooltipBuffer" );
            public override void HandleMouseover()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                tooltipBuffer.Add( setting.DisplayName ).Add( "\n" ).Add( setting.Description );
                tooltipBuffer.AddDlcMod(setting, "此设置由以下内容添加：" );
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }

        public class tExpansionName : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                Expansion expansion = GetExpansionForController( this );
                if ( expansion == null ) return;

                buffer.Add( "<size=60%>扩展包：</size> " ).StartColor( expansion.ColorForDisplay ).Add( expansion.DisplayName );
                if ( expansion.Abbreviation.Length > 0 )
                    buffer.Add( " (" ).Add( expansion.Abbreviation ).Add( ")" );
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_SettingsMenu-tExpansionName-tooltipBuffer" );
            public override void HandleMouseover()
            {
                Expansion expansion = GetExpansionForController( this );
                if ( expansion == null ) return;
                if ( expansion.DisplayName.Length > 0 )
                {
                    tooltipBuffer.Add( "<size=60%>扩展包：</size> " ).StartColor( expansion.ColorForDisplay ).Add( expansion.DisplayName );
                    if ( expansion.Abbreviation.Length > 0 )
                        tooltipBuffer.Add( " (" ).Add( expansion.Abbreviation ).Add( ")" );
                    tooltipBuffer.Add( "\n" );
                    tooltipBuffer.EndColor();
                    tooltipBuffer.Add( expansion.Description );

                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
                }
            }
        }

        public class tExpansionNotInstalled : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "未安装" );
            }

            public override void HandleMouseover()
            {
                Expansion expansion = GetExpansionForController( this );
                if ( expansion == null ) return;
                if ( expansion.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, expansion.DisplayName + "\n" + expansion.Description );
            }
        }

        public class tExpansionToggleDisabledDuringGame : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                Expansion expansion = GetExpansionForController( this );
                if ( expansion == null ) return;

                ArcenSetting setting = expansion.GetRelatedSetting();

                buffer.Add( !setting.TempValue_Bool ? "已启用" : "<color=#666666>已禁用" );
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_SettingsMenu-tExpansionToggleDisabledDuringGame-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.Add( "在游戏过程中无法启用或禁用扩展包。\n" );
                Expansion exp = GetExpansionForController( this );
                if ( exp != null )
                {
                    tooltipBuffer.AddTooltipFor(exp);
                }

                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }

        public class bExpansionToggle : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                Expansion expansion = GetExpansionForController( this );
                if ( expansion == null ) return;

                ArcenSetting setting = expansion.GetRelatedSetting();

                buffer.Add( !setting.TempValue_Bool ? "已启用" : "<color=#666666>已禁用" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Expansion expansion = GetExpansionForController( this );
                if ( expansion == null ) return MouseHandlingResult.None;

                ArcenSetting setting = expansion.GetRelatedSetting();

                setting.TempValue_Bool = !setting.TempValue_Bool;
                return MouseHandlingResult.None;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_SettingsMenu-bExpansionToggle-tooltipBuffer" );
            public override void HandleMouseover()
            {
                Expansion exp = GetExpansionForController( this );
                if ( exp != null )
                {
                    tooltipBuffer.AddTooltipFor(exp);
                }

                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }

        public class tXmlModName : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                XmlMod mod = GetXmlModForController( this );
                if ( mod == null ) return;

                if ( mod.GetIsTotalConversion() )
                    buffer.Add("<size=60%>T-C:</size> <pos=20>");
                else if ( mod.GetIsMiniDLC())
                    buffer.Add("<size=50%>DLC:</size> <pos=20>");
                else if ( mod.GetIsFeatured() )
                    buffer.Add("<size=50%>Feat:</size> <pos=20>");
                else
                    buffer.Add("<size=60%>Mod:</size>");
                buffer.StartColor( mod.GetColorForDisplay() ).Add( "<size=90%>").Add( mod.GetDisplayName() );
//                buffer.Add( mod.GetIsTotalConversion() ? "<size=60%>T-C:</size> <pos=20>" : "<size=60%>Mod:</size> <pos=20>" ).;
                if ( mod.GetAbbreviation().Length > 0 )
                    buffer.Add( " </size>(" ).Add( mod.GetAbbreviation() ).Add( ")" );
                buffer.EndColor();
                buffer.Add( "\n<pos=20><size=70%>作者：" ).Add( mod.GetAuthor() ).Add( " </size>" );
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_SettingsMenu-tXmlModName-tooltipBuffer" );
            public override void HandleMouseover()
            {
                XmlMod mod = GetXmlModForController( this );
                if ( mod == null ) return;

                tooltipBuffer.AddTooltipFor(mod);
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                var action = InputActionTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "HoldAndClickToViewDetailsOfContents" );
                if (action != null && 
                    action.CalculateValue() > 0.0f &&
                    input.LeftButtonClicked)
                {
                    XmlMod mod = GetXmlModForController( this );
                    if (mod != null &&
                        mod.ShortDescription != mod.Description)
                    {
                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Tall, null, mod.GetDisplayName(), mod.GetDescription(), "确定" );
                    }
                }

                return MouseHandlingResult.None;
            }
        }

        public class bXmlModToggle : ButtonAbstractBase
        {
            public XmlMod Mod
            {
                get
                {
                    return GetXmlModForController( this );
                }
            }

            public override void OnMainThreadUpdate()
            {
                base.OnMainThreadUpdate();
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                XmlMod mod = GetXmlModForController( this );
                if ( mod == null ) return;

                ArcenSetting setting = mod.GetRelatedSetting();
                bool ison = setting.TempValue_Bool;
                if (mod.IsTotalConversion)
                    ison = mod.IsOn();

                bool canchange = (Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.ModalMenu);
                if (canchange)
                {
                    if (ison)
                    {
                        buffer.Add( mod.IsTotalConversion ? "点击还原" : "已启用" );
                    }
                    else
                    {
                        buffer.Add( mod.IsTotalConversion ? "点击转换" : "已禁用", "666666" );
                    }
                }
                else
                {
                    buffer.Add( ison ? "已启用" : "<color=#666666>已禁用</color>" );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                XmlMod mod = GetXmlModForController( this );
                if ( mod == null ) 
                    return MouseHandlingResult.None;
                
                var action = InputActionTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "HoldAndClickToViewDetailsOfContents" );
                if (action != null && 
                    action.CalculateValue() > 0.0f &&
                    input.LeftButtonClicked)
                { 
                    if ( mod.ShortDescription != mod.Description )
                    {
                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Tall, null, mod.GetDisplayName(), mod.GetDescription(), "确定" );
                    }

                    return MouseHandlingResult.None;
                }

                bool canchange = (Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.ModalMenu);// && !mod.IsFrameworkMod);
                if (!canchange)
                {
                    return MouseHandlingResult.None;
                }

                if ( mod.IsOff() )
                {
                    if ( mod.RequiredExpansions.Count > 0 )
                    {
                        foreach ( Expansion exp in mod.RequiredExpansions )
                        {
                            if ( exp != null )
                            {
                                if (!exp.IsInstalledAndEnabled)
                                {
                                    string expansionList = string.Empty;
                                    foreach ( Expansion exp2 in mod.RequiredExpansions )
                                    {
                                        if ( expansionList.Length > 0 )
                                            expansionList += ", ";
                                        expansionList += exp2.DisplayName;

                                        if ( !exp2.IsInstalledAtAll )
                                            expansionList += " (未安装)";
                                        else
                                        {
                                            ArcenSetting set2 = exp2.GetRelatedSetting();
                                            if ( set2.TempValue_Bool )
                                                expansionList += " (未启用)";
                                        }
                                    }
                                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "无法启用Mod", "此Mod使用的所有扩展包，包括 " +
                                        expansionList + " 必须全部启用才能开启此Mod。", "确定" );

                                    return MouseHandlingResult.PlayClickDeniedSound;
                                }
                            }
                        }
                    }
                }

                if ( mod.GetIsTotalConversion() )
                {
                    ModalPopupData.CreateAndLogYesNoStyle( delegate
                    {
                        string totalConversionFile = Engine_Universal.CurrentRootApplicationDirectory + "PlayerData/" + "TotalConversionModOption.txt";

                        if ( System.IO.File.Exists( totalConversionFile ) )
                            System.IO.File.Delete( totalConversionFile );

                        if ( mod.IsOn() )
                        {
                            System.IO.File.AppendAllText( totalConversionFile, "//" + mod.InternalName );
                        }
                        else
                        {
                            System.IO.File.AppendAllText( totalConversionFile, mod.InternalName );
                        }

                        Application.Quit();

                    }, null, 
                    mod.IsOn() ? "结束完全转换？" : "开始完全转换？",
                    mod.IsOn() ?
                        "禁用此完全转换将使您返回普通游戏及您希望在此使用的任何非完全转换Mod。" +
                        "\n\n此操作将在点击'是'后立即执行，不会保存您在此处的其他设置。但是，此完全转换之前保存的所有设置和数据将为您保留。" +
                        "\n\n您可以随意切换进出完全转换。当您开启或关闭完全转换时，游戏将关闭，您需要手动重新启动游戏。但在您做出更改之前，游戏将无限期保持您上次请求的状态。"
                        :
                        "启用此完全转换将完全改变整个游戏，移除对大多数其他Mod的直接访问，仅使用此Mod中包含的新功能集。这类似于将半条命1变成反恐精英或胜利之日的规模变化。" +
                        "\n\n此操作将在点击'是'后立即执行，不会保存您在此处的其他设置。但是，基础游戏之前保存的所有设置和数据将为您保留，在完全转换中您将拥有完全独立的设置集。" +
                        "\n\n您可以随意切换进出完全转换。当您开启或关闭完全转换时，游戏将关闭，您需要手动重新启动游戏。但在您做出更改之前，游戏将无限期保持您上次请求的状态。",
                        "确认转换", "取消" );

                    //ArcenSetting setting = mod.GetRelatedSetting();
                    //setting.TempValue_Bool = !setting.TempValue_Bool;

                    return MouseHandlingResult.None;
                }

                var setting = mod.GetRelatedSetting();
                setting.TempValue_Bool = !setting.TempValue_Bool;

                return MouseHandlingResult.None;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_SettingsMenu-bXmlModToggle-tooltipBuffer" );
            public override void HandleMouseover()
            {
                XmlMod mod = GetXmlModForController( this );

                if ( mod != null )
                {
                    bool canchange = (Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.ModalMenu);
                    
                    if (!canchange)
                    {
                        tooltipBuffer.Add( "您无法在游戏中启用或禁用Mod。\n\n" );
                    }
                    else if ( mod.GetIsTotalConversion() )
                    {
                    tooltipBuffer.Add( "点击启用或禁用完全转换Mod将立即关闭整个游戏，不会保存此处的其他设置（您需要手动重新启动游戏）。\n\n" );
                    tooltipBuffer.Add( "重新启动游戏后，您将拥有完全不同的设置集，并且只会看到当前完全转换的子Mod。\n\n" );
                    tooltipBuffer.Add( "完全转换是绝对大规模的Mod,几乎替换了整个游戏，因此一次只能激活一个。但是，每个完全转换都可以像普通游戏一样进行Mod,因此可能有一些特定于它的Mod在主游戏中不存在。\n\n" );
                    }
                    else if (mod.IsFrameworkMod)
                    {
                        tooltipBuffer.Add( "这是一个框架Mod。如果需要，它将被其他Mod自动开启。\n\n" );
                    }

                    tooltipBuffer.AddTooltipFor(mod);
                }

                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }

        public class tSubSectionHeader : TextAbstractBase
        {
            public ArcenSettingSubcategory GetSubcategory()
            {
                string subCatName = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString;
                if ( subCatName != null ) {
                    return ArcenSettingSubcategoryTable.Instance.GetRowByName(subCatName);
                }
                return null;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                ArcenSettingSubcategory subCat = this.GetSubcategory();
                if ( subCat == null )
                    buffer.Add( "<size=125%>空子分类！" );
                else
                    buffer.Add( "<size=125%>" ).StartColor( subCat.Color ).Add( subCat.DisplayName );
            }

            public override void HandleMouseover()
            {
                ArcenSettingSubcategory subCat = this.GetSubcategory();
                if ( subCat == null )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, "空子分类！" );
                else
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, subCat.DisplayName + "\n" + subCat.Description );
            }
        }

        public class tPublicIPHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.StartColor( "ff974b" ).Add( "您的公网IP地址" );
            }

            public override void HandleMouseover()
            {
                string IPAddress = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString;
                iPublicIPBody.ShowTooltip( this.Element, IPAddress );
            }
        }

        public class iPublicIPBody : InputAbstractBase
        {
            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                return '\0'; //accept no inpit
            }
            public override void OnUpdate()
            {
                string IPAddress = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString;
                if ( GameSettings.Current.GetBoolBySetting( "HideIPAddressInLobbyAndEscMenu" ) )
                    IPAddress = "[悬停查看IP]";

                ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;
                elementAsType.SetText( IPAddress );
            }

            public static void ShowTooltip( ArcenUI_Element Element, string IPAddress )
            {
                string tooltip = "您的公网IP地址：" + IPAddress + "\n\n" +
                    "如果您计划使用Forge网络或类似方式作为多人游戏主机，您需要将此地址提供给将加入您游戏的其他玩家。" +
                    "这是假设其他玩家不在您同一位置，而是通过互联网连接到您。我们的软件将尝试与您的路由器配合" +
                    "以允许他们连接到您——这个过程称为'NAT穿透'——但这通常不太成功。\n\n如果这是您连接朋友的唯一方式（没有Steam或GOG），" +
                    "并且他们无法直接连接到此地址，那么您需要在路由器上设置'端口转发'（具体说明因硬件而异，但可以轻松在线搜索），或者" +
                    "您需要与朋友设置VPN,如Hamachi或Tunngle,然后您可以通过VPN软件中看到的新'本地'IP地址相互连接。这都是比较老且更困难的方式。";

                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, tooltip );
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
                string IPAddress = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString;
                iPublicIPBody.ShowTooltip( this.Element, IPAddress );
            }
        }

        public class tLocalIPHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.StartColor( "acff4b" ).Add( "本地IP地址" );
            }

            public override void HandleMouseover()
            {
                string IPAddress = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString;
                iLocalIPBody.ShowTooltip( this.Element, IPAddress );
            }
        }

        public class iLocalIPBody : InputAbstractBase
        {
            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                return '\0'; //accept no inpit
            }
            public override void OnUpdate()
            {
                string IPAddress = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString;
                if ( GameSettings.Current.GetBoolBySetting( "HideIPAddressInLobbyAndEscMenu" ) )
                    IPAddress = "[悬停查看IP]";

                ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;
                elementAsType.SetText( IPAddress );
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

            public static void ShowTooltip( ArcenUI_Element Element, string IPAddress )
            {
                string tooltip = "您的本地IP地址：" + IPAddress + "\n\n";

                if ( IPAddress.Contains( ":") )
                    tooltip +=
                        "此IP地址使用较新的IPV6格式！这可能不被大多数网络框架支持，也不是必需的。" +
                        "\n\nIPV6的长期目标是为全球更多的人和设备提供更多地址。这是一个好主意，大多数现代网卡都将其作为选项支持，但实际使用它的网络并不多。这从1995年就开始了，但直到2017年才被国际接受。即使在编写此提示的2020年，这也不是您需要担心的事情。";
                else
                    tooltip +=
                        "您可能有多个这样的地址！这可能是因为您的计算机上有无线网卡和有线连接，或者是因为您的机器上安装了一个或多个VPN。" +
                        "\n\n如果您计划使用Forge网络或类似方式作为多人游戏主机，并且您正在举办局域网聚会（所有玩家都在同一共享网络上），或者您正在使用VPN软件与远程玩家创建'虚拟局域网'，那么您需要将此处看到的其中一个地址提供给将加入您游戏的其他玩家。\n\n" +
                        "您应该提供哪个地址？这取决于您的连接方式。大多数真正的本地网络地址格式为10.x.x.x、192.168.x.x或172.16.x.x。如果您正在举办局域网聚会，您看到的可能是其中一个。这是与同一位置的人一起玩的最快最好方式，因为所有内容都不会经过互联网再返回——全部保留在您位置的网络上。\n\n" +
                        "如果您与朋友使用共享VPN（如Hamachi或Tunngle），那么地址可能有各种格式。实际上，由于某种原因它可能未列在此处（尽管应该列出）。大多数VPN软件会告诉您在VPN网络中'您的本地地址'是什么。如果您在此处看不到它，那就是您应该提供给其他玩家的地址。";

                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, tooltip );
            }

            public override void HandleMouseover()
            {
                string IPAddress = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString;
                iLocalIPBody.ShowTooltip( this.Element, IPAddress );
            }
        }

        public class tSettingValueDescription : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                switch ( setting.Type )
                {
                    case ArcenSettingType.BoolToggle:
                        break;
                    case ArcenSettingType.FloatSlider:
                        {
                            buffer.Add( "  " );
                            float tempValue = setting.TempValue_Float;
                            switch ( setting.RoundingType )
                            {
                                case ArcenSettingRoundingType.Tenths:
                                    buffer.AddFixedDecimal( tempValue, 1 );
                                    break;
                                case ArcenSettingRoundingType.Twentieths:
                                    buffer.AddFixedDecimal( tempValue, 2 );
                                    break;
                                case ArcenSettingRoundingType.None:
                                    buffer.Add( tempValue );
                                    break;
                            }
                            buffer.Add( "       " ).Add( FontSizes.SLIGHTLY_SMALLER_SIZE_V2_STRING ).Add( "<color=#999999>(" ).Add( setting.MinFloatValue );
                            if ( setting.MaxFloatValue > 0 && setting.MaxFloatValue > setting.MinFloatValue )
                                buffer.Add( " 至 " ).AddFixedDecimalThousands( setting.MaxFloatValue, 4 );
                            buffer.Add( ")" );
                        }
                        break;
                    case ArcenSettingType.IntSlider:
                        {
                            buffer.Add( "  " );
                            int tempValue = setting.TempValue_Int;
                            buffer.AddNumberMoreReadable( tempValue );
                            buffer.Add("       ").Add( FontSizes.SLIGHTLY_SMALLER_SIZE_V2_STRING ).Add("<color=#999999>(").Add( setting.MinIntValue );
                            if ( setting.MaxIntValue > 0 && setting.MaxIntValue > setting.MinIntValue )
                                buffer.Add( " 至 " ).AddNumberMoreReadable( setting.MaxIntValue );
                            buffer.Add( ")" );
                        }
                        break;
                    case ArcenSettingType.IntTextbox:
                        int valueAsInt;
                        if ( !Int32.TryParse( setting.TempValue_String, out valueAsInt ) )
                            buffer.Add( "  必须为整数。" );
                        else if ( valueAsInt < setting.MinIntValue )
                            buffer.Add( "  至少为 " ).Add( setting.MinIntValue );
                        else if ( setting.MaxIntValue > 0 && setting.MaxIntValue > setting.MinIntValue && valueAsInt > setting.MaxIntValue )
                            buffer.Add( "  最多为 " ).Add( setting.MaxIntValue );
                        break;
                }
            }

            public override void HandleMouseover()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, setting.Description );
            }
        }

        public class bToggle : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                buffer.Add( setting.TempValue_Bool ? "开" : "<color=#666666>关" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return MouseHandlingResult.None;

                setting.TempValue_Bool = !setting.TempValue_Bool;
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, setting.Description );
            }
        }

        public class iIntInput : InputAbstractBase
        {
            public override void HandleChangeInValue( string NewValue )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                int newValueAsInt;
                if ( !Int32.TryParse( NewValue, out newValueAsInt ) )
                    return;
                if ( newValueAsInt < setting.MinIntValue )
                    return;
                if ( setting.MaxIntValue > setting.MinIntValue && newValueAsInt > setting.MaxIntValue )
                    return;

                setting.TempValue_String = NewValue;
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

            public override void OnUpdate()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                //only update to the current value if we're not editing this field right now
                if ( !this.GetIsCurrentlyBeingEdited() )
                {
                    ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;
                    elementAsType.SetText( setting.TempValue_String );
                }
            }

            public override void HandleMouseover()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, setting.Description );
            }
        }

        public class sFloatSlider : SliderAbstractBase
        {
            public override void OnUpdate()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                float currentValue = setting.TempValue_Float;
                float range = setting.MaxFloatValue - setting.MinFloatValue;
                if ( range == 0 ) range = 1;
                float currentPortion = ( currentValue - setting.MinFloatValue ) / range;

                ArcenUI_Slider elementAsType = (ArcenUI_Slider)this.Element;
                elementAsType.ReferenceSlider.value = currentPortion;
            }

            public override void OnChange( float NewValue )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                float range = setting.MaxFloatValue - setting.MinFloatValue;
                float adjustedNewValue = setting.MinFloatValue + ( range * NewValue );

                setting.TempValue_Float = adjustedNewValue;
            }

            public override void HandleMouseover()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, setting.Description );
            }
        }

        public class sIntSlider : SliderAbstractBase
        {
            public override void OnUpdate()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                int currentValue = setting.TempValue_Int;
                int range = setting.MaxIntValue - setting.MinIntValue;
                if ( range == 0 ) range = 1;
                float currentPortion = (float)( currentValue - setting.MinIntValue ) / (float)range;

                ArcenUI_Slider elementAsType = (ArcenUI_Slider)this.Element;
                elementAsType.ReferenceSlider.value = currentPortion;
            }

            public override void OnChange( float NewValue )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                int range = setting.MaxIntValue - setting.MinIntValue;
                int adjustedNewValue = setting.MinIntValue + Mathf.RoundToInt( range * NewValue );

                setting.TempValue_Int = adjustedNewValue;
            }

            public override void HandleMouseover()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, setting.Description );
            }
        }

        public class dIntDropdown : DropdownAbstractBase
        {
            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                int newValue = elementAsType.IndexOfItem( Item );
                setting.TempValue_Int = newValue;
            }

            public override void HandleMouseover()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, setting.Description );
            }

            public override void OnUpdate()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;
                if ( elementAsType == null )
                    return;

                int tempValue = setting.TempValue_Int;
                if ( setting.DropdownFiller != null )
                {
                    tempValue = setting.DropdownFiller.GetTempValue( setting );

                    List<IArcenUI_Dropdown_Option> options = setting.DropdownFiller.GetListOfDropdownOptions();
                    if ( options == null )
                        elementAsType.ClearItems();
                    else if ( elementAsType.GetItemCount() != options.Count )
                    {
                        elementAsType.ClearItems();
                        for ( int i = 0; i < options.Count; i++ )
                            elementAsType.AddItem( options[i], i == tempValue );
                    }
                }

                if ( elementAsType.GetItemCount() > tempValue && tempValue != elementAsType.GetSelectedIndex() )
                    elementAsType.SetSelectedIndex( tempValue, DropdownSetType.FromExternalData );
            }

            #region TextOption
            private class TextOption : IArcenUI_Dropdown_Option
            {
                private readonly string Item;

                public TextOption( string Item )
                {
                    this.Item = Item;
                }

                public object GetItem()
                {
                    return this.Item;
                }

                public string GetOptionNameFromVolatile()
                {
                    return this.Item;
                }

                public Sprite GetOptionSprite()
                {
                    return null;
                }
            }
            #endregion
        }

        public class bCancel : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();

                World_AIW2.Instance.DoOnSpecialEvent_OnMainThread_ClientOrHost_ForAllFactionsAndExternalWorldBaseInfo( SpecialEventType.PersonalSettingsCanceled, Engine_AIW2.Instance.MainThreadContext_ClientOrHost );
                return MouseHandlingResult.None;
            }
        }

        public class bSave : ButtonAbstractBase
        {
            public static bSave Instance;
            public bSave()
            {
                Instance = this;
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                bool willRequireReload = false;
                StringBuilder reasonsForReload = new StringBuilder();
                //Enabled Expansions
                for ( int i = 0; i < ExpansionTable.Instance.Rows.Count; i++ )
                {
                    Expansion expansion = ExpansionTable.Instance.Rows[i];
                    ArcenSetting setting = expansion.GetRelatedSetting();
                    if ( setting.TempValue_Bool != GameSettings.Current.GetBoolBySetting( setting ) )
                    {
                        willRequireReload = true;
                        reasonsForReload.Append( "扩展「" ).Append( expansion.DisplayName ).Append( "」" ).Append( setting.TempValue_Bool ? "已禁用" : "已启用" ).Append( "\n" );
                    }
                }

                //Mods
                for ( int i = 0; i < XmlModTable.Instance.Rows.Count; i++ )
                {
                    XmlMod mod = XmlModTable.Instance.Rows[i];
                    ArcenSetting setting = mod.GetRelatedSetting();
                    if ( setting.TempValue_Bool != GameSettings.Current.GetBoolBySetting( setting ) )
                    {
                        willRequireReload = true;
                        reasonsForReload.Append( "模组「" ).Append( mod.DisplayName ).Append( "」" ).Append( setting.TempValue_Bool ? "已启用" : "已禁用" ).Append( "\n" );
                    }
                }

                if ( willRequireReload )
                {
                    ModalPopupData.CreateAndLogYesNoStyle( delegate
                    {
                        if ( Engine_Universal.IsReloadingAllXml )
                            return; //we clicked it a second time somehow

                        //save the settings
                        ArcenSettingTable.Instance.CopyTempValuesToCurrent();
                        GameSettings.SaveToDisk();
                        GameSettings.Current.DoGraphicsSettingsNeedARefresh = true;

                        //force the windows here to close, rather ungracefully
                        Window_SettingsMenu.Instance.IsOpen = false;
                        ArcenUI.Instance.OnMainThreadUpdate();
                        ArcenUI.Instance.OnUpdateFromMainThread();

                        //do the hot-reload
                        Engine_Universal.ReloadXmlDataAsMuchAsIsAllowed();

                        World_AIW2.Instance.DoOnSpecialEvent_OnMainThread_ClientOrHost_ForAllFactionsAndExternalWorldBaseInfo( SpecialEventType.PersonalSettingsSavedAndApplied, Engine_AIW2.Instance.MainThreadContext_ClientOrHost );

                    }, null, 
                    "重新加载数据文件？", 
                    "您的更改需要应用程序重新加载大量数据文件（大多数内容需要清除并使用您新选择的扩展包和Mod组合重新加载）。如果导致任何错误，请手动关闭并重新启动游戏，一切应该正常（除非Mod确实损坏）。\n\n以下是导致需要数据重新加载的更改：\n\n" +
                        reasonsForReload, "继续并重新加载", "取消" );
                }
                else
                {
                    ArcenSettingTable.Instance.CopyTempValuesToCurrent();
                    GameSettings.SaveToDisk();
                    GameSettings.Current.DoGraphicsSettingsNeedARefresh = true;
                    World_AIW2.Instance.DoOnSpecialEvent_OnMainThread_ClientOrHost_ForAllFactionsAndExternalWorldBaseInfo( SpecialEventType.PersonalSettingsSavedAndApplied, Engine_AIW2.Instance.MainThreadContext_ClientOrHost );
                    Window_SettingsMenu.Instance.Close();
                }
                return MouseHandlingResult.None;
            }
        }

        public class bSetDefaults : ButtonAbstractBase
        {
            public override void DoAnyCustomButtonStuffFromVolatile( ArcenUI_Button Button )
            {
                Button.UseGetTextToShowFromAnyThread = true;

                base.DoAnyCustomButtonStuffFromVolatile( Button );
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );

                if (Window_SettingsMenu.Instance.CurrentCategory != null)
                {
                    if (Window_SettingsMenu.Instance.CurrentCategory.InternalName.Equals("Mods"))
                        Buffer.Add("全部禁用");
                    else
                        Buffer.Add("恢复默认");
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if (Window_SettingsMenu.Instance.CurrentCategory.InternalName.Equals("Mods"))
                {
                    foreach (var row in XmlModTable.Instance.Rows)
                    {
                        var setting = row.GetRelatedSetting();
                        setting.TempValue_Bool = setting.DefaultBoolValue;
                        
                    }

                    bSave.Instance.HandleClick(MouseHandlingInput.Empty);

                    return MouseHandlingResult.None;
                }
                else
                {
                    int rowCount = Window_SettingsMenu.Instance.CurrentCategory.VisibleRows_All.Count;
                    for ( int i = 0; i < rowCount; i++ )
                    {
                        ArcenSetting setting = ArcenSettingTable.Instance.VisibleRows_All[i];
                        if ( setting.RowFromExpansion != null && setting.RowFromExpansion.IsDisabledBasedOnSettings )
                            continue; //if from disabled expansion
                        if ( setting.RowFromXmlMod != null && setting.RowFromXmlMod.IsOff() )
                            continue; //if from disabled mod

                        //ArcenDebugging.ArcenDebugLogSingleLine(string.Format("ArcenSetting {0} of type {1}", setting.InternalName, setting.Type), Verbosity.DoNotShow);

                        switch ( setting.Type )
                        {
                            case ArcenSettingType.BoolToggle:
                                setting.TempValue_Bool = setting.DefaultBoolValue;
                                break;
                            case ArcenSettingType.FloatSlider:
                                setting.TempValue_Float = setting.DefaultFloatValue;
                                break;
                            case ArcenSettingType.IntSlider:
                                setting.TempValue_Int = setting.DefaultIntValue;
                                break;
                            case ArcenSettingType.IntTextbox:
                                setting.TempValue_Int = setting.DefaultIntValue;
                                setting.TempValue_String = setting.DefaultIntValue.ToString();
                                break;
                        }
                    }
                    return MouseHandlingResult.None;
                }
            }
        }

        public override void Close()
        {
            ArcenSettingTable.Instance.CopyCurrentValuesToTemp(); // shouldn't matter, but tidies things up
            base.Close();
        }

        //from IInputActionHandler
        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.InternalName )
            {
                case "OpenSystemMenu":
                case "Return":
                    this.Close();
                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
            }
        }
    }

    public static partial class Extensions
    {
        public static void AddTooltipFor( this ArcenCharacterBufferBase buffer, Expansion exp )
        {
            buffer.Add( "<size=60%>扩展包：</size> " ).StartColor( exp.ColorForDisplay ).Add( exp.DisplayName );
                    if ( exp.Abbreviation.Length > 0 )
                        buffer.Add( " (" ).Add( exp.Abbreviation ).Add( ")" );
                    buffer.Add( "\n" );
                    buffer.EndColor();
                    buffer.Add( exp.Description, "eeeeee" );

            buffer.EndColor();
            buffer.Add("\n");
        }

        public static void AddTooltipFor( this ArcenCharacterBufferBase buffer, XmlMod mod )
        {
            buffer
                .Add( mod.GetIsTotalConversion() ? "<size=60%>完全转换Mod：</size> " : "<size=60%>Mod：</size> " )
                .StartColor( mod.GetColorForDisplay() )
                .Add( mod.GetDisplayName() );
            if ( mod.GetAbbreviation().Length > 0 )
                buffer.Add( " (" ).Add( mod.GetAbbreviation() ).Add( ")" );

            buffer.EndColor();
            buffer.Add("\n");

            buffer.Add("<pos=4><size=80%>作者：" ).Add( mod.GetAuthor() ).Add( "\n</size>" );

            if (mod.RequiredExpansions.Count > 0 || mod.RequiredMods.Count > 0)
            {
               buffer.Add("<pos=4><size=60%>使用 ");

                if ( mod.RequiredExpansions.Count > 0)
                {
                    for (int i = 0; i < mod.RequiredExpansions.Count; i++)
                    {
                        var other = mod.RequiredExpansions[i];
                            
                        buffer
                            .Add( "<size=50%>")
                            .Add( other.Abbreviation, other.ColorForDisplay )
                            .Add( "</size>" )
                            .Add( " " )
                            ;
                    }
                }
                if (mod.RequiredMods.Count > 0)
                {
                    for (int i = 0; i < mod.RequiredMods.Count; i++)
                    {
                        var other = mod.RequiredMods[i];
                            
                        buffer
                            .Add( "<size=50%>")
                            .Add( other.Abbreviation, other.ColorForDisplay )
                            .Add( "</size>" )
                            .Add( " " )
                            ;
                    }
                }

                buffer.Add("</size>");
                buffer.Add("\n");
            }
            
            buffer
                .Add( "\n" )
                .Add( mod.GetShortDescription(), "eeeeee" )
                .Add( "\n" )
                ;
            
            string hasmore = null;
            if (mod.GetShortDescription() != mod.GetDescription())
                hasmore = "完整版";

            EntityText.Write_Tooltip_Hotkeys_Footer(buffer, false, false, hasmore);
        }
    }
}
