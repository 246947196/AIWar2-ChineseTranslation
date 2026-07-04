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
                Buffer.Add( "This unit is trying to decide where to bore a wormhole" );
                return;
            }
            if ( RelatedEntityOrNull.TypeData.GetHasTag( "MobileWormholeBorer" ) )
                Buffer.Add( "I am a going to create a new wormhole from " ).Add( start.Name, "a1ffa1" ).Add( " to " ).Add( dest.Name, "a1a1ff" ).Add( ". " );
            else
            {
                Faction facOrNull = RelatedEntityOrNull.GetFactionOrNull_Safe();
                if ( facOrNull == null )
                    return;
                AISentinelsCoreData factionExternal = facOrNull.TryGetAISentinelsCoreData()?.SentinelInfo;
                AIDifficulty difficulty = factionExternal.AIDifficulty;
                int timeSinceCreated = World_AIW2.Instance.GameSecond - RelatedEntityOrNull.GameSecondCreated;
                int timeLeft = (difficulty.WormholeBorerCompletionTime - timeSinceCreated).IntValue;
                Buffer.Add( "This structure will create a new wormhole from " ).Add( start.Name, "a1ffa1" ).Add( " to " ).Add( dest.Name, "a1a1ff" ).Add( " in " ).AddHoursAndMinutes( timeLeft, "ffa1a1" ).Add( ". " );
            }
        }
    }
}
