using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class VGLocusDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;
            if ( DarkSpireFactionBaseInfo.LocusWarpInTime == 0 )
                return;
            int percent = (100 * RelatedEntityOrNull.GetSecondsSinceCreation()) / DarkSpireFactionBaseInfo.LocusWarpInTime;
            DarkSpireFactionBaseInfo dsdata = RelatedEntityOrNull.TryGetFactionBaseInfoOrNullAs_Safe<DarkSpireFactionBaseInfo>();
            if ( dsdata == null )
                return;
            if ( dsdata.ConquestMode )
            {
                Buffer.Add( "鏆楁槦澶勪簬寰佹湇妯″紡锛屽洜姝ゆ柊鐨勫浠囩敓鎴愬櫒鍦ㄥ畬鍏ㄦ姌璺冭繘鍏ュ悗浠嶇劧浼氬緢鑴嗗急銆? );
            }
            else
                Buffer.Add( "濡傛灉浣犲厑璁稿浠囩敓鎴愬櫒鐨勫叾浣欓儴鍒嗘姌璺冭繘鍏ワ紝瀹冨皢鍙樺緱鏃犳晫銆? );
            Buffer.Add( "浣嶇疆鐐规鍦ㄦ姌璺冭繘鍏ヤ腑锛屽凡瀹屾垚 " + percent + "%" );
        }
    }
}
