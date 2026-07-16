using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_TechRefunds : Window_DynamicallyFilledAbstractBase
    {
        public static Window_TechRefunds Instance;
        
        private static readonly Dictionary<TechUpgrade,bool> _updatesToRefund = Dictionary<TechUpgrade, bool>.Create_WillNeverBeGCed( 90, "Window_TechRefunds-updatesToRefund" );
        private static readonly Dictionary<Fleet, bool> _fleetsToRefund = Dictionary<Fleet, bool>.Create_WillNeverBeGCed( 90, "Window_TechRefunds-fleetsToRefund" );
        
        private bool _open;
        
        public Window_TechRefunds()
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
        
        public void Open()
        {
            _open = true;
            _updatesToRefund.Clear();
            _fleetsToRefund.Clear();
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

        #endregion
        
        #region bCancel
        public class bCancel : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }
        }
        #endregion

        public static int GetHackingPointCost()
        {
            Faction localFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFactionOrNull != null && NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( localFactionOrNull ) )
            {
                //necromancer has different respec costs due to their different resource structure
                return (_updatesToRefund.Count + _fleetsToRefund.Count) * 
                    HackingTypeTable.Instance.GetRowByName( "RetrieveSpentScience_Necromancer" ).GetHackPointCostForTarget( null ).GetNearestIntPreferringHigher();
            }
            return (_updatesToRefund.Count + _fleetsToRefund.Count) * 
                HackingTypeTable.Instance.GetRowByName( "RetrieveSpentScience" ).GetHackPointCostForTarget( null ).GetNearestIntPreferringHigher();
        }

        public static int GetScienceReturned()
        {
            int spentScienceToGetBack = 0;
            Faction localFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFactionOrNull != null )
            {
                foreach ( KeyValuePair<TechUpgrade, bool> kv in _updatesToRefund )
                {
                    spentScienceToGetBack += localFactionOrNull.RefundTechIfPossible( kv.Key, true );
                }
                foreach ( KeyValuePair<Fleet, bool> kv in _fleetsToRefund )
                {
                    spentScienceToGetBack += localFactionOrNull.RefundFleetIfPossible( kv.Key, true );
                }
            }
            return spentScienceToGetBack;
        }

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "<size=95%>取回 " ).AddNumberMoreReadable( GetScienceReturned() ).Add( " 已花费的科技点？</size>");
            }
        }
        #endregion

        #region bOk
        public class bOk : ButtonAbstractBase
        {
            public static bOk OkInstance;
            public bOk()
            {
                OkInstance = this;
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Faction localFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( (_updatesToRefund.Count + _fleetsToRefund.Count) == 0 )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "未选择任何项目", "要执行此操作，你必须选择至少一项科技或舰队来取回科技点。", "确定" );
                    return MouseHandlingResult.None;
                }
                int hackCost = GetHackingPointCost();
                if ( hackCost > localFactionOrNull.StoredHacking )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "入侵点不足！", "你的阵营没有足够的入侵点来执行此操作。", "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                string itemStr = "个项目";
                if ( _updatesToRefund.Count + _fleetsToRefund.Count == 1 )
                    itemStr = "个项目";

                ModalPopupData.CreateAndLogYesNoStyle( delegate
                {
                    if ( localFactionOrNull == null )
                        return;
                    if ( hackCost > localFactionOrNull.StoredHacking )
                    {
                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "入侵点不足！", "你的阵营没有足够的入侵点来执行此操作。", "确定" );
                        return;
                    }
                    foreach ( KeyValuePair<TechUpgrade, bool> kv in _updatesToRefund )
                    {
                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.RetrieveSpentScience], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                        command.RelatedString = kv.Key.InternalName;
                        command.RelatedIntegers.Add( localFactionOrNull.FactionIndex );
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                    }
                    foreach ( KeyValuePair<Fleet, bool> kv in _fleetsToRefund )
                    {
                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.RetrieveSpentScience], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                        command.RelatedIntegers2.Add( kv.Key.FleetID );
                        command.RelatedIntegers.Add( localFactionOrNull.FactionIndex );
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                    }

                    Instance.Close();
                }, null, "确认", "你确定要退还 <color=#a1ffa1>" + (_updatesToRefund.Count + _fleetsToRefund.Count ) + "</color> " + itemStr + "，花费 <color=#3DE799>" +
                     ArcenExternalUIUtilities.HackingTextColorAndIcon + GetHackingPointCost() + "</color> 入侵点？你将获得 <color=#7CE9FF>" + ArcenExternalUIUtilities.ScienceTextColorAndIcon + GetScienceReturned().ToString( "#,##0" ) + "</color> " +
                     "科技点以重新分配。\n\n请注意，已花费的入侵点无法退还。", "是，退还", "返回" );

                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( (_updatesToRefund.Count + _fleetsToRefund.Count) == 0 )
                {
                    Buffer.StartColor( "777777" ).Add( "确定" );
                }
                else
                {
                    //Buffer.Add( "<align=left>" );
                    //Buffer.Add( "<pos=5>" );
                    Buffer.Add( "      确定" );
                    Buffer.StartColor( ArcenExternalUIUtilities.HackingTextColor );
                    Buffer.Add( "   <size=70%><voffset=0.2em>（花费：" );
                    Buffer.Add( ArcenExternalUIUtilities.HackingTextColorAndIcon );
                    Buffer.Add( GetHackingPointCost() );
                    Buffer.Add( ")" );
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
                if ( bOk.OkInstance != null && bOk.OkInstance.Element != null )
                    ( bOk.OkInstance.Element as ArcenUI_Button).UseGetTextToShowFromAnyThread = true;
            }
        }

        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_TechRefunds.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                    }
                    #endregion
                }

                this.WindowController.myXPositionScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" );
                this.WindowController.myScale = GameSettings.Current.GetFloatBySetting( "NotificationsScale" );

                //if you change away from the techs tab, close the fleet management popout
                if ( Window_InGameSidebarBase.Current != InGameSidebarType.Science ||
                    Engine_Universal.RunStatus == RunStatus.GameStart )
                {
                    Instance.Close();
                    return;
                }
            }
        }

        private float fullWidth = 600;

        private ArcenCachedExternalTypeDirect type_bReadExplanation = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bReadExplanation ) );
        private ArcenCachedExternalTypeDirect type_bTechForRefund = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bTechForRefund ) );
        private ArcenCachedExternalTypeDirect type_bNoTechsAtTheMoment = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bNoTechsAtTheMoment ) );
        private ArcenCachedExternalTypeDirect type_bFleetForRefund = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bFleetForRefund ) );
        private ArcenCachedExternalTypeDirect type_bNoFleetsAtTheMoment = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bNoFleetsAtTheMoment ) );

        public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            if ( bMainContentParent.ParentT == null )
                return;
            this.Window.SetOverridingTransformToWhichToAddChildren( bMainContentParent.ParentT );

            Faction localFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();

            float runningY = topBuffer;

            Rect leftBounds;
            
            #region Top Line (bReadExplanation) *****************************************
            {
                this.rowHeight = ROW_HEIGHT_DEFAULT;
                this.CalculateBoundsSingle( out leftBounds, ref runningY, fullWidth );
                AddButton( Set, type_bReadExplanation, string.Empty, -1, -1, leftBounds, -1f );
            }
            #endregion *****************************************                

            #region Lines For Techs
            int techsAdded = 0;
            foreach ( TechUpgrade tech in TechUpgradeTable.Instance.SortedTechUpgrades )
            {
                int upgradesSoFar = 0;
                if ( localFactionOrNull != null )
                    upgradesSoFar = localFactionOrNull.TechUnlocks[tech.RowIndexNonSim];
                if ( upgradesSoFar == 0 )
                    continue; //only show ones we have already upgraded 

                int amountToRefund = localFactionOrNull.RefundTechIfPossible( tech, true );
                if ( amountToRefund <= 0 )
                    continue;
                techsAdded++;

                #region (bTechForRefund) *****************************************
                this.rowHeight = ROW_HEIGHT_DEFAULT;
                this.CalculateBoundsSingle( out leftBounds, ref runningY, fullWidth );
                AddButton( Set, type_bTechForRefund, tech.InternalName, -1, amountToRefund, leftBounds, -1f );
                #endregion *****************************************
            }
            if ( techsAdded == 0  )
            {
                this.rowHeight = ROW_HEIGHT_DEFAULT;
                this.CalculateBoundsSingle( out leftBounds, ref runningY, fullWidth );
                AddButton( Set, type_bNoTechsAtTheMoment, string.Empty, -1, -1, leftBounds, -1f );
            }
            #endregion

            #region Lines For Fleets
            Window_InGameSidebarFleets.RefillFleetsByPurpose( localFactionOrNull, null );

            int fleetsAdded = 0;
            for ( FleetCategoryPurpose purpose = FleetCategoryPurpose.None; purpose < FleetCategoryPurpose.Length; purpose++ )
            {
                List<Fleet> fleetList = Window_InGameSidebarFleets.myFleetsByPurpose[purpose];
                if ( fleetList.Count <= 0 )
                    continue;
                //bool isFirstOfNewFleetPurpose = true;
                foreach ( Fleet fleet in fleetList )
                {
                    if ( fleet.AddedMarkLevelsForFleet_FromScience <= 0 )
                        continue; //if nothing to refund, don't show it to me!

                    int amountToRefund = localFactionOrNull.RefundFleetIfPossible( fleet, true );
                    if ( amountToRefund <= 0 )
                        continue; //if we get nothing back, don't show it.

                    fleetsAdded++;
                    //if ( isFirstOfNewFleetPurpose )
                    //{
                    //    runningY += 5f;
                    //    isFirstOfNewFleetPurpose = false;
                    //}

                    #region (bFleetForRefund) *****************************************
                    this.rowHeight = ROW_HEIGHT_DEFAULT;
                    this.CalculateBoundsSingle( out leftBounds, ref runningY, fullWidth );

                    string fleetPrefix = string.Empty;
                    switch (purpose)
                    {
                        case FleetCategoryPurpose.BattlestationBasic:
                            fleetPrefix = "<color=#ff903e>战斗站：</color> ";
                            break;
                        case FleetCategoryPurpose.BattlestationCitadel:
                            fleetPrefix = "<color=#e63eff>堡垒：</color> ";
                            break;
                        case FleetCategoryPurpose.CityCenter:
                            fleetPrefix = "<color=#ff3eab>城市中心：</color> ";
                            break;
                        case FleetCategoryPurpose.PlanetCommand:
                            fleetPrefix = "<color=#7afc46>指挥站：</color> ";
                            break;
                        case FleetCategoryPurpose.MobileOfficerFleetFlagship:
                            fleetPrefix = "<color=#53d9ff>军官舰队：</color> ";
                            break;
                        case FleetCategoryPurpose.MobileStrikeFleetFlagship:
                            fleetPrefix = "<color=#ffd053>打击舰队：</color> ";
                            break;
                        case FleetCategoryPurpose.MobileSupportFleetFlagship:
                            fleetPrefix = "<color=#edff53>支援舰队：</color> ";
                            break;
                    }

                    AddButton( Set, type_bFleetForRefund, fleetPrefix, fleet.FleetID, amountToRefund, leftBounds, -1f );
                    #endregion *****************************************
                }
            }
            if ( fleetsAdded == 0 )
            {
                this.rowHeight = ROW_HEIGHT_DEFAULT;
                this.CalculateBoundsSingle( out leftBounds, ref runningY, fullWidth );
                AddButton( Set, type_bNoFleetsAtTheMoment, string.Empty, -1, -1, leftBounds, -1f );
            }
            #endregion

            bMainContentParent.ParentRT.UI_SetHeight( runningY );
        }

        #region GetFleetFromElement
        public static Fleet GetFleetFromElement( ArcenUI_Element element )
        {
            if ( element.CreatedByCodeDirective == null )
                return null;
            int fleetID = element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            return World_AIW2.Instance.GetFleetByID( fleetID );
        }
        #endregion

        #region GetFleetPrefixCurrentlyInElement
        public static string GetFleetPrefixCurrentlyInElement( ArcenUI_Element element )
        {
            if ( element.CreatedByCodeDirective == null )
                return string.Empty;
            string fleetPrefix = element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString;
            return fleetPrefix;
        }
        #endregion

        #region GetTechFromElement
        public static TechUpgrade GetTechFromElement( ArcenUI_Element element )
        {
            if ( element.CreatedByCodeDirective == null )
                return null;
            string techName = element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString;
            return TechUpgradeTable.Instance.GetRowByNameOrNullIfNotFound( techName );
        }
        #endregion

        #region GetScienceAmountCurrentlyInElement
        public static int GetScienceAmountCurrentlyInElement( ArcenUI_Element element )
        {
            if ( element.CreatedByCodeDirective == null )
                return 0;
            int scienceAmount = element.CreatedByCodeDirective.Identifier.CodeDirectiveTag2;
            return scienceAmount;
        }
        #endregion

        #region bTechForRefund
        public class bTechForRefund : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                TechUpgrade upgrade = GetTechFromElement( this.Element );
                int scienceRefund = GetScienceAmountCurrentlyInElement( this.Element );
                if ( upgrade == null )
                {
                    Buffer.Add( "Null TechUpgrade...?" );
                    return;
                }
                Buffer.Add("<align=left>");
                Buffer.Add("<pos=5>");
                Buffer.Add( upgrade.DisplayName );
                Buffer.Add( "<pos=300>已投入：" );
                Buffer.AddNumberMoreReadable( scienceRefund );

                Faction localFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                int upgradesSoFar = 0;
                if ( localFactionOrNull != null )
                    upgradesSoFar = localFactionOrNull.TechUnlocks[upgrade.RowIndexNonSim];

                if ( _updatesToRefund.ContainsKey( upgrade ) )
                    Buffer.Add( "<pos=500><color=#fff94e>退还 " ).Add( upgradesSoFar ).Add( "</color>" );
                else
                {
                    Buffer.Add( "<pos=500><color=#6abac6>保留 " ).Add( upgradesSoFar ).Add( "</color>" );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                TechUpgrade upgrade = GetTechFromElement( this.Element );
                if ( upgrade == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                #region instead of normal click behavior, show details
                if ( InputCaching.CalculateHoldAndClickToViewDetailsOfContents() )
                {
                    int scienceRefund = GetScienceAmountCurrentlyInElement( this.Element );
                    if ( scienceRefund <= 0 )
                        scienceRefund = 1;

                    Faction localFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                    int upgradesSoFar = 0;
                    if ( localFactionOrNull != null )
                        upgradesSoFar = localFactionOrNull.TechUnlocks[upgrade.RowIndexNonSim];

                    Window_InGameSidebarScience.btnScienceTech.ShowDetailsOfATechUpgradeContents( upgrade, 0, scienceRefund, upgradesSoFar );
                    return MouseHandlingResult.None;
                }
                #endregion

                if ( _updatesToRefund.ContainsKey( upgrade ) )
                    _updatesToRefund.Remove( upgrade );
                else
                    _updatesToRefund[upgrade] = true;

                return MouseHandlingResult.None;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_TechRefunds-bTechForRefund-tooltipBuffer" );
            public override void HandleMouseover()
            {
                TechUpgrade upgrade = GetTechFromElement( this.Element );
                int scienceRefund = GetScienceAmountCurrentlyInElement( this.Element );

                if ( upgrade == null )
                {
                    Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "未选择要退还的升级！", "ShipTooltipScale" );
                    return;
                }

                Faction localFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                int upgradesSoFar = 0;
                if ( localFactionOrNull != null )
                    upgradesSoFar = localFactionOrNull.TechUnlocks[upgrade.RowIndexNonSim];

                Window_InGameSidebarScience.btnScienceTech.WriteTechTooltip( upgrade, upgradesSoFar, -1, scienceRefund, false, tooltipBuffer );

                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
            }
        }
        #endregion

        #region bFleetForRefund
        public class bFleetForRefund : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Fleet fleet = GetFleetFromElement( this.Element );
                int scienceRefund = GetScienceAmountCurrentlyInElement( this.Element );
                if ( fleet == null )
                {
                    Buffer.Add( "Null Fleet...?" );
                    return;
                }
                string fleetPrefix = GetFleetPrefixCurrentlyInElement( this.Element );
                
                Buffer.Add( "<align=left>" );
                Buffer.Add( "<pos=5>" );
                Buffer.Add( fleetPrefix );
                Buffer.Add( fleet.GetName() );
                Buffer.Add( "<pos=300>已投入：" );
                Buffer.AddNumberMoreReadable( scienceRefund );

                int upgradesSoFar = fleet.AddedMarkLevelsForFleet_FromScience;

                if ( _fleetsToRefund.ContainsKey( fleet ) )
                    Buffer.Add( "<pos=500><color=#fff94e>退还 " ).Add( upgradesSoFar ).Add( "</color>" );
                else
                {
                    Buffer.Add( "<pos=500><color=#6abac6>保留 " ).Add( upgradesSoFar ).Add( "</color>" );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Fleet fleet = GetFleetFromElement( this.Element );
                if ( fleet == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                #region instead of normal click behavior, show details
                if ( InputCaching.CalculateHoldAndClickToViewDetailsOfContents() )
                {
                    int scienceRefund = GetScienceAmountCurrentlyInElement( this.Element );
                    if ( scienceRefund <= 0 )
                        scienceRefund = 1;

                    int upgradesSoFar = fleet.AddedMarkLevelsForFleet_FromScience;

                    Window_FleetManagementSidebarPopout.bFleetScienceLevel.ShowDetailsOfAFleetUpgradeContents( fleet, 0, scienceRefund, upgradesSoFar );

                    return MouseHandlingResult.None;
                }
                #endregion

                if ( _fleetsToRefund.ContainsKey( fleet ) )
                    _fleetsToRefund.Remove( fleet );
                else
                    _fleetsToRefund[fleet] = true;

                return MouseHandlingResult.None;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_TechRefunds-bFleetForRefund-tooltipBuffer" );
            public override void HandleMouseover()
            {
                Fleet fleet = GetFleetFromElement( this.Element );
                int scienceRefund = GetScienceAmountCurrentlyInElement( this.Element );

                if ( fleet == null )
                {
                    Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "未选择要退还的舰队！", "ShipTooltipScale" );
                    return;
                }

                int upgradesSoFar = fleet.AddedMarkLevelsForFleet_FromScience;

                Window_FleetManagementSidebarPopout.bFleetScienceLevel.FillFleetUpgradeTooltipInfo( fleet, tooltipBuffer, upgradesSoFar, scienceRefund );

                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
            }
        }
        #endregion

        #region bReadExplanation
        public class bReadExplanation : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "<align=left>" );
                Buffer.Add( "<pos=5>" );
                Buffer.StartColor( "aaaaaa" );
                Buffer.Add( "请解释一下" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.TallWide, null, "这些'科技点'到底是什么？",
                    "我们知道你已经开始好奇这一切是如何运作的了，指挥官。我们特意简化了你的指挥界面的部分内容，使其易于理解。但如果你对技术细节感兴趣，我们很乐意分享。" +
                    "\n\n我们单位的自动建造是我们缺少活着且服从（更不用说称职）的人类的必要副产品。此外，只有机器运营的工厂才能跟上我们现在面对的势力。这与大多数小型单位的半自动远程驾驶背后的原理相同。" +
                    "\n\n我们在尽可能使用人脑，但可用的数量有限。我们制造中心的主机受到非常严格的监控，以确保它至少比产生意识低一个数量级。如果我们在这场战争中幸存下来，我们预计大多数社会都会采用这个合理的限度，因为我们现在大致知道意识的界限在哪里。" +
                    "\n\n然而，即使是这也是一种过度简化。'主机'是一个必要的概念，但也是一个脆弱点。更不用说出于后勤原因，我们需要在整个被称为银河系的虫洞网络中以超光速能力提供信息。当我们'收集科技'时，我们实际上是在扩展网格处理网络的处理能力。这是一种分布式的主机形式，我们已经了解了在一个行星系统中安全运行节点数量的限度：大约2,000个。随着网格计算能力的增加，我们能够为我们档案中的各种单位蓝图构建和维护更复杂的材料和武器。" +
                    "\n\n然而，以有效方式应用这些并不只是简单地更换文件中的材料。这实际上是一个极其耗费处理器的任务。通过将网格节点从一组蓝图——一种技术——上释放出来，我们可以将其算力重新定向到其他地方。" +
                    "\n\n关于网格计算网络，我们在一个系统中安全运行节点数量的考虑之一是，我们能以绝对没有被敌方势力检测到或被敌方势力利用的风险来部署多少个节点。我们绝对不能让我们的'中央'系统有任何后门，而这个系统必须尽可能无处不在地存在于我们控制的空间中及周围。" +
                    "\n\n通常我们需要在微节点的制造和部署期间保持对一个系统的控制，但在它们部署之后，我们不需要继续持有该领土。" +
                    "\n\n我们入侵敌方网络的能力——虽然有限——是对我们永远不要过度扩张的严厉警告。如果我们被反入侵，那不会是以小范围、分割的方式；那将是终结。在部署微节点时对我们的行动进行伪装的冗余程度，以及每AU空间中微节点密度的极端预防措施，证明了我们在这个话题上的极度偏执。" +
                    "\n\n\n ", 
                    "好吧" );

                return MouseHandlingResult.None;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_TechRefunds-bReadExplanation-tooltipBuffer" );
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "此界面允许你通过'遗忘'中央科技或舰队升级来取回'已花费'的科技点。你可能想知道这是如何运作的，指挥官。如果你愿意听工程师们的解释，我们已经为你准备了一份说明。", "ShipTooltipScale" );
            }
        }
        #endregion

        #region bNoTechsAtTheMoment
        public class bNoTechsAtTheMoment : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "<align=left>" );
                Buffer.Add( "<pos=5>" );
                Buffer.Add( "当前没有中央科技已升级" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.PlayClickDeniedSound;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_TechRefunds-bNoTechsAtTheMoment-tooltipBuffer" );
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "看起来你目前没有解锁任何中央科技。要从中获得退还，你需要先解锁一些。从其他来源获得的免费升级无法退还。", "ShipTooltipScale" );
            }
        }
        #endregion

        #region bNoFleetsAtTheMoment
        public class bNoFleetsAtTheMoment : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "<align=left>" );
                Buffer.Add( "<pos=5>" );
                Buffer.Add( "当前没有舰队有直接升级" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.PlayClickDeniedSound;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_TechRefunds-bNoFleetsAtTheMoment-tooltipBuffer" );
            public override void HandleMouseover()
            {
                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "你知道吗？查看舰队详情时，你可以直接用科技点升级它。这对指挥站尤其强大，因为不仅指挥站会升级，炮台也会升级。这对移动舰队的用处较小，但对于中心有巨像或方舟的舰队仍然很棒。至少在早期游戏中，直接升级你的母星指挥站舰队一两次几乎是一个不二之选。", "ShipTooltipScale" );
            }
        }
        #endregion
    }
}
