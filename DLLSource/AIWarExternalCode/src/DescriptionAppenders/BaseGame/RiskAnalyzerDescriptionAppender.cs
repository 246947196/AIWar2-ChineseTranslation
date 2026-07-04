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
                hour = " hour";
            else
                hour = " hours";

            Buffer.Add( "When the planet is controlled by the AI, AIP goes up by " + AIPIncrease + " every " + timeInHours );
            Buffer.Add( hour + ". When it is controlled by the player, AIP goes down by " + AIPDecrease + " every " + timeInHours + hour + ". On death, it goes up by " + AIPIncreaseOnDeath + "." );
        }
    }
}
