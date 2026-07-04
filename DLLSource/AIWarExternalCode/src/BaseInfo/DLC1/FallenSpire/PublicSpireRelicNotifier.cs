using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class PublicSpireRelicNotifier : NotifierBaseDataSingleton
    {
        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Relic;
        private static bool hasInitialized = false;

        public static PublicSpireRelicNotifier Instance = new PublicSpireRelicNotifier();

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_Relic = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/spirerelic2.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            Planet planet = Data.Planet;
            if ( planet == null )
                return MouseHandlingResult.PlayClickDeniedSound;

            if ( !Data.inSearchMode )
            {
                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                    Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( planet, false );
                else
                    World_AIW2.Instance.SwitchViewToPlanet( planet );
                return MouseHandlingResult.None;
            }
            return MouseHandlingResult.DoNotPlayClickSound;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            tooltipBuffer.Clear();
            if ( Data.planetIdx <= 0 )
            {
                tooltipBuffer.Add( "BUG: no planet index\n" );
                return true;
            }
            Planet relicPlanet = World_AIW2.Instance.GetPlanetByIndex( Data.planetIdx );
            if ( Data.inSearchMode )
            {
                tooltipBuffer.Add( "There is a Spire Relic somewhere in the galaxy. You must search for it by hacking planets with the Relic Search hack. When you search you learn how far from the Relic that planet is; you can use this information to deduce the actual planet. Be warned, the more you search the stronger the AI response to getting that relic will be!\n" );
                if ( Data.Int16List.Count > 0 ) //searchedPlanetList
                {
                    tooltipBuffer.Add( "Searched planets:" );
                    for ( int i = 0; i < Data.Int16List.Count; i++ )
                    {
                        Planet searchedPlanet = World_AIW2.Instance.GetPlanetByIndex( Data.Int16List[i] );
                        tooltipBuffer.Add( "\n\t" ).Add( searchedPlanet.Name, "a1ffa1" ).Add( ": hops to relic planet: " ).Add( relicPlanet.GetHopsTo( searchedPlanet ).ToString(), "a1ffa1" );
                    }
                }
            }
            else
            {
                World_AIW2.Instance.FocusedPlanetForMapDarkening = relicPlanet;
                Faction controller = relicPlanet.GetControllingFaction();
                tooltipBuffer.Add( "There is a Spire Relic on " ).Add( relicPlanet.Name, controller.FactionCenterColor.ColorHexBrighter ).Add( ". Hack that planet to try to bring the relic home." );
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
                Image.UpdateWith( sprite_Relic, true, "SpireRelic" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "Relic\n" );
                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                Planet relicPlanet = World_AIW2.Instance.GetPlanetByIndex( Data.planetIdx );
                if ( relicPlanet == null )
                {
                    buffer.Add( "Bug. NULL" );
                    return true;
                }
                debugStage = 20;

                if ( !Data.inSearchMode )
                    buffer.Add( relicPlanet.Name );
                else if ( Data.Int16List.Count > 0 ) //searchedPlanetList
                {
                    buffer.Add( Data.Int16List.Count ).Add( "\n" );
                    buffer.Add( "Searched " );
                }
                debugStage = 30;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicSpireRelicNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
}
