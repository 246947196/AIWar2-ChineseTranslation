using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class InstigatorNotifier : NotifierBaseDataSingleton
    {
        public static InstigatorNotifier Instance = new InstigatorNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Instigator;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_Instigator = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/instigatorclock.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            var entity = Data.Entity.GetSquad();
            if ( entity == null )
                return MouseHandlingResult.PlayClickDeniedSound;
            
            var planet = entity.Planet;
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
            Planet planet = entity.Planet;
            if ( planet == null )
                return false;


            tooltipBuffer.Clear();
            string colorString = string.Empty;
            colorString = entity.GetFactionCenterColorHexBrighter_Safe();

            if ( planet.IntelLevel > PlanetIntelLevel.Unexplored ) //instigator bases are always "visible" if you've explored the planet
            {
                InstigatorPerUnitBaseInfo localPerUnitData = entity.TryGetExternalBaseInfoAs<InstigatorPerUnitBaseInfo>();
                World_AIW2.Instance.FocusedPlanetForMapDarkening = planet;
                tooltipBuffer.Add( "在 " ).Add( "煽动者基地", colorString ).Add( " 位于 " + entity.GetPlanetName_Safe() + "。你需要尽快摧毁它。\n" );
                if ( localPerUnitData != null )
                {
                    if ( localPerUnitData.InstigatorEffectIndex != -1 )
                    {
                        InstigatorEffectData rowdata = InstigatorDataTable.Instance.GetRowById( localPerUnitData.InstigatorEffectIndex );
                        Faction entityFacOrNull = entity.GetFactionOrNull_Safe();
                        if ( entityFacOrNull != null )
                        {
                            Faction faction = null;
                            InstigatorFactionBaseInfo factionData = entityFacOrNull.GetExternalBaseInfoAs<InstigatorFactionBaseInfo>();
                            if ( factionData != null && factionData.AIFactionIndexForNextSpawn != -1 )
                                faction = World_AIW2.Instance.GetFactionByIndex( factionData.AIFactionIndexForNextSpawn );
                            tooltipBuffer.Add( rowdata.GetHoverText( faction ) );
                        }
                    }

                    if ( localPerUnitData.NumTimesEffectHappened > 0 )
                        tooltipBuffer.Add( "\n此基地的能力已触发 <color=#cfd988>" + localPerUnitData.NumTimesEffectHappened + "</color> 次。" );
                    if ( GameSettings.Current.GetBoolBySetting("Debug_Tooltip") )
                        tooltipBuffer.Add("Time for next effect: " + localPerUnitData.TimeForNextEffect ).Add("\n");
                }
            }
            else
            {
                //hover all the unexplored ones
                Planet.SetCurrentlyAllUnexploredPlanetsHoveredOver();

                tooltipBuffer.Add( "在银河系某处有一个 " ).Add( "煽动者基地", colorString ).Add( "。你需要侦察它的位置以便摧毁它" );
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
                InitIfNeeded();

                GameEntity_Squad entity = Data.Entity.GetSquad();
                if ( entity == null )
                    return false;
                Planet planet = entity.Planet;
                if ( planet == null )
                    return false;

                debugStage = 0;
                InitIfNeeded();
                debugStage = 1;
                Image.UpdateWith( sprite_Instigator, true, "Human_Fin" );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "煽动者\n\n" );
                SubTexts[0].Text.FinishWritingToBuffer();

                debugStage = 6;
                buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 9;

                InstigatorPerUnitBaseInfo localData = entity.TryGetExternalBaseInfoAs<InstigatorPerUnitBaseInfo>();
                if ( planet.IntelLevel > PlanetIntelLevel.Unexplored )
                    buffer.Add( planet.Name );
                else
                    buffer.Add( "???" );
                debugStage = 12;
                buffer.Add( "\n" );
                debugStage = 13;
                if ( localData != null )
                {
                    if ( localData.TimeForNextEffect < World_AIW2.Instance.GameSecond )
                        buffer.Add( "?s" );
                    else
                        buffer.AddSecondsRemaining( localData.TimeForNextEffect - World_AIW2.Instance.GameSecond );
                }
                else
                    buffer.Add( "?s" );
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in InstigatorNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
    }
}
