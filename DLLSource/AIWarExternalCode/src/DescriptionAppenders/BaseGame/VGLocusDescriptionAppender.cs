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
                Buffer.Add( "The Dark Spire is in Conquest Mode, so new Vengeance Generators will still be vulnerable after fully warping in." );
            }
            else
                Buffer.Add( "If you allow the rest of the Vengeance Generator to warp in then it will become invulnerable." );
            Buffer.Add( " Locus is " + percent + " percent of the way to fully warping in" );
        }
    }
}
