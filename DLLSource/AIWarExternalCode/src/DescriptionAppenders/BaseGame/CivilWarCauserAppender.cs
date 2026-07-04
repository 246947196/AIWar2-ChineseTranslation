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
                Buffer.Add( "鐩墠鏄熺郴涓湁 " + numFound + " 涓绫诲缓绛戙€? );
            else
                Buffer.Add( " 杩欐槸鏈€鍚庝竴涓€傝闈炲父灏忓績鍦版懅姣佸畠锛? );
        }
    }
}
