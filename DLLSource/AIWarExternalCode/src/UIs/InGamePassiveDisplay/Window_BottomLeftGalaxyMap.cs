using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using TMPro;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_BottomLeftGalaxyMap : WindowControllerAbstractBase, IInputActionHandler
    {
        public static Window_BottomLeftGalaxyMap Instance;
        public Window_BottomLeftGalaxyMap()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true;
        }

        #region GetShouldDrawThisFrame_Subclass
        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( Engine_AIW2.Instance.CurrentGameViewMode != GameViewMode.GalaxyMapView )
                return false;
            //only draw on the galaxy map
            return true;
        }
        #endregion

        public const float SCALE_MULTIPLIER = 1f;

        //from IInputActionHandler
        public void Handle( int Int1, InputActionTypeData InputActionType )
        {
            //ArcenDebugging.ArcenDebugLogSingleLine(string.Format("Window_BottomLeftGalaxyMap.Handle called {0}", InputActionType.InternalName), Verbosity.DoNotShow);

            switch ( InputActionType.InternalName )
            {
                case "FocusSearchBox":
                    var e = iSearchOrSimilar.Instance.Element as ArcenUI_Input;
                    e.ReferenceInputField.onFocusSelectAll = true;
                    e.Focus();
                    break;
            }
        }

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                Window_BottomLeftGalaxyMap.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.TimeSinceStartF;
            }

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_BottomLeftGalaxyMap.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        hasGlobalInitialized = true;
                        
                    }
                    #endregion
                }

                this.WindowController.myScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" ) * SCALE_MULTIPLIER; //manually make it smaller
                this.WindowController.myXPositionScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" );
            }
        }

        #region btnEditPlanet
        public class btnEditPlanet : HighlightSettableButtonBase
        {
            public static btnEditPlanet Instance;
            public btnEditPlanet() { if ( Instance == null ) Instance = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                if ( planet == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                Window_EditPlanetWindow.Instance.Open( planet );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                if ( planet == null )
                    return;
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "编辑该星球的名称、备注和优先级：" + planet.Name );
            }
        }
        #endregion

        #region btnPingPlanet
        public class btnPingPlanet : HighlightSettableButtonBase
        {
            public static btnPingPlanet Instance;
            public btnPingPlanet() { if ( Instance == null ) Instance = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Engine_AIW2.Instance.PendingTargetedAction = null;
                // todo: make these all ITargetedInputAction(s)
                Engine_AIW2.Instance.IsInPingLocationMode = !Engine_AIW2.Instance.IsInPingLocationMode;
                Engine_AIW2.Instance.PlacingDirectBuildable = DirectBuildable.CreateBlank();
                Engine_AIW2.Instance.PlacingOutguardDeployable = null;
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "点击进入标记模式，可在银河地图或单个星球上使用（快捷键：" +
                    InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "TogglePingMode" ) + "）。你在银河地图上留下的标记会持续6秒，但不会随时间变小。它们用于用一个小绿点吸引多人游戏盟友的注意。如果你们几个人同时在标记，最好轮流进行以便清晰讨论。在单个星球上，你可以画出一连串标记，这些标记会随时间变小，从而让你指示方向。" );
            }
        }
        #endregion

        #region btnFleetStatus
        public class btnFleetStatus : HighlightSettableButtonBase
        {
            public static btnFleetStatus Instance;
            public btnFleetStatus() { if ( Instance == null ) Instance = this; }

            public static void ToggleFleetHealthForPlanet()
            {
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                string reason = "FleetHealth" + planet.Name;
                if ( Window_BottomLeftSelfUpdatingTextWindow.Instance.GetIsOpenForReason( reason ) )
                    Window_BottomLeftSelfUpdatingTextWindow.Instance.Close();
                else
                    Window_BottomLeftSelfUpdatingTextWindow.Instance.Open( reason, 0.5f, 2f, "舰队状态：" + planet.Name,
                        delegate ( ArcenDoubleCharacterBuffer Buffer )
                        {
                            bool foundValid = GetAllFleetHealth( Buffer, planet );
                            if ( foundValid )
                                priorBufferString = Buffer.GetStringAndResetForNextUpdate();
                            Buffer.Add( priorBufferString );
                            return foundValid;
                        } );
            }

            public static void ToggleFleetHealthForAllPlanets()
            {
                if ( Window_BottomLeftSelfUpdatingTextWindow.Instance.GetIsOpenForReason( "FleetHealthAll" ) )
                    Window_BottomLeftSelfUpdatingTextWindow.Instance.Close();
                else
                    Window_BottomLeftSelfUpdatingTextWindow.Instance.Open( "FleetHealthAll", 0.5f, 2f, "舰队状态（所有星球）",
                        delegate ( ArcenDoubleCharacterBuffer Buffer )
                        {
                            bool foundValid = GetAllFleetHealth( Buffer, null );
                            if ( foundValid )
                                priorBufferString = Buffer.GetStringAndResetForNextUpdate();
                            Buffer.Add( priorBufferString );
                            return foundValid;
                        } );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( input.LeftButtonClicked )
                {
                    ToggleFleetHealthForPlanet();
                }
                else
                {
                    ToggleFleetHealthForAllPlanets();
                }
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "左键点击查看 " + planet.Name + " 上所有舰队的详情（快捷键：" +
                    InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "ToggleFleetStatusWindowForPlanet" ) + "）。右键点击查看所有星球上舰队的详情（快捷键：" +
                    InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "ToggleFleetStatusWindowForAll" ) + "）。" );
            }

            private static string priorBufferString = string.Empty;

            private static List<Fleet> SortedFleets = List<Fleet>.Create_WillNeverBeGCed( 300, "Window_BottomLeftGalaxyMap-btnFleetStatus-SortedFleets" );
            private static List<FleetMembership> SortedShipLines = List<FleetMembership>.Create_WillNeverBeGCed( 3000, "Window_BottomLeftGalaxyMap-btnFleetStatus-SortedShipLines" );
            private static bool GetAllFleetHealth( ArcenDoubleCharacterBuffer buffer, Planet specificPlanet )
            {
                int debugCode = 0;
                try
                {
                    Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                    if ( localFaction == null )
                        return false;
                    int allMetalCosts = 0;
                    bool showVerboseDetails = InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1();
                    bool includeOtherFactions = InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key2();
                    bool exitAfterStatus = false;
                    SortedFleets.Clear();
                    foreach ( Fleet fleet in World_AIW2.Instance.Fleets( FleetStatus.CenterpieceMustLive ) )
                    {
                        if ( fleet == null || fleet.Centerpiece.GetSquad() == null )
                            continue;
                        if ( !fleet.Faction.GetIsFriendlyTowards( localFaction ) )
                            continue;
                        if ( fleet.Faction != localFaction && !includeOtherFactions )
                            continue;
                        if ( fleet.Category != FleetCategory.PlayerMobile && 
                        fleet.Category != FleetCategory.PlayerCustomCityFedMobile && fleet.Category != FleetCategory.PlayerCustomUnattachedMobile )
                            continue; //only mobile fleets

                        if ( specificPlanet != null && fleet.Centerpiece.GetSquad().Planet != specificPlanet )
                            continue;//not on this planet?
                        if ( fleet.Centerpiece.GetSquad().TypeData.SpecialType == SpecialEntityType.MobileSupportFleetFlagship )
                            continue; //not for support fleets
                        if ( fleet.GetCountOfShipLines( false, false ) <= 1 && (fleet.Centerpiece.GetSquad().TypeData.SpecialType == SpecialEntityType.MobileCustomUnattachedFleetFlagship ||
                                                                fleet.Centerpiece.GetSquad().TypeData.SpecialType == SpecialEntityType.MobileStrikeCombatFleetFlagship) )
                            continue; //don't flag "regular" fleets that have no ship lines (by default flagships seem to have 'themselves' and 'empty slot' at least)
                        SortedFleets.Add( fleet );
                    }
                    SortedFleets.Sort( static delegate ( Fleet L, Fleet R )
                    {
                        if ( L.TiedToKeybindIndexOneIndexed != R.TiedToKeybindIndexOneIndexed )
                            return L.TiedToKeybindIndexOneIndexed.CompareTo( R.TiedToKeybindIndexOneIndexed );
                        if ( L.GetName() != R.GetName() )
                            return L.GetName().CompareTo( R.GetName() );
                        return L.GetMaxStrengthOfFleet_ForUIOnly( false ).CompareTo( R.GetMaxStrengthOfFleet_ForUIOnly( false ) );
                    } );
                    for ( int i = 0; i < SortedFleets.Count; i++ )
                    {
                        Fleet fleet = SortedFleets[i];
                        int currentStrength = fleet.GetCurrentStrengthOfFleet_ForUIOnly( false );
                        int maxStrength = fleet.GetMaxStrengthOfFleet_ForUIOnly( false );
                        float ratio = (float)currentStrength / maxStrength;
                        Color fleetColor = EntityText.GetProportionalStrengthColor( ratio );
                        buffer.StartColor( fleetColor );
                        buffer.Add( fleet.GetName() ).EndColor();
                        if ( fleet.TiedToKeybindIndexOneIndexed >= 0 )
                        {
                            buffer.Add( " (" ).Add( fleet.TiedToKeybindIndexOneIndexed ).Add( ")" );
                        }

                        buffer.Add( "在 " ).Add( fleet.Centerpiece.GetSquad().GetPlanetName_Safe(), fleet.Centerpiece.GetSquad().Planet.GetControllingOrInfluencingFaction().FactionCenterColor.ColorHexBrighter ).Add( " " );
                        if ( showVerboseDetails )
                        {
                            //first print our and enemy strength, then set up for the fleet line details
                            int enemyStr = fleet.Centerpiece.GetSquad().PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                            int myStr = fleet.Centerpiece.GetSquad().PlanetFaction.DataByStance[FactionStance.Self].TotalStrength + fleet.Centerpiece.GetSquad().PlanetFaction.DataByStance[FactionStance.Friendly].TotalStrength;
                            buffer.Add( "(" );
                            buffer.Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon );
                            buffer.StartColor( fleet.Faction.FactionCenterColor.ColorHexBrighter );
                            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, myStr, true, true );
                            buffer.EndColor();
                            //                        buffer.Add( myStr.ToString(), fleet.Faction.FactionCenterColor.ColorHexBrighter );
                            buffer.Add( " " ).Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon );
                            buffer.StartColor( "ff0000" );
                            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, enemyStr, true, true );
                            buffer.EndColor();
                            buffer.Add( ") " );
                        }

                        Color fullHealthColor = EntityText.GetProportionalStrengthColor( 1 );
                        exitAfterStatus = false;
                        if ( currentStrength == maxStrength )
                        {
                            buffer.Add( "满 " ).Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon ).Add( "：" ).StartColor( fleetColor );
                            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, maxStrength, true, true );
                            buffer.EndColor();
                            exitAfterStatus = true;
                        }
                        else
                        {
                            buffer.Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon ).StartColor( fleetColor );
                            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, currentStrength, true, true );
                            buffer.EndColor();
                            {
                                buffer.Add( "/" ).StartColor( fullHealthColor );
                                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, maxStrength, true, true );
                                buffer.EndColor();
                            }
                        }
                        buffer.Add( "." );
                        buffer.Add( "旗舰" );
                        if ( fleet.Centerpiece.GetSquad().GetIsCrippled() )
                            buffer.Add( " 已残废", "ffa1a1" );
                        else
                        {
                            float centerpieceRatio = (float)fleet.Centerpiece.GetSquad().GetCurrentHullPoints() / fleet.Centerpiece.GetSquad().GetMaxHullPoints();
                            Color centerpieceColor = EntityText.GetProportionalStrengthColor( centerpieceRatio );
                            buffer.Add( " 在 " );
                            buffer.StartColor( centerpieceColor );
                            buffer.Add( (int)(centerpieceRatio * 100) );
                            buffer.Add( "%" );
                            buffer.EndColor();
                            buffer.Add(" badger ");
                            if ( ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() && GameSettings.Current.GetBoolBySetting( "ShowTooltipProgressBars" ) )
                                ArcenExternalUIUtilities.AppendBar( buffer, (int)(centerpieceRatio * 100), centerpieceColor, 8 );
                        }

                        if ( fleet.Centerpiece.GetSquad().ActiveHack != null )
                        {
                            buffer.Add( " 且正在入侵", "a1ffa1" );
                        }
                        string typeDataColor = "c0c0c0";
                        if ( fleet.SupportingFactoriesInRange.Count == 0 )
                        {
                            buffer.Add( " 且不在建造范围内", "8a8a8a" );
                            typeDataColor = "8a8a8a";
                        };
                        if ( fleet.IsFleetConstructionPaused )
                        {
                            buffer.Add( " 已暂停建造。" );
                            continue;
                        }
                        else if ( fleet.IsFleetConstructionBlocked )
                        {
                            buffer.Add( " 建造已被阻止。" );
                            continue;
                        }
                        buffer.Add( ". " );
                        if ( fleet.Faction != localFaction )
                        {
                            buffer.Add( "此舰队属于 " ).Add( fleet.Faction.GetDisplayName(), fleet.Faction.FactionCenterColor.ColorHexBrighter ).Add( "。  " );
                        }
                        bool foundAnyToBuild = false;
                        int metalToRebuild = 0;

                        if ( showVerboseDetails )
                            buffer.Add( "单位线详情：" );
                        SortedShipLines.Clear();
                        foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                        {
                            debugCode = 5912;
                            if ( mem == null )
                                continue;
                            if ( mem.TypeData.IsDrone || mem.TypeData.SelfConstructs )
                                continue;
                            if ( mem.TypeData.IsFleetLeader )
                                continue;
                            if ( mem.EffectiveSquadCap <= 0 )
                                continue;
                            SortedShipLines.Add(mem);
                        }
                        for ( int j = 0; j < SortedShipLines.Count; j++ )
                        {
                            FleetMembership mem = SortedShipLines[j];
                            foundAnyToBuild = true;
                            int remainingShips = mem.EffectiveSquadCap - mem.GetRemainingCap( false, -1, ExtraFromStacks.IncludePrecalc );
                            int costToRebuildThisShipLine = mem.GetRemainingCap( false, -1, ExtraFromStacks.IncludePrecalc ) * mem.GetMetalCost();
                            if ( !mem.IsFleetMembershipConstructionPaused && fleet.Faction == localFaction )
                            {
                                //only include non-paused fleets that we own
                                metalToRebuild += costToRebuildThisShipLine;
                                allMetalCosts += costToRebuildThisShipLine;
                            }
                            if ( !showVerboseDetails )
                                continue;
                            float memRatio = (float)remainingShips / mem.EffectiveSquadCap;
                            Color memColor = EntityText.GetProportionalStrengthColor( memRatio );

                            buffer.AddShipIconInline(mem.TypeData, localFaction);
                            buffer.Add(mem.TypeData.DisplayNameForSidebar, typeDataColor );
      
                            if ( mem.IsFleetMembershipConstructionPaused )
                                buffer.Add( "已暂停" );
                            if ( remainingShips == mem.EffectiveSquadCap )
                                buffer.StartColor( memColor ).Add( " (" ).Add( remainingShips ).EndColor().Add( ") " );
                            else
                                buffer.StartColor( memColor ).Add( " (" ).Add( remainingShips ).EndColor().Add( "/" ).StartColor( fullHealthColor ).Add( mem.EffectiveSquadCap ).EndColor().Add( ") " );
                            
                            // buffer.Add( "  " ).StartColor( ColorMath.LightLeafGreen );
                            // buffer.Add( (((float)mem.MetalSpentConstructingCurrentReplacement / (float)mem.GetMetalCost()) * 100f).ToString( "00.0" ) );
                            // buffer.Add( "%" ).EndColor().Add( ")" );
                        };
                        {
                            if ( !foundAnyToBuild )
                                buffer.Add( "已完成所有建造\n" );
                            else if ( metalToRebuild > 0 )
                            {
                                buffer.Add( "重建损失单位所需金属：" );
                                buffer.StartColor( "ccccee" );
                                buffer.Add( "<b>" );
                                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, metalToRebuild, true, false );
                                buffer.Add( "</b>" );
                                buffer.EndColor();
                                buffer.Add( "." );
                            }
                        }
                        buffer.Add( "\n\n" );
                    }
                    if ( exitAfterStatus )
                        buffer.Add( "\n\n" ); //add the newlines we missed earlier
                    if ( allMetalCosts > 0 && allMetalCosts < localFaction.StoredMetal )
                    {
                        buffer.Add( "你将剩余 ", "47b247" );
                        buffer.StartColor( "ccccee" );
                        ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, (localFaction.StoredMetal - allMetalCosts).IntValue, true, false );
                        buffer.EndColor();
                        buffer.Add( " 金属用于重建损失后剩余。\n\n", "47b247" );
                    }
                    else if ( allMetalCosts > localFaction.StoredMetal )
                    {
                        buffer.Add( "你还差 ", "b24747" );
                        buffer.StartColor( "ccccee" );
                        ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, (allMetalCosts - localFaction.StoredMetal).IntValue, true, false );
                        buffer.EndColor();
                        buffer.Add( " 金属才能重建你的损失。\n\n", "b24747" );
                    }
                    //TODO: use this
                    //                   if ( localFaction.NetEnergy <= 0 )
                    //ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( Buffer, localFaction.NetEnergy, true, false );
                    buffer.Add( FontSizes.SLIGHTLY_SMALLER_SIZE_STRING );
                    buffer.Add( "<color=#d18444>按住 <color=#996f4c>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key1" ) ).Add( "</color> 查看每艘旗舰的详细信息。</color>\n" );
                    buffer.Add( "<color=#d18444>按住 <color=#996f4c>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key2" ) ).Add( "</color> 查看其他玩家的旗舰。</color>" );
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Exception in calculating fleet health at debugCode " + debugCode + ". Exception: " + e, Verbosity.ShowAsError );
                    return false;
                }
                return true;
            }
        }
        #endregion

        #region dFactionDropdown
        public class dFactionDropdown : DropdownAbstractBase
        {
            public static dFactionDropdown Instance;
            public dFactionDropdown()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                //set this locally for the current player only
                FactionFilterDropdownOption ItemAsType = (FactionFilterDropdownOption)Item;
                PlayerAccount_AIW2.SetCurrentGalaxyMapDisplayMode_FactionIndexSafe( ItemAsType.Filter.GetFactionIndex() );
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                int factionIndexToSelect = PlayerAccount_AIW2.GetCurrentGalaxyMapDisplayMode_FactionIndexSafe();
                List<FactionFilter> factionFilters = FactionFilter.GetLatestSortedFiltersAll();

                bool foundMismatch = false;
                if ( elementAsType.CurrentlySelectedOption == null || ((FactionFilter)elementAsType.CurrentlySelectedOption.GetItem()).GetFactionIndex() != factionIndexToSelect )
                {
                    foundMismatch = true;
                }
                else
                {
                    for ( int i = 0; i < factionFilters.Count; i++ )
                    {
                        FactionFilter row = factionFilters[i];
                        //if ( row.IsHidden )
                        //    continue;
                        if ( elementAsType.GetItemCount() <= i )
                        {
                            foundMismatch = true;
                            break;
                        }
                        IArcenUI_Dropdown_Option option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        FactionFilter optionItemAsType = (FactionFilter)option.GetItem();
                        if ( row.GetFactionIndex() == optionItemAsType.GetFactionIndex() )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( int i = 0; i < factionFilters.Count; i++ )
                    {
                        FactionFilter row = factionFilters[i];
                        //if ( row.IsHidden )
                        //    continue;
                        FactionFilterDropdownOption option = new FactionFilterDropdownOption( row );
                        elementAsType.AddItem( option, row.GetFactionIndex() == factionIndexToSelect );
                    }
                }
            }
            public override void HandleMouseover()
            {
                string mouseoverText = "选择右侧银河地图显示模式中使用的派系或派系类型作为筛选器。";
                FactionFilter currentFilter = FactionFilter.GetFactionFilterByIndex( PlayerAccount_AIW2.GetCurrentGalaxyMapDisplayMode_FactionIndexSafe() );
                if ( currentFilter.GetIsValid() )
                {
                    mouseoverText += "\n\n当前：<color=#" + currentFilter.GetTextColor() + ">" + currentFilter.GetDisplayName() + "</color>\n" + currentFilter.GetTooltip();
                }
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, mouseoverText );
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenUI_Dropdown_Option Item )
            {
                FactionFilter ItemAsType = (FactionFilter)Item.GetItem();
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( ItemElement, "选择右侧银河地图显示模式中使用的派系或派系类型作为筛选器。\n\n<color=#" +
                    ItemAsType.GetTextColor() + ">" +
                    ItemAsType.GetDisplayName() + "</color>：\n" + ItemAsType.GetTooltip() );
            }
        }

        public class FactionFilterDropdownOption : IArcenUI_Dropdown_Option
        {
            public FactionFilter Filter;

            public FactionFilterDropdownOption( FactionFilter Filter )
            {
                this.Filter = Filter;
            }

            public object GetItem()
            {
                return this.Filter;
            }

            public string GetOptionNameFromVolatile()
            {
                return "<color=#" + this.Filter.GetTextColor() + ">" + this.Filter.GetDisplayName();
            }

            public Sprite GetOptionSprite()
            {
                return null;
            }
        }
        #endregion

        #region dMapFunction
        public class dMapFunction : DropdownAbstractBase
        {
            public static dMapFunction Instance;
            public dMapFunction()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                //set this locally for the current player only
                GalaxyMapDisplayMode ItemAsType = (GalaxyMapDisplayMode)Item.GetItem();
                PlayerAccount_AIW2.SetCurrentGalaxyMapDisplayModeSafe( ItemAsType );
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                //WorldSetup setupToViewOnly = World_AIW2.Instance.SetupWorkingForLobbyOnly;
                GalaxyMapDisplayMode typeDataToSelect = PlayerAccount_AIW2.GetCurrentGalaxyMapDisplayModeSafe();
                if ( typeDataToSelect == null )
                    typeDataToSelect = GalaxyMapDisplayModeTable.Instance.DefaultRow;

                bool foundMismatch = false;
                if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (GalaxyMapDisplayMode)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
                {
                    foundMismatch = true;
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.InternalName, Verbosity.DoNotShow );
                }
                else
                {
                    for ( int i = 0; i < GalaxyMapDisplayModeTable.Instance.Rows.Count; i++ )
                    {
                        GalaxyMapDisplayMode row = GalaxyMapDisplayModeTable.Instance.Rows[i];
                        if ( row.IsHidden ||!row.Implementation.GetShouldBeShownInCurrentCampaign() )
                            continue;
                        if ( elementAsType.GetItemCount() <= i )
                        {
                            foundMismatch = true;
                            break;
                        }
                        IArcenUI_Dropdown_Option option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        GalaxyMapDisplayMode optionItemAsType = (GalaxyMapDisplayMode)option.GetItem();
                        if ( row == optionItemAsType )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( int i = 0; i < GalaxyMapDisplayModeTable.Instance.Rows.Count; i++ )
                    {
                        GalaxyMapDisplayMode row = GalaxyMapDisplayModeTable.Instance.Rows[i];
                        if ( row.IsHidden || !row.Implementation.GetShouldBeShownInCurrentCampaign() )
                            continue;
                        DropdownOptionGalaxyMapDisplayMode option = new DropdownOptionGalaxyMapDisplayMode( row );
                        elementAsType.AddItem( option, row == typeDataToSelect );
                    }
                }
            }
            public override void HandleMouseover()
            {
                string mouseoverText = "选择右侧文本框的功能。";
                GalaxyMapDisplayMode typeDataToSelect = PlayerAccount_AIW2.GetCurrentGalaxyMapDisplayModeSafe();
                if ( typeDataToSelect != null )
                {
                    mouseoverText += "\n\n当前：<color=#7ab9ff>" + typeDataToSelect.DisplayName + "</color>\n" + typeDataToSelect.Tooltip;
                }
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, mouseoverText );
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenUI_Dropdown_Option Item )
            {
                GalaxyMapDisplayMode ItemAsType = (GalaxyMapDisplayMode)Item.GetItem();
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( ItemElement, "选择右侧文本框的功能。\n\n<color=#ffc87a>" +
                    ItemAsType.DisplayName + "</color>：\n" + ItemAsType.Tooltip );
            }
        }

        public class DropdownOptionGalaxyMapDisplayMode : RowBasedDropdownOption<GalaxyMapDisplayMode>
        {
            public DropdownOptionGalaxyMapDisplayMode( GalaxyMapDisplayMode Row ) : base( Row )
            {
            }
        }
        #endregion

        #region dTextboxFunction
        public class dTextboxFunction : DropdownAbstractBase
        {
            public static dTextboxFunction Instance;
            public dTextboxFunction()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                //set this locally for the current player only
                GalaxyMapTextboxFunction ItemAsType = (GalaxyMapTextboxFunction)Item.GetItem();
                PlayerAccount_AIW2.SetCurrentGalaxyMapTextboxFunctionSafe( ItemAsType );
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                //WorldSetup setupToViewOnly = World_AIW2.Instance.SetupWorkingForLobbyOnly;
                GalaxyMapTextboxFunction typeDataToSelect = PlayerAccount_AIW2.GetCurrentGalaxyMapTextboxFunctionSafe();
                if ( typeDataToSelect == null  )
                    typeDataToSelect = GalaxyMapTextboxFunctionTable.Instance.DefaultRow;

                bool foundMismatch = false;
                if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (GalaxyMapTextboxFunction)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
                {
                    foundMismatch = true;
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.InternalName, Verbosity.DoNotShow );
                }
                else
                {
                    for ( int i = 0; i < GalaxyMapTextboxFunctionTable.Instance.Rows.Count; i++ )
                    {
                        GalaxyMapTextboxFunction row = GalaxyMapTextboxFunctionTable.Instance.Rows[i];
                        if ( row.IsHidden )
                            continue;
                        if ( elementAsType.GetItemCount() <= i )
                        {
                            foundMismatch = true;
                            break;
                        }
                        IArcenUI_Dropdown_Option option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        GalaxyMapTextboxFunction optionItemAsType = (GalaxyMapTextboxFunction)option.GetItem();
                        if ( row == optionItemAsType )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( int i = 0; i < GalaxyMapTextboxFunctionTable.Instance.Rows.Count; i++ )
                    {
                        GalaxyMapTextboxFunction row = GalaxyMapTextboxFunctionTable.Instance.Rows[i];
                        if ( row.IsHidden )
                            continue;
                        DropdownOptionGalaxyMapTextboxFunction option = new DropdownOptionGalaxyMapTextboxFunction( row );
                        elementAsType.AddItem( option, row == typeDataToSelect );
                    }
                }
            }
            public override void HandleMouseover()
            {
                string mouseoverText = "选择右侧文本框的功能。";
                GalaxyMapTextboxFunction typeDataToSelect = PlayerAccount_AIW2.GetCurrentGalaxyMapTextboxFunctionSafe();
                if ( typeDataToSelect != null )
                {
                    mouseoverText += "\n\n当前：<color=#7ab9ff>" + typeDataToSelect.DisplayName + "</color>\n" + typeDataToSelect.Tooltip;
                }
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, mouseoverText );
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenUI_Dropdown_Option Item )
            {
                GalaxyMapTextboxFunction ItemAsType = (GalaxyMapTextboxFunction)Item.GetItem();
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( ItemElement, "选择右侧文本框的功能。\n\n<color=#ffc87a>" +
                    ItemAsType.DisplayName + "</color>：\n" + ItemAsType.Tooltip );
            }
        }

        public class DropdownOptionGalaxyMapTextboxFunction : RowBasedDropdownOption<GalaxyMapTextboxFunction>
        {
            public DropdownOptionGalaxyMapTextboxFunction( GalaxyMapTextboxFunction Row ) : base( Row )
            {
            }
        }
        #endregion

        #region iSearchOrSimilar
        public class iSearchOrSimilar : InputAbstractBase
        {
            public static iSearchOrSimilar Instance;
            public iSearchOrSimilar() { Instance = this; }
            public override void HandleChangeInValue( string NewValue )
            {
                PlayerAccount_AIW2.SetCurrentGalaxyMapTextboxTextSafe( NewValue );
            }

            private string lastPlaceholderText = string.Empty;

            public override void OnUpdate()
            {
                ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;

                //set the placeholder text based on what is set to the left
                GalaxyMapTextboxFunction function = PlayerAccount_AIW2.GetCurrentGalaxyMapTextboxFunctionSafe();
                if ( function != null &&
                    this.lastPlaceholderText != function.PlaceholderText )
                {
                    this.lastPlaceholderText = function.PlaceholderText;
                    TextMeshProUGUI placeHolderText = elementAsType.ReferenceInputField.placeholder as TextMeshProUGUI;
                    if ( placeHolderText )
                    {
                        placeHolderText.text = function.PlaceholderText;
                    }
                }
                //only update to the current value if we're not editing this field right now
                if ( !this.GetIsCurrentlyBeingEdited() )
                {
                    elementAsType.SetText( PlayerAccount_AIW2.GetCurrentGalaxyMapTextboxTextSafe() );
                }
            }

            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                //ArcenDebugging.ArcenDebugLogSingleLine(string.Format("Window_BottomLeftGalaxyMap.iSearchOrSimilar.Handle called {0}", Action.InternalName), Verbosity.DoNotShow);

                switch ( Action.InternalName )
                {
                    case "OpenSystemMenu": //escape key
                        PlayerAccount_AIW2.SetCurrentGalaxyMapTextboxTextSafe( string.Empty ); //wipe out this field, since we hit escape
                        return InputActionTextboxResult.UnfocusMe;
                    case "Return": //enter key
                        return InputActionTextboxResult.UnfocusMe;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }
        }
        #endregion

        public class bClearText : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                PlayerAccount_AIW2.SetCurrentGalaxyMapTextboxTextSafe( string.Empty );
                return MouseHandlingResult.None;
            }
        }
    }
}
