using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class HackCustomButtonListInfo
    {
        public HackingType HackingType = null;
        public GameEntity_Squad TargetShip = null;
        public Planet TargetPlanet = null;
        public Faction HackFaction = null;

        public void Update( GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, Faction HackerFaction, HackingType HackType )
        {
            HackingType = HackType;
            TargetShip = TargetIfShip;
            TargetPlanet = TargetIfPlanet;
            HackFaction = HackerFaction;
            if ( TargetPlanet == null && TargetShip != null )
                TargetPlanet = TargetShip.Planet;
        }
    }
}
