using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using Arcen.AIW2.External;

namespace Arcen.AIW2.External
{
    public class InitialSetupForDLL : IArcenExternalDllInitialLoadCall
    {
        public void RunOnFirstTimeExternalAssemblyLoaded()
        {
            if ( ArcenInput.PreInput == null )
                ArcenInput.PreInput = new PreInputHandler();

            SaveLoadMethods.PopulateCampaignFolders();

            //Construct the shared Harmony instance backing HarmonyIntegration's mod
            //method-patching API. The AssemblyResolve handler that locates 0Harmony.dll
            //was already wired up by ArcenAIW2Core's HarmonyDLLLoader during integration
            //init (Engine_AIW2.InitializeIntegrationsBeforeTCCheck), well before now.
            //Initialize() is idempotent; ApplyPatchesFromAssembly lazy-calls it on its
            //own first use, but doing it explicitly here gives a deterministic init log
            //line at a predictable point in startup.
            HarmonyIntegration.Initialize();
        }
    }

    public class PreInputHandler : IPreInputHandler
    {
        public void DoActionsJustBeforeInput()
        {
            //these are all things that need to be held down in order to stay on.
            //so we only clear them to false right before the input check
            ArcenInput_AIW2.ShouldShowShipRanges_Selected = false;
            ArcenInput_AIW2.ShouldShowShipRanges_Hovered = false;
            ArcenInput_AIW2.ShouldShowShipRanges_All = false;
            ArcenInput_AIW2.ShouldShowShipOrders = false;
        }
    }
}