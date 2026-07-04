using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class NecromancerSidekickFactionDeepInfo : NecromancerEmpireFactionDeepInfo
    {
        public override bool SeedUnitsOnStartingPlanetDuringMapGen( Planet StartingPlanet, ConfigurationForFaction factionConfig, PlanetFaction pFaction, 
            ref ArcenPoint commandStationPoint, ref bool stillNeedsToSeedHumanHomeworldStuff, 
            Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull, ArcenHostOnlySimContext Context )
        {
            int debugIndex = 0;
            try
            {
                debugIndex = 10;
                Faction necroFaction = pFaction.Faction;
                debugIndex = 15;

                NecromancerEmpireFactionDeepInfo necroDeepInfo = pFaction.Faction.GetExternalDeepInfoAs<NecromancerEmpireFactionDeepInfo>();
                NecromancerEmpireFactionBaseInfo necroBaseInfo = necroDeepInfo.BaseInfo;

                debugIndex = 20;

                stillNeedsToSeedHumanHomeworldStuff = false;

                GameEntity_Squad necroFlagship;
                GameEntity_Squad necropolis = necroDeepInfo.SpawnNecromancerNecropolis( ArcenPoint.ZeroZeroPoint, StartingPlanet, "SidekickPhylactery", necroFaction, Context, out necroFlagship, false );
                Fleet necroCity = necropolis.GetFleetOrNull_Safe();

                FInt placementOffsetScale = FInt.FromParts( 1, 500 );

                StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, necropolis.WorldLocation, pFaction, necroCity, placementOffsetScale, "NecromancerShipyard", 2400, 600 );

                if ( necroFlagship != null )
                {
                    Fleet necroFleet = necroFlagship.GetFleetOrNull_Safe();
                    GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    command.RelatedIntegers.Add( necroFleet.FleetID ); //FleetID
                    command.RelatedIntegers.Add( PlayerAccount.Local.PlayerPrimaryKeyID );
                    command.RelatedString = "ToggleIsFleetOnPlayerWatchlist";
                    command.RelatedBool = true;
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                    necroFleet.IsFleetInTransportLoadMode = false;

                    GameEntityTypeData spawnType;

                    //60 skeleton base
                    spawnType = GameEntityTypeDataTable.Instance.GetRowByName( "BaseSkeleton" );
                    for ( int i = 0; i < 60; i++ )
                        necroFlagship.SpawnEntity_ReturnNullIfMPClient( spawnType, 1, necroFleet, 0, 0, Context, "NecroSidekickStart", false );

                    //18 wight base
                    spawnType = GameEntityTypeDataTable.Instance.GetRowByName( "BaseWight" );
                    for ( int i = 0; i < 18; i++ )
                        necroFlagship.SpawnEntity_ReturnNullIfMPClient( spawnType, 1, necroFleet, 0, 0, Context, "NecroSidekickStart", false );
                }

                debugIndex = 50;
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Lore", string.Empty, necroFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Introduction", string.Empty, necroFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Resources", string.Empty, necroFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Phylactery", string.Empty, necroFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );

                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Hexes", string.Empty, necroFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );

                debugIndex = 100;
                necroFaction.StoredMetal = (FInt)(400 * 1000);
                necroFaction.StoredScience = FInt.FromParts( 3000, 000 );
                necroFaction.StoredHacking = FInt.FromParts( 45, 000 );
                necroFaction.StoredFactionResourceOne = FInt.FromParts( 50, 000 ); //enough to get things started.

                //At the beginning of the game the player gets one lowest-tier skeleton type and wight variant type
                //the goal of this is to let the player start out a bit stronger
                debugIndex = 200;
                string tagAndFieldName = "NecromancerSkeletonGroup";
                NecromancerUpgrade upgrade = NecromancerUpgradeTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( tagAndFieldName, false ) );
                if ( upgrade == null )
                {
                    upgrade = NecromancerUpgradeTable.Instance.GetRandomBonusStartingSkeletonType( Context );
                }
                necroBaseInfo.NecromancerCompletedUpgrades.Add( upgrade );

                debugIndex = 400;
                NecromancerUpgradeEvent thisEvent = NecromancerUpgradeEvent.Create( necroFaction.FactionIndex, -1, StartingPlanet.Index, upgrade.Index, -1, null );
                necroBaseInfo.NecromancerHistory.Add( thisEvent );
                StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, necropolis.WorldLocation, pFaction, necroCity, placementOffsetScale, upgrade.ShipForCapIncrease, 700, 600 );

                debugIndex = 500;
                tagAndFieldName = "NecromancerWightGroup";
                upgrade = NecromancerUpgradeTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( tagAndFieldName, false ) );
                if ( upgrade == null )
                {
                    upgrade = NecromancerUpgradeTable.Instance.GetRandomBonusStartingWightType( Context );
                }
                necroBaseInfo.NecromancerCompletedUpgrades.Add( upgrade );
                debugIndex = 600;
                thisEvent = NecromancerUpgradeEvent.Create( necroFaction.FactionIndex, -1, StartingPlanet.Index, upgrade.Index, -1, null );
                necroBaseInfo.NecromancerHistory.Add( thisEvent );
                StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, necropolis.WorldLocation, pFaction, necroCity, placementOffsetScale, upgrade.ShipForCapIncrease, 1600, 600 );

                debugIndex = 650;
                tagAndFieldName = "NecromancerUtilityGroup";
                upgrade = NecromancerUpgradeTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( tagAndFieldName, false ) );
                if ( upgrade == null )
                {
                    upgrade = NecromancerUpgradeTable.Instance.GetRandomBonusStartingUtilityType( Context );
                }
                necroBaseInfo.NecromancerCompletedUpgrades.Add( upgrade );
                debugIndex = 600;
                thisEvent = NecromancerUpgradeEvent.Create( necroFaction.FactionIndex, -1, StartingPlanet.Index, upgrade.Index, -1, null );
                necroBaseInfo.NecromancerHistory.Add( thisEvent );
                StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, necropolis.WorldLocation, pFaction, necroCity, placementOffsetScale, upgrade.ShipForCapIncrease, 1200, -200 );

                debugIndex = 700;
                if ( necroBaseInfo.BonusStartingResources > 0 )
                {
                    necroFaction.StoredScience += FInt.FromParts( 1000, 000 ) * necroBaseInfo.BonusStartingResources;
                    necroFaction.StoredHacking += FInt.FromParts( 20, 000 ) * necroBaseInfo.BonusStartingResources;
                    necroFaction.StoredFactionResourceOne += FInt.FromParts( 10, 000 ) * necroBaseInfo.BonusStartingResources;
                }
                debugIndex = 800;
                if ( necroBaseInfo.BonusStartingWight )
                {
                    do
                    {
                        upgrade = NecromancerUpgradeTable.Instance.GetRandomBonusStartingWightType( Context );
                    } while ( necroBaseInfo.NecromancerCompletedUpgrades.Contains( upgrade ) );
                    necroBaseInfo.NecromancerCompletedUpgrades.Add( upgrade );
                    thisEvent = NecromancerUpgradeEvent.Create( necroFaction.FactionIndex, -1, StartingPlanet.Index, upgrade.Index, -1, null );
                    necroBaseInfo.NecromancerHistory.Add( thisEvent );
                }
                if ( necroBaseInfo.StartWithAllUpgrades )
                {
                    while ( true )
                    {
                        upgrade = NecromancerUpgradeTable.Instance.GetNextFactionUpgrade( );
                        if ( upgrade == null )
                            break;
                        necroBaseInfo.NecromancerCompletedUpgrades.Add( upgrade );
                        thisEvent = NecromancerUpgradeEvent.Create( necroFaction.FactionIndex, -1, StartingPlanet.Index, upgrade.Index, -1, null );
                        necroBaseInfo.NecromancerHistory.Add( thisEvent );
                    }
                }

                debugIndex = 2000;
            }
            catch ( Exception e )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "NecromancerSidekickSpecificCodeDeepInfo SeedUnitsOnStartingPlanetDuringMapGen error at debugIndex " + debugIndex + ": " + e );
                return false;
            }
            return true;
        }

        public override void SeedSpecialEntities_LateAfterAllFactionSeeding_CustomForPlayerType( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData MapData )
        {
            IList<Planet> planetsSeeded;
            //one Skeleton Amplifier and one Wight Amplifier near this necromancer
            int necromancersInGame = 0;
            foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
            {
                PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                if ( playerType == null )
                    continue; //only seed ARS if they say to
                if ( playerType.GetHasTag("NecromancerSidekick") )
                {
                    //each necromancer gets a skeleton and wight amplifier close to them
                    necromancersInGame++;
                    planetsSeeded = StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "SkeletonAmplifier", SeedingType.CapturableWeightsAndMax,
                        1, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 1, 2, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                                     player, 1 );
                    planetsSeeded = StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "WightAmplifier", SeedingType.CapturableWeightsAndMax,
                        1, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 2, 3, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                                     player, 1 );
                }
            }
            if ( necromancersInGame == 0 )
                return;
            int skeletonAmps = 2 + necromancersInGame * 2;
            int wightAmps = 1 + necromancersInGame * 2;
            int mummyAmps = 1 + necromancersInGame;
            //seed skeleton amps
            planetsSeeded = StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "SkeletonAmplifier", SeedingType.CapturableWeightsAndMax,
                                                        skeletonAmps, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 3, 99, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
            //seed wight amps
            planetsSeeded = StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "WightAmplifier", SeedingType.CapturableWeightsAndMax,
                                                        wightAmps, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 3, 99, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
                        //And mummy amps
            planetsSeeded = StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "MummyAmplifier", SeedingType.CapturableWeightsAndMax,
                                                        mummyAmps, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 3, 99, 5, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
            
        }
    }
}
