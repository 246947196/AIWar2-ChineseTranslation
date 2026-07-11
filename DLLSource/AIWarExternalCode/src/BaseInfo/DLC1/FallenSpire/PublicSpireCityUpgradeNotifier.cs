using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class PublicSpireCityUpgradeNotifier : NotifierBaseDataSingleton
    {
        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_CityUpgrade;
        private static bool hasInitialized = false;

        public static PublicSpireCityUpgradeNotifier Instance = new PublicSpireCityUpgradeNotifier();

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_CityUpgrade = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/spirerelic3.png" );
        }

        private void UpgradeCity( GameEntity_Squad entity )
        {
            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.UpgradeSpireCity], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
            command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
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

            int nonSpirePlanetsOwnedByPlayers = FallenSpireFactionBaseInfo.GetNonSpirePlanetsOwnedByPlayers();
            if ( !FallenSpireFactionBaseInfo.CalculateCanCityUpgrade( nonSpirePlanetsOwnedByPlayers, entity ) )
            {
                SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                if ( chatHandlerOrNull != null )
                    chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( entity );

                World_AIW2.Instance.QueueChatMessageOrCommand( "目前无法升级城市 " + entity.GetFleetName_Safe() +
                    '。', ChatType.ShowLocallyOnly, "CannotDoThatThing", chatHandlerOrNull );
                return MouseHandlingResult.PlayClickDeniedSound;
            }
            ModalPopupData.CreateAndLogYesNoStyle( delegate { UpgradeCity( entity  ); }, null, "升级城市", "你确定要升级城市 " + entity.GetFleetName_Safe() + " 在 " + entity.Planet.Name + " 吗？", "升级城市", "不升级" );



            return MouseHandlingResult.DoNotPlayClickSound;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            GameEntity_Squad entity = Data.Entity.GetSquad();
            tooltipBuffer.Clear();
            if ( entity == null )
            {
                tooltipBuffer.Add( "BUG: no city\n" );
            }
            else
            {
                World_AIW2.Instance.FocusedPlanetForMapDarkening = entity.Planet;
                FallenSpireFactionBaseInfo.WriteCityTooltipDetails( tooltipBuffer, entity );
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
                GameEntity_Squad entity = Data.Entity.GetSquad();
                if ( entity == null )
                    return false;
                Planet planet = entity.Planet;
                if ( planet == null )
                    return false;

                debugStage = 0;
                InitIfNeeded();

                debugStage = 10;
                Image.UpdateWith( sprite_CityUpgrade, true, "SpireCityUpgrade" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "Upgrade\n" );
                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                if ( entity == null )
                {
                    buffer.Add( "Bug. NULL" );
                    return true;
                }
                debugStage = 20;

                Fleet cityFleet = entity.GetFleetOrNull_Safe();
                if ( cityFleet == null )
                    buffer.Add( 2 );
                else
                    buffer.Add( (cityFleet.AddedMarkLevelsForFleet_FromScience + 2) );
                buffer.Add( "\n" );
                string fleetName = entity.GetFleetName_Safe();
                if ( fleetName.Length > 10 )
                    buffer.Add( entity.Planet.Name );
                else
                    buffer.Add( entity.GetFleetName_Safe() );
                debugStage = 30;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicSpireCityUpgradeNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
}
