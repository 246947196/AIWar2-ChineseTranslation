using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class WormholeBorerNotifier : NotifierBaseDataSingleton
    {
        public static WormholeBorerNotifier Instance = new WormholeBorerNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Mobile;
        private static UnityEngine.Sprite sprite_ActivelyBoring;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_Mobile = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/wormholeborermobile.png" );
            sprite_ActivelyBoring = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/wormholecreation.png" );
        }

        private int cyclingIndex = 0;
        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            if ( cyclingIndex >= Data.EntityList.Count )
                cyclingIndex = 0;
            if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( Data.EntityList[cyclingIndex].Planet, false );
            else
                World_AIW2.Instance.SwitchViewToPlanet( Data.EntityList[cyclingIndex].Planet );
            cyclingIndex++;
            return MouseHandlingResult.None;
        }

        public override bool GetShouldBeHidden( NotifierFillData Data )
        {
            return false;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            tooltipBuffer.Clear();
            int debugCode = 0;
            try
            {
                bool isFirst = true;
                for ( int i = 0; i < Data.EntityList.Count; i++ )
                {
                    debugCode = 100;
                    GameEntity_Squad borer = Data.EntityList[i].GetSquad();
                    if ( borer == null )
                        continue;
                    debugCode = 200;
                    Planet startPlanet = World_AIW2.Instance.GetPlanetByIndex( borer.WBStartPlanet );
                    Planet destPlanet = World_AIW2.Instance.GetPlanetByIndex( borer.WBDestinationPlanet );
                    if ( startPlanet == null )
                        throw new Exception( "Invalid start planet for " + borer.ToString() );
                    if ( destPlanet == null )
                        throw new Exception( "Invalid dest planet for " + borer.ToString() );

                    if ( isFirst )
                    {
                        isFirst = false;
                        World_AIW2.Instance.FocusedPlanetForMapDarkening = startPlanet;
                    }
                    else
                        World_AIW2.Instance.AlsoFocusedPlanetsForMapDarkening[startPlanet] = ArcenTime.TimeSinceStartF; //multi hover
                    World_AIW2.Instance.AlsoFocusedPlanetsForMapDarkening[destPlanet] = ArcenTime.TimeSinceStartF; //multi hover

                    debugCode = 300;
                    if ( borer.TypeData.GetHasTag( "MobileWormholeBorer" ) )
                    {
                        debugCode = 400;
                        if ( borer.GetShouldBeVisibleBasedOnPlanetIntel() )
                            tooltipBuffer.Add( "A Mobile Wormhole Borer is on " ).Add( borer.GetPlanetName_Safe(), "a1ffa1" ).Add( " and is en route to " ).Add( startPlanet.Name, "a1ffa1" ).Add( "; it will then bore a wormhole to " ).Add( destPlanet.Name, "a1ffa1" ).Add( ".\n" );
                        else
                            tooltipBuffer.Add( "A Mobile Wormhole Borer is somewhere in the galaxy and is en route to " ).Add( startPlanet.Name, "a1ffa1" ).Add( "; it will then bore a wormhole to " ).Add( destPlanet.Name, "a1ffa1" ).Add( ".\n" );
                    }
                    else
                    {
                        debugCode = 500;
                        tooltipBuffer.Add( "A Wormhole Borer is on " ).Add( borer.GetPlanetName_Safe(), "a1ffa1" ).Add( " and is actively boring a new wormhole to " ).Add( destPlanet.Name, "a1ffa1" ).Add( ". It will complete in " ).AddHoursAndMinutes( this.GetRemainingBorerTime( Data, borer ), "ffa1a1" ).Add( ".\n" );
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit bug in borer mouseover handler code " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, tooltipBuffer.GetStringAndResetForNextUpdate() );
            return true;
        }

        public override bool ContentGetter( NotifierFillData Data, ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
        {
            int debugStage = -1;
            try
            {
                debugStage = 0;
                InitIfNeeded();
                //bool minersOnly = false;
                bool activeBorers = false;
                int shortestBoringTimeLeft = -1;
                for ( int i = 0; i < Data.EntityList.Count; i++ )
                {
                    if ( Data.EntityList[i].TypeData.GetHasTag( "WormholeBorer" ) )
                    {
                        activeBorers = true;
                        int timeLeft = GetRemainingBorerTime( Data, Data.EntityList[i].GetSquad() );
                        if ( timeLeft < shortestBoringTimeLeft ||
                             shortestBoringTimeLeft == -1 )
                            shortestBoringTimeLeft = timeLeft;
                    }
                }

                debugStage = 10;
                if ( activeBorers )
                    Image.UpdateWith( sprite_ActivelyBoring, true, "Human_Fin" );
                else
                    Image.UpdateWith( sprite_Mobile, true, "Human_Fin" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                if ( activeBorers )
                    buffer.Add( "Boring" );
                else
                    buffer.Add( "Mobile" );
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 20;
                buffer = SubTexts[1].Text.StartWritingToBuffer();
                if ( activeBorers )
                {
                    buffer.AddSecondsRemaining( shortestBoringTimeLeft );
                }
                debugStage = 100;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in WormholeBorerNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }

        private int GetRemainingBorerTime( NotifierFillData Data, GameEntity_Squad entity )
        {
            if ( entity == null )
                return 99;
            AISentinelsCoreData factionExternal = Data.Faction.TryGetAISentinelsCoreData()?.SentinelInfo;
            if ( factionExternal == null )
                return 99;
            AIDifficulty difficulty = factionExternal.AIDifficulty;
            int timeSinceCreated = World_AIW2.Instance.GameSecond - entity.GameSecondCreated;
            return (difficulty.WormholeBorerCompletionTime - timeSinceCreated).IntValue;
        }
    }
}
