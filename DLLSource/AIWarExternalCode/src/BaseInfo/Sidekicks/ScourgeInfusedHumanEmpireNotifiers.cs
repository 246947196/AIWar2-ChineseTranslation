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
            tooltipBuffer.Add("Your scourge vassals have upgradable structures.\n");
            tooltipBuffer.Add("Here are their locations:\n");
            for ( int i = 0; i < Data.EntityList.Count; i++ )
            {
                GameEntity_Squad structure = Data.EntityList[i].GetSquad();
                if ( structure == null )
                    continue;
                tooltipBuffer.Add("\tA mark ").Add( structure.CurrentMarkLevel, "a1ffa1" ).Add( " ").Add(structure.TypeData.GetDisplayName() ).Add(" on ").Add(structure.Planet.Name, "ffbba1").Add(" can be upgraded.\n").Add("\n");
            }
            tooltipBuffer.Add("\nUse the hacking menu to upgrade your structures.\n");
            tooltipBuffer.Add("\nYou can give your scourge vassals some instructions by using the Edit Planet part of the galaxy menu; set the Priority Level to a value between Fire_Low and Fire_Override. This will make the scourge want to interact with those planets (Low being less important, Override being most important).\n");
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
                buffer.Add( "Scourge\n" );
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
            tooltipBuffer.Add("Your scourge vassals have some flowers growing.\n");
            tooltipBuffer.Add("Here are their locations:\n");
            for ( int i = 0; i < Data.EntityList.Count; i++ )
            {
                GameEntity_Squad flower = Data.EntityList[i].GetSquad();
                if ( flower == null )
                    continue;
                tooltipBuffer.Add("\tA ").Add(flower.TypeData.GetDisplayName() ).Add(" on ").Add(flower.Planet.Name, "ffbba1").Add(" is growing and must be protected from the AI.\n").Add("\n");
            }
            tooltipBuffer.Add("\nIf it finishes growing it will become a Corbomite Crystal and can be destroyed to grant Corbomite.\n");
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
                buffer.Add( "Flowers\n" );
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
