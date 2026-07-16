using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;

namespace Arcen.AIW2.External
{
    public class Window_FleetManagementSidebarPopout : Window_DynamicallyFilledAbstractBase
    {        
        public static Window_FleetManagementSidebarPopout Instance;
        
        private Fleet _fleetToManage;
        private bool _open;
        
        public Window_FleetManagementSidebarPopout()
        {
            Instance = this;
            
            this.topBuffer = -3;
            this.leftBuffer = 5;
            this.rowHeight = ROW_HEIGHT_DEFAULT;
            this.rowBuffer = 1.5f;
        }

        #region Open/Close Stuff

        public override void Close()
        {
            _open = false;
        }
        
        public void Open( Fleet fleet )
        {
            _open = true;
            _fleetToManage = fleet;
        }
        
        public bool GetIsOpen()
        {
            return _open;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            return _open;
        }

        public override void OnHideAfterShowing()
        {
            _open = false;
            base.OnHideAfterShowing();
        }
        
        public void Toggle_Show( Fleet fleet )
        {
            if (_open && 
                _fleetToManage == fleet)
            {
                this.Close();
                return;
            }
            
            _open = true;
            _fleetToManage = fleet;
        }

        public bool Is_Showing( Fleet fleet )
        {
            return _open && 
                   _fleetToManage == fleet;
        }

        public class bClose : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }
        }
        
        #endregion

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "管理舰队：" ).Add( Instance._fleetToManage.GetName(), "a1a1ff" );
            }
        }
        #endregion
        
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

        public class customParent : CustomUIAbstractBase
        {
            public override void OnUpdate()
            {
                this.WindowController.myXPositionScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" );
                this.WindowController.myScale = GameSettings.Current.GetFloatBySetting( "NotificationsScale" );

                //if you change away from the fleets tab, close the fleet management popout
                if ( //( Window_InGameSidebarBase.Current != InGameSidebarType.Fleets &&
                    //Window_InGameSidebarBase.Current != InGameSidebarType.Ships ) ||
                    Engine_Universal.RunStatus == RunStatus.GameStart )
                {
                    Instance.Close();
                    return;
                }
            }
        }

        private float leftWidth_Normal = 220;
        private float rightWidth_Normal = 240;

        private float leftWidth_VeryWideL = 380;
        private float rightWidth_VeryWideL = 80;

        private float leftWidth_VeryWideTriple = 280;
        private float rightWidth_VeryWideTriple = 60;
        private float thirdWidth_VeryWideTriple = 60;
        private float fourthWidth_VeryWideTriple = 60;

        public static readonly List<FleetMembership> memLookups = List<FleetMembership>.Create_WillNeverBeGCed( 90, "Window_FleetManagementSidebarPopout-memLookups" );

        private float fullWidth = 440;

        private ArcenCachedExternalTypeDirect type_tFleetNameHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tFleetNameHeader ) );
        private ArcenCachedExternalTypeDirect type_bEditFleetName = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bEditFleetName ) );
        private ArcenCachedExternalTypeDirect type_bCannotEditFleetName = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bCannotEditFleetName ) );
        private ArcenCachedExternalTypeDirect type_tFleetType = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tFleetType ) );
        private ArcenCachedExternalTypeDirect type_tFleetKeybindHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tFleetKeybindHeader ) );
        private ArcenCachedExternalTypeDirect type_dFleetKeybindIndex = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( dFleetKeybindIndex ) );
        private ArcenCachedExternalTypeDirect type_bFleetScienceLevel = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bFleetScienceLevel ) );
        private ArcenCachedExternalTypeDirect type_bFleetLeaderTypeTransformation = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bFleetLeaderTypeTransformation ) );
        private ArcenCachedExternalTypeDirect type_tFleetStrength = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tFleetStrength ) );
        private ArcenCachedExternalTypeDirect type_tFactoryConstructionStatusHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tFactoryConstructionStatusHeader ) );
        private ArcenCachedExternalTypeDirect type_bToggleFactoryConstructionStatus = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bToggleFactoryConstructionStatus ) );
        private ArcenCachedExternalTypeDirect type_tTransportModeHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tTransportModeHeader ) );
        private ArcenCachedExternalTypeDirect type_bToggleTransportMode = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bToggleTransportMode ) );
        private ArcenCachedExternalTypeDirect type_tFleetBehaviorHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tFleetBehaviorHeader ) );
        private ArcenCachedExternalTypeDirect type_bCycleFleetBehavior = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bCycleFleetBehavior ) );
        private ArcenCachedExternalTypeDirect type_tFleetMetricsHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tFleetMetricsHeader ) );
        private ArcenCachedExternalTypeDirect type_bShowFleetMetrics = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bShowFleetMetrics ) );
        private ArcenCachedExternalTypeDirect type_tToggleIsFleetFlagshipStationaryStatusInverted = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tToggleIsFleetFlagshipStationaryStatusInverted ) );
        private ArcenCachedExternalTypeDirect type_bToggleIsFleetFlagshipStationaryStatusInverted = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bToggleIsFleetFlagshipStationaryStatusInverted ) );
        private ArcenCachedExternalTypeDirect type_tToggleFlagshipAllowedToUseMovementModesStatus = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tToggleFlagshipAllowedToUseMovementModesStatus ) );
        private ArcenCachedExternalTypeDirect type_bToggleFlagshipAllowedToUseMovementModesStatus = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bToggleFlagshipAllowedToUseMovementModesStatus ) );
        private ArcenCachedExternalTypeDirect type_tDestroyFleetMembersOnOtherPlanetsHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tDestroyFleetMembersOnOtherPlanetsHeader ) );
        private ArcenCachedExternalTypeDirect type_bDestroyFleetMembersOnOtherPlanets = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bDestroyFleetMembersOnOtherPlanets ) );
        private ArcenCachedExternalTypeDirect type_tToggleIsFleetOnPlayerWatchlist = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tToggleIsFleetOnPlayerWatchlist ) );
        private ArcenCachedExternalTypeDirect type_bToggleIsFleetOnPlayerWatchlist = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bToggleIsFleetOnPlayerWatchlist ) );
        private ArcenCachedExternalTypeDirect type_tGiftFleetToOtherPlayerHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tGiftFleetToOtherPlayerHeader ) );
        private ArcenCachedExternalTypeDirect type_bGiftFleetToOtherPlayer = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bGiftFleetToOtherPlayer ) );
        private ArcenCachedExternalTypeDirect type_tChangeBolsteredFleetForCityHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tChangeBolsteredFleetForCityHeader ) );
        private ArcenCachedExternalTypeDirect type_bChangeBolsteredFleetForCity = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bChangeBolsteredFleetForCity ) );
        private ArcenCachedExternalTypeDirect type_tSingleLineInfo = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tSingleLineInfo ) );
        private ArcenCachedExternalTypeDirect type_tFleetMemberHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tFleetMemberHeader ) );
        private ArcenCachedExternalTypeDirect type_tBlankSpace = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tBlankSpace ) );
        private ArcenCachedExternalTypeDirect type_bToggleFleetMemberConstruction = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bToggleFleetMemberConstruction ) );
        private ArcenCachedExternalTypeDirect type_bOpenModulePanel = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bOpenModulePanel ) );
        private ArcenCachedExternalTypeDirect type_bSwapAllMembers = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bSwapAllMembers ) );
        private ArcenCachedExternalTypeDirect type_bSwapFleetMember = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bSwapFleetMember ) );
        private ArcenCachedExternalTypeDirect type_tFleetEmptySlotsHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tFleetEmptySlotsHeader ) );
        private ArcenCachedExternalTypeDirect type_bSwapFleetEmptySlot = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bSwapFleetEmptySlot ) );

        public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            if ( bMainContentParent.ParentT == null )
                return;
            
            if (Instance._fleetToManage == null)
            {
                this.Close();
                return;
            }
            
            this.Window.SetOverridingTransformToWhichToAddChildren( bMainContentParent.ParentT );
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            float runningY = topBuffer;

            if ( Instance._fleetToManage.Faction != localFaction )
            {
                this.Close();
                return;
            }

            Rect leftBounds;
            Rect rightBounds;
            Rect thirdBounds = new Rect();
            Rect fourthBounds = new Rect();

            GameEntity_Squad centerpiece = Instance._fleetToManage.Centerpiece.GetSquad();

            runningY += rowBuffer; //space it down just slightly

            #region Fleet Name *****************************************
            this.rowHeight = ROW_HEIGHT_DEFAULT;
            this.CalculateBoundsDual( out leftBounds, out rightBounds, ref runningY, leftWidth_VeryWideL, rightWidth_VeryWideL );
            AddText( Set, type_tFleetNameHeader, string.Empty, -1, -1, leftBounds, 13f );
            switch ( Instance._fleetToManage.Category )
            {
                case FleetCategory.PlayerMobile:
                case FleetCategory.PlayerCustomCityFedMobile:
                case FleetCategory.PlayerCustomUnattachedMobile:
                case FleetCategory.PlayerBattlestation:
                case FleetCategory.PlayerCustomCity:
                    AddButton( Set, type_bEditFleetName, string.Empty, -1, -1, rightBounds, -1f );
                    break;
                default:
                    AddButton( Set, type_bCannotEditFleetName, string.Empty, -1, -1, rightBounds, -1f );
                    break;
            }
            #endregion *****************************************

            #region Fleet Type *****************************************
            this.rowHeight = ROW_HEIGHT_SHORT;
            this.CalculateBoundsSingle( out leftBounds, ref runningY, fullWidth );
            AddText( Set, type_tFleetType, string.Empty, -1, -1, leftBounds, 13f );
            #endregion *****************************************

            #region Fleet Keybind Index *****************************************
            this.rowHeight = ROW_HEIGHT_DEFAULT;
            this.CalculateBoundsDual( out leftBounds, out rightBounds, ref runningY, leftWidth_VeryWideL, rightWidth_VeryWideL );
            AddText( Set, type_tFleetKeybindHeader, string.Empty, -1, -1, leftBounds, 13f );
            AddDropdown( Set, type_dFleetKeybindIndex, string.Empty, -1, -1, rightBounds, -1f );
            #endregion *****************************************

            if ( Instance._fleetToManage.GetIsFleetToHaveScienceButton( localFaction ) )
            {
                #region Fleet Science Upgrades *****************************************
                this.rowHeight = ROW_HEIGHT_SHORT;
                this.CalculateBoundsSingle( out leftBounds, ref runningY, fullWidth );
                AddButton( Set, type_bFleetScienceLevel, string.Empty, -1, -1, leftBounds, 13f );
                #endregion *****************************************
            }

            if ( Instance._fleetToManage.GetFleetTransforms() != null )
            {
                #region Fleet Leader Type Transformation *****************************************
                this.rowHeight = ROW_HEIGHT_SHORT;
                this.CalculateBoundsSingle( out leftBounds, ref runningY, fullWidth );
                AddButton( Set, type_bFleetLeaderTypeTransformation, string.Empty, -1, -1, leftBounds, 13f );
                #endregion *****************************************
            }

            #region Fleet Strength *****************************************
            this.rowHeight = ROW_HEIGHT_SHORT;
            this.CalculateBoundsSingle( out leftBounds, ref runningY, fullWidth );
            AddText( Set, type_tFleetStrength, string.Empty, -1, -1, leftBounds, 13f );
            #endregion *****************************************

            if ( Instance._fleetToManage.Category == FleetCategory.PlayerMobile || 
                Instance._fleetToManage.Category == FleetCategory.PlayerCustomCityFedMobile || Instance._fleetToManage.Category == FleetCategory.PlayerCustomUnattachedMobile ||
                Instance._fleetToManage.Category == FleetCategory.PlayerBattlestation )
            {
                #region Factory Construction Status *****************************************
                this.rowHeight = ROW_HEIGHT_DEFAULT;
                this.CalculateBoundsDual( out leftBounds, out rightBounds, ref runningY, leftWidth_Normal, rightWidth_Normal );
                AddText( Set, type_tFactoryConstructionStatusHeader, string.Empty, -1, -1, leftBounds, 13f );
                AddButton( Set, type_bToggleFactoryConstructionStatus, string.Empty, -1, -1, rightBounds, -1f );
                #endregion *****************************************

                #region Transport Mode *****************************************
                this.rowHeight = ROW_HEIGHT_DEFAULT;
                this.CalculateBoundsDual( out leftBounds, out rightBounds, ref runningY, leftWidth_Normal, rightWidth_Normal );
                AddText( Set, type_tTransportModeHeader, string.Empty, -1, -1, leftBounds, 13f );
                AddButton( Set, type_bToggleTransportMode, string.Empty, -1, -1, rightBounds, -1f );
                #endregion *****************************************

                if ( ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() )
                {
                    #region Fleet Behavior *****************************************
                    this.rowHeight = ROW_HEIGHT_DEFAULT;
                    this.CalculateBoundsDual( out leftBounds, out rightBounds, ref runningY, leftWidth_Normal, rightWidth_Normal );
                    AddText( Set, type_tFleetBehaviorHeader, string.Empty, -1, -1, leftBounds, 13f );
                    AddButton( Set, type_bCycleFleetBehavior, string.Empty, -1, -1, rightBounds, -1f );
                    #endregion *****************************************

                    if ( Instance._fleetToManage.BaseInfo is FleetMetricsBaseInfo )
                    {
                        #region Fleet Metrics *****************************************
                        this.rowHeight = ROW_HEIGHT_DEFAULT;
                        this.CalculateBoundsDual( out leftBounds, out rightBounds, ref runningY, leftWidth_Normal, rightWidth_Normal );
                        AddText( Set, type_tFleetMetricsHeader, string.Empty, -1, -1, leftBounds, 13f );
                        AddButton( Set, type_bShowFleetMetrics, string.Empty, -1, -1, rightBounds, -1f );
                        #endregion *****************************************
                    }
                }
            }

            switch ( Instance._fleetToManage.Category )
            {
                case FleetCategory.PlayerMobile:
                case FleetCategory.PlayerBattlestation:
                case FleetCategory.PlayerCustomCityFedMobile:
                case FleetCategory.PlayerCustomUnattachedMobile:
                    {
                        #region Flagship Stationary Modes Status *****************************************
                        this.rowHeight = ROW_HEIGHT_DEFAULT;
                        this.CalculateBoundsDual( out leftBounds, out rightBounds, ref runningY, leftWidth_Normal, rightWidth_Normal );
                        AddText( Set, type_tToggleIsFleetFlagshipStationaryStatusInverted, string.Empty, -1, -1, leftBounds, 13f );
                        AddButton( Set, type_bToggleIsFleetFlagshipStationaryStatusInverted, string.Empty, -1, -1, rightBounds, -1f );
                        #endregion *****************************************

                        #region Flagship Roaming Modes Status *****************************************
                        this.rowHeight = ROW_HEIGHT_DEFAULT;
                        this.CalculateBoundsDual( out leftBounds, out rightBounds, ref runningY, leftWidth_Normal, rightWidth_Normal );
                        AddText( Set, type_tToggleFlagshipAllowedToUseMovementModesStatus, string.Empty, -1, -1, leftBounds, 13f );
                        AddButton( Set, type_bToggleFlagshipAllowedToUseMovementModesStatus, string.Empty, -1, -1, rightBounds, -1f );
                        #endregion *****************************************

                        #region Destroy Fleet Members On Other Planets *****************************************
                        this.rowHeight = ROW_HEIGHT_DEFAULT;
                        this.CalculateBoundsDual( out leftBounds, out rightBounds, ref runningY, leftWidth_Normal, rightWidth_Normal );
                        AddText( Set, type_tDestroyFleetMembersOnOtherPlanetsHeader, string.Empty, -1, -1, leftBounds, 13f );
                        AddButton( Set, type_bDestroyFleetMembersOnOtherPlanets, string.Empty, -1, -1, rightBounds, -1f );
                        #endregion *****************************************
                    }
                    break;
            }

            switch ( Instance._fleetToManage.Category )
            {
                case FleetCategory.PlayerCustomCity:
                    if (Instance._fleetToManage?.GetFleetBolsteredByThisCity() != null || Instance._fleetToManage?.GetCanBolsterAnything() != null)
                    {
                        #region Change Supporting Mobile Fleet *****************************************
                        this.rowHeight = ROW_HEIGHT_DEFAULT;
                        this.CalculateBoundsDual( out leftBounds, out rightBounds, ref runningY, leftWidth_Normal, rightWidth_Normal );
                        AddText( Set, type_tChangeBolsteredFleetForCityHeader, string.Empty, -1, -1, leftBounds, 13f );
                        AddButton( Set, type_bChangeBolsteredFleetForCity, string.Empty, -1, -1, rightBounds, -1f );
                        #endregion *****************************************
                    }
                    break;
            }

            #region Fleet Watchlist *****************************************
            this.rowHeight = ROW_HEIGHT_DEFAULT;
            this.CalculateBoundsDual( out leftBounds, out rightBounds, ref runningY, leftWidth_Normal, rightWidth_Normal );
            AddText( Set, type_tToggleIsFleetOnPlayerWatchlist, string.Empty, -1, -1, leftBounds, 13f );
            AddButton( Set, type_bToggleIsFleetOnPlayerWatchlist, string.Empty, -1, -1, rightBounds, -1f );
            #endregion *****************************************

            if ( World_AIW2.Instance.FactionsThatCanBeGiftedShips.Count > 1 )
            {
                #region Gift Fleet To Other Player *****************************************
                this.rowHeight = ROW_HEIGHT_DEFAULT;
                this.CalculateBoundsDual( out leftBounds, out rightBounds, ref runningY, leftWidth_Normal, rightWidth_Normal );
                AddText( Set, type_tGiftFleetToOtherPlayerHeader, string.Empty, -1, -1, leftBounds, 13f );
                AddButton( Set, type_bGiftFleetToOtherPlayer, string.Empty, -1, -1, rightBounds, -1f );
                #endregion *****************************************
            }

            runningY += ROW_HEIGHT_SPACER;

            ArcenRejectionReason cannotSwapFromFleetReason = _fleetToManage.GetCanSwapFleetLinesWithOtherFleets();
            bool canSwapFromThisFleet = cannotSwapFromFleetReason == ArcenRejectionReason.Unknown;

            #region cannotSwapFromFleetReason
            if ( !canSwapFromThisFleet )
            {
                string cannotSwapReason = string.Empty;
                switch ( cannotSwapFromFleetReason )
                {
                    case ArcenRejectionReason.CannotSwapContents_FlagshipCrippled:
                        cannotSwapReason = "旗舰受损时无法进行交换。";
                        break;
                    case ArcenRejectionReason.CannotSwapContents_FlagshipDeadOrMissing:
                        cannotSwapReason = "旗舰死亡或缺失时无法进行交换。";
                        break;
                    case ArcenRejectionReason.CannotSwapContents_FlagshipNotFullyClaimedYet:
                        cannotSwapReason = "旗舰尚未完全占领时无法进行交换。";
                        break;
                }
                if ( cannotSwapReason.Length > 0 )
                {
                    this.rowHeight = ROW_HEIGHT_SHORT;
                    this.CalculateBoundsSingle( out leftBounds, ref runningY, fullWidth );
                    AddText( Set, type_tSingleLineInfo, cannotSwapReason, -1, -1, leftBounds, 12f );
                }
            }
            #endregion

            memLookups.Clear();

            foreach ( FleetMembership mem in _fleetToManage.MemberGroupsSorted_NonSim_ForUIOnly_SeriouslyDoNotCallFromBGCode() )
            {
                if ( !ShouldShowMembershipInFleet( mem ) )
                    continue;

                int j = memLookups.Count;
                memLookups.Add( mem );
                bool isFleetLeader = mem.TypeData.IsFleetLeader;

                #region Fleet Members *****************************************
                if ( (Instance._fleetToManage.Category == FleetCategory.PlayerMobile ||
                       Instance._fleetToManage.Category == FleetCategory.PlayerCustomCityFedMobile ||
                       Instance._fleetToManage.Category == FleetCategory.PlayerCustomCity ||
                       Instance._fleetToManage.Category == FleetCategory.PlayerCustomUnattachedMobile) )
                {
                    this.rowHeight = ROW_HEIGHT_DEFAULT;
                    this.CalculateBoundsQuadruple( out leftBounds, out rightBounds, out thirdBounds, out fourthBounds, ref runningY,
                        leftWidth_VeryWideTriple, rightWidth_VeryWideTriple, thirdWidth_VeryWideTriple, fourthWidth_VeryWideTriple );
                }
                else
                {
                    this.rowHeight = ROW_HEIGHT_SHORT;
                    this.CalculateBoundsSingle( out leftBounds, ref runningY, fullWidth );
                }
                //header
                leftBounds = new Rect( leftBounds.x, leftBounds.y, leftBounds.width + 100, leftBounds.height ); //be super sure no wrapping
                AddText( Set, type_tFleetMemberHeader, string.Empty, j, j, leftBounds, 13f );

                if ( Instance._fleetToManage.Category == FleetCategory.PlayerMobile )
                {
                    //modular bits
                    if ( mem.TypeData.IsModular )
                        AddButton( Set, type_bOpenModulePanel, string.Empty, j, j, rightBounds, -1f );

                    if ( mem.TypeData.CannotActuallyBeBuilt_IsNotAShipLine || isFleetLeader )
                        AddText( Set, type_tBlankSpace, string.Empty, j, j, thirdBounds, 13f );
                    else
                        AddButton( Set, type_bToggleFleetMemberConstruction, string.Empty, j, j, thirdBounds, -1f );
                    if ( canSwapFromThisFleet && !mem.TypeData.IsDrone && !mem.TypeData.ImmuneToSwappingBetweenFleets )
                    {
                        if ( mem.TypeData.IsFleetLeader )
                            AddButton( Set, type_bSwapAllMembers, string.Empty, j, j, fourthBounds, -1f );
                        else
                            AddButton( Set, type_bSwapFleetMember, string.Empty, j, j, fourthBounds, -1f );
                    }
                }
                else if ( Instance._fleetToManage.Category == FleetCategory.PlayerCustomCityFedMobile ||
                          Instance._fleetToManage.Category == FleetCategory.PlayerCustomUnattachedMobile )
                {
                    //modular bits
                    if ( mem.TypeData.IsModular )
                        AddButton( Set, type_bOpenModulePanel, string.Empty, j, j, thirdBounds, -1f );

                    if ( mem.TypeData.CannotActuallyBeBuilt_IsNotAShipLine || isFleetLeader )
                        AddText( Set, type_tBlankSpace, string.Empty, j, j, fourthBounds, 13f );
                    else
                        AddButton( Set, type_bToggleFleetMemberConstruction, string.Empty, j, j, fourthBounds, -1f );
                }
                else if ( Instance._fleetToManage.Category == FleetCategory.PlayerCustomCity && //so Phylacteries and Necropolises can use modularity
                          mem.TypeData.IsModular )
                {
                    AddButton( Set, type_bOpenModulePanel, string.Empty, j, j, fourthBounds, -1f );
                }
                #endregion *****************************************
            }

            //empty slots
            if ( Instance._fleetToManage.Category == FleetCategory.PlayerMobile && canSwapFromThisFleet )
            {
                int emptySlots = Instance._fleetToManage.CalculateRemainingShipLineSlotCount();
                if ( emptySlots > 0 )
                {
                    this.rowHeight = ROW_HEIGHT_DEFAULT;
                    this.CalculateBoundsQuadruple( out leftBounds, out rightBounds, out thirdBounds, out fourthBounds, ref runningY, 
                        leftWidth_VeryWideTriple, rightWidth_VeryWideTriple, thirdWidth_VeryWideTriple, fourthWidth_VeryWideTriple );

                    AddText( Set, type_tFleetEmptySlotsHeader, string.Empty, -1, -1, leftBounds, 13f );
                    fourthBounds.xMin = thirdBounds.xMin; //make this double-wide
                    AddButton( Set, type_bSwapFleetEmptySlot, string.Empty, -1, -1, fourthBounds, -1f );
                }
            }

            bMainContentParent.ParentRT.UI_SetHeight( runningY );
        }

        #region ShouldShowMembershipInFleet
        public static bool ShouldShowMembershipInFleet( FleetMembership mem )
        {
            int effectiveHere = mem.GetCountPresent( true, ExtraFromStacks.IncludePrecalc );
            if ( mem.EffectiveSquadCap <= 0 && !mem.TypeData.CannotActuallyBeBuilt_IsNotAShipLine ) //if CannotActuallyBeBuilt_IsNotAShipLine, then DO show it here
            {
                //if there's no cap for a unit type, and none present, don't show it.
                //this is something that previously existed here but not longer does.
                if ( effectiveHere <= 0 )
                    return false;
            }
            bool isFleetLeader = mem.TypeData.IsFleetLeader;
            //don't draw old fleet leaders that aren't here anymore.
            //typically happens from switching command station types.
            if ( isFleetLeader && effectiveHere <= 0 )
                return false;
            return true;
        }
        #endregion

        #region tFleetNameHeader
        public class tFleetNameHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( Instance._fleetToManage.GetName() );
            }
        }
        #endregion

        #region bEditFleetName
        public class bEditFleetName : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "<size=11>编辑名称" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_ModalTextboxWindow.Instance.Open( "重命名舰队", "舰队名称：", false, Instance._fleetToManage.GetName(), 30,
                    delegate ( string NewFleetName )
                    {
                        if ( Window_FleetManagementSidebarPopout.Instance._fleetToManage.GetName() == NewFleetName )
                            return;
                        GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                        command.RelatedIntegers.Add( Instance._fleetToManage.FleetID ); //FleetID
                        command.RelatedString = "EditFleetName";
                        command.RelatedString2 = NewFleetName;
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                    } );                

                return MouseHandlingResult.None;
            }
        }
        #endregion

        #region bCannotEditFleetName
        public class bCannotEditFleetName : ButtonAbstractBase
        {
            public static bCannotEditFleetName Instance;

            public bCannotEditFleetName()
            {
                Instance = this;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                Buffer.Add( "<size=10>无法编辑" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "无法重命名舰队！", "此类型的舰队无法重命名，抱歉！", "确定" );
                return MouseHandlingResult.None;
            }
        }
        #endregion

        #region tFleetType
        public class tFleetType : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "<color=#777777>舰队类型：" ).Add( Instance._fleetToManage.GetFleetFleetCategoryPurpose().GetFullDisplayName() );
                buffer.Add( "</color>" );
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, Instance._fleetToManage.GetFleetFleetCategoryPurpose().GetDescription() );
            }
        }
        #endregion
        
        #region bFleetScienceLevel
        public class bFleetScienceLevel : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                int scienceOrOtherResourceRequired = Instance._fleetToManage.GetScienceOrOtherResourceNeededForNextLevelUp( Instance._fleetToManage.AddedMarkLevelsForFleet_FromScience );
                string resourceName = Instance._fleetToManage.GetResourceNameNeededForNextLevelUp();

                buffer.Add( "<color=#777777>" );

                GameEntity_Squad centerpOrNull = Instance._fleetToManage.Centerpiece.GetSquad();
                if ( centerpOrNull == null )
                    buffer.Add( "指挥站已损毁！等级提升来源：" ).Add( resourceName ).Add( "：</color> " );
                else
                    buffer.Add( centerpOrNull.TypeData.ThisCenterpieceGrantsItsDirectScienceUpgradesToRestOfFleet ? "全舰队" : "旗舰" )
                        .Add( " 等级提升来源：" ).Add( resourceName ).Add( "：</color> " );
                buffer.AddNumberMoreReadable( Instance._fleetToManage.AddedMarkLevelsForFleet_FromScience );

                if ( scienceOrOtherResourceRequired >= 0 )
                {
                    UpgradeResourceStyle resourceNeeded = Instance._fleetToManage.GetResourceNeededForNextLevelUp();
                    if ( resourceNeeded == UpgradeResourceStyle.Resource1 )
                    {
                        if ( scienceOrOtherResourceRequired <= Instance._fleetToManage.Faction.StoredFactionResourceOne )
                            buffer.Add( "<color=#" + Instance._fleetToManage.Faction.Resource1Color + ">" );
                        else
                            buffer.Add( "<color=#ff0000>" );
                    }
                    else if ( resourceNeeded == UpgradeResourceStyle.Science &&
                              scienceOrOtherResourceRequired > Instance._fleetToManage.Faction.StoredScience )
                        buffer.Add( "<color=#f74343>" );
                    else
                        buffer.Add( "<color=#43c2f7>" );

                    buffer.Add( " (" ).AddNumberMoreReadable( scienceOrOtherResourceRequired ).Add( " 升级)" );

                    int shipStrengthIncrease = Instance._fleetToManage.UIOnly_Fleet_UpgradedShipStrengthIncrease_FromOneMarkLevel;
                    int defenseStrengthIncrease = Instance._fleetToManage.UIOnly_Fleet_UpgradedDefenseStrengthIncrease_FromOneMarkLevel;
                    if ( shipStrengthIncrease > 0 && shipStrengthIncrease < 1000 )
                        shipStrengthIncrease = 1000;
                    if ( defenseStrengthIncrease > 0 && defenseStrengthIncrease < 1000 )
                        defenseStrengthIncrease = 1000;

                    if ( defenseStrengthIncrease > 0 || shipStrengthIncrease > 0 )
                    {
                        if ( shipStrengthIncrease > 0 )
                        {
                            buffer.Add( "    " ).Add( ArcenExternalUIUtilities.GUI_StrengthTextIcon_AndShipLineIncreaseColor )
                            .StartColor( ArcenExternalUIUtilities.ShipLineIncreaseColor ).AddStrengthTiered( shipStrengthIncrease ).EndColor().Add( "</pos>" );
                        }
                        if ( defenseStrengthIncrease > 0 )
                        {
                            buffer.Add( "    " )
                            .StartColor( ArcenExternalUIUtilities.DefenseLineIncreaseColor ).Add( ArcenExternalUIUtilities.GUI_StrengthTextIcon_AndDefenseLineIncreaseColor )
                            .AddStrengthTiered( defenseStrengthIncrease ).EndColor().Add( "</pos>" );
                        }
                    }
                }
                else
                    buffer.Add( " (已达最高等级)" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return UpgradeFleet( Instance._fleetToManage );
            }

            #region UpgradeFleet
            public static MouseHandlingResult UpgradeFleet( Fleet fleet )
            {
                if ( fleet == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                int scienceOrOtherResourceRequired = fleet.GetScienceOrOtherResourceNeededForNextLevelUp( fleet.AddedMarkLevelsForFleet_FromScience );
                if ( scienceOrOtherResourceRequired <= 0 )
                    return MouseHandlingResult.PlayClickDeniedSound;

                #region instead of normal click behavior, show details
                if ( InputCaching.CalculateHoldAndClickToViewDetailsOfContents() )
                {
                    ShowDetailsOfAFleetUpgradeContents( fleet, scienceOrOtherResourceRequired, 0, fleet.AddedMarkLevelsForFleet_FromScience );
                    return MouseHandlingResult.None;
                }
                #endregion

                UpgradeResourceStyle resourceNeeded = fleet.GetResourceNeededForNextLevelUp();
                switch ( resourceNeeded )
                {
                    case UpgradeResourceStyle.Resource1:
                        if ( scienceOrOtherResourceRequired > fleet.Faction.StoredFactionResourceOne )
                        {
                            //if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                            World_AIW2.Instance.QueueChatMessageOrCommand( "没有足够的 " + World_AIW2.Instance.Resource1DisplayName + " 来升级舰队！", ChatType.ShowLocallyOnly, null );
                            return MouseHandlingResult.PlayClickDeniedSound;
                        }
                        break;
                    case UpgradeResourceStyle.Resource2:
                        if ( scienceOrOtherResourceRequired > fleet.Faction.StoredFactionResourceTwo )
                        {
                            //if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                            World_AIW2.Instance.QueueChatMessageOrCommand( "没有足够的 " + World_AIW2.Instance.Resource2DisplayName + " 来升级舰队！", ChatType.ShowLocallyOnly, null );
                            return MouseHandlingResult.PlayClickDeniedSound;
                        }
                        break;
                    default:
                    case UpgradeResourceStyle.Science:
                        if ( scienceOrOtherResourceRequired > fleet.Faction.StoredScience )
                        {
                            //if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                            World_AIW2.Instance.QueueChatMessageOrCommand( "没有足够的科技点来升级舰队！", ChatType.ShowLocallyOnly, null );
                            return MouseHandlingResult.PlayClickDeniedSound;
                        }
                        break;
                }

                GameEntity_Squad centerpOrNull = fleet.Centerpiece.GetSquad();
                if ( centerpOrNull == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                bool requirePrompt = GameSettings.Current.GetBoolBySetting( "UpgradeShipPrompt" ) && !InputCaching.CalculateHoldAndSuppressTechUpgradePrompt();
                if ( requirePrompt )
                {
                    string resourceName = fleet.GetResourceNameNeededForNextLevelUp();

                    System.Text.StringBuilder builder = new System.Text.StringBuilder();
                    builder.Append( "你确定要花费 " ).Append( scienceOrOtherResourceRequired.ToString( "#,##0" ) );                    

                    if ( fleet.Category == FleetCategory.PlayerPlanetaryCommand )
                        builder.Append( " " ).Append( resourceName ).Append( " 来升级位于星球 " ).Append( fleet.Planet == null ? "null" : fleet.GetPlanetName_Safe() )
                            .Append( " 的舰队，提升一个标记等级？\n\n这不会对战斗站、堡垒或星球舰队之外的其他单位生效。但它会使星球舰队内的所有单位受益（达到其个人最高等级）。\n\n这是改善经济或在银河系特定地点加强防御的绝佳方式。但必须谨慎操作，因为 " + resourceName + " 是不可再生资源。\n\n如果你失去这个星球后重新夺回，或选择更改此处的指挥站类型，你已支付的升级将保持不变。" );
                    else if ( fleet.Category == FleetCategory.PlayerBattlestation ) //battlestation or citadel
                        builder.Append( " " ).Append( resourceName ).Append( " 来升级舰队 " ).Append( fleet.GetName() )
                            .Append( " ，提升一个标记等级？\n\n这仅影响该特定战斗站/堡垒及其舰队成员，不影响该舰队所在星球上的其他单位或炮台。但它会使该舰队内的所有单位受益（达到其个人最高等级）。\n\n这是以可在银河系中移动的方式改善防御的绝佳方式。但必须谨慎操作，因为 " + resourceName +" 是不可再生资源。" );
                    else
                    {
                        if ( centerpOrNull.TypeData.ThisCenterpieceGrantsItsDirectScienceUpgradesToRestOfFleet )
                            builder.Append( " " ).Append( resourceName ).Append( " 来升级舰队 " ).Append( fleet.GetName() )
                                .Append( " ，提升一个标记等级？\n\n这仅影响该特定旗舰的舰队，但会使该舰队内的所有单位受益（达到其个人最高等级）。\n\n这是改善特定打击部队的绝佳方式。但必须谨慎操作，因为 " + resourceName + " 是不可再生资源。" );
                        else
                            builder.Append( " " ).Append( resourceName ).Append( " 来升级旗舰 " ).Append( centerpOrNull.TypeData.DisplayName )
                                .Append( " ，提升一个标记等级？\n\n这仅影响该特定旗舰，不影响其舰队成员（如果有的话）。必须谨慎操作，因为 " + resourceName + " 是不可再生资源。" );
                    }

                    builder.Append( "\n \n" + "<color=#888888>要禁用此提示，请进入游戏设置，在游戏选项卡下将其切换为关闭。或者在点击升级按钮时按住 " )
                        .Append( InputActionTypeDataTable.GetActionByName_FairlySlow( "SuppressTechUpgradePrompt" ).GetHumanReadableKeyCombo() )
                        .Append( " 来跳过本次提示。</color>" );

                    ModalPopupData.CreateAndLogYesNoStyle( delegate
                    {
                        GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                        command.RelatedIntegers.Add( fleet.FleetID ); //FleetID
                        command.RelatedString = "UpgradeFleetViaScience";
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                    }, null, "花费 " + resourceName + " 升级此舰队？", builder.ToString(), "是，升级", "不，等等！" );
                }
                else
                {
                    GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    command.RelatedIntegers.Add( fleet.FleetID ); //FleetID
                    command.RelatedString = "UpgradeFleetViaScience";
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                }                

                return MouseHandlingResult.None;
            }
            #endregion

            private static readonly Regex diffSplitRegex = new Regex( @"(<.+?>)|( )", RegexOptions.Compiled | RegexOptions.Singleline );

            private static SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark> fleetUpgrade_Ships_ThatYouHave = 
                SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark>.Create_WillNeverBeGCed( 30, delegate { return new ShipDataByMark(); }, "Window_FleetManagementSidebarPopout-bFleetScienceLevel-fleetUpgrade_Ships_ThatYouHave" );

            #region WriteDetailsOfATFleetUpgradeContents
            public static bool WriteDetailsOfATFleetUpgradeContents( ArcenDoubleCharacterBuffer buffer, Fleet fleetBeingUpgraded, int CostToUpgradeScienceOrOtherwise, int AmountToRefund, int UpgradesSoFar,
                float PositionScaleMultiplier )
            {
                EntityText.Write_Tooltip_Hotkeys_Footer( buffer, false, true, null );
                buffer.Add( "\n\n" );

                GameEntity_Squad centerpOrNull = fleetBeingUpgraded.Centerpiece.GetSquad();

                bool upgradeEntireFleet = centerpOrNull != null && centerpOrNull.TypeData.ThisCenterpieceGrantsItsDirectScienceUpgradesToRestOfFleet;

                TechUpgrade upgrade = null;
                if ( fleetBeingUpgraded != null && centerpOrNull != null )
                    upgrade = centerpOrNull.TypeData.TechUpgradeThatIsUsedForMyDirectScience;

                if ( upgradeEntireFleet || centerpOrNull == null )
                {
                    if ( AmountToRefund > 0 )
                        buffer.Add( "<u>舰队降级信息：</u>\n" );
                    else
                        buffer.Add( "<u>舰队升级信息：</u>\n" );
                    buffer.Add( "<b>舰队：" ).Add( fleetBeingUpgraded.GetName() ).Add( "</b>\n" );
                }
                else
                {
                    if ( AmountToRefund > 0 )
                        buffer.Add( "<u>旗舰降级信息：</u>\n" );
                    else
                        buffer.Add( "<u>旗舰升级信息：</u>\n" );
                    buffer.Add( "<b>旗舰：" ).Add( centerpOrNull.TypeData.DisplayName ).Add( "，隶属于舰队 " ).Add( fleetBeingUpgraded.GetName() ).Add( "</b>\n" );
                    buffer.Add( "如果旗舰生产的无人机存在，它们也将被升级。\n" );
                }

                List<int> upgradeCosts = upgrade == null ? null : upgrade.GetScienceOrOtherResourceCostsPerTimeUnlocked();
                string resourceName = fleetBeingUpgraded.GetResourceNameNeededForNextLevelUp();
                
                buffer.Add( "已直接升级 " ).Add( UpgradesSoFar ).Add( " 次，最多 " ).Add( upgradeCosts == null ? "???" : upgradeCosts.Count.ToString() ).Add( " 次。\n" );

                if ( AmountToRefund > 0 )
                    buffer.Add( "将退还 " ).Add( AmountToRefund ).Add( " " ).Add( resourceName ).Add( " 以重置为零。\n" );
                else
                {
                    if ( CostToUpgradeScienceOrOtherwise >= 99999 )
                    {
                        buffer.Add( "无法继续升级。\n" );
                        return true; //nothing more to say
                    }
                    else
                        buffer.Add( "下次升级将花费 " ).Add( CostToUpgradeScienceOrOtherwise ).Add( " " ).Add( resourceName ).Add( "。\n" );
                }

                if ( AmountToRefund > 0 )
                {
                    buffer.Add( "\n<color=#27f985>（括号中为当前标记等级的数值。）</color>\n\n" );
                    buffer.Add( "<color=#aaaaaa>将降低本舰队中以下单位的标记等级（降低 " ).Add( UpgradesSoFar ).Add( " 级）：</color>\n" );
                }
                else
                {
                    buffer.Add( "\n<color=#27f985>（括号中为上一标记等级的数值，新增内容以<i>斜体</i>显示。）</color>\n\n" );
                    buffer.Add( "<color=#aaaaaa>将提升本舰队中以下单位的标记等级（提升一级）：</color>\n" );
                }

                buffer.Add( "\n\n" );

                Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();

                #region shipsThatBenefit_ThatYouHave;
                ShipListerUtils.CalculateShipsThatBenefit_ThatYouHave( fleetMembership => ShouldShowMembershipInFleet( fleetMembership ), fleetUpgrade_Ships_ThatYouHave, true,
                    fleetBeingUpgraded, upgradeEntireFleet );

                GameEntityTypeData ship;
                bool isFirst = true;
                foreach ( KeyValuePair<GameEntityTypeData, ShipDataByMark> pair in fleetUpgrade_Ships_ThatYouHave )
                {
                    ship = pair.Key;
                    if ( isFirst )
                    {
                        if ( AmountToRefund > 0 )
                            buffer.Add( "\n<b><u><size=120%>将降低本舰队中以下单位的标记等级（降低 " ).Add( UpgradesSoFar ).Add( " 级）：</b></u></size>\n" );
                        else
                            buffer.Add( "\n<b><u><size=120%>将提升本舰队中以下单位的标记等级（提升一级）：</b></u></size>\n" );
                        isFirst = false;
                    }

                    for ( byte mark = 0; mark < pair.Value.CountsByMarkLength(); mark++ )
                    {
                        int count = pair.Value.GetCountByMark( mark );
                        if ( count <= 0 )
                            continue;

                        GameEntityTypeData.MarkLevelStats oldStats = pair.Key.MarkStatsFor( mark );
                        GameEntityTypeData.MarkLevelStats newStats = ship.MarkStatsFor( (byte)(mark + (AmountToRefund > 0 ? -UpgradesSoFar : 1)) );

                        int oldShipCount = count;
                        int newShipCount = ship.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouDoNOTKnowBaseCap( ship, oldShipCount, false, oldStats.MarkLevel, newStats.MarkLevel );

                        Fleet fleetToUse = pair.Value.FleetToUse;
                        Faction factionToUse = fleetToUse != null ? fleetToUse.Faction : null;
                        if ( factionToUse == null )
                            factionToUse = localFaction;

                        ArcenCharacterBuffer tempBuffer1 = ArcenCharacterBuffer.GetFromPoolOrCreate( "Window_FleetManagementSidebarPopout-bFleetScienceLevel-tempBuffer1" );
                        EntityText.GetTooltip( tempBuffer1, null, null,
                            ship, fleetToUse, factionToUse.FactionCenterColor.ColorHexBrighter, string.Empty,
                            oldShipCount, factionToUse, mark, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None, PositionScaleMultiplier, true );
                        string oldText = tempBuffer1.ToStringAndReturnToPool();
                        //ArcenDebugging.ArcenDebugLogSingleLine( "OLD TEXT:" + Environment.NewLine + oldText, Verbosity.Chat );
                        System.Collections.Generic.List<string> oldTokens = diffSplitRegex.Split( oldText ).ToList();


                        ArcenCharacterBuffer tempBuffer2 = ArcenCharacterBuffer.GetFromPoolOrCreate( "Window_FleetManagementSidebarPopout-bFleetScienceLevel-tempBuffer2" );
                        EntityText.GetTooltip( tempBuffer2, null, null,
                            ship, fleetToUse, factionToUse.FactionCenterColor.ColorHexBrighter, string.Empty,
                            newShipCount, factionToUse, newStats.MarkLevel.Ordinal, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None, PositionScaleMultiplier, true );
                        string newText = tempBuffer2.ToStringAndReturnToPool();
                        //ArcenDebugging.ArcenDebugLogSingleLine( "NEW TEXT:" + Environment.NewLine + newText, Verbosity.Chat );
                        System.Collections.Generic.List<string> newTokens = diffSplitRegex.Split( newText ).ToList();

                        string finalText = GenerateDiff( oldTokens, newTokens );
                        //ArcenDebugging.ArcenDebugLogSingleLine( "FINAL TEXT: " + Environment.NewLine + finalText.ToString(), Verbosity.Chat );
                        buffer.Add( finalText );
                        buffer.Add( "\n\n" );
                    }
                }
                #endregion

                buffer.Add( "\n\n\n\n" );

                return true;
            }

            private static string GenerateDiff( System.Collections.Generic.List<string> text1, System.Collections.Generic.List<string> text2 )
            {
                StringBuilder builder = new StringBuilder();
                int i1 = 0;
                int i2 = 0;

                DifferController.GenerateDiff( text1, text2, delegate ( DiffDataForSection section )
                {
                    if ( section.IsMatch )
                        builder.Append( JoinRange( text1, i1, section.LengthInCollection1 ) );
                    else
                    {
                        System.Collections.Generic.List<string> text1Range = text1.GetRange( i1, section.LengthInCollection1 );
                        bool hasPreviousValue = text1Range.Any( x => !string.IsNullOrWhiteSpace( x ) );

                        if ( !hasPreviousValue )
                        {
                            //Display any brand new text in italics
                            builder.Append( "<i>" );
                        }
                        builder.Append( JoinRange( text2, i2, section.LengthInCollection2 ) );
                        if ( !hasPreviousValue )
                        {
                            builder.Append( "</i>" );
                        }
                        // Display previous value, if it exists and is something else than whitespace
                        if ( hasPreviousValue )
                        {
                            //ignore html tags in previous value display
                            builder.Append( " (" + string.Join( "", text1Range.Where( x => !x.StartsWith( "<" ) ) ) + ")" );
                        }
                    }

                    i1 += section.LengthInCollection1;
                    i2 += section.LengthInCollection2;
                } );
                return builder.ToString();
            }
            #endregion

            private static string JoinRange( System.Collections.Generic.List<string> strings, int index, int count )
            {
                return string.Join( "", strings.GetRange( index, count ) );
            }

            public static void ShowDetailsOfAFleetUpgradeContents( Fleet fleetToUpgrade, int CostToUpgradeScienceOrOtherwise, int AmountToRefund, int UpgradesSoFar )
            {
                if ( fleetToUpgrade == null )
                    return;

                float centerPopupScale = GameSettings.Current.GetFloatBySetting( "CentralPopupTextScale" );
                Window_ModalSelfUpdatingTextWindow_Wide.Instance.Open( 0.25f, 2f, "科技升级单位详情", "关闭",
                    delegate ( ArcenDoubleCharacterBuffer Buffer ) { return WriteDetailsOfATFleetUpgradeContents( Buffer, fleetToUpgrade, CostToUpgradeScienceOrOtherwise, AmountToRefund, 
                        UpgradesSoFar, centerPopupScale ); } );
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_FleetManagementSidebarPopout-bFleetScienceLevel-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.Clear();
                FillFleetUpgradeTooltipInfo( Instance._fleetToManage, tooltipBuffer, Instance._fleetToManage.AddedMarkLevelsForFleet_FromScience, 0 );
                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
            }

            public static void FillFleetUpgradeTooltipInfo( Fleet fleet, ArcenDoubleCharacterBuffer tooltipBuffer, int upgradesSoFar, int RefundAmount )
            {
                GameEntity_Squad centerpOrNull = fleet.Centerpiece.GetSquad();

                Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( playerFaction == null )
                    playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();

                if ( centerpOrNull != null && centerpOrNull.TypeData.ThisCenterpieceGrantsItsDirectScienceUpgradesToRestOfFleet )
                {
                    tooltipBuffer.Add( "这种类型的舰队可以通过直接消耗科技点来升级（所有单位都会受益）。目前已被升级 " )
                        .Add( upgradesSoFar )
                        .Add( " 次。这是一种非常节省科技点的获取更多金属收入或能量产出的方式。\n\n如果这是行星舰队，请注意更改指挥站类型、失去星球后重新夺回都不会导致你在此星球上的升级丢失。" );
                }
                else
                {
                    if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( playerFaction ) &&
                         centerpOrNull != null )
                    {
                        if ( centerpOrNull.TypeData.IsMobile )
                            tooltipBuffer.Add( "此旗舰位于 " ).Add(  centerpOrNull.Planet.Name, "a1a1ff").Add("。\n");
                        else
                            tooltipBuffer.Add( "此 " + centerpOrNull.TypeData.GetDisplayName() + " 位于 " ).Add(  centerpOrNull.Planet.Name, "a1a1ff").Add("。\n");

                    }
                    tooltipBuffer.Add( "这种类型的旗舰可以通过 ");
                    tooltipBuffer.Add ( fleet.GetResourceTextColorAndIconNeededForNextLevelUp() );
                    tooltipBuffer.Add(" 直接升级，但不会升级其舰队内的所有单位。目前已被升级 " )
                        .Add( upgradesSoFar )
                        .Add( " 次。" );
                }
                if ( RefundAmount > 0 )
                    tooltipBuffer.Add( "\n当前你可以获得 " )
                        .Add( RefundAmount )
                        .Add( " 科技点的退还，将所有受影响的单位回退 " ).Add( upgradesSoFar )
                        .Add( " 个标记等级。" );

                bool upgradeEntireFleet = centerpOrNull != null && centerpOrNull.TypeData.ThisCenterpieceGrantsItsDirectScienceUpgradesToRestOfFleet;

                ShipListerUtils.CalculateShipsThatBenefit_ThatYouHave( fleetMembership => ShouldShowMembershipInFleet( fleetMembership ), fleetUpgrade_Ships_ThatYouHave, true,
                    fleet, upgradeEntireFleet );


                #region shipsThatBenefit_ThatYouHave
                GameEntityTypeData ship;
                bool isFirst = true;
                foreach ( KeyValuePair<GameEntityTypeData, ShipDataByMark> pair in fleetUpgrade_Ships_ThatYouHave )
                {
                    if ( pair.Key.IsTurret || !pair.Key.IsMobile || pair.Key.FleetMembershipStyle == FleetMembershipStyle.Planetary )
                        continue; //defenses, so skip!

                    ship = pair.Key;
                    if ( isFirst )
                    {
                        if ( RefundAmount > 0 )
                            tooltipBuffer.Add( "\n<b><u><size=110%>将降低以下单位的标记等级（降低 " ).Add( upgradesSoFar ).Add( " 级）：</b></u></size>\n" );
                        else
                            tooltipBuffer.Add( "\n<b><u><size=110%>将提升以下单位的标记等级（提升一级）：</b></u></size>\n" );
                        isFirst = false;
                    }
                    else
                        tooltipBuffer.Add( ", " );
                    bool showIcons = InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1();
                    ShipListerUtils.WriteCountsAndStrengthsForEachMarkLevelFrom_ForTechUpgradeOrDowngrade( ship, pair.Value, tooltipBuffer,
                                    RefundAmount > 0 ? -upgradesSoFar : 1, playerFaction, showIcons );
                }
                int shipStrengthIncrease = 0;
                if ( RefundAmount > 0 )
                    shipStrengthIncrease = (fleet.UIOnly_Fleet_DowngradedShipStrengthIncrease_FromClearingMarkLevels);
                else
                    shipStrengthIncrease = (fleet.UIOnly_Fleet_UpgradedShipStrengthIncrease_FromOneMarkLevel);
                int defenseStrengthIncrease = 0;
                if ( RefundAmount > 0 )
                    defenseStrengthIncrease = (fleet.UIOnly_Fleet_DowngradedDefenseStrengthIncrease_FromClearingMarkLevels);
                else
                    defenseStrengthIncrease = (fleet.UIOnly_Fleet_UpgradedDefenseStrengthIncrease_FromOneMarkLevel);
                if ( shipStrengthIncrease > 0 && shipStrengthIncrease < 1000 )
                    shipStrengthIncrease = 1000;
                if ( defenseStrengthIncrease > 0 && defenseStrengthIncrease < 1000 )
                    defenseStrengthIncrease = 1000;

                if ( shipStrengthIncrease != 0 )
                {
                    tooltipBuffer.Add( shipStrengthIncrease < 0 ? "\n所有 " : "\n所有 " )
                        .Add( "单位", ArcenExternalUIUtilities.ShipLineIncreaseColor ).Add( " 的总战力变化约为 " )
                        .Add( ArcenExternalUIUtilities.GUI_StrengthTextIcon_AndShipLineIncreaseColor ).StartColor( ArcenExternalUIUtilities.ShipLineIncreaseColor )
                        .AddStrengthTiered( shipStrengthIncrease ).EndColor().Add( "。共影响 " );
                    if ( fleet.UIOnly_Fleet_ShipLinesAffected == 1 )
                        tooltipBuffer.Add( fleet.UIOnly_Fleet_ShipLinesAffected, ArcenExternalUIUtilities.ShipLineIncreaseColor ).Add( " 条单位线。" );
                    else if ( fleet.UIOnly_Fleet_ShipLinesAffected > 1 )
                        tooltipBuffer.Add( fleet.UIOnly_Fleet_ShipLinesAffected, ArcenExternalUIUtilities.ShipLineIncreaseColor ).Add( " 条单位线。" );
                }


                bool hadAnyOfTheGroupAbove = !isFirst;

                isFirst = true;
                foreach ( KeyValuePair<GameEntityTypeData, ShipDataByMark> pair in fleetUpgrade_Ships_ThatYouHave )
                {
                    if ( pair.Key.IsTurret || !pair.Key.IsMobile || pair.Key.FleetMembershipStyle == FleetMembershipStyle.Planetary )
                    { } //defenses, so do it!
                    else
                        continue; //skip the ships we already did

                    ship = pair.Key;
                    if ( isFirst )
                    {
                        if ( hadAnyOfTheGroupAbove )
                            tooltipBuffer.Add( "\n" );
                        if ( RefundAmount > 0 )
                            tooltipBuffer.Add( "\n<b><u><size=110%>将降低以下防御设施的标记等级（降低 " ).Add( upgradesSoFar ).Add( " 级）：</b></u></size>\n" );
                        else
                            tooltipBuffer.Add( "\n<b><u><size=110%>将提升以下防御设施的标记等级（提升一级）：</b></u></size>\n" );
                        isFirst = false;
                    }
                    else
                        tooltipBuffer.Add( ", " );
                    bool showIcons = InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1();
                    ShipListerUtils.WriteCountsAndStrengthsForEachMarkLevelFrom_ForTechUpgradeOrDowngrade( ship, pair.Value, tooltipBuffer,
                        RefundAmount > 0 ? -upgradesSoFar : 1, playerFaction, showIcons);
                }

                if ( defenseStrengthIncrease != 0 )
                {
                    tooltipBuffer.Add( defenseStrengthIncrease < 0 ? "\n所有 " : "\n所有 " )
                        .Add( "防御设施", ArcenExternalUIUtilities.DefenseLineIncreaseColor ).Add( " 的总战力变化约为 " )
                        .Add( ArcenExternalUIUtilities.GUI_StrengthTextIcon_AndDefenseLineIncreaseColor ).StartColor( ArcenExternalUIUtilities.DefenseLineIncreaseColor )
                        .AddStrengthTiered( defenseStrengthIncrease ).EndColor().Add( "。共影响 " );
                    if ( fleet.UIOnly_Fleet_DefensiveLinesAffected == 1 )
                        tooltipBuffer.Add( fleet.UIOnly_Fleet_DefensiveLinesAffected, ArcenExternalUIUtilities.DefenseLineIncreaseColor ).Add( " 种防御类型。" );
                    else if ( fleet.UIOnly_Fleet_DefensiveLinesAffected > 1 )
                        tooltipBuffer.Add( fleet.UIOnly_Fleet_DefensiveLinesAffected, ArcenExternalUIUtilities.DefenseLineIncreaseColor ).Add( " 种防御类型。" );

                }
                tooltipBuffer.Add( "\n" );
                #endregion

                {
                    tooltipBuffer.Add( "\n" ).Add( FontSizes.MUCH_SMALLER_SIZE_PLUS_A_TAD_STRING );
                    tooltipBuffer.Add( "<color=#3f6c9e>按住 </color><color=#4486d1>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "HoldAndClickToViewDetailsOfContents" ) )
                            .Add( "</color> <color=#3f6c9e>并点击此处可查看此次科技消耗升级的所有单位详情。</color>  " );
                    if ( GameSettings.Current.GetBoolBySetting( "UpgradeShipPrompt" ) )
                    {
                        tooltipBuffer.Add( "<color=#3f6c9e>按住 </color><color=#4486d1>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "SuppressTechUpgradePrompt" ) )
                            .Add( "</color> <color=#3f6c9e>可跳过科技升级的'你确定吗'提示。</color>  " );
                    }
                    tooltipBuffer.Add( "</size>" );
                }
            }
        }
        #endregion        

        #region bFleetLeaderTypeTransformation
        public class bFleetLeaderTypeTransformation : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "<color=#777777>" );
                Faction localR1Faction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();

                IFleetTransforms fleetTransforms = Instance._fleetToManage.GetFleetTransforms();
                GameEntity_Squad centerpOrNull = Instance._fleetToManage.Centerpiece.GetSquad();
                if ( centerpOrNull == null ) 
                {
                    buffer.Add( "旗舰已损毁！当前无法切换类型</color> " );
                    return; 
                }
                if ( fleetTransforms == null ) 
                {
                    buffer.Add( "没有可切换的类型！当前无法切换类型</color> " );
                    return;
                } 
                if ( !fleetTransforms.HasAnyTransforms ) 
                {
                    buffer.Add( fleetTransforms.NoTransformsText );
                    return;
                } 
                if ( centerpOrNull.GetIsCrippled() ) 
                {
                    buffer.Add( "受损状态下无法变形</color> " );
                    return;
                }

                int minHacking = 999;
                int maxHacking = 0;
                int minResourceOne = 999;
                int maxResourceOne = 0;
                foreach (IFleetTransformTarget type in fleetTransforms.TypesCanSwitchTo)
                {
                    if ( type.HackingCost > 0 ) {
                        if ( type.HackingCost < minHacking )
                            minHacking = type.HackingCost;
                        if ( type.HackingCost > maxHacking )
                            maxHacking = type.HackingCost;
                    }
                    if ( type.ResourceOneCost > 0 ) {
                        if ( type.ResourceOneCost < minResourceOne )
                            minResourceOne = type.ResourceOneCost;
                        if ( type.ResourceOneCost > maxResourceOne )
                            maxResourceOne = type.ResourceOneCost;
                    }
                }

                buffer.Add(fleetTransforms.DisplayName).Add(":").EndColor().Add(" ");
                
                if ( maxHacking > 0 || maxResourceOne > 0)
                {
                    if ( maxHacking > 0 )
                    {
                        buffer.Add( ArcenExternalUIUtilities.HackingTextColorAndIcon );
                        if ( minHacking == maxHacking )
                            buffer.AddNumberMoreReadable( minHacking );
                        else
                        {
                            buffer.AddNumberMoreReadable( minHacking );
                            buffer.Add( "-" );
                            buffer.AddNumberMoreReadable( maxHacking );
                        }
                        buffer.EndColor();
                    }
                    
                    if ( maxResourceOne > 0 )
                    {
                        buffer.Add( localR1Faction != null && localR1Faction.Resource1TextColorAndIcon.Length > 0 ? localR1Faction.Resource1TextColorAndIcon : World_AIW2.Instance.Resource1TextColorAndIcon );
                        if ( minResourceOne == maxResourceOne )
                            buffer.AddNumberMoreReadable( minResourceOne );
                        else
                        {
                            buffer.AddNumberMoreReadable( minResourceOne );
                            buffer.Add( "-" );
                            buffer.AddNumberMoreReadable( maxResourceOne );
                        }
                        buffer.EndColor();
                    }
                    
                    buffer.Add( " <color=#888888>变形" );
                }
                else
                {
                    buffer.Add("变形");
                }
            }

            private static ProtectedList<CustomPopupData> transformationOptions = ProtectedList<CustomPopupData>.Create_WillNeverBeGCed( 40, "Window_FleetManagementSidebarPopout-bFleetLeaderTypeTransformation-transformationOptions" );
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                IFleetTransforms fleetTransforms = Instance._fleetToManage.GetFleetTransforms();
                GameEntity_Squad centerpiece = Instance._fleetToManage.Centerpiece.GetSquad();
                if ( centerpiece == null || centerpiece.GetIsCrippled() )
                    return MouseHandlingResult.PlayClickDeniedSound;

                Faction facOrNull = centerpiece.GetFactionOrNull_Safe();
                PlayerTypeData playerType = facOrNull.PlayerTypeDataOrNull_ModeratelyExpensive;
                bool flagshipsMustBeUnique = (playerType.GetHasTag( "Necromancer" ) && AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "NecroFlagshipsMustBeUnique" ));
                FInt availableHackingPoints = facOrNull == null ? FInt.Zero : facOrNull.StoredHacking;
                FInt costForActiveHacks = HackingUtils.CalculateActiveHackingCosts(facOrNull);
                FInt availableResourceOne = facOrNull == null ? FInt.Zero : facOrNull.StoredFactionResourceOne;

                transformationOptions.Clear( true );

                #region fill transformation options

                var buffer = ArcenCharacterBuffer.GetFromPoolOrCreate("bFleetLeaderTypeTransformation.buffer");
                try
                {
                    foreach (IFleetTransformTarget target in fleetTransforms.TypesCanSwitchTo) 
                    {
                        //don't add an option for the type we already are
                        if ( target.TypeData == centerpiece.TypeData )
                            continue; 

                        bool alreadyHasType = false;
                        if ( flagshipsMustBeUnique )
                        {
                            foreach ( GameEntity_Squad e in facOrNull.Squads( EntityRollupType.FleetLeaders ) )
                            {
                                if ( e.TypeData == target.TypeData )
                                {
                                    alreadyHasType = true;
                                    break;
                                }
                            }
                        }

                        CustomPopupData option = CustomPopupData.GetFromPoolOrCreate();
                        if ( alreadyHasType )
                        {
                            option.CanBeSelected = false;
                            option.CannotBeSelectedReason = "你已经拥有了！";
                        }
                        else if (!target.CanTransformInto(Instance._fleetToManage, out string InvalidReason)) 
                        {
                            option.CanBeSelected = false;
                            option.CannotBeSelectedReason = InvalidReason;
                        } 
                        else if (target.HackingCost > availableHackingPoints) 
                        {
                            option.CanBeSelected = false;
                            option.CannotBeSelectedReason = "入侵点不足！";
                        } 
                        else if (target.HackingCost + costForActiveHacks > availableHackingPoints)
                        {
                            option.CanBeSelected = false;
                            option.CannotBeSelectedReason = "因正在进行的入侵导致入侵点不足！";
                        }
                        else if ( target.ResourceOneCost > availableResourceOne )
                        {
                            option.CanBeSelected = false;
                            option.CannotBeSelectedReason = "精华不足！";
                        }
                        else 
                        {
                            option.CanBeSelected = true;
                        }

                        buffer.Clear();
                        buffer.Align("center");
                        buffer.AddShipIconInline(target.TypeData, Instance._fleetToManage?.Faction).Add(target.TypeData.GetDisplayName());
                        
                        if (target.ResourceOneCost > 0 || target.HackingCost > 0)
                        {
                            buffer.Add(" ");
                        
                            if (target.ResourceOneCost > 0)
                                buffer.AddResourceOne(target.ResourceOneCost, true);
                            if (target.HackingCost > 0)
                                buffer.AddHacking(target.HackingCost, true);
                        }
                        buffer.EndAlign();
                        
                        option.DisplayName = buffer.ToString();
                        option.InternalName = target.InternalName;
                        option.Tooltiptext = target.TypeData.Description;
                        option.SortingName = target.TypeData.GetShortDisplayName();
                        
                        transformationOptions.Add( option );
                    }
                }
                finally
                {
                    buffer.ReturnToPool();
                }

                #endregion

                transformationOptions.Sort( static delegate ( CustomPopupData Left, CustomPopupData Right )
                {
                    return Left.SortingName.CompareTo( Right.SortingName );
                } );

                Window_PopupScrollingColumnButtonList.Instance.Open( "选择变形目标", null, transformationOptions,
                    delegate ( CustomPopupData _option )
                    {
                        if ( _option == null || !_option.CanBeSelected )
                            return;
                        if ( centerpiece.GetIsCrippled() )
                            return;
                        GameCommand command = fleetTransforms.CreateTransformCommand(GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer, centerpiece, _option.InternalName);
                        if (command == null)
                            return;
                        
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                    },
                    delegate( ArcenDoubleCharacterBuffer _buffer, CustomPopupData _option )
                    {
                        GameEntityTypeData typeData = fleetTransforms.GetTypeDataForName(_option.InternalName);
                        _buffer.NewLine();
                        EntityText.GetTooltip( _buffer, null, null, typeData, -1, centerpiece.GetFactionOrNull_Safe(),
                            centerpiece.CurrentMarkLevel, FromSidebarType.Sidebar_SingleUnit, ShipExtraDetailFlags.BuildInfo, 1f, false );
                        EntityText.Write_Tooltip_Hotkeys_Footer( _buffer, false, true, null );
                    } );

                return MouseHandlingResult.None;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_FleetManagementSidebarPopout-bFleetLeaderTypeTransformation-tooltipBuffer" );
            public override void HandleMouseover()
            {
                IFleetTransforms fleetTransforms = Instance._fleetToManage.GetFleetTransforms();
                fleetTransforms.GetTooltip(tooltipBuffer);
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText(
                    this.Element, tooltipBuffer.GetStringAndResetForNextUpdate());
            }
        }
        #endregion

        #region tFleetStrength
        public class tFleetStrength : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "<color=#777777>舰队战力（当前 / 最大）：</color> " );
                EntityText.AddSingleValueStrengthOnly( buffer, Instance._fleetToManage.GetCurrentStrengthOfFleet_ForUIOnly( Instance._fleetToManage.GetFactionType_Safe() == FactionType.NaturalObject ) );
                buffer.Add( " / " );
                EntityText.AddSingleValueStrengthOnly( buffer, Instance._fleetToManage.GetMaxStrengthOfFleet_ForUIOnly( Instance._fleetToManage.GetFactionType_Safe() == FactionType.NaturalObject ) );
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "不同舰队拥有不同的最大战力，但通过科技升级其中的单位/建筑可以使其更强大。\n\n请注意，最大战力假设所有单位/建筑都已部署，而当前战力则基于实际已建造的数量。" );
            }
        }
        #endregion
        
        #region tTransportModeHeader
        public class tTransportModeHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "运输模式：" );
            }

            public override void HandleMouseover()
            {
                tTransportModeHeader.TooltipForFactoryConstructionStatus( this.Element );
            }

            public static void TooltipForFactoryConstructionStatus( ArcenUI_Element Element )
            {
                if ( Instance._fleetToManage.IsFleetInTransportLoadMode )
                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( Element, "舰队处于运输装载模式，所有单位都会尝试进入旗舰。\n\n工厂为该舰队建造的新单位将直接进入旗舰，旗舰所在星球上的现有单位将以3倍速度移动并装入旗舰。\n\n位于其他星球上的单位将照常行动。默认按 'U' 键可将单位从旗舰中卸出。" );
                else
                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( Element, "舰队未处于运输模式，单位可自由行动。如果你想让它们装入旗舰以进行安全保管（或隐身，或在旗舰速度较快时更快通过），可以默认按 'L' 键将它们装入。它们只能在与旗舰同一星球时装入。" );
            }
        }
        #endregion

        #region bToggleTransportMode
        public class bToggleTransportMode : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( Instance._fleetToManage.IsFleetInTransportLoadMode )
                {
                    Buffer.StartColor( QuickColors.NewValue );
                    Buffer.Add( "运输中！装载入旗舰。" );
                    Buffer.EndColor();
                }
                else
                {
                    Buffer.Add( "未运输" );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                GameEntity_Squad centerpiece = Instance._fleetToManage.Centerpiece.GetSquad();
                if ( centerpiece == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                if ( Instance._fleetToManage.IsFleetInTransportLoadMode )
                {
                    GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.UnloadTransports], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    command.RelatedBools.Add( InputCaching.inputAddToSelection.CalculateIsKeyDownNow_IgnoreConflicts() ); //ToBeQueued
                    command.RelatedEntityIDs.Add( centerpiece.PrimaryKeyID );
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                }
                else
                {
                    GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetTransportIntoLoadMode], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    command.RelatedEntityIDs.Add( centerpiece.PrimaryKeyID );
                    command.PlanetOrderWasIssuedFrom = centerpiece.Planet.Index; //Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed();
                    command.ToBeQueued = false;// Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.Additive );
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                }

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                tTransportModeHeader.TooltipForFactoryConstructionStatus( this.Element );
            }
        }
        #endregion

        #region tFleetBehaviorHeader
        public class tFleetBehaviorHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "舰队行为：" );
            }

            public override void HandleMouseover()
            {
                tFleetBehaviorHeader.ShowTooltip( this.Element );
            }

            public static void ShowTooltip( ArcenUI_Element Element )
            {
                switch ( Instance._fleetToManage.Behavior )
                {
                    case FleetBehavior.WardenMode:
                        Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( Element, "舰队处于守卫模式。它将像守卫舰队一样行动，留守防御友方星球。如果你下达直接命令，它将执行命令然后继续防御。守卫舰队会在邻近星球的战斗中协助你的部队。\n\n点击按钮切换到下一个行为。" );
                        break;
                    case FleetBehavior.HunterMode:
                        Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( Element, "舰队处于猎手模式。它将像猎手舰队一样行动，主动寻找并攻击敌人。\n\n点击按钮切换到下一个行为。" );
                        break;
                    default:
                        Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( Element, "舰队处于玩家控制模式（默认）。它将正常响应玩家命令。\n\n点击按钮切换到下一个行为：守卫模式，然后是猎手模式。" );
                        break;
                }
            }
        }
        #endregion

        #region bCycleFleetBehavior
        public class bCycleFleetBehavior : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                switch ( Instance._fleetToManage.Behavior )
                {
                    case FleetBehavior.WardenMode:
                        Buffer.StartColor( "74c6ff" );
                        Buffer.Add( "守卫模式" );
                        Buffer.EndColor();
                        break;
                    case FleetBehavior.HunterMode:
                        Buffer.StartColor( "ff7474" );
                        Buffer.Add( "猎手模式" );
                        Buffer.EndColor();
                        break;
                    default:
                        Buffer.Add( "玩家控制", "74ffbc" );
                        break;
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                FleetBehavior next = (FleetBehavior)(((int)Instance._fleetToManage.Behavior + 1) % 3);
                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedIntegers.Add( Instance._fleetToManage.FleetID );
                command.RelatedString = "SetFleetBehavior";
                command.RelatedIntegers2.Add( (int)next );
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                tFleetBehaviorHeader.ShowTooltip( this.Element );
            }
        }
        #endregion

        #region tFleetMetricsHeader
        public class tFleetMetricsHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "舰队指标：" );
            }

            public override void HandleMouseover()
            {
                bShowFleetMetrics.ShowMetricsTooltip( this.Element );
            }
        }
        #endregion

        #region bShowFleetMetrics
        public class bShowFleetMetrics : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "悬停查看" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                ShowMetricsTooltip( this.Element );
            }

            public static void ShowMetricsTooltip( ArcenUI_Element Element )
            {
                FleetMetricsBaseInfo metrics = Instance._fleetToManage.BaseInfo as FleetMetricsBaseInfo;
                if ( metrics == null )
                    return;
                ArcenDoubleCharacterBuffer buffer = new ArcenDoubleCharacterBuffer( "bShowFleetMetrics.Tooltip" );
                metrics.AddMetricsToTooltipForFleet( buffer );
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( Element, buffer.GetStringAndResetForNextUpdate() );
            }
        }
        #endregion

        #region tFactoryConstructionStatusHeader
        public class tFactoryConstructionStatusHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "工厂建造状态：" );
            }

            public override void HandleMouseover()
            {
                tFactoryConstructionStatusHeader.TooltipForFactoryConstructionStatus( this.Element );
            }

            public static void TooltipForFactoryConstructionStatus( ArcenUI_Element Element )
            {
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( Element, "通常情况下，位于该舰队旗舰同一星球或相邻星球上的所有工厂都会为该舰队建造单位（如果需要的话）。\n\n如果你金属短缺或单纯希望该舰队不再补充单位，你可以禁用该舰队的建造。" );
            }
        }
        #endregion

        #region bToggleFactoryConstructionStatus
        public class bToggleFactoryConstructionStatus : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( Instance._fleetToManage.IsFleetConstructionPaused )
                    Buffer.Add( "<color=#ff784f>已暂停" );
                else
                    Buffer.Add( "<color=#74ffbc>正常（建造中）" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedIntegers.Add( Window_FleetManagementSidebarPopout.Instance._fleetToManage.FleetID ); //FleetID
                command.RelatedString = "ToggleFactoryConstructionStatus";
                command.RelatedBool = !Instance._fleetToManage.IsFleetConstructionPaused;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                tFactoryConstructionStatusHeader.TooltipForFactoryConstructionStatus( this.Element );
            }
        }
        #endregion

        #region tToggleIsFleetFlagshipStationaryStatusInverted
        public class tToggleIsFleetFlagshipStationaryStatusInverted : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "旗舰指令：" );
            }

            public override void HandleMouseover()
            {
                tToggleIsFleetFlagshipStationaryStatusInverted.TooltipForIsFleetFlagshipStationaryStatus( this.Element );
            }

            public static void TooltipForIsFleetFlagshipStationaryStatus( ArcenUI_Element Element )
            {
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( Element, "有些旗舰适合战斗，而有些适合留在后方。退后多远取决于你。\n\n当设置为 <color=#4fffb2>'执行所有命令'</color> 时，它将像舰队中的其他单位一样行动。这对巨像和方舟非常适用。但对没有武装的运输舰则不太合适。\n\n当设置为 <color=#ee3198>'旗舰驻停模式'</color> 时，旗舰将停留在原地，存储（但不执行）你下达的所有命令。假设这些命令是给舰队其余成员和从旗舰中产生的单位的。<b>这是将旗舰部署在一个星球上，同时为其单位设置到邻近星球集结点的好方法，还有其他用途。</b>\n\n要在驻停旗舰模式下覆盖此设置，请在向旗舰下达命令时按住 " +
                    InputActionTypeDataTable.GetActionByName_FairlySlow( "HoldToGiveOrdersToStationaryFlagships" ).GetHumanReadableKeyCombo() + "。旗舰将像没有驻停模式一样执行命令。这非常适合快速向你的无武装运输舰和它们所支援的舰队其余部分下达不同命令。" );
            }
        }
        #endregion

        #region bToggleIsFleetFlagshipStationaryStatusInverted
        public class bToggleIsFleetFlagshipStationaryStatusInverted : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( Instance._fleetToManage.GetIsIsFleetFlagshipStationary() )
                    Buffer.Add( "<color=#ee3198>旗舰驻停模式" );
                else
                    Buffer.Add( "<color=#4fffb2>执行所有命令" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedIntegers.Add( Window_FleetManagementSidebarPopout.Instance._fleetToManage.FleetID ); //FleetID
                command.RelatedString = "ToggleIsFleetFlagshipStationaryStatusInverted";
                command.RelatedBool = !Instance._fleetToManage.IsFleetFlagshipStationaryStatusOn;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                tToggleIsFleetFlagshipStationaryStatusInverted.TooltipForIsFleetFlagshipStationaryStatus( this.Element );
            }
        }
        #endregion

        #region tToggleIsFleetOnPlayerWatchlist
        public class tToggleIsFleetOnPlayerWatchlist : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "舰队监视列表：" );
            }

            public override void HandleMouseover()
            {
                tToggleIsFleetOnPlayerWatchlist.TooltipForIsFleetFlagshipStationaryStatus( this.Element );
            }

            public static void TooltipForIsFleetFlagshipStationaryStatus( ArcenUI_Element Element )
            {
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( Element, "通常在星球侧边栏上，你可以看到该星球上所有舰队的列表，包括旗舰的血量。但有时你想随时关注一个舰队，即使它在其他星球上。舰队监视列表允许你在侧边栏的星球选项卡中监控特定舰队，无论你去哪里。" );
            }
        }
        #endregion

        #region bToggleIsFleetOnPlayerWatchlist
        public class bToggleIsFleetOnPlayerWatchlist : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( Instance._fleetToManage.GetIsFleetOnPlayerWatchlist( PlayerAccount.Local.PlayerPrimaryKeyID ) )
                    Buffer.Add( "<color=#fd9632>已加入监视" );
                else
                    Buffer.Add( "<color=#999999>未加入监视" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedIntegers.Add( Window_FleetManagementSidebarPopout.Instance._fleetToManage.FleetID ); //FleetID
                command.RelatedIntegers.Add( PlayerAccount.Local.PlayerPrimaryKeyID );
                command.RelatedString = "ToggleIsFleetOnPlayerWatchlist";
                command.RelatedBool = !Instance._fleetToManage.GetIsFleetOnPlayerWatchlist( PlayerAccount.Local.PlayerPrimaryKeyID );
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                tToggleIsFleetOnPlayerWatchlist.TooltipForIsFleetFlagshipStationaryStatus( this.Element );
            }
        }
        #endregion

        #region tToggleFlagshipAllowedToUseMovementModesStatus
        public class tToggleFlagshipAllowedToUseMovementModesStatus : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "旗舰巡逻：" );
            }

            public override void HandleMouseover()
            {
                tToggleFlagshipAllowedToUseMovementModesStatus.TooltipForIsFleetFlagshipAllowedToUseMovementModes( this.Element );
            }

            public static void TooltipForIsFleetFlagshipAllowedToUseMovementModes( ArcenUI_Element Element )
            {
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( Element, "你的旗舰通常很重要，你不希望它们在追击模式或攻击移动模式下四处游荡。然而，你经常需要将它们设置为这些模式，以便它们建造的单位以这些模式出现。\n\n当设置为 <color=#4ff9ff>'除非直接命令否则不动'</color> 时，它将按预期行动，使旗舰忽略追击模式等，但其创建的单位会被放入该模式（并遵守该模式）。\n\n当设置为 <color=#ffc74f>'接到指令则巡逻'</color> 模式时，旗舰将在追击模式下追击，在攻击移动模式下攻击，等等。这对某些情况下的方舟和巨像很有用，甚至对机动兵工厂（在和平星球上）也有用。" );
            }
        }
        #endregion

        #region bToggleFlagshipAllowedToUseMovementModesStatus
        public class bToggleFlagshipAllowedToUseMovementModesStatus : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( Instance._fleetToManage.IsFleetFlagshipAllowedToUseMovementModes )
                    Buffer.Add( "<color=#ffc74f>接到指令则巡逻" ); 
                else
                    Buffer.Add( "<color=#4ff9ff>除非直接命令否则不动" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedIntegers.Add( Window_FleetManagementSidebarPopout.Instance._fleetToManage.FleetID ); //FleetID
                command.RelatedString = "ToggleFlagshipAllowedToUseMovementModesStatus";
                command.RelatedBool = !Instance._fleetToManage.IsFleetFlagshipAllowedToUseMovementModes;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                tToggleFlagshipAllowedToUseMovementModesStatus.TooltipForIsFleetFlagshipAllowedToUseMovementModes( this.Element );
            }
        }
        #endregion

        #region tDestroyFleetMembersOnOtherPlanetsHeader
        public class tDestroyFleetMembersOnOtherPlanetsHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "远方成员：" );
            }

            public override void HandleMouseover()
            {
                tDestroyFleetMembersOnOtherPlanetsHeader.TooltipForFactoryConstructionStatus( this.Element );
            }

            #region CalculateCountOfFleetMembersOnOtherPlanet
            public static int CalculateCountOfFleetMembersOnOtherPlanet()
            {
                return Instance._fleetToManage.CalculateCountOfFleetMembersOnOtherPlanet();
            }
            #endregion

            public static void TooltipForFactoryConstructionStatus( ArcenUI_Element Element )
            {
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( Element, "目前有 " + Instance._fleetToManage.CalculateCountOfFleetMembersOnOtherPlanet() + 
                    " 个该舰队成员位于远离旗舰的星球上。远方成员本身不是问题，但有时你想清除那些不在舰队主力中的单位。你可以点击此处快速轻松地完成。" );
            }
        }
        #endregion

        #region bDestroyFleetMembersOnOtherPlanets
        public class bDestroyFleetMembersOnOtherPlanets : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                int countOfDistant = tDestroyFleetMembersOnOtherPlanetsHeader.CalculateCountOfFleetMembersOnOtherPlanet();
                if ( countOfDistant <= 0 )
                {
                    Buffer.StartColor( QuickColors.NewValue );
                    Buffer.Add( "无！" );
                    Buffer.EndColor();
                }
                else
                {
                    Buffer.Add( countOfDistant + "（点击销毁）" );
                }
            }
            private static DictionaryOfSortedDictionaries<Planet, GameEntityTypeData, int> remoteShips = 
                DictionaryOfSortedDictionaries<Planet, GameEntityTypeData, int>.Create_WillNeverBeGCed( 300, 60, "Window_FleetManagementSidebarPopout-bDestroyFleetMembersOnOtherPlanets-remoteShips" );

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                GameEntity_Squad centerpiece = Instance._fleetToManage.Centerpiece.GetSquad();
                if ( centerpiece == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                int countOfDistant = tDestroyFleetMembersOnOtherPlanetsHeader.CalculateCountOfFleetMembersOnOtherPlanet();
                if ( countOfDistant > 0 )
                {
                    remoteShips.Clear();
                    System.Text.StringBuilder builder = new System.Text.StringBuilder();
                    Planet centerpiecePlanet = centerpiece.Planet;
                    builder.Append( "该舰队的旗舰位于星球 " ).Append( centerpiecePlanet.Name ).Append( "。是否销毁该舰队在其他星球上的所有成员？以下是：\n\n" );

                    foreach ( GameEntity_Squad squad in Instance._fleetToManage.Entities )
                    {
                        if ( squad.Planet != centerpiecePlanet )
                        {
                            SortedDictionary<GameEntityTypeData, int> shipsForPlanet = remoteShips[squad.Planet];
                            shipsForPlanet[squad.TypeData]++;
                        }
                    }
                    bool isFirst = true;
                    foreach ( KeyValuePair<Planet, SortedDictionary<GameEntityTypeData, int>> outerPair in remoteShips )
                    {
                        outerPair.Value.SortIntoList( delegate ( KeyValuePair <GameEntityTypeData, int> L, KeyValuePair <GameEntityTypeData, int> R)
                        {
                            return L.Key.DisplayName.CompareTo(R.Key.DisplayName);
                        } );
                        if ( isFirst )
                            isFirst = false;
                        else
                            builder.Append( ", " );
                        //First iterate over all the units to get the total
                        int totalUnits = 0;
                        foreach ( KeyValuePair<GameEntityTypeData, int> innerPair in outerPair.Value )
                        {
                            totalUnits += innerPair.Value;
                        }

                        builder.Append("星球 " + outerPair.Key.Name + " 共有 " + totalUnits +" 个单位：\n");
                        foreach ( KeyValuePair<GameEntityTypeData, int> innerPair in outerPair.Value )
                        {
                            builder.Append("\t" + innerPair.Key.GetDisplayName()).Append(": ").Append(innerPair.Value).Append("\n");
                        }
                    }

                    ModalPopupData.CreateAndLogYesNoStyle( delegate
                    {
                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.DestroyDistantFleetMembers], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                        command.RelatedEntityIDs.Add( centerpiece.PrimaryKeyID );
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                    }, null, "销毁远方单位？", builder.ToString(), "是，销毁它们", "不，等等！" );
                }
                else
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "没有需要清除的远方单位！", "该舰队没有位于旗舰所在星球以外的成员。", "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                tDestroyFleetMembersOnOtherPlanetsHeader.TooltipForFactoryConstructionStatus( this.Element );
            }
        }
        #endregion

        #region tChangeBolsteredFleetForCityHeader
        public class tChangeBolsteredFleetForCityHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( Instance._fleetToManage.Centerpiece.GetSquad().TypeData.NameForCityCenter ).Add( " Bolsters:" );
            }

            public override void HandleMouseover()
            {
                tChangeBolsteredFleetForCityHeader.TooltipForFleetBolstering( this.Element );
            }

            public static void TooltipForFleetBolstering( ArcenUI_Element Element )
            {
                if ( Instance._fleetToManage == null )
                    return;

                Fleet currentlyBolstering = World_AIW2.Instance.GetFleetByID( Instance._fleetToManage.CityBolstersFleetID );
                bool anythingToBolster = Instance._fleetToManage.GetCanBolsterAnything();
                string cityTerm = Instance._fleetToManage.Centerpiece.GetSquad().TypeData.NameForCityCenter;

                ArcenCharacterBuffer buffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "FleetManagementPopout-TooltipForFleetBolstering-buffer", 5f );
                if ( currentlyBolstering == null ) {
                    buffer.Add("此").Add(cityTerm).Add("当前未支援任何舰队。");
                    if (anythingToBolster) {
                        buffer.Add("最好将其设置为支援某个舰队，因为这是移动单位从这里建造到那里的方式。");
                    }
                } else {
                    buffer.Add("此").Add(cityTerm).Add("当前");
                    if (anythingToBolster) {
                        buffer.Add("正在支援 ");
                    } else {
                        buffer.Add("支援 ");
                    }
                    buffer.Add(currentlyBolstering.GetName()).Add("。");
                    if (anythingToBolster) {
                        buffer.Add(" 如果你想更改，可以点击此处操作。但在此期间，该舰队将从这个 ");
                        buffer.Add(cityTerm).Add(" 获得所有移动单位生产的好处。");
                    }
                }
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( Element, buffer.ToStringAndReturnToPool() );
            }
        }
        #endregion

        #region bChangeBolsteredFleetForCity
        public class bChangeBolsteredFleetForCity : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Fleet currentlyBolstering = World_AIW2.Instance.GetFleetByID( Instance._fleetToManage.CityBolstersFleetID );

                if ( currentlyBolstering != null )
                {
                    Buffer.StartColor( QuickColors.NewValue );
                    Buffer.Add( currentlyBolstering.GetName() );
                    Buffer.EndColor();
                }
                else
                {
                    Buffer.StartColor( QuickColors.OldValue );
                    Buffer.Add( "尚未选择！" );
                }
            }

            private static ProtectedList<CustomPopupData> bolsterOptions = ProtectedList<CustomPopupData>.Create_WillNeverBeGCed( 40, "Window_FleetManagementSidebarPopout-bChangeBolsteredFleetForCity-bolsterOptions" );
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( Instance._fleetToManage == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                List<SafeSquadWrapper> fleets = Instance._fleetToManage?.GetFleetsThatCanBeBolstered();

                if ( fleets == null || fleets.Count == 0 )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "目前没有可供支援的舰队！", "目前似乎没有可以被支援的舰队。", "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                bolsterOptions.Clear( true );
                #region Fill bolsterOptions
                foreach ( SafeSquadWrapper fleetCenterpice in fleets )
                {
                    GameEntity_Squad squad = fleetCenterpice.GetSquad();
                    if ( squad == null )
                        continue;
                    Fleet fleet = squad.GetFleetOrNull_Safe();
                    if ( fleet == null )
                        continue;
                    CustomPopupData option = CustomPopupData.GetFromPoolOrCreate();
                    option.CanBeSelected = Instance._fleetToManage.CityBolstersFleetID != fleet.FleetID; //can't assign to the one that is already selected

                    option.DisplayName = fleet.GetName();
                    option.InternalID = squad.PrimaryKeyID;
                    option.Tooltiptext = string.Empty;

                    bolsterOptions.Add( option );
                }
                #endregion

                bolsterOptions.Sort( static delegate ( CustomPopupData Left, CustomPopupData Right )
                {
                    return Left.DisplayName.CompareTo( Right.DisplayName );
                } );

                Window_PopupScrollingColumnButtonList.Instance.Open( "选择要支援的舰队", null, bolsterOptions,
                    delegate ( CustomPopupData Option )
                    {
                        if ( Option == null || !Option.CanBeSelected )
                            return;
                        GameEntity_Squad centerpiece = World_AIW2.Instance.GetEntityByID_Squad( Option.InternalID );
                        if ( centerpiece == null )
                            return;

                        GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.ChangeBolsterTarget], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                        command.RelatedMagnitude = Instance._fleetToManage.FleetID;
                        command.RelatedIntegers.Add( centerpiece.GetFleetID_Safe() );
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                    },
                    delegate ( ArcenDoubleCharacterBuffer buffer, CustomPopupData Option )
                    {
                        GameEntity_Squad centerpiece = World_AIW2.Instance.GetEntityByID_Squad( Option.InternalID );
                        if ( centerpiece == null )
                            return;
                        buffer.NewLine();
                        EntityText.GetTooltip( buffer, centerpiece, centerpiece.FleetMembership, centerpiece.TypeData, -1, centerpiece.GetFactionOrNull_Safe(),
                            centerpiece.CurrentMarkLevel, FromSidebarType.Sidebar_SingleUnit, ShipExtraDetailFlags.BuildInfo, 1f, false );
                        EntityText.Write_Tooltip_Hotkeys_Footer( buffer, false, true, null );
                    } );

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                tChangeBolsteredFleetForCityHeader.TooltipForFleetBolstering( this.Element );
                Fleet currentlyBolstering = World_AIW2.Instance.GetFleetByID( Instance._fleetToManage.CityBolstersFleetID );
                World_AIW2.Instance.FocusedSquadForMapDarkening = currentlyBolstering?.Centerpiece.GetSquad();
            }
        }
        #endregion

        #region tGiftFleetToOtherPlayerHeader
        public class tGiftFleetToOtherPlayerHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "赠送舰队：" );
            }

            public override void HandleMouseover()
            {
                tGiftFleetToOtherPlayerHeader.TooltipForFleetGifting( this.Element );
            }

            public static void TooltipForFleetGifting( ArcenUI_Element Element )
            {
                if ( Instance._fleetToManage == null )
                    return;

                ArcenRejectionReason cannotGiftFleetReason = Instance._fleetToManage.GetCanGiftFleetToAnotherPlayer();
                string text = string.Empty;
                switch (cannotGiftFleetReason)
                {
                    case ArcenRejectionReason.Unknown:
                        text = "如果你想将此舰队赠送给另一个人类帝国，你可以这样做。";
                        break;
                    case ArcenRejectionReason.CannotGiftFleet_FlagshipDeadOrMissing:
                        text = "该舰队缺少核心单位（旗舰或指挥站），因此无法在此状态下赠送。";
                        break;
                    case ArcenRejectionReason.CannotGiftFleet_Homeplanet:
                        text = "母星不能在玩家之间赠送！";
                        break;
                }
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( Element, text );
            }
        }
        #endregion

        #region bGiftFleetToOtherPlayer
        public class bGiftFleetToOtherPlayer : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                ArcenRejectionReason cannotGiftFleetReason = Instance._fleetToManage.GetCanGiftFleetToAnotherPlayer();
                
                if ( cannotGiftFleetReason == ArcenRejectionReason.Unknown )
                {
                    Buffer.StartColor( QuickColors.NewValue );
                    Buffer.Add( "赠送给其他玩家" );
                    Buffer.EndColor();
                }
                else
                {
                    Buffer.StartColor( QuickColors.OldValue );
                    Buffer.Add( "当前无法赠送" );
                }
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( Instance._fleetToManage == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                ArcenRejectionReason cannotGiftFleetReason = Instance._fleetToManage.GetCanGiftFleetToAnotherPlayer();
                if ( cannotGiftFleetReason != ArcenRejectionReason.Unknown )
                    return MouseHandlingResult.PlayClickDeniedSound;

                Window_GiftFleetToPlayer.Instance.Open( Instance._fleetToManage );

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                tGiftFleetToOtherPlayerHeader.TooltipForFleetGifting( this.Element );
            }
        }
        #endregion

        #region tFleetKeybindHeader
        public class tFleetKeybindHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( "<color=#777777>舰队快捷键：" );
            }

            public override void HandleMouseover()
            {
                tFleetKeybindHeader.TooltipForFleetKeybind( this.Element );
            }

            public static void TooltipForFleetKeybind( ArcenUI_Element Element )
            {
                int tiedTo = Instance._fleetToManage.GetTiedToKeybindIndexForDisplay();
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( Element, "你可以将一个或多个舰队绑定到键盘上的任意数字键。按下数字键将立即选中所有绑定到该键的舰队。\n\n该舰队当前绑定到快捷键：" +
                    (tiedTo < 0 ? "无" : tiedTo.ToString() ) );
            }
        }
        #endregion

        #region dFleetKeybindIndex
        public class dFleetKeybindIndex : DropdownAbstractBase
        {
            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;
                if ( SetType == DropdownSetType.FromMismatch )
                    return;
                DropdownOptionFleetKeybindIndexData selected = (DropdownOptionFleetKeybindIndexData)Item;
                if ( selected == null )
                    return;

                int indexPreviouslySelected = Instance._fleetToManage.TiedToKeybindIndexOneIndexed;
                if ( indexPreviouslySelected == selected.UnderlyingIndex )
                    return;

                //set it immediately so that FromMismatch doesn't get triggered
                //this is client-side nonsim anyway, so it doesn't hurt anything or cause desyncs
                Instance._fleetToManage.TiedToKeybindIndexOneIndexed = (Int16)selected.UnderlyingIndex;

                //then actually send it to the server permanently so that it gets saved into savegames
                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedIntegers.Add( Window_FleetManagementSidebarPopout.Instance._fleetToManage.FleetID ); //FleetID
                command.RelatedString = "FleetKeybindIndex";
                command.RelatedIntegers2.Add( selected.UnderlyingIndex );
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );                
            }

            private bool hasEverSetUp = false;
            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                int indexToSelect = Instance._fleetToManage.TiedToKeybindIndexOneIndexed;

                if ( !hasEverSetUp )
                {
                    hasEverSetUp = true;
                    for ( int i = 0; i <= 10; i++ )
                    {
                        DropdownOptionFleetKeybindIndexData option = new DropdownOptionFleetKeybindIndexData( i );
                        elementAsType.AddItem( option, i == indexToSelect );
                    }
                }

                DropdownOptionFleetKeybindIndexData selected = (DropdownOptionFleetKeybindIndexData)elementAsType.CurrentlySelectedOption;
                if ( selected == null || selected.UnderlyingIndex != indexToSelect )
                {
                    elementAsType.SetSelectedItem( indexToSelect, DropdownSetType.FromMismatch );
                }
            }
            public override void HandleMouseover()
            {
                tFleetKeybindHeader.TooltipForFleetKeybind( this.Element );
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenUI_Dropdown_Option Item )
            {
                //tFleetKeybindHeader.TooltipForFleetKeybind();
            }
        }

        public class DropdownOptionFleetKeybindIndexData : IArcenUI_Dropdown_Option
        {
            public int UnderlyingIndex;
            public int DisplayIndex;

            public DropdownOptionFleetKeybindIndexData( int Index )
            {
                this.UnderlyingIndex = Index;
                this.DisplayIndex = Index;
                if ( this.DisplayIndex <= 0 )
                    this.DisplayIndex = -1;
                if ( this.DisplayIndex == 10 )
                    this.DisplayIndex = 0;
            }

            public object GetItem()
            {
                return this.UnderlyingIndex;
            }

            public string GetOptionNameFromVolatile()
            {
                if ( this.DisplayIndex < 0 )
                    return "无";
                return this.DisplayIndex.ToString();
            }

            public Sprite GetOptionSprite()
            {
                return null;
            }
        }
        #endregion

        #region GetMembershipFromElement
        public static FleetMembership GetMembershipFromElement( ArcenUI_Element element )
        {
            if ( element == null || Instance._fleetToManage == null )
                return null;
            if ( element.CreatedByCodeDirective == null )
                return null;
            int index = element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            if ( index < 0 || index >= memLookups.Count )
                return null;
            return memLookups[index];
        }
        #endregion

        #region tBlankSpace
        public class tBlankSpace : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                
            }
            
            public override void HandleMouseover()
            {
                
            }
        }
        #endregion

        #region tSingleLineInfo
        public class tSingleLineInfo : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                string text = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString;
                if ( text.Length > 80 )
                    text = text.Substring( 0, 80 ) + "...";
                buffer.StartColor( "ff8942" ).Add( text );
            }

            public override void HandleMouseover()
            {
                string text = this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString;

                if ( text.Length > 0 )
                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, text );
            }
        }
        #endregion

        #region tFleetMemberHeader
        public class tFleetMemberHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                FleetMembership mem = GetMembershipFromElement( this.Element );
                if ( mem == null )
                    return;
                GameEntityTypeData typeData = mem.TypeData;
                if ( typeData == null )
                    return;
                GameEntityTypeData.MarkLevelStats markDataOrNull = mem.ForMark;

                string drawName = mem.GetDisplayNameForSidebar();
                int effectiveHere = mem.GetCountPresent( true, ExtraFromStacks.IncludePrecalc );
                if ( drawName.Length + mem.CalculateTransportedContentsCount().ToString().Length + effectiveHere.ToString().Length >= 25 )
                    buffer.Add("<size=85%>");
                else if ( drawName.Length + mem.CalculateTransportedContentsCount().ToString().Length + effectiveHere.ToString().Length >= 20 )
                    buffer.Add("<size=90%>");
                else if ( drawName.Length + mem.CalculateTransportedContentsCount().ToString().Length + effectiveHere.ToString().Length >= 17 )
                    buffer.Add("<size=95%>");
                bool isFleetLeader = typeData.IsFleetLeader;
                if ( !isFleetLeader )
                {
                    int strengthToShow = mem.GetStrengthPerSquad_PlayerFleetsOnly();
                    if ( mem.EffectiveSquadCap > 1 && mem.EffectiveSquadCap > effectiveHere )
                        strengthToShow *= mem.EffectiveSquadCap;
                    else if ( effectiveHere > 1 )
                        strengthToShow *= effectiveHere;

                    string colorString = ArcenExternalUIUtilities.StrengthTextColor;
                    buffer.Add( "<color=#" ).Add( colorString ).Add( ">" );
                    ArcenExternalUIUtilities.GUI_WriteStrengthIconWithColor( buffer, colorString );
                    ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, strengthToShow, true, true );
                    buffer.EndColor();
                }
                else
                {
                    int strengthToShow = mem.GetStrengthPerSquad_PlayerFleetsOnly();
                    string colorString = ArcenExternalUIUtilities.StrengthTextColor;
                    buffer.Add( "<color=#" ).Add( colorString ).Add( ">" );
                    ArcenExternalUIUtilities.GUI_WriteStrengthIconWithColor( buffer, colorString );
                    ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, strengthToShow, true, true );
                    buffer.EndColor();
                }

                buffer.Add( "<pos=50>" );
                buffer.AddShipIconInline(typeData, World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull() );
                buffer.Add( drawName == null || drawName.Length == 0 ? "nulltype" : drawName );

                if ( markDataOrNull != null )
                    buffer.StartColor( markDataOrNull.MarkLevel.ColorHex );
                buffer.Add( " " )
                    .Add( markDataOrNull == null ? "nullmark" : markDataOrNull.MarkLevel.Abbreviation );
                if ( markDataOrNull != null )
                    buffer.EndColor();

                if ( typeData.CannotActuallyBeBuilt_IsNotAShipLine )
                    return;
                
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
                        buffer.Add( " x" ).Add( effectiveHere );
                        if ( !mem.TypeData.NoExplicitCap )
                            buffer.Add( "/" ).Add( mem.EffectiveSquadCap ); //we almost always want to print the cap, but some unit types (necromancer in particular) use a different sort of cap
                        buffer.EndColor();
                    }

                    if ( mem.TransportContents.Count > 0 )
                        buffer.StartColor( ColorMath.IceBlue ).Add( " (T. " ).Add( mem.CalculateTransportedContentsCount() ).Add( ")" ).EndColor();
                }
                else //yes is a fleet leader
                {
                    ArcenOverLinkedList<GameEntity_Squad>.ArcenLinkedListItem firstItem = mem.EntitiesOfFMem.GetFirst();
                    if ( firstItem == null )
                        buffer.StartColor( QuickColors.OldValue ).Add( "（缺失）" ).EndColor();
                    else
                    {
                        GameEntity_Squad centerpiece = firstItem.Contained;
                        if ( centerpiece == null )
                            buffer.StartColor( QuickColors.OldValue ).Add( "（缺失）" ).EndColor();
                        else if ( centerpiece.GetIsCrippled() )
                            buffer.StartColor( QuickColors.OldValue ).Add( "（受损）" ).EndColor();
                        else if ( centerpiece.GetIsNonFunctional() )
                            buffer.StartColor( QuickColors.OldValue ).Add( "（失能）" ).EndColor();
                    }
                }
                if ( drawName.Length + mem.CalculateTransportedContentsCount().ToString().Length + effectiveHere.ToString().Length >= 17 )
                    buffer.Add("</size>");

                if ( typeData.IsModular && markDataOrNull != null )
                {
                    if ( mem.FreeModulePoints() > 0 )
                        buffer.Add( "\n<pos=50><size=70%><color=#ff6d40>" + drawName + " 中有未使用的模块点数</color></size>" );
                }
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_FleetManagementSidebarPopout-tFleetMemberHeader-tooltipBuffer" );
            public override void HandleMouseover()
            {
                FleetMembership mem = GetMembershipFromElement( this.Element );
                if ( mem == null )
                    return;

                GameEntity_Squad singleEntity = null;
                if ( mem.EntitiesOfFMem.Count == 1 )
                    singleEntity = mem.EntitiesOfFMem.GetFirst().Contained;

                EntityText.GetTooltip( tooltipBuffer, singleEntity, mem, null, -1, null, 0, FromSidebarType.Sidebar_MultipleUnits, ShipExtraDetailFlags.BuildInfo, 1f, false );
                EntityText.Write_Tooltip_Hotkeys_Footer( tooltipBuffer, false, true, null );

                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
        }
        #endregion

        #region tFleetEmptySlotsHeader
        public class tFleetEmptySlotsHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                int emptySlots = Instance._fleetToManage.CalculateRemainingShipLineSlotCount();
                buffer.Add( "<pos=50>" );
                buffer.Add( emptySlots );
                buffer.Add( " 个空槽位" );
                if ( Instance._fleetToManage.CalculateIsEliteSlotFilled() )
                    buffer.Add( " <size=75%>（0 个精英）</size>" );
                else
                    buffer.Add( " <size=75%>（1 个精英）</size>" );
            }
            
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "空槽位允许你通过入侵 ARS 来填充空槽位，或通过与其他舰队交换单位线来定制你的舰队。" );
            }
        }
        #endregion

        #region bOpenModulePanel
        public class bOpenModulePanel : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                FleetMembership mem = GetMembershipFromElement( this.Element );
                if ( mem == null )
                    return;
                
                Buffer.Add( "<size=75%>" );
                if ( mem.TypeData.IsModular )
                {
                    Buffer.Add( "<color=#74ffbc>模块" );
                    if ( mem.TypeData.IsModular )
                    {
                        GameEntityTypeData.MarkLevelStats markDataOrNull = mem.ForMark;
                        if ( markDataOrNull != null && mem.CalculateCurrentModuleCost() < markDataOrNull.ModulePointsAvailable )
                            Buffer.Add( "\n<size=60%><color=#ff6d40>未使用</color></size>" );
                    }
                }
                else
                    Buffer.Add( "<color=#6d3c2e>无模块" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                FleetMembership mem = GetMembershipFromElement( this.Element );
                if ( mem == null || !mem.TypeData.IsModular )
                    return MouseHandlingResult.PlayClickDeniedSound;

                Window_ModalFleetMemberModularEditing.Instance.Open( mem );

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                FleetMembership mem = GetMembershipFromElement( this.Element );
                if ( mem == null )
                    return;

                if ( mem.TypeData.IsModular )
                    Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "这是模块化单位！你可以在此处控制整个舰队线的装备配置。同一条线中的所有单位共享它们的装备。" );
                else
                    Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "这不是模块化单位线。如果是的话，你将在此处控制其装备。" );
            }
        }
        #endregion

        #region bToggleFleetMemberConstruction
        public class bToggleFleetMemberConstruction : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                FleetMembership mem = GetMembershipFromElement( this.Element );
                if ( mem == null )
                    return;

                Buffer.Add( "<size=75%>" );
                if ( mem.IsFleetMembershipConstructionPaused )
                    Buffer.Add( "<color=#ff784f>已暂停" );
                else
                    Buffer.Add( "<color=#74ffbc>建造" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                FleetMembership mem = GetMembershipFromElement( this.Element );
                if ( mem == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedIntegers.Add( Window_FleetManagementSidebarPopout.Instance._fleetToManage.FleetID ); //FleetID
                command.RelatedString = "ToggleFleetMembershipConstructionStatus";
                command.RelatedIntegers2.Add( mem.UniqueTypeDataDifferentiatorForDuplicates );
                command.RelatedBool = !mem.IsFleetMembershipConstructionPaused;
                command.RelatedString2 = mem.TypeData.InternalName;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "如果你不希望该舰队的特定部分由本星球或相邻星球的工厂自动补充，你可以暂停建造，这样就不会建造该类型的新单位。" );
            }
        }
        #endregion

        #region bSwapFleetMember
        public class bSwapFleetMember : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "<size=90%>交换" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                FleetMembership mem = GetMembershipFromElement( this.Element );
                if ( mem == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                
                Window_ModalSwapFleetMembers.Instance.Open( mem.Fleet, mem );

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "将此单位类型与你控制的另一个移动舰队中的不同类型交换。" );
            }
        }
        #endregion

        #region bSwapAllMembers
        public class bSwapAllMembers : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "<size=85%>全部交换" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                FleetMembership mem = GetMembershipFromElement( this.Element );
                if ( mem == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                
                Window_ModalSwapFleetMembers.Instance.Open( mem.Fleet, mem );

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "将该舰队中所有非旗舰、非无人机的成员与另一个舰队交换。" );
            }
        }
        #endregion

        #region bSwapFleetEmptySlot
        public class bSwapFleetEmptySlot : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "<size=90%>批量交换" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( Instance._fleetToManage == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                
                Window_ModalSwapFleetMembers.Instance.Open( Instance._fleetToManage, null );

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "从你或人类盟友控制的其他舰队中交换一条或多条单位线。" );
            }
        }
        #endregion
    }

    public static partial class FleetExtensions
    {
        #region GetIsFleetToHaveScienceButton
        public static bool GetIsFleetToHaveScienceButton( this Fleet F, Faction localFaction )
        {
            if ( F.Category == FleetCategory.PlayerPlanetaryCommand ||
               F.Category == FleetCategory.PlayerBattlestation )
            {
                return true;
            }
            else
            {
                //other fleet types get to choose for themselves
                GameEntity_Squad centerpiece = F.Centerpiece.GetSquad();
                if ( centerpiece != null && (centerpiece.TypeData.IsUpgradeableByDirectScience) )
                {
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}
