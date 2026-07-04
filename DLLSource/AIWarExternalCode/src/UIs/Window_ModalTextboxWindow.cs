using Arcen.Universal;
using Arcen.AIW2.Core;
using System;


namespace Arcen.AIW2.External
{
    public class Window_ModalTextboxWindow : WindowControllerAbstractBase, IInputActionHandler
    {        
        public static Window_ModalTextboxWindow Instance;
        public Window_ModalTextboxWindow()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
            this.PreventsNormalInputHandlers = true;
        }

        private bool IsOpen;
        private SaveDelegate onSave;
        private string headerText;
        private string labelText;
        private bool mustBeAlphaNumeric;
        public void Open( string HeaderText, string LabelText, bool MustBeAlphaNumeric, string StartingText, int maxTextLength, SaveDelegate OnSave )
        {
            this.IsOpen = true;
            iModalTextbox.Instance.SetText( StartingText );
            iModalTextbox.Instance.maxTextLength = maxTextLength;
            ((ArcenUI_Input)iModalTextbox.Instance.Element).Focus();
            this.onSave = OnSave;
            this.headerText = HeaderText;
            this.labelText = LabelText;
            this.mustBeAlphaNumeric = MustBeAlphaNumeric;
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

        public class bSave : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                TryToSave();
                return MouseHandlingResult.None;
            }

            #region TryToSave
            public static void TryToSave()
            {
                string newText = iModalTextbox.Instance.GetText();
                if ( newText == null || newText.Length <= 0 )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "需要文本！", "未输入任何文本！", "返回" );
                    return;
                }

                Instance.onSave( newText );

                Instance.Close();
            }
            #endregion

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "保存" );
            }
        }
        
        public class bCancel : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "取消" );
            }
        }

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( Instance.headerText );
            }
            public override void OnUpdate() { }
        }

        public class tLabelText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( Instance.labelText );
            }
            public override void OnUpdate() { }
        }

        public class iModalTextbox : InputAbstractBase
        {
            public int maxTextLength = 10;
            public static iModalTextbox Instance;
            public iModalTextbox() { Instance = this; }
            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                if ( input.Length >= this.maxTextLength )
                    return '\0';
                if ( Window_ModalTextboxWindow.Instance.mustBeAlphaNumeric )
                {
                    //use a whitelist of approved characters only
                    if ( Char.IsLetterOrDigit( addedChar ) ) //must be alphanumeric
                        return addedChar;
                    if ( addedChar == '_' || addedChar == ' ' || addedChar == '-' )
                        return addedChar;
                    //block everything except alphanumerics and _ and -
                    //for right now
                    return '\0';
                }
                else
                    return addedChar;
            }
            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                switch ( Action.InternalName )
                {
                    case "OpenSystemMenu": //escape key
                        Window_ModalTextboxWindow.Instance.Close();
                        ArcenInput.BlockForAJustPartOfOneSecond();
                        return InputActionTextboxResult.UnfocusMe;
                    case "Return": //enter key
                        bSave.TryToSave();
                        break;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }
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
                case "Return":
                    bSave.TryToSave();
                    break;
            }
        }

        public delegate void SaveDelegate( string NewText );
    }
}
