using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public partial class MovementPlanning : ArcenShortTermPlanningContext, IBetweenMapGenPoolable<MovementPlanning>
    {
        private static ReferenceTracker RefTracker;
        private MovementPlanning()
            : base( "_ST.MovementPlanning" )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "MovementPlanning" );
            RefTracker.IncrementObjectCount();
        }

        public static int Accumulator_MOB = 0;
        public static int Accumulator_MOF = 0;

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            movementDebugLogger.Clear();

            Accumulator_MOB = 0;
            Accumulator_MOF = 0;
        }

        private static readonly BetweenMapGenPool<MovementPlanning> Pool = BetweenMapGenPool<MovementPlanning>.Create_WillNeverBeGCed( "MovementPlanning", 30, 10,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new MovementPlanning(); } );

        public static MovementPlanning GetFromPoolOrCreate()
        {
            MovementPlanning context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion

        public readonly ArcenGameDebugLogger movementDebugLogger = new ArcenGameDebugLogger( "MovementPlanning" );

        private const int OVERALL_METADATA = 0;
        private const int PLANET_INFO = 1;
        private const int PER_SHIP_INFO = 2;

        private ThreadingExchanger IsCurrentlyWaitingOnThread = new ThreadingExchanger( "MovementPlanning", 30f );

        public override void Execute()
        {
            if ( CentralVars.DEBUG_TURN_OFF_SHORT_TERM_PLANNNING_ALL )
                return;
            if ( CentralVars.DEBUG_TURN_OFF_MOVEMENT_PLANNING )
                return;
            if ( World.Instance.IsPaused )
                return;
            if ( IsCurrentlyWaitingOnThread.IsBusy() )
                return;
            if ( !IsCurrentlyWaitingOnThread.DoNextOnlyIfNotAlreadyBusy( string.Empty ) )
                return;

            bool tracing = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Movement );
            try
            {
                Interlocked.Increment( ref Accumulator_MOB );

                if ( tracing )
                {
                    movementDebugLogger.Log( OVERALL_METADATA, 0, "World Frame: " + World_AIW2.Instance.Network_CurrentFrameNumber + " World Seed: " + World_AIW2.Instance.Setup.MapConfig.Seed );
                    movementDebugLogger.Log( OVERALL_METADATA, 1, "StartingRand Next: " + this.RandomToUse.Next() + " Seed: " + this.RandomToUse.GetCurrentSeed() );
                }

                this.PlanMovement_Starting();
                
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( tracing )
                        movementDebugLogger.Log( PLANET_INFO, planet.Index, planet.Name + " Process " + planet.BattleStatus_ProcessThisSimStep + " Status: " + planet.BattleStatus + " " + planet.RandomSeed );
                    if ( !planet.BattleStatus_ProcessThisSimStep )
                        continue; //skip this step
                    
                    this.PlanMovement_ForPlanet_Starting(planet);

                    foreach ( GameEntity_Squad _mv_sq in planet.Squads() )
                        PlanMovementForEntity_Squad_Multithreaded( _mv_sq );
                    foreach ( GameEntity_Shot _mv_sh in planet.Shots() )
                        PlanMovementForEntity_Shot_Multithreaded( _mv_sh );

                    this.PlanMovement_ForPlanet_Ending(planet);
                }

                this.PlanMovement_Ending();
                
                if ( tracing )
                {
                    movementDebugLogger.Log( OVERALL_METADATA, 2, "EndingRand Next: " + this.RandomToUse.Next() + " Seed: " + this.RandomToUse.GetCurrentSeed() );
                    movementDebugLogger.DumpToDiskAndClearForNext( "Frame" + World_AIW2.Instance.Network_CurrentFrameNumber );
                }

                Interlocked.Increment( ref Accumulator_MOF );
                IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
            }
            catch ( Exception e )
            {
                IsCurrentlyWaitingOnThread.MarkAsNoLongerBusy();
                ArcenDebugging.ArcenDebugLog( "Exception in MovementPlanning.Execute:" + e.ToString(), Verbosity.ShowAsError );
            }
        }

        private DelReturn PlanMovementForEntity_Squad_Multithreaded( GameEntity_Squad entity )
        {
            if ( entity == null )
                return DelReturn.Continue;
            
            var planet = entity.Planet;
            if ( planet == null )
                return DelReturn.Continue;
            
            var typeData = entity.TypeData;
            if ( typeData == null )
                return DelReturn.Continue;
            
            var markData = entity.DataForMark;
            if ( markData == null )
                return DelReturn.Continue;

            entity.FramePlan_DoMove = false;
            entity.FramePlan_Move_TouchedTarget = false;
            entity._FramePlan_Move_NextMovePoint = ArcenPoint.ZeroZeroPoint;
            entity.WorkingIndex_OnlyWriteFromMovementPlanning = planet.Index;
            entity.SetWorkingDestination(ArcenPoint.ZeroZeroPoint);
            
            var Speed = entity.CalculatedSpeed;
            var EffectiveDeltaTime = planet.LocalDeltaTime_UnpausedOnly;

            int locationSetFrom = 0;
            string extraDebugInfo = string.Empty;
            string extraDebugInfo2 = string.Empty;
            int debugStage = 0;
            bool tracing = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Movement );
            try
            {
                //if this can NEVER move
                if ( !typeData.IsMobile )
                {
                    entity.SetWorkingDestination(ArcenPoint.ZeroZeroPoint);
                    if ( tracing ) extraDebugInfo = " !IsMobile ";
                    return DelReturn.Continue;
                }
                
                Inner_HandleSquadKnockback( entity, planet );
                var targetDest = entity.WorldLocation;
                
                //these can't be given orders, either!
                if ( typeData.IsMobileOrbiter && string.IsNullOrEmpty(typeData.OrbitsParentUnlessParentSystemHasTarget) )
                {
                    entity.SetWorkingDestination(ArcenPoint.ZeroZeroPoint);
                    if ( tracing ) extraDebugInfo = " IsMobileOrbiter ";
                    return DelReturn.Continue;
                }

                //if ( World_AIW2.CurrentSimCycleMove != entity.SimCycleGroup_Move )
                //    return DelReturn.Continue; //catch you next time!  Spread out this thinking over frames

                debugStage = 1000;
                if ( Speed <= 0 )
                {
                    locationSetFrom = 10;
                    entity.SetWorkingDestination(ArcenPoint.ZeroZeroPoint);
                    if ( tracing ) extraDebugInfo = " speedLessThan0 ";
                    return DelReturn.Continue;
                }

                if ( tracing ) extraDebugInfo2 += "sA";
                
                var requestedOffset = ArcenPoint.ZeroZeroPoint;
                
                debugStage = 3000;
                if ( entity.DecollisionMoveTarget.X != 0 ) 
                {
                    //overridden by DecollisionMoveTarget
                    debugStage = 4000;
                    targetDest = entity.DecollisionMoveTarget;
                    if ( tracing ) extraDebugInfo2 += "sD";
                }
                else
                {
                    debugStage = 5000;
                    var currentOrder = entity.ForShortTermPlanning_CurrentValidOrder;
                    if ( currentOrder.TypeData == null )
                    {
                        locationSetFrom = 20;
                        entity.SetWorkingDestination(ArcenPoint.ZeroZeroPoint);
                        if ( tracing ) extraDebugInfo = " currentOrderNull ";
                        
                        return DelReturn.Continue;
                    }
                    
                    var orderType = currentOrder.TypeData;
                    if ( orderType == null  )
                    {
                        locationSetFrom = 30;
                        entity.SetWorkingDestination(ArcenPoint.ZeroZeroPoint);
                        if ( tracing ) extraDebugInfo = " currentOrderNull ";
                        
                        return DelReturn.Continue;
                    }

                    debugStage = 6000;
                    
                    // Stop-to-shoot only works when we're not getting into a transport.
                    // Also note that the transport could be on a different planet and current order is to take a wormhole.
                    if ( entity.StopToShootAnySeenTargets && 
                         !entity.GetIsNonFlagshipInLoadingModeMode() &&
                         (currentOrder.TypeData == null || orderType.Type != EntityOrderType.GetIntoTransport) )
                    {
                        for ( int i = 0; i < entity.Systems.Count; i++ )
                        {
                            EntitySystem system = entity.Systems[i];
                            
                            if ( system.CountOfPotentialTargetsByPriorityForSystem() > 0 && 
                                 (World_AIW2.Instance.GameSecond - system.LastGameSecondIFiredShot) < 10 && 
                                 system.LastShotFireAbortCode == 0 )
                            {
                                locationSetFrom = 30;
                                if ( tracing )
                                {
                                    extraDebugInfo = " abortcode " + system.LastShotFireAbortCode + 
                                        " lastfired " + system.LastGameSecondIFiredShot + 
                                        " lastfiredago " + (World_AIW2.Instance.GameSecond - system.LastGameSecondIFiredShot);
                                }
                                
                                entity.SetWorkingDestination(ArcenPoint.ZeroZeroPoint);
                                
                                return DelReturn.Continue;
                            }
                        }
                    }
                    
                    debugStage = 8000;
                    GameEntity_Squad targetEnemyEntity = null;
                    GameEntity_Squad targetAllyEntity = null;
                    bool use_assist_range = true;
                    bool approach_for_max_damage = false;
                    
                    #region Assign, from EntityOrderType
                    switch ( orderType.Type )
                    {
                        case EntityOrderType.Move_Normal:
                        case EntityOrderType.Move_Decollision:
                            {
                                debugStage = 8100;
                                targetDest = currentOrder.RelatedPoint;
                                if ( tracing ) extraDebugInfo2 += "sM";
                                break;
                            }
                        case EntityOrderType.Attack:
                            {
                                debugStage = 8200;
                                targetEnemyEntity = currentOrder.RelatedSquad.GetSquad();
                                requestedOffset = currentOrder.RelatedPoint;
                                approach_for_max_damage = true;
                                /*
                                if (currentOrder.Source == OrderSource.HumanPlayer || 
                                    !entity.GetMayBeGivenOrders(UnitOrdersFromPlayerType.Movement))
                                {
                                    approach_for_max_damage = true;
                                }
                                */
                                break;
                            }
                        case EntityOrderType.Assist:
                            {
                                debugStage = 8300;
                                targetAllyEntity = currentOrder.RelatedSquad.GetSquad();
                                break;
                            }
                        case EntityOrderType.GetIntoTransport:
                            {
                                debugStage = 8400;
                                targetAllyEntity = currentOrder.RelatedSquad.GetSquad();
                                use_assist_range = false;
                                break;
                            }
                        case EntityOrderType.Wormhole:
                            {
                                debugStage = 9000;
                                bool debug = false;
                                //if ( entity.GetSpecialFactionIsConsideredHunter_Safe() )
                                //    debug = true;
                                GameEntity_Other wormhole = currentOrder.GetWormholeToOtherPlanetOrNull( planet );
                                if ( wormhole == null || 
                                     (!currentOrder.RefuseToWait && 
                                      entity.WaitingAgainstPlanetIndex != -1 && 
                                      entity.GetSecondsSinceEnteringThisPlanet() > 1) )
                                {
                                    if ( debug )
                                    {
                                        LOG.Msg("entity {0} ; wormhole={1}, WaitingAgainstPlanetIndex={2}, order.RefuseToWait={3}", 
                                                wormhole.Exists(), entity.ToStringWithPlanet(), entity.WaitingAgainstPlanetIndex, currentOrder.RefuseToWait);
                                    }
                                    
                                    //if we can't find a wormhole, or if we are waiting and don't have a 'refuse to wait' override
                                    ArcenPoint pointToWaitAt;
                                    GameEntity_Squad commandStation = planet.GetCommandStationOrNull();
                                    if ( commandStation == null )
                                        pointToWaitAt = Engine_AIW2.Instance.CombatCenter;
                                    else
                                        pointToWaitAt = commandStation.WorldLocation;
                                    int maxDistance = ExternalConstants.Instance.WaitingBeforeWormholeDistance - markData.Radius;
                                    int minDistance = maxDistance >> 1;
                                    MersenneTwister randIfNeeded = new MersenneTwister( entity.PrimaryKeyID );
                                    pointToWaitAt = pointToWaitAt.GetRandomPointWithinDistance( randIfNeeded, minDistance, maxDistance );
                                    int threshold = markData.Radius << 1;
                                    if ( entity.GetIsWithinRangeOf( pointToWaitAt, threshold ) )
                                        pointToWaitAt = entity.WorldLocation; // close enough, we're good
                                    targetDest = pointToWaitAt;
                                    if ( tracing )
                                        extraDebugInfo2 += "sW1";
                                }
                                else
                                {
                                    //When flying to a wormhole, don't go directly to the wormhole. Instead fly to a random spot around the wormhole that's within range for you to
                                    //take the wormhole. The goal is to make ships to less of a straight conga line, which doesn't look great with the unit overlap
                                    GameEntityTypeData.MarkLevelStats myDataForMark = markData;
                                    int maxDist = (int)myDataForMark.CalculateRadiusForDisplay( planet ) + (int)wormhole.TypeData.BaseMark.Radius;
                                    //targetDest = wormhole.WorldLocation; //orig path
                                    targetDest = Mat.GetPointFromCircleCenter( wormhole.WorldLocation, maxDist, AngleDegrees.Create( entity.PrimaryKeyID % 360 ) );

                                    if ( tracing )
                                        extraDebugInfo2 += "sW2";
                                }
                                requestedOffset = currentOrder.RelatedPoint;
                                break;
                            }
                        case EntityOrderType.Custom:
                            {
                                var squad = currentOrder.RelatedSquad.GetSquad();
                                if (squad != null)
                                {
                                    if (squad.GetIsHostileTowards_Safe(entity.GetFactionOrNull_Safe()))
                                        targetEnemyEntity = squad;
                                    else
                                        targetAllyEntity = squad;
                                    
                                    requestedOffset = currentOrder.RelatedPoint;
                                }
                                else
                                {
                                    targetDest = currentOrder.RelatedPoint;
                                }
                                
                                if (tracing) extraDebugInfo2 += "sC";
                                break;
                            }
                        default:
                            {
                                if (tracing) extraDebugInfo2 += "sDef";
                                break;
                            }
                    }
                    #endregion
                    
                    // If we have our enemy target in a tractor hold (a regular one, not a reverse tractor)
                    // then do not use it as our kite-target, since we won't ever get any closer.
                    debugStage = 11000;
                    if ( targetEnemyEntity != null && 
                         !entity.TypeData.OriginalXmlData.GetBool("custom_KiteEvenTractored", false) )
                    {
                        if ( targetEnemyEntity.CurrentTractorSourcesHittingThis.DisplayContains(entity.PrimaryKeyID) == true ||
                             entity.OtherEntityPullingMeByMyOwnReverseTractor.Display == targetEnemyEntity )
                        {
                            if ( tracing ) extraDebugInfo2 += "sEnTT " + targetDest;
                            targetEnemyEntity = null;
                        }
                    }
                    
                    debugStage = 11050;
                    if ( targetEnemyEntity != null )
                    {
                        debugStage = 11100;
                        int distanceAfterWhichShipsWillNotChaseTargets = ExternalConstants.Instance.Balance_DistanceAfterWhichShipsWillNotChaseTargets;
                        
                        if (AIWar2GalaxySettingQuickAccess.IntelligentPursuit) 
                        {
                            targetDest = AimAhead(entity, targetEnemyEntity);
                            if ( tracing ) extraDebugInfo2 += "sEnIP " + targetDest;
                        }
                        else 
                        {
                            targetDest = targetEnemyEntity.WorldLocation;
                            if ( tracing ) extraDebugInfo2 += "sEn1 " + targetDest;
                        }
                        
                        debugStage = 11200;
                        if ( entity.Systems.Count == 0 )
                        {
                            // We have no systems but were told to attack, do nothing.
                            debugStage = 18100;
                            entity.SetWorkingDestination(ArcenPoint.ZeroZeroPoint);
                            locationSetFrom = 70;
                            if ( tracing )
                                extraDebugInfo = " nosystems?";

                            return DelReturn.Continue;
                        }
                        
                        int prefDistance = int.MaxValue;
                        int highestKitePriority = -1;
                        EntitySystem kiteSystem = null;

                        EntitySystem system;
                        EntitySystemTypeData systemData;
                        EntitySystemTypeData.MarkLevelStats markDataSys;
                        
                        void AssignBestKiteDist()
                        {
                            debugStage = 11300;
                            if ( typeData.ExplicitKiteDistance > 0 )
                            {
                                prefDistance = typeData.ExplicitKiteDistance;
                                if ( tracing )
                                    extraDebugInfo2 += "ekiteDistance";

                                return;
                            }

                            debugStage = 12100;

                            for ( int i = 0; i < entity.Systems.Count; i++ )
                            {
                                system = entity.Systems[i];
                                systemData = system.TypeData;
                                
                                if ( systemData == null )
                                    continue;
                                
                                markDataSys = system.DataForMark;
                                if ( markData == null )
                                    continue;
                                
                                if (systemData.OnlyFiresOnDeath)
                                    continue;
                                
                                if (systemData.FiringTiming == FiringTiming.Never ||
                                    systemData.FiringTiming == FiringTiming.WhenParentEntityHit)
                                {
                                    continue;
                                }

                                if ( systemData.PriorityForKiting == 0)
                                {
                                    if ( tracing ) extraDebugInfo2 += " " + systemData.InternalName_Original + " 0 priority ";
                                    
                                    continue;
                                }

                                var disabledReason = system.ComputeDisabledReason(CheckReload:systemData.OnlyKiteIfReadyToFire);
                                if ( disabledReason != ArcenRejectionReason.Unknown )
                                {
                                    if ( tracing ) extraDebugInfo2 += " " + systemData.InternalName_Original + " " + Extensions.ToString(disabledReason);
                                    
                                    continue;
                                }

                                if ( systemData.ShotTypeData == null )
                                    continue;

                                //if (systemData.OnlyKiteIfValidTarget)
                                {
                                    if ( !system.GetIsTargetValid( targetEnemyEntity ) )
                                    {
                                        if ( tracing ) extraDebugInfo2 += " " + systemData.InternalName_Original + " target not valid ";
                                        continue;
                                    }
                                }
                                
                                if ( systemData.PriorityForKiting < highestKitePriority )
                                {
                                    if ( tracing ) extraDebugInfo2 += " " + systemData.InternalName_Original + " low priority ";
                                    continue;
                                }

                                // thats all the reasons to completely skip a system
                                //
                                // now we want to pick a kiting distance for the one highest priority
                                // with the shortest range
                                
                                int dist = system.GetRangeToShoot(targetEnemyEntity, approach_for_max_damage);
                                if (dist < 0)
                                    continue;
                                
                                if ( systemData.PriorityForKiting > highestKitePriority )
                                {
                                    highestKitePriority = systemData.PriorityForKiting;
                                    prefDistance = int.MaxValue;
                                    kiteSystem = null;
                                }

                                if ( dist < prefDistance )
                                {
                                    kiteSystem = system;
                                    prefDistance = dist;
                                }
                            }
                        }

                        AssignBestKiteDist();
                        
                        entity.MovementPlanning_LastKiteTarget.Set(targetEnemyEntity);
                        entity.MovementPlanning_LastKiteSystem = kiteSystem;
                        entity.MovementPlanning_LastPreferredKiteDist = prefDistance;
                        
                        debugStage = 13100;
                        if (kiteSystem == null && prefDistance == int.MaxValue)
                        {
                            debugStage = 13110;
                            
                            // We have only non-damaging systems like tachyon/tractor, do nothing.
                            // To Do (?): Make this code instead cause ships to kite just within their passive system range.
                            // debugStage = 13110; 
                            entity.SetWorkingDestination(ArcenPoint.ZeroZeroPoint);
                            locationSetFrom = 40;
                            if ( tracing ) extraDebugInfo = " noKiteSystem";
                            
                            return DelReturn.Continue;
                        }
                        
                        /*
                        else
                        if ( kiteSystem.TypeData.IsMelee )
                        {
                            locationSetFrom = 41;
                            if ( tracing )
                                extraDebugInfo = " meleeKiteSystem";
                            
                            debugStage = 13120;
                            if ( targetEnemyEntity.FramePlan_Move_NextMovePoint != ArcenPoint.ZeroZeroPoint )
                            {
                                targetDest = targetEnemyEntity.FramePlan_Move_NextMovePoint;
                                if ( tracing )
                                    extraDebugInfo2 += "sMe1";
                            }
                            else
                            {
                                targetDest = targetEnemyEntity.WorldLocation;
                                if ( tracing )
                                    extraDebugInfo2 += "sMe2";
                            }
                        }
                        */
                        
                        if (prefDistance >= (planet.GravWellSize.DistanceScale_GravwellRadius*2))
                        {
                            locationSetFrom = 42;
                            if ( tracing ) extraDebugInfo = " sniperKiteSystem";
                            
                            debugStage = 13130;
                            entity.SetWorkingDestination(ArcenPoint.ZeroZeroPoint);
                            
                            return DelReturn.Continue;
                        }
                        
                        debugStage = 15203;
                        int kiteDistance;
                        int stopDistance;
                        int approachDistance;
                        int target_radius = Mathf.Max(targetEnemyEntity.CalculatedCurrentShieldRadius, targetEnemyEntity.GetRadius());
                        int currentDistance = Mat.DistanceBetweenPointsSIMD( entity.WorldLocation, targetDest );
                        
                        if ((prefDistance - target_radius) < 1000)
                        {
                            debugStage = 15204;
                            
                            // this is a very small distance range we need to be in -- ie, a melee range weapon system
                            // this starts having problems being 'in range to shoot' which uses an approximation for the distance calc
                            // so to compensate for that, our kite distance needs to be recessed farther than the normal 90% ratio
                            kiteDistance = (((prefDistance - target_radius) * 7) / 10) + target_radius;
                            approachDistance = (((prefDistance - target_radius) * 6) / 10) + target_radius;
                            stopDistance = (((prefDistance - target_radius) * 5) / 10) + target_radius;
                        }
                        else
                        {
                            debugStage = 15205;
                            kiteDistance = (((prefDistance - target_radius) * 9) / 10) + target_radius;
                            approachDistance = (((prefDistance - target_radius) * 8) / 10) + target_radius;
                            stopDistance = (((prefDistance - target_radius) * 7) / 10) + target_radius;
                        }
                        
                        debugStage = 15210;
                        if ( tracing ) extraDebugInfo2 += $" mypos={entity.WorldLocation} targetpos={targetDest} dist={currentDistance} kitesystem='{((kiteSystem?.TypeData.DisplayName)??"null")}' prefDistance={prefDistance} kiteDistance={kiteDistance} approachDistance={approachDistance} stopDistance={stopDistance} ";

                        
                        if ( currentDistance > stopDistance && 
                             currentDistance < approachDistance )
                        {
                            //we are between stop (close) and approach (far) distance, so this is a good spot to just stand and shoot
                            //without this, units could say "Oh, we're too close, lets move away", then "Oh, we're too far, lets get close" over and over
                            
                            debugStage = 15500;
                            entity.SetWorkingDestination(ArcenPoint.ZeroZeroPoint);
                            
                            return DelReturn.Continue;
                        }
                        
                        if ( currentDistance <= stopDistance )
                        {
                            debugStage = 16100;
                            
                            // some factions/ships do not back away once in range, only approach
                            var facOrNull = entity.GetFactionOrNull_Safe();
                            if ( facOrNull == null || 
                                 !facOrNull.UnitsAutoKite_NeverSetDirectlyOrItBreaksEverything || 
                                 typeData.DisallowKiting )
                            {
                                entity.SetWorkingDestination(ArcenPoint.ZeroZeroPoint);
                                locationSetFrom = 65;
                                if ( tracing ) extraDebugInfo += " autokite " + (facOrNull == null ? "null" : facOrNull.UnitsAutoKite_NeverSetDirectlyOrItBreaksEverything + "" ) +
                                                                 " disallowKiting " + typeData.DisallowKiting;

                                return DelReturn.Continue;
                            }
                            
                            //try to stay at the highest range
                            AngleDegrees currentAngle = targetDest.GetAngleToDegrees( entity.WorldLocation );
                            targetDest = targetDest.GetPointAtAngleAndDistance( currentAngle, stopDistance );
                            if ( tracing ) extraDebugInfo2 += "sEn2 " + targetDest;
                        }
                        
                        if ( currentDistance > approachDistance )
                        {
                            //Now make sure that we are heading to a point where we can hit the target from our appropriate kiting distance
                            debugStage = 17100;
                            AngleDegrees currentAngle = targetDest.GetAngleToDegrees( entity.WorldLocation );
                            targetDest = targetDest.GetPointAtAngleAndDistance( currentAngle, approachDistance );
                            if ( tracing ) extraDebugInfo2 += "sEn3B " + targetDest;
                        }
                    }

                    debugStage = 20100;
                    if ( targetAllyEntity != null )
                    {
                        debugStage = 20200;

                        if (AIWar2GalaxySettingQuickAccess.IntelligentPursuit)
                        {
                            targetDest = AimAhead(entity, targetAllyEntity);
                            if ( tracing ) extraDebugInfo2 += "sAlIP " + targetDest;
                        }
                        else
                        {
                            targetDest = targetAllyEntity.WorldLocation;
                            if ( tracing ) extraDebugInfo2 += "sAl1 " + targetDest;
                        }

                        int stopDistance = use_assist_range ? (entity.DataForMark.AssistRange * 6) / 10 : 0;
                        int currentDistance = Mat.DistanceBetweenPointsSIMD( entity.WorldLocation, targetDest );
                        if ( currentDistance <= stopDistance )
                        {
                            entity.SetWorkingDestination(ArcenPoint.ZeroZeroPoint);
                            locationSetFrom = 80;
                            //if ( tracing )
                            //    extraDebugInfo = " currentDistance " + currentDistance +
                            //        " stopDistance " + stopDistance;
                            //no kiting from assisting units!
                            return DelReturn.Continue;
                        }
                        
                        int approachDistance = use_assist_range ? (entity.DataForMark.AssistRange * 8) / 10 : 0;
                        AngleDegrees currentAngle = targetDest.GetAngleToDegrees( entity.WorldLocation );
                        targetDest = targetDest.GetPointAtAngleAndDistance( currentAngle, approachDistance );
                        if ( tracing ) extraDebugInfo2 += "sAl2 " + targetDest;
                    }
                }

                debugStage = 21100;
                ArcenPoint offsetTargetDest = targetDest + requestedOffset;
                
                // this has already been applied, but left variable in
                ArcenPoint KnockbackOffset = ArcenPoint.ZeroZeroPoint;
                
                debugStage = 22100;
                int distanceToTarget = entity.WorldLocation.GetDistanceTo( offsetTargetDest-KnockbackOffset, false );
                entity.SetWorkingDestination(offsetTargetDest);

                debugStage = 22300;
                locationSetFrom = 100;
                if ( tracing )
                    extraDebugInfo = " targetDest " + targetDest +
                        " requestedOffset " + requestedOffset +
                        " knockbackOffset " + KnockbackOffset +
                        " distanceToTarget " + distanceToTarget +
                        " EffectiveDeltaTime " + EffectiveDeltaTime.ToDouble();

                debugStage = 22400;
                int distanceToMove;
                if ( EffectiveDeltaTime <= FInt.Zero )
                {
                    debugStage = 22500;
                    distanceToMove = 0;
                    entity.SetNextMovePoint(entity.WorldLocation + KnockbackOffset);
                    entity.DecollisionMoveTarget = ArcenPoint.ZeroZeroPoint;
                }
                else
                {
                    debugStage = 22600;

                    distanceToMove = (Speed * EffectiveDeltaTime).IntValue;
                    if ( distanceToMove >= distanceToTarget )
                    {
                        debugStage = 23100;
                        entity.SetNextMovePoint( offsetTargetDest + KnockbackOffset );
                        entity.DecollisionMoveTarget = ArcenPoint.ZeroZeroPoint;
                    }
                    else
                    {
                        debugStage = 23500;
                        entity.SetNextMovePoint( entity.WorldLocation.GetPointTowardsOther( offsetTargetDest, distanceToMove, distanceToTarget ) + KnockbackOffset );
                    }
                }

                debugStage = 24100;
                entity.FramePlan_DoMove = (entity.WorldLocation != entity.FramePlan_Move_NextMovePoint);
                entity.FramePlan_Move_TouchedTarget = distanceToMove >= distanceToTarget;
                //entity.DebugText = "off" + offsetTargetDest + " loc" + entity.WorldLocation + " next" + entity.FramePlan_Move_NextMovePoint +
                //    " dest" + entity.WorkingDestination_OnlyWriteFromMovementPlanning + " dec" + entity.DecollisionMoveTarget;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "MovementPlanning Error at debugStage " + debugStage + ": " + e, Verbosity.ShowAsError );
            }
            finally
            {
                if ( tracing )
                {
                    //if ( entity.WorkingDestination_OnlyWriteFromMovementPlanning != ArcenPoint.ZeroZeroPoint )
                    {
                        EntityOrder currentOrder = entity.ForShortTermPlanning_CurrentValidOrder;
                        Faction facOrNull = entity.GetFactionOrNull_Safe();
                        movementDebugLogger.Log( PER_SHIP_INFO, entity.PrimaryKeyID,
                            (entity.WorkingDestination_OnlyWriteFromMovementPlanning != ArcenPoint.ZeroZeroPoint ? "PLAN " : "BLANK ") +
                            typeData.DisplayName +
                            " Fac " + entity.GetFactionDisplayNameSafe() +
                            " Kites: " + (facOrNull == null ? "null" : facOrNull.UnitsAutoKite_NeverSetDirectlyOrItBreaksEverything + "") +
                            " Move: " + entity.FramePlan_Move_NextMovePoint +
                            " Decol: " + entity.DecollisionMoveTarget +
                            " WkDest: " + entity.WorkingDestination_OnlyWriteFromMovementPlanning +
                            "\nspeed " + Speed +
                            " locationSetFrom " + locationSetFrom +
                            " OrderCount: " + entity.Orders.GetQueuedOrderCount() +
                            "\nextraDebugInfo " + extraDebugInfo +
                            "\nextraDebugInfo2 " + extraDebugInfo2 +
                            "\ncurrentOrder: " + (currentOrder.TypeData == null ? "null" : currentOrder.ToString()) );
                    }
                }
            }
            return DelReturn.Continue;
        }

        private DelReturn PlanMovementForEntity_Shot_Multithreaded( GameEntity_Shot entity )
        {
            if ( entity == null )
                return DelReturn.Continue;
            Planet planet = entity.Planet;
            if ( planet == null )
                return DelReturn.Continue;
            GameEntityTypeData typeData = entity.TypeData;
            if ( typeData == null )
                return DelReturn.Continue;

            FInt EffectiveDeltaTime = planet.LocalDeltaTime_UnpausedOnly;
            entity.FramePlan_DoMove = false;
            entity.FramePlan_Move_TouchedTarget = false;
            entity._FramePlan_Move_NextMovePoint = ArcenPoint.ZeroZeroPoint;

            int speed = entity.CalculatedSpeed;

            if ( speed <= 0 )
            {
                entity.SetWorkingDestination(ArcenPoint.ZeroZeroPoint);
                return DelReturn.Continue;
            }

            FInt portionOfMovementAllowedBySimStatus = FInt.One;
            if ( entity.RemainingDelayUntilEntersSim > FInt.Zero && EffectiveDeltaTime > FInt.Zero && speed > 0 )
                portionOfMovementAllowedBySimStatus = FInt.One - (entity.RemainingDelayUntilEntersSim / EffectiveDeltaTime);

            if ( portionOfMovementAllowedBySimStatus <= FInt.Zero )
            {
                entity.SetWorkingDestination(ArcenPoint.ZeroZeroPoint);
                return DelReturn.Continue;
            }

            var target = entity.Target.GetSquad();
            {
                if ( target != null && 
                     entity.TargetInitialLocation == ArcenPoint.ZeroZeroPoint )
                {
                    entity.TargetInitialLocation = target.WorldLocation;
                }
                
                entity.SetWorkingDestination(entity.TargetInitialLocation);
            }
            
            if ( entity.WorkingDestination_OnlyWriteFromMovementPlanning == ArcenPoint.ZeroZeroPoint )
                return DelReturn.Continue;

            entity.FramePlan_DoMove = true;
            int distanceToTarget = entity.WorldLocation.GetDistanceTo( entity.WorkingDestination_OnlyWriteFromMovementPlanning, false );

            int distanceToMove;
            if ( EffectiveDeltaTime <= FInt.Zero )
            {
                distanceToMove = 0;
                entity.SetNextMovePoint(entity.WorldLocation);
                
                if ( entity.GetWouldCoordinateBeOutOfRange( entity.FramePlan_Move_NextMovePoint ) )
                {
                    if ( target == null )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Shot target was null!", Verbosity.ShowAsError );
                    else
                        ArcenDebugging.ArcenDebugLogSingleLine( "Shot target " + target.TypeData.GetDisplayName() + " is at position out of range! entity was dead: " +
                            (target.ToBeRemovedAtEndOfThisFrame || target.HasBeenRemovedFromSim) + " target pos: " + target.WorldLocation, Verbosity.ShowAsError );
                }
            }
            else
            {
                speed = (speed * portionOfMovementAllowedBySimStatus).IntValue;
                distanceToMove = (speed * EffectiveDeltaTime).IntValue;
                if ( distanceToMove >= distanceToTarget )
                {
                    entity.SetNextMovePoint(entity.WorkingDestination_OnlyWriteFromMovementPlanning);
                    //if ( entity.GetWouldCoordinateBeOutOfRange( entity.FramePlan_Move_NextMovePoint ) )
                    //{
                    //    if ( target == null )
                    //        ArcenDebugging.ArcenDebugLogSingleLine( "WorkingDestination_OnlyWriteFromMovementPlanning Shot target was null!", Verbosity.ShowAsError );
                    //    else
                    //        ArcenDebugging.ArcenDebugLogSingleLine( "WorkingDestination_OnlyWriteFromMovementPlanning Shot target " + target.TypeData.GetDisplayName() + " is at position out of range! entity was dead: " +
                    //            (target.ToBeRemovedAtEndOfThisFrame || target.HasBeenRemovedFromSim) + " target pos: " + target.WorldLocation, Verbosity.ShowAsError );
                    //}
                }
                else
                {
                    entity.SetNextMovePoint( entity.WorldLocation.GetPointTowardsOther( entity.WorkingDestination_OnlyWriteFromMovementPlanning, distanceToMove, distanceToTarget ) );

                    //if ( entity.GetWouldCoordinateBeOutOfRange( entity.FramePlan_Move_NextMovePoint ) )
                    //{
                    //    if ( target == null )
                    //        ArcenDebugging.ArcenDebugLogSingleLine( "GetPointTowardsOther Shot is at position out of range! " +
                    //              "   WorldLocation: " + entity.WorldLocation +
                    //             "   WorkingDestination_OnlyWriteFromMovementPlanning: " + entity.WorkingDestination_OnlyWriteFromMovementPlanning +
                    //             "   FramePlan_Move_NextMovePoint: " + entity.FramePlan_Move_NextMovePoint, Verbosity.ShowAsError );
                    //    else
                    //        ArcenDebugging.ArcenDebugLogSingleLine( "GetPointTowardsOther Shot target " + target.TypeData.GetDisplayName() + " is at position out of range! entity was dead: " +
                    //            (target.ToBeRemovedAtEndOfThisFrame || target.HasBeenRemovedFromSim) + " target pos: " + target.WorldLocation +
                    //            "   WorldLocation: " + entity.WorldLocation +
                    //            "   WorkingDestination_OnlyWriteFromMovementPlanning: " + entity.WorkingDestination_OnlyWriteFromMovementPlanning +
                    //            "   FramePlan_Move_NextMovePoint: " + entity.FramePlan_Move_NextMovePoint, Verbosity.ShowAsError );
                    //}
                }
            }

            if ( entity.FramePlan_Move_NextMovePoint == ArcenPoint.ZeroZeroPoint )
                entity.FramePlan_DoMove = false;

            entity.FramePlan_Move_TouchedTarget = distanceToMove >= distanceToTarget;

            return DelReturn.Continue;
        }

        private ArcenPoint Inner_HandleSquadKnockback( GameEntity_Squad entity, Planet planet )
        {
            var currentPos = entity.WorldLocation;
            var directknockbackOffset = entity.KnockbackToBeAppliedNextFrame; // Knockback from entity.TakeKnockback( knockback amount, angle ), for modders who want to do custom stuff
            if ( !directknockbackOffset.Valid && entity.KnockbackSourceAndPower.Count == 0 )
                return ArcenPoint.ZeroZeroPoint; // Early out do nothing
            
            var calculatedKnockbackOffset = Inner_CalculateKnockbackFromSources( entity ); // Knockback from entity.AddKnockbackSourceForFramePlan, restrictive compared to entity.TakeKnockback
            var totalKnockbackOffset = calculatedKnockbackOffset + directknockbackOffset;

            // Pass different knockback offset from the one used for orders if its currently out of bounds
            var knockbackOffsetNoOrderGiven = Inner_CheckForInvalidMovement( planet, currentPos, totalKnockbackOffset );
            var restingPos = currentPos + knockbackOffsetNoOrderGiven;

            entity.KnockbackSourceAndPower.Clear();
            entity.KnockbackToBeAppliedNextFrame = ArcenPoint.ZeroZeroPoint;
            entity.SetWorldLocation(restingPos);
            
            return totalKnockbackOffset;
        }

        private ArcenPoint Inner_CheckForInvalidMovement( Planet AtPlanet, ArcenPoint TargetPoint, ArcenPoint DesiredOffset )
        {
            ArcenPoint checkInWormhole = TargetPoint + DesiredOffset;
            bool isOutsideGravWell = AtPlanet != null && AtPlanet.GetIsPointOutsideGravWell_SlowButCorrect( checkInWormhole );
            if ( isOutsideGravWell )
            {
                // Out of Bounds! Change desired offset so that we remain inbounds
                AngleDegrees angle = Engine_AIW2.Instance.CombatCenter.GetAngleToDegrees( checkInWormhole ); // Get angle to final resting position
                DesiredOffset = Engine_AIW2.Instance.CombatCenter.GetPointAtAngleAndDistance( angle, AtPlanet.GravWellSize.DistanceScale_GravwellRadius ); // Get point closest to edge at that angle
                DesiredOffset -= TargetPoint; // subtract out Target Point to make Desired Offset an offset of Target Point again
            }
            return DesiredOffset;
        }

        private ArcenPoint Inner_CalculateCentroidForKnockback( GameEntity_Squad entity )
        {
            var knockbackList = entity.KnockbackSourceAndPower;
            var knockbackCount = knockbackList.Count;
            if ( knockbackCount == 0 )
                return ArcenPoint.ZeroZeroPoint;
            
            if ( knockbackCount == 1 )
                return knockbackList[0].Key;

            // Sum all sources of knockback.
            // Might need a temporary 64 bit variable if there's overflow issues with lots of knockback sources.
            var centroid = ArcenPoint.ZeroZeroPoint;
            for ( int i = 0; i < knockbackCount; i++ )
            {
                centroid += knockbackList[i].Key;
            }
            
            // Get the averages of the locations
            centroid.X /= knockbackCount;
            centroid.Y /= knockbackCount;
            
            return centroid;
        }

        private ArcenPoint Inner_CalculateKnockbackFromSources( GameEntity_Squad entity )
        {
            List<KeyValuePair<ArcenPoint, int>> knockbackList = entity.KnockbackSourceAndPower;

            if (knockbackList.Count == 1) // No cosine and other math needed, just get point at angle and distance
            {
                AngleDegrees angle = knockbackList[0].Key.GetAngleToDegrees( entity.WorldLocation );
                int knockback = knockbackList[0].Value;
                ArcenPoint knockbackOffset = ArcenPoint.ZeroZeroPoint;
                if (knockback < 0)
                {
                    int distanceToSource = knockbackList[0].Key.GetDistanceTo( entity.WorldLocation, false );
                    if ( knockback < -distanceToSource )
                        knockback = -distanceToSource;
                }
                knockbackOffset = knockbackOffset.GetPointAtAngleAndDistance( angle, knockback );
                return knockbackOffset;
            }

            ArcenPoint entityLocation = entity.WorldLocation;
            ArcenPoint centroidLocation = Inner_CalculateCentroidForKnockback( entity );
            AngleDegrees centroidToEntityAngle = centroidLocation.GetAngleToDegrees( entityLocation );
            int knockbackCount = knockbackList.Count;
            int knockbackAlongCentroidLine = 0;

            for ( int i = 0; i < knockbackCount; i++ )
            {
                ArcenPoint knockbackSource = knockbackList[i].Key;
                int knockbackAmount = knockbackList[i].Value;
                AngleDegrees knockbackToEntityAngle = knockbackSource.GetAngleToDegrees( entityLocation );
                AngleDegrees deltaAngle = AngleDegrees.Create( centroidToEntityAngle.GetAbsoluteDeltaNeededToGetToOther( knockbackToEntityAngle ) );

                knockbackAlongCentroidLine += (int)(deltaAngle.Cos() * knockbackAmount);
            }

            if ( knockbackAlongCentroidLine < 0 )
            {
                int distanceToCentroid = entityLocation.GetDistanceTo( centroidLocation, false );
                if ( -knockbackAlongCentroidLine > distanceToCentroid )
                {
                    knockbackAlongCentroidLine = -distanceToCentroid; // If knockback amount would pull this ship through the centroid, stop it at the centroid instead.
                }
            }

            ArcenPoint knockbackEndPosition = ArcenPoint.ZeroZeroPoint;
            knockbackEndPosition = knockbackEndPosition.GetPointAtAngleAndDistance( centroidToEntityAngle, knockbackAlongCentroidLine );

            return knockbackEndPosition;
        }

        static ArcenPoint AimAhead(GameEntity_Squad entity, GameEntity_Squad target)
        {
            return Mat.AimAtTarget(
                entity.WorldLocation, target.WorldLocation, target.CalculateDestinationPoint_Safe(),
                entity.CalculatedSpeed, target.CalculatedSpeed
            );
        }
    }
}
