using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public abstract class FactionUtilityMethodsDeepLinkRoot : IExternalDeepLink
    {
        public static FactionUtilityMethodsDeepLinkRoot Instance;

        public abstract Planet FindSuitablePlanet( ArcenHostOnlySimContext Context, Int16 minHopsFromHumanPlanet, Int16 maxHopsFromHumanPlanet, byte preferredMarkUnderX, bool aiMustOwnPlanet );
    }
}
