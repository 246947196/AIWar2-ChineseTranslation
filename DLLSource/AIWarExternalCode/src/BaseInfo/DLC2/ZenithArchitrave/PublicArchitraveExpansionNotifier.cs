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
                    tooltipBuffer.Add( Data.Faction.StartFactionColourForLog() + "天顶拱门</color> 已足够庞大，将激起其他拱门攻击，并继续以这 " ).Add( Data.NumPioneers, "a1ffa1" ).Add( " 个先锋进行扩张。" );
                }
                else
                {
                    tooltipBuffer.Add( Data.Faction.StartFactionColourForLog() + "天顶拱门</color> 将保持扩张模式，直到剩余的 " ).Add( Data.NumPioneers, "a1ffa1" ).Add( " 个先锋死亡；它们可以被击杀，或在征服星球时转化为生成器。" );
                    if ( Data.anyTruce )
                        tooltipBuffer.Add( "\n此拱门目前无视与您的休战协议，直到所有先锋死亡。" );
                }
            }
            else if ( Data.eventTimeRemaining > 0 )
            {
                string timerColor = ArcenExternalUIUtilities.GetColorForNomadMoveTime( Data.eventTimeRemaining ); //timerColor gets more red the sooner the pioneers will appear
                tooltipBuffer.Add( Data.Faction.StartFactionColourForLog() + "天顶拱门</color> 将在 " ).AddHoursAndMinutes( Data.eventTimeRemaining, timerColor ).Add( " 后进入扩张模式。\n在扩张模式下，拱门将建造一些先锋船——强大的舰船，可转化为拱门的新的生产设施。他们将保持扩张模式，直到所有先锋被击杀或转化。" );
                ZenithArchitraveFactionBaseInfo gdata = Data.Faction.TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
                if ( gdata != null )
                {
                    if ( gdata.PlayerAllied )
                        tooltipBuffer.Add( "\n\n此拱门仍将与您保持友好。" );
                    else if ( Data.anyTruce )
                        tooltipBuffer.Add( "\n\n此拱门将对所有人敌对，即使之前与您有休战协议，直到所有先锋死亡。" );
                }
            }
            if ( Data.TimesPioneersInterrupted > 0 )
                tooltipBuffer.Add( "\n\n此拱门在准备发射先锋时被攻击了 " ).Add( Data.TimesPioneersInterrupted, "a1ffa1" ).Add( " 次。" );

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
