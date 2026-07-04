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
                    objective.DisplayNameBase = "Destroy ";
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

                buffer.Add( "Available Upgrades:\n\n", ObjectiveColors.Header );
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
                            buffer.Add( " ×" + shipsToGet, "77a1aa" );
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
                buffer.Add( "\nTip: ", ObjectiveColors.Header ).Add( "kill the AI Command Station on this planet first to reduce the hacking cost." );
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
                buffer.Add( "On " ).Add( entity.GetPlanetName_Safe(), ObjectiveColors.Reward ).Add( ".\n\n" );
                buffer.Add( "Destroying it prevents the Templar from expanding and grants you resources. Taking out " ).Add( "Constructors", ObjectiveColors.Keyword ).Add( " is a good way to weaken the Templar." );
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
                buffer.Add("This structure enhances your necromancy for the Flagship bolstered by the Necropolis on the planet:\n\n");
                GameEntityTypeData skeletonType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "SkeletonAmplifier" );
                GameEntityTypeData wightType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "WightAmplifier" );
                GameEntityTypeData mummyType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "MummyAmplifier" );
                if ( entity_DLC3TypeData.BonusSkeletonPercent > 0 )
                {
                    if ( skeletonType != null ) buffer.AddShipIconInline( skeletonType, localFaction, TextStyle.Ship_Sprite_Ency ).Add( " " );
                    buffer.Add( "Skeletons", ObjectiveColors.Reward ).Add( ": +" ).Add( entity_DLC3TypeData.BonusSkeletonPercent, ObjectiveColors.Reward ).Add( "% chance to raise multiples\n\n" );
                }
                if ( entity_DLC3TypeData.BonusWightPercent > 0 )
                {
                    if ( wightType != null ) buffer.AddShipIconInline( wightType, localFaction, TextStyle.Ship_Sprite_Ency ).Add( " " );
                    buffer.Add( "Wights", ObjectiveColors.Reward ).Add( ": +" ).Add( entity_DLC3TypeData.BonusWightPercent, ObjectiveColors.Reward ).Add( "% chance to raise multiples\n\n" );
                }
                if ( entity_DLC3TypeData.BonusMummyPercent > 0 )
                {
                    if ( mummyType != null ) buffer.AddShipIconInline( mummyType, localFaction, TextStyle.Ship_Sprite_Ency ).Add( " " );
                    buffer.Add( "Mummies", ObjectiveColors.Reward ).Add( ": +" ).Add( entity_DLC3TypeData.BonusMummyPercent, ObjectiveColors.Reward ).Add( "% chance to raise multiples\n\n" );
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
            buffer.Add( "What Rifts Give You\n", ObjectiveColors.Header );
            buffer.Add( "Ships and Upgrades", ObjectiveColors.Reward ).Add( ": rifts provide ships and upgrades that directly strengthen your army.\n" );
            buffer.Add( "Flagship Blueprints", ObjectiveColors.Reward ).Add( ": some rifts unlock powerful flagship variants.\n\n" );

            buffer.Add( "Key Tips\n", ObjectiveColors.Header );
            buffer.Add( "Kill the Command Station first", ObjectiveColors.Reward ).Add( ": destroying the Command Station on a rift's planet makes hacking it cheaper.\n" );
            buffer.Add( "See the Tips sidebar for more details about Rifts." );
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
            buffer.Add( "What You Gain\n", ObjectiveColors.Header );
            buffer.Add( "Fighting the Templar is your primary source of Hacking Points and Science.\n\n" );

            buffer.Add( "Key Targets\n\n", ObjectiveColors.Header );
            Faction templarFaction = FactionUtilityMethods.Instance.GetTemplarFaction();
            GameEntityTypeData constructorType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "TemplarConstructor" );
            GameEntityTypeData riftType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "TemplarRift" );
            if ( constructorType != null )
                buffer.AddShipIconInline( constructorType, templarFaction, TextStyle.Ship_Sprite_Ency ).Add( " " );
            buffer.Add( "Templar Constructors", ObjectiveColors.Reward ).Add( "\n\n" );
            buffer.Add( "Highest priority. Destroying them prevents Templar expansion and grants bonus resources.\n\n" );
            if ( riftType != null )
                buffer.AddShipIconInline( riftType, templarFaction, TextStyle.Ship_Sprite_Ency ).Add( " " );
            buffer.Add( "Templar Rifts", ObjectiveColors.Reward ).Add( "\n\n" );
            buffer.Add( "Hack them for ships, upgrades, and flagship blueprints.\n\n" );

            buffer.Add( "See the Tips and Journal sidebars for more details." );
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
            buffer.Add( "It is important to upgrade your flagships. You can do this via the Tech Menu or the Fleet menu; upgrading costs Essence, and is an important way to increase your fleet's combat power. Also, higher tier Flagships are eligible to be transformed into more powerful variants. You can get these variants by hacking Elderlings or through Rifts. The most powerful variants are from fighting Elderlings.\n\nDeciding how to allocate your Essence (into flagship upgrades, necropolis upgrades or building necropolises) is an important decision." );
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
            buffer.Add( "Alongside upgrading a flagship's mark levels, there's a lot of power in using new flagship variants. To do this you must claim Blueprints. Some Blueprints are available from Rifts, but the most powerful Blueprints are obtained by using the Transform Elderling hack on an Elderling and then winning the resulting battle.\n\nTo use a Blueprint on a flagship, look in the Hacking Menu for 'Transform Flagship'." );
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
            buffer.Add( "In general you want to use the hexes in your Necropolises. When looking on the galaxy map, a Necropolis will show its count of unused hexes just under the icon.\n\nNecropolises with unused hexes:" );

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
            buffer.Add( "It is important to upgrade your Necropolises. You can do this via the Tech Menu or the Fleet menu; upgrading costs Essence. Upgrading a Necropolis gives more Hexes, allowing you to build more structures to defend or strengthen your fleet.\n\nDeciding how to allocate your Essence (into flagship upgrades, necropolis upgrades or building necropolises) is an important decision." );
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
            buffer.Add( "It is important to build new Necropolises to strengthen your forces. A Major Necropolis is the most expensive but the most powerful, granting you a new Flagship. A Minor Necropolis will bolster an existing Flagship, adding to its strength. Defensive Necropolises are exceptional for defense or holding a planet, but do not strengthen a flagship.\n\nDeciding how to allocate your Essence (into flagship upgrades, necropolis upgrades or building necropolises) is an important decision." );
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

            buffer.Add( "What Elderlings Give You\n", ObjectiveColors.Header );
            buffer.Add( "Essence", ObjectiveColors.Reward ).Add( ": your primary resource for upgrading Necropolises and Flagships.\n" );
            buffer.Add( "Flagship Blueprints", ObjectiveColors.Reward ).Add( ": hack stronger Elderlings to unlock the most powerful flagship variants.\n\n" );

            buffer.Add( "How to Hunt Them\n", ObjectiveColors.Header );
            buffer.Add( "Hack an Elderling to track its location, or to attract Elderlings you have already tracked.\n\n" );

            buffer.Add( "Best Early Targets\n", ObjectiveColors.Header );
            GameEntityTypeData feebleData = GameEntityTypeDataTable.Instance.GetRowByName( "FeebleElderling" );
            if ( feebleData != null )
                buffer.AddShipIconInline( feebleData, localFaction );
            buffer.Add( "Feeble Elderlings", ObjectiveColors.Reward ).Add( " grant a lot of Essence." );
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
            buffer.Add( "Investing science into a few key upgrades early on goes a long way for the Necromancer. Aim to get at least 2 upgrades into each of the following:\n" );

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
                buffer.Add( "\nSkeleton (Ship tech)", ObjectiveColors.Reward );
            if ( wightUpgrades < 2 )
                buffer.Add( "\nWight (Ship tech)", ObjectiveColors.Reward );
            if ( towerUpgrades < 2 )
                buffer.Add( "\nTower Defense (Defense tech)", ObjectiveColors.Reward );

            buffer.Add( "\n\nEarly on your army is mostly basic units, and you have a lot of them. Strengthening them (especially early on) is a really science-efficient way to strengthen your fleet.\n\n" );
            buffer.Add( "Basic skeletons and wights are excellent frontline tanks, and are often raised directly in the middle of enemy forces. The increased durability will pay off immediately. Advanced wights will revert to basic wights, so upgrading the base form benefits your entire wight pool. Tower defenses are particularly valuable for holding against the Templar's powerful scaling attacks." );
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
            buffer.Add( "Elderlings tend to wander into territory that is not heavily defended by hostile forces. To have good hunting grounds for Elderlings, you need a number of planets near your own with relatively low hostile strength.\n\nConsider clearing out nearby hostile forces (such as AI guard posts and patrols) so that Elderlings have safe planets to wander onto, where you can then hunt them down for Essence." );
        }
    }
}
