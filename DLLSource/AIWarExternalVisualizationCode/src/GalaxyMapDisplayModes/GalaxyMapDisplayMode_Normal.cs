using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using Arcen.AIW2.External;
using UnityEngine;

namespace Arcen.AIW2.ExternalVisualization
{
    public class GalaxyMapDisplayMode_Normal : BaseGalaxyMapDisplayMode
    {
        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            GalaxyMapDisplayMode_Normal.ShowNormalIcons( planet, EntityToSkip, ArrayToFill, OnlyShowThingsThatShouldBeInFarZoom );
        }
        
        //State for the static cb_ShowNormalIconsEvaluator delegate below. [ThreadStatic] because
        //the galaxy-map visuals pass that calls ShowNormalIcons runs on a background thread; this
        //carries the per-call OnlyShowThingsThatShouldBeInFarZoom flag without a captured closure.
        [ThreadStatic]
        private static bool cb_onlyShowThingsThatShouldBeInFarZoom;

        //Allocated once at type init rather than a fresh display-class per ShowNormalIcons call.
        //ShowNormalIcons is invoked once per planet per frame while the galaxy map is open (88/frame
        //in profiling), and the old inline delegate captured OnlyShowThingsThatShouldBeInFarZoom,
        //allocating a closure (~12.5KB/frame) each time.
        private static readonly GameEntity_Squad.EvaluatorDelegate cb_ShowNormalIconsEvaluator = ShowNormalIconsEvaluator;

        private static bool ShowNormalIconsEvaluator( GameEntity_Squad entity )
        {
            if ( cb_onlyShowThingsThatShouldBeInFarZoom )
            {
                if ( !entity.GetIsSelected() )
                    return false; //if in far zoom, only show selected entities
            }
            if ( entity.TypeData.DrawInGalaxyView || entity.TypeData.GetHasTag( "ShowsOnNormalDisplayMode" ) || entity.TypeData.GetHasTag( "ProgressReducer" ) || entity.TypeData.GetHasTag( "Capturable" ) )
            {
                //this unit is eligible to be shown
                if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    return true;
            }
            return false;
        }

        public static void ShowNormalIcons( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            //can't skip mobile units
            if ( EntityToSkip != null && EntityToSkip.TypeData.IsMobile )
                EntityToSkip = null;

            int currentIndex = 0;
            cb_onlyShowThingsThatShouldBeInFarZoom = OnlyShowThingsThatShouldBeInFarZoom;
            UtilityMethodsFor_GalaxyMapDisplayMode.BasePickGalaxyMapOtherThingsToShow( ref currentIndex, planet, EntityToSkip, ArrayToFill,
             cb_ShowNormalIconsEvaluator );
        }

        public override void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;
            
            bool isMinimalFogOfWar = AIWar2GalaxySettingQuickAccess.GalaxyMinimalFogOfWar;

            int threatStrengthInt, hostileStrengthMinusThreatInt, myTotalStrength, myMobileStrength, myAndAlliedTotalStrength, myAndAlliedMobileStrength;
            Faction hostileFaction;
            Faction myOrAlliedFaction;
            Faction alliedFaction;
            Window_InGameHoverPlanetInfo.GetPlanetFactionalData( planet, out threatStrengthInt, out hostileStrengthMinusThreatInt, out myTotalStrength, out myMobileStrength,
                out myAndAlliedTotalStrength, out myAndAlliedMobileStrength, out myOrAlliedFaction, out alliedFaction, out hostileFaction, isMinimalFogOfWar );

            //LEFT
            // Your immobile strength
            // Your mobile strength
            if ( myAndAlliedTotalStrength > 0 )
            {
                string textColor = "ffffff";
                if ( (myMobileStrength > 0 || 
                     myAndAlliedMobileStrength == myMobileStrength) &&
                     myOrAlliedFaction != null )
                {
                    textColor = myOrAlliedFaction.FactionCenterColor.GetColorHexBrighter( false );
                }
                else
                if (alliedFaction != null)
                {
                    textColor = alliedFaction.FactionCenterColor.GetColorHexBrighter( false );
                }
                
                if (string.IsNullOrWhiteSpace(textColor))
                    textColor = "ffffff";

                LeftBuffer.StartColor( textColor );
                int myAndAlliedImmobileStrength = Math.Max( myAndAlliedTotalStrength - myAndAlliedMobileStrength, 0 );
                ArcenExternalUIUtilities.ByPlanet_WriteStrengthIconWithColor( LeftBuffer, textColor );
                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( LeftBuffer, myAndAlliedMobileStrength, true, true );
                LeftBuffer.Add( "\n" );
                ArcenExternalUIUtilities.ByPlanet_WriteStrengthIconWithColor( LeftBuffer, textColor );
                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( LeftBuffer, myAndAlliedImmobileStrength, true, true );
            }

            //RIGHT LINE 1
            // Enemy Guard Strength
            if ( hostileStrengthMinusThreatInt > 0 )
            {
                string textColor = "ffffff";
                if ( hostileFaction != null )
                    textColor = hostileFaction.FactionCenterColor.GetColorHexBrighter( false );
               
                if (string.IsNullOrWhiteSpace(textColor))
                    textColor = "ffffff";
                
                RightBuffer.StartColor( textColor );
                ArcenExternalUIUtilities.ByPlanet_WriteStrengthIconWithColor( RightBuffer, textColor );
                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( RightBuffer, hostileStrengthMinusThreatInt, true, true );
                if ( !isMinimalFogOfWar && planet.IntelLevel < PlanetIntelLevel.CurrentlyWatched )
                    RightBuffer.Add( "<size=12>?</size>" );

                if ( threatStrengthInt > 0 )
                    RightBuffer.Add( "</color>\n" );
            }

            //RIGHT LINE 2
            // Enemy Threat Strength
            if ( threatStrengthInt > 0 )
            {
                string textColor = "ffffff";
                if ( hostileFaction != null )
                    textColor = hostileFaction.FactionCenterColor.GetColorHexBrighter( false );
               
                RightBuffer.StartColor( textColor );
                ArcenExternalUIUtilities.ByPlanet_WriteStrengthIconWithColor( RightBuffer, textColor );

                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( RightBuffer, threatStrengthInt, true, true );
                if ( !isMinimalFogOfWar && planet.IntelLevel < PlanetIntelLevel.CurrentlyWatched )
                    RightBuffer.Add( "<size=12>?</size>" );

            }
        }
    }
}
