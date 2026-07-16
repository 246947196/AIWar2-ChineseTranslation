using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using TMPro;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameSelectionInfo : WindowControllerAbstractBase
    {
        public static Window_InGameSelectionInfo Instance;
        public Window_InGameSelectionInfo()
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
            if ( InputCaching.CalculateShouldTooltipsBeSuppressed() )
                return false;
            if ( Engine_AIW2.Instance.GetHasSelectionEvenIncludingOnesICannotGiveOrdersTo() )
            { }
            else
                return false;

            return true;
        }
        #endregion

        private static ButtonAbstractBase.ButtonPool<btnSelectedShipEntry> btnSelectedShipEntryPool;

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                Window_InGameSelectionInfo.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.TimeSinceStartF;
            }

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_InGameSelectionInfo.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( btnSelectedShipEntry.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnSelectedShipEntryPool = new ButtonAbstractBase.ButtonPool<btnSelectedShipEntry>( btnSelectedShipEntry.Original, 10 );
                        }
                    }
                    #endregion
                }

                this.WindowController.myScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" );
                //this.Element.Window.MaxDeltaTimeBeforeUpdates = -1;

                float currentY = 0;

                this.OnUpdateSelected( ref currentY );

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)btnSelectedShipEntry.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }

            int heightToShow = 0;

            #region OnUpdateSelected
            private readonly List<Fleet> fleetsToAdd = List<Fleet>.Create_WillNeverBeGCed( 200, "Window_InGameSelectionInfo-fleetsToAdd" );
            private readonly List<SafeSquadWrapper> squadsToAdd_Sublist = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 3000, "Window_InGameSelectionInfo-squadsToAdd_Sublist" );
            private readonly List<FourTuple<Faction,GameEntityTypeData,FleetMembership,bool>> typesToAdd_General = List<FourTuple<Faction, GameEntityTypeData, FleetMembership, bool>>.Create_WillNeverBeGCed( 300, "Window_InGameSelectionInfo-typesToAdd_General" );

            private readonly Dictionary<Faction, bool> workingFactionList = Dictionary<Faction, bool>.Create_WillNeverBeGCed( 250, "Window_InGameSelectionInfo-workingFactionList" );

            public void OnUpdateSelected( ref float currentY )
            {
                if ( !hasGlobalInitialized )
                    return;
                btnSelectedShipEntryPool.Clear( 10 );
                fleetsToAdd.Clear();
                squadsToAdd_Sublist.Clear();
                typesToAdd_General.Clear();
                workingFactionList.Clear();

                for ( int i = 0; i < GameEntityTypeDataTable.Instance.Rows.Count; i++ )
                {
                    GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.Rows[i];
                    typeData.NonSim_SelectionWindow_OtherFac_Normal.Clear();
                    typeData.NonSim_SelectionWindow_OtherFac_Remains.Clear();
                    typeData.NonSim_SelectionWindow_FleetNotSelected_MyFac_Normal.Clear();
                    typeData.NonSim_SelectionWindow_FleetNotSelected_MyFac_Remains.Clear();
                    typeData.NonSim_SelectionWindow_FleetIsSelected_MyFac_Normal.Clear();
                    typeData.NonSim_SelectionWindow_FleetIsSelected_MyFac_Remains.Clear();
                    typeData.NonSim_SelectionWindow_FleetMembership_Normal.Clear();
                    typeData.NonSim_SelectionWindow_FleetMembership_Remains.Clear();
                }

                hadAnySelected_GroupMove = false;
                hadAnySelected_StopToShoot = false;
                hadAnySelected_Pursuit = false;
                hadAnySelected_AttackMove = false;
                hadAnySelected_HoldFire = false;
                try
                {
                    if ( Engine_AIW2.Instance.GetHasSelectionEvenIncludingOnesICannotGiveOrdersTo() )
                    {
                        foreach ( GameEntity_Squad squad in Engine_AIW2.Instance.SelectedSquadsEvenIncludingOnesICannotGiveOrdersTo )
                        {
                            DelReturn r = SelectedEntityCounter( squad );
                            if ( r == DelReturn.Break ) break;
                        }
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "选择信息文本生成异常:" + e.ToString(), Verbosity.ShowAsError );
                }

                int countAdded = 0;

                Faction localFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                int strengthToReport = 0;
                if ( localFactionOrNull != null )
                {
                    for ( int i = 0; i < World_AIW2.Instance.AllFleets.Count; i++ )
                    {
                        Fleet data = World_AIW2.Instance.AllFleets[i];
                        if ( !data.IsConsideredSelected_NonSim )
                            continue;
                        if ( data.Faction != localFactionOrNull )
                        {
                            data.IsConsideredSelected_NonSim = false;
                            continue;
                        }
                        fleetsToAdd.Add( data );
                        strengthToReport += data.GetCurrentStrengthOfFleet_ForUIOnly( data.GetFactionType_Safe() == FactionType.NaturalObject );
                        countAdded++;
                    }
                }
                if ( localFactionOrNull == null )
                    localFactionOrNull = World_AIW2.Instance.GetFirstPlayerFactionOrNull(); //spectator mode

                bool includeShipsFromFleets = GameSettings.Current.GetBoolBySetting( "IncludeSelectedShipsThatAreSelectedByFleet" );

                for ( int i = 0; i < GameEntityTypeDataTable.Instance.Rows.Count; i++ )
                {
                    GameEntityTypeData data = GameEntityTypeDataTable.Instance.Rows[i];
                    int normalCount = data.NonSim_SelectionWindow_FleetNotSelected_MyFac_Normal.Count;
                    if ( includeShipsFromFleets )
                        normalCount += data.NonSim_SelectionWindow_FleetIsSelected_MyFac_Normal.Count;

                    int remainsCount = data.NonSim_SelectionWindow_FleetNotSelected_MyFac_Remains.Count;
                    if ( includeShipsFromFleets )
                        remainsCount += data.NonSim_SelectionWindow_FleetIsSelected_MyFac_Remains.Count;

                    if ( normalCount > 0 )
                    {
                        strengthToReport += data.Non_Sim_SelectionWindow_CalculateStrength( false );
                        typesToAdd_General.Add( FourTuple<Faction, GameEntityTypeData, FleetMembership, bool>.Create( localFactionOrNull, data, null, false ) );
                        countAdded++;
                        if ( remainsCount > 0 )
                            countAdded++;
                    }
                    if ( remainsCount > 0 )
                    {
                        typesToAdd_General.Add( FourTuple<Faction, GameEntityTypeData, FleetMembership, bool>.Create( localFactionOrNull, data, null, true ) );
                        countAdded++;
                    }

                    if ( data.NonSim_SelectionWindow_FleetMembership_Normal.Count > 0 )
                    {
                        //modular ship - not remains
                        foreach ( FleetMembership mem in data.NonSim_SelectionWindow_FleetMembership_Normal )
                        {
                            foreach ( GameEntity_Squad ship in mem.Entities )
                            {
                                if ( ship.SecondsSpentAsRemains > 0 )
                                    continue; //skip remains
                                Fleet myFleet = ship.GetFleetOrNull_Safe();
                                if ( myFleet != null && !myFleet.IsConsideredSelected_NonSim)
                                    strengthToReport += ship.GetStrengthOfSelfAndContentsButNotFleetCentricBits(); //we would be double-counting to add this in for fleets that are selected
                            }

                            typesToAdd_General.Add( FourTuple<Faction, GameEntityTypeData, FleetMembership, bool>.Create( mem.Fleet.Faction, mem.TypeData, mem, false ) );
                            countAdded++;
                            if ( remainsCount > 0 )
                                countAdded++;
                        }
                    }
                    if ( data.NonSim_SelectionWindow_FleetMembership_Remains.Count > 0 )
                    {
                        //modular ship - remains
                        foreach ( FleetMembership mem in data.NonSim_SelectionWindow_FleetMembership_Remains )
                        {
                            typesToAdd_General.Add( FourTuple<Faction, GameEntityTypeData, FleetMembership, bool>.Create( mem.Fleet.Faction, mem.TypeData, mem, true ) );
                            countAdded++;
                            if ( remainsCount > 0 )
                                countAdded++;
                        }
                    }

                    if ( data.NonSim_SelectionWindow_OtherFac_Normal.Count > 0 )
                    {
                        workingFactionList.Clear();
                        GameEntity_Squad squad;
                        for ( int j = 0; j < data.NonSim_SelectionWindow_OtherFac_Normal.Count; j++ )
                        {
                            squad = data.NonSim_SelectionWindow_OtherFac_Normal[j].GetSquad();
                            if ( squad == null )
                                continue;
                            Faction fac = squad.GetFactionOrNull_Safe();
                            if ( fac == null )
                                continue;
                            if ( workingFactionList.ContainsKey( fac ) )
                                continue;
                            workingFactionList[fac] = true;

                            typesToAdd_General.Add( FourTuple<Faction, GameEntityTypeData, FleetMembership, bool>.Create( fac, data, null, false ) );
                            countAdded++;
                        }
                    }
                    if ( data.NonSim_SelectionWindow_OtherFac_Remains.Count > 0 )
                    {
                        workingFactionList.Clear();
                        GameEntity_Squad squad;
                        for ( int j = 0; j < data.NonSim_SelectionWindow_OtherFac_Remains.Count; j++ )
                        {
                            squad = data.NonSim_SelectionWindow_OtherFac_Remains[j].GetSquad();
                            if ( squad == null )
                                continue;
                            Faction fac = squad.GetFactionOrNull_Safe();
                            if ( fac == null )
                                continue;
                            if ( workingFactionList.ContainsKey( fac ) )
                                continue;
                            workingFactionList[fac] = true;

                            typesToAdd_General.Add( FourTuple<Faction, GameEntityTypeData, FleetMembership, bool>.Create( fac, data, null, true ) );
                            countAdded++;
                        }
                    }
                }
                if ( strengthToReport > 0 )
                {
                    ArcenCharacterBuffer acb = ArcenCharacterBuffer.GetFromPoolOrCreate( "Window_InGameSelectionInfo-OnUpdateSelected" );
                    acb.Add( "<color=#cccc03>大约选择战力</color>: " ).WrapStrengthTruncated( strengthToReport, true, false );
                    btnSelectedShipEntry item = btnSelectedShipEntryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item != null )
                    {
                        item.Assign( acb.ToStringAndReturnToPool() );
                        countAdded++;
                    }
                }

                {
                    //perhaps this reminder text should be disablable by a setting, since it is consuming valuable window space.
                    //I could imagine experienced players might want it disablable?
                    string text ="<color=#cccc03>按住 </color>: <color=#ff007f>" + InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key2" ) + "</color> 查看快捷键";
                    btnSelectedShipEntry item = btnSelectedShipEntryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item != null )
                    {
                        item.Assign( text );
                        countAdded++;
                    }
                }

                //first do the fleets
                if ( fleetsToAdd.Count > 0 )
                {
                    #region Sort Fleets
                    if ( fleetsToAdd.Count > 1 )
                    {
                        fleetsToAdd.Sort( static delegate ( Fleet left, Fleet right )
                        {
                            int leftKey = left.GetTiedToKeybindIndexForDisplay();
                            int rightKey = left.GetTiedToKeybindIndexForDisplay();
                            //sort first by ones that have keys, compared to ones that do no
                            int val = (rightKey <= 0).CompareTo( leftKey <= 0 );
                            if ( val != 0 )
                                return val;
                            val = leftKey.CompareTo( rightKey ); //then asc sort by hotkey
                            if ( val != 0 )
                                return val;
                            val = left.GetName().CompareTo( right.GetName() ); //then asc sort by name
                            if ( val != 0 )
                                return val;
                            return left.FleetID.CompareTo( right.FleetID ); //finally sort by fleetID if they have identical names
                        } );
                    }
                    #endregion

                    foreach ( Fleet data in fleetsToAdd )
                    {
                        btnSelectedShipEntry item = btnSelectedShipEntryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( item == null )
                            break; //time slicing, too many added right now
                        item.Assign( data );
                    }
                }

                //last do the standalone individual ships
                if ( typesToAdd_General.Count > 0 )
                {
                    #region Sort Ship Types
                    if ( typesToAdd_General.Count > 1 )
                    {
                        typesToAdd_General.Sort( static delegate ( FourTuple<Faction, GameEntityTypeData, FleetMembership, bool> left, FourTuple<Faction, GameEntityTypeData, FleetMembership, bool> right )
                        {
                            int val = right.SecondItem.IsFleetLeader.CompareTo( left.SecondItem.IsFleetLeader ); //fleet leaders first
                            if ( val != 0 )
                                return val;
                            val = left.SecondItem.DisplayNameForSidebar.CompareTo( right.SecondItem.DisplayNameForSidebar ); //then asc sort by name
                            if ( val != 0 )
                                return val;
                            val = left.SecondItem.RowIndexNonSim.CompareTo( right.SecondItem.RowIndexNonSim ); //then sort by row index if they have identical names
                            if ( val != 0 )
                                return val;
                            val = right.FourthItem.CompareTo( left.FourthItem ); //then sort by if they are remains or not
                            if ( val != 0 )
                                return val;
                            val = (left.ThirdItem == null ? string.Empty : left.ThirdItem.GetDisplayNameForSidebar() ).CompareTo( 
                                (right.ThirdItem == null ? string.Empty : right.ThirdItem.GetDisplayNameForSidebar()) ); //then sort by their custom sidebar names if they are modular
                            if ( val != 0 )
                                return val;
                            return left.FirstItem.FactionIndex.CompareTo( right.FirstItem.FactionIndex ); //finally sort by faction index if they are the same item
                        } );
                    }
                    #endregion

                    foreach ( FourTuple<Faction, GameEntityTypeData, FleetMembership, bool> data in typesToAdd_General )
                    {
                        btnSelectedShipEntry item = btnSelectedShipEntryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( item == null )
                            break; //time slicing, too many added right now
                        item.Assign( data.SecondItem, data.FirstItem, data.ThirdItem, data.FourthItem );
                    }
                }

                int maxRows = GameSettings.Current.GetIntBySetting( "MaxLinesToShowForSelectedShipsBeforeScrolling" );
                int rowsToShow = Mathf.Min( maxRows, countAdded );
                int newHeight = 58 + ( rowsToShow * 19 );
                if ( heightToShow != newHeight )
                {
                    heightToShow = newHeight;
                    this.Element.RelevantRect.anchorMin = new Vector2( 0, 0 );
                    this.Element.RelevantRect.anchorMax = new Vector2( 0, 0 );
                    this.Element.RelevantRect.pivot = new Vector2( 0, 0 );
                    this.Element.RelevantRect.UI_SetHeight( heightToShow );
                }

                if ( btnGroupMove.Instance != null )
                {
                    btnGroupMove.Instance.SetHighlightOn( hadAnySelected_GroupMove );
                    FindAndAssignHotkeyTextSubObjectIfNeeded( btnGroupMove.Instance );
                    SetHotkeyText( btnGroupMove.Instance, InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( btnGroupMove.KeyComboAdd ));
                }
                if ( btnStopAndShoot.Instance != null )
                {
                    btnStopAndShoot.Instance.SetHighlightOn( hadAnySelected_StopToShoot );
                    FindAndAssignHotkeyTextSubObjectIfNeeded( btnStopAndShoot.Instance );
                    SetHotkeyText( btnStopAndShoot.Instance, InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( btnStopAndShoot.KeyComboEnable ) );
                }
                if ( btnNewAttackMove.Instance != null )
                {
                    btnNewAttackMove.Instance.SetHighlightOn( hadAnySelected_AttackMove );
                    FindAndAssignHotkeyTextSubObjectIfNeeded( btnNewAttackMove.Instance );
                    SetHotkeyText(btnNewAttackMove.Instance, InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( btnNewAttackMove.KeyComboEnable ) );
                }
                if (btnFRD.Instance != null)
                {
                    btnFRD.Instance.SetHighlightOn( hadAnySelected_Pursuit );
                    FindAndAssignHotkeyTextSubObjectIfNeeded( btnFRD.Instance );
                    SetHotkeyText(btnFRD.Instance, InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( btnFRD.KeyComboEnable ) );

                }
                if (btnHoldFire.Instance != null)
                {
                    btnHoldFire.Instance.SetHighlightOn( hadAnySelected_HoldFire );
                    FindAndAssignHotkeyTextSubObjectIfNeeded( btnHoldFire.Instance );
                    SetHotkeyText(btnHoldFire.Instance, InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( btnHoldFire.KeyComboDisable ) );
                }
                if ( btnScrap.Instance != null )
                {
                    FindAndAssignHotkeyTextSubObjectIfNeeded( btnScrap.Instance );
                    SetHotkeyText( btnScrap.Instance, InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( btnScrap.KeyCombo ) );
                }

                #region Positioning Logic
                {
                    btnSelectedShipEntryPool.ApplyItemsInRows( 0, ref currentY, TEXT_ROW_HEIGHTS + 1f, 240f, TEXT_ROW_HEIGHTS );
                }
                #endregion
            }
            #endregion
        }

        private static void FindAndAssignHotkeyTextSubObjectIfNeeded( StandingOrderBaseButtonBase button )
        {
            if ( button.HotkeyText == null )
            {
                button.HotkeyText =
                    button.Element.transform.Find( "HotkeyText" )?.GetComponent<TextMeshProUGUI>();
            }
        }

        private static void SetHotkeyText( StandingOrderBaseButtonBase button, string text )
        {
            if ( button.HotkeyText != null)
            {
                // TODO Make this more performance-friendly
                // There's only room for one letter, so don't display shortcut key if it's too long
                button.HotkeyText.SetText( text.Length == 1 ? text : string.Empty );
            }
        }

        public static Color color_GroupMove = ColorMath.HexToColor( "89ebfd" ); //cyan
        public static Color color_StopToShoot = ColorMath.HexToColor( "ffc770" ); //orange
        public static Color color_Pursuit = ColorMath.HexToColor( "fe6698" ); //pink-red
        public static Color color_AttackMove = ColorMath.HexToColor( "ff6d46" ); //reddish
        public static Color color_HoldFire = ColorMath.HexToColor( "ff6d46" ); //reddish
        public static Color color_White = Color.white;

        private static bool hadAnySelected_GroupMove = false;
        private static bool hadAnySelected_StopToShoot = false;
        private static bool hadAnySelected_Pursuit = false;
        private static bool hadAnySelected_AttackMove = false;
        private static bool hadAnySelected_HoldFire = false;

        private static DelReturn SelectedEntityCounter( GameEntity_Squad entity )
        {
            if ( entity == null || entity.HasBeenRemovedFromSim || entity.TypeData == null || entity.ToBeRemovedAtEndOfThisFrame )
                return DelReturn.Continue;
            FleetMembership fleetMem = entity.FleetMembership;
            if ( fleetMem == null )
                return DelReturn.Continue;
            Fleet fleet = fleetMem.Fleet;
            if ( fleet == null )
                return DelReturn.Continue;

            if ( entity.GetFactionOrNull_Safe().GetIsLocalFaction() )
            {
                if ( entity.TypeData.IsModular || entity.TypeData.IsFleetLeader )
                {
                    // Always show all fleet leaders and modular ships by ship line.
                    FleetMembership fMem = entity.FleetMembership;
                    if ( fMem != null )
                    {
                        if ( entity.SecondsSpentAsRemains < 0 )
                            entity.TypeData.NonSim_SelectionWindow_FleetMembership_Normal.AddIfNotAlreadyIn( fMem );
                        else
                            entity.TypeData.NonSim_SelectionWindow_FleetMembership_Remains.AddIfNotAlreadyIn( fMem );
                    }
                }
                else if ( !fleet.IsConsideredSelected_NonSim )
                {
                    //only show the individual ship if the fleet itself isn't selected
                    if ( entity.SecondsSpentAsRemains < 0 )
                        entity.TypeData.NonSim_SelectionWindow_FleetNotSelected_MyFac_Normal.Add( entity );
                    else
                        entity.TypeData.NonSim_SelectionWindow_FleetNotSelected_MyFac_Remains.Add( entity );
                }
                else
                {
                    if ( entity.SecondsSpentAsRemains < 0 )
                        entity.TypeData.NonSim_SelectionWindow_FleetIsSelected_MyFac_Normal.Add( entity );
                    else
                        entity.TypeData.NonSim_SelectionWindow_FleetIsSelected_MyFac_Remains.Add( entity );
                }

                if ( !entity.TypeData.IsDrone )
                {
                    if ( !hadAnySelected_GroupMove )
                    {
                        if ( entity.SpeedLimitFromGroupMove > 0 )
                            hadAnySelected_GroupMove = true;
                    }
                    if ( !hadAnySelected_StopToShoot )
                    {
                        if ( entity.StopToShootAnySeenTargets )
                            hadAnySelected_StopToShoot = true;
                    }
                    EntityOrderCollection orders = entity.Orders;
                    if ( orders != null )
                    {
                        if ( !hadAnySelected_Pursuit )
                        {
                            if ( orders.Behavior == EntityBehaviorType.Attacker_Full )
                                hadAnySelected_Pursuit = true;
                        }
                        if ( !hadAnySelected_AttackMove )
                        {
                            if ( orders.Behavior == EntityBehaviorType.Attacker_PursueOnlyInRange )
                                hadAnySelected_AttackMove = true;
                        }
                    }
                    if ( !hadAnySelected_HoldFire )
                    {
                        if ( entity.IsInHoldFireMode )
                            hadAnySelected_HoldFire = true;
                    }
                }
            }
            else
            {
                if ( entity.SecondsSpentAsRemains < 0 )
                    entity.TypeData.NonSim_SelectionWindow_OtherFac_Normal.Add( entity );
                else
                    entity.TypeData.NonSim_SelectionWindow_OtherFac_Remains.Add( entity );
            }
            return DelReturn.Continue;
        }

        public const float TEXT_ROW_HEIGHTS = 18f;

        #region btnSelectedShipEntry
        public class btnSelectedShipEntry : ButtonAbstractBase
        {
            public static btnSelectedShipEntry Original;
            public btnSelectedShipEntry() { if ( Original == null ) Original = this; }

            private GameEntityTypeData TypeDataForShipOrNull = null;
            private Faction FactionOwnerOrNull = null;
            private FleetMembership FleetMemIfByMembership = null;
            private bool IsRemains = false;
            private Fleet FleetOrNull = null;
            private string TextToDisplay;
            private int Count = 0;

            private ArcenUIImageArray relatedImages = null;
            private InputActionTypeData inputTypeData = null;
            public void Assign( GameEntityTypeData TypeData, Faction OwnerOrNull, FleetMembership FleetMemIfByMembership, bool Remains )
            {
                this.TypeDataForShipOrNull = TypeData;
                this.IsRemains = Remains;
                this.FleetMemIfByMembership = FleetMemIfByMembership;
                this.FactionOwnerOrNull = OwnerOrNull;

                InitIfPossible();
                if ( relatedImages != null )
                {
                    Color factionCenterColor = Color.white;
                    Color factionTrimColor = Color.black;;

                    if ( FactionOwnerOrNull != null &&
                        FactionOwnerOrNull.FactionCenterColor != null &&
                        FactionOwnerOrNull.FactionTrimColor != null )
                    {
                        factionCenterColor = FactionOwnerOrNull.FactionCenterColor.TeamColor;
                        factionTrimColor = FactionOwnerOrNull.FactionTrimColor.TeamColor;
                    }

                    if (TypeData != null && TypeData.OverrideFactionColor_Center != null)
                        factionCenterColor = TypeData.OverrideFactionColor_Center.TeamColor;
                    if (TypeData != null && TypeData.OverrideFactionColor_Trim != null)
                        factionTrimColor = TypeData.OverrideFactionColor_Trim.TeamColor;
                    
                    if ( TypeData?.GUISprite_Icon != null )
                        relatedImages.SetSpriteAndColor( 0, TypeData.GUISprite_Icon, factionCenterColor );

                    if ( TypeData?.GUISprite_IconBorder != null )
                        relatedImages.SetSpriteAndColor( 1, TypeData.GUISprite_IconBorder, factionTrimColor );

                    // maybe the two above should be clearing to BlankGUISprite too if null?
                    if ( TypeData?.GUISprite_IconOverlay != null )
                        relatedImages.SetSpriteAndColor( 2, TypeData.GUISprite_IconOverlay, Color.white );
                    else
                        relatedImages.SetSpriteAndColor( 2, ExternalConstants.Instance.BlankGUISprite, Color.white );
                }
            }

            public void Assign( Fleet Fleet )
            {
                this.FleetOrNull = Fleet;
                this.FleetMemIfByMembership = null;

                InitIfPossible();
                if ( relatedImages != null )
                {
                    relatedImages.SetSpriteAndColor( 0, ExternalConstants.Instance.BlankGUISprite, Color.white );
                    relatedImages.SetSpriteAndColor( 1, ExternalConstants.Instance.BlankGUISprite, Color.white );

                    int controlGroup = Fleet.GetTiedToKeybindIndexForDisplay();
                    if ( controlGroup >= 0 && controlGroup < ExternalConstants.Instance.UISprite_ControlGroupNumbers.Length )
                        relatedImages.SetSpriteAndColor( 2, ExternalConstants.Instance.UISprite_ControlGroupNumbers[controlGroup], Color.white );
                    else
                        relatedImages.SetSpriteAndColor( 2, ExternalConstants.Instance.BlankGUISprite, Color.white );
                }
            }

            public void Assign( string text )
            {
                this.TextToDisplay = text;
                this.TypeDataForShipOrNull = GameEntityTypeDataTable.Instance.Rows[0];
                this.FleetMemIfByMembership = null;

                InitIfPossible();
                if ( relatedImages != null )
                    relatedImages.SetAllToBlank();
            }

            private void InitIfPossible()
            {
                if ( relatedImages == null && this.Element != null )
                {
                    relatedImages = new ArcenUIImageArray( ExternalConstants.Instance.BlankGUISprite );
                    relatedImages.InitializeFrom_ArcenUI_Button( this.Element );
                }
                bool showSelectionShortcut = InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key2();
                int debugCode = 0;
                inputTypeData = null;
                if ( showSelectionShortcut )
                {
                    try{
                        debugCode = 100;
                        GameEntityTypeData typeData = null;
                        if ( (FleetOrNull != null && FleetOrNull.Centerpiece.GetSquad() != null ) )
                            typeData = FleetOrNull.Centerpiece.GetSquad().TypeData;
                        debugCode = 200;
                        if ( TypeDataForShipOrNull != null )
                            typeData = TypeDataForShipOrNull;
                        debugCode = 300;
                        if ( typeData != null )
                        {
                            debugCode = 400;
                            if ( typeData.SpecialType == SpecialEntityType.MobileOfficerCombatFleetFlagship ||
                                 typeData.SpecialType == SpecialEntityType.MobileStrikeCombatFleetFlagship ) 
                                inputTypeData = InputActionTypeDataTable.GetActionByName_FairlySlow( "SelectMobileFlagships" );

                            if ( typeData.SpecialType ==  SpecialEntityType.BattlestationBasic ||
                                 typeData.SpecialType == SpecialEntityType.BattlestationCitadel )
                                inputTypeData = InputActionTypeDataTable.GetActionByName_FairlySlow( "SelectBattlestationsAndCitadels" );
                            debugCode = 500;
                            if ( typeData.HasAnyInternalCloakingAbility )
                                inputTypeData = InputActionTypeDataTable.GetActionByName_FairlySlow( "SelectCloakingUnits" );
                            debugCode = 600;
                            if ( typeData.MeleeRangeIsPossibleEver )
                                inputTypeData = InputActionTypeDataTable.GetActionByName_FairlySlow( "SelectMelee" );
                            debugCode = 700;
                            if ( typeData.SniperRangeIsPossibleEver )
                                inputTypeData = InputActionTypeDataTable.GetActionByName_FairlySlow( "SelectSnipers" );
                            debugCode = 800;
                            if ( typeData.IsTractorSource )
                                inputTypeData = InputActionTypeDataTable.GetActionByName_FairlySlow( "SelectTractors" );
                            debugCode = 900;
                            if ( typeData.IsEngineer )
                                inputTypeData = InputActionTypeDataTable.GetActionByName_FairlySlow( "SelectEngineers" );

                            debugCode = 1000;
                            if ( inputTypeData != null && inputTypeData.KeyCode == ArcenInputCode.None &&
                                 inputTypeData.Modifier1KeyCode == ArcenInputCode.None )
                                inputTypeData = null;
                        }
                    }catch(Exception e)
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in InitIfPossible debugCode " + debugCode + " " + e.ToString() , Verbosity.DoNotShow );
                    }
                }
            }

            public override bool GetShouldBeHidden()
            {
                return TypeDataForShipOrNull == null && FleetOrNull == null && String.IsNullOrEmpty( this.TextToDisplay );
            }

            public override void Clear()
            {
                this.TypeDataForShipOrNull = null;
                this.FleetOrNull = null;
                this.TextToDisplay = string.Empty;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                bool showSelectionShortcut = InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key2();
                if ( !String.IsNullOrEmpty( this.TextToDisplay ) )
                {
                    Buffer.Add(this.TextToDisplay);
                }
                else if ( this.FleetMemIfByMembership != null )
                {
                    Buffer.Add( this.FleetMemIfByMembership.GetDisplayNameForSidebar() );
                    if ( inputTypeData != null && showSelectionShortcut )
                    {
                        Buffer.StartColor( ColorMath.Orange ).Add( "     " ).Add( "选择: " ).Add( "(" );
                        if ( inputTypeData.KeyCode != ArcenInputCode.None )
                            Buffer.Add( InputActionTypeDataTable.GetHumanReadableStringForKeyCode( inputTypeData.KeyCode ) );
                        if ( inputTypeData.Modifier1KeyCode != ArcenInputCode.None )
                            Buffer.Add( " + " ).Add( InputActionTypeDataTable.GetHumanReadableStringForKeyCode( inputTypeData.Modifier1KeyCode ) );
                        Buffer.Add( ")" ).EndColor();
                    }
                    else
                    {
                        if ( this.IsRemains )
                            Buffer.Add( " 残骸" );
                    }

                    int strength = 0;
                    int healthCurrent = 0;
                    int healthMax = 0;
                    int count = 0;
                    //here's a modular ship
                    foreach ( GameEntity_Squad ship in this.FleetMemIfByMembership.Entities )
                    {
                        if ( this.IsRemains )
                        {
                            if ( ship.SecondsSpentAsRemains <= 0 )
                                continue; //skip non-remains
                        }
                        else
                        {
                            if ( ship.SecondsSpentAsRemains > 0 )
                                continue; //skip remains
                        }
                        healthCurrent += ship.GetCurrentHullPoints();
                        healthMax += ship.GetMaxHullPoints();
                        strength += ship.GetStrengthOfSelfAndContentsButNotFleetCentricBits();
                        count++;
                    }

                    int healthPercent = 0;
                    if ( healthMax <= 0 )
                        healthPercent = 0;
                    else
                        healthPercent = Mathf.RoundToInt( ((float)healthCurrent / (float)healthMax) * 100f );
                    if ( healthPercent < 0 ) healthPercent = 0;
                    else if ( healthPercent > 100 ) healthPercent = 100;

                    string healthColor = "73ffe0"; //green
                    if ( healthPercent < 34 || this.IsRemains )
                        healthColor = "fa6697"; //red
                    else if ( healthPercent < 67 )
                        healthColor = "ffd179"; //orange

                    this.Count = count;
                    Buffer.Add( "  x" ).Add( count ).Add( " " ).StartColor( healthColor ).Add( healthPercent ).Add( "%" );
                }
                else if ( TypeDataForShipOrNull != null )
                {
                    Buffer.Add( TypeDataForShipOrNull.DisplayNameForSidebar );
                    if ( inputTypeData != null && showSelectionShortcut )
                    {
                        Buffer.StartColor( ColorMath.Orange ).Add("     ").Add("选择: ").Add("(");
                        if ( inputTypeData.KeyCode != ArcenInputCode.None )
                            Buffer.Add(InputActionTypeDataTable.GetHumanReadableStringForKeyCode(inputTypeData.KeyCode ) );
                        if ( inputTypeData.Modifier1KeyCode != ArcenInputCode.None )
                            Buffer.Add(" + ").Add(InputActionTypeDataTable.GetHumanReadableStringForKeyCode(inputTypeData.Modifier1KeyCode ) );
                        Buffer.Add(")").EndColor();
                    }
                    else
                    {
                        if ( this.IsRemains )
                            Buffer.Add( " 残骸" );

                        int strength, healthPercent;
                        int count = this.TypeDataForShipOrNull.GetCountAndStrengthForSelectedSidebar( this.FactionOwnerOrNull, out strength, out healthPercent, this.IsRemains );
                        this.Count = count;
                        string healthColor = "73ffe0"; //green
                        if ( healthPercent < 34 || this.IsRemains )
                            healthColor = "fa6697"; //red
                        else if ( healthPercent < 67 )
                            healthColor = "ffd179"; //orange

                        Buffer.Add( "  x" ).Add( count ).Add( " " ).StartColor( healthColor ).Add( healthPercent ).Add( "%" );
                    }
                }
                else if ( FleetOrNull != null )
                {
                    if ( FleetOrNull.GetName().Length > 20 )
                        Buffer.Add("<size=75%>");
                    else if ( FleetOrNull.GetName().Length > 10 )
                        Buffer.Add("<size=85%>");

                    Buffer.Add( FleetOrNull.GetName() );
                    if ( FleetOrNull.GetName().Length > 10 )
                        Buffer.Add("</size>");
                    if ( inputTypeData != null && showSelectionShortcut )
                    {
                        Buffer.StartColor( ColorMath.Orange ).Add("    ").Add("选择: ").Add("(");
                        if ( inputTypeData.KeyCode != ArcenInputCode.None )
                            Buffer.Add(InputActionTypeDataTable.GetHumanReadableStringForKeyCode(inputTypeData.KeyCode ) );
                        if ( inputTypeData.Modifier1KeyCode != ArcenInputCode.None )
                            Buffer.Add(" ").Add(InputActionTypeDataTable.GetHumanReadableStringForKeyCode(inputTypeData.Modifier1KeyCode ) );
                        Buffer.Add(")").EndColor();
                    }
                    else
                    {
                        int strength = this.FleetOrNull.GetMaxStrengthOfFleet_ForUIOnly( false );
                        int fullCap = this.FleetOrNull.GetMaxCountOfFleet_ForUIOnly( false );
                        int builtCount = this.FleetOrNull.GetCurrentCountOfFleet_ForUIOnly();
                        long maxHealth = this.FleetOrNull.GetMaxHullOfFleet_ForUIOnly( false );
                        long currentHealth = this.FleetOrNull.GetCurrentHullOfFleet_ForUIOnly();

                        if ( maxHealth < 0 )
                            maxHealth = 1;

                        int healthPercent = Mathf.CeilToInt( ( (float)currentHealth / (float)maxHealth ) * 100f );
                        if ( healthPercent > 100 )
                            healthPercent = 100; //I've seen it at 101 sometimes, oddly enough
                        string healthColor = "73ffe0"; //green
                        if ( healthPercent < 34 )
                            healthColor = "fa6697"; //red
                        else if ( healthPercent < 67 )
                            healthColor = "ffd179"; //orange

                        if ( builtCount >= fullCap )
                            Buffer.StartColor( QuickColors.NewValue );
                        else
                            Buffer.StartColor( ColorMath.Orange );

                        Buffer.Add( "  " ).Add( builtCount ).Add( " / " ).Add( fullCap ).Add( "   " );
                        Buffer.EndColor();
                        Buffer.StartColor( healthColor ).Add( healthPercent ).Add( "%" );
                    }
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( InputCaching.inputRemoveFromSelection.CalculateIsKeyDownNow_IgnoreConflicts() ) //deselect the clicked thing
                {
                    if ( this.TypeDataForShipOrNull != null )
                    {
                        {
                            var e = Engine_AIW2.Instance.SelectedSquadsEvenIncludingOnesICannotGiveOrdersTo.GetEnumerator();
                            while ( e.MoveNext() )
                            {
                                DelReturn r = DeselectIfAMatch( e.Current );
                                if ( r == DelReturn.RemoveAndContinue ) { e.RemoveCurrent(); continue; }
                                if ( r == DelReturn.Break ) break;
                            }
                        }
                        return MouseHandlingResult.None;
                    }
                    else if ( this.FleetOrNull != null )
                    {
                        this.FleetOrNull.IsConsideredSelected_NonSim = false;
                        return MouseHandlingResult.None;
                    }
                }
                else //select only the clicked thing
                {
                    if ( this.TypeDataForShipOrNull != null )
                    {
                        for ( int i = 0; i < World_AIW2.Instance.PlayerFleets.Count; i++ )
                        {
                            Fleet data = World_AIW2.Instance.PlayerFleets[i];
                            if ( data.IsConsideredSelected_NonSim )
                                data.IsConsideredSelected_NonSim = false;
                        }
                        {
                            var e = Engine_AIW2.Instance.SelectedSquadsEvenIncludingOnesICannotGiveOrdersTo.GetEnumerator();
                            while ( e.MoveNext() )
                            {
                                DelReturn r = DeselectIfNotAMatch( e.Current );
                                if ( r == DelReturn.RemoveAndContinue ) { e.RemoveCurrent(); continue; }
                                if ( r == DelReturn.Break ) break;
                            }
                        }
                        return MouseHandlingResult.None;
                    }
                    else if ( this.FleetOrNull != null )
                    {
                        for ( int i = 0; i < World_AIW2.Instance.PlayerFleets.Count; i++ )
                        {
                            Fleet data = World_AIW2.Instance.PlayerFleets[i];
                            if ( data == this.FleetOrNull )
                                continue;
                            if ( data.IsConsideredSelected_NonSim )
                                data.IsConsideredSelected_NonSim = false;
                        }
                        {
                            var e = Engine_AIW2.Instance.SelectedSquadsEvenIncludingOnesICannotGiveOrdersTo.GetEnumerator();
                            while ( e.MoveNext() )
                            {
                                DelReturn r = DeselectForSure( e.Current );
                                if ( r == DelReturn.RemoveAndContinue ) { e.RemoveCurrent(); continue; }
                                if ( r == DelReturn.Break ) break;
                            }
                        }
                        return MouseHandlingResult.None;
                    }
                }

                return MouseHandlingResult.PlayClickDeniedSound;
            }

            private DelReturn DeselectIfAMatch( GameEntity_Squad entity )
            {
                //NOTE!!  IF you try to call Unselect() or ForceUnselect() during a DFSelected, it won't get everything!
                //        Instead, you should just return RemoveAndContinue for anything you want to deselect, and that will work fine.
                if ( entity == null || entity.HasBeenRemovedFromSim || entity.TypeData == null || entity.ToBeRemovedAtEndOfThisFrame )
                {
                    if ( entity != null )
                        entity.ReasonIterationRemoved = "DeadDuringDeselectIfAMatch";
                    return DelReturn.RemoveAndContinue;
                }
                if ( this.FleetMemIfByMembership != null ) {
                    if ( entity.FleetMembership == this.FleetMemIfByMembership ) {
                        entity.ReasonIterationRemoved = "DeselectIfAMatch";
                        return DelReturn.RemoveAndContinue;
                    } else {
                        return DelReturn.Continue;
                    }
                } else {
                    if ( entity.TypeData == this.TypeDataForShipOrNull )
                    {
                        entity.ReasonIterationRemoved = "DeselectIfAMatch";
                        return DelReturn.RemoveAndContinue;
                    }
                    return DelReturn.Continue;
                }
            }

            private DelReturn DeselectIfNotAMatch( GameEntity_Squad entity )
            {
                //NOTE!!  IF you try to call Unselect() or ForceUnselect() during a DFSelected, it won't get everything!
                //        Instead, you should just return RemoveAndContinue for anything you want to deselect, and that will work fine.
                if ( entity == null || entity.HasBeenRemovedFromSim || entity.TypeData == null || entity.ToBeRemovedAtEndOfThisFrame )
                {
                    if ( entity != null )
                        entity.ReasonIterationRemoved = "DeadDuringDeselectIfNotAMatch";
                    return DelReturn.RemoveAndContinue;
                }
                if ( this.FleetMemIfByMembership != null ) {
                    if ( entity.FleetMembership != this.FleetMemIfByMembership ) {
                        entity.ReasonIterationRemoved = "DeselectIfNotAMatch";
                        return DelReturn.RemoveAndContinue;
                    } else {
                        return DelReturn.Continue;
                    }
                } else {
                    if ( entity.TypeData != this.TypeDataForShipOrNull )
                    {
                        entity.ReasonIterationRemoved = "DeselectIfNotAMatch";
                        return DelReturn.RemoveAndContinue;
                    }
                    return DelReturn.Continue;
                }
            }

            private DelReturn DeselectForSure( GameEntity_Squad entity )
            {
                if ( entity != null )
                    entity.ReasonIterationRemoved = "DeselectForSure";
                //NOTE!!  IF you try to call Unselect() or ForceUnselect() during a DFSelected, it won't get everything!
                //        Instead, you should just return RemoveAndContinue for anything you want to deselect, and that will work fine.
                return DelReturn.RemoveAndContinue;
            }

            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_InGameSelectionInfo-tooltipBuffer" );
            public override void HandleMouseover()
            {
                GameEntity_Squad squadForTooltip = null;
                FromSidebarType sidebarType = FromSidebarType.None;
                
                // todo: get rid of all of this, just iterate the selected ships
                if ( this.FleetMemIfByMembership != null ) 
                {
                    foreach ( GameEntity_Squad ship in this.FleetMemIfByMembership.Entities )
                    {
                        if ( this.IsRemains && ship.SecondsSpentAsRemains == 0 ) {
                            continue; //skip non-remains
                        } else if ( !this.IsRemains && ship.SecondsSpentAsRemains > 0 ) {
                            continue; //skip remains
                        }
                        squadForTooltip = ship;
                        break;
                    }
                        
                    if ( this.FleetMemIfByMembership.Fleet.IsConsideredSelected_NonSim )
                        sidebarType = FromSidebarType.SelectionWindow_FromFleet;
                    else
                        sidebarType = FromSidebarType.SelectionWindow_MultipleUnits;
                } 
                else 
                if ( this.TypeDataForShipOrNull != null )
                {
                    Faction localFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                    if ( localFactionOrNull == null )
                        localFactionOrNull = World_AIW2.Instance.GetFirstPlayerFactionOrNull(); //spectator mode

                    if ( localFactionOrNull != this.FactionOwnerOrNull )
                    {
                        //if for another faction, not the current player
                        if ( this.IsRemains )
                        {
                            List<SafeSquadWrapper> squadList = this.TypeDataForShipOrNull.NonSim_SelectionWindow_OtherFac_Remains;
                            for ( int i = 0; i < squadList.Count; i++ )
                            {
                                GameEntity_Squad squad = squadList[i].GetSquad();
                                if ( squad == null || squad.GetFactionOrNull_Safe() != this.FactionOwnerOrNull )
                                    continue;

                                if ( squadForTooltip == null )
                                {
                                    squadForTooltip = squad;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            List<SafeSquadWrapper> squadList = this.TypeDataForShipOrNull.NonSim_SelectionWindow_OtherFac_Normal;
                            for ( int i = 0; i < squadList.Count; i++ )
                            {
                                GameEntity_Squad squad = squadList[i].GetSquad();
                                if ( squad == null || squad.GetFactionOrNull_Safe() != this.FactionOwnerOrNull )
                                    continue;
                                if ( squadForTooltip == null )
                                {
                                    squadForTooltip = squad;
                                    break;
                                }
                            }
                        }
                    }
                    else //this is for my faction, actually
                    {
                        if ( this.IsRemains )
                        {
                            int remainsCount = this.TypeDataForShipOrNull.NonSim_SelectionWindow_FleetNotSelected_MyFac_Remains.Count;
                            remainsCount += this.TypeDataForShipOrNull.NonSim_SelectionWindow_FleetIsSelected_MyFac_Remains.Count;

                            List<SafeSquadWrapper> squadList = this.TypeDataForShipOrNull.NonSim_SelectionWindow_FleetNotSelected_MyFac_Remains;
                            if ( squadList.Count <= 0 )
                                squadList = this.TypeDataForShipOrNull.NonSim_SelectionWindow_FleetIsSelected_MyFac_Remains;

                            if (squadList.Count > 0)
                                squadForTooltip = squadList[0].GetSquad();
                            
                            if ( remainsCount == 1 )
                                sidebarType = FromSidebarType.SelectionWindow_SingleUnit;
                            else if ( remainsCount > 1 )
                                sidebarType = FromSidebarType.SelectionWindow_MultipleUnits;
                        }
                        else
                        {
                            int normalCount = this.TypeDataForShipOrNull.NonSim_SelectionWindow_FleetNotSelected_MyFac_Normal.Count;
                            normalCount += this.TypeDataForShipOrNull.NonSim_SelectionWindow_FleetIsSelected_MyFac_Normal.Count;

                            List<SafeSquadWrapper> squadList = this.TypeDataForShipOrNull.NonSim_SelectionWindow_FleetNotSelected_MyFac_Normal;
                            if ( squadList.Count <= 0 )
                                squadList = this.TypeDataForShipOrNull.NonSim_SelectionWindow_FleetIsSelected_MyFac_Normal;

                            if (squadList.Count > 0)
                                squadForTooltip = squadList[0].GetSquad();
                            
                            if ( normalCount == 1 )
                                sidebarType = FromSidebarType.SelectionWindow_SingleUnit;
                            else if ( normalCount > 1 )
                                sidebarType = FromSidebarType.SelectionWindow_MultipleUnits;
                        }
                    }
                }
                
                if (squadForTooltip != null)
                {
                    if (sidebarType != FromSidebarType.SelectionWindow_FromFleet)
                    {
                        sidebarType = squadForTooltip.ShipCount > 1 ? FromSidebarType.SelectionWindow_MultipleUnits : FromSidebarType.SelectionWindow_SingleUnit;
                    }
                    
                    var moreContentString = EntityText.GetHasContentsToView( squadForTooltip );
                    GameEntity_Base.SetCurrentlyHoveredOver( squadForTooltip, sidebarType );
                    
                    EntityText.GetTooltip( tooltipBuffer, squadForTooltip, null,
                        null, this.Count, null, 0, sidebarType, ShipExtraDetailFlags.None, 1f, false );
                    
                    EntityText.Write_Tooltip_Hotkeys_Footer( tooltipBuffer, true, true, moreContentString );
                    //Engine_Universal.DebugText = "text " + DateTime.Now + " " + tooltipBuffer.GetStringWithoutResettingForNextUpdate();
                    Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
                }
                else if ( this.FleetOrNull != null )
                {
                    Window_InGameSidebarFleets.btnFleet.WriteFleetTooltip( this.FleetOrNull, tooltipBuffer );
                    tooltipBuffer.Add( FontSizes.MUCH_SMALLER_SIZE_PLUS_A_TAD_STRING ).Add( "\n点击选择此舰队。按住 <color=#d18444>" )
                        .Add( InputCaching.inputRemoveFromSelection.GetHumanReadableKeyCombo() ).Add( "</color> 并点击以将此舰队从选择中移除。</color></size>" );
                    Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
                }
                else if ( !String.IsNullOrEmpty( this.TextToDisplay ) )
                {
                    Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( this.TextToDisplay, "ShipTooltipScale" );
                }
                else
                {
                    Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "空选择项！", "ShipTooltipScale" );
                }
            }
        }
        #endregion

        public class StandingOrderBaseButtonBase : HighlightSettableButtonBase
        {
            // TODO Use proper Arcen wrapper for this
            public TextMeshProUGUI HotkeyText;
        }

        #region btnStopAndShoot
        public class btnStopAndShoot : StandingOrderBaseButtonBase
        {
            public const string KeyComboEnable = "ToggleStopToShoot";
            public static btnStopAndShoot Instance;
            public btnStopAndShoot() { if ( Instance == null ) Instance = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                EndpointFunctions.ToggleStopToShootAnySeenTargets( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "启用停止射击：你的飞船在前往目的地途中会向经过的敌人开火。但启用此模式后，它们会真正停下来与遇到的任何敌人战斗。\n你也可以按 " +
                    InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( KeyComboEnable ) + " 开启停止射击模式。",
                    "GeneralTooltipScale" );
            }
        }
        #endregion

        #region btnGroupMove
        public class btnGroupMove : StandingOrderBaseButtonBase
        {
            public const string KeyComboAdd = "ToggleGroupMove";
            public static btnGroupMove Instance;
            public btnGroupMove() { if ( Instance == null ) Instance = this; }


            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                EndpointFunctions.ToggleSpeedGroupForPlayerShipsONLY( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "启用编队移动：在编队移动模式下，一组飞船会相互保持速度同步，仅以最慢飞船的速度移动。\n你也可以按 " +
                    InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( KeyComboAdd ) + " 切换编队移动。",
                    "GeneralTooltipScale" );
            }
        }
        #endregion

        #region btnFRD
        public class btnFRD : StandingOrderBaseButtonBase
        {
            public const string KeyComboEnable = "TogglePursuitMode";
            public static btnFRD Instance;
            public btnFRD() { if ( Instance == null ) Instance = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                EndpointFunctions.TogglePursuitMode_FromPlayer( GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "启用追击模式：在追击模式下，你的飞船会自主追击敌人、进行风筝操作等。默认情况下，你的飞船只会移动到你指定的位置，但可以自由开火。\n你也可以按 " +
                    InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( KeyComboEnable ) + " 切换追击模式。",
                    "GeneralTooltipScale" );
            }
        }
        #endregion

        #region btnNewAttackMove
        public class btnNewAttackMove : StandingOrderBaseButtonBase
        {
            public const string KeyComboEnable = "ToggleAttackMove";
            public static btnNewAttackMove Instance;
            public btnNewAttackMove() { if ( Instance == null ) Instance = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                EndpointFunctions.ToggleAttackMove_FromPlayer( GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "启用攻击移动模式：在攻击移动模式下，你的飞船会自主追击敌人、进行风筝操作等。这与追击模式类似，但有一个关键区别——你的飞船不会追击超出射程的敌人。在敌方领地使用更安全，而追击模式则适合在你的星球上使用。\n你也可以按 " +
                    InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( KeyComboEnable ) + " 切换攻击移动模式。",
                    "GeneralTooltipScale" );
            }
        }
        #endregion

        #region btnHoldFire
        public class btnHoldFire : StandingOrderBaseButtonBase
        {
            public const string KeyComboDisable = "ToggleShipsEnabled";
            public static btnHoldFire Instance;
            public btnHoldFire() { if (Instance == null) Instance = this; }

            public override MouseHandlingResult HandleClick_Subclass(MouseHandlingInput input)
            {
                EndpointFunctions.ToggleShipsEnabled( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText("关闭选中飞船上除引擎以外的所有功能（如果有的话）。禁用后，军用飞船将不会攻击。这对于需要保持隐身的隐形飞船非常有用。禁用后，工程师将不会消耗金属进行工作，工厂将停止运行，在建工程将不会继续等。你也可以按 " +
                    InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( KeyComboDisable ) + " 在启用和禁用飞船之间切换。",
                    "GeneralTooltipScale" );
            }
        }
        #endregion

        #region btnScrap
        public class btnScrap : StandingOrderBaseButtonBase
        {
            public const string KeyCombo = "ScrapUnits";
            public static btnScrap Instance;
            public btnScrap() { if ( Instance == null ) Instance = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                EndpointFunctions.ScrapSelectedUnits( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( $"废弃你当前选中的所有单位？(快捷键: <color=yellow>{InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( KeyCombo )}</color>)",
                    "GeneralTooltipScale" );
            }
        }
        #endregion

        #region txtHeader
        public class txtHeader : TextAbstractBase
        {
            public static txtHeader Instance;
            public txtHeader() { if ( Instance == null ) Instance = this; }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                try
                {
                    if ( Engine_AIW2.Instance.GetHasSelectionEvenIncludingOnesICannotGiveOrdersTo() )
                    {
                        Buffer.Add( "已选飞船和/或舰队: " );
                    }
                }
                catch
                {} //just a transient null from threads competing, I'm sure
            }
            public override void HandleMouseover()
            {
            }
        }
        #endregion        
    }

    public static class GameEntityTypeDataExtends
    {
        #region GetCountAndStrengthForSelectedSidebar
        public static int GetCountAndStrengthForSelectedSidebar( this GameEntityTypeData TypeData, Faction Faction, out int Strength, out int HealthPercent, bool IsForRemains )
        {
            int count = 0;
            Strength = 0;
            int healthCurrent = 0;
            int healthMax = 0;
            GameEntity_Squad squad = null;

            Faction localFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFactionOrNull == null )
                localFactionOrNull = World_AIW2.Instance.GetFirstPlayerFactionOrNull(); //spectator mode

            if ( Faction == localFactionOrNull )
            {
                //it's me, ur local faction
                for ( int outer = 0; outer <= 1; outer++ )
                {
                    List<SafeSquadWrapper> entities = IsForRemains ?
                        (outer == 0 ? TypeData.NonSim_SelectionWindow_FleetNotSelected_MyFac_Remains : TypeData.NonSim_SelectionWindow_FleetIsSelected_MyFac_Remains) :
                        (outer == 0 ? TypeData.NonSim_SelectionWindow_FleetNotSelected_MyFac_Normal : TypeData.NonSim_SelectionWindow_FleetIsSelected_MyFac_Normal);
                    for ( int i = 0; i < entities.Count; i++ )
                    {
                        squad = entities[i].GetSquad();
                        if ( squad == null || squad.HasBeenRemovedFromSim )
                            continue;
                        count++;
                        Strength += squad.GetStrengthPerSquad();
                        healthCurrent += squad.GetCurrentHullPoints();
                        healthMax += squad.GetMaxHullPoints();

                        if ( squad.ExtraStackedSquadsInThis > 0 )
                        {
                            count += squad.ExtraStackedSquadsInThis;
                            int addedHealth = squad.ExtraStackedSquadsInThis * squad.GetMaxHullPoints();
                            healthCurrent += addedHealth;
                            healthMax += addedHealth;
                            Strength += (squad.ExtraStackedSquadsInThis * squad.GetStrengthPerSquad());
                        }
                    }
                }
            }
            else
            {
                //this is for some other faction we have selected
                List<SafeSquadWrapper> entities = IsForRemains ? TypeData.NonSim_SelectionWindow_OtherFac_Remains :
                        TypeData.NonSim_SelectionWindow_OtherFac_Normal;

                for ( int i = 0; i < entities.Count; i++ )
                {
                    squad = entities[i].GetSquad();
                    if ( squad == null || squad.HasBeenRemovedFromSim )
                        continue;
                    if ( squad.GetFactionOrNull_Safe() != Faction )
                        continue; //if from the wrong faction, ignore it

                    count++;
                    Strength += squad.GetStrengthPerSquad();
                    healthCurrent += squad.GetCurrentHullPoints();
                    healthMax += squad.GetMaxHullPoints();

                    if ( squad.ExtraStackedSquadsInThis > 0 )
                    {
                        count += squad.ExtraStackedSquadsInThis;
                        int addedHealth = squad.ExtraStackedSquadsInThis * squad.GetMaxHullPoints();
                        healthCurrent += addedHealth;
                        healthMax += addedHealth;
                        Strength += (squad.ExtraStackedSquadsInThis * squad.GetStrengthPerSquad());
                    }
                }
            }

            if ( healthMax <= 0 )
                HealthPercent = 0;
            else
                HealthPercent = Mathf.RoundToInt( ( (float)healthCurrent / (float)healthMax ) * 100f );
            if ( HealthPercent < 0 ) HealthPercent = 0;
            else if ( HealthPercent > 100 ) HealthPercent = 100;

            return count;
        }
        #endregion
    }
}
