using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using Arcen.AIW2.External;

namespace Arcen.AIW2.ExternalVisualization
{
    public class GalaxyMapDisplayMode_EnergyConsumption : BaseGalaxyMapDisplayMode
    {
        public override bool GetShouldBeShownInCurrentCampaign()
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            PlayerTypeData playerType = localFaction == null ? null : localFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
            if ( playerType == null || playerType.UsesEnergyAndFuel )
                return true;
            return false;
        }

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

            //reset our list of top items
            UtilityMethodsFor_GalaxyMapDisplayMode.ResetForCollectTopmostSquads( ArrayOfTopItems );

            int effectiveMaxCount = Math.Min( MAX_ICONS_WE_WANT, MAX_ICONS_AVAILABLE_OVERALL - newOtherGimbalIndex );

            FactionFilter currentFilter = FactionFilter.GetFactionFilterByIndex( PlayerAccount_AIW2.GetCurrentGalaxyMapDisplayMode_FactionIndexSafe() );
            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.EnergyConsumers ) )
            {
                if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                if ( !entity.GetMatchesFactionFilterSafe( currentFilter ) )
                    continue;
                if ( !entity.GetIsPlayerUnit() )
                    continue; //only player units use energy!

                //is this among the top 12 strongest types of squads present for this filter?  If so, include it.
                UtilityMethodsFor_GalaxyMapDisplayMode.CollectTopmostSquads( ArrayOfTopItems, entity, entity.GetEnergyUsage(), true, true, effectiveMaxCount );

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

            FactionFilter currentFilter = FactionFilter.GetFactionFilterByIndex( PlayerAccount_AIW2.GetCurrentGalaxyMapDisplayMode_FactionIndexSafe() );

            FInt energyForFilter = FInt.Zero;
            FInt energyForFilterContents = FInt.Zero;
            foreach ( GameEntity_Squad entity in planet.Squads() )
            {
                if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                if ( !entity.GetIsPlayerUnit() )
                    continue; //only player units use energy!

                if ( entity.GetMatchesFactionFilterSafe( currentFilter ) )
                {
                    energyForFilter += entity.GetEnergyUsage();
                    energyForFilterContents += entity.GetEnergyCostOfContentsIfAny();
                }
            }

            //RIGHT ONLY
            if ( energyForFilter > FInt.Zero )
                RightBuffer.Add( ArcenExternalUIUtilities.ByPlanet_EnergyTextColorAndIcon )
                    .StartColor( ArcenExternalUIUtilities.EnergyTextColor ).Add( "-" ).AddNumberMoreReadable( energyForFilter.IntValue ).EndColor().Add( "\n" );
            if ( energyForFilterContents > FInt.Zero )
                RightBuffer.StartColor( ArcenExternalUIUtilities.EnergyTextColor ).Add( ArcenExternalUIUtilities.ByPlanet_EnergyTextColorAndIcon )
                    .Add( "-" ).AddNumberMoreReadable( energyForFilterContents.IntValue ).EndColor().Add( "\n" );
        }
    }
}
