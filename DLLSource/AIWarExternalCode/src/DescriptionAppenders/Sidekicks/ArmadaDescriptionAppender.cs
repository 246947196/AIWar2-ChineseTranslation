using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ArmadaDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            int debugCode = 0;
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
                debugCode = 100;
                SafeSquadWrapper RelatedEntityWrapper = SafeSquadWrapper.Create( RelatedEntityOrNull );
                TooltipDetail detailLevel = EntityText.Detail;
                
                ArmadaPerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<ArmadaPerUnitBaseInfo>();
                if ( data == null )
                {
                    Buffer.Add("无数据");
                    return;
                }
                
                Faction faction = RelatedEntityOrNull.PlanetFaction.Faction;
                if (RelatedEntityTypeData.GetHasTag("DysonFlagship") )
                {
                
                }
                if (RelatedEntityTypeData.GetHasTag("ArmadaProducer") )
                {
                    int interval = data.ProducerNextTransportTime - World_AIW2.Instance.GameSecond;
                    Buffer.Add("此生产者将在 ").Add( interval, "a1ffa1" ).Add(" 秒后生成新的运输船。");
                    if ( faction.StoredMetal < 5000 )
                    {
                        Buffer.Add("仅金属模式", "ff3366");
                    }
                    return;
                }

                ArmadaFactionBaseInfo globaldata = faction.TryGetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
                if ( globaldata == null )
                    return;
                debugCode = 200;
                if ( RelatedEntityTypeData.GetHasTag("BoostsRangerCap") ||
                     RelatedEntityTypeData.GetHasTag("BoostsDireRangerCap") )
                {
                    debugCode = 300;
                    Dictionary<Planet, int> rangerDict = globaldata.RangersPerPlanet.GetDisplayDict();
                    bool printedIntro = false;
                    foreach ( KeyValuePair<Planet, int> kv in rangerDict )
                    {
                        if ( !printedIntro)
                        {
                            printedIntro = true;
                            Buffer.Add("您帝国所有游骑兵的统计：\n");
                        }
                        Buffer.Add("\t").Add(kv.Key.Name, "a1a1ff").Add(": ").Add( kv.Value, "a1ffa1" ).Add("\n");
                        continue;
                    }
                    return;
                }
                debugCode = 400;
                if ( RelatedEntityTypeData.GetHasTag("SwarmLure") )
                {
                    debugCode = 500;
                    int timeLeft = data.TimeForNextSwarmSummon - World_AIW2.Instance.GameSecond;
                    Buffer.Add("此诱饵将在 ").Add( timeLeft, "ffa1a1" ).Add(" 秒后吸引新的一群蝗虫。\n");
                    int totalLocusts = 0;
                    int totalHops = 0;
                    Dictionary<Planet, int> totalLocustsDict = globaldata.LocustsPerLure.GetDisplayDict();
                    Dictionary<Planet, int> totalHopsDict = globaldata.TotalHopsForLocustsPerLure.GetDisplayDict();
                    debugCode = 600;
                    foreach ( KeyValuePair<Planet, int> pair in totalLocustsDict )
                    {
                        if ( pair.Key != RelatedEntityOrNull.Planet)
                            continue;
                        totalLocusts = pair.Value;
                    }
                    foreach ( KeyValuePair<Planet, int> pair in totalHopsDict )
                    {
                        if ( pair.Key != RelatedEntityOrNull.Planet)
                            continue;
                        totalHops = pair.Value;
                    }
                    if ( totalLocusts <= 0 )
                    {
                        Buffer.Add("目前没有蝗虫正在前往此诱饵。\n");
                    }
                    else
                    {
                        int averageDist = totalHops / totalLocusts;
                        string proximityLabel, proximityColor;
                        if ( averageDist <= 1 )      { proximityLabel = "即将到达"; proximityColor = "a1ffa1"; }
                        else if ( averageDist <= 3 ) { proximityLabel = "附近";        proximityColor = "ccff66"; }
                        else if ( averageDist <= 6 ) { proximityLabel = "途中";      proximityColor = "ffdd66"; }
                        else                         { proximityLabel = "遥远";        proximityColor = "ff9944"; }
                        Buffer.Add( totalLocusts, "a1ffa1" ).Add(" 只蝗虫正在途中 — 平均 ")
                            .Add( averageDist.ToString(), proximityColor ).Add(" 颗星球距离（")
                            .Add( proximityLabel, proximityColor ).Add("）\n");
                    }
                }
                debugCode = 600;
                if ( RelatedEntityTypeData.GetHasTag("ArmadaLocust") )
                {
                    if ( data.LocustDestination != null )
                        Buffer.Add("此蝗虫正 swarm 向 ").Add( data.LocustDestination.Name).Add("。\n");
                    else
                        Buffer.Add("此蝗虫已失控。\n");
                }
                debugCode = 700;
                if ( RelatedEntityTypeData.GetHasTag("ArmadaMine") )
                {
                    int timeLeft = data.MineFinishTime - World_AIW2.Instance.GameSecond;
                    Buffer.Add("此矿井将在 ").Add( timeLeft ).Add(" 秒后完成。\n");
                }
                debugCode = 800;
                if ( RelatedEntityTypeData.GetHasTag("ArmadaTransport") )
                {
                    Buffer.Add("正在从矿井运输资源。");
                    if ( data.MetalTransported > 0 )
                        Buffer.Add("此运输船携带 " ).Add( data.MetalTransported, "ccccee" ).Add(" 金属。" );
                    if ( data.ScienceTransported > 0 )
                        Buffer.Add("此运输船携带 " ).Add( data.ScienceTransported, "7CE9FF" ).Add(" 科技。" );
                    if ( data.HackingTransported > 0 )
                        Buffer.Add("此运输船携带 " ).Add( data.HackingTransported, "dd3377" ).Add(" 入侵。" );
                    if ( data.TiberiumTransported > 0 )
                        Buffer.Add("此运输船携带 " ).Add(data.TiberiumTransported.ToString(), RelatedEntityOrNull.PlanetFaction.Faction.Resource1Color).Add(" 钛矿。" );

                }
                debugCode = 900;
                if ( RelatedEntityTypeData.KillsToTriggerTransformation > 0 )
                {
                    Buffer.Add(" 此舰队已击杀 ").Add( data.UnitsKilled, "a1ffa1" ).Add(" 个单位；在击杀 ").Add( RelatedEntityTypeData.KillsToTriggerTransformation, "ffa1a1" ).Add(" 个后将变形为 ").Add(RelatedEntityTypeData.TransformAfterKills.GetDisplayName()).Add("。");
                }
                debugCode = 1000;
                if ( RelatedEntityTypeData.GetHasTag("ArmadaStarbase") && data != null )//&& detailLevel >= TooltipDetail.Full)
                {
                    if ( data.RangerMetal > 0 && data.DireRangerMetal > 0 )
                    {
                        Buffer.Add("<size=80%>我们拥有 ").Add( data.RangerMetal, "999999" ).Add(" 游骑兵金属和 ").Add( data.DireRangerMetal, "999999" ).Add(" 精英游骑兵金属。</size> ");  
                    }
                    else if ( data.RangerMetal > 0 )
                        Buffer.Add("<size=80%>我们拥有 ").Add( data.RangerMetal, "999999" ).Add(" 金属用于建造游骑兵。</size> ");
                    else if ( data.DireRangerMetal > 0 )
                        Buffer.Add("<size=80%>我们拥有 ").Add( data.DireRangerMetal, "999999" ).Add(" 金属用于建造精英游骑兵。</size> ");
                    if ( data.DireRangerMetal > 0 || data.RangerMetal > 0)
                        Buffer.Add("\n");
                    if ( globaldata.RangersPerStarbase.GetDisplayDict()[RelatedEntityWrapper] > 0 )
                        Buffer.Add("<size=80%>当前支持 ").Add( globaldata.RangersPerStarbase.GetDisplayDict()[RelatedEntityWrapper], "999999" ).Add(" / " ).Add( data.RangerCap, "a1ffa1" ).Add(" 游骑兵防御此星堡。</size> ");
                    if ( globaldata.DireRangersPerStarbase.GetDisplayDict()[RelatedEntityWrapper] > 0 )
                        Buffer.Add("<size=80%>当前支持 ").Add( globaldata.DireRangersPerStarbase.GetDisplayDict()[RelatedEntityWrapper], "999999" ).Add(" / " ).Add( data.DireRangerCap, "a1ffa1" ).Add(" 精英游骑兵防御此星堡。</size> ");
                    if ( globaldata.RangersPerStarbase.GetDisplayDict()[RelatedEntityWrapper] > 0 ||  globaldata.DireRangersPerStarbase.GetDisplayDict()[RelatedEntityWrapper] > 0 )
                        Buffer.Add("\n");
                }
                
            }
            catch ( Exception e ) { ArcenDebugging.ArcenDebugLogSingleLine("Whoops? exception " + e.ToString() + "in ArmadaDescriptionAppender. Debug code " + debugCode, Verbosity.DoNotShow );}

            return;
        }
    }
}
