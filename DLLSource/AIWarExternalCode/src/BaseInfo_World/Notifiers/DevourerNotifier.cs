using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DevourerNotifier : NotifierBaseDataSingleton
    {
        public static DevourerNotifier Instance = new DevourerNotifier();

        //these are just data from asset bundles, that's definitely okay to be static   
        private static UnityEngine.Sprite sprite_DevourerOnPlanet;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_DevourerOnPlanet = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/devoureronplanet.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            var entity = Data.Entity.GetSquad();
            if ( entity == null )
                return MouseHandlingResult.PlayClickDeniedSound;
            var planet = Data.Planet;
            if ( planet == null )
                return MouseHandlingResult.PlayClickDeniedSound;
           
            ObjectiveGenerator.CenteringHelper(planet, entity);

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
            string colorString = string.Empty;
            colorString = Data.Faction.FactionCenterColor.ColorHexBrighter;
            tooltipBuffer.Add( "<color=#" ).Add( colorString ).Add( ">" );
            tooltipBuffer.Add( "吞噬者魔像" );
            tooltipBuffer.Add( "</color>" );
            tooltipBuffer.Add( " 位于 " );
            tooltipBuffer.Add( planet.Name );
            //Perhaps we should show the next planet here as a little QoL
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
                InitIfNeeded();

                Planet planet = Data.Planet;
                if ( planet == null )
                    return false;

                debugStage = 0;
                debugStage = 1;
                Image.UpdateWith( sprite_DevourerOnPlanet, true, "Human_Fin" );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();

                SubTexts[0].Text.FinishWritingToBuffer();

                debugStage = 6;
                buffer = SubTexts[1].Text.StartWritingToBuffer();
                buffer.Add( "吞噬者\n" );
                buffer.Add( "魔像\n" );

                debugStage = 9;

                buffer.Add( planet.Name );
                debugStage = 12;
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in DevourerNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
    }
}
