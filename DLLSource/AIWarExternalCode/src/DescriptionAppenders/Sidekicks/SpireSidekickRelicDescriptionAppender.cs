using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class SpireSidekickRelicDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            // Make sure we are getting an entity.
            if ( RelatedEntityOrNull == null )
                return;
            SpireSidekickPerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<SpireSidekickPerUnitBaseInfo>();
            if ( data.DestinationPlanet != null &&
                 data.DestinationPlanet != RelatedEntityOrNull.Planet )
                Buffer.Add( "此遗物正在前往 " ).Add( data.DestinationPlanet.Name ).Add( " 的途中。" );
            if ( data.MustBuildOnStartPlanet )
                Buffer.Add( "此遗物的能源供应已被AI破坏，必须在此星球上建造一座尖塔城市。" );
        }
    }
    public class SpireSidekickOutpostDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            // Make sure we are getting an entity.

            if ( RelatedEntityTypeData == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "No type data?", Verbosity.DoNotShow );
                return;
            }
            if ( RelatedEntityOrNull == null )
                return;
            SafeSquadWrapper RelatedEntityWrapper = SafeSquadWrapper.Create( RelatedEntityOrNull );
            TooltipDetail detailLevel = EntityText.Detail;
                
            SpireSidekickPerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<SpireSidekickPerUnitBaseInfo>();
            if ( data == null )
            {
                Buffer.Add("无数据");
                return;
            }
                
            Faction faction = RelatedEntityOrNull.PlanetFaction.Faction;
            SpireSidekickFactionBaseInfo globaldata = faction.TryGetExternalBaseInfoAs<SpireSidekickFactionBaseInfo>();
            if ( globaldata == null )
            {
                return;
            }
            if ( RelatedEntityTypeData.GetHasTag("BoostsRangerCap") ||
                 RelatedEntityTypeData.GetHasTag("BoostsDireRangerCap") )
            {
                if ( data.RangerMetal > 0 ||  data.DireRangerMetal > 0 )
                {
                    Buffer.Add("<size=80%>我们有 ").Add( data.RangerMetal, "999999" ).Add(" 资源用于水晶守卫者，").Add( data.DireRangerMetal, "999999" ).Add(" 资源用于寒颤者。</size> ");  
                }
                Dictionary<Planet, int> rangerDict = globaldata.RangersPerPlanet.GetDisplayDict();
                bool printedIntro = false;
                foreach ( KeyValuePair<Planet, int> kv in rangerDict )
                {
                    if ( !printedIntro)
                    {
                        printedIntro = true;
                        Buffer.Add("您所有防御单位的统计：\n");
                    }
                    Buffer.Add("\t").Add(kv.Key.Name, "a1a1ff").Add(": ").Add( kv.Value, "a1ffa1" ).Add("\n");
                    continue;
                }

                return;
            }
        }
    }
    
    public class SpireSidekickHubDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            // Make sure we are getting an entity.
            if ( RelatedEntityOrNull == null )
                return;
            if ( SpireSidekickFactionBaseInfo.Instance == null )
            {
                Buffer.Add( "SpireSidekickFactionBaseInfo.Instance 由于某种原因为空！" );
                return;
            }
            // Make sure we have our city list before continuing.
            if ( SpireSidekickFactionBaseInfo.Instance.SpireCities.Count <= 0 )
            {
                Buffer.Add( "尖塔可能尚未初始化？显示没有城市。请取消暂停游戏，同时请报告此错误。" );
                return;
            }

            SpireSidekickPerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<SpireSidekickPerUnitBaseInfo>();
            if ( data == null )
            {
                Buffer.Add("无数据");
                return;
            }
            Faction faction = RelatedEntityOrNull.PlanetFaction.Faction;
            SpireSidekickFactionBaseInfo globaldata = faction.TryGetExternalBaseInfoAs<SpireSidekickFactionBaseInfo>();
            if ( data.RangerMetal > 0 ||  data.DireRangerMetal > 0 )
            {
                Buffer.Add("<size=80%>我们有 ").Add( data.RangerMetal, "999999" ).Add(" 游骑兵金属和 ").Add( data.DireRangerMetal, "999999" ).Add(" 精英游骑兵金属。\n</size> ");  
            }
            Dictionary<Planet, int> rangerDict = globaldata.RangersPerPlanet.GetDisplayDict();
            bool printedIntro = false;
            foreach ( KeyValuePair<Planet, int> kv in rangerDict )
            {
                if ( !printedIntro)
                {
                    printedIntro = true;
                    Buffer.Add("您所有游骑兵的统计：\n");
                }
                Buffer.Add("\t").Add(kv.Key.Name, "a1a1ff").Add(": ").Add( kv.Value, "a1ffa1" ).Add("\n");
                continue;
            }

            SpireSidekickFactionBaseInfo.WriteCityTooltipDetails( Buffer, RelatedEntityOrNull );


        }
    }

}
