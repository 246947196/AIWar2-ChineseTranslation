using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class WormholeInvasionNotifier : NotifierBaseDataSingleton
    {
        public static WormholeInvasionNotifier Instance = new WormholeInvasionNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_WormholeInvasion;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_WormholeInvasion = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/wormholeinvasion.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            Planet planet = Data.Planet;
            if ( planet == null )
                return MouseHandlingResult.PlayClickDeniedSound;

            if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( planet, false );
            else
                World_AIW2.Instance.SwitchViewToPlanet( planet );
            return MouseHandlingResult.DoNotPlayClickSound;
        }

        public override bool GetShouldBeHidden( NotifierFillData Data )
        {
            return false;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            int debugCode = 0;
            try{
                debugCode = 100;
                Planet planet = Data.Planet;
                if ( planet == null )
                    return false;

                Planet destPlanet = Data.PlanetList[0];

                //galaxy map hover
                World_AIW2.Instance.FocusedPlanetForMapDarkening = planet;
                if (destPlanet != null) {
                    World_AIW2.Instance.AlsoFocusedPlanetsForMapDarkening[destPlanet] = ArcenTime.TimeSinceStartF; //multi hover
                }
                debugCode = 200;
                string colorString = string.Empty;
                colorString = Data.Faction.FactionCenterColor.ColorHexBrighter;
                
                tooltipBuffer.Clear();
                if ( destPlanet == null )
                    return false;
                string destColorString = string.Empty;
                destColorString = destPlanet.GetControllingOrInfluencingFaction().FactionCenterColor.ColorHexBrighter;
                debugCode = 300;
                if ( Data.Int64List.Count != 3 )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("A, len " + Data.Int64List.Count, Verbosity.DoNotShow );
                    return false;
                }
                debugCode = 400;
                int projectorAppearanceTime = (int)Data.Int64List[0];
                int planetLinkTime = (int)Data.Int64List[1];
                int nextWaveTime = (int)Data.Int64List[2];
                int timeTillProjectorAppears = projectorAppearanceTime - World_AIW2.Instance.GameSecond;
                int timeTillProjectorLinks = planetLinkTime - World_AIW2.Instance.GameSecond;
                int timeTillNextWave = nextWaveTime - World_AIW2.Instance.GameSecond;
                if ( timeTillProjectorAppears > 0  )
                {
                    debugCode = 500;
                    tooltipBuffer.Add( "一个 ").Add("虫洞投射器", colorString ).Add( " 将在 " ).Add(  planet.Name, colorString).Add(" 上出现，倒计时 " ).AddHoursAndMinutes( timeTillProjectorAppears, "a1a1ff").Add("。\n");
                    tooltipBuffer.Add( "在 ").AddHoursAndMinutes(timeTillProjectorLinks, "ffa1a1").Add(" 后，一个虫洞将连接 ").Add(planet.Name, colorString).Add(" 和 ").Add(destPlanet.Name, destColorString).Add("。\n");
                    tooltipBuffer.Add( "在 ").AddHoursAndMinutes(timeTillNextWave, "ffa1ff").Add(" 后，一波攻击将在投射器处生成。\n");
                }
                else if (timeTillProjectorLinks > 0 )
                {
                    debugCode = 600;
                    tooltipBuffer.Add( "在 ").AddHoursAndMinutes(timeTillProjectorLinks, "ffa1a1").Add(" 后，一个虫洞将连接 ").Add( planet.Name, colorString).Add(" 和 ").Add(destPlanet.Name, destColorString).Add("，然后发动一些攻击。\n");
                    tooltipBuffer.Add( "在 ").AddHoursAndMinutes(timeTillNextWave, "ffa1ff").Add(" 后，一波攻击将在投射器处生成。\n");
                }
                else if ( timeTillNextWave > 0 )
                {
                    debugCode = 700;
                    tooltipBuffer.Add( "在 ").AddHoursAndMinutes(timeTillNextWave, "ffa1a1").Add(" 后，一波攻击将在投射器处生成。\n");
                    tooltipBuffer.Add("攻击力量：\n");
                    int strength = 0;
                    foreach ( KeyValuePair<GameEntityTypeData, int> kv in Data.ShipDictionary )
                    {
                        tooltipBuffer.Add("\t").Add( kv.Key.GetDisplayName() ).Add(": ").Add(kv.Value).Add("\n");
                        GameEntityTypeData.MarkLevelStats markLevelStats = kv.Key.MarkStatsFor( Data.Faction.CurrentGeneralMarkLevel );
                        strength += (markLevelStats.StrengthPerSquad_CalculatedWithNullFleetMembership * kv.Value);
                    }
                    if ( strength < 1000 )
                        strength = 1000;
                    tooltipBuffer.Add("总力量：").Add( (strength/1000).ToString(), "ff0000");
                }
                debugCode = 800;
                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, tooltipBuffer.GetStringAndResetForNextUpdate() );
            } catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in mouseover for wormhole invasions. debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            return true;
        }

        public override bool ContentGetter( NotifierFillData Data, ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
        {
            int debugStage = -1;
            try
            {
                InitIfNeeded();

                Planet planet = Data.Planet;
                if ( planet == null )
                    return false;

                debugStage = 0;
                debugStage = 1;
                Image.UpdateWith( sprite_WormholeInvasion, true, "Human_Fin" );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "虫洞\n" );
                SubTexts[0].Text.FinishWritingToBuffer();

                debugStage = 6;
                buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 9;

                buffer.Add( planet.Name );
                int projectorAppearanceTime = (int)Data.Int64List[0];
                int planetLinkTime = (int)Data.Int64List[1];
                int nextWaveTime = (int)Data.Int64List[2];
                int timeTillProjectorAppears = projectorAppearanceTime - World_AIW2.Instance.GameSecond;
                int timeTillProjectorLinks = planetLinkTime - World_AIW2.Instance.GameSecond;
                int timeTillNextWave = nextWaveTime - World_AIW2.Instance.GameSecond;

                debugStage = 12;
                buffer.Add( "\n" );
                debugStage = 13;
                if ( timeTillProjectorAppears > 0 )
                    buffer.AddSecondsRemaining( timeTillProjectorAppears );
                else if ( timeTillProjectorLinks > 0 )
                    buffer.AddSecondsRemaining( timeTillProjectorLinks );
                else if ( timeTillNextWave > 0 )
                    buffer.AddSecondsRemaining( timeTillNextWave );

                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PrivateDevourerNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
    }
}
