using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class SphereFactionNotifier : NotifierBaseDataSingleton
    {
        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite iconSprite;
        private static bool hasInitialized = false;

        public static SphereFactionNotifier Instance = new SphereFactionNotifier();

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            iconSprite = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/DysonSphere.png" );
        }

        private bool IsAnnoyed( NotifierFillData Data ) => Data.BoolList[0];
        private bool IsAngry( NotifierFillData Data ) => Data.BoolList[1];
        private bool IsAngryAtHack( NotifierFillData Data ) => Data.BoolList[2];
        private bool IsOnSphereWorld( NotifierFillData Data ) => Data.BoolList[3];
        private bool IsAntagonized( NotifierFillData Data ) => Data.BoolList[4];
        private bool AntagonizerExists( NotifierFillData Data ) => Data.BoolList[5];
        private int TimeLeft( NotifierFillData Data ) => (int)Data.Int64List[0];
        private int MaxStrength( NotifierFillData Data ) => (int)Data.Int16List[0];
        private string SphereType( NotifierFillData Data ) => Data.StringList[0];
        private Planet SpherePlanet( NotifierFillData Data ) => Data.PlanetList[0];
        private Planet AntagonizerPlanet( NotifierFillData Data ) => Data.PlanetList[1];

        private Planet lastPlanet = null;

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            if ( SpherePlanet( Data ) == null && AntagonizerPlanet( Data ) == null )
                return MouseHandlingResult.PlayClickDeniedSound;

            if ( !Data.inSearchMode )
            {
                Planet focusPlanet = SpherePlanet( Data );
                if ( (IsAntagonized( Data ) || AntagonizerExists( Data )) && lastPlanet == focusPlanet )
                    focusPlanet = AntagonizerPlanet( Data );

                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                    Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( focusPlanet, false );
                else
                    World_AIW2.Instance.SwitchViewToPlanet( focusPlanet );

                lastPlanet = focusPlanet;

                return MouseHandlingResult.None;
            }

            return MouseHandlingResult.DoNotPlayClickSound;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            tooltipBuffer.Clear();
            if ( IsAntagonized( Data ) )
            {
                tooltipBuffer.AddFactionColoredString( SphereType( Data ), Data.Faction ).Add( " " ).AddFactionNameInItsColor( Data.Faction ).EndColor().Add( $" is currently Antagonized by the AI, and is relentlessly hunting you down. You must defeat its Antagonizer located on {AntagonizerPlanet( Data ).Name} in order to free them from the AI's control.\n\n" );
            }
            else
            {
                if ( AntagonizerExists( Data ) )
                {
                    tooltipBuffer.AddFactionColoredString( SphereType( Data ), Data.Faction ).Add( " " ).AddFactionNameInItsColor( Data.Faction ).EndColor().Add( $" will soon be Antagonized by an AI Antagonizer on {AntagonizerPlanet( Data ).Name}. You have {(TimeLeft( Data ) / 60).ToString( "0" )}:{(TimeLeft( Data ) % 60).ToString( "00" )} until the Antagonizer activates.\n\n" );
                }

                if ( IsAngryAtHack( Data ) )
                {
                    tooltipBuffer.AddFactionColoredString( SphereType( Data ), Data.Faction ).Add( " " ).AddFactionNameInItsColor( Data.Faction ).EndColor().Add( $" is currently Angry at you due to being hacked and will remain as such for {TimeLeft( Data )} more seconds.\n\n" );
                }
                else if ( IsAngry( Data ) )
                {
                    tooltipBuffer.AddFactionColoredString( SphereType( Data ), Data.Faction ).Add( " " ).AddFactionNameInItsColor( Data.Faction ).EndColor().Add( $" is currently Angry at you, and will remain as such for {TimeLeft( Data )} more seconds.\n\n" );
                }
                else if ( IsAnnoyed( Data ) )
                {
                    tooltipBuffer.AddFactionColoredString( SphereType( Data ), Data.Faction ).Add( " " ).AddFactionNameInItsColor( Data.Faction ).EndColor().Add( $" is currently Annoyed at you, and will turn aggressive in {TimeLeft( Data )} seconds." );
                    if ( IsOnSphereWorld( Data ) )
                        tooltipBuffer.Add( " They are annoyed at you for having over " ).StartStrength( true ).Add( MaxStrength( Data ) ).EndColor().Add( " strength on their Sphere world.\n\n" );
                    else
                        tooltipBuffer.Add( " They are annoyed at you for having over " ).StartStrength( true ).Add( MaxStrength( Data ) ).EndColor().Add( " strength near their military forces.\n\n" );
                }
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

                debugStage = 10;
                Image.UpdateWith( iconSprite, true, "DysonSphere" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();

                buffer.AddFactionColoredString( "Sphere\n", Data.Faction ).EndColor();
                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 20;

                if ( AntagonizerExists( Data ) && !IsAntagonized( Data ) )
                {
                    buffer.Add( "挑衅\n" );
                    buffer.Add( $"{(TimeLeft( Data ) / 60).ToString( "0" )}:{(TimeLeft( Data ) % 60).ToString( "00" )}" );
                }
                else
                {
                    if ( IsAntagonized( Data ) )
                    {
                        buffer.Add( "挑衅\n" );
                        buffer.Add( AntagonizerPlanet( Data ).Name );
                    }
                    else if ( IsAngryAtHack( Data ) || IsAngry( Data ) )
                    {
                        buffer.Add( "愤怒\n" );
                        buffer.Add( (TimeLeft( Data ) / 60) ).Add( ":" ).AddPaddedInt( (TimeLeft( Data ) % 60), 2 );
                    }
                    else if ( IsAnnoyed( Data ) )
                    {
                        buffer.Add( "恼怒\n" );
                        buffer.Add( (TimeLeft( Data ) / 60) ).Add( ":" ).AddPaddedInt( (TimeLeft( Data ) % 60), 2 );
                    }
                }

                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in SphereFactionNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
}
