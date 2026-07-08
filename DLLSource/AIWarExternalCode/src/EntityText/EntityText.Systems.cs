
using System;
using Arcen.Universal;
using Arcen.AIW2.Core;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public struct SystemText
    {
        public readonly EntitySystem System;
        public readonly EntitySystemTypeData Type;
        public readonly EntitySystemTypeData.MarkLevelStats ForMark;
        public readonly GameEntity_Squad Squad;
        public EntityText.Config Config;

        private bool IsRetaliatory => Type.FiringTiming == FiringTiming.WhenParentEntityHit;
        private bool IsMirrorShot => IsRetaliatory && Type.ReturnsThisPercentageOfDamageWhenFiringRetaliatoryShot > FInt.Zero;
        private bool IsOnDeath => Type.OnlyFiresOnDeath;
        private bool IsDroneGun => (Type.ShotTypeData?.Category??GameEntityCategory.None) == GameEntityCategory.Ship;
        private bool IsReloading => System.TimeUntilNextShot > FInt.Zero;

        public SystemText(EntitySystem system, EntityText.Config config)
        {
            System = system;
            Type = system.TypeData;
            Squad = system.ParentEntity;
            ForMark = system.DataForMark;
            Config = config;
            
            if (Squad.IsFakeEntity)
            {
                Config.WeaponDetail = WeaponActivityDetail.Nothing;
                Config.ShowAbortCode = false;
                Config.ShowTargetInfo = false;
            }
        }
        
        public void Write(EntityTextWriter writer)
        {
            //EntityText.Config config = this.Config;
            var buffer = writer.Buffer;
            
            var detailLevel = Config.Detail;
            bool useIcons = Config.UseIcons;
            bool useText = Config.UseText;
            //bool useShipIconAndName = setup.UseShipIconAndName;
            //if (Squad.IsFakeEntity)
            //{
            //    WeaponActivityDetails = WeaponActivityDetail.Nothing;
            //    ShowAbortCode = false;
            //    ShowTargetInfo = false;
            //}
            
            // todo: purge these
            
            #region Helper Methods
            void WriteSystemStateOfMatterSuffix( ArcenCharacterBufferBase buf, EntitySystemTypeData systemData, GameEntity_Squad relatedSquadOrNull )
            {
                if ( systemData.CareAboutStateOfMatterToBeEnabled )
                {
                    buffer.Add( "必须是 " ).Add( systemData.MustBeThisStateOfMatterToBeEnabled.DisplayName ).Add( " 物质态才能运作" ).EndStatement(EndStatementStyle.Normal);
                    if ( relatedSquadOrNull != null && !systemData.IsInvisibleInTooltipsIfNotAMatchByStateOfMatter )
                    {
                        if ( relatedSquadOrNull.CurrentStateOfMatter != systemData.MustBeThisStateOfMatterToBeEnabled )
                            buffer.Add( "（已禁用）" );
                        else
                            buffer.Add( "（已启用）" );
                    }
                }
            }
            void WriteMinMaxOrVariant( ArcenCharacterBufferBase buf, string AnyName, string OnlyName, bool IsBrief, string Color, FInt min, FInt max )
            {
                if ( min <= FInt.Zero && max <= FInt.Zero )
                {
                    buf.Add( AnyName );
                    buf.Add( "" ).EndStatement(EndStatementStyle.Normal);
                    return;
                } else
                {
                    buf.Add( OnlyName );
                    if ( min > FInt.Zero )
                    {
                        if ( IsBrief )
                            buf.Add( " > " );
                        else
                            buf.Add( " 大于 " );
                        buf.StartColor( Color );
                        buf.AddFixedDecimal( min.ToFloatNonSim(), 2 );
                        buf.EndColor();

                        if ( max > FInt.Zero )
                        {
                            if ( IsBrief )
                                buf.Add( " 且 < " );
                            else
                                buf.Add( " 且小于 " );
                            buf.StartColor( Color );
                            buf.AddFixedDecimal( max.ToFloatNonSim(), 2 );
                        }
                        buf.Add( "</color>" ).EndStatement(EndStatementStyle.Normal);
                    } else //no min
                    {
                        if ( IsBrief )
                            buf.Add( " < " );
                        else
                            buf.Add( " 小于 " );
                        buf.StartColor( Color );
                        buf.AddFixedDecimal( max.ToFloatNonSim(), 2 );
                        buf.Add( "</color>" ).EndStatement(EndStatementStyle.Normal);
                    }
                }
            }
            #endregion
            
            Faction facOrNull = Squad.GetFactionOrNull_Safe();
            byte effectiveMarkLevel = Squad.CurrentMarkLevel;
            Planet relatedPlanet = Squad.Planet;
            FleetMembership relatedMembershipOrNull = Squad.FleetMembership;
            Color damageColor = ColorMath.LightRed;
            
            // non-weapon systems use the same style as attributes
            var Line_Style = TextStyle.Attr_Line;
            var Label_Style = TextStyle.Attr_Label;
                    
            int debugStage = 0;
            try
            {
                #region CustomSystem
                if ( System.CustomSystem != null)
                {
                    debugStage = 100;
                    (System.CustomSystem as ExternalData_CustomSystem).AppendText(buffer);
                }
                #endregion
                else
                #region Weapon
                if ( Type.Category == EntitySystemCategory.Weapon )
                {
                    debugStage = 200;
                    
                    buffer.BeginStatement(TextStyle.System_Line);
                    
                    #region Line1

                    debugStage = 201;
                    
                    int display_shotdamage = ForMark.DamagePerShot;
                    if (System != null)
                        display_shotdamage = System.GetShotDamage();
                    
                    debugStage = 202;
                    
                    //if (display_shotdamage == 0)
                        //LOG.Msg("{0} on {1} display_shotdamage is zero?", System.TypeData.InternalName_Original, this.Squad.TypeData.InternalName);
                    
                        debugStage = 203;
                        
                    int display_shotcorrosiondamage = ForMark.CorrosionDamage;
                    if (Type.AllDamageIsCorrosive)
                        display_shotcorrosiondamage += display_shotdamage;
                    if (System != null)
                        display_shotcorrosiondamage = System.GetShotDamage_OfCorrosion();
                    
                    debugStage = 204;
                    
                    if (Type.AllDamageIsCorrosive)
                        display_shotdamage = 0;
                    
                    debugStage = 205;
                    
                    int display_salvodps_min = ForMark.DamagePerSecond.GetNearestIntPreferringHigher();
                    int display_salvodps_max = ForMark.DamagePerSecond.GetNearestIntPreferringHigher();
                    if (System != null)
                    {
                        display_salvodps_min = System.GetSalvoDps_Min().GetNearestIntPreferringHigher();
                        display_salvodps_max = System.GetSalvoDps_Max().GetNearestIntPreferringHigher();
                    }
                    
                    debugStage = 206;
                    
                    
                    //if (isDroneGun)
                    //{
                    //    debugStage = 211;
                    //    buffer.AddVarReplace(TextVarMap.Tooltip_DroneGun_Weapon_Line_Format, AppendVar_Weapon);
                    //}
                    //else
                    {
                        debugStage = 212;
                        TextVarMap.Tooltip_Weapon_Line_Format.AddVarReplace(FormatArgs.Alloc(buffer, AppendVar_Weapon, evalCond:EvalCondition));
                    }
                    
                    debugStage = 213;
                    
                    #region ShowWeaponActivityDetails
                    /*
                    if ( Config.WeaponDetail > WeaponActivityDetail.Nothing &&
                         !Squad.IsFakeEntity && 
                         !Config.ForMultipleShips && 
                         Squad.GetMetalToClaimRemaining() == 0 &&
                         !Squad.GetIsCrippled() &&
                         !Squad.GetIsRemains() &&
                         Squad.Planet.GetDoHumansHaveVision() &&
                         (Squad.GetIsPlayerUnit() || Config.ShowDebugInfo) )
                    {
                        debugStage = 214;
                        buffer.AddVarReplace(TextVarMap.Weapon_Activity_Format, AppendVar_Activity);
                    }
                    
                    debugStage = 224;
                    */
                    #endregion
                    
                    #region unused
#if false
                    
                    string display_name = Type.DisplayName;//Type.InternalName_Original;
                    if ( Type.CustomType != null )
                        display_name = Type.CustomType.DisplayName;

                    int display_shotdamage = ForMark.DamagePerShot;
                    if (System != null)
                        display_shotdamage = System.GetShotDamage();
                    
                    //if (display_shotdamage == 0)
                        //LOG.Msg("{0} on {1} display_shotdamage is zero?", System.TypeData.InternalName_Original, this.Squad.TypeData.InternalName);
                    
                    int display_shotcorrosiondamage = ForMark.CorrosionDamage;
                    if (Type.AllDamageIsCorrosive)
                        display_shotcorrosiondamage += display_shotdamage;
                    if (System != null)
                        display_shotcorrosiondamage = System.GetShotDamage_OfCorrosion();
                    
                    if (Type.AllDamageIsCorrosive)
                        display_shotdamage = 0;

                    int display_salvodps_min = ForMark.DamagePerSecond.GetNearestIntPreferringHigher();
                    int display_salvodps_max = ForMark.DamagePerSecond.GetNearestIntPreferringHigher();
                    if (System != null)
                    {
                        display_salvodps_min = System.GetSalvoDps_Min().GetNearestIntPreferringHigher();
                        display_salvodps_max = System.GetSalvoDps_Max().GetNearestIntPreferringHigher();
                    }
                    
                    debugStage = 183;
                    bool isRetaliatory = Type.FiringTiming == FiringTiming.WhenParentEntityHit;
                    bool isMirrorShot = isRetaliatory && Type.ReturnsThisPercentageOfDamageWhenFiringRetaliatoryShot > FInt.Zero;
                    bool isOnDeath = Type.OnlyFiresOnDeath;

                    //buffer
                    //    .Open(system_line1_label).Add(TextTerm.System_Weapon).Close(system_line1_label)
                    //    .Add(" [").Add(System.TypeData.InternalName_Original).Add("]: ")
                    //    ;

                    buffer.Add( display_name, TextStyle.System_Label );
                    buffer.Add( ": " );

                    debugStage = 185;

                    if ( Type.ShotTypeData != null && Type.ShotTypeData.Category == GameEntityCategory.Ship )
                    {
                        GameEntityTypeData spawnType = Type.ShotTypeData;
                        byte subMark = effectiveMarkLevel;
                        if ( subMark > spawnType.MaxMarkLevel )
                            subMark = spawnType.MaxMarkLevel;
                        if ( subMark < spawnType.BaseMark.MarkLevel.Ordinal )
                            subMark = spawnType.BaseMark.MarkLevel.Ordinal;
                        Faction toDisplayFor = facOrNull;
                        if ( toDisplayFor == null )
                            toDisplayFor = spawnType.EncyclopediaOnly_LastFactionForColor.Display;
                        
                        buffer.AddShipIconInline( spawnType, toDisplayFor );
                        if (useShipIconAndName)
                            buffer.Add( spawnType.GetDisplayName() );
                    }
                    else 
                    if ( Type.CanDevour )
                    {
                        buffer.WrapDamage( Type.DevourFunctionName, false, false );
                    }
                    else 
                    if ( Type.IonDamageToAlbedoLessThan > FInt.Zero )
                    {
                        buffer.StartExoticDamageWrapper( useIcons ).Add( Type.IonPercentagePerMarkLevelLower * FInt.OneHundred ).Add( "%" );
                        if ( useText )
                            buffer.Add( "Ion Exotic Damage" );
                        buffer.EndColor();
                    } 
                    else 
                    if ( isMirrorShot )
                    {
                        buffer.StartExoticDamageWrapper( useIcons ).Add( Type.ReturnsThisPercentageOfDamageWhenFiringRetaliatoryShot * FInt.OneHundred ).Add( "%" );
                        if ( useText )
                            buffer.Add( " Mirrored Exotic Damage" );
                        buffer.EndColor();
                    } 
                    else 
                    if ( Type.OverdrivesShields )
                    {
                        buffer.WrapDamage( "100%", useIcons, useText ).Add( " Target Shield" );
                    } 
                    else
                    {
                        if ( isRetaliatory )
                            buffer.Add( "Retaliatory " );
                        
                        bool wrotePotentialIcon = false;
                        
                        if ( display_shotdamage > 0 )
                        {
                            if ( isRetaliatory )
                            {
                                //if ( detailLevel < TooltipDetail.Full )
                                    //buffer.WrapExoticDamageTruncated( display_shotdamage, useIcons, useText );
                                //else
                                    buffer.WrapExoticDamageMoreReadable( display_shotdamage, useIcons, useText );
                            } 
                            else
                            {
                                //if ( detailLevel < TooltipDetail.Full )
                                    //buffer.WrapDamageTruncated( display_shotdamage, useIcons, useText );
                                //else
                                    buffer.WrapDamageMoreReadable( display_shotdamage, useIcons, useText );
                            }
                            wrotePotentialIcon = true;
                        }
                        
                        if ( display_shotcorrosiondamage > 0 )
                        {
                            if ( display_shotdamage > 0 )
                            {
                                if ( detailLevel < TooltipDetail.Full )
                                    buffer.Add( " + " );
                                else
                                    buffer.Add( " and " );
                            }
                            
                            if ( isRetaliatory )
                            {
                                //if ( detailLevel < TooltipDetail.Full )
                                    //buffer.WrapExoticDamageTruncated( display_shotcorrosiondamage, useIcons && !wrotePotentialIcon, false );
                                //else
                                    buffer.WrapDamageMoreReadable( display_shotcorrosiondamage, useIcons && !wrotePotentialIcon, false );
                            } 
                            else
                            {
                                //if ( detailLevel < TooltipDetail.Full )
                                    //buffer.WrapCorrosiveDamageTruncated( display_shotcorrosiondamage, useIcons && !wrotePotentialIcon, false );
                                //else
                                    buffer.WrapCorrosiveDamageMoreReadable( display_shotcorrosiondamage, useIcons && !wrotePotentialIcon, false );
                            }
                            
                            if ( useText )
                                buffer.Add( " Corrosive Damage" );
                        }
                    }
                    debugStage = 200;
                    if ( isOnDeath )
                    {
                        buffer.Add(" on ").WrapReload( "Death", false, false );
                    }
                    else 
                    if ( isRetaliatory )
                    {
                        buffer.Add( " on " ).WrapReload( "Hit", false, false );
                    }
                    else
                    {
                        int secondsPerSalvoRightNow = ForMark.SecondsPerSalvo;
                        int forAnotherXSalvos = -1;
                        if ( System != null )
                        {
                            if ( System.IsInAltSalvoFiringMode )
                                secondsPerSalvoRightNow = ForMark.AltSecondsPerSalvo;
                            if ( ForMark.AltSecondsPerSalvo > 0 )
                            {
                                if ( System.IsInAltSalvoFiringMode )
                                    forAnotherXSalvos = Type.UseAlternateRateOfFireForXShotsBeforeReverting - System.ShotsSinceLastSwitchingSalvoFiringMode;
                                else
                                    forAnotherXSalvos = Type.UseAlternateRateOfFireAfterXShots - System.ShotsSinceLastSwitchingSalvoFiringMode;
                            }
                        }

                        if ( ForMark.ShotsPerSalvo > 1 )
                            buffer.StartMultishotWrapper( false ).Add("  x").Add( ForMark.ShotsPerSalvo ).Add( "</color> " );

                        buffer.Add( "<size=80%> / </size>" ).StartReloadWrapper( false ).Add( secondsPerSalvoRightNow ).Add( "s</color>" );
                        if ( forAnotherXSalvos > 0 && (detailLevel >= TooltipDetail.Full || Config.ForMultipleShips) )
                            buffer.Add( " for another " ).WrapReload( forAnotherXSalvos, false, false ).Add( " salvos" );

                        if ( ForMark.AltSecondsPerSalvo > 0 )
                        {
                            buffer.Add( "BURST FIRE" );
                            if ( detailLevel >= TooltipDetail.Medium )
                            {
                                buffer.Add( ": " );
                                if ( detailLevel >= TooltipDetail.Full )
                                    buffer.WrapReload( Type.UseAlternateRateOfFireAfterXShots, false, false ).Add( " salvos fired at " )
                                        .WrapReload( ForMark.SecondsPerSalvo, false, false ).Add( "s per salvo before switching to " )
                                        .WrapReload( Type.UseAlternateRateOfFireForXShotsBeforeReverting, false, false ).Add( " salvos fired at " )
                                        .WrapReload( ForMark.AltSecondsPerSalvo, false, false ).Add( "s per salvo (and then switching back)" );
                                else
                                    buffer.WrapReload( Type.UseAlternateRateOfFireAfterXShots, false, false ).Add( " at " )
                                        .WrapReload( ForMark.SecondsPerSalvo, false, false ).Add( "s before " )
                                        .WrapReload( Type.UseAlternateRateOfFireForXShotsBeforeReverting, false, false ).Add( " at " )
                                        .WrapReload( ForMark.AltSecondsPerSalvo, false, false ).Add( "s" );
                            }
                        }
                    }
                    
                    if ( display_salvodps_min > 0 && !isRetaliatory && !isOnDeath)
                    {
                        buffer.Add( " => " );
                        //if ( detailLevel < TooltipDetail.Full )
                        //{
                        //    buffer.WrapDamagePerSecondTruncated( display_salvodps_min, false, false );
                        //    if ( display_salvodps_max != display_salvodps_min )
                        //        buffer.Add( " to " ).WrapDamagePerSecondTruncated( display_salvodps_max, false, false );
                        //} 
                        //else
                        {
                            buffer.WrapDamagePerSecondMoreReadable( display_salvodps_min, false, false );
                            if ( display_salvodps_max != display_salvodps_min )
                            {
                                buffer.Add( " to " ).WrapDamagePerSecondMoreReadable( display_salvodps_max, false, false );
                            }
                        }
                        buffer.Add( " DPS" );
                    }

                    debugStage = 205;

                    if ( isRetaliatory || ( Type.ShotTypeData != null && Type.ShotTypeData.Category == GameEntityCategory.Ship ) )
                    { }
                    else 
                    if ( Type.ShotsDetonateImmediately &&
                        //beam weapons detonate immediately but still have a range
                        Type.BeamLengthMultiplier <= FInt.Zero )
                    {
                        //buffer.Add(" at ").WrapRange( "Detonates From Ship", false, false );
                    }
                    else 
                    if ( Type.IsMelee )
                        buffer.Add( " at " ).AddNumber( "Melee", TextTerm.Range, TermUse.Name);
                    else
                    {
                        int actualRange;
                        if ( !Squad.IsFakeEntity ) 
                        {
                            actualRange = ForMark.CalculateActualRange( Squad );
                        } 
                        else 
                        {
                            actualRange = ForMark.CalculateActualRange( facOrNull, relatedPlanet, 0 );
                        }
                        
                        buffer.Add( " at " );
                        if ( actualRange > 99999 ) 
                        {
                            //FiresFromAnyRange actually just has to do with targeting logic, and doesn't seem
                            //like something that we should be surfacing to players here.  Just doesn't seem relevant.
                            buffer.AddNumber( "Infinite", TextTerm.Range, TermUse.Name);
                        } 
                        else 
                        if ( detailLevel < TooltipDetail.Full )
                        {
                            buffer.AddNumber( actualRange, TextTerm.Range, TermUse.Name);
                        } 
                        else
                        {
                            buffer.AddNumber( actualRange, TextTerm.Range, TermUse.Name);
                        }
                    }

                    if ( Type.CareAboutStateOfMatterToBeEnabled )
                    {
                        buffer.Add( " - " );
                        if ( !Squad.IsFakeEntity )
                        {
                            if ( Type.MustBeThisStateOfMatterToBeEnabled == Squad.CurrentStateOfMatter )
                            {
                                buffer.Add( "Enabled", Color.green ).Add( " due to being in " ).Add( Type.MustBeThisStateOfMatterToBeEnabled.DisplayName, "aaaaaa" ).Add( " state of matter" );
                            } 
                            else
                            {
                                buffer.Add( "已禁用", Color.red ).Add( " 因为不在 " ).Add( Type.MustBeThisStateOfMatterToBeEnabled.DisplayName, "aaaaaa" ).Add( " 物质态" );
                            }
                        } 
                        else
                        {
                            buffer.Add( "必须在 " ).Add( Type.MustBeThisStateOfMatterToBeEnabled.DisplayName, "aaaaaa" ).Add( " 物质态才能启用" );
                        }
                    }
                    //WriteSystemStateOfMatterSuffix( buffer, Type, Squad );
                    
                    debugStage = 210;

#endif
                    #endregion
                    
                    #endregion

                    debugStage = 191;
                    buffer.Open( TextStyle.System_Sub_Lines );

                    #region Damage, Reload, Salvo-Size

                    #endregion
                    
                    #region Line2
                    {
                        debugStage = 214;

                        #region Corrosion
                        
                        if (display_shotcorrosiondamage > 0)
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            buffer.Add("腐蚀", TextStyle.System_Label2).Add(": ").Add(TextTerm.Damage_Corrosive, TermUse.Icon_Name).Add(" 随时间直接作用于 ").Add(TextTerm.Hull, TermUse.Icon_Name).Add(".");
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        
                        #endregion
                        
                        #region Devour
                        
                        if (System.TypeData.CanDevour)
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            
                            buffer.Add( "Devour", TextStyle.System_Label2 ).Add( ": Instakill targets ");
                            
                            buffer.AddTermRange(TermRange.Alloc(TextTerm.Mark, TermCmp.Less, this.ForMark.MarkLevel.Ordinal));
                            if ( Type.DevourMassRange.IsSet )
                            {
                                buffer
                                    .Add(" and ")
                                    .AddTermRange(TermRange.Alloc(TextTerm.Mass_tX, TermCmp.Less, Type.DevourMassLowerThan.ToFloat()));
                            }
                                    
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        
                        #endregion
                        
                        #region Infest
                        
                        if (System.TypeData.CanInfest)
                        {
                            var infestType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound(System.TypeData.UnitToSpawnOnInfestation);
                            if (infestType != null)
                            {
                                buffer.Open( TextStyle.System_Line2 );
                                buffer.Add("感染", TextStyle.System_Label2).Add(": ");
                                buffer.Add("此武器的击杀会生成 ").WriteSpawn(infestType, TextStyle.Ship_Sprite_Smaller).Add(".");
                                buffer.Close( TextStyle.System_Line2 );
                                buffer.Close(TextStyle.System_Sub_Lines);
                                buffer.Open(TextStyle.System_Line2_Reapply);
                            }
                        }
                        
                        #endregion

                        debugStage = 215;

                        #region AOE
                        if ( ForMark.ShotAreaOfEffect > 0 )
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            
                            //lighting-style burst out of ship
                            if ( Type.ShotsDetonateImmediately )
                            {
                                if ( detailLevel < TooltipDetail.Full )
                                {
                                    buffer.Add("直接范围",TextStyle.System_Label2).Add("：半径 <color=#ffdf72>" );
                                    buffer.AddNumberMoreReadable( ForMark.ShotAreaOfEffect );
                                    buffer.Add( "</color>, " );
                                    if ( Type.AOESpreadsDamageAmongAvailableTargets )
                                        buffer.Add( "DMG split between targets, " );
                                    if ( ForMark.AOEMaximumNumberOfTargetsHitPerShot > 0 )
                                        buffer.Add( "<= <color=#ffdf72>" ).Add( ForMark.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> targets" );
                                    else
                                        buffer.Add( "all targets in range" );

                                    if ( Type.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                    {
                                        buffer.Add( " and " );
                                        buffer.Add( Type.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                        buffer.Add( "% damage to non-primary targets" );
                                    }

                                    if ( Type.AOEHitsFriendlyTargets )
                                        buffer.Add( ", friendly fire" ).EndStatement(EndStatementStyle.Normal);
                                    else
                                        buffer.Add( "" ).EndStatement(EndStatementStyle.Normal);
                                }
                                else
                                {
                                    buffer.Add("范围",TextStyle.System_Label2).Add("：从本舰直接发出冲击波，半径 <color=#ffdf72>" );
                                    buffer.AddNumberMoreReadable( ForMark.ShotAreaOfEffect );
                                    buffer.Add( "</color>, " );
                                    if ( Type.AOESpreadsDamageAmongAvailableTargets )
                                    {
                                        buffer.Add( "spreading its damage among " );
                                        if ( ForMark.AOEMaximumNumberOfTargetsHitPerShot > 0 )
                                            buffer.Add( "at most <color=#ffdf72>" ).Add( ForMark.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> targets" );
                                        else
                                            buffer.Add( "all targets in range" );
                                        if ( Type.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                        {
                                            buffer.Add( ", with " );
                                            buffer.Add( Type.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                            buffer.Add( "% damage to targets other than the primary" );
                                        }
                                    }
                                    else
                                    {
                                        if ( Type.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                        {
                                            buffer.Add( "doing its full damage to the primary target, and " );
                                            buffer.Add( Type.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                            buffer.Add( "% damage to " );
                                        }
                                        else
                                            buffer.Add( "doing their full damage to " );
                                        if ( ForMark.AOEMaximumNumberOfTargetsHitPerShot > 0 )
                                            buffer.Add( "at most <color=#ffdf72>" ).Add( ForMark.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> targets" );
                                        else
                                            buffer.Add( "all targets in range" );
                                    }
                                    if ( Type.AOEHitsFriendlyTargets )
                                        buffer.Add( " -- including any friendlies caught in the blast" ).EndStatement(EndStatementStyle.Normal);
                                    else
                                        buffer.Add( "" ).EndStatement(EndStatementStyle.Normal);
                                }
                            }
                            else //AOE at destination
                            {
                                if ( detailLevel < TooltipDetail.Full )
                                {
                                    buffer.Add("目标范围",TextStyle.System_Label2).Add("：半径 <color=#ffdf72>" );
                                    buffer.AddNumberMoreReadable( ForMark.ShotAreaOfEffect );
                                    buffer.Add( "</color>, " );
                                    if ( Type.AOESpreadsDamageAmongAvailableTargets )
                                        buffer.Add( "DMG split between targets, " );
                                    if ( ForMark.AOEMaximumNumberOfTargetsHitPerShot > 0 )
                                        buffer.Add( "<= <color=#ffdf72>" ).Add( ForMark.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> targets" );
                                    else
                                        buffer.Add( "all targets in range" );

                                    if ( Type.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                    {
                                        buffer.Add( " and " );
                                        buffer.Add( Type.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                        buffer.Add( "% damage to non-primary targets" );
                                    }

                                    if ( Type.AOEHitsFriendlyTargets )
                                        buffer.Add( ", friendly fire" ).EndStatement(EndStatementStyle.Normal);
                                    else
                                        buffer.Add( "" ).EndStatement(EndStatementStyle.Normal);
                                }
                                else
                                {
                                    buffer.Add("范围",TextStyle.System_Label2).Add("：上述武器的射击会爆炸，半径 <color=#ffdf72>" );
                                    buffer.AddNumberMoreReadable( ForMark.ShotAreaOfEffect );
                                    buffer.Add( "</color> on impact, " );
                                    if ( Type.AOESpreadsDamageAmongAvailableTargets )
                                    {
                                        buffer.Add( "spreading their damage among " );
                                        if ( ForMark.AOEMaximumNumberOfTargetsHitPerShot > 0 )
                                            buffer.Add( "at most <color=#ffdf72>" ).Add( ForMark.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> targets" );
                                        else
                                            buffer.Add( "all targets in range" );
                                        if ( Type.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                        {
                                            buffer.Add( ", with " );
                                            buffer.Add( Type.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                            buffer.Add( "% damage to targets other than the primary" );
                                        }
                                    }
                                    else
                                    {
                                        if ( Type.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                        {
                                            buffer.Add( "doing its full damage to the primary target, and " );
                                            buffer.Add( Type.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                            buffer.Add( "% damage to " );
                                        }
                                        else
                                            buffer.Add( "doing their full damage to " );
                                        if ( ForMark.AOEMaximumNumberOfTargetsHitPerShot > 0 )
                                            buffer.Add( "at most <color=#ffdf72>" ).Add( ForMark.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> targets" );
                                        else
                                            buffer.Add( "all targets in range" );
                                    }
                                    if ( Type.AOEHitsFriendlyTargets )
                                        buffer.Add( " -- including any friendlies caught in the blast" ).EndStatement(EndStatementStyle.Normal);
                                    else
                                        buffer.Add( "" ).EndStatement(EndStatementStyle.Normal);
                                }
                            }
                            
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        #endregion

                        debugStage = 220;

                        #region Beam Weapon
                        if ( Type.BeamLengthMultiplier > FInt.Zero )
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            
                            if ( Type.IsCoilbeam )
                            {
                                // coil-beam, coil-beam-array
                                
                                if ( Type.NumberBeamsToFire == 1 )
                                {
                                    buffer.Add("线圈光束",TextStyle.System_Label2).Add("：一道光束 " );
                                }
                                else
                                {
                                    //if ( Type.DistanceFromCenterForBeamEmission > 0 )
                                        //buffer.Add("Radial-Beam-Array Weapon",TextStyle.System_Label2).Add(": " );
                                    //else
                                        buffer.Add("线圈光束阵列",TextStyle.System_Label2).Add("：" );
                                            
                                    buffer.AddNumber( Type.NumberBeamsToFire ).Add( " 束光束，每束 " );
                                }
                                
                                if ( ForMark.AOEMaximumNumberOfTargetsHitPerShot == 0 )
                                {
                                }
                                else
                                {
                                    buffer
                                            .Add(" 最多击中 ")
                                            .Add( ForMark.AOEMaximumNumberOfTargetsHitPerShot, TextStyle.Emphasis )
                                            .Add( " 个目标，");
                                }

                                int secondary_damage = display_shotdamage;
                                if (Type.AOEAndBeamDamageMultiplierToNonPrimaryTarget != FInt.Zero)
                                    secondary_damage = (secondary_damage.ToFInt() * Type.AOEAndBeamDamageMultiplierToNonPrimaryTarget).ToInt();
                                
                                buffer
                                        .Add(" 造成 ")
                                        .AddNumber(display_shotdamage, TextTerm.Damage, TermUse.Icon)
                                        .Add(" 对主目标，另外 ")
                                        .AddNumber(secondary_damage, TextTerm.Damage, TermUse.Icon)
                                        .Add(" 分摊至其他目标 " );

                                
                                if ( Type.AOEHitsFriendlyTargets )
                                    buffer.Add( ", including friendlies" );
                                
                                buffer.Add(".");
                            }
                            else 
                            if ( !Type.HitsAllIntersectingTargets )
                            {
                                // point-beam, or chain-beam...
                                
                                if ( Type.BeamChainsOutToTargetsXTimes <= 0 ||
                                     Type.BeamChainsOutToTargetsXRange <= 0 )
                                {
                                    // how about we just dont call it a beam at all
                                    // and thereby dont confuse the hell out of people?
                                    //if (Config.Detail >= TooltipDetail.Full)
                                        //buffer.Add("Point-Beam",TextStyle.System_Label2).Add(": Hits a single target." );
                                }
                                else
                                {
                                    buffer
                                        .Add("链式闪电",TextStyle.System_Label2)
                                        .Add("：连锁射出，击中 ");
                                        
                                    int cap = System.GetMaxChainHits();
                                    if (cap < 1)
                                        cap = int.MaxValue;
                                            
                                    FInt amt = Type.AOEAndBeamDamageMultiplierToNonPrimaryTarget;
                                    if (amt == FInt.Zero)
                                        amt = FInt.OneHundred;
                                    
                                    buffer
                                        .AddNumber(cap)
                                        .Add(" 更多目标，每次 ")
                                        .AddNumber(
                                            ()=>
                                            {
                                                int perc = amt.ToInt();
                                                buffer
                                                    .AddNumber(perc, null, TextStyle.Empty)
                                                    .Add("%");
                                            },
                                            null, TextTerm.Damage, TermUse.Icon, null);
                                    
                                    if ( Type.AOEHitsFriendlyTargets )
                                        buffer.Add( ", including friendlies" );
                                    
                                    buffer.Add("，跳跃 ");
                                    
                                    if (Type.BeamChainsOutToTargetMinRange > 0)
                                    {
                                        var type = this.Type;
                                        
                                        buffer.AddNumber(
                                            ()=>
                                                {
                                                    buffer
                                                        .AddNumber(type.BeamChainsOutToTargetMinRange, null, TextStyle.Empty)
                                                        .Add(" 禄 ")
                                                        .AddNumber(type.BeamChainsOutToTargetsXRange, null, TextStyle.Empty);
                                                }, 
                                            null, TextTerm.Range, TermUse.Name, null );
                                    }
                                    else
                                    {
                                        buffer.AddNumber(Type.BeamChainsOutToTargetsXRange, TextTerm.Range, TermUse.Name);
                                    }

                                    buffer.Add(".");
                                }
                            }
                            else
                            {
                                // regular beam, or beam-array...
                                
                                if ( Type.NumberBeamsToFire == 1 )
                                {
                                    buffer.Add("光束",TextStyle.System_Label2).Add("：一道光束 " );
                                }
                                else
                                {
                                    if ( Type.DistanceFromCenterForBeamEmission > 0 )
                                        buffer.Add("径向光束阵列",TextStyle.System_Label2).Add("：" );
                                    else
                                        buffer.Add("光束阵列",TextStyle.System_Label2).Add("：" );
                                            
                                    buffer.AddNumber( Type.NumberBeamsToFire ).Add( " 束光束，每束 " );
                                }
                                
                                if ( ForMark.AOEMaximumNumberOfTargetsHitPerShot == 0 )
                                {
                                }
                                else
                                {
                                    buffer
                                            .Add(" 最多击中 ")
                                            .Add( ForMark.AOEMaximumNumberOfTargetsHitPerShot, TextStyle.Emphasis )
                                            .Add( " 个目标，");
                                }

                                int secondary_damage = display_shotdamage;
                                if (Type.AOEAndBeamDamageMultiplierToNonPrimaryTarget != FInt.Zero)
                                    secondary_damage = (secondary_damage.ToFInt() * Type.AOEAndBeamDamageMultiplierToNonPrimaryTarget).ToInt();
                                
                                if (secondary_damage != display_shotdamage)
                                {
                                buffer
                                        .Add(" 造成 ")
                                        .AddNumber(display_shotdamage, TextTerm.Damage, TermUse.Icon)
                                        .Add(" 对主目标，另外 ")
                                        .AddNumber(secondary_damage, TextTerm.Damage, TermUse.Icon)
                                        .Add(" 分摊至其他目标 " );
                                }
                                else
                                {
                                    buffer
                                        .Add(" 造成 ")
                                        .AddNumber(display_shotdamage, TextTerm.Damage, TermUse.Icon)
                                        .Add(" 对每个目标 ");
                                }

                                if ( Type.AOEHitsFriendlyTargets )
                                    buffer.Add( ", including friendlies" );

                                buffer.Add(".");
                            }
                            
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        #endregion

                        debugStage = 223;

                        #region ShotsPerSalvo
                        if ( Type.ForMark[effectiveMarkLevel].ShotsPerSalvo > 1 && !IsMirrorShot )
                        {
                            if ( detailLevel >= TooltipDetail.Full )
                            {
                                buffer.Open( TextStyle.System_Line2 );
                                
                                var map = TextVarMap.Get("Salvo_Format");

                                buffer.AddVarReplace(map, this.EvalCondition, this.AppendVar_Weapon, this);
                                
                                /*
                                if ( Type.BeamLengthMultiplier > FInt.Zero && 
                                     Type.HitsAllIntersectingTargets)
                                {
                                    buffer
                                        .Add("多重光束",TextStyle.System_Label2).Add("：最多 <color=#ffdf72>" ).Add( Type.ForMark[effectiveMarkLevel].ShotsPerSalvo )
                                        .Add( "</color> 条光束可同时发射。" );
                                    
                                    if ( Type.ForMark[effectiveMarkLevel].ShotsPerSalvo > Type.ForMark[effectiveMarkLevel].ShotsPerTarget )
                                        buffer.Add( "每目标仅 " ).Add( Type.ForMark[effectiveMarkLevel].ShotsPerTarget ).Add( " 条。" );
                                    else
                                        buffer.Add( "全部可瞄准同一目标。" );
                                    
                                    if ( !Type.HitsAllIntersectingTargets )
                                        buffer.Add( "单个目标可能被多条交叉光束命中。每条光束造成上述全额伤害。" );
                                    else 
                                    if ( ForMark.AOEMaximumNumberOfTargetsHitPerShot >= 1 )
                                    {
                                        buffer.Add( "每条光束造成上述全额伤害。" );
                                    }
                                } 
                                else
                                {
                                    buffer
                                        .Add("齐射",TextStyle.System_Label2)
                                        .Add("：最多 <color=#ffdf72>" )
                                        .Add( Type.ForMark[effectiveMarkLevel].ShotsPerSalvo )
                                        .Add( "</color> 发可同时发射。" );
                                    
                                    if ( Type.ForMark[effectiveMarkLevel].ShotsPerSalvo > Type.ForMark[effectiveMarkLevel].ShotsPerTarget )
                                        buffer.Add( "每目标仅 " ).Add( Type.ForMark[effectiveMarkLevel].ShotsPerTarget ).Add( " 发" );
                                    else
                                        buffer.Add( " All can be aimed at the same target" );
                                    
                                    buffer.Add( ", and each shot does the full damage listed above." );
                                }
                                */
                                buffer.Close( TextStyle.System_Line2 );
                            }
                        }
                        #endregion

                        debugStage = 220;
                        
                        #region isMirrorShot
                        if ( IsRetaliatory )
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            buffer.Add( "反击", TextStyle.System_Label2 ).Add("：被击中时向攻击者还击。");
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        #endregion
                        
                        debugStage = 221;
                        #region MobileImmobile
                        if ( Type.OnlyTargetsStaticUnits )
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            buffer.Add("有限目标",TextStyle.System_Label2).Add("：只能攻击固定设施。" );
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        if ( Type.OnlyTargetsMobileUnits )
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            buffer.Add("有限目标",TextStyle.System_Label2).Add("：只能攻击移动单位。" );
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        if ( Type.OnlyTargetsStrikecraftAndFrigates )
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            buffer.Add("有限目标",TextStyle.System_Label2).Add("：只能攻击战机与护卫舰。" );
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        #endregion
                        
                        #region EngineStun
                        debugStage = 225;
                        if ( Type.EngineStunToEngine_gxLessThan > 0 )
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            
                            if ( detailLevel < TooltipDetail.Full )
                            {
                                if ( Type.MaxEngineStunSeconds > 0 )
                                    buffer.Add("引擎减速",TextStyle.System_Label2).Add("：<color=#ffdf72>" );
                                else
                                    buffer.Add("引擎眩晕",TextStyle.System_Label2).Add("：<color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( ForMark.EngineStunPerShot );
                                buffer.Add( "s</color> if target engine < <color=#ffdf72>" );
                                buffer.Add( Type.EngineStunToEngine_gxLessThan );
                                buffer.Add( " gx</color>" );
                                
                                if ( Type.MaxEngineStunSeconds > 0 )
                                {
                                    if ( Type.MaxEngineStunSeconds >= ExternalConstants.Instance.EngineStunMultipliersByStunSeconds.Count )
                                        buffer.Add( "." );
                                    else 
                                        buffer.Add( ", max " ).AddNumberMoreReadable( Type.MaxEngineStunSeconds ).Add( "s." );
                                }
                                else
                                    buffer.Add( "." );
                            }
                            else
                            {
                                if ( Type.MaxEngineStunSeconds > 0 )
                                    buffer.Add("引擎减速",TextStyle.System_Label2).Add("：上述武器的射击减速敌方引擎 <color=#ffdf72>" );
                                else
                                    buffer.Add("引擎眩晕",TextStyle.System_Label2).Add("：上述武器的射击眩晕敌方引擎 <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( ForMark.EngineStunPerShot );
                                buffer.Add( "s</color> if the target has an engine power less than <color=#ffdf72>" );
                                buffer.Add( Type.EngineStunToEngine_gxLessThan );
                                buffer.Add( " gx</color>." );
                                
                                if ( Type.MaxEngineStunSeconds > 0 )
                                {
                                    FInt engineSpeed = FInt.Zero;
                                    if ( Type.MaxEngineStunSeconds >= ExternalConstants.Instance.EngineStunMultipliersByStunSeconds.Count )
                                    { } //fully stunned
                                    else //partially stunned
                                        engineSpeed = ExternalConstants.Instance.EngineStunMultipliersByStunSeconds[Type.MaxEngineStunSeconds];

                                    buffer.Add( " The target can be slowed up to a full " ).AddNumberMoreReadable( Type.MaxEngineStunSeconds )
                                        .Add( "s, at which point its movement speed will only be  <color=#ffdf72>" ).Add( engineSpeed.ReadableString ).Add( "x</color> normal." );
                                }
                                else
                                {
                                    buffer.Add( " The more stun-seconds accumlated on a target, the slower it goes. 4s = 50% move speed, 7s+ = immobilized." );
                                }
                            }
                            
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        #endregion
                        
                        #region Paralyzer
                        debugStage = 235;
                        if ( Type.ParalysisToShipsMass_tXLessThan > 0 )
                        {
                            buffer.Open( TextStyle.System_Line2 );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add("麻痹",TextStyle.System_Label2).Add("：<color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( ForMark.ParalysisSecondsPerShot );
                                buffer.Add( "s</color> if target mass < <color=#ffdf72>" );
                                buffer.Add( Type.ParalysisToShipsMass_tXLessThan.ReadableString );
                                buffer.Add( " tX</color>." );
                            }
                            else
                            {
                                buffer.Add("麻痹",TextStyle.System_Label2).Add("：上述武器的射击完全瘫痪敌方舰船 <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( ForMark.ParalysisSecondsPerShot );
                                buffer.Add( "s</color> if the target has a mass less than <color=#ffdf72>" );
                                buffer.Add( Type.ParalysisToShipsMass_tXLessThan.ReadableString );
                                buffer.Add( " tX</color>." );
                            }
                            
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        #endregion
                        
                        #region FiresThroughEnemyShields
                        debugStage = 236;
                        if ( Type.FiresThroughEnemyShields )
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            
                            buffer.Add("穿透力场",TextStyle.System_Label2);

                            if ( detailLevel >= TooltipDetail.Medium )
                            {
                                buffer.Add("：上述武器的射击完全无视力场，正常伤害力场下的舰船。" );
                            }
                            
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        #endregion

                        #region SetsTargetToRandomLocation
                        if ( Type.SetsTargetToRandomLocation )
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            
                            buffer.Add("目标扭曲",TextStyle.System_Label2);

                            if ( detailLevel >= TooltipDetail.Medium )
                            {
                                buffer.Add( ": Shots from the above weapon cause enemies that are hit to be moved to a completely random spot in the planet's gravity well." );
                            }
                            
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        #endregion
                        
                        #region StackingDamage
                        debugStage = 237;
                        if ( Type.MaxStacksToKill != 1 )
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            
                            buffer.Add("多重击杀",TextStyle.System_Label2).Add("：每击可击杀 <color=#ffdf72>").Add(Type.MaxStacksToKill).Add("</color> 个堆叠舰船。" );
                            
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        #endregion

                        debugStage = 238;

                        #region TractorDamage
                        if ( ForMark.DamageMultiplierToTractoredUnits > FInt.Zero )
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            
                            buffer.Add("增益",TextStyle.System_Label2).Add("：上述武器的射击对牵引单位造成 <color=#ffdf72>" ).Add( ForMark.DamageMultiplierToTractoredUnits.ReadableString, "a1ffa1" ).Add( "x</color> 伤害。" );
                            
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        #endregion

                        debugStage = 239;

                        #region ForcefieldDamageMultiplier
                        if ( Type.DamageModifierWhileUnderForcefield != FInt.FromParts( 0, 500 ) )
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            
                            buffer.Add("增益",TextStyle.System_Label2).Add("：上述武器的射击造成 <color=#ffdf72>" ).Add( Type.DamageModifierWhileUnderForcefield.ReadableString, "a1ffa1" ).Add( "x</color> 伤害（若在力场下，无视正常惩罚）。" );
                            
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        #endregion

                        debugStage = 240;

                        #region WeaponPoints
                        // The case where it doesn't consume Weapon Points on firing, i.e Neinzul Firefly style.
                        if ( Type.AdditionalDamageModifierPerWeaponPoint != FInt.Zero && Type.NumberOfWeaponPointsToConsumeOnFiring == 0 )
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            
                            if ( detailLevel < TooltipDetail.Full )
                                buffer.Add("增益",TextStyle.System_Label2).Add("：额外 <color=#ffdf72>" ).Add( Type.AdditionalDamageModifierPerWeaponPoint.ReadableString ).Add( "x</color> 每当前武器点数。" );
                            else
                                buffer.Add("增益",TextStyle.System_Label2).Add("：上述武器的射击每当前武器点数造成额外 <color=#ffdf72>" ).Add( Type.AdditionalDamageModifierPerWeaponPoint.ReadableString ).Add( "x</color> 伤害。" );
                            
                            buffer.Close( TextStyle.System_Line2 );
                        }

                        // The case where it DOES consume Weapon Points on firing, i.e Powerslaver style.
                        if ( Type.AdditionalDamageModifierPerWeaponPoint != FInt.Zero && Type.NumberOfWeaponPointsToConsumeOnFiring != 0 )
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            
                            if ( detailLevel < TooltipDetail.Full )
                                buffer.Add("增益",TextStyle.System_Label2).Add("：消耗 <color=#ffdf72>" + Type.NumberOfWeaponPointsToConsumeOnFiring + "</color> 武器点数，每消耗的武器点数额外 <color=#ffdf72>" ).Add( Type.AdditionalDamageModifierPerWeaponPoint.ReadableString ).Add( "x</color> 伤害。" );
                            else
                                buffer.Add("增益",TextStyle.System_Label2).Add("：上述武器的射击每齐射消耗 <color=#ffdf72>" + Type.NumberOfWeaponPointsToConsumeOnFiring + "</color> 武器点数，每消耗的武器点数造成额外 <color=#ffdf72>" ).Add( Type.AdditionalDamageModifierPerWeaponPoint.ReadableString ).Add( "x</color> 伤害。" );
                            
                            buffer.Close( TextStyle.System_Line2 );
                        }

                        if ( Type.NumberOfWeaponPointsToGainOnFiring != FInt.Zero )
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            
                            buffer.Add("获得武器点数",TextStyle.System_Label2).Add("：上述武器的射击增加本单位的武器点数 ").AddColor(Type.NumberOfWeaponPointsToGainOnFiring,"ffdf72").Add("，上限 ").AddColor(Type.ParentEntityTypeData.MaxNumberOfWeaponPoints,"ffdf72").Add("。");
                            
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        #endregion

                        debugStage = 250;

                        #region Death Effects
                        if ( Type.HasAnyDeathEffectOffenses )
                        {
                            foreach (var pair in ForMark.DeathEffectDamagePerShotByType)
                            {
                                DeathEffectType row = pair.Key;
                                
                                int damageAmount = pair.Value;
                                if (damageAmount <= 0)
                                    continue;

                                buffer.Open( TextStyle.System_Line2 );
                                
                                buffer
                                        .Add(row.DescriptionPrefix, TextStyle.System_Label2)
                                        .Add( ": " )
                                        .Open(TextStyle.Number).AddNumber( damageAmount ).Close(TextStyle.Number)
                                        .Add(" ").Add( row.DescriptionDamageName ).Add( " damage." );
                                
                                if ( detailLevel > TooltipDetail.Medium )
                                {
                                    buffer.Add( " " );
                                    
                                    if (!string.IsNullOrEmpty(row.DescriptionConditions))
                                        buffer.Add( row.DescriptionConditions ).Add(". ");
                                    
                                    buffer
                                        .Add( "If at least " )
                                        .Open(TextStyle.Number).AddNumber( row.Scale ).Close(TextStyle.Number)
                                        .Add( " " ).Add( row.DescriptionDamageName )
                                        .Add( " 伤害在目标死亡时已造成，然后 " ).Add( row.DescriptionOfEffects ).Add("。");
                                }
                                
                                //only for player ships or things that are being contemplated for construction
                                if ((Squad == null || 
                                    Squad.GetFactionTypeSafe() != FactionType.Player) &&
                                    !string.IsNullOrEmpty(row.DescriptionAddedNoteForPlayers))
                                {
                                    buffer.Add(" ").Add( row.DescriptionAddedNoteForPlayers );
                                }
                                
                                buffer.Close( TextStyle.System_Line2 );
                            }
                        }
                        #endregion

                        debugStage = 260;

                        #region IsMelee
                        if ( Type.IsMelee && detailLevel >= TooltipDetail.Full )
                        {
                            buffer.Open( TextStyle.System_Line2 );

                            buffer.Add("近战武器",TextStyle.System_Label2).Add("：本舰必须进入触碰范围内才能命中目标。" );
                            
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        #endregion

                        debugStage = 265;

                        #region EnemyWeaponReloadSlowing
                        if ( Type.EnemyWeaponReloadSlowingSecondsArmor_mmLessThan > 0 )
                        {
                            buffer.Open( TextStyle.System_Line2 );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add("武器干扰器",TextStyle.System_Label2).Add("：目标装填时间 +<color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( ForMark.EnemyWeaponReloadSlowingSecondsPerShot );
                                buffer.Add( "s</color> if armor < <color=#ffdf72>" );
                                buffer.Add( Type.EnemyWeaponReloadSlowingSecondsArmor_mmLessThan );
                                buffer.Add( "mm</color>, max " );
                                if ( Type.MaxEnemyWeaponReloadSlowingSeconds > 0 )
                                    buffer.Add( Type.MaxEnemyWeaponReloadSlowingSeconds );
                                else
                                    buffer.Add( ExternalConstants.Instance.MaxWeaponAddedReloadSeconds );
                                buffer.Add( "s." );
                            }
                            else
                            {
                                buffer.Add("武器干扰器",TextStyle.System_Label2).Add("：上述武器的射击增加被命中敌方的装填时间 <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( ForMark.EnemyWeaponReloadSlowingSecondsPerShot );
                                buffer.Add( "s</color> if the target has an armor thickness of less than <color=#ffdf72>" );
                                buffer.Add( Type.EnemyWeaponReloadSlowingSecondsArmor_mmLessThan );
                                buffer.Add( "mm</color>.  The total amount of extra reload time per target that can be applied is " );
                                if ( Type.MaxEnemyWeaponReloadSlowingSeconds > 0 )
                                    buffer.Add( Type.MaxEnemyWeaponReloadSlowingSeconds );
                                else
                                    buffer.Add( ExternalConstants.Instance.MaxWeaponAddedReloadSeconds );
                                buffer.Add( "s." );
                            }
                            
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        #endregion

                        debugStage = 270;

                        #region PercentDamageBypassesPersonalShields
                        if ( ForMark.PercentDamageBypassesPersonalShields > FInt.Zero )
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            
                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add("聚变反应",TextStyle.System_Label2).Add("：<color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( ForMark.PercentDamageBypassesPersonalShields.ToFloatNonSim() * 100f ) );
                                buffer.Add( "%</color> 直接作用于目标船体。" );
                            }
                            else if ( !Type.FiresThroughEnemyShields )
                            {
                                buffer.Add("聚变反应",TextStyle.System_Label2).Add("：上述武器的射击将 <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( ForMark.PercentDamageBypassesPersonalShields.ToFloatNonSim() * 100f ) );
                                buffer.Add( "%</color> 的伤害直接作用于目标船体，无视个人护盾（不包括泡泡力场）。" );
                            }
                            else
                            {
                                buffer.Add("聚变反应",TextStyle.System_Label2).Add("：上述武器的射击将 <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( ForMark.PercentDamageBypassesPersonalShields.ToFloatNonSim() * 100f ) );
                                buffer.Add( "%</color> 的伤害直接作用于目标船体，无视个人护盾。" );
                            }
                            
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        #endregion

                        debugStage = 275;

                        #region ShieldDrain
                        if ( ForMark.ShieldDrainPercent > FInt.Zero )
                        {
                            buffer.Open( TextStyle.System_Line2 );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "护盾汲取", TextStyle.System_Label2 ).Add( "：<color=#72cfff>" );
                                buffer.Add( Mathf.RoundToInt( ForMark.ShieldDrainPercent.ToFloatNonSim() * 100f ) );
                                buffer.Add( "%</color> of shield damage restored to attacker's shields." );
                            }
                            else
                            {
                                buffer.Add( "护盾汲取", TextStyle.System_Label2 ).Add( "：每次射击恢复 <color=#72cfff>" );
                                buffer.Add( Mathf.RoundToInt( ForMark.ShieldDrainPercent.ToFloatNonSim() * 100f ) );
                                buffer.Add( "%</color> of the shield damage it deals back to this ship's own shields (capped by current shield damage taken)." );
                            }

                            buffer.Close( TextStyle.System_Line2 );
                        }
                        #endregion

                        debugStage = 280;

                        #region KnockbackToTarget
                        if ( Type.KnockbackPerShotToShipsMass_tXLessThan > 0 )
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            
                            #region Abbreviated
                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add("击退",TextStyle.System_Label2).Add("：" );
                                if ( ForMark.KnockbackPerShot > 0 )
                                {
                                    if ( !Type.KnockbackAtTargetLocation )
                                    {
                                        buffer.Add( "Targets pushed <color=#ffdf72>" );
                                        buffer.AddNumberMoreReadable( ForMark.KnockbackPerShot );
                                        buffer.Add( "</color> away from this ship if mass <= <color=#ffdf72>" );
                                        buffer.Add( Type.KnockbackPerShotToShipsMass_tXLessThan );
                                        buffer.Add( " tX</color>. Knockback decreases with higher mass." );
                                    }
                                    else
                                    {
                                        buffer.Add( "Targets hit by the AoE pushed <color=#ffdf72>" );
                                        buffer.AddNumberMoreReadable( ForMark.KnockbackPerShot );
                                        buffer.Add( "</color> away from the AoE center if mass <= <color=#ffdf72>" );
                                        buffer.Add( Type.KnockbackPerShotToShipsMass_tXLessThan );
                                        buffer.Add( " tX</color>. Knockback decreases with higher mass." );
                                    }
                                }
                                else
                                {
                                    if ( !Type.KnockbackAtTargetLocation )
                                    {
                                        buffer.Add( "Targets pulled <color=#ffdf72>" );
                                        buffer.AddNumberMoreReadable( ForMark.KnockbackPerShot );
                                        buffer.Add( "</color> towards this ship if mass <= <color=#ffdf72>" );
                                        buffer.Add( Type.KnockbackPerShotToShipsMass_tXLessThan );
                                        buffer.Add( " tX</color>. Knockback decreases with higher mass." );
                                    }
                                    else
                                    {
                                        buffer.Add( "Targets hit by the AoE pulled <color=#ffdf72>" );
                                        buffer.AddNumberMoreReadable( ForMark.KnockbackPerShot );
                                        buffer.Add( "</color> towards the AoE center if mass <= <color=#ffdf72>" );
                                        buffer.Add( Type.KnockbackPerShotToShipsMass_tXLessThan );
                                        buffer.Add( " tX</color>. Knockback decreases with higher mass." );
                                    }
                                }
                            }
                            #endregion

                            #region Non-Abbreviated
                            else
                            {
                                buffer.Add("击退",TextStyle.System_Label2).Add("：被此武器击中的敌人 " );
                                if ( ForMark.KnockbackPerShot > 0 )
                                    buffer.Add( "pushed away from " );
                                else
                                    buffer.Add( "pulled towards " );
                                if ( !Type.KnockbackAtTargetLocation )
                                    buffer.Add( "this ship. " );
                                else
                                    buffer.Add( "the center of the AoE of this ship's shots. " );
                                if ( ForMark.KnockbackPerShot > 0 )
                                    buffer.Add( "The maximum distance a ship can be pushed is <color=#ffdf72>" );
                                else
                                    buffer.Add( "The maximum distance a ship can be pulled is <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( ForMark.KnockbackPerShot );
                                buffer.Add( "</color>, decreasing as the target's mass approaches the max mass of <color=#ffdf72>" );
                                buffer.Add( Type.KnockbackPerShotToShipsMass_tXLessThan );
                                buffer.Add( "tX</color>." );
                            }
                            #endregion
                            
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        #endregion

                        debugStage = 285;

                        #region DamageAmplification
                        if ( Type.DamageAmplification && Type.DamageAmplificationDuration_Max15 > 0 )
                        {
                            string label = "Damage-Amplification";
                            if (detailLevel < TooltipDetail.Medium)
                                label = "Damage-Amp";
                            
                            buffer.Open( TextStyle.System_Line2 );
                            
                            buffer.Add(label,TextStyle.System_Label2).Add("：目标将承受 ");

                            if ( Type.DamageAmplificationMult != 1 )
                            {
                                buffer.Add("<color=#ffdf72>").Add( Mathf.RoundToInt( Type.DamageAmplificationMult.ToFloatNonSim() * 100f ) ).Add( "%");
                                if ( ForMark.DamageAmplificationFlat > 0 )
                                    buffer.Add("<color=#ffdf72> + ").Add( Mathf.RoundToInt( ForMark.DamageAmplificationFlat ) );
                                
                                buffer.Add( "</color>");
                            }
                            else if ( ForMark.DamageAmplificationFlat > 0 )
                            {
                                buffer.Add("<color=#ffdf72> +").Add( Mathf.RoundToInt( ForMark.DamageAmplificationFlat ) ).Add( "</color>");
                            }
                            
                            buffer
                                .Add(" 的正常伤害，持续 ")
                                .AddMinutesAndSeconds(Mathf.RoundToInt( Type.DamageAmplificationDuration_Max15 ))
                                .Add("。");

                            buffer.Close( TextStyle.System_Line2 );
                        }
                        #endregion

                        debugStage = 290;

                        #region Vampirism/SelfDamage //Damage-based Vampirism/Self-Damage
                        if ( Type.HealthChangePerDamageDealt != FInt.Zero )
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            
                            if ( Type.HealthChangePerDamageDealt > FInt.Zero )
                                buffer.Add("吸血",TextStyle.System_Label2).Add("：修复 <color=#ffdf72>" );
                            else
                                buffer.Add("自伤",TextStyle.System_Label2).Add("：自身受到 <color=#ffdf72>" );

                            buffer.AddNumberMoreReadable( Type.HealthChangePerDamageDealt );
                            buffer.Add( " health</color> per damage dealt." );
                            
                            buffer.Close( TextStyle.System_Line2 );
                        }

                        //Max-Health-based Vampirism/Self-Damage
                        if ( Type.HealthChangeByMaxHealthDividedByThisPerAttack != FInt.Zero  && !IsOnDeath )
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            
                            if ( Type.HealthChangeByMaxHealthDividedByThisPerAttack >= FInt.One * -1 && Type.HealthChangeByMaxHealthDividedByThisPerAttack < FInt.Zero )
                            {
                                buffer.Add("自毁",TextStyle.System_Label2).Add("：自毁以攻击。" );
                            }
                            else
                            {
                                if ( Type.HealthChangeByMaxHealthDividedByThisPerAttack > FInt.Zero )
                                    buffer.Add("自组装",TextStyle.System_Label2).Add("：自我修复 <color=#ffdf72>" );
                                else
                                    buffer.Add("分解",TextStyle.System_Label2).Add("：自身受到 <color=#ffdf72>" );

                                buffer.AddPercentFormated( (1 / Type.HealthChangeByMaxHealthDividedByThisPerAttack).ToPercent( 1 ) );
                                buffer.Add( " health</color> each attack." );
                            }
                            
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        #endregion

                        debugStage = 300;

                        #region StateOfMatter-Inflicting

                        if ( Type.InflictsStateOfMatterOnTargetForSeconds > 0 && Type.StateOfMatterForTargetToBecome != null )
                        {
                            buffer.Open( TextStyle.System_Line2 );
                            
                            buffer
                                .Add("相位",TextStyle.System_Label2)
                                .Add("：造成 " )
                                .Add( Type.StateOfMatterForTargetToBecome.DisplayName, "aaaaaa" )
                                .Add( " for " )
                                .StartReloadWrapper( false ).Add( Type.InflictsStateOfMatterOnTargetForSeconds ).Add( "s</color>" );
                            
                            if ( Type.CannotInflictStateOfMatterIfTargetHasAnyShieldsUp )
                            {
                                if ( detailLevel < TooltipDetail.Full )
                                    buffer.Add( " if target " ).WrapHull( "hull struck", false, false );
                                else
                                    buffer.Add( " if the target's " ).WrapHull( "hull is struck", false, false );
                                if ( Type.CannotInflictStateOfMatterIfTargetHasEnergyUsageOfAtLeast > 0 )
                                {
                                    if ( detailLevel < TooltipDetail.Full )
                                        buffer.Add( " and consumes < " ).WrapEnergyTruncated( Type.CannotInflictStateOfMatterIfTargetHasEnergyUsageOfAtLeast, useIcons, useText );
                                    else
                                        buffer.Add( " and the target uses less than " ).WrapEnergyMoreReadable( Type.CannotInflictStateOfMatterIfTargetHasEnergyUsageOfAtLeast, useIcons, useText );
                                }
                            } 
                            else 
                            if ( Type.CannotInflictStateOfMatterIfTargetHasEnergyUsageOfAtLeast > 0 )
                            {
                                if ( Type.CannotInflictStateOfMatterIfTargetHasEnergyUsageOfAtLeast > 0 )
                                {
                                    if ( detailLevel < TooltipDetail.Full )
                                        buffer.Add( " if target consumes < " ).WrapEnergyTruncated( Type.CannotInflictStateOfMatterIfTargetHasEnergyUsageOfAtLeast, useIcons, useText );
                                    else
                                        buffer.Add( " if the target uses less than " ).WrapEnergyMoreReadable( Type.CannotInflictStateOfMatterIfTargetHasEnergyUsageOfAtLeast, useIcons, useText );
                                }
                            }
                            
                            buffer.Close( TextStyle.System_Line2 );
                        }
                        #endregion

                        debugStage = 310;

                        for ( int k = 0; k < Type.OutgoingDamageModifiers_FullList.Count; k++ )
                        {
                            writer.Write(Type.OutgoingDamageModifiers_FullList[k]);
                            //EntityText.WriteDamageModifierData( buffer, effectiveMarkLevel, Type.OutgoingDamageModifiers_FullList[k], ForMark );
                        }

                        //if ( haveDoneNewLineAndSize )
                        //    buffer.Add( "</size>" );
                    }

                    buffer.Close( TextStyle.System_Sub_Lines );
                    
                    #region Debug/Targeting
                    if ( !Squad.IsFakeEntity &&
                         Config.ShowTargetInfo )
                    {
                        int counter = 0;
                        var sys = this.System;
                        var planet = this.Squad.Planet;
                        var config = this.Config;
                        
                        int frd_p = System.CurrentFRDPriority;
                        var frd_w  = System.CurrentFRDTarget;
                        var frd_s = frd_w.GetSquad();
                        int num = Math.Min(config.MaxDisplayedTargets, System.CountOfPotentialTargetsByPriorityForSystem());
                        
                        if (frd_s != null || 
                            frd_w.PrimaryKeyID > 0 || 
                            num > 0)
                        {
                            void WriteATarget(ArcenCharacterBufferBase B, LazyLoadSquadWrapper W, EntitySystem SYS, EntityTextWriter ETW, bool IsFRD, int Counter)
                            {
                                var e = W.GetSquad();
                                if (e == null && W.PrimaryKeyID <= 0)
                                    return;
                                
                                int pri = SYS.PotentialTargetToPriorityLookup.Display[W.PrimaryKeyID];
                                if (IsFRD)
                                    pri = frd_p;

                                void Target_AppendVar(string _n, TextStyle _s, ArcenCharacterBufferBase _b, object _)
                                {
                                    if (_n == "prefix")
                                    {
                                        _b.Open(TextStyle.Color_Gray);
                                        _b.Add("[");
                                        if (pri == -1)
                                            _b.Add( "?", TextStyle.Number);
                                        else
                                            _b.Add( pri, TextStyle.Number );
                                        _b.Add("]");
                                        
                                        _b.StartSize("60%");
                                        _b.Add("#");
                                        if (IsFRD)
                                        {
                                            _b.Add( "frd", TextStyle.Color_Green);
                                        }
                                        else
                                        {
                                            _b.Add( counter );
                                        }
                                        _b.Space("0.1em");
                                        _b.EndSize();
                                        
                                        _b.Close(TextStyle.Color_Gray);

                                        return;
                                    }
                                    
                                    if (_n == "entity")
                                    {
                                        if (e == null)
                                            _b.Add("null");
                                        else
                                        {
                                            var ship = new ShipForDisplay(e, ETW.Config);
                                            ship.Write(_b, TextVarMap.Inline_Ship_Format_Target);
                                        }
                                        return;
                                    }
                                    
                                    if (_n == "id")
                                    {
                                        _b.Add("#").Add(W.PrimaryKeyID);
                                        return;
                                    }
                                    
                                    if (_n == "oor")
                                    {
                                        if ( e != null && !SYS.GetIsTargetInRange( e, RangeCheckType.ForActualFiring ) )
                                            _b.Add( " (OOR)" );
                                        return;
                                    }
                                }
                                
                                bool Target_EvalCond(string _c, object _a)
                                {
                                    if (_c == "IsFRD")
                                    {
                                        return IsFRD;
                                    }
                                    if (_c == "NotNull")
                                    {
                                        return e != null;
                                    }
                                    return false;
                                }
                                
                                B.AddVarReplace(TextVarMap.System_Targeting_Format, Target_EvalCond, Target_AppendVar, null);
                            }
                            
                            buffer.Open( TextStyle.SystemTargets_Line );
                            buffer.Add("目标锁定",TextStyle.SystemTargets_Label).Add("：" );
                            WriteATarget(buffer, frd_w, sys, writer, true, counter);
                            buffer.Close(TextStyle.SystemTargets_Line);
                            
                            buffer.Open( TextStyle.SystemTargets_Sub_Lines );
                            foreach ( EntitySystem.TargetWithPriority twp in sys.Targets() )
                            {
                                if ( counter >= config.MaxDisplayedTargets )
                                    break;

                                LazyLoadSquadWrapper w = twp.Wrapper;
                                //int p = twp.Priority;
                                //LOG.Msg("(SystemText) {0} Importance = {1}", w.PrimaryKeyID, p);

                                counter++;
                                //buffer.Add( " | ", TextStyle.Color_Gray );

                                WriteATarget(buffer, w, sys, writer, false, counter);
                            }
                            buffer.Close(TextStyle.SystemTargets_Sub_Lines);
                        }
                        //if ( count == 0 )
                            //buffer.Add( "None" );
                        //buffer.Add( "\nIn sim planning group " ).Add( Squad.GetFactionTargetPlanningGroupSafe() );
                    }
                    #endregion
                    
                    #endregion

                    buffer.EndStatement(TextStyle.System_Line);
                }
                #endregion
                else
                #region ModuleStatAdjuster
                if ( Type.ModuleStatAdjuster == ModularStatAdjustment.HullHealth ||
                     Type.ModuleStatAdjuster == ModularStatAdjustment.ShieldHealth )
                {
                    debugStage = 300;
                    
                    var term = TextTerm.Hull;
                    if ( Type.ModuleStatAdjuster == ModularStatAdjustment.ShieldHealth )
                        term = TextTerm.Shields;
                        
                    var use = TermUse.Icon;
                    if (detailLevel >= TooltipDetail.Medium)
                        use = TermUse.Icon_Name;

                    //if ( detailLevel < TooltipDetail.Full )
                        buffer.AddMultiplier( Type.ModuleStatAdjusterMultiplier, term, use );
                }
                #endregion
                else
                #region Tachyon
                if ( Type.TachyonHitsAlbedoLessThan > FInt.Zero || 
                     Type.TachyonHitsAlbedoMoreThan > FInt.Zero )
                {
                    debugStage = 400;
                    
                    buffer.BeginStatement(Line_Style);
                    
                    float secondsPerSimFrame = World_AIW2.Instance.SimulationProfile.SecondsPerFrameNonSim;
                    float simFrameMultiplier = 1f / secondsPerSimFrame;
                    
                    if ( detailLevel < TooltipDetail.Full )
                    {
                        buffer.AddModuleTag(System).Add("反隐形",TextStyle.System_Label).Add("：范围 <color=#ffdf72>" );
                        buffer.AddNumberMoreReadable( ForMark.TachyonRange );
                        buffer.Add( "</color>, strength <color=#ffdf72>" );
                        //we use simFrameMultiplier to get the actual number per second, rather than the number per frame
                        buffer.AddNumberMoreReadable( Mathf.RoundToInt( ForMark.TachyonPoints * simFrameMultiplier ) );
                        buffer.Add( "</color>, " );

                        WriteMinMaxOrVariant( buffer, "any albedo", "only albedo", true, "ffdf72", Type.TachyonHitsAlbedoMoreThan, Type.TachyonHitsAlbedoLessThan );
                    }
                    else
                    {
                        buffer.AddModuleTag(System).Add("反隐形",TextStyle.System_Label).Add("：范围 <color=#ffdf72>" );
                        buffer.AddNumberMoreReadable( ForMark.TachyonRange );
                        buffer.Add( "</color> 内的隐形敌舰被速子场饱和，每秒消耗 <color=#ffdf72>" );
                        //we use simFrameMultiplier to get the actual number per second, rather than the number per frame
                        buffer.AddNumberMoreReadable( Mathf.RoundToInt( ForMark.TachyonPoints * simFrameMultiplier ) );
                        buffer.Add( "</color> 隐形点数，来自 " );

                        WriteMinMaxOrVariant( buffer, "any albedo", "an albedo", true, "ffdf72", Type.TachyonHitsAlbedoMoreThan, Type.TachyonHitsAlbedoLessThan );
                    }
                    if ( Type.CareAboutStateOfMatterToBeEnabled )
                        WriteSystemStateOfMatterSuffix( buffer, Type, Squad );
                    
                    buffer.EndStatement(Line_Style);
                    //buffer.Add(system_line1.CloseTags);
                }
                #endregion
                else
                #region CloakingPoints
                if ( ForMark.CloakingPoints > 0 )
                {
                    debugStage = 500;
                    
                    FInt maxWeaponCloakingReductionCost = FInt.Zero;
                    for ( int i = 0; i < Type.ParentEntityTypeData.SystemTypes.Count; i++ )
                    {
                        EntitySystemTypeData otherSystem = Type.ParentEntityTypeData.SystemTypes[i];
                        if ( otherSystem.MaxMarkLevelToFunction < effectiveMarkLevel || otherSystem.MinMarkLevelToFunction > effectiveMarkLevel || otherSystem.SystemIsHiddenForUI )
                            continue;
                        
                        if ( otherSystem.CareAboutStateOfMatterToBeEnabled && otherSystem.IsInvisibleInTooltipsIfNotAMatchByStateOfMatter )
                        {
                            if ( !Squad.IsFakeEntity && Squad.CurrentStateOfMatter != otherSystem.MustBeThisStateOfMatterToBeEnabled )
                                continue; //skip completely, since this will be invisible AND disabled
                        }
                        if ( otherSystem.IsModule && !otherSystem.IsModuleOn( relatedMembershipOrNull ) )
                            continue;
                        
                        if ( otherSystem.Category == EntitySystemCategory.Weapon )
                        {
                            if ( maxWeaponCloakingReductionCost < otherSystem.CloakingPercentLossFromFiring )
                                maxWeaponCloakingReductionCost = otherSystem.CloakingPercentLossFromFiring;
                        }
                    }

                    Color cloakColor = ColorMath.PaleVioletRed;
                    
                    if (Squad.IsFakeEntity)
                    {
                        buffer.BeginStatement(Line_Style);
                        buffer
                            .AddModuleTag(System).Add( "Cloaking", TextStyle.System_Label )
                            .Add( ": Cannot be targeted while points remain (max " )
                            .WrapCloak(ForMark.CloakingPoints, false, false).Add(").");
                        
                        if ( Type.CareAboutStateOfMatterToBeEnabled )
                            WriteSystemStateOfMatterSuffix( buffer, Type, Squad );
                        buffer.EndStatement(Line_Style);
                    }
                    
                    /*
                        buffer.Add("Details", TextStyle.System_Line2).Add(": ")
                        buffer.AddNumberMoreReadable( ForMark.CloakingPoints );
                        buffer.Add( "</color> max cloaking points" ).EndStatement(EndStatementStyle.Normal);
                        
                        if ( detailLevel > TooltipDetail.Medium)
                        {
                            buffer.Add("As long as its current cloaking points are above zero, it will be invisible to enemies (although detected)" ).EndStatement(EndStatementStyle.Normal);

                            if ( Type.ParentEntityTypeData.IsCombatant )
                            {
                                buffer
                                    .Add( "Every time this ship fires, it will expend " )
                                    .Add( Mathf.RoundToInt( maxWeaponCloakingReductionCost.ToFloatNonSim() * 100f ) )
                                    .Add( "% of its cloaking points" ).EndStatement(EndStatementStyle.Normal);
                            }
                            
                            if ( Squad.TypeData.OnlyCloakedWhenOwningPlanet )
                                buffer.Add( "This unit only stays cloaked if its faction owns its planet" ).EndStatement(EndStatementStyle.Normal);

                            buffer.Add( "After " ).Add( ExternalConstants.Instance.SecondsToWaitBeforeRecloaking ).Add( " seconds without losing any, this ship will regain all points" ).EndStatement(EndStatementStyle.Normal);
                        }
                    }
                    */
                    
                    
                    //buffer.Add(system_line1.CloseTags);
                }
                #endregion
                else
                #region TractorCount
                if ( ForMark.TractorCount > 0 )
                {
                    buffer.BeginStatement(TextStyle.System_Line);
                    debugStage = 600;
                    
                    if ( Type.IsReverseTractorBeam )
                        buffer.AddModuleTag(System).Add("抓钩光束",TextStyle.System_Label).Add("：");
                    else
                        buffer.AddModuleTag(System).Add("牵引光束",TextStyle.System_Label).Add("：");
                    
                    buffer.Open(TextStyle.Color_Count).Add(ForMark.TractorCount).Add( Text.Count ).Close(TextStyle.Color_Count).Add(" 光束");
                    buffer.Add(" | ",TextStyle.Color_Gray).AddNumber(ForMark.TractorRange, TextTerm.Range, TermUse.Name);
                    if (Type.TractorAlbedoRange.IsSet || 
                        Type.TractorAlbedoRange.IsSet || 
                        Type.TractorAlbedoRange.IsSet)
                    {
                        buffer.Add(" | ",TextStyle.Color_Gray).Add("如果 ", TextStyle.JustBold);
                        int counter=0;
                        if (Type.TractorAlbedoRange.IsSet) { buffer.AddNumberRange(Type.TractorAlbedoRange, TextTerm.Albedo, TermUse.Icon); counter++; }
                        if (Type.TractorEngineRange.IsSet) { if (counter > 0) {buffer.Add(" "); counter--;} buffer.AddNumberRange(Type.TractorEngineRange, TextTerm.Engine_gX, TermUse.Icon); counter++; }
                        if (Type.TractorMassRange.IsSet) { if (counter > 0) {buffer.Add(" "); counter--;} buffer.AddNumberRange(Type.TractorMassRange, TextTerm.Mass_tX, TermUse.Icon); counter++; }
                    }
                    if (detailLevel > TooltipDetail.Medium)
                    {
                        buffer.Open(TextStyle.System_Sub_Lines);
                        buffer.Open(TextStyle.System_Line2);
                        if (Type.IsReverseTractorBeam)
                            buffer.Add("抓住一艘敌舰，本舰随之移动。敌舰无法离开本星球。本舰所有武器均视为在抓取目标射程内。目标被视为被牵引以触发增益。");
                        else
                            buffer.Add("将敌舰固定原地，使其完全无法移动。被牵引的舰船始终可以攻击牵引源（反之则不一定）。");
                        buffer.Close(TextStyle.System_Line2);
                        buffer.Close(TextStyle.System_Sub_Lines);
                    }
                   
                    if ( Type.CareAboutStateOfMatterToBeEnabled )
                        WriteSystemStateOfMatterSuffix( buffer, Type, Squad );
                    
                    buffer.EndStatement(TextStyle.System_Line);
                }
                #endregion
                else
                #region Gravity
                if ( Type.GravityHitsEngine_gxLessThan > 0 )
                {
                    debugStage = 700;
                    buffer.BeginStatement(Line_Style);
                    
                    var system = this.System;
                    
                    buffer.AddModuleTag(System).Add("重力场",TextStyle.System_Label).Add("：");

                    buffer
                        .Add(" 减速敌人 ")
                        .AddNumber( Type.GravityHitsEngine_gxLessThan, " ≤", TextTerm.Engine_gX, TermUse.Icon )
                        .Add(" ")
                        .AddNumber(
                            ()=> buffer.AddMultiplier(system.DataForMark.GravitySpeedMultiplier, TextStyle.Empty),
                            null, TextTerm.Speed, TermUse.Icon, null)
                        .Add(" 在 ");
                        
                    if (ForMark.GravityRange > 99999)
                        buffer.AddNumber(int.MaxValue, TextTerm.Range, TermUse.Name);
                    else
                        buffer.AddNumber(ForMark.GravityRange, TextTerm.Range, TermUse.Name);
                    
                    buffer.Add(".");
                    
                    if ( Type.CareAboutStateOfMatterToBeEnabled )
                        WriteSystemStateOfMatterSuffix( buffer, Type, Squad );
                    
                    buffer.EndStatement(Line_Style);
                }
                #endregion
                else
                #region Attract
                if ( ForMark.AttractRangeForShotsAgainstAllies > 0 )
                {
                    debugStage = 800;
                    buffer.BeginStatement(Line_Style);
                    
                    buffer
                        .AddModuleTag(System).Add("引力场",Label_Style)
                        .Add("：针对友军的射击被自动偏转。" )
                        .AddNumber( ForMark.AttractRangeForShotsAgainstAllies )
                        .Add( "</color> are automatically redirected at this unit, instead." );
                    
                    if ( Type.CareAboutStateOfMatterToBeEnabled )
                        WriteSystemStateOfMatterSuffix( buffer, Type, Squad );
                    
                    buffer.EndStatement(Line_Style);
                }
                #endregion
            }
            catch ( System.Threading.ThreadAbortException )
            {
                // Spurious abort during shutdown; let it propagate without noise.
            }
            catch ( Exception ex)
            {
                LOG.Err("Error in {0}() for system {1} at debugstage {2}; exception:\n{3}", this.TypeNameAndMethod(), (this.System?.TypeData?.InternalName_Longer).OrNull(), debugStage, ex);
            }
        }

        public bool EvalCondition( string name, object _args )
        {
            int debugstage = 0;
            try
            {
                if (name == "IsReal")
                {
                    return !this.Squad.IsFakeEntity;
                }
                
                if (name == "IsModule")
                {
                    return this.System.TypeData.IsModule;
                }
                
                if (name == "ShowActivity")
                {
                    if ( Config.WeaponDetail > WeaponActivityDetail.Nothing &&
                         !Squad.IsFakeEntity && 
                         !Config.ForMultipleShips && 
                         Squad.GetMetalToClaimRemaining() == 0 &&
                         !Squad.GetIsCrippled() &&
                         !Squad.GetIsRemains() &&
                         Squad.Planet.GetDoHumansHaveVision() &&
                         (Squad.GetIsPlayerUnit() || Config.ShowDebugInfo) )
                    {
                        return true;
                    }
                    
                    return false;
                }
                
                if (name == "ShowLastDamage")
                {
                    if ( !IsRetaliatory && 
                         !IsOnDeath && 
                         System.LastGameSecondMyShotHit > 0 &&
                         ( this.Config.WeaponDetail == WeaponActivityDetail.LastDamage || 
                           this.Config.WeaponDetail == WeaponActivityDetail.Both ) )
                    {
                        return true;
                    }
                    
                    return false;
                }
                
                if (name == "ShowLastAbortCode")
                {
                    if ( System.LastDamageAbortCode > 0 &&
                         ( this.Config.ShowAbortCode || Config.ShowDebugInfo ) )
                    {
                        return true;
                    }
                    
                    return false;
                }
                
                if (name == "IsCounting")
                {
                    if ( !IsRetaliatory && 
                         !IsOnDeath && 
                         IsReloading &&
                         ( this.Config.WeaponDetail == WeaponActivityDetail.ReloadTime || 
                           this.Config.WeaponDetail == WeaponActivityDetail.Both ) )
                    {
                        //LOG.Msg("{0}.IsCounting = true; System.TimeUntilNextShot={1}", System.ToString(), System.TimeUntilNextShot.ToRounded(1));
                        return true;
                    }
                    
                    return false;
                }
                
                if (name == "IsMultiShot")
                {
                    return this.ForMark.ShotsPerSalvo > 1;
                }
                
                if (name == "IsLimited")
                {
                    if (this.ForMark.ShotsPerTarget < this.ForMark.ShotsPerSalvo &&
                        this.ForMark.ShotsPerTarget > 1)
                    {
                        return true;
                    }
                    
                    return false;
                }
                
                if (name == "IsSpread")
                {
                    return this.ForMark.ShotsPerTarget == 1;
                }
                
                LOG.Msg("Warning: Unhandled condition '{0}'", name);
            }
            catch (Exception e)
            {
                LOG.Err("at debugstage {0}\n{1}", debugstage, e);
            }
            
            return false;
        }
        
        public void AppendVar_Weapon( string name, TextStyle style, ArcenCharacterBufferBase buffer, object _args )
        {
            int debugstage = 0;
            try
            {
                debugstage = 10;
                if (name.Equals("name"))
                {
                    debugstage = 100;
                    buffer.Add( this.Type.GetDisplayName() );

                    return;
                }
                
                if (name.Equals("output"))
                {
                    debugstage = 200;
                    
                    var shot_type = this.System.TypeData.ShotTypeData;
                    
                    var dps_min = this.System.GetSalvoDps_Min().ToFloat();
                    var dps_max = this.System.GetSalvoDps_Max().ToFloat();
                    
                    var shot_damage = (float)this.System.GetShotDamage();
                    var shot_damage_corrosive = (float)this.System.GetShotDamage_OfCorrosion();
                    
                    var sec_per_shot_min = shot_damage / dps_min;
                    var sec_per_shot_max = shot_damage / dps_max;
                    
                    float time_interval = sec_per_shot_max;
                    if (sec_per_shot_min > sec_per_shot_max)
                        time_interval = sec_per_shot_min;

                    var count_min = (int)Math.Round(1.0f / sec_per_shot_min * time_interval,0);
                    var count_max = (int)Math.Round(1.0f / sec_per_shot_max * time_interval,0);
                    
                    //if ( Type.CanDevour )
                    //{
                    //    buffer.WrapDamage( Type.DevourFunctionName, false, false ).Add(" if ").Open(TextTerm.Mass_tX, TermUse.Icon).Add("鈮?).Close(TextTerm.Mass_tX);
                    //}
                    //else 
                    if ( Type.IonDamageToAlbedoLessThan > FInt.Zero )
                    {
                        buffer
                            .Open(TextStyle.Ion)
                            .Add( Type.IonPercentagePerMarkLevelLower * FInt.OneHundred )
                            .Add( "% Ion Damage" ).Close(TextStyle.Ion);
                        
                        buffer.Add(" | ",TextStyle.Color_Gray).Add("如果 ", TextStyle.JustBold);
                        buffer.AddNumberRange(Type.IonAlbedoRange, TextTerm.Albedo, TermUse.Icon);
                    }
                    else 
                    if ( IsMirrorShot )
                    {
                        buffer
                            //.Add("Returns ")
                            .Open(TextTerm.Damage_Exotic, TermUse.Icon)
                            .Add( Type.ReturnsThisPercentageOfDamageWhenFiringRetaliatoryShot * FInt.OneHundred )
                            .Add( "%" ).Close(TextTerm.Damage_Exotic)
                            .Add(" 的 ").Add(TextTerm.Damage, TermUse.Name)
                            .Add( " on " ).WrapReload( "Hit", false, false );
                    }
                    else
                    if ( Type.OverdrivesShields )
                    {
                        buffer.Add( "100%", TextTerm.Damage, TermUse.Icon).Add(" ").Add("目标 ", TextTerm.Shields, TermUse.IconAfter);
                    }
                    else
                    if ( IsDroneGun )
                    {
                        (buffer.UserObj as EntityTextWriter).WriteSpawn(shot_type);
                        
                        buffer.Add(" ").Open(TextStyle.Color_Count).Add(Text.Multiply).Add(count_min);
                        if (dps_min != dps_max)
                            buffer.Add(" 禄 ").Add(count_max);
                        buffer.Close(TextStyle.Color_Count);
                        
                        buffer.Add(" ").Open(TextStyle.Number_Units).Open(TextTerm.Reload, TermUse.Color);
                        buffer.Add("/");
                        if (time_interval != 1)
                            buffer.Add((float)Math.Round(time_interval, 1));
                        buffer.Add("秒");
                        buffer.Close(TextTerm.Reload).Close(TextStyle.Number_Units);
                    }
                    else
                    if ( IsOnDeath )
                    {
                        buffer.Open( TextTerm.Damage, TermUse.Icon, TextStyle.Empty );
                        buffer.AddNumber( dps_min, null, TextStyle.Empty );
                        if (dps_min != dps_max)
                            buffer.Add(" 禄 ").AddNumber( dps_max, null, TextStyle.Empty );
                        buffer.Close( TextTerm.Damage );
                        
                        buffer.Add( " on " ).WrapReload( "Death", false, false );
                    }
                    else
                    if ( IsRetaliatory )
                    {
                        buffer.Open( TextTerm.Damage, TermUse.Icon, TextStyle.Empty );
                        buffer.AddNumber( dps_min, null, TextStyle.Empty );
                        if (dps_min != dps_max)
                            buffer.Add(" 禄 ").AddNumber( dps_max, null, TextStyle.Empty );
                        buffer.Close( TextTerm.Damage );
                        
                        buffer.Add( " on " ).WrapReload( "Hit", false, false );
                    }
                    else
                    {
                        buffer.Open( TextTerm.DPS, TermUse.Icon, TextStyle.Empty );
                        if (dps_min == dps_max)
                            buffer.AddNumber( dps_min, null, TextStyle.Empty ).Add(" /sec", TextStyle.Fraction_Gray);
                        else
                            buffer.AddNumber( dps_min, null, TextStyle.Empty ).Add(" 禄 ").AddNumber( dps_max, null, TextStyle.Empty ).Add(" /sec", TextStyle.Fraction_Gray);
                        buffer.Close( TextTerm.DPS );
                    }
                    
                    if (shot_damage_corrosive > 0)
                    {
                        var perc = (int)(shot_damage / shot_damage_corrosive * 100);
                        
                        buffer.Add(" | ", TextStyle.Color_Gray);
                        buffer.Open(TextTerm.Damage_Corrosive, TermUse.Icon);
                        buffer.Add(perc).Add("% 腐蚀");
                        buffer.Close(TextTerm.Damage_Corrosive);
                    }
                        
                    if ( Type.CanDevour )
                    {
                        buffer.Add(" | ", TextStyle.Color_Gray);
                        buffer.Add( "Devour", TextStyle.Color_Devour );
                    }
                    
                    if ( Type.CanInfest )
                    {
                        buffer.Add(" | ", TextStyle.Color_Gray);
                        buffer.Add( "Infest", TextStyle.Color_Infest );
                    }

                    return;
                }
                
                if (name.Equals("range"))
                {
                    debugstage = 300;
                    
                    if ( IsRetaliatory || IsOnDeath )
                    {
                         if ( System.GetShotAreaOfEffect() < 1 )   
                             return;
                    }
                    
                    buffer.Add(" | ", TextStyle.Color_Gray);
                    
                    if ( Type.IsMelee )
                    {
                        buffer.AddNumber( "Melee", TextTerm.Range, TermUse.Name );
                    }
                    else
                    {
                        var num = this.ForMark.CalculateActualRange(this.Squad);
                        if (num > 600000)
                            num = int.MaxValue;
                        
                        buffer.AddNumber(num, TextTerm.Range, TermUse.Name);
                    }

                    return;
                }
                
                if (name.Equals("damage"))
                {
                    debugstage = 400;
                    
                    buffer.AddNumber(this.System.GetShotDamage(), TextTerm.Damage, TermUse.Icon_Name_Abbr);

                    return;
                }
                
                if (name.Equals("shots"))
                {
                    debugstage = 500;
                    
                    buffer.Add(this.ForMark.ShotsPerSalvo);
                    return;
                    
                    #region unused
                    /*
                    var shotType = this.System.TypeData.ShotTypeData;
                
                    //if (alt)
                    //{
                    //    this.System.DataForMark.ShotsPerSalvo
                    //}
                    
                    if (shotType.Category == GameEntityCategory.Shot)
                    {
                        buffer.AddNumber(this.ForMark.ShotsPerSalvo, TextTerm.Shots, TermUse.Name_Abbr);
                        return;
                    }
                    
                    if (shotType.Category == GameEntityCategory.Ship)
                    {
                        var reload1 = this.System.DataForMark.SecondsPerSalvo;
                        var reload2 = this.System.DataForMark.AltSecondsPerSalvo;//this.System.GetWeaponReloadTime(false);
                        var numSalvos1 = this.System.TypeData.UseAlternateRateOfFireAfterXShots;
                        var numSlavos2 = this.System.TypeData.UseAlternateRateOfFireForXShotsBeforeReverting;
                        var shotsPerSalvo = this.System.DataForMark.ShotsPerSalvo;
                        
                        //var shotsPerSalvo2 = this.System.DataForMark.A;
                        Faction facOrNull = Squad.GetFactionOrNull_Safe();
                        byte effectiveMarkLevel = Squad.CurrentMarkLevel;
            
                        GameEntityTypeData spawnType = Type.ShotTypeData;
                        byte subMark = effectiveMarkLevel;
                        if ( subMark > spawnType.MaxMarkLevel )
                            subMark = spawnType.MaxMarkLevel;
                        if ( subMark < spawnType.BaseMark.MarkLevel.Ordinal )
                            subMark = spawnType.BaseMark.MarkLevel.Ordinal;
                        Faction toDisplayFor = facOrNull;
                        if ( toDisplayFor == null )
                            toDisplayFor = spawnType.EncyclopediaOnly_LastFactionForColor.Display;
                     
                        new ShipForDisplay(facOrNull, subMark, shotType, shotsPerSalvo, this.Config).Write(buffer, TextVarMap.Inline_Ship_Format);

                        return;
                    }
                    */
                    #endregion
                }

                #region unused
                /*
                if (name.Equals("Shots"))
                {
                    debugstage = 500;
                    
                    buffer.AddNumber(this.ForMark.ShotsPerSalvo, TextTerm.Shots, TermUse.Name_Abbr);

                    return;
                }
                */
                #endregion
                
                if ( name.Equals("reload"))
                {
                    debugstage = 600;
                    buffer.AddMinutesAndSeconds(this.System.DataForMark.SecondsPerSalvo);
                    return;
                    #region unused
                    /*
                    if (alt)
                    {
                        if (System.DataForMark.AltSecondsPerSalvo > 0)
                        {
                            buffer.AddNumber(System.DataForMark.AltSecondsPerSalvo, TextTerm.Reload, TermUse.Name_Abbr);
                        }
                        
                        return;
                    }

                    buffer.AddNumber(this.System.DataForMark.SecondsPerSalvo, TextTerm.Reload, TermUse.Name_Abbr);

                    //buffer.Add( this.Type.GetDisplayName() );

                    return;
                    */
                    #endregion
                }

                #region Weapon Activity Details
                if ( name.Equals("Status"))
                {
                    debugstage = 700;
                    if ( System.TimeUntilNextShot <= FInt.Zero )
                        buffer.Add("就绪", TextStyle.CustomSystem_SystemStatus_Ready);
                    else
                        buffer.Add("装填中", TextStyle.CustomSystem_SystemStatus_Cooldown);
                    
                    return;
                }
                
                if ( name.Equals("Activity"))
                {
                    debugstage = 800;
                    var time = System.TimeUntilNextShot.ToRounded(1).ToFloat();
                    var timerColor = time.TimeToColor(true);
                    buffer.AddMinutesAndSeconds(time, timerColor.GetHexCode(), true);

                    return;
                }
                
                if (name.Equals("LastDamage"))
                {
                    debugstage = 900;
                    buffer.AddNumber( System.LastTotalDamageMyShotDidCaused, TextTerm.Damage, TermUse.Icon );

                    return;
                }
                
                if (name.Equals("LastAbortCode"))
                {
                    debugstage = 1000;
                    buffer.Add(Extensions.ToString(System.LastDamageAbortCode));

                    return;
                }
                
                #endregion
            }
            catch (Exception ex)
            {
                LOG.Err("Error in {0}() at debugstage={1}; exception:\n{2}", this.TypeNameAndMethod(), debugstage, ex);
            }
        }
    }
    
    public static partial class EntityText
    {
        /// <summary>
        /// Enumerate and add to the passed collection, all systems of 'e' that should be displayed/visible to the player.
        /// If arg 'systems' is the only non-null collection passed,
        ///   all results are added to it.
        /// If arg 'system_mods' is passed,
        ///   all systems that are mods will be only added to it, and removed from 'systems'.
        /// If arg 'stat_mods' is passed,
        ///   all stat-modules will only be added to it, and they will be removed from 'system_mods' and 'systems'.
        /// </summary>
        public static void EnumerateSystems( GameEntity_Squad e, EntitySystemCollection systems, EntitySystemCollection system_mods, EntitySystemCollection stat_mods )
        {
            if (e.Systems == null)
                return;
            
            for (int i = 0; i < e.Systems.Count; i++)
            {
                var sys = e.Systems[i];
                if (sys == null)
                {
                    LOG.Msg("Warning: {0}.Systems[{1}] is null in {2}.", e.ToString(), i, "EntityText.EnumerateSystems");
                    continue;
                }
                var type = sys.TypeData;
                if (type == null)
                {
                    LOG.Msg("Warning: {0}.Systems[{1}] has null TypeData in {2}.", e.ToString(), i, "EntityText.EnumerateSystems");
                    continue;
                }
                
                var forMark = sys.DataForMark;
                if (forMark == null)
                {
                    LOG.Msg("Warning: {0}.Systems[{1}] ({2}) has null DataForMark in {3}.", e.ToString(), i, type.InternalName_Original, "EntityText.EnumerateSystems");
                    continue;
                }
                
                var mark = e.CurrentMarkLevel;
                
                if ( type.MaxMarkLevelToFunction < mark || 
                     type.MinMarkLevelToFunction > mark || 
                     type.SystemIsHiddenForUI )
                    continue;
                
                if ( type.CareAboutStateOfMatterToBeEnabled && 
                     type.IsInvisibleInTooltipsIfNotAMatchByStateOfMatter &&
                     e.CurrentStateOfMatter != type.MustBeThisStateOfMatterToBeEnabled )
                {
                    //skip completely, since this will be invisible AND disabled
                    continue; 
                }

                if (!e.IsFakeEntity)
                {
                    if ( type.IsModule && 
                         !type.IsModuleOn( e.FleetMembership ) )
                        continue;
                }
                
                if (sys.CustomSystem != null)
                {
                    sys.SortOrder = 1000;
                }
                else
                if (sys.TypeData.Category == EntitySystemCategory.Weapon)
                {
                    sys.SortOrder = 1100;
                }
                else
                if (forMark.CloakingPoints > 0)
                {
                    sys.SortOrder = 1200;
                    systems.Cloaking = sys;
                }
                else
                if (forMark.TachyonPoints > 0)
                {
                    sys.SortOrder = 1300;
                    systems.Tachyon = sys;
                }
                else
                if (forMark.TractorCount > 0)
                {
                    sys.SortOrder = 1400;
                    systems.Tractor = sys;
                }
                else
                if (forMark.GravityRange > 0)
                {
                    sys.SortOrder = 1500;
                    systems.Gravity = sys;
                }
                else
                if (forMark.AttractRangeForShotsAgainstAllies > 0)
                {
                    sys.SortOrder = 1600;
                    systems.Attract = sys;
                }
                else
                if (type.ModuleStatAdjuster == ModularStatAdjustment.BubbleForcefield)
                {
                    //sys.SortOrder = 1700;
                    continue;
                }
                else
                if (type.ModuleStatAdjuster == ModularStatAdjustment.ShieldHealth)
                {
                    sys.SortOrder = 1710;
                }
                else
                if (sys.TypeData.ModuleStatAdjuster == ModularStatAdjustment.HullHealth)
                {
                    sys.SortOrder = 1720;
                }
                
                // modular systems below non-modular of same type
                if (type.IsModule)
                {
                    sys.SortOrder += 50;
                }
                
                
                if (type.IsModule)
                {
                    if (stat_mods != null)
                    {
                        if (type.ModuleStatAdjuster != ModularStatAdjustment.None)
                        {
                            stat_mods.Add(sys);
                            continue;
                        }
                    }
                    
                    if (system_mods != null)
                    {
                        system_mods.Add(sys);
                        continue;
                    }
                }
                
                systems.Add(sys);
            }

            int compare(EntitySystem a, EntitySystem b)
            {
                int dif = a.SortOrder.CompareTo(b.SortOrder);
                if (dif == 0)
                    dif = a.TypeData.InternalName_Original.CompareTo(b.TypeData.InternalName_Original);
                return dif;
            }
            
            systems.StableSort( compare );
            stat_mods.StableSort( compare );
        }
    }
}
        