using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class MacrophageNotifier : NotifierBaseDataSingleton
    {
        public static MacrophageNotifier Instance = new MacrophageNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Macrophage;
        private static bool hasInitialized = false;
        private static int index = 0;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_Macrophage = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/macrophage.png" );
        }

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
            //nothing to do on the galaxy map hover
            //World_AIW2.Instance.FocusedPlanetForMapDarkening = planet;

            string colorString = string.Empty;
            if ( Data.Faction != null )
                colorString = Data.Faction.FactionCenterColor.ColorHexBrighter;

            tooltipBuffer.Clear();
            tooltipBuffer.Add( "有 " ).Add( Data.numEntitiesAttacking ).Add( " 只愤怒的巨噬细胞", colorString ).Add( " 在你的母星附近。" );
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
                debugStage = 1;
                Image.UpdateWith( sprite_Macrophage, true, "Mac" );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "巨噬细胞\n\n" );
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 6;
                buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 9;
                buffer.Add( Data.numEntitiesAttacking );

                debugStage = 2;
                debugStage = 5;

                // FInt percent = (data.CurrentExoStrength * 100) / data.StrengthRequiredForNextExo ;

                // buffer.Add( "<color=#" ).Add( colorString ).Add( ">" );
                // buffer.Add(percent.IntValue).Add("%");
                // buffer.Add( "</color>\n" );
                debugStage = 12;
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in MacrophageNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
    }
}
