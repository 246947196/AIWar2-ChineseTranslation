using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class PublicTemplarWaveNotifier : NotifierBaseDataSingleton
    {
        public static PublicTemplarWaveNotifier Instance = new PublicTemplarWaveNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Wave;
        private static bool hasInitialized = false;
        private static int index = 0;
        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            index = 0;
            hasInitialized = true;
            sprite_Wave = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/bronchialtube.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            if ( index >= Data.EntityList.Count )
                index = 0; //since index is a static it could be a stale value from before, so check here if the index is reasonable
            GameEntity_Squad entity = Data.EntityList[index].GetSquad();
            if ( entity != null &&
                 entity.GetShouldBeVisibleBasedOnPlanetIntel() )
            {
                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                    Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( entity.Planet, false );
                else
                    World_AIW2.Instance.SwitchViewToPlanet( entity.Planet );
            }
            index++;
            return MouseHandlingResult.None;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {

            int debugCode = 0;
            try
            {
                debugCode = 100;
                tooltipBuffer.Add("The Templar have ").Add( Data.EntityList.Count, "a1ffa1" ).Add(" wave leaders in command of attacks against the necromancer.\n");
                bool anyPrinted = false;
                for (int i = 0; i < Data.EntityList.Count; i++ )
                {
                    GameEntity_Squad entity = Data.EntityList[i].GetSquad();
                    if ( entity == null )
                        continue;
                    Faction faction = entity.Planet.GetControllingOrInfluencingFaction();
                    if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    {
                        if ( !anyPrinted )
                            tooltipBuffer.Add("Ships visible to you:\n");
                        anyPrinted = true;
                        tooltipBuffer.Add("\t").AddShipIconInline(entity.TypeData,World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull()).Add(" ");
                        tooltipBuffer.Add(entity.TypeData.GetDisplayName()).Add(" on ").Add(entity.Planet.Name, faction.FactionCenterColor.ColorHexBrighter).Add("\n");
                    }
                }
                if (!anyPrinted )
                    tooltipBuffer.Add("\n").Add("No visible ships").Add("\n");

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in necromancer wave notification mouseover handler, code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
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
                Image.UpdateWith( sprite_Wave, true, "TemplarWave" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "Templar\n" );
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 20;
                if ( Data.EntityList.Count == 0 )
                    return true;
                string color = "3333ff";
                if ( Data.Int64List.Count > 0 &&
                     Data.Int64List[0] > 0 )
                    color = "ff3333";
                buffer = SubTexts[1].Text.StartWritingToBuffer();
                buffer.Add(Data.EntityList.Count.ToString(), color);
                debugStage = 30;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicTemplarWaveNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
    public class PublicTemplarConstructorNotifier : NotifierBaseDataSingleton
    {
        public static PublicTemplarConstructorNotifier Instance = new PublicTemplarConstructorNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Constructor;
        private static bool hasInitialized = false;
        private static int index = 0;
        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            index = 0;
            hasInitialized = true;
            sprite_Constructor = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/ecology.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            if ( index >= Data.EntityList.Count )
                index = 0; //since index is a static it could be a stale value from before, so check here if the index is reasonable
            GameEntity_Squad entity = Data.EntityList[index].GetSquad();
            if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( entity.Planet, false );
            else
                World_AIW2.Instance.SwitchViewToPlanet( entity.Planet );

            index++;
            return MouseHandlingResult.None;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {

            int debugCode = 0;
            try
            {
                debugCode = 100;
                tooltipBuffer.Add("The Templar have ").Add( Data.EntityList.Count, "a1ffa1" ).Add(" constructors to build new defenses. Killing these can be very valuable, both to weaken the Templar and to get resources. Currently visible to you:\n");
                for (int i = 0; i < Data.EntityList.Count; i++ )
                {
                    GameEntity_Squad entity = Data.EntityList[i].GetSquad();
                    if ( entity == null )
                        continue;

                    if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    {
                        Faction faction = entity.Planet.GetControllingOrInfluencingFaction();
                        tooltipBuffer.Add("\t").Add(entity.TypeData.GetDisplayName()).Add(" on ").Add(entity.Planet.Name, faction.FactionCenterColor.ColorHexBrighter).Add("\n");
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in necromancer wave notification mouseover handler, code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
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
                Image.UpdateWith( sprite_Constructor, true, "TemplarConstructor" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "Templar\n" );
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 20;
                if ( Data.EntityList.Count == 0 )
                    return true;
                buffer = SubTexts[1].Text.StartWritingToBuffer();
                buffer.Add( Data.EntityList.Count, "ffa1a1" );
                debugStage = 30;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicTemplarWaveNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
    
}
