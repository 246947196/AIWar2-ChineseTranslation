using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class SplinteringSpireCollectorNotifier : NotifierBaseDataSingleton
    {
        private static bool hasInitialized = false;

        public static SplinteringSpireCollectorNotifier Instance = new SplinteringSpireCollectorNotifier();

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
        }

        private const byte StringCount = 2;

        private int GetDerelictCount( NotifierFillData Data ) => Data.Int16List[0];
        private int GetLowestMinutes( NotifierFillData Data ) => Data.Int16List[1];
        private int GetLowestSeconds( NotifierFillData Data ) => Data.Int16List[2];

        private short clickedPlanetIndex = -1;
        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            clickedPlanetIndex++;
            if ( clickedPlanetIndex < 0 || clickedPlanetIndex >= Data.PlanetList.Count )
                clickedPlanetIndex = 0;

            if ( clickedPlanetIndex >= Data.PlanetList.Count ) // Theoretically possible if this notifier is added without planets. Which... would be very weird.
                return MouseHandlingResult.PlayClickDeniedSound;

            Planet planet = Data.PlanetList[clickedPlanetIndex];
            if ( planet == null )
                return MouseHandlingResult.PlayClickDeniedSound;

            if ( planet.IntelLevel > PlanetIntelLevel.Unexplored )
            {
                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                    Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( planet, false );
                else
                    World_AIW2.Instance.SwitchViewToPlanet( planet );
                return MouseHandlingResult.None;
            }

            return MouseHandlingResult.DoNotPlayClickSound;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            tooltipBuffer.Clear();

            // All formatting is handled before the data gets here. Just spit it out.
            for ( int x = 0; x + StringCount - 1 < Data.StringList.Count; x += StringCount )
            {
                tooltipBuffer.Add( Data.StringList[x] );
                tooltipBuffer.Add( Data.StringList[x+1] );
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
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();

                buffer.Add( "Derelicts\n" );
                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                buffer.Add( $"Count: {GetDerelictCount( Data )}\n" );

                debugStage = 20;

                buffer.Add( GetLowestMinutes( Data ) );
                buffer.Add( ':' );
                buffer.AddPaddedInt( GetLowestSeconds( Data ), 2 );

                debugStage = 30;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in SplinteringSpireCollectorNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
}
