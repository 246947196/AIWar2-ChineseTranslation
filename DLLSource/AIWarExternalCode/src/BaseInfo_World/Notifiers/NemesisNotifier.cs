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
                    tooltipBuffer.Add( "Your allied Scourge are converting a Subjugator into a Nemesis in the galaxy!\n" );
                }
                else
                    tooltipBuffer.Add( "The Scourge are spawning a converting a Subjugator into a Nemesis in the galaxy! You should find it and kill it.\n\nYou can prevent a Nemesis from spawning in the future by killing enough Mark 7 Scourge Spawners.\n" );
                tooltipBuffer.Add( "The summoning will be finished in ").AddHoursAndMinutes(Countdown).Add( "." );
            }
            else
            {
                //fallback case where we don't have the data so we don't know if it's allied or hostile
                tooltipBuffer.Add( "The Scourge are spawning a Nemesis in the galaxy!\n" );
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
                buffer.Add( "Nemesis" );
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
