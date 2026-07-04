using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class PlanetGeneralFocusChatHandler : PlanetViewChatHandlerBase
    {
        public override void DoOnClick( MouseHandlingInput Input )
        {
            Planet plan = this.PlanetToView;
            if ( plan != null )
            {
                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                    Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( plan, false );
                else
                {
                    World_AIW2.Instance.SwitchViewToPlanet( plan );
                }
            }
        }

        public override void DoOnTooltip( ArcenDoubleCharacterBuffer Buffer )
        {
            Planet plan = this.PlanetToView;
            if ( plan != null )
            {
                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                    Buffer.Add( "Click to center the galaxy view on " ).Add( plan.Name ).Add( ". " );
                else
                {
                    Buffer.Add( "Click to move your view to the planet " ).Add( plan.Name ).Add( ". " );
                }
            }
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddPlanetIndex_Neg1ToPos( MetaData, this.PlanetToView == null ? (Int16)(-1) : this.PlanetToView.Index, "PlanetToView" );
        }
        public override void DeserializeInto( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.PlanetToView = World_AIW2.Instance.GetPlanetByIndex( Buffer.ReadPlanetIndex_Neg1ToPos( MetaData, "PlanetToView" ) );
        }
    }
}
