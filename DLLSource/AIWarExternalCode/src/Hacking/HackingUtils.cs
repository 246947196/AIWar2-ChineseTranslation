using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public static class HackingUtils
    {
        #region GetDeterministicRandomGeneratorForHackTarget_Threadsafe
        public static MersenneTwister GetDeterministicRandomGeneratorForHackTarget_Threadsafe( GameEntity_Squad squad )
        {

            if ( squad == null )
                return new MersenneTwister( -17 );

            //hacking seed is used if set, otherwise it uses the primary key ID
            return new MersenneTwister( squad.HackingSeed > 0 ? squad.HackingSeed : squad.PrimaryKeyID );
        }
        #endregion

        #region GetListOfTechsValidForFactionFromTag
        public static void GetListOfTechsValidForFactionFromTag( List<TechUpgrade> ListToFill, Faction fac, string TagToMatch )
        {
            ListToFill.Clear();

            if ( !TechUpgradeTable.Instance.RowsByTag.CheckIfAlreadyHasKey( TagToMatch ) )
                return;

            List<TechUpgrade> upgradesByTag = TechUpgradeTable.Instance.RowsByTag[TagToMatch];
            foreach ( TechUpgrade tech in upgradesByTag )
            {
                if ( fac == null )
                    ListToFill.Add( tech );
                else
                {
                    int unused;
                    ArcenRejectionReason reason = fac.GetCanUnlockTech( tech, false, out unused, false );
                    if ( reason == ArcenRejectionReason.Unknown )
                        ListToFill.Add( tech );
                }
            }
        }
        #endregion

        #region StartListOfTechsForTechVaultStyleGranter_ByTag
        public static void StartListOfTechsForTechVaultStyleGranter_ByTag( List<TechUpgrade> ListToFill, GameEntity_Squad target, Faction hackerFaction, string TagToMatch, int NumberTechsToAdd )
        {
            ListToFill.Clear();

            if ( target == null )
                return;
            if ( hackerFaction == null )
                return;

            MersenneTwister rand = GetDeterministicRandomGeneratorForHackTarget_Threadsafe( target );

            GetListOfTechsValidForFactionFromTag( ListToFill, hackerFaction, TagToMatch );

            while ( NumberTechsToAdd > 0 && ListToFill.Count > NumberTechsToAdd )
            {
                int index = rand.Next( 0, ListToFill.Count );
                ListToFill.RemoveAt( index );
            }
        }
        #endregion

        #region AddToListOfTechsForTechVaultStyleGranter_ByTag
        public static int AddToListOfTechsForTechVaultStyleGranter_ByTag( List<TechUpgrade> existingList, GameEntity_Squad target, Faction hackerFaction, string TagToMatch, int NumberTechsToAdd )
        {
            if ( target == null )
                return 0;
            if ( hackerFaction == null )
                return 0;

            MersenneTwister rand = GetDeterministicRandomGeneratorForHackTarget_Threadsafe( target );

            List<TechUpgrade> availableTechsFromTag = TechUpgrade.GetTemporaryTechUpgradeList( "AddToListOfTechsForTechVaultStyleGranter_ByTag-availableTechsFromTag", 10f );
            if ( availableTechsFromTag == null ) //blocked for teardown/shutdown; bail
                return 0;
            GetListOfTechsValidForFactionFromTag( availableTechsFromTag, hackerFaction, TagToMatch );

            int numberAdded = 0;
            while ( NumberTechsToAdd > 0 && availableTechsFromTag.Count > 0 )
            {
                int index = rand.Next( 0, availableTechsFromTag.Count );
                existingList.Add( availableTechsFromTag[index] );
                availableTechsFromTag.RemoveAt( index );
                NumberTechsToAdd--;
                numberAdded++;
            }
            TechUpgrade.ReleaseTemporaryTechUpgradeList( availableTechsFromTag );
            return numberAdded;
        }
        #endregion

        #region GetListOfTargetsForHackForPlanet
        public static void GetListOfTargetsForHackForPlanet( List<SafeSquadWrapper> ListToFill, Planet planet, Faction hackerFaction, HackingType hackType )
        {
            ListToFill.Clear();

            if ( planet == null )
                return;
            if ( hackerFaction == null )
                return;
            if ( hackType == null )
                return;

            foreach ( GameEntity_Squad squad in planet.Squads( EntityRollupType.Hackable ) )
            {
                if ( squad == null || squad.TypeData == null )
                    continue;
                //must be valid for this hack in general
                if ( !squad.TypeData.GetIsEligibleForHack( hackType ) )
                    continue;
                //must be valid for this hack in terms of faction allegieances
                if ( !hackType.GetIsHackValidAgainst( squad, false ) )
                    continue;

                ListToFill.Add( squad );

            }
        }
        #endregion

        #region GetMinAndMaxHackingPointCostsForTargetList
        public static void GetMinAndMaxHackingPointCostsForTargetList( List<SafeSquadWrapper> targets, HackingType hackType, out FInt minCost, out FInt maxCost )
        {
            minCost = (FInt)9999;
            maxCost = FInt.Zero;

            foreach ( SafeSquadWrapper wrap in targets )
            {
                GameEntity_Squad target = wrap.GetSquad();
                if ( target == null )
                    continue;
                FInt cost = hackType.GetHackPointCostForTarget( target );

                if ( cost < minCost )
                    minCost = cost;
                if ( cost > maxCost )
                    maxCost = cost;
            }
        }
        #endregion

        #region CalculateNextBoundsForSingleHackingOption
        public static void CalculateNextBoundsForSingleHackingOption( out Rect soleBounds, ref float runningY, Rect priorBounds, float rowBuffer )
        {
            soleBounds = ArcenRectangle.UnityRect_Empty;
            soleBounds.x = priorBounds.x;
            soleBounds.y = runningY;
            soleBounds.width = priorBounds.width;
            soleBounds.height = priorBounds.height;

            runningY += priorBounds.height + rowBuffer;
        }
        #endregion

        #region PopulateOneCustomHackingOptionButton
        public static void PopulateOneCustomHackingOptionButton( ArcenUI_SetOfCreateElementDirectives Set,
            ref float runningY, ref UnityEngine.Rect currentBounds, float rowBuffer, AddHackButtonToWindow WindowAdder,
            string IdentifyingString, int IdentifyingInt1, int IdentifyingInt2, ArcenCachedExternalTypeDirect CustomHackingButtonType )
        {
            //add the button
            WindowAdder( Set, CustomHackingButtonType, IdentifyingString, IdentifyingInt1, IdentifyingInt2, currentBounds );
            //calculate the bounds for the next button, if there is one
            CalculateNextBoundsForSingleHackingOption( out currentBounds, ref runningY, currentBounds, rowBuffer );
        }
        #endregion

        #region CalculateHackerForHack
        public static GameEntity_Squad CalculateHackerForHack( GameEntity_Squad TargetToChooseFor, Planet PlanetToChooseFor, HackingType HackTypeToChooseFor, bool ShowErrorIfMultipleOptions )
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null )
            {
                //spectator mode
                return null;
            }

            //PlanetFaction localPlanetFaction = PlanetToChooseFor.GetPlanetFactionForFaction( localFaction );
            GameEntity_Squad hacker = GetPreferredHacker( TargetToChooseFor, HackTypeToChooseFor, PlanetToChooseFor, ShowErrorIfMultipleOptions );

            if ( hacker == null && !HackTypeToChooseFor.LocalHackerRequired )
                hacker = localFaction.GetFirstMatching( EntityRollupType.KingUnitsOnly, false, false );
            return hacker;
        }
        #endregion

        #region GetPreferredHacker
        public static GameEntity_Squad GetPreferredHacker( GameEntity_Squad target, HackingType type, Planet planet, bool ShowErrorIfMultipleOptions )
        {
            if ( type == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLog( "Null HackingType passed to GetPreferredHacker!", Verbosity.ShowAsError );
                return null;
            }
            if ( type.HackCompletesInstantly || type.DontPromptOnMultipleHackers )
                ShowErrorIfMultipleOptions = false; //don't care about multiple options if it's instant.  Just choose whatever

            int debugCode = 0;
            try
            {
                debugCode = 100;
                Faction hackerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( hackerFaction == null )
                    return null;
                debugCode = 101;

                if ( planet == null )
                {
                    if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                        ArcenDebugging.ArcenDebugLog( "Null planet passed to GetPreferredHacker!", Verbosity.ShowAsError );
                    return null;
                }

                debugCode = 107;
                PlanetFaction localPlanetFaction = planet.GetPlanetFactionForFaction( hackerFaction );
                debugCode = 110;
                if ( target == null && !type.HackIsAgainstPlanet )
                    throw new Exception( "GetPreferredHacker called with null target and hack is not against planet. Hack " + type.Name );
                if ( localPlanetFaction == null || hackerFaction == null )
                    return null; //this can happen if you left the popup open when exiting one tutorial and entering another tutorial
                if ( !type.LocalHackerRequired )
                {
                    if ( hackerFaction.UsesAnyRandomHackerForHacks_Safe() )
                        return hackerFaction.GetFirstMatching( EntityRollupType.Hackers, false, false ); //just pick any random hacker anywhere for a non-human empire faction
                    return hackerFaction.GetFirstMatching( EntityRollupType.KingUnitsOnly, false, false );
                }
                debugCode = 120;

                GameEntity_Squad chosenHacker = null;

                if ( type.HackerMustBeBattlestation )
                {
                    //first check selected
                    if ( HandleHackerTierSelectedOnly( localPlanetFaction, EntityRollupType.FleetLeaders,
                        SpecialEntityType.BattlestationCitadel, out chosenHacker, "Citadel fleet/hackers" ) )
                        return chosenHacker;
                    if ( HandleHackerTierSelectedOnly( localPlanetFaction, EntityRollupType.FleetLeaders,
                        SpecialEntityType.BattlestationBasic, out chosenHacker, "Battlestation fleet/hackers" ) )
                        return chosenHacker;

                    //then check pairs by tier
                    if ( HandleHackerTierPair( localPlanetFaction, ShowErrorIfMultipleOptions, EntityRollupType.Battlestation,
                        SpecialEntityType.BattlestationCitadel, out chosenHacker, "Citadel fleet/hackers" ) )
                        return chosenHacker;
                    if ( HandleHackerTierPair( localPlanetFaction, ShowErrorIfMultipleOptions, EntityRollupType.Battlestation,
                        SpecialEntityType.BattlestationBasic, out chosenHacker, "Battlestation fleet/hackers" ) )
                        return chosenHacker;

                    return null;
                }

                //first check selected
                if ( HandleHackerTierSelectedOnly( localPlanetFaction, EntityRollupType.FleetLeaders,
                    SpecialEntityType.HumanHomeArk, out chosenHacker, "Human Home Ark" ) )
                    return chosenHacker;
                if ( HandleHackerTierSelectedOnly( localPlanetFaction, EntityRollupType.FleetLeaders,
                    SpecialEntityType.MobileCustomUnattachedFleetFlagship, out chosenHacker, "Custom Unattached Fleet Leaders" ) )
                    return chosenHacker;
                if ( HandleHackerTierSelectedOnly( localPlanetFaction, EntityRollupType.FleetLeaders,
                    SpecialEntityType.MobileCustomCityFedFleetFlagship, out chosenHacker, "City-fed fleet/hackers" ) )
                    return chosenHacker;
                if ( HandleHackerTierSelectedOnly( localPlanetFaction, EntityRollupType.FleetLeaders,
                    SpecialEntityType.MobileOfficerCombatFleetFlagship, out chosenHacker, "Officer fleet/hackers" ) )
                    return chosenHacker;
                if ( HandleHackerTierSelectedOnly( localPlanetFaction, EntityRollupType.FleetLeaders,
                    SpecialEntityType.MobileStrikeCombatFleetFlagship, out chosenHacker, "Strike fleet/hackers" ) )
                    return chosenHacker;
                if ( type.HackerCanBeSupportFleet )
                    if ( HandleHackerTierSelectedOnly( localPlanetFaction, EntityRollupType.FleetLeaders,
                        SpecialEntityType.MobileSupportFleetFlagship, out chosenHacker, "Support fleet/hackers" ) )
                        return chosenHacker;
                if ( type.HackerCanBeBattlestation )
                {
                    if ( HandleHackerTierSelectedOnly( localPlanetFaction, EntityRollupType.FleetLeaders,
                        SpecialEntityType.BattlestationCitadel, out chosenHacker, "Citadel fleet/hackers" ) )
                        return chosenHacker;
                    if ( HandleHackerTierSelectedOnly( localPlanetFaction, EntityRollupType.FleetLeaders,
                        SpecialEntityType.BattlestationBasic, out chosenHacker, "Battlestation fleet/hackers" ) )
                        return chosenHacker;
                }

                //then check pairs by tier
                if ( HandleHackerTierPair( localPlanetFaction, ShowErrorIfMultipleOptions, EntityRollupType.FleetLeaders,
                    SpecialEntityType.HumanHomeArk, out chosenHacker, "Human Home Ark" ) )
                    return chosenHacker;
                if ( HandleHackerTierPair( localPlanetFaction, ShowErrorIfMultipleOptions, EntityRollupType.FleetLeaders,
                    SpecialEntityType.MobileOfficerCombatFleetFlagship, out chosenHacker, "Officer fleet/hackers" ) )
                    return chosenHacker;
                if ( HandleHackerTierPair( localPlanetFaction, ShowErrorIfMultipleOptions, EntityRollupType.FleetLeaders,
                    SpecialEntityType.MobileStrikeCombatFleetFlagship, out chosenHacker, "Strike fleet/hackers" ) )
                    return chosenHacker;
                if ( HandleHackerTierPair( localPlanetFaction, ShowErrorIfMultipleOptions, EntityRollupType.FleetLeaders,
                    SpecialEntityType.MobileCustomCityFedFleetFlagship, out chosenHacker, "City-fed fleet/hackers" ) )
                    return chosenHacker;
                if ( HandleHackerTierPair( localPlanetFaction, ShowErrorIfMultipleOptions, EntityRollupType.FleetLeaders,
                    SpecialEntityType.MobileCustomUnattachedFleetFlagship, out chosenHacker, "Custom Unattached Fleet Leaders" ) )
                    return chosenHacker;

                if ( type.HackerCanBeSupportFleet )
                    if ( HandleHackerTierPair( localPlanetFaction, ShowErrorIfMultipleOptions, EntityRollupType.FleetLeaders,
                        SpecialEntityType.MobileSupportFleetFlagship, out chosenHacker, "Support fleet/hackers" ) )
                        return chosenHacker;
                if ( type.HackerCanBeBattlestation )
                {
                    if ( HandleHackerTierPair( localPlanetFaction, ShowErrorIfMultipleOptions, EntityRollupType.FleetLeaders,
                        SpecialEntityType.BattlestationCitadel, out chosenHacker, "Citadel fleet/hackers" ) )
                        return chosenHacker;
                    if ( HandleHackerTierPair( localPlanetFaction, ShowErrorIfMultipleOptions, EntityRollupType.FleetLeaders,
                        SpecialEntityType.BattlestationBasic, out chosenHacker, "Battlestation fleet/hackers" ) )
                        return chosenHacker;
                }

                return null;
            }
            catch ( Exception e )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in GetPreferredHacker code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            return null;
        }
        #endregion

        #region HandleHackerTierSelectedOnly
        private static bool HandleHackerTierSelectedOnly( PlanetFaction localPlanetFaction,
            EntityRollupType RollupToUse, SpecialEntityType MustBeThisType, out GameEntity_Squad result,
            string ErrorGroupName )
        {
            if ( Helper_HandleHackerTier( localPlanetFaction, false, RollupToUse,
                MustBeThisType, true, out result, ErrorGroupName ) )
                return true;
            return false;
        }
        #endregion

        #region HandleHackerTierPair
        private static bool HandleHackerTierPair( PlanetFaction localPlanetFaction,
            bool ShowErrorIfMultipleOptions, EntityRollupType RollupToUse, SpecialEntityType MustBeThisType, out GameEntity_Squad result,
            string ErrorGroupName )
        {
            if ( Helper_HandleHackerTier( localPlanetFaction, ShowErrorIfMultipleOptions, RollupToUse,
                MustBeThisType, true, out result, ErrorGroupName ) )
                return true;
            if ( Helper_HandleHackerTier( localPlanetFaction, ShowErrorIfMultipleOptions, RollupToUse,
                MustBeThisType, false, out result, ErrorGroupName ) )
                return true;
            return false;
        }
        #endregion

        #region Helper_HandleHackerTier
        private static bool Helper_HandleHackerTier( PlanetFaction localPlanetFaction,
            bool ShowErrorIfMultipleOptions, EntityRollupType RollupToUse, SpecialEntityType MustBeThisType, bool MustBeSelected, out GameEntity_Squad result,
            string ErrorGroupName )
        {
            GameEntity_Squad chosenHacker = null;
            int countOfValidHackers = 0;
            foreach ( GameEntity_Squad flagship in localPlanetFaction.Entities.Squads( RollupToUse ) )
            {
                if ( flagship.GetIsCrippled() || flagship.GetIsNonFunctional() )
                    continue;
                //if not matching type, skip
                if ( flagship.TypeData.SpecialType != MustBeThisType )
                    continue;
                //first check only selected hackers
                if ( MustBeSelected )
                {
                    if ( !flagship.GetIsSelected() )
                        continue;
                }
                chosenHacker = flagship;
                countOfValidHackers++;
            }
            if ( countOfValidHackers > 1 && ShowErrorIfMultipleOptions )
            {
                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, MustBeSelected ? "选择了多个黑客" : "存在多个黑客",
                    "你当前有多个 " + ErrorGroupName + (MustBeSelected ? " 在此星球上被选中" : " 在此星球上") + "，我们无法确定你要使用哪个进行黑客。请选择一个黑客后重试。", "确定" );
                result = null;
                return false;
            }
            if ( chosenHacker != null )
            {
                result = chosenHacker;
                return true;
            }
            result = null;
            return false;
        }
        #endregion

        public static bool IsHackForFaction( this HackingType type, Faction localFaction )
        {
            PlayerTypeData localPlayerTypeData = localFaction?.PlayerTypeDataOrNull_ModeratelyExpensive;
            if ( localPlayerTypeData == null )
                return false; //we have no player type
            if ( type.ForAllSidekicks && localPlayerTypeData.GetHasTag("Sidekick"))
                return true;
            if ( type.ForPlayerTypesOrBlankIfForAllEmpireStyleTypes.Count <= 0 ) {
                //since there are no types specified, this means it's for empire-style factions in general
                return localPlayerTypeData.IsConsideredAFullEmpire;
            } else {
                //since there are types specified, we need to be one of those types
                return type.ForPlayerTypesOrBlankIfForAllEmpireStyleTypes.Contains( localPlayerTypeData.InternalName );
            }
        }

        public static bool IsHackValidOnPlanet( this HackingType type, Planet planet )
        {
            //LOG.Line();
            if (planet == null)
                return false;
            //LOG.Line();
            //don't show irrelevant scouting hacks
            if ( planet.IntelLevel > PlanetIntelLevel.Unexplored &&
               type.OnlyForUnexploredPlanets )
                return false;
            //LOG.Line();
            if ( planet.IntelLevel > PlanetIntelLevel.CurrentlyWatched &&
               type.OnlyForUnwatchedPlanets )
                return false;
            //LOG.Line();
            //don't show higher level hacks if the planet isn't explored
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored &&
               !type.OnlyForUnexploredPlanets )
                return false;
            //LOG.Line();
            Faction controllingFactionOfPlanet = planet.GetControllingFaction();
            //LOG.Line();
            if ( type.OnlyForNeutralPlanets &&
                (controllingFactionOfPlanet != null &&
                 controllingFactionOfPlanet.Type != FactionType.NaturalObject) )
                return false;
            //LOG.Line();
            if ( type.OnlyForAIPlanets &&
                (controllingFactionOfPlanet != null &&
                 controllingFactionOfPlanet.Type != FactionType.AI) )
                return false;
            //LOG.Line();
            if ( type.MaxMarkLevelOfAIPlanets > 0 &&
                 controllingFactionOfPlanet != null &&
                 planet.MarkLevelForAIOnly != null &&
                 planet.MarkLevelForAIOnly.Ordinal > type.MaxMarkLevelOfAIPlanets )
            {
                return false;
            }
            //LOG.Line();
            return true;
        }


        public static bool IsHackValidInCampaign( this HackingType type )
        {
            AIDifficulty highestDifficulty = FactionUtilityMethods.Instance.GetHighestAIDifficulty_AsDifficulty();
            if ( highestDifficulty != null )
            {
                byte strongestAI = highestDifficulty.Difficulty;
                //if too low of a level, skip
                if ( strongestAI < type.OnlyShowWhenMaxAIDifficultyIsGreaterThanOrEqualTo &&
                    type.OnlyShowWhenMaxAIDifficultyIsGreaterThanOrEqualTo > 0 )
                    return false;
                //if too high of a level, skip
                if ( strongestAI >= type.OnlyShowWhenMaxAIDifficultyIsLessThan &&
                    type.OnlyShowWhenMaxAIDifficultyIsLessThan > 0 )
                    return false;
            }
            return true;
        }

        #region GetShouldSkipHackOnSidebar
        public static bool GetShouldSkipHackOnSidebar( HackingType type, Faction localFaction, Planet planet )
        {
            if ( type.Deprecated )
                return true;
            //all hacks are only for specific types of player faction (can be for multiple, though)
            if ( !type.IsHackForFaction(localFaction) ) {
                return true;
            }
            if ( !type.IsHackValidOnPlanet(planet) ) {
                return true;
            }
            if ( !type.IsHackValidInCampaign() ) {
                return true;
            }
            return false;
        }
        #endregion

        public static bool GetShouldSkipHackInEntityTooltip( this HackingType type, GameEntity_Squad entity)
        {
            if ( type.Deprecated )
                return true;
            
            if ( entity.IsFakeEntity )
                return false;
            
            if ( !type.GetIsHackValidAgainst( entity, false ) )
                return true;
            
            Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
            if ( !type.IsHackForFaction(localFaction) )
                return true;
            
            if ( !type.IsHackValidInCampaign() )
                return true;
            
            if (!entity.TypeData.CanGoThroughWormholes) 
            {
                // If the entity can't go through wormholes, check if the
                // hack can be done on its current planet.
                // If it can go through wormholes, we should show hacks that
                // could be done elsewhere.
                if ( !type.IsHackValidOnPlanet(entity.Planet) ) {
                    return true;
                }
            }
            
            // For ships that grant ship lines there is already a whole separate tooltip line
            // showing it, and the lines it grants. So we avoid duplicating that information again
            // later in the tooltip by returning true here.
            if (type.IsAGrantShipStyleHack && 
                EntityText.Use != WriterToUse.Formatted) 
            {
                return true;
            }
            
            return false;
        }


        public static FInt CalculateActiveHackingCosts(Faction faction)
        {
            FInt costForActiveHacks = FInt.Zero;
            foreach ( GameEntity_Squad entity in faction.Squads( EntityRollupType.Hackers ) )
            {
                //if no active hack, don't tell me about this thing!
                if ( entity.ActiveHack == null )
                    continue;
                //this isn't precise (since sometimes hacking cost can be modified), but it's still an improvement
                Planet planet = World_AIW2.Instance.GetPlanetByIndex( entity.ActiveHack_Planet );
                GameEntity_Squad target = World_AIW2.Instance.GetEntityByID_Squad( entity.ActiveHack_Target );
                if ( entity.ActiveHack.GetIsPerSecondStyleCost() )
                    costForActiveHacks += entity.ActiveHack.GetPerSecondCostToHack( target, planet );
                else
                    costForActiveHacks += entity.ActiveHack.GetLumpSumCostToHack( target, planet );
            }
            return costForActiveHacks;
        }

        #region CalculateCanDoThisHack
        public static bool CalculateCanDoThisHack( ref string lastNoHackReason, GameEntity_Squad TargetToChooseFor, Planet PlanetToChooseFor, HackingType HackTypeToChooseFor )
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null )
            {
                lastNoHackReason = "你没有控制任何派系，因此无法进行黑客行为。";
                return false;
            }

            string rejectionReasonDescription;
            GameEntity_Squad hacker = CalculateHackerForHack( TargetToChooseFor, PlanetToChooseFor, HackTypeToChooseFor, false );
            PlanetFaction localPlanetFaction = PlanetToChooseFor.GetPlanetFactionForFaction( localFaction );

            if ( HackTypeToChooseFor.Implementation.GetCanBeHacked( TargetToChooseFor, hacker, PlanetToChooseFor, localFaction, HackTypeToChooseFor, string.Empty, -1, out rejectionReasonDescription ) != Hackable.CanBeHacked )
            {
                lastNoHackReason = rejectionReasonDescription;
                return false;
            }
            FInt cost = FInt.Zero;
            if ( HackTypeToChooseFor.GetIsPerSecondStyleCost() )
                cost = HackTypeToChooseFor.GetPerSecondCostToHack( TargetToChooseFor, PlanetToChooseFor );
            else
                cost = HackTypeToChooseFor.GetLumpSumCostToHack( TargetToChooseFor, PlanetToChooseFor );

            FInt costForActiveHacks = CalculateActiveHackingCosts(localFaction);
            if ( localFaction.StoredHacking < cost )
            {
                lastNoHackReason = "你没有足够的黑客点。";
                return false;
            }
            if ( localFaction.StoredFactionResourceOne < HackTypeToChooseFor.GetResourceOneCostForTarget( TargetToChooseFor ) )
            {
                if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( localFaction ) )
                    lastNoHackReason = "你没有足够的精华。你需要黑客裂隙或与长老战斗以获得更多精华。";
                else if ( ScourgeInfusedHumanEmpireFactionBaseInfo.GetIsThisAScourgeEmpireFaction( localFaction ))
                    lastNoHackReason = "你没有足够的 Corbomite。";
                else
                    lastNoHackReason = "你没有足够的资源一。";
                return false;
            }
            if ( localFaction.StoredFactionResourceTwo < HackTypeToChooseFor.BaseCostInResourceTwo )
            {
                lastNoHackReason = "你没有足够的资源二。";
                return false;
            }
            if ( localFaction.StoredFactionResourceThree < HackTypeToChooseFor.BaseCostInResourceTwo )
            {
                lastNoHackReason = "你没有足够的资源三。";
                return false;
            }
            if ( localFaction.StoredMetal < HackTypeToChooseFor.BaseCostInMetal )
            {
                lastNoHackReason = "你没有足够的金属。";
                return false;
            }

            if ( localFaction.StoredHacking < cost + costForActiveHacks )
            {
                lastNoHackReason = "由于你正在进行的黑客行为，你没有足够的黑客点。";
                return false;
            }

            if ( localPlanetFaction == null )
            {
                lastNoHackReason = "你在此星球没有本地派系，因此无法进行黑客行为。";
                return false;
            }

            if ( hacker == null )
            {
                lastNoHackReason = "此处没有有效的黑客。";
                return false;
            }

            if ( HackTypeToChooseFor.HackIsAgainstPlanet )
            {
                if ( hacker.ActiveHack != null )
                {
                    lastNoHackReason = "一次只能进行一个探索黑客行为。";
                    return false;
                }
            }
            else
            {
                bool foundActiveHackByMe = false;
                bool foundActiveHackByAnotherFaction = false;
                //do this for ALL factions at the planet
                foreach ( GameEntity_Squad flagship in localPlanetFaction.Planet.Squads( EntityRollupType.Hackers ) )
                {
                    if ( flagship.ActiveHack != null )
                    {
                        if ( flagship.PlanetFaction == localPlanetFaction )
                            foundActiveHackByAnotherFaction = true;
                        else
                            foundActiveHackByMe = true;
                        break;
                    }
                }
                if ( foundActiveHackByMe )
                {
                    lastNoHackReason = "你在此星球上有一个正在进行的黑客行为。";
                    return false;
                }
                if ( foundActiveHackByAnotherFaction )
                {
                    lastNoHackReason = "一个盟友在此星球上有一个正在进行的黑客行为。";
                    return false;
                }
            }

            if ( TargetToChooseFor != null )
            {
                if ( !HackTypeToChooseFor.GetIsHackValidAgainst( TargetToChooseFor, false ) )
                {
                    lastNoHackReason = "此星球上的所有有效目标均不匹配筛选器 " + HackTypeToChooseFor.OnlyForTargetType + "！";
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region TryDoHack
        public static MouseHandlingResult TryDoHack( ref string lastNoHackReason, GameEntity_Squad TargetToChooseFor, Planet PlanetToChooseFor, HackingType HackTypeToChooseFor,
                                                     string RelatedString, int RelatedInteger3, Planet ChosenPlanetForEffect )
        {
            if ( !CalculateCanDoThisHack( ref lastNoHackReason, TargetToChooseFor, PlanetToChooseFor, HackTypeToChooseFor ) )
            {
                return MouseHandlingResult.PlayClickDeniedSound;
            }

            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null )
            {
                //cannot do this in spectator mode
                return MouseHandlingResult.PlayClickDeniedSound;
            }
            PlanetFaction localPFaction = PlanetToChooseFor.GetPlanetFactionForFaction( localFaction );

            GameEntity_Squad hacker = CalculateHackerForHack( TargetToChooseFor, PlanetToChooseFor, HackTypeToChooseFor, true );
            PlanetFaction localPlanetFaction = PlanetToChooseFor.GetPlanetFactionForFaction( localFaction );

            if ( hacker == null )
                return MouseHandlingResult.PlayClickDeniedSound;

            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetActiveHack], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
            command.RelatedString2 = HackTypeToChooseFor.InternalName;
            command.RelatedEntityIDs.Add( hacker.PrimaryKeyID );
            if ( HackTypeToChooseFor.HackIsAgainstPlanet )
                command.RelatedIntegers4.Add( -1 );
            else
                command.RelatedIntegers4.Add( TargetToChooseFor.PrimaryKeyID );
            if ( HackTypeToChooseFor.ChooseASpecificPlanetToTarget )
                command.RelatedIntegers4.Add( ChosenPlanetForEffect.Index );
            else
                command.RelatedIntegers4.Add( PlanetToChooseFor.Index );

            if ( RelatedInteger3 >= 0 )
                command.RelatedIntegers3.Add( RelatedInteger3 );

            if ( RelatedString != null )
                command.RelatedString = RelatedString;

            if ( command.RelatedEntityIDs.Count > 0 )
            {
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                //close this if it's open
                Window_HackChoicesSidebarPopout.Instance.Close();
            }
            else //prevents having a leak!
                command.ReturnToPool();
            return MouseHandlingResult.None;
        }
        #endregion

        #region CalculateListOfTechs_TechVault
        public static void CalculateListOfTechs_TechVault( List<TechUpgrade> results, GameEntity_Squad target, Faction hackerFaction )
        {
            results.Clear();
            HackingUtils.StartListOfTechsForTechVaultStyleGranter_ByTag( results, target, hackerFaction, "Weapon", 2 );
            int totalToAddNext = results.Count - 2;

            //totalToAddNext += 1;
            //totalToAddNext -= HackingUtils.AddToListOfTechsForTechVaultStyleGranter_ByTag( results, target, hackerFaction, "HullStandard", totalToAddNext );

            totalToAddNext += 1;
            totalToAddNext -= HackingUtils.AddToListOfTechsForTechVaultStyleGranter_ByTag( results, target, hackerFaction, "SpecialBonus", totalToAddNext );

            TechUpgradeTable.SortTechList( results );
        }
        #endregion
    }

    public enum HackingExcuse
    {
        NoExcuse = 0,
        NoHackerHere,
        NothingHackableHere
    }
}
