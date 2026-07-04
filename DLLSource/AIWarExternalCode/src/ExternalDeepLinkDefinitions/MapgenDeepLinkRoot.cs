using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public abstract class MapgenDeepLinkRoot : IExternalDeepLink
    {
        public static MapgenDeepLinkRoot Instance;

        public abstract void SeedWardenSecretNinjaHideouts( ArcenHostOnlySimContext Context, Faction faction, int numToSeed );
    }
}
