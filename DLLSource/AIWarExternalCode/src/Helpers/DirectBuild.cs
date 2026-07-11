using System;
using Arcen.AIW2.Core;
using UnityEngine;
using Arcen.Universal;


namespace Arcen.AIW2.External
{
    #region DirectBuildHelpers
    public static class DirectBuildHelpers 
    {
        public static GameEntity_Squad GetMobileBuilderForPlanet(PlanetFaction localFaction)
        {
            GameEntity_Squad builder = null;
            GameEntity_Squad crippledBuilder = null;
            foreach ( GameEntity_Squad entity in localFaction.Entities.Squads( EntityRollupType.MobileFleetFlagships ) ) {
                if ( entity.PlanetFaction != localFaction ) {
                    continue;
                }
                if ( crippledBuilder != null) {
                    crippledBuilder = entity;
                }
                if ( entity.GetIsCrippled() || entity.GetIsNonFunctional() ) {
                    continue;
                }
                if ( entity.HasNotYetBeenFullyClaimed /*|| entity.IsInHoldFireMode*/ ) {
                    continue;
                }
                builder = entity;
                break;
            }
            return builder ?? crippledBuilder;
        }
        public static int GetAmountToPlace( this DirectBuildMode mode, int SquadCap )
        {
            switch (mode) {
                case DirectBuildMode.HalfUnits:
                    return SquadCap / 2;
                case DirectBuildMode.ThirdUnits:
                    return SquadCap / 3;
                case DirectBuildMode.x5Units:
                    return 5;
                case DirectBuildMode.x10Units:
                    return 10;
                case DirectBuildMode.x50Units:
                    return 50;
                case DirectBuildMode.OneUnit:
                    return 1;
                default:
                    throw new NotImplementedException( "Error: Unimplemented Direct Build Mode: " + mode + "!" );
            }
        }
        public static bool ShouldShownInCampaign( this GameEntityTypeData type )
        {
            if (type.GetHasTag("If_Fuel_Enabled") && !World_AIW2.Instance.IsFuelEnabled)
                return false;
            
            return true;
        }
    }
    #endregion

    #region DirectBuild_FleetMembership
    class DirectBuild_FleetMembership : IDirectBuildableImplementation {
        static IDirectBuildableImplementation Implementation;
        public static DirectBuildable Create(FleetMembership fleetMembership, Faction localFaction)
        {
            if (fleetMembership == null)
                return DirectBuildable.CreateBlank();

            if ( !fleetMembership.ForMark.GetMetalFlow( MetalFlowPurpose.SelfConstruction ).HasRealData )
                return DirectBuildable.CreateBlank();

            // TODO: Should this be handled by Window_InGameSidebarDirectBuild instead?
            GameEntity_Squad builder = fleetMembership.Fleet.Centerpiece.GetSquad();
            if ( builder != null && 
                 builder.TypeData.SpecialType == SpecialEntityType.ThirdPartySellerToPlayers ) 
            {
                PlanetFaction planetFaction = builder.PlanetFaction.Planet.Factions[localFaction.FactionIndex];
                return DirectBuild_ThirdPartySellerToPlayers.Create(fleetMembership, planetFaction.FleetUsedAtPlanet);
            }

            InitIfNecessary();

            DirectBuildable buildable = new DirectBuildable {} ;
            buildable.fleetMembership_ForImplementation = fleetMembership;
            buildable.typeData_ForImplementation = fleetMembership.TypeData;
            buildable.Implementation = DirectBuild_FleetMembership.Implementation;
            return buildable;
        }

        private DirectBuild_FleetMembership() {}
        private static void InitIfNecessary() {
            if (DirectBuild_FleetMembership.Implementation == null) {
                DirectBuild_FleetMembership.Implementation = new DirectBuild_FleetMembership();
            }
        }

        public GameEntity_Squad GetBuilder(ref DirectBuildable buildable) { return buildable.fleetMembership_ForImplementation.Fleet.Centerpiece.GetSquad(); }
        public byte GetEffectiveMark(ref DirectBuildable buildable) { return buildable.fleetMembership_ForImplementation.EffectiveMark; }

        public FleetCategory GetFleetCategory(ref DirectBuildable buildable) { return buildable.fleetMembership_ForImplementation.Fleet.Category; }
        public string GetFleetName(ref DirectBuildable buildable) { return buildable.fleetMembership_ForImplementation.Fleet.GetName(); }

        public int GetBuildCap(ref DirectBuildable buildable) { return buildable.fleetMembership_ForImplementation.EffectiveSquadCap; }
        public int GetCurrentlyBuilt(ref DirectBuildable buildable) { return buildable.fleetMembership_ForImplementation.GetCountPresent( true, ExtraFromStacks.IncludePrecalc ); }

        public int GetMetalCost(ref DirectBuildable buildable) {
            Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
            return buildable.fleetMembership_ForImplementation.GetMetalCostForPlanet(planet);
        }

        public void AddFleetDescriptor( ref DirectBuildable buildable, ArcenCharacterBufferBase buffer )
        {
            FleetMembership fleetMembership = buildable.fleetMembership_ForImplementation;
            switch ( fleetMembership.Fleet.Category )
            {
                case FleetCategory.PlayerPlanetaryCommand:
                    buffer.StartColor( "9DFBA7" ).Add( "星球 " ).Add( fleetMembership.Fleet.Planet.Name );
                    break;
                case FleetCategory.PlayerBattlestation:
                    buffer.StartColor( "FADE9D" ).Add( fleetMembership.Fleet.GetName() );
                    break;
                default:
                  int strLen = fleetMembership.Fleet.GetName().Length;
                  if ( strLen > 25)
                      buffer.Add("<size=70%>");
                  else if (strLen > 18)
                      buffer.Add("<size=80%>");
                  buffer.StartColor("FA9DF2").Add(fleetMembership.Fleet.GetName());
                  if ( strLen >= 18 )
                        buffer.Add("</size>");
                  break;
            }
        }
        public bool AddOverridingCapText(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            if ( IsPreventedFromBuildingByDires(ref buildable) ) {
                buffer.Add( "<size=85%>" ).Add("被远程哨站阻挡", Color.red).Add("</size>");
                return true;
            }
            return false;
        }
        public void GetHoverText(ref DirectBuildable buildable, ArcenDoubleCharacterBuffer buffer)
        {
            if ( IsPreventedFromBuildingByDires(ref buildable) ) {
                buffer.StartColor(Color.red);
                buffer.Add( "末日模式：此星球上的远程守卫哨站阻止了所有炮台建造。" );
                buffer.EndColor().NewLine();
            }
            EntityText.GetTooltip( buffer, null, buildable.fleetMembership_ForImplementation, null, -1, null, 0, FromSidebarType.Sidebar_SingleUnit, ShipExtraDetailFlags.BuildInfo, 1f, false );
        }

        bool IsPreventedFromBuildingByDires(ref DirectBuildable buildable)
        {
            if ( buildable.TypeData.IsTurret && World_AIW2.Instance.Setup.GetBoolBySetting("DireGuardPostsPreventTurrets") ) {
                Planet currentPlanet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                if ( currentPlanet != null ) {
                    bool hadAnyDires = false;
                    foreach ( GameEntity_Squad squad in currentPlanet.Squads( SpecialEntityType.DireGuardPost ) )
                    {
                        if (squad.GetIsHostileToLocalFaction_Safe()) {
                            hadAnyDires = true;
                            break;
                        }
                    }
                    return hadAnyDires;
                }
            }
            return false;
        }

        public MouseHandlingResult HandleButtonClick(ref DirectBuildable buildable, MouseHandlingInput input) 
        {
            if ( IsPreventedFromBuildingByDires(ref buildable) ) {
                World_AIW2.Instance.QueueChatMessageOrCommand( "末日模式：此星球上的远程守卫哨站阻止了所有炮台建造。", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return MouseHandlingResult.PlayClickDeniedSound;
            }

            if (input.LeftButtonDoubleClicked || input.RightButtonClicked)
            {
                // this is similar to the HandleAutobuild method

                var mem = buildable.fleetMembership_ForImplementation;
                var currentCountToBuild = mem.EffectiveSquadCap;
                var planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                var commandStation = planet.GetCommandStationOrNull();
                if (commandStation != null && commandStation.GetFactionOrNull_Safe().Type == FactionType.AI)
                    commandStation = null;

                var builder = GetBuilder(ref buildable);
                if (builder == null)
                    return MouseHandlingResult.None;

                if ( mem.GetCanBuildAnother( true, currentCountToBuild, ExtraFromStacks.Recalculate ) != ArcenRejectionReason.Unknown )
                    return MouseHandlingResult.None;

                var cmdType = GameCommandTypeTable.Instance.GetRowByName("AutoPlaceBuildable");
                var cmd = GameCommand.Create(cmdType, GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer);
                
                var fac = mem.Fleet.Faction;
                if (fac == null)
                    return MouseHandlingResult.None;
                
                cmd.RelatedFactionIndex = mem.Fleet.Faction?.FactionIndex ?? -1;
                cmd.RelatedIntegers.Add(planet.Index);
                cmd.RelatedIntegers.Add(builder.PrimaryKeyID);
                cmd.RelatedIntegers.Add(commandStation?.PrimaryKeyID ?? -1);
                cmd.RelatedIntegers.Add(mem.Fleet.FleetID);
                cmd.RelatedIntegers.Add(mem.UniqueTypeDataDifferentiatorForDuplicates);
                cmd.RelatedString = mem.TypeData.InternalName;
                
                World_AIW2.Instance.QueueGameCommand( fac, cmd, true );
                
                Engine_AIW2.Instance.PlacingDirectBuildable = DirectBuildable.CreateBlank();
                
                return MouseHandlingResult.None;
            }

            Engine_AIW2.Instance.PlacingDirectBuildable = buildable;

            return MouseHandlingResult.None;
        }
        public GameCommand CreateBuildCommand(ref DirectBuildable buildable, GameCommandSource source, ArcenPoint worldLocation, DirectBuildMode buildMode)
        {
            FleetMembership fleetMembership = buildable.fleetMembership_ForImplementation;
            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.PlaceSelfBuildingUnit], source );
            command.RelatedString2 = fleetMembership.TypeData.InternalName;
            command.RelatedIntegers.Add( fleetMembership.Fleet.FleetID );
            command.RelatedPoints.Add( worldLocation );
            command.RelatedEntityIDs.Add( fleetMembership.Fleet.Centerpiece.GetPrimaryKeyID() );
            command.RelatedMagnitude = buildMode.GetAmountToPlace(fleetMembership.EffectiveSquadCap);
            return command;
        }
    }
    #endregion

    #region DirectBuild_CommandStation
    class DirectBuild_CommandStation : IDirectBuildableImplementation, ICustomBuildSidebarCategoryInfo {
        bool ICustomBuildSidebarCategoryInfo.GetShouldShow(Planet planet, Faction localFaction) {
            GameEntity_Squad commandStationOfPlanet = planet.GetCommandStationOrNull();
            return commandStationOfPlanet == null;
        }
        DirectBuildable ICustomBuildSidebarCategoryInfo.CreateDirectBuildable(Planet planet, Faction faction, GameEntityTypeData TypeData)
        {
            if (!TypeData.ShouldShownInCampaign())
                return new DirectBuildable();
            
            Fleet FleetUsedAtPlanet = planet.Factions[faction.FactionIndex].FleetUsedAtPlanet;
            DirectBuildable buildable = new DirectBuildable();
            buildable.typeData_ForImplementation = TypeData;
            buildable.fleetToBuild_ForImplementation = FleetUsedAtPlanet;
            buildable.Implementation = this;
            return buildable;
        }

        public GameEntity_Squad GetBuilder(ref DirectBuildable buildable)
        {
            Fleet fleetUsedAtPlanet = buildable.fleetToBuild_ForImplementation;
            PlanetFaction localFaction = fleetUsedAtPlanet.Planet.Factions[fleetUsedAtPlanet.Faction.FactionIndex];
            return DirectBuildHelpers.GetMobileBuilderForPlanet(localFaction);
        }
        public byte GetEffectiveMark(ref DirectBuildable buildable) {
            byte localFactionMark;
            return buildable.fleetToBuild_ForImplementation.CalculateMarkLevelForShipLine( buildable.TypeData, null, out localFactionMark );
        }

        public FleetCategory GetFleetCategory(ref DirectBuildable buildable) { return FleetCategory.PlayerPlanetaryCommand; }
        public string GetFleetName(ref DirectBuildable buildable) { return buildable.fleetToBuild_ForImplementation.GetName(); }

        public int GetBuildCap(ref DirectBuildable buildable) { return 0; }
        public int GetCurrentlyBuilt(ref DirectBuildable buildable) { return 0; }

        public int GetMetalCost(ref DirectBuildable buildable) { return buildable.typeData_ForImplementation.MarkStatsFor( buildable.EffectiveMark ).MetalCost; }

        public void AddFleetDescriptor(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            buffer.StartColor( "9DFBA7" ).Add( "星球 " ).Add( buildable.fleetToBuild_ForImplementation.Planet.Name );
        }
        public bool AddOverridingCapText(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            Planet planet = buildable.fleetToBuild_ForImplementation.Planet;
            if ( planet != null && planet.GetIsPlayerCommandStationPinned() && planet.MostRecentPlayerNonHomCommandStationType != buildable.TypeData ) {
                buffer.Add("被压制", Color.red);
                return true;
            }
            return false;
        }
        public void GetHoverText(ref DirectBuildable buildable, ArcenDoubleCharacterBuffer buffer)
        {
            Planet planet = buildable.fleetToBuild_ForImplementation.Planet;
            if ( planet != null && planet.GetIsPlayerCommandStationPinned() && planet.MostRecentPlayerNonHomCommandStationType != buildable.TypeData ) {
                buffer.StartColor(Color.red);
                buffer.Add("目前，你只能建造一个 ").Add(planet.MostRecentPlayerNonHomCommandStationType.DisplayName);
                buffer.Add("。此星球当前被来袭的波次或星球上1+的敌方力量压制。");
                buffer.EndColor().NewLine();
            }
            EntityText.GetTooltip( buffer, null, null, buildable.TypeData, -1, null, buildable.EffectiveMark, FromSidebarType.Sidebar_SingleUnit, ShipExtraDetailFlags.BuildInfo, 1f, false );
        }

        public MouseHandlingResult HandleButtonClick(ref DirectBuildable buildable, MouseHandlingInput input) {
            Planet planet = buildable.fleetToBuild_ForImplementation.Planet;
            if ( planet != null && planet.GetIsPlayerCommandStationPinned() && planet.MostRecentPlayerNonHomCommandStationType != buildable.TypeData )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "目前，你只能建造一个 " + planet.MostRecentPlayerNonHomCommandStationType.DisplayName +
                        "。此星球当前被来袭的波次或星球上1+的敌方力量压制。", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return MouseHandlingResult.PlayClickDeniedSound;
            }
            Engine_AIW2.Instance.PlacingDirectBuildable = buildable;
            return MouseHandlingResult.None;
        }
        public GameCommand CreateBuildCommand(ref DirectBuildable buildable, GameCommandSource source, ArcenPoint worldLocation, DirectBuildMode buildMode)
        {
            Fleet fleetUsedAtPlanet = buildable.fleetToBuild_ForImplementation;
            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.PlaceSelfBuildingUnit], source );
            command.RelatedString2 = buildable.typeData_ForImplementation.InternalName;
            command.RelatedIntegers.Add( fleetUsedAtPlanet.FleetID );
            command.RelatedEntityIDs.Add( this.GetBuilder(ref buildable).PrimaryKeyID );
            // We can build one command station
            command.RelatedIntegers2.Add( 1 );
            command.RelatedPoints.Add( worldLocation );
            command.RelatedMagnitude = 1;
            return command;
        }
    }
    #endregion

    #region DirectBuild_CommandStationUpgrade
    class DirectBuild_CommandStationUpgrade : IDirectBuildableImplementation, ICustomBuildSidebarCategoryInfo {
        bool ICustomBuildSidebarCategoryInfo.GetShouldShow(Planet planet, Faction localFaction) {
            return true;
        }
        DirectBuildable ICustomBuildSidebarCategoryInfo.CreateDirectBuildable(Planet planet, Faction faction, GameEntityTypeData TypeData)
        {
            if (!TypeData.ShouldShownInCampaign())
                return new DirectBuildable();
            
            Fleet FleetUsedAtPlanet = planet.Factions[faction.FactionIndex].FleetUsedAtPlanet;
            if (FleetUsedAtPlanet.Centerpiece.GetSquad()?.TypeData == TypeData) {
                return new DirectBuildable();
            }
            DirectBuildable buildable = new DirectBuildable();
            buildable.typeData_ForImplementation = TypeData;
            buildable.fleetToBuild_ForImplementation = FleetUsedAtPlanet;
            buildable.Implementation = this;
            return buildable;
        }

        public GameEntity_Squad GetBuilder(ref DirectBuildable buildable)
        {
            Fleet fleetUsedAtPlanet = buildable.fleetToBuild_ForImplementation;
            return fleetUsedAtPlanet.Centerpiece.GetSquad();
        }
        public byte GetEffectiveMark(ref DirectBuildable buildable) {
            byte localFactionMark;
            return buildable.fleetToBuild_ForImplementation.CalculateMarkLevelForShipLine( buildable.TypeData, null, out localFactionMark );
        }

        public FleetCategory GetFleetCategory(ref DirectBuildable buildable) { return FleetCategory.PlayerPlanetaryCommand; }
        public string GetFleetName(ref DirectBuildable buildable) { return buildable.fleetToBuild_ForImplementation.GetName(); }

        public int GetBuildCap(ref DirectBuildable buildable) { return 0; }
        public int GetCurrentlyBuilt(ref DirectBuildable buildable) { return 0; }

        public int GetMetalCost(ref DirectBuildable buildable) { return buildable.typeData_ForImplementation.MarkStatsFor( buildable.EffectiveMark ).MetalCost; }

        public void AddFleetDescriptor(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            buffer.StartColor( "9DFBA7" ).Add( "星球 " ).Add( buildable.fleetToBuild_ForImplementation.Planet.Name );
        }
        public bool AddOverridingCapText(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            Planet planet = buildable.fleetToBuild_ForImplementation.Planet;
            if ( planet != null && planet.GetIsPlayerCommandStationPinned() && planet.MostRecentPlayerNonHomCommandStationType != buildable.TypeData ) {
                buffer.Add("被压制", Color.red);
                return true;
            }
            return false;
        }

        public const string PinnedCommandStationText =
            "无法在此处建造替代指挥站。现有的指挥站当前被来袭的波次或星球上1+的敌方力量压制。";

        public void GetHoverText(ref DirectBuildable buildable, ArcenDoubleCharacterBuffer buffer)
        {
            Planet planet = buildable.fleetToBuild_ForImplementation.Planet;
            if ( planet != null && planet.GetIsPlayerCommandStationPinned() && planet.MostRecentPlayerNonHomCommandStationType != buildable.TypeData ) {
                buffer.Add(PinnedCommandStationText, Color.red).NewLine();
            }
            EntityText.GetTooltip( buffer, null, null, buildable.TypeData, -1, null, buildable.EffectiveMark, FromSidebarType.Sidebar_SingleUnit, ShipExtraDetailFlags.BuildInfo, 1f, false );
        }

        public MouseHandlingResult HandleButtonClick(ref DirectBuildable buildable, MouseHandlingInput input) {
            GameEntity_Squad currentCommandStation = buildable.fleetToBuild_ForImplementation.Centerpiece.GetSquad();
            Planet planet = buildable.fleetToBuild_ForImplementation.Planet;

            if (currentCommandStation == null) {
                ArcenDebugging.ArcenDebugLogSingleLine( $"Command station disappeared before we could tranform it on planet {planet}.", Verbosity.Chat);
            }

            if ( currentCommandStation.TypeData.SpecialType == SpecialEntityType.HumanHomeCommand ) {
                World_AIW2.Instance.QueueChatMessageOrCommand( "无法在你的主指挥站位置建造新的指挥站！", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return MouseHandlingResult.PlayClickDeniedSound;
            }
            if ( planet.GetIsPlayerCommandStationPinned() ) {
                World_AIW2.Instance.QueueChatMessageOrCommand( PinnedCommandStationText, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return MouseHandlingResult.PlayClickDeniedSound;
            }

            //transform in place into another unit type
            Fleet fleetUsedAtPlanet = buildable.fleetToBuild_ForImplementation;
            GameEntityTypeData typeData = buildable.TypeData;
            ModalPopupData.CreateAndLogYesNoStyle(delegate { DoTransform(fleetUsedAtPlanet, typeData); }, null, "你确定吗",
                    "你确定要将你当前的 " + currentCommandStation.TypeData.DisplayName + " 变形为 " + buildable.TypeData.DisplayName
                    + "？它将以几乎为零的生命值开始，并且在 " + ExternalConstants.Instance.Balance_RepairImpossibleForSecondsAfterTransformingCommandStation + " 秒内无法修复。", "是的，变形", "否");
            return MouseHandlingResult.None;
        }

        void DoTransform(Fleet fleetUsedAtPlanet, GameEntityTypeData typeData)
        {
            GameEntity_Squad currentCommandStation = fleetUsedAtPlanet.Centerpiece.GetSquad();
            if (currentCommandStation == null) {
                ArcenDebugging.ArcenDebugLogSingleLine( $"Command station disappeared before we could tranform it on planet {fleetUsedAtPlanet.Planet}.", Verbosity.Chat);
            }
            GameCommand command = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransformUnits], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer);
            command.RelatedEntityIDs.Add(currentCommandStation.PrimaryKeyID);
            command.RelatedString2 = typeData.InternalName;
            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true);
        }

        public GameCommand CreateBuildCommand(ref DirectBuildable buildable, GameCommandSource source, ArcenPoint worldLocation, DirectBuildMode buildMode)
        {
            ArcenDebugging.ArcenDebugLogSingleLine( "(BUG) Tried to place a command station upgrade, instead of triggering a transform.", Verbosity.Chat);
            return null;
        }
    }
    #endregion

    #region DirectBuild_ThirdPartySellerToPlayers
    class DirectBuild_ThirdPartySellerToPlayers : IDirectBuildableImplementation {
        static IDirectBuildableImplementation Implementation;
        public static DirectBuildable Create(FleetMembership fleetMembership, Fleet fleetToBuild)
        {
            InitIfNecessary();
            DirectBuildable buildable = new DirectBuildable();
            buildable.fleetMembership_ForImplementation = fleetMembership;
            buildable.fleetToBuild_ForImplementation = fleetToBuild;
            buildable.typeData_ForImplementation = fleetMembership.TypeData;
            buildable.Implementation = DirectBuild_ThirdPartySellerToPlayers.Implementation;
            return buildable;
        }

        private DirectBuild_ThirdPartySellerToPlayers() {}
        private static void InitIfNecessary() {
            if (DirectBuild_ThirdPartySellerToPlayers.Implementation == null) {
                DirectBuild_ThirdPartySellerToPlayers.Implementation = new DirectBuild_ThirdPartySellerToPlayers();
            }
        }

        public GameEntity_Squad GetBuilder(ref DirectBuildable buildable) { return buildable.fleetToBuild_ForImplementation.Centerpiece.GetSquad(); }
        public byte GetEffectiveMark(ref DirectBuildable buildable) {
            byte localFactionMark;
            return buildable.fleetToBuild_ForImplementation.CalculateMarkLevelForShipLine( buildable.TypeData, null, out localFactionMark );
        }
        public FleetCategory GetFleetCategory(ref DirectBuildable buildable) { return buildable.fleetMembership_ForImplementation.Fleet.Category; }
        public string GetFleetName(ref DirectBuildable buildable) { return buildable.fleetMembership_ForImplementation.Fleet.GetName(); }

        public int GetBuildCap(ref DirectBuildable buildable) { return buildable.fleetMembership_ForImplementation.EffectiveSquadCap; }
        public int GetCurrentlyBuilt(ref DirectBuildable buildable) { return buildable.fleetMembership_ForImplementation.GetCountPresent( true, ExtraFromStacks.IncludePrecalc ); }

        public int GetMetalCost(ref DirectBuildable buildable) {
            Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
            return buildable.fleetMembership_ForImplementation.GetMetalCostForPlanet(planet);
        }

        public void AddFleetDescriptor(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            buffer.StartColor( "FA9DF2" ).Add( buildable.fleetMembership_ForImplementation.Fleet.GetName() );
        }
        public bool AddOverridingCapText(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            return false;
        }
        public void GetHoverText(ref DirectBuildable buildable, ArcenDoubleCharacterBuffer buffer)
        {
            EntityText.GetTooltip( buffer, null, buildable.fleetMembership_ForImplementation, null, -1, null, 0, FromSidebarType.Sidebar_SingleUnit, ShipExtraDetailFlags.BuildInfo, 1f, false );
        }

        public MouseHandlingResult HandleButtonClick(ref DirectBuildable buildable, MouseHandlingInput input) {
            Engine_AIW2.Instance.PlacingDirectBuildable = buildable;
            return MouseHandlingResult.None;
        }
        public GameCommand CreateBuildCommand(ref DirectBuildable buildable, GameCommandSource source, ArcenPoint worldLocation, DirectBuildMode buildMode)
        {
            FleetMembership fleetMembership = buildable.fleetMembership_ForImplementation;
            Fleet fleetToBuild = buildable.fleetToBuild_ForImplementation;
            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.PlaceSelfBuildingUnit], source );
            command.RelatedString2 = fleetMembership.TypeData.InternalName;
            command.RelatedIntegers.Add( fleetToBuild.FleetID );
            command.RelatedEntityIDs.Add( fleetToBuild.Centerpiece.GetPrimaryKeyID() );
            //we are sending this for cases like the Zenith Trader to send the squad cap across in case we need to create a FleetMembership during this purchase
            command.RelatedIntegers2.Add( fleetMembership.EffectiveSquadCap );
            command.RelatedPoints.Add( worldLocation );
            command.RelatedMagnitude = buildMode.GetAmountToPlace(fleetMembership.EffectiveSquadCap);
            return command;
        }
    }
    #endregion

    #region DirectBuild_Necropolis
    class DirectBuild_Necropolis : IDirectBuildableImplementation, ICustomBuildSidebarCategoryInfo  {
        public bool GetShouldShow(Planet planet, Faction faction) {
            if ( planet.GetControllingFaction().GetIsHostileTowards(faction) ) {
                return false;
            }
            NecromancerEmpireFactionBaseInfo baseInfo = faction.TryGetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
            if (baseInfo == null) {
                return false;
            }
            if (baseInfo.DoesPlanetHaveNecropolis(planet)) {
                return false;
            }
            return true;
        }
        public DirectBuildable CreateDirectBuildable(Planet planet, Faction faction, GameEntityTypeData TypeData)
        {
            if (!TypeData.ShouldShownInCampaign())
                return new DirectBuildable();
            
            DirectBuildable buildable = new DirectBuildable();
            buildable.typeData_ForImplementation = TypeData;
            // Use FleetUsedPlanet as an awkward way to record the planet being built on.
            buildable.fleetToBuild_ForImplementation = planet.Factions[faction.FactionIndex].FleetUsedAtPlanet;
            buildable.Implementation = this;
            return buildable;
        }

        public GameEntity_Squad GetBuilder(ref DirectBuildable buildable)
        {
            Fleet fleetUsedAtPlanet = buildable.fleetToBuild_ForImplementation;
            PlanetFaction localFaction = fleetUsedAtPlanet.Planet.Factions[fleetUsedAtPlanet.Faction.FactionIndex];
            return DirectBuildHelpers.GetMobileBuilderForPlanet(localFaction);
        }
        public byte GetEffectiveMark(ref DirectBuildable buildable) { return 1; }

        public FleetCategory GetFleetCategory(ref DirectBuildable buildable) { return FleetCategory.PlayerCustomCity; }
        public string GetFleetName(ref DirectBuildable buildable) { return ""; }

        public int GetBuildCap(ref DirectBuildable buildable) { return 0; }
        public int GetCurrentlyBuilt(ref DirectBuildable buildable) { return 0; }

        public int GetMetalCost(ref DirectBuildable buildable) { return buildable.typeData_ForImplementation.MarkStatsFor( buildable.EffectiveMark ).MetalCost; }

        public void AddFleetDescriptor(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            buffer.StartColor( "9DFBA7" ).Add( "星球 " ).Add( buildable.fleetToBuild_ForImplementation.Planet.Name );
        }
        public bool AddOverridingCapText(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            return false;
        }
        public void GetHoverText(ref DirectBuildable buildable, ArcenDoubleCharacterBuffer buffer)
        {
            EntityText.GetTooltip( buffer, null, null, buildable.TypeData, -1, null, buildable.EffectiveMark, FromSidebarType.Sidebar_SingleUnit, ShipExtraDetailFlags.BuildInfo, 1f, false );
        }

        public MouseHandlingResult HandleButtonClick(ref DirectBuildable buildable, MouseHandlingInput input) {
            Engine_AIW2.Instance.PlacingDirectBuildable = buildable;
            return MouseHandlingResult.None;
        }
        public GameCommand CreateBuildCommand(ref DirectBuildable buildable, GameCommandSource source, ArcenPoint worldLocation, DirectBuildMode buildMode)
        {
            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.BuildNecropolis], source );
            command.RelatedString2 = buildable.typeData_ForImplementation.InternalName;
            command.RelatedEntityIDs.Add( this.GetBuilder(ref buildable).PrimaryKeyID );
            command.RelatedPoints.Add( worldLocation );
            return command;
        }
    }
    #endregion

    #region DirectBuild_SpireCity
    class DirectBuild_SpireCity : IDirectBuildableImplementation, ICustomBuildSidebarCategoryInfo  {
        public bool GetShouldShow(Planet planet, Faction faction) {
            FallenSpireFactionBaseInfo baseInfo = faction.TryGetExternalBaseInfoAs<FallenSpireFactionBaseInfo>();
            if (baseInfo == null) {
                return false;
            }

            SpireCityBlockingReason reason = baseInfo.IsPlanetAllowedToBuildCity( planet, null );
            if ( reason == SpireCityBlockingReason.OnSamePlanetAsOtherCity ) {
                return false;
            }
            return true;
        }
        public DirectBuildable CreateDirectBuildable(Planet planet, Faction faction, GameEntityTypeData TypeData)
        {
            if (!TypeData.ShouldShownInCampaign())
                return new DirectBuildable();
            
            var baseInfo = faction.TryGetExternalBaseInfoAs<FallenSpireFactionBaseInfo>();
            if (baseInfo == null)
                return new DirectBuildable();
            if (!baseInfo.ExpertMode && TypeData.GetHasTag("Spire_Expert"))
                return new DirectBuildable();
            if (baseInfo.ExpertMode && TypeData.GetHasTag("Spire_Normal"))
                return new DirectBuildable();

            DirectBuildable buildable = new DirectBuildable();
            buildable.typeData_ForImplementation = TypeData;
            buildable.Implementation = this;
            
            // Use FleetUsedPlanet as an awkward way to record the planet being built on.
            buildable.fleetToBuild_ForImplementation = planet.Factions[faction.FactionIndex].FleetUsedAtPlanet;
            
            return buildable;
        }

        public GameEntity_Squad GetBuilder(ref DirectBuildable buildable)
        {
            Fleet fleetUsedAtPlanet = buildable.fleetToBuild_ForImplementation;
            return fleetUsedAtPlanet.Centerpiece.GetSquad();
        }
        public byte GetEffectiveMark(ref DirectBuildable buildable) { return 1; }

        public FleetCategory GetFleetCategory(ref DirectBuildable buildable) { return FleetCategory.PlayerCustomCity; }
        public string GetFleetName(ref DirectBuildable buildable) { return ""; }

        public int GetBuildCap(ref DirectBuildable buildable) { return 0; }
        public int GetCurrentlyBuilt(ref DirectBuildable buildable) { return 0; }

        public int GetMetalCost(ref DirectBuildable buildable) { return buildable.typeData_ForImplementation.MarkStatsFor( buildable.EffectiveMark ).MetalCost; }

        public void AddFleetDescriptor(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            buffer.StartColor( "9DFBA7" ).Add( "星球 " ).Add( buildable.fleetToBuild_ForImplementation.Planet.Name );
        }
        public bool AddOverridingCapText(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            Planet planet = buildable.fleetToBuild_ForImplementation.Planet;
            Faction faction = buildable.fleetToBuild_ForImplementation.Faction;
            FallenSpireFactionBaseInfo baseInfo = faction.TryGetExternalBaseInfoAs<FallenSpireFactionBaseInfo>();
            if (baseInfo == null) {
                return false;
            }

            SpireCityBlockingReason reason = baseInfo.IsPlanetAllowedToBuildCity( planet, null );
            if ( reason != SpireCityBlockingReason.None ) {
                buffer.StartColor( Color.grey ).Add( "<size=85%>" ).Add(FallenSpireFactionBaseInfo.SpireCityBlockingReason_ToStringForUI( reason ) ).Add("</size>").EndColor();
                return true;
            }
            return false;
        }
        public void GetHoverText(ref DirectBuildable buildable, ArcenDoubleCharacterBuffer buffer)
        {
            EntityText.GetTooltip( buffer, null, null, buildable.TypeData, -1, null, buildable.EffectiveMark, FromSidebarType.Sidebar_SingleUnit, ShipExtraDetailFlags.BuildInfo, 1f, false );
        }

        public MouseHandlingResult HandleButtonClick(ref DirectBuildable buildable, MouseHandlingInput input) {
            Planet planet = buildable.fleetToBuild_ForImplementation.Planet;
            Faction faction = buildable.fleetToBuild_ForImplementation.Faction;
            FallenSpireFactionBaseInfo baseInfo = faction.TryGetExternalBaseInfoAs<FallenSpireFactionBaseInfo>();
            if (baseInfo == null) {
                return MouseHandlingResult.PlayClickDeniedSound;
            }

            SpireCityBlockingReason reason = baseInfo.IsPlanetAllowedToBuildCity( planet, null );
            if ( reason != SpireCityBlockingReason.None ) {
                World_AIW2.Instance.QueueChatMessageOrCommand(
                    "无法在此处建造尖塔城市：" + FallenSpireFactionBaseInfo.SpireCityBlockingReason_ToStringForUI( reason ),
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return MouseHandlingResult.PlayClickDeniedSound;
            }
            Engine_AIW2.Instance.PlacingDirectBuildable = buildable;
            return MouseHandlingResult.None;
        }
        public GameCommand CreateBuildCommand(ref DirectBuildable buildable, GameCommandSource source, ArcenPoint worldLocation, DirectBuildMode buildMode)
        {
            Fleet fleetUsedAtPlanet = buildable.fleetToBuild_ForImplementation;
            GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.PlaceSelfBuildingUnit], source );
            command.RelatedString2 = buildable.typeData_ForImplementation.InternalName;
            command.RelatedIntegers.Add( fleetUsedAtPlanet.FleetID );
            command.RelatedEntityIDs.Add( this.GetBuilder(ref buildable).PrimaryKeyID );
            // We can build one city
            command.RelatedIntegers2.Add( 1 );
            command.RelatedPoints.Add( worldLocation );
            command.RelatedMagnitude = 1;
            return command;
        }
    }
    #endregion

    #region DirectBuild_Stronghold
    class DirectBuild_Stronghold : IDirectBuildableImplementation, ICustomBuildSidebarCategoryInfo  {
        public bool GetShouldShow(Planet planet, Faction faction) {
            if ( planet.GetControllingFaction().GetIsHostileTowards(faction) ) {
                return false;
            }
            DysonSidekickFactionBaseInfo baseInfo = faction.TryGetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
            if (baseInfo == null) {
                return false;
            }
            if (baseInfo.DoesPlanetHaveStronghold(planet)) {
                return false;
            }
            if (baseInfo.DoesPlanetHaveSphere(planet)) {
                return false;
            }

            if ( planet.IsRavaged)
                return false;

            return true;
        }
        public DirectBuildable CreateDirectBuildable(Planet planet, Faction faction, GameEntityTypeData TypeData)
        {
            if (!TypeData.ShouldShownInCampaign())
                return new DirectBuildable();
            
            DirectBuildable buildable = new DirectBuildable();
            buildable.typeData_ForImplementation = TypeData;
            // Use FleetUsedPlanet as an awkward way to record the planet being built on.
            buildable.fleetToBuild_ForImplementation = planet.Factions[faction.FactionIndex].FleetUsedAtPlanet;
            buildable.Implementation = this;
            return buildable;
        }

        public GameEntity_Squad GetBuilder(ref DirectBuildable buildable)
        {
            Fleet fleetUsedAtPlanet = buildable.fleetToBuild_ForImplementation;
            PlanetFaction localFaction = fleetUsedAtPlanet.Planet.Factions[fleetUsedAtPlanet.Faction.FactionIndex];
            return DirectBuildHelpers.GetMobileBuilderForPlanet(localFaction);
        }
        public byte GetEffectiveMark(ref DirectBuildable buildable) { return 1; }

        public FleetCategory GetFleetCategory(ref DirectBuildable buildable) { return FleetCategory.PlayerCustomCity; }
        public string GetFleetName(ref DirectBuildable buildable) { return ""; }

        public int GetBuildCap(ref DirectBuildable buildable) { return 0; }
        public int GetCurrentlyBuilt(ref DirectBuildable buildable) { return 0; }

        public int GetMetalCost(ref DirectBuildable buildable) { return buildable.typeData_ForImplementation.MarkStatsFor( buildable.EffectiveMark ).MetalCost; }

        public void AddFleetDescriptor(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            buffer.StartColor( "9DFBA7" ).Add( "星球 " ).Add( buildable.fleetToBuild_ForImplementation.Planet.Name );
        }
        public bool AddOverridingCapText(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            return false;
        }
        public void GetHoverText(ref DirectBuildable buildable, ArcenDoubleCharacterBuffer buffer)
        {
            EntityText.GetTooltip( buffer, null, null, buildable.TypeData, -1, null, buildable.EffectiveMark, FromSidebarType.Sidebar_SingleUnit, ShipExtraDetailFlags.BuildInfo, 1f, false );
        }

        public MouseHandlingResult HandleButtonClick(ref DirectBuildable buildable, MouseHandlingInput input) {
            Engine_AIW2.Instance.PlacingDirectBuildable = buildable;
            return MouseHandlingResult.None;
        }
        public GameCommand CreateBuildCommand(ref DirectBuildable buildable, GameCommandSource source, ArcenPoint worldLocation, DirectBuildMode buildMode)
        {
            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.BuildStronghold], source );
            command.RelatedString2 = buildable.typeData_ForImplementation.InternalName;
            command.RelatedEntityIDs.Add( this.GetBuilder(ref buildable).PrimaryKeyID );
            command.RelatedPoints.Add( worldLocation );
            return command;
        }
    }
    #endregion
    
    #region DirectBuild_DysonDrill
    class DirectBuild_DysonDrill : IDirectBuildableImplementation, ICustomBuildSidebarCategoryInfo  {
        public bool GetShouldShow(Planet planet, Faction faction) {
            if ( FactionUtilityMethods.Instance.HasPlayerKing( planet ))
                return false;
            DysonSidekickFactionBaseInfo baseInfo = faction.TryGetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
            if (baseInfo.DoesPlanetHaveDrill(planet) ) {
                return false;
            }

            return true;
        }
        public DirectBuildable CreateDirectBuildable(Planet planet, Faction faction, GameEntityTypeData TypeData)
        {
            if (!TypeData.ShouldShownInCampaign())
                return new DirectBuildable();
            
            DirectBuildable buildable = new DirectBuildable();
            buildable.typeData_ForImplementation = TypeData;
            // Use FleetUsedPlanet as an awkward way to record the planet being built on.
            buildable.fleetToBuild_ForImplementation = planet.Factions[faction.FactionIndex].FleetUsedAtPlanet;
            buildable.Implementation = this;
            return buildable;
        }

        public GameEntity_Squad GetBuilder(ref DirectBuildable buildable)
        {
            Fleet fleetUsedAtPlanet = buildable.fleetToBuild_ForImplementation;
            PlanetFaction localFaction = fleetUsedAtPlanet.Planet.Factions[fleetUsedAtPlanet.Faction.FactionIndex];
            return DirectBuildHelpers.GetMobileBuilderForPlanet(localFaction);
        }
        public byte GetEffectiveMark(ref DirectBuildable buildable) { return 1; }

        public FleetCategory GetFleetCategory(ref DirectBuildable buildable) { return FleetCategory.PlayerCustomCity; }
        public string GetFleetName(ref DirectBuildable buildable) { return ""; }

        public int GetBuildCap(ref DirectBuildable buildable) { return 0; }
        public int GetCurrentlyBuilt(ref DirectBuildable buildable) { return 0; }

        public int GetMetalCost(ref DirectBuildable buildable) { return buildable.typeData_ForImplementation.MarkStatsFor( buildable.EffectiveMark ).MetalCost; }

        public void AddFleetDescriptor(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            buffer.StartColor( "9DFBA7" ).Add( "星球 " ).Add( buildable.fleetToBuild_ForImplementation.Planet.Name );
        }
        public bool AddOverridingCapText(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            return false;
        }
        public void GetHoverText(ref DirectBuildable buildable, ArcenDoubleCharacterBuffer buffer)
        {
            EntityText.GetTooltip( buffer, null, null, buildable.TypeData, -1, null, buildable.EffectiveMark, FromSidebarType.Sidebar_SingleUnit, ShipExtraDetailFlags.BuildInfo, 1f, false );
            DysonSidekickFactionBaseInfo baseInfo = buildable.fleetToBuild_ForImplementation.Faction.TryGetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
            if ( baseInfo != null )
            {
                if ( buildable.TypeData.GetHasTag( "DysonPlanetaryDrill" ) )
                    buffer.Add( "\n建造此建筑将产生 " ).Add( baseInfo.Difficulty.AIPForPlanetDrilling.ToString(), "ff0000" ).Add( " AIP,因为星球正在被钻探。" );
                else if ( buildable.TypeData.GetHasTag( "DysonOverloader" ) )
                    buffer.Add( "\n建造此建筑将产生 " ).Add( baseInfo.Difficulty.AIPForPlanetOverloading.ToString(), "ff0000" ).Add( " AIP,并在过载完成时摧毁星球。" );
            }
        }

        public MouseHandlingResult HandleButtonClick(ref DirectBuildable buildable, MouseHandlingInput input) {
            Engine_AIW2.Instance.PlacingDirectBuildable = buildable;
            return MouseHandlingResult.None;
        }
        public GameCommand CreateBuildCommand(ref DirectBuildable buildable, GameCommandSource source, ArcenPoint worldLocation, DirectBuildMode buildMode)
        {
            GameCommand command = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.BuildDrill], source );
            command.RelatedString2 = buildable.typeData_ForImplementation.InternalName;
            command.RelatedEntityIDs.Add( this.GetBuilder(ref buildable).PrimaryKeyID );
            command.RelatedPoints.Add( worldLocation );
            return command;
        }
    }
    #endregion
    
    #region DirectBuild_ScourgeVassal
    class DirectBuild_ScourgeVassal : IDirectBuildableImplementation, ICustomBuildSidebarCategoryInfo  {
        public bool GetShouldShow(Planet planet, Faction faction) {
            ScourgeInfusedHumanEmpireFactionBaseInfo baseInfo = faction.TryGetExternalBaseInfoAs<ScourgeInfusedHumanEmpireFactionBaseInfo>();
            if (baseInfo.VassalFaction == null )
                return false;
            ScourgeVassalFactionBaseInfo scourgeBaseInfo = baseInfo.GetVassalBaseInfo();
            if ( scourgeBaseInfo == null )
                return false;

            // if ( scourgeBaseInfo.PlanetHasArmory( planet ))
            //     return false;
            // int range = 2;
            // if ( scourgeBaseInfo.PlanetHasSpawner( planet ))
            //     return false;


            return true;
        }
        public DirectBuildable CreateDirectBuildable(Planet planet, Faction faction, GameEntityTypeData TypeData)
        {
            if (!TypeData.ShouldShownInCampaign())
                return new DirectBuildable();

            ScourgeInfusedHumanEmpireFactionBaseInfo baseInfo = faction.TryGetExternalBaseInfoAs<ScourgeInfusedHumanEmpireFactionBaseInfo>();
            if ( baseInfo != null )
            {
                if ( TypeData.GetHasTag( "TierOne" ) && !baseInfo.IsFortressBuildable( TypeData ) )
                    return new DirectBuildable();
                if ( TypeData.GetHasTag( "TierTwo" ) && !baseInfo.IsGreaterFortressBuildable( TypeData ) )
                    return new DirectBuildable();
            }

            DirectBuildable buildable = new DirectBuildable();
            buildable.typeData_ForImplementation = TypeData;
            // Use FleetUsedPlanet as an awkward way to record the planet being built on.
            buildable.fleetToBuild_ForImplementation = planet.Factions[faction.FactionIndex].FleetUsedAtPlanet;
            buildable.Implementation = this;
            return buildable;
        }

        public GameEntity_Squad GetBuilder(ref DirectBuildable buildable)
        {
            Fleet fleetUsedAtPlanet = buildable.fleetToBuild_ForImplementation;
            PlanetFaction localFaction = fleetUsedAtPlanet.Planet.Factions[fleetUsedAtPlanet.Faction.FactionIndex];
            return DirectBuildHelpers.GetMobileBuilderForPlanet(localFaction);
        }
        public byte GetEffectiveMark(ref DirectBuildable buildable) { return 1; }

        public FleetCategory GetFleetCategory(ref DirectBuildable buildable) { return FleetCategory.PlayerCustomCity; }
        public string GetFleetName(ref DirectBuildable buildable) { return ""; }

        public int GetBuildCap(ref DirectBuildable buildable) { return 0; }
        public int GetCurrentlyBuilt(ref DirectBuildable buildable) { return 0; }

        public int GetMetalCost(ref DirectBuildable buildable) { return buildable.typeData_ForImplementation.MarkStatsFor( buildable.EffectiveMark ).MetalCost; }

        public void AddFleetDescriptor(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            buffer.StartColor( "9DFBA7" ).Add( "星球 " ).Add( buildable.fleetToBuild_ForImplementation.Planet.Name );
        }
        public bool AddOverridingCapText(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            return false;
        }
        public void GetHoverText(ref DirectBuildable buildable, ArcenDoubleCharacterBuffer buffer)
        {
            EntityText.GetTooltip( buffer, null, null, buildable.TypeData, -1, null, buildable.EffectiveMark, FromSidebarType.Sidebar_SingleUnit, ShipExtraDetailFlags.BuildInfo, 1f, false );
        }

        public MouseHandlingResult HandleButtonClick(ref DirectBuildable buildable, MouseHandlingInput input) {
            Engine_AIW2.Instance.PlacingDirectBuildable = buildable;
            return MouseHandlingResult.None;
        }
        public GameCommand CreateBuildCommand(ref DirectBuildable buildable, GameCommandSource source, ArcenPoint worldLocation, DirectBuildMode buildMode)
        {
            GameCommand command = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.BuildScourgeStructure], source );
            command.RelatedString2 = buildable.typeData_ForImplementation.InternalName;
            command.RelatedEntityIDs.Add( this.GetBuilder(ref buildable).PrimaryKeyID );
            command.RelatedPoints.Add( worldLocation );
            return command;
        }
    }
    #endregion
    
    #region DirectBuild_DysonSidekickSphere
    class DirectBuild_DysonSidekickSphere : IDirectBuildableImplementation, ICustomBuildSidebarCategoryInfo  {
        public bool GetShouldShow(Planet planet, Faction faction) {
            if ( planet.GetControllingFaction().GetIsHostileTowards(faction) ) {
                return false;
            }
            DysonSidekickFactionBaseInfo baseInfo = faction.TryGetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
            if (baseInfo == null) {
                return false;
            }
            GameEntityTypeData sphereData = GameEntityTypeDataTable.Instance.GetRowByName("DysonSidekickZenithSphere"); //it doesn't matter which sphere, they all have the same rules
            if (baseInfo.DoesPlanetHaveStronghold(planet) || baseInfo.DoesPlanetHaveSphere(planet)) {
                return false;
            }
            if ( !planet.IsRavaged ) //can only be built on ravaged planets
                return false;
            return true;
        }
        public DirectBuildable CreateDirectBuildable(Planet planet, Faction faction, GameEntityTypeData TypeData)
        {
            if (!TypeData.ShouldShownInCampaign())
                return new DirectBuildable();
            
            DirectBuildable buildable = new DirectBuildable();
            buildable.typeData_ForImplementation = TypeData;
            // Use FleetUsedPlanet as an awkward way to record the planet being built on.
            buildable.fleetToBuild_ForImplementation = planet.Factions[faction.FactionIndex].FleetUsedAtPlanet;
            buildable.Implementation = this;
            return buildable;
        }

        public GameEntity_Squad GetBuilder(ref DirectBuildable buildable)
        {
            Fleet fleetUsedAtPlanet = buildable.fleetToBuild_ForImplementation;
            PlanetFaction localFaction = fleetUsedAtPlanet.Planet.Factions[fleetUsedAtPlanet.Faction.FactionIndex];
            return DirectBuildHelpers.GetMobileBuilderForPlanet(localFaction);
        }
        public byte GetEffectiveMark(ref DirectBuildable buildable) { return 1; }

        public FleetCategory GetFleetCategory(ref DirectBuildable buildable) { return FleetCategory.PlayerCustomCity; }
        public string GetFleetName(ref DirectBuildable buildable) { return ""; }

        public int GetBuildCap(ref DirectBuildable buildable) { return 0; }
        public int GetCurrentlyBuilt(ref DirectBuildable buildable) { return 0; }

        public int GetMetalCost(ref DirectBuildable buildable) { return buildable.typeData_ForImplementation.MarkStatsFor( buildable.EffectiveMark ).MetalCost; }

        public void AddFleetDescriptor(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            buffer.StartColor( "9DFBA7" ).Add( "星球 " ).Add( buildable.fleetToBuild_ForImplementation.Planet.Name );
        }
        public bool AddOverridingCapText(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            return false;
        }
        public void GetHoverText(ref DirectBuildable buildable, ArcenDoubleCharacterBuffer buffer)
        {
            EntityText.GetTooltip( buffer, null, null, buildable.TypeData, -1, null, buildable.EffectiveMark, FromSidebarType.Sidebar_SingleUnit, ShipExtraDetailFlags.BuildInfo, 1f, false );
        }

        public MouseHandlingResult HandleButtonClick(ref DirectBuildable buildable, MouseHandlingInput input) {
            Engine_AIW2.Instance.PlacingDirectBuildable = buildable;
            return MouseHandlingResult.None;
        }
        public GameCommand CreateBuildCommand(ref DirectBuildable buildable, GameCommandSource source, ArcenPoint worldLocation, DirectBuildMode buildMode)
        {
            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.BuildSphere], source );
            command.RelatedString2 = buildable.typeData_ForImplementation.InternalName;
            command.RelatedEntityIDs.Add( this.GetBuilder(ref buildable).PrimaryKeyID );
            command.RelatedPoints.Add( worldLocation );
            return command;
        }
    }
    #endregion

    /* For Armada */
    #region DirectBuild_Starbase
    class DirectBuild_Starbase : IDirectBuildableImplementation, ICustomBuildSidebarCategoryInfo  {
        public bool GetShouldShow(Planet planet, Faction faction) {
            if ( planet.GetControllingFaction().GetIsHostileTowards(faction) ) {
                return false;
            }
            ArmadaFactionBaseInfo baseInfo = faction.TryGetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
            if (baseInfo == null) {
                return false;
            }
            if (baseInfo.DoesPlanetHaveStarbase(planet)) {
                return false;
            }
            if ( planet.IsRavaged)
                return false;

            return true;
        }
        public DirectBuildable CreateDirectBuildable(Planet planet, Faction faction, GameEntityTypeData TypeData)
        {
            if (!TypeData.ShouldShownInCampaign())
                return new DirectBuildable();

            DirectBuildable buildable = new DirectBuildable();
            buildable.typeData_ForImplementation = TypeData;
            // Use FleetUsedPlanet as an awkward way to record the planet being built on.
            buildable.fleetToBuild_ForImplementation = planet.Factions[faction.FactionIndex].FleetUsedAtPlanet;
            buildable.Implementation = this;
            return buildable;
        }

        public GameEntity_Squad GetBuilder(ref DirectBuildable buildable)
        {
            Fleet fleetUsedAtPlanet = buildable.fleetToBuild_ForImplementation;
            PlanetFaction localFaction = fleetUsedAtPlanet.Planet.Factions[fleetUsedAtPlanet.Faction.FactionIndex];
            return DirectBuildHelpers.GetMobileBuilderForPlanet(localFaction);
        }
        public byte GetEffectiveMark(ref DirectBuildable buildable) { return 1; }

        public FleetCategory GetFleetCategory(ref DirectBuildable buildable) { return FleetCategory.PlayerCustomCity; }
        public string GetFleetName(ref DirectBuildable buildable) { return ""; }

        public int GetBuildCap(ref DirectBuildable buildable) { return 0; }
        public int GetCurrentlyBuilt(ref DirectBuildable buildable) { return 0; }

        public int GetMetalCost(ref DirectBuildable buildable) { return buildable.typeData_ForImplementation.MarkStatsFor( buildable.EffectiveMark ).MetalCost; }

        public void AddFleetDescriptor(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            buffer.StartColor( "9DFBA7" ).Add( "星球 " ).Add( buildable.fleetToBuild_ForImplementation.Planet.Name );
        }
        public bool AddOverridingCapText(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            return false;
        }
        public void GetHoverText(ref DirectBuildable buildable, ArcenDoubleCharacterBuffer buffer)
        {
            EntityText.GetTooltip( buffer, null, null, buildable.TypeData, -1, null, buildable.EffectiveMark, FromSidebarType.Sidebar_SingleUnit, ShipExtraDetailFlags.BuildInfo, 1f, false );
        }

        public MouseHandlingResult HandleButtonClick(ref DirectBuildable buildable, MouseHandlingInput input) {
            Engine_AIW2.Instance.PlacingDirectBuildable = buildable;
            return MouseHandlingResult.None;
        }
        public GameCommand CreateBuildCommand(ref DirectBuildable buildable, GameCommandSource source, ArcenPoint worldLocation, DirectBuildMode buildMode)
        {
            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.BuildStarbase], source );
            command.RelatedString2 = buildable.typeData_ForImplementation.InternalName;
            command.RelatedEntityIDs.Add( this.GetBuilder(ref buildable).PrimaryKeyID );
            command.RelatedPoints.Add( worldLocation );
            return command;
        }
    }
    #endregion
    #region DirectBuild_Mine
    abstract class DirectBuild_Mine_Base : IDirectBuildableImplementation, ICustomBuildSidebarCategoryInfo
    {
        protected abstract int Tier { get; }

        public bool GetShouldShow( Planet planet, Faction faction )
        {
            if ( !planet.GetControllingOrInfluencingFaction().GetIsHostileTowards( faction ) )
                return false;
            ArmadaFactionBaseInfo baseInfo = faction.TryGetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
            if ( baseInfo == null )
                return false;
            baseInfo.PlanetMineCount.TryGetValue( planet, out int mineCount );
            int planetTier = Math.Min( ( mineCount / baseInfo.Income.MiningDepthIncreaseRate ) + 1, 4 );
            return planetTier == Tier;
        }

        public DirectBuildable CreateDirectBuildable( Planet planet, Faction faction, GameEntityTypeData TypeData )
        {
            if ( !TypeData.ShouldShownInCampaign() )
                return new DirectBuildable();

            DirectBuildable buildable = new DirectBuildable();
            buildable.typeData_ForImplementation = TypeData;
            // Use FleetUsedAtPlanet as an awkward way to record the planet being built on.
            buildable.fleetToBuild_ForImplementation = planet.Factions[faction.FactionIndex].FleetUsedAtPlanet;
            buildable.Implementation = this;
            return buildable;
        }

        public GameEntity_Squad GetBuilder( ref DirectBuildable buildable )
        {
            Fleet fleetUsedAtPlanet = buildable.fleetToBuild_ForImplementation;
            PlanetFaction localFaction = fleetUsedAtPlanet.Planet.Factions[fleetUsedAtPlanet.Faction.FactionIndex];
            return DirectBuildHelpers.GetMobileBuilderForPlanet( localFaction );
        }
        public byte GetEffectiveMark( ref DirectBuildable buildable ) { return 1; }

        public FleetCategory GetFleetCategory( ref DirectBuildable buildable ) { return FleetCategory.PlayerCustomCity; }
        public string GetFleetName( ref DirectBuildable buildable ) { return ""; }

        public int GetBuildCap( ref DirectBuildable buildable ) { return 0; }
        public int GetCurrentlyBuilt( ref DirectBuildable buildable ) { return 0; }

        public int GetMetalCost( ref DirectBuildable buildable ) { return buildable.typeData_ForImplementation.MarkStatsFor( buildable.EffectiveMark ).MetalCost; }

        public void AddFleetDescriptor( ref DirectBuildable buildable, ArcenCharacterBufferBase buffer )
        {
            buffer.StartColor( "9DFBA7" ).Add( "星球 " ).Add( buildable.fleetToBuild_ForImplementation.Planet.Name );
        }
        private int GetCooldownSecondsRemaining( ref DirectBuildable buildable )
        {
            Planet planet = buildable.fleetToBuild_ForImplementation.Planet;
            ArmadaFactionBaseInfo baseInfo = buildable.fleetToBuild_ForImplementation.Faction.TryGetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
            if ( baseInfo == null )
                return 0;
            return baseInfo.GetMiningCooldownSecondsRemaining( planet );
        }

        public bool AddOverridingCapText( ref DirectBuildable buildable, ArcenCharacterBufferBase buffer )
        {
            int secondsLeft = GetCooldownSecondsRemaining( ref buildable );
            if ( secondsLeft <= 0 )
                return false;
            buffer.StartColor( Color.red ).Add( secondsLeft ).Add( "秒冷却" ).EndColor();
            return true;
        }
        public void GetHoverText( ref DirectBuildable buildable, ArcenDoubleCharacterBuffer buffer )
        {
            EntityText.GetTooltip( buffer, null, null, buildable.TypeData, -1, null, buildable.EffectiveMark, FromSidebarType.Sidebar_SingleUnit, ShipExtraDetailFlags.BuildInfo, 1f, false );
            int secondsLeft = GetCooldownSecondsRemaining( ref buildable );
            if ( secondsLeft > 0 )
                buffer.Add( "\n\n" ).StartColor( Color.red ).Add( "此星球最近被开采过。可用时间：" ).Add( secondsLeft ).Add( "秒。" ).EndColor();
        }

        public MouseHandlingResult HandleButtonClick( ref DirectBuildable buildable, MouseHandlingInput input )
        {
            int secondsLeft = GetCooldownSecondsRemaining( ref buildable );
            if ( secondsLeft > 0 )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand(
                    buildable.fleetToBuild_ForImplementation.Planet.Name + " 最近被开采过；你必须等待 " + secondsLeft + " 更多秒。",
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return MouseHandlingResult.PlayClickDeniedSound;
            }
            Engine_AIW2.Instance.PlacingDirectBuildable = buildable;
            return MouseHandlingResult.None;
        }
        public GameCommand CreateBuildCommand( ref DirectBuildable buildable, GameCommandSource source, ArcenPoint worldLocation, DirectBuildMode buildMode )
        {
            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.BuildMine], source );
            command.RelatedString2 = buildable.typeData_ForImplementation.InternalName;
            command.RelatedEntityIDs.Add( this.GetBuilder( ref buildable ).PrimaryKeyID );
            command.RelatedPoints.Add( worldLocation );
            return command;
        }
    }

    class DirectBuild_Mine_T1 : DirectBuild_Mine_Base { protected override int Tier => 1; }
    class DirectBuild_Mine_T2 : DirectBuild_Mine_Base { protected override int Tier => 2; }
    class DirectBuild_Mine_T3 : DirectBuild_Mine_Base { protected override int Tier => 3; }
    class DirectBuild_Mine_T4 : DirectBuild_Mine_Base { protected override int Tier => 4; }
    #endregion
    #region DirectBuild_Swarm
    class DirectBuild_Swarm : IDirectBuildableImplementation, ICustomBuildSidebarCategoryInfo  {
        public bool GetShouldShow(Planet planet, Faction faction) {
            EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( faction );
            int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;

            if ( hostileStrength > 10 * 1000 )
                return true;
            return false;
        }

        public DirectBuildable CreateDirectBuildable(Planet planet, Faction faction, GameEntityTypeData TypeData)
        {
            if (!TypeData.ShouldShownInCampaign())
                return new DirectBuildable();

            DirectBuildable buildable = new DirectBuildable();
            buildable.typeData_ForImplementation = TypeData;
            // Use FleetUsedPlanet as an awkward way to record the planet being built on.
            buildable.fleetToBuild_ForImplementation = planet.Factions[faction.FactionIndex].FleetUsedAtPlanet;
            buildable.Implementation = this;
            return buildable;
        }

        public GameEntity_Squad GetBuilder(ref DirectBuildable buildable)
        {
            Fleet fleetUsedAtPlanet = buildable.fleetToBuild_ForImplementation;
            PlanetFaction localFaction = fleetUsedAtPlanet.Planet.Factions[fleetUsedAtPlanet.Faction.FactionIndex];
            return DirectBuildHelpers.GetMobileBuilderForPlanet(localFaction);
        }
        public byte GetEffectiveMark(ref DirectBuildable buildable) { return 1; }

        public FleetCategory GetFleetCategory(ref DirectBuildable buildable) { return FleetCategory.PlayerCustomCity; }
        public string GetFleetName(ref DirectBuildable buildable) { return ""; }

        public int GetBuildCap(ref DirectBuildable buildable) { return 0; }
        public int GetCurrentlyBuilt(ref DirectBuildable buildable) { return 0; }

        public int GetMetalCost(ref DirectBuildable buildable) { return buildable.typeData_ForImplementation.MarkStatsFor( buildable.EffectiveMark ).MetalCost; }

        public void AddFleetDescriptor(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            buffer.StartColor( "9DFBA7" ).Add( "星球 " ).Add( buildable.fleetToBuild_ForImplementation.Planet.Name );
        }
        public bool AddOverridingCapText(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            return false;
        }
        public void GetHoverText(ref DirectBuildable buildable, ArcenDoubleCharacterBuffer buffer)
        {
            EntityText.GetTooltip( buffer, null, null, buildable.TypeData, -1, null, buildable.EffectiveMark, FromSidebarType.Sidebar_SingleUnit, ShipExtraDetailFlags.BuildInfo, 1f, false );
        }

        public MouseHandlingResult HandleButtonClick(ref DirectBuildable buildable, MouseHandlingInput input) {
            Engine_AIW2.Instance.PlacingDirectBuildable = buildable;
            return MouseHandlingResult.None;
        }
        public GameCommand CreateBuildCommand(ref DirectBuildable buildable, GameCommandSource source, ArcenPoint worldLocation, DirectBuildMode buildMode)
        {
            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.BuildArmadaStructure], source );
            command.RelatedString2 = buildable.typeData_ForImplementation.InternalName;
            command.RelatedEntityIDs.Add( this.GetBuilder(ref buildable).PrimaryKeyID );
            command.RelatedPoints.Add( worldLocation );
            return command;
        }
    }
    #endregion
    /* For Apkallu */
    #region DirectBuild_Ziggurat
    class DirectBuild_Ziggurat : IDirectBuildableImplementation, ICustomBuildSidebarCategoryInfo  {
        public bool GetShouldShow(Planet planet, Faction faction) {
            if ( planet.GetControllingFaction().GetIsHostileTowards(faction) ) {
                return false;
            }
            ApkalluFactionBaseInfo baseInfo = faction.TryGetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            if (baseInfo == null) {
                return false;
            }
            if (baseInfo.DoesPlanetHaveZiggurat(planet)) {
                return false;
            }
            if ( planet.IsRavaged)
                return false;

            return true;
        }
        public DirectBuildable CreateDirectBuildable(Planet planet, Faction faction, GameEntityTypeData TypeData)
        {
            if (!TypeData.ShouldShownInCampaign())
                return new DirectBuildable();

            DirectBuildable buildable = new DirectBuildable();
            buildable.typeData_ForImplementation = TypeData;
            // Use FleetUsedPlanet as an awkward way to record the planet being built on.
            buildable.fleetToBuild_ForImplementation = planet.Factions[faction.FactionIndex].FleetUsedAtPlanet;
            buildable.Implementation = this;
            return buildable;
        }

        public GameEntity_Squad GetBuilder(ref DirectBuildable buildable)
        {
            Fleet fleetUsedAtPlanet = buildable.fleetToBuild_ForImplementation;
            PlanetFaction localFaction = fleetUsedAtPlanet.Planet.Factions[fleetUsedAtPlanet.Faction.FactionIndex];
            return DirectBuildHelpers.GetMobileBuilderForPlanet(localFaction);
        }
        public byte GetEffectiveMark(ref DirectBuildable buildable) { return 1; }

        public FleetCategory GetFleetCategory(ref DirectBuildable buildable) { return FleetCategory.PlayerCustomCity; }
        public string GetFleetName(ref DirectBuildable buildable) { return ""; }

        public int GetBuildCap(ref DirectBuildable buildable) { return 0; }
        public int GetCurrentlyBuilt(ref DirectBuildable buildable) { return 0; }

        public int GetMetalCost(ref DirectBuildable buildable) { return buildable.typeData_ForImplementation.MarkStatsFor( buildable.EffectiveMark ).MetalCost; }

        public void AddFleetDescriptor(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            buffer.StartColor( "9DFBA7" ).Add( "星球 " ).Add( buildable.fleetToBuild_ForImplementation.Planet.Name );
        }
        public bool AddOverridingCapText(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            return false;
        }
        public void GetHoverText(ref DirectBuildable buildable, ArcenDoubleCharacterBuffer buffer)
        {
            EntityText.GetTooltip( buffer, null, null, buildable.TypeData, -1, null, buildable.EffectiveMark, FromSidebarType.Sidebar_SingleUnit, ShipExtraDetailFlags.BuildInfo, 1f, false );
        }

        public MouseHandlingResult HandleButtonClick(ref DirectBuildable buildable, MouseHandlingInput input) {
            Engine_AIW2.Instance.PlacingDirectBuildable = buildable;
            return MouseHandlingResult.None;
        }
        public GameCommand CreateBuildCommand(ref DirectBuildable buildable, GameCommandSource source, ArcenPoint worldLocation, DirectBuildMode buildMode)
        {
            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.BuildZiggurat], source );
            command.RelatedString2 = buildable.typeData_ForImplementation.InternalName;
            command.RelatedEntityIDs.Add( this.GetBuilder(ref buildable).PrimaryKeyID );
            command.RelatedPoints.Add( worldLocation );
            return command;
        }
    }
    #endregion

    #region DirectBuild_Duru
    class DirectBuild_Duru : IDirectBuildableImplementation, ICustomBuildSidebarCategoryInfo  {
        public bool GetShouldShow(Planet planet, Faction faction) {
            if ( planet.GetControllingFaction().GetIsHostileTowards(faction) )
                return false;
            ApkalluFactionBaseInfo baseInfo = faction.TryGetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            if (baseInfo == null)
                return false;
            if (baseInfo.DoesPlanetHaveDuru(planet))
                return false;
            if ( planet.IsRavaged)
                return false;
            return true;
        }
        public DirectBuildable CreateDirectBuildable(Planet planet, Faction faction, GameEntityTypeData TypeData)
        {
            if (!TypeData.ShouldShownInCampaign())
                return new DirectBuildable();
            DirectBuildable buildable = new DirectBuildable();
            buildable.typeData_ForImplementation = TypeData;
            buildable.fleetToBuild_ForImplementation = planet.Factions[faction.FactionIndex].FleetUsedAtPlanet;
            buildable.Implementation = this;
            return buildable;
        }
        public GameEntity_Squad GetBuilder(ref DirectBuildable buildable)
        {
            Fleet fleetUsedAtPlanet = buildable.fleetToBuild_ForImplementation;
            PlanetFaction localFaction = fleetUsedAtPlanet.Planet.Factions[fleetUsedAtPlanet.Faction.FactionIndex];
            return DirectBuildHelpers.GetMobileBuilderForPlanet(localFaction);
        }
        public byte GetEffectiveMark(ref DirectBuildable buildable) { return 1; }
        public FleetCategory GetFleetCategory(ref DirectBuildable buildable) { return FleetCategory.PlayerCustomCity; }
        public string GetFleetName(ref DirectBuildable buildable) { return ""; }
        public int GetBuildCap(ref DirectBuildable buildable) { return 0; }
        public int GetCurrentlyBuilt(ref DirectBuildable buildable) { return 0; }
        public int GetMetalCost(ref DirectBuildable buildable) { return buildable.typeData_ForImplementation.MarkStatsFor( buildable.EffectiveMark ).MetalCost; }
        public void AddFleetDescriptor(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            buffer.StartColor( "9DFBA7" ).Add( "星球 " ).Add( buildable.fleetToBuild_ForImplementation.Planet.Name );
        }
        public bool AddOverridingCapText(ref DirectBuildable buildable, ArcenCharacterBufferBase buffer)
        {
            return false;
        }
        public void GetHoverText(ref DirectBuildable buildable, ArcenDoubleCharacterBuffer buffer)
        {
            EntityText.GetTooltip( buffer, null, null, buildable.TypeData, -1, null, buildable.EffectiveMark, FromSidebarType.Sidebar_SingleUnit, ShipExtraDetailFlags.BuildInfo, 1f, false );
        }
        public MouseHandlingResult HandleButtonClick(ref DirectBuildable buildable, MouseHandlingInput input) {
            Engine_AIW2.Instance.PlacingDirectBuildable = buildable;
            return MouseHandlingResult.None;
        }
        public GameCommand CreateBuildCommand(ref DirectBuildable buildable, GameCommandSource source, ArcenPoint worldLocation, DirectBuildMode buildMode)
        {
            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.BuildDuru], source );
            command.RelatedString2 = buildable.typeData_ForImplementation.InternalName;
            command.RelatedEntityIDs.Add( this.GetBuilder(ref buildable).PrimaryKeyID );
            command.RelatedPoints.Add( worldLocation );
            return command;
        }
    }
    #endregion
}
