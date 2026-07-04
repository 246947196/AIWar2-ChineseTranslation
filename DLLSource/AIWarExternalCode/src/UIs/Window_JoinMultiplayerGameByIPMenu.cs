using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Net;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Arcen.AIW2.External
{
    public class Window_JoinMultiplayerGameByIPMenu : ToggleableWindowController, IInputActionHandler
    {
        public static Window_JoinMultiplayerGameByIPMenu Instance;
        public Window_JoinMultiplayerGameByIPMenu()
        {
            Instance = this;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
            this.PreventsNormalInputHandlers = true;
        }

        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_ModalTextboxWindow.Instance != null )
                {
                    if ( !hasGlobalInitialized )
                    {
                        hasGlobalInitialized = true;
                    }
                }
            }
        }

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "通过IP直连主机" );
            }
            public override void OnUpdate() { }
        }

        #region tQuestionMarkInfo
        public class tQuestionMarkInfo : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "[多人游戏问题？]" );
            }
            public override void HandleMouseover()
            {
                string message;
                NetworkingFramework frame = NetworkingFrameworkTable.Instance.GetRowByNameOrNullIfNotFound( GameSettings.Current.GetStringBySetting( "LastChosenNetworkType" ) );
                if ( frame == null )
                    message = "尚未选择网络框架。";
                else
                {
                    message = frame.ClientConnectTooltip;
                }
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, message );
            }
        }
        #endregion

        #region tExtraInfoText
        public class tExtraInfoText : TextAbstractBase
        {
            private bool hasDoneWrapping = false;
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( !hasDoneWrapping )
                {
                    hasDoneWrapping = true;
                    ((ArcenUI_Text)this.Element).ReferenceText.enableWordWrapping = true;
                }

                string message;
                NetworkingFramework frame = NetworkingFrameworkTable.Instance.GetRowByNameOrNullIfNotFound( GameSettings.Current.GetStringBySetting( "LastChosenNetworkType" ) );
                if ( frame == null )
                    message = "Network framework not yet chosen.";
                else
                {
                    message = "<size=" + frame.ClientConnectWindowTextSize + ">" + frame.ClientConnectWindowText;
                }
                Buffer.Add( message );
            }
            public override void HandleMouseover()
            {
            }
        }
        #endregion

        public class bCancel : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "关闭" );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }
        }

        public class tLabelText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "要连接的IP：" );
            }
            public override void OnUpdate() { }
        }

        public class iModalTextbox : InputAbstractBase
        {
            public string IP = "127.0.0.1";

            public static iModalTextbox Instance;

            public iModalTextbox()
            {
                Instance = this;
                this.IP = GameSettings.Current.GetStringBySetting( "LastIPTriedToConnectTo" );
            }

            public override void HandleChangeInValue( string NewValue )
            {
                this.IP = NewValue;
            }

            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                switch ( addedChar )
                {
                    case '.': //ok, for IPv4
                    case ':': //ok, for IPv6
                        return addedChar;
                    default:
                        if ( ArcenStrings.GetIsHexadecimalDigit( addedChar ) )
                            return addedChar;
                        break;
                }

                return '\0';
            }

            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                switch ( Action.InternalName )
                {
                    case "OpenSystemMenu": //escape key
                    case "Return": //enter key
                        return InputActionTextboxResult.UnfocusMe;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }

            public override void OnUpdate()
            {
                //only update to the current value if we're not editing this field right now
                if ( !this.GetIsCurrentlyBeingEdited() )
                {
                    ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;
                    elementAsType.SetText( this.IP );
                }
            }
        }

        public class bSave : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                base.GetTextToShowFromVolatile( Buffer );
                Buffer.Add( "连接" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( Window_ClientMultiplayerConnectionStatus.Instance.AwaitingResultOfInitialConnectionThread.IsBusy() )
                    return MouseHandlingResult.PlayClickDeniedSound; //already searching!

                string ipToUseForActualConnection = iModalTextbox.Instance.IP;
                if ( !this.ValidateIPForSubmission( ipToUseForActualConnection ) )
                    return MouseHandlingResult.PlayClickDeniedSound;
                Instance.Close();

                GameSettings.Current.SetStringBySetting( "LastIPTriedToConnectTo", ipToUseForActualConnection );
                GameSettings.SaveToDisk();

                //ArcenNetworkAuthority.SuppressReadLoop = true;
                if ( Window_ClientMultiplayerConnectionStatus.Instance.AwaitingResultOfInitialConnectionThread.DoNextOnlyIfNotAlreadyBusy( string.Empty ) )
                {
                    Window_ClientMultiplayerConnectionStatus.Instance.Open();
                    ArcenThreading.RunTaskOnBackgroundThread( "_UI.TryConnectToIP", false, false, () => OtherThread_DoActualConnection( ipToUseForActualConnection ) );
                }
                return MouseHandlingResult.None;
            }

            private void OtherThread_DoActualConnection( string ipToUseForActualConnection )
            {
                try
                {
                    ArcenNetworkAuthority.ActiveSocket.ConnectAsClient( ipToUseForActualConnection );
                }
                catch ( ArcenPleaseStopThisThreadException )//just means shutting down
                {
                } 
                catch ( Exception e ) //any other kind of error
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Error in network connection attempt: " + e, Verbosity.ShowAsError );
                }

                Window_ClientMultiplayerConnectionStatus.Instance.AwaitingResultOfInitialConnectionThread.MarkAsNoLongerBusy();
            }

            private bool ValidateIPForSubmission( string ip )
            {
                IPAddress dummy;
                return IPAddress.TryParse( ip, out dummy );
            }

            public override void HandleMouseover() { }
            public override void OnUpdate() { }
        }

        public override void OnOpen()
        {
            ArcenUI_Input elementAsType = (ArcenUI_Input)iModalTextbox.Instance.Element;
            elementAsType.ReferenceInputField.ActivateInputField();
            elementAsType.ReferenceInputField.Select();
        }

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.InternalName )
            {
                case "OpenSystemMenu":
                    this.Close();
                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
            }
        }
    }
}
