using Arcen.AIW2.Core;
using System;

using Arcen.Universal;
using Arcen.AIW2.External;
using UnityEngine;

namespace Arcen.AIW2.ExternalVisualization
{
    public class GalaxyMapDisplayMode_HopsFromHere : BaseGalaxyMapDisplayMode
    {
        public override bool GetShouldReplaceNormalPlanetTooltip()
        {
            return false;
        }

        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            GalaxyMapDisplayMode_Normal.ShowNormalIcons( planet, EntityToSkip, ArrayToFill, OnlyShowThingsThatShouldBeInFarZoom );
        }

        public override void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
	    Planet currentPlanet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();

            if ( planet.IntelLevel > PlanetIntelLevel.Unexplored )
		LeftBuffer.StartColor(planet.GetControllingOrInfluencingFaction()?.FactionCenterColor.TeamColor ?? Color.grey);
            LeftBuffer.Add(currentPlanet.GetHopsTo(planet));
        }
    }
}
