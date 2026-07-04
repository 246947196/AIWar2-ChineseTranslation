using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class FactionUtilityMethodsDeepLink : FactionUtilityMethodsDeepLinkRoot
    {
        public FactionUtilityMethodsDeepLink()
        {
            FactionUtilityMethodsDeepLinkRoot.Instance = this;
        }

        public override Planet FindSuitablePlanet( ArcenHostOnlySimContext Context, short minHopsFromHumanPlanet, short maxHopsFromHumanPlanet, byte preferredMarkUnderX, bool aiMustOwnPlanet ) => FactionUtilityMethods.FindSuitablePlanet( Context, minHopsFromHumanPlanet, maxHopsFromHumanPlanet, preferredMarkUnderX, aiMustOwnPlanet );
    }
}
