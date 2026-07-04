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
                Buffer.Add( "姝ゅ崟浣嶇殑鏁版嵁涓虹┖銆傚鏋滀綘鍒氬垰鍔犺浇浜嗘父鎴忥紝璇峰彇娑堟殏鍋滐紝鏁版嵁搴旇浼氬～鍏? );
                return;
            }
            if ( data.OutguardGroup != null )
                Buffer.Add( "姝ゅ崟浣嶆潵鑷?" + data.OutguardGroup.DisplayName + " 缁勩€? + data.OutguardGroup.Description + "銆?璇ョ粍瀛樺湪鏄负浜?" + data.OutguardGroup.Class );
        }
    }
}
