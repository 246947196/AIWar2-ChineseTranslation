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
                Buffer.Add( "无法在此处找到MacrophageFactionBaseInfo。这是一个错误" );
                return;
            }
            MacrophagePerTeliumBaseInfo tData = RelatedEntityOrNull.TryGetExternalBaseInfoAs< MacrophagePerTeliumBaseInfo>();
            if ( tData == null )
            {
                Buffer.Add( "此泰利姆的 tData 为空。这是一个错误。" );
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
            Buffer.Add( tData.CurrentMetal.ToString( "0.##" ) + "/" + cost.ToString( "0.##" ) + " 金属，距离下次建造事件" );
            if ( infestation.isLoner )
                Buffer.Add( "此泰利姆只能建造采集器。" );
            else if ( tData.CurrentHarvesters == 0 )
                Buffer.Add( "此泰利姆将在下次事件中建造一个采集器，因为它目前没有采集器。" );
            else
                Buffer.Add( "此泰利姆有 " + infestation.GetSporeChance( tData.CurrentHarvesters ) + "% 的几率在下次事件中释放 " + sporesToRelease + " 个孢子，而不是建造一个采集器。" );

            if ( infestation.isLoner )
                Buffer.Add( "此泰利姆目前支持 " + tData.CurrentHarvesters + " 个采集器，其采集器仅在此泰利姆被摧毁时才会暴怒。" );
            else
                Buffer.Add( "此泰利姆目前支持 " + tData.CurrentHarvesters + "/" + (harvesterLimit - 1) + " 个采集器。当超过容量时，它将暴怒 " + harvestersToEnrage + " 个采集器。" );

            if ( infestation.isBerserk )
                Buffer.Add( "它当前处于" ).StartColor( UnityEngine.Color.red ).Add( "狂暴" ).EndColor().Add( "状态，金属消耗降低 " + (100 - (100 / infestation.BerserkCostDivisor)) + "%！" );
        }
    }
}
