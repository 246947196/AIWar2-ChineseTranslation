using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class PublicZenithMinerNotifier : NotifierBaseDataSingleton
    {
        public static PublicZenithMinerNotifier Instance = new PublicZenithMinerNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Probe;
        private static UnityEngine.Sprite sprite_Miner;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_Probe = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/miningprobe2.png" );
            sprite_Miner = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/activelymining.png" );
        }

        private static int index = 0;
        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            List<SafeSquadWrapper> miners = Data.EntityList;
            List<SafeSquadWrapper> probes = Data.EntityList2;
            GameEntity_Squad entity = null;
            if ( probes.Count == 0 && miners.Count == 1 )
            {
                entity = miners[0].GetSquad();
            }
            else if ( probes.Count == 1 && miners.Count == 0 )
                entity = probes[0].GetSquad();
            else
            {
                if ( index >= (probes.Count + miners.Count) )
                    index = 0;
                if ( probes.Count > 0 && index < probes.Count )
                {
                    entity = probes[index].GetSquad();
                }
                if ( miners.Count > 0 && index >= probes.Count && index < miners.Count )
                {
                    entity = miners[index].GetSquad();
                }
                index++;
            }
            if ( entity != null )
            {
                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                    Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( entity.Planet, false );
                else
                    World_AIW2.Instance.SwitchViewToPlanet( entity.Planet );
                return MouseHandlingResult.None;
            }
            return MouseHandlingResult.DoNotPlayClickSound;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                tooltipBuffer.Clear();
                List<SafeSquadWrapper> miners = Data.EntityList;
                List<SafeSquadWrapper> probes = Data.EntityList2;
                if ( probes.Count > 0 )
                {
                    debugCode = 200;
                    if ( probes.Count == 1 )
                    {
                        GameEntity_Squad probe = probes[0].GetSquad();
                        if ( probe == null )
                            return true;
                        World_AIW2.Instance.FocusedPlanetForMapDarkening = probe.Planet;
                        debugCode = 300;
                        ZenithMinersPerUnitBaseInfo data = probe.TryGetExternalBaseInfoAs<ZenithMinersPerUnitBaseInfo>();
                        string color = "a1ffa1";
                        if ( data != null )
                            color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.RemainingDuration );

                        tooltipBuffer.Add( "A Zenith Miner Probe is on " ).Add( probe.GetPlanetName_Safe(), "a1ffa1" ).Add( " and will summon a miner in " )
                            .Add( data == null ? "???" : data.RemainingDuration.ToString(), color ).Add( " seconds." ).Add( " The Miner will " )
                            .Add( data == null ? "???" : ZenithMinersFactionBaseInfo.EffectToString( data.Effect ) )
                            .Add( ", unless you or another faction destroys the Miner first.\n" );
                    }
                    else
                    {
                        debugCode = 400;
                        tooltipBuffer.Add( "There are " ).Add( probes.Count ).Add( " Zenith Miner Probes in the galaxy" ).Add( "\n" );
                        bool isFirst = true;
                        for ( int i = 0; i < probes.Count; i++ )
                        {
                            debugCode = 500;
                            GameEntity_Squad probe = probes[i].GetSquad();
                            if ( probe == null )
                                continue;
                            if ( isFirst )
                            {
                                isFirst = false;
                                World_AIW2.Instance.FocusedPlanetForMapDarkening = probe.Planet;
                            }
                            else
                                World_AIW2.Instance.AlsoFocusedPlanetsForMapDarkening[probe.Planet] = ArcenTime.TimeSinceStartF; //multi hover
                            ZenithMinersPerUnitBaseInfo data = probe.TryGetExternalBaseInfoAs<ZenithMinersPerUnitBaseInfo>();
                            string color = "a1ffa1";
                            if ( data != null )
                                color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.RemainingDuration );
                            tooltipBuffer.Add( "\t" ).Add( "The Probe on " ).Add( probe.GetPlanetName_Safe(), "a1ffa1" ).Add( " will summon a Zenith Miner in " )
                                .Add( data == null ? "???" : data.RemainingDuration.ToString(), color ).Add( " seconds." ).Add( " The Miner will " )
                                .Add( data == null ? "???" : ZenithMinersFactionBaseInfo.EffectToString( data.Effect ) ).Add( ". \n" );
                        }
                    }
                }
                debugCode = 600;
                if ( miners.Count > 0 )
                {
                    debugCode = 700;
                    if ( miners.Count == 1 )
                    {
                        debugCode = 800;
                        GameEntity_Squad miner = miners[0].GetSquad();
                        if ( miner == null || miner.HasBeenRemovedFromSim || miner.ToBeRemovedAtEndOfThisFrame )
                        { }
                        else
                        {
                            ZenithMinersPerUnitBaseInfo data = miner.TryGetExternalBaseInfoAs<ZenithMinersPerUnitBaseInfo>();
                            if ( data == null )
                            { }
                            else
                            {
                                debugCode = 900;
                                if ( data.InMiningMode )
                                {
                                    tooltipBuffer.Add( "A Zenith Miner is on " ).Add( miner.GetPlanetName_Safe(), "a1ffa1" ).Add( " and will " )
                                        .Add( data == null ? "???" : ZenithMinersFactionBaseInfo.EffectToString( data.Effect ) ).Add( " in " );
                                }
                                else
                                    tooltipBuffer.Add( "A Zenith Miner is on " ).Add( miner.GetPlanetName_Safe(), "a1ffa1" ).Add( " and is killing its enemies. Once it has relieved the planet of its most pressing enemies it will deploy its drill and " )
                                        .Add( data == null ? "???" : ZenithMinersFactionBaseInfo.EffectToString( data.Effect ) ).Add( " " );
                                string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.RemainingDuration ); //moveTimerColor gets more red the closer the planet is to moving                    
                                tooltipBuffer.Add( "in " ).Add( data == null ? "???" : data.RemainingDuration.ToString(), color ).Add( " seconds" ).Add( "\n" );
                            }
                        }
                    }
                    else
                    {
                        debugCode = 1000;
                        tooltipBuffer.Add( "There are " ).Add( miners.Count ).Add( " Zenith Miners active in the galaxy" ).Add( "\n" );
                        for ( int i = 0; i < miners.Count; i++ )
                        {
                            debugCode = 1100;
                            GameEntity_Squad miner = miners[i].GetSquad();
                            if ( miner == null || miner.HasBeenRemovedFromSim || miner.ToBeRemovedAtEndOfThisFrame )
                                continue;
                            ZenithMinersPerUnitBaseInfo data = miner.TryGetExternalBaseInfoAs<ZenithMinersPerUnitBaseInfo>();
                            if ( data == null )
                                continue;
                            if ( data.InMiningMode )
                            {
                                debugCode = 1200;
                                tooltipBuffer.Add( "\tThe Miner on " ).Add( miner.GetPlanetName_Safe(), "a1ffa1" ).Add( " will " )
                                    .Add( data == null ? "???" : ZenithMinersFactionBaseInfo.EffectToString( data.Effect ) ).Add( " in " );
                            }
                            else
                                tooltipBuffer.Add( "\tThe Miner on " ).Add( miner.GetPlanetName_Safe(), "a1ffa1" ).Add( " is killing all of its enemies. Once it has clear the planet it will deploy its drill and " )
                                    .Add( data == null ? "???" : ZenithMinersFactionBaseInfo.EffectToString( data.Effect ) ).Add( " " );
                            string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.RemainingDuration ); //moveTimerColor gets more red the closer the planet is to moving                    
                            tooltipBuffer.Add( "in " ).Add( data == null ? "???" : data.RemainingDuration.ToString(), color ).Add( " seconds." ).Add( "\n" );
                        }
                    }
                }
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in ZM mouseover notification debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
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
                //bool minersOnly = false;
                bool probesOnly = false;
                List<SafeSquadWrapper> miners = Data.EntityList;
                List<SafeSquadWrapper> probes = Data.EntityList2;
                if ( miners.Count == 0 && probes.Count > 0 )
                    probesOnly = true;
                //if ( probes.Count == 0 && miners.Count > 0 )
                //    minersOnly = true;
                debugStage = 10;
                if ( probesOnly )
                    Image.UpdateWith( sprite_Probe, true, "Human_Fin" );
                else
                    Image.UpdateWith( sprite_Miner, true, "Human_Fin" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                if ( probesOnly )
                    buffer.Add( "Probe" );
                else
                    buffer.Add( "Miner" );
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 20;

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                if ( probes.Count == 1 && miners.Count == 0 )
                {
                    debugStage = 25;
                    string color;
                    GameEntity_Squad probe = probes[0].GetSquad();
                    if ( probe == null )
                        return true;
                    ZenithMinersPerUnitBaseInfo data = probe.TryGetExternalBaseInfoAs<ZenithMinersPerUnitBaseInfo>();

                    if ( data != null && data.RemainingDuration >= 0 )
                        color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.RemainingDuration ); //moveTimerColor gets more red the closer the planet is to moving
                    else
                        color = probe.Planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter;

                    buffer.Add( probe.GetPlanetName_Safe(), color );
                }
                else if ( probes.Count == 0 && miners.Count == 1 )
                {
                    debugStage = 35;
                    GameEntity_Squad miner = miners[0].GetSquad();
                    if ( miner == null )
                        return true;
                    ZenithMinersPerUnitBaseInfo data = miner.TryGetExternalBaseInfoAs<ZenithMinersPerUnitBaseInfo>();
                    string color;
                    if ( data != null && data.RemainingDuration >= 0 )
                        color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.RemainingDuration ); //moveTimerColor gets more red the closer the planet is to moving
                    else
                        color = miner.Planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter;
                    buffer.Add( miners[0].GetPlanetName_Safe(), color );
                }
                else
                {
                    debugStage = 40;
                    buffer.Add( miners.Count + probes.Count ).Add( " planets" );
                }
                debugStage = 100;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicZenithMinerNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
}
