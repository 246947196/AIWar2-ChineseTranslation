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
                Buffer.Add( "FallenSpireFactionBaseInfo.Instance 由于某种原因为空！" );
                return;
            }
            // Make sure we have our city list before continuing.
            if ( FallenSpireFactionBaseInfo.Instance.SpireCities.Count <= 0 )
            {
                Buffer.Add( "尖塔可能尚未初始化？显示没有城市。请取消暂停游戏，同时请报告此错误。" );
                return;
            }

            FallenSpireFactionBaseInfo.WriteCityTooltipDetails( Buffer, RelatedEntityOrNull );
        }
    }
}
