using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class MarauderOutpostDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;
            if ( MarauderFactionBaseInfo.AllMarauderFactions.Count <= 0 )
                return; //this is a valid condition if the game has just been loaded
            
            MarauderOutpostRaiderPerUnitBaseInfo unitData = RelatedEntityOrNull.TryGetExternalBaseInfoAs<MarauderOutpostRaiderPerUnitBaseInfo>();
            if ( unitData == null )
            {
                Buffer.Add( "绌烘暟鎹紒" );
                return;
            }
            Faction facOrNull = RelatedEntityOrNull.GetFactionOrNull_Safe();
            if ( facOrNull == null )
            {
                Buffer.Add( "绌洪樀钀ワ紒" );
                return;
            }
            MarauderFactionBaseInfo factionData = facOrNull.TryGetExternalBaseInfoAs<MarauderFactionBaseInfo>();
            if ( factionData == null || factionData.RaidersPerOutpost == null )
            {
                Buffer.Add( "绌?MarauderFactionBaseInfo锛? );
                return;
            }

            if ( factionData.NoMark3Outposts )
                return;
            if ( (factionData.PlayerAllied || factionData.aiAllied) && factionData.NoMark3OutpostsOnAlliedPlanets )
                Buffer.Add( "鐩熷弸锛堝嵆浜虹被鍙嬪ソ鎴朅I鍙嬪ソ锛夌殑鎺犲ず鑰呭墠鍝ㄧ珯涓嶈兘鍦ㄦ嫢鏈夌洘鍐涙寚鎸ョ珯鐨勬槦鐞冧笂杈惧埌3绾с€? );

            if ( RelatedEntityOrNull.CurrentMarkLevel == 3 )
            {
                Buffer.Add( "姝ゅ墠鍝ㄧ珯姝ｅ湪鏀寔鍏跺厑璁哥殑 " + factionData.MaxRaidersPerMark3Outpost + " 鍚嶆帬澶鸿€呬腑鐨?" + factionData.RaidersPerOutpost.Display[RelatedEntityOrNull.PrimaryKeyID] + " 鍚嶃€? );
            }
            else
            {
                if ( RelatedEntityOrNull.Planet.GetControllingFactionType() == FactionType.Player && (factionData.PlayerAllied && factionData.NoMark3OutpostsOnAlliedPlanets) )
                {
                    Buffer.Add( "鍦ㄤ綘鎷ユ湁鐨勬槦鐞冧笂鐨勭洘鍙嬫帬澶鸿€呭墠鍝ㄧ珯鏃犳硶鍏呭垎鍙戞尌鍏跺缓閫犺繘鏀昏埌闃熺殑娼滃姏锛涘畠浠湪璇ユ槦鐞冧笂涔熶細寤洪€犳洿灏戠殑鍓嶅摠绔欍€傚畠浠渶瑕佽嚜宸辩殑鏄熺悆鎵嶈兘瀹屽叏鍗囩骇銆? );
                }
                else
                {
                    Buffer.Add( "姝ゅ墠鍝ㄧ珯灏嗗湪杈惧埌3绾у悗寮€濮嬬敓浜ф帬澶鸿€呫€傛帬澶鸿€呮槸鎺犲ず鑰呯敤鏉ュ緛鏈嶆柊鏄熺悆鐨勫己澶ф姢鍗埌銆? );
                }
            }
        }
    }
}
