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
                Buffer.Add( "此遗物正在前往 " ).Add( data.DestinationPlanet.Name ).Add( " 的途中。" );
            if ( data.MustBuildOnStartPlanet )
                Buffer.Add( "此遗物的能源供应已被AI破坏，必须在此星球上建造一座尖塔城市。" );
        }
    }
}
