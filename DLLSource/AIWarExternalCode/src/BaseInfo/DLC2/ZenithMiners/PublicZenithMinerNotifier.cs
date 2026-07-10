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

                        tooltipBuffer.Add( "天顶矿工探测器在 " ).Add( probe.GetPlanetName_Safe(), "a1ffa1" ).Add( " 上，将在 " )
                            .Add( data == null ? "???" : data.RemainingDuration.ToString(), color ).Add( " 秒后召唤矿工。" ).Add( "矿工将" )
                            .Add( data == null ? "???" : ZenithMinersFactionBaseInfo.EffectToString( data.Effect ) )
                            .Add( "，除非您或其他派系先摧毁矿工。\n" );
                    }
                    else
                    {
                        debugCode = 400;
                        tooltipBuffer.Add( "银河系中有 " ).Add( probes.Count ).Add( " 个天顶矿工探测器" ).Add( "\n" );
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
                            tooltipBuffer.Add( "\t" ).Add( "探测器在 " ).Add( probe.GetPlanetName_Safe(), "a1ffa1" ).Add( " 上，将在 " )
                                .Add( data == null ? "???" : data.RemainingDuration.ToString(), color ).Add( " 秒后召唤天顶矿工。" ).Add( "矿工将" )
                                .Add( data == null ? "???" : ZenithMinersFactionBaseInfo.EffectToString( data.Effect ) ).Add( "。\n" );
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
                                tooltipBuffer.Add( "天顶矿工在 " ).Add( miner.GetPlanetName_Safe(), "a1ffa1" ).Add( " 上，将" )
                                    .Add( data == null ? "???" : ZenithMinersFactionBaseInfo.EffectToString( data.Effect ) ).Add( " " );
                                }
                                else
                                    tooltipBuffer.Add( "天顶矿工在 " ).Add( miner.GetPlanetName_Safe(), "a1ffa1" ).Add( " 上，正在击杀敌人。一旦清除了星球上最紧迫的敌人，它将部署钻头并" )
                                        .Add( data == null ? "???" : ZenithMinersFactionBaseInfo.EffectToString( data.Effect ) ).Add( " " );
                                string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.RemainingDuration ); //moveTimerColor gets more red the closer the planet is to moving                    
                                tooltipBuffer.Add( "在 " ).Add( data == null ? "???" : data.RemainingDuration.ToString(), color ).Add( " 秒后" ).Add( "\n" );
                            }
                        }
                    }
                    else
                    {
                        debugCode = 1000;
                        tooltipBuffer.Add( "银河系中有 " ).Add( miners.Count ).Add( " 个天顶矿工活跃" ).Add( "\n" );
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
                                tooltipBuffer.Add( "\t矿工在 " ).Add( miner.GetPlanetName_Safe(), "a1ffa1" ).Add( " 上，将" )
                                    .Add( data == null ? "???" : ZenithMinersFactionBaseInfo.EffectToString( data.Effect ) ).Add( " " );
                            }
                            else
                                tooltipBuffer.Add( "\t矿工在 " ).Add( miner.GetPlanetName_Safe(), "a1ffa1" ).Add( " 上正在击杀所有敌人。一旦清除了星球上的敌人，它将部署钻头并" )
                                    .Add( data == null ? "???" : ZenithMinersFactionBaseInfo.EffectToString( data.Effect ) ).Add( " " );
                            string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.RemainingDuration ); //moveTimerColor gets more red the closer the planet is to moving                    
                            tooltipBuffer.Add( "在 " ).Add( data == null ? "???" : data.RemainingDuration.ToString(), color ).Add( " 秒后。" ).Add( "\n" );
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
                    buffer.Add( "探测器" );
                else
                    buffer.Add( "矿工" );
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
                    buffer.Add( miners.Count + probes.Count ).Add( " 个星球" );
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
