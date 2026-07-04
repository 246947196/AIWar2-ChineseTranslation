using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class GlobalGeneralDeepInfoCommandHandler : ExternalWorldDeepInfo
    {
        public static GlobalGeneralDeepInfoCommandHandler Instance;
        public GlobalGeneralDeepInfoCommandHandler()
        {
            Instance = this;
        }

        public override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public override string GetIdentifierForErrorMessages()
        {
            return "GlobalGeneralDeepInfoCommandHandler";
        }

        public override bool GetShouldIBeInUse()
        {
            return true; //always in use!
        }

        #region SerializeTo
        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            
        }
        #endregion

        #region DeserializeIntoSelf
        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType ) 
        {
            
        }
        #endregion
        
        #region ShowdownDevices
        public void HandleShowdownDevices( ArcenHostOnlySimContext Context )
        {
            int debugCode = 0;
            bool debug = false;
            if ( World.Instance.ConclusionType != CampaignConclusionType.NotConcluded )
                return; //the game is over
            try{
                debugCode = 100;
                if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "EnableShutdownDevices" ) ) // yes, its actually called 'shutdown' in the xml, bizarre but true
                {
                    debugCode = 200;
                    if (!GlobalAIWorldBaseInfo.Instance.HasSpawnedDevices)
                    {
                        debugCode = 300;
                        //Spawn devices!
                        GlobalAIWorldBaseInfo.Instance.HasSpawnedDevices = true;
                        int devices = GlobalAIWorldBaseInfo.Instance.DevicesToSpawn;
                        bool seedNearPlayer = false;

                        if (!seedNearPlayer)
                        {
                            IList<Planet> planetsSeeded = StandardMapPopulator.Mapgen_SeedSpecialEntities(Context, World_AIW2.Instance.CurrentGalaxy, FactionType.NaturalObject, SpecialEntityType.None, "ShowdownDevice", SeedingType.HardcodedCount, devices,
                                                                                                           MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 7, 4, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal);
                            int retries = 10;
                            int seededSoFar = planetsSeeded.Count;
                            while (seededSoFar < devices && retries-- > 0)
                            {
                                planetsSeeded = StandardMapPopulator.Mapgen_SeedSpecialEntities(Context, World_AIW2.Instance.CurrentGalaxy, FactionType.NaturalObject, SpecialEntityType.None, "ShowdownDevice", SeedingType.HardcodedCount, devices - seededSoFar,
                                                                                                 MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 4, 2, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal);
                                seededSoFar += planetsSeeded.Count;
                                //Puffin reported too few showdown devices being seeded in CF
                            }
                        }
                        else
                        {
                            //seed the showdown devices adjacent to the player homeworld for testing
                            Planet playerPlanet = null;
                            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                            {
                                if (entity.GetFactionTypeSafe() == FactionType.Player)
                                {
                                    playerPlanet = entity.Planet;
                                    break;
                                }
                            }

                            int spawned = 0;
                            foreach ( Planet neighbor in playerPlanet.LinkedNeighbors( false ) )
                            {
                                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "ShowdownDevice");
                                neighbor.Mapgen_SeedEntity(Context, World_AIW2.Instance.GetNeutralFaction(), entityData, PlanetSeedingZone.MostAnywhere);
                                spawned++;
                            }
                            GlobalAIWorldBaseInfo.Instance.DevicesToSpawn = spawned;
                        }
                        return;
                    }
                    debugCode = 400;
                    if (GlobalAIWorldBaseInfo.Instance.CrisisTriggered)
                    {
                        debugCode = 500;
                        
                        if (debug && World_AIW2.Instance.GameSecond % 10 == 0)
                            ArcenDebugging.ArcenDebugLogSingleLine("Crisis has happened", Verbosity.DoNotShow);
                        //I don't want to try flushing all the reinforcement points and donating all the ships in one sim frame since that's going to
                        //tank performance in the middle of an endgame crisis. Instead we just do a few ships/guard posts at a time
                        //If it turns out we don't need to rate-limit then we just remove those checks
                        GameCommand transferCommand = null;
                        int ShipsLeftToTransferThisInterval = 140;
                        debugCode = 510;
                        for (int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++)
                        {
                            Faction faction = World_AIW2.Instance.AIFactions[i];
                            if (faction.FactionIsDefeated)
                                continue;
                            debugCode = 520;
                            AISentinelsFactionBaseInfo sentinelsBaseInfo = faction.GetAISentinelsCoreData();

                            foreach ( GameEntity_Squad entity in faction.Squads( EntityRollupType.MobileCombatants ) )
                            {
                                debugCode = 530;
                                if (entity.TypeData.IsKingUnit)
                                    continue;
                                if (transferCommand == null)
                                {
                                    transferCommand = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransferEntitiesToFaction], GameCommandSource.AnythingElse);
                                    transferCommand.RelatedFactionIndex = sentinelsBaseInfo.SubFac_RelentlessWave.FactionIndex;
                                }
                                transferCommand.RelatedEntityIDs.Add(entity.PrimaryKeyID);
                                if (ShipsLeftToTransferThisInterval-- <= 0)
                                    break;
                            }
                            debugCode = 600;
                            debugCode = 700;
                            if (transferCommand != null)
                            {
                                if (debug)
                                    ArcenDebugging.ArcenDebugLogSingleLine("Donating some ships!", Verbosity.DoNotShow);
                                debugCode = 710;
                                World_AIW2.Instance.QueueGameCommand(faction, transferCommand, false);
                                transferCommand = null;
                            }
                            debugCode = 800;

                            //Flush all units from reinforcement points
                            int maxToFlush = 10;
                            foreach ( GameEntity_Squad entity in faction.Squads( EntityRollupType.ReinforcementLocations ) )
                            {
                                debugCode = 810;
                                if ( FactionUtilityMethods.Instance.TryDeployReinforcementContents( entity, Context ) )
                                    if ( maxToFlush-- <= 0 )
                                        break;
                            }

                            //If we've transferred all the regular ships, also start transferring warden ships into hunter.
                            //I was looking at a Showdown game and realized that the Warden Fleet just ignoring the climactic battle felt really weird
                            if ( ShipsLeftToTransferThisInterval > 0 && sentinelsBaseInfo.SubFac_Hunter != null )
                            {
                                debugCode = 910;
                                
                                foreach ( GameEntity_Squad entity in sentinelsBaseInfo.SubFac_Warden.Squads( EntityRollupType.MobileCombatants ) )
                                {
                                    debugCode = 920;
                                    if (entity.TypeData.IsKingUnit)
                                        continue;
                                    if (transferCommand == null)
                                    {
                                        transferCommand = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransferEntitiesToFaction], GameCommandSource.AnythingElse);
                                        transferCommand.RelatedFactionIndex = sentinelsBaseInfo.SubFac_Hunter.FactionIndex;
                                    }
                                    transferCommand.RelatedEntityIDs.Add(entity.PrimaryKeyID);
                                    if (ShipsLeftToTransferThisInterval-- <= 0)
                                        break;
                                }

                                if (transferCommand != null)
                                {
                                    if (debug)
                                        ArcenDebugging.ArcenDebugLogSingleLine("Donating some ships!", Verbosity.DoNotShow);
                                    debugCode = 930;
                                    World_AIW2.Instance.QueueGameCommand(faction, transferCommand, false);
                                    transferCommand = null;
                                }
                            }
                        }
                        return;
                    }
                    if (!GlobalAIWorldBaseInfo.Instance.CrisisCountdownTriggered)
                        return; //the player hasn't started the crisis
                    if (GlobalAIWorldBaseInfo.Instance.CrisisFailed)
                        return; //the player has failed to trigger the crisis (too many 
                    int playerOwnedDevices = 0;
                    debugCode = 600;
                    foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "ShowdownDevice" ) )
                    {
                        if (entity.PlanetFaction.Faction.Type == FactionType.Player)
                            playerOwnedDevices++;
                    }
                    if (playerOwnedDevices < (GlobalAIWorldBaseInfo.Instance.DevicesToSpawn - 1) &&
                         !GlobalAIWorldBaseInfo.Instance.CrisisTriggered)
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine("You've lost too many showdown devices", Verbosity.DoNotShow);
                        GlobalAIWorldBaseInfo.Instance.SecondsUntilCrisis = -1;
                        GlobalAIWorldBaseInfo.Instance.CrisisFailed = true;
                    }
                    if (debug && World_AIW2.Instance.GameSecond % 10 == 0)
                        ArcenDebugging.ArcenDebugLogSingleLine("There are " + playerOwnedDevices + " player owned devices in the galaxy. Seconds till effect: " + GlobalAIWorldBaseInfo.Instance.SecondsUntilCrisis + " and Triggered? " + GlobalAIWorldBaseInfo.Instance.CrisisTriggered, Verbosity.DoNotShow);

                    int ExoInterval = 90;
                    int WormholeInterval = 120;
                    int FinalEffectTime = 1200;
                    if (!GlobalAIWorldBaseInfo.Instance.CrisisTriggered &&
                         GlobalAIWorldBaseInfo.Instance.CrisisCountdownTriggered &&
                         GlobalAIWorldBaseInfo.Instance.SecondsUntilCrisis == -1)
                    {
                        //The countdown for the crisis has begun!
                        if (debug)
                            ArcenDebugging.ArcenDebugLogSingleLine("Final Effect Countdown begun", Verbosity.DoNotShow);
                        GlobalAIWorldBaseInfo.Instance.SecondsUntilCrisis = FinalEffectTime;
                        Faction aiFaction = World_AIW2.GetRandomAIFaction(Context);
                        AISentinelsFactionBaseInfo BaseInfo = aiFaction.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
                        int waveBudget = BaseInfo.GetSpecificBudgetThreshold(AIBudgetType.Wave, GlobalAIWorldBaseInfo.Instance.AIProgress_Effective);
                        GlobalAIWorldBaseInfo.Instance.StrengthForNextExo = waveBudget * 2;
                        GlobalAIWorldBaseInfo.Instance.StrengthForNextWormholeInvasion = waveBudget * 2;
                        GlobalAIWorldBaseInfo.Instance.TimeForNextExo = World_AIW2.Instance.GameSecond + ExoInterval;
                        GlobalAIWorldBaseInfo.Instance.TimeForNextWormholeInvasion = World_AIW2.Instance.GameSecond + WormholeInterval;
                    }
                    if (GlobalAIWorldBaseInfo.Instance.SecondsUntilCrisis > 0)
                    {
                        if (GlobalAIWorldBaseInfo.Instance.TimeForNextWormholeInvasion <= World_AIW2.Instance.GameSecond)
                        {
                            //launch wormhole invasion
                            if (debug)
                                ArcenDebugging.ArcenDebugLogSingleLine("launch wormhole invasion", Verbosity.DoNotShow);

                            GlobalAIWorldBaseInfo.Instance.TimeForNextWormholeInvasion = WormholeInterval + World_AIW2.Instance.GameSecond;
                            Faction aiFaction = World_AIW2.GetRandomAIFaction(Context);
                            AISentinelsFactionBaseInfo BaseInfo = aiFaction.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
                            int waveBudget = BaseInfo.GetSpecificBudgetThreshold(AIBudgetType.Wave, GlobalAIWorldBaseInfo.Instance.AIProgress_Effective);
                            waveBudget *= Context.RandomToUse.Next(1, 3);

                            WormholeInvasionOptions options = WormholeInvasionOptions.CreateWithDefaults(waveBudget, aiFaction);
                            options.WaveCount = 1;
                            options.WaveInterval = 30;
                            options.ProjectorAppearanceTime = World_AIW2.Instance.GameSecond + 20;
                            options.PlanetLinkTime = World_AIW2.Instance.GameSecond + 30;
                            options.ForceInvasionLaunch = true;
                            bool wasLaunched = WormholeInvasionManager.LaunchWormholeInvasion(options, Context);
                        }
                        if (GlobalAIWorldBaseInfo.Instance.TimeForNextExo <= World_AIW2.Instance.GameSecond)
                        {
                            //launch exo
                            if (debug)
                                ArcenDebugging.ArcenDebugLogSingleLine("launch exo", Verbosity.DoNotShow);

                            GlobalAIWorldBaseInfo.Instance.TimeForNextExo = ExoInterval + World_AIW2.Instance.GameSecond;
                            Faction aiFaction = World_AIW2.GetRandomAIFaction(Context);
                            AISentinelsFactionBaseInfo BaseInfo = aiFaction.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
                            int waveBudget = BaseInfo.GetSpecificBudgetThreshold(AIBudgetType.Wave, GlobalAIWorldBaseInfo.Instance.AIProgress_Effective);
                            waveBudget *= Context.RandomToUse.Next(1, 3);
                            //The exo will either target player kings or the showdown devices, to force lots of defense
                            List<SafeSquadWrapper> workingTargets = List<SafeSquadWrapper>.Create_WillNeverBeGCed(10, "ShutdownDevicesBaseInfo-workingTargets");
                            if (Context.RandomToUse.Next(0, 100) < 50)
                                FactionUtilityMethods.Instance.findAllHumanKings(workingTargets);
                            else
                            {
                                foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "ShowdownDevice" ) )
                                {
                                    if (entity.PlanetFaction.Faction.Type == FactionType.Player)
                                        workingTargets.Add(entity);
                                }
                            }

                            ExoOptions options = ExoOptions.CreateWithDefaults(workingTargets, waveBudget, aiFaction, aiFaction);
                            if (GlobalAIWorldBaseInfo.Instance.SecondsUntilCrisis < 600)
                                options.newExoLeaderTag = "ExtragalacticWar";
                            ExoGalacticAttackManager.SendExoGalacticAttack(options, Context);
                        }
                        GlobalAIWorldBaseInfo.Instance.SecondsUntilCrisis--;
                    }
                    if (GlobalAIWorldBaseInfo.Instance.SecondsUntilCrisis == 0)
                    {
                        //Do the necessary here. I haven't decided if GlobalAIWorldBaseInfo.Instance should make you win the game outright,
                        //or trigger the Overlords to be turned into Mark 2/Dire Guard Posts destroyed
                        //Possibly GlobalAIWorldBaseInfo.Instance should also trigger all AI ships to join their relentless wave faction as well
                        GlobalAIWorldBaseInfo.Instance.CrisisTriggered = true;
                        GlobalAIWorldBaseInfo.Instance.SecondsUntilCrisis--;
                        if (debug)
                            ArcenDebugging.ArcenDebugLogSingleLine("Trigger the crisis", Verbosity.DoNotShow);
                        //first destroy all dire guard posts
                        foreach ( GameEntity_Squad otherEntity in World_AIW2.Instance.Squads( SpecialEntityType.DireGuardPost ) )
                        {
                            otherEntity.Die(Context, false, null);
                        }
                        foreach ( GameEntity_Squad otherEntity in World_AIW2.Instance.Squads( EntityRollupType.WarpEntryPoints ) )
                        {
                            if (otherEntity.TypeData.GetHasTag("WarpGate"))
                                otherEntity.Die(Context, false, null);
                        }
                        foreach ( GameEntity_Squad otherEntity in World_AIW2.Instance.Squads( EntityRollupType.ReinforcementLocations ) )
                            FactionUtilityMethods.Instance.TryDeployReinforcementContents( otherEntity, Context );

                        foreach ( GameEntity_Squad king in World_AIW2.Instance.Squads( "AIOverlordPhase1_AnyType" ) )
                        {
                            World_AIW2.Instance.QueueLogJournalEntryToSidebar("NA_ShowdownDevices_OverlordTransformation", string.Empty, king.GetFactionOrNull_Safe(), null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients);
                            Engine_AIW2.Instance.PresentationLayer.PlaySoundByType(SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.AIOverlordTransforms);
                            GameEntityTypeData tag = GetTypeDataForGCS(king);
                            GameEntity_Squad newOverlord = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(king.PlanetFaction, tag,
                                                                                                   king.CurrentMarkLevel,
                                                                                                   king.PlanetFaction.Faction.LooseFleet, 0,
                                                                                                   king.WorldLocation, Context, "AIShowdown-OverlordTransform");
                            king.Die(Context, false, null);
                        }
                    }
                }
            }catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in HandleShowdownDevices debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        private GameEntityTypeData GetTypeDataForGCS( GameEntity_Squad king )
        {
            AISentinelsFactionBaseInfo BaseInfo = king.PlanetFaction.Faction.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
            int difficulty = BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
            string name = "GalacticControlShip_Diff" + difficulty;
            GameEntityTypeData output = GameEntityTypeDataTable.Instance.GetRowByName( name );
            if ( output == null )
                ArcenDebugging.ArcenDebugLogSingleLine("Could not find gameEntityType with name " + name, Verbosity.DoNotShow );
            return output;
        }
        #endregion

        protected override void DoPerSimStepLogic_OnMainThread_NonSim__HostOnly( ArcenHostOnlySimContext Context )
        {
            //nothing to do!
        }
        protected override void DoPerSecondLogic_OnMainThread_NonSim__HostOnly( ArcenHostOnlySimContext Context )
        {
            Faction localPlayerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localPlayerFaction != null ) //for spectator mode
            {
                foreach ( Planet planet in World_AIW2.Instance.CurrentGalaxy.Planets( false ) )
                {
                      PlanetFaction pFaction = planet.GetPlanetFactionForFaction( localPlayerFaction );
                      if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength == 0 )
                      {
                          planet.FriendlyMetalLost = 0;
                          planet.HostileMetalLost = 0;
                          planet.NecromancerScienceEarned = 0;
                          planet.NecromancerHackingEarned = 0;
                          planet.NecromancerEssenceEarned = 0;
                      }
                }
            }
            HandleShowdownDevices( Context );
        }

        protected override void DoOnPlayerScrapped( GameEntity_Squad entity, Faction entityOwningFaction, ArcenHostOnlySimContext Context )
        {
            if ( entity.TypeData.SpecialType != SpecialEntityType.NormalHumanCommandStation )
                return;
            
            if ( entity.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength <= 1000 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Skipping SharkB because no enemy strength on planet when scrapped.", Verbosity.DoNotShow );
                return;
            }

            if ( entity.SelfBuildingMetalRemaining > 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Skipping SharkB because was still under construction when scrapped.", Verbosity.DoNotShow );
                return;
            }

            if ( entity.GetHasBeenDestroyed() )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Skipping SharkB because was already destroyed when scrapped.", Verbosity.DoNotShow );
                return;
            }

            if ( entity.SecondsSpentAsRemains > 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Skipping SharkB because was already remains when scrapped.", Verbosity.DoNotShow );
                return;
            }
            
            TriggerSharkB( entityOwningFaction, null, entity, Context );
        }

        #region TriggerSharkB
        public static void TriggerSharkB( Faction factionThatHadADeath, Faction killingFactionOrNull, GameEntity_Squad entityThatDiedOrNull, ArcenHostOnlySimContext Context, Planet planetForDeathOrNull = null, GameEntityTypeData typeDataOrNull = null )
        {
            if ( Context == null ) //client
                return;
            int debugStage = 0;
            try
            {
                // The setting can exist and have a value from a prior run
                // but the mod itself is off currently.
                // (The setting is defined in core but hidden so that its sort order
                // puts it near existing settings its related to).
                var mod = XmlModTable.GetModByNameOrAltName("RebalancingParty");
                if (mod != null && mod.IsOn())
                {
                    if (World_AIW2.Instance.Setup.GetBoolBySetting( "SharkB_Entirely_Off" ))
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "Skipping SharkB because specified off in settings.", Verbosity.DoNotShow );
                        return;
                    }
                }

                debugStage = 3300;
                //find my king
                GameEntity_Squad kingUnit = null;
                debugStage = 3400;
                if ( factionThatHadADeath != null )
                {
                    foreach ( GameEntity_Squad kentity in factionThatHadADeath.Squads( EntityRollupType.KingUnitsOnly ) )
                    {
                        kingUnit = kentity;
                    }
                }
                //ArcenDebugging.ArcenDebugLogSingleLine( "kingUnit = " + ( kingUnit == null ? "null" : "ok" ), Verbosity.DoNotShow );
                debugStage = 3500;
                if ( kingUnit != null )
                {
                    debugStage = 3600;
                    if ( killingFactionOrNull == null || killingFactionOrNull.Type != FactionType.AI )
                        killingFactionOrNull = World_AIW2.GetRandomAIFaction( Context );
                    if ( killingFactionOrNull == null || !killingFactionOrNull.CheckBlocksVictory() )
                        return; //no living AIs left
                    AIDifficulty difficulty = FactionUtilityMethods.Instance.GetHighestAIDifficulty_AsDifficulty();

                    debugStage = 3700;
                    FInt strengthForAttack = FInt.Zero;
                    bool sharkB = World_AIW2.Instance.Setup.GetBoolBySetting( "SharkB" );
                    if ( sharkB )
                        strengthForAttack += ExternalConstants.Instance.SharkBBaseStrength + (ExternalConstants.Instance.SharkBBonusStrengthPerAIP * GlobalAIWorldBaseInfo.Instance.AIProgress_Effective);
                    strengthForAttack += difficulty.SharkB2_BaseStrength + (difficulty.SharkB2_BonusStrengthPerAIP * GlobalAIWorldBaseInfo.Instance.AIProgress_Effective);

                    //ArcenDebugging.ArcenDebugLogSingleLine( "strengthForAttack = " + strengthForAttack, Verbosity.DoNotShow );
                    if ( strengthForAttack > FInt.Zero )
                    {
                        ExoOptions options = ExoOptions.CreateWithDefaults( kingUnit, strengthForAttack.GetNearestIntPreferringHigher(), null, killingFactionOrNull );
                        if ( entityThatDiedOrNull != null )
                            options.exoText = "<color=#ff2233>The AI is sending an Exogalactic Strikeforce after the death of the command station on </color><color=#a1ffa1>" + entityThatDiedOrNull.Planet.Name + ".</color>";
                        else
                            options.exoText = "<color=#ff2233>The AI is sending an Exogalactic Strikeforce after the death of the " + typeDataOrNull.GetDisplayName() + " on </color><color=#a1ffa1>" + planetForDeathOrNull.Name + ".</color>";
                        ExoGalacticAttackManager.SendExoGalacticAttack( options, Context );
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception during TriggerSharkB debugStage " + debugStage + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion
    }
}
