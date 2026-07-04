using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using Arcen.AIW2.External;

namespace Arcen.AIW2.ExternalVisualization
{
    public class GalaxyMapDisplayMode_AsteroidCounts : BaseGalaxyMapDisplayMode
    {
        public override bool GetShouldBeShownInCurrentCampaign()
        {
            if ( !World_AIW2.Instance.PlayersAreInDistributedResourceGenerationMode )
                return false;

            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            PlayerTypeData playerType = localFaction == null ? null : localFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
            if ( playerType == null || playerType.UsesMetal || playerType.UsesEnergyAndFuel )
                return true;
            return false;
        }
        public override bool GetShouldReplaceNormalPlanetTooltip()
        {
            return true;
        }
        public override void WriteToPlanetTooltip( Planet planet, ArcenDoubleCharacterBuffer Buffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;
            int asteroidCountClaimed = 0;
            int asteroidCountTotal = 0;
            foreach ( GameEntity_Squad producer in planet.Squads( EntityRollupType.MetalProducers ) )
            {
                if ( producer.TypeData.IsAsteroidMine )
                {
                    asteroidCountTotal++;
                    if ( producer.GetFactionTypeSafe() == FactionType.Player && !producer.HasNotYetBeenFullyClaimed )
                        asteroidCountClaimed++;
                }
            }

            Buffer.Add( "\nAsteroid Mining Powerplants: " ).Add( asteroidCountClaimed ).Add( "/" ).Add( asteroidCountTotal );
            if ( asteroidCountTotal < 4 )
                Buffer.StartColor( "af9380" ).Add( "\nThis is a VERY poor location to try to bolster your economy." );
            else if ( asteroidCountTotal < 7 )
                Buffer.StartColor( "ffd674" ).Add( "\nThis is an average quality location to bolster your economy." );
            else if ( asteroidCountTotal < 10 )
                Buffer.StartColor( "cfff4f" ).Add( "\nThis is an excellent location to bolster your economy." );
            else
                Buffer.StartColor( "ea5dff" ).Add( "\nThis is a crazy good location to bolster your economy." );
        }

        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            GalaxyMapDisplayMode_Normal.ShowNormalIcons( planet, EntityToSkip, ArrayToFill, OnlyShowThingsThatShouldBeInFarZoom );
        }

        public override void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            int asteroidCountClaimed = 0;
            int asteroidCountTotal = 0;
            foreach ( GameEntity_Squad producer in planet.Squads( EntityRollupType.MetalProducers ) )
            {
                if ( producer.TypeData.IsAsteroidMine )
                {
                    asteroidCountTotal++;
                    if ( producer.GetFactionTypeSafe() == FactionType.Player && !producer.HasNotYetBeenFullyClaimed )
                        asteroidCountClaimed++;
                }
            }

            RightBuffer.Add( "<size=120%>" );

            if ( asteroidCountTotal < 4 )
                RightBuffer.StartColor( "af9380" );
            else if ( asteroidCountTotal < 7 )
                RightBuffer.StartColor( "ffd674" );
            else if ( asteroidCountTotal < 10 )
                RightBuffer.StartColor( "cfff4f" );
            else
                RightBuffer.StartColor( "ea5dff" );

            bool isOwnedByPlayer = false;
            Faction ownerFaction = planet.GetControllingOrInfluencingFaction();
            if ( ownerFaction != null && ownerFaction.Type == FactionType.Player )
                isOwnedByPlayer = true;

            if ( asteroidCountClaimed == asteroidCountTotal || ( !isOwnedByPlayer && asteroidCountClaimed == 0 ) )
                RightBuffer.Add( asteroidCountTotal );
            else
                RightBuffer.Add( asteroidCountClaimed ).Add( "/" ).Add( asteroidCountTotal );
        }
    }
}
