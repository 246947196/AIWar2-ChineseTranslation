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
                Buffer.Add( "Please unpause the game to view additional information" );
                return;
            }
            NanocaustFactionBaseInfo mgr = facOrNull.TryGetExternalBaseInfoAs<NanocaustFactionBaseInfo>();

            if ( mgr == null )
            {
                Buffer.Add( "No NanocaustFactionBaseInfo found!" );
                return;
            }
            Buffer.Add( "This Nanobot Center is supporting " ).Add( (mgr.StrengthPerNanobotCenter.Display[RelatedEntityOrNull.PrimaryKeyID] / 1000).ToString(), "a1ffa1" ).Add( " strength. " );
            //            Buffer.Add("State " + mgr.state);
        }
    }
}
