using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using Arcen.AIW2.External;
using UnityEngine;

namespace Arcen.AIW2.ExternalVisualization
{
    public class GalaxyMapDisplayMode_Strength : BaseGalaxyMapDisplayMode
    {
        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            //first get the player squads
            int newOtherGimbalIndex = 0;
            UtilityMethodsFor_GalaxyMapDisplayMode.AddPlayerFleetsToGalaxyMapOtherThingsToShow( planet, EntityToSkip, ArrayToFill, ref newOtherGimbalIndex );
        }

        public override void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            int totalStrength = 0;
            FactionFilter currentFilter = FactionFilter.GetFactionFilterByIndex( PlayerAccount_AIW2.GetCurrentGalaxyMapDisplayMode_FactionIndexSafe() );
            foreach ( GameEntity_Squad entity in planet.Squads() )
            {
                if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                if ( !entity.GetMatchesFactionFilterSafe( currentFilter ) )
                    continue;
                totalStrength += entity.GetStrengthOfSelfAndContents();
            }

            //RIGHT ONLY
            if ( totalStrength > 0 )
            {
                RightBuffer.StartColor( currentFilter.GetTextColor() );
                ArcenExternalUIUtilities.ByPlanet_WriteStrengthIconWithColor( RightBuffer, currentFilter.GetTextColor() );

                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( RightBuffer, totalStrength, true, true );
            }
        }
    }

    public class GalaxyMapDisplayMode_StrengthWithTopShips : BaseGalaxyMapDisplayMode
    {
        private const int MAX_ICONS_AVAILABLE_OVERALL = 12; //12 is the max we can have
        private const int MAX_ICONS_WE_WANT = 3; //12 is the max we can have, but let's only choose the top 3
        private RefPair<SafeSquadWrapper, int>[] ArrayOfTopItems = new RefPair<SafeSquadWrapper, int>[MAX_ICONS_WE_WANT];

        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            //first get the player squads
            int newOtherGimbalIndex = 0;
            UtilityMethodsFor_GalaxyMapDisplayMode.AddPlayerFleetsToGalaxyMapOtherThingsToShow( planet, EntityToSkip, ArrayToFill, ref newOtherGimbalIndex );

            //reset our ilst of top items
            UtilityMethodsFor_GalaxyMapDisplayMode.ResetForCollectTopmostSquads( ArrayOfTopItems );

            int effectiveMaxCount = Math.Min( MAX_ICONS_WE_WANT, MAX_ICONS_AVAILABLE_OVERALL - newOtherGimbalIndex );

            FactionFilter currentFilter = FactionFilter.GetFactionFilterByIndex( PlayerAccount_AIW2.GetCurrentGalaxyMapDisplayMode_FactionIndexSafe() );
            foreach ( GameEntity_Squad entity in planet.Squads() )
            {
                if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                if ( !entity.GetMatchesFactionFilterSafe( currentFilter ) )
                    continue;
                if ( !entity.TypeData.IsCombatant )
                    continue; //skip any non-combatants

                //is this among the top 12 strongest types of squads present for this filter?  If so, include it.
                UtilityMethodsFor_GalaxyMapDisplayMode.CollectTopmostSquads( ArrayOfTopItems, entity, entity.GetStrengthPerSquad(), true, true, effectiveMaxCount );

            }

            //whatever we found for the top items, add them
            foreach ( RefPair<SafeSquadWrapper, int> kv in ArrayOfTopItems )
            {
                GameEntity_Squad squad = kv.LeftItem.GetSquad();
                if ( squad == null )
                    continue;
                ArrayToFill[newOtherGimbalIndex] = squad;
                newOtherGimbalIndex++;
                if ( newOtherGimbalIndex >= ArrayToFill.Length )
                    break;
            }
        }

        public override void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            int totalStrength = 0;
            FactionFilter currentFilter = FactionFilter.GetFactionFilterByIndex( PlayerAccount_AIW2.GetCurrentGalaxyMapDisplayMode_FactionIndexSafe() );
            foreach ( GameEntity_Squad entity in planet.Squads() )
            {
                if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                if ( !entity.GetMatchesFactionFilterSafe( currentFilter ) )
                    continue;
                totalStrength += entity.GetStrengthOfSelfAndContents();
            }

            //RIGHT ONLY
            if ( totalStrength > 0 )
            {
                RightBuffer.StartColor( currentFilter.GetTextColor() );
                ArcenExternalUIUtilities.ByPlanet_WriteStrengthIconWithColor( RightBuffer, currentFilter.GetTextColor() );

                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( RightBuffer, totalStrength, true, true );
            }
        }
    }
}