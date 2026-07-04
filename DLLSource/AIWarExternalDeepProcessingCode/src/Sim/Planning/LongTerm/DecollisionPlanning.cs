using Arcen.Universal;
using System;

using System.Diagnostics;
using System.Threading;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public class DecollisionPlanning_Player : DecollisionPlanning_Root, IBetweenMapGenPoolable<DecollisionPlanning_Player>
    {
        private DecollisionPlanning_Player()
        {
            this.DecollFor = DecollGroup.Player;
        }

        public override int GetSecondsToLiveEachCycle()
        {
            return 30;
        }

        public override int GetSecondsAfterWhichToWarnInOneCycle()
        {
            return 10;
        }

        public override float GetTimeAfterWhichToWarnOfNotRunning()
        {
            return 5f;
        }

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            this.DoNotRunAgainUntilTime = 0;
            CleanupBase();
        }

        private static readonly BetweenMapGenPool<DecollisionPlanning_Player> Pool = BetweenMapGenPool<DecollisionPlanning_Player>.Create_WillNeverBeGCed( "DecollisionPlanning_Player", 30, 10,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new DecollisionPlanning_Player(); } );

        public static DecollisionPlanning_Player GetFromPoolOrCreate()
        {
            DecollisionPlanning_Player context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion
    }

    public class DecollisionPlanning_NPC_A : DecollisionPlanning_Root, IBetweenMapGenPoolable<DecollisionPlanning_NPC_A>
    {
        private DecollisionPlanning_NPC_A()
        {
            this.DecollFor = DecollGroup.NPC_A;
        }

        public override int GetSecondsToLiveEachCycle()
        {
            return 90;
        }

        public override int GetSecondsAfterWhichToWarnInOneCycle()
        {
            return 30;
        }

        public override float GetTimeAfterWhichToWarnOfNotRunning()
        {
            return 15f;
        }

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            this.DoNotRunAgainUntilTime = 0;
            CleanupBase();
        }

        private static readonly BetweenMapGenPool<DecollisionPlanning_NPC_A> Pool = BetweenMapGenPool<DecollisionPlanning_NPC_A>.Create_WillNeverBeGCed( "DecollisionPlanning_NPC_A", 30, 10,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new DecollisionPlanning_NPC_A(); } );

        public static DecollisionPlanning_NPC_A GetFromPoolOrCreate()
        {
            DecollisionPlanning_NPC_A context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion
    }

    public class DecollisionPlanning_NPC_B : DecollisionPlanning_Root, IBetweenMapGenPoolable<DecollisionPlanning_NPC_B>
    {
        private DecollisionPlanning_NPC_B()
        {
            this.DecollFor = DecollGroup.NPC_B;
        }

        public override int GetSecondsToLiveEachCycle()
        {
            return 90;
        }

        public override int GetSecondsAfterWhichToWarnInOneCycle()
        {
            return 30;
        }

        public override float GetTimeAfterWhichToWarnOfNotRunning()
        {
            return 15f;
        }

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            this.DoNotRunAgainUntilTime = 0;
            CleanupBase();
        }

        private static readonly BetweenMapGenPool<DecollisionPlanning_NPC_B> Pool = BetweenMapGenPool<DecollisionPlanning_NPC_B>.Create_WillNeverBeGCed( "DecollisionPlanning_NPC_B", 30, 10,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new DecollisionPlanning_NPC_B(); } );

        public static DecollisionPlanning_NPC_B GetFromPoolOrCreate()
        {
            DecollisionPlanning_NPC_B context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion
    }

    public abstract class DecollisionPlanning_Root : ArcenLongTermContinuousPlanningContext
    {
        protected DecollisionPlanning_Root()
            : base( ArcenSimContextType.LongTermContinuous )
        {
        }

        protected DecollGroup DecollFor = DecollGroup.Player;

        protected float DoNotRunAgainUntilTime;
        public override bool GetNeedsToRun()
        {
            return !this.IsRunning && this.DoNotRunAgainUntilTime < ArcenTime.TimeSinceStartF;
        }

        public const int MAX_DECOLLISIONS_TO_CHECK = 1000;
        public const int MAX_DECOLLISIONS_TO_SEND = 500;

        private int workingShipsChecked = 0;
        private int workingShipsWithDataToSend = 0;
        private int currentPlanningCycle;

        private readonly List<DecollisionData> decollisionsToRun = List<DecollisionData>.Create_WillNeverBeGCed( MAX_DECOLLISIONS_TO_SEND, "DecollisionPlanning-decollisionsToRun" );

        protected override void Execute()
        {
            //UnityEngine.Debug.Log( "DECOL Thread:" + Thread.CurrentThread.ManagedThreadId );
            try
            {
                DecollisionStats stats = World_AIW2.Instance.Decollision_Player;
                switch ( this.DecollFor )
                {
                    case DecollGroup.NPC_A:
                        stats = World_AIW2.Instance.Decollision_NPC_A;
                        break;
                    case DecollGroup.NPC_B:
                        stats = World_AIW2.Instance.Decollision_NPC_B;
                        break;
                }
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                {
                    //don't let that timer get strange
                    stats.TimeOfCurrentDecollisionCycleStart = 0;
                    return;
                }
                if ( World.Instance.IsPaused )
                {
                    //don't let that timer get strange
                    stats.TimeOfCurrentDecollisionCycleStart = 0;
                    return;
                }
                if ( stats.TimeOfCurrentDecollisionCycleStart == 0 )
                    stats.TimeOfCurrentDecollisionCycleStart = ArcenTime.TimeSinceStartF;

                if ( CentralVars.DEBUG_TURN_OFF_DECOL_PLAN )
                    return;
                
                decollisionsToRun.Clear();
                workingShipsChecked = 0;
                workingShipsWithDataToSend = 0;
                currentPlanningCycle = stats.CurrentDecollisionCycle;

                #region Central Loop
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    for ( int j = 0; j < planet.Factions.Count; j++ )
                    {
                        PlanetFaction faction = planet.Factions[j];
                        if ( this.DecollFor == DecollGroup.Player )
                        {
                            if ( faction.Faction.Type != FactionType.Player )
                                continue; //only handle players
                        }
                        else
                        {
                            if ( faction.Faction.Type == FactionType.Player )
                                continue; //handle all BUT players

                            if ( this.DecollFor == DecollGroup.NPC_A )
                            {
                                if ( faction.FactionIndex % 2 == 0 )
                                    continue; //only run half of the NPCs
                            }
                            else
                            {
                                if ( faction.FactionIndex % 2 != 0 )
                                    continue; //run the other half of the NPCs
                            }
                        }
                        bool breakFactionLoop = false;
                        foreach ( GameEntity_Squad entity in faction.Entities.Squads() )
                        {
                            if ( DoEntityFramePlanningLogic_Collision( entity ) == DelReturn.Break )
                            {
                                breakFactionLoop = true;
                                break;
                            }
                        }
                        if ( breakFactionLoop )
                            break;
                    }
                    if ( workingShipsChecked >= MAX_DECOLLISIONS_TO_CHECK || workingShipsWithDataToSend >= MAX_DECOLLISIONS_TO_SEND )
                        break;
                }
                #endregion

                //UnityEngine.Debug.Log( "DECOL CHECK: " + decollisionsToRun.Count );
                if ( decollisionsToRun.Count > 0 )
                {
                    GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[this.DecollFor == DecollGroup.Player ? BaseGameCommand.Code.DecollideUnits_MoveManyToMany_Player :
                        BaseGameCommand.Code.DecollideUnits_MoveManyToMany_NPC], GameCommandSource.AnythingElse );
                    command.ToBeQueued = false;
                    DecollisionData decoll;
                    World_AIW2.Instance.TotalDecollisionsScheduled += decollisionsToRun.Count;
                    for ( int i = 0; i < decollisionsToRun.Count; i++ )
                    {
                        decoll = decollisionsToRun[i];
                        command.RelatedPoints.Add( decoll.Dest );
                        command.RelatedEntityIDs.Add( decoll.SquadID );
                        command.RelatedIntegers.Add( decoll.PlanetIndexOfSquad );
                    }
                    if ( command.RelatedPoints.Count > 0 && command.RelatedEntityIDs.Count > 0 && command.RelatedIntegers.Count > 0 )
                    {
                        //UnityEngine.Debug.Log( "Sending: " + command.WriteToStringInefficient() );
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetNaturalObjectFactionNeverNull(), command, false );
                    }
                    else //prevents having a leak!
                        command.ReturnToPool();
                    decollisionsToRun.Clear();
                }

                stats.CurrentSoFarDecollisionCycleAssignments += workingShipsWithDataToSend;

                //hey, we finished without getting a full load of checks; must mean we got everything this cycle
                if ( workingShipsChecked < MAX_DECOLLISIONS_TO_CHECK )
                {
                    stats.CurrentDecollisionCycle++;
                    float currentTime = ArcenTime.TimeSinceStartF;
                    //only set that timer if we have real data for it
                    if ( stats.TimeOfCurrentDecollisionCycleStart > 0 )
                    {
                        stats.TotalTimeOfLastDecollisionCycle = currentTime - stats.TimeOfCurrentDecollisionCycleStart;
                        if ( stats.TotalTimeOfLastDecollisionCycle < 0.01f )
                            stats.TotalTimeOfLastDecollisionCycle = 0.01f;
                    }
                    //don't directly set the timer for the next cycle, instead set it to 0 so that it will get properly set when the next timer starts if there is a gap
                    stats.TimeOfCurrentDecollisionCycleStart = 0;

                    stats.LastDecollisionCycleAssignments = stats.CurrentSoFarDecollisionCycleAssignments;
                    stats.CurrentSoFarDecollisionCycleAssignments = 0;

                    //try to run this every 5 seconds, or with 1 second gaps, whichever is slower
                    float timeToWaitBeforeNextStart = 1f - stats.TotalTimeOfLastDecollisionCycle;
                    if ( timeToWaitBeforeNextStart < 1 )
                        timeToWaitBeforeNextStart = 1;
                    this.DoNotRunAgainUntilTime = ArcenTime.TimeSinceStartF + timeToWaitBeforeNextStart;
                }
            }

            catch ( ArcenPleaseStopThisThreadException ) { }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "DecollisionPlanning Exception: " + e, Verbosity.ShowAsError );
            }
        }

        /// <summary>
        /// This is frames number of spacing between when the ships can collide once and the next time
        /// </summary>
        private const int MIN_FRAMES_BETWEEN_COLLISION_CHECKS = 1;

        private DelReturn DoEntityFramePlanningLogic_Collision( GameEntity_Squad entity )
        {
            int debugNum = 0;
            try
            {
                debugNum = -100;
                
                if (entity == null)
                    return DelReturn.Continue;

                debugNum = -99;
                
                if ( entity.PrimaryKeyID == -17 )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Oi!  Tried to run DoEntityFramePlanningLogic_Collision on the FakeEntity!  How did this even get into the game?  Faction: " +
                        entity.GetFactionDisplayNameSafe() + " typeData: " + entity.GetTypeDisplayNameSafe() + " planet: " + entity.GetPlanetNameSafe(), Verbosity.ShowAsError );
                    return DelReturn.Continue;
                }

                debugNum = -98;
                
                if ( workingShipsChecked >= MAX_DECOLLISIONS_TO_CHECK || workingShipsWithDataToSend >= MAX_DECOLLISIONS_TO_SEND )
                    return DelReturn.Break;

                debugNum = -97;
                
                if ( entity.LastDecollisionCycle >= currentPlanningCycle )
                    return DelReturn.Continue;

                debugNum = -96;
                
                int secondsBetween = 5;
                switch ( entity.Planet.BattleStatus )
                {
                    case PlanetBattleStatus.Tier3_PlayersAbsent_OffFrame:
                    case PlanetBattleStatus.Tier3_PlayersAbsent_OnFrame:
                        secondsBetween = 15;
                        break;
                }

                debugNum = -95;
                
                if ( ( World_AIW2.Instance.GameSecond - entity.LastGameSecondOfDecollision_OrZeroIfMoreThan15sAgo ) < secondsBetween )
                    return DelReturn.Continue;
                if ( entity.GameSecondEnteredThisPlanet >= World_AIW2.Instance.GameSecond - 3 )
                    return DelReturn.Continue; //changed planets too recently
                if ( entity.TypeData.IsMobileOrbiter || entity.TypeData.IsPlanetaryOrbiter )
                    return DelReturn.Continue; //these should never decollide with anything!
                if ( entity.TypeData.IsFleetLeader )
                {
                    if ( entity.CalculateShouldOrderBeIgnoredByFlagshipAsIfWasStationaryMode( false ) ) //don't send inverter when checking this
                        return DelReturn.Continue; //don't tell us things we won't do!
                }

                debugNum = -94;
                
                int planetIndexAtStartForEntity = entity.Planet == null ? -1 : entity.Planet.Index;

                entity.LastDecollisionCycle = currentPlanningCycle;
                workingShipsChecked++;

                debugNum = 1;
                entity.NonSim_WorkingDecollisionMoveTarget = ArcenPoint.ZeroZeroPoint;
                //entity.DebugText = "decattempt A";
                //means we're already in the process of decolliding
                if ( entity.DecollisionMoveTarget.X != 0 )
                    return DelReturn.Continue;
                debugNum = 2;
                //entity.DebugText = "decattempt B";

                //this means that it has not moved since the last time we checked on it, so leave it alone in general
                //if ( entity.NonSim_LastDecollisionFrameIfPositiveOrHasNotMovedIfNegative < 0 )
                //    return DelReturn.Continue;

                //if I'm trying to load into a centerpiece as a transport, and I'm not actually the transport, then ignore me completely, please!
                Fleet entityFleet = entity.GetFleetOrNull_Safe();
                if ( entityFleet != null && entityFleet.IsFleetInTransportLoadMode && entity != entityFleet.Centerpiece.GetSquad() )
                    return DelReturn.Continue;

                EntityOrderCollection ordersExisting = entity.Orders;
                if ( ordersExisting.GetQueuedOrderCount() > 0 )
                {
                    debugNum = 3;
                    EntityOrder order = ordersExisting.GetQueuedOrderAtIndex_OrNull( 0 );
                    if ( order.TypeData != null && entity.CalculatedSpeed > 0 )
                    {
                        switch ( order.TypeData.Type )
                        {
                            //case EntityOrderType.Attack:
                            case EntityOrderType.Wormhole:
                                {
                                    GameEntity_Other wormhole = order.GetWormholeToOtherPlanetOrNull( entity.Planet );
                                    debugNum = 4;
                                    if ( wormhole == null )
                                        return DelReturn.Continue; //if this wormhole is null (presumably because we already went through the wormhole, do nothing
                                    if ( entity.GetFactionTypeSafe() == FactionType.Player )
                                    {
                                        if ( wormhole.WorldLocation.GetHasAnyChanceOfBeingInRange( entity.WorldLocation, 5000 ) )
                                        {
                                            //if a player and pretty close to the wormhole, then don't try to give decollision orders!
                                            return DelReturn.Continue;
                                        }
                                    }
                                    else
                                    {
                                        //if not a player and heading to a wormhole at all, then don't try to give decollision orders!
                                        return DelReturn.Continue;
                                    }
                                }
                                break;
                            case EntityOrderType.GetIntoTransport:
                                {
                                    GameEntity_Squad centerpiece = order.RelatedSquad.GetSquad();
                                    if (centerpiece == null)
                                        return DelReturn.Continue; //Must have entered the centerpiece already
                                    ArcenPoint centerpieceLocation = centerpiece.WorldLocation;
                                    if (centerpieceLocation.GetHasAnyChanceOfBeingInRange(entity.WorldLocation, 4000))
                                    {
                                        //Don't give decollision orders when near the fleet centerpiece we're entering
                                        return DelReturn.Continue;
                                    }
                                }
                                break;
                        }
                    }
                }
                debugNum = 5;
                //yes this is not part of the actual sim, so the current frame number might change during the overall loop here.  That's very much ok!
                if ( entity.NonSim_LastDecollisionFrameIfPositiveOrHasNotMovedIfNegative + MIN_FRAMES_BETWEEN_COLLISION_CHECKS > World_AIW2.Instance.Network_CurrentFrameNumber )
                    return DelReturn.Continue;

                //entity.DebugText = "decattempt C";

                if ( entity.MovedLastFrame )
                    return DoEntityFramePlanningLogic_CollisionBasedOnEndLocation( entity );
                debugNum = 60;
                //entity.DebugText = "decattempt E";
                if ( entity.DataForMark == null )
                    return DelReturn.Continue;
                int speed = entity.DataForMark.Speed; //use the default speed, not the current speed
                if ( speed <= 0 )
                    return DelReturn.Continue;
                //entity.DebugText = "decattempt F";
                debugNum = 61;
                int collisionRadius = entity.DataForMark.Radius;
                debugNum = 7;
                
                GameEntity_Squad mostPenetratedEntity = null;
                if ( !CalculateDoIHaveACollisionAtLocation( entity, entity.WorldLocation, collisionRadius, out mostPenetratedEntity ) )
                {
                    //entity.DebugText = "decattempt G";
                    //tell me to stop looking for collisions on this entity
                    //this data is not saved, and is server-only, which is the only reason why this is ok to set here rather than in a GameCommand!
                    debugNum = 8;
                    entity.NonSim_LastDecollisionFrameIfPositiveOrHasNotMovedIfNegative = World_AIW2.Instance.Network_CurrentFrameNumber + 5;
                    return DelReturn.Continue;
                }

                //entity.DebugText = "decattempt H";
                debugNum = 9;

                int innerSpread = collisionRadius / 2;
                int outerSpread = collisionRadius * 6;
                int originalOuterSpread = outerSpread;
                var searchOrigin = entity.WorldLocation;

                bool CircleContainsCircle( ArcenPoint p1, int r1, ArcenPoint p2, int r2)
                {
                    var d = Math.Sqrt(p1.GetSquareDistanceTo(p2));
                    d += r2;
                    if (d <= r1)
                        return true;
                    return false;
                }

                if (mostPenetratedEntity != null)
                {
                    if (CircleContainsCircle( mostPenetratedEntity.WorldLocation, mostPenetratedEntity.GetRadius(), entity.WorldLocation, collisionRadius))
                    {
                        //ArcenDebugging.ArcenDebugLogSingleLine(string.Format("Entity doing decollision {0} is entirely contained by {1}", entity.GetTypeDisplayNameSafe(), mostPenetratedEntity.GetTypeDisplayNameSafe()), Verbosity.DoNotShow);

                        // So we need to decollide against something
                        // and that something is so big we are entirely contained by it.
                        //
                        // Rather than starting the search for a non-colliding location
                        // right were we are currently...
                        //
                        // Start at the closest edge of the thing containing us.

                        var vec = entity.WorldLocation - mostPenetratedEntity.WorldLocation;
                        var norm = vec.ToVector2().normalized;
                        searchOrigin = mostPenetratedEntity.WorldLocation + (norm * mostPenetratedEntity.GetRadius()).ToArcenPoint();
                    }
                }

                for ( int outerLoop = 0; outerLoop < 5; outerLoop++ )
                {
                    for ( int i = 0; i < 10; i++ )
                    {
                        debugNum = 10;
                        ArcenPoint potentialPoint = entity.Planet.GetRandomPointWithinCircleAndAlsoWithinGravWell( searchOrigin, innerSpread, outerSpread, RandomToUse );
                        if ( !CalculateDoIHaveACollisionAtLocation( entity, potentialPoint, collisionRadius, out mostPenetratedEntity ) )
                        {
                            //yay, we found a good place!
                            //tell us to go there
                            debugNum = 11;
                            //if the entity changed planets while we calculated this, then don't consider this valid1
                            if ( planetIndexAtStartForEntity == entity.Planet.Index && entity.GameSecondEnteredThisPlanet < World_AIW2.Instance.GameSecond - 3 )
                                decollisionsToRun.Add( DecollisionData.CreateNew( entity.PrimaryKeyID, entity.Planet.Index, potentialPoint ) );
                            workingShipsWithDataToSend++;
                            entity.NonSim_WorkingDecollisionMoveTarget = potentialPoint;
                            return DelReturn.Continue;
                        }
                    }
                    outerSpread += originalOuterSpread;
                }

                return DelReturn.Continue;
            }
            catch ( ArcenPleaseStopThisThreadException )
            {
            }
            catch ( Exception e )
            {
                LOG.Err( "Exception in DecollisionPlanning.DoEntityFramePlanningLogic_Collision at debugNum={0}\n{1}", debugNum, e);
            }
            
            return DelReturn.Continue;
        }
        
        private DelReturn DoEntityFramePlanningLogic_CollisionBasedOnEndLocation( GameEntity_Squad entity )
        {
            //entity.DebugText = "decattempt D";
            ArcenPoint targetPoint = entity.WorkingDestination_OnlyWriteFromMovementPlanning;

            if ( entity.WorkingIndex_OnlyWriteFromMovementPlanning != entity.Planet.Index )
                return DelReturn.Continue; //we've changed planets since the last time movement planning ran, so the movement planning data is stale
            
            if ( entity.WorkingDestination_OnlyWriteFromMovementPlanning == ArcenPoint.ZeroZeroPoint )
                return DelReturn.Continue;

            int collisionRadiusReal = entity.DataForMark.Radius;

            int distanceFromTarget = entity.WorldLocation.GetDistanceTo( targetPoint, false );
            if ( distanceFromTarget < collisionRadiusReal + collisionRadiusReal )
            {
                //we're really close to the target, so shift our offset to be next to nothing to avoid problems.
                decollisionsToRun.Add( DecollisionData.CreateNew( entity.PrimaryKeyID, entity.Planet.Index, ArcenPoint.ZeroZeroPoint ) );
                workingShipsWithDataToSend++;
                entity.NonSim_WorkingDecollisionMoveTarget = ArcenPoint.ZeroZeroPoint;
                return DelReturn.Continue;
            }

            if ( entity.DecollisionMoveTarget.X != 0 )
            {
                int distanceFromDecollisionTarget = entity.WorldLocation.GetDistanceTo( entity.DecollisionMoveTarget, false );
                if ( distanceFromDecollisionTarget < 20 )
                {
                    //we're really close to the decollision target, so shift our offset to be next to nothing to avoid problems.
                    decollisionsToRun.Add( DecollisionData.CreateNew( entity.PrimaryKeyID, entity.Planet.Index, ArcenPoint.ZeroZeroPoint ) );
                    workingShipsWithDataToSend++;
                    entity.NonSim_WorkingDecollisionMoveTarget = ArcenPoint.ZeroZeroPoint;
                    return DelReturn.Continue;
                }
            }

            int collisionRadiusToSpread = entity.DataForMark.Radius;
            if ( distanceFromTarget > collisionRadiusReal * 20 )
                collisionRadiusToSpread *= 3;
            else if ( distanceFromTarget > collisionRadiusReal * 10 )
                collisionRadiusToSpread *= 2;

            if ( !CalculateDoIHaveACollisionAtCurrentLocationWithShipsHeadingToTargetLocation( entity, entity.WorldLocation, collisionRadiusReal, targetPoint, collisionRadiusReal + collisionRadiusReal ) )
            {
                //tell me to stop looking for collisions on this entity
                //this data is not saved, and is server-only, which is the only reason why this is ok to set here rather than in a GameCommand!
                entity.NonSim_LastDecollisionFrameIfPositiveOrHasNotMovedIfNegative = World_AIW2.Instance.Network_CurrentFrameNumber + 5;
                return DelReturn.Continue;
            }

            int innerSpread = collisionRadiusToSpread / 2;
            int outerSpread = collisionRadiusToSpread * 3;
            int originalOuterSpread = outerSpread;
            for ( int outerLoop = 0; outerLoop < 5; outerLoop++ )
            {
                for ( int i = 0; i < 10; i++ )
                {
                    ArcenPoint potentialPoint = entity.Planet.GetRandomPointWithinCircleAndAlsoWithinGravWell( targetPoint, innerSpread, outerSpread, RandomToUse );
                    if ( !CalculateDoIHaveACollisionAtCurrentLocationWithShipsHeadingToTargetLocation( entity, potentialPoint, collisionRadiusReal, targetPoint, collisionRadiusReal + collisionRadiusReal ) )
                    {
                        //yay, we found a good place!
                        //tell us to go there
                        decollisionsToRun.Add( DecollisionData.CreateNew( entity.PrimaryKeyID, entity.Planet.Index, potentialPoint ) );
                        workingShipsWithDataToSend++;
                        entity.NonSim_WorkingDecollisionMoveTarget = potentialPoint;
                        return DelReturn.Continue;
                    }
                }
                outerSpread += originalOuterSpread;
            }

            return DelReturn.Continue;
        }

        //this being set above 0 causes problems with placement other places
        public const int EXTRA_COLLISION_SPACING = 0;

        private bool CalculateDoIHaveACollisionAtLocation( GameEntity_Squad entity, ArcenPoint worldLocationToCheck, int collisionRadius, out GameEntity_Squad mostPenetratedEntity )
        {
            workingCollisionRadius = collisionRadius + EXTRA_COLLISION_SPACING;
            workingPointToCheckAsLocation = worldLocationToCheck;
            workingPointTargetMustBeNear = ArcenPoint.ZeroZeroPoint;
            workingPointTargetMustBeNear_Radius = -1;
            workingEntity = entity;
            workingHitFound = false;
            workingHitEntity = null;
            workingHitPenetration = -1;
            if ( entity.Planet != null )
                foreach ( GameEntity_Squad otherEntity in entity.Planet.Squads() )
                    if ( Inner_CheckForCollisionOrMakeEntityMove( otherEntity ) == DelReturn.Break )
                        break;
            mostPenetratedEntity = workingHitEntity;
            return workingHitFound;
        }

        private bool CalculateDoIHaveACollisionAtCurrentLocationWithShipsHeadingToTargetLocation( GameEntity_Squad entity, ArcenPoint worldLocationToCheck, 
            int collisionRadius, ArcenPoint targetLocationMustMatch, int targetDistanceMustBeAtMost )
        {
            workingCollisionRadius = collisionRadius + EXTRA_COLLISION_SPACING;
            workingPointToCheckAsLocation = worldLocationToCheck;
            workingPointTargetMustBeNear = targetLocationMustMatch;
            workingPointTargetMustBeNear_Radius = targetDistanceMustBeAtMost;
            workingEntity = entity;
            workingHitFound = false;
            workingHitEntity = null;
            workingHitPenetration = -1;
            if ( entity.CurrentStateOfMatter.HasSeparateViewMode )
                workingStateOfMatterMustBe = entity.CurrentStateOfMatter;
            if ( entity.Planet != null )
                foreach ( GameEntity_Squad otherEntity in entity.Planet.Squads() )
                    if ( Inner_CheckForCollisionOrMakeEntityMove( otherEntity ) == DelReturn.Break )
                        break;
            return workingHitFound;
        }

        private GameEntity_Squad workingEntity;
        private bool workingHitFound;
        private int workingHitPenetration;
        private GameEntity_Squad workingHitEntity;
        private ArcenPoint workingPointToCheckAsLocation;
        private ArcenPoint workingPointTargetMustBeNear;
        private int workingPointTargetMustBeNear_Radius;
        private int workingCollisionRadius;
        private StateOfMatterTypeData workingStateOfMatterMustBe = null;
        
        private DelReturn Inner_CheckForCollisionOrMakeEntityMove( GameEntity_Squad otherEntity )
        {
            int debugNum = 0;
            try
            {
                GameEntity_Squad entity = workingEntity;
                debugNum = 10;
                if ( otherEntity == null || otherEntity.DataForMark == null )
                    return DelReturn.Continue;
                //don't collide with myself
                if ( entity == otherEntity )
                    return DelReturn.Continue;
                if ( otherEntity.TypeData.IsMobileOrbiter || otherEntity.TypeData.IsPlanetaryOrbiter )
                    return DelReturn.Continue; //these should never collide with anything!
                
                // If we are reverse tractored to the other entity, obviously don't decollide
                if ( otherEntity.CurrentlyIAmBeingReverseTractoredByTheseOtherShipsAndThusPullingThem_OrNull?.DisplayContains(entity) == true)
                    return DelReturn.Continue; 
                
                debugNum = 15;
                //if the other thing is trying to load into a centerpiece as a transport, and it's not actually the transport, then ignore it completely, please!
                Fleet otherEntityFleet = otherEntity.GetFleetOrNull_Safe();
                if ( otherEntityFleet != null && otherEntityFleet.IsFleetInTransportLoadMode && otherEntity != otherEntityFleet.Centerpiece.GetSquad() )
                    return DelReturn.Continue;
                debugNum = 16;
                if ( workingStateOfMatterMustBe != null && otherEntity.CurrentStateOfMatter != workingStateOfMatterMustBe )
                    return DelReturn.Continue;
                debugNum = 17;
                debugNum = 18;
                //the really raw check to see if any of these points are in range
                int penetration = -1;
                if ( otherEntity != null && (!otherEntity.GetIsWithinRangeOf( workingPointToCheckAsLocation, workingCollisionRadius, out penetration) ))
                {
                    debugNum = 20;
                    return DelReturn.Continue;
                    //if ( otherEntity != null && (otherEntity.DecollisionMoveTarget.X == 0 || !otherEntity.DecollisionMoveTarget.GetHasAnyChanceOfBeingInRange( workingPointToCheckAsLocation, collisionRadiusWithOtherEntity ) ))
                    //{
                    //    debugNum = 30;
                    //    if ( otherEntity.NonSim_WorkingDecollisionMoveTarget.X == 0 || !otherEntity.NonSim_WorkingDecollisionMoveTarget.GetHasAnyChanceOfBeingInRange( workingPointToCheckAsLocation, collisionRadiusWithOtherEntity ) )
                    //        return DelReturn.Continue;
                    //}
                }
                //ArcenDebugging.ArcenDebugLogSingleLine(string.Format("entity {0} found a hit with {1} and penetration is {2}", entity.GetTypeDisplayNameSafe(), otherEntity.GetTypeDisplayNameSafe(), penetration), Verbosity.DoNotShow);

                debugNum = 40;
                //CHRIS_TODO: Hey, this might be a bug!  Should this be checking that it literally overlaps??
                //if ( !otherEntity.GetIsWithinRangeOf( workingPointToCheckAsLocation, entity.DataForMark.Radius ) )
                //    return DelReturn.Continue;
                //if the destination of the ship we're checking has to be near the destination of the ship that is moving
                if ( workingPointTargetMustBeNear_Radius > 0 )
                {
                    debugNum = 50;
                    if ( otherEntity.WorkingDestination_OnlyWriteFromMovementPlanning.X == 0 )
                        return DelReturn.Continue; //no destination
                    debugNum = 60;
                    if ( workingPointTargetMustBeNear.GetDistanceTo( otherEntity.WorkingDestination_OnlyWriteFromMovementPlanning, false ) > workingPointTargetMustBeNear_Radius )
                        return DelReturn.Continue; //destination is too far from our destination, so don't keep track of this
                }
                debugNum = 70;
                bool hasPriority = otherEntity.TypeData.CollisionPriority < entity.TypeData.CollisionPriority;
                if ( !hasPriority &&
                     otherEntity.TypeData.CollisionPriority == entity.TypeData.CollisionPriority &&
                     otherEntity.PrimaryKeyID >= entity.PrimaryKeyID )
                    hasPriority = true;
                debugNum = 80;
                if ( hasPriority && otherEntity.CalculatedSpeed <= 0 )
                {
                    //if the thing that we're checking is a fleet leader or has a buble forcefield, 
                    //and the thing that might bump it out of the way CAN move
                    //but is just frozen now for any reason, then
                    //don't let that other thing bump the fleet leader out of position
                    if ( otherEntity.DataForMark.Speed > 0 &&
                        (entity.TypeData.IsFleetLeader ||
                        entity.GetHasBubbleForcefieldRightNow() ) )
                    { }
                    else
                        hasPriority = false;
                }
                if ( hasPriority )
                    return DelReturn.Continue;
                debugNum = 90;

                workingHitFound = true;
                if (penetration > workingHitPenetration)
                {
                    workingHitPenetration = penetration;
                    workingHitEntity = otherEntity;
                }
            }
            catch ( Exception e )
            {
                if ( otherEntity == null || otherEntity.DataForMark == null || workingEntity == null || workingEntity.DataForMark == null )
                {
                    //these errors can happen because the entity was being torn down on a different thread (either entity was), so we'll just ignore this error
                    //this error won't hurt anything and is just a cross-threading thing.
                    return DelReturn.Continue;
                }
                ArcenDebugging.ArcenDebugLog( "Exception in DecollisionPlanning.Inner_CheckForCollisionOrMakeEntityMove. debug num " + debugNum +
                    ". error:\n" + e, Verbosity.ShowAsError );
                
            }
            return DelReturn.Break;
        }

        public struct DecollisionData
        {
            public int SquadID;
            public Int16 PlanetIndexOfSquad;
            public ArcenPoint Dest;

            public static DecollisionData CreateNew( int SquadID, Int16 PlanetIndexOfSquad, ArcenPoint Dest )
            {
                DecollisionData data;
                data.SquadID = SquadID;
                data.PlanetIndexOfSquad = PlanetIndexOfSquad;
                data.Dest = Dest;
                return data;
            }
        }        

        protected enum DecollGroup
        {
            Player,
            NPC_A,
            NPC_B
        }
    }
}
