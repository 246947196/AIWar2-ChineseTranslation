using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public enum LobbyTabType
    {
        Factions,
        Map,
        Options
    }

    /* Hey, some notes!  These apply to all the lobby setup windows.
     * 
     *     1. If you try to touch World_AIW2.Instance.Factions from in here, you're going to have a bad time.
     *        That's something that may be out of date, and in general is not the "data to show to players in here."
     *        
     *     2. You really should be most interested in World_AIW2.Instance.SetupWorkingForLobbyOnly.FactionConfigurations.
     *        That has the things-I-want-based-on-UI-selections data in it.  As that gets changed it will -- in its own
     *        good time -- propagate over to World_AIW2.Instance.Factions.
     *        
     *     3. Same for World_AIW2.Instance.SetupStoredLongTerm.  Don't be messing with that!  It's out of date probably,
     *        and could be in any old indeterminate state.  That is for AFTER the game starts.  Until the game has started,
     *        you should just be messing with World_AIW2.Instance.SetupWorkingForLobbyOnly, and it will automatically
     *        propagate over to World_AIW2.Instance.SetupStoredLongTerm as needed.
     * */
    public class Window_SetupTopTabs : WindowControllerAbstractBase
    {
        public static LobbyTabType Current = LobbyTabType.Factions;

        public static Window_SetupTopTabs Instance;
        public Window_SetupTopTabs()
        {
            Instance = this;
        }

        public override void SetWindow( ArcenUI_Window Window )
        {
            base.SetWindow( Window );
            Current = LobbyTabType.Map;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( !World_AIW2.Instance.InSetupPhase )
                return false;
            if ( World_AIW2.Instance.IsFromQuickLoad )
                return false;
            return true;
        }

        public static float ScreenSpaceBottom;

        #region custSetupTopTabs
        public class custSetupTopTabs : CustomUIAbstractBase
        {
            public override void OnUpdate()
            {
                ScreenSpaceBottom = ArcenUI.Instance.guiCamera.WorldToScreenPoint( this.Element.RelevantRect.GetWorldSpaceBottomLeftCorner() ).y;
                //ArcenDebugging.ArcenDebugLogSingleLine( this.Element.RelevantRect + " rect " + this.Element.RelevantRect.GetWorldSpaceBottomLeftCorner() + " bottom " + ScreenSpaceBottom, Verbosity.DoNotShow );

                if ( Engine_AIW2.Instance.CurrentGameViewMode != GameViewMode.GalaxyMapView )
                    Engine_AIW2.Instance.SetCurrentGameViewMode( GameViewMode.GalaxyMapView );
            }
        }
        #endregion

        #region tabFactions
        public class tabFactions : SetupTabBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( !this.GetIsSelected() )
                    Window_SetupTopTabs.Current = LobbyTabType.Factions;
                ArcenUI.HideAnyOpenDropdowns();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() {                     Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "配置将包含在银河系中的所有派系。敌人、盟友、人类玩家和神秘的第三方。" ); }
            public override bool GetIsSelected()
            {
                return Window_SetupTopTabs.Current == LobbyTabType.Factions;
            }
        }
        #endregion

        #region tabOptions
        public class tabOptions : SetupTabBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( !this.GetIsSelected() )
                    Window_SetupTopTabs.Current = LobbyTabType.Options;
                ArcenUI.HideAnyOpenDropdowns();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() {                     Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "这些选项特定于此战役，通常定义了超出基础规则之外的游戏玩法。" ); }
            public override bool GetIsSelected()
            {
                return Window_SetupTopTabs.Current == LobbyTabType.Options;
            }
        }
        #endregion

        #region tabMap
        public class tabMap : SetupTabBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( !this.GetIsSelected() )
                    Window_SetupTopTabs.Current = LobbyTabType.Map;
                ArcenUI.HideAnyOpenDropdowns();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() {                     Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "银河系地图的整体结构是什么样的？同时可以在此选择起始行星。" ); }
            public override bool GetIsSelected()
            {
                return Window_SetupTopTabs.Current == LobbyTabType.Map;
            }
        }
        #endregion

        #region SetupTabBase
        public abstract class SetupTabBase : ButtonAbstractBase
        {
            private static Color unselectedColor = ColorMath.HexToColor( "969696" );
            private static Color selectedColor = ColorMath.HexToColor( "ffffff" );
            private SelectedStatus lastSelected = SelectedStatus.Unknown;

            public ArcenUI_Button elementAsButton = null;
            public override void OnUpdate()
            {
                if ( elementAsButton == null )
                    this.elementAsButton = (ArcenUI_Button)this.Element;
                if ( this.elementAsButton != null )
                {
                    SelectedStatus newSelected = this.GetIsSelected() ? SelectedStatus.Selected : SelectedStatus.NotSelected;
                    if ( newSelected == lastSelected )
                        return;
                    lastSelected = newSelected;
                    this.elementAsButton.SetColor( newSelected == SelectedStatus.Selected ? selectedColor : unselectedColor );
                    this.elementAsButton.RelatedImages[0].sprite = this.elementAsButton.RelatedSprites[newSelected == SelectedStatus.Selected ? 1 : 0];
                }
            }

            public abstract bool GetIsSelected();

            private enum SelectedStatus
            {
                Unknown = 0,
                Selected,
                NotSelected
            }
        }
        #endregion        

        #region tQuestionMarkInfo
        public class tQuestionMarkInfo : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Host )
                    Buffer.Add( "正在主持多人游戏大厅！    [多人游戏问题？]" );
                else if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                    Buffer.Add( "已作为多人游戏客户端加入此大厅！    [多人游戏问题？]" );
            }
            public override void HandleMouseover()
            {
                string message = "除主机外，所有玩家默认都是观察者。默认只有一个人类玩家派系。任意数量的玩家可以共享控制一个派系，或者每个玩家都有自己控制的派系。您可以自由组合搭配。";
                ArcenNetworkAuthority.GetAddedStringForMultiplayerInfo( ref message );
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, message );
            }

            public override bool GetShouldBeHidden()
            {
                return ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.SinglePlayerOnly;
            }
        }
        #endregion
    }
}
