using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class DysonSidekickObjectivesGenerator
    {
        public static void CheckForDysonSidekickObjectives_BackgroundThread_ClientOrHost()
        {
            try
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( !DysonSidekickFactionBaseInfo.GetIsThisADysonFaction( localFaction ) )
                    return;
                GenerateCuendillarAsteroidObjectives();
                GenerateCuendillarPlanetoidObjectives();
                GenerateReaperChrysalisObjectives();
                GenerateDysonUnusedSocketObjectives( localFaction );
                GenerateDysonStrategyObjectives();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in DysonSidekickObjectivesGenerator: " + e, Verbosity.ShowAsError );
            }
        }

        private static void GenerateCuendillarAsteroidObjectives()
        {
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "CuendillarAsteroid" ) )
            {
                if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                DysonSidekickPerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                if ( data != null && data.CuendillarRemaining <= 0 )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "CuendillarAsteroid" );
                objective.RelatedEntity1 = entity;
                objective.RelatedPlanet1 = entity.Planet;
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        private static void GenerateCuendillarPlanetoidObjectives()
        {
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "CuendillarPlanetoid" ) )
            {
                if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                DysonSidekickPerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                if ( data != null && data.CuendillarRemaining <= 0 )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "CuendillarPlanetoidObj" );
                objective.RelatedEntity1 = entity;
                objective.RelatedPlanet1 = entity.Planet;
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        private static void GenerateReaperChrysalisObjectives()
        {
            ReapersFactionBaseInfo reapers = ReapersFactionBaseInfo.Instance;
            if ( reapers == null )
                return;
            foreach ( GameEntity_Squad chrysalis in reapers.Chrysalises.DisplaySquads() )
            {
                if ( chrysalis.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "ReaperChrysalisObj" );
                objective.RelatedEntity1 = chrysalis;
                objective.RelatedPlanet1 = chrysalis.Planet;
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        private static void GenerateDysonStrategyObjectives()
        {
            string[] hooks = new string[] { "DysonDrilling", "DysonReapers", "DysonMoons", "DysonRaceSpecializations", "DysonSphere" };
            foreach ( string hook in hooks )
            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( hook );
                ObjectiveCategory.AddActualObjective( obj );
            }
        }

        private static void GenerateDysonUnusedSocketObjectives( Faction playerFaction )
        {
            DysonSidekickFactionBaseInfo info = playerFaction.TryGetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
            if ( info == null )
                return;
            foreach ( GameEntity_Squad stronghold in info.Strongholds.DisplaySquads() )
            {
                if ( stronghold.FleetMembership?.Fleet == null )
                    continue;
                Fleet fleet = stronghold.FleetMembership.Fleet;
                if ( fleet.CalculateRemainingCitySockets() < 1 )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "DysonUnusedSockets" );
                objective.RelatedPlanet1 = stronghold.Planet;
                ObjectiveCategory.AddActualObjective( objective );
            }
        }
    }

    public class CuendillarAsteroid : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedEntity1 == null )
            {
                buffer.Add( "Bug in CuendillarAsteroid: null entity" );
                return;
            }
            try
            {
                buffer.Add( "一个库恩达小行星位于" ).Add( Objective.RelatedEntity1.Planet?.Name ?? "?", ObjectiveColors.Reward ).Add( "。\n\n" );
                DysonSidekickPerUnitBaseInfo data = Objective.RelatedEntity1.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                if ( data != null )
                    buffer.Add( "剩余库恩达：" ).Add( data.CuendillarRemaining.ToString(), "8888ff" ).Add( "\n\n" );
                buffer.Add( "在此建造库恩达小行星钻机以提取库恩达并将其发送到你的戴森球。" );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in CuendillarAsteroid.TooltipHandler: " + e, Verbosity.ShowAsError );
            }
        }
    }

    public class CuendillarPlanetoid : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedEntity1 == null )
            {
                buffer.Add( "Bug in CuendillarPlanetoid: null entity" );
                return;
            }
            try
            {
                buffer.Add( "一个库恩达小行星体位于" ).Add( Objective.RelatedEntity1.Planet?.Name ?? "?", "88ff88" ).Add( "。\n\n" );
                DysonSidekickPerUnitBaseInfo data = Objective.RelatedEntity1.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                if ( data != null )
                    buffer.Add( "剩余库恩达：" ).Add( data.CuendillarRemaining.ToString(), "88ff88" ).Add( "\n\n" );
                buffer.Add( "在星球上建造戴森钻机以随时间提取其库恩达。" );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in CuendillarPlanetoid.TooltipHandler: " + e, Verbosity.ShowAsError );
            }
        }
    }

    // Note: class name matches the typo in DS_ObjectiveDetailsHooks.xml (type_name="Arcen.AIW2.External.ReaperChrsyalis")
    public class ReaperChrsyalis : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedEntity1 == null )
            {
                buffer.Add( "Bug in ReaperChrsyalis: null entity" );
                return;
            }
            try
            {
                buffer.Add( "一个收割者茧位于" ).Add( Objective.RelatedEntity1.Planet?.Name ?? "?", "ff8888" ).Add( "。\n\n" );
                ReapersPerUnitBaseInfo rData = Objective.RelatedEntity1.TryGetExternalBaseInfoAs<ReapersPerUnitBaseInfo>();
                if ( rData != null )
                {
                    int spawnTime = rData.ChrysalisHatchTime - World_AIW2.Instance.GameSecond;
                    if ( spawnTime > 0 )
                    {
                        string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( spawnTime );
                        buffer.Add( "在" ).Add( spawnTime.ToString(), color ).Add( "秒后孵化，生成收割者部队。\n" );
                    }
                    else
                        buffer.Add( "即将孵化（收割者部队来袭！）\n" );
                    buffer.Add( "内含库恩达：" ).Add( rData.CuendillarRemaining.ToString(), "ff4444" ).Add( "\n\n" );
                }
                buffer.Add( "在此茧孵化前摧毁它以消除威胁并回收其库恩达。" );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in ReaperChrsyalis.TooltipHandler: " + e, Verbosity.ShowAsError );
            }
        }
    }

    public class DysonDrilling : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "库恩达", "e16cff" ).Add( "是你的主要资源，从三个来源提取。\n\n" );
            buffer.Add( "库恩达小行星", "8888ff" ).Add( "和" ).Add( "库恩达小行星体", "88ff88" ).Add( "提供适中但稳定的收入；足以维持运转。两者都在情报菜单中列为目标。\n\n" );
            buffer.Add( "钻探整颗星球", "e16cff" ).Add( "可提供大量库恩达；足以实现真正的实力飙升。完全钻探一颗星球将使其荒废。\n\n" );
            buffer.Add( "使星球荒废后你可以在此建造戴森球；戴森球提供强大的旗舰，会自动防御你的星球。", "ffccff" );
        }
    }

    public class DysonReapers : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "收割者", "ff8888" ).Add( "是戴森联盟面临的主要派系特定威胁。它们直接猎取库恩达。\n\n" );
            buffer.Add( "收割者茧", "ff4444" ).Add( "是需要关注的关键威胁。在茧孵化前摧毁它可消除其会生成的波次并回收其储存的库恩达。如果茧孵化，可能触发全面收割者入侵。\n\n" );
            buffer.Add( "不要完全忽视收割者。", "ffcccc" ).Add( "如果不加控制，它们可以通过收割AI舰船变得极其强大。\n\n" );
            buffer.Add( "茧在情报菜单中列为目标。", "ffaaaa" );
        }
    }

    public class DysonMoons : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "卫星", "aaddff" ).Add( "是戴森联盟的关键防御平台。它们提供可建造防御和支援建筑的空间，这些建筑无法放置在其他地方。\n\n" );
            buffer.Add( "收割者定期发动", "ffcccc" ).Add( "月球入侵", "ff8888" ).Add( "：在地图上生成新卫星的事件。然而，你必须击败入侵的收割者部队后才能认领卫星。这些事件既是威胁，也是扩展防御网络的机会。\n\n" );
            buffer.Add( "优先保护卫星并在月球入侵期间认领新卫星是维持强大后期局势的关键。", "aaddff" );
        }
    }

    public class DysonRaceSpecializations : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "戴森联盟由四个种族组成，每个种族在建造其据点时贡献不同的舰船和资源收入。\n\n" );
            buffer.Add( "尖塔", "88aaff" ).Add( "据点可生产大量科技。\n" );
            buffer.Add( "天顶", "aaffaa" ).Add( "据点可生产较少量金属和科技。\n" );
            buffer.Add( "Neinzul", "ffaaaa" ).Add( "据点可生产金属和入侵点数。\n" );
            buffer.Add( "圣殿骑士", ObjectiveColors.Keyword ).Add( "据点提供最佳防御；守护者尤其可以防御多个星球。\n\n" );
            buffer.Add( "选择你的据点组合是主要的长期战略决策。你可以通过入侵菜单拆除据点以回收部分资源并重新调整构成。", "ffeecc" );
        }
    }

    public class DysonSphere : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "你可以在一颗荒废的星球上建造", "ffccff" ).Add( "戴森球", "e16cff" ).Add( "。\n\n" );
            buffer.Add( "戴森球生产强大的旗舰，会自动防御你的帝国，同时也是你钻探时运输船的目的地。\n\n" );
            buffer.Add( "将戴森球科技提升到Mark 7将使你的球体生产更强大的傀儡，但代价是引发非常显著的AI反应。", "ffccff" );
        }
    }

    public class DysonUnusedSockets : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedPlanet1 == null )
            {
                buffer.Add( "Bug in DysonUnusedSockets: null planet" );
                return;
            }
            buffer.Add( "你的据点" ).Add( Objective.RelatedPlanet1.Name, "8092ff" ).Add( "上有未使用的建筑插槽。\n\n填充插槽可增强你的防御并解锁额外能力。使用该星球上的建造菜单查看可建造内容。" );
        }
    }
}
