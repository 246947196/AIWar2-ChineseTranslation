using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    // Does NOT hold Warhead or Interplanetary Weapons hacking; those can be found in SpireRising_Hacking.
    public class Hacking_IncreaseSphereProduction : BaseHackingImplementation
    {
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            SphereFactionBaseInfo baseInfo = target.GetFactionBaseInfoOrNullAs_Safe<SphereFactionBaseInfo>();
            if ( baseInfo == null )
                return base.GetDynamicDescription( target, hackerOrNull, planet, hackerFaction, hackingType );
            FInt oldValue = baseInfo.GetPerSecondBudget;
            FInt increase = baseInfo.PerSecondBudgetBeforeMultiplier * baseInfo.Difficulty.BudgetPerSecond_MultiplierPerHack;
            int perc = (100 * increase / oldValue).IntValue;
            return $"\nThis will increase the budget of this sphere by <color=#cc0000>{perc}%</color>.";
        }
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            SphereFactionBaseInfo baseInfo = Target.PlanetFaction.Faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            if ( baseInfo == null )
            {
                RejectionReasonDescription = "Failed to load Target's Faction Base Info, was this spawned for a non Sphere faction?";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( baseInfo.IsCurrentlyAngryDueToHack )
            {
                RejectionReasonDescription = $"This {baseInfo.SphereType} Sphere is still angry at you from a previous hack. It will calm down and become hackable again in {baseInfo.HackedAngerDurationInSeconds - baseInfo.SecondsSinceLastHack} seconds.";
                return Hackable.AlreadyHasBeenHacked_ButStillShow;
            }

            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }

        public override int EstimateTotalDifficulty( HackingType type, GameEntity_Squad TargetOrNull, Planet PlanetOrNull, out int totalResponseStrengthEstimate, out ArcenCharacterBuffer debugLogOrNull, bool assumeAIStrengthLevels )
        {
            debugLogOrNull = null;
            SphereFactionBaseInfo baseInfo = TargetOrNull?.TryGetFactionBaseInfoOrNullAs_Safe<SphereFactionBaseInfo>();
            if ( baseInfo != null )
                totalResponseStrengthEstimate = baseInfo.GetMaxStrengthWhileBeingHacked.GetNearestIntPreferringHigher();
            else
                totalResponseStrengthEstimate = 0;
            return totalResponseStrengthEstimate;
        }

        public override void DoOneSecondOfHackingLogic_HackSpecificLogic( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            SphereFactionBaseInfo baseInfo = Target?.PlanetFaction.Faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            if ( baseInfo == null )
                return;

            // Get angry.
            baseInfo.GameSecondLastHacked = World_AIW2.Instance.GameSecond;

            FInt multiplier = GetHackingLevelMultiplier( Target.PlanetFaction.Faction );

            if ( Hacker.ActiveHack_DurationThusFar % type.GetPrimaryHackResponseInterval() == 0 )
            {
                Faction facOrNull = Target.GetFactionOrNull_Safe();
                if ( facOrNull != null )
                    facOrNull.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, (type.PrimaryResponseStrengthPerInterval * multiplier), Context, Event );
            }
            if ( type.GetSecondaryHackResponseInterval() != 0 && type.SecondaryResponseStrengthPerInterval != 0 &&
                Hacker.ActiveHack_DurationThusFar % type.GetSecondaryHackResponseInterval() == 0 )
            {
                Faction facOrNull = Target.GetFactionOrNull_Safe();
                if ( facOrNull != null )
                    facOrNull.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, (type.SecondaryResponseStrengthPerInterval * multiplier), Context, Event );
            }
            if ( type.GetTertiaryHackResponseInterval() != 0 && type.TertiaryResponseStrengthPerInterval != 0 &&
                Hacker.ActiveHack_DurationThusFar % type.GetTertiaryHackResponseInterval() == 0 )
            {
                Faction facOrNull = Target.GetFactionOrNull_Safe();
                if ( facOrNull != null )
                    facOrNull.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, (type.TertiaryResponseStrengthPerInterval * multiplier), Context, Event );
            }
        }

        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            SphereFactionBaseInfo baseInfo = TargetOrNull?.PlanetFaction.Faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            if ( baseInfo != null )
                baseInfo.TimesHackedForBudget++;

            return base.DoSuccessfulCompletionLogic_Extra( TargetOrNull, planet, Hacker, Context, type, Event );
        }
    }

    public class Hacking_IncreaseSphereStrength : BaseHackingImplementation
    {
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            SphereFactionBaseInfo baseInfo = target.GetFactionBaseInfoOrNullAs_Safe<SphereFactionBaseInfo>();
            if ( baseInfo == null )
                return base.GetDynamicDescription( target, hackerOrNull, planet, hackerFaction, hackingType );
            FInt oldValue = baseInfo.GetMaxStrength;
            FInt increase = baseInfo.MaxStrengthBeforeMultiplier * baseInfo.Difficulty.MaxStrength_MultiplierPerHack;
            int perc = (100 * increase / oldValue).IntValue;
            return $"\nThis will increase the max strength of this sphere by <color=#cc0000>{perc}%</color>.";
        }
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            SphereFactionBaseInfo baseInfo = Target.PlanetFaction.Faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            if ( baseInfo == null )
            {
                RejectionReasonDescription = "Failed to load Target's Faction Base Info, was this spawned for a non Sphere faction?";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( baseInfo.IsCurrentlyAngryDueToHack )
            {
                RejectionReasonDescription = $"This {baseInfo.SphereType} Sphere is still angry at you from a previous hack. It will calm down and become hackable again in {baseInfo.HackedAngerDurationInSeconds - baseInfo.SecondsSinceLastHack} seconds.";
                return Hackable.AlreadyHasBeenHacked_ButStillShow;
            }

            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }

        public override int EstimateTotalDifficulty( HackingType type, GameEntity_Squad TargetOrNull, Planet PlanetOrNull, out int totalResponseStrengthEstimate, out ArcenCharacterBuffer debugLogOrNull, bool assumeAIStrengthLevels )
        {
            debugLogOrNull = null;
            SphereFactionBaseInfo baseInfo = TargetOrNull?.TryGetFactionBaseInfoOrNullAs_Safe<SphereFactionBaseInfo>();
            if ( baseInfo != null )
                totalResponseStrengthEstimate = baseInfo.GetMaxStrengthWhileBeingHacked.GetNearestIntPreferringHigher();
            else
                totalResponseStrengthEstimate = 0;
            return totalResponseStrengthEstimate;
        }

        public override void DoOneSecondOfHackingLogic_HackSpecificLogic( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            SphereFactionBaseInfo baseInfo = Target?.PlanetFaction.Faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            if ( baseInfo == null )
                return;

            // Get angry.
            baseInfo.GameSecondLastHacked = World_AIW2.Instance.GameSecond;

            FInt multiplier = GetHackingLevelMultiplier( Target.PlanetFaction.Faction );

            if ( Hacker.ActiveHack_DurationThusFar % type.GetPrimaryHackResponseInterval() == 0 )
            {
                Faction facOrNull = Target.GetFactionOrNull_Safe();
                if ( facOrNull != null )
                    facOrNull.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, (type.PrimaryResponseStrengthPerInterval * multiplier), Context, Event );
            }
            if ( type.GetSecondaryHackResponseInterval() != 0 && type.SecondaryResponseStrengthPerInterval != 0 &&
                Hacker.ActiveHack_DurationThusFar % type.GetSecondaryHackResponseInterval() == 0 )
            {
                Faction facOrNull = Target.GetFactionOrNull_Safe();
                if ( facOrNull != null )
                    facOrNull.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, (type.SecondaryResponseStrengthPerInterval * multiplier), Context, Event );
            }
            if ( type.GetTertiaryHackResponseInterval() != 0 && type.TertiaryResponseStrengthPerInterval != 0 &&
                Hacker.ActiveHack_DurationThusFar % type.GetTertiaryHackResponseInterval() == 0 )
            {
                Faction facOrNull = Target.GetFactionOrNull_Safe();
                if ( facOrNull != null )
                    facOrNull.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, (type.TertiaryResponseStrengthPerInterval * multiplier), Context, Event );
            }
        }

        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            SphereFactionBaseInfo baseInfo = TargetOrNull?.PlanetFaction.Faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            if ( baseInfo != null )
                baseInfo.TimesHackedForStrength++;

            return base.DoSuccessfulCompletionLogic_Extra( TargetOrNull, planet, Hacker, Context, type, Event );
        }
    }

    public class Hacking_IncreaseSphereHops : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            SphereFactionBaseInfo baseInfo = Target.PlanetFaction.Faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            if ( baseInfo == null )
            {
                RejectionReasonDescription = "Failed to load Target's Faction Base Info, was this spawned for a non Sphere faction?";
                return Hackable.NeverCanBeHacked_Hide;
            }

            if ( baseInfo.TimesHackedForHops > baseInfo.GetBaseHopLimit() * 3 )
            {
                RejectionReasonDescription = "This sphere has already reached its maximum hop limit.";
                return Hackable.NeverCanBeHacked_Hide;
            }

            if ( baseInfo.IsCurrentlyAngryDueToHack )
            {
                RejectionReasonDescription = $"This {baseInfo.SphereType} Sphere is still angry at you from a previous hack. It will calm down and become hackable again in {baseInfo.HackedAngerDurationInSeconds - baseInfo.SecondsSinceLastHack} seconds.";
                return Hackable.AlreadyHasBeenHacked_ButStillShow;
            }

            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }

        public override int EstimateTotalDifficulty( HackingType type, GameEntity_Squad TargetOrNull, Planet PlanetOrNull, out int totalResponseStrengthEstimate, out ArcenCharacterBuffer debugLogOrNull, bool assumeAIStrengthLevels )
        {
            debugLogOrNull = null;
            SphereFactionBaseInfo baseInfo = TargetOrNull?.TryGetFactionBaseInfoOrNullAs_Safe<SphereFactionBaseInfo>();
            if ( baseInfo != null )
                totalResponseStrengthEstimate = baseInfo.GetMaxStrengthWhileBeingHacked.GetNearestIntPreferringHigher();
            else
                totalResponseStrengthEstimate = 0;
            return totalResponseStrengthEstimate;
        }

        public override void DoOneSecondOfHackingLogic_HackSpecificLogic( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            SphereFactionBaseInfo baseInfo = Target?.PlanetFaction.Faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            if ( baseInfo == null )
                return;

            // Get angry.
            baseInfo.GameSecondLastHacked = World_AIW2.Instance.GameSecond;

            FInt multiplier = GetHackingLevelMultiplier( Target.PlanetFaction.Faction );

            if ( Hacker.ActiveHack_DurationThusFar % type.GetPrimaryHackResponseInterval() == 0 )
            {
                Faction facOrNull = Target.GetFactionOrNull_Safe();
                if ( facOrNull != null )
                    facOrNull.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, (type.PrimaryResponseStrengthPerInterval * multiplier), Context, Event );
            }
            if ( type.GetSecondaryHackResponseInterval() != 0 && type.SecondaryResponseStrengthPerInterval != 0 &&
                Hacker.ActiveHack_DurationThusFar % type.GetSecondaryHackResponseInterval() == 0 )
            {
                Faction facOrNull = Target.GetFactionOrNull_Safe();
                if ( facOrNull != null )
                    facOrNull.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, (type.SecondaryResponseStrengthPerInterval * multiplier), Context, Event );
            }
            if ( type.GetTertiaryHackResponseInterval() != 0 && type.TertiaryResponseStrengthPerInterval != 0 &&
                Hacker.ActiveHack_DurationThusFar % type.GetTertiaryHackResponseInterval() == 0 )
            {
                Faction facOrNull = Target.GetFactionOrNull_Safe();
                if ( facOrNull != null )
                    facOrNull.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, (type.TertiaryResponseStrengthPerInterval * multiplier), Context, Event );
            }
        }

        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            SphereFactionBaseInfo baseInfo = TargetOrNull?.PlanetFaction.Faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            if ( baseInfo != null )
                baseInfo.TimesHackedForHops++;

            return base.DoSuccessfulCompletionLogic_Extra( TargetOrNull, planet, Hacker, Context, type, Event );
        }
    }

    public class Hacking_GrantShipLine_Sphere : Hacking_GrantShipLine_DontDestroyTarget
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            SphereFactionBaseInfo baseInfo = Target.PlanetFaction.Faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            if ( baseInfo == null )
            {
                RejectionReasonDescription = "Failed to load Target's Faction Base Info, was this spawned for a non Sphere faction?";
                return Hackable.NeverCanBeHacked_Hide;
            }

            if ( baseInfo.TimesHackedForUnits >= Type.NumberOfTimesIndividualUnitCanBeHacked )
            {
                RejectionReasonDescription = $"You only can hack a Dyson for ship lines {Type.NumberOfTimesIndividualUnitCanBeHacked} time(s).";
                return Hackable.AlreadyHasBeenHacked_Hide;
            }

            if ( baseInfo.IsCurrentlyAngryDueToHack )
            {
                RejectionReasonDescription = $"This {baseInfo.SphereType} Sphere is still angry at you from a previous hack. It will calm down and become hackable again in {baseInfo.HackedAngerDurationInSeconds - baseInfo.SecondsSinceLastHack} seconds.";
                return Hackable.AlreadyHasBeenHacked_ButStillShow;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }

        public override int EstimateTotalDifficulty( HackingType type, GameEntity_Squad TargetOrNull, Planet PlanetOrNull, out int totalResponseStrengthEstimate, out ArcenCharacterBuffer debugLogOrNull, bool assumeAIStrengthLevels )
        {
            debugLogOrNull = null;
            SphereFactionBaseInfo baseInfo = TargetOrNull?.TryGetFactionBaseInfoOrNullAs_Safe<SphereFactionBaseInfo>();
            if ( baseInfo != null )
                totalResponseStrengthEstimate = baseInfo.GetMaxStrengthWhileBeingHacked.GetNearestIntPreferringHigher();
            else
                totalResponseStrengthEstimate = 0;
            return totalResponseStrengthEstimate;
        }

        public override void DoOneSecondOfHackingLogic_HackSpecificLogic( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            SphereFactionBaseInfo baseInfo = Target?.PlanetFaction.Faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            if ( baseInfo == null )
                return;

            // Get angry.
            baseInfo.GameSecondLastHacked = World_AIW2.Instance.GameSecond;

            FInt multiplier = GetHackingLevelMultiplier( Target.PlanetFaction.Faction );

            if ( Hacker.ActiveHack_DurationThusFar % type.GetPrimaryHackResponseInterval() == 0 )
            {
                Faction facOrNull = Target.GetFactionOrNull_Safe();
                if ( facOrNull != null )
                    facOrNull.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, (type.PrimaryResponseStrengthPerInterval * multiplier), Context, Event );
            }
            if ( type.GetSecondaryHackResponseInterval() != 0 && type.SecondaryResponseStrengthPerInterval != 0 &&
                Hacker.ActiveHack_DurationThusFar % type.GetSecondaryHackResponseInterval() == 0 )
            {
                Faction facOrNull = Target.GetFactionOrNull_Safe();
                if ( facOrNull != null )
                    facOrNull.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, (type.SecondaryResponseStrengthPerInterval * multiplier), Context, Event );
            }
            if ( type.GetTertiaryHackResponseInterval() != 0 && type.TertiaryResponseStrengthPerInterval != 0 &&
                Hacker.ActiveHack_DurationThusFar % type.GetTertiaryHackResponseInterval() == 0 )
            {
                Faction facOrNull = Target.GetFactionOrNull_Safe();
                if ( facOrNull != null )
                    facOrNull.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, (type.TertiaryResponseStrengthPerInterval * multiplier), Context, Event );
            }
        }

        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            SphereFactionBaseInfo baseInfo = Target?.PlanetFaction.Faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            baseInfo.TimesHackedForUnits++;

            // They get stronger as well.
            int mult = type.NumberOfTimesIndividualUnitCanBeHacked;
            if ( mult < 1 )
                mult = 1;
            FInt increase = FInt.One / mult;
            baseInfo.AddedMaxStrengthMultiplierFromExternalSources += increase;
            baseInfo.AddedBudgetMultiplierFromExternalSources += increase;

            // Get angry.
            baseInfo.GameSecondLastHacked = World_AIW2.Instance.GameSecond;

            return base.DoSuccessfulCompletionLogic_Extra( Target, planet, Hacker, Context, type, Event );
        }
    }
}
