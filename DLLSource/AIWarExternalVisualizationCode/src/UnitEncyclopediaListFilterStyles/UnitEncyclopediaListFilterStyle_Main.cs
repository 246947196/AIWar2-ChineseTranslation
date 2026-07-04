using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using Arcen.AIW2.External;
using UnityEngine;

namespace Arcen.AIW2.ExternalVisualization
{
    public class UnitEncyclopediaListFilterStyle_Main : IUnitEncyclopediaListFilterStyleImplementation
    {
        private static readonly FInt zeroPointSeven = FInt.FromParts( 0, 700 );
        private static readonly FInt zeroPointThree = FInt.FromParts( 0, 300 );
        private static readonly FInt zeroPointFour = FInt.FromParts( 0, 400 );
        private static readonly FInt zeroPointFour01 = FInt.FromParts( 0, 401 );
        private static readonly FInt zeroPointSix99 = FInt.FromParts( 0, 699 );
        private static readonly FInt zeroPointEight = FInt.FromParts( 0, 800 );
        private static readonly FInt zeroPointEight50 = FInt.FromParts( 0, 850 );

        public bool CalculateDoesUnitMatchFilter( GameEntityTypeData TypeData, UnitEncyclopediaListFilterStyle FilterStyle )
        {
            switch ( FilterStyle.InternalName )
            {
                case "None":
                    return true; //all match
                case "Cloaking":
                    if ( TypeData.HasAnyInternalCloakingAbility )
                        return true;
                    return false;
                case "Albedo_1_Low":
                    if ( TypeData.MarkStatsFor( 99 ).Albedo <= zeroPointFour )
                        return true;
                    return false;
                case "Albedo_2_Mid":
                    if ( TypeData.MarkStatsFor( 99 ).Albedo <= zeroPointSix99 &&
                        TypeData.MarkStatsFor( 99 ).Albedo >= zeroPointFour01 )
                        return true;
                    return false;
                case "Albedo_3_High":
                    if ( TypeData.MarkStatsFor( 99 ).Albedo >= zeroPointSeven )
                        return true;
                    return false;
                case "Albedo_4_VeryHigh":
                    if ( TypeData.MarkStatsFor( 99 ).Albedo >= zeroPointEight )
                        return true;
                    return false;
                case "Albedo_5_Extreme":
                    if ( TypeData.MarkStatsFor( 99 ).Albedo >= zeroPointEight50 )
                        return true;
                    return false;
                #region Energy
                case "Energy_1_1t9k":
                    if ( TypeData.EnergyUsage >= 1000 && TypeData.EnergyUsage < 10000 )
                        return true;
                    return false;
                case "Energy_2_10k":
                    if ( TypeData.EnergyUsage >= 10000 )
                        return true;
                    return false;
                case "Energy_3_100k":
                    if ( TypeData.EnergyUsage >= 100000 )
                        return true;
                    return false;
                case "Energy_4_200k":
                    if ( TypeData.EnergyUsage >= 200000 )
                        return true;
                    return false;
                case "Energy_5_500k":
                    if ( TypeData.EnergyUsage >= 500000 )
                        return true;
                    return false;
                #endregion
                #region Armor
                case "Armor_1_30mm":
                    if ( TypeData.Armor_mm <= 30 )
                        return true;
                    return false;
                case "Armor_2_31t50mm":
                    if ( TypeData.Armor_mm >= 31 && TypeData.EnergyUsage <= 50 )
                        return true;
                    return false;
                case "Armor_3_51t99mm":
                    if ( TypeData.Armor_mm >= 51 && TypeData.EnergyUsage <= 99 )
                        return true;
                    return false;
                case "Armor_4_100t150mm":
                    if ( TypeData.Armor_mm >= 100 && TypeData.EnergyUsage <= 150 )
                        return true;
                    return false;
                case "Armor_5_151t200mm":
                    if ( TypeData.Armor_mm >= 151 && TypeData.EnergyUsage <= 200 )
                        return true;
                    return false;
                case "Armor_6_201mm":
                    if ( TypeData.Armor_mm >= 201 )
                        return true;
                    return false;
                #endregion
                case "FuelArgon":
                    if ( TypeData.FuelUseType == ResourceType.FuelArgon )
                        return true;
                    return false;
                case "FuelXenon":
                    if ( TypeData.FuelUseType == ResourceType.FuelXenon )
                        return true;
                    return false;
                case "FuelRadon":
                    if ( TypeData.FuelUseType == ResourceType.FuelRadon )
                        return true;
                    return false;
                case "Weapon_X_Beam":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.NumberBeamsToFire > 0 )
                            return true;
                    }
                    return false;
                case "Weapon_X_BeamCoilbeam":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.IsCoilbeam )
                            return true;
                    }
                    return false;
                case "Weapon_X_BeamMultiBeam":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.NumberBeamsToFire > 1 )
                            return true;
                    }
                    return false;
                case "Weapon_X_BeamAllIntersecting":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.NumberBeamsToFire > 0 && System.HitsAllIntersectingTargets )
                            return true;
                    }
                    return false;
                case "Weapon_X_BeamPoint":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.NumberBeamsToFire > 0 && !System.HitsAllIntersectingTargets )
                            return true;
                    }
                    return false;
                case "Weapon_X_Melee":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.IsMelee )
                            return true;
                    }
                    return false;
                case "Weapon_X_Sniper":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.IsSniperRange )
                            return true;
                    }
                    return false;
                case "TractorBeam":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.BaseTractorCount > 0 )
                            return true;
                    }
                    return false;
                case "TractorReverse":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.BaseTractorCount > 0 && System.IsReverseTractorBeam )
                            return true;
                    }
                    return false;
                case "TachyonBeam":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.TachyonHitsAlbedoLessThan > FInt.Zero || System.TachyonHitsAlbedoMoreThan > FInt.Zero )
                            return true;
                    }
                    return false;
                case "GravityField":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.GravityHitsEngine_gxLessThan > 0 )
                            return true;
                    }
                    return false;
                case "AttractantField":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.MarkStatsFor( 99 ).AttractRangeForShotsAgainstAllies > 0 )
                            return true;
                    }
                    return false;
                case "Weapon_X_WeaponJammer":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.EnemyWeaponReloadSlowingSecondsArmor_mmLessThan > 0 )
                            return true;
                    }
                    return false;
                case "Weapon_X_FusionReaction":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.MarkStatsFor( 99 ).PercentDamageBypassesPersonalShields > FInt.Zero )
                            return true;
                    }
                    return false;
                case "Weapon_X_Knockback":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.KnockbackPerShotToShipsMass_tXLessThan > 0 )
                            return true;
                    }
                    return false;
                case "Weapon_X_DamageAmplification":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.DamageAmplification )
                            return true;
                    }
                    return false;
                case "Weapon_X_Vampirism":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.HealthChangePerDamageDealt > FInt.Zero )
                            return true;
                    }
                    return false;
                case "Weapon_X_SelfDamage":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.HealthChangePerDamageDealt < FInt.Zero )
                            return true;
                    }
                    return false;
                case "Weapon_X_Retaliatory":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.FiringTiming == FiringTiming.WhenParentEntityHit )
                            return true;
                    }
                    return false;
                case "Weapon_X_Mirror":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        bool isRetaliatory = System.FiringTiming == FiringTiming.WhenParentEntityHit;
                        bool isMirrorShot = isRetaliatory && System.ReturnsThisPercentageOfDamageWhenFiringRetaliatoryShot > FInt.Zero;
                        if ( isMirrorShot )
                            return true;
                    }
                    return false;
                case "Weapon_X_IonDamage":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.IonDamageToAlbedoLessThan > FInt.Zero )
                            return true;
                    }
                    return false;
                case "Weapon_X_ShieldOverdrive":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OverdrivesShields )
                            return true;
                    }
                    return false;
                case "Weapon_X_BurstFire":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.MarkStatsFor( 99 ).AltSecondsPerSalvo > 0 )
                            return true;
                    }
                    return false;
                case "Weapon_X_InflictsStateChange":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.InflictsStateOfMatterOnTargetForSeconds > 0 && System.StateOfMatterForTargetToBecome != null )
                            return true;
                    }
                    return false;
                case "Weapon_X_DirectAOE":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.MarkStatsFor( 99 ).ShotAreaOfEffect > 0 && System.ShotsDetonateImmediately )
                            return true;
                    }
                    return false;
                case "Weapon_X_AOEAtTarget":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.MarkStatsFor( 99 ).ShotAreaOfEffect > 0 && !System.ShotsDetonateImmediately )
                            return true;
                    }
                    return false;
                case "Weapon_X_ChainLightning":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.BeamChainsOutToTargetsXTimes > 0 && System.BeamChainsOutToTargetsXRange > 0 )
                            return true;
                    }
                    return false;
                case "Weapon_X_MultiShot":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.BeamLengthMultiplier <= FInt.Zero &&
                            System.MarkStatsFor( 99 ).ShotsPerSalvo > 1 )
                            return true;
                    }
                    return false;
                case "Weapon_X_EngineStun":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.EngineStunToEngine_gxLessThan > 0 )
                            return true;
                    }
                    return false;
                case "Weapon_X_Paralysis":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.ParalysisToShipsMass_tXLessThan > 0 )
                            return true;
                    }
                    return false;
                case "Weapon_X_FFBypass":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.FiresThroughEnemyShields )
                            return true;
                    }
                    return false;
                case "Weapon_X_TractoredBonus":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.MarkStatsFor( 99 ).DamageMultiplierToTractoredUnits > FInt.Zero )
                            return true;
                    }
                    return false;
                case "Weapon_X_UnderFFBonus":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.DamageModifierWhileUnderForcefield != FInt.FromParts( 0, 500 ) )
                            return true;
                    }
                    return false;
                case "Weapon_X_WeaponPoints":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.AdditionalDamageModifierPerWeaponPoint != FInt.Zero )
                            return true;
                    }
                    return false;
                case "Weapon_X_Devour":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.CanDevour )
                            return true;
                    }
                    return false;
                case "Weapon_X_DroneGun":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.ShotTypeData != null && System.ShotTypeData.Category == GameEntityCategory.Ship )
                            return true;
                    }
                    return false;
                case "Weapon_X_Infestation":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.UnitToSpawnOnInfestation.Length > 0 )
                            return true;
                    }
                    return false;
                case "TargetLimiting_OnlyStatic":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OnlyTargetsStaticUnits )
                            return true;
                    }
                    return false;
                case "TargetLimiting_OnlyMobile":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OnlyTargetsMobileUnits )
                            return true;
                    }
                    return false;
                case "TargetLimiting_OnlyStrikecraftAndFrigates":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OnlyTargetsStrikecraftAndFrigates )
                            return true;
                    }
                    return false;
                #region Weapon Attack Bonuses
                case "Weapon_Z_AttackBonus_A_Any":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier <= FInt.One )
                                continue;
                            return true;
                        }
                    }
                    return false;
                case "Weapon_Z_AttackBonus_B_AlbedoLow":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier <= FInt.One )
                                continue;
                            switch ( Modifer.ComparisonType )
                            {
                                case DamageModifierComparisonType.AtLeast:
                                case DamageModifierComparisonType.GreaterThan:
                                    continue; //cannot be above
                            }
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.Albedo:
                                    return true;
                            }
                        }
                    }
                    return false;
                case "Weapon_Z_AttackBonus_B_AlbedoHigh":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier <= FInt.One )
                                continue;
                            switch ( Modifer.ComparisonType )
                            {
                                case DamageModifierComparisonType.AtLeast:
                                case DamageModifierComparisonType.GreaterThan:
                                    break; //only count above
                                default:
                                    continue;
                            }
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.Albedo:
                                    return true;
                            }
                        }
                    }
                    return false;
                case "Weapon_Z_AttackBonus_B_MassLow":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier <= FInt.One )
                                continue;
                            switch ( Modifer.ComparisonType )
                            {
                                case DamageModifierComparisonType.AtLeast:
                                case DamageModifierComparisonType.GreaterThan:
                                    continue; //cannot be above
                            }
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.Mass_tX:
                                    return true;
                            }
                        }
                    }
                    return false;
                case "Weapon_Z_AttackBonus_B_MassHigh":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier <= FInt.One )
                                continue;
                            switch ( Modifer.ComparisonType )
                            {
                                case DamageModifierComparisonType.AtLeast:
                                case DamageModifierComparisonType.GreaterThan:
                                    break; //only count above
                                default:
                                    continue;
                            }
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.Mass_tX:
                                    return true;
                            }
                        }
                    }
                    return false;
                case "Weapon_Z_AttackBonus_B_ArmorLow":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier <= FInt.One )
                                continue;
                            switch ( Modifer.ComparisonType )
                            {
                                case DamageModifierComparisonType.AtLeast:
                                case DamageModifierComparisonType.GreaterThan:
                                    continue; //cannot be above
                            }
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.Armor_mm:
                                    return true;
                            }
                        }
                    }
                    return false;
                case "Weapon_Z_AttackBonus_B_ArmorHigh":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier <= FInt.One )
                                continue;
                            switch ( Modifer.ComparisonType )
                            {
                                case DamageModifierComparisonType.AtLeast:
                                case DamageModifierComparisonType.GreaterThan:
                                    break; //only count above
                                default:
                                    continue;
                            }
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.Armor_mm:
                                    return true;
                            }
                        }
                    }
                    return false;
                case "Weapon_Z_AttackBonus_B_EnergyLow":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier <= FInt.One )
                                continue;
                            switch ( Modifer.ComparisonType )
                            {
                                case DamageModifierComparisonType.AtLeast:
                                case DamageModifierComparisonType.GreaterThan:
                                    continue; //cannot be above
                            }
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.EnergyUsage:
                                    return true;
                            }
                        }
                    }
                    return false;
                case "Weapon_Z_AttackBonus_B_EnergyHigh":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier <= FInt.One )
                                continue;
                            switch ( Modifer.ComparisonType )
                            {
                                case DamageModifierComparisonType.AtLeast:
                                case DamageModifierComparisonType.GreaterThan:
                                    break; //only count above
                                default:
                                    continue;
                            }
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.EnergyUsage:
                                    return true;
                            }
                        }
                    }
                    return false;
                case "Weapon_Z_AttackBonus_B_EnginesLow":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier <= FInt.One )
                                continue;
                            switch ( Modifer.ComparisonType )
                            {
                                case DamageModifierComparisonType.AtLeast:
                                case DamageModifierComparisonType.GreaterThan:
                                    continue; //cannot be above
                            }
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.Engine_gx:
                                    return true;
                            }
                        }
                    }
                    return false;
                case "Weapon_Z_AttackBonus_B_EnginesHigh":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier <= FInt.One )
                                continue;
                            switch ( Modifer.ComparisonType )
                            {
                                case DamageModifierComparisonType.AtLeast:
                                case DamageModifierComparisonType.GreaterThan:
                                    break; //only count above
                                default:
                                    continue;
                            }
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.Engine_gx:
                                    return true;
                            }
                        }
                    }
                    return false;
                case "Weapon_Z_AttackBonus_B_TargetHull":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier <= FInt.One )
                                continue;
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.TargetHullPercentageMissing:
                                case DamageModifierBasedOn.MaxHull:
                                case DamageModifierBasedOn.CurrentHullPercentage:
                                    return true;
                            }
                        }
                    }
                    return false;
                case "Weapon_Z_AttackBonus_B_MyHull":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier <= FInt.One )
                                continue;
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.MyHullPercentageMissing:
                                    return true;
                            }
                        }
                    }
                    return false;
                case "Weapon_Z_AttackBonus_B_TargetShields":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier <= FInt.One )
                                continue;
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.TargetShieldPercentageMissing:
                                case DamageModifierBasedOn.MaxPersonalShield:
                                case DamageModifierBasedOn.CurrentPersonalShieldPercentage:
                                case DamageModifierBasedOn.MaxBubbleForcefield:
                                    return true;
                            }
                        }
                    }
                    return false;
                case "Weapon_Z_AttackBonus_B_MyShields":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier <= FInt.One )
                                continue;
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.MyShieldPercentageMissing:
                                    return true;
                            }
                        }
                    }
                    return false;

                case "Weapon_Z_AttackBonus_B_TargetTimeAtPlanet":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier <= FInt.One )
                                continue;
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.TargetTimeAtPlanet:
                                    return true;
                            }
                        }
                    }
                    return false;
                case "Weapon_Z_AttackBonus_B_MyTimeAtPlanet":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier <= FInt.One )
                                continue;
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.MyTimeAtPlanet:
                                    return true;
                            }
                        }
                    }
                    return false;
                case "Weapon_Z_AttackBonus_B_Distance":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier <= FInt.One )
                                continue;
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.AttackDistance:
                                    return true;
                            }
                        }
                    }
                    return false;
                case "Weapon_Z_AttackBonus_B_FactionNetEnergy":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier <= FInt.One )
                                continue;
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.FactionNetEnergy:
                                    return true;
                            }
                        }
                    }
                    return false;
                case "Weapon_Z_AttackPenalty_A_Any":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier >= FInt.One )
                                continue;
                            return true;
                        }
                    }
                    return false;
                case "Weapon_Z_AttackPenalty_B_Albedo":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier >= FInt.One )
                                continue;
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.Albedo:
                                    return true;
                            }
                        }
                    }
                    return false;
                case "Weapon_Z_AttackPenalty_B_Mass":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier >= FInt.One )
                                continue;
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.Mass_tX:
                                    return true;
                            }
                        }
                    }
                    return false;
                case "Weapon_Z_AttackPenalty_B_Distance":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( System.OutgoingDamageModifiers_FullList.Count <= 0 )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DamageModifier Modifer in System.OutgoingDamageModifiers_FullList )
                        {
                            FInt Multiplier = Modifer.MultiplierForMark[markData.MarkLevel.Ordinal];
                            if ( !Modifer.IsForOutgoingDamage || Multiplier >= FInt.One )
                                continue;
                            switch ( Modifer.BasedOn )
                            {
                                case DamageModifierBasedOn.AttackDistance:
                                    return true;
                            }
                        }
                    }
                    return false;
                #endregion
                #region Death Effects
                case "DeathEffectZombify":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( !System.HasAnyDeathEffectOffenses )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DeathEffectType DeathEffectToCheck in DeathEffectTypeTable.Instance.Rows )
                        {
                            if ( !DeathEffectToCheck.IsSomeKindOfNormalZombification )
                                continue;
                            if ( markData.DeathEffectDamagePerShotByType[DeathEffectToCheck] > 0 )
                                return true;
                        }
                    }
                    return false;
                case "DeathEffectZombifyAlly":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( !System.HasAnyDeathEffectOffenses )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DeathEffectType DeathEffectToCheck in DeathEffectTypeTable.Instance.Rows )
                        {
                            if ( DeathEffectToCheck.InternalName != "Zombification" )
                                continue;
                            if ( markData.DeathEffectDamagePerShotByType[DeathEffectToCheck] > 0 )
                                return true;
                        }
                    }
                    return false;
                case "DeathEffectZombifyHostileToAll":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( !System.HasAnyDeathEffectOffenses )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DeathEffectType DeathEffectToCheck in DeathEffectTypeTable.Instance.Rows )
                        {
                            if ( DeathEffectToCheck.InternalName != "Zombification_HostileToAll" )
                                continue;
                            if ( markData.DeathEffectDamagePerShotByType[DeathEffectToCheck] > 0 )
                                return true;
                        }
                    }
                    return false;
                case "DeathEffectNanocaustation":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( !System.HasAnyDeathEffectOffenses )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DeathEffectType DeathEffectToCheck in DeathEffectTypeTable.Instance.Rows )
                        {
                            if ( DeathEffectToCheck.InternalName != "Nanocaustation" )
                                continue;
                            if ( markData.DeathEffectDamagePerShotByType[DeathEffectToCheck] > 0 )
                                return true;
                        }
                    }
                    return false;
                case "DeathEffectNecromancy":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( !System.HasAnyDeathEffectOffenses )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DeathEffectType DeathEffectToCheck in DeathEffectTypeTable.Instance.Rows )
                        {
                            if ( DeathEffectToCheck.InternalName != "Necromancy" )
                                continue;
                            if ( markData.DeathEffectDamagePerShotByType[DeathEffectToCheck] > 0 )
                                return true;
                        }
                    }
                    return false;
                case "DeathEffectMetabolizeAny":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( !System.HasAnyDeathEffectOffenses )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DeathEffectType DeathEffectToCheck in DeathEffectTypeTable.Instance.Rows )
                        {
                            if ( DeathEffectToCheck.InternalName != "Metabolization" && DeathEffectToCheck.InternalName != "GreaterMetabolization" )
                                continue;
                            if ( markData.DeathEffectDamagePerShotByType[DeathEffectToCheck] > 0 )
                                return true;
                        }
                    }
                    return false;
                case "DeathEffectMetabolizeGreater":
                    foreach ( EntitySystemTypeData System in TypeData.SystemTypes )
                    {
                        if ( System.MaxMarkLevelToFunction == 0 )
                            continue;
                        if ( !System.HasAnyDeathEffectOffenses )
                            continue;
                        EntitySystemTypeData.MarkLevelStats markData = System.MarkStatsFor( 99 );
                        foreach ( DeathEffectType DeathEffectToCheck in DeathEffectTypeTable.Instance.Rows )
                        {
                            if ( DeathEffectToCheck.InternalName != "GreaterMetabolization" )
                                continue;
                            if ( markData.DeathEffectDamagePerShotByType[DeathEffectToCheck] > 0 )
                                return true;
                        }
                    }
                    return false;
                #endregion
                case "CripplesInsteadOfDeath":
                    if ( TypeData.IsCrippledInsteadOfDying )
                        return true;
                    return false;
                case "DiesToRemains":
                    if ( TypeData.DiesToRemains )
                        return true;
                    return false;
                case "DiesWhenParentNotPresent":
                    if ( TypeData.SelfAttritionsXPercentPerSecondIfParentShipNotOnPlanet > 0 )
                        return true;
                    return false;
                case "CannotBeRepaired":
                    if ( TypeData.ImmuneToRepairs )
                        return true;
                    return false;
                case "Regenerating":
                    if ( TypeData.SecondsToFullyRegenerateHull > FInt.Zero )
                        return true;
                    return false;
                case "Transforming":
                    if ( TypeData.TransformWhenOutnumbered != null || TypeData.TransformWhenOutnumbering != null )
                        return true;
                    return false;
                case "DroneSpawner":
                    if ( TypeData.FleetDesignTemplateIUseForDrones != null &&
                        (TypeData.FleetDesignTemplateIUseForDrones.Strikecraft.DrawBag.GetHasItems() ||
                        TypeData.FleetDesignTemplateIUseForDrones.Frigates.DrawBag.GetHasItems()) )
                        return true;
                    return false;
                case "Res_Metal":
                    if ( TypeData.MarkStatsFor( 99 ).GetResourceProductionBeforeAnyBonuses( ResourceType.Metal ) != FInt.Zero )
                        return true;
                    return false;
                case "Res_Energy":
                    if ( TypeData.MarkStatsFor( 99 ).GetResourceProductionBeforeAnyBonuses( ResourceType.Energy ) != FInt.Zero )
                        return true;
                    return false;
                case "Res_FuelArgon":
                    if ( TypeData.MarkStatsFor( 99 ).GetResourceProductionBeforeAnyBonuses( ResourceType.FuelArgon ) != FInt.Zero )
                        return true;
                    return false;
                case "Res_FuelRadon":
                    if ( TypeData.MarkStatsFor( 99 ).GetResourceProductionBeforeAnyBonuses( ResourceType.FuelRadon ) != FInt.Zero )
                        return true;
                    return false;
                case "Res_FuelXenon":
                    if ( TypeData.MarkStatsFor( 99 ).GetResourceProductionBeforeAnyBonuses( ResourceType.FuelXenon ) != FInt.Zero )
                        return true;
                    return false;
                case "Res_Multiplier":
                    for ( ResourceType resource = ResourceType.None + 1; resource < ResourceType.Length; resource++ )
                    {
                        FInt resourceMultiplier = TypeData.MarkStatsFor( 99 ).GetResourceProductionMultiplier( resource );
                        if ( resourceMultiplier == FInt.Zero )
                            continue;
                        return true;
                    }
                    if ( TypeData.TimeRequiredToBeHereAndNonCrippledToBoostMetalOrEnergyProduction > 0 )
                        return true;
                    return false;
                case "Hackable":
                    if ( TypeData.GetIsEligibleForAnyHack() )
                        return true;
                    return false;
                case "WormholeAcceleration":
                    if ( TypeData.SpeedMultiplierFirst5SecondsOnPlanet > FInt.One )
                        return true;
                    return false;
                case "VonNeumann":
                    if ( TypeData.BuildPointsPerDamageDealt > FInt.Zero ||
                        TypeData.BuildPointsPerDamageTaken > FInt.Zero )
                        return true;
                    return false;
                case "BlackHole":
                    if ( TypeData.AddsBlackHoleEffectForEntitiesWithEngine_gxLessThan > 0 )
                        return true;
                    if ( TypeData.AddsBlackHoleEffectForAllEntitiesPeriod )
                        return true;
                    return false;
                case "AggroInvisible":
                    if ( TypeData.CannotTargetOrAlertAIReinforcementSpots )
                        return true;
                    return false;
                case "AIAccelerator":
                    if ( TypeData.AIReinforcementMultiplier > FInt.One )
                        return true;
                    return false;
                case "PeriodicSpawns":
                    if ( TypeData.PeriodicSpawn_InitialDelay > 0 )
                        return true;
                    return false;
                case "GuardFreer":
                    if ( TypeData.FreesGuardsWhenLocalForceRatioIsWorseThan > FInt.Zero )
                        return true;
                    return false;
                case "Recon":
                    if ( TypeData.WatchPlanetsAtXHops > 0 )
                        return true;
                    return false;
                case "NorrisEffect":
                    if ( TypeData.PushesEnemyShields )
                        return true;
                    return false;
                case "Orbit_GravWell":
                    if ( TypeData.OrbitsGravityWellCenterAtItsCurrentRadius )
                        return true;
                    return false;
                case "Orbit_Ancestor":
                    if ( TypeData.OrbitsParentAtRange > 0 )
                        return true;
                    return false;
                case "Orbit_Flagship":
                    if ( TypeData.OrbitsFlagshipAtRange > 0 )
                        return true;
                    return false;
                case "Forcefield":
                    if ( TypeData.MarkStatsFor( 99 ).ShieldRadius > 0 )
                        return true;
                    return false;
                case "Electrotoxic":
                    if ( TypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack > FInt.Zero )
                        return true;
                    return false;
                case "PlanetaryAttackMultiplier":
                    if ( TypeData.MarkStatsFor( 99 ).AlliedAttackMultiplier != FInt.One ||
                        TypeData.MarkStatsFor( 99 ).HostileAttackMultiplier != FInt.One )
                        return true;
                    return false;
                case "PlanetarySpeedMultiplier":
                    if ( TypeData.MarkStatsFor( 99 ).AlliedSpeedMultiplier != FInt.One ||
                        TypeData.MarkStatsFor( 99 ).AlliedSpeedFlatBonus != 0 ||
                        TypeData.MarkStatsFor( 99 ).HostileSpeedMultiplier != FInt.One ||
                        TypeData.MarkStatsFor( 99 ).HostileSpeedFlatBonus != 0 )
                        return true;
                    return false;
                case "Attritioner":
                    if ( TypeData.MarkStatsFor( 99 ).AttritionDamagePreFleetModifiers > 0 )
                        return true;
                    return false;
                case "Hardened":
                    if ( TypeData.DamageTakenCannotBeMoreThanThisPercentageOfMaxHealth > FInt.Zero &&
                        TypeData.DamageTakenCannotBeMoreThanThisPercentageOfMaxHealth < FInt.One )
                        return true;
                    return false;
                case "Ceasefire":
                    if ( TypeData.CreatesCeasefireOnPlanet || TypeData.BlocksCeasefireOnPlanet )
                        return true;
                    return false;
                case "Harmonic":
                    if ( TypeData.AmountAddedToDamagePerShipOfThisTypeOnPlanet > 0 )
                        return true;
                    return false;
                case "FleetWideBonuses":
                    if ( TypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess > 0 )
                        return true;
                    return false;
                case "DeathSpawn":
                    if ( !EntityTypeDrawingBag.IsNullOrInvalid( TypeData.SpawnOnDeath_EntityTypeDrawingBag.Value ) )
                        return true;
                    return false;
                case "Invulnerability":
                    if ( TypeData.ExternalInvulnerabilityUnitRequiredCount > 0 )
                        return true;
                    return false;
                case "IncomingDamageModifiers":
                    if ( TypeData.IncomingDamageModifiers_FullList.Count > 0 )
                        return true;
                    return false;
                case "AIPOnDeath":
                    if ( TypeData.AIPOnDeath != 0 || TypeData.AIPOnDeathWhenNoneLeft != 0 )
                        return true;
                    return false;
                case "AIPOnClaim":
                    if ( TypeData.AIPToClaim != 0 )
                        return true;
                    return false;
                case "BonusOnDeath":
                    if ( TypeData.AIPOnDeath < 0 || 
                        TypeData.MetalToGrantOnDeath > 0 || 
                        TypeData.ScienceToGrantOnDeath > 0 ||
                        TypeData.HackingToGrantOnDeath > 0 )
                        return true;
                    return false;
                case "Progenitor":
                    if ( TypeData.BuildPointsPerSecond > 0 )
                        return true;
                    return false;
                case "Elite":
                    if ( TypeData.IsElite )
                        return true;
                    return false;
                case "AIWarpPoint":
                    if ( TypeData.ProvidesAIWarpEntryPoint || TypeData.IsWarpBeacon )
                        return true;
                    return false;
                case "WormholeBlocked":
                    if ( TypeData.IsMobile && TypeData.FleetMembershipStyle == FleetMembershipStyle.Planetary )
                        return true;
                    return false;
                case "Attritions":
                    if ( TypeData.AlwaysSelfAttritions )
                        return true;
                    return false;
                case "FFImmune":
                    if ( TypeData.ImmuneToProtectionByForcefields )
                        return true;
                    return false;
                case "FFHarmonics":
                    if ( TypeData.CanPassThroughEnemyForcefields )
                        return true;
                    return false;
                case "Phasing":
                    if ( TypeData.PhasesToOtherAlternativeStateOfMatterAfterSeconds > 0 ||
                         TypeData.StateOfMatterToBecomeOnWormholeExit != null ||
                         TypeData.ForcedToBeThisStateOfMatterWhenOnFormerOrCurrentAIHomeworldOrBastionWorldsUnlessEnemyKingPresent != null )
                        return true;
                    return false;
                default:
                    throw new Exception( "Typo! UnitEncyclopediaListFilterStyle '" + FilterStyle.InternalName + "' has not been properly set up." );
            }
        }

    }
}