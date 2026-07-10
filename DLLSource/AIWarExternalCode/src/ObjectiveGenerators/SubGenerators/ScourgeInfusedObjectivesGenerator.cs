using Arcen.AIW2.Core;
using System;

using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class ScourgeInfusedObjectivesGenerator
    {
        public static void CheckForScourgeInfusedObjectives_BackgroundThread_ClientOrHost()
        {
            try
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( !ScourgeInfusedHumanEmpireFactionBaseInfo.GetIsThisAScourgeEmpireFaction( localFaction ) )
                    return;
                ScourgeInfusedHumanEmpireFactionBaseInfo empireInfo = localFaction.GetExternalBaseInfoAs<ScourgeInfusedHumanEmpireFactionBaseInfo>();
                if ( empireInfo == null )
                    return;
                ScourgeVassalFactionBaseInfo vassalInfo = GetVassalInfoSafe( empireInfo );
                GenerateScourgeInfusedStrategyObjectives( empireInfo, vassalInfo );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in ScourgeInfusedObjectivesGenerator: " + e, Verbosity.ShowAsError );
            }
        }

        private static ScourgeVassalFactionBaseInfo GetVassalInfoSafe( ScourgeInfusedHumanEmpireFactionBaseInfo empireInfo )
        {
            try
            {
                return empireInfo.GetVassalBaseInfo();
            }
            catch
            {
                return null;
            }
        }

        private static void GenerateScourgeInfusedStrategyObjectives( ScourgeInfusedHumanEmpireFactionBaseInfo empireInfo, ScourgeVassalFactionBaseInfo vassalInfo )
        {
            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "ScourgeSpawners" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "ScourgeArmories" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "ScourgeCorbomite" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            if ( vassalInfo != null && vassalInfo.UpgradableInfrastructureCount.Display > 0 )
            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "ScourgeUpgradeInfrastructure" );
                obj.RelatedInt1 = vassalInfo.UpgradableInfrastructureCount.Display;
                ObjectiveCategory.AddActualObjective( obj );
            }

            int unlockedCount = empireInfo.UnlockedRaces.Count;
            int totalRaceCount = ScourgeTypeDataTable.Instance != null ? ScourgeTypeDataTable.Instance.Rows.Count : 7;
            if ( unlockedCount < totalRaceCount )
            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "ScourgeUnlockRaces" );
                obj.RelatedInt1 = unlockedCount;
                obj.RelatedInt2 = totalRaceCount;
                ObjectiveCategory.AddActualObjective( obj );
            }
        }
    }

    public class ScourgeSpawners : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "生成器", "ff8877" ).Add( "是你天灾部队的基础。\n\n" );
            buffer.Add( "每个生成器都会稳定产出天灾战士。你建造的生成器越多、等级越高，天灾军队就越庞大、越强大。\n\n" );
            buffer.Add( "生成器使用科博麦特建造。将它们部署到银河各处，将天灾的力量投射到新的区域。请注意，生成器被摧毁后需要时间重新升级。\n\n" );
            buffer.Add( "一旦生成器积累足够的经验，你可以通过入侵菜单升级它们。", ObjectiveColors.Hint );
        }
    }

    public class ScourgeArmories : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "军械库", "ff8877" ).Add( "让你的天灾战士得以进化。\n\n" );
            buffer.Add( "当战士到达军械库时，它可以进化成该军械库的种族特定变体：更强大的进化战士，或进一步成为强大的混合体。更高等级的军械库允许战士达到更高等级。\n\n" );
            buffer.Add( "通过科技树解锁的每个种族都会启用一种新型军械库和要塞。每个已解锁种族至少建造一个军械库，让你的战士可以专精。\n\n" );
            buffer.Add( "怪物图鉴", "ff8877" ).Add( "是一种相关建筑：每个图鉴生产一艘极其强大的舰船来防御附近星球。" );
            buffer.Add( "要塞", "ff8877" ).Add( "为其周边星球生产防御舰队。", ObjectiveColors.Hint );
        }
    }

    public class ScourgeCorbomite : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "科博麦特", "ff8877" ).Add( "是用于建造所有天灾建筑的资源。\n\n" );
            buffer.Add( "它从", ObjectiveColors.Hint ).Add( "种子", "ff8877" ).Add( "生长成", ObjectiveColors.Hint ).Add( "花", "ff8877" ).Add( "，然后产生可收获的科博麦特水晶。\n\n" );
            buffer.Add( "AI会积极试图摧毁你的花；保护它们很重要。种植更多的种子和花以维持稳定的科博麦特收入。\n\n" );
            buffer.Add( "你也可以使用天灾建筑上的入侵菜单执行各种科博麦特相关操作。", ObjectiveColors.Hint );
        }
    }

    public class ScourgeUpgradeInfrastructure : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            int count = Objective.RelatedInt1;
            buffer.Add( count.ToString(), "ffaa44" ).Add( count == 1 ? " 个天灾建筑已" : " 个天灾建筑已", ObjectiveColors.Hint ).Add( "积累足够的经验可供升级。\n\n" );
            buffer.Add( "打开建筑上的入侵菜单进行升级。更高等级的生成器产生更多战士，更高等级的军械库允许战士达到更高等级，更高等级的怪物图鉴生产更强大的舰船。\n\n" );
            buffer.Add( "检查通知面板以查看准备升级的具体建筑。", ObjectiveColors.Hint );
        }
    }

    public class ScourgeUnlockRaces : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            int unlocked = Objective.RelatedInt1;
            int total = Objective.RelatedInt2;
            buffer.Add( unlocked.ToString(), ObjectiveColors.Reward ).Add( " / " ).Add( total.ToString(), "ffaa44" ).Add( " 外星种族已解锁。\n\n" );
            buffer.Add( "通过科技树解锁种族可启用：\n" );
            buffer.Add( "  鈥?特定种族的", ObjectiveColors.Hint ).Add( "军械库", "ff8877" ).Add( "让战士进化为该种族的变体。\n" );
            buffer.Add( "  鈥?特定种族的", ObjectiveColors.Hint ).Add( "要塞", "ff8877" ).Add( "生产该种族的防御舰船。\n\n" );
            buffer.Add( "解锁种族的第二级可启用混合体形态。\n\n" );
            buffer.Add( "可用种族：Burlust、Evuck、Thoraxian、Peltian、Neinzul、Spire、Zenith。", ObjectiveColors.Hint );
        }
    }
}
