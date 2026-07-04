using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Diagnostics;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameSidebarClosed : Window_InGameSidebarBase
    {
        public static Window_InGameSidebarClosed Instance;
        public Window_InGameSidebarClosed()
        {
            Instance = this;
            this.OnlyShowInGame = true;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            return Current == InGameSidebarType.Closed;
        }
    }
}