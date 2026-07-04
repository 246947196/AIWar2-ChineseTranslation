using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class Hacking_AnalyzeSpireDebris : BaseHackingImplementation
    {
        public override void DoOneSecondOfHackingLogic_HackSpecificLogic( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            FallenSpirePerUnitBaseInfo debrisData = Target.CreateExternalBaseInfo<FallenSpirePerUnitBaseInfo>( "FallenSpirePerUnitBaseInfo" );
            debrisData.TimeUntilDebrisVanishes += 1;
        }
    }
    public class Hacking_SearchForRelic : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            Faction fallenSpireFaction = FactionUtilityMethods.Instance.GetFallenSpireFaction();
            if ( fallenSpireFaction == null )
            {
                RejectionReasonDescription = "Fallen Spire isn't on path A";
                return Hackable.NeverCanBeHacked_Hide;
            }
            FallenSpireFactionBaseInfo spireData = fallenSpireFaction.TryGetExternalBaseInfoAs<FallenSpireFactionBaseInfo>();
            if ( spireData == null )
            {
                RejectionReasonDescription = "Fallen Spire isn't on path B";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( spireData.CurrentRelicSpawnPlanetIdx == -1 )
            {
                RejectionReasonDescription = "There is no Relic right now";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( spireData.CurrentRelicInSearchMode == false &&
                 spireData.CurrentRelicSpawnPlanetIdx != planet.Index )
            {
                RejectionReasonDescription = "This is not where the Relic is (and we aren't in search mode)";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( spireData.RelicOnMap )
            {
                RejectionReasonDescription = "The Relic is in the galaxy";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( spireData.PlanetsSearchedForCurrentRelic.Contains( planet.Index ) )
            {
                RejectionReasonDescription = "You have already searched here for the Relic";
                return Hackable.AlreadyHasBeenHacked_ButStillShow;
            }
            RejectionReasonDescription = string.Empty;
            return Hackable.CanBeHacked;
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            if ( planet == null )
            {
                if ( Target != null )
                    planet = Target.Planet;
            }
            if ( planet == null )
                throw new Exception( "No planet was hacked!  Could not complete hack." );

            Faction fallenSpireFaction = FactionUtilityMethods.Instance.GetFallenSpireFaction();
            if ( fallenSpireFaction == null )
                throw new Exception( "Spire isn't on. This is perplexing" );

            FallenSpireFactionBaseInfo spireData = fallenSpireFaction.TryGetExternalBaseInfoAs<FallenSpireFactionBaseInfo>();
            if ( spireData == null )
                throw new Exception( "Spire Global Data is null. This is perplexing" );

            if ( planet.Index == spireData.CurrentRelicSpawnPlanetIdx )
            {
                spireData.CurrentRelicSpawnPlanetIdx = -1;
                spireData.TimeForNextRelicSpawn = World_AIW2.Instance.GameSecond + FallenSpireFactionBaseInfo.Instance.RelicSpawnInterval + Context.RandomToUse.Next( 0, FallenSpireFactionBaseInfo.Instance.RelicSpawnIntervalRandomness );

                FallenSpireFactionBaseInfo.Instance.CreateRelic( planet, fallenSpireFaction, Hacker.GetFactionOrNull_Safe(), Context, FInt.One, false, Engine_AIW2.Instance.CombatCenter, true );
            }
            else
            {
                PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                if ( chatHandlerOrNull != null )
                    chatHandlerOrNull.PlanetToView = planet;

                Planet relicPlanetOrNull = World_AIW2.Instance.GetPlanetByIndex( spireData.CurrentRelicSpawnPlanetIdx );
                World_AIW2.Instance.QueueChatMessageOrCommand( "You did not find the Relic on " + planet.Name + ". The Relic is " +
                    (relicPlanetOrNull == null ? "unknown" : relicPlanetOrNull.GetHopsTo( planet ).ToString()) + " hops away. Every planet searched this way increases the AI response when you find the Relic.",
                    ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                spireData.PlanetsSearchedForCurrentRelic.Add( planet.Index );
            }
            return true;
        }
    }
    public class Hacking_StealTeliumDebris : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            Faction facOrNull = Target.GetFactionOrNull_Safe();
            if ( facOrNull != null )
            {
                // Must be a Spire Infested Telia, and its faction must have debris.
                if ( Target.TypeData.GetHasTag( MacrophageFactionBaseInfoCore.SpireTeliumTag ) &&
                    //we do this kind of check rather than the usual faction InternalName check
                    //because this is three different kinds of faction that all have a common root
                    Target.GetFactionBaseInfoOrNullAs_Safe<MacrophageFactionBaseInfoCore>() != null &&
                    facOrNull.HasObtainedSpireDebris )
                {
                    RejectionReasonDescription = string.Empty;
                    return Hackable.CanBeHacked;
                }
            }

            RejectionReasonDescription = string.Empty;
            return Hackable.NeverCanBeHacked_Hide;
        }

        public override void DoOneSecondOfHackingLogic_HackSpecificLogic( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            Faction spawnFaction = Target.GetFactionOrNull_Safe();
            MacrophageFactionBaseInfoCore infestation = spawnFaction.TryGetExternalBaseInfoAs<MacrophageFactionBaseInfoCore>();

            if ( Target.TryGetExternalBaseInfoAs<MacrophagePerTeliumBaseInfo>() != null )
            {
                if ( Hacker.ActiveHack_DurationThusFar % type.GetPrimaryHackResponseInterval() == 0 )
                {
                    byte markLevel = (byte)Math.Min( 7, (Hacker.ActiveHack_DurationThusFar / type.GetPrimaryHackResponseInterval() * type.PrimaryResponseStrengthIncreasePerEffect).GetNearestIntPreferringHigher() );
                    for ( int x = 0; x < type.PrimaryResponseStrengthPerInterval; x++ )
                    {
                        MacrophageDeepLinkRoot.Instance.SpawnNewHarvester( infestation, Context, Target, Target.TryGetExternalBaseInfoAs<MacrophagePerTeliumBaseInfo>(), false, markLevel );
                    }
                }
            }

            if ( Target.CalculateAttackingTargetID_Safe() != Hacker.PrimaryKeyID )
            {
                Target.Orders.InsertOrderAtStart( Target, EntityOrder.Create_Attack( Hacker.PrimaryKeyID, false, "HackerPKID", true, OrderSource.Other, false ) );
            }
        }

        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            // Remove its debris.
            Faction facOrNull = Target.GetFactionOrNull_Safe();
            if ( facOrNull != null )
                facOrNull.HasObtainedSpireDebris = false;

            // Blow up the Telium, and let the harvesters loose.
            Target.Die( Context, true );

            return true;
        }
    }

    // Splintering Spire Hacking
    public class Hacking_StealWarheadDesigns_SpireSphere : Hacking_GrantShipLine_DontDestroyTarget
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            SplinteringSpireFactionBaseInfo parentInfo = SplinteringSpireFactionBaseInfo.Instance;
            // CHRIS NOTICE - If we want to disable this Hack without a Sphere War being enabled, we can simply remove the following comments.
            //if ( parentInfo == null )
            //{
            //    RejectionReasonDescription = "Sphere Wars is either not enabled, or has been improperly loaded.";
            //    return Hackable.NeverCanBeHacked_Hide;
            //}
            SphereFactionBaseInfo baseInfo = Target.PlanetFaction.Faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            if ( baseInfo == null )
            {
                RejectionReasonDescription = "Target is not of a Spire Sphere faction of type Grey or Dark.";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( baseInfo.IsCurrentlyAngryDueToHack )
            {
                RejectionReasonDescription = $"This {baseInfo.SphereType} Sphere is still angry at you from a previous hack. It will calm down and become hackable again in {baseInfo.HackedAngerDurationInSeconds - baseInfo.SecondsSinceLastHack} seconds.";
                return Hackable.AlreadyHasBeenHacked_ButStillShow;
            }
            short timesWon = 0;
            if ( parentInfo != null )
                parentInfo.TotalTimesWon.TryGetValue( Target.PlanetFaction.Faction, out timesWon );
            int requiredPoints = 2 + (baseInfo.TimesHackedForUnits * 2);
            if ( timesWon + baseInfo.TotalTimesHacked - baseInfo.TimesHackedForUnits < requiredPoints )
            {
                RejectionReasonDescription = $"{Target.TypeData.DisplayName} is not strong enough to steal from. It has {timesWon + baseInfo.TotalTimesHacked - baseInfo.TimesHackedForUnits} points out of a required {requiredPoints} points. It gains points by claiming the most resources during Splintering Spire events, or by you hacking them to increase their budget, strength, or range.";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }

        public override int EstimateTotalDifficulty( HackingType type, GameEntity_Squad TargetOrNull, Planet PlanetOrNull, out int totalResponseStrengthEstimate, out ArcenCharacterBuffer debugLogOrNull, bool assumeAIStrengthLevels )
        {
            SphereFactionBaseInfo baseInfo = TargetOrNull?.PlanetFaction.Faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            if ( baseInfo != null )
            {
                totalResponseStrengthEstimate = baseInfo.GetMaxStrengthWhileBeingHacked.GetNearestIntPreferringHigher();
                debugLogOrNull = null;
                return totalResponseStrengthEstimate;
            }

            return base.EstimateTotalDifficulty( type, TargetOrNull, PlanetOrNull, out totalResponseStrengthEstimate, out debugLogOrNull, assumeAIStrengthLevels );
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
            SphereFactionBaseInfo baseInfo = Target.PlanetFaction.Faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            if ( baseInfo != null )
                baseInfo.TimesHackedForUnits++;

            return base.DoSuccessfulCompletionLogic_Extra( Target, planet, Hacker, Context, type, Event );
        }
    }

    public class HackToStealInterplanetaryWeapon_SpireSphere : Hacking_GrantShipLine_DontDestroyTarget
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            SplinteringSpireFactionBaseInfo parentInfo = SplinteringSpireFactionBaseInfo.Instance;
            // CHRIS NOTICE - If we want to disable this Hack without a Sphere War being enabled, we can simply remove the following comments.
            //if ( parentInfo == null )
            //{
            //    RejectionReasonDescription = "Sphere Wars is either not enabled, or has been improperly loaded.";
            //    return Hackable.NeverCanBeHacked_Hide;
            //}
            SphereFactionBaseInfo baseInfo = Target.PlanetFaction.Faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            if ( baseInfo == null )
            {
                RejectionReasonDescription = "Target is not of a Spire Sphere faction of type Chromatic or Imperial.";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( baseInfo.IsCurrentlyAngryDueToHack )
            {
                RejectionReasonDescription = $"This {baseInfo.SphereType} Sphere is still angry at you from a previous hack. It will calm down and become hackable again in {baseInfo.HackedAngerDurationInSeconds - baseInfo.SecondsSinceLastHack} seconds.";
                return Hackable.AlreadyHasBeenHacked_ButStillShow;
            }
            short timesWon = 0;
            if ( parentInfo != null )
                parentInfo.TotalTimesWon.TryGetValue( Target.PlanetFaction.Faction, out timesWon );
            int requiredPoints = 5;
            if ( timesWon + baseInfo.TotalTimesHacked - baseInfo.TimesHackedForUnits < requiredPoints )
            {
                RejectionReasonDescription = $"{Target.TypeData.DisplayName} is not strong enough to steal from. It has {timesWon + baseInfo.TotalTimesHacked - baseInfo.TimesHackedForUnits} points out of a required {requiredPoints} points. It gains points by claiming the most resources during Splintering Spire events, or by you hacking them to increase their budget, strength, or range.";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }

        public override int EstimateTotalDifficulty( HackingType type, GameEntity_Squad TargetOrNull, Planet PlanetOrNull, out int totalResponseStrengthEstimate, out ArcenCharacterBuffer debugLogOrNull, bool assumeAIStrengthLevels )
        {
            SphereFactionBaseInfo baseInfo = TargetOrNull?.PlanetFaction.Faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            if ( baseInfo != null )
            {
                totalResponseStrengthEstimate = baseInfo.GetMaxStrengthWhileBeingHacked.GetNearestIntPreferringHigher();
                debugLogOrNull = null;
                return totalResponseStrengthEstimate;
            }

            return base.EstimateTotalDifficulty( type, TargetOrNull, PlanetOrNull, out totalResponseStrengthEstimate, out debugLogOrNull, assumeAIStrengthLevels );
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
            SphereFactionBaseInfo baseInfo = Target.PlanetFaction.Faction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
            if ( baseInfo != null )
                baseInfo.TimesHackedForUnits++;

            return base.DoSuccessfulCompletionLogic_Extra( Target, planet, Hacker, Context, type, Event );
        }
    }
}
