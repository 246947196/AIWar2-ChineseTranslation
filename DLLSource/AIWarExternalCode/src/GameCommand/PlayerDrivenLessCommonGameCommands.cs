using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    #region GameCommand_PlanetPing
    public class GameCommand_PlanetPing : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedIntegers.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "PlanetPing: RelatedIntegers count != 1!", Verbosity.ShowAsError );
                return;
            }
            Int16 planetIndex = (Int16)command.RelatedIntegers.First;
            Planet plan = World_AIW2.Instance.CurrentGalaxy.GetPlanetByIndex( planetIndex );
            if ( plan == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "PlanetPing: Planet with index " + planetIndex + " could not be found!", Verbosity.ShowAsError );
                return;
            }
            bool isForGalaxyMapOnly = command.RelatedBool;

            PlanetPingColor pingColor = (PlanetPingColor)command.RelatedMagnitude;

            if ( isForGalaxyMapOnly )
            {
                PlanetPing.CreateForGalaxyMap( plan, pingColor );
                plan.GameSecondLastPinged = World_AIW2.Instance.GameSecond;
            }
            else
            {
                if ( command.RelatedPoints.Count != 1 )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "PlanetPing: RelatedPoints count != 1!", Verbosity.ShowAsError );
                    return;
                }

                ArcenPoint pt = command.RelatedPoints.First;

                PlanetPing.CreateForPlanetView( plan, pt, pingColor );
            }
        }
    }
    #endregion

    #region GameCommand_EditPlanet
    public class GameCommand_EditPlanet : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedIntegers.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "EditPlanet: RelatedIntegers count != 1!", Verbosity.ShowAsError );
                return;
            }
            Int16 planetIndex = (Int16)command.RelatedIntegers.First;
            Planet plan = World_AIW2.Instance.CurrentGalaxy.GetPlanetByIndex( planetIndex );
            if ( plan == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "EditPlanet: Planet with index " + planetIndex + " could not be found!", Verbosity.ShowAsError );
                return;
            }

            if ( command.RelatedString == null || command.RelatedString.Length <= 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "EditPlanet: empty string passed for planet name!", Verbosity.ShowAsError );
                return;
            }
            plan.PlanetImportanceUIOnly = (PlanetImportance)command.RelatedMagnitude;
            plan.Name = command.RelatedString;
            plan.PlayerNotes = command.RelatedString2;
        }
    }
    #endregion

    #region GameCommand_EditShipModule
    public class GameCommand_EditShipModule : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            int fleetID = command.RelatedMagnitude;
            bool turnOn = command.RelatedBool;
            string entityTypeName = command.RelatedString;
            if ( command.RelatedIntegers.Count != 2 && command.RelatedIntegers2.Count != 2 )
            {
                //RelatedIntegers is used for regular module commands, RelatedIntegers2 is the Ignore Notifications mechanism
                ArcenDebugging.ArcenDebugLogSingleLine( "GameCommand_EditShipModule: RelatedIntegers count != 2 and RelatedIntegers2 != 2!", Verbosity.ShowAsError );
                return;
            }


            Fleet fleet = World_AIW2.Instance.GetFleetByID( fleetID );
            if ( fleet == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "GameCommand_EditShipModule: Could not find fleet with ID: " + fleetID, Verbosity.ShowAsError );
                return;
            }
            GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRowByName( entityTypeName );
            if ( typeData == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "GameCommand_EditShipModule: Could not find entity type data with name: " + entityTypeName, Verbosity.ShowAsError );
                return;
            }

            if (command.RelatedIntegers.Count == 2)
            {
                //enable/disable modules
                int uniqueTypeDataDifferentiatorForDuplicates = 0;
                int uniqueModuleIndex = 0;
                int _ri_i = 0;
                foreach ( var _ri_v in command.RelatedIntegers )
                {
                    if ( _ri_i == 0 ) uniqueTypeDataDifferentiatorForDuplicates = _ri_v;
                    else if ( _ri_i == 1 ) { uniqueModuleIndex = _ri_v; break; }
                    _ri_i++;
                }
                EntitySystemTypeData systemData = typeData.GetSystemTypeByModuleUniqueID( uniqueModuleIndex );
                if ( systemData == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "GameCommand_EditShipModule: Could not find system on entity data with uniqueModuleIndex: " + uniqueModuleIndex, Verbosity.ShowAsError );
                    return;
                }

                FleetMembership fMem = fleet.GetOrAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates(typeData, uniqueTypeDataDifferentiatorForDuplicates);
                fMem.SetModuleOnOrOff(systemData, turnOn);
            }
            else if ( command.RelatedIntegers2.Count == 2 )
            {
                //suppress the notification
                int uniqueTypeDataDifferentiatorForDuplicates = 0;
                byte modularBreakpoint = 0;
                int _ri2_i = 0;
                foreach ( var _ri2_v in command.RelatedIntegers2 )
                {
                    if ( _ri2_i == 0 ) uniqueTypeDataDifferentiatorForDuplicates = _ri2_v;
                    else if ( _ri2_i == 1 ) { modularBreakpoint = (byte)_ri2_v; break; }
                    _ri2_i++;
                }
                FleetMembership fMem = fleet.GetOrAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates(typeData, uniqueTypeDataDifferentiatorForDuplicates);
                fMem.SetModuleNotificationBreakpoint(modularBreakpoint);
            }
            //if ( fMem == null || fMem.EffectiveSquadCap <= 0 )
            //{
            //    ArcenDebugging.ArcenDebugLogSingleLine( "GameCommand_EditShipModule: Could not find entity type data with name: " + entityTypeName, Verbosity.ShowAsError );
            //    return;
            //}
        }
    }
    #endregion

    #region GameCommand_ChangeBolsterTarget
    public class GameCommand_ChangeBolsterTarget : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            int bolsteringFleetID = command.RelatedMagnitude;
            if ( command.RelatedIntegers.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "GameCommand_ChangeBolsterTarget: RelatedIntegers count != 1!", Verbosity.ShowAsError );
                return;
            }
            int bolsteredFleetID = command.RelatedIntegers.First;

            Fleet bolsteringFleet = World_AIW2.Instance.GetFleetByID( bolsteringFleetID );
            if ( bolsteringFleet == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "GameCommand_ChangeBolsterTarget: Could not find bolsteringFleet with ID: " + bolsteringFleetID, Verbosity.ShowAsError );
                return;
            }

            Fleet bolsteredFleet = World_AIW2.Instance.GetFleetByID( bolsteredFleetID );
            if ( bolsteredFleet == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "GameCommand_ChangeBolsterTarget: Could not find bolsteredFleetID with ID: " + bolsteredFleetID, Verbosity.ShowAsError );
                return;
            }

            bolsteringFleet.CityBolstersFleetID = bolsteredFleetID;
        }
    }
    #endregion

    #region GameCommand_BelatedlyCreateFaction
    public class GameCommand_BelatedlyCreateFaction : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            string factionName = command.RelatedString;
            if ( factionName == null || factionName == String.Empty )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "GameCommand_BelatedlyCreateFaction: RelatedString is blank!=!", Verbosity.ShowAsError );
                return;
            }
            
            Faction newFac = World_AIW2.Instance.BelatedlyCreateFaction_ShouldBeCalledFromGameCommand( factionName, null );
            if ( newFac != null )
            {
                //any other data to set?
            }
        }
    }
    #endregion

    #region GameCommand_BelatedlyCreateFactionsFromBeacon
    public class GameCommand_BelatedlyCreateFactionsFromBeacon : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            string beaconLookupName = command.RelatedString;
            if ( beaconLookupName == null || beaconLookupName == String.Empty )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "GameCommand_BelatedlyCreateFactionsFromBeacon: RelatedString is blank!=!", Verbosity.ShowAsError );
                return;
            }

            if ( !BeaconFactionOptionTable.Instance.FullListOfBeaconOptionsByName.ContainsKey( beaconLookupName ) )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Warning! The beacon lookup name '" + beaconLookupName +
                    "' was missing!  We don't know how to bring in the faction you specified", Verbosity.ShowAsError );
                return;
            }

            BeaconFactionOptionHackChoice choice = BeaconFactionOptionTable.Instance.FullListOfBeaconOptionsByName[beaconLookupName];

            int factionIndex = 0;
            foreach ( SpecialFactionData factionData in choice.FactionsIncluded )
            {
                void Setup(Faction f)
                {
                    //you bet we have more info to set!
                    foreach ( var fieldData in choice.CustomFieldValues )
                    {
                        if ( fieldData.FirstItem != factionIndex )
                            continue; //must be a match for our specific faction index

                        //it was a match so set our custom field data to whatever it said!
                        f.Config.SetCustomFieldValue( fieldData.SecondItem, fieldData.ThirdItem );
                        //ArcenDebugging.LogSingleLine(string.Format("setting custom field: {0}.Config.{1}={2}", f.FactionNameOrEmpty, fieldData.SecondItem, fieldData.ThirdItem), Verbosity.DoNotShow);
                    }
                };

                Faction newFac = World_AIW2.Instance.BelatedlyCreateFaction_ShouldBeCalledFromGameCommand( factionData.InternalName, Setup );
                if ( newFac != null )
                {
                    
                    //Beacon factions should spawn in with a random colour,
                    //ideally one not to close to any other faction's colour
                    int retries = 20;
                    do
                    {
                        newFac.Config.FactionCenterColor = TeamColorDefinitionTable.Instance.GetRandomRow();
                        newFac.Config.FactionTrimColor = TeamColorDefinitionTable.Instance.GetRandomRow();
                        for (int i = 0; i < World_AIW2.Instance.Setup.FactionConfigurations.Count; i++)
                        {
                            ConfigurationForFaction iterConfig = World_AIW2.Instance.Setup.FactionConfigurations[i];
                            if ( iterConfig == newFac.Config )
                                continue;
                            if (TeamColorDefinitionTable.Instance.AreColorsSimilar(iterConfig.FactionCenterColor, newFac.Config.FactionCenterColor))
                            {
                                retries = 0; //exit the outer loop too
                                break;
                            }
                        }
                    } while ( retries-- > 0);

                    //this must be done after the custom settings are set, above
                    if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                        newFac.DeepInfo.SeedStartingEntities_LaterEverythingElse( World_AIW2.Instance.CurrentGalaxy, context.GetHostOnlyContext(), World_AIW2.Instance.Setup.MapConfig.MapType );
                }
                factionIndex++;
            }
        }
    }
    #endregion

    public class GameCommand_DestroyDistantFleetMembers : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            List<SafeSquadWrapper> targetEntities = GameEntity_Squad.GetTemporarySquadList( "GameCommand_DestroyDistantFleetMembers-targetEntities", 10f );
            if ( targetEntities == null ) //blocked for teardown/shutdown; bail
                return;

            Helper_GetListOfEntitiesWithSomeAsNull( command, targetEntities );
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
                    List<SafeSquadWrapper> shipsToRemove = GameEntity_Squad.GetTemporarySquadList( "GameCommand_DestroyDistantFleetMembers-shipsToRemove", 10f );
                    if ( shipsToRemove == null ) //blocked for teardown/shutdown; bail
                    {
                        GameEntity_Squad.ReleaseTemporarySquadList( targetEntities );
                        return;
                    }
                    Planet centerpiecePlanet = entity.Planet;
                    foreach ( GameEntity_Squad squad in fleet.Entities )
                    {
                        if ( squad.Planet != centerpiecePlanet )
                            shipsToRemove.Add( squad );
                    }
                    for ( int j = 0; j < shipsToRemove.Count; j++ )
                        shipsToRemove[j].Despawn( context, false, InstancedRendererDeactivationReason.PlayerIsScrappingMe );
                    GameEntity_Squad.ReleaseTemporarySquadList( shipsToRemove );
                }
            }

            GameEntity_Squad.ReleaseTemporarySquadList( targetEntities );
        }
    }

    public class GameCommand_RetrieveSpentScience : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedIntegers.Count < 1 )
            {
                ArcenDebugging.ArcenDebugLog( "command.RelatedIntegers is empty in GameCommand_RetrieveSpentScience!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            int factionIndex = command.RelatedIntegers.First;
            Faction faction = World_AIW2.Instance.GetFactionByIndex( factionIndex );
            if ( faction == null )
            {
                ArcenDebugging.ArcenDebugLog( "Could not find faction with factionIndex " + factionIndex + " in GameCommand_RetrieveSpentScience!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }

            HackingType hackToDo = HackingTypeTable.Instance.GetRowByName( "RetrieveSpentScience" );
            FInt hackCost = hackToDo.GetHackPointCostForTarget( null );
            if ( hackCost > faction.StoredHacking )
            {
                if ( faction.GetIsLocalFaction() )
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "黑客点数不足！",
                        "派系 " + faction.GetDisplayName() + " 没有足够的黑客点数来完成 " + hackToDo.DisplayName, "确定" );
                return;
            }

            if ( command.RelatedIntegers2.Count > 0 )
            {
                //refunding for a fleet
                int fleetID = command.RelatedIntegers2.First;
                Fleet fleet = World_AIW2.Instance.GetFleetByID( fleetID );
                if ( fleet == null )
                {
                    ArcenDebugging.ArcenDebugLog( "Could not find faction with fleet " + fleetID + " in GameCommand_RetrieveSpentScience!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                    return;
                }
                if ( fleet.Faction != faction )
                {
                    ArcenDebugging.ArcenDebugLog( "Fleet " + fleetID + " does not belong to the correct faction in GameCommand_RetrieveSpentScience!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                    return;
                }
                int scienceBack = faction.RefundFleetIfPossible( fleet, false );
                faction.StoredHacking -= hackCost;

                HackingEvent hackEvent = HackingEvent.Create( faction.FactionIndex, faction.FactionIndex, -1,
                    hackToDo, null, false, "Fleet Upgrade Refund: " + fleet.GetName(), scienceBack );
                hackEvent.HackingPointsSpent = hackCost;
                faction.HackingHistory.Add( hackEvent );
            }
            else
            {
                //refunding for a tech
                if ( command.RelatedString.Length == 0 )
                {
                    ArcenDebugging.ArcenDebugLog( "command.RelatedString is empty in GameCommand_RetrieveSpentScience!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                    return;
                }
                TechUpgrade upgrade = TechUpgradeTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString );
                if ( upgrade == null )
                {
                    ArcenDebugging.ArcenDebugLog( "Could not find tech with name '" + command.RelatedString + "' in GameCommand_RetrieveSpentScience!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                    return;
                }

                int scienceBack = faction.RefundTechIfPossible( upgrade, false );
                faction.StoredHacking -= hackCost;

                HackingEvent hackEvent = HackingEvent.Create( faction.FactionIndex, faction.FactionIndex, -1,
                    hackToDo, null, false, "Tech Upgrade Refund: " + upgrade.DisplayName, scienceBack );
                hackEvent.HackingPointsSpent = hackCost;
                faction.HackingHistory.Add( hackEvent );
            }

            // check for achievements
            if ( faction.Config.IsFactionControlledByLocalPlayer )
                hackToDo.TriggerAnyAchievementsRelatedToThisHack();
        }
    }

    public class GameCommand_UnlockTech : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            TechUpgrade tech = TechUpgradeTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString );
            if ( tech == null )
                return;
            int cost;
            ArcenRejectionReason rejectionReason = command.GetRelatedFaction().GetCanUnlockTech( tech, false, out cost );
            if ( rejectionReason != ArcenRejectionReason.Unknown )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "Could not unlock tech " + tech.DisplayName + " because " + rejectionReason, ChatType.ShowLocallyOnly, null );
                return;
            }

            //it charges you the science and whatnot in here
            command.GetRelatedFaction().UnlockTech( tech, false );
        }
    }

    #region GameCommand_SetPlanetViewedByPlayerAccount
    public class GameCommand_SetPlanetViewedByPlayerAccount : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedIntegers.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "SetPlanetViewedByPlayerAccount: RelatedIntegers count != 1!", Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedIntegers2.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "SetPlanetViewedByPlayerAccount: RelatedIntegers2 count != 1!", Verbosity.ShowAsError );
                return;
            }

            byte playerPrimaryKeyID = (byte)command.RelatedIntegers.First;
            Int16 ViewingPlanetIndex = (Int16)command.RelatedIntegers2.First;

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                    if ( planet.Index == ViewingPlanetIndex )
                        planet.ViewedByPlayerAccounts_DuringGame.AddIfNotAlreadyIn( playerPrimaryKeyID );
                    else
                        planet.ViewedByPlayerAccounts_DuringGame.Remove( playerPrimaryKeyID );
            }
        }
    }
    #endregion

    #region GameCommand_SetUnit_UIImportance
    public class GameCommand_SetUnit_UIImportance : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedIntegers.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "SetUnit_UIImportance: RelatedIntegers count != 1!", Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedIntegers2.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "SetUnit_UIImportance: RelatedIntegers2 count != 1!", Verbosity.ShowAsError );
                return;
            }

            //command.RelatedIntegers.Add( Squad.PrimaryKeyID );
            //command.RelatedIntegers2.Add( (int)Importance );

            int unitPKID = command.RelatedIntegers.First;
            ObjectiveImportance importance = (ObjectiveImportance)command.RelatedIntegers2.First;

            GameEntity_Squad squad = World_AIW2.Instance.GetEntityByID_Squad( unitPKID );
            if ( squad != null )
            {
                squad.ImportanceUIOnly = importance;
                squad.FlagForForcedFullSyncToClients_FromHost(); //just extra paranoia, really
            }
        }
    }
    #endregion

    #region GameCommand_SetPlanet_UIImportance
    public class GameCommand_SetPlanet_UIImportance : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedIntegers.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "SetPlanet_UIImportance: RelatedIntegers count != 1!", Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedIntegers2.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "SetPlanet_UIImportance: RelatedIntegers2 count != 1!", Verbosity.ShowAsError );
                return;
            }

            //command.RelatedIntegers.Add( Plan.Index );
            //command.RelatedIntegers2.Add( (int)Importance );

            Int16 planetIndex = (Int16)command.RelatedIntegers.First;
            ObjectiveImportance importance = (ObjectiveImportance)command.RelatedIntegers2.First;

            Planet plan = World_AIW2.Instance.CurrentGalaxy.GetPlanetByIndex( planetIndex );
            if ( plan != null )
            {
                plan.ImportanceUIOnly = importance; //if there were a problem betwen the client and host, this gets synced anyway back to the client soon
            }
        }
    }
    #endregion

    public class GameCommand_ChangeFrameSize : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            //Phase 2: a player tap steps the player's INTENDED speed (PlayerTargetSpeedMultiplier); the auto-budget steps the
            //actual (possibly auto-inflated) FrameSizeMultiplier. Seeding a player tap from an auto-inflated frame size would
            //jump the intended speed by a wrong, off-grid amount, so seed from whichever value this command actually adjusts.
            bool isPlayerSourced = command.FromActualInputEventOfPlayerID != 255;
            FInt newSize = isPlayerSourced ? World_AIW2.Instance.PlayerTargetSpeedMultiplier : World_AIW2.Instance.FrameSizeMultiplier;

            while (command.RelatedMagnitude != 0)
            {
                if ( command.RelatedMagnitude < 0 )
                {
                    if ( newSize <= FInt.One )
                        newSize -= FInt.FromParts( 0, 100 );
                    else
                    {
                        newSize -= FInt.FromParts( 0, 500 );
                        if ( newSize < FInt.One )
                            newSize = FInt.One;
                    }

                    command.RelatedMagnitude++;
                }
                else
                {
                    if ( newSize < FInt.One )
                    {
                        newSize += FInt.FromParts( 0, 100 );
                        if ( newSize > FInt.One )
                            newSize = FInt.One;
                    }
                    else
                        newSize += FInt.FromParts( 0, 500 );

                    command.RelatedMagnitude--;
                }
            }

            int max = World_AIW2.MAX_FRAME_SIZE_MULTIPLIER;
            if (GameSettings.Current.GetBoolBySetting( "TurboMode" ))
                max = 100;

            //command.RelatedMagnitude
            if ( newSize < FInt.FromParts( 0, 500 ) ) newSize = FInt.FromParts( 0, 500 );
            if ( newSize > max ) newSize = (FInt)max;
            World_AIW2.Instance.FrameSizeMultiplier = newSize;

            //A PLAYER-sourced change also sets the intended-speed invariant (and resets the frame size to it, discarding any
            //auto-inflation, so the auto-budget re-derives from the new intent). An auto-budget command (FromActualInputEvent-
            //OfPlayerID == 255) only adjusts the frame size and must leave the intended speed alone.
            if ( isPlayerSourced )
                World_AIW2.Instance.PlayerTargetSpeedMultiplier = newSize;
            //The step size just changed, which changes per-step compute time, so reset the performance-sample window. Runs on
            //the bound frame on every peer; the buffer is non-sim / per-peer, so this is sync-safe.
            World_AIW2.Instance.ResetSimTimeSampleWindow_NonSim();
        }
    }

    public class GameCommand_ChangeGameSpeed : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            World_AIW2.Instance.GameSpeedDifferentialFromDefault = (Int16)command.RelatedMagnitude;
            World_AIW2.Instance.SetGameSpeedDifferentialValues();
        }
    }

    public class GameCommand_ChangePlanetFactionBooleanFlag : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedIntegers == null || command.RelatedIntegers.Count < 1 )
                return;
            Planet planet = World_AIW2.Instance.GetPlanetByIndex( (Int16)command.RelatedIntegers.First );
            PlanetFaction faction = planet.GetPlanetFactionForFaction( command.GetRelatedFaction() );
            faction.SetPlanetFactionBooleanFlag( (PlanetFactionBooleanFlag)command.RelatedIntegers2.First, command.RelatedBool );
        }
    }
}
