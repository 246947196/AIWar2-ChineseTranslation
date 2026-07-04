using Arcen.Universal;
using System;

using System.Diagnostics;
using System.Threading;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// This is for things that we want to run on the client for local-only alerts, or things of that nature.
    /// These are not actually being used for notifications of the normal sense, but anything we want to shove in here.
    /// </summary>
    public class AudioAndSimilarHandler : IExternalPersonalNotificationGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            //these MUST be cleared!
            recentlyHadLotsOfMetal = true;
            recentlyHadNoMetal = false;
            recentlyHadLotsOfEnergy = true;
            perPlanetAudioTrackers.Clear();
        }

        //the audio queue thresholds aren't sync'd to disk deliberately, so a player gets warnings at game load time
        public bool recentlyHadLotsOfMetal = true;
        public bool recentlyHadNoMetal = false;
        public bool recentlyHadLotsOfEnergy = true;

        #region ClientOrHostAudioTracking
        [NotForDumping]
        public readonly Dictionary<int, ClientOrHostAudioTracking> perPlanetAudioTrackers = Dictionary<int, ClientOrHostAudioTracking>.Create_WillNeverBeGCed( 500, "AudioAndSimilarHandler-perPlanetAudioTrackers" );

        public class ClientOrHostAudioTracking
        {
            //each audioData is attached to a planet - these are played on clients or hosts, as they are about fighting at worlds, which everyone has the data for
            //We track the last time various audio effects played.
            //When we detect that an audio effect is going to trigger,
            //we set "TimeThisBattleStarted". Then every second we check
            //whether that planet still has enemies and me on it
            //Once the battle ends, we set TimeThisBattleStarted = -1
            //once we are in a battle, don't play any more effects for a really long time
            //Exception: If we transition to the "Major attack" from "InCombat"
            //then play the Major Attack
            //We deliberately don't sync this in a save game since when you reload a game
            //you probably want to know
            public readonly Int16 planetIndex;
            public int TimeInCombatOnOtherPlanetPlayed = -1;
            public int TimeAttackOnMyPlanetPlayed = -1;

            public ClientOrHostAudioTracking( Int16 planetIndex )
            {
                this.planetIndex = planetIndex;
            }
        }

        public ClientOrHostAudioTracking GetAudioTrackerForPlanetIndex( Int16 PlanetIndex )
        {
            if ( !perPlanetAudioTrackers.ContainsKey( PlanetIndex ) )
            {
                ClientOrHostAudioTracking newTracker = new ClientOrHostAudioTracking( PlanetIndex );
                perPlanetAudioTrackers[PlanetIndex] = newTracker;
                return newTracker;
            }
            return perPlanetAudioTrackers[PlanetIndex];
        }
        #endregion

        public void GeneratePersonalNotificationOnClientOrHost_BackgroundThread( Faction focalFaction, ArcenSimContextAnyStatus Context )
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null )
                return; //if we're a spectator, then don't bother doing this for us

            int debugStage = 1;
            try
            {
                debugStage = 500;
                HandleAdaptiveMusic( localFaction, Context );
                debugStage = 1000;
                HandleInCombatAudioCues( localFaction, Context );
                debugStage = 2000;
                HandleMetalAudioCues( localFaction, Context );
                debugStage = 4000;
                HandleEnergyAudioCues( localFaction, Context );
                debugStage = 5000;
                UpdateStrengthByTechLine( localFaction, Context );
                debugStage = 6000;
                UpdateStrengthByFleet( localFaction, Context );
                debugStage = 7000;
                UpdateNPCShipCountsByCap( Context );
                debugStage = 8000;
                CountShipsForEscapeMenu( Context );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "AudioAndSimilarHandler Error at debug stage " +
                    debugStage + ":\n" + e, Verbosity.ShowAsError );
            }
        }

        #region HandleMetalAudioCues
        private void HandleMetalAudioCues( Faction localFaction, ArcenSimContextAnyStatus Context )
        {
            PlayerTypeData playerType = localFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
            if ( playerType != null && !playerType.UsesMetal )
                return;

            bool AudioDebug = false;

            int currentMetal = localFaction.StoredMetal.IntValue;
            int thresholdForLowMetalWarning = GameSettings.Current.GetIntBySetting( "MetalWarningThreshold" );
            if ( AudioDebug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Metal warning threshold: " + thresholdForLowMetalWarning + " currentMetal " + currentMetal, Verbosity.DoNotShow );
            if ( currentMetal <= 1000 && !recentlyHadNoMetal )
            {
                recentlyHadNoMetal = true;
                if ( AudioDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Play metal exhausted effect", Verbosity.DoNotShow );
                //play voice effect
                Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.PlayLocallyOnly, SFXItemType_NonPositional.MetalExhausted );

            }
            if ( currentMetal < thresholdForLowMetalWarning && recentlyHadLotsOfMetal )
            {
                recentlyHadLotsOfMetal = false;
                if ( AudioDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Play low metal effect", Verbosity.DoNotShow );
                //play voice effect
                Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.PlayLocallyOnly, SFXItemType_NonPositional.MetalLow );
            }
            if ( currentMetal >= thresholdForLowMetalWarning << 1 )
            {
                if ( AudioDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "reset metal counter", Verbosity.DoNotShow );
                recentlyHadLotsOfMetal = true;
                recentlyHadNoMetal = false;
            }
        }
        #endregion

        #region HandleEnergyAudioCues
        private void HandleEnergyAudioCues( Faction localFaction, ArcenSimContextAnyStatus Context )
        {
            bool AudioDebug = false;

            int currentEnergy = localFaction.NetEnergy;
            int thresholdForLowEnergyWarning = GameSettings.Current.GetIntBySetting( "EnergyWarningThreshold" );
            if ( AudioDebug )
                ArcenDebugging.ArcenDebugLogSingleLine( "energy warning threshold: " + thresholdForLowEnergyWarning + " currentenergy " + currentEnergy, Verbosity.DoNotShow );

            if ( currentEnergy < thresholdForLowEnergyWarning && recentlyHadLotsOfEnergy )
            {
                recentlyHadLotsOfEnergy = false;
                if ( AudioDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Play energy effect", Verbosity.DoNotShow );

                //play voice effect
                Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.PlayLocallyOnly, SFXItemType_NonPositional.LowOnEnergy );
            }
            if ( currentEnergy >= thresholdForLowEnergyWarning << 1 )
            {
                if ( AudioDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "reset energy counter", Verbosity.DoNotShow );
                recentlyHadLotsOfEnergy = true;
            }
        }
        #endregion

        #region HandleAdaptiveMusic
        private void HandleAdaptiveMusic( Faction localFaction, ArcenSimContextAnyStatus Context )
        {
            if (!GameSettings.Current.GetBool(ArcenBoolSetting_Universal.EnableMusic) ||
                    !GameSettings_AIW2.Current.GetBool(ArcenBoolSetting_AIW2.AdaptiveMusic)) {
                return;
            }

            bool SomethingExcitingHappening = false;

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                PlanetFaction controllingFactionOrNull = planet.GetControllingPlanetFaction();
                PlanetFaction playerPlanetFaction = planet.GetPlanetFactionForFaction( localFaction );
                if ( playerPlanetFaction == null ) {
                    ArcenDebugging.ArcenDebugLogSingleLine( "playerPlanetFaction is null for " + planet.Name, Verbosity.DoNotShow );
                    continue;
                }

                int hostileStrength = playerPlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                int myStrength = playerPlanetFaction.DataByStance[FactionStance.Self].TotalStrength;
                {
                    //This code is used for Adaptive Music. If the strength of forces are high enough then play some exciting music!
                    //note that the exciting threshold changes with AIP
                    int strengthForExciting = 5000 + 1000 * (FactionUtilityMethods.Instance.GetCurrentAIP() / 20).IntValue;
                    if ( myStrength > strengthForExciting &&
                         hostileStrength > strengthForExciting )
                    {
                        SomethingExcitingHappening = true;
                        break;
                    }
                }
            }
            World_AIW2.Instance.ExcitingGameState = SomethingExcitingHappening;
        }
        #endregion

        #region HandleInCombatAudioCues
        private void HandleInCombatAudioCues( Faction localFaction, ArcenSimContextAnyStatus Context )
        {
            bool PlayOurCombat = false;
            bool PlayCriticalCombat = false;
            bool PlayHomeworldCombat = false;
            bool PlayBorderCombat = false;
            bool NanocaustAttacking = false; //some unique lines for the nanocaust
            bool PlayNeutralCombat = false;
            bool PlayEnemyCombat = false;
            bool PlayMajorAttack = false;
            bool AudioDebug = false;

            if ( World.Instance.IsPaused )
                return; //no voice lines if the game is paused, it can get annoying

            //Iterate over all the planets. We then set a bool for each possible outcome,
            //then play the effect if desired
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                bool isInNotableBattle = false;
                bool isMyPlanet = false;
                if ( planet == null )
                    continue;
                PlanetFaction controllingFactionOrNull = planet.GetControllingPlanetFaction();
                PlanetFaction playerPlanetFaction = planet.GetPlanetFactionForFaction( localFaction );
                if ( playerPlanetFaction == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "playerPlanetFaction is null for " + planet.Name, Verbosity.DoNotShow );
                    break;
                }

                int hostileStrength = playerPlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                int myAndAlliedStrength = playerPlanetFaction.DataByStance[FactionStance.Self].TotalStrength + playerPlanetFaction.DataByStance[FactionStance.Friendly].TotalStrength;
                int myStrength = playerPlanetFaction.DataByStance[FactionStance.Self].TotalStrength;
                int myMobileStrength = playerPlanetFaction.DataByStance[FactionStance.Self].MobileStrength;
                ClientOrHostAudioTracking audio = GetAudioTrackerForPlanetIndex( planet.Index );
                isMyPlanet = (controllingFactionOrNull != null && controllingFactionOrNull.Faction == localFaction );

                if ( isMyPlanet || myMobileStrength > 3000 || myStrength > 8000 )
                {
                    if (hostileStrength > myAndAlliedStrength * GameSettings.Current.GetFloatBySetting( "AlertIfEnemyAttackersOverpowerByRatio" )) {
                        isInNotableBattle = true;
                    }
                }
                //do nothing more if we're not in a notable battle here
                if ( !isInNotableBattle )
                    continue;

                // see if it involves the nanocaust
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    if ( otherFaction.GetIsFriendlyToLocalFaction() )
                        continue;
                    if ( otherFaction.SpecialFactionData.InternalName == "Nanocaust" )
                    {
                        PlanetFaction otherPFaction = planet.GetPlanetFactionForFaction( otherFaction );
                        if ( otherPFaction.DataByStance[FactionStance.Self].TotalStrength >
                                otherPFaction.DataByStance[FactionStance.Friendly].TotalStrength / 2 )
                        {
                            if ( AudioDebug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Nanocaust is attacking " + planet.Name, Verbosity.DoNotShow );
                            NanocaustAttacking = true;
                        }
                        if ( NanocaustAttacking )
                            break;
                    }
                }

                if ( AudioDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "battle on " + planet.Name + " my and allied str: " + myAndAlliedStrength + " enemy str " + myStrength, Verbosity.DoNotShow );


                int minSecondsBetweenAlerts = GameSettings.Current.GetIntBySetting( "MinSecondsBetweenAttackAlertsOnSamePlanet" );
                if ( isMyPlanet )
                {
                    if ( audio.TimeAttackOnMyPlanetPlayed + minSecondsBetweenAlerts >= World_AIW2.Instance.GameSecond )
                        continue;
                    audio.TimeAttackOnMyPlanetPlayed = World_AIW2.Instance.GameSecond;

                    if ( hostileStrength > myAndAlliedStrength * GameSettings.Current.GetFloatBySetting( "EnemyAttackersOverpowerByRatioForMajorAttackWarning" ) &&
                            hostileStrength > GameSettings.Current.GetIntBySetting( "MinEnemyStrengthRequiredForMajorAttackWarning" ) * 1000 ) {
                        PlayMajorAttack = true;
                    }

                    GameEntity_Squad king = localFaction.GetFactionKing();
                    if ( king != null && king.Planet == planet )
                        PlayHomeworldCombat = true;
                    else
                    {
                        bool foundCriticalStructure = false;
                        foreach ( GameEntity_Squad entity in playerPlanetFaction.Entities.Squads( EntityRollupType.CriticalInfrastructure ) )
                        {
                            foundCriticalStructure = true;
                            break;
                        }
                        if ( foundCriticalStructure )
                            PlayCriticalCombat = true;
                        else
                            PlayOurCombat = true;
                    }
                }
                else if ( planet.GetControllingFaction().GetIsHostileToLocalFaction() || planet.GetFactionWithSpecialInfluenceHere().GetIsHostileToLocalFaction() )
                {
                    if ( audio.TimeInCombatOnOtherPlanetPlayed + minSecondsBetweenAlerts >= World_AIW2.Instance.GameSecond )
                        continue;
                    audio.TimeInCombatOnOtherPlanetPlayed = World_AIW2.Instance.GameSecond;
                    PlayEnemyCombat = true;
                }
                else if ( planet.GetControllingFaction().GetIsFriendlyToLocalFaction() || planet.GetFactionWithSpecialInfluenceHere().GetIsFriendlyToLocalFaction() )
                {
                    if ( audio.TimeInCombatOnOtherPlanetPlayed + minSecondsBetweenAlerts >= World_AIW2.Instance.GameSecond )
                        continue;
                    audio.TimeInCombatOnOtherPlanetPlayed = World_AIW2.Instance.GameSecond;
                    PlayBorderCombat = true; // Border World here means allied planets
                }
                else
                {
                    if ( audio.TimeInCombatOnOtherPlanetPlayed + minSecondsBetweenAlerts >= World_AIW2.Instance.GameSecond )
                        continue;
                    audio.TimeInCombatOnOtherPlanetPlayed = World_AIW2.Instance.GameSecond;
                    PlayNeutralCombat = true;
                }
            }

            //Note that we will only try to play one audio effect. If multiple cases are true
            //then play Major Attack warning if it's available
            if ( PlayMajorAttack )
            {
                if ( AudioDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "playing major attack", Verbosity.DoNotShow );
                Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.PlayLocallyOnly, SFXItemType_NonPositional.MajorEnemyForceInOurTerritory );
            }
            else if ( PlayBorderCombat )
            {
                if ( AudioDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "playing border", Verbosity.DoNotShow );
                //A "Border World" is an allied planet
                if ( NanocaustAttacking )
                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.PlayLocallyOnly, SFXItemType_NonPositional.NanocaustFrenziesAgainstBorderWorld );
                else
                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.PlayLocallyOnly, SFXItemType_NonPositional.OurShipsAreBeingDamagedOnABorderWorld );
            }
            else if ( PlayCriticalCombat )
            {
                if ( AudioDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "playing critical", Verbosity.DoNotShow );
                if ( NanocaustAttacking )
                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.PlayLocallyOnly, SFXItemType_NonPositional.NanocaustFrenziesAgainstCriticalWorld );
                else
                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.PlayLocallyOnly, SFXItemType_NonPositional.OurShipsAreBeingDamagedOnCriticalPlanet );
            }
            else if ( PlayHomeworldCombat )
            {
                if ( AudioDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "playing homeworld, nanocaust? " + NanocaustAttacking, Verbosity.DoNotShow );

                if ( NanocaustAttacking )
                {
                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.PlayLocallyOnly, SFXItemType_NonPositional.NanocaustFrenziesAgainstHomeWorld );
                }
                else
                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.PlayLocallyOnly, SFXItemType_NonPositional.OurShipsAreBeingDamagedOnHomeworld );
            }
            else if ( PlayOurCombat )
            {
                if ( AudioDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "playing our planet", Verbosity.DoNotShow );
                Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.PlayLocallyOnly, SFXItemType_NonPositional.OurShipsAreBeingDamagedInOurTerritory );
            }

            else if ( PlayEnemyCombat )
            {
                if ( AudioDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "playing enemy", Verbosity.DoNotShow );
                Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.PlayLocallyOnly, SFXItemType_NonPositional.OurShipsAreBeingDamagedInEnemyTerritory );
            }
            else if ( PlayNeutralCombat )
            {
                if ( AudioDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "playing neutral", Verbosity.DoNotShow );
                Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.PlayLocallyOnly, SFXItemType_NonPositional.OurShipsAreBeingDamagedInNeutralTerritory );
            }
        }
        #endregion

        #region UpdateStrengthByTechLine
        private static bool debug = false;

        public void UpdateStrengthByTechLine( Faction localFaction, ArcenSimContextAnyStatus Context )
        {
            PlayerTypeData playerTypeData = localFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
            if ( playerTypeData == null )
                return;

            Dictionary<GameEntityTypeData, int> LinesCounted = GameEntityTypeData.GetTemporaryGameEntityTypeDataIntDict( "AudioAndSimilarHandler-UpdateStrengthByTechLine-LinesCounted", 10f );
            if ( LinesCounted == null ) //blocked for teardown/shutdown; bail
                return;

            List<TechUpgrade> upgrades = playerTypeData.TechUpgradesForThisPlayerType;
            for ( int i = 0; i < upgrades.Count; i++ )
            {
                TechUpgrade upgrade = upgrades[i];

                if ( localFaction.GetIsTechFullyUnlocked( upgrade ) )
                {
                    upgrade.UIOnly_Tech_UpgradedDefenseStrengthIncrease_FromOneMarkLevel = 0;
                    upgrade.UIOnly_Tech_UpgradedShipStrengthIncrease_FromOneMarkLevel = 0;
                    upgrade.UIOnly_Tech_DowngradedDefenseStrengthIncrease_FromClearingMarkLevels = 0;
                    upgrade.UIOnly_Tech_DowngradedShipStrengthIncrease_FromClearingMarkLevels = 0;
                    upgrade.UIOnly_Tech_CurrentDefenseStrength = 0;
                    upgrade.UIOnly_Tech_CurrentShipStrength = 0;
                    upgrade.UIOnly_Tech_ShipLinesAffected = 0;
                    upgrade.UIOnly_Tech_DefensiveLinesAffected = 0;
                    continue; //this has been maxed out!
                }
                int currentUpgradesHeld = localFaction.TechUnlocks[upgrade.RowIndexNonSim];
                int upgradedDefenseStrengthIncreaseCalculated = 0;
                int upgradedShipStrengthIncreaseCalculated = 0;
                int downgradedDefenseStrengthIncreaseCalculated = 0;
                int downgradedShipStrengthIncreaseCalculated = 0;
                int currentDefenseStrengthCalculated = 0;
                int currentShipStrengthCalculated = 0;
                int numShipLinesIncreased = 0;
                int numDefensiveLinesIncreased = 0;
                LinesCounted.Clear();
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Updating ship counters for " + upgrade.ToString(), Verbosity.DoNotShow );
                foreach ( Fleet fleet in World_AIW2.Instance.Fleets( localFaction, FleetStatus.AnyStatus ) )
                {
                    if ( fleet == null )
                        continue;
                    bool hasPrintedFleet = false;
                    foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                    {
                        if ( mem == null || mem.TypeData == null )
                            continue;
                        if ( mem.TypeData.IsDrone )
                            continue;
                        if ( mem.EffectiveSquadCap <= 0 && mem.EntitiesOfFMem.Count <= 0 && mem.TransportContents.Count <= 0 )
                            continue; //nothing to check here

                        if ( mem.TypeData.SpecialType == SpecialEntityType.SmallShipNotStackable )
                            continue; //these are usually things like tesla torpedoes
                        if ( mem.TypeData.TechUpgradesThatBenefitMe.Contains( upgrade ) )
                        {
                            if ( mem.TypeData.IsTurret || !mem.TypeData.IsMobile || mem.TypeData.FleetMembershipStyle == FleetMembershipStyle.Planetary )
                            {
                                //TODO: since turrets ship groups are duplicated per planet,
                                //we need to be more careful about the counting. This doesn't work at the moment
                                if ( !LinesCounted.ContainsKey( mem.TypeData ) )
                                {
                                    LinesCounted[mem.TypeData] = 1;
                                    numDefensiveLinesIncreased++;
                                }

                                upgradedDefenseStrengthIncreaseCalculated += mem.GetStrengthIncreaseFromLevelUp();
                                if ( currentUpgradesHeld != 0 )
                                    downgradedDefenseStrengthIncreaseCalculated += mem.GetStrengthDecreaseFromLevelDown( currentUpgradesHeld );
                                currentDefenseStrengthCalculated += (mem.GetStrengthPerSquad_PlayerFleetsOnly() * Math.Max( mem.EntitiesOfFMem.Count, mem.EffectiveSquadCap ));
                                if ( debug && !hasPrintedFleet )
                                {
                                    ArcenDebugging.ArcenDebugLogSingleLine( "\tChecking for fleet " + fleet.GetName() + " owned by " + fleet.Faction.GetDisplayName(), Verbosity.DoNotShow );
                                    hasPrintedFleet = true;
                                }

                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "\t\tdefensive structure '" + mem.TypeData.GetDisplayName() + "' fleet " + fleet.GetName() + " mark level " + localFaction.GetGlobalMarkLevelForShipLine( mem.TypeData ) + " cap " + mem.EffectiveSquadCap + " counter: " + numShipLinesIncreased, Verbosity.DoNotShow );
                                //                                ArcenDebugging.ArcenDebugLogSingleLine("\tdefense " + mem.TypeData.GetDisplayName() + " mark level " +localFaction.GetGlobalMarkLevelForShipLine(mem.TypeData) + " cap "  + mem.EffectiveSquadCap + "  current " + mem.LocalPlayerUIFullMembershipStrength + " upgraded " + mem.GetStrengthIncreaseFromLevelUp().IntValue, Verbosity.DoNotShow );
                            }
                            else
                            {
                                /*if ( !LinesCounted.ContainsKey( mem.TypeData ) )
                                {
                                LinesCounted[mem.TypeData] = true; */
                                numShipLinesIncreased++; //we just always increase the number; the goal is "How many copies of all ship lines", not "how many distinct ship lines
                                                         //                                }
                                upgradedShipStrengthIncreaseCalculated += mem.GetStrengthIncreaseFromLevelUp();
                                if ( currentUpgradesHeld != 0 )
                                    downgradedShipStrengthIncreaseCalculated += mem.GetStrengthDecreaseFromLevelDown( currentUpgradesHeld );
                                currentShipStrengthCalculated += (mem.GetStrengthPerSquad_PlayerFleetsOnly() * Math.Max( mem.EntitiesOfFMem.Count, mem.EffectiveSquadCap ));
                                if ( debug && !hasPrintedFleet )
                                {
                                    ArcenDebugging.ArcenDebugLogSingleLine( "\tChecking for fleet " + fleet.GetName() + " owned by " + fleet.Faction.GetDisplayName(), Verbosity.DoNotShow );
                                    hasPrintedFleet = true;
                                }

                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "\t\tship '" + mem.TypeData.GetDisplayName() + "' fleet " + fleet.GetName() + " mark level " + localFaction.GetGlobalMarkLevelForShipLine( mem.TypeData ) + " cap " + mem.EffectiveSquadCap + " counter: " + numShipLinesIncreased, Verbosity.DoNotShow );
                            }
                            //                          ArcenDebugging.ArcenDebugLogSingleLine("\tStrength increase for " + mem.TypeData.GetDisplayName() + " would be " + mem.GetStrengthIncreaseFromLevelUp().IntValue, Verbosity.DoNotShow );
                        }
                    }
                }
                //                ArcenDebugging.ArcenDebugLogSingleLine("For tech " + upgrade.GetDisplayName() + " ship increase " + (upgrade.UpgradedShipStrength_ForUIOnly - upgrade.CurrentShipStrength_ForUIOnly) + " turret increase " + (upgrade.UpgradedDefenseStrength_ForUIOnly - upgrade.CurrentDefenseStrength_ForUIOnly), Verbosity.DoNotShow );
                upgrade.UIOnly_Tech_UpgradedDefenseStrengthIncrease_FromOneMarkLevel = upgradedDefenseStrengthIncreaseCalculated;
                upgrade.UIOnly_Tech_UpgradedShipStrengthIncrease_FromOneMarkLevel = upgradedShipStrengthIncreaseCalculated;
                upgrade.UIOnly_Tech_DowngradedDefenseStrengthIncrease_FromClearingMarkLevels = downgradedDefenseStrengthIncreaseCalculated;
                upgrade.UIOnly_Tech_DowngradedShipStrengthIncrease_FromClearingMarkLevels = downgradedShipStrengthIncreaseCalculated;
                upgrade.UIOnly_Tech_CurrentDefenseStrength = currentDefenseStrengthCalculated;
                upgrade.UIOnly_Tech_CurrentShipStrength = currentShipStrengthCalculated;
                upgrade.UIOnly_Tech_ShipLinesAffected = numShipLinesIncreased;
                upgrade.UIOnly_Tech_DefensiveLinesAffected = numDefensiveLinesIncreased;
            }
            debug = false;

            GameEntityTypeData.ReleaseTemporaryGameEntityTypeDataIntDict( LinesCounted );
        }
        #endregion

        #region UpdateStrengthByFleet
        public void UpdateStrengthByFleet( Faction localFaction, ArcenSimContextAnyStatus Context )
        {
            PlayerTypeData playerTypeData = localFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
            if ( playerTypeData == null )
                return;

            Dictionary<GameEntityTypeData, int> LinesCounted = GameEntityTypeData.GetTemporaryGameEntityTypeDataIntDict( "AudioAndSimilarHandler-UpdateStrengthByFleet-LinesCounted", 10f );
            if ( LinesCounted == null ) //blocked for teardown/shutdown; bail
                return;

            foreach ( Fleet fleetToUpgrade in World_AIW2.Instance.Fleets( localFaction, FleetStatus.AnyStatus ) )
            {
                   if ( fleetToUpgrade.AddedMarkLevelsForFleet_FromScience >= 6 )
                   {
                       fleetToUpgrade.UIOnly_Fleet_UpgradedDefenseStrengthIncrease_FromOneMarkLevel = 0;
                       fleetToUpgrade.UIOnly_Fleet_UpgradedShipStrengthIncrease_FromOneMarkLevel = 0;
                       fleetToUpgrade.UIOnly_Fleet_DowngradedDefenseStrengthIncrease_FromClearingMarkLevels = 0;
                       fleetToUpgrade.UIOnly_Fleet_DowngradedShipStrengthIncrease_FromClearingMarkLevels = 0;
                       fleetToUpgrade.UIOnly_Fleet_CurrentDefenseStrength = 0;
                       fleetToUpgrade.UIOnly_Fleet_CurrentShipStrength = 0;
                       fleetToUpgrade.UIOnly_Fleet_ShipLinesAffected = 0;
                       fleetToUpgrade.UIOnly_Fleet_DefensiveLinesAffected = 0;
                       continue; //this has been maxed out!
                   }
                   int currentUpgradesHeld = fleetToUpgrade.AddedMarkLevelsForFleet_FromScience;
                   int upgradedDefenseStrengthIncreaseCalculated = 0;
                   int upgradedShipStrengthIncreaseCalculated = 0;
                   int downgradedDefenseStrengthIncreaseCalculated = 0;
                   int downgradedShipStrengthIncreaseCalculated = 0;
                   int currentDefenseStrengthCalculated = 0;
                   int currentShipStrengthCalculated = 0;
                   int numShipLinesIncreased = 0;
                   int numDefensiveLinesIncreased = 0;
                   LinesCounted.Clear();
                   if ( debug )
                       ArcenDebugging.ArcenDebugLogSingleLine( "Updating ship counters for " + fleetToUpgrade.GetName().ToString(), Verbosity.DoNotShow );

                   bool hasPrintedFleet = false;

                   GameEntity_Squad centerpOrNull = fleetToUpgrade.Centerpiece.GetSquad();
                   bool upgradeEntireFleet = centerpOrNull != null && centerpOrNull.TypeData.ThisCenterpieceGrantsItsDirectScienceUpgradesToRestOfFleet;

                   foreach ( FleetMembership mem in fleetToUpgrade.MemberGroupsUnsorted_Sim )
                   {
                       if ( mem == null || mem.TypeData == null )
                           continue;
                       if ( mem.TypeData.IsDrone )
                           continue;
                       if ( mem.EffectiveSquadCap <= 0 && mem.EntitiesOfFMem.Count <= 0 )
                           continue; //nothing to check here

                       if ( mem.TypeData.SpecialType == SpecialEntityType.SmallShipNotStackable )
                           continue; //these are usually things like tesla torpedoes
                       if ( upgradeEntireFleet || mem.TypeData.IsFleetLeader )
                       {
                           if ( mem.TypeData.IsTurret || !mem.TypeData.IsMobile || mem.TypeData.FleetMembershipStyle == FleetMembershipStyle.Planetary )
                           {
                               //TODO: since turrets ship groups are duplicated per planet,
                               //we need to be more careful about the counting. This doesn't work at the moment
                               if ( !LinesCounted.ContainsKey( mem.TypeData ) )
                               {
                                   LinesCounted[mem.TypeData] = 1;
                                   numDefensiveLinesIncreased++;
                               }

                               upgradedDefenseStrengthIncreaseCalculated += mem.GetStrengthIncreaseFromLevelUp();
                               if ( currentUpgradesHeld != 0 )
                                   downgradedDefenseStrengthIncreaseCalculated += mem.GetStrengthDecreaseFromLevelDown( currentUpgradesHeld );
                               currentDefenseStrengthCalculated += (mem.GetStrengthPerSquad_PlayerFleetsOnly() * Math.Max( mem.EntitiesOfFMem.Count, mem.EffectiveSquadCap ));
                               if ( debug && !hasPrintedFleet )
                               {
                                   ArcenDebugging.ArcenDebugLogSingleLine( "\tChecking for fleet " + fleetToUpgrade.GetName() + " owned by " + fleetToUpgrade.Faction.GetDisplayName(), Verbosity.DoNotShow );
                                   hasPrintedFleet = true;
                               }

                               if ( debug )
                                   ArcenDebugging.ArcenDebugLogSingleLine( "\t\tdefensive structure '" + mem.TypeData.GetDisplayName() + "' fleet " + fleetToUpgrade.GetName() + " mark level " + localFaction.GetGlobalMarkLevelForShipLine( mem.TypeData ) + " cap " + mem.EffectiveSquadCap + " counter: " + numShipLinesIncreased, Verbosity.DoNotShow );
                               //                                ArcenDebugging.ArcenDebugLogSingleLine("\tdefense " + mem.TypeData.GetDisplayName() + " mark level " +localFaction.GetGlobalMarkLevelForShipLine(mem.TypeData) + " cap "  + mem.EffectiveSquadCap + "  current " + mem.LocalPlayerUIFullMembershipStrength + " upgraded " + mem.GetStrengthIncreaseFromLevelUp().IntValue, Verbosity.DoNotShow );
                           }
                           else
                           {
                               /*if ( !LinesCounted.ContainsKey( mem.TypeData ) )
                               {
                               LinesCounted[mem.TypeData] = true; */
                               numShipLinesIncreased++; //we just always increase the number; the goal is "How many copies of all ship lines", not "how many distinct ship lines
                                                        //                                }
                               upgradedShipStrengthIncreaseCalculated += mem.GetStrengthIncreaseFromLevelUp();
                               if ( currentUpgradesHeld != 0 )
                                   downgradedShipStrengthIncreaseCalculated += mem.GetStrengthDecreaseFromLevelDown( currentUpgradesHeld );
                               currentShipStrengthCalculated += (mem.GetStrengthPerSquad_PlayerFleetsOnly() * Math.Max( mem.EntitiesOfFMem.Count, mem.EffectiveSquadCap ));
                               if ( debug && !hasPrintedFleet )
                               {
                                   ArcenDebugging.ArcenDebugLogSingleLine( "\tChecking for fleet " + fleetToUpgrade.GetName() + " owned by " + fleetToUpgrade.Faction.GetDisplayName(), Verbosity.DoNotShow );
                                   hasPrintedFleet = true;
                               }

                               if ( debug )
                                   ArcenDebugging.ArcenDebugLogSingleLine( "\t\tship '" + mem.TypeData.GetDisplayName() + "' fleet " + fleetToUpgrade.GetName() + " mark level " + localFaction.GetGlobalMarkLevelForShipLine( mem.TypeData ) + " cap " + mem.EffectiveSquadCap + " counter: " + numShipLinesIncreased, Verbosity.DoNotShow );
                           }
                           //                          ArcenDebugging.ArcenDebugLogSingleLine("\tStrength increase for " + mem.TypeData.GetDisplayName() + " would be " + mem.GetStrengthIncreaseFromLevelUp().IntValue, Verbosity.DoNotShow );
                       }
                   }

                   //                ArcenDebugging.ArcenDebugLogSingleLine("For tech " + upgrade.GetDisplayName() + " ship increase " + (upgrade.UpgradedShipStrength_ForUIOnly - upgrade.CurrentShipStrength_ForUIOnly) + " turret increase " + (upgrade.UpgradedDefenseStrength_ForUIOnly - upgrade.CurrentDefenseStrength_ForUIOnly), Verbosity.DoNotShow );
                   fleetToUpgrade.UIOnly_Fleet_UpgradedDefenseStrengthIncrease_FromOneMarkLevel = upgradedDefenseStrengthIncreaseCalculated;
                   fleetToUpgrade.UIOnly_Fleet_UpgradedShipStrengthIncrease_FromOneMarkLevel = upgradedShipStrengthIncreaseCalculated;
                   fleetToUpgrade.UIOnly_Fleet_DowngradedDefenseStrengthIncrease_FromClearingMarkLevels = downgradedDefenseStrengthIncreaseCalculated;
                   fleetToUpgrade.UIOnly_Fleet_DowngradedShipStrengthIncrease_FromClearingMarkLevels = downgradedShipStrengthIncreaseCalculated;
                   fleetToUpgrade.UIOnly_Fleet_CurrentDefenseStrength = currentDefenseStrengthCalculated;
                   fleetToUpgrade.UIOnly_Fleet_CurrentShipStrength = currentShipStrengthCalculated;
                   fleetToUpgrade.UIOnly_Fleet_ShipLinesAffected = numShipLinesIncreased;
                   fleetToUpgrade.UIOnly_Fleet_DefensiveLinesAffected = numDefensiveLinesIncreased;
            }
            debug = false;

            GameEntityTypeData.ReleaseTemporaryGameEntityTypeDataIntDict( LinesCounted );
        }
        #endregion

        #region UpdateNPCShipCountsByCap
        public void UpdateNPCShipCountsByCap( ArcenSimContextAnyStatus Context )
        {
            int npcCapTypeCount = NPCShipCapTypeTable.Instance.Rows.Count;

            //do for all the factions
            foreach ( Faction fac in World_AIW2.Instance.Factions )
            {
                //if missing existing count dictionaries, create them
                if ( fac.NPCShipCountsByCapType == null || fac.NPCShipCountsByCapType.Length < npcCapTypeCount )
                    fac.NPCShipCountsByCapType = new int[npcCapTypeCount];

                if ( fac.NextNPCShipCountsByCapType == null || fac.NextNPCShipCountsByCapType.Length < npcCapTypeCount )
                    fac.NextNPCShipCountsByCapType = new int[npcCapTypeCount];

                //swap the two dictionaries
                {
                    int[] one = fac.NextNPCShipCountsByCapType;
                    fac.NextNPCShipCountsByCapType = fac.NPCShipCountsByCapType;
                    fac.NPCShipCountsByCapType = one;
                }

                //reset all the next counts
                for ( int i = 0; i < fac.NextNPCShipCountsByCapType.Length; i++ )
                    fac.NextNPCShipCountsByCapType[i] = 0;
            }

            //now count all of the entities in the game and put their data in
            foreach ( GameEntity_Squad squad in World_AIW2.Instance.Squads() )
            {
                Faction fac = squad.GetFactionOrNull_Safe();
                GameEntityTypeData typeData = squad.TypeData;
                if ( fac != null && typeData != null )
                {
                    fac.NextNPCShipCountsByCapType[typeData.NPCShipCap.RowIndexNonSim]++;
                }
            }
        }
        #endregion

        public static int TotalShots = 0;
        public static int TotalOtherEntities = 0;

        public static int TotalShips = 0;
        public static int TotalSpeedGroups = 0;

        public static int TotalPlanetsOn = 0;
        public static int TotalPlanetsOff = 0;
        public static int TotalPlanetsTier1 = 0;
        public static int TotalPlanetsTier2 = 0;
        public static int TotalPlanetsTier3 = 0;

        public static int TotalDeathRegistryEntries = 0;
        public static int TotalStandaloneShips = 0;
        public static int TotalUnstackableSquads = 0;
        public static int TotalStacksOfAtLeastTwoUnits = 0;
        public static int TotalStackedShips = 0;
        public static int TotalContainedShips = 0;
        public static int TotalContainers = 0;

        #region CountShipsForEscapeMenu
        public void CountShipsForEscapeMenu( ArcenSimContextAnyStatus Context )
        {
            int speedGroupCount = 0;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction faction = World_AIW2.Instance.Factions[i];
                for ( int j = 0; j < faction.SpeedGroups_HostOnly.Count; j++ )
                {
                    SpeedGroup group = faction.SpeedGroups_HostOnly[j];
                    if ( group != null )
                        speedGroupCount++;
                }
            }
            TotalSpeedGroups = speedGroupCount;

            int squadCountRawWithDeadEntries = World_AIW2.Instance.GetSquadCount();
            int squadCountLone = 0;
            int squadCountUnstackable = 0;
            int stackCountsLargerThan1 = 0;
            int squadCountStacked = 0;
            int containedCount = 0;
            int containers = 0;
            try
            {
                foreach ( GameEntity_Squad squad in World_AIW2.Instance.Squads() )
                {
                    squadCountLone++;
                    
                    if ( squad.ExtraStackedSquadsInThis > 0 )
                    {
                        squadCountStacked += squad.ExtraStackedSquadsInThis;
                        stackCountsLargerThan1++;
                    }

                    if (squad.TypeData.CannotBeStacked)
                        squadCountUnstackable++;
                    
                    int contained = squad.CalculateContentsCount( false );
                    if ( contained > 0 )
                    {
                        containedCount += contained;
                        containers++;
                    }

                }
            }
            catch { }

            TotalDeathRegistryEntries = squadCountRawWithDeadEntries - squadCountLone;
            TotalStandaloneShips = squadCountLone;
            TotalUnstackableSquads = squadCountUnstackable;
            TotalStacksOfAtLeastTwoUnits = stackCountsLargerThan1;
            TotalStackedShips = squadCountStacked;
            TotalContainedShips = containedCount;
            TotalContainers = containers;

            TotalShips = TotalStandaloneShips + TotalStackedShips + TotalContainedShips;

            Int16 planetsOn = 0;
            Int16 planetsOff = 0;
            Int16 planetsTier1 = 0;
            Int16 planetsTier2 = 0;
            Int16 planetsTier3 = 0;

            try
            {
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( planet.BattleStatus_ProcessThisSimStep )
                        planetsOn++;
                    else
                        planetsOff++;
                    switch ( planet.BattleStatus )
                    {
                        case PlanetBattleStatus.Tier1_PlayerLookingAtMe:
                            planetsTier1++;
                            break;
                        case PlanetBattleStatus.Tier2_PlayerShipsHere_OffFrame:
                        case PlanetBattleStatus.Tier2_PlayerShipsHere_OnFrame:
                            planetsTier2++;
                            break;
                        case PlanetBattleStatus.Tier3_PlayersAbsent_OffFrame:
                        case PlanetBattleStatus.Tier3_PlayersAbsent_OnFrame:
                            planetsTier3++;
                            break;
                    }
                }
            }
            catch { }

            TotalPlanetsOn = planetsOn;
            TotalPlanetsOff = planetsOff;

            TotalPlanetsTier1 = planetsTier1;
            TotalPlanetsTier2 = planetsTier2;
            TotalPlanetsTier3 = planetsTier3;

            int totalShots = 0;
            try
            {
                foreach ( GameEntity_Shot shot in World_AIW2.Instance.Shots() )
                {
                    totalShots++;
                }
            }
            catch { }

            TotalShots = totalShots;

            int totalOthers = 0;
            try
            {
                foreach ( GameEntity_Other other in World_AIW2.Instance.Others() )
                {
                    totalOthers++;
                }
            }
            catch { }

            TotalOtherEntities = totalOthers;
        }
        #endregion
    }
}
