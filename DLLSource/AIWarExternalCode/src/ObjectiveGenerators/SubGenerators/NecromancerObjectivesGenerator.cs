using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class NecromancerObjectivesGenerator
    {
        public static void CheckForNecromancerObjectives_BackgroundThread_ClientOrHost()
        {
            try
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( !NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( localFaction ) )
                    return;
                GenerateRiftObjectives();
                GenerateConstructorObjectives();
                GenerateAmplifierObjectives();
                GenerateNecromancerBeginnerObjectives();
                // GenerateZenithPowerGeneratorObjectives();
                // GenerateZenithMatterConverterObjectives();
                // GenerateGrantsAddedToCommandStationObjectives();
                // GenerateSpireArchiveObjectives();
                // GenerateAcquireHackingObjectives();
                // GenerateAcquireScienceByDestructionObjectives();
                // GenerateAcquireHackingByDestructionObjectives();
                // GenerateAcquireTechObjectives();
                // GenerateAcquireScienceAndHackingByDestructionObjectives();            
            }
            catch (Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in NecromancerObjectivesGenerator generation: " + e, Verbosity.ShowAsError );
            }
}
        private static void GenerateNecromancerBeginnerObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( !NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( playerFaction ) )
                return;
            #region Hack Rifts
            ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
            objective.SetHook( "HackRifts" );
            ObjectiveCategory.AddActualObjective( objective );
            #endregion

            #region Hunt Elderlings
            objective = ActualObjective.GetFromPoolOrCreate();
            objective.SetHook( "HuntElderlings" );
            ObjectiveCategory.AddActualObjective( objective );
            #endregion

            #region Fight Templar
            objective = ActualObjective.GetFromPoolOrCreate();
            objective.SetHook( "FightTemplar" );
            ObjectiveCategory.AddActualObjective( objective );
            #endregion
            NecromancerEmpireFactionBaseInfo info = playerFaction.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
            if ( info != null )
            {
                if ( info.Necropoleis.Count < 3 )
                {
                    objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "BuildNecropolises" );
                    ObjectiveCategory.AddActualObjective( objective );
                }
                {
                    int upgrades = 0;
                    Planet firstPlanetWithUnusedHexes = null;
                    foreach ( GameEntity_Squad city in info.Necropoleis.DisplaySquads() )
                    {
                        if ( city.FleetMembership.Fleet == null )
                            continue;
                        if ( city.CurrentMarkLevel > 1 )
                            upgrades += city.CurrentMarkLevel - 1;
                        Fleet fleet = city.FleetMembership.Fleet;
                        if ( fleet == null )
                            continue;
                        if ( fleet.CalculateRemainingCitySockets() >= 1 && firstPlanetWithUnusedHexes == null )
                            firstPlanetWithUnusedHexes = city.Planet;
                    }
                    if ( firstPlanetWithUnusedHexes != null )
                    {
                        objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "UnusedHexes" );
                        objective.RelatedPlanet1 = firstPlanetWithUnusedHexes;
                        ObjectiveCategory.AddActualObjective( objective );
                    }
                    if ( upgrades <= 2 && (info.Necropoleis.Count >= 2 || World_AIW2.Instance.GameSecond > 600 ) )
                    {
                        //don't show the Upgrade Necropolis too early, make sure they've had a chance to build more necropolises first
                        objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "UpgradeNecropolises" );
                        ObjectiveCategory.AddActualObjective( objective );
                    }
                }
                //for flagships
                {
                    int upgrades = 0;
                    bool foundAnyVariants = false;
                    foreach ( GameEntity_Squad flagship in info.Flagships.DisplaySquads() )
                    {
                        if ( !flagship.TypeData.GetHasTag("NecroBaseFlagship") )
                            foundAnyVariants = true;
                        if ( flagship.CurrentMarkLevel > 1 )
                            upgrades += flagship.CurrentMarkLevel - 1;
                    }
                    if ( upgrades > 3 && !foundAnyVariants )
                    {
                        objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "UpgradeNecroFlagshipVariants" );
                        ObjectiveCategory.AddActualObjective( objective );
                    }
                    if ( upgrades <= 2 && ( info.Flagships.Count > 1 || World_AIW2.Instance.GameSecond > 600 ) )
                    {
                        //don't show this immediately
                        objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "UpgradeNecroFlagships" );
                        ObjectiveCategory.AddActualObjective( objective );

                    }
                }
                //early upgrades: nudge the player to invest a couple of science upgrades into basic skeletons, basic wights, and totem defenses
                {
                    TechUpgrade skeletonTech = TechUpgradeTable.Instance.GetRowByName( "SkeletonWeapon" );
                    TechUpgrade wightTech = TechUpgradeTable.Instance.GetRowByName( "WightWeapon" );
                    TechUpgrade totemTech = TechUpgradeTable.Instance.GetRowByName( "NecromancerTotems" );

                    int skeletonUpgrades = playerFaction.TechUnlocks[skeletonTech.RowIndexNonSim] + playerFaction.FreeTechUnlocks[skeletonTech.RowIndexNonSim];
                    int wightUpgrades = playerFaction.TechUnlocks[wightTech.RowIndexNonSim] + playerFaction.FreeTechUnlocks[wightTech.RowIndexNonSim];
                    int totemUpgrades = playerFaction.TechUnlocks[totemTech.RowIndexNonSim] + playerFaction.FreeTechUnlocks[totemTech.RowIndexNonSim];

                    if ( skeletonUpgrades < 2 || wightUpgrades < 2 || totemUpgrades < 2 )
                    {
                        objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "EarlyUpgrades" );
                        ObjectiveCategory.AddActualObjective( objective );
                    }
                }
                //for elderling hunting grounds: the necromancer needs some nearby planets that are safe enough to hunt elderlings on
                {
                    const int hopsToCheck = 2;
                    const int safeStrengthThreshold = 5000; //the UI displays strength scaled by 1K, so this is "5K"
                    const int minSafePlanetsNeeded = 6;

                    Arcen.Universal.List<Planet> nearbyPlanets = Planet.GetTemporaryPlanetList( "NecromancerObjectivesGenerator-ElderlingHuntingGrounds", 10f );
                    if ( nearbyPlanets == null ) //blocked for teardown/shutdown; bail
                        return;
                    foreach ( Planet ownedPlanet in World_AIW2.Instance.Planets( false ) )
                    {
                        if ( ownedPlanet.GetControllingFaction() != playerFaction )
                            continue;
                        foreach ( Planet.PlanetAtHopDistance phd in ownedPlanet.PlanetsWithinXHops_NoFilters( hopsToCheck ) )
                        {
                            if ( !nearbyPlanets.Contains( phd.Planet ) )
                                nearbyPlanets.Add( phd.Planet );
                        }
                    }

                    int safePlanetCount = 0;
                    for ( int i = 0; i < nearbyPlanets.Count; i++ )
                    {
                        PlanetFaction pFaction = nearbyPlanets[i].GetPlanetFactionForFaction( playerFaction );
                        if ( pFaction == null )
                            continue;
                        if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength < safeStrengthThreshold )
                            safePlanetCount++;
                    }
                    Planet.ReleaseTemporaryPlanetList( nearbyPlanets );

                    if ( safePlanetCount < minSafePlanetsNeeded )
                    {
                        objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "ClearElderlingHuntingGrounds" );
                        ObjectiveCategory.AddActualObjective( objective );
                    }
                }
            }
        }
        private static void GenerateRiftObjectives()
        {
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "TemplarRift" ) )
            {
                if(entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored)
                    continue;
                {
                    #region Rifts
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "HackRift" );
                    objective.RelatedEntity1 = entity;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }
        private static void GenerateConstructorObjectives()
        {
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "TemplarConstructor" ) )
            {
                if(entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored)
                    continue;
                {
                    #region Rifts
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "DestroyConstructor" );
                    objective.DisplayNameBase = "摧毁 ";
                    objective.RelatedEntity1 = entity;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }
        private static void GenerateAmplifierObjectives()
        {
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "NecromancerAmplifier" ) )
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( entity.PlanetFaction.Faction.GetIsFriendlyTowards( localFaction ) )
                    continue; //skip already captured structures
                DLC3GameEntityTypeDataExtension entity_DLC3TypeData = entity.TypeData.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );
                if ( entity_DLC3TypeData == null )
                    continue; //this should only happen on bugs I expect
                if( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored)
                    continue;
                {
                    #region Rifts
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "CaptureAmplifier" );
                    objective.RelatedEntity1 = entity;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }

    }
    public class HackRift : IObjectiveHookManager
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
                buffer.Add( "Bug in HackRift: null entity" );
                return;
            }
            
            int debugStage = 0;
            try
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                GameEntity_Squad entity = Objective.RelatedEntity1;
                TemplarPerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<TemplarPerUnitBaseInfo>();

                buffer.Add( "可用升级：\n\n", ObjectiveColors.Header );
                for ( int i = 0; i < data.AvailableUpgrades.Count; i++ )
                {
                    buffer.Add("\t");
                    NecromancerUpgrade upgrade = data.AvailableUpgrades[i];
                    GameEntityTypeData typeData = upgrade.RelatedShip;
                    if ( typeData == null || typeData.TexEmbedSprite_Icon == null )
                    {
                        typeData = upgrade.ShipForCapIncrease;
                    }

                    if (typeData != null)
                        buffer.AddShipIconInline(typeData, localFaction );

                    buffer.Add( upgrade.DisplayName );

                    if ( typeData != null &&
                         typeData.IsMobileCombatant )
                    {
                        GameEntityTypeData CapIncreaseShipType = upgrade.ShipForCapIncrease;
                        int shipsToGet = 1;
                        if ( CapIncreaseShipType != null &&
                             CapIncreaseShipType != typeData )
                        {
                            //we are getting a structure that grants the ship
                            GameEntityTypeData grantedShipTypeData = GameEntityTypeDataTable.Instance.GetRowByName( CapIncreaseShipType.ShipTypeNameToGrantMoreOfInCustomFleet );
                            if ( typeData != null && typeData == grantedShipTypeData )
                            {
                                int shipsPerStructure = CapIncreaseShipType.ShipTypeCountToGrantMoreOfInCustomFleet;
                                shipsToGet = shipsPerStructure * upgrade.CapIncrease;
                            }
                        }
                        if ( shipsToGet > 1 )
                            buffer.Add( " 脳" + shipsToGet, "77a1aa" );
                        buffer.Add( "  " ); //a bit more whitespace
                        byte markLevel = localFaction.GetGlobalMarkLevelForShipLine( typeData );
                        GameEntityTypeData.MarkLevelStats markStatsForDisplay = typeData.MarkStatsFor( markLevel );
                        int totalStrength = (shipsToGet * markStatsForDisplay.StrengthPerSquad_CalculatedWithNullFleetMembership);
                        ArcenExternalUIUtilities.AddSizeAndVOssetToText( buffer, ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon, 12, 2 );
                        buffer.StartColor( ArcenExternalUIUtilities.StrengthTextColor );
                        ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, totalStrength, true, true );
                        buffer.EndColor();
                    }
                    buffer.Add("\n");
                }
                buffer.Add( "\n提示：", ObjectiveColors.Header ).Add( "先摧毁该星球上的AI指挥站以降低入侵成本。" );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in HackRift.TooltipHandler debugStage " + debugStage + " Exception: " + e, Verbosity.ShowAsError );
            }
        }
    }
    public class DestroyConstructor : IObjectiveHookManager
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
                buffer.Add( "Bug in DestroyConstructor: null entity" );
                return;
            }
            int debugStage = 0;
            try
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                GameEntity_Squad entity = Objective.RelatedEntity1;
                if ( entity == null )
                    return;
                buffer.AddObjectiveEntityHeader( entity, entity.GetFactionCenterColorHexBrighter_Safe() );
                buffer.Add( "位于" ).Add( entity.GetPlanetName_Safe(), ObjectiveColors.Reward ).Add( "。\n\n" );
                buffer.Add( "摧毁它可以阻止圣殿骑士扩张并为你提供资源。干掉" ).Add( "建造者", ObjectiveColors.Keyword ).Add( "是削弱圣殿骑士的好方法。" );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in DestroyConstructor.TooltipHandler debugStage " + debugStage + " Exception: " + e, Verbosity.ShowAsError );
            }
        }
    }
    public class CaptureAmplifier : IObjectiveHookManager
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
                buffer.Add( "Bug in CaptureAmplifier: null entity" );
                return;
            }
            int debugStage = 0;
            try
            {
                debugStage = 100;
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                GameEntity_Squad entity = Objective.RelatedEntity1;
                TemplarPerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<TemplarPerUnitBaseInfo>();
                DLC3GameEntityTypeDataExtension entity_DLC3TypeData = entity.TypeData.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );
                if ( entity_DLC3TypeData == null )
                    buffer.Add("This should not happen");
                debugStage = 200;
                buffer.Add("此建筑增强你的亡灵法术，用于加强该星球上死灵城所支持的旗舰：\n\n");
                GameEntityTypeData skeletonType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "SkeletonAmplifier" );
                GameEntityTypeData wightType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "WightAmplifier" );
                GameEntityTypeData mummyType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "MummyAmplifier" );
                if ( entity_DLC3TypeData.BonusSkeletonPercent > 0 )
                {
                    if ( skeletonType != null ) buffer.AddShipIconInline( skeletonType, localFaction, TextStyle.Ship_Sprite_Ency ).Add( " " );
                    buffer.Add( "骷髅", ObjectiveColors.Reward ).Add( ": +" ).Add( entity_DLC3TypeData.BonusSkeletonPercent, ObjectiveColors.Reward ).Add( "% 几率召唤多个\n\n" );
                }
                if ( entity_DLC3TypeData.BonusWightPercent > 0 )
                {
                    if ( wightType != null ) buffer.AddShipIconInline( wightType, localFaction, TextStyle.Ship_Sprite_Ency ).Add( " " );
                    buffer.Add( "尸妖", ObjectiveColors.Reward ).Add( ": +" ).Add( entity_DLC3TypeData.BonusWightPercent, ObjectiveColors.Reward ).Add( "% 几率召唤多个\n\n" );
                }
                if ( entity_DLC3TypeData.BonusMummyPercent > 0 )
                {
                    if ( mummyType != null ) buffer.AddShipIconInline( mummyType, localFaction, TextStyle.Ship_Sprite_Ency ).Add( " " );
                    buffer.Add( "木乃伊", ObjectiveColors.Reward ).Add( ": +" ).Add( entity_DLC3TypeData.BonusMummyPercent, ObjectiveColors.Reward ).Add( "% 几率召唤多个\n\n" );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in HackShipGranter.TooltipHandler debugStage " + debugStage + " Exception: " + e, Verbosity.ShowAsError );
            }
        }
    }
        public class HackRifts : IObjectiveHookManager
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
            buffer.Add( "裂隙给你什么\n", ObjectiveColors.Header );
            buffer.Add( "单位和升级", ObjectiveColors.Reward ).Add( "：裂隙提供直接增强你军队的单位和升级。\n" );
            buffer.Add( "旗舰蓝图", ObjectiveColors.Reward ).Add( "：某些裂隙解锁强大的旗舰变体。\n\n" );

            buffer.Add( "关键提示\n", ObjectiveColors.Header );
            buffer.Add( "先摧毁指挥站", ObjectiveColors.Reward ).Add( "：摧毁裂隙星球上的指挥站可以降低入侵成本。\n" );
            buffer.Add( "查看提示侧边栏以获取关于裂隙的更多详情。" );
        }
    }

    public class FightTemplar : IObjectiveHookManager
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
            buffer.Add( "你的收获\n", ObjectiveColors.Header );
            buffer.Add( "与圣殿骑士战斗是你的入侵点数和科技的主要来源。\n\n" );

            buffer.Add( "关键目标\n\n", ObjectiveColors.Header );
            Faction templarFaction = FactionUtilityMethods.Instance.GetTemplarFaction();
            GameEntityTypeData constructorType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "TemplarConstructor" );
            GameEntityTypeData riftType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "TemplarRift" );
            if ( constructorType != null )
                buffer.AddShipIconInline( constructorType, templarFaction, TextStyle.Ship_Sprite_Ency ).Add( " " );
            buffer.Add( "圣殿骑士建造者", ObjectiveColors.Reward ).Add( "\n\n" );
            buffer.Add( "最高优先级。摧毁它们可以阻止圣殿骑士扩张并提供额外资源。\n\n" );
            if ( riftType != null )
                buffer.AddShipIconInline( riftType, templarFaction, TextStyle.Ship_Sprite_Ency ).Add( " " );
            buffer.Add( "圣殿骑士裂隙", ObjectiveColors.Reward ).Add( "\n\n" );
            buffer.Add( "入侵它们以获得单位、升级和旗舰蓝图。\n\n" );

            buffer.Add( "查看提示和日志侧边栏以获取更多详情。" );
        }
    }
    public class UpgradeNecroFlagships : IObjectiveHookManager
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
            buffer.Add( "升级旗舰很重要。你可以通过科技菜单或舰队菜单进行升级；升级消耗精华，是提升舰队战斗力的重要途径。此外，更高级的旗舰有资格转化为更强大的变体。你可以通过入侵远古生物或通过裂隙获得这些变体。最强大的变体来自与远古生物的战斗。\n\n决定如何分配你的精华（用于旗舰升级、死灵城升级或建造死灵城）是一个重要的决策。" );
        }
    }
    public class UpgradeNecroFlagshipVariants : IObjectiveHookManager
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
            buffer.Add( "除了升级旗舰的等级之外，使用新的旗舰变体也能获得巨大力量。为此你必须获取蓝图。某些蓝图可以从裂隙获得，但最强大的蓝图是通过对远古生物使用转化远古生物入侵然后赢得战斗获得的。\n\n要在旗舰上使用蓝图，请在入侵菜单中查找'转化旗舰'。" );
        }
    }
    public class UnusedHexes : IObjectiveHookManager
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
            buffer.Add( "一般来说，你应该使用死灵城中的六边形格位。在银河地图上查看时，死灵城会在图标下方显示其未使用的六边形格位数量。\n\n具有未使用六边形格位的死灵城：" );

            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            NecromancerEmpireFactionBaseInfo info = playerFaction?.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
            if ( info == null )
                return;
            foreach ( GameEntity_Squad city in info.Necropoleis.DisplaySquads() )
            {
                Fleet fleet = city.FleetMembership.Fleet;
                if ( fleet == null )
                    continue;
                if ( fleet.CalculateRemainingCitySockets() >= 1 )
                    buffer.Add( "\n" ).Add( city.Planet.Name, ObjectiveColors.Reward );
            }
        }
    }
    public class UpgradeNecropolises : IObjectiveHookManager
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
            buffer.Add( "升级你的死灵城很重要。你可以通过科技菜单或舰队菜单进行升级；升级消耗精华。升级死灵城可获得更多六边形格位，让你建造更多建筑来防御或加强你的舰队。\n\n决定如何分配你的精华（用于旗舰升级、死灵城升级或建造死灵城）是一个重要的决策。" );
        }
    }
    public class BuildNecropolises : IObjectiveHookManager
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
            buffer.Add( "建造新的死灵城来加强你的部队很重要。主要死灵城最昂贵但也最强大，为你提供一艘新旗舰。次级死灵城将增强现有旗舰，增加其力量。防御性死灵城在防御或守住星球方面非常出色，但不会加强旗舰。\n\n决定如何分配你的精华（用于旗舰升级、死灵城升级或建造死灵城）是一个重要的决策。" );
        }
    }
    public class HuntElderlings : IObjectiveHookManager
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
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();

            buffer.Add( "远古生物给你什么\n", ObjectiveColors.Header );
            buffer.Add( "精华", ObjectiveColors.Reward ).Add( "：用于升级死灵城和旗舰的主要资源。\n" );
            buffer.Add( "旗舰蓝图", ObjectiveColors.Reward ).Add( "：入侵更强的远古生物以解锁最强大的旗舰变体。\n\n" );

            buffer.Add( "如何猎杀它们\n", ObjectiveColors.Header );
            buffer.Add( "入侵远古生物以追踪其位置，或吸引你已经追踪到的远古生物。\n\n" );

            buffer.Add( "最佳早期目标\n", ObjectiveColors.Header );
            GameEntityTypeData feebleData = GameEntityTypeDataTable.Instance.GetRowByName( "FeebleElderling" );
            if ( feebleData != null )
                buffer.AddShipIconInline( feebleData, localFaction );
            buffer.Add( "虚弱远古生物", ObjectiveColors.Reward ).Add( "提供大量精华。" );
        }
    }
    public class EarlyUpgrades : IObjectiveHookManager
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
            buffer.Add( "早期将科技投入几个关键升级对死灵法师大有裨益。目标是将以下各项至少升级2级：\n" );

            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;

            TechUpgrade skeletonTech = TechUpgradeTable.Instance.GetRowByName( "SkeletonWeapon" );
            TechUpgrade wightTech = TechUpgradeTable.Instance.GetRowByName( "WightWeapon" );
            TechUpgrade towerTech = TechUpgradeTable.Instance.GetRowByName( "NecromancerDefense" );

            int skeletonUpgrades = playerFaction.TechUnlocks[skeletonTech.RowIndexNonSim] + playerFaction.FreeTechUnlocks[skeletonTech.RowIndexNonSim];
            int wightUpgrades = playerFaction.TechUnlocks[wightTech.RowIndexNonSim] + playerFaction.FreeTechUnlocks[wightTech.RowIndexNonSim];
            int towerUpgrades = playerFaction.TechUnlocks[towerTech.RowIndexNonSim] + playerFaction.FreeTechUnlocks[towerTech.RowIndexNonSim];

            if ( skeletonUpgrades < 2 )
                buffer.Add( "\n骷髅（单位科技）", ObjectiveColors.Reward );
            if ( wightUpgrades < 2 )
                buffer.Add( "\n尸妖（单位科技）", ObjectiveColors.Reward );
            if ( towerUpgrades < 2 )
                buffer.Add( "\n塔防（防御科技）", ObjectiveColors.Reward );

            buffer.Add( "\n\n早期你的军队主要由基础单位组成，而且数量很多。加强它们（尤其是在早期）是一种非常高效的利用科技加强舰队的方式。\n\n" );
            buffer.Add( "基础骷髅和尸妖是优秀的前线坦克，通常直接在敌人部队中间被召唤。增加的耐久度将立即见效。高级尸妖会退化为基础尸妖，所以升级基础形态会使你整个尸妖池受益。塔防对于抵挡圣殿骑士强大的渐进攻击尤其有价值。" );
        }
    }
    public class ClearElderlingHuntingGrounds : IObjectiveHookManager
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
            buffer.Add( "远古生物倾向于游荡到没有强大敌对防御的领土。要为远古生物建立良好的猎场，你需要在自己附近有一些敌对力量相对较低的星球。\n\n考虑清理附近的敌对力量（如AI守卫哨站和巡逻队），这样远古生物就有安全的星球可以游荡，然后你可以在那里猎杀它们以获取精华。" );
        }
    }
}
