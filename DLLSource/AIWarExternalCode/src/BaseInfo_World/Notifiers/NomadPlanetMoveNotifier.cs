using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class NomadPlanetMoveNotifier : NotifierBaseDataSingleton
    {
        public static NomadPlanetMoveNotifier Instance = new NomadPlanetMoveNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_NomadPlanet;
        private static bool hasInitialized = false;
        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_NomadPlanet = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/nomadplanet.png" );
        }

        private static int idx = 0; //for cycling through
        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            if ( Data.PlanetList.Count > 0 )
            {
                if ( idx >= Data.PlanetList.Count ) //if the number of planets in the list has decreased since the last click, the idx can be too high
                    idx = 0;

                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                    Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( Data.PlanetList[idx], false );
                else
                    World_AIW2.Instance.SwitchViewToPlanet( Data.PlanetList[idx] );
                idx++;
                if ( idx >= Data.PlanetList.Count )
                    idx = 0;
                return MouseHandlingResult.None;
            }
            return MouseHandlingResult.DoNotPlayClickSound;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            tooltipBuffer.Clear();
            tooltipBuffer.Add( "There are Nomad Planets that will move soon:\n\n" );
            bool isFirst = true;
            for ( int i = 0; i < Data.PlanetList.Count; i++ )
            {
                Planet planet = Data.PlanetList[i];
                if ( isFirst )
                {
                    isFirst = false;
                    World_AIW2.Instance.FocusedPlanetForMapDarkening = planet;
                }
                else
                    World_AIW2.Instance.AlsoFocusedPlanetsForMapDarkening[planet] = ArcenTime.TimeSinceStartF; //multi hover
                int timeTillNextMove = planet.TimeForNextMove - World_AIW2.Instance.GameSecond;
                string moveTimerColor = ArcenExternalUIUtilities.GetColorForNomadMoveTime( timeTillNextMove ); //moveTimerColor gets more red the closer the planet is to moving
                Faction controller = planet.GetControllingOrInfluencingFaction();
                tooltipBuffer.Add( "\tPlanet " ).Add( planet.Name, controller.FactionCenterColor.ColorHexBrighter ).Add( " will move in " ).AddHoursAndMinutes( timeTillNextMove, moveTimerColor ).Add( ". " );
                tooltipBuffer.Add( "\n" );
                int maxPlanets = 18;
                if ( i > maxPlanets && Data.PlanetList.Count > maxPlanets + 2 )
                {
                    tooltipBuffer.Add( "There are " + (Data.PlanetList.Count - i) + " additional planets that will move soon.\n" );
                    break;
                }
            }

            Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, tooltipBuffer.GetStringAndResetForNextUpdate() );
            return true;
        }

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

                debugStage = 10;
                Image.UpdateWith( sprite_NomadPlanet, true, "Nomad" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                Planet firstPlanet = null;
                debugStage = 20;
                for ( int i = 0; i < Data.PlanetList.Count; i++ )
                {
                    firstPlanet = Data.PlanetList[i];
                    if ( firstPlanet != null )
                        break;
                }
                debugStage = 30;
                if ( firstPlanet == null )
                {
                    buffer.Add("Nomads");
                    SubTexts[0].Text.FinishWritingToBuffer();
                    return true;
                }
                debugStage = 40;
                if ( Data.PlanetList.Count == 1 )
                    buffer.Add( firstPlanet.Name );
                else 
                    buffer.Add( Data.PlanetList.Count ).Add( " Nomads" );
                debugStage = 50;
                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                debugStage = 60;
                int timeTillNextMove = firstPlanet.TimeForNextMove - World_AIW2.Instance.GameSecond;
                buffer.AddSecondsRemaining( timeTillNextMove, TimeIntensity.TenMinutes );
                debugStage = 70;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in NomadPlanetMoveNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
}
