using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using UnityEngine;

namespace Arcen.AIW2.External
{
    //This Scenario is the "Main Game" scenario. Other scenarios exist for
    //Tutorials and the Test Chamber
    public class Scenario_AftermathOfLosingTheAIWar : BaseScenario
    {
        protected override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap_Inner()
        {
            //nothing to do here
        }

        public override bool WriteCurrentOngoingMessageToDisplay( ArcenDoubleCharacterBuffer Buffer )
        {
            return base.WriteCurrentOngoingMessageToDisplay( Buffer );
        }
    }
}
