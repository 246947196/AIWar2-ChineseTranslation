using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using Arcen.AIW2.External;

namespace Arcen.AIW2.ExternalVisualization
{
    public class GalaxyMapDisplayMode_DeepstrikeDanger : BaseGalaxyMapDisplayMode
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
            if ( ownerFaction == null || ownerFaction.Type == FactionType.NaturalObject )
            {
                Buffer.StartColor( "5d9aff" ).Add( "\nDeepstrike Safe Zone" ).EndColor().Add( "\n" );
                Buffer.Add( "<size=80%>This planet is not owned by anyone, so it is a natural safe haven from the AI Reserves.  " );
            }
            else if ( ownerFaction.GetIsFriendlyToLocalFaction() )
            {
                Buffer.StartColor( "5d9aff" ).Add( "\nAllied Territory" ).EndColor().Add( "\n" );
                Buffer.Add( "<size=80%>This planet is controlled by you or one of your allies, so is a natural safe haven from the AI Reserves.  " );
            }
            else
            {
                if ( planet.IsEligibleForDeepStrike )
                {
                    bool isOnExtraAlert = planet.WillBeOnExtraDeepstrikeAlertUntilGameSecond >= World_AIW2.Instance.GameSecond;

                    Buffer.StartColor( "ff3939" ).Add( "\nDeepstrike Danger Zone" ).EndColor().Add( "\n" );
                    Buffer.Add( "<size=80%>This planet is far enough into AI territory (away from your own planets) that <color=#ff3939>the AI will become very alarmed by any forces you bring that are not inside transports</color>.  " );
                    Buffer.Add( "The AI Reserves subfaction of the AI <color=#ff3939>will start deploying emergency units</color> if you hang around here long enough.  They don't have infinite resources and have to build up over time, but the danger of their response should not be underestimated.  " );
                    Buffer.Add( "\n\nIt would be good if you <color=#ff3939>come in fast</color>, deal damage as needed, capture or hack any important targets, and <color=#ff3939>then get back out</color>.  Alternatively, destroy the AI Command Station and Guard Posts as fast as possible to cancel any blowback.</size>" );

                    if ( isOnExtraAlert )
                    {
                        Buffer.StartColor( "ff2a7f" ).Add( "\n\n<size=90%>Deepstrike Full Alert!</size>" ).EndColor().Add( "\n" );
                        Buffer.Add( "<size=70%>This planet has noticed that humans brought unloaded transports to it, and consequently it will be on extra high alert for deepstriking for another " )
                            .AddHoursAndMinutes( planet.WillBeOnExtraDeepstrikeAlertUntilGameSecond - World_AIW2.Instance.GameSecond ).Add( ".  " );
                        Buffer.Add( "During this period of extra alert, this planet will monitor human ships in and out of transports, on itself and on adjacent planets.  You must defeat this planet, or retreat at least two hops away from it to avoid deployment of the AI Reserves.</size>" );
                    }
                }
                else
                {
                    Buffer.StartColor( "5d9aff" ).Add( "\nDeepstrike Safe Zone" ).EndColor().Add( "\n" );
                    Buffer.Add( "<size=80%>This planet is either not controlled by the AI, or is close enough to your territory that <color=#5d9aff>there is no risk of the AI Reserves coming after you</color> if you unload a transport here.  " );
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

            if ( planet.IsEligibleForDeepStrike )
            {
                bool isOnExtraAlert = planet.WillBeOnExtraDeepstrikeAlertUntilGameSecond >= World_AIW2.Instance.GameSecond;
                string colorToUse = isOnExtraAlert ? "ff2a7f" : "ff3939";
                AIReservesFactionBaseInfo aiReservesInfo = AIReservesFactionBaseInfo.Instance;
                if ( aiReservesInfo.AbsorbShipsMode )
                {
                    if ( isOnExtraAlert )
                        colorToUse = "ff6c39";
                }

                //LEFT ONLY
                LeftBuffer.StartColor( colorToUse ).Add( "DEEP" ).EndColor().Add( "\n" );
                if ( isOnExtraAlert )
                {
                    if ( aiReservesInfo.AbsorbShipsMode )
                        LeftBuffer.StartColor( colorToUse ).Add( "RESIDUAL" ).EndColor().Add( "\n" );
                    else
                        LeftBuffer.StartColor( colorToUse ).Add( "FULL" ).EndColor().Add( "\n" );
                }

                //RIGHT ONLY
                RightBuffer.StartColor( colorToUse ).Add( "STRIKE" ).EndColor().Add( "\n" );
                if ( isOnExtraAlert )
                    RightBuffer.StartColor( colorToUse ).Add( "ALERT" ).EndColor().Add( "\n" );
            }
        }
    }
}
