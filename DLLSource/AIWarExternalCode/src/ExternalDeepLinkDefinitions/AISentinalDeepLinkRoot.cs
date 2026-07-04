using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcen.AIW2.External
{
    public abstract class AiSentinalDeepLinkRoot : IExternalDeepLink
    {
        public static AiSentinalDeepLinkRoot Instance;

        public abstract void DoAIDefeatLogicOnNoKingsLeftLogic( Faction aifaction, ArcenHostOnlySimContext context, GameEntity_Squad kingOrNull );
    }
}
