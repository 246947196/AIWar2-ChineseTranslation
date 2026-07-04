using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class NecromancerDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            bool debug = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" );
            if ( RelatedEntityOrNull == null )
                return;
            //Buffer.Add( "Necromancer ship\n" );
            if ( RelatedEntityOrNull.TypeData.GetHasTag("Igor") )
            {
                Buffer.Add( "<size=40%>警告：此舰船是艾比号</size>", "808080" );
            }
        }
    }
}
