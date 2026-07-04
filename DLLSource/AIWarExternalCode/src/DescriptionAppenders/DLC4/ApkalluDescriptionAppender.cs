using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public class ApkalluDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        // Scratch list for collecting available summoner tags from Ziggurat structures.
        // Only ever called from the UI thread, so a static is safe.
        private static readonly List<string> availableTagsScratch = List<string>.Create_WillNeverBeGCed( 8, "ApkalluDescriptionAppender-AvailableTags" );

        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            try
            {
                if ( RelatedEntityTypeData == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "No type data?", Verbosity.DoNotShow );
                    return;
                }
                if ( RelatedEntityOrNull == null )
                    return;

                if ( RelatedEntityTypeData.GetHasTag( "CorruptedZiggurat" ) )
                {
                    MalwarePerUnitBaseInfo zigData = RelatedEntityOrNull.TryGetExternalBaseInfoAs<MalwarePerUnitBaseInfo>();
                    if ( zigData == null )
                        return;
                    ConcurrentDictionary<GameEntityTypeData, int> stored = zigData.StoredEscortsInsideThisZiggurat;
                    List<SafeSquadWrapper> deployed = zigData.DeployedEscortsOfThisZiggurat.GetDisplayList();
                    int deployedCount = deployed == null ? 0 : deployed.Count;
                    if ( stored.Count > 0 || deployedCount > 0 )
                    {
                        Buffer.Add( "\n护航舰：" );
                        if ( stored.Count > 0 )
                        {
                            Buffer.Add( "内部储存 (" );
                            bool isFirst = true;
                            foreach ( KeyValuePair<GameEntityTypeData, int> kv in stored )
                            {
                                if ( isFirst ) isFirst = false;
                                else Buffer.Add( ", " );
                                Buffer.Add( kv.Key.GetDisplayName(), "a1ffa1" ).Add( " x" ).Add( kv.Value, "ffffff" );
                            }
                            Buffer.Add( ")" );
                        }
                        if ( deployedCount > 0 )
                        {
                            if ( stored.Count > 0 ) Buffer.Add( "; " );
                            Buffer.Add( deployedCount, "ffa1a1" ).Add( " 已部署" );
                        }
                        Buffer.Add( ". " );
                    }
                    else
                        Buffer.Add( "\n无剩余护航舰。" );
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag( "ApkalluFlagship" ) )
                {
                    ApkalluFactionBaseInfo globaldata = RelatedEntityOrNull.PlanetFaction.Faction.TryGetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
                    if ( globaldata == null )
                        return;

                    List<string> activeTags = RelatedEntityOrNull.GetEnabledSummonerTagsDirect();

                    // Find the Ziggurat that currently owns this Lamassu
                    GameEntity_Squad ownerZiggurat = null;
                    foreach ( GameEntity_Squad ziggurat in globaldata.Ziggurats.DisplaySquads() )
                    {
                        ApkalluPerUnitBaseInfo zigUnit = ziggurat.TryGetExternalBaseInfoAs<ApkalluPerUnitBaseInfo>();
                        if ( zigUnit != null && zigUnit.LamassuEntityPrimaryKeyID == RelatedEntityOrNull.PrimaryKeyID )
                        {
                            ownerZiggurat = ziggurat;
                            break;
                        }
                    }

                    if ( ownerZiggurat != null && ownerZiggurat.Planet != null )
                    {
                        // Collect summoner tags the Ziggurat's current structures provide
                        availableTagsScratch.Clear();
                        foreach ( GameEntity_Squad entity in ownerZiggurat.Planet.Squads() )
                        {
                            if ( entity.GetFactionOrNull_Safe() != RelatedEntityOrNull.PlanetFaction.Faction )
                                continue;
                            if ( entity.TypeData.GetHasTag( "ApkalluZigguratSummoner" ) )
                            {
                                List<string> tags = entity.TypeData.TagsList;
                                if ( tags != null )
                                    for ( int i = 0; i < tags.Count; i++ )
                                        if ( tags[i].StartsWith( "ApkalluSummoner" ) && !availableTagsScratch.Contains( tags[i] ) )
                                            availableTagsScratch.Add( tags[i] );
                            }
                        }

                        // Compare the two sets
                        bool differs = false;
                        int activeCount = activeTags == null ? 0 : activeTags.Count;
                        if ( activeCount != availableTagsScratch.Count )
                            differs = true;
                        else
                        {
                            for ( int i = 0; i < availableTagsScratch.Count && !differs; i++ )
                                if ( activeTags == null || !activeTags.Contains( availableTagsScratch[i] ) )
                                    differs = true;
                            for ( int i = 0; i < activeCount && !differs; i++ )
                                if ( !availableTagsScratch.Contains( activeTags[i] ) )
                                    differs = true;
                        }

                        if ( differs )
                            Buffer.Add( "\n拉玛苏返回其金字形神塔后将有新武器可用。", "a1ffa1" );
                    }
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag( "ApkalluZiggurat" ) )
                {
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag( "ApkalluOutpost" ) )
                {
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag( "ApkalluPilgrim" ) )
                {
                    ApkalluPerUnitBaseInfo localData = RelatedEntityOrNull.TryGetExternalBaseInfoAs<ApkalluPerUnitBaseInfo>();
                    if ( localData != null )
                    {
                        Buffer.Add( "\n此朝圣者有 " ).Add( localData.ResourcePoints, "a1ffa1" ).Add( " 资源点。" );
                    return;
                    }
                }
            }
            catch ( Exception ) { ArcenDebugging.ArcenDebugLogSingleLine( "Whoops? exception in ApkalluDescriptionAppender", Verbosity.DoNotShow ); }

            return;
        }
    }
}
