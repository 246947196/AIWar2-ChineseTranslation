using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using UnityEngine;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// Shown when one or more Lamassus have out-of-sync modules 鈥?new ApkalluZigguratSummoner
    /// structures were built on the Ziggurat's planet after the Lamassu was last synced.
    /// Data.EntityList contains the out-of-sync Lamassu squads.
    /// </summary>
    public class LamassuModuleDesyncNotifier : NotifierBaseDataSingleton
    {
        public static LamassuModuleDesyncNotifier Instance = new LamassuModuleDesyncNotifier();

        private static UnityEngine.Sprite sprite_Desync;
        private static bool hasInitialized = false;
        private static int index = 0;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_Desync = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/unspentfuelpoints.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            if ( Data.EntityList.Count == 0 )
                return MouseHandlingResult.None;
            if ( index >= Data.EntityList.Count )
                index = 0;

            if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( Data.EntityList[index].Planet, false );
            else
                World_AIW2.Instance.SwitchViewToPlanet( Data.EntityList[index].Planet );
            index++;
            if ( index >= Data.EntityList.Count )
                index = 0;
            return MouseHandlingResult.None;
        }

        public override bool MouseoverHandler( NotifierFillData Data )
        {
            tooltipBuffer.Clear();
            if ( Data.EntityList.Count == 0 )
                return true;

            int debugCode = 0;
            try
            {
                debugCode = 100;
                if ( Data.EntityList.Count == 1 )
                {
                    GameEntity_Squad lamassu = Data.EntityList[0].GetSquad();
                    if ( lamassu != null )
                    tooltipBuffer.Add( "位于 " ).Add( lamassu.Planet.Name, "a1ffa1" )
                        .Add( " 的拉玛苏有新武器可用。\n\n将其带回齐古拉特并使用重新同步模块黑客技术更新其装备。" );
                }
                else
                {
                    debugCode = 200;
                    tooltipBuffer.Add( Data.EntityList.Count, "ffcc22" ).Add( " 个拉玛苏有新武器可用：\n" );
                    for ( int i = 0; i < Data.EntityList.Count; i++ )
                    {
                        GameEntity_Squad lamassu = Data.EntityList[i].GetSquad();
                        if ( lamassu == null )
                            continue;
                        tooltipBuffer.Add( "\t" ).Add( lamassu.Planet.Name, "a1ffa1" ).Add( "\n" );
                    }
                    tooltipBuffer.Add( "\n将每个拉玛苏带回其齐古拉特并使用重新同步模块黑客技术更新其装备。" );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in LamassuModuleDesyncNotifier mouseover handler, code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
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
                Image.UpdateWith( sprite_Desync, true, "LamassuDesync" );

                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "重新同步\n", "a1ffa1" );
                SubTexts[0].Text.FinishWritingToBuffer();

                debugStage = 20;
                buffer = SubTexts[1].Text.StartWritingToBuffer();
                if ( Data.EntityList.Count > 1 )
                    buffer.Add( Data.EntityList.Count );
                SubTexts[1].Text.FinishWritingToBuffer();

                debugStage = 30;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in LamassuModuleDesyncNotifier.ContentGetter at stage " + debugStage + ": " + e.ToString(), Verbosity.ShowAsError );
            }
            return true;
        }
    }
}
