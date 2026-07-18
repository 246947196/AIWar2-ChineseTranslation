using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class GameCommand_Attack : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            List<SafeSquadWrapper> targetEntities = GameEntity_Squad.GetTemporarySquadList( "GameCommand_Attack-targetEntities", 10f );
            if ( targetEntities == null ) //blocked for teardown/shutdown; bail
                return;
            Helper_GetListOfEntitiesWithSomeAsNull( command, targetEntities );

            OrderSource orderFom = OrderSource.Other;
            if ( command.FromActualInputEventOfPlayerID < 255 )
            {
                orderFom = OrderSource.HumanPlayer;
                //Debug.Log( "ORDER FROM HUMAN: ATTACK" );
            }

            GameEntity_Squad targetToAttack = null;
            if ( command.RelatedIntegers4 != null && command.RelatedIntegers4.Count > 0 )
            {
                targetToAttack = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedIntegers4.First );
            }

            for ( int i = 0; i < targetEntities.Count; i++ )
            {
                GameEntity_Squad entity = targetEntities[i].GetSquad();
                if ( entity == null || entity.Planet == null || entity.HasBeenRemovedFromSim || entity.ToBeRemovedAtEndOfThisFrame )
                    continue;
                if ( ShouldSkipEntityForPlanetOrderMismatch( command, entity, true, "Attack" ) )
                    continue;//if this is a queued command and the ship is going to be getting to this planet, allow the order to be queued
                EntityOrderCollection orders = entity.Orders;
                if ( !command.ToBeQueued )
                    orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, orderFom == OrderSource.HumanPlayer ? ClearSource.YesClearAnyOrders_IncludingFromHumans : ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "AttackNotToBeQueued" );
                else
                    orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.OnlyClearDecollisionOrdersAndNothingElse, ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "AttackAndQueue" );
                if ( targetToAttack != null )
                {
                    EntityOrder newOrder = EntityOrder.Create_Attack( targetToAttack.PrimaryKeyID, false, "GCAttack", true, orderFom,
                        entity.CalculateShouldOrderBeIgnoredByFlagshipAsIfWasStationaryMode( command.RelatedMagnitude > 0 ) );
                    if ( newOrder.TypeData == null )
                        continue;
                    orders.QueueOrder( entity, newOrder );
                    if ( orderFom == OrderSource.HumanPlayer ||
                         entity.ExoGalacticAttackTarget.GetSquad() != null ) //exos also will force the attack home
                        entity.PreferredEntityTypeDataForTargeting = targetToAttack.TypeData;
                }
                else
                {
                    if ( orderFom == OrderSource.HumanPlayer )
                        entity.PreferredEntityTypeDataForTargeting = null;
                }
            }

            GameEntity_Squad.ReleaseTemporarySquadList( targetEntities );
        }
    }

    public class GameCommand_Assist : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            List<SafeSquadWrapper> targetEntities = GameEntity_Squad.GetTemporarySquadList( "GameCommand_Assist-targetEntities", 10f );
            if ( targetEntities == null ) //blocked for teardown/shutdown; bail
                return;
            Helper_GetListOfEntitiesWithSomeAsNull( command, targetEntities );

            OrderSource orderFom = OrderSource.Other;
            if ( command.FromActualInputEventOfPlayerID < 255 )
            {
                orderFom = OrderSource.HumanPlayer;
                //Debug.Log( "ORDER FROM HUMAN: ASSIST" );
            }

            var assist_target = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedIntegers4.First );
            var assist_target_type = assist_target?.TypeData;

            for ( int i = 0; i < targetEntities.Count; i++ )
            {
                GameEntity_Squad entity = targetEntities[i].GetSquad();
                if ( entity == null || entity.Planet == null || entity.HasBeenRemovedFromSim || entity.ToBeRemovedAtEndOfThisFrame )
                    continue;
                if ( ShouldSkipEntityForPlanetOrderMismatch( command, entity, false, "Assist" ) )
                    continue;

                EntityOrderCollection orders = entity.Orders;
                if ( !command.ToBeQueued )
                    orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, orderFom == OrderSource.HumanPlayer ? ClearSource.YesClearAnyOrders_IncludingFromHumans : ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "AssistButNotToBeQueued" );
                else
                    orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.OnlyClearDecollisionOrdersAndNothingElse, ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "AssistAndQueue" );

                if ( command.RelatedIntegers4 != null && command.RelatedIntegers4.Count > 0 )
                {
                    
                    EntityOrder newOrder = EntityOrder.Create_Assist( command.RelatedIntegers4.First, false, "GCAssist", orderFom,
                        entity.CalculateShouldOrderBeIgnoredByFlagshipAsIfWasStationaryMode( command.RelatedMagnitude > 0 ) );
                    if ( newOrder.TypeData == null )
                        continue;
                    orders.QueueOrder( entity, newOrder );

                    if (assist_target_type != null)
                    {
                        entity.PreferredEntityTypeDataForTargeting = assist_target_type;
                    }
                }
            }

            GameEntity_Squad.ReleaseTemporarySquadList( targetEntities );
        }
    }

    public class GameCommand_SetTransportIntoLoadMode : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            List<SafeSquadWrapper> targetEntities = GameEntity_Squad.GetTemporarySquadList( "GameCommand_SetTransportIntoLoadMode-targetEntities", 10f );
            if ( targetEntities == null ) //blocked for teardown/shutdown; bail
                return;
            Helper_GetListOfEntitiesWithSomeAsNull( command, targetEntities );

            int numberOfTransportsCrippledForLocalPlayer = 0;
            for ( int i = 0; i < targetEntities.Count; i++ )
            {
                GameEntity_Squad entity = targetEntities[i].GetSquad();
                if ( entity == null || entity.Planet == null || entity.HasBeenRemovedFromSim || entity.ToBeRemovedAtEndOfThisFrame )
                    continue;
                Fleet fleet = entity.GetFleetOrNull_Safe();
                if ( fleet == null )
                    continue;

                if ( fleet.Centerpiece.GetSquad() == entity )
                {
                    //if ( fleet.NumberGameSecondsBeforeCanChangeTransportStatus > 0 )
                    //    continue;
                    if ( !fleet.IsFleetInTransportLoadMode && fleet.TimesInLoadModes_UIOnly < 100 )
                        fleet.TimesInLoadModes_UIOnly++; //don't bother tracking if too high
                    fleet.IsFleetInTransportLoadMode = true;
                    fleet.NumberGameSecondsBeforeCanBeAssisted = ExternalConstants.Instance.Balance_SecondsAfterTransportChangeBeforeCanSwitchBack;

                    if ( entity.GetIsCrippled() )
                    {
                        if ( entity.GetIsLocalFaction_Safe() )
                            numberOfTransportsCrippledForLocalPlayer++;
                    }
                }
            }
            if ( numberOfTransportsCrippledForLocalPlayer > 0 )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( numberOfTransportsCrippledForLocalPlayer +
                    " of your transports are currently crippled and cannot be loaded.  Repair them first!", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
            }
            GameEntity_Squad.ReleaseTemporarySquadList( targetEntities );
        }
    }

    public class GameCommand_UnloadTransports : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            var unloadTargets = GameEntity_Squad.GetTemporarySquadList( "GameCommand_UnloadTransports-targetEntities", 10f );
            if ( unloadTargets == null ) //blocked for teardown/shutdown; bail
                return;
            Helper_GetListOfEntitiesWithSomeAsNull( command, unloadTargets );

            if ( command.RelatedBools.Count <= 0 )
            {
                ArcenDebugging.ArcenDebugLog( "No ToBeQueued bool included! " + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                GameEntity_Squad.ReleaseTemporarySquadList( unloadTargets );

                return;
            }

            bool isQueued = command.RelatedBools.First;

            for ( int i = 0; i < unloadTargets.Count; i++ )
            {
                var transport = unloadTargets[i].GetSquad();
                if ( transport == null || transport.Planet == null || transport.HasBeenRemovedFromSim || transport.ToBeRemovedAtEndOfThisFrame )
                    continue;
                
                var fleet = transport.GetFleetOrNull_Safe();
                if ( fleet == null )
                    continue;

                if ( fleet.Centerpiece.GetSquad() != transport )
                    continue;

                if ( isQueued )
                {
                    var orders = transport.Orders;
                    if (orders != null && orders.GetQueuedOrderCount() > 0)
                    {
                        // only queue this if there is already at least one order in the hopper.
                        // otherwise just fall down below and do it directly.
                        EntityOrder newOrder = EntityOrder.Create_Unload_Transport( OrderSource.HumanPlayer, false );

                        //newOrder.ShouldStationaryModeFlagshipExecuteThis = command.RelatedMagnitude > 0;
                        orders.QueueOrder( transport, newOrder );
                        
                        continue;
                    }
                }

                fleet.IsFleetInTransportLoadMode = false;
                fleet.NumberGameSecondsBeforeCanBeAssisted = ExternalConstants.Instance.Balance_SecondsAfterTransportChangeBeforeCanSwitchBack;

                // Nothing to unload, done.
                if ( !fleet.GetHasAnyMobileFleetTransportContents() )
                    continue;

                // Handle currently loaded contents (unload them).
                fleet.FlagForForcedFullSyncToClients_FromHost();
                foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                {
                        if ( mem.TransportContents.Count == 0 )
                            continue;

                        Fireteam fireteam = null;
                        if ( transport.FireteamId > -1 )
                            fireteam = transport.GetFactionOrNull_Safe()?.BaseInfo.GetFireteamBaseById( transport.FireteamId ) as Fireteam;

                        for ( int j = 0; j < mem.TransportContents.Count; j++ )
                        {
                            var item = mem.TransportContents[j];
                            if ( item == null )
                                continue;

                            var info = World_AIW2.Instance.GetEntityRegistryInfo_Squad( item.GameEntityID );
                            if ( info != null && 
                                 info.Squad != null )
                            {
                                // Huh!  Apparently we already unloaded.
                                // Or maybe we were in the list of transporting entities twice.
                                // Either way, let's not make another of us
                                continue; 
                            }

                            var spawnedShip = item.CreateGameEntity_SquadFromMe_OrNull( transport, mem, context.GetHostOnlyContext() );
                            if ( spawnedShip != null )
                            {
                                if ( transport.Orders != null )
                                {
                                    bool copyBehavior = !spawnedShip.TypeData.IsDrone && 
                                                        !spawnedShip.TypeData.HasCustomMoveOrderAndNoOtherDirectCommandsCanBeGivenFromPlayer;
                                    transport.Orders.CopyTo( transport, spawnedShip, spawnedShip.Orders, copyBehavior, false, true );
                                }

                                if ( fireteam != null )
                                    fireteam.DeepInfo.AddUnit( spawnedShip );
                            }
                        }
                        
                        mem.TransportContents.Clear( true );
                    
                    }
            }

            GameEntity_Squad.ReleaseTemporarySquadList( unloadTargets );
        }
    }

    public class GameCommand_SetWormholePath : BaseGameCommand
    {
        private static readonly List<SafeSquadWrapper> entitiesToAddToSpeedGroup = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "GameCommand_SetWormholePath-entitiesToAddToSpeedGroup" );
        private static readonly List<Planet> workingPath = List<Planet>.Create_WillNeverBeGCed( 500, "GameCommand_SetWormholePath-workingPath" );

        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            List<SafeSquadWrapper> targetEntities = GameEntity_Squad.GetTemporarySquadList( "GameCommand_SetWormholePath-targetEntities", 10f );
            if ( targetEntities == null ) //blocked for teardown/shutdown; bail
                return;
            Helper_GetListOfEntitiesWithSomeAsNull( command, targetEntities );
            if ( command.RelatedIntegers == null )
            {
                GameEntity_Squad.ReleaseTemporarySquadList( targetEntities );
                ArcenDebugging.ArcenDebugLog( "command.RelatedIntegers == null in GameCommand_SetWormholePath!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }

            OrderSource orderFom = OrderSource.Other;
            if ( command.FromActualInputEventOfPlayerID < 255 )
            {
                orderFom = OrderSource.HumanPlayer;
                //Debug.Log( "ORDER FROM HUMAN: WORMHOLE" );
            }

            SpeedGroup newSpeedGroup_HostOnly = null;
            entitiesToAddToSpeedGroup.Clear();

            List<Planet> path = null;
            for ( int j = 0; j < targetEntities.Count; j++ )
            {
                GameEntity_Squad entity = targetEntities[j].GetSquad();
                if ( entity == null || !entity.TypeData.IsMobile )
                    continue;
                if ( entity.TypeData.FleetMembershipStyle == FleetMembershipStyle.Planetary )
                    continue;
                if ( entity.IsBlackHoledAtMoment.Display )
                    continue;
                if ( entity.Planet == null || entity.HasBeenRemovedFromSim || entity.ToBeRemovedAtEndOfThisFrame )
                    continue;
                if ( ShouldSkipEntityForPlanetOrderMismatch( command, entity, false, "SetWormholePath" ) )
                    continue;
                if ( command.RelatedFactionIndex != -1 &&
                     entity.GetFactionIndex_Safe() != command.RelatedFactionIndex )
                    continue; //we've moved this entity to a different faction (like hunter fleet donation)

                EntityOrderCollection orders = entity.Orders;
                if ( !command.ToBeQueued )
                    orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, orderFom == OrderSource.HumanPlayer ? ClearSource.YesClearAnyOrders_IncludingFromHumans : ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "SetWormholePathNotQueued" );
                else
                    orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.OnlyClearDecollisionOrdersAndNothingElse, ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "SetWormholePathYesQueued" );

                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                {
                    if ( entity.GetFactionTypeSafe() == FactionType.Player )
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

                if ( path == null )
                {
                    workingPath.Clear();
                    path = workingPath;
                    foreach ( int _ri_v in command.RelatedIntegers )
                    {
                        Planet targetPlanet = entity.Planet.ParentGalaxy.GetPlanetByIndex( (Int16)_ri_v );
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
                                    path = null;
                                    break;
                                }
                                break;
                            default:
                                if ( targetPlanet.IsBlockedToNPCTravelViaWormholes )
                                    path = null;
                                break;
                        }
                        if ( path == null )
                            break;
                        if ( targetPlanet == null )
                        {
                            path = null;
                            ArcenDebugging.ArcenDebugLogSingleLine( "BUG: null planet with index " + _ri_v + " in SetWormholePath::Execute (" + (command.RelatedString == null ? "null" : command.RelatedString) + ", ASK:" + command.RelatedMagnitude + ")", Verbosity.ShowAsError );
                            break;
                        }
                        path.Add( targetPlanet );
                    }
                }
                if ( path == null )
                    continue;
                for ( int i = 0; i < path.Count; i++ )
                {
                    EntityOrder newOrder = EntityOrder.Create_Wormhole( path[i].Index, true, command.RelatedBool, orderFom,
                        entity.CalculateShouldOrderBeIgnoredByFlagshipAsIfWasStationaryMode( command.RelatedMagnitude > 0 ) );
                    if ( newOrder.TypeData == null )
                        continue;
                    //if ( i == path.Count - 1 )
                    {
                        //Chris says: any of this that we set would be wiped out as they actually get to the new planet anyhow.

                        //if ( orders.Behavior == EntityBehaviorType.Attacker_Full && 
                        //    entity.GetFactionTypeSafe() == FactionType.Player &&
                        //    entity.TypeData.IsMobileAndHasRepairOrAssistConstructionFlowsOrIsMobileMeleeUnit )// used by engineer FRD to have a point to return to, and melee
                        //{
                        //    entity.GuardOrPatrolOffsetPoints.Clear();
                        //    entity.GuardOrPatrolOffsetPoints.Add( wormholeOnFarSideThatWeComeOutOf.WorldLocation ); 
                        //}
                    }
                    //only queue the order if it's valid from up above
                    orders.QueueOrder( entity, newOrder );
                }

                // If a human player orders a Warden/Hunter flagship to a different planet, record the
                // destination so the fleet fights to the death there instead of auto-retreating.
                if ( orderFom == OrderSource.HumanPlayer && entity.TypeData.IsFleetLeader && path.Count > 0 )
                {
                    Fleet fleet = entity.GetFleetOrNull_Safe();
                    if ( fleet != null && fleet.Behavior != FleetBehavior.UnderPlayerControl )
                        fleet.PlayerOverridePlanetIndex = path[path.Count - 1].Index;
                }
            }

            GameEntity_Squad.ReleaseTemporarySquadList( targetEntities );

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
        }
    }

    public class GameCommand_ClearAllOrdersOfTheseShips : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            List<SafeSquadWrapper> targetEntities = GameEntity_Squad.GetTemporarySquadList( "GameCommand_ClearAllOrdersOfTheseShips-targetEntities", 10f );
            if ( targetEntities == null ) //blocked for teardown/shutdown; bail
                return;
            Helper_GetListOfEntitiesWithSomeAsNull( command, targetEntities );
            if ( command.RelatedIntegers == null )
            {
                GameEntity_Squad.ReleaseTemporarySquadList( targetEntities );
                ArcenDebugging.ArcenDebugLog( "command.RelatedIntegers == null in GameCommand_ClearAllOrdersOfTheseShips!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }

            for ( int j = 0; j < targetEntities.Count; j++ )
            {
                GameEntity_Squad entity = targetEntities[j].GetSquad();
                if ( entity == null || !entity.TypeData.IsMobile )
                    continue;
                if ( entity.TypeData.FleetMembershipStyle == FleetMembershipStyle.Planetary )
                    continue;
                if ( entity.Planet == null || entity.HasBeenRemovedFromSim || entity.ToBeRemovedAtEndOfThisFrame )
                    continue;
                if ( command.RelatedFactionIndex != -1 &&
                     entity.GetFactionIndex_Safe() != command.RelatedFactionIndex )
                    continue; //we've moved this entity to a different faction (like hunter fleet donation)

                EntityOrderCollection orders = entity.Orders;
                orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.YesClearAnyOrders_IncludingFromHumans, "ClearAllOrdersOfTheseShips" );

            } //end for loop

            GameEntity_Squad.ReleaseTemporarySquadList( targetEntities );
        }
    }

    public class GameCommand_UnitSetCommands : BaseGameCommand
    {
        private static readonly List<SafeSquadWrapper> allowedGuardTargets = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 400, "GameCommand_UnitSetCommands-allowedGuardTargets" );
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedEntityIDs == null )
                return;
            Code commandCode = (Code)command.TypeData.ExternalCode;

            bool isFirst = true;
            foreach ( int id in command.RelatedEntityIDs )
            {
                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( id );
                if ( entity == null || entity.TypeData == null )
                    continue;
                Planet planet = entity.Planet;
                if ( planet == null )
                    continue;
                EntityOrderCollection orders = entity.Orders;
                if ( orders == null )
                    continue;
                PlanetFaction pFaction = entity.PlanetFaction;
                if ( pFaction == null )
                    continue;

                int debugStage = 0;
                try
                {
                    debugStage = 100;
                    switch ( commandCode )
                    {
                        case Code.SetHoldFireMode:
                        {
                            debugStage = 200;
                            entity.IsInHoldFireMode = command.RelatedBool;
                            if ( isFirst )
                            {
                                debugStage = 300;
                                isFirst = false;
                                if ( command.ShouldGenerateLocalUIFeedback )
                                    World_AIW2.Instance.QueueChatMessageOrCommand( (command.RelatedBool ? "Enabled" : "Disabled") + " hold fire mode for ship(s).", ChatType.ShowLocallyOnly, string.Empty, null );
                            }
                            break;
                        }
                        case Code.SetBehavior_FromFaction_SomeOtherReason:
                        case Code.SetBehavior_FromFaction_GoBackToGuardingCommand:
                        case Code.SetBehavior_FromFaction_ThreatGoesBackToSleep:
                        case Code.SetBehavior_FromFaction_AbandonBecauseLargeEnemyForce:
                        case Code.SetBehavior_FromFaction_ITractoredShips:
                        case Code.SetBehavior_FromFaction_NoPraetorianGuard:
                        case Code.SetBehavior_FromFaction_RunAwayWithEnemyShips:
                        case Code.SetBehavior_FromFaction_ThreatTimeToFight:
                        case Code.SetBehavior_FromFaction_SendOnThreatRaid:
                        case Code.SetBehavior_FromFaction_RetreatThreat:
                        case Code.SetBehavior_FromPlayer:
                        {
                            if ( commandCode == Code.SetBehavior_FromPlayer )
                            {
                                debugStage = 410;
                                bool toBeQueued = command.RelatedBools.Count <= 0 ? false : command.RelatedBools.First;
                                debugStage = 420;
                                if ( toBeQueued )
                                {
                                    debugStage = 430;
                                    if ( orders != null && orders.GetQueuedOrderCount() > 0 )
                                    {
                                        debugStage = 440;
                                        //only queue this if there is already at least one order in the hopper.
                                        //otherwise just fall down below and do it directly.
                                        EntityOrder newOrder = new EntityOrder( -1);
                                        switch ( (EntityBehaviorType)command.RelatedMagnitude )
                                        {
                                            case EntityBehaviorType.Stationary:
                                                newOrder = EntityOrder.Create_SetBehavior_Stationary( OrderSource.HumanPlayer, false );
                                                //newOrder.ShouldStationaryModeFlagshipExecuteThis = command.RelatedMagnitude > 0;
                                                orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.DoNotClearDecollision, 
                                                    ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "setting stationary");
                                                break;
                                            case EntityBehaviorType.Attacker_Full:
                                                newOrder = EntityOrder.Create_SetBehavior_Attacker_Full( OrderSource.HumanPlayer, false );
                                                //newOrder.ShouldStationaryModeFlagshipExecuteThis = command.RelatedMagnitude > 0;
                                                orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.DoNotClearDecollision,
                                                    ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "setting pursit" );
                                                break;
                                            case EntityBehaviorType.Attacker_PursueOnlyInRange:
                                                newOrder = EntityOrder.Create_SetBehavior_Attacker_PursueOnlyInRange( OrderSource.HumanPlayer, false );
                                                //newOrder.ShouldStationaryModeFlagshipExecuteThis = command.RelatedMagnitude > 0;
                                                orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.DoNotClearDecollision,
                                                    ClearSource.DoNotClearHumanOrders_AndDoNotClearBehaviorsIfHumanOrdersPresent, "setting attacker" );
                                                break;
                                        }
                                        debugStage = 480;

                                        if ( newOrder.TypeData != null )
                                        {
                                            orders.QueueOrder( entity, newOrder );
                                            continue; //skip doing the normal stuff for this one
                                        }
                                    }
                                }
                            }
                            debugStage = 500;
                            orders.SetBehaviorDirectlyInSim( (EntityBehaviorType)command.RelatedMagnitude, command.GetRelatedFaction()?.FactionIndex ?? -1 ); //is fine, main sim thread
                            debugStage = 600;
                            if ( orders.Behavior == EntityBehaviorType.Attacker_Full || orders.Behavior == EntityBehaviorType.Attacker_PursueOnlyInRange )
                            {
                                debugStage = 700;
                                entity.GuardedUnit.Clear();
                                entity.GuardOrPatrolOffsetPoints.Clear();
                                if ( entity.GetFactionTypeSafe() == FactionType.Player &&
                                    entity.TypeData.IsMobileAndHasRepairOrAssistConstructionFlowsOrIsMobileMeleeUnit )// used by engineer FRD to have a point to return to, and melee
                                {
                                    entity.GuardOrPatrolOffsetPoints.Add( entity.WorldLocation );
                                }
                            }

                            debugStage = 800;
                            if ( (EntityBehaviorType)command.RelatedMagnitude == EntityBehaviorType.Guard_Guardian_Patrolling ||
                                 (EntityBehaviorType)command.RelatedMagnitude == EntityBehaviorType.Guard_Guardian_Anchored )
                            {
                                debugStage = 900;
                                //This code path is currently only used for allowing Threat units to go back to guarding,
                                //and in particular threat units against minor factions we don't want to stay Threat against.
                                //If there are no valid anchor units to Guard ("valid" here is by convention; I don't want units guarding random things, that looks weird)
                                //then we just remove the behaviour related faction index (which is done automatically since the GameCommand doesn't specify a related faction)
                                GameEntity_Squad unitToGuard = null;
                                debugStage = 1000;
                                allowedGuardTargets.Clear();
                                foreach ( GameEntity_Squad guardpost in planet.Squads( EntityRollupType.ReinforcementLocations ) )
                                {
                                    if ( pFaction.GetIsFriendlyTowards(guardpost.PlanetFaction) ) {
                                        debugStage = 1100;
                                        allowedGuardTargets.Add( guardpost );
                                    }
                                }
                                debugStage = 1200;
                                GameEntity_Squad commandStation = planet.GetCommandStationOrNull();
                                if ( commandStation != null && commandStation.PlanetFaction.Faction.GetIsFriendlyTowards(pFaction.Faction) )
                                    allowedGuardTargets.Add( commandStation );
                                debugStage = 1300;
                                if ( allowedGuardTargets.Count > 0 )
                                    unitToGuard = allowedGuardTargets[context.RandomToUse.Next( 0, allowedGuardTargets.Count - 1 )].GetSquad();
                                debugStage = 1400;
                                if ( unitToGuard != null )
                                {
                                    debugStage = 1500;
                                    orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.YesClearAnyOrders_IncludingFromHumans, "UnitSetCommands" );
                                    //if we don't have anything to guard the game will kick the units back to 'attacker' which is okay
                                    debugStage = 1600;
                                    entity.GuardedUnit = LazyLoadSquadWrapper.Create( unitToGuard );

                                    debugStage = 1700;
                                    if ( (EntityBehaviorType)command.RelatedMagnitude == EntityBehaviorType.Guard_Guardian_Patrolling )
                                    {
                                        debugStage = 1800;
                                        //Set the patrol radius
                                        int minDistance = (entity.Planet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 050 )).IntValue;
                                        int maxDistance = (entity.Planet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 100 )).IntValue;
                                        int distance = context.RandomToUse.Next( minDistance, maxDistance );
                                        AngleDegrees initialAngle = unitToGuard.WorldLocation.GetAngleToDegrees( entity.WorldLocation );
                                        ArcenPoint startPoint = unitToGuard.WorldLocation.GetPointAtAngleAndDistance( initialAngle, distance );
                                        //                                ArcenDebugging.ArcenDebugLogSingleLine(entity.ToStringWithLocation() + " now guarding " + unitToGuard.ToStringWithLocation() + " one of " + allowedGuardTargets.Count + " targets. Patrol distance " + distance + " (min " + minDistance + " max " + maxDistance +"). Heading to start point " + startPoint, Verbosity.DoNotShow );

                                        debugStage = 1900;
                                        entity.GuardOrPatrolOffsetPoints.Add( startPoint - unitToGuard.WorldLocation );
                                        //use the more expensive distance method to ensure correctness here
                                        int step = UnityEngine.Mathf.RoundToInt( AngleDegrees.MAX_VALUE / 6 );
                                        for ( int j = step; j < AngleDegrees.MAX_VALUE; j += step )
                                        {
                                            debugStage = 2000;
                                            AngleDegrees angleToThisPoint = initialAngle.Add( AngleDegrees.Create( (float)j ) );
                                            ArcenPoint thisPoint = unitToGuard.WorldLocation.GetPointAtAngleAndDistance( angleToThisPoint, distance );
                                            entity.GuardOrPatrolOffsetPoints.Add( thisPoint - unitToGuard.WorldLocation );
                                        }
                                    }
                                    else
                                    {
                                        debugStage = 2100;
                                        //Tell the ship to go near the structure it's guarding
                                        int minDistance = (entity.Planet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 050 )).IntValue;
                                        int maxDistance = (entity.Planet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 100 )).IntValue;
                                        int distance = context.RandomToUse.Next( minDistance, maxDistance );
                                        AngleDegrees initialAngle = unitToGuard.WorldLocation.GetAngleToDegrees( entity.WorldLocation );
                                        ArcenPoint startPoint = unitToGuard.WorldLocation.GetPointAtAngleAndDistance( initialAngle, distance );
                                        debugStage = 2200;
                                        EntityOrder newOrder = EntityOrder.Create_Move_Normal( startPoint, true, OrderSource.Other, false );
                                        //newOrder.ShouldStationaryModeFlagshipExecuteThis = command.RelatedMagnitude > 0;
                                        debugStage = 2300;
                                        if ( newOrder.TypeData == null )
                                            continue;
                                        debugStage = 2400;
                                        orders.QueueOrder( entity, newOrder );
                                    }
                                }
                            }
                            //if ( isFirst )
                            //{
                            //    isFirst = false;
                            //    if ( command.ShouldGenerateLocalUIFeedback )
                            //        World_AIW2.Instance.QueueChatMessageOrCommand( "Set behavior for ship(s) to: " + orders.Behavior, "" );
                            //}
                            debugStage = 2500;
                            break;
                        }
                        case Code.SetWaiting:
                        {
                            debugStage = 2600;
                            if ( command.RelatedIntegers == null || command.RelatedIntegers.Count < 1 )
                                entity.StartedWaitingAtGameSecond = -1; // no longer waiting
                            else
                            {
                                debugStage = 2700;
                                if ( entity.WaitingAgainstPlanetIndex != command.RelatedIntegers.First )
                                {
                                    debugStage = 2800;
                                    entity.WaitingAgainstPlanetIndex = (Int16)command.RelatedIntegers.First;
                                    if ( entity.WaitingAgainstPlanetIndex < 0 )
                                    {
                                        debugStage = 2900;
                                        entity.StartedWaitingAtGameSecond = -1; // no longer waiting
                                                                                //so if RelatedBool is set, discard existing orders; these units are now free to have their behaviours take over
                                        if ( command.RelatedBool == true )
                                        {
                                            debugStage = 3000;
                                            EntityOrder order = entity.RemoveInvalidatedOrdersAndReturnFirstValid_IncludingDecollision();
                                            debugStage = 3100;
                                            if ( order.TypeData != null && order.TypeData.Type == EntityOrderType.Wormhole )
                                            {
                                                debugStage = 3200;
                                                orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.YesClearAnyOrders_IncludingFromHumans, "SetWaiting" );
                                            }
                                        }
                                    }
                                    else
                                    {
                                        debugStage = 3300;
                                        entity.StartedWaitingAtGameSecond = World_AIW2.Instance.GameSecond; // started waiting; note that this resets if the planet changes
                                    }
                                }
                                debugStage = 3400;
                            }
                            if ( isFirst )
                            {
                                debugStage = 3500;
                                isFirst = false;
                                if ( command.ShouldGenerateLocalUIFeedback )
                                    World_AIW2.Instance.QueueChatMessageOrCommand( "Set ship(s) waiting at planet.", ChatType.ShowLocallyOnly, string.Empty, null );
                            }
                            debugStage = 3600;
                            break;
                        }
                        case Code.SetActiveHack:
                        {
                            debugStage = 3700;
                            if ( command.RelatedIntegers4 == null || command.RelatedIntegers4.Count <= 0 )
                                break;
                            
                            debugStage = 3800;
                            if ( entity.ActiveHack != null )
                            {
                                debugStage = 3900;
                                Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.PlayLocallyOnly, SFXItemType_NonPositional.HackingFailed );
                                Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.PlayLocallyOnly, SFXItemType_NonPositional.HackingFinished );
                            }
                            
                            debugStage = 4000;
                            HackingType type = HackingTypeTable.Instance.GetRowByName( command.RelatedString2 );
                            
                            debugStage = 4100;
                            entity.ActiveHack = type;
                            {
                                int _ri4_t = 0, _ri4_p = 0;
                                int _ri4_i = 0;
                                foreach ( var _ri4_v in command.RelatedIntegers4 )
                                {
                                    if ( _ri4_i == 0 ) _ri4_t = _ri4_v;
                                    else if ( _ri4_i == 1 ) { _ri4_p = _ri4_v; break; }
                                    _ri4_i++;
                                }
                                entity.ActiveHack_Target = _ri4_t;
                                entity.ActiveHack_Planet = (Int16)_ri4_p;
                            }
                            entity.ActiveHack_DurationThusFar = 0;
                            
                            // Set up the Hacking Event
                            debugStage = 4300;
                            {
                                GameEntityTypeData target = null;
                                Int16 hackedFaction = -1;
                                
                                debugStage = 4400;
                                if ( !(entity.ActiveHack.HackIsAgainstPlanet || entity.ActiveHack.ChooseASpecificPlanetToTarget) )
                                {
                                    debugStage = 4500;
                                    var targetSquad = World_AIW2.Instance.GetEntityByID_Squad( entity.ActiveHack_Target );
                                    if ( targetSquad == null )
                                        break;
                                    
                                    debugStage = 4600;
                                    target = targetSquad.TypeData;
                                    hackedFaction = World_AIW2.Instance.GetEntityByID_Squad( entity.ActiveHack_Target ).GetFactionIndex_Safe();
                                }
                                
                                debugStage = 4700;
                                entity.ActiveHackEvent = HackingEvent.Create( 
                                    pFaction.Faction.FactionIndex, 
                                    hackedFaction, 
                                    entity.ActiveHack_Planet,
                                    HackingTypeTable.Instance.GetRowByName( command.RelatedString2 ), 
                                    target, 
                                    type.HackIsAgainstPlanet,
                                    command.RelatedString, 
                                    command.RelatedIntegers3.Count <= 0 ? -1 : command.RelatedIntegers3.First );

                                // additional numbers past the first are passed along into the RelatedInt list
                                {
                                    int _ri3_n = 0;
                                    foreach ( var _ri3_v in command.RelatedIntegers3 )
                                    {
                                        if ( _ri3_n >= 1 )
                                            entity.ActiveHackEvent.RelatedInt.Add(_ri3_v);
                                        _ri3_n++;
                                    }
                                }
                            }

                            debugStage = 4800;
                            if ( isFirst )
                            {
                                debugStage = 4900;
                                isFirst = false;
                                if ( command.ShouldGenerateLocalUIFeedback )
                                {
                                    debugStage = 5000;
                                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.PlayLocallyOnly, SFXItemType_NonPositional.HackingStarted );
                                    string addedText = string.Empty;
                                    if ( entity.ActiveHack.RelatedStringIsAShipType && entity.ActiveHackEvent != null && entity.ActiveHackEvent.RelatedStringOrNull != null && entity.ActiveHackEvent.RelatedStringOrNull.Length > 0 )
                                    {
                                        debugStage = 5100;
                                        GameEntityTypeData relatedTypeData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( entity.ActiveHackEvent.RelatedStringOrNull );
                                        addedText = " (" + (relatedTypeData == null ? "null" : relatedTypeData.DisplayName) + ")";
                                    }
                                    
                                    debugStage = 5200;
                                    if ( ArcenNetworkAuthority.GetIsHostMode() )
                                    {
                                        if ( entity.ActiveHack.HackCompletesInstantly )
                                        {
                                            FactionViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<FactionViewChatHandlerBase>( "HackingHistory" );
                                            if ( chatHandlerOrNull != null )
                                                chatHandlerOrNull.Faction = LazyLoadFactionWrapper.Create( entity.GetFactionOrNull_Safe() );

                                            World_AIW2.Instance.QueueChatMessageOrCommand( "Performed hack of type: <color=#a1ff22>" + entity.ActiveHack.DisplayName + "</color>" + addedText,
                                                ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                                        }
                                        else
                                        {
                                            SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                                            if ( chatHandlerOrNull != null )
                                                chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( entity );

                                            World_AIW2.Instance.QueueChatMessageOrCommand( "开始入侵，类型：<color=#a1ff22>" + entity.ActiveHack.DisplayName + "</color>" + addedText,
                                                ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                                        }
                                    }
                                }
                            }
                            
                            debugStage = 5300;

                            if ( entity.ActiveHack.HackCompletesInstantly )
                            {
                                GameEntity_Squad targetSquad = World_AIW2.Instance.GetEntityByID_Squad( entity.ActiveHack_Target );
                                Planet hackPlanet = World_AIW2.Instance.CurrentGalaxy.GetPlanetByIndex( entity.ActiveHack_Planet );
                                if ( entity.ActiveHack.Implementation.DoSuccessfulCompletionLogic_CalledFromMainSimOnly( targetSquad, hackPlanet, entity, context.GetHostOnlyContext(), entity.ActiveHack,
                                    entity.ActiveHackEvent ) )
                                {
                                    entity.ActiveHack = null;
                                    entity.ActiveHack_Target = 0;
                                    entity.ActiveHack_Planet = -1;
                                    entity.ActiveHack_DurationThusFar = 0;
                                    entity.ActiveHackEvent.HackEndTime = World_AIW2.Instance.GameSecond;
                                    Faction facOrNull = entity.GetFactionOrNull_Safe();
                                    if ( facOrNull != null )
                                        facOrNull.HackingHistory.Add( entity.ActiveHackEvent );
                                    entity.ActiveHackEvent = null;

                                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.PlayLocallyOnly, SFXItemType_NonPositional.HackingSucceeded );
                                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.PlayLocallyOnly, SFXItemType_NonPositional.HackingFinished );
                                }
                            }
                            
                            break;
                        }
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "GameCommand_UnitSetCommands exception at debugStage " + debugStage + ", Error: " + e, Verbosity.ShowAsError ); ;
                }
            }
        }
    }

    public class GameCommand_SetStopToShootAnySeenTargets : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedEntityIDs == null )
                return;
            if ( command.RelatedBools.Count <= 0 )
            {
                ArcenDebugging.ArcenDebugLog( "No ToBeQueued bool included! " + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            bool toBeQueued = command.RelatedBools.First;
            foreach ( int id in command.RelatedEntityIDs )
            {
                GameEntity_Squad ship = World_AIW2.Instance.GetEntityByID_Squad( id );
                if ( ship == null || ship.TypeData == null || ship.Planet == null )
                    continue;
                if ( toBeQueued )
                {
                    EntityOrderCollection orders = ship.GetEffectiveOrders();
                    if ( orders != null && orders.GetQueuedOrderCount() > 0 )
                    {
                        //only queue this if there is already at least one order in the hopper.
                        //otherwise just fall down below and do it directly.
                        EntityOrder newOrder = command.RelatedBool ?
                            EntityOrder.Create_SetBehavior_StopToShootAnySeenTargets_On( OrderSource.HumanPlayer, false ) :
                            EntityOrder.Create_SetBehavior_StopToShootAnySeenTargets_Off( OrderSource.HumanPlayer, false );
                        //newOrder.ShouldStationaryModeFlagshipExecuteThis = command.RelatedMagnitude > 0;
                        orders.QueueOrder( ship, newOrder );
                        continue;
                    }
                }
                ship.StopToShootAnySeenTargets = command.RelatedBool;
            }
        }
    }

    public class GameCommand_ToggleEnabled : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedEntityIDs == null )
                return;
            foreach ( int id in command.RelatedEntityIDs )
            {
                GameEntity_Squad ship = World_AIW2.Instance.GetEntityByID_Squad( id );
                if ( ship == null || ship.TypeData == null || ship.Planet == null )
                    continue;
                ship.IsInHoldFireMode = !command.RelatedBool;
            }
        }
    }

    public class GameCommand_PlaceSelfBuildingUnit : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            //World_AIW2.Instance.QueueChatMessageOrCommand( "Place self-building unit " + ArcenNetworkAuthority.DesiredStatus, ChatType.ShowLocallyOnly, string.Empty, null );

            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //client will hear about it as a fast-blast!

            if ( command.RelatedEntityIDs == null )
            {
                ArcenDebugging.ArcenDebugLog( "During placement: No RelatedEntityIDs == null!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedEntityIDs.Count <= 0 )
            {
                ArcenDebugging.ArcenDebugLog( "During placement: No RelatedEntityIDs!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedIntegers.Count <= 0 )
            {
                ArcenDebugging.ArcenDebugLog( "During placement: No RelatedIntegers!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            GameEntityTypeData typeToPlace = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( typeToPlace == null )
            {
                ArcenDebugging.ArcenDebugLog( "During placement: typeToPlace == null!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            GameEntity_Squad entityDoingThePlacing = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );
            if ( entityDoingThePlacing == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "Construction source died prior to construction start -- cannot build this " + typeToPlace.GetDisplayName() + " now.", 
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            Fleet fleetThatThisGoesInto = World_AIW2.Instance.GetFleetByID( command.RelatedIntegers.First );
            if ( fleetThatThisGoesInto == null )
            {
                ArcenDebugging.ArcenDebugLog( "During placement: fleetThatThisGoesInto == null!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }

            int overridingSquadCapFromUnusualConstructor = -1;
            if ( command.RelatedIntegers2 != null && command.RelatedIntegers2.Count > 0 )
                overridingSquadCapFromUnusualConstructor = command.RelatedIntegers2.First;

            FleetMembership fleetMembershipForTypeUnlessFleetChanges = fleetThatThisGoesInto.GetButDoNotAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( typeToPlace );
            bool addedOrChangedFleetMembership = false;
            if ( fleetMembershipForTypeUnlessFleetChanges == null && overridingSquadCapFromUnusualConstructor > 0 )
            {
                fleetMembershipForTypeUnlessFleetChanges = fleetThatThisGoesInto.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( typeToPlace );
                addedOrChangedFleetMembership = true;
            }

            if ( fleetMembershipForTypeUnlessFleetChanges == null )
            {
                ArcenDebugging.ArcenDebugLog( "Error During placement: fleetMembershipForType == null!  Was this from an MP client? " + command.WriteToStringInefficient( true, true, true ), Verbosity.DoNotShow );
                return;
            }

            if ( addedOrChangedFleetMembership )
            {
                Faction localPlayerFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                int unused = 0;
                fleetMembershipForTypeUnlessFleetChanges.PerFrame_CalculateEffectiveFleetData_P1(
                    fleetMembershipForTypeUnlessFleetChanges.Fleet.Centerpiece.GetSquad(), localPlayerFactionOrNull,
                    ref unused );
                if ( unused > 0 ) { }
            }

            if ( typeToPlace.IsCommandStation && entityDoingThePlacing.Planet.GetCommandStationOrNull() != null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "这里已经有一个指挥站了——你不能放置另一个！", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }

            if ( command.RelatedPoints == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedPoints == null in GameCommand_PlaceSelfBuildingUnit", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( command.RelatedPoints.Count < 1 )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedPoints.Count < 1 in GameCommand_PlaceSelfBuildingUnit", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            //World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedMagnitude:" + command.RelatedMagnitude, ChatType.ShowLocallyOnly, string.Empty, null );
            for ( int i = 0; i < command.RelatedMagnitude; i++ )
            {
                ArcenRejectionReason rejectionReason = fleetMembershipForTypeUnlessFleetChanges.GetCanBuildAnother( true, overridingSquadCapFromUnusualConstructor, ExtraFromStacks.IncludePrecalc );

                if ( rejectionReason != ArcenRejectionReason.Unknown && command.ShouldGenerateLocalUIFeedback )
                {
                    switch ( rejectionReason )
                    {
                        case ArcenRejectionReason.FactionDoesNotHaveEnoughCap:
                            World_AIW2.Instance.QueueChatMessageOrCommand( "已达到数量上限：" + typeToPlace.DisplayName + '！', ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                            break;
                        case ArcenRejectionReason.GalaxyWideCapForPlayersHasBeenHit:
                            World_AIW2.Instance.QueueChatMessageOrCommand( "已达到全银河数量上限：" + typeToPlace.DisplayName + '！', ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                            break;
                        case ArcenRejectionReason.FactionDoesNotHaveCapForThisFleet:
                            World_AIW2.Instance.QueueChatMessageOrCommand( "当前舰队未设置数量上限：" + typeToPlace.DisplayName, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                            break;
                        case ArcenRejectionReason.FactionDoesNotHaveEnoughEnergy:
                            World_AIW2.Instance.QueueChatMessageOrCommand( "能量不足，无法放置 " + typeToPlace.DisplayName, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                            break;
                        case ArcenRejectionReason.NotEnoughCitySockets:
                            World_AIW2.Instance.QueueChatMessageOrCommand( "没有足够的 " + typeToPlace.NameForCitySockets_Short_Plural + " 在 " + typeToPlace.NameForCityCenter + " 的 " + fleetMembershipForTypeUnlessFleetChanges.Fleet.GetName() + " 中来放置 " + typeToPlace.DisplayName +
                                '（' + fleetMembershipForTypeUnlessFleetChanges.Fleet.CalculateRemainingCitySockets() + " 可用，需要 " + typeToPlace.CitySocketCost + "）。通常升级你的城市以增加数量。", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                            break;
                        case ArcenRejectionReason.FleetCenterpieceIsMissing:
                            World_AIW2.Instance.QueueChatMessageOrCommand( "舰队没有核心单位：" + typeToPlace.DisplayName, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                            break;
                        case ArcenRejectionReason.FleetDoesNotContainThisType:
                            World_AIW2.Instance.QueueChatMessageOrCommand( "舰队设计不包含 " + typeToPlace.DisplayName, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                            break;
                        default:
                            World_AIW2.Instance.QueueChatMessageOrCommand( "放置期间，拒绝原因：" + rejectionReason, ChatType.ShowLocallyOnly, null );
                            break;
                    }
                    break;
                }
                if ( rejectionReason != ArcenRejectionReason.Unknown )
                    continue;

                Faction placingFaction = fleetThatThisGoesInto.Faction;
                if ( typeToPlace.CostInResourceOne > 0 && placingFaction != null )
                {
                    if ( placingFaction.StoredFactionResourceOne < typeToPlace.CostInResourceOne )
                    {
                        if ( command.ShouldGenerateLocalUIFeedback )
                        {
                            PlayerTypeData pTypeData = placingFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
                            string resourceName = pTypeData != null ? pTypeData.Resource1DisplayName : "Resource 1";
                            World_AIW2.Instance.QueueChatMessageOrCommand( "不足 " + resourceName + "；你需要 " + typeToPlace.CostInResourceOne +
                                "，你的派系仅有 " + placingFaction.StoredFactionResourceOne, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                        }
                        break;
                    }
                }
                if ( typeToPlace.CostInResourceTwo > 0 && placingFaction != null )
                {
                    if ( placingFaction.StoredFactionResourceTwo < typeToPlace.CostInResourceTwo )
                    {
                        if ( command.ShouldGenerateLocalUIFeedback )
                        {
                            PlayerTypeData pTypeData = placingFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
                            string resourceName = pTypeData != null ? pTypeData.Resource2DisplayName : "Resource 2";
                            World_AIW2.Instance.QueueChatMessageOrCommand( "不足 " + resourceName + "；你需要 " + typeToPlace.CostInResourceTwo +
                                "，你的派系仅有 " + placingFaction.StoredFactionResourceTwo, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                        }
                        break;
                    }
                }
                if ( typeToPlace.CostInResourceThree > 0 && placingFaction != null )
                {
                    if ( placingFaction.StoredFactionResourceThree < typeToPlace.CostInResourceThree )
                    {
                        if ( command.ShouldGenerateLocalUIFeedback )
                        {
                            PlayerTypeData pTypeData = placingFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
                            string resourceName = pTypeData != null ? pTypeData.Resource3DisplayName : "Resource 3";
                            World_AIW2.Instance.QueueChatMessageOrCommand( "不足 " + resourceName + "；你需要 " + typeToPlace.CostInResourceThree +
                                "，你的派系仅有 " + placingFaction.StoredFactionResourceThree, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                        }
                        break;
                    }
                }

                ArcenPoint placementPoint = command.RelatedPoints.First;
                ArcenPoint effectivePlacementPoint = placementPoint;
                int radiusToCheck = typeToPlace.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius;
                Planet planet = entityDoingThePlacing.Planet;

                {
                    int totalFailsAllowed = 1000;
                    int failsLeft = totalFailsAllowed;
                    bool foundEligibleSpot = false;
                    while ( failsLeft > 0 )
                    {
                        failsLeft--;
                        if ( planet.GetIsPlacementPointSafe( typeToPlace, effectivePlacementPoint, true ) )
                        {
                            foundEligibleSpot = true;
                            break;
                        }
                        effectivePlacementPoint = placementPoint.GetRandomPointWithinDistance( context.RandomToUse, 1, (totalFailsAllowed - failsLeft) * 10 );
                    }
                    if ( !foundEligibleSpot )
                    {
                        World_AIW2.Instance.QueueChatMessageOrCommand( "During placement: Could not find any eligible spot to place this: " + typeToPlace.DisplayName, ChatType.ShowLocallyOnly, null );
                        break;
                    }
                }

                //entityDoingThePlacing is based around a mobile fleet for command stations
                //entityDoingThePlacing is based around the command station of the planet when Zenith Trader

                byte markLevel = fleetMembershipForTypeUnlessFleetChanges.EffectiveMark;
                GameEntity_Squad newEntity;
                newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(
                        entityDoingThePlacing.PlanetFaction, typeToPlace, markLevel,
                        fleetThatThisGoesInto,
                        0, //self building units don't get put into fleet sub-groups ever
                        effectivePlacementPoint, context.GetHostOnlyContext(), "PlaceSelfBuildingUnit" );

                if ( typeToPlace.CostInResourceOne > 0 && placingFaction != null )
                    placingFaction.StoredFactionResourceOne -= typeToPlace.CostInResourceOne;
                if ( typeToPlace.CostInResourceTwo > 0 && placingFaction != null )
                    placingFaction.StoredFactionResourceTwo -= typeToPlace.CostInResourceTwo;
                if ( typeToPlace.CostInResourceThree > 0 && placingFaction != null )
                    placingFaction.StoredFactionResourceThree -= typeToPlace.CostInResourceThree;

                newEntity.HullPointsLost = newEntity.GetCurrentHullPoints() - 1;
                newEntity.ShieldPointsLost = newEntity.GetCurrentShieldPoints();
                newEntity.SetCurrentMarkLevel( markLevel );
                newEntity.SelfBuildingMetalRemaining = (FInt)newEntity.GetMetalCost();
                if ( command.RelatedBool ) //used to say "build immediately"; generally for cheat/debug
                    newEntity.SelfBuildingMetalRemaining = FInt.One;

                if ( newEntity.FleetMembership == null )
                    fleetMembershipForTypeUnlessFleetChanges.AddEntityToFleetMembership( newEntity, "Place Self-Building Unit" );

                if ( newEntity.TypeData.IsCommandStation )
                {
                    Fleet fleet = newEntity.GetFleetOrNull_Safe();
                    if ( fleet != null )
                    {
                        fleet.ResetAllExplictShipCapsForMemberships();
                        fleet.SetAllMembershipsUpFromDesignTemplates_HostOnly( null, newEntity.TypeData.FleetDesignLogicIGrantOneOf,
                            newEntity.TypeData.FleetDesignTemplatesIAlwaysGrant );
                        fleet.FlagForForcedFullSyncToClients_FromHost(); 
                    }
                }

                fleetThatThisGoesInto.FlagForForcedFullSyncToClients_FromHost();
            }
        }
    }
}
