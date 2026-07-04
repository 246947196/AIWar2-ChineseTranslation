using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_SetupMapTabLeft : Window_SetupTabWindowBase
    {
        public static Window_SetupMapTabLeft Instance;

        public Window_SetupMapTabLeft()
        {
            Instance = this;
            this.topBuffer = 3;
            this.leftBuffer = 5;
            this.rowHeight = 24;
            this.rowBuffer = 1.5f;

            this.PreventsNormalInputHandlers = true;

            this.topBuffer = 3;
            this.leftBuffer = 2;
            this.rowHeight = 24;
            this.rowBuffer = 1.5f;
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
            if ( Window_SetupTopTabs.Current != LobbyTabType.Map )
                return false;
            return true;
        }
        #endregion

        public static float ScreenSpaceRight;

        #region UI Control Types

        #region General

        #region custSetupWindowScrollingStuffOnLeft
        public class custSetupWindowScrollingStuffOnLeft : CustomUIAbstractBase
        {
            public override void OnUpdate()
            {
                ScreenSpaceRight = ArcenUI.Instance.guiCamera.WorldToScreenPoint( this.Element.RelevantRect.GetWorldSpaceTopRightCorner() ).x;
                //ArcenDebugging.ArcenDebugLogSingleLine( this.Element.RelevantRect + " rect " +  this.Element.RelevantRect.GetWorldSpaceTopRightCorner() + " right " + ScreenSpaceRight, Verbosity.DoNotShow );
            }
        }
        #endregion
        
        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "游戏设置 - 地图选项" );
            }
        }
        #endregion

        #region PopulateSubclassControls
        private float fullWidth = 200;
        private float leftButtonWidth = 100;
        private float rightButtonWidth = 100;
        private float spacerSize_Small = 8f;
        private float spacerSize_Medium = 12f;

        //private float spacerSize_Large = 40f;

        private const float SMALLER_ROW_FONT_SIZE = 11f;
        private const float SMALLER_ROW_HEADER_HEIGHT = 16f;
        private const float SMALLER_ROW_DROPDOWN_HEIGHT = 21f;
        private const float FULL_ROW_DROPDOWN_HEIGHT = 24f;
        private const float SMALLER_ROW_HEADER_SPACING = 0.1f;
        private const float SMALLER_ROW_DROPDOWN_SPACING = 0.2f;
        private const float FULL_ROW_DROPDOWN_SPACING = 0.5f;

        private ArcenCachedExternalTypeDirect type_tMapTypeHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tMapTypeHeader ) );
        private ArcenCachedExternalTypeDirect type_dMapType = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( dMapType ) );
        private ArcenCachedExternalTypeDirect type_tPlanetNameTypeHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tPlanetNameTypeHeader ) );
        private ArcenCachedExternalTypeDirect type_dPlanetNameType = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( dPlanetNameType ) );
        private ArcenCachedExternalTypeDirect type_tNumPlanetsHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tNumPlanetsHeader ) );
        private ArcenCachedExternalTypeDirect type_dIntNumPlanets = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( dIntNumPlanets ) );
        private ArcenCachedExternalTypeDirect type_tSeedHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tSeedHeader ) );
        private ArcenCachedExternalTypeDirect type_bRandomSeed = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bRandomSeed ) );
        private ArcenCachedExternalTypeDirect type_bPriorSeed = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bPriorSeed ) );
        private ArcenCachedExternalTypeDirect type_iSeed = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( iSeed ) );
        private ArcenCachedExternalTypeDirect type_bRegenerateMap = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bRegenerateMap ) );

        private ArcenCachedExternalTypeDirect type_tSettingName = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tSettingName ) );
        private ArcenCachedExternalTypeDirect type_BoolToggle = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( BoolToggle ) );
        private ArcenCachedExternalTypeDirect type_IntBasedCustomDropdown = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( IntBasedCustomDropdown ) );
        private ArcenCachedExternalTypeDirect type_IntBasedSlider = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( IntBasedSlider ) );

        protected override void PopulateSubclassControls( ArcenUI_SetOfCreateElementDirectives Set, ref float runningY )
        {

            Rect leftBounds;
            Rect rightBounds;

            //Map Type Header
            this.CalculateBoundsSingleCustomHeight( out leftBounds, ref runningY, fullWidth, SMALLER_ROW_HEADER_HEIGHT, SMALLER_ROW_HEADER_SPACING );
            AddText( Set, type_tMapTypeHeader, string.Empty, -1, -1, leftBounds, SMALLER_ROW_FONT_SIZE );
            //Map Type Dropdown
            this.CalculateBoundsSingleCustomHeight( out leftBounds, ref runningY, fullWidth, FULL_ROW_DROPDOWN_HEIGHT, FULL_ROW_DROPDOWN_SPACING );
            AddDropdown( Set, type_dMapType, string.Empty, -1, -1, leftBounds, -1f );

            this.AddMapTypeSpecificControls( Set, ref runningY );

            //Spacer
            runningY += spacerSize_Medium;

            //Planet Name Type Header
            this.CalculateBoundsSingleCustomHeight( out leftBounds, ref runningY, fullWidth, SMALLER_ROW_HEADER_HEIGHT, SMALLER_ROW_HEADER_SPACING );
            AddText( Set, type_tPlanetNameTypeHeader, string.Empty, -1, -1, leftBounds, SMALLER_ROW_FONT_SIZE );
            //Planet Name Type Dropdown
            this.CalculateBoundsSingleCustomHeight( out leftBounds, ref runningY, fullWidth, SMALLER_ROW_DROPDOWN_HEIGHT, SMALLER_ROW_DROPDOWN_SPACING );
            AddDropdown( Set, type_dPlanetNameType, string.Empty, -1, -1, leftBounds, -1f );
            
            //Number of Planets Header
            this.CalculateBoundsSingleCustomHeight( out leftBounds, ref runningY, fullWidth, SMALLER_ROW_HEADER_HEIGHT, SMALLER_ROW_HEADER_SPACING );
            AddText( Set, type_tNumPlanetsHeader, string.Empty, -1, -1, leftBounds, SMALLER_ROW_FONT_SIZE );
            //Number of Planets Slider
            this.CalculateBoundsSingleCustomHeight( out leftBounds, ref runningY, fullWidth, FULL_ROW_DROPDOWN_HEIGHT, FULL_ROW_DROPDOWN_SPACING );
            AddDropdown( Set, type_dIntNumPlanets, string.Empty, -1, -1, leftBounds, -1f );

            //Spacer
            runningY += spacerSize_Small;
            
            //Map Seed Header, and Randomize Seed
            this.CalculateBoundsDualCustomHeight( out leftBounds, out rightBounds, ref runningY, leftButtonWidth, rightButtonWidth, SMALLER_ROW_DROPDOWN_HEIGHT, SMALLER_ROW_DROPDOWN_SPACING );

            float farRightWidth = 40;
            leftBounds.width -= (farRightWidth + 1);
            AddText( Set, type_tSeedHeader, string.Empty, -1, -1, leftBounds, SMALLER_ROW_FONT_SIZE );

            rightBounds.x -= (farRightWidth + 1);
            Rect farRightBounds = new Rect( rightBounds.xMax + 2, rightBounds.y, farRightWidth, rightBounds.height );

            AddButton( Set, type_bRandomSeed, string.Empty, -1, -1, rightBounds, -1f );
            AddButton( Set, type_bPriorSeed, string.Empty, -1, -1, farRightBounds, -1f );

            //Map Seed Textbox
            this.CalculateBoundsSingleCustomHeight( out leftBounds, ref runningY, fullWidth, FULL_ROW_DROPDOWN_HEIGHT, FULL_ROW_DROPDOWN_SPACING );
            AddInput( Set, type_iSeed, string.Empty, -1, -1, leftBounds );

            //Spacer
            runningY += spacerSize_Small;

            //Regenerate Map
            this.CalculateBoundsSingleCustomHeight( out leftBounds, ref runningY, fullWidth, SMALLER_ROW_DROPDOWN_HEIGHT, SMALLER_ROW_DROPDOWN_SPACING );
            AddButton( Set, type_bRegenerateMap, string.Empty, -1, -1, leftBounds, -1f );
        }
        #endregion

        #region tMapTypeHeader
        public class tMapTypeHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "地图类型" );
            }
        }
        #endregion

        #region dMapType
        public class dMapType : DropdownAbstractBase
        {
            public static dMapType Instance;
            public dMapType()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;
                WorldSetup setupToViewOnly = World_AIW2.Instance.Setup;
                if ( setupToViewOnly == null )
                    return;

                MapTypeData ItemAsType = (MapTypeData)Item.GetItem();
                if ( ItemAsType == null )
                {
                    // "Random Galaxy Type" was selected — send a special "Random" command; the actual type is hidden from the player
                    if ( setupToViewOnly.MapConfig.UseRandomMapType )
                        return;
                    GameCommand randomCommand = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    randomCommand.RelatedString = "MapType";
                    randomCommand.RelatedString2 = "Random";
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), randomCommand, true );
                    return;
                }

                //don't set this again if it was already the same
                if ( !setupToViewOnly.MapConfig.UseRandomMapType && setupToViewOnly.MapConfig.MapType == ItemAsType )
                    return;

                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "MapType";
                command.RelatedString2 = ItemAsType.InternalName;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                WorldSetup setupToViewOnly = World_AIW2.Instance.Setup;
                bool useRandom = setupToViewOnly.MapConfig.UseRandomMapType;
                MapTypeData typeDataToSelect = useRandom ? null : setupToViewOnly.MapConfig.MapType;
                int harshness = World_AIW2.Instance.CampaignType.HarshnessRating;

                bool dlc4RandomEnabled = ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled();
                int randomOffset = dlc4RandomEnabled ? 1 : 0;

                bool foundMismatch = false;
                if ( useRandom )
                {
                    // Random option should be selected at index 0
                    if ( !dlc4RandomEnabled || elementAsType.GetItemCount() == 0
                        || !(elementAsType.GetItems_DoNotAlterDirectly()[0] is DropdownOptionMapTypeRandom)
                        || elementAsType.CurrentlySelectedOption == null
                        || !(elementAsType.CurrentlySelectedOption is DropdownOptionMapTypeRandom) )
                        foundMismatch = true;
                }
                else if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (MapTypeData)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
                {
                    foundMismatch = true;
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.InternalName, Verbosity.DoNotShow );
                }
                if ( !foundMismatch )
                {
                    if ( dlc4RandomEnabled )
                    {
                        if ( elementAsType.GetItemCount() == 0 || !(elementAsType.GetItems_DoNotAlterDirectly()[0] is DropdownOptionMapTypeRandom) )
                            foundMismatch = true;
                    }
                    if ( !foundMismatch )
                    {
                        for ( int i = 0; i < MapTypeDataTable.Instance.NormalNonHiddenMaps.Count; i++ )
                        {
                            MapTypeData row = MapTypeDataTable.Instance.NormalNonHiddenMaps[i];

                            if ( elementAsType.GetItemCount() <= i + randomOffset )
                            {
                                foundMismatch = true;
                                break;
                            }
                            IArcenUI_Dropdown_Option option = elementAsType.GetItems_DoNotAlterDirectly()[i + randomOffset];
                            MapTypeData optionItemAsType = (MapTypeData)option.GetItem();
                            if ( row == optionItemAsType )
                                continue;
                            foundMismatch = true;
                            break;
                        }
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    if ( dlc4RandomEnabled )
                        elementAsType.AddItem( new DropdownOptionMapTypeRandom(), useRandom );

                    for ( int i = 0; i < MapTypeDataTable.Instance.NormalNonHiddenMaps.Count; i++ )
                    {
                        MapTypeData row = MapTypeDataTable.Instance.NormalNonHiddenMaps[i];
                        if ( harshness > row.HiddenFromListsAboveHarshness )
                            continue;
                        if ( harshness < row.HiddenFromListsBelowHarshness )
                            continue;
                        DropdownOptionMapTypeData option = new DropdownOptionMapTypeData( row );
                        elementAsType.AddItem( option, row == typeDataToSelect );
                    }
                }
            }
            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window-SetupMapTabLeft-dMapType-tooltipBuffer" );
            public override void HandleMouseover()
            {
                string mouseoverText = "地图类型决定了银河系的形状，但不决定其填充方式。";
                WorldSetup setupToViewOnly = World_AIW2.Instance.Setup;
                tooltipBuffer.Add( "地图类型决定了银河系的形状，但不决定其填充方式。" );
                if ( setupToViewOnly != null && setupToViewOnly.MapConfig.MapType != null )
                {
                    //the specific option under mouse
                    this.FillMouseoverPartsForItem( true, setupToViewOnly.MapConfig.MapType );
                }
                //now draw it
                this.FinishAndDisplayTooltipFromBuffer( this.Element );
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, mouseoverText );
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenUI_Dropdown_Option Item )
            {
                MapTypeData ItemAsType = (MapTypeData)Item.GetItem();

                tooltipBuffer.Add( "地图类型决定了银河系的形状，但不决定其填充方式。" );
                if ( ItemAsType == null )
                {
                    tooltipBuffer.Add( "\n\n<b><color=#ffc87a>随机银河系类型</color></b>\n选择此选项时随机选择银河系类型。" );
                }
                else
                {
                    //the specific option under mouse
                    this.FillMouseoverPartsForItem( true, ItemAsType );
                }
                //now draw it
                this.FinishAndDisplayTooltipFromBuffer( this.Element );
            }

            public bool FillMouseoverPartsForItem( bool AddDoubleNewline, MapTypeData OptionValue )
            {
                if ( OptionValue == null )
                    return false;

                if ( AddDoubleNewline )
                    tooltipBuffer.Add( "\n\n" );

                tooltipBuffer.Add( "<b><color=#ffc87a>" ).Add( OptionValue.GetDisplayName() ).Add( "</color></b>\n" ).Add( OptionValue.AddDescription );
                
                tooltipBuffer.AddDlcMod(OptionValue, "This map type was added by" );
                return true;
            }

            public void FinishAndDisplayTooltipFromBuffer( IArcenUIElementForSizing Element )
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }
        #endregion

        #region Custom Map Controls

        /// <summary>
        /// Chris notes: Okay... so all of these settings are not stored in the WorldSetup object at all, but rather are in ArcenSettings itself.
        ///              This... probably will be problematic for multiplayer at some point, but it's not a super ultra crisis... I guess?
        ///              Further thinking: so long as the settings are consistent between players it will now be fine.
        ///              The actual data is properly synced for multiplayer, so it's just a matter of any custom settings someone adds.  Those need to be consistent between players.
        ///              
        /// </summary>
        private void AddMapTypeSpecificControls( ArcenUI_SetOfCreateElementDirectives Set, ref float runningY )
        {
            Rect rect;

            bool localDebug = false;
            WorldSetup setupToViewOnly = World_AIW2.Instance.Setup;
            MapTypeData mapType = setupToViewOnly.MapConfig.MapType;

            string mapName = mapType.InternalName;
            if ( localDebug ) ArcenDebugging.ArcenDebugLogSingleLine( "Window_MapSetup: Map type is " + mapName, Verbosity.DoNotShow );

            List<MapSettingOption> mapOptions = mapType.Options;
                        
            float settingNameFontSize = SMALLER_ROW_FONT_SIZE - 1f;
            float settingOptionFontSize = 14f;

            for ( int i = 0; i < mapOptions.Count; i++ )
            {
                var mapOption = mapOptions[i];

                if ( localDebug ) ArcenDebugging.ArcenDebugLogSingleLine( "Window_MapSetup: handling " + mapOption.InternalName, Verbosity.DoNotShow );

                this.CalculateBoundsSingleCustomHeight( out rect, ref runningY, fullWidth, SMALLER_ROW_HEADER_HEIGHT, SMALLER_ROW_HEADER_SPACING );
                AddText( Set, type_tSettingName, mapOption.InternalName, i, -1, string.Empty, rect, settingNameFontSize );

                this.CalculateBoundsSingleCustomHeight( out rect, ref runningY, fullWidth, SMALLER_ROW_DROPDOWN_HEIGHT, SMALLER_ROW_DROPDOWN_SPACING );

                switch ( mapOption.Type )
                {
                    case MapSettingOptionType.OldSchool:
                        {
                            AddDropdown( Set, type_IntBasedCustomDropdown, mapOption.InternalName, i, -1, rect, settingOptionFontSize );
                            break;
                        }
                    case MapSettingOptionType.BoolToggle:
                        {
                            AddButton( Set, type_BoolToggle, mapOption.InternalName, i, -1, rect, settingOptionFontSize );
                            break;
                        }
                    case MapSettingOptionType.CustomDropdownArbitraryOptions:
                        {
                            AddDropdown( Set, type_IntBasedCustomDropdown, mapOption.InternalName, i, -1, rect, settingOptionFontSize );
                            break;
                        }
                    case MapSettingOptionType.CustomSliderArbitraryOptions:
                        {
                            AddHorizontalSlider( Set, type_IntBasedSlider, mapOption.InternalName, i, -1, rect );
                            break;
                        }
                }
            }
        }

        public class tSettingName : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                MapSettingOption setting = GetMapSettingOptionForController( this );
                if ( setting == null ) return;

                var choice = GetMapSettingChoiceForController(this);
                if (choice == null)
                    return;

                buffer.StartColor( "8ba0e1" );
                buffer.Add( setting.DisplayName );
                buffer.Add( ": ");
                buffer.EndColor();
                buffer.StartColor( "5b7ce7" ).Add( "<size=80%>" ).Add( choice.DisplayName );
                buffer.Add("</size>" ).EndColor();
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window-SetupMapTabLeft-tSettingName-tooltipBuffer" );
            public override void HandleMouseover()
            {
                MapSettingOption setting = GetMapSettingOptionForController( this );
                if ( setting == null ) return;

                tooltipBuffer.Add( setting.DisplayName ).Add( "\n" ).Add( setting.Description );
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }

        #endregion

        #region tNumPlanetsHeader
        public class tNumPlanetsHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "期望行星数" );
                if ( World_AIW2.Instance.CurrentGalaxy != null )
                {
                    int planetCount = World_AIW2.Instance.CurrentGalaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise();
                    buffer.Add( "<size=80%> （实际 " ).Add( planetCount ).Add( "）</size>" );
                }
                else
                    buffer.Add( "<size=80%> （空银河系）</size>" );
            }
        }
        #endregion

        #region dIntNumPlanets
        public class dIntNumPlanets : DropdownAbstractBase
        {
            public static dIntNumPlanets Instance;
            public dIntNumPlanets()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;
                WorldSetup setupToViewOnly = World_AIW2.Instance.Setup;
                if ( setupToViewOnly == null )
                    return;

                int adjustedNewValue = Convert.ToInt32( Item.GetItem().ToString() );
                int currentValue = setupToViewOnly.MapConfig.GetClampedNumberOfPlanetsForMapType( setupToViewOnly.MapConfig.MapType );
                //don't set this again if it was already the same
                if ( adjustedNewValue == currentValue )
                    return;

                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "iNumPlanets";
                command.RelatedIntegers.Add( adjustedNewValue );
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
            }

            private int lastMin = -1;
            private int lastMax = -1;
            private int lastCurrent = -1;

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;
                
                WorldSetup setupToViewOnly = World_AIW2.Instance.Setup;

                int currentValue = setupToViewOnly.MapConfig.GetClampedNumberOfPlanetsForMapType( setupToViewOnly.MapConfig.MapType );

                int minPlanets = setupToViewOnly.MapConfig.MapType.GetMinPlanets();
                int maxPlanets = setupToViewOnly.MapConfig.MapType.GetMaxPlanets();

                if ( lastMin != minPlanets || lastMax != maxPlanets || currentValue != lastCurrent )
                {
                    lastMin = minPlanets;
                    lastMax = maxPlanets;
                    lastCurrent = currentValue;
                    elementAsType.ClearItems();

                    int range = maxPlanets - minPlanets;

                    for ( int i = minPlanets; i <= maxPlanets; i++ )
                    {
                        if ( range > 10 )
                        {
                            if ( i <= 50 )
                            {
                                if ( i % 2 != 0 )
                                    continue; //every even number if under 50
                            }
                            else if ( i <= 120 )
                            {
                                if ( i % 5 != 0 )
                                    continue; //every fifth number if under 120
                            }
                            else
                            {
                                if ( i % 10 != 0 )
                                    continue; //every tength number if above 100
                            }
                        }

                        DropdownOptionPlanetCountData option = new DropdownOptionPlanetCountData( i );
                        elementAsType.AddItem( option, i == currentValue );
                    }
                }
            }
            public override void HandleMouseover()
            {
                WorldSetup setupToViewOnly = World_AIW2.Instance.Setup;
                int currentValue = setupToViewOnly.MapConfig.GetClampedNumberOfPlanetsForMapType( setupToViewOnly.MapConfig.MapType );
                string mouseoverText = "选择银河系中的行星数量。当前值：<color=#7ab9ff>" + currentValue +
                    "</color>\n\n对于地图类型" + setupToViewOnly.MapConfig.MapType.GetDisplayName() +
                    "，数量必须在" + setupToViewOnly.MapConfig.MapType.GetMinPlanets() + "到" + setupToViewOnly.MapConfig.MapType.GetMaxPlanets() + "之间。";
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, mouseoverText );
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenUI_Dropdown_Option Item )
            {
                int ItemAsType = (int)Item.GetItem();
                if ( ItemAsType <= 50 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( ItemElement, "选择银河系中的行星数量。\n\n拥有<color=#ffc87a>" + ItemAsType + "</color>颗行星的地图在有多个派系时会相当拥挤。" );
                else if ( ItemAsType <= 70 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( ItemElement, "选择银河系中的行星数量。\n\n拥有<color=#ffc87a>" + ItemAsType + "</color>颗行星的地图较小，节奏稍快，但不算微小。" );
                else if ( ItemAsType <= 100 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( ItemElement, "选择银河系中的行星数量。\n\n拥有<color=#ffc87a>" + ItemAsType + "</color>颗行星的地图处于平均范围，应该有足够的空间容纳大多数场景。" );
                else if ( ItemAsType <= 120 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( ItemElement, "选择银河系中的行星数量。\n\n拥有<color=#ffc87a>" + ItemAsType + "</color>颗行星的地图较大，在某些机器上可能运行稍慢，但有额外空间容纳更多派系。" );
                else if ( ItemAsType <= 170 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( ItemElement, "选择银河系中的行星数量。\n\n拥有<color=#ffc87a>" + ItemAsType + "</color>颗行星的地图非常大，在大多数CPU和较差网络上很可能会运行较慢，但有额外空间容纳更多派系。如果在您的机器上运行不佳，我们可能无法提供帮助。游戏针对较低行星数进行了优化。但如果您有很棒的配置，不妨试试！" );
                else if ( ItemAsType <= 210 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( ItemElement, "选择银河系中的行星数量。\n\n拥有<color=#ffc87a>" + ItemAsType + "</color>颗行星的地图极其大，在许多CPU和大多数网络上几乎肯定运行较慢，但有额外空间容纳更多派系。也许最适合超级计算机上的单人游戏。如果在您的机器上运行不佳，我们可能无法提供帮助。游戏针对较低行星数进行了优化。但如果您有很棒的配置，不妨试试！" );
                else
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( ItemElement, "选择银河系中的行星数量。\n\n拥有<color=#ffc87a>" + ItemAsType + "</color>颗行星的地图绝对荒谬地巨大，除非您有一台非常强大的计算机并且可能在单人游戏或局域网上与少数玩家一起玩，否则运行会很慢。但嘿，这么多空间给派系！如果在您的机器上运行不佳，我们可能无法提供帮助。游戏针对较低行星数进行了优化。但如果您有很棒的配置，不妨试试！" );
            }
        }
        #endregion

        #region tPlanetNameTypeHeader
        public class tPlanetNameTypeHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "行星命名风格" );
            }
        }
        #endregion
        
        #region dPlanetNameType
        public class dPlanetNameType : DropdownAbstractBase
        {
            public static dPlanetNameType Instance;
            public dPlanetNameType()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;
                WorldSetup setupToViewOnly = World_AIW2.Instance.Setup;
                if ( setupToViewOnly == null )
                    return;

                PlanetNameTypeData ItemAsType = (PlanetNameTypeData)Item.GetItem();
                //don't set this again if it was already the same
                if ( setupToViewOnly.MapConfig.PlanetNameType == ItemAsType )
                    return;

                //ArcenDebugging.ArcenDebugLogSingleLine( "Issuing command to be " + ItemAsType.InternalName, Verbosity.DoNotShow );

                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "PlanetNameType";
                command.RelatedString2 = ItemAsType.InternalName;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                WorldSetup setupToViewOnly = World_AIW2.Instance.Setup;
                PlanetNameTypeData typeDataToSelect = null;
                if ( setupToViewOnly != null )
                    typeDataToSelect = setupToViewOnly.MapConfig.PlanetNameType;

                bool foundMismatch = false;
                if ( typeDataToSelect != null && ( elementAsType.CurrentlySelectedOption == null || ( PlanetNameTypeData)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect )  )
                {
                    foundMismatch = true;
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.InternalName, Verbosity.DoNotShow );
                }
                else
                {
                    for ( int i = 0; i < PlanetNameTypeDataTable.Instance.Rows.Count; i++ )
                    {
                        PlanetNameTypeData row = PlanetNameTypeDataTable.Instance.Rows[i];
                        if ( row.IsHidden )
                            continue;
                        if ( elementAsType.GetItemCount() <= i )
                        {
                            foundMismatch = true;
                            break;
                        }
                        IArcenUI_Dropdown_Option option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        PlanetNameTypeData optionItemAsType = (PlanetNameTypeData)option.GetItem();
                        if ( row == optionItemAsType )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( int i = 0; i < PlanetNameTypeDataTable.Instance.Rows.Count; i++ )
                    {
                        PlanetNameTypeData row = PlanetNameTypeDataTable.Instance.Rows[i];
                        if ( row.IsHidden )
                            continue;
                        DropdownOptionPlanetNameTypeData option = new DropdownOptionPlanetNameTypeData( row );
                        elementAsType.AddItem( option, row == typeDataToSelect );
                    }
                }
            }
            public override void HandleMouseover()
            {
                string mouseoverText = "选择行星名称的命名池。";
                WorldSetup setupToViewOnly = World_AIW2.Instance.Setup;
                if ( setupToViewOnly != null && setupToViewOnly.MapConfig.PlanetNameType != null )
                {
                    mouseoverText += "\n\nCurrently: <color=#7ab9ff>" + setupToViewOnly.MapConfig.PlanetNameType.DisplayName + "</color>\n" + setupToViewOnly.MapConfig.PlanetNameType.Description;
                }
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, mouseoverText );
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenUI_Dropdown_Option Item )
            {
                PlanetNameTypeData ItemAsType = (PlanetNameTypeData)Item.GetItem();
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( ItemElement, "选择行星名称的命名池。\n\n<color=#ffc87a>" + 
                    ItemAsType.DisplayName + "</color>:\n" + ItemAsType.Description );
            }
        }
        #endregion

        #region tSeedHeader
        public class tSeedHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "地图种子" );
            }
        }
        #endregion

        #region iSeed
        public class iSeed : InputAbstractBase
        {
            public static iSeed Instance;

            public iSeed()
            {
                Instance = this;
            }

            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                if ( input.Length >= 10 )
                    return '\0';
                if ( !char.IsDigit( addedChar ) )
                    return '\0';
                return addedChar;
            }

            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                if (!this.GetIsCurrentlyBeingEdited())
                    return InputActionTextboxResult.ExecuteNormalLogic;
                
                if (Action.InternalName == "Return")
                {
                    int seed;
                    if (!int.TryParse(this.GetText(), out seed))
                    {
                        return InputActionTextboxResult.DoNothingFurther;
                    }
                        
                    Window_SetupMapTabLeft.Instance.SetSeed(seed);
                    
                    return InputActionTextboxResult.UnfocusMe;
                }
                     
                // escape key
                if (Action.InternalName == "OpenSystemMenu") 
                {
                    return InputActionTextboxResult.UnfocusMe;
                }
                
                return InputActionTextboxResult.ExecuteNormalLogic;
            }
            
            public override void OnUpdate()
            {
                if ( !this.GetIsCurrentlyBeingEdited() )
                {
                    var setup = World_AIW2.Instance.Setup;
                    var seed = setup.MapConfig.Seed;
                    
                    int cur;
                    if (!int.TryParse(this.GetText(), out cur) ||
                        cur != seed)
                    {
                        var e = (ArcenUI_Input)this.Element;
                        e.SetText( seed.ToString() );
                    }
                }
            }
        }
        #endregion

        #region bRandomSeed
        public class bRandomSeed : ButtonAbstractBase
        {
            public static bRandomSeed Instance;

            public bRandomSeed()
            {
                Instance = this;
            }

            public DateTime TimeOfLastClick = DateTime.Now.AddSeconds( -10 );
            public const float MIN_TIME_BETWEEN_CLICKS_CLIENT = 1.5f;
            public const float MIN_TIME_BETWEEN_CLICKS_HOST = 1f;
            public const float MIN_TIME_BETWEEN_CLICKS_SOLO = 0.2f;

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                float timeSincLastClick = (float)( DateTime.Now - this.TimeOfLastClick ).TotalSeconds;
                float timeLimiter = MIN_TIME_BETWEEN_CLICKS_SOLO;
                switch ( ArcenNetworkAuthority.DesiredStatus )
                {
                    case DesiredMultiplayerStatus.Host:
                        timeLimiter = MIN_TIME_BETWEEN_CLICKS_HOST;
                        break;
                    case DesiredMultiplayerStatus.Client:
                        timeLimiter = MIN_TIME_BETWEEN_CLICKS_CLIENT;
                        break;
                }

                if ( timeSincLastClick < timeLimiter )
                {
                    Buffer.Add( "<size=11>" ).AddFixedDecimal( (timeLimiter - timeSincLastClick ), 1 );
                }
                else
                    Buffer.Add( "<size=11>随机化" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return Window_SetupMapTabLeft.Instance.Randomize();
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window-SetupMapTabLeft-bRandomSeed-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.Add( "随机选择新的地图种子。" );
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }
        #endregion

        #region bPriorSeed
        public class bPriorSeed : ButtonAbstractBase
        {
            public static bPriorSeed Instance;

            public bPriorSeed()
            {
                Instance = this;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                
                Buffer.StartSize("9");
                
                if (Window_SetupMapTabLeft.Instance.Throttle(true))
                    Buffer.Add( "-" );
                else
                    Buffer.Add( "上一个" );
                
                Buffer.EndSize();
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return Window_SetupMapTabLeft.Instance.Prior();
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window-SetupMapTabLeft-bPriorSeed-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.Add( 
                    "返回上一个种子和起始行星设置。" );
                
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }
        #endregion

        #region bRegenerateMap
        public class bRegenerateMap : ButtonAbstractBase
        {
            public static bRegenerateMap Instance;
            public bRegenerateMap()
            {
                Instance = this;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                if ( Mapgen.IsMapCurrentlyGenerating )
                {
                    Buffer.Add( "正在生成 " );
                    Buffer.AddFixedDecimal( (ArcenTime.TimeSinceStartF - Mapgen.LastMapgenStartTime), 1 );
                    Buffer.Add( "s...</b></color>\n" );
                }
                else
                    Buffer.Add( "重新生成地图" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return Window_SetupMapTabLeft.Instance.Regenerate();
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window-SetupMapTabLeft-bRegenerateMap-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.Add( "如果您做了尚未反映的更改，可以点击此处强制重新生成。" );
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }

        #endregion

        #region DropdownOption Types
        public class DropdownOptionMapTypeData : RowBasedDropdownOption<MapTypeData>
        {
            public DropdownOptionMapTypeData( MapTypeData Row ) : base( Row )
            {
            }
        }

        public class DropdownOptionMapTypeRandom : DropdownOptionMapTypeData
        {
            public DropdownOptionMapTypeRandom() : base( null )
            {
            }

            private static ArcenDoubleCharacterBuffer nameBuffer = new ArcenDoubleCharacterBuffer( "DropdownOptionMapTypeRandom-nameBuffer" );
            public override string GetOptionNameFromVolatile()
            {
                nameBuffer.Add( "随机银河系类型" );
                return nameBuffer.GetStringAndResetForNextUpdate();
            }
        }

        public class DropdownOptionPlanetNameTypeData : RowBasedDropdownOption<PlanetNameTypeData>
        {
            public DropdownOptionPlanetNameTypeData( PlanetNameTypeData Row ) : base( Row )
            {
            }
        }

        public class DropdownOptionFleetNameTypeData : RowBasedDropdownOption<FleetNameTypeData>
        {
            public DropdownOptionFleetNameTypeData( FleetNameTypeData Row ) : base( Row )
            {
            }
        }

        public class DropdownOptionPlanetCountData : IntBasedDropdownOption
        {
            public DropdownOptionPlanetCountData( int PlanetCount ) : base( PlanetCount, PlanetCount.ToString() )
            {
            }
        }
        #endregion
        
        #endregion

        #region Map Type Settings

        public static MapSettingOption GetMapSettingOptionForController( ElementAbstractBase controller )
        {
            WorldSetup setupToViewOnly = World_AIW2.Instance.Setup;
            MapTypeData mapType = setupToViewOnly.MapConfig.MapType;

            
            int optionIndex = controller.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;

            if ( optionIndex >= mapType.Options.Count || optionIndex < 0 )
                return null;
            return mapType.Options[optionIndex];
        }

        public static MapSettingOptionChoice GetMapSettingChoiceForController( ElementAbstractBase controller )
        {
            WorldSetup setupToViewOnly = World_AIW2.Instance.Setup;
            MapTypeData mapType = setupToViewOnly.MapConfig.MapType;

            int optionIndex = controller.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            if ( optionIndex >= mapType.Options.Count || optionIndex < 0 )
                return null;

            var opt = World_AIW2.Instance.Setup.MapConfig.MapType.Options[optionIndex];
            var val = World_AIW2.Instance.Setup.MapConfig.GetCustomInt(optionIndex);
            for (int i = 0; i < opt.Choices.Count; i++)
            {
                var c = opt.Choices[i];
                if (c.RelatedIntValue == val)
                {
                    return c;
                }
            }

            return null;
        }

        public static int GetMapSettingValueForController( ElementAbstractBase controller )
        {
            WorldSetup setupToViewOnly = World_AIW2.Instance.Setup;
            MapTypeData mapType = setupToViewOnly.MapConfig.MapType;

            int optionIndex = controller.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            if ( optionIndex >= setupToViewOnly.MapConfig.CustomInts.Count || optionIndex < 0 )
                return -1;
            return setupToViewOnly.MapConfig.CustomInts[optionIndex];
        }

        public class BoolToggle : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                MapSettingOption setting = GetMapSettingOptionForController( this );
                if ( setting == null ) return;

                var val = GetMapSettingValueForController( this );
                buffer.Add( val > 0 ? "开启" : "<color=#666666>关闭" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                MapSettingOption setting = GetMapSettingOptionForController( this );
                if ( setting == null ) return MouseHandlingResult.None;

                var val = GetMapSettingValueForController( this );

                var btn = (this.Element as ArcenUI_Button);
                
                if (val > 0)
                    val = 0;
                else
                    val = 1;

                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "MapIntForLobby";
                command.RelatedIntegers.Add( setting.ID );
                command.RelatedIntegers2.Add( val );
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                MapSettingOption setting = GetMapSettingOptionForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, setting.Description );
            }
        }

        public class IntBasedCustomDropdown : DropdownAbstractBase
        {
            private int lastValue = -1;
            private bool initialized = false;

            public override void OnMainThreadUpdate()
            {
                if (!initialized)
                {
                    var elementAsType = (ArcenUI_Dropdown)this.Element;

                    var setting = GetMapSettingOptionForController( this );
                    if ( setting == null ) 
                        return;

                    int val = World_AIW2.Instance.Setup.MapConfig.GetCustomInt(setting.ID);
                    //if (val == -1)
                    //{
                    //    val = setting.DefaultID;
                    //}

                    IntBasedDropdownOption sel = null;
                    for (int i = 0; i < setting.Choices.Count; i++)
                    {
                        var c = setting.Choices[i];

                        string name = c.DisplayName;
                        if (c.RelatedIntValue == setting.DefaultValue)
                            name = string.Format("<color=#ffcc6a>{0} (Default)</color>", name);

                        var item = new IntBasedDropdownOption(c.RelatedIntValue, name);

                        if (c.RelatedIntValue == val)
                            sel = item;
                        
                        elementAsType.AddItem(item, sel == item);
                    }

                    initialized = true;
                }

                base.OnMainThreadUpdate();
            }

            public override void OnUpdate()
            {
                var elementAsType = (ArcenUI_Dropdown)this.Element;

                var setting = GetMapSettingOptionForController( this );
                if ( setting == null ) 
                    return;
                
                int val = World_AIW2.Instance.Setup.MapConfig.GetCustomInt(setting.ID);
                //if (val == -1)
                //{
                //    val = setting.DefaultID;
                //}

                if (val != lastValue)
                {
                    var items = elementAsType.GetItems_DoNotAlterDirectly();

                    IntBasedDropdownOption sel = null;
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i] as IntBasedDropdownOption;
                        if (item.OptionValue == val)
                        {
                            elementAsType.SetSelectedItem( sel, DropdownSetType.FromMismatch );
                            break;
                        }
                    }
                    
                    lastValue = val;
                }
            }

            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                MapSettingOption setting = GetMapSettingOptionForController( this );
                if ( setting == null ) 
                    return;
                
                var opt = Item as IntBasedDropdownOption;
                if (opt == null)
                    return;

                int newValue = opt.OptionValue;

                //ArcenDebugging.ArcenDebugLogSingleLine(string.Format("Dropdown value change new is {0}", newValue), Verbosity.DoNotShow);

                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "MapIntForLobby";
                command.RelatedIntegers.Add( setting.ID );
                command.RelatedIntegers2.Add( newValue );
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                //World_AIW2.Instance.Setup.MapConfig.SetCustomInt(setting.ID, newValue);
            }

            public override void HandleMouseover()
            {
                MapSettingOption setting = GetMapSettingOptionForController( this );
                if ( setting == null ) return;

                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;
                IArcenUI_Dropdown_Option selectedItem = elementAsType == null ? null : elementAsType.CurrentlySelectedOption;
                IOptionIntBasedDropdownOption selectedItemAsDDop = selectedItem as IOptionIntBasedDropdownOption;

                if ( selectedItemAsDDop != null )
                {
                    IOptionIntBasedDropdownOption.tooltipBuffer.Add( setting.Description );
                    selectedItemAsDDop.FillMouseoverPartsForItem( setting.Description.Length > 0 );
                    selectedItemAsDDop.FinishAndDisplayTooltipFromBuffer( this.Element );
                }
                else
                {
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, setting.Description );
                }
            }

            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenUI_Dropdown_Option Item )
            {
                MapSettingOption setting = GetMapSettingOptionForController( this );
                if ( setting == null ) return;

                IOptionIntBasedDropdownOption itemAs = Item as IOptionIntBasedDropdownOption;

                if ( itemAs != null )
                {
                    itemAs.FillMouseoverPartsForItem( false );
                    itemAs.FinishAndDisplayTooltipFromBuffer( this.Element );
                }
            }
        }

        public class IntBasedSlider : SliderAbstractBase
        {
            private int lastVal = -1;

            public override void OnUpdate()
            {
                var elementAsType = (ArcenUI_Slider)this.Element;
                var refSlider = elementAsType.ReferenceSlider;
                var setting = GetMapSettingOptionForController( this );
                if ( setting == null ) return;

                refSlider.minValue = 0;
                refSlider.maxValue = setting.Choices.Count-1;
                refSlider.wholeNumbers = true;
                
                int val = World_AIW2.Instance.Setup.MapConfig.GetCustomInt(setting.ID);
                //if (val == -1)
                //{
                //    val = setting.DefaultID;
                //}

                if (val != lastVal)
                {
                    int newIndex = 0;
                    for (int i = 0; i < setting.Choices.Count; i++)
                    {
                        var c = setting.Choices[i];
                        if (c.RelatedIntValue == val)
                        {
                            newIndex = i;
                            break;
                        }
                    }

                    elementAsType.ReferenceSlider.value = newIndex;
                    lastVal = val;
                }
           }

            public override void OnChange( float NewValue )
            {
                //ArcenDebugging.ArcenDebugLogSingleLine(string.Format("Slider value change new is {0}", NewValue), Verbosity.DoNotShow);

                MapSettingOption setting = GetMapSettingOptionForController( this );
                if ( setting == null ) return;
                
                int newIndex = (int)Math.Round(NewValue);
                int newValue = setting.Choices[newIndex].RelatedIntValue;

                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "MapIntForLobby";
                command.RelatedIntegers.Add( setting.ID );
                command.RelatedIntegers2.Add( newValue );
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                //World_AIW2.Instance.Setup.MapConfig.SetCustomInt(setting.ID, newValue);
            }

            public override void HandleMouseover()
            {
                MapSettingOption setting = GetMapSettingOptionForController( this );
                if ( setting == null ) return;
                if ( setting.Description.Length > 0 )
                    Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, setting.Description );
            }
        }

        #endregion

        #endregion

        #region Commands
        
        public DateTime TimeOfLastClick = DateTime.Now.AddSeconds( -10 );
        public const float MIN_TIME_BETWEEN_CLICKS_CLIENT = 1.5f;
        public const float MIN_TIME_BETWEEN_CLICKS_HOST = 1f;
        public const float MIN_TIME_BETWEEN_CLICKS_SOLO = 0.2f;
            
        private bool Throttle(bool noupdate = false)
        {
            float timeSincLastClick = (float)(DateTime.Now - this.TimeOfLastClick).TotalSeconds;
            float timeLimiter = MIN_TIME_BETWEEN_CLICKS_SOLO;
            switch ( ArcenNetworkAuthority.DesiredStatus )
            {
                case DesiredMultiplayerStatus.Host:
                    timeLimiter = MIN_TIME_BETWEEN_CLICKS_HOST;
                    break;
                case DesiredMultiplayerStatus.Client:
                    timeLimiter = MIN_TIME_BETWEEN_CLICKS_CLIENT;
                    break;
            }

            if ( timeSincLastClick < timeLimiter )
                return true;
            
            if (!noupdate)
                TimeOfLastClick = DateTime.Now;
            
            return false;
        }
        
        private MouseHandlingResult Prior()
        {
            if (Throttle())
                return MouseHandlingResult.PlayClickDeniedSound;

            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
            command.RelatedString = "Prior";
            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
            
            return MouseHandlingResult.None;
        }
        
        private MouseHandlingResult SetSeed(int seed)
        {
            if (World_AIW2.Instance.Setup?.MapConfig?.Seed == seed)
                return MouseHandlingResult.None;
            
            if (Throttle())
                return MouseHandlingResult.PlayClickDeniedSound;

            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
            command.RelatedString = "SetSeed";
            command.RelatedIntegers.Add(seed);
            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
            
            return MouseHandlingResult.None;
        }
        
        private MouseHandlingResult Randomize()
        {
            if (Throttle())
                return MouseHandlingResult.PlayClickDeniedSound;

            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
            command.RelatedString = "SetSeed";
            command.RelatedIntegers.Add(Engine_Universal.PermanentQualityRandom.Next( 1, 999999999 ));
            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
            
            return MouseHandlingResult.None;
        }
        
        private MouseHandlingResult Regenerate()
        {
            if (Throttle())
                return MouseHandlingResult.PlayClickDeniedSound;

            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetupOnly_RequestSetupChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
            command.RelatedString = "Regenerate";
            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
            
            return MouseHandlingResult.None;
        }
        
        #endregion
    }
}
