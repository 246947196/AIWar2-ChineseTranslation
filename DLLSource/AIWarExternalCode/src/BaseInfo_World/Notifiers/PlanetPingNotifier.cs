using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class PlanetPingNotifier : NotifierBaseDataSingleton
    {
        public static PlanetPingNotifier Instance = new PlanetPingNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Ping;
        private static bool hasInitialized = false;
        private static int index = 0;
        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_Ping = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/nanocaustattack.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            if ( index >= Data.PlanetList.Count )
                index = 0; //since index is a static it could be a stale value from before, so check here if the index is reasonable
            Planet planetToUse = Data.PlanetList[index];
            if (Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView)
            {
                Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet(planetToUse, false);
                PlanetPing.CreateForGalaxyMap(planetToUse, PlanetPingColor.Green);
            }
            else
                World_AIW2.Instance.SwitchViewToPlanet(planetToUse);

            index++;
            if ( index >= Data.PlanetList.Count )
                index = 0;
            return MouseHandlingResult.None;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            //nothing to do on galaxy map hover
            //World_AIW2.Instance.FocusedPlanetForMapDarkening = Data.Planet;

            tooltipBuffer.Clear();
            if ( Data == null || Data.PlanetList.Count == 0 )
                return true;
            tooltipBuffer.Add( "There have been " ).Add( Data.PlanetList.Count, "a1a1ff" ).Add( " planets pinged in the last few minutes:\n" );
            for ( int i = 0; i < Data.PlanetList.Count; i++ )
            {
                Planet planet = Data.PlanetList[i];
                if ( planet == null )
                    continue;
                int timeSincePing = World_AIW2.Instance.GameSecond - planet.GameSecondLastPinged;
                tooltipBuffer.Add("\t").Add( Data.PlanetList[i].Name, "a1ffa1" ).Add(" was last pinged ").Add( timeSincePing, "ffa1a1" ).Add(" seconds ago.\n");
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

                int TopTierSpawnersPerNemesis, TopTierSpawners, Countdown;
                ScourgeFactionBaseInfo.GetScourgeStateForNotifications( Data.Faction, out TopTierSpawnersPerNemesis, out TopTierSpawners, out Countdown );

                debugStage = 10;
                Image.UpdateWith( sprite_Ping, true, "Ping" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "Pings" );
                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                if ( Data != null )
                    buffer.Add( Data.PlanetList.Count );
                debugStage = 30;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in NemesisNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
}
