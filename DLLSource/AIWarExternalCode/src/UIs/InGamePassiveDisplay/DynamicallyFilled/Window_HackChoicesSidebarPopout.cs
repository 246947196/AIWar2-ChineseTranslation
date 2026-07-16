using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_HackChoicesSidebarPopout : Window_DynamicallyFilledAbstractBase
    {
        public static Window_HackChoicesSidebarPopout Instance;
        
        private static List<Planet> _sortedPlanetsForHacking = List<Planet>.Create_WillNeverBeGCed( 500, "Window_HackChoicesSidebarPopout-sortedPlanetsForHacking" );
        private static List<SafeSquadWrapper> _sortedUnitsForHacking = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 50, "Window_HackChoicesSidebarPopout-sortedUnitsForHacking" );
        
        public GameEntity_Squad TargetToChooseFor { get; private set; }
        public Planet PlanetToChooseFor { get; private set; }
        public HackingType HackTypeToChooseFor { get; private set; }
        public GameEntity_Squad HackerToUseOrNullIfNoneHere { get; private set; }
        
        private bool _open;
        
        public Window_HackChoicesSidebarPopout()
        {
            Instance = this;
            this.topBuffer = -3;
            this.leftBuffer = 5;
            this.rowHeight = ROW_HEIGHT_DEFAULT;
            this.rowBuffer = 1.5f;
        }

        #region Open/Close Stuff
        
        public void Open( GameEntity_Squad target, Planet planet, HackingType hackType, GameEntity_Squad hacker )
        {
            _open = true;
            
            TargetToChooseFor = target;
            PlanetToChooseFor = planet;
            HackTypeToChooseFor = hackType;
            HackerToUseOrNullIfNoneHere = hacker;
        }
        
        public override void Close()
        {
            _open = false;
        }

        public override void OnHideAfterShowing()
        {
            _open = false;
            
            base.OnHideAfterShowing();
        }
        
        public bool GetIsOpen()
        {
            return _open;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            return _open;
        }

        public void Toggle_Show( GameEntity_Squad target, Planet planet, HackingType hackType, GameEntity_Squad hacker )
        {
            if (_open && 
                TargetToChooseFor == target &&
                PlanetToChooseFor == planet &&
                HackTypeToChooseFor == hackType &&
                HackerToUseOrNullIfNoneHere == hacker )
            {
                this.Close();
                return;
            }
            
            _open = true;
            
            TargetToChooseFor = target;
            PlanetToChooseFor = planet;
            HackTypeToChooseFor = hackType;
            HackerToUseOrNullIfNoneHere = hacker;
        }

        public bool Is_Showing( GameEntity_Squad target, Planet planet, HackingType hackType )
        {
            return _open && 
                   TargetToChooseFor == target &&
                   PlanetToChooseFor == planet &&
                   HackTypeToChooseFor == hackType;
        }

        #endregion
        
        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( Instance.HackTypeToChooseFor.GetDisplayName() );
                if ( Instance.HackTypeToChooseFor.NumberOfTimesIndividualUnitCanBeHacked > 1 )
                {
                    if ( Instance.TargetToChooseFor != null && Instance.TargetToChooseFor.FleetMembership != null )
                    {
                        int timesDoneSoFar = Instance.TargetToChooseFor.GetNumberOfTimesHacked( Instance.HackTypeToChooseFor );
                        int remain = Instance.HackTypeToChooseFor.NumberOfTimesIndividualUnitCanBeHacked - timesDoneSoFar;

                        Buffer.Add( " (" ).Add( remain ).Add( " / " ).Add( Instance.HackTypeToChooseFor.NumberOfTimesIndividualUnitCanBeHacked ).Add( " 剩余)" );
                    }
                }
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
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_HackChoicesSidebarPopout.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                    }
                    #endregion
                }

                this.WindowController.myXPositionScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" );
                this.WindowController.myScale = GameSettings.Current.GetFloatBySetting( "NotificationsScale" );

                //if you change away from the hacking tab, close the hacking choice popout
                if ( Window_InGameSidebarBase.Current != InGameSidebarType.Hacking )
                {
                    Instance.Close();
                    return;
                }
            }
        }

        //private float leftWidth_Normal = 200;
        //private float rightWidth_Normal = 240;

        //private float leftWidth_VeryWideL = 360;
        //private float rightWidth_VeryWideL = 80;

        //private float leftWidth_VeryWideTriple = 280;
        //private float rightWidth_VeryWideTriple = 80;
        //private float thirdWidth_VeryWideTriple = 80;

        private float fullWidth = 440;

        private ArcenCachedExternalTypeDirect type_tHackerChoiceText = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tHackerChoiceText ) );
        private ArcenCachedExternalTypeDirect type_tHackerFleetHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tHackerFleetHeader ) );
        private ArcenCachedExternalTypeDirect type_bChooseASpecificShipLineToGrant = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bChooseASpecificShipLineToGrant ) );
        private ArcenCachedExternalTypeDirect type_bChooseASpecificPlanetNomadCrash = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bChooseASpecificPlanetNomadCrash ) );
        private ArcenCachedExternalTypeDirect type_bChooseASpecificPlanet = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bChooseASpecificPlanet ) );
        private ArcenCachedExternalTypeDirect type_bChooseASpecificNecropolis = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bChooseASpecificNecropolis ) );

        public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            if ( bMainContentParent.ParentT == null )
                return;
            this.Window.SetOverridingTransformToWhichToAddChildren( bMainContentParent.ParentT );

            float runningY = topBuffer;

            Rect leftBounds;

            GameEntity_Squad hackerOrNullIfNoneHere = HackingUtils.CalculateHackerForHack( this.TargetToChooseFor, this.PlanetToChooseFor, this.HackTypeToChooseFor, false );

            #region Hacking Choice Instructions *****************************************
            if ( this.HackTypeToChooseFor.ChoiceText != null && this.HackTypeToChooseFor.ChoiceText.Length > 0 )
            {
                this.rowHeight = this.HackTypeToChooseFor.ChoiceTextHeight;
                this.CalculateBoundsSingle( out leftBounds, ref runningY, fullWidth );
                AddText( Set, type_tHackerChoiceText, string.Empty, -1, -1, leftBounds, 13f );
                    
            }
            #endregion *****************************************

            #region Hacker Fleet Header *****************************************
            this.rowHeight = ROW_HEIGHT_DEFAULT;
            this.CalculateBoundsSingle( out leftBounds, ref runningY, fullWidth );
            AddText( Set, type_tHackerFleetHeader, string.Empty, -1, -1, leftBounds, 13f );
            #endregion *****************************************

            #region ChooseASpecificShipLineToGrant Buttons *******************************
            if ( Instance.HackTypeToChooseFor.ChooseASpecificShipLineToGrant )
            {
                for ( int j = 0; j < TargetToChooseFor.ShipGrantsList.Count; j++ )
                {
                    this.CalculateBoundsSingle( out leftBounds, ref runningY, fullWidth );
                    AddButton( Set, type_bChooseASpecificShipLineToGrant, TargetToChooseFor.ShipGrantsList[j].TypeData.InternalName, j, j, leftBounds, -1f );
                }
            }
            #endregion *******************************

            #region ChooseASpecificPlanetToTarget *******************************
            if ( Instance.HackTypeToChooseFor.ChooseASpecificPlanetToTarget && hackerOrNullIfNoneHere != null )
            {
                _sortedPlanetsForHacking.Clear();
                short idxCurrentlyViewed = Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed();
                Planet currentPlanet = World_AIW2.Instance.GetPlanetByIndex(idxCurrentlyViewed);
                foreach ( Planet planet in World_AIW2.Instance.CurrentGalaxy.Planets( false ) )
                {
                    if ( planet == null )
                        continue;
                    if ( planet.IntelLevel == PlanetIntelLevel.Unexplored )
                        continue; //only explored planets
                    if ( Instance.HackTypeToChooseFor.MustChooseAdjacentPlanet && currentPlanet != null )
                    {
                        if ( !planet.GetIsDirectlyLinkedTo (false, currentPlanet ) )
                            continue;
                    }
                    // Faction controllingFaction = planet.GetControllingFaction();
                    // if ( controllingFaction != null &&
                    //      controllingFaction.Type != FactionType.Player )
                    // {
                        _sortedPlanetsForHacking.Add(planet);
                    //}
                }
                _sortedPlanetsForHacking.Sort( static delegate( Planet L, Planet R)
                {
                    //overlords at the top, then sorted from High-Mark AI planets to low-mark AI planets
                    if ( L.PopulationType == PlanetPopulationType.AIHomeworld &&
                         R.PopulationType != PlanetPopulationType.AIHomeworld )
                        return -1;
                    if ( R.PopulationType == PlanetPopulationType.AIHomeworld &&
                         L.PopulationType != PlanetPopulationType.AIHomeworld )
                        return 1;
                    int val = R.MarkLevelForAIOnly.Ordinal.CompareTo(L.MarkLevelForAIOnly.Ordinal);
                    if ( val != 0 )
                        return val;
                    return L.Name.CompareTo(R.Name);
                } );
                for ( int i = 0; i < _sortedPlanetsForHacking.Count; i++ )
                {
                    Planet planet = _sortedPlanetsForHacking[i];
                    this.CalculateBoundsSingle( out leftBounds, ref runningY, fullWidth );
                    if ( Instance.HackTypeToChooseFor.InternalName == "SendNomadToNearestAIKing" )
                        AddButton( Set, type_bChooseASpecificPlanetNomadCrash, planet.Name, planet.Index, -1, leftBounds, -1f );
                    else
                        AddButton( Set, type_bChooseASpecificPlanet, planet.Name, planet.Index, -1, leftBounds, -1f );
                }
            }
            #endregion *******************************
            #region ChooseASpecificNecropolisToTarget *******************************
            if ( Instance.HackTypeToChooseFor.ChooseASpecificNecropolisToTarget && hackerOrNullIfNoneHere != null )
            {
                _sortedUnitsForHacking.Clear();
                short idxCurrentlyViewed = Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed();
                Planet currentPlanet = World_AIW2.Instance.GetPlanetByIndex(idxCurrentlyViewed);

                NecromancerEmpireFactionBaseInfo baseInfo = hackerOrNullIfNoneHere.PlanetFaction.Faction.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
                foreach ( KeyValuePair<SafeSquadWrapper, bool> _kv in baseInfo.Necropoleis.GetDisplayList() )
                {
                    SafeSquadWrapper cityWrapper = _kv.Key;
                    GameEntity_Squad city = cityWrapper.GetSquad();
                    if ( city == null )
                        continue;
                    if ( city.GetIsCrippled() || city.TypeData.GetHasTag("Phylactery") )
                        continue;
                    if ( city.SelfBuildingMetalRemaining > 0 )
                        continue; //still under construction
                    if ( city.Planet == currentPlanet )
                        continue; //don't show the current planet
                    _sortedUnitsForHacking.Add( cityWrapper );
                }

                _sortedUnitsForHacking.Sort( static delegate( SafeSquadWrapper L, SafeSquadWrapper R)
                {
                    if ( L.CurrentMarkLevel != R.CurrentMarkLevel )
                        return L.CurrentMarkLevel.CompareTo( R.CurrentMarkLevel );
                    return L.Planet.Name.CompareTo(R.Planet.Name);
                } );
                for ( int i = 0; i < _sortedUnitsForHacking.Count; i++ )
                {
                    GameEntity_Squad squad = _sortedUnitsForHacking[i].GetSquad();
                    if ( squad == null )
                        continue;
                    this.CalculateBoundsSingle( out leftBounds, ref runningY, fullWidth );
                    AddButton( Set, type_bChooseASpecificNecropolis, squad.Planet.Name, squad.PrimaryKeyID, -1, leftBounds, -1f );
                }
            }
            #endregion *******************************

            #region AddAllButtonsForSingularChoiceInSubMenu Buttons *******************************
            if ( Instance.HackTypeToChooseFor.Implementation.GetDoesHackRequireASinglularChoiceFromASubmenu() )
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction != null )
                {
                    ///bounds of the first button, whether we use it or not
                    this.CalculateBoundsSingle( out leftBounds, ref runningY, fullWidth );
                    //now add 0+ buttons
                    Instance.HackTypeToChooseFor.Implementation.AddAllButtonsForSingularChoiceInSubMenu( Instance.TargetToChooseFor,
                        Instance.PlanetToChooseFor, localFaction, Instance.HackTypeToChooseFor, Set, ref runningY, leftBounds, this.rowBuffer,
                        AddCustomHackingButton );
                }
                
            }
            #endregion *******************************

            bMainContentParent.ParentRT.UI_SetHeight( runningY );
        }

        /// <summary>
        /// used with a delegate
        /// </summary>
        public void AddCustomHackingButton( ArcenUI_SetOfCreateElementDirectives Set, ArcenCachedExternalTypeDirect ControllerType, string CodeDirectiveTagString, int CodeDirectiveTag1, int CodeDirectiveTag2, Rect rect )
        {
            AddButton( Set, ControllerType, CodeDirectiveTagString, CodeDirectiveTag1, CodeDirectiveTag2, rect, -1f );
        }

        #region tHackerChoiceText
        public class tHackerChoiceText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.Add( Instance.HackTypeToChooseFor.ChoiceText );
                if ( Instance.HackTypeToChooseFor.HoldKeyForVerbose )
                    buffer.Add("\n").Add( "<size=80%><color=#d18444>按住 <color=#996f4c>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key1" ) ).Add( "</color> 查看更详细的信息。</color></size>" );
            }
        }
        #endregion

        #region tHackerFleetHeader
        public class tHackerFleetHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                GameEntity_Squad hacker = HackingUtils.CalculateHackerForHack( Instance.TargetToChooseFor, Instance.PlanetToChooseFor, Instance.HackTypeToChooseFor, false ); //this is handled above, in terms of true cases
                if ( hacker != null )
                    buffer.Add( "<color=#999999>入侵者舰队：</color>" ).Add( hacker.GetFleetName_Safe() );
                else
                    buffer.Add( "<color=#999999>入侵者舰队：</color>无" );
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_HackChoicesSidebarPopout-tHackerFleetHeader-tooltipBuffer" );
            public override void HandleMouseover()
            {
                GameEntity_Squad hacker = HackingUtils.CalculateHackerForHack( Instance.TargetToChooseFor, Instance.PlanetToChooseFor, Instance.HackTypeToChooseFor, false ); //this is handled above, in terms of true cases
                if ( hacker == null )
                    return;

                Window_InGameSidebarFleets.btnFleet.WriteFleetTooltip( hacker.GetFleetOrNull_Safe(), tooltipBuffer );
                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
            }
        }
        #endregion

        #region GetGameTypeDataFromElement
        public static GameEntityTypeData GetGameTypeDataFromElementOrNull( ArcenUI_Element element )
        {
            if ( element == null || Instance.HackTypeToChooseFor == null )
                return null;
            if ( element.CreatedByCodeDirective == null )
                return null;
            string entityTypeName = element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString;
            return GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( entityTypeName );
        }
        #endregion

        #region GetUniqueTypeDataDifferentiatorForDuplicatesFromElement
        public static byte GetUniqueTypeDataDifferentiatorForDuplicatesFromElement( ArcenUI_Element element )
        {
            if ( element == null || Instance.HackTypeToChooseFor == null )
                return 0;
            if ( element.CreatedByCodeDirective == null )
                return 0;
            return (byte)element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
        }
        #endregion

        //#region GetGameTypeDataFromElement
        //public static GameEntityTypeData GetGameTypeDataFromElement( ArcenUI_Element element )
        //{
        //    if ( element == null || Instance.hackTypeToChooseFor == null )
        //        return null;
        //    if ( element.CreatedByCodeDirective == null )
        //        return null;
        //    int index = element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
        //    if ( index < 0 || index >= Instance.hackTypeToChooseFor.MemberGroups.Count )
        //        return null;
        //    return Instance.hackTypeToChooseFor.MemberGroups[index];
        //}
        //#endregion

        #region bChooseASpecificShipLineToGrant
        public class bChooseASpecificShipLineToGrant : ButtonAbstractBase
        {
            public int GetCountToAdd( GameEntityTypeData typeData )
            {
                ShipLineEntry entry = null;
                Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                if ( localFaction == null )
                    return 0;
                for ( int i = 0; i < Instance.TargetToChooseFor.ShipGrantsList.Count; i++ )
                {
                    entry = Instance.TargetToChooseFor.ShipGrantsList[i];
                    if ( entry?.TypeData == typeData )
                    {
                        byte markLevel = localFaction.GetGlobalMarkLevelForShipLine(typeData);
                        GameEntityTypeData.MarkLevelStats markStatsForDisplay = entry?.TypeData?.MarkStatsFor( markLevel );
                        if ( markStatsForDisplay == null )
                            return 0;
                        int numShips = entry.GetNumShipsForHackAndHacker( Instance.HackerToUseOrNullIfNoneHere, Instance.HackTypeToChooseFor );
                        //Chris says: these are theoretical adds, so this is the base cap and that gets marked up.  This is okay.
                        int cap = typeData.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( entry.TypeData, numShips, numShips, markStatsForDisplay.MarkLevel );
                        return cap;
                    }
                }
                return 0;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                GameEntityTypeData typeData = GetGameTypeDataFromElementOrNull( this.Element );
                if ( typeData == null )
                    return;
                bool showVerboseDetails = InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1();

                Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                if ( localFaction == null ) 
                    return;

                Buffer.AddShipIconInline( typeData, localFaction );

                if ( !this.GetCanHackForThisLine() )
                {
                    if ( showVerboseDetails )
                        Buffer.Add("<size=80%>"); //make the name a bit smaller if we are in verbose mode, since there's more text to fit in the line
                    Buffer.Add( "<color=#c74639>无法选择：</color> " );
                    if ( showVerboseDetails )
                        Buffer.Add("</size>");
                }
                byte markLevel = localFaction.GetGlobalMarkLevelForShipLine( typeData );

                GameEntityTypeData.MarkLevelStats markStatsForDisplay = typeData.MarkStatsFor( markLevel );
                if ( markStatsForDisplay != null && markStatsForDisplay.MarkLevel.Ordinal > 0 )
                {
                    Buffer.StartColor( markStatsForDisplay.MarkLevel.ColorHex );
                    if ( showVerboseDetails )
                        Buffer.Add("<size=80%>"); //make the name a bit smaller if we are in verbose mode, since there's more text to fit in the line
                    Buffer.Add( typeData.DisplayName );
                    if ( showVerboseDetails )
                        Buffer.Add("</size>");
                    Buffer.Add( " " );
                    Buffer.Add( markStatsForDisplay.MarkLevel.MapDisplay );
                    Buffer.EndColor();
                }
                else
                    Buffer.Add( typeData.DisplayName );

                int countToAdd = this.GetCountToAdd( typeData );
                Buffer.Add( "<color=#ffdf72> x" ).Add( countToAdd ).Add( "</color>" );

                if ( markStatsForDisplay != null )
                {
                    Buffer.Add( "  " );
                    int totalStrength = (countToAdd * markStatsForDisplay.StrengthPerSquad_CalculatedWithNullFleetMembership);
                    ArcenExternalUIUtilities.AddSizeAndVOssetToText( Buffer, ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon, 12, 2 );
                    Buffer.StartColor( ArcenExternalUIUtilities.StrengthTextColor );
                    ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( Buffer, totalStrength, true, true );
                    Buffer.EndColor();
                    if ( showVerboseDetails )
                    {
                        Buffer.StartColor( Color.grey ).Add("<size=60%>").Add(" ");
                        for ( int i = 0; i < typeData.TechUpgradesThatBenefitMe.Count; i++ )
                        {
                            TechUpgrade upgrade = typeData.TechUpgradesThatBenefitMe[i];
                            Buffer.Add(" ").Add(upgrade.DisplayName);
                            Buffer.Add(": ").Add( upgrade.UIOnly_Tech_ShipLinesAffected.ToString(), ArcenExternalUIUtilities.ShipLineIncreaseColor ).Add(" ").Add( upgrade.UIOnly_Tech_DefensiveLinesAffected.ToString(),  ArcenExternalUIUtilities.DefenseLineIncreaseColor ).Add("");
                        }
                        Buffer.Add("</size>").EndColor();
                    }
                    Buffer.Add(" "); //my "show verbose details" code does not toggle on/off correctly without this extra line. I don't know why
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                GameEntityTypeData typeData = GetGameTypeDataFromElementOrNull( this.Element );
                if ( typeData == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                if ( !this.GetCanHackForThisLine() )
                    return MouseHandlingResult.PlayClickDeniedSound;

                bool doAreYouSurePrompt = GameSettings.Current.GetBoolBySetting( "UpgradeShipPrompt" ) && !InputCaching.CalculateHoldAndSuppressTechUpgradePrompt();
                if ( doAreYouSurePrompt )
                {
                    GameEntity_Squad hacker = HackingUtils.GetPreferredHacker( Instance.TargetToChooseFor, Instance.HackTypeToChooseFor, Instance.PlanetToChooseFor, true );
                    if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                        return MouseHandlingResult.PlayClickDeniedSound;
                    ModalPopupData.CreateAndLogYesNoStyle( DoHack, null, "确认", "你确定要对 " + typeData.DisplayName + " 执行入侵 " + Instance.HackTypeToChooseFor.DisplayName + 
                        " 吗？\n \n" + "<color=#888888>要禁用此提示，请进入游戏设置，在游戏选项卡下将其切换为关闭。或者在点击升级按钮时按住 " +
                        InputActionTypeDataTable.GetActionByName_FairlySlow( "SuppressTechUpgradePrompt" ).GetHumanReadableKeyCombo() + " 来跳过本次提示。</color>", "是，入侵", "不，不要" );
                }
                else
                    DoHack();

                return MouseHandlingResult.None;
            }

            public void DoHack()
            {
                GameEntity_Squad hacker = HackingUtils.GetPreferredHacker( Instance.TargetToChooseFor, Instance.HackTypeToChooseFor, Instance.PlanetToChooseFor, true );
                if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                    return;
                HackingUtils.TryDoHack( ref lastRejectionReason, Instance.TargetToChooseFor, Instance.PlanetToChooseFor, Instance.HackTypeToChooseFor,
                                            this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString, -1, null );
            }

            private string lastRejectionReason = string.Empty;
            public bool GetCanHackForThisLine()
            {
                GameEntity_Squad hacker = HackingUtils.CalculateHackerForHack( Instance.TargetToChooseFor, Instance.PlanetToChooseFor, Instance.HackTypeToChooseFor, false ); //this is handled above, in terms of true cases
                if ( hacker == null )
                {
                    lastRejectionReason = "当前没有可用的入侵者";
                    return false;
                }
                return Instance.HackTypeToChooseFor.Implementation.GetCanBeHacked( Instance.TargetToChooseFor, hacker,
                    Instance.PlanetToChooseFor, hacker.GetFactionOrNull_Safe(), Instance.HackTypeToChooseFor,
                    this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString, -1, out lastRejectionReason ) == Hackable.CanBeHacked;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_HackChoicesSidebarPopout-bChooseASpecificShipLineToGrant-tooltipBuffer" );
            public override void HandleMouseover()
            {
                GameEntityTypeData typeData = GetGameTypeDataFromElementOrNull( this.Element );
                if ( typeData == null )
                    Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "Null GameEntityTypeData, this is a bug." );
                else
                {
                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    byte markLevel = localFaction.GetGlobalMarkLevelForShipLine( typeData );

                    tooltipBuffer.Add( "<b><u>入侵：" ).Add( Instance.HackTypeToChooseFor.DisplayName ).Add( "</u></b>\n" );

                    EntityText.GetTooltip( tooltipBuffer, null, null, typeData, this.GetCountToAdd( typeData ),
                        localFaction, markLevel, FromSidebarType.Sidebar_MultipleUnits,
                        ShipExtraDetailFlags.BuildInfo | ShipExtraDetailFlags.AnyGrantHackInfo | ShipExtraDetailFlags.WindowHackChoicesSidebarPopoutForData, 1f, false );
                    EntityText.Write_Tooltip_Hotkeys_Footer( tooltipBuffer, false, true, null );

                    if ( !this.GetCanHackForThisLine() )
                        tooltipBuffer.Add( "\n\n<color=#c74639>无法选择此选项：" + this.lastRejectionReason + "。</color>" );
                    else
                    {
                    if ( GameSettings.Current.GetBoolBySetting( "UpgradeShipPrompt" ) )
                    {
                        tooltipBuffer.Add( "\n\n<color=#3f6c9e>按住 </color><color=#4486d1>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "SuppressTechUpgradePrompt" ) )
                            .Add( "</color> <color=#3f6c9e>可跳过特定入侵的'你确定吗'提示。</color>  " );
                    }
                    }

                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
                }
            }
        }
        #endregion

        #region bChooseASpecificPlanetNomadCrash
        public class bChooseASpecificPlanetNomadCrash : ButtonAbstractBase
        {
            private Planet planet = null;
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( planet == null ||
                     planet.Name != this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString )
                    planet = World_AIW2.Instance.GetPlanetByIndex((short)this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1);
                if ( planet != null )
                {
                    Faction faction = planet.GetControllingFaction();
                    string factionColor = "a1ffa1";
                    if ( faction != null )
                        factionColor = faction.FactionCenterColor.ColorHexBrighter;
                    if ( planet.PopulationType == PlanetPopulationType.AIBastionWorld )
                    {
                        Buffer.Add("<b>堡垒："+ this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString + "</b>", factionColor );
                    }
                    else if ( planet.PopulationType == PlanetPopulationType.AIHomeworld )
                    {
                        Buffer.Add("<b>母星："+ this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString + "</b>", factionColor );
                    }
                    else
                    {
                        if ( faction.Type == FactionType.AI )
                            Buffer.Add( "标记 " + planet.MarkLevelForAIOnly.Ordinal +": " +this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString, factionColor );
                        else
                            Buffer.Add( this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString, factionColor );
                    }
                }
                else
                    Buffer.Add( "???" );
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( planet == null ||
                     planet.Name != this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString )
                    planet = World_AIW2.Instance.GetPlanetByIndex( (short)this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1 );

                bool doAreYouSurePrompt = GameSettings.Current.GetBoolBySetting( "UpgradeShipPrompt" ) && !InputCaching.CalculateHoldAndSuppressTechUpgradePrompt();
                if ( doAreYouSurePrompt )
                {
                    GameEntity_Squad hacker = HackingUtils.GetPreferredHacker( Instance.TargetToChooseFor, Instance.HackTypeToChooseFor, Instance.PlanetToChooseFor, true );
                    if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                        return MouseHandlingResult.PlayClickDeniedSound;
                    ModalPopupData.CreateAndLogYesNoStyle( DoHack, null, "确认", "你确定要对 " + (planet == null ? "null" : planet.Name ) + " 执行入侵 " + Instance.HackTypeToChooseFor.DisplayName +
                        " 吗？\n \n" + "<color=#888888>要禁用此提示，请进入游戏设置，在游戏选项卡下将其切换为关闭。或者在点击升级按钮时按住 " +
                        InputActionTypeDataTable.GetActionByName_FairlySlow( "SuppressTechUpgradePrompt" ).GetHumanReadableKeyCombo() + " 来跳过本次提示。</color>", "是，入侵", "不，不要" );
                }
                else
                    DoHack();

                return MouseHandlingResult.None;
            }

            public void DoHack()
            {
                GameEntity_Squad hacker = HackingUtils.GetPreferredHacker( Instance.TargetToChooseFor, Instance.HackTypeToChooseFor, Instance.PlanetToChooseFor, true );
                if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                    return;

                string lastRejectionReason = "";
                if ( planet == null ||
                     planet.Name != this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString )
                    planet = World_AIW2.Instance.GetPlanetByIndex( (short)this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1 );

                HackingUtils.TryDoHack( ref lastRejectionReason, Instance.TargetToChooseFor, Instance.PlanetToChooseFor, Instance.HackTypeToChooseFor,
                                        this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString, this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1, planet );
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_HackChoicesSidebarPopout-bChooseASpecificPlanetNomadCrash-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( planet == null ||
                     planet.Name != this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString )
                    planet = World_AIW2.Instance.GetPlanetByIndex((short)this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1);
                
                if ( planet != null )
                {
                    tooltipBuffer.Add( "<b><u>入侵：" ).Add( Instance.HackTypeToChooseFor.DisplayName ).Add( "</u></b>\n" );

                    tooltipBuffer.Add( "将你的流浪者撞向 " + this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString +"。");
                    if ( planet.PopulationType == PlanetPopulationType.AIBastionWorld )
                    {
                        tooltipBuffer.Add("\n").Add( "预计AI将对威胁堡垒世界做出大规模回应。");
                    }
                    else if ( planet.PopulationType == PlanetPopulationType.AIHomeworld )
                    {
                        tooltipBuffer.Add("\n").Add( "预计AI将对威胁AI母星做出大规模回应。");
                    }
                    else
                        tooltipBuffer.Add("\n").Add( "预计AI将对此世界做出非常强力的回应。");
                    short idxCurrentlyViewed = Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed();
                    Planet currentPlanet = World_AIW2.Instance.GetPlanetByIndex(idxCurrentlyViewed);
                    if ( currentPlanet != null )
                    {
                        int distance = Mat.DistanceBetweenPointsImprecise( planet.GalaxyLocation, currentPlanet.GalaxyLocation );
                        int approximateTime = NomadPlanetsFactionBaseInfo.Instance == null ? -1 : NomadPlanetsFactionBaseInfo.Instance.GetCrashTime( planet, currentPlanet, distance );
                        if ( approximateTime > 0 )
                        {
                            tooltipBuffer.Add("\n").Add( "流浪者将在 ").AddHoursAndMinutes( approximateTime, "a1ffa1").Add(" 内以距离 " + distance + " 撞向 ").Add(this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString).Add("。 ");
                        }
                    }

                    if ( GameSettings.Current.GetBoolBySetting( "UpgradeShipPrompt" ) )
                    {
                        tooltipBuffer.Add( "\n\n<color=#3f6c9e>按住 </color><color=#4486d1>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "SuppressTechUpgradePrompt" ) )
                            .Add( "</color> <color=#3f6c9e>可跳过特定入侵的'你确定吗'提示。</color>  " );
                    }

                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
                }
            }
        }
        #endregion
        #region bChooseASpecificPlanet
        public class bChooseASpecificPlanet : ButtonAbstractBase
        {
            private Planet planet = null;
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( planet == null ||
                     planet.Name != this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString )
                    planet = World_AIW2.Instance.GetPlanetByIndex((short)this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1);
                if ( planet != null )
                {
                    Faction faction = planet.GetControllingFaction();
                    string factionColor = "a1ffa1";
                    if ( faction != null )
                        factionColor = faction.FactionCenterColor.ColorHexBrighter;
                    if ( planet.PopulationType == PlanetPopulationType.AIBastionWorld )
                    {
                        Buffer.Add("<b>Bastion: "+ this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString + "</b>", factionColor );
                    }
                    else if ( planet.PopulationType == PlanetPopulationType.AIHomeworld )
                    {
                        Buffer.Add("<b>Overlord: "+ this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString + "</b>", factionColor );
                    }
                    else
                    {
                        if ( faction.Type == FactionType.AI )
                            Buffer.Add( "等级 " + planet.MarkLevelForAIOnly.Ordinal +"： " +this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString, factionColor );
                        else
                            Buffer.Add( this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString, factionColor );
                    }
                }
                else
                    Buffer.Add( "???" );
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( planet == null ||
                     planet.Name != this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString )
                    planet = World_AIW2.Instance.GetPlanetByIndex((short)this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1);

                bool doAreYouSurePrompt = GameSettings.Current.GetBoolBySetting( "UpgradeShipPrompt" ) && !InputCaching.CalculateHoldAndSuppressTechUpgradePrompt();
                if ( doAreYouSurePrompt )
                {
                    GameEntity_Squad hacker = HackingUtils.GetPreferredHacker( Instance.TargetToChooseFor, Instance.HackTypeToChooseFor, Instance.PlanetToChooseFor, true );
                    if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                        return MouseHandlingResult.PlayClickDeniedSound;
                    ModalPopupData.CreateAndLogYesNoStyle( DoHack, null, "确认", "你确定要对 " + (planet == null ? "null" : planet.Name) + " 执行入侵 " + Instance.HackTypeToChooseFor.DisplayName +
                        " 吗？\n \n" + "<color=#888888>要禁用此提示，请进入游戏设置，在游戏选项卡下将其切换为关闭。或者在点击升级按钮时按住 " +
                        InputActionTypeDataTable.GetActionByName_FairlySlow( "SuppressTechUpgradePrompt" ).GetHumanReadableKeyCombo() + " 来跳过本次提示。</color>", "是，入侵", "不，不要" );
                }
                else
                    DoHack();

                 return MouseHandlingResult.None;
            }

            public void DoHack()
            {
                GameEntity_Squad hacker = HackingUtils.GetPreferredHacker( Instance.TargetToChooseFor, Instance.HackTypeToChooseFor, Instance.PlanetToChooseFor, true );
                if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                    return;

                string lastRejectionReason = "";
                if ( planet == null ||
                     planet.Name != this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString )
                    planet = World_AIW2.Instance.GetPlanetByIndex( (short)this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1 );

                HackingUtils.TryDoHack( ref lastRejectionReason, Instance.TargetToChooseFor, Instance.PlanetToChooseFor, Instance.HackTypeToChooseFor,
                                        this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString, this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1, planet );
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_HackChoicesSidebarPopout-bChooseASpecificPlanet-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( planet == null ||
                     planet.Name != this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString )
                    planet = World_AIW2.Instance.GetPlanetByIndex((short)this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1);
                
                if ( planet != null )
                {
                    tooltipBuffer.Add( "<b><u>Hack: " ).Add( Instance.HackTypeToChooseFor.DisplayName ).Add( "</u></b>\n" );

                    tooltipBuffer.Add("你可以选择 " + planet.Name + " 作为目标。");


                    if ( GameSettings.Current.GetBoolBySetting( "UpgradeShipPrompt" ) )
                    {
                        tooltipBuffer.Add( "\n\n<color=#3f6c9e>按住 </color><color=#4486d1>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "SuppressTechUpgradePrompt" ) )
                            .Add( "</color> <color=#3f6c9e>可跳过特定入侵的'你确定吗'提示。</color>  " );
                    }

                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
                }
            }
        }
        #endregion
        #region bChooseASpecificNecropolis
        public class bChooseASpecificNecropolis : ButtonAbstractBase
        {
            private GameEntity_Squad squad  = null;
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( squad == null )
                    squad = World_AIW2.Instance.GetEntityByID_Squad(this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1);
                if ( squad != null )
                {
                    PlanetFaction pFaction = squad.PlanetFaction;
                    if ( pFaction != null )
                    {
                        Faction faction = squad.PlanetFaction.Faction;
                        string factionColor = "a1ffa1";
                        if ( faction != null )
                            factionColor = faction.FactionCenterColor.ColorHexBrighter;
                        Buffer.Add("<b>").Add(squad.Planet.Name).Add("<b>");
                    }
                }
                else
                    Buffer.Add( "??? we had key " + this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1);
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( squad == null )
                    squad = World_AIW2.Instance.GetEntityByID_Squad((short)this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1);
                string unused = "";
                bool canDo = HackingUtils.CalculateCanDoThisHack( ref unused, Instance.TargetToChooseFor, Instance.PlanetToChooseFor, Instance.HackTypeToChooseFor );
                if ( !canDo )
                    return MouseHandlingResult.PlayClickDeniedSound;

                bool doAreYouSurePrompt = GameSettings.Current.GetBoolBySetting( "UpgradeShipPrompt" ) && !InputCaching.CalculateHoldAndSuppressTechUpgradePrompt();
                if ( doAreYouSurePrompt )
                {
                    GameEntity_Squad hacker = HackingUtils.GetPreferredHacker( Instance.TargetToChooseFor, Instance.HackTypeToChooseFor, Instance.PlanetToChooseFor, true );
                    if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                        return MouseHandlingResult.PlayClickDeniedSound;
                    ModalPopupData.CreateAndLogYesNoStyle( DoHack, null, "确认", "你确定要对 " + (squad == null ? "null" : squad.Planet.Name) + " 执行入侵 " + Instance.HackTypeToChooseFor.DisplayName +
                        " 吗？\n \n" + "<color=#888888>要禁用此提示，请进入游戏设置，在游戏选项卡下将其切换为关闭。或者在点击升级按钮时按住 " +
                        InputActionTypeDataTable.GetActionByName_FairlySlow( "SuppressTechUpgradePrompt" ).GetHumanReadableKeyCombo() + " 来跳过本次提示。</color>", "是，入侵", "不，不要" );
                }
                else
                    DoHack();

                 return MouseHandlingResult.None;
            }

            public void DoHack()
            {
                GameEntity_Squad hacker = HackingUtils.GetPreferredHacker( Instance.TargetToChooseFor, Instance.HackTypeToChooseFor, Instance.PlanetToChooseFor, true );
                if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                    return;

                string lastRejectionReason = "";
                if ( squad == null )
                    squad = World_AIW2.Instance.GetEntityByID_Squad((short)this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1);

                HackingUtils.TryDoHack( ref lastRejectionReason, Instance.TargetToChooseFor, Instance.PlanetToChooseFor, Instance.HackTypeToChooseFor,
                                        this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString, this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1, squad.Planet );
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_HackChoicesSidebarPopout-bChooseASpecificPlanet-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( squad == null )
                    squad = World_AIW2.Instance.GetEntityByID_Squad((short)this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1);

                if ( squad != null )
                {
                    World_AIW2.Instance.FocusedSquadForMapDarkening = squad;
                    tooltipBuffer.Add( "<b><u>入侵：" ).Add( Instance.HackTypeToChooseFor.DisplayName ).Add( "</u></b>\n" );
                    
                    tooltipBuffer.Add("你可以选择将此亡灵城与 " ).Add( squad.FleetMembership.Fleet.GetName(), "a11fa1").Add(" 在 " ).Add( squad.Planet.Name, "a35511").Add( " 进行交换。");


                    if ( GameSettings.Current.GetBoolBySetting( "UpgradeShipPrompt" ) )
                    {
                        tooltipBuffer.Add( "\n\n<color=#3f6c9e>按住 </color><color=#4486d1>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "SuppressTechUpgradePrompt" ) )
                            .Add( "</color> <color=#3f6c9e>可跳过特定入侵的'你确定吗'提示。</color>  " );
                    }

                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
                }
            }
        }
        #endregion
        #region bClose
        public class bClose : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "<color=#999999>关闭" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }
        }
        #endregion
    }
}
