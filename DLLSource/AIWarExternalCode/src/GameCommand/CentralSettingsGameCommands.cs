using Arcen.AIW2.Core;
using System;

using System.Linq;
using System.Text;
using Arcen.Universal;
using UnityEngine;

namespace Arcen.AIW2.External
{
    #region GameCommand_SetupOnly_RequestSetupChanges
    /// <summary>
    /// If we're just changing part of the setup, then let's do that here.  To some extent this is more of a pain,
    /// but it prevents people editing the map at the same time, or editing the map during mapgen, from causing
    /// unrelated changes to eat one another.
    /// 
    /// This is also generally just for updating the for-lobby-only data, 
    /// not the actual data that is generating the map's long-term setup data.
    /// </summary>
    public class GameCommand_SetupOnly_RequestSetupChanges : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            this.HandleCommand( command, context, false );
        }

        #region WriteDiscardedBecauseLobbyOnly
        private void WriteDiscardedBecauseLobbyOnly( GameCommand command, string ExtraLocation )
        {
            ArcenDebugging.ArcenDebugLogSingleLine( ExtraLocation + ": Discarded Post-Lobby Setup Change Not Of A Valid Type To Do After Game Start: '" + command.RelatedString + "':\n" +
                command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
        }
        #endregion

        #region WriteDiscardedBecauseDuringGameOnly
        private void WriteDiscardedBecauseDuringGameOnly( GameCommand command, string ExtraLocation )
        {
            ArcenDebugging.ArcenDebugLogSingleLine( ExtraLocation + ": Discarded Setup Change Not Of A Valid Type To Do Before Game Start: '" + command.RelatedString + "':\n" +
                command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
        }
        #endregion

        #region WriteDiscardedBecauseDuringGameplayFaction
        private void WriteDiscardedBecauseDuringGameplayFaction( GameCommand command, string ExtraLocation )
        {
            ArcenDebugging.ArcenDebugLogSingleLine( ExtraLocation + ": Discarded Post-Lobby Setup Change Not Valid For a Faction: '" + command.RelatedString + "':\n" +
                command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
        }
        #endregion

        protected void HandleCommand( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame )
        {
            if ( World_AIW2.Instance == null )
                return;

            #region Check If The Basics Are Allowed
            if ( IsForDuringGame )
            {
                if ( World_AIW2.Instance.InSetupPhase )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Discarded (Because Game Not Yet Started) Post-Lobby Setup Change: '" + command.RelatedString + "':\n" +
                        command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                    return;
                }
            }
            else
            {
                if ( !World_AIW2.Instance.InSetupPhase )
                {
                    if ( GameSettings.Current.GetBoolBySetting( "Debug_WriteLobbySetupChangesToLog" ) )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Discarded (Because Fully Started) Lobby Setup Changes Requested: '" + command.RelatedString + "':\n" +
                            command.WriteToStringInefficient( true, true, true ), Verbosity.DoNotShow );
                    return;
                }

                if ( World_AIW2.Instance.Setup.ShouldSeedDetailsYet )
                {
                    if ( GameSettings.Current.GetBoolBySetting( "Debug_WriteLobbySetupChangesToLog" ) )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Discarded (Because Starting) Lobby Setup Changes Requested: '" + command.RelatedString + "':\n" +
                            command.WriteToStringInefficient( true, true, true ), Verbosity.DoNotShow );
                    return;
                }
            }
            #endregion

            if ( command.RelatedString == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Null RelatedString passed to HandleCommand!", Verbosity.ShowAsError );
                return;
            }
            if ( GameSettings.Current.GetBoolBySetting( "Debug_WriteLobbySetupChangesToLog" ) )
                ArcenDebugging.ArcenDebugLogSingleLine( "Lobby Setup Changes Requested: '" + command.RelatedString + "':\n" +
                    command.WriteToStringInefficient( true, true, true ), Verbosity.DoNotShow );

            //ArcenDebugging.ArcenDebugLog( "LOBBY_WTH: GameCommand_SetupOnly_RequestSetupChanges: " + command.RelatedString + " cmds: " +
            //    World_AIW2.Instance.OnClient_GameCommandsThatHaveBeenReceivedFromServerButNotYetExecuted.Count + " cmdsbk: " +
            //    World_AIW2.Instance.OnClient_GameCommandsThatHaveBeenReceivedFromServerButNotYetExecuted_Backup.Count + " cmds_cli_notyettosrv: " +
            //    World_AIW2.Instance.OnClient_GameCommandsThatHaveNotYetBeenSentToServer.Count + " cmds_srv_notyettocli: " +
            //    World_AIW2.Instance.OnServer_GameCommandsThatHaveNotYetBeenSentToClients.Count, Verbosity.DoNotShow );

            float amountOfTimeToBlockGalaxyMap = 0.3f;

            switch ( command.RelatedString )
            {
                case "Prior":
                    this.Prior( command, context, IsForDuringGame );
                    break;
                case "StartingIndexChanges":
                    amountOfTimeToBlockGalaxyMap = 0;
                    this.StartingIndexChanges( command, context, IsForDuringGame, false );
                    break;
                case "StartingIndex_FromAutoAssignInMapgen":
                    amountOfTimeToBlockGalaxyMap = 0;
                    this.StartingIndexChanges( command, context, IsForDuringGame, true );
                    break;
                case "SetSeed":
                    this.SetSeed( command, context, IsForDuringGame );
                    break;
                case "Regenerate":
                    this.Regenerate( command, context, IsForDuringGame );
                    break;
                case "MapType":
                    this.MapType( command, context, IsForDuringGame );
                    break;
                case "PlanetNameType":
                    this.PlanetNameType( command, context, IsForDuringGame );
                    break;
                case "iNumPlanets":
                    this.iNumPlanets( command, context, IsForDuringGame );
                    break;
                case "MapIntForLobby": //this also handles the bool settings
                    this.MapIntForLobby( command, context, IsForDuringGame );
                    break;
                case "AIWar2GalaxySettingChanged_Int":
                    this.AIWar2GalaxySettingChanged_Int( command, context, IsForDuringGame );
                    break;
                case "AIWar2GalaxySettingChanged_String":
                    this.AIWar2GalaxySettingChanged_String( command, context, IsForDuringGame );
                    break;
                case "DataForFaction_CustomFieldDefinitionChanged":
                    this.DataForFaction_CustomFieldDefinitionChanged( command, context, IsForDuringGame, false );
                    break;
                case "DataForFaction_CustomFieldDefinitionChangedValidForDuringGame":
                    this.DataForFaction_CustomFieldDefinitionChanged( command, context, IsForDuringGame, true );
                    break;
                case "FactionNameChanged":
                    this.FactionNameChanged( command, context, IsForDuringGame );
                    break;
                case "FactionOwnershipChanged":
                    this.FactionOwnershipChanged( command, context, IsForDuringGame );
                    break;
                case "dFactionType_Add":
                    this.dFactionType_Add( command, context, IsForDuringGame );
                    break;
                case "dFactionType_Remove":
                    this.dFactionType_Remove( command, context, IsForDuringGame );
                    break;
                case "CustomFactionFieldDropdown":
                    this.CustomFactionFieldDropdown( command, context, IsForDuringGame );
                    break;
                case "bTeamColor":
                    this.bTeamColor( command, context, IsForDuringGame );
                    break;
                case "SimplyRegenerate":
                    this.SimplyRegenerate( command, context, IsForDuringGame );
                    break;
                case "FactionGift":
                    this.FactionGift( command, context, IsForDuringGame );
                    break;
                case "CampaignType":
                    this.CampaignType( command, context, IsForDuringGame );
                    break;
                default:
                    ArcenDebugging.ArcenDebugLogSingleLine( "Unknown RelatedString '" + command.RelatedString + "' passed to HandleCommand!", Verbosity.ShowAsError );
                    break;
            }

            if ( amountOfTimeToBlockGalaxyMap > 0 )
                GalaxyMapManager.BlockedVisualUpdatesUntil = ArcenTime.TimeSinceStartF + amountOfTimeToBlockGalaxyMap;
        }
        
        private bool EnableLogging = false;
        
        #region Prior
        private void Prior( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame )
        {
            if ( IsForDuringGame )
            {
                WriteDiscardedBecauseLobbyOnly( command, "LATECHECK" );
                return;
            }
            
            var setup = World_AIW2.Instance.Setup;
            if ( setup.FactionConfigurations.Count == 0 )
                return;
            
            if (EnableLogging) LOG.Msg("{0}() called.", this.MethodName());
            
            var map = setup.MapConfig;
            map.PopUndoState();
        }
        #endregion

        #region StartingIndexChanges
        private void StartingIndexChanges( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame, bool FromAutoAssignInMapgen )
        {
            if ( IsForDuringGame )
            {
                WriteDiscardedBecauseLobbyOnly( command, "LATECHECK" );
                return;
            }
            
            var setup = World_AIW2.Instance.Setup;
            if ( setup.FactionConfigurations.Count == 0 )
                return;

            if (EnableLogging) LOG.Msg("{0}() called.", this.TypeNameAndMethod());
            
            if ( command.RelatedIntegers.Count != command.RelatedIntegers2.Count )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "StartingIndexChanges: RelatedIntegers and RelatedIntegers2 count mismatch!", Verbosity.ShowAsError );
                return;
            }
            
            int[] _ri = new int[command.RelatedIntegers.Count];
            command.RelatedIntegers.CopyTo( _ri, 0 );
            int[] _ri2 = new int[command.RelatedIntegers2.Count];
            command.RelatedIntegers2.CopyTo( _ri2, 0 );
            for ( int i = 0; i < _ri.Length; i++ )
            {
                var fid = (Int16)_ri[i];
                var pid = (Int16)_ri2[i];

                var fac = setup.GetConfigurationForFaction(fid);
                if (fac == null)
                {
                    LOG.Err("Error in {0}(). No faction with index {1} exists.", this.TypeNameAndMethod(), fid);
                    continue;
                }
                
                var config = setup.FactionConfigurations[fid];
                config.StartingIndex = pid;
            }
        }
        #endregion

        #region MapSeed
        private void SetSeed( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame )
        {
            if ( IsForDuringGame )
            {
                WriteDiscardedBecauseLobbyOnly( command, "LATECHECK" );
                return;
            }

            if ( command.RelatedIntegers.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "MapSeed: RelatedIntegers count != 1!", Verbosity.ShowAsError );
                return;
            }
            
            var setup = World_AIW2.Instance.Setup;
            var map = setup.MapConfig;
            int newSeed = command.RelatedIntegers.First;

            if (EnableLogging) LOG.Msg("{0}() called.\n  Seed is {1} (was {2}).", this.TypeNameAndMethod(), newSeed, map.Seed);
            
            if ( map.Seed != newSeed )
            {
                map.PushUndoState();
                setup.ClearPlayerStartingPlanets();
                map.Seed = newSeed;
            }

            setup.LobbyWorking_MarkAsChanged( StartWorldSource1.RegenerateLobbyFromPlayerInput, StartWorldSource2.NotLoadingAnything );
        }
        #endregion

        #region Regenerate
        private void Regenerate( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame )
        {
            if ( IsForDuringGame )
            {
                WriteDiscardedBecauseLobbyOnly( command, "LATECHECK" );
                return;
            }
            
            var setup = World_AIW2.Instance.Setup;
            var map = setup.MapConfig;
            
            if (EnableLogging) LOG.Msg("{0}() called.\n  Seed is {1}.", this.TypeNameAndMethod(), map.Seed);
            
            map.PushUndoState();
            setup.ClearPlayerStartingPlanets();
            setup.LobbyWorking_MarkAsChanged( StartWorldSource1.RegenerateLobbyFromPlayerInput, StartWorldSource2.NotLoadingAnything );
        }
        #endregion

        #region MapType
        private void MapType( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame )
        {
            if ( IsForDuringGame )
            {
                WriteDiscardedBecauseLobbyOnly( command, "LATECHECK" );
                return;
            }
            WorldSetup setupToChange = World_AIW2.Instance.Setup;

            if ( command.RelatedString2 == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "MapType: RelatedString2 null!", Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedString2 == "Random" )
            {
                if ( setupToChange.MapConfig.UseRandomMapType )
                    return;
                setupToChange.MapConfig.SetUseRandomMapType();
                setupToChange.MapConfig.ResetForNewMapType();
                setupToChange.LobbyWorking_MarkAsChanged( StartWorldSource1.RegenerateLobbyFromPlayerInput, StartWorldSource2.NotLoadingAnything );
                return;
            }
            MapTypeData mapTypeData = MapTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( mapTypeData == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "MapType: Could not find MapTypeData with internal name '" + command.RelatedString2 + "'!", Verbosity.ShowAsError );
                return;
            }
            if ( setupToChange.MapConfig.MapType == mapTypeData )
                return;

            setupToChange.MapConfig.MapType = mapTypeData;
            setupToChange.MapConfig.SetClampedNumberOfPlanetsForMapType( setupToChange.MapConfig.MapType, setupToChange.MapConfig.NumberOfPlanetsRaw );
            //reset to the defaults when changing a map type!
            setupToChange.MapConfig.ResetForNewMapType();

            setupToChange.LobbyWorking_MarkAsChanged( StartWorldSource1.RegenerateLobbyFromPlayerInput, StartWorldSource2.NotLoadingAnything );
        }
        #endregion

        #region PlanetNameType
        private void PlanetNameType( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame )
        {
            if ( IsForDuringGame )
            {
                WriteDiscardedBecauseLobbyOnly( command, "LATECHECK" );
                return;
            }
            WorldSetup setupToChange = World_AIW2.Instance.Setup;

            if ( command.RelatedString2 == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "PlanetNameType: RelatedString2 null!", Verbosity.ShowAsError );
                return;
            }
            PlanetNameTypeData planetNameTypeData = PlanetNameTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( planetNameTypeData == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "PlanetNameType: Could not find PlanetNameTypeData with internal name '" + command.RelatedString2 + "'!", Verbosity.ShowAsError );
                return;
            }
            if ( setupToChange.MapConfig.PlanetNameType == planetNameTypeData )
                return;

            setupToChange.MapConfig.PlanetNameType = planetNameTypeData;
            setupToChange.LobbyWorking_MarkAsChanged( StartWorldSource1.RegenerateLobbyFromPlayerInput, StartWorldSource2.NotLoadingAnything );
        }
        #endregion
        
        #region iNumPlanets
        private void iNumPlanets( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame )
        {
            if ( IsForDuringGame )
            {
                WriteDiscardedBecauseLobbyOnly( command, "LATECHECK" );
                return;
            }
            WorldSetup setupToChange = World_AIW2.Instance.Setup;

            if ( command.RelatedIntegers.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "iNumPlanets: RelatedIntegers count != 1!", Verbosity.ShowAsError );
                return;
            }
            Int16 numberOfPlanets = (Int16)command.RelatedIntegers.First;
            if ( setupToChange.MapConfig.NumberOfPlanetsRaw == numberOfPlanets )
                return;

            setupToChange.MapConfig.SetClampedNumberOfPlanetsForMapType( setupToChange.MapConfig.MapType, numberOfPlanets );
            setupToChange.LobbyWorking_MarkAsChanged( StartWorldSource1.RegenerateLobbyFromPlayerInput, StartWorldSource2.NotLoadingAnything );
        }
        #endregion

        #region MapIntForLobby
        private void MapIntForLobby( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame )
        {
            if ( IsForDuringGame )
            {
                WriteDiscardedBecauseLobbyOnly( command, "LATECHECK" );
                return;
            }
            WorldSetup setupToChange = World_AIW2.Instance.Setup;

            if ( command.RelatedIntegers.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "MapIntForLobby: RelatedIntegers count != 1!", Verbosity.ShowAsError );
                return;
            }

            int optionIndex = command.RelatedIntegers.First;

            if (command.RelatedIntegers2.Count > 0)
            {
                int val = command.RelatedIntegers2.First;

                if ( setupToChange.MapConfig.GetCustomInt( optionIndex ) != val )
                {
                    setupToChange.MapConfig.SetCustomInt(optionIndex, val );
                    setupToChange.LobbyWorking_MarkAsChanged( StartWorldSource1.RegenerateLobbyFromPlayerInput, StartWorldSource2.NotLoadingAnything );
                }
            }
            else
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "MapIntForLobby: RelatedIntegers2 must be set!", Verbosity.ShowAsError );
            }

            //ArcenDebugging.ArcenDebugLogSingleLine( "Setting: settingName: " + setupToChange.MapType.Options[mapCustomIntIndex].InternalName + 
            //    " (" + mapCustomIntIndex + ") is now " + mapCustomIntValue + 
            //    " (" + setupToChange.MapType.Options[mapCustomIntIndex].Choices[mapCustomIntValue].DisplayName + ")", Verbosity.DoNotShow );
        }
        #endregion

        #region AIWar2GalaxySettingChanged_Int
        private void AIWar2GalaxySettingChanged_Int( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame )
        {
            if ( IsForDuringGame )
            {
                WriteDiscardedBecauseLobbyOnly( command, "LATECHECK" );
                return;
            }
            WorldSetup setupToChange = World_AIW2.Instance.Setup;

            if ( command.RelatedString2 == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "AIWar2GalaxySettingChanged_Int: RelatedString2 null!", Verbosity.ShowAsError );
                return;
            }
            AIWar2GalaxySetting setting = AIWar2GalaxySettingTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( setting == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "AIWar2GalaxySettingChanged_Int: Could not find AIWar2GalaxySetting with internal name '" + command.RelatedString2 + "'!", Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedIntegers.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "AIWar2GalaxySettingChanged_Int: RelatedIntegers count != 1!", Verbosity.ShowAsError );
                return;
            }
            int newValue = command.RelatedIntegers.First;
            if ( setupToChange.GetIntBySetting( setting ) == newValue )
                return;
            setupToChange.SetIntBySetting( setting, newValue );
            AIWar2GalaxySettingQuickAccess.UpdateQuickAccessSetting( command.RelatedString2 );

            //note from Chris: do NOT mark the lobby as changed, since we don't care about the map regenerating from this
            //setupToChange.LobbyWorking_MarkAsChanged();
        }
        #endregion

        #region AIWar2GalaxySettingChanged_String
        private void AIWar2GalaxySettingChanged_String( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame )
        {
            if ( IsForDuringGame )
            {
                WriteDiscardedBecauseLobbyOnly( command, "LATECHECK" );
                return;
            }
            WorldSetup setupToChange = World_AIW2.Instance.Setup;

            if ( command.RelatedString2 == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "AIWar2GalaxySettingChanged_String: RelatedString2 null!", Verbosity.ShowAsError );
                return;
            }
            AIWar2GalaxySetting setting = AIWar2GalaxySettingTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( setting == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "AIWar2GalaxySettingChanged_String: Could not find AIWar2GalaxySetting with internal name '" + command.RelatedString2 + "'!", Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedString3 == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "AIWar2GalaxySettingChanged_String: RelatedString3 null!", Verbosity.ShowAsError );
                return;
            }
            if ( setupToChange.GetStringBySetting( setting ) == command.RelatedString3 )
                return;
            setupToChange.SetStringBySetting( setting, command.RelatedString3 );
            AIWar2GalaxySettingQuickAccess.UpdateQuickAccessSetting( command.RelatedString2 );

            //note from Chris: do NOT mark the lobby as changed, since we don't care about the map regenerating from this
            //setupToChange.LobbyWorking_MarkAsChanged();
        }
        #endregion

        #region DataForFaction_CustomFieldDefinitionChanged
        private void DataForFaction_CustomFieldDefinitionChanged( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame, bool IsValidForDuringGame )
        {
            WorldSetup setupToChange = World_AIW2.Instance.Setup;

            if ( command.RelatedIntegers.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "DataForFaction_CustomFieldDefinitionChanged: RelatedIntegers count != 1!", Verbosity.ShowAsError );
                return;
            }

            Int16 factionArrayIndex = (Int16)command.RelatedIntegers.First;
            if ( factionArrayIndex < 0 || factionArrayIndex >= setupToChange.FactionConfigurations.Count )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "DataForFaction_CustomFieldDefinitionChanged: factionArrayIndex was " + factionArrayIndex +
                    " when setupToChange.FactionConfigurations.Count was " + setupToChange.FactionConfigurations.Count + "!", Verbosity.ShowAsError );
                return;
            }

            ConfigurationForFaction factionConfigToDisplay = setupToChange.FactionConfigurations[factionArrayIndex];
            if ( factionConfigToDisplay == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "DataForFaction_CustomFieldDefinitionChanged: faction at factionArrayIndex " + factionArrayIndex +
                    " is null!", Verbosity.ShowAsError );
                return;
            }

            if ( command.RelatedString2 == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "DataForFaction_CustomFieldDefinitionChanged: RelatedString2 null!", Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedString3 == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "DataForFaction_CustomFieldDefinitionChanged: RelatedString3 null!", Verbosity.ShowAsError );
                return;
            }

            if ( IsForDuringGame )
            {
                if ( !IsValidForDuringGame )
                {
                    WriteDiscardedBecauseDuringGameplayFaction( command, "DURINGGAME" );
                    return;
                }
            }

            factionConfigToDisplay.SetCustomFieldValue( command.RelatedString2,  command.RelatedString3 );

            if ( IsForDuringGame )
            {
                //nothing to do here now, settings are now picked up automatically from factions
            }
            else
            {
                //note from Chris: do NOT mark the lobby as changed, since we don't care about the map regenerating from this
                //setupToChange.LobbyWorking_MarkAsChanged();
                if ( command.RelatedBool ) {
                    //ArcenDebugging.ArcenDebugLogSingleLine( "markaschanged", Verbosity.ShowAsError );
                    setupToChange.LobbyWorking_MarkAsChanged( StartWorldSource1.RegenerateLobbyFromPlayerInput, StartWorldSource2.NotLoadingAnything );
                }
            }
        }
        #endregion

        #region FactionNameChanged
        private void FactionNameChanged( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame )
        {
            WorldSetup setupToChange = World_AIW2.Instance.Setup;

            if ( command.RelatedIntegers.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "FactionNameChanged: RelatedIntegers count != 1!", Verbosity.ShowAsError );
                return;
            }

            Int16 factionArrayIndex = (Int16)command.RelatedIntegers.First;
            if ( factionArrayIndex < 0 || factionArrayIndex >= setupToChange.FactionConfigurations.Count )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "FactionNameChanged: factionArrayIndex was " + factionArrayIndex +
                    " when setupToChange.FactionConfigurations.Count was " + setupToChange.FactionConfigurations.Count + "!", Verbosity.ShowAsError );
                return;
            }

            ConfigurationForFaction factionConfigToDisplay = setupToChange.FactionConfigurations[factionArrayIndex];
            if ( factionConfigToDisplay == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "FactionNameChanged: faction at factionArrayIndex " + factionArrayIndex +
                    " is null!", Verbosity.ShowAsError );
                return;
            }

            string factionName = command.RelatedString3;
            if ( factionName == null )
                factionName = string.Empty;

            Faction factionOrNull = null;
            if ( IsForDuringGame )
            {
                factionOrNull = World_AIW2.Instance.Factions[factionArrayIndex];
            }

            //ArcenDebugging.ArcenDebugLogSingleLine( "New2: " + factionName, Verbosity.DoNotShow );

            factionConfigToDisplay.FactionNameOrEmpty = factionName;
            if ( factionOrNull != null )
                factionOrNull.FactionNameOrEmpty = factionName;
        }
        #endregion

        #region FactionOwnershipChanged
        private void FactionOwnershipChanged( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame )
        {
            if ( command.RelatedIntegers.Count != 2 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "FactionOwnershipChanged: RelatedIntegers count != 2!", Verbosity.ShowAsError );
                return;
            }

            Int16 factionArrayIndexToControl = 0;
            byte playerAccountToDoTheControlling = 0;
            int _ri_i = 0;
            foreach ( var _ri_v in command.RelatedIntegers )
            {
                if ( _ri_i == 0 ) factionArrayIndexToControl = (Int16)_ri_v;
                else if ( _ri_i == 1 ) { playerAccountToDoTheControlling = (byte)_ri_v; break; }
                _ri_i++;
            }
            bool addToFaction = command.RelatedBool;

            for ( int i = 0; i < World_AIW2.Instance.Setup.FactionConfigurations.Count; i++ )
            {
                ConfigurationForFaction fac = World_AIW2.Instance.Setup.FactionConfigurations[i];
                fac.RemovePlayerAccountFromControllingThisFaction( playerAccountToDoTheControlling );

                if ( factionArrayIndexToControl == i && addToFaction )
                {
                    fac.SetPlayerAccountInControlOfThisFaction( playerAccountToDoTheControlling );
                }
            }
        }
        #endregion

        #region dFactionType_Add
        private void dFactionType_Add( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame )
        {
            if ( IsForDuringGame )
            {
                WriteDiscardedBecauseLobbyOnly( command, "LATECHECK" );
                return;
            }
            WorldSetup setupToChange = World_AIW2.Instance.Setup;

            if ( command.RelatedString2 == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "dFactionType_Add: RelatedString2 null!", Verbosity.ShowAsError );
                return;
            }
            SpecialFactionData factionData = SpecialFactionDataTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( factionData == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "dFactionType_Add: Could not find SpecialFactionData with internal name '" + command.RelatedString2 + "'!", Verbosity.ShowAsError );
                return;
            }

            setupToChange.AddFaction(factionData, true );
        }

        #endregion

        #region dFactionType_Remove
        private void dFactionType_Remove( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame )
        {
            if ( IsForDuringGame )
            {
                WriteDiscardedBecauseLobbyOnly( command, "LATECHECK" );
                return;
            }
            WorldSetup setupToChange = World_AIW2.Instance.Setup;
            
            if ( command.RelatedIntegers.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "dFactionType_Remove: RelatedIntegers count != 1!", Verbosity.ShowAsError );
                return;
            }
            Int16 factionConfigIndex = (Int16)command.RelatedIntegers.First;
            if ( factionConfigIndex < 0 || factionConfigIndex >= setupToChange.FactionConfigurations.Count )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "dFactionType_Remove: Trying to remove factionConfigIndex " + factionConfigIndex + 
                    " when the count of factions was " + setupToChange.FactionConfigurations.Count, Verbosity.ShowAsError );
                return;
            }

            ConfigurationForFaction factionDataBeingRemoved = setupToChange.FactionConfigurations[factionConfigIndex];
            setupToChange.FactionConfigurations.RemoveAt( factionConfigIndex );

            //if removing an AI, then remove the next special forces and hunter fleet and praetorean guard as well
            if ( factionDataBeingRemoved.SpecialFactionData.Type == FactionType.AI )
            {
                for ( int i = factionConfigIndex; i < setupToChange.FactionConfigurations.Count; i++ )
                {
                    if ( setupToChange.FactionConfigurations[i].SpecialFactionData.InternalName == "AIWarden" )
                    {
                        setupToChange.FactionConfigurations.RemoveAt( i );
                        break;
                    }
                }
                for ( int i = factionConfigIndex; i < setupToChange.FactionConfigurations.Count; i++ )
                {
                    if ( setupToChange.FactionConfigurations[i].SpecialFactionData.InternalName == "HunterFleet" )
                    {
                        setupToChange.FactionConfigurations.RemoveAt( i );
                        break;
                    }
                }
                for ( int i = factionConfigIndex; i < setupToChange.FactionConfigurations.Count; i++ )
                {
                    if ( setupToChange.FactionConfigurations[i].SpecialFactionData.InternalName == "PraetorianGuard" )
                    {
                        setupToChange.FactionConfigurations.RemoveAt( i );
                        break;
                    }
                }
            }

            //adjust all the indices for those that come after
            for ( int i = factionConfigIndex; i < setupToChange.FactionConfigurations.Count; i++ )
                setupToChange.FactionConfigurations[i].LobbyOnly_SortIndex = i;

            if ( factionDataBeingRemoved.SpecialFactionData.Type == FactionType.Player )
            {
                //mark as changed since there will now be one less planet to see
                setupToChange.LobbyWorking_MarkAsChanged( StartWorldSource1.RegenerateLobbyFromPlayerInput, StartWorldSource2.NotLoadingAnything );
            }
            else
            {
                //mark as changed because... just paranoia, really.  I don't like it when the config and the underlying factions are out of sync
                setupToChange.LobbyWorking_MarkAsChanged( StartWorldSource1.RegenerateLobbyFromPlayerInput, StartWorldSource2.NotLoadingAnything );
            }
        }
        #endregion

        #region CustomFactionFieldDropdown
        private void CustomFactionFieldDropdown( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame )
        {
            WorldSetup setupToChange = World_AIW2.Instance.Setup;

            if ( command.RelatedString2 == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "CustomFactionFieldDropdown: RelatedString2 null!", Verbosity.ShowAsError );
                return;
            }
            string factionFieldName = command.RelatedString2;
            if ( factionFieldName.Length <= 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "CustomFactionFieldDropdown: Passed a blank factionFieldName '" + factionFieldName + "'!", Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedString3 == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "CustomFactionFieldDropdown: RelatedString3 null!", Verbosity.ShowAsError );
                return;
            }
            string factionFieldValue = command.RelatedString3;
            if ( factionFieldValue.Length <= 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "CustomFactionFieldDropdown: Passed a blank factionFieldValue '" + factionFieldValue + "'!", Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedIntegers.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "CustomFactionFieldDropdown: RelatedIntegers count != 1!", Verbosity.ShowAsError );
                return;
            }

            Int16 factionConfigIndex = (Int16)command.RelatedIntegers.First;

            if ( IsForDuringGame )
            {
                WriteDiscardedBecauseDuringGameplayFaction( command, "DURINGGAME" );
                return;
            }

            setupToChange.FactionConfigurations[factionConfigIndex].SetCustomFieldValue( factionFieldName, factionFieldValue );

            if ( IsForDuringGame )
            {
                //nothing to do here now, settings are now picked up automatically from factions
            }
            else
            {
                //note from Chris: do NOT mark the lobby as changed, since we don't care about the map regenerating from this
                //setupToChange.LobbyWorking_MarkAsChanged();
            }
        }
        #endregion

        #region bTeamColor
        private void bTeamColor( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame )
        {
            WorldSetup setupToChange = World_AIW2.Instance.Setup;

            if ( command.RelatedIntegers.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "bTeamColor: RelatedIntegers count != 1!", Verbosity.ShowAsError );
                return;
            }

            Int16 factionArrayIndex = (Int16)command.RelatedIntegers.First;
            if ( factionArrayIndex < 0 || factionArrayIndex >= setupToChange.FactionConfigurations.Count )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "bTeamColor: factionArrayIndex was " + factionArrayIndex +
                    " when setupToChange.FactionConfigurations.Count was " + setupToChange.FactionConfigurations.Count + "!", Verbosity.ShowAsError );
                return;
            }

            ConfigurationForFaction factionConfigToDisplay = setupToChange.FactionConfigurations[factionArrayIndex];
            if ( factionConfigToDisplay == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "bTeamColor: faction at factionArrayIndex " + factionArrayIndex +
                    " is null!", Verbosity.ShowAsError );
                return;
            }

            if ( command.RelatedString2 == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "bTeamColor: RelatedString2 null!", Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedString3 == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "bTeamColor: RelatedString3 null!", Verbosity.ShowAsError );
                return;
            }
            TeamColorDefinition colorToUse = TeamColorDefinitionTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString3 );
            if ( colorToUse == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "bTeamColor: TeamColorDefinition '" + command.RelatedString3 +
                    "' could not be found!", Verbosity.ShowAsError );
                return;
            }

            Faction factionOrNull = null;
            if ( IsForDuringGame )
                factionOrNull = World_AIW2.Instance.Factions[factionArrayIndex];

            switch ( command.RelatedString2 )
            {
                case "FactionCenterColor":
                    factionConfigToDisplay.FactionCenterColor = colorToUse;
                    break;
                case "FactionTrimColor":
                    factionConfigToDisplay.FactionTrimColor = colorToUse;
                    break;
                default:
                    ArcenDebugging.ArcenDebugLogSingleLine( "bTeamColor: RelatedString2 unknown value '" + command.RelatedString2 + "'!", Verbosity.ShowAsError );
                    break;
            }

            if ( IsForDuringGame )
            {
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                    World_AIW2.Instance.Factions[i].SyncTeamColorToAndFromOthersIfNeeded();
            }
            else
            {
                //don't do this yet, it will error and it also doesn't matter
            }

            //note from Chris: do NOT mark the lobby as changed, since we don't care about the map regenerating from this
            //setupToChange.LobbyWorking_MarkAsChanged();
        }
        #endregion

        #region SimplyRegenerate
        private void SimplyRegenerate( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame )
        {
            if ( IsForDuringGame )
            {
                WriteDiscardedBecauseLobbyOnly( command, "LATECHECK" );
                return;
            }
            WorldSetup setupToChange = World_AIW2.Instance.Setup;
            setupToChange.LobbyWorking_MarkAsChanged( StartWorldSource1.RegenerateLobbyFromPlayerInput, StartWorldSource2.NotLoadingAnything );
        }
        #endregion

        #region FactionGift
        private void FactionGift( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame )
        {
            if ( !IsForDuringGame )
            {
                WriteDiscardedBecauseDuringGameOnly( command, "FactionGift" );
                return;
            }

            if ( command.RelatedIntegers.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "FactionGift: RelatedIntegers count != 1!", Verbosity.ShowAsError );
                return;
            }

            Int16 factionArrayIndex = (Int16)command.RelatedIntegers.First;
            if ( factionArrayIndex < 0 || factionArrayIndex >= World_AIW2.Instance.Factions.Count )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "FactionGift: factionArrayIndex was " + factionArrayIndex +
                    " when World_AIW2.Instance.Factions.Count was " + World_AIW2.Instance.Factions.Count + "!", Verbosity.ShowAsError );
                return;
            }
            Faction faction = World_AIW2.Instance.Factions[factionArrayIndex];
            if ( faction == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "FactionGift: faction at factionArrayIndex " + factionArrayIndex +
                    " is null!", Verbosity.ShowAsError );
                return;
            }

            PlayerTypeData playerType = faction.PlayerTypeDataOrNull_ModeratelyExpensive;
            if ( playerType == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "FactionGift: playerType at factionArrayIndex " + factionArrayIndex +
                    " is null!", Verbosity.ShowAsError );
                return;
            }

            if ( command.RelatedIntegers2.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "FactionGift: RelatedIntegers2 count != 1!", Verbosity.ShowAsError );
                return;
            }
            int giftAmount = command.RelatedIntegers2.First;
            if ( giftAmount <= 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "FactionGift: giftAmount <= 0!", Verbosity.ShowAsError );
                return;
            }
            string giftType = command.RelatedString2;
            if ( giftType == null || giftType.Length <= 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "FactionGift: giftType is blank!", Verbosity.ShowAsError );
                return;
            }

            if ( command.RelatedIntegers3.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "FactionGift: RelatedIntegers3 count != 1!", Verbosity.ShowAsError );
                return;
            }

            Int16 targetFactionArrayIndex = (Int16)command.RelatedIntegers3.First;
            if ( targetFactionArrayIndex < 0 || targetFactionArrayIndex >= World_AIW2.Instance.Factions.Count )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "FactionGift: targetFactionArrayIndex was " + targetFactionArrayIndex +
                    " when World_AIW2.Instance.Factions.Count was " + World_AIW2.Instance.Factions.Count + "!", Verbosity.ShowAsError );
                return;
            }
            Faction targetFaction = World_AIW2.Instance.Factions[targetFactionArrayIndex];
            if ( targetFaction == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "FactionGift: targetFaction at targetFactionArrayIndex " + targetFactionArrayIndex +
                    " is null!", Verbosity.ShowAsError );
                return;
            }

            PlayerTypeData targetPlayerType = targetFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
            if ( targetPlayerType == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "FactionGift: targetPlayerType at targetFactionArrayIndex " + targetFactionArrayIndex +
                    " is null!", Verbosity.ShowAsError );
                return;
            }

            switch ( giftType )
            {
                case "Metal_OneTime":
                    #region Metal To Gift
                    {
                        if ( !targetPlayerType.UsesMetal )
                        {
                            if ( ArcenNetworkAuthority.GetIsHostMode() )
                            {
                                string results = faction.GetDisplayName() + " wanted to give " + giftAmount.ToString( "#,##0" ) + " metal to " + targetFaction.GetDisplayName() + 
                                    ", but the recipient does not use metal and so cannot accept it.";
                                World_AIW2.Instance.QueueChatMessageOrCommand( results, ChatType.LogToCentralChat, null );
                            }
                            break;
                        }
                        if ( !playerType.UsesMetal )
                        {
                            if ( ArcenNetworkAuthority.GetIsHostMode() )
                            {
                                string results = faction.GetDisplayName() + " wanted to give " + giftAmount.ToString( "#,##0" ) + " metal to " + targetFaction.GetDisplayName() + 
                                    ", but the gifting faction does not use metal and thus cannot gift it.";
                                World_AIW2.Instance.QueueChatMessageOrCommand( results, ChatType.LogToCentralChat, null );
                            }
                            break;
                        }
                        int metalToGift = Math.Min( faction.StoredMetal.IntValue, giftAmount );
                        bool wroteALog = false;
                        if ( metalToGift < giftAmount )
                        {
                            wroteALog = true;
                            if ( ArcenNetworkAuthority.GetIsHostMode() )
                            {
                                string results = faction.GetDisplayName() + " wanted to gift " + giftAmount.ToString( "#,##0" ) + " metal to " + targetFaction.GetDisplayName() + ", but only had " +
                                metalToGift.ToString( "#,##0" ) + " on hand, so gifted that.";
                                World_AIW2.Instance.QueueChatMessageOrCommand( results, ChatType.LogToCentralChat, null );
                            }
                        }
                        int metalToGet = Math.Min( targetFaction.MetalStorage - targetFaction.StoredMetal.IntValue, metalToGift );
                        if ( metalToGet < metalToGift )
                        {
                            wroteALog = true;
                            if ( ArcenNetworkAuthority.GetIsHostMode() )
                            {
                                string results = faction.GetDisplayName() + " gave " + metalToGift.ToString( "#,##0" ) + " metal to " + targetFaction.GetDisplayName() + ", but the recipient only had room for " +
                                    metalToGet.ToString( "#,##0" ) + ", so that's all that was accepted.";
                                World_AIW2.Instance.QueueChatMessageOrCommand( results, ChatType.LogToCentralChat, null );
                            }
                        }
                        if ( !wroteALog )
                        {
                            if ( ArcenNetworkAuthority.GetIsHostMode() )
                            {
                                string results = faction.GetDisplayName() + " gave " + metalToGet.ToString( "#,##0" ) + " metal to " + targetFaction.GetDisplayName() + ".";
                                World_AIW2.Instance.QueueChatMessageOrCommand( results, ChatType.LogToCentralChat, null );
                            }
                        }

                        faction.StoredMetal -= metalToGet;
                        targetFaction.StoredMetal += metalToGet;
                    }
                    #endregion
                    break;
                case "Hacking_OneTime":
                    #region Hacking Points To Gift
                    {
                        int hackingToGift = Math.Min( faction.StoredHacking.IntValue, giftAmount );
                        bool wroteALog = false;
                        if ( hackingToGift < giftAmount )
                        {
                            wroteALog = true;
                            if ( ArcenNetworkAuthority.GetIsHostMode() )
                            {
                                string results = faction.GetDisplayName() + " wanted to gift " + giftAmount.ToString( "#,##0" ) + " hacking points to " + targetFaction.GetDisplayName() + ", but only had " +
                                hackingToGift.ToString( "#,##0" ) + " on hand, so gifted that.";
                                World_AIW2.Instance.QueueChatMessageOrCommand( results, ChatType.LogToCentralChat, null );
                            }
                        }
                        if ( !wroteALog )
                        {
                            if ( ArcenNetworkAuthority.GetIsHostMode() )
                            {
                                string results = faction.GetDisplayName() + " gave " + hackingToGift.ToString( "#,##0" ) + " hacking points to " + targetFaction.GetDisplayName() + ".";
                                World_AIW2.Instance.QueueChatMessageOrCommand( results, ChatType.LogToCentralChat, null );
                            }
                        }

                        faction.StoredHacking -= hackingToGift;
                        targetFaction.StoredHacking += hackingToGift;
                    }
                    #endregion
                    break;
                case "Metal_PerSecond":
                    #region Metal Per Second
                    {
                        if ( !targetPlayerType.UsesMetal )
                        {
                            if ( ArcenNetworkAuthority.GetIsHostMode() )
                            {
                                string results = faction.GetDisplayName() + " wanted to give " + giftAmount.ToString( "#,##0" ) + " metal per second to to " + targetFaction.GetDisplayName() +
                                    ", but the recipient does not use metal and so cannot accept it.";
                                World_AIW2.Instance.QueueChatMessageOrCommand( results, ChatType.LogToCentralChat, null );
                            }
                            break;
                        }
                        if ( !playerType.UsesMetal )
                        {
                            if ( ArcenNetworkAuthority.GetIsHostMode() )
                            {
                                string results = faction.GetDisplayName() + " wanted to give " + giftAmount.ToString( "#,##0" ) + " metal per second to to " + targetFaction.GetDisplayName() +
                                    ", but the gifting faction does not use metal and thus cannot gift it.";
                                World_AIW2.Instance.QueueChatMessageOrCommand( results, ChatType.LogToCentralChat, null );
                            }
                            break;
                        }

                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            string results = faction.GetDisplayName() + " is now giving " + giftAmount.ToString( "#,##0" ) + " metal per second to " + targetFaction.GetDisplayName() + ".";
                            World_AIW2.Instance.QueueChatMessageOrCommand( results, ChatType.LogToCentralChat, null );
                        }

                        for ( int i = faction.MetalGiftsFromThisPlayer.Count - 1; i >= 0; i-- )
                        {
                            if ( faction.MetalGiftsFromThisPlayer[i].Key == targetFaction.FactionIndex )
                            {
                                //if already gifting to this player, remove the old one
                                faction.MetalGiftsFromThisPlayer.RemoveAt( i );
                            }
                        }
                        faction.MetalGiftsFromThisPlayer.Add( new KeyValuePair<int, int>( targetFaction.FactionIndex, giftAmount ) );
                    }
                    #endregion
                    break;
                case "Energy_Ongoing":
                    #region Energy Ongoing
                    {
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            string results = faction.GetDisplayName() + " is now providing " + giftAmount.ToString( "#,##0" ) + " energy to " + targetFaction.GetDisplayName() + ".";
                            World_AIW2.Instance.QueueChatMessageOrCommand( results, ChatType.LogToCentralChat, null );
                        }

                        for ( int i = faction.EnergyGiftsFromThisPlayer.Count - 1; i >= 0; i-- )
                        {
                            if ( faction.EnergyGiftsFromThisPlayer[i].Key == targetFaction.FactionIndex )
                            {
                                //if already gifting to this player, remove the old one
                                faction.EnergyGiftsFromThisPlayer.RemoveAt( i );
                            }
                        }
                        faction.EnergyGiftsFromThisPlayer.Add( new KeyValuePair<int, int>( targetFaction.FactionIndex, giftAmount ) );
                    }
                    #endregion
                    break;
                case "ClearOngoingMetalGift":
                    #region ClearOngoingMetalGift
                    {
                        for ( int i = faction.MetalGiftsFromThisPlayer.Count - 1; i >= 0; i-- )
                        {
                            if ( faction.MetalGiftsFromThisPlayer[i].Key == targetFaction.FactionIndex )
                            {
                                if ( ArcenNetworkAuthority.GetIsHostMode() )
                                {
                                    string results = faction.GetDisplayName() + " has stopped giving " + faction.MetalGiftsFromThisPlayer[i].Value.ToString( "#,##0" ) + 
                                        " metal per second to " + targetFaction.GetDisplayName() + ".";
                                    World_AIW2.Instance.QueueChatMessageOrCommand( results, ChatType.LogToCentralChat, null );
                                }
                                //if gifting to this player, remove it
                                faction.MetalGiftsFromThisPlayer.RemoveAt( i );
                            }
                        }
                    }
                    #endregion
                    break;
                case "ClearOngoingEnergyGift":
                    #region ClearOngoingEnergyGift
                    {
                        for ( int i = faction.EnergyGiftsFromThisPlayer.Count - 1; i >= 0; i-- )
                        {
                            if ( faction.EnergyGiftsFromThisPlayer[i].Key == targetFaction.FactionIndex )
                            {
                                if ( ArcenNetworkAuthority.GetIsHostMode() )
                                {
                                    string results = faction.GetDisplayName() + " has stopped providing " + faction.EnergyGiftsFromThisPlayer[i].Value.ToString( "#,##0" ) +
                                        " ongoing energy to " + targetFaction.GetDisplayName() + ".";
                                    World_AIW2.Instance.QueueChatMessageOrCommand( results, ChatType.LogToCentralChat, null );
                                }
                                //if gifting to this player, remove it
                                faction.EnergyGiftsFromThisPlayer.RemoveAt( i );
                            }
                        }
                    }
                    #endregion
                    break;
                default:
                    ArcenDebugging.ArcenDebugLogSingleLine( "FactionGift: unknown giftType '" + giftType + "'!", Verbosity.ShowAsError );
                    break;
            }
        }
        #endregion

        #region CampaignType
        private void CampaignType( GameCommand command, ArcenClientOrHostSimContextCore context, bool IsForDuringGame )
        {
            if ( IsForDuringGame )
            {
                WriteDiscardedBecauseLobbyOnly( command, "LATECHECK" );
                return;
            }

            if ( command.RelatedString2 == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "CampaignType: RelatedString2 null!", Verbosity.ShowAsError );
                return;
            }
            CampaignTypeData campaignTypeData = CampaignTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( campaignTypeData == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "CampaignType: Could not find CampaignTypeData with internal name '" + command.RelatedString2 + "'!", Verbosity.ShowAsError );
                return;
            }
            if ( World_AIW2.Instance.CampaignType == campaignTypeData )
                return;

            World_AIW2.Instance.CampaignType = campaignTypeData;
            //nothing would visually change in the lobby, so no reason to regenerate
            //setupToChange.LobbyWorking_MarkAsChanged( StartWorldSource1.RegenerateLobbyFromPlayerInput, StartWorldSource2.NotLoadingAnything );

            //this is the range where we start considering it Expert mode, though 1000 is technicall expert
            bool isExpert = campaignTypeData.HarshnessRating >= 700;
            //if we're in the territory of Expert mode, a minimum of two AIs is now required
            int requiredAIs = isExpert ? 2 : 1;
            int countOfAIFactions = 0;
            for ( int i = 0; i < World_AIW2.Instance.Setup.FactionConfigurations.Count; i++ )
            {
                ConfigurationForFaction fac = World_AIW2.Instance.Setup.FactionConfigurations[i];
                if (fac.SpecialFactionData.InternalName == "AI") {
                    countOfAIFactions++;
                    if (countOfAIFactions <= requiredAIs) {
                        fac.AllowRemoval = false;
                    } else {
                        fac.AllowRemoval = true;
                    }
                }
            }
            while (countOfAIFactions < requiredAIs) {
                ConfigurationForFaction fac = World_AIW2.Instance.Setup.AddFaction(SpecialFactionDataTable.Instance.GetRowByName("AI"), true );
                fac.AllowRemoval = false;
                countOfAIFactions++;
            }
        }
        #endregion
    }
    #endregion

    #region GameCommand_AfterGameStartOnly_RequestFactionChanges
    /// <summary>
    /// This is for making changes to the long-term data and the factions data AFTER the game has
    /// already started, and mapgen is complete, and the lobby data is meaningless.
    /// </summary>
    public class GameCommand_AfterGameStartOnly_RequestFactionChanges : GameCommand_SetupOnly_RequestSetupChanges
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            this.HandleCommand( command, context, true );
        }
    }
    #endregion

    /// <summary>
    /// This is for changing data on the long-term settings once the game has already started.
    /// </summary>
    public class GameCommand_DuringGameOnly_RequestGalaxySettingChanges : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( World_AIW2.Instance == null )
                return;

            if ( World_AIW2.Instance.InSetupPhase )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Discarded (Because Not Yet Fully Started) Galaxy Option Changes Requested: '" + command.RelatedString + "':\n" +
                    command.WriteToStringInefficient( true, true, true ), Verbosity.DoNotShow );
                return;
            }
            if ( command.RelatedString == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Null RelatedString passed to DuringGameOnly_RequestGalaxySettingChanges!", Verbosity.ShowAsError );
                return;
            }
            //ArcenDebugging.ArcenDebugLogSingleLine( "Galaxy Option Change Requested: '" + command.RelatedString + "':\n" +
            //    command.WriteToStringInefficient( true, true, true ), Verbosity.DoNotShow );
            switch ( command.RelatedString )
            {
                case "AIWar2GalaxySettingChanged_Int":
                    //ArcenDebugging.ArcenDebugLogSingleLine( "AIWar2GalaxySettingChanged_Int: '" + command.RelatedString + "':\n" +
                    //    command.WriteToStringInefficient( true, true, true ), Verbosity.DoNotShow );
                    this.AIWar2GalaxySettingChanged_Int( command, context );
                    break;
                case "AIWar2GalaxySettingChanged_String":
                    //ArcenDebugging.ArcenDebugLogSingleLine( "AIWar2GalaxySettingChanged_String: '" + command.RelatedString + "':\n" +
                    //    command.WriteToStringInefficient( true, true, true ), Verbosity.DoNotShow );
                    this.AIWar2GalaxySettingChanged_String( command, context );
                    break;
                default:
                    ArcenDebugging.ArcenDebugLogSingleLine( "Unknown RelatedString '" + command.RelatedString + "' passed to DuringGameOnly_RequestGalaxySettingChanges!", Verbosity.ShowAsError );
                    break;
            }
            World_AIW2.Instance.DoOnSpecialEvent_OnMainThread_ClientOrHost_ForAllFactionsAndExternalWorldBaseInfo( SpecialEventType.IngameGalaxyOptionsSavedAndAppliedFromGameCommand, context );
        }

        #region AIWar2GalaxySettingChanged_Int
        private void AIWar2GalaxySettingChanged_Int( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            WorldSetup setupToChange = World_AIW2.Instance.Setup;

            if ( command.RelatedString2 == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "AIWar2GalaxySettingChanged_Int: RelatedString2 null!", Verbosity.ShowAsError );
                return;
            }
            AIWar2GalaxySetting setting = AIWar2GalaxySettingTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( setting == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "AIWar2GalaxySettingChanged_Int: Could not find AIWar2GalaxySetting with internal name '" + command.RelatedString2 + "'!", Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedIntegers.Count != 1 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "AIWar2GalaxySettingChanged_Int: RelatedIntegers count != 1!", Verbosity.ShowAsError );
                return;
            }
            int newValue = command.RelatedIntegers.First;
            if ( setupToChange.GetIntBySetting( setting ) == newValue )
                return;
            setupToChange.SetIntBySetting( setting, newValue );
            AIWar2GalaxySettingQuickAccess.UpdateQuickAccessSetting( setting );
        }
        #endregion

        #region AIWar2GalaxySettingChanged_String
        private void AIWar2GalaxySettingChanged_String( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            WorldSetup setupToChange = World_AIW2.Instance.Setup;

            if ( command.RelatedString2 == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "AIWar2GalaxySettingChanged_String: RelatedString2 null!", Verbosity.ShowAsError );
                return;
            }
            AIWar2GalaxySetting setting = AIWar2GalaxySettingTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( setting == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "AIWar2GalaxySettingChanged_String: Could not find AIWar2GalaxySetting with internal name '" + command.RelatedString2 + "'!", Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedString3 == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "AIWar2GalaxySettingChanged_String: RelatedString3 null!", Verbosity.ShowAsError );
                return;
            }
            if ( setupToChange.GetStringBySetting( setting ) == command.RelatedString3 )
            {
                //ArcenDebugging.ArcenDebugLogSingleLine( "AIWar2GalaxySettingChanged_String: '" + setupToChange.GetStringBySetting( setting ) + " equals " +
                //    command.RelatedString3, Verbosity.DoNotShow );
                return;
            }
            setupToChange.SetStringBySetting( setting, command.RelatedString3 );
            AIWar2GalaxySettingQuickAccess.UpdateQuickAccessSetting( setting );
            //ArcenDebugging.ArcenDebugLogSingleLine( "AIWar2GalaxySettingChanged_String: '" + setupToChange.GetStringBySetting( setting ) + " did NOT equal " +
            //    command.RelatedString3 + ", but should now.", Verbosity.DoNotShow );
        }
        #endregion        
    }
    
    public class GameCommand_AlterLobbyTabsDueToReset : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            switch( Window_SetupTopTabs.Current )
            {
                case LobbyTabType.Map:
                    //map tab should be fine
                    //Window_SetupTopTabs.Current = LobbyTabType.Factions;
                    break;
                default:
                    Window_SetupTopTabs.Current = LobbyTabType.Map;
                    break;
            }
        }
    }
}
