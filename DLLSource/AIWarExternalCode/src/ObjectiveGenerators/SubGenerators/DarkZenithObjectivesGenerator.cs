using Arcen.AIW2.Core;
using System;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class DarkZenithObjectivesGenerator
    {
        public static void CheckForDarkZenithObjectives_BackgroundThread_ClientOrHost()
        {
            try
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( !DarkZenithSidekickFactionBaseInfo.GetIsThisADZFaction( localFaction ) )
                    return;
                DarkZenithSidekickFactionBaseInfo info = localFaction.GetExternalBaseInfoAs<DarkZenithSidekickFactionBaseInfo>();
                if ( info == null )
                    return;
                GenerateDZStrategyObjectives( info );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in DarkZenithObjectivesGenerator: " + e, Verbosity.ShowAsError );
            }
        }

        private static void GenerateDZStrategyObjectives( DarkZenithSidekickFactionBaseInfo info )
        {
            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "DZFleetMenu" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "DZEconomy" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "DZTechTree" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "DZShipVariants" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            if ( info.WinterEnabled )
            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "DZFimbulwinter" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "DZNadir" );
                obj.RelatedInt1 = info.NadirBases.Count;
                ObjectiveCategory.AddActualObjective( obj );
            }
        }
    }

    public class DZFleetMenu : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "舰队菜单", "55aaff" ).Add( "是黑暗 Zenith帝国的主要控制面板。\n\n" );
            buffer.Add( "点击屏幕顶部资源栏的", ObjectiveColors.Hint ).Add( "入侵区域", "55aaff" ).Add( "即可打开。\n\n" );
            buffer.Add( "在舰队菜单中你可以：\n" );
            buffer.Add( "  鈥?查看完整的", ObjectiveColors.Hint ).Add( "经济概览", "55aaff" ).Add( "：柱楣分配、Terminus资源库存、运输船活动。\n" );
            buffer.Add( "  鈥?", ObjectiveColors.Hint ).Add( "分配每个柱楣建造什么", "55aaff" ).Add( "：舰船、Terminii、建筑或升级。\n" );
            buffer.Add( "  鈥?浏览和了解你的", ObjectiveColors.Hint ).Add( "科技树升级", "55aaff" ).Add( "。\n" );
            buffer.Add( "  鈥?追踪", ObjectiveColors.Hint ).Add( "建造者", "55aaff" ).Add( "的活动，了解新的经济建筑被放置在哪里。\n\n" );
            buffer.Add( "黑暗 Zenith会自动驾驶，但舰队菜单是你指导它们优先级的方式。", ObjectiveColors.Hint );
        }
    }

    public class DZEconomy : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "黑暗 Zenith的经济运行于六种资源和三种建筑类型之上。\n\n" );
            buffer.Add( "Terminii", "55aaff" ).Add( "是资源提取器。每个星球可以容纳一种或多种资源类型的Terminii：" );
            buffer.Add( "Octiron", "d3d3d3" ).Add( "（基础金属），" );
            buffer.Add( "Thaumite", "10ff10" ).Add( "，" );
            buffer.Add( "Chelonium", "4444ff" ).Add( "，" );
            buffer.Add( "Alkahest", "D7BE69" ).Add( "，" );
            buffer.Add( "Izumite", "ff4422" ).Add( "和" );
            buffer.Add( "Skrith", "8aaa8a" ).Add( "。\n\n" );
            buffer.Add( "柱楣", "55aaff" ).Add( "是你的工厂。它们将存储的资源转换为舰船、新的Terminii和其他建筑。" );
            buffer.Add( "柱楣不能在AI控制的星球上建造", "ffaa44" ).Add( "；AI指挥站的辐射会干扰它们。\n\n" );
            buffer.Add( "运输船", "55aaff" ).Add( "在星球之间运输资源以保持柱楣的供应。它们需要" );
            buffer.Add( "连续的星球链", "ffaa44" ).Add( "；AI会尽可能狙击你的运输船。\n\n" );
            buffer.Add( "收割者", "55aaff" ).Add( "与Octiron Terminii一起工作以提取金属。" );
            buffer.Add( "私掠者", "55aaff" ).Add( "会偷取你的其他运输船供给海盗柱楣，后者生产超强力的舰船。", ObjectiveColors.Hint );
        }
    }

    public class DZTechTree : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "黑暗 Zenith科技树由", ObjectiveColors.Hint ).Add( "变体升级", "55aaff" ).Add( "驱动（解锁每个舰船层级的特殊灌注形态）。\n\n" );

            buffer.Add( "舰船层级：\n", ObjectiveColors.Hint );
            buffer.Add( "  鈥?", ObjectiveColors.Hint ).Add( "攻击艇", "55aaff" ).Add( "：始终可用。\n" );
            buffer.Add( "  鈥?", ObjectiveColors.Hint ).Add( "守护者层级", "55aaff" ).Add( "：需要3个变体升级。\n" );
            buffer.Add( "  鈥?", ObjectiveColors.Hint ).Add( "凶暴层级", "55aaff" ).Add( "：需要6个变体 + 守护者层级 + Mark 2。\n" );
            buffer.Add( "  鈥?", ObjectiveColors.Hint ).Add( "Exo层级", "55aaff" ).Add( "：需要10个变体 + 凶暴层级 + Mark 3。\n\n" );

            buffer.Add( "等级", ObjectiveColors.Hint ).Add( "解锁所有舰船的更强版本：\n" );
            buffer.Add( "  Mark 2: 3个变体  鈥? Mark 3: 5个变体 + 守护者  鈥? Mark 4: 7个变体\n" );
            buffer.Add( "  Mark 5: 9个变体 + 凶暴  鈥? Mark 6: 11个变体  鈥? Mark 7: 13个变体 + Exo\n\n" );

            buffer.Add( "通过任何柱楣上的", ObjectiveColors.Hint ).Add( "入侵菜单", "55aaff" ).Add( "解锁升级。每解锁一种新变体类型都会计入上述门槛，所以要多样化你的变体，而不是重复同一种。\n\n" );
            buffer.Add( "柱楣也可以升级以产生永久的被动资源收入，并提高其Terminus和建造上限。", ObjectiveColors.Hint );
        }
    }

    public class DZShipVariants : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "黑暗 Zenith舰船可以用次级资源灌注以创造具有特殊能力的", ObjectiveColors.Hint ).Add( "变体形态", "55aaff" ).Add( "。每个层级有五种变体：\n\n" );
            buffer.Add( "  鈥?", ObjectiveColors.Hint ).Add( "坚韧", "10ff10" ).Add( "（Thaumite）：更耐久；提升船体和韧性。\n" );
            buffer.Add( "  鈥?", ObjectiveColors.Hint ).Add( "强化", "4444ff" ).Add( "（Chelonium）：增加一个额外防御系统。\n" );
            buffer.Add( "  鈥?", ObjectiveColors.Hint ).Add( "灵动", "D7BE69" ).Add( "（Alkahest）：隐形且更快；更难被锁定。\n" );
            buffer.Add( "  鈥?", ObjectiveColors.Hint ).Add( "激怒", "ff4422" ).Add( "（Izumite）：针对单个强大目标的额外武器。\n" );
            buffer.Add( "  鈥?", ObjectiveColors.Hint ).Add( "诡诈", "8aaa8a" ).Add( "（Skrith）：对抗集群敌人的群体控制额外武器。\n\n" );
            buffer.Add( "变体必须为每个舰船层级分别解锁，并从上一层级同类型变体链式解锁。" );
            buffer.Add( "你解锁的每种变体也会计入层级和等级前提条件。\n\n" );
            buffer.Add( "通过任何柱楣上的", ObjectiveColors.Hint ).Add( "入侵菜单", "55aaff" ).Add( "解锁变体。解锁一个层级的所有五种变体通常是进入下一层级的最快路径。", ObjectiveColors.Hint );
        }
    }

    public class DZFimbulwinter : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "芬布尔之冬", "55aaff" ).Add( "是一种永久改变星球以利于黑暗 Zenith的地形改造过程。\n\n" );
            buffer.Add( "Hjarnum", "55aaff" ).Add( "单位环绕星球并逐渐施加芬布尔之冬改造。完成后：\n" );
            buffer.Add( "  鈥?星球", ObjectiveColors.Hint ).Add( "产生科技", ObjectiveColors.Reward ).Add( "。\n" );
            buffer.Add( "  鈥?", ObjectiveColors.Hint ).Add( "黑暗 Zenith及盟友舰船", "55aaff" ).Add( "在星球上移动更快。\n" );
            buffer.Add( "  鈥?", ObjectiveColors.Hint ).Add( "敌方舰船", "ff8877" ).Add( "进入星球时速度减慢。\n\n" );
            buffer.Add( "每个安全经历了芬布尔之冬的星球都会产生科技；这是你在战斗击杀之外获取科技的主要途径。\n\n" );
            buffer.Add( "Hjarnum在改造期间是", ObjectiveColors.Hint ).Add( "被动的", "55aaff" ).Add( "，不会战斗。保护它们工作，但AI通常会忽略它们。", ObjectiveColors.Hint );
        }
    }

    public class DZNadir : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            int count = Objective.RelatedInt1;
            if ( count > 0 )
                buffer.Add( count.ToString(), "ff8877" ).Add( count == 1 ? " 个天底基地存在于" : " 个天底基地存在于", ObjectiveColors.Hint ).Add( "银河中。\n\n" );

            buffer.Add( "天底基地", "ff8877" ).Add( "是AI部署的专门用于对抗黑暗 Zenith的建筑。它们不断生成反DZ舰船以消耗你的力量和经济。\n\n" );
            buffer.Add( "摧毁天底基地：\n" );
            buffer.Add( "  鈥?消除持续的反DZ压力源。\n" );
            buffer.Add( "  鈥?获得", ObjectiveColors.Hint ).Add( "Alkahest", "D7BE69" ).Add( "和其他DZ资源。\n" );
            buffer.Add( "  鈥?为你的柱楣提供", ObjectiveColors.Hint ).Add( "入侵点数", ObjectiveColors.Reward ).Add( "。\n\n" );
            buffer.Add( "天底基地", ObjectiveColors.Hint ).Add( "的等级会随着AI威胁升级而提高", "ff8877" ).Add( "，所以要优先处理它们，以免它们变得难以攻克。", ObjectiveColors.Hint );
        }
    }
}
