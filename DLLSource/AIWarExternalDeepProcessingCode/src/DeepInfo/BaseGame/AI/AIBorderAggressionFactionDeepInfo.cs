using System;

using System.Linq;
using System.Text;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public sealed class AIBorderAggressionFactionDeepInfo : AIRelentlessAndBorderAggressionFactionDeepInfoRoot
    {
        public override void SubCleanup()
        {
        }

        //this method lets them do logic that is specific to search for themselves
        public override bool GetFactionMatchesMyInternalName( Faction faction )
        {
            if ( faction == null )
                return false;
            return faction.SpecialFactionData.InternalName == "AIBorderAggression";
        }
    }
}
