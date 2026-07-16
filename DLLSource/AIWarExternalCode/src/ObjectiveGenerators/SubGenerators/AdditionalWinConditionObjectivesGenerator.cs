using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class AdditionalWinConditionObjectivesGenerator
    {
        public static void CheckForMiniorFactionObjectives_BackgroundThread_ClientOrHost()
        {
            try
            {
                if ( AIWar2GalaxySettingQuickAccess.DefeatAllMinorFactions )
                {
                    foreach ( Faction faction in World_AIW2.Instance.Factions )
                    {
                        if ( faction.Type == FactionType.Player || faction.Type == FactionType.AI )//AIs are handled separately in the KingObjectivesGenerator
                            continue;
                        if ( !faction.GetIsHostileToAnyPlayerFaction() )
                            continue;
                        if ( faction.SpecialFactionData == null )
                            continue;
                        if ( faction.FactionIsDefeated )
                            continue;
                        switch ( faction.SpecialFactionData.DefeatCondition )
                        {
                            case FactionDefeatCondition.AllKingsDead:
                                GameEntity_Squad king = FactionUtilityMethods.Instance.findKing( faction );
                                if ( king != null && !king.GetHasBeenDestroyed() )
                                {
                                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                                    objective.SetHook( "DestroyCustomKing" );
                                    objective.RelatedString = ((ArcenCharacterBuffer) ArcenCharacterBuffer.GetFromPoolOrCreate( "MinorFactionWinObjectivesGenerator-1" )
                                        .NewLine().AddFactionNameInItsColor( faction )).ToStringAndReturnToPool();
                                    if ( king.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
                                        objective.RelatedEntity1 = king;
                                    objective.RelatedInt1 = faction.FactionIndex;
                                    ObjectiveCategory.AddActualObjective( objective );
                                }
                                break;
                            case FactionDefeatCondition.OwnsNoPlanets:
                                Planet planetToLiberate = null;
                                bool foundAny = false;
                                int foundAndVisible = 0;
                                int found = 0;
                                foreach ( Planet planet in faction.ControlledPlanetsSingleThread() )
                                {
                                    found++;
                                    if ( !foundAny )
                                        foundAny = true;
                                    if ( planet.IntelLevel > PlanetIntelLevel.Unexplored )
                                    {
                                        foundAndVisible++;
                                        if ( planetToLiberate == null )
                                            planetToLiberate = planet;
                                    }
                                }
                                if ( foundAny )
                                {
                                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                                    objective.SetHook( "LiberatePlanets" );
                                    objective.RelatedString = ((ArcenCharacterBuffer) ArcenCharacterBuffer.GetFromPoolOrCreate( "MinorFactionWinObjectivesGenerator-2" )
                                        .NewLine().AddFactionNameInItsColor( faction )).ToStringAndReturnToPool();
                                    objective.RelatedInt1 = faction.FactionIndex;
                                    objective.RelatedPlanet1 = planetToLiberate;
                                    ObjectiveCategory.AddActualObjective( objective );
                                }
                                break;
                            case FactionDefeatCondition.OwnsCoreTerritoryIfAnyAtAll:
                                planetToLiberate = null;
                                foundAny = false;
                                foundAndVisible = 0;
                                foreach ( Planet planet in faction.ControlledOrInfluencedPlanetsSingleThread() )
                                {
                                    if ( planet.IsZenithArchitraveHome || planet.IsZenithArchitraveTerritory )
                                        continue;
                                    if ( !foundAny )
                                        foundAny = true;
                                    if ( planet.IntelLevel > PlanetIntelLevel.Unexplored )
                                    {
                                        foundAndVisible++;
                                        if ( planetToLiberate == null )
                                            planetToLiberate = planet;
                                    }
                                }
                                if ( foundAny )
                                {
                                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                                    objective.SetHook( "LiberatePlanets_ExcludeHomeTerritory" );
                                    objective.RelatedString = ((ArcenCharacterBuffer) ArcenCharacterBuffer.GetFromPoolOrCreate( "MinorFactionWinObjectivesGenerator-3" )
                                        .NewLine().AddFactionNameInItsColor( faction )).ToStringAndReturnToPool();
                                    objective.RelatedInt1 = faction.FactionIndex;
                                    objective.RelatedPlanet1 = planetToLiberate;
                                    objective.RelatedInt2 = foundAndVisible;
                                    ObjectiveCategory.AddActualObjective( objective );
                                }
                                break;
                            case FactionDefeatCondition.HasNoMobileCombattantStrength:
                                Planet primaryPlanet = null;
                                int primaryPlanetStrength = 0;
                                int totalStrength = 0;
                                int totalVisibleStrength = 0;
                                int currentStrength;
                                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                                {
                                    currentStrength = planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Self].NonGuardMobileStrength;
                                    totalStrength += currentStrength;
                                    if ( planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                                    {
                                        totalVisibleStrength += currentStrength;
                                        if ( primaryPlanetStrength < currentStrength )
                                        {
                                            primaryPlanet = planet;
                                            primaryPlanetStrength = currentStrength;
                                        }
                                    }
                                }
                                if ( totalStrength > 0 )
                                {
                                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                                    objective.SetHook( "ExterminateMobileCombattants" );
                                    objective.RelatedString = ((ArcenCharacterBuffer) ArcenCharacterBuffer.GetFromPoolOrCreate( "MinorFactionWinObjectivesGenerator-4" )
                                        .NewLine().AddFactionNameInItsColor( faction )).ToStringAndReturnToPool();
                                    objective.RelatedInt1 = faction.FactionIndex;
                                    if ( primaryPlanet != null )
                                        objective.RelatedPlanet1 = primaryPlanet;
                                    objective.RelatedInt2 = totalVisibleStrength;
                                    objective.RelatedInt3 = totalVisibleStrength;
                                    ObjectiveCategory.AddActualObjective( objective );
                                }
                                break;
                            case FactionDefeatCondition.HasNoCombattantStrength:
                                primaryPlanet = null;
                                primaryPlanetStrength = 0;
                                totalStrength = 0;
                                totalVisibleStrength = 0;
                                StrengthData_PlanetFaction_Stance data;
                                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                                {
                                    data = planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Self];
                                    currentStrength = data.NonGuardMobileStrength + data.GuardStrength;
                                    totalStrength += currentStrength;
                                    if ( planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                                    {
                                        totalVisibleStrength += currentStrength;
                                        if ( primaryPlanetStrength < currentStrength )
                                        {
                                            primaryPlanet = planet;
                                            primaryPlanetStrength = currentStrength;
                                        }
                                    }
                                }
                                if ( totalStrength > 0 )
                                {
                                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                                    objective.SetHook( "ExterminateCombattants" );
                                    objective.RelatedString = ((ArcenCharacterBuffer) ArcenCharacterBuffer.GetFromPoolOrCreate( "MinorFactionWinObjectivesGenerator-5" )
                                        .NewLine().AddFactionNameInItsColor( faction )).ToStringAndReturnToPool();
                                    objective.RelatedInt1 = faction.FactionIndex;
                                    if ( primaryPlanet != null )
                                        objective.RelatedPlanet1 = primaryPlanet;
                                    objective.RelatedInt2 = totalVisibleStrength;
                                    objective.RelatedInt3 = totalVisibleStrength;
                                    ObjectiveCategory.AddActualObjective( objective );
                                }
                                break;
                            case FactionDefeatCondition.HasNoStrength:
                                primaryPlanet = null;
                                primaryPlanetStrength = 0;
                                totalStrength = 0;
                                totalVisibleStrength = 0;
                                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                                {
                                    currentStrength = planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Self].TotalStrength;
                                    totalStrength += currentStrength;
                                    if ( planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                                    {
                                        totalVisibleStrength += currentStrength;
                                        if ( primaryPlanetStrength < currentStrength )
                                        {
                                            primaryPlanet = planet;
                                            primaryPlanetStrength = currentStrength;
                                        }
                                    }
                                }
                                if ( totalStrength > 0 )
                                {
                                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                                    objective.SetHook( "ExterminateAll" );
                                    objective.RelatedString = ((ArcenCharacterBuffer) ArcenCharacterBuffer.GetFromPoolOrCreate( "MinorFactionWinObjectivesGenerator-6" )
                                        .NewLine().AddFactionNameInItsColor( faction )).ToStringAndReturnToPool();
                                    objective.RelatedInt1 = faction.FactionIndex;
                                    if ( primaryPlanet != null )
                                        objective.RelatedPlanet1 = primaryPlanet;
                                    objective.RelatedInt2 = totalVisibleStrength;
                                    objective.RelatedInt3 = totalVisibleStrength;
                                    ObjectiveCategory.AddActualObjective( objective );
                                }
                                break;
                        }
                    }
                }
                if ( AIWar2GalaxySettingQuickAccess.HomeworldsAreSafe )
                {
                    GameEntity_Squad playerKing;
                    int hostileStrength;
                    foreach ( Faction faction in World_AIW2.Instance.AllPlayerFactions )
                    {
                        if ( faction.FactionIsDefeated )
                            continue;
                        playerKing = FactionUtilityMethods.Instance.findKing( faction );
                        if ( playerKing == null || playerKing.GetHasBeenDestroyed() )
                            continue;
                        hostileStrength = playerKing.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                        if ( hostileStrength <= 0 )
                            continue;
                        ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "SecureHomeworld" );
                        objective.RelatedString = ((ArcenCharacterBuffer) ArcenCharacterBuffer.GetFromPoolOrCreate( "MinorFactionWinObjectivesGenerator-7" )
                            .AddPlanetNameFormated( playerKing.Planet, false )).ToStringAndReturnToPool();
                        objective.RelatedInt1 = faction.FactionIndex;
                        objective.RelatedPlanet1 = playerKing.Planet;
                        objective.RelatedInt2 = hostileStrength;
                        ObjectiveCategory.AddActualObjective( objective );
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in KingObjectivesGenerator generation: " + e, Verbosity.ShowAsError );
            }
        }
    }

    public class DestroyCustomKing: IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            Planet planet = Objective.RelatedEntity1?.Planet;
            if ( planet != null )
                World_AIW2.Instance.SwitchViewToPlanet( planet );
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            GameEntity_Squad kingIfFound = Objective.RelatedEntity1;
            Faction faction = World_AIW2.Instance.Factions[Objective.RelatedInt1];
            buffer.Add( "阻止机器威胁必须始终是我们的首要目标 - 只要它存在于我们的银河中，人类就不可能有安全的未来 - 但同时我们也在与" )
                .AddFactionNameInItsColor( faction )
                .Add( "交战。\n要彻底结束当前战争，我们需要摧毁他们的" );
            if ( kingIfFound == null )
                buffer.Add( "中央指挥能力。目前其位置未知，我们需要进一步探索银河以找到它。" );
            else
                buffer.AddFactionColoredString( kingIfFound.TypeData.DisplayName, faction ).Add( "位于" ).AddPlanetNameFormated( kingIfFound.Planet, false ).Add( "。" );
        }
    }

    public class LiberatePlanets : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            Planet planet = Objective.RelatedPlanet1;
            if ( planet != null )
                World_AIW2.Instance.SwitchViewToPlanet( planet );
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            int planetsFound = Objective.RelatedInt2;
            Planet planet = Objective.RelatedPlanet1;
            Faction faction = World_AIW2.Instance.Factions[Objective.RelatedInt1];
            buffer.Add( "阻止机器威胁必须始终是我们的首要目标 - 只要它存在于我们的银河中，人类就不可能有安全的未来 - 但同时我们也在与" )
                .AddFactionNameInItsColor( faction )
                .Add( "交战。\n要彻底结束当前战争，我们需要摧毁他们所有的行星控制器。" );
            if ( planetsFound > 1 )
                buffer.Add( "\n他们控制着我们所知的" ).Add( planetsFound ).Add( "个星球，所有这些都必须成为我们的目标" );
            else if ( planet != null )
                buffer.Add( "\n目前他们只控制着" ).AddPlanetNameFormated( planet, false ).Add( "，这意味着我们需要舰队征服或至少解放它。" );
            else
                buffer.Add( "\n目前我们有读数表明他们拥有至少一个星球，但没有确定的打击位置。我们需要进一步探索银河。" );
        }
    }

    public class LiberatePlanets_ExcludeHomeTerritory : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            Planet planet = Objective.RelatedPlanet1;
            if ( planet != null )
                World_AIW2.Instance.SwitchViewToPlanet( planet );
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            Planet planet = Objective.RelatedPlanet1;
            int planetsFound = Objective.RelatedInt2;
            Faction faction = World_AIW2.Instance.Factions[Objective.RelatedInt1];
            buffer.Add( "阻止机器威胁必须始终是我们的首要目标 - 只要它存在于我们的银河中，人类就不可能有安全的未来 - 但同时我们也在与" )
                .AddFactionNameInItsColor( faction )
                .Add( "交战。\n要彻底结束当前战争，我们至少需要将其在我们银河中的影响力限制在他们声称的核心世界（并以不懈的 Vigour 防御）。" );
            if ( planetsFound > 1 )
                buffer.Add( "\n他们控制着我们所知的" ).Add( planetsFound ).Add( "个非核心领土星球，所有这些都必须成为我们的目标" );
            else if ( planet != null )
                buffer.Add( "\n目前" ).AddPlanetNameFormated( planet, false ).Add( "是唯一可行的目标，这意味着我们需要舰队征服或至少解放它。" );
            else
                buffer.Add( "\n目前我们有读数表明他们在核心领土之外拥有至少一个星球，但没有确定的打击位置。我们需要进一步探索银河。" );
        }
    }

    public class ExterminateMobileCombattants : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            Planet planet = Objective.RelatedPlanet1;
            if ( planet != null )
                World_AIW2.Instance.SwitchViewToPlanet( planet );
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            Planet planet = Objective.RelatedPlanet1;
            int totalVisibleApplicableStrength = Objective.RelatedInt2;
            int primaryPlanetStrength = Objective.RelatedInt3;
            Faction faction = World_AIW2.Instance.Factions[Objective.RelatedInt1];
            buffer.Add( "阻止机器威胁必须始终是我们的首要目标 - 只要它存在于我们的银河中，人类就不可能有安全的未来 - 但同时我们也在与" )
                .AddFactionNameInItsColor( faction )
                .Add( "交战。\n要彻底结束当前战争，我们需要摧毁他们所有的移动战斗单位，使他们不再威胁我们的世界。" );
            if ( planet != null )
            {
                buffer.Add( "\n他们" );
                if ( totalVisibleApplicableStrength == primaryPlanetStrength )
                    buffer.Add( "唯一的" );
                else
                    buffer.Add( "主要的" );
                buffer.Add( "可见兵力为" ).WrapStrengthTruncated( primaryPlanetStrength, true, false ).Add( "，位于" ).AddPlanetNameFormated( planet, false ).Add( "。" );
            }
            if ( totalVisibleApplicableStrength != primaryPlanetStrength || totalVisibleApplicableStrength == 0 )
            {
                if ( totalVisibleApplicableStrength > 0 )
                    buffer.Add( "\n我们观测到合计" ).WrapStrengthTruncated( totalVisibleApplicableStrength, true, false ).Add( "的兵力在银河中。" );
                else
                    buffer.Add( "\n我们的读数显示他们存在，但迄今为止我们看不到他们的单位。我们需要进一步探索银河。" );
            }
        }
    }

    public class ExterminateCombattants : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            Planet planet = Objective.RelatedPlanet1;
            if ( planet != null )
                World_AIW2.Instance.SwitchViewToPlanet( planet );
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            Planet planet = Objective.RelatedPlanet1;
            int totalVisibleApplicableStrength = Objective.RelatedInt2;
            int primaryPlanetStrength = Objective.RelatedInt3;
            Faction faction = World_AIW2.Instance.Factions[Objective.RelatedInt1];
            buffer.Add( "阻止机器威胁必须始终是我们的首要目标 - 只要它存在于我们的银河中，人类就不可能有安全的未来 - 但同时我们也在与" )
                .AddFactionNameInItsColor( faction )
                .Add( "交战。\n要彻底结束当前战争，我们需要摧毁他们所有的军事力量，包括攻击单位和行星防御。" );
            if ( planet != null )
            {
                buffer.Add( "\n他们" );
                if ( totalVisibleApplicableStrength == primaryPlanetStrength )
                    buffer.Add( "唯一的" );
                else
                    buffer.Add( "主要的" );
                buffer.Add( "可见兵力为" ).WrapStrengthTruncated( primaryPlanetStrength, true, false ).Add( "，位于" ).AddPlanetNameFormated( planet, false ).Add( "。" );
            }
            if ( totalVisibleApplicableStrength != primaryPlanetStrength || totalVisibleApplicableStrength == 0 )
            {
                if ( totalVisibleApplicableStrength > 0 )
                    buffer.Add( "\n我们观测到合计" ).WrapStrengthTruncated( totalVisibleApplicableStrength, true, false ).Add( "的兵力在银河中。" );
                else
                    buffer.Add( "\n我们的读数显示他们存在，但迄今为止我们看不到他们的单位。我们需要进一步探索银河。" );
            }
        }
    }

    public class ExterminateAll : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            Planet planet = Objective.RelatedPlanet1;
            if ( planet != null )
                World_AIW2.Instance.SwitchViewToPlanet( planet );
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            Planet planet = Objective.RelatedPlanet1;
            int totalVisibleApplicableStrength = Objective.RelatedInt2;
            int primaryPlanetStrength = Objective.RelatedInt3;
            Faction faction = World_AIW2.Instance.Factions[Objective.RelatedInt1];
            buffer.Add( "阻止机器威胁必须始终是我们的首要目标 - 只要它存在于我们的银河中，人类就不可能有安全的未来 - 但同时我们也在与" )
                .AddFactionNameInItsColor( faction )
                .Add( "交战。\n要彻底结束当前战争，我们需要摧毁他们的一切，无论是军事还是非军事。" );
            if ( planet != null )
            {
                buffer.Add( "\n他们" );
                if ( totalVisibleApplicableStrength == primaryPlanetStrength )
                    buffer.Add( "唯一的" );
                else
                    buffer.Add( "主要的" );
                buffer.Add( "可见兵力为" ).WrapStrengthTruncated( primaryPlanetStrength, true, false ).Add( "，位于" ).AddPlanetNameFormated( planet, false ).Add( "。" );
            }
            if ( totalVisibleApplicableStrength != primaryPlanetStrength || totalVisibleApplicableStrength == 0 )
            {
                if ( totalVisibleApplicableStrength > 0 )
                    buffer.Add( "\n我们观测到合计" ).WrapStrengthTruncated( totalVisibleApplicableStrength, true, false ).Add( "的兵力在银河中。" );
                else
                    buffer.Add( "\n我们的读数显示他们存在，但迄今为止我们看不到他们的单位。我们需要进一步探索银河。" );
            }
        }
    }

    public class SecureHomeworld : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            Planet planet = Objective.RelatedPlanet1;
            if ( planet != null )
                World_AIW2.Instance.SwitchViewToPlanet( planet );
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            Planet planet = Objective.RelatedPlanet1;
            int enemyStrengthThere = Objective.RelatedInt2;
            Faction faction = World_AIW2.Instance.Factions[Objective.RelatedInt1];
            bool isOurHomeworld = World_AIW2.Instance.GetLocalPlayerFactionOrNull() == faction;
            buffer.Add( "为了确保人类的未来，我们必须保护" ).AddPlanetNameFormated( planet, false ).Add( "，" );
            if ( isOurHomeworld )
                buffer.Add( "我们的母星" );
            else
                buffer.AddFactionNameInItsColor( faction ).Add( "的母星" );
            buffer.Add( "的安全。目前有" ).WrapStrengthTruncated( enemyStrengthThere, true, false ).Add( "敌人在这里。" );
        }
    }
}
