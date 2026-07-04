using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class NemesisNotifier : NotifierBaseDataSingleton
    {
        public static NemesisNotifier Instance = new NemesisNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Nemesis;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_Nemesis = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/scourgenemesis.png" );
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
            if ( Data != null )
            {
                //It seems possible for this to be null, though I don't know why
                int TopTierSpawnersPerNemesis, TopTierSpawners, Countdown;
                ScourgeFactionBaseInfo.GetScourgeStateForNotifications( Data.Faction, out TopTierSpawnersPerNemesis, out TopTierSpawners, out Countdown );

                if ( Data.Faction.GetIsFriendlyToLocalFaction() )
                {
                    tooltipBuffer.Add( "你盟友的天灾正在将一个征服者转化为复仇者！\n" );
                }
                else
                    tooltipBuffer.Add( "天灾正在将一个征服者转化为复仇者！你应该找到并摧毁它。\n\n通过消灭足够的7级天灾生成器，你可以防止复仇者在未来生成。\n" );
                tooltipBuffer.Add( "召唤将在 ").AddHoursAndMinutes(Countdown).Add( " 后完成。" );
            }
            else
            {
                //fallback case where we don't have the data so we don't know if it's allied or hostile
                tooltipBuffer.Add( "天灾正在银河系中生成一个复仇者！\n" );
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
                Image.UpdateWith( sprite_Nemesis, true, "Nemesis" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "复仇者" );
                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                buffer.AddSecondsRemaining( Countdown );
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
