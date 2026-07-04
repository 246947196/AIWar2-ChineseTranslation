using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class PublicAIReservesNotifier : NotifierBaseDataSingleton
    {
        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_AIReserves;
        private static bool hasInitialized = false;

        public static PublicAIReservesNotifier Instance = new PublicAIReservesNotifier();

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_AIReserves = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/servers.png" );
        }

        private static int index = 0;
        public override MouseHandlingResult ClickHandler( NotifierFillData BaseInfo )
        {
            AIReservesFactionBaseInfo data = FactionUtilityMethods.Instance.GetAIReservesFactionBaseInfo();
            List<SafeSquadWrapper> wormholes = data.Wormholes.GetDisplayList();
            if ( wormholes.Count == 0 )
                return MouseHandlingResult.None;

            if ( index >= wormholes.Count )
                index = 0; //since index is a static it could be a stale value from before, so check here if the index is reasonable

            if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( wormholes[index].Planet, false );
            else
                World_AIW2.Instance.SwitchViewToPlanet( wormholes[index].Planet );
            index++;
            if ( index >= wormholes.Count )
                index = 0;
            return MouseHandlingResult.None;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData BaseInfo )
        {
            //nothing to do on galaxy map hover
            //World_AIW2.Instance.FocusedPlanetForMapDarkening = BaseInfo.Planet;

            AIReservesFactionBaseInfo data = FactionUtilityMethods.Instance.GetAIReservesFactionBaseInfo();
            List<SafeSquadWrapper> wormholes = data.Wormholes.GetDisplayList();
            bool isFirst = true;
            for ( int i = 0; i < wormholes.Count; i++ )
            {
                GameEntity_Squad hole = wormholes[i].GetSquad();
                if ( hole == null )
                    continue;

                if ( isFirst )
                {
                    isFirst = false;
                    World_AIW2.Instance.FocusedPlanetForMapDarkening = hole.Planet;
                }
                else
                    World_AIW2.Instance.AlsoFocusedPlanetsForMapDarkening[hole.Planet] = ArcenTime.TimeSinceStartF; //multi hover
            }

            tooltipBuffer.Clear();
            if ( data.AbsorbShipsMode )
                tooltipBuffer.Add( "The reserves are pulling out. Once all their ships are evacuated, the wormholes will start to destabilize and vanish.\n" );
            else if ( data.TimeForNextWormhole <= -1 )
                tooltipBuffer.Add( "The AI Reserves are responding to your deepstrike\n" );
            else
            {
                tooltipBuffer.Add( "The AI Reserves will open a new wormhole to bring more reinforcements in " ).AddHoursAndMinutes( data.TimeForNextWormhole - World_AIW2.Instance.GameSecond, "a1ffa1" ).Add( ".\n" );
            }
            tooltipBuffer.Add( "\tThere are currently " ).Add( data.Wormholes.Count, "ffa1a1" ).Add( " wormholes that can spawn AI reserves." );
            if ( BaseInfo.PlanetList.Count > 0 )
            {
                tooltipBuffer.Add( "\nThe AI is responding to player forces on " );
                for ( int j = 0; j < BaseInfo.PlanetList.Count; j++ )
                {
                    tooltipBuffer.Add("\t").Add(BaseInfo.PlanetList[j].Name, "a1a1a1");
                }
                tooltipBuffer.Add( "." );
            }
            Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, tooltipBuffer.GetStringAndResetForNextUpdate() );
            return true;
        }

        public override bool GetShouldBeHidden( NotifierFillData Data )
        {
            return false;
        }

        public override bool ContentGetter( NotifierFillData BaseInfo, ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
        {
            int debugStage = -1;
            try
            {
                debugStage = 0;
                InitIfNeeded();
                AIReservesFactionBaseInfo data = FactionUtilityMethods.Instance.GetAIReservesFactionBaseInfo();
                debugStage = 10;
                Image.UpdateWith( sprite_AIReserves, true, "AIR" );
                debugStage = 20;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                debugStage = 30;
                buffer.Add( "Deepstrike" );
                debugStage = 40;
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 50;
                buffer = SubTexts[1].Text.StartWritingToBuffer();
                debugStage = 60;

                if ( data.AbsorbShipsMode )
                {
                    buffer.Add( "ABSORB\n", "dfffa1" );
                    //line 2 is the number of wormholes
                    buffer.Add( data.Wormholes.Count.ToString(), "dfffa1" );
                }
                else //not in absorb mode
                {
                    //line 1 is the timer or nothing
                    if ( data.TimeForNextWormhole == -1 ||
                         data.TimeForNextWormhole < World_AIW2.Instance.GameSecond ) //this can happen on MP clients, since faction data is serialized infrequently
                        buffer.Add( "-" );
                    else
                        buffer.StartColor( "a1ffa1" ).Add( (data.TimeForNextWormhole - World_AIW2.Instance.GameSecond) ).Add( "s" ).EndColor();
                    //line 2 is the number of wormholes
                    buffer.Add( "\n" ).Add( data.Wormholes.Count.ToString(), "ffa1a1" );
                }

                debugStage = 70;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicAIReservesNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
}
