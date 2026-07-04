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
                    Buffer.Add( "吞噬星球！这将彻底摧毁该星球并将其从星系中移除。" );
                else if ( data.Effect == ZenithMinerEffect.RavagePlanet )
                    Buffer.Add( "蹂躏星球！这将破坏该星球并移除其大部分资源，但它仍将是星系的一部分。" );
                else if ( data.Effect == ZenithMinerEffect.SlowShipsOnPlanet )
                    Buffer.Add( "增加行星引力以永久 " ).Add( "减慢", "a1ffa1" ).Add( " 该星球上所有舰船的速度。" );
                else if ( data.Effect == ZenithMinerEffect.SpeedupShipsOnPlanet )
                    Buffer.Add( "降低行星引力以永久 " ).Add( "加速", "a1ffa1" ).Add( " 该星球上所有舰船的速度。" );
                else if ( data.Effect == ZenithMinerEffect.MakePlanetNomadic )
                    Buffer.Add( "使该星球像游牧星球一样在星系中移动。" );
                else
                    Buffer.Add( "TODO: 为此效果定义附加数据 " + data.Effect );
                if ( RelatedEntityTypeData.GetHasTag( "ZenithMinerStationary" ) )
                {
                    Buffer.Add( "矿工将在 " ).AddHoursAndMinutes( data.RemainingDuration, "aaffaa" ).Add( " 后完成。" );
                }
                else
                    Buffer.Add( "矿工将部署其钻头并开始开采星球，一旦消灭所有附近敌人。钻头部署后将需要 " ).AddHoursAndMinutes( data.RemainingDuration, "aaffaa" ).Add( " 秒。" );
            }
            else if ( RelatedEntityTypeData.GetHasTag( "ZenithMinerProbe" ) )
            {
                Buffer.Add( "天顶矿工将在 " ).AddHoursAndMinutes( data.RemainingDuration, "aaffaa" ).Add( " 后到达。" );
            }
            return;
        }
    }
}
