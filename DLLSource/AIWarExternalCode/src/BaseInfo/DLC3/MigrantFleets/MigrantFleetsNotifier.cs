using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class MigrantFleetsNotifier : NotifierBaseDataSingleton
    {
        public static MigrantFleetsNotifier Instance = new MigrantFleetsNotifier();

        // these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite icon;
        private static bool hasInitialized = false;
        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            icon = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/spiredebris.png" );
        }

        private static int index = 0;
        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            index++;

            int absoluteMax = Data.EntityList.Count + Data.EntityList2.Count;
            if ( index >= absoluteMax )
                index = 0;

            Planet targetPlanet = index < Data.EntityList.Count ? Data.EntityList[index].Planet : Data.EntityList2[index - Data.EntityList.Count].Planet;

            if ( targetPlanet != null )
                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                    Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( targetPlanet, false );
                else
                    World_AIW2.Instance.SwitchViewToPlanet( targetPlanet );

            return MouseHandlingResult.None;

        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            Dictionary<Planet, int> wormholeList = Planet.GetTemporaryPlanetDictOfInts( "MigrantFleetsNotifier-wormholeList", 10f );
            if ( wormholeList == null ) //blocked for teardown/shutdown; bail
                return false;
            Dictionary<Planet, int> migrantList = Planet.GetTemporaryPlanetDictOfInts( "MigrantFleetsNotifier-migrantList", 10f );
            if ( migrantList == null ) //blocked for teardown/shutdown; bail
            {
                Planet.ReleaseTemporaryPlanetDictOfInts( wormholeList ); //release already-acquired temp before bailing (finally not yet entered)
                return false;
            }
            int unexploredWormholes = 0;
            int unexploredMigrants = 0;

            tooltipBuffer.Clear();
            int debugCode = 0;
            try
            {
                debugCode = 1;
                Data.EntityList.ForEach( wormholeWrapper =>
                {
                    GameEntity_Squad wormhole = wormholeWrapper.GetSquad();
                    if ( wormhole == null )
                        return;
                    if ( wormhole.Planet.IntelLevel == PlanetIntelLevel.Unexplored )
                    {
                        unexploredWormholes++;
                    }
                    else if ( wormholeList.GetHasKey( wormhole.Planet ) )
                    {
                        wormholeList[wormhole.Planet]++;
                    }
                    else
                    {
                        wormholeList.Add( wormhole.Planet, 1 );
                    }
                } );

                debugCode = 2;
                Data.EntityList2.ForEach( migrantWrapper =>
                {
                    GameEntity_Squad migrant = migrantWrapper.GetSquad();
                    if ( migrant == null )
                        return;
                    if ( migrant.Planet.IntelLevel == PlanetIntelLevel.Unexplored )
                    {
                        unexploredMigrants++;
                    }
                    else if ( migrantList.GetHasKey( migrant.Planet ) )
                    {
                        migrantList[migrant.Planet]++;
                    }
                    else
                    {
                        migrantList.Add( migrant.Planet, 1 );
                    }
                } );

                debugCode = 100;
                bool isFirst = true;

                if ( wormholeList.Count > 0 )
                {
                    debugCode = 110;
                    tooltipBuffer.Add( $"There {(wormholeList.Count == 1 ? "is currently 1 planet" : $"are currently {wormholeList.Count} planets")} with at least one Wormhole on them.\n" );
                    debugCode = 120;
                    foreach ( KeyValuePair<Planet, int> pair in wormholeList )
                    {
                        tooltipBuffer.Add( $"{pair.Key.Name}: {pair.Value}\n" );

                        if ( isFirst )
                        {
                            isFirst = false;
                            World_AIW2.Instance.FocusedPlanetForMapDarkening = pair.Key;
                        }
                        else
                        {
                            World_AIW2.Instance.AlsoFocusedPlanetsForMapDarkening[pair.Key] = ArcenTime.TimeSinceStartF; //multi hover
                        }
                    }
                    tooltipBuffer.NewLine();
                }
                if ( unexploredWormholes > 0 )
                {
                    tooltipBuffer.Add( unexploredWormholes == 1 ? "There is " : "There are " )
                        .Add( unexploredWormholes )
                        .Add( unexploredWormholes == 1 ? " Wormhole" : " Wormholes" )
                        .Add( " on unexplored planets." );
                }

                if ( migrantList.Count > 0 )
                {
                    debugCode = 130;
                    tooltipBuffer.Add( $"There {(migrantList.Count == 1 ? "is currently 1 unsecure planet" : $"are currently {migrantList.Count} unsecure planets")} with at least one Migrant on them.\n" );
                    debugCode = 140;
                    foreach ( KeyValuePair<Planet, int> pair in migrantList )
                    {
                        tooltipBuffer.Add( $"{pair.Key.Name}: {pair.Value}\n" );

                        if ( isFirst )
                        {
                            isFirst = false;
                            World_AIW2.Instance.FocusedPlanetForMapDarkening = pair.Key;
                        }
                        else
                        {
                            World_AIW2.Instance.AlsoFocusedPlanetsForMapDarkening[pair.Key] = ArcenTime.TimeSinceStartF; //multi hover
                        }
                    }
                    tooltipBuffer.NewLine();
                }
                if ( unexploredMigrants > 0 )
                {
                    tooltipBuffer.Add( unexploredMigrants == 1 ? "There is " : "There are " )
                        .Add( unexploredMigrants )
                        .Add( unexploredMigrants == 1 ? " Migrant" : " Migrants" )
                        .Add( " on unexplored planets." );
                }

                if ( Data.Int16List.Count > 0 )
                {
                    debugCode = 150;
                    tooltipBuffer.Add( $"There are currently {Data.Int16List[0]} Clanlings in our territory." );
                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                {
                    Verbosity verbosity;
                    if ( debugCode < 100 ) // Early errors can occur if entities die mid tooltip generation. In which case, gracefully fail and try again. Still log the error, as this means the additional null checks were not enough.
                        verbosity = Verbosity.DoNotShow;
                    else
                        verbosity = Verbosity.ShowAsError;

                    ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in MigrantFleets notification mouseover handler, code " + debugCode + " " + e.ToString(), verbosity );
                }
            }
            finally
            {
                Planet.ReleaseTemporaryPlanetDictOfInts( wormholeList );
                Planet.ReleaseTemporaryPlanetDictOfInts( migrantList );
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
                Image.UpdateWith( icon, true, "SpireDebris" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "Migrants" );
                SubTexts[0].Text.FinishWritingToBuffer();
                buffer = SubTexts[1].Text.StartWritingToBuffer();
                debugStage = 20;
                buffer.Add( $"Wmhols: {Data.EntityList.Count}\n" );
                debugStage = 30;
                buffer.Add( $"Mgrnts: {Data.EntityList2.Count}" );
                debugStage = 40;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in MigrantFleetsNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
}
