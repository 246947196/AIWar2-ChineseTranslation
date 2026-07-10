using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class ScourgeUpgradableStructureNotifier : NotifierBaseDataSingleton
    {
        public static ScourgeUpgradableStructureNotifier Instance = new ScourgeUpgradableStructureNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Building;
        private static bool hasInitialized = false;
        private static int index = 0;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_Building = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/darkdata.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            if ( index >= Data.EntityList.Count )
                index = 0; //since index is a static it could be a stale value from before, so check here if the index is reasonable

            if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( Data.EntityList[index].Planet, false );
            else
                World_AIW2.Instance.SwitchViewToPlanet( Data.EntityList[index].Planet );
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
            if ( Data.EntityList.Count == 0 )
                return true;
            tooltipBuffer.Add("您的天灾附庸有可升级的建筑。\n");
            tooltipBuffer.Add("以下是它们的位置：\n");
            for ( int i = 0; i < Data.EntityList.Count; i++ )
            {
                GameEntity_Squad structure = Data.EntityList[i].GetSquad();
                if ( structure == null )
                    continue;
                tooltipBuffer.Add("\t一个等级 ").Add( structure.CurrentMarkLevel, "a1ffa1" ).Add( " ").Add(structure.TypeData.GetDisplayName() ).Add(" 在 ").Add(structure.Planet.Name, "ffbba1").Add(" 可以升级。\n").Add("\n");
            }
            tooltipBuffer.Add("\n使用破解菜单升级您的建筑。\n");
            tooltipBuffer.Add("\n您可以通过使用星系菜单的编辑星球部分给您的天灾附庸一些指示；将优先级级别设置为 Fire_Low 和 Fire_Override 之间的值。这将使天灾更倾向于与这些星球互动（Low 表示不太重要，Override 表示最重要）。\n");
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
                Image.UpdateWith( sprite_Building, true, "Scourge" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "天灾\n" );
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 20;
                if ( Data.EntityList.Count == 0 )
                    return true;
                if ( Data.EntityList.Count > 0 )
                {
                    buffer.Add( Data.EntityList.Count );
                }
                else
                {
                    GameEntity_Squad structure = Data.EntityList[0].GetSquad();
                    if ( structure != null )
                        buffer.Add(structure.Planet.Name );
                    else
                        buffer.Add("1");
                }

                debugStage = 30;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in ScourgeUpgradableStructureNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
    public class ScourgeFlowerNotifier : NotifierBaseDataSingleton
    {
        public static ScourgeFlowerNotifier Instance = new ScourgeFlowerNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Building;
        private static bool hasInitialized = false;
        private static int index = 0;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_Building = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/darkmatter2.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            if ( index >= Data.EntityList.Count )
                index = 0; //since index is a static it could be a stale value from before, so check here if the index is reasonable

            if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( Data.EntityList[index].Planet, false );
            else
                World_AIW2.Instance.SwitchViewToPlanet( Data.EntityList[index].Planet );
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
            if ( Data.EntityList.Count == 0 )
                return true;
            tooltipBuffer.Add("您的天灾附庸有一些花正在生长。\n");
            tooltipBuffer.Add("以下是它们的位置：\n");
            for ( int i = 0; i < Data.EntityList.Count; i++ )
            {
                GameEntity_Squad flower = Data.EntityList[i].GetSquad();
                if ( flower == null )
                    continue;
                tooltipBuffer.Add("\t一个 ").Add(flower.TypeData.GetDisplayName() ).Add(" 在 ").Add(flower.Planet.Name, "ffbba1").Add(" 正在生长，必须防止被 AI 破坏。\n").Add("\n");
            }
            tooltipBuffer.Add("\n如果它完成生长，将变成科博迈特水晶，摧毁后可获得科博迈特。\n");
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
                Image.UpdateWith( sprite_Building, true, "Flowers" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "花朵\n" );
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 20;
                if ( Data.EntityList.Count == 0 )
                    return true;
                if ( Data.EntityList.Count > 0 )
                {
                    buffer.Add( Data.EntityList.Count );
                }
                else
                {
                    GameEntity_Squad structure = Data.EntityList[0].GetSquad();
                    if ( structure != null )
                        buffer.Add(structure.Planet.Name );
                    else
                        buffer.Add("1");
                }

                debugStage = 30;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in ScourgeFlowerNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
}
