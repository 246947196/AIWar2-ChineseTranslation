using System;
using System.Text;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class XmlModTable_Codehook_Handler : IArcenExternalCodeHookHandler
    {       
        public void HandleExternalHook( object MainObject, object SecondaryObject, object[] AdditionalObjects, ArcenExternalCodeHook Hook, ArcenSimContextBase Context )
        {
            if (Hook.InternalName == "ReloadSelectData")
            {
                XmlModTable.Instance.ParseModFolders();
            }
        }
    }
}
