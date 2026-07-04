using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class ApkalluInfusedHumanEmpireFactionBaseInfo : ApkalluFactionBaseInfo
    {
        // public override void SetStartingFactionRelationships()
        // {
        //     // This IS the human empire faction; no ally-to-humans call needed
        // }

        public static bool GetIsThisAnApkalluInfusedFaction( Faction fac )
        {
            return fac.GetExternalBaseInfoAs<ApkalluInfusedHumanEmpireFactionBaseInfo>() != null;
        }
    }
}
