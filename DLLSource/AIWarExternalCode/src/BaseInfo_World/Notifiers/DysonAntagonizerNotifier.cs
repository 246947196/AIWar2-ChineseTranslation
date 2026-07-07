using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DysonAntagonizerNotifier : NotifierBaseDataSingleton
    {
        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_DysonAntagonizer;
        private static bool hasInitialized = false;

        public static DysonAntagonizerNotifier Instance = new DysonAntagonizerNotifier();

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_DysonAntagonizer = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/dysonsphere.png" );
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
            GameEntity_Squad entity = Data.Entity.GetSquad();
            if ( entity == null )
                return false;
            Planet planet = Data.Planet;
            if ( planet == null )
                return false;

            World_AIW2.Instance.FocusedPlanetForMapDarkening = planet;

            string colorString = string.Empty;
            colorString = Data.Faction.FactionCenterColor.ColorHexBrighter;

            tooltipBuffer.Clear();
            if ( entity.TypeData.GetHasTag( "WarpingInDysonAntagonizer" ) )
            {
                if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    tooltipBuffer.Add( "一个戴森球挑衅者", colorString ).Add( " 正在 " + planet.Name + " 折跃进入。" + entity.SecondsTillTransformation + " 秒后戴森球挑衅者将被激活。\n" );
                else
                    tooltipBuffer.Add( "一个戴森球挑衅者", colorString ).Add( " 正在银河系某处折跃进入。" + entity.SecondsTillTransformation + " 秒后戴森球挑衅者将被激活。\n" );
            }
            else
            {
                if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    tooltipBuffer.Add( "一个戴森球挑衅者", colorString ).Add( " 位于 " + planet.Name + "。" );
                else
                    tooltipBuffer.Add( "一个戴森球挑衅者", colorString ).Add( " 位于银河系某处。" );
            }
            tooltipBuffer.Add( "只要存在任何活跃的戴森球挑衅者，所有戴森球都将集中力量攻击你" );
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

                GameEntity_Squad entity = Data.Entity.GetSquad();
                if ( entity == null )
                    return false;
                Planet planet = Data.Planet;
                if ( planet == null )
                    return false;

                debugStage = 0;
                debugStage = 1;
                Image.UpdateWith( sprite_DysonAntagonizer, true, "Dyson" );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "戴森\n" );
                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                debugStage = 6;
                if ( entity.SecondsTillTransformation > 0 )
                    buffer.Add( "\n" ).Add( entity.SecondsTillTransformation );
                debugStage = 9;
                if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    buffer.Add( "\n" ).Add( planet.Name );
                else
                    buffer.Add( "\n" ).Add( "未知" );
                debugStage = 12;
                buffer.Add( "\n" );
                debugStage = 13;
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in DysonAntagonizerNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
    }
}
