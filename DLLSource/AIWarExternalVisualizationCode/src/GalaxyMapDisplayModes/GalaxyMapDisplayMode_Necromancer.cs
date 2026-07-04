using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

namespace Arcen.AIW2.ExternalVisualization
{
    public class GalaxyMapDisplayMode_Necromancer : BaseGalaxyMapDisplayMode
    {
        public override bool GetShouldBeShownInCurrentCampaign()
        {
            // Only shows with a Necromancer in game.
            return World_AIW2.Instance.GetFirstFactionWithBaseInfoSourceType( "NecromancerEmpireFactionBaseInfo" ) != null;
        }

        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            //first get the player squads
            int newOtherGimbalIndex = 0;
            UtilityMethodsFor_GalaxyMapDisplayMode.AddPlayerFleetsToGalaxyMapOtherThingsToShow( planet, EntityToSkip, ArrayToFill, ref newOtherGimbalIndex );

            // Add anything that Necromancers would care about.
            foreach ( GameEntity_Squad entity in planet.Squads() )
            {
                ElderlingsPerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<ElderlingsPerUnitBaseInfo>();
                if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() && !(data?.TrackedByPlayer ?? false) )
                    continue; // Don't let the player see things they shouldn't.

                if ( newOtherGimbalIndex >= ArrayToFill.Length )
                    break; // Ran out of room.

                if ( entity.TypeData.GetHasTag( "EssenceGranter" ) ||
                    entity.TypeData.GetHasTag( "TemplarPrimaryDefensiveStructure" ) ||
                    entity.TypeData.GetHasTag( "TemplarWaveLeader" ) ||
                    entity.TypeData.GetHasTag( "TemplarRift" ) ||
                    entity.TypeData.GetHasTag( "NecromancerAmplifier" ) )
                {
                    ArrayToFill[newOtherGimbalIndex] = entity;
                    newOtherGimbalIndex++;
                }

            }
        }

        public override void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            int hostileStrength = 0;
            FInt totalHacking = FInt.Zero,  totalScience = FInt.Zero, totalEssence = FInt.Zero;

            Faction faction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( faction != null )
                hostileStrength = planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Hostile].TotalStrength;

            foreach ( GameEntity_Squad entity in planet.Squads() )
            {
                DLC3GameEntityTypeDataExtension entity_DLC3TypeData = entity.TypeData.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );
                if ( entity_DLC3TypeData == null || entity.TypeData.IsCommandStation )
                    continue;

                ElderlingsPerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<ElderlingsPerUnitBaseInfo>();
                if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() && !(data?.TrackedByPlayer ?? false) )
                    continue; // Don't let the player see things they shouldn't.

                entity_DLC3TypeData.GetNecromancerResourcesToGrantOnDeath(entity,
                        out FInt scienceToGrantOnDeath, out FInt hackingToGrantOnDeath, out FInt resourceOneToGrantOnDeath);
                totalHacking += hackingToGrantOnDeath;
                totalEssence += resourceOneToGrantOnDeath;
                totalScience += scienceToGrantOnDeath;

            }

            // Left Buffer
            if ( totalHacking > FInt.Zero )
                LeftBuffer.AddHacking_Truncated( totalHacking, true );

            LeftBuffer.Add( "\n" );

            if ( totalEssence > FInt.Zero )
                LeftBuffer.AddResourceOne_Truncated( totalEssence, true );

            // Right Buffer
            if ( hostileStrength > 0 )
            {
                Faction pressuringFaction = planet.GetControllingOrInfluencingFaction();
                if ( faction.GetIsFriendlyTowards( pressuringFaction ) )
                    pressuringFaction = World_AIW2.Instance.GetFirstFactionWithBaseInfoSourceType( "TemplarFactionBaseInfo" );

                ArcenExternalUIUtilities.ByPlanet_WriteStrengthIconWithColor( RightBuffer, pressuringFaction?.FactionCenterColor.ColorHex ?? "ffffff" );
                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( RightBuffer, hostileStrength, true, true );
                RightBuffer.EndColor();
            }

            RightBuffer.Add( "\n" );

            if ( totalScience > FInt.Zero )
                RightBuffer.AddScience_Truncated( totalScience, true );
        }
    }
}