using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Diagnostics;

namespace Arcen.AIW2.External
{
    public class Window_ErrorReportMenu : WindowControllerAbstractBase, IInputActionHandler
    {
        public static Window_ErrorReportMenu Instance;
        public Window_ErrorReportMenu()
        {
            Instance = this;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
            this.ShouldShowEvenWhenGUIHidden = true;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( Engine_Universal.LastErrorText.Length <= 0 )
                return false;
            if ( Window_LoadQuickStartMenu.Instance.GetIsOpen() )
                return false;
            if ( Window_SetupTopTabs.Instance.GetShouldDrawThisFrame() )
                return false;
            if ( Window_SetupTopTabs.Instance.GetShouldDrawThisFrame() )
                return false;
            if ( Window_JoinMultiplayerGameByIPMenu.Instance.GetShouldDrawThisFrame() )
                return false;
            if ( Window_JoinMultiplayerGameByListMenu.Instance.GetShouldDrawThisFrame() )
                return false;
            if ( Window_ClientMultiplayerConnectionStatus.Instance.GetShouldDrawThisFrame() )
                return false;
            if ( this.IsPermanentlyClosed )
            {
                this.Close();
                return false;
            }
            return true;
        }

        private bool IsPermanentlyClosed;

        public override void OnShowAfterNotShowing()
        {
            if ( World.Instance.IsLoaded )
                Window_InGameEscapeMenu.HandleOpeningAMenuThatMightPause();
            
            base.OnShowAfterNotShowing();
        }

        public override void OnHideAfterShowing()
        {
            if ( World.Instance.IsLoaded )
                Window_InGameEscapeMenu.HandleClosingAMenuThatMightPause();
            
            base.OnHideAfterShowing();
        }
        
        public void Close()
        {
            Engine_Universal.LastErrorText = string.Empty;
        }

        //from IInputActionHandler
        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            //LOG.Msg("{0}() called for '{1}'", this.TypeNameAndMethod(), InputActionType.InternalName);

            switch ( InputActionType.InternalName )
            {
                case "OpenSystemMenu":
                case "Return":
                {
                    this.Close();
                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
                }
            }
        }
        
        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                
            }
        }
        #endregion

        public class customParent : CustomUIAbstractBase
        {
            public override void OnUpdate()
            {
            }
        }

        public class bOpenLog : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Process.Start( Engine_Universal.CurrentPlayerDataDirectory + "ArcenDebugLog.txt" );
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "请随时在 bugtracker.arcengames.com 上报告错误并附上这份日志！主菜单的「附加内容」部分有一个链接可以跳转到问题跟踪器。" ); }
            public override void OnUpdate() { }
        }

        public class bIgnore : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "有些错误只是无害的界面小故障（但我们仍然希望修复它们）。其他错误则会让游戏运行略微异常，直到你重启程序。" ); }
            public override void OnUpdate() { }
        }

        public class bIgnoreAndStopReporting : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.IsPermanentlyClosed = true;
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "错误将不再以可见方式弹出，但它们会继续记录到日志中。你会在游戏画面右上角看到自游戏开始以来发生错误的次数。你的游戏可能会变得越来越卡顿，错误日志可能会在循环超过最大限制后开始覆盖自身。如果你遇到如此多的错误以至于想使用此选项，那么你应该保存游戏（尽量不要覆盖已有的存档文件！）并退出，尽快重启程序。" ); }
            public override void OnUpdate() { }
        }

        public class tBodyText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                string textToShow = Engine_Universal.LastErrorText;
                if ( textToShow.Length > 5000 )
                    textToShow.Substring( 5000 );
                Buffer.Add( textToShow );
                Buffer.Add( "\n\n\n\n\n \n" ); //add extra spacing to prevent clipping
            }
            public override void OnUpdate() { }
        }
    }
}