using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class PublicDarkSpireNotifier : NotifierBaseDataSingleton
    {
        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_DarkSpireVengeanceStrike;
        private static bool hasInitialized = false;

        public static PublicDarkSpireNotifier Instance = new PublicDarkSpireNotifier();

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_DarkSpireVengeanceStrike = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/darkspirevengeancestrike.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            return MouseHandlingResult.DoNotPlayClickSound;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            //nothing to do on the galaxy map hover
            //World_AIW2.Instance.FocusedPlanetForMapDarkening = Data.Planet;

            string colorString = string.Empty;
            if ( Data.Faction != null )
                colorString = Data.Faction.FactionCenterColor.ColorHexBrighter;

            tooltipBuffer.Clear();

            tooltipBuffer.Add( "<color=#" ).Add( colorString ).Add( ">" );
            tooltipBuffer.Add( "黑暗尖塔</color> 复仇打击将在 ").AddHoursAndMinutes(Data.eventTimeRemaining - World_AIW2.Instance.GameSecond).Add("后启动。" );
            tooltipBuffer.Add( "\n\n" ).Add( "\t复仇打击将使星系中所有复仇发生器同时产生大量舰船。" );
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
                debugStage = 0;
                debugStage = 1;
                Image.UpdateWith( sprite_DarkSpireVengeanceStrike, true, "DS" );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "黑暗尖塔\n\n" );
                SubTexts[0].Text.FinishWritingToBuffer();
                int secondsRemaining = Data.eventTimeRemaining - World_AIW2.Instance.GameSecond;
                debugStage = 6;
                buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 9;
                if ( secondsRemaining < 0 ) //can happen on MP clients if the faction hasn't been serialized recently
                    buffer.Add( "-" );
                else
                    buffer.AddSecondsRemaining( secondsRemaining );

                // FInt percent = (data.CurrentExoStrength * 100) / data.StrengthRequiredForNextExo ;

                // buffer.Add( "<color=#" ).Add( colorString ).Add( ">" );
                // buffer.Add(percent.IntValue).Add("%");
                // buffer.Add( "</color>\n" );
                debugStage = 12;
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicDarkSpireNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
    }
}
