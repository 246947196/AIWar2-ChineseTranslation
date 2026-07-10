using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class BeginnerObjectivesGenerator
    {
        public static void CheckForBeginnerObjectives_BackgroundThread_ClientOrHost()
        {
            try
            {
                GenerateScienceSpendingObjectives();
                GenerateWarpGateObjectives();
                GenerateEngineerRelatedObjectives();
                GenerateEnergyRelatedObjectives();
                GenerateFlagshipCapturingObjectives();
                GenerateCPACapturingObjectives();
                GenerateARSHackingObjectives();
                GenerateEarlyExpansionObjectives();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in FlagshipObjectivesGenerator generation: " + e, Verbosity.ShowAsError );
            }
        }
        
        private static bool GetIsThisAHumanEmpireFaction( Faction faction )
        {
            if ( faction == null )
                return false;
            if ( faction.Type != FactionType.Player )
                return false;
            PlayerTypeData playerTypeData = faction.PlayerTypeDataOrNull_ModeratelyExpensive;
            if ( playerTypeData == null )
                return false;
            switch ( playerTypeData.InternalName )
            {
                case "HumanEmpire":
                case "HumanArkEmpire":
                case "SpireInfusedEmpire":
                case "ScourgeInfusedHumanEmpire":
                case "ApkalluInfusedEmpire":
                    return true;
            }
            return false;
        }

        private static void GenerateFlagshipCapturingObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( !GetIsThisAHumanEmpireFaction( playerFaction ) )
                return;

            int numFlagships = 0;
            foreach ( GameEntity_Squad flagship in playerFaction.Squads( EntityRollupType.MobileCombatFlagships ) )
            {
                numFlagships++;
            }
            if ( numFlagships <= 1 )
            {
                {
                    #region Get Flagships
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "NeedFlagships" );
                    ObjectiveCategory.AddActualObjective( objective);
                    #endregion
                }
            }
        }
        private static void GenerateCPACapturingObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( !GetIsThisAHumanEmpireFaction( playerFaction ) )
                return;

            //TSS
            if ( playerFaction.OnePlayer_AddedToCommandStationsAndBattlestations_Permanent.Count == 0 )
            {
                {
                    #region Get TSS
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "NeedTSSes" );
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }
        private static void GenerateARSHackingObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( !GetIsThisAHumanEmpireFaction( playerFaction ) )
                return;

            if ( playerFaction.NumARSsHacked_ForUI == 0 )
            {
                {
                    #region Get ARS
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "NeedARSs" );
                    ObjectiveCategory.AddActualObjective(objective);
                    #endregion
                }
            }
        }
        
        private static void GenerateEnergyRelatedObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( !GetIsThisAHumanEmpireFaction( playerFaction ) )
                return;

            int energyRemaining = playerFaction.NetEnergy;
            if (energyRemaining <= 40000)
            {
                {
                    #region Get Energy
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "NeedEnergy" );
                    ObjectiveCategory.AddActualObjective( objective);
                    #endregion
                }
            }
        }
        private static void GenerateEngineerRelatedObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( !GetIsThisAHumanEmpireFaction( playerFaction ) )
                return;

            int numEngineers = 0;
            foreach ( GameEntity_Squad entity in playerFaction.Squads( "Engineer" ) )
            {
                numEngineers++;
                if ( numEngineers >= 5 )
                    break;
            }
            int minForObjective = 5;
            if(numEngineers < minForObjective)
            {
                {
                    #region Build Engineers
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "BuildEngineers" );
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }
        private static void GenerateWarpGateObjectives()
        {
            //The rules for this are as follows:
            //If you have a Warp Gate next to your homeworld, Objective: Defend Homeworld from Waves
            //If you have a Warp Gate next to any planet, Objective: Defend planet frmo Waves
            //If you have >= 3 planets with adjacent warp gates, Objective: Gate Raiding
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            GameEntity_Squad aRandomWarpGate = null;
            int numWaveVulnerablePlanets = 0;
            int minumumForGateRaidWarning = 3;

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                if ( planet.GetControllingFaction() != playerFaction )
                    continue;
                bool warpGateFound = false;
                //                ArcenDebugging.ArcenDebugLogSingleLine("Checking for warp gates adjacent to " + planet.Name, Verbosity.DoNotShow );
                foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                {
                    //Check for a warp gate on any neighbor
                    foreach ( GameEntity_Squad WarpPoint in neighbor.Squads( EntityRollupType.WarpEntryPoints ) )
                    {
                        warpGateFound = true;
                        aRandomWarpGate = WarpPoint;
                        break;
                    }
                }
                if ( warpGateFound )
                {
                    numWaveVulnerablePlanets++;
                    {
                        #region Defend from waves
                        ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "DefendFromWaves" );
                        objective.RelatedPlanet1 = planet;
                        ObjectiveCategory.AddActualObjective( objective );
                        #endregion
                    }
                }
            }
            if ( numWaveVulnerablePlanets >= minumumForGateRaidWarning )
            {
                {
                    #region Gate Raiding
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "GateRaiding" );
                    objective.RelatedInt1 = numWaveVulnerablePlanets;
                    objective.RelatedEntity1 = aRandomWarpGate;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }
        private static void GenerateScienceSpendingObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( !GetIsThisAHumanEmpireFaction( playerFaction ) )
                return;
            int WarningLevel = 8000;
            if(playerFaction.StoredScience.IntValue > WarningLevel)
            {
                {
                    #region Spend Science
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "SpendScience" );
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }
        private static void GenerateEarlyExpansionObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( !GetIsThisAHumanEmpireFaction( playerFaction ) )
                return;

            int numPlanetsOwned = 0;
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                if ( planet.GetControllingFaction() == playerFaction )
                    numPlanetsOwned++;
            }
            if(numPlanetsOwned < 3)
            {
                {
                    #region Capture a few planets
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "EarlyExpansion" );
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }
    }



    public class GateRaiding : IObjectiveHookManager
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
            buffer.Add( "跃迁门袭击", ObjectiveColors.Keyword ).Add( "意味着只摧毁AI星球上的" ).Add( "跃迁门", ObjectiveColors.Keyword ).Add( "（而不占领星球本身），这样AI就无法通过它发送波次。\n\n" );
            buffer.Add( "AI只对与跃迁门相邻的星球发送波次。如果你控制的星球附近有太多跃迁门，你就被迫防御所有它们。袭击这些门让你可以选择攻击将在何处着陆，然后加固该位置。\n\n" );
            buffer.Add( "记住，你也可以通过入侵来摧毁跃迁门。", "ffeecc" );
        }
    }

    public class DefendFromWaves : IObjectiveHookManager
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
            buffer.Add( "AI", "ff8888" ).Add( "波次", "ff8888" ).Add( "通过" ).Add( "跃迁门", ObjectiveColors.Keyword ).Add( "攻击相邻的人类星球。\n\n" );
            buffer.Add( "跃迁门旁边的星球风险较高，应重点加固。考虑进行跃迁门袭击以减少需要防御的星球数量。", "ffeecc" );
        }
    }

    public class EarlyExpansion : IObjectiveHookManager
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
            buffer.Add( "早期占领星球可让你获得更多" ).Add( "科技", "7ce9ff" ).Add( "、" ).Add( "金属", "ccccee" ).Add( "和" ).Add( "入侵点数", "3de799" ).Add( "。目标是大约3-4个星球（足以建立强大经济而不会使" ).Add( "AI进程", "ff8888" ).Add( "增长过快）。\n\n" );
            buffer.Add( "好的早期目标：\n" );
            buffer.Add( "  旗舰", ObjectiveColors.Keyword ).Add( "：提供新的舰船线以增强进攻能力。\n" );
            buffer.Add( "  高级研究站（ARS）", ObjectiveColors.Keyword ).Add( "：入侵它们以为入侵舰队提供新的舰船线。\n" );
            buffer.Add( "  炮塔蓝图服务器（TSS）", ObjectiveColors.Keyword ).Add( "：入侵它们以使你所有星球都能建造更多炮塔。\n\n" );
            buffer.Add( "查看情报菜单和银河地图以在附近找到这些目标。", "ffeecc" );
        }
    }

    public class SpendScience : IObjectiveHookManager
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
            buffer.Add( "将", "7ce9ff" ).Add( "科技", "7ce9ff" ).Add( "投入研究是最有影响力的行动之一。早期专注于一两种使多个舰船线受益的" ).Add( "武器科技", ObjectiveColors.Keyword ).Add( "，然后扩展到" ).Add( "船体科技", ObjectiveColors.Keyword ).Add( "用于进攻、" ).Add( "炮塔科技", ObjectiveColors.Keyword ).Add( "用于防御，或随着舰队发展选择其他武器科技。\n\n" );
            buffer.Add( "强力的早期选择：\n" );
            buffer.Add( "  力场1级", ObjectiveColors.Keyword ).Add( "：增强所有力场，在攻击期间争取更多时间。\n" );
            buffer.Add( "  工程师1级", ObjectiveColors.Keyword ).Add( "：使所有工程师工作更快，节省大量时间。\n\n" );
            buffer.Add( "你也可以将科技直接投入星球或舰队以提升其" ).Add( "等级", ObjectiveColors.Keyword ).Add( "。关键早期优先级：将你的" ).Add( "母星升级到3级", ObjectiveColors.Keyword ).Add( "（对其产出的提升显著，这是科技的最佳早期用途之一）。\n\n" );
            buffer.Add( "通过占领星球或入侵AI星球获得更多科技。在侦察更多银河并了解可用舰船和机会之前，保留科技储备也是可以的。", "ffeecc" );
        }
    }

    public class BuildEngineers : IObjectiveHookManager
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
            buffer.Add( "工程师", ObjectiveColors.Keyword ).Add( "可以更快地建造单位并修复战斗中受损的舰船。在活跃星球上有足够的工程师能显著影响你从攻击中恢复的速度。\n\n" );
            buffer.Add( "你可以通过" ).Add( "设置 → 自动化", ObjectiveColors.Keyword ).Add( "菜单在所有星球上自动建造或自动FRD工程师。\n\n" );
            buffer.Add( "在3级时，工程师获得隐形能力，使其在战斗中更难被击杀。", "ffeecc" );
        }
    }

    public class NeedEnergy : IObjectiveHookManager
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
            buffer.Add( "产生更多" ).Add( "能量", "ffde00" ).Add( "的主要方式是建造" ).Add( "经济", ObjectiveColors.Keyword ).Add( "和" ).Add( "后勤指挥站", ObjectiveColors.Keyword ).Add( "。占领更多星球以放置这些指挥站，或转换现有的军事指挥站。\n\n" );
            buffer.Add( "如果指挥站提供的能量仍不够：\n" );
            buffer.Add( "  物质转换器", ObjectiveColors.Keyword ).Add( "：将金属转化为能量。\n" );
            buffer.Add( "  废弃不需要的舰船", ObjectiveColors.Keyword ).Add( "：提供临时能量提升。\n\n" );
            buffer.Add( "提升指挥站的等级也会增加其能量产出。", "ffeecc" );
        }
    }

    public class NeedFlagships : IObjectiveHookManager
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
            buffer.Add( "占领", ObjectiveColors.Keyword ).Add( "移动战斗旗舰", ObjectiveColors.Keyword ).Add( "是提升你进攻力量的主要方式之一。每艘旗舰解锁新的舰船线。\n\n" );
            buffer.Add( "你可以从旗舰中混合搭配舰船线，按自己的喜好组建舰队。你可以把旗舰想象成变异的棋子，各有优劣。\n\n" );
            buffer.Add( "使用银河地图或情报菜单在附近寻找值得占领的舰队。", "ffeecc" );
        }
    }

    public class NeedTSSes : IObjectiveHookManager
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
            buffer.Add( "炮塔蓝图服务器", ObjectiveColors.Keyword ).Add( "（TSS）和其他防御蓝图服务器", ObjectiveColors.Keyword ).Add( "（ODSS）通过入侵来解锁额外的炮塔、雷区和防御，适用于你所有的星球、战斗空间站和堡垒。\n\n" );
            buffer.Add( "这些是" ).Add( "入侵而非占领", ObjectiveColors.Keyword ).Add( "的；你不需要永久控制该星球。注意，摧毁星球上的AI指挥站会降低入侵成本。\n\n" );
            buffer.Add( "使用银河地图或情报菜单在附近寻找好的目标。", "ffeecc" );
        }
    }

    public class NeedGCAs : IObjectiveHookManager
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
            buffer.Add( "全局指挥增强器", ObjectiveColors.Keyword ).Add( "（GCA）使你所有的星球获得额外的炮塔插槽。\n\n" );
            buffer.Add( "你可以" ).Add( "占领", ObjectiveColors.Keyword ).Add( "一个GCA星球或" ).Add( "入侵", ObjectiveColors.Keyword ).Add( "它以获得相同的好处。如果你先占领了星球，入侵会更便宜；否则你不需要永久控制它。\n\n" );
            buffer.Add( "使用银河地图或情报菜单在附近寻找好的目标。", "ffeecc" );
        }
    }

    public class NeedARSs : IObjectiveHookManager
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
            buffer.Add( "入侵" ).Add( "高级研究站", ObjectiveColors.Keyword ).Add( "（ARS）可为入侵舰队提供一条新的舰船线。\n\n" );
            buffer.Add( "入侵ARS是增强你舰队的关键方式。\n\n" );
            buffer.Add( "使用银河地图或情报菜单在附近寻找好的目标。", "ffeecc" );
        }
    }

}
