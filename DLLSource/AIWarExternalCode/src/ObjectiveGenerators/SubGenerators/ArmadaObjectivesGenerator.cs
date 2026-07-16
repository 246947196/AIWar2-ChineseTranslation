using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class ArmadaObjectivesGenerator
    {
        public static void CheckForArmadaObjectives_BackgroundThread_ClientOrHost()
        {
            try
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( !ArmadaFactionBaseInfo.GetIsThisAnArmadaFaction( localFaction ) )
                    return;
                ArmadaFactionBaseInfo info = localFaction.GetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
                if ( info == null )
                    return;
                GenerateUnusedSocketsObjectives( info );
                GenerateUnclaimedStarbaseObjectives( info );
                GenerateArmadaStrategyObjectives( info );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in ArmadaObjectivesGenerator generation: " + e, Verbosity.ShowAsError );
            }
        }

        private static void GenerateUnusedSocketsObjectives( ArmadaFactionBaseInfo info )
        {
            foreach ( GameEntity_Squad city in info.Starbases.DisplaySquads() )
            {
                if ( city.FleetMembership?.Fleet == null )
                    continue;
                if ( city.FleetMembership.Fleet.CalculateRemainingCitySockets() < 1 )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "UnusedSockets" );
                objective.RelatedPlanet1 = city.Planet;
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        private static void GenerateUnclaimedStarbaseObjectives( ArmadaFactionBaseInfo info )
        {
            foreach ( GameEntity_Squad starbase in info.UnownedStarbases.DisplaySquads() )
            {
                if ( starbase.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "ArmadaUnclaimedStarbase" );
                objective.RelatedEntity1 = starbase;
                objective.RelatedPlanet1 = starbase.Planet;
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        private static void GenerateArmadaStrategyObjectives( ArmadaFactionBaseInfo info )
        {
            if ( info.UnownedStarbases.Count > 0 )
            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "ArmadaClaimStarbases" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "ArmadaSwarmTactics" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "ArmadaMining" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "ArmadaRangers" );
                ObjectiveCategory.AddActualObjective( obj );
            }

            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "ArmadaFightTyderian" );
                ObjectiveCategory.AddActualObjective( obj );
            }
        }
    }

    public class UnusedSockets : IObjectiveHookManager
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
                buffer.Add( "Bug in UnusedSockets: null planet" );
                return;
            }
            buffer.Add( "你的星站" ).Add( Objective.RelatedPlanet1.Name, ObjectiveColors.Reward ).Add( "上有未使用的建筑插槽。\n\n填充插槽可增强防御并解锁新能力。打开该星站的舰队菜单查看可建造内容。" );
        }
    }

    public class ArmadaUnclaimedStarbase : IObjectiveHookManager
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
                buffer.Add( "Bug in ArmadaUnclaimedStarbase: null entity" );
                return;
            }
            buffer.AddObjectiveEntityHeader( Objective.RelatedEntity1, Objective.RelatedEntity1.GetFactionCenterColorHexBrighter_Safe() );
            string starbaseDescription = Objective.RelatedEntity1.TypeData.Description;
            if ( !string.IsNullOrEmpty( starbaseDescription ) )
                buffer.Add( starbaseDescription ).Add( "\n\n" );
            buffer.Add( "一个未认领的舰队星站位于" ).Add( Objective.RelatedEntity1.GetPlanetName_Safe(), ObjectiveColors.Reward ).Add( "。\n\n" );
            buffer.Add( "占领后可获得新的" ).Add( "单位类型", ObjectiveColors.Keyword ).Add( "。" );
        }
    }

    public class ArmadaClaimStarbases : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "舰队帝国可以通过认领散布在银河各处的", ObjectiveColors.Hint ).Add( "星站", "ffaa44" ).Add( "来扩张。\n\n" );
            buffer.Add( "每个星站都是一个行动基地：它容纳你的舰队、生产游侠，并锚定你的采矿补给线。地图上的星站可让你获得强大的新型单位类型，并且升级也更便宜。\n\n" );
            buffer.Add( "未认领的星站在情报菜单的关键可占领物下列出。", ObjectiveColors.Hint );
        }
    }

    public class ArmadaSwarmTactics : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "舰队的标志性攻势是", ObjectiveColors.Hint ).Add( "蝗虫群", "ffaa44" ).Add( "。\n\n" );
            buffer.Add( "蝗虫发射器", ObjectiveColors.Keyword ).Add( "生成蝗虫；这是一种可消耗的蜂群单位，以数量压制敌人。更多的蝗虫发射器将生成更多蝗虫。\n\n" );
            buffer.Add( "蝗虫诱饵", ObjectiveColors.Keyword ).Add( "吸引来自所有发射器的蝗虫。到达后，蝗虫会吸收原本会击中你星站或旗舰的伤害，同时造成大量伤害。一旦你在战斗中占据优势，诱饵会逐渐损耗。\n\n" );

            buffer.Add( "一旦诱饵被摧毁，蝗虫将扩散到银河各处攻击敌人。", ObjectiveColors.Hint );
        }
    }

    public class ArmadaMining : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "舰队的主要收入来源是", ObjectiveColors.Hint ).Add( "采矿", "ffaa44" ).Add( "。\n\n" );
            buffer.Add( "在敌对星球上建造", ObjectiveColors.Keyword ).Add( "矿场", ObjectiveColors.Keyword ).Add( "来提取资源。每个矿场满仓后会自动转化为运输船，将货物运回最近的星站。\n\n" );
            buffer.Add( "你开采的星球越多，产生的收入就越多。但要注意，每个矿场在运输途中很脆弱。保护返回星站的路线至关重要。\n\n" );
            buffer.Add( "查看资源栏的Tyderian部分以了解当前采矿收入的细分。", ObjectiveColors.Hint );
        }
    }

    public class ArmadaRangers : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "游侠", "ffaa44" ).Add( "是舰队的自动防御单位，每个游侠绑定到母星站。\n\n" );
            buffer.Add( "它们会自动攻击星站所在星球上的威胁，无需玩家指挥。当星站失守时，其游侠也会随之失去。\n\n" );
            buffer.Add( "游侠前哨站", ObjectiveColors.Keyword ).Add( "增加你的游侠上限，让你每个星站可以部署更多防御者。\n" );
            buffer.Add( "凶暴游侠前哨站", ObjectiveColors.Keyword ).Add( "解锁凶暴游侠；更重型、更强大的变体，适用于严重威胁。\n\n" );
            buffer.Add( "在星站插槽中建造前哨站以保持高防御上限。", ObjectiveColors.Hint );
        }
    }

    public class ArmadaFightTyderian : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "Tyderian矿脉", "ffaa44" ).Add( "既是威胁也是机遇。\n\n" );
            buffer.Add( "放任不管，矿脉会增强AI并生成敌对单位。但摧毁它们可获得", ObjectiveColors.Hint ).Add( "Tyderian", "ffaa44" ).Add( "，一种用于关键舰队升级和建造新星站的资源。\n\n" );
            buffer.Add( "你也可以入侵矿脉以获得更大收益：\n" );
            buffer.Add( "  召唤Tyderian憎恶", ObjectiveColors.Keyword ).Add( "：击败它以获得大量Tyderian奖励。\n" );
            buffer.Add( "  召唤Tyderian终末", ObjectiveColors.Keyword ).Add( "：击败它以获得科技。\n" );
            buffer.Add( "  增强矿脉", ObjectiveColors.Keyword ).Add( "：提升矿脉产量，但风险增加。\n\n" );
        }
    }

}
