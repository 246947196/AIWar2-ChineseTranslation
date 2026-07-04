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
                Buffer.Add( "MacrophageFactionBaseInfo could not be found here. This is a BUG" );
                return;
            }
            MacrophagePerTeliumBaseInfo tData = RelatedEntityOrNull.TryGetExternalBaseInfoAs< MacrophagePerTeliumBaseInfo>();
            if ( tData == null )
            {
                Buffer.Add( "tData for this Telium is null. This is a bug." );
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
            Buffer.Add( tData.CurrentMetal.ToString( "0.##" ) + "/" + cost.ToString( "0.##" ) + " metal until next build event. " );
            if ( infestation.isLoner )
                Buffer.Add( "This Telium is only capable of building Harvesters. " );
            else if ( tData.CurrentHarvesters == 0 )
                Buffer.Add( "This Telium will build a Harvester for its next event as it currently has none. " );
            else
                Buffer.Add( "This Telium has a " + infestation.GetSporeChance( tData.CurrentHarvesters ) + "% chance to spawn " + sporesToRelease + " Spores for its next event instead of a Harvester. " );

            if ( infestation.isLoner )
                Buffer.Add( "This Telium currently supports " + tData.CurrentHarvesters + " Harvesters, and its Harvesters will only enrage when this Telium is destroyed. " );
            else
                Buffer.Add( "This Telium currently supports " + tData.CurrentHarvesters + "/" + (harvesterLimit - 1) + " Harvesters. When it's over capacity, it will enrage " + harvestersToEnrage + " Harvesters. " );

            if ( infestation.isBerserk )
                Buffer.Add( "It is currently " ).StartColor( UnityEngine.Color.red ).Add( "berserk" ).EndColor().Add( ", reducing its metal costs by " + (100 - (100 / infestation.BerserkCostDivisor)) + "%! " );
        }
    }
}
