using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class AIObjectivesGenerator
    {
        public static void CheckForAIObjectives_BackgroundThread_ClientOrHost()
        {
            GenerateProgressReducerObjectives();
            GenerateShipGrantingStructureObjectives();
            GenerateShipLineCapIncreasingStructureObjectives();
            //GenerateCPAStrengtheningObjectives(); //honestly, the CPA bunkers aren't that important
            //            GenerateAIHackableObjectives();
        }
        private static void GenerateProgressReducerObjectives()
        {
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "DestroyToReduceProgress" ) )
            {
                if(entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored)
                    continue;
                {
                    #region Reduce Progress
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "DestroyToReduceProgress" );
                    objective.DisplayNameBase = "摧毁 ";
                    objective.RelatedEntity1 = entity;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "HoldToReduceProgressLoseToRaiseProgress" ) )
            {
                if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                {
                    #region Reduce Progress
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "HoldToReduceAIPLoseToRaiseAIP" );
                    if ( entity.GetFactionTypeSafe() == FactionType.Player )
                        objective.DisplayNameBase = "坚守 ";
                    else
                        objective.DisplayNameBase = "占领并坚守 ";

                    objective.RelatedEntity1 = entity;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "HackToReduceProgress" ) )
            {
                if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                {
                    #region Reduce Progress
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "HackToReduceProgress" );
                    objective.DisplayNameBase = "入侵 ";
                    objective.RelatedEntity1 = entity;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }
        private static void GenerateCPAStrengtheningObjectives()
        {
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "StrengthenCPA" ) )
            {
                if(entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored)
                    continue;
                {
                    #region Reduce Progress
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "DestroyToWeakenCPA" );
                    objective.DisplayNameBase = "摧毁 ";
                    objective.RelatedEntity1 = entity;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }
        private static void GenerateShipGrantingStructureObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( playerFaction ) )
                return;

            //Faction neutralFaction = World_AIW2.Instance.GetNeutralFaction();
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "HackForShipType" ) )
            {
                int debugStage = 0;
                try
                {
                    //Some things granting ship types are minor faction (Dyson Sphere), others are neutral (schematic server),
                    //others are AI owned (design template server)
                    if ( entity.GetFactionTypeSafe() == FactionType.Player )
                        continue;
                    debugStage = 100;

                    if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                        continue;
                    debugStage = 200;

                    {
                        debugStage = 300;
                        #region Hack for ship type
                        {
                            ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                            objective.SetHook( "HackShipGranter" );
                            objective.DisplayNameBase = "入侵 ";
                            objective.RelatedEntity1 = entity;
                            ObjectiveCategory.AddActualObjective( objective );
                        }
                        #region Check for optional objective types that have been enabled in settings to show specific unit types (aoe, sniper, melee, etc)
                        foreach ( ObjectiveCategory objectiveCategory in ObjectiveCategoryTable.Instance.Rows )
                        {
                            if ( !objectiveCategory.IsFilteredUnitAquisitionObjective )
                                continue;
                            bool foundMatchingUnitType = false;
                            for ( int i = 0; i < entity.ShipGrantsList.Count; i++ )
                            {
                                GameEntityTypeData entityType = entity.ShipGrantsList[i].TypeData;
                                if ( !GetEntityMatchesObjectiveCategory( entityType, objectiveCategory ) )
                                    continue;
                                foundMatchingUnitType = true;
                                break;
                            }
                            if ( !foundMatchingUnitType )
                                continue;
                            ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                            objective.SetHook( objectiveCategory.InternalName );
                            objective.DisplayNameBase = "入侵 ";
                            objective.RelatedEntity1 = entity;
                            ObjectiveCategory.AddActualObjective( objective );
                        }
                        #endregion
                        #endregion
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "GenerateShipGrantingStructureObjectives HackForShipType exception at debugStage " +debugStage + ": " + e.ToString(), Verbosity.ShowAsError );
                }
            }
        }
        
        public static bool GetEntityMatchesObjectiveCategory( GameEntityTypeData entityType, ObjectiveCategory objectiveCategory )
        {
            if ( entityType == null )
                return false;
            bool? result = objectiveCategory.CachedShipTypeMembership[entityType];
            if ( result == null )
                result = objectiveCategory.CachedShipTypeMembership[entityType] = SemiSlow_ComputeEntityMatchesObjectiveCategory( entityType, objectiveCategory );
            return result.Value;
        }

        private static bool SemiSlow_ComputeEntityMatchesObjectiveCategory( GameEntityTypeData entityType, ObjectiveCategory objectiveCategory )
        {
            if ( objectiveCategory.IsForSystemsWith_ShotTypes.Count > 0 )
            {
                bool foundMatch = false;
                for ( int i = 0; i < entityType.SystemTypes.Count; i++ )
                {
                    EntitySystemTypeData systemType = entityType.SystemTypes[i];
                    if ( systemType.ShotTypeData == null )
                        continue;
                    if ( !objectiveCategory.IsForSystemsWith_ShotTypes.Contains( systemType.ShotTypeData ) )
                        continue;
                    foundMatch = true;
                    break;
                }
                if ( !foundMatch )
                    return false;
            }
            if ( objectiveCategory.IsForShipsWith_Rollups.Count > 0 )
            {
                bool foundMatch = false;
                for ( int i = 0; i < objectiveCategory.IsForShipsWith_Rollups.Count; i++ )
                {
                    EntityRollupType rollup = objectiveCategory.IsForShipsWith_Rollups[i];
                    if ( !entityType.GetMatches_SemiSlow( rollup ) )
                        continue;
                    foundMatch = true;
                    break;
                }
                if ( !foundMatch )
                    return false;
            }
            if ( objectiveCategory.IsForShipsMatchingFilterTypes.Count > 0 )
            {
                bool foundMatch = false;
                for ( int i = 0; i < objectiveCategory.IsForShipsMatchingFilterTypes.Count; i++ )
                {
                    ObjectiveEntityTypeFilter filter = objectiveCategory.IsForShipsMatchingFilterTypes[i];
                    switch ( filter )
                    {
                        case ObjectiveEntityTypeFilter.EngineDamage:
                        {
                            bool foundMatchingSystem = false;
                            for ( int systemIndex = 0; systemIndex < entityType.SystemTypes.Count; systemIndex++ )
                            {
                                EntitySystemTypeData systemData = entityType.SystemTypes[systemIndex];
                                if ( systemData.GetBaseMark().EngineStunPerShot <= 0 )
                                    continue;
                                foundMatchingSystem = true;
                                break;
                            }
                            if ( !foundMatchingSystem )
                                continue;
                        }
                            break;
                        case ObjectiveEntityTypeFilter.AreaOfEffect:
                        {
                            bool foundMatchingSystem = false;
                            for ( int systemIndex = 0; systemIndex < entityType.SystemTypes.Count; systemIndex++ )
                            {
                                EntitySystemTypeData systemData = entityType.SystemTypes[systemIndex];
                                if ( systemData.GetBaseMark().ShotAreaOfEffect <= 0 )
                                    continue;
                                foundMatchingSystem = true;
                                break;
                            }
                            if ( !foundMatchingSystem )
                                continue;
                        }
                            break;
                        case ObjectiveEntityTypeFilter.DroneLauncher:
                        {
                            bool foundMatchingSystem = false;
                            for ( int systemIndex = 0; systemIndex < entityType.SystemTypes.Count; systemIndex++ )
                            {
                                EntitySystemTypeData systemData = entityType.SystemTypes[systemIndex];
                                if ( systemData.ShotTypeData == null || systemData.ShotTypeData.Category != GameEntityCategory.Ship )
                                    continue;
                                foundMatchingSystem = true;
                                break;
                            }
                            if ( !foundMatchingSystem )
                                continue;
                        }
                            break;
                        case ObjectiveEntityTypeFilter.ParalysisDamage:
                        {
                            bool foundMatchingSystem = false;
                            for ( int systemIndex = 0; systemIndex < entityType.SystemTypes.Count; systemIndex++ )
                            {
                                EntitySystemTypeData systemData = entityType.SystemTypes[systemIndex];
                                if ( systemData.GetBaseMark().ParalysisSecondsPerShot <= 0 )
                                    continue;
                                foundMatchingSystem = true;
                                break;
                            }
                            if ( !foundMatchingSystem )
                                continue;
                        }
                            break;
                        default:
                            ArcenDebugging.ArcenDebugLogSingleLine( "Unimplemented ObjectiveEntityTypeFilter." + filter, Verbosity.ShowAsError );
                            continue;
                    }
                    foundMatch = true;
                    break;
                }
                if ( !foundMatch )
                    return false;
            }
            if ( objectiveCategory.IsForShips_WithDamageModifierBasedOn.Count > 0 )
            {
                bool foundMatch = false;
                for ( int i = 0; i < objectiveCategory.IsForShips_WithDamageModifierBasedOn.Count; i++ )
                {
                    DamageModifierBasedOn modifierType = objectiveCategory.IsForShips_WithDamageModifierBasedOn[i];
                    bool foundMatchingSystem = false;
                    for ( int systemIndex = 0; systemIndex < entityType.SystemTypes.Count; systemIndex++ )
                    {
                        EntitySystemTypeData systemData = entityType.SystemTypes[systemIndex];
                        for ( int modifierIndex = 0; modifierIndex < systemData.OutgoingDamageModifiers_FullList.Count; modifierIndex++ )
                        {
                            DamageModifier modifier = systemData.OutgoingDamageModifiers_FullList[modifierIndex];
                            if ( modifier.BasedOn != modifierType )
                                continue;
                            if ( objectiveCategory.IsForShips_WithDamageModifierComparisonType.Count > 0 && !objectiveCategory.IsForShips_WithDamageModifierComparisonType.Contains( modifier.ComparisonType ) )
                                continue;
                            foundMatchingSystem = true;
                            break;
                        }
                        if ( foundMatchingSystem )
                            break;
                    }
                    if ( !foundMatchingSystem )
                        continue;
                    foundMatch = true;
                    break;
                }
                if ( !foundMatch )
                    return false;
            }
            if ( objectiveCategory.BypassesPersonalShieldsGreaterThanOrEqualTo > 0 )
            {
                bool foundMatch = false;
                for ( int systemIndex = 0; systemIndex < entityType.SystemTypes.Count; systemIndex++ )
                {
                    EntitySystemTypeData systemData = entityType.SystemTypes[systemIndex];
                    if ( systemData.GetBaseMark().PercentDamageBypassesPersonalShields < objectiveCategory.BypassesPersonalShieldsGreaterThanOrEqualTo )
                        continue;
                    foundMatch = true;
                    break;
                }
                if ( !foundMatch )
                    return false;
            }
            if ( objectiveCategory.SpeedGreaterThanOrEqualTo != null && entityType.BaseMark.Speed < objectiveCategory.SpeedGreaterThanOrEqualTo.Speed )
                return false;
            return true;
        }

        private static void GenerateShipLineCapIncreasingStructureObjectives()
        {
            //Faction neutralFaction = World_AIW2.Instance.GetNeutralFaction();
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( playerFaction ) )
                return;

            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "HackForShipTypeCapIncrease" ) )
            {
            int debugStage = 0;
                try
                {
                    //Some things granting ship types are minor faction (Dyson Sphere), others are neutral (schematic server),
                    //others are AI owned (design template server)
                    if ( entity.GetFactionTypeSafe() == FactionType.Player )
                        continue;
                    debugStage = 100;

                    if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                        continue;
                    debugStage = 200;

                    {
                        #region Hack for ship line cap increase
                        debugStage = 300;
                        ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "HackShipCapLineGranter" );
                        objective.DisplayNameBase = "入侵 ";
                        objective.RelatedEntity1 = entity;
                        ObjectiveCategory.AddActualObjective( objective );
                        #endregion
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "GenerateShipLineCapIncreasingStructureObjectives HackForShipTypeCapIncrease exception at debugStage " + debugStage + ": " + e.ToString(), Verbosity.ShowAsError );
                }
            }
        }
    }



    public class DestroyToReduceProgress : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            GameEntity_Squad entity = Objective.RelatedEntity1;
            string color = entity.GetFactionCenterColorHexBrighter_Safe();
            if ( entity.TypeData.AIPOnDeathWhenNoneLeft != 0 )
            {
                //for co-processors, or the like
                //This is a big performance hit, but it's only while hovering a tooltip

                int numEntitiesLeft = 0;

                foreach ( GameEntity_Squad otherEntity in World_AIW2.Instance.Squads( EntityRollupType.AIPOnDeathWhenNoneLeft ) )
                {
                    if ( otherEntity.TypeData != entity.TypeData )
                        continue;
                    if ( otherEntity.HasAlreadyDoneOnFirstDeath )
                        continue;
                    numEntitiesLeft++;
                }

                buffer.AddObjectiveEntityHeader( entity, color );
                buffer.Add( "减少AI进程对你的生存至关重要。摧毁所有此类建筑（包括" ).Add( entity.GetPlanetName_Safe(), color ).Add( "上的这个）将使AI进程减少" ).Add( "" + -entity.TypeData.AIPOnDeathWhenNoneLeft, ObjectiveColors.Reward ).Add( "。" );
                if ( numEntitiesLeft == 1 )
                    buffer.Add( "这是最后一个；摧毁它将触发AI进程减少。" );
                else
                    buffer.Add( "你必须摧毁所有剩余" ).Add( numEntitiesLeft, ObjectiveColors.Reward ).Add( "个此类建筑后，AI进程才会减少。" );
            }
            else
            {
                buffer.AddObjectiveEntityHeader( entity, color );
                buffer.Add( "减少AI进程对你的生存至关重要。摧毁" + entity.GetPlanetName_Safe() +
                           "上的它将使AI进程减少" ).Add( "" + -entity.TypeData.AIPOnDeath, ObjectiveColors.Reward ).Add( "。" );
            }
        }
    }
    public class DestroyToWeakenCPA : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            GameEntity_Squad entity = Objective.RelatedEntity1;
            string color = entity.GetFactionCenterColorHexBrighter_Safe();

            buffer.AddObjectiveEntityHeader( entity, color );
            buffer.Add( "跨星球攻击是AI单位攻击你星球的强大浪潮。摧毁所有此类建筑（包括" ).Add( entity.GetPlanetName_Safe(), color ).Add( "上的这个）将削弱下一次CPA。" );
        }
    }

    public class HoldToReduceAIPLoseToRaiseAIP : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedEntity1.GetFactionTypeSafe() == FactionType.Player && Objective.RelatedEntity1.TypeData.AIPOnDeath > 0 )
            {
                buffer.AddObjectiveEntityHeader( Objective.RelatedEntity1, Objective.RelatedEntity1.Planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter );
                buffer.Add( "减少AI进程对你的生存至关重要。如果AI摧毁了" ).Add( Objective.RelatedEntity1.GetPlanetName_Safe(), Objective.RelatedEntity1.Planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( "上的它，则AIP将增加" ).Add( Objective.RelatedEntity1.TypeData.AIPOnDeath, ObjectiveColors.AIP ).Add( "。" );
                if ( Objective.RelatedEntity1.TypeData.GetHasTag( "GeneratesBonusHunterShips" ) )
                    buffer.Add( "AI视此为有价值的目標，可能会投入大量猎杀舰队资源来摧毁它。" );
            }
            else
            {
                //you haven't captured this yet
                buffer.AddObjectiveEntityHeader( Objective.RelatedEntity1, Objective.RelatedEntity1.Planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter );
                buffer.Add( "减少AI进程对你的生存至关重要。占领并守住" ).Add( Objective.RelatedEntity1.GetPlanetName_Safe(), Objective.RelatedEntity1.Planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( "将使AI进程减少" ).Add( "" + -Objective.RelatedEntity1.TypeData.AIPToClaim, ObjectiveColors.Reward ).Add( "。" );
                if ( Objective.RelatedEntity1.TypeData.AIPOnDeath > 0 )
                {
                    buffer.Add( "然而，AI会在你占领后开始试图摧毁此建筑。如果成功，AI进程将增加" ).Add( Objective.RelatedEntity1.TypeData.AIPOnDeath, ObjectiveColors.AIP ).Add( "。" );
                }
            }
        }
    }

    public class HackToReduceProgress : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            //GameEntity_Squad entity = Objective.RelatedEntity1;
            buffer.AddObjectiveEntityHeader( Objective.RelatedEntity1, Objective.RelatedEntity1.Planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter );
            buffer.Add( "减少AI进程对你的生存至关重要。入侵" ).Add( Objective.RelatedEntity1.GetPlanetName_Safe(), Objective.RelatedEntity1.Planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter )
                .Add( "上的它将减少AI进程" );
        }
    }

    public class HackShipGranter : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedEntity1 == null )
            {
                buffer.Add( "Bug in ship granter: null entity" );
                return;
            }
            int debugStage = 0;
            try
            {
                debugStage = 100;
                if ( Objective.RelatedEntity1.TypeData.GetHasTag( "DysonSphere" ) )
                {
                    debugStage = 200;
                    buffer.AddObjectiveEntityHeader( Objective.RelatedEntity1, Objective.RelatedEntity1.GetFactionCenterColorHexBrighter_Safe() ).Add( "入侵" + Objective.RelatedEntity1.GetPlanetName_Safe() + "上的它将获得一种新单位类型。入侵戴森球很危险，不应轻率行事。" );
                }
                else if ( Objective.RelatedEntity1.TypeData.GetHasTag( "VengeanceGenerator" ) )
                {
                    debugStage = 300;
                    buffer.AddObjectiveEntityHeader( Objective.RelatedEntity1, Objective.RelatedEntity1.GetFactionCenterColorHexBrighter_Safe() ).Add( "入侵" + Objective.RelatedEntity1.GetPlanetName_Safe() + "上的它将获得一种新单位类型。入侵黑暗尖塔极其危险，不应轻率行事。" );
                }
                else
                {
                    debugStage = 400;
                    Faction controllingOrInfluencing = Objective.RelatedEntity1.Planet.GetControllingOrInfluencingFaction();
                    debugStage = 410;
                    buffer.AddObjectiveEntityHeader( Objective.RelatedEntity1, Objective.RelatedEntity1.GetFactionCenterColorHexBrighter_Safe() ).Add( "入侵" )
                        .Add( Objective.RelatedEntity1.GetPlanetName_Safe(), controllingOrInfluencing == null ? "ffffff" : controllingOrInfluencing.FactionCenterColor.ColorHexBrighter ).Add( "上的它将获得" );

                    debugStage = 600;
                    if ( Objective.RelatedEntity1.ShipGrantsList.Count > 1 )
                    {
                        debugStage = 700;
                        if ( Objective.RelatedEntity1.TypeData.GrantsStuffToBeAddedToPlayerFleets )
                            buffer.Add( "以下之一（你可选择一种）：\n " );
                    }
                    debugStage = 800;
                    ShipLineEntry entry = null;
                    for ( int i = 0; i < Objective.RelatedEntity1.ShipGrantsList.Count; i++ )
                    {
                        debugStage = 900;
                        entry = Objective.RelatedEntity1.ShipGrantsList[i];
                        debugStage = 1100;
                        if ( Objective.Hook.Category != null && !AIObjectivesGenerator.GetEntityMatchesObjectiveCategory( entry.TypeData, Objective.Hook.Category ) )
                            continue;
                        debugStage = 1200;
                        debugStage = 1300;
                        Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( playerFaction == null )
                            playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                        debugStage = 1400;
                        byte mark = playerFaction.GetGlobalMarkLevelForShipLine( entry.TypeData );
                        debugStage = 1500;
                        GameEntityTypeData.MarkLevelStats markStats = entry.TypeData.MarkStatsFor( mark );
                        debugStage = 1600;
                        int numShips = entry.GetNumShipsForHackAndHacker( null, null );
                        debugStage = 1600;
                        buffer.AddShipIconInline(entry.TypeData, playerFaction, TextStyle.Ship_Sprite_Ency ).Add( "  " );

                        buffer.Add( "<color=#" + markStats.MarkLevel.ColorHex + ">" ).Add( entry.TypeData == null ? "null" : entry.TypeData.GetDisplayName() )
                            .Add( " " ).Add( markStats.MarkLevel.MapDisplay ).Add( "</color>" ).Add( " x" )
                            //Chris says: these are theoretical adds, so this is the base cap and that gets marked up.  This is okay.
                            .Add( entry.TypeData.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( entry.TypeData, numShips, numShips, markStats.MarkLevel ).ToString(), entry.GetColorForShipLineScore() );

                        buffer.Add("\n");
                        List<TechUpgrade> techsForLine = entry.TypeData.TechUpgradesThatBenefitMe;
                        if ( techsForLine != null && techsForLine.Count > 0 )
                        {
                            buffer.Add( "         Techs: ", "ffeecc" );
                            for ( int t = 0; t < techsForLine.Count; t++ )
                            {
                                if ( t > 0 ) buffer.Add( ", " );
                                TechUpgrade tech = techsForLine[t];
                                int upgradesSoFar = playerFaction == null ? 0 : playerFaction.TechUnlocks[tech.RowIndexNonSim] + playerFaction.FreeTechUnlocks[tech.RowIndexNonSim];
                                int upgradeIndex = upgradesSoFar < 0 ? 0 : upgradesSoFar + 1;
                                if ( upgradeIndex >= Balance_MarkLevelTable.Instance.RowsByOrdinal.Length )
                                    upgradeIndex = Balance_MarkLevelTable.Instance.RowsByOrdinal.Length - 1;
                                buffer.Add( tech.DisplayName, Balance_MarkLevelTable.Instance.RowsByOrdinal[upgradeIndex].ColorHex );
                            }
                            buffer.Add( "\n" );
                        }
                        buffer.Add( "\n" );
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in HackShipGranter.TooltipHandler debugStage " + debugStage + " Exception: " + e, Verbosity.ShowAsError );
            }
        }
    }

    public class HackShipCapLineGranter : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedEntity1 == null )
            {
                buffer.Add( "Bug in ship granter: null entity" );
                return;
            }
            buffer.Add( "入侵" ).Add( Objective.RelatedEntity1.TypeData.GetDisplayName(), Objective.RelatedEntity1.GetFactionCenterColorHexBrighter_Safe() ).Add( "上的" ).Add( Objective.RelatedEntity1.GetPlanetName_Safe(), Objective.RelatedPlanet1.GetControllingFaction().FactionCenterColor.ColorHexBrighter )
                .Add( "将使执行入侵的舰队中的一条单位线容量翻倍（你可选择哪条线）。" );
        }
    }
}
