using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_GiftFleetToPlayer : Window_DynamicallyFilledAbstractBase
    {
        public static Window_GiftFleetToPlayer Instance;
        
        private Fleet _fleet;
        private bool _open;
        
        public Window_GiftFleetToPlayer()
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
            _fleet = fleet;
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

        public void Toggle_Show( Fleet fleet )
        {
            if (_open && 
                _fleet == fleet)
            {
                this.Close();
                return;
            }
            
            _open = true;
            _fleet = fleet;
        }

        public bool Is_Showing( Fleet fleet )
        {
            return _open && 
                   _fleet == fleet;
        }

        #endregion

        #region bClose
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
                Buffer.Add( "<size=95%>赠送舰队：" ).Add( Instance._fleet.GetName(), "a1a1ff" ).Add( " 给其他玩家？</size>");
            }
        }
        #endregion

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

        #region customParent
        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_GiftFleetToPlayer.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                    }
                    #endregion
                }

                this.WindowController.myXPositionScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" );
                this.WindowController.myScale = GameSettings.Current.GetFloatBySetting( "NotificationsScale" );

                //if you change away from the fleets tab, close the fleet management popout
                //if ( (Window_InGameSidebarBase.Current != InGameSidebarType.Fleets &&
                //    Window_InGameSidebarBase.Current != InGameSidebarType.Ships) )
                //{
                //    Instance.Close();
                //    return;
                //}
            }
        }
        #endregion

        #region PopulateFreeFormControls
        
        private float fullWidth = 600;
        private static readonly List<Faction> factionsICanGiftTo = List<Faction>.Create_WillNeverBeGCed( 300, "Window_GiftFleetToPlayer-factionsICanGiftTo" );
        private ArcenCachedExternalTypeDirect type_bChooseFaction = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bChooseFaction ) );

        public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            if ( bMainContentParent.ParentT == null )
                return;
            this.Window.SetOverridingTransformToWhichToAddChildren( bMainContentParent.ParentT );

            Faction localFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();

            #region Calculate factionsICanGiftTo
            factionsICanGiftTo.Clear();
            if ( localFactionOrNull != null )
            {
                for ( int i = 0; i < World_AIW2.Instance.FactionsThatCanBeGiftedShips.Count; i++ )
                {
                    if ( World_AIW2.Instance.FactionsThatCanBeGiftedShips[i] != localFactionOrNull )
                    {
                        factionsICanGiftTo.Add( World_AIW2.Instance.FactionsThatCanBeGiftedShips[i] );
                    }
                }
            }
            #endregion

            float runningY = topBuffer;

            Rect leftBounds;

            Faction otherFac;

            for ( int j = 0; j < factionsICanGiftTo.Count; j++ )
            {
                otherFac = factionsICanGiftTo[j];

                #region Line Items To Select (bChooseFaction) *****************************************
                this.rowHeight = ROW_HEIGHT_DEFAULT;
                this.CalculateBoundsSingle( out leftBounds, ref runningY, fullWidth );
                AddButton( Set, type_bChooseFaction, string.Empty, otherFac.FactionIndex, otherFac.FactionIndex, leftBounds, -1f );
                #endregion *****************************************                
            }

            bMainContentParent.ParentRT.UI_SetHeight( runningY );
        }
        
        #endregion

        #region GetFactionFromElement
        public static Faction GetFactionFromElement( ArcenUI_Element element )
        {
            if ( element.CreatedByCodeDirective == null )
                return null;
            int index = element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            return World_AIW2.Instance.GetFactionByIndex( index );
        }
        #endregion

        #region bChooseFaction
        public class bChooseFaction : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Faction otherFac = GetFactionFromElement( this.Element );
                if ( otherFac == null )
                {
                    Buffer.Add( "空阵营...?" );
                    return;
                }
                Buffer.Add("<align=left>");
                Buffer.Add("<pos=5>");
                Buffer.StartColor( otherFac.FactionCenterColor.ColorHexBrighter );
                Buffer.Add( otherFac.GetDisplayName() );
                Buffer.EndColor();
                Buffer.Add("</pos>");
                Buffer.Add("</align>");
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Fleet _fleet = Instance._fleet;
                Faction giftToFac = GetFactionFromElement( this.Element );

                if ( _fleet == null )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "错误", "空舰队无法赠送！", "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                ArcenRejectionReason cannotGiftFleetReason = _fleet.GetCanGiftFleetToAnotherPlayer();
                if ( cannotGiftFleetReason != ArcenRejectionReason.Unknown )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "错误", "目前无法赠送舰队：" + cannotGiftFleetReason, "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                if ( giftToFac == null )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "错误", "目标阵营为空！", "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                ModalPopupData.CreateAndLogYesNoStyle( delegate
                {
                    GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    command.RelatedIntegers.Add( _fleet.FleetID ); //FleetID
                    command.RelatedIntegers2.Add( giftToFac.FactionIndex ); //FactionIndex
                    command.RelatedString = "GiftFleetToFaction";
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                    Instance.Close();

                }, null, "赠送舰队？", "你确定要将舰队 " + _fleet.GetName() +
                    " 赠送给 " + giftToFac.GetDisplayName() + " 吗？他们可以将它赠送回来，你也可以暂时控制他们的帝国将其赠送回来。", "是的，赠送", "不，等等！" );

                return MouseHandlingResult.None;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_GiftFleetToPlayer-bChooseFaction-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( Instance._fleet == null )
                    Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "没有选择要赠送的舰队！", "ShipTooltipScale" );
                Faction otherFac = GetFactionFromElement( this.Element );
                if ( otherFac == null )
                {
                    Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "没有可赠送的目标阵营！", "ShipTooltipScale" );
                    return;
                }
                ArcenRejectionReason cannotGiftFleetReason = Instance._fleet.GetCanGiftFleetToAnotherPlayer();
                if ( cannotGiftFleetReason != ArcenRejectionReason.Unknown )
                    Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "目前无法赠送此舰队！", "ShipTooltipScale" );
                else
                    Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( "将舰队 " + Instance._fleet.GetName() + " 赠送给 " + otherFac.GetDisplayName() + "？", "ShipTooltipScale" );
            }
        }
        #endregion
    }
}
