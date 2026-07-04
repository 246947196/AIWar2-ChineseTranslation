using System;

using System.Linq;
using System.Text;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    //this must be different from the border aggression one because it otherwise gets mixed up with checks for "find the relentless wave logic for this AI" and similar.
    //but the actual logic of the two is identical... mostly
    public sealed class AIRelentlessWaveFactionDeepInfo : AIRelentlessAndBorderAggressionFactionDeepInfoRoot
    {       
        public override void SubCleanup()
        {
        }

        //this method lets them do logic that is specific to search for themselves
        public override bool GetFactionMatchesMyInternalName( Faction faction )
        {
            if ( faction == null )
                return false;
            return faction.SpecialFactionData.InternalName == "AIRelentlessWave";
        }
    }
}
