using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_SetupChatStandaloneRight : Window_SetupTabWithChatWindowBase
    {
        public Window_SetupChatStandaloneRight()
        {
            //all of the interesting stuff in this class happens in Window_SetupTabWithChatWindowBase
            //this only shows right now when the map tab shows, because this is built into the other tab windows directly
        }

        #region GetShouldDrawThisFrame_Subclass
        public override bool GetShouldDrawThisFrame_Subclass()
        {
            return Window_SetupTabWithChatWindowBase.CalculateIsMultiplayer() &&
                Window_SetupTopTabs.Current == LobbyTabType.Map && //the others have it built in
                Window_SetupTopTabs.Instance.GetShouldDrawThisFrame_Subclass();
        }
        #endregion

        public static float ScreenSpaceLeft;

        #region custSetupWindowChatOnRight
        public class custSetupWindowChatOnRight : CustomUIAbstractBase
        {
            public override void OnUpdate()
            {
                ScreenSpaceLeft = ArcenUI.Instance.guiCamera.WorldToScreenPoint( this.Element.RelevantRect.GetWorldSpaceBottomLeftCorner() ).x;
                //ArcenDebugging.ArcenDebugLogSingleLine( this.Element.RelevantRect + " rect " +  this.Element.RelevantRect.GetWorldSpaceBottomLeftCorner() + " left " + ScreenSpaceLeft, Verbosity.DoNotShow );
            }
        }
        #endregion

        #region Inherited Stuff
        public class tChatText : tChatText_Base
        { }

        public class tPlayerInfoText : tPlayerInfoText_Base
        { }

        public class tChatHeaderText : tChatHeaderText_Base
        { }

        public class btnSendChat : btnSendChat_Base
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                iChatTextbox.Instance.DoSend();
                return MouseHandlingResult.None;
            }
        }

        public class iChatTextbox : iChatTextbox_Base
        {
            public static iChatTextbox Instance;
            public iChatTextbox() { Instance = this; }

            public override void DoSend()
            {
                DoSend_Inner( this );
            }

            public override void ClearTextbox()
            {
                this.SetText( string.Empty );
            }
        }
        #endregion
    }
}
