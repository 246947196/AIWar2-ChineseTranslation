using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class InstigatorBaseDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;
            InstigatorPerUnitBaseInfo localPerUnitData = RelatedEntityOrNull.TryGetExternalBaseInfoAs<InstigatorPerUnitBaseInfo>();
            if ( localPerUnitData == null )
                return;
            if ( localPerUnitData.InstigatorEffectIndex == -1 )
                return;

            Faction facOrNull = RelatedEntityOrNull.GetFactionOrNull_Safe();
            if ( facOrNull == null )
            {
                Buffer.Add( "Please unpause the game to view additional information about this" );
                return;
            }

            InstigatorEffectData rowdata = InstigatorDataTable.Instance.GetRowById( localPerUnitData.InstigatorEffectIndex );
            Faction faction = null;
            InstigatorFactionBaseInfo factionData = facOrNull.GetExternalBaseInfoAs<InstigatorFactionBaseInfo>();
            if ( factionData.AIFactionIndexForNextSpawn != -1 )
                faction = World_AIW2.Instance.GetFactionByIndex( factionData.AIFactionIndexForNextSpawn );
            Buffer.Add( rowdata.GetHoverText( faction ) );
            // if(localPerUnitData.CumulativeEffectSoFar > 0)
            // {
            //     //This number isn't really meaningful, so lets omit it
            //     Buffer.Add(" The Cumulative Effect of this base so far is " + localPerUnitData.CumulativeEffectSoFar).Add(".");
            // }
            if ( localPerUnitData.NumTimesEffectHappened > 0 )
                Buffer.Add( " This base has triggered <color=#cfd988>" + localPerUnitData.NumTimesEffectHappened + "</color> times." );
        }
    }
}
