using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class WormholeBorerAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;
            Planet start = World_AIW2.Instance.GetPlanetByIndex( RelatedEntityOrNull.WBStartPlanet );
            Planet dest = World_AIW2.Instance.GetPlanetByIndex( RelatedEntityOrNull.WBDestinationPlanet );
            if ( start == null || dest == null )
            {
                Buffer.Add( "此单位正在尝试决定在哪里钻孔虫洞" );
                return;
            }
            if ( RelatedEntityOrNull.TypeData.GetHasTag( "MobileWormholeBorer" ) )
                Buffer.Add( "我将从 " ).Add( start.Name, "a1ffa1" ).Add( " 到 " ).Add( dest.Name, "a1a1ff" ).Add( " 创建一个新的虫洞。" );
            else
            {
                Faction facOrNull = RelatedEntityOrNull.GetFactionOrNull_Safe();
                if ( facOrNull == null )
                    return;
                AISentinelsCoreData factionExternal = facOrNull.TryGetAISentinelsCoreData()?.SentinelInfo;
                AIDifficulty difficulty = factionExternal.AIDifficulty;
                int timeSinceCreated = World_AIW2.Instance.GameSecond - RelatedEntityOrNull.GameSecondCreated;
                int timeLeft = (difficulty.WormholeBorerCompletionTime - timeSinceCreated).IntValue;
                Buffer.Add( "此结构将从 " ).Add( start.Name, "a1ffa1" ).Add( " 到 " ).Add( dest.Name, "a1a1ff" ).Add( " 创建一个新的虫洞，耗时 " ).AddHoursAndMinutes( timeLeft, "ffa1a1" ).Add( "。" );
            }
        }
    }
}
