using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcen.AIW2.External
{
    public class AiSentinalDeepLink : AiSentinalDeepLinkRoot
    {
        public AiSentinalDeepLink()
        {
            Instance = this;
        }

        public override void DoAIDefeatLogicOnNoKingsLeftLogic( Faction aifaction, ArcenHostOnlySimContext context, GameEntity_Squad kingOrNull )
        {
            var deep = aifaction.GetAISentinelsDeepLogic();
            if (deep == null)
            {
                ArcenDebugging.ArcenDebugLog(string.Format("Error passed faction {0} that was not an ai sentinal.", aifaction?.GetDisplayName()??"null"), Verbosity.ShowAsError);
                return;
            }

            deep.DoAIDefeatLogicOnNoKingsLeftLogic(context, kingOrNull);
        }
    }
}
