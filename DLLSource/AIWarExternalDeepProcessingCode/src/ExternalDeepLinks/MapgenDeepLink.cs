using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class MapgenDeepLink : MapgenDeepLinkRoot
    {
        public MapgenDeepLink()
        {
            MapgenDeepLinkRoot.Instance = this;
        }

        public override void SeedWardenSecretNinjaHideouts( ArcenHostOnlySimContext Context, Faction faction, int numToSeed )
        {
            if ( Context == null )
                return; //client
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, World_AIW2.Instance.CurrentGalaxy, faction, SpecialEntityType.WardenSecretNinjaHideout, null, SeedingType.HardcodedCount, numToSeed,
                MapGenCountPerPlanet.One, MapGenSeedStyle.SmallBad, 2, -1, PlanetSeedingZone.OuterSystem, SeedingExpansionType.DoNoExpansion, faction.GetParentFactionOrNull(), 0 );
        }
    }
}
