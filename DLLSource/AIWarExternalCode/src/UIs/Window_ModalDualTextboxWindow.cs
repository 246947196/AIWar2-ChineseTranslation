using Arcen.Universal;
using Arcen.AIW2.Core;
using System;


namespace Arcen.AIW2.External
{
    public class Window_ModalDualTextboxWindow : WindowControllerAbstractBase, IInputActionHandler
    {        
        public static Window_ModalDualTextboxWindow Instance;
        public Window_ModalDualTextboxWindow()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
            this.PreventsNormalInputHandlers = true;
        }

        private bool IsOpen;
        private SaveDelegate onSave;
        private string headerText;
        private string firstLabelText;
        private string secondLabelText;
        private bool firstMustBeAlphaNumeric;
        private bool secondMustBeAlphanumeric;
        public void Open( string HeaderText, string FirstLabelText, string SecondLabelText,  bool FirstMustBeAlphaNumeric, 
            bool SecondMustBeAlphaNumeric, string StartingText, string SecondStartingText, int maxFirstTextLength, int maxSecondTextLength, SaveDelegate OnSave )
        {
            this.IsOpen = true;
            iFirstModalTextbox.Instance.SetText( StartingText );
            iFirstModalTextbox.Instance.maxTextLength = maxFirstTextLength;
            iSecondModalTextbox.Instance.SetText( SecondStartingText );
            iSecondModalTextbox.Instance.maxTextLength = maxSecondTextLength;
            this.onSave = OnSave;
            this.headerText = HeaderText;
            this.firstLabelText = FirstLabelText;
            this.secondLabelText = SecondLabelText;
            this.firstMustBeAlphaNumeric = FirstMustBeAlphaNumeric;
            this.secondMustBeAlphanumeric = SecondMustBeAlphaNumeric;
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
                if ( Window_ModalDualTextboxWindow.Instance != null )
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
                string newText = iFirstModalTextbox.Instance.GetText();
                string newText2 = iSecondModalTextbox.Instance.GetText();
                if ( newText == null || newText.Length <= 0 )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "需要文本！", "未输入任何文本！", "返回" );
                    return MouseHandlingResult.None;
                }

                Instance.onSave( newText, newText2 );

                Instance.Close();
                return MouseHandlingResult.None;
            }
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

        public class tFirstLabelText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( Instance.firstLabelText );
            }
            public override void OnUpdate() { }
        }

        public class tSecondLabelText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( Instance.secondLabelText );
            }
            public override void OnUpdate() { }
        }

        public class iFirstModalTextbox : InputAbstractBase
        {
            public int maxTextLength = 10;
            public static iFirstModalTextbox Instance;
            public iFirstModalTextbox() { Instance = this; }
            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                if ( input.Length >= this.maxTextLength )
                    return '\0';
                if ( Window_ModalDualTextboxWindow.Instance.firstMustBeAlphaNumeric )
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
                    case "Return": //enter key
                        return InputActionTextboxResult.UnfocusMe;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }
        }

        public class iSecondModalTextbox : InputAbstractBase
        {
            public int maxTextLength = 10;
            public static iSecondModalTextbox Instance;
            public iSecondModalTextbox() { Instance = this; }
            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                if ( input.Length >= this.maxTextLength )
                    return '\0';
                if ( Window_ModalDualTextboxWindow.Instance.secondMustBeAlphanumeric )
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
                    case "Return": //enter key
                        return InputActionTextboxResult.UnfocusMe;
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
            }
        }

        public delegate void SaveDelegate( string NewText , string NewText2);
    }
}
