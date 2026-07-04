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
                Buffer.Add( "This relic is en route to " ).Add( data.DestinationPlanet.Name ).Add( "." );
            if ( data.MustBuildOnStartPlanet )
                Buffer.Add( "This relics power supply was crippled by the AI, and must build a Spire City on this planet." );
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
                Buffer.Add("no data");
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
                    Buffer.Add("<size=80%>We have ").Add( data.RangerMetal, "999999" ).Add(" resource for Crystalline Wardens and ").Add( data.DireRangerMetal, "999999" ).Add(" resource for Shivers.</size> ");  
                }
                Dictionary<Planet, int> rangerDict = globaldata.RangersPerPlanet.GetDisplayDict();
                bool printedIntro = false;
                foreach ( KeyValuePair<Planet, int> kv in rangerDict )
                {
                    if ( !printedIntro)
                    {
                        printedIntro = true;
                        Buffer.Add("An accounting of all your defensive ships:\n");
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
                Buffer.Add( "SpireSidekickFactionBaseInfo.Instance is null for some reason!" );
                return;
            }
            // Make sure we have our city list before continuing.
            if ( SpireSidekickFactionBaseInfo.Instance.SpireCities.Count <= 0 )
            {
                Buffer.Add( "Fallen Spire not yet initialized, perhaps?  It says no cities. Please unpause the game, but also report this bug. " );
                return;
            }

            SpireSidekickPerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<SpireSidekickPerUnitBaseInfo>();
            if ( data == null )
            {
                Buffer.Add("no data");
                return;
            }
            Faction faction = RelatedEntityOrNull.PlanetFaction.Faction;
            SpireSidekickFactionBaseInfo globaldata = faction.TryGetExternalBaseInfoAs<SpireSidekickFactionBaseInfo>();
            if ( data.RangerMetal > 0 ||  data.DireRangerMetal > 0 )
            {
                Buffer.Add("<size=80%>We have ").Add( data.RangerMetal, "999999" ).Add(" ranger metal and ").Add( data.DireRangerMetal, "999999" ).Add(" dire ranger metal.\n</size> ");  
            }
            Dictionary<Planet, int> rangerDict = globaldata.RangersPerPlanet.GetDisplayDict();
            bool printedIntro = false;
            foreach ( KeyValuePair<Planet, int> kv in rangerDict )
            {
                if ( !printedIntro)
                {
                    printedIntro = true;
                    Buffer.Add("An accounting of all your rangers:\n");
                }
                Buffer.Add("\t").Add(kv.Key.Name, "a1a1ff").Add(": ").Add( kv.Value, "a1ffa1" ).Add("\n");
                continue;
            }

            SpireSidekickFactionBaseInfo.WriteCityTooltipDetails( Buffer, RelatedEntityOrNull );


        }
    }

}
