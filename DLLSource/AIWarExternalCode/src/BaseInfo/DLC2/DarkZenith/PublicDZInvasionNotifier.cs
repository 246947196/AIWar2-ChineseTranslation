using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class PublicDZInvasionNotifier : NotifierBaseDataSingleton
    {
        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_DarkZenith;
        private static bool hasInitialized = false;

        public static PublicDZInvasionNotifier Instance = new PublicDZInvasionNotifier();

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_DarkZenith = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/darkmatter.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            return MouseHandlingResult.DoNotPlayClickSound;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            //nothing to do on galaxy map hover
            //World_AIW2.Instance.FocusedPlanetForMapDarkening = Data.Planet;

            tooltipBuffer.Clear();
            int timeTillLaunch = Data.eventTimeRemaining - World_AIW2.Instance.GameSecond;
            string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( timeTillLaunch );
            tooltipBuffer.Add( "The " ).Add( "Dark Zenith", Data.Faction.FactionCenterColor.ColorHexBrighter ).Add( " will start their invasion in " ).AddHoursAndMinutes( timeTillLaunch, color ).Add( "." );
            Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, tooltipBuffer.GetStringAndResetForNextUpdate() );
            return true;
        }

        public override bool GetShouldBeHidden( NotifierFillData Data )
        {
            int timeTillLaunch = Data.eventTimeRemaining - World_AIW2.Instance.GameSecond;
            if ( timeTillLaunch < 0 )
                return true;
            return false;
        }

        public override bool ContentGetter( NotifierFillData Data, ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
        {
            int debugStage = -1;
            try
            {
                InitIfNeeded();

                debugStage = 0;
                debugStage = 1;
                Image.UpdateWith( sprite_DarkZenith, true, "DZ" );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "Invasion\n" );
                SubTexts[0].Text.FinishWritingToBuffer();

                debugStage = 6;
                buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 9;

                //Exo charge percent, with a bit of defensive code
                int timeTillLaunch = Data.eventTimeRemaining - World_AIW2.Instance.GameSecond;
                buffer.AddSecondsRemaining( timeTillLaunch, TimeIntensity.TenMinutes );

                debugStage = 12;
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicDZInvasionNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
    }
}
