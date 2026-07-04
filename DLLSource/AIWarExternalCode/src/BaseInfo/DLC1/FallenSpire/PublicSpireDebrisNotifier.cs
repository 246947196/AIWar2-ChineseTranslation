using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class PublicSpireDebrisNotifier : NotifierBaseDataSingleton
    {
        public static PublicSpireDebrisNotifier Instance = new PublicSpireDebrisNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Debris;
        private static bool hasInitialized = false;
        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_Debris = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/spiredebris.png" );
        }

        private static int index = 0;
        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            if ( index >= Data.EntityList.Count )
                index = 0; //since index is a static it could be a stale value from before, so check here if the index is reasonable

            var e = Data.EntityList[index].GetSquad();
            if ( e == null )
                return MouseHandlingResult.PlayClickDeniedSound;
            
            Planet planet = e.Planet;
            if ( planet == null )
                return MouseHandlingResult.PlayClickDeniedSound;

            ObjectiveGenerator.CenteringHelper(planet, e);

            index++;
            if ( index >= Data.EntityList.Count )
                index = 0;
            
            return MouseHandlingResult.None;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            tooltipBuffer.Clear();
            if ( Data.EntityList.Count == 1 )
                tooltipBuffer.Add( "There is " ).Add( Data.EntityList.Count, "a1ffa1" ).Add( " piece of spire debris in the galaxy.\n" );
            else
                tooltipBuffer.Add( "星系中有 " ).Add( Data.EntityList.Count, "a1ffa1" ).Add( " 块尖塔残骸。\n" );
            int debugCode = 0;
            try
            {
                debugCode = 100;
                bool isFirst = true;
                for ( int i = 0; i < Data.EntityList.Count; i++ )
                {
                    debugCode = 200;
                    GameEntity_Squad debris = Data.EntityList[i].GetSquad();
                    if ( debris == null )
                        continue;
                    debugCode = 210;
                    FallenSpirePerUnitBaseInfo debrisData = debris.TryGetExternalBaseInfoAs<FallenSpirePerUnitBaseInfo>();
                    if ( debrisData == null )
                        continue;
                    int remainingTime = debrisData.TimeUntilDebrisVanishes - World_AIW2.Instance.GameSecond;
                    Planet planet = debris.Planet;
                    if ( isFirst )
                    {
                        isFirst = false;
                        World_AIW2.Instance.FocusedPlanetForMapDarkening = planet;
                    }
                    else
                        World_AIW2.Instance.AlsoFocusedPlanetsForMapDarkening[planet] = ArcenTime.TimeSinceStartF; //multi hover
                    debugCode = 220;
                    if ( debris.AmIBeingHacked() )
                        tooltipBuffer.Add( "\t位于 " ).Add( planet.Name, planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( " 的残骸正在被入侵。" );
                    else
                        tooltipBuffer.Add( "\t位于 " ).Add( planet.Name, planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( " 的残骸将在 " ).AddHoursAndMinutes( remainingTime, "a1ffa1" ).Add( "后丢失。" );
                    debugCode = 230;
                    Faction destFaction = World_AIW2.Instance.GetFactionByIndex( debrisData.FactionIndexForDebris );
                    debugCode = 240;
                    if ( destFaction != null && !debris.AmIBeingHacked() )
                    {
                        tooltipBuffer.Add( " 残骸将被 " ).Add( destFaction.GetDisplayName(), destFaction.FactionCenterColor.ColorHexBrighter );
                        debugCode = 250;
                        if ( destFaction.Type == FactionType.AI )
                            tooltipBuffer.Add( " 将其转化为强大的尖塔舰船使用。" );
                        else
                            tooltipBuffer.Add( " 将利用其技术建造新舰船。" );
                    }
                    tooltipBuffer.Add( "\n" );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in Debris notification mouseover handler, code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
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
                Image.UpdateWith( sprite_Debris, true, "SpireDebris" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "残骸\n" );
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 20;
                buffer = SubTexts[1].Text.StartWritingToBuffer();
                buffer.Add( Data.EntityList.Count );
                debugStage = 30;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicSpireDebrisNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
}
