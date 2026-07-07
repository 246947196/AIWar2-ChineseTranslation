using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class SporeDescriptionAppender : GameEntityDescriptionAppenderBase
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
            if ( infestation.Telia == null )
            {
                Buffer.Add( "请暂停游戏以查看关于此孢子的附加信息" );
                return;
            }
            MacrophagePerSporeBaseInfo sDataOrNull = RelatedEntityOrNull.TryGetExternalBaseInfoAs<MacrophagePerSporeBaseInfo>();
            if ( sDataOrNull == null )
            {
                Buffer.Add( "如果你刚刚加载了游戏，请取消暂停，数据应该会填充" );
                return;
            }
            if ( infestation.Telia == null )
                return;

            List<SafeSquadWrapper> telia = infestation.Telia.GetDisplayList();
            foreach ( SafeSquadWrapper wrap in telia )
            {
                GameEntity_Squad tSquad = wrap.GetSquad();
                if ( tSquad == null )
                    continue;
                if ( sDataOrNull.TeliumID == tSquad.PrimaryKeyID )
                    Buffer.Add( "此孢子来自位�" + tSquad.GetPlanetName_Safe() + " 上的泰利姆。如果有来自 " + infestation.NumDifferentTeliaRequired + " 个不同星球的孢子，它们将形成一个新的泰利亚。" );
            }

            if ( infestation.SporeLifespan > 0 )
            {
                int secondsLeft = (infestation.SporeLifespan + sDataOrNull.SpawnTime) - World_AIW2.Instance.GameSecond;
                Buffer.Add( $"此孢子有有限的生命周期，即将消失：{(secondsLeft / 60).ToString("0")}:{(secondsLeft % 60).ToString("00")}" );
            }
        }
    }
}
