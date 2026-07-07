using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class RiskAnalyzerNotifier : NotifierBaseDataSingleton
    {
        public static RiskAnalyzerNotifier Instance = new RiskAnalyzerNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_RiskAnalyzerNotifier;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_RiskAnalyzerNotifier = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/riskanalyzernotifier.png" );
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
            //World_AIW2.Instance.FocusedPlanetForMapDarkening = planet;

            tooltipBuffer.Clear();

            string colorString = string.Empty;
            if ( Data.Faction != null )
                colorString = Data.Faction.FactionCenterColor.ColorHexBrighter;

            if ( Data.eventTimeRemaining > 0 )
            {
                tooltipBuffer.Add( "风险分析器", colorString ).Add( " 将在 " ).AddHoursAndMinutes( Data.eventTimeRemaining ).Add( " 后触发。净AIP变化将为 " + Data.NetAIPChange );
            }
            else
                tooltipBuffer.Add( "风险分析器", colorString ).Add( " 的触发时间无效。净AIP变化可能也不正确，但显示为 " + Data.NetAIPChange );
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
                Image.UpdateWith( sprite_RiskAnalyzerNotifier, true, "RA" );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "风险分析器\n\n" );
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 6;
                buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 9;

                if ( Data.eventTimeRemaining > 0 )
                {
                    if ( Data.eventTimeRemaining > 30 )
                        buffer.Add( "<color=#ffd632>" ); //yellow
                    else
                        buffer.Add( "<color=#ff4f32>" ); //red

                    debugStage = 2;
                    debugStage = 5;
                    buffer.Add( Data.eventTimeRemaining );
                    debugStage = 10;
                    buffer.Add( "</color>\n" );
                }
                debugStage = 12;
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PrivateHackingNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
    }
}
