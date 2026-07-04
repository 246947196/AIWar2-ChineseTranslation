using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using Arcen.AIW2.External;
using UnityEngine;

namespace Arcen.AIW2.ExternalVisualization
{
    public class GalaxyMapDisplayMode_Threat : BaseGalaxyMapDisplayMode
    {
        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            //first get the player squads
            int newOtherGimbalIndex = 0;
            UtilityMethodsFor_GalaxyMapDisplayMode.AddPlayerFleetsToGalaxyMapOtherThingsToShow( planet, EntityToSkip, ArrayToFill, ref newOtherGimbalIndex );
        }

        public override void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            if ( planet.GetControllingFactionType() == FactionType.Player )
                return;

            Faction playerFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
            if ( playerFaction == null )
                return;

            int totalStrength = planet.GetPlanetFactionForFaction( playerFaction ).DataByStance[FactionStance.Hostile].RelativeToHumanTeam_ThreatStrengthVisible;

            //RIGHT ONLY
            if ( totalStrength > 0 )
            {
                RightBuffer.StartColor( ArcenExternalUIUtilities.StrengthTextColor );
                ArcenExternalUIUtilities.ByPlanet_WriteStrengthIconWithColor( RightBuffer, ArcenExternalUIUtilities.StrengthTextColor );

                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( RightBuffer, totalStrength, true, true );
            }
        }
    }
}
