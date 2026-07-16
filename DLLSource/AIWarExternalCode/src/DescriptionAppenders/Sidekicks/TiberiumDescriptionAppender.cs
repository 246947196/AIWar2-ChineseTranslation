using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class TiberiumDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            try
            {
                bool debug = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" ) || (Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Templar ));
                if ( RelatedEntityTypeData == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "No type data?", Verbosity.DoNotShow );
                    return;
                }
                if ( RelatedEntityOrNull == null )
                    return;
                SafeSquadWrapper RelatedEntityWrapper = SafeSquadWrapper.Create( RelatedEntityOrNull );
                TooltipDetail detailLevel = EntityText.Detail;

                TiberiumPerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<TiberiumPerUnitBaseInfo>();
                if ( data == null )
                {
                    Buffer.Add("无数据");
                    return;
                }
                Faction faction = RelatedEntityOrNull.PlanetFaction.Faction;
                TiberiumFactionBaseInfo globaldata = faction.TryGetExternalBaseInfoAs<TiberiumFactionBaseInfo>();
                if ( globaldata == null )
                    return;
                if ( RelatedEntityTypeData.GetHasTag("TiberiumVein") )
                {
                    Buffer.Add("此矿脉有 ").Add( data.Points, "ffaaff" ).Add(" 点数将用于 ").Add( data.NextUpgrade.ToFriendlyString(), "cc2277" ).Add("。" );
                    if ( data.AutoDefenseBuildPoints > 0 )
                    {
                        Buffer.Add("矿脉还在建造单位以保卫其周围领地；它有 ").Add( data.AutoDefenseBuildPoints, "a1ffa1" ).Add( " 防御建造点数。" );
                    }
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag("TiberiumSummoner") )
                {
                    int seconds = data.TimeForNextSpawn - World_AIW2.Instance.GameSecond;
                    Buffer.Add("此召唤者将在 ").Add( seconds, "a1ffa1" ).Add(" 秒后生产一艘泰德里安舰船。" );
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag("TiberiumDropship") )
                {
                    Planet dest = RelatedEntityOrNull.GetDestinationPlanet();
                    if ( dest != RelatedEntityOrNull.Planet )
                        Buffer.Add("此运输船正在前往 ").Add( dest.ToString(), "a1ffa1" ).Add( "。" );
                    return;
                }

            }
            catch ( Exception ) { ArcenDebugging.ArcenDebugLogSingleLine("Whoops? exception in ArmadaDescriptionAppender", Verbosity.DoNotShow );}

            return;
        }
    }
}
