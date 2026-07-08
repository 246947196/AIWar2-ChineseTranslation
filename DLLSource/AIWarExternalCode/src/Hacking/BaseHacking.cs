using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Text;
using UnityEngine;
using Sys=System.Collections.Generic;

namespace Arcen.AIW2.External
{
    public abstract class BaseHackingImplementation : IHackingImplementation
    {
        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "BaseHackingImplementations" );

        public BaseHackingImplementation()
        {
            RefTracker.IncrementObjectCount();
        }

        public virtual Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, 
            Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            RejectionReasonDescription = string.Empty;
            return Hackable.CanBeHacked;
        }

        public virtual bool GetDoesHackRequireASinglularChoiceFromASubmenu()
        {
            return false;
        }

        public virtual bool GetIsHackOfSelfUnit()
        {
            return false;
        }

        public virtual void AddAllButtonsForSingularChoiceInSubMenu( GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, Faction HackerFaction, HackingType HackType, ArcenUI_SetOfCreateElementDirectives Set,
            ref float runningY, UnityEngine.Rect firstBounds, float rowBuffer, AddHackButtonToWindow WindowAdder )
        {
        }

        public virtual void GetMinAndMaxCostToHackForSidebar( GameEntity_Squad Target, Planet planet, Faction hackerFaction, HackingType Type, out FInt MinCost, out FInt MaxCost )
        {
            FInt hackCost = Type.GetHackPointCostForTarget( Target );
            MinCost = hackCost;
            MaxCost = hackCost;
        }

        public virtual int GetTotalSecondsToHack( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, HackingType type )
        {
            return type.GetEffectiveHackDuration( Target, planet );
        }

        public virtual bool CheckIfHackIsDone_OnlyIfPerSecondStyleCost( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, HackingType type )
        {
            //This is used for effect-per-second hacks like Science or Superterminal
            return true;
        }

        public virtual bool WriteAnySpecialDisplayCodeForHackedShipTooltip( GameEntity_Squad Target, Faction HackerFaction, ArcenCharacterBufferBase Buffer, BaseTooltipDetail TooltipDetail )
        {
            return false;
        }
        
        private static bool printToLog = false;
        public virtual int EstimateTotalDifficulty( HackingType type, GameEntity_Squad TargetOrNull, Planet PlanetOrNull, out int totalResponseStrengthEstimate, out ArcenCharacterBuffer debugLogOrNull, bool assumeAIStrengthLevels )
        {
            byte difficultyEstimate = 1;
            int debugStage = 1;
            totalResponseStrengthEstimate = -1;
            bool generateDebugLog = GameSettings.Current.GetBoolBySetting( "HackingDebug" );
            if ( generateDebugLog )
                debugLogOrNull = ArcenCharacterBuffer.GetFromPoolOrCreate( "BaseHackingImplementation-debugLogOrNull" );
            else
                debugLogOrNull = null;
            try
            {
                //we can give a (cruder) estimate without a planet or a target explicitly specified
                int numPrimaryEffects = 0;
                int numSecondaryEffects = 0;
                int numTertiaryEffects = 0;

                debugStage = 100;
                //Figure out multiplier
                Faction targetFactionOrNull = null;
                if ( type.HackIsAgainstPlanet && PlanetOrNull != null )
                    targetFactionOrNull = PlanetOrNull.GetControllingOrInfluencingFaction();
                else if ( TargetOrNull != null )
                    targetFactionOrNull = TargetOrNull.GetFactionOrNull_Safe();

                if ( targetFactionOrNull?.Type == FactionType.NaturalObject ||
                     targetFactionOrNull?.Type == FactionType.Player ||
                     type.HackingResponseIsAsAI)
                {
                    targetFactionOrNull = PlanetOrNull?.GetControllingOrInfluencingFaction().Type == FactionType.AI ? PlanetOrNull.GetControllingOrInfluencingFaction() : PlanetOrNull?.GetInitialControllingAIFactionOrNull();
                }

                if ( generateDebugLog )
                    debugLogOrNull.Add( "Estimate log for " ).Add( type.Name );
                if ( targetFactionOrNull != null && generateDebugLog )
                    debugLogOrNull.Add( " using faction " ).Add( targetFactionOrNull.GetDisplayName() ).Add( " as the faction for these calculations." );
                debugStage = 200;

                if ( !ShouldRespondToHack( targetFactionOrNull, generateDebugLog, debugLogOrNull ) )
                    return 0;

                if ( targetFactionOrNull == null || targetFactionOrNull.Type == FactionType.NaturalObject )
                {
                    if ( World_AIW2.Instance.AIFactions.Count > 0 )
                    {
                        if ( generateDebugLog )
                            debugLogOrNull.Add( " Using first random AI faction. " );
                        targetFactionOrNull = World_AIW2.Instance.AIFactions[0];
                    }
                }

                debugStage = 300;

                FInt multiplier = targetFactionOrNull == null ? FInt.One : GetHackingLevelMultiplier( targetFactionOrNull );
                FInt postMultiplier = multiplier;
                if ( !type.GetIsPerSecondStyleCost() && targetFactionOrNull != null)
                {
                    postMultiplier = GetHackingLevelMultiplier( targetFactionOrNull, type.GetLumpSumCostToHack( TargetOrNull, PlanetOrNull ).IntValue ); //after the hack is done
                    if ( generateDebugLog )
                        debugLogOrNull.Add( "base multiplier " ).Add( multiplier ).Add( " postHackMultiplier " ).Add( postMultiplier ).Add( " cost to hack: " ).Add( type.GetLumpSumCostToHack( TargetOrNull, PlanetOrNull ).IntValue ).Add( "\n" );
                }
                int timeToUse = type.GetEffectiveHackDuration( TargetOrNull, PlanetOrNull );

                FInt multiplierFromRepeatHacksAgainstSameTarget = FInt.One;
                if ( type.ResponseStrengthMultiplierPerTimeHackedSameTarget > FInt.One && TargetOrNull != null && TargetOrNull.FleetMembership != null )
                {
                    int timesHacked = TargetOrNull.GetNumberOfTimesHacked( type );
                    for ( int i = 0; i < timesHacked; i++ )
                        multiplierFromRepeatHacksAgainstSameTarget *= type.ResponseStrengthMultiplierPerTimeHackedSameTarget;
                }

                debugStage = 400;

                int primaryInterval = type.GetPrimaryHackResponseInterval();
                if ( type.PrimaryResponseStrengthPerInterval != FInt.Zero && primaryInterval > 0 )
                {
                    numPrimaryEffects = timeToUse / primaryInterval;
                    if (  timeToUse == primaryInterval )
                        numPrimaryEffects--; //we don't trigger the effect if it would happen the second the hack ends
                }
                debugStage = 500;
                int secondaryInterval = type.GetSecondaryHackResponseInterval();
                if ( type.SecondaryResponseStrengthPerInterval != FInt.Zero && secondaryInterval > 0 )
                {
                    numSecondaryEffects = timeToUse / secondaryInterval;
                    if ( timeToUse == secondaryInterval )
                        numSecondaryEffects--; //we don't trigger the effect if it would happen the second the hack ends
                }
                debugStage = 600;
                int tertiaryInterval = type.GetTertiaryHackResponseInterval();
                if ( type.TertiaryResponseStrengthPerInterval != FInt.Zero && tertiaryInterval > 0 )
                {
                    numTertiaryEffects = timeToUse / tertiaryInterval;
                    if ( timeToUse == tertiaryInterval )
                        numTertiaryEffects--; //we don't trigger the effect if it would happen the second the hack ends
                }

                debugStage = 700;
                FInt primaryStrength = (type.PrimaryResponseStrengthPerInterval * multiplier * multiplierFromRepeatHacksAgainstSameTarget) * numPrimaryEffects;
                if ( generateDebugLog )
                    debugLogOrNull.Add( "primaryStrength " ).Add( primaryStrength ).Add( " = " ).Add( type.PrimaryResponseStrengthPerInterval ).Add( " * " ).Add( multiplier ).Add( " * " ).Add( multiplierFromRepeatHacksAgainstSameTarget ).Add( " * " ).Add( numPrimaryEffects ).Add( "\n" );
                debugStage = 800;
                FInt secondaryStrength = (type.SecondaryResponseStrengthPerInterval * multiplier * multiplierFromRepeatHacksAgainstSameTarget) * numSecondaryEffects;
                debugStage = 900;
                FInt tertiaryStrength = (type.TertiaryResponseStrengthPerInterval * multiplier * multiplierFromRepeatHacksAgainstSameTarget) * numTertiaryEffects;
                debugStage = 1000;

                //note we use the postMultiplier since this is calculated after the hack is done
                FInt completionStrength = type.ResponseStrengthOnCompletion * ( postMultiplier ) * multiplierFromRepeatHacksAgainstSameTarget;
                if ( generateDebugLog )
                    debugLogOrNull.Add( "completion strength rundown: " ).Add( completionStrength ).Add( " = " ).Add( type.ResponseStrengthOnCompletion ).Add( " * " ).Add( postMultiplier ).Add( " * " ).Add( multiplierFromRepeatHacksAgainstSameTarget ).Add( ".\n" );
                //
                FInt exoStrength = FInt.Zero;
                Faction aiFactionToUse = targetFactionOrNull;
                if (aiFactionToUse == null || aiFactionToUse.Type != FactionType.AI )
                    aiFactionToUse = World_AIW2.GetRandomAIFaction( null );
                AISentinelsCoreData sentinelInfo = aiFactionToUse.TryGetAISentinelsCoreData()?.SentinelInfo;

                if ( type.ExoOnCompletion  && sentinelInfo != null )
                {
                    int campaignMultiplier = AIWar2GalaxySettingQuickAccess.PostHackingExoStrengthMultiplier;

                    FInt postHackExoWaveSize = (sentinelInfo.AIDifficulty.BaseHackingWaveSize * type.ResponseStrengthOnCompletion );
                    exoStrength = postHackExoWaveSize * ( postMultiplier) * campaignMultiplier;
                    if ( generateDebugLog )
                        debugLogOrNull.Add( "exoStrength " ).Add( exoStrength ).Add( " = " ).Add( postHackExoWaveSize ).Add( " * " ).Add( postMultiplier ).Add( " * " ).Add( campaignMultiplier ).Add( "\n" );
                }
                debugStage = 1100;

                if ( (TargetOrNull != null && ( TargetOrNull.GetFactionTypeSafe() == FactionType.AI || assumeAIStrengthLevels ) && sentinelInfo != null ) ||
                     (PlanetOrNull != null && targetFactionOrNull?.Type == FactionType.AI && sentinelInfo != null ) ) //also allow hacks against planets to be counted
                {
                    //compute the total strength response estimate.
                    //Hacks against minor factions all have their own mechanisms, so these calculations will not work.
                    //I'm not sure if there are any weird outlying anti-planet hacks so skipping those (usually they aren't too dangerous, I think?)
                    //7/4/21: If you have vetted a minor faction response strength and it seems safe to reuse these calculations then the minor faction should
                    //pass in assumeAIStrengthLevels as true and you will redo these calculations as if the responder was an AI
                    int HackingWaveSize = sentinelInfo.AIDifficulty.BaseHackingWaveSize;
                    FInt totalMultiplier = primaryStrength + secondaryStrength + tertiaryStrength + completionStrength;

                    // HackingWaveSize += 10; //a fudge factor, because I'd rather a hack look too scary than too weak. This used to be 500, but that was much too high
                    // if ( assumeAIStrengthLevels )
                    //     HackingWaveSize += 10; //a bit more of a fudge factor

                    totalResponseStrengthEstimate = (totalMultiplier * HackingWaveSize).IntValue;
                    FInt aipMultiplier = sentinelInfo.AIDifficulty.HackingAipMultiplier;
                    int bonusAIPStrength = 0;
                    int responseStrengthBeforeAIPMultiplier = 0;
                    FInt AIP = GlobalAIWorldBaseInfo.Instance.AIProgress_Effective;
                    if ( aipMultiplier > FInt.Zero )
                    {
                        bonusAIPStrength = (aipMultiplier * AIP * totalResponseStrengthEstimate).IntValue;
                        responseStrengthBeforeAIPMultiplier = totalResponseStrengthEstimate;
                    }

                    totalResponseStrengthEstimate += bonusAIPStrength;
                    FInt fallenSpireMultiplier = FallenSpireFactionBaseInfo.GetFallenSpireGeneralAIResponseMultiplier();
                    int fallenSpireBonusStrength = (totalResponseStrengthEstimate * fallenSpireMultiplier).IntValue;
                    totalResponseStrengthEstimate += fallenSpireBonusStrength;
                    totalResponseStrengthEstimate += exoStrength.IntValue;
                    if ( generateDebugLog )
                        debugLogOrNull.Add( "wave size to use: " ).Add( HackingWaveSize ).Add( " mult from prim/second/tert/complete " ).Add( totalMultiplier ).Add( " bonus AIP strength " ).Add( bonusAIPStrength ).Add( " (" ).Add( aipMultiplier ).Add( " * " ).Add( AIP + " * " ).Add( responseStrengthBeforeAIPMultiplier ).Add( "). Fallen spire bonus " ).Add( fallenSpireBonusStrength ).Add( ", bonus from exo strike: " ).Add( exoStrength ).Add( "\n" );
                    //Note that we are using the "budget", which is (I believe) in units of CostForAIToPurchase, not strength.
                    //I've run some experiments though and this seems a pretty reasonable, if imprecise, proxy.
                }
                else
                    totalResponseStrengthEstimate = -1;

                difficultyEstimate = (byte)(primaryStrength.IntValue + secondaryStrength.IntValue + tertiaryStrength.IntValue + completionStrength.IntValue + exoStrength.IntValue );
                debugStage = 1200;
                if ( type.ResponseStrengthMultiplierPerEffect > FInt.Zero && type.GetIsPerSecondStyleCost() )
                {
                    //a crude increase for the superterminal
                    difficultyEstimate *= 4;
                }
                debugStage = 1400;
                if ( generateDebugLog )
                {
                    debugLogOrNull.Add( " Total Estimate: " ).Add( totalResponseStrengthEstimate ).Add( " (" ).Add( (totalResponseStrengthEstimate/1000) ).Add( "). Breakdown: primary " ).Add( primaryStrength ).Add( ", secondary " ).Add( secondaryStrength ).Add( " tertiary " ).Add( tertiaryStrength ).Add( ", completion " ).Add( completionStrength ).Add( " ( " ).Add( type.ResponseStrengthMultiplierPerEffect ).Add( " * " ).Add( postMultiplier + " * " ).Add( multiplierFromRepeatHacksAgainstSameTarget ).Add( "), exo " ).Add( exoStrength ).Add( ". time " ).Add( timeToUse ).Add( " difficulty estimate: " ).Add( difficultyEstimate );
                    if ( targetFactionOrNull != null )
                        debugLogOrNull.Add( " targetFaction " ).Add( targetFactionOrNull.GetDisplayName() ).Add( " idx " ).Add( targetFactionOrNull.FactionIndex );
                    if ( !printToLog && type.ChooseASpecificShipLineToGrant )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine(debugLogOrNull.ToString(), Verbosity.DoNotShow );
                        printToLog = true;
                    }
                }
            }
            catch ( Exception e )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "EstimateTotalDifficulty exception at debugStage " + debugStage + " for " + type.InternalName + ": " + e, Verbosity.ShowAsError );
            }
            return difficultyEstimate;
        }

        public virtual bool ShouldRespondToHack( Faction TargetFactionOrNull, bool GenerateDebugLog, ArcenCharacterBufferBase DebugLog )
        {
            if ( TargetFactionOrNull != null )
            {
                if ( TargetFactionOrNull.SpecialFactionData.CeasesHackResponsesIfDefeated )
                {
                    if ( GenerateDebugLog )
                        DebugLog.Add( " Target has CeasesHackResponsesIfDefeated, checking." );
                    if ( !TargetFactionOrNull.CheckBlocksVictory() )
                    {
                        if ( GenerateDebugLog )
                            DebugLog.Add( " Returning 0 because the faction ceases hack responses if defeated, and it either has the FactionIsDefeated flag set or no does not block victories at the moment." );
                        return false;
                    } else
                    {
                        if ( GenerateDebugLog )
                            DebugLog.Add( " Target is blocking victory, returning true." );
                    }
                }
                if ( TargetFactionOrNull.SpecialFactionData.CeasesHackResponsesIfAllAIsAreDefeated )
                {
                    if ( GenerateDebugLog )
                        DebugLog.Add( " Target has CeasesHackResponsesIfAllAIsAreDefeated, checking." );
                    bool foundLivingAI = false;
                    foreach ( Faction aiFaction in World_AIW2.Instance.AIFactions )
                    {
                        if ( aiFaction.CheckBlocksVictory() )
                        {
                            foundLivingAI = true;
                            break;
                        }
                    }
                    if ( !foundLivingAI )
                    {
                        if ( GenerateDebugLog )
                            DebugLog.Add( " Returning 0 because the faction ceases hack responses if all AIs have been defeated, they all were." );
                        return false;
                    } else
                    {
                        if ( GenerateDebugLog )
                            DebugLog.Add( " Found living AI, returning true." );
                    }
                }
            } else
            {
                if ( GenerateDebugLog )
                    DebugLog.Add( " Target faction was null, return true." );
            }
            return true;
        }

        public void DoOneSecondOfHackingLogic_CalledFromMainSimOnly(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            int debugCode = 0;
            try
            {
                debugCode = 8;
                bool debug = GameSettings.Current.GetBoolBySetting( "HackingDebug" );
                Faction targetFaction = null;
                debugCode = 10;
                if ( type.HackIsAgainstPlanet )
                    targetFaction = planet.GetControllingOrInfluencingFaction();
                else
                    targetFaction = Target.GetFactionOrNull_Safe();

                debugCode = 20;
                if ( targetFaction.Type == FactionType.NaturalObject ||
                     targetFaction.Type == FactionType.Player ||
                     FactionUtilityMethods.Instance.IsFactionAlliedToAnyPlayer( targetFaction ) ||
                     type.HackingResponseIsAsAI)
                {
                    targetFaction = planet.GetControllingOrInfluencingFaction().Type == FactionType.AI
                        ? planet.GetControllingOrInfluencingFaction()
                        : planet.GetInitialControllingAIFactionOrNull();

                    // Can occur if we use the debug homeworld option.
                    if ( targetFaction == null )
                        targetFaction = FactionUtilityMethods.Instance.GetRandomAIFactionWithDifficulty( Context, 1 );
                }

                debugCode = 30;
                if ( type.GetIsPerSecondStyleCost() )
                {
                    debugCode = 31;
                    FInt perSecondCost = type.GetPerSecondCostToHack( Target, planet );
                    if ( type.HackingPointsAgainstThisFactionPercentageRecordedModifer > FInt.Zero )
                    {
                        debugCode = 32;
                        Faction facOrNull = Target.GetFactionOrNull_Safe();
                        if ( facOrNull != null )
                            facOrNull.HackingPointsUsedAgainstThisFaction += (perSecondCost * type.HackingPointsAgainstThisFactionPercentageRecordedModifer);
                    }

                    debugCode = 33;
                    {
                        Faction facOrNull = Hacker.GetFactionOrNull_Safe();
                        debugCode = 34;
                        if ( facOrNull != null )
                            facOrNull.StoredHacking -= perSecondCost;
                    }

                    debugCode = 35;
                    Event.HackingPointsSpent += perSecondCost;
                }

                debugCode = 380;
                ArcenCharacterBuffer debugBuffer = null;
                if ( debug )
                    debugBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "DoOneSecondOfHackingLogic_CalledFromMainSimOnly-debugBuffer" );
                bool shouldRespondToHacks = ShouldRespondToHack( targetFaction, false, debugBuffer );
                if( !shouldRespondToHacks && debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Skipping all responses to " + type.GetDisplayName() + "> against planet " + planet.Name + " because the target faction " + targetFaction + ": " + debugBuffer.ToString(), Verbosity.DoNotShow );
                if (debugBuffer != null)
                    debugBuffer.ReturnToPool();
                debugCode = 381;
                FInt multiplier = GetHackingLevelMultiplier( targetFaction );
                int primaryInterval = type.GetPrimaryHackResponseInterval();
                int maxTimeForEffect = type.GetEffectivePerEffectIncreaseDuration( Target, planet );

                debugCode = 382;
                int timeForEffect = Hacker.ActiveHack_DurationThusFar;
                if ( maxTimeForEffect > 0 && timeForEffect > maxTimeForEffect )
                    timeForEffect = maxTimeForEffect;

                debugCode = 390;
                int numEffectsThusFar = primaryInterval <= 0 ? 0 : timeForEffect / primaryInterval;
                if ( debug && Hacker.ActiveHack_DurationThusFar == 1 )
                {
                    if (  !type.HackIsAgainstPlanet  )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Generic DoOneSecond " + Hacker.ActiveHack_DurationThusFar + " <" + type.GetDisplayName() + "> against target " + Target.TypeData.GetDisplayName() + " faction type " + targetFaction.Type + " and name " + targetFaction.GetDisplayName() +" idx " + targetFaction.FactionIndex +". multiplier from hacking points spent " + multiplier, Verbosity.DoNotShow );
                    else if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Generic DoOneSecond " + Hacker.ActiveHack_DurationThusFar + " <" + type.GetDisplayName() + "> against planet " + planet.Name, Verbosity.DoNotShow );
                }

                debugCode = 391;
                FInt multiplierFromRepeatHacksAgainstSameTarget = FInt.One;
                if ( type.ResponseStrengthMultiplierPerTimeHackedSameTarget > FInt.One && Target != null && Target.FleetMembership != null )
                {
                    int timesHacked = Target.GetNumberOfTimesHacked( type );
                    for ( int i = 0; i < timesHacked; i++ )
                        multiplierFromRepeatHacksAgainstSameTarget *= type.ResponseStrengthMultiplierPerTimeHackedSameTarget;
                }

                debugCode = 40;
                if ( Hacker.ActiveHack_DurationThusFar >= type.GetEffectiveHackDuration( Target, planet ) )
                {
                    debugCode = 50;
                    if ( this.DoSuccessfulCompletionLogic_CalledFromMainSimOnly( Target, planet, Hacker, Context, type, Event ) )
                    {
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            FactionViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<FactionViewChatHandlerBase>( "HackingHistory" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.Faction = LazyLoadFactionWrapper.Create( Hacker.GetFactionOrNull_Safe() );
                            if ( !type.GetIsPerSecondStyleCost() )
                                World_AIW2.Instance.QueueChatMessageOrCommand( "黑客 <color=#a1ff22>" + type.GetDisplayName( Event.HackedEntityTypeData, false ) +
                                      "</color> 成功。", ChatType.LogToCentralChat, chatHandlerOrNull );
                        }
                    }

                    if ( shouldRespondToHacks && type.ResponseStrengthOnCompletion > FInt.Zero )
                    {
                        FInt hackingMultiplier = GetHackingLevelMultiplier( targetFaction );
                        FInt finalResponseStrength = ((type.ResponseStrengthOnCompletion * hackingMultiplier)) * multiplierFromRepeatHacksAgainstSameTarget;
                        if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine( "Using post-hack pulse with strength " + finalResponseStrength + ". hackingMult " + hackingMultiplier + " * responsestrength " + type.ResponseStrengthOnCompletion + " * repeate attempt mult " + multiplierFromRepeatHacksAgainstSameTarget, Verbosity.DoNotShow );
                        targetFaction.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, finalResponseStrength * Hacker.TypeData.HackingEffectMultiplier, Context, Event, targetFaction );
                    }

                    return;
                }

                // not done, just advancing a second as normal

                debugCode = 60;
                if ( shouldRespondToHacks && Hacker.ActiveHack_DurationThusFar % primaryInterval == 0 )
                {
                    FInt responseLevel = type.PrimaryResponseStrengthPerInterval;
                    responseLevel += type.PrimaryResponseStrengthIncreasePerEffect * numEffectsThusFar;
                    FInt response = (responseLevel * multiplier) * multiplierFromRepeatHacksAgainstSameTarget;
                    if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine( "Using primary response " + response + " = " + responseLevel + " * " + multiplier + " = " + responseLevel * multiplier, Verbosity.DoNotShow );

                    targetFaction.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, response, Context, Event, targetFaction );
                }
                debugCode = 70;
                if ( shouldRespondToHacks && type.SecondaryResponseStrengthPerInterval > FInt.Zero ) //if secondary enabled
                {
                    debugCode = 71;
                    FInt responseLevel = type.SecondaryResponseStrengthPerInterval * Hacker.TypeData.HackingEffectMultiplier;
                    responseLevel += type.SecondaryResponseStrengthIncreasePerEffect * numEffectsThusFar;
                    debugCode = 72;
                    if ( Hacker.ActiveHack_DurationThusFar % type.GetSecondaryHackResponseInterval() == 0 )
                    {
                        debugCode = 73;
                        FInt response = (responseLevel * multiplier) * multiplierFromRepeatHacksAgainstSameTarget;
                        if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine( "using secondary response " + response + " = " + responseLevel + " * " + multiplier + " * " + Hacker.TypeData.HackingEffectMultiplier, Verbosity.DoNotShow );
                        if ( targetFaction == null )
                            throw new Exception("Whoops, targetFaction is null");
                        targetFaction.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, response, Context, Event, targetFaction );
                    }
                }
                debugCode = 80;
                if ( shouldRespondToHacks && type.TertiaryResponseStrengthPerInterval > FInt.Zero ) //if tertiary enabled
                {
                    FInt responseLevel = type.TertiaryResponseStrengthPerInterval * Hacker.TypeData.HackingEffectMultiplier;
                    responseLevel += type.TertiaryResponseStrengthIncreasePerEffect * numEffectsThusFar;
                    if ( Hacker.ActiveHack_DurationThusFar % type.GetTertiaryHackResponseInterval() == 0 )
                    {
                        if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine( "using tertiary response " + responseLevel + " * " + multiplier + " * " + Hacker.TypeData.HackingEffectMultiplier, Verbosity.DoNotShow );
                        targetFaction.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, (responseLevel * multiplier) * multiplierFromRepeatHacksAgainstSameTarget, Context, Event, targetFaction );
                    }
                }
                debugCode = 90;
                if ( shouldRespondToHacks && type.PeriodicExoStrength > FInt.Zero )
                {
                    if ( Hacker.ActiveHack_DurationThusFar % type.PeriodicExoInterval == 0 )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "using periodic exo response " + type.PeriodicExoStrength + " * " + multiplier, Verbosity.DoNotShow );
                        if ( targetFaction.Type != FactionType.AI )
                            targetFaction = World_AIW2.GetRandomAIFaction( Context );
                        AISentinelsCoreData factionExternal = targetFaction.TryGetAISentinelsCoreData()?.SentinelInfo;
                        GameEntity_Squad king = null;
                        Faction facOrNull = Hacker.GetFactionOrNull_Safe();
                        if ( facOrNull != null )
                            king = facOrNull.GetFirstMatching( EntityRollupType.KingUnitsOnly, false, false );
                        ExoOptions options = ExoOptions.CreateWithDefaults( king, (factionExternal.AIDifficulty.BaseHackingWaveSize * 
                            type.PeriodicExoStrength * multiplier * multiplierFromRepeatHacksAgainstSameTarget).IntValue, targetFaction, targetFaction );
                        ExoGalacticDeepLinkRoot.Instance.SendExoGalacticAttack( options, Context );
                    }
                }

                debugCode = 102;
                this.DoOneSecondOfHackingLogic_HackSpecificLogic( Target, planet, Hacker, Context, type, Event );
            }
            catch ( Exception e )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    LOG.Err( "Exception hit during Base DoOneSecondOfHackingLogic_AsPartOfMainSim debugcode={0}:\n{1}", debugCode, e);
            }
        }

        public virtual void DoOneSecondOfHackingLogic_HackSpecificLogic(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
        }

        #region GetHackDataForTargetOrNull
        public HackData GetHackDataForTargetOrNull( GameEntity_Squad Target, HackingType hackingType )
        {
            if ( Target == null || Target.TypeData == null )
                return null;
            return Target.TypeData.GetHackDataOrNull( hackingType );
        }
        #endregion

        #region GetNumberOfTimesHacked
        public int GetNumberOfTimesHacked( GameEntity_Squad Target, HackingType type )
        {
            if ( Target == null )
                return 0;
            return Target.GetNumberOfTimesHacked( type );
        }
        #endregion

        public virtual FInt GetHackingLevelMultiplier( Faction targetFaction, int bonusHackingPointsForCalculation = 0 )
        {
            if ( targetFaction == null )
                return FInt.One;

            //here is where I use the current Hacking Level to let me adjust the response
            //TODO: use the difficulty to figure out how to adjust the response. This basically consists of
            //looking up which index of the table we should use, then the appropriate multiplier for that level
            //finish filling out the XML with difficulty levels for hacking level
            if ( targetFaction.Type != FactionType.AI )
            {
                FInt modifiedHackingLevel = FInt.FromParts( 0, 500 );
                if ( modifiedHackingLevel * targetFaction.HackingPointsUsedAgainstThisFaction < FInt.One )
                    return FInt.One;
                else
                    return modifiedHackingLevel * targetFaction.HackingPointsUsedAgainstThisFaction;
            }

            AIDifficulty difficulty = targetFaction.TryGetAISentinelsCoreData().SentinelInfo.AIDifficulty;
            if ( difficulty == null ) //huh!  Use the highest difficulty, then.
                difficulty = FactionUtilityMethods.Instance.GetHighestAIDifficulty_AsDifficulty();
            if ( difficulty == null ) //huh!  Ignore all that, then.
                return FInt.One;

            //AISentinelsCoreData factionExternal = targetFaction.GetSentinelsExternal()?.SentinelInfo;
            FInt hackingSoFar = targetFaction.HackingPointsUsedAgainstThisFaction + bonusHackingPointsForCalculation;
            int hackingLevel = 0;
            for ( int i = 0; i < difficulty.HackingDifficultyLevel.Count; i++ )
            {
                if ( hackingSoFar <= difficulty.HackingDifficultyLevel[i] )
                {
                    break;
                }

                hackingLevel = i;
            }

            FInt multiplier = difficulty.HackingDifficultyMultiplier[hackingLevel];
            return multiplier;
        }

        public bool DoSuccessfulCompletionLogic_CalledFromMainSimOnly(
            GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                bool debug = GameSettings.Current.GetBoolBySetting( "HackingDebug" );
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Generic DoSuccessfulCompletionLogic  <" + type.GetDisplayName() + ">", Verbosity.DoNotShow );
                
                debugCode = 200;
                Faction facOrNull = Hacker.GetFactionOrNull_Safe();
                if ( type.ScienceToGrantOnCompletion > 0 )
                {
                    debugCode = 210;
                    if ( facOrNull != null )
                        facOrNull.StoredScience += type.ScienceToGrantOnCompletion;
                    Event.ScienceGained = type.ScienceToGrantOnCompletion;
                }

                debugCode = 400;
                FInt hackCost = FInt.Zero;
                if ( !type.GetIsPerSecondStyleCost() )
                    hackCost = type.GetLumpSumCostToHack( TargetOrNull, planet );

                debugCode = 500;
                if ( facOrNull != null )
                {
                    facOrNull.StoredHacking -= hackCost;
                    
                    if ( type.BaseCostInResourceOne > FInt.Zero )
                        facOrNull.StoredFactionResourceOne -= type.GetResourceOneCostForTarget( TargetOrNull );
                    
                    if ( type.BaseCostInResourceTwo > FInt.Zero )
                        facOrNull.StoredFactionResourceTwo -= type.BaseCostInResourceTwo;
                    
                    if ( type.BaseCostInResourceThree > FInt.Zero )
                        facOrNull.StoredFactionResourceThree -= type.BaseCostInResourceThree;
                    
                    if ( type.BaseCostInMetal > FInt.Zero )
                        facOrNull.StoredMetal -= type.BaseCostInMetal;
                }

                Event.HackingPointsSpent += hackCost;
                Event.HackingPointsLeftAfterHack = FInt.Zero;
                
                if ( facOrNull != null )
                {
                    Event.HackingPointsLeftAfterHack = facOrNull.StoredHacking;
                }
                if ( TargetOrNull != null )
                    Event.HackedEntityTypeData = TargetOrNull.TypeData;

                debugCode = 600;
                if ( type.HackingPointsAgainstThisFactionPercentageRecordedModifer > FInt.Zero )
                {
                    if ( TargetOrNull != null )
                    {
                        Faction targetFacOrNull = TargetOrNull.GetFactionOrNull_Safe();
                        if ( targetFacOrNull != null )
                            targetFacOrNull.HackingPointsUsedAgainstThisFaction += (hackCost * type.HackingPointsAgainstThisFactionPercentageRecordedModifer);
                    }
                }

                debugCode = 700;
                bool shouldRespondToHacks = ShouldRespondToHack( TargetOrNull?.PlanetFaction?.Faction, false, null );
                
                if ( debug && !shouldRespondToHacks )
                    ArcenDebugging.ArcenDebugLogSingleLine( "\tSkipping all final responses because the target faction " + TargetOrNull?.PlanetFaction.Faction?.GetDisplayName() + " stopped hacking responses.", Verbosity.DoNotShow );
                
                if ( shouldRespondToHacks && type.ExoOnCompletion )
                {
                    debugCode = 800;
                    //send an exo wave against Hacker's king
                    GameEntity_Squad king = Hacker.GetFirstMatchingInFaction( EntityRollupType.KingUnitsOnly, false, false );
                    Faction factionToSendExo = null;
                    FInt multiplier = FInt.One;
                    if ( TargetOrNull != null )
                        factionToSendExo = TargetOrNull.GetFactionOrNull_Safe();
                    else if ( planet != null && planet.GetControllingOrInfluencingFaction().Type == FactionType.AI )
                        factionToSendExo = planet.GetControllingOrInfluencingFaction();

                    //make sure we have a valid faction (use an AI if we didn't already have one)
                    if ( factionToSendExo == null || factionToSendExo.Type != FactionType.AI )
                        factionToSendExo = World_AIW2.Instance.AIFactions[Context.RandomToUse.Next( 0, World_AIW2.Instance.AIFactions.Count )];

                    debugCode = 900;
                    multiplier = GetHackingLevelMultiplier( factionToSendExo );
                    AISentinelsCoreData factionExternal = factionToSendExo.TryGetAISentinelsCoreData()?.SentinelInfo;
                    FInt PostHackExoWaveSize = (factionExternal.AIDifficulty.BaseHackingWaveSize * type.ResponseStrengthOnCompletion * multiplier *
                                                Hacker.TypeData.HackingEffectMultiplier);

                    int campaignMultiplier = AIWar2GalaxySettingQuickAccess.PostHackingExoStrengthMultiplier;
                    PostHackExoWaveSize = campaignMultiplier * PostHackExoWaveSize;

                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "\t" + factionToSendExo.ToString() + " Sending an Exo of size " + factionExternal.AIDifficulty.BaseHackingWaveSize + " * " + type.ResponseStrengthOnCompletion + " * " + multiplier + " (*= campaign multiplier " + campaignMultiplier +" = " + PostHackExoWaveSize +".", Verbosity.DoNotShow );

                    debugCode = 1000;
                    if ( ArcenNetworkAuthority.GetIsHostMode() )
                    {
                        PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                        if ( chatHandlerOrNull != null )
                            chatHandlerOrNull.PlanetToView = planet;

                        World_AIW2.Instance.QueueChatMessageOrCommand(
                            "AI 正在对你对 " + planet.Name + " 的黑客行为发动反击。", ChatType.LogToCentralChat, string.Empty,
                            chatHandlerOrNull );
                    }

                    ExoOptions options = ExoOptions.CreateWithDefaults( king, PostHackExoWaveSize.IntValue, factionToSendExo, factionToSendExo );

                    debugCode = 1100;
                    ExoGalacticDeepLinkRoot.Instance.SendExoGalacticAttack( options, Context );

                    if ( Event != null ) //also include the exo response in the "approx response strength" calculation
                        Event.ApproxResponseStrength += PostHackExoWaveSize.IntValue;
                }

                debugCode = 2000;
                if ( type.AIPOnCompletion > 0 )
                {
                    debugCode = 2100;
                    FInt AIPToIncrease = (FInt) type.AIPOnCompletion;
                    if ( TargetOrNull == null )
                    {
                        GlobalAIWorldBaseInfo.Instance.ChangeAIP(
                            AIPToIncrease, AIPChangeReason.Hacking, null, Hacker.GetFactionIndex_Safe(), planet.Index, -1 );
                    }
                    else
                    {
                        GlobalAIWorldBaseInfo.Instance.ChangeAIP(
                            AIPToIncrease, AIPChangeReason.Hacking, TargetOrNull.TypeData, Hacker.GetFactionIndex_Safe(), TargetOrNull.Planet.Index,
                            TargetOrNull.GetFactionIndex_Safe() );
                    }

                    Event.AIPchanged = AIPToIncrease;
                }

                debugCode = 3000;
                if ( shouldRespondToHacks && type.WaveOnCompletion )
                {
                    //send an normal wave against the hacking faction. Not implemented yet
                }

                debugCode = 3100;
                this.DoSuccessfulCompletionLogic_Extra( TargetOrNull, planet, Hacker, Context, type, Event );

                debugCode = 4000;

                // check for achievements
                if ( Hacker.GetIsFactionControlledByLocalPlayerAccount_Safe() )
                    type.TriggerAnyAchievementsRelatedToThisHack();

                debugCode = 5000;

                //does this thing die?  Or does it keep track of how many times it was hacked?
                this.HandleDestructionOrIncrementing( TargetOrNull, type, Context, debug );

                //on hack completion, make sure this faction gets sync'd promptly to clients
                if ( World_AIW2.Instance.GetIsHostAnyShouldPrepareToSendNewEntitiesToClients() ) 
                    World_AIW2.Instance.OnServer_FactionsToFastBlastToClients.Enqueue( Hacker.PlanetFaction.Faction );
            }
            catch ( Exception e )
            {
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                    return true; //errors happen on teh client.  No biggie.
                
                LOG.Err( "Exception hit during DoSuccessfulCompletionLogic_CalledFromMainSimOnly debugcode={0}:\n{1}", debugCode, e);

                return false;
            }

            return true;
        }

        private void HandleDestructionOrIncrementing( GameEntity_Squad TargetOrNull, HackingType type, ArcenHostOnlySimContext Context, bool debug )
        {
            if ( TargetOrNull == null )
                return;

            int numTimes = TargetOrNull.IncrementNumberOfTimesHacked( type );
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "\tThis has been hacked " + numTimes, Verbosity.DoNotShow );
            
            if ( !type.DestroyHackTargetAtCompletion )
                return;

            if ( numTimes < type.NumberOfTimesIndividualUnitCanBeHacked )
                return;
            
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "\tAttempting to kill " + TargetOrNull.ToStringWithPlanetAndOwner(), Verbosity.DoNotShow );
            
            TargetOrNull.Die( Context, true, null, DamageSource.SomeSortOfEnemy );

            if ( !TargetOrNull.GetHasBeenDestroyed() )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "\tAttempting to despawn " + TargetOrNull.ToStringWithPlanetAndOwner() + " since Die() isn't enough", Verbosity.DoNotShow );

                TargetOrNull.Despawn( Context, true, InstancedRendererDeactivationReason.WasHackedFully );
            }
        }

        public void DoOnFail_CalledFromMainSimOnly(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, HackingType type, HackingEvent Event, ArcenHostOnlySimContext Context )
        {
            //the target of the hack died before the hack finished,
            //or the hack was ended. Note that this applies to the "end" of per-second hacks like
            //superterminals or science hacking if the hacker is scrapped or killed
            try
            {
                if ( type.AIPOnFailedHack > 0 )
                    GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt)type.AIPOnFailedHack, AIPChangeReason.Hacking, Hacker.TypeData, Hacker.GetFactionIndex_Safe(), Hacker.Planet.Index, -1 );
            }
            catch ( Exception e )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    LOG.Err( "DoOnFail_CalledFromMainSimOnly fail Area 1:\n{0}", e);
            }

            try
            {
                this.DoOnFail_Extra( Target, planet, Hacker, type, Event, Context );
            }
            catch ( Exception e )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    LOG.Err( "DoOnFail_CalledFromMainSimOnly fail Area DoOnFail_Extra:\n{0}", e);
            }

            try
            {
                //does this thing die?  Or does it keep track of how many times it was hacked?
                this.HandleDestructionOrIncrementing( Target, type, Context, false );
            }
            catch ( Exception e )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    LOG.Err( "DoOnFail_CalledFromMainSimOnly fail Area HandleDestructionOrIncrementing:\n{0}", e);
            }
        }

        public virtual void DoOnFail_Extra(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, HackingType type, HackingEvent Event, ArcenHostOnlySimContext Context )
        {
        }

        public void DoOnCancel_CalledFromMainSimOnly(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, HackingType type, HackingEvent Event, ArcenHostOnlySimContext Context )
        {
            Faction facOrNull = Hacker.GetFactionOrNull_Safe();
            Event.HackingPointsLeftAfterHack = facOrNull == null ? FInt.Zero : facOrNull.StoredHacking;
            Event.WasCancelled = true;
            Hacker.ActiveHack = null;
            Hacker.ActiveHack_Target = 0;

            if ( type.AIPOnFailedHack > 0 )
                GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt)type.AIPOnFailedHack, AIPChangeReason.Hacking, Hacker.TypeData, Hacker.GetFactionIndex_Safe(), Hacker.Planet.Index, -1 );

            this.DoOnCancel_Extra( Target, planet, Hacker, type, Event, Context );

            if ( type.DestroyHackTargetAtCancel )
            {
                //does this thing die?  Or does it keep track of how many times it was hacked?
                this.HandleDestructionOrIncrementing( Target, type, Context, false );
            }
        }

        public virtual void DoOnCancel_Extra(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, HackingType type, HackingEvent Event, ArcenHostOnlySimContext Context )
        {
        }

        public virtual bool DoSuccessfulCompletionLogic_Extra(
            GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            return true;
        }

        public virtual string GetDynamicDescription(
            GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            return string.Empty; // return target != null ? "<color=#9cbad3>Will target: " + target.TypeData.DisplayName + ".</color>" : string.Empty;
        }
        
        public virtual Sys.IEnumerable<GrantedShip> EnumeratePossibleShipGrants(GameEntity_Squad Target)
        {
            yield break;
        }
    }
}
