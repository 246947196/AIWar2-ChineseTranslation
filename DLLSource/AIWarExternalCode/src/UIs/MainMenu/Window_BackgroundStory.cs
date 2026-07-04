using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_BackgroundStory : ToggleableWindowController
    {
        public static Window_BackgroundStory Instance;
        public Window_BackgroundStory()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
        }

        public class customParent : CustomUIAbstractBase
        {
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