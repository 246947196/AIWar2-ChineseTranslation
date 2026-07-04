using Arcen.Universal;
using System;
using System.Diagnostics;
using Arcen.AIW2.Core;
using UnityEngine;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// All of the things here are run on a secondary thread, 
    /// from in the World_AIW2.Instance.DoWorldStepLogic tree,
    /// in the last secondary-thread step of the sim before SimPlannerImplementation 
    /// (on the main thread) can flip to the next sim-frame.
    /// 
    /// This gets run once per entity.
    /// </summary>
    public class EntitySimLogicImplementation_BaseInfo : EntitySimLogicBaseInfo
    {
        public override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            workingWithinRangeForAssister.Clear( true ); //not static, so yay
            WorkingShipGroups.Clear(); //static

            //these two don't matter much
            WorkingShipGroups.Clear();

            Accumulator_BLDC = 0;
            Accumulator_GPLA = 0;
            Accumulator_WTIM = 0;
            Accumulator_WSEC = 0;
            Accumulator_FLC = 0;
            Accumulator_FAC = 0;
            Accumulator_WOR = 0;
            Accumulator_PLA = 0;
            Accumulator_COM = 0;
            Accumulator_REM = 0;
            Accumulator_SAI = 0;
            Accumulator_WSUF = 0;
            Accumulator_BLDF = 0;
            Accumulator_NET = 0;

            Accumulator_CSPB = 0;
            Accumulator_CSPF = 0;
            Accumulator_CCSB = 0;
            Accumulator_CCSF = 0;
            Accumulator_CSIB = 0;
            Accumulator_CSIF = 0;
            Accumulator_CSOB = 0;
            Accumulator_CSOF = 0;
        }

        public static int Accumulator_BLDC = 0;
        public static int Accumulator_GPLA = 0;
        public static int Accumulator_WTIM = 0;
        public static int Accumulator_WSEC = 0;
        public static int Accumulator_FLC = 0;
        public static int Accumulator_FAC = 0;
        public static int Accumulator_WOR = 0;
        public static int Accumulator_PLA = 0;
        public static int Accumulator_COM = 0;
        public static int Accumulator_REM = 0;
        public static int Accumulator_SAI = 0;
        public static int Accumulator_WSUF = 0;
        public static int Accumulator_BLDF = 0;
        public static int Accumulator_NET = 0;

        public static int Accumulator_CSPB = 0;
        public static int Accumulator_CSPF = 0;
        public static int Accumulator_CCSB = 0;
        public static int Accumulator_CCSF = 0;
        public static int Accumulator_CSIB = 0;
        public static int Accumulator_CSIF = 0;
        public static int Accumulator_CSOB = 0;
        public static int Accumulator_CSOF = 0;

        public EntitySimLogicImplementation_BaseInfo()
        {
            EntitySimLogicBaseInfo.Instance = this;
        }

        private bool _trace;
        private ArcenCharacterBuffer traceBuffer;

        #region ReevaluateUnitOrders
        public override void ReevaluateUnitOrders( ArcenClientOrHostSimContextCore Context, GameEntity_Squad Entity )
        {
            if ( World.Instance.IsPaused )
                return; //don't waste my time, buddy!
            
            if (Entity == null || Entity.Planet == null || Entity.FleetMembership == null)
                return;
            
            EntityBehaviorType effectiveBehavior = EntityBehaviorType.None;
            int debugStage = 0;
            try
            {
                debugStage = 1;
                if ( Entity.Planet.BattleStatus == PlanetBattleStatus.Tier1_PlayerLookingAtMe )
                {
                    debugStage = 2;
                    //only run the sim cycle groups on the main planet; otherwise we may miss sim cycles entirely
                    if ( World_AIW2.CurrentSimCycleSlow != Entity.SimCycleGroup_Slow )
                        return; //catch you next time!
                }
                
                debugStage = 3;
                
                if ( CentralVars.DEBUG_TURN_OFF_RE_EVAL_UNIT_ORDERS )
                    return;
                
                debugStage = 4;
                
                if ( Entity.TypeData.IsFleetLeader )
                {
                    debugStage = 5;
                    if ( Entity.CalculateShouldOrderBeIgnoredByFlagshipAsIfWasStationaryMode( false ) ) //don't send inverter when checking this
                        return; //not sure how much this does, but might keep them out of pursuit mode, etc
                }

                debugStage = 6;
                
                if ( Entity.PrimaryKeyID == -17 )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Oi!  Tried to run ReevaluateUnitOrders on the FakeEntity!  How did this even get into the game?  Faction: " +
                        Entity.GetFactionDisplayNameSafe() + " typeData: " + Entity.GetTypeDisplayNameSafe() + " planet: " + Entity.GetPlanetNameSafe(), Verbosity.ShowAsError );

                debugStage = 10;
                FleetMembership fleetMem = Entity.FleetMembership;
                if ( fleetMem == null )
                {
                    //don't complain about this, this is a unit that just died.
                    return;
                }

                debugStage = 20;
                PlanetFaction pFaction = Entity.PlanetFaction;
                if ( pFaction == null )
                {
                    //don't try to give orders to things that have no fleet membership
                    //complain visually about the lack of fleet membership unless MP client.
                    //if no fleet membership on the client, it will be fixed soon by sync.  Otherwise the host will error and let us know.
                    if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                        ArcenDebugging.ArcenDebugLog( "Blank PlanetFaction for ship of type " + Entity.TypeData.DisplayName + "!", Verbosity.ShowAsError );
                    return;
                }
                debugStage = 30;
                Faction faction = pFaction.Faction;
                if ( faction == null )
                {
                    //don't try to give orders to things that have no fleet membership
                    //complain visually about the lack of fleet membership unless MP client.
                    //if no fleet membership on the client, it will be fixed soon by sync.  Otherwise the host will error and let us know.
                    if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                        ArcenDebugging.ArcenDebugLog( "Blank Faction for ship of type " + Entity.TypeData.DisplayName + "!", Verbosity.ShowAsError );
                    return;
                }

                debugStage = 40;
                GameEntityTypeData.MarkLevelStats entityMarkData = Entity.DataForMark;
                if ( entityMarkData == null )
                {
                    //don't try to give orders to things that have no fleet membership
                    //complain visually about the lack of fleet membership unless MP client.
                    //if no fleet membership on the client, it will be fixed soon by sync.  Otherwise the host will error and let us know.
                    if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                        ArcenDebugging.ArcenDebugLog( "Blank DataForMark for ship of type " + Entity.TypeData.DisplayName + "!", Verbosity.ShowAsError );
                    return;
                }


                EntityOrderCollection entityOrders = Entity.Orders;
                if ( entityOrders == null )
                {
                    if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                        ArcenDebugging.ArcenDebugLog( "Blank Entity.Orders for ship of type " + Entity.TypeData.DisplayName + "!", Verbosity.ShowAsError );
                    return;
                }

                debugStage = 1000;

                #region tracing
                _trace = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.IndividualLogic ) && Entity == GameEntity_Base.CurrentlyHoveredOver;
                traceBuffer = _trace? ArcenCharacterBuffer.GetFromPoolOrCreate( "EntitySimLogicImpl-ReevaluateUnitOrders-trace", 10f ) : null;
                if ( _trace ) traceBuffer.Add( "ReevaluateUnitOrders: " ).Add( Entity.TypeData.InternalName ).Add( " " ).Add( Entity.PrimaryKeyID );
                #endregion

                debugStage = 1100;

                #region tracing
                if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "#start Orders.Behavior=" ).Add( EnumNameCache.GetName( Entity.Orders.Behavior ) );
                #endregion

                #region Self Building Checks
                if ( Entity.SelfBuildingMetalRemaining.RawValue > 0 )
                {
                    debugStage = 1200;
                    //if this isn't a player ship, we never self-build anyhow.
                    if ( faction.Type != FactionType.Player )
                        Entity.SelfBuildingMetalRemaining = FInt.Zero;
                    else //okay, this does belong to the player
                    {
                        //this belongs to the player, but was told to self-build and is not a self-builder, then stop it!
                        //may have died to remains and come back, or similar.  Handle it with regular repairs.
                        if ( !entityMarkData.GetMetalFlow( MetalFlowPurpose.SelfConstruction ).HasRealData )
                            Entity.SelfBuildingMetalRemaining = FInt.Zero;
                    }
                }
                #endregion

                #region Suicide Checks For Ships After Non Human Teams
                if ( Entity.IsAfterANonHumanTeam_NonSim && ArcenNetworkAuthority.GetIsHostMode() )
                {
                    Int16 factionIndexTargeting = Entity.CalculateFactionIndexIsChasingInsteadOfHumans();
                    //-1 = unset
                    //-2 = a whole category of factions
                    if ( factionIndexTargeting >= 0 || factionIndexTargeting == -2 )
                    {
                        bool shouldBeKeptAlive = true;
                        if ( Entity.GetSpecialFactionIsConsideredHunter_Safe() &&
                            factionIndexTargeting >= 0 )
                        {
                            Faction targetedFaction = World_AIW2.Instance.GetFactionByIndex( factionIndexTargeting );
                            if ( targetedFaction != null && targetedFaction.SpecialFactionData.HunterTargetingThisFactionDiesImmediately )
                                shouldBeKeptAlive = false;
                        }

                        //bool isLikelyToBehave = true;
                        //if ( Entity.FireteamSpecificationOrNull == null || !Entity.FireteamSpecificationOrNull.IsActive() )
                        //    isLikelyToBehave = false;
                        //else
                        //{
                        //    if ( Entity.FireteamId < 0 )
                        //        isLikelyToBehave = false;
                        //    else
                        //    {
                        //        Fireteam team = null;
                        //        ExternalFactionBaseInfo factionBaseInfo = Entity.GetFactionBaseInfoOrNull_Safe();
                        //        if ( factionBaseInfo != null )
                        //            team = (Fireteam)factionBaseInfo.GetFireteamBaseById( Entity.FireteamId );
                        //        if ( team == null || team.SpecificationOrNull == null || !team.SpecificationOrNull.IsActive() )
                        //            isLikelyToBehave = false;
                        //    }
                        //}

                        if ( ( Entity.GetSpecialFactionIsConsideredWarden_Safe() ||
                            Entity.GetSpecialFactionIsConsideredPraetorian_Safe()  )&&
                            factionIndexTargeting >= 0 )
                        {
                            Planet currentPlanet = Entity.Planet;
                            if ( currentPlanet != null )
                            {
                                bool foundOurTargetAroundHere = false;
                                foreach ( Planet p in currentPlanet.LinkedNeighborsAndSelf( false ) )
                                {
                                    if ( p == null )
                                        continue;
                                    if ( p.UnderInfluenceOfFactionIndex.Contains( factionIndexTargeting ) )
                                    {
                                        foundOurTargetAroundHere = true;
                                        break;
                                    }
                                    if ( p.Factions[factionIndexTargeting].Entities.SquadCount > 0 )
                                    {
                                        foundOurTargetAroundHere = true;
                                        break;
                                    }
                                }
                                if ( !foundOurTargetAroundHere )
                                    shouldBeKeptAlive = false;
                            }
                        }
                        if ( !shouldBeKeptAlive )
                        {
                            int unitCount = 1 + Entity.ExtraStackedSquadsInThis;
                            int unitStrength = Entity.GetStrengthOfSelfAndContents();
                            World_AIW2.Instance.KilledBecauseChasingAFactionWeAreTooFarFrom_Count += unitCount;
                            World_AIW2.Instance.KilledBecauseChasingAFactionWeAreTooFarFrom_Strength += unitStrength;

                            Faction facOrNull = Entity.GetFactionOrNull_Safe();
                            if ( facOrNull != null )
                            {
                                facOrNull.KilledBecauseChasingAFactionWeAreTooFarFrom_Count += unitCount;
                                facOrNull.KilledBecauseChasingAFactionWeAreTooFarFrom_Strength += unitStrength;
                            }

                            Entity.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.KilledBecauseChasingAFactionWeAreTooFarFrom );

                            //ArcenDebugging.ArcenDebugLogSingleLine( "Kill " + Entity.GetFactionDisplayNameSafe() + " " + Entity.TypeData.GetDisplayName() + " that was after faction " +
                            //    (factionIndexTargeting < 0 ? "a group of factions" :
                            //    World_AIW2.Instance.Factions[factionIndexTargeting].GetDisplayName() ) + " but not in a fireteam targeting them, or something to that effect.", Verbosity.DoNotShow );
                        }
                    }
                    else if ( factionIndexTargeting == -2 ) //a whole category of factions
                    {
                        //Chris says: to get here, the fireteam targeting must have already been present
                        //if ( Entity.FireteamSpecificationOrNull == null )
                        //{
                        //    Entity.FireteamSpecificationOrNull = new FireteamRequiredTarget();
                        //    Entity.FireteamSpecificationOrNull.FactionIdx = factionIndexTargeting;
                        //}
                    }
                }
                #endregion

                debugStage = 1500;

                GameEntity_Squad guarded = Entity.GuardedUnit.GetSquad();
                debugStage = 1600;
                //if this is not our faction type anymore, stop guarding!
                if ( guarded != null && guarded.GetFactionTypeSafe() != faction.Type )
                {
                    debugStage = 1700;
                    guarded = null;
                    Entity.GuardOrPatrolOffsetPoints.Clear();
                    Entity.GuardedUnit.Clear();
                }
                debugStage = 2000;

                EntityOrder order = Entity.RemoveInvalidatedOrdersAndReturnFirstValid_ThatIsNotDecollision( true );
                debugStage = 2100;

                if ( order.TypeData != null )
                {
                    debugStage = 2110;
                    switch ( order.TypeData.Type )
                    {
                        case EntityOrderType.Unload_Transport:
                            {
                                Fleet fleet = Entity.GetFleetOrNull_Safe();
                                if ( fleet != null )
                                    fleet.IsFleetInTransportLoadMode = false; //switch to unload mode!
                            }
                            return;
                        case EntityOrderType.SetBehavior_Stationary:
                            Entity.GetEffectiveOrders().SetBehaviorDirectlyInSim( EntityBehaviorType.Stationary );
                            return;
                        case EntityOrderType.SetBehavior_Attacker_Full:
                            Entity.GetEffectiveOrders().SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                            return;
                        case EntityOrderType.SetBehavior_Attacker_PursueOnlyInRange:
                            Entity.GetEffectiveOrders().SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_PursueOnlyInRange );
                            return;
                        case EntityOrderType.SetBehavior_StopToShootAnySeenTargets_On:
                            Entity.StopToShootAnySeenTargets = true;
                            return;
                        case EntityOrderType.SetBehavior_StopToShootAnySeenTargets_Off:
                            Entity.StopToShootAnySeenTargets = false;
                            return;
                        case EntityOrderType.Custom:
                            order.CustomOrder.Evaluate(Context, ref order);
                            return;
                    }
                }

                effectiveBehavior = Entity.GetEffectiveOrders().Behavior;
                debugStage = 2200;
                #region tracing
                if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "#2 effectiveBehavior = Entity.GetEffectiveOrders().Behavior=" ).Add( EnumNameCache.GetName( effectiveBehavior ) );
                #endregion

                debugStage = 2300;
                //"implicit melee attacker with order from human, suppressing guard offset to avoid going back to the remembered point after the order
                if ( Entity.GuardedUnit.GetSquad() == null &&
                     Entity.GuardOrPatrolOffsetPoints.Count > 0 &&
                     order.TypeData != null &&
                     (order.Source == OrderSource.HumanPlayer || order.TypeData.Type == EntityOrderType.GetIntoTransport) &&
                     GetShouldEntityUseImplicitMeleeAttackerBehavior( Entity, faction, entityOrders ) )
                {
                    debugStage = 2400;
                    Entity.GuardOrPatrolOffsetPoints.Clear();
                    #region tracing
                    if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "implicit melee attacker with order from human, suppressing guard offset to avoid going back to the remembered point after the order" );
                    #endregion
                }
                debugStage = 2500;

                if ( effectiveBehavior != EntityBehaviorType.Attacker_PursueOnlyInRange )
                {
                    debugStage = 2600;
                    if ( order.TypeData != null && order.ShouldOverrideBehavior )
                    {
                        #region tracing
                        if ( _trace )
                        {
                            if ( order.TypeData.Type == EntityOrderType.Wormhole )
                            {
                                traceBuffer.Add( "Obeying order type " + order.TypeData.Type + " to " + World_AIW2.Instance.GetPlanetByIndex( order.RelatedPlanetIndex ).Name + " which overrides the current behaviour" );
                            }
                            else
                                traceBuffer.Add( "Obeying order type " + order.TypeData.Type + " which overrides the current behaviour" );

                            ArcenDebugging.ArcenDebugLogSingleLine( traceBuffer.ToString(), Verbosity.DoNotShow );
                            traceBuffer.ReturnToPool();
                            traceBuffer = null;
                        }
                        #endregion
                        return;
                    }
                    debugStage = 2700;

                    //we rely on this to block us from accidentally ordering around things that are already given orders by humans.
                    //that way we can use ClearSource.YesClearHumanOrders, below
                    if ( order.TypeData != null && order.Source == OrderSource.HumanPlayer )
                    {
                        #region tracing
                        if ( _trace )
                        {
                            traceBuffer.Add( "Obeying order from player which overrides behaviour" );
                            ArcenDebugging.ArcenDebugLogSingleLine( traceBuffer.ToString(), Verbosity.DoNotShow );
                            traceBuffer.ReturnToPool();
                            traceBuffer = null;
                        }
                        #endregion
                        return;
                    }
                }
                debugStage = 3000;

                if ( GetShouldEntityUseImplicitMeleeAttackerBehavior( Entity, faction, entityOrders ) )
                {
                    debugStage = 3100;
                    effectiveBehavior = EntityBehaviorType.Attacker_PursueOnlyInRange;
                    debugStage = 3200;
                    //if ( Entity.Guarding.Ref == null && Entity.GuardingOffsets.Count <= 0 )
                    //{
                    //    Entity.GuardingOffsets.Add( Entity.WorldLocation ); // so the melee unit returns to its original place when done
                    //    #region tracing
                    //    if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "implicit melee attacker, adding guard offset to return to" );
                    //    #endregion
                    //}
                    #region tracing
                    if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "effectiveBehavior = " ).Add( EnumNameCache.GetName( effectiveBehavior ) ).Add( " because Entity.TypeData.MeleeRange && effectiveBehavior == EntityBehaviorType.None && !Entity.IsInHoldFireMode && pFaction != null && faction.Type == FactionType.Player" );
                    #endregion
                }

                debugStage = 4000;
                switch ( effectiveBehavior )
                {
                    case EntityBehaviorType.Guard_FleetShip:
                    case EntityBehaviorType.Guard_Guardian_Patrolling:
                    case EntityBehaviorType.Guard_Guardian_Anchored:
                        {
                            debugStage = 5000;
                            bool isFreeingFromAggro = false;
                            GameEntity_Squad guardSquad = Entity.GuardedUnit.GetSquad();
                            debugStage = 5100;
                            if ( guardSquad != null )
                            {
                                debugStage = 5200;
                                if ( guardSquad.LastTimeTakenDamageFromBeingShotByAnyone > 0 )
                                {
                                    //aggro me because guard was aggro'd
                                    if ( Entity.LastTimeTakenDamageFromBeingShotByAnyone < guardSquad.LastTimeTakenDamageFromBeingShotByAnyone )
                                        Entity.LastTimeTakenDamageFromBeingShotByAnyone = guardSquad.LastTimeTakenDamageFromBeingShotByAnyone;
                                    isFreeingFromAggro = true;
                                }
                                else if ( Entity.LastTimeTakenDamageFromBeingShotByAnyone > 0 )
                                {
                                    //aggro guard because I was aggro'd
                                    if ( Entity.LastTimeTakenDamageFromBeingShotByAnyone > guardSquad.LastTimeTakenDamageFromBeingShotByAnyone )
                                        guardSquad.LastTimeTakenDamageFromBeingShotByAnyone = Entity.LastTimeTakenDamageFromBeingShotByAnyone;
                                    isFreeingFromAggro = true;
                                }
                            }
                            else if ( Entity.LastTimeTakenDamageFromBeingShotByAnyone > 0 )
                                isFreeingFromAggro = true;

                            debugStage = 5500;
                            if ( isFreeingFromAggro )
                            {
                                entityOrders.ClearOrders( ClearBehavior.AnythingIncludingAttackerMode, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.YesClearAnyOrders_IncludingFromHumans, "isFreeingFromAggro" );
                                Entity.GuardOrPatrolOffsetPoints.Clear();
                                entityOrders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //works fine since part of sim
                                effectiveBehavior = EntityBehaviorType.Attacker_Full;
                            }
                        }
                        break;
                }
                debugStage = 6000;

                switch ( effectiveBehavior )
                {
                    case EntityBehaviorType.Guard_FleetShip:
                        debugStage = 7000;
                        if ( Entity.TypeData.CannotBeStoredInsideGuardPost )
                        {
                            debugStage = 7100;
                            effectiveBehavior = EntityBehaviorType.Attacker_Full;
                            #region tracing
                            if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "acting as Attacker because CannotBeStoredInsideGuardPost" );
                            #endregion
                        }
                        else
                        {
                            debugStage = 7200;
                            if ( faction.Type == FactionType.Player )
                            {
                                debugStage = 7300;
                                if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrengthIncludingNonMilitary > 0 )
                                {
                                    effectiveBehavior = EntityBehaviorType.Attacker_Full;
                                    #region tracing
                                    if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "acting as Attacker because player-side and Hostile is present" );
                                    #endregion
                                }
                            }
                            else
                            {
                                debugStage = 7400;
                                StrengthData_PlanetFaction_Stance hostileData = pFaction.DataByStance[FactionStance.Hostile];
                                if ( hostileData.SecondsSinceHadAnyStrength >= 0 &&
                                     hostileData.SecondsSinceHadAnyStrength < ExternalConstants.Instance.ResidualAlertSeconds )
                                {
                                    effectiveBehavior = EntityBehaviorType.Attacker_Full;
                                    #region tracing
                                    if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "acting as Attacker because non-player-side and player is present (" ).Add( hostileData.SecondsSinceHadAnyStrength ).Add( " seconds ago, residual alert for " ).Add( ExternalConstants.Instance.ResidualAlertSeconds ).Add( " seconds)" );
                                    #endregion
                                }
                            }
                        }
                        break;
                    case EntityBehaviorType.Guard_Guardian_Patrolling:
                        {
                            debugStage = 8000;
                            StrengthData_PlanetFaction_Stance hostileData = pFaction.DataByStance[FactionStance.Hostile];
                            debugStage = 8100;
                            int secondsSinceLastHostilePresence = hostileData.SecondsSinceHadAnyStrength;
                            debugStage = 8200;
                            if ( secondsSinceLastHostilePresence >= 0 &&
                                 secondsSinceLastHostilePresence < ExternalConstants.Instance.ResidualAlertSeconds )
                            {
                                debugStage = 8300;
                                bool isFreeing = false;
                                Int16 freeingAainstFactionIndex = -1;
                                if ( Entity.HullPointsLost > 0 || Entity.ShieldPointsLost > 0 )
                                    isFreeing = true;
                                else
                                {
                                    debugStage = 8400;
                                    // if we need a more sophisticated "am I near any enemies" check than Entity, (for instance, better limiting on the range), then we should probably find some way to offload the bulk of Entity to a planning thread
                                    DelegateHelper_CheckForGuardFreeingProvocation_guardPoint = Entity.WorldLocation;
                                    DelegateHelper_CheckForGuardFreeingProvocation_foundHit = null;
                                    debugStage = 8500;
                                    for ( int i = 0; i < Entity.Planet.Factions.Count; i++ )
                                    {
                                        debugStage = 8600;
                                        PlanetFaction otherFaction = Entity.Planet.Factions[i];
                                        debugStage = 8700;
                                        if ( !pFaction.GetIsHostileTowards( otherFaction ) )
                                            continue;
                                        debugStage = 8800;
                                        foreach ( GameEntity_Squad _gfp_e in otherFaction.Entities.Squads() )
                                            if ( DelegateHelper_CheckForGuardFreeingProvocation( _gfp_e ) == DelReturn.Break ) break;
                                        debugStage = 8900;
                                        if ( DelegateHelper_CheckForGuardFreeingProvocation_foundHit != null )
                                        {
                                            debugStage = 8990;
                                            isFreeing = true;
                                            freeingAainstFactionIndex = DelegateHelper_CheckForGuardFreeingProvocation_foundHit.GetFactionIndex_Safe();
                                            break;
                                        }
                                    }
                                    DelegateHelper_CheckForGuardFreeingProvocation_foundHit = null;
                                }
                                
                                debugStage = 9000;
                                if ( isFreeing )
                                {
                                    debugStage = 9100;
                                    effectiveBehavior = EntityBehaviorType.Attacker_Full;
                                    //if ( hostileData.MobileStrength > FInt.Zero )
                                    {
                                        debugStage = 9200;
                                        entityOrders.ClearOrders( ClearBehavior.AnythingIncludingAttackerMode, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.YesClearAnyOrders_IncludingFromHumans, "isFreeing" );
                                        Entity.GuardOrPatrolOffsetPoints.Clear();
                                        entityOrders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, freeingAainstFactionIndex ); //works fine because sim
                                    }
                                }
                            }
                        }
                        break;
                }
                debugStage = 11000;

                #region tracing
                if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "effectiveBehavior=" ).Add( EnumNameCache.GetName( effectiveBehavior ) );
                #endregion
                debugStage = 11100;

                switch ( effectiveBehavior )
                {
                    // the reason we are calling attackerlogic for 'none'
                    // .. this is how ships with a seek-range greater than their weapon-range
                    //    go out and engage them
                    case EntityBehaviorType.None:
                    case EntityBehaviorType.Stationary:
                        debugStage = 11200;
                        this.AttackerLogic( Context, Entity, order, guarded, false, true, fleetMem, entityMarkData, faction, entityOrders );
                        break;
                    case EntityBehaviorType.Attacker_Full:
                        debugStage = 11200;
                        this.AttackerLogic( Context, Entity, order, guarded, false, false, fleetMem, entityMarkData, faction, entityOrders );
                        break;
                    case EntityBehaviorType.Attacker_PursueOnlyInRange:
                        debugStage = 11300;
                        this.AttackerLogic( Context, Entity, order, guarded, true, true, fleetMem, entityMarkData, faction, entityOrders );
                        break;
                    //case EntityBehaviorType.RallyToPlayerFleet:
                    //    this.FleetRallyLogic( Context, Entity, order );
                    //    break;
                    case EntityBehaviorType.Guard_FleetShip:
                        debugStage = 11400;
                        this.Guard_FleetShipLogic( Context, Entity, order, guarded, fleetMem, entityMarkData, faction, pFaction, entityOrders );
                        break;
                    case EntityBehaviorType.Guard_Guardian_Patrolling:
                        debugStage = 11500;
                        this.Guard_Guardian_PatrollingLogic( Context, Entity, order, guarded, fleetMem, entityMarkData, faction, pFaction, entityOrders );
                        break;
                    case EntityBehaviorType.Guard_Guardian_Anchored:
                        debugStage = 11600;
                        this.Guard_Guardian_AnchoredLogic( Context, Entity, order, guarded, fleetMem, entityMarkData, faction, pFaction, entityOrders );
                        break;
                    default:
                        debugStage = 11700;
                        this.AssisterLogic_Stationary( Context, Entity, order, guarded, fleetMem, entityMarkData, entityOrders );
                        break;
                }
            }
            catch ( Exception e )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    LOG.Err( "exception in ReevaluateUnitOrders at debugstage {0}\n{1}", debugStage, e);
            }
            finally
            {
                #region tracing
                if ( traceBuffer != null )
                {
                    traceBuffer.Add( Environment.NewLine ).Add( "#end effectiveBehavior=" ).Add( EnumNameCache.GetName( effectiveBehavior ) );

                    ArcenDebugging.ArcenDebugLogSingleLine( traceBuffer.ToString(), Verbosity.DoNotShow );
                    traceBuffer.ReturnToPool();
                    traceBuffer = null;
                }
                #endregion
            }
        }

        public bool GetShouldEntityUseImplicitMeleeAttackerBehavior( GameEntity_Squad Entity, Faction faction, EntityOrderCollection entityOrders )
        {
            return false;
            /*
            return Entity.GetHasAMeleeWeaponRightNow() &&
                   (entityOrders.Behavior == EntityBehaviorType.None || entityOrders.Behavior == EntityBehaviorType.Stationary ) &&
                   !Entity.IsInHoldFireMode &&
                   faction.Type == FactionType.Player;
            */
        }
        #endregion

        #region AttackerLogic
        private void AttackerLogic( ArcenClientOrHostSimContextCore Context, GameEntity_Squad Entity, EntityOrder order, GameEntity_Squad guarded, 
            bool AllowOverridingHumanOrders, bool OnlyInRange, FleetMembership fleetMem, GameEntityTypeData.MarkLevelStats entityMarkData, Faction faction, 
            EntityOrderCollection entityOrders )
        {
            if ( guarded != null && Entity.TypeData.IsReinforcementLocation )
            {
                // if we're a guardian, guarding something, but we're in the Attacker mode, then we should cut loose the rest of the way and become real threat
                entityOrders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.YesClearAnyOrders_IncludingFromHumans, "StoppingGuard" );
                guarded = null;
                Entity.GuardOrPatrolOffsetPoints.Clear();
                Entity.GuardedUnit.Clear();

                if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "is a reinforcement location guarding something, so stopping that to attack" );

                return; // next time around we'll revisit the question of what really to do 
            }

            if ( !Entity.IsAllowedToUseMovementModes() ) 
            {
                if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "no new order because !IsAllowedToUseMovementModes" );

                return; //flagships are only allowed to use this logic if the user has requested it
            }

            if ( Entity.GetIsCrippled() ) 
            {
                if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "no new order because is crippled" );

                return;
            }
            if ( Entity.TypeData.HasAnyMetalFlows &&
                 entityMarkData.AssistRange > 0 &&
                 (!Entity.TypeData.IsCombatant || Entity.TypeData.IsCombatantDespiteNoWeapons ) )
            {
                this.AttackerLogic_Assister( Context, Entity, order, guarded, OnlyInRange, fleetMem, entityMarkData, entityOrders );
            }
            else
            {
                this.AttackerLogic_Combat( Context, Entity, order, guarded, AllowOverridingHumanOrders, fleetMem, entityMarkData, faction, entityOrders );
            }
        }
        #endregion

        #region AssisterLogic_Stationary
        private void AssisterLogic_Stationary( ArcenClientOrHostSimContextCore Context, GameEntity_Squad Entity, EntityOrder order, GameEntity_Squad guarded, FleetMembership fleetMem, 
            GameEntityTypeData.MarkLevelStats entityMarkData, EntityOrderCollection entityOrders )
        {
            if ( Entity.TypeData.HasAnyMetalFlows &&
                 entityMarkData.AssistRange > 0 &&
                 (!Entity.TypeData.IsCombatant || Entity.TypeData.IsCombatantDespiteNoWeapons) )
            {
                this.AttackerLogic_Assister( Context, Entity, order, guarded, true, fleetMem, entityMarkData, entityOrders );
            }            
        }
        #endregion

        private ProtectedList<ShipAssistanceData> workingWithinRangeForAssister = ProtectedList<ShipAssistanceData>.Create_WillNeverBeGCed( 20000, "EntitySimLogicImplementation_BaseInfo-workingWithinRangeForAssister" );

        #region AttackerLogic_Assister
        private void AttackerLogic_Assister( ArcenClientOrHostSimContextCore Context, GameEntity_Squad Entity, EntityOrder order, GameEntity_Squad guarded, bool OnlyInRange, 
            FleetMembership fleetMem, GameEntityTypeData.MarkLevelStats entityMarkData,
            EntityOrderCollection entityOrders )
        {
            if ( Entity == null )
                return;
            if ( Entity.TypeData.IsFleetLeader )
                return;

            if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add("doing assister logic");

            bool needNewOrder = order.TypeData == null;
            if ( order.TypeData != null )
            {
                switch ( order.TypeData.Type )
                {
                    case EntityOrderType.Move_Normal:
                    case EntityOrderType.Wormhole:
                        needNewOrder = false;
                        break;
                }
            }
            //we rely on this to block us from accidentally ordering around things that are already given orders by humans.
            //that way we can use ClearSource.YesClearHumanOrders, below
            if ( order.TypeData != null && order.Source == OrderSource.HumanPlayer )
                return;
            if ( !needNewOrder )
            {
                if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "no new order needed, already moving" );
                return;
            }

            GameEntity_Squad bestTarget = null;
            //bool bestTargetIsInRange = false;
            //Chris says: hey!  This whole logic below is very impressive and all, but it's entirely dependent on the flows that are given to the engies prior to this.
            //                  Most of the time, they just get one or two flows based on whatever is nearby.  But they can help multiple things at once, so in those cases this kicks in.
            //                  But realistically, most of the time there is one option at this point, so all this fancy logic is for not-much.
            {
                workingWithinRangeForAssister.Clear( true );

                //first fill workingWithinRangeForAssister
                List<PlannedMetalFlow> flows = Entity.SquadPlannedFlows.GetDisplayList();
                for ( int i = 0; i < flows.Count; i++ )
                {
                    PlannedMetalFlow flow = flows[i];
                    GameEntity_Squad target = flow.SquadRecipient;
                    if ( target == null)
                        continue;
                    //target.DebugText = "target under consideration!";
                    bool isInRange = flow.IsInRangeAtTheMoment;

                    if ( isInRange || !OnlyInRange ) //either in range or we don't care
                    {
                        int distance = Entity.GetDistanceTo_ExpensiveAccurate( target, RadiusCheck.SubtractRadiiFromDistance, false );
                        workingWithinRangeForAssister.Add( ShipAssistanceData.GetFromPoolOrCreate( target, flow, distance, isInRange ) );
                    }
                }

                //Entity.DebugText = "tuples to check: " + workingWithinRangeForAssister.Count + " of " + Entity.FramePlan_PlannedFlows.Count;

                //only do the next part if there is something that meets the criteria for us to assist!
                if ( workingWithinRangeForAssister.Count <= 0 )
                {
                    needNewOrder = false;
                    if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "no new order needed, there were no targets we could find" );
                    return;
                }

                ShipAssistanceData bestAssistanceData = null;
                float bestAssistancePriority = 0;

                //only check for emergency repairs at first
                for ( int i = 0; i < workingWithinRangeForAssister.Count; i++ )
                {
                    ShipAssistanceData assistanceData = workingWithinRangeForAssister[i];
                    switch ( assistanceData.FlowReference.Purpose )
                    {
                        case MetalFlowPurpose.RepairingHullsOfFriendlies:
                        case MetalFlowPurpose.RepairingShieldsOfFriendlies:
                            break;
                        default:
                            continue;
                    }

                    //is this tuple valid to ever have emergency repairs?
                    if ( assistanceData.Target.TypeData.EmergencyRepairPriorityIfBelowPercentage <= 0 )
                        continue;

                    //the tuple was valid, so is this particular engineer valid to do it based on modulus of PKID?
                    if ( assistanceData.Target.TypeData.EmergencyRepairOnlyIfEngineerModulusOf == 0 ||
                        Entity.PrimaryKeyID % assistanceData.Target.TypeData.EmergencyRepairOnlyIfEngineerModulusOf == 0)
                        continue;

                    //the engineer and tuple are good to go, so is the health actually low enough?
                    Int64 lostHealth = assistanceData.Target.HullPointsLost + assistanceData.Target.ShieldPointsLost;
                    Int64 maxHealth = assistanceData.Target.GetMaxHullPoints() + assistanceData.Target.GetMaxShieldPoints();

                    //calculate the percentage it is fine for us to have lost less than
                    maxHealth *= ( 100 - assistanceData.Target.TypeData.EmergencyRepairPriorityIfBelowPercentage );
                    maxHealth /= 100;

                    // this is -not- an emergency its just not finished being built
                    // its construction_priority can already be specified something very large
                    // if it should be a priority for that, this is for repairs
                    if (assistanceData.Target.SelfBuildingMetalRemaining > 0 || assistanceData.Target.HasNotYetBeenFullyClaimed)
                        continue;

                    //if we have lost less than that, then ignore this, it's not an emergency.  If we could have lost 15%, but only have lost 10%, call me later.
                    if ( lostHealth < maxHealth )
                        continue;

                    if ( bestAssistanceData == null )
                    {
                        bestAssistanceData = assistanceData;
                        bestAssistancePriority = 10000;
                        //Entity.DebugText += "first emergency repair";
                    }
                    else
                    {
                        if ( assistanceData.Distance < bestAssistanceData.Distance || //closer
                            ( assistanceData.IsInRange && !bestAssistanceData.IsInRange) ) //or in range when the other is not
                        {
                            bestAssistancePriority = 10000;
                            bestAssistanceData = assistanceData; //if multiple emergencies, pick the closest one
                            //Entity.DebugText += "closest emergency repair";
                        }
                    }
                }

                //only check for assisting self construction or rebuilding for now, based on priorities
                if ( bestAssistanceData == null )
                {
                    for ( int i = 0; i < workingWithinRangeForAssister.Count; i++ )
                    {
                        ShipAssistanceData assistanceEntry = workingWithinRangeForAssister[i];
                        switch ( assistanceEntry.FlowReference.Purpose )
                        {
                            case MetalFlowPurpose.AssistSelfConstruction:
                            case MetalFlowPurpose.RebuildingRemains:
                            case MetalFlowPurpose.SelfConstruction:
                            case MetalFlowPurpose.ClaimingNeutrals:
                                break;
                            default:
                                continue;
                        }

                        var ispreferred = Entity.PreferredEntityTypeDataForTargeting == assistanceEntry.Target.TypeData;

                        var priority = assistanceEntry.Target.TypeData.ConstructionPriority;
                        if (ispreferred)
                        {
                            priority *= 100;
                        }
                        else
                        {
                            if ( assistanceEntry.FlowReference.Purpose == MetalFlowPurpose.ClaimingNeutrals )
                            {
                                priority /= 2;
                            }
                            else
                            {
                                if ( assistanceEntry.Target.SelfBuildingMetalRemaining <= FInt.Zero && assistanceEntry.Target.SecondsSpentAsRemains <= 0 )
                                {
                                    //tuple.FirstItem.DebugText += "no self building metal or remains!";
                                    continue; //so that we don't get caught up in factory assistance, if we're not still self-building or remains, skip this
                                }
                            }
                        }

                        if ( bestAssistanceData == null )
                        {
                            bestAssistanceData = assistanceEntry;
                            bestAssistancePriority = priority;
                            //Entity.DebugText += "first const or rebuild";
                        }
                        else
                        if ( priority > bestAssistancePriority )
                        {
                            //Entity.DebugText += "highest priority of const or rebuild";
                            bestAssistanceData = assistanceEntry; //if a higher construction priority, then choose that
                            bestAssistancePriority = priority;
                        }
                        else 
                        if ( assistanceEntry.Target.TypeData.ConstructionPriority == bestAssistancePriority )
                        {
                            if ( assistanceEntry.Distance < bestAssistanceData.Distance || //closer
                                (assistanceEntry.IsInRange && !bestAssistanceData.IsInRange) ) //or in range when the other is not
                            {
                                //Entity.DebugText += "closest of highest priority of const or rebuild";
                                bestAssistanceData = assistanceEntry; //if a equal construction priority, then choose the closest
                                bestAssistancePriority = priority;
                            }
                        }
                    }
                }

                if ( bestAssistanceData == null )
                {
                    //now check based on the priority of the flow
                    for ( int i = 0; i < workingWithinRangeForAssister.Count; i++ )
                    {
                        ShipAssistanceData assistanceEntry = workingWithinRangeForAssister[i];

                        var priority = assistanceEntry.Target.TypeData.ConstructionPriority;
                        if (Entity.PreferredEntityTypeDataForTargeting == assistanceEntry.Target.TypeData)
                            priority *= 100;

                        if ( bestAssistanceData == null )
                        {
                            //Entity.DebugText += "first in general from final check";
                            bestAssistanceData = assistanceEntry;
                             bestAssistancePriority = priority;
                        }
                        else
                        if ( assistanceEntry.FlowReference.PriorityLevel > bestAssistancePriority )
                        {
                            bestAssistanceData = assistanceEntry; //if a higher flow priority, then choose that
                            bestAssistancePriority = priority;
                            //Entity.DebugText += "higher priority of flow (" + tuple.SecondItem.PriorityLevel + ")";
                        }
                        else 
                        if ( assistanceEntry.FlowReference.PriorityLevel == bestAssistancePriority )
                        {
                            if ( assistanceEntry.Distance < bestAssistanceData.Distance || //closer
                                (assistanceEntry.IsInRange && !bestAssistanceData.IsInRange) ) //or in range when the other is not
                            {
                                bestAssistanceData = assistanceEntry; //if a equal flow priority, then choose the closest
                                bestAssistancePriority = priority;
                                //Entity.DebugText += "closest of higher priority of flow (" + tuple.SecondItem.PriorityLevel + ")";
                            }
                        }
                    }
                }

                if ( bestAssistanceData == null )
                {
                    //now pick anything, if we found nothing.
                    for ( int i = 0; i < workingWithinRangeForAssister.Count; i++ )
                    {
                        ShipAssistanceData assistanceEntry = workingWithinRangeForAssister[i];

                        if ( bestAssistanceData == null )
                        {
                            bestAssistanceData = assistanceEntry;
                            //Entity.DebugText += "first of anything, we found nothing";
                        }
                        else
                        if ( assistanceEntry.Distance < bestAssistanceData.Distance || //closer
                             (assistanceEntry.IsInRange && !bestAssistanceData.IsInRange) ) //or in range when the other is not
                        {
                            bestAssistanceData = assistanceEntry; //if more than one, just choose closest
                            //Entity.DebugText += "closest of anything, we found nothing";
                        }
                    }
                }

                if ( bestAssistanceData != null )
                {
                    bestTarget = bestAssistanceData.Target;
                    needNewOrder = !bestAssistanceData.IsInRange;
                }
            }

            if ( !needNewOrder )
            {
                if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "no new order needed, already in range of an assist target" );
                return;
            }

            ArcenPoint targetPoint;

            Planet entityPlanet = Entity.Planet;
            if ( entityPlanet == null )
            {
                if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "entity planet is null, so we can't give orders" );
                return;
            }

            if ( bestTarget == null )
            {
                if ( Entity.GuardOrPatrolOffsetPoints.Count <= 0 )
                {
                    if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "no new order needed, no eligible assist targets and no remembered guard point" );
                    return;
                }
                targetPoint = Entity.GuardOrPatrolOffsetPoints[0];
                if ( Entity.GetIsWithinRangeOf( targetPoint, Entity.DataForMark.AssistRange ) )
                {
                    if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "no new order needed, no eligible assist targets and within range of remembered guard point" );
                    return;
                }
                if ( entityPlanet.GetIsPointOutsideGravWell_SlowButCorrect( targetPoint ) )
                {
                    if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "target point is outside of gravity well: " + targetPoint );
                    return;
                }
            }
            else
            {
                if ( bestTarget.Planet != entityPlanet )
                {
                    if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "best target is not on our planet!" );
                    return;
                }
                AngleDegrees angleFromTargetToSelf = bestTarget.WorldLocation.GetAngleToDegrees( Entity.WorldLocation );
                int closeToWithinRange = ( entityMarkData.AssistRange * 8 ) / 10;
                targetPoint = bestTarget.WorldLocation.GetPointAtAngleAndDistance( angleFromTargetToSelf, closeToWithinRange );

                if ( entityPlanet.GetIsPointOutsideGravWell_SlowButCorrect( targetPoint ) )
                {
                    if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "target point from best target is outside of gravity well: " + targetPoint );
                    return;
                }
            }

            if ( targetPoint == ArcenPoint.ZeroZeroPoint )
                return; //this would be an invalid order

            entityOrders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.YesClearAnyOrders_IncludingFromHumans, "AutomatedAssister" );
            EntityOrder newOrder = EntityOrder.Create_Move_Normal( targetPoint, false, OrderSource.Other, false );
            if ( newOrder.TypeData == null )
                return;

            entityOrders.QueueOrder( Entity, newOrder );
            if ( _trace )
            {
                if ( bestTarget != null )
                    traceBuffer.Add( Environment.NewLine ).Add( "picked assist target" ).Add( bestTarget.TypeData.InternalName );
                else
                    traceBuffer.Add( Environment.NewLine ).Add( "no assist target, going back to targetPoint " ).Add( targetPoint );
            }
        }
        #endregion

        #region AttackerLogic_Combat
        private void AttackerLogic_Combat( ArcenClientOrHostSimContextCore Context, GameEntity_Squad Entity, EntityOrder order, GameEntity_Squad guarded, bool AllowOverridingHumanOrders, 
            FleetMembership fleetMem, GameEntityTypeData.MarkLevelStats entityMarkData, Faction faction,
            EntityOrderCollection entityOrders )
        {
            bool needNewOrder = order.TypeData == null || order.TypeData.Type != EntityOrderType.Attack;
            bool insertOrderInsteadOfClear = false; 

            if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add("doing attacker logic");

            //we rely on this to block us from accidentally ordering around things that are already given orders by humans.
            //that way we can use ClearSource.YesClearHumanOrders, below
            if ( !AllowOverridingHumanOrders )
            {
                if ( order.TypeData != null && order.Source == OrderSource.HumanPlayer )
                {
                    if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "no new order because following human orders" );
                    return;
                }
            }
            else
            {
                insertOrderInsteadOfClear = true;
                
                if ( order.TypeData != null && 
                     order.Source == OrderSource.HumanPlayer )
                {
                    //can't override these
                    switch ( order.TypeData.Type )
                    {
                        case EntityOrderType.Wormhole:
                        case EntityOrderType.Attack:
                            {
                                if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( string.Format("current order {0} is never overriden", order.TypeData.Type) );
                                return;
                            }
                        default:
                            break;
                    }
                }
            }

            //Entity.DebugText = DateTime.Now + " needNewOrder: " + needNewOrder;
            #region If we have an existing order, think about changing it based on the FRD stuf
            if ( !needNewOrder && !insertOrderInsteadOfClear )
            {
                GameEntity_Squad existingTarget = order.RelatedSquad.GetSquad();
                
                // jcf: im suspicious of this method, why is there a special check only for this?? just check if its a valid target at all
                if ( existingTarget == null || 
                     existingTarget.CalculateShouldBeClearedFromTargetingOfAttacker( Entity ) )
                {
                    //this existing target is invalid!  Get rid of that order
                    needNewOrder = true;
                    entityOrders.RemoveQueuedOrder( order );
                    order = new EntityOrder( -1 );
                }
                else
                {
                    //Entity.DebugText += "  existingTarget: " + existingTarget.TypeData.InternalName;
                }
            }

            if ( order.TypeData == null )
                needNewOrder = true;

            if ( !needNewOrder )
            {
                if ( order.Source == OrderSource.HumanPlayer )
                {
                    //if it was given by the player specifically, then don't override it!
                }
                else
                {
                    bool foundAMatch = false;
                    for ( int i = 0; i < Entity.Systems.Count; i++ )
                    {
                        var system = Entity.Systems[i];
                        var systemData = system.TypeData;
                        if ( systemData == null )
                            continue;
                        
                        var disabledReason = system.ComputeDisabledReason();
                        if ( disabledReason != ArcenRejectionReason.Unknown )
                            continue;
                        
                        if ( system.CurrentFRDTarget.GetPrimaryKeyID() == order.RelatedSquad.GetPrimaryKeyID() )
                            foundAMatch = true;
                    }
                    
                    //none of our systems are after the current FRD target anymore!
                    if ( !foundAMatch )
                    {
                        needNewOrder = true;
                        
                        if (order.TypeData != null &&
                            order.TypeData.Type == EntityOrderType.Attack && 
                            order.Source != OrderSource.HumanPlayer)
                        {
                             if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "removed frd attack order, it was no longer valid" );
                            entityOrders.RemoveQueuedOrder(order);
                        }
                    }
                }
            }
            #endregion
            
            if ( !needNewOrder )
            {
                //Entity.DebugText += "  finalNotNeedNewOrder";
                if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "no new order needed" );
                return;
            }

            //since we might have multiple systems that want to do FRD chasing, choose the best one from the list
            GameEntity_Squad bestTarget = null;
            int bestTargetPriority = 0;
            {
                bool entityHasAnySniperRangeWeapinsRightNow = Entity.GetHasAnySniperRangedWeaponsRightNow();

                for ( int i = 0; i < Entity.Systems.Count; i++ )
                {
                    var system = Entity.Systems[i];
                    var systemData = system.TypeData;
                    if ( systemData == null )
                        continue;
                    
                    var disabledReason = system.ComputeDisabledReason();
                    if ( disabledReason != ArcenRejectionReason.Unknown )
                        continue;
                          
                    if ( entityHasAnySniperRangeWeapinsRightNow )
                    {
                        //if the ship has any sniper-ranged systems, then ONLY those systems can do FRD targeting
                        // jcf: im very suspicious of this, it seems to circumvent all the rules concerning kiting
                        //      priority ...
                        if ( !systemData.FiresFromAnyRange && system.DataForMark.BaseRange < 999999 )
                            continue; 
                    }
                    
                    if ( system.CurrentFRDTarget.GetPrimaryKeyID() <= 0 )
                        continue;
                    
                    var workingTarget = system.CurrentFRDTarget.GetSquad();
                    
                    if ( workingTarget == null || 
                         workingTarget.GetHasBeenDestroyed() || 
                         workingTarget.HasBeenRemovedFromSim ||
                         workingTarget.SecondsSpentAsRemains > 0 || 
                         workingTarget.ToBeRemovedAtEndOfThisFrame )
                    {
                        //target not valid!  Stop tracking it now
                        system.CurrentFRDTarget.SetInternalRef( null );
                        system.CurrentFRDPriority = 0;
                        system.TimeFRDTargetExpires = 0;
                        
                        continue;
                    }
                    
                    if ( bestTarget  == null || 
                         system.CurrentFRDPriority > bestTargetPriority )
                    {
                        bestTarget = workingTarget;
                        bestTargetPriority = system.CurrentFRDPriority;
                    }
                }
            }

            if ( bestTarget != null )
            {
                //Entity.DebugText += "  bestTarget: " + bestTarget.TypeData.InternalName;

                EntityOrder newOrder = EntityOrder.Create_Attack( bestTarget.PrimaryKeyID, false, "ReevalCombatTarg", false, OrderSource.Other, false );
                if ( newOrder.TypeData == null )
                {
                    if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( string.Format("tried to give order to attack {0} but seems to be dead?", bestTarget.ToString()) );
                        return; //some sort of problem, probably the entity is dead!
                }
                
                newOrder.CalculateStrengthCountingData_ClientAndHost( Entity, true, true, true );

                if ( insertOrderInsteadOfClear )
                {
                    entityOrders.InsertOrderAtStart( Entity, newOrder );
                }
                else
                {
                    entityOrders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.YesClearAnyOrders_IncludingFromHumans, "AutomatedCombat" );
                    entityOrders.QueueOrder( Entity, newOrder );
                    
                    // if we were idle till now
                    // remember this location to return to
                    //
                    // jcf: has issues still
                    /*
                    if ( order.TypeData == null )
                    {
                        if (Entity.GuardOrPatrolOffsetPoints.Count == 0)
                        {
                            Entity.GuardOrPatrolOffsetPoints.Add(Entity.WorldLocation);
                        }
                    }
                    */
                }
                
                if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "picked target" ).Add( bestTarget.TypeData.InternalName );
                
                return;
            }
            
            // so we don't have an frd target
            // .. well, time for melee stuff to return to their point of station
            // jcf: not currently used do to buggy behavior.
        
            /*
            //Entity.DebugText += "  bestTarget null!";
            if ( !GetShouldEntityUseImplicitMeleeAttackerBehavior( Entity, faction, entityOrders ) )
            {
                if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "no order because bestTarget == null && !GetShouldEntityUseImplicitMeleeAttackerBehavior( Entity )" );
                return;
            }
            if ( Entity.GuardOrPatrolOffsetPoints.Count <= 0 )
            {
                if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "no order despite implicit melee behavior, becasue bestTarget == null && Entity.GuardingOffsets.Count <= 0" );
                return;
            }
            
            ArcenPoint targetPoint = Entity.GuardOrPatrolOffsetPoints[0];
            if ( targetPoint == ArcenPoint.ZeroZeroPoint )
            {
                if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "no order despite implicit melee behavior and Entity.GuardingOffsets.Count > 0, because the first point is 0,0, which is invalid" );
                return;
            }

            entityOrders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.YesClearAnyOrders_IncludingFromHumans, "AutomatedCombatMove" );
            EntityOrder newOrder = EntityOrder.Create_Move_Normal( targetPoint, false, OrderSource.Other, false );
            
            if ( newOrder.TypeData == null )
            {
                if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "no order despite implicit melee behavior and valid Entity.GuardingOffsets[0], because EntityOrder.Create_Move_Normal returned null. Null! The nerve." );
                return;
            }

            entityOrders.QueueOrder( Entity, newOrder );
            
            if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "no bestTarget, so because of implicit melee behavior, going back to targetPoint " ).Add( targetPoint );
            */
        }
        #endregion

        #region Guard_FleetShipLogic
        private void Guard_FleetShipLogic( ArcenClientOrHostSimContextCore Context, GameEntity_Squad Entity, EntityOrder order, GameEntity_Squad guarded,
            FleetMembership fleetMem, GameEntityTypeData.MarkLevelStats entityMarkData, Faction faction, PlanetFaction pFaction,
            EntityOrderCollection entityOrders )
        {
            if ( guarded == null ||
                 guarded.ToBeRemovedAtEndOfThisFrame ||
                 guarded.HasBeenRemovedFromSim ||
                 guarded.Planet != Entity.Planet )
            {
                if ( !Entity.TypeData.CannotBeStoredInsideGuardPost && Entity.TypeData.IsDrone )
                {
                    if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "Guard_FleetShip: dying because my guarded-object is gone and I'm a drone" );
                    Entity.Die( Context, true );
                }
                else
                {
                    if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "Guard_FleetShip: clearing orders and going Attacker because my guarded-object is gone" );
                    entityOrders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "GuardGoingAttackerBecauseGuardedIsGone" );
                    Int16 bestFactionIndex = pFaction.GetIndexOfMostAnnoyingFaction( Context );
                    entityOrders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, bestFactionIndex ); //works fine because sim
                }
            }
            else
            {
                if ( !Entity.TypeData.CannotBeStoredInsideGuardPost && Entity.GetIsWithinRangeOf_VeryBasicCheckOnly( guarded.WorldLocation, ExternalConstants.Instance.GuardReabsorptionDistance ) )
                {
                    if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "Guard_FleetShip: absorbing because I'm close enough to my guarded-object" );
                    guarded.AddToAIReinforcementPointContents( Entity.TypeData, 1 + Entity.ExtraStackedSquadsInThis, "AbsorbGuard", Entity.GetSpecialFactionDataInternalName_Safe() );
                    Entity.AddOrSetExtraStackedSquadsInThis( 0, true ); //without this, it will just remove one from the stack and exponentially get more because of it!
                    Entity.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.GettingIntoTransport );
                }
                else
                {
                    bool needNewOrder = false;

                    if ( order.TypeData == null )
                    {
                        if ( _trace )
                        {
                            traceBuffer.Add( Environment.NewLine ).Add( "Guard_FleetShip: need new order because no current order" )
                                .Add( " (currently " ).Add( Entity.GetDistanceTo_VeryCheapButExtremelyRough( guarded, RadiusCheck.IgnoreRadii ) ).Add( " distance from guarded object " )
                                .Add( guarded.TypeData.InternalName ).Add( " #" ).Add( guarded.PrimaryKeyID )
                                .Add( ")" );
                        }
                        needNewOrder = true;
                    }
                    else if ( order.TypeData.Type != EntityOrderType.Move_Normal )
                    {
                        if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "Guard_FleetShip: need new order because current order is " ).Add( EnumNameCache.GetName( order.TypeData.Type ) ).Add( " instead of Move" );
                    }
                    else if ( !guarded.GetIsWithinRangeOf_VeryBasicCheckOnly( order.RelatedPoint, ExternalConstants.Instance.GuardReabsorptionDistance ) )
                    {
                        if ( _trace )
                            traceBuffer.Add( Environment.NewLine )
                                .Add( "Guard_FleetShip: need new order because current order point (to " )
                                .Add( order.RelatedPoint )
                                .Add( ") is too far from my guarded-object (" )
                                .Add( guarded.WorldLocation )
                                .Add( "); my current location is (" )
                                .Add( Entity.WorldLocation )
                                .Add( ")" )
                                ;
                        needNewOrder = true;
                    }
                    if ( needNewOrder )
                    {
                        if ( _trace ) traceBuffer.Add( Environment.NewLine ).Add( "Guard_FleetShip: clearing orders and issuing new move order to get closer to my guarded-object" );
                        entityOrders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "GuardGettingCloserToGuarded" );
                        EntityOrder newOrder = EntityOrder.Create_Move_Normal( guarded.WorldLocation, false, OrderSource.Other, false );
                        if ( newOrder.TypeData == null )
                            return;
                        entityOrders.QueueOrder( Entity, newOrder );
                    }
                    else
                    {
                        if ( _trace )
                            traceBuffer.Add( Environment.NewLine )
                                .Add( "Guard_FleetShip: doing nothing because I already have a move order (to " )
                                .Add( order.RelatedPoint )
                                .Add( ") which is close enough to my guarded-object (" )
                                .Add( guarded.WorldLocation )
                                .Add( "); my current location is (" )
                                .Add( Entity.WorldLocation )
                                .Add( ")" )
                                ;
                    }
                }
            }
        }
        #endregion

        #region Guard_Guardian_PatrollingLogic
        private void Guard_Guardian_PatrollingLogic( ArcenClientOrHostSimContextCore Context, GameEntity_Squad Entity, EntityOrder order, GameEntity_Squad guarded,
            FleetMembership fleetMem, GameEntityTypeData.MarkLevelStats entityMarkData, Faction faction, PlanetFaction pFaction,
            EntityOrderCollection entityOrders )
        {
            if ( Entity == null || Entity.Planet == null )
                return;
            int debugStage = 1;
            try
            {
                debugStage = 100;
                if ( Entity.Planet != null )
                {
                    debugStage = 110;
                    switch ( Entity.Planet.BattleStatus )
                    {
                        case PlanetBattleStatus.Tier3_PlayersAbsent_OffFrame:
                        case PlanetBattleStatus.Tier3_PlayersAbsent_OnFrame:
                            return; //if on a tier 3 planet (no player looking at me, no player ships), then NEVER bother with patrolling guardians.  It's a waste
                    }
                }

                debugStage = 200;
                if ( guarded == null || Entity.GuardOrPatrolOffsetPoints.Count <= 0 )
                {
                    guarded = null;
                    debugStage = 300;
                    Entity.GuardOrPatrolOffsetPoints.Clear();
                    Entity.GuardedUnit.Clear();

                    debugStage = 340;
                    entityOrders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "GuardedDeadSoGuardGoingAttacker" );
                    debugStage = 360;
                    Int16 bestFactionIndex = pFaction.GetIndexOfMostAnnoyingFaction( Context );
                    debugStage = 380;
                    entityOrders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, bestFactionIndex ); //works fine because sim
                }
                else
                {
                    debugStage = 400;
                    bool needNewOrder = false;
                    if ( Entity.NextGuardOrPatrolOffsetPointIndex >= Entity.GuardOrPatrolOffsetPoints.Count )
                        Entity.NextGuardOrPatrolOffsetPointIndex = 0;
                    debugStage = 420;
                    ArcenPoint offset = Entity.GuardOrPatrolOffsetPoints[Entity.NextGuardOrPatrolOffsetPointIndex];
                    debugStage = 440;
                    ArcenPoint targetPoint = guarded.WorldLocation + offset;
                    debugStage = 460;
                    if ( order.TypeData == null )
                    {
                        debugStage = 500;
                        if ( !Entity.GetIsWithinRangeOf_VeryBasicCheckOnly( targetPoint, ExternalConstants.Instance.EXTRA_SPACE_FOR_MOVEMENT_ORDERS_BETWEEN_SHIPS ) )
                            needNewOrder = true;
                    }
                    else
                    {
                        debugStage = 510;
                        if ( order.RelatedSquad.GetPrimaryKeyID() != guarded.PrimaryKeyID &&
                            !order.RelatedPoint.GetHasAnyChanceOfBeingInRange( targetPoint, ExternalConstants.Instance.EXTRA_SPACE_FOR_MOVEMENT_ORDERS_BETWEEN_SHIPS ) )
                            needNewOrder = true;
                    }
                    
                    if ( needNewOrder )
                    {
                        debugStage = 600;
                        entityOrders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "GuardGoingToPatrol" );
                        debugStage = 610;
                        EntityOrder newOrder = EntityOrder.Create_Move_Normal( targetPoint, false, OrderSource.Other, false );
                        if ( newOrder.TypeData == null )
                            return;
                        debugStage = 620;
                        entityOrders.QueueOrder( Entity, newOrder );
                    }
                    else
                    {
                        debugStage = 700;
                        if ( Entity.GuardOrPatrolOffsetPoints.Count > 1 && order.TypeData == null )
                            Entity.NextGuardOrPatrolOffsetPointIndex++;
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Guard_Guardian_PatrollingLogic debugStage: " + debugStage + " \n" + e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region Guard_Guardian_AnchoredLogic
        private void Guard_Guardian_AnchoredLogic( ArcenClientOrHostSimContextCore Context, GameEntity_Squad Entity, EntityOrder order, GameEntity_Squad guarded,
            FleetMembership fleetMem, GameEntityTypeData.MarkLevelStats entityMarkData, Faction faction, PlanetFaction pFaction,
            EntityOrderCollection entityOrders )
        {
            if ( guarded == null )
            {
                Entity.GuardOrPatrolOffsetPoints.Clear();
                Entity.GuardedUnit.Clear();

                entityOrders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "GuardWasAnchoredButGuardedDead" );
                Int16 bestFactionIndex = pFaction.GetIndexOfMostAnnoyingFaction( Context );
                entityOrders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full, bestFactionIndex ); //works fine because sim
            }
        }
        #endregion

        #region DelegateHelper_CheckForGuardFreeingProvocation
        private static ArcenPoint DelegateHelper_CheckForGuardFreeingProvocation_guardPoint;
        private static GameEntity_Squad DelegateHelper_CheckForGuardFreeingProvocation_foundHit;
        private static DelReturn DelegateHelper_CheckForGuardFreeingProvocation( GameEntity_Squad otherEntity )
        {
            //if ( otherfaction.Type != FactionType.Player )
            //    return DelReturn.Continue;
            //these ships are invisible to aggroing on distance!
            if ( otherEntity.TypeData.CannotTargetOrAlertAIReinforcementSpots )
                return DelReturn.Continue;
            if ( !otherEntity.GetIsWithinRangeOf_VeryBasicCheckOnly( DelegateHelper_CheckForGuardFreeingProvocation_guardPoint, AIWar2GalaxySettingQuickAccess.GuardAggroDistance ) )
                return DelReturn.Continue;
            if ( otherEntity.GetCurrentCloakingPoints() > 0 )
                return DelReturn.Continue;
            DelegateHelper_CheckForGuardFreeingProvocation_foundHit = otherEntity;
            return DelReturn.Break;
        }
        #endregion

        #region DoSystemStep
        public override void DoSystemStep( FInt EffectiveDeltaTime, ArcenClientOrHostSimContextCore Context, EntitySystem System, bool DoShotsAllInstaHit )
        {
            int debugStage = 0;
            //Interlocked.Increment( ref Engine_Universal.Accumulator_CUS1 );
            try
            {
                if ( CentralVars.DEBUG_TURN_OFF_SYTEM_STEP )
                    return;
                
                debugStage = 1000;
                if ( System == null )
                    return;

                debugStage = 1100;
                EntitySystemTypeData.MarkLevelStats dataForMark = System.DataForMark;
                if ( dataForMark == null )
                    return;
                
                debugStage = 1200;
                if ( !dataForMark.IsFunctionalAtThisMarkLevel )
                    return;

                debugStage = 1300;
                if ( System.IsToggledOff() )
                    return;

                debugStage = 1400;
                EntitySystemTypeData typeData = System.TypeData;
                if ( typeData == null )
                    return;

                debugStage = 1500;
                GameEntity_Squad parent = System.ParentEntity;
                if ( parent == null )
                    return;

                debugStage = 2000;
                (System.CustomSystem as ExternalData_CustomSystem)?.Update(Context, EffectiveDeltaTime);
                
                debugStage = 2100;
                if ( typeData.CareAboutStateOfMatterToBeEnabled )
                {
                    if ( parent != null )
                    {
                        if ( parent.CurrentStateOfMatter != typeData.MustBeThisStateOfMatterToBeEnabled )
                            return;
                    }
                }
                
                #region tracing
                debugStage = 3000;
                bool trace = Engine_AIW2.TraceAtAll && 
                             parent == GameEntity_Base.CurrentlyHoveredOver && 
                             Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.IndividualLogic ) &&
                             Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Firing );
                
                ArcenCharacterBuffer tracingBuffer = null;
                if ( trace ) tracingBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "EntitySimLogicImpl-DoSystemStep-trace", 10f );
                #endregion
                
                if ( EffectiveDeltaTime <= FInt.Zero )
                {
                    #region tracing
                    if ( !World.Instance.IsPaused ) // avoid spamming the log with useless skip messages while paused
                    {
                        if ( trace ) tracingBuffer.Add( "Skipping DoSystemStep for " ).Add( typeData.InternalName_Longer ).Add( " from " ).Add( parent.TypeData.InternalName ).Add( " #" ).Add( parent.PrimaryKeyID ).Add( " because EffectiveDeltaTime <= FInt.Zero" );
                        if ( trace )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                            tracingBuffer.ReturnToPool();
                            tracingBuffer = null;
                        }
                    }
                    #endregion
                    return;
                }
                
                tracingBuffer?.Add( "Tracing DoSystemStep for " ).Add( typeData.InternalName_Longer ).Add( " from " ).Add( parent.TypeData.InternalName ).Add( " #" ).Add( parent.PrimaryKeyID );

                debugStage = 4000;
                if ( typeData.OnlyFiresOnDeath )
                {
                    #region tracing
                    if ( trace ) tracingBuffer.Add( "\n" ).Add( "skipping because this.TypeData.OnlyFiresOnDeath" );
                    if ( trace )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                    return;
                }

                debugStage = 5000;
                ArcenRejectionReason disabledReason = System.ForShortTermPlanning_DisabledReason;

                debugStage = 6000;
                if ( disabledReason != ArcenRejectionReason.Unknown && 
                     disabledReason != ArcenRejectionReason.EntityIsInHoldFireMode )
                {
                    #region tracing
                    if ( trace ) tracingBuffer.Add( "\n" ).Add( "skipping because disabledReason == " ).Add( EnumNameCache.GetName( disabledReason ) );
                    if ( trace )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                    return;
                }

                debugStage = 7000;
                System.TimeUntilNextShot -= EffectiveDeltaTime;
                if ( System.TimeUntilNextShot > FInt.Zero )
                {
                    #region tracing
                    if ( trace ) tracingBuffer.Add( "\n" ).Add( "skipping because this.TimeUntilNextShot > FInt.Zero" );
                    if ( trace )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                    return;
                }
                
                // jcf: this loses time, but..
                System.TimeUntilNextShot = FInt.Zero;

                debugStage = 7100;
                if ( disabledReason == ArcenRejectionReason.EntityIsInHoldFireMode )
                {
                    #region tracing
                    if ( trace ) tracingBuffer.Add( "\n" ).Add( "skipping because disabledReason == " ).Add( EnumNameCache.GetName( disabledReason ) );
                    if ( trace )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                    return;
                }

                debugStage = 8000;
                if ( typeData.FiringTiming == FiringTiming.Never )
                {
                    #region tracing
                    if ( trace ) tracingBuffer.Add( "\n" ).Add( "skipping because this.TypeData.FiringTiming == FiringTiming.Never" );
                    if ( trace )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                    return;
                }
                
                debugStage = 9000;
                if ( typeData.Category == EntitySystemCategory.Passive )
                {
                    #region tracing
                    if ( trace ) tracingBuffer.Add( "\n" ).Add( "skipping because this.TypeData.Category == EntitySystemCategory.Passive" );
                    if ( trace )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                    return;
                }

                debugStage = 10000;
                if ( typeData.Category == EntitySystemCategory.Weapon && 
                     typeData.FiringTiming == FiringTiming.WhenParentEntityHit )
                {
                    #region tracing
                    if ( trace ) tracingBuffer.Add( "\n" ).Add( "skipping because this.TypeData.Category == EntitySystemCategory.Weapon and this.TypeData.FiringTiming == FiringTiming.WhenParentEntityHit" );
                    if ( trace )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                    return;
                }

                debugStage = 11000;
                ArcenRejectionReason preventionReason = System.ForShortTermPlanning_CannotBeFiredReason;
                if ( preventionReason != ArcenRejectionReason.Unknown )
                {
                    #region tracing
                    if ( trace ) tracingBuffer.Add( "\n" ).Add( "skipping because preventionReason == " ).Add( EnumNameCache.GetName( preventionReason ) );
                    if ( trace )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                    return;
                }

                //Interlocked.Increment( ref Engine_Universal.Accumulator_CUB1 );
                bool didSomethingThatCausesASalvoToBeMarked = false;
                
                debugStage = 12000;
                if ( typeData.Category == EntitySystemCategory.Weapon )
                {
                    debugStage = 13000;
                    bool didTrigger = ActuallyFireSalvoAtTargetPriorityList( Context, System, trace, tracingBuffer, DoShotsAllInstaHit );
                    
                    debugStage = 14000;
                    if ( !didTrigger )
                    {
                        #region tracing
                        if ( trace ) tracingBuffer.Add( "\n" ).Add( "skipping remainder of logic because ActuallyFireSalvo returned false" );
                        if ( trace )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                            tracingBuffer.ReturnToPool();
                            tracingBuffer = null;
                        }
                        #endregion
                        return;
                    }
                    
                    didSomethingThatCausesASalvoToBeMarked = true;
                }
                //Interlocked.Increment( ref Engine_Universal.Accumulator_CUB2 );

                debugStage = 16000;
                //Interlocked.Increment( ref Engine_Universal.Accumulator_CUB3 );

                if ( didSomethingThatCausesASalvoToBeMarked )
                    this.MarkSalvoAsHavingBeenFired( System );
                //Interlocked.Increment( ref Engine_Universal.Accumulator_CUB4 );

                #region tracing
                if ( trace )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "DoSystemStep error at debugStage " + debugStage + ", error: " + e, Verbosity.ShowAsError );
            }
            //finally
            //{
            //    Interlocked.Increment( ref Engine_Universal.Accumulator_CUS2 );
            //}
        }
        #endregion

        #region DoSystemOnDeathEffect
        public override void DoSystemOnDeathEffect( ArcenSimContextAnyStatus Context, EntitySystem System, int numDying )
        {
            if ( System == null || Context == null )
                return;
            EntitySystemTypeData typeData = System.TypeData;
            if ( typeData == null )
                return;
            GameEntity_Squad parent = System.ParentEntity;
            if ( parent == null )
                return;

            if ( typeData.Category == EntitySystemCategory.Weapon )
            {
                bool doShotsAllInstaHit = AIWar2GalaxySettingQuickAccess.ShotsAllInstaHit;
                ActuallyFireSalvoFromOnDeath( Context.GetHostOnlyContext(), System, numDying, false, null, doShotsAllInstaHit );
            }
        }
        #endregion

        #region MarkSalvoAsHavingBeenFired
        private void MarkSalvoAsHavingBeenFired( EntitySystem System )
        {
            if ( System == null )
                return;
            EntitySystemTypeData typeData = System.TypeData;
            if ( typeData == null )
                return;

            System.TimeUntilNextShot += (FInt)System.GetWeaponReloadTime( true );

            if ( System.DataForMark.AltSecondsPerSalvo > 0 )
            {
                System.ShotsSinceLastSwitchingSalvoFiringMode++;
                if ( System.IsInAltSalvoFiringMode )
                {
                    if ( System.ShotsSinceLastSwitchingSalvoFiringMode >= typeData.UseAlternateRateOfFireForXShotsBeforeReverting )
                    {
                        System.IsInAltSalvoFiringMode = false;
                        System.ShotsSinceLastSwitchingSalvoFiringMode = 0;
                    }
                }
                else
                {
                    if ( System.ShotsSinceLastSwitchingSalvoFiringMode >= typeData.UseAlternateRateOfFireAfterXShots )
                    {
                        System.IsInAltSalvoFiringMode = true;
                        System.ShotsSinceLastSwitchingSalvoFiringMode = 0;
                    }
                }
            }
        }
        #endregion

        #region ActuallyFireSalvoAtTargetPriorityList
        public bool ActuallyFireSalvoAtTargetPriorityList( ArcenClientOrHostSimContextCore Context, EntitySystem System, bool trace, ArcenCharacterBuffer tracingBuffer, bool DoShotsAllInstaHit )
        {
            if ( System == null )
                return false;
            
            var typeData = System.TypeData;
            if ( typeData == null )
                return false;

            //Interlocked.Increment( ref Engine_Universal.Accumulator_CUB1 );
            int debugStage = 0;
            try
            {
                debugStage = 100;
                System.LastShotFireAbortCode = 0;
                //System.ParentEntity.DebugText = "TRY SALVO";
                if ( typeData.OnlyFiresOnDeath )
                {
                    debugStage = 200;
                    System.LastShotFireAbortCode = FireAbortCode.FiresOnDeath;
                    return false; //no shot was fired
                }

                var systemParent = System.ParentEntity;
                if ( systemParent == null )
                    return false;

                var systemParentTypeData = systemParent.TypeData;
                if ( systemParentTypeData == null )
                    return false;

                var systemParentPFaction = systemParent.PlanetFaction;
                if ( systemParentPFaction == null )
                    return false;
                
                var systemParentFaction = systemParentPFaction.Faction;
                if ( systemParentFaction == null )
                    return false;

                var systemParentOrders = systemParent.Orders;
                if ( systemParentOrders == null )
                    return false;

                var forMark = System.DataForMark;
                if ( forMark == null )
                    return false;
                
                debugStage = 300;
                #region tracing
                if ( trace ) tracingBuffer.Add( "\n" ).Add( "Tracing ActuallyFireSalvo for " ).Add( typeData.InternalName_Longer ).Add( " from " ).Add( systemParentTypeData.InternalName ).Add( " #" ).Add( systemParent.PrimaryKeyID );
                #endregion
                
                #region numShotsAlive
                int numShotsAlive = 0;
                int maxAlive = forMark.MaxShotsAlive;
                if (maxAlive > 0 && 
                    typeData.ShotTypeData.Category == GameEntityCategory.Ship)
                {
                    debugStage = 301;
                    int numPending = 0;
                    foreach ( var itr in systemParent.PendingDroneGunShots)
                    {
                        if (itr.System == System)
                            numPending += itr.Count;
                    }
                    
                    int numAlive = 0;
                    foreach ( var itr in systemParent.ChildSquads)
                    {
                        debugStage = 302;
                        var child = itr.GetSquad();
                        if (child == null) 
                            continue;
                        
                        if (child.TypeData == typeData.ShotTypeData)
                            numAlive += child.ShipCount;
                    }
                    
                    numShotsAlive = numPending + numAlive;
                    
                    #region tracing
                    if ( trace )
                        tracingBuffer.Add(string.Format("\n\tnumShotsAlive={0} [numPending={1} + numAlive={2}]; max={3}", numShotsAlive, numPending, numAlive, maxAlive));
                    #endregion
                }
                #endregion
                
                debugStage = 1000;
                int cloakingPointsIfMobileNPC = 0;
                if ( systemParentTypeData.IsMobile && systemParentFaction.Type != FactionType.Player )
                    cloakingPointsIfMobileNPC = systemParent.GetCurrentCloakingPoints();

                debugStage = 1100;
                //Interlocked.Increment( ref Engine_Universal.Accumulator_CUS1 );
                EntityOrder order = systemParent.RemoveInvalidatedOrdersAndReturnFirstValid_ThatIsNotDecollision( false );
                //Interlocked.Increment( ref Engine_Universal.Accumulator_CUS2 );
                
                debugStage = 1200;
                GameEntity_Squad frdAttackTarget = System.CurrentFRDTarget.GetSquad();
                GameEntity_Squad mainAttackTarget = null;
                
                debugStage = 1300;
                if ( order.TypeData?.Type == EntityOrderType.Attack )
                {
                    mainAttackTarget = order.RelatedSquad.GetSquad();
                }
                else 
                {
                    //no attack order at the moment
                    debugStage = 1400;
                    if ( cloakingPointsIfMobileNPC > 0 &&
                         typeData.FiringTiming != FiringTiming.Always )
                    {
                        //don't fire at all if we don't have a direct target and we're cloaked.
                        System.LastShotFireAbortCode = FireAbortCode.MaintainCloak;
                        return false; 
                    }
                }

                bool IsInvalid(GameEntity_Squad target, StateOfMatterTypeData mysom)
                {
                    return target.GetHasBeenDestroyed() || 
                           target.HasBeenRemovedFromSim || 
                           target.SecondsSpentAsRemains > 0 || 
                           target.ToBeRemovedAtEndOfThisFrame ||
                           target.PlanetFaction == null || 
                           target.GetFactionTypeSafe() == FactionType.NaturalObject || 
                           target.CurrentStateOfMatter != mysom;
                }
                
                debugStage = 2000;
                if ( mainAttackTarget != null )
                {
                    debugStage = 2100;
                    if ( IsInvalid(mainAttackTarget, systemParent.CurrentStateOfMatter) )
                    {
                        debugStage = 2200;
                        mainAttackTarget = null;
                        //Interlocked.Increment( ref Engine_Universal.Accumulator_CUS3 );
                        systemParentOrders.RemoveQueuedOrder( order );
                        //Interlocked.Increment( ref Engine_Universal.Accumulator_CUS4 );
                    }
                }
                
                debugStage = 3000;
                if ( frdAttackTarget != null )
                {
                    debugStage = 3100;
                    if (IsInvalid(frdAttackTarget, systemParent.CurrentStateOfMatter))
                    {
                        debugStage = 3200;
                        frdAttackTarget = null;
                        System.CurrentFRDTarget.SetInternalRef( null );
                    }
                }

                //systemParent.DebugText += " SHOOT?";

                debugStage = 4000;
                if ( typeData.FiringTiming != FiringTiming.Always &&
                     mainAttackTarget == null && 
                     frdAttackTarget == null && 
                     System.CountOfPotentialTargetsByPriorityForSystem() <= 0 )
                {
                    debugStage = 4100;
                    if ( System != null )
                        System.LastShotFireAbortCode = FireAbortCode.NoTarget;
                    
                    return false; //no shot was fired
                }

                debugStage = 4200;
                bool chooseNewFRDIfPossible = (mainAttackTarget == null && frdAttackTarget == null && systemParentOrders.Behavior == EntityBehaviorType.Attacker_Full);
                debugStage = 4300;
                bool chooseNewAttackMoveIfPossible = (mainAttackTarget == null && frdAttackTarget == null && systemParentOrders.Behavior == EntityBehaviorType.Attacker_PursueOnlyInRange);

                debugStage = 5000;
                int totalShotsToFire = System.DataForMark.ShotsPerSalvo * systemParent.ShipCount;
                if (maxAlive > 0)
                {
                    totalShotsToFire = (maxAlive - numShotsAlive).Max(0).Min(totalShotsToFire);
                    
                    debugStage = 303;
                    if (totalShotsToFire <= 0)
                    {
                        System.LastShotFireAbortCode = FireAbortCode.CapacityReached;
                        return false;
                    }
                }
                
                debugStage = 5300;
                ArcenPoint originPoint = System.GetWorldLocation();
                ArcenPoint targetPoint = originPoint;
                
                int shot_delay_min = System.TypeData.SalvoShotMinDelay;
                int shot_delay_max = System.TypeData.SalvoShotMaxDelay;

                debugStage = 5400;
                Planet myPlanet = systemParent.Planet;

                //systemParent.DebugText += " SHOOT!";

                bool hasPlayedSoundYet = false;
                //int runningTargetIndex = 0;
                int numberOfShotsFired = 0;
                int shotCount = 0;
                int estimatedDamage = 0;
                int damagePerShot = 0;
                int shotsThatCanBeCut = 0;
                int targetDurability = 0;
                bool hadAnyOutOfRange = false;
                int startingTargetIndex = 0;
                int currentActualTargetIndex = -2;
                bool alreadyFiredAtMainAttackTarget = false;
                bool alreadyFiredAtFRDAttackTarget = false;
                #region tracing
                if ( trace )
                {
                    //tracingBuffer.Add( "\n" ).Add( "\t").Add( "damageMultiplierFromSubsquads" ).Add( ":" ).Add( damageMultiplierFromSubsquads );
                    tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "totalShotsToFire" ).Add( ":" ).Add( totalShotsToFire );
                    tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "System.CountOfPotentialTargetsByPriorityForSystem" ).Add( ":" ).Add( System.CountOfPotentialTargetsByPriorityForSystem() );
                }
                #endregion
                debugStage = 6000;
                FInt runningDelay = FInt.Zero;
                //Interlocked.Increment( ref Engine_Universal.Accumulator_CUS5 );
                for ( int outerLoop = 0; outerLoop <= 1; outerLoop++ )
                {
                    debugStage = 6100;
                    //do two passes through.  The first pass, only fire at things that will not be overkilled.
                    //if no targets found, then fire even on things that will be overkilled.
                    bool careAboutOverkill = (outerLoop == 0);

                    if ( outerLoop > 0 )
                    {
                        debugStage = 6200;
                        //THAT said, if the system takes more than 10 seconds between shots, then skip that overkill bit afterward, as that can be too wasteful.
                        if ( System.GetWeaponReloadTime( false ) > 10 )
                            break;
                        // If we take damage from shooting, then skip overkill
                        if ( System.TypeData.HealthChangeByMaxHealthDividedByThisPerAttack < FInt.Zero ) {
                            // TODO: We should still fire any shots up to the next multiple of ShotsPerSalvo, since we will be
                            // taking damage for that many shots anyway.
                            break;
                        }
                        //If we've already fired upon every target we could skip the 2nd loop entirely. We'd be checking nothing anyway.
                        if ( startingTargetIndex >= System.CountOfPotentialTargetsByPriorityForSystem() )
                            break;
                    }
                    int targetIndex = -2;

                    debugStage = 7000;
                    int attemptCount = 100;
                    while ( numberOfShotsFired < totalShotsToFire && 
                            targetIndex < System.CountOfPotentialTargetsByPriorityForSystem() && 
                            attemptCount-- > 0 )
                    {
                        debugStage = 7100;
                        //systemParent.DebugText += " " + targetIndex;
                        #region tracing
                        if ( trace ) tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "targetIndex " ).Add( targetIndex );
                        #endregion
                        GameEntity_Squad potentialTarget;
                        {
                            debugStage = 7200;
                            if ( System.CountOfPotentialTargetsByPriorityForSystem() <= targetIndex )
                                break;
                            
                            debugStage = 7300;
                            if ( targetIndex == -2 ) //try to attack the main focus of this entity if it has orders
                            {
                                debugStage = 7400;
                                if ( mainAttackTarget == null || alreadyFiredAtMainAttackTarget)
                                {
                                    targetIndex++;
                                    continue;
                                }
                                potentialTarget = mainAttackTarget;
                                currentActualTargetIndex = -2;
                                #region tracing
                                if ( trace )
                                {
                                    tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "autoTarget=mainAttackTarget" ).Add( ":" ).Add( potentialTarget?.TypeData.InternalName ?? "null" );
                                }
                                #endregion
                            }
                            else 
                            if ( targetIndex == -1 ) //try to attack the FRD focus of this system
                            {
                                debugStage = 7500;
                                if ( frdAttackTarget == null || alreadyFiredAtFRDAttackTarget || frdAttackTarget == mainAttackTarget )
                                {
                                    targetIndex++; //can't attack the same thing more than once
                                    continue;
                                }
                                potentialTarget = frdAttackTarget;
                                currentActualTargetIndex = -1;
                                #region tracing
                                if ( trace )
                                {
                                    tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "autoTarget=frdAttackTarget" ).Add( ":" ).Add( potentialTarget?.TypeData.InternalName ?? "null" );
                                }
                                #endregion
                            }
                            else
                            {
                                if(outerLoop > 0 && targetIndex == 0)//if this is the start of the 2nd outer loop, but the first time iterating over the actual target list, skip ahead if necessary
                                {
                                    targetIndex = startingTargetIndex;
                                }
                                debugStage = 7600;
                                if ( cloakingPointsIfMobileNPC > 0 && numberOfShotsFired == 0 )
                                    return false; //don't fire at all if we don't have a direct target that we can hit and we're cloaked

                                potentialTarget = System.TryGetPotentialTargetByPriorityForSystemAtIndex( targetIndex );
                                currentActualTargetIndex = targetIndex;
                                if ( potentialTarget == null || potentialTarget == frdAttackTarget || potentialTarget == mainAttackTarget )
                                {
                                    targetIndex++; //can't attack the same thing more than once
                                    continue;
                                }
                                #region tracing
                                if ( trace )
                                {
                                    tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "autoTarget=System.targetPriorityList[targetIndex].GetSquad()" ).Add( ":" ).Add( potentialTarget?.TypeData.InternalName ?? "null" );
                                }
                                #endregion
                            }
                            
                            debugStage = 8000;
                            //systemParent.DebugText += " attempt";
                            #region tracing
                            if ( trace )
                            {
                                tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "targetIndex" ).Add( ":" ).Add( targetIndex );
                                //tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "targetSquadID" ).Add( ":" ).Add( targetPriority.TargetSquadID );
                            }
                            #endregion
                            debugStage = 8100;

                            bool isInvalid = false;
                            try
                            {
                                isInvalid = potentialTarget == null || potentialTarget.HasBeenRemovedFromSim || potentialTarget.ToBeRemovedAtEndOfThisFrame ||
                                    potentialTarget.SecondsSpentAsRemains > 0 || potentialTarget.GetHasBeenDestroyed() ||
                                    potentialTarget.PlanetFaction == null || potentialTarget.GetFactionTypeSafe() == FactionType.NaturalObject;
                            }
                            catch { isInvalid = true; }

                            if ( isInvalid )
                            {
                                debugStage = 8200;
                                //if ( potentialTarget != null )//&& potentialTarget.TypeData.InternalName.Contains( "Spider" ) )
                                //    systemParent.DebugText += potentialTarget.HasBeenRemovedFromSim + " " + potentialTarget.ToBeRemovedAtEndOfThisFrame + " " + potentialTarget.SecondsSpentAsRemains + " " + potentialTarget.TypeData.InternalName;
                                //else
                                //    systemParent.DebugText += " null target!";
                                #region tracing
                                if ( trace ) tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "target null, done here" );
                                #endregion
                                //this thing is dead or dying, don't shoot it and instead shoot something else if we can
                                if ( targetIndex >= 0 )
                                    System.TryRemovePotentialTargetByPriorityForSystemAtIndex( targetIndex ); //don't increment targetIndex if was an actual target
                                else
                                    targetIndex++; //do increase targetIndex if it was an auto-target from less than zero
                                continue;
                            }
                            
                            debugStage = 8300;
                            if ( potentialTarget.Planet != myPlanet )
                            {
                                debugStage = 8400;
                                //systemParent.DebugText += potentialTarget.GetPlanetName_Safe() + " " + myPlanet.Name + " " + potentialTarget.TypeData.InternalName;
                                //If you suspect foul play with a background thread putting junk in here, uncomment this.  Results on 8/29/2018 were good.
                                //ArcenDebugging.ArcenDebugLog( "ERROR: Firing between planets! " + typeData.InternalName + " tried to shoot a " + autoTarget.TypeData.InternalName +
                                //    " from planet " + ( myPlanet == null ? "NULL" : myPlanet.Name ) + " to planet " + ( autoTarget.Planet == null ? "NULL" : autoTarget.GetPlanetName_Safe() ), Verbosity.ShowAsError );

                                //this thing left the current planet, don't shoot it and instead shoot something else if we can
                                if ( targetIndex >= 0 )
                                    System.TryRemovePotentialTargetByPriorityForSystemAtIndex( targetIndex ); //don't increment targetIndex if was an actual target
                                else
                                    targetIndex++; //do increase targetIndex if it was an auto-target from less than zero
                                continue;
                            }
                            
                            debugStage = 8500;
                            if ( careAboutOverkill )
                            {
                                debugStage = 8600;
                                //if it would be overkilled and it's not under a shield, ignore it for now.
                                if ( !potentialTarget.GetIsProtectedByAnyForcefield() && potentialTarget.GetExpectsToBeOverkilled( 0 ) )
                                {
                                    targetIndex++; //we will skip it, but we will not take it out of our list just yet since the overkill may not really happen
                                    continue;
                                }
                            }
                            
                            debugStage = 9000;
                            //if the target should be unattackable by the weapon, ignore it for now.
                            if ( potentialTarget.TypeData.IsMobile && typeData.OnlyTargetsStaticUnits || !potentialTarget.TypeData.IsMobile && typeData.OnlyTargetsMobileUnits )
                            {
                                debugStage = 9100;
                                if ( targetIndex >= 0 )
                                    System.TryRemovePotentialTargetByPriorityForSystemAtIndex( targetIndex ); //don't increment targetIndex if was an actual target
                                else
                                    targetIndex++; //do increase targetIndex if it was an auto-target from less than zero
                                continue;
                            }
                            
                            debugStage = 9200;
                            //if the target is invulnerable, ignore for now.
                            isInvalid = false;
                            try
                            {
                                isInvalid = (potentialTarget.CountOfEntitiesProvidingExternalInvulnerability > 0 &&
                                potentialTarget.CountOfEntitiesProvidingExternalInvulnerability >= potentialTarget.TypeData.ExternalInvulnerabilityUnitRequiredCount) ||
                                !potentialTarget.TypeData.ShipClass.CanBeDamaged || potentialTarget.Debug_IgnoresDamage;
                            }
                            catch { isInvalid = true; }

                            if ( isInvalid )
                            {
                                debugStage = 9300;
                                targetIndex++;
                                continue;
                            }
                            
                            debugStage = 10100;
                            //if there are any damage modifiers, and any of them would result in a 0x damage, ignore for now.
                            if ( typeData.OutgoingDamageModifiers_FullList.Count > 0 )
                            {
                                debugStage = 10200;
                                bool skip = false;
                                bool excludeFromTargets = false;
                                DamageModifier mod;
                                int mark = systemParent.CurrentMarkLevel;
                                debugStage = 10300;
                                for ( int i = 0; i < typeData.OutgoingDamageModifiers_FullList.Count; i++ )
                                {
                                    debugStage = 10400;
                                    mod = typeData.OutgoingDamageModifiers_FullList[i];
                                    debugStage = 10500;
                                    if ( mod.MultiplierForMark[mark] == FInt.Zero && mod.CalculateDoesMeetCriteria( potentialTarget, systemParent ) )
                                    {
                                        debugStage = 10600;
                                        skip = true;
                                        switch ( mod.BasedOn )//if the damage modifier is based on a static property (that is to say there is NO WAY this type will ever NOT apply), exclude it from the target list
                                        {
                                            case DamageModifierBasedOn.Albedo:
                                            case DamageModifierBasedOn.Armor_mm:
                                            case DamageModifierBasedOn.EnergyUsage:
                                            case DamageModifierBasedOn.Engine_gx:
                                            case DamageModifierBasedOn.Mass_tX:
                                                excludeFromTargets = true;
                                                break;
                                        }
                                        break;
                                    }
                                }
                                
                                debugStage = 10700;
                                if ( skip )
                                {
                                    debugStage = 1080;
                                    if ( excludeFromTargets && targetIndex >= 0 )
                                        System.TryRemovePotentialTargetByPriorityForSystemAtIndex( targetIndex ); //don't increment targetIndex if was an actual target
                                    else
                                        targetIndex++;
                                    continue;
                                }
                            }

                            targetIndex++;
                        }
                        
                        debugStage = 11100;

                        //the first one is probably the best
                        if ( chooseNewFRDIfPossible )
                        {
                            debugStage = 11200;
                            System.CurrentFRDTarget.SetInternalRef( potentialTarget );
                            System.TryRemovePotentialTargetByPriorityForSystemAtIndex( currentActualTargetIndex );
                            currentActualTargetIndex = -2;//make it invalid for swapping later on
                            chooseNewFRDIfPossible = false;
                        }

                        debugStage = 11300;
                        if ( !System.GetIsTargetInRange( potentialTarget, RangeCheckType.ForActualFiring ) )
                        {
                            debugStage = 11400;
                            hadAnyOutOfRange = true;
                            //systemParent.DebugText += " not in range";
                            continue; //not all of them will be in range!  This is ok and expected.  Possibly none of them will be in range!
                        }

                        debugStage = 11500;
                        //the first one is probably the best -- difference is MUST be in range
                        if ( chooseNewAttackMoveIfPossible )
                        {
                            debugStage = 11600;
                            System.CurrentFRDTarget.SetInternalRef( potentialTarget );
                            System.TryRemovePotentialTargetByPriorityForSystemAtIndex( currentActualTargetIndex );
                            currentActualTargetIndex = -1;//make it invalid for swapping later on
                            chooseNewAttackMoveIfPossible = false;
                        }

                        debugStage = 11700;
                        shotCount = Math.Min( System.GetMaxCompressedShotsAtThisTarget( potentialTarget ), totalShotsToFire - numberOfShotsFired );
                        if ( shotCount <= 0 )
                        {
                            debugStage = 11710;
                            targetIndex++;
                            continue; //this is an invalid target, and we're just find out about it late, which is fine.
                        }

                        debugStage = 11720;

                        if ( careAboutOverkill && !potentialTarget.GetIsProtectedByAnyForcefield() )
                        {
                            estimatedDamage = System.GetAttackPowerAgainst( potentialTarget, tracingBuffer, true, 0, shotCount );
                            targetDurability = potentialTarget.EstimateRemainingDurabilityAfterAllShots( estimatedDamage );

                            if ( trace )
                            {
                                tracingBuffer.Add( "\nFor " ).Add( systemParent.ToString() ).Add( " shooting " ).Add( potentialTarget.ToString() ).Add( " with " ).Add( potentialTarget.ExtraStackedSquadsInThis )
                                    .Add( " extra stacks: Planning " ).Add( shotCount ).Add( " shots with " ).Add( estimatedDamage ).Add( " est. damage for " ).Add( targetDurability );
                            }

                            if ( targetDurability < 0 )//if we were about to overkill the target, see if we can't reduce the shot count
                            {
                                //See how many we can kill if we reduce our shot count
                                damagePerShot = estimatedDamage / shotCount;
                                if ( damagePerShot * shotCount < estimatedDamage )
                                    damagePerShot++;//round up so it does not underestimate the damage per shot, it's better to overestimate for the following calculations
                                if ( damagePerShot <= 0 )
                                {
                                    targetIndex++;
                                    continue; //this is an invalid target, and we're just find out about it late, which is fine.
                                }
                                shotsThatCanBeCut = -targetDurability / damagePerShot;
                                if ( shotsThatCanBeCut > 0 )
                                {
                                    if ( trace )
                                    {
                                        tracingBuffer.Add( "\nFor " ).Add( systemParent.ToString() ).Add( " shooting " ).Add( potentialTarget.ToString() ).Add( " seeing if we can reduce the shot count: Old: " )
                                            .Add( shotCount ).Add( ", reduced by " ).Add( shotsThatCanBeCut ).Add( " at " ).Add( damagePerShot ).Add( " damage/shot. This would result in " )
                                            .Add( potentialTarget.EstimateRemainingDurabilityAfterAllShots( damagePerShot * (shotCount - shotsThatCanBeCut) ) ).Add( " remaining durability" );
                                    }
                                    shotCount -= shotsThatCanBeCut;
                                }
                            }
                        }

                        #region tracing
                        if ( trace )
                        {
                            tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "shotCount (shot compression)" ).Add( ":" ).Add( shotCount ).Add(" (ShotsPerTarget: ").Add(System.DataForMark.ShotsPerSalvo)
                                .Add(", ExtraStackedSquadsInThis: ").Add(potentialTarget.ExtraStackedSquadsInThis).Add(", totalShotsToFire: ").Add(totalShotsToFire)
                                .Add(", numberOfShotsFired: ").Add(numberOfShotsFired).Add(", remaining: ").Add(totalShotsToFire - numberOfShotsFired).Add(")");
                        }
                        #endregion

                        debugStage = 12100;

                        if ( shotCount <= 0 )
                        {
                            debugStage = 12110;
                            targetIndex++;
                            continue; //this is an invalid target, and we're just find out about it late, which is fine.
                        }

                        debugStage = 12200;

                        //systemParent.DebugText += " FIRE!";
                        InternalCreateActualShotForSalvo( ref hasPlayedSoundYet, ref runningDelay, potentialTarget, originPoint,
                                Context.GetHostOnlyContext(), System, trace, tracingBuffer, shot_delay_min, shot_delay_max, 0, shotCount, false, DoShotsAllInstaHit );
                        targetPoint = potentialTarget.WorldLocation;

                        numberOfShotsFired += shotCount;

                        debugStage = 12205;
                        if ( outerLoop == 0 )
                        {
                            if ( currentActualTargetIndex >= 0 )
                            {
                                if ( startingTargetIndex != currentActualTargetIndex )
                                {
                                    debugStage = 12210;
                                    //Swap the target fired upon to the front section of the targets so it can be skipped in the 2nd loop
                                    System.TrySwapSquadsInPotentialTargetByPriorityForSystemAtIndices( startingTargetIndex, currentActualTargetIndex );
                                }
                                startingTargetIndex++;
                            }
                            //if the main attack target was fired upon, skip it for loop 2
                            else if ( currentActualTargetIndex == -2 )
                            {
                                alreadyFiredAtMainAttackTarget = true;
                            }
                            //if the main attack target was fired upon, skip it for loop 2
                            else if ( currentActualTargetIndex == -1 )
                            {
                                alreadyFiredAtFRDAttackTarget = true;
                            }
                        }
                        
                    } //end while looop
                } //end outerloop

                //Interlocked.Increment( ref Engine_Universal.Accumulator_CUS6 );

                debugStage = 13100;
                if ( numberOfShotsFired > 0 )
                {
                    //Interlocked.Increment( ref Engine_Universal.Accumulator_CUS7 );
                    debugStage = 13200;
                    if ( System.LastGameSecondIFiredShot < World_AIW2.Instance.GameSecond )
                    {
                        System.LastGameSecondIFiredShot = World_AIW2.Instance.GameSecond;
                        //point ourselves at the last target we shot at, if we need to
                        if ( systemParentTypeData.NonSimRotatesToFaceTargets && targetPoint != originPoint )
                        {
                            systemParent.CurrentAngle = originPoint.GetAngleToDegrees( targetPoint ).Add( systemParentTypeData.AnglesOffsetFromStartToAddOrSubtractWhenFacingTarget );
                            systemParent.NonSim_TimeCurrentAngleLastSet = ArcenTime.TimeSinceStartF;
                        }
                    }

                    if (System.TypeData.ChargeOps != null)
                    {
                        foreach (var op in System.TypeData.ChargeOps)
                        {
                            var idx = op.OpTarget.Index;
                            var C = systemParent.ChargeAmounts[idx];

                            var amount = C.Amount;
                            if (op.OpType == ChargeOp.Type.Add)
                            {
                                amount += op.RVal;
                            }

                            int max = systemParent.TypeData.ChargeTypes[idx].NumMax;
                            if (amount > max)
                                amount = max;
                            if (amount < 0)
                                amount = 0;
                            /*
                            ArcenDebugging.ArcenDebugLogNoDateOrAnything( 
                                string.Format("{0} fired salvo for system {1} with chargeop. [{6}] amount {2}+{3} max {4} -> {5}",
                                    systemParent.ToString(), System.TypeData.InternalName_Original, C.Amount, op.RVal, max, amount, idx),
                                DebugLogDestination.ArcenDebugLog, Verbosity.DoNotShow);
                            */

                            C.Amount = amount;
                            systemParent.ChargeAmounts[idx] = C;
                        }
                    }

                    // OLD LOGIC: if could not fire full salvo, do partial refund on reload timer
                    // Chris's new logic, for now at least: you just are out of luck, better luck next time. ;)
                    //if ( numberOfShotsFired < totalShotsToFire && totalShotsToFire > 0 )
                    //{
                    //    FInt percentUnused = ( (FInt)totalShotsToFire - (FInt)numberOfShotsFired ) / (FInt)totalShotsToFire;
                    //    System.TimeUntilNextShot -= percentUnused * (FInt)System.GetWeaponReloadTime(true);
                    //}

                    debugStage = 13300;
                    if ( systemParent.GetMaxCloakingPoints() > 0 )
                    {
                        debugStage = 13400;
                        if ( systemParent.GetCurrentCloakingPoints() > 0 )
                        {
                            debugStage = 13500;
                            int pointsLost = (systemParent.GetMaxCloakingPoints() * typeData.CloakingPercentLossFromFiring).GetNearestIntPreferringHigher();
                            if ( pointsLost < 1 )
                                pointsLost = 1;
                            systemParent.CloakingPointsLost += pointsLost;
                        }
                        debugStage = 13600;
                        systemParent.GameSecondOfLastCloakingPointLoss = World_AIW2.Instance.GameSecond;
                    }
                    debugStage = 14100;
                    // Puffin thing. This is like the normal self damage, but it has no connection to the damage actually done by the system.
                    // It instead deals a percentage of the units total health as damage each time it even fires. Intended for Minefields, Railpods, so on.
                    if ( System.TypeData.HealthChangeByMaxHealthDividedByThisPerAttack != FInt.Zero )
                    {
                        int numberOfSquadsFiring = (int)Math.Ceiling(numberOfShotsFired / (float)System.DataForMark.ShotsPerSalvo);
                        int healthChange = ((systemParent.GetMaxHullPoints() + systemParent.GetMaxShieldPoints()) * numberOfSquadsFiring / System.TypeData.HealthChangeByMaxHealthDividedByThisPerAttack).IntValue;
                        if (trace) {
                            tracingBuffer.Add( "\n\t" ).Add( "numberOfSquadsFiring:" ).Add( numberOfSquadsFiring ).Add(" healthChange:").Add(healthChange);
                        }
                        debugStage = 14200;
                        if ( healthChange < 0)
                        {
                            debugStage = 14300;
                            systemParent.TakeDamageDirectly( -healthChange, null, null, DamageSource.SelfDamageFromMyOwnWeapons, Context );
                            if ( systemParentTypeData.CustomRepairImpossibleForSecondsAfterDamagedByEnemy_Max15 > 0 )
                                systemParent.RepairImpossibleForSeconds = (byte)systemParentTypeData.CustomRepairImpossibleForSecondsAfterDamagedByEnemy_Max15;
                            else
                                systemParent.RepairImpossibleForSeconds = (byte)ExternalConstants.Instance.Balance_RepairImpossibleForSecondsAfterDamagedByEnemy;
                        }
                        else
                        {
                            debugStage = 14500;
                            int selfHealing = ((systemParent.GetMaxHullPoints() + systemParent.GetMaxShieldPoints()) / System.TypeData.HealthChangeByMaxHealthDividedByThisPerAttack).IntValue;
                            healthChange -= systemParent.TakeHullRepair( healthChange );
                            if ( healthChange > 0 )
                                systemParent.TakeShieldRepair( healthChange );
                        }
                    }
                    
                    return true;
                }
                
                //no shot was fired!
                
                //Interlocked.Increment( ref Engine_Universal.Accumulator_CUS7 );
                debugStage = 15100;
                if ( hadAnyOutOfRange )
                    System.LastShotFireAbortCode = FireAbortCode.NothingInRange;
                else
                    System.LastShotFireAbortCode = FireAbortCode.NothingViable;
                
                debugStage = 15200;
                
                //in cases where we didn't fire a shot, wait a random amount of time between 0 and 0.2 seconds before trying again
                //this adds more flavor into how ships shoot, for one, but it also prevents hammering this method every frame
                // jcf: this appears in the tooltip for the weapon activity as not ready to fire, flickering on/off
                //      so, lets not...
                //System.TimeUntilNextShot = FInt.FromParts( 0, Context.RandomToUse.Next( 0, 200 ) );
                
                return false;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "ActuallyFireSalvoAtTargetPriorityList error at debugStage " + debugStage + ", error: " + e, Verbosity.ShowAsError );
                return false;
            }
            //finally
            //{
            //    Interlocked.Increment( ref Engine_Universal.Accumulator_CUB2 );
            //}
        }

        #region ActuallyFireSalvoFromOnDeath
        public void ActuallyFireSalvoFromOnDeath( ArcenHostOnlySimContext Context, EntitySystem System, int shotCount, bool trace, ArcenCharacterBuffer tracingBuffer, bool DoShotsAllInstaHit )
        {
            if ( Context == null ) //client
                return; 
            bool hasPlayedSoundYet = false;
            FInt runningDelay = FInt.Zero;
            ArcenPoint originPoint = System.GetWorldLocation();
            InternalCreateActualShotForSalvo( ref hasPlayedSoundYet, ref runningDelay, System.ParentEntity, originPoint,
                Context, System, trace, tracingBuffer, 1, 3, 0, shotCount, false, DoShotsAllInstaHit );
        }
        #endregion

        #region InternalCreateActualShotForSalvo
        private void InternalCreateActualShotForSalvo( ref bool hasPlayedSoundYet, ref FInt runningDelay, GameEntity_Squad target, ArcenPoint originPoint,
            ArcenHostOnlySimContext Context, EntitySystem System, bool trace, ArcenCharacterBuffer tracingBuffer, int withinSalvoLowEnd, int withinSalvoHighEnd, 
            int OverridingDamage, int shotCount, bool IsReturnFireShot, bool DoShotsAllInstaHit )
        {
            if ( Context == null ) //client
                return;
            int debugStage = 0;
            try
            {
                if ( System == null )
                    return;
                EntitySystemTypeData typeData = System.TypeData;
                if ( typeData == null )
                    return;
                if ( target == null )
                    return;
                GameEntity_Squad systemParent = System.ParentEntity;
                if ( systemParent == null )
                    return;
                Planet systemParentPlanet = systemParent.Planet;
                if ( systemParentPlanet == null )
                    return;
                if ( shotCount <= 0 )
                    return;

                debugStage = 1000;

                if ( target != null && target.AreaBoosters_ShotEvaluated.Count > 0 )
                {
                    debugStage = 1100;
                    List<GameEntity_Squad> boosters = target.AreaBoosters_ShotEvaluated.GetDisplayList();
                    foreach ( GameEntity_Squad ship in boosters )
                    {
                        if ( ship == null )
                            continue;
                        if ( ship.TypeData.DoesAttractShotsAgainstAllies ) //if protected by a ship that attracts in shots, then make the shot go to that target
                        {
                            target = ship;
                            break;
                        }
                    }
                }
                debugStage = 1500;

                bool doInstaHit = false;
                if ( DoShotsAllInstaHit )
                    doInstaHit = true;

                debugStage = 2000;
                
                #region Check To See If Relevant: On Background Planets, Shots Should Insta-Hit And Not Spawn Entities
                if ( !doInstaHit && 
                     systemParent != null && 
                     systemParentPlanet != null && 
                     systemParentPlanet.BattleStatus_ShotsInstaHitUnlessPlayer )
                {
                    debugStage = 2100;
                    if ( systemParent.GetFactionTypeSafe() != FactionType.Player )
                    {
                        debugStage = 2200;
                        if ( target == null || 
                             target.GetFactionTypeSafe() != FactionType.Player )
                        {
                            //we're firing at something OTHER than a player ship, and we're not a player ship, so  do the thing
                            //It IS Relevant, So Do The Insta-Hit
                            doInstaHit = true;
                        }
                    }
                }
                #endregion

                debugStage = 3100;
                if ( doInstaHit )
                {
                    debugStage = 3200;
                    int compressedShots = System.GetMaxCompressedShotsAtThisTarget( target );
                    if ( !this.CheckForShotAOEDetonation( System, compressedShots, System, target, Context ) )
                    {
                        debugStage = 3300;
                        this.DoShotHitLogic( System, compressedShots, System, target, Context );
                    }
                    return;
                }

                debugStage = 4100;
                if ( /*typeData.ShotPickerDef != null || */
                    ( typeData.ShotTypeData != null && typeData.ShotTypeData.Category == GameEntityCategory.Ship ) )
                {
                    debugStage = 4200;

                    debugStage = 4400;
                    bool playSound = false;
                    if ( !hasPlayedSoundYet )
                    {
                        debugStage = 5200;
                        hasPlayedSoundYet = true; //otherwise we spam the queue and I'm just going to discard them anyway!
                        playSound = true;
                    }

                    debugStage = 4420;
                    if ( typeData.FiresSalvoSequentially )
                    {
                        debugStage = 4430;
                        runningDelay += FInt.FromParts( 0, Context.RandomToUse.NextInclus( withinSalvoLowEnd, withinSalvoHighEnd ) );
                    }

                    debugStage = 4440;
                    
                    /*
                    if (typeData.ShotPickerDef != null)
                    {
                        foreach (var r in System.ShotPicker.Pick<GameEntityTypeData>(Context))
                        {
                            var droneGunShot = new DroneGunShot()
                            {
                                Type = r.Object,
                                Count = r.Count * shotCount,
                                Target = SafeSquadWrapper.Create(target),
                                Delay = runningDelay,
                                System = System,
                                PlaySound = playSound,
                            };

                            systemParent.PendingDroneGunShots.Add(droneGunShot);
                        }
                    }
                    else
                    */
                    {
                        var droneGunShot = new DroneGunShot()
                        {
                            Type = typeData.ShotTypeData,
                            Count = shotCount,
                            Target = SafeSquadWrapper.Create(target),
                            Delay = runningDelay,
                            System = System,
                            PlaySound = playSound,
                        };
                        
                        systemParent.PendingDroneGunShots.Add(droneGunShot);
                    }

                    return;
                }
                
                // normal shots
                {
                    debugStage = 8100;
                    GameEntity_Shot newShot = SpawnShot_ReturnNullIfMPClient( System, originPoint, shotCount, Context );
                    debugStage = 8200;
                    if ( newShot == null )
                        return; //if we are a client
                    debugStage = 8300;
                    #region tracing
                    if ( trace ) tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "spawned actual shot:" ).Add( newShot.PrimaryKeyID ).Add( ":" ).Add( newShot.TypeData.InternalName ).Add( " from system index:" ).Add( newShot.OriginSystemIndex );
                    #endregion
                    newShot.SetTarget( target );

                    debugStage = 8400;
                    if ( OverridingDamage > 0 )
                        newShot.OverridingDamageToDo = OverridingDamage;
                    newShot.IsReturnFireShot = IsReturnFireShot;

                    //ArcenDebugging.ArcenDebugLog( "Fire shot: " + systemParent.TypeData.DisplayName + " at " + target.TypeData.DisplayName, Verbosity.DoNotShow );

                    debugStage = 8500;
                    if ( typeData.FiresSalvoSequentially )
                    {
                        runningDelay += FInt.FromParts( 0, Context.RandomToUse.NextInclus( withinSalvoLowEnd, withinSalvoHighEnd ) );
                        newShot.RemainingDelayUntilEntersSim = runningDelay;
                    }
                    else if (newShot.RemainingDelayUntilEntersSim != FInt.Zero)
                    {
                        LOG.Msg("newShot.RemainingDelayUntilEntersSim appears to not have been reset!");
                        newShot.RemainingDelayUntilEntersSim = FInt.Zero;
                    }

                    debugStage = 8600;
                    if ( !hasPlayedSoundYet )
                    {
                        debugStage = 8700;
                        hasPlayedSoundYet = true; //otherwise we spam the queue and I'm just going to discard them anyway!

                        //Chris says: we used to just directly call PlayJustFiredSound().
                        //But now we set Network_PlayFiringSoundDuringFirstSimLoop, which will make sure it also happens on the client. 
                        newShot.Network_PlayFiringSoundDuringFirstSimLoop = true;
                        //Chris says: it was possible that we would wind up processing the shot on the same frame, previously.
                        //That might have been glitchy even in single player, but in MP it definitely would have caused missing animations and sounds on clients.
                        //This is the safer way to be sure shots don't act unpredictably.
                        newShot.Network_FrameToStartAnyProcessing = World_AIW2.Instance.Network_CurrentFrameNumber + 1;
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "InternalCreateActualShotForSalvo error at debugStage " + debugStage + ", error: " + e, Verbosity.ShowAsError );
            }
        }
        #endregion
        public override bool DoShotHitLogic( IShotHitSource ShotHitNeverNull, int CompressedShots, EntitySystem OriginSystemForShot, GameEntity_Squad Target, ArcenSimContextAnyStatus Context )
        {
            int dummy1, dummy2;
            return DoShotHitLogic_Inner( ShotHitNeverNull, CompressedShots, OriginSystemForShot, Target, false, FInt.OneHundred, out dummy1, out dummy2, Context );
        }

        public override bool DoShotHitLogic( IShotHitSource ShotHitNeverNull, int CompressedShots, EntitySystem OriginSystemForShot, GameEntity_Squad Target, bool HonorFiniteHitCountAOE, ArcenSimContextAnyStatus Context )
        {
            int dummy1, dummy2;
            return DoShotHitLogic_Inner( ShotHitNeverNull, CompressedShots, OriginSystemForShot, Target, HonorFiniteHitCountAOE, FInt.OneHundred, out dummy1, out dummy2, Context );
        }

        public override bool DoShotHitLogic( IShotHitSource ShotHitNeverNull, int CompressedShots, EntitySystem OriginSystemForShot, GameEntity_Squad Target, FInt PercentOfTotalAttackPowerForThisHitOutOf100, out int TotalDamageDealt, ArcenSimContextAnyStatus Context )
        {
            int dummy;
            return DoShotHitLogic_Inner( ShotHitNeverNull, CompressedShots, OriginSystemForShot, Target, false, PercentOfTotalAttackPowerForThisHitOutOf100, out TotalDamageDealt, out dummy, Context );
        }

        public override bool DoShotHitLogic( IShotHitSource ShotHitNeverNull, int CompressedShots, EntitySystem OriginSystemForShot, GameEntity_Squad Target, bool HonorFiniteHitCountAOE, FInt PercentOfTotalAttackPowerForThisHitOutOf100, ArcenSimContextAnyStatus Context )
        {
            int dummy1, dummy2;
            return DoShotHitLogic_Inner( ShotHitNeverNull, CompressedShots, OriginSystemForShot, Target, HonorFiniteHitCountAOE, PercentOfTotalAttackPowerForThisHitOutOf100, out dummy1, out dummy2, Context );
        }

        public override bool DoShotHitLogic( IShotHitSource ShotHitNeverNull, int CompressedShots, EntitySystem OriginSystemForShotOrNull, GameEntity_Squad Target, bool HonorFiniteHitCountAOE, 
            FInt PercentOfTotalAttackPowerForThisHitOutOf100, out int TotalDamageDealt, out int ActualCompressedShotsHit, ArcenSimContextAnyStatus Context )
        {
            return DoShotHitLogic_Inner( ShotHitNeverNull, CompressedShots, OriginSystemForShotOrNull, Target, HonorFiniteHitCountAOE, PercentOfTotalAttackPowerForThisHitOutOf100, 
                out TotalDamageDealt, out ActualCompressedShotsHit, Context );
        }

        private bool DoShotHitLogic_Inner( IShotHitSource ShotHitNeverNull, int CompressedShots, EntitySystem OriginSystemForShotOrNull, GameEntity_Squad Target, bool HonorFiniteHitCountAOE,
            FInt PercentOfTotalAttackPowerForThisHitOutOf100, out int TotalDamageDealt, out int ActualCompressedShotsHit, ArcenSimContextAnyStatus Context )
        {
            int debugStage = 0;
            try
            {
                TotalDamageDealt = 0;
                ActualCompressedShotsHit = 0;
                
                debugStage = 100;
                if ( OriginSystemForShotOrNull != null && !OriginSystemForShotOrNull.GetCanHitByDesireOrNot_AndIAlreadyKnowIAmAWeapon( Target ) )
                    return false;
                if ( Target == null )
                    return false;

                if ( ShotHitNeverNull == null )
                {
                    ArcenDebugging.ArcenDebugLog( "DoShotHitLogic: Null ShotHitNeverNull!", Verbosity.ShowAsError );
                    return false;
                }

                PlanetFaction shotFaction = ShotHitNeverNull.GetPlanetFaction();
                Planet shotPlanet = ShotHitNeverNull.GetPlanet();
                StateOfMatterTypeData shotStateOfMatter = ShotHitNeverNull.GetCurrentStateOfMatter();

                if ( Target.CurrentStateOfMatter != shotStateOfMatter )
                {
                    return false;
                }

                debugStage = 200;
                bool wasAlive = !Target.GetHasBeenDestroyed();

                //ArcenDebugging.ArcenDebugLog( OriginSystemForShot.ParentEntity.TypeData.DisplayName + " against " + Target.TypeData.DisplayName + " initial check", Verbosity.DoNotShow );

                debugStage = 300;
                GameEntity_Squad protectingShieldThatTookTheHit = null;
                bool foundProtectorButCouldNotHitDueToFiniteHitCountAOE = false;
                bool doShotsAllInstaHit = AIWar2GalaxySettingQuickAccess.ShotsAllInstaHit;

                if ( OriginSystemForShotOrNull != null )
                {
                    debugStage = 400;
                    OriginSystemForShotOrNull.LastGameSecondMyShotHit = World_AIW2.Instance.GameSecond;
                    OriginSystemForShotOrNull.LastTotalDamageMyShotDidCaused = 0;
                    OriginSystemForShotOrNull.LastDamageAbortCode = 0;
                }

                {
                    #region tracing
                    bool trace = false;
                    if ( Engine_AIW2.TraceAtAll )
                    {
                        if ( Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ShotHitLogic ) )
                            trace = true;
                        else 
                        if ( Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.IndividualLogic ) && 
                             Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Firing ) && 
                             OriginSystemForShotOrNull != null && 
                             OriginSystemForShotOrNull.ParentEntity == GameEntity_Base.CurrentlyHoveredOver )
                        { 
                            trace = true;
                        }
                    }
                    ArcenCharacterBuffer tracingBuffer = null;
                    if ( trace ) tracingBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "EntitySimLogicImpl-DoShotHitLogic_Inner-trace", 10f );
                    bool trace_origin = trace && OriginSystemForShotOrNull != null;
                    if ( trace_origin ) 
                    {
                        tracingBuffer
                            .Add( "Tracing DoHitLogic for shot from " ).Add( OriginSystemForShotOrNull.TypeData.InternalName_Longer )
                            .Add( " from " ).Add(1 + OriginSystemForShotOrNull.ParentEntity.ExtraStackedSquadsInThis).Add("x ")
                            .Add( OriginSystemForShotOrNull.ParentEntity.TypeData.InternalName ).Add( " #" ).Add( OriginSystemForShotOrNull.ParentEntity.PrimaryKeyID );
                    }
                    #endregion

                    debugStage = 500;
                    protectingShieldThatTookTheHit = this.FindProtectingForcefieldToHitInsteadOfTarget( ShotHitNeverNull, OriginSystemForShotOrNull, Target, HonorFiniteHitCountAOE,
                        out foundProtectorButCouldNotHitDueToFiniteHitCountAOE, Context );

                    //ArcenDebugging.ArcenDebugLog( OriginSystemForShot.ParentEntity.TypeData.DisplayName + " " + OriginSystemForShot.TypeData.FiresThroughEnemyShields + " " + 
                    //    (protectingShieldThatTookTheHit == null ? "null" : protectingShieldThatTookTheHit.TypeData.DisplayName ), Verbosity.DoNotShow );

                    debugStage = 600;
                    int actualDamageDone = 0, damageAbortCode = 0, attackPowerAgainstThisTarget = 0, adjustedAttackPower = 0;

                    int theoreticalEntitiesHit;
                    if ( protectingShieldThatTookTheHit != null ) //if there's a forcefield protecting us and we can hit it
                    {
                        debugStage = 1000;
                        #region tracing
                        if ( trace_origin ) 
                        { 
                            tracingBuffer
                                .Add( " against " ).Add( 1 + protectingShieldThatTookTheHit.ExtraStackedSquadsInThis ).Add( "x " )
                                .Add( protectingShieldThatTookTheHit.TypeData.InternalName ).Add( " #" ).Add( protectingShieldThatTookTheHit.PrimaryKeyID );
                        }
                        #endregion
                        
                        DoInternalExtraShotHitLogic( ShotHitNeverNull, OriginSystemForShotOrNull, protectingShieldThatTookTheHit, HonorFiniteHitCountAOE,
                            PercentOfTotalAttackPowerForThisHitOutOf100, Context, out theoreticalEntitiesHit, out actualDamageDone, out damageAbortCode, CompressedShots,
                            out ActualCompressedShotsHit, out attackPowerAgainstThisTarget, out adjustedAttackPower, doShotsAllInstaHit, ref debugStage, ref tracingBuffer, ref trace );

                        Target.LastTimeTakenDamageFromBeingShotByAnyone = World_AIW2.Instance.GameSecond;//even though I have been protected I aggro
                    }
                    else 
                    if ( foundProtectorButCouldNotHitDueToFiniteHitCountAOE ) //if there's a forcefield protecting us and we cannot hit it because we already did
                    {
                        //do... nothing I guess?  This is what we always used to do
                    }
                    else //no forcefield protecting me, so we just shoot the thing
                    {
                        debugStage = 2000;
                        #region tracing
                        if ( trace_origin ) 
                        { 
                            tracingBuffer
                                .Add( " against " ).Add( 1 + Target.ExtraStackedSquadsInThis ).Add( "x " ).Add( Target.TypeData.InternalName ).Add( " #" ).Add( Target.PrimaryKeyID );
                        }
                        #endregion
                        debugStage = 2100;
                        DoInternalExtraShotHitLogic( ShotHitNeverNull, OriginSystemForShotOrNull, Target, HonorFiniteHitCountAOE,
                            PercentOfTotalAttackPowerForThisHitOutOf100, Context.GetHostOnlyContext(), out theoreticalEntitiesHit, out actualDamageDone, out damageAbortCode, CompressedShots,
                            out ActualCompressedShotsHit, out attackPowerAgainstThisTarget, out adjustedAttackPower, doShotsAllInstaHit, ref debugStage, ref tracingBuffer, ref trace );
                    }

                    //ArcenDebugging.ArcenDebugLog( OriginSystemForShot.ParentEntity.TypeData.DisplayName + " " + OriginSystemForShot.TypeData.FiresThroughEnemyShields + " " +
                    //    (protectingShieldThatTookTheHit == null ? "nullShield" : protectingShieldThatTookTheHit.TypeData.DisplayName), Verbosity.DoNotShow );

                    debugStage = 4000;
                    if ( OriginSystemForShotOrNull != null )
                    {
                        debugStage = 4100;
                        OriginSystemForShotOrNull.LastTotalDamageMyShotDidCaused += actualDamageDone;
                        if ( damageAbortCode != 0 )
                            OriginSystemForShotOrNull.LastDamageAbortCode = (DamageAbortCode)damageAbortCode;
                    }
                    
                    debugStage = 5000;

                    #region tracing
                    if ( trace )
                    {
                        tracingBuffer.Add( "\n" ).Add( "actualDamageDone = " ).Add( actualDamageDone );
                        ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                    
                    if ( actualDamageDone <= 0 )
                        return false;
                    
                    debugStage = 5100;
                    if ( attackPowerAgainstThisTarget > 0 ) // it will be, but just to be sure
                    {
                        debugStage = 5200;
                        if ( OriginSystemForShotOrNull != null )
                        {
                            if ( OriginSystemForShotOrNull.TypeData.DamageAmplification )
                            {
                                int amp = OriginSystemForShotOrNull.TypeData.DamageAmplificationDuration_Max15;
                                if ( Target.TypeData?.ShipClass != null )
                                    amp = Target.TypeData.ShipClass.ModifyDebuff( ShipClassData_DebuffType.Acid, amp );
                                
                                debugStage = 5300;
                                
                                //no more than 15 seconds, no less than 1
                                amp = amp.Min(15).Max(1);
                                Target.IncomingDamageAmplifiedDuration_Max15 = (byte)amp;
                                
                                debugStage = 5400;
                                if ( OriginSystemForShotOrNull.DataForMark.DamageAmplificationFlat > 0 &&
                                     Target.IncomingDamageAmplifiedByFlat < OriginSystemForShotOrNull.DataForMark.DamageAmplificationFlat )
                                {
                                    Target.IncomingDamageAmplifiedByFlat = OriginSystemForShotOrNull.DataForMark.DamageAmplificationFlat;
                                }
                                
                                debugStage = 5500;
                                if ( OriginSystemForShotOrNull.TypeData.DamageAmplificationMult != FInt.One &&
                                    Target.IncomingDamageAmplifiedByMult < OriginSystemForShotOrNull.TypeData.DamageAmplificationMult )
                                {
                                    Target.IncomingDamageAmplifiedByMult = OriginSystemForShotOrNull.TypeData.DamageAmplificationMult;
                                }
                            }
                        }

                        debugStage = 5600;
                        /*
                        if ( actualDamageDone == adjustedAttackPower )
                            PercentOfTotalAttackPowerUsedForThisHitOutOf100 = PercentOfTotalAttackPowerForThisHitOutOf100; // otherwise sometimes precision errors can make it look like the whole shot was not used
                        else
                            PercentOfTotalAttackPowerUsedForThisHitOutOf100 += ((FInt)(actualDamageDone * 100) / (FInt)attackPowerAgainstThisTarget);*/
                        TotalDamageDealt = actualDamageDone;
                    }
                    
                    debugStage = 6000;
                    if ( OriginSystemForShotOrNull != null && 
                         OriginSystemForShotOrNull.TypeData.HealthChangePerDamageDealt != FInt.Zero && 
                         actualDamageDone > 0 )
                    {
                        debugStage = 6100;
                        if ( OriginSystemForShotOrNull.TypeData.HealthChangePerDamageDealt < FInt.Zero )
                        {
                            debugStage = 6200;
                            int selfDamage = (actualDamageDone * -OriginSystemForShotOrNull.TypeData.HealthChangePerDamageDealt).IntValue;
                            debugStage = 6300;
                            OriginSystemForShotOrNull.ParentEntity.TakeDamageDirectly( selfDamage, null, null, DamageSource.SelfDamageFromMyOwnWeapons, Context );
                            debugStage = 6400;
                            if ( OriginSystemForShotOrNull.ParentEntity.TypeData.CustomRepairImpossibleForSecondsAfterDamagedByEnemy_Max15 > 0 )
                                OriginSystemForShotOrNull.ParentEntity.RepairImpossibleForSeconds = (byte)OriginSystemForShotOrNull.ParentEntity.TypeData.CustomRepairImpossibleForSecondsAfterDamagedByEnemy_Max15;
                            else
                                OriginSystemForShotOrNull.ParentEntity.RepairImpossibleForSeconds = (byte)ExternalConstants.Instance.Balance_RepairImpossibleForSecondsAfterDamagedByEnemy;                           
                        }
                        else
                        {
                            debugStage = 7000;
                            int selfHealing = (actualDamageDone * OriginSystemForShotOrNull.TypeData.HealthChangePerDamageDealt).IntValue;
                            debugStage = 7100;
                            selfHealing -= OriginSystemForShotOrNull.ParentEntity.TakeHullRepair( selfHealing );
                            debugStage = 7200;
                            if ( selfHealing > 0 )
                            {
                                debugStage = 7300;
                                OriginSystemForShotOrNull.ParentEntity.TakeShieldRepair( selfHealing );
                            }
                        }
                    }
                }

                debugStage = 9000;
                World_AIW2.Instance.TotalShotsHit++;

                debugStage = 9100;
                bool wholeSquadKilled = wasAlive && Target.GetHasBeenDestroyed();
                int shipsKilled = wholeSquadKilled ? 1 : 0;

                debugStage = 9200;
                GameEntity_Shot shotOrNull = ShotHitNeverNull as GameEntity_Shot;
                if ( shotPlanet != null && shotPlanet.BattleStatus == PlanetBattleStatus.Tier1_PlayerLookingAtMe && shotOrNull != null )
                {
                    debugStage = 9300;
                    if ( shotOrNull.VisualLinkObject_GenericOnly != null || shotOrNull.InstancedRenderer != null )
                    {
                        debugStage = 9400;
                        if ( shotOrNull.InstancedRenderer != null )
                        {
                            debugStage = 9500;
                            shotOrNull.InstancedRenderer.ReactToShotHittingSquad( Target, protectingShieldThatTookTheHit, shipsKilled, wholeSquadKilled );
                        }
                        else
                        {
                            debugStage = 9600;
                            Engine_AIW2.Instance.PresentationLayer.ReactToShotHittingSquad( shotOrNull, Target, protectingShieldThatTookTheHit, shipsKilled, wholeSquadKilled );
                        }
                    }
                    else
                    {
                        debugStage = 9700;
                        if ( Target.InstancedRenderer != null )
                        {
                            debugStage = 9800;
                            Target.InstancedRenderer.ReactToShotHittingSquad( Target, protectingShieldThatTookTheHit, shipsKilled, wholeSquadKilled );
                        }
                    }
                }
                else
                {
                    debugStage = 11000;
                    if ( (shipsKilled > 0 || wholeSquadKilled) && Target.TypeData.Category == GameEntityCategory.Ship )
                    {
                        debugStage = 11100;
                        //ArcenDebugging.ArcenDebugLog( "Killed ship! " + Target.TypeData.InternalName + " InstancedRenderer = " + (Target.InstancedRenderer == null ? "null" : "ok"), Verbosity.DoNotShow );
                        Target.PlayJustDiedSoundIfNotOnCurrentLocalPlanet( Context, wholeSquadKilled );
                    }
                }

                return true;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "DoShotHitLogic_Inner A error at debugStage " + debugStage + ": " + e, Verbosity.ShowAsError );
                TotalDamageDealt = 0;
                ActualCompressedShotsHit = 0;
                return false;
            }
        }

        //simply to unify targets and shields protecting targets getting hit in one part of code
        private void DoInternalExtraShotHitLogic( 
            IShotHitSource ShotHitNeverNull, EntitySystem OriginSystemForShotOrNull, GameEntity_Squad ActualTarget, bool HonorFiniteHitCountAOE, FInt PercentOfTotalAttackPowerForThisHitOutOf100, 
            ArcenSimContextAnyStatus Context, 
            out int theoreticalEntitiesHit, out int actualDamageDone, out int damageAbortCode, int compressedShots,
            out int actualCompressedShotsHit, out int attackPowerAgainstThisTarget, out int adjustedAttackPower, 
            bool doShotsAllInstaHit, ref int debugStage, ref ArcenCharacterBuffer tracingBuffer, ref bool trace )

        {
            // The "time last in combat" value is used to check when we should play
            // audio cues. We will eventually deprecate CheckForDamageTakenAudioCue and handle
            // that in the External faction DoPerSecond code based on the TimeFactionLastInCombatOnPlanet values
            if ( !ActualTarget.suppressDeathAudioCue && !ActualTarget.suppressDeathAudioCue )
                ActualTarget.PlanetFaction.TimeFactionLastInCombatOnPlanet = World_AIW2.Instance.GameSecond;
            
            debugStage = 131000;
            ActualTarget.CheckForDamageTakenAudioCue( true, ActualTarget.TypeData.IsKingUnit );

            debugStage = 132000;
            ActualTarget.LastTimeTakenDamageFromBeingShotByAnyone = World_AIW2.Instance.GameSecond;
            
            if ( OriginSystemForShotOrNull == null )
            {
                theoreticalEntitiesHit = Math.Min( ActualTarget.ExtraStackedSquadsInThis, compressedShots );
                actualDamageDone = 0;
                damageAbortCode = 0;
                attackPowerAgainstThisTarget = 0;
                adjustedAttackPower = 0;
                actualCompressedShotsHit = 0;
                return;
            }
            
            debugStage = 133000;
            int shotsPerTarget = OriginSystemForShotOrNull.DataForMark.ShotsPerTarget;
            
            // This can be 0 sometimes, presuamably due to some sort of funky race condition.
            // Just avoid the error (no balance thought was given).
            if ( shotsPerTarget == 0 )
                shotsPerTarget = 1;
            
            // Divide the shots among as few target theoretical entities as possible.
            theoreticalEntitiesHit = compressedShots / shotsPerTarget;
            
            // Add 1 more if integer division forgot the rest.
            if ((theoreticalEntitiesHit * OriginSystemForShotOrNull.DataForMark.ShotsPerTarget) < compressedShots)
                theoreticalEntitiesHit++;
            
            // This shouldn't happen, but just to be sure.
            if ( theoreticalEntitiesHit > ActualTarget.ShipCount )
                theoreticalEntitiesHit = ActualTarget.ShipCount;

            debugStage = 134000;
            attackPowerAgainstThisTarget = OriginSystemForShotOrNull.GetAttackPowerAgainst( ActualTarget, tracingBuffer, true,
                ShotHitNeverNull.GetOverridingDamageToDo(), 1 );//only calculate a single compressed shot here! The actual bonus damage is applied in the TakeDamageDirectly code!
            debugStage = 134100;
            adjustedAttackPower = ((attackPowerAgainstThisTarget * PercentOfTotalAttackPowerForThisHitOutOf100) / 100).IntValue;
            
            //lower the amount before we get into multipliers from shields or whatnot
            if ( OriginSystemForShotOrNull != null && OriginSystemForShotOrNull.TypeData.MaxDamageToDealPerTargetHitByMark.Count > 0 )
            {
                int maxToDo = OriginSystemForShotOrNull.TypeData.MaxDamageToDealPerTargetHitByMark[ActualTarget.CurrentMarkLevel];
                if ( maxToDo < attackPowerAgainstThisTarget )
                    attackPowerAgainstThisTarget = maxToDo;
            }

            #region tracing
            if ( trace ) tracingBuffer.Add( "\n" ).Add( "int adjustedAttackPower = ( ( attackPowerAgainstThisTarget " + attackPowerAgainstThisTarget + " * PercentOfTotalAttackPowerForThisHit " + PercentOfTotalAttackPowerForThisHitOutOf100 + " ) / 100 ).IntValue = " ).Add( adjustedAttackPower );
            #endregion
            debugStage = 134200;
            int unused1, unused2, unused3;
            ActualTarget.TakeDamageDirectly( adjustedAttackPower, OriginSystemForShotOrNull, ShotHitNeverNull, DamageSource.SomeSortOfEnemy, false,
                HonorFiniteHitCountAOE, OriginSystemForShotOrNull.TypeData.MaxStacksToKill, compressedShots, ShotHitNeverNull.GetExtraStacksOfSource(), false,
                out actualDamageDone, out actualCompressedShotsHit, out damageAbortCode, Context, trace ? tracingBuffer : null );

            debugStage = 134300;
            int damagePerCompressedShot = actualDamageDone;
            if ( theoreticalEntitiesHit > 1 )
            {
                damagePerCompressedShot /= theoreticalEntitiesHit;
            }

            debugStage = 135000;
            #region ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack
            if ( ActualTarget.TypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack > FInt.Zero && damagePerCompressedShot > 0 )
            {
                GameEntity_Squad firingShip = OriginSystemForShotOrNull?.ParentEntity;
                if ( firingShip != null && firingShip.TypeData.ShipClass.CanReceiveExoticDamageFrom(ShipClassData_ExoticDamageType.Electrotoxicity) )
                {
                    int damageAmount = (ActualTarget.TypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack * damagePerCompressedShot ).GetNearestIntPreferringHigher();
                    damageAmount = firingShip.TypeData.ShipClass.ModifyExoticDamage( ShipClassData_ExoticDamageType.Electrotoxicity, damageAmount );
                    firingShip.TakeDamageDirectly( damageAmount, null, null, DamageSource.SomeSortOfEnemy, false, false, 1, theoreticalEntitiesHit,
                        ShotHitNeverNull.GetExtraStacksOfSource(), false, out unused1, out unused2, out unused3, Context );
                }
            }
            #endregion
             
            debugStage = 135100;
            //Puffin Note: Neinzul Firefly weapon point setup.
            if ( OriginSystemForShotOrNull.TypeData.NumberOfWeaponPointsToGainOnFiring > 0 )
            {
                OriginSystemForShotOrNull.ParentEntity.NumberOfWeaponPoints += OriginSystemForShotOrNull.TypeData.NumberOfWeaponPointsToGainOnFiring;

                if ( OriginSystemForShotOrNull.ParentEntity.NumberOfWeaponPoints > OriginSystemForShotOrNull.ParentEntity.TypeData.MaxNumberOfWeaponPoints )
                    OriginSystemForShotOrNull.ParentEntity.NumberOfWeaponPoints = OriginSystemForShotOrNull.ParentEntity.TypeData.MaxNumberOfWeaponPoints;
            }
            
            debugStage = 135200;
            //Puffin Note: The second part of Neinzul Firefly weapon point setup.
            if ( OriginSystemForShotOrNull.TypeData.NumberOfWeaponPointsToConsumeOnFiring != 0 && (OriginSystemForShotOrNull.ParentEntity.NumberOfWeaponPoints >= OriginSystemForShotOrNull.TypeData.NumberOfWeaponPointsToConsumeOnFiring) )
            {
                OriginSystemForShotOrNull.ParentEntity.NumberOfWeaponPoints -= OriginSystemForShotOrNull.TypeData.NumberOfWeaponPointsToConsumeOnFiring;

                if ( OriginSystemForShotOrNull.ParentEntity.NumberOfWeaponPoints < 0 )
                    OriginSystemForShotOrNull.ParentEntity.NumberOfWeaponPoints = 0;
            }

            debugStage = 136000;
            #region HasAWeaponThatReturnsFireWhenParentHit
            if ( ActualTarget.DataForMark != null && ActualTarget.GetHasAWeaponThatReturnsFireWhenParentHitRightNow() &&
                !ShotHitNeverNull.GetIsReturnFireShot() ) //no chains of return-fire shots!
            {
                debugStage = 136100;
                GameEntity_Squad firingShip = OriginSystemForShotOrNull?.ParentEntity;
                if ( firingShip != null && 
                     firingShip.TypeData.ShipClass.CanReceiveExoticDamageFrom(ShipClassData_ExoticDamageType.RevengeShot) )
                {
                    debugStage = 136200;
                    bool hasPlayedSoundYet = false;
                    FInt runningDelay = FInt.Zero;
                    foreach ( EntitySystem system in ActualTarget.Systems )
                    {
                        debugStage = 136300;
                        if ( system == null )
                            continue;
                        EntitySystemTypeData typeData = system.TypeData;
                        if ( typeData == null )
                            continue;
                        if ( typeData.FiringTiming != FiringTiming.WhenParentEntityHit )
                            continue; //if the weapon is not the "when parent entity hit" type
                        if ( system.ComputeDisabledReason() != ArcenRejectionReason.Unknown )
                            continue; //if the weapon is off
                        if ( Context == null )
                            continue; //this can happen on MP clients
                        debugStage = 136400;
                        if ( typeData.ReturnsThisPercentageOfDamageWhenFiringRetaliatoryShot > FInt.Zero )
                        {
                            debugStage = 136500;
                            if ( damagePerCompressedShot > 0 )
                            {
                                debugStage = 136600;
                                int damageAmount = ((typeData.ReturnsThisPercentageOfDamageWhenFiringRetaliatoryShot * actualDamageDone) / actualCompressedShotsHit)
                                    .GetNearestIntPreferringHigher();
                                if ( damageAmount > 0 )
                                {
                                    InternalCreateActualShotForSalvo( ref hasPlayedSoundYet, ref runningDelay, firingShip, ActualTarget.WorldLocation,
                                        Context.GetHostOnlyContext(), system, trace, tracingBuffer, 0, 0, damageAmount, actualCompressedShotsHit, true, doShotsAllInstaHit );
                                }
                            }
                        }
                        else
                        {
                            debugStage = 136700;
                            InternalCreateActualShotForSalvo( ref hasPlayedSoundYet, ref runningDelay, firingShip, ActualTarget.WorldLocation,
                                Context.GetHostOnlyContext(), system, trace, tracingBuffer, 0, 0, 0, actualCompressedShotsHit, true, doShotsAllInstaHit );
                        }
                    }
                }
            }
            debugStage = 137000;
            #endregion
        }

        #region FindProtectingForcefieldToHitInsteadOfTarget
        private GameEntity_Squad FindProtectingForcefieldToHitInsteadOfTarget( IShotHitSource ShotHitNeverNull, EntitySystem OriginSystemForShotOrNull, GameEntity_Squad Target,
            bool HonorFiniteHitCountAOE, out bool FoundProtectorButCouldNotHitDueToFiniteHitCountAOE, ArcenSimContextAnyStatus Context )
        {
            FoundProtectorButCouldNotHitDueToFiniteHitCountAOE = false;
            if ( OriginSystemForShotOrNull != null && OriginSystemForShotOrNull.TypeData.FiresThroughEnemyShields )
            {
                //ArcenDebugging.ArcenDebugLog( OriginSystemForShot.ParentEntity.TypeData.DisplayName + " FiresThroughEnemyShields", Verbosity.DoNotShow );
                return null; //don't even check for forcefields if we shoot past them!
            }
            if ( Target == null )
            {
                //ArcenDebugging.ArcenDebugLog( Target.GetIsProtectedByAnyForcefield() + " GetIsProtectedByAnyForcefield or null", Verbosity.DoNotShow );
                return null; //if there's no target or it's not protected by any forcefields, then skip it for sure also.
            }
            if ( !Target.GetIsProtectedByAnyForcefield() )
            {
                if ( Target.TypeData.ProjectsForcefield && Target.GetCurrentShieldPoints() > 0 )
                    return Target; //bring back the target itself as the protecting shield if it is a shield generator not under other generators.
                //                   the idea with bringing it back on its own is that then it can take only shield damage, not hull damage, which seems more correct.
                //                   that also makes it consistent whether you attack the generator or something under it, which is definitely more correct.
                return null;
            }

            int debugStage = 1;
            try
            {
                debugStage = 100;
                //for ( int shieldType = 0; shieldType < 2; shieldType++ )
                {
                    debugStage = 200;
                    var protectingShields = Target.ProtectingShields.GetDisplayList();
                    
                    if ( protectingShields.Count == 0 )
                    {
                        //ArcenDebugging.ArcenDebugLog( "protectingShields.Count == 0 at shieldType " + shieldType, Verbosity.DoNotShow );
                        return null;
                    }
                    
                    //ArcenDebugging.ArcenDebugLog( "protectingShields count to check: " + protectingShields.Count, Verbosity.DoNotShow );

                    debugStage = 400;
                    for ( int i = protectingShields.Count - 1; i >= 0; i-- )
                    {
                        GameEntity_Squad protector = null;
                        try
                        {
                            protector = protectingShields[i].GetSquad();
                        }
                        catch { continue; } //threading issue
                        debugStage = 500;
                        if ( protector == null || protector.HasBeenRemovedFromSim || protector.ToBeRemovedAtEndOfThisFrame || protector.GetCurrentShieldPoints() <= 0 )
                        {
                            //ArcenDebugging.ArcenDebugLog( "protectingShields to be removed at index " + i, Verbosity.DoNotShow );
                            try
                            {
                                protectingShields.RemoveAt( i );
                            }
                            catch { }
                            continue;
                        }

                        debugStage = 400;
                        if ( protector.Debug_IgnoresDamage )
                        {
                            //ArcenDebugging.ArcenDebugLog( "protectingShields Debug_IgnoresDamage " + i, Verbosity.DoNotShow );
                            continue;
                        }
                        debugStage = 500;
                        if ( HonorFiniteHitCountAOE && Context.WorkingAOETargetsThatHaveBeenHitList_Contains( protector ) )
                        {
                            //ArcenDebugging.ArcenDebugLog( "protectingShields FoundProtectorButCouldNotHitDueToFiniteHitCountAOE " + i, Verbosity.DoNotShow );
                            FoundProtectorButCouldNotHitDueToFiniteHitCountAOE = true;
                            continue;
                        }

                        debugStage = 600;
                        if ( HonorFiniteHitCountAOE )
                            Context.WorkingAOETargetsThatHaveBeenHitList_Add( protector );

                        FoundProtectorButCouldNotHitDueToFiniteHitCountAOE = false; //we're all good now

                        //ArcenDebugging.ArcenDebugLog( "protectingShields yay return protector " + i, Verbosity.DoNotShow );
                        return protector; // even if the shield didn't have enough points to block the shot, it doesn't proceed further
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "FindProtectingForcefieldToHitInsteadOfTarget error at debugStage " + debugStage + ": " + e, Verbosity.ShowAsError );
            }
            //ArcenDebugging.ArcenDebugLog( "protectingShields found nothing?", Verbosity.DoNotShow );
            return null;
        }
        #endregion

        public override bool CheckForShotAOEDetonation( IShotHitSource ShotHitNeverNull, int CompressedShots, EntitySystem OriginSystemForShotOrNull, 
            GameEntity_Squad TargetOrNull, ArcenSimContextAnyStatus Context )
        {
            int debugStage = 0;
            try
            {
                if ( ShotHitNeverNull == null )
                {
                    ArcenDebugging.ArcenDebugLog( "CheckForShotAOEDetonation: Null ShotHitNeverNull!", Verbosity.ShowAsError );
                    return false;
                }
                if ( OriginSystemForShotOrNull == null )//the ship might've died, the game then saved and loaded, and now there is no valid system left?
                {
                    return false;
                }
                debugStage = 10;
                int aoe = ShotHitNeverNull.GetShotAreaOfEffect();
                debugStage = 20;
                GameEntity_Squad originEntity = OriginSystemForShotOrNull.ParentEntity;
                debugStage = 30;
                PlanetFaction myFaction = ShotHitNeverNull.GetPlanetFaction();
                if ( myFaction == null && originEntity != null )
                    myFaction = originEntity.PlanetFaction;
                debugStage = 31;
                Planet myPlanet = ShotHitNeverNull.GetPlanet();
                if ( myPlanet == null && originEntity != null )
                    myPlanet = originEntity.Planet;
                debugStage = 32;
                StateOfMatterTypeData stateofMatter = ShotHitNeverNull.GetCurrentStateOfMatter();
                if ( stateofMatter == null )
                    stateofMatter = (originEntity != null ? originEntity.CurrentStateOfMatter : StateOfMatterTypeDataTable.Instance.DefaultRow);

                debugStage = 33;
                if ( !stateofMatter.CanTargetOtherUnitsInThisState )
                    return true; //didn't work, but that's because we are in a state where we can't do that

                #region explosions
                if ( aoe > 0 )
                {
                    DoMultiHittingAoEAttack( ShotHitNeverNull, OriginSystemForShotOrNull, Context, TargetOrNull, CompressedShots, aoe, myFaction, myPlanet, stateofMatter,
                        GameSettings.Current.GetBoolBySetting( "Debug_LogAOEMath" ), ref debugStage );
                    Context.WorkingAOETargetsThatHaveBeenHitList_Clear();
                    ShotHitNeverNull.IncrementPostAOEData();
                    
                    return true;
                }
                #endregion
                
                #region beams
                if ( OriginSystemForShotOrNull.TypeData.BeamLengthMultiplier > FInt.Zero )
                {
                    EntityLineType lineType = OriginSystemForShotOrNull.TypeData.BeamWeaponLineType;
                    if (lineType == null)
                        lineType = EntityLineTypeTable.Instance.RowsByHardcodedType[OriginSystemForShotOrNull.TypeData.BeamWeaponVisualStyle];
                    
                    debugStage = 7000;
                    if ( TargetOrNull == null )
                        return true; //didn't work, but still an AOE shot so report true
                    
                    GameEntity_Squad targetEntity = TargetOrNull;

                    #region tracing
                    bool trace = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.IndividualLogic ) && OriginSystemForShotOrNull.ParentEntity == GameEntity_Base.CurrentlyHoveredOver;
                    ArcenCharacterBuffer tracingBuffer = null;
                    if ( trace ) tracingBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "EntitySimLogicImpl-CheckForShotAOEDetonation-trace", 10f );
                    if ( trace ) tracingBuffer.Add( "Tracing CheckForAOEDetonation for shot from " ).Add( OriginSystemForShotOrNull.TypeData.InternalName_Longer ).Add( " from " ).Add( OriginSystemForShotOrNull.ParentEntity.TypeData.InternalName ).Add( " #" ).Add( OriginSystemForShotOrNull.ParentEntity.PrimaryKeyID );
                    #endregion

                    int beamLength = (OriginSystemForShotOrNull.DataForMark.CalculateActualRange( originEntity ) * OriginSystemForShotOrNull.TypeData.BeamLengthMultiplier).IntValue;
                    int beamWidth = OriginSystemForShotOrNull.TypeData.BeamWidth;
                    int beamCount = OriginSystemForShotOrNull.TypeData.NumberBeamsToFire;
                    
                    #region Regular Multi-Hit Beams
                    debugStage = 7100;
                    if ( OriginSystemForShotOrNull.TypeData.HitsAllIntersectingTargets )
                    {
                        bool drawVisuals = GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.DrawBeamWeaponVisuals ) &&
                            myPlanet != null && myPlanet.Index == PlayerAccount_AIW2.GetViewingPlanetIndexSafe();

                        bool debug_LogCoilbeamMath = GameSettings.Current.GetBoolBySetting( "Debug_LogCoilbeamMath" );

                        debugStage = 7200;
                        
                        #region Arrays
                        
                        if ( beamCount > 1 && 
                             OriginSystemForShotOrNull.TypeData.DistanceFromCenterForBeamEmission > 0 )
                        {
                            debugStage = 7300;
                            List<ArcenPoint> workingEmissionPoints = Mat.GetTemporaryArcenPointList( "EntitySimLogicImplementation_BaseInfo-Beams-workingEmissionPoints", 10f );
                            if ( workingEmissionPoints == null ) //blocked for teardown/shutdown; bail
                                return false;

                            debugStage = 7400;
                            Mat.FillRingOfPointsAtDistance( originEntity.WorldLocation, ref workingEmissionPoints, beamCount,
                                 OriginSystemForShotOrNull.TypeData.DistanceFromCenterForBeamEmission );
                            //UnityEngine.Mathf.RoundToInt( OriginSystemForShotOrNull.TypeData.DistanceFromCenterForBeamEmission * myPlanet.GravWellSize.GeneralMultiplier )  Should already be scaled...

                            debugStage = 7500;
                            ArcenPoint endPoint = targetEntity.WorldLocation;
                            int maxTargets = OriginSystemForShotOrNull.DataForMark.AOEMaximumNumberOfTargetsHitPerShot != 0 ? OriginSystemForShotOrNull.DataForMark.AOEMaximumNumberOfTargetsHitPerShot : -1;

                            debugStage = 7600;

                            List<SafeSquadWrapper> intersectingTargets = GameEntity_Squad.GetTemporarySquadList( "EntitySimLogicImpl-CheckForShotAOEDetonation-intersectingTargets1", 10f );
                            if ( intersectingTargets == null ) //blocked for teardown/shutdown; bail
                            {
                                Mat.ReleaseTemporaryArcenPointList( workingEmissionPoints );
                                return false;
                            }
                            debugStage = 7700;
                            foreach ( ArcenPoint originPoint in workingEmissionPoints )
                            {
                                intersectingTargets.Clear();
                                debugStage = 7800;
                                OriginSystemForShotOrNull.GetEnemyEntitiesIntersectingInstaFireConicalShot( intersectingTargets, originPoint, endPoint, beamLength, beamWidth,
                                    targetEntity, stateofMatter, tracingBuffer );

                                debugStage = 7900;
                                DoMultiHittingBeamAttack( ShotHitNeverNull, OriginSystemForShotOrNull, Context, targetEntity, intersectingTargets, CompressedShots, maxTargets, ref endPoint, debug_LogCoilbeamMath );

                                debugStage = 8000;
                                if ( drawVisuals )
                                {
                                    if ( lineType != null && originEntity != null )
                                    {
                                        debugStage = 8100;
                                        lineType.WriteWithOffsetPointToDrawBufferForLengthOfTime( originEntity, originPoint - originEntity.WorldLocation, endPoint, 0.7f );
                                    }
                                }
                            }

                            debugStage = 8200;
                            GameEntity_Squad.ReleaseTemporarySquadList( intersectingTargets );
                            Mat.ReleaseTemporaryArcenPointList( workingEmissionPoints );

                            debugStage = 8300;
                            #region tracing
                            if ( trace )
                            {
                                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                                tracingBuffer.ReturnToPool();
                                tracingBuffer = null;
                            }
                            #endregion
                            
                            return true;
                        }
                        
                        #endregion

                        #region Regular Beam
                        {
                            debugStage = 11200;
                            ArcenPoint originPoint = originEntity.WorldLocation;
                            ArcenPoint targetPoint = targetEntity.WorldLocation;
                            AngleDegrees angle = originPoint.GetAngleToDegrees( targetPoint );
                            AngleDegrees beamBeginAngle = beamCount == 1 ? angle : angle.Add( -((OriginSystemForShotOrNull.TypeData.DegreesOffsetPerBeam * beamCount) / 2) );
                            
                            debugStage = 11400;
                            if ( OriginSystemForShotOrNull.TypeData.SecondsForBeamToRotateFully > 0 )
                            {
                                debugStage = 11500;
                                //if this is a spinning beam, the origin beam now starts at an additional offset
                                int degreesPerRotation = 360 / OriginSystemForShotOrNull.TypeData.SecondsForBeamToRotateFully;
                                int secondsForRotation = World_AIW2.Instance.GameSecond % OriginSystemForShotOrNull.TypeData.SecondsForBeamToRotateFully;
                                int rotationAmount = secondsForRotation * degreesPerRotation;
                                AngleDegrees rotation = AngleDegrees.Create( rotationAmount );
                                beamBeginAngle = beamBeginAngle.Add( rotationAmount );
                            }
                            
                            debugStage = 11600;
                            #region tracing
                            if ( trace )
                            {
                                tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "originPoint" ).Add( ":" ).Add( originPoint );
                                tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "targetPoint" ).Add( ":" ).Add( targetPoint );
                                tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "angle" ).Add( ":" ).Add( angle.ToString() );
                                tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "beamLength" ).Add( ":" ).Add( beamLength );
                                tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "beamBeginAngle" ).Add( ":" ).Add( beamBeginAngle.ToString() );
                            }
                            #endregion

                            AngleDegrees workingAngle = beamBeginAngle;
                            debugStage = 11700;
                            List<SafeSquadWrapper> intersectingTargets = GameEntity_Squad.GetTemporarySquadList( "EntitySimLogicImpl-CheckForShotAOEDetonation-intersectingTargets2", 10f );
                            if ( intersectingTargets == null ) //blocked for teardown/shutdown; bail
                                return false;
                            int maxTargets = OriginSystemForShotOrNull.DataForMark.AOEMaximumNumberOfTargetsHitPerShot != 0 ? OriginSystemForShotOrNull.DataForMark.AOEMaximumNumberOfTargetsHitPerShot : int.MaxValue;
                            debugStage = 11800;
                            for ( int i = 0; i < beamCount; i++, workingAngle += OriginSystemForShotOrNull.TypeData.DegreesOffsetPerBeam )
                            {
                                debugStage = 11900;
                                ArcenPoint endPoint = originPoint.GetPointAtAngleAndDistance( workingAngle, beamLength );
                                intersectingTargets.Clear();
                                debugStage = 12100;
                                OriginSystemForShotOrNull.GetEnemyEntitiesIntersectingInstaFireConicalShot( intersectingTargets, originPoint, endPoint, beamLength, beamWidth,
                                    targetEntity, stateofMatter, tracingBuffer );
                                debugStage = 12200;
                                DoMultiHittingBeamAttack( ShotHitNeverNull, OriginSystemForShotOrNull, Context, targetEntity, intersectingTargets, CompressedShots, maxTargets, ref endPoint, debug_LogCoilbeamMath );

                                debugStage = 12300;
                                if ( drawVisuals )
                                {
                                    if ( lineType != null && originEntity != null )
                                    {
                                        debugStage = 12400;
                                        lineType.WriteWithOffsetPointToDrawBufferForLengthOfTime( originEntity, originPoint - originEntity.WorldLocation, endPoint, 0.7f );
                                    }
                                }
                            }
                            
                            debugStage = 12500;
                            GameEntity_Squad.ReleaseTemporarySquadList( intersectingTargets );

                            #region tracing
                            if ( trace ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                            if ( trace )
                            {
                                tracingBuffer.ReturnToPool();
                                tracingBuffer = null;
                            }
                                #endregion
                        }
                        
                        #endregion
                        
                        return true;
                    }
                    #endregion

                    #region chain-lightning
                    if ( OriginSystemForShotOrNull.TypeData.BeamChainsOutToTargetsXTimes > 0 && 
                         OriginSystemForShotOrNull.TypeData.BeamChainsOutToTargetsXRange > 0 )
                    {
                        debugStage = 14100;
                        int maxTargets = OriginSystemForShotOrNull.DataForMark.AOEMaximumNumberOfTargetsHitPerShot;
                        int remainingTargets = maxTargets > 0 ? maxTargets : 9999999;

                        debugStage = 14200;
                        //first clear our working lists that we need for this
                        Context.WorkingAOETargetsThatHaveBeenHitList_Clear();
                        Context.WorkingAOETargetsToHitList_Clear();

                        List<SafeSquadWrapper> entitiesThatWeShouldChainFromThisCycle = GameEntity_Squad.GetTemporarySquadList( "EntitySimLogicImplementation_BaseInfo-entitiesThatWeShouldChainFromThisCycle", 10f );
                        if ( entitiesThatWeShouldChainFromThisCycle == null ) //blocked for teardown/shutdown; bail
                            return false;

                        int maxTargetsPerSource = OriginSystemForShotOrNull.TypeData.BeamChainsOutToMaxTargetsFromEachSource;

                        List<SafeSquadWrapper> entitiesToChainFromNextTime = Context.WorkingAOETargetsToHitList_GetList();

                        debugStage = 14300;
                        int chainMaxRange = OriginSystemForShotOrNull.TypeData.BeamChainsOutToTargetsXRange;
                        int chainMinRange = OriginSystemForShotOrNull.TypeData.BeamChainsOutToTargetMinRange;
                        bool drawVisuals = GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.DrawBeamWeaponVisuals ) &&
                            myPlanet != null && myPlanet.Index == PlayerAccount_AIW2.GetViewingPlanetIndexSafe();

                        debugStage = 14400;

                        #region next populate the total list of ships we can hit
                        //we do this once only, since there are some categories that we don't want to do over and over again.
                        List<SafeSquadWrapper> allValidTargets = GameEntity_Squad.GetTemporarySquadList( "EntitySimLogicImplementation_BaseInfo-allValidTargets", 10f );
                        if ( allValidTargets == null ) //blocked for teardown/shutdown; bail
                        {
                            GameEntity_Squad.ReleaseTemporarySquadList( entitiesThatWeShouldChainFromThisCycle );
                            return false;
                        }
                        PlanetFaction fac;
                        for ( int i = 0; i < myPlanet.Factions.Count; i++ )
                        {
                            debugStage = 14500;
                            fac = myPlanet.Factions[i];
                            if ( fac == null )
                                continue;

                            //doing this only once per faction, and more centrally, is quite efficient when there's a lot of entities
                            if ( !OriginSystemForShotOrNull.TypeData.AOEHitsFriendlyTargets && 
                                 !myFaction.GetIsHostileTowards( fac ) )
                            {
                                continue;
                            }

                            debugStage = 14600;
                            foreach ( GameEntity_Squad e in fac.Entities.Squads() )
                            {
                                if ( e == originEntity)
                                    continue;

                                if ( e.GetDamageForbiddenToThisTargetByTargetingRules( myFaction, TargetOrNull != null && e.PrimaryKeyID == TargetOrNull.PrimaryKeyID )
                                     != GameEntity_Squad.TargetEligibilityResult.Valid)
                                {
                                    continue;
                                }

                                if ( e.ProtectingShields.Count > 0 )
                                {
                                    continue; //don't chain to under forcefields
                                }

                                if ( e == targetEntity )
                                    continue; //don't hit our original target

                                if ( e.GetCurrentCloakingPoints() > 0 )
                                    continue; //don't hit cloaked ships

                                allValidTargets.Add( e );
                            }
                        }
                        #endregion

                        debugStage = 14700;
                        //next remember the first entity that we are hitting
                        entitiesThatWeShouldChainFromThisCycle.Add( targetEntity );

                        debugStage = 14800;
                        //do the hit logic against the target, or it doesn't seem to get hit at all
                        this.DoShotHitLogic( ShotHitNeverNull, CompressedShots, OriginSystemForShotOrNull, targetEntity, Context );

                        debugStage = 14900;
                        FInt percentageForAfterMain = FInt.OneHundred;
                        if ( OriginSystemForShotOrNull.TypeData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                        {
                            percentageForAfterMain = OriginSystemForShotOrNull.TypeData.AOEAndBeamDamageMultiplierToNonPrimaryTarget;
                        }

                        debugStage = 15100;
                        if ( drawVisuals )
                        {
                            debugStage = 15200;
                            
                            //note: this cone of beams is for visual purposes only
                            if ( beamCount > 1 && 
                                 OriginSystemForShotOrNull.TypeData.DistanceFromCenterForBeamEmission > 0 )
                            {
                                debugStage = 15300;
                                List<ArcenPoint> workingEmissionPoints = Mat.GetTemporaryArcenPointList( "EntitySimLogicImplementation_BaseInfo-ChainLightning-workingEmissionPoints", 10f );
                                if ( workingEmissionPoints == null ) //blocked for teardown/shutdown; bail
                                {
                                    GameEntity_Squad.ReleaseTemporarySquadList( entitiesThatWeShouldChainFromThisCycle );
                                    GameEntity_Squad.ReleaseTemporarySquadList( allValidTargets );
                                    return false;
                                }

                                Mat.FillRingOfPointsAtDistance( originEntity.WorldLocation, ref workingEmissionPoints, beamCount,
                                    OriginSystemForShotOrNull.TypeData.DistanceFromCenterForBeamEmission );

                                debugStage = 15400;
                                foreach ( ArcenPoint emissionPoint in workingEmissionPoints )
                                {
                                    if ( lineType != null && originEntity != null && targetEntity != null )
                                    {
                                        lineType.WriteWithOffsetPointToDrawBufferForLengthOfTime( originEntity, emissionPoint - originEntity.WorldLocation, targetEntity.WorldLocation, 0.4f );
                                    }
                                }
                                Mat.ReleaseTemporaryArcenPointList( workingEmissionPoints );
                            }
                            else //the normal single beam
                            {
                                debugStage = 15600;

                                if ( lineType != null && originEntity != null && targetEntity != null )
                                {
                                    debugStage = 15700;
                                    lineType.WriteToDrawBufferForLengthOfTime( originEntity, targetEntity.WorldLocation, 0.4f );
                                }
                            }
                        }
                        
                        debugStage = 16100;
                        
                        //how many iterations out do we go?
                        for ( int chainIndex = 0; chainIndex < OriginSystemForShotOrNull.TypeData.BeamChainsOutToTargetsXTimes; chainIndex++ )
                        {
                            debugStage = 16200;
                            
                            //if no more targets, then stop looking for anything to do
                            if ( allValidTargets.Count <= 0 )
                                break; 
                            
                            //if nothing new was hit last cycle, then also stop looking for anything to do
                            if ( entitiesThatWeShouldChainFromThisCycle.Count <= 0 )
                                break; 
                            
                            //if we already hit too many targets
                            if ( remainingTargets <= 0 )
                                break; 

                            debugStage = 16250;
                            entitiesToChainFromNextTime.Clear();

                            #region Find All The Hits For Each Chain Source
                            int distance;
                            foreach ( SafeSquadWrapper chainSourceWrapper in entitiesThatWeShouldChainFromThisCycle )
                            {
                                //if we already hit too many targets
                                if ( remainingTargets <= 0 )
                                    break; 
                                
                                GameEntity_Squad chainSource = chainSourceWrapper.GetSquadEvenIfDeadSoLongAsPKIDNotReassigned();
                                if ( chainSource == null )
                                    continue;

                                debugStage = 16300;
                                int remainingTargetsForThisSource = maxTargetsPerSource > 0 ? maxTargetsPerSource : 999;

                                ArcenPoint myLoc = chainSource.WorldLocation;

                                for ( int i = allValidTargets.Count - 1; i >= 0; i-- )
                                {
                                    debugStage = 16400;
                                    
                                    GameEntity_Squad possibleTarget = allValidTargets[i].GetSquad();
                                    if ( possibleTarget == null )
                                        continue;
                                    
                                    if ( possibleTarget.CurrentStateOfMatter != stateofMatter )
                                        continue;
                                    
                                    debugStage = 16500;
                                    distance = chainMaxRange + possibleTarget.DataForMark.Radius + possibleTarget.CalculatedCurrentShieldRadius;
                                    if ( !possibleTarget.WorldLocation.GetHasAnyChanceOfBeingInRange( myLoc, distance ) )
                                        continue;
                                    
                                    int actualDist = possibleTarget.WorldLocation.GetDistanceTo( myLoc, false );
                                    if ( actualDist > distance || actualDist < chainMinRange )
                                        continue;
                                    
                                    //we got a hit!
                                    remainingTargets--;
                                    remainingTargetsForThisSource--;
                                    entitiesToChainFromNextTime.Add( possibleTarget );
                                    allValidTargets.RemoveAt( i );
                                    
                                    #region Do The Hit!

                                    debugStage = 16600;
                                    
                                    this.DoShotHitLogic( 
                                        ShotHitNeverNull, CompressedShots, OriginSystemForShotOrNull, possibleTarget, false,
                                        targetEntity == possibleTarget ? 
                                            FInt.OneHundred : 
                                            percentageForAfterMain, Context );

                                    debugStage = 16700;
                                    if ( drawVisuals )
                                    {
                                        if ( lineType != null && 
                                             chainSource != null && 
                                             possibleTarget != null )
                                        {
                                            debugStage = 16800;
                                            lineType.WriteToDrawBufferForLengthOfTime( chainSource, possibleTarget.WorldLocation, 0.45f );
                                        }
                                    }

                                    //if this source already hit as many as possible
                                    if ( remainingTargetsForThisSource <= 0 )
                                        break; 
                                    
                                    //if we already hit too many targets
                                    if ( remainingTargets <= 0 )
                                        break; 
                                    
                                    #endregion
                                }
                            }
                            #endregion

                            debugStage = 16900;
                            if ( remainingTargets <= 0 )
                                break; //if we already hit too many targets
                            if ( entitiesToChainFromNextTime.Count <= 0 )
                                break; //if no targets hit freshly this time

                            debugStage = 17100;
                            //if there will be another cycle, then record what we should chain from next cycle
                            if ( chainIndex < OriginSystemForShotOrNull.TypeData.BeamChainsOutToTargetsXTimes - 1 )
                            {
                                debugStage = 17200;
                                entitiesThatWeShouldChainFromThisCycle.Clear();
                                entitiesThatWeShouldChainFromThisCycle.AddRange( entitiesToChainFromNextTime );
                                entitiesToChainFromNextTime.Clear();
                            }
                        }

                        debugStage = 17300;
                        GameEntity_Squad.ReleaseTemporarySquadList( entitiesThatWeShouldChainFromThisCycle );
                        GameEntity_Squad.ReleaseTemporarySquadList( allValidTargets );
                        
                        return true;
                    }
                    #endregion

                    #region point-beams
                    {
                        debugStage = 21200;
                        //do the hit logic against the target, or it doesn't seem to get hit at all
                        this.DoShotHitLogic( ShotHitNeverNull, CompressedShots, OriginSystemForShotOrNull, targetEntity, Context );

                        debugStage = 21300;

                        if ( GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.DrawBeamWeaponVisuals ) &&
                            myPlanet != null && 
                            myPlanet.Index == PlayerAccount_AIW2.GetViewingPlanetIndexSafe() )
                        {
                            debugStage = 21400;
                            if ( beamCount > 1 && 
                                 OriginSystemForShotOrNull.TypeData.DistanceFromCenterForBeamEmission > 0 )
                            {
                                debugStage = 21500;
                                List<ArcenPoint> workingEmissionPoints = Mat.GetTemporaryArcenPointList( "EntitySimLogicImplementation_BaseInfo-PointBeams-workingEmissionPoints", 10f );
                                if ( workingEmissionPoints == null ) //blocked for teardown/shutdown; bail
                                    return false;

                                Mat.FillRingOfPointsAtDistance( originEntity.WorldLocation, ref workingEmissionPoints, beamCount,
                                    OriginSystemForShotOrNull.TypeData.DistanceFromCenterForBeamEmission );

                                debugStage = 21600;
                                foreach ( ArcenPoint emissionPoint in workingEmissionPoints )
                                {
                                    if ( lineType != null && originEntity != null && targetEntity != null )
                                    {
                                        debugStage = 21700;
                                        lineType.WriteWithOffsetPointToDrawBufferForLengthOfTime( originEntity, emissionPoint - originEntity.WorldLocation, targetEntity.WorldLocation, 0.7f );
                                    }
                                }
                                Mat.ReleaseTemporaryArcenPointList( workingEmissionPoints );
                                debugStage = 21800;
                            }
                            else
                            {
                                debugStage = 22100;
                                if ( lineType != null && originEntity != null && targetEntity != null )
                                {
                                    debugStage = 22200;
                                    lineType.WriteToDrawBufferForLengthOfTime( originEntity, targetEntity.WorldLocation, 0.7f );
                                }
                            }
                        }
                        
                        return true;
                    }
                    #endregion
                }
                #endregion
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "CheckForShotAOEDetonation error at debugStage " + debugStage + ": " + e, Verbosity.ShowAsError );
            }

            //not an AOE shot
            return false;
        }

        public void DoMultiHittingAoEAttack( IShotHitSource ShotHitNeverNull, EntitySystem OriginSystemForShotNeverNull, ArcenSimContextAnyStatus Context, GameEntity_Squad targetEntity,
            int compressedShots, int aoe, PlanetFaction myFaction, Planet myPlanet, StateOfMatterTypeData stateofMatter, bool debug_LogAOEMath, ref int debugStage )
        {
            //NRSLTODO: Early out if the primary target can already be hit for all the compressed shots

            #region find stuff in AoE range
            debugStage = 1000;
            PlanetFaction faction;
            ArcenPoint location = ShotHitNeverNull.GetWorldLocation();
            Context.WorkingAOETargetsToHitList_EntityAndRangeSquare_Clear();
            debugStage = 2000;
            long distance;
            long squareDistance;
            int numberOfTheoreticalTargetsToHit = 0;
            GameEntity_Squad originEntity = OriginSystemForShotNeverNull?.ParentEntity;

            for ( int i = 0; i < myPlanet.Factions.Count; i++ )
            {
                debugStage = 3000;
                faction = myPlanet.Factions[i];
                if ( (OriginSystemForShotNeverNull == null || !OriginSystemForShotNeverNull.TypeData.AOEHitsFriendlyTargets) &&
                     !myFaction.GetIsHostileTowards( faction ) )
                {
                    continue;
                }
                debugStage = 4000;
                foreach ( GameEntity_Squad otherEntity in faction.Entities.Squads() )
                {
                    if ( otherEntity == null )
                        continue;
                    if ( otherEntity == originEntity)
                        continue;
                    if ( otherEntity.CurrentStateOfMatter != stateofMatter )
                        continue;
                    try
                    {
                        distance = aoe + otherEntity.DataForMark.Radius + otherEntity.CalculatedCurrentShieldRadius;
                        squareDistance = distance * distance;

                        if ( otherEntity.WorldLocation.GetSquareDistanceTo( location ) > squareDistance )//use range squares for maximum performance and precision
                            continue;
                        if ( otherEntity.GetDamageForbiddenToThisTargetByTargetingRules( myFaction, targetEntity != null && otherEntity.PrimaryKeyID == targetEntity.PrimaryKeyID ) != GameEntity_Squad.TargetEligibilityResult.Valid )
                            continue;

                        if ( otherEntity.Network_FrameToStartAnyProcessing > World_AIW2.Instance.Network_CurrentFrameNumber )
                            continue;
                        
                        Context.WorkingAOETargetsToHitList_EntityAndRangeSquare_Add( otherEntity, (int)distance );
                        numberOfTheoreticalTargetsToHit += 1 + otherEntity.ExtraStackedSquadsInThis;
                    }
                    catch ( Exception e )
                    {
                        if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Exception in AOE calculation target accumulation: " + e, Verbosity.ShowAsError );
                    }
                }
                debugStage = 5000;
            }
            
            debugStage = 6000;
            if ( Context.WorkingAOETargetsToHitList_EntityAndRangeSquare_Count() <= 0 )//this shouldn't be possible, but just in case
                return;

            debugStage = 7000;
            int maxHitEvents;
            if ( OriginSystemForShotNeverNull.DataForMark.AOEMaximumNumberOfTargetsHitPerShot > 0 )
                maxHitEvents = compressedShots * OriginSystemForShotNeverNull.DataForMark.AOEMaximumNumberOfTargetsHitPerShot;
            else
                maxHitEvents = int.MaxValue;

            debugStage = 8000;
            if ( maxHitEvents < numberOfTheoreticalTargetsToHit )
            {
                Context.WorkingAOETargetsToHitList_EntityAndRangeSquare_Sort();
            }
            #endregion

            debugStage = 9000;
            int totalDamage = 0;
            int remainingTotalHitEvents = OriginSystemForShotNeverNull.GetMaxAoEHits( compressedShots );
            int projectedHitEvents = Math.Min( maxHitEvents, numberOfTheoreticalTargetsToHit );
            if (projectedHitEvents <= 0)
                return;
            
            FInt portionOutOf100_Primary;
            FInt portionOutOf100_Secondary;
            if ( OriginSystemForShotNeverNull.TypeData.AOESpreadsDamageAmongAvailableTargets )
                portionOutOf100_Primary = FInt.OneHundred / projectedHitEvents;
            else
                portionOutOf100_Primary = FInt.OneHundred;
            
            if ( OriginSystemForShotNeverNull.TypeData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero)
                portionOutOf100_Secondary = ( OriginSystemForShotNeverNull.TypeData.AOEAndBeamDamageMultiplierToNonPrimaryTarget / 100 ) * portionOutOf100_Primary;
            else
                portionOutOf100_Secondary = portionOutOf100_Primary;
            
            debugStage = 10000;

            if ( debug_LogAOEMath )
                ArcenDebugging.ArcenDebugLogSingleLine( "### LOOP START: " + (1 + OriginSystemForShotNeverNull.ParentEntity?.ExtraStackedSquadsInThis) +
                    "x " + OriginSystemForShotNeverNull.ParentEntity?.TypeData?.InternalName + " attacking PRIMARY TARGET " + (1 + targetEntity?.ExtraStackedSquadsInThis) + "x " +
                    targetEntity?.TypeData?.InternalName + "-> remainingTotalHitEvents: " + remainingTotalHitEvents + ", projectedHitEvents: " + projectedHitEvents +
                    ", numberOfTheoreticalTargetsToHit: " + numberOfTheoreticalTargetsToHit + ", portionOutOf100_Primary: " + portionOutOf100_Primary +
                    ", portionOutOf100_Secondary: " + portionOutOf100_Secondary, Verbosity.DoNotShow );

            debugStage = 11000;
            int currentHitCount;
            int damageDealt;
            int actualHitEvents;
            bool result;
            GameEntity_Squad currentTarget;
            for ( int i = 0; i < Context.WorkingAOETargetsToHitList_EntityAndRangeSquare_Count() && remainingTotalHitEvents > 0; i++ )
            {
                debugStage = 12000;
                currentTarget = Context.WorkingAOETargetsToHitList_EntityAndRangeSquare_AtIndex( i );
                int targetStacksBefore = 1 + currentTarget.ExtraStackedSquadsInThis;
                
                int attackerStacksBefore = 1;
                if ( OriginSystemForShotNeverNull != null && OriginSystemForShotNeverNull.ParentEntity != null )
                    attackerStacksBefore = 1 + OriginSystemForShotNeverNull.ParentEntity.ExtraStackedSquadsInThis;
                
                currentHitCount = GetAoEOrBeamHitsAtTarget( currentTarget, OriginSystemForShotNeverNull, compressedShots, remainingTotalHitEvents );
                if (currentHitCount <= 0)//in case the target has died, or something else that was unexpected happened
                    continue;

                if (currentTarget == targetEntity)
                {
                    result = this.DoShotHitLogic( ShotHitNeverNull, currentHitCount, OriginSystemForShotNeverNull, currentTarget, debug_LogAOEMath,
                        portionOutOf100_Primary, out damageDealt, out actualHitEvents, Context );
                } 
                else
                {
                    result = this.DoShotHitLogic( ShotHitNeverNull, currentHitCount, OriginSystemForShotNeverNull, currentTarget, debug_LogAOEMath,
                        portionOutOf100_Secondary, out damageDealt, out actualHitEvents, Context );
                }
                
                debugStage = 13000;
                LogExplosionHit( debug_LogAOEMath, 'A', currentTarget, OriginSystemForShotNeverNull, currentHitCount, remainingTotalHitEvents, attackerStacksBefore, targetStacksBefore, maxHitEvents, damageDealt, actualHitEvents, result );
                if ( damageDealt <= 0 )
                    continue;
                
                totalDamage += damageDealt;
                remainingTotalHitEvents -= actualHitEvents;
            }
            
            OriginSystemForShotNeverNull.LastTotalDamageMyShotDidCaused = totalDamage;
        }

        public void LogExplosionHit( bool log, char path, GameEntity_Squad Target, EntitySystem System, int compressedShots, int maxHitEventsRemaining, int attackerStacksBefore,
            int targetStacksBefore, int maxHitEventsPossibleForAoE, int damageDone, int actualHitEventsAtTarget, bool result )
        {
            if ( !log )
                return;
            ArcenDebugging.ArcenDebugLogSingleLine( "PATH " + path + ": " + (1 + System.ParentEntity?.ExtraStackedSquadsInThis) + "x (before: " + attackerStacksBefore + ") "
                + System.ParentEntity?.TypeData?.InternalName + " attacking " + (1 + Target?.ExtraStackedSquadsInThis) + "x (before: " + targetStacksBefore + ") "
                + Target?.TypeData?.InternalName + " with " + compressedShots + " hit events of " + System.TypeData?.InternalName_Original + " (sharedDamage: " +
                System.TypeData?.AOESpreadsDamageAmongAvailableTargets + "), maxHitEventsPossibleForAoE: " + maxHitEventsPossibleForAoE + ", maxHitEventsRemaining: " +
                maxHitEventsRemaining + " | Results: damageDone: " + damageDone + " in " + actualHitEventsAtTarget + " hit events, returned " + result, Verbosity.DoNotShow );
        }

        public void DoMultiHittingBeamAttack( IShotHitSource ShotHitNeverNull, EntitySystem OriginSystemForShotNeverNull, ArcenSimContextAnyStatus Context,
            GameEntity_Squad targetEntity, List<SafeSquadWrapper> intersectingTargets, int compressedShots, int maxTargetsPerBeam,
            ref ArcenPoint BeamEndPoint, bool debug_LogCoilbeamMath )
        {
            int totalDamage = 0;
            
            int totalPossibleHitEventsRemaining;
            if( OriginSystemForShotNeverNull.DataForMark.AOEMaximumNumberOfTargetsHitPerShot <= 0)
                totalPossibleHitEventsRemaining = int.MaxValue;
            else
                totalPossibleHitEventsRemaining = compressedShots * OriginSystemForShotNeverNull.DataForMark.AOEMaximumNumberOfTargetsHitPerShot;
            
            int maxHitEventsAtTarget;
            int damageDone;
            int actualHitEventsAtTarget;
            int targetStacksBefore = 1 + targetEntity.ExtraStackedSquadsInThis;
            int attackerStacksBefore = 1;
            if ( OriginSystemForShotNeverNull != null && OriginSystemForShotNeverNull.ParentEntity != null )
                attackerStacksBefore = 1 + OriginSystemForShotNeverNull.ParentEntity.ExtraStackedSquadsInThis;
            
            FInt damagePercentBase = FInt.One;
            // this is how we could have done fans, if we wanted the total shot damage to be evenly split between the beams, instead of per beam
            //if (OriginSystemForShotNeverNull.TypeData.NumberBeamsToFire > 1)
                //damagePercentBase /= OriginSystemForShotNeverNull.TypeData.NumberBeamsToFire.ToFInt();
            
            FInt damagePercentToPrimary = FInt.OneHundred * damagePercentBase;
            bool isCoilbeam = false;
            bool mustDoSeparatePrimaryHit = false;
            if ( OriginSystemForShotNeverNull.TypeData != null )
            {
                isCoilbeam = OriginSystemForShotNeverNull.TypeData.IsCoilbeam;
                mustDoSeparatePrimaryHit = isCoilbeam || OriginSystemForShotNeverNull.TypeData.AOEAndBeamDamageMultiplierToNonPrimaryTarget != FInt.One;
            }
            bool result;
            if (mustDoSeparatePrimaryHit)
            {
                //We must calculate the primary hits SEPARATELY in this case because otherwise the coilbeam and primary target damage would be dealt too many times to the primary target
                //There can only be so many primary hits as there is compressed shots - 1 primary hit per shot, so skip getting maxHitEventsAtTarget.
                result = this.DoShotHitLogic( ShotHitNeverNull, compressedShots, OriginSystemForShotNeverNull, targetEntity, false, damagePercentToPrimary, out damageDone, out actualHitEventsAtTarget, Context );
                LogCoilbeamHit( debug_LogCoilbeamMath, 'P', targetEntity, OriginSystemForShotNeverNull, compressedShots, totalPossibleHitEventsRemaining, attackerStacksBefore, targetStacksBefore, maxTargetsPerBeam, compressedShots, damageDone, actualHitEventsAtTarget, result );
                if ( maxTargetsPerBeam <= 1 + targetEntity.ExtraStackedSquadsInThis )
                    BeamEndPoint = targetEntity.WorldLocation;
                totalDamage += damageDone;
                if ( !isCoilbeam )
                    totalPossibleHitEventsRemaining -= actualHitEventsAtTarget;//if this wasn't a coilbeam hitting its primary target we've actually consumed some hit events
            }
            if ( intersectingTargets.Count <= 0 ) //we intersected nothing
            {
                if ( !mustDoSeparatePrimaryHit )//unless the primary target was already hit
                {
                    //always hit the primary target, regardless of whatever else
                    //in this case we give all the damage to it
                    maxHitEventsAtTarget = GetAoEOrBeamHitsAtTarget( targetEntity, OriginSystemForShotNeverNull, compressedShots, totalPossibleHitEventsRemaining );
                    result = this.DoShotHitLogic( ShotHitNeverNull, maxHitEventsAtTarget, OriginSystemForShotNeverNull, targetEntity, false, damagePercentToPrimary, out damageDone, out actualHitEventsAtTarget, Context );
                    LogCoilbeamHit( debug_LogCoilbeamMath, 'A', targetEntity, OriginSystemForShotNeverNull, compressedShots, totalPossibleHitEventsRemaining, attackerStacksBefore, targetStacksBefore, maxTargetsPerBeam, maxHitEventsAtTarget, damageDone, actualHitEventsAtTarget, result );
                    if ( maxTargetsPerBeam <= 1 + targetEntity.ExtraStackedSquadsInThis )
                        BeamEndPoint = targetEntity.WorldLocation;
                    totalDamage += damageDone;
                }
            } 
            else 
            if ( intersectingTargets.Count == 1 && intersectingTargets[0].GetPrimaryKeyID() == targetEntity.PrimaryKeyID ) //we intersected only the target, so skip some math
            {
                if ( !mustDoSeparatePrimaryHit )//unless the primary target was already hit
                {
                    //we only hit the target, so be sure to just give it all the damage anyway
                    maxHitEventsAtTarget = GetAoEOrBeamHitsAtTarget( targetEntity, OriginSystemForShotNeverNull, compressedShots, totalPossibleHitEventsRemaining );
                    result = this.DoShotHitLogic( ShotHitNeverNull, maxHitEventsAtTarget, OriginSystemForShotNeverNull, targetEntity, false, damagePercentToPrimary, out damageDone, out actualHitEventsAtTarget, Context );
                    LogCoilbeamHit( debug_LogCoilbeamMath, 'B', targetEntity, OriginSystemForShotNeverNull, compressedShots, totalPossibleHitEventsRemaining, attackerStacksBefore, targetStacksBefore, maxTargetsPerBeam, maxHitEventsAtTarget, damageDone, actualHitEventsAtTarget, result );
                    if ( maxTargetsPerBeam <= 1 + targetEntity.ExtraStackedSquadsInThis )
                        BeamEndPoint = targetEntity.WorldLocation;
                    totalDamage += damageDone;
                }
            } 
            else //begin we are splitting betwen multiple targets
            {
                if ( !mustDoSeparatePrimaryHit )//unless the primary target was already hit
                {
                    maxHitEventsAtTarget = GetAoEOrBeamHitsAtTarget( targetEntity, OriginSystemForShotNeverNull, compressedShots, totalPossibleHitEventsRemaining );
                    result = this.DoShotHitLogic( ShotHitNeverNull, maxHitEventsAtTarget, OriginSystemForShotNeverNull, targetEntity, false, damagePercentToPrimary, out damageDone, out actualHitEventsAtTarget, Context );
                    LogCoilbeamHit( debug_LogCoilbeamMath, 'C', targetEntity, OriginSystemForShotNeverNull, compressedShots, totalPossibleHitEventsRemaining, attackerStacksBefore, targetStacksBefore, maxTargetsPerBeam, maxHitEventsAtTarget, damageDone, actualHitEventsAtTarget, result );
                    totalPossibleHitEventsRemaining -= actualHitEventsAtTarget;
                    totalDamage += damageDone;
                }
                
                if(totalPossibleHitEventsRemaining <= 0)
                {
                    if ( debug_LogCoilbeamMath )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Exiting coilbeam calculations because all possible hit events were used up.", Verbosity.DoNotShow );
                    BeamEndPoint = targetEntity.WorldLocation;
                    return;
                }
                
                FInt damageMultiplierOutOfOne = OriginSystemForShotNeverNull.TypeData.AOEAndBeamDamageMultiplierToNonPrimaryTarget * damagePercentBase;
                if ( damageMultiplierOutOfOne <= FInt.Zero )
                    damageMultiplierOutOfOne = FInt.One;
                FInt FinalMultiplierOutOf100;
                int coilbeamTargetsToSplit = 0;
                
                if ( OriginSystemForShotNeverNull.TypeData.IsCoilbeam )
                {
                    if(intersectingTargets.Count > maxTargetsPerBeam)
                    {
                        coilbeamTargetsToSplit = maxTargetsPerBeam;
                    } 
                    else
                    {
                        coilbeamTargetsToSplit = 0;
                        int next;
                        for(int i = 0; i < intersectingTargets.Count; i++ )
                        {
                            if(intersectingTargets[i].GetPrimaryKeyID() == targetEntity.PrimaryKeyID)
                            {
                                continue;
                            }
                            next = coilbeamTargetsToSplit + 1 + intersectingTargets[i].ExtraStackedSquadsInThis;
                            if(next >= maxTargetsPerBeam)
                            {
                                coilbeamTargetsToSplit = maxTargetsPerBeam;
                                break;
                            }
                            coilbeamTargetsToSplit = next;
                        }
                    }
                    FinalMultiplierOutOf100 = FInt.OneHundred * damageMultiplierOutOfOne / coilbeamTargetsToSplit;
                } 
                else
                {
                    FinalMultiplierOutOf100 = FInt.OneHundred * damageMultiplierOutOfOne;
                }

                if ( debug_LogCoilbeamMath )
                    ArcenDebugging.ArcenDebugLogSingleLine( "### LOOP START (primary target already hit in Path C or P): " + (1 + OriginSystemForShotNeverNull.ParentEntity?.ExtraStackedSquadsInThis) +
                        "x " + OriginSystemForShotNeverNull.ParentEntity?.TypeData?.InternalName + " attacking PRIMARY TARGET " + (1 + targetEntity?.ExtraStackedSquadsInThis) + "x " +
                        targetEntity?.TypeData?.InternalName + "-> damageMultiplierOutOfOne: " + damageMultiplierOutOfOne + ", FinalMultiplierOutOf100: " + FinalMultiplierOutOf100 +
                        ", coilbeamTargetsToSplit (only if it's a coilbeam): " + coilbeamTargetsToSplit + ", maxHitEventsRemaining remaining: " + totalPossibleHitEventsRemaining, Verbosity.DoNotShow );
                
                GameEntity_Squad intersectedTarget = null;
                int k = 0;
                
                for ( ; k < intersectingTargets.Count && totalPossibleHitEventsRemaining > 0; k++ )
                {
                    intersectedTarget = intersectingTargets[k].GetSquad();
                    
                    if ( intersectedTarget == null )
                        continue;
                    if ( intersectedTarget == targetEntity )
                        continue;

                    maxHitEventsAtTarget = GetAoEOrBeamHitsAtTarget( intersectedTarget, OriginSystemForShotNeverNull, compressedShots, totalPossibleHitEventsRemaining );
                    if(maxHitEventsAtTarget <= 0)
                        continue;
                    
                    targetStacksBefore = 1 + targetEntity.ExtraStackedSquadsInThis;
                    attackerStacksBefore = 1;
                    
                    if ( OriginSystemForShotNeverNull != null && OriginSystemForShotNeverNull.ParentEntity != null )
                        attackerStacksBefore = 1 + OriginSystemForShotNeverNull.ParentEntity.ExtraStackedSquadsInThis;
                    
                    if ( OriginSystemForShotNeverNull.TypeData.IsCoilbeam ) //this is a non-primary target hit with the coilbeam
                    {
                        result = this.DoShotHitLogic( ShotHitNeverNull, maxHitEventsAtTarget, OriginSystemForShotNeverNull, intersectedTarget, false, FinalMultiplierOutOf100, out damageDone, out actualHitEventsAtTarget, Context );
                        LogCoilbeamHit( debug_LogCoilbeamMath, 'D', intersectedTarget, OriginSystemForShotNeverNull, compressedShots, totalPossibleHitEventsRemaining, attackerStacksBefore, targetStacksBefore, maxTargetsPerBeam, maxHitEventsAtTarget, damageDone, actualHitEventsAtTarget, result );
                    } 
                    else //generic beam hit; could be primary or secondary
                    {
                        result = this.DoShotHitLogic( ShotHitNeverNull, maxHitEventsAtTarget, OriginSystemForShotNeverNull, intersectedTarget, false, FinalMultiplierOutOf100, out damageDone, out actualHitEventsAtTarget, Context );
                        LogCoilbeamHit( debug_LogCoilbeamMath, 'E', intersectedTarget, OriginSystemForShotNeverNull, compressedShots, totalPossibleHitEventsRemaining, attackerStacksBefore, targetStacksBefore, maxTargetsPerBeam, maxHitEventsAtTarget, damageDone, actualHitEventsAtTarget, result );
                    }
                    
                    totalDamage += damageDone;
                    totalPossibleHitEventsRemaining -= actualHitEventsAtTarget;
                }
                
                //if we hit the target limit in there somewhere
                if ( k < intersectingTargets.Count )
                {
                    if ( intersectedTarget == null )
                        BeamEndPoint = targetEntity.WorldLocation;//beam ends at the primary target, as no other targets were hit
                    else
                        BeamEndPoint = intersectedTarget.WorldLocation;//beam ends at the last target hit
                }
                
            } //end we are splitting betwen multiple target
            
            intersectingTargets.Clear();
            OriginSystemForShotNeverNull.LastTotalDamageMyShotDidCaused = totalDamage;
        }

        //Just so this can be debugged/updated with minimal effort
        public int GetAoEOrBeamHitsAtTarget(GameEntity_Squad Target, EntitySystem System, int compressedShots, int maxHitsRemaining)
        {
            if ( Target == null )
                return 0;
            
            int max = Target.ShipCount;
            
            int cap = System.DataForMark.AOEMaximumNumberOfTargetsHitPerShot;
            if (cap > 0 &&
                cap < max)
            {
                max = cap;
            }
            
            return Math.Min(max * compressedShots, maxHitsRemaining);
        }

        public void LogCoilbeamHit(bool log, char path, GameEntity_Squad Target, EntitySystem System, int compressedShots, int maxHitEventsRemaining, int attackerStacksBefore,
            int targetStacksBefore, int maxTargetsPerBeam, int maxHitEventsAtTarget, int damageDone, int actualHitEventsAtTarget, bool result )
        {
            if ( !log )
                return;
            ArcenDebugging.ArcenDebugLogSingleLine( "PATH " + path + ": " + (1 + System.ParentEntity?.ExtraStackedSquadsInThis) + "x (before: " + attackerStacksBefore + ") "
                + System.ParentEntity?.TypeData?.InternalName + " " + System.ParentEntity?.PrimaryKeyID + " attacking " + (1 + Target?.ExtraStackedSquadsInThis) + "x (before: " + targetStacksBefore + ") " +
                Target?.TypeData?.InternalName + " " + Target?.PrimaryKeyID + " with " + compressedShots + " compressed shots of " + System.TypeData?.InternalName_Original + " (coilbeam: " +
                System.TypeData?.IsCoilbeam + "): maxTargetsPerBeam: " + maxTargetsPerBeam + ", maxHitEventsAtTarget: " + maxHitEventsAtTarget + ", maxHitEventsRemaining: " +
                maxHitEventsRemaining + " | Results: damageDone: " + damageDone + " in " + actualHitEventsAtTarget + " hit events, returned " + result, Verbosity.DoNotShow );
        }

        public GameEntity_Shot SpawnShot_ReturnNullIfMPClient( EntitySystem System, ArcenPoint StartingLocation, int shotCount, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return null;
            if ( System == null )
                return null;
            EntitySystemTypeData typeData = System.TypeData;
            if ( typeData == null )
                return null;
            return SpawnShot_ReturnNullIfMPClient_Inner( System, typeData.ShotTypeData, StartingLocation, shotCount, Context );
        }

        public GameEntity_Shot SpawnShot_ReturnNullIfMPClient( EntitySystem System, GameEntityTypeData effectiveType, ArcenPoint StartingLocation, int shotCount, ArcenHostOnlySimContext Context )
        {
            return SpawnShot_ReturnNullIfMPClient_Inner( System, effectiveType, StartingLocation, shotCount, Context );
        }

        private GameEntity_Shot SpawnShot_ReturnNullIfMPClient_Inner( EntitySystem System, GameEntityTypeData effectiveType, ArcenPoint StartingLocation, int shotCount, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return null;
            GameEntity_Shot newShot = GameEntity_Shot.CreateShotNew_CallFromHostOnly( System.ParentEntity.PlanetFaction, effectiveType, System, StartingLocation, shotCount, Context );
            return newShot;
        }

        public GameEntity_Squad SpawnSquadStyleShot_ReturnNullIfMPClient( EntitySystem System, GameEntityTypeData effectiveType, ArcenPoint StartingLocation, int shotCount, ArcenHostOnlySimContext Context )
        {
            return SpawnSquadStyleShot_ReturnNullIfMPClient_Inner( System, effectiveType, StartingLocation, shotCount, Context );
        }

        public GameEntity_Squad SpawnSquadStyleShot_ReturnNullIfMPClient_Inner( EntitySystem System, GameEntityTypeData effectiveType, ArcenPoint StartingLocation, int shotCount, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return null;
            if ( System == null )
                return null;
            GameEntity_Squad parent = System.ParentEntity;
            if ( parent == null )
                return null;
            PlanetFaction pFaction = parent.PlanetFaction;
            if ( pFaction == null )
                return null;
            Fleet fleet = parent.GetFleetOrNull_Safe();
            if ( fleet == null )
                return null;

            byte markLevel = parent.CurrentMarkLevel;
            GameEntity_Squad newShot = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, effectiveType, markLevel,
                fleet, 0, StartingLocation, Context, "DroneFiredFromGun" );
            if ( newShot == null )
                return null;

			// idea: drone guns become the parent entity
			//newShot.ParentGameEntity = LazyLoadSquadWrapper.Create( System.ParentEntity );
			//System.ParentEntity.ChildSquads.Add( LazyLoadSquadWrapper.Create( newShot ) );

            newShot.AddOrSetExtraStackedSquadsInThis( (Int16)(shotCount - 1), true ); //don't count the original unit
            
            if ( markLevel > 1 )
                newShot.CustomBaseMark = markLevel;
            return newShot;
        }
        #endregion

        #region DoWormholeTraversalLogic
        //wormhole transit; transit wormhole
        public override void DoWormholeTraversalLogic( ArcenClientOrHostSimContextCore Context )
        {
            if ( CentralVars.DEBUG_TURN_OFF_WORMHOLE_TRAVERSAL )
                return;
            int debugStage = 0;
            try
            {
                debugStage = 10;
                bool didShipsGoThroughWormhole = false;
                int currentIndex = -1;
                ArcenPoint shipWormholePoint = ArcenPoint.ZeroZeroPoint;
                if ( PlayerAccount.Local != null )
                {
                    debugStage = 15;
                    currentIndex = PlayerAccount_AIW2.GetViewingPlanetIndexSafe();
                }

                debugStage = 17;
                bool humansCanTractorEnemiesThroughWormholes = World_AIW2.Instance.Setup.GetBoolBySetting( "HumansCanTractorEnemiesThroughWormholes" );

                debugStage = 20;
                //bool onGalaxyMap = Engine_AIW2.Instance.CurrentGameViewMode != GameViewMode.MainGameView;
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    debugStage = 100;
                    if ( !planet.BattleStatus_ProcessThisSimStep )
                        continue;
                    debugStage = 200;
                    while ( planet.ShipsReadyForWormholeTraversalAtEndOfThisFrame.Count > 0 )
                    {
                        debugStage = 300;
                        GameEntity_Squad entity = null;
                        if ( !planet.ShipsReadyForWormholeTraversalAtEndOfThisFrame.TryDequeue( out entity ) )
                            continue;

                        debugStage = 310;
                        EntityOrder order = entity.RemoveInvalidatedOrdersAndReturnFirstValid_ThatIsNotDecollision( false );
                        debugStage = 400;
                        if ( order.TypeData == null || order.TypeData.Type != EntityOrderType.Wormhole )
                            continue;
                        if ( entity.ActiveHack_Target != 0 )
                            continue;
                        
                        if ( entity.GetHasBeenDestroyed() || entity.ToBeRemovedAtEndOfThisFrame )
                            continue;
                        debugStage = 500;
                        Planet myPlanet = entity.Planet;
                        if ( myPlanet != planet )
                            continue;
                        GameEntity_Other thisSideWormhole = order.GetWormholeToOtherPlanetOrNull( myPlanet );
                        if ( thisSideWormhole == null )
                            continue;
                        debugStage = 600;

                        FleetMembership fleetMem = entity.FleetMembership;
                        if ( fleetMem == null )
                            continue;
                        debugStage = 700;
                        PlanetFaction pFaction = entity.PlanetFaction;
                        if ( pFaction == null )
                            continue;
                        Faction faction = pFaction.Faction;
                        if ( faction == null )
                            continue;
                        debugStage = 800;
                        GameEntityTypeData.MarkLevelStats entityMarkData = entity.DataForMark;
                        if ( entityMarkData == null )
                            continue;
                        debugStage = 900;

                        EntityOrderCollection entityOrders = entity.Orders;
                        if ( entityOrders == null )
                            continue;
                        debugStage = 910;
                        
                        if ( entity.IsBlackHoledAtMoment.Display )
                            continue; //whoops, it's blackholed at the moment, so don't do anything
							
                        if ( entity.CurrentCountOfTractorsPullingOnThis > 0 )
                            continue; //can't go through wormhole if being tractored

                        Planet targetPlanet = thisSideWormhole.GetLinkedPlanet();
                        if ( targetPlanet == null || targetPlanet == myPlanet )
                            continue;
                        debugStage = 920;

                        //Handle the Wait For Stragglers code
                        if ( entity.PlanetFaction.Faction.Type == FactionType.Player &&
                             entity.TypeData.IsFleetLeader && entity.FleetMembership.Fleet != null &&
                             entity.FleetMembership.Fleet.IsFleetInTransportLoadMode &&
                             !entity.GetIsCrippled() )
                        {
                            //If this is a flagship in load mode
                            PlayerAccount playerControlling = entity.PlanetFaction.Faction.GetFirstAssociatedPlayerAccountOrNull();
                            bool waitOnSafePlanets = false;
                            bool waitOnAllPlanets = false;
                            if ( playerControlling != null && playerControlling.GetNetworkAttachedBoolBySetting( "WaitForStragglers" ) )
                                waitOnSafePlanets = true;
                            if ( playerControlling != null && playerControlling.GetNetworkAttachedBoolBySetting( "WaitForStragglersFull" ) )
                                waitOnAllPlanets = true;
                            if ( !waitOnAllPlanets && !waitOnSafePlanets &&
                                 DysonSidekickFactionBaseInfo.GetIsThisADysonFaction( entity.PlanetFaction.Faction ) &&
                                 AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame("DysonAutoDefend") )
                                waitOnSafePlanets = true; //Dyson sidekick in auto-defend mode uses waitForStregglers

                            if ( waitOnAllPlanets || ( waitOnSafePlanets &&
                                 ( entity.Planet.GetControllingFaction().GetIsFriendlyTowards( entity.PlanetFaction.Faction ) ||
                                 ( entity.Planet.GetControllingFaction().GetIsNeutralTowards( entity.PlanetFaction.Faction ) &&
                                   entity.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength == 0 ) ) ) )
                            {
                                if (entity.FleetMembership.Fleet.CalculateCountOfMobileFleetMembersOnThisPlanetThatCanLoadIntoTransports() > 0)
                                    continue;
                            }
                        }

                        FactionType entityFactionType = entity.GetFactionTypeSafe();
                        switch ( entityFactionType )
                        {
                            case FactionType.Player:
                                if ( targetPlanet.IsBlockedToPlayerTravelViaWormholes )
                                {
                                    if ( ArcenTime.TimeSinceStartF - EntityOrder.LastTimeReportedOnUnableToGoToPlanet > 3f )
                                    {
                                        EntityOrder.LastTimeReportedOnUnableToGoToPlanet = ArcenTime.TimeSinceStartF;
                                        World_AIW2.Instance.QueueChatMessageOrCommand( "Apologies, but travel to the planet " + targetPlanet.Name + " is disallowed " +
                                            (World_AIW2.Instance.TutorialOrNull == null ? "right now." : "in this tutorial."), ChatType.ShowLocallyOnly, null );
                                    }
                                    entity.Orders.ClearOrders( ClearBehavior.AnythingIncludingAttackerMode, 
                                        ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.YesClearAnyOrders_IncludingFromHumans, "Wormhole Blocked : Planet is blocked to Players" );
                                    continue;
                                }
                                break;
                            default:
                                if ( targetPlanet.IsBlockedToNPCTravelViaWormholes )
                                {
                                    entity.Orders.ClearOrders( ClearBehavior.AnythingIncludingAttackerMode,
                                        ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.YesClearAnyOrders_IncludingFromHumans, "Wormhole Blocked : Planet is blocked To NPCs" );
                                    continue;
                                }
                                break;
                        }

                        debugStage = 1000;
                        if ( !entity.TypeData.CanPassThroughEnemyForcefields )
                        {
                            bool foundBlockingShield = false;
                            debugStage = 1100;
                            foreach ( GameEntity_Squad shieldGenerator in thisSideWormhole.Planet.Squads( EntityRollupType.ProjectsForcefield ) )
                            {
                                debugStage = 2000;
                                if ( !shieldGenerator.GetIsHostileTowards_Safe( faction ) )
                                    continue;
                                int distanceThreshold = shieldGenerator.CalculatedCurrentShieldRadius;
                                if ( distanceThreshold <= 0 )
                                    continue;
                                if (shieldGenerator.TypeData.OriginalXmlData.GetBool( "custom_forcefield_is_nonblocking", false, false ))
                                    continue;
                                debugStage = 2100;
                                distanceThreshold += entity.DataForMark.Radius;
                                distanceThreshold += thisSideWormhole.TypeData.BaseMark.Radius;
                                if ( shieldGenerator.GetDistanceTo_ExpensiveAccurate( thisSideWormhole, RadiusCheck.IgnoreRadii, false ) > distanceThreshold )
                                    continue;
                                foundBlockingShield = true;
                                break;
                            }
                            debugStage = 2200;
                            if ( foundBlockingShield && !entity.TypeData.PushesEnemyShields && !entity.GetIsCrippled() )
                                continue; //if the wormhole is blocked by a forcefield, don't let the ships go in (unless they have the norris effect or are crippled)
                        }
                        debugStage = 3000;
                        debugStage = 3100;
                        GameEntity_Other otherSideWormhole = null;
                        PlanetFaction targetPlanetDestinationFaction = null;
                        try
                        {
                            debugStage = 3200;
                            debugStage = 3300;
                            otherSideWormhole = targetPlanet.GetWormholeTo( myPlanet );
                            if ( otherSideWormhole == null )
                            {
                                // jcf: This is the moment where we could instead pick a position on the edge of the destination planets gravity well facing the source planet
                                //      and allow travel, if we wanted to actually support one-way wormholes, which would be -cool-.
                                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "Was looking for a wormhole on the far side of " + targetPlanet.Name + " from " + myPlanet.Name +
                                        " with wormhole on " + thisSideWormhole.GetPlanetName_Safe() + " but couldn't find one.  We had one in one direction only.", Verbosity.DoNotShow );
                                continue;
                            }
                            debugStage = 3400;
                            targetPlanetDestinationFaction = targetPlanet.GetPlanetFactionForFaction( faction );
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception when trying to move " + entity.ToStringWithPlanetAndOwner() + " to " + targetPlanet.Name + " " + e.ToString(), Verbosity.DoNotShow );
                        }


                        debugStage = 4000;
                        if ( !didShipsGoThroughWormhole )
                        {
                            if ( currentIndex == targetPlanet.Index )
                            {
                                debugStage = 4100;
                                shipWormholePoint = otherSideWormhole.WorldLocation;
                                didShipsGoThroughWormhole = true;
                            }
                            else if ( currentIndex == myPlanet.Index )
                            {
                                debugStage = 4200;
                                shipWormholePoint = thisSideWormhole.WorldLocation;
                                didShipsGoThroughWormhole = true;
                            }
                        }

                        //ArcenPoint exitAnimationStartingPoint = entity.WorldLocation;
                        //AngleDegrees exitAnimationAngle = Engine_AIW2.Instance.CombatCenter.GetAngleToDegrees( thisSideWormhole.WorldLocation );
                        //int exitAnimationDistance = Engine_AIW2.Instance.CombatCenter.GetDistanceTo( thisSideWormhole.WorldLocation, false ) * 1000;
                        //ArcenPoint exitAnimationTargetPoint = Engine_AIW2.Instance.CombatCenter.GetPointAtAngleAndDistance( exitAnimationAngle, exitAnimationDistance );
                        //CHRIS_TODO: animation of ship zipping off into the distance from exitAnimationStartingPoint to exitAnimationTargetPoint (to get those, uncomment the four lines above)
                        //bool isPlayerFactionEntity = faction.Type == FactionType.Player;

                        debugStage = 5000;
                        entity.RemoveFromSpatialPartitioning( false, InstancedRendererDeactivationReason.WentThroughWormhole );
                        entity.UnitForciblyDecloaked = false; //the temporary "force decloak" is unset when a unit goes through a wormhole
                                                              //AngleDegrees angleToWormhole = Engine_AIW2.Instance.CombatCenter.GetAngleToDegrees( otherSideWormhole.WorldLocation );
                                                              //int distanceToWormhole = Engine_AIW2.Instance.CombatCenter.GetDistanceTo( otherSideWormhole.WorldLocation, false );
                        debugStage = 5100;
                        AngleDegrees spawnAngle = AngleDegrees.Create( (float)Context.RandomToUse.Next( 1, 360 ) );
                        FInt distance = Context.RandomToUse.Next(0, 30) * FInt.FromParts(0, 001);
                        ArcenPoint exitPoint = otherSideWormhole.WorldLocation.GetPointAtAngleAndDistance( spawnAngle, (targetPlanet.GravWellSize.DistanceScale_GravwellRadius * distance ).IntValue );
                        entity.GameSecondEnteredThisPlanet = World_AIW2.Instance.GameSecond;
                        entity.SetWorldLocation( exitPoint );

                        debugStage = 5200;
                        pFaction.SwitchToFaction( entity, targetPlanetDestinationFaction, true, "Normal Wormhole Traversal" );
                        entity.AddToNewPlanet( targetPlanet, "Normal Wormhole Traversal" );
                        this.ShipLogicForAfterItTransitsWormhole( entity, myPlanet, true );

                        debugStage = 5300;
                        //clear any decollision orders that were previously in place before going through the wormhole.
                        entityOrders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.OnlyClearDecollisionOrdersAndNothingElse, ClearSource.DoNotClearAnythingExceptDecollision, "DoWormholeTraversalLogic" );

                        debugStage = 5400;
                        if ( entity.GuardedUnit.GetSquad() == null &&
                             entity.GuardOrPatrolOffsetPoints.Count > 0 &&
                             GetShouldEntityUseImplicitMeleeAttackerBehavior( entity, faction, entityOrders ) )
                            entity.GuardOrPatrolOffsetPoints.Clear(); // clear melee unit's memory of where to go back to

                        debugStage = 6000;
                        if ( entity.CurrentlyStrongestTractorSourceHittingThese.Count > 0 &&
                            (humansCanTractorEnemiesThroughWormholes || entityFactionType != FactionType.Player ) )
                        {
                            debugStage = 6100;
                            List<int> iAmTheStrongestTractorHittingThese = entity.CurrentlyStrongestTractorSourceHittingThese.GetDisplayList();
                            for ( int k = 0; k < iAmTheStrongestTractorHittingThese.Count; k++ )
                            {
                                debugStage = 6200;
                                int id = iAmTheStrongestTractorHittingThese[k];
                                GameEntity_Squad tractoredEntity = World_AIW2.Instance.GetEntityByID_Squad( id );
                                if ( tractoredEntity == null )
                                    continue;
                                if ( !tractoredEntity.TypeData.CanGoThroughWormholes )
                                    continue; //ships that normally can't pass through wormholes (like station-keepers) can't be dragged away
                                debugStage = 6300;
                                if ( tractoredEntity.TypeData.ProjectsForcefield && tractoredEntity.ShieldPointsLost < tractoredEntity.GetMaxShieldPoints() )
                                    continue; // can tractor something with a shield, but you can't actually tow it
                                debugStage = 6400;
                                if ( tractoredEntity.Planet == targetPlanet ) // might already be there if both are still mobile and both are trying to transit the same wormhole
                                    continue;

                                FactionType tractoredFactionType = tractoredEntity.GetFactionTypeSafe();
                                if ( tractoredFactionType == FactionType.Player && targetPlanet.IntelLevel <= PlanetIntelLevel.Unexplored )
                                    continue; //player ships can no longer be dragged into unexplored planets by enemy tractor beams.  This was allowing for extra scouting by virtue of enemy tractor beams.

                                debugStage = 6500;
                                tractoredEntity.RemoveFromSpatialPartitioning( false, InstancedRendererDeactivationReason.DraggedThroughWormholeByTractorBeam );

                                debugStage = 6600;
                                tractoredEntity.PlanetFaction.SwitchToFaction( tractoredEntity, targetPlanet.GetPlanetFactionForFaction( tractoredEntity.PlanetFaction.Faction ), true, "Wormhole Traversal - Dragged By Tractor" );

                                debugStage = 6700;
                                tractoredEntity.GameSecondEnteredThisPlanet = World_AIW2.Instance.GameSecond;
                                //for tractored entities, pop them out of the wormhole at a random place near the exitPoint.
                                //It might be more efficient to calculate this range elsewhere and cache it?
                                int tractorRange = 0;
                                for ( int index = 0; index < entity.Systems.Count; index++ )
                                {
                                    debugStage = 6800;
                                    EntitySystem system = entity.Systems[index];
                                    if ( system.DataForMark.TractorCount <= 0 )
                                        continue;
                                    if ( system.ForShortTermPlanning_DisabledReason != ArcenRejectionReason.Unknown )
                                        continue;
                                    debugStage = 6900;
                                    tractorRange = Math.Max( tractorRange, system.DataForMark.TractorRange );
                                }
                                debugStage = 7000;
                                //if no tractor beam range found, then probably it is a reverse beam
                                if ( tractorRange <= 0 )
                                {
                                    debugStage = 7100;
                                    for ( int index = 0; index < tractoredEntity.Systems.Count; index++ )
                                    {
                                        debugStage = 7200;
                                        EntitySystem system = tractoredEntity.Systems[index];
                                        if ( system.DataForMark.TractorCount <= 0 )
                                            continue;
                                        if ( system.ForShortTermPlanning_DisabledReason != ArcenRejectionReason.Unknown )
                                            continue;
                                        debugStage = 7300;
                                        tractorRange = Math.Max( tractorRange, system.DataForMark.TractorRange );
                                    }
                                }
                                //if still not found for some reason, then probably a beam shut off.  Go ahead and use at least some range
                                if ( tractorRange <= 0 )
                                    tractorRange = 200;

                                debugStage = 8000;
                                AngleDegrees angle = AngleDegrees.Create( (float)Context.RandomToUse.Next( 1, 360 ) );
                                ArcenPoint point = exitPoint.GetPointAtAngleAndDistance( angle, tractorRange );
                                debugStage = 8100;
                                if ( targetPlanet.GetIsPointOutsideGravWell_SlowButCorrect( point ) )
                                {
                                    debugStage = 8200;
                                    //if it would be outside the gravwell, mirror it to inside the gravwell.
                                    //I hope this code path is hit infrequently enough to not cause slowdowns.
                                    point = exitPoint.GetPointAtAngleAndDistance( angle.GetOpposite(), tractorRange );
                                    if ( targetPlanet.GetIsPointOutsideGravWell_SlowButCorrect( point ) ) {
                                        point = targetPlanet.GetPointOnRadiusOfGravWellIfOutOfRange_Slow( point );
                                    }
                                }
                                debugStage = 8300;
                                //if tractor range would put the units outside the gravwell, don't
                                tractoredEntity.SetWorldLocation( point );

                                debugStage = 8400;
                                tractoredEntity.AddToNewPlanet( targetPlanet, "Wormhole Traversal - Dragged By Tractor" );
                                debugStage = 8500;
                                this.ShipLogicForAfterItTransitsWormhole( tractoredEntity, myPlanet, false );
                            }
                        }

                        debugStage = 9000;
                        //ArcenPoint entranceAnimationTargetPoint = entity.WorldLocation;
                        //AngleDegrees entranceAnimationAngle = angleToWormhole;
                        //int entranceAnimationDistance = distanceToWormhole * 1000;
                        //ArcenPoint entranceAnimationStartingPoint = Engine_AIW2.Instance.CombatCenter.GetPointAtAngleAndDistance( entranceAnimationAngle, entranceAnimationDistance );
                        //CHRIS_TODO: animation of ship zipping in from the distance from entranceAnimationStartingPoint to entranceAnimationTargetPoint (to get those, uncomment the four lines above)
                    }
                    debugStage = 9500;
                    planet.ShipsReadyForWormholeTraversalAtEndOfThisFrame.Clear();
                }

                debugStage = 11200;
                if ( didShipsGoThroughWormhole )
                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.PlayLocallyOnly, SFXItemType_Positional.WormholeTransit, shipWormholePoint );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "DoWormholeTraversalLogic error at debugStage " + debugStage + ": " + e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region ShipLogicForAfterItTransitsWormhole
        public void ShipLogicForAfterItTransitsWormhole( GameEntity_Squad Ship, Planet OldPlanet, bool MadeTransitOfOwnFreeWill )
        {
            //these things ONLY happen when ships go through wormholes, rather than whenever they are added to a planet from other sources.
            //so for instance, not when exiting transports or guard posts.  This is a useful distinction.
            if ( Ship == null )
                return;

            #region StateOfMatterToBecomeOnWormholeExit
            if ( Ship.TypeData.StateOfMatterToBecomeOnWormholeExit != null )
            {
                Ship.CurrentStateOfMatter = Ship.TypeData.StateOfMatterToBecomeOnWormholeExit;
                if ( Ship.TypeData.ReturnsToDefaultStateOfMatterAfterSecondsFromWormholeExit > 0 )
                    Ship.GameSecondWillExitStateOfMatter = World_AIW2.Instance.GameSecond + Ship.TypeData.ReturnsToDefaultStateOfMatterAfterSecondsFromWormholeExit;
                else
                    Ship.GameSecondWillExitStateOfMatter = 0; //stay in there indefinitely, apparently!
            }
            #endregion

            Ship.HostOnly_SendExactLocationUntil = ArcenTime.TimeSinceStartF + 3f; //send exact position for the next 3 seconds

            Planet plan = Ship.Planet;
            if ( ( plan != null && plan.BattleStatus == PlanetBattleStatus.Tier1_PlayerLookingAtMe ) ||
                (OldPlanet != null && OldPlanet.BattleStatus == PlanetBattleStatus.Tier1_PlayerLookingAtMe ) )
                Ship.FlagForForcedFullSyncToClients_FromHost();
        }
        #endregion

        #region DoCombatStepForPlanet
        public override void DoCombatStepForPlanet( Planet planet, ArcenClientOrHostSimContextCore Context )
        {
            if ( CentralVars.DEBUG_TURN_OFF_COMBAT_STEP_AT_PLANETS )
                return;

            if ( planet == null || !planet.BattleStatus_ProcessThisSimStep )
            {
                return; //this is a background planet battle (or background ships moving around), so skip is this frame
            }

            Interlocked.Increment( ref Accumulator_CSPB );

            ArcenClientOrHostSimContextCore DelegateHelper_Context = Context;

            Context.RandomToUse.ReinitializeWithSeed( World_AIW2.Instance.Network_CurrentFrameNumber + planet.RandomSeed );

            int debugStage = 0;
            try
            {

                if ( !CentralVars.DEBUG_TURN_OFF_COMBAT_CENTRAL_STUFF_PER_STEP_LOGIC )
                {
                    Interlocked.Increment( ref Accumulator_CCSB );
                    
                    debugStage = 1000;
                    this.RecalculateAICounterattackForcesStrength( planet );

                    debugStage = 1200;
                    
                    #region beginning-of-frame processing for per-frame AI variables
                    foreach ( GameEntity_Squad entity in planet.Squads() )
                    {
                        debugStage = 1250;
                        FleetMembership fleetMem = entity.FleetMembership;
                        if ( fleetMem == null )
                            continue;
                        PlanetFaction pFaction = entity.PlanetFaction;
                        if ( pFaction == null )
                            continue;
                        Faction faction = pFaction.Faction;
                        if ( faction == null )
                            continue;

                        debugStage = 1300;
                        entity.CalculatedAttackerDamageTotal_LastFrame = entity.CalculatedAttackerDamageTotal_InProgress;
                        debugStage = 1400;
                        entity.CalculatedAttackerDamageTotal_InProgress = 0;
                        debugStage = 1500;
                        if ( faction.Type == FactionType.Player )
                        {
                            debugStage = 1600;
                            if ( fleetMem != null )
                            {
                                debugStage = 1700;
                                entity.SetCurrentMarkLevelIfHigherThanCurrent( fleetMem.EffectiveMark );
                            }
                        }
                    }

                    debugStage = 1800;
                    if ( planet.WhiteoutRemainingDuration > 0 )
                    {
                        planet.WhiteoutRemainingDuration -= Engine_Universal.GameDeltaTime;
                    }
                    #endregion

                    #region do per-step logic for each natural object
                    if ( !CentralVars.DEBUG_TURN_OFF_COMBAT_NATURAL_OBJECT_PER_STEP_LOGIC )
                    {
                        debugStage = 3000;
                        foreach ( GameEntity_Other entity in planet.Others() )
                        {
                            debugStage = 3100;
                            entity.DoEntityStepLogic_NaturalObject(Context);
                        }
                    }
                    #endregion

                    Interlocked.Increment( ref Accumulator_CCSF );
                }

                #region do per-step logic for each ship
                if ( !CentralVars.DEBUG_TURN_OFF_COMBAT_SHIP_PER_STEP_LOGIC )
                {
                    Interlocked.Increment( ref Accumulator_CSIB );

                    bool doShotsAllInstaHit = AIWar2GalaxySettingQuickAccess.ShotsAllInstaHit;

                    debugStage = 4000;
                    for ( int i = 0; i < planet.Factions.Count; i++ )
                    {
                        PlanetFaction faction = planet.Factions[i];
                        if ( faction.Entities.SquadCount <= 0 )
                            continue;
                        Context.RandomToUse.ReinitializeWithSeed( World_AIW2.Instance.Network_CurrentFrameNumber + planet.RandomSeed + i );

                        foreach ( GameEntity_Squad entity in faction.Entities.Squads() )
                        {
                            debugStage = 4100;
                            if ( entity == null )
                                continue;
                            debugStage = 4150;
                            bool wasSelected = entity.GetIsSelected();
                            debugStage = 4200;
                            entity.DoEntityStepLogic_Ship( planet.LocalDeltaTime_UnpausedOnly, DelegateHelper_Context, doShotsAllInstaHit );
                            //if ( !entity.MovedLastFrame && entity.DecollisionMoveTarget.X != 0 )
                            //    entity.DecollisionMoveTarget = ArcenPoint.ZeroZeroPoint;
                            debugStage = 4300;
                            if ( entity.ToBeRemovedAtEndOfThisFrame || entity.GetHasBeenDestroyed() )
                            {
                                debugStage = 4400;
                                if ( entity.ExtraStackedSquadsInThis > 0 ) //better late than never...
                                {
                                    debugStage = 4500;
                                    entity.EjectEntireStackFromMyselfIfPresent( Context.GetHostOnlyContext(), 0, wasSelected );
                                }
                            }
                        }

                        //now for this same faction, do the late entity processing just for those that need it
                        foreach ( GameEntity_Squad entity in faction.Entities.Squads( EntityRollupType.DoesLateEntityProcessing ) )
                        {
                            debugStage = 5100;
                            if ( entity == null )
                                continue;
                            debugStage = 5200;
                            entity.DoEntityStepLogic_Ship_Late( planet.LocalDeltaTime_UnpausedOnly, DelegateHelper_Context );
                        }
                    }

                    Interlocked.Increment( ref Accumulator_CSIF );
                }
                #endregion

                #region move each shot on the board
                if ( !CentralVars.DEBUG_TURN_OFF_COMBAT_SHOT_MOVEMENT )
                {
                    Interlocked.Increment( ref Accumulator_CSOB );

                    debugStage = 6000;
                    foreach ( GameEntity_Shot entity in planet.Shots() )
                    {
                        debugStage = 6100;
                        if ( entity.TypeData == null )
                        {
                            debugStage = 6200;
                            entity.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.MissingTypeData );
                            continue;
                        }

                        debugStage = 6300;
                        //UnityEngine.Debug.Log( entity.PrimaryKeyID + ":" + entity.TypeData.InternalName + " "  + ( entity.Planet == null ? "null" : entity.GetPlanetName_Safe() ) + " crea " +
                        //    entity.SecondsSinceCreation + " rem " + entity.ToBeRemovedAtEndOfThisFrame + " loc " + entity.WorldLocation );
                        if ( entity.DoEntityStepLogic_Shot( DelegateHelper_Context, planet.LocalDeltaTime_UnpausedOnly ) )
                        {
                            debugStage = 6400;
                            if ( entity.VisualLinkObject_GenericOnly != null || entity.InstancedRenderer != null )
                                entity.IHaveDiedSoPleaseDisregardAnyRequestForANewVisualObject = true;
                            debugStage = 6500;
                            entity.DoOnDeathInCombatLogic( DelegateHelper_Context );
                            debugStage = 6600;
                            entity.SetTarget( null ); // decrements overkill counter, etc
                            debugStage = 6700;
                            entity.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.ShotHasReachedTargetSoGetRidOfShot );
                        }
                    }

                    Interlocked.Increment( ref Accumulator_CSOF );
                }
                #endregion
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( planet.Name + " DoCombatStepForPlanet error at debugStage " + debugStage + ": " + e, Verbosity.ShowAsError );
            }

            DelegateHelper_Context = null;


            Interlocked.Increment( ref Accumulator_CSPF );
        }
        #endregion

        #region DoCombatSecond_FromSimBGThread
        public override void DoCombatSecond_FromSimBGThread( Planet planet, ArcenClientOrHostSimContextCore Context )
        {
            if ( CentralVars.DEBUG_TURN_OFF_COMBAT_PER_SECOND_PLANET_LOGIC )
                return;

            this.DoAICounterattackForcesPerSecondLogic( planet, Context.GetHostOnlyContext() );
        }
        #endregion

        #region CheckForInternalShipDeployment_DroneProducers_FromSimBGThread
        private void CheckForInternalShipDeployment_DroneProducers_FromSimBGThread( Planet planet, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return;

            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.DroneProducer ) )
            {
                FleetMembership fleetMem = entity.FleetMembership;
                if ( fleetMem == null )
                    continue;
                
                PlanetFaction pFaction = entity.PlanetFaction;
                if ( pFaction == null )
                    continue;
                
                Faction faction = pFaction.Faction;
                if ( faction == null )
                    continue;
                
                GameEntityTypeData.MarkLevelStats entityMarkData = entity.DataForMark;
                if ( entityMarkData == null )
                    continue;
                
                bool threatPresent = false;
                PlanetFaction pFac = pFaction;
                if ( pFac != null )
                {
                    foreach ( PlanetFaction f in pFac.RelatedFactions( FactionRelationship.FactionsIAmHostileTowards ) )
                    {
                        foreach ( GameEntity_Squad e in f.Entities.Squads( EntityRollupType.Combatants ) )
                        {
                            if (e.GetIsCrippled())
                                continue;
                            if (e.SecondsSpentAsRemains > 0)
                                continue;

                            threatPresent = true;
                            break;
                        }

                        if ( threatPresent )
                            break;
                    }
                    
                    //ArcenDebugging.ArcenDebugLogSingleLine( entity.TypeData.InternalName + " DeployDroneContents? isDeploying" + isDeploying, Verbosity.DoNotShow );
                }
                
                if ( threatPresent )
                    fleetMem.Fleet.DeployDroneContents( Context, DeployReason.ThreatDetected );
                else
                    fleetMem.Fleet.RecallDeployedDrones( Context, RecallReason.NoThreat );

            }
        }
        #endregion

        #region DoAICounterattackForcesPerSecondLogic
        public void DoAICounterattackForcesPerSecondLogic( Planet planet, ArcenHostOnlySimContext Context )
        {
            if ( Context == null )
            {
                // client still counts down timer though
                if (planet.AICountdownTimerForCounterattack > 0)
                    planet.AICountdownTimerForCounterattack--;
                return;
            }

            int debugStage = 0;
            try
            {
                debugStage = 100;

                #region Spend any of the AICounterattackUnspentBudget if it is there
                if ( planet.AICounterattackUnspentBudget > FInt.Zero )
                {
                    debugStage = 200;
                    if ( planet.ShipGroup_WavesFromHere_Normal != null )
                    {
                        debugStage = 300;

                        bool madeAnyAdditions = false;
                        int numFailures = 0;

                        while ( numFailures < 5 )
                        {
                            var typeToAdd = planet.ShipGroup_WavesFromHere_Normal.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );

                            debugStage = 400;
                            
                            if (typeToAdd == null)
                            {
                                numFailures++;
                                continue;
                            }

                            debugStage = 500;

                            if (typeToAdd.CostForAIToPurchase > planet.AICounterattackUnspentBudget)
                            {
                                numFailures++;
                                continue;
                            }

                            debugStage = 600;

                            if ( !planet.AICounterattackForces.ContainsKey( typeToAdd ) )
                                planet.AICounterattackForces[typeToAdd] = 1;
                            else
                                planet.AICounterattackForces[typeToAdd]++;
                            
                            debugStage = 600;
                            
                            planet.AICounterattackUnspentBudget -= typeToAdd.CostForAIToPurchase;
                            madeAnyAdditions = true;
                        }

                        debugStage = 700;

                        if ( madeAnyAdditions )
                            this.RecalculateAICounterattackForcesStrength( planet );
                    }
                }
                #endregion

                debugStage = 1000;

                if ( planet.PrecalculatedAICounterattackForcesStrength <= 0 )
                    return;

                if ( planet.AICounterAttacksCurrentlyStalled )
                    return;
                if ( planet.AICounterAttacksNotSufficientToTryToSend )
                    return;

                debugStage = 1100;

                planet.AICountdownTimerForCounterattack--;

                debugStage = 1100;

                if ( planet.AICountdownTimerForCounterattack <= 0 )
                {
                    debugStage = 2000;
                    //ArcenDebugging.ArcenDebugLogSingleLine( "START trying to launch counterattack!", Verbosity.DoNotShow );

                    PlanetFaction pFactionToUse = null;
                    debugStage = 2100;
                    byte bestMarkLevelOfPresent = planet.MarkLevelForAIOnly.Ordinal;
                    debugStage = 2200;
                    //int counterAttackEnablerCount = 0;
                    foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.AICounterattackEnablers ) )
                    {
                        PlanetFaction localPFaction = entity.PlanetFaction;
                        if ( localPFaction == null )
                            continue;
                        Faction faction = localPFaction.Faction;
                        if ( faction == null )
                            continue;
                        if ( faction.Type != FactionType.AI )
                            continue;
                        
                        //counterAttackEnablerCount++;

                        debugStage = 2400;
                        pFactionToUse = localPFaction;
                        
                        debugStage = 2500;
                        if ( faction.CurrentGeneralMarkLevel > bestMarkLevelOfPresent )
                            bestMarkLevelOfPresent = faction.CurrentGeneralMarkLevel;
                        
                    }

                    debugStage = 2600;
                    //missing a planet faction to spawn from, which means the player won!
                    if ( pFactionToUse == null )
                    {
                        debugStage = 2700;
                        planet.PrecalculatedAICounterattackForcesStrength = 0;
                        planet.AICounterattackForces.Clear();
                        planet.AICountdownTimerForCounterattack = 0;
                        planet.AICounterattackToBeStalledByPlayerStrengthOf = 999999;
                        planet.PlayerStrengthHereForBlockingAICounterAttacks = 0;
                        //ArcenDebugging.ArcenDebugLogSingleLine( "FAIL trying to launch counterattack because missing planet faction!", Verbosity.DoNotShow );
                        return;
                    }

                    debugStage = 3000;
                    AngleDegrees angle = AngleDegrees.Create( (float)Context.RandomToUse.Next( 1, 360 ) );
                    ArcenPoint center = Engine_AIW2.Instance.CombatCenter;
                    float warpInMultiplier = 0.7f;
                    debugStage = 3100;
                    ArcenPoint spawnLocation = center.GetPointAtAngleAndDistance( angle, (int)(planet.GravWellSize.DistanceScale_GravwellRadius * warpInMultiplier) );
                    ArcenPoint WarpInStart = center.GetPointAtAngleAndDistance( angle, planet.GravWellSize.DistanceScale_GravwellRadius );

                    debugStage = 3200;
                    int countSpawned = 0;
                    int strengthSpawned = 0;
                    debugStage = 3300;
                    foreach ( KeyValuePair<GameEntityTypeData, int> pair in planet.AICounterattackForces )
                    {
                        debugStage = 3500;
                        if ( pair.Value > 0 )
                        {
                            debugStage = 3600;
                            int numberToAdd = pair.Value;
                            while ( numberToAdd > 0 )
                            {
                                debugStage = 3700;
                                GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(
                                    pFactionToUse, pair.Key, pair.Key.MarkFor( bestMarkLevelOfPresent ),
                                    pFactionToUse.Faction.LooseFleet,
                                    0, //no differentiated sub-groups on loose fleets
                                    spawnLocation, Context, "AICounterattackForces" );
                                debugStage = 3800;
                                entity.spawnVis = SpawnVisualization.WarpIn;

                                debugStage = 3900;
                                entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                                numberToAdd--;

                                debugStage = 4000;
                                if ( numberToAdd >= 9 )
                                {
                                    debugStage = 4100;
                                    entity.AddOrSetExtraStackedSquadsInThis( 9, false );
                                    numberToAdd -= 9;
                                }
                                else if ( numberToAdd > 0 )
                                {
                                    debugStage = 4200;
                                    entity.AddOrSetExtraStackedSquadsInThis( (Int16)numberToAdd, false );
                                    numberToAdd = 0;
                                }

                                debugStage = 4300;
                                countSpawned += 1 + entity.ExtraStackedSquadsInThis;
                                strengthSpawned += entity.GetStrengthOfStack();
                            }
                        }
                    }
                    //ArcenDebugging.ArcenDebugLogSingleLine( "MORE countSpawned " + countSpawned + " strengthSpawned " + strengthSpawned +
                    //    " AICounterattackForces.GetPairCount " + planet.AICounterattackForces.Count, Verbosity.DoNotShow );
                    debugStage = 5000;
                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.PlayerFailsAtSomething );
                    //the spawning is done!
                    debugStage = 5100;
                    planet.PrecalculatedAICounterattackForcesStrength = 0;
                    debugStage = 5200;
                    planet.AICounterattackForces.Clear();

                    debugStage = 5300;
                    if ( countSpawned > 0 )
                    {
                        debugStage = 5400;
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = planet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( "AI Counterattack at " + planet.Name + ": " + countSpawned + " ships of total strength " + (strengthSpawned / 1000f).ToString( "0.#" ),
                                ChatType.LogToCentralChat, "MaraudersAttacking", chatHandlerOrNull );
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in DoAICounterattackForcesPerSecondLogic at debugStage " + debugStage + ".  Exception: " + e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region RecalculateAICounterattackForcesStrength
        public void RecalculateAICounterattackForcesStrength( Planet planet )
        {
            if ( Engine_AIW2.Instance.IsTestChamber )
                return;
            int debugStage = 0;
            try
            {
                debugStage = 1000;
                if ( planet.MarkLevelForAIOnly == null )
                    planet.MarkLevelForAIOnly = Balance_MarkLevelTable.Instance.RowsByOrdinal[1];

                debugStage = 1050;
                int bestMarkLevelOfPresent = planet.MarkLevelForAIOnly.Ordinal;
                bool foundAnyEnablers = false;

                planet.AIStrengthRequiredToSend = 0;
                debugStage = 1100;
                foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.AICounterattackEnablers ) )
                {
                    FleetMembership fleetMem = entity.FleetMembership;
                    if ( fleetMem == null )
                        continue;
                    PlanetFaction pFaction = entity.PlanetFaction;
                    if ( pFaction == null )
                        continue;
                    Faction faction = pFaction.Faction;
                    if ( faction == null )
                        continue;
                    GameEntityTypeData.MarkLevelStats entityMarkData = entity.DataForMark;
                    if ( entityMarkData == null )
                        continue;

                    if ( pFaction != null && faction.Type == FactionType.AI )
                    {
                        foundAnyEnablers = true;
                        if ( faction.CurrentGeneralMarkLevel > bestMarkLevelOfPresent )
                            bestMarkLevelOfPresent = faction.CurrentGeneralMarkLevel;
                        if ( planet.AIStrengthRequiredToSend < faction.CounterattackMinStrengthFromExternal )
                            planet.AIStrengthRequiredToSend = faction.CounterattackMinStrengthFromExternal;

                    }
                }
                debugStage = 1200;
                if ( !foundAnyEnablers )
                {
                    planet.PrecalculatedAICounterattackForcesStrength = 0;
                    planet.AICounterattackForces.Clear();
                    planet.AICountdownTimerForCounterattack = 0;
                    planet.AICounterattackToBeStalledByPlayerStrengthOf = 999999;
                    planet.PlayerStrengthHereForBlockingAICounterAttacks = 0;
                    return;
                }

                debugStage = 1300;
                planet.PrecalculatedAICounterattackForcesStrength = 0;
                foreach ( KeyValuePair<GameEntityTypeData, int> pair in planet.AICounterattackForces )
                {
                    if ( pair.Value > 0 )
                        planet.PrecalculatedAICounterattackForcesStrength += (pair.Key.GetForMark( bestMarkLevelOfPresent ).StrengthPerSquad_CalculatedWithNullFleetMembership * pair.Value);
                }

                debugStage = 1400;
                planet.AICounterattackToBeStalledByPlayerStrengthOf = planet.PrecalculatedAICounterattackForcesStrength / 2;

                debugStage = 1500;
                planet.PlayerStrengthHereForBlockingAICounterAttacks = 0;
                Faction fac;
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    fac = World_AIW2.Instance.Factions[i];
                    if (fac == null)
                        continue;
                    if ( fac.Type != FactionType.Player && (fac.SpecialFactionData == null || !fac.SpecialFactionData.CountsAsPartOfPlayerStrengthAtPlanetForCounterattackBlocking) )
                        continue;

                    var pfac = planet.GetPlanetFactionForFaction( fac );
                    if (pfac == null)
                        continue;
                    
                    var selfData = pfac.DataByStance[FactionStance.Self];
                    planet.PlayerStrengthHereForBlockingAICounterAttacks += selfData.TotalStrength;
                }

                debugStage = 1600;
                if ( planet.PlayerStrengthHereForBlockingAICounterAttacks >= planet.AICounterattackToBeStalledByPlayerStrengthOf )
                {
                    planet.AICountdownTimerForCounterattack = ExternalConstants.Instance.TimeForCounterattackTimer;
                    planet.AICounterAttacksCurrentlyStalled = true;
                }
                else
                    planet.AICounterAttacksCurrentlyStalled = false;

                debugStage = 1700;
                if ( planet.AIStrengthRequiredToSend > planet.PrecalculatedAICounterattackForcesStrength )
                    planet.AICounterAttacksNotSufficientToTryToSend = true;
                else
                    planet.AICounterAttacksNotSufficientToTryToSend = false;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "RecalculateAICounterattackForcesStrength exception at debugStage " + debugStage + ", exception: " + e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region DoWorldStepLogic_ClientOrHost_FromSimBGThread
        public override void DoWorldStepLogic_ClientOrHost_FromSimBGThread( ArcenClientOrHostSimContextCore Context )
        {
            if ( CentralVars.DEBUG_TURN_OFF_WORLD_STEP_LOGIC )
                return;
            if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                return;
            if ( World_AIW2.Instance == null )
                return;

            Stopwatch perFrameSW = new Stopwatch();
            perFrameSW.Start();

            //ArcenDebugging.ArcenDebugLogSingleLine( "Start WorldStepLogic: TID" + Thread.CurrentThread.ManagedThreadId, Verbosity.DoNotShow );

            int debugStage = 0;
            try
            {
                debugStage = 10;
                World_AIW2.Instance.OnServer_FramesStoredUpForNextBatch++;
                World_AIW2.CurrentSimCycleSlow = GameEntity_Base.CalculateSimCycleFromInt_Slow( World_AIW2.Instance.Network_CurrentFrameNumber );
                World_AIW2.CurrentSimCycleFast = GameEntity_Base.CalculateSimCycleFromInt_Fast( World_AIW2.Instance.Network_CurrentFrameNumber );

                debugStage = 20;
                DoVeryFirstSimStepCalculationsLogicOnPlanets( Context );
                debugStage = 30;
                if ( World_AIW2.Instance.InSetupPhase )
                {
                    //if it's the setup phase, what to do?
                }
                else
                {
                    debugStage = 100;

                    Interlocked.Increment( ref Accumulator_BLDC );

                    debugStage = 200;

                    PerFrameTicksLastFrame_Added++;
                    if ( PerFrameTicksLastFrame_Added >= 10 )
                    {
                        ResetCounter( PerFrameTicksLastFrame_GlobalPlayer );
                        ResetCounter( PerFrameTicksLastFrame_WorldStep1 );
                        ResetCounter( PerFrameTicksLastFrame_WorldSec );
                        ResetCounter( PerFrameTicksLastFrame_Fleet );
                        ResetCounter( PerFrameTicksLastFrame_Scenario );
                        ResetCounter( PerFrameTicksLastFrame_Faction );
                        ResetCounter( PerFrameTicksLastFrame_Planet );
                        ResetCounter( PerFrameTicksLastFrame_Combat );
                        ResetCounter( PerFrameTicksLastFrame_RemoveDead );
                        ResetCounter( PerFrameTicksLastFrame_ShipAILogic );
                        ResetCounter( PerFrameTicksLastFrame_WorldSuffix );
                        PerFrameTicksLastFrame_Added = 0;
                    }

                    {
                        debugStage = 300;
                        Interlocked.Increment( ref Accumulator_GPLA );
                        long startingTicks = perFrameSW.ElapsedTicks;

                        DoGlobalPlayerPerStepLogic( Context );

                        PerFrameTicksLastFrame_GlobalPlayer.RightItem += ( perFrameSW.ElapsedTicks - startingTicks );
                    }

                    {
                        debugStage = 400;
                        Interlocked.Increment( ref Accumulator_WTIM );
                        long startingTicks = perFrameSW.ElapsedTicks;

                        DoWorldStepLogic_ProcessArbitraryAmountOfTime( Context );

                        PerFrameTicksLastFrame_WorldStep1.RightItem += ( perFrameSW.ElapsedTicks - startingTicks );
                    }

                    {
                        debugStage = 500;
                        Interlocked.Increment( ref Accumulator_WSEC );
                        long startingTicks = perFrameSW.ElapsedTicks;

                        DoWorld_PerStep_TimeKeeping( Context );

                        PerFrameTicksLastFrame_WorldSec.RightItem += ( perFrameSW.ElapsedTicks - startingTicks );
                    }

                    {
                        debugStage = 600;
                        Interlocked.Increment( ref Accumulator_FLC );
                        long startingTicks = perFrameSW.ElapsedTicks;

                        //Asynchronously run fleet per-step logic.  We want to have it happen once per sim frame,
                        //and all on one shared thread, but its timing does not need perfection
                        ArcenThreading.RunTaskOnBackgroundThread( "_Sim.FleetPerStep", false, false, delegate
                        {
                            DoFleetPerStepCalculationsOnly();
                        } );

                        PerFrameTicksLastFrame_Fleet.RightItem += ( perFrameSW.ElapsedTicks - startingTicks );
                    }

                    {
                        debugStage = 700;
                        long startingTicks = perFrameSW.ElapsedTicks;

                        DoScenarioPerStepLogic( Context );

                        PerFrameTicksLastFrame_Scenario.RightItem += ( perFrameSW.ElapsedTicks - startingTicks );
                    }

                    {
                        debugStage = 900;
                        Interlocked.Increment( ref Accumulator_WOR );
                        long startingTicks = perFrameSW.ElapsedTicks;

                        DoWorldPerStepLogic( Context );

                        PerFrameTicksLastFrame_WorldStep2.RightItem += ( perFrameSW.ElapsedTicks - startingTicks );
                    }

                    {
                        debugStage = 1000;
                        Interlocked.Increment( ref Accumulator_PLA );
                        long startingTicks = perFrameSW.ElapsedTicks;

                        //Asynchronously run planet per-step logic.  We want to have it happen once per sim frame,
                        //and all on one shared thread, but its timing does not need perfection
                        ArcenThreading.RunTaskOnBackgroundThread( "_Sim.PlanetPerStep", false, false, delegate
                        {
                            DoPlanetPerStepLogicAsync();
                        } );

                        PerFrameTicksLastFrame_Planet.RightItem += ( perFrameSW.ElapsedTicks - startingTicks );
                    }

                    {
                        debugStage = 1100;
                        long startingTicks = perFrameSW.ElapsedTicks;
                        Interlocked.Increment( ref Accumulator_COM );
                        DoCombatPerStepLogic( Context );

                        PerFrameTicksLastFrame_Combat.RightItem += ( perFrameSW.ElapsedTicks - startingTicks );
                    }

                    {
                        debugStage = 1200;
                        long startingTicks = perFrameSW.ElapsedTicks;
                        Interlocked.Increment( ref Accumulator_REM );
                        DoRemoveDeadEntitiesLogic( Context );

                        PerFrameTicksLastFrame_RemoveDead.RightItem += ( perFrameSW.ElapsedTicks - startingTicks );
                    }

                    {
                        debugStage = 1300;
                        long startingTicks = perFrameSW.ElapsedTicks;
                        Interlocked.Increment( ref Accumulator_SAI );
                        DoShipAILogic( Context );

                        PerFrameTicksLastFrame_ShipAILogic.RightItem += ( perFrameSW.ElapsedTicks - startingTicks );
                    }

                    {
                        debugStage = 1400;
                        long startingTicks = perFrameSW.ElapsedTicks;
                        Interlocked.Increment( ref Accumulator_WSUF );
                        DoWorldSuffixLogic( Context );

                        PerFrameTicksLastFrame_WorldSuffix.RightItem += ( perFrameSW.ElapsedTicks - startingTicks );

                    }

                    debugStage = 1500;
                    Interlocked.Increment( ref Accumulator_BLDF );
                }

                Interlocked.Increment( ref Accumulator_NET );

                //do this last, so that the context object is in sync as much as possible
                {
                    debugStage = 1600;
                    long startingTicks = perFrameSW.ElapsedTicks;

                    //Asynchronously run faction per-step logic.  We want to have it happen once per sim frame,
                    //and all on one shared thread, but it doesn't have to be absolutely identical in timing every time, or block anything else.
                    ArcenThreading.RunTaskOnBackgroundThread( "_Sim.FactionPerStep", false, false, delegate
                    {
                        DoFactionPerStepLogic( Context );
                    } );

                    PerFrameTicksLastFrame_Faction.RightItem += (perFrameSW.ElapsedTicks - startingTicks);
                }

                debugStage = 2100;
                AIWar2NetworkSync.PerSecondCleanupOnClientOrHost_FromBGThread();

                Interlocked.Increment( ref Accumulator_FAC );

                debugStage = 5100;
                //the call below will not block the calling thread, but instead spawns many threads and just lets them run
                //we do this at the end so that it will be ready by the time the next sim step rolls around.
                ArcenShortTermPlanningManager.RunAllContextsOnBackgroundThreadsFromMainSimThread();
            }
            catch ( ArcenPleaseStopThisThreadException )
            { }//this is normal
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in DoWorldStepLogic_ClientOrHost_FromSimBGThread at debugStage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }
            //finally
            //{
            //    ArcenDebugging.ArcenDebugLogSingleLine( "Completed WorldStepLogic: TID" + Thread.CurrentThread.ManagedThreadId, Verbosity.DoNotShow );
            //}

            perFrameSW.Stop();
        }

        public void DoVeryFirstSimStepCalculationsLogicOnPlanets( ArcenClientOrHostSimContextCore Context )
        {
            int currentBackgroundStep = World_AIW2.Instance.Network_CurrentFrameNumber % 10;
            bool doCoarseProcessing = AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "CoarseProcessBackgroundPlanets" );

            #region VeryFirstSimStepcalculations Logic On Planets
            //this is faster on one thread rather than many...
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                planet.VeryFirstSimStepcalculations_Multithreaded( currentBackgroundStep, doCoarseProcessing );
            }
            #endregion
        }

        public void DoGlobalPlayerPerStepLogic( ArcenClientOrHostSimContextCore Context )
        {
            World_AIW2.Instance.GlobalPlayerPerStepLogic( Context );
        }

        public void DoWorldStepLogic_ProcessArbitraryAmountOfTime( ArcenClientOrHostSimContextCore Context )
        {
            TimeBasedPoolBase.ProcessTimeBasedPoolingChecksIfNeeded();
        }

        public void DoWorld_PerStep_TimeKeeping( ArcenClientOrHostSimContextCore Context )
        {
            #region Per Second Logic
            //Engine_Universal.BeginProfilerSample( "world-second" );
            World_AIW2.Instance.ProgressTowardsNextFullSecond += World_AIW2.Instance.SimulationProfile.SecondsPerFrameSim_GlobalOnly_NotForPlanetsOrCombat_UnpausedOnly;
            World_AIW2.Instance.IsFirstFrameOfSecond = false;
            while ( World_AIW2.Instance.ProgressTowardsNextFullSecond >= FInt.One )
            {
                World_AIW2.Instance.ProgressTowardsNextFullSecond -= FInt.One;

                DoWorldSecondLogic_FromSimBGThread( Context );
                World_AIW2.Instance.IsFirstFrameOfSecond = true;
            }
            //Engine_Universal.EndProfilerSample( "world-second" );
            #endregion
        }

        public void DoFleetPerStepCalculationsOnly()
        {
            #region Fleet Per Step Logic
            //first clear out all the list of player fleets at the planet
            foreach ( Planet planet in World_AIW2.Instance.Planets( true ) ) //include destroyed planets
            {
                planet.PlayerFleetsAtPlanet_Prior.Clear();
            }

            Faction localPlayerFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();

            //Engine_Universal.BeginProfilerSample( "fleet-step" );
            for ( int i = 0; i < World_AIW2.Instance.AllFleets.Count; i++ ) //paused or unpaused
                FleetSimLogic.Instance.PerFrame_CalculateEffectiveFleetData( World_AIW2.Instance.AllFleets[i], localPlayerFactionOrNull );

            foreach ( Planet planet in World_AIW2.Instance.Planets( true ) ) //include destroyed planets
            {
                List<Fleet> workingFleets = planet.PlayerFleetsAtPlanet_Prior;
                planet.PlayerFleetsAtPlanet_Prior = planet.PlayerFleetsAtPlanet_Current;
                planet.PlayerFleetsAtPlanet_Current = workingFleets;
            }

            FInt effectiveDeltaTime_UnpausedOnly = World_AIW2.Instance.SimulationProfile.SecondsPerFrameSim_GlobalOnly_NotForPlanetsOrCombat_UnpausedOnly;

            if ( !World.Instance.IsPaused & effectiveDeltaTime_UnpausedOnly > FInt.Zero )
            {
                //drone construction!
                foreach ( Fleet fleet in World_AIW2.Instance.Fleets( FleetStatus.CenterpieceMustLive ) )
                {
                    GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                    if ( centerpiece == null || 
                         centerpiece.GetIsCrippled() || 
                         centerpiece.SecondsSpentAsRemains > 0 || 
                         centerpiece.SelfBuildingMetalRemaining > 0 ||
                         centerpiece.GetFactionTypeSafe() == FactionType.NaturalObject ||
                         centerpiece.HasNotYetBeenFullyClaimed )
                    {
                        continue;
                    }
                    GameEntityTypeData centerpieceType = centerpiece.TypeData;
                    if ( centerpieceType == null )
                        continue;
                    if ( centerpieceType.FleetDesignTemplateIUseForDrones == null )
                        continue; //if no drone types for this centerpiece, then do nothing

                    GameEntityTypeData.MarkLevelStats centerpieceMarkStats = centerpiece.DataForMark;
                    if ( centerpieceMarkStats == null )
                        continue;
                    FInt metalFlowRate = centerpieceMarkStats.GetMetalFlowThroughput( MetalFlowPurpose.BuildingDronesInternally ) * effectiveDeltaTime_UnpausedOnly;
                    if ( metalFlowRate <= FInt.Zero )
                        continue; //if no metal throughput of this kind, ignore it

                    Faction fleetFaction = fleet.Faction;
                    bool isAClient = ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client;

                    foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                    {
                        if ( mem == null )
                            continue;
                        GameEntityTypeData typeData = mem.TypeData;
                        if ( typeData == null || !typeData.IsDrone )
                            continue; //must be a drone!

                        if ( mem.GetRemainingCap( false, -1, ExtraFromStacks.IncludePrecalc ) <= 0 )
                            continue; //if we're already at drone cap for this one, skip it!

                        if ( fleetFaction != null && fleetFaction.Type != FactionType.Player && typeData.NPCShipCap != null )
                        {
                            int currentCount = fleetFaction.NPCShipCountsByCapType == null ? 0 : fleetFaction.NPCShipCountsByCapType[typeData.NPCShipCap.RowIndexNonSim];
                            int cap = fleetFaction.SpecialFactionData.NPCShipCapsByType[typeData.NPCShipCap.RowIndexNonSim];

                            if ( currentCount >= cap )
                            {
                                if ( currentCount > cap && mem.EntitiesOfFMem.Count > 0 )
                                {
                                    //if the NPC faction is over drone count, and has drones deployed, then delete one for now to get back toward cap
                                    if ( !isAClient )
                                    {
                                        GameEntity_Squad shipToDelete = null;
                                        try
                                        {
                                            shipToDelete = mem.EntitiesOfFMem.GetFirst().Contained;
                                        }
                                        catch { }

                                        if ( shipToDelete != null )
                                            shipToDelete.OnlyInMapgenOrInActuallyGettingRidOfEntities_ImmediatelyRemoveFromSim( InstancedRendererDeactivationReason.SelfDestructOnTooHighOfCap );
                                    }
                                }

                                //do not create more drones for NPC factions if they already have too many
                                continue;
                            }
                        }

                        FInt metalCostPerItem = (FInt)mem.GetMetalCost();
                        FInt remainingMetalToConstructThisItem = metalCostPerItem - mem.MetalSpentConstructingCurrentReplacement;
                        FInt metalToSpendOnThisItem = Mat.Min( metalFlowRate, remainingMetalToConstructThisItem );

                        if ( metalToSpendOnThisItem > 0 )
                            mem.MetalSpentConstructingCurrentReplacement += metalToSpendOnThisItem;

                        if ( mem.MetalSpentConstructingCurrentReplacement >= metalCostPerItem )
                        {
                            mem.MetalSpentConstructingCurrentReplacement = FInt.Zero;
                            mem.AddOrSetNumberCreatedButNotDeployed( 1, false );
                        }

                    }
                }
            }
            //Engine_Universal.EndProfilerSample( "fleet-step" );
            #endregion
        }

        public void DoScenarioPerStepLogic( ArcenClientOrHostSimContextCore Context )
        {
            #region Scenario Per Step Logic
            IScenarioImplementation scenarioImp = World_AIW2.Instance.GetScenarioImplementationSafe_OrNull();
            if ( scenarioImp == null )
                return;

            //Engine_Universal.BeginProfilerSample( "scenario-step" );
            try
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    scenarioImp.DoPerSimStepLogic_OnMainThreadAndPartOfSim_HostOnly( Context.GetHostOnlyContext() );
                scenarioImp.DoPerSimStepLogic_OnMainThreadAndPartOfSim_ClientAndHost( Context );
            }
            catch( Exception e )
            {
                ScenarioData scenario = World_AIW2.Instance.GetScenarioSafe_OrNull();
                ArcenDebugging.ArcenDebugLogSingleLine( scenario?.InternalName + " Scenario Exception in scenario DoPerSimStepLogic_OnMainThreadAndPartOfSim: " + e, Verbosity.ShowAsError );
            }
            //Engine_Universal.EndProfilerSample( "scenario-step" );
            #endregion
        }

        private ThreadingExchanger isRunningFactionPerStepLogic = new ThreadingExchanger( "AllFactions-isRunningFactionPerStepLogic", 30f );

        public void DoFactionPerStepLogic( ArcenClientOrHostSimContextCore Context )
        {
            if ( isRunningFactionPerStepLogic.IsBusy() )
                return;
            if ( !isRunningFactionPerStepLogic.DoNextOnlyIfNotAlreadyBusy( string.Empty ) )
                return;
            try
            {
                if ( World.Instance.IsPaused )
                {
                    //run this logic while paused!  It's the external logic, it's different from stage0, 1, 2, etc below!
                    foreach ( Faction faction in World_AIW2.Instance.Factions )
                    {

                        faction.Safe_BaseInfo_DoPerSecondLogic_Stage1Clearing_OnMainThreadAndPartOfSim_ClientAndHost( Context );
                        faction.Safe_BaseInfo_DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( Context );
                        faction.Safe_BaseInfo_DoPerSecondLogic_Stage2APostAllFactionAggregating_OnMainThreadAndPartOfSim_ClientAndHost( Context );
                    }
                }

                #region Faction Per Step Logic
                //This is NOT the logic that is run per-second on BaseInfo and DeepInfo!
                //That's needlessly confusing!  I should have called these something else.

                //Engine_Universal.BeginProfilerSample( "faction-step" );
                foreach ( Faction faction in World_AIW2.Instance.Factions )
                {
                    if (  GameSettings.Current.GetBoolBySetting( "PerformanceTicksDebug" ) )
                        faction.stopwatch.Start();
                    faction.DoFactionStepLogic_Stage0_Singlethreaded();
                    if (  GameSettings.Current.GetBoolBySetting( "PerformanceTicksDebug" ) )
                    {
                        faction.Stage0Ticks = faction.stopwatch.ElapsedTicks;
                        faction.stopwatch.Reset();
                    }
                }
                foreach ( Faction faction in World_AIW2.Instance.Factions )
                {
                    if (  GameSettings.Current.GetBoolBySetting( "PerformanceTicksDebug" ) )
                        faction.stopwatch.Start();

                    faction.DoFactionStepLogic_Stage1_SingleThread( Context );
                    if (  GameSettings.Current.GetBoolBySetting( "PerformanceTicksDebug" ) )
                    {
                        faction.Stage1Ticks = faction.stopwatch.ElapsedTicks;
                        faction.stopwatch.Reset();
                    }
                }
                foreach ( Faction faction in World_AIW2.Instance.Factions )
                {
                    if (  GameSettings.Current.GetBoolBySetting( "PerformanceTicksDebug" ) )
                        faction.stopwatch.Start();

                    faction.DoFactionStepLogic_Stage2_SingleThread( Context );
                    if (  GameSettings.Current.GetBoolBySetting( "PerformanceTicksDebug" ) )
                    {
                        faction.Stage2Ticks = faction.stopwatch.ElapsedTicks;
                        faction.stopwatch.Reset();
                    }
                }

                foreach ( Faction faction in World_AIW2.Instance.Factions )
                {
                    if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    {
                        if (  GameSettings.Current.GetBoolBySetting( "PerformanceTicksDebug" ) )
                            faction.stopwatch.Start();

                        faction.Safe_DeepInfo_DoPerSimStepLogic_OnMainThreadAndPartOfSim_HostOnly( Context.GetHostOnlyContext() );
                        if (  GameSettings.Current.GetBoolBySetting( "PerformanceTicksDebug" ) )
                        {
                            faction.Stage3Ticks = faction.stopwatch.ElapsedTicks;
                            faction.stopwatch.Reset();
                        }

                    }
                    faction.Safe_BaseInfo_DoGeneralAggregationsPausedOrUnpaused_OnMainThread();
                    faction.Safe_BaseInfo_DoPerSimStepLogic_OnMainThreadAndPartOfSim_ClientAndHost( Context );
                }
                //Engine_Universal.EndProfilerSample( "faction-step" );
                #endregion

                isRunningFactionPerStepLogic.MarkAsNoLongerBusy();
            }
            catch ( ArcenPleaseStopThisThreadException )
            {
                isRunningFactionPerStepLogic.MarkAsNoLongerBusy();
            }
            catch ( Exception e )
            {
                isRunningFactionPerStepLogic.MarkAsNoLongerBusy();
                ArcenDebugging.ArcenDebugLogSingleLine( "DoFactionPerStepLogic outer error: " + e, Verbosity.ShowAsError );
            }
        }

        public void DoWorldPerStepLogic( ArcenClientOrHostSimContextCore Context )
        {
            bool isFirstSimStepOfSecond = World_AIW2.Instance.IsFirstFrameOfSecond;

            #region World Per Step Logic
            foreach ( ExternalWorldBaseInfoSource row in ExternalWorldBaseInfoSourceTable.Instance.Rows )
            {
                if ( row.Singleton.GetShouldIBeInUse() )
                {
                    row.Singleton.Safe_DoPerSimStepLogic_OnMainThreadAndPartOfSim_ClientAndHost( Context );
                    if ( isFirstSimStepOfSecond )
                        row.Singleton.Safe_DoPerSecondLogic_OnMainThreadAndPartOfSim_ClientAndHost( Context );
                }
            }

            ArcenHostOnlySimContext hostOnlyContext = Context.GetHostOnlyContext();
            if ( hostOnlyContext != null ) //never run these bits on clients!
            {
                foreach ( ExternalWorldDeepInfoSource row in ExternalWorldDeepInfoSourceTable.Instance.Rows )
                {
                    if ( row.Singleton.GetShouldIBeInUse() )
                    {
                        row.Singleton.Safe_DoPerSimStepLogic_OnMainThread_NonSim__HostOnly( hostOnlyContext );
                        if ( isFirstSimStepOfSecond )
                            row.Singleton.Safe_DoPerSecondLogic_OnMainThread_NonSim__HostOnly( hostOnlyContext );
                    }
                }
            }
            #endregion
        }

        public void DoPlanetPerStepLogicAsync()
        {
            #region countOfPlanetsLostByAI
            int countOfPlanetsLostByAI = 0;
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                //was originally owned by an AI
                if ( planet.InitialOwningAIFactionIndex >= 0 )
                {
                    Faction owner = planet.GetControllingFaction();
                    if ( owner == null || owner.Type != FactionType.AI )
                        countOfPlanetsLostByAI++;
                }
            }
            #endregion

            //Engine_Universal.BeginProfilerSample( "planet-step" );
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet == null )
                    continue;
                int debugStage = 0;
                try
                {
                    debugStage = 100;
                    planet.RecalculateOwnershipsIfBlank();
                    debugStage = 200;
                    planet.CheckForOwnershipChange();
                    debugStage = 300;
                    planet.CalculateAISentinelAlertStatusAndRelatedBits( countOfPlanetsLostByAI );
                    debugStage = 400;
                    if ( !planet.BattleStatus_ProcessThisSimStep )
                        continue;
                    debugStage = 500;
                    for ( int i = 0; i < planet.Factions.Count; i++ )
                        planet.Factions[i].DoPrePlanetFactionStepCleanup_Multithreaded();
                    debugStage = 600;
                    for ( int i = 0; i < planet.Factions.Count; i++ )
                        planet.Factions[i].DoPlanetFactionStepLogic_Multithreaded();
                    debugStage = 700;
                    for ( int i = 0; i < planet.Factions.Count; i++ )
                        planet.Factions[i].DoPostPlanetFactionStepCleanup_Multithreaded();
                    debugStage = 800;
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "DoPlanetPerStepLogic Error for planet " +
                        planet.Name + ", debugStage: " + debugStage + " Exception: " + e, Verbosity.ShowAsError );
                }
            }
        }

        private ThreadingExchanger isRunningCombatPerStepLogic = new ThreadingExchanger( "AllFactions-isRunningCombatPerStepLogic", 30f );

        public void DoCombatPerStepLogic( ArcenClientOrHostSimContextCore Context )
        {
            if ( World.Instance.IsPaused )
                return;

            if ( isRunningCombatPerStepLogic.IsBusy() )
                return;
            if ( !isRunningCombatPerStepLogic.DoNextOnlyIfNotAlreadyBusy( string.Empty ) )
                return;

            try
            {
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    this.DoCombatStepForPlanet( planet, Context );
                }
                isRunningCombatPerStepLogic.MarkAsNoLongerBusy();
            }
            catch ( ArcenPleaseStopThisThreadException )
            {
                isRunningCombatPerStepLogic.MarkAsNoLongerBusy();
            }
            catch ( Exception e )
            {
                isRunningCombatPerStepLogic.MarkAsNoLongerBusy();
                ArcenDebugging.ArcenDebugLogSingleLine( "DoCombatPerStepLogic outer error: " + e, Verbosity.ShowAsError );
            }
        }

        public void DoRemoveDeadEntitiesLogic( ArcenClientOrHostSimContextCore Context )
        {
            #region Remove dead entities
            World_AIW2.Instance.CheckForActuallyGettingRidOfRemovedEntities();
            #endregion
        }

        public void DoShipAILogic( ArcenClientOrHostSimContextCore Context )
        {
            #region Ship AI
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( !planet.BattleStatus_ProcessThisSimStep )
                    continue;
                
                foreach ( GameEntity_Squad e in planet.Squads() )
                {
                    if ( e == null )
                        continue;

                    this.ReevaluateUnitOrders( Context, e );
                }
            }
            #endregion
        }

        public void DoWorldSuffixLogic( ArcenClientOrHostSimContextCore Context )
        {
            int debugStage = 0;
            try
            {

                debugStage = 100;
                debugStage = 200;

                #region check for player victory
                if ( World.Instance.ConclusionType == CampaignConclusionType.NotConcluded && 
                     !World_AIW2.Instance.InSetupPhase && !Engine_AIW2.Instance.IsTestChamber &&
                     World_AIW2.Instance.TutorialOrNull == null && 
                     ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client &&
                     !World_AIW2.Instance.IsOutsideOfNormalGameplay && 
                     !World_AIW2.Instance.GetHasAnythingPreventingGameFromConcluding() )
                {
                    Faction factionBlockingVictory = FactionUtilityMethods.Instance.GetFactionBlockingVictoryOrNull();
                    if ( factionBlockingVictory == null && 
                         !World_AIW2.Instance.GetIsTutorial() )
                    {
                        debugStage = 600;

                        //Handle achievements
                        IScenarioImplementation scenarioImp = World_AIW2.Instance.GetScenarioImplementationSafe_OrNull();
                        if ( scenarioImp != null )
                            scenarioImp.DoOnPlayerVictory( Context.GetHostOnlyContext() );

                        debugStage = 700;
                        //note: CP_AIDef_Final should already handle this, now.
                        //World_AIW2.Instance.QueueChatMessageOrCommand( "You have won!", JournalEntryImportance.NeverDiscard, "" );
                        World_AIW2.Instance.DoConclusionOfGame( CampaignConclusionType.Won );
                    }
                }
                #endregion

                debugStage = 800;
                if ( !World_AIW2.Instance.IsOutsideOfNormalGameplay )
                {
                    //check for wormhole traversal
                    this.DoWormholeTraversalLogic( Context );

                    #region If The First Few Seconds
                    if ( World_AIW2.Instance.GameSecond <= 2)
                    {
                        foreach ( Faction fac in World_AIW2.Instance.AllPlayerFactions )
                        {
                            if ( fac == null )
                                continue;
                            PlayerTypeData pType = fac.PlayerTypeDataOrNull_ModeratelyExpensive;
                            if ( pType == null )
                                continue;
                            #region RevealsEntireMapAtStart
                            if ( pType.RevealsEntireMapAtStart )
                            {
                                World_AIW2.Instance.Debug_JustShowEverything = true;

                                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                                {
                                    planet.IntelLevel = PlanetIntelLevel.PermanentlyWatched;
                                    planet.GameSecondLastHadVisionBase = World_AIW2.Instance.GameSecond;
                                }
                            }
                            #endregion
                        }
                    }
                    #endregion
                }

                debugStage = 900;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in DoWorldSuffixLogic, debugStage " + debugStage + ", Exception: " + e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region DoPerStepLogic
        
        private void AddDroneGunShotToSim(DroneGunShot shot, ArcenHostOnlySimContext ctx)
        {
            if (ctx == null)
                return;
            var sys = shot.System;
            if (sys == null)
                return;
            var parent = sys.ParentEntity;
            if (parent == null)
                return;

            int debugStage = 0;
            
            ArcenCharacterBuffer tracingBuffer = null;
            if (Engine_AIW2.TraceAtAll && parent == GameEntity_Base.CurrentlyHoveredOver && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.IndividualLogic ))
                tracingBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "EntitySimLogicImpl-DoSystemStep-trace", 10f );

            try
            {
                ArcenPoint RandomPointInRadius(RandomGenerator rand, ArcenPoint P, int R)
                {
                    var vec = Vector2.zero;

                    for (int i = 0; i < 100; i++)
                    {
                        vec = new Vector2()
                        {
                            x = rand.NextFloat(R*2) - R,
                            y = rand.NextFloat(R*2) - R,
                        };

                        var len = vec.magnitude;
                        if (len > R)
                            continue;

                        vec.x += P.X;
                        vec.y += P.Y;

                        break;
                    }

                    return vec.ToArcenPoint();
                }

                debugStage = 100;
                var radius = parent.GetRadius();
                
                debugStage = 200;
                tracingBuffer?.Add( "\n\tdrone gun picking spawn point, parent radius is " + radius );
                radius = (int)((float)radius / 3.0f * 2.0f);
                if (radius < 10)
                    radius = 10;
                if (radius > 200)
                    radius = 200;

                debugStage = 300;
                var pos = RandomPointInRadius(ctx.RandomToUse, parent.WorldLocation, radius);
                
                debugStage = 400;
                GameEntity_Squad squad = SpawnSquadStyleShot_ReturnNullIfMPClient( shot.System, shot.Type, parent.WorldLocation, shot.Count, ctx );
                
                debugStage = 500;
                if (squad == null)
                    return;
                
                debugStage = 600;
                squad.SetWorldLocation( pos );

                debugStage = 700;
                if ( tracingBuffer != null ) tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "spawned actual squad style shot:" ).Add( squad.PrimaryKeyID ).Add( ":" ).Add( squad.TypeData.InternalName );

                debugStage = 800;
                squad.ParentGameEntity.SetInternalRef( parent );

                if (shot.PlaySound)
                {
                    //Chris says: we used to just directly call PlayJustFiredSoundIfIAmShotOutOfASystem().
                    //But now we set Network_PlayFiringSoundDuringFirstSimLoop, which will make sure it also happens on the client. 
                    squad.Network_PlayFiringSoundDuringFirstSimLoop = true;
                }

                //Chris says: it was possible that we would wind up processing the shot on the same frame, previously.
                //That might have been glitchy even in single player, but in MP it definitely would have caused missing animations and sounds on clients.
                //This is the safer way to be sure ships-as-shots don't act unpredictably.
                squad.Network_FrameToStartAnyProcessing = World_AIW2.Instance.Network_CurrentFrameNumber + 1;

                debugStage = 900;
                
                //put us in FRD mode
                squad.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );

                debugStage = 1000;

                GameEntity_Squad target = shot.Target.GetSquad();
                if (target != null)
                {
                    //give a direct attack order, but only IF it's actually something the spawned ship can shoot
                    bool foundValidSystem = false;
                    for ( int i = 0; i < squad.Systems.Count; i++ )
                    {
                        EntitySystem system = squad.Systems[i];
                        if (system == null)
                            continue;
                        
                        if ( system.TypeData.Category != EntitySystemCategory.Weapon )
                            continue;
                        
                        if ( !system.GetIsTargetValid( target ) )
                            continue;
                        
                        foundValidSystem = true;
                        break;
                    }
                    debugStage = 2000;

                    if ( foundValidSystem )
                    {
                        debugStage = 2100;
                        EntityOrder entityOrder = EntityOrder.Create_Attack( target.PrimaryKeyID, false, "FireShipFromSalvo", true, OrderSource.HumanPlayer, false ); //pretend it's the human player so it has more weight
                        squad.Orders.QueueOrder( squad, entityOrder );

                        debugStage = 2200;
                        for ( int i = 0; i < squad.Systems.Count; i++ )
                        {
                            EntitySystem system = squad.Systems[i];
                            
                            debugStage = 2210;
                            if ( system.TypeData.Category != EntitySystemCategory.Weapon )
                                continue;
                            
                            //set the new FRD target to be the target
                            debugStage = 2220;
                            system.CurrentFRDTarget = LazyLoadSquadWrapper.Create( target );
                            system.TimeFRDTargetExpires = 10; //stay for 10 seconds at least (as long as the target lives)
                            system.CurrentFRDPriority = 10000; //consider this super high priority
                        }
                    }
                }
                debugStage = 5000;

                //ArcenDebugging.ArcenDebugLog( "Fire shot: " + newSquadStyleShot.TypeData.DisplayName + " at " + target.TypeData.DisplayName, Verbosity.DoNotShow );
                //Note!  Apparently RemainingDelayUntilEntersSim breaks ships when it's used on them.
            }
            catch (Exception e)
            {
                ArcenDebugging.ArcenDebugLogNoDateOrAnything( string.Format( "exception adding drone gun shot to sim at debugstage={0}\n{1}\n", debugStage, e ), DebugLogDestination.ArcenDebugLog, Verbosity.ShowAsError );
            }

            if (tracingBuffer != null)
                ArcenDebugging.ArcenDebugLogNoDateOrAnything(tracingBuffer.ToStringAndReturnToPool()+"\n", DebugLogDestination.ArcenDebugLog, Verbosity.DoNotShow);
        }

        public override void DoPerStepLogic( FInt effectiveDeltaTime, GameEntity_Squad squad, ArcenClientOrHostSimContextCore context )
        {
            int debugstage = 0;
            try
            {
                debugstage = 10;
                if ( squad.ChargeAmounts != null )
                {
                    debugstage = 20;
                    if (squad.ChargeAmounts.Length < squad.TypeData.ChargeTypes.Count)
                    {
                        LOG.Err("Squad {0} has ChargeAmounts[{1}] which is less than TypeData.ChargeTypes[{2}]!", squad, squad.ChargeAmounts.Length, squad.TypeData.ChargeTypes.Count);
                    }
                    else
                    {
                        for ( int i = 0; i < squad.TypeData.ChargeTypes.Count; i++)
                        {
                            debugstage = 30;
                            var T = squad.TypeData.ChargeTypes[i];
                            
                            var C = squad.ChargeAmounts[i];

                            var before = C.Amount;

                            debugstage = 40;
                            if (T.ChangeAmount > 0 && T.ChangePerInterval > 0)
                            {
                                debugstage = 50;
                                C.TillPassive -= effectiveDeltaTime.IntValue;
                                while (C.TillPassive < 0)
                                {
                                    debugstage = 60;
                                    C.TillPassive += T.ChangePerInterval;
                                    C.Amount += T.ChangeAmount;
                                }
                            }

                            debugstage = 70;
                            if (C.Amount > T.NumMax)
                            {
                                C.Amount = T.NumMax;
                            }
                            if (C.Amount < 0)
                            {
                                C.Amount = 0;
                            }

                            //ArcenDebugging.ArcenDebugLogNoDateOrAnything(string.Format("Charge ticked {0} -> {1}. ChangeAmount {2} Interval {3} Max {4}", before, C.Amount, T.ChangeAmount, T.ChangePerInterval, T.NumMax), DebugLogDestination.ArcenDebugLog, Verbosity.DoNotShow );

                            debugstage = 80;
                            squad.ChargeAmounts[i] = C;
                        }
                    }
                }

                var hostCtx = context.GetHostOnlyContext();
                if (hostCtx != null)
                {
                    debugstage = 90;
                    for ( int i = 0; i < squad.PendingDroneGunShots.Count; i++)
                    {
                        debugstage = 100;
                        var shot = squad.PendingDroneGunShots[i];
                        shot.Delay -= effectiveDeltaTime;
                        if (shot.Delay > FInt.Zero)
                        {
                            debugstage = 110;
                            squad.PendingDroneGunShots[i] = shot;
                            continue;
                        }

                        debugstage = 120;
                        AddDroneGunShotToSim( shot, hostCtx );

                        debugstage = 130;
                        squad.PendingDroneGunShots.RemoveAt( i );
                        i--;
                    }
                }

                debugstage = 140;
            }
            catch (Exception e)
            {
                LOG.Err("exception at debugstage {0}\n{1}", debugstage, e);
            }
        }
        #endregion

        public static long PerSecondTicksLastFrame_SpeedGroups = 0;
        public static long PerSecondTicksLastFrame_FactionPerSecondBasics = 0;
        public static long PerSecondTicksLastFrame_PlanetInfluenceSort = 0;
        public static long PerSecondTicksLastFrame_Combat = 0;
        public static long PerSecondTicksLastFrame_Fleets = 0;
        public static long PerSecondTicksLastFrame_HackingDiff = 0;

        public static RefPair<long,long> PerFrameTicksLastFrame_GlobalPlayer = RefPair<long, long>.Create( 0, 0 );
        public static RefPair<long, long> PerFrameTicksLastFrame_WorldStep1 = RefPair<long, long>.Create( 0, 0 );
        public static RefPair<long, long> PerFrameTicksLastFrame_WorldSec = RefPair<long, long>.Create( 0, 0 );
        public static RefPair<long, long> PerFrameTicksLastFrame_Fleet = RefPair<long, long>.Create( 0, 0 );
        public static RefPair<long, long> PerFrameTicksLastFrame_Scenario = RefPair<long, long>.Create( 0, 0 );
        public static RefPair<long, long> PerFrameTicksLastFrame_Faction = RefPair<long, long>.Create( 0, 0 );
        public static RefPair<long, long> PerFrameTicksLastFrame_WorldStep2 = RefPair<long, long>.Create( 0, 0 );
        public static RefPair<long, long> PerFrameTicksLastFrame_Planet = RefPair<long, long>.Create( 0, 0 );
        public static RefPair<long, long> PerFrameTicksLastFrame_Combat = RefPair<long, long>.Create( 0, 0 );
        public static RefPair<long, long> PerFrameTicksLastFrame_RemoveDead = RefPair<long, long>.Create( 0, 0 );
        public static RefPair<long, long> PerFrameTicksLastFrame_ShipAILogic = RefPair<long, long>.Create( 0, 0 );
        public static RefPair<long, long> PerFrameTicksLastFrame_WorldSuffix = RefPair<long, long>.Create( 0, 0 );
        public static int PerFrameTicksLastFrame_Added = 0;
        
        private static void ResetCounter( RefPair<long, long> Count )
        {
            Count.LeftItem = Count.RightItem;
            Count.RightItem = 0;
        }

        #region DoWorldSecondLogic_FromSimBGThread
        public void DoWorldSecondLogic_FromSimBGThread( ArcenClientOrHostSimContextCore Context )
        {
            World_AIW2.Instance.GameSecond++;
            Stopwatch perSecondSW = new Stopwatch();
            perSecondSW.Start();

            //int secondsPerAIPIncrease = this.Setup.MinutesPerAIPIncrease * 60;
            //if ( secondsPerAIPIncrease > 0 && this.GameSecond % secondsPerAIPIncrease == 0 )
            //    this.ChangeAIP( (FInt)this.Setup.AIPPerAIPIncrease, AIPChangeReason.AutoIncrease, null, Context );

            bool simStageDebugLog = GameSettings_AIW2.Current.GetBool( ArcenBoolSetting_AIW2.PerSecondSimStageDebugLog );
            if ( simStageDebugLog )
                ArcenDebugging.ArcenDebugLogSingleLine( "START WorldSecondLogic", Verbosity.DoNotShow );

            //Go over all the ship groups to see if any need to be updated
            UpdateReactiveShipGroups( Context );

            //Engine_Universal.BeginProfilerSample( "faction-second" );
            foreach ( Faction faction in World_AIW2.Instance.Factions )
            {
                long startingTicks = perSecondSW.ElapsedTicks;
                if ( simStageDebugLog )
                    ArcenDebugging.ArcenDebugLogSingleLine( "START And BaseInfo Stage1 " + faction.GetDisplayName(), Verbosity.DoNotShow );
                faction.Safe_BaseInfo_DoPerSecondLogic_Stage1Clearing_OnMainThreadAndPartOfSim_ClientAndHost( Context );
                faction.PerSecondFactionTicksLastFrame_Stage1 = perSecondSW.ElapsedTicks - startingTicks;
                if ( simStageDebugLog )
                    ArcenDebugging.ArcenDebugLogSingleLine( "FINISH BaseInfo Stage1 " + faction.GetDisplayName(), Verbosity.DoNotShow );
            }
            foreach ( Faction faction in World_AIW2.Instance.Factions )
            {
                long startingTicks = perSecondSW.ElapsedTicks;
                if ( simStageDebugLog )
                    ArcenDebugging.ArcenDebugLogSingleLine( "START And BaseInfo Stage2 " + faction.GetDisplayName(), Verbosity.DoNotShow );
                faction.Safe_BaseInfo_DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( Context );
                faction.PerSecondFactionTicksLastFrame_Stage2 = perSecondSW.ElapsedTicks - startingTicks;
                if ( simStageDebugLog )
                    ArcenDebugging.ArcenDebugLogSingleLine( "FINISH BaseInfo Stage2 " + faction.GetDisplayName(), Verbosity.DoNotShow );
            }
            foreach ( Faction faction in World_AIW2.Instance.Factions )
            {
                long startingTicks = perSecondSW.ElapsedTicks;
                if ( simStageDebugLog )
                    ArcenDebugging.ArcenDebugLogSingleLine( "START And BaseInfo Stage2A " + faction.GetDisplayName(), Verbosity.DoNotShow );
                faction.Safe_BaseInfo_DoPerSecondLogic_Stage2APostAllFactionAggregating_OnMainThreadAndPartOfSim_ClientAndHost( Context );
                faction.PerSecondFactionTicksLastFrame_Stage2A = perSecondSW.ElapsedTicks - startingTicks;
                if ( simStageDebugLog )
                    ArcenDebugging.ArcenDebugLogSingleLine( "FINISH BaseInfo Stage2A " + faction.GetDisplayName(), Verbosity.DoNotShow );
            }

            foreach ( Faction faction in World_AIW2.Instance.Factions )
            {
                long startingTicks = perSecondSW.ElapsedTicks;
                if ( simStageDebugLog )
                    ArcenDebugging.ArcenDebugLogSingleLine( "START And BaseInfo UpdatePowerLevel " + faction.GetDisplayName(), Verbosity.DoNotShow );
                try
                {
                    if ( faction.BaseInfo != null )
                        faction.BaseInfo.UpdatePowerLevel();
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "UpdatePowerLevel error in faction: " + faction.GetDisplayName() + "\n" + e, Verbosity.DoNotShow );
                }
                if ( simStageDebugLog )
                    ArcenDebugging.ArcenDebugLogSingleLine( "FINISH BaseInfo UpdatePowerLevel " + faction.GetDisplayName(), Verbosity.DoNotShow );
            }

            {
                long startingTicks = perSecondSW.ElapsedTicks;
                foreach ( Faction faction in World_AIW2.Instance.Factions )
                {
                    if ( simStageDebugLog )
                        ArcenDebugging.ArcenDebugLogSingleLine( "START SpeedGroup" + faction.GetDisplayName(), Verbosity.DoNotShow );
                    faction.DoSpeedGroupLogic_HostOnly();
                    if ( simStageDebugLog )
                        ArcenDebugging.ArcenDebugLogSingleLine( "FINISH SpeedGroup" + faction.GetDisplayName(), Verbosity.DoNotShow );
                }
                PerSecondTicksLastFrame_SpeedGroups = perSecondSW.ElapsedTicks - startingTicks;
            }

            {
                long startingTicks = perSecondSW.ElapsedTicks;
                foreach ( Faction faction in World_AIW2.Instance.Factions )
                {
                    if ( simStageDebugLog )
                        ArcenDebugging.ArcenDebugLogSingleLine( "START PerSecond And DeepInfo bits" + faction.GetDisplayName(), Verbosity.DoNotShow );

                    faction.DoPerSecondSimUpdate( Context );

                    try
                    {
                        if ( faction.DeepInfo != null )
                            faction.DeepInfo.CheckIfPlayerHasSeenFaction_HostOnly( Context.GetHostOnlyContext() );
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "CheckIfPlayerHasSeenFaction_HostOnly error in faction: " + faction.GetDisplayName() + "\n" + e, Verbosity.DoNotShow );
                    }
                    try
                    {
                        if ( faction.DeepInfo != null )
                            faction.DeepInfo.UpdatePlanetInfluence_HostOnly( Context.GetHostOnlyContext() );
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "UpdatePlanetInfluence_HostOnly error in faction: " + faction.GetDisplayName() + "\n" + e, Verbosity.DoNotShow );
                    }
                    try
                    {
                        if ( faction.DeepInfo != null )
                            faction.DeepInfo.UpdateInvasionTime_HostOnly( Context.GetHostOnlyContext() );
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "UpdateInvasionTime_HostOnly error in faction: " + faction.GetDisplayName() + "\n" + e, Verbosity.DoNotShow );
                    }

                    if ( simStageDebugLog )
                        ArcenDebugging.ArcenDebugLogSingleLine( "FINISH PerSecond And DeepInfo bits" + faction.GetDisplayName(), Verbosity.DoNotShow );
                }
                PerSecondTicksLastFrame_FactionPerSecondBasics = perSecondSW.ElapsedTicks - startingTicks;
            }

            if ( simStageDebugLog )
                ArcenDebugging.ArcenDebugLogSingleLine( "START Planet Influence Calculations", Verbosity.DoNotShow );

            {
                long startingTicks = perSecondSW.ElapsedTicks;

                ArcenThreading.RunTaskOnBackgroundThread( "_PSec.PlanetInfluence", false, false, delegate  //a sort in here?  Heck yeah we want these on a background non-blocking thread
                {
                    //Now that we've updated all the planetary influences, sort them from "strongest force on planet"
                    //to "weakest force
                    foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                    {
                        if ( planet.UnderInfluenceOfFactionIndex.Count == 0 )
                        {
                            planet.PrimaryInfluencingFaction = -1;
                            continue;
                        }
                        // Pre-compute strengths once to avoid repeated faction lookups inside the sort comparator
                        WorkingStrengthByFactionIndex.Clear();
                        for ( int si = 0; si < planet.UnderInfluenceOfFactionIndex.Count; si++ )
                        {
                            short fIdx = planet.UnderInfluenceOfFactionIndex[si];
                            Faction f = World_AIW2.Instance.GetFactionByIndex( fIdx );
                            WorkingStrengthByFactionIndex[fIdx] = planet.GetPlanetFactionForFaction( f ).DataByStance[FactionStance.Self].TotalStrength;
                        }
                        planet.UnderInfluenceOfFactionIndex.Sort( static delegate ( short L, short R )
                        {
                            return WorkingStrengthByFactionIndex[R].CompareTo( WorkingStrengthByFactionIndex[L] );
                        } );
                        planet.PrimaryInfluencingFaction = planet.UnderInfluenceOfFactionIndex[0];
                    }
                } );

                PerSecondTicksLastFrame_PlanetInfluenceSort = perSecondSW.ElapsedTicks - startingTicks;
            }

            if ( simStageDebugLog )
                ArcenDebugging.ArcenDebugLogSingleLine( "FINISH Planet Influence Calculations", Verbosity.DoNotShow );

            foreach ( Faction faction in World_AIW2.Instance.Factions )
            {
                long startingTicks = perSecondSW.ElapsedTicks;
                if ( simStageDebugLog )
                    ArcenDebugging.ArcenDebugLogSingleLine( "START And BaseInfo Stage3 " + faction.GetDisplayName(), Verbosity.DoNotShow );
                faction.Safe_BaseInfo_DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost( Context );
                faction.PerSecondFactionTicksLastFrame_Stage3Base = perSecondSW.ElapsedTicks - startingTicks;
                if ( simStageDebugLog )
                    ArcenDebugging.ArcenDebugLogSingleLine( "FINISH BaseInfo Stage3 " + faction.GetDisplayName(), Verbosity.DoNotShow );
            }

            foreach ( Faction faction in World_AIW2.Instance.Factions )
            {
                long startingTicks = perSecondSW.ElapsedTicks;
                if ( simStageDebugLog )
                    ArcenDebugging.ArcenDebugLogSingleLine( "START Stage3 " + faction.GetDisplayName(), Verbosity.DoNotShow );
                if ( faction.SpecialFactionData.TakesOnCurrentGeneralMarkLevelOfParentFaction )
                {
                    Faction parent = faction.GetParentFactionOrNull();
                    if ( parent != null )
                        faction.CurrentGeneralMarkLevel_Base = parent.CurrentGeneralMarkLevel_Base;
                }


                faction.Safe_DeepInfo_DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( Context.GetHostOnlyContext() );
                FactionPerSecondBonusGeneralMarkLevelsCheck( faction, Context );
                if ( simStageDebugLog )
                    ArcenDebugging.ArcenDebugLogSingleLine( "FINISH Stage3 " + faction.GetDisplayName(), Verbosity.DoNotShow );
                faction.PerSecondFactionTicksLastFrame_Stage3Deep = perSecondSW.ElapsedTicks - startingTicks;
            }

            foreach ( Faction faction in World_AIW2.Instance.Factions )
            {
                long startingTicks = perSecondSW.ElapsedTicks;
                if ( simStageDebugLog )
                    ArcenDebugging.ArcenDebugLogSingleLine( "START Stage4 " + faction.GetDisplayName(), Verbosity.DoNotShow );
                faction.Safe_BaseInfo_DoPerSecondLogic_Stage4AdvancedAllegianceCode_OnMainThreadAndPartOfSim_ClientAndHost( Context );
                FactionPerSecondBonusGeneralMarkLevelsCheck( faction, Context );
                if ( simStageDebugLog )
                    ArcenDebugging.ArcenDebugLogSingleLine( "FINISH Stage4 " + faction.GetDisplayName(), Verbosity.DoNotShow );
                faction.PerSecondFactionTicksLastFrame_Stage4 = perSecondSW.ElapsedTicks - startingTicks;
            }

            if ( simStageDebugLog )
                ArcenDebugging.ArcenDebugLogSingleLine( "START Planet View Calculations", Verbosity.DoNotShow );

            int localPlanetID = PlayerAccount_AIW2.GetViewingPlanetIndexSafe();

            {
                long startingTicks = perSecondSW.ElapsedTicks;

                //Engine_Universal.BeginProfilerSample( "planet-second" );
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    this.DoCombatSecond_FromSimBGThread( planet, Context );

                    //fix old savegames if they are off
                    if ( planet.Index == localPlanetID && planet.ViewedByPlayerAccounts_DuringGame.Count == 0 )
                    {
                        GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetPlanetViewedByPlayerAccount], GameCommandSource.AnythingElse );
                        command.RelatedIntegers.Add( PlayerAccount.Local.PlayerPrimaryKeyID );
                        command.RelatedIntegers2.Add( planet.Index );
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, false );
                    }
                }

                PerSecondTicksLastFrame_Combat = perSecondSW.ElapsedTicks - startingTicks;
            }

            //Asynchronously run logic for all of the units in the game
            ArcenThreading.RunTaskOnBackgroundThread( "_PSec.EntitySecondLogic", false, false, delegate
            {
                AIWar2NetworkSync.SquadValue Temp = new AIWar2NetworkSync.SquadValue();
                
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    Int16 reinforcementLocationCount = 0;
                    foreach ( GameEntity_Squad entity in planet.Squads() )
                    {
                        entity.DoEntitySecondLogic_FromAsync( Context );
                        
                        if (ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client)
                        {
                            if (entity.Network_ClientOnly_SimCyclesSinceLastHostUpdate > Temp.Value)
                            {
                                Temp.Value = entity.Network_ClientOnly_SimCyclesSinceLastHostUpdate;
                                Temp.Squad = entity;
                            }
                        }
                        else
                        {
                            if (entity.Network_ServerOnly_GameSecondLastFullySyncedNonSim > Temp.Value)
                            {
                                Temp.Value = entity.Network_ServerOnly_GameSecondLastFullySyncedNonSim;
                                Temp.Squad = entity;
                            }
                        }
                        
                        if ( entity.TypeData.IsReinforcementLocation )
                            reinforcementLocationCount++;

                        if (entity.TypeData.SpecialPerSecondLogic != null)
                            entity.TypeData.SpecialPerSecondLogic.RunEntitySpecialPerSecondLogic(entity, Context);

                    }

                    if ( reinforcementLocationCount > planet.MaxReinforcementPlacesEverSeenHere )
                        planet.MaxReinforcementPlacesEverSeenHere = reinforcementLocationCount;
                }
                
                if (ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client)
                {
                    AIWar2NetworkSync.OnClient_SquadNotSyncedForLongestTime_Cycles = Temp;
                }
                else
                {
                    AIWar2NetworkSync.OnHost_SquadNotSyncedForLongestTime_GameSecond = Temp;   
                }
            } );

            //Asynchronously run deployment logic for all of the units in the game
            ArcenThreading.RunTaskOnBackgroundThread( "_PSec.Planets.DoCombatSecond", false, false, delegate
            {
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    for ( int i = 0; i < planet.Factions.Count; i++ )
                        planet.Factions[i].DoCombatSecond_Async( Context );

                    this.CheckForInternalShipDeployment_DroneProducers_FromSimBGThread( planet, Context.GetHostOnlyContext() );
                }
            } );

            if ( simStageDebugLog )
                ArcenDebugging.ArcenDebugLogSingleLine( "FINISH Planet View Calculations", Verbosity.DoNotShow );

            if ( simStageDebugLog )
                ArcenDebugging.ArcenDebugLogSingleLine( "START Fleet PerSecond Calculations", Verbosity.DoNotShow );

            {
                long startingTicks = perSecondSW.ElapsedTicks;

                //Engine_Universal.EndProfilerSample( "planet-second" );
                for ( int i = 0; i < World_AIW2.Instance.AllFleets.Count; i++ )
                {
                    Fleet fleet = World_AIW2.Instance.AllFleets[i];
                    if ( fleet == null )
                        continue;
                    FleetSimLogic.Instance.PerSecond_UpdateFleetData( fleet, Context );
                }

                PerSecondTicksLastFrame_Fleets = perSecondSW.ElapsedTicks - startingTicks;
            }

            if ( simStageDebugLog )
                ArcenDebugging.ArcenDebugLogSingleLine( "FINISH Fleet PerSecond Calculations", Verbosity.DoNotShow );

            if ( simStageDebugLog )
                ArcenDebugging.ArcenDebugLogSingleLine( "START Hacking Difficulty Estimate Calculations", Verbosity.DoNotShow );

            {
                long startingTicks = perSecondSW.ElapsedTicks;

                //Asynchronously run deployment logic for all of the units in the game
                ArcenThreading.RunTaskOnBackgroundThread( "_PSec.HackingDifficultyEstimates", false, false, delegate
                {
                    foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                    {
                        if ( Window_InGameSidebarBase.Current == InGameSidebarType.Hacking &&
                             HackingTypeTable.Instance.DifficultyEstimationDone )
                            break; //don't run this if the hacking menu is open, it makes the colour flicker
                        if ( !HackingTypeTable.Instance.DifficultyEstimationDone ||
                            (HackingTypeTable.Instance.DifficultyEstimationDone && World_AIW2.Instance.GameSecond % 5 == 0) )
                        {
                            //Badger isn't sure if this is the optimal place for this. It should be done once at game load
                            //time, and then sporadically at other times
                            HackingTypeTable.Instance.UpdateDifficultyEstimates();
                        }
                    }
                } );

                PerSecondTicksLastFrame_HackingDiff = perSecondSW.ElapsedTicks - startingTicks;
            }


            if ( simStageDebugLog )
                ArcenDebugging.ArcenDebugLogSingleLine( "FINISH Hacking Difficulty Estimate Calculations", Verbosity.DoNotShow );

            if ( simStageDebugLog )
                ArcenDebugging.ArcenDebugLogSingleLine( "FINISH WorldSecondLogic", Verbosity.DoNotShow );

            perSecondSW.Stop();
        }
        #endregion

        private static List<AIShipGroup> WorkingShipGroups = List<AIShipGroup>.Create_WillNeverBeGCed( 40, "EntitySimLogicImplementation_BaseInfo-WorkingShipGroups" );
        private static Dictionary<short, int> WorkingStrengthByFactionIndex = Dictionary<short, int>.Create_WillNeverBeGCed( 10, "EntitySimLogicImplementation_BaseInfo-WorkingStrengthByFactionIndex" );
        
        private void UpdateReactiveShipGroups( ArcenClientOrHostSimContextCore Context )
        {
            bool debug = false;
            bool verboseDebug = false;
            int interval = 345;

            if ( World_AIW2.Instance.GameSecond != 5 && World_AIW2.Instance.GameSecond % interval != 0 )
                return;
            
            if ( debug ) LOG.Msg("updating reactive ship groups");

            int highestAIDifficulty = FactionUtilityMethods.Instance.GetHighestAIDifficulty();

            //First, reset all the reactive ship groups
            AIShipGroupTable.Instance.ResetAllReactiveShipGroups();

            if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "Venators" ) )
            {
                //Note: I'm a bit fuzzy on exactly what's being updated, since the ShipGroup being used for BasicGuardians on a planet for one AI
                //might be the wave guardians for another AI.
                //So the MinAI will be "If any AI is > Min, this trigger applies"
                //Also, we need to track "Has this trigger already been applied?"
                //We can do this by looking at the related string and related integer
                for ( int i = 0; i < AIShipGroupTable.Instance.Rows.Count; i++ )
                {
                    AIShipGroup shipGroup = AIShipGroupTable.Instance.Rows[i];
                    if ( shipGroup.TriggerConditions.Count == 0 )
                        continue;
                    
                    WorkingShipGroups.Clear();
                    
                    int maxAvailablePercent = 0;
                    for ( int j = 0; j < shipGroup.TriggerConditions.Count; j++ )
                    {
                        ReactiveShipGroupTrigger trigger = shipGroup.TriggerConditions[j];
                        
                        if ( debug ) LOG.Msg("Checking {0} with trigger <{1}>", shipGroup.InternalName, trigger.ToString() );
                        
                        if ( trigger.IsActive() )
                        {
                            if ( trigger.MustHaveAIAtDifficulty > highestAIDifficulty )
                            {
                                if ( debug ) LOG.Msg("\tTrigger deactivated due to ai difficulty; highest diff: {0} and required {1}", highestAIDifficulty, trigger.MustHaveAIAtDifficulty);
                                
                                continue;
                            }
                            
                            if ( debug ) LOG.Msg("\tThis ship group is active, since we met the criteria and AI with difficulty {0}", highestAIDifficulty );

                            if ( trigger.PercentOfShipGroups > maxAvailablePercent )
                                maxAvailablePercent = trigger.PercentOfShipGroups;

                            //Iterate over all the planets and decide whether to update their reactive draw bags
                            //I tried using DFPlanetsParallel but I saw some odd behaviours, like debug statements not being printed when I expected,
                            //so single thread is safer
                            int planetsConsidered = 0;
                            //int planetsDiscardedDueToRandomness = 0;
                            //int planetsUpdated = 0;
                            //World_AIW2.Instance.DFPlanetsParallel( false, delegate ( Planet planet )
                            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                            {
                                planetsConsidered++;
                                
                                Faction faction = planet.GetControllingFaction();
                                if ( faction == null || faction.Type != FactionType.AI )
                                    continue;

                                PlanetFaction pFaction = planet.GetControllingPlanetFaction();
                                if ( pFaction == null )
                                    continue;
                                
                                //We can update the Dire Guardians, Guardians or strikecraft
                                AIShipGroup groupToConsider = null;
                                if ( trigger.UpdateStrikecraft )
                                    groupToConsider = pFaction.ShipGroup_Strikecraft;
                                else if ( trigger.UpdateGuardians )
                                    groupToConsider = pFaction.ShipGroup_BasicGuardians;
                                else
                                    groupToConsider = pFaction.ShipGroup_DireGuardians;
                                
                                if ( groupToConsider != null )
                                {
                                    if ( !WorkingShipGroups.Contains( groupToConsider ) )
                                    {
                                        if ( verboseDebug ) LOG.Msg("Adding {0} to the working ship groups", groupToConsider.InternalName );

                                        WorkingShipGroups.Add(groupToConsider);
                                    }
                                }
                            }
                        }
                    }
                    
                    for ( int j = 0; j < WorkingShipGroups.Count; j++ )
                    {
                        AIShipGroup groupForReactiveUpdate = WorkingShipGroups[j];
                        AIShipGroupTable.EnabledReactiveShipGroups.Add(WorkingShipGroups[j]);
                        
                        //we always add the first one, since sometimes there are no other choices (can be the case for Dires)
                        int random = Context.RandomToUse.Next(0, 100);
                        if ( j == 0 || random < maxAvailablePercent )
                        {
                            if ( verboseDebug ) LOG.Msg("Adding the ships from {0} to {1} path", shipGroup.InternalName, groupForReactiveUpdate.InternalName);
                            
                            groupForReactiveUpdate.ReactiveDrawBag.AddAllFromOtherToThis( shipGroup.DrawBag );
                        }
                    }
                }
            }

            // Chance for external code to add more ships to reactive ship groups.
            ArcenExternalCodeHook.Invoke( "OnUpdateReactiveShipGroups", null, null, null, Context );

            //now that we've updated all the reactive draw bags, recalculate the draw bag to combine base + reactive
            AIShipGroupTable.Instance.UpdateDrawbagsWithReactiveElements();
        }

        private static void FactionPerSecondBonusGeneralMarkLevelsCheck( Faction Fac, ArcenClientOrHostSimContextCore Context )
        {
            if ( World_AIW2.Instance.GetIsTutorial() )
                return; //don't add any mark levels to ships in the tutorial.

            byte addedLevels = 0;
            if ( Fac.SpecialFactionData.GetsPraetorianBonusToMarkLevelFromAIFactionDifficulty )
            {
                Faction relatedAIFaction = Fac.GetParentFactionOrNull();
                if ( relatedAIFaction != null )
                {
                    AISentinelsCoreData sentinelsExt = relatedAIFaction.TryGetAISentinelsCoreData()?.SentinelInfo;
                    if ( sentinelsExt != null )
                    {
                        AIDifficulty difficulty = sentinelsExt.AIDifficulty;
                        if ( difficulty != null )
                        {
                            addedLevels = difficulty.AddedPraetorianMarkLevelsAboveAmbient;
                        }
                    }
                }
            }
            Fac.CurrentGeneralMarkLevel_Added = addedLevels;
        }
    }
}
