using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class PublicZenithArchitraveNotifier : NotifierBaseDataSingleton
    {
        public static PublicZenithArchitraveNotifier Instance = new PublicZenithArchitraveNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_ZenithArchitrave;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_ZenithArchitrave = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/zenitharchitrave.png" );
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
            bool debug = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" );
            string debugOutput = "";
            string nameToUse = "this one";
            string architravePlurality = "Architraves have";
            tooltipBuffer.Add( "Once a Zenith Architrave takes too many planets, the other Architraves will all unite against it.\n" ).Add( "At the moment " );
            List<Faction> tooLargeFactions = Data.FactionList;
            List<Faction> smallerFactions = Data.FactionList2;
            if ( tooLargeFactions.Count == 1 )
            {
                ZenithArchitraveFactionBaseInfo globaldata = tooLargeFactions[0].TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
                tooltipBuffer.Add( "there is one " ).Add( tooLargeFactions[0].StartFactionColourForLog() + "Zenith Architrave" ).Add( "</color> that the " );
                architravePlurality = "Architrave has";
                if ( smallerFactions.Count == 1 )
                {
                    tooltipBuffer.Add( "other " );
                    tooltipBuffer.Add( smallerFactions[0].StartFactionColourForLog() + "Zenith Architrave</color> " );
                    if ( globaldata.TimeUntilOtherArchitravesShouldAttackMe > 0 )
                        tooltipBuffer.Add( " will attack in " ).AddHoursAndMinutes( globaldata.TimeUntilOtherArchitravesShouldAttackMe, "ff0000" ).Add( "." );
                    else
                        tooltipBuffer.Add( " is attacking." );
                }
                else
                {
                    tooltipBuffer.Add( "remaining " ).Add( smallerFactions.Count, "a1ffa1" ).Add( " (" );
                    for ( int i = 0; i < smallerFactions.Count; i++ )
                    {
                        if ( debug ) debugOutput = " " + smallerFactions[i].FactionIndex;
                        nameToUse = "this";
                        if ( smallerFactions[i].FactionNameOrEmpty != "" )
                            nameToUse = smallerFactions[i].FactionNameOrEmpty;

                        tooltipBuffer.Add( smallerFactions[i].StartFactionColourForLog() + nameToUse + debugOutput + "</color>" );
                        if ( i != smallerFactions.Count - 1 )
                            tooltipBuffer.Add( ", " );
                        if ( i == smallerFactions.Count - 2 )
                            tooltipBuffer.Add( "and " );
                    }
                    tooltipBuffer.Add( ") Architraves" );
                    if ( globaldata.TimeUntilOtherArchitravesShouldAttackMe > 0 )
                        tooltipBuffer.Add( " will ally against in " ).AddHoursAndMinutes( globaldata.TimeUntilOtherArchitravesShouldAttackMe, "ff0000" ).Add( "." );
                    else
                        tooltipBuffer.Add( " are allied against." );

                }
            }
            else
            {
                if ( Data.WarStarted )
                {
                    tooltipBuffer.Add( "the Architraves over the limit are:\n" );
                    for ( int i = 0; i < tooLargeFactions.Count; i++ )
                    {
                        ZenithArchitraveFactionBaseInfo globaldata = tooLargeFactions[i].TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
                        if ( debug ) debugOutput = " " + tooLargeFactions[i].FactionIndex;
                        nameToUse = "this one";
                        if ( tooLargeFactions[i].FactionNameOrEmpty != "" )
                            nameToUse = tooLargeFactions[i].FactionNameOrEmpty;
                        tooltipBuffer.Add( tooLargeFactions[i].StartFactionColourForLog() + "\t" + nameToUse + " " + debugOutput ).Add( "</color> " );
                        if ( globaldata.TimeUntilOtherArchitravesShouldAttackMe > 0 )
                        {
                            string timerColor = ArcenExternalUIUtilities.GetColorForNomadMoveTime( globaldata.TimeUntilOtherArchitravesShouldAttackMe ); //timerColor gets more red the sooner the pioneers will appear
                            tooltipBuffer.Add( "(in " ).Add( globaldata.TimeUntilOtherArchitravesShouldAttackMe.ToString(), timerColor ).Add( ")" );
                        }
                        tooltipBuffer.Add( "\n" );
                    }
                    if ( smallerFactions.Count <= 1 )
                        tooltipBuffer.Add( "All Architraves are in a free for all." );
                    else
                    {
                        tooltipBuffer.Add( "The remaining " ).Add( smallerFactions.Count, "a1ffa1" ).Add( " (" );
                        for ( int i = 0; i < smallerFactions.Count; i++ )
                        {
                            if ( debug ) debugOutput = " " + smallerFactions[i].FactionIndex;
                            nameToUse = "this";
                            if ( tooLargeFactions[i].FactionNameOrEmpty != "" )
                                nameToUse = tooLargeFactions[i].FactionNameOrEmpty;

                            tooltipBuffer.Add( smallerFactions[i].StartFactionColourForLog() + nameToUse + debugOutput + "</color>" );
                            if ( i != smallerFactions.Count - 1 )
                                tooltipBuffer.Add( ", " );
                            if ( i == smallerFactions.Count - 2 )
                                tooltipBuffer.Add( "and " );
                        }
                        tooltipBuffer.Add( ") architraves are allied against them." );
                    }
                }
                else
                {
                    tooltipBuffer.Add( "the Architraves that will be over the limit are:\n" );
                    for ( int i = 0; i < tooLargeFactions.Count; i++ )
                    {
                        ZenithArchitraveFactionBaseInfo globaldata = tooLargeFactions[i].TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
                        if ( debug ) debugOutput = " " + tooLargeFactions[i].FactionIndex;
                        tooltipBuffer.Add( tooLargeFactions[i].StartFactionColourForLog() + "\tthis one" + debugOutput ).Add( "</color> " );
                        if ( globaldata.TimeUntilOtherArchitravesShouldAttackMe > 0 )
                        {
                            string timerColor = ArcenExternalUIUtilities.GetColorForNomadMoveTime( globaldata.TimeUntilOtherArchitravesShouldAttackMe ); //timerColor gets more red the sooner the pioneers will appear
                            tooltipBuffer.Add( "in " ).Add( globaldata.TimeUntilOtherArchitravesShouldAttackMe.ToString(), timerColor ).Add( "" );
                        }
                        tooltipBuffer.Add( "\n" );
                    }
                    if ( smallerFactions.Count <= 1 )
                        tooltipBuffer.Add( "All Architraves will be in a free for all." );
                    else
                    {
                        tooltipBuffer.Add( "The remaining " ).Add( smallerFactions.Count, "a1ffa1" ).Add( " (" );
                        for ( int i = 0; i < smallerFactions.Count; i++ )
                        {
                            if ( debug ) debugOutput = " " + smallerFactions[i].FactionIndex;
                            tooltipBuffer.Add( smallerFactions[i].StartFactionColourForLog() + "this" + debugOutput + "</color>" );
                            if ( i != smallerFactions.Count - 1 )
                                tooltipBuffer.Add( ", " );
                            if ( i == smallerFactions.Count - 2 )
                                tooltipBuffer.Add( "and " );
                        }
                        tooltipBuffer.Add( ") architraves will be allied against them." );
                    }
                }
            }

            tooltipBuffer.Add( "\nArchitraves in Civil War are exceptionally powerful, and will produce many Golems to fight eachother; once the offending " ).Add( architravePlurality ).Add( " been weakened, the war will end and the other Architraves will retreat back to their territory.\nYour ships and planets could get caught in the crossfire." );

            if ( Data.anyTruce )
                tooltipBuffer.Add( "\nThe Architraves are currently ignoring any truces with you until the end of the civil war." );
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
                Image.UpdateWith( sprite_ZenithArchitrave, true, "ZA" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "Civil War" );
                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                debugStage = 30;
                if ( !Data.WarStarted )
                {
                    List<Faction> tooLargeFactions = Data.FactionList;
                    List<Faction> smallerFactions = Data.FactionList2;
                    int shortestTime = -1;
                    for ( int i = 0; i < tooLargeFactions.Count; i++ )
                    {
                        ZenithArchitraveFactionBaseInfo globaldata = tooLargeFactions[i].TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
                        if ( shortestTime == -1 ||
                             globaldata.TimeUntilOtherArchitravesShouldAttackMe < shortestTime )
                            shortestTime = globaldata.TimeUntilOtherArchitravesShouldAttackMe;
                    }
                    buffer.AddSecondsRemaining( shortestTime, TimeIntensity.TenMinutes );
                }
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicZenithArchitraveNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
}
