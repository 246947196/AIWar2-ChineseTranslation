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
                Buffer.Add( "暗星处于征服模式，因此新的复仇生成器在完全折跃进入后仍然会很脆弱。" );
            }
            else
                Buffer.Add( "如果你允许复仇生成器的其余部分折跃进入，它将变得无敌。" );
            Buffer.Add( "位置点正在折跃进入中，已完成 " + percent + "%" );
        }
    }
}
