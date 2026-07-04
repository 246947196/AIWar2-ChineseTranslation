using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class TeliumDescriptionAppender : GameEntityDescriptionAppenderBase
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
            MacrophagePerTeliumBaseInfo tData = RelatedEntityOrNull.TryGetExternalBaseInfoAs< MacrophagePerTeliumBaseInfo>();
            if ( tData == null )
            {
                Buffer.Add( "姝ゆ嘲鍒╁鐨?tData 涓虹┖銆傝繖鏄竴涓?BUG銆? );
                return;
            }
            int cost = tData.MetalForNextBuild;
            int harvestersToEnrage, harvesterLimit, sporesToRelease;
            if ( RelatedEntityOrNull.TypeData.GetHasTag( MacrophageFactionBaseInfoCore.SpireTeliumTag ) )
            {
                harvestersToEnrage = infestation.SpireHarvestersToEnrage;
                harvesterLimit = infestation.SpireHarvesterLimitBeforeEnraging;
                sporesToRelease = infestation.SpireSporesPerRelease;
            }
            else
            {
                harvestersToEnrage = infestation.HarvestersToEnrage;
                harvesterLimit = infestation.HarvesterLimitBeforeEnraging;
                sporesToRelease = infestation.SporesPerRelease;
            }
            Buffer.Add( tData.CurrentMetal.ToString( "0.##" ) + "/" + cost.ToString( "0.##" ) + " 閲戝睘锛岃窛绂讳笅娆″缓閫犱簨浠躲€? );
            if ( infestation.isLoner )
                Buffer.Add( "姝ゆ嘲鍒╁鍙兘寤洪€犻噰闆嗗櫒銆? );
            else if ( tData.CurrentHarvesters == 0 )
                Buffer.Add( "姝ゆ嘲鍒╁灏嗗湪涓嬫浜嬩欢涓缓閫犱竴涓噰闆嗗櫒锛屽洜涓哄畠鐩墠娌℃湁閲囬泦鍣ㄣ€? );
            else
                Buffer.Add( "姝ゆ嘲鍒╁鏈?" + infestation.GetSporeChance( tData.CurrentHarvesters ) + "% 鐨勫嚑鐜囧湪涓嬫浜嬩欢涓噴鏀?" + sporesToRelease + " 涓瀛愶紝鑰屼笉鏄缓閫犱竴涓噰闆嗗櫒銆? );

            if ( infestation.isLoner )
                Buffer.Add( "姝ゆ嘲鍒╁鐩墠鏀寔 " + tData.CurrentHarvesters + " 涓噰闆嗗櫒锛屽叾閲囬泦鍣ㄤ粎鍦ㄦ娉板埄濮嗚鎽ф瘉鏃舵墠浼氭毚鎬掋€? );
            else
                Buffer.Add( "姝ゆ嘲鍒╁鐩墠鏀寔 " + tData.CurrentHarvesters + "/" + (harvesterLimit - 1) + " 涓噰闆嗗櫒銆傚綋瓒呰繃瀹归噺鏃讹紝瀹冨皢鏆存€?" + harvestersToEnrage + " 涓噰闆嗗櫒銆? );

            if ( infestation.isBerserk )
                Buffer.Add( "瀹冨綋鍓嶅浜?" ).StartColor( UnityEngine.Color.red ).Add( "鐙傛毚" ).EndColor().Add( " 鐘舵€侊紝閲戝睘娑堣€楅檷浣?" + (100 - (100 / infestation.BerserkCostDivisor)) + "%锛? );
        }
    }
}
