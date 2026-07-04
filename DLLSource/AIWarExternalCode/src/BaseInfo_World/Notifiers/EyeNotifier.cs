using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class EyeNotifier : NotifierBaseDataSingleton
    {
        public static EyeNotifier Instance = new EyeNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_AlertedEye;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_AlertedEye = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/aieye.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            var entity = Data.Entity.GetSquad();
            if ( entity == null )
                return MouseHandlingResult.PlayClickDeniedSound;
            var planet = Data.Planet;
            if ( planet == null )
                return MouseHandlingResult.PlayClickDeniedSound;
           
            ObjectiveGenerator.CenteringHelper(planet, entity);

            return MouseHandlingResult.DoNotPlayClickSound;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            Planet planet = Data.Planet;
            if ( planet == null )
                return false;

            //galaxy map hover
            World_AIW2.Instance.FocusedPlanetForMapDarkening = planet;

            string colorString = string.Empty;
            colorString = Data.Faction.FactionCenterColor.ColorHexBrighter;

            tooltipBuffer.Clear();
            tooltipBuffer.Add( "一个警戒之眼", colorString ).Add( " 位于 " + planet.Name + "。这是一个极其强大的武器，只有当AI敌人的力量超过AI在此星球上的部队时才会激活。" );
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
                InitIfNeeded();

                Planet planet = Data.Planet;
                if ( planet == null )
                    return false;

                debugStage = 0;
                debugStage = 1;
                Image.UpdateWith( sprite_AlertedEye, true, "Human_Fin" );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                buffer.Add( "激活\n" );
                buffer.Add( "之眼\n\n" );

                debugStage = 6;

                debugStage = 9;

                buffer.Add( planet.Name );
                debugStage = 12;
                buffer.Add( "\n" );
                debugStage = 13;
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in EyeNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
    }
}
