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
                    Buffer.Add( "\nDefensive Metal: " ).Add( data.DefensiveMetalToSpend, "a1ffa1" ).Add( "." );
                    if ( data.Destination != null )
                    {
                        Buffer.Add( "En route to " + data.Destination.ToString() );
                        return;
                    }
                    if ( data.BuildPlanetIndex != -1 )
                    {
                        Planet buildPlanet = World_AIW2.Instance.GetPlanetByIndex( data.BuildPlanetIndex );
                        Buffer.Add( "En route to " + buildPlanet.Name );
                    }
                    if ( data.NextShipToCreate != null )
                    {
                        Buffer.Add( "We will build a " ).Add( data.NextShipToCreate.DisplayName, "a1ffa1" ).Add( "." );
                    }
                    return;
                }

                if ( RelatedEntityTypeData.GetHasTag( "SpireFracture" ) )
                {
                    RenegadeSpirePerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<RenegadeSpirePerUnitBaseInfo>();
                    if ( data == null )
                        return;

                    Buffer.Add( "\nMetal Stored: " ).Add( data.MetalStored, "a1ffa1" ).Add( "." );

                    if ( RelatedEntityOrNull.CurrentMarkLevel >= 7 )
                    {
                        Buffer.Add( "\nMark: MAX" );
                    }
                    else
                    {
                        int markupInterval = 600;
                        RenegadeSpireFactionBaseInfo factionInfo = RelatedEntityOrNull.PlanetFaction?.Faction?.GetExternalBaseInfoAs<RenegadeSpireFactionBaseInfo>();
                        if ( factionInfo?.Difficulty != null )
                            markupInterval = factionInfo.Difficulty.MarkupInterval;

                        if ( data.LastMarkupSecond == -1 )
                            Buffer.Add( "\nMark up in: soon" );
                        else
                        {
                            int remaining = Math.Max( 0, markupInterval - ( World_AIW2.Instance.GameSecond - data.LastMarkupSecond ) );
                            Buffer.Add( "\nMark up in: " ).Add( remaining, "ffffa1" ).Add( "s" );
                        }
                    }
                    return;
                }
            }
            catch ( Exception ) { ArcenDebugging.ArcenDebugLogSingleLine( "Exception in RenegadeSpireDescriptionAppender", Verbosity.DoNotShow ); }
        }
    }
}
