using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public abstract class NecromancerDeepLinkRoot : IExternalDeepLink
    {
        public static NecromancerDeepLinkRoot Instance;

        public abstract void SpawnNecromancerNecropolis( ArcenPoint spawnLocation, Planet planet, string Tag, Faction faction, ArcenHostOnlySimContext Context );
    }
}
