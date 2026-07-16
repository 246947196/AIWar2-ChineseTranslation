using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_ModalSwapFleetMembers : Window_DynamicallyFilledAbstractBase
    {
        public static Window_ModalSwapFleetMembers Instance;
        
        private Fleet _fleet;
        private FleetMembership _member;
        private bool _open;
        
        public Window_ModalSwapFleetMembers()
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
            //LOG.Msg("{0}.Close() called from\n{1}", this.GetType().Name, LOG.StackTrace);
            _open = false;
        }
        
        public void Open( Fleet fleet, FleetMembership member )
        {
            //LOG.Msg("{0}.Open(fleet={1}, member={2}) called from\n{3}", this.GetType().Name, fleet.OrNull(), member.OrNull(), LOG.StackTrace);
            
            _open = true;
            _fleet = fleet;
            _member = member;
            membersToSwap.Clear();
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

        #endregion

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                bool isFleetLeader = Instance._member == null ? false : Instance._member.TypeData.IsFleetLeader;

                if ( isFleetLeader )
                    Buffer.Add( "<size=90%>Fleet: " ).Add( Instance._fleet.GetName(), "a1a1ff" ).Add( ", Swap All Members Other Than Flagship</size>");
                else
                    Buffer.Add( "<size=95%>Fleet: " ).Add( Instance._fleet.GetName(), "a1a1ff" ).Add( ", Swap Away: " ).Add(
                      Instance._member == null ? "空槽位" : Instance._member.TypeData.GetDisplayName(), "a1ffa1" ).Add( "</size>" );
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

        private static ButtonAbstractBase.ButtonPool<btnTextWithIcon> btnTextWithIconPool = null;

        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_ModalSwapFleetMembers.Instance != null )
                {   
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( btnTextWithIcon.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnTextWithIconPool = new ButtonAbstractBase.ButtonPool<btnTextWithIcon>( btnTextWithIcon.Original, 5 );
                        }
                    }
                    #endregion
                }

                this.WindowController.myXPositionScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" );
                this.WindowController.myScale = GameSettings.Current.GetFloatBySetting( "NotificationsScale" );

                //if you change away from the fleets tab, close the fleet management popout
                if ( //(Window_InGameSidebarBase.Current != InGameSidebarType.Fleets &&
                    //Window_InGameSidebarBase.Current != InGameSidebarType.Ships) ||
                    Engine_Universal.RunStatus == RunStatus.GameStart )
                {
                    Instance.Close();
                    return;
                }
            }
        }

        private static readonly List<FleetMembership> membersICanSwapTo = List<FleetMembership>.Create_WillNeverBeGCed( 3000, "Window_ModalSwapFleetMembers-membersICanSwapTo" );
        private static readonly List<Fleet> fleetsWithEmptySlotICanSwapTo = List<Fleet>.Create_WillNeverBeGCed( 3000, "Window_ModalSwapFleetMembers-fleetsWithEmptySlotICanSwapTo" );

        public static readonly Dictionary<FleetMembership, bool> membersToSwap = Dictionary<FleetMembership, bool>.Create_WillNeverBeGCed( 3000, "Window_ModalSwapFleetMembers-membersToSwap" );

        private static List<Faction> swapFactions = List<Faction>.Create_WillNeverBeGCed( 300, "Window_ModalSwapFleetMembers-swapFactions" );

        public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            if ( bMainContentParent.ParentT == null )
                return;
            this.Window.SetOverridingTransformToWhichToAddChildren( bMainContentParent.ParentT );

            if ( btnTextWithIconPool == null )
                return;

            btnTextWithIconPool.Clear( 10 );

            #region Calculate membersICanSwapTo
            membersICanSwapTo.Clear();
            swapFactions.Clear();
            for ( int i = 0; i < World_AIW2.Instance.EmpireStylePlayerFactions.Count; i++ )
                swapFactions.Add( World_AIW2.Instance.EmpireStylePlayerFactions[i] );

            Faction localFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFactionOrNull != null )
            {
                bool isElite = _member == null ? false : _member.TypeData.IsElite;
                bool isFleetLeader = _member == null ? false : _member.TypeData.IsFleetLeader;
                bool canSwapForElites = true;
                if ( !isElite )
                {
                    if ( Instance._fleet.CalculateIsEliteSlotFilled() )
                        canSwapForElites = false;
                }
                GameEntityTypeData existingFleetLeader = Instance._fleet.Centerpiece.GetSquad()?.TypeData;

                for ( int i = 0; i < swapFactions.Count; i++ )
                {
                    foreach ( Fleet fleet in World_AIW2.Instance.Fleets( swapFactions[i], FleetStatus.CenterpieceMustLive ) )
                    {
                        //can't swap with this same fleet!
                        if ( fleet == _fleet )
                            continue;
                        if ( fleet.GetCanSwapFleetLinesWithOtherFleets() != ArcenRejectionReason.Unknown )
                            continue;

                        foreach ( FleetMembership memInner in fleet.MemberGroupsSorted_NonSim_ForUIOnly_SeriouslyDoNotCallFromBGCode() )
                        {
                            //no swapping drones
                            if ( memInner.TypeData.IsDrone )
                                continue;
                            //no swapping those immune to it
                            if ( memInner.TypeData.ImmuneToSwappingBetweenFleets )
                                continue;
                            //no swapping elites if we say not
                            if ( !canSwapForElites )
                            {
                                if ( memInner.TypeData.IsElite )
                                    continue;
                            }
                            //if we are swapping an elite, ONLY show other elites
                            if ( isElite )
                            {
                                if ( !memInner.TypeData.IsElite )
                                    continue;
                            }
                            //if we are swapping a fleet leader, ONLY show other fleet leaders
                            if ( isFleetLeader )
                            {
                                if ( !memInner.TypeData.IsFleetLeader )
                                    continue;
                                if ( existingFleetLeader == null )
                                    continue;
                                //must be same category
                                //if ( existingFleetLeader.SpecialType != memInner.TypeData.SpecialType )
                                //    continue;
                                //not same exact type
                                if ( existingFleetLeader == memInner.TypeData )
                                    continue;

                            }
                            else
                            {
                                //otherwise, don't show fleet leaders!
                                if ( memInner.TypeData.IsFleetLeader )
                                    continue;
                            }
                            if ( memInner.EffectiveSquadCap <= 0 && memInner.EntitiesOfFMem.Count <= 0 )
                                continue;
                            //this working variable is not changed by anything other than the UI thread, mostly this location
                            memInner.WorkingFullMembershipStrength_ForUISortOnly = memInner.EffectiveFullMembershipStrength_PlayerFleetsOnly;
                            membersICanSwapTo.Add( memInner );
                        }
                    }
                }
            }

            //now sort the membersICanSwapTo so that their order makes some sort of sense! First by keybinding, then by strength, then by name
            membersICanSwapTo.StableSort( static delegate ( FleetMembership left, FleetMembership right )
            {
                int val = left.Fleet.TiedToKeybindIndexOneIndexed.CompareTo( right.Fleet.TiedToKeybindIndexOneIndexed );
                if ( val != 0 )
                    return val;

                val = right.WorkingFullMembershipStrength_ForUISortOnly.CompareTo( left.WorkingFullMembershipStrength_ForUISortOnly ); //use this version so that it doesn't get updated by other threads and throw an error
                if ( val != 0 )
                    return val;

                val = left.TypeData.DisplayName.CompareTo( right.TypeData.DisplayName );
                if ( val != 0 )
                    return val;

                return left.Fleet.NameRaw.CompareTo( right.Fleet.NameRaw );
            } );
            #endregion

            #region Calculate fleetsWithEmptySlotICanSwapTo
            fleetsWithEmptySlotICanSwapTo.Clear();
            if ( _member != null ) //only allow swapping to empty slots if we're not swapping FROM an empty slot.  Swapping an empty to an empty is just strange
            {
                bool isElite = _member.TypeData.IsElite;
                bool isFleetLeader = _member.TypeData.IsFleetLeader;
                if ( localFactionOrNull != null && !isFleetLeader ) //cannot swap fleet leaders with blanks
                {
                    for ( int i = 0; i < swapFactions.Count; i++ )
                    {
                        foreach ( Fleet fleet in World_AIW2.Instance.Fleets( swapFactions[i], FleetStatus.CenterpieceMustLive ) )
                        {
                            //can't swap with this same fleet!
                            if ( fleet == _fleet )
                                continue;
                            if ( fleet.GetCanSwapFleetLinesWithOtherFleets() != ArcenRejectionReason.Unknown )
                                continue;
                            //can only swap with other mobile fleet types
                            //if ( fleet.Category != FleetCategory.PlayerMobile )
                            //    continue;
                            if ( fleet.CalculateRemainingShipLineSlotCount() <= 0 )
                                continue;
                            if ( isElite )
                            {
                                if ( fleet.CalculateIsEliteSlotFilled() )
                                    continue;
                            }

                            fleetsWithEmptySlotICanSwapTo.Add( fleet );
                        }
                    }
                }

                //now sort the fleetsWithEmptySlotICanSwapTo so that their order makes some sort of sense!
                fleetsWithEmptySlotICanSwapTo.StableSort( static delegate ( Fleet left, Fleet right )
                {
                    //flagships w/o keybindings go at the bottom
                    int val = (left.TiedToKeybindIndexOneIndexed <= 0).CompareTo( right.TiedToKeybindIndexOneIndexed <= 0 );
                    if ( val != 0 )
                        return val;

                    val = left.TiedToKeybindIndexOneIndexed.CompareTo( right.TiedToKeybindIndexOneIndexed );
                    if ( val != 0 )
                        return val;
                    val = left.NameRaw.CompareTo( right.NameRaw );
                    if ( val != 0 )
                        return val;
                    return left.FleetID.CompareTo( right.FleetID );
                } );
            }
            #endregion

            float runningY = -3;

            FleetMembership memPair;
            Fleet fleetPair;

            for ( int j = 0; j < membersICanSwapTo.Count; j++ )
            {
                memPair = membersICanSwapTo[j];

                btnTextWithIcon item = btnTextWithIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( item == null )
                    break; //time slicing, too many added right now
                item.Assign( memPair );              
            }
            for ( int j = 0; j < fleetsWithEmptySlotICanSwapTo.Count; j++ )
            {
                fleetPair = fleetsWithEmptySlotICanSwapTo[j];
                // if ( fleetPair.MemberGroups.Count > 1 && fleetPair.GetCenterpiece().SpecialType != SpecialEntityType.MobileSupportFleetFlagship )
                //     continue; //used flagships were handled earlier

                btnTextWithIcon item = btnTextWithIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( item == null )
                    break; //time slicing, too many added right now
                item.Assign( fleetPair );       
            }

            if ( membersICanSwapTo.Count <= 0 && fleetsWithEmptySlotICanSwapTo.Count <= 0 )
            {
                btnTextWithIcon item = btnTextWithIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( item != null )
                {
                    item.Assign( "目前没有可交换的有效选项！" );
                }
            }

            btnTextWithIconPool.ApplyItemsInRows( 0, ref runningY, 25f, 640f, 24 );

            #region Positioning Logic
            //Now size the parent, called Content, to get scrollbars to appear if needed.
            RectTransform rTran = (RectTransform)btnTextWithIcon.Original.Element.RelevantRect.parent;
            Vector2 sizeDelta = rTran.sizeDelta;
            sizeDelta.y = Mat.Abs( runningY ) + 10f;
            rTran.sizeDelta = sizeDelta;
            #endregion
        }

        #region btnTextWithIcon
        public class btnTextWithIcon : ButtonAbstractBase
        {
            public static btnTextWithIcon Original;
            public btnTextWithIcon() { if ( Original == null ) Original = this; }

            private FleetMembership memPair = null;
            private Fleet fleetPairWithEmptySlot = null;
            private string TextToDisplay;

            private ArcenUIImageArray relatedImages = null;
            private Sprite checkboxOff = null;
            private Sprite checkboxOn = null;

            public void Assign( FleetMembership memPair )
            {
                this.memPair = memPair;

                InitIfPossible();
                if ( relatedImages != null )
                {
                    Color factionCenterColor = ColorMath.White;
                    Color factionTrimColor = ColorMath.Black;
                    if ( memPair != null && memPair.Fleet != null && memPair.Fleet.Faction != null &&
                        memPair.Fleet.Faction.FactionCenterColor != null &&
                        memPair.Fleet.Faction.FactionTrimColor != null )
                    {
                        factionCenterColor = memPair.Fleet.Faction.FactionCenterColor.TeamColor;
                        factionTrimColor = memPair.Fleet.Faction.FactionTrimColor.TeamColor;
                    }
                    if ( memPair != null )
                    {
                        if ( memPair.EffectiveSquadCap > 0 )
                            memPair.EffectiveFullMembershipStrength_PlayerFleetsOnly = memPair.EffectiveSquadCap * memPair.GetStrengthPerSquad_PlayerFleetsOnly();
                    }

                    if ( memPair != null && memPair.TypeData.GUISprite_Icon != null )
                    {
                        relatedImages.SetSpriteAndColor( 0, memPair.TypeData.GUISprite_Icon, factionCenterColor );
                    }

                    if ( memPair != null && memPair.TypeData.GUISprite_IconBorder != null )
                    {
                        relatedImages.SetSpriteAndColor( 1, memPair.TypeData.GUISprite_IconBorder, factionTrimColor );
                    }

                    if ( memPair != null && memPair.TypeData.GUISprite_IconOverlay != null )
                        relatedImages.SetSpriteAndColor( 2, memPair.TypeData.GUISprite_IconOverlay, Color.white );
                    else
                        relatedImages.SetSpriteAndColor( 2, ExternalConstants.Instance.BlankGUISprite, Color.white );

                    if ( Instance._member == null )
                    {
                        if ( membersToSwap.ContainsKey( memPair ) )
                            relatedImages.SetSpriteAndColor( 3, this.checkboxOn, Color.white );
                        else
                            relatedImages.SetSpriteAndColor( 3, this.checkboxOff, Color.white );
                    }
                    else
                        relatedImages.SetSpriteAndColor( 3, ExternalConstants.Instance.BlankGUISprite, Color.white );
                }
            }

            public void Assign( Fleet Fleet )
            {
                this.fleetPairWithEmptySlot = Fleet;

                if ( Fleet != null )
                    Fleet.GetMaxStrengthOfFleet_ForUIOnly( false );

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

                    relatedImages.SetSpriteAndColor( 3, ExternalConstants.Instance.BlankGUISprite, Color.white );
                }
            }

            public void Assign( string text )
            {
                this.TextToDisplay = text;

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

                    this.checkboxOff = this.Element.RelatedSprites[0];
                    this.checkboxOn = this.Element.RelatedSprites[1];
                }
            }

            public override bool GetShouldBeHidden()
            {
                return memPair == null && fleetPairWithEmptySlot == null && String.IsNullOrEmpty( this.TextToDisplay );
            }

            public override void Clear()
            {
                this.memPair = null;
                this.fleetPairWithEmptySlot = null;
                this.TextToDisplay = string.Empty;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( playerFaction == null )
                    playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                if ( this.memPair != null )
                {
                    byte markLevel = memPair.Fleet.Faction.GetGlobalMarkLevelForShipLine( memPair.TypeData );
                    GameEntityTypeData.MarkLevelStats markStatsForDisplay = memPair.TypeData.MarkStatsFor( markLevel );
                    Buffer.Add( "<align=left>" );
                    Buffer.Add( "<pos=5>" );

                    bool isForFleetLeader = memPair.TypeData.IsFleetLeader;
                    int lineCount = 0;
                    int lineShipCount = 0;
                    int lineStrength = 0;
                    if ( isForFleetLeader )
                    {
                        foreach ( FleetMembership memberGroup in memPair.Fleet.MemberGroupsUnsorted_Sim )
                        {
                            if ( memberGroup == null )
                                continue;
                            if ( memberGroup.TypeData.IsDrone || memberGroup.TypeData.SelfConstructs )
                                continue;
                            if ( memberGroup.TypeData.IsFleetLeader )
                                continue;
                            if ( memberGroup.TypeData.ImmuneToSwappingBetweenFleets || memberGroup.TypeData.CannotActuallyBeBuilt_IsNotAShipLine )
                                continue;
                            if ( memberGroup.EffectiveSquadCap <= 0 && memberGroup.EntitiesOfFMem.Count <= 0 )
                                continue;
                            lineCount++;
                            int newLineShipCount = Math.Max( memberGroup.EffectiveSquadCap, memberGroup.EntitiesOfFMem.Count );
                            lineShipCount += newLineShipCount;
                            lineStrength += (newLineShipCount * memberGroup.GetStrengthPerSquad_PlayerFleetsOnly());
                        }
                    }


                    Buffer.StartColor( "c0c0c0" );
                    if ( isForFleetLeader )
                    {
                        Buffer.Add( lineCount ).Add( lineCount != 1 ? " Ship Lines" : "舰线" );
                    }
                    else
                    {
                        string drawName = memPair.GetDisplayNameForSidebar();
                        if ( drawName.Length >= 25 )
                            Buffer.Add( "<size=80%>" );
                        else if ( drawName.Length >= 18 )
                            Buffer.Add( "<size=90%>" );
                        Buffer.Add( drawName.Substring( 0, Math.Min( 40, drawName.Length ) ) );
                        if ( drawName.Length >= 18 )
                            Buffer.Add( "</size>" );
                    }
                    Buffer.EndColor();

                    Buffer.Add( "</pos>" );

                    Buffer.Add( "<pos=210>" );
                    Buffer.StartColor( markStatsForDisplay.MarkLevel.ColorHex );
                    if ( isForFleetLeader )
                    {
                        Buffer.Add( " (" ).Add( lineShipCount ).Add( ")" );
                    }
                    else
                    {
                        if ( memPair.EffectiveSquadCap > 0 )
                        {
                            Buffer.Add( " x" ).Add( memPair.EffectiveSquadCap );
                        }
                    }
                    Buffer.Add( "</pos>" );
                    Buffer.EndColor();
                    Buffer.Add( "<pos=250> " );
                    Buffer.Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon );
                    Buffer.StartColor( "ffa1a1" );
                    if ( isForFleetLeader )
                        ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( Buffer, lineStrength, true, true );
                    else
                        ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( Buffer, memPair.EffectiveFullMembershipStrength_PlayerFleetsOnly, true, true );
                    Buffer.EndColor();
                    Buffer.Add( "</pos>" );
                    if ( playerFaction != memPair.Fleet.Faction )
                    {
                        Buffer.Add( "<pos=310>" );
                        if ( memPair.Fleet.Faction.GetDisplayName().Length > 10 )
                            Buffer.Add( "<size=75%>" );
                        Buffer.Add( memPair.Fleet.Faction.GetDisplayName().Substring( 0, Math.Min( 16, memPair.Fleet.Faction.GetDisplayName().Length ) ), memPair.Fleet.Faction.FactionCenterColor.ColorHexBrighter );
                        if ( memPair.Fleet.Faction.GetDisplayName().Length > 12 )
                            Buffer.Add( "</size>" );
                        Buffer.Add( "</pos>" );
                    }
                    Buffer.Add( "<pos=430>" );
                    if ( memPair.Fleet.GetName().Length > 18 )
                        Buffer.Add( "<size=75%>" );
                    else if ( memPair.Fleet.GetName().Length > 12 )
                        Buffer.Add( "<size=95%>" );
                    Buffer.Add( memPair.Fleet.GetName().Substring( 0, Math.Min( 20, memPair.Fleet.GetName().Length ) ), "7dffc3" );
                    if ( memPair.Fleet.GetName().Length > 12 )
                        Buffer.Add( "</size>" );
                    if ( memPair.Fleet.TiedToKeybindIndexOneIndexed > 0 )
                        Buffer.Add( ", " ).Add( memPair.Fleet.TiedToKeybindIndexOneIndexed, "ff7dc3" );
                    Buffer.Add( ", " );
                    Buffer.Add( "<size=75%>" ).Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon );
                    ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( Buffer, memPair.Fleet.GetMaxStrengthOfFleet_ForUIOnly( false ), true, true );
                    Buffer.Add( "</size>" );
                    Buffer.Add( "</pos>" );
                    Buffer.Add( "</align>" );
                }
                else if ( this.fleetPairWithEmptySlot != null )
                {
                    Fleet fleetPair = this.fleetPairWithEmptySlot;
                    Buffer.Add( "<align=left>" );
                    Buffer.Add( "<pos=5>" );
                    string slot = "Empty Slots:";
                    Buffer.Add( slot, "a9a9a9" );
                    Buffer.Add( "</pos>" );

                    Buffer.Add( "<pos=210>" );
                    Buffer.StartColor( "9370db" );
                    Buffer.Add( " " ).Add( fleetPair.CalculateRemainingShipLineSlotCount() ); //number of ships
                    if ( fleetPair.CalculateIsEliteSlotFilled() )
                        Buffer.Add( " <size=75%>(0x Elite)</size>" );
                    else
                        Buffer.Add( " <size=75%>(1x Elite)</size>" );
                    Buffer.EndColor();
                    Buffer.Add( "</pos>" );
                    //                Buffer.Add("</pos>");
                    //                Buffer.Add("<align=right>");
                    if ( playerFaction != fleetPair.Faction )
                    {
                        Buffer.Add( "<pos=310>" );
                        if ( fleetPair.Faction.GetDisplayName().Length > 10 )
                            Buffer.Add( "<size=75%>" );
                        Buffer.Add( fleetPair.Faction.GetDisplayName().Substring( 0, Math.Min( 16, fleetPair.Faction.GetDisplayName().Length ) ), fleetPair.Faction.FactionCenterColor.ColorHexBrighter ); //faction name
                        if ( fleetPair.Faction.GetDisplayName().Length > 12 )
                            Buffer.Add( "</size>" );
                        Buffer.Add( "</pos>" );
                    }
                    Buffer.Add( "<pos=430>" );

                    if ( fleetPair.GetName().Length > 18 )
                        Buffer.Add( "<size=75%>" );
                    else if ( fleetPair.GetName().Length > 12 )
                        Buffer.Add( "<size=95%>" );
                    Buffer.Add( fleetPair.GetName().Substring( 0, Math.Min( 20, fleetPair.GetName().Length ) ), "7dffc3" );//fleet name
                    if ( fleetPair.GetName().Length > 12 )
                        Buffer.Add( "</size>" );

                    if ( fleetPair.TiedToKeybindIndexOneIndexed > 0 )
                        Buffer.Add( ", " ).Add( fleetPair.TiedToKeybindIndexOneIndexed, "ff7dc3" ); //fleet keybinding
                    int maxStrength = fleetPair.GetMaxStrengthOfFleet_ForUIOnly( false );
                    if ( maxStrength > 1000 )
                    {
                        Buffer.Add( "<size=75%>" );
                        Buffer.Add( ", " + ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon ); //fleet strength
                        ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( Buffer, maxStrength, true, true );
                        Buffer.Add( "</size>" );
                    }
                    Buffer.Add( "</pos>" );
                }
                else if ( !String.IsNullOrEmpty( this.TextToDisplay ) )
                {
                    Buffer.Add( this.TextToDisplay );
                }
                else
                    Buffer.Add( "空？" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.memPair != null )
                {
                    FleetMembership memPairToSwapIn = this.memPair;
                    Fleet fleetToSwapOut = Instance._fleet;
                    FleetMembership memToSwapOutOrNull = Instance._member;

                    if ( memPairToSwapIn == null )
                    {
                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "错误", "不是有效的交换类型！", "确定" );
                        return MouseHandlingResult.PlayClickDeniedSound;
                    }
                    if ( fleetToSwapOut == null )
                    {
                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "错误", "不是有效的交换舰队！", "确定" );
                        return MouseHandlingResult.PlayClickDeniedSound;
                    }
                    if ( memPairToSwapIn.Fleet == fleetToSwapOut )
                    {
                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "无效选择", "交换的类型不能来自同一舰队！", "确定" );
                        return MouseHandlingResult.PlayClickDeniedSound;
                    }

                    if ( memPairToSwapIn.TypeData.IsFleetLeader )
                    {
                        GameEntityTypeData existingFleetLeader = Instance._fleet.Centerpiece.GetSquad()?.TypeData;
                        if ( existingFleetLeader == null )
                        {
                            ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "错误", "你试图交换舰队中除旗舰外的所有成员，但找不到旗舰。", "确定" );
                            return MouseHandlingResult.PlayClickDeniedSound;
                        }
                        //if ( existingFleetLeader.SpecialType != memPairToSwapIn.TypeData.SpecialType )
                        //{
                        //    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "错误", "You are trying to swap a fleet leader, but the category of the new one and existing one do not match.", "确定" );
                        //    return MouseHandlingResult.PlayClickDeniedSound;
                        //}
                    }

                    bool isFleetLeaderSwap = false;
                    if ( memToSwapOutOrNull == null )
                    {
                        if ( membersToSwap.ContainsKey( this.memPair ) )
                            membersToSwap.Remove( this.memPair );
                        else
                            membersToSwap[this.memPair] = true;
                    }
                    else
                    {
                        isFleetLeaderSwap = memToSwapOutOrNull.TypeData.IsFleetLeader;

                        string questionText;
                        if ( isFleetLeaderSwap )
                        {
                            questionText = "确定要交换从舰队 <color=#7dffc3>" + fleetToSwapOut.GetName() +
                                "</color> 与舰队 <color=#7dffc3>" + memPairToSwapIn.Fleet.GetName() + "</color> 之间的所有非旗舰舰线吗？";
                            questionText += "  如果单位不在新旗舰所在星球上或舰线已加载，这将自动报废双向交换的任何现有单位。";
                        }
                        else
                        {
                            questionText = "确定要交换 " + (memToSwapOutOrNull == null ? "空白槽位" : memToSwapOutOrNull.TypeData.GetDisplayName()) + " 从舰队 <color=#7dffc3>" + fleetToSwapOut.GetName() +
                                "</color> 换取 " + memPairToSwapIn.TypeData.GetDisplayName() + " 从舰队 <color=#7dffc3>" + memPairToSwapIn.Fleet.GetName() + "</color>？";
                            questionText += "  如果单位不在新旗舰所在星球上或舰线已加载，这将自动报废两种类型的现有单位。";
                        }

                        ModalPopupData.CreateAndLogYesNoStyle( delegate
                        {
                            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                            if ( memToSwapOutOrNull != null && memToSwapOutOrNull.TypeData.IsFleetLeader )
                            {
                                command.RelatedIntegers.Add( memPairToSwapIn.Fleet.FleetID ); //FleetID 2
                                command.RelatedIntegers2.Add( fleetToSwapOut.FleetID ); //FleetID
                                command.RelatedString = "SwapAllNonFlagshipFleetMembers";
                            }
                            else if ( memToSwapOutOrNull == null )
                            {
                                command.RelatedIntegers.Add( memPairToSwapIn.Fleet.FleetID ); //FleetID 2
                                command.RelatedIntegers2.Add( fleetToSwapOut.FleetID ); //FleetID
                                command.RelatedIntegers3.Add( memPairToSwapIn.UniqueTypeDataDifferentiatorForDuplicates );
                                command.RelatedIntegers4.Add( fleetToSwapOut.GetNextUniqueIntToUseOfMatchingMembershipGroupsBasedOnSquadType( memPairToSwapIn.TypeData ) ); //next ID on fleet1 for fleet2's type
                                command.RelatedString = "SwapFleetMemberWithEmpty";
                                command.RelatedString2 = memPairToSwapIn.TypeData.InternalName;
                            }
                            else //not an empty slot, so allow full swapping
                            {
                                command.RelatedIntegers.Add( fleetToSwapOut.FleetID ); //FleetID
                                command.RelatedIntegers2.Add( memPairToSwapIn.Fleet.FleetID ); //FleetID 2
                                command.RelatedIntegers3.Add( memToSwapOutOrNull.UniqueTypeDataDifferentiatorForDuplicates );
                                command.RelatedIntegers3.Add( memPairToSwapIn.UniqueTypeDataDifferentiatorForDuplicates );
                                command.RelatedIntegers4.Add( memToSwapOutOrNull.Fleet.GetNextUniqueIntToUseOfMatchingMembershipGroupsBasedOnSquadType( memPairToSwapIn.TypeData ) ); //next ID on fleet1 for fleet2's type
                                command.RelatedIntegers4.Add( memPairToSwapIn.Fleet.GetNextUniqueIntToUseOfMatchingMembershipGroupsBasedOnSquadType( memToSwapOutOrNull.TypeData ) ); //next ID on fleet2 for fleet1's type
                                command.RelatedString = "SwapFleetMembers";
                                command.RelatedString2 = memToSwapOutOrNull.TypeData.InternalName;
                                command.RelatedString3 = memPairToSwapIn.TypeData.InternalName;
                            }
                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                            Instance.Close();

                        }, null, isFleetLeaderSwap ? "交换所有非旗舰舰队成员？" : "交换舰队成员？", questionText, "是，交换", "否，等等！" );
                    }

                    return MouseHandlingResult.None;
                }
                else if ( this.fleetPairWithEmptySlot != null )
                {
                    Fleet fleetPairToSwapIn = this.fleetPairWithEmptySlot;
                    FleetMembership memToSwapOut = Instance._member; //this can't be null here, no swapping null for null

                    if ( fleetPairToSwapIn == null )
                    {
                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "错误", "不是有效的交换类型！", "确定" );
                        return MouseHandlingResult.PlayClickDeniedSound;
                    }
                    if ( memToSwapOut == null )
                    {
                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "错误", "不是有效的换出类型！", "确定" );
                        return MouseHandlingResult.PlayClickDeniedSound;
                    }
                    if ( fleetPairToSwapIn == memToSwapOut.Fleet )
                    {
                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "无效选择", "交换的类型不能来自同一舰队！", "确定" );
                        return MouseHandlingResult.PlayClickDeniedSound;
                    }

                    string question = "确定要交换 " + memToSwapOut.TypeData.GetDisplayName() + " 从舰队 <color=#7dffc3>" + memToSwapOut.Fleet.GetName() +
                            "</color> 换取来自舰队 <color=#7dffc3>" + fleetPairToSwapIn.GetName() + "</color> 的空槽位吗？这将自动报废被交换类型的现有单位。";

                    ModalPopupData.CreateAndLogYesNoStyle( delegate
                    {
                        GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                        command.RelatedIntegers.Add( memToSwapOut.Fleet.FleetID ); //FleetID
                        command.RelatedIntegers2.Add( fleetPairToSwapIn.FleetID ); //FleetID 2
                        command.RelatedIntegers3.Add( memToSwapOut.UniqueTypeDataDifferentiatorForDuplicates );
                        command.RelatedIntegers4.Add( fleetPairToSwapIn.GetNextUniqueIntToUseOfMatchingMembershipGroupsBasedOnSquadType( memToSwapOut.TypeData ) ); //next ID on fleet2 for fleet1's type
                        command.RelatedString = "SwapFleetMemberWithEmpty";
                        command.RelatedString2 = memToSwapOut.TypeData.InternalName;
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                        Instance.Close();

                    }, null, "交换舰队成员？", question, "是，交换", "否，等等！" );
                }

                return MouseHandlingResult.PlayClickDeniedSound;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_ModalSwapFleetMembers-btnTextWithIcon-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( this.memPair != null )
                {
                    FleetMembership memPairToSwapIn = this.memPair;
                    if ( memPairToSwapIn != null )
                    {
                        if ( memPairToSwapIn.TypeData.IsFleetLeader && memPairToSwapIn.EntitiesOfFMem.Count > 0 )
                            EntityText.GetTooltip( tooltipBuffer, memPairToSwapIn.EntitiesOfFMem.GetFirst().Contained, memPairToSwapIn, null, -1, null, 0, FromSidebarType.Sidebar_MultipleUnits, ShipExtraDetailFlags.BuildInfo, 1f, false );
                        else
                            EntityText.GetTooltip( tooltipBuffer, null, memPairToSwapIn, null, -1, null, 0, FromSidebarType.Sidebar_MultipleUnits, ShipExtraDetailFlags.BuildInfo, 1f, false );
                        EntityText.Write_Tooltip_Hotkeys_Footer( tooltipBuffer, false, true, null );
                        Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
                    }
                    else
                        Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "空项！", "ShipTooltipScale" );
                }
                else if ( this.fleetPairWithEmptySlot != null )
                {
                    Fleet fleetPairToSwapIn = this.fleetPairWithEmptySlot;
                    if ( fleetPairToSwapIn != null )
                    {
                        int eliteCount = fleetPairToSwapIn.CalculateIsEliteSlotFilled() ? 0 : 1;
                        Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "舰队可用空槽位: " + fleetPairToSwapIn.GetName() + ": " +
                            fleetPairToSwapIn.CalculateRemainingShipLineSlotCount() + " <size=75%>(" + eliteCount + "x Elite Slot)</size>" );
                    }
                    else
                        Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "空项！", "ShipTooltipScale" );
                }
            }
        }
        #endregion

        public class bCancel : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }
        }

        public class bOk : ButtonAbstractBase
        {
            private static readonly List<FleetMembership> memList = List<FleetMembership>.Create_WillNeverBeGCed( 40, "Window_ModalSwapFleetMembers-bOk-memList" );
            private static readonly Dictionary<GameEntityTypeData, int> nextUniqueIntPerType = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed( 40, "Window_ModalSwapFleetMembers-bOk-nextUniqueIntPerType" );

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( Instance._member != null )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "错误", "不能同时换入多种类型和换出一种类型", "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                if ( Instance._fleet == null )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "错误", "未选择要换入这些内容的舰队。", "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                int openSlots = Instance._fleet.CalculateRemainingShipLineSlotCount();
                bool hasEliteSlotOpen = !Instance._fleet.CalculateIsEliteSlotFilled();
                int numberAdded = 0;
                int numberElitesAdded = 0;
                int numberFleetLeadersAdded = 0;

                GameEntityTypeData existingFleetLeader = Instance._fleet.Centerpiece.GetSquad()?.TypeData;

                memList.Clear();
                foreach ( KeyValuePair<FleetMembership, bool> pair in Window_ModalSwapFleetMembers.membersToSwap )
                {
                    if ( pair.Key != null && pair.Key.TypeData != null && //must not be null
                        pair.Key.Fleet != Instance._fleet ) //must not be in same fleet
                    {
                        memList.Add( pair.Key );
                        if ( pair.Key.TypeData.IsElite )
                            numberElitesAdded++;
                        if ( pair.Key.TypeData.IsFleetLeader )
                        {
                            if ( existingFleetLeader == null )
                            {
                                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "错误", "你试图交换舰队旗舰，但找不到现有旗舰。", "确定" );
                                return MouseHandlingResult.PlayClickDeniedSound;
                            }
                            //if ( existingFleetLeader.SpecialType != pair.Key.TypeData.SpecialType )
                            //{
                            //    ModalPopupData.CreateAndLogOKStyle( false, null, "错误", "You are trying to swap a fleet leader, but the category of the new one and existing one do not match.", "确定" );
                            //    return MouseHandlingResult.PlayClickDeniedSound;
                            //}

                            if ( existingFleetLeader == pair.Key.TypeData )
                            {
                                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "错误", "你试图交换舰队旗舰，但新旧类型完全相同！", "确定" );
                                return MouseHandlingResult.PlayClickDeniedSound;
                            }
                            numberFleetLeadersAdded++;
                        }

                        if ( !pair.Key.TypeData.IsFleetLeader && //don't count fleet leaders, since they will be swapped
                            ( !pair.Key.TypeData.IsElite || hasEliteSlotOpen) ) //don't count elites if the slot is filled, again because of swap
                            numberAdded++; 
                    }
                }
                if ( numberAdded > openSlots )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "错误", "你试图添加 " + numberAdded + 
                        " 条舰线，但只有 " + openSlots + " 个空槽位。", "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                if ( numberElitesAdded > 1 )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "错误", "你试图添加 " + numberElitesAdded +
                        " 条精英舰线，但每支舰队只能有一个。", "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                if ( numberFleetLeadersAdded > 0 )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "错误", "你试图交换 " + numberFleetLeadersAdded +
                        " 条旗舰舰线，但在多交换界面中不允许这样做。", "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                if ( !hasEliteSlotOpen && numberElitesAdded > 0 )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "错误", "你试图添加精英舰线，但每支舰队只能有一个（此舰队已有一个）。如果愿意，你需要将现有舰线换出。", "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                string questionText = "确定要将这 " + memList.Count + " 条舰线交换到舰队 <color=#7dffc3>" + Instance._fleet.GetName() +
"</color> 吗？";
                if ( numberElitesAdded > 0 )
                {
                    if ( hasEliteSlotOpen )
                        questionText += "  这将填充空白精英槽位。";
                    else
                        questionText += "  这将替换已填充的精英槽位。";
                }
                if ( numberFleetLeadersAdded > 0 )
                {
                    questionText += "  这将交换舰队旗舰。";
                }

                questionText += "  如果单位不在新旗舰所在星球上或舰线已加载，这将自动报废两种类型的现有单位。";

                ModalPopupData.CreateAndLogYesNoStyle( delegate
                {
                    nextUniqueIntPerType.Clear();

                    foreach ( FleetMembership memPair in memList )
                    {
                        if ( memPair.TypeData.IsFleetLeader )
                        {
                            //swap fleet leaders
                            if ( Instance._fleet.Centerpiece.GetSquad() != null )
                            {
                                GameEntityTypeData fleetLeader = Instance._fleet.Centerpiece.GetSquad().TypeData;

                                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                                command.RelatedIntegers.Add( Instance._fleet.FleetID ); //FleetID
                                command.RelatedIntegers2.Add( memPair.Fleet.FleetID ); //FleetID 2
                                command.RelatedIntegers3.Add( 0 );// memToSwapOutOrNull.UniqueTypeDataDifferentiatorForDuplicates );
                                command.RelatedIntegers3.Add( memPair.UniqueTypeDataDifferentiatorForDuplicates );
                                command.RelatedIntegers4.Add( Instance._fleet.GetNextUniqueIntToUseOfMatchingMembershipGroupsBasedOnSquadType( memPair.TypeData ) ); //next ID on fleet1 for fleet2's type
                                command.RelatedIntegers4.Add( memPair.Fleet.GetNextUniqueIntToUseOfMatchingMembershipGroupsBasedOnSquadType( fleetLeader ) ); //next ID on fleet2 for fleet1's type
                                command.RelatedString = "SwapFleetMembers";
                                command.RelatedString2 = fleetLeader.InternalName;
                                command.RelatedString3 = memPair.TypeData.InternalName;
                                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                                continue;
                            }
                            else //if we can't find the centerpiece, then just skip this one
                                continue;
                        }
                        else if ( memPair.TypeData.IsElite && Instance._fleet.CalculateIsEliteSlotFilled() )
                        {
                            //swap elite
                            FleetMembership eliteMem = Instance._fleet.GetEliteSlotOrNull();
                            if ( eliteMem != null )
                            {
                                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                                command.RelatedIntegers.Add( Instance._fleet.FleetID ); //FleetID
                                command.RelatedIntegers2.Add( memPair.Fleet.FleetID ); //FleetID 2
                                command.RelatedIntegers3.Add( 0 );// memToSwapOutOrNull.UniqueTypeDataDifferentiatorForDuplicates );
                                command.RelatedIntegers3.Add( memPair.UniqueTypeDataDifferentiatorForDuplicates );
                                command.RelatedIntegers4.Add( Instance._fleet.GetNextUniqueIntToUseOfMatchingMembershipGroupsBasedOnSquadType( memPair.TypeData ) ); //next ID on fleet1 for fleet2's type
                                command.RelatedIntegers4.Add( memPair.Fleet.GetNextUniqueIntToUseOfMatchingMembershipGroupsBasedOnSquadType( eliteMem.TypeData ) ); //next ID on fleet2 for fleet1's type
                                command.RelatedString = "SwapFleetMembers";
                                command.RelatedString2 = eliteMem.TypeData.InternalName;
                                command.RelatedString3 = memPair.TypeData.InternalName;
                                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                                continue;
                            }
                            else
                            {
                                //fall down below and just add the elite
                            }
                        }

                        //else if we did not do one of the swaps above...
                        {
                            //just pull in the new item with no swap
                            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                            command.RelatedIntegers.Add( memPair.Fleet.FleetID ); //FleetID 2
                            command.RelatedIntegers2.Add( Instance._fleet.FleetID ); //FleetID
                            command.RelatedIntegers3.Add( memPair.UniqueTypeDataDifferentiatorForDuplicates );
                            int nextID = Instance._fleet.GetNextUniqueIntToUseOfMatchingMembershipGroupsBasedOnSquadType( memPair.TypeData );
                            //if we already added (say) Bombers, then we need to increment AGAIN in this same loop for adding yet more bombers.
                            if ( nextUniqueIntPerType.ContainsKey( memPair.TypeData ) )
                            {
                                nextID = nextUniqueIntPerType[memPair.TypeData];
                                nextID++;
                            }
                            nextUniqueIntPerType[memPair.TypeData] = nextID;

                            command.RelatedIntegers4.Add( nextID ); //next ID on fleet1 for fleet2's type
                            command.RelatedString = "SwapFleetMemberWithEmpty";
                            command.RelatedString2 = memPair.TypeData.InternalName;
                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                        }
                    }

                    Instance.Close();

                }, null, "交换舰队成员？", questionText, "是，交换", "否，等等！" );

                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                //only show the okay button if we're doing checkbox mode, which is specifically also when we are swapping a blank spot out
                return Instance._member != null;
            }
        }
    }
}
