using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class FlagshipObjectivesGenerator
    {
        public static void CheckForFlagshipObjectives_BackgroundThread_ClientOrHost()
        {
            try
            {
                Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( playerFaction == null )
                    playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                if ( playerFaction == null )
                    return;
                if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( playerFaction ) )
                    return;

                Faction neutralFaction = World_AIW2.Instance.GetNeutralFaction();
                foreach ( GameEntity_Squad entity in neutralFaction.Squads( EntityRollupType.MobileFleetFlagships ) )
                {
                    if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                        continue;
                    {
                        #region Reclaim Flagship
                        {
                            ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                            switch ( entity.TypeData.SpecialType )
                            {
                                case SpecialEntityType.MobileOfficerCombatFleetFlagship:
                                    objective.SetHook( "ReclaimFlagship_Officer" );
                                    break;
                                case SpecialEntityType.MobileStrikeCombatFleetFlagship:
                                case SpecialEntityType.MobileCustomUnattachedFleetFlagship:
                                    objective.SetHook( "ReclaimFlagship_Strike" );
                                    break;
                                case SpecialEntityType.MobileSupportFleetFlagship:
                                    objective.SetHook( "ReclaimFlagship_Support" );
                                    break;
                                default:
                                    objective.SetHook( "ReclaimFlagship_Base" );
                                    break;
                            }
                            objective.RelatedEntity1 = entity;
                            ObjectiveCategory.AddActualObjective( objective );
                        }
                        {
                            #region Check for optional objective types that have been enabled in settings to show specific unit types (aoe, sniper, melee, etc)
                            foreach ( ObjectiveCategory objectiveCategory in ObjectiveCategoryTable.Instance.Rows )
                            {
                                if ( !objectiveCategory.IsFilteredUnitAquisitionObjective )
                                    continue;
                                bool foundMatchingUnitType = false;
                                Fleet entityFleet = entity.GetFleetOrNull_Safe();
                                if ( entityFleet != null )
                                {
                                    foreach ( FleetMembership mem in entityFleet.MemberGroupsUnsorted_Sim )
                                    {
                                        GameEntityTypeData entityType = mem.TypeData;
                                        if ( mem.TypeData == entity.TypeData )
                                            continue; // skip flagship
                                        if ( !AIObjectivesGenerator.GetEntityMatchesObjectiveCategory( entityType, objectiveCategory ) )
                                            continue;
                                        foundMatchingUnitType = true;
                                        break;
                                    }
                                }
                                if ( !foundMatchingUnitType )
                                    continue;
                                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                                objective.SetHook( objectiveCategory.FleetHookName );
                                objective.RelatedEntity1 = entity;
                                ObjectiveCategory.AddActualObjective( objective );
                            }
                            #endregion
                        }
                        #endregion
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in FlagshipObjectivesGenerator generation: " + e, Verbosity.ShowAsError );
            }
        }

        public static void CheckForBattlestationObjectives_BackgroundThread_ClientOrHost()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( playerFaction ) )
                return;

            Faction neutralFaction = World_AIW2.Instance.GetNeutralFaction();
            foreach ( GameEntity_Squad entity in neutralFaction.Squads( EntityRollupType.Battlestation ) )
            {
                if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                {
                    #region Reclaim Battlestation
                    {
                        ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                        switch ( entity.TypeData.SpecialType )
                        {
                            case SpecialEntityType.BattlestationCitadel:
                                objective.SetHook( "ReclaimCitadel" );
                                break;
                            default:
                                objective.SetHook( "ReclaimBattlestation" );
                                break;
                        }

                        objective.RelatedEntity1 = entity;
                        ObjectiveCategory.AddActualObjective( objective );

                    }
                    {
                        #region Check for optional objective types that have been enabled in settings to show specific unit types (aoe, sniper, melee, etc)
                        foreach ( ObjectiveCategory objectiveCategory in ObjectiveCategoryTable.Instance.Rows )
                        {
                            if ( !objectiveCategory.IsFilteredUnitAquisitionObjective )
                                continue;
                            bool foundMatchingUnitType = false;
                            Fleet entityFleet = entity.GetFleetOrNull_Safe();
                            if ( entityFleet != null )
                            {
                                foreach ( FleetMembership mem in entityFleet.MemberGroupsUnsorted_Sim )
                                {
                                    GameEntityTypeData entityType = mem.TypeData;
                                    if ( mem.TypeData == entity.TypeData )
                                        continue; // skip flagship
                                    if ( !AIObjectivesGenerator.GetEntityMatchesObjectiveCategory( entityType, objectiveCategory ) )
                                        continue;
                                    foundMatchingUnitType = true;
                                    break;
                                }
                            }
                            if ( !foundMatchingUnitType )
                                continue;
                            ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                            objective.SetHook( objectiveCategory.CitadelHookName );
                            objective.RelatedEntity1 = entity;
                            ObjectiveCategory.AddActualObjective( objective );
                        }
                        #endregion
                    }
                    #endregion
                }
            }
        }
    }

    public class ReclaimFlagship : IObjectiveHookManager
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
            Fleet entityFleet = entity.GetFleetOrNull_Safe();
            int shipLineCount = 0;
            if ( entityFleet != null )
            {
                foreach ( FleetMembership mem in entityFleet.MemberGroupsUnsorted_Sim )
                {
                    if ( mem.TypeData == entity.TypeData )
                        continue;
                    if ( Objective.Hook.Category != null && !AIObjectivesGenerator.GetEntityMatchesObjectiveCategory( mem.TypeData, Objective.Hook.Category ) )
                        continue;
                    if ( mem.EffectiveSquadCap <= 0 || mem.TypeData.CannotActuallyBeBuilt_IsNotAShipLine )
                        continue;
                    shipLineCount++;
                }
            }


            buffer.AddObjectiveEntityHeader( entity, ObjectiveColors.Reward );
            string flagshipDescription = entity.TypeData.Description;
            if ( !string.IsNullOrEmpty( flagshipDescription ) )
                buffer.Add( flagshipDescription ).Add( "\n\n" );
            buffer.Add( "Claim this Fleet, found on " + entity.GetPlanetName_Safe() + "." ).Add("\n");
            if ( shipLineCount == 0 )
                return;
            buffer.Add( "<color=#8092ff>" ).Add( entityFleet == null ? 0 : shipLineCount ).Add( "</color>" ).Add( " Units" );
            if ( Objective.Hook.Category != null && Objective.Hook.Category.IsFilteredUnitAquisitionObjective )
                buffer.Add( ", including" );
            buffer.Add( ":\n " );
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( entityFleet != null )
            {
                foreach ( FleetMembership mem in entityFleet.MemberGroupsSorted_NonSim_ForUIOnly_SeriouslyDoNotCallFromBGCode() )
                {
                    if ( mem.TypeData == entity.TypeData )
                        continue;
                    if ( Objective.Hook.Category != null && !AIObjectivesGenerator.GetEntityMatchesObjectiveCategory( mem.TypeData, Objective.Hook.Category ) )
                        continue;
                    if ( mem.EffectiveSquadCap <= 0 || mem.TypeData.CannotActuallyBeBuilt_IsNotAShipLine )
                        continue;
                    buffer.AddShipIconInline(mem.TypeData, playerFaction, TextStyle.Ship_Sprite_Ency).Add( "  " );
                    byte mark = playerFaction.GetGlobalMarkLevelForShipLine( mem.TypeData );
                    GameEntityTypeData.MarkLevelStats markStats = mem.TypeData.MarkStatsFor( mark );
                    buffer.Add( "<color=#" ).Add( markStats.MarkLevel.ColorHex ).Add( ">" );
                    buffer.Add( mem.TypeData == null ? "nulltype" : mem.TypeData.DisplayName )
                        .Add( mem.ForMark == null ? "nullmark" : " " + markStats.MarkLevel.Abbreviation );
                    buffer.Add( "</color>" );
                    int effectiveHere = mem.GetCountPresent( true, ExtraFromStacks.IncludePrecalc );
                    if ( mem.EffectiveSquadCap <= 0 )
                    {
                        if ( effectiveHere > mem.CalculateTransportedContentsCount() )
                        {
                            buffer.Add( " x" ).Add( "<color=#" ).Add( ShipLineEntry.GetColorForShipLineScore( mem.Score ) ).Add( ">" ).Add( effectiveHere ).Add( "</color>" );
                        }
                    }
                    else
                    {
                        if ( mem.EffectiveSquadCap > mem.CalculateTransportedContentsCount() )
                        {
                            buffer.Add( " x" ).Add( "<color=#" ).Add( ShipLineEntry.GetColorForShipLineScore( mem.Score ) ).Add( ">" ).Add( mem.EffectiveSquadCap ).Add( "</color>" );
                        }
                    }
                    buffer.Add( "\n" );
                    List<TechUpgrade> techsForLine = mem.TypeData.TechUpgradesThatBenefitMe;
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
    }

    public class ReclaimBattlestation : IObjectiveHookManager
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
            Fleet entityFleet = entity.GetFleetOrNull_Safe();
            int tmpstrength = entityFleet == null ? 0 : entityFleet.GetMaxStrengthOfFleet_ForUIOnly( true );
            int strength = tmpstrength / 1000;
            if ( tmpstrength % 1000 > 500 )
                strength++;
            buffer.AddObjectiveEntityHeader( entity, ObjectiveColors.Reward );
            string battlestationDescription = entity.TypeData.Description;
            if ( !string.IsNullOrEmpty( battlestationDescription ) )
                buffer.Add( battlestationDescription ).Add( "\n\n" );
            buffer.Add( "Claim this Battlestation, found on " + entity.GetPlanetName_Safe() + " with strength " ).Add( strength, ObjectiveColors.Reward ).Add( "." );
            buffer.Add( "\n" ).Add( " Units" );
            if ( Objective.Hook.Category != null && Objective.Hook.Category.IsFilteredUnitAquisitionObjective )
                buffer.Add( ", including" );
            buffer.Add( ":\n" );
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( entityFleet != null )
            {
                foreach ( FleetMembership mem in entityFleet.MemberGroupsSorted_NonSim_ForUIOnly_SeriouslyDoNotCallFromBGCode() )
                {
                    if ( mem.TypeData == entity.TypeData )
                        continue;
                    if ( Objective.Hook.Category != null && !AIObjectivesGenerator.GetEntityMatchesObjectiveCategory( mem.TypeData, Objective.Hook.Category ) )
                        continue;
                    if ( mem.EffectiveSquadCap <= 0 || mem.TypeData.CannotActuallyBeBuilt_IsNotAShipLine )
                        continue;
                    buffer.AddShipIconInline(mem.TypeData, playerFaction, TextStyle.Ship_Sprite_Ency).Add( "  " );

                    byte mark = playerFaction.GetGlobalMarkLevelForShipLine( mem.TypeData );
                    GameEntityTypeData.MarkLevelStats markStats = mem.TypeData.MarkStatsFor( mark );
                    buffer.Add( "<color=#" ).Add( markStats.MarkLevel.ColorHex ).Add( ">" );

                    buffer.Add( mem.TypeData == null ? "nulltype" : mem.TypeData.DisplayName )
                        .Add( mem.ForMark == null ? "nullmark" : " " + mem.ForMark.MarkLevel.Abbreviation );
                    buffer.Add( "</color>" );
                    int effectiveHere = mem.GetCountPresent( true, ExtraFromStacks.IncludePrecalc );
                    if ( mem.EffectiveSquadCap <= 0 )
                    {
                        if ( effectiveHere > mem.CalculateTransportedContentsCount() )
                        {
                            buffer.Add( " x" ).Add( "<color=#" ).Add( "7699ff" ).Add( ">" ).Add( effectiveHere ).Add( "</color>" );
                        }
                    }
                    else
                    {
                        if ( mem.EffectiveSquadCap > mem.CalculateTransportedContentsCount() )
                        {
                            buffer.Add( " x" ).Add( "<color=#" ).Add( "7699ff" ).Add( ">" ).Add( mem.EffectiveSquadCap ).Add( "</color>" );
                        }
                    }
                    buffer.Add( "\n" );
                    List<TechUpgrade> techsForLine = mem.TypeData.TechUpgradesThatBenefitMe;
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
    }
}
