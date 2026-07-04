using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class NecromancerDeepLink : NecromancerDeepLinkRoot
    {
        public NecromancerDeepLink()
        {
            NecromancerDeepLinkRoot.Instance = this;
        }

        public override void SpawnNecromancerNecropolis( ArcenPoint spawnLocation, Planet planet, string Tag, Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( Context == null )
                return; //client
            var deep = faction.GetExternalDeepInfoAs<NecromancerEmpireFactionDeepInfo>();
            if (deep == null)
                return;

            deep.SpawnNecromancerNecropolis( spawnLocation, planet, Tag, faction, Context, out GameEntity_Squad unusedNecroFlag, true );
        }
    }
}
