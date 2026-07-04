using Arcen.Universal;
using System;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public sealed class MetalFlowPlanning_Player : ArcenShortTermPlanningContext, IBetweenMapGenPoolable<MetalFlowPlanning_Player>
    {
        public static int LastCountOfRepairingHullsOfFriendlies = 0;
        public static int LastCountOfRepairingShieldsOfFriendlies = 0;
        public static int LastCountOfRepairingEnginesOfFriendlies = 0;
        public static int LastCountOfSelfAssistConstructions = 0;
        public static int LastCountOfFactoryAssistConstructions = 0;

        public static int Accumulator_MFB = 0;
        public static int Accumulator_MFF = 0;

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            Accumulator_MFB = 0;
            Accumulator_MFF = 0;
        }

        private static readonly BetweenMapGenPool<MetalFlowPlanning_Player> Pool = BetweenMapGenPool<MetalFlowPlanning_Player>.Create_WillNeverBeGCed( "MetalFlowPlanning_Player", 30, 10,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new MetalFlowPlanning_Player(); } );

        public static MetalFlowPlanning_Player GetFromPoolOrCreate()
        {
            MetalFlowPlanning_Player context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion

        private readonly MetalFlowPlanningWorker worker;

        private static ReferenceTracker RefTracker;
        private MetalFlowPlanning_Player()
            : base( "_ST.MetalFlowPlanning_Player" )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "MetalFlowPlanning_Player" );
            RefTracker.IncrementObjectCount();

            worker = new MetalFlowPlanningWorker( false );
        }


        private static ThreadingExchanger IsCurrentlyWaitingOnThread = new ThreadingExchanger( "MetalFlowPlanning_Player", 30f );

        public override void Execute()
        {
            if ( CentralVars.DEBUG_TURN_OFF_SHORT_TERM_PLANNNING_ALL )
                return;
            if ( CentralVars.DEBUG_TURN_OFF_METAL_FLOW_PLAN )
                return;

            if ( IsCurrentlyWaitingOnThread.IsBusy() )
            {
                //ArcenDebugging.ArcenDebugLogSingleLine( "Tried to MetalFlowPlanning.Execute, but it was already running!", Verbosity.ShowAsError );
                return;
            }
            if ( !IsCurrentlyWaitingOnThread.DoNextOnlyIfNotAlreadyBusy( string.Empty ) )
                return;
            try
            {
                Interlocked.Increment( ref Accumulator_MFB );

                worker.InnerExecuteClear();

                worker.WorkingCountOfRepairingHullsOfFriendlies = 0;
                worker.WorkingCountOfRepairingShieldsOfFriendlies = 0;
                worker.WorkingCountOfRepairingEnginesOfFriendlies = 0;
                worker.WorkingCountOfSelfAssistConstructions = 0;
                worker.WorkingCountOfFactoryAssistConstructions = 0;

                #region Factories Early
                //first clear CalcOnly_DoNotUse_FactoriesWorking
                foreach ( Fleet fleet in World_AIW2.Instance.Fleets( FleetStatus.AnyStatus ) )
                {
                    fleet.SupportingFactoriesInRange.ClearConstructionListForStartingConstruction();
                }
                #endregion

                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    List<PlanetFaction> pFactions = planet.Factions;
                    for ( int j = 0; j < pFactions.Count; j++ )
                    {
                        PlanetFaction pFac = pFactions[j];
                        if ( pFac.Faction.Type != FactionType.Player )
                            continue;

                        worker.PreCalculateFactoryTargetsForPlanetFactionAndAnySpecialFactoryTypes( pFac );

                        worker.help_DoPerPlanetFactionLogic( pFac, pFac.Faction.PlayerTypeDataOrNull_ModeratelyExpensive );
                    }
                }

                #region Factories Late
                foreach ( Fleet fleet in World_AIW2.Instance.Fleets( FleetStatus.AnyStatus ) )
                {
                    fleet.SupportingFactoriesInRange.SwitchConstructionToDisplay();
                    fleet.HasSupportingFactoriesInRangeEverBeenSet = true;
                }
                #endregion

                LastCountOfRepairingHullsOfFriendlies = worker.WorkingCountOfRepairingHullsOfFriendlies;
                LastCountOfRepairingShieldsOfFriendlies = worker.WorkingCountOfRepairingShieldsOfFriendlies;
                LastCountOfRepairingEnginesOfFriendlies = worker.WorkingCountOfRepairingEnginesOfFriendlies;
                LastCountOfSelfAssistConstructions = worker.WorkingCountOfSelfAssistConstructions;
                LastCountOfFactoryAssistConstructions = worker.WorkingCountOfFactoryAssistConstructions;

                worker.InnerExecutePostLogic();

                Interlocked.Increment( ref Accumulator_MFF );
                IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
            }
            catch ( Exception e )
            {
                IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
                ArcenDebugging.ArcenDebugLog( "Exception in MetalFlowPlanning_Player.Execute:" + e.ToString(), Verbosity.ShowAsError );
            }
        }
    }

    public sealed class MetalFlowPlanning_NPC : ArcenShortTermPlanningContext, IBetweenMapGenPoolable<MetalFlowPlanning_NPC>
    {
        public static int LastCountOfRepairingHullsOfFriendlies = 0;
        public static int LastCountOfRepairingShieldsOfFriendlies = 0;
        public static int LastCountOfRepairingEnginesOfFriendlies = 0;
        public static int LastCountOfSelfAssistConstructions = 0;
        public static int LastCountOfFactoryAssistConstructions = 0;

        public static int Accumulator_MFB = 0;
        public static int Accumulator_MFF = 0;

        private readonly MetalFlowPlanningWorker worker;

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            Accumulator_MFB = 0;
            Accumulator_MFF = 0;
        }

        private static readonly BetweenMapGenPool<MetalFlowPlanning_NPC> Pool = BetweenMapGenPool<MetalFlowPlanning_NPC>.Create_WillNeverBeGCed( "MetalFlowPlanning_NPC", 30, 10,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new MetalFlowPlanning_NPC(); } );

        public static MetalFlowPlanning_NPC GetFromPoolOrCreate()
        {
            MetalFlowPlanning_NPC context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion

        private static ReferenceTracker RefTracker;
        private MetalFlowPlanning_NPC()
            : base( "_ST.MetalFlowPlanning_NPC" )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "MetalFlowPlanning_NPC" );
            RefTracker.IncrementObjectCount();

            worker = new MetalFlowPlanningWorker( true );
        }

        private static ThreadingExchanger IsCurrentlyWaitingOnThread = new ThreadingExchanger( "MetalFlowPlanning_NPC", 30f );

        public override void Execute()
        {
            if ( CentralVars.DEBUG_TURN_OFF_SHORT_TERM_PLANNNING_ALL )
                return;
            if ( CentralVars.DEBUG_TURN_OFF_METAL_FLOW_PLAN )
                return;

            if ( IsCurrentlyWaitingOnThread.IsBusy() )
            {
                //ArcenDebugging.ArcenDebugLogSingleLine( "Tried to MetalFlowPlanning.Execute, but it was already running!", Verbosity.ShowAsError );
                return;
            }
            if ( !IsCurrentlyWaitingOnThread.DoNextOnlyIfNotAlreadyBusy( string.Empty ) )
                return;

            try
            {
                Interlocked.Increment( ref Accumulator_MFB );

                //ArcenDebugging.ArcenDebugLogSingleLine( "metalFlows RUN!", Verbosity.DoNotShow );  //PAUSE_3_TODO

                worker.InnerExecuteClear();

                worker.WorkingCountOfRepairingHullsOfFriendlies = 0;
                worker.WorkingCountOfRepairingShieldsOfFriendlies = 0;
                worker.WorkingCountOfRepairingEnginesOfFriendlies = 0;
                worker.WorkingCountOfSelfAssistConstructions = 0;
                worker.WorkingCountOfFactoryAssistConstructions = 0;

                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    List<PlanetFaction> pFactions = planet.Factions;
                    for ( int j = 0; j < pFactions.Count; j++ )
                    {
                        PlanetFaction pFac = pFactions[j];
                        if ( pFac.Faction.Type == FactionType.Player )
                            continue;
                        worker.help_DoPerPlanetFactionLogic( pFac, null );
                    }
                }

                LastCountOfRepairingHullsOfFriendlies = worker.WorkingCountOfRepairingHullsOfFriendlies;
                LastCountOfRepairingShieldsOfFriendlies = worker.WorkingCountOfRepairingShieldsOfFriendlies;
                LastCountOfRepairingEnginesOfFriendlies = worker.WorkingCountOfRepairingEnginesOfFriendlies;
                LastCountOfSelfAssistConstructions = worker.WorkingCountOfSelfAssistConstructions;
                LastCountOfFactoryAssistConstructions = worker.WorkingCountOfFactoryAssistConstructions;

                worker.InnerExecutePostLogic();

                Interlocked.Increment( ref Accumulator_MFF );
                IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
            }
            catch ( Exception e )
            {
                IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
                ArcenDebugging.ArcenDebugLog( "Exception in MetalFlowPlanning_NPC.Execute:" + e.ToString(), Verbosity.ShowAsError );
            }
        }
    }

    public class MetalFlowPlanningWorker
    {
        public int WorkingCountOfRepairingHullsOfFriendlies = 0;
        public int WorkingCountOfRepairingShieldsOfFriendlies = 0;
        public int WorkingCountOfRepairingEnginesOfFriendlies = 0;
        public int WorkingCountOfSelfAssistConstructions = 0;
        public int WorkingCountOfFactoryAssistConstructions = 0;

        private readonly bool IsForNPCsOnly;

        public MetalFlowPlanningWorker( bool IsForNPCsOnly )
        {
            this.IsForNPCsOnly = IsForNPCsOnly;
        }

        public void InnerExecuteClear()
        {
            foreach ( GameEntity_Squad _mfp_e in World_AIW2.Instance.Squads() )
                help_ZeroOutputVariables( _mfp_e );

            foreach ( Faction faction in World_AIW2.Instance.Factions )
                if ( help_DoPerFactionLogic_PrePerFaction( faction ) == DelReturn.Break )
                    break;

            //clear stuff to start out
            lastPreCalculateClaimableTargetsForPlanetFaction_PFaction = null;
            lastPreCalculateAllRepairableTargetsForPlanetFaction_PFaction = null;
            lastPreCalculateRemainsRebuildTargetsForPlanetFaction_PFaction = null;
            lastPreCalculateSelfAssistConstTargetsForPlanetFaction_PFaction = null;
            lastPreCalculateFactoryAssistConstTargetsForPlanetFaction_PFaction = null;
        }

        #region help_ZeroOutputVariables
        private DelReturn help_ZeroOutputVariables( GameEntity_Squad entity )
        {
            Faction faction = entity.GetFactionOrNull_Safe();
            if ( faction == null )
                return DelReturn.Continue;
            if ( faction.Type == FactionType.NaturalObject )
                return DelReturn.Continue;
            if ( this.IsForNPCsOnly )
            {
                if ( faction.Type == FactionType.Player )
                    return DelReturn.Continue;
            }
            else //for players
            {
                if ( faction.Type != FactionType.Player )
                    return DelReturn.Continue;
            }

            entity.FramePlan_ExpectSquadReplacements = 0;
            entity.FramePlan_ExpectedRepairs = FInt.Zero;
            return DelReturn.Continue;
        }
        #endregion

        #region help_DoPerFactionLogic_PrePerFaction
        private DelReturn help_DoPerFactionLogic_PrePerFaction( Faction faction )
        {
            if ( faction.Type == FactionType.NaturalObject )
                return DelReturn.Continue;
            if ( this.IsForNPCsOnly )
            {
                if ( faction.Type == FactionType.Player )
                    return DelReturn.Continue;
            }
            else //for players
            {
                if ( faction.Type != FactionType.Player )
                    return DelReturn.Continue;
            }
            faction.FactionPlannedFlows.ClearConstructionListForStartingConstruction();
            return DelReturn.Continue;
        }
        #endregion

        public void InnerExecutePostLogic()
        {
            foreach ( Faction faction in World_AIW2.Instance.Factions )
                if ( help_DoPerFactionLogic_PostPerFaction( faction ) == DelReturn.Break )
                    break;
        }

        #region help_DoPerFactionLogic_PostPerFaction
        private DelReturn help_DoPerFactionLogic_PostPerFaction( Faction faction )
        {
            if ( faction.Type == FactionType.NaturalObject )
                return DelReturn.Continue;
            if ( this.IsForNPCsOnly )
            {
                if ( faction.Type == FactionType.Player )
                    return DelReturn.Continue;
            }
            else //for players
            {
                if ( faction.Type != FactionType.Player )
                    return DelReturn.Continue;
            }
            faction.FactionPlannedFlows.SwitchConstructionToDisplay();
            return DelReturn.Continue;
        }
        #endregion

        public void help_DoPerPlanetFactionLogic( PlanetFaction pFaction, PlayerTypeData ForPlayerType )
        {
            if ( pFaction == null || pFaction.Entities.EntitiesOrNull_Squad == null ) //if no squads here, don't even try it!
                return;
            if ( pFaction.Entities.EntitiesOrNull_Squad.Count == 0 )
                return;
            Faction parentFaciton = pFaction.Faction;
            if ( parentFaciton == null || parentFaciton.Type == FactionType.NaturalObject )
                return;
            //FramePlan_PlannedFlows already have been cleared on the parent faction of the parent faction

            FInt effectiveDeltaTime_PausedOrUnpaused = World_AIW2.Instance.SimulationProfile.SecondsPerFrameSim_GlobalOnly_NotForPlanetsOrCombat_PausedOrUnpaused;
            if ( effectiveDeltaTime_PausedOrUnpaused > FInt.Zero )
                Unrolled_CalculateRequestedFlows_Outer( effectiveDeltaTime_PausedOrUnpaused, pFaction, ForPlayerType );
        }

        private void Unrolled_CalculateRequestedFlows_Outer( FInt effectiveDeltaTime, PlanetFaction pFaction, PlayerTypeData ForPlayerType )
        {
            ArcenOverLinkedList<GameEntity_Squad> rollup = pFaction.Entities.GetListOfEntitiesByRollupOrNull( EntityRollupType.HasAnyMetalFlows );
            if ( rollup == null )
                return;
            ArcenOverLinkedList<GameEntity_Squad>.ArcenLinkedListItem wrapper = rollup.GetFirst();
            while ( wrapper != null )
            {
                GameEntity_Squad entity = wrapper.Contained;
                if ( entity != null && !entity.HasBeenRemovedFromSim )
                {
                    entity.SquadPlannedFlows.ClearConstructionListForStartingConstruction();

                    CalculateRequestedFlows_Outer( effectiveDeltaTime, pFaction, entity, ForPlayerType );

                    entity.SquadPlannedFlows.SwitchConstructionToDisplay();
                }
                wrapper = wrapper.NextItem;
            }
        }

        private DelReturn CalculateRequestedFlows_Outer( FInt effectiveDeltaTime, PlanetFaction pFaction, GameEntity_Squad entity, PlayerTypeData ForPlayerType )
        {
            if ( entity.IsInHoldFireMode )
                return DelReturn.Continue;
            bool foundNonSelfBuildingFlow = false;
            for ( MetalFlowPurpose purpose = MetalFlowPurpose.None + 1; purpose < MetalFlowPurpose.Length; purpose++ )
            {
                switch ( purpose )
                {
                    case MetalFlowPurpose.AssistFactoryConstruction:
                    case MetalFlowPurpose.AssistSelfConstruction:
                    case MetalFlowPurpose.ClaimingNeutrals:
                    case MetalFlowPurpose.FactoryConstructionForPlayerMobileFleets:
                    case MetalFlowPurpose.RebuildingRemains:
                    case MetalFlowPurpose.RepairingEnginesOfFriendlies:
                    case MetalFlowPurpose.RepairingHullsOfFriendlies:
                    case MetalFlowPurpose.RepairingShieldsOfFriendlies:
                        if ( this.IsForNPCsOnly )
                            continue;
                        break;
                    case MetalFlowPurpose.SelfConstruction:
                        continue;
                }
                if ( !entity.DataForMark.GetMetalFlow( purpose ).HasRealData )
                    continue;
                foundNonSelfBuildingFlow = true;
            }
            if ( !foundNonSelfBuildingFlow && entity.SelfBuildingMetalRemaining <= FInt.Zero )
                return DelReturn.Continue;
            //non-functional entiteis can't even spend metal on self-building!
            if ( entity.GetIsNonFunctional() )
                return DelReturn.Continue; 
            //crippled entities can't spend metal unless still self building
            if ( entity.GetIsCrippled() && entity.SelfBuildingMetalRemaining <= 0 ) 
                return DelReturn.Continue;
            //self-constructing things that are blocked can't spend metal
            if ( entity.GetIsSelfConstructionBlocked() != ArcenRejectionReason.Unknown ) 
                return DelReturn.Continue;
            
            return CalculateRequestedFlows( effectiveDeltaTime, pFaction, entity, ForPlayerType );
        }

        private DelReturn CalculateRequestedFlows( FInt effectiveDeltaTime, PlanetFaction pFaction, GameEntity_Squad metalPlanEntity, PlayerTypeData ForPlayerType )
        {
            //skip invalid stuff
            if ( metalPlanEntity == null || metalPlanEntity.TypeData == null || metalPlanEntity.FleetMembership == null ||
                metalPlanEntity.HasBeenRemovedFromSim || metalPlanEntity.ToBeRemovedAtEndOfThisFrame ||
                metalPlanEntity.Planet == null )
                return DelReturn.Continue;

            ArcenCharacterBuffer traceBuffer = null;            

            int debugCode = 0;
            try
            {

                //entity.DebugText = "FLOWS:";
                debugCode = 100;
                EntityOrder order = metalPlanEntity.RemoveInvalidatedOrdersAndReturnFirstValid_ThatIsNotDecollision( false );
                GameEntity_Squad desiredTarget = null;
                if ( order.TypeData != null && order.TypeData.Type == EntityOrderType.Assist )
                    desiredTarget = order.RelatedSquad.GetSquad();
                debugCode = 200;
                for ( int purposeIndex = 0; purposeIndex < MetalFlowPurposeStatic.OrderedPurposesForProcessing.Length; purposeIndex++ )
                {
                    MetalFlowPurpose purpose = MetalFlowPurposeStatic.OrderedPurposesForProcessing[purposeIndex];

                    debugCode = 250;
                    try
                    {
                        switch ( purpose )
                        {
                            case MetalFlowPurpose.SelfConstruction:
                            case MetalFlowPurpose.FactoryConstructionForPlayerMobileFleets:
                            case MetalFlowPurpose.ClaimingNeutrals:
                            case MetalFlowPurpose.RebuildingRemains:
                            case MetalFlowPurpose.AssistFactoryConstruction:
                                if ( metalPlanEntity.GetFactionTypeSafe() != FactionType.Player )
                                    continue; //only player factions do these types
                                break;
                            case MetalFlowPurpose.RepairingHullsOfFriendlies:
                            case MetalFlowPurpose.RepairingShieldsOfFriendlies:
                            case MetalFlowPurpose.RepairingEnginesOfFriendlies:
                            case MetalFlowPurpose.AssistSelfConstruction:
                                if ( metalPlanEntity.GetFactionTypeSafe() == FactionType.AI )
                                    continue; //only non-AI factions do these types
                                break;
                            case MetalFlowPurpose.BuildingDronesInternally: //nobody does these in this fashion!!
                                continue;
                        }
                    }
                    catch { return DelReturn.Continue; } //in the event that something goes wrong here, we know it's because of cross-thread stuff

                    try
                    {
                        switch ( purpose )
                        {
                            case MetalFlowPurpose.SelfConstruction:
                                break;
                            default:
                                if ( metalPlanEntity.HasNotYetBeenFullyClaimed || metalPlanEntity.GetIsCrippled() )
                                    return DelReturn.Continue;
                                if ( metalPlanEntity.SelfBuildingMetalRemaining > FInt.Zero )
                                    continue; //if still self-building, don't do anything else
                                if ( metalPlanEntity.SecondsSpentAsRemains > 0 )
                                    continue; //if remains, don't do anything else
                                break;
                        }
                    }
                    catch { return DelReturn.Continue; } //in the event that something goes wrong here, we know it's because of cross-thread stuff

                    help_flowPurpose = purpose;
                    debugCode = 300;
                    EntityMetalFlowEntry entry = metalPlanEntity.DataForMark.GetMetalFlow( purpose );
                    if ( !entry.HasRealData )
                        continue;
                    //you can't claim neutrals on AI planets, or planets with
                    //units that block claim flows
                    if ( purpose == MetalFlowPurpose.ClaimingNeutrals &&
                         metalPlanEntity.Planet.GetControllingFactionType() != FactionType.Player )
                        continue;
                    if ( purpose == MetalFlowPurpose.ClaimingNeutrals &&
                         metalPlanEntity.Planet.GetNumberIn( FactionType.AI, EntityRollupType.BlocksEnemyClaimFlows, false, false ) > 0 )
                        continue;
                    debugCode = 310;
                    if ( purpose == MetalFlowPurpose.SelfConstruction )
                    {
                        ArcenRejectionReason rejectionReason = metalPlanEntity.ComputeDisabledReason( ArcenRejectionReason.EntityIsSelfBuilding );
                        if ( rejectionReason != ArcenRejectionReason.Unknown && 
                             rejectionReason != ArcenRejectionReason.CrippledInseadOfDead ) //DO still count NonFunctionalWhenNotOnPlanetOwnedByMyFaction
                        {
                            //entity.DebugText = "DISABLED:" + entity.ComputeDisabledReason( ArcenRejectionReason.EntityIsSelfBuilding );
                            continue;
                        }
                    }
                    else
                    {
                        if ( metalPlanEntity.ForShortTermPlanning_DisabledReason != ArcenRejectionReason.Unknown )
                        {
                            //entity.DebugText = "DISABLED PLANNING:" + entity.ForShortTermPlanning_DisabledReason;
                            continue;
                        }
                        else if ( metalPlanEntity.ComputeDisabledReason( ArcenRejectionReason.Unknown ) != ArcenRejectionReason.Unknown ) //just for safety  
                        {
                            continue;
                        }
                    }
                    debugCode = 320;
                    FInt maxPossibleFlow = entry.EffectiveThroughput * effectiveDeltaTime;
                    if ( maxPossibleFlow <= FInt.Zero )
                        continue;
                    debugCode = 325;
                    help_plannedFlow = PlannedMetalFlow.Create( null, MetalFlowPurpose.None );
                    debugCode = 330;
                    switch ( purpose )
                    {
                        case MetalFlowPurpose.FactoryConstructionForPlayerMobileFleets:
                            #region Factory Construction
                            DoubleBufferedList<FleetMembership> factoryTargets = pFaction.FactoryBoostsBySpecialFactoryType.GetListForOrNull( metalPlanEntity.TypeData.SpecialFactoryType );
                            if ( factoryTargets == null || factoryTargets.Count == 0 )
                                break;
                            if ( metalPlanEntity.IsInHoldFireMode )
                                break;
                            if ( metalPlanEntity.ComputeDisabledReason() != ArcenRejectionReason.Unknown )
                                break;
                            debugCode = 340;

                            InitPlannedFlowIfNull( metalPlanEntity, "FactoryHelping" );
                            help_plannedFlow.IsInRangeAtTheMoment = true; //factories have no assist range on planets
                            
                            help_plannedFlow.RequestedFlow = maxPossibleFlow;

                            //when in brownout, halve factory-based metal spending
                            if ( metalPlanEntity.PlanetFaction.Faction.Type == FactionType.Player &&
                                 metalPlanEntity.PlanetFaction.Faction.SecondsSinceBrownout >= 0 )
                                help_plannedFlow.RequestedFlow /= 2;
                            #endregion
                            break;
                        case MetalFlowPurpose.ClaimingNeutrals:
                            {
                                #region ClaimingNeutrals
                                debugCode = 360;
                                PreCalculateClaimableTargetsForPlanetFaction( pFaction, ForPlayerType );
                                metpln_bestTarget = null;
                                metpln_priorityLevel = 0;
                                metpln_bestTargetDistance = 0;
                                metpln_bestNeededPercent = FInt.Zero;
                                //first check our desired target.  If we can get it, check nothing else
                                if ( desiredTarget != null
                                    //that said, it MUST be one of the valid things to assist in the first place!
                                    && PassesInnerCheck_ClaimingAssist( desiredTarget, pFaction, ForPlayerType ) )
                                {
                                    MetalFlowLocus = metalPlanEntity.WorldLocation;
                                    //MetalFlowRadius = entity.DataForMark.AssistRange;
                                    MetalFlowRadius = 99999999; //we would move to our desired target
                                    CheckClaimable( metalPlanEntity, desiredTarget );
                                }
                                if ( metpln_bestTarget == null && //metpln_bestTarget may have been set by the desiredTarget check above
                                    lastPreCalculateClaimableTargetsForPlanetFaction_Squads.Count == 0 )
                                    continue;
                                //if we had no desired target, or couldn't get it right now, then check other stuff
                                if ( metpln_bestTarget == null )
                                {
                                    debugCode = 361;
                                    if ( metalPlanEntity.DataForMark.AssistRange > 0 && metalPlanEntity.Orders.Behavior != EntityBehaviorType.Attacker_Full )
                                    {
                                        MetalFlowLocus = metalPlanEntity.WorldLocation;
                                        MetalFlowRadius = metalPlanEntity.DataForMark.AssistRange;
                                    }
                                    else
                                    {
                                        MetalFlowLocus = metalPlanEntity.WorldLocation;
                                        MetalFlowRadius = 99999999;
                                    }

                                    GameEntity_Squad claimable;
                                    for ( int i = 0; i < lastPreCalculateClaimableTargetsForPlanetFaction_Squads.Count; i++ )
                                    {
                                        claimable = lastPreCalculateClaimableTargetsForPlanetFaction_Squads[i].GetSquad();
                                        if ( claimable == null )
                                            continue;
                                        CheckClaimable( metalPlanEntity, claimable );
                                    }
                                }
                                if ( metpln_bestTarget == null )
                                    break;

                                FInt repairExpected = (FInt)metpln_bestTarget.HullPointsLost;
                                int maxHullPoints = metpln_bestTarget.GetMaxHullPoints();
                                FInt portionNeedingRepair = maxHullPoints <= 0 ? FInt.Zero : repairExpected / (FInt)maxHullPoints;
                                FInt repairCost = portionNeedingRepair * metpln_bestTarget.DataForMark.MetalCostToClaim;

                                if ( repairCost > 0 && repairCost > maxPossibleFlow )
                                {
                                    repairExpected = (repairExpected * maxPossibleFlow) / repairCost; // HP/s = ( HP * Metal/s ) / Metal 
                                    repairCost = maxPossibleFlow;
                                }
                                if ( repairExpected < 1 )
                                    repairExpected = FInt.One;
                                if ( repairCost < 1 )
                                    repairCost = FInt.One;
                                debugCode = 362;
                                InitPlannedFlowIfNull( metalPlanEntity, "Claiming" );
                                help_plannedFlow.SquadRecipient = metpln_bestTarget;
                                help_plannedFlow.PriorityLevel = metpln_priorityLevel;
                                help_plannedFlow.RequestedFlow += repairCost;
                                help_plannedFlow.IsInRangeAtTheMoment = metalPlanEntity.DataForMark.AssistRange <= 0 || metpln_bestTargetDistance <= metalPlanEntity.DataForMark.AssistRange;
                                metpln_bestTarget.FramePlan_ExpectedRepairs += repairExpected;
                                #endregion ClaimingNeutrals
                            }
                            break;
                        case MetalFlowPurpose.RepairingHullsOfFriendlies:
                        case MetalFlowPurpose.RepairingShieldsOfFriendlies:
                        case MetalFlowPurpose.RepairingEnginesOfFriendlies:
                            #region Repairs
                            {
                                debugCode = 3700;
                                metpln_bestTarget = null;
                                metpln_priorityLevel = 0;
                                metpln_bestTargetDistance = 0;
                                metpln_bestNeededPercent = FInt.Zero;

                                PreCalculateAllRepairableTargetsForPlanetFaction( pFaction );
                                #region Tracing
                                if ( traceBuffer != null )
                                {
                                    ArcenDebugging.ArcenDebugLogSingleLine( traceBuffer.ToString(), Verbosity.Chat );
                                    traceBuffer.ReturnToPool();
                                    traceBuffer = null;
                                }
                                #endregion
                                debugCode = 3710;
                                //first check our desired target.  If we can get it, check nothing else  
                                if ( desiredTarget != null
                                   //that said, it MUST be one of the valid things to assist in the first place!
                                   && PassesInnerCheck_RepairsOfType( desiredTarget )
                                   //and this second check ensures that this specific ship is damaged in the way that we care about here
                                   && PassesInnerCheck_NeedsRepairsOfType( desiredTarget, purpose ) )
                                {
                                    MetalFlowLocus = metalPlanEntity.WorldLocation;
                                    //MetalFlowRadius = entity.DataForMark.AssistRange;
                                    MetalFlowRadius = 99999999; //we would move to our desired target
                                    debugCode = 3720;
                                    CheckRepairable_Inner( metalPlanEntity, desiredTarget );
                                }
                                debugCode = 3725;
                                switch ( purpose )
                                {
                                    case MetalFlowPurpose.RepairingHullsOfFriendlies:
                                        WorkingCountOfRepairingHullsOfFriendlies++;
                                        if ( metpln_bestTarget == null && //metpln_bestTarget may have been set by the desiredTarget check above
                                            lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Hull.Count <= 0 )
                                            continue;
                                        break;
                                    case MetalFlowPurpose.RepairingShieldsOfFriendlies:
                                        WorkingCountOfRepairingShieldsOfFriendlies++;
                                        if ( metpln_bestTarget == null && //metpln_bestTarget may have been set by the desiredTarget check above
                                            lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Shields.Count <= 0 )
                                            continue;
                                        break;
                                    case MetalFlowPurpose.RepairingEnginesOfFriendlies:
                                        WorkingCountOfRepairingEnginesOfFriendlies++;
                                        if ( metpln_bestTarget == null && //metpln_bestTarget may have been set by the desiredTarget check above
                                            lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Engines.Count <= 0 )
                                            continue;
                                        break;
                                }
                                debugCode = 3730;
                                //if we had no desired target, or couldn't get it right now, then check other stuff
                                if ( metpln_bestTarget == null )
                                    CheckRepairable( metalPlanEntity );
                                debugCode = 3740;
                                if ( metpln_bestTarget != null )
                                {
                                    debugCode = 3750;
                                    #region Tracing
                                    bool tracing = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Repairs ) && (
                                                        (metpln_bestTarget == GameEntity_Base.CurrentlyHoveredOver) ||
                                                        (metalPlanEntity == GameEntity_Base.CurrentlyHoveredOver)
                                        );
                                    if ( tracing ) traceBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "MetalFlows-Repairs-trace", 10f );
                                    if ( tracing ) traceBuffer.Add( "\n" ).Add( metalPlanEntity.TypeData.InternalName + " " + metalPlanEntity.PrimaryKeyID ).Add( " Planning " ).Add( EnumNameCache.GetName( purpose ) ).Add( " for " ).Add( metpln_bestTarget.TypeData.InternalName );
                                    #endregion
                                    int currentLost;
                                    int max;
                                    switch ( purpose )
                                    {
                                        case MetalFlowPurpose.RepairingHullsOfFriendlies:
                                            currentLost = metpln_bestTarget.HullPointsLost;
                                            max = metpln_bestTarget.GetMaxHullPoints();
                                            break;
                                        case MetalFlowPurpose.RepairingShieldsOfFriendlies:
                                            currentLost = metpln_bestTarget.ShieldPointsLost;
                                            max = metpln_bestTarget.GetMaxShieldPoints();
                                            break;
                                        case MetalFlowPurpose.RepairingEnginesOfFriendlies:
                                            currentLost = metpln_bestTarget.CurrentEngineStunSeconds;
                                            max = ExternalConstants.Instance.MaxEngineStun;
                                            break;
                                        default:
                                            return DelReturn.Continue;
                                    }
                                    FInt repairExpected = (FInt)currentLost;
                                    FInt portionNeedingRepair = max <= 0 ? FInt.Zero : repairExpected / max;
                                    int metalCost = metpln_bestTarget.GetMetalCost();
                                    if ( metalCost <= 0 )
                                        metalCost = metpln_bestTarget.DataForMark.MetalCostToClaim;

                                    FInt repairCost = portionNeedingRepair * metalCost;
                                    if ( repairCost > 0 && repairCost > maxPossibleFlow )
                                    {
                                        repairExpected = (repairExpected * maxPossibleFlow) / repairCost;
                                        repairCost = maxPossibleFlow;
                                    }
                                    if ( repairExpected < 1 )
                                        repairExpected = FInt.One;
                                    if ( repairCost < 1 )
                                        repairCost = FInt.One;

                                    InitPlannedFlowIfNull( metalPlanEntity, "RepairOrSomething" );
                                    help_plannedFlow.SquadRecipient = metpln_bestTarget;
                                    help_plannedFlow.PriorityLevel = metpln_priorityLevel;
                                    help_plannedFlow.RequestedFlow += repairCost;
                                    help_plannedFlow.IsInRangeAtTheMoment = metalPlanEntity.DataForMark.AssistRange <= 0 || metpln_bestTargetDistance <= metalPlanEntity.DataForMark.AssistRange;
                                    metpln_bestTarget.FramePlan_ExpectedRepairs += repairExpected;
                                    #region Tracing
                                    if ( tracing ) traceBuffer.Add( ":" ).Add( "requesting " ).Add( repairCost.ReadableString ).Add( " to pay for " ).Add( repairExpected.ReadableString ).Add( " repairs" );
                                    #endregion
                                }
                                #region Tracing
                                if ( traceBuffer != null )
                                {
                                    ArcenDebugging.ArcenDebugLogSingleLine( traceBuffer.ToString(), Verbosity.Chat );
                                    traceBuffer.ReturnToPool();
                                    traceBuffer = null;
                                }
                                #endregion
                            }
                            #endregion Repairs
                            break;
                        case MetalFlowPurpose.RebuildingRemains:
                            #region Remains Rebuild
                            {
                                PreCalculateRemainsRebuildTargetsForPlanetFaction( pFaction );
                                debugCode = 380;
                                metpln_bestTarget = null;
                                metpln_priorityLevel = 0;
                                metpln_bestTargetDistance = 0;
                                metpln_bestNeededPercent = FInt.Zero;

                                //first check our desired target.  If we can get it, check nothing else
                                if ( desiredTarget != null
                                    //that said, it MUST be one of the valid things to assist in the first place!
                                    && PassesInnerCheck_RebuildAssist( desiredTarget ) )
                                {
                                    MetalFlowLocus = metalPlanEntity.WorldLocation;
                                    //MetalFlowRadius = entity.DataForMark.AssistRange;
                                    MetalFlowRadius = 99999999; //we would move to our desired target
                                    CheckRemainsRebuildable( desiredTarget );
                                }
                                if ( metpln_bestTarget == null && //metpln_bestTarget may have been set by the desiredTarget check above
                                    lastPreCalculateRemainsRebuildTargetsForPlanetFaction_Squads.Count == 0 )
                                    continue;
                                //if we had no desired target, or couldn't get it right now, then check other stuff
                                if ( metpln_bestTarget == null )
                                {
                                    MetalFlowLocus = metalPlanEntity.WorldLocation;
                                    MetalFlowRadius = 99999999;
                                    GameEntity_Squad rebuildable;
                                    for ( int i = 0; i < lastPreCalculateRemainsRebuildTargetsForPlanetFaction_Squads.Count; i++ )
                                    {
                                        rebuildable = lastPreCalculateRemainsRebuildTargetsForPlanetFaction_Squads[i].GetSquad();
                                        CheckRemainsRebuildable( rebuildable );
                                    }
                                }
                                if ( metpln_bestTarget == null )
                                    break;
                                InitPlannedFlowIfNull( metalPlanEntity, "Remains" );
                                help_plannedFlow.SquadRecipient = metpln_bestTarget;
                                help_plannedFlow.PriorityLevel = metpln_priorityLevel;
                                help_plannedFlow.RequestedFlow += metpln_bestTarget.GetMetalCost();
                                // Rebuilding is infinite range, since most rebuilders are flagships, which often won't move towards their target.
                                help_plannedFlow.IsInRangeAtTheMoment = true;
                                metpln_bestTarget.FramePlan_ExpectSquadReplacements++; // a misnomer, but basically means "somebody is rebuilding this, so no one else needs to"
                            }
                            #endregion
                            break;
                        case MetalFlowPurpose.AssistSelfConstruction:
                            #region Assist Self Construction
                            {
                                PreCalculateSelfAssistConstTargetsForPlanetFaction( pFaction );
                                WorkingCountOfSelfAssistConstructions++;
                                debugCode = 390;
                                metpln_bestTarget = null;
                                metpln_priorityLevel = 0;
                                metpln_bestTargetDistance = 0;
                                metpln_bestNeededPercent = FInt.Zero;
                                //first check our desired target.  If we can get it, check nothing else
                                if ( desiredTarget != null
                                    //that said, it MUST be one of the valid things to assist in the first place!
                                    && PassesInnerCheck_SelfConstAssist( desiredTarget ) )
                                {
                                    MetalFlowLocus = metalPlanEntity.WorldLocation;
                                    //MetalFlowRadius = entity.DataForMark.AssistRange;
                                    MetalFlowRadius = 99999999; //we would move to our desired target
                                    CheckSelfConstructionAssistable( metalPlanEntity, desiredTarget );
                                }
                                if ( metpln_bestTarget == null && //metpln_bestTarget may have been set by the desiredTarget check above
                                    lastPreCalculateSelfAssistConstTargetsForPlanetFaction_Squads.Count == 0 )
                                    continue;
                                //if we had no desired target, or couldn't get it right now, then check other stuff
                                if ( metpln_bestTarget == null )
                                {
                                    if ( metalPlanEntity.DataForMark.AssistRange > 0 && metalPlanEntity.Orders.Behavior != EntityBehaviorType.Attacker_Full )
                                    {
                                        MetalFlowLocus = metalPlanEntity.WorldLocation;
                                        MetalFlowRadius = metalPlanEntity.DataForMark.AssistRange;
                                    }
                                    else
                                    {
                                        MetalFlowLocus = metalPlanEntity.WorldLocation;
                                        MetalFlowRadius = 99999999;
                                    }
                                    GameEntity_Squad assistable;
                                    for ( int i = 0; i < lastPreCalculateSelfAssistConstTargetsForPlanetFaction_Squads.Count; i++ )
                                    {
                                        assistable = lastPreCalculateSelfAssistConstTargetsForPlanetFaction_Squads[i].GetSquad();
                                        CheckSelfConstructionAssistable( metalPlanEntity, assistable );
                                    }
                                }
                                if ( metpln_bestTarget == null )
                                    break;
                                InitPlannedFlowIfNull( metalPlanEntity, "SelfAssistConst" );
                                help_plannedFlow.SquadRecipient = metpln_bestTarget;
                                help_plannedFlow.PriorityLevel = metpln_priorityLevel;
                                help_plannedFlow.RequestedFlow += maxPossibleFlow;
                                help_plannedFlow.IsInRangeAtTheMoment = metalPlanEntity.DataForMark.AssistRange <= 0 || metpln_bestTargetDistance <= metalPlanEntity.DataForMark.AssistRange;
                            }
                            break;
                        #endregion End Self Assist Construction
                        case MetalFlowPurpose.AssistFactoryConstruction:
                            #region Assist Factory Construction
                            {
                                debugCode = 39001;
                                PreCalculateFactoryAssistConstTargetsForPlanetFaction( pFaction );
                                WorkingCountOfFactoryAssistConstructions++;
                                debugCode = 39002;
                                metpln_bestTarget = null;
                                metpln_priorityLevel = 0;
                                metpln_bestTargetDistance = 0;
                                metpln_bestNeededPercent = FInt.Zero;
                                debugCode = 39003;
                                //first check our desired target.  If we can get it, check nothing else
                                if ( desiredTarget != null &&
                                    //that said, it MUST be one of the valid things to assist in the first place!
                                    PassesInnerCheck_FactoryAssist( desiredTarget ) )
                                {
                                    debugCode = 39010;
                                    MetalFlowLocus = metalPlanEntity.WorldLocation;
                                    //MetalFlowRadius = entity.DataForMark.AssistRange;
                                    MetalFlowRadius = 99999999; //we would move to our desired target
                                    debugCode = 39020;
                                    CheckFactoryConstructionAssistable( metalPlanEntity, desiredTarget );
                                }
                                debugCode = 39030;
                                if ( metpln_bestTarget == null && //metpln_bestTarget may have been set by the desiredTarget check above
                                    lastPreCalculateFactoryAssistConstTargetsForPlanetFaction_Squads.Count == 0 )
                                    continue;
                                debugCode = 39040;
                                //if we had no desired target, or couldn't get it right now, then check other stuff
                                if ( metpln_bestTarget == null )
                                {
                                    debugCode = 39050;
                                    if ( metalPlanEntity.DataForMark.AssistRange > 0 && metalPlanEntity.Orders.Behavior != EntityBehaviorType.Attacker_Full )
                                    {
                                        debugCode = 39060;
                                        MetalFlowLocus = metalPlanEntity.WorldLocation;
                                        MetalFlowRadius = metalPlanEntity.DataForMark.AssistRange;
                                    }
                                    else
                                    {
                                        debugCode = 39070;
                                        MetalFlowLocus = metalPlanEntity.WorldLocation;
                                        MetalFlowRadius = 99999999;
                                    }
                                    debugCode = 39080;
                                    GameEntity_Squad assistable;
                                    for ( int i = 0; i < lastPreCalculateFactoryAssistConstTargetsForPlanetFaction_Squads.Count; i++ )
                                    {
                                        debugCode = 39090;
                                        assistable = lastPreCalculateFactoryAssistConstTargetsForPlanetFaction_Squads[i].GetSquad();
                                        debugCode = 39091;
                                        CheckFactoryConstructionAssistable( metalPlanEntity, assistable );
                                    }
                                }
                                debugCode = 39110;
                                if ( metpln_bestTarget == null )
                                    break;
                                debugCode = 39120;
                                InitPlannedFlowIfNull( metalPlanEntity, "FactoryAssistConst" );
                                debugCode = 39130;
                                help_plannedFlow.SquadRecipient = metpln_bestTarget;
                                debugCode = 39140;
                                help_plannedFlow.PriorityLevel = metpln_priorityLevel;
                                debugCode = 39150;
                                FInt flowToAdd = maxPossibleFlow;
                                //when in brownout, halve factory-based metal spending
                                if ( metalPlanEntity.PlanetFaction.Faction.Type == FactionType.Player && //todo: maybe this needs a separate check for alternate player factions like the necromancer?
                                     metalPlanEntity.PlanetFaction.Faction.SecondsSinceBrownout >= 0 )
                                    flowToAdd /= 2;

                                debugCode = 39160;
                                help_plannedFlow.RequestedFlow += flowToAdd;
                                help_plannedFlow.IsInRangeAtTheMoment = metalPlanEntity.DataForMark.AssistRange <= 0 || metpln_bestTargetDistance <= metalPlanEntity.DataForMark.AssistRange;
                            }
                            break;
                        #endregion End Assist Factory Construction
                        case MetalFlowPurpose.SelfConstruction:
                            debugCode = 400;
                            if ( metalPlanEntity.SelfBuildingMetalRemaining <= FInt.Zero )
                                break;
                            InitPlannedFlowIfNull( metalPlanEntity, "SelfConst" );
                            help_plannedFlow.RequestedFlow = metalPlanEntity.SelfBuildingMetalRemaining;
                            help_plannedFlow.IsInRangeAtTheMoment = true;
                            help_plannedFlow.SquadRecipient = metalPlanEntity; //link to myself
                            break;
                    }
                    debugCode = 410;
                    if ( help_plannedFlow.FromEntity == null )
                        continue; //nothing to do!

                    help_plannedFlow.RequestedFlow = Mat.Min( help_plannedFlow.RequestedFlow, maxPossibleFlow );
                    //entity.DebugText += " " + plannedFlow.Purpose + " " + plannedFlow.RequestedFlow;
                    if ( help_plannedFlow.RequestedFlow <= FInt.Zero )
                    {
                        continue;
                    }
                    debugCode = 420;
                    Faction facOrNull = metalPlanEntity.GetFactionOrNull_Safe();
                    debugCode = 422;
                    if ( facOrNull != null )
                    {
                        debugCode = 424;
                        facOrNull.FactionPlannedFlows.AddToConstructionList( help_plannedFlow );
                    }
                    debugCode = 425;
                    PlannedMetalFlow flowCopy = help_plannedFlow.CreateCopy();
                    debugCode = 426;
                    switch ( flowCopy.Purpose )
                    {
                        case MetalFlowPurpose.FactoryConstructionForPlayerMobileFleets:
                            metalPlanEntity.LastTimeSinceStartFWasFactoryConstructing = ArcenTime.TimeSinceStartF;
                            break;
                        case MetalFlowPurpose.SelfConstruction:
                            metalPlanEntity.LastTimeSinceStartFWasSelfBuilding = ArcenTime.TimeSinceStartF;
                            break;
                    }
                    metalPlanEntity.SquadPlannedFlows.AddToConstructionList( flowCopy ); //adding a single flow to two lists causes CHAOS since they are pooled.
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "CalculateRequestedFlows exception hit. debugCode " + debugCode + " exception " + e, Verbosity.DoNotShow );
            }
            if ( traceBuffer != null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( traceBuffer.ToString(), Verbosity.Chat );
                traceBuffer.ReturnToPool();
                traceBuffer = null;
            }
            return DelReturn.Continue;
        }

        public void InitPlannedFlowIfNull( GameEntity_Squad metalPlanEntity, string AppendixReason )
        {
            if ( help_plannedFlow.FromEntity != null )
                return;
            help_plannedFlow = PlannedMetalFlow.Create( metalPlanEntity, help_flowPurpose );
        }

        #region PreCalculateFactoryTargetsForPlanetFaction
        private Dictionary<string,bool> lastPreCalculatedFactoryTypes = Dictionary<string, bool>.Create_WillNeverBeGCed( 5, "MetalFlowPlanning-lastPreCalculatedFactoryTypes" );

        public void PreCalculateFactoryTargetsForPlanetFactionAndAnySpecialFactoryTypes( PlanetFaction pFaction )
        {
            pFaction.FactoryBoostsBySpecialFactoryType.ClearConstructionListForStartingConstruction();
            if ( pFaction.Entities.GetCountFromListOfEntitiesByRollup( EntityRollupType.HasFactoryFlows ) <= 0 )
            {
                pFaction.FactoryBoostsBySpecialFactoryType.SwitchConstructionToDisplay();
                return; //nothing to do
            }

            lastPreCalculatedFactoryTypes.Clear();

            foreach ( GameEntity_Squad factory in pFaction.Entities.Squads( EntityRollupType.HasFactoryFlows ) )
            {
                if ( factory.ComputeDisabledReason() != ArcenRejectionReason.Unknown )
                    continue; //skip if disabled
                string specialFactoryType = factory.TypeData.SpecialFactoryType;
                if ( lastPreCalculatedFactoryTypes.ContainsKey( specialFactoryType ) )
                    continue; //skip if already calculated for this special factory type on this planet
                lastPreCalculatedFactoryTypes[specialFactoryType] = true;

                bool factoriesBuildForAllies = (specialFactoryType != null && specialFactoryType.Length > 0) || //special factories (like spire) always boost allies
                    (pFaction.Faction.IsConsideredAFullEmpirePlayerType_Safe() && AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "FactoriesBuildForAllies" ));
                if ( factoriesBuildForAllies )
                {
                    PlayerTypeData playerType = pFaction.Faction.PlayerTypeDataOrNull_ModeratelyExpensive;
                    if ( playerType != null && !playerType.UsesMetal )
                        factoriesBuildForAllies = false;
                }

                DoubleBufferedList<FleetMembership> listToAddTo = null;

                int debugCode = 1;
                try
                {
                    foreach ( Fleet fleet in World_AIW2.Instance.Fleets( FleetStatus.AnyStatus ) )
                    {
                        debugCode = 350;
                        if ( fleet.IsFleetConstructionPaused )
                            continue; //are these turned off?
                        if ( fleet.IsFleetConstructionBlocked )
                            continue;
                        if ( fleet.Faction == null || fleet.Faction.Type != FactionType.Player )
                            continue;
                        if ( !factoriesBuildForAllies && fleet.Faction != pFaction.Faction )
                            continue;

                        GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                        if ( centerpiece == null )
                        {
                            //ArcenDebugging.ArcenDebugLogSingleLine( "Null flagship on fleet '" + fleet.GetName() + " in metal flow planning", Verbosity.ShowAsError );
                            continue;
                        }
                        if ( centerpiece.HasNotYetBeenFullyClaimed ) // If we're still claiming the centerpiece, don't build ships
                            continue;
                        if ( centerpiece.GetIsCrippled() ) // If this centerpiece is crippled, it can't build
                            continue;
                        if ( centerpiece.TypeData.SpecialFactoryType != specialFactoryType )
                            continue;
                        
                        if ( centerpiece.Planet != pFaction.Planet )
                        {
                            debugCode = 351;
                            bool isOnANeighboringPlanet = false;
                            foreach ( Planet otherPlanet in pFaction.Planet.LinkedNeighbors( false ) )
                            {
                                if ( otherPlanet == centerpiece.Planet )
                                {
                                    isOnANeighboringPlanet = true;
                                    break;
                                }
                            }
                            //can't contribute to fleets that are not on our planet or an adjacent planet
                            if ( !isOnANeighboringPlanet )
                                continue;
                        }
                        debugCode = 35201;

                        debugCode = 352002;
                        //at this point it's our fleet and it's on our planet or an adjacent planet, so let's see what work needs doing
                        bool hadAtLeastOneMemberTypeToBuildFrom = false;
                        foreach ( FleetMembership member in fleet.MemberGroupsUnsorted_Sim )
                        {
                            debugCode = 353;
                            if ( member.TypeData.IsDrone || member.TypeData.SelfConstructs || member.TypeData.IsIgnoredByFactories ||
                                member.TypeData.IsFleetLeader ||
                                member.TypeData.CannotActuallyBeBuilt_BuildsSelfIndirectly ||
                                member.TypeData.CannotActuallyBeBuilt_IsNotAShipLine ||
                                member.TypeData.FleetMembershipStyle == FleetMembershipStyle.Planetary )
                                continue;
                            if ( member.IsFleetMembershipConstructionPaused )
                                continue; //are these turned off?

                            if ( World_AIW2.Instance.IsFuelOveruseDisabled && member.TypeData.FuelArgonUsage > 0 && pFaction.Faction.NetFuelArgon - member.TypeData.FuelArgonUsage < 0 )
                                continue;
                            if ( World_AIW2.Instance.IsFuelOveruseDisabled && member.TypeData.FuelRadonUsage > 0 && pFaction.Faction.NetFuelRadon - member.TypeData.FuelRadonUsage < 0 )
                                continue;
                            if ( World_AIW2.Instance.IsFuelOveruseDisabled && member.TypeData.FuelXenonUsage > 0 && pFaction.Faction.NetFuelXenon - member.TypeData.FuelXenonUsage < 0 )
                                continue;

                            hadAtLeastOneMemberTypeToBuildFrom = true;

                            //ArcenDebugging.ArcenDebugLogSingleLine( pFaction.Faction.GetDisplayName() + ": " + 
                            //    fleet.GetName() + ": " + member.TypeData.DisplayName + " type: " + SpecialFactoryType, Verbosity.DoNotShow );

                            debugCode = 354;
                            //if there's room for the cap, then add to that to the list of recipients
                            if ( member.GetRemainingCap( true, -1, ExtraFromStacks.IncludePrecalc ) > 0 )
                            {
                                if ( listToAddTo == null )
                                    listToAddTo = pFaction.FactoryBoostsBySpecialFactoryType[specialFactoryType];
                                listToAddTo.AddToConstructionList( member );
                            }
                        }

                        debugCode = 35405;
                        if ( hadAtLeastOneMemberTypeToBuildFrom && 
                            fleet.NumberGameSecondsBeforeCanBeAssisted <= 0 ) //if it can't be assisted yet, don't log the factory as able to!
                        {
                            //log that these factories want to help
                            foreach ( GameEntity_Squad factory2 in pFaction.Entities.Squads( EntityRollupType.HasFactoryFlows ) )
                            {
                                if ( factory2.TypeData.SpecialFactoryType != specialFactoryType )
                                    continue;
                                fleet.SupportingFactoriesInRange.AddToConstructionList( factory2 );
                            }
                        }

                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "PreCalculateFactoryTargetsForPlanetFaction exception hit. debugCode " + debugCode + " exception " + e, Verbosity.DoNotShow );
                }

            }

            pFaction.FactoryBoostsBySpecialFactoryType.SwitchConstructionToDisplay();            
        }
        #endregion

        #region PreCalculateClaimableTargetsForPlanetFaction
        private PlanetFaction lastPreCalculateClaimableTargetsForPlanetFaction_PFaction;
        private List<SafeSquadWrapper> lastPreCalculateClaimableTargetsForPlanetFaction_Squads = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 3000, "MetalFlowPlanning-lastPreCalculateClaimableTargetsForPlanetFaction_Squads" );
        public void PreCalculateClaimableTargetsForPlanetFaction( PlanetFaction pFaction, PlayerTypeData ClaimingPlayerType )
        {
            if ( lastPreCalculateClaimableTargetsForPlanetFaction_PFaction == pFaction )
                return; //we're lazy-calculating this, in case nothing needs to be calculated.
            lastPreCalculateClaimableTargetsForPlanetFaction_PFaction = pFaction;

            if ( pFaction.Entities.GetCountFromListOfEntitiesByRollup( EntityRollupType.HasClaimFlows ) <= 0 )
                return; //if nothing can do claims, don't check for things to repair

            lastPreCalculateClaimableTargetsForPlanetFaction_Squads.Clear();

            int debugCode = 1;
            try
            {
                foreach ( GameEntity_Squad claimable in pFaction.Planet.Squads( EntityRollupType.Claimables ) )
                {
                    
                    if ( PassesInnerCheck_ClaimingAssist( claimable, pFaction, ClaimingPlayerType ) )
                        lastPreCalculateClaimableTargetsForPlanetFaction_Squads.Add( claimable );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "PreCalculateClaimableTargetsForPlanetFaction exception hit. debugCode " + debugCode + " exception " + e, Verbosity.DoNotShow );
            }
        }

        private bool PassesInnerCheck_ClaimingAssist( GameEntity_Squad claimable, PlanetFaction pFaction, PlayerTypeData ClaimingPlayerType )
        {
            if ( claimable.TypeData.GetHasTag( "NotAutoClaimable" ) )
                return false; // must be claimed via a specific hack, not by general neutral-claiming income
            if ( claimable.GetFactionTypeSafe() != FactionType.NaturalObject && !claimable.HasNotYetBeenFullyClaimed )
                return false; // skip things that aren't claimable
            if ( claimable.IsInHoldFireMode )
                return false; //skip things in hold fire mode!
            //if have been in existence -- or dead from reverting to claimed -- for too little time, then skip
            if ( claimable.GetRemainingSecondsBeforeCanClaim( pFaction ) > 0 )
                return false;
            if ( claimable.RepairImpossibleForSeconds > 0 )
            {
                return false; // skip things that have been damaged too recently to be repairable
            }
            if ( claimable.TypeData.ClaimDisallowedForPlayersWhoDoNotUseMetal )
            {
                if ( ClaimingPlayerType == null || !ClaimingPlayerType.UsesMetal )
                    return false; //don't allow us to try to claim things that are disallowed for our player type
            }
            if ( pFaction.Faction.Type == FactionType.Player )
            {
                //if a player, and we could not afford the energy cost of the unit that is to be claimed, then don't allow claiming
                if ( claimable.TypeData.EnergyUsage > 0 && pFaction.Faction.NetEnergy - claimable.TypeData.EnergyUsage < 0 )
                    return false;
                if ( World_AIW2.Instance.IsFuelOveruseDisabled && claimable.TypeData.FuelArgonUsage > 0 && pFaction.Faction.NetFuelArgon - claimable.TypeData.FuelArgonUsage < 0 ) {
                    return false;
                }
                if ( World_AIW2.Instance.IsFuelOveruseDisabled && claimable.TypeData.FuelRadonUsage > 0 && pFaction.Faction.NetFuelRadon - claimable.TypeData.FuelRadonUsage < 0 )
                {
                    return false;
                }
                if ( World_AIW2.Instance.IsFuelOveruseDisabled && claimable.TypeData.FuelXenonUsage > 0 && pFaction.Faction.NetFuelXenon - claimable.TypeData.FuelXenonUsage < 0 )
                {
                    return false;
                }
            }
            //claimable.DebugText = "HasNotYetBeenFullyClaimed: " + claimable.HasNotYetBeenFullyClaimed + " giving the ok";
            return true;
        }
        #endregion

        #region PreCalculateAllRepairableTargetsForPlanetFaction
        private PlanetFaction lastPreCalculateAllRepairableTargetsForPlanetFaction_PFaction;
        private List<SafeSquadWrapper> lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Hull = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 3000, "MetalFlowPlanning-lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Hull" );
        private List<SafeSquadWrapper> lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Shields = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 3000, "MetalFlowPlanning-lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Shields" );
        private List<SafeSquadWrapper> lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Engines = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 3000, "MetalFlowPlanning-lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Engines" );
        public void PreCalculateAllRepairableTargetsForPlanetFaction( PlanetFaction pFaction )
        {
            if ( lastPreCalculateAllRepairableTargetsForPlanetFaction_PFaction == pFaction )
                return; //we're lazy-calculating this, in case nothing needs to be calculated.
            lastPreCalculateAllRepairableTargetsForPlanetFaction_PFaction = pFaction;

            lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Hull.Clear();
            lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Shields.Clear();
            lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Engines.Clear();

            if ( pFaction.Entities.GetCountFromListOfEntitiesByRollup( EntityRollupType.HasRepairFlows ) <= 0 )
                return; //if nothing can do repairs, don't check for things to repair
            bool repairOtherPlayers = ( pFaction.Faction.IsConsideredAFullEmpirePlayerType_Safe() && AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "RepairOtherPlayers" ) );
            bool repairNPCAllies = (pFaction.Faction.IsConsideredAFullEmpirePlayerType_Safe() && AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "RepairNPCAllies" ));

            //int countWithSomeLost_Hulls = 0;
            //int countWithSomeLost_Shields = 0;
            //int countWithSomeLost_Engines = 0;
            //int countNotClaimed = 0;
            //int countRemainsOrDead = 0;
            //int countNoRepairAllies = 0;
            //int countSelfBuilding = 0;
            //int countZombies = 0;
            //int countRepairDelays = 0;

            int debugCode = 1;
            try
            {
                debugCode = 1000;
                foreach ( PlanetFaction faction in pFaction.RelatedFactions( FactionRelationship.Self.Add( FactionRelationship.FactionsIAmFriendlyTowards ) ) )
                {
                    debugCode = 1100;
                    if ( pFaction.Faction.Type == FactionType.Player && faction.Faction != pFaction.Faction )
                    {
                        if ( faction.Faction.Type == FactionType.Player ) //the other faction is a player
                        {
                            if ( !repairOtherPlayers ) //this is global for all players in MP, which seems fine
                            {
                                //countNoRepairAllies++;
                                continue;
                            }
                        }
                        else //the other faction is an allied NPC
                        {
                            if ( !repairNPCAllies ) //this is global for all players in MP, which seems fine
                            {
                                //countNoRepairAllies++;
                                continue;
                            }
                        }
                    }
                    debugCode = 3800;
                    if ( pFaction.Faction.SpecialFactionData.InternalName == "AntiAIZombie" )
                    {
                        //countZombies++;
                        // No one is allowed allowed to heal zombie ships
                        continue;
                    }

                    debugCode = 2000;
                    foreach ( GameEntity_Squad possibleTarget in faction.Entities.Squads() )
                    {
                        debugCode = 3000;

                        if ( PassesInnerCheck_RepairsOfType( possibleTarget ) )
                        {
                            debugCode = 4000;
                            if ( possibleTarget.HullPointsLost > 0 || possibleTarget.HasNotYetBeenFullyClaimed )
                                lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Hull.Add( possibleTarget );
                            debugCode = 5000;
                            if ( possibleTarget.ShieldPointsLost > 0 )
                                lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Shields.Add( possibleTarget );
                            debugCode = 6000;
                            if ( possibleTarget.CurrentEngineStunSeconds > 0 )
                                lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Engines.Add( possibleTarget );
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "PreCalculateAllRepairableTargetsForPlanetFaction exception hit. debugCode " + debugCode + " exception " + e, Verbosity.DoNotShow );
            }


            //StringBuilder builder = new StringBuilder();
            //builder.Append( "Planet: " ).Append( pFaction.GetPlanetName_Safe() ).Append( " faction: " ).Append( " faction: " ).Append( pFaction.Faction.GetDisplayName() );
            //builder.Append( "\nHulls: " ).Append( lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Hull.Count );
            //for ( int i = 0; i < lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Hull.Count; i++ )
            //    builder.Append( ", " ).Append( lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Hull[i].TypeData.DisplayName );
            //builder.Append( "\nShields: " ).Append( lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Shields.Count );
            //for ( int i = 0; i < lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Shields.Count; i++ )
            //    builder.Append( ", " ).Append( lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Shields[i].TypeData.DisplayName );
            //builder.Append( "\nEngines: " ).Append( lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Engines.Count );
            //for ( int i = 0; i < lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Engines.Count; i++ )
            //    builder.Append( ", " ).Append( lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Engines[i].TypeData.DisplayName );

            //ArcenDebugging.ArcenDebugLogSingleLine( builder.ToString(), Verbosity.DoNotShow );
        }

        private bool PassesInnerCheck_RepairsOfType( GameEntity_Squad possibleTarget )
        {
            if ( possibleTarget.TypeData.SelfAttritionsXPercentPerSecondIfParentShipNotOnPlanet > 0 )
                return false;

            if ( possibleTarget.TypeData.ImmuneToRepairs )
                return false;

            if ( possibleTarget.HullPointsLost <= 0 && possibleTarget.ShieldPointsLost <= 0 && possibleTarget.CurrentEngineStunSeconds <= 0 )
                return false;

            //if ( possibleTarget.HasNotYetBeenFullyClaimed )
            //{
            //    //countNotClaimed++;
            //    return false;
            //}
            if ( possibleTarget.TypeData.IsNonfunctionalWhenOnPlanetOwnedByAI &&
                 possibleTarget.Planet.GetControllingFactionType() == FactionType.AI )
                return false;
            if ( possibleTarget.SecondsSpentAsRemains >= 0 || possibleTarget.ToBeRemovedAtEndOfThisFrame || possibleTarget.GetCurrentHullPoints() <= 0 )
            {
                //countRemainsOrDead++;
                return false;
            }
            if ( !possibleTarget.HasNotYetBeenFullyClaimed ) //only make self-constructing items "impossible to repair" 
            {
                if ( possibleTarget.LastTimeSinceStartFWasSelfBuilding >= ArcenTime.TimeSinceStartF - 1f || //if was self building in the last second
                    possibleTarget.SelfBuildingMetalRemaining.RawValue > 0 )
                {
                    //countSelfBuilding++;
                    //note that just checking active flows isn't enough since the player may have put the building into HoldFireMode
                    return false;
                }
            }

            if ( possibleTarget.RepairImpossibleForSeconds > 0 )
            {
                //countRepairDelays++;
                return false;
            }
            return true;
        }

        private bool PassesInnerCheck_NeedsRepairsOfType( GameEntity_Squad possibleTarget, MetalFlowPurpose purpose )
        {
            switch (purpose)
            {
                case MetalFlowPurpose.RepairingHullsOfFriendlies:
                    return possibleTarget.HullPointsLost > 0 || possibleTarget.HasNotYetBeenFullyClaimed;
                case MetalFlowPurpose.RepairingShieldsOfFriendlies:
                    return possibleTarget.ShieldPointsLost > 0;
                case MetalFlowPurpose.RepairingEnginesOfFriendlies:
                    return possibleTarget.CurrentEngineStunSeconds > 0;
            }
            return false;
        }
        #endregion

        #region PreCalculateRemainsRebuildTargetsForPlanetFaction
        private PlanetFaction lastPreCalculateRemainsRebuildTargetsForPlanetFaction_PFaction;
        private List<SafeSquadWrapper> lastPreCalculateRemainsRebuildTargetsForPlanetFaction_Squads = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 3000, "MetalFlowPlanning-lastPreCalculateRemainsRebuildTargetsForPlanetFaction_Squads" );
        public void PreCalculateRemainsRebuildTargetsForPlanetFaction( PlanetFaction pFaction )
        {
            if ( lastPreCalculateRemainsRebuildTargetsForPlanetFaction_PFaction == pFaction )
                return; //we're lazy-calculating this, in case nothing needs to be calculated.
            lastPreCalculateRemainsRebuildTargetsForPlanetFaction_PFaction = pFaction;

            if ( pFaction.Entities.GetCountFromListOfEntitiesByRollup( EntityRollupType.HasRemainsRebuildFlows ) <= 0 )
                return; //if nothing can do rebuilds, don't check for things to rebuild

            lastPreCalculateRemainsRebuildTargetsForPlanetFaction_Squads.Clear();

            int debugCode = 1;
            try
            {
                foreach ( PlanetFaction faction in pFaction.RelatedFactions( FactionRelationship.Self.Add( FactionRelationship.FactionsIAmFriendlyTowards ) ) )
                {
                    foreach ( GameEntity_Squad possibleRemains in faction.Entities.Squads( EntityRollupType.PotentialRemainsToRebuild ) )
                    {
                        if ( PassesInnerCheck_RebuildAssist( possibleRemains ) )
                            lastPreCalculateRemainsRebuildTargetsForPlanetFaction_Squads.Add( possibleRemains );
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "PreCalculateRemainsRebuildTargetsForPlanetFaction exception hit. debugCode " + debugCode + " exception " + e, Verbosity.DoNotShow );
            }
        }

        private bool PassesInnerCheck_RebuildAssist( GameEntity_Squad possibleRemains )
        {
            if ( possibleRemains.GetCannotRebuildRemainsReason() != ArcenRejectionReason.Unknown )
                return false;
            if ( possibleRemains.FramePlan_ExpectSquadReplacements > 0 ) // a misnomer, but basically means "somebody is rebuilding this, so no one else needs to"
                return false;
            return true;
        }
        #endregion

        #region PreCalculateSelfAssistConstTargetsForPlanetFaction
        private PlanetFaction lastPreCalculateSelfAssistConstTargetsForPlanetFaction_PFaction;
        private List<SafeSquadWrapper> lastPreCalculateSelfAssistConstTargetsForPlanetFaction_Squads = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 3000, "MetalFlowPlanning-lastPreCalculateSelfAssistConstTargetsForPlanetFaction_Squads" );
        public void PreCalculateSelfAssistConstTargetsForPlanetFaction( PlanetFaction pFaction )
        {
            if ( lastPreCalculateSelfAssistConstTargetsForPlanetFaction_PFaction == pFaction )
                return; //we're lazy-calculating this, in case nothing needs to be calculated.
            lastPreCalculateSelfAssistConstTargetsForPlanetFaction_PFaction = pFaction;

            if ( pFaction.Entities.GetCountFromListOfEntitiesByRollup( EntityRollupType.SelfConstructs ) <= 0 )
                return; //if nothing can do SelfAssist, don't check for things to SelfAssist

            lastPreCalculateSelfAssistConstTargetsForPlanetFaction_Squads.Clear();

            bool assistAlliedConstruction = (pFaction.Faction.IsConsideredAFullEmpirePlayerType_Safe() && AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "AssistAlliedConstruction" ) );
            if ( assistAlliedConstruction )
            {
                PlayerTypeData playerType = pFaction.Faction.PlayerTypeDataOrNull_ModeratelyExpensive;
                if ( playerType != null && !playerType.UsesMetal )
                    assistAlliedConstruction = false;
            }

            int debugCode = 1;
            try
            {
                foreach ( PlanetFaction faction in pFaction.RelatedFactions( FactionRelationship.Self.Add( FactionRelationship.FactionsIAmFriendlyTowards ) ) )
                {
                    if ( pFaction.Faction.Type == FactionType.Player && faction.Faction != pFaction.Faction )
                    {
                        bool isEmpire = pFaction.Faction.IsConsideredAFullEmpirePlayerType_Safe();
                        if ( isEmpire )
                        {
                            if ( isEmpire != faction.Faction.IsConsideredAFullEmpirePlayerType_Safe() )
                                continue; //empire can't help non-empire, and vice versa; they are balanced very differently
                        }
                        else
                        {
                            //if we're not an empire...
                            if ( pFaction.Faction.PlayerTypeName_ModeratelyExpensive != faction.Faction.PlayerTypeName_ModeratelyExpensive )
                                continue; //non-empires can only help other non-empires of the exact same sort
                        }

                        if ( !assistAlliedConstruction ) //this is global for all players in MP, which seems fine
                        {
                            //countNoRepairAllies++;
                            continue;
                        }
                    }

                    foreach ( GameEntity_Squad assistable in faction.Entities.Squads( EntityRollupType.SelfConstructs ) )
                    {
                        if ( PassesInnerCheck_SelfConstAssist( assistable ) )
                            lastPreCalculateSelfAssistConstTargetsForPlanetFaction_Squads.Add( assistable );
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "PreCalculateSelfAssistConstTargetsForPlanetFaction exception hit. debugCode " + debugCode + " exception " + e, Verbosity.DoNotShow );
            }

            //StringBuilder builder = new StringBuilder();
            //builder.Append( "Planet: " ).Append( pFaction.GetPlanetName_Safe() ).Append( " faction: " ).Append( " faction: " ).Append( pFaction.Faction.GetDisplayName() );
            //builder.Append( "\nAssistable: " ).Append( lastPreCalculateSelfAssistConstTargetsForPlanetFaction_Squads.Count );
            //for ( int i = 0; i < lastPreCalculateSelfAssistConstTargetsForPlanetFaction_Squads.Count; i++ )
            //    builder.Append( ", " ).Append( lastPreCalculateSelfAssistConstTargetsForPlanetFaction_Squads[i].TypeData.DisplayName );

            //ArcenDebugging.ArcenDebugLogSingleLine( builder.ToString(), Verbosity.DoNotShow );
        }

        private bool PassesInnerCheck_SelfConstAssist( GameEntity_Squad assistable )
        {
            if ( assistable.LastTimeSinceStartFWasSelfBuilding < ArcenTime.TimeSinceStartF - 1f ) //if has not been self building in the last second
                return false;
            if ( assistable.SecondsSpentAsRemains >= 0 || assistable.ToBeRemovedAtEndOfThisFrame || assistable.GetCurrentHullPoints() <= 0 )
                return false;
            // If not still constructing, don't help in this way
            if ( assistable.SelfBuildingMetalRemaining.RawValue <= 0 )
                return false;
            //so we don't try to help things that are blocked from building
            if ( assistable.GetIsSelfConstructionBlocked() != ArcenRejectionReason.Unknown )
                return false;
            return true;
        }
        #endregion

        #region PreCalculateFactoryAssistConstTargetsForPlanetFaction
        private PlanetFaction lastPreCalculateFactoryAssistConstTargetsForPlanetFaction_PFaction;
        private List<SafeSquadWrapper> lastPreCalculateFactoryAssistConstTargetsForPlanetFaction_Squads = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 3000, "MetalFlowPlanning-lastPreCalculateFactoryAssistConstTargetsForPlanetFaction_Squads" );
        public void PreCalculateFactoryAssistConstTargetsForPlanetFaction( PlanetFaction pFaction )
        {
            if ( lastPreCalculateFactoryAssistConstTargetsForPlanetFaction_PFaction == pFaction )
                return; //we're lazy-calculating this, in case nothing needs to be calculated.
            lastPreCalculateFactoryAssistConstTargetsForPlanetFaction_PFaction = pFaction;

            if ( pFaction.Entities.GetCountFromListOfEntitiesByRollup( EntityRollupType.HasFactoryFlows ) <= 0 )
                return; //if nothing can do FactoryAssist, don't check for things to FactoryAssist

            lastPreCalculateFactoryAssistConstTargetsForPlanetFaction_Squads.Clear();

            bool helpFactoriesOfAllies = (pFaction.Faction.IsConsideredAFullEmpirePlayerType_Safe() && AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "AssistAlliedFactories" ));
            if ( helpFactoriesOfAllies )
            {
                PlayerTypeData playerType = pFaction.Faction.PlayerTypeDataOrNull_ModeratelyExpensive;
                if ( playerType != null && !playerType.UsesMetal )
                    helpFactoriesOfAllies = false;
            }


            int debugCode = 1;
            try
            {
                foreach ( PlanetFaction faction in pFaction.RelatedFactions( FactionRelationship.Self.Add( FactionRelationship.FactionsIAmFriendlyTowards ) ) )
                {
                    if ( pFaction.Faction.Type == FactionType.Player && faction.Faction != pFaction.Faction )
                    {
                        if ( !helpFactoriesOfAllies ) //this is global for all players in MP, which seems fine
                        {
                            //countNoRepairAllies++;
                            continue;
                        }
                    }

                    foreach ( GameEntity_Squad assistable in faction.Entities.Squads( EntityRollupType.HasFactoryFlows ) )
                    {
                        if ( PassesInnerCheck_FactoryAssist( assistable ) )
                            lastPreCalculateFactoryAssistConstTargetsForPlanetFaction_Squads.Add( assistable );
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "PreCalculateFactoryAssistConstTargetsForPlanetFaction exception hit. debugCode " + debugCode + " exception " + e, Verbosity.DoNotShow );
            }

            //StringBuilder builder = new StringBuilder();
            //builder.Append( "Planet: " ).Append( pFaction.GetPlanetName_Safe() ).Append( " faction: " ).Append( " faction: " ).Append( pFaction.Faction.GetDisplayName() );
            //builder.Append( "\nAssistable: " ).Append( lastPreCalculateFactoryAssistConstTargetsForPlanetFaction_Squads.Count );
            //for ( int i = 0; i < lastPreCalculateFactoryAssistConstTargetsForPlanetFaction_Squads.Count; i++ )
            //    builder.Append( ", " ).Append( lastPreCalculateFactoryAssistConstTargetsForPlanetFaction_Squads[i].TypeData.DisplayName );

            //ArcenDebugging.ArcenDebugLogSingleLine( builder.ToString(), Verbosity.DoNotShow );
        }

        private bool PassesInnerCheck_FactoryAssist( GameEntity_Squad assistable )
        {
            if ( assistable.LastTimeSinceStartFWasFactoryConstructing < ArcenTime.TimeSinceStartF - 1f ) //if has not been factory constructing in the last second
                return false;
            if ( assistable.SecondsSpentAsRemains >= 0 || assistable.ToBeRemovedAtEndOfThisFrame || assistable.GetCurrentHullPoints() <= 0 )
                return false;
            if ( assistable.GetIsNonFunctional() )
                return false; //no assisting things that are nonfunctional
            return true;
        }
        #endregion

        private ArcenPoint MetalFlowLocus;
        private int MetalFlowRadius;
        
        private GameEntity_Squad metpln_bestTarget;
        private int metpln_priorityLevel;
        private int metpln_bestTargetDistance;
        private FInt metpln_bestNeededPercent;
        private PlannedMetalFlow help_plannedFlow;
        private MetalFlowPurpose help_flowPurpose;
        #region CheckClaimable
        private void CheckClaimable( GameEntity_Squad metalPlanEntity, GameEntity_Squad target )
        {
            if ( target == null )
                return;
            bool debugging = false;// claimable.Planet == Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
            //first very raw check, good for weeding out
            if ( !target.WorldLocation.GetHasAnyChanceOfBeingInRange( MetalFlowLocus, MetalFlowRadius ) )
                return;

            if ( metalPlanEntity.GetFactionTypeSafe() == FactionType.AI )
                return; // the AI can claim the distribution nodes, but nothing else

            int distance = metalPlanEntity.DataForMark.AssistRange > 0 ?
                metalPlanEntity.GetDistanceTo_ExpensiveAccurate( target, RadiusCheck.SubtractRadiiFromDistance, false )
                : 1000; //don't bother calculating distance for things with infinite range.  They don't care what they're touching
            if ( metpln_bestTarget != null && metalPlanEntity.Orders.Behavior == EntityBehaviorType.Attacker_Full )
            {
                if ( distance >= metpln_bestTargetDistance )
                    return;
            }

            int maxHull = target.GetMaxHullPoints();
            FInt percentLeft = maxHull <= 0 ? FInt.Zero : (FInt)( target.HullPointsLost - target.FramePlan_ExpectedRepairs ) / maxHull;
            if ( metpln_bestTarget != null )
            {
                if ( metpln_bestTarget.DataForMark.MetalCostToClaim < target.DataForMark.MetalCostToClaim )
                {
                    if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( target.TypeData.InternalName + ":e:" + metpln_bestTarget.TypeData.InternalName, Verbosity.DoNotShow );
                    return; // prefer less-expensive claimable
                }
                if ( metpln_bestTarget.DataForMark.MetalCostToClaim == target.DataForMark.MetalCostToClaim &&
                     percentLeft >= metpln_bestNeededPercent )
                {
                    if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( target.TypeData.InternalName + ":f:" + metpln_bestTarget.TypeData.InternalName, Verbosity.DoNotShow );
                    return; // prefer more-complete claimable
                }
                if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( target.TypeData.InternalName + ":g:" + metpln_bestTarget.TypeData.InternalName, Verbosity.DoNotShow );
            }
            else
            {
                if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( target.TypeData.InternalName + ":h", Verbosity.DoNotShow );
            }

            metpln_bestTarget = target;
            metpln_priorityLevel = 0;
            metpln_bestNeededPercent = percentLeft;
            metpln_bestTargetDistance = distance;
            return;
        }
        #endregion

        #region CheckRepairable
        private void CheckRepairable( GameEntity_Squad metalPlanEntity )
        {
            if ( metalPlanEntity == null )
                return;
            GameEntityTypeData.MarkLevelStats dataForMark = metalPlanEntity.DataForMark;
            if ( dataForMark == null )
                return;

            if ( dataForMark.AssistRange > 0 && metalPlanEntity.Orders.Behavior != EntityBehaviorType.Attacker_Full )
            {
                MetalFlowLocus = metalPlanEntity.WorldLocation;
                MetalFlowRadius = dataForMark.AssistRange;
            }
            else
            {
                MetalFlowLocus = metalPlanEntity.WorldLocation;
                MetalFlowRadius = 99999999;
            }
            List<SafeSquadWrapper> squadsToCheck = null;
            switch ( help_flowPurpose )
            {
                case MetalFlowPurpose.RepairingHullsOfFriendlies:
                    squadsToCheck = lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Hull;
                    break;
                case MetalFlowPurpose.RepairingShieldsOfFriendlies:
                    squadsToCheck = lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Shields;
                    break;
                case MetalFlowPurpose.RepairingEnginesOfFriendlies:
                    squadsToCheck = lastPreCalculateAllRepairableTargetsForPlanetFaction_Squads_Engines;
                    break;
            }

            for ( int i = 0; i < squadsToCheck.Count; i++ )
                CheckRepairable_Inner( metalPlanEntity, squadsToCheck[i].GetSquad() );
        }
        #endregion

        #region CheckRepairable_Inner
        private void CheckRepairable_Inner( GameEntity_Squad metalPlanEntity, GameEntity_Squad target )
        {
            if ( target == null )
                return; //it died
            if ( target.SelfBuildingMetalRemaining > FInt.Zero )
                return; //if still self-building, don't repair
            if ( target.HasNotYetBeenFullyClaimed )
                return;

            #region Tracing
            bool tracing = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Repairs ) && (
                               (target == GameEntity_Base.CurrentlyHoveredOver) ||
                               (metalPlanEntity == GameEntity_Base.CurrentlyHoveredOver)
                );
            ArcenCharacterBuffer traceBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "MetalFlows-CheckRepairable_Inner-trace", 10f ) : null;
            if ( tracing ) traceBuffer.Add( "\n" ).Add( "Considering " ).Add( EnumNameCache.GetName( help_flowPurpose ) ).Add( " for " ).Add( target.TypeData.InternalName ).Add(" From: " + metalPlanEntity.TypeData.InternalName + " " + metalPlanEntity.PrimaryKeyID);
            #endregion

            bool isCrippledFlagship = false;
            if ( target.TypeData.IsCrippledInsteadOfDying &&
                 target.GetIsCrippled() )//|| possibleTarget.GetIsNonfunctionalForAnotherReason() ) don't care about nonfunctional here
                isCrippledFlagship = true;

            bool isCommandStation = target.TypeData.IsCommandStation;

            bool isCommandStationOrCrippledFlagship = isCommandStation || isCrippledFlagship;

            //possibleTarget.DebugText = "PossibleTargetForRepair: " + DateTime.Now + "\n";

            int currentLost;
            int max;
            switch ( help_flowPurpose )
            {
                case MetalFlowPurpose.RepairingHullsOfFriendlies:
                    currentLost = target.HullPointsLost;
                    max = target.GetMaxHullPoints();
                    if ( target.HasNotYetBeenFullyClaimed )
                        currentLost = max;
                    break;
                case MetalFlowPurpose.RepairingShieldsOfFriendlies:
                    currentLost = target.ShieldPointsLost;
                    max = target.GetMaxShieldPoints();
                    break;
                case MetalFlowPurpose.RepairingEnginesOfFriendlies:
                    currentLost = target.CurrentEngineStunSeconds;
                    max = ExternalConstants.Instance.MaxEngineStun;
                    break;
                default:
                    {
                        //possibleTarget.DebugText += "exit 3";
                        return;
                    }
            }
            if ( currentLost <= 0 )
            {
                //possibleTarget.DebugText += "exit 2";
                #region Tracing
                if ( tracing ) traceBuffer.Add( ":" ).Add( "Skipping due to currentLost <= 0" );
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( traceBuffer.ToString(), Verbosity.Chat );
                    traceBuffer.ReturnToPool();
                    traceBuffer = null;
                }
                #endregion
                return; //we already checked this, but let's check again just to be sure
            }

            GameEntity_Squad assister = metalPlanEntity;
            bool assisterIsInFullPursuitMode = assister.Orders.Behavior == EntityBehaviorType.Attacker_Full;
            int distance = assister.DataForMark.AssistRange > 0 ?
                assister.GetDistanceTo_ExpensiveAccurate( target, RadiusCheck.SubtractRadiiFromDistance, false )
                : 1000; //don't bother calculating distance for things with infinite range.  They don't care what they're touching

            if ( !assisterIsInFullPursuitMode && assister.DataForMark.AssistRange > 0 )
            {
                if ( distance >= assister.DataForMark.AssistRange )
                    return; //if we're not in pursuit mode and this is outside of the assist range, and this does not have infinite range, then skip it
            }

            //prioritize command stations with nothing  set
            if ( isCommandStationOrCrippledFlagship &&
                 metalPlanEntity.Orders.Behavior == EntityBehaviorType.Attacker_Full )
            {
                metpln_bestTarget = target;
                metpln_priorityLevel = 100;
                metpln_bestNeededPercent = FInt.Smallest;
                metpln_bestTargetDistance = distance;
                #region Tracing
                if ( tracing ) traceBuffer.Add( ":" ).Add( "Auto-accepting with priority=100 due to crippled-flagship-with-no-assistors rule" );
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( traceBuffer.ToString(), Verbosity.Chat );
                    traceBuffer.ReturnToPool();
                    traceBuffer = null;
                }
                #endregion
                return;
            }

            FInt percentLeft = max <= 0 ? FInt.One : (FInt)(currentLost - target.FramePlan_ExpectedRepairs) / max;

            if ( metpln_bestTarget == null )
            {
                metpln_bestTarget = target;
                metpln_bestTargetDistance = distance;
                metpln_bestNeededPercent = percentLeft;
                metpln_priorityLevel = 0;
                //Entity.DebugText += "first const or rebuild";
            }
            else
            {
                if ( distance < metpln_bestTargetDistance )
                {
                    //Entity.DebugText += "closest of highest priority of const or rebuild";
                    metpln_bestTarget = target; //choose the closest
                    metpln_bestTargetDistance = distance;
                    metpln_bestNeededPercent = percentLeft;
                    metpln_priorityLevel = 0;
                }
            }

            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( traceBuffer.ToString(), Verbosity.Chat );
                traceBuffer.ReturnToPool();
                traceBuffer = null;
            }
        }
        #endregion CheckRepairable_Inner

        #region CheckRemainsRebuildable
        private void CheckRemainsRebuildable( GameEntity_Squad target )
        {
            if ( target == null )
                return;
            //first very raw check, good for weeding out
            if ( !target.WorldLocation.GetHasAnyChanceOfBeingInRange( MetalFlowLocus, MetalFlowRadius ) )
                return;

            FInt neededPercent = (FInt)target.SecondsSpentAsRemains;
            if ( metpln_bestTarget != null && neededPercent <= metpln_bestNeededPercent )
                return;
            
            metpln_bestTarget = target;
            metpln_priorityLevel = 0;
            metpln_bestNeededPercent = neededPercent;
            metpln_bestTargetDistance = 0;
        }
        #endregion

        #region CheckSelfConstructionAssistable
        private void CheckSelfConstructionAssistable( GameEntity_Squad metalPlanEntity, GameEntity_Squad target )
        {
            if ( target == null )
                return;
            bool assisterIsInFullPursuitMode = metalPlanEntity.Orders.Behavior == EntityBehaviorType.Attacker_Full;
            int distance = metalPlanEntity.DataForMark.AssistRange > 0 ?
                metalPlanEntity.GetDistanceTo_ExpensiveAccurate( target, RadiusCheck.SubtractRadiiFromDistance, false ) 
                : 1000; //don't bother calculating distance for things with infinite range.  They don't care what they're touching

            if ( !assisterIsInFullPursuitMode && metalPlanEntity.DataForMark.AssistRange > 0 )
            {                
                if ( distance >= metalPlanEntity.DataForMark.AssistRange )
                    return; //if we're not in pursuit mode and this is outside of the assist range, and this does not have infinite range, then skip it
            }

            if ( metpln_bestTarget == null )
            {
                metpln_bestTarget = target;
                metpln_bestTargetDistance = distance;
                //Entity.DebugText += "first const or rebuild";
            }
            else
            {
                if ( target.TypeData.ConstructionPriority > metpln_bestTarget.TypeData.ConstructionPriority )
                {
                    //Entity.DebugText += "highest priority of const or rebuild";
                    metpln_bestTarget = target; //if a higher construction priority, then choose that
                    metpln_bestTargetDistance = distance;
                }
                else if ( target.TypeData.ConstructionPriority == metpln_bestTarget.TypeData.ConstructionPriority )
                {
                    if ( distance < metpln_bestTargetDistance )
                    {
                        //Entity.DebugText += "closest of highest priority of const or rebuild";
                        metpln_bestTarget = target; //if a equal construction priority, then choose the closest
                        metpln_bestTargetDistance = distance;
                    }
                }
            }

            metpln_priorityLevel = 0;
        }
        #endregion

        #region CheckFactoryConstructionAssistable
        private void CheckFactoryConstructionAssistable( GameEntity_Squad metalPlanEntity, GameEntity_Squad target )
        {
            if ( target == null )
                return;
            bool assisterIsInFullPursuitMode = metalPlanEntity.Orders.Behavior == EntityBehaviorType.Attacker_Full;
            int distance = metalPlanEntity.DataForMark.AssistRange > 0 ?
                metalPlanEntity.GetDistanceTo_ExpensiveAccurate( target, RadiusCheck.SubtractRadiiFromDistance, false )
                : 1000; //don't bother calculating distance for things with infinite range.  They don't care what they're touching

            if ( !assisterIsInFullPursuitMode && metalPlanEntity.DataForMark.AssistRange > 0 )
            {
                if ( distance >= metalPlanEntity.DataForMark.AssistRange )
                    return; //if we're not in pursuit mode and this is outside of the assist range, and this does not have infinite range, then skip it
            }

            if ( metpln_bestTarget == null )
            {
                metpln_bestTarget = target;
                metpln_bestTargetDistance = distance;
                //Entity.DebugText += "first const or rebuild";
            }
            else
            {
                if ( distance < metpln_bestTargetDistance )
                {
                    metpln_bestTarget = target; //choose the closest, since we're just helping the factory, not build a thing
                    metpln_bestTargetDistance = distance;
                }
            }

            metpln_priorityLevel = 0;
        }
        #endregion
    }
}
