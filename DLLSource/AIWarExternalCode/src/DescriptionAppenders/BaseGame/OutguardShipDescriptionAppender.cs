using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class OutguardShipDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;
            OutguardPerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<OutguardPerUnitBaseInfo>();
            if ( data == null )
            {
                Buffer.Add( "data for this outguard is null. If you have just loaded a game then please unpause it and it should fill in" );
                return;
            }
            if ( data.OutguardGroup != null )
                Buffer.Add( "This unit is from the group of " + data.OutguardGroup.DisplayName + ". " + data.OutguardGroup.Description + ".  This group exists for " + data.OutguardGroup.Class );
        }
    }
}
