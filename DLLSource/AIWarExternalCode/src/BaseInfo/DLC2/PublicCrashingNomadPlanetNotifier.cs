using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class PublicCrashingNomadPlanetNotifier : NotifierBaseDataSingleton
    {
        public static PublicCrashingNomadPlanetNotifier Instance = new PublicCrashingNomadPlanetNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_NomadPlanetCrashing;
        private static bool hasInitialized = false;
        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_NomadPlanetCrashing = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/nomadcrash.png" );
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
            tooltipBuffer.Add( "These are Nomad Planets en route to crashing:\n\n" );
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
                if ( planet.NomadTargetPlanetIdx != -1 )
                {
                    Faction faction = FactionUtilityMethods.Instance.GetNomadPlanetFaction();
                    NomadPlanetsFactionBaseInfo gData = faction.TryGetExternalBaseInfoAs<NomadPlanetsFactionBaseInfo>();
                    Planet target = World_AIW2.Instance.GetPlanetByIndex( planet.NomadTargetPlanetIdx );
                    tooltipBuffer.Add( "\tPlanet " ).Add( planet.Name, controller.FactionCenterColor.ColorHexBrighter ).Add( " is going to crash into " );
                    tooltipBuffer.Add( target.Name, target.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( " and will next move in " );
                    tooltipBuffer.AddHoursAndMinutes( timeTillNextMove, moveTimerColor ).Add( ".\n" );
                    int timeEstimate = planet.SecondsTillNomadCrashes;
                    if ( timeEstimate <= 0 || timeEstimate >= 9000 )
                        tooltipBuffer.Add( "\t\t" ).Add( "The planet should crash soon" ).Add( ".\n" );
                    else
                    {
                        moveTimerColor = ArcenExternalUIUtilities.GetColorForNomadMoveTime( timeEstimate );
                        tooltipBuffer.Add( "\t\t" ).Add( "Time to crash is " ).AddHoursAndMinutes( timeEstimate, moveTimerColor ).Add( ".\n" );
                    }
                    continue;
                }

                tooltipBuffer.Add( "\tPlanet " ).Add( planet.Name, controller.FactionCenterColor.ColorHexBrighter ).Add( " will move in " ).AddHoursAndMinutes( timeTillNextMove, moveTimerColor ).Add( ". " );
                if ( planet.NomadTargetPlanetIdx != -1 )
                {
                    Planet targetPlanet = World_AIW2.Instance.GetPlanetByIndex( planet.NomadTargetPlanetIdx );
                    tooltipBuffer.Add( "This planet is going to crash into  " ).Add( targetPlanet.Name, "ffa1a1" ).Add( "." );
                }
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
            if ( Data.PlanetList.Count <= 0 )
                return true;
            int timeTillNextMove = Data.PlanetList[0].SecondsTillNomadCrashes;
            if ( timeTillNextMove < 0 )
                return true;

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
                Image.UpdateWith( sprite_NomadPlanetCrashing, true, "Nomad" );

                if ( Data.PlanetList.SafeTryGet( 0, out Planet planet ) )
                {
                    debugStage = 20;
                    ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                    if ( Data.PlanetList.Count == 1 )
                    {
                        buffer.Add( planet.Name );
                    }
                    else
                        buffer.Add( Data.PlanetList.Count ).Add( " Nomads" );
                    SubTexts[0].Text.FinishWritingToBuffer();

                    debugStage = 22;
                    buffer = SubTexts[1].Text.StartWritingToBuffer();
                    debugStage = 23;
                    int timeTillNextMove = 0;
                    if (planet != null)
                        timeTillNextMove = planet.SecondsTillNomadCrashes;
                    debugStage = 24;
                    buffer.AddSecondsRemaining( timeTillNextMove, TimeIntensity.TenMinutes );
                    debugStage = 30;
                    SubTexts[1].Text.FinishWritingToBuffer();
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PrivateNomadPlanetNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
}
