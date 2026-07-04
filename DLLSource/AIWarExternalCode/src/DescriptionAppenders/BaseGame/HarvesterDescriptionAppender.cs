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
                Buffer.Add( "MacrophageFactionBaseInfo could not be found here. This is a BUG" );
                return;
            }
            MacrophagePerHarvesterBaseInfo hData = RelatedEntityOrNull.TryGetExternalBaseInfoAs<MacrophagePerHarvesterBaseInfo>();
            if ( hData == null )
            {
                Buffer.Add( "hData for this Harvester is null. If you have just loaded a game then please unpause it and it should fill in" );
                return;
            }
            Buffer.Add( "This Harvester has collected " + hData.CurrentMetal + " metal " );
            if ( hData.ReturningToTelium )
                Buffer.Add( "and is currently returning to its Telium to deposit metal. " );
            else
                Buffer.Add( "and will return to its Telium when it has collected at least " + infestation.MetalHarvesterCanHold + ". " );
        }
    }
}
