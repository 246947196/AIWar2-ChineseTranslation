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
                tooltipBuffer.Add( "来自星球 " ).Add( planet.Name, planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( " 的反击正在酝酿中，当前力量为 " )
                    .Add( (planet.PrecalculatedAICounterattackForcesStrength / 1000f).ToString( "0.##" ), planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( "。\n\n" )
                    .Add( "你在这个星球上损失的越多，反击就越强大。如果你能在反击发起前摧毁所有哨站，反击将被取消，你可以安全离开。\n\n" );
            if ( planet.AICounterAttacksCurrentlyStalled )
                tooltipBuffer.Add( "反击目前被阻止发动，因为你在此处的力量为 " )
                    .Add( (planet.PlayerStrengthHereForBlockingAICounterAttacks / 1000f).ToString( "0.##" ), localFaction.FactionCenterColor.ColorHexBrighter ).Add( "，超过了当前要求的 " )
                    .Add( (planet.AICounterattackToBeStalledByPlayerStrengthOf / 1000f).ToString( "0.##" ), localFaction.FactionCenterColor.ColorHexBrighter ).Add( "。然而，随着反击力量的增长，这个要求也会上升，如果变得足够高，你将只有两分钟的反应时间。请考虑你是否能维持这种情况。" );
            else
                tooltipBuffer.Add( "反击将在 " ).Add( planet.AICountdownTimerForCounterattack, "a1ffa1" ).Add( " 秒后到达。目前你在此处的部队总力量为 " )
                    .Add( (planet.PlayerStrengthHereForBlockingAICounterAttacks / 1000f).ToString( "0.##" ), localFaction.FactionCenterColor.ColorHexBrighter ).Add( " ——如果你能在此星球集结至少 " )
                    .Add( (planet.AICounterattackToBeStalledByPlayerStrengthOf / 1000f).ToString( "0.##" ), localFaction.FactionCenterColor.ColorHexBrighter ).Add( " 的力量，你就可以无限期地拖延反击。与此同时，你最好做好迎击准备……" );
            tooltipBuffer.Add( "\n\n" ).Add( "你必须消灭星球上剩余的 " ).Add( numEnablers, "a1ffa1" ).Add( " 个哨站来取消反击。" );
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
                    buffer.Add( "已停滞" );
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
