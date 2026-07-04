using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DarkZenithSidekickSpecificCodeDeepInfo : ExternalFactionDeepInfoRoot
    {
        public override bool SeedUnitsOnStartingPlanetDuringMapGen( Planet StartingPlanet, ConfigurationForFaction factionConfig, PlanetFaction pFaction, 
            ref ArcenPoint commandStationPoint, ref bool stillNeedsToSeedHumanHomeworldStuff, 
            Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull, ArcenHostOnlySimContext Context )
        {
            int debugIndex = 0;
            try
            {
                //This is not used; the one in DarkZenithFactionDeepInfo is used instead
            }
            catch ( Exception e)
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "DarkZenithEmpireSpecificCodeDeepInfo SeedUnitsOnStartingPlanetDuringMapGen error at debugIndex " + debugIndex + ": " + e );
                return false;
            }
            return true;
        }
        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                 if ( planet.GetControllingFaction().Type == FactionType.AI)
                 {
                     PlanetFaction pFaction = planet.GetPlanetFactionForFaction( this.AttachedFaction );
                     pFaction.AIPLeftFromWarpGate = 5;
                     pFaction.AIPLeftFromCommandStation = 15;
                 }
            }
        }
    }
}
