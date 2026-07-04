using Arcen.Universal;
using System;

using System.Diagnostics;
using System.Threading;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// This allows for mods to generate client-or-host personal-style notifications that 
    /// work in multiplayer.
    /// </summary>
    public class CorePersonalNotificationGenerator : IExternalPersonalNotificationGenerator
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            //likely would never be anything to do here
        }

        public void GeneratePersonalNotificationOnClientOrHost_BackgroundThread( Faction focalFaction, ArcenSimContextAnyStatus Context )
        {
            int debugStage = 1;
            try
            {
                debugStage = 10000;
                #region Notes on Planets with Player Forces in Combat
                {
                    PlanetFaction localOrFirstPFaction;
                    int enemyStrength;
                    foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                    {
                        if ( World_AIW2.Instance.TutorialOrNull != null &&
                             World_AIW2.Instance.TutorialOrNull.SuppressNotifications )
                            continue; //if this is a tutorial set to not show notification, skip this

                        bool doHumansHaveADeadCommandStationHere = false;
                        bool doHumansHaveACrippledCityHere = false;
                        bool doPlayersLoseIfUnitLostHere = false;
                        bool isMeAttackingSomeoneElse = false;
                        bool isMeFightingOverNeutralWorld = false;
                        Faction controllingFaction = planet.GetControllingFaction();

                        int playerStrengthHere = 0;
                        int playerMobileStrengthHere = 0;
                        foreach ( Faction fac in World_AIW2.Instance.AllPlayerFactions )
                        {
                            PlanetFaction pfac = planet.GetPlanetFactionForFaction( fac );
                            
                            var data = pfac.DataByStance[FactionStance.Self];
                            playerStrengthHere += data.MobileStrength;
                            playerStrengthHere += data.TurretStrength;
                            
                            playerMobileStrengthHere += data.MobileStrength;
                        }
                            
                        foreach ( GameEntity_Squad e in planet.Squads( EntityRollupType.PlayerLosesIfAnyDie ) )
                        {
                            if ( e == null || e.PlanetFaction == null || e.GetFactionTypeSafe() != FactionType.Player )
                                continue;

                            doPlayersLoseIfUnitLostHere = true;
                        }
                        
                        if ( controllingFaction == null || controllingFaction.Type != FactionType.Player )
                        {
                            foreach ( GameEntity_Squad squad in planet.Squads( EntityRollupType.CommandStation ) )
                            {
                                if ( squad == null || squad.PlanetFaction == null || squad.GetFactionTypeSafe() != FactionType.Player )
                                    continue;
                                if ( squad.SecondsSpentAsRemains >= 0 )
                                {
                                    doHumansHaveADeadCommandStationHere = true;
                                    controllingFaction = squad.GetFactionOrNull_Safe();
                                    break;
                                }
                            }

                            foreach ( GameEntity_Squad squad in planet.Squads( EntityRollupType.CityCenter ) )
                            {
                                if (squad == null || squad.PlanetFaction == null || squad.GetFactionTypeSafe() != FactionType.Player) {
                                    continue;
                                }
                                if (squad.CrippledUntilReachesFullHealth) {
                                    doHumansHaveACrippledCityHere = true;
                                    if (!doHumansHaveADeadCommandStationHere) {
                                        controllingFaction = squad.GetFactionOrNull_Safe();
                                    }
                                    break;
                                }
                            }

                            // these other cases we show the note regardless of player strength
                            if ( !doHumansHaveADeadCommandStationHere && 
                                 !doHumansHaveACrippledCityHere && 
                                 !doPlayersLoseIfUnitLostHere )
                            {
                                // but dont show this case, insignificant player strength (or even none)
                                if ( playerStrengthHere < 5000 )
                                    continue;
                               
                                if ( controllingFaction == null || controllingFaction.Type == FactionType.NaturalObject )
                                    isMeFightingOverNeutralWorld = true;
                                else
                                    isMeAttackingSomeoneElse = true;
                            }
                        }

                        localOrFirstPFaction = planet.GetPlanetFactionForFaction( focalFaction );
                        enemyStrength = localOrFirstPFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                        
                        // if we have a dead command station or city here, let us know even if there are no enemies left
                        if ( !doHumansHaveADeadCommandStationHere || !doHumansHaveACrippledCityHere )
                        {
                            if ( enemyStrength <= 0 )
                                continue;
                        }

                        if ( isMeFightingOverNeutralWorld || isMeAttackingSomeoneElse )
                        {
                            if ( playerMobileStrengthHere == 0 )
                            {
                                //If we are on a non-player planet and we have only immobile units, don't bother with a notification
                                //This can happen with necromancer guard posts in particular
                                continue;
                            }

                            NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                            fillData.enemyStrength = enemyStrength;
                            fillData.myAndAlliedStrength = 
                                localOrFirstPFaction.DataByStance[FactionStance.Friendly].TotalStrength +
                                localOrFirstPFaction.DataByStance[FactionStance.Self].TotalStrength;
                            
                            fillData.Planet = planet;
                            fillData.alliedAttackerColorHex = "ffffff";
                            fillData.isNeutralWorld = isMeFightingOverNeutralWorld;
                            
                            fillData.controllingFaction = controllingFaction;
                            if ( controllingFaction != null )
                                fillData.enemyOwnerColorHex = controllingFaction.FactionCenterColor.ColorHexBrighter;
                            else
                                fillData.enemyOwnerColorHex = "ffffff";

                            if ( localOrFirstPFaction.DataByStance[FactionStance.Self].TotalStrength > 0 )
                                fillData.alliedAttackerColorHex = localOrFirstPFaction.Faction.FactionCenterColor.ColorHexBrighter;
                            else
                            {
                                PlanetFaction allliedFaction;
                                for ( int j = 0; j < planet.Factions.Count; j++ )
                                {
                                    allliedFaction = planet.Factions[j];
                                    if ( !allliedFaction.GetIsFriendlyTowards( localOrFirstPFaction ) )
                                        continue;
                                    if ( allliedFaction.DataByStance[FactionStance.Self].TotalStrength <= 0 )
                                        continue;
                                    
                                    fillData.alliedAttackerColorHex = allliedFaction.Faction.FactionCenterColor.ColorHexBrighter;
                                    
                                    break;
                                }
                            }

                            SortedNotificationPriorityLevel priority = SortedNotificationPriorityLevel.Informational;

                            NotificationNonSim notification = new NotificationNonSim();
                            notification.Assign( IAttackAPlanetNotifier.Instance, fillData, planet.Name, 0, "AttacksOnNeutralPlanets", priority );
                            
                            continue;
                        }
                        else
                        {
                            NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                            fillData.enemyStrength = enemyStrength;
                            fillData.myAndAlliedStrength = 
                                localOrFirstPFaction.DataByStance[FactionStance.Friendly].TotalStrength +
                                localOrFirstPFaction.DataByStance[FactionStance.Self].TotalStrength;
                            
                            fillData.Planet = planet;
                            fillData.attackerColorHex = "ffffff";
                            fillData.doHumansHaveADeadCommandStationHere = doHumansHaveADeadCommandStationHere;
                            fillData.doHumansHaveACrippledCityHere = doHumansHaveACrippledCityHere;
                            fillData.doPlayersLoseIfUnitLostHere = doPlayersLoseIfUnitLostHere;
                            if ( controllingFaction != null )
                                fillData.ownerColorHex = controllingFaction.FactionCenterColor.ColorHexBrighter;
                            else
                                fillData.ownerColorHex = "ffffff";

                            PlanetFaction enemyFaction;
                            for ( int j = 0; j < planet.Factions.Count; j++ )
                            {
                                enemyFaction = planet.Factions[j];
                                if ( !enemyFaction.GetIsHostileTowards( localOrFirstPFaction ) )
                                    continue;
                                if ( enemyFaction.DataByStance[FactionStance.Self].TotalStrength <= 0 )
                                    continue;
                                fillData.attackerColorHex = enemyFaction.Faction.FactionCenterColor.ColorHexBrighter;
                                break;
                            }

                            SortedNotificationPriorityLevel priority = SortedNotificationPriorityLevel.Medium;
                            if ( doPlayersLoseIfUnitLostHere )
                            {
                                if ( fillData.enemyStrength > fillData.myAndAlliedStrength )
                                    priority = SortedNotificationPriorityLevel.OMG;
                                else if ( fillData.enemyStrength > fillData.myAndAlliedStrength / 2 )
                                    priority = SortedNotificationPriorityLevel.Major;
                                else if ( fillData.enemyStrength < fillData.myAndAlliedStrength * 10 )
                                    priority = SortedNotificationPriorityLevel.Minor;
                            }
                            else
                            {
                                if ( fillData.enemyStrength > fillData.myAndAlliedStrength + fillData.myAndAlliedStrength )
                                    priority = SortedNotificationPriorityLevel.Major;
                                else if ( fillData.enemyStrength < fillData.myAndAlliedStrength / 2 )
                                    priority = SortedNotificationPriorityLevel.Minor;
                            }
                            
                            if ( (doHumansHaveADeadCommandStationHere || doHumansHaveACrippledCityHere) && priority < SortedNotificationPriorityLevel.Major )
                                priority = SortedNotificationPriorityLevel.Major;

                            NotificationNonSim notification = new NotificationNonSim();
                            notification.Assign( MyPlanetAttackNotifier.Instance, fillData, planet.Name, 0, "AttacksOnMyPlanets", priority );
                            continue;
                        }
                    }
                }
                #endregion

                debugStage = 80000;
                #region Fill NonSim Notifications List Relating To Dyson Antagonizers
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    if ( otherFaction.SpecialFactionData.InternalName != "AntagonizedDysonSphere" )
                        continue;
                    GameEntity_Squad antagonizer = null;

                    foreach ( GameEntity_Squad entity in otherFaction.Squads( "DysonAntagonizer", "WarpingInDysonAntagonizer" ) )
                    {
                        antagonizer = entity;
                    }
                    if ( antagonizer == null )
                        continue;
                    NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                    fillData.Faction = otherFaction;
                    fillData.Planet = antagonizer.Planet;
                    fillData.Entity = SafeSquadWrapper.Create( antagonizer );
                    NotificationNonSim notification = new NotificationNonSim();
                    notification.Assign( DysonAntagonizerNotifier.Instance, fillData, "", 0, "Dyson Antagonizers", SortedNotificationPriorityLevel.Major );
                }
                #endregion

                debugStage = 160000;
                #region Tell Us About Any Hackers That Are Hacking
                //this is a concurrent dictionary, so we must use a foreach
                //when we do a foreach, it gets a copy of the list at that time
                //so we can remove from it during the foreach without incident
                //this is the opposite of how a regular dictionary works, where
                //removals during foreach are catastrophic
                foreach ( KeyValuePair<GameEntity_Squad, int> kv in World_AIW2.Instance.CurrentHackers )
                {
                    if ( kv.Key.ActiveHack == null || //if we stopped hacking
                        kv.Key.HasBeenRemovedFromSim || //if we died and went back to the pool
                        kv.Key.PrimaryKeyID != kv.Value ) //if our PKID changed, which means we came OUT of the pool
                    {
                        //...then get rid of the reference to us, please!
                        World_AIW2.Instance.CurrentHackers.TryRemove( kv.Key, out int unused );
                    }
                    else
                    {
                        //otherwise, cool, tell us about this hacker then, man!
                        NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                        fillData.Entity = SafeSquadWrapper.Create( kv.Key );
                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( HackingNotifier.Instance, fillData, "", 0, "ongoing hacking events", SortedNotificationPriorityLevel.Hacking );
                    }
                }
                #endregion

                debugStage = 220000;
                #region Fill NonSim Notifications List Relating To Brownouts
                foreach ( Faction humanFac in World_AIW2.Instance.EmpireStylePlayerFactions )
                {
                    if ( humanFac != null && humanFac.SecondsSinceBrownout >= 0 )
                    {
                        NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                        fillData.Faction = humanFac;
                        fillData.IsLocalFaction = humanFac == focalFaction;
                        fillData.eventTimeRemaining = ExternalConstants.Instance.SecondsToWaitBeforeBrownoutEnds - humanFac.SecondsSinceBrownout;
                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( BrownoutNotifier.Instance, fillData, "", 0, "Brownout", SortedNotificationPriorityLevel.OMG );
                    }
                }
                #endregion

                debugStage = 230000;
                #region Fill NonSim Notifications List Relating To Unspent Module Points
                {
                    NotifierFillData fillData = null;
                    foreach ( Fleet fleet in World_AIW2.Instance.Fleets( focalFaction, FleetStatus.AnyStatus ) )
                    {
                        if ( fleet == null )
                            continue;
                        foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                        {
                            if ( mem == null )
                                continue;
                            GameEntityTypeData typeData = mem.TypeData;
                            if ( typeData == null )
                                continue;
                            GameEntityTypeData.MarkLevelStats forMark = mem.ForMark;
                            if ( forMark == null )
                                continue;
                            if ( typeData.IsModular )
                            {
                                if ( mem.EffectiveSquadCap <= 0 && mem.EntitiesOfFMem.Count == 0 )
                                    continue;

                                if ( mem.IgnoreModuleNotification() )
                                    continue;
                                int unspent = mem.FreeModulePoints();

                                if ( unspent > 0 )
                                {
                                    if ( fillData == null )
                                        fillData = NotifierFillData.GetFromPoolOrCreate();
                                    fillData.ObjectList.Add( mem );
                                }
                            }
                        }
                    }

                    if ( fillData != null )
                    {
                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( UnspentModulePointsNotifier.Instance, fillData, "", 0, "UnspentModulePoints", SortedNotificationPriorityLevel.Major );
                    }
                }
                #endregion

                debugStage = 110000;
                #region Fill NonSim Notifications List Relating To the Devourer
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    if ( otherFaction.SpecialFactionData.InternalName != "DevourerGolem" )
                        continue;
                    foreach ( GameEntity_Squad entity in otherFaction.Squads( "Devourer" ) )
                    {
                        if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() && World_AIW2.Instance.GameSecond > 1 )
                        {
                            NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                            Planet planet = entity.Planet;
                            fillData.Planet = planet;
                            fillData.Faction = otherFaction;
                            fillData.Entity = SafeSquadWrapper.Create(entity);

                            NotificationNonSim notification = new NotificationNonSim();

                            SortedNotificationPriorityLevel priority = SortedNotificationPriorityLevel.Informational;
                            if ( planet != null )
                            {
                                Faction ownerFaction = planet.GetControllingFaction();
                                if ( ownerFaction != null )
                                {
                                    if ( !ownerFaction.GetIsFriendlyToLocalFaction() )
                                        priority = SortedNotificationPriorityLevel.Informational;
                                    else if ( planet.PopulationType == PlanetPopulationType.HumanHomeworld )
                                        priority = SortedNotificationPriorityLevel.Minor;
                                }
                            }

                            notification.Assign( DevourerNotifier.Instance, fillData, "", 0, "Devourer", priority );
                        }
                    }
                }
                #endregion

                debugStage = 120000;
                #region Fill NonSim Notifications List Relating To the Zenith Trader
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    if ( otherFaction.SpecialFactionData.InternalName != "ZenithTrader" )
                        continue;
                    foreach ( GameEntity_Squad entity in otherFaction.Squads( "ZenithTrader" ) )
                    {
                        if ( entity.Planet.GetControllingFaction() == focalFaction )
                        {
                            NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                            fillData.Planet = entity.Planet;
                            fillData.Faction = otherFaction;
                            NotificationNonSim notification = new NotificationNonSim();
                            notification.Assign( ZenithTraderNotifier.Instance, fillData, "", 0, "Zenith Trader", SortedNotificationPriorityLevel.Informational );
                        }
                    }
                }
                #endregion

                debugStage = 121000;
                #region Fill NonSim Notifications List Relating To Planet Ceasefires
                foreach ( Planet plan in World_AIW2.Instance.Planets( false ) )
                {
                    if ( !plan.PlanetCurrentlyUnderCeasefire )
                        continue;

                    Faction controlling = plan.GetControllingFaction();
                    bool myselfOrAllyControls = (controlling != null && (controlling.Type == FactionType.Player || controlling == focalFaction));
                    bool iHaveShipsHere = false;
                    PlanetFaction pFaction = plan.GetPlanetFactionForFaction( focalFaction );
                    if ( pFaction != null )
                    {
                        EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> pFactionStanceData = pFaction.DataByStance;
                        if ( pFactionStanceData != null )
                        {
                            iHaveShipsHere = (pFactionStanceData[FactionStance.Self].TotalStrength > 0);
                        }
                    }

                    if ( !myselfOrAllyControls && !iHaveShipsHere )
                        continue;

                    NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                    fillData.Planet = plan;
                    fillData.iHaveShipsHere = iHaveShipsHere;
                    fillData.myselfOrAllyControls = myselfOrAllyControls;
                    NotificationNonSim notification = new NotificationNonSim();
                    notification.Assign( PlanetCeasefireNotifier.Instance, fillData, "", 0, "Ceasefire", SortedNotificationPriorityLevel.Informational );
                }
                #endregion

                debugStage = 60000;
                #region Fill NonSim Notifications List Relating to Alerted Eyes
                for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.AIFactions[i];
                    GameEntity_Squad eye = null;

                    foreach ( GameEntity_Squad entity in otherFaction.Squads( "AlertedEye" ) )
                    {
                        if ( entity.Planet.IntelLevel <= PlanetIntelLevel.CurrentlyWatched )
                            continue;
                        eye = entity;
                        if ( World_AIW2.Instance.GameSecond % 5 == 0 )
                        {
                            //Make sure this gets sync'd regularly to clients, sometimes these can fall behind
                            eye.FlagForForcedFullSyncToClients_FromHost();
                        }
                    }
                    
                    if ( eye == null )
                        continue;
                    
                    NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                    fillData.Faction = otherFaction;
                    fillData.Planet = eye.Planet;
                    fillData.Entity = SafeSquadWrapper.Create(eye);
                    
                    NotificationNonSim notification = new NotificationNonSim();

                    notification.Assign( EyeNotifier.Instance, fillData, "", 0, "Alerted Eyes", SortedNotificationPriorityLevel.Minor );
                }
                #endregion

                debugStage = 70000;

                #region Fill NonSim Notifications List Relating to Raid Engines
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    debugStage = 71000;
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    debugStage = 72000;
                    if ( otherFaction.Type != FactionType.AI && otherFaction.GetParentFactionOrNull()?.Type != FactionType.AI )
                        continue;//only AI factions and their sub-factions can do periodic spawning
                    foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.NeedsSpecialPerSecondLogic ) )
                    {
                        debugStage = 74000;
                        if ( !entity.TypeData.HasPeriodicSpawn )
                            continue;

                        debugStage = 75000;
                        if ( entity.TypeData.PeriodicSpawn_MaxHopsToTrigger <= 0 && (entity.Planet.SentinelsAlertLevel == null || entity.Planet.SentinelsAlertLevel.Ordinal < 4) ) //must be on alert level 4 or more, or it must be able to trigger for more distant enemies
                            continue;
                        debugStage = 76000;
                        short hops = (short) entity.TypeData.PeriodicSpawn_MaxHopsToTrigger;
                        int minStrength = entity.TypeData.PeriodicSpawn_MinHostileStrengthToTrigger;
                        if ( entity.HasStartedDoingPeriodicSpawns && entity.TypeData.PeriodicSpawn_NeverStopOnceTriggered )
                        {
                            hops = short.MaxValue;
                            minStrength = 0;
                        }
                        debugStage = 76500;
                        PlanetFaction bestFaction = entity.PlanetFaction.GetMostAnnoyingPlanetFaction( hops, minStrength,
                            entity.TypeData.PeriodicSpawn_OnlyTriggerOnOccupation, entity.TypeData.PeriodicSpawn_OnlyTriggerAgainstPlayer );
                        debugStage = 78000;
                        if ( bestFaction == null || bestFaction.Planet == null || bestFaction.Faction == null )
                            continue;
                        /* Already checked in GetIndexOfMostAnnoyingFaction
                        if ( !targetFaction.SpecialFactionData.AICanSendWavesAgainstThis )
                            continue;
                        */
                        debugStage = 79000;
                        NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                        fillData.targetFaction = bestFaction.Faction;
                        fillData.Faction = otherFaction;
                        fillData.Planet = entity.Planet;
                        fillData.planetIdx = (short) bestFaction.Planet.Index;
                        fillData.Entity = SafeSquadWrapper.Create( entity );

                        int notificationLevel = 0;
                        FInt timeFraction;
                        if ( entity.HasStartedDoingPeriodicSpawns )
                            timeFraction = ((FInt) entity.PeriodicSpawn_TimeUntilNextSpawn) / entity.TypeData.PeriodicSpawn_DelayBetweenSpawns;
                        else
                            timeFraction = ((FInt) entity.PeriodicSpawn_TimeUntilNextSpawn) / entity.TypeData.PeriodicSpawn_InitialDelay;
                        if ( timeFraction < FInt.FromParts( 0, 50 ) || entity.PeriodicSpawn_TimeUntilNextSpawn <= 10 )
                            notificationLevel += 2;
                        else if ( timeFraction < FInt.FromParts( 0, 200 ) || entity.PeriodicSpawn_TimeUntilNextSpawn <= 60 )
                            notificationLevel++;
                        if ( bestFaction.Faction.GetIsLocalFaction() )
                            notificationLevel += 2;
                        else
                        {
                            Faction localPlayer = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                            if ( localPlayer != null && localPlayer.GetIsFriendlyTowards( bestFaction.Faction ) )
                                notificationLevel++;
                        }
                        int maxNotificationLevel = (int) SortedNotificationPriorityLevel.Informational;
                        SortedNotificationPriorityLevel priority = (SortedNotificationPriorityLevel) (maxNotificationLevel - Math.Min( maxNotificationLevel, notificationLevel ));

                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( RaidEngineNotifier.Instance, fillData, "", 0, "Raid Engine", priority );
                    }
                }
                #endregion

                debugStage = 90000;
                #region Fill NonSim Notifications List Relating To Counterattacks charging
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                        continue;
                    
                    if ( planet.PrecalculatedAICounterattackForcesStrength <= 0 || 
                         planet.AICounterAttacksNotSufficientToTryToSend)
                    {
                        continue;
                    }
                    
                    NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                    fillData.Planet = planet;
                    
                    NotificationNonSim notification = new NotificationNonSim();

                    SortedNotificationPriorityLevel priority = SortedNotificationPriorityLevel.Medium;

                    if ( planet.AICounterAttacksCurrentlyStalled )
                        priority = SortedNotificationPriorityLevel.Minor;
                    else if ( focalFaction != null )
                    {
                        int secondsRemaining = planet.AICountdownTimerForCounterattack;
                        int strengthOfCounterAttack = planet.PrecalculatedAICounterattackForcesStrength;
                        int strengthOfFriends = planet.GetStrengthOfFactions_FriendlyTo( focalFaction, true );
                        int strengthOfEnemies = planet.GetStrengthOfFactions_HostileTo( focalFaction ) + strengthOfCounterAttack;

                        if ( strengthOfFriends > strengthOfEnemies )
                            priority = SortedNotificationPriorityLevel.Minor;
                        else if ( strengthOfFriends + strengthOfFriends < strengthOfEnemies )
                            priority = SortedNotificationPriorityLevel.Major;
                    }

                    notification.Assign( CounterattackNotifier.Instance, fillData, "", 0, "Counterattack", priority );
                }
                #endregion

                debugStage = 180000;
                #region Fill NonSim Notifications List Relating to Macrophages (specifically enraged ones attacking you)
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    debugStage = 180010;
                    
                    Faction macros = World_AIW2.Instance.Factions[i];
                    switch (macros.SpecialFactionData.InternalName )
                    {
                        case "MacrophageInfestation":
                        case "TamedMacrophage":
                        case "EnragedMacrophage":
                            break; //this one is okay, check it
                        
                        default:
                            continue; //skip anything else
                    }
                    
                    if ( macros.GetIsFriendlyTowards( focalFaction ) )
                        continue;
                    
                    NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                    fillData.Faction = macros;
                    
                    foreach ( GameEntity_Squad e in macros.Squads( MacrophageFactionBaseInfo.EnragedHarvesterTag ) )
                    {
                        debugStage = 180020;
                        foreach ( Faction humanEmpire in World_AIW2.Instance.EmpireStylePlayerFactions )
                        {
                            if ( humanEmpire == null )
                                continue;

                            GameEntity_Squad king = humanEmpire.GetFactionKing();
                            if ( king == null )
                                continue;

                            debugStage = 180030;
                            if ( e.Planet.GetHopsTo( king.Planet ) < 3 )
                            {
                                fillData.EntityList.Add(e);
                                fillData.numEntitiesAttacking++;

                                break;
                            }
                        }
                    }
                    
                    
                    if ( fillData.EntityList.Count > 0 )
                    {
                        NotificationNonSim notification = new NotificationNonSim();
                        notification.Assign( MacrophageNotifier.Instance, fillData, "", 0, "Macrophages (specifically enraged ones attacking you)",
                            fillData.EntityList.Count > 4 ? SortedNotificationPriorityLevel.Medium : SortedNotificationPriorityLevel.Minor );
                    }
                    else
                    {
                        fillData.ReturnToPool();
                    }
                }
                #endregion

                debugStage = 240000;

                #region Fill NonSim Notifications List Relating to Nomad Planets
                NotifierFillData nomadPlanetfillData = null;
                int timeForNomadPlanetMoveNotifiction = 300;
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    debugStage = 240010;
                    if ( planet == null )
                        continue;
                    if ( planet.TypeData.Type != PlanetType.Nomad || planet.IsDisabledNomad )
                        continue;
                    debugStage = 240020;
                    if ( planet.NomadTargetPlanetIdx != -1 )
                    {
                        //we are a crashing nomad, handle us elsewhere
                        continue;
                    }
                    debugStage = 240030;
                    if ( planet.TimeForNextMove - timeForNomadPlanetMoveNotifiction <= World_AIW2.Instance.GameSecond )
                    {
                        debugStage = 240040;
                        if ( nomadPlanetfillData == null )
                            nomadPlanetfillData = NotifierFillData.GetFromPoolOrCreate();
                        nomadPlanetfillData.PlanetList.Add( planet );
                    }
                }
                debugStage = 240050;
                if ( nomadPlanetfillData != null )
                {
                    debugStage = 240060;
                    //sort from soonest to move to latest to move
                    debugStage = 240070;
                    nomadPlanetfillData.PlanetList.Sort( static delegate ( Planet Left, Planet Right )
                    {
                        return Left.TimeForNextMove.CompareTo( Right.TimeForNextMove );
                    } );
                    NotificationNonSim notification = new NotificationNonSim();
                    notification.Assign( NomadPlanetMoveNotifier.Instance, nomadPlanetfillData, "", 0, "Nomad Planet Movement", SortedNotificationPriorityLevel.Informational );
                }
                #endregion

                debugStage = 280000;

                #region Fill NonSim Notifications List Relating To the Chromatic Horror
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    if ( otherFaction.SpecialFactionData.InternalName != "ChromaticHorror" )
                        continue;
                    foreach ( GameEntity_Squad entity in otherFaction.Squads( "ChromaticHorror" ) )
                    {
                        if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() && World_AIW2.Instance.GameSecond > 1 )
                        {
                            NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                            Planet planet = entity.Planet;
                            fillData.Planet = planet;
                            fillData.Faction = otherFaction;
                            fillData.Entity = SafeSquadWrapper.Create(entity);
                            NotificationNonSim notification = new NotificationNonSim();

                            SortedNotificationPriorityLevel priority = SortedNotificationPriorityLevel.Informational;
                            if ( planet != null )
                            {
                                Faction ownerFaction = planet.GetControllingFaction();
                                if ( ownerFaction != null )
                                {
                                    if ( planet.PopulationType == PlanetPopulationType.HumanHomeworld )
                                        priority = SortedNotificationPriorityLevel.Minor;
                                }
                            }

                            notification.Assign( ChromaticHorrorNotifier.Instance, fillData, "", 0, "ChromaticHorror", priority );
                        }
                    }
                }
                #endregion
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "CorePersonalNotificationGenerator.GeneratePersonalNotificationOnClientOrHost_BackgroundThread Error at debug stage " + 
                    debugStage + ":\n" + e, Verbosity.ShowAsError );
            }
        }
    }
}
