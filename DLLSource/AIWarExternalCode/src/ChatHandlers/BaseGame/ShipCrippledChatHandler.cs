using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ShipCrippledChatHandler : SquadViewChatHandlerBase
    {
        public override void DoOnClick( MouseHandlingInput Input )
        {
            GameEntity_Squad squad = this.SquadToView.GetSquad();
            Planet plan = squad?.Planet;
            if ( plan != null )
            {
                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                    Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( plan, false );
                else
                {
                    World_AIW2.Instance.SwitchViewToPlanet( plan );
                    Engine_AIW2.Instance.PresentationLayer.CenterPlanetViewOnEntity( squad, true );
                }
            }
        }

        public override void DoOnTooltip( ArcenDoubleCharacterBuffer Buffer )
        {
            GameEntity_Squad squad = this.SquadToView.GetSquad();
            Planet plan = squad?.Planet;
            if ( plan != null )
            {
                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                    Buffer.Add( "鐐瑰嚮灏嗘槦绯昏鍥惧眳涓埌 " ).Add( plan.Name ).Add( "锛? ).Add( squad.TypeData.DisplayName ).Add( " 鎵€鍦ㄧ殑鏄熺悆銆? );
                else
                {
                    Buffer.Add( "鐐瑰嚮灏嗘槦鐞冭鍥惧眳涓埌 " ).Add( squad.TypeData.DisplayName ).Add( "锛屼綅浜庢槦鐞?" ).Add( plan.Name ).Add( "銆? );
                }
            }
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            GameEntity_Squad squad = this.SquadToView.GetSquad();
            int pkID = squad == null ? 0 : squad.PrimaryKeyID;
            Buffer.AddBig3PrimaryKeyID_PosNoDef( MetaData, pkID, "SquadToView" );
        }
        public override void DeserializeInto( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.SquadToView = LazyLoadSquadWrapper.Create( Buffer.ReadBig3PrimaryKeyID_PosNoDef( MetaData, "SquadToView", true ), true, "ShipCrippledChatHandler" );
        }
    }
}
