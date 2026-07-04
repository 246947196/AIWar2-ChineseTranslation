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

            Buffer.Add( "褰撴槦鐞冭AI鎺у埗鏃讹紝AIP姣?" + timeInHours );
            Buffer.Add( hour + " 澧炲姞 " + AIPIncrease + "銆傚綋琚帺瀹舵帶鍒舵椂锛孉IP姣?" + timeInHours + hour + " 鍑忓皯 " + AIPDecrease + "銆傛浜℃椂锛孉IP澧炲姞 " + AIPIncreaseOnDeath + "銆? );
        }
    }
}
