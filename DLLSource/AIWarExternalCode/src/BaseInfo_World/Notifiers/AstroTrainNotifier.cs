using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class AstroTrainNotifier : NotifierBaseDataSingleton
    {
        public static AstroTrainNotifier Instance = new AstroTrainNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_AstroTrainNotifier;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_AstroTrainNotifier = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/astrotrainnotifier.png" );
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
            PlanetFaction pFaction = entity.PlanetFaction;
            if ( pFaction == null )
                return false;
            Faction faction = pFaction.Faction;
            if ( faction == null )
                return false;

            tooltipBuffer.Clear();
            if ( entity == null )
                return true;//I've seen a null reference in here; perhaps a race between the UI and the entity being removed?
            int debugCode = 0;
            try
            {
                debugCode = 100;
                AstroTrainsPerTrainBaseInfo data = entity.GetExternalBaseInfoAs<AstroTrainsPerTrainBaseInfo>();
                Planet finalDestinationPlanet = data == null ? null : World_AIW2.Instance.GetPlanetByIndex( data.TargetDepotPlanetID );
                debugCode = 110;
                string destString = "has a final destination on an unexplored planet";
                if ( finalDestinationPlanet != null && finalDestinationPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                {
                    destString = "has as a final destination the Depot on " + finalDestinationPlanet.Name;
                    World_AIW2.Instance.FocusedPlanetForMapDarkening = finalDestinationPlanet;
                }
                debugCode = 120;
                string locationString = "on unknown planet";
                if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                {
                    debugCode = 130;
                    locationString = "on " + entity.GetPlanetName_Safe();

                    //galaxy map hover
                    World_AIW2.Instance.AlsoFocusedPlanetsForMapDarkening[entity.Planet] = ArcenTime.TimeSinceStartF; //multi hover
                }
                debugCode = 140;

                string colorString = string.Empty;
                colorString = entity.GetFactionCenterColorHexBrighter_Safe();

                debugCode = 150;
                debugCode = 160;
                Planet nextDest = entity.GetDestinationPlanet();

                tooltipBuffer.Add( "An Astro Train on ").Add( locationString, entity.GetFactionCenterColorHexBrighter_Safe()).Add(" ").Add( destString ).Add(".");
                if ( nextDest != null && nextDest.IntelLevel > PlanetIntelLevel.Unexplored && nextDest != entity.Planet )
                    tooltipBuffer.Add("\nThis train's stop is the Station on ").Add(nextDest.Name, "ffa1a1").Add(".");

                Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in Mouseover for Astro Trains. debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
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
                GameEntity_Squad entity = Data.Entity.GetSquad();
                if ( entity == null )
                    return false;
                PlanetFaction pFaction = entity.PlanetFaction;
                if ( pFaction == null )
                    return false;
                Faction faction = pFaction.Faction;
                if ( faction == null )
                    return false;

                debugStage = 0;
                debugStage = 1;
                Image.UpdateWith( sprite_AstroTrainNotifier, true, "AT" );

                debugStage = 3;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "Astro\n" );
                SubTexts[0].Text.FinishWritingToBuffer();

                debugStage = 6;
                buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 9;

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
                ArcenDebugging.ArcenDebugLog( "Exception in AstroTrainNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
    }
}
