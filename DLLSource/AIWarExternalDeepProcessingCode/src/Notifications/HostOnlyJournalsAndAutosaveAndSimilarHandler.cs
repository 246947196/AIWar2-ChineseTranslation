using Arcen.Universal;
using System;

using System.Diagnostics;
using System.Threading;
using Arcen.AIW2.Core;

//Chris says: this is in the main namespace because we are going to cheat and actually run this on the host after all
//            it's not strictly how this type of handler is meant to be used, but it's on a convenient thread
//            and we can easily make it host-only.
namespace Arcen.AIW2.External
{
    /// <summary>
    /// This is for things we want to run on the host, but not connected to a specific player faction.
    /// Even more to the point, we really really need this to run on 
    /// </summary>

    public class HostOnlyJournalsAndAutosaveAndSimilarHandler : IExternalPersonalNotificationGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            //probably does not matter
            strengthOfCenterpiecesPerPlanet.Clear();
            EntitiesTriggering.Clear();

            //matters a little
            hasLoggedARSMessage = false;
            hasLoggedFlagshipMessage = false;
            hasLoggedAdditionalMessage = false;
            LastJournalTime = 0;
        }

        public void GeneratePersonalNotificationOnClientOrHost_BackgroundThread( Faction focalFaction, ArcenSimContextAnyStatus baseContext )
        {
            //doing this on the client would break the world!
            var hostContext = baseContext?.GetHostOnlyContext();
            if ( hostContext == null ) 
                return; 
            
            //But we also want this to happen precisely once on the host, not once per human faction.
            //...and we want this to happen regardless of type of human faction.  Solo ark, necromancer, etc.
                        
            //also don't do this on the first second
            // todo: instead, skip until not the same second game was loaded
            //       ie, wait till they unpause
            if ( World_AIW2.Instance.GameSecond <= 1 ) 
                return;

            int debugStage = 1;
            try
            {
                debugStage = 1000;
                AutosaveHandlerDeepInfo.Instance.UpdateAutosave_HostOnly(); //yes do autosaves even in tutorials
                debugStage = 7000;
                HandleAchievementsEarnedHostOnly( hostContext );
                debugStage = 9000;
                if ( GameSettings.Current.GetBoolBySetting( "AllegianceDebug" ) ) //this makes AllegianceDebug a host-only feature, just by definition
                    Debug_PrintAllFactionRelationships();

                debugStage = 12000;
                HandleLoreJournals_HostOnly_NotPlayerSpecific( hostContext );

                debugStage = 13000;
                bool anyDefensiveAutomationSet = false;
                bool everyFactionHadDefensiveAutomationSet = true;
                foreach ( Faction faction in World_AIW2.Instance.Factions )
                {
                    if ( faction.Type == FactionType.Player )
                    {
                        debugStage = 14000;
                        HandleAutoKiteOnHost_PerFaction( faction, hostContext );
                        debugStage = 15000;
                        HandleForcefieldsMovingBackOnHost_PerFaction( faction, hostContext );
                        debugStage = 16000;
                        HandleAutoFRD_ForSpecificFaction( faction, hostContext );
                        debugStage = 17000;
                        HandleDebugSpawns_ForSpecificFaction( faction, hostContext );
                        debugStage = 18000;
                        if ( CheckForPlayerDefenseAutoBuilding( faction ) ) //yes do autobuild even in tutorials, but auto building has been moved to its own thread
                            anyDefensiveAutomationSet = true;
                        else
                            everyFactionHadDefensiveAutomationSet = false;
                        debugStage = 19000;
                        HandleAIPForPlanetCapture_ForSpecificFaction( faction, hostContext ); //if we have captured a planet but not paid the price for it already, do so now
                        debugStage = 20000;
                        HandleAutoKiteOnHost_PerFaction( faction, hostContext );
                        debugStage = 25000;
                        HandleHelperJournals_HostOnly_RunOnEachPlayerFaction( faction, hostContext );
                        debugStage = 26000;
                        HandleAdvancedHelperJournals_HostOnly_RunOnEachPlayerFaction( faction, hostContext );
                        debugStage = 27000;
                        HandleBeaconJournals_HostOnly_RunOnEachPlayerFaction( faction, hostContext );
                    }
                }

                debugStage = 42000;
                HandleHelperJournals_HostOnly_NotPlayerSpecific( hostContext,
                    //let's complain if not every human faction has at least some automation
                    anyDefensiveAutomationSet && everyFactionHadDefensiveAutomationSet );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "HostOnlyJournalsAndAutosaveAndSimilarHandler Error at debug stage " +
                    debugStage + ":\n" + e, Verbosity.ShowAsError );
            }
        }

        public static bool CheckForPlayerDefenseAutoBuilding( Faction aFaction )
        {
            PlayerAccount playerControlling = aFaction.GetFirstAssociatedPlayerAccountOrNull();
            if ( playerControlling == null )
                return false; //only do this for factions that are being controlled by a player account!

            if ( World_AIW2.Instance.TutorialOrNull != null && World_AIW2.Instance.TutorialOrNull.DisableAutobuild )
                return true; //don't do autobuild stuff during a tutorial unless the tutorial requests it

            if ( PlayerAutobuilding.Instance.HasFinishedCalculatingAutoBuildingSettings )
                return true; //this is probably still in generation

            bool hasFoundActiveDefenseSetting = false;
            foreach( Tuple<ArcenSetting, List<GameEntityTypeData>, bool> setting in PlayerAutobuilding.Instance.AutoBuildingSettings)
            {
                if ( !setting.Item3 )
                    continue;

                switch ( setting.Item1.Type )
                {
                    case ArcenSettingType.BoolHidden:
                    case ArcenSettingType.BoolToggle:
                        if ( playerControlling.GetNetworkAttachedBoolBySetting( setting.Item1 ) )
                            hasFoundActiveDefenseSetting = true;
                        break;
                    case ArcenSettingType.IntDropdown:
                    case ArcenSettingType.IntHidden:
                    case ArcenSettingType.IntSlider:
                    case ArcenSettingType.IntTextbox:
                        if ( playerControlling.GetNetworkAttachedIntBySetting( setting.Item1 ) > 0 )
                            hasFoundActiveDefenseSetting = true;
                        break;
                    case ArcenSettingType.FloatHidden:
                    case ArcenSettingType.FloatSlider:
                        if ( playerControlling.GetNetworkAttachedFloatBySetting( setting.Item1 ) > 0f )//there shouldn't be a reason for floats, FInts or strings for these settings but just in case - go
                            hasFoundActiveDefenseSetting = true;
                        break;
                    case ArcenSettingType.FIntHidden:
                        if ( playerControlling.GetNetworkAttachedFIntBySetting( setting.Item1 ) > FInt.Zero )
                            hasFoundActiveDefenseSetting = true;
                        break;
                    case ArcenSettingType.StringHidden:
                        if ( playerControlling.GetNetworkAttachedStringBySetting( setting.Item1 ).Equals( "Y" ) )
                            hasFoundActiveDefenseSetting = true;
                        break;
                }

                if ( hasFoundActiveDefenseSetting )
                    return true;
            }
            return false;
        }

        #region Debug_PrintAllFactionRelationships
            private static void Debug_PrintAllFactionRelationships()
        {
            //For debugging faction relationship problems
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "  ", Verbosity.DoNotShow );
                Faction afaction = World_AIW2.Instance.Factions[i];
                string aAllegiance = afaction.BaseInfo.Allegiance;
                for ( int j = 0; j < World_AIW2.Instance.Factions.Count; j++ )
                {
                    Faction bfaction = World_AIW2.Instance.Factions[j];
                    if ( i == j )
                        continue;
                    string bAllegiance = bfaction.BaseInfo.Allegiance;
                    if ( afaction.GetIsHostileTowards( bfaction ) )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( afaction.Type + ": " + afaction.GetDisplayName() + " " + afaction.FactionIndex + " (" + aAllegiance + ") is hostile  towards " + bfaction.Type + ": " + bfaction.GetDisplayName() + " " + bfaction.FactionIndex + " (" + bAllegiance + ")", Verbosity.DoNotShow );
                    }
                    else if ( afaction.GetIsNeutralTowards( bfaction ) )
                        ArcenDebugging.ArcenDebugLogSingleLine( afaction.Type + ": " + afaction.GetDisplayName() + " " + afaction.FactionIndex + " (" + aAllegiance + ") is Neutral towards " + bfaction.Type + ": " + bfaction.GetDisplayName() + " " + bfaction.FactionIndex + " " + bfaction.FactionIndex + " (" + bAllegiance + ")", Verbosity.DoNotShow );
                    else
                        ArcenDebugging.ArcenDebugLogSingleLine( afaction.Type + ": " + afaction.GetDisplayName() + " " + afaction.FactionIndex + " (" + aAllegiance + ") is FRIENDLY towards " + bfaction.Type + ": " + bfaction.GetDisplayName() + " " + bfaction.FactionIndex + " " + bfaction.FactionIndex + " (" + bAllegiance + ")", Verbosity.DoNotShow );
                }
            }
        }
        #endregion

        #region HandleAIPForPlanetCapture_ForSpecificFaction
        /// <summary>
        /// Chris says: we can safely do this just on the host, and the client will see any ramifications of AIP changes.
        ///             assuming that the faction can see AIP at all, it would just go with the "frequent faction update" data
        ///             that the host sends to the clients.  No need for a gamecommand here for that kind of data.
        /// </summary>
        private void HandleAIPForPlanetCapture_ForSpecificFaction( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            bool AIPDebug = false;
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.GetControllingFaction() != aPlayerFaction )
                    continue;

                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( aPlayerFaction );
                //If we control this planet but haven't paid the AIP price yet, do so now
                if ( pFaction.AIPLeftFromCommandStation > 0 )
                {
                    if ( AIPDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Planet " + planet.Name + " add an additional " + pFaction.AIPLeftFromCommandStation + " from command station. fromWarpGate " + pFaction.AIPLeftFromWarpGate, Verbosity.DoNotShow );

                    GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt)pFaction.AIPLeftFromCommandStation + pFaction.AIPLeftFromWarpGate, AIPChangeReason.PlanetCapture, null, aPlayerFaction.FactionIndex, planet.Index, planet.InitialOwningAIFactionIndex );
                    //if we have just captured a new planet (and we haven't paid the AIP price for it) then we're allowed to scout for that capture
                    IScenarioImplementation scenarioImp = World_AIW2.Instance.GetScenarioImplementationSafe_OrNull();
                    if ( scenarioImp != null )
                        scenarioImp.DoScoutingAfterCommandStationDeath( Context, planet );
                    for ( int j = 0; j < World_AIW2.Instance.Factions.Count; j++ )
                    {
                        Faction localFaction = World_AIW2.Instance.Factions[j];
                        if ( localFaction.Type == FactionType.Player )
                        {
                            PlanetFaction localPlanetFaction = planet.GetPlanetFactionForFaction( localFaction );
                            localPlanetFaction.AIPLeftFromCommandStation = 0;
                            localPlanetFaction.AIPLeftFromWarpGate = 0;
                        }
                    }
                }
            }
        }
        #endregion

        #region HandleForcefieldsMovingBackOnHost_PerFaction
        /// The logic here for what to check coresponds to that in <c>ProtectionPlanning.RememberOriginPoint_IfNeeded</c>
        private void HandleForcefieldsMovingBackOnHost_PerFaction( Faction aHumanFaction, ArcenHostOnlySimContext Context )
        {
            //If we are a forcefield that has been bumped out of position, go back to position.
            foreach ( GameEntity_Squad entity in aHumanFaction.Squads( EntityRollupType.ProjectsForcefield ) )
            {
                if ( !entity.TypeData.MovesBackAfterBeingNorrised || !entity.TypeData.IsMobile )
                    continue; //only for forcefield generators
                if ( entity.GuardOrPatrolOffsetPoints.Count == 0 )
                    continue;
                EntityOrder currentOrder = entity.RemoveInvalidatedOrdersAndReturnFirstValid_IncludingDecollision();
                if ( currentOrder.TypeData != null )
                    continue; //if we already have a move command, don't do anything
                int rangeToDetectShieldMoving = 500;
                if ( Mat.DistanceBetweenPointsImprecise( entity.WorldLocation, entity.GuardOrPatrolOffsetPoints[0] ) > rangeToDetectShieldMoving )
                {
                    GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_PlayerForcefieldReturns], GameCommandSource.AnythingElse );
                    moveCommand.PlanetOrderWasIssuedFrom = entity.Planet.Index;
                    moveCommand.RelatedPoints.Add( entity.GuardOrPatrolOffsetPoints[0] );
                    moveCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetNaturalObjectFactionNeverNull(), moveCommand, false );
                    entity.GuardOrPatrolOffsetPoints.Clear();
                }
            }
        }
        #endregion

        #region HandleAchievementsEarnedHostOnly
        private void HandleAchievementsEarnedHostOnly( ArcenHostOnlySimContext Context )
        {
            int debugCode = 0;
            //if the game has already been won, then re-check post-victory achievements every 2 seconds since those might not have submitted for some reason.
            bool debug = false;
            Achievement achievement = null;
            try
            {
                debugCode = 100;
                if ( World.Instance.ConclusionType == CampaignConclusionType.Won )
                    BaseScenario.DoPostVictoryAchievementChecks();
                for ( int i = 0; i < AchievementTable.Instance.Rows.Count; i++ )
                {
                    debugCode = 200;
                    achievement = AchievementTable.Instance.Rows[i];
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("Checking achievement  " + achievement.InternalName, Verbosity.DoNotShow );
                    if ( achievement == null )
                        continue;
                    debugCode = 300;
                    if ( achievement.IsTimeElapsedAchievement )
                    {
                        debugCode = 400;
                        if ( achievement.GetIsAlreadyCompleteOrBlockedByCheating( AchievementOnClient.CanOnlyBeLoggedByHost ) )
                            continue; //skipping these is more efficient than checking them, especially for certain kinds
                        if ( World.Instance.ConclusionType == CampaignConclusionType.NotConcluded )
                        {
                            //only do this for games that aren't done yet
                            if ( World_AIW2.Instance.GameSecond >= achievement.TimeElapsed )
                            {
                                if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                                { }
                            }
                        }
                    }
                    debugCode = 500;
                    if ( achievement.IsShipKillsAchievement )
                    {
                        debugCode = 600;
                        if ( achievement.GetIsAlreadyCompleteOrBlockedByCheating( AchievementOnClient.CanOnlyBeLoggedByHost ) )
                            continue; //skipping these is more efficient than checking them, especially for certain kinds

                        int shipsLost = 0;
                        int shipsKilled = 0;
                        foreach ( Faction faction in World_AIW2.Instance.AllPlayerFactions )
                        {
                            //KILL_DEATH_TRACKING_TODO
                            shipsLost += faction.TotalUnitsLost;
                            shipsKilled += faction.TotalUnitsKilled;
                        }

                        if ( achievement.ShipsLost != 0 && achievement.ShipsKilled != 0 )
                        {
                            //count both lost and killed
                            if ( shipsLost >= achievement.ShipsLost && shipsKilled >= achievement.ShipsKilled )
                            {
                                if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                                { }
                            }
                        }
                        else if ( achievement.ShipsLost != 0 )
                        {
                            //ships lost achievement
                            if ( shipsLost >= achievement.ShipsLost )
                            {
                                if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                                { }
                            }
                        }
                        else if ( achievement.ShipsKilled != 0 )
                        {
                            //ships killed achievement
                            if ( shipsKilled >= achievement.ShipsKilled )
                            {
                                if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                                { }
                            }
                        }
                    }
                    debugCode = 700;
                    if ( achievement.IsControlPlanetsBasedAchievement )
                    {
                        debugCode = 800;
                        if ( achievement.GetIsAlreadyCompleteOrBlockedByCheating( AchievementOnClient.CanOnlyBeLoggedByHost ) )
                            continue; //skipping these is more efficient than checking them, especially for certain kinds
                        int controlledPlanets = 0;
                        foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                        {
                            Faction faction = planet.GetControllingFaction();
                            if ( faction != null && faction.Type == FactionType.Player )
                                controlledPlanets++;
                        }

                        if ( controlledPlanets >= achievement.NumberOfPlanetsToControl )
                        {
                            if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                            { }
                        }
                    }
                    debugCode = 900;
                    if ( achievement.IsResourceAcquisitionAchievement )
                    {
                        debugCode = 1000;
                        if ( achievement.GetIsAlreadyCompleteOrBlockedByCheating( AchievementOnClient.CanOnlyBeLoggedByHost ) )
                            continue; //skipping these is more efficient than checking them, especially for certain kinds
                        if ( ArcenStrings.Equals( achievement.Resource, "Metal" ) )
                        {
                            int amountOfResource = 0;
                            foreach ( Faction faction in World_AIW2.Instance.AllPlayerFactions )
                                amountOfResource += faction.StoredMetal.IntValue;
                            if ( achievement.AmountOfResource <= amountOfResource )
                            {
                                if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                                { }
                            }
                        }
                        if ( ArcenStrings.Equals( achievement.Resource, "Energy" ) )
                        {
                            int amountOfResource = 0;
                            foreach ( Faction faction in World_AIW2.Instance.AllPlayerFactions )
                                amountOfResource += faction.NetEnergy;
                            if ( achievement.AmountOfResource <= amountOfResource )
                            {
                                if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                                { }
                            }
                        }
                        if ( ArcenStrings.Equals( achievement.Resource, "Hacking" ) )
                        {
                            int amountOfResource = 0;
                            foreach ( Faction faction in World_AIW2.Instance.AllPlayerFactions )
                                amountOfResource += faction.StoredHacking.IntValue;
                            if ( achievement.AmountOfResource <= amountOfResource )
                            {
                                if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                                { }
                            }
                        }
                    }
                    debugCode = 1100;
                    if ( achievement.IsSuperterminalHackAchievement )
                    {
                        debugCode = 1200;
                        if ( achievement.GetIsAlreadyCompleteOrBlockedByCheating( AchievementOnClient.CanOnlyBeLoggedByHost ) )
                            continue; //skipping these is more efficient than checking them, especially for certain kinds
                        Faction aifaction = World_AIW2.Instance.AIFactions[0];
                        if ( aifaction == null )
                            continue;
                        AISentinelsCoreData factionExternal = aifaction.TryGetAISentinelsCoreData()?.SentinelInfo;
                        FInt superterminalChange = FInt.Zero;
                        if ( factionExternal != null )
                        {
                            for ( int j = 0; j < GlobalAIWorldBaseInfo.Instance.AIPChangeHistory.Count; j++ )
                            {
                                AIPChange change = GlobalAIWorldBaseInfo.Instance.AIPChangeHistory[j];
                                if ( change.Reason == AIPChangeReason.Hacking && change.RelatedEntityTypeData != null &&
                                     change.RelatedEntityTypeData.GetHasTag( "SuperTerminal" ) )
                                    superterminalChange += change.Change.Inverse;
                            }
                        }
                        if ( superterminalChange >= achievement.AIPToReduceViaSuperterminal )
                        {
                            if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                            { }
                        }
                    }
                    debugCode = 1300;
                    switch ( achievement.ConditionType )
                    {
                        case AchievementConditionType.KillUnit:
                            debugCode = 1500;
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine("\tKill Unit Achievement", Verbosity.DoNotShow );
                            if ( achievement.GetIsAlreadyCompleteOrBlockedByCheating( AchievementOnClient.CanOnlyBeLoggedByHost ) )
                                continue; //skipping these is more efficient than checking them, especially for certain kinds
                            int quantityNeeded = Math.Max( achievement.ConditionMagnitude, 1 );
                            int quantityFound = 0;
                            debugCode = 1510;
                            foreach ( GameEntityTypeData entityData in GameEntityTypeDataTable.Instance.GetTypesMatching( achievement.ConditionStringMode, achievement.ConditionRelatedStrings ) )
                            {
                                if ( entityData == null )
                                    continue;
                                debugCode = 1520;
                                int kills = 0;
                                foreach ( Faction faction in World_AIW2.Instance.AllPlayerFactions )
                                {
                                    if ( faction.HasKilledUnitType[entityData] )
                                        if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                                        { }
                                }

                                if ( kills <= 0 )
                                    continue;
                                quantityFound += kills;
                            }
                            debugCode = 1530;
                            if ( quantityFound >= quantityNeeded )
                            {
                                debugCode = 1540;
                                //StringBuilder matchesData = new StringBuilder();
                                //foreach ( GameEntityTypeData entityData in GameEntityTypeDataTable.Instance.GetTypesMatching( achievement.ConditionStringMode, achievement.ConditionRelatedStrings ) )
                                //{
                                //    int kills = stats.GetKills( entityData );
                                //    matchesData.Append( "\nmatch: " ).Append( entityData.InternalName ).Append( ", kills: " ).Append( kills );
                                //}

                                //ArcenDebugging.ArcenDebugLogSingleLine( achievement.InternalName + " Achievement was ok because: quantityFound " + quantityFound +
                                //    ", quantityNeeded: " + quantityNeeded + ", faction: " + faction.GetDisplayName() + matchesData.ToString(), Verbosity.DoNotShow );

                                if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                                { }
                            }
                            break;
                        case AchievementConditionType.AITechLevel:
                            debugCode = 1600;
                            if ( achievement.GetIsAlreadyCompleteOrBlockedByCheating( AchievementOnClient.CanOnlyBeLoggedByHost ) )
                                continue; //skipping these is more efficient than checking them, especially for certain kinds
                            for ( int j = 0; j < World_AIW2.Instance.AIFactions.Count; j++ )
                            {
                                Faction otherFaction = World_AIW2.Instance.AIFactions[j];
                                AISentinelsCoreData factionExternal = otherFaction.TryGetAISentinelsCoreData()?.SentinelInfo;
                                if ( factionExternal == null )
                                    continue;
                                if ( otherFaction.CurrentGeneralMarkLevel < achievement.ConditionMagnitude )
                                    continue;
                                if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                                { }
                                break;
                            }
                            break;
                        case AchievementConditionType.UpgradeFleetLineToMark:
                          if ( debug )
                          {
                              ArcenDebugging.ArcenDebugLogSingleLine("\tUpgrade Fleet Line Achievement. (" + achievement.InternalName +") mode " + achievement.ConditionStringMode + " magnitude " + achievement.ConditionMagnitude+ ". Related strings:" , Verbosity.DoNotShow );
                              for ( int j = 0; j < achievement.ConditionRelatedStrings.Count; j++ )
                                  ArcenDebugging.ArcenDebugLogSingleLine("\t\t" + achievement.ConditionRelatedStrings[j], Verbosity.DoNotShow );
                          }

                          foreach ( GameEntityTypeData entityData in GameEntityTypeDataTable.Instance.GetTypesMatching( achievement.ConditionStringMode, achievement.ConditionRelatedStrings ) )
                          {
                              Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                              int markLevel = localFaction.GetGlobalMarkLevelForShipLine( entityData ); //this doesn't work for all unit types, but give it a try first just in case
                              if ( markLevel < achievement.ConditionMagnitude )
                              {
                                  //we're too low. Try looking at every unit with this tag (for things like spire cities)
                                  //NOTE: This is very slow, so only use this achievement for rare units. If you make it something like V-Wings the game will chug
                                  for ( int j = 0; j < achievement.ConditionRelatedStrings.Count && markLevel < achievement.ConditionMagnitude; j++ )
                                  {
                                      foreach ( GameEntity_Squad entity in localFaction.Squads( achievement.ConditionRelatedStrings[j] ) )
                                      {
                                          if ( markLevel < entity.CurrentMarkLevel )
                                              markLevel = entity.CurrentMarkLevel;
                                          if ( markLevel >= achievement.ConditionMagnitude )
                                              break;
                                      }
                                  }
                              }
                              if ( markLevel >= achievement.ConditionMagnitude )
                              {
                                  if ( debug )
                                  ArcenDebugging.ArcenDebugLogSingleLine("Triggering achievement " + achievement.InternalName + " since we have a " + entityData.GetDisplayName() + " at mark " + achievement.ConditionMagnitude, Verbosity.DoNotShow );
                                  if ( achievement.MarkCompleteAndReturnIfAnyDataChanged( false ) ) //don't save over and over again if multiple achievements trip
                                  { }
                              }
                          }
                        break;
                    }
                    debugCode = 2000;
                } //end of the for loop
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception during HandleAchievementsEarned debugCode " + debugCode + " achievement <" + achievement.InternalName + "> " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion end HandleAchievementsEarnedHostOnly

        #region HandleAutoKiteOnHost_PerFaction
        private void HandleAutoKiteOnHost_PerFaction( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            PlayerAccount playerControlling = aPlayerFaction.GetFirstAssociatedPlayerAccountOrNull();
            if ( playerControlling == null )
                return; //only do this for factions that are being controlled by a player account!

            //the PlayerAccount settings for autokite are synced to the host, and the host
            //handles all of the settings via gamecommands from here
            //this is a nonsim area, so no direct changing of data!
            bool autoKite = playerControlling.GetNetworkAttachedBoolBySetting( "AutoKite" );
            EndpointFunctions.SetFactionKitingIfNeeded( aPlayerFaction, autoKite );
        }
        #endregion

        #region HandleAutoFRD_ForSpecificFaction
        private void HandleAutoFRD_ForSpecificFaction( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            PlayerAccount playerControlling = aPlayerFaction.GetFirstAssociatedPlayerAccountOrNull();
            if ( playerControlling == null )
                return; //only do this for factions that are being controlled by a player account!

            bool debug = false;
            if ( playerControlling.GetNetworkAttachedBoolBySetting( "AutoFRDEngineers" ) )
            {
                GameCommand command = null;
                EntityBehaviorType targetType = EntityBehaviorType.Attacker_Full;
                foreach ( GameEntity_Squad entity in aPlayerFaction.Squads( EntityRollupType.HasAnyMetalFlows ) )
                {
                    if ( entity.TypeData.GetHasTag( "Engineer" ) )
                    {
                        if ( entity.Orders.Behavior != EntityBehaviorType.Attacker_Full )
                        {
                            if ( command == null )
                                command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetBehavior_FromPlayer], GameCommandSource.AnythingElse );
                            command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                        }
                    }
                }
                if ( command != null )
                {
                    command.RelatedMagnitude = (int)targetType;
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Queueing frd command with " + command.RelatedEntityIDs.Count + " engineers", Verbosity.DoNotShow );
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetNaturalObjectFactionNeverNull(), command, false );
                }
            }
        }
        #endregion

        //, "HostOnlyJournalsAndAutosaveAndSimilarHandler-EntitiesTriggering"
        #region HandleDebugSpawns_ForSpecificFaction
        private void HandleDebugSpawns_ForSpecificFaction( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            if ( aPlayerFaction.Debug_SpawnShips )
            {
                int debugStage = 0;
                List<SafeSquadWrapper> playerKings = GameEntity_Squad.GetTemporarySquadList( "HostOnlyJournalsAndAutosaveAndSimilarHandler-HandleDebugSpawns_ForSpecificFaction-playerKings-Debug_SpawnShips", 10f );
                if ( playerKings == null ) //blocked for teardown/shutdown; bail
                    return;
                try
                {
                    debugStage = 100;

                    debugStage = 200;
                    foreach ( GameEntity_Squad entity in aPlayerFaction.Squads( EntityRollupType.KingUnitsOnly ) )
                    {
                        if ( entity.GetFactionTypeSafe() == FactionType.Player )
                            playerKings.Add( entity );
                    }
                    debugStage = 300;
                    for ( int i = 0; i < playerKings.Count; i++ )
                    {
                        debugStage = 400;
                        GameEntity_Squad entity = playerKings[i].GetSquad();
                        if ( entity == null )
                            continue;
                        debugStage = 500;
                        CheatsAndCommands.PlayerPotluck( entity.GetFactionOrNull_Safe(), entity.Planet, entity.WorldLocation, "TransportFlagship_Starter", "Potluck ", Context, true );
                    }
                }
                catch ( System.Threading.ThreadAbortException ) { } //simply return
                catch ( Exception e )
                {
                    if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                        ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in Debug_SpawnShips. debugStage " + debugStage + " " + e.ToString(), Verbosity.ShowAsError );
                }
                finally
                {
                    GameEntity_Squad.ReleaseTemporarySquadList( playerKings );                    
                }
            }
            if ( aPlayerFaction.Debug_SpawnZenithPowerGenerator )
            {
                List<SafeSquadWrapper> playerKings = GameEntity_Squad.GetTemporarySquadList( "HostOnlyJournalsAndAutosaveAndSimilarHandler-HandleDebugSpawns_ForSpecificFaction-playerKings-Debug_SpawnZenithPowerGenerator", 10f );
                if ( playerKings == null ) //blocked for teardown/shutdown; bail
                    return;

                foreach ( GameEntity_Squad entity in aPlayerFaction.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    if ( entity.GetFactionTypeSafe() == FactionType.Player )
                        playerKings.Add( entity );
                }
                for ( int i = 0; i < playerKings.Count; i++ )
                {
                    GameEntity_Squad entity = playerKings[i].GetSquad();
                    if ( entity == null )
                        continue;
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ZenithPowerGenerator" );
                    PlanetFaction pFaction = entity.PlanetFaction;
                    ArcenPoint spawnLocation = entity.Planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 300 ) );
                    GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                        pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "Cheat-ZPG" );
                }
                GameEntity_Squad.ReleaseTemporarySquadList( playerKings );
            }
            aPlayerFaction.Debug_SpawnShips = false;
            aPlayerFaction.Debug_SpawnZenithPowerGenerator = false;
        }
        #endregion

        #region HandleLoreJournals_HostOnly_NotPlayerSpecific
        private void HandleLoreJournals_HostOnly_NotPlayerSpecific( ArcenHostOnlySimContext Context )
        {
            bool foundBaseZenith = false; //these are zenith of the "old" sort, from this dimension. There are multiple factions possible, so no colour
            bool foundNewZenith = false; //these are new zenith from alternate dimensions. There are multiple factions possible, so no colour
            //we track the faction so we can use the colour
            Faction spireFaction = null;
            Faction riskAnalyzerFaction = null;
            Faction scourgeFaction = null;
            Faction aiFaction = null;
            Faction vassalFaction = null;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction faction = World_AIW2.Instance.Factions[i];
                if ( faction == null )
                    continue;
                if ( !faction.HasBeenSeenByPlayer &&
                     !AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "AlwaysShowFactions" ) )
                    continue;
                if ( faction.RandomImpact != TypeDifficulty.Unset && //this is a random faction
                     !faction.HasBeenSeenByPlayer ) //that hasn't been seen by a player
                    continue; //no lore messages for factions the player hasn't seen
                switch (faction.SpecialFactionData.InternalName )
                {
                    case "DevourerGolem":
                    case "ZenithDysonSphere":
                    case "AntagonizedDysonSphere":
                    case "ZenithMiners":
                    case "ZenithTrader":
                        foundBaseZenith = true;
                        break;
                    case "DarkZenith":
                    case "ZenithArchitrave":
                        foundNewZenith = true;
                        break;
                }
                if ( faction.IsVassal )
                    vassalFaction = faction;
                if ( faction.SpecialFactionData.InternalName == "FallenSpire" )
                    spireFaction = faction;
                if ( faction.SpecialFactionData.InternalName == "AIRiskAnalyzers" )
                    riskAnalyzerFaction = faction;
                if ( faction.SpecialFactionData.InternalName == "Scourge" )
                    scourgeFaction = faction;
                
                if ( faction.Type == FactionType.AI )
                    aiFaction = faction;
            }
            if ( foundBaseZenith )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Base_Lore_Zenith", string.Empty, null, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            if ( foundNewZenith )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Expansion_Lore_Zenith", string.Empty, null, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            if ( spireFaction != null )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Base_Lore_Spire", string.Empty, spireFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            if ( riskAnalyzerFaction != null )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Base_Lore_RiskAnalyzers", string.Empty, riskAnalyzerFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            if ( scourgeFaction != null )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Base_Lore_Scourge", string.Empty, scourgeFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            if ( aiFaction != null )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Base_Lore_TheAI", string.Empty, aiFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );

            //Vassals are a deprecated feature from DLC3; much of the code is in, but there was not time for the UI; also it was felt this was a major potential source of feature creep, as players might want to do More and More
            // if ( vassalFaction != null )
            //     World_AIW2.Instance.QueueLogJournalEntryToSidebar( "EOTA_Vassal_Gameplay", string.Empty, vassalFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        }
        #endregion

        #region Helper_GetPlanetCountData
        private void Helper_GetPlanetCountData( Faction aHumanFaction, out int PlanetsOwnedByMyFaction,
            out int PlanetsOwnedByAnyHumanFaction, out int PlanetsPaidFor, out int PlanetsNeutered )
        {
            int planetsOwnedByMyFaction = 0;
            int planetsOwnedByAnyHumanFaction = 0;
            int planetsPaidFor = 0;
            int planetsNeutered = 0;
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                PlanetFaction pFactionForAHuman = planet.GetPlanetFactionForFaction( aHumanFaction );
                #region Check For If You Paid AIP For This
                if ( pFactionForAHuman.AIPLeftFromCommandStation == 0 )
                {
                    //AI homeworlds and DZ planets never have AIPLeftFromCommandStation, per notes from Badger
                    if ( planet.PopulationType == PlanetPopulationType.AIHomeworld || planet.PopulationType == PlanetPopulationType.DarkZenith ) //ignore AIBastionWorld ok.
                    {
                        //if an AI homeworld is owned by players, then go ahead and count it
                        Faction aiHomeworldFaction = planet.GetControllingFaction();
                        if ( aiHomeworldFaction != null && aiHomeworldFaction.Type == FactionType.Player )
                            planetsPaidFor++;
                    }
                    else //non-homeworld AI planets can be safely counted
                        planetsPaidFor++;
                }
                #endregion Check For If You Paid AIP For This

                #region Check For Neutered Planets
                //consider planets where we overwhelm the enemy as being neutered, to handle third parties beyond the AI.
                if ( pFactionForAHuman.DataByStance[FactionStance.Hostile].TotalStrength <
                     pFactionForAHuman.DataByStance[FactionStance.Self].TotalStrength + pFactionForAHuman.DataByStance[FactionStance.Friendly].TotalStrength )
                    planetsNeutered++;
                else //not considered neutered by dint of us or allies having overwhelming force there.
                {
                    //check for actual neutering based on guard posts
                    int reinforcementLocationCount = 0;
                    foreach ( GameEntity_Squad reinforcementPoint in planet.Squads( EntityRollupType.ReinforcementLocations ) )
                    {
                        reinforcementLocationCount++;
                    }
                    //if there are no reinforcement points left, or less than a third of them are left, consider the planet neutered
                    if ( reinforcementLocationCount == 0 || reinforcementLocationCount <= planet.MaxReinforcementPlacesEverSeenHere / 3 )
                        planetsNeutered++;
                }
                #endregion Check For Neutered Planets

                if ( planet.GetControllingFaction() == aHumanFaction )
                    planetsOwnedByMyFaction++;
                if ( planet.GetControllingFaction()?.SpecialFactionData.Type == FactionType.Player )
                    planetsOwnedByAnyHumanFaction++;
            }

            PlanetsOwnedByMyFaction = planetsOwnedByMyFaction;
            PlanetsOwnedByAnyHumanFaction = planetsOwnedByAnyHumanFaction;
            PlanetsPaidFor = planetsPaidFor;
            PlanetsNeutered = planetsNeutered;
        }
        #endregion

        #region Helper_CanTryLogCentralJournal
        public bool Helper_CanTryLogCentralJournal( string UniqueID_ToUseIfGroupIDNotPresent )
        {
            if ( UniqueID_ToUseIfGroupIDNotPresent != null && UniqueID_ToUseIfGroupIDNotPresent.Length > 0 )
            {
                JournalEntry entry = JournalEntryTable.Instance.GetRowByNameOrNullIfNotFound( UniqueID_ToUseIfGroupIDNotPresent );
                if ( entry != null )
                {
                    if ( entry.IsBlockedFromTriggering )
                        return false;
                }

                for ( int i = 0; i < World_AIW2.Instance.JournalHistory.Count; i++ )
                {
                    JournalEntryInCampaign journal = null;
                    try
                    {
                        journal = World_AIW2.Instance.JournalHistory[i];
                    }
                    catch { continue; }
                    if ( journal == null )
                        continue;
                    if ( journal.UniqueID == UniqueID_ToUseIfGroupIDNotPresent )
                        return false; //we already logged this one!

                }
                return true; //have not had it yet
            }
            else
                return true;
        }
        #endregion

        //From here until the next marker are the journal entries run on the host only, but not for any specific faction
        //******************************************************************************************

        #region HandleHelperJournals_HostOnly_NotPlayerSpecific
        public void HandleHelperJournals_HostOnly_NotPlayerSpecific( ArcenHostOnlySimContext Context, bool atLeastSomeAutomationOnEveryPlayer )
        {
            if ( World_AIW2.Instance.TutorialOrNull != null )
                return; //journals don't go into tutorials
            if ( !ArcenNetworkAuthority.GetIsHostMode() )
                return; //no need for this on clients!
            if ( !GameSettings.Current.GetBoolBySetting( "BeginnerJournals" ) ) //only when enabled ON THE HOST, not checking network-style
                return;

            //CRITICAL!  These things should relate to ALL players, as they are run on the host
            //           These pull a reference to the host's faction, but really should not be too faction-bound.
            //           If there are per-faction tips, those should happen in
            //-------------------------------------------------------------------

            Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();

            //there are no from-combat things here
            if ( World_AIW2.Instance.GameSecond % 55 != 0 )
                return; //only do this "every so often", since it's expensive

            Helper_GetPlanetCountData( localFaction, out int PlanetsOwnedByMyFaction, out int PlanetsOwnedByAnyHumanFaction,
                out int PlanetsPaidFor, out int PlanetsNeutered );

            //these journals are opt in.
            //The goal is to catch some places where a new player might be making bad ideas, then give them messages about it.
            Journal_TooMuchAIP_HostOnly( Context, PlanetsPaidFor, PlanetsNeutered );

            Journal_WatchFleets_HostOnly( Context );
            Journal_QueueUnload( Context );

            Journal_GlobalThreat_HostOnlyForAllPlayers( localFaction, Context );

            //early game progression items
            Journal_GameProgressionItems_HostOnlyForAllPlayers( Context );

            if ( !atLeastSomeAutomationOnEveryPlayer && World_AIW2.Instance.GameSecond > 300 && //If it's a few minutes into the game and any player has no automation, suggest it
                 !FactionUtilityMethods.Instance.OnlyNecromancerFactions() ) //not applicable for necromancers
            {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_Automation", string.Empty, null, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
            if ( World_AIW2.Instance.GameSecond > 180 )
            {
                PlayerAccount playerControlling = localFaction.GetFirstAssociatedPlayerAccountOrNull();
                if ( playerControlling != null && !playerControlling.GetNetworkAttachedBoolBySetting( "WaitForStragglers" ) )
                {
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_WaitForStragglers", string.Empty, null, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                }
            }
        }
        #endregion end HandleHelperJournals_HostOnly_LocalPlayerOnly

        #region Journal_TooMuchAIP_HostOnly
        public void Journal_TooMuchAIP_HostOnly( ArcenHostOnlySimContext Context, int planetsPaidFor, int planetsNeutered )
        {
            if ( !Helper_CanTryLogCentralJournal( "Beginner_TooMuchAIP" ) )
                return;
            /*  Warn the player about accruing too much AIP early */

            int numForWarning = 4;
            if ( planetsPaidFor > numForWarning &&
                 (planetsNeutered - planetsPaidFor) < 2 )
            {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_TooMuchAIP", string.Empty, null, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
        }
        #endregion

        #region Journal_GameProgressionItems_HostOnlyForAllPlayers
        private bool hasLoggedARSMessage = false;
        private bool hasLoggedFlagshipMessage = false;
        private bool hasLoggedAdditionalMessage = false;
        public void Journal_GameProgressionItems_HostOnlyForAllPlayers( ArcenHostOnlySimContext Context )
        {
            if ( World_AIW2.Instance.TutorialOrNull != null )
                return; //These journals don't go into tutorials

            if ( hasLoggedARSMessage && hasLoggedFlagshipMessage )
            {
                if ( !hasLoggedAdditionalMessage )
                {
                    hasLoggedFlagshipMessage = true;
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_NextSteps", string.Empty, null, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                }
                return; //we're done
            }
            Planet bestFlagshipPlanetNearAnyPlayer = null;
            int bestFlagshipEnemyStr = 999999999;
            foreach ( Faction faction in World_AIW2.Instance.EmpireStylePlayerFactions )
            {
                Planet flagshipPlanet_OrNullIfNone = FactionUtilityMethods.Instance.GetNearestFlagshipToKing_OrNullIfNone( faction, Context );
                int flagshipEnemyStr = flagshipPlanet_OrNullIfNone == null ? 0 : flagshipPlanet_OrNullIfNone.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Hostile].TotalStrength;

                if ( bestFlagshipPlanetNearAnyPlayer == null || flagshipEnemyStr < bestFlagshipEnemyStr )
                {
                    bestFlagshipPlanetNearAnyPlayer = flagshipPlanet_OrNullIfNone;
                    bestFlagshipEnemyStr = flagshipEnemyStr;
                }
            }

            Planet bestARSPlanetNearAnyPlayer = null;
            int bestARSEnemyStr = 999999999;
            foreach ( Faction faction in World_AIW2.Instance.EmpireStylePlayerFactions )
            {
                Planet arsPlanet_OrNullIfNone = FactionUtilityMethods.Instance.GetNearestARSToKing_OrNullIfNone( faction, Context );
                int arsEnemyStr = arsPlanet_OrNullIfNone == null ? 0 : arsPlanet_OrNullIfNone.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Hostile].TotalStrength;

                if ( bestARSPlanetNearAnyPlayer == null || arsEnemyStr < bestARSEnemyStr )
                {
                    bestARSPlanetNearAnyPlayer = arsPlanet_OrNullIfNone;
                    bestARSEnemyStr = arsEnemyStr;
                }
            }

            if ( !hasLoggedARSMessage && !hasLoggedFlagshipMessage )
            {
                //play the message for the weaker planet
                if ( bestFlagshipEnemyStr < bestARSEnemyStr && bestFlagshipPlanetNearAnyPlayer != null )
                {
                    hasLoggedFlagshipMessage = true;
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_CaptureNearbyFlagship", string.Empty, null, null, bestFlagshipPlanetNearAnyPlayer, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                }
                else
                {
                    if ( bestARSPlanetNearAnyPlayer != null )
                    {
                        hasLoggedARSMessage = true;
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_HackNearbyARS", string.Empty, null, null, bestARSPlanetNearAnyPlayer, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    }
                }
                return;
            }
            if ( hasLoggedFlagshipMessage )
            {
                //see if we should log the ARS message

                int numTransportFleets = 0;
                foreach ( Faction faction in World_AIW2.Instance.EmpireStylePlayerFactions )
                {
                    foreach ( Fleet fleet in World_AIW2.Instance.Fleets( faction, FleetStatus.CenterpieceMustLiveOrLooseFleet ) )
                    {
                        if ( fleet == null || fleet.Centerpiece.GetSquad() == null )
                            continue;
                        if ( fleet.Centerpiece.GetSquad().TypeData.SpecialType != SpecialEntityType.MobileStrikeCombatFleetFlagship )
                            continue;
                        numTransportFleets++;
                    }
                }
                if ( numTransportFleets == 2 )
                {
                    if ( bestARSPlanetNearAnyPlayer != null )
                    {
                        hasLoggedARSMessage = true;
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_HackNearbyARS", string.Empty, null, null, bestARSPlanetNearAnyPlayer, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    }
                }
            }
            if ( hasLoggedARSMessage )
            {
                bool hasFoundARSHack = false;
                foreach ( Faction faction in World_AIW2.Instance.EmpireStylePlayerFactions )
                {
                    for ( int i = 0; i < faction.HackingHistory.Count; i++ )
                    {
                        if ( faction.HackingHistory[i].HackType.InternalName == "HackToGrantShipLine_ARS" )
                        {
                            hasFoundARSHack = true;
                            break;
                        }
                    }
                }
                if ( hasFoundARSHack )
                {
                    if ( bestFlagshipPlanetNearAnyPlayer != null )
                    {
                        hasLoggedFlagshipMessage = true;
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_CaptureNearbyFlagship", string.Empty, null, null, bestFlagshipPlanetNearAnyPlayer, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    }
                }
            }
        }
        #endregion end Journal_GameProgressionItems_HostOnlyForAllPlayers

        #region Journal_GlobalThreat_HostOnlyForAllPlayers
        public void Journal_GlobalThreat_HostOnlyForAllPlayers( Faction hostFaction, ArcenHostOnlySimContext Context )
        {
            FInt threatHumans = FInt.Zero;
            FInt threatOthers = FInt.Zero;
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                //if ( planet.GetControllingFactionType() == FactionType.Player )
                //    return DelReturn.Continue;
                var data = planet.GetPlanetFactionForFaction( hostFaction ).DataByStance[FactionStance.Hostile]; //we use the host faction here, but that data is the same on any human faction
                if ( data.RelativeToHumanTeam_ThreatStrength > 0 )
                    threatHumans += data.RelativeToHumanTeam_ThreatStrength;
                if ( data.RelativeToOtherFaction_ThreatStrength > 0 )
                    threatOthers += data.RelativeToOtherFaction_ThreatStrength;
            }
            //add some journals here
            if ( threatHumans > (200 * 1000) )
            {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_LowThreat", string.Empty, null, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
            if ( threatHumans > (800 * 1000) )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_HighThreat", string.Empty, null, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        }
        #endregion

        #region Journal_QueueUnload
        public void Journal_QueueUnload( ArcenHostOnlySimContext Context )
        {
            if ( World_AIW2.Instance.GameSecond % 2700 == 0 )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_QueueUnload", string.Empty, null, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        }
        #endregion

        #region Journal_WatchFleets_HostOnly
        public void Journal_WatchFleets_HostOnly( ArcenHostOnlySimContext Context )
        {
            if ( World_AIW2.Instance.GameSecond % 690 == 0 ) //since this only happens on one specific second, we don't need to check if it was already done
            {
                foreach ( Faction faction in World_AIW2.Instance.EmpireStylePlayerFactions )
                {
                    //each player account with each human faction must be watching a fleet or you get the message
                    for ( int k = 0; k < faction.Config.CountOfPlayersFactionControllingFaction; k++ )
                    {
                        byte playerAccountID = faction.Config.GetPKIDOfControllingPlayerAtIndex( k );
                        bool foundWatchedFleet = false;
                        foreach ( Fleet fleet in World_AIW2.Instance.Fleets( faction, FleetStatus.CenterpieceMustLiveOrLooseFleet ) )
                        {
                            if ( fleet == null || fleet.Centerpiece.GetSquad() == null )
                                continue;
                            if ( fleet.Centerpiece.GetSquad().PlanetFaction.Faction != faction )
                                continue;
                            if ( fleet.GetIsFleetOnPlayerWatchlist( playerAccountID ) )
                            {
                                foundWatchedFleet = true;
                                break;
                            }
                        }

                        if ( !foundWatchedFleet )
                        {
                            World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_WatchFleets", string.Empty, null, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                            return; //once we miss a watched fleet status for anyone, then we send it but stop having more after that
                        }
                    }
                }
            }
        }
        #endregion

        //From here down is the journal entries that are run on the host for each faction in turn
        //******************************************************************************************

        #region HandleHelperJournals_HostOnly_RunOnEachPlayerFaction
        public void HandleHelperJournals_HostOnly_RunOnEachPlayerFaction( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            PlayerAccount playerControlling = aPlayerFaction.GetFirstAssociatedPlayerAccountOrNull();
            if ( playerControlling == null )
                return; //only do this for factions that are being controlled by a player account!
            if ( !playerControlling.GetNetworkAttachedBoolBySetting( "BeginnerJournals" ) )
                return; //only do this for this faction if that player account controlling it has that setting enabled.  This is network-synced.

            Journal_CombatAndPlanets_AnyPlayer( aPlayerFaction, Context ); //these journals play during combat, so not on the time interval

            //these journals need to play quickly when you show up with (say) a raid engine
            Journal_HandleEntriesTriggeredByPlayerEncounter_AnyPlayer( aPlayerFaction, Context ); 

            if ( World_AIW2.Instance.GameSecond % 55 != 0 )
                return; //only do this "every so often", since it's expensive

            Helper_GetPlanetCountData( aPlayerFaction, out int PlanetsOwnedByMyFaction, out int PlanetsOwnedByAnyHumanFaction,
                out int PlanetsPaidFor, out int PlanetsNeutered );

            Journal_TechUnlocks_AnyPlayer( aPlayerFaction, Context );
            Journal_UpgradeMetal_AnyPlayer( aPlayerFaction, Context, PlanetsOwnedByMyFaction );
            Journal_GetTSS_AnyPlayer( aPlayerFaction, Context, PlanetsOwnedByMyFaction );
            Journal_OfficerFleets_AnyPlayer( aPlayerFaction, Context );
            Journal_BuildTurrets_AnyPlayer( aPlayerFaction, Context );
            Journal_FlagshipBits_AnyPlayer( aPlayerFaction, Context );
            Journal_WarpGateNearKing_AnyPlayer( aPlayerFaction, Context, PlanetsOwnedByMyFaction );
            Journal_SpendScience_AnyPlayer( aPlayerFaction, Context );
        }
        #endregion
        public void HandleBeaconJournals_HostOnly_RunOnEachPlayerFaction( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            if ( World_AIW2.Instance.Setup.GetBoolBySetting( "BeaconsEnabled" ) )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "BeaconEncounter", string.Empty, null, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );

        }
        public void HandleAdvancedHelperJournals_HostOnly_RunOnEachPlayerFaction( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            PlayerAccount playerControlling = aPlayerFaction.GetFirstAssociatedPlayerAccountOrNull();
            if ( playerControlling == null )
                return; //only do this for factions that are being controlled by a player account!
            if ( !playerControlling.GetNetworkAttachedBoolBySetting( "AdvancedHelperTips" ) )
                return; //only do this for this faction if that player account controlling it has that setting enabled.  This is network-synced.

            if ( World_AIW2.Instance.GameSecond > 10 )
                return; //only do this in the first 10 seconds

            Journal_ExpertModeFuel_AnyPlayer( aPlayerFaction, Context );
            Journal_ChallengerMode_AnyPlayer( aPlayerFaction, Context );
            Journal_ExpertMode_AnyPlayer( aPlayerFaction, Context );
            Journal_LogisticianMode_AnyPlayer( aPlayerFaction, Context );
            Journal_DeathwishMode_AnyPlayer( aPlayerFaction, Context );
            Journal_ExpertUpLimitedBattlestations_AnyPlayer( aPlayerFaction, Context );
        }

        #region Journal_CombatAndPlanets_AnyPlayer
        private static readonly Dictionary<Planet, int> strengthOfCenterpiecesPerPlanet = Dictionary<Planet, int>.Create_WillNeverBeGCed( 100, "HostOnlyJournalsAndAutosaveAndSimilarHandler-strengthOfCenterpiecesPerPlanet" );
        public void Journal_CombatAndPlanets_AnyPlayer( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            JournalEntry guardsEntry = JournalEntryTable.Instance.GetRowByName( "Beginner_HandlingGuards" );
            JournalEntry outnumberedEntry = JournalEntryTable.Instance.GetRowByName( "Beginner_OutnumberedInAttack" );
            if ( guardsEntry.GetIsAlreadyLoggedInCurrentCampaign() && outnumberedEntry.GetIsAlreadyLoggedInCurrentCampaign() )
                return; //if we've already done these, don't do them anymore
            strengthOfCenterpiecesPerPlanet.Clear();
            foreach ( Fleet fleet in World_AIW2.Instance.Fleets( aPlayerFaction, FleetStatus.CenterpieceMustLiveOrLooseFleet ) )
            {
                if ( fleet == null )
                    continue;
                if ( fleet.IsFleetInTransportLoadMode )
                    continue;
                GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                if ( centerpiece == null )
                    continue;
                Planet planet = centerpiece.Planet;
                if ( planet == null )
                    continue;
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( aPlayerFaction );
                if ( pFaction == null )
                    continue;
                int enemyStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                int guardStrength = pFaction.DataByStance[FactionStance.Hostile].GuardStrength;
                if ( guardStrength < 10 * 1000 )
                    continue; //only for stronger planets, not immediately
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_HandlingGuards", string.Empty, aPlayerFaction, null, planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                strengthOfCenterpiecesPerPlanet[planet] += fleet.GetCurrentStrengthOfFleet_ForUIOnly( false );
            }
            foreach ( KeyValuePair<Planet, int> pair in strengthOfCenterpiecesPerPlanet )
            {
                PlanetFaction pFaction = pair.Key.GetPlanetFactionForFaction( aPlayerFaction );
                if ( pFaction == null )
                    continue;
                if ( pair.Value < 7 * 1000 ) //you need to be suitably strong; try not to play this immediately. Most players will find the first few planets easy on low difficulties
                    continue;
                int enemyStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                if ( enemyStrength > pair.Value * 3 )
                {
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_OutnumberedInAttack", string.Empty, aPlayerFaction, null, pair.Key, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    break;
                }
            }
        }
        #endregion end Journal_CombatAndPlanets_AnyPlayer

        #region Journal_HandleEntriesTriggeredByPlayerEncounter_AnyPlayer
        private static readonly List<SafeSquadWrapper> EntitiesTriggering = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "HostOnlyJournalsAndAutosaveAndSimilarHandler-EntitiesTriggering" );
        private int LastJournalTime = 0; //don't bother serializing this, since playing a journal at game load is okay
        public void Journal_HandleEntriesTriggeredByPlayerEncounter_AnyPlayer( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            //A number of entities will play journal entries when the player encounters them.
            //High priority entries play instantly (this is for things like an AI Eye)
            //Normal entries can be rate-limited by setting the Rate Limiter, in case players find this overwhelming

            EntitiesTriggering.Clear();
            
            // 
            int InternalJournalRateLimiterInterval = 5; //to disable the rate limiter, set this to 0
            
            foreach ( GameEntity_Squad e in World_AIW2.Instance.Squads( EntityRollupType.PlayJournalOnPlayerEncounter ) )
            {
                if (!e.GetShouldBeVisibleBasedOnPlanetIntel())
                    continue;

                JournalEntry entry = JournalEntryTable.Instance.GetRowByName( e.TypeData.JournalNameToPlayOnPlayerEncounter );
                if ( entry == null )
                    throw new Exception( "Could not find journal entry <" + e.TypeData.JournalNameToPlayOnPlayerEncounter + "> on " + e.ToStringWithPlanetAndOwner() );

                if (entry.IsBlockedFromTriggering)
                    continue;

                if ( !entry.CanRecordAnotherCopyIfAlreadyRecordedInThisCampaign &&
                     entry.GetIsAlreadyLoggedInCurrentCampaign() )
                {
                    continue;
                }

                if ( e.TypeData.JournalHighPriority )
                {
                    //Just play this one right now if possible
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( e.TypeData.JournalNameToPlayOnPlayerEncounter, string.Empty, aPlayerFaction, null, e.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );

                    continue;
                }

                // collect list of the ones not high priority, which we will play one of over time.
                EntitiesTriggering.Add( e );
            }
            
            if ( EntitiesTriggering.Count == 0 )
                return;
            
            if ( InternalJournalRateLimiterInterval > 0 &&
                 LastJournalTime > 0 &&
                 World_AIW2.Instance.GameSecond <= (LastJournalTime + InternalJournalRateLimiterInterval))
            {
                return;
            }
            
            for ( int i = 0; i < EntitiesTriggering.Count; i++ )
            {
                //we do it this way to allow us to stagger these journal entries if desired
                GameEntity_Squad entity = EntitiesTriggering[i].GetSquad();
                if ( entity == null )
                    continue;
                
                //ArcenDebugging.ArcenDebugLogSingleLine("triggering entry " + i + " of " + EntitiesTriggering.Count + " at " + World_AIW2.Instance.GameSecond + ": " + entity.TypeData.JournalNameToPlayOnPlayerEncounter, Verbosity.DoNotShow );
                bool didplay = World_AIW2.Instance.QueueLogJournalEntryToSidebar( entity.TypeData.JournalNameToPlayOnPlayerEncounter, string.Empty, aPlayerFaction, null, entity.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                if (!didplay)
                    continue;
                
                LastJournalTime = World_AIW2.Instance.GameSecond;
                
                //one at a time if we have the rate limiter on
                if ( InternalJournalRateLimiterInterval > 0 )
                    break; 
            }
        }
        #endregion

        #region Journal_TechUnlocks_AnyPlayer
        public void Journal_TechUnlocks_AnyPlayer( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            if ( !Helper_CanTryLogCentralJournal( "Beginner_TechUnlocks" ) )
                return;
            ProtectedList<TechUpgrade> upgrades = TechUpgradeTable.Instance.Rows;
            for ( int i = 0; i < upgrades.Count; i++ )
            {
                TechUpgrade upgrade = upgrades[i];
                int currentUpgradesHeld = aPlayerFaction.TechUnlocks[upgrade.RowIndexNonSim];
                if ( currentUpgradesHeld <= 0 )
                    continue;
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_TechUnlocks", string.Empty, aPlayerFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
        }
        #endregion

        #region Journal_UpgradeMetal_AnyPlayer
        public void Journal_UpgradeMetal_AnyPlayer( Faction aPlayerFaction, ArcenHostOnlySimContext Context, int planetsOwned )
        {
            if ( !Helper_CanTryLogCentralJournal( "Beginner_UpgradeMetal" ) )
                return;

            /* Suggest upgrading metal production if the player hasn't done so and the player is low on metal */
            if ( aPlayerFaction.StoredMetal.IntValue < 10000 )
            {
                bool needToUpgradeMetal = false;
                //now check if we've done any suitable metal income upgrading
                foreach ( Fleet fleet in World_AIW2.Instance.Fleets( aPlayerFaction, FleetStatus.CenterpieceMustLiveOrLooseFleet ) )
                {
                    if ( fleet == null || fleet.Centerpiece.GetSquad() == null )
                        continue;
                    if ( fleet.Centerpiece.GetSquad().TypeData.SpecialType != SpecialEntityType.HumanHomeCommand )
                        continue;
                    if ( fleet.AddedMarkLevelsForFleet_FromScience <= 1 )
                    {
                        needToUpgradeMetal = true;
                        break;
                    }
                }
                List<TechUpgrade> upgrades = TechUpgradeTable.Instance.SortedTechUpgrades;
                for ( int i = 0; i < upgrades.Count; i++ )
                {
                    TechUpgrade upgrade = upgrades[i];
                    if ( upgrade.InternalName != "MetalGeneration" )
                        continue;
                    int upgradesSoFar = aPlayerFaction.TechUnlocks[upgrade.RowIndexNonSim] + aPlayerFaction.FreeTechUnlocks[upgrade.RowIndexNonSim];
                    if ( upgradesSoFar <= 1 )
                    {
                        needToUpgradeMetal = true;
                    }
                }

                if ( needToUpgradeMetal )
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_UpgradeMetal", string.Empty, aPlayerFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
        }
        #endregion

        #region Journal_ExpertModeFuel_AnyPlayer
        public void Journal_ExpertModeFuel_AnyPlayer( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            if ( !World_AIW2.Instance.IsFuelEnabled || !Helper_CanTryLogCentralJournal( "ExpertMode_Fuel" ) )
                return;

            //if fuel is enabled, use the tip about that
            World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ExpertMode_Fuel", string.Empty, aPlayerFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        }
        #endregion

        #region Journal_ChallengerMode_AnyPlayer
        public void Journal_ChallengerMode_AnyPlayer( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            int harshness = World_AIW2.Instance.CampaignType.HarshnessRating;
            if ( harshness > 400 && harshness < 600 )
            { 
                //Challenger
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ChallengerMode_Intro", string.Empty, aPlayerFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
        }
        #endregion

        #region Journal_ExpertMode_AnyPlayer
        public void Journal_ExpertMode_AnyPlayer( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            int harshness = World_AIW2.Instance.CampaignType.HarshnessRating;
            if ( harshness > 900 && harshness < 1100 )
            { 
                //Expert
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ExpertMode_Intro", string.Empty, aPlayerFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
        }
        #endregion

        #region Journal_LogisticianMode_AnyPlayer
        public void Journal_LogisticianMode_AnyPlayer( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            int harshness = World_AIW2.Instance.CampaignType.HarshnessRating;
            if ( harshness > 1900 && harshness < 2100 )
            {
                //Logistician
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "LogisticianMode_Intro", string.Empty, aPlayerFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
        }
        #endregion

        #region Journal_DeathwishMode_AnyPlayer
        public void Journal_DeathwishMode_AnyPlayer( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            int harshness = World_AIW2.Instance.CampaignType.HarshnessRating;
            if ( harshness > 4500 && harshness < 6000 )
            {
                //Deatwish
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DeathwishMode_Intro", string.Empty, aPlayerFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
        }
        #endregion

        #region Journal_ExpertUpLimitedBattlestations_AnyPlayer
        public void Journal_ExpertUpLimitedBattlestations_AnyPlayer( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            int harshness = World_AIW2.Instance.CampaignType.HarshnessRating;
            if ( harshness > 900 )
            {
                //Expert or higher
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ExpertUpLimitedBattlestations_Intro", string.Empty, aPlayerFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
        }
        #endregion

        #region Journal_GetTSS_AnyPlayer
        public void Journal_GetTSS_AnyPlayer( Faction aPlayerFaction, ArcenHostOnlySimContext Context, int planetsOwnedByMePersonally )
        {
            if ( !Helper_CanTryLogCentralJournal( "Beginner_HackDSS" ) )
                return;
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction != null && NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( localFaction )) 
                return; //not for necromancers


            /* Suggest that the player should hack a DSS if they have not done so */
            if ( planetsOwnedByMePersonally >= 3 && aPlayerFaction.OnePlayer_AddedToCommandStationsAndBattlestations_Permanent.Count == 0 && World_AIW2.Instance.GameSecond % 190 == 0 )
            {
                //we own at least 3 planets and no DSS hack, so prompt the player to hack a DSS.
                //Note this also appears in the Intel Tab under "Beginner stuff", but no harm in putting it here too.
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_HackDSS", string.Empty, aPlayerFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
        }
        #endregion

        #region Journal_OfficerFleets_AnyPlayer
        public void Journal_OfficerFleets_AnyPlayer( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            int totalOfficers = 0;
            Planet citadelPlanet = null;
            int highestMarkOfficer = 0;
            foreach ( Fleet fleet in World_AIW2.Instance.Fleets( aPlayerFaction, FleetStatus.CenterpieceMustLiveOrLooseFleet ) )
            {
                GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                if ( centerpiece == null )
                    continue;
                if ( centerpiece.PlanetFaction.Faction != aPlayerFaction )
                    continue;

                if ( centerpiece.TypeData.SpecialType == SpecialEntityType.BattlestationCitadel )
                    citadelPlanet = centerpiece.Planet;
                if ( centerpiece.TypeData.SpecialType == SpecialEntityType.MobileOfficerCombatFleetFlagship )
                {
                    if ( centerpiece.CurrentMarkLevel > highestMarkOfficer )
                        highestMarkOfficer = centerpiece.CurrentMarkLevel;
                    totalOfficers++;
                }
            }
            if ( totalOfficers > 0 )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_FirstOfficer", string.Empty, aPlayerFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            //if ( highestMarkOfficer > X )
            //   play appropriate journal
            if ( citadelPlanet != null )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_Citadel", string.Empty, aPlayerFaction, null, citadelPlanet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        }
        #endregion

        #region Journal_BuildTurrets_AnyPlayer
        public void Journal_BuildTurrets_AnyPlayer( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            /* If the player hasn't been building turrets after a while, suggest they do so */
            int turretCountToFlag = 10;
            int timeToFlag = 1200;
            int turretsFound = 0;
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction != null && NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( localFaction )) 
                return; //not for necromancers
            foreach ( GameEntity_Squad entity in aPlayerFaction.Squads( EntityRollupType.ForReinforcementType_Turret ) )
            {
                turretsFound++;
                if ( turretsFound > turretCountToFlag )
                    break;
            }
            if ( turretsFound < turretCountToFlag && World_AIW2.Instance.GameSecond > timeToFlag )
            {
                if ( !NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( aPlayerFaction )  ) //the necromancer has their own journal entry
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_BuildTurrets", string.Empty, aPlayerFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
        }
        #endregion

        #region Journal_FlagshipBits_AnyPlayer
        public void Journal_FlagshipBits_AnyPlayer( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            /* If a player's flagship has been crippled 3 times and is level 1, or if a player isn't using Load on any flagships */
            bool foundFlagshipUnupgradedAndRepeatedlyCrippled = false;
            bool foundFlagshipWithLotsOfLoads = false;
            int timesCrippledToCount = 2;
            int numLoadsToCount = 3;
            int highestLevelFlagship = -1;
            foreach ( Fleet fleet in World_AIW2.Instance.Fleets( aPlayerFaction, FleetStatus.CenterpieceMustLive ) )
            {
                if ( fleet == null || fleet.Centerpiece.GetSquad() == null )
                    continue;
                switch ( fleet.Category )
                {
                    case FleetCategory.PlayerMobile:
                    case FleetCategory.PlayerCustomCityFedMobile:
                    case FleetCategory.PlayerCustomUnattachedMobile:
                        break;
                    default:
                        continue;
                }
                if ( fleet.AddedMarkLevelsForFleet_FromScience > highestLevelFlagship )
                    highestLevelFlagship = fleet.AddedMarkLevelsForFleet_FromScience;
                if ( fleet.TimesInLoadModes_UIOnly >= numLoadsToCount )
                    foundFlagshipWithLotsOfLoads = true;
                if ( fleet.TimesCrippled_UIOnly > timesCrippledToCount )
                {
                    foundFlagshipUnupgradedAndRepeatedlyCrippled = true;
                    break;
                }
            }

            if ( foundFlagshipUnupgradedAndRepeatedlyCrippled && highestLevelFlagship < 2 )
            {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_UpgradeFlagships", string.Empty, aPlayerFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
            int time = 1800;
            if ( !foundFlagshipWithLotsOfLoads && World_AIW2.Instance.GameSecond > time &&
                 World_AIW2.Instance.GameSecond % 570 == 0 )
            {
                //after 20 minutes, players should probably have used the load command 3 times
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_UseLoadMode", string.Empty, aPlayerFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
        }
        #endregion

        #region Journal_WarpGateNearKing_AnyPlayer
        public void Journal_WarpGateNearKing_AnyPlayer( Faction aPlayerFaction, ArcenHostOnlySimContext Context, int planetsOwnedByMePersonally )
        {
            /* If the player has captured 2 planets but still has warp gates adjacent to the player homeworld */
            if ( planetsOwnedByMePersonally > 2 )
            {
                GameEntity_Squad king = FactionUtilityMethods.Instance.findKing( aPlayerFaction );
                if ( king == null )
                {
                    //if ( World.Instance.ConclusionType != CampaignConclusionType.Lost )
                    //    throw new Exception( "No king found for " + aPlayerFaction.GetDisplayName() + " index " + aPlayerFaction.FactionIndex );
                    return; //the player is already dead
                }
                bool foundWarpGateAdjacentToKing = false;
                foreach ( Planet neighbor in king.Planet.LinkedNeighbors( false ) )
                {
                    Faction controllingFaction = neighbor.GetControllingFaction();
                    if ( controllingFaction.Type != FactionType.AI )
                        continue;
                    PlanetFaction pFaction = neighbor.GetControllingPlanetFaction();
                    foreach ( GameEntity_Squad entity in pFaction.Entities.Squads( EntityRollupType.WarpEntryPoints ) )
                    {
                        foundWarpGateAdjacentToKing = true;
                        break;
                    }
                    if ( foundWarpGateAdjacentToKing )
                        break;
                }
                if ( foundWarpGateAdjacentToKing && World_AIW2.Instance.GameSecond % 800 == 0 )
                {
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_WarpGateNearKing", string.Empty, aPlayerFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                }
            }
        }
        #endregion

        #region Journal_SpendScience_AnyPlayer
        public void Journal_SpendScience_AnyPlayer( Faction aPlayerFaction, ArcenHostOnlySimContext Context )
        {
            /* Suggest a player should spend science if they haven't spent much yet */
            int savedScience = 18000;
            int spentScience = 3000;
            if ( (aPlayerFaction.StoredScience >= savedScience &&
                  aPlayerFaction.GetTotalSpentScience() < spentScience) || //if we have a lot of stored science and haven't spent much
                 (aPlayerFaction.StoredScience >= 10000 && World_AIW2.Instance.GameSecond > 2100) ) //if we have a ton of science available after a good ways into the game
            {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Beginner_SpendScience", string.Empty, aPlayerFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
        }
        #endregion
    }
}
