using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class CounterattackNotifier : NotifierBaseDataSingleton
    {
        public static CounterattackNotifier Instance = new CounterattackNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Counterattack;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_Counterattack = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/counterattack.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            Planet planet = Data.Planet;
            if ( planet == null )
                return MouseHandlingResult.PlayClickDeniedSound;

            if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( planet, false );
            else
                World_AIW2.Instance.SwitchViewToPlanet( planet );
            return MouseHandlingResult.DoNotPlayClickSound;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            Planet planet = Data.Planet;

            if ( planet == null )
            {
                tooltipBuffer.Clear();
                tooltipBuffer.Add( "Null planet!" );
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, tooltipBuffer.GetStringAndResetForNextUpdate() );
                return true;
            }
            World_AIW2.Instance.FocusedPlanetForMapDarkening = planet;
            Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
            int numEnablers = 0;
            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.AICounterattackEnablers ) )
            {
                if ( !entity.PlanetFaction.Faction.GetIsFriendlyTowards( localFaction ) )
                     numEnablers++; //you can have player-allied guard posts from the necromancer
            }
            tooltipBuffer.Clear();
            tooltipBuffer.Add( "A counterattack from planet " ).Add( planet.Name, planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( " is building, currently with strength " )
                .Add( (planet.PrecalculatedAICounterattackForcesStrength / 1000f).ToString( "0.##" ), planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( ".\n\n" )
                .Add( "The more losses you take on this planet, the stronger the counterattack will become.  If you can destroy all the guard posts before the counterattack launches, it will be canceled and you walk away free.\n\n" );
            if ( planet.AICounterAttacksCurrentlyStalled )
                tooltipBuffer.Add( "The counterattack is currently prevented from launching, because your forces of " )
                    .Add( (planet.PlayerStrengthHereForBlockingAICounterAttacks / 1000f).ToString( "0.##" ), localFaction.FactionCenterColor.ColorHexBrighter ).Add( " strength are more than the current requirement of " )
                    .Add( (planet.AICounterattackToBeStalledByPlayerStrengthOf / 1000f).ToString( "0.##" ), localFaction.FactionCenterColor.ColorHexBrighter ).Add( ".  However, as the counterattack builds strength, that requirement will rise, and if it gets high enough then you'll only have two minutes to react before the counterattack launches.  Consider whether you can keep this up or not and go from there." );
            else
                tooltipBuffer.Add( "The counterattack is going to arrive in " ).Add( planet.AICountdownTimerForCounterattack, "a1ffa1" ).Add( " seconds.  Currently your forces here total " )
                    .Add( (planet.PlayerStrengthHereForBlockingAICounterAttacks / 1000f).ToString( "0.##" ), localFaction.FactionCenterColor.ColorHexBrighter ).Add( " strength -- if you can muster at least " )
                    .Add( (planet.AICounterattackToBeStalledByPlayerStrengthOf / 1000f).ToString( "0.##" ), localFaction.FactionCenterColor.ColorHexBrighter ).Add( " strength on this planet, then you can stall the counterattack indefinitely.  In the meantime you might be better advised to prepare for impact..." );
            tooltipBuffer.Add( "\n\n" ).Add( "You must kill " ).Add( numEnablers, "a1ffa1" ).Add( " guard posts left on the planet to cancel the counterattack." );
            Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, tooltipBuffer.GetStringAndResetForNextUpdate() );
            return true;
        }

        #region GetPlanetName_Safe
        public string GetPlanetName_Safe( NotifierFillData Data )
        {
            Planet plan = Data.Planet;
            if ( plan == null )
                return "[null planet]";
            return plan.Name;
        }
        #endregion

        public override bool GetShouldBeHidden( NotifierFillData Data )
        {
            return false;
        }

        public override bool ContentGetter( NotifierFillData Data, ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
        {
            int debugStage = -1;
            try
            {
                debugStage = 0;
                InitIfNeeded();
                UnityEngine.Color colorForCounterattack = UnityEngine.Color.white;
                debugStage = 1;
                Planet planet = Data.Planet;
                if ( planet == null )
                    return false;
                {
                    {
                        debugStage = 4;
                        //there has been an odd Null reference exception in this function right after a game load,
                        //so include some code to bail out here just in case
                        try
                        {
                            if ( planet.AICounterAttacksCurrentlyStalled )
                                colorForCounterattack = UnityEngine.Color.gray;
                        }
                        catch { return false; } //that nullref right after game load was still persisting, so this just forcibly ignores those and bails out
                    }
                }

                debugStage = 10;
                Image.UpdateWith( sprite_Counterattack, true, "Human_Fin" );
                debugStage = 1010;
                Image.SetColor( colorForCounterattack );

                debugStage = 1020;
                int secondsRemaining = planet.AICountdownTimerForCounterattack;
                
                debugStage = 1030;
                ArcenDoubleCharacterBuffer buffer = SubTexts[1].Text.StartWritingToBuffer();
                {
                    debugStage = 1040;
                    //buffer.Add( "COUNTER\n" );
                    int strengthAsInt = planet.PrecalculatedAICounterattackForcesStrength;
                    buffer.Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon );
                    ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, strengthAsInt, true, true );
                    buffer.Add( "\n" );
                }

                debugStage = 1050;
                if ( planet.AICounterAttacksCurrentlyStalled )
                {
                    buffer.Add( "STALLED" );
                }
                else
                {
                    debugStage = 1060;
                    if ( secondsRemaining < 20 )
                        buffer.Add( "<color=#ff4f32>" ); //red
                    else if ( secondsRemaining < 60 )
                        buffer.Add( "<color=#ffd632>" ); //yellow

                    debugStage = 20;
                    debugStage = 50;
                    buffer.Add( secondsRemaining / 60 );
                    debugStage = 100;
                    buffer.Add( ":" );
                    int secondsPortion = secondsRemaining % 60;
                    debugStage = 150;
                    if ( secondsPortion < 10 ) //GC-neutral instead of ToString with formatting
                        buffer.Add( "0" );
                    debugStage = 200;
                    buffer.Add( secondsPortion );
                }
                debugStage = 210;
                SubTexts[1].Text.FinishWritingToBuffer();

                debugStage = 250;

                buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( this.GetPlanetName_Safe( Data ) );

                SubTexts[0].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in CounterattackNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
}
