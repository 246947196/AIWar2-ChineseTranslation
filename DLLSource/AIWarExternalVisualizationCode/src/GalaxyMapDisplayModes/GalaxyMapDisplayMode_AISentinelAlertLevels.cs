using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using Arcen.AIW2.External;

namespace Arcen.AIW2.ExternalVisualization
{
    public class GalaxyMapDisplayMode_AISentinelAlertLevels : BaseGalaxyMapDisplayMode
    {
        public override bool GetShouldReplaceNormalPlanetTooltip()
        {
            return true;
        }
        public override void WriteToPlanetTooltip( Planet planet, ArcenDoubleCharacterBuffer Buffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            Faction ownerFaction = planet.GetControllingOrInfluencingFaction();
            if ( ownerFaction == null || ownerFaction.Type != FactionType.AI )
            {
                Buffer.StartColor( "5d9aff" ).Add( "\n不是AI星球" ).EndColor().Add( "\n" );
                Buffer.Add( "<size=80%>这个星球不归AI所有，因此他们无法在此进行增援。" );
            }
            else
            {
                if ( planet.SentinelsAlertLevel == null )
                {
                    Buffer.StartColor( "999999" ).Add( "无警报信息" ).EndColor().Add( "\n" );
                    Buffer.Add( "<size=80%>这个星球没有考虑自身应该处于何种警报级别，原因不明。</size>  " );
                }
                else
                {
                    Buffer.StartColor( planet.SentinelsAlertLevel.ColorHex ).Add( "\nAI哨兵警报等级 " ).Add( planet.SentinelsAlertLevel.Ordinal )
                        .Add( ": " ).Add( planet.SentinelsAlertLevel.DisplayName ).EndColor().Add( "\n" );
                    Buffer.Add( "<size=80%>" );
                    Buffer.Add( planet.SentinelsAlertLevel.Description );
                    Buffer.Add( "\n增援上限倍率：", "999999" );
                    Buffer.Add( ( planet.SentinelsAlertLevel.ReinforcementCapMultiplier * 100 ).IntValue ).Add( "%" );
                    Buffer.Add( "\n增援概率：", "999999" );
                    Buffer.Add( planet.SentinelsAlertLevel.ReinforcementGrabBagTickets ).Add( "%" );
                    Buffer.Add( "\n额外说明：", "999999" );
                    Buffer.Add( planet.SentinelsAlertLevel.ExtraNotes );
                    Buffer.Add( "</size>" );
                }
            }
        }

        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            GalaxyMapDisplayMode_Normal.ShowNormalIcons( planet, EntityToSkip, ArrayToFill, OnlyShowThingsThatShouldBeInFarZoom );
        }

        public override void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;
            Faction ownerFaction = planet.GetControllingOrInfluencingFaction();
            if ( ownerFaction == null || ownerFaction.Type != FactionType.AI )
                return;

            if ( planet.SentinelsAlertLevel == null )
                RightBuffer.StartColor( "999999" ).Add( "无警报" ).EndColor().Add( "\n" );
            else
                RightBuffer.StartColor( planet.SentinelsAlertLevel.ColorHex ).Add( "警报 " ).Add( planet.SentinelsAlertLevel.Ordinal ).EndColor().Add( "\n" );
        }
    }
}
