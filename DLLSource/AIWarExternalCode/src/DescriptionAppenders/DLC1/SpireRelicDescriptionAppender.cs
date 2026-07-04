using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class SpireRelicDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            // Make sure we are getting an entity.
            if ( RelatedEntityOrNull == null )
                return;
            FallenSpirePerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<FallenSpirePerUnitBaseInfo>();
            if ( data.DestinationPlanet != null &&
                 data.DestinationPlanet != RelatedEntityOrNull.Planet )
                Buffer.Add( "This relic is en route to " ).Add( data.DestinationPlanet.Name ).Add( "." );
            if ( data.MustBuildOnStartPlanet )
                Buffer.Add( "This relics power supply was crippled by the AI, and must build a Spire City on this planet." );
        }
    }
}
