using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class PlanetCeasefireNotifier : NotifierBaseDataSingleton
    {
        public static PlanetCeasefireNotifier Instance = new PlanetCeasefireNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_PlanetCeasefireOnPlanet;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_PlanetCeasefireOnPlanet = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/ecology.png" );
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
                return false;

            //galaxy map hover
            World_AIW2.Instance.FocusedPlanetForMapDarkening = planet;

            tooltipBuffer.Clear();
            tooltipBuffer.Add( "一个单位正在 " ).Add( planet.Name ).Add( " 上执行临时停火。" );
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

                Planet planet = Data.Planet;
                if ( planet == null )
                    return false;

                debugStage = 1;
                Image.UpdateWith( sprite_PlanetCeasefireOnPlanet, true, "CF" );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "停火\n" );
                buffer.Add( "\n" );
                SubTexts[0].Text.FinishWritingToBuffer();

                debugStage = 6;
                buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 9;

                buffer.Add( planet.Name );
                debugStage = 12;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PlanetCeasefireNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
    }
}
