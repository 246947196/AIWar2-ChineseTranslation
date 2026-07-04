using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ExoNotifier : NotifierBaseDataSingleton
    {
        public static ExoNotifier Instance = new ExoNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_ExogalacticStrikeforce;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_ExogalacticStrikeforce = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/exogalacticstrikeforce.png" );
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
            ExoData data = (ExoData)Data.OtherImportantData;
            bool bonusDebug = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" );
            Faction originFaction = World_AIW2.Instance.GetFactionByIndex( data.FactionIndexOfOriginFaction );
            FInt percent = data.GetPercentageCharged();
            string attackReason = originFaction.GetDisplayName();
            int timeTillAttack = data.OverrideLaunchTime - World_AIW2.Instance.GameSecond;
            if ( data.IsSyncingWithCPA && timeTillAttack <= 600 )
            {
                tooltipBuffer.Add( "WARNING", "ffa1a1" ).Add( ": Incoming AI  Exogalactic Strikeforce Synchronizing With CPA\n\n" );
            }
            else if ( data.IsSyncingWithWormholeInvasion && timeTillAttack < ExternalConstants.Instance.WormholeInvasionWarningTime )
                tooltipBuffer.Add( "WARNING", "ffa1a1" ).Add( ": Incoming AI  Exogalactic Strikeforce Synchronizing With Wormhole Invasion\n\n" );

            if ( (data.IsSyncingWithCPA || data.IsSyncingWithWormholeInvasion) && bonusDebug )
            {
                tooltipBuffer.Add( "Debug: Time till assault launches: " + (data.OverrideLaunchTime - World_AIW2.Instance.GameSecond) ).Add( "\n" );
            }
            if ( bonusDebug )
            {
                if ( data.IsSyncingWithWormholeInvasion )
                    tooltipBuffer.Add("Debug: syncing with wormhole invasion\n");
                if ( data.IsSyncingWithCPA )
                    tooltipBuffer.Add("Debug: syncing with CPA\n");

                tooltipBuffer.Add( "Debug: exo strength : " + data.CurrentExoStrength + " and required str for next exo: " + data.StrengthRequiredForNextExo ).Add( "\n" );
            }
            string colorString = string.Empty;
            Faction exoFactionOrNull = World_AIW2.Instance.GetFactionByIndex( data.FactionIndexOfExoSpawnFaction );
            if ( exoFactionOrNull != null )
                colorString = exoFactionOrNull.FactionCenterColor.ColorHexBrighter;
            else
                colorString = "ffffff";

            if ( !string.IsNullOrEmpty( data.ExoReasonOverride ) )
                attackReason = data.ExoReasonOverride;
            tooltipBuffer.Add( "Exogalactic Strikeforce", colorString ).Add( " detected due to " + attackReason + ". It is " + percent.IntValue + " percent charged." );
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
                Image.UpdateWith( sprite_ExogalacticStrikeforce, true, "Human_Fin" );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "Exostrike\n" );
                SubTexts[0].Text.FinishWritingToBuffer();

                debugStage = 6;
                buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 9;

                //Exo charge percent, with a bit of defensive code
                ExoData data = (ExoData)Data.OtherImportantData;
                if ( data != null )
                {
                    FInt percent = data.GetPercentageCharged();
                    buffer.Add( percent.IntValue ).Add( "%\n" );
                }

                debugStage = 12;
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in ExoNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
    }
}
