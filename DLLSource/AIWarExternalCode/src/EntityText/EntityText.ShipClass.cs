
using System;
using Arcen.Universal;
using Arcen.AIW2.Core;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public struct ShipClassText
    {
        public readonly GameEntity_Squad Squad;
        public readonly ShipClassData ShipClass;
        public readonly EntityText.Config Config;
        
        public ShipClassText(GameEntity_Squad squad, EntityText.Config config)
        {
            Squad = squad;
            Config = config;
            ShipClass = squad.TypeData.ShipClass;
        }
        
        public void Write(ArcenCharacterBufferBase buffer)
        {
            StructList<ShipClassData_ModifierData> list = null;
            
            try
            {
                if (ShipClass.IsDefault || ShipClass.IsHidden || ShipClass.IsNoneRow || !ShipClass.RequiresTooltipAtAll)
                    return;
                
                var block = TextStyle.Get("ShipClass_Block");
                var padding = TextStyle.Get("ShipClass_Block_Spacing");
                var line = TextStyle.Get("ShipClass_Line");
                var greater_cat = TextStyle.Get("ShipClass_Greater_Cat");
                var lesser_cat = TextStyle.Get("ShipClass_Lesser_Cat");
                
                list = StructList<ShipClassData_ModifierData>.Get();
                FillModifierList(list);
            }
            finally
            {
                list?.Return();
            }
        }
        
        private void FillModifierList(StructList<ShipClassData_ModifierData> list)
        {
            ShipClassData_ModifierData data;

            if ( !ShipClass.CanReceiveDebuffs )
            {
                //WriteShipClass_GreaterCategoryStartIfNeeded( Buffer, ShipClass, ref AlreadyWroteGeneralStart, ref AlreadyWroteAbsoluteStart, DetailLevel, GreaterCategory );
                //WriteShipClass_LesserCategoryStartIfNeeded( Buffer, ref AlreadyWroteLesserStart, DetailLevel, GreaterCategory, ShipClassData_LesserCategoryType.Debuff, true );
                //if ( DetailLevel == TooltipDetail.Full )
                //    Buffer.Add( "All Debuffs" );
                //else
                //    Buffer.Add( "Debuffs" );
            } 
            
            if ( ShipClass.HasAnyDebuffModifiers )
            {
                for (int i = 0; i < ShipClass.DebuffModifiers.Length; i++)
                {
                    data = ShipClass.DebuffModifiers[0];
                    list.Add(data);
                }
                
                data = ShipClass.GraviticCoreModifier;
                if ( Squad.DataForMark.Speed > 0 &&
                     data.ModifierType != ShipClassData_ModifierData_Type.NoModifier &&
                     data.ModifierType != ShipClassData_ModifierData_Type.Immunity )
                {
                    list.Add(data);
                }
            }
            
            if ( !ShipClass.CanReceiveDeathEffects )
            {
                //WriteShipClass_GreaterCategoryStartIfNeeded( Buffer, ShipClass, ref AlreadyWroteGeneralStart, ref AlreadyWroteAbsoluteStart, DetailLevel, GreaterCategory );
                //WriteShipClass_LesserCategoryStartIfNeeded( Buffer, ref AlreadyWroteLesserStart, DetailLevel, GreaterCategory, ShipClassData_LesserCategoryType.DeathEffect, true );
                //if ( DetailLevel == TooltipDetail.Full )
                //    Buffer.Add( "All Death Effects" );
                //else
                //    Buffer.Add( "Death Effects" );
            } 
            /* Since a *lot* of things have zombification immunity being all they use from the ShipClass this is not displayed here but in the main tooltip of the unit
             * Less visual clutter this way

            else if ( !ShipClass.CanBeZombifiedAndSimilar && GreaterCategory == ShipClassData_ModifierData_Type.Immunity )
            {
                //Do not individually list zombification-type death effects for immunity, just that one info is sufficient
                WriteShipClass_GreaterCategoryStartIfNeeded( Buffer, ref AlreadyWroteGeneralStart, DetailLevel, GreaterCategory );
                WriteShipClass_LesserCategoryStartIfNeeded( Buffer, ref AlreadyWroteLesserStart, DetailLevel, GreaterCategory, ShipClassData_LesserCategoryType.DeathEffect, false );
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( "所有亡灵化类型" );
                else
                    Buffer.Add( "亡灵化类型" );
            }*/
            
            /*
            else
            if ( ShipClass.HasAnyDeathEffectModifiers )
            {
                DeathEffectType deathEffect;
                for ( int i = 0; i < ShipClass.DeathEffectModifiers.Length; i++ )
                {
                    data = ShipClass.DeathEffectModifiers[i];
                    deathEffect = DeathEffectTypeTable.Instance.Rows[i];

                    list.Add(data);
                    
                    if ( deathEffect.IsSomeKindOfNormalZombification && EntityBaseSpeed <= 0 )
                    {
                        //TODO Death Effect short names
                        WriteShipClass_ModifierData( Buffer, ShipClass, deathEffect.InternalName, deathEffect.InternalName, data, ShipClassData_ModifiedUnit.None, DetailLevel,
                            ref AlreadyWroteLesserStart, ref AlreadyWroteAbsoluteStart, ref AlreadyWroteGeneralStart, GreaterCategory,ShipClassData_LesserCategoryType.DeathEffect, false );
                    }
                }
            }
            */

            if ( !ShipClass.CanBeDamaged )
            {
                //if ( GreaterCategory == ShipClassData_ModifierData_Type.Immunity )
                //{
                //    WriteShipClass_GreaterCategoryStartIfNeeded( Buffer, ShipClass, ref AlreadyWroteGeneralStart, ref AlreadyWroteAbsoluteStart, DetailLevel, GreaterCategory );
                //    WriteShipClass_LesserCategoryStartIfNeeded( Buffer, ref AlreadyWroteLesserStart, DetailLevel, GreaterCategory, ShipClassData_LesserCategoryType.General伤害，true );
                //    if ( DetailLevel == TooltipDetail.Full )
                //        Buffer.Add( "Immune to all damage" );
                //    else
                //        Buffer.Add( "Invulnerable" );
                //}
            } 
            
            
            data = ShipClass.NonExoticDamageModifier;
            if ( data.ModifierType != ShipClassData_ModifierData_Type.NoModifier )
            {
                list.Add(data);
                //WriteShipClass_ModifierData( Buffer, ShipClass, "All Shots", "All", ShipClass.NonExoticDamageModifier, ShipClassData_ModifiedUnit.None, DetailLevel,
                //    ref AlreadyWroteLesserStart, ref AlreadyWroteAbsoluteStart, ref AlreadyWroteGeneralStart, GreaterCategory,ShipClassData_LesserCategoryType.AmmoType, false );
            }
            
            if ( ShipClass.HasAnyAmmoDamageModifiers )
            {
                for ( int i = 0; i < ShipClass.AmmoDamageModifiers.Length; i++ )
                {
                    data = ShipClass.AmmoDamageModifiers[i];
                    AmmoTypeData ammo = AmmoTypeDataTable.Instance.Rows[i];
                    
                    if ( data.ModifierType != ShipClassData_ModifierData_Type.NoModifier )
                    {
                        list.Add(data);
                    }
                }
            }

            data = ShipClass.AllExoticDamageModifier;
            if ( data.ModifierType != ShipClassData_ModifierData_Type.NoModifier )
            {
                list.Add(data);
                //WriteShipClass_ModifierData( Buffer, ShipClass, "All Exotic Damage", "Exotic Damage", ShipClass.AllExoticDamageModifier, ShipClassData_ModifiedUnit.None,
                    //DetailLevel, ref AlreadyWroteLesserStart, ref AlreadyWroteAbsoluteStart, ref AlreadyWroteGeneralStart, GreaterCategory, ShipClassData_LesserCategoryType.Exotic伤害，true );
            } 
             
            if ( ShipClass.HasAnyExoticDamageModifiers )
            {
                for ( int i = 0; i < ShipClass.ExoticDamageModifiers.Length; i++ )
                {
                    data = ShipClass.ExoticDamageModifiers[i];

                    if ( data.ModifierType != ShipClassData_ModifierData_Type.NoModifier )
                    {
                        list.Add(data);
                    }
                }
            }

            /*
            if (GreaterCategory == ShipClassData_ModifierData_Type.Immunity)
            {
                if ( !ShipClass.CanBeSubjectedToSpecialMechanics )
                {
                    WriteShipClass_GreaterCategoryStartIfNeeded( Buffer, ShipClass, ref AlreadyWroteGeneralStart, ref AlreadyWroteAbsoluteStart, DetailLevel, GreaterCategory );
                    WriteShipClass_LesserCategoryStartIfNeeded( Buffer, ref AlreadyWroteLesserStart, DetailLevel, GreaterCategory, ShipClassData_LesserCategoryType.SpecialMechanic, true );
                    if ( DetailLevel == TooltipDetail.Full )
                        Buffer.Add( "牵引光束  黑洞机器  被吞噬  被寄生" );
                    else
                        Buffer.Add( "所有特殊机制" );
                } else
                {
                    if ( !(ShipClass.CanBeTractored && ShipClass.CanBeBlackHoleMachineBlocked && ShipClass.CanBeDevoured && ShipClass.CanBeInfested) )
                    {
                        WriteShipClass_GreaterCategoryStartIfNeeded( Buffer, ShipClass, ref AlreadyWroteGeneralStart, ref AlreadyWroteAbsoluteStart, DetailLevel, GreaterCategory );
                        WriteShipClass_LesserCategoryStartIfNeeded( Buffer, ref AlreadyWroteLesserStart, DetailLevel, GreaterCategory, ShipClassData_LesserCategoryType.SpecialMechanic,
                            false );
                        if ( !ShipClass.CanBeTractored )
                        {
                            if(DetailLevel == TooltipDetail.Full)
                                Buffer.Add( "牵引光束 " );
                            else
                                Buffer.Add( "牵引 " );
                            if ( !ShipClass.CanBeBlackHoleMachineBlocked )
                                Buffer.Add( " " );
                        }
                        if ( !ShipClass.CanBeBlackHoleMachineBlocked )
                        {
                            if ( DetailLevel == TooltipDetail.Full )
                                Buffer.Add( "黑洞机器 " );
                            else
                                Buffer.Add( "黑洞 " );
                            if ( !ShipClass.CanBeDevoured )
                                Buffer.Add( " " );
                        }
                        if ( !ShipClass.CanBeDevoured )
                            if ( DetailLevel == TooltipDetail.Full )
                                Buffer.Add( "被吞噬 " );
                            else
                                Buffer.Add( "吞噬 " );
                        if ( !ShipClass.CanBeInfested )
                            if ( DetailLevel == TooltipDetail.Full )
                                Buffer.Add( "被寄生 " );
                            else
                                Buffer.Add( "寄生 " );
                    }
                }
            }
            */
        }
        
    }
}
