using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arcen.AIW2.External
{
    //This is passed into the WormholeInvasionManager, and is used to figure out the details for this invasion
    //For example, if the Fallen Spire AI Response wanted to send a wormhole invasion, they would make one of thse objects
    //then call WormholeInvasionManager.LaunchWormholeInvasion()
    public struct WormholeInvasionOptions
    {
        public int AttackStrength;
        public int TurretStrength;
        public int GuardStrength;
        public Faction ResponsibleAIFaction; //the AI who sent this Invasion

        public int WaveCount;  //if want (say) 1 giant wave, or 5 waves over time
        public int WaveInterval;

        //in general either provide both or neither
        public Planet StartPlanet;
        public Planet DestinationPlanet;

        public int ProjectorAppearanceTime;
        public int PlanetLinkTime;

        public bool ForceInvasionLaunch; //ordinarily if we have can't find a good planet to attack, we just do nothing (and wait for more budget).
                                         //This flag requires the invasion to be launched, and is typically used when launching the invasion due to a specific event, rather than just general budget spending

        //At ProjectorAppearanceTime the projector appears. And then at ProjectorLinkTime it will link the planets.
        //After that, every WaveInterval more units will spawn from the RelentlessWave associated with the ResponsibleAIFaction

        //When the projector appears, TurretStrength of turrets are created, and GuardianStrength defensive dire guardians
        //If the projector dies, those units will attrition
        public static WormholeInvasionOptions CreateWithDefaults( int totalStrength )
        {
            /*  60% strength to attack, 20% for turrets and guardians respectively */
            WormholeInvasionOptions options = new WormholeInvasionOptions();
            int strengthUnit = totalStrength / 5;
            options.AttackStrength = strengthUnit * 3;
            options.TurretStrength = strengthUnit;
            options.GuardStrength = strengthUnit;
            options.StartPlanet = null;
            options.DestinationPlanet = null;
            return options;
        }
        public static WormholeInvasionOptions CreateWithDefaults( int totalStrength, Faction responsibleAIFaction )
        {
            /*  60% strength to attack, 20% for turrets and guardians respectively */
            WormholeInvasionOptions options = new WormholeInvasionOptions();
            int strengthUnit = totalStrength / 5;
            options.AttackStrength = strengthUnit * 3;
            options.TurretStrength = strengthUnit;
            options.GuardStrength = strengthUnit;
            options.ResponsibleAIFaction = responsibleAIFaction;
            options.StartPlanet = null;
            options.DestinationPlanet = null;
            return options;
        }

        public static WormholeInvasionOptions CreateWithDefaults( int attackStrength, int turretStrength, int guardStrength, Faction responsibleAIFaction )
        {
            WormholeInvasionOptions options = new WormholeInvasionOptions();
            options.AttackStrength = attackStrength;
            options.TurretStrength = turretStrength;
            options.GuardStrength = guardStrength;
            options.ResponsibleAIFaction = responsibleAIFaction;
            options.StartPlanet = null;
            options.DestinationPlanet = null;
            return options;
        }


        public static WormholeInvasionOptions CreateWithSpecifics( int attackStrength, int turretStrength, int guardStrength, Faction responsibleAIFaction,
                                                                   int waveCount, int waveInterval, Planet startPlanet, Planet destPlanet, int projectorAppearanceTime, int planetLinkTime,
                                                                   bool forceInvasionLaunch )
        {
            WormholeInvasionOptions options;
            options.AttackStrength = attackStrength;
            options.TurretStrength = turretStrength;
            options.GuardStrength = guardStrength;
            options.ResponsibleAIFaction = responsibleAIFaction;
            options.WaveCount = waveCount;
            options.WaveInterval = waveInterval;
            options.StartPlanet = startPlanet;
            options.DestinationPlanet = destPlanet;
            options.ProjectorAppearanceTime = projectorAppearanceTime;
            options.PlanetLinkTime = planetLinkTime;
            options.ForceInvasionLaunch = forceInvasionLaunch;
            return options;
        }

    }
}
