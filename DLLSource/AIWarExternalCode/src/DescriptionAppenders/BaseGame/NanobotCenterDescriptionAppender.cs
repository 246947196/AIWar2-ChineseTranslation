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
                Buffer.Add( "璇锋殏鍋滄父鎴忎互鏌ョ湅闄勫姞淇℃伅" );
                return;
            }
            NanocaustFactionBaseInfo mgr = facOrNull.TryGetExternalBaseInfoAs<NanocaustFactionBaseInfo>();

            if ( mgr == null )
            {
                Buffer.Add( "鏈壘鍒?NanocaustFactionBaseInfo锛? );
                return;
            }
            Buffer.Add( "姝ょ撼绫虫満鍣ㄤ汉涓績姝ｅ湪鎻愪緵 " ).Add( (mgr.StrengthPerNanobotCenter.Display[RelatedEntityOrNull.PrimaryKeyID] / 1000).ToString(), "a1ffa1" ).Add( " 鎴樺姏銆? );
            //            Buffer.Add("State " + mgr.state);
        }
    }
}
