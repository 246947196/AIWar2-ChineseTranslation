using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class HumanEmpireFactionDeepInfo : ExternalFactionDeepInfoRoot
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

        public override void DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly( ref int debugStage, GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull,
              Faction factionThatKilledEntity, Faction entityOwningFaction, int numExtraStacksKilled, ArcenHostOnlySimContext Context )
        {
            base.DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly( ref debugStage, entity, Damage, FiringSystemOrNull, factionThatKilledEntity, entityOwningFaction, numExtraStacksKilled, Context);
            HumanFactionSharedDeep.CheckAndHandleStationDeath( AttachedFaction, ref debugStage, entity, Damage, FiringSystemOrNull, factionThatKilledEntity, entityOwningFaction, numExtraStacksKilled, Context );
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 5;

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            FleetBehaviorLRP.DoLRP( AttachedFaction, Context );
        }

        public override bool SeedUnitsOnStartingPlanetDuringMapGen( Planet StartingPlanet, ConfigurationForFaction factionConfig, PlanetFaction pFaction, 
            ref ArcenPoint commandStationPoint, ref bool stillNeedsToSeedHumanHomeworldStuff, 
            Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull, ArcenHostOnlySimContext Context )
        {
            int debugIndex = 0;
            try
            {
                debugIndex = 10;

                GameEntityTypeData humanKingUnitData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "NormalHumanEmpireHumanHomeCommand" );

                if ( humanKingUnitData == null )
                    throw new Exception( "No humanKingUnitData could be found!  Evidently nothing has the tag of NormalHumanEmpireHumanHomeCommand." );

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
                GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, humanKingUnitData, humanKingUnitData.MarkFor( pFaction ),
                    pFaction.FleetUsedAtPlanet, 0, commandStationPoint, Context, "HumanEmpireStartSpawn" );
                stillNeedsToSeedHumanHomeworldStuff = false;
                debugIndex = 500;
                pFaction.SetPlanetFactionBooleanFlag( PlanetFactionBooleanFlag.TryToCapture, true );
                debugIndex = 600;

                FInt placementOffsetScale = FInt.FromParts( 1, 500 );

                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeForcefield )
                    StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, commandStationPoint, pFaction, placementOffsetScale, "StartingForcefieldGenerator", -400, 0 );
                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeEngineers )
                {
                    StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, commandStationPoint, pFaction, placementOffsetScale, "Engineer", -750, 400 );
                    StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, commandStationPoint, pFaction, placementOffsetScale, "Engineer", -600, 400 );
                }
                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeFactory )
                    StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, commandStationPoint, pFaction, placementOffsetScale, "Factory", 700, 0 );

                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeHumanSettlement )
                {
                    int numberOfHomeHumanSettlements = factionConfig.GetIntValueForCustomFieldOrDefaultValue( "HomeHumanSettlementsToStartWith", true );

                    for ( int index = 0; index < numberOfHomeHumanSettlements; index++ )
                        StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, commandStationPoint, pFaction, placementOffsetScale, "HomeHumanSettlement", -1000 + (200 * index), -400 );
                }

                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeHumanCryogenicPods )
                {
                    int numberOfHumanCryoPods = factionConfig.GetIntValueForCustomFieldOrDefaultValue( "HumanCryogenicPodsToStartWith", true );
                    int cryoPodsSoFarThisRow = 0;
                    int cryoPodYoffsetDistanceMax = -600;
                    for ( int index = 0; index < numberOfHumanCryoPods; index++ )
                    {
                        if ( cryoPodsSoFarThisRow >= 10 )
                        {
                            cryoPodsSoFarThisRow = 0;
                            cryoPodYoffsetDistanceMax -= 120;
                        }
                        StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, commandStationPoint, pFaction, placementOffsetScale, "HumanCryogenicPod", -1000 + (120 * cryoPodsSoFarThisRow), cryoPodYoffsetDistanceMax );
                        cryoPodsSoFarThisRow++;
                    }
                }
                debugIndex = 2000;
                int innerSystemMinimumRadius = (StartingPlanet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 150 )).IntValue;
                int innerSystemMaximumRadius = (StartingPlanet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 300 )).IntValue;

                debugIndex = 2100;

                if ( !World_AIW2.Instance.GetIsTutorial() ) //test ships only seed on the human homeworld now in non-tutorials.
                {
                    IList<GameEntityTypeData> testShipDatas = GameEntityTypeDataTable.Instance.RowsByRollup[EntityRollupType.PlayerTestShip];
                    for ( int j = 0; j < testShipDatas.Count; j++ )
                    {
                        GameEntityTypeData testShipData = testShipDatas[j];
                        //int distanceToTestUnit = Context.RandomToUse.Next( innerSystemMinimumRadius, innerSystemMaximumRadius );
                        ArcenPoint otherPoint = Engine_AIW2.Instance.CombatCenter.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                        GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, testShipData, testShipData.MarkFor( pFaction ),
                            pFaction.FleetUsedAtPlanet, 0, otherPoint, Context, "HumanEmpireStartSpawn" );
                    }
                }
                debugIndex = 3000;
                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeInitialCombatFleet )
                {
                    FleetDesignTemplate initialPlayerFleet = FleetDesignTemplateTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( "StartingFleet", false ) );
                    if ( TutorialPlanetOrNull != null && TutorialPlanetOrNull.InitialPlayerCombatFleet != null )
                        initialPlayerFleet = TutorialPlanetOrNull.InitialPlayerCombatFleet;
                    if ( initialPlayerFleet == null ) //the "Random" name won't be found, so this will suffice to say "or random!"
                    {
                        //if was null or random, choose one at random.
                        retry:
                        initialPlayerFleet = (FleetDesignTemplate)FleetDesignTemplateTable.Instance.InitialPlayerFleetsAsBaseRows[Engine_Universal.PermanentQualityRandom.Next( 0, FleetDesignTemplateTable.Instance.InitialPlayerFleetsAsBaseRows.Count )];
                        if ( initialPlayerFleet.WeightInDrawBags == 0 )
                            goto retry;
                    }

                    FleetItem centerpiece = initialPlayerFleet.GetCategory( FleetItemDrawBagCategory.Centerpiece ).DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( centerpiece == null || centerpiece.TypeData == null )
                        Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null centerpiece found in InitialPlayerFleet " + initialPlayerFleet.InternalName );
                    else
                    {
                        debugIndex = 3500;
                        ArcenPoint entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                        entityPoint = StartingPlanet.GetSafePlacementPoint_SpecificPoint( Context, centerpiece.TypeData, entityPoint, 200, 1000 );
                        GameEntity_Squad actualCenterpiece = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, centerpiece.TypeData, 1, null, 0, entityPoint, Context, "HumanEmpireStartSpawn" );
                        if ( actualCenterpiece == null )
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null actualCenterpiece found in InitialPlayerFleet FleetItem " + centerpiece.TypeData.InternalName );
                        else
                        {
                            Fleet centerpieceFleet = actualCenterpiece.GetFleetOrNull_Safe();
                            if ( centerpieceFleet != null )
                            {
                                centerpieceFleet.ClearAllMembershipsExceptForCenterpiece();
                                centerpieceFleet.SetAllMembershipsUpFromDesignTemplates_HostOnly( initialPlayerFleet, FleetDesignLogic.Unused, null );

                                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                                command.RelatedIntegers.Add( centerpieceFleet.FleetID ); //FleetID
                                command.RelatedIntegers.Add( PlayerAccount.Local.PlayerPrimaryKeyID );
                                command.RelatedString = "ToggleIsFleetOnPlayerWatchlist";
                                command.RelatedBool = true;
                                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                            }
                            centerpieceFleet.NameRaw = initialPlayerFleet.DisplayName;
                        }
                    }
                }

                bool isExpertMode = World_AIW2.Instance.CampaignType.HarshnessRating >= 700;

                debugIndex = 4000;
                //Battlestation1
                bool wasRandomBattle1 = false;
                FleetDesignTemplate itemToAvoidForBattle2 = null;
                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeInitialBattlestation )
                {
                    FleetDesignTemplate initialPlayerBattlestation1 = FleetDesignTemplateTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( "StartingBattlestation1", false ) );
                    if ( initialPlayerBattlestation1 == null ) //the "Random" name won't be found, so this will suffice to say "or random!"
                    {
                        wasRandomBattle1 = true;
                        //if was null or random, choose one at random.
                        retry:
                        initialPlayerBattlestation1 = (FleetDesignTemplate)FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows[Engine_Universal.PermanentQualityRandom.Next( 0, FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows.Count )];
                        if ( initialPlayerBattlestation1.WeightInDrawBags == 0 )
                            goto retry;
                    }
                    if ( TutorialPlanetOrNull != null && TutorialPlanetOrNull.InitialPlayerBattlestationFleet != null )
                        initialPlayerBattlestation1 = TutorialPlanetOrNull.InitialPlayerBattlestationFleet;

                    if ( !wasRandomBattle1 )
                    {
                        //if they choose the turtle option, then yell at them
                        if ( isExpertMode && initialPlayerBattlestation1.InternalName == "Mod_TurtleDefenses" )
                        {
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Whoah, apologies!  The starting battlestation of " + initialPlayerBattlestation1.DisplayName + " is too powerful.  Please choose another." );
                            return false;
                        }
                    }
                    else
                    {
                        if ( isExpertMode ) //if they were given the turtle option randomly, give them something else in expert mode
                        {
                            while ( initialPlayerBattlestation1.InternalName == "Mod_TurtleDefenses" || initialPlayerBattlestation1.WeightInDrawBags == 0)
                                initialPlayerBattlestation1 = (FleetDesignTemplate)FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows[Engine_Universal.PermanentQualityRandom.Next( 0, FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows.Count )];
                        }
                    }

                    itemToAvoidForBattle2 = initialPlayerBattlestation1;

                    FleetItem centerpiece = initialPlayerBattlestation1.GetCategory( FleetItemDrawBagCategory.Centerpiece ).DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( centerpiece == null || centerpiece.TypeData == null )
                        Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null centerpiece found in InitialPlayerBattlestation " + initialPlayerBattlestation1.InternalName );
                    else
                    {
                        debugIndex = 4500;
                        ArcenPoint entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                        entityPoint = StartingPlanet.GetSafePlacementPoint_SpecificPoint( Context, centerpiece.TypeData, entityPoint, 200, 1000 );
                        GameEntity_Squad actualCenterpiece = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, centerpiece.TypeData, 1, null, 0, entityPoint, Context, "HumanEmpireStartSpawn" );
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

                debugIndex = 5000;
                //Battlestation2
                if ( TutorialPlanetOrNull == null && !isExpertMode ) //aka "don't give me a second battlestation in expert mode!"
                {
                    FleetDesignTemplate initialPlayerBattlestation2 = FleetDesignTemplateTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( "StartingBattlestation2", false ) );
                    bool wasRandomBattle2 = false;
                    if ( initialPlayerBattlestation2 == null || initialPlayerBattlestation2 == itemToAvoidForBattle2 ) //the "Random" name won't be found, so this will suffice to say "or random!"
                    {
                        wasRandomBattle2 = true;
                        //if was null or random, choose one at random.
                        retry:
                        initialPlayerBattlestation2 = (FleetDesignTemplate)FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows[Engine_Universal.PermanentQualityRandom.Next( 0, FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows.Count )];
                        if ( initialPlayerBattlestation2.WeightInDrawBags == 0 )
                            goto retry;
                    }
                    int loopCount = 0;
                    while ( (wasRandomBattle2 || wasRandomBattle1) && initialPlayerBattlestation2 == itemToAvoidForBattle2 && loopCount++ < 1000 )
                    {
                        //if either was random, make sure that the two are not identical
                        retry:
                        initialPlayerBattlestation2 = (FleetDesignTemplate)FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows[Engine_Universal.PermanentQualityRandom.Next( 0, FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows.Count )];
                        if ( initialPlayerBattlestation2.WeightInDrawBags == 0 )
                            goto retry;
                    }

                    FleetItem centerpiece = initialPlayerBattlestation2.GetCategory( FleetItemDrawBagCategory.Centerpiece ).DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( centerpiece == null || centerpiece.TypeData == null )
                        Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null centerpiece found in InitialPlayerBattlestation " + initialPlayerBattlestation2.InternalName );
                    else
                    {
                        debugIndex = 5500;
                        ArcenPoint entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                        entityPoint = StartingPlanet.GetSafePlacementPoint_SpecificPoint( Context, centerpiece.TypeData, entityPoint, 200, 1000 );
                        GameEntity_Squad actualCenterpiece = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, centerpiece.TypeData, 1, null, 0, entityPoint, Context, "HumanEmpireStartSpawn" );
                        if ( actualCenterpiece == null )
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null actualCenterpiece found in InitialPlayerBattlestation FleetItem " + centerpiece.TypeData.InternalName );
                        else
                        {
                            Fleet centerpieceFleet = actualCenterpiece.GetFleetOrNull_Safe();
                            if ( centerpieceFleet != null )
                            {
                                centerpieceFleet.ClearAllMembershipsExceptForCenterpiece();
                                centerpieceFleet.SetAllMembershipsUpFromDesignTemplates_HostOnly( initialPlayerBattlestation2, FleetDesignLogic.Unused, null );
                            }
                            centerpieceFleet.NameRaw = initialPlayerBattlestation2.DisplayName;
                        }
                    }
                }

                debugIndex = 6000;
                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeInitialSupportFleet )
                {
                    FleetDesignTemplate initialPlayerSupportFleet = FleetDesignTemplateTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( "StartingSupportFleet", false ) );
                    if ( initialPlayerSupportFleet == null ) //the "Random" name won't be found, so this will suffice to say "or random!"
                    {
                        //if was null or random, choose one at random.
                        retry:
                        initialPlayerSupportFleet = (FleetDesignTemplate)FleetDesignTemplateTable.Instance.InitialPlayerSupportFleetsAsBaseRows[Engine_Universal.PermanentQualityRandom.Next( 0, FleetDesignTemplateTable.Instance.InitialPlayerSupportFleetsAsBaseRows.Count )];
                        if ( initialPlayerSupportFleet.WeightInDrawBags == 0 )
                            goto retry;
                    }
                    if ( TutorialPlanetOrNull != null && TutorialPlanetOrNull.InitialPlayerSupportFleet != null )
                        initialPlayerSupportFleet = TutorialPlanetOrNull.InitialPlayerSupportFleet;

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
            }
            catch ( Exception e)
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "NormalHumanEmpireSpecificCodeDeepInfo SeedUnitsOnStartingPlanetDuringMapGen error at debugIndex " + debugIndex + ": " + e );
                return false;
            }
            return true;
        }
    }
}
