using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class GameCommand_MoveManyToOnePoint : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            List<SafeSquadWrapper> targetEntities = GameEntity_Squad.GetTemporarySquadList( "GameCommand_MoveManyToOnePoint-targetEntities", 10f );
            if ( targetEntities == null ) //blocked for teardown/shutdown; bail
                return;
            Helper_GetListOfEntitiesWithSomeAsNull( command, targetEntities );
            if ( command.RelatedPoints == null )
            {
                ArcenDebugging.ArcenDebugLog( "command.RelatedPoints == null in GameCommand_MoveManyToOnePoint!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                GameEntity_Squad.ReleaseTemporarySquadList( targetEntities );
                return;
            }
            if ( command.RelatedPoints.Count < 1 )
            {
                ArcenDebugging.ArcenDebugLog( "command.RelatedPoints.Count < 1 in GameCommand_MoveManyToOnePoint!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                GameEntity_Squad.ReleaseTemporarySquadList( targetEntities );
                return;
            }

            OrderSource orderFrom = OrderSource.Other;
            if ( command.FromActualInputEventOfPlayerID < 255 )
            {
                orderFrom = OrderSource.HumanPlayer;
                //Debug.Log( "ORDER FROM HUMAN: MOVE" );
            }

            ArcenPoint targetPoint = command.RelatedPoints.First;
            if (targetPoint == ArcenPoint.ZeroZeroPoint)
            {
                if ( targetEntities.Count > 0 )
                {
                    //This seems likely to indicate some sort of misbehaviour, but i'm not sure what.
                    //The problem is in Starkelp's code which I don't know well
                    //ArcenDebugging.ArcenDebugLogSingleLine("Got invalid command for " + targetEntities.Count + " units; the first unit is " + targetEntities[0].GetSquad().ToStringWithPlanetAndOwner(), Verbosity.DoNotShow );
                }
                GameEntity_Squad.ReleaseTemporarySquadList( targetEntities );
                return; //this is an invalid command for whatever reason, so ignore it.
            }

            SpeedGroup newSpeedGroup_HostOnly = null;
            List<SafeSquadWrapper> entitiesToAddToSpeedGroup = GameEntity_Squad.GetTemporarySquadList( "GameCommand_MoveManyToOnePoint-entitiesToAddToSpeedGroup", 10f );
            if ( entitiesToAddToSpeedGroup == null ) //blocked for teardown/shutdown; bail
            {
                GameEntity_Squad.ReleaseTemporarySquadList( targetEntities );
                return;
            }

            for ( int i = 0; i < targetEntities.Count; i++ )
            {
                GameEntity_Squad entity = targetEntities[i].GetSquad();
                if ( entity == null || entity.Planet == null || entity.HasBeenRemovedFromSim || entity.ToBeRemovedAtEndOfThisFrame )
                    continue;
                if ( ShouldSkipEntityForPlanetOrderMismatch( command, entity, true, "MoveManyToOnePoint" ) )
                    continue; //if this is a queued command and the ship is going to be getting to this planet, allow the order to be queued
                entity.DecollisionMoveTarget = ArcenPoint.ZeroZeroPoint;
                EntityOrderCollection orders = entity.Orders;
                if ( !command.ToBeQueued )
                    orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, orderFrom == OrderSource.HumanPlayer ? ClearSource.YesClearAnyOrders_IncludingFromHumans : ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "MoveManyToOnePointNotQueued" );
                else
                    orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.OnlyClearDecollisionOrdersAndNothingElse, ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "MoveManyToOnePointAndQueue" );

                EntityOrder newOrder = EntityOrder.Create_Move_Normal( targetPoint, true, orderFrom,
                    entity.CalculateShouldOrderBeIgnoredByFlagshipAsIfWasStationaryMode( command.RelatedMagnitude > 0 ) );
                if ( newOrder.TypeData == null )
                    continue;
                orders.QueueOrder( entity, newOrder );

                if ( entity.GuardOrPatrolOffsetPoints.Count > 0 )
                    entity.GuardOrPatrolOffsetPoints.Clear();
                if ( orders.Behavior == EntityBehaviorType.Attacker_Full &&
                    entity.GetFactionTypeSafe() == FactionType.Player &&
                    entity.TypeData.IsMobileAndHasRepairOrAssistConstructionFlowsOrIsMobileMeleeUnit )// used by engineer FRD to have a point to return to, and melee
                {
                    entity.GuardOrPatrolOffsetPoints.Add( targetPoint );
                }

                if ( orderFrom == OrderSource.HumanPlayer )
                    entity.PreferredEntityTypeDataForTargeting = null;

                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                {
                    entitiesToAddToSpeedGroup.Add( entity );

                    // If any unit is in a speed group, treat this as a group move and create a new speed group so all units can be placed in it.
                    // This is the least confusing since the group move indicator was already on after selecting them prior to moving.
                    if ( entity.GroupMoveSpeed_HostOnly != null && !entity.GroupMoveSpeed_HostOnly.IsDummy && newSpeedGroup_HostOnly == null )
                    {
                        newSpeedGroup_HostOnly = SpeedGroup.Create_CallFromHostOnly( entity.GetFactionOrNull_Safe(),
                                entity.GroupMoveSpeed_HostOnly.UseFancyPlayerStyle );
                        newSpeedGroup_HostOnly.SetOverrideSpeedLimit( entity.GroupMoveSpeed_HostOnly.OverrideSpeedLimit );
                    }
                }
            }

            if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
            {
                if ( newSpeedGroup_HostOnly != null )
                {
                    foreach ( SafeSquadWrapper wrap in entitiesToAddToSpeedGroup )
                    {
                        GameEntity_Squad entity = wrap.GetSquad();
                        if ( entity == null )
                            continue;
                        newSpeedGroup_HostOnly.AddEntity_HostOnly( entity, false );
                    }
                    newSpeedGroup_HostOnly.CalculateUnitSpeedLimits_HostOnly();
                }
            }

            GameEntity_Squad.ReleaseTemporarySquadList( targetEntities );
            GameEntity_Squad.ReleaseTemporarySquadList( entitiesToAddToSpeedGroup );
        }
    }

    public class GameCommand_MoveManyToManyPoints : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            int debugNum = 0;

            OrderSource orderFom = OrderSource.Other;
            if ( command.FromActualInputEventOfPlayerID < 255 )
            {
                orderFom = OrderSource.HumanPlayer;
                //Debug.Log( "ORDER FROM HUMAN: MOVE" );
            }

            List<SafeSquadWrapper> targetEntities = GameEntity_Squad.GetTemporarySquadList( "GameCommand_MoveManyToManyPoints-targetEntities", 10f );
            if ( targetEntities == null ) //blocked for teardown/shutdown; bail
                return;
            try
            {
                Helper_GetListOfEntitiesWithSomeAsNull( command, targetEntities );
                debugNum = 1;
                if ( command.RelatedPoints == null )
                {
                    ArcenDebugging.ArcenDebugLog( "command.RelatedPoints == null in GameCommand_MoveManyToManyPoints!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                    return;
                }
                debugNum = 2;
                if ( command.RelatedPoints.Count < targetEntities.Count )
                {
                    ArcenDebugging.ArcenDebugLog( "command.RelatedPoints.Count(" +
                                                  command.RelatedPoints.Count + ") < " + targetEntities.Count + " in GameCommand_MoveManyToManyPoints!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                    return;
                }
                debugNum = 3;
                //Walk RelatedPoints with its struct enumerator in lockstep with the entity index
                //instead of snapshotting the ChainList into a temp ArcenPoint[] purely to index it
                //(ChainList has no indexer). The count check above guarantees there are at least
                //targetEntities.Count points available.
                ChainList<ArcenPoint>.Enumerator pointsEnum = command.RelatedPoints.GetEnumerator();
                for ( int i = 0; i < targetEntities.Count; i++ )
                {
                    if ( !pointsEnum.MoveNext() )
                        break;
                    ArcenPoint currentRelatedPoint = pointsEnum.Current;
                    debugNum = 5;
                    GameEntity_Squad entity = targetEntities[i].GetSquad();
                    if ( entity == null )
                        continue;
                    debugNum = 6;
                    entity.DecollisionMoveTarget = ArcenPoint.ZeroZeroPoint;
                    entity.NonSim_WorkingDecollisionMoveTarget = ArcenPoint.ZeroZeroPoint;
                    debugNum = 7;
                    EntityOrderCollection orders = entity.Orders;
                    if ( !command.ToBeQueued )
                        orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, orderFom == OrderSource.HumanPlayer ? ClearSource.YesClearAnyOrders_IncludingFromHumans : ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "MoveManyToManyPointsAndNotToBeQueued" );
                    else
                        orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.OnlyClearDecollisionOrdersAndNothingElse, ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "MoveManyToManyPointsButQueue" );
                    debugNum = 17;

                    ArcenPoint targetPoint = currentRelatedPoint;
                    if ( targetPoint == ArcenPoint.ZeroZeroPoint )
                        continue; //this is an invalid command for whatever reason, so ignore it.
                    debugNum = 18;
                    EntityOrder newOrder = EntityOrder.Create_Move_Normal( targetPoint, true, orderFom,
                        entity.CalculateShouldOrderBeIgnoredByFlagshipAsIfWasStationaryMode( command.RelatedMagnitude > 0 ) );
                    if ( newOrder.TypeData == null )
                        continue;
                    orders.QueueOrder( entity, newOrder );
                    debugNum = 18;
                    if ( entity.GuardOrPatrolOffsetPoints.Count > 0 )
                        entity.GuardOrPatrolOffsetPoints.Clear();
                    if ( orders.Behavior == EntityBehaviorType.Attacker_Full &&
                        entity.GetFactionTypeSafe() == FactionType.Player &&
                        entity.TypeData.IsMobileAndHasRepairOrAssistConstructionFlowsOrIsMobileMeleeUnit ) // used by engineer FRD to have a point to return to, and melee
                    {
                        debugNum = 19;
                        entity.GuardOrPatrolOffsetPoints.Add( targetPoint );
                    }

                    if ( orderFom == OrderSource.HumanPlayer )
                        entity.PreferredEntityTypeDataForTargeting = null;
                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLog( "Exception in GameCommand_MoveManyToMany. debug num " + debugNum +
                        ". error:\n" + e, Verbosity.ShowAsError );
            }
            finally
            {
                GameEntity_Squad.ReleaseTemporarySquadList( targetEntities );
            }
        }
    }

    public class GameCommand_DecollideUnits_MoveManyToMany : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            int debugNum = 0;

            List<SafeSquadWrapper> targetEntities = GameEntity_Squad.GetTemporarySquadList( "GameCommand_DecollideUnits_MoveManyToMany-targetEntities", 10f );
            if ( targetEntities == null ) //blocked for teardown/shutdown; bail
                return;
            try
            {
                Helper_GetListOfEntitiesWithSomeAsNull( command, targetEntities );
                debugNum = 1;
                if ( command.RelatedPoints == null )
                {
                    ArcenDebugging.ArcenDebugLog( "command.RelatedPoints == null in GameCommand_DecollideUnits_MoveManyToMany!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                    return;
                }
                debugNum = 2;
                if ( command.RelatedPoints.Count < targetEntities.Count )
                {
                    ArcenDebugging.ArcenDebugLog( "command.RelatedPoints.Count(" +
                                                  command.RelatedPoints.Count + ") < " + targetEntities.Count + " in GameCommand_DecollideUnits_MoveManyToMany!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                    return;
                }
                debugNum = 3;
                if ( command.RelatedIntegers == null )
                {
                    ArcenDebugging.ArcenDebugLog( "command.RelatedIntegers == null in GameCommand_DecollideUnits_MoveManyToMany with decollision!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                    return;
                }
                if ( command.RelatedIntegers.Count < targetEntities.Count )
                {
                    ArcenDebugging.ArcenDebugLog( "command.RelatedIntegers.Count(" +
                                                  command.RelatedIntegers.Count + ") < " + targetEntities.Count + " in GameCommand_DecollideUnits_MoveManyToMany with decollision!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                    return;
                }
                debugNum = 4;
                //Walk RelatedPoints and RelatedIntegers with their struct enumerators in lockstep
                //with the entity index instead of snapshotting both ChainLists into temp arrays
                //purely to index them (ChainList has no indexer). The count checks above guarantee
                //at least targetEntities.Count entries are available in each. Both MoveNexts are
                //evaluated every iteration (no short-circuit) so the two stay in sync.
                ChainList<ArcenPoint>.Enumerator pointsEnum = command.RelatedPoints.GetEnumerator();
                ChainList<int>.Enumerator integersEnum = command.RelatedIntegers.GetEnumerator();
                for ( int i = 0; i < targetEntities.Count; i++ )
                {
                    bool havePoint = pointsEnum.MoveNext();
                    bool haveInteger = integersEnum.MoveNext();
                    if ( !havePoint || !haveInteger )
                        break;
                    ArcenPoint currentRelatedPoint = pointsEnum.Current;
                    int currentRelatedInteger = integersEnum.Current;
                    debugNum = 5;
                    GameEntity_Squad entity = targetEntities[i].GetSquad();
                    if ( entity == null )
                    {
                        //ArcenDebugging.ArcenDebugLog( "Null entity, so skipping decollision.", Verbosity.ShowAsError );
                        continue;
                    }
                    debugNum = 6;
                    entity.DecollisionMoveTarget = ArcenPoint.ZeroZeroPoint;
                    entity.LastGameSecondOfDecollision_OrZeroIfMoreThan15sAgo = World_AIW2.Instance.GameSecond;
                    entity.NonSim_WorkingDecollisionMoveTarget = ArcenPoint.ZeroZeroPoint;
                    debugNum = 7;
                    {

                        debugNum = 8;
                        int IndexEntityWasOnWhenSetToDecollide = currentRelatedInteger;
                        if ( entity.Planet == null || entity.Planet.Index != IndexEntityWasOnWhenSetToDecollide )
                            continue; //this entity changed planets, so skip it!
                        if ( entity.GameSecondEnteredThisPlanet >= World_AIW2.Instance.GameSecond - 3 )
                            continue; //this entity has changed planets within the last 3 seconds, so skip it for that reason!

                        debugNum = 9;
                        EntityOrderCollection ordersExisting = entity.Orders;
                        debugNum = 10;
                        if ( ordersExisting.GetQueuedOrderCount() > 0 )
                        {
                            debugNum = 11;
                            EntityOrder order = ordersExisting.GetQueuedOrderAtIndex_OrNull( 0 );
                            if ( order.TypeData == null )
                                continue;
                            switch ( order.TypeData.Type )
                            {
                                //case EntityOrderType.Attack:
                                case EntityOrderType.Wormhole:
                                    {
                                        debugNum = 12;
                                        GameEntity_Other wormhole = order.GetWormholeToOtherPlanetOrNull( entity.Planet );
                                        if ( wormhole == null )
                                        {
                                            //ArcenDebugging.ArcenDebugLog( "Null wormhole, so skipping decollision for entity " + entity.PrimaryKeyID + " " + entity.TypeData.DisplayName + " on " + entity.GetPlanetName_Safe(), Verbosity.ShowAsError );
                                            continue; //wormhole is null, presumably because we've just gone through it
                                        }
                                        if ( wormhole.WorldLocation.GetHasAnyChanceOfBeingInRange( entity.WorldLocation, 2000 ) )
                                        {
                                            //ArcenDebugging.ArcenDebugLog( "Heading for wormhole, so skipping decollision for entity " + entity.PrimaryKeyID + " " + entity.TypeData.DisplayName + " on " + entity.GetPlanetName_Safe(), Verbosity.ShowAsError );
                                            continue; //if pretty close to the wormhole, then disregard our decollision order!
                                        }
                                    }
                                    break;
                            }
                        }
                        debugNum = 13;
                        World_AIW2.Instance.TotalDecollisionsRun++;

                        if ( currentRelatedPoint == ArcenPoint.ZeroZeroPoint )
                        {
                            //ArcenDebugging.ArcenDebugLog( "Empty location for decollision orders!", Verbosity.ShowAsError );
                            continue; //this one would be invalid...
                        }

                        entity.DecollisionMoveTarget = currentRelatedPoint;
                        //entity.DebugText = " dec" + entity.DecollisionMoveTarget;
                        if ( entity.MovedLastFrame && entity.DecollisionMoveTarget.X != 0 )
                        {
                            debugNum = 14;
                            if ( ordersExisting.GetQueuedOrderCount() > 0 )
                            {
                                EntityOrder order = ordersExisting.GetQueuedOrderAtIndex_OrNull( 0 );
                                if ( order.TypeData == null )
                                    continue;
                                switch ( order.TypeData.Type )
                                {
                                    case EntityOrderType.Move_Decollision:
                                        debugNum = 15;
                                        //We've calculated a "preferred" new target, so just use that
                                        ordersExisting.ReplaceQueuedOrderAtIndex( 0, EntityOrder.Create_Move_Decollision( entity.DecollisionMoveTarget, order.RelatedPlanetIndex,
                                            order.ShouldOverrideBehavior, order.Source, order.ShouldOrderBeIgnoredByFlagshipAsIfWasStationaryMode ) );
                                        //UnityEngine.Debug.Log( order.RelatedPoint + "  diff: " + ( entity.DecollisionMoveTarget - entity.WorldLocation ) ); //this is older logic that doesn't seem to work well anymore
                                        break;
                                        //case EntityOrderType.Attack:
                                        //case EntityOrderType.Wormhole:
                                        //    //same here, but just store the offset
                                        //    order.RelatedPoint = ( entity.DecollisionMoveTarget - entity.WorldLocation );
                                        //    break;
                                }
                            }
                        }
                    }

                    debugNum = 16;
                    EntityOrderCollection orders = entity.Orders;
                    orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.OnlyClearDecollisionOrdersAndNothingElse, ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "DecollideUnits_MoveManyToMany" );

                    //else
                    //    orders.ClearAnyRallyingBehaviors( ClearDecollisionOnParent.YesClear );
                    debugNum = 17;
                    EntityOrder newOrder = EntityOrder.Create_Move_Decollision( currentRelatedPoint, (Int16)currentRelatedInteger, true, OrderSource.Other, false );
                    if ( newOrder.TypeData == null )
                    {
                        //ArcenDebugging.ArcenDebugLog( "Null order from Create_Move_Decollision!", Verbosity.ShowAsError );
                        continue;
                    }
                    //newOrder.ShouldStationaryModeFlagshipExecuteThis = command.RelatedMagnitude > 0;
                    orders.QueueOrder( entity, newOrder );
                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLog( "Exception in GameCommand_DecollideUnits_MoveManyToMany. debug num " + debugNum +
                        ". error:\n" + e, Verbosity.ShowAsError );
            }
            finally
            {
                GameEntity_Squad.ReleaseTemporarySquadList( targetEntities );
            }
        }
    }
}
