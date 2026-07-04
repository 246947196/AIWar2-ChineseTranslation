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
                buffer.Add( "\nIf you switch the type of command station at this planet, the range increase will be gone.  However, if you switch back to this type, it will be there again." );

            FleetMembership targetMem = target.FleetMembership;
            int hopCount = target.TypeData.WatchPlanetsAtXHops + (targetMem == null ? 0 : targetMem.Hacked_ExtraWatchPlanetsAtXHops);

            int hackingRangeIncrease = GetAddedRangePerHack( target, hackingType );
            buffer.Add( "\nExisting recon range of " ).Add( hopCount ).Add( " will be increased by " ).Add( hackingRangeIncrease );
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
            buffer.Add( "Hull Multiplier: " );
            if ( multiplier <= FInt.One )
                buffer.Add( " Empty Multiplier!  Error!" );
            else
            {
                buffer.AddFixedDecimalThousands( multiplier.ToDouble(), 2 ).Add( "x Hull" );
                buffer.Add( "   Current Hull: " );
                EntityText.WriteHullOrShieldsNumber( buffer, CurrentOption.GetMaxHullPoints() );
                buffer.Add( "   New Hull: " );
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
                buffer.Add( " Empty Multiplier!  Error!" );
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
            buffer.Add( "Shield Multiplier: " );
            if ( multiplier <= FInt.One )
                buffer.Add( " Empty Multiplier!  Error!" );
            else
            {
                buffer.AddFixedDecimalThousands( multiplier.ToDouble(), 2 ).Add( "x Shields" );
                buffer.Add( "   Current Shields: " );
                EntityText.WriteHullOrShieldsNumber( buffer, CurrentOption.GetMaxShieldPoints() );
                buffer.Add( "   New Shields: " );
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
                buffer.Add( " Empty Multiplier!  Error!" );
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
            buffer.Add( "Weapons Multiplier: " );
            if ( multiplier <= FInt.One )
                buffer.Add( " Empty Multiplier!  Error!" );
            else
            {
                buffer.AddFixedDecimalThousands( multiplier.ToDouble(), 2 ).Add( "x Attack" );
                buffer.Add( "   Current DPS: " );
                int dps = CurrentOption.GeTotalDPS_ForDisplayOnly();
                buffer.AddNumberMoreReadable( dps );
                buffer.Add( "   New DPS: " );
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
                buffer.Add( " Empty Multiplier!  Error!" );
            else
                buffer.Add( "  " ).AddFixedDecimalThousands( multiplier.ToDouble(), 2 ).Add( "x Hull" );
        }

        public override void WriteAnyDynamicDescriptionSubData( ArcenDoubleCharacterBuffer buffer, List<SafeSquadWrapper> eligibleTargets, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
        }
    }
}