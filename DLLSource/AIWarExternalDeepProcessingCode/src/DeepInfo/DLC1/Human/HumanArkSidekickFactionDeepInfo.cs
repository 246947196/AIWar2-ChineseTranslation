using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    // note: this is actually a mod faction, added by the sidekicks mod, despite being in this external code csproj
    public class HumanArkSidekickFactionDeepInfo : HumanArkEmpireFactionDeepInfo
    {
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( EntityRollupType.MobileFleetFlagships ) )
            {
                Fleet fleet = entity.GetFleetOrNull_Safe();
                if ( fleet != null && !(fleet.BaseInfo is HumanMobileFleetBaseInfo) )
                    fleet.CreateExternalBaseInfo<HumanMobileFleetBaseInfo>( "HumanMobileFleetBaseInfo" );
            }
        }

        public override bool SeedUnitsOnStartingPlanetDuringMapGen( Planet StartingPlanet, ConfigurationForFaction factionConfig, PlanetFaction pFaction, 
            ref ArcenPoint commandStationPoint, ref bool stillNeedsToSeedHumanHomeworldStuff, 
            Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull, ArcenHostOnlySimContext Context )
        {
            // dont call base, we are overloading it
            //bool noerror = base.SeedUnitsOnStartingPlanetDuringMapGen( StartingPlanet, factionConfig, pFaction, ref commandStationPoint, ref stillNeedsToSeedHumanHomeworldStuff, TutorialPlanetOrNull, Context );
            //if (noerror == false)
                //return false;

            #region Start-of-game work for Ark Empires
            int debugIndex = 0;
            try
            {
                 CanClaimFreePlanet = true;

                // First planet is free! Get it while it's hot.
                foreach ( Planet workingPlanet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( workingPlanet.GetIsControlledByFactionType( FactionType.Player ) )
                        continue;

                    PlanetFaction pf = workingPlanet.GetPlanetFactionForFaction( AttachedFaction );
                    pf.AIPLeftFromCommandStation = 0;
                    pf.AIPLeftFromWarpGate = 0;
                }
                World_AIW2.Instance.QueueChatMessageOrCommand( $"You have managed to lay claim to a powerful Ark, a small fleet, and an extended map of the galaxy. Due to how minor your forces are, the first planet you capture will cost 0 AIP, so pick wisely. If you raise your AIP above 20 before doing so, the AI will take full notice of you and you will no longer get a free planet.", ChatType.LogToCentralChat, null );
                IScenarioImplementation scenarioImp = World_AIW2.Instance.GetScenarioImplementationSafe_OrNull();
                foreach ( GameEntity_Squad king in AttachedFaction.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    if ( scenarioImp != null && king.Planet != null )
                    {
                        scenarioImp.DoScoutingAfterCommandStationDeath( Context, king.Planet );
                        scenarioImp.DoScoutingAfterCommandStationDeath( Context, king.Planet );
                    }
                }

                debugIndex = 10;

                GameEntityTypeData humanKingUnitData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( "StartingArkSidekick", false ) );
                if ( humanKingUnitData == null ) //the "Random" name won't be found, so this will suffice to say "or random!"
                {
                    //if was null or random, choose one at random.
                    retry:
                    humanKingUnitData = (GameEntityTypeData)GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "HumanArkSidekickArks" );
                    if ( humanKingUnitData.OriginalXmlData.GetBool( "custom_neverRandom", false, false ) )
                        goto retry;
                }

                if ( humanKingUnitData == null )
                    throw new Exception( "No humanKingUnitData could be found!  Evidently nothing has the tag of HumanArkEmpireArks." );

                debugIndex = 100;
                int offsetDistanceMin = humanKingUnitData.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius * 2;
                int offsetDistanceMax = offsetDistanceMin * 3;
                int minDistance = 30000;
                int loop = 0;

                debugIndex = 200;

                commandStationPoint = ArcenPoint.OutOfRange;
                do
                {
                    if ( commandStationPoint == ArcenPoint.OutOfRange )
                        commandStationPoint = StandardMapPopulator.EnsureFarFromWormholesIfPossible( offsetDistanceMin, offsetDistanceMax, minDistance,
                            30, Engine_AIW2.Instance.CombatCenter, StartingPlanet, Context );

                    minDistance -= 1000;
                }
                while ( loop++ < 30 && commandStationPoint == ArcenPoint.OutOfRange );
                GameEntity_Squad newArk = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, humanKingUnitData, humanKingUnitData.MarkFor( pFaction ),
                    pFaction.FleetUsedAtPlanet, 0, commandStationPoint, Context, "ArkEmpireInitialSeed" );
                stillNeedsToSeedHumanHomeworldStuff = false;
                debugIndex = 500;

                debugIndex = 2000;
                int innerSystemMinimumRadius = (StartingPlanet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 150 )).IntValue;
                int innerSystemMaximumRadius = (StartingPlanet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 300 )).IntValue;

                Fleet arkFleet = newArk.FleetMembership.Fleet;
                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedIntegers.Add( arkFleet.FleetID ); //FleetID
                command.RelatedIntegers.Add( PlayerAccount.Local.PlayerPrimaryKeyID );
                command.RelatedString = "ToggleIsFleetOnPlayerWatchlist";
                command.RelatedBool = true;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                for ( int i = 1; i <= 5; i++ )
                {
                    string tagAndfieldName = "ArkSidekickOptionGroup" + i;

                    GameEntityTypeData newShipLineData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( tagAndfieldName, false ) );
                    if ( newShipLineData == null ) //the "Random" name won't be found, so this will suffice to say "or random!"
                    {
                        //if was null or random, choose one at random.
                        newShipLineData = (GameEntityTypeData)GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, tagAndfieldName );
                    }
                    if ( newShipLineData != null )
                    {
                        short shipCap = newShipLineData.OriginalXmlData.GetInt16( "custom_arkempire_fleetcap", 50, true );
                        var mem = arkFleet.GetOrAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates( newShipLineData, arkFleet.GetNextUniqueIntToUseOfMatchingMembershipGroupsBasedOnSquadType( newShipLineData ) );
                        mem.ExplicitBaseSquadCap = shipCap;
                    }
                }

                {
                    //Also give the Ark Sidekick a battlestation
                    FleetDesignTemplate initialPlayerBattlestation1 = (FleetDesignTemplate)FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows[Engine_Universal.PermanentQualityRandom.Next( 0, FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows.Count )];
                    FleetItem centerpiece = initialPlayerBattlestation1.GetCategory( FleetItemDrawBagCategory.Centerpiece ).DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( centerpiece == null || centerpiece.TypeData == null )
                        Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null centerpiece found in InitialPlayerBattlestation " + initialPlayerBattlestation1.InternalName );
                    else
                    {
                        debugIndex = 4500;
                        ArcenPoint entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                        entityPoint = StartingPlanet.GetSafePlacementPoint_SpecificPoint( Context, centerpiece.TypeData, entityPoint, 200, 1000 );
                        GameEntity_Squad actualCenterpiece = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, centerpiece.TypeData, 1, null, 0, entityPoint, Context, "HumanArkSidekickStartSpawn" );
                        if ( actualCenterpiece == null )
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null actualCenterpiece found in InitialPlayerBattlestation FleetItem " + centerpiece.TypeData.InternalName );
                        else
                        {
                            Fleet centerpieceFleet = actualCenterpiece.GetFleetOrNull_Safe();
                            if ( centerpieceFleet != null )
                            {
                                centerpieceFleet.ClearAllMembershipsExceptForCenterpiece();
                                centerpieceFleet.SetAllMembershipsUpFromDesignTemplates_HostOnly( initialPlayerBattlestation1, FleetDesignLogic.Unused, null );
                            }
                            centerpieceFleet.NameRaw = initialPlayerBattlestation1.DisplayName;
                        }
                    }
                }
                {
                    //Also give the ark sidekick a support fleet
                    FleetDesignTemplate initialPlayerSupportFleet = (FleetDesignTemplate)FleetDesignTemplateTable.Instance.InitialPlayerSupportFleetsAsBaseRows[Engine_Universal.PermanentQualityRandom.Next( 0, FleetDesignTemplateTable.Instance.InitialPlayerSupportFleetsAsBaseRows.Count )];

                    FleetItem centerpiece = initialPlayerSupportFleet.GetCategory( FleetItemDrawBagCategory.Centerpiece ).DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( centerpiece == null || centerpiece.TypeData == null )
                        Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null centerpiece found in InitialPlayerSupportFleets " + initialPlayerSupportFleet.InternalName );
                    else
                    {
                        debugIndex = 6500;
                        ArcenPoint entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                        entityPoint = StartingPlanet.GetSafePlacementPoint_SpecificPoint( Context, centerpiece.TypeData, entityPoint, 200, 1000 );
                        GameEntity_Squad actualCenterpiece = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, centerpiece.TypeData, 1, null, 0, entityPoint, Context, "HumanEmpireStartSpawn" );
                        if ( actualCenterpiece == null )
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null actualCenterpiece found in InitialPlayerSupportFleets FleetItem " + centerpiece.TypeData.InternalName );
                        else
                        {
                            Fleet centerpieceFleet = actualCenterpiece.GetFleetOrNull_Safe();
                            if ( centerpieceFleet != null )
                            {
                                centerpieceFleet.ClearAllMembershipsExceptForCenterpiece();
                                centerpieceFleet.SetAllMembershipsUpFromDesignTemplates_HostOnly( initialPlayerSupportFleet, FleetDesignLogic.Unused, null );
                            }
                            centerpieceFleet.NameRaw = initialPlayerSupportFleet.DisplayName;
                        }
                    }

                }             
                foreach ( Planet planet in StartingPlanet.LinkedNeighborsAndSelf( false ) )
                {
                    planet.GrantIntel( PlanetIntelLevel.CurrentlyWatched );
                }
            }
            catch ( Exception e )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "HumanArkEmpireSpecificCodeDeepInfo SeedUnitsOnStartingPlanetDuringMapGen error at debugIndex " + debugIndex + ": " + e );
                return false;
            }
            #endregion

            return true;
        }
    }
}
