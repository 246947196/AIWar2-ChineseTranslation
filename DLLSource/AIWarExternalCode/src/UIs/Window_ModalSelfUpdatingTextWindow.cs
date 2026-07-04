using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Diagnostics;

namespace Arcen.AIW2.External
{
    public class Window_ModalSelfUpdatingTextWindow : Window_ModalSelfUpdatingTextWindowBase, IInputActionHandler
    {
        public static Window_ModalSelfUpdatingTextWindow Instance;
        public Window_ModalSelfUpdatingTextWindow()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
            this.PreventsNormalInputHandlers = true;
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
        }
    }

    public class Window_ModalSelfUpdatingTextWindow_Wide : Window_ModalSelfUpdatingTextWindowBase, IInputActionHandler
    {
        public static Window_ModalSelfUpdatingTextWindow_Wide Instance;
        public Window_ModalSelfUpdatingTextWindow_Wide()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
            this.PreventsNormalInputHandlers = true;
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
        }
    }

    public class Window_ModalSelfUpdatingTextWindow_UltraWide : Window_ModalSelfUpdatingTextWindowBase, IInputActionHandler
    {
        public static Window_ModalSelfUpdatingTextWindow_UltraWide Instance;
        public Window_ModalSelfUpdatingTextWindow_UltraWide()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
            this.PreventsNormalInputHandlers = true;
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
        }
    }

    public abstract class Window_ModalSelfUpdatingTextWindowBase : WindowControllerAbstractBase, IInputActionHandler
    {
        private bool IsOpen;
        private UpdaterDelegate updater;
        private string headerText;
        private string lastBodyText;
        private static readonly ArcenDoubleCharacterBuffer bodyTextBuffer = new ArcenDoubleCharacterBuffer( "Window_ModalSelfUpdatingTextWindowBase-bodyTextBuffer" );
        private DateTime lastUpdate = DateTime.Now;
        private DateTime lastPositiveResultTime = DateTime.Now;
        private float secondsBetweenUpdates = 1;
        //how long to wait, keeping on getting a false result, before we show the "nothing to show" result.  Otherwise keep showing the old data
        private float secondsBeforeUpdateToNothing = 2f;
        private string closeText;
        private bool pauseWorld;

        public Window_ModalSelfUpdatingTextWindowBase()
        {
            this.PreventsNormalInputHandlers = true;
        }

        public override void OnShowAfterNotShowing()
        {
            base.OnShowAfterNotShowing();
            
            if ( pauseWorld && World.Instance.IsLoaded )
                Window_InGameEscapeMenu.HandleOpeningAMenuThatMightPause();
            
            if ( tBodyText.bodyTextTransform )
            {
                UnityEngine.Vector3 pos = tBodyText.bodyTextTransform.localPosition;
                pos.y = 0;
                tBodyText.bodyTextTransform.localPosition = pos;
            }
        }

        public override void OnHideAfterShowing()
        {
            base.OnHideAfterShowing();
            
            if ( pauseWorld && World.Instance.IsLoaded )
                Window_InGameEscapeMenu.HandleClosingAMenuThatMightPause();
        }

        public void Open( float SecondsBetweenUpdates, float SecondsBeforeUpdateToNothing, string HeaderText, string CloseText, UpdaterDelegate Updater )
        {
            Open(SecondsBetweenUpdates, SecondsBeforeUpdateToNothing, HeaderText, CloseText, Updater, false);
        }
        
        public void Open( float SecondsBetweenUpdates, float SecondsBeforeUpdateToNothing, string HeaderText, string CloseText, UpdaterDelegate Updater, bool PauseWorld )
        {
            this.IsOpen = true;
            this.updater = Updater;
            this.headerText = HeaderText;
            this.closeText = CloseText;
            this.secondsBetweenUpdates = SecondsBetweenUpdates;
            this.secondsBeforeUpdateToNothing = SecondsBeforeUpdateToNothing;

            this.updater( bodyTextBuffer ); //we don't care about the bool result in this particular case
            this.lastBodyText = bodyTextBuffer.GetStringAndResetForNextUpdate();
            this.lastUpdate = DateTime.Now;
            this.lastPositiveResultTime = DateTime.Now;
            this.pauseWorld = PauseWorld;
        }

        public static Window_ModalSelfUpdatingTextWindowBase GetCurrentInstance()
        {
            if ( Window_ModalSelfUpdatingTextWindow_Wide.Instance.IsOpen )
                return Window_ModalSelfUpdatingTextWindow_Wide.Instance;
            if ( Window_ModalSelfUpdatingTextWindow_UltraWide.Instance.IsOpen )
                return Window_ModalSelfUpdatingTextWindow_UltraWide.Instance;
            return Window_ModalSelfUpdatingTextWindow.Instance;
        }

        public void Close()
        {
            this.IsOpen = false;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( !this.IsOpen )
                return false;
            return true;
        }

        public bool GetIsOpen()
        {
            return this.IsOpen;
        }

        public class customParent : CustomUIAbstractBase
        {
            public override void OnUpdate()
            {
                Window_ModalSelfUpdatingTextWindowBase instance = GetCurrentInstance();
                if (InputActionTypeDataTable.GetActionByName_FairlySlow("Return").CalculateIsSinglePress())
                {
                    instance.Close();
                    return;
                }
                this.Element.Window.MaxDeltaTimeBeforeUpdates = 0;

                if ( ( DateTime.Now - instance.lastUpdate ).TotalSeconds >= instance.secondsBetweenUpdates )
                {
                    if ( instance.updater( bodyTextBuffer ) )
                    {
                        //we had some positive text to write -- yay!
                        bool changed;
                        instance.lastBodyText = bodyTextBuffer.GetStringAndResetForNextUpdate(out changed);
                        
                        instance.lastUpdate = DateTime.Now;
                        instance.lastPositiveResultTime = DateTime.Now;
                    }
                    else
                    {
                        //do another check in 1/4 of the normal interval, since we got a negative result
                        instance.lastUpdate = DateTime.Now.AddSeconds( -instance.secondsBetweenUpdates / 4f );
                        //meanwhile, keep showing the old thing, I guess
                        if ( (DateTime.Now - instance.lastPositiveResultTime).TotalSeconds >= instance.secondsBeforeUpdateToNothing )
                        {
                            //...unless it has been "too long", in which case update to show the negative result
                            instance.lastBodyText = bodyTextBuffer.GetStringAndResetForNextUpdate();
                        }
                        else
                        {
                            //if we really are ignoring it, then make sure the buffer gets reset
                            bodyTextBuffer.ResetForNextUpdate();
                        }
                    }
                }
            }
        }

        public class bClose : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Window_ModalSelfUpdatingTextWindowBase instance = GetCurrentInstance();
                Buffer.Add( instance.closeText );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_ModalSelfUpdatingTextWindowBase instance = GetCurrentInstance();
                //ArcenDebugging.ArcenDebugLogSingleLine( "\n" + instance.lastBodyText + "\n", Verbosity.DoNotShow );
                instance.Close();
                return MouseHandlingResult.None;
            }
        }

        public class tBodyText : TextAbstractBase
        {
            public static UnityEngine.RectTransform bodyTextTransform = null;
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Window_ModalSelfUpdatingTextWindowBase instance = GetCurrentInstance();
                Buffer.Add( instance.lastBodyText );
                Buffer.Add( "\n\n\n\n\n\n" ); //add extra spacing to prevent clipping
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
                Window_ModalSelfUpdatingTextWindowBase instance = GetCurrentInstance();
                Buffer.Add( instance.headerText );
            }
            public override void OnUpdate() { }
        }

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.InternalName )
            {
                case "OpenSystemMenu":
                case "Return":
                    this.Close();
                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
                case "TogglePause":
                case "TogglePauseAlt":
                    EndpointFunctions.TogglePause( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    break;
            }
        }

        public delegate bool UpdaterDelegate( ArcenDoubleCharacterBuffer BufferToWriteTo );
    }
}