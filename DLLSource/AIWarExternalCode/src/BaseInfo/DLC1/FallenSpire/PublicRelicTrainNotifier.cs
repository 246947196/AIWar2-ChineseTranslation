using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class PublicRelicTrainNotifier : NotifierBaseDataSingleton
    {
        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_RelicTrainNotifier;
        private static bool hasInitialized = false;

        public static PublicRelicTrainNotifier Instance = new PublicRelicTrainNotifier();

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

            FallenSpirePerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<FallenSpirePerUnitBaseInfo>();
            if ( data == null )
                return false;
            //                Planet destinationPlanet = World_AIW2.Instance.GetPlanetByIndex(data.TargetDepotPlanetID);
            Planet destinationPlanet = null;

            string destString = "";
            EntityOrderCollection orders = entity.Orders;

            tooltipBuffer.Add( "This AI Relic Transport ", colorString );


            string locationString = "is on an unknown planet";
            if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
            {
                locationString = "is on " + entity.GetPlanetName_Safe();

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
                        destString = "It is heading to <color=#ffa1a1>" + destinationPlanet.Name + "</color>.";
                    else
                        destString = "It is heading to <color=#ffa1a1>an unexplored planet</color>.";
                }
            }
            if ( orders == null || destinationPlanet == null )
                destString = "It is heading to a metal generator on its current planet to refuel.";
            tooltipBuffer.Add( destString ).Add( "\n" );
            string hopsLeft = "";
            //string nextDest = "";
            if ( data != null )
            {
                if ( data.HopsLeftForTrain == 0 )
                    hopsLeft = "\tIt is en route to its final destination";
                else if ( data.HopsLeftForTrain > 0 )
                {
                    hopsLeft = "\tIt has <color=#a1ffa1>" + data.HopsLeftForTrain + "</color>";
                    if ( data.HopsLeftForTrain == 1 )
                        hopsLeft += " planet ";
                    else
                        hopsLeft += " planets ";
                    hopsLeft += "left to visit.";
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
                buffer.Add( "Relic\n" );
                SubTexts[0].Text.FinishWritingToBuffer();

                debugStage = 6;
                buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 9;

                //Nanocaust frenzy strength (if we want? remove the ???s above first)
                // FInt percent = (data.CurrentExoStrength * 100) / data.StrengthRequiredForNextExo ;

                buffer.Add( "Train\n" );
                if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    buffer.Add( entity.GetPlanetName_Safe() );
                else
                    buffer.Add( "En Route" );

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
