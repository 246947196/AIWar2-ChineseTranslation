using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Diagnostics;

namespace Arcen.AIW2.External
{
    public class Window_ModalOKTall : Window_ModalOKTallBase, IInputActionHandler
    {
        public static Window_ModalOKTall Instance;
        public Window_ModalOKTall()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
            this.PreventsNormalInputHandlers = true;
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
        }

        public override bool DoesDataStyleMatch( ModalPopupStyle Style )
        {
            return Style == ModalPopupStyle.OkTall;
        }
    }

    public class Window_ModalOKTall_Wide : Window_ModalOKTallBase, IInputActionHandler
    {
        public static Window_ModalOKTall_Wide Instance;
        public Window_ModalOKTall_Wide()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
            this.PreventsNormalInputHandlers = true;
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
        }

        public override bool DoesDataStyleMatch( ModalPopupStyle Style )
        {
            return Style == ModalPopupStyle.OkTallWide;
        }
    }

    public class Window_ModalOKTall_UltraWide : Window_ModalOKTallBase, IInputActionHandler
    {
        public static Window_ModalOKTall_UltraWide Instance;
        public Window_ModalOKTall_UltraWide()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
            this.PreventsNormalInputHandlers = true;
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
        }

        public override bool DoesDataStyleMatch( ModalPopupStyle Style )
        {
            return Style == ModalPopupStyle.OkTallUltraWide;
        }
    }

    public abstract class Window_ModalOKTallBase : ToggleableWindowController, IInputActionHandler
    {       
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
                        CloseAll();
                        try
                        {
                            popupData.OnNoOrClose?.Invoke();
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "Error in clicking OK button: " + e, Verbosity.ShowAsError );
                        }
                    }
                    catch
                    {
                        CloseAll();
                    }
                }
            }
        }

        public static void CloseAll()
        {
            Window_ModalOKTall.Instance.Close();
            Window_ModalOKTall_Wide.Instance.Close();
            Window_ModalOKTall_UltraWide.Instance.Close();
        }
        public abstract bool DoesDataStyleMatch( ModalPopupStyle Style );

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
            if ( !this.DoesDataStyleMatch( data.Style ) )
                return false;
            return true;
        }

        public override void OnShowAfterNotShowing()
        {
            //scroll back to the top
            if ( tBodyText.bodyTextTransform )
            {
                UnityEngine.Vector3 pos = tBodyText.bodyTextTransform.localPosition;
                pos.y = 0;
                tBodyText.bodyTextTransform.localPosition = pos;
            }
        }

        public class bClose : ButtonAbstractBase
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
                    CloseAll();
                    try
                    {
                        popupData.OnNoOrClose?.Invoke();
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "Error in clicking OK button: " + e, Verbosity.ShowAsError );
                    }
                }
                catch
                {
                    CloseAll();
                }
                return MouseHandlingResult.None;
            }
        }

        public class tBodyText : TextAbstractBase
        {
            //public static UnityEngine.UI.ScrollRect scrollRect = null;
            //public static UnityEngine.UI.Scrollbar scrollbar = null;
            public static UnityEngine.RectTransform bodyTextTransform = null;

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( Engine_Universal.CurrentPopups[0].BodyText );
                Buffer.Add( "\n\n\n\n  \n" );
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
                case "Return":
                    try
                    {
                        Engine_Universal.CurrentPopups[0].OnNoOrClose?.Invoke();
                        Engine_Universal.CurrentPopups.RemoveAt( 0 );
                    }
                    catch { }
                    CloseAll();
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
