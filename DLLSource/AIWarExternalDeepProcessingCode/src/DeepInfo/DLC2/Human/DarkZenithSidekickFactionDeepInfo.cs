using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    /*
Some gameplay notes


new balance levers/costs for sidekick; income upgrades too cheap
combine the income upgrades for the hacking menu view

find some other way to cap things. Maybe few transports? Maybe you need to capture transports?

I think some of the upgrades need to go away (unlock guardian tier?)
too many dires, not enough mix once Dires are unlocked.
I think this is probably true for the base DZ too

     Unit cap?
     transports too fast?
     buff templar

Spire sidekick:
fallen spire, but no metal (necromancer rules)
Some mechanic for science, otherwise just good to go.
Maybe AI gets science generating Dark Spire themed units.
Start with a mini-ship, then you need to get the Capitol

recommend slightly lower difficulty, since human allies won't get anything

#######################
Change the Merkismathr somehow? It's annoying how long its out of phase
Option to keep galaxy view locked
Spire city upgrades are shown to non-human players
     */
    public sealed class DarkZenithSidekickFactionDeepInfo : DarkZenithFactionDeepInfoRoot, IExternalDeepInfo_Singleton
    {
        public override bool IsSvikari => false;
        public override bool IsHumanSidekick => true;

        public static DarkZenithSidekickFactionDeepInfo Instance = null;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<DarkZenithSidekickFactionBaseInfo>();
            Instance = this;
            this.AttachedFaction.ExternallyHandleMarkLevels = true;
        }

        protected override void SubCleanup()
        {
            Instance = null;
        }
        public override void SeedSpecialEntities_LateAfterAllFactionSeeding_CustomForPlayerType( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData MapData )
        {
           //The DZ Sidekick flagships are spawned with the other Jormugandr spawning in the GameCommand
            AttachedFaction.StoredScience = FInt.FromParts( 8000, 000 );
            AttachedFaction.StoredHacking = FInt.FromParts( 25, 000 );
            int seedThisMany = 3;
            //a few near the player
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "DarkZenithFlagshipTierOne", SeedingType.CapturableWeightsAndMax,
                seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 2, 5, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
            //and some further away
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "DarkZenithFlagshipTierOne", SeedingType.CapturableWeightsAndMax,
            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 4, 99, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );

            seedThisMany = 1;
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "DZFortress", SeedingType.CapturableWeightsAndMax,
            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 1, 2, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
            //and some further away
            seedThisMany = 3;
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "DZFortress", SeedingType.CapturableWeightsAndMax,
            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 2, 5, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );

            seedThisMany = 4;
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "DZFortress", SeedingType.CapturableWeightsAndMax,
                seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 4, 99, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );

            //A few Shattered Moons; these are only for the Dark Zenith
            seedThisMany = 1;
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "DarkZenithShatteredMoon", SeedingType.CapturableWeightsAndMax,
                seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 2, 5, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
            seedThisMany = 1;
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "DarkZenithShatteredMoon", SeedingType.CapturableWeightsAndMax,
                seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 4, 99, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );

        }
        public override bool SeedUnitsOnStartingPlanetDuringMapGen( Planet StartingPlanet, ConfigurationForFaction factionConfig, PlanetFaction pFaction, 
            ref ArcenPoint commandStationPoint, ref bool stillNeedsToSeedHumanHomeworldStuff, 
            Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull, ArcenHostOnlySimContext Context )
        {
            int debugIndex = 0;
            try
            {
                //We do this seeding elsewhere when the planets are created
                stillNeedsToSeedHumanHomeworldStuff = false;
            }
            catch ( Exception e)
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "DarkZenithEmpireSpecificCodeDeepInfo SeedUnitsOnStartingPlanetDuringMapGen error at debugIndex " + debugIndex + ": " + e );
                return false;
            }
            return true;
        }

    }
}
