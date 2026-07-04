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
                Buffer.Add( "鏃犳硶鍦ㄦ澶勬壘鍒?MacrophageFactionBaseInfo銆傝繖鏄竴涓?BUG" );
                return;
            }
            MacrophagePerHarvesterBaseInfo hData = RelatedEntityOrNull.TryGetExternalBaseInfoAs<MacrophagePerHarvesterBaseInfo>();
            if ( hData == null )
            {
                Buffer.Add( "姝ら噰闆嗗櫒鐨?hData 涓虹┖銆傚鏋滀綘鍒氬垰鍔犺浇浜嗘父鎴忥紝璇峰彇娑堟殏鍋滐紝鏁版嵁搴旇浼氬～鍏? );
                return;
            }
            Buffer.Add( "姝ら噰闆嗗櫒宸叉敹闆?" + hData.CurrentMetal + " 閲戝睘 " );
            if ( hData.ReturningToTelium )
                Buffer.Add( "鐩墠姝ｅ湪杩斿洖鍏舵嘲鍒╁浠ュ瓨鏀鹃噾灞炪€? );
            else
                Buffer.Add( "褰撴敹闆嗗埌鑷冲皯 " + infestation.MetalHarvesterCanHold + " 鍚庡皢杩斿洖鍏舵嘲鍒╁銆? );
        }
    }
}
