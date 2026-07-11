using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class HumanArkEmpireFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        protected bool CanClaimFreePlanet;

        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            base.SerializeFactionTo( MetaData, Buffer, SerializationCmdType );
            Buffer.AddBool( MetaData, CanClaimFreePlanet );
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            base.DeserializeFactionIntoSelf( MetaData, Buffer, SerializationCmdType );
            CanClaimFreePlanet = Buffer.ReadBool( MetaData );
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            CanClaimFreePlanet = false;
        }
        
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( Context );

            #region CanClaimFreePlanet Handling
            if ( !CanClaimFreePlanet )
                return;
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( EntityRollupType.MobileFleetFlagships ) )
            {
                Fleet fleet = entity.GetFleetOrNull_Safe();
                if ( fleet != null && !(fleet.BaseInfo is HumanMobileFleetBaseInfo) )
                    fleet.CreateExternalBaseInfo<HumanMobileFleetBaseInfo>( "HumanMobileFleetBaseInfo" );
            }

            GameEntity_Squad command = AttachedFaction.GetFirstMatching( EntityRollupType.CommandStation, true, true );
            if ( command != null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( $"你获得了一个指挥站。AI 现在完全意识到了你的存在，你将不再能免费占领星球。", ChatType.LogToCentralChat, null );
                foreach ( Planet workingPlanet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( workingPlanet.GetIsControlledByFactionType( FactionType.Player ) || command.Planet == workingPlanet )
                        continue;

                    PlanetFaction pFaction = workingPlanet.GetPlanetFactionForFaction( AttachedFaction );

                    pFaction.AIPLeftFromCommandStation = ExternalConstants.Instance.Balance_CommandStationAIP;
                    pFaction.AIPLeftFromWarpGate = ExternalConstants.Instance.Balance_WarpGateAIP;
                }
                CanClaimFreePlanet = false;
            }
            else if ( FactionUtilityMethods.Instance.GetCurrentAIP() > 20 )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( $"你的 AIP 已超过 20。AI 现在完全意识到了你的存在，你将不再能免费占领星球。", ChatType.LogToCentralChat, null );
                foreach ( Planet workingPlanet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( workingPlanet.GetIsControlledByFactionType( FactionType.Player ) )
                        continue;

                    PlanetFaction pFaction = workingPlanet.GetPlanetFactionForFaction( AttachedFaction );

                    pFaction.AIPLeftFromCommandStation = ExternalConstants.Instance.Balance_CommandStationAIP;
                    pFaction.AIPLeftFromWarpGate = ExternalConstants.Instance.Balance_WarpGateAIP;
                }
                CanClaimFreePlanet = false;
            }
            #endregion
        }

        public override void DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly( 
            ref int debugStage, GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, Faction factionThatKilledEntity, Faction entityOwningFaction, int numExtraStacksKilled, ArcenHostOnlySimContext Context )
        {
            base.DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly( ref debugStage, entity, Damage, FiringSystemOrNull, factionThatKilledEntity, entityOwningFaction, numExtraStacksKilled, Context );

            HumanFactionSharedDeep.CheckAndHandleStationDeath( AttachedFaction, ref debugStage, entity, Damage, FiringSystemOrNull, factionThatKilledEntity, entityOwningFaction, numExtraStacksKilled, Context );

            #region CanClaimFreePlanet Handling
            if ( !CanClaimFreePlanet )
                return;

            if ( entity == null || factionThatKilledEntity == null || entityOwningFaction == null )
                return; // Sanity checks.

            if ( factionThatKilledEntity.Type != FactionType.Player )
                return;

            if ( entity.TypeData.SpecialType == SpecialEntityType.AICommandStationOriginal || entity.TypeData.GetHasTag( "WarpGate" ) )
            {
                CanClaimFreePlanet = false;

                // Purge the other structure from the planet to avoid cheesing.
                GameEntity_Squad toRemove = entity.Planet.GetFirstMatching( FactionType.AI, SpecialEntityType.AICommandStationOriginal, true, true );
                if ( toRemove != null )
                    toRemove.Die( Context, true, FiringSystemOrNull );
                toRemove = entity.Planet.GetFirstMatching( FactionType.AI, "WarpGate", true, true );
                if ( toRemove != null )
                    toRemove.Die( Context, true, FiringSystemOrNull );

                // Only occurs once; reset all AIPLeft values for this faction.
                foreach ( Planet workingPlanet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( workingPlanet.GetIsControlledByFactionType( FactionType.Player ) || entity.Planet == workingPlanet )
                        continue;

                    // AI Homeworld planets do not cost 15 more aip to build on after already killing the overlord.
                    if (workingPlanet.PopulationType == PlanetPopulationType.AIHomeworld)
                        continue;

                    PlanetFaction pFaction = workingPlanet.GetPlanetFactionForFaction( AttachedFaction );

                    pFaction.AIPLeftFromCommandStation = ExternalConstants.Instance.Balance_CommandStationAIP;
                    pFaction.AIPLeftFromWarpGate = ExternalConstants.Instance.Balance_WarpGateAIP;
                }
                World_AIW2.Instance.QueueChatMessageOrCommand( $"You, or one of your allies, have claimed a Planet before the AI was even truly aware of you. Needless to say, that's the only freebie you'll get.", ChatType.LogToCentralChat, null );
            }
            #endregion
        }

        public override bool SeedUnitsOnStartingPlanetDuringMapGen( Planet StartingPlanet, ConfigurationForFaction factionConfig, PlanetFaction pFaction, 
            ref ArcenPoint commandStationPoint, ref bool stillNeedsToSeedHumanHomeworldStuff, 
            Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull, ArcenHostOnlySimContext Context )
        {
            bool noerror = base.SeedUnitsOnStartingPlanetDuringMapGen( StartingPlanet, factionConfig, pFaction, ref commandStationPoint, ref stillNeedsToSeedHumanHomeworldStuff, TutorialPlanetOrNull, Context );
            if (noerror == false)
                return false;

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

                GameEntityTypeData humanKingUnitData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( "StartingArk", false ) );
                if ( humanKingUnitData == null ) //the "Random" name won't be found, so this will suffice to say "or random!"
                {
                    //if was null or random, choose one at random.
                    retry:
                    humanKingUnitData = (GameEntityTypeData)GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "HumanArkEmpireArks" );
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
                    string tagAndfieldName = "ArkEmpireOptionGroup" + i;
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

        protected override int MinimumSecondsBetweenLongRangePlannings => 5;

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            FleetBehaviorLRP.DoLRP( AttachedFaction, Context );
        }
    }
}
