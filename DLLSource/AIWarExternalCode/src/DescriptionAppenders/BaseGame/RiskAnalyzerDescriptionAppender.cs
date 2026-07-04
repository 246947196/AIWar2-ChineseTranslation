using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class RiskAnalyzerDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;

            RiskAnalyzerFactionBaseInfo BaseInfo = RelatedEntityOrNull?.PlanetFaction.Faction.TryGetExternalBaseInfoAs<RiskAnalyzerFactionBaseInfo>();
            if ( BaseInfo == null )
                return;

            int timeIntervalForAnalyzer = BaseInfo.timeIntervalForAnalyzer;
            int AIPIncrease = BaseInfo.AIPIncrease;
            int AIPIncreaseOnDeath = BaseInfo.AIPIncreaseOnDeath;
            int AIPDecrease = BaseInfo.AIPDecrease;
            string hour;
            TimeSpan time = TimeSpan.FromSeconds( timeIntervalForAnalyzer );
            double timeInHours = time.TotalHours;

            if ( timeInHours == 1 )
                hour = " 小时";
            else
                hour = " 小时";

            Buffer.Add( "当星球被AI控制时，AIP增加 " + AIPIncrease + " 每 " + timeInHours );
            Buffer.Add( hour + "。当被玩家控制时，AIP减少 " + AIPDecrease + " 每 " + timeInHours + hour + "。死亡时，AIP增加 " + AIPIncreaseOnDeath + "。" );
        }
    }
}
