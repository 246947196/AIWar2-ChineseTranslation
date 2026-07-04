using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public class RenegadeSpireDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            try
            {
                if ( RelatedEntityTypeData == null )
                    return;
                if ( RelatedEntityOrNull == null )
                    return;

                if ( RelatedEntityTypeData.GetHasTag( "RenegadeSpireDefiler" ) )
                {
                    RenegadeSpirePerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<RenegadeSpirePerUnitBaseInfo>();
                    if ( data == null )
                        return;
                    Buffer.Add( "\n防御金属：" ).Add( data.DefensiveMetalToSpend, "a1ffa1" ).Add( "。" );
                    if ( data.Destination != null )
                    {
                        Buffer.Add( "正在前往 " + data.Destination.ToString() );
                        return;
                    }
                    if ( data.BuildPlanetIndex != -1 )
                    {
                        Planet buildPlanet = World_AIW2.Instance.GetPlanetByIndex( data.BuildPlanetIndex );
                        Buffer.Add( "正在前往 " + buildPlanet.Name );
                    }
                    if ( data.NextShipToCreate != null )
                    {
                        Buffer.Add( "我们将建造 " ).Add( data.NextShipToCreate.DisplayName, "a1ffa1" ).Add( "。" );
                    }
                    return;
                }

                if ( RelatedEntityTypeData.GetHasTag( "SpireFracture" ) )
                {
                    RenegadeSpirePerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<RenegadeSpirePerUnitBaseInfo>();
                    if ( data == null )
                        return;

                    Buffer.Add( "\n储存金属：" ).Add( data.MetalStored, "a1ffa1" ).Add( "。" );

                    if ( RelatedEntityOrNull.CurrentMarkLevel >= 7 )
                    {
                        Buffer.Add( "\n标记：最大" );
                    }
                    else
                    {
                        int markupInterval = 600;
                        RenegadeSpireFactionBaseInfo factionInfo = RelatedEntityOrNull.PlanetFaction?.Faction?.GetExternalBaseInfoAs<RenegadeSpireFactionBaseInfo>();
                        if ( factionInfo?.Difficulty != null )
                            markupInterval = factionInfo.Difficulty.MarkupInterval;

                        if ( data.LastMarkupSecond == -1 )
                            Buffer.Add( "\n升级：即将" );
                        else
                        {
                            int remaining = Math.Max( 0, markupInterval - ( World_AIW2.Instance.GameSecond - data.LastMarkupSecond ) );
                            Buffer.Add( "\n升级： " ).Add( remaining, "ffffa1" ).Add( "秒" );
                        }
                    }
                    return;
                }
            }
            catch ( Exception ) { ArcenDebugging.ArcenDebugLogSingleLine( "Exception in RenegadeSpireDescriptionAppender", Verbosity.DoNotShow ); }
        }
    }
}
