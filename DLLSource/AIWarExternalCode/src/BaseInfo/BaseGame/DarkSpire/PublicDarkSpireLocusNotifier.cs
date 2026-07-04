using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class PublicDarkSpireLocusNotifier : NotifierBaseDataSingleton
    {
        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_DarkSpireLocus;
        private static bool hasInitialized = false;

        public static PublicDarkSpireLocusNotifier Instance = new PublicDarkSpireLocusNotifier();

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_DarkSpireLocus = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/darkspirevengeancestrike.png" );
        }

        private static int idx = 0; //for cycling
        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            if ( Data.EntityList.Count > 0 )
            {
                if ( idx >= Data.EntityList.Count ) //if the number of planets in the list has decreased since the last click, the idx can be too high
                    idx = 0;
                if ( Data.EntityList[idx].GetShouldBeVisibleBasedOnPlanetIntel() )
                {
                    if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                        Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( Data.EntityList[idx].Planet, false );
                    else
                        World_AIW2.Instance.SwitchViewToPlanet( Data.EntityList[idx].Planet );
                }
                idx++;
                if ( idx >= Data.EntityList.Count )
                    idx = 0;
                return MouseHandlingResult.None;
            }
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
            if ( Data.EntityList.Count == 1 )
            {
                tooltipBuffer.Add( "星系中存在一个黑暗尖塔据点。如果您不摧毁它，它将产生一个新的复仇发生器。\n\n" );
            }
            else
                tooltipBuffer.Add( "星系中存在 " ).Add( Data.EntityList.Count ).Add( " 个黑暗尖塔据点。如果您不摧毁它们，它们将产生新的复仇发生器。\n\n" );
            for ( int i = 0; i < Data.EntityList.Count; i++ )
            {
                GameEntity_Squad locus = Data.EntityList[i].GetSquad();
                if ( locus == null )
                    continue;
                tooltipBuffer.Add( "\t一个据点" );
                if ( locus.GetShouldBeVisibleBasedOnPlanetIntel() )
                {
                    tooltipBuffer.Add( "在 " ).Add( locus.GetPlanetName_Safe(), colorString );
                }
                else
                    tooltipBuffer.Add( "位于星系某处 " );
                int secondsUntilWarpIn = this.secondsTillWarpIn( locus );
                tooltipBuffer.Add( " 将在 " ).AddHoursAndMinutes( secondsUntilWarpIn, "ffa1a1" ).Add( "后变形为复仇发生器。\n" );

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
                InitIfNeeded();
                debugStage = 0;
                debugStage = 1;
                Image.UpdateWith( sprite_DarkSpireLocus, true, "DSLoc" );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                int lowestTime = lowestSecondsTillWarpIn( Data );
                if ( Data.EntityList.Count == 1 )
                {
                    debugStage = 4;
                    if ( Data.EntityList[0].GetShouldBeVisibleBasedOnPlanetIntel() )
                        buffer.Add( Data.EntityList[0].GetPlanetName_Safe() );
                    else
                        buffer.Add( "未知" );
                    buffer.Add( "\n" );
                }
                else
                {
                    debugStage = 5;
                    buffer.Add( Data.EntityList.Count ).Add( " 个据点" );
                    buffer.Add( "\n" );
                }
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 6;
                buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 9;

                buffer.AddSecondsRemaining(lowestTime);

                // FInt percent = (data.CurrentExoStrength * 100) / data.StrengthRequiredForNextExo ;

                // buffer.Add( "<color=#" ).Add( colorString ).Add( ">" );
                // buffer.Add(percent.IntValue).Add("%");
                // buffer.Add( "</color>\n" );
                debugStage = 12;
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicDarkSpireLocusNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
        private int secondsTillWarpIn( GameEntity_Squad locus )
        {
            return (DarkSpireFactionBaseInfo.LocusWarpInTime - locus.GetSecondsSinceCreation());
        }
        private int lowestSecondsTillWarpIn( NotifierFillData Data )
        {
            int lowestSoFar = -1;
            for ( int i = 0; i < Data.EntityList.Count; i++ )
            {
                GameEntity_Squad squad = Data.EntityList[i].GetSquad();
                if ( squad == null )
                    continue;
                if ( secondsTillWarpIn( squad ) < lowestSoFar || lowestSoFar == -1 )
                {
                    lowestSoFar = secondsTillWarpIn( squad );
                }
            }
            return lowestSoFar;
        }
    }
}
