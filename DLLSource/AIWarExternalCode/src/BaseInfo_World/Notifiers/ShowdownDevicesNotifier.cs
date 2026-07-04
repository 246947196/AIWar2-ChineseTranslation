using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ShowdownDevicesNotifier : NotifierBaseDataSingleton
    {
        public static ShowdownDevicesNotifier Instance = new ShowdownDevicesNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_ShowdownDevicesNotifier;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_ShowdownDevicesNotifier = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/astrotrainnotifier.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            GameEntity_Squad entity = Data.Entity.GetSquad();
            if ( entity == null )
                return MouseHandlingResult.PlayClickDeniedSound;
            PlanetFaction pFaction = entity.PlanetFaction;
            if ( pFaction == null )
                return MouseHandlingResult.PlayClickDeniedSound;
            Faction faction = pFaction.Faction;
            if ( faction == null )
                return MouseHandlingResult.PlayClickDeniedSound;

            if ( entity != null && entity.GetShouldBeVisibleBasedOnPlanetIntel() )
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
                string timerColor = ArcenExternalUIUtilities.GetColorForNomadMoveTime( Data.eventTimeRemaining );
                tooltipBuffer.Add( "AI与折跃网络的连接将在 " ).Add( Data.eventTimeRemaining.ToString(), timerColor ).Add(" 后断开，只要你或你的盟友控制 ").Add( (GlobalAIWorldBaseInfo.Instance.DevicesToSpawn - 1), "a1a1ff").Add(" 个决战装置").Add("\n");
                if ( Data.Int64List.Count == 4 )
                {
                    int exoTimer = (int)Data.Int64List[0];
                    int exoStrength = (int)Data.Int64List[1];
                    int wormholeInvasionTimer = (int)Data.Int64List[2];
                    int wormholeInvasionStrength = (int)Data.Int64List[3];
                    int timeTillExo = exoTimer - World_AIW2.Instance.GameSecond;
                    int timeTillWormhole = wormholeInvasionTimer - World_AIW2.Instance.GameSecond;

                    if ( timeTillExo < 0 )
                        tooltipBuffer.Add("\t下一次远征打击即将开始\n");
                    else
                        tooltipBuffer.Add("\t下一次远征打击将在 ").Add( timeTillExo, "a1ffa1" ).Add(" 后开始\n");
                    if ( timeTillWormhole < 0 )
                        tooltipBuffer.Add("\t下一次虫洞入侵即将开始\n");
                    else
                        tooltipBuffer.Add("\t下一次虫洞入侵将在 ").Add( timeTillWormhole, "ffa1a1" ).Add(" 后开始\n");
                }
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in Mouseover for Showdown Devices. debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
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
                InitIfNeeded();

                debugStage = 0;
                debugStage = 1;
                Image.UpdateWith( sprite_ShowdownDevicesNotifier, true, "AT" );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "决战\n" );
                SubTexts[0].Text.FinishWritingToBuffer();

                debugStage = 6;
                buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 9;
                if ( Data.eventTimeRemaining >= 0 )
                {
                    Data.eventTimeRemaining = GlobalAIWorldBaseInfo.Instance.SecondsUntilCrisis;
                    string timerColor = ArcenExternalUIUtilities.GetColorForNomadMoveTime( Data.eventTimeRemaining );
                    buffer.Add( Data.eventTimeRemaining.ToString(), timerColor );

                    buffer.Add( "\n" );
                }
                debugStage = 12;
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in ShowdownDevicesNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
    }
}
