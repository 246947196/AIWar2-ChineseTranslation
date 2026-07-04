using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;
using UnityEngine.UI;

namespace Arcen.AIW2.External
{
    public class Window_CenterScreenPopups : WindowControllerAbstractBase
    {
        public static Window_CenterScreenPopups Instance = new Window_CenterScreenPopups();
        public Window_CenterScreenPopups()
        {
            this.OnlyShowInGame = false;
            this.ShouldShowEvenWhenGUIHidden = true;
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true;
            Instance = this;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( InputCaching.CalculateShouldTooltipsBeSuppressed() )
                return false;
            if ( tText.Instance == null )
                return false;
            if ( tText.Instance.GetShouldBeHidden() )
                return false;
            return true;
        }

        public class tText : TextAbstractBase
        {
            public static tText Instance;
            public tText() { Instance = this; }

            public override bool GetShouldBeHidden()
            {
                if ( InputCaching.CalculateShouldTooltipsBeSuppressed() )
                    return true;
                return updateBufferResult.Length == 0;
            }

            public override bool GetShouldRunUpdatesEvenWhenHidden()
            {
                return true;
            }

            private bool hasDoneInitialSetup = false;

            private ArcenDoubleCharacterBuffer updateBuffer = new ArcenDoubleCharacterBuffer( "Window_CenterScreenPopups-updateBuffer" );
            private string updateBufferResult = string.Empty;
            public override void OnUpdate()
            {
                this.WindowController.myScale = GameSettings.Current.GetFloatBySetting( "GeneralTooltipScale" );
                if ( !this.hasDoneInitialSetup )
                {
                    this.hasDoneInitialSetup = true;
                    ArcenUI_Text textElement = this.Element as ArcenUI_Text;
                    textElement.ReferenceText.alignment = TMPro.TextAlignmentOptions.Center;
                    textElement.ReferenceText.raycastTarget = false;

                    Image bgImage = this.Element.GetComponentInChildren<Image>();
                    bgImage.color = ColorMath.FromAlphaAndColor( Color.black, 0.9f );
                    bgImage.raycastTarget = false;
                }

                CenterScreenPopupData.ClearAnyExpiredMessages();

                //Examples:
                //CenterScreenPopupData.CreateAndLogNewOrExtendExistingAndReplaceText( "First test messsage, with some length on it.", "FIRSTTEST", 0.5f );
                //CenterScreenPopupData.CreateAndLogNewOrExtendExistingAndReplaceText( "Second test message.", "SECTEST", 0.2f );
                //CenterScreenPopupData.CreateAndLogNewOrExtendExistingAndReplaceText( "third test message.", "sdf", 0.2f );
                //CenterScreenPopupData.CreateAndLogNewOrExtendExistingAndReplaceText( "fourth test message.", "dfgre", 0.2f );

                updateBufferResult = string.Empty;
                updateBuffer.EnsureResetForNextUpdate();

                int debugStage = 1;
                try
                {
                    debugStage = 4000;
                    for ( int i = 0; i < Engine_Universal.CurrentCenterScreenDisplays.Count; i++ )
                    {
                        debugStage = 4100;
                        CenterScreenPopupData display = Engine_Universal.CurrentCenterScreenDisplays[i];
                        debugStage = 4200;
                        if ( display == null )
                            continue;
                        if ( !updateBuffer.GetIsEmpty() )
                            updateBuffer.Add( "\n" );
                        updateBuffer.Add( display.Text );
                        //CHIRS_TESTING
                        //updateBuffer.Add( " " + (display.ShowUntil - DateTime.Now).TotalSeconds + "s remaining" );
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Center screen display error at stage " + debugStage + ": " + e, Verbosity.ShowAsError );
                }

                updateBufferResult = updateBuffer.GetStringAndResetForNextUpdate();
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( updateBufferResult.Length > 0 )
                    buffer.Add( updateBufferResult );
            }
        }
    }
}
