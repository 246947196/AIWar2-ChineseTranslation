using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ShipGeneralFocusChatHandler : SquadViewChatHandlerBase
    {
        public override void DoOnClick( MouseHandlingInput Input )
        {
            GameEntity_Squad squad = this.SquadToView.GetSquad();
            Planet plan = squad?.Planet;
            if ( plan != null && squad != null && squad.GetShouldBeVisibleBasedOnPlanetIntel() )
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
            if ( plan != null && squad != null )
            {
                if ( !squad.GetShouldBeVisibleBasedOnPlanetIntel() )
                {
                    Buffer.Add( "The " ).Add( squad.TypeData.DisplayName ).Add( " is currently on a planet where you don't have vision. " );
                }
                else
                {
                    if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                        Buffer.Add( "Click to center the galaxy view on " ).Add( plan.Name ).Add( ", the planet where " ).Add( squad.TypeData.DisplayName ).Add( " is located. " );
                    else
                    {
                        Buffer.Add( "Click to center the planet view on " ).Add( squad.TypeData.DisplayName ).Add( ", on the planet " ).Add( plan.Name ).Add( ". " );
                    }
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
            this.SquadToView = LazyLoadSquadWrapper.Create( Buffer.ReadBig3PrimaryKeyID_PosNoDef( MetaData, "SquadToView", true ), true, "ShipGeneralFocusChatHandler" );
        }
    }
}
