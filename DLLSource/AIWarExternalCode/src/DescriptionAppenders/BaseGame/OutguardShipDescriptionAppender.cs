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
                Buffer.Add( "此单位的数据为空。如果你刚刚加载了游戏，请取消暂停，数据应该会填满。" );
                return;
            }
            if ( data.OutguardGroup != null )
                Buffer.Add( "此单位来自" + data.OutguardGroup.DisplayName + " 组。" + data.OutguardGroup.Description + "。该组存在是为了" + data.OutguardGroup.Class );
        }
    }
}
