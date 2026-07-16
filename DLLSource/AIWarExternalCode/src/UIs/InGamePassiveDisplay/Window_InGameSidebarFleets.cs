using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Diagnostics;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameSidebarFleets : Window_InGameSidebarBase
    {
        public static Window_InGameSidebarFleets Instance;
        public Window_InGameSidebarFleets()
        {
            Instance = this;
            this.OnlyShowInGame = true;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            return Current == InGameSidebarType.Fleets;
        }

        private static PoolableGUIGroup.GroupPool<FleetCategory> FleetCategoryPool;

        public static CustomUIAbstractBase CustomParentInstance;
        
        #region customParent
        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                Window_InGameSidebarFleets.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.TimeSinceStartF;
            }

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                AdjustHeightToScreenMax( 60, "NotificationsScale", "SidebarScale", "ResourceBarScale", Window_InGameSidebarShips.CustomParentInstance,
                    Window_InGameSidebarFleets.CustomParentInstance, Window_InGameSidebarDirectBuild.CustomParentInstance,
                    Window_InGameSidebarScience.CustomParentInstance, Window_InGameSidebarHacking.CustomParentInstance,
                    Window_InGameSidebarOutguard.CustomParentInstance, Window_InGameSidebarObjectives.CustomParentInstance, 
                    Window_InGameSidebarJournal.CustomParentInstance, Window_InGameSidebarTips.CustomParentInstance );

                if ( Window_InGameSidebarFleets.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( btnFleetCategory.Original != null )
                        {
                            hasGlobalInitialized = true;
                            FleetCategoryPool = new PoolableGUIGroup.GroupPool<FleetCategory>( new FleetCategory( btnFleetCategory.Original ), 6 );
                        }
                    }
                    #endregion
                }

                float currentY = 0; //the position of the first entry

                this.OnUpdateFleets( ref currentY );

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)btnFleetCategory.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }

            public const float TEXT_ROW_HEIGHTS_UPPER = 25f;
            public const float TEXT_ROW_HEIGHTS_LOWER = 26.04f;
            public const float TEXT_ROW_HEIGHTS_LOWER_ITEMS = 14.7f;
            public const float ROW_ADVANCE_WHEN_CLOSED = 5f;


            #region OnUpdateFleets
            public void OnUpdateFleets( ref float currentY )
            {
                FleetCategoryPool.Clear( 5 );
                int remainingSubItemsCanAdd = 10;

                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return;
                
                Planet currentlyViewedPlanet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                RefillFleetsByPurpose( localFaction, currentlyViewedPlanet );

                bool isDZSidekick = ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() && DarkZenithSidekickFactionBaseInfo.GetIsThisADZFaction( localFaction );
                for ( var purpose = FleetCategoryPurpose.None; purpose < FleetCategoryPurpose.Length; purpose++ )
                {
                    List<Fleet> fleets = myFleetsByPurpose[purpose];

                    if ( fleets.Count > 0 || ( ( purpose == FleetCategoryPurpose.DZEconomy || purpose == FleetCategoryPurpose.DZLogistics || purpose == FleetCategoryPurpose.DZTechTree ) && isDZSidekick ) )
                    {
                        FleetCategory fleetPurposeCat = FleetCategoryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( fleetPurposeCat == null )
                            break; //time slicing, too many added right now
                        fleetPurposeCat.Purpose = purpose;
                        fleetPurposeCat.FleetsInCategory.Clear();
                        fleetPurposeCat.FleetsInCategory.AddRange( fleets );
                    }
                }

                var cats = FleetCategoryPool.GetInUseList();
                for ( int i = 0; i < cats.Count; i++ )
                    cats[i].Update( ref currentY, ref remainingSubItemsCanAdd );
            }
            #endregion
        }
        #endregion
        
        #region RefillFleetsByPurpose
        public static readonly EnumIndexedArrayOfLists<FleetCategoryPurpose, Fleet> myFleetsByPurpose = 
            EnumIndexedArrayOfLists<FleetCategoryPurpose, Fleet>.Create_WillNeverBeGCed( 300, "Window_InGameSidebarFleets-myFleetsByPurpose" );

        public static void RefillFleetsByPurpose( Faction localFaction, Planet currentlyViewedPlanetOrNull )
        {
            if ( localFaction == null )
                return;

            myFleetsByPurpose.Clear();

            #region Distribute All the Fleets that we have into myFleetsByPurpose lists
            foreach ( Fleet fleet in World_AIW2.Instance.Fleets( localFaction, FleetStatus.CenterpieceMustLive ) )
            {
                switch ( fleet.Category )
                {
                    //skip these
                    default:
                    case Core.FleetCategory.NonPlayerDrone:
                    case Core.FleetCategory.NPC:
                    case Core.FleetCategory.PlayerLoose:
                        continue;
                    //show these
                    case Core.FleetCategory.PlayerMobile:
                    case Core.FleetCategory.PlayerCustomCityFedMobile:
                    case Core.FleetCategory.PlayerCustomUnattachedMobile:
                    case Core.FleetCategory.PlayerBattlestation:
                    case Core.FleetCategory.PlayerPlanetaryCommand:
                    case Core.FleetCategory.PlayerCustomCity:
                        break;
                }

                FleetCategoryPurpose purpose = fleet.GetFleetFleetCategoryPurpose();
                if ( purpose != FleetCategoryPurpose.None )
                    myFleetsByPurpose[purpose].Add( fleet );
            }
            #endregion

            #region Sort all of the myFleetsByPurpose lists and add their categories if they are to be there
            for ( FleetCategoryPurpose i = FleetCategoryPurpose.None; i < FleetCategoryPurpose.Length; i++ )
            {
                List<Fleet> fleets = myFleetsByPurpose[i];
                //if we have content, then sort it!
                if ( fleets.Count > 0 )
                {
                    fleets.Sort( static delegate ( Fleet left, Fleet right )
                    {
                        int val = (left.TiedToKeybindIndexOneIndexed <= 0).CompareTo( right.TiedToKeybindIndexOneIndexed <= 0 );
                        if ( val != 0 )
                            return val;
                        val = left.GetName().CompareTo( right.GetName() );
                        if ( val != 0 ) return val;
                        val = left.GetSocketConstrainedMaxStrength_ForUIOnly( false ).CompareTo( right.GetSocketConstrainedMaxStrength_ForUIOnly( false ) );
                        return val;
                    } );
                }
            }
            #endregion
        }
        #endregion

        #region btnFleetCategory
        public class btnFleetCategory : ButtonAbstractBase
        {
            public static btnFleetCategory Original;
            public btnFleetCategory() { if ( Original == null ) Original = this; }

            public FleetCategory ParentCategory;
            private string lastDisplayName = string.Empty;

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.ParentCategory != null )
                {
                    if ( this.ParentCategory.Purpose == FleetCategoryPurpose.DZEconomy )
                        Window_DZEconomySidebarPopout.Instance?.Toggle();
                    else if ( this.ParentCategory.Purpose == FleetCategoryPurpose.DZLogistics )
                        Window_DZLogisticsSidebarPopout.Instance?.Toggle();
                    else if ( this.ParentCategory.Purpose == FleetCategoryPurpose.DZTechTree )
                        Window_DZTechTreePopout.Instance?.Toggle();
                    else
                        this.ParentCategory.ThisCategoryIsOpen = !this.ParentCategory.ThisCategoryIsOpen;
                }
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( this.ParentCategory != null && this.ParentCategory.Purpose == FleetCategoryPurpose.DZEconomy )
                {
                    bool isOpen = Window_DZEconomySidebarPopout.Instance?.GetIsOpen() ?? false;
                    Buffer.Add( isOpen ? "(-) " : "(+) " ).Add( "暗黑天顶经济", "a1d4ff" );
                    return;
                }

                if ( this.ParentCategory != null && this.ParentCategory.Purpose == FleetCategoryPurpose.DZLogistics )
                {
                    bool isOpen = Window_DZLogisticsSidebarPopout.Instance?.GetIsOpen() ?? false;
                    Buffer.Add( isOpen ? "(-) " : "(+) " ).Add( "暗黑天顶物流", "a1d4ff" );
                    return;
                }

                if ( this.ParentCategory != null && this.ParentCategory.Purpose == FleetCategoryPurpose.DZTechTree )
                {
                    bool isOpen = Window_DZTechTreePopout.Instance?.GetIsOpen() ?? false;
                    Buffer.Add( isOpen ? "(-) " : "(+) " ).Add( "暗黑天顶科技树", "a1d4ff" );
                    return;
                }

                if ( this.ParentCategory == null || this.ParentCategory.ThisCategoryIsOpen )
                    Buffer.Add( "(-) " );
                else
                    Buffer.Add( "(+) " );

                if ( this.ParentCategory != null )
                    this.lastDisplayName = this.ParentCategory.Purpose.GetShortDisplayName();

                Buffer.Add( this.lastDisplayName ).Add( "   x" );
                if ( this.ParentCategory == null )
                    Buffer.Add( "??" );
                else
                    Buffer.Add( this.ParentCategory.FleetsInCategory.Count );
            }
            public override void HandleMouseover()
            {
                if ( this.ParentCategory == null || this.ParentCategory.Purpose == FleetCategoryPurpose.None )
                    return;
                string tooltipText = this.ParentCategory.Purpose.GetDescription();
                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipText, "GeneralTooltipScale" );
            }
            public override bool GetShouldBeHidden()
            {
                return this.ParentCategory == null || this.ParentCategory.Purpose == FleetCategoryPurpose.None;
            }
        }
        #endregion

        #region btnFleet
        public class btnFleet : ImageButtonAbstractBase
        {
            public static btnFleet Original;
            public btnFleet() { if ( Original == null ) Original = this; }

            public Fleet FleetShown;
            public FleetCategory ParentCategory;

            public override void Clear()
            {
                this.FleetShown = null;
                this.ParentCategory = null;
            }

            public bool DebugUpdateContentFromVolatile = false;
            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {

                int debugStage = 0;
                try
                {
                    debugStage = 5;

                    Fleet fleet = this.FleetShown;

                    if ( fleet == null )
                    {
                        debugStage = 10;
                        SubTexts[0].Text.StartWritingToBuffer().Add( "???" );
                        debugStage = 11;
                    }
                    else
                    {
                        debugStage = 14;
                        //Faction localGlobalFaction = World_AIW2.Instance.GetLocalPlayerFaction();
                        //PlanetFaction localPlanetFaction = planet.GetPlanetFactionForFaction( localGlobalFaction );
                        debugStage = 15;
                        int strength = fleet.GetCurrentStrengthOfFleet_ForUIOnly( false );
                        debugStage = 151;
                        int fullCap = fleet.GetMaxCountOfFleet_ForUIOnly( false );
                        debugStage = 152;
                        int builtCount = fleet.GetCurrentCountOfFleet_ForUIOnly();
                        debugStage = 153;
                        long maxHealth = fleet.GetMaxHullOfFleet_ForUIOnly( false );
                        debugStage = 154;
                        long currentHealth = fleet.GetCurrentHullOfFleet_ForUIOnly();
                        debugStage = 16;
                        GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                        debugStage = 161;
                        Planet planet = centerpiece == null ? fleet.Planet : centerpiece.Planet;
                        if ( planet == null )
                        {
                            debugStage = 162;
                            if ( fleet.Planet != null )
                                planet = fleet.Planet;
                        }

                        if ( maxHealth < 0 )
                            maxHealth = 1;

                        debugStage = 17;
                        double healthPercent = (double)currentHealth / (double)maxHealth;

                        debugStage = 20;

                        bool isOpenFleetManagementWindow = Window_FleetManagementSidebarPopout.Instance.Is_Showing( fleet );

                        //main text - name of the fleet
                        {
                            ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                            buffer.StartColor( isOpenFleetManagementWindow ? ColorMath.BrightLeaf :
                                (healthPercent < 0.33 ? ColorMath.Orange : (healthPercent < 0.66 ? ColorMath.LightYellow : Color.white)) )
                                .Add( fleet.GetName() );
                            if ( fleet.IsFleetInTransportLoadMode )
                                buffer.EndColor().StartColor( "54b1ff" ).Add( "  <voffset=0.2em><b>(T)</b></voffset>" ).EndColor();
                            debugStage = 21;
                            SubTexts[0].Text.FinishWritingToBuffer();
                        }

                        debugStage = 30;
                        //ship cap text
                        if ( fleet.Category == Core.FleetCategory.PlayerCustomCity )
                        {
                            int totalSockets = fleet.CalculateTotalCitySockets();
                            int spentSockets = fleet.CalculateSpentCitySockets();
                            SubTexts[1].Text.StartWritingToBuffer().StartColor( spentSockets < totalSockets ? Color.white : ColorMath.LightGreen ).Add(
                                "<size=80%>" ).Add( spentSockets ).Add( "/" ).Add( totalSockets ).Add( " " ).Add( centerpiece?.TypeData.NameForCitySockets_Plural ?? "插槽" ).Add( "</size>" );
                        }
                        else
                        {
                            SubTexts[1].Text.StartWritingToBuffer().StartColor( builtCount < fullCap ? Color.white : ColorMath.LightGreen ).Add(
                                builtCount ).Add( "/" ).Add( fullCap );
                        }
                        debugStage = 31;
                        SubTexts[1].Text.FinishWritingToBuffer();

                        debugStage = 40;
                        if ( fleet.Category != Core.FleetCategory.PlayerCustomCity )
                        {
                            if ( centerpiece != null && centerpiece.GetIsCrippled() )
                            {
                                //crippled text
                                SubTexts[2].Text.StartWritingToBuffer().StartColor( ColorMath.Gray ).Add( "<size=80%>损毁</size>" );
                                SubTexts[2].Text.StartWritingToBuffer().StartColor( healthPercent < 0.33 ? ColorMath.LightOrange : (healthPercent < 0.66 ? ColorMath.LightYellow : ColorMath.LightGreen) )
                                    .Add( (int)System.Math.Round( healthPercent * 100 ) ).Add( "%" );
                            }
                            else
                            {
                                //health text
                                SubTexts[2].Text.StartWritingToBuffer().StartColor( healthPercent < 0.33 ? ColorMath.LightOrange : (healthPercent < 0.66 ? ColorMath.LightYellow : ColorMath.LightGreen) )
                                    .Add( (int)System.Math.Round( healthPercent * 100 ) ).Add( "%" );
                            }
                            debugStage = 41;
                            SubTexts[2].Text.FinishWritingToBuffer();
                        }
                        else
                        {
                            debugStage = 41;
                            SubTexts[2].Text.StartWritingToBuffer();
                            SubTexts[2].Text.FinishWritingToBuffer();
                        }

                        debugStage = 50;
                        //planet text
                        {
                            ArcenDoubleCharacterBuffer buffer = SubTexts[3].Text.StartWritingToBuffer();
                            ArcenExternalUIUtilities.AddSizeAndVOssetToText( buffer, ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon, 7, -1 );
                            buffer.StartColor( ArcenExternalUIUtilities.StrengthTextColor );
                            buffer.Add( " " );
                            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, strength, true, true );
                            buffer.EndColor().Add( "    " );

                            if ( planet == null )
                                buffer.Add( "???" );
                            else
                            {
                                Faction controllingFaction = planet.GetControllingFaction();
                                if ( controllingFaction != null )
                                    buffer.StartColor( controllingFaction.FactionCenterColor.TeamColorBrighter );
                                buffer.Add( planet.Name );
                            }

                            debugStage = 51;
                            SubTexts[3].Text.FinishWritingToBuffer();
                        }

                        debugStage = 60;
                        //keybind text
                        int keybindIndexForDisplay = fleet.GetTiedToKeybindIndexForDisplay();
                        ArcenCharacterBufferBase keybindBuf = SubTexts[4].Text.StartWritingToBuffer();
                        if ( keybindIndexForDisplay >= 0 )
                            keybindBuf.Add( keybindIndexForDisplay );
                        debugStage = 61;
                        SubTexts[4].Text.FinishWritingToBuffer();
                    }
                    debugStage = 20;
                    debugStage = 21;
                }
                catch ( Exception e )
                {
                    if ( DebugUpdateContentFromVolatile )
                        ArcenDebugging.ArcenDebugLog( "Exception in btnFleetIcon.UpdateContentFromVolatile at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                }
            }

            #region WriteFleetTooltip
            public static void WriteFleetTooltip( Fleet fleet, ArcenDoubleCharacterBuffer buffer )
            {
                bool debug_ShowFleetShipLineNumbers = GameSettings.Current.GetBoolBySetting( "Debug_ShowFleetShipLineNumbers" );

                int debugStage = 0;
                try
                {
                    debugStage = 100;
                    if ( fleet == null )
                        buffer.Add( "错误：舰队为空！  " );
                    else
                    {
                        int currentStrength = fleet.GetCurrentStrengthOfFleet_ForUIOnly( false );
                        int maxStrength = fleet.GetSocketConstrainedMaxStrength_ForUIOnly( false );
                        debugStage = 2000;
                        buffer.Add( "<b>" ).Add( fleet.GetName() ).Add( "</b>" );
                        buffer.Add( "<pos=300>最大战力：" );
                        EntityText.AddSingleValueStrengthOnly( buffer, maxStrength );
                        buffer.Add( "\n<color=#777777>舰队类型：" ).Add( fleet.GetFleetFleetCategoryPurpose().GetFullDisplayName() );
                        buffer.Add( "</color>" );

                        debugStage = 2100;
                        GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();

                        if ( fleet.Category == Core.FleetCategory.PlayerPlanetaryCommand ||
                            fleet.Category == Core.FleetCategory.PlayerBattlestation )
                        {
                            debugStage = 2200;
                            string resourceName = fleet.GetResourceNameNeededForNextLevelUp();
                            debugStage = 2210;
                            buffer.Add( "\n<color=#777777>" )
                                .Add( fleet.Centerpiece.GetSquad().TypeData.ThisCenterpieceGrantsItsDirectScienceUpgradesToRestOfFleet ? "整个舰队" : "核心舰" )
                                .Add( " 等级来自 " ).Add( resourceName ).Add( "：</color> " ).AddNumberMoreReadable( fleet.AddedMarkLevelsForFleet_FromScience ).Add( " (" )
                                .AddNumberMoreReadable( fleet.GetScienceOrOtherResourceNeededForNextLevelUp( fleet.AddedMarkLevelsForFleet_FromScience ) ).Add( " 升级)" );
                        }
                        else if ( centerpiece != null && centerpiece.TypeData.IsUpgradeableByDirectScience )
                        {
                            debugStage = 2300;
                            string resourceName = fleet.GetResourceNameNeededForNextLevelUp();
                            debugStage = 2310;
                            buffer.Add( "\n<color=#777777>" )
                                .Add( centerpiece.TypeData.ThisCenterpieceGrantsItsDirectScienceUpgradesToRestOfFleet ? "整个舰队" : "旗舰" )
                                .Add( " 等级来自 " ).Add( resourceName ).Add( "：</color> " ).AddNumberMoreReadable( fleet.AddedMarkLevelsForFleet_FromScience ).Add( " (" )
                                 .AddNumberMoreReadable( fleet.GetScienceOrOtherResourceNeededForNextLevelUp( fleet.AddedMarkLevelsForFleet_FromScience ) ).Add( " 升级)" );
                        }
                        else if ( fleet.AddedMarkLevelsForFleet_FromScience > 0 )
                        {
                            debugStage = 2400;
                            string resourceName = fleet.GetResourceNameNeededForNextLevelUp();
                            debugStage = 2410;
                            buffer.Add( "\n<color=#777777>舰队等级来自 " ).Add( resourceName ).Add( " (意外)：</color> " ).Add( fleet.AddedMarkLevelsForFleet_FromScience );
                        }

                        debugStage = 2500;
                        buffer.Add( "\n<color=#777777>舰队战力（当前/最大）：</color> " );
                        EntityText.AddSingleValueStrengthOnly( buffer, currentStrength );
                        buffer.Add( " / " );
                        EntityText.AddSingleValueStrengthOnly( buffer, maxStrength );

                        debugStage = 2600;
                        bool hasNeedOfFactories = false;
                        switch ( fleet.Category )
                        {
                            case Core.FleetCategory.PlayerMobile:
                            case Core.FleetCategory.PlayerCustomCityFedMobile:
                            case Core.FleetCategory.PlayerCustomUnattachedMobile:
                            case Core.FleetCategory.PlayerBattlestation:
                                debugStage = 2700;
                                if ( centerpiece != null && centerpiece.GetIsCrippled() )
                                {
                                    buffer.StartColor( QuickColors.OldValue );
                                    buffer.Add( "\n建造已停止：旗舰损毁" );
                                    buffer.EndColor();
                                }
                                else if ( centerpiece != null && centerpiece.GetIsNonFunctional() )
                                {
                                    buffer.StartColor( QuickColors.OldValue );
                                    buffer.Add( "\n建造已停止：旗舰失效" );
                                    buffer.EndColor();
                                }
                                else if ( fleet.IsFleetConstructionPaused )
                                {
                                    buffer.StartColor( QuickColors.OldValue );
                                    buffer.Add( "\n建造已停止：手动暂停" );
                                    buffer.EndColor();
                                }
                                else if ( fleet.IsFleetConstructionBlocked )
                                {
                                    buffer.StartColor( QuickColors.OldValue );
                                    buffer.Add( "\n建造被阻：敌方行动" );
                                    buffer.EndColor();
                                }

                                hasNeedOfFactories = true;

                                debugStage = 2800;
                                if ( fleet.IsFleetInTransportLoadMode )
                                {
                                    buffer.StartColor( QuickColors.NewValue );
                                    buffer.Add( "\n舰队处于运输装载模式，所有单位尝试进入旗舰。" );
                                    buffer.EndColor();
                                }
                                break;
                            case Core.FleetCategory.PlayerCustomCity:
                                debugStage = 2900;
                                Fleet currentlyBolstering = fleet.GetFleetBolsteredByThisCity();
                                if (currentlyBolstering != null || fleet.GetCanBolsterAnything()) {
                                    buffer.NewLine().StartColor("777777");
                                    buffer.Add( fleet.Centerpiece.GetSquad().TypeData.NameForCityCenter ).Add( " 支援：" );
                                    buffer.EndColor();


                                    if ( currentlyBolstering != null )
                                    {
                                        buffer.StartColor( QuickColors.NewValue );
                                        buffer.Add( currentlyBolstering.GetName() );
                                        buffer.EndColor();
                                    }
                                    else
                                    {
                                        buffer.StartColor( QuickColors.OldValue );
                                        buffer.Add( "暂无！" );
                                        buffer.EndColor();
                                    }
                                }
                                int totalCityPoints = fleet.CalculateTotalCitySockets();
                                if ( totalCityPoints > 0 )
                                {
                                    int spentCityPoints = fleet.CalculateSpentCitySockets();
                                    buffer.NewLine().StartColor("777777");
                                    buffer.Add( centerpiece.TypeData.NameForCitySockets_Plural ).Add( " 已用：" );
                                    buffer.EndColor();
                                    buffer.Add( spentCityPoints ).Add( "/" ).Add( totalCityPoints );
                                }
                                break;
                        }
                        debugStage = 3000;
                        if ( !hasNeedOfFactories )
                        {
                            debugStage = 3100;
                            foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                            {
                                if ( mem == null )
                                    continue;
                                GameEntityTypeData typeData = mem.TypeData;
                                if ( typeData == null )
                                    continue;
                                if ( typeData.IsDrone || typeData.SelfConstructs )
                                    continue;
                                debugStage = 3300;
                                if ( mem.GetCanBuildAnother( true, -1, ExtraFromStacks.IncludePrecalc ) != ArcenRejectionReason.Unknown )
                                    continue;
                                hasNeedOfFactories = true;
                                break;
                            }
                        }
                        debugStage = 4000;
                        if ( hasNeedOfFactories )
                        {
                            debugStage = 4100;
                            if ( fleet.SupportingFactoriesInRange.Count > 0 )
                                buffer.StartColor( QuickColors.NewValue );
                            else
                                buffer.StartColor( QuickColors.OldValue );
                            debugStage = 4200;
                            buffer.Add( "\n范围内工厂：" ).Add( fleet.SupportingFactoriesInRange.Count );
                            buffer.EndColor();
                        }

                        debugStage = 5000;
                        switch ( fleet.Category )
                        {
                            case Core.FleetCategory.PlayerMobile:
                            case Core.FleetCategory.PlayerCustomCityFedMobile:
                            case Core.FleetCategory.PlayerCustomUnattachedMobile:
                            case Core.FleetCategory.PlayerBattlestation:
                                {
                                    debugStage = 5100;
                                    buffer.Add( "\n<color=#777777>旗舰移动模式：</color> " );
                                    if ( fleet.IsFleetFlagshipAllowedToUseMovementModes )
                                        buffer.Add( "<color=#ffc74f>按指令巡逻</color>" );
                                    else
                                        buffer.Add( "<color=#4ff9ff>未获指令时驻守</color>" );
                                }
                                break;
                        }

                        buffer.Add( "\n" );

                        debugStage = 6000;

                        foreach ( FleetMembership mem in fleet.MemberGroupsSorted_NonSim_ForUIOnly_SeriouslyDoNotCallFromBGCode() )
                        {
                            debugStage = 6100;
                            GameEntityTypeData typeData = mem.TypeData;
                            if ( typeData == null )
                                continue;
                            debugStage = 6200;

                            int effectiveHere = mem.GetCountPresent( true, ExtraFromStacks.IncludePrecalc );
                            if ( mem.EffectiveSquadCap <= 0 && !typeData.CannotActuallyBeBuilt_IsNotAShipLine ) //if CannotActuallyBeBuilt_IsNotAShipLine, then DO show it here
                            {
                                //if there's no cap for a unit type, and none present, don't show it.
                                //this is something that previously existed here but not longer does.
                                if ( effectiveHere <= 0 )
                                    continue;
                            }

                            bool isFleetLeader = typeData.IsFleetLeader;
                            //don't draw old fleet leaders that aren't here anymore.
                            //typically happens from switching command station types.
                            if ( isFleetLeader && effectiveHere <= 0 )
                                continue;
                            buffer.AddShipIconInline(typeData, World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull());
                            buffer.Add( typeData == null ? "nulltype" : typeData.DisplayName );

                            if ( debug_ShowFleetShipLineNumbers )
                            {
                                buffer.Add( "-" ).Add( mem.UniqueTypeDataDifferentiatorForDuplicates );
                            }

                            GameEntityTypeData.MarkLevelStats forMark = mem.ForMark;

                            if ( forMark != null )
                                buffer.StartColor( forMark.MarkLevel.ColorHex );
                            buffer.Add( " " ).Add( forMark == null ? "nullmark" : forMark.MarkLevel.Abbreviation );
                            if ( forMark != null )
                                buffer.EndColor();

                            if ( mem.EffectiveSquadCap <= 0 )
                            {
                                buffer.Add( "\n" );
                                continue;
                            }
                            if ( !isFleetLeader )
                            {
                                if ( mem.EffectiveSquadCap <= 0 )
                                {
                                    if ( effectiveHere > 0 )
                                        buffer.StartColor( QuickColors.NewValue );
                                    else
                                        buffer.StartColor( QuickColors.OldValue );
                                    buffer.Add( " x" ).Add( effectiveHere );
                                    buffer.EndColor();
                                }
                                else
                                {
                                    if ( effectiveHere >= mem.EffectiveSquadCap )
                                        buffer.StartColor( QuickColors.NewValue );
                                    else
                                        buffer.StartColor( ColorMath.Orange );
                                    buffer.Add( " x" ).Add( effectiveHere ).Add( "/" ).Add( mem.EffectiveSquadCap );
                                    buffer.EndColor();
                                }

                                if ( mem.TransportContents.Count > 0 )
                                    buffer.StartColor( ColorMath.IceBlue ).Add( " (运送中 " ).Add( mem.CalculateTransportedContentsCount() ).Add( ")" ).EndColor();

                                if ( mem.IsFleetMembershipConstructionPaused )
                                    buffer.StartColor( QuickColors.OldValue ).Add( " (已暂停)" ).EndColor();
                            }
                            else //yes is a fleet leader
                            {
                                ArcenOverLinkedList<GameEntity_Squad>.ArcenLinkedListItem firstItem = mem.EntitiesOfFMem.GetFirst();
                                if ( firstItem == null )
                                    buffer.StartColor( QuickColors.OldValue ).Add( " (缺失)" ).EndColor();
                                else
                                {
                                    GameEntity_Squad centerpieceInner = firstItem.Contained;
                                    if ( centerpieceInner == null )
                                        buffer.StartColor( QuickColors.OldValue ).Add( " (缺失)" ).EndColor();
                                    else if ( centerpieceInner.GetIsCrippled() )
                                        buffer.StartColor( QuickColors.OldValue ).Add( " (损毁)" ).EndColor();
                                    else if ( centerpieceInner.GetIsNonFunctional() )
                                        buffer.StartColor( QuickColors.OldValue ).Add( " (失效)" ).EndColor();
                                }
                            }

                            buffer.Add( "\n" );
                        }
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "WriteFleetTooltip exception at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                }
            }
            #endregion
            
            public override void HandleMouseover()
            {
                Fleet fleet = this.FleetShown;
                if ( fleet == null )
                    return;
                World_AIW2.Instance.FocusedSquadForMapDarkening = fleet.Centerpiece.GetSquad();

                if ( Window_FleetManagementSidebarPopout.Instance.Is_Showing( fleet ) )
                    return; //don't show the tooltip for the fleet we've got the management window open for

                bool showVerboseDetails = InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1();
                bool showFleetEffectiveness = InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key2();
                bool showEnemyMetrics = showFleetEffectiveness && InputCaching.CalculateHoldToSeeShipStrengthsAndWeaknesses();

                var buffer = Window_AtMouseTooltipPanelBesideSidebar.TooltipBuffer;
                if ( showFleetEffectiveness )
                {
                    if ( showVerboseDetails )
                        (fleet.BaseInfo as FleetMetricsBaseInfo)?.AddMetricsToTooltipForFleet( buffer );
                    else if ( showEnemyMetrics )
                        (fleet.BaseInfo as FleetMetricsBaseInfo)?.AppendEnemyMetricsCompact( buffer );
                    else
                        (fleet.BaseInfo as FleetMetricsBaseInfo)?.AppendFleetMetricsCompact( buffer );
                }
                else
                {
                    WriteFleetTooltip( fleet, buffer );
                    buffer.Add( "<size=80%><color=#3f6c9e>右键点击</color><color=#4486d1>选择此舰队（按住Shift可添加到当前选中）。\n" );
                    buffer.Add( "<color=#3f6c9e>中键点击</color>将视角居中于舰队核心。</color></size>  " );
                    if ( ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() )
                    {
                        string key2 = InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key2" );
                        string key3 = InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "HoldToSeeShipStrengthsAndWeaknesses" );
                        buffer.Add( "\n" );
                        buffer.Add( "<color=#d18444>按住 <color=#996f4c>" ).Add( key2 ).Add( "</color> 查看舰队效能指标。</color>\n" );
                        buffer.Add( "<color=#d18444>按住 <color=#996f4c>" ).Add( key2 ).Add( " + " ).Add( key3 ).Add( "</color> 查看敌方指标。</color>\n" );
                        buffer.Add( "<color=#d18444>按住 <color=#996f4c>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key1" ) ).Add( " + " ).Add( key2 ).Add( "</color> 查看详细指标。</color>\n" );
                    }
                }

                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( buffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
            }
            public override MouseHandlingResult HandleClick( MouseHandlingInput input )
            {
                Fleet fleet = this.FleetShown;
                if ( fleet == null )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( "BUG: FleetShown is null", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                //when right-clicking, select the fleet
                if ( input.RightButtonClicked )
                {
                    Input_FleetHandler.SelectFleetGroup( -1, fleet, FleetSelectionType.SelectOnly, GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    return MouseHandlingResult.None;
                }
                //when middle-clicking, center on the fleet
                if ( input.MiddleButtonClicked )
                {
                    Input_FleetHandler.SelectFleetGroup( -1, fleet, FleetSelectionType.CenterOnly, GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    return MouseHandlingResult.None;
                }

                Window_FleetManagementSidebarPopout.Instance.Open( fleet );
                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                return this.FleetShown == null || this.ParentCategory == null || !this.ParentCategory.ThisCategoryIsOpen ||
                    this.ParentCategory.Button == null || this.ParentCategory.Button.GetShouldBeHidden();
            }
        }
        #endregion

        #region FleetCategory
        public class FleetCategory : PoolableGUIGroup
        {
            public bool ThisCategoryIsOpen = true;

            public btnFleetCategory Button;
            public FleetCategoryPurpose Purpose;
            public readonly List<Fleet> FleetsInCategory = List<Fleet>.Create_WillNeverBeGCed( 300, "Window_InGameSidebarFleets-FleetCategory-FleetsInCategory" );

            private ImageButtonAbstractBase.ImageButtonPool<btnFleet> btnFleetPool;

            public FleetCategory( btnFleetCategory But )
            {
                this.Button = But;
                this.Button.ParentCategory = this;
            }

            public override void PostInit()
            {
                btnFleetPool = new ImageButtonAbstractBase.ImageButtonPool<btnFleet>( btnFleet.Original, 1 );
            }

            public bool DebugUpdate = true;
            public override void Update()
            {
                throw new Exception( "Not really using this update method..." );
            }

            public const float TEXT_ROW_HEIGHTS = 25f;
            public const float ROW_ADVANCE_WHEN_CLOSED = 5f;

            public void Update( ref float currentY, ref int RemainingSubItemsCanAdd )
            {
                int debugStage = 0;
                try
                {
                    debugStage = 10;
                    btnFleetPool.Clear( RemainingSubItemsCanAdd );
                    if ( this.Purpose == FleetCategoryPurpose.None )
                        return;

                    debugStage = 11;
                    Faction localPlayerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                    debugStage = 12;
                    if ( localPlayerFaction == null )
                        return;

                    Fleet item = null;
                    if ( RemainingSubItemsCanAdd > 0 )
                    {
                        for ( int j = 0; j < this.FleetsInCategory.Count; j++ )
                        {
                            item = this.FleetsInCategory[j];
                            debugStage = 22;
                            if ( item == null )
                                continue;
                            debugStage = 23;
                            btnFleet itemButton = btnFleetPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                            if ( itemButton == null )
                                break; //time slicing, too many added right now
                            debugStage = 25;
                            itemButton.FleetShown = item;
                            itemButton.ParentCategory = this;
                            debugStage = 26;
                        }
                    }

                    RemainingSubItemsCanAdd = btnFleetPool.GetRemainingAllowedToAddBeforeNextClear();

                    debugStage = 41;
                }
                catch ( Exception e )
                {
                    if ( DebugUpdate )
                        ArcenDebugging.ArcenDebugLog( "Exception in FleetCategory.DebugUpdate at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                }

                #region Positioning Logic
                RectTransform rTran = null;
                {
                    rTran = this.Button.Element.RelevantRect;
                    rTran.anchoredPosition = new Vector2( 0, currentY );
                    currentY -= TEXT_ROW_HEIGHTS;

                    if ( this.ThisCategoryIsOpen )
                        btnFleetPool.ApplyItemsInRows( 0, ref currentY, 32f, 180, 30f );
                    else
                        currentY -= ROW_ADVANCE_WHEN_CLOSED;
                }
                #endregion
            }

            public override void Clear()
            {
                this.Purpose = FleetCategoryPurpose.None;
                this.FleetsInCategory.Clear();
            }
            public override PoolableGUIGroup DuplicateSelf()
            {
                btnFleetCategory newCategoryBut = (btnFleetCategory)this.Button.DuplicateSelf();
                //int siblingIndex = btnQuickDefenses.Instance.Element.transform.GetSiblingIndex();
                //newCategoryBut.Element.GO.transform.SetSiblingIndex( siblingIndex );
                return new FleetCategory( newCategoryBut );
            }
        }
        #endregion
    }

    public enum FleetCategoryPurpose
    {
        None = 0,
        MobileOfficerFleetFlagship,
        MobileStrikeFleetFlagship,
        MobileSupportFleetFlagship,
        BattlestationBasic,
        BattlestationCitadel,
        CityCenter,
        PlanetCommand,
        DZEconomy,
        DZLogistics,
        DZTechTree,
        Length
    }

    public static class FleetCategoryPurposeExtensionMethods
    {
        public static string GetShortDisplayName( this FleetCategoryPurpose Purpose )
        {
            switch ( Purpose )
            {
                case FleetCategoryPurpose.BattlestationBasic:
                    return "战斗堡垒";
                case FleetCategoryPurpose.BattlestationCitadel:
                    return "城堡";
                case FleetCategoryPurpose.CityCenter:
                    return "城市中心";
                case FleetCategoryPurpose.PlanetCommand:
                    return "指挥站";
                case FleetCategoryPurpose.MobileOfficerFleetFlagship:
                    return "机动军官";
                case FleetCategoryPurpose.MobileStrikeFleetFlagship:
                    return "机动打击";
                case FleetCategoryPurpose.MobileSupportFleetFlagship:
                    return "机动支援";
                case FleetCategoryPurpose.DZEconomy:
                    return "暗黑天顶经济";
                case FleetCategoryPurpose.DZLogistics:
                    return "暗黑天顶物流";
                case FleetCategoryPurpose.DZTechTree:
                    return "暗黑天顶科技树";
                default:
                    return "ERR:" + Purpose;
            }
        }

        public static string GetFullDisplayName( this FleetCategoryPurpose Purpose )
        {
            switch ( Purpose )
            {
                case FleetCategoryPurpose.BattlestationBasic:
                    return "战斗堡垒";
                case FleetCategoryPurpose.BattlestationCitadel:
                    return "城堡";
                case FleetCategoryPurpose.CityCenter:
                    return "城市中心";
                case FleetCategoryPurpose.PlanetCommand:
                    return "行星指挥站";
                case FleetCategoryPurpose.MobileOfficerFleetFlagship:
                    return "机动战斗军官编队";
                case FleetCategoryPurpose.MobileStrikeFleetFlagship:
                    return "机动战斗打击编队";
                case FleetCategoryPurpose.MobileSupportFleetFlagship:
                    return "机动支援";
                case FleetCategoryPurpose.DZEconomy:
                    return "暗黑天顶经济";
                case FleetCategoryPurpose.DZLogistics:
                    return "暗黑天顶物流";
                case FleetCategoryPurpose.DZTechTree:
                    return "暗黑天顶科技树";
                default:
                    return "ERR:" + Purpose;
            }
        }

        public static string GetDescription( this FleetCategoryPurpose Purpose )
        {
            switch ( Purpose )
            {
                case FleetCategoryPurpose.BattlestationBasic:
                    return "通用战斗堡垒，可用于保卫行星或在敌方行星上建立进攻桥头堡。";
                case FleetCategoryPurpose.BattlestationCitadel:
                    return "城堡是通用战斗堡垒的强化版本，本身拥有强大的战斗力，同时可为行星防御或敌方行星上的进攻桥头堡提供防御设施。";
                case FleetCategoryPurpose.CityCenter:
                    return "城市中心在不同阵营中具有不同用途。";
                case FleetCategoryPurpose.PlanetCommand:
                    return "你在行星上的每个指挥站都有一支以基础、实用或经济单位为主的小型部队。但是，如果你从天顶商人处购买了其他独特的俘获物，这里也可能有一些威力惊人的固定位置武器。";
                case FleetCategoryPurpose.MobileOfficerFleetFlagship:
                    return "一支非常灵活的战斗力量，拥有大量小型打击艇和护卫舰，中央还有一艘庞大、可怕、令人兴奋的旗舰。这艘旗舰本身通常就是一种威胁，可能是方舟或傀儡，但也可以作为所有小型单位的运输载体。";
                case FleetCategoryPurpose.MobileStrikeFleetFlagship:
                    return "你最灵活的战斗力量：通常用于进攻，但也能够根据需要回防你的行星。完全由小型打击艇和护卫舰组成，围绕一个在战斗中基本无用的核心运输舰。所有军事力量都来自你的中小型单位，运输舰则负责将这些单位快速运送到各处，并在工厂范围内批量生产新单位。";
                case FleetCategoryPurpose.MobileSupportFleetFlagship:
                    return "不寻常的支援舰队，能够作为你进攻或防守舰队的力量倍增器、在远程打击中提供远程补给，或执行其他令人意想不到的次要任务。";
                case FleetCategoryPurpose.DZEconomy:
                    return "打开暗黑天顶经济管理面板，用于控制柱头生产、查看资源短缺情况，以及切换单个柱头的锁定/优先级。";
                case FleetCategoryPurpose.DZLogistics:
                    return "打开暗黑天顶物流面板，查看终端以及往返于它们之间的运输舰。";
                case FleetCategoryPurpose.DZTechTree:
                    return "打开暗黑天顶科技树，显示所有可用升级、其前置条件，并允许你选择升级柱头的下一个研究方向。";
                default:
                    return "ERR:" + Purpose;
            }
        }
    }

    public static class FleetExtends
    {
        #region GetFleetFleetCategoryPurpose
        public static FleetCategoryPurpose GetFleetFleetCategoryPurpose( this Fleet fleet )
        {
            GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
            //when there's no centerpiece
            if ( centerpiece == null )
                return FleetCategoryPurpose.None;

            switch ( fleet.Category )
            {
                case Core.FleetCategory.PlayerMobile:
                case FleetCategory.PlayerCustomCityFedMobile:
                case FleetCategory.PlayerCustomUnattachedMobile:
                    switch ( centerpiece.TypeData.SpecialType )
                    {
                        case SpecialEntityType.MobileStrikeCombatFleetFlagship:
                        case SpecialEntityType.MobileCustomUnattachedFleetFlagship:
                            return FleetCategoryPurpose.MobileStrikeFleetFlagship;
                        case SpecialEntityType.MobileSupportFleetFlagship:
                            return FleetCategoryPurpose.MobileSupportFleetFlagship;
                        default:
                        case SpecialEntityType.MobileOfficerCombatFleetFlagship:
                            return FleetCategoryPurpose.MobileOfficerFleetFlagship;
                    }
                case Core.FleetCategory.PlayerBattlestation:
                    switch ( centerpiece.TypeData.SpecialType )
                    {
                        case SpecialEntityType.BattlestationCitadel:
                            return FleetCategoryPurpose.BattlestationCitadel;
                        default:
                        case SpecialEntityType.BattlestationBasic:
                            return FleetCategoryPurpose.BattlestationBasic;
                    }
                case Core.FleetCategory.PlayerCustomCity:
                    switch ( centerpiece.TypeData.SpecialType )
                    {
                        default:
                        case SpecialEntityType.CityCenter:
                            return FleetCategoryPurpose.CityCenter;
                    }
                case Core.FleetCategory.PlayerPlanetaryCommand:
                    return FleetCategoryPurpose.PlanetCommand;
                default:
                    return FleetCategoryPurpose.None;
            }
        }
        #endregion
    }
}
