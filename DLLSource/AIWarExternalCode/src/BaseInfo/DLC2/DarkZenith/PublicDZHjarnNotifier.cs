using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class PublicDZHjarnNotifier : NotifierBaseDataSingleton
    {
        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_DarkZenith;
        private static bool hasInitialized = false;

        public static PublicDZHjarnNotifier Instance = new PublicDZHjarnNotifier();

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            //CHRIS TODO: add a new notification icon
            sprite_DarkZenith = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/darkdata.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            //hop between planets
            return MouseHandlingResult.DoNotPlayClickSound;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            //nothing to do on galaxy map hover
            //World_AIW2.Instance.FocusedPlanetForMapDarkening = Data.Planet;

            tooltipBuffer.Clear();
            int intensity = Data.Faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
            DarkZenithDifficulty Difficulty = DarkZenithDifficultyTable.Instance.GetRowByIntensity( (byte)intensity, Data.Faction );
            tooltipBuffer.Add( "There are " ).Add( Data.EntityList.Count, "a1ffa1" ).Add( " planets being transformed by the " ).Add( "Dark Zenith", Data.Faction.FactionCenterColor.ColorHexBrighter ).Add( ".\n" );
            for ( int i = 0; i < Data.EntityList.Count; i++ )
            {
                GameEntity_Squad squad = Data.EntityList[i].GetSquad();
                if ( squad == null )
                    continue;
                int timeTillConversion = Difficulty.TimeToConvertPlanet - squad.GetSecondsSinceEnteringThisPlanet();
                string timerColor = ArcenExternalUIUtilities.GetColorForNomadMoveTime( timeTillConversion ); //timerColor gets more red the closer the planet is to succumbing
                if ( squad.GetShouldBeVisibleBasedOnPlanetIntel() )
                    tooltipBuffer.Add( "\t" ).Add( squad.GetPlanetName_Safe(), "ffa1a1" ).Add( " will succumb to the Fimbulwinter in " );
                else
                    tooltipBuffer.Add( "\tAn unknown planet in the galaxy will succumb to the Fimbulwinter in " );

                tooltipBuffer.AddHoursAndMinutes( timeTillConversion, timerColor ).Add( "." ).Add( "\n" );
            }
            if ( Data.EntityList.Count == 1 )
                tooltipBuffer.Add( "If the Hjarn on that planet is destroyed then it will stop the conversion." );
            else
                tooltipBuffer.Add( "If the Hjarn on one of those planets is destroyed then it will stop that planet's conversion." );
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
                Image.UpdateWith( sprite_DarkZenith, true, "DZ" );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "Fimbulwinter\n" );
                SubTexts[0].Text.FinishWritingToBuffer();

                debugStage = 6;
                buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 9;

                //Exo charge percent, with a bit of defensive code
                buffer.Add( Data.EntityList.Count );

                debugStage = 12;
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicDZHjarnNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
    }
}
