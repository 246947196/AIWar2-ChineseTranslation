using Arcen.Universal;
using Arcen.AIW2.Core;
using System;


namespace Arcen.AIW2.External
{
    public class Input_FleetHandler : BaseInputHandler
    {
        public override void HandleInner( Int32 Int1, InputActionTypeData InputActionType )
        {
            if ( ArcenUI.CurrentlyShownWindowsWith_PreventsNormalInputHandlers.Count > 0 )
                return;

            switch ( InputActionType.InternalName )
            {
                case "RemoveFleetGroup":
                    {
                        var fleets = Fleet.GetTemporaryFleetList("Input_FleetHandler-inputRemoveFleetGroup", 10);
                        if ( fleets == null ) //blocked for teardown/shutdown; bail
                            return;

                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();

                        foreach ( GameEntity_Squad selected in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
                        {
                            if ( selected.GetFactionOrNull_Safe() != localFaction )
                                continue;

                            var fleet = selected.FleetMembership.Fleet;
                            if (fleets.Contains(fleet))
                                continue;

                            fleets.Add(fleet);
                        }

                        foreach ( var fleet in fleets )
                        {
                            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                            command.RelatedIntegers.Add( fleet.FleetID );
                            command.RelatedString = "FleetKeybindIndex";
                            command.RelatedIntegers2.Add( -1 );
                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                        }

                        Fleet.ReleaseTemporaryFleetList(fleets);
                    }
                    break;
                case "CycleFleetBehavior":
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction == null )
                            break;
                        foreach ( Fleet fleet in World_AIW2.Instance.Fleets( localFaction, FleetStatus.CenterpieceMustLive ) )
                        {
                            if ( !fleet.IsConsideredSelected_NonSim )
                                continue;
                            FleetBehavior next = (FleetBehavior)( ( (int)fleet.Behavior + 1 ) % 3 );
                            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                            command.RelatedIntegers.Add( fleet.FleetID );
                            command.RelatedString = "SetFleetBehavior";
                            command.RelatedIntegers2.Add( (int)next );
                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                        }
                    }
                    break;
                case "SelectFleetGroup_1":
                case "SelectFleetGroup_2":
                case "SelectFleetGroup_3":
                case "SelectFleetGroup_4":
                case "SelectFleetGroup_5":
                case "SelectFleetGroup_6":
                case "SelectFleetGroup_7":
                case "SelectFleetGroup_8":
                case "SelectFleetGroup_9":
                case "SelectFleetGroup_10":
                    {
                        if ( !World.Instance.IsLoaded )
                            return;
                        if ( InputCaching.inputAssignFleetGroup.CalculateIsKeyDownNow_IgnoreConflicts() )
                        {
                            bool addOnly = false;
                            if ( InputCaching.inputAddFleetGroup.CalculateIsKeyDownNow_IgnoreConflicts() )
                                addOnly = true;

                            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();

                            Dictionary<Fleet, int> fleetsToAssign = Fleet.GetTemporaryFleetDictOfInts( "Input_FleetHandler-fleetsToAssign", 10f );
                            if ( fleetsToAssign == null ) //blocked for teardown/shutdown; bail
                                return;
                            Dictionary<Fleet, int> fleetsToRemove = Fleet.GetTemporaryFleetDictOfInts( "Input_FleetHandler-fleetsToRemove", 10f );
                            if ( fleetsToRemove == null ) //blocked for teardown/shutdown; bail
                            {
                                Fleet.ReleaseTemporaryFleetDictOfInts( fleetsToAssign );
                                return;
                            }

                            foreach ( GameEntity_Squad selected in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
                            {
                                if ( selected.GetFactionOrNull_Safe() != localFaction )
                                    continue; //in case we have selected something not belonging to us

                                fleetsToAssign[selected.FleetMembership.Fleet] = 1;
                            }

                            int newNumber = InputActionType.RelatedInt1;
                            if (!addOnly)
                            {
                                foreach ( Fleet fleet in World_AIW2.Instance.Fleets( localFaction, FleetStatus.AnyStatus ) )
                                {
                                    if ( fleet.TiedToKeybindIndexOneIndexed == newNumber )
                                    {
                                        if ( !fleetsToAssign.ContainsKey( fleet ) )
                                            fleetsToRemove[fleet] = 1;
                                    }
                                }
                            }

                            if ( fleetsToAssign.Count <= 0 )
                                World_AIW2.Instance.QueueChatMessageOrCommand( "未选择舰队来分配到此快捷键！", ChatType.ShowLocallyOnly,
                                    "CannotDoThatThing", null );
                            else
                            {
                                World_AIW2.Instance.QueueChatMessageOrCommand( fleetsToAssign.Count +
                                    " 个舰队已分配到此快捷键" +
                                    (fleetsToRemove.Count > 0 ? "，并从该快捷键移除了 " + fleetsToRemove.Count + " 个舰队。" : "。"),
                                    ChatType.ShowLocallyOnly, string.Empty, null );

                                foreach ( KeyValuePair<Fleet, int> kv in fleetsToAssign )
                                {
                                    GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                                    command.RelatedIntegers.Add( kv.Key.FleetID ); //FleetID
                                    command.RelatedString = "FleetKeybindIndex";
                                    command.RelatedIntegers2.Add( newNumber );
                                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                                }

                                foreach ( KeyValuePair<Fleet, int> kv in fleetsToRemove )
                                {
                                    GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                                    command.RelatedIntegers.Add( kv.Key.FleetID ); //FleetID
                                    command.RelatedString = "FleetKeybindIndex";
                                    command.RelatedIntegers2.Add( -1 );
                                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                                }
                            }

                            Fleet.ReleaseTemporaryFleetDictOfInts( fleetsToAssign );
                            Fleet.ReleaseTemporaryFleetDictOfInts( fleetsToRemove );
                        }
                        
                        if ( InputActionType.CalculateIsDoublePress() )
                            Input_FleetHandler.SelectFleetGroup( InputActionType.RelatedInt1, null, FleetSelectionType.CenterOnly, GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                        else
                            Input_FleetHandler.SelectFleetGroup( InputActionType.RelatedInt1, null, FleetSelectionType.SelectOnly, GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    }
                    break;
            }
        }

        public static void SelectFleetGroup(int FleetGroupIndexOrNegative, Fleet SpecificFleet, FleetSelectionType SelectionType, GameCommandSource Source )
        {
            bool isAdditive = Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.Additive );
            bool isSubtractive = !isAdditive && Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.Subtractive );
            
            bool clearSelectionFirst = !isAdditive && !isSubtractive;
            
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null )
                return;

            bool doSelecting = false;
            switch ( SelectionType )
            {
                case FleetSelectionType.SelectOnly:
                case FleetSelectionType.SelectAndCenter:
                    doSelecting = true;
                    break;
            }

            List<Fleet> workingFleetsIJustSelected = Fleet.GetTemporaryFleetList( "Input_FleetHandler-workingFleetsIJustSelected", 10f );
            if ( workingFleetsIJustSelected == null ) //blocked for teardown/shutdown; bail
                return;

            if ( doSelecting )
            {
                bool wasFleetAlreadySelected = (SpecificFleet != null && SpecificFleet.IsConsideredSelected_NonSim);

                if ( !wasFleetAlreadySelected )
                {
                    foreach ( Fleet fleet in World_AIW2.Instance.Fleets( localFaction, FleetStatus.CenterpieceMustLive ) )
                    {
                        if ( (FleetGroupIndexOrNegative > 0 && fleet.TiedToKeybindIndexOneIndexed == FleetGroupIndexOrNegative) || fleet == SpecificFleet )
                        {
                            if ( fleet.IsConsideredSelected_NonSim )
                            {
                                wasFleetAlreadySelected = true;
                                break;
                            }
                        }
                    }
                }

                if ( clearSelectionFirst )
                    Engine_AIW2.Instance.ClearSelection( true, true );

                //do the actual selection
                foreach ( Fleet fleet in World_AIW2.Instance.Fleets( localFaction, FleetStatus.CenterpieceMustLive ) )
                {
                    if ( (FleetGroupIndexOrNegative > 0 && fleet.TiedToKeybindIndexOneIndexed == FleetGroupIndexOrNegative) || fleet == SpecificFleet )
                    {
                        workingFleetsIJustSelected.Add( fleet );

                        if ( !fleet.IsConsideredSelected_NonSim )
                            fleet.MarkAsSelected();

                        foreach ( GameEntity_Squad squad in fleet.Entities )
                        {
                            if ( !squad.GetIsSelected() )
                            {
                                if ( squad.GetMayBeSelected( UnitSelectionType.SelectedBecauseFleetSelected ) )
                                    squad.Select( true, "NewlySelectingFleet" );
                            }
                        }
                    }
                }
            }

            bool doCentering = false;
            switch ( SelectionType )
            {
                case FleetSelectionType.CenterOnly:
                case FleetSelectionType.SelectAndCenter:
                    doCentering = true;
                    break;
            }

            if ( doCentering )
            {
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                bool foundOneCenterpieceOnCurrentPlanet = false;

                workingFleetsIJustSelected.Clear();
                foreach ( Fleet fleet in World_AIW2.Instance.Fleets( localFaction, FleetStatus.CenterpieceMustLive ) )
                {
                    if ( (FleetGroupIndexOrNegative > 0 && fleet.TiedToKeybindIndexOneIndexed == FleetGroupIndexOrNegative) || fleet == SpecificFleet )
                    {
                        workingFleetsIJustSelected.Add( fleet );
                    }
                }

                Fleet workingFleet;
                for ( int i = 0; i < workingFleetsIJustSelected.Count; i++ )
                {
                    workingFleet = workingFleetsIJustSelected[i];
                    GameEntity_Squad centerpieceSquad = workingFleet.Centerpiece.GetSquad();
                    if ( centerpieceSquad != null && centerpieceSquad.Planet == planet )
                    {
                        if ( !foundOneCenterpieceOnCurrentPlanet )
                        {
                            foundOneCenterpieceOnCurrentPlanet = true;
                            //if the centerpiece of this squad is on the current planet, center on that
                            if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                                Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( centerpieceSquad.Planet, false );
                            else
                                Engine_AIW2.Instance.PresentationLayer.CenterPlanetViewOnEntity( centerpieceSquad, false );
                            break;
                        }
                    }
                }

                if ( !foundOneCenterpieceOnCurrentPlanet )
                {
                    //If we want to change the player's view (because we had selected everything already)
                    //to be centered on the control group, but haven't moved the view yet, figure out where to move it
                    for ( int i = 0; i < workingFleetsIJustSelected.Count; i++ )
                    {
                        workingFleet = workingFleetsIJustSelected[i];
                        GameEntity_Squad centerpieceSquad = workingFleet.Centerpiece.GetSquad();
                        if ( centerpieceSquad != null )
                        {
                            Engine_AIW2.Instance.PresentationLayer.ReactToLeavingPlanetView( planet );
                            planet = centerpieceSquad.Planet;
                            if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                                Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( planet, false );
                            else
                            {
                                World_AIW2.Instance.SwitchViewToPlanet( planet );
                                Engine_AIW2.Instance.PresentationLayer.CenterPlanetViewOnEntity( centerpieceSquad, true );
                                Engine_AIW2.Instance.PresentationLayer.ReactToEnteringPlanetView( planet );
                            }
                            break;
                        }
                    }
                }
            }

            Fleet.ReleaseTemporaryFleetList( workingFleetsIJustSelected );
        }
    }

    public enum FleetSelectionType
    {
        SelectOnly,
        SelectAndCenter,
        CenterOnly
    }
}
