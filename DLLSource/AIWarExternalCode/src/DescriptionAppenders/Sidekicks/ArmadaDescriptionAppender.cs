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
                    Buffer.Add("no data");
                    return;
                }
                
                Faction faction = RelatedEntityOrNull.PlanetFaction.Faction;
                if (RelatedEntityTypeData.GetHasTag("DysonFlagship") )
                {
                
                }
                if (RelatedEntityTypeData.GetHasTag("ArmadaProducer") )
                {
                    int interval = data.ProducerNextTransportTime - World_AIW2.Instance.GameSecond;
                    Buffer.Add("This producer will spawn a new transport in ").Add( interval, "a1ffa1" ).Add(" seconds. ");
                    if ( faction.StoredMetal < 5000 )
                    {
                        Buffer.Add("Metal-Only Mode", "ff3366");
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
                            Buffer.Add("An accounting of all your empire's rangers:\n");
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
                    Buffer.Add("This Lure will attract a new swarm of locusts in ").Add( timeLeft, "ffa1a1" ).Add(" seconds.\n");
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
                        Buffer.Add("No locusts are currently en route to this lure.\n");
                    }
                    else
                    {
                        int averageDist = totalHops / totalLocusts;
                        string proximityLabel, proximityColor;
                        if ( averageDist <= 1 )      { proximityLabel = "arriving soon"; proximityColor = "a1ffa1"; }
                        else if ( averageDist <= 3 ) { proximityLabel = "nearby";        proximityColor = "ccff66"; }
                        else if ( averageDist <= 6 ) { proximityLabel = "en route";      proximityColor = "ffdd66"; }
                        else                         { proximityLabel = "distant";        proximityColor = "ff9944"; }
                        Buffer.Add( totalLocusts, "a1ffa1" ).Add(" locusts en route — avg. ")
                            .Add( averageDist.ToString(), proximityColor ).Add(" planets away (")
                            .Add( proximityLabel, proximityColor ).Add(")\n");
                    }
                }
                debugCode = 600;
                if ( RelatedEntityTypeData.GetHasTag("ArmadaLocust") )
                {
                    if ( data.LocustDestination != null )
                        Buffer.Add("This locust is swarming to ").Add( data.LocustDestination.Name).Add(".\n");
                    else
                        Buffer.Add("This locust has gone wild.\n");
                }
                debugCode = 700;
                if ( RelatedEntityTypeData.GetHasTag("ArmadaMine") )
                {
                    int timeLeft = data.MineFinishTime - World_AIW2.Instance.GameSecond;
                    Buffer.Add("This mine will be finished in  ").Add( timeLeft ).Add(" seconds.\n");
                }
                debugCode = 800;
                if ( RelatedEntityTypeData.GetHasTag("ArmadaTransport") )
                {
                    Buffer.Add("Transporting resources from a mine. ");
                    if ( data.MetalTransported > 0 )
                        Buffer.Add("This transport has " ).Add( data.MetalTransported, "ccccee" ).Add(" Metal. ");
                    if ( data.ScienceTransported > 0 )
                        Buffer.Add("This transport has " ).Add( data.ScienceTransported, "7CE9FF" ).Add(" Science. ");
                    if ( data.HackingTransported > 0 )
                        Buffer.Add("This transport has " ).Add( data.HackingTransported, "dd3377" ).Add(" Hacking. ");
                    if ( data.TiberiumTransported > 0 )
                        Buffer.Add("This transport has " ).Add(data.TiberiumTransported.ToString(), RelatedEntityOrNull.PlanetFaction.Faction.Resource1Color).Add(" Tiberium. ");

                }
                debugCode = 900;
                if ( RelatedEntityTypeData.KillsToTriggerTransformation > 0 )
                {
                    Buffer.Add(" This fleet has killed ").Add( data.UnitsKilled, "a1ffa1" ).Add(" units; after it kills ").Add( RelatedEntityTypeData.KillsToTriggerTransformation, "ffa1a1" ).Add(" then we will transform into ").Add(RelatedEntityTypeData.TransformAfterKills.GetDisplayName()).Add(". ");
                }
                debugCode = 1000;
                if ( RelatedEntityTypeData.GetHasTag("ArmadaStarbase") && data != null )//&& detailLevel >= TooltipDetail.Full)
                {
                    if ( data.RangerMetal > 0 && data.DireRangerMetal > 0 )
                    {
                        Buffer.Add("<size=80%>We have ").Add( data.RangerMetal, "999999" ).Add(" ranger metal and ").Add( data.DireRangerMetal, "999999" ).Add(" dire ranger metal.</size> ");  
                    }
                    else if ( data.RangerMetal > 0 )
                        Buffer.Add("<size=80%>We have ").Add( data.RangerMetal, "999999" ).Add(" metal to spend on a Ranger.</size> ");
                    else if ( data.DireRangerMetal > 0 )
                        Buffer.Add("<size=80%>We have ").Add( data.DireRangerMetal, "999999" ).Add(" metal to spend on a Dire Ranger.</size> ");
                    if ( data.DireRangerMetal > 0 || data.RangerMetal > 0)
                        Buffer.Add("\n");
                    if ( globaldata.RangersPerStarbase.GetDisplayDict()[RelatedEntityWrapper] > 0 )
                        Buffer.Add("<size=80%>Currently supporting ").Add( globaldata.RangersPerStarbase.GetDisplayDict()[RelatedEntityWrapper], "999999" ).Add(" of " ).Add( data.RangerCap, "a1ffa1" ).Add(" rangers defending this starbase.</size> ");
                    if ( globaldata.DireRangersPerStarbase.GetDisplayDict()[RelatedEntityWrapper] > 0 )
                        Buffer.Add("<size=80%>Currently supporting ").Add( globaldata.DireRangersPerStarbase.GetDisplayDict()[RelatedEntityWrapper], "999999" ).Add(" of " ).Add( data.DireRangerCap, "a1ffa1" ).Add(" dire rangers defending this starbase.</size> ");
                    if ( globaldata.RangersPerStarbase.GetDisplayDict()[RelatedEntityWrapper] > 0 ||  globaldata.DireRangersPerStarbase.GetDisplayDict()[RelatedEntityWrapper] > 0 )
                        Buffer.Add("\n");
                }
                
            }
            catch ( Exception e ) { ArcenDebugging.ArcenDebugLogSingleLine("Whoops? exception " + e.ToString() + "in ArmadaDescriptionAppender. Debug code " + debugCode, Verbosity.DoNotShow );}

            return;
        }
    }
}
