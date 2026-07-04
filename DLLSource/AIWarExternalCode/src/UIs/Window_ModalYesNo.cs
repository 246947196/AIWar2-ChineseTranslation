using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Diagnostics;

namespace Arcen.AIW2.External
{
    public class Window_ModalYesNo : ToggleableWindowController, IInputActionHandler
    {
        public static Window_ModalYesNo Instance;
        public Window_ModalYesNo()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
            this.PreventsNormalInputHandlers = true;
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
        }

        public class customParent : CustomUIAbstractBase
        {
            public override void OnUpdate()
            {
                if (InputActionTypeDataTable.GetActionByName_FairlySlow("Return").CalculateIsSinglePress())
                {
                    try
                    {
                        ModalPopupData popupData = Engine_Universal.CurrentPopups[0];
                        Engine_Universal.CurrentPopups.RemoveAt( 0 );
                        Instance.Close();
                        try
                        {
                            popupData.OnYes?.Invoke();
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "Error in clicking Yes button: " + e, Verbosity.ShowAsError );
                        }
                    }
                    catch
                    {
                        Instance.Close();
                    }
                }
            }
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            //if ( !base.GetShouldDrawThisFrame_Subclass() )
            //    return false;
            if ( Engine_Universal.CurrentPopups.Count <= 0 )
                return false;
            ModalPopupData data = Engine_Universal.CurrentPopups[0];
            if ( data == null )
            {
                Engine_Universal.CurrentPopups.RemoveAt( 0 );
                ArcenDebugging.ArcenDebugLogSingleLine( "There was null ModalPopupData found in Window_ModalOK at Engine_Universal.CurrentPopups[0]!  Now there are " +
                    Engine_Universal.CurrentPopups.Count + " popups left in that list...", Verbosity.DoNotShow );
                return false;
            }
            if ( data.Style != ModalPopupStyle.YesNo )
                return false;
            return true;
        }

        public override void OnShowAfterNotShowing()
        {
            if ( tBodyText.bodyTextTransform )
            {
                UnityEngine.Vector3 pos = tBodyText.bodyTextTransform.localPosition;
                pos.y = 0;
                tBodyText.bodyTextTransform.localPosition = pos;
            }
        }

        public class bYes : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( Engine_Universal.CurrentPopups[0].YesButtonText );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                try
                {
                    ModalPopupData popupData = Engine_Universal.CurrentPopups[0];
                    Engine_Universal.CurrentPopups.RemoveAt( 0 );
                    Instance.Close();
                    try
                    {
                        popupData.OnYes?.Invoke();
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "Error in clicking Yes button: " + e, Verbosity.ShowAsError );
                    }
                }
                catch
                {
                    Instance.Close();
                }
                return MouseHandlingResult.None;
            }
        }

        public class bNo : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( Engine_Universal.CurrentPopups[0].NoOrCloseButtonText );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                try
                {
                    ModalPopupData popupData = Engine_Universal.CurrentPopups[0];
                    Engine_Universal.CurrentPopups.RemoveAt( 0 );
                    Instance.Close();
                    try
                    {
                        popupData.OnNoOrClose?.Invoke();
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "Error in clicking No button: " + e, Verbosity.ShowAsError );
                    }
                }
                catch
                {
                    Instance.Close();
                }
                return MouseHandlingResult.None;
            }
        }

        public class tBodyText : TextAbstractBase
        {
            public static UnityEngine.RectTransform bodyTextTransform = null;
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( Engine_Universal.CurrentPopups[0].BodyText );
                Buffer.Add( "\n\n\n\n\n \n" ); //add extra spacing to prevent clipping
            }
            public override void OnUpdate()
            {
                if ( bodyTextTransform == null )
                    bodyTextTransform = this.Element.RelevantRect;

                ArcenUI_Text textElement = this.Element as ArcenUI_Text;
                if ( textElement )
                    textElement.FontScale = GameSettings.Current.GetFloatBySetting( "CentralPopupTextScale" );
            }
        }

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( Engine_Universal.CurrentPopups[0].HeaderText );
            }
            public override void OnUpdate() { }
        }

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.InternalName )
            {
                case "OpenSystemMenu":
                    try
                    {
                        ModalPopupData popupData = Engine_Universal.CurrentPopups[0];
                        Engine_Universal.CurrentPopups.RemoveAt( 0 );
                        Instance.Close();
                        try
                        {
                            popupData.OnNoOrClose?.Invoke();
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "Error in clicking No button: " + e, Verbosity.ShowAsError );
                        }
                    }
                    catch
                    {
                        Instance.Close();
                    }
                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
                case "Return":
                    try
                    {
                        ModalPopupData popupData = Engine_Universal.CurrentPopups[0];
                        Engine_Universal.CurrentPopups.RemoveAt( 0 );
                        Instance.Close();
                        try
                        {
                            popupData.OnYes?.Invoke();
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "Error in clicking Yes button: " + e, Verbosity.ShowAsError );
                        }
                    }
                    catch
                    {
                        Instance.Close();
                    }
                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
                case "TogglePause":
                case "TogglePauseAlt":
                    EndpointFunctions.TogglePause( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    break;
            }
        }
    }
}