using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class Hacking_IncreaseReconRange : Hacking_HackSelfUnit_Base
    {
        public int GetAddedRangePerHack( GameEntity_Squad Target, HackingType type )
        {
            HackData hackData = this.GetHackDataForTargetOrNull( Target, type );
            if ( hackData == null )
                return 0;
            return hackData.GetCustomData_Int( "AddedRangePerHack" );
        }

        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            int hackingRangeIncrease = GetAddedRangePerHack( Target, type );
            FleetMembership fMem = Target.FleetMembership;
            if ( fMem != null )
            {
                fMem.Hacked_ExtraWatchPlanetsAtXHops += hackingRangeIncrease;
            }
            Target.FlagForForcedFullSyncToClients_FromHost();
            return true;
        }

        public override void AddToDynamicDescription( ArcenDoubleCharacterBuffer buffer, GameEntity_Squad target, GameEntity_Squad hackerOrNull,
            Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            if ( target.TypeData.IsCommandStation )
                buffer.Add( "\n如果你切换此星球上的指挥站类型，范围增加将消失。但如果你切换回此类型，它将再次生效。" );

            FleetMembership targetMem = target.FleetMembership;
            int hopCount = target.TypeData.WatchPlanetsAtXHops + (targetMem == null ? 0 : targetMem.Hacked_ExtraWatchPlanetsAtXHops);

            int hackingRangeIncrease = GetAddedRangePerHack( target, hackingType );
            buffer.Add( "\n现有侦察范围 " ).Add( hopCount ).Add( " 将增加 " ).Add( hackingRangeIncrease );
        }
    }

    public class Hacking_IncreaseHullDurability : Hacking_SelfUnitWithSubSelection_Base
    {
        public FInt GetAddedMultiplierPerHack( GameEntity_Squad Target, HackingType type )
        {
            HackData hackData = this.GetHackDataForTargetOrNull( Target, type );
            if ( hackData == null )
                return FInt.Zero;
            return hackData.GetCustomData_FInt( "AddedMultiplierPerHack" );
        }

        public override bool DoSuccessfulCompletionSubLogic( GameEntity_Squad ChosenTargetOrNull, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            FInt multiplier = this.GetAddedMultiplierPerHack( ChosenTargetOrNull, type );
            FleetMembership fMem = ChosenTargetOrNull?.FleetMembership;
            if ( fMem != null )
            {
                if ( fMem.Hacked_HullHealthMultiplier <= FInt.One )
                    fMem.Hacked_HullHealthMultiplier = multiplier;
                else
                    fMem.Hacked_HullHealthMultiplier *= multiplier;
            }
            else
                return false;

            ChosenTargetOrNull?.FlagForForcedFullSyncToClients_FromHost();
            return true;
        }

        public override SubItemDropdownWriter GetSubItemDropdownWriter()
        {
            return SubDropdownWriter;
        }

        public void SubDropdownWriter( ArcenDoubleCharacterBuffer buffer, GameEntity_Squad CurrentOption, Faction hackerFaction, HackingType hackingType )
        {
            FInt multiplier = this.GetAddedMultiplierPerHack( CurrentOption, hackingType );
            buffer.Add( "<b><size=100%>" );
            buffer.Add( "船体倍率： " );
            if ( multiplier <= FInt.One )
                buffer.Add( " 空白倍率！错误！" );
            else
            {
                buffer.AddFixedDecimalThousands( multiplier.ToDouble(), 2 ).Add( "倍船体" );
                buffer.Add( "   当前船体： " );
                EntityText.WriteHullOrShieldsNumber( buffer, CurrentOption.GetMaxHullPoints() );
                buffer.Add( "   新船体： " );
                EntityText.WriteHullOrShieldsNumber( buffer, ( CurrentOption.GetMaxHullPoints() * multiplier ).IntValue );
            }
            buffer.Add( "</size></b>\n" );
        }

        public override SubItemButtonWriter GetSubItemButtonWriter()
        {
            return SubButtonWriter;
        }

        public void SubButtonWriter( ArcenDoubleCharacterBuffer buffer, GameEntity_Squad CurrentOption, Faction hackerFaction, HackingType hackingType )
        {
            FInt multiplier = this.GetAddedMultiplierPerHack( CurrentOption, hackingType );
            if ( multiplier <= FInt.One )
                buffer.Add( " 空白倍率！错误！" );
            else
                buffer.Add( "  " ).AddFixedDecimalThousands( multiplier.ToDouble(), 2 ).Add( "x Hull" );
        }

        public override void WriteAnyDynamicDescriptionSubData( ArcenDoubleCharacterBuffer buffer, List<SafeSquadWrapper> eligibleTargets, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            
        }
    }

    public class Hacking_IncreaseShieldDurability : Hacking_SelfUnitWithSubSelection_Base
    {
        public FInt GetAddedMultiplierPerHack( GameEntity_Squad Target, HackingType type )
        {
            HackData hackData = this.GetHackDataForTargetOrNull( Target, type );
            if ( hackData == null )
                return FInt.Zero;
            return hackData.GetCustomData_FInt( "AddedMultiplierPerHack" );
        }

        public override bool DoSuccessfulCompletionSubLogic( GameEntity_Squad ChosenTargetOrNull, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            FInt multiplier = this.GetAddedMultiplierPerHack( ChosenTargetOrNull, type );
            FleetMembership fMem = ChosenTargetOrNull?.FleetMembership;
            if ( fMem != null )
            {
                if ( fMem.Hacked_ShieldHealthMultiplier <= FInt.One )
                    fMem.Hacked_ShieldHealthMultiplier = multiplier;
                else
                    fMem.Hacked_ShieldHealthMultiplier *= multiplier;
            }
            else
                return false;

            ChosenTargetOrNull?.FlagForForcedFullSyncToClients_FromHost();
            return true;
        }

        public override SubItemDropdownWriter GetSubItemDropdownWriter()
        {
            return SubDropdownWriter;
        }

        public void SubDropdownWriter( ArcenDoubleCharacterBuffer buffer, GameEntity_Squad CurrentOption, Faction hackerFaction, HackingType hackingType )
        {
            FInt multiplier = this.GetAddedMultiplierPerHack( CurrentOption, hackingType );
            buffer.Add( "<b><size=100%>" );
            buffer.Add( "护盾倍率： " );
            if ( multiplier <= FInt.One )
                buffer.Add( " 空白倍率！错误！" );
            else
            {
                buffer.AddFixedDecimalThousands( multiplier.ToDouble(), 2 ).Add( "倍护盾" );
                buffer.Add( "   当前护盾： " );
                EntityText.WriteHullOrShieldsNumber( buffer, CurrentOption.GetMaxShieldPoints() );
                buffer.Add( "   新护盾： " );
                EntityText.WriteHullOrShieldsNumber( buffer, (CurrentOption.GetMaxShieldPoints() * multiplier).IntValue );
            }
            buffer.Add( "</size></b>\n" );
        }

        public override SubItemButtonWriter GetSubItemButtonWriter()
        {
            return SubButtonWriter;
        }

        public void SubButtonWriter( ArcenDoubleCharacterBuffer buffer, GameEntity_Squad CurrentOption, Faction hackerFaction, HackingType hackingType )
        {
            FInt multiplier = this.GetAddedMultiplierPerHack( CurrentOption, hackingType );
            if ( multiplier <= FInt.One )
                buffer.Add( " 空白倍率！错误！" );
            else
                buffer.Add( "  " ).AddFixedDecimalThousands( multiplier.ToDouble(), 2 ).Add( "x Hull" );
        }

        public override void WriteAnyDynamicDescriptionSubData( ArcenDoubleCharacterBuffer buffer, List<SafeSquadWrapper> eligibleTargets, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
        }
    }



    public class Hacking_IncreaseWeaponPower : Hacking_SelfUnitWithSubSelection_Base
    {
        public FInt GetAddedMultiplierPerHack( GameEntity_Squad Target, HackingType type )
        {
            HackData hackData = this.GetHackDataForTargetOrNull( Target, type );
            if ( hackData == null )
                return FInt.Zero;
            return hackData.GetCustomData_FInt( "AddedMultiplierPerHack" );
        }

        public override bool DoSuccessfulCompletionSubLogic( GameEntity_Squad ChosenTargetOrNull, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            FInt multiplier = this.GetAddedMultiplierPerHack( ChosenTargetOrNull, type );
            FleetMembership fMem = ChosenTargetOrNull?.FleetMembership;
            if ( fMem != null )
            {
                if ( fMem.Hacked_WeaponsMultiplier <= FInt.One )
                    fMem.Hacked_WeaponsMultiplier = multiplier;
                else
                    fMem.Hacked_WeaponsMultiplier *= multiplier;
            }
            else
                return false;

            ChosenTargetOrNull?.FlagForForcedFullSyncToClients_FromHost();
            return true;
        }

        public override SubItemDropdownWriter GetSubItemDropdownWriter()
        {
            return SubDropdownWriter;
        }

        public void SubDropdownWriter( ArcenDoubleCharacterBuffer buffer, GameEntity_Squad CurrentOption, Faction hackerFaction, HackingType hackingType )
        {
            FInt multiplier = this.GetAddedMultiplierPerHack( CurrentOption, hackingType );
            buffer.Add( "<b><size=100%>" );
            buffer.Add( "武器倍率： " );
            if ( multiplier <= FInt.One )
                buffer.Add( " 空白倍率！错误！" );
            else
            {
                buffer.AddFixedDecimalThousands( multiplier.ToDouble(), 2 ).Add( "倍攻击" );
                buffer.Add( "   当前 DPS： " );
                int dps = CurrentOption.GeTotalDPS_ForDisplayOnly();
                buffer.AddNumberMoreReadable( dps );
                buffer.Add( "   新 DPS： " );
                buffer.AddNumberMoreReadable( ( dps * multiplier ).IntValue );
            }
            buffer.Add( "</size></b>\n" );
        }

        public override SubItemButtonWriter GetSubItemButtonWriter()
        {
            return SubButtonWriter;
        }

        public void SubButtonWriter( ArcenDoubleCharacterBuffer buffer, GameEntity_Squad CurrentOption, Faction hackerFaction, HackingType hackingType )
        {
            FInt multiplier = this.GetAddedMultiplierPerHack( CurrentOption, hackingType );
            if ( multiplier <= FInt.One )
                buffer.Add( " 空白倍率！错误！" );
            else
                buffer.Add( "  " ).AddFixedDecimalThousands( multiplier.ToDouble(), 2 ).Add( "x Hull" );
        }

        public override void WriteAnyDynamicDescriptionSubData( ArcenDoubleCharacterBuffer buffer, List<SafeSquadWrapper> eligibleTargets, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
        }
    }
}