using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class HarvesterDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;
            MacrophageFactionBaseInfoCore infestation = null;
            Faction facOrNull = RelatedEntityOrNull.GetFactionOrNull_Safe();
            if ( facOrNull != null )
                infestation = facOrNull.TryGetExternalBaseInfoAs<MacrophageFactionBaseInfoCore>();
            if ( infestation == null )
            {
                Buffer.Add( "无法在此处找到MacrophageFactionBaseInfo。这是一个错误" );
                return;
            }
            MacrophagePerHarvesterBaseInfo hData = RelatedEntityOrNull.TryGetExternalBaseInfoAs<MacrophagePerHarvesterBaseInfo>();
            if ( hData == null )
            {
                Buffer.Add( "此采集器的 hData 为空。如果你刚刚加载了游戏，请取消暂停，数据应该会填满。" );
                return;
            }
            Buffer.Add( "此采集器已收集 " + hData.CurrentMetal + " 金属 " );
            if ( hData.ReturningToTelium )
                Buffer.Add( "目前正在返回其泰利姆以存放金属。" );
            else
                Buffer.Add( "当收集到至少 " + infestation.MetalHarvesterCanHold + " 后将返回其泰利姆。" );
        }
    }
}
