using System;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class GalaxyMapLinkUtils
    {
        public static void DetectAiAndWarpGateAdjacencyToPlayer( Planet Planet1, Planet Planet2, out Planet aiPlanet, out bool hasGate )
        {
            aiPlanet = null;
            hasGate = false;
                
            if ( Planet1 == null || Planet2 == null )
                return;

            FactionType faction1_type = Planet1.GetControllingFactionType();
            FactionType faction2_type = Planet2.GetControllingFactionType();

            Planet playerPlanet = null;
            if (faction1_type == FactionType.Player)
                playerPlanet = Planet1;
            else if (faction1_type == FactionType.AI)
                aiPlanet = Planet1;
            if (faction2_type == FactionType.Player)
                playerPlanet = Planet2;
            else if (faction2_type == FactionType.AI)
                aiPlanet = Planet2;
            
            if (aiPlanet != null)
            {
                bool temp = false;
                PlanetFaction pfacForGate = aiPlanet.GetControllingPlanetFaction();
                if ( pfacForGate != null )
                {
                    foreach ( GameEntity_Squad e in pfacForGate.Entities.Squads( EntityRollupType.WarpEntryPoints ) )
                    {
                        temp = true;
                        break;
                    }
                }

                hasGate = temp;
            }
            
            if (playerPlanet == null)
                aiPlanet = null;
        }
    }
}
