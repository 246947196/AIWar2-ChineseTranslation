using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Diagnostics;

namespace Arcen.AIW2.External
{
    public class Window_ErrorReportMenuThatClearsAfter : WindowControllerAbstractBase, IInputActionHandler
    {
        public static Window_ErrorReportMenuThatClearsAfter Instance;
        public Window_ErrorReportMenuThatClearsAfter()
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
            if ( Window_LoadGameMenu.Instance.GetIsOpen() )
                return true;
            if ( Window_LoadQuickStartMenu.Instance.GetIsOpen() )
                return true;
            if ( Window_SetupTopTabs.Instance.GetShouldDrawThisFrame() )
                return true;
            if ( Window_SetupTopTabs.Instance.GetShouldDrawThisFrame() )
                return true;
            if ( Window_JoinMultiplayerGameByIPMenu.Instance.GetShouldDrawThisFrame() )
                return true;
            if ( Window_JoinMultiplayerGameByListMenu.Instance.GetShouldDrawThisFrame() )
                return true;
            if ( Window_ClientMultiplayerConnectionStatus.Instance.GetShouldDrawThisFrame() )
                return true;
            //if the correct other window is not open, then don't show this message
            return false;
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

        public class bIgnore : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                //this clears the error count, since these were not problematic in the main
                ArcenDebugging.ErrorSinceStart = 0;

                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover() { Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "此错误不会影响本次游戏运行的其他部分。" ); }
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