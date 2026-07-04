using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using Arcen.AIW2.External;

namespace Arcen.AIW2.ExternalVisualization
{
    public class GalaxyMapDisplayMode_FuelAvailability : BaseGalaxyMapDisplayMode
    {
        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;
            int newOtherGimbalIndex = 0;
            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.FuelArgonProducers ) )
            {
                if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                //skip fleet leaders of players, since we'll add those later anyway.  Don't need double!
                if ( entity.TypeData.IsFleetLeader && entity.GetFactionTypeSafe() == FactionType.Player )
                    continue;
                ArrayToFill[newOtherGimbalIndex] = entity;
                newOtherGimbalIndex++;
                if ( newOtherGimbalIndex >= ArrayToFill.Length )
                    break;
            }
            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.FuelRadonProducers ) )
            {
                if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                //skip fleet leaders of players, since we'll add those later anyway.  Don't need double!
                if ( entity.TypeData.IsFleetLeader && entity.GetFactionTypeSafe() == FactionType.Player )
                    continue;
                // Skip things that produce Argon, since they have already been added
                if (entity.TypeData.GetMatches_SemiSlow(EntityRollupType.FuelArgonProducers)) {
                    continue;
                }
                ArrayToFill[newOtherGimbalIndex] = entity;
                newOtherGimbalIndex++;
                if ( newOtherGimbalIndex >= ArrayToFill.Length )
                    break;
            }
            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.FuelXenonProducers ) )
            {
                if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                //skip fleet leaders of players, since we'll add those later anyway.  Don't need double!
                if ( entity.TypeData.IsFleetLeader && entity.GetFactionTypeSafe() == FactionType.Player )
                    continue;
                // Skip things that produce Argon or Radon, since they have already been added
                if (entity.TypeData.GetMatches_SemiSlow(EntityRollupType.FuelArgonProducers)
                       || entity.TypeData.GetMatches_SemiSlow(EntityRollupType.FuelRadonProducers)) {
                    continue;
                }
                ArrayToFill[newOtherGimbalIndex] = entity;
                newOtherGimbalIndex++;
                if ( newOtherGimbalIndex >= ArrayToFill.Length )
                    break;
            }

            UtilityMethodsFor_GalaxyMapDisplayMode.AddPlayerFleetsToGalaxyMapOtherThingsToShow( planet, EntityToSkip, ArrayToFill, ref newOtherGimbalIndex );
        }

        public override bool GetShouldBeShownInCurrentCampaign()
        {
            if ( !World_AIW2.Instance.IsFuelEnabled )
                return false;

            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            PlayerTypeData playerType = localFaction == null ? null : localFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
            if ( playerType == null || playerType.UsesEnergyAndFuel )
                return true;
            return false;
        }

        public override void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            FactionFilter currentFilter = FactionFilter.GetFactionFilterByIndex( PlayerAccount_AIW2.GetCurrentGalaxyMapDisplayMode_FactionIndexSafe() );

            FInt argon = FInt.Zero;
            FInt radon = FInt.Zero;
            FInt xenon = FInt.Zero;
            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.FuelArgonProducers ) )
            {
                if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                if ( entity.GetMatchesFactionFilterSafe( currentFilter ) )
                    argon += entity.GetFullyMultipliedFuelArgonToProduce();
            }
            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.FuelRadonProducers ) )
            {
                if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                if ( entity.GetMatchesFactionFilterSafe( currentFilter ) )
                    radon += entity.GetFullyMultipliedFuelRadonToProduce();
            }
            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.FuelXenonProducers ) )
            {
                if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                if ( entity.GetMatchesFactionFilterSafe( currentFilter ) )
                    xenon += entity.GetFullyMultipliedFuelXenonToProduce();
            }

            //RIGHT ONLY
            if ( argon > FInt.Zero )
                RightBuffer.Add( ArcenExternalUIUtilities.ByPlanet_FuelArgonTextColorAndIcon )
                    .StartColor( ArcenExternalUIUtilities.FuelArgonTextColor ).AddNumberMoreReadable( ( argon.IntValue / 1000 ) ).Add( "k" ).EndColor().Add( "\n" );
            if ( radon > FInt.Zero )
                RightBuffer.Add( ArcenExternalUIUtilities.ByPlanet_FuelRadonTextColorAndIcon )
                    .StartColor( ArcenExternalUIUtilities.FuelRadonTextColor ).AddNumberMoreReadable( (radon.IntValue / 1000) ).Add( "k" ).EndColor().Add( "\n" );
            if ( xenon > FInt.Zero )
                RightBuffer.Add( ArcenExternalUIUtilities.ByPlanet_FuelXenonTextColorAndIcon )
                    .StartColor( ArcenExternalUIUtilities.FuelXenonTextColor ).AddNumberMoreReadable( (xenon.IntValue / 1000) ).Add( "k" ).EndColor().Add( "\n" );
        }
    }
}
