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
                    Buffer.Add("no data");
                    return;
                }
                Faction faction = RelatedEntityOrNull.PlanetFaction.Faction;
                TiberiumFactionBaseInfo globaldata = faction.TryGetExternalBaseInfoAs<TiberiumFactionBaseInfo>();
                if ( globaldata == null )
                    return;
                if ( RelatedEntityTypeData.GetHasTag("TiberiumVein") )
                {
                    Buffer.Add("This vein has ").Add( data.Points, "ffaaff" ).Add(" points that it will spend on ").Add( data.NextUpgrade.ToFriendlyString(), "cc2277" ).Add(". ");
                    if ( data.AutoDefenseBuildPoints > 0 )
                    {
                        Buffer.Add("The vein is also constructing ships to defend the territory around it; it has ").Add( data.AutoDefenseBuildPoints, "a1ffa1" ).Add( " defense build points." );
                    }
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag("TiberiumSummoner") )
                {
                    int seconds = data.TimeForNextSpawn - World_AIW2.Instance.GameSecond;
                    Buffer.Add("This summoner will produce a Tyderian ship in ").Add( seconds, "a1ffa1" ).Add(" seconds." );
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag("TiberiumDropship") )
                {
                    Planet dest = RelatedEntityOrNull.GetDestinationPlanet();
                    if ( dest != RelatedEntityOrNull.Planet )
                        Buffer.Add("This dropship is on route to  ").Add( dest.ToString(), "a1ffa1" ).Add("." );
                    return;
                }

            }
            catch ( Exception ) { ArcenDebugging.ArcenDebugLogSingleLine("Whoops? exception in ArmadaDescriptionAppender", Verbosity.DoNotShow );}

            return;
        }
    }
}
