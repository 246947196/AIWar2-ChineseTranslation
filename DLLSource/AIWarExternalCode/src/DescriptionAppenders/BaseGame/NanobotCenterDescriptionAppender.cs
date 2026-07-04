using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class NanobotCenterDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;
            Faction facOrNull = RelatedEntityOrNull.GetFactionOrNull_Safe();
            if ( facOrNull == null )
            {
                Buffer.Add( "请暂停游戏以查看附加信息" );
                return;
            }
            NanocaustFactionBaseInfo mgr = facOrNull.TryGetExternalBaseInfoAs<NanocaustFactionBaseInfo>();

            if ( mgr == null )
            {
                Buffer.Add( "未找到NanocaustFactionBaseInfo！" );
                return;
            }
            Buffer.Add( "此纳米机器人中心正在提供 " ).Add( (mgr.StrengthPerNanobotCenter.Display[RelatedEntityOrNull.PrimaryKeyID] / 1000).ToString(), "a1ffa1" ).Add( " 战力。" );
            //            Buffer.Add("State " + mgr.state);
        }
    }
}
