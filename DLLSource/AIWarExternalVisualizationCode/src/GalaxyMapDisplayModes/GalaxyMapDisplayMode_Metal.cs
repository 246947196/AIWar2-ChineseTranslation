using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using Arcen.AIW2.External;

namespace Arcen.AIW2.ExternalVisualization
{
    public class GalaxyMapDisplayMode_Metal : BaseGalaxyMapDisplayMode
    {
        public override bool GetShouldBeShownInCurrentCampaign()
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            PlayerTypeData playerType = localFaction == null ? null : localFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
            if ( playerType == null || playerType.UsesMetal )
                return true;
            return false;
        }

        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            if(planet.IntelLevel <= PlanetIntelLevel.Unexplored)
                return;
            Dictionary<GameEntityTypeData, int> typeDatasAlreadyShown = GameEntityTypeData.GetTemporaryGameEntityTypeDataIntDict( "GalaxyMapDisplayMode_Metal-typeDatasAlreadyShown", 10f );
            if ( typeDatasAlreadyShown == null ) //blocked for teardown/shutdown; bail
                return;

            int newOtherGimbalIndex = 0;
            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.MetalProducers ) )
            {
                if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                if ( typeDatasAlreadyShown.ContainsKey( entity.TypeData ) )
                    continue;
                //skip fleet leaders of players, since we'll add those later anyway.  Don't need double!
                if ( entity.TypeData.IsFleetLeader && entity.GetFactionTypeSafe() == FactionType.Player )
                    continue;
                typeDatasAlreadyShown[entity.TypeData] = 1;
                ArrayToFill[newOtherGimbalIndex] = entity;
                newOtherGimbalIndex++;
                if ( newOtherGimbalIndex >= ArrayToFill.Length )
                    break;
            }

            UtilityMethodsFor_GalaxyMapDisplayMode.AddPlayerFleetsToGalaxyMapOtherThingsToShow( planet, EntityToSkip, ArrayToFill, ref newOtherGimbalIndex );
            GameEntityTypeData.ReleaseTemporaryGameEntityTypeDataIntDict( typeDatasAlreadyShown );
        }

        public override void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            FactionFilter currentFilter = FactionFilter.GetFactionFilterByIndex( PlayerAccount_AIW2.GetCurrentGalaxyMapDisplayMode_FactionIndexSafe() );

            FInt metalForFilter = FInt.Zero;
            FInt metalForOther = FInt.Zero;
            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.MetalProducers ) )
            {
                if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                if ( entity.GetMatchesFactionFilterSafe( currentFilter ) )
                    metalForFilter += entity.GetFullyMultipliedMetalToProduce();
                else
                    metalForOther += entity.GetFullyMultipliedMetalToProduce();
            }

            //RIGHT ONLY
            if ( metalForFilter > FInt.Zero )
                RightBuffer.Add( ArcenExternalUIUtilities.ByPlanet_MetalTextColorAndIcon )
                    .StartColor( ArcenExternalUIUtilities.MetalTextColor ).AddNumberMoreReadable( metalForFilter.IntValue ).EndColor().Add( "\n" );
            if ( metalForOther > FInt.Zero )
                RightBuffer.StartColor( ColorMath.HealthRedInner ).Add( ArcenExternalUIUtilities.ByPlanet_MetalTextColorAndIcon )
                    .AddNumberMoreReadable( metalForOther.IntValue ).EndColor().Add( "\n" );
        }
    }
}
