using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class WormholeProjectorAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;
            WormholeInvasionPerUnitBaseInfo data = RelatedEntityOrNull.CreateExternalBaseInfo<WormholeInvasionPerUnitBaseInfo>( "WormholeInvasionPerUnitBaseInfo" );
            Planet DestPlanet = World_AIW2.Instance.GetPlanetByIndex( (short)data.LinkedPlanetIdx );
            if ( RelatedEntityTypeData.GetHasTag("LivingWormholeProjector" ) )
            {
                Buffer.Add( " This unit is connecting  " ).Add(RelatedEntityOrNull.Planet.Name, "a1ffa1").Add(" and ").Add(DestPlanet.Name, "ffa1a1").Add("\n");
            }
            
            if ( RelatedEntityTypeData.GetHasTag("DestabilizedWormholeProjector" ) )
            {
                Buffer.Add( " This unit is connecting  " ).Add(RelatedEntityOrNull.Planet.Name, "a1ffa1").Add(" and ").Add(DestPlanet.Name, "ffa1a1").Add("\n");
                Buffer.Add(" The wormhole is destabilized and will vanish in ").AddHoursAndMinutes(data.TimeToRemoveUnit - World_AIW2.Instance.GameSecond, "a1a1ff").Add(".");
            }

        }
    }
}
