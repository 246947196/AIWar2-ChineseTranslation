using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class CivilWarCauserAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;
            //Note this is really expensive
            int numFound = 1;//count yourself
            foreach ( GameEntity_Squad otherEntity in World_AIW2.Instance.Squads( EntityRollupType.CivilWarWhenNoneLeft ) )
            {
                if ( otherEntity.TypeData != RelatedEntityOrNull.TypeData )
                    continue;
                if ( otherEntity.HasAlreadyDoneOnFirstDeath )
                    continue;
                if ( otherEntity == RelatedEntityOrNull )
                    continue;
                numFound++;
            }
            if ( numFound > 1 )
                Buffer.Add( "There are currently " + numFound + " of these structures in the galaxy." );
            else
                Buffer.Add( " This is the last one. Be very wary about destroying it!" );
        }
    }
}
