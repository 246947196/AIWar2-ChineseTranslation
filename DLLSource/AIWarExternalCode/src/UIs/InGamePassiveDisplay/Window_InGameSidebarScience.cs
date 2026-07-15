using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Text;

namespace Arcen.AIW2.External
{
    public class Window_InGameSidebarScience : Window_InGameSidebarBase
    {
        public static Window_InGameSidebarScience Instance;
        public Window_InGameSidebarScience()
        {
            Instance = this;
            this.OnlyShowInGame = true;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            return Current == InGameSidebarType.Science;
        }

        private static ImageButtonAbstractBase.ImageButtonPool<btnScienceTech> btnScienceTechPool;
        private static readonly Dictionary<TechUpgrade, Sprite> TechIcons = Dictionary<TechUpgrade, Sprite>.Create_WillNeverBeGCed( 300, "Window_InGameSidebarScience-TechIcons" );
        public const string ScienceTooltipText = "花费<b>科学点</b>来解锁更高级的单位和建筑。主要通过占领新星球并守住一段时间来获取。" +
            "\n\n每个星系中科学家能学到的东西有限，所以你需要不断为他们寻找新的研究目标。" +
            "\n\n单一飞船类型可以被多个科技升级，所以请仔细考虑你的选择。" +
            "\n\n随着你捕获可以从中受益的飞船，更多科技将出现在科技侧边栏中。";

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                Window_InGameSidebarScience.CustomParentInstance = this;
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

                if ( Window_InGameSidebarScience.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( btnScienceTech.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnScienceTechPool = new ImageButtonAbstractBase.ImageButtonPool<btnScienceTech>( btnScienceTech.Original, 5 );

                            //Load tech images

                            List<TechUpgrade> upgrades = TechUpgradeTable.Instance.SortedTechUpgrades;
                            foreach ( TechUpgrade upgrade in upgrades )
                            {
                                if ( !string.IsNullOrWhiteSpace( upgrade.IconPath ) )
                                {
                                    //example path: "assets/icons/officialgui/science/weapon_melee.jpg"
                                    Sprite icon = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", upgrade.IconPath );
                                    TechIcons[upgrade] = icon; 
                                }
                            }
                        }
                    }
                    #endregion
                }

                float currentY = 0f; //the position of the first entry

                this.OnUpdateScience( ref currentY );                

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)btnScienceHeader.Instance.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }

            public const float TEXT_ROW_HEIGHTS = 25f;        

            #region OnUpdateScience
            public void OnUpdateScience( ref float currentY )
            {
                if ( !hasGlobalInitialized )
                    return;

                btnScienceTechPool.Clear( 5 );

                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return;

                List<TechUpgrade> upgrades = TechUpgradeTable.Instance.SortedTechUpgrades;
                int itemCost;
                ArcenRejectionReason rejection;
                int priorSortGroup = -1;
                bool rejectIfBenefitsNothing = localFaction.RejectTechUpgradesThatBenefitNothing_Safe();
                bool onlyShowIfBenefitsMe = localFaction.OnlyShowTechUpgradesThatBenefitMe_Safe();
                for ( int i = 0; i < upgrades.Count; i++ )
                {
                    TechUpgrade upgrade = upgrades[i];
                    
                    rejection = localFaction.GetCanUnlockTech( upgrade, rejectIfBenefitsNothing, out itemCost );
                    int upgradesSoFar = localFaction.TechUnlocks[upgrade.RowIndexNonSim] + localFaction.FreeTechUnlocks[upgrade.RowIndexNonSim];//okay to use here, as it will be consistent per run
                    TechBenefitLevel benefitLevel = TechBenefitLevel.StuffIHave;
                    switch ( rejection )
                    {
                        case ArcenRejectionReason.Unknown:
                            // Unknown is not rejected
                            break;
                        case ArcenRejectionReason.NoFleetsOfMineWouldUseThis:
                            if ( upgradesSoFar <= 0 ) //if we've already upgraded it, don't care anymore.
                            {
                                #region If none of my fleets would use this, check if it would benefit a ship I could capture
                                bool foundSomethingToBenefit = false;
                                foreach ( Fleet fleet in World_AIW2.Instance.Fleets( FleetStatus.AnyStatus ) )
                                {
                                    switch ( fleet.Category )
                                    {
                                        case FleetCategory.PlayerMobile:
                                        case FleetCategory.PlayerCustomCityFedMobile:
                                        case FleetCategory.PlayerCustomUnattachedMobile:
                                        case FleetCategory.PlayerBattlestation:
                                            break;
                                        default:
                                            continue;
                                    }
                                    if ( fleet.Faction == null || fleet.Faction.Type != FactionType.NaturalObject )
                                        continue;
                                    GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                                    if ( centerpiece == null || centerpiece.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                                        continue; //don't know about this yet!

                                    foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                                    {
                                        //if ( mem.EffectiveSquadCap <= 0 )
                                        //    continue;
                                        if ( mem.TypeData.TechUpgradesThatBenefitMe.Contains( upgrade ) )
                                        {
                                            foundSomethingToBenefit = true;
                                            break;
                                        }
                                    }
                                    if ( foundSomethingToBenefit )
                                        break;
                                }
                                #endregion
                                #region If I still haven't found anything that uses this tech, check in any visible ARSs
                                if ( !foundSomethingToBenefit )
                                {
                                    for ( int j = 0; j < World_AIW2.Instance.AIFactions.Count; j++ )
                                    {
                                        if ( foundSomethingToBenefit )
                                            break;
                                        Faction aiFaction = World_AIW2.Instance.AIFactions[j];
                                        foreach ( GameEntity_Squad entity in aiFaction.Squads( EntityRollupType.GrantsStuffToPlayers ) )
                                        {
                                            if ( entity == null )
                                                continue;
                                            Planet plan = entity.Planet;
                                            if ( plan == null )
                                                continue;
                                            if ( plan.IntelLevel <= PlanetIntelLevel.Unexplored )
                                                continue;
                                            ProtectedList<ShipLineEntry> shipGrants = entity.ShipGrantsList;
                                            if ( shipGrants == null )
                                                continue;
                                            for ( int k = 0; k < shipGrants.Count; k++ )
                                            {
                                                ShipLineEntry entry = shipGrants[k];
                                                if ( entry == null )
                                                    continue;
                                                if ( entry.TypeData.TechUpgradesThatBenefitMe.Contains( upgrade ) )
                                                {
                                                    foundSomethingToBenefit = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                                #endregion
                                if ( !foundSomethingToBenefit )
                                    benefitLevel = TechBenefitLevel.NothingICanSee;
                                else
                                    benefitLevel = TechBenefitLevel.StuffICanCapture;
                            }
                            break;
                        case ArcenRejectionReason.TechIsAlreadyFullyUnlocked:
                            // We still want to show the button, if the tech is fully unlocked.
                            break;
                        case ArcenRejectionReason.FactionIsWrongPlayerType:
                        case ArcenRejectionReason.TechIsNotForUnlockingBecauseInvisibleOnMenus:
                            continue;
                        case ArcenRejectionReason.FactionDoesNotHaveEnoughScience:
                            break; //this is fine.  Still show the button, etc.
                        case ArcenRejectionReason.FactionDoesNotHaveEnoughResourceOne:
                        case ArcenRejectionReason.FactionDoesNotHaveEnoughResourceTwo:
                        case ArcenRejectionReason.FactionDoesNotHaveEnoughResourceThree:
                            break; //this is fine.  Still show the button, etc.

                        default:
                            ArcenDebugging.ArcenDebugLog( "Unexpected rejection reason from Faction.CanUnlockTech in OnUpdateScience: " + rejection + " (Tech: " + upgrade.InternalName + ")", Verbosity.ShowAsError );
                            continue;
                    }
                    if ( onlyShowIfBenefitsMe &&
                         (benefitLevel == TechBenefitLevel.NothingICanSee || benefitLevel == TechBenefitLevel.StuffICanCapture) )
                        continue;

                    {
                        btnScienceTech item = btnScienceTechPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( item == null )
                            break; //time slicing, too many added right now
                        
                        Sprite icon;
                        TechIcons.TryGetValue( upgrade, out icon );
                        item.Assign( upgrade, itemCost, upgradesSoFar, rejection, icon, benefitLevel );
                        
                        if ( priorSortGroup != upgrade.SortGroup )
                        {
                            if ( priorSortGroup != -1 )
                                item.ExtraSpaceBeforeInAutoSizing = 5f;
                            priorSortGroup = upgrade.SortGroup;
                        }
                    }
                }

                Planet currentlyViewedPlanet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                Window_InGameSidebarFleets.RefillFleetsByPurpose( localFaction, currentlyViewedPlanet );

                //now do them for fleets
                for( FleetCategoryPurpose i = FleetCategoryPurpose.None; i < FleetCategoryPurpose.Length; i++ )
                {
                    bool isFirstInCategory = false;
                    List<Fleet> fleets = Window_InGameSidebarFleets.myFleetsByPurpose[i];
                    bool breakOuter = false;
                    //I guess we have something to draw, so let's do that!
                    if ( fleets.Count > 0 )
                    {
                        for ( int k = 0; k < fleets.Count; k++ )
                        {
                            Fleet fleet = fleets[k];
                            if ( !fleet.GetIsFleetToHaveScienceButton( localFaction ) )
                                continue;

                            btnScienceTech item = btnScienceTechPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                            if ( item == null )
                            {
                                breakOuter = true;
                                break; //time slicing, too many added right now
                            }

                            int scienceOrOtherResourceRequired = fleet.GetScienceOrOtherResourceNeededForNextLevelUp( fleet.AddedMarkLevelsForFleet_FromScience);
                            UpgradeResourceStyle resourceNeeded = fleet.GetResourceNeededForNextLevelUp();
                            switch ( resourceNeeded )
                            {
                                case UpgradeResourceStyle.Resource1:
                                    if ( scienceOrOtherResourceRequired > fleet.Faction.StoredFactionResourceOne )
                                        rejection = ArcenRejectionReason.FactionDoesNotHaveEnoughScience;
                                    else
                                        rejection = ArcenRejectionReason.Unknown;
                                    break;
                                case UpgradeResourceStyle.Resource2:
                                    if ( scienceOrOtherResourceRequired > fleet.Faction.StoredFactionResourceTwo )
                                        rejection = ArcenRejectionReason.FactionDoesNotHaveEnoughScience;
                                    else
                                        rejection = ArcenRejectionReason.Unknown;
                                    break;
                                default:
                                case UpgradeResourceStyle.Science:
                                    if ( scienceOrOtherResourceRequired > fleet.Faction.StoredScience )
                                        rejection = ArcenRejectionReason.FactionDoesNotHaveEnoughScience;
                                    else
                                        rejection = ArcenRejectionReason.Unknown;
                                    break;
                            }
                            item.Assign( fleet, i, rejection );
                            if ( isFirstInCategory )
                                item.ExtraSpaceBeforeInAutoSizing = 5f;
                        }
                    }
                    if ( breakOuter )
                        break;
                }

                {
                    //now do one for the re-spec option
                    btnScienceTech item = btnScienceTechPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item != null )
                    {
                        item.AssignAsTechRespecButton();
                        item.ExtraSpaceBeforeInAutoSizing = 5f;
                        item.ExtraSpaceAfterInAutoSizing = 5f;
                    }
                }

                #region Positioning Logic
                RectTransform rTran = null;
                {
                    rTran = btnScienceHeader.Instance.Element.RelevantRect;
                    rTran.anchoredPosition = new Vector2( 0, currentY );
                    currentY -= TEXT_ROW_HEIGHTS;
                    btnScienceTechPool.ApplyItemsInRows( 0, ref currentY, 32.1f, 180, 32f );
                }
                #endregion
            }
            #endregion
        }

        #region btnScienceHeader
        public class btnScienceHeader : ButtonAbstractBase
        {
            public static btnScienceHeader Instance;
            public btnScienceHeader() { if ( Instance == null ) Instance = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                //do something
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return;
                Buffer.AddNumberMoreReadable( localFaction.StoredScience.IntValue );
                localFaction.BaseInfo.WriteAddedScienceHeaderInfo( Buffer );
            }
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( Window_InGameSidebarScience.ScienceTooltipText, "GeneralTooltipScale" );
            }
        }
        #endregion
        
        #region btnScienceTech
        public class btnScienceTech : ImageButtonAbstractBase
        {
            public static btnScienceTech Original;
            public btnScienceTech() { if ( Original == null ) Original = this; }

            public TechUpgrade TechUpgrade;
            public int CostToUpgrade;
            public int UpgradesSoFar;
            public ArcenRejectionReason RejectionReason;
            public Sprite Icon;
            public TechBenefitLevel BenefitLevel;
            public bool IsTechRespecButton = false;
            public Fleet FleetToUpgrade = null;
            public FleetCategoryPurpose FleetPurpose = FleetCategoryPurpose.None;

            public void Assign( TechUpgrade Upgrade, int Cost, int Upgrades, ArcenRejectionReason rejectionReason, Sprite icon, TechBenefitLevel BenefitLevel )
            {
                this.TechUpgrade = Upgrade;
                this.CostToUpgrade = Cost;
                this.UpgradesSoFar = Upgrades;
                this.RejectionReason = rejectionReason;
                this.Icon = icon;
                this.BenefitLevel = BenefitLevel;
                this.FleetToUpgrade = null;
                this.FleetPurpose = FleetCategoryPurpose.None;
            }

            private static Sprite sprite_MobileOfficerFleetFlagship;
            private static Sprite sprite_MobileStrikeFleetFlagship;
            private static Sprite sprite_MobileSupportFleetFlagship;
            private static Sprite sprite_BattlestationBasic;
            private static Sprite sprite_BattlestationCitadel;
            private static Sprite sprite_CityCenter;
            private static Sprite sprite_PlanetCommand;

            private static Color fleetColor = ColorMath.HexToColor( "c5ffc1" );
            private static Color techColor = ColorMath.HexToColor( "c1faff" );

            private static bool hasInitialized = false;
            public static void InitIfNeeded()
            {
                if ( hasInitialized )
                    return;
                hasInitialized = true;
                sprite_MobileOfficerFleetFlagship = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/sci-fi_game_icons/icons/png/transparent/101_b.png" );
                sprite_MobileStrikeFleetFlagship = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/sci-fi_game_icons/icons/png/transparent/89_b.png" );
                sprite_MobileSupportFleetFlagship = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/sci-fi_game_icons/icons/png/transparent/59_b.png" );

                sprite_BattlestationBasic = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/sci-fi_game_icons/icons/png/transparent/4_b.png" );
                sprite_BattlestationCitadel = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/sci-fi_game_icons/icons/png/transparent/51_b.png" );

                sprite_CityCenter = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/sci-fi_game_icons/icons/png/transparent/48_b.png" );
                sprite_PlanetCommand = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/sci-fi_game_icons/icons/png/transparent/49_b.png" );
            }

            public void Assign( Fleet fleet, FleetCategoryPurpose purpose, ArcenRejectionReason rejectionReason )
            {
                InitIfNeeded();

                this.FleetToUpgrade = fleet;
                this.FleetPurpose = purpose;
                this.UpgradesSoFar = this.FleetToUpgrade.AddedMarkLevelsForFleet_FromScience;
                this.CostToUpgrade = this.FleetToUpgrade.GetScienceOrOtherResourceNeededForNextLevelUp( this.FleetToUpgrade.AddedMarkLevelsForFleet_FromScience );
                this.BenefitLevel = TechBenefitLevel.StuffIHave;
                switch ( this.FleetPurpose )
                {
                    case FleetCategoryPurpose.MobileOfficerFleetFlagship:
                        this.Icon = sprite_MobileOfficerFleetFlagship;
                        break;
                    case FleetCategoryPurpose.MobileStrikeFleetFlagship:
                        this.Icon = sprite_MobileStrikeFleetFlagship;
                        break;
                    case FleetCategoryPurpose.MobileSupportFleetFlagship:
                        this.Icon = sprite_MobileSupportFleetFlagship;
                        break;
                    case FleetCategoryPurpose.BattlestationBasic:
                        this.Icon = sprite_BattlestationBasic;
                        break;
                    case FleetCategoryPurpose.BattlestationCitadel:
                        this.Icon = sprite_BattlestationCitadel;
                        break;
                    case FleetCategoryPurpose.CityCenter:
                        this.Icon = sprite_CityCenter;
                        break;
                    case FleetCategoryPurpose.PlanetCommand:
                        this.Icon = sprite_PlanetCommand;
                        break;
                }

                if ( this.CostToUpgrade <= 0 )
                    this.RejectionReason = ArcenRejectionReason.TechIsAlreadyFullyUnlocked;
                else
                    this.RejectionReason = rejectionReason;
            }

            public void AssignAsTechRespecButton()
            {
                this.IsTechRespecButton = true;
                this.AlternativeHeightToUseInAutoSizing = 20f;
            }

            public override void Clear()
            {
                this.TechUpgrade = null;
                this.CostToUpgrade = -1;
                this.UpgradesSoFar = 0;
                this.RejectionReason = ArcenRejectionReason.Unknown;
                this.BenefitLevel = TechBenefitLevel.NothingICanSee;
                this.IsTechRespecButton = false;
                this.FleetToUpgrade = null;
                this.FleetPurpose = FleetCategoryPurpose.None;
            }

            public bool DebugUpdateContentFromVolatile = false;
            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                if ( this.IsTechRespecButton )
                {
                    SubImages[0].SetActiveIfNeeded( false );
                    SubTexts[0].Text.StartWritingToBuffer().Add( "<size=240%><voffset=-1em>取回已花费的科学点" );
                    SubTexts[0].Text.FinishWritingToBuffer();
                    SubTexts[1].Text.StartWritingToBuffer();
                    SubTexts[1].Text.FinishWritingToBuffer();
                    SubTexts[2].Text.StartWritingToBuffer();
                    SubTexts[2].Text.FinishWritingToBuffer();
                    return;
                }
                if ( this.FleetToUpgrade != null )
                {
                    this.UpdateContentFromVolatile_FleetUpgrade( Image, SubImages, SubTexts );
                    return;
                }

                this.UpdateContentFromVolatile_TechUpgrade( Image, SubImages, SubTexts );
            }

            #region UpdateContentFromVolatile_TechUpgrade
            private void UpdateContentFromVolatile_TechUpgrade( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            { 
                if ( this.TechUpgrade == null )
                    return;
                {
                    for ( int sti = 0; sti < 3; sti++ )
                    {
                        TMPro.TextMeshProUGUI tmp = SubTexts[sti].ReferenceText;
                        if ( tmp != null )
                        {
                            tmp.enableAutoSizing = false;
                            tmp.fontSize = 9f;
                        }
                    }
                    int debugStage = 0;
                    try
                    {
                        //UnityEngine.Debug.Log( ( this.TechType == null ? "null" : this.TechType.InternalName ) + "   " + ( typeData == null ? "null" : typeData.InternalName ) +
                        //    "    " + ( SubTexts[0].Text.GO ? this.Element.ElementName : "nullGO" ),
                        //    SubTexts[0].Text.GO );

                        debugStage = 5;

                        {
                            debugStage = 14;
                            //MainText
                            int shipStrengthIncrease = this.TechUpgrade.UIOnly_Tech_UpgradedShipStrengthIncrease_FromOneMarkLevel;
                            int defenseStrengthIncrease = this.TechUpgrade.UIOnly_Tech_UpgradedDefenseStrengthIncrease_FromOneMarkLevel;
                            if ( shipStrengthIncrease > 0 && shipStrengthIncrease < 1000 )
                                shipStrengthIncrease = 1000;
                            if ( defenseStrengthIncrease > 0 && defenseStrengthIncrease < 1000 )
                                defenseStrengthIncrease = 1000;

                            if ( defenseStrengthIncrease > 0 || shipStrengthIncrease > 0 )
                            {
                                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                                buffer.StartColor( this.BenefitLevel.GetColor() ).Add( this.TechUpgrade.DisplayName ).EndColor();
                                if ( shipStrengthIncrease > 0 )
                                {
                                    buffer.Add( "<pos=65%>" ).Add( ArcenExternalUIUtilities.GUI_StrengthTextIcon_AndShipLineIncreaseColor )
                                    .StartColor( ArcenExternalUIUtilities.ShipLineIncreaseColor ).AddStrengthTiered( shipStrengthIncrease ).EndColor().Add( "</pos>" );
                                }
                                if ( defenseStrengthIncrease > 0 )
                                {
                                    buffer.Add( "<pos=85%>" )
                                    .StartColor( ArcenExternalUIUtilities.DefenseLineIncreaseColor ).Add( ArcenExternalUIUtilities.GUI_StrengthTextIcon_AndDefenseLineIncreaseColor ).AddStrengthTiered( defenseStrengthIncrease ).EndColor().Add( "</pos>" );
                                }
                            }
                            else
                                SubTexts[0].Text.StartWritingToBuffer().StartColor( this.BenefitLevel.GetColor() ).Add( this.TechUpgrade.DisplayName ).EndColor();
                            debugStage = 15;
                            SubTexts[0].Text.FinishWritingToBuffer();
                        }

                        {
                            debugStage = 21;
                            Balance_MarkLevel markByOrdinal = Balance_MarkLevelTable.Instance.RowsByOrdinal[this.UpgradesSoFar < 0 ? 0 : this.UpgradesSoFar + 1];

                            List<int> upgradeCosts = this.TechUpgrade.GetScienceOrOtherResourceCostsPerTimeUnlocked();

                            //MarkText
                            bool showExtraUpgradables = (this.TechUpgrade.UIOnly_Tech_ShipLinesAffected + this.TechUpgrade.UIOnly_Tech_DefensiveLinesAffected > 0);
                            if ( showExtraUpgradables )
                            {
                                ArcenDoubleCharacterBuffer buffer = SubTexts[1].Text.StartWritingToBuffer();
                                buffer.Add( markByOrdinal.ColorHexStart ).Add( "等级 " ).Add( this.UpgradesSoFar
                                    ).Add( "/" ).Add( upgradeCosts.Count ).Add( "</color>" ).Add( "\t"
                                    ).Add( FontSizes.MUCH_SMALLER_SIZE_PLUS_A_TAD_STRING );
                                buffer.Add( "<pos=65%><color=#" )
                                    .Add( ArcenExternalUIUtilities.ShipLineIncreaseColor ).Add( ">" );
                                if ( this.TechUpgrade.UIOnly_Tech_ShipLinesAffected > 0 )
                                    buffer.Add( this.TechUpgrade.UIOnly_Tech_ShipLinesAffected );
                                else
                                    buffer.Add( " " );
                                buffer.Add( "</color><color=#" ).Add( ArcenExternalUIUtilities.DefenseLineIncreaseColor ).Add( "> " );
                                if ( this.TechUpgrade.UIOnly_Tech_DefensiveLinesAffected > 0 )
                                    buffer.Add( this.TechUpgrade.UIOnly_Tech_DefensiveLinesAffected );
                                else
                                    buffer.Add( " " );
                                buffer.Add("</color></pos></size>");
                            }
                            else
                                SubTexts[1].Text.StartWritingToBuffer().Add( markByOrdinal.ColorHexStart ).Add( "等级 " ).Add(
                                  this.UpgradesSoFar ).Add( "/" ).Add( upgradeCosts.Count ).Add("</color>");
                            debugStage = 22;
                            SubTexts[1].Text.FinishWritingToBuffer();
                        }

                        if ( this.RejectionReason == ArcenRejectionReason.TechIsAlreadyFullyUnlocked )
                        {
                            debugStage = 31;
                            //Cost Text
                            SubTexts[2].Text.StartWritingToBuffer().StartColor( Color.gray ).Add( "已完成" );
                            debugStage = 32;
                            SubTexts[2].Text.FinishWritingToBuffer();
                        }
                        else
                        {
                            debugStage = 31;

                            bool shouldShowAsRed = false;
                            switch ( this.RejectionReason )
                            {
                                case ArcenRejectionReason.Unknown:
                                //case ArcenRejectionReason.NoFleetsOfMineWouldUseThis:
                                    break;
                                default:
                                    shouldShowAsRed = true;
                                    break;
                            }

                            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                            if ( playerFaction == null )
                                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();

                            string resourceIconAndColor = this.TechUpgrade.GetResourceTextColorAndIconNeededForNextLevelUp( playerFaction );

                            //Cost Text
                            SubTexts[2].Text.StartWritingToBuffer().StartColor( shouldShowAsRed ? ColorMath.LightRed : Color.white ).Add( "费用：<size=70%>" ).Add( resourceIconAndColor ).Add( "</size>" ).AddNumberMoreReadable( this.CostToUpgrade );
                            debugStage = 32;
                            SubTexts[2].Text.FinishWritingToBuffer();
                        }

                        {
                            debugStage = 40;
                            var subImage = SubImages[0];
                            subImage.SetActiveIfNeeded( true );
                            subImage.SetSpriteIfNeeded( Icon );
                            subImage.SetColorIfNeeded( techColor );
                        }

                        debugStage = 90;
                    }
                    catch ( Exception e )
                    {
                        if ( DebugUpdateContentFromVolatile )
                            ArcenDebugging.ArcenDebugLog( "Exception in btnScienceTech.UpdateContentFromVolatile_TechUpgrade at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                    }
                }
            }
            #endregion

            #region UpdateContentFromVolatile_FleetUpgrade
            private void UpdateContentFromVolatile_FleetUpgrade( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                if ( this.FleetToUpgrade == null )
                    return;
                {
                    for ( int sti = 0; sti < 3; sti++ )
                    {
                        TMPro.TextMeshProUGUI tmp = SubTexts[sti].ReferenceText;
                        if ( tmp != null )
                        {
                            tmp.enableAutoSizing = false;
                            tmp.fontSize = 9f;
                        }
                    }
                    int debugStage = 0;
                    try
                    {
                        //UnityEngine.Debug.Log( ( this.TechType == null ? "null" : this.TechType.InternalName ) + "   " + ( typeData == null ? "null" : typeData.InternalName ) +
                        //    "    " + ( SubTexts[0].Text.GO ? this.Element.ElementName : "nullGO" ),
                        //    SubTexts[0].Text.GO );

                        debugStage = 5;

                        {
                            debugStage = 14;
                            //MainText
                            int shipStrengthIncrease = this.FleetToUpgrade.UIOnly_Fleet_UpgradedShipStrengthIncrease_FromOneMarkLevel;
                            int defenseStrengthIncrease = this.FleetToUpgrade.UIOnly_Fleet_UpgradedDefenseStrengthIncrease_FromOneMarkLevel;
                            if ( shipStrengthIncrease > 0 && shipStrengthIncrease < 1000 )
                                shipStrengthIncrease = 1000;
                            if ( defenseStrengthIncrease > 0 && defenseStrengthIncrease < 1000 )
                                defenseStrengthIncrease = 1000;

                            if ( defenseStrengthIncrease > 0 || shipStrengthIncrease > 0 )
                            {
                                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                                buffer.StartColor( this.BenefitLevel.GetColor() ).Add( this.FleetToUpgrade.GetName() ).EndColor();
                                if ( shipStrengthIncrease > 0 )
                                {
                                    buffer.Add( "<pos=65%>" ).Add( ArcenExternalUIUtilities.GUI_StrengthTextIcon_AndShipLineIncreaseColor )
                                    .StartColor( ArcenExternalUIUtilities.ShipLineIncreaseColor ).AddStrengthTiered( shipStrengthIncrease ).EndColor().Add( "</pos>" );
                                }
                                if ( defenseStrengthIncrease > 0 )
                                {
                                    buffer.Add( "<pos=85%>" )
                                    .StartColor( ArcenExternalUIUtilities.DefenseLineIncreaseColor ).Add( ArcenExternalUIUtilities.GUI_StrengthTextIcon_AndDefenseLineIncreaseColor ).AddStrengthTiered( defenseStrengthIncrease ).EndColor().Add( "</pos>" );
                                }
                            }
                            else
                                SubTexts[0].Text.StartWritingToBuffer().StartColor( this.BenefitLevel.GetColor() ).Add( this.FleetToUpgrade.GetName() ).EndColor();
                            debugStage = 15;
                            SubTexts[0].Text.FinishWritingToBuffer();
                        }

                        {
                            debugStage = 21;
                            Balance_MarkLevel markByOrdinal = Balance_MarkLevelTable.Instance.RowsByOrdinal[this.UpgradesSoFar < 0 ? 0 : this.UpgradesSoFar + 1];

                            //MarkText
                            bool showExtraUpgradables = (this.FleetToUpgrade.UIOnly_Fleet_ShipLinesAffected + this.FleetToUpgrade.UIOnly_Fleet_DefensiveLinesAffected > 0);
                            if ( showExtraUpgradables )
                            {
                                ArcenDoubleCharacterBuffer buffer = SubTexts[1].Text.StartWritingToBuffer();
                                buffer.Add( markByOrdinal.ColorHexStart ).Add( "等级 " ).Add( this.UpgradesSoFar
                                    ).Add( "/6</color>" ).Add( "\t"
                                    ).Add( FontSizes.MUCH_SMALLER_SIZE_PLUS_A_TAD_STRING );
                                buffer.Add( "<pos=65%><color=#" )
                                    .Add( ArcenExternalUIUtilities.ShipLineIncreaseColor ).Add( ">" );
                                if ( this.FleetToUpgrade.UIOnly_Fleet_ShipLinesAffected > 0 )
                                    buffer.Add( this.FleetToUpgrade.UIOnly_Fleet_ShipLinesAffected );
                                else
                                    buffer.Add( " " );
                                buffer.Add( "</color><color=#" ).Add( ArcenExternalUIUtilities.DefenseLineIncreaseColor ).Add( "> " );
                                if ( this.FleetToUpgrade.UIOnly_Fleet_DefensiveLinesAffected > 0 )
                                    buffer.Add( this.FleetToUpgrade.UIOnly_Fleet_DefensiveLinesAffected );
                                else
                                    buffer.Add( " " );
                                buffer.Add( "</color></pos></size>" );
                            }
                            else
                                SubTexts[1].Text.StartWritingToBuffer().Add( markByOrdinal.ColorHexStart ).Add( "等级 " ).Add(
                                  this.UpgradesSoFar ).Add( "/6</color>" );
                            debugStage = 22;
                            SubTexts[1].Text.FinishWritingToBuffer();
                        }

                        if ( this.RejectionReason == ArcenRejectionReason.TechIsAlreadyFullyUnlocked )
                        {
                            debugStage = 31;
                            //Cost Text
                            SubTexts[2].Text.StartWritingToBuffer().StartColor( Color.gray ).Add( "已完成" );
                            debugStage = 32;
                            SubTexts[2].Text.FinishWritingToBuffer();
                        }
                        else
                        {
                            debugStage = 31;

                            bool shouldShowAsRed = false;
                            switch ( this.RejectionReason )
                            {
                                case ArcenRejectionReason.Unknown:
                                    //case ArcenRejectionReason.NoFleetsOfMineWouldUseThis:
                                    break;
                                default:
                                    shouldShowAsRed = true;
                                    break;
                            }

                            string resourceIconAndColor = this.FleetToUpgrade.GetResourceTextColorAndIconNeededForNextLevelUp();

                            //Cost Text
                            SubTexts[2].Text.StartWritingToBuffer().StartColor( shouldShowAsRed ? ColorMath.LightRed : Color.white ).Add( "费用：<size=70%>" ).Add( resourceIconAndColor ).Add( "</size>" ).AddNumberMoreReadable( this.CostToUpgrade );
                            debugStage = 32;
                            SubTexts[2].Text.FinishWritingToBuffer();
                        }

                        {
                            debugStage = 40;
                            var subImage = SubImages[0];
                            subImage.SetActiveIfNeeded( true );
                            subImage.SetSpriteIfNeeded( Icon );
                            subImage.SetColorIfNeeded( fleetColor );
                        }

                        debugStage = 90;
                    }
                    catch ( Exception e )
                    {
                        if ( DebugUpdateContentFromVolatile )
                            ArcenDebugging.ArcenDebugLog( "Exception in btnScienceTech.UpdateContentFromVolatile_FleetUpgrade at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                    }
                }
            }
            #endregion

            private readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_InGameSidebarScience-tooltipBuffer" );

            private static readonly SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark> techUpgrade_Ships_ThatYouHave = 
                SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark>.Create_WillNeverBeGCed( 30, delegate { return new ShipDataByMark(); }, "Window_InGameSidebarScience-techUpgrade_Ships_ThatYouHave" );
            private static TechUpgrade techUpgrade_Ships_ThatYouHave_LastTech = null;
            private static float techUpgrade_Ships_ThatYouHave_NextTime = 0;

            private static readonly SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataForSingleMark> techUpgrade_Ships_ThatYouCanCapture = 
                SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataForSingleMark>.Create_WillNeverBeGCed( 30, delegate { return new ShipDataForSingleMark(); }, "Window_InGameSidebarScience-techUpgrade_Ships_ThatYouCanCapture" );
            
            public override void HandleMouseover()
            {
                if ( this.IsTechRespecButton )
                {
                    Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( 
                        "允许你取回'已花费'的科学点，代价是'遗忘'中央科技或基于舰队的升级。", "ShipTooltipScale" );
                    return;
                }

                if ( this.FleetToUpgrade != null )
                {
                    tooltipBuffer.Clear();
                    World_AIW2.Instance.FocusedSquadForMapDarkening = this.FleetToUpgrade.Centerpiece.GetSquad();
                    Window_FleetManagementSidebarPopout.bFleetScienceLevel.FillFleetUpgradeTooltipInfo( this.FleetToUpgrade, tooltipBuffer, 
                        this.FleetToUpgrade.AddedMarkLevelsForFleet_FromScience, 0 );
                    Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
                    return;
                }

                tooltipBuffer.Clear();
                WriteTechTooltip( this.TechUpgrade, this.UpgradesSoFar, this.CostToUpgrade, -1, true, tooltipBuffer );

                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
            }

            #region WriteTechTooltip
            public static void WriteTechTooltip( TechUpgrade TechUpgrade, int UpgradesSoFar, int CostToUpgrade, int AmountToRefund, bool IncludeShipPromptInfo, ArcenCharacterBufferBase tooltipBuffer )
            { 
                int debugStage = 1;
                try
                {
                    List<int> upgradeCosts = TechUpgrade.GetScienceOrOtherResourceCostsPerTimeUnlocked();
                    Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                    if ( playerFaction == null )
                        playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();

                    string resourceName = TechUpgrade.GetResourceNameNeededForNextLevelUp( playerFaction );

                    debugStage = 100;
                    Balance_MarkLevel markByOrdinal = Balance_MarkLevelTable.Instance.RowsByOrdinal[UpgradesSoFar < 0 ? 0 : UpgradesSoFar];
                    debugStage = 200;
                    Balance_MarkLevel maxMark = Balance_MarkLevelTable.Instance.RowsByOrdinal[upgradeCosts.Count < 0 ? 0 : upgradeCosts.Count];
                    
                    debugStage = 300;
                    tooltipBuffer.Add( "<b>科技：" ).Add( TechUpgrade.DisplayName ).Add( "</b>\n" );
                    if ( TechUpgrade.Description.Length > 0 )
                        tooltipBuffer.Add( "<size=90%>" ).Add( TechUpgrade.Description ).Add( "</size>\n" );
                    
                    debugStage = 400;
                    tooltipBuffer.Add( "已升级 " ).Add( UpgradesSoFar, markByOrdinal.ColorHex ).Add( " 次，最多 " ).Add( upgradeCosts.Count, maxMark.ColorHex ).Add( " 次。\n" );
                    
                    debugStage = 500;
                    if ( CostToUpgrade > 0 )
                    {
                        if ( CostToUpgrade >= 99999 )
                        {
                            tooltipBuffer.Add( "无法再进一步升级。\n" );
                            return; //nothing more to say
                        }
                        else
                        {
                            tooltipBuffer.Add( "下次升级将花费 " ).Add( CostToUpgrade, "7CE9FF" ).Add( " " ).Add( resourceName ).Add( "。\n" );
                        }
                    }
                    if ( AmountToRefund > 0 )
                    {
                        tooltipBuffer.Add( "将返还 " ).Add( AmountToRefund, "7CE9FF" ).Add( " " ).Add( resourceName ).Add( "，并将此科技重置为0次升级。\n" );
                    }

                    debugStage = 1000;

                    if ( techUpgrade_Ships_ThatYouHave_LastTech != TechUpgrade )
                    {
                        techUpgrade_Ships_ThatYouHave_LastTech = TechUpgrade;
                        techUpgrade_Ships_ThatYouHave_NextTime = 0;
                    }

                    bool doCalculationsThisFrame = false;
                    if ( ArcenTime.TimeSinceStartF >= techUpgrade_Ships_ThatYouHave_NextTime )
                    {
                        techUpgrade_Ships_ThatYouHave_NextTime = ArcenTime.TimeSinceStartF + Engine_Universal.PermanentQualityRandom.NextFloat( 0.8f, 1.2f );
                        doCalculationsThisFrame = true;
                        ShipListerUtils.CalculateShipsThatBenefit_ThatYouHave( TechUpgrade, techUpgrade_Ships_ThatYouHave, true );
                    }

                    #region shipsThatBenefit_ThatYouHave
                    debugStage = 1100;
                    GameEntityTypeData ship;
                    bool isFirst = true;
                    foreach ( KeyValuePair<GameEntityTypeData, ShipDataByMark> pair in techUpgrade_Ships_ThatYouHave )
                    {
                        debugStage = 1300;
                        if ( pair.Key.IsTurret || !pair.Key.IsMobile || pair.Key.FleetMembershipStyle == FleetMembershipStyle.Planetary )
                            continue; //defenses, so skip!

                        ship = pair.Key;
                        if ( isFirst )
                        {
                            if ( CostToUpgrade > 0 )
                                tooltipBuffer.Add( "\n<b><u><size=110%>将提升你拥有的这些飞船的等级（+1）：</b></u></size>\n" );
                            if ( AmountToRefund > 0 )
                                tooltipBuffer.Add( "\n<b><u><size=110%>将降低你拥有的这些飞船的等级（-" ).Add( UpgradesSoFar ).Add( "）：</b></u></size>\n" );
                            isFirst = false;
                        }
                        else
                            tooltipBuffer.Add( ", " );
                        debugStage = 1400;
                        bool showIcons = InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1();
                        ShipListerUtils.WriteCountsAndStrengthsForEachMarkLevelFrom_ForTechUpgradeOrDowngrade( ship, pair.Value, tooltipBuffer,
                                                                                                               AmountToRefund > 0 ? -UpgradesSoFar : 1, playerFaction, showIcons );
                    }
                    debugStage = 1500;
                    int shipStrengthIncrease = 0; 
                    if ( AmountToRefund > 0 )
                        shipStrengthIncrease = ( TechUpgrade.UIOnly_Tech_DowngradedShipStrengthIncrease_FromClearingMarkLevels);
                    else
                        shipStrengthIncrease = (TechUpgrade.UIOnly_Tech_UpgradedShipStrengthIncrease_FromOneMarkLevel);
                    int defenseStrengthIncrease = 0;
                    if ( AmountToRefund > 0 )
                        defenseStrengthIncrease = (TechUpgrade.UIOnly_Tech_DowngradedDefenseStrengthIncrease_FromClearingMarkLevels);
                    else
                        defenseStrengthIncrease = (TechUpgrade.UIOnly_Tech_UpgradedDefenseStrengthIncrease_FromOneMarkLevel);
                    if ( shipStrengthIncrease > 0 && shipStrengthIncrease < 1000 )
                        shipStrengthIncrease = 1000;
                    if ( defenseStrengthIncrease > 0 && defenseStrengthIncrease < 1000 )
                        defenseStrengthIncrease = 1000;
                    debugStage = 1600;
                    if ( shipStrengthIncrease != 0 )
                    {
                        tooltipBuffer.Add( shipStrengthIncrease < 0 ? "\n所有 " : "\n所有 " )
                            .Add( "飞船", ArcenExternalUIUtilities.ShipLineIncreaseColor ).Add( " 的总强度变化约为 " )
                            .Add( ArcenExternalUIUtilities.GUI_StrengthTextIcon_AndShipLineIncreaseColor ).StartColor( ArcenExternalUIUtilities.ShipLineIncreaseColor )
                            .AddStrengthTiered( shipStrengthIncrease ).EndColor().Add( "。共 " );
                        if ( TechUpgrade.UIOnly_Tech_ShipLinesAffected == 1 )
                            tooltipBuffer.Add( TechUpgrade.UIOnly_Tech_ShipLinesAffected, ArcenExternalUIUtilities.ShipLineIncreaseColor ).Add( " 条受影响。" );
                        else if ( TechUpgrade.UIOnly_Tech_ShipLinesAffected > 1 )
                            tooltipBuffer.Add( TechUpgrade.UIOnly_Tech_ShipLinesAffected, ArcenExternalUIUtilities.ShipLineIncreaseColor ).Add( " 条受影响。" );
                    }

                    debugStage = 1610;

                    bool hadAnyOfTheGroupAbove = !isFirst;
                    
                    isFirst = true;
                    foreach ( KeyValuePair<GameEntityTypeData, ShipDataByMark> pair in techUpgrade_Ships_ThatYouHave )
                    {
                        debugStage = 1620;
                        if ( pair.Key.IsTurret || !pair.Key.IsMobile || pair.Key.FleetMembershipStyle == FleetMembershipStyle.Planetary )
                        { } //defenses, so do it!
                        else
                            continue; //skip the ships we already did

                        ship = pair.Key;
                        if ( isFirst )
                        {
                            if ( hadAnyOfTheGroupAbove )
                                tooltipBuffer.Add( "\n" );
                            if ( CostToUpgrade > 0 )
                            {
                                tooltipBuffer.Add( "\n<b><u><size=110%>将提升你拥有的这些防御设施的等级（+1）：</b></u></size>\n" );
                            }
                            if ( AmountToRefund > 0 )
                                tooltipBuffer.Add( "\n<b><u><size=110%>将降低你拥有的这些防御设施的等级（-" ).Add( UpgradesSoFar ).Add( "）：</b></u></size>\n" );
                            isFirst = false;
                        }
                        else
                            tooltipBuffer.Add( ", " );
                        debugStage = 1650;
                        bool showIcons = InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1();
                        ShipListerUtils.WriteCountsAndStrengthsForEachMarkLevelFrom_ForTechUpgradeOrDowngrade( ship, pair.Value, tooltipBuffer,
                                        AmountToRefund > 0 ? -UpgradesSoFar : 1, playerFaction, showIcons );
                    }

                    debugStage = 1700;
                    if ( defenseStrengthIncrease != 0 )
                    {
                        tooltipBuffer.Add( defenseStrengthIncrease < 0 ? "\n所有 " : "\n所有 " )
                            .Add( "防御设施", ArcenExternalUIUtilities.DefenseLineIncreaseColor ).Add( " 的总强度变化约为 " )
                            .Add( ArcenExternalUIUtilities.GUI_StrengthTextIcon_AndDefenseLineIncreaseColor ).StartColor( ArcenExternalUIUtilities.DefenseLineIncreaseColor )
                            .AddStrengthTiered( defenseStrengthIncrease ).EndColor().Add( "。共 " );
                        if ( TechUpgrade.UIOnly_Tech_DefensiveLinesAffected == 1 )
                            tooltipBuffer.Add( TechUpgrade.UIOnly_Tech_DefensiveLinesAffected, ArcenExternalUIUtilities.DefenseLineIncreaseColor ).Add( " 类受影响。" );
                        else if ( TechUpgrade.UIOnly_Tech_DefensiveLinesAffected > 1 )
                            tooltipBuffer.Add( TechUpgrade.UIOnly_Tech_DefensiveLinesAffected, ArcenExternalUIUtilities.DefenseLineIncreaseColor ).Add( " 类受影响。" );

                    }
                    tooltipBuffer.Add( "\n" );
                    #endregion

                    debugStage = 2000;

                    #region shipsThatBenefit_ThatYouCanCapture
                    if ( doCalculationsThisFrame )
                    {
                        //don't exclude ship types that we already have, since knowing that there's more to capture of that sort is still useful!
                        ShipListerUtils.CalculateShipsThatBenefit_ThatYouCanCapture( TechUpgrade, techUpgrade_Ships_ThatYouCanCapture, null, true );
                    }

                    debugStage = 2100;
                    if ( techUpgrade_Ships_ThatYouCanCapture.GetCountOfLists() > 0 )
                        tooltipBuffer.Add( "\n<b><u><size=110%>将提升这 " + techUpgrade_Ships_ThatYouCanCapture.GetCountOfLists() + " 种可捕获单位的等级（+1）：</b></u></size>\n" );
                    debugStage = 2200;
                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    debugStage = 2400;
                    isFirst = true;
                    foreach ( KeyValuePair<GameEntityTypeData,ShipDataForSingleMark> kv in techUpgrade_Ships_ThatYouCanCapture )
                    {
                        debugStage = 2500;
                        ship = kv.Key;
                        if ( isFirst )
                            isFirst = false;
                        else
                            tooltipBuffer.Add( ", " );
                        debugStage = 2600;
                        bool showIcons = InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1();
                        ShipListerUtils.WriteCountsAndStrengthsForEachMarkLevelFrom_ForTechUpgradeOrDowngrade_DataSingleMark( kv.Value, ship, tooltipBuffer,
                            AmountToRefund > 0 ? -UpgradesSoFar : 1, showIcons );
                    }
                    #endregion
                    
                    if ( InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1() )
                    {
                        //print costs for all the science costs for this upgrade
                        for ( int i = 0; i < upgradeCosts.Count; i++ )
                        {
                            int mark = i + 2;
                            Balance_MarkLevel markByOrdinalForUI = Balance_MarkLevelTable.Instance.RowsByOrdinal[mark];
                            if ( i == 0 )
                                tooltipBuffer.Add("\n");
                            tooltipBuffer.Add("费用：").Add(markByOrdinalForUI.ColorHexStart).Add( "等级 " + mark  ).Add( "</color>: ").Add( upgradeCosts[i]).Add(" ").Add(ArcenExternalUIUtilities.ScienceTextColorAndIcon).Add("\n");
                        }
                    }
                    debugStage = 3000;
                    {
                        tooltipBuffer.Add( "\n" ).Add( FontSizes.MUCH_SMALLER_SIZE_PLUS_A_TAD_STRING );
                        tooltipBuffer.Add( "<color=#3f6c9e>按住 </color><color=#4486d1>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "HoldAndClickToViewDetailsOfContents" ) )
                                .Add( "</color> <color=#3f6c9e>并点击此处查看此科技升级的所有飞船类型详情。</color>  " );
                        debugStage = 3100;
                        if ( IncludeShipPromptInfo && GameSettings.Current.GetBoolBySetting( "UpgradeShipPrompt" ) )
                        {
                            debugStage = 3200;
                            tooltipBuffer.Add( "<color=#3f6c9e>按住 </color><color=#4486d1>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "SuppressTechUpgradePrompt" ) )
                                .Add( "</color> <color=#3f6c9e>可跳过科技升级的'你确定吗'提示。</color>  " );
                        }
                        tooltipBuffer.Add( "<color=#3f6c9e>按住 <color=#4486d1>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key1" ) ).Add( "</color> 查看此科技的所有科技费用和飞船图标。</color>\n" );
                        tooltipBuffer.Add( "</size>" );
                    }

                    debugStage = 4000;
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Tech mouseover error at debugStage " + debugStage + ". Error: " + e, Verbosity.ShowAsError );
                }
            }
            #endregion

            public void UnlockTech()
            {
                if ( this.TechUpgrade == null )
                    return;
                Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                GameCommand command = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.UnlockTech], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer);
                command.RelatedFactionIndex = localFaction.FactionIndex;
                command.RelatedString = this.TechUpgrade.InternalName;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true);
            }
            public override MouseHandlingResult HandleClick(MouseHandlingInput input)
            {
                if ( this.IsTechRespecButton )
                {
                    Window_TechRefunds.Instance.Open();
                    return MouseHandlingResult.None;
                }
                if ( this.FleetToUpgrade != null )
                {
                    return Window_FleetManagementSidebarPopout.bFleetScienceLevel.UpgradeFleet( this.FleetToUpgrade );
                }

                if ( this.TechUpgrade == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                #region instead of normal click behavior, show details
                if ( InputCaching.CalculateHoldAndClickToViewDetailsOfContents() )
                {
                    ShowDetailsOfATechUpgradeContents( this.TechUpgrade, this.CostToUpgrade, 0, this.UpgradesSoFar );
                    return MouseHandlingResult.None;
                }
                #endregion

                Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                int costToUpgrade;
                bool rejectIfBenefitsNothing = localFaction.RejectTechUpgradesThatBenefitNothing_Safe();

                ArcenRejectionReason rejectionReason = localFaction.GetCanUnlockTech( this.TechUpgrade, rejectIfBenefitsNothing, out costToUpgrade );
                //if ( rejectionReason != ArcenRejectionReason.Unknown )
                //{
                //    switch ( rejectionReason )
                //    {
                //        case ArcenRejectionReason.NoFleetsOfMineWouldUseThis:
                //            rejectionReason = ArcenRejectionReason.Unknown; //allow unlocking
                //            break;
                //    }
                //}
                if ( costToUpgrade > 0 )
                { }
                if ( rejectionReason != ArcenRejectionReason.Unknown )
                {
                    switch ( rejectionReason )
                    {
                        case ArcenRejectionReason.NoFleetsOfMineWouldUseThis:
                            World_AIW2.Instance.QueueChatMessageOrCommand( "无法解锁科技 " + this.TechUpgrade.DisplayName +
                                "，因为你没有任何可以从中实际受益的飞船！", ChatType.ShowLocallyOnly, null );
                            break;
                        case ArcenRejectionReason.TechIsAlreadyFullyUnlocked:
                            World_AIW2.Instance.QueueChatMessageOrCommand( "无法解锁科技 " + this.TechUpgrade.DisplayName +
                                "，因为你已经完全解锁了它！", ChatType.ShowLocallyOnly, null );
                            break;
                        case ArcenRejectionReason.FactionDoesNotHaveEnoughScience:
                            World_AIW2.Instance.QueueChatMessageOrCommand( "无法解锁科技 " + this.TechUpgrade.DisplayName +
                                "，因为你只有 " + localFaction.StoredScience.IntValue + "，需要 " + costToUpgrade + " 科学点！", ChatType.ShowLocallyOnly, null );
                            break;
                        case ArcenRejectionReason.FactionDoesNotHaveEnoughResourceOne:
                            {
                                string resourceName = TechUpgrade.GetResourceNameNeededForNextLevelUp( localFaction );
                                World_AIW2.Instance.QueueChatMessageOrCommand( "无法解锁科技 " + this.TechUpgrade.DisplayName +
                                    "，因为你只有 " + localFaction.StoredFactionResourceOne.IntValue + "，需要 " + costToUpgrade + " " + resourceName + "！", ChatType.ShowLocallyOnly, null );
                            }
                            break;
                        case ArcenRejectionReason.FactionDoesNotHaveEnoughResourceTwo:
                            {
                                string resourceName = TechUpgrade.GetResourceNameNeededForNextLevelUp( localFaction );
                                World_AIW2.Instance.QueueChatMessageOrCommand( "无法解锁科技 " + this.TechUpgrade.DisplayName +
                                "，因为你只有 " + localFaction.StoredFactionResourceTwo.IntValue + "，需要 " + costToUpgrade + " " + resourceName + "！", ChatType.ShowLocallyOnly, null );
                            }
                            break;
                        case ArcenRejectionReason.FactionDoesNotHaveEnoughResourceThree:
                            {
                                string resourceName = TechUpgrade.GetResourceNameNeededForNextLevelUp( localFaction );
                                World_AIW2.Instance.QueueChatMessageOrCommand( "无法解锁科技 " + this.TechUpgrade.DisplayName +
                                "，因为你只有 " + localFaction.StoredFactionResourceThree.IntValue + "，需要 " + costToUpgrade + " " + resourceName + "！", ChatType.ShowLocallyOnly, null );
                            }
                            break;
                        default:
                            World_AIW2.Instance.QueueChatMessageOrCommand( "无法解锁科技 " + this.TechUpgrade.DisplayName +
                                "，原因：" + rejectionReason + "（请告知我们需要改进此原因文本，以及原因文本是什么）", ChatType.ShowLocallyOnly, null );
                            break;
                    }
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                bool UpgradeShips = GameSettings.Current.GetBoolBySetting( "UpgradeShipPrompt" ) && !InputCaching.CalculateHoldAndSuppressTechUpgradePrompt();
                if ( UpgradeShips )
                {
                    ModalPopupData.CreateAndLogYesNoStyle( UnlockTech, null, "确认", "你确定要升级 " + this.TechUpgrade.DisplayName + " 吗？\n \n" + "<color=#888888>要禁用此提示，请进入游戏设置，在游戏标签页下将其切换为关闭。或者在点击升级按钮时按住 " + 
                        InputActionTypeDataTable.GetActionByName_FairlySlow( "SuppressTechUpgradePrompt" ).GetHumanReadableKeyCombo() + " 来跳过一次。</color>", "是，升级", "否，不升级" );
                }
                else
                    UnlockTech();
                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                return this.TechUpgrade == null && !this.IsTechRespecButton && this.FleetToUpgrade == null;
            }

            private static readonly Regex diffSplitRegex = new Regex( @"(<.+?>)|( )", RegexOptions.Compiled | RegexOptions.Singleline );

            #region WriteDetailsOfATechContents
            public static bool WriteDetailsOfATechContents( ArcenDoubleCharacterBuffer buffer, TechUpgrade tech, int CostToUpgrade, int AmountToRefund, int UpgradesSoFar, float PositionScaleMultiplier )
            {
                EntityText.Write_Tooltip_Hotkeys_Footer( buffer, false, true, null );
                buffer.Add( "\n\n" );

                List<int> upgradeCosts = tech.GetScienceOrOtherResourceCostsPerTimeUnlocked();
                Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( playerFaction == null )
                    playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();

                string resourceName = tech.GetResourceNameNeededForNextLevelUp( playerFaction );

                bool isComplete = CostToUpgrade >= 99999 && AmountToRefund <= 0;

                buffer.Add( "<u>科技信息：</u>\n" );
                buffer.Add( "<b>科技：" ).Add( tech.DisplayName ).Add( "</b>\n" );
                buffer.Add( "已升级 " ).Add( UpgradesSoFar ).Add( " 次，最多 " ).Add( upgradeCosts.Count ).Add( " 次。\n" );
                if ( AmountToRefund > 0 )
                    buffer.Add( "将返还 " ).Add( AmountToRefund ).Add( " " ).Add( resourceName ).Add( " 以降级回零。\n" );
                else
                {
                    if ( isComplete )
                    {
                        buffer.Add( "无法再进一步升级。\n" );
                        return true;
                    }
                    else
                        buffer.Add( "下次升级将花费 " ).Add( CostToUpgrade ).Add( " " ).Add( resourceName ).Add( "。\n" );
                }

                if ( AmountToRefund > 0 )
                {
                    buffer.Add( "\n<color=#27f985>（当前等级的数值在括号内。）</color>\n" );
                }
                else
                {
                    buffer.Add( "\n<color=#27f985>（前一等级的数值在括号内，新增内容以<i>斜体</i>显示。）</color>\n" );
                }

                Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                PlayerTypeData playerType = localFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
                #region shipsThatBenefit_ThatYouHave;
                if ( techUpgrade_Ships_ThatYouHave_LastTech != tech )
                {
                    techUpgrade_Ships_ThatYouHave_LastTech = tech;
                    techUpgrade_Ships_ThatYouHave_NextTime = 0;
                }

                bool doCalculationsThisFrame = false;
                if ( ArcenTime.TimeSinceStartF >= techUpgrade_Ships_ThatYouHave_NextTime )
                {
                    techUpgrade_Ships_ThatYouHave_NextTime = ArcenTime.TimeSinceStartF + Engine_Universal.PermanentQualityRandom.NextFloat( 0.8f, 1.2f );
                    doCalculationsThisFrame = true;
                    ShipListerUtils.CalculateShipsThatBenefit_ThatYouHave( tech, techUpgrade_Ships_ThatYouHave, true );
                }

                #region Ships That We Have
                GameEntityTypeData ship;
                bool isFirst = true;
                foreach ( KeyValuePair<GameEntityTypeData, ShipDataByMark> pair in techUpgrade_Ships_ThatYouHave )
                {
                    if ( pair.Key.IsTurret || !pair.Key.IsMobile || pair.Key.FleetMembershipStyle == FleetMembershipStyle.Planetary )
                        continue; //defenses, so skip!

                    ship = pair.Key;
                    if ( isFirst )
                    {
                        if ( CostToUpgrade > 0 )
                            buffer.Add( "\n<b><u><size=120%>将提升你拥有的这些飞船的等级（+1）：</b></u></size>\n" );
                        if ( AmountToRefund > 0 )
                            buffer.Add( "\n<b><u><size=120%>将降低你拥有的这些飞船的等级（-" ).Add( UpgradesSoFar ).Add( "）：</b></u></size>\n" );
                        isFirst = false;
                    }

                    for( byte mark = 0; mark < pair.Value.CountsByMarkLength(); mark++ )
                    {
                        int count = pair.Value.GetCountByMark( mark );
                        if ( count <= 0 )
                            continue;

                        if ( pair.Value.TypeData != pair.Key )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Incorrect mark mapping D! : " + pair.Value.TypeData.InternalName + " vs " + pair.Key.InternalName, Verbosity.ShowAsError );

                        GameEntityTypeData.MarkLevelStats oldStats = pair.Key.MarkStatsFor( mark );
                        GameEntityTypeData.MarkLevelStats newStats = ship.MarkStatsFor( (byte)(mark + (AmountToRefund > 0 ? -UpgradesSoFar : 1)) );

                        int oldShipCount = count;
                        int newShipCount = ship.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouDoNOTKnowBaseCap( ship, oldShipCount, false, oldStats.MarkLevel, newStats.MarkLevel );
                        if ( playerType.MarkLevelIncreaseDoesNotIncreaseShipCap )
                            newShipCount = oldShipCount;

                        Fleet fleetToUse = pair.Value.FleetToUse;
                        Faction factionToUse = fleetToUse != null ? fleetToUse.Faction : null;
                        if ( factionToUse == null )
                            factionToUse = localFaction;

                        if ( factionToUse.Type != FactionType.Player )
                            newShipCount = oldShipCount;

                        ArcenCharacterBuffer tempBuffer1 = ArcenCharacterBuffer.GetFromPoolOrCreate( "Window_InGameSidebarScience-tempBuffer1" );
                        EntityText.GetTooltip( tempBuffer1, null, null,
                            ship, fleetToUse, factionToUse.FactionCenterColor.ColorHexBrighter, string.Empty,
                            oldShipCount, factionToUse, mark, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None, PositionScaleMultiplier, true );
                        string oldText = tempBuffer1.ToStringAndReturnToPool();
                        //ArcenDebugging.ArcenDebugLogSingleLine( "OLD TEXT:" + Environment.NewLine + oldText, Verbosity.Chat );
                        System.Collections.Generic.List<string> oldTokens = diffSplitRegex.Split( oldText ).ToList();

                        ArcenCharacterBuffer tempBuffer2 = ArcenCharacterBuffer.GetFromPoolOrCreate( "Window_InGameSidebarScience-tempBuffer1" );
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

                #region Defenses That We Have
                isFirst = true;
                foreach ( KeyValuePair<GameEntityTypeData, ShipDataByMark> pair in techUpgrade_Ships_ThatYouHave )
                {
                    if ( pair.Key.IsTurret || !pair.Key.IsMobile || pair.Key.FleetMembershipStyle == FleetMembershipStyle.Planetary )
                    { } //defenses, so do it!
                    else
                        continue; //skip the ships we already did

                    ship = pair.Key;
                    if ( isFirst )
                    {
                        if ( CostToUpgrade > 0 )
                        {
                            buffer.Add( "\n<b><u><size=120%>将提升你拥有的这些防御设施的等级（+1）：</b></u></size>\n" );
                        }
                        if ( AmountToRefund > 0 )
                            buffer.Add( "\n<b><u><size=120%>将降低你拥有的这些防御设施的等级（-" ).Add( UpgradesSoFar ).Add( "）：</b></u></size>\n" );
                        isFirst = false;
                    }

                    for ( byte mark = 0; mark < pair.Value.CountsByMarkLength(); mark++ )
                    {
                        int count = pair.Value.GetCountByMark( mark );
                        if ( count <= 0 )
                            continue;

                        if ( pair.Value.TypeData != pair.Key )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Incorrect mark mapping E! : " + pair.Value.TypeData.InternalName + " vs " + pair.Key.InternalName, Verbosity.ShowAsError );

                        GameEntityTypeData.MarkLevelStats oldStats = pair.Key.MarkStatsFor( mark );
                        GameEntityTypeData.MarkLevelStats newStats = ship.MarkStatsFor( (byte)(mark + (AmountToRefund > 0 ? -UpgradesSoFar : 1)) );

                        int oldShipCount = count;
                        int newShipCount = ship.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouDoNOTKnowBaseCap( ship, oldShipCount, false, oldStats.MarkLevel, newStats.MarkLevel );

                        Fleet fleetToUse = pair.Value.FleetToUse;
                        Faction factionToUse = fleetToUse != null ? fleetToUse.Faction : null;
                        if ( factionToUse == null )
                            factionToUse = localFaction;

                        if ( factionToUse.Type != FactionType.Player )
                            newShipCount = oldShipCount;

                        ArcenCharacterBuffer tempBuffer3 = ArcenCharacterBuffer.GetFromPoolOrCreate( "Window_InGameSidebarScience-tempBuffer3" );
                        EntityText.GetTooltip( tempBuffer3, null, null,
                            ship, fleetToUse, factionToUse.FactionCenterColor.ColorHexBrighter, string.Empty,
                            oldShipCount, factionToUse, mark, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None, PositionScaleMultiplier, true );
                        string oldText = tempBuffer3.ToStringAndReturnToPool();
                        //ArcenDebugging.ArcenDebugLogSingleLine( "OLD TEXT:" + Environment.NewLine + oldText, Verbosity.Chat );
                        System.Collections.Generic.List<string> oldTokens = diffSplitRegex.Split( oldText ).ToList();

                        ArcenCharacterBuffer tempBuffer4 = ArcenCharacterBuffer.GetFromPoolOrCreate( "Window_InGameSidebarScience-tempBuffer4" );
                        EntityText.GetTooltip( tempBuffer4, null, null,
                            ship, fleetToUse, factionToUse.FactionCenterColor.ColorHexBrighter, string.Empty,
                            newShipCount, factionToUse, newStats.MarkLevel.Ordinal, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None, PositionScaleMultiplier, true );
                        string newText = tempBuffer4.ToStringAndReturnToPool();
                        //ArcenDebugging.ArcenDebugLogSingleLine( "NEW TEXT:" + Environment.NewLine + newText, Verbosity.Chat );
                        System.Collections.Generic.List<string> newTokens = diffSplitRegex.Split( newText ).ToList();

                        string finalText = GenerateDiff( oldTokens, newTokens );
                        //ArcenDebugging.ArcenDebugLogSingleLine( "FINAL TEXT: " + Environment.NewLine + finalText.ToString(), Verbosity.Chat );
                        buffer.Add( finalText );
                        buffer.Add( "\n\n" );
                    }
                }
                #endregion
                #endregion

                #region shipsThatBenefit_ThatYouCanCapture
                if ( doCalculationsThisFrame )
                {
                    ShipListerUtils.CalculateShipsThatBenefit_ThatYouCanCapture( tech, techUpgrade_Ships_ThatYouCanCapture, null, true );
                }

                if ( techUpgrade_Ships_ThatYouCanCapture.GetCountOfLists() > 0 )
                {
                    if ( AmountToRefund > 0 )
                        buffer.Add( "\n<b><u><size=120%>将降低这些可捕获单位的等级（-" ).Add( UpgradesSoFar ).Add( "）：</b></u></size>\n" );
                    else
                        buffer.Add( "\n<b><u><size=120%>将提升这些可捕获单位的等级（+1）：</b></u></size>\n" );
                }

                foreach ( KeyValuePair<GameEntityTypeData,ShipDataForSingleMark> kv in techUpgrade_Ships_ThatYouCanCapture )
                {
                    ship = kv.Key;
                    byte oldMark = localFaction.GetGlobalMarkLevelForShipLine( ship );
                    int baseCount = kv.Value.GetCountToCapture();
                    int oldCap = ship.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( ship, baseCount, baseCount, oldMark );

                    ArcenCharacterBuffer tempBuffer5 = ArcenCharacterBuffer.GetFromPoolOrCreate( "Window_InGameSidebarScience-tempBuffer5" );
                    EntityText.GetTooltip( tempBuffer5, null, null,
                        ship, null, "ffffff", " which is Capturable",
                        oldCap, localFaction, oldMark, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None, PositionScaleMultiplier, true );
                    string oldText = tempBuffer5.ToStringAndReturnToPool();
                    System.Collections.Generic.List<string> oldTokens = diffSplitRegex.Split( oldText ).ToList();

                    GameEntityTypeData.MarkLevelStats newStats = ship.MarkStatsFor( (byte)(oldMark + (AmountToRefund > 0 ? -UpgradesSoFar : 1)) );
                    
                    int newShipCount = ship.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( ship, baseCount, oldCap, newStats.MarkLevel );

                    ArcenCharacterBuffer tempBuffer6 = ArcenCharacterBuffer.GetFromPoolOrCreate( "Window_InGameSidebarScience-tempBuffer6" );
                    EntityText.GetTooltip( tempBuffer6, null, null,
                        ship, null, "ffffff", " which is Capturable",
                        newShipCount, localFaction, (byte)(oldMark + (AmountToRefund > 0 ? -UpgradesSoFar : 1)), FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None, PositionScaleMultiplier, true );
                    string newText = tempBuffer6.ToStringAndReturnToPool();
                    System.Collections.Generic.List<string> newTokens = diffSplitRegex.Split( newText ).ToList();

                    string finalText = GenerateDiff( oldTokens, newTokens );
                    buffer.Add( finalText );
                    buffer.Add( "\n\n" );
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

                DifferController.GenerateDiff( text1, text2, delegate( DiffDataForSection section )
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

            public static void ShowDetailsOfATechUpgradeContents( TechUpgrade tech, int CostToUpgrade, int AmountToRefund, int UpgradesSoFar )
            {
                if ( tech == null )
                    return;

                float centerPopupScale = GameSettings.Current.GetFloatBySetting( "CentralPopupTextScale" );
                Window_ModalSelfUpdatingTextWindow_Wide.Instance.Open( 0.25f, 2f, 
                    AmountToRefund > 0 ? "科技退款后的飞船详情" : "科技升级的飞船详情", "关闭",
                    delegate ( ArcenDoubleCharacterBuffer Buffer ) { return WriteDetailsOfATechContents( Buffer, tech, CostToUpgrade, AmountToRefund, UpgradesSoFar, centerPopupScale ); } );
            }
        }
        #endregion
    }

    public enum TechBenefitLevel
    {
        StuffIHave,
        StuffICanCapture,
        NothingICanSee
    }

    public static class TechBenefitLevelExtensionMethods
    {
        public static Color GetColor( this TechBenefitLevel Tech )
        {
            switch ( Tech )
            {
                case TechBenefitLevel.NothingICanSee:
                    return ColorMath.Gray;
                case TechBenefitLevel.StuffICanCapture:
                    return ColorMath.LightGray;
                default:
                    return ColorMath.White;
            }
        }
    }
}
