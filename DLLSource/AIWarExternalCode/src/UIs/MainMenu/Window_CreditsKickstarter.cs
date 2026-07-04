using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_CreditsKickstarter : ToggleableWindowController
    {
        public static Window_CreditsKickstarter Instance;
        public Window_CreditsKickstarter()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
        }

        public class customParent : CustomUIAbstractBase
        {
            private bool hasInitialized = false;
            public override void OnUpdate()
            {
                if ( !hasInitialized )
                {
                    hasInitialized = true;
                    RectTransform rectT = this.WindowController.Window.GetCanvasRectTransformForOneTimeChange_YouBetterKnowWhatYouAreDoing();
                    rectT.localRotation = Quaternion.Euler( 0, 5.39f, 0 );
                    rectT.localPosition = new Vector3( 0.71f, 0, ArcenUI.POSITION_Z );
                    rectT.localScale *= 1.08f;
                }
            }
        }

        public class bClose : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }
        }
    }
}