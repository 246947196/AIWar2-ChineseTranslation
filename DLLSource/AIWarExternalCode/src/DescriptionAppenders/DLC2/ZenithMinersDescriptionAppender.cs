using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ZenithMinersDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            bool debug = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" ) || (Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithMiners ));
            if ( RelatedEntityTypeData == null )
                return;
            ZenithMinersPerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<ZenithMinersPerUnitBaseInfo>();
            if ( data == null )
                return;
            Faction faction = RelatedEntityOrNull.GetFactionOrNull_Safe();
            ZenithMinersFactionBaseInfo globaldata = faction.TryGetExternalBaseInfoAs<ZenithMinersFactionBaseInfo>();

            int intensity = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
            ZenithMinersDifficulty diff = ZenithMinersDifficultyTable.Instance.GetRowByIntensity( globaldata.Intensity );

            if ( RelatedEntityTypeData.GetHasTag( "ZenithMinerMobile" ) ||
                 RelatedEntityTypeData.GetHasTag( "ZenithMinerStationary" ) )
            {
                if ( data.Effect == ZenithMinerEffect.DestroyPlanet || data.Effect == ZenithMinerEffect.DestroyDysonSphere || data.Effect == ZenithMinerEffect.DiminishZenithArchitrave )
                    Buffer.Add( "Eating Planet! This will utterly destroy the planet and remove it from the galaxy. " );
                else if ( data.Effect == ZenithMinerEffect.RavagePlanet )
                    Buffer.Add( "Ravaging Planet! This will devastate the planet and remove many of its resources, but it will still be part of the galaxy. " );
                else if ( data.Effect == ZenithMinerEffect.SlowShipsOnPlanet )
                    Buffer.Add( "Increasing planetary gravity to permanently " ).Add( "slow", "a1ffa1" ).Add( " all ships on this planet. " );
                else if ( data.Effect == ZenithMinerEffect.SpeedupShipsOnPlanet )
                    Buffer.Add( "Decreasing planetary gravity to permanently " ).Add( "speedup", "a1ffa1" ).Add( " all ships on this planet. " );
                else if ( data.Effect == ZenithMinerEffect.MakePlanetNomadic )
                    Buffer.Add( "Makes the planet move around the galaxy like a nomad planet. " );
                else
                    Buffer.Add( "TODO: define appender data for effect " + data.Effect );
                if ( RelatedEntityTypeData.GetHasTag( "ZenithMinerStationary" ) )
                {
                    Buffer.Add( "The Miner will be done in " ).AddHoursAndMinutes( data.RemainingDuration, "aaffaa" ).Add( ". " );
                }
                else
                    Buffer.Add( "The Miner will deploy its drill and begin mining the planet once it kills all of its nearby enemies. Once the drill is deployed it will take " ).AddHoursAndMinutes( data.RemainingDuration, "aaffaa" ).Add( " seconds. " );
            }
            else if ( RelatedEntityTypeData.GetHasTag( "ZenithMinerProbe" ) )
            {
                Buffer.Add( "The Zenith Miner will arrive in " ).AddHoursAndMinutes( data.RemainingDuration, "aaffaa" ).Add( ". " );
            }
            return;
        }
    }
}
