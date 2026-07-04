using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class PublicSpireSidekickCityUpgradeNotifier : NotifierBaseDataSingleton
    {
        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_CityUpgrade;
        private static bool hasInitialized = false;

        public static PublicSpireSidekickCityUpgradeNotifier Instance = new PublicSpireSidekickCityUpgradeNotifier();

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_CityUpgrade = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/spirerelic3.png" );
        }

        private void UpgradeCity( GameEntity_Squad entity )
        {
            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.UpgradeSpireSidekickCity], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
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

            int nonSpirePlanetsOwnedByPlayers = SpireSidekickFactionBaseInfo.GetNonSpirePlanetsOwnedByPlayers();
            if ( !SpireSidekickFactionBaseInfo.CalculateCanCityUpgrade( nonSpirePlanetsOwnedByPlayers, entity ) )
            {
                SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                if ( chatHandlerOrNull != null )
                    chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( entity );

                World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot upgrade city " + entity.GetFleetName_Safe() +
                    " at the moment.", ChatType.ShowLocallyOnly, "CannotDoThatThing", chatHandlerOrNull );
                return MouseHandlingResult.PlayClickDeniedSound;
            }
            ModalPopupData.CreateAndLogYesNoStyle( delegate { UpgradeCity( entity  ); }, null, "Upgrade City", "Are you sure you want to upgrade the city " + entity.GetFleetName_Safe() + " on " + entity.Planet.Name +"?", "Upgrade City", "Do Not Upgrade" );



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
                SpireSidekickFactionBaseInfo.WriteCityTooltipDetails( tooltipBuffer, entity );
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
                buffer.Add( "升级\n" );
                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                if ( entity == null )
                {
                    buffer.Add( "错误。NULL" );
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
    public class PublicRelicTrainSidekickNotifier : NotifierBaseDataSingleton
    {
        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_RelicTrainNotifier;
        private static bool hasInitialized = false;

        public static PublicRelicTrainSidekickNotifier Instance = new PublicRelicTrainSidekickNotifier();

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_RelicTrainNotifier = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/spirerelictrain.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            GameEntity_Squad entity = Data.Entity.GetSquad();
            if ( entity == null )
                return MouseHandlingResult.PlayClickDeniedSound;
            Planet planet = entity.Planet;
            if ( planet == null )
                return MouseHandlingResult.PlayClickDeniedSound;

            if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
            {
                if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                    Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( planet, false );
                else
                    World_AIW2.Instance.SwitchViewToPlanet( planet );
                return MouseHandlingResult.None;
            }

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

            SpireSidekickPerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<SpireSidekickPerUnitBaseInfo>();
            if ( data == null )
                return false;
            //                Planet destinationPlanet = World_AIW2.Instance.GetPlanetByIndex(data.TargetDepotPlanetID);
            Planet destinationPlanet = null;

            string destString = "";
            EntityOrderCollection orders = entity.Orders;

            tooltipBuffer.Add( "这辆 AI 遗物运输车 ", colorString );


            string locationString = "位于未知星球";
            if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
            {
                locationString = "位于 " + entity.GetPlanetName_Safe();

                //galaxy map hover
                World_AIW2.Instance.FocusedPlanetForMapDarkening = planet;
            }
            else
                Planet.SetCurrentlyAllUnexploredPlanetsHoveredOver(); //for when we don't know where it is

            tooltipBuffer.Add( locationString ).Add( "\n" );
            if ( orders != null )
            {
                destinationPlanet = orders.GetFinalDestinationOrNull();
                if ( destinationPlanet != null )
                {
                    if ( destinationPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                        destString = "它正前往 <color=#ffa1a1>" + destinationPlanet.Name + "</color>。";
                    else
                        destString = "它正前往 <color=#ffa1a1>一颗未探索的星球</color>。";
                }
            }
            if ( orders == null || destinationPlanet == null )
                destString = "它正前往当前星球的金属发电机进行补给。";
            tooltipBuffer.Add( destString ).Add( "\n" );
            string hopsLeft = "";
            //string nextDest = "";
            if ( data != null )
            {
                if ( data.HopsLeftForTrain == 0 )
                    hopsLeft = "\t它正前往最终目的地";
                else if ( data.HopsLeftForTrain > 0 )
                {
                    hopsLeft = "\t它还有 <color=#a1ffa1>" + data.HopsLeftForTrain + "</color>";
                    if ( data.HopsLeftForTrain == 1 )
                        hopsLeft += " 颗星球 ";
                    else
                        hopsLeft += " 颗星球 ";
                    hopsLeft += "需要访问。";
                }
            }
            tooltipBuffer.Add( hopsLeft );

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
                debugStage = 1;
                Image.UpdateWith( sprite_RelicTrainNotifier, true, "RelicTr" );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "遗物\n" );
                SubTexts[0].Text.FinishWritingToBuffer();

                debugStage = 6;
                buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 9;

                //Nanocaust frenzy strength (if we want? remove the ???s above first)
                // FInt percent = (data.CurrentExoStrength * 100) / data.StrengthRequiredForNextExo ;

                buffer.Add( "列车\n" );
                if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    buffer.Add( entity.GetPlanetName_Safe() );
                else
                    buffer.Add( "运输中" );

                buffer.Add( "\n" );
                debugStage = 12;
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicRelicTrainNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
    }
}
