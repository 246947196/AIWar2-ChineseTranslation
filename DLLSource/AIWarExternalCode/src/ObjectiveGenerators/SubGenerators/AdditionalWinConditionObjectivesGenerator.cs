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
            buffer.Add( "Stopping the machine menace has to be our primary objective at all times - there can never be a secure future for Humanity with it in our galaxy - but at the same time we are at war with the " )
                .AddFactionNameInItsColor( faction )
                .Add( " as well.\nTo fully conclude the current war we need to destroy their " );
            if ( kingIfFound == null )
                buffer.Add( "central command capabilities. Right now it's location is unknown, we need to further explore the galaxy to find it." );
            else
                buffer.AddFactionColoredString( kingIfFound.TypeData.DisplayName, faction ).Add( " on " ).AddPlanetNameFormated( kingIfFound.Planet, false ).Add( "." );
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
            buffer.Add( "Stopping the machine menace has to be our primary objective at all times - there can never be a secure future for Humanity with it in our galaxy - but at the same time we are at war with the " )
                .AddFactionNameInItsColor( faction )
                .Add( " as well.\nTo fully conclude the current war we need to destroy all their planetary controllers." );
            if ( planetsFound > 1 )
                buffer.Add( "\nThey control " ).Add( planetsFound ).Add( " planets which we know of, all of them must be our targets" );
            else if ( planet != null )
                buffer.Add( "\nCurrently they only control " ).AddPlanetNameFormated( planet, false ).Add( ", which means we will need our fleets to conquer or at the very least liberate it." );
            else
                buffer.Add( "\nRight now we have readings that suggest they own at least one planet, but no fixed location on where to strike. We need to further explore the galaxy." );
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
            buffer.Add( "Stopping the machine menace has to be our primary objective at all times - there can never be a secure future for Humanity with it in our galaxy - but at the same time we are at war with the " )
                .AddFactionNameInItsColor( faction )
                .Add( " as well.\nTo fully conclude the current war we need to at the very least limit their influence in our galaxy to the core worlds they claim (and defend with unrelenting vigour)." );
            if ( planetsFound > 1 )
                buffer.Add( "\nThey control " ).Add( planetsFound ).Add( " non-core territory planets which we know of, all of them must be our targets" );
            else if ( planet != null )
                buffer.Add( "\nCurrently " ).AddPlanetNameFormated( planet, false ).Add( " is the only viable target, which means we will need our fleets to conquer or at the very least liberate it." );
            else
                buffer.Add( "\nRight now we have readings that suggest they own at least one planet beyond their core territory, but no fixed location on where to strike. We need to further explore the galaxy." );
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
            buffer.Add( "Stopping the machine menace has to be our primary objective at all times - there can never be a secure future for Humanity with it in our galaxy - but at the same time we are at war with the " )
                .AddFactionNameInItsColor( faction )
                .Add( " as well.\nTo fully conclude the current war we need to destroy all their mobile combattants so they can no longer threaten our worlds." );
            if ( planet != null )
            {
                buffer.Add( "\nTheir " );
                if ( totalVisibleApplicableStrength == primaryPlanetStrength )
                    buffer.Add( "only " );
                else
                    buffer.Add( "primary " );
                buffer.Add( "visible force of " ).WrapStrengthTruncated( primaryPlanetStrength, true, false ).Add( " is located on " ).AddPlanetNameFormated( planet, false ).Add( "." );
            }
            if ( totalVisibleApplicableStrength != primaryPlanetStrength || totalVisibleApplicableStrength == 0 )
            {
                if ( totalVisibleApplicableStrength > 0 )
                    buffer.Add( "\nWe have vision on a combined total of " ).WrapStrengthTruncated( totalVisibleApplicableStrength, true, false ).Add( " in the galaxy." );
                else
                    buffer.Add( "\nOur readings suggest that they are present, but so far we have no visual on their ships. We need to further explore the galaxy." );
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
            buffer.Add( "Stopping the machine menace has to be our primary objective at all times - there can never be a secure future for Humanity with it in our galaxy - but at the same time we are at war with the " )
                .AddFactionNameInItsColor( faction )
                .Add( " as well.\nTo fully conclude the current war we need to destroy all their military, both their attack ships and planetary defenses." );
            if ( planet != null )
            {
                buffer.Add( "\nTheir " );
                if ( totalVisibleApplicableStrength == primaryPlanetStrength )
                    buffer.Add( "only " );
                else
                    buffer.Add( "primary " );
                buffer.Add( "visible force of " ).WrapStrengthTruncated( primaryPlanetStrength, true, false ).Add( " is located on " ).AddPlanetNameFormated( planet, false ).Add( "." );
            }
            if ( totalVisibleApplicableStrength != primaryPlanetStrength || totalVisibleApplicableStrength == 0 )
            {
                if ( totalVisibleApplicableStrength > 0 )
                    buffer.Add( "\nWe have vision on a combined total of " ).WrapStrengthTruncated( totalVisibleApplicableStrength, true, false ).Add( " in the galaxy." );
                else
                    buffer.Add( "\nOur readings suggest that they are present, but so far we have no visual on their ships. We need to further explore the galaxy." );
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
            buffer.Add( "Stopping the machine menace has to be our primary objective at all times - there can never be a secure future for Humanity with it in our galaxy - but at the same time we are at war with the " )
                .AddFactionNameInItsColor( faction )
                .Add( " as well.\nTo fully conclude the current war we need to destroy absolutely all of them. Military and not." );
            if ( planet != null )
            {
                buffer.Add( "\nTheir " );
                if ( totalVisibleApplicableStrength == primaryPlanetStrength )
                    buffer.Add( "only " );
                else
                    buffer.Add( "primary " );
                buffer.Add( "visible force of " ).WrapStrengthTruncated( primaryPlanetStrength, true, false ).Add( " is located on " ).AddPlanetNameFormated( planet, false ).Add( "." );
            }
            if ( totalVisibleApplicableStrength != primaryPlanetStrength || totalVisibleApplicableStrength == 0 )
            {
                if ( totalVisibleApplicableStrength > 0 )
                    buffer.Add( "\nWe have vision on a combined total of " ).WrapStrengthTruncated( totalVisibleApplicableStrength, true, false ).Add( " in the galaxy." );
                else
                    buffer.Add( "\nOur readings suggest that they are present, but so far we have no visual on their ships. We need to further explore the galaxy." );
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
            buffer.Add( "In order to secure the future of Mankind we must keep " ).AddPlanetNameFormated( planet, false ).Add( ", " );
            if ( isOurHomeworld )
                buffer.Add( "our homeworld" );
            else
                buffer.Add( "the homeworld of " ).AddFactionNameInItsColor( faction );
            buffer.Add( " safe. Right now " ).WrapStrengthTruncated( enemyStrengthThere, true, false ).Add( " enemies are here." );
        }
    }
}
