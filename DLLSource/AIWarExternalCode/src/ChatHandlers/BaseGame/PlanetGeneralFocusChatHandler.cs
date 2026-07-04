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
                    Buffer.Add( "鐐瑰嚮灏嗘槦绯昏鍥惧眳涓埌 " ).Add( plan.Name ).Add( "銆? );
                else
                {
                    Buffer.Add( "鐐瑰嚮灏嗚鍥剧Щ鑷虫槦鐞?" ).Add( plan.Name ).Add( "銆? );
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
