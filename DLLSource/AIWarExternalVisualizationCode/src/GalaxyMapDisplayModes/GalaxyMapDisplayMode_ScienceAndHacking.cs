using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using Arcen.AIW2.External;

namespace Arcen.AIW2.ExternalVisualization
{
    public class GalaxyMapDisplayMode_ScienceAndHacking : BaseGalaxyMapDisplayMode
    {
        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;
            int newOtherGimbalIndex = 0;
            UtilityMethodsFor_GalaxyMapDisplayMode.AddPlayerFleetsToGalaxyMapOtherThingsToShow( planet, EntityToSkip, ArrayToFill, ref newOtherGimbalIndex );
        }

        public override void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            int scienceLeft = planet.GetScienceLeftForHumans().IntValue;
            int hackingLeft = planet.GetHackingLeftForHumans().IntValue;

            //RIGHT ONLY
            if ( scienceLeft > 0 )
                RightBuffer.Add( ArcenExternalUIUtilities.ByPlanet_ScienceTextColorAndIcon )
                    .StartColor( ArcenExternalUIUtilities.ScienceTextColor ).AddNumberMoreReadable( scienceLeft ).EndColor().Add( "\n" );
            if ( hackingLeft > 0 )
                RightBuffer.StartColor( ArcenExternalUIUtilities.HackingTextColor ).Add( ArcenExternalUIUtilities.ByPlanet_HackingTextColorAndIcon )
                    .AddNumberMoreReadable( hackingLeft ).EndColor().Add( "\n" );
        }
    }
}
