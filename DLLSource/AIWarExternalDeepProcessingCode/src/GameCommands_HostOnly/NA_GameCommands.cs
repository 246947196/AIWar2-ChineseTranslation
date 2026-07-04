using Arcen.AIW2.Core;
using Arcen.Universal;
using System;


namespace Arcen.AIW2.External
{
    public class GameCommand_BuildNecropolis : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
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
            GameEntityTypeData typeToPlace = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( typeToPlace == null )
            {
                ArcenDebugging.ArcenDebugLog( "During placement: typeToPlace == null!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            GameEntity_Squad entityDoingThePlacing = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );
            if ( entityDoingThePlacing == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "建造源在建造开始前已死亡——无法建造此" + typeToPlace.GetDisplayName() + "。",
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( command.RelatedPoints == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedPoints == null in GameCommand_BuildNecropolis", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( command.RelatedPoints.Count != 1 )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedPoints.Count != 1 in GameCommand_BuildNecropolis", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }

            Faction faction = entityDoingThePlacing.PlanetFaction.Faction;
            NecromancerEmpireFactionBaseInfo BaseInfo = faction.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
            NecromancerEmpireFactionDeepInfo DeepInfo = faction.GetExternalDeepInfoAs<NecromancerEmpireFactionDeepInfo>();
            if ( BaseInfo.DoesPlanetHaveNecropolis( entityDoingThePlacing.Planet ) )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "这里已经有一座亡灵城——你无法再放置一座！", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( entityDoingThePlacing.Planet.GetControllingFaction().GetIsHostileTowards(faction) ) {
                World_AIW2.Instance.QueueChatMessageOrCommand( "该星球由敌人控制——你无法在此放置亡灵城！", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
            }
            if ( faction.StoredFactionResourceOne < typeToPlace.CostInResourceOne )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "精华不足；你需要" + typeToPlace.CostInResourceOne + "，而你的阵营" + faction.GetDisplayName() +
                    "仅有" + faction.StoredFactionResourceOne, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            
            int galaxyCap =  typeToPlace.CalculateEffectiveGalaxyWideCapForPlayersConstructing(faction);
            if ( galaxyCap > 0 )
            {
                int galaxyBuiltCount = 0;
                foreach ( GameEntity_Squad squad in faction.Squads( EntityRollupType.HasGalaxyWideCapForPlayersConstructing ) )
                {
                    if ( squad.TypeData.GalaxyWideCapMatchString == typeToPlace.GalaxyWideCapMatchString )
                        galaxyBuiltCount++;
                }
                if ( galaxyBuiltCount >= galaxyCap )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( "你已达到" + typeToPlace.GetDisplayName() + "的银河系上限。你需要占领更多星球。", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                    return;
                }
            }
            // TODO: This should maybe pass in an arguement to make the city
            // start out unbuilt, rather than completely built.
            // Though, in that case, should the fleet be created immediately.
            DeepInfo.SpawnNecromancerNecropolis(command.RelatedPoints.First, entityDoingThePlacing.Planet, command.RelatedString2, faction, context.GetHostOnlyContext(), out GameEntity_Squad unusedNecroFlag, true );
            faction.StoredFactionResourceOne -= typeToPlace.CostInResourceOne;
        }
    }

    public class GameCommand_TransformNecrofleet : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //client will hear about it as a fast-blast!

            if ( command.RelatedEntityIDs == null )
            {
                ArcenDebugging.ArcenDebugLog( "During transform flagship: No RelatedEntityIDs == null!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            if ( command.RelatedEntityIDs.Count <= 0 )
            {
                ArcenDebugging.ArcenDebugLog( "During transform flagship: No RelatedEntityIDs!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }

            GameEntity_Squad centerpiece = World_AIW2.Instance.GetEntityByID_Squad(command.RelatedEntityIDs.First);
            if (centerpiece == null) {
                World_AIW2.Instance.QueueChatMessageOrCommand( "舰队核心在变形前已死亡——无法进行变形。",
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }

            Faction faction = centerpiece.PlanetFaction.Faction;

            if ( centerpiece.GetIsCrippled() )
            {
                if ( faction.GetIsLocalFaction() )
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "变形目标已损坏！",
                            centerpiece.TypeData.DisplayName + "已损坏，因此阵营" + faction.GetDisplayName() + "未对其进行变形。", "确定" );
                return;
            }

            if ( centerpiece.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength > 500 )
            {
                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "核心单位位于敌对星球！",
                        centerpiece.TypeData.DisplayName + "位于敌对星球上，因此阵营" + faction.GetDisplayName() + "未对其进行变形。", "确定" );
                return;
            }

            NecromancerEmpireFactionBaseInfo baseInfo = faction.TryGetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();

            NecromancerUpgrade upgrade = null;
            if (command.RelatedString2 != null && command.RelatedString2.Length > 0 ) {
                List<NecromancerUpgrade> workingBlueprints = baseInfo.AvailableBlueprints.GetDisplayList();
                for ( int i = 0; i < workingBlueprints.Count; i++ ) {
                    if ( workingBlueprints[i].InternalName == command.RelatedString2)
                    {
                        upgrade = workingBlueprints[i];
                        break;
                    }
                }
            }
            if (upgrade == null) {
                ArcenDebugging.ArcenDebugLog( "During necrofleet upgrade: upgrade == null!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }

            if (centerpiece.CurrentMarkLevel < upgrade.MinFlagshipLevel)
            {
                World_AIW2.Instance.QueueChatMessageOrCommand(
                    "旗舰等级过低。此物品需要等级为" + upgrade.MinFlagshipLevel + "的旗舰",
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null);
                return;
            }

            if ( faction.StoredHacking < upgrade.BlueprintTransformCostInHacking )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "黑客点数不足；你需要" + upgrade.BlueprintTransformCostInHacking + "，而你的阵营" + faction.GetDisplayName() +
                    "仅有" + faction.StoredHacking, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( faction.StoredFactionResourceOne < upgrade.BlueprintTransformCostInResourceOne )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "精华不足；你需要" + upgrade.BlueprintTransformCostInResourceOne + "，而你的阵营" + faction.GetDisplayName() +
                    "仅有" + faction.StoredFactionResourceOne, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            FInt costForActiveHacks = HackingUtils.CalculateActiveHackingCosts(faction);
            if ( faction.StoredHacking < upgrade.BlueprintTransformCostInHacking + costForActiveHacks)
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "黑客点数不足；你需要" + upgrade.BlueprintTransformCostInHacking + "，而你的阵营" + faction.GetDisplayName() +
                    "在活跃黑客之后仅有" + (faction.StoredHacking - costForActiveHacks), ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }

            GameEntity_Squad newEntity = centerpiece.TransformInto(context.GetHostOnlyContext(), upgrade.RelatedShip, 1, true);
            faction.StoredHacking -= upgrade.BlueprintTransformCostInHacking;
            faction.StoredFactionResourceOne -= upgrade.BlueprintTransformCostInResourceOne;

            HackingType hackToDo = HackingTypeTable.Instance.GetRowByName("TransformNecromancerFlagship");
            HackingEvent hackEvent = HackingEvent.Create( faction.FactionIndex, faction.FactionIndex, -1,
                    hackToDo, null, false, centerpiece.TypeData.DisplayName + " into " + upgrade.RelatedShip.DisplayName, -1 );
            hackEvent.HackingPointsSpent = (FInt)upgrade.BlueprintTransformCostInHacking;
            hackEvent.HackingPointsLeftAfterHack = faction.StoredHacking;
            faction.HackingHistory.Add( hackEvent );
        }
    }

    public class GameCommand_TransformApkalluFlagship : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            if ( command.RelatedEntityIDs == null || command.RelatedEntityIDs.Count <= 0 )
            {
                ArcenDebugging.ArcenDebugLog( "TransformApkalluFlagship: no RelatedEntityIDs" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }

            GameEntity_Squad centerpiece = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );
            if ( centerpiece == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "舰队核心在变形前已死亡——无法进行变形。",
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }

            Faction faction = centerpiece.PlanetFaction.Faction;

            if ( centerpiece.GetIsCrippled() )
            {
                if ( faction.GetIsLocalFaction() )
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "变形目标已损坏！",
                        centerpiece.TypeData.DisplayName + "已损坏，因此无法变形。", "确定" );
                return;
            }

            if ( centerpiece.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength > 500 )
            {
                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "核心单位位于敌对星球！",
                    centerpiece.TypeData.DisplayName + "位于敌对星球上，因此无法变形。", "确定" );
                return;
            }

            if ( string.IsNullOrEmpty( command.RelatedString2 ) )
            {
                ArcenDebugging.ArcenDebugLog( "TransformApkalluFlagship: RelatedString2 (breach name) is empty", Verbosity.ShowAsError );
                return;
            }

            MalwareBreach breach = MalwareBreachTable.Instance.GetRowByName( command.RelatedString2 );
            if ( breach == null || breach.UnlockFlagshipForm == null )
            {
                ArcenDebugging.ArcenDebugLog( "TransformApkalluFlagship: breach '" + command.RelatedString2 + "' not found or has no UnlockFlagshipForm", Verbosity.ShowAsError );
                return;
            }

            ApkalluFactionBaseInfo apkalluInfo = faction.TryGetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            if ( apkalluInfo == null )
            {
                ArcenDebugging.ArcenDebugLog( "TransformApkalluFlagship: faction has no ApkalluFactionBaseInfo", Verbosity.ShowAsError );
                return;
            }

            bool breachCompleted = false;
            for ( int i = 0; i < apkalluInfo.CompletedBreaches.Count; i++ )
            {
                if ( apkalluInfo.CompletedBreaches[i]?.name == breach.name )
                {
                    breachCompleted = true;
                    break;
                }
            }
            // Starting flagship (Uanna) is always available without a breach
            bool isStartingForm = breach.UnlockFlagshipForm.GetHasTag( "ApkalluStartingFlagship" );
            if ( !breachCompleted && !isStartingForm )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "此旗舰形态尚未解锁。",
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }

            if ( centerpiece.TypeData == breach.UnlockFlagshipForm )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "你的旗舰已经是此形态。",
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }

            if ( centerpiece.CurrentMarkLevel < breach.FlagshipFormTransformMinMarkLevel )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand(
                    "旗舰等级过低。此形态需要等级为" + breach.FlagshipFormTransformMinMarkLevel + "的旗舰。",
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }

            if ( faction.StoredFactionResourceOne < breach.FlagshipFormTransformCostResourceOne )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand(
                    "资源不足；你需要" + breach.FlagshipFormTransformCostResourceOne +
                    "，而你的阵营仅有" + faction.StoredFactionResourceOne.IntValue + "。",
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }

            faction.StoredFactionResourceOne -= breach.FlagshipFormTransformCostResourceOne;
            centerpiece.TransformInto( context.GetHostOnlyContext(), breach.UnlockFlagshipForm, 1, true );
        }
    }

    public class GameCommand_BuildStronghold : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
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
            GameEntityTypeData typeToPlace = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( typeToPlace == null )
            {
                ArcenDebugging.ArcenDebugLog( "During placement: typeToPlace == null!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            GameEntity_Squad entityDoingThePlacing = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );
            if ( entityDoingThePlacing == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "建造源在建造开始前已死亡——无法建造此" + typeToPlace.GetDisplayName() + "。",
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( command.RelatedPoints == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedPoints == null in GameCommand_BuildStronghole", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( command.RelatedPoints.Count != 1 )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedPoints.Count != 1 in GameCommand_BuildStronghold", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }

            Faction faction = entityDoingThePlacing.PlanetFaction.Faction;
            DysonSidekickFactionBaseInfo BaseInfo = faction.GetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
            DysonSidekickFactionDeepInfo DeepInfo = faction.GetExternalDeepInfoAs<DysonSidekickFactionDeepInfo>();
            if ( BaseInfo.DoesPlanetHaveStronghold( entityDoingThePlacing.Planet ) )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "这里已经有一座要塞——你无法再放置一座！", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( entityDoingThePlacing.Planet.GetControllingFaction().GetIsHostileTowards(faction) ) {
                World_AIW2.Instance.QueueChatMessageOrCommand( "该星球由敌人控制——你无法在此放置要塞！", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
            }
            
            if ( faction.StoredFactionResourceOne < typeToPlace.CostInResourceOne )
            {
                PlayerTypeData pTypeData = faction.PlayerTypeDataOrNull_ModeratelyExpensive;
                if ( pTypeData != null )
                    World_AIW2.Instance.QueueChatMessageOrCommand( pTypeData.Resource1DisplayName + "不足；你需要" + typeToPlace.CostInResourceOne + "，而你的阵营" + faction.GetDisplayName() +
                      "仅有" + faction.StoredFactionResourceOne, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                else
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( "精华不足；你需要" + typeToPlace.CostInResourceOne + "，而你的阵营" + faction.GetDisplayName() +
                      "仅有" + faction.StoredFactionResourceOne, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                }
                return;
            }
            
            int galaxyCap =  typeToPlace.CalculateEffectiveGalaxyWideCapForPlayersConstructing(faction);
            if ( galaxyCap > 0 )
            {
                int galaxyBuiltCount = 0;
                foreach ( GameEntity_Squad squad in faction.Squads( EntityRollupType.HasGalaxyWideCapForPlayersConstructing ) )
                {
                    if ( squad.TypeData.GalaxyWideCapMatchString == typeToPlace.GalaxyWideCapMatchString )
                        galaxyBuiltCount++;
                }
                if ( galaxyBuiltCount >= galaxyCap )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( "你已达到" + typeToPlace.GetDisplayName() + "的银河系上限。你需要占领更多星球。", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                    return;
                }
            }
            // TODO: This should maybe pass in an arguement to make the city
            // start out unbuilt, rather than completely built.
            // Though, in that case, should the fleet be created immediately.
            if ( faction.StoredMetal < typeToPlace.GetForMark(1).MetalCost )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "金属不足", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            DeepInfo.SpawnDysonStronghold(command.RelatedPoints.First, entityDoingThePlacing.Planet, command.RelatedString2, faction, context.GetHostOnlyContext(), out GameEntity_Squad unusedNecroFlag, true );
            faction.StoredFactionResourceOne -= typeToPlace.CostInResourceOne;
            faction.StoredMetal -= typeToPlace.GetForMark(1).MetalCost;
        }
    }
    public class GameCommand_BuildSphere : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
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
            GameEntityTypeData typeToPlace = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( typeToPlace == null )
            {
                ArcenDebugging.ArcenDebugLog( "During placement: typeToPlace == null!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            GameEntity_Squad entityDoingThePlacing = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );
            if ( entityDoingThePlacing == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "建造源在建造开始前已死亡——无法建造此" + typeToPlace.GetDisplayName() + "。",
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( command.RelatedPoints == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedPoints == null in GameCommand_BuildStronghole", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( command.RelatedPoints.Count != 1 )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedPoints.Count != 1 in GameCommand_BuildStronghold", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }

            Faction faction = entityDoingThePlacing.PlanetFaction.Faction;
            DysonSidekickFactionBaseInfo BaseInfo = faction.GetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
            DysonSidekickFactionDeepInfo DeepInfo = faction.GetExternalDeepInfoAs<DysonSidekickFactionDeepInfo>();
            if (!BaseInfo.HasAdequateDistrictsToBuild(typeToPlace))
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "建造球体需要更多区域。点击资源栏的'金属'部分查看所需内容。", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );

                return;
            }

            if ( faction.StoredFactionResourceOne < typeToPlace.CostInResourceOne )
            {
                PlayerTypeData pTypeData = faction.PlayerTypeDataOrNull_ModeratelyExpensive;
                if ( pTypeData != null )
                    World_AIW2.Instance.QueueChatMessageOrCommand( pTypeData.Resource1DisplayName + "不足；你需要" + typeToPlace.CostInResourceOne + "，而你的阵营" + faction.GetDisplayName() +
                      "仅有" + faction.StoredFactionResourceOne, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                else
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( "精华不足；你需要" + typeToPlace.CostInResourceOne + "，而你的阵营" + faction.GetDisplayName() +
                      "仅有" + faction.StoredFactionResourceOne, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                }
                return;
            }
            if ( !BaseInfo.HasAdequateDistrictsToBuild(typeToPlace) &&
                 !BaseInfo.StartWithAllUpgrades)
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "区域不足：你需要在建造球体之前将所有种族的区域提升到2级", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
            }

            Planet targetPlanet = entityDoingThePlacing.Planet;
            bool tooCloseToSphere = false;
            foreach ( GameEntity_Squad existingSphere in faction.Squads( "DysonSidekickSphere" ) )
            {
                if ( existingSphere.Planet.GetHopsTo( targetPlanet ) <= 1 )
                {
                    tooCloseToSphere = true;
                    break;
                }
            }
            if ( tooCloseToSphere )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "无法在此建造：此星球或相邻星球已有一座戴森球。", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }

            // TODO: This should maybe pass in an arguement to make the city
            // start out unbuilt, rather than completely built.
            // Though, in that case, should the fleet be created immediately.

            DeepInfo.SpawnDysonSphere(command.RelatedPoints.First, entityDoingThePlacing.Planet, command.RelatedString2, faction, context.GetHostOnlyContext(), true );
            faction.StoredFactionResourceOne -= typeToPlace.CostInResourceOne;
        }
    }
    public class GameCommand_BuildDrill : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
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
            GameEntityTypeData typeToPlace = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( typeToPlace == null )
            {
                ArcenDebugging.ArcenDebugLog( "During placement: typeToPlace == null!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            GameEntity_Squad entityDoingThePlacing = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );
            if ( entityDoingThePlacing == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "建造源在建造开始前已死亡——无法建造此" + typeToPlace.GetDisplayName() + "。",
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( command.RelatedPoints == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedPoints == null in GameCommand_BuildStronghole", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( command.RelatedPoints.Count != 1 )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedPoints.Count != 1 in GameCommand_BuildStronghold", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            Faction faction = entityDoingThePlacing.PlanetFaction.Faction;
            DysonSidekickFactionBaseInfo BaseInfo = faction.GetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
            DysonSidekickFactionDeepInfo DeepInfo = faction.GetExternalDeepInfoAs<DysonSidekickFactionDeepInfo>();
            if ( typeToPlace.GetHasTag("DysonPlanetaryDrill"))
            {
                bool foundZenith = false;
                bool foundStronghold = false;
                bool foundSphere = false;
                if ( BaseInfo.DoesPlanetHaveStronghold(entityDoingThePlacing.Planet) )
                    foundStronghold = true;
                if ( BaseInfo.DoesPlanetHaveSphere(entityDoingThePlacing.Planet) )
                    foundSphere = true;
                foreach ( GameEntity_Squad stronghold in BaseInfo.Strongholds.DisplaySquads() )
                {
                    //there will never be that many strongholds in the galaxy, and building drills is infrequent
                    if ( stronghold.TypeData.GetHasTag("DysonZenithStronghold"))
                    {
                        foundZenith = true;
                        break;
                    }
                }

                if ( foundStronghold || foundSphere )
                {
                    if ( !foundZenith)
                        World_AIW2.Instance.QueueChatMessageOrCommand("你必须拥有一座<color=#a1ffa1>赞尼西要塞</color>才能建造行星钻机。你也不能在拥有要塞的星球上钻探。", ChatType.ShowLocallyOnly, "CannotDoThatThing", null);
                    World_AIW2.Instance.QueueChatMessageOrCommand("你也不能在拥有要塞或球体的星球上钻探。", ChatType.ShowLocallyOnly, "CannotDoThatThing", null);
                    return;
                }
                if ( !foundZenith )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你必须拥有一座<color=#a1ffa1>赞尼西要塞</color>才能建造行星钻机。", ChatType.ShowLocallyOnly, "CannotDoThatThing", null);
                    return;
                }
                if (entityDoingThePlacing.Planet.IsRavaged)
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("此星球已被蹂躏。", ChatType.ShowLocallyOnly, "CannotDoThatThing", null);
                    return;
                }

            }
            if ( typeToPlace.GetHasTag("DysonAsteroidDrill"))
            {
                GameEntity_Squad asteroid = FactionUtilityMethods.Instance.GetAsteroidOnPlanetOrNull( entityDoingThePlacing.Planet );
                GameEntity_Squad chrysalis = FactionUtilityMethods.Instance.GetReaperChrysalisOnPlanetOrNull( entityDoingThePlacing.Planet );
                GameEntity_Squad planetoid = FactionUtilityMethods.Instance.GetPlanetoidOnPlanetOrNull( entityDoingThePlacing.Planet );
                if ( asteroid == null && chrysalis == null && planetoid == null )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("此处没有小行星、行星体或虫蛹。", ChatType.ShowLocallyOnly, "CannotDoThatThing", null);
                    return;
                }
            }

            if ( faction.StoredMetal < typeToPlace.GetForMark(1).MetalCost )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "金属不足", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            DeepInfo.SpawnDysonDrill(command.RelatedPoints.First, entityDoingThePlacing.Planet, command.RelatedString2, faction, context.GetHostOnlyContext(), true );
            faction.StoredFactionResourceOne -= typeToPlace.CostInResourceOne;
            faction.StoredMetal -= typeToPlace.GetForMark(1).MetalCost;
        }
    }
    public class GameCommand_BuildScourgeStructure : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
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
            GameEntityTypeData typeToPlace = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( typeToPlace == null )
            {
                ArcenDebugging.ArcenDebugLog( "During placement: typeToPlace == null!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            GameEntity_Squad entityDoingThePlacing = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );
            if ( entityDoingThePlacing == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "建造源在建造开始前已死亡——无法建造此" + typeToPlace.GetDisplayName() + "。",
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( command.RelatedPoints == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedPoints == null in GameCommand_BuildStronghole", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( command.RelatedPoints.Count != 1 )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedPoints.Count != 1 in GameCommand_BuildStronghold", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            Faction faction = entityDoingThePlacing.PlanetFaction.Faction;
            ScourgeInfusedHumanEmpireFactionBaseInfo empireBaseInfo = faction.GetExternalBaseInfoAs<ScourgeInfusedHumanEmpireFactionBaseInfo>();
            ScourgeVassalFactionBaseInfo vassalBaseInfo = empireBaseInfo.GetVassalBaseInfo();
            ScourgeInfusedHumanEmpireFactionDeepInfo DeepInfo = faction.GetExternalDeepInfoAs<ScourgeInfusedHumanEmpireFactionDeepInfo>();
            Planet planet = entityDoingThePlacing.Planet;
            //dArcenDebugging.LogSingleLine("trying to place a " + typeToPlace.InternalName, Verbosity.DoNotShow );
            if ( typeToPlace.GetHasTag("ScourgeFlower"))
            {
                bool invalidPlacement = false;
                foreach ( GameEntity_Squad flower in vassalBaseInfo.Flowers.DisplaySquads() )
                {
                    if ( flower.Planet == entityDoingThePlacing.Planet )
                    {
                        invalidPlacement = true;
                        break;
                    }
                }
                foreach ( GameEntity_Squad seed in vassalBaseInfo.Seeds.DisplaySquads() )
                {
                    if ( seed.Planet == entityDoingThePlacing.Planet )
                    {
                        invalidPlacement = true;
                        break;
                    }
                }
                if ( invalidPlacement )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("种子不能建造在已有种子或花朵的星球上", ChatType.LogToCentralChat, "CannotDoThatThing", null);
                    return;
                }
                if ( planet.GetControllingFaction().Type != FactionType.AI )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("种子必须建造在AI控制的星球上", ChatType.LogToCentralChat, "CannotDoThatThing", null);
                    return;
                }
            }
            if ( typeToPlace.GetHasTag("ScourgeFortress") )
            {
                if ( vassalBaseInfo.PlanetHasFortress( planet ))
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你在" + planet.Name + "上已经有一座堡垒", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }
                if ( typeToPlace.GetHasTag("ScourgeNeinzulFortress") && !empireBaseInfo.NeinzulWarriorUnlocked())
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你尚未解锁此种族。", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }
                if ( typeToPlace.GetHasTag("ScourgeNeinzulGreaterFortress") && !empireBaseInfo.NeinzulHybridUnlocked())
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你必须为此种族解锁科技等级2。", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }

                if ( typeToPlace.GetHasTag("ScourgeBurlustFortress") && !empireBaseInfo.BurlustWarriorUnlocked())
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你尚未解锁此种族。", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }
                if ( typeToPlace.GetHasTag("ScourgeBurlustGreaterFortress") && !empireBaseInfo.BurlustHybridUnlocked())
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你必须为此种族解锁科技等级2。", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }

                if ( typeToPlace.GetHasTag("ScourgeEvuckFortress") && !empireBaseInfo.EvuckWarriorUnlocked())
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你尚未解锁此种族。", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }
                if ( typeToPlace.GetHasTag("ScourgeEvuckGreaterFortress") && !empireBaseInfo.EvuckHybridUnlocked())
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你必须为此种族解锁科技等级2。", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }

                if ( typeToPlace.GetHasTag("ScourgeThoraxianFortress") && !empireBaseInfo.ThoraxianWarriorUnlocked())
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你尚未解锁此种族。", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }
                if ( typeToPlace.GetHasTag("ScourgeThoraxianGreaterFortress") && !empireBaseInfo.ThoraxianHybridUnlocked())
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你必须为此种族解锁科技等级2。", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }

                if ( typeToPlace.GetHasTag("ScourgePeltianFortress") && !empireBaseInfo.PeltianWarriorUnlocked())
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你尚未解锁此种族。", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }
                if ( typeToPlace.GetHasTag("ScourgePeltianGreaterFortress") && !empireBaseInfo.PeltianHybridUnlocked())
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你必须为此种族解锁科技等级2。", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }

                if ( typeToPlace.GetHasTag("ScourgeSpireFortress") && !empireBaseInfo.SpireWarriorUnlocked())
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你尚未解锁此种族。", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }
                if ( typeToPlace.GetHasTag("ScourgeSpireGreaterFortress") && !empireBaseInfo.SpireHybridUnlocked())
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你必须为此种族解锁科技等级2。", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }

                if ( typeToPlace.GetHasTag("ScourgeZenithFortress") && !empireBaseInfo.ZenithWarriorUnlocked())
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你尚未解锁此种族。", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }
                if ( typeToPlace.GetHasTag("ScourgeZenithGreaterFortress") && !empireBaseInfo.ZenithHybridUnlocked())
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你必须为此种族解锁科技等级2。", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }


            }
            if ( typeToPlace.GetHasTag("ScourgeSpawner"))
            {
                if ( planet.GetControllingFaction().GetIsHostileTowards(faction) )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你必须拥有此星球才能建造。", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }
                int hops = 2;
                if ( vassalBaseInfo.PlanetHasSpawner( planet, hops ))
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你不能在离另一个繁殖场如此近的地方建造繁殖场。", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }
                if ( vassalBaseInfo.PlanetHasArmory( planet ))
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你在此处已经有一座军械库。", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }
                if ( vassalBaseInfo.PlanetHasBestiary( planet ))
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你在此处已经有一座兽栏。", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }

            }
            if ( typeToPlace.GetHasTag("ScourgeArmory") || typeToPlace.GetHasTag("ScourgeBestiary"))
            {
                if ( planet.GetControllingFaction().GetIsHostileTowards(faction) )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你必须拥有此星球才能建造。", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }
                if ( vassalBaseInfo.PlanetHasSpawner( planet ))
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你在" + planet.Name + "上已经有一座繁殖场", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }
                if ( vassalBaseInfo.PlanetHasArmory( planet ))
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你在" + planet.Name + "上已经有一座军械库", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }
                if ( vassalBaseInfo.PlanetHasBestiary( planet ))
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你在" + planet.Name + "上已经有一座兽栏", ChatType.LogToCentralChat, "CannotDoThatThing", null);    
                    return;
                }
            }
            if ( faction.StoredFactionResourceOne < typeToPlace.CostInResourceOne )
            {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你的科博石不足。", ChatType.LogToCentralChat, "CannotDoThatThing", null);
                return;
            }
            if ( faction.StoredMetal < typeToPlace.GetForMark(1).MetalCost )
            {
                    World_AIW2.Instance.QueueChatMessageOrCommand("你的金属不足。", ChatType.LogToCentralChat, "CannotDoThatThing", null);
                return;
            }

            DeepInfo.SpawnScourgeStructure(command.RelatedPoints.First, entityDoingThePlacing.Planet, command.RelatedString2, faction, context.GetHostOnlyContext(), true );
            faction.StoredFactionResourceOne -= typeToPlace.CostInResourceOne;

            faction.StoredMetal -= typeToPlace.GetForMark(1).MetalCost;
        }
    }
    public class GameCommand_TransformScourgeStructure : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
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
            if ( command.RelatedBools.Count > 0 && command.RelatedBools.First )
            {
                //this is a markup request, so handle it here then return
                GameEntity_Squad structure = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );
                if ( structure == null )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( "建筑已死亡。",
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                    return;
                }

                ScourgeVassalFactionDeepInfo deepInfo = structure.PlanetFaction.Faction.GetExternalDeepInfoAs<ScourgeVassalFactionDeepInfo>();
                if ( deepInfo == null )
                    throw new Exception("No vassalBaseInfo");

                if ( structure.TypeData.GetHasTag("ScourgeArmory"))
                    deepInfo.UpgradeArmory( structure,  context.GetHostOnlyContext() );
                if ( structure.TypeData.GetHasTag("ScourgeFortress"))
                    deepInfo.UpgradeFortress( structure, context.GetHostOnlyContext() );
                if ( structure.TypeData.GetHasTag("ScourgeSpawner"))
                    deepInfo.UpgradeSpawner( structure,  context.GetHostOnlyContext() );
                if ( structure.TypeData.GetHasTag("ScourgeBestiary"))
                    deepInfo.UpgradeBestiary( structure,  context.GetHostOnlyContext() );

                return;
            }
            ScourgeTypeData typeData = ScourgeTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( typeData == null )
            {
                ArcenDebugging.ArcenDebugLog( "no typeData found!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            GameEntity_Squad armory = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );
            if ( armory == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "军械库已死亡。",
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            ScourgePerUnitBaseInfo armoryData = armory.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
            armoryData.ScourgeTypeId = typeData.id;
        }
    }
    public class GameCommand_BuildStarbase : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
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
            GameEntityTypeData typeToPlace = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( typeToPlace == null )
            {
                ArcenDebugging.ArcenDebugLog( "During placement: typeToPlace == null!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            GameEntity_Squad entityDoingThePlacing = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );
            if ( entityDoingThePlacing == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "建造源在建造开始前已死亡——无法建造此" + typeToPlace.GetDisplayName() + "。",
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( command.RelatedPoints == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedPoints == null in GameCommand_BuildStarbase", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( command.RelatedPoints.Count != 1 )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedPoints.Count != 1 in GameCommand_BuildStarbase", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }

            Faction faction = entityDoingThePlacing.PlanetFaction.Faction;
            ArmadaFactionBaseInfo BaseInfo = faction.GetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
            ArmadaFactionDeepInfo DeepInfo = faction.GetExternalDeepInfoAs<ArmadaFactionDeepInfo>();
            if ( BaseInfo.DoesPlanetHaveStarbase( entityDoingThePlacing.Planet ) )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "这里已经有一座星际基地——你无法再放置一座！", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( entityDoingThePlacing.Planet.GetControllingFaction().GetIsHostileTowards(faction) ) {
                World_AIW2.Instance.QueueChatMessageOrCommand( "该星球由敌人控制——你无法在此放置星际基地！", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
            }
            
            if ( faction.StoredFactionResourceOne < typeToPlace.CostInResourceOne )
            {
                PlayerTypeData pTypeData = faction.PlayerTypeDataOrNull_ModeratelyExpensive;
                if ( pTypeData != null )
                    World_AIW2.Instance.QueueChatMessageOrCommand( pTypeData.Resource1DisplayName + "不足；你需要" + typeToPlace.CostInResourceOne + "，而你的阵营" + faction.GetDisplayName() +
                      "仅有" + faction.StoredFactionResourceOne, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                else
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( "精华不足；你需要" + typeToPlace.CostInResourceOne + "，而你的阵营" + faction.GetDisplayName() +
                      "仅有" + faction.StoredFactionResourceOne, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                }
                return;
            }
            if ( faction.StoredMetal < typeToPlace.GetForMark(1).MetalCost )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "金属不足", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }

            int galaxyCap =  typeToPlace.CalculateEffectiveGalaxyWideCapForPlayersConstructing(faction);
            if ( galaxyCap > 0 )
            {
                int galaxyBuiltCount = 0;
                foreach ( GameEntity_Squad squad in faction.Squads( EntityRollupType.HasGalaxyWideCapForPlayersConstructing ) )
                {
                    if ( squad.TypeData.GalaxyWideCapMatchString == typeToPlace.GalaxyWideCapMatchString )
                        galaxyBuiltCount++;
                }
                if ( galaxyBuiltCount >= galaxyCap )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( "你已达到" + typeToPlace.GetDisplayName() + "的银河系上限。你需要占领更多星球。", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                    return;
                }
            }
            // TODO: This should maybe pass in an arguement to make the city
            // start out unbuilt, rather than completely built.
            // Though, in that case, should the fleet be created immediately.
            DeepInfo.SpawnArmadaStarbase(command.RelatedPoints.First, entityDoingThePlacing.Planet, command.RelatedString2, faction, context.GetHostOnlyContext(), out GameEntity_Squad unusedNecroFlag, true );
            faction.StoredFactionResourceOne -= typeToPlace.CostInResourceOne;
            faction.StoredMetal -= typeToPlace.GetForMark(1).MetalCost;
        }
    }
    public class GameCommand_BuildMine : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
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
            GameEntityTypeData typeToPlace = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( typeToPlace == null )
            {
                ArcenDebugging.ArcenDebugLog( "During placement: typeToPlace == null!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            GameEntity_Squad entityDoingThePlacing = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );
            if ( entityDoingThePlacing == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "建造源在建造开始前已死亡——无法建造此" + typeToPlace.GetDisplayName() + "。",
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( command.RelatedPoints == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedPoints == null in GameCommand_BuildMine", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( command.RelatedPoints.Count != 1 )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedPoints.Count != 1 in GameCommand_BuildMine", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if (entityDoingThePlacing.Planet.IsRavaged)
            {
                World_AIW2.Instance.QueueChatMessageOrCommand("此星球已被蹂躏。", ChatType.ShowLocallyOnly, "CannotDoThatThing", null);
                return;
            }
            Faction faction = entityDoingThePlacing.PlanetFaction.Faction;
            ArmadaFactionBaseInfo BaseInfo = faction.GetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
            ArmadaFactionDeepInfo DeepInfo = faction.GetExternalDeepInfoAs<ArmadaFactionDeepInfo>();

            int eligibleTime = BaseInfo.IsPlanetMiningEligible(entityDoingThePlacing.Planet);
            if ( eligibleTime != -1 )
            {
                int secondsLeft = eligibleTime - World_AIW2.Instance.GameSecond;
                World_AIW2.Instance.QueueChatMessageOrCommand("星球" + entityDoingThePlacing.Planet.Name + "刚被舰队开采过不久；你必须等待" + secondsLeft + "秒。", ChatType.LogToCentralChat, "CannotDoThatThing", null);
                return;

            }
            if ( faction.StoredMetal < typeToPlace.GetForMark(1).MetalCost )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "金属不足", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( typeToPlace.CostInResourceOne > 0 &&
                 faction.StoredFactionResourceOne < typeToPlace.CostInResourceOne )
            {
                PlayerTypeData pTypeData = faction.PlayerTypeDataOrNull_ModeratelyExpensive;
                if ( pTypeData != null )
                    World_AIW2.Instance.QueueChatMessageOrCommand( pTypeData.Resource1DisplayName + "不足；你需要" + typeToPlace.CostInResourceOne + "，而你的阵营" + faction.GetDisplayName() +
                      "仅有" + faction.StoredFactionResourceOne, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                else
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( "精华不足；你需要" + typeToPlace.CostInResourceOne + "，而你的阵营" + faction.GetDisplayName() +
                      "仅有" + faction.StoredFactionResourceOne, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                }
                return;
            }

            DeepInfo.SpawnArmadaMine(command.RelatedPoints.First, entityDoingThePlacing.Planet, command.RelatedString2, faction, context.GetHostOnlyContext(), true );
            faction.StoredFactionResourceOne -= typeToPlace.CostInResourceOne;
            faction.StoredMetal -= typeToPlace.GetForMark(1).MetalCost;
        }
    }
    public class GameCommand_BuildArmadaStructure : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
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
            GameEntityTypeData typeToPlace = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( typeToPlace == null )
            {
                ArcenDebugging.ArcenDebugLog( "During placement: typeToPlace == null!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            GameEntity_Squad entityDoingThePlacing = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );
            if ( entityDoingThePlacing == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "建造源在建造开始前已死亡——无法建造此" + typeToPlace.GetDisplayName() + "。",
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( command.RelatedPoints == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedPoints == null in GameCommand_BuildArmadaStructure", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( command.RelatedPoints.Count != 1 )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedPoints.Count != 1 in GameCommand_BuildArmadaStructure", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if (entityDoingThePlacing.Planet.IsRavaged)
            {
                World_AIW2.Instance.QueueChatMessageOrCommand("此星球已被蹂躏。", ChatType.ShowLocallyOnly, "CannotDoThatThing", null);
                return;
            }
            Faction faction = entityDoingThePlacing.PlanetFaction.Faction;
            ArmadaFactionBaseInfo BaseInfo = faction.GetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
            ArmadaFactionDeepInfo DeepInfo = faction.GetExternalDeepInfoAs<ArmadaFactionDeepInfo>();

            // int eligibleTime = BaseInfo.IsPlanetMiningEligible(entityDoingThePlacing.Planet);
            // if ( eligibleTime != -1 )
            // {
            //     int secondsLeft = eligibleTime - World_AIW2.Instance.GameSecond;
            //     World_AIW2.Instance.QueueChatMessageOrCommand("This planet has been mined too recently; you must wait " + secondsLeft + " seconds.", ChatType.ShowLocallyOnly, "CannotDoThatThing", null);
            //     return;

            // }
            bool failLure = false;
            foreach ( GameEntity_Squad lure in BaseInfo.SwarmLures.DisplaySquads() )
            {
                if ( !typeToPlace.GetHasTag("SwarmLure"))
                    break;
                if ( lure.Planet == entityDoingThePlacing.Planet )
                {
                    failLure = true;
                    break;
                }
            }
            if ( failLure)
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "一个星球不能有两个虫群诱饵", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( faction.StoredMetal < typeToPlace.GetForMark(1).MetalCost )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "金属不足", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( typeToPlace.CostInResourceOne > 0 &&
                 faction.StoredFactionResourceOne < typeToPlace.CostInResourceOne )
            {
                PlayerTypeData pTypeData = faction.PlayerTypeDataOrNull_ModeratelyExpensive;
                if ( pTypeData != null )
                    World_AIW2.Instance.QueueChatMessageOrCommand( pTypeData.Resource1DisplayName + "不足；你需要" + typeToPlace.CostInResourceOne + "来建造" + typeToPlace.ToString() + "，而你的阵营" + faction.GetDisplayName() +
                      "仅有" + faction.StoredFactionResourceOne, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                else
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( "精华不足；你需要" + typeToPlace.CostInResourceOne + "，而你的阵营" + faction.GetDisplayName() +
                      "仅有" + faction.StoredFactionResourceOne, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                }
                return;
            }

            DeepInfo.SpawnArmadaStructure(command.RelatedPoints.First, entityDoingThePlacing.Planet, command.RelatedString2, faction, context.GetHostOnlyContext(), true );
            faction.StoredFactionResourceOne -= typeToPlace.CostInResourceOne;
            faction.StoredMetal -= typeToPlace.GetForMark(1).MetalCost;
        }
    }
    public class GameCommand_BuildZiggurat : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
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
            GameEntityTypeData typeToPlace = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( typeToPlace == null )
            {
                ArcenDebugging.ArcenDebugLog( "During placement: typeToPlace == null!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            GameEntity_Squad entityDoingThePlacing = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );
            if ( entityDoingThePlacing == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "建造源在建造开始前已死亡——无法建造此" + typeToPlace.GetDisplayName() + "。",
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( command.RelatedPoints == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedPoints == null in GameCommand_BuildStarbase", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( command.RelatedPoints.Count != 1 )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "command.RelatedPoints.Count != 1 in GameCommand_BuildStarbase", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }

            Faction faction = entityDoingThePlacing.PlanetFaction.Faction;
            ApkalluFactionBaseInfo BaseInfo = faction.GetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            ApkalluFactionDeepInfo DeepInfo = faction.GetExternalDeepInfoAs<ApkalluFactionDeepInfo>();
            if ( BaseInfo.DoesPlanetHaveZiggurat( entityDoingThePlacing.Planet ) )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "这里已经有一座星际基地——你无法再放置一座！", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( entityDoingThePlacing.Planet.GetControllingFaction().GetIsHostileTowards(faction) ) {
                World_AIW2.Instance.QueueChatMessageOrCommand( "该星球由敌人控制——你无法在此放置星际基地！", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
            }
            
            if ( faction.StoredFactionResourceOne < typeToPlace.CostInResourceOne )
            {
                PlayerTypeData pTypeData = faction.PlayerTypeDataOrNull_ModeratelyExpensive;
                if ( pTypeData != null )
                    World_AIW2.Instance.QueueChatMessageOrCommand( pTypeData.Resource1DisplayName + "不足；你需要" + typeToPlace.CostInResourceOne + "，而你的阵营" + faction.GetDisplayName() +
                      "仅有" + faction.StoredFactionResourceOne, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                else
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( "精华不足；你需要" + typeToPlace.CostInResourceOne + "，而你的阵营" + faction.GetDisplayName() +
                      "仅有" + faction.StoredFactionResourceOne, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                }
                return;
            }
            if ( faction.StoredMetal < typeToPlace.GetForMark(1).MetalCost )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "金属不足", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }

            int galaxyCap =  typeToPlace.CalculateEffectiveGalaxyWideCapForPlayersConstructing(faction);
            if ( galaxyCap > 0 )
            {
                int galaxyBuiltCount = 0;
                foreach ( GameEntity_Squad squad in faction.Squads( EntityRollupType.HasGalaxyWideCapForPlayersConstructing ) )
                {
                    if ( squad.TypeData.GalaxyWideCapMatchString == typeToPlace.GalaxyWideCapMatchString )
                        galaxyBuiltCount++;
                }
                if ( galaxyBuiltCount >= galaxyCap )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( "你已达到" + typeToPlace.GetDisplayName() + "的银河系上限。你需要占领更多星球。", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                    return;
                }
            }
            // TODO: This should maybe pass in an arguement to make the city
            // start out unbuilt, rather than completely built.
            // Though, in that case, should the fleet be created immediately.
            DeepInfo.SpawnApkalluZiggurat(command.RelatedPoints.First, entityDoingThePlacing.Planet, command.RelatedString2, faction, context.GetHostOnlyContext(), out GameEntity_Squad unusedNecroFlag, true );
            faction.StoredFactionResourceOne -= typeToPlace.CostInResourceOne;
            faction.StoredMetal -= typeToPlace.GetForMark(1).MetalCost;
        }
    }

    public class GameCommand_BuildDuru : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            if ( command.RelatedEntityIDs == null || command.RelatedEntityIDs.Count <= 0 )
            {
                ArcenDebugging.ArcenDebugLog( "BuildDuru: No RelatedEntityIDs!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            GameEntityTypeData typeToPlace = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( command.RelatedString2 );
            if ( typeToPlace == null )
            {
                ArcenDebugging.ArcenDebugLog( "BuildDuru: typeToPlace == null!" + command.WriteToStringInefficient( true, true, true ), Verbosity.ShowAsError );
                return;
            }
            GameEntity_Squad entityDoingThePlacing = World_AIW2.Instance.GetEntityByID_Squad( command.RelatedEntityIDs.First );
            if ( entityDoingThePlacing == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "建造源在建造开始前已死亡——无法建造此" + typeToPlace.GetDisplayName() + "。",
                    ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( command.RelatedPoints == null || command.RelatedPoints.Count != 1 )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "GameCommand_BuildDuru中RelatedPoints无效", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }

            Faction faction = entityDoingThePlacing.PlanetFaction.Faction;
            ApkalluFactionBaseInfo BaseInfo = faction.GetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            ApkalluFactionDeepInfo DeepInfo = faction.GetExternalDeepInfoAs<ApkalluFactionDeepInfo>();
            if ( BaseInfo.DoesPlanetHaveDuru( entityDoingThePlacing.Planet ) )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "这里已经有一座杜鲁——你无法再放置一座！", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( entityDoingThePlacing.Planet.GetControllingFaction().GetIsHostileTowards( faction ) )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "该星球由敌人控制——你无法在此放置杜鲁！", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( faction.StoredFactionResourceOne < typeToPlace.CostInResourceOne )
            {
                PlayerTypeData pTypeData = faction.PlayerTypeDataOrNull_ModeratelyExpensive;
                string resourceName = pTypeData != null ? pTypeData.Resource1DisplayName : "精华";
                World_AIW2.Instance.QueueChatMessageOrCommand( resourceName + "不足；你需要" + typeToPlace.CostInResourceOne +
                    "，而你的阵营仅有" + faction.StoredFactionResourceOne, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }
            if ( faction.StoredMetal < typeToPlace.GetForMark(1).MetalCost )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "金属不足", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                return;
            }

            DeepInfo.SpawnApkalluDuru( command.RelatedPoints.First, entityDoingThePlacing.Planet, command.RelatedString2, faction, context.GetHostOnlyContext(), true );
            faction.StoredFactionResourceOne -= typeToPlace.CostInResourceOne;
            faction.StoredMetal -= typeToPlace.GetForMark(1).MetalCost;
        }
    }
}
