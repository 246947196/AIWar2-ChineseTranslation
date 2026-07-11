using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Linq;
using System.Text;

namespace Arcen.AIW2.External
{
    public static class EndpointFunctions
    {
        private delegate bool ProcessSquadForFit( GameEntity_Squad Squad );

        #region GetListOfUnitsToGiveOrdersTo
        private enum IfEqual
        {
            Yes,
            No
        }

        private enum SelectedType
        {
            Normal,
            EnableDisabledList
        }

        private enum FinalList
        {
            JustThoseToChange,
            AllShips
        }

        private static void GetListOfUnitsToGiveOrdersTo( List<SafeSquadWrapper> ListToFill, IfEqual IfEq, SelectedType SelType, FinalList FinalL, out bool ThereWereMoreYes, 
            ProcessSquadForFit MatchesYesState, ProcessSquadForFit OptionalShipsToOutRightExclude )
        {
            ListToFill.Clear();

            List<SafeSquadWrapper> workingUnitsToGiveOrdersTo_Yes = GameEntity_Squad.GetTemporarySquadList( "GetListOfUnitsToGiveOrdersTo-workingUnitsToGiveOrdersTo_Yes", 10f );
            if ( workingUnitsToGiveOrdersTo_Yes == null ) //blocked for teardown/shutdown; bail
            {
                ThereWereMoreYes = false;
                return;
            }
            List<SafeSquadWrapper> workingUnitsToGiveOrdersTo_No = GameEntity_Squad.GetTemporarySquadList( "GetListOfUnitsToGiveOrdersTo-workingUnitsToGiveOrdersTo_No", 10f );
            if ( workingUnitsToGiveOrdersTo_No == null ) //blocked for teardown/shutdown; bail
            {
                GameEntity_Squad.ReleaseTemporarySquadList( workingUnitsToGiveOrdersTo_Yes );
                ThereWereMoreYes = false;
                return;
            }

            if ( SelType == SelectedType.EnableDisabledList )
            {
                //sort everything into two bins: yes and no states
                foreach ( GameEntity_Squad ship in Engine_AIW2.Instance.SelectedSquadsICanGiveEnableDisableOrdersTo )
                {
                    if ( OptionalShipsToOutRightExclude != null )
                    {
                        if ( OptionalShipsToOutRightExclude( ship ) )
                            continue;
                    }
                    if ( MatchesYesState( ship ) )
                        workingUnitsToGiveOrdersTo_Yes.Add( ship );
                    else
                        workingUnitsToGiveOrdersTo_No.Add( ship );
                }
            }
            else
            {
                //sort everything into two bins: yes and no states
                foreach ( GameEntity_Squad ship in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
                {
                    if ( OptionalShipsToOutRightExclude != null )
                    {
                        if ( OptionalShipsToOutRightExclude( ship ) )
                            continue;
                    }
                    if ( MatchesYesState( ship ) )
                        workingUnitsToGiveOrdersTo_Yes.Add( ship );
                    else
                        workingUnitsToGiveOrdersTo_No.Add( ship );
                }
            }

            bool moreYes = workingUnitsToGiveOrdersTo_Yes.Count > workingUnitsToGiveOrdersTo_No.Count;
            if ( !moreYes )
            {
                //yikes, tiebreaker
                if ( workingUnitsToGiveOrdersTo_Yes.Count == workingUnitsToGiveOrdersTo_No.Count )
                    moreYes = ( IfEq == IfEqual.Yes );
            }

            //if there are more yes states than no, then return the yes states to be turned into nos
            if ( moreYes )
            {
                if ( FinalL == FinalList.AllShips )
                    workingUnitsToGiveOrdersTo_Yes.AddRange( workingUnitsToGiveOrdersTo_No );
                workingUnitsToGiveOrdersTo_No.Clear();
                ThereWereMoreYes = true;
                ListToFill.AddRange( workingUnitsToGiveOrdersTo_Yes );
                GameEntity_Squad.ReleaseTemporarySquadList( workingUnitsToGiveOrdersTo_Yes );
                GameEntity_Squad.ReleaseTemporarySquadList( workingUnitsToGiveOrdersTo_No );
            }
            //or if there are more no states than yes, then return the no states to be turned into yeses
            else
            {
                if ( FinalL == FinalList.AllShips )
                    workingUnitsToGiveOrdersTo_No.AddRange( workingUnitsToGiveOrdersTo_Yes );
                workingUnitsToGiveOrdersTo_Yes.Clear();
                ThereWereMoreYes = false;
                ListToFill.AddRange( workingUnitsToGiveOrdersTo_No );
                GameEntity_Squad.ReleaseTemporarySquadList( workingUnitsToGiveOrdersTo_Yes );
                GameEntity_Squad.ReleaseTemporarySquadList( workingUnitsToGiveOrdersTo_No );
            }
        }
        #endregion

        #region SplitSelection
        public static void SplitSelection()
        {
            //if ( Engine_Universal.RunStatus == RunStatus.GameStart )
            //    return;
            if ( !World.Instance.IsLoaded )
                return;

            Dictionary<GameEntityTypeData, int> splitWorkingShipCounts = GameEntityTypeData.GetTemporaryGameEntityTypeDataIntDict( "EndpointFunctions-SplitSelection-splitWorkingShipCounts", 10f );
            if ( splitWorkingShipCounts == null ) //blocked for teardown/shutdown; bail
                return;

            //Form a dictionary mapping from EntityTypeData to NumberOfShips
            foreach ( GameEntity_Squad selected in Engine_AIW2.Instance.SelectedSquadsEvenIncludingOnesICannotGiveOrdersTo )
            {
                splitWorkingShipCounts[selected.TypeData]++;
            }
            //Now that I've counted all the ships, iterate over the count and halve each number. This is the
            //number of ships to keep in the gruop. Note that I always round up (so a group with 1 golem will keep the golem, for example)
            foreach ( KeyValuePair<GameEntityTypeData, int> kv in splitWorkingShipCounts )
            {
                int newVal = kv.Value;
                if( newVal % 2 == 1)
                    newVal++;
                newVal /= 2;
                splitWorkingShipCounts[kv.Key] = newVal;
            }
            //Now iterate over the selected ships and choose the ones to remove
            List<SafeSquadWrapper> shipsToRemove = GameEntity_Squad.GetTemporarySquadList( "EndpointFunctions-SplitSelection-shipsToRemove", 10f );
            if ( shipsToRemove == null ) //blocked for teardown/shutdown; bail
            {
                GameEntityTypeData.ReleaseTemporaryGameEntityTypeDataIntDict( splitWorkingShipCounts );
                return;
            }

            foreach ( GameEntity_Squad selected in Engine_AIW2.Instance.SelectedSquadsEvenIncludingOnesICannotGiveOrdersTo )
            {
                if( splitWorkingShipCounts[selected.TypeData] > 0)
                {
                    splitWorkingShipCounts[selected.TypeData]--;
                    continue;
                }
                shipsToRemove.Add(selected);
            }
            //Now remove ships from the selection
            for(int i = 0; i < shipsToRemove.Count; i++)
            {
                shipsToRemove[i].Unselect(false, "SplitSelection" );
            }

            GameEntity_Squad.ReleaseTemporarySquadList( shipsToRemove );
            GameEntityTypeData.ReleaseTemporaryGameEntityTypeDataIntDict( splitWorkingShipCounts );
        }
        #endregion

        #region TogglePursuitMode_FromPlayer
        public static void TogglePursuitMode_FromPlayer( GameCommandSource Source )
        {
            if ( !World.Instance.IsLoaded )
                return;

            EntityBehaviorType targetType = EntityBehaviorType.Attacker_Full;
            bool moreShipsWereInPursuitModeAlready;
            List<SafeSquadWrapper> shipsToToggle = GameEntity_Squad.GetTemporarySquadList( "EndpointFunctions-TogglePursuitMode_FromPlayer-shipsToToggle", 10f );
            if ( shipsToToggle == null ) //blocked for teardown/shutdown; bail
                return;
            GetListOfUnitsToGiveOrdersTo( shipsToToggle, IfEqual.Yes, SelectedType.Normal, FinalList.JustThoseToChange, out moreShipsWereInPursuitModeAlready,
                delegate ( GameEntity_Squad ship )
                {
                    return ship.Orders.Behavior == targetType;
                },
                //exclude these:
                delegate ( GameEntity_Squad ship )
                {
                    if ( !ship.TypeData.IsMobile || ship.DataForMark.Speed <= 0 ||
                        ship.TypeData.IsMobileOrbiter || ship.TypeData.IsPlanetaryOrbiter ||
                        ship.TypeData.HasCustomMoveOrderAndNoOtherDirectCommandsCanBeGivenFromPlayer )
                        return true; 
                    return false;
                } );

            if ( shipsToToggle.Count == 0 )
            {
                GameEntity_Squad.ReleaseTemporarySquadList( shipsToToggle );
                return; //nothing to order around!
            }

            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetBehavior_FromPlayer], Source );
            foreach ( SafeSquadWrapper wrap in shipsToToggle )
                command.RelatedEntityIDs.Add( wrap.PrimaryKeyID );

            command.RelatedBools.Add( InputCaching.inputAddToSelection.CalculateIsKeyDownNow_IgnoreConflicts() ); //ToBeQueued
            command.RelatedMagnitude = moreShipsWereInPursuitModeAlready ? (int)EntityBehaviorType.Stationary : (int)targetType;
            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

            GameEntity_Squad.ReleaseTemporarySquadList( shipsToToggle );
        }
        #endregion

        #region ToggleAttackMove_FromPlayer
        public static void ToggleAttackMove_FromPlayer( GameCommandSource Source )
        {
            if ( !World.Instance.IsLoaded )
                return;

            EntityBehaviorType targetType = EntityBehaviorType.Attacker_PursueOnlyInRange;
            bool moreShipsWereInAttackMoveAlready;
            List<SafeSquadWrapper> shipsToToggle = GameEntity_Squad.GetTemporarySquadList( "EndpointFunctions-ToggleAttackMove_FromPlayer-shipsToToggle", 10f );
            if ( shipsToToggle == null ) //blocked for teardown/shutdown; bail
                return;
            GetListOfUnitsToGiveOrdersTo( shipsToToggle, IfEqual.Yes, SelectedType.Normal, FinalList.JustThoseToChange, out moreShipsWereInAttackMoveAlready,
                delegate ( GameEntity_Squad ship )
                {
                    return ship.Orders.Behavior == targetType;
                },
                //exclude these:
                delegate ( GameEntity_Squad ship )
                {
                    if ( !ship.TypeData.IsMobile || ship.DataForMark.Speed <= 0 ||
                        ship.TypeData.IsMobileOrbiter || ship.TypeData.IsPlanetaryOrbiter  ||
                        ship.TypeData.HasCustomMoveOrderAndNoOtherDirectCommandsCanBeGivenFromPlayer )
                        return true; 
                    return false;
                } );

            if ( shipsToToggle.Count == 0 )
            {
                GameEntity_Squad.ReleaseTemporarySquadList( shipsToToggle );
                return; //nothing to order around!
            }

            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetBehavior_FromPlayer], Source );
            foreach ( SafeSquadWrapper wrap in shipsToToggle )
                command.RelatedEntityIDs.Add( wrap.PrimaryKeyID );

            command.RelatedBools.Add( InputCaching.inputAddToSelection.CalculateIsKeyDownNow_IgnoreConflicts() ); //ToBeQueued
            command.RelatedMagnitude = moreShipsWereInAttackMoveAlready ? (int)EntityBehaviorType.Stationary : (int)targetType;
            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

            GameEntity_Squad.ReleaseTemporarySquadList( shipsToToggle );
        }
        #endregion

        #region ToggleStopToShootAnySeenTargets
        public static void ToggleStopToShootAnySeenTargets( Faction ForFaction, GameCommandSource Source )
        {
            if ( !World.Instance.IsLoaded )
                return;

            bool moreShipsWereInStopToShootAlready;
            List<SafeSquadWrapper> shipsToToggle = GameEntity_Squad.GetTemporarySquadList( "EndpointFunctions-ToggleStopToShootAnySeenTargets-shipsToToggle", 10f );
            if ( shipsToToggle == null ) //blocked for teardown/shutdown; bail
                return;
            GetListOfUnitsToGiveOrdersTo( shipsToToggle, IfEqual.Yes, SelectedType.Normal, FinalList.JustThoseToChange, out moreShipsWereInStopToShootAlready,
                delegate ( GameEntity_Squad ship )
                {
                    return ship.StopToShootAnySeenTargets;
                },
                //exclude these:
                delegate ( GameEntity_Squad ship )
                {
                    if ( !ship.TypeData.IsMobile || ship.DataForMark.Speed <= 0 ||
                        ship.TypeData.IsMobileOrbiter || ship.TypeData.IsPlanetaryOrbiter  ||
                        ship.TypeData.HasCustomMoveOrderAndNoOtherDirectCommandsCanBeGivenFromPlayer )
                        return true; 

                    return false;
                } );

            if ( shipsToToggle.Count == 0 )
            {
                GameEntity_Squad.ReleaseTemporarySquadList( shipsToToggle );
                return; //nothing to order arond!
            }

            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetStopToShootAnySeenTargets], Source );
            foreach ( SafeSquadWrapper wrap in shipsToToggle )
                command.RelatedEntityIDs.Add( wrap.PrimaryKeyID );

            command.RelatedBools.Add( InputCaching.inputAddToSelection.CalculateIsKeyDownNow_IgnoreConflicts() ); //ToBeQueued
            command.RelatedBool = !moreShipsWereInStopToShootAlready;
            World_AIW2.Instance.QueueGameCommand( ForFaction, command, true );

            GameEntity_Squad.ReleaseTemporarySquadList( shipsToToggle );
        }
        #endregion

        #region ToggleShipsEnabled
        public static void ToggleShipsEnabled( Faction ForFaction, GameCommandSource Source )
        {
            if ( !World.Instance.IsLoaded )
                return;

            bool moreShipsWereEnabledAlready;
            List<SafeSquadWrapper> shipsToToggle = GameEntity_Squad.GetTemporarySquadList( "EndpointFunctions-ToggleShipsEnabled-shipsToToggle", 10f );
            if ( shipsToToggle == null ) //blocked for teardown/shutdown; bail
                return;
            GetListOfUnitsToGiveOrdersTo( shipsToToggle, IfEqual.No, SelectedType.EnableDisabledList, FinalList.JustThoseToChange, out moreShipsWereEnabledAlready,
                delegate ( GameEntity_Squad ship )
                {
                    return !ship.IsInHoldFireMode;
                }, 
                //exclude these:
                delegate ( GameEntity_Squad ship )
                {
                    if ( ship.TypeData.HasCustomMoveOrderAndNoOtherDirectCommandsCanBeGivenFromPlayer )
                        return true; 

                    return false;
                });

            if ( shipsToToggle.Count == 0 )
            {
                GameEntity_Squad.ReleaseTemporarySquadList( shipsToToggle );
                return; //nothing to order around!
            }

            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.ToggleEnabled], Source );
            foreach ( SafeSquadWrapper wrap in shipsToToggle )
                command.RelatedEntityIDs.Add( wrap.PrimaryKeyID );

            command.RelatedBool = !moreShipsWereEnabledAlready;
            World_AIW2.Instance.QueueGameCommand( ForFaction, command, true );

            GameEntity_Squad.ReleaseTemporarySquadList( shipsToToggle );
        }
        #endregion

        #region ToggleSpeedGroupForPlayerShipsONLY
        public static void ToggleSpeedGroupForPlayerShipsONLY( Faction ForFaction, GameCommandSource Source )
        {
            if ( !World.Instance.IsLoaded )
                return;

            bool moreShipsWereInGroupMoveAlready;
            List<SafeSquadWrapper> shipsToToggle = GameEntity_Squad.GetTemporarySquadList( "EndpointFunctions-ToggleSpeedGroupForPlayerShipsONLY-shipsToToggle", 10f );
            if ( shipsToToggle == null ) //blocked for teardown/shutdown; bail
                return;
            GetListOfUnitsToGiveOrdersTo( shipsToToggle, IfEqual.No, SelectedType.Normal, FinalList.AllShips, out moreShipsWereInGroupMoveAlready,
                delegate ( GameEntity_Squad ship )
                {
                    if ( (ship.GroupMoveSpeed_HostOnly != null && !ship.GroupMoveSpeed_HostOnly.IsDummy) || //on host only
                           ship.SpeedLimitFromGroupMove > 0 ) //on client or host
                        return true; //yes already in group move
                    return false; //no not already in group move
                },
                //exclude these:
                delegate ( GameEntity_Squad ship )
                {
                    if ( !ship.TypeData.IsMobile || ship.DataForMark.Speed <= 0 ||
                        ship.TypeData.IsMobileOrbiter || ship.TypeData.IsPlanetaryOrbiter  ||
                        ship.TypeData.HasCustomMoveOrderAndNoOtherDirectCommandsCanBeGivenFromPlayer )
                        return true; 

                    return false;
                } );

            if ( shipsToToggle.Count == 0 )
            {
                GameEntity_Squad.ReleaseTemporarySquadList( shipsToToggle );
                return; //nothing to order around!
            }

            if ( moreShipsWereInGroupMoveAlready )
            {
                //remove the existing speed group
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.CreateSpeedGroup_Player], Source );
                command.RelatedString = "DestroySpeedGroups"; //delete the speed group
                foreach ( SafeSquadWrapper wrap in shipsToToggle )
                    command.RelatedEntityIDs.Add( wrap.PrimaryKeyID );
                World_AIW2.Instance.QueueGameCommand( ForFaction, command, true );
            }
            else
            {
                //create a new speed group
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.CreateSpeedGroup_Player], Source );
                command.RelatedString = "PlayerStyle"; //when this is not set, we would use the NPC/AI style instead
                foreach ( SafeSquadWrapper wrap in shipsToToggle )
                    command.RelatedEntityIDs.Add( wrap.PrimaryKeyID );
                World_AIW2.Instance.QueueGameCommand( ForFaction, command, true );
            }

            GameEntity_Squad.ReleaseTemporarySquadList( shipsToToggle );
        }
        #endregion

        #region Order_Stop
        public static void Order_Stop( Faction ForFaction, GameCommandSource Source )
        {
            if ( !World.Instance.IsLoaded )
                return;
            
            var cmd = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.Stop], Source );
            
            foreach ( GameEntity_Squad e in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
            {
                cmd.RelatedEntityIDs.Add(e.PrimaryKeyID);
            }
            
            if (cmd.RelatedEntityIDs.Count > 0)
                World_AIW2.Instance.QueueGameCommand( ForFaction, cmd, true );
            else
                cmd.ReturnToPool();
        }
        #endregion
        /*
        #region Order_Attack
        public static void Order_Attack( Faction ForFaction, GameCommandSource Source )
        {
            if ( !World.Instance.IsLoaded )
                return;
        }
        #endregion
        
        #region Order_Defend
        public static void Order_Defend( Faction ForFaction, GameCommandSource Source )
        {
            LOG.Line(); 
            
            if ( !World.Instance.IsLoaded )
                return;
            
            var row = TargetedInputActionTypeTable.Instance.GetRowByName("DefendClickedTarget");
            if (row != null)
            {
                string reason;
                if (!row.Handle.CanBegin(out reason))
                {
                    if (!string.IsNullOrEmpty(reason))
                        World_AIW2.Instance.QueueChatMessageOrCommand( reason, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                    return;
                }
                
                Engine_AIW2.Instance.PendingTargetedAction = row.Handle;
            }
        }
        #endregion
        
        #region Order_Patrol
        public static void Order_Patrol( Faction ForFaction, GameCommandSource Source )
        {
            LOG.Line(); 
            
            if ( !World.Instance.IsLoaded )
                return;
        }
        #endregion
        */
        #region Order_CustomSystem
        public static void Order_CustomSystem( Faction ForFaction, GameCommandSource Source, int ordinal )
        {
            LOG.Line(); 
            
            if ( !World.Instance.IsLoaded )
                return;
            
            int numFound = 0;
            int debugstage = 0;
            
            try
            {
                debugstage = 100;
                
                foreach ( GameEntity_Squad e in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
                {
                    debugstage = 200;
                    foreach (var s in e.Systems)
                    {
                        debugstage = 300;
                        if (s.CustomSystem != null)
                        {
                            debugstage = 400;
                            if (s.CustomSystem.Type.InputNumber == ordinal)
                            {
                                (s.CustomSystem as ExternalData_CustomSystem).UserToggle();

                                debugstage = 1300;
                                numFound++;
                                break;
                            }
                        }
                    }

                    debugstage = 1400;
                    if (numFound > 0)
                        break;

                    debugstage = 1500;
                }
                
                debugstage = 2000;
            }
            catch (Exception e)
            {
                LOG.Err("Exception in EndpoingFunctions.Order_CustomSystem debugstage {0}\n{1}", debugstage, e);
            }
        }
        #endregion

        #region DeselectAnySelectedIfDoesNotMatchThisFleet
        public static void DeselectAnySelectedIfDoesNotMatchThisFleet( Fleet fleetToMatch, bool OnlyDeselectIndividualShips )
        {
            if ( !World.Instance.IsLoaded )
                return;
            if ( !OnlyDeselectIndividualShips )
            {
                for ( int i = 0; i < World_AIW2.Instance.PlayerFleets.Count; i++ )
                {
                    Fleet data = World_AIW2.Instance.PlayerFleets[i];
                    if ( data.IsConsideredSelected_NonSim && data != fleetToMatch )
                        data.IsConsideredSelected_NonSim = false;
                }
            }
            {
                var e = Engine_AIW2.Instance.SelectedSquadsEvenIncludingOnesICannotGiveOrdersTo.GetEnumerator();
                while ( e.MoveNext() )
                {
                    GameEntity_Squad entity = e.Current;
                    //NOTE!!  IF you try to call Unselect() or ForceUnselect() during a DFSelected, it won't get everything!
                    //        Instead, you should just return RemoveAndContinue for anything you want to deselect, and that will work fine.
                    if ( entity == null || entity.HasBeenRemovedFromSim || entity.TypeData == null || entity.FleetMembership == null )
                    {
                        if ( entity != null )
                            entity.ReasonIterationRemoved = "DeadDuringDeselectCheck";
                        e.RemoveCurrent(); continue;
                    }
                    if ( entity.GetFleetOrNull_Safe() != fleetToMatch )
                    {
                        entity.ReasonIterationRemoved = "DeselectAllNotMatchingFleet";
                        e.RemoveCurrent(); continue;
                    }
                }
            }
        }
        #endregion

        #region DeselectAnySelectedIfDoesYesActuallyMatchThisFleet
        public static void DeselectAnySelectedIfDoesYesActuallyMatchThisFleet( Fleet fleetToMatch, bool OnlyDeselectIndividualShips )
        {
            if ( !World.Instance.IsLoaded )
                return;
            if ( !OnlyDeselectIndividualShips )
            {
                for ( int i = 0; i < World_AIW2.Instance.PlayerFleets.Count; i++ )
                {
                    Fleet data = World_AIW2.Instance.PlayerFleets[i];
                    if ( data.IsConsideredSelected_NonSim && data == fleetToMatch )
                        data.IsConsideredSelected_NonSim = false;
                }
            }
            {
                var e = Engine_AIW2.Instance.SelectedSquadsEvenIncludingOnesICannotGiveOrdersTo.GetEnumerator();
                while ( e.MoveNext() )
                {
                    GameEntity_Squad entity = e.Current;
                    //NOTE!!  IF you try to call Unselect() or ForceUnselect() during a DFSelected, it won't get everything!
                    //        Instead, you should just return RemoveAndContinue for anything you want to deselect, and that will work fine.
                    if ( entity == null || entity.HasBeenRemovedFromSim || entity.TypeData == null || entity.FleetMembership == null )
                    {
                        if ( entity != null )
                            entity.ReasonIterationRemoved = "DeadDuringDeselectCheck";
                        e.RemoveCurrent(); continue;
                    }
                    if ( entity.GetFleetOrNull_Safe() == fleetToMatch )
                    {
                        entity.ReasonIterationRemoved = "DeselectAllYesMatchingFleet";
                        e.RemoveCurrent(); continue;
                    }
                }
            }
        }
        #endregion

        public static void UnloadTransports( Faction ForFaction, GameCommandSource Source )
        {
            if ( !World.Instance.IsLoaded )
                return;
            
            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.UnloadTransports], Source );

            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null)
            {
                command.ReturnToPool();
                return;
            }
            foreach ( Fleet fleet in World_AIW2.Instance.Fleets( localFaction, FleetStatus.CenterpieceMustLive ) )
            {
                //if not selected at a fleet level AND centerpiece not selected, then ignore me
                if ( !fleet.IsConsideredSelected_NonSim && (fleet.Centerpiece.GetSquad() == null || !fleet.Centerpiece.GetSquad().GetIsSelected() ) )
                    continue;

                //if not already in the transport mode, then skip!
                if ( !fleet.IsFleetInTransportLoadMode )
                {
                    //...unless it somehow has ships in it
                    if ( fleet.GetMobileFleetTransportContentsCount() <= 0 )
                        continue;
                }

                GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();

                //if no centerpiece, then skip this!
                if ( centerpiece == null )
                    continue;

                command.RelatedEntityIDs.Add( centerpiece.PrimaryKeyID );
            }

            if ( command.RelatedEntityIDs.Count > 0 )
            {
                command.RelatedBools.Add( InputCaching.inputAddToSelection.CalculateIsKeyDownNow_IgnoreConflicts() ); //ToBeQueued
                //ArcenDebugging.ArcenDebugLogSingleLine( "Original: " + command.WriteToStringInefficient( true, true, true ), Verbosity.DoNotShow );
                World_AIW2.Instance.QueueGameCommand( ForFaction, command, true );
            }
            else //prevents having a leak!
                command.ReturnToPool();
        }

        public static void LoadTransports( Faction ForFaction, GameCommandSource Source )
        {
            if ( !World.Instance.IsLoaded )
                return;

            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.SetTransportIntoLoadMode], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );

            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null )
            {
                command.ReturnToPool();
                return;
            }
            foreach ( Fleet fleet in World_AIW2.Instance.Fleets( localFaction, FleetStatus.CenterpieceMustLive ) )
            {
                //if not selected at a fleet level AND centerpiece not selected, then ignore me
                if ( !fleet.IsConsideredSelected_NonSim && ( fleet.Centerpiece.GetSquad() == null || !fleet.Centerpiece.GetSquad().GetIsSelected() ) )
                    continue;
                //if already in the transport mode, then skip!
                if ( fleet.IsFleetInTransportLoadMode )
                    continue;

                GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                //if no centerpiece, then skip this!
                if ( centerpiece == null )
                    continue;

                command.RelatedEntityIDs.Add( centerpiece.PrimaryKeyID );
            }
            
            command.PlanetOrderWasIssuedFrom = Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed();
            command.ToBeQueued = Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.Additive );

            if ( command.RelatedEntityIDs.Count > 0 )
                World_AIW2.Instance.QueueGameCommand( ForFaction, command, true );
            else //prevents having a leak!
                command.ReturnToPool();            
        }

        public static void ToggleGalaxyMap()
        {
            if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                return;
            switch ( Engine_AIW2.Instance.CurrentGameViewMode )
            {
                case GameViewMode.MainGameView:
                    Engine_AIW2.Instance.PresentationLayer.ReactToLeavingPlanetView( Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() );
                    Engine_AIW2.Instance.SetCurrentGameViewMode( GameViewMode.GalaxyMapView );
                    break;
                case GameViewMode.GalaxyMapView:
                    Engine_AIW2.Instance.SetCurrentGameViewMode( GameViewMode.MainGameView );
                    Engine_AIW2.Instance.PresentationLayer.ReactToEnteringPlanetView( Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() );
                    break;
            }
        }

        public static void OpenEncyclopedia_AndSearch()
        {
            Window_UnitEncyclopedia.Instance.Open();
        }

        public static void OpenGalaxyMap_AndSearch()
        {
            if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                return;
            
            if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.MainGameView )
            {
                Engine_AIW2.Instance.PresentationLayer.ReactToLeavingPlanetView( Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() );
                Engine_AIW2.Instance.SetCurrentGameViewMode( GameViewMode.GalaxyMapView );
            }

            var func = GalaxyMapTextboxFunctionTable.Instance.GetRowByName("FindAnything");
            PlayerAccount_AIW2.SetCurrentGalaxyMapTextboxFunctionSafe(func);
            ArcenUI_Input e = Window_BottomLeftGalaxyMap.iSearchOrSimilar.Instance.Element as ArcenUI_Input;
            e.ReferenceInputField.onFocusSelectAll = true;
            e.Focus();
        }

        public static void TogglePause( Faction ForFaction, GameCommandSource Source, bool firstUnpauseOfGame = false)
        {
            //LOG.Msg("{0}() called. ForFaction={1}, Source={2}, firstUnpauseOfGame={3} called from:\n{4}", 
            //        LOG.MethodName(), ForFaction.OrNull(), Extensions.ToString(Source), firstUnpauseOfGame, LOG.StackTrace(10));
            
            if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                return;
            if ( Source == GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer ) 
            {
                if ( World_AIW2.Instance.IsOutsideOfNormalGameplay )
                    return; //if the player says it, then we don't care about much else unless it's outside of normal gameplay
            }
            else //if something other than the player said it, then this needs to be marked as the first unpause to do that unpause.
            {
                //this function is used to pause/unpause the game, and also to Start the game for the first time.
                //If the game hasn't been started, we will only unpause for the GameStart button in Window_GameSetup.cs
                if ( World_AIW2.Instance.GameSecond <= 0 && !firstUnpauseOfGame )
                    return;
            }
            //ArcenDebugging.ArcenDebugLog( "TogglePause: " , Verbosity.DoNotShow ); //PAUSE_TODO
            GameCommand command = GameCommand.Create ( BaseGameCommand.CommandsByCode[World.Instance.IsPaused ? BaseGameCommand.Code.UnpauseOnly : BaseGameCommand.Code.PauseOnly], Source );
            World_AIW2.Instance.QueueGameCommand( ForFaction, command, true );
        }

        public static void Debug_ScoutAll( Faction ForFaction, GameCommandSource Source )
        {
            if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                return;
            GameCommand command = GameCommand.Create ( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.Debug_RevealAll], Source );
            World_AIW2.Instance.QueueGameCommand( ForFaction, command, true );
        }
        public static void Debug_ExploreAll( Faction ForFaction, GameCommandSource Source )
        {
            if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                return;
            GameCommand command = GameCommand.Create ( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.Debug_ExploreAll], Source );
            World_AIW2.Instance.QueueGameCommand( ForFaction, command, true );
        }

        public static void ScrapSelectedUnits( Faction ForFaction, GameCommandSource Source )
        {
            if ( !World.Instance.IsLoaded )
                return;

            // jcf: We could try to actually show the ships that would be scrapped instead of a big empty
            //      dialog box... but, it would take more work than i've done here to make that look nice
            //      and its really wierd when it shows things like transports that actually wont be scrapped
            //      and in fact have a separate message afterwards saying they werent.
            /*
            ArcenCharacterBuffer tmp = ArcenCharacterBuffer.GetFromPoolOrCreate("ScrapSelectedUnits.tmp");
            tmp.Add("The following units you have selected will be scrapped:");
            tmp.Add("\n");
            var tmpdict = GameEntityTypeData.GetTemporaryGameEntityTypeDataIntDict("ScrapSelectedUnits.tmpdict", 5.0f);
            */

            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.ScrapUnits], Source );

            foreach ( GameEntity_Squad ship in Engine_AIW2.Instance.SelectedSquadsEvenIncludingOnesICannotGiveOrdersTo )
            {
                if (ship.GetIsFactionControlledByLocalPlayerAccount_Safe() ||
                    ship.TypeData.CanBeGivenOrdersByAnyPlayerEvenIfNotOwner ||
                    Engine_AIW2.Instance.IsTestChamber)
                {
                    //tmpdict[ship.TypeData] += ship.ExtraStackedSquadsInThis + 1;
                    command.RelatedEntityIDs.Add( ship.PrimaryKeyID );
                }
            }

            if (command.RelatedEntityIDs.Count == 0)
            {
                command.ReturnToPool();
                return;
            }
                
            ModalPopupData.CreateAndLogYesNoStyle( 
                ()=>
                {
                    World_AIW2.Instance.QueueGameCommand( ForFaction, command, true );
                }, 
                null, 
                "拆解选中的单位？", "你确定要拆解当前选中的单位吗？", "是的，销毁它们", "不，等等！" );
        }

        public static void TogglePlanetFactionBooleanFlagAtCurrentPlanet( Faction ForFaction, GameCommandSource Source, PlanetFactionBooleanFlag Flag)
        {
            if ( !World.Instance.IsLoaded )
                return;
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null )
                return;
            Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
            PlanetFaction faction = planet.GetPlanetFactionForFaction( localFaction );
            if ( faction == null )
                return;
            GameCommand command = GameCommand.Create ( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.ChangePlanetFactionBooleanFlag], Source );
            command.RelatedFactionIndex = faction.Faction.FactionIndex;
            command.RelatedIntegers.Add( planet.Index );
            command.RelatedIntegers2.Add( (int)Flag );
            command.RelatedBool = !faction.GetPlanetFactionBooleanFlag( Flag );
            World_AIW2.Instance.QueueGameCommand( ForFaction, command, true );
        }

        public static void TogglePlanetFactionBooleanFlag( Faction ForFaction, PlanetFaction pFaction, GameCommandSource Source, PlanetFactionBooleanFlag Flag )
        {
            if ( !World.Instance.IsLoaded )
                return;
            if ( pFaction == null )
                return;
            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.ChangePlanetFactionBooleanFlag], Source );
            command.RelatedFactionIndex = pFaction.Faction.FactionIndex;
            command.RelatedIntegers.Add( pFaction.PlanetIndex );
            command.RelatedIntegers2.Add( (int)Flag );
            command.RelatedBool = !pFaction.GetPlanetFactionBooleanFlag( Flag );
            World_AIW2.Instance.QueueGameCommand( ForFaction, command, true );
        }

        public static void QuickSelect(QuickSelectionType SelectionType)
        {
            if ( !World.Instance.IsLoaded )
                return;
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null )
                return;
            Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
            if ( planet == null )
                return;
            EntityRollupType rollup;
            MetalFlowPurpose flowType = MetalFlowPurpose.None;
            switch ( SelectionType )
            {
                case QuickSelectionType.MobileMilitary:
                    rollup = EntityRollupType.MobileCombatants;
                    break;
                case QuickSelectionType.CommandStation:
                    rollup = EntityRollupType.CommandStation;
                    break;
                case QuickSelectionType.FactoriesForPlayerMobileFleets:
                    rollup = EntityRollupType.HasAnyMetalFlows;
                    flowType = MetalFlowPurpose.FactoryConstructionForPlayerMobileFleets;
                    break;
                case QuickSelectionType.CloakingUnits:
                    rollup = EntityRollupType.HasAnyInternalCloakingAbility;
                    break;
                case QuickSelectionType.MobileFlagships:
                    rollup = EntityRollupType.FleetLeaders;
                    break;
                case QuickSelectionType.BattlestationsAndCitadels:
                    rollup = EntityRollupType.Battlestation;
                    break;
                case QuickSelectionType.Snipers:
                    rollup = EntityRollupType.SniperRangeIsPossibleEver;
                    break;
                case QuickSelectionType.NonFlagships:
                    rollup = EntityRollupType.MobileNonFlagshipCombatants;
                    break;
                case QuickSelectionType.Melee:
                    rollup = EntityRollupType.MeleeRangeIsPossibleEver;
                    break;
                case QuickSelectionType.TractorUnits:
                    rollup = EntityRollupType.TractorSource;
                    break;
                case QuickSelectionType.Engineers:
                    rollup = EntityRollupType.Engineers;
                    break;
                default:
                    return;
            }

            bool unselectingInstead = false;
            if ( Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.Additive ) )
            { }
            else if ( Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.Subtractive ) )
            {
                unselectingInstead = true;
            }
            else
            {
//                Engine_AIW2.Instance.ClearSelection( SelectionCommandScope.CurrentPlanet_UnlessViewingGalaxy );
                //clear the entire previious selection
                Engine_AIW2.Instance.ClearSelection( true, true );
            }

            for ( int i = 0; i < planet.Factions.Count; i++ )
            {
                PlanetFaction faction = planet.Factions[i];
                if ( !faction.Faction.Config.IsFactionControlledByLocalPlayer )
                    continue;
                //ArcenDebugging.ArcenDebugLogSingleLine( "Quick select: " + rollup, Verbosity.DoNotShow );
                foreach ( GameEntity_Squad entity in faction.Entities.Squads( rollup ) )
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine( "entity: " + entity.TypeData.InternalName + " check", Verbosity.DoNotShow );
                    if ( flowType != MetalFlowPurpose.None &&
                         !entity.DataForMark.GetMetalFlow( flowType ).HasRealData )
                        continue;
                    if ( rollup == EntityRollupType.MobileCombatants && !entity.TypeData.CanGoThroughWormholes )
                        continue; //Don't allow "Select All Military" to grab Fortresses or things like that
                    if ( rollup == EntityRollupType.FleetLeaders && !entity.TypeData.CanGoThroughWormholes )
                        continue; // this selection is for 'mobile' fleet leaders, not command stations
                    //ArcenDebugging.ArcenDebugLogSingleLine( "entity: " + entity.TypeData.InternalName + " do select or unselect!", Verbosity.DoNotShow );
                    if ( unselectingInstead )
                        entity.Unselect( false, "QuickSelectUnselect" );
                    else
                        entity.Select( false, "QuickSelect" );
                }
            }
        }

        public static bool DoDisabledButtonBasedOnGameStatusCyclingIfNeeded( IArcenUIElementForSizing MustBeAboveOrBelow )
        {
            if ( ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() )
            {
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( MustBeAboveOrBelow, "<color=#ff6935>请稍等，游戏仍在从上次退出中刷新。</color>" );
                return true;
            }
            return false;
        }

        public static int CalculatePredictedGameLoad( ArcenCharacterBufferBase OptionalExplainCalculation, bool SummarizeExplanation )
        {
            float calculatedLoad = 0;

            int unknownFactionCount = 0;
            float loadFromFactions = 0;
            int knownFactionCount = 0;
            Faction fac;
            if ( World_AIW2.Instance == null )
                return 0;
            if ( Mapgen.IsMapCurrentlyGenerating || World_AIW2.Instance.Setup.ChangedSinceLastMapGenCall )
                return -1;

            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                fac = World_AIW2.Instance.Factions[i];
                if ( fac != null )
                {
                    ExternalFactionBaseInfo baseInfo = fac.BaseInfo;
                    if ( baseInfo != null )
                    {
                        float newLoad = baseInfo.CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( SummarizeExplanation ? null : OptionalExplainCalculation );
                        if ( newLoad > 0 )
                        {
                            if ( !SummarizeExplanation )
                            {
                                if ( OptionalExplainCalculation != null )
                                    OptionalExplainCalculation.Add( "\n" );
                            }
                            calculatedLoad += newLoad;
                            loadFromFactions += newLoad;
                            knownFactionCount++;
                        }
                    }
                }
                else
                    unknownFactionCount++;
            }

            if ( unknownFactionCount > 0 )
            {
                calculatedLoad += (unknownFactionCount * 100);
                if ( OptionalExplainCalculation != null )
                {
                    OptionalExplainCalculation.Add( "\n" ).AddNumberMoreReadable( (unknownFactionCount * 100) ).Add( " 负载来自未知阵营" );
                }
            }

            if ( OptionalExplainCalculation != null )
            {
                if ( unknownFactionCount > 0 )
                    OptionalExplainCalculation.Add( "\n" ).AddNumberMoreReadable( loadFromFactions ).Add( " 直接负载来自 " ).Add( knownFactionCount ).Add( " 个已知阵营" );
                else
                    OptionalExplainCalculation.Add( "\n" ).AddNumberMoreReadable( loadFromFactions ).Add( " 直接负载来自 " ).Add( knownFactionCount ).Add( " 个阵营" );
            }

            int totalFactionCount = knownFactionCount + unknownFactionCount;

            #region Faction Multiplicative Load
            if ( totalFactionCount <= 5 )
            {
                //Nothing to do, we'll just consider the direct effects here
                calculatedLoad *= 0.7f;
            }
            else if ( totalFactionCount <= 9 )
            {
                float amountToMultiplyBy = 1 + ( 0.05f * (totalFactionCount - 5) );
                calculatedLoad *= amountToMultiplyBy;
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n" ).Add( UnityEngine.Mathf.RoundToInt( amountToMultiplyBy * 100 ) ).Add( "% 正常负载来自 " ).Add( totalFactionCount ).Add( " 个阵营之间的交互" );
            }
            else if ( totalFactionCount <= 15 )
            {
                float amountToMultiplyBy = 1 + (0.02f * (9 - 5)) + (0.04f * (totalFactionCount - 9));
                calculatedLoad *= amountToMultiplyBy;
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n" ).Add( UnityEngine.Mathf.RoundToInt( amountToMultiplyBy * 100 ) ).Add( "% 正常负载来自 " ).Add( totalFactionCount ).Add( " 个阵营之间的交互" );
            }
            else if ( totalFactionCount <= 20 )
            {
                float amountToMultiplyBy = 1 + (0.02f * (9 - 5)) + (0.04f * (15 - 9)) + (0.08f * (totalFactionCount - 15));
                calculatedLoad *= amountToMultiplyBy;
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n" ).Add( UnityEngine.Mathf.RoundToInt( amountToMultiplyBy * 100 ) ).Add( "% 正常负载来自 " ).Add( totalFactionCount ).Add( " 个阵营之间的交互" );
            }
            else if ( totalFactionCount <= 25 )
            {
                float amountToMultiplyBy = 1 + (0.02f * (9 - 5)) + (0.04f * (15 - 9)) + (0.08f * (20 - 15)) + (0.12f * (totalFactionCount - 20));
                calculatedLoad *= amountToMultiplyBy;
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n" ).Add( UnityEngine.Mathf.RoundToInt( amountToMultiplyBy * 100 ) ).Add( "% 正常负载来自 " ).Add( totalFactionCount ).Add( " 个阵营之间的交互" );
            }
            else
            {
                float amountToMultiplyBy = 1 + (0.02f * (9 - 5)) + (0.04f * (15 - 9)) + (0.08f * (20 - 15)) + (0.12f * (25 - 20)) + (0.16f * (totalFactionCount - 25));
                calculatedLoad *= amountToMultiplyBy;
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n" ).Add( UnityEngine.Mathf.RoundToInt( amountToMultiplyBy * 100 ) ).Add( "% 正常负载来自 " ).Add( totalFactionCount ).Add( " 个阵营之间的交互" );
            }
            #endregion

            Galaxy gal = World_AIW2.Instance.CurrentGalaxy;

            #region Planets Load Count
            int planetCount = gal == null ? 80 : gal.GetCountOfTotalPlanetsDestroyedAndOtherwise();

            if ( planetCount <= 60 )
            {
                calculatedLoad *= 0.7f;
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n70% 正常负载来自 " ).Add( planetCount ).Add( " 个星球" );
            }
            else if ( planetCount <= 70 )
            {
                calculatedLoad *= 0.8f;
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n80% 正常负载来自 " ).Add( planetCount ).Add( " 个星球" );
            }
            else if ( planetCount < 80 )
            {
                calculatedLoad *= 0.9f;
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n90% 正常负载来自 " ).Add( planetCount ).Add( " 个星球" );
            }
            else if ( planetCount < 90 )
            {
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n正常负载来自 " ).Add( planetCount ).Add( " 个星球" );
            }
            else if ( planetCount < 100 )
            {
                calculatedLoad *= 1.2f;
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n120% 正常负载来自 " ).Add( planetCount ).Add( " 个星球" );
            }
            else if ( planetCount < 110 )
            {
                calculatedLoad *= 1.4f;
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n140% 正常负载来自 " ).Add( planetCount ).Add( " 个星球" );
            }
            else if ( planetCount < 120 )
            {
                calculatedLoad *= 1.6f;
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n160% 正常负载来自 " ).Add( planetCount ).Add( " 个星球" );
            }
            else if ( planetCount < 130 )
            {
                calculatedLoad *= 1.9f;
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n190% 正常负载来自 " ).Add( planetCount ).Add( " 个星球" );
            }
            else if ( planetCount < 140 )
            {
                calculatedLoad *= 2.5f;
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n280% 正常负载来自 " ).Add( planetCount ).Add( " 个星球" );
            }
            else if ( planetCount < 160 )
            {
                calculatedLoad *= 3.2f;
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n320% 正常负载来自 " ).Add( planetCount ).Add( " 个星球" );
            }
            else if ( planetCount < 170 )
            {
                calculatedLoad *= 3.4f;
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n340% 正常负载来自 " ).Add( planetCount ).Add( " 个星球" );
            }
            else if ( planetCount < 180 )
            {
                calculatedLoad *= 3.8f;
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n380% 正常负载来自 " ).Add( planetCount ).Add( " 个星球" );
            }
            else if ( planetCount < 190 )
            {
                calculatedLoad *= 4.0f;
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n400% 正常负载来自 " ).Add( planetCount ).Add( " 个星球" );
            }
            else if ( planetCount < 200 )
            {
                calculatedLoad *= 4.3f;
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n430% 正常负载来自 " ).Add( planetCount ).Add( " 个星球" );
            }
            else if ( planetCount < 210 )
            {
                calculatedLoad *= 4.6f;
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n460% 正常负载来自 " ).Add( planetCount ).Add( " 个星球" );
            }
            else
            {
                calculatedLoad *= 5f;
                if ( OptionalExplainCalculation != null )
                    OptionalExplainCalculation.Add( "\n500%+ 正常负载来自 " ).Add( planetCount ).Add( " 个星球" );
            }
            #endregion

            return UnityEngine.Mathf.RoundToInt( calculatedLoad );
        }

        public static string CalculateLoadColorAndCategory( int totalLoad, out string CategoryName, out string Description )
        {
            if ( totalLoad > 5000 )
            {
                CategoryName = "灾难级";
                Description = "在5000+负载下，所有人的机器都会很卡。你不太可能在这种负载水平下获得良好的体验，但也许你有一台我们没想到的超级计算机。";
                return "ff34f8";
            }
            else if ( totalLoad > 4000 )
            {
                CategoryName = "超级极端";
                Description = "在3000-4000负载下，2022年一款非常好的现代CPU大概能以卡顿的30fps运行，但老计算机会吃不消。存档会很大很慢，有些东西可能会太慢而无法完全正常工作。";
                return "ff34cd";
            }
            else if ( totalLoad > 2700 )
            {
                CategoryName = "极端";
                Description = "在2700-4000负载下，2022年一款非常好的现代CPU大概能运行，甚至可能达到60-80fps，但老计算机会吃不消。存档会很大且稍慢，但所有计算应该都能正确完成。";
                return "ff34cd";
            }
            else if ( totalLoad > 1600 )
            {
                CategoryName = "非常高";
                Description = "在1600-2700负载下，这是一个相当大的游戏，可能会出现一些性能问题。较旧或较弱的CPU不太可能很好地处理这个。";
                return "ff348f";
            }
            else if ( totalLoad > 800 )
            {
                CategoryName = "高";
                Description = "在800-1600负载下，这是一个非常繁忙的游戏，将需要你所有的核心。希望你不会有性能问题，但这取决于你的CPU年龄和质量。";
                return "ff5f34";
            }
            else if ( totalLoad > 500 )
            {
                CategoryName = "中等";
                Description = "在500-800负载下，这对2015年及以后的大多数CPU来说应该没问题，但较旧的CPU可能会吃力。四个或更多核心仍然是理想的。2018年及以后的中高端电脑可能会达到90-120fps和完美的模拟速度。";
                return "ffab34";
            }
            else if ( totalLoad > 300 )
            {
                CategoryName = "低";
                Description = "在300-500负载下，2011年及以后的任何CPU大概都能运行这个。越往后走，是否有四个核心就越重要。较新的双核机器应该没问题。2017年及以后的中高端电脑可能会达到90-120fps和完美的模拟速度。";
                return "34baff";
            }
            else
            {
                CategoryName = "非常低";
                Description = "在1-300负载下，2011年及以后的任何CPU大概都能运行这个。两个核心可能就够了，但更多总是好的。2016年及以后的中高端电脑可能会达到90-120fps和完美的模拟速度。";
                return "7fb2ff";
            }
        }

        public static bool GetShouldButtonClickFailBasedOnGameStatusCycling()
        {
            if ( ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() )
                return true;
            return false;
        }

        public static void ToggleFreeLook()
        {
            if ( !World.Instance.IsLoaded )
                return;
            Engine_AIW2.Instance.PresentationLayer.ToggleFreelook();
        }

        //public static void IncreaseOrDecreaseFrameFrequency( GameCommandSource Source, bool Increase )
        //{
        //    if ( !World.Instance.IsLoaded )
        //        return;
        //    GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.ChangeFrameFrequency], Source );
        //    command.RelatedMagnitude = 1;
        //    if ( !Increase )
        //        command.RelatedMagnitude = -command.RelatedMagnitude;
        //    World_AIW2.Instance.QueueGameCommand( command, true );
        //}

        public static void IncreaseOrDecreaseFrameSize( Faction ForFaction, GameCommandSource Source, bool Increase, bool AllowLocalAudioVisualReaction = true )
        {
            if ( !World.Instance.IsLoaded )
                return;
            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.ChangeFrameSize], Source );
            command.RelatedMagnitude = 1;
            if ( !Increase )
                command.RelatedMagnitude = -command.RelatedMagnitude;
            //AllowLocalAudioVisualReaction=false lets the Phase-2 auto-budget adjust frame size silently (no manual "tweak" click).
            World_AIW2.Instance.QueueGameCommand( ForFaction, command, AllowLocalAudioVisualReaction ); ;
        }

        public static void IncreaseOrDecreaseFrameSize( Faction ForFaction, GameCommandSource Source, int SignedChange )
        {
            if ( !World.Instance.IsLoaded )
                return;
            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.ChangeFrameSize], Source );
            command.RelatedMagnitude = SignedChange;
            World_AIW2.Instance.QueueGameCommand( ForFaction, command, true ); ;
        }

        public static void LowerOrRaiseGameSpeedStyle( Faction ForFaction, GameCommandSource Source, bool Increase )
        {
            if ( !World.Instance.IsLoaded )
                return;
            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.ChangeGameSpeed], Source );

            int newOffset = World_AIW2.Instance.GameSpeedDifferentialFromDefault + ( Increase ? 1 : -1 );
            int currentGameSpeedTypeIndex = GameSpeedTypeTable.Instance.DefaultSpeedIndex + newOffset;
            if ( currentGameSpeedTypeIndex < 0 )
                currentGameSpeedTypeIndex = 0;
            if ( currentGameSpeedTypeIndex >= GameSpeedTypeTable.Instance.SortedGameSpeeds.Count )
                currentGameSpeedTypeIndex = GameSpeedTypeTable.Instance.SortedGameSpeeds.Count - 1;

            newOffset = currentGameSpeedTypeIndex - GameSpeedTypeTable.Instance.DefaultSpeedIndex;

            //GameSpeedType speedType = GameSpeedTypeTable.Instance.SortedGameSpeeds[currentGameSpeedTypeIndex];

            command.RelatedMagnitude = newOffset;
            World_AIW2.Instance.QueueGameCommand( ForFaction, command, true ); ;
        }
        public static void OpenChat()
        {
            if ( World_AIW2.Instance.IsOutsideOfNormalGameplay )
                return;
            if ( Window_InGameEscapeMenu.Instance.GetIsOnStack())
                return;

            if ( !Window_ChatboxWindow.Instance.IsOpen )
            {
                Window_ChatboxWindow.Instance.Open();

                // this prevents the key press bound to this action from getting typed into the textbox
                // ... not because it doesn't still receive it, but we reject it in its validator if this flag is set
                ArcenInput.BlockForAJustPartOfOneSecond(32); 
            }
            else
                Window_ChatboxWindow.Instance.Close( true );
        }

        public static void MarkAllJournalAsReadForLocal()
        {
            PlayerAccount localAccount = PlayerAccount.Local;
            if ( localAccount == null )
                return;
            
            for ( int i = 0; i < World_AIW2.Instance.JournalHistory.Count; i++ )
            {
                var entry = World_AIW2.Instance.JournalHistory[i];
                if ( entry == null )
                    continue;
                if ( entry.IsTipRatherThanJournalEntry )
                    continue;
                if ( !entry.CalculateIsNewForLocalPlayer() )
                    continue;
                
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.JournalEntryMarkedAsRead], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = entry.UniqueID;
                command.RelatedIntegers.Add( localAccount.PlayerPrimaryKeyID );
                World_AIW2.Instance.QueueGameCommand( null, command, false );
            }
        }
        
        public static void MarkJournalEntryAsReadForLocal( Faction ForFaction, JournalEntryInCampaign Entry )
        {
            if ( Entry == null )
                return;
            PlayerAccount localAccount = PlayerAccount.Local;
            if ( localAccount == null )
                return;
            if ( Entry.HasBeenViewedByPlayerAccountIDs.ContainsKey( localAccount.PlayerPrimaryKeyID ) )
                return; //already was marked as read, this happens, so just ignore this request

            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.JournalEntryMarkedAsRead], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
            command.RelatedString = Entry.UniqueID;
            command.RelatedIntegers.Add( localAccount.PlayerPrimaryKeyID );
            World_AIW2.Instance.QueueGameCommand( ForFaction, command, true );
        }

        public static void SetFactionKitingIfNeeded( Faction faction, bool AutoKite )
        {
            if ( faction.UnitsAutoKite_NeverSetDirectlyOrItBreaksEverything != AutoKite )
            {
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.ModderCommand], GameCommandSource.AnythingElse );
                command.RelatedModdableCommandCode = "CMP_AutoKite"; //should be unique enough that no other modder would use this command code, but ideally not a lengthy string
                command.RelatedFactionIndex = faction.FactionIndex;
                command.RelatedIntegers.Add( AutoKite ? 1 : 0 );
                command.RelatedIntegers.Add( faction.FactionIndex );
                World_AIW2.Instance.QueueGameCommand( faction, command, false );
            }
        }

        #region SwitchLocalPlayerAccountToControllingNextHumanFaction
        public static void SwitchLocalPlayerAccountToControllingNextHumanFaction( Faction ForFaction )
        {
            PlayerAccount player = PlayerAccount.Local;
            if ( player == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "Could not find the local player account.", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }

            Faction currentFactionBeingControlled = null;
            Faction nextFactionAfterThatOneThatIsControlled = null;
            foreach ( Faction fac in World_AIW2.Instance.Factions )
            {
                if ( currentFactionBeingControlled == null ) //find the faction we control, or this will just stay null if we were in specator mode, which is fine
                {
                    if ( fac.Config.GetDoesPlayerControlThisFaction( player.PlayerPrimaryKeyID ) )
                        currentFactionBeingControlled = fac;
                }
                else //we already found the faction, so find the next one if there is one after it
                {
                    if ( fac.Type == FactionType.Player )
                    {
                        nextFactionAfterThatOneThatIsControlled = fac;
                        break;
                    }
                }
            }
            //there was no faction after the current one.  So look for one before it, we looped around
            if ( nextFactionAfterThatOneThatIsControlled == null )
            {
                foreach ( Faction fac in World_AIW2.Instance.Factions )
                {
                    if ( currentFactionBeingControlled != fac ) //can't be the one we already control
                    {
                        if ( fac.Type == FactionType.Player )
                        {
                            nextFactionAfterThatOneThatIsControlled = fac;
                            break;
                        }
                    }
                }
            }

            if ( nextFactionAfterThatOneThatIsControlled == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "Could not any faction beyond the current one to switch to.  There probably is only one human faction in this campaign.", 
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }

            if ( currentFactionBeingControlled != null )
            {
                //remove us from the first faction, if we were controlling a faction (it's possible this was null because of specator mode)
                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.AfterGameStartOnly_RequestFactionChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "FactionOwnershipChanged";
                command.RelatedIntegers.Add( currentFactionBeingControlled.FactionIndex );
                command.RelatedIntegers.Add( player.PlayerPrimaryKeyID );
                //set false to remove
                command.RelatedBool = false;
                World_AIW2.Instance.QueueGameCommand( ForFaction, command, true );
            }

            {
                //add us to the new faction
                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.AfterGameStartOnly_RequestFactionChanges], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedString = "FactionOwnershipChanged";
                command.RelatedIntegers.Add( nextFactionAfterThatOneThatIsControlled.FactionIndex );
                command.RelatedIntegers.Add( player.PlayerPrimaryKeyID );
                //set true to take control
                command.RelatedBool = true;
                World_AIW2.Instance.QueueGameCommand( ForFaction, command, true );
            }

            World_AIW2.Instance.QueueChatMessageOrCommand( player.Username + " switched from " +
                (currentFactionBeingControlled == null ? "spectator mode" : currentFactionBeingControlled.GetDisplayName()) + " to " +
                nextFactionAfterThatOneThatIsControlled.GetDisplayName(),
                ChatType.ShowLocallyOnly, string.Empty, null );

        }
        #endregion

        public static void TransferEntityToFaction( GameEntity_Squad entity, Faction faction,string ReasonCode )
        {
            if ( entity == null )
                return;
            if ( faction == null )
                return;
            byte oldMark = entity.CurrentMarkLevel;
            PlanetFaction destinationFaction = entity.Planet.GetPlanetFactionForFaction( faction );
            entity.PlanetFaction.SwitchToFaction( entity, destinationFaction, false, ReasonCode );
            //when transferrring to a new faction, set self to be in Attacker mode
            entity.Orders.ClearOrders( ClearBehavior.AnythingIncludingAttackerMode, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.YesClearAnyOrders_IncludingFromHumans, "TransferEntitiesToFaction" );
            EntityOrderCollection orders = entity.Orders;
            orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //works fine, in game command (so main sim)
            entity.GuardOrPatrolOffsetPoints.Clear();
            entity.GuardedUnit.Clear();
            entity.WaitingAgainstPlanetIndex = -1;
            entity.StartedWaitingAtGameSecond = -1;
            if ( oldMark > 1 )
                entity.CustomBaseMark = oldMark;
        }

        public static bool GetHasAnyErrorsPreventingStart( bool WriteShort, ArcenDoubleCharacterBuffer Buffer )
        {
            bool hasBlockingError = false;
            List<ConfigurationForFaction> factionConfigs = World_AIW2.Instance.Setup.FactionConfigurations;
            for ( int i = 0; i < factionConfigs.Count; i++ )
            {
                ConfigurationForFaction factionConfig = factionConfigs[i];
                if ( factionConfig.SpecialFactionData.Type == FactionType.Player )
                {
                    PlayerTypeData playerType = factionConfig.PlayerTypeDataOrNull_ModeratelyExpensive;
                    Planet planet = World_AIW2.Instance.GetPlanetByIndex( factionConfig.StartingIndex );
                    if ( planet == null )
                    {
                        hasBlockingError = true;
                        if ( Buffer != null )
                        {
                            if ( !Buffer.GetIsEmpty() )
                                Buffer.Add( "\n" );
                            Buffer.Add( "Missing Start Planet For " );
                            factionConfig.WriteFactionNameToBuffer( Buffer );
                        }
                        continue;
                    }

                    string invalidReason = planet.GetPlanetInvalidReason_StartingWorldForFaction( factionConfig );

                    if ( invalidReason != null && invalidReason.Length > 0 )
                    {
                        hasBlockingError = true;
                        if ( Buffer != null )
                        {
                            if ( !Buffer.GetIsEmpty() )
                                Buffer.Add( "\n" );
                            Buffer.Add( "Invalid Start Planet For " );
                            factionConfig.WriteFactionNameToBuffer( Buffer );
                            //if ( !WriteShort )
                                Buffer.Add( " Because: " ).Add( invalidReason );
                        }
                    }

                    if ( playerType.DisallowedFactionsWhenPresent.Count > 0 )
                    {
                        for ( int j = 0; j < factionConfigs.Count; j++ )
                        {
                            ConfigurationForFaction otherFac = factionConfigs[j];
                            if ( otherFac == factionConfig )
                                continue;
                            string otherFacName = otherFac.SpecialFactionData.InternalName;
                            for ( int k = 0; k < playerType.DisallowedFactionsWhenPresent.Count; k++ )
                            {
                                if ( otherFacName == playerType.DisallowedFactionsWhenPresent[k] )
                                {
                                    hasBlockingError = true;
                                    if ( Buffer != null )
                                    {
                                        if ( !Buffer.GetIsEmpty() )
                                            Buffer.Add( "\n" );
                                        Buffer.Add( playerType.DisplayName ).Add( " Cannot Be Used With " ).Add( otherFac.SpecialFactionData.DisplayName );
                                        if ( !WriteShort )
                                            Buffer.Add( "\n<size=80%>This player type and this faction cannot be used at the same time -- only one of them is valid at a time.  Apologies for any inconvenience!</size>" );
                                        break;
                                    }
                                }
                            }
                            if ( hasBlockingError )
                                break;
                        }
                    }

                    if ( playerType.MustBeOnlyOneOfMyself )
                    {
                        for ( int j = 0; j < factionConfigs.Count; j++ )
                        {
                            ConfigurationForFaction otherFac = factionConfigs[j];
                            if ( otherFac == factionConfig || otherFac.SpecialFactionData.Type != FactionType.Player )
                                continue;
                            PlayerTypeData otherPlayerType = otherFac.PlayerTypeDataOrNull_ModeratelyExpensive;
                            if ( otherPlayerType != null && otherPlayerType.InternalName == playerType.InternalName )
                            {
                                hasBlockingError = true;
                                if ( Buffer != null )
                                {
                                    if ( !Buffer.GetIsEmpty() )
                                        Buffer.Add( "\n" );
                                    Buffer.Add( playerType.DisplayName ).Add( " Cannot Be Used By Multiple Factions" );
                                    if ( !WriteShort )
                                        Buffer.Add( "\n<size=80%>Only one of this player type can be used in a game at once.  Two players can share a single faction in multiplayer, or other players can use other player types, but two independent factions of this player type cannot be used.</size>" );
                                    break;
                                }
                            }
                        }
                    }

                    if ( playerType.MustBeSolePlayerFaction )
                    {
                        for ( int j = 0; j < factionConfigs.Count; j++ )
                        {
                            ConfigurationForFaction otherFac = factionConfigs[j];
                            if ( otherFac == factionConfig || otherFac.SpecialFactionData.Type != FactionType.Player )
                                continue;
                            hasBlockingError = true;
                            if ( Buffer != null )
                            {
                                if ( !Buffer.GetIsEmpty() )
                                    Buffer.Add( "\n" );
                                Buffer.Add( playerType.DisplayName ).Add( " Cannot Be Used With Other Players Present" );
                                if ( !WriteShort )
                                    Buffer.Add( "\n<size=80%>If you want to have a spectator in multiplayer, then just have the player(s) in question just remove themself from being attached to any player factions, and they will become spectators that share the view of the human players.  The player-type form of spectator is omniscient, and meant for watching factions duke it out without any player involvement.</size>" );
                                break;
                            }
                        }
                    }

                    if ( playerType.UsesResource2 )
                    {
                        for ( int j = 0; j < factionConfigs.Count; j++ )
                        {
                            ConfigurationForFaction otherFac = factionConfigs[j];
                            if ( otherFac == factionConfig || otherFac.SpecialFactionData.Type != FactionType.Player )
                                continue;

                            PlayerTypeData otherPlayer = otherFac.PlayerTypeDataOrNull_ModeratelyExpensive;
                            if ( otherPlayer.UsesResource2 && playerType.Resource2DisplayName != otherPlayer.Resource2DisplayName )
                            {
                                hasBlockingError = true;
                                if ( Buffer != null )
                                {
                                    if ( !Buffer.GetIsEmpty() )
                                        Buffer.Add( "\n" );
                                    Buffer.Add( playerType.DisplayName ).Add( " uses the resource " ).Add( playerType.Resource2DisplayName ).Add( ", which " );
                                    Buffer.Add( otherPlayer.DisplayName ).Add( " uses as " ).Add( otherPlayer.Resource2DisplayName );
                                    if ( !WriteShort )
                                        Buffer.Add( "\n<size=80%>These two player types cannot be used in the same campaign because of this conflict.</size>" );
                                    break;
                                }
                            }
                        }
                    }

                    if ( !playerType.CanLoseGame && !playerType.PreventsGameWinOrLoss )
                    {
                        bool foundOtherPlayerThatCanLose = false;
                        for ( int j = 0; j < factionConfigs.Count; j++ )
                        {
                            ConfigurationForFaction otherFac = factionConfigs[j];
                            if ( otherFac == factionConfig || otherFac.SpecialFactionData.Type != FactionType.Player )
                                continue;
                            PlayerTypeData otherPlayerType = otherFac.PlayerTypeDataOrNull_ModeratelyExpensive;
                            if ( otherPlayerType.CanLoseGame )
                            {
                                foundOtherPlayerThatCanLose = true;
                                break;
                            }
                        }
                        if ( !foundOtherPlayerThatCanLose )
                        {
                            hasBlockingError = true;
                            if ( Buffer != null )
                            {
                                if ( !Buffer.GetIsEmpty() )
                                    Buffer.Add( "\n" );
                                Buffer.Add( playerType.DisplayName ).Add( " Cannot Be Used Without Other Players" );
                                if ( WriteShort )
                                    Buffer.Add( " <size=60%>(Click Start Game or hover faction in faction tab for details)</size>" );
                                else
                                    Buffer.Add( "\n<size=80%>This player type is unable to lose the game, so it needs to accompany another player type that can.</size>" );
                            }
                        }
                    }

                    if ( playerType.RequiredVassalCount > 0 )
                    {
                        int foundVassalCount = 0;
                        for ( int j = 0; j < factionConfigs.Count; j++ )
                        {
                            ConfigurationForFaction otherFac = factionConfigs[j];
                            if ( otherFac == factionConfig || otherFac.SpecialFactionData.Type == FactionType.Player )
                                continue;
                            if ( otherFac.GetBoolValueForCustomFieldOrDefaultValue( "Vassal", false ) )
                                foundVassalCount++;
                        }
                        if ( foundVassalCount < playerType.RequiredVassalCount )
                        {
                            hasBlockingError = true;
                            if ( Buffer != null )
                            {
                                if ( !Buffer.GetIsEmpty() )
                                    Buffer.Add( "\n" );
                                Buffer.Add( playerType.DisplayName ).Add( " Requires " ).Add( playerType.RequiredVassalCount - foundVassalCount ).Add( " More Vassals" );
                                if ( WriteShort )
                                    Buffer.Add( " <size=60%>(Click Start Game or hover faction in faction tab for details)</size>" );
                                else
                                {
                                    Buffer.Add( "\n<size=80%>This faction requires " ).Add( playerType.RequiredVassalCount )
                                        .Add( " vassals.  Vassals are friendly factions that are subordinate to you: you can't control them directly, but you can give them targets and goals, and sometimes broad orders.\n\nCurrently Available Vassal Choices:" );

                                    foreach ( SpecialFactionData facType in SpecialFactionDataTable.Instance.Rows )
                                    {
                                        if ( facType.CanBeAVassal )
                                            Buffer.Add( "\n" ).Add( facType.DisplayName );
                                    }

                                    Buffer.Add( "</size>" );
                                }
                            }
                        }
                    }
                } //endif for if the player type is player

                //for any faction type from here on

                if ( factionConfig.SpecialFactionData.RequiresAtLeastOneMetalUsingPlayer )
                {
                    bool foundAMetalUser = false;
                    bool foundAnyNonSpecators = false;
                    for ( int j = 0; j < factionConfigs.Count; j++ )
                    {
                        ConfigurationForFaction otherFac = factionConfigs[j];
                        if ( otherFac == factionConfig || otherFac.SpecialFactionData.Type != FactionType.Player )
                            continue;
                        PlayerTypeData otherPlayerType = otherFac.PlayerTypeDataOrNull_ModeratelyExpensive;
                        if ( otherPlayerType != null )
                        {
                            if ( !otherPlayerType.IsSpectator )
                            {
                                foundAnyNonSpecators = true;
                                if ( otherPlayerType.UsesMetal )
                                    foundAMetalUser = true;
                            }
                        }
                    }
                    if ( !foundAMetalUser && foundAnyNonSpecators )
                    {
                        hasBlockingError = true;
                        if ( Buffer != null )
                        {
                            if ( !Buffer.GetIsEmpty() )
                                Buffer.Add( "\n" );
                            Buffer.Add( factionConfig.SpecialFactionData.DisplayName ).Add( " Requires A Human Player Who Uses Metal" );
                            if ( !WriteShort )
                                Buffer.Add( "\n<size=80%>Unless everyone is a spectator, this faction needs to be used with at least one metal-using player type, or else you won't be able to complete it properly.</size>" );
                            break;
                        }
                    }
                }
            }
            return hasBlockingError;
        }

        public static void SetDefaultsForLobby()
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            if ( Mapgen.IsMapCurrentlyGenerating )
            {
                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "先前地图生成仍在进行中", 
                    "上一次地图生成仍在运行中。请稍等片刻，然后再次点击。（可能当你点击确定时已经完成了。）", "确定" );
                return;
            }
            Engine_Universal.ClearAllTraceOfExistingGame();

            //keep the player accounts in multiplayer!
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.SinglePlayerOnly )
            {
                World.Instance.AllPlayerAccounts.Clear();
                PlayerAccount playerAccount = PlayerAccount.CreateBrandNew_AndAddToListOfAccounts( PlayerProfile.Local.DisplayName,
                    PlayerProfile.Local.GameSpecificSubObject.GetFactionCenterColorLookupName(),
                        PlayerProfile.Local.GameSpecificSubObject.GetFactionTrimColorLookupName(), true );
                PlayerAccount.SetLocalPlayerAccount( playerAccount );
            }
            else
            {
                for ( int i = 0; i < World.Instance.AllPlayerAccounts.Count; i++ )
                {
                    PlayerAccount acct = World.Instance.AllPlayerAccounts[i];
                    acct.HasAutoCreatedFactionForThisAccountBefore = false;
                }
            }

            World_AIW2.Instance.ResetForNewMapGeneration( null );
            World_AIW2.Instance.ResetForDefaultMap_ClearFactionsEtc();
            World_AIW2.Instance.Setup.Clear();
            World_AIW2.Instance.Setup.ScenarioDoNotCallDirectly = ScenarioDataTable.Instance.DefaultRow;
            World_AIW2.Instance.Setup.SetUpDefaultFactionConfigurations( false );
            World_AIW2.Instance.Setup.ShouldSeedDetailsYet = false;
            World_AIW2.Instance.Setup.MapConfig.Seed = Engine_Universal.PermanentQualityRandom.Next( 0, 2000000 );
            World_AIW2.Instance.Setup.MapConfig.MapType = MapTypeDataTable.Instance.DefaultRow;
            World_AIW2.Instance.Setup.MapConfig.PlanetNameType = PlanetNameTypeDataTable.Instance.DefaultRow;
            World_AIW2.Instance.Setup.MapConfig.NumberOfPlanetsRaw = 80;

            Engine_AIW2.Instance.CurrentlyFocusedFactionInLobby = null;

            World_AIW2.Instance.AssignPlayersToFactionsAfterClearingAllPlayerFactionLinks( false, true, true );

            Engine_AIW2.Instance.GenerateNewWorldThatIsBlankForLobbyOrIsPopulatedByTutorialOrTestChamber( ScenarioDataTable.Instance.DefaultRow, null, StartWorldSource1.StartingTheLobbyDefaults, StartWorldSource2.NotLoadingAnything );
        }

        #region SecondaryClickSquad
        public static SecondaryClickResult SecondaryClickSquads( List<SafeSquadWrapper> EntitiesUnderCursor )
        {
            if ( EntitiesUnderCursor == null )
                return SecondaryClickResult.NothingWasThere;

            ArcenInputFlags mouseEventFlags = ArcenInputFlags.None;

            bool ordersToStationaryFlagships = InputCaching.inputHoldToGiveOrdersToStationaryFlagships.CalculateIsKeyDownNow_IgnoreConflicts();

            #region setting mouseEventFlags based on what's being held down
            if ( InputCaching.inputAddToSelection.CalculateIsKeyDownNow_IgnoreConflicts() )
                mouseEventFlags = mouseEventFlags.Add( ArcenInputFlags.Additive );
            if ( InputCaching.inputRemoveFromSelection.CalculateIsKeyDownNow_IgnoreConflicts() )
                mouseEventFlags = mouseEventFlags.Add( ArcenInputFlags.Subtractive );
            if ( ordersToStationaryFlagships )
                mouseEventFlags = mouseEventFlags.Add( ArcenInputFlags.OrdersReachStationaryFlagshipsOrSkipRegularOnes );
            #endregion

            bool unused = false;
            SecondaryClickResult result = SecondaryClickResult.NothingWasThere;

            for ( int i = 0; i < EntitiesUnderCursor.Count; i++ )
            {
                if ( SecondaryClickSquad( EntitiesUnderCursor[i].GetSquad(), ref unused, mouseEventFlags ) ==
                     SecondaryClickResult.DidAnyNeededActions )
                {
                    mouseEventFlags = mouseEventFlags.Add( ArcenInputFlags.Additive );
                    result = SecondaryClickResult.DidAnyNeededActions;
                }
            }
            
            return result;
        }

        public static SecondaryClickResult SecondaryClickSquad( GameEntity_Squad EntityUnderCursor )
        {
            if ( EntityUnderCursor == null )
                return SecondaryClickResult.NothingWasThere;
            ArcenInputFlags mouseEventFlags = ArcenInputFlags.None;
            bool ordersToStationaryFlagships = InputCaching.inputHoldToGiveOrdersToStationaryFlagships.CalculateIsKeyDownNow_IgnoreConflicts();

            #region setting mouseEventFlags based on what's being held down
            if ( InputCaching.inputAddToSelection.CalculateIsKeyDownNow_IgnoreConflicts() )
                mouseEventFlags = mouseEventFlags.Add( ArcenInputFlags.Additive );
            if ( InputCaching.inputRemoveFromSelection.CalculateIsKeyDownNow_IgnoreConflicts() )
                mouseEventFlags = mouseEventFlags.Add( ArcenInputFlags.Subtractive );
            if ( ordersToStationaryFlagships )
                mouseEventFlags = mouseEventFlags.Add( ArcenInputFlags.OrdersReachStationaryFlagshipsOrSkipRegularOnes );
            #endregion

            bool unused = false;
            return SecondaryClickSquad( EntityUnderCursor, ref unused, mouseEventFlags );
        }

        public static SecondaryClickResult SecondaryClickSquad( GameEntity_Squad EntityUnderCursor, ref bool hasDoneSomethingYet, ArcenInputFlags InputFlags )
        {
            if ( EntityUnderCursor == null )
                return SecondaryClickResult.NothingWasThere;

            Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
            Int16 entityUnderCursorPlanetIndex = EntityUnderCursor.GetPlanetIndexSafe();

            // clicked on enemy
            #region clicked enemy
            if ( localFaction.GetIsHostileTowards( EntityUnderCursor.PlanetFaction.Faction ) )
            {
                GameEntity_Squad enemySquad = EntityUnderCursor;

                if ( enemySquad.GetCurrentCloakingPoints() > 0 )
                {
                    return SecondaryClickResult.ClickedOnWhatMayAsWellBeEmptySpace;
                }
                if ( !enemySquad.CanIsATargetThatCanBeShotAtAll( localFaction.Type, null, false ) )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( "<color=#777777>" + DateTime.Now.ToShortTimeString() + ":" + DateTime.Now.Second + "</color> None of your selected ships can currently damage " + enemySquad.TypeData.DisplayName +
                        ".  Check its tooltip for invulnerabilities.", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                    return SecondaryClickResult.DidAnyNeededActions;
                }

                List<int> workingEntityIDs = Mat.GetTemporaryIntList( "EndpointFunctions-SecondaryClickSquad-workingEntityIDs1", 10f );
                if ( workingEntityIDs == null ) //blocked for teardown/shutdown; bail
                    return SecondaryClickResult.NothingWasThere;

                bool hadFailuresToHit = false;
                bool didAnyAttackBits = false;
                bool haveAnySelectedShips = false;

                foreach ( GameEntity_Squad selected in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
                {
                    if ( selected.GetPlanetIndexSafe() == entityUnderCursorPlanetIndex )
                        continue; //skip anything that is on my current planet
                    if ( selected.DataForMark.Speed <= 0 )
                        continue;
                    if ( selected.TypeData.FleetMembershipStyle == FleetMembershipStyle.Planetary )
                        continue;
                    haveAnySelectedShips = true;
                    if ( selected.TypeData.HasCustomMoveOrderAndNoOtherDirectCommandsCanBeGivenFromPlayer )
                        continue; //these types ignore all move orders

                    if ( !selected.CanTargetWithWeapon( enemySquad ) && !selected.TypeData.IsFleetLeader )
                    {
                        hadFailuresToHit = true;
                        continue;
                    }
                    if ( selected.GetIsNonFlagshipInLoadingModeMode() )
                        continue;

                    workingEntityIDs.Add( selected.PrimaryKeyID );
                }

                if ( workingEntityIDs.Count > 0 )
                {
                    hasDoneSomethingYet = true;
                    didAnyAttackBits = true;

                    PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();

                    BringRemoteShipsToPlanet( World_AIW2.Instance.GetPlanetByIndex( entityUnderCursorPlanetIndex ),
                        enemySquad, InputFlags.Has( ArcenInputFlags.Additive ), InputFlags, pathingCacheData );

                    pathingCacheData.ReturnToPool();

                    /* Now handle the attack command for the remote ships */
                    var remoteCommand = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.Attack], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    remoteCommand.PlanetOrderWasIssuedFrom = Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed();
                    remoteCommand.ToBeQueued = true; //this is always queued, since we have some wormhole move commands already
                    remoteCommand.RelatedIntegers4.Add( EntityUnderCursor.PrimaryKeyID );
                    remoteCommand.RelatedMagnitude = InputFlags.Has( ArcenInputFlags.OrdersReachStationaryFlagshipsOrSkipRegularOnes ) ? 1 : 0;
                    remoteCommand.RelatedEntityIDs.AddRange( workingEntityIDs );
                    
                    workingEntityIDs.Clear();
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), remoteCommand, true );
                }

                workingEntityIDs.Clear();

                bool hasAnyThatCanActuallyShootIt = false;
                foreach ( GameEntity_Squad e in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
                {
                    if ( e.GetPlanetIndexSafe() != entityUnderCursorPlanetIndex )
                        continue; //skip anything that is NOT on my current planet

                    haveAnySelectedShips = true;

                    if ( e.TypeData.HasCustomMoveOrderAndNoOtherDirectCommandsCanBeGivenFromPlayer )
                        continue; //these types ignore attack orders

                    if ( e.GetIsNonFlagshipInLoadingModeMode() )
                        continue;

                    if ( e.CanTargetWithWeapon( enemySquad ) )
                        hasAnyThatCanActuallyShootIt = true;
                    else
                    {
                        hadFailuresToHit = true;
                        continue;
                    }

                    workingEntityIDs.Add( e.PrimaryKeyID );
                }

                if ( workingEntityIDs.Count > 0 && hasAnyThatCanActuallyShootIt )
                { 
                    didAnyAttackBits = true;
                    hasDoneSomethingYet = true;
                    
                    var command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.Attack], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );

                    command.PlanetOrderWasIssuedFrom = Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed();
                    command.ToBeQueued = InputFlags.Has( ArcenInputFlags.Additive );
                    command.RelatedMagnitude = InputFlags.Has( ArcenInputFlags.OrdersReachStationaryFlagshipsOrSkipRegularOnes ) ? 1 : 0;
                    command.RelatedIntegers4.Add( EntityUnderCursor.PrimaryKeyID );
                    command.RelatedEntityIDs.AddRange( workingEntityIDs );
                    workingEntityIDs.Clear();

                    //UnityEngine.Debug.Log( "attack:" + command.WriteToStringInefficient() );
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                }

                Mat.ReleaseTemporaryIntList( workingEntityIDs );

                if ( didAnyAttackBits )
                    return SecondaryClickResult.DidAnyNeededActions;

                if ( !haveAnySelectedShips )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand(
                        "<color=#777777>" + DateTime.Now.ToShortTimeString() + ":" + DateTime.Now.Second +
                        "</color> Please select some ships before trying to attack.", ChatType.ShowLocallyOnly,
                        "CannotDoThatThing", null );
                }
                else if ( hadFailuresToHit )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand(
                        "<color=#777777>" + DateTime.Now.ToShortTimeString() + ":" + DateTime.Now.Second +
                        "</color> None of your selected ships can currently damage " + enemySquad.TypeData.DisplayName +
                        ".  Check its tooltip for invulnerabilities.", ChatType.ShowLocallyOnly, "CannotDoThatThing",
                        null );
                }
                else
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand(
                        "<color=#777777>" + DateTime.Now.ToShortTimeString() + ":" + DateTime.Now.Second +
                        "</color> None of your selected ships are ready or able to attack this.  They are all in loading mode, or unable to leave another planet, or lack weapons, etc.",
                        ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                }

                return SecondaryClickResult.DidAnyNeededActions;
            }
            #endregion

            // clicked on friendly (or neutral?)
            #region clicked friend
            {
                int delaySeconds = 0;
                if ( EntityUnderCursor is GameEntity_Squad )
                    delaySeconds = ((GameEntity_Squad)EntityUnderCursor).RepairImpossibleForSeconds;

                if ( EntityUnderCursor.TypeData.ImmuneToRepairs || delaySeconds != 0 )
                    return SecondaryClickResult.ClickedOnWhatMayAsWellBeEmptySpace;
                
                //GameEntity_Squad alliedSquad = (GameEntity_Squad)EntityUnderCursor;
                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.Assist], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.PlanetOrderWasIssuedFrom = Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed();
                command.ToBeQueued = InputFlags.Has( ArcenInputFlags.Additive );
                command.RelatedIntegers4.Add( EntityUnderCursor.PrimaryKeyID );
                command.RelatedMagnitude = InputFlags.Has( ArcenInputFlags.OrdersReachStationaryFlagshipsOrSkipRegularOnes ) ? 1 : 0;
                foreach ( GameEntity_Squad selected in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
                {
                    if ( selected.GetPlanetIndexSafe() != entityUnderCursorPlanetIndex )
                        continue; //skip anything that is NOT on my current planet

                    //don't check for rebuild, rebuild just happens automatically and isn't controlled by the player
                    //Having the check enabled made it hard to maneouver combat fleets in battle if you were in a bunched formation
                    if ( !selected.DataForMark.CanAssist_Claim &&
                         !selected.DataForMark.CanAssist_SelfConstruction &&
                         !selected.DataForMark.CanAssist_FactoryConstruction &&
                         !selected.DataForMark.CanAssist_Repair )
                        continue;
                    if ( selected.TypeData.HasCustomMoveOrderAndNoOtherDirectCommandsCanBeGivenFromPlayer )
                        continue; //these types ignore all move orders
                    if ( selected.GetIsNonFlagshipInLoadingModeMode() )
                        continue;
                    command.RelatedEntityIDs.Add( selected.PrimaryKeyID );
                }

                if ( command.RelatedEntityIDs.Count > 0 )
                {
                    hasDoneSomethingYet = true;
                    //UnityEngine.Debug.Log( "assist:" + command.WriteToStringInefficient() );
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                    return SecondaryClickResult.DidAnyNeededActions;
                }
                else //prevents having a leak!
                    command.ReturnToPool();

                return SecondaryClickResult.ClickedOnWhatMayAsWellBeEmptySpace;
            }
            #endregion
        }

        #endregion

        #region BringRemoteShipsToPlanet

        public static void BringRemoteShipsToPlanet( Planet planet, GameEntity_Squad TargetSquadOrNull, bool ToBeQueued, ArcenInputFlags InputFlags, PerFactionPathCache PathCacheData )
        {
            // If there are ships on remote planets, set up wormhole commands to bring them to the Planet
            // Note that I need multiple wormhole commands to 
            // Note that I'll need to generate a separate Attack command for those which is queued
            // I know which ships will get the queued version by looking at the entitiesOnDifferentPlanets
            
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null )
                return; //spectator mode

            DictionaryOfLists<Planet, SafeSquadWrapper> entitiesOnDifferentPlanets = Planet.GetTemporaryPlanetDictOfSquadLists( "EndpointFunctions-BringRemoteShipsToPlanet-entitiesOnDifferentPlanets", 10f );
            if ( entitiesOnDifferentPlanets == null ) //blocked for teardown/shutdown; bail
                return;

            foreach ( GameEntity_Squad selected in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
            {
                // This ship is already at the target planet.
                if ( !ToBeQueued &&
                     selected.Planet == planet)
                    continue;

                // This ship is already going to the target planet.
                if ( ToBeQueued &&
                     selected.GetDestinationPlanet() == planet )
                    continue;

                if ( selected.DataForMark.Speed <= 0 )
                    continue;

                if ( selected.TypeData.FleetMembershipStyle == FleetMembershipStyle.Planetary )
                    continue;

                // Send everyone selected, i don't care if you can't shoot it, everyone else is going.
                //if ( TargetSquadOrNull != null && !selected.GetHasAnyWeaponThatCanHitAssumingItIsEnemy( TargetSquadOrNull, true ) && !selected.TypeData.IsFleetLeader )
                    //return DelReturn.Continue;

                if ( selected.TypeData.HasCustomMoveOrderAndNoOtherDirectCommandsCanBeGivenFromPlayer )
                    continue; //these types ignore all move orders

                if ( selected.GetIsNonFlagshipInLoadingModeMode() )
                    continue;

                Planet currentOrDestinationPlanet = selected.Planet;

                if ( ToBeQueued )
                    currentOrDestinationPlanet = selected.GetDestinationPlanet();

                entitiesOnDifferentPlanets[currentOrDestinationPlanet].Add( selected );
            }

            if ( entitiesOnDifferentPlanets.GetCountOfLists() > 0 )
            {
                foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in entitiesOnDifferentPlanets )
                {
                    GameCommand wormholecommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_Player], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    wormholecommand.RelatedString = "PView_PlayerGather";
                    wormholecommand.RelatedMagnitude = InputFlags.Has( ArcenInputFlags.OrdersReachStationaryFlagshipsOrSkipRegularOnes ) ? 1 : 0;

                    wormholecommand.PlanetOrderWasIssuedFrom = -1;
                    wormholecommand.ToBeQueued = ToBeQueued;
                    for ( int j = 0; j < pair.Value.Count; j++ )
                        wormholecommand.RelatedEntityIDs.Add( pair.Value[j].GetPrimaryKeyID() );
                    
                    PathingMode mode = PathingHelper.GetPathingModeForLocalPlayer();
                    PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( localFaction, "LocalFac-BringRemoteShipsToPlanet", pair.Key, planet, mode, 
                        Engine_AIW2.Instance.MainThreadContext_ClientOrHost, PathCacheData );

                    //This typically means the GameEntity is already on the planet
                    if ( pathCache == null || pathCache.PathToReadOnly.Count <= 0 )
                        continue; 

                    for ( int j = 0; j < pathCache.PathToReadOnly.Count; j++ )
                        wormholecommand.RelatedIntegers.Add( pathCache.PathToReadOnly[j].Index );

                    if ( wormholecommand.RelatedEntityIDs.Count > 0 )
                        World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), wormholecommand, true );
                    else
                        wormholecommand.ReturnToPool();
                }
            }

            Planet.ReleaseTemporaryPlanetDictOfSquadLists( entitiesOnDifferentPlanets );
        }
        #endregion
    }

    public enum QuickSelectionType
    {
        None,
        CommandStation,
        MobileMilitary,
        FactoriesForPlayerMobileFleets,
        CloakingUnits,
        MobileFlagships,
        BattlestationsAndCitadels,
        Snipers,
        NonFlagships,
        Melee,
        TractorUnits,
        Engineers,
    }

    public enum SecondaryClickResult
    {
        NothingWasThere,
        ClickedOnWhatMayAsWellBeEmptySpace,
        DidAnyNeededActions,
    }
}
