using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class RaidEngineNotifier : NotifierBaseDataSingleton
    {
        public static RaidEngineNotifier Instance = new RaidEngineNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_AlertedRaidEngine;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_AlertedRaidEngine = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/raidengine.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            var e = Data.Entity.GetSquad();
            if ( e == null )
                return MouseHandlingResult.PlayClickDeniedSound;
            
            Planet planet = e.Planet;
            if ( planet == null )
                return MouseHandlingResult.PlayClickDeniedSound;

            ObjectiveGenerator.CenteringHelper(planet, e);

            return MouseHandlingResult.None;
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

            GameEntity_Squad entity = Data.Entity.GetSquad();
            if ( entity == null )
                return false;

            tooltipBuffer.Clear();

            var isPermanentlyAlerted = entity.HasStartedDoingPeriodicSpawns && entity.TypeData.PeriodicSpawn_NeverStopOnceTriggered;
            if (isPermanentlyAlerted)
                tooltipBuffer.Add( "一个永久警戒的 " );
            else
                tooltipBuffer.Add( "一个警戒的 " );

            tooltipBuffer.Add( entity.TypeData.DisplayName, Data.Faction.FactionCenterColor.ColorHexBrighter ).Add( " 位于 " );

            if ( entity.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
                tooltipBuffer.AddPlanetNameFormated( planet, true );
            else
                tooltipBuffer.Add( "一个未知星球" );

            if (isPermanentlyAlerted)
            {
                tooltipBuffer.Add(". ");
            }
            else
            {
                tooltipBuffer.Add( " 正在响应 " );

                if ( entity.TypeData.PeriodicSpawn_OnlyTriggerOnOccupation )
                    tooltipBuffer.Add( "敌方占领。 " );
                else
                {
                    var presenceOnPlanet = World_AIW2.Instance.GetPlanetByIndex( Data.planetIdx );
                    if (presenceOnPlanet == planet)
                        tooltipBuffer.Add( "该处的敌方存在。 " );
                    else
                    {
                        tooltipBuffer.Add( "位于 " ).AddPlanetNameFormated( presenceOnPlanet, true ).Add( " 的敌方存在。 " );
                    }
                }
            }

            if ( entity.TypeData.PeriodicallySpawnsUnits )
            {
                entity.TypeData.PeriodicSpawn_EntityTypeDrawingBag.Value.WriteToBuffer( tooltipBuffer, entity.TypeData, false );
                tooltipBuffer.Add( "</color> 属于其 " ).Add( Extensions.ToString(entity.TypeData.Periodic_SpawnFactionForUnit) ).Add( " 阵营，对抗 " );
            }
            else
            {
                tooltipBuffer.Add( "它将发动一次" );
                if ( entity.TypeData.PeriodicSpawn_CreatesExoStrike )
                {
                    tooltipBuffer.Add( "远征打击 " );
                    if ( entity.TypeData.PeriodicSpawn_CreatesWave )
                        tooltipBuffer.Add( "和" );
                }
                if ( entity.TypeData.PeriodicSpawn_CreatesWave )
                    tooltipBuffer.Add( " 波次 " );
                tooltipBuffer.Add( " 的 <color=#ffdf72>" ).Add( entity.TypeData.PeriodicSpawn_WaveOrExoSizeMultiplier.ReadableString ).Add( "x</color> 正常力量，攻击 " );
            }
            tooltipBuffer.StartColor( Data.targetFaction.FactionCenterColor.TeamColor );
            tooltipBuffer.Add( Data.targetFaction.GetDisplayName() );
            tooltipBuffer.EndColor();
            tooltipBuffer.Add( " 阵营中的 " ).Add( entity.PeriodicSpawn_TimeUntilNextSpawn, "fdfdfd" ).Add( " 秒。" );
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
                Image.UpdateWith( sprite_AlertedRaidEngine, true, "Human_Fin" );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                if ( entity.TypeData.PeriodicallySpawnsEvent )
                {
                    buffer.Add( "引擎" );
                }
                else
                {
                    buffer.Add( "巢穴" );
                }

                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 6;

                if ( entity.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
                    buffer.Add( planet.Name );
                else
                    buffer.Add( "未知" );
                debugStage = 12;
                buffer.Add( "\n" );
                buffer.Add( entity.PeriodicSpawn_TimeUntilNextSpawn );
                debugStage = 13;
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in RaidEngineNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
    }
}
