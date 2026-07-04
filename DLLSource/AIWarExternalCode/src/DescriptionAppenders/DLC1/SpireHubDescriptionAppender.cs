using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class SpireHubDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            // Make sure we are getting an entity.
            if ( RelatedEntityOrNull == null )
                return;
            if ( FallenSpireFactionBaseInfo.Instance == null )
            {
                Buffer.Add( "FallenSpireFactionBaseInfo.Instance is null for some reason!" );
                return;
            }
            // Make sure we have our city list before continuing.
            if ( FallenSpireFactionBaseInfo.Instance.SpireCities.Count <= 0 )
            {
                Buffer.Add( "Fallen Spire not yet initialized, perhaps?  It says no cities. Please unpause the game, but also report this bug. " );
                return;
            }

            FallenSpireFactionBaseInfo.WriteCityTooltipDetails( Buffer, RelatedEntityOrNull );
        }
    }
}
