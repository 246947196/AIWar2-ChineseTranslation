using Arcen.Universal;
using System;
using Arcen.AIW2.Core;
using System.Text;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// These are just methods that were in Fleet that have been moved into this dll.
    /// The main reason is to make it so that this can access things like pathfinders, but it also helps make more of the game open source.
    /// </summary>
    public class FleetSimLogicImplementation : FleetSimLogic
    {
        public override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            
        }

        public FleetSimLogicImplementation()
        {
            FleetSimLogic.Instance = this;
        }

        #region PerSecond_UpdateFleetData
        public override void PerSecond_UpdateFleetData( Fleet fleet, ArcenSimContextAnyStatus Context )
        {
            //This is where we handle things that auto-build copies of themselves
            //as they do damage. Viral Shredders for example.
            //Each of these things has a "UnitToMakeWithBuildPoints" and a "BaseShipLineForBuildPoints".
            //We steal the cap from BaseShipLineForBuildPoints, and create copies of the UnitToMake if appropriate
            int debugCode = 0;
            try
            {
                debugCode = 50;
                if ( fleet.NumberGameSecondsBeforeCanBeAssisted > 0 )
                    fleet.NumberGameSecondsBeforeCanBeAssisted--;

                debugCode = 100;
                BuildForMembershipsByBuildPoints( fleet, Context.GetHostOnlyContext() );
                debugCode = 400;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "In PerSecond_UpdateFleetData, debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion

        #region BuildForMembershipsByBuildPoints
        private void BuildForMembershipsByBuildPoints( Fleet fleet, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return; //if you let this happen on the MP client, it will get into an infinite loop!

            int debugCode = 0;
            try
            {
                GameEntity_Squad centerpieceSquad = fleet.Centerpiece.GetSquad();
                if ( centerpieceSquad == null || centerpieceSquad.GetIsCrippled() || centerpieceSquad.GetIsNonFunctional() )
                    return;

                GameEntityTypeData typeToBuild;
                foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                {
                    debugCode = 300;
                    //Have the membership serialize the viral shredder stuff
                    //then manage it all here
                    typeToBuild = mem.TypeData.UnitToMakeWithBuildPoints_TypeData;
                    if ( typeToBuild == null || mem.TypeData.BuildPointsPerDamageDealt == FInt.Zero )
                        continue;
                    #region If We Can't Be Built Ourselves, But We Build Something Else (eg Viral Shredder Copies)
                    if ( mem.TypeData.CannotActuallyBeBuilt_BuildsSelfIndirectly && mem.TypeData != mem.TypeData.BaseShipLineForBuildPoints_TypeData )
                    {
                        FleetMembership baseMem = mem.GetBaseMembershipForBuildPoints();
                        if ( baseMem == null ) //this other group was probably trasferred out (eg main viral shredders)
                        {
                            //if this fleet actually has some of the child type (eg viral shredder copies) then delete them
                            if ( mem.EntitiesOfFMem.Count > 0 )
                            {
                                ArcenOverLinkedList<GameEntity_Squad>.ArcenLinkedListItem wrapper = mem.EntitiesOfFMem.GetFirst();
                                ArcenOverLinkedList<GameEntity_Squad>.ArcenLinkedListItem nextWrapper;
                                while ( wrapper != null )
                                {
                                    GameEntity_Squad entity = wrapper.Contained;
                                    nextWrapper = wrapper.NextItem;
                                    if ( entity != null )
                                        entity.Despawn( Context, true, InstancedRendererDeactivationReason.PlayerIsScrappingMe );
                                    wrapper.RemoveMe( false );
                                    wrapper = nextWrapper;
                                }
                            }
                            continue;
                        }
                    }
                    #endregion

                    debugCode = 400;
                    //ArcenDebugging.ArcenDebugLogSingleLine(mem.TypeData.GetDisplayName() + " has " + mem.BuildPoints + " build points; cost is " + typeToBuild.MarkStatsFor( mem.EffectiveMark ).MetalCost , Verbosity.DoNotShow );
                    //Okay, so this unit generates build points. Check how many build points we have. If there are "enough"
                    //then check the baseMem for the cap. Create a ship if appropriate
                    debugCode = 500;
                    while ( mem.BuildPoints > typeToBuild.MarkStatsFor( mem.EffectiveMark ).MetalCost )
                    {
                        debugCode = 600;
                        FleetMembership baseMem = mem.GetBaseMembershipForBuildPoints();
                        if ( baseMem == null )
                        {
                            //don't complain to me if we hit this point, which we should not be hitting now
                            //if ( !ArcenNetworkAuthority.IsClient )
                            //    ArcenDebugging.ArcenDebugLogSingleLine( "BuildForMembershipsByBuildPoints: Build points attempt for " + mem.TypeData.GetDisplayName() +
                            //        ": no base unit type exists in this fleet for '" + mem.TypeData.BaseShipLineForBuildPoints_TypeData?.InternalName + "'.",
                            //        Verbosity.ShowAsError );
                            break;
                        }
                        debugCode = 700;
                        FleetMembership shipLineToBuild = mem.GetOrAddTargetMembershipForBuildPoints();
                        debugCode = 750;
                        if ( shipLineToBuild.GetCanBuildAnother( false, baseMem.EffectiveSquadCap, ExtraFromStacks.IncludePrecalc ) != ArcenRejectionReason.Unknown ) {
                            break;
                        }
                        debugCode = 800;
                        GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(
                            centerpieceSquad.PlanetFaction, typeToBuild, mem.EffectiveMark,
                            fleet, shipLineToBuild.UniqueTypeDataDifferentiatorForDuplicates, centerpieceSquad.WorldLocation, Context, "Fleet-BuildByBuildPoints" );
                        if ( newEntity != null ) //generall means an MP client
                        {
                            centerpieceSquad.Orders.CopyTo( centerpieceSquad, newEntity, newEntity.Orders, true, true, true );
                            //we can make a new ship! Check the cap for the base item
                            mem.BuildPoints -= typeToBuild.MarkStatsFor( mem.EffectiveMark ).MetalCost;
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "In BuildForMembershipsByBuildPoints, debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion

        #region FixUpShipCapsThatAreNotExplicitlySaved
        /// <summary>
        /// The reason for doing this here is so that balance tuning can be used to alter these fleets in existing savegames.
        /// These aren't procedural in nature, so we may as well not save the data but rather let it be directly tune-able.
        /// </summary>
        public override void FixUpShipCapsThatAreNotExplicitlySaved( Fleet fleet )
        {
            fleet.RemoveAnyEmptySlotsPresent();

            GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
            switch ( fleet.Category )
            {
                case FleetCategory.PlayerPlanetaryCommand:
                    //this is ok to be null, and can happen when the player no longer controls that planet.  No worries!
                    if ( centerpiece != null )
                    {
                        fleet.HasBeenSetUp = false;
                        //assuming it's not null, let's set our membership caps!
                        fleet.SetAllMembershipsUpFromDesignTemplates_HostOnly( null, FleetDesignLogic.Unused, centerpiece.TypeData.FleetDesignTemplatesIAlwaysGrant );
                        //ArcenDebugging.ArcenDebugLog( "Trying to repair memberships from PlayerPlanetaryCommand planet " + ( fleet.Planet == null ? "null" : fleet.GetPlanetName_Safe() ) + " " + 
                        //    centerpiece.TypeData.InternalName, Verbosity.ShowAsError );
                    }
                    //else
                    //    ArcenDebugging.ArcenDebugLog( "Null centerpiece on PlayerPlanetaryCommand planet " + ( fleet.Planet == null ? "null" : fleet.GetPlanetName_Safe() ), Verbosity.ShowAsError );
                    break;
                case FleetCategory.NonPlayerDrone:
                    if ( centerpiece == null )
                    {
                        RepairMissingCenterpieceFromMembershipContents( fleet );
                        centerpiece = fleet.Centerpiece.GetSquad();
                    }
                    //if this is null, then... well, probably the fleet leader died?  Deal with that elsewhere.
                    if ( centerpiece != null )
                    {
                        fleet.HasBeenSetUp = false;
                        //assuming it's not null, let's set our membership caps!
                        fleet.SetAllMembershipsUpFromDesignTemplates_HostOnly( centerpiece.TypeData.FleetDesignTemplateIUseForDrones, FleetDesignLogic.Unused, null );
                    }
                    //else
                    //    ArcenDebugging.ArcenDebugLog( "Null centerpiece on Drone fleet " + ( fleet.Faction == null ? "null" : fleet.Faction.GetDisplayName() ) + " " +
                    //        fleet.Centerpiece.PrimaryKeyID, Verbosity.ShowAsError );
                    break;
                default:
                    if ( centerpiece == null )
                    {
                        RepairMissingCenterpieceFromMembershipContents( fleet );
                        centerpiece = fleet.Centerpiece.GetSquad();
                    }
                    if ( centerpiece != null && centerpiece.TypeData.SpecialType == SpecialEntityType.ThirdPartySellerToPlayers )
                    {
                        fleet.HasBeenSetUp = false;
                        //assuming it's not null, let's set our membership caps!
                        fleet.SetAllMembershipsUpFromDesignTemplates_HostOnly( null, FleetDesignLogic.Unused, centerpiece.TypeData.FleetDesignTemplatesIAlwaysGrant );
                        //ArcenDebugging.ArcenDebugLog( "Trying to repair memberships from PlayerPlanetaryCommand planet " + ( fleet.Planet == null ? "null" : fleet.GetPlanetName_Safe() ) + " " + 
                        //    centerpiece.TypeData.InternalName, Verbosity.ShowAsError );
                    }
                    break;
            }
        }
        #endregion

        #region RepairMissingCenterpieceFromMembershipContents
        public void RepairMissingCenterpieceFromMembershipContents( Fleet fleet )
        {
            switch (fleet.Category)
            {
                case FleetCategory.PlayerLoose:
                    return; //don't try it for this kind
            }

            foreach ( GameEntity_Squad ship in fleet.Entities )
            {
                if ( ship != null && ship.TypeData != null && !ship.ToBeRemovedAtEndOfThisFrame )
                {
                    switch ( fleet.Category )
                    {
                        case FleetCategory.NonPlayerDrone:
                            if ( ship.TypeData.FleetDesignTemplateIUseForDrones != null )
                            {
                                fleet.Centerpiece = LazyLoadSquadWrapper.Create( ship );
                                if ( GameSettings.Current.GetBoolBySetting( "Debug_WriteFleetCenterpieceRepairsInLog" ) )
                                    ArcenDebugging.ArcenDebugLog( "Had to repair the centerpiece for drone fleet with type " + ship.TypeData.InternalName + " (fleet " + fleet.FleetID + ")", Verbosity.Chat );
                                break;
                            }
                            break;
                        case FleetCategory.PlayerMobile:
                            switch ( ship.TypeData.SpecialType )
                            {
                                case SpecialEntityType.MobileOfficerCombatFleetFlagship:
                                case SpecialEntityType.MobileStrikeCombatFleetFlagship:
                                case SpecialEntityType.MobileCustomUnattachedFleetFlagship:
                                case SpecialEntityType.MobileCustomCityFedFleetFlagship:
                                case SpecialEntityType.MobileSupportFleetFlagship:
                                case SpecialEntityType.HumanHomeArk:
                                    fleet.Centerpiece = LazyLoadSquadWrapper.Create( ship );
                                    if ( GameSettings.Current.GetBoolBySetting( "Debug_WriteFleetCenterpieceRepairsInLog" ) )
                                        ArcenDebugging.ArcenDebugLog( "Had to repair the centerpiece for PlayerMobile fleet with type " + ship.TypeData.InternalName + " (fleet " + fleet.FleetID + ")", Verbosity.Chat );
                                    break;
                            }
                            break;
                        case FleetCategory.PlayerCustomCityFedMobile:
                            if ( ship.TypeData.SpecialType == SpecialEntityType.MobileCustomCityFedFleetFlagship )
                            {
                                fleet.Centerpiece = LazyLoadSquadWrapper.Create( ship );
                                break;
                            }
                            break;
                        case FleetCategory.PlayerCustomUnattachedMobile:
                            if ( ship.TypeData.IsFleetLeader )
                            {
                                fleet.Centerpiece = LazyLoadSquadWrapper.Create( ship );
                                break;
                            }
                            break;
                        case FleetCategory.PlayerBattlestation:
                            switch ( ship.TypeData.SpecialType )
                            {
                                case SpecialEntityType.BattlestationBasic:
                                case SpecialEntityType.BattlestationCitadel:
                                    fleet.Centerpiece = LazyLoadSquadWrapper.Create( ship );
                                    if ( GameSettings.Current.GetBoolBySetting( "Debug_WriteFleetCenterpieceRepairsInLog" ) )
                                        ArcenDebugging.ArcenDebugLog( "Had to repair the centerpiece for PlayerBattlestation fleet with type " + ship.TypeData.InternalName + " (fleet " + fleet.FleetID + ")", Verbosity.Chat );
                                    break;
                            }
                            break;
                        case FleetCategory.PlayerPlanetaryCommand:
                            switch ( ship.TypeData.SpecialType )
                            {
                                case SpecialEntityType.HumanHomeCommand:
                                case SpecialEntityType.NormalHumanCommandStation:
                                    fleet.Centerpiece = LazyLoadSquadWrapper.Create( ship );
                                    if ( GameSettings.Current.GetBoolBySetting( "Debug_WriteFleetCenterpieceRepairsInLog" ) )
                                        ArcenDebugging.ArcenDebugLog( "Had to repair the centerpiece for PlayerBattlestation fleet with type " + ship.TypeData.InternalName + " (fleet " + fleet.FleetID + ")", Verbosity.Chat );
                                    break;
                            }
                            break;
                        case FleetCategory.PlayerCustomCity:
                            if ( ship.TypeData.SpecialType == SpecialEntityType.CityCenter )
                            {
                                fleet.Centerpiece = LazyLoadSquadWrapper.Create( ship );
                                break;
                            }
                            break;
                        case FleetCategory.NPC:
                            switch ( ship.TypeData.SpecialType )
                            {
                                case SpecialEntityType.ThirdPartySellerToPlayers:
                                    {
                                        fleet.Centerpiece = LazyLoadSquadWrapper.Create( ship );
                                        if ( GameSettings.Current.GetBoolBySetting( "Debug_WriteFleetCenterpieceRepairsInLog" ) )
                                            ArcenDebugging.ArcenDebugLog( "Had to repair the centerpiece for NPC trader fleet with type " + ship.TypeData.InternalName + " (fleet " + fleet.FleetID + ")", Verbosity.Chat );
                                        break;
                                    }
                                case SpecialEntityType.NPCFactionCenterpiece:
                                    {
                                        fleet.Centerpiece = LazyLoadSquadWrapper.Create( ship );
                                        if ( GameSettings.Current.GetBoolBySetting( "Debug_WriteFleetCenterpieceRepairsInLog" ) )
                                            ArcenDebugging.ArcenDebugLog( "Had to repair the centerpiece for NPC general fleet with type " + ship.TypeData.InternalName + " (fleet " + fleet.FleetID + ")", Verbosity.Chat );
                                        break;
                                    }
                            }
                            break;
                        case FleetCategory.PlayerLoose:
                            break; //nothing to do on these
                        default:
                            ArcenDebugging.ArcenDebugLogSingleLine( "No RepairMissingCenterpieceFromMembershipContents logic for fleet.Category:" + fleet.Category, Verbosity.ShowAsError );
                            break;
                    }
                }
            }

        }
        #endregion

        #region PerFrame_CalculateEffectiveFleetData
        public override void PerFrame_CalculateEffectiveFleetData( Fleet fleet, Faction LocalPlayerFactionForUICalculations)
        {
            int debugStage = 1;
            try
            {
                debugStage = 100;
                fleet.CalculateConstructionBlocked();
                fleet.CalculatePresenceOnPlanets();

                debugStage = 150;

                var centerpiece = fleet.Centerpiece.GetSquad();
                if ( centerpiece != null && (centerpiece.HasBeenRemovedFromSim || centerpiece.ToBeRemovedAtEndOfThisFrame) )
                {
                    centerpiece = null;
                    fleet.Centerpiece.Clear();
                }

                debugStage = 200;
                
                #region Missing Centerpiece
                if ( centerpiece == null )
                {
                    switch ( fleet.Category )
                    {
                        case FleetCategory.PlayerMobile:
                        case FleetCategory.PlayerBattlestation:
                        case FleetCategory.PlayerCustomCityFedMobile:
                        case FleetCategory.PlayerCustomUnattachedMobile:
                        case FleetCategory.PlayerPlanetaryCommand:
                        case FleetCategory.NonPlayerDrone:
                        {
                            RepairMissingCenterpieceFromMembershipContents( fleet );
                            centerpiece = fleet.Centerpiece.GetSquad();
                            break;
                        }
                    }
                }
                
                // Still null? Dump npc drones.
                if (fleet.Category == FleetCategory.NonPlayerDrone && 
                    centerpiece == null && 
                    ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                {
                    DumpFleetContentsIntoFactionLoose( fleet );
                }
                #endregion

                debugStage = 300;
                if ( centerpiece != null && centerpiece.TypeData.FleetDesignTemplateIUseForDrones != null )
                    fleet.SetAllMembershipsUpFromDesignTemplate_FullCapOverwritingNotAdding_AssumeNoDuplicates( centerpiece.TypeData.FleetDesignTemplateIUseForDrones );

                debugStage = 400;
                #region Do Fleet Design Initialization If It Has Not Yet Been Done
                if ( !fleet.HasBeenSetUp && centerpiece != null )
                {
                    switch ( fleet.Category )
                    {
                        case FleetCategory.NonPlayerDrone:
                            fleet.SetAllMembershipsUpFromDesignTemplate_FullCapOverwritingNotAdding_AssumeNoDuplicates( centerpiece.TypeData.FleetDesignTemplateIUseForDrones );
                            break;
                        default:
                            fleet.SetAllMembershipsUpFromDesignTemplates_HostOnly( null, centerpiece.TypeData.FleetDesignLogicIGrantOneOf,
                                centerpiece.TypeData.FleetDesignTemplatesIAlwaysGrant );
                            break;
                    }
                }
                #endregion

                #region Reset Player-Fleet Calculations
                debugStage = 600;
                switch ( fleet.Category )
                {
                    case FleetCategory.PlayerMobile:
                    case FleetCategory.PlayerCustomCityFedMobile:
                    case FleetCategory.PlayerCustomUnattachedMobile:
                    case FleetCategory.PlayerPlanetaryCommand:
                    case FleetCategory.PlayerBattlestation:
                    case FleetCategory.PlayerCustomCity:
                    {
                        fleet.OverridingMinSpeed = 0;
                        fleet.HullMultiplier = FInt.Zero;
                        fleet.ShieldsMultiplier = FInt.Zero;
                        fleet.AttackPowerMultiplier = FInt.Zero;
                        fleet.MaxMarkLevelOfAnyInFleet = 0;
                        break;
                    }
                }
                #endregion

                #region OnePlayer_AddedToCommandStationsAndBattlestations_Permanent
                debugStage = 800;
                FInt mult;
                // Note: All planets have this type of fleet, for every player.
                //       But unless the player actually builds a station, it won't have a centerpiece.
                if ( (fleet.Category == FleetCategory.PlayerPlanetaryCommand ||
                      fleet.Category == FleetCategory.PlayerBattlestation) &&
                     (mult = centerpiece?.TypeData?.DefensiveStructureCap_Multiplier??FInt.Zero) > FInt.Zero )
                {
                    debugStage = 810;

                    foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                    {
                            mem.WorkingAddedShipCountFromWhateverSource = 0;
                        }

                    debugStage = 840;
                    
                    // If not owned by us, show a preview as if they were owned by us.
                    // ie. Citadels
                    Faction myFac = fleet.Faction;
                    if ( myFac.Type == FactionType.NaturalObject )
                    {
                        var local = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                        if ( local != null )
                            myFac = local;
                    }

                    if ( myFac != null )
                    {
                        debugStage = 850;
                        for ( int i = 0; i < myFac.OnePlayer_AddedToCommandStationsAndBattlestations_Permanent.Count; i++ )
                        {
                            var pair = myFac.OnePlayer_AddedToCommandStationsAndBattlestations_Permanent[i];
                            if ( pair.Key == null )
                                continue;
                            
                            int amountToAdd = (pair.Value * mult).GetNearestIntPreferringHigher();
                            if ( amountToAdd <= 0 )
                                continue;
                            
                            //don't grant certain things to battlestations or citadels
                            if ( centerpiece.TypeData.IsBattlestation && pair.Key.IsBlockedFromDSSGrantToBattlestationsAndCitadels )
                                continue;
                            //BattlestationsGetHacksForAll only governs basic battlestations, not citadels
                            if ( centerpiece.TypeData.SpecialType == SpecialEntityType.BattlestationBasic &&
                                 !AIWar2GalaxySettingQuickAccess.BattlestationsGetHacksForAll )
                                continue;
                            //don't grant certain things to command stations
                            if ( centerpiece.TypeData.IsCommandStation && pair.Key.IsBlockedFromDSSGrantToCommandStations )
                                continue;

                            var mem = fleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( pair.Key );
                            if ( mem != null ) mem.WorkingAddedShipCountFromWhateverSource += amountToAdd;
                        }
                    }
                }
                #endregion
                
                #region Remove Empty FleetMembership
                debugStage = 900;
                //Backwards iterate because we remove null-typed memberships (DF's RemoveAndContinue semantics).
                for ( int memIdx = fleet.MemberGroupCount - 1; memIdx >= 0; memIdx-- )
                {
                    FleetMembership mem = fleet.GetMemberGroupAt( memIdx );
                    if ( mem == null )
                        continue;
                    if ( mem.TypeData == null )
                        fleet.RemoveMemGroupExplicit( mem );
                }
                #endregion
                
                debugStage = 950;
                fleet.BaseInfo?.PerFrame_UpdateFleetData( LocalPlayerFactionForUICalculations );

                #region PerFrame_CalculateEffectiveFleetData_P1
                debugStage = 1000;
                foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                {
                    if ( mem != null )
                        mem.PerFrame_CalculateEffectiveFleetData_P1( centerpiece, LocalPlayerFactionForUICalculations, ref debugStage );
                }
                #endregion

                #region PerFrame_CalculateEffectiveFleetData_P2_PlayerFleetOnly
                debugStage = 1200;
                switch ( fleet.Category )
                {
                    case FleetCategory.PlayerMobile:
                    case FleetCategory.PlayerCustomCityFedMobile:
                    case FleetCategory.PlayerCustomUnattachedMobile:
                    case FleetCategory.PlayerBattlestation:
                        {
                            bool humanEnergyDiscountsForLowerMarkUnits = AIWar2GalaxySettingQuickAccess.HumanEnergyDiscountsForLowerMarkUnits;
                            bool humanMetalDiscountsForLowerMarkUnits = AIWar2GalaxySettingQuickAccess.HumanMetalDiscountsForLowerMarkUnits;
                            foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                            {
                                if ( mem != null )
                                    mem.PerFrame_CalculateEffectiveFleetData_P2_PlayerFleetOnly(
                                        humanEnergyDiscountsForLowerMarkUnits, humanMetalDiscountsForLowerMarkUnits );
                            }
                        }
                        break;
                    case FleetCategory.PlayerPlanetaryCommand:
                        {
                            if ( centerpiece == null )
                                break; //all planets have this type of fleet for every player.  Don't process them unless the player controls it!

                            bool humanEnergyDiscountsForLowerMarkUnits = AIWar2GalaxySettingQuickAccess.HumanEnergyDiscountsForLowerMarkUnits;
                            bool humanMetalDiscountsForLowerMarkUnits = AIWar2GalaxySettingQuickAccess.HumanMetalDiscountsForLowerMarkUnits;

                            foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                            {
                                if ( mem != null )
                                    mem.PerFrame_CalculateEffectiveFleetData_P2_PlayerFleetOnly( humanEnergyDiscountsForLowerMarkUnits, humanMetalDiscountsForLowerMarkUnits );
                            }
                        }
                        break;
                }
                #endregion
                
                debugStage = 1300;

                // this use to only be for certain fleet types
                // but being able to put a drone fleet, or an npc fleet,
                // into load mode to trigger it to actually load
                // is useful, and more flexible
                void HandleLoadMode()
                {
                    if ( centerpiece == null )
                        return;
                    if ( fleet.IsFleetInTransportLoadMode )
                    {
                        if ( centerpiece.GetIsCrippled() || centerpiece.GetIsNonFunctional() )
                        {
                            //if it's crippled, can't be in transport mode
                            //fleet.IsFleetInTransportLoadMode = false;
                            //fleet.NumberGameSecondsBeforeCanChangeTransportStatus = ExternalConstants.Instance.Balance_SecondsAfterTransportChangeBeforeCanSwitchBack;

                            //if this is crippled and it's the host, tell every to get out now
                            if ( ArcenNetworkAuthority.GetIsHostMode() )
                            {
                                GameCommand command = GameCommand.Create( GameCommandTypeTable.Instance.GetRowByName( "UnloadTransports" ), GameCommandSource.AnythingElse );
                                command.RelatedEntityIDs.Add( centerpiece.PrimaryKeyID );
                                command.RelatedBools.Add( false ); //ToBeQueued
                                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                            }
                        }
                        else
                        {
                            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();

                            // If we are in load mode (and not crippled), then loop members for anything that needs to load.
                            // The client is also allowed to execute this, since we are just issuing orders, not actually removing entities yet.
                            foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                            {
                                if ( mem != null )
                                    PerFrame_CheckForFleetLoading( mem,  pathingCacheData, centerpiece );
                                
                            }

                            pathingCacheData.ReturnToPool();
                        }
                    }
                    else
                    {
                        //if this is the host, tell every to get out now
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            if ( fleet.GetMobileFleetTransportContentsCount() > 0 )
                            {
                                GameCommand command = GameCommand.Create( GameCommandTypeTable.Instance.GetRowByName( "UnloadTransports" ), GameCommandSource.AnythingElse );
                                command.RelatedEntityIDs.Add( centerpiece.PrimaryKeyID );
                                command.RelatedBools.Add( false ); //ToBeQueued
                                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                            }
                        }
                    }
                }
                
                HandleLoadMode();
                debugStage = 9000;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in PerFrame_CalculateEffectiveFleetData, debugStage " + debugStage + " \n" + e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region DumpFleetContentsIntoFactionLoose
        public void DumpFleetContentsIntoFactionLoose( Fleet fleet )
        {
            var con = Engine_AIW2.Instance.MainThreadContext_ClientOrHost.GetHostOnlyContext();
            if (con == null)
                return;

            if (fleet == null)
                return;
            
            StringBuilder tracebuffer = null;
            if (Engine_AIW2.TraceAtAll && 
                Engine_AIW2.TracingFlags.Has(ArcenTracingFlags.EntityRemoval))
                tracebuffer = new StringBuilder();

            tracebuffer?.AppendLine( "DumpFleetContentsIntoFactionLoose for category " + fleet.Category + " and fleet " + fleet.FleetID );

            if ( fleet.Faction == null )
            {
                tracebuffer?.AppendLine( "fleet.Faction is null so no loose fleet to give to, destroying instead" );
                DestroyFleetContentsAndWriteLog( fleet, false );
            }
            else if ( fleet.Faction.LooseFleet == null )
            {
                tracebuffer?.AppendFormat("This is probably bad: '{0}' faction's LooseFleet is null??", fleet.Faction.GetDisplayName());
                DestroyFleetContentsAndWriteLog( fleet, false );
            }
            else if ( fleet.Faction.LooseFleet == fleet )
            {
                tracebuffer?.AppendFormat("This is probably bad: '{0}' faction's LooseFleet is suppose to dump its contents????", fleet.Faction.GetDisplayName());
                DestroyFleetContentsAndWriteLog( fleet, false );
            }
            else
            {
                var newfleet = fleet.Faction.LooseFleet;
                
                // jcf: This is already called, i think, when the actual killing damage is dealt
                //      so, this is kind of late to be doing it. I think by not calling this here
                //      it allows despawn to not leave the drones behind, maybe.
                //fleet.DeployDroneContents(con, DeployReason.DyingCenterpiece);
                
                foreach ( GameEntity_Squad e in fleet.Entities )
                    {
                        if (e.ToBeRemovedAtEndOfThisFrame || e.HasBeenRemovedFromSim )
                            continue;
                        if (e.FleetMembership?.Fleet?.Centerpiece.GetSquad() == e)
                            continue;

                        // this code essentially stolen from Squad.TransformInto except its the same type and a different fleet

                        var newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(e.PlanetFaction, e.TypeData, e.CurrentMarkLevel, newfleet, 0, e.WorldLocation, con, "FleetDiedAndImNowFree");
                        newEntity.SetShipCount(e.ShipCount);

                        e.Orders.CopyTo( e, newEntity, newEntity.Orders, true, true, true );
                        newEntity.CurrentAngle = e.CurrentAngle;
                        newEntity.GameSecondEnteredThisPlanet = e.GameSecondEnteredThisPlanet;
                        newEntity.spawnVis = SpawnVisualization.Normal;
                        newEntity.IncomingDamageAmplifiedByFlat = e.IncomingDamageAmplifiedByFlat;
                        newEntity.IncomingDamageAmplifiedByMult = e.IncomingDamageAmplifiedByMult;
                        newEntity.IncomingDamageAmplifiedDuration_Max15 = e.IncomingDamageAmplifiedDuration_Max15;
                        newEntity.CurrentEngineStunSeconds = e.CurrentEngineStunSeconds;
                        newEntity.CurrentWeaponAddedReloadSeconds = e.CurrentWeaponAddedReloadSeconds;
                        newEntity.CurrentParalysisSeconds = e.CurrentParalysisSeconds;
                        newEntity.GameSecondOfLastCloakingPointLoss = e.GameSecondOfLastCloakingPointLoss;
                        newEntity.HullPointsLost = e.HullPointsLost;
                        newEntity.ShieldPointsLost = e.ShieldPointsLost;
                        newEntity.FlagForForcedFullSyncToClients_FromHost();
                
                        e.FlagForForcedFullSyncToClients_FromHost();
                        e.despawnVis = DespawnVisualization.Transformation;
                        e.Despawn( con, true, InstancedRendererDeactivationReason.FleetDiedAndImNowFree );

                        tracebuffer?.AppendLine( string.Format("created {0} in {1} to replace {2} in {3}", newEntity, newfleet, e, fleet ) );
                    }
                
                    // was previously:
                    /*
                    foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                    {
                        Fleet.TakeMembershipFromAnotherfleet(newFleet, mem, 0);

                    }
                    */

                // um does this need to get.. buffered, delayed, called from main thread only?
                World_AIW2.Instance.UnregisterFleet( fleet );
                fleet.ReturnToPool();
            }
            
            if (tracebuffer != null)
            {
                ArcenDebugging.ArcenDebugLog( tracebuffer.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion

        #region DestroyFleetContentsAndWriteLog
        public void DestroyFleetContentsAndWriteLog( Fleet fleet, bool ThisIsDuringDeserializationSoDoNotComplain )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            if ( !World_AIW2.Instance.InSetupPhase && !ThisIsDuringDeserializationSoDoNotComplain ) //only write to the log if this is not the lobby and not during deserialization
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine( "DestroyFleetContentsAndWriteLog for category " + fleet.Category + " and fleet " + fleet.FleetID );

                foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                {
                    if ( mem.EntitiesOfFMem.GetItemCount() > 0 )
                    {
                        builder.AppendLine( mem.EntitiesOfFMem.GetItemCount() + " entities destroyed of type " + mem.TypeData.InternalName );
                        ArcenOverLinkedList<GameEntity_Squad>.ArcenLinkedListItem wrapper = mem.EntitiesOfFMem.GetFirst();
                        while ( wrapper != null )
                        {
                            GameEntity_Squad entity = wrapper.Contained;
                            if ( entity != null )
                            {
                                entity.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.ExceptionWithFleetAndSoDestroyingContents );
                            }
                            wrapper = wrapper.NextItem;
                        }
                        mem.EntitiesOfFMem.Clear();
                    }
                    if ( mem.NumberCreatedButNotDeployed > 0 )
                        builder.AppendLine( mem.NumberCreatedButNotDeployed + " NumberCreatedButNotDeployed discarded of type " + mem.TypeData.InternalName );
                }

                World_AIW2.Instance.UnregisterFleet( fleet );
                fleet.ReturnToPool();
                ArcenDebugging.ArcenDebugLog( builder.ToString(), Verbosity.ShowAsError );
            }
            else //never been unpaused, aka in lobby, so just remove these without a fuss
            {
                foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                {
                    if ( mem.EntitiesOfFMem.GetItemCount() > 0 )
                    {
                        ArcenOverLinkedList<GameEntity_Squad>.ArcenLinkedListItem wrapper = mem.EntitiesOfFMem.GetFirst();
                        while ( wrapper != null )
                        {
                            GameEntity_Squad entity = wrapper.Contained;
                            if ( entity != null )
                            {
                                entity.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.ExceptionWithFleetAndSoDestroyingContents );
                            }
                            wrapper = wrapper.NextItem;
                        }
                        mem.EntitiesOfFMem.Clear();
                    }
                }

                World_AIW2.Instance.UnregisterFleet( fleet );
                fleet.ReturnToPool();
            }
        }
        #endregion

        #region DestroyFleetContentsSilent
        public void DestroyFleetContentsSilent( Fleet fleet )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
            {
                if ( mem.EntitiesOfFMem.GetItemCount() > 0 )
                {
                    ArcenOverLinkedList<GameEntity_Squad>.ArcenLinkedListItem wrapper = mem.EntitiesOfFMem.GetFirst();
                    while ( wrapper != null )
                    {
                        GameEntity_Squad entity = wrapper.Contained;
                        if ( entity != null )
                        {
                            entity.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.DestroyFleetContentsSilent );
                        }
                        wrapper = wrapper.NextItem;
                    }
                    mem.EntitiesOfFMem.Clear();
                }
            }

            World_AIW2.Instance.UnregisterFleet( fleet );
            fleet.ReturnToPool();
        }
        #endregion

        public void PerFrame_CheckForFleetLoading( FleetMembership mem, PerFactionPathCache PathCacheData, GameEntity_Squad centerpieceSquad )
        {
            if ( mem.EntitiesOfFMem.GetItemCount() == 0 || centerpieceSquad == null )
                return;
            
            if ( !mem.TypeData.IsDrone && mem.TypeData.GetCanBeTransported() != CannotTransportReason.TranportingIsFine )
                return;
            
            if ( centerpieceSquad.TypeData == mem.TypeData )
                return;

            int debugStage = 1;

            foreach ( GameEntity_Squad ship in mem.Entities )
            {
                try
                {
                    debugStage = 10;
                    if ( ship == null )
                        continue;
                    
                    debugStage = 100;
                    bool OnCorrectPlanet = true;
                    if ( ship.Planet != centerpieceSquad.Planet )
                        OnCorrectPlanet = false;
                    
                    debugStage = 200;
                    EntityOrder order = ship.RemoveInvalidatedOrdersAndReturnFirstValid_IncludingDecollision();
                    PathBetweenPlanetsForFaction path = null;
                    
                    debugStage = 300;
                    if ( order.TypeData != null )
                    {
                        //if already heading for a transport, then don't try to make us do so again!
                        if ( OnCorrectPlanet && order.TypeData.Type == EntityOrderType.GetIntoTransport )
                        {
                            Trace.Orders(ship)?.Msg("Ship {0} on same planet as {1} and already ordered to GetIntoTransport", ship, centerpieceSquad);

                            continue;
                        }

                        if ( !OnCorrectPlanet && order.TypeData.Type == EntityOrderType.Wormhole )
                        {
                            debugStage = 400;
                            Faction shipFac = ship.GetFactionOrNull_Safe();
                            if ( shipFac == null )
                                continue;

                            debugStage = 420;
                            
                            //The centerpiece is on a different planet and we're heading to some other planet; lets see if it's the right one
                            path = PathingHelper.FindPathFreshOrFromCache( shipFac, "PerFrame_CheckForPlayerFleetLoading1", ship.Planet, centerpieceSquad.Planet, PathingMode.Safest, null, PathCacheData );
                            if ( path == null )
                                continue;
                            
                            debugStage = 430;
                            if ( path.PathToReadOnly.Count > 0 && order.RelatedPlanetIndex == path.PathToReadOnly[0].Index )
                            {
                                debugStage = 440;
                                continue; //our next planet is correct; if the transport is moving then we'll notice when the next planet is no longer correct and update the path
                            }
                        }
                    }
                    debugStage = 490;
                    if ( ship.Planet == null || ship.HasBeenRemovedFromSim || ship.ToBeRemovedAtEndOfThisFrame )
                    {
                        continue;
                    }

                    if ( ship.ActiveHack != null )
                    {
                        continue;
                    }

                    debugStage = 500;
                    EntityOrderCollection orders = ship.Orders;

                    if ( !OnCorrectPlanet )
                    {
                        Trace.Orders(ship)?.Msg("Ship {0} on different planet as {1} ordered to that planet", ship, centerpieceSquad);
                        
                        debugStage = 700;
                        
                        // We have a squad in Load mode, but the centerpiece is on a different planet. Lets get to the centerpiece's planet!
                        //we weren't previously heading to the transport (since we catch that case above), so discard our current orders
                        orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.YesClearAnyOrders_IncludingFromHumans, "FleetNotOnCorrectPlanetAndLoading" );
                        
                        debugStage = 800;
                        if ( path == null )
                        {
                            Faction shipFac = ship.GetFactionOrNull_Safe();
                            if ( shipFac == null )
                                continue;
                            
                            debugStage = 810;
                            path = PathingHelper.FindPathFreshOrFromCache( shipFac, "PerFrame_CheckForPlayerFleetLoading2", ship.Planet, centerpieceSquad.Planet, PathingMode.Safest, null, PathCacheData ); //only calculate path if we didn't do it earlier
                        }
                        
                        debugStage = 900;
                        if ( path == null )
                            continue; //defensive
                        
                        for ( int j = 0; j < path.PathToReadOnly.Count; j++ )
                        {
                            if ( path.PathToReadOnly[j] == null )
                                continue;
                            
                            EntityOrder newOrder = EntityOrder.Create_Wormhole( path.PathToReadOnly[j].Index, true, true, OrderSource.Other, false );
                            
                            debugStage = 910;
                            if ( newOrder.TypeData != null )
                                orders.QueueOrder( ship, newOrder );
                        }
                    }
                    else
                    {
                        Trace.Orders(ship)?.Msg( "Ship {0} on same planet as {1} ordered to GetIntoTransport\nprev orders:\n{2})", ship, centerpieceSquad, ship.Orders);

                        debugStage = 1000;

                        //We are on the same planet as the centerpiece, so just go there
                        orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.YesClearAnyOrders_IncludingFromHumans, "FleetYesOnCorrectPlanetAndLoading" );
                        
                        debugStage = 1100;
                        
                        EntityOrder newOrder = EntityOrder.Create_GetIntoTransport( centerpieceSquad.PrimaryKeyID, false, "FleetLoading", OrderSource.Other, false );
                        if ( newOrder.TypeData != null )
                            orders.QueueOrder( ship, newOrder );
                    }
                }
                catch ( Exception e )//race conditions again?
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "PerFrame_CheckForPlayerFleetLoading exception at stage " + debugStage + ": " + e.ToString(), Verbosity.DoNotShow );
                }
            }
        }
        
        public override void DeployDroneContents( Fleet Fleet, ArcenHostOnlySimContext Context, DeployReason Reason )
        {
            // Client doesn't do this.
            if ( Context == null )
                return;
            
            GameEntity_Squad centerpieceSquad = Fleet.Centerpiece.GetSquadAndIgnoreAnyPermaNull();
            if ( centerpieceSquad == null )
            {
                //Trace.Orders(ship)?.Msg( "But this fleets centerpiece squad appears to be null." );
                return;
            }
            
            Trace.Orders(centerpieceSquad)?.Msg( "Asked to DeployDroneContents for Fleet {0} because {1}.", Fleet.GetName(), Reason );

            // early out if ordered not to engage
            if (Reason == DeployReason.ThreatDetected)
            {
                if ( Fleet.IsFleetInTransportLoadMode )
                    return;
                if ( centerpieceSquad.IsInHoldFireMode )
                    return;
            }
            
            //if (Reason == DeployReason.ThreatDetected)
                //Fleet.IsDroneThreatPresent = true;
            
            Faction faction = Fleet.Faction;
            
            foreach ( FleetMembership mem in Fleet.MemberGroupsUnsorted_Sim )
            {
                    if (Reason == DeployReason.ThreatDetected)
                    {
                        if ( mem.IsFleetMembershipConstructionPaused )
                            continue;
                    }

                    if (mem.ForMark == null)
                        continue;

                    GameEntityTypeData currentType = mem.TypeData;
                    int totalToSpawn = mem.NumberCreatedButNotDeployed;
                    bool canStack = !currentType.CannotBeStacked && currentType.IsMobile && !currentType.IsFleetLeader;
                    int remainingStacks = 1;

                    int StackingCutoff = AIWar2GalaxySettingQuickAccess.StackingCutoffNPCs;
                    if ( centerpieceSquad.PlanetFaction.Faction.Type == FactionType.Player )
                        StackingCutoff = AIWar2GalaxySettingQuickAccess.StackingCutoffPlayers;
                    if ( StackingCutoff == 0 )
                        StackingCutoff = 60;

                    if ( canStack )
                        remainingStacks = Math.Max( 1, StackingCutoff - centerpieceSquad.PlanetFaction.Entities.GetCountFromListOfEntitiesByEntityType( currentType ) );

                    if ( faction != null && faction.Type != FactionType.Player && mem.TypeData.NPCShipCap != null )
                    {
                        int currentTypeCount = faction.NPCShipCountsByCapType == null ? 0 : faction.NPCShipCountsByCapType[currentType.NPCShipCap.RowIndexNonSim];
                        int globalLimit = faction.SpecialFactionData.NPCShipCapsByType[currentType.NPCShipCap.RowIndexNonSim];
                        int rem = globalLimit - currentTypeCount;
                        if ( totalToSpawn > rem )
                            totalToSpawn = rem;
                    }

                    int stackSizeToSpawn;
                    while ( totalToSpawn > 0 )
                    {
                        if ( canStack )
                        {
                            stackSizeToSpawn = Math.Max( 1, totalToSpawn / remainingStacks );
                            totalToSpawn -= stackSizeToSpawn;
                            remainingStacks--;
                        }
                        else
                        {
                            stackSizeToSpawn = 1;
                            totalToSpawn--;
                        }

                        Trace.Orders(centerpieceSquad)?.Msg( "Spawning a stack of {0} {1} (rem: {2}).", stackSizeToSpawn, currentType.InternalName, totalToSpawn );

                        string createReason;
                        if (Reason == DeployReason.DyingCenterpiece)
                            createReason = "DeployDrones_DyingCenterpiece";
                        else
                            createReason = "DeployDrones_ThreatDetected";

                        var spawnAt = centerpieceSquad.WorldLocation;
                        
                        // idea: maybe do better than just spawning them all at the centerpoint?
                        /*
                        var radius = centerpieceSquad.GetRadius();
                        int min = (int)(radius * 0.9f);
                        int max = (int)(radius * 1.1f);
                        spawnAt = spawnAt.GetRandomPointWithinDistance(Context.RandomToUse, min, max);
                        */
                        GameEntity_Squad drone = faction.SpawnNewUnit_ReturnNullIfMPClient( 
                                                                        Context, centerpieceSquad.Planet, 
                                                                        spawnAt, mem.TypeData, mem.EffectiveMark, Fleet,
                                                                        mem.UniqueTypeDataDifferentiatorForDuplicates,
                                                                        centerpieceSquad.TypeData.DroneStartingBehaviorType, 
                                                                        centerpieceSquad.Orders.BehaviorRelatedFactionIndex, centerpieceSquad, createReason );
                            
                        drone.AddOrSetExtraStackedSquadsInThis( (short)(stackSizeToSpawn-1), true );
                        //drone.ParentGameEntity.SetInternalRef( centerpieceSquad );

                        // the hells the point of DroneStartingBehaviorType then??
                        drone.GetEffectiveOrders().SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                        
                        mem.AddOrSetNumberCreatedButNotDeployed( (short)-stackSizeToSpawn, false );
                    }
                    
                }
            
            // Note this call occurs continuously, while threat is present.
            // As such, we can also invalidate previously issued LoadIntoTransport
            // orders, that are now invalid for drones already deployed
            // but inappropriately on their way back.
            //
            // For normal, non-drone ships this is handled elsewhere by the order itself
            // which knows it is invalid and removes itself--since normal ships only
            // can validly be order to load when the fleet itself is in loading mode.
            //
            // Drones however can validly be recalled and load into their transport
            // despite that transport not being in loading mode, since drones are
            // auto-recalled when there is nothing threatening present.
            //
            // So, we handle canceling inappropriate recall/load orders to living drones right here.
            
            foreach ( FleetMembership mem in Fleet.MemberGroupsUnsorted_Sim )
            {
                    if (mem.TypeData.IsDrone == false)
                        continue;
                    
                    foreach ( GameEntity_Squad e in mem.Entities )
                    {
                                if (e.Orders.GetHasAnyOrdersOfType(EntityOrderType.GetIntoTransport))
                                {
                                    e.Orders.ClearOrders(ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.YesClearAnyOrders_IncludingFromHumans, "DroneRecallAborted");
                                    Trace.Orders(centerpieceSquad)?.Msg("Deployed drone {0} was being recalled to {1} but now redeployed.", e.ToString(), centerpieceSquad.ToString());
                                }
                    }
                    
                }
        }
        
        public override void RecallDeployedDrones( Fleet Fleet, ArcenHostOnlySimContext Context, RecallReason reason )
        {
            if ( Context == null )
                return;

            GameEntity_Squad centerpieceSquad = Fleet.Centerpiece.GetSquadAndIgnoreAnyPermaNull();
            if ( centerpieceSquad == null )
            {
                //Trace.Orders(ship)?.Msg(ship)?.Msg( "But this fleets centerpiece squad appears to be null." );
                return;
            }
            
            Trace.Orders(centerpieceSquad)?.Msg( "Asked to RecallDeployedDrones for Fleet {0} because {1}.", Fleet.GetName(), reason );
            
            //Fleet.IsDroneThreatPresent = false;
            
            PerFactionPathCache facPathCache = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                foreach ( FleetMembership mem in Fleet.MemberGroupsUnsorted_Sim )
                {
                    if (mem.TypeData.IsDrone == false)
                        continue;
                    
                    foreach ( GameEntity_Squad e in mem.Entities )
                    {
                                var order = e.Orders.GetQueuedOrderAtIndex_OrNull(0);
                                
                                if (order.TypeData != null && 
                                    order.TypeData.Type == EntityOrderType.GetIntoTransport)
                                {
                                    continue;    
                                }
                                
                                e.Orders.ClearOrders(ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.YesClearAnyOrders_IncludingFromHumans, "DroneBeingRecalled");
                                e.Orders.QueueOrder(e, EntityOrder.Create_GetIntoTransport(centerpieceSquad.PrimaryKeyID, false, "RecallDeployedDrones", OrderSource.Other, false));
                                
                                Trace.Orders(centerpieceSquad)?.Msg("Sending drone {0} to load into {1}.", e.ToString(), centerpieceSquad.ToString());
                    }
                    
                }
            }
            catch (Exception e)
            {
                LOG.Err("Exception in RecallDeployedDrones\n{0}", e);
            }
            finally
            {
                facPathCache.ReturnToPool();
            }
        }
    }

    public static class FleetMembershipExtensions {
        public static FleetMembership GetBaseMembershipForBuildPoints(this FleetMembership targetMembership)
        {
            Fleet fleet = targetMembership.Fleet;
            GameEntityTypeData baseType =  targetMembership?.TypeData.BaseShipLineForBuildPoints_TypeData;
            return fleet.GetButDoNotAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates(baseType, targetMembership.UniqueTypeDataDifferentiatorForDuplicates);
        }
        public static FleetMembership GetOrAddTargetMembershipForBuildPoints(this FleetMembership targetMembership)
        {
            Fleet fleet = targetMembership.Fleet;
            GameEntityTypeData targetType =  targetMembership?.TypeData.UnitToMakeWithBuildPoints_TypeData;
            return fleet.GetOrAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates(targetType, targetMembership.UniqueTypeDataDifferentiatorForDuplicates);
        }
    }
    
    public static partial class FleetExtensions
    {
        public static void DeployDroneContents( this Fleet Fleet, ArcenHostOnlySimContext Context, DeployReason Reason )
        {
            FleetSimLogic.Instance.DeployDroneContents(Fleet, Context, Reason);
        }
        
        public static void RecallDeployedDrones( this Fleet Fleet, ArcenHostOnlySimContext Context, RecallReason Reason )
        {
            FleetSimLogic.Instance.RecallDeployedDrones(Fleet, Context, Reason);
        }
    }
}
