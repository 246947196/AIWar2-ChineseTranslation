using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class PlayerAutobuilding : ExternalWorldDeepInfo
    {
        public static PlayerAutobuilding Instance;

        public List<Tuple<ArcenSetting, List<GameEntityTypeData>, bool>> AutoBuildingSettings =
            List<Tuple<ArcenSetting, List<GameEntityTypeData>, bool>>.Create_WillNeverBeGCed( 30, "HostOnlyJournalsAndAutosaveAndSimilarHandler-AutoBuildingSettings" );
        public bool HasFinishedCalculatingAutoBuildingSettings = false;

        public PlayerAutobuilding()
        {
            Instance = this;
        }

        public override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            HasFinishedCalculatingAutoBuildingSettings = false;
            AutoBuildingSettings.Clear();
        }

        public override string GetIdentifierForErrorMessages()
        {
            return GetType().Name;
        }

        public override bool GetShouldIBeInUse()
        {
            return true;
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType ) { }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType ) { }

        protected override void DoPerSimStepLogic_OnMainThread_NonSim__HostOnly( ArcenHostOnlySimContext Context ) { }

        protected override void DoPerSecondLogic_OnMainThread_NonSim__HostOnly( ArcenHostOnlySimContext Context )
        {
            foreach ( Faction faction in World_AIW2.Instance.Factions )
            {
                HandleAutobuild_ForSpecificFaction( faction, Context );
            }
        }

        #region HandleAutobuild_ForSpecificFaction
        private void HandleAutobuild_ForSpecificFaction( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            PlayerAccount playerControlling = aPlayerFaction.GetFirstAssociatedPlayerAccountOrNull();
            if ( playerControlling == null )
                return; //only do this for factions that are being controlled by a player account!

            if ( World_AIW2.Instance.TutorialOrNull != null && World_AIW2.Instance.TutorialOrNull.DisableAutobuild )
                return; //don't do autobuild stuff during a tutorial unless the tutorial requests it

            int debugCode = 0;
            try
            {
                debugCode = 1000;

                if ( AutoBuildingSettings.Count <= 0 )
                {
                    ArcenSettingTable settingTable = ArcenSettingTable.Instance;
                    for ( int i = 0; i < settingTable.Rows.Count; i++ )
                    {
                        ArcenSetting set = settingTable.Rows[i];
                        if ( !set.Tags.Contains( "IsAutoBuildingSetting" ) )
                            continue;
                        if ( set.RowFromXmlMod != null && set.RowFromXmlMod.IsOff() )
                            continue;
                        List<string> tags = set.Tags;
                        List<GameEntityTypeData> types = List<GameEntityTypeData>.Create_WillNeverBeGCed( 30, "HostOnlyJournalsAndAutosaveAndSimilarHandler-HandleAutobuild_ForSpecificFaction-BuidTypeList" );
                        for ( int j = 0; j < tags.Count; j++ )
                        {
                            List<GameEntityTypeData> currentTypes = GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( tags[j] );
                            if ( currentTypes != null && currentTypes.Count > 0 )
                                types.AddRange( currentTypes );
                        }
                        bool countsAsDefenseForHelperJournal = tags.Contains( "CountsAsDefenseForHelperJournal" );
                        if ( types.Count > 0 )
                            AutoBuildingSettings.Add( new Tuple<ArcenSetting, List<GameEntityTypeData>, bool>( set, types, countsAsDefenseForHelperJournal ) );
                        else
                            ArcenDebugging.SingleLineQuickDebug( "Warning: Could not find any entity type for auto-building setting " + set.InternalName +
                                ". Either a mod or expansion is missing or something is wrong with the tags used." );
                        if ( !set.IsNetworkAndGameSyncedSetting )
                            ArcenDebugging.SingleLineQuickDebug( "Warning: Auto-building setting " + set.InternalName + " is currently not synced across the network - this might work on hosts, but not clients." );
                    }
                }

                debugCode = 2000;

                bool haveThingsToAutoBuild = false;
                ArcenSetting setting;
                for ( int i = 0; i < AutoBuildingSettings.Count && !haveThingsToAutoBuild; i++ )
                {
                    setting = AutoBuildingSettings[i].Item1;
                    switch ( setting.Type )
                    {
                        case ArcenSettingType.BoolHidden:
                        case ArcenSettingType.BoolToggle:
                            if ( playerControlling.GetNetworkAttachedBoolBySetting( setting ) )
                                haveThingsToAutoBuild = true;
                            break;
                        case ArcenSettingType.IntDropdown:
                        case ArcenSettingType.IntHidden:
                        case ArcenSettingType.IntSlider:
                        case ArcenSettingType.IntTextbox:
                            if ( playerControlling.GetNetworkAttachedIntBySetting( setting ) > 0 )
                                haveThingsToAutoBuild = true;
                            break;
                        case ArcenSettingType.FloatHidden:
                        case ArcenSettingType.FloatSlider:
                            if ( playerControlling.GetNetworkAttachedFloatBySetting( setting ) > 0f )//there shouldn't be a reason for floats, FInts or strings for these settings but just in case - go
                                haveThingsToAutoBuild = true;
                            break;
                        case ArcenSettingType.FIntHidden:
                            if ( playerControlling.GetNetworkAttachedFIntBySetting( setting ) > FInt.Zero )
                                haveThingsToAutoBuild = true;
                            break;
                        case ArcenSettingType.StringHidden:
                            if ( playerControlling.GetNetworkAttachedStringBySetting( setting ).Equals( "Y" ) )
                                haveThingsToAutoBuild = true;
                            break;
                    }
                }

                debugCode = 3000;

                if ( haveThingsToAutoBuild )
                {
                    debugCode = 2100;
                    //Check for all out command stations to see if there is an associated energy collector, and if not issue a GameCommand
                    //to build one
                    foreach ( GameEntity_Squad commandStation in aPlayerFaction.Squads( EntityRollupType.CommandStation ) )
                    {
                        debugCode = 2200;
                        if ( commandStation.SecondsSpentAsRemains > 0 )
                            continue;
                        if ( commandStation.RepairImpossibleForSeconds > 0 )
                            continue;
                        if ( commandStation.SelfBuildingMetalRemaining > FInt.Zero )
                            continue; //command station is still building
                                                       //PlanetFaction planetFaction = commandStation.Planet.GetPlanetFactionForFaction( faction );

                        debugCode = 2300;
                        FleetMembership fleetMem = commandStation.FleetMembership;
                        if ( fleetMem == null )
                            continue;
                        debugCode = 2310;
                        Fleet fleet = fleetMem.Fleet;
                        if ( fleet == null )
                            continue;
                        debugCode = 2320;
                        Planet planet = commandStation.Planet;
                        if ( planet == null )
                            continue;
                        debugCode = 2330;

                        int countToBuild;
                        Tuple<ArcenSetting, List<GameEntityTypeData>, bool> currentItem;
                        for ( int i = 0; i < AutoBuildingSettings.Count; i++ )
                        {
                            debugCode = 2500;
                            currentItem = AutoBuildingSettings[i];
                            setting = currentItem.Item1;
                            countToBuild = 0;
                            switch ( setting.Type )
                            {
                                case ArcenSettingType.BoolHidden:
                                case ArcenSettingType.BoolToggle:
                                    if ( playerControlling.GetNetworkAttachedBoolBySetting( setting ) )
                                        countToBuild = int.MaxValue;
                                    break;
                                case ArcenSettingType.IntDropdown:
                                case ArcenSettingType.IntHidden:
                                case ArcenSettingType.IntSlider:
                                case ArcenSettingType.IntTextbox:
                                    countToBuild = playerControlling.GetNetworkAttachedIntBySetting( setting );
                                    break;
                                case ArcenSettingType.FloatHidden:
                                case ArcenSettingType.FloatSlider:
                                    countToBuild = (int) Math.Round( playerControlling.GetNetworkAttachedFloatBySetting( setting ) );
                                    break;
                                case ArcenSettingType.FIntHidden:
                                    countToBuild = playerControlling.GetNetworkAttachedFIntBySetting( setting ).IntValue;
                                    break;
                                case ArcenSettingType.StringHidden:
                                    if ( playerControlling.GetNetworkAttachedStringBySetting( setting ).Equals( "Y" ) )
                                        countToBuild = int.MaxValue;
                                    break;
                            }
                            if ( countToBuild <= 0 )
                                continue;
                            debugCode = 2600;
                            GameEntityTypeData currentType;
                            int currentCountToBuild;
                            for ( int j = 0; j < currentItem.Item2.Count; j++ )
                            {
                                currentCountToBuild = countToBuild;
                                currentType = currentItem.Item2[j];
                                try
                                {
                                    foreach ( FleetMembership currentItemMem in fleet.MemberGroupsUnsorted_Sim )
                                    {
                                        debugCode = 2700;
                                        if ( currentItemMem == null || currentItemMem.TypeData != currentType )
                                            continue;
                                        if ( currentCountToBuild > currentItemMem.EffectiveSquadCap )
                                            currentCountToBuild = currentItemMem.EffectiveSquadCap;
                                        if ( currentCountToBuild <= 0 )
                                            continue;
                                        if ( currentItemMem.GetCanBuildAnother( true, currentCountToBuild, ExtraFromStacks.Recalculate ) != ArcenRejectionReason.Unknown )
                                            continue;
                                        StateOfMatterTypeData stateOfMatter = currentItemMem.TypeData.ForcedToAlwaysBeThisStateOfMatter;
                                        if ( stateOfMatter == null )
                                            stateOfMatter = StateOfMatterTypeDataTable.Instance.DefaultRow;
                                        if ( currentType.AutoplacementAnchor == PlacementAnchor.CenterEntity )
                                        {
                                            PlacementCondition.PlaceUnitsUpToCap_AroundEntity( Context, currentItemMem, planet, commandStation, stateOfMatter, currentCountToBuild, true, true, false );
                                        } else if ( currentType.AutoplacementAnchor == PlacementAnchor.CenterOfPlanet )
                                        {
                                            PlacementCondition.PlaceUnitsUpToCap_AroundPlanetCenter( Context, currentItemMem, planet, stateOfMatter, currentCountToBuild, true, true, false );
                                        } else
                                        {
                                            PlacementCondition.PlaceUnitsUpToCap_InSniperRing( Context, currentItemMem, planet, commandStation, stateOfMatter, currentCountToBuild, true, true, false );
                                        }
                                    }
                                } catch( IndexOutOfRangeException ) { } //there is a cross-threading IndexOutOfRangeException which is somewhat likely to happen if the code tries really hard and long to find good locations
                            }
                        }
                    }
                }
            } catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception during HandleAutobuildForSpecificFaction debugCode " + debugCode + " " +
                    " on faction " + aPlayerFaction.GetDisplayName() + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion
    }
}
