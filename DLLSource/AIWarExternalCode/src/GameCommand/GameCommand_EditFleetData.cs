using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    /// <summary>
     /// Each piece of data about a fleet is changed one bit at a time, and rather than having a bajillion message types
     /// for all those pieces of data, we're just handling it centrally.
     /// </summary>
    public class GameCommand_EditFleetData : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            try
            {
                if ( command.RelatedIntegers.Count == 0 )
                {
                    if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                        ArcenDebugging.ArcenDebugLogSingleLine( "No RelatedIntegers passed to GameCommand_EditFleetData!", Verbosity.ShowAsError );
                    return;
                }
                Fleet fleetToManage = World_AIW2.Instance.GetFleetByID( command.RelatedIntegers.First );
                if ( fleetToManage == null )
                {
                    if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Null fleetToManage found in GameCommand_EditFleetData when attempting to get ID " + command.RelatedIntegers.First + "!", Verbosity.ShowAsError );
                    return;
                }

                if ( command.RelatedString == null )
                {
                    if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Null RelatedString passed to GameCommand_EditFleetData!", Verbosity.ShowAsError );
                    return;
                }
                switch ( command.RelatedString )
                {
                    case "EditFleetName":
                        this.EditFleetName( command, fleetToManage, context );
                        break;
                    case "ToggleFactoryConstructionStatus":
                        this.ToggleFactoryConstructionStatus( command, fleetToManage, context );
                        break;
                    case "ToggleFlagshipAllowedToUseMovementModesStatus":
                        this.ToggleFlagshipAllowedToUseMovementModesStatus( command, fleetToManage, context );
                        break;
                    case "ToggleIsFleetFlagshipStationaryStatusInverted":
                        this.ToggleIsFleetFlagshipStationaryStatusInverted( command, fleetToManage, context );
                        break;
                    case "ToggleIsFleetOnPlayerWatchlist":
                        this.ToggleIsFleetOnPlayerWatchlist( command, fleetToManage, context );
                        break;
                    case "FleetKeybindIndex":
                        this.FleetKeybindIndex( command, fleetToManage, context );
                        break;
                    case "ToggleFleetMembershipConstructionStatus":
                        this.ToggleFleetMembershipConstructionStatus( command, fleetToManage, context );
                        break;
                    case "SwapFleetMembers":
                        this.SwapFleetMembers( command, fleetToManage, context );
                        break;
                    case "SwapFleetMemberWithEmpty":
                        this.SwapFleetMemberWithEmpty( command, fleetToManage, context );
                        break;
                    case "SwapAllNonFlagshipFleetMembers":
                        this.SwapAllNonFlagshipFleetMembers( command, fleetToManage, context );
                        break;
                    case "GiftFleetToFaction":
                        this.GiftFleetToFaction( command, fleetToManage, context );
                        break;
                    case "UpgradeFleetViaScience":
                        this.UpgradeFleetViaScience( command, fleetToManage, context );
                        break;
                    case "SetFleetBehavior":
                        this.SetFleetBehavior( command, fleetToManage, context );
                        break;
                    case "ClearPlayerOverride":
                        fleetToManage.PlayerOverridePlanetIndex = -1;
                        break;

                    default:
                        if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Unknown RelatedString '" + command.RelatedString + "' passed to GameCommand_EditFleetData!", Verbosity.ShowAsError );
                        break;
                }
            }
            catch ( Exception e )
            {
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                    return; //no worries; we'll get the info from the host.
                throw e; //otherwise report the error
            }
        }

        #region EditFleetName
        private void EditFleetName( GameCommand command, Fleet fleetToManage, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedString2 == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "EditFleetName: RelatedString2 null!", Verbosity.ShowAsError );
                return;
            }
            string newFleetName = command.RelatedString2;
            if ( newFleetName.Length <= 0 )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "EditFleetName: Passed a blank newFleetName '" + newFleetName + "'!", Verbosity.ShowAsError );
                return;
            }
            fleetToManage.NameRaw = newFleetName;
        }
        #endregion        

        #region ToggleFactoryConstructionStatus
        private void ToggleFactoryConstructionStatus( GameCommand command, Fleet fleetToManage, ArcenClientOrHostSimContextCore context )
        {
            fleetToManage.IsFleetConstructionPaused = command.RelatedBool;
        }
        #endregion        

        #region FleetKeybindIndex
        private void FleetKeybindIndex( GameCommand command, Fleet fleetToManage, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedIntegers2.Count == 0 )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "FleetKeybindIndex: RelatedIntegers2.Count == 0!", Verbosity.ShowAsError );
                return;
            }
            fleetToManage.TiedToKeybindIndexOneIndexed = (Int16)command.RelatedIntegers2.First;
            //this will keep it from being auto-assigned like it is by default when you get a new fleet
            if ( fleetToManage.TiedToKeybindIndexOneIndexed == 0 )
                fleetToManage.TiedToKeybindIndexOneIndexed = -1;
        }
        #endregion        

        #region ToggleFleetMembershipConstructionStatus
        private void ToggleFleetMembershipConstructionStatus( GameCommand command, Fleet fleetToManage, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedString2 == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "ToggleFleetMembershipConstructionStatus: RelatedString2 null!", Verbosity.ShowAsError );
                return;
            }

            GameEntityTypeData typeDataToEdit = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( typeDataToEdit == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "ToggleFleetMembershipConstructionStatus: GameEntityTypeData '" + command.RelatedString2 + "' could not be found!", Verbosity.ShowAsError );
                return;
            }

            if ( command.RelatedIntegers2.Count == 0 )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "ToggleFleetMembershipConstructionStatus: No RelatedIntegers2 passed in!", Verbosity.ShowAsError );
                return;
            }
            int uniqueTypeDataDifferentiatorForDuplicates = command.RelatedIntegers2.First;

            FleetMembership mem = fleetToManage.GetButDoNotAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates( typeDataToEdit, uniqueTypeDataDifferentiatorForDuplicates );
            if ( mem == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "ToggleFleetMembershipConstructionStatus: FleetMembership '" + command.RelatedString2 + "' with unique ID " +
                        uniqueTypeDataDifferentiatorForDuplicates + " could not be found!", Verbosity.ShowAsError );
                return;
            }

            mem.IsFleetMembershipConstructionPaused = command.RelatedBool;
        }
        #endregion        

        #region SwapFleetMembers
        private void SwapFleetMembers( GameCommand command, Fleet fleetToManage, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedString2 == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMembers: RelatedString2 null!", Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedString3 == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMembers: RelatedString3 null!", Verbosity.ShowAsError );
                return;
            }

            GameEntityTypeData typeDataToSwapOut = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( typeDataToSwapOut == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMembers: GameEntityTypeData '" + command.RelatedString2 + "' could not be found!", Verbosity.ShowAsError );
                return;
            }

            GameEntityTypeData typeDataToSwapIn = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString3 );
            if ( typeDataToSwapIn == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMembers: GameEntityTypeData '" + command.RelatedString3 + "' could not be found!", Verbosity.ShowAsError );
                return;
            }

            if ( command.RelatedIntegers3.Count != 2 )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMembers: RelatedIntegers3.Count != 2!", Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedIntegers4.Count != 2 )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMembers: RelatedIntegers4.Count != 2!", Verbosity.ShowAsError );
                return;
            }

            int memToSwapOutOldID = 0;
            int memPairToSwapInOldID = 0;
            {
                int _i = 0;
                foreach ( var _v in command.RelatedIntegers3 )
                {
                    if ( _i == 0 ) memToSwapOutOldID = _v;
                    else if ( _i == 1 ) { memPairToSwapInOldID = _v; break; }
                    _i++;
                }
            }

            //command.RelatedIntegers3.Add( memToSwapOut.UniqueTypeDataDifferentiatorForDuplicates );
            //command.RelatedIntegers3.Add( memPairToSwapIn.UniqueTypeDataDifferentiatorForDuplicates );

            int nextIDOnFleet1ForFleet2sType = 0;
            int nextIDOnFleet2ForFleet1sType = 0;
            {
                int _i = 0;
                foreach ( var _v in command.RelatedIntegers4 )
                {
                    if ( _i == 0 ) nextIDOnFleet1ForFleet2sType = _v;
                    else if ( _i == 1 ) { nextIDOnFleet2ForFleet1sType = _v; break; }
                    _i++;
                }
            }

            //command.RelatedIntegers4.Add( memToSwapOut.Fleet.GetNextUniqueIntToUseOfMatchingMembershipGroupsBasedOnSquadType( memPairToSwapIn.TypeData ) ); //next ID on fleet1 for fleet2's type
            //command.RelatedIntegers4.Add( memPairToSwapIn.Fleet.GetNextUniqueIntToUseOfMatchingMembershipGroupsBasedOnSquadType( memToSwapOut.TypeData ) ); //next ID on fleet2 for fleet1's type

            FleetMembership memOut = fleetToManage.GetButDoNotAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates( typeDataToSwapOut, memToSwapOutOldID );
            if ( memOut == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMembers: FleetMembership '" + command.RelatedString2 + "' could not be found!", Verbosity.ShowAsError );
                return;
            }
            if ( memOut.TypeData.IsFleetLeader )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMembers: Tried to swap a fleet leader between fleets, which is not valid!", Verbosity.ShowAsError );
                return;
            }

            if ( command.RelatedIntegers2.Count == 0 )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMembers: No RelatedIntegers2 passed in!", Verbosity.ShowAsError );
                return;
            }
            Fleet fleetToSwapFrom = World_AIW2.Instance.GetFleetByID( command.RelatedIntegers2.First );
            if ( fleetToSwapFrom == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMembers: Null fleetToSwapFrom found when attempting to get ID " + command.RelatedIntegers2.First + "!", Verbosity.ShowAsError );
                return;
            }

            FleetMembership memIn = fleetToSwapFrom.GetButDoNotAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates( typeDataToSwapIn, memPairToSwapInOldID );
            if ( memIn == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMembers: FleetMembership '" + command.RelatedString3 + "' could not be found!", Verbosity.ShowAsError );
                return;
            }
            if ( memIn.TypeData.IsFleetLeader )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMembers: Tried to swap a fleet leader between fleets, which is not valid!", Verbosity.ShowAsError );
                return;
            }
            bool isOfSamePlayer = memOut.Fleet.Faction == memIn.Fleet.Faction;

            Planet planetMustBeOn = memIn.Fleet?.Centerpiece.GetSquad()?.Planet;

            if ( !memOut.TypeData.IsFleetLeader ) //only delete non-fleet leaders
            {
                memOut.DespawnAllContentsFromSwap( isOfSamePlayer, planetMustBeOn, context.GetHostOnlyContext() );
            }

            planetMustBeOn = memOut.Fleet?.Centerpiece.GetSquad()?.Planet;
            //ArcenDebugging.ArcenDebugLogSingleLine( "wrapperCounterA: " + wrapperCounter, Verbosity.DoNotShow );
            //wrapperCounter = 0;
            if ( !memIn.TypeData.IsFleetLeader ) //only delete non-fleet leaders
            {
                memIn.DespawnAllContentsFromSwap( isOfSamePlayer, planetMustBeOn, context.GetHostOnlyContext() );
                //ArcenDebugging.ArcenDebugLogSingleLine( "wrapperCounterB: " + wrapperCounter, Verbosity.DoNotShow );
            }

            Fleet.SwapMembershipWithAnotherfleet( memIn, memOut, nextIDOnFleet1ForFleet2sType, nextIDOnFleet2ForFleet1sType, context );
        }
        #endregion

        #region SwapFleetMemberWithEmpty
        private void SwapFleetMemberWithEmpty( GameCommand command, Fleet fleetToManage, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedString2 == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMemberWithEmpty: RelatedString2 null!", Verbosity.ShowAsError );
                return;
            }

            GameEntityTypeData typeDataToSwapOut = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( typeDataToSwapOut == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMemberWithEmpty: GameEntityTypeData '" + command.RelatedString2 + "' could not be found!", Verbosity.ShowAsError );
                return;
            }

            if ( command.RelatedIntegers3.Count != 1 )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMemberWithEmpty: RelatedIntegers3.Count != 1!", Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedIntegers4.Count != 1 )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMemberWithEmpty: RelatedIntegers4.Count != 1!", Verbosity.ShowAsError );
                return;
            }

            int memOldID = command.RelatedIntegers3.First;

            int nextIDOnNewFleet = command.RelatedIntegers4.First;

            FleetMembership memOut = fleetToManage.GetButDoNotAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates( typeDataToSwapOut, memOldID );
            if ( memOut == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMemberWithEmpty: FleetMembership '" + command.RelatedString2 + "' could not be found!", Verbosity.ShowAsError );
                return;
            }

            if ( command.RelatedIntegers2.Count == 0 )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMemberWithEmpty: No RelatedIntegers2 passed in!", Verbosity.ShowAsError );
                return;
            }
            Fleet fleetWithEmptySlot = World_AIW2.Instance.GetFleetByID( command.RelatedIntegers2.First );
            if ( fleetWithEmptySlot == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMemberWithEmpty: Null fleetWithEmptySlot found when attempting to get ID " + command.RelatedIntegers2.First + "!", Verbosity.ShowAsError );
                return;
            }
            if ( fleetWithEmptySlot.CalculateRemainingShipLineSlotCount() <= 0 )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMemberWithEmpty: fleetWithEmptySlot did not actually have any empty slots!", Verbosity.ShowAsError );
                return;
            }
            if ( memOut.TypeData.IsElite && fleetWithEmptySlot.CalculateIsEliteSlotFilled() )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMemberWithEmpty: fleetWithEmptySlot does not have room for any more elite lines!", Verbosity.ShowAsError );
                return;
            }
            if ( memOut.TypeData.IsFleetLeader )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapFleetMemberWithEmpty: Tried to swap a fleet leader with empty, which is not valid!", Verbosity.ShowAsError );
                return;
            }

            Planet planetMustBeOn = fleetWithEmptySlot?.Centerpiece.GetSquad()?.Planet;

            {
                //int wrapperCounter = 0;
                //StringBuilder wrapperOutput = new StringBuilder();
                bool isOfSamePlayer = memOut.Fleet.Faction == fleetWithEmptySlot.Faction;

                memOut.DespawnAllContentsFromSwap( isOfSamePlayer, planetMustBeOn, context.GetHostOnlyContext() );

                //ArcenDebugging.ArcenDebugLogSingleLine( "wrapperCounterC: " + wrapperCounter, Verbosity.DoNotShow );
                //ArcenDebugging.ArcenDebugLogSingleLine( "OutputC:\n" + wrapperOutput.ToString(), Verbosity.DoNotShow );
            }
            //memOut.EntitiesOfFMem.ReportOnAdditions = false;

            Fleet.TakeMembershipFromAnotherfleet( fleetWithEmptySlot, memOut, nextIDOnNewFleet );
        }
        #endregion

        #region SwapAllNonFlagshipFleetMembers
        private void SwapAllNonFlagshipFleetMembers( GameCommand command, Fleet fleetToManage, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedIntegers2.Count == 0 )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapAllNonFlagshipFleetMembers: No RelatedIntegers2 passed in!", Verbosity.ShowAsError );
                return;
            }
            Fleet fleetToSwapWith = World_AIW2.Instance.GetFleetByID( command.RelatedIntegers2.First );
            if ( fleetToSwapWith == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SwapAllNonFlagshipFleetMembers: Null fleetToSwapWith found when attempting to get ID " + command.RelatedIntegers2.First + "!", Verbosity.ShowAsError );
                return;
            }

            Fleet fleet1 = fleetToManage;
            Fleet fleet2 = fleetToSwapWith;

            Planet planet1 = fleet1?.Centerpiece.GetSquad()?.Planet;
            Planet planet2 = fleet2?.Centerpiece.GetSquad()?.Planet;

            List<FleetMembership> linesMovedToFleet2 = FleetMembership.GetTemporaryFleetMembershipList( "GCEditFleetData-SwapAllNonFlagshipFleetMembers-linesMovedToFleet2", 10f );
            if ( linesMovedToFleet2 == null ) //blocked for teardown/shutdown; bail
                return;
            List<FleetMembership> linesMovedToFleet1 = FleetMembership.GetTemporaryFleetMembershipList( "GCEditFleetData-SwapAllNonFlagshipFleetMembers-linesMovedToFleet1", 10f );
            if ( linesMovedToFleet1 == null ) //blocked for teardown/shutdown; bail
            {
                FleetMembership.ReleaseTemporaryFleetMembershipList( linesMovedToFleet2 );
                return;
            }
            fleet1.SwapAllNonFleetLeaderMembershipsWithOtherFleet( fleet2, linesMovedToFleet2, linesMovedToFleet1 );

            bool isOfSamePlayer = fleet1.Faction == fleet2.Faction;

            foreach ( FleetMembership mem in linesMovedToFleet2 )
            {
                mem.DespawnAllContentsFromSwap( isOfSamePlayer, planet2, context.GetHostOnlyContext() );
            }
            foreach ( FleetMembership mem in linesMovedToFleet1 )
            {
                mem.DespawnAllContentsFromSwap( isOfSamePlayer, planet1, context.GetHostOnlyContext() );
            }

            FleetMembership.ReleaseTemporaryFleetMembershipList( linesMovedToFleet2 );
            FleetMembership.ReleaseTemporaryFleetMembershipList( linesMovedToFleet1 );
        }
        #endregion

        #region GiftFleetToFaction
        private void GiftFleetToFaction( GameCommand command, Fleet fleetToGift, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedIntegers2.Count != 1 )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "GiftFleetToFaction: RelatedIntegers2.Count != 1!", Verbosity.ShowAsError );
                return;
            }
            int otherFationIndex = command.RelatedIntegers2.First;
            Faction otherFaction = World_AIW2.Instance.GetFactionByIndex( otherFationIndex );
            if ( otherFaction == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "GiftFleetToFaction: otherFaction was null from index " + otherFationIndex + "!", Verbosity.ShowAsError );
                return;
            }
            Faction giftingFaction = fleetToGift.Faction;
            if ( giftingFaction == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "GiftFleetToFaction: giftingFaction was null!", Verbosity.ShowAsError );
                return;
            }
            if ( giftingFaction == otherFaction )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "GiftFleetToFaction: giftingFaction was otherFaction!", Verbosity.ShowAsError );
                return;
            }

            PlanetFaction giftingPlanetFaction = null;
            PlanetFaction otherPlanetFaction = null;
            Planet planetOfFleet = null;
            Fleet fleetToGiveInReturn = null;
            if ( fleetToGift.Category == FleetCategory.PlayerPlanetaryCommand )
            {
                planetOfFleet = fleetToGift.Planet;
                if ( planetOfFleet == null )
                {
                    if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                        ArcenDebugging.ArcenDebugLogSingleLine( "GiftFleetToFaction: planetOfFleet was null on a PlayerPlanetaryCommand!", Verbosity.ShowAsError );
                    return;
                }
                giftingPlanetFaction = planetOfFleet.GetPlanetFactionForFaction( giftingFaction );
                if ( giftingPlanetFaction == null )
                {
                    if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                        ArcenDebugging.ArcenDebugLogSingleLine( "GiftFleetToFaction: giftingPlanetFaction was null on a PlayerPlanetaryCommand!", Verbosity.ShowAsError );
                    return;
                }
                otherPlanetFaction = planetOfFleet.GetPlanetFactionForFaction( otherFaction );
                if ( otherPlanetFaction == null )
                {
                    if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                        ArcenDebugging.ArcenDebugLogSingleLine( "GiftFleetToFaction: otherPlanetFaction was null on a PlayerPlanetaryCommand!", Verbosity.ShowAsError );
                    return;
                }
                fleetToGiveInReturn = otherPlanetFaction.FleetUsedAtPlanet;
                if ( fleetToGiveInReturn == null )
                {
                    if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                        ArcenDebugging.ArcenDebugLogSingleLine( "GiftFleetToFaction: fleetToGiveInReturn was null on a PlayerPlanetaryCommand!", Verbosity.ShowAsError );
                    return;
                }
                if ( fleetToGiveInReturn.Category != FleetCategory.PlayerPlanetaryCommand )
                {
                    if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                        ArcenDebugging.ArcenDebugLogSingleLine( "GiftFleetToFaction: fleetToGiveInReturn did not have category PlayerPlanetaryCommand on a PlayerPlanetaryCommand!", Verbosity.ShowAsError );
                    return;
                }
                if ( giftingPlanetFaction.FleetUsedAtPlanet != fleetToGift )
                {
                    if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                        ArcenDebugging.ArcenDebugLogSingleLine( "GiftFleetToFaction: giftingPlanetFaction.FleetUsedAtPlanet != fleetToGift on a PlayerPlanetaryCommand!", Verbosity.ShowAsError );
                    return;
                }

                giftingPlanetFaction.FleetUsedAtPlanet = fleetToGiveInReturn;
                otherPlanetFaction.FleetUsedAtPlanet = fleetToGift;
            }

            //first refund any science if there were any upgrades to them
            if ( fleetToGift.AddedMarkLevelsForFleet_FromScience > 0 )
                fleetToGift.Faction.RefundFleetIfPossible( fleetToGift, false );
            //also for the one being swapped with us if there is one
            if ( fleetToGiveInReturn != null && fleetToGiveInReturn.AddedMarkLevelsForFleet_FromScience > 0 )
                fleetToGiveInReturn.Faction.RefundFleetIfPossible( fleetToGiveInReturn, false );

            //now start actually swapping them over

            fleetToGift.Faction = otherFaction;
            foreach ( GameEntity_Squad squad in fleetToGift.Entities )
            {
                //ships in the fleet may be scattered across planet planets.  Don't take any shortcuts
                PlanetFaction otherPFaction = squad.Planet.GetPlanetFactionForFaction( otherFaction );
                squad.PlanetFaction.RemoveEntity( squad );
                otherPFaction.AddEntity( squad, "Gift Fleet To Faction - A" );
                squad.SetPlanetFaction( otherPFaction, "Gift Fleet To Faction - A" );
                SquadRegistryInfo registryInfo = World_AIW2.Instance.GetEntityRegistryInfo_Squad( squad.PrimaryKeyID );
                if ( registryInfo != null )
                    registryInfo.Faction = otherFaction;
            }

            if ( fleetToGiveInReturn != null )
            {
                fleetToGiveInReturn.Faction = giftingFaction;
                foreach ( GameEntity_Squad squad in fleetToGiveInReturn.Entities )
                {
                    //ships in the fleet may be scattered across planet planets.  Don't take any shortcuts
                    PlanetFaction giftingPFaction = squad.Planet.GetPlanetFactionForFaction( giftingFaction );
                    squad.PlanetFaction.RemoveEntity( squad );
                    giftingPFaction.AddEntity( squad, "Gift Fleet To Faction - B" );
                    squad.SetPlanetFaction( giftingPFaction, "Gift Fleet To Faction - B" );
                    SquadRegistryInfo registryInfo = World_AIW2.Instance.GetEntityRegistryInfo_Squad( squad.PrimaryKeyID );
                    if ( registryInfo != null )
                        registryInfo.Faction = giftingFaction;
                }
            }
        }
        #endregion

        #region ToggleFlagshipAllowedToUseMovementModesStatus
        private void ToggleFlagshipAllowedToUseMovementModesStatus( GameCommand command, Fleet fleetToManage, ArcenClientOrHostSimContextCore context )
        {
            fleetToManage.IsFleetFlagshipAllowedToUseMovementModes = command.RelatedBool;
        }
        #endregion  

        #region ToggleIsFleetFlagshipStationaryStatusInverted
        private void ToggleIsFleetFlagshipStationaryStatusInverted( GameCommand command, Fleet fleetToManage, ArcenClientOrHostSimContextCore context )
        {
            fleetToManage.IsFleetFlagshipStationaryStatusOn = command.RelatedBool;
        }
        #endregion

        #region ToggleIsFleetOnPlayerWatchlist
        private void ToggleIsFleetOnPlayerWatchlist( GameCommand command, Fleet fleetToManage, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedIntegers.Count != 2 )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "ToggleIsFleetOnPlayerWatchlist: RelatedIntegers.Count != 2!", Verbosity.ShowAsError );
                return;
            }
            byte forPlayerAccount = 0;
            {
                int _i = 0;
                foreach ( var _v in command.RelatedIntegers )
                {
                    if ( _i == 1 ) { forPlayerAccount = (byte)_v; break; }
                    _i++;
                }
            }
            bool isOn = fleetToManage.GetIsFleetOnPlayerWatchlist( forPlayerAccount );
            if ( isOn == command.RelatedBool )
                return; //nothing to do here, it already matches

            if ( fleetToManage.IsFleetOnPlayerWatchlistForPlayerAccountIDs == null )
                fleetToManage.IsFleetOnPlayerWatchlistForPlayerAccountIDs = List<byte>.Create_WillNeverBeGCed( 200, "Fleet-IsFleetOnPlayerWatchlistForPlayerAccountIDs" );
            if ( command.RelatedBool )
            {
                if ( !fleetToManage.IsFleetOnPlayerWatchlistForPlayerAccountIDs.Contains( forPlayerAccount ) )
                    fleetToManage.IsFleetOnPlayerWatchlistForPlayerAccountIDs.Add( forPlayerAccount );
            }
            else
            {
                if ( fleetToManage.IsFleetOnPlayerWatchlistForPlayerAccountIDs.Contains( forPlayerAccount ) )
                    fleetToManage.IsFleetOnPlayerWatchlistForPlayerAccountIDs.Remove( forPlayerAccount );
            }
        }
        #endregion        

        #region UpgradeFleetViaScience
        private void UpgradeFleetViaScience( GameCommand command, Fleet fleetToManage, ArcenClientOrHostSimContextCore context )
        {
            if ( fleetToManage.Centerpiece.GetSquad() == null || !fleetToManage.Centerpiece.GetSquad().TypeData.IsUpgradeableByDirectScience )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "UpgradeFleetViaScience: Not IsUpgradeableByDirectScience, or centerpiece missing!", Verbosity.ShowAsError );
                return;
            }
            int scienceOrOtherResourceRequired = fleetToManage.GetScienceOrOtherResourceNeededForNextLevelUp( fleetToManage.AddedMarkLevelsForFleet_FromScience );
            if ( scienceOrOtherResourceRequired <= 0 )
            {
                //if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot upgrade fleet any further!", ChatType.ShowLocallyOnly, null );
                return;
            }
            UpgradeResourceStyle resourceNeeded = fleetToManage.GetResourceNeededForNextLevelUp();
            switch (resourceNeeded)
            {
                case UpgradeResourceStyle.Resource1:
                    if ( scienceOrOtherResourceRequired > fleetToManage.Faction.StoredFactionResourceOne )
                    {
                        //if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                            World_AIW2.Instance.QueueChatMessageOrCommand( "Not enough " + World_AIW2.Instance.Resource1DisplayName + " to upgrade fleet!", ChatType.ShowLocallyOnly, null );
                        return;
                    }
                    fleetToManage.Faction.StoredFactionResourceOne -= scienceOrOtherResourceRequired;
                    break;
                case UpgradeResourceStyle.Resource2:
                    if ( scienceOrOtherResourceRequired > fleetToManage.Faction.StoredFactionResourceTwo )
                    {
                        //if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                            World_AIW2.Instance.QueueChatMessageOrCommand( "Not enough " + World_AIW2.Instance.Resource2DisplayName + " to upgrade fleet!", ChatType.ShowLocallyOnly, null );
                        return;
                    }
                    fleetToManage.Faction.StoredFactionResourceTwo -= scienceOrOtherResourceRequired;
                    break;
                default:
                case UpgradeResourceStyle.Science:
                    if ( scienceOrOtherResourceRequired > fleetToManage.Faction.StoredScience )
                    {
                        //if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                            World_AIW2.Instance.QueueChatMessageOrCommand( "Not enough Science to upgrade fleet!", ChatType.ShowLocallyOnly, null );
                        return;
                    }
                    fleetToManage.Faction.StoredScience -= scienceOrOtherResourceRequired;
                    break;
            }

            fleetToManage.AddedMarkLevelsForFleet_FromScience++;

            fleetToManage.Faction.TechHistory.Add( TechHistoryEvent.Create_FleetBased( fleetToManage, scienceOrOtherResourceRequired, World_AIW2.Instance.GameSecond,
                fleetToManage.AddedMarkLevelsForFleet_FromScience, false ) );

            // check for achievements for local player, client or host is fine
            if ( fleetToManage.Faction.Config.IsFactionControlledByLocalPlayer )
            {
                foreach ( Achievement achievement in AchievementTable.Instance.Rows )
                {
                    if ( achievement.ConditionType == AchievementConditionType.UpgradeFleetByScience )
                    {
                        if ( achievement.GetIsAlreadyCompleteOrBlockedByCheating( AchievementOnClient.CanBeLoggedByClient ) )
                            continue; //skipping these is more efficient than checking them, especially for certain kinds
                        if ( fleetToManage.Centerpiece.GetSquad() != null && fleetToManage.Centerpiece.GetSquad().TypeData.GetMatches( achievement.ConditionStringMode, achievement.ConditionRelatedStrings ) )
                        {
                            if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                            { }
                        }
                    }
                }
            }
        }
        #endregion

        #region SetFleetBehavior
        private void SetFleetBehavior( GameCommand command, Fleet fleetToManage, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedIntegers2.Count == 0 )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SetFleetBehavior: RelatedIntegers2.Count == 0!", Verbosity.ShowAsError );
                return;
            }
            FleetBehavior newBehavior = (FleetBehavior)command.RelatedIntegers2.First;
            fleetToManage.Behavior = newBehavior;
            if ( newBehavior == FleetBehavior.WardenMode || newBehavior == FleetBehavior.HunterMode )
            {
                foreach ( GameEntity_Squad entity in fleetToManage.Entities )
                {
                    if ( entity == null || entity.Orders == null )
                        continue;
                    if ( !entity.TypeData.IsMobileCombatant )
                        continue;
                    entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                }
            }
        }
        #endregion
    }
}
