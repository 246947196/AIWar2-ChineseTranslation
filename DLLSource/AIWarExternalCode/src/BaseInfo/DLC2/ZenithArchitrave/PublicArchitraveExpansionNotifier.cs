using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class PublicArchitraveExpansionNotifier : NotifierBaseDataSingleton
    {
        public static PublicArchitraveExpansionNotifier Instance = new PublicArchitraveExpansionNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Expansion;
        private static UnityEngine.Sprite sprite_Pioneers;
        private static UnityEngine.Sprite sprite_Countdown;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_Expansion = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/zenitharchitraveexpansion.psd" );
            sprite_Pioneers = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/zenitharchitravepioneers.psd" );
            sprite_Countdown = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/zenitharchitravecountdown.psd" );
        }

        private static int index = 0; //for cycling
        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            List<SafeSquadWrapper> Pioneers_ForNotification = Data.EntityList;
            if ( Pioneers_ForNotification.Count == 0 )
                return MouseHandlingResult.DoNotPlayClickSound;
            try
            {
                if ( index >= Pioneers_ForNotification.Count )
                    index = 0; //since index is a static it could be a stale value from before, so check here if the index is reasonable
                if ( Pioneers_ForNotification[index].Planet.IntelLevel == PlanetIntelLevel.Unexplored )
                {
                    index++;
                    return MouseHandlingResult.DoNotPlayClickSound;
                }

                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                    Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( Pioneers_ForNotification[index].Planet, false );
                else
                    World_AIW2.Instance.SwitchViewToPlanet( Pioneers_ForNotification[index].Planet );

                if ( index >= Pioneers_ForNotification.Count )
                    index = 0;
            }
            catch { }
            return MouseHandlingResult.DoNotPlayClickSound;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            //nothing to do on galaxy map hover
            //World_AIW2.Instance.FocusedPlanetForMapDarkening = Data.Planet;

            tooltipBuffer.Clear();
            if ( Data.NumPioneers > 0 )
            {
                if ( Data.ProvokingWar )
                {
                    tooltipBuffer.Add( Data.Faction.StartFactionColourForLog() + "Zenith Architrave</color> is large enough to provoke other Architraves to attack it, and will continue to expand with these " ).Add( Data.NumPioneers, "a1ffa1" ).Add( " pioneers." );
                }
                else
                {
                    tooltipBuffer.Add( Data.Faction.StartFactionColourForLog() + "Zenith Architrave</color> will remain in Expansion Mode until its remaining " ).Add( Data.NumPioneers, "a1ffa1" ).Add( " pioneers die; they can be killed or they will transform into spawners when they conquer a planet." );
                    if ( Data.anyTruce )
                        tooltipBuffer.Add( "\nThis Architrave is currently ignoring the truce with you until all its Pioneers are dead." );
                }
            }
            else if ( Data.eventTimeRemaining > 0 )
            {
                string timerColor = ArcenExternalUIUtilities.GetColorForNomadMoveTime( Data.eventTimeRemaining ); //timerColor gets more red the sooner the pioneers will appear
                tooltipBuffer.Add( Data.Faction.StartFactionColourForLog() + "Zenith Architrave</color> will enter expansion mode in " ).AddHoursAndMinutes( Data.eventTimeRemaining, timerColor ).Add( ".\nIn expansion mode the Architrave will build some Pioneers, powerful ships which can transform into new production facilities for the Architrave. They will remain in expansion mode until all of their pioneers are killed or transform." );
                ZenithArchitraveFactionBaseInfo gdata = Data.Faction.TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
                if ( gdata != null )
                {
                    if ( gdata.PlayerAllied )
                        tooltipBuffer.Add( "\n\nThis Architrave will still be your friend." );
                    else if ( Data.anyTruce )
                        tooltipBuffer.Add( "\n\nThis Architrave will also become hostile to everyone, even if you had a truce with them before, until the Pioneers all die." );
                }
            }
            if ( Data.TimesPioneersInterrupted > 0 )
                tooltipBuffer.Add( "\n\nThis Architrave was attacked " ).Add( Data.TimesPioneersInterrupted, "a1ffa1" ).Add( " times as it was preparing to launch Pioneers." );

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

                Faction fac = Data.Faction;

                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                //note that we can rely on the icon here 
                if ( Data.NumPioneers > 0 )
                {
                    buffer.Add( "Pioneers", fac == null ? "ffffff" : fac.FactionCenterColor.ColorHexBrighter );
                    Image.UpdateWith( sprite_Pioneers, true, "Human_Fin" );
                }
                else if ( Data.eventTimeRemaining > 0 )
                {
                    buffer.Add( "ZA", fac == null ? "ffffff" : fac.FactionCenterColor.ColorHexBrighter );
                    Image.UpdateWith( sprite_Countdown, true, "Human_Fin" );
                }
                else
                {
                    buffer.Add( "Expansion", fac == null ? "ffffff" : fac.FactionCenterColor.ColorHexBrighter );
                    Image.UpdateWith( sprite_Expansion, true, "Human_Fin" );
                }
                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                if ( Data.NumPioneers > 0 )
                {
                    buffer.Add( Data.NumPioneers, "a1ffa1" );
                }
                else if ( Data.eventTimeRemaining > 0 )
                {
                    buffer.AddSecondsRemaining( Data.eventTimeRemaining, TimeIntensity.TenMinutes );
                }
                else
                    buffer.Add( "Soon" );

                debugStage = 30;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicArchitraveExpansionNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
}
