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
                Buffer.Add( "鏃犳硶鍦ㄦ澶勬壘鍒?MacrophageFactionBaseInfo銆傝繖鏄竴涓?BUG" );
                return;
            }
            if ( infestation.Telia == null )
            {
                Buffer.Add( "璇锋殏鍋滄父鎴忎互鏌ョ湅鍏充簬姝ゅ瀛愮殑闄勫姞淇℃伅" );
                return;
            }
            MacrophagePerSporeBaseInfo sDataOrNull = RelatedEntityOrNull.TryGetExternalBaseInfoAs<MacrophagePerSporeBaseInfo>();
            if ( sDataOrNull == null )
            {
                Buffer.Add( "濡傛灉浣犲垰鍒氬姞杞戒簡娓告垙锛岃鍙栨秷鏆傚仠锛屾暟鎹簲璇ヤ細濉厖" );
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
                    Buffer.Add( "姝ゅ瀛愭潵鑷綅浜?" + tSquad.GetPlanetName_Safe() + " 涓婄殑娉板埄濮嗐€傚鏋滄湁鏉ヨ嚜 " + infestation.NumDifferentTeliaRequired + " 涓笉鍚屾槦鐞冪殑瀛㈠瓙锛屽畠浠皢褰㈡垚涓€涓柊鐨勬嘲鍒╀簹銆? );
            }

            if ( infestation.SporeLifespan > 0 )
            {
                int secondsLeft = (infestation.SporeLifespan + sDataOrNull.SpawnTime) - World_AIW2.Instance.GameSecond;
                Buffer.Add( $"姝ゅ瀛愭湁鏈夐檺鐨勭敓鍛藉懆鏈燂紝鍗冲皢娑堝け锛歿(secondsLeft / 60).ToString("0")}:{(secondsLeft % 60).ToString("00")}" );
            }
        }
    }
}
