using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{/*
    public class WarpingInOutguardNotifier : NotifierBaseDataSingleton
    {
        public static WarpingInOutguardNotifier Instance = new WarpingInOutguardNotifier();

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
            int debugCode = 0;
            try{
                debugCode = 100;
                Planet planet = Data.Planet;
                if ( planet == null )
                    return false;

                //galaxy map hover
                World_AIW2.Instance.FocusedPlanetForMapDarkening = planet;
                debugCode = 200;
                string colorString = string.Empty;
                colorString = Data.Faction.FactionCenterColor.ColorHexBrighter;
                
                tooltipBuffer.Clear();
                string destColorString = string.Empty;

                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, tooltipBuffer.GetStringAndResetForNextUpdate() );
            } catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in mouseover for WarpingInOutguardNotifier. debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            return true;
        }

        public override bool ContentGetter( NotifierFillData Data, ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
        {
            int debugStage = 0;
            try
            {
                Planet planet = Data.Planet;
                if ( planet == null )
                    return false;

                debugStage = 0;
                debugStage = 1;
                Image.UpdateWith( sprite_WormholeInvasion, true, "Human_Fin" );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "Wormhole\n" );
                SubTexts[0].Text.FinishWritingToBuffer();

                debugStage = 6;
                buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 9;

                buffer.Add( planet.Name );
                int projectorAppearanceTime = (int)Data.Int64List[0];
                int planetLinkTime = (int)Data.Int64List[1];
                int nextWaveTime = (int)Data.Int64List[2];
                int timeTillProjectorAppears = projectorAppearanceTime - World_AIW2.Instance.GameSecond;
                int timeTillProjectorLinks = planetLinkTime - World_AIW2.Instance.GameSecond;
                int timeTillNextWave = nextWaveTime - World_AIW2.Instance.GameSecond;

                debugStage = 12;
                buffer.Add( "\n" );
                debugStage = 13;
                if ( timeTillProjectorAppears > 0 )
                    buffer.Add( timeTillProjectorAppears );
                else if ( timeTillProjectorLinks > 0 )
                    buffer.Add( timeTillProjectorLinks );
                else if ( timeTillNextWave > 0 )
                    buffer.Add( timeTillNextWave );

                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in WarpingInOutguardNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
    }*/
}
