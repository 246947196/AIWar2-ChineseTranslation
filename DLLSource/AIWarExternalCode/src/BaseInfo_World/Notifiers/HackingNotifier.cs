using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class HackingNotifier : NotifierBaseDataSingleton
    {
        public static HackingNotifier Instance = new HackingNotifier();

        //these are just data from asset bundles, that's definitely okay to be static   
        private static UnityEngine.Sprite sprite_HackingNotifier;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_HackingNotifier = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/hackingnotifier.png" );
        }

        void onCancel( LazyLoadSquadWrapper squad )
        {
            GameEntity_Squad entity = squad.GetSquad();
            if ( entity == null )
                return;
            HackingType hackType = entity.ActiveHack;
            if ( hackType == null )
                return;
            HackingEvent hackEvent = entity.ActiveHackEvent;
            if ( hackEvent == null )
                return;

            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.CancelHack], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
            command.RelatedIntegers.Add( entity.PrimaryKeyID ); //squad id
            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            GameEntity_Squad entity = Data.Entity.GetSquad();
            if ( entity == null )
                return MouseHandlingResult.PlayClickDeniedSound;
            Planet planet = entity.Planet;
            if ( planet == null )
                return MouseHandlingResult.PlayClickDeniedSound;
            HackingType hackType = entity.ActiveHack;
            if ( hackType == null )
                return MouseHandlingResult.PlayClickDeniedSound;

            if ( entity != null )
            {
                World_AIW2.Instance.SwitchViewToPlanet( planet );
                if ( !hackType.CannotBeCanceled )
                {
                    LazyLoadSquadWrapper hacker = LazyLoadSquadWrapper.Create( entity );
                    ModalPopupData.CreateAndLogYesNoStyle( delegate { onCancel( hacker ); }, null, "取消入侵", "你确定要取消当前的入侵吗？取消入侵仍需消耗全部入侵点数。", "取消入侵", "不，继续入侵" );

                    World.Instance.IsPaused = true;
                }
                return MouseHandlingResult.None;
            }
            return MouseHandlingResult.DoNotPlayClickSound;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            int debugStage = 0;
            try
            {
                debugStage = 10;
                GameEntity_Squad entity = Data.Entity.GetSquad();
                if ( entity == null )
                    return false;
                debugStage = 20;
                Planet planet = entity.Planet;
                if ( planet == null )
                    return false;
                debugStage = 30;
                HackingType hackType = entity.ActiveHack;
                if ( hackType == null )
                    return false;
                debugStage = 40;
                HackingEvent hackEvent = entity.ActiveHackEvent;
                if ( hackEvent == null )
                    return false;
                debugStage = 50;

                tooltipBuffer.Clear();
                {
                    debugStage = 100;
                    string colorString = string.Empty;
                    Faction fullFaction = entity.GetFactionOrNull_Safe();
                    debugStage = 200;
                    if ( fullFaction != null )
                        colorString = fullFaction.FactionCenterColor.ColorHexBrighter;

                    debugStage = 300;
                    //galaxy map hover
                    World_AIW2.Instance.FocusedPlanetForMapDarkening = planet;

                    debugStage = 500;
                    tooltipBuffer.Add( "正在进行 " + hackType.DisplayName, colorString );
                    if ( hackType.RelatedStringIsAShipType && hackEvent.RelatedStringOrNull != null && hackEvent.RelatedStringOrNull.Length > 0 )
                    {
                        debugStage = 600;
                        GameEntityTypeData relatedTypeData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( hackEvent.RelatedStringOrNull );
                        debugStage = 700;
                        tooltipBuffer.Add( " (" ).Add( relatedTypeData == null ? "null" : relatedTypeData.DisplayName ).Add( ")" );
                    }
                    debugStage = 800;
                    tooltipBuffer.Add( " 对 " ).Add( entity.GetPlanetName_Safe() ).Add( " 的入侵。此入侵已持续 " ).AddHoursAndMinutes( entity.ActiveHack_DurationThusFar ).Add( "。\n\n入侵期间，入侵旗舰不得离开此星球。\n点击此通知可以取消入侵。\n\n" );

                    debugStage = 900;
                    PlanetFaction faction;
                    tooltipBuffer.Add( "<b>敌方部队</b>\n" );
                    for ( int i = 0; i < planet.Factions.Count; i++ )
                    {
                        debugStage = 1000;
                        faction = planet.Factions[i];
                        debugStage = 1100;
                        if ( faction.Faction.Type == FactionType.NaturalObject || faction.GetIsFriendlyToLocalFaction() )
                            continue;
                        debugStage = 1200;
                        ArcenExternalUIUtilities.WriteSideContents( faction, tooltipBuffer, true );
                    }
                    debugStage = 2000;
                    tooltipBuffer.Add( "\n<b>友方部队</b>\n" );
                    for ( int i = 0; i < planet.Factions.Count; i++ )
                    {
                        debugStage = 2100;
                        faction = planet.Factions[i];
                        debugStage = 2200;
                        if ( faction.Faction.Type == FactionType.NaturalObject || !faction.GetIsFriendlyToLocalFaction() )
                            continue;
                        debugStage = 2300;
                        ArcenExternalUIUtilities.WriteSideContents( faction, tooltipBuffer, !faction.GetIsLocalFaction() );
                    }
                }
                debugStage = 3000;
                ArcenExternalUIUtilities.ShowTooltipWide( tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in hacking notifier mouseover at debugStage " + debugStage + ": " + e, Verbosity.ShowAsError );

                return false;
            }
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
                debugStage = 0;
                GameEntity_Squad entity = Data.Entity.GetSquad();
                if ( entity == null )
                    return false;
                Planet planet = entity.Planet;
                if ( planet == null )
                    return false;
                HackingType hackType = entity.ActiveHack;
                if ( hackType == null )
                    return false;
                HackingEvent hackEvent = entity.ActiveHackEvent;
                if ( hackEvent == null )
                    return false;

                debugStage = 1;
                Image.UpdateWith( sprite_HackingNotifier, true, "Hack" );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "入侵" );
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 4;

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                debugStage = 5;
                int secondsActive = entity.ActiveHack_DurationThusFar;
                int totalSeconds = hackType.GetEffectiveHackDuration( World_AIW2.Instance.GetEntityByID_Squad( entity.ActiveHack_Target ),
                    World_AIW2.Instance.CurrentGalaxy.GetPlanetByIndex( entity.ActiveHack_Planet ) );
                int secondsRemaining = totalSeconds - secondsActive;
                debugStage = 6;
                debugStage = 9;

                if ( hackType.GetIsPerSecondStyleCost() )
                {
                    debugStage = 99;
                    buffer.Add( secondsActive );
                }
                else
                {
                    if ( secondsRemaining > 30 )
                        buffer.Add( "<color=#ffd632>" ); //yellow
                    else
                        buffer.Add( "<color=#ff4f32>" ); //red

                    debugStage = 2;
                    debugStage = 5;
                    buffer.Add( secondsRemaining );
                    debugStage = 10;
                    buffer.Add( "</color>\n" );
                    debugStage = 12;
                }
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in HackingNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
    }
}
