using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Text;

namespace Arcen.AIW2.External
{
    public sealed class AISentinelsFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        //Set immediately before the PotentialWBPlanets sorts so the comparisons can be non-capturing
        //static delegates.  [ThreadStatic] for safety since this is background-planning code.
        [ThreadStatic] private static Planet cb_aisWBSortPlanet;
        [ThreadStatic] private static Dictionary<Planet, int> cb_aisConnectedCount;
        public AISentinelsFactionBaseInfo BaseInfo;
        public bool OverlordGuardPostFlushActive = false;

        private static Expansion cachedDlc4Expansion_Sentinels = null;
        private static bool dlc4LookupDone_Sentinels = false;
        private static bool GetIsDlc4InstalledAndEnabled()
        {
            if ( !dlc4LookupDone_Sentinels )
            {
                cachedDlc4Expansion_Sentinels = ExpansionTable.Instance.GetRowByNameOrNullIfNotFound( "4_Forge_Of_Empires_Supporter" );
                dlc4LookupDone_Sentinels = true;
            }
            return cachedDlc4Expansion_Sentinels != null && cachedDlc4Expansion_Sentinels.IsInstalledAndEnabled;
        }

        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
        }

        protected override void Cleanup()
        {
            BaseInfo = null;
            OverlordGuardPostFlushActive = false;

            //may matter a lot
            pendingSetWaitCommands.Clear();

            //probably does not matter, but why not
            exoGenerators.Clear();
            ShipsInExogalacticAttacks.Clear();
            RelicTrains.Clear();
            EnemyKingAttackers.Clear();
            WormholeBorers.Clear();
            CuendillarTransports.Clear();
            PreferredWarpGates.Clear();
            FallbackWarpGates.Clear();
            WarpGatesToUse.Clear();
            ExtragalacticBag.Clear();
            BorerStartPlanets.Clear();
        }

        #region DoAIDefeatLogicOnNoKingsLeftLogic
        public void DoAIDefeatLogicOnNoKingsLeftLogic( ArcenHostOnlySimContext Context, GameEntity_Squad kingUnitOrNull )
        {
            if ( AttachedFaction.FactionIsDefeated )
                return;

            AttachedFaction.FactionIsDefeated = true;
            bool undefeatedAIsRemaining = false;

            //This AI has lost its coordination with the other factions, and is now hostile to the other AIs
            AllegianceHelper.MakeAIHostileToOtherAIs( AttachedFaction );

            //blow up anything that should die with the AI overlord (like exogalactic wormholes)
            foreach ( GameEntity_Squad entityToDie in AttachedFaction.Squads( EntityRollupType.AutomaticallyDiesWithAIOverlord ) )
            {
                entityToDie.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut ); //not sure if this is the right reason
            }

            Faction factionBlockingVictory = FactionUtilityMethods.Instance.GetFactionBlockingVictoryOrNull();
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                //Now check for whether other AIs are left to fight, and also to handle the sub-factions for this AI
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( AttachedFaction == otherFaction )
                    continue;

                if ( otherFaction.Type == FactionType.AI )
                {
                    if ( !otherFaction.FactionIsDefeated )
                        undefeatedAIsRemaining = true;
                }
                else if ( FactionUtilityMethods.Instance.IsACoreAISubFaction( otherFaction ) )
                {
                    var otherFactionExternal = otherFaction.BaseInfo;
                    if ( otherFaction.FactionIndexOfMyParentIfIHaveOne == AttachedFaction.FactionIndex )
                    {
                        //this is one of my minor factions, which should also become hostile to all the other AIs
                        AllegianceHelper.MakeAIHostileToOtherAIs( otherFaction );
                    }
                }
            }
            if ( ArcenNetworkAuthority.GetIsHostMode() )
            {
                if ( undefeatedAIsRemaining )
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "CP_AIDef_More", string.Empty, kingUnitOrNull, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                else if ( factionBlockingVictory != null )
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "CP_AIDef_MoreOthers", string.Empty, kingUnitOrNull, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                else
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "CP_AIDef_Final", string.Empty, kingUnitOrNull, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
        }
        #endregion

        #region DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly
        public override void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context)
        {
            if ( entity == null )
                return;
            if ( Context == null ) //client
                return;

            int debugStage = 0;
            try
            {
                // jcf: this could be refactored to use the aitype callback for this i added and use below 
                {
                    debugStage = 100;
                    if ( this.BaseInfo.SentinelInfo.AIType.GenerateExoOnImportantStructureDeath && FiringSystemOrNull != null )
                    {
                        debugStage = 200;
                        //This is for the Vengeful AI type
                        if ( (
                            entity.TypeData.SpecialType == SpecialEntityType.GuardPost ||
                            entity.TypeData.SpecialType == SpecialEntityType.DireGuardPost ||
                             entity.TypeData.AIPOnDeath != 0 ||
                             entity.TypeData.IsCommandStation ||
                             entity.TypeData.GetHasTag( "NormalPlanetNastyPick" )
                             )
                              //not still under construction
                              && entity.SelfBuildingMetalRemaining <= FInt.Zero
                             )
                        {
                            debugStage = 300;
                            GameEntity_Squad firingParent = FiringSystemOrNull.ParentEntity;
                            debugStage = 400;
                            if ( firingParent != null && FactionUtilityMethods.Instance.IsFactionAlliedToAnyPlayer( firingParent.GetFactionOrNull_Safe() ) )
                            {
                                debugStage = 500;
                                //this is a guard post, command station or something important. And it was killed by a player
                                //note this may need to fire on minor factions? Unclear

                                //There are two basic paths. One is to do something fancy (option A). The other is to just spawn a little Exo
                                //right now (option B)

                                //Option A: when a structure like this is killed, we update a FInt on the faction object. The units of that FInt are "WaveSize worth of Exo response to the deaths
                                //And we update an int that says "Last time an exo generating thing was killed".
                                //Once it's been 60 seconds since such a unit died, we drop an exo in a few minutes.
                                //The exo should actually be sent in the sim code based on the Strength and Last Time Killed
                                //Perhaps we should also track the units killed because that might be fun for the player? Or the number of units killed?

                                //Option B:
                                Faction faction = entity.GetFactionOrNull_Safe();
                                GameEntity_Squad exoTarget = null;
                                debugStage = 500;
                                if ( FiringSystemOrNull.ParentEntity.GetFactionTypeSafe() == FactionType.Player )
                                {
                                    debugStage = 510;
                                    exoTarget = ExoGalacticAttackManager.GetHumanHomeCommandStation( FiringSystemOrNull.ParentEntity.PlanetFaction.Faction );
                                }
                                else
                                {
                                    debugStage = 550;
                                    int mostAnnoyingHumanFactionIdx = entity.PlanetFaction.GetIndexOfMostAnnoyingHumanFaction( Context );
                                    Faction potentialFaction = World_AIW2.Instance.GetFactionByIndex( mostAnnoyingHumanFactionIdx );
                                    if ( potentialFaction != null && potentialFaction.Type == FactionType.Player )
                                        exoTarget = ExoGalacticAttackManager.GetHumanHomeCommandStation( potentialFaction );
                                    else
                                        exoTarget = ExoGalacticAttackManager.GetRandomHumanHomeCommandStation( Context );
                                }
                                debugStage = 610;
                                if ( exoTarget != null )
                                {
                                    debugStage = 620;
                                    int WaveSize = this.BaseInfo.GetSpecificBudgetThreshold( AIBudgetType.Wave, GlobalAIWorldBaseInfo.Instance.AIProgress_Effective );
                                    int exoSize = WaveSize / 3;
                                    if ( exoSize < 1000 )
                                        exoSize = 1000;
                                    debugStage = 630;
                                    ExoOptions options = ExoOptions.CreateWithDefaults( exoTarget, exoSize, null, faction );
                                    debugStage = 640;
                                    int random = Context.RandomToUse.Next( 0, 100 );
                                    string aicolor = faction.FactionCenterColor.ColorHexBrighter;
                                    debugStage = 680;
                                    string planetStr = "<color=#" + faction.FactionCenterColor.ColorHexBrighter + ">" + entity.GetPlanetName_Safe() + "</color>";
                                    if ( random < 10 )
                                        options.exoText = "来自地狱黑暗之心，位于 " + planetStr + " 的 <color=#" + aicolor + ">" + entity.TypeData.GetDisplayName() + "</color> 刺向你！";
                                    else if ( random < 20 )
                                        options.exoText = "AI 希望为位于 " + planetStr + " 的 <color=#" + aicolor + ">" + entity.TypeData.GetDisplayName() + "</color> 复仇。";
                                    else if ( random < 30 )
                                        options.exoText = "位于 " + planetStr + " 的 <color=#" + aicolor + ">" + entity.TypeData.GetDisplayName() + "</color> 的回响将永存。";
                                    else if ( random < 40 )
                                        options.exoText = "AI 河外打击力量正在逼近；看来摧毁位于 " + planetStr + " 的 <color=#" + aicolor + ">" + entity.TypeData.GetDisplayName() + "</color> 产生了后果。";
                                    else if ( random < 50 )
                                        options.exoText = "为位于 " + planetStr + " 的 <color=#" + aicolor + ">" + entity.TypeData.GetDisplayName() + "</color> 复仇一击。";
                                    else if ( random < 60 )
                                        options.exoText = "位于 " + planetStr + " 的 <color=#" + aicolor + ">" + entity.TypeData.GetDisplayName() + "</color> 将从坟墓中复仇。";
                                    else if ( random < 70 )
                                        options.exoText = "摧毁位于 " + planetStr + " 的 <color=#" + aicolor + ">" + entity.TypeData.GetDisplayName() + "</color> 触发了 AI 远征。";
                                    else if ( random < 80 )
                                        options.exoText = "位于 " + planetStr + " 的 <color=#" + aicolor + ">" + entity.TypeData.GetDisplayName() + "</color> 致以问候。";
                                    else if ( random < 90 )
                                        options.exoText = "AI 正在派遣远征，以报复您摧毁了位于 " + planetStr + " 的 <color=#" + aicolor + ">" + entity.TypeData.GetDisplayName() + "</color>。";
                                    else
                                        options.exoText = "这次远征是为了位于 " + planetStr + " 的 <color=#" + aicolor + ">" + entity.TypeData.GetDisplayName() + "</color>。";
                                    ExoGalacticAttackManager.SendExoGalacticAttack( options, Context );
                                }
                            }
                        }
                    }
                }

                // callback into the aitype for this event
                this.BaseInfo.SentinelInfo.AIType.Implementation.DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( entity, Damage, FiringSystemOrNull, Context);

                debugStage = 990;

                if ( entity.TypeData.IsCommandStation
                    && FiringSystemOrNull != null // the game knows who killed it
                    && FiringSystemOrNull.ParentEntity.GetIsFactionControlledByLocalPlayerAccount_Safe() ) // the killer is the local player (for "any player", check for ControlledByPlayerAccounts.Count > 0)
                {
                    debugStage = 1000;
                    bool dysonEffectPlayed = false;
                    //First check if killing this controller freed the Dyson Sphere; if so then that message takes priority
                    if ( entity.Planet.GetFirstMatching( FactionType.SpecialFaction, SphereFactionBaseInfo.Tag_ZenithSphere, true, true ) != null )
                    {
                        dysonEffectPlayed = true;

                        PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                        if ( chatHandlerOrNull != null )
                            chatHandlerOrNull.PlanetToView = entity.Planet;

                        World_AIW2.Instance.QueueChatMessageOrCommand( entity.GetPlanetName_Safe() + " 指挥站被摧毁！", ChatType.LogToCentralChat,
                            "ArkChiefOfStaff_DysonLiberatedFromPlayer", chatHandlerOrNull );
                    }
                    debugStage = 1200;

                    if ( !dysonEffectPlayed )
                    {
                        //If this is a high mark planet
                        if ( entity.Planet.MarkLevelForAIOnly.Ordinal >= 4 )
                        {
                            Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.PlayerDestroysAIControllerHard );
                        }
                        else
                        {
                            Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.PlayerDestroysAIControllerNormal );
                        }
                    }
                }
                debugStage = 2100;
                if ( entity.TypeData.GetHasTag( "AIOverlordPhase1_AnyType" )
                    && FiringSystemOrNull != null &&
                     entity.GetShouldBeVisibleBasedOnPlanetIntel() ) // the game knows who killed it
                {
                    debugStage = 2200;
                    //Doesn't have to be by a player (perhaps the player has persuaded a minor faction to do the killing?)
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "AI_Overlord_Transforms", string.Empty, entity.GetFactionOrNull_Safe(), null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.AIOverlordTransforms );
                    if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "EnhancedOverlord" ) )
                    {
                        debugStage = 2400;
                        if ( GetIsDlc4InstalledAndEnabled() )
                            this.OverlordGuardPostFlushActive = true;
                        //trigger a CPA immediately as the overlord transforms
                        {
                            PlannedWave wave = PlannedWave.GetFromPoolOrCreate();

                            wave.SendingFactionIndex = AttachedFaction.FactionIndex;
                            string waveWarning = World_AIW2.Instance.Setup.GetStringBySetting( "WaveWarning" );
                            Int16 secondsOfWarningToGive = 120;
                            wave.gameTimeInSecondsForLaunchWave = secondsOfWarningToGive + World_AIW2.Instance.GameSecond;
                            wave.secondsAdvanceWarningToGive = secondsOfWarningToGive;
                            wave.playerBeingAlerted = true;
                            wave.isActuallyACrossPlanetAttack = true;
                            if ( AISentinelsFactionBaseInfo.DebugWaveAndCPASpawns )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Spawn CPA in " + secondsOfWarningToGive + " seconds", Verbosity.DoNotShow );

                            this.BaseInfo.WaveList.Add( wave );
                        }
                        //trigger a wormhole invasion
                        {
                            int waveBudget = (this.BaseInfo.GetSpecificBudgetThreshold(  AIBudgetType.Wave, GlobalAIWorldBaseInfo.Instance.AIProgress_Effective ) * 3) ;
                            int wormholeBudget = Context.RandomToUse.Next(waveBudget / 2, waveBudget * 3 );
                            int retries = 10;
                            bool launched = false;
                            do{
                                WormholeInvasionOptions options = WormholeInvasionOptions.CreateWithDefaults( wormholeBudget, this.AttachedFaction );
                                wormholeBudget += 12 * 1000; //give a few retries with larger budgets, in case the player has heavily defended everything
                                options.WaveCount = 1;
                                options.WaveInterval = 30;
                                options.ProjectorAppearanceTime = World_AIW2.Instance.GameSecond + 20;
                                options.PlanetLinkTime = World_AIW2.Instance.GameSecond + 30;
                                launched = WormholeInvasionManager.LaunchWormholeInvasion( options, Context );
                            }while ( !launched && retries-- > 0 );
                        }
                    }
                }
                debugStage = 3100;
                if ( entity.TypeData.GetHasTag( "AIOverlordPhase2_AnyType" ) )
                {
                    debugStage = 3200;
                    Faction faction = entity.GetFactionOrNull_Safe();
                    DoAIDefeatLogicOnNoKingsLeftLogic( Context, entity );
                }
                debugStage = 4100;
                if ( entity.TypeData.GetHasTag("SpireRelicTrain" ) )
                {
                    debugStage = 4200;
                    //if the player has killed a Spire Relic Train then spawn a relic
                    Faction playerFactionToGetRelic = null;
                    if ( FiringSystemOrNull != null ) // the game knows who killed it (which generally means 'not scrapped', but just in case
                    {
                        debugStage = 4300;
                        //we give the relic to either the player who killed it, or to the player with the most strength on the planet
                        Faction killingFaction = FiringSystemOrNull.ParentEntity.GetFactionOrNull_Safe();
                        int mostAnnoyingHumanFactionIdx = entity.PlanetFaction.GetIndexOfMostAnnoyingHumanFaction(Context);
                        if ( killingFaction.Type == FactionType.Player )
                        {
                            playerFactionToGetRelic = killingFaction;
                        }
                        else if ( mostAnnoyingHumanFactionIdx != -1 )
                            playerFactionToGetRelic = World_AIW2.Instance.GetFactionByIndex(mostAnnoyingHumanFactionIdx);
                    }
                    debugStage = 4500;
                    if ( playerFactionToGetRelic != null )
                    {
                        debugStage = 4600;
                        bool differentPlayerRequired = false;
                        if ( !playerFactionToGetRelic.IsConsideredAFullEmpirePlayerType_Safe() )
                        {
                            //a non-empire player is okay if that player counts as a spire faction
                            PlayerTypeData playerType = playerFactionToGetRelic.PlayerTypeDataOrNull_ModeratelyExpensive;
                            if ( playerType == null || !playerType.CountsAsSpireFaction)
                                differentPlayerRequired = true;
                        }
                        if ( differentPlayerRequired )
                        {
                            //fallen spire relics must go to an empire-style faction (it's not at all balanced otherwise)
                            playerFactionToGetRelic = World_AIW2.GetRandomEmpireStyleFaction( Context );
                        }
                        Faction fsFaction = FactionUtilityMethods.Instance.GetFallenSpireFaction();
                        if ( fsFaction == null )
                            throw new Exception("No fallen spire faction in death of spire relic train");
                        if ( SpireSidekickFactionBaseInfo.GetSpireSidekickFactionCount() > 0 )
                            SpireSidekickFactionBaseInfo.Instance.CreateRelic(entity.Planet, fsFaction, playerFactionToGetRelic, Context, FInt.One, false, entity.WorldLocation, false);
                        else
                        {
                            //Fallen Spire/Spire Infused Empire
                            FallenSpireFactionBaseInfo.Instance.CreateRelic(entity.Planet, fsFaction, playerFactionToGetRelic, Context, FInt.One, false, entity.WorldLocation, false);
                        }
                    }
                }
                debugStage = 5100;
                if ( entity.TypeData.GetHasTag("AISpireResearchLab" ) )
                {
                    debugStage = 5200;
                    //if the player has killed a Spire Relic Train then spawn a relic
                    Faction playerFactionToGetRelic = null;
                    if ( FiringSystemOrNull != null ) // the game knows who killed it (which generally means 'not scrapped', but just in case
                    {
                        debugStage = 5300;
                        //we give the relic to either the player who killed it, or to the player with the most strength on the planet
                        Faction killingFaction = FiringSystemOrNull.ParentEntity.GetFactionOrNull_Safe();
                        int mostAnnoyingHumanFactionIdx = entity.PlanetFaction.GetIndexOfMostAnnoyingHumanFaction(Context);
                        if ( killingFaction.Type == FactionType.Player )
                        {
                            playerFactionToGetRelic = killingFaction;
                        }
                        else if ( mostAnnoyingHumanFactionIdx != -1 )
                            playerFactionToGetRelic = World_AIW2.Instance.GetFactionByIndex(mostAnnoyingHumanFactionIdx);
                    }
                    debugStage = 5400;
                    if ( playerFactionToGetRelic != null )
                    {
                        debugStage = 5600;
                        bool differentPlayerRequired = false;
                        if ( !playerFactionToGetRelic.IsConsideredAFullEmpirePlayerType_Safe() )
                        {
                            //a non-empire player is okay if that player counts as a spire faction
                            PlayerTypeData playerType = playerFactionToGetRelic.PlayerTypeDataOrNull_ModeratelyExpensive;
                            if ( playerType == null || !playerType.CountsAsSpireFaction)
                                differentPlayerRequired = true;
                        }
                        if ( differentPlayerRequired )
                        {
                            //fallen spire relics must go to an empire-style faction (it's not at all balanced otherwise)
                            playerFactionToGetRelic = World_AIW2.GetRandomEmpireStyleFaction( Context );
                        }
                        Faction fsFaction = FactionUtilityMethods.Instance.GetFallenSpireFaction();
                        if ( fsFaction == null )
                            throw new Exception("No fallen spire faction in death of spire research lab");

                        if ( SpireSidekickFactionBaseInfo.GetSpireSidekickFactionCount() > 0 )
                            SpireSidekickFactionBaseInfo.Instance.CreateRelic(entity.Planet, fsFaction, playerFactionToGetRelic, Context, FInt.One, false, entity.WorldLocation, false);
                        else
                        {
                            //Fallen Spire/Spire Infused Empire
                            FallenSpireFactionBaseInfo.Instance.CreateRelic(entity.Planet, fsFaction, playerFactionToGetRelic, Context, FInt.One, false, entity.WorldLocation, false);
                        }
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_AILoreDump", string.Empty, fsFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    }

                }
                debugStage = 6100;
                if ( entity.TypeData.GetHasTag("AISpireCitadel" ) )
                {
                    debugStage = 6200;
                    //if the player has killed a Spire Relic Train then spawn a relic
                    Faction playerFactionToGetRelic = null;
                    if ( FiringSystemOrNull != null ) // the game knows who killed it (which generally means 'not scrapped', but just in case
                    {
                        debugStage = 6300;
                        //we give the relic to either the player who killed it, or to the player with the most strength on the planet
                        Faction killingFaction = FiringSystemOrNull.ParentEntity.GetFactionOrNull_Safe();
                        int mostAnnoyingHumanFactionIdx = entity.PlanetFaction.GetIndexOfMostAnnoyingHumanFaction(Context);
                        if ( killingFaction.Type == FactionType.Player )
                        {
                            playerFactionToGetRelic = killingFaction;
                        }
                        else if ( mostAnnoyingHumanFactionIdx != -1 )
                            playerFactionToGetRelic = World_AIW2.Instance.GetFactionByIndex(mostAnnoyingHumanFactionIdx);
                    }
                    debugStage = 6500;
                    if ( playerFactionToGetRelic != null )
                    {
                        debugStage = 6600;
                        bool differentPlayerRequired = false;
                        if ( !playerFactionToGetRelic.IsConsideredAFullEmpirePlayerType_Safe() )
                        {
                            //a non-empire player is okay if that player counts as a spire faction
                            PlayerTypeData playerType = playerFactionToGetRelic.PlayerTypeDataOrNull_ModeratelyExpensive;
                            if ( playerType == null || !playerType.CountsAsSpireFaction)
                                differentPlayerRequired = true;
                        }
                        if ( differentPlayerRequired )
                        {
                            //fallen spire relics must go to an empire-style faction (it's not at all balanced otherwise)
                            playerFactionToGetRelic = World_AIW2.GetRandomEmpireStyleFaction( Context );
                        }
                        Faction fsFaction = FactionUtilityMethods.Instance.GetFallenSpireFaction();
                        if ( fsFaction == null )
                            throw new Exception("No fallen spire faction in death of spire citadel");
                        if ( SpireSidekickFactionBaseInfo.GetSpireSidekickFactionCount() > 0 )
                            SpireSidekickFactionBaseInfo.Instance.CreateRelic(entity.Planet, fsFaction, playerFactionToGetRelic, Context, FInt.One, false, entity.WorldLocation, false);
                        else
                        {
                            //Fallen Spire/Spire Infused Empire
                            FallenSpireFactionBaseInfo.Instance.CreateRelic(entity.Planet, fsFaction, playerFactionToGetRelic, Context, FInt.One, false, entity.WorldLocation, false);
                        }

                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_AILoreDump", string.Empty, fsFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    }
                }
                debugStage = 7100;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in AI.DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly stage " + debugStage + "\n" + e, Verbosity.ShowAsError );
            }
        }
        #endregion end DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly


        //This is a very simple version of Mapgen_SeedSpecialEntities
        private void SeedEntities( ArcenHostOnlySimContext Context, string Tag, Galaxy galaxy,
            int minDistanceFromHumanHomeworld, int maxDistanceFromHumanHomeworld,
            int minDistanceFromAIHomeworld, int maxDistanceFromAIHomeworld, int percentToSeedOn,
            int numToSeed )
        {
            //seeds only on planets owned by this faction
            if(percentToSeedOn <= 0 && numToSeed <= 0)
                return;
            List<GameEntityTypeData> eligibleEntityDatas = GameEntityTypeDataTable.Instance.RowsByTag[Tag];

            if ( eligibleEntityDatas == null || eligibleEntityDatas.Count <= 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Harmless warning: AI:SeedEntities called with tag '" + (Tag == null ? "null" : Tag) +
                       "' but none of those exist.  This may be entirely be design, based on mods or expansions.", Verbosity.DoNotShow );
                return;
            }

            List<Planet> workingPotentialPlanets = Planet.GetTemporaryPlanetList( "AISent-SeedEntities-workingPotentialPlanets", 10f );
            if ( workingPotentialPlanets == null ) //blocked for teardown/shutdown; bail
                return;
            List<Planet> backupPotentialPlanets = Planet.GetTemporaryPlanetList( "AiSent-SeedEntities-backupPotentialPlanets", 10f );
            if ( backupPotentialPlanets == null ) //blocked for teardown/shutdown; bail
            {
                Planet.ReleaseTemporaryPlanetList( workingPotentialPlanets );
                return;
            }

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.PopulationType == PlanetPopulationType.AIHomeworld || planet.PopulationType == PlanetPopulationType.HumanHomeworld ) //AIBastionWorld ignore
                    continue;
                if ( minDistanceFromAIHomeworld > 0 && planet.OriginalHopsToAIHomeworld < minDistanceFromAIHomeworld )
                    continue;
                if ( maxDistanceFromAIHomeworld > 0 && planet.OriginalHopsToAIHomeworld > maxDistanceFromAIHomeworld )
                    continue;
                if ( minDistanceFromHumanHomeworld > 0 && planet.OriginalHopsToHumanHomeworld < minDistanceFromHumanHomeworld )
                    continue;
                if ( maxDistanceFromHumanHomeworld > 0 && planet.OriginalHopsToHumanHomeworld > maxDistanceFromHumanHomeworld )
                    continue;

                if ( planet.InitialOwningAIFactionIndex != AttachedFaction.FactionIndex )
                    continue;

                backupPotentialPlanets.Add( planet );

                if ( planet.MapGen_IsFullyUsedByAFaction && planet.MapGen_FullyUsingFaction.FactionIndex != AttachedFaction.FactionIndex )
                    continue;

                workingPotentialPlanets.Add( planet );
            }

            if ( workingPotentialPlanets.Count == 0 )
                workingPotentialPlanets.AddRange( backupPotentialPlanets );

            Planet.ReleaseTemporaryPlanetList( backupPotentialPlanets );

            int planetsToSeedOn = numToSeed;
            if(percentToSeedOn > 0)
            {
                FInt percent = FInt.FromParts(100, 000) / percentToSeedOn;
                planetsToSeedOn = (workingPotentialPlanets.Count / percent).IntValue;
            }
            //            ArcenDebugging.ArcenDebugLogSingleLine("Faction " + faction.FactionIndex + " with " + potentialPlanets.Count + "  potential planets, we will seed on " + planetsToSeedOn + " which is " + percentToSeedOn + " percent (" + percent + ")", Verbosity.DoNotShow );
            if ( planetsToSeedOn == 0 )
            {
                if ( MapgenLogger.IsActive )
                {
                    MapgenLogger.Log( Tag + " Tag not seeded at all (potentialPlanets.Count: " + workingPotentialPlanets.Count + ")" + 
                        " minDistanceFromAIHomeworld:" + minDistanceFromAIHomeworld +
                        " maxDistanceFromAIHomeworld:" + maxDistanceFromAIHomeworld +
                        " minDistanceFromHumanHomeworld:" + minDistanceFromHumanHomeworld +
                        " maxDistanceFromHumanHomeworld:" + maxDistanceFromHumanHomeworld );
                }
                Planet.ReleaseTemporaryPlanetList( workingPotentialPlanets );
                return;
            }
            ThrowawayDrawBagCanMemLeak<GameEntityTypeData> workingDrawBag = ThrowawayDrawBagCanMemLeak<GameEntityTypeData>.Create_WillActuallyBeGCed( 30 );
            while ( planetsToSeedOn > 0 && workingPotentialPlanets.Count > 0)
            {
                planetsToSeedOn--;
                Planet planet = workingPotentialPlanets[Context.RandomToUse.Next( 0, workingPotentialPlanets.Count )];
                workingPotentialPlanets.Remove( planet );
                if ( !workingDrawBag.GetHasItems() )
                    for ( int i = 0; i < eligibleEntityDatas.Count; i++ )
                        workingDrawBag.AddItem( eligibleEntityDatas[i], 1 );
                if ( !workingDrawBag.GetHasItems() )
                    break;
                GameEntityTypeData squadTypeData = workingDrawBag.PickRandomItemAndDoNotReplace( Context.RandomToUse );
                planet.Mapgen_SeedEntity( Context, AttachedFaction, squadTypeData, PlanetSeedingZone.InnerSystem );

                if ( MapgenLogger.IsActive )
                {
                    MapgenLogger.Log( Tag + " Tag caused seeding of " + ( squadTypeData == null ? "null" : squadTypeData.InternalName ) + " on planet " + planet.Name );
                }
            }
            Planet.ReleaseTemporaryPlanetList( workingPotentialPlanets );
        }
        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType)
        {
            //Seed things based on the AI difficulty, in this case normalPlanetNastyPick
            AIDifficulty difficulty = this.BaseInfo.SentinelInfo.AIDifficulty;
            Tutorial tutorialData = World_AIW2.Instance.TutorialOrNull;
            if ( tutorialData != null && tutorialData.SkipAllGalaxyWideCapturablesAndObstacles )
                return;

            AITypeData aiType = this.BaseInfo.SentinelInfo.AIType;
            AIBudgetItem budgetItem = aiType.BudgetItems[AIBudgetType.Reinforcement];
            if ( budgetItem != null )
            {
                if ( budgetItem.DireSingularFreakySurprisesAIShipGroup != null )
                {
                    SeedOneFromShipGroupCategoryAtStartOnEveryPlanetOfThisFaction_MapGenOnly( budgetItem.RegularSingularFreakySurprisesAIShipGroup, galaxy, Context, 2, 99, false, true );
                    SeedOneFromShipGroupCategoryAtStartOnEveryPlanetOfThisFaction_MapGenOnly( budgetItem.DireSingularFreakySurprisesAIShipGroup, galaxy, Context, 2, 99, true, false );
                }
                else
                    SeedOneFromShipGroupCategoryAtStartOnEveryPlanetOfThisFaction_MapGenOnly( budgetItem.RegularSingularFreakySurprisesAIShipGroup, galaxy, Context, 2, 99, false, false );
            }

            //For any AI Types that want to seed specific units (like how Golemite wants to seed Golems)
            this.BaseInfo.SentinelInfo.AIType.Implementation.SeedStartingEntitiesForAIType( AttachedFaction, Context );

            if ( tutorialData == null || !tutorialData.SkipAINormalPlanetNastyPicks )
            {
                SeedEntities( Context, aiType.BigGunNastyPickTag, galaxy, 3, 999, -1, 999, PercentageFromMultiplayer_BadThings( difficulty.PercentBigGunNastyPick ), 0 );
                SeedEntities( Context, aiType.EyeNastyPickTag, galaxy, 3, 999, -1, 999, PercentageFromMultiplayer_BadThings( difficulty.PercentEyeNastyPick ), 0 );
                SeedEntities( Context, aiType.SupportStructureNastyPickTag, galaxy, 3, 999, -1, 999, PercentageFromMultiplayer_BadThings( difficulty.PercentSupportStructureNastyPick ), 0 );
                SeedEntities( Context, aiType.WildCardNastyPickTag, galaxy, 3, 999, -1, 999, PercentageFromMultiplayer_BadThings( difficulty.PercentWildCardNastyPick ), 0 );
            }
            if ( tutorialData == null || !tutorialData.SkipSpireArchives )
            {
                int countOfPlayerFactionsNeedingSpireArchives = 0;
                foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                {
                    PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                    if ( playerType == null )
                        continue; //only seed if they say to
                    if ( playerType.MapGen_ShouldSpireArchiveSeedForMe )
                        countOfPlayerFactionsNeedingSpireArchives++;
                }

                if ( countOfPlayerFactionsNeedingSpireArchives > 0 )
                    SeedEntities( Context, "SpireArchive", galaxy, -1, 99, 2, 3, 0, 1 );
            }
            if ( tutorialData == null || !tutorialData.SkipCivilWarTriggers )
            {
                if ( World_AIW2.Instance.AIFactions.Count > 1 && !AttachedFaction.InCivilWarMode )
                {
                    if ( World_AIW2.Instance.Setup.GetBoolBySetting( "SeedAICivilWarTriggers" ) )
                        SeedEntities( Context, "CivilWarTrigger", galaxy, -1, 999, 2, 3, 0, 1 );
                }
            }

            //the following code is debugging and left for future reference
            bool eyedebug = false;
            if(eyedebug)
            {
                Planet playerPlanet = null;
                GameEntityTypeData entityData;
                foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    if ( entity.GetFactionTypeSafe() == FactionType.Player )
                    {
                        playerPlanet = entity.Planet;
                    }
                }
                entityData =  GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "IonEye");
                foreach ( Planet neighbor in playerPlanet.LinkedNeighbors( false ) )
                {
                                                       neighbor.Mapgen_SeedEntity( Context, AttachedFaction, entityData, PlanetSeedingZone.InnerSystem);
                }
            }
        }

        public int PercentageFromMultiplayer_BadThings( int OriginalPercentage )
        {
            int humanEmpireCount = World_AIW2.Instance.EmpireStylePlayerFactions.Count;
            if ( humanEmpireCount <= 1 || OriginalPercentage <= 0 || OriginalPercentage >= 100 )
                return OriginalPercentage;
            int newPercentage = OriginalPercentage;
            for ( int i = 1; i < humanEmpireCount; i++ )
            {
                if ( newPercentage < 15 )
                    newPercentage += 2;
                else if ( newPercentage < 30 )
                    newPercentage += 4;
                else if ( newPercentage < 50 )
                    newPercentage += 8;
                else if ( newPercentage < 60 )
                    newPercentage += 12;
                else if ( newPercentage < 70 )
                    newPercentage += 14;
                else if ( newPercentage < 80 )
                    newPercentage += 18;
                else if ( newPercentage < 90 )
                    newPercentage += 1;
            }
            return newPercentage;
        }

        private readonly List<Planet> workingOwnedPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "AISentinelsFactionDeepInfo-workingOwnedPlanets" );

        private readonly ArcenDoubleCharacterBuffer workingBuffer = new ArcenDoubleCharacterBuffer( "AISentinelsFactionDeepInfo-workingBuffer" );

        public void SeedOneFromShipGroupCategoryAtStartOnEveryPlanetOfThisFaction_MapGenOnly( AIShipGroupCategory ShipGroupCategory, Galaxy galaxy, 
            ArcenHostOnlySimContext Context, byte MinMarkLevelOfPlanet, byte MaxMarkLevelOfPlanet, bool SkipIfNotAIHomeOrBastion, bool SkipIfAIHomeOrBastion )
        {
            if ( ShipGroupCategory == null || ShipGroupCategory.DrawBag.InternalListSize <= 0 )
                return; //this is ok!

            //we need to find the planets owned by this AI faction (future-proofing for when the player can start with more AI factions
            workingOwnedPlanets.Clear();
            foreach ( Planet planet in galaxy.Planets( false ) )
            {
                if ( planet.MarkLevelForAIOnly == null )
                    continue;
                if ( planet.MarkLevelForAIOnly.Ordinal < MinMarkLevelOfPlanet || planet.MarkLevelForAIOnly.Ordinal > MaxMarkLevelOfPlanet )
                    continue;
                switch ( planet.PopulationType )
                {
                    case PlanetPopulationType.AIBastionWorld:
                    case PlanetPopulationType.AIHomeworld:
                        if ( SkipIfAIHomeOrBastion )
                            continue;
                        break;
                    default:
                        if ( SkipIfNotAIHomeOrBastion )
                            continue;
                        break;
                }
                if ( planet.InitialOwningAIFactionIndex == AttachedFaction.FactionIndex )
                    workingOwnedPlanets.Add( planet );
            }

            for ( int i = 0; i < workingOwnedPlanets.Count; i++ )
            {
                AIShipGroup groupFromCategory = ShipGroupCategory.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                if ( groupFromCategory == null || groupFromCategory.DrawBag.InternalListSize <= 0 )
                    continue; //this is ok, too!

                Planet planet = workingOwnedPlanets[i];

                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                GameEntityTypeData entityData = groupFromCategory.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                if ( entityData == null )
                    continue;
                ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData,  FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 700 ) );
                GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                    pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "AISentinelsOnePerPlanetStarting" ); //fine because mapgen
                if ( entityData.IsMobile )
                {
                    entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Guard_Guardian_Anchored ); //fine because mapgen
                    GameEntity_Squad commandStation = planet.GetCommandStationOrNull();
                    entity.GuardedUnit = LazyLoadSquadWrapper.Create( commandStation );
                    entity.GuardOrPatrolOffsetPoints.Add( entity.WorldLocation - Engine_AIW2.Instance.CombatCenter ); //TODO: perhaps this needs more offsets? Find a golem and see if it's patrolling properly
                    AngleDegrees initialAngle = commandStation.WorldLocation.GetAngleToDegrees( entity.WorldLocation );
                    //use the more expensive distance method to ensure correctness here
                    int initialDistance = commandStation.WorldLocation.GetDistanceTo( entity.WorldLocation, false );
                    int step = UnityEngine.Mathf.RoundToInt( AngleDegrees.MAX_VALUE / 12 );
                    for ( int j = step; j < AngleDegrees.MAX_VALUE; j += step )
                    {
                        AngleDegrees angleToThisPoint = initialAngle.Add( AngleDegrees.Create( (float)j ) );
                        ArcenPoint thisPoint = commandStation.WorldLocation.GetPointAtAngleAndDistance( angleToThisPoint, initialDistance );
                        entity.GuardOrPatrolOffsetPoints.Add( thisPoint - Engine_AIW2.Instance.CombatCenter );
                    }
                }
            }
        }

        #region UpdateExtragalacticBudgets
        public void UpdateExtragalacticBudgets()
        {
            bool debug = false;
            ProtectedList<ExtragalacticBudget> budgets = this.BaseInfo.SentinelInfo.ExtragalacticBudgets;
            for ( int i = 0; i < budgets.Count; i++ )
            {
                //reset before updating
                budgets[i].PowerLevel = FInt.Zero;
            }
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Updating extragalactic budgets", Verbosity.DoNotShow );
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null )
                    continue;
                if ( AttachedFaction == otherFaction )
                    continue;

                if ( !AttachedFaction.GetIsHostileTowards( otherFaction ) )
                    continue; //only for hostile factions

                if ( otherFaction.Type == FactionType.SpecialFaction && AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "ExtragalacticWarIgnoreMinorFactions" ) )
                    continue;
                if ( otherFaction.Type == FactionType.Player && AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "ExtragalacticWarIgnorePlayers" ) )
                    continue;
                if ( otherFaction.OverallPowerLevel == FInt.Zero )
                    continue;
                bool foundPlayerAlly = false;
                if ( otherFaction.Type == FactionType.SpecialFaction && AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "ExtragalacticWarIgnorePlayerAlliedMinorFactions" ) )
                {
                    foundPlayerAlly = FactionUtilityMethods.Instance.IsFactionAlliedToAnyPlayer( otherFaction );
                    if ( foundPlayerAlly )
                        continue;
                }

                if ( otherFaction.Type == FactionType.AI && otherFaction.FactionIsDefeated )
                    continue; //don't count dead AIs

                if ( AttachedFaction.GetIsHostileTowards( otherFaction ) )
                {
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Considering " + otherFaction.GetDisplayName() + " found player ally: " + foundPlayerAlly, Verbosity.DoNotShow );
                    string playerAllegiance = "对玩家友好"; //all player allied factions are tagged this way, and players are implicitly tagged this way too
                    ExtragalacticBudget budget = ExtragalacticBudget.GetBudgetFromList( budgets, otherFaction );
                    if ( budget == null )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "\tMaking a new budget for " + otherFaction.GetDisplayName() + " (" + otherFaction.BaseInfo.Allegiance + ")", Verbosity.DoNotShow );

                        //initializing the value
                        //hostile to all is by faction, unset allegiance is by faction, everything else by allegiance

                        if ( otherFaction.Type == FactionType.Player || foundPlayerAlly )
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "A", Verbosity.DoNotShow );
                            budget = ExtragalacticBudget.Create( playerAllegiance, FInt.Zero, null );
                        }
                        else if ( string.IsNullOrEmpty( otherFaction.BaseInfo.Allegiance ) ||
                             otherFaction.BaseInfo.Allegiance == "对所有敌对" )
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "B", Verbosity.DoNotShow );
                            budget = ExtragalacticBudget.Create( otherFaction, FInt.Zero, null );
                        }
                        else
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "C", Verbosity.DoNotShow );

                            budget = ExtragalacticBudget.Create( otherFaction.BaseInfo.Allegiance, FInt.Zero, null );
                        }
                        budgets.Add( budget );
                    }
                    budget.PowerLevel += otherFaction.OverallPowerLevel;
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "\tbudget " + budget.ToString() + " Power level is now " + budget.PowerLevel, Verbosity.DoNotShow );
                }
            }
            for ( int i = 0; i < budgets.Count; i++ )
            {
                //reset before updating
                budgets[i].PowerLevel = AdjustEnemyPower( budgets[i].PowerLevel );
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "\tafter adjust, " + budgets[i].ToString() + " Power level is now " + budgets[i].PowerLevel, Verbosity.DoNotShow );
            }
        }
        #endregion UpdateExtragalacticBudgets

        private FInt AdjustEnemyPower( FInt enemyPower)
        {
            //modify enemyPower based on AI Difficulty.
            //modify enemyPower based on AIP/how close the enemy is to the homeworld
            //Cap it based on XML
            AIDifficulty difficulty = this.BaseInfo.SentinelInfo.AIDifficulty;
            if ( enemyPower > difficulty.MaxExtragalacticWarTier )
                return (FInt)difficulty.MaxExtragalacticWarTier;

            //do AIP adjustment
            FInt AIP = GlobalAIWorldBaseInfo.Instance.AIProgress_Effective;
            if ( AIP.IntValue > 400 )
                enemyPower += FInt.FromParts(0, 100);
            if ( AIP.IntValue > 800 )
                enemyPower += FInt.FromParts(0, 100);
            bool isKingThreatened = FactionUtilityMethods.Instance.GetIsKingThreatened( AttachedFaction );
            if ( isKingThreatened )
                enemyPower += FInt.FromParts(0, 500);
            return enemyPower;
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 2;

        
        public override void DoPerSimStepLogic_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            //Check the state of all waves. If it's time to send a wave then do so.
            //if the Warp Gate has been destroyed then Cancel the wave appropriately
            if ( !World_AIW2.Instance.IsFirstFrameOfSecond )
                return;

            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            HandleWavesForAI( Context, pathingCacheData );
            pathingCacheData.ReturnToPool();
        }

        private void HandleWavesForAI( ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData )
        {
            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.BudgetSpend );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AISent-HandleWavesForAI-trace", 10f ) : null;
            //bool somethingToLog = false;
            #endregion

            bool debug = false;
            int debugLogInterval = 60;

            ProtectedList<PlannedWave> QueuedWaves = this.BaseInfo.WaveList;
            if ( QueuedWaves == null )
                return;

            if ( debug && World_AIW2.Instance.GameSecond % debugLogInterval == 0 )
                ArcenDebugging.ArcenDebugLogSingleLine( "PerSimStep: There are " + QueuedWaves.Count + "  queued waves", Verbosity.DoNotShow );

            if ( AISentinelsFactionBaseInfo.DebugWaveAndCPASpawns && QueuedWaves.Count > 0 )
                ArcenDebugging.ArcenDebugLogSingleLine( "Wave count: " + QueuedWaves.Count + " wave timer 1: " + (QueuedWaves[0].gameTimeInSecondsForLaunchWave - World_AIW2.Instance.GameSecond), Verbosity.DoNotShow );

            int debugValue = 0;
            try{
                int originalWavesCount = QueuedWaves.Count;
                for ( int itr = 0; itr < QueuedWaves.Count; itr++ )
                {
                    PlannedWave wave = QueuedWaves[itr];
                    if ( wave == null )
                        continue;

                    //Check and make sure this wave is still valid (the warpgate is still alive and the target still makes sense)

                    debugValue = 10;
                    Planet warpGatePlanet = World_AIW2.Instance.GetPlanetByIndex( wave.planetWithWarpGateIdx );
                    Planet targetPlanet = World_AIW2.Instance.GetPlanetByIndex( wave.targetPlanetIdx );
                    if ( !wave.playerBeingAlerted )
                    {
                        debugValue = 20;
                        //update whether the player is being alerted to this wave
                        if ( wave.gameTimeInSecondsForLaunchWave - World_AIW2.Instance.GameSecond <= wave.secondsAdvanceWarningToGive && (wave.targetPlanetIdx != -1 && World_AIW2.Instance.GetPlanetByIndex(wave.targetPlanetIdx).IntelLevel > PlanetIntelLevel.Unexplored))
                        {
                            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                            if (! wave.playerBeingAlerted && targetPlanet != null &&
                                targetPlanet.GetControllingFaction().GetIsLocalFaction() && localFaction != null )
                            {
                                //play some voice lines when the wave alert pops up
                                GameEntity_Squad localKing = localFaction.GetFactionKing();
                                if ( localKing != null && localKing.Planet == targetPlanet )
                                {
                                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.WaveTargetingHomePlanet );
                                }
                                else
                                {
                                    PlanetFaction pFaction = targetPlanet.GetPlanetFactionForFaction( localFaction );
                                    bool foundCriticalStructure = false;
                                    foreach ( GameEntity_Squad entity in pFaction.Entities.Squads( EntityRollupType.CriticalInfrastructure ) )
                                    {
                                        foundCriticalStructure = true;
                                        break;
                                    }
                                    if ( foundCriticalStructure )
                                        Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.WaveTargetingCriticalPlanet );
                                }
                            }
                            if ( targetPlanet != null && targetPlanet.IntelLevel > PlanetIntelLevel.Unexplored && wave.overrideEntityToSpawnAt == null ) //don't give messages for AI waves spawning at a specific entity. Those are generally for hacking, and the messages are excessive
                            {
                                string taunt = "";
                                Faction controllingFaction = targetPlanet.GetControllingFaction();
                                string colorForTarget = controllingFaction.FactionCenterColor.ColorHexBrighter;
                                if ( controllingFaction.Type == FactionType.Player )
                                    taunt = "EnemyWaveArrived";
                                if ( controllingFaction.GetIsFriendlyTowards( AttachedFaction ) ) //A minor faction might be occupying this w/o killing the command station
                                    colorForTarget = targetPlanet.GetFactionWithSpecialInfluenceHere().FactionCenterColor.ColorHexBrighter;
                                if ( ArcenNetworkAuthority.GetIsHostMode() )
                                {
                                    PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                                    if ( chatHandlerOrNull != null )
                                        chatHandlerOrNull.PlanetToView = targetPlanet;

                                    World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "AI</color> 向 <color=#" + colorForTarget + ">" + 
                                        targetPlanet.Name + "</color>", ChatType.LogToCentralChat, taunt, chatHandlerOrNull );

                                    //definitely taunt
                                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.WaveSpawnsAgainstLocalPlayer );
                                }
                            }
                            else //not normally showing a warning, but maybe taunt anyway
                            {
                                if ( ArcenNetworkAuthority.GetIsHostMode() )
                                {
                                    if ( wave.TargetFactionIndex >= 0 )
                                    {
                                        Faction targetFaction = World_AIW2.Instance.GetFactionByIndex(wave.TargetFactionIndex);
                                        if ( targetFaction != null && targetFaction.Type == FactionType.Player )
                                        {
                                            //Check for this here (the entityToSpawnAt != null case is for hacking waves)
                                            Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.WaveSpawnsAgainstLocalPlayer );
                                        }
                                    }
                                }
                            }
                            wave.playerBeingAlerted = true;
                        }
                    }
                    
                    debugValue = 25;

                    if ( targetPlanet != null && 
                         !wave.playerBeingAlerted &&
                         !wave.isReconquestWave && //reconquest waves are fine with targeting undefended things
                         ( ( targetPlanet.GetControllingFactionType() == FactionType.NaturalObject && targetPlanet.PrimaryInfluencingFaction == -1 ) ||
                           (targetPlanet.GetControllingFaction()?.GetIsFriendlyTowards( AttachedFaction )??false) ) )
                    {
                        PlanetFaction myFaction = targetPlanet.GetPlanetFactionForFaction( AttachedFaction );
                        int hostileTurretStrength = myFaction.DataByStance[FactionStance.Hostile].TurretStrength;
                        if ( !(AttachedFaction.InCivilWarMode && hostileTurretStrength > 300 ) )
                        {
                            //The AI has queued up a wave against a target planet, but whoever owned that planet before has lost control of the planet. Since the player hasn't been alerted,
                            //silently find a new target for it. Note that AIs in Civil War Mode are allowed to send waves at unowned enemy planets that have turrets
                            if ( tracing )
                                tracingBuffer.Add( "PerSimStep wave re-route: the target planet ").Add( targetPlanet.Name ).Add(", is no longer a threat. Find a new target for the wave which will spawn at " + wave.gameTimeInSecondsForLaunchWave );

                            //spawn a brand new wave with the same launch time as this wave. The player won't know the difference
                            this.TryToSpendBudget_Wave( Context, wave.aiCostBudgetForWave, wave.gameTimeInSecondsForLaunchWave );

                            wave.deQueueWave = true;
                            if ( itr >= originalWavesCount )
                                throw new Exception( "Wave " + itr + " <> was just queued, but it seems to have a problem and needs to be dequeued immediately. Something is wrong. The most likely outcome is that the  game is finding a 'valid' target for a wave, but the 're-route invalid waves' code is immediately flagging it. Usually this means something is wrong with the wave targeting. The last time this was hit, the AI was inadvertently hostile to the Neutral Faction");
                            continue;
                        }
                    }
                    if ( warpGatePlanet != null )
                    {
                        bool foundWarpGate = false;
                        bool foundExoWormhole = false;
                        for(int j = 0; j < World_AIW2.Instance.AIFactions.Count; j++)
                        {
                            //ai factions are allowed to use eachother's warp gates
                            Faction aiFaction = World_AIW2.Instance.AIFactions[j];
                            if( aiFaction.GetIsHostileTowards( AttachedFaction))
                                continue;

                            PlanetFaction pFaction = warpGatePlanet.GetPlanetFactionForFaction(aiFaction);
                        
                            foreach ( GameEntity_Squad entity in pFaction.Entities.Squads( EntityRollupType.WarpEntryPoints ) )
                            {
                                foundWarpGate = true;
                                if(entity.TypeData.GetHasTag("ExogalacticWormhole"))
                                    foundExoWormhole = true;
                            }
                            if(foundExoWormhole)
                                wave.isExogalacticWormholeWave = true;
                        }
                        if(!foundWarpGate && !wave.isExogalacticWormholeWave)
                        {
                            debugValue = 30;
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "PerSimStep: Warp Gate on planet " + warpGatePlanet.Name + " has been destroyed", Verbosity.DoNotShow );
                        /*
                          if Player Has Not Been Warned About Wave
                          attempt to pick a new suitable warp gate/target. If no other valid targets, spawn wave as threat.
                          This way a player can't be penalized for interfering with a wave they don't know about
  
                          If player has been warned about the wave, strengthen the next wave/wormhole invasion, then
                          reschedule the wave to arrive again soon
                        */
                            if ( wave.playerBeingAlerted )
                            {
                                AIBudgetItem item = this.BaseInfo.SentinelInfo.AIType.BudgetItems[AIBudgetType.Wave];
                                int timeForNextWave = World_AIW2.Instance.GameSecond + item.SecondsBetweenAttemptsToSpend/2;
                                debugValue = 40;
                                if ( tracing )
                                    tracingBuffer.Add( "PerSimStep wave cancellation: Humans have been warned, so strengthen the next wave by the budget of this wave (" + wave.aiCostBudgetForWave + " * " + wave.cancelRefundRatioForNextWave + ") and strengthen the next wormhole invasion by " + wave.aiCostBudgetForWave + " * " + wave.cancelRefundRatioForNextWormholeInvasion + "), then reschedule this wave at " + timeForNextWave );
                                this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.Wave] += wave.aiCostBudgetForWave * wave.cancelRefundRatioForNextWave;
                                this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.WormholeInvasion] += wave.aiCostBudgetForWave * wave.cancelRefundRatioForNextWormholeInvasion;
                                debugValue = 50;
                                this.TryToSpendBudget_Wave( Context, wave.aiCostBudgetForWave, timeForNextWave );
                            }
                            else
                            {
                                if ( tracing )
                                    tracingBuffer.Add( "PerSimStep wave cancellation: respawnWave with overrideTime " + wave.gameTimeInSecondsForLaunchWave );

                                //spawn a brand new wave with the same launch time as this wave. The player won't know the difference
                                this.TryToSpendBudget_Wave( Context, wave.aiCostBudgetForWave, wave.gameTimeInSecondsForLaunchWave );
                            }
                            wave.deQueueWave = true;
                            continue;
                        }
                    }
                    debugValue = 60;
                    //Check if this wave is ready to launch
                    if ( debug && World_AIW2.Instance.GameSecond % debugLogInterval == 0 )
                        ArcenDebugging.ArcenDebugLogSingleLine( "PerSimStep: currentTime " + World_AIW2.Instance.GameSecond + " Wave " + itr + ": ", Verbosity.DoNotShow );
                    if ( AttachedFaction.Debug_ImmediatelyLaunchAllWaves || wave.gameTimeInSecondsForLaunchWave <= World_AIW2.Instance.GameSecond )
                    {
                        debugValue = 70;
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "PerSimStep: Launch this wave", Verbosity.DoNotShow );
                        wave.deQueueWave = true;
                        wave.sendWaveThisSimStep = true;
                    }
                }
                
                AttachedFaction.Debug_ImmediatelyLaunchAllWaves = false;
                for ( int i = 0; i < QueuedWaves.Count; i++ )
                {
                    PlannedWave wave = QueuedWaves[i];
                    //launch wave
                    debugValue = 80;
                    if ( wave.sendWaveThisSimStep )
                    {
                        debugValue = 90;
                        
                        //Figure out if we are against a particular minor faction or something
                        Int16 targetFactionIdx = wave.TargetFactionIndex;
                        Faction targetFaction = World_AIW2.Instance.GetFactionByIndex(wave.TargetFactionIndex);
                        Planet targetPlanet = World_AIW2.Instance.GetPlanetByIndex( wave.targetPlanetIdx );
                        if ( targetFaction != null && targetPlanet != null &&
                             targetFaction.Type == FactionType.NaturalObject )
                        {
                            if ( targetPlanet.GetControllingOrInfluencingFaction().GetIsHostileTowards( AttachedFaction ) )
                                targetFactionIdx = targetPlanet.GetControllingOrInfluencingFaction().FactionIndex;
                            else if ( targetPlanet.GetFactionWithSpecialInfluenceHere().GetIsHostileTowards( AttachedFaction ) )
                                targetFactionIdx = targetPlanet.GetFactionWithSpecialInfluenceHere().FactionIndex;
                            else
                                targetFactionIdx = -1;
                        }
                        
                        this.SpawnWave( Context, PathCacheData, wave, targetFactionIdx ); // to launch a wave against a faction other than the humans, put its index in place of that -1
                    }
                }
                
                RemoveOldWavesToDequeue( this.BaseInfo.WaveList, ref debugValue, debug );
            }
            catch( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Error during DoPerSimStepLogic_OnMainThreadAndPartOfSim debug number " + debugValue +
                                              "\n" + e, Verbosity.ShowAsError );
            }
            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracingBuffer != null )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
        }

        private void RemoveOldWavesToDequeue( ProtectedList<PlannedWave> QueuedWaves, ref int debugValue, bool debug )
        {
            ArcenCharacterBuffer debugBufferOrNull = null;

            int previousCount = QueuedWaves.Count;
            int wavesRemoved = 0;
            for ( int i = QueuedWaves.Count - 1; i >= 0; i-- )
            {
                debugValue = 8;
                //remove waves from list (it was cancelled or launched)
                if ( QueuedWaves[i].deQueueWave )
                {
                    wavesRemoved++;
                    debugValue = 100;
                    if ( debug )
                    {
                        if ( debugBufferOrNull == null )
                            debugBufferOrNull = ArcenCharacterBuffer.GetFromPoolOrCreate( "RemoveOldWavesToDequeue-debugBufferOrNull", 10f );
                        else
                            debugBufferOrNull.Clear();

                        debugBufferOrNull.Add( "Dequeuing wave " + i + " " );
                        QueuedWaves[i].AppendStateForInterfaceDisplay( debugBufferOrNull );

                        ArcenDebugging.ArcenDebugLogSingleLine( debugBufferOrNull.ToString(), Verbosity.DoNotShow );
                    }
                    QueuedWaves.Remove( QueuedWaves[i], true );
                }
            }
            if ( debug && wavesRemoved > 0 )
                ArcenDebugging.ArcenDebugLogSingleLine("After dequeuing, we had " + previousCount + " waves, and removed " + wavesRemoved + " so now we have " + QueuedWaves.Count, Verbosity.DoNotShow );
            if ( wavesRemoved > 0 )
                World_AIW2.Instance.OnServer_FactionsToFastBlastToClients.Enqueue( this.AttachedFaction ); //make sure to immediately remove stale waves
            if ( debugBufferOrNull != null )
                debugBufferOrNull.ReturnToPool();
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            int debugStage = 1;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                debugStage = 100;
                //if the player has won the game, the AI doesn't get to do much anymore
                if ( World.Instance.ConclusionType == CampaignConclusionType.Won || AttachedFaction.FactionIsDefeated )
                    return;

                debugStage = 300;
                if ( this.BaseInfo.SentinelInfo.AdaptiveAIDifficulty != TypeDifficulty.Unset &&
                     World_AIW2.Instance.GameSecond % ExternalConstants.Instance.AdaptiveAIChangeInterval == 0 )
                {
                    debugStage = 310;
                    //if we are an Adaptive AI type and it's time to change types, update the type, then change the defensive placers
                    //so we get new wave/reinforcements
                    this.BaseInfo.SetNewAITypeFromAdaptiveAI( AITypeDataTable.Instance.GetRandomTypeByDifficulty( this.BaseInfo.SentinelInfo.AdaptiveAIDifficulty ) );
                    debugStage = 320;
                    foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                    {
                        debugStage = 340;
                        AIDefensePlacer placer = planet.GetCurrentDefensePlacer( Context );
                        debugStage = 350;
                        AIDefensePlacer_Default.DefinePlanetFactionDefenseTypesIfNeeded( Context, planet, AttachedFaction, true );
                    }
                }

                debugStage = 400;
                FInt ataip = GlobalAIWorldBaseInfo.Instance.AIProgress_Effective;
                this.BaseInfo.SentinelInfo.AIType.Implementation.SetSpendingRatios( AttachedFaction, this.BaseInfo.SentinelInfo.CurrentBudgetConfiguration, ataip );

                #region Tracing
                bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.BudgetSpend );
                bool tracing_wave = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Wave );
                ArcenCharacterBuffer tracingBuffer = (tracing || tracing_wave) ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AISent-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f ) : null;
                bool somethingToLog = false;
                #endregion
                #region check budgets
                debugStage = 500;
                for ( AIBudgetType budget = AIBudgetType.None + 1; budget < AIBudgetType.Length; budget++ )
                {
                    debugStage = 510;
                    AIBudgetItem item = this.BaseInfo.SentinelInfo.AIType.BudgetItems[budget];

                    FInt newStrength = FInt.Zero;
                    debugStage = 520;
                    if ( World.Instance.ConclusionType != CampaignConclusionType.Won )
                        newStrength = this.BaseInfo.GetSpecificBudgetAIPurchaseCostGainPerSecond( budget, true, true, ataip );
                    bool canUpdateThisBudget = true;
                    //Check to make sure we are above the AIP threshold for certain budget types
                    if ( budget == AIBudgetType.Reconquest )
                    {
                        debugStage = 530;
                        if ( ataip < this.BaseInfo.SentinelInfo.AIDifficulty.AIPUnlockReconquestWave )
                            canUpdateThisBudget = false;
                        debugStage = 540;
                        bool reconquestWavesOn = World_AIW2.Instance.Setup.GetBoolBySetting( "ReconquestWave" );
                        if ( !reconquestWavesOn )
                        {
                            canUpdateThisBudget = false;
                        }
                    }
                    else if ( budget == AIBudgetType.BorderAggression )
                    {
                        if ( !AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "EnableBorderAggression" ) )
                        {
                            //border aggression is disabled, so put all these resources into waves instead
                            this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.Wave] += newStrength;
                            continue;
                        }
                        else if ( this.BaseInfo.SentinelInfo.AIDifficulty.Budget_IncomeMultiplierForBorderAggression > 0 )
                        {
                            //at higher difficulties, the AI gets a lot more border aggression
                            newStrength *= this.BaseInfo.SentinelInfo.AIDifficulty.Budget_IncomeMultiplierForBorderAggression;
                        }
                    }
                    else
                    {
                        debugStage = 600;
                        if ( budget == AIBudgetType.WormholeInvasion &&
                             (this.BaseInfo.SentinelInfo.AIDifficulty.AIPUnlockWormholeInvasion == -1 ||
                              ataip < this.BaseInfo.SentinelInfo.AIDifficulty.AIPUnlockWormholeInvasion) )
                            canUpdateThisBudget = false;
                        else
                        {
                            debugStage = 650;
                            if ( budget == AIBudgetType.Wave && this.BaseInfo.SentinelInfo.WavesTemporarilyDisabled )
                                canUpdateThisBudget = false; //waves are temporarily disabled; this is intended for the tutorial, but is also used for when the user has explicitly disabled waves
                                                             //If we can't update this budget type, just dump the strength into reinforcements and don't try to spend anything
                        }
                    }
                    debugStage = 700;
                    if ( !canUpdateThisBudget )
                    {
                        debugStage = 710;
                        //                    ArcenDebugging.ArcenDebugLogSingleLine("Donating " + budget + " budget to reinforcements", Verbosity.DoNotShow );
                        //Chris says: just throw it away, don't give it to reinforcements
                        //this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.Reinforcement] += newStrength;
                        continue;
                    }
                    bool verboseDebug = false;
                    if ( tracing && verboseDebug )
                        tracingBuffer.Add("\tBudget: " + budget + " at " + this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budget] + " + newStrength " + newStrength ).Add("\n");
                    this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budget] += newStrength;
                    debugStage = 800;

                    if ( AttachedFaction.Debug_ImmediatelyLaunchCPA && budget == AIBudgetType.CPA )
                    {
                        this.TryToSpendBudget( Context, budget );
                        AttachedFaction.Debug_ImmediatelyLaunchCPA = false;
                        continue;
                    }
                    
                    //We can either spend budget whenever we hit the threshold (for things like Reinforcements),
                    //or at a time interval (things the player directly interacts with like CPAs or Waves).
                    if ( budget == AIBudgetType.Wave && this.BaseInfo.SentinelInfo.WavesTemporarilyDisabled )
                    {
                        //waves are temporarily disabled; this is intended for the tutorial
                        this.BaseInfo.SentinelInfo.NextEventTime[budget]++; //push the next wave to be sent into the future by 1 second each second
                    }
                    else 
                    if ( item.SpendOnThreshold || (budget == AIBudgetType.Wave && AttachedFaction.Debug_ImmediatelyLaunchAllWaves) )
                    {
                        debugStage = 900;
                        int threshold = this.BaseInfo.GetSpecificBudgetThreshold(  budget, GlobalAIWorldBaseInfo.Instance.AIProgress_Effective );
                        if ( this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budget] < threshold || this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budget] == 0 )
                        {
                            //Confusingly, sometimes the threshold can be 0 but we want to spend anyway (for example, if the Hunter Fleet isn't getting
                            //income from the base AI then the threshold can be 0, but there could be Hunter Fleet income from an Instigator base)
                            continue;
                        }
                        if ( tracing )
                        {
                            somethingToLog = true;
                            tracingBuffer.Add( "AI Spending " + this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budget] + " on  " + budget + " at  " + World_AIW2.Instance.GameSecond + " since we are over the spend threshold of " + threshold ).Add("\n");
                        }
                        this.TryToSpendBudget( Context, budget );
                    }
                    else
                    {
                        debugStage = 1000;
                        if ( this.BaseInfo.SentinelInfo.NextEventTime[budget] < 10 ) //ie if the value is uninitialized
                        {
                            //This is either a load of an old save game or the very beginning of the game
                            if ( World_AIW2.Instance.AIFactions.Count > 1 )
                            {
                                //if there are multiple AI factions, stagger the start times.
                                int myOffset = 0;
                                for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
                                {
                                    if ( World_AIW2.Instance.AIFactions[i] == AttachedFaction )
                                        myOffset = i;
                                }
                                int timeModifierForFaction = myOffset * item.SecondsBetweenAttemptsToSpend / World_AIW2.Instance.AIFactions.Count;
                                this.BaseInfo.SentinelInfo.NextEventTime[budget] = World_AIW2.Instance.GameSecond + item.SecondsBetweenAttemptsToSpend + timeModifierForFaction;

                                // If the setting is enabled, don't stagger the start time of waves..
                                // This means each AI sends their wave at once - this can be at different targets, or it can double up.
                                // This kind of stacking far as I (Puffin) know, doesn't occur much normally.
                                if ( budget == AIBudgetType.Wave && AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "AIsSyncWaves" ) )
                                {
                                    this.BaseInfo.SentinelInfo.NextEventTime[budget] = World_AIW2.Instance.GameSecond + item.SecondsBetweenAttemptsToSpend;
                                }

                                // If the setting is enabled, don't stagger the start time of CPAs.
                                // This means each AI sends their CPA at once, as opposed to a CPA at 2 hours, then a CPA at 3, then at 4, then at 5...
                                if ( budget == AIBudgetType.CPA && AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame ( "AIsSyncCPAs" ))
                                {
                                    this.BaseInfo.SentinelInfo.NextEventTime[budget] = World_AIW2.Instance.GameSecond + item.SecondsBetweenAttemptsToSpend;
                                }
                            }
                            else
                                this.BaseInfo.SentinelInfo.NextEventTime[budget] = World_AIW2.Instance.GameSecond + item.SecondsBetweenAttemptsToSpend;
                            if ( budget == AIBudgetType.Wave )
                                this.BaseInfo.SentinelInfo.PreviousWaveLength = item.SecondsBetweenAttemptsToSpend;
                            if ( tracing || tracing_wave )
                            {
                                somethingToLog = true;
                                tracingBuffer.Add( "AI initializing spend time for " + budget + " to " + this.BaseInfo.SentinelInfo.NextEventTime[budget] + "; it is currently " + World_AIW2.Instance.GameSecond + "\n" );
                            }
                            continue;
                        }
                        debugStage = 1100;
                        //                    tracingBuffer.Add ("AI next spend time for " + budget + " is " +this.BaseInfo.SentinelInfo.NextEventTime[budget] +  "; it is currently " + World_AIW2.Instance.GameSecond + "\n");
                        if ( this.BaseInfo.SentinelInfo.NextEventTime[budget] < World_AIW2.Instance.GameSecond )
                        {
                            int percentChanceToPushBack = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "PercentageChanceToDelayAIWavesAndCPAs" );
                            int percentAmountToPushBack = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "PercentageAmountToDelayAIWavesAndCPAs" );

                            if ( budget == AIBudgetType.BorderAggression )
                            {
                                percentChanceToPushBack = 50;
                                percentAmountToPushBack = 45;
                                int minBorderAggroSize = 1500;
                                if ( (int)this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.BorderAggression].IntValue < minBorderAggroSize ) //too small, don't bother sending
                                    this.BaseInfo.SentinelInfo.NextEventTime[budget] = World_AIW2.Instance.GameSecond + (item.SecondsBetweenAttemptsToSpend * percentAmountToPushBack) / 100;
                            }
                            if ( budget == AIBudgetType.Wave )
                            {
                                //some wave specific logic
                                int minWaveSize = 600;
                                if ( (int)this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.Wave].IntValue < minWaveSize )
                                {
                                    //don't send too weak of a wave
                                    this.BaseInfo.SentinelInfo.NextEventTime[budget] = World_AIW2.Instance.GameSecond + (item.SecondsBetweenAttemptsToSpend * percentAmountToPushBack) / 100;
                                    if ( tracing || tracing_wave )
                                    {
                                        somethingToLog = true;
                                        tracingBuffer.Add( "AI " + budget + "budget is " + (int)this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.Wave].IntValue + " < " + minWaveSize +". Delay for " + (this.BaseInfo.SentinelInfo.NextEventTime[budget] - World_AIW2.Instance.GameSecond) + " seconds. This is probably a low-intensity AI, perhaps during the civil war\n" );
                                    }

                                    continue;
                                }
                                if ( item.DoubleWaveIntervalEachTime )
                                    percentChanceToPushBack = 0; //don't mess with the budget further
                                bool directWavesOn = World_AIW2.Instance.Setup.GetBoolBySetting( "DirectWave" );
                                bool crossPlanetWavesOn = World_AIW2.Instance.Setup.GetBoolBySetting( "CrossPlanetWave" );
                                if ( !directWavesOn && !crossPlanetWavesOn )
                                {
                                    //early bailout if user requested no waves.
                                    if ( tracing_wave || tracing )
                                    {
                                        somethingToLog = true;
                                        this.BaseInfo.SentinelInfo.WavesTemporarilyDisabled = true;
                                        tracingBuffer.Add( "The user has requested no waves" ).Add( "\n" );
                                        continue;
                                    }
                                }
                            }

                            debugStage = 1200;
                            if ( Context.RandomToUse.Next( 1, 100 ) < percentChanceToPushBack || this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budget] == FInt.Zero )
                            {
                                //slight chance to push the event back a bit, just to make things feel a bit unpredictable. So a wave at a 10 minute interval
                                //might take 12 minutes (and be a bit bigger), or a CPA at every 2 hours might come 12 minutes later
                                //Note that if the budget is 0, don't bother trying to spend it so push it back
                                this.BaseInfo.SentinelInfo.NextEventTime[budget] = World_AIW2.Instance.GameSecond + (item.SecondsBetweenAttemptsToSpend * percentAmountToPushBack) / 100;
                                if ( tracing || tracing_wave ) { somethingToLog = true; tracingBuffer.Add( "AI randomly delaying spending " + budget + " to " + this.BaseInfo.SentinelInfo.NextEventTime[budget] + ". it is currently " + World_AIW2.Instance.GameSecond + "\n" ); }
                                continue;
                            }
                            if ( item.DoubleWaveIntervalEachTime && budget == AIBudgetType.Wave )
                            {
                                int WaveTimeMultiplier = 2;
                                if ( tracing || tracing_wave )
                                {
                                    somethingToLog = true;
                                    tracingBuffer.Add( "The previous wave interval was " + this.BaseInfo.SentinelInfo.PreviousWaveLength + " so next one should be " + (WaveTimeMultiplier * this.BaseInfo.SentinelInfo.PreviousWaveLength) );
                                }
                                this.BaseInfo.SentinelInfo.NextEventTime[budget] = World_AIW2.Instance.GameSecond + this.BaseInfo.SentinelInfo.PreviousWaveLength * WaveTimeMultiplier;
                                this.BaseInfo.SentinelInfo.PreviousWaveLength *= WaveTimeMultiplier;
                            }
                            else
                            {
                                if ( tracing || tracing_wave )
                                {
                                    somethingToLog = true;
                                    tracingBuffer.Add( "About to spend budget on " + budget + ". Next attempt to spend time calculated by  " + World_AIW2.Instance.GameSecond + " + " + item.SecondsBetweenAttemptsToSpend + "\n" );
                                }
                                this.BaseInfo.SentinelInfo.NextEventTime[budget] = World_AIW2.Instance.GameSecond + item.SecondsBetweenAttemptsToSpend; //usual case
                            }
                            if ( tracing || tracing_wave )
                            {
                                somethingToLog = true;
                                tracingBuffer.Add( "AI spending " + this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budget] + " on " + budget + " at " + World_AIW2.Instance.GameSecond + ". Next spend attempt at " + this.BaseInfo.SentinelInfo.NextEventTime[budget] + "\n " );
                            }
                            bool result = this.TryToSpendBudget( Context, budget );
                            if ( !result )
                            {
                                //we failed to spend this budget, so try again "soon"
                                //this is primarily for the Wormhole Invasion case, where if a wormhole invasion couldn't find a target it would
                                //wait 2 hours then spawn a double strength one
                                this.BaseInfo.SentinelInfo.NextEventTime[budget] = World_AIW2.Instance.GameSecond + 120;
                            }
                        }
                    }
                }
                #endregion

                debugStage = 2000;
                #region Wormhole Invasion (Exogalactic Wormhole)
                this.SendExogalacticWormholeInvasionIfNecessary( Context );
                #endregion

                #region CPA Bunkers
                this.SpawnCPABunkersIfNecessary( Context );
                #endregion

                
                #region check NeedsSpecialPerSecondLogic entries
                debugStage = 3000;
                //Execute the same logic for the AI Sentinels and all their sub-factions, but even for their entities the logic itself is still based in the main Sentinels faction!

                void runSpecialLogic(Faction faction)
                {
                    if (faction == null)
                        return;

                    foreach ( GameEntity_Squad entity in faction.Squads( EntityRollupType.NeedsSpecialPerSecondLogic ) )
                    {
                        //raid engines
                        if ( entity.TypeData.PeriodicSpawn_InitialDelay > 0 )
                            this.SendsWaveAfterBeingAlertedForXSeconds( Context, pathingCacheData, entity );

                    }
                }

                runSpecialLogic(AttachedFaction);
                debugStage = 3100;
                runSpecialLogic(BaseInfo.SubFac_Hunter);
                debugStage = 3200;
                runSpecialLogic(BaseInfo.SubFac_Warden);
                debugStage = 3300;
                runSpecialLogic(BaseInfo.SubFac_Praetorian);
                debugStage = 3400;
                runSpecialLogic(BaseInfo.SubFac_BorderAggression);
                debugStage = 3500;
                runSpecialLogic(BaseInfo.SubFac_CPA);
                debugStage = 3600;
                runSpecialLogic(BaseInfo.SubFac_RelentlessWave);

                #endregion
                debugStage = 4000;
                #region check Spire Relic Trains
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( EntityRollupType.IsTrain ) )
                {
                    debugStage = 4100;
                    GameEntity_Squad king = FactionUtilityMethods.Instance.findKing( AttachedFaction );
                    if ( king == null )
                        continue;
                    int RangeForUpgrade = 5000;
                    if ( king.Planet != entity.Planet )
                        continue;
                    debugStage = 4300;
                    if ( Mat.DistanceBetweenPointsImprecise( entity.WorldLocation, king.WorldLocation ) < RangeForUpgrade )
                    {
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            workingBuffer.Add( "The " ).Add( AttachedFaction.GetDisplayName(), AttachedFaction.FactionCenterColor.ColorHexBrighter )
                            .Add( " has received a relic from a transport. They will use it to build a single powerful ship to use against their enemies." );
                            World_AIW2.Instance.QueueChatMessageOrCommand( workingBuffer.GetStringAndResetForNextUpdate(), ChatType.LogToCentralChat, string.Empty, null );
                        }
                        //TODO: use the AI version of this game entity
                        GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "SpireBattleship" );
                        if ( entityData == null )
                            throw new Exception( "No valid XML is found for the spire unit to be spawned for the AI" );
                        PlanetFaction pFaction = king.Planet.GetPlanetFactionForFaction( AttachedFaction );

                        AttachedFaction.SpawnNewUnit_ReturnNullIfMPClient( Context, entity.Planet, entity.WorldLocation, entityData, AttachedFaction.CurrentGeneralMarkLevel,
                                pFaction.Faction.LooseFleet, 0, EntityBehaviorType.Attacker_Full, -1, null, "AISentinelsRelicTrainMkBattle" );

                        entity.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                    }
                }
                #endregion
                debugStage = 4400;
                this.CheckForExoGeneration( Context );
                debugStage = 4500;
                this.CheckForIncreasingGeneralMarkLevel( Context );
                debugStage = 4600;
                this.SyncGeneralMarkLevel();
                debugStage = 4700;
                this.ReactToPowerLevel_HostOnly( Context, pathingCacheData );
                debugStage = 4800;
                this.HandleWormholeBorers_MainSim( ref somethingToLog, Context );
                debugStage = 4900;
                this.HandleEnhancedOverlord( FactionUtilityMethods.Instance.findKing( AttachedFaction ), Context );
                debugStage = 5000;
                this.HandleCuendillarDrills_MainSim( Context, pathingCacheData );
                #region Tracing
                if ( tracing || tracing_wave ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " PerSecond trace ends" ).Add( " for " ).Add( this.BaseInfo.SentinelInfo.AIType.DisplayName ).Add( " idx " ).Add( AttachedFaction.FactionIndex );
                if ( (tracing || tracing_wave) && somethingToLog ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracing || tracing_wave )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "AI special faction error DoPerSecondLogic_Stage3Main, debug stage: " + debugStage + "     " + e.ToString(), Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();
            }
        }

        #region Enhanced Overlord
        public readonly List<SafeSquadWrapper> exoTargets = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 30, "AISentinelsFactionDeepInfo-exoTargets" );

        private void HandleEnhancedOverlord( GameEntity_Squad king, ArcenHostOnlySimContext Context )
        {
            if ( !AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "EnhancedOverlord" ) )
                return;
            if ( this.OverlordGuardPostFlushActive )
            {
                int maxToFlush = 15;
                bool anyRemaining = false;
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( EntityRollupType.ReinforcementLocations ) )
                {
                    if ( FactionUtilityMethods.Instance.TryDeployReinforcementContents( entity, Context ) )
                    {
                        if ( --maxToFlush <= 0 )
                        {
                            anyRemaining = true;
                            break;
                        }
                    }
                }
                if ( !anyRemaining )
                    this.OverlordGuardPostFlushActive = false;
            }
            if ( king == null || !king.TypeData.GetHasTag("AIOverlordPhase2_AnyType") )
                return;
            int wormholeInterval = 120;
            int waveBudget = (this.BaseInfo.GetSpecificBudgetThreshold(  AIBudgetType.Wave, GlobalAIWorldBaseInfo.Instance.AIProgress_Effective ) * 3) / 2;
            bool debug = false;
            if ( World_AIW2.Instance.GameSecond % wormholeInterval == 0 )
            {
                bool launched = false;
                int wormholeBudget = Context.RandomToUse.Next(waveBudget / 2, waveBudget * 2 );
                int retries = 10;
                do{
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("spawning wormhole invasion with budget " + wormholeBudget + " (wave budget " + waveBudget +") launched? " + launched, Verbosity.DoNotShow );
                    retries--;
                    WormholeInvasionOptions options = WormholeInvasionOptions.CreateWithDefaults( wormholeBudget, this.AttachedFaction );
                    wormholeBudget += 12 * 1000; //give a few retries with larger budgets, in case the player has heavily defended everything
                    options.WaveCount = 2;
                    options.WaveInterval = wormholeInterval / 2;
                    options.ProjectorAppearanceTime = World_AIW2.Instance.GameSecond + 10;
                    options.PlanetLinkTime = World_AIW2.Instance.GameSecond + 30;
                    launched = WormholeInvasionManager.LaunchWormholeInvasion( options, Context );
                }while ( !launched && retries-- > 0 );
                if ( !launched )
                {
                    //We didn't find a good target, so just pick a bad target
                    WormholeInvasionOptions options = WormholeInvasionOptions.CreateWithDefaults( wormholeBudget, this.AttachedFaction );
                    wormholeBudget += 12 * 1000; //give a few retries with larger budgets, in case the player has heavily defended everything
                    options.WaveCount = 2;
                    options.WaveInterval = wormholeInterval / 2;
                    options.ProjectorAppearanceTime = World_AIW2.Instance.GameSecond + 10;
                    options.PlanetLinkTime = World_AIW2.Instance.GameSecond + 30;
                    options.ForceInvasionLaunch = true;
                    WormholeInvasionManager.LaunchWormholeInvasion( options, Context );
                }
            }

            int exoInterval = 90;
            if ( World_AIW2.Instance.GameSecond % exoInterval == 0 )
            {
                exoTargets.Clear();
                FactionUtilityMethods.Instance.findAllHumanKings( exoTargets );
                int exoBudget = Context.RandomToUse.Next( waveBudget/2, waveBudget * 2);
                ExoOptions options = ExoOptions.CreateWithDefaults(exoTargets, waveBudget, AttachedFaction, AttachedFaction );
                options.ForceOrigin = king.Planet;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("spawning exo with budget " + waveBudget + " against " + exoTargets[0].ToStringWithPlanet(), Verbosity.DoNotShow );
                ExoGalacticAttackManager.SendExoGalacticAttack( options, Context );
            }
        }
        #endregion
        #region CheckForExoGeneration
        public readonly List<SafeSquadWrapper> exoGenerators = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 30, "AISentinelsFactionDeepInfo-exoGenerators" );
        private void CheckForExoGeneration( ArcenHostOnlySimContext Context )
        {
            //this once handled exo generation for major data centers, but is currently unused. Maybe something else can use it later?
            //. Exos for other reasons (fallen spire, risk analyzers, etc)
            //are handled elsewhere. We only want to track this on the ai faction with the highest difficulty
            int requiredDifficultyForExo = 999;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.BudgetSpend );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AISent-CheckForExoGeneration-trace", 10f ) : null;
            exoGenerators.Clear();

            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.GeneratesExosWhenPlayerOwned ) )
            {
                if ( entity.GetFactionTypeSafe() == FactionType.Player )
                {
                    exoGenerators.Add(entity);
                    if ( requiredDifficultyForExo > entity.TypeData.ExoGenerationDifficulty )
                        requiredDifficultyForExo = entity.TypeData.ExoGenerationDifficulty;
                }
            }

            if ( exoGenerators.Count <= 0 )
            {
                #region Tracing
                if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracingBuffer != null )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
                return;
            }

            int highestDifficulty = FactionUtilityMethods.Instance.GetHighestAIDifficulty();
            if ( highestDifficulty < requiredDifficultyForExo )
            {
                #region Tracing
                if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracingBuffer != null )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
                return;
            }
        }
        #endregion
        

        #region SendsWaveAfterBeingAlertedForXSeconds
        private void SendsWaveAfterBeingAlertedForXSeconds( ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData, GameEntity_Squad entity )
        {
            if ( !entity.TypeData.HasPeriodicSpawn )
                return;
            if ( entity.PeriodicSpawn_TimeUntilNextSpawn == 0 )
            {
                entity.PeriodicSpawn_TimeUntilNextSpawn = entity.TypeData.PeriodicSpawn_InitialDelay;
                return;
            }
            if ( entity.TypeData.PeriodicSpawn_MaxHopsToTrigger <= 0 && ( entity.Planet.SentinelsAlertLevel == null || entity.Planet.SentinelsAlertLevel.Ordinal < 4 ) ) //must be on alert level 4 or more, or it must be able to trigger for more distant enemies
                return;
            short hops = (short) entity.TypeData.PeriodicSpawn_MaxHopsToTrigger;
            int minStrength = entity.TypeData.PeriodicSpawn_MinHostileStrengthToTrigger;
            if (entity.HasStartedDoingPeriodicSpawns && entity.TypeData.PeriodicSpawn_NeverStopOnceTriggered )
            {
                hops = short.MaxValue;
                minStrength = 0;
            }
            PlanetFaction bestFaction = entity.PlanetFaction.GetMostAnnoyingPlanetFaction( hops, minStrength,
                entity.TypeData.PeriodicSpawn_OnlyTriggerOnOccupation, entity.TypeData.PeriodicSpawn_OnlyTriggerAgainstPlayer );
            if ( bestFaction == null )
                return;
            /*Already filtered for in GetIndexOfMostAnnoyingFaction()
            if ( !bestFaction.SpecialFactionData.AICanSendWavesAgainstThis)
                return; //this is something like the devourer or macrophage that shouldn't get waves sent against it
            */

            entity.PeriodicSpawn_TimeUntilNextSpawn--;
            if ( entity.PeriodicSpawn_TimeUntilNextSpawn > 0 )
                return;
            entity.PeriodicSpawn_TimeUntilNextSpawn = entity.TypeData.PeriodicSpawn_DelayBetweenSpawns;
            entity.HasStartedDoingPeriodicSpawns = true;
            if( entity.TypeData.PeriodicallySpawnsEvent )
            {
                if(entity.TypeData.PeriodicSpawn_CreatesWave)
                {
                    int waveBudget = (this.BaseInfo.GetSpecificBudgetThreshold( AIBudgetType.Wave, GlobalAIWorldBaseInfo.Instance.AIProgress_Effective ) * entity.TypeData.PeriodicSpawn_WaveOrExoSizeMultiplier).GetNearestIntPreferringHigher();
                    waveBudget = (this.BaseInfo.SentinelInfo.AIDifficulty.WaveBudgetMultiplier * waveBudget).GetNearestIntPreferringHigher();
                    ArcenDebugging.ArcenDebugLogSingleLine( entity.ToStringWithPlanetAndOwner() + " is sending a wave against " + bestFaction.Faction.GetDisplayName(), Verbosity.DoNotShow );
                    this.SendWave( Context, PathCacheData, waveBudget, null, null, bestFaction.Faction.FactionIndex, false, false );
                }
                
                if(entity.TypeData.PeriodicSpawn_CreatesExoStrike)
                {
                    //If possible spawn against a command station
                    GameEntity_Squad target = bestFaction.Planet.GetCommandStationOrNull();

                    //If none exist, try spawning against the strongest minor faction planetary controller
                    if ( target == null )
                    {
                        int bestStrength = 0;
                        int tmpStrenth;
                        foreach ( GameEntity_Squad squad in bestFaction.Entities.Squads( EntityRollupType.GrantsMinorFactionPlanetControl ) )
                        {
                            tmpStrenth = target.GetStrengthPerSquad();
                            if ( bestStrength < tmpStrenth )
                            {
                                bestStrength = tmpStrenth;
                                target = squad;
                            }
                        }
                    }

                    //If none exist, try spawning against the strongest enemy
                    if ( target == null )
                    {
                        int bestStrength = 0;
                        int tmpStrenth;
                        foreach ( GameEntity_Squad squad in bestFaction.Entities.Squads() )
                        {
                            tmpStrenth = target.GetStrengthPerSquad();
                            if ( bestStrength < tmpStrenth )
                            {
                                bestStrength = tmpStrenth;
                                target = squad;
                            }
                        }
                    }

                    //If still none exist, it must've been some kind of cross-threading issue. Don't exo.
                    if (target != null)
                    {
                        ExoOptions options = ExoOptions.CreateWithDefaults( target,
                            (BaseInfo.SentinelInfo.AIDifficulty.BaseHackingWaveSize * entity.TypeData.PeriodicSpawn_WaveOrExoSizeMultiplier).IntValue,
                            BaseInfo.AttachedFaction, bestFaction.Faction );
                        ArcenDebugging.ArcenDebugLogSingleLine( entity.ToStringWithPlanetAndOwner() + " is sending an exo strike against " + bestFaction.Faction.GetDisplayName(), Verbosity.DoNotShow );
                        ExoGalacticDeepLinkRoot.Instance.SendExoGalacticAttack( options, Context );
                    }
                }
                
                return;
            }
            
            Faction toSpawnFor = this.BaseInfo.GetAiSubFaction(entity.TypeData.Periodic_SpawnFactionForUnit);
            if ( toSpawnFor == null )
                return;

            var maxStacks = AIWar2GalaxySettingQuickAccess.StackingCutoffNPCs;
            var pfaction = entity.Planet.GetPlanetFactionForFaction( toSpawnFor );
            var bag = entity.TypeData.PeriodicSpawn_EntityTypeDrawingBag.Value;
            
            using (var spawned = StructList<EntityTypeAndCount>.Get())
            {
                bag.Draw( Context, toSpawnFor, entity.CurrentMarkLevel, spawned );
                
                for ( int i = 0; i < spawned.Count; i++ )
                {
                    var spawn = spawned[i];
                    var spawnTypeData = spawn.TypeData;
                    var stacksRemaining = spawn.Count;
                    int countOfEntities = Math.Max( Math.Min( maxStacks - pfaction.Entities.GetCountFromListOfEntitiesByEntityType( spawnTypeData ), stacksRemaining ), 1 );

                    GameEntity_Squad squad;
                    int currentStacks;
                    for ( int j = 0; j < countOfEntities; j++ )
                    {
                        if ( spawnTypeData.CannotBeStacked )
                            currentStacks = 1;
                        else
                            currentStacks = stacksRemaining / (countOfEntities - j);

                        stacksRemaining -= currentStacks;

                        squad = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pfaction, spawnTypeData, AttachedFaction.CurrentGeneralMarkLevel, AttachedFaction.LooseFleet, 0, entity.WorldLocation, Context, "AISentinelsSendWaveAfterBeingAlerted" );
                        squad.AddOrSetExtraStackedSquadsInThis( (short) (currentStacks - 1), true );
                        squad.Orders.SetBehaviorDirectlyInSim(EntityBehaviorType.Attacker_Full, bestFaction.Faction.FactionIndex);
                        //ArcenDebugging.ArcenDebugLogSingleLine( entity.ToStringWithPlanetAndOwner() + " is sending " + ( 1 + squad.ExtraStackedSquadsInThis ) + "x " + squad.TypeData + " against " + bestFaction.Faction.GetDisplayName(),
                        //    Verbosity.DoNotShow );
                    }
                }
            }
        }

        #endregion

        #region CheckForIncreasingGeneralMarkLevel
        private void CheckForIncreasingGeneralMarkLevel( ArcenHostOnlySimContext Context )
        {
            //first make sure the mark level is not less than 1
            if ( this.BaseInfo.SentinelInfo.AIDifficulty == null )
                throw new Exception("In CheckForIncreaseingGeneralMarkLevel, this.BaseInfo.SentinelInfo.AIDifficulty is null\n");
            if ( this.BaseInfo.SentinelInfo.AIDifficulty.AIPForMarkLevel == null )
                throw new Exception("In CheckForIncreaseingGeneralMarkLevel, this.BaseInfo.SentinelInfo.AIDifficulty.AIPForMarkLevel is null\n");
            if ( AttachedFaction.CurrentGeneralMarkLevel_Base < 1 )
                AttachedFaction.CurrentGeneralMarkLevel_Base = 1;
            //then go through and see if it can upgrade mark level based on the current AIP.  It can never downgrade!
            Balance_MarkLevel mark = null;
            for ( int i = 0; i < this.BaseInfo.SentinelInfo.AIDifficulty.AIPForMarkLevel.Count; i++ )
            {
                //Note: If adding a new Mark Level you need to update the AIDifficulty xml and the Balance_MarkTable xml
                if ( Balance_MarkLevelTable.Instance.Rows.Count > i )
                    mark = Balance_MarkLevelTable.Instance.Rows[i];
                else
                    continue;

                if ( GlobalAIWorldBaseInfo.Instance.AIProgress_Effective >= this.BaseInfo.SentinelInfo.AIDifficulty.AIPForMarkLevel[i] &&
                    AttachedFaction.CurrentGeneralMarkLevel_Base < mark.Ordinal )
                {
                    AttachedFaction.CurrentGeneralMarkLevel_Base = mark.Ordinal;

                    if ( ArcenNetworkAuthority.GetIsHostMode() )
                        World_AIW2.Instance.QueueChatMessageOrCommand( "The general mark level of <color=#" + AttachedFaction.FactionCenterColor.ColorHexBrighter + ">" + 
                            AttachedFaction.GetDisplayName() + "</color> has increased to " + mark.Ordinal + " because of AI Progress.", ChatType.LogToCentralChat, string.Empty, null );
                }
            }

            if ( this.BaseInfo.SentinelInfo.AIDifficulty.Difficulty >= 10 )
            {
                if ( AttachedFaction.CurrentGeneralMarkLevel_Base < 2 )
                {
                    foreach ( Planet plan in World_AIW2.Instance.CurrentGalaxy.Planets( false ) )
                    {
                        if ( plan == null || plan.MarkLevelForAIOnly == null || plan.MarkLevelForAIOnly.Ordinal < 7 ) //this would be an AIBastionWorld, but we can just check mark 7 for working better with old saves
                        continue;
                        if ( plan.GetControllingFaction() == AttachedFaction )
                        {
                            PlanetFaction pFac = plan.GetPlanetFactionForFaction( AttachedFaction );
                            if ( pFac != null )
                            {
                                if ( pFac.DataByStance[FactionStance.Hostile].TotalPlayerStrengthNotInTransports > 10000 ) //10 strength, not in transports
                            {
                                    AttachedFaction.CurrentGeneralMarkLevel_Base = 2;
                                    if ( ArcenNetworkAuthority.GetIsHostMode() )
                                        World_AIW2.Instance.QueueChatMessageOrCommand( "The general mark level of <color=#" + AttachedFaction.FactionCenterColor.ColorHexBrighter + ">" +
                                            AttachedFaction.GetDisplayName() + "</color> has increased to " + 2 + " because of human aggression on their mark 7 worlds.", ChatType.LogToCentralChat, string.Empty, null );
                                    continue;
                                }
                            }
                        }
                    }
                }
                if ( AttachedFaction.CurrentGeneralMarkLevel_Base < 3 )
                {
                    foreach ( Planet plan in World_AIW2.Instance.CurrentGalaxy.Planets( false ) )
                    {
                        if ( plan == null || plan.PopulationType != PlanetPopulationType.AIHomeworld ) //AIBastionWorld ignore
                        continue;
                        if ( plan.GetControllingFaction() == AttachedFaction )
                        {
                            PlanetFaction pFac = plan.GetPlanetFactionForFaction( AttachedFaction );
                            if ( pFac != null )
                            {
                                if ( pFac.DataByStance[FactionStance.Hostile].TotalPlayerStrengthNotInTransports > 10000 ) //10 strength, not in transports
                            {
                                    AttachedFaction.CurrentGeneralMarkLevel_Base = 3;
                                    if ( ArcenNetworkAuthority.GetIsHostMode() )
                                        World_AIW2.Instance.QueueChatMessageOrCommand( "The general mark level of <color=#" + AttachedFaction.FactionCenterColor.ColorHexBrighter + ">" +
                                            AttachedFaction.GetDisplayName() + "</color> has increased to " + 3 + " because of human aggression at their homeworld.", ChatType.LogToCentralChat, string.Empty, null );
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region SyncGeneralMarkLevel
        private void SyncGeneralMarkLevel()
        {
            //This version syncs all factions that are supposed to take the AIs faction general mark level
            Faction otherFaction;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null || otherFaction == AttachedFaction )
                    continue;
                //children of myself
                if ( otherFaction.FactionIndexOfMyParentIfIHaveOne == this.AttachedFaction.FactionIndex )
                {
                    if ( otherFaction.CurrentGeneralMarkLevel_Base < AttachedFaction.CurrentGeneralMarkLevel_Base )
                        otherFaction.CurrentGeneralMarkLevel_Base = AttachedFaction.CurrentGeneralMarkLevel_Base;
                    continue;
                }
                switch ( otherFaction.Type )
                {
                    //Stop syncing the AI mark level between AIs; we want to allow low difficulty AIs to have different mark levels from high difficulty AIs
                    // case FactionType.AI:
                    //     break;
                    case FactionType.SpecialFaction:
                        if ( otherFaction.SpecialFactionData.TakesOnHighestCurrentGeneralMarkLevelOfAnyAIFaction )
                        {
                            if ( otherFaction.CurrentGeneralMarkLevel_Base < AttachedFaction.CurrentGeneralMarkLevel_Base )
                                otherFaction.CurrentGeneralMarkLevel_Base = AttachedFaction.CurrentGeneralMarkLevel_Base;
                        }
                        break;
                }
            }
        }
        #endregion

        private readonly List<Faction> lrpFactionGuards = List<Faction>.Create_WillNeverBeGCed( 20, "AISentinelsFactionDeepInfo-lrpFactionGuards" );
        private readonly List<SafeSquadWrapper> lrpGuardUnits = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 60, "AISentinelsFactionDeepInfo-lrpGuardUnits" );

        private readonly List<GameCommand> pendingSetWaitCommands = List<GameCommand>.Create_WillNeverBeGCed( 5, "AISentinelsFactionDeepInfo-pendingSetWaitCommands" );
        private readonly List<SafeSquadWrapper> ShipsInExogalacticAttacks = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 60, "AISentinelsFactionDeepInfo-ShipsInExogalacticAttacks" );
        private readonly List<SafeSquadWrapper> RelicTrains = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 5, "AISentinelsFactionDeepInfo-RelicTrains" );
        private readonly List<SafeSquadWrapper> EnemyKingAttackers = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 60, "AISentinelsFactionDeepInfo-EnemyKingAttackers" );
        private readonly List<SafeSquadWrapper> WormholeBorers = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 7, "AISentinelsFactionDeepInfo-WormholeBorers" );
        private readonly List<SafeSquadWrapper> CuendillarTransports = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 7, "AISentinelsFactionDeepInfo-CuendillarTransports" );
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            //Overview (updated 4/26/19)
            //First we check for whether ships that are en route should Wait
            //  The AI will do this if it feels it's too Dangerous to go somewhere
            //  The classic case here is "We were going to Murdoch, but since we started the player has reinforced it"
            //
            //If we are "WaitingAgainst" a planet, we were too weak to go there last time
            //     If we have been waiting long enough to go join the hunter fleet, do that unless I hate a non-human faction
            //  if we are strong enough to attack we unset desiredWaitIndexValue
            //if we are not "waiting against" a planet
            //  Check if the next planet we are going to is too Dangerous now
            //     If so, then we start waiting

            // Then we look at Threat Routing
            //  Threat ships on each planet that aren't currently busy ( "en route", in an exo, etc) are added to the local ThreatChunk (split based on the minor faction we are hostile to)
            //  Handle the case of Guarding ship combat (do we fight? become threat?)

            //  Then determine what to do with Threat (retreat? Find a new target?) Note that the Attack code path sometimes
            // starts off with the units Waiting, if the next target is potentially scary and needs deeper analysis
            int debugStage = 0;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                AITypeData aiType = this.BaseInfo.SentinelInfo.AIType;
                AIDifficulty difficulty = this.BaseInfo.SentinelInfo.AIDifficulty;
                pendingSetWaitCommands.Clear();
                ShipsInExogalacticAttacks.Clear();
                RelicTrains.Clear();
                EnemyKingAttackers.Clear();
                WormholeBorers.Clear();
                CuendillarTransports.Clear();

                GameCommand command = null;
                debugStage = 100;
                //if the player has won the game, the AI doesn't get to do much anymore
                if ( World.Instance.ConclusionType == CampaignConclusionType.Won )
                    return;

                /* If the AI homeworld is under attack, all threat ships immediately route there */
                bool hasKingGoneMobile = false;
                Planet KingPlanet = GetKingPlanet( out hasKingGoneMobile );
                bool KingUnderAttack = GetIsKingUnderAttack( KingPlanet ) || hasKingGoneMobile;

                #region check for telling en-route threat ships to start or stop waiting
                {
                    #region Tracing
                    ArcenCharacterBuffer tracingBuffer = null;
                    #endregion
                    GameCommand pendingTransferToHunterFleetCommand = null;
                    GameCommand pendingTransferToPraetorianGuardCommand = null;
                    GameCommand goBackToGuardingCommand = null; //sometimes threat can just become guards again
                    bool foundNoHunterFleet = false;
                    bool foundNoPraetorianGuard = false;
                    foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                    {
                        debugStage = 1010;
                        if ( entity == null )
                            continue;
                        debugStage = 10111;
                        if ( entity.TypeData.ShouldNotBeGivenNormalOrdersByAILogic )
                        {
                            if ( entity.TypeData.ShouldAlwaysBeGivenOrdersToAttackPlayerKing )
                                EnemyKingAttackers.Add( entity );
                            continue; //units not to tell what to do
                        }
                        debugStage = 1012;
                        if ( !entity.TypeData.IsMobile )
                            continue; //only for mobile units
                        debugStage = 1013;
                        if ( entity.GetFactionOrNull_Safe() != AttachedFaction ) //this shouldn't really be possible
                            continue;
                        debugStage = 1015;
                        if ( entity.TypeData.GetHasTag( "MobileWormholeBorer" ) )
                        {
                            WormholeBorers.Add( entity );
                            continue; //wormhole borers do their movement and processing differently than most AI ships
                        }
                        if ( entity.TypeData.GetHasTag( "AICuendillarTransport" ) )
                        {
                            CuendillarTransports.Add( entity );
                            continue; //Cuendillar Transports have unique movement
                        }

                        if ( entity.TypeData.GetHasTag( "SpireRelicTrain" ) )
                        {
                            RelicTrains.Add( entity );
                            continue; //relic trains do their movement and processing differently than most AI ships
                        }
                        if ( entity.ExoGalacticAttackTarget.GetSquad() != null )
                        {
                            ShipsInExogalacticAttacks.Add( entity );
                            continue; //this unit is in an exogalactic attack, so ignore it here and process it later
                        }
                        if ( entity.CalculateNextHopPlanetIndex_Safe() == entity.GetPlanetIndexSafe() )
                        {
                            continue; //This ship is going here already?
                        }
                        #region Tracing
                        bool tracing = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ThreatWaiting ) && entity.GetPlanetIndexSafe() == Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed();
                        if ( tracing && tracingBuffer == null)
                        {
                            // Note that this buffer is logged and freed outside the foreach loop.
                            // Thus, we only want to create it and log the inital message once.
                            tracingBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "AISent-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f );
                            tracingBuffer.Add( "Threat-Waiting logic considering situation on Planet " ).Add( entity.Planet.Name );
                        }
                        debugStage = 102;
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "considering unit " ).Add( entity.PrimaryKeyID ).Add( " (" ).Add( entity.TypeData.InternalName ).Add( ")" );
                        #endregion


                        #region ThreatGoesBackToSleep
                        Faction targetFaction = World_AIW2.Instance.GetFactionByIndex( entity.Orders.BehaviorRelatedFactionIndex );
                        if ( targetFaction != null &&
                             entity.Orders.Behavior == EntityBehaviorType.Attacker_Full &&
                             (targetFaction.SpecialFactionData.AIThreatAgainstThisFactionGoesBackToGuarding &&
                              entity.Planet.GetDataByStanceForFaction( targetFaction, FactionStance.Self ).TotalStrength == 0) ||
                            AttachedFaction.GetIsFriendlyTowards( targetFaction ) )
                        {
                            /* Some units need to go back to Guarding that had been threat. There are two classes for this.
                               First is if we are now allied to that faction, for example threat against
                               the Dyson needs to just chill out if there is a Dyson Antagonizer.
                               Secondly, Some minor factions shouldn't be allowed to generate Threat that stays Threat; it causes balance problems
                               and is frustrating to the player. The best example of this is the anti-AI zombies that wander into the AI's planets
                               and trigger threat that attacks player planets (if those planets have zombies on them).

                               Note this logic is duplicated below; look for places where we check the entity.Orders.BehaviorRelatedFactionIndex.
                               If you change one place, please change both
                            */
                            if ( goBackToGuardingCommand == null )
                            {
                                goBackToGuardingCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetBehavior_FromFaction_ThreatGoesBackToSleep], GameCommandSource.AnythingElse );
                                goBackToGuardingCommand.RelatedMagnitude = (int)EntityBehaviorType.Guard_Guardian_Anchored;
                            }
                            goBackToGuardingCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                            if ( tracing ) tracingBuffer.Add( "\n\tis going back to guarding path 2" );
                            continue;
                        }

                        if ( entity.CalculateNextHopPlanetIndex_Safe() < 0 )
                            continue; //threat ships will generally have a next hop set

                        #endregion
                        //if have existed long enough in threatfleet, then try to join the hunter fleet
                        int secondsIHaveBeenThreatfleet = entity.BecameThreatfleetAtGameSecond <= 0 ? 0 : World_AIW2.Instance.GameSecond - entity.BecameThreatfleetAtGameSecond;
                        int hunterFleetExistsThreshold = this.BaseInfo.SentinelInfo.AIDifficulty.SecondsThreatExistsAsThreatBeforeJoiningHunterFleet;
                        if ( secondsIHaveBeenThreatfleet >= hunterFleetExistsThreshold )
                        {
                            if ( Helper_ConsiderJoiningTheHunterFleetFromThreatfleet( entity, targetFaction, ref pendingTransferToHunterFleetCommand,
                                ref foundNoHunterFleet, tracing, tracingBuffer, Context ) )
                                continue;
                        }

                        int desiredWaitIndexValue;

                        if ( entity.WaitingAgainstPlanetIndex >= 0 )
                        {
                            //We are currently waiting against a planet (since it was too scary last time we looked).
                            //Evaluate if we should keep waiting
                            if ( entity.CalculateNextHopHasARefuseToWaitOrder_Safe() )
                            {
                                #region Tracing
                                if ( tracing ) tracingBuffer.Add( "\n\t***Accepting stop-wait proposal due to having a refuse-to-wait order (the stop-wait proposal means we're not waiting anymore, we're going to attack)" );
                                #endregion
                                desiredWaitIndexValue = -1;
                            }
                            else
                            {
                                debugStage = 103;
                                Int16 targetIndex = entity.WaitingAgainstPlanetIndex;
                                Planet targetPlanet = World_AIW2.Instance.CurrentGalaxy.GetPlanetByIndex( targetIndex );
                                if ( targetPlanet == null )
                                    continue;
                                #region Tracing
                                if ( tracing ) tracingBuffer.Add( " waiting against planet " ).Add( targetPlanet.Name ).Add( ". It's our current target, but last time we checked it was too strong for us. Checking again now." ).Add( "\n" );
                                #endregion
                                if ( targetPlanet.IsPlanetToBeDestroyed || targetPlanet.HasPlanetBeenDestroyed )
                                {
                                    if ( pendingTransferToHunterFleetCommand == null )
                                    {
                                        Faction hunter = this.BaseInfo.SubFac_Hunter;
                                        if ( hunter != null )
                                        {
                                            pendingTransferToHunterFleetCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransferEntitiesToFaction], GameCommandSource.AnythingElse );
                                            pendingTransferToHunterFleetCommand.RelatedFactionIndex = hunter.FactionIndex;
                                        }
                                    }
                                    if ( pendingTransferToHunterFleetCommand != null )
                                    {
                                        #region Tracing
                                        if ( tracing ) tracingBuffer.Add( "\n\t***The planet I wanted to attack has been destroyed; join the hunter fleet" );
                                        #endregion
                                        pendingTransferToHunterFleetCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                                    }
                                    continue;
                                }
                                int currentLevel, threshold;
                                //CurrentLevel = Current AI forces attacking or considering attacking a planet
                                //threshold = Enemy Strength * RequiredStrRationToAttack
                                AnalyzeFriendlyToHostileBalance( tracing, tracingBuffer, targetPlanet, out currentLevel, out threshold );
                                if ( currentLevel <= threshold )
                                {
                                    debugStage = 104;
                                    int secondsIHaveBeenWaiting = World_AIW2.Instance.GameSecond - Math.Max( entity.StartedWaitingAtGameSecond, entity.StartedWaitingAtGameSecond );
                                    int hunterFleetWaitThreshold = this.BaseInfo.SentinelInfo.AIDifficulty.SecondsThreatWaitsBeforeJoiningHunterFleet;

                                    #region Tracing
                                    if ( tracing ) tracingBuffer.Add( "\n\t***Rejecting stop-wait proposal (aka we are too weak and must keep waiting) due to " ).Add( currentLevel ).Add( " <= " ).Add( threshold ).Add( " I have been waiting " + secondsIHaveBeenWaiting + " and the hunter fleet threshold is " + hunterFleetWaitThreshold );
                                    #endregion
                                    if ( KingUnderAttack &&
                                         !entity.TypeData.IsDrone )
                                    {
                                        //The king is under attack; all threat joins the Praetorian Guard
                                        debugStage = 105;
                                        #region Tracing
                                        if ( tracing ) tracingBuffer.Add( "\n\t***The king is under attack; go join the Praetorian Guard." );
                                        #endregion
                                        if ( pendingTransferToPraetorianGuardCommand == null && !foundNoPraetorianGuard )
                                        {
                                            lrpFactionGuards.Clear();
                                            for ( Int16 factionIndex = 0; factionIndex < World_AIW2.Instance.Factions.Count; factionIndex++ )
                                            {
                                                debugStage = 1051;
                                                Faction otherFaction = World_AIW2.Instance.Factions[factionIndex];
                                                if ( !otherFaction.GetIsFriendlyTowards( AttachedFaction ) )
                                                    continue;
                                                debugStage = 1052;
                                                if ( otherFaction.SpecialFactionData.InternalName != "PraetorianGuard" )
                                                    continue;
                                                debugStage = 1053;
                                                var otherFactionExternal = otherFaction.BaseInfo;
                                                debugStage = 1054;
                                                if ( otherFaction.FactionIndexOfMyParentIfIHaveOne == AttachedFaction.FactionIndex )
                                                    lrpFactionGuards.Add( otherFaction ); //note that an AI can have multiple hunters, if other AIs have died
                                            }
                                            if ( lrpFactionGuards.Count <= 0 )
                                                foundNoPraetorianGuard = true;
                                            else
                                            {
                                                debugStage = 1058;
                                                pendingTransferToPraetorianGuardCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransferEntitiesToFaction], GameCommandSource.AnythingElse );
                                                pendingTransferToPraetorianGuardCommand.RelatedFactionIndex = lrpFactionGuards[Context.RandomToUse.Next( 0, lrpFactionGuards.Count )].FactionIndex;
                                            }
                                        }
                                        if ( pendingTransferToPraetorianGuardCommand != null )
                                        {
                                            debugStage = 1059;
                                            pendingTransferToPraetorianGuardCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                                        }
                                        else
                                        {
                                            #region Tracing
                                            if ( tracing ) tracingBuffer.Add( "\n\t***Cannot find the Praetorian Guard" );
                                            #endregion
                                        }
                                    }
                                    else if ( secondsIHaveBeenWaiting >= hunterFleetWaitThreshold )
                                    {
                                        //if have been waiting at one spot for long enough while threatfleet, then join the hunter fleet
                                        Helper_ConsiderJoiningTheHunterFleetFromThreatfleet( entity, targetFaction, ref pendingTransferToHunterFleetCommand,
                                            ref foundNoHunterFleet, tracing, tracingBuffer, Context );
                                    }
                                    else
                                    {
                                        #region Tracing
                                        if ( tracing ) tracingBuffer.Add( "\n\t***I've been waiting " ).Add( secondsIHaveBeenWaiting ).Add( ", not long enough to transfer to the Hunter Fleet (" ).Add( hunterFleetWaitThreshold ).Add( ")" );
                                        #endregion
                                    }
                                    continue;
                                }
                                #region Tracing
                                if ( tracing ) tracingBuffer.Add( "\n\t***This unit is going to stop waiting and attack  due to " ).Add( currentLevel ).Add( " > " ).Add( threshold );
                                #endregion
                                desiredWaitIndexValue = -1;
                            }
                        }
                        else
                        {
                            //We are currently moving. See if the next planet on our list is too scary (ie do we accept a start-wait proposal)
                            debugStage = 1070;
                            if ( entity.CalculateNextHopHasARefuseToWaitOrder_Safe() )
                            {
                                #region Tracing
                                if ( tracing ) tracingBuffer.Add( "\n\t***Refusing start-wait proposal due to having a refuse-to-wait order" );
                                #endregion
                                continue;
                            }
                            debugStage = 1071;
                            int targetIndex = entity.CalculateNextHopPlanetIndex_Safe();
                            if ( targetIndex == -1 )
                            {
                                #region Tracing
                                if ( tracing ) tracingBuffer.Add( "\n\t***apparently has no NextHopPlanetIndex set?" );
                                #endregion
                                continue;
                            }
                            debugStage = 1072;
                            Planet targetPlanet = World_AIW2.Instance.CurrentGalaxy.GetPlanetByIndex( entity.CalculateNextHopPlanetIndex_Safe() );
                            if ( targetPlanet == null )
                                continue;
                            debugStage = 1073;
                            #region Tracing
                            if ( tracing ) tracingBuffer.Add( " considering waiting against planet " ).Add( targetPlanet.Name );
                            #endregion
                            int currentLevel, threshold;
                            AnalyzeFriendlyToHostileBalance( tracing, tracingBuffer, targetPlanet, out currentLevel, out threshold );
                            if ( currentLevel >= threshold )
                            {
                                #region Tracing
                                if ( tracing ) tracingBuffer.Add( "\n\t***Rejecting start-wait proposal due to my strength " ).Add( currentLevel ).Add( " >= enemy strength " ).Add( threshold );
                                #endregion
                                continue;
                            }
                            else if ( KingUnderAttack )
                            {
                                #region Tracing
                                if ( tracing ) tracingBuffer.Add( "\n\t***Rejecting start-wait proposal because my king is under attack" );
                                #endregion
                                continue;
                            }
                            #region Tracing
                            if ( tracing ) tracingBuffer.Add( "\n\t***Accepting start-wait proposal due to " ).Add( currentLevel ).Add( " < " ).Add( threshold ).Add( "\n" );
                            #endregion
                            desiredWaitIndexValue = targetIndex;
                        }
                        debugStage = 108;
                        for ( int j = 0; j < pendingSetWaitCommands.Count; j++ )
                        {
                            GameCommand existingCommand = pendingSetWaitCommands[j];
                            if ( existingCommand.RelatedIntegers == null )
                                continue;
                            if ( existingCommand.RelatedIntegers.First != desiredWaitIndexValue )
                                continue;
                            command = existingCommand;
                        }
                        if ( command == null )
                        {
                            command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWaiting], GameCommandSource.AnythingElse );
                            command.RelatedIntegers.Add( desiredWaitIndexValue );
                            pendingSetWaitCommands.Add( command );
                        }
                        command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                    }
                    debugStage = 109;
                    for ( int i = 0; i < pendingSetWaitCommands.Count; i++ )
                    {
                        if ( pendingSetWaitCommands[i].RelatedEntityIDs.Count > 0 )
                            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, pendingSetWaitCommands[i], false );
                    }
                    if ( pendingTransferToHunterFleetCommand != null )
                    {
                        if ( tracingBuffer != null ) tracingBuffer.Add( " donating " ).Add( pendingTransferToHunterFleetCommand.RelatedEntityIDs.Count ).Add( " units from generic AI threat to Hunter fleet" );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, pendingTransferToHunterFleetCommand, false );
                    }
                    if ( pendingTransferToPraetorianGuardCommand != null )
                    {
                        if ( tracingBuffer != null ) tracingBuffer.Add( " donating " ).Add( pendingTransferToPraetorianGuardCommand.RelatedEntityIDs.Count ).Add( " units from generic AI threat to Praetorian Guard" );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, pendingTransferToPraetorianGuardCommand, false );
                    }
                    if ( goBackToGuardingCommand != null )
                    {
                        if ( tracingBuffer != null ) tracingBuffer.Add( " Ships going back to guarding: " ).Add( goBackToGuardingCommand.RelatedEntityIDs.Count ).Add( "; threat is too much work. Right now this means it was threat against anti-ai zombies or something" );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, goBackToGuardingCommand, false );
                    }

                    #region Tracing
                    if ( tracingBuffer != null ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    if ( tracingBuffer != null )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                }
                #endregion

                #region check for routing non-en-route threat ships, and freeing guard ships under overwhelming assault
                debugStage = 200;
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    /* Nota Bene: This code doesn't look at the ships Behaviour, but we do check for ship Behaviour of Attacker_Full when
                       calculating Threat elsewhere. Badger is not quite sure what all the Behaviours are right now.  */
                    #region Tracing
                    bool tracing = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ThreatRouting ) && planet == Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                    ArcenCharacterBuffer tracingBuffer = null;
                    if ( tracing ) tracingBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "AISent-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-InPlanets-trace", 10f );
                    if ( tracing ) tracingBuffer.Add( "Threat Traffic Control considering situation on Planet " ).Add( planet.Name );
                    #endregion
                    var factionData = planet.GetStanceDataForFaction( AttachedFaction );
                    StrengthData_PlanetFaction_Stance myStrengthData = factionData[FactionStance.Self];
                    StrengthData_PlanetFaction_Stance friendlyStrengthData = factionData[FactionStance.Friendly];
                    StrengthData_PlanetFaction_Stance hostileStrengthData = factionData[FactionStance.Hostile];
                    int friendlyStrengthTotal = myStrengthData.TotalStrength + friendlyStrengthData.TotalStrength;
                    int friendlyStrengthGuard = myStrengthData.GuardStrength + friendlyStrengthData.GuardStrength;
                    int friendlyStrengthThreat = myStrengthData.ThreatStrength + friendlyStrengthData.ThreatStrength;
                    int friendlyStrengthWaiting = myStrengthData.WaitingStrength + friendlyStrengthData.WaitingStrength;
                    int friendlyStrengthIncoming = myStrengthData.IncomingStrength + friendlyStrengthData.IncomingStrength;
                    int hostileStrengthTotal = hostileStrengthData.TotalStrength;
                    int hostileStrengthIncludingNonMilitary = hostileStrengthData.TotalStrengthIncludingNonMilitary;
                    int threatProvokingHostileStrengthTotal = hostileStrengthData.TotalThreatProvokingStrength;
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "FactionIndex:" ).Add( AttachedFaction.FactionIndex );
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "MyDesireToRaidThisPlanet:" ).Add( this.BaseInfo.GetRaidDesirability( planet ) );
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "RaidDifficulty:" ).Add( this.BaseInfo.GetRaidTraversalDifficulty( planet ) );
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Local Infestation Strength:" ).Add( hostileStrengthTotal );
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Local Friendly Guard Strength:" ).Add( friendlyStrengthGuard );
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Local Friendly Threat Strength:" ).Add( friendlyStrengthThreat );
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Incoming Friendly Strength One Hop Away:" ).Add( friendlyStrengthIncoming );
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Friendly Strength Waiting Against This Planet:" ).Add( friendlyStrengthWaiting );
                    #endregion
                    ThreatChunkCollection threatChunks = ThreatChunkCollection.GetFromPoolOrCreate();
                    try
                    {
                        Int64 totalUnassignedThreatShipTimeOnPlanet = 0;
                        lrpGuardUnits.Clear();
                        PlanetFaction planetFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                        int totalThreatFound = 0; //for debug logging
                        debugStage = 201;
                        foreach ( GameEntity_Squad entity in planetFaction.Entities.Squads( EntityRollupType.MobileCombatants ) )
                        {
                            debugStage = 202;
                            #region Tracing
                            string debugPlanetName = "Cinna";
                            bool innerTracing = tracing && (entity == GameEntity_Base.CurrentlyHoveredOver || planet.Name == debugPlanetName);
                            //bool innerTracing = tracing;
                            if ( innerTracing ) tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "Checking if threat:" ).Add( entity.ToString() ).Add( ")" );
                            #endregion
                            debugStage = 203;
                            if ( entity.TypeData.ShouldNotBeGivenNormalOrdersByAILogic )
                                continue; //units not to tell what to do
                            debugStage = 204;
                            GameEntity_Squad guarded = entity.GuardedUnit.GetSquad();
                            if ( entity.Orders.Behavior == EntityBehaviorType.Guard_Guardian_Patrolling )
                            {
                                #region Tracing
                                if ( innerTracing )
                                {
                                    tracingBuffer.Add( ":" ).Add( "counting as guard because it has patrolling guard behavior" );
                                }
                                #endregion
                                lrpGuardUnits.Add( entity );
                            }
                            else if ( guarded != null )
                            {
                                #region Tracing
                                if ( innerTracing )
                                {
                                    tracingBuffer.Add( ":" ).Add( "counting as guard because guarding #" ).Add( guarded?.PrimaryKeyID ?? -1 ).Add( " (" ).Add( guarded?.TypeData.InternalName ?? "null" ).Add( ")" );
                                }
                                #endregion
                                lrpGuardUnits.Add( entity );
                            }
                            else
                            {
                                //not a guarding ship, so can take orders to fight, or other things
                                debugStage = 205;
                                if ( entity.GetPlanetIndexSafe() != planet.Index )
                                {
                                    #region Tracing
                                    if ( innerTracing ) tracingBuffer.Add( ":" ).Add( "skipping because was not on this planet at the beginning of this planning cycle" );
                                    #endregion
                                    continue;
                                }
                                if ( entity.TypeData.GetHasTag( "SpireRelicTrain" ) )
                                {
                                    #region Tracing
                                    if ( innerTracing ) tracingBuffer.Add( ":" ).Add( "skipping because is a relic train" );
                                    #endregion

                                    continue; //relic trains do their movement and processing differently than most AI ships
                                }
                                if ( entity.TypeData.GetHasTag( "MobileWormholeBorer" ) )
                                {
                                    #region Tracing
                                    if ( innerTracing ) tracingBuffer.Add( ":" ).Add( "skipping because is a wormhole borer" );
                                    #endregion

                                    continue; //borers do their own thing
                                }
                                if ( entity.TypeData.GetHasTag( "AICuendillarTransport" ) )
                                {
                                    #region Tracing
                                    if ( innerTracing ) tracingBuffer.Add( ":" ).Add( "skipping because is a cuendillar transport" );
                                    #endregion

                                    continue; //borers do their own thing
                                }

                                if ( entity.ExoGalacticAttackTarget.GetSquad() != null )
                                {
                                    #region Tracing
                                    if ( innerTracing ) tracingBuffer.Add( ":" ).Add( "skipping because is part of an Exogalactic Attack against " + entity.ExoGalacticAttackTarget.GetSquad().ToStringWithPlanetAndOwner() );
                                    #endregion
                                    continue; //this unit is in an exogalactic attack, so ignore it for now
                                }

                                debugStage = 206;
                                if ( entity.CalculateFinalDestinationPlanetIndex_Safe() != -1 &&
                                     entity.CalculateFinalDestinationPlanetIndex_Safe() != planet.Index &&
                                     entity.WaitingAgainstPlanetIndex == -1 ) // Only exclude ships that are in motion
                                {
                                    //we are going to another planet; but lets make sure we don't have a forcefield blocking us on the way!
                                    bool foundBlockingShield = false;
                                    if ( hostileStrengthTotal > friendlyStrengthTotal / 2 && difficulty.Difficulty >= 7 )
                                    {
                                        //only do this check if we have significant enemy forces on the planet (since there might be a ton of units to check, and it might cause game slowdown)
                                        foundBlockingShield = FactionUtilityMethods.Instance.IsShieldBlockingWormholeToPlanet( entity.Planet, World_AIW2.Instance.GetPlanetByIndex( entity.CalculateNextHopPlanetIndex_Safe() ), AttachedFaction );
                                        if ( foundBlockingShield && friendlyStrengthTotal > hostileStrengthTotal )
                                            foundBlockingShield = false; //if we are winning this battle, we want to stay and blow up the forcefield
                                    }
                                    if ( !foundBlockingShield )
                                    {
                                        #region Tracing
                                        if ( innerTracing ) tracingBuffer.Add( ":" ).Add( "skipping because heading to a different planet" );
                                        #endregion
                                        continue;
                                    }
                                    else
                                    {
                                        //we are outnumbered and found a forcefield blocking the wormhole we were hoping to use to escape... go back into "find something to do" logic
                                        //and maybe we can find a better planet to escape to
                                    }
                                }
                                Faction targetFaction = World_AIW2.Instance.GetFactionByIndex( entity.Orders.BehaviorRelatedFactionIndex );
                                if ( targetFaction != null &&
                                     (targetFaction.SpecialFactionData.AIThreatAgainstThisFactionGoesBackToGuarding &&
                                      entity.Planet.GetDataByStanceForFaction( targetFaction, FactionStance.Self ).TotalStrength == 0) ||
                                     AttachedFaction.GetIsFriendlyTowards( targetFaction ) )
                                {
                                    #region Tracing
                                    if ( innerTracing ) tracingBuffer.Add( ":" ).Add( "skipping because this threat is going to go back to guarding" );
                                    #endregion
                                    continue;
                                }
                                if ( entity.TypeData.IsDrone )
                                {
                                    #region Tracing
                                    if ( innerTracing ) tracingBuffer.Add( ":" ).Add( "skipping because this is a drone" );
                                    #endregion
                                    continue;
                                }
                                if ( entity.IsAllowedToClaimPlanet() && World_AIW2.Instance.GameSecond - entity.GameSecondEnteredThisPlanet <= 10 )
                                {
                                    //usurpers are allowed to claim unowned planets if they have been on a planet for >= 10 seconds.
                                    //If a usurper is spawned next to a wormhole for an eligible planet and then leaves before those 10 seconds are up, it looks weird
                                    #region Tracing
                                    if ( innerTracing ) tracingBuffer.Add( ":" ).Add( "skipping because this usurper will be able to claim the planet shortly" );
                                    #endregion
                                    continue;
                                }
                                #region Tracing
                                if ( innerTracing ) tracingBuffer.Add( ":" ).Add( "counting as threat against faction " + entity.Orders.BehaviorRelatedFactionIndex + " with strength (contents) " ).Add( entity.GetStrengthOfContentsIfAny() ).Add( " (stack) " ).Add( entity.GetStrengthOfStack() ).Add( " total " ).Add( entity.GetStrengthOfSelfAndContents() );
                                #endregion

                                totalThreatFound += entity.GetStrengthOfSelfAndContents();
                                threatChunks.AddEntity( entity ); //threatChunks allows the AI to differentiate which minor faction this threat is hostile toward
                                totalUnassignedThreatShipTimeOnPlanet += entity.GetSecondsSinceEnteringThisPlanet();
                            }
                        }
                        if ( tracing )
                        {
                            tracingBuffer.Add( "\n" ).Add( "We calculated total threat of " ).Add( totalThreatFound ).Add( " All threat chunks:" );
                            foreach ( KeyValuePair<short,ThreatChunk> kv in threatChunks.Chunks )
                            {
                                tracingBuffer.Add( "\n\t" ).Add( "Against faction " ).Add( kv.Value.TargetFactionIndex ).Add( " with total ships: " ).Add( kv.Value.Entities.Count ).Add( " strength: " ).Add( kv.Value.TotalStrength.IntValue ); //hostile toward faction -1 means "General purpose threat"
                            }
                        }
                        Int64 averageUnassignedThreatShipTimeOnPlanet = 0;
                        if ( threatChunks.GetHasAnything() )
                            averageUnassignedThreatShipTimeOnPlanet = totalUnassignedThreatShipTimeOnPlanet / threatChunks.GetTotalCount();
                        int effectiveMyStrengthForInferiorityCases = friendlyStrengthTotal + friendlyStrengthIncoming;
                        debugStage = 207;

                        if ( lrpGuardUnits.Count > 0 )
                        {
                            if ( threatProvokingHostileStrengthTotal > effectiveMyStrengthForInferiorityCases * aiType.RatioOfInferiorityAtWhichGuardShipsGoThreat )
                            {
                                //This planet has a large enemy force on it.
                                //Badger 11/13: the "threatProvokingHostileStrengthTotal should no longer be counting strength from factions the AI doesn't generate threat against (like the devourer)
                                //so I don't know if this code is necessary anymore.
                                #region check for reason to not abandon (ie something like the devourer is here)
                                bool foundReasonToNotAbandon = false;
                                debugStage = 208;
                                int strengthFromNonAbandonWorthyFactions = 0;
                                for ( Int16 factionIndex = 0; factionIndex < World_AIW2.Instance.Factions.Count; factionIndex++ )
                                {
                                    Faction otherFaction = World_AIW2.Instance.Factions[factionIndex];
                                    if ( !otherFaction.GetIsHostileTowards( AttachedFaction ) )
                                        continue;
                                    if ( !otherFaction.SpecialFactionData.AIDoesNotGenerateThreatAgainstThisFaction ) //if this faction generates threat, skip it
                                        continue;
                                    //We only want to check for factions that don't generate threat. If you generate threat we want to abandon.
                                    //This is so the AI doesn't have a fleet chasing the Devourer
                                    foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.MobileCombatants ) )
                                    {
                                        if ( entity.GetPlanetIndexSafe() != planet.Index )
                                            continue;
                                        strengthFromNonAbandonWorthyFactions += entity.GetStrengthOfSelfAndContents();
                                        break;
                                    }
                                }
                                if ( strengthFromNonAbandonWorthyFactions >= threatProvokingHostileStrengthTotal / 2 )
                                    foundReasonToNotAbandon = true;
                                #endregion
                                if ( foundReasonToNotAbandon )
                                {
                                    #region some reason to not abandon, hunker instead
                                    #region Tracing
                                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "guards would abandon, but there's some reason to not do so (presumably the cookie monster), so we'll just hunker instead" );
                                    #endregion
                                    Helper_HunkerDown( Context, planet, tracing, tracingBuffer, lrpGuardUnits, AttachedFaction );
                                    #endregion
                                }
                                else
                                {
                                    #region abandon case
                                    #region Tracing
                                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "guards abandon posts!" ); //ie "become Threat now, and the Threat logic will make it retreat"
                                    #endregion
                                    GameCommand abandonCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetBehavior_FromFaction_AbandonBecauseLargeEnemyForce], GameCommandSource.AnythingElse );
                                    abandonCommand.RelatedMagnitude = (int)EntityBehaviorType.Attacker_Full;
                                    abandonCommand.RelatedFactionIndex = planetFaction.GetIndexOfMostAnnoyingFaction( Context );
                                    if ( GameSettings.Current.GetBoolBySetting( "ThreatDebug" ) )
                                    {
                                        Faction logFaction = World_AIW2.Instance.GetFactionByIndex( abandonCommand.RelatedFactionIndex );
                                        if ( logFaction != null )
                                            ArcenDebugging.ArcenDebugLogSingleLine( "guards abandon posts to go after " + logFaction.GetDisplayName() + " a", Verbosity.DoNotShow );
                                        else
                                            ArcenDebugging.ArcenDebugLogSingleLine( "guards abandon posts to go after -1 a", Verbosity.DoNotShow );
                                    }
                                    for ( int k = 0; k < lrpGuardUnits.Count; k++ )
                                        abandonCommand.RelatedEntityIDs.Add( lrpGuardUnits[k].PrimaryKeyID );
                                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, abandonCommand, false );
                                    abandonCommand = null;
                                    #endregion
                                }
                            }
                            else if ( hostileStrengthTotal > effectiveMyStrengthForInferiorityCases * aiType.RatioOfInferiorityAtWhichDefenseHunkersDown &&
                                      difficulty.Difficulty <= 6 )
                            {
                                //Turns out that hunkering is actually just a worse strategy than running away.
                                //It made more sense during a time period in early development where the AI Planetary Controller
                                //always came with a shield. Only hunker at lower difficulty levels
                                #region hunker down case
                                Helper_HunkerDown( Context, planet, tracing, tracingBuffer, lrpGuardUnits, AttachedFaction );
                                #endregion
                            }
                            else if ( KingUnderAttack )
                            {
                                if ( planet.GetHopsTo( KingPlanet ) <= this.BaseInfo.SentinelInfo.AIDifficulty.GuardFreeDistanceFromKing )
                                {
                                    #region free guards around the king to protect the king
                                    #region Tracing
                                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "guards join to help help AI king from up to " + this.BaseInfo.SentinelInfo.AIDifficulty.GuardFreeDistanceFromKing + " hops away!" ); //ie "become Threat now, and the Threat logic will make it retreat"
                                    #endregion
                                    lrpFactionGuards.Clear();
                                    for ( Int16 factionIndex = 0; factionIndex < World_AIW2.Instance.Factions.Count; factionIndex++ )
                                    {
                                        Faction otherFaction = World_AIW2.Instance.Factions[factionIndex];
                                        if ( !otherFaction.GetIsFriendlyTowards( AttachedFaction ) )
                                            continue;
                                        if ( otherFaction.SpecialFactionData.InternalName != "PraetorianGuard" )
                                            continue;
                                        if ( otherFaction.FactionIndexOfMyParentIfIHaveOne == AttachedFaction.FactionIndex )
                                            lrpFactionGuards.Add( otherFaction ); //note that an AI can have multiple hunters, if other AIs have died
                                    }
                                    if ( lrpFactionGuards.Count == 0 )
                                    {
                                        //There's no praetorian guard for this faction for no obvious reason, so just join the threat fleet
                                        GameCommand abandonCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetBehavior_FromFaction_NoPraetorianGuard], GameCommandSource.AnythingElse );
                                        abandonCommand.RelatedMagnitude = (int)EntityBehaviorType.Attacker_Full;
                                        abandonCommand.RelatedFactionIndex = planetFaction.GetIndexOfMostAnnoyingFaction( Context );
                                        if ( GameSettings.Current.GetBoolBySetting( "ThreatDebug" ) )
                                        {
                                            Faction logFaction = World_AIW2.Instance.GetFactionByIndex( abandonCommand.RelatedFactionIndex );
                                            if ( logFaction != null )
                                                ArcenDebugging.ArcenDebugLogSingleLine( "no praetorian, so go after " + logFaction.GetDisplayName() + " b", Verbosity.DoNotShow );
                                            else
                                                ArcenDebugging.ArcenDebugLogSingleLine( "no praetorian, so go after -1 b", Verbosity.DoNotShow );
                                        }
                                        for ( int k = 0; k < lrpGuardUnits.Count; k++ )
                                            abandonCommand.RelatedEntityIDs.Add( lrpGuardUnits[k].PrimaryKeyID );
                                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, abandonCommand, false );
                                        abandonCommand = null;
                                    }
                                    else
                                    {
                                        GameCommand pendingTransferToPraetorianGuardCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransferEntitiesToFaction], GameCommandSource.AnythingElse );
                                        pendingTransferToPraetorianGuardCommand.RelatedFactionIndex = lrpFactionGuards[Context.RandomToUse.Next( 0, lrpFactionGuards.Count )].FactionIndex;
                                        for ( int k = 0; k < lrpGuardUnits.Count; k++ )
                                            pendingTransferToPraetorianGuardCommand.RelatedEntityIDs.Add( lrpGuardUnits[k].PrimaryKeyID );
                                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, pendingTransferToPraetorianGuardCommand, false );
                                        pendingTransferToPraetorianGuardCommand = null;

                                    }
                                    #endregion
                                }
                            }
                            else
                            {
                                #region normal fight case for guards
                                #region Tracing
                                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "guards fight normally. threatProvokingHostileStrengthTotal " + threatProvokingHostileStrengthTotal + " effectiveMyStrengthForInferiorityCases " + effectiveMyStrengthForInferiorityCases + " RatioOfInferiorityAtWhichGuardShipsGoThreat " + aiType.RatioOfInferiorityAtWhichGuardShipsGoThreat );
                                #endregion
                                Helper_GuardTractorShips( Context, planet, tracing, tracingBuffer, lrpGuardUnits );
                                #endregion
                            }
                        }
                        else
                        {
                            #region Tracing
                            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "no guards here" );
                            #endregion
                        }
                        debugStage = 209;
                        if ( threatChunks.GetHasAnything() )
                        {
                            if ( (!hostileStrengthData.HasKingUnitPresent ? hostileStrengthTotal : hostileStrengthTotal / 20) >
                                 effectiveMyStrengthForInferiorityCases * aiType.RatioOfInferiorityAtWhichThreatRetreats )
                            {
                                debugStage = 210;
                                if ( averageUnassignedThreatShipTimeOnPlanet < this.BaseInfo.SentinelInfo.AIDifficulty.MinimumSecondsOnPlanetBeforeRetreat )
                                {
                                    #region case where we would retreat, but are restrained from doing so by that stupid timer the humans insist we have so they have a chance to play shooting-gallery
                                    #region Tracing
                                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "threat wants to retreat, but is restrained by a mysterious farce!" );
                                    #endregion
                                    // no commands, let the ship level AI take care of it
                                    #endregion
                                }
                                else if ( friendlyStrengthData.HasKingUnitPresent )
                                {
                                    #region Tracing
                                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "threat wants to retreat, but this is my king planet, so fight to the death" );
                                    #endregion
                                }
                                else
                                {
                                    #region retreat case
                                    #region Tracing
                                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "threat retreat!" );
                                    #endregion
                                    this.Helper_RetreatThreat( tracing, tracingBuffer, threatChunks, planet, Context );
                                    #endregion
                                }
                            }
                            else if ( !hostileStrengthData.HasKingUnitPresent &&
                                      friendlyStrengthTotal > hostileStrengthIncludingNonMilitary * aiType.RatioOfSuperiorityAtWhichThreatOverruns )
                            {
                                debugStage = 2110;
                                #region overrun case
                                FInt threatToLeaveBehind = (hostileStrengthIncludingNonMilitary * aiType.RatioOfSuperiorityAtWhichThreatOverruns) - friendlyStrengthGuard;
                                #region Tracing
                                #endregion
                                if ( threatToLeaveBehind > FInt.Zero )
                                {
                                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "threat overrun! Threat Strength I Need To Leave Behind To Stamp Out Infestation:" ).Add( threatToLeaveBehind.ReadableString );
                                    foreach ( KeyValuePair<short, ThreatChunk> kv in threatChunks.Chunks )
                                    {
                                        ThreatChunk chunk = kv.Value;
                                        for ( int k = 0; k < chunk.Entities.Count; k++ )
                                        {
                                            GameEntity_Squad entity = chunk.Entities[k].GetSquad();
                                            if ( entity == null )
                                                continue;
                                            threatToLeaveBehind -= entity.GetStrengthOfSelfAndContents();
                                            chunk.RemoveEntity( entity );
                                            k--;
                                            if ( threatToLeaveBehind <= FInt.Zero )
                                                break;
                                        }
                                    }
                                }
                                if ( threatChunks.GetHasAnything() )
                                {
                                    debugStage = 2111;
                                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Dispatching Threat for a new target" );
                                    this.Helper_SendThreatOnRaid( tracing, tracingBuffer, threatChunks, World_AIW2.Instance.CurrentGalaxy, planet, Context, pathingCacheData );
                                }
                                #endregion
                            }
                            else
                            {
                                #region normal fight case
                                #region Tracing
                                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "threat fight normally (there may be sub-commands)" );
                                #endregion
                                //This is generic "Combat on a planet"
                                //Normal case is "let ship level AI handle things"
                                //if we are on a player planet, we may choose to bum rush the command station,
                                //or if we think this planet is stronger than we want, we can try to attack the weakest adjacent player planet
                                debugStage = 229;

                                Helper_MakeThreatShipsFight( Context, planet, tracing, tracingBuffer, threatChunks );

                                bool playAudioEffectForCommand = false;
                                if ( planet.GetControllingFactionType() == FactionType.Player && threatChunks.GetHasAnything() )
                                {
                                    Planet kingPlanet = null;
                                    debugStage = 213;
                                    int rand = Context.RandomToUse.Next( 0, 100 );

                                    if ( this.BaseInfo.SentinelInfo.AIDifficulty.AllowedToGoForPlayerHomeworld &&
                                         FactionUtilityMethods.Instance.IsPlanetAdjacentToPlayerKing( planet, out kingPlanet ) )
                                    {
                                        debugStage = 214;
                                        //                                    ArcenDebugging.ArcenDebugLogSingleLine("found force adjacent to player king. King is on " + kingPlanet.Name, Verbosity.DoNotShow );
                                        if ( kingPlanet == null )
                                            throw new Exception( "king is adjacent, but somehow the kingPlanet is null" );

                                        var neighborFactionData = kingPlanet.GetStanceDataForFaction( AttachedFaction );
                                        StrengthData_PlanetFaction_Stance neighborHostileStrengthData = neighborFactionData[FactionStance.Hostile];
                                        int neighborHostileStrengthTotal = neighborHostileStrengthData.TotalStrength;
                                        if ( neighborHostileStrengthTotal < (friendlyStrengthTotal + neighborFactionData[FactionStance.Self].TotalStrength) / 2 )
                                        {
                                            //                                        ArcenDebugging.ArcenDebugLogSingleLine("strong enough!", Verbosity.DoNotShow );
                                            GameEntity_Other thisWormhole = planet.GetWormholeTo( kingPlanet );
                                            bool foundBlockingShield = FactionUtilityMethods.Instance.IsShieldBlockingWormholeToPlanet( planet, kingPlanet, AttachedFaction );
                                            if ( !foundBlockingShield )
                                            {
                                                GameCommand sneakCommand = null;
                                                debugStage = 226;
                                                foreach ( KeyValuePair<short, ThreatChunk> kv in threatChunks.Chunks )
                                                {
                                                    debugStage = 227;
                                                    ThreatChunk chunk = kv.Value;
                                                    if (chunk.TargetFaction != null && GameSettings.Current.GetBoolBySetting("Debug_ThreatRaidsUseChunkTarget")) {
                                                        continue;
                                                    }
                                                    if ( chunk.TotalStrength > 0 )
                                                    {
                                                        for ( int k = 0; k < chunk.Entities.Count; k++ )
                                                        {
                                                            if ( sneakCommand == null )
                                                                sneakCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_AIRaidKing], GameCommandSource.AnythingElse );
                                                            sneakCommand.RelatedEntityIDs.Add( chunk.Entities[k].PrimaryKeyID );
                                                        }
                                                    }
                                                }
                                                debugStage = 228;
                                                if ( sneakCommand != null && sneakCommand.RelatedEntityIDs.Count > 0 )
                                                {
                                                    sneakCommand.RelatedString = "AI_GOKING";
                                                    sneakCommand.ToBeQueued = true;
                                                    sneakCommand.RelatedIntegers.Add( kingPlanet.Index );
                                                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, sneakCommand, playAudioEffectForCommand );
                                                }
                                            }
                                        }
                                    }
                                    else if ( rand < this.BaseInfo.SentinelInfo.AIDifficulty.AttackPercentCommandStation )
                                    {
                                        debugStage = 215;
                                        GameEntity_Squad commandStation = planet.GetCommandStationOrNull();
                                        if ( commandStation != null )
                                        {
                                            debugStage = 216;
                                            GameCommand commandStationAttackCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.Attack], GameCommandSource.AnythingElse );
                                            commandStationAttackCommand.ToBeQueued = true;

                                            commandStationAttackCommand.RelatedIntegers4.Add( commandStation.PrimaryKeyID );
                                            debugStage = 217;
                                            foreach ( KeyValuePair<short, ThreatChunk> kv in threatChunks.Chunks )
                                            {
                                                debugStage = 218;
                                                ThreatChunk chunk = kv.Value;
                                                if (chunk.TargetFaction != null && GameSettings.Current.GetBoolBySetting("Debug_ThreatRaidsUseChunkTarget")) {
                                                    continue;
                                                }
                                                if ( chunk.TotalStrength > 0 )
                                                {
                                                    for ( int k = 0; k < chunk.Entities.Count; k++ )
                                                        commandStationAttackCommand.RelatedEntityIDs.Add( chunk.Entities[k].PrimaryKeyID );
                                                }
                                            }
                                            debugStage = 219;
                                            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, commandStationAttackCommand, playAudioEffectForCommand );
                                            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Threat of " + commandStationAttackCommand.RelatedEntityIDs.Count + " units attacking command station" );
                                            commandStationAttackCommand = null;
                                            //ArcenDebugging.ArcenDebugLogSingleLine("Sending threat right for command station", Verbosity.DoNotShow );
                                        }
                                    }
                                    else if ( hostileStrengthTotal > effectiveMyStrengthForInferiorityCases )
                                    {
                                        debugStage = 220;
                                        rand = Context.RandomToUse.Next( 0, 100 ); //recalculate the random number
                                        if ( rand < this.BaseInfo.SentinelInfo.AIDifficulty.AttackPercentFindWeakerTarget )
                                        {
                                            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Threat bypass to weaker planet" );
                                            debugStage = 221;
                                            //If this planet seems pretty tough for me, see if there are any adjacent weaker player planets and go for those
                                            //TODO: we should actually use a List here and select randomly in case there are multiple good options
                                            //We might also want to enhance Helper_RetreatThreat to incorporate this style of 'sneaking past player defenses'
                                            Planet newTarget = null;
                                            int weakestPlanetNeighborStrength = hostileStrengthTotal;
                                            foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                                            {
                                                if ( neighbor.GetControllingFactionType() != FactionType.Player )
                                                    continue;
                                                debugStage = 222;
                                                var neighborFactionData = neighbor.GetStanceDataForFaction( AttachedFaction );
                                                StrengthData_PlanetFaction_Stance neighborHostileStrengthData = neighborFactionData[FactionStance.Hostile];
                                                int friendlyStrength = neighborFactionData[FactionStance.Self].TotalStrength + neighborFactionData[FactionStance.Friendly].TotalStrength;
                                                int neighborHostileStrengthTotal = neighborHostileStrengthData.TotalStrength - friendlyStrength;
                                                debugStage = 223;
                                                if ( neighborHostileStrengthTotal < weakestPlanetNeighborStrength && !FactionUtilityMethods.Instance.IsShieldBlockingWormholeToPlanet( planet, neighbor, AttachedFaction ) )
                                                {
                                                    weakestPlanetNeighborStrength = neighborHostileStrengthTotal;
                                                    newTarget = neighbor;
                                                }
                                            }
                                            debugStage = 224;
                                            if ( newTarget != null )
                                            {
                                                debugStage = 225;

                                                GameCommand sneakCommand = null;
                                                debugStage = 226;
                                                foreach ( KeyValuePair<short, ThreatChunk> kv in threatChunks.Chunks )
                                                {
                                                    debugStage = 227;
                                                    ThreatChunk chunk = kv.Value;
                                                    if (chunk.TargetFaction != null && GameSettings.Current.GetBoolBySetting("Debug_ThreatRaidsUseChunkTarget")) {
                                                        continue;
                                                    }
                                                    if ( chunk.TotalStrength > 0 )
                                                    {
                                                        for ( int k = 0; k < chunk.Entities.Count; k++ )
                                                        {
                                                            if ( sneakCommand == null )
                                                                sneakCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_AIRaidKing], GameCommandSource.AnythingElse );
                                                            sneakCommand.RelatedEntityIDs.Add( chunk.Entities[k].PrimaryKeyID );
                                                        }
                                                    }
                                                }
                                                debugStage = 228;
                                                if ( sneakCommand.RelatedEntityIDs.Count > 0 )
                                                {
                                                    sneakCommand.RelatedString = "AI_T_CHUNKS";
                                                    sneakCommand.ToBeQueued = true;
                                                    sneakCommand.RelatedIntegers.Add( newTarget.Index );
                                                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, sneakCommand, playAudioEffectForCommand );
                                                    sneakCommand = null;
                                                }
                                            }
                                        }
                                    }
                                }
                                debugStage = 229;
                                Helper_ThreatTractorShips( Context, planet, tracing, tracingBuffer, threatChunks );
                            }
                            // no overriding commands, let the ship level AI take care of it
                            #endregion
                        }
                        else
                        {
                            #region Tracing
                            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "no threat forces here" );
                            #endregion
                        }
                        #region Tracing
                        if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                        if ( tracing )
                        {
                            tracingBuffer.ReturnToPool();
                            tracingBuffer = null;
                        }
                        #endregion
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "AI ThreatChunks Error: " + e, Verbosity.ShowAsError );
                    }
                    finally
                    {
                        threatChunks.ReturnToPool();
                    }
                }
                #endregion
                debugStage = 300;
                HandleExogalacticAttacks( Context, pathingCacheData );
                HandleRelicTrains( Context, pathingCacheData );
                HandleEnemyKingAttackers( Context, pathingCacheData );
                HandleWormholeBorers_LRP( Context, pathingCacheData );
                HandleCuendillarTransports_LRP( Context, pathingCacheData );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in AI.DoLongRangePlanning_OnBackgroundNonSimThread_Subclass stage " + debugStage + "\n" + e, Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();
            }
        }

        private readonly List<Faction> lrpHunterFleets = List<Faction>.Create_WillNeverBeGCed( 20, "AISentinelsFactionDeepInfo-lrpHunterFleets" );

        private bool Helper_ConsiderJoiningTheHunterFleetFromThreatfleet( GameEntity_Squad entity, Faction targetFaction,
            ref GameCommand pendingTransferToHunterFleetCommand, ref bool foundNoHunterFleet,
            bool tracing, ArcenCharacterBuffer tracingBuffer, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( entity.TypeData.IsDrone )
                return false; //drones should never join hunter fleet; they'll die once off planet. I'm not sure if drones can actually be Threat anymore, so this check is a "better safe than sorry"
            if ( entity.TypeData.NotEligibleToJoinHunterFleet ) //for stuff like Overlord Part 2 or Usurpers)
                return false;

            if ( targetFaction != null && targetFaction.Type != FactionType.Player )
            {
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n\t***I would transfer to a Hunter Fleet, but I'm targeting a non-human faction (" ).Add( targetFaction.SpecialFactionData.InternalName ).Add( ") so I won't" );
                #endregion
                return false;
            }
            else
            {
                if ( pendingTransferToHunterFleetCommand == null && !foundNoHunterFleet )
                {
                    lrpHunterFleets.Clear();
                    for ( Int16 factionIndex = 0; factionIndex < World_AIW2.Instance.Factions.Count; factionIndex++ )
                    {
                        Faction otherFaction = World_AIW2.Instance.Factions[factionIndex];
                        if ( !otherFaction.GetIsFriendlyTowards( AttachedFaction ) )
                            continue;
                        if ( otherFaction.SpecialFactionData.InternalName != "HunterFleet" )
                            continue;
                        if ( otherFaction.FactionIndexOfMyParentIfIHaveOne == AttachedFaction.FactionIndex )
                            lrpHunterFleets.Add( otherFaction ); //note that an AI can have multiple hunters, if other AIs have died
                    }
                    if ( lrpHunterFleets.Count <= 0 )
                        foundNoHunterFleet = true;
                    else
                    {
                        pendingTransferToHunterFleetCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransferEntitiesToFaction], GameCommandSource.AnythingElse );
                        pendingTransferToHunterFleetCommand.RelatedFactionIndex = lrpHunterFleets[Context.RandomToUse.Next( 0, lrpHunterFleets.Count )].FactionIndex;
                    }
                }
                if ( pendingTransferToHunterFleetCommand != null )
                {
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n\t***I've been waiting long enough so transferring to the Hunter Fleet" );
                    #endregion
                    pendingTransferToHunterFleetCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                    return true;
                }
                else
                {
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n\t***I've been waiting long enough so I would transfer to a Hunter Fleet, but I can't find a friendly one" );
                    #endregion
                    return false;
                }
            }
        }


        #region HandleRelicTrains
        private void HandleRelicTrains( ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            int debugCode = 0;
            try{
            if ( RelicTrains.Count == 0 )
                return;
            debugCode = 100;
            //if ( Expansion.GetExpansionStatus_SemiExpensive( "1_The_Spire_Rises", true ) != ExpansionsStatus.InstalledAndEnabled )
            //    throw new Exception("We somehow have a relic train but not the expansion with that installed. This should be impossible");

            for ( int i = 0; i < RelicTrains.Count; i++ )
            {
                debugCode = 200;
                GameEntity_Squad train = RelicTrains[i].GetSquad();
                if ( train == null )
                    continue;
                if ( train.Orders == null )
                    continue;
                debugCode = 300;
                if ( train.Orders.GetQueuedOrderCount() == 0 )
                {
                    debugCode = 400;
                    GameEntity_Squad king = FactionUtilityMethods.Instance.findKing(AttachedFaction);
                    if ( king == null )
                        continue;
                    FallenSpirePerUnitBaseInfo trainData = train.TryGetExternalBaseInfoAs<FallenSpirePerUnitBaseInfo>();
                    if ( trainData == null )
                        continue;
                    debugCode = 500;
                    if ( train.Planet == king.Planet && trainData.HopsLeftForTrain <= 0 )
                    {
                        //we need to get to the king
                        debugCode = 600;
                        GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCChaseAKing], GameCommandSource.AnythingElse );
                        moveCommand.PlanetOrderWasIssuedFrom = train.Planet.Index;
                        moveCommand.RelatedPoints.Add( king.WorldLocation );
                        moveCommand.RelatedEntityIDs.Add( train.PrimaryKeyID );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );
                        continue;
                    }
                    debugCode = 700;
                    if ( Context.RandomToUse.Next( 0, 100 ) < 50 )
                        FactionUtilityMethods.Instance.SendUnitToRandomMetalGenerator( AttachedFaction, Context, train, 10f );
                    else
                    {
                        debugCode = 800;
                        if ( trainData == null )
                            throw new Exception(train.ToStringWithPlanetAndOwner() + " does not have fallen spire per unit data");
                        debugCode = 850;
                        Planet target = null;
                        trainData.HopsLeftForTrain--;
                        if ( trainData.HopsLeftForTrain == 0 )
                            target = king.Planet;
                        else
                        {
                            debugCode = 875;
                            // if ( FallenSpireFactionDeepInfo == null ) {
                            //     ArcenDebugging.LogSingleLine("A", Verbosity.DoNotShow );
                            // }
                            if ( FallenSpireFactionDeepInfo.Instance == null ) {
                                ArcenDebugging.LogSingleLine("B", Verbosity.DoNotShow );
                            }
                            debugCode = 880;
                            target = FallenSpireFactionDeepInfo.Instance.GetRelicTrainPlanet(AttachedFaction, train.Planet, 3, 8, Context, PathCacheData );
                        }
                        debugCode = 900;
                        //DEBUG
                        // if ( target == null )
                        //     ArcenDebugging.ArcenDebugLogSingleLine("We have reached our destination on " + train.Planet.Name + " but coult find no next planet! There are " + trainData.HopsLeftForTrain + " hops left.", Verbosity.DoNotShow );
                        // else
                        //     ArcenDebugging.ArcenDebugLogSingleLine("We have reached our destination on " + train.Planet.Name + " and are now heading to " + target.Name +". There are " + trainData.HopsLeftForTrain + " hops left.", Verbosity.DoNotShow );
                        if ( target == null )
                            throw new Exception("Could not find next planet for relic train");
                        PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "SentinelsHandleRelicTrains", train.Planet, target, PathingMode.Default, Context, PathCacheData );
                        debugCode = 1000;
                        if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                        {
                            debugCode = 1100;
                            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse );
                            command.RelatedString = "SpireTrain_Move";
                            command.RelatedEntityIDs.Add( train.PrimaryKeyID );
                            for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                                command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                        }
                    }
                }
            }
            } catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit excpetion in HandleRelicTrains debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion

        #region HandleEnemyKingAttackers
        private void HandleEnemyKingAttackers( ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            //            ArcenDebugging.ArcenDebugLogSingleLine( "HandleEnemyKingAttackers: " + EnemyKingAttackers.Count, Verbosity.DoNotShow );
            if ( EnemyKingAttackers.Count == 0 )
                return;

            for ( int i = 0; i < EnemyKingAttackers.Count; i++ )
            {
                GameEntity_Squad kingAttacker = EnemyKingAttackers[i].GetSquad();
                if ( kingAttacker == null )
                    continue;
                //ArcenDebugging.ArcenDebugLogSingleLine( "kingAttacker: " + kingAttacker.TypeData.DisplayName, Verbosity.DoNotShow );
                if ( kingAttacker.Orders == null )
                    continue;

                //If no orders, provide orders
                bool getNewOrders = false;
                if ( kingAttacker.Orders.GetQueuedOrderCount() == 0 )
                    getNewOrders = true;
                GameEntity_Squad enemyKing = FactionUtilityMethods.Instance.findNearestHumanKing( kingAttacker.Planet );
                if ( enemyKing == null )
                    continue;
                if ( !getNewOrders && kingAttacker.CalculateFinalDestinationPlanetIndex_Safe() != enemyKing.Planet.Index )
                    getNewOrders = true;

                if ( !getNewOrders && kingAttacker.Orders.GetQueuedOrderCount() > 3)
                {
                    //if we are an overlord a decent ways from a player king, see if there's a faster route.
                    //This will allow units to take advantage of changing map topology (wormhole invasions/nomads/etc)
                    Planet target = enemyKing.Planet;

                    PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "SentinelsHandleEnemyKingAttackersDefault1", kingAttacker.Planet, target, PathingMode.Default, Context, PathCacheData );
                    if ( pathCache == null || pathCache.PathToReadOnly.Count <= 0  )
                        pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "SentinelsHandleEnemyKingAttackersShortest1", kingAttacker.Planet, target, PathingMode.Shortest, Context, PathCacheData );

                    //ArcenDebugging.ArcenDebugLogSingleLine( "pathCache: " + (pathCache == null ? "null" : pathCache.PathToReadOnly.Count.ToString() ), Verbosity.DoNotShow );
                    if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 &&
                         pathCache.PathToReadOnly.Count < kingAttacker.Orders.GetQueuedOrderCount())
                    {
                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse );
                        command.RelatedString = "EnemyKingAttacker_Move";
                        command.RelatedEntityIDs.Add( kingAttacker.PrimaryKeyID );
                        for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                            command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                    }
                    continue;
                }
                if ( getNewOrders )
                {
                    if ( kingAttacker.Planet == enemyKing.Planet )
                    {
                        //we need to get to the enemy king
                        GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCChaseAKing], GameCommandSource.AnythingElse );
                        moveCommand.PlanetOrderWasIssuedFrom = kingAttacker.Planet.Index;
                        moveCommand.RelatedPoints.Add( enemyKing.WorldLocation );
                        moveCommand.RelatedEntityIDs.Add( kingAttacker.PrimaryKeyID );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );
                        continue;
                    }
                    //if ( Context.RandomToUse.Next( 0, 100 ) < 10 )
                    //    FactionUtilityMethods.Instance.SendUnitToRandomMetalGenerator( faction, Context, kingAttacker );
                    //else
                    {
                        Planet target = enemyKing.Planet;
                        PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "SentinelsHandleEnemyKingAttackersDefault2", 
                            kingAttacker.Planet, target, PathingMode.Default, Context, PathCacheData );
                        if ( pathCache == null || pathCache.PathToReadOnly.Count <= 0  )
                            pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "SentinelsHandleEnemyKingAttackersShortest2", 
                                kingAttacker.Planet, target, PathingMode.Shortest, Context, PathCacheData );

                        //ArcenDebugging.ArcenDebugLogSingleLine( "pathCache: " + (pathCache == null ? "null" : pathCache.PathToReadOnly.Count.ToString() ), Verbosity.DoNotShow );
                        if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                        {
                            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse );
                            command.RelatedString = "EnemyKingAttacker_Move";
                            command.RelatedEntityIDs.Add( kingAttacker.PrimaryKeyID );
                            for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                                command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                        }
                    }
                    continue;
                }
            }
        }
        #endregion

        public void HandleCuendillarTransports_LRP(ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData)
        {
            for (int i = 0; i < CuendillarTransports.Count; i++)
            {
                GameEntity_Squad transport = CuendillarTransports[i].GetSquad();
                if (transport == null)
                    continue;
                if ( transport.HasQueuedOrders() )
                    continue;
                GameEntity_Squad localKing = this.AttachedFaction.GetFactionKing();

                if ( transport.Planet != localKing.Planet )
                {
                    AutoDefendUtility.GoToPlanet( transport, localKing.Planet, Context, PathCacheData );
                    continue;
                }

                if (Mat.DistanceBetweenPointsImprecise(transport.WorldLocation, localKing.WorldLocation) < 100)
                {

                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName("AICuendillarDestroyer");
                    GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( transport.PlanetFaction, entityData, AttachedFaction.CurrentGeneralMarkLevel,
                                                                                               transport.PlanetFaction.FleetUsedAtPlanet, 0, transport.WorldLocation, Context, "AICuendillarDestroyer" ); //fine because mapgen

                    transport.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut); //not sure if this is the right reason
                }
                else
                {
                    GameCommand moveCommand = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse);
                    moveCommand.PlanetOrderWasIssuedFrom = transport.Planet.Index;
                    moveCommand.RelatedPoints.Add(localKing.WorldLocation);
                    moveCommand.RelatedEntityIDs.Add(transport.PrimaryKeyID);
                    World_AIW2.Instance.QueueGameCommand(this.AttachedFaction, moveCommand, false);
                }
            }
        }
        public void HandleCuendillarDrills_MainSim(ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData)
        {
            int debugCode = 0;
            try{
            foreach ( GameEntity_Squad drill in this.AttachedFaction.Squads( "AICuendillarDrill" ) )
            {
                if (drill == null)
                    continue;
                DysonSidekickPerUnitBaseInfo data = drill.GetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                debugCode = 100;
                if (data == null)
                {
                    continue;
                }
                debugCode = 200;
                int interval = 120;
                int cuendillarToMine = 1;
                GameEntity_Squad drillTarget = FactionUtilityMethods.Instance.GetReaperChrysalisOnPlanetOrNull(drill.Planet);
                if ( drillTarget == null )
                    drillTarget = FactionUtilityMethods.Instance.GetPlanetoidOnPlanetOrNull( drill.Planet );
                if ( drillTarget == null )
                {
                    drill.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut); //not sure if this is the right reason
                    continue;
                }
                debugCode = 300;
                if (data.TimeForNextTransport == -1)
                    data.TimeForNextTransport = World_AIW2.Instance.GameSecond + interval;
                if (data.TimeForNextTransport <= World_AIW2.Instance.GameSecond)
                {
                    debugCode = 400;
                    data.TimeForNextTransport = World_AIW2.Instance.GameSecond + interval;

                    GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "AICuendillarTransport");
                    ArcenPoint spawnLocation = drill.Planet.GetSafePlacementPoint_AroundEntity(Context, typeData, drill, FInt.FromParts(0, 025), FInt.FromParts(0, 150));
                    GameEntity_Squad transport = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(drill.PlanetFaction, typeData, 1,
                                                                                                  null, 0, spawnLocation, Context, "AI-NewTransport");
                    debugCode = 500;
                    if ( drillTarget.TypeData.GetHasTag("ReaperChrysalis"))
                    {
                        ReapersPerUnitBaseInfo perUnitData = drillTarget.CreateExternalBaseInfo<ReapersPerUnitBaseInfo>( "ReapersPerUnitBaseInfo" );

                        int amountToMine = Math.Min(cuendillarToMine, perUnitData.CuendillarRemaining);
                        perUnitData.CuendillarRemaining -= amountToMine;
                        if ( perUnitData.CuendillarRemaining <= 0 )
                        {
                            drillTarget.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut);
                        }

                    }
                    else
                    {
                        DysonSidekickPerUnitBaseInfo perUnitData = drillTarget.CreateExternalBaseInfo<DysonSidekickPerUnitBaseInfo>( "DysonSidekickPerUnitBaseInfo" );

                        int amountToMine = Math.Min(cuendillarToMine, perUnitData.CuendillarRemaining);
                        perUnitData.CuendillarRemaining -= amountToMine;
                        if ( perUnitData.CuendillarRemaining <= 0 )
                        {
                            FactionUtilityMethods.Instance.DespawnCuendillarAsteroidOnPlanet(drillTarget.Planet, Context);
                        }

                    }
                }
            }
            } catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in HandleCuendillarDrills_MainSim debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }

        public void HandleWormholeBorers_LRP( ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            for ( int i = 0; i < WormholeBorers.Count; i++ )
            {
                GameEntity_Squad borer = WormholeBorers[i].GetSquad();
                if ( borer == null )
                    continue;
                Planet startBoringPlanet = World_AIW2.Instance.GetPlanetByIndex(borer.WBStartPlanet);
                Planet endBoringPlanet = World_AIW2.Instance.GetPlanetByIndex(borer.WBDestinationPlanet);
                if ( borer.HasQueuedOrders() )
                    continue; //we're doing something now!
                if ( borer.Planet != startBoringPlanet )
                {
//                    ArcenDebugging.ArcenDebugLogSingleLine("LRP: moving borer from " + borer.GetPlanetName_Safe() + " to " + startBoringPlanet.Name + " end planet " + endBoringPlanet.Name, Verbosity.DoNotShow );
                    PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "SentinelsWormholeBorerLRP", borer.Planet, startBoringPlanet, PathingMode.Safest, Context, PathCacheData );
                    if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                    {
                        GameCommand wormholeCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_AIThreatRaid], GameCommandSource.AnythingElse );
                        wormholeCommand.RelatedEntityIDs.Add( borer.PrimaryKeyID );
                        for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                        {
//                            ArcenDebugging.ArcenDebugLogSingleLine("\t" + path[k].Name, Verbosity.DoNotShow );
                            wormholeCommand.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                        }
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, wormholeCommand, false );
                    }
                }
                else
                {
                    GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCWander], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );

                    moveCommand.PlanetOrderWasIssuedFrom = borer.Planet.Index;
                 //   moveCommand.ToBeQueued = true; //this is queued
                    GameEntityTypeData TypeData = GameEntityTypeDataTable.Instance.GetRowByName( "WormholeBorer" );
                    ArcenPoint spawnLocation = startBoringPlanet.GetSafePlacementPointAroundPlanetCenter(Context, TypeData, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 100 ) );

                    moveCommand.RelatedPoints.Add( spawnLocation );
                    moveCommand.RelatedEntityIDs.Add( borer.PrimaryKeyID );
                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );
                }
            }
        }

        private void HandleExogalacticAttacks( ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( ShipsInExogalacticAttacks.Count == 0 )
                return;

            Dictionary<RefPair<Planet, Planet>, GameCommand> exoMoveCommands = Planet.GetTemporaryPlanetToPlanetDictOfGameCommands( "AISentinelsFactionDeepInfo-exoMoveCommands", 10f );
            if ( exoMoveCommands == null ) //blocked for teardown/shutdown; bail
                return;
            Dictionary<SafeSquadWrapper, GameCommand> exoAttackCommands = GameEntity_Squad.GetTemporarySquadDictOfGameCommands( "AISentinelsFactionDeepInfo-exoAttackCommands", 10f );
            if ( exoAttackCommands == null ) //blocked for teardown/shutdown; bail
            {
                Planet.ReleaseTemporaryPlanetToPlanetDictOfGameCommands( exoMoveCommands );
                return;
            }
            Dictionary<Planet, SafeSquadWrapper> blackHoleGenerators = Planet.GetTemporaryPlanetDictOfSquads( "AISentinelsFactionDeepInfo-blackHoleGenerators", 10f );
            if ( blackHoleGenerators == null ) //blocked for teardown/shutdown; bail
            {
                Planet.ReleaseTemporaryPlanetToPlanetDictOfGameCommands( exoMoveCommands );
                GameEntity_Squad.ReleaseTemporarySquadDictOfGameCommands( exoAttackCommands );
                return;
            }

            //we could potentially have lots of exos with lots of targets
            int debugCode = 0;
            try
            {
                for ( int i = 0; i < ShipsInExogalacticAttacks.Count; i++ )
                {
                    debugCode = 100;
                    GameEntity_Squad entity = ShipsInExogalacticAttacks[i].GetSquad();
                    if ( entity == null )
                        continue;
                    GameEntity_Squad target = entity.ExoGalacticAttackTarget.GetSquad();
                    if ( target == null )
                        continue;
                    Planet entityPlanet = entity.Planet;
                    Planet targetPlanet = target.Planet;
                    debugCode = 200;
                    if ( entity == null || target == null || entityPlanet == null || targetPlanet == null )
                    {
                        //this is in long range planning, so someone might have killed the target between the check in DoLongRangePlanning and here
                        //once this is detected, the entity per-second code will remove this unit from the exo
                        continue;
                    }
                    if ( entityPlanet != targetPlanet )
                    {
                        debugCode = 300;
                        if ( entity.IsBlackHoledAtMoment.Display )
                        {
                            debugCode = 350;
                            //if we are black holed, go kill the black hole target
                            if ( !blackHoleGenerators.ContainsKey( entityPlanet ) )
                                blackHoleGenerators[entityPlanet] = SafeSquadWrapper.Create( GetHostileBlackHoleGenerator( entity, Context ) );
                            debugCode = 362;
                            if ( entity.CalculateAttackingTargetID_Safe() == blackHoleGenerators[entityPlanet].PrimaryKeyID )
                                continue;//we are black holed, but already ready to kill the generator
                            debugCode = 364;
                            //set ourselves to target the black hole generator
                            if ( !exoAttackCommands.ContainsKey( target ) )
                                exoAttackCommands.Set( target, GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.Attack], GameCommandSource.AnythingElse ) );
                            debugCode = 366;
                            GameCommand command = exoAttackCommands.Get( target );
                            command.RelatedIntegers4.Add( blackHoleGenerators[entityPlanet].PrimaryKeyID ); //this is our target now
                            command.RelatedEntityIDs.Add( entity.PrimaryKeyID ); //this is me
                            debugCode = 368;
                            //ArcenDebugging.ArcenDebugLogSingleLine(entity.ToStringWithPlanet() + " is in exo, but diverted to attack " + blackHoleGenerators[entityPlanet].ToStringWithPlanet(), Verbosity.DoNotShow );
                            command.ToBeQueued = true;
                            continue;
                        }
                        debugCode = 391;
                        if ( entity.CalculateFinalDestinationPlanetIndex_Safe() == targetPlanet.Index )
                            continue; //we are already heading to the target
                        //send us to the target
                        debugCode = 392;
                        RefPair<Planet, Planet> pair = RefPair<Planet, Planet>.Create( entityPlanet, targetPlanet );
                        GameCommand gameC = null;
                        if ( !exoMoveCommands.ContainsKey( pair ) )
                        {
                            debugCode = 393;
                            PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "SentinelsHandleExogalacticAttacks",
                                entityPlanet, targetPlanet, PathingMode.Default, Context, PathCacheData );
                            if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                            {
                                exoMoveCommands[pair] = gameC = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_ExoMove], GameCommandSource.AnythingElse );
                                gameC.RelatedString = "AI_EXO_MOVE";
                                gameC.RelatedIntegers.Add( targetPlanet.Index );
                                gameC.ToBeQueued = false;
                                for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                                {
                                    debugCode = 394;
                                    gameC.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                                }
                            }
                        }
                        else
                        {
                            debugCode = 395;
                            gameC = exoMoveCommands[pair];
                        }
                        debugCode = 396;
                        if ( gameC != null )
                            gameC.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                    }
                    else
                    {
                        debugCode = 400;
                        //we are already attacking this target, so skip
                        if ( entity.CalculateAttackingTargetID_Safe() == target.PrimaryKeyID )
                            continue;

                        if ( !exoAttackCommands.ContainsKey( target ) )
                        {
                            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.Attack], GameCommandSource.AnythingElse );
                            command.RelatedIntegers4.Add( target.PrimaryKeyID );
                            command.ToBeQueued = true;
                            exoAttackCommands.Set( target, command );
                        }
                        exoAttackCommands.Get( target ).RelatedEntityIDs.Add( entity.PrimaryKeyID );
                    }
                }
                //note: I think there's no way for these to be invalid and thus need to just go back to the pool?
                debugCode = 500;
                foreach ( KeyValuePair<RefPair<Planet, Planet>, GameCommand> item in exoMoveCommands )
                {
                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, item.Value, false );
                }
                foreach ( KeyValuePair<SafeSquadWrapper, GameCommand> item in exoAttackCommands )
                {
                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, item.Value, false );
                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "debugCode " + debugCode + " handle exo attacks " + e.ToString(), Verbosity.DoNotShow );
            }

            Planet.ReleaseTemporaryPlanetToPlanetDictOfGameCommands( exoMoveCommands );
            GameEntity_Squad.ReleaseTemporarySquadDictOfGameCommands( exoAttackCommands );
            Planet.ReleaseTemporaryPlanetDictOfSquads( blackHoleGenerators );
        }

        private GameEntity_Squad GetHostileBlackHoleGenerator( GameEntity_Squad squad, ArcenLongTermIntermittentPlanningContext Context )
        {
            //Find the black hole generator affecting this squad
            GameEntity_Squad hole = null;
            PlanetFaction pFaction = squad.Planet.GetPlanetFactionForFaction(AttachedFaction);
            foreach ( PlanetFaction otherFaction in pFaction.RelatedFactions( FactionRelationship.FactionsThatAreHostileTowardsMe ) )
            {
                foreach ( GameEntity_Squad entity in otherFaction.Entities.Squads() )
                {
                    if ( entity.TypeData.AddsBlackHoleEffectForEntitiesWithEngine_gxLessThan >= squad.TypeData.Engine_gx )
                    {
                        hole = entity;
                        break;
                    }
                }
                if ( hole != null )
                    break;
            }
            //if ( hole == null ) Chris says: don't complain.  I'm guessing this can happen from some secondary effects at times.
            //    ArcenDebugging.ArcenDebugLogSingleLine(squad.ToStringWithPlanetAndOwner() + " is black holed, but no black hole generator found", Verbosity.ShowAsError );
            return hole;
        }
        private void Helper_GuardTractorShips ( ArcenLongTermIntermittentPlanningContext Context, Planet planet,  bool tracing, ArcenCharacterBuffer tracingBuffer, List<SafeSquadWrapper> list )
        {
            //If a guard ship has tractored a bunch of ships, make it Threat and let the threat code handle it
            GameCommand threatCommand = null;
            for ( int i = 0; i < list.Count; i++ )
            {
                GameEntity_Squad ship = list[i].GetSquad();
                if ( ship == null )
                    continue;
                if ( ship.ReadyToDragTractoredShipsAway &&
                     ship.Orders.Behavior != EntityBehaviorType.Attacker_Full )
                {
                    if ( threatCommand == null )
                        threatCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetBehavior_FromFaction_ITractoredShips], GameCommandSource.AnythingElse );
                    threatCommand.RelatedEntityIDs.Add( ship.PrimaryKeyID );
                }
            }
            if ( threatCommand != null && threatCommand.RelatedEntityIDs.Count > 0)
            {
                threatCommand.RelatedMagnitude = (int)EntityBehaviorType.Attacker_Full;
                threatCommand.RelatedFactionIndex = planet.GetPlanetFactionForFaction(AttachedFaction).GetIndexOfMostAnnoyingFaction( Context );
                if ( GameSettings.Current.GetBoolBySetting( "ThreatDebug" ) )
                {
                    Faction logFaction = World_AIW2.Instance.GetFactionByIndex(threatCommand.RelatedFactionIndex);
                    if ( logFaction != null )
                        ArcenDebugging.ArcenDebugLogSingleLine("ships are going " + logFaction.GetDisplayName() + " tractor c ", Verbosity.DoNotShow );
                    else
                        ArcenDebugging.ArcenDebugLogSingleLine("ships are going after  -1 tractor c ", Verbosity.DoNotShow );
                }
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, threatCommand, false );
            }
            else
            {
                if ( threatCommand != null )
                    threatCommand.ReturnToPool(); //prevent leak of commands!
            }
        }
        private void Helper_ThreatTractorShips ( ArcenLongTermIntermittentPlanningContext Context, Planet planet,  bool tracing, ArcenCharacterBuffer tracingBuffer, ThreatChunkCollection threatChunks )
        {
            //If we are a ship with tractor beams and have grabbed "enough" player ships, drag them off planet
            //Note that we also will need to set those ships to be Threat afterwards
            GameCommand moveCommand = null;
            GameCommand threatCommand = null;

            foreach ( KeyValuePair<short, ThreatChunk> kv in threatChunks.Chunks )
            {
                ThreatChunk chunk = kv.Value;
                if(chunk.TotalStrength > 0)
                {
                    for ( int j = 0; j < chunk.Entities.Count; j++ )
                    {                
                        if ( chunk.Entities[j].ReadyToDragTractoredShipsAway )
                        {
                            if ( moveCommand == null )
                                moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_AIThreatTractor], GameCommandSource.AnythingElse );
                            moveCommand.RelatedEntityIDs.Add(chunk.Entities[j].PrimaryKeyID);
                            if ( chunk.Entities[j].Orders.Behavior != EntityBehaviorType.Attacker_Full )
                            {
                                if ( threatCommand == null )
                                {
                                    threatCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetBehavior_FromFaction_RunAwayWithEnemyShips], GameCommandSource.AnythingElse );
                                    threatCommand.RelatedMagnitude = (int)EntityBehaviorType.Attacker_Full;
                                }
                                threatCommand.RelatedEntityIDs.Add( chunk.Entities[j].PrimaryKeyID );
                            }
                        }
                    }
                }
            }
            if ( moveCommand == null )
            {
                //nothing to do
                if ( threatCommand != null )
                    threatCommand.ReturnToPool();
                return;
            }

            //find the planet to drag to
            Planet dragTarget = null;
            int strengthOnCurrentBest = -999999999;
            foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
            {
                var neighborFactionData = neighbor.GetStanceDataForFaction( AttachedFaction );
                int myStrengthOnNeighbor = neighborFactionData[FactionStance.Self].TotalStrength;
                int alliedStrengthOnNeighbor = neighborFactionData[FactionStance.Friendly].TotalStrength;
                int friendlyStrength = myStrengthOnNeighbor + alliedStrengthOnNeighbor;
                int hostileStrength = neighborFactionData[FactionStance.Hostile].TotalStrength;
                if ( strengthOnCurrentBest < friendlyStrength - hostileStrength )
                {
                    dragTarget = neighbor;
                    strengthOnCurrentBest = friendlyStrength - hostileStrength;
                }
            }
            if ( dragTarget == null) //this shouldn't happen
                return;
            moveCommand.RelatedIntegers.Add( dragTarget.Index );
            moveCommand.RelatedString = "AI_THREAT_TRAC";

            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );
            if ( threatCommand != null )
            {
                threatCommand.RelatedFactionIndex = planet.GetPlanetFactionForFaction(AttachedFaction).GetIndexOfMostAnnoyingFaction( Context );
                if ( GameSettings.Current.GetBoolBySetting( "ThreatDebug" ) )
                {
                    Faction logFaction = World_AIW2.Instance.GetFactionByIndex(threatCommand.RelatedFactionIndex);
                    if ( logFaction != null )
                        ArcenDebugging.ArcenDebugLogSingleLine( " ships are to go after " + logFaction.GetDisplayName() + " X", Verbosity.DoNotShow );
                    else
                        ArcenDebugging.ArcenDebugLogSingleLine( " ships are to go after -1 X", Verbosity.DoNotShow );
                }
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, threatCommand, false );
            }
        }
        
        private void Helper_MakeThreatShipsFight( ArcenLongTermIntermittentPlanningContext Context, Planet planet,  bool tracing, ArcenCharacterBuffer tracingBuffer, ThreatChunkCollection threatChunks )
        {
            //These threat ships may have been Waiting against another planet, so clear that Wait state so they can fight normally
            //We may also need to set the Behaviour to Attacker_Full
            //We generate the two Commands (setWait and , then apply the commands to any necessary ships
            int debugCode = 0;
            try{
            debugCode = 100;
            PlanetFaction planetFaction = planet.GetPlanetFactionForFaction ( AttachedFaction );
            GameCommand clearWaitCommand = null;
            debugCode = 200;
            //Now tell that threat to fight
            GameCommand threatFightCommand = null;

            foreach ( KeyValuePair<short, ThreatChunk> kv in threatChunks.Chunks )
            {
                debugCode = 300;
                ThreatChunk chunk = kv.Value;
                if(chunk.TotalStrength > 0)
                {
                    debugCode = 400;
                    for ( int k = 0; k < chunk.Entities.Count; k++ )
                    {
                        debugCode = 500;
                        GameEntity_Squad squad = chunk.Entities[k].GetSquad();
                        if ( squad == null ||
                             squad.GetHasBeenDestroyed() )
                            continue;
                        if ( squad.WaitingAgainstPlanetIndex != -1 )
                        {
                            if ( clearWaitCommand == null )
                                clearWaitCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWaiting], GameCommandSource.AnythingElse );
                            clearWaitCommand.RelatedEntityIDs.Add( squad.PrimaryKeyID );
                        }
                        debugCode = 600;
                        if ( squad.Orders.Behavior != EntityBehaviorType.Attacker_Full ||
                             squad.WaitingAgainstPlanetIndex != -1 ) //if we are resetting the Wait then it will also reset Behaviour
                        {
                            if ( threatFightCommand == null )
                                threatFightCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetBehavior_FromFaction_ThreatTimeToFight], GameCommandSource.AnythingElse );
                            threatFightCommand.RelatedEntityIDs.Add( squad.PrimaryKeyID );
                        }
                    }
                }
            }
            debugCode = 700;
            if ( clearWaitCommand != null )
            {
                clearWaitCommand.RelatedIntegers.Add( -1 );
                clearWaitCommand.RelatedBool = true; //discard existing orders so they can fight
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, clearWaitCommand, false );
            }
            debugCode = 800;
            if ( threatFightCommand != null )
            {
                debugCode = 900;
                threatFightCommand.RelatedMagnitude = (int)EntityBehaviorType.Attacker_Full;
                threatFightCommand.RelatedFactionIndex = planetFaction.GetIndexOfMostAnnoyingFaction( Context );
                if ( GameSettings.Current.GetBoolBySetting( "ThreatDebug" ) )
                {
                    Faction logFaction = World_AIW2.Instance.GetFactionByIndex(threatFightCommand.RelatedFactionIndex);
                    if ( logFaction != null )
                        ArcenDebugging.ArcenDebugLogSingleLine("ships on " + planet.Name + " are to go after " + logFaction.GetDisplayName() + " threat fight on " + planet.Name, Verbosity.DoNotShow );
                    else
                        ArcenDebugging.ArcenDebugLogSingleLine("ships on " + planet.Name + " are to go after -1 threat fight on " + planet.Name, Verbosity.DoNotShow );
                }
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, threatFightCommand, false );
            }
            } catch( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in Helper_MakeThreatShipsFight debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        
        private static void Helper_HunkerDown( ArcenLongTermIntermittentPlanningContext Context, Planet planet, bool tracing, ArcenCharacterBuffer tracingBuffer, List<SafeSquadWrapper> guardShips, Faction faction )
        {
            //If we are badly outnumbered, everyone cower near the strongest guard post and make the humans pay to defeat us
            #region Tracing
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "guards hunker!" );
            #endregion
            //if the command station is dead, don't bother
            GameEntity_Squad commandStation = planet.GetCommandStationOrNull();
            if(commandStation == null)
                return;

            GameEntity_Squad strongestGuardPost = null;
            //Find the safest place to hunker down
            foreach ( GameEntity_Squad guardPost in planet.Squads( EntityRollupType.ReinforcementLocations ) )
            {
                if ( ! guardPost.GetIsFriendlyTowards_Safe(faction) )
                    continue;
                if ( strongestGuardPost == null || guardPost.TypeData.BaseMark.StrengthPerSquad_CalculatedWithNullFleetMembership > strongestGuardPost.TypeData.BaseMark.StrengthPerSquad_CalculatedWithNullFleetMembership )
                    strongestGuardPost = guardPost;
            }
            if ( strongestGuardPost == null )
                return;

            if ( guardShips.Count > 0 )
            {
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_BaseAIHunkerDown], GameCommandSource.AnythingElse );
                ArcenPoint hidingPoint = strongestGuardPost.WorldLocation;
                command.RelatedPoints.Add( hidingPoint );
                for ( int k = 0; k < guardShips.Count; k++ )
                    command.RelatedEntityIDs.Add( guardShips[k].PrimaryKeyID );
                World_AIW2.Instance.QueueGameCommand( faction, command, false );
            }
            //bool setAttack = true;
            //if ( setAttack )
            //{
            //    command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetBehavior] );
            //    command.RelatedEntityIDs = List<Int64>.Create_WillNeverBeGCed();
            //    command.RelatedMagnitude = (int)EntityBehaviorType.Attacker;
            //    for ( int k = 0; k < guardShips.Count; k++ )
            //        command.RelatedEntityIDs.Add( guardShips[k].PrimaryKeyID );
            //    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction,  command, false );
            //}
        }

        private void AnalyzeFriendlyToHostileBalance(bool tracing, ArcenCharacterBuffer tracingBuffer, Planet targetPlanet, out int currentLevel, out int threshold )
        {
            int debugCode = 0;
            try{
                debugCode = 100;
                AITypeData aiType = this.BaseInfo.SentinelInfo.AIType;
                debugCode = 200;
                var factionData = targetPlanet.GetStanceDataForFaction( AttachedFaction );
                debugCode = 300;
                if ( factionData == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Cross threading issue?  factionData for " + targetPlanet.Name + " is null. ToBeDestroyed " + targetPlanet.IsPlanetToBeDestroyed + ", " + targetPlanet.HasPlanetBeenDestroyed, Verbosity.DoNotShow );
                    currentLevel = 0;
                    threshold = 0;
                    return;//cross threading issue!
                }
                StrengthData_PlanetFaction_Stance myStrengthData = factionData[FactionStance.Self];
                StrengthData_PlanetFaction_Stance friendlyStrengthData = factionData[FactionStance.Friendly];
                StrengthData_PlanetFaction_Stance hostileStrengthData = factionData[FactionStance.Hostile];
                debugCode = 400;
                int friendlyStrengthPresent = myStrengthData.TotalStrength + friendlyStrengthData.TotalStrength;
                int friendlyStrengthWaiting = myStrengthData.WaitingStrength + friendlyStrengthData.WaitingStrength;
                int friendlyStrengthIncoming = myStrengthData.IncomingStrength + friendlyStrengthData.IncomingStrength;
                int hostileStrengthPresent = hostileStrengthData.TotalStrength;
                int nearbyUnengagedHostileMobileStrength = 0;
                int divisor = 2;
                debugCode = 500;
                for ( int j = 1; j < hostileStrengthData.UnengagedMobileStrengthByHopCount.Length; j++, divisor *= 3 ) // starting with 1 to ignore what's on this planet itself
                    nearbyUnengagedHostileMobileStrength += hostileStrengthData.UnengagedMobileStrengthByHopCount[j] / divisor;
                currentLevel = friendlyStrengthPresent + friendlyStrengthWaiting + friendlyStrengthIncoming;
                currentLevel = (this.BaseInfo.SentinelInfo.AIDifficulty.OverconfidenceRatio * currentLevel).IntValue;
                threshold = ( (hostileStrengthPresent  +  aiType.RatioOfSuperiorityAtWhichThreatActuallyAttacks) + (nearbyUnengagedHostileMobileStrength * aiType.RatioOfFearOfRemoteEnemies )).GetNearestIntPreferringHigher();
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n\t" ).Add( "friendlyStrengthPresent=" ).Add( friendlyStrengthPresent );
                if ( tracing ) tracingBuffer.Add( "\n\t" ).Add( "friendlyStrengthWaiting=" ).Add( friendlyStrengthWaiting );
                if ( tracing ) tracingBuffer.Add( "\n\t" ).Add( "friendlyStrengthIncoming=" ).Add( friendlyStrengthIncoming );
                if ( tracing ) tracingBuffer.Add( "\n\t" ).Add( "hostileStrengthPresent=" ).Add( hostileStrengthPresent );
                if ( tracing ) tracingBuffer.Add( "\n\t" ).Add( "nearbyUnengagedHostileMobileStrength=" ).Add( nearbyUnengagedHostileMobileStrength );
                if ( tracing ) tracingBuffer.Add( "\n\t" ).Add( "RatioOfSuperiorityAtWhichThreatActuallyAttacks=" ).Add( aiType.RatioOfSuperiorityAtWhichThreatActuallyAttacks.ReadableString );
                #endregion
            }catch(Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in AnalyzeFriendlyToHostileBalance debugCode " + debugCode + " " + e, Verbosity.ShowAsError );
                threshold = 0;
                currentLevel = 0;
            }
        }

        private class ThreatChunk : ConcurrentPoolable<ThreatChunk>, IProtectedListable
        {
            public Int16 TargetFactionIndex = -1; //-1 means "General Use Threat"
            public FInt TotalStrength;
            public readonly List<SafeSquadWrapper> Entities =List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "AISentinelsFactionDeepInfo-ThreatChunk-Entities" );

            public Faction TargetFaction {
                get {
                    Faction targetFaction = World_AIW2.Instance.GetFactionByIndex( this.TargetFactionIndex );
                    if ( targetFaction?.Type == FactionType.Player )
                    {
                        return null; // just use the default logic, which will target the humans
                    }
                    return targetFaction;
                }
            }

            public void AddEntity( GameEntity_Squad entity )
            {
                this.TotalStrength += entity.GetStrengthOfSelfAndContents();
                // if(entity.Planet == Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed())
                //     ArcenDebugging.ArcenDebugLogSingleLine("Adding " + entity.TypeData.InternalName + " " +  entity.PrimaryKeyID + " on " +entity.GetPlanetName_Safe() + " to threat with strength + " (squad) " + entity.GetStrengthPerSquad() + " (total) " +  (entity.GetStrengthOfSelfAndContents()) + ". Chunk running total: " + this.TotalStrength + " target faction " + entity.Orders.BehaviorRelatedFactionIndex, Verbosity.DoNotShow );
                this.Entities.Add( entity );
            }

            public void RemoveEntity( GameEntity_Squad entity )
            {
                this.TotalStrength -= entity.GetStrengthOfSelfAndContents();
                this.Entities.Remove( entity );
            }

            #region Pooling
            public static ThreatChunk GetFromPoolOrCreate()
            {
                ThreatChunk data = Pool.GetFromPoolOrCreate();
                return data;
            }

            private static ReferenceTracker RefTracker;
            private ThreatChunk()
            {
                if ( RefTracker == null )
                    RefTracker = new ReferenceTracker( "ThreatChunks" );
                RefTracker.IncrementObjectCount();
            }
            private static readonly ConcurrentPool<ThreatChunk> Pool = new ConcurrentPool<ThreatChunk>( "ThreatChunk", 30000,
                KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new ThreatChunk(); } );

            public ThreatChunk CreateNewForPool()
            {
                return new ThreatChunk();
            }

            public void ReturnToPool()
            {
                Pool.ReturnToPool( this );
            }

            public override void DoAnyBelatedCleanupWhenComingOutOfPool()
            {
            }

            public override void DoEarlyCleanupWhenGoingBackIntoPool()
            {
                this.Entities.Clear();
                this.TargetFactionIndex = -1;
                this.TotalStrength = FInt.Zero;
            }

            //IProtectedDictionaryable
            public void DoBeforeRemoveOrClear()
            {
                Pool.ReturnToPool( this );
            }
            #endregion
        }

        private class ThreatChunkCollection : ConcurrentPoolable<ThreatChunkCollection>
        {
            public readonly ProtectedValDictionary<Int16, ThreatChunk> Chunks = ProtectedValDictionary<Int16, ThreatChunk>.Create_WillNeverBeGCed( 20000, "AISentinelsFactionDeepInfo-ThreatChunkCollection-Chunks" );

            #region Pooling
            public static ThreatChunkCollection GetFromPoolOrCreate()
            {
                ThreatChunkCollection data = Pool.GetFromPoolOrCreate();
                return data;
            }

            private static readonly ReferenceTracker RefTracker = new ReferenceTracker( "ThreatChunkCollections" );
            private ThreatChunkCollection()
            {
                if ( RefTracker != null ) //it will be null for the two above in the static definitions
                    RefTracker.IncrementObjectCount();
            }
            private static readonly ConcurrentPool<ThreatChunkCollection> Pool = new ConcurrentPool<ThreatChunkCollection>( "ThreatChunkCollection", 30000, 
                 KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new ThreatChunkCollection(); } );

            public ThreatChunkCollection CreateNewForPool()
            {
                return new ThreatChunkCollection();
            }

            public void ReturnToPool()
            {
                Pool.ReturnToPool( this );
            }

            public override void DoAnyBelatedCleanupWhenComingOutOfPool()
            {
            }

            public override void DoEarlyCleanupWhenGoingBackIntoPool()
            {
                this.Chunks.Clear();
            }

            //IProtectedDictionaryable
            public void DoBeforeRemoveOrClear()
            {
                Pool.ReturnToPool( this );
            }
            #endregion

            public void AddEntity( GameEntity_Squad entity )
            {
                Int16 targetFactionIndex = entity.Orders.BehaviorRelatedFactionIndex;
                Faction targetFaction = World_AIW2.Instance.GetFactionByIndex(targetFactionIndex);
                if (targetFaction == null || targetFaction.Type == FactionType.Player)
                    targetFactionIndex = -1; //-1 means "general purpose threat", not against a specific minor faction
                if ( !this.Chunks.ContainsKey( targetFactionIndex ) )
                {
                    this.Chunks[targetFactionIndex] = ThreatChunk.GetFromPoolOrCreate();
                    this.Chunks[targetFactionIndex].TargetFactionIndex = targetFactionIndex;
                }
                this.Chunks[targetFactionIndex].AddEntity( entity );
            }

            public bool GetHasAnything()
            {
                foreach ( KeyValuePair<short, ThreatChunk> kv in this.Chunks )
                    if ( kv.Value.Entities.Count > 0 )
                        return true;
                return false;
            }

            public int GetTotalCount()
            {
                int result = 0;
                foreach ( KeyValuePair<short, ThreatChunk> kv in this.Chunks )
                    result += kv.Value.Entities.Count;
                return result;
            }
        }

        private void Helper_SendThreatOnRaid(bool tracing, ArcenCharacterBuffer tracingBuffer, ThreatChunkCollection chunks,  Galaxy galaxy, Planet planet, 
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            foreach ( KeyValuePair<short, ThreatChunk> kv in chunks.Chunks )
            {
                //Each chunk is focused on a different minor faction
                ThreatChunk chunk = kv.Value;
                Helper_SendThreatOnRaid( tracing, tracingBuffer, chunk, galaxy, planet, Context, PathCacheData );
            }
        }

        private void Helper_SendThreatOnRaid(bool tracing, ArcenCharacterBuffer tracingBuffer, ThreatChunk chunk, Galaxy galaxy, Planet planet, 
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            List<Planet> working_potentialAttackTargets = null;
            List<Planet> working_highPriorityPotentialAttackTargets = null;
            List<Planet> working_overridinglyHighPriorityPotentialAttackTargets = null;
            List<Planet> working_planetsToCheckInFlood = null;
            List<Planet> pathFinal = null;
            List<RefPair<Planet, int>> working_potentialAttackTargetWithDesirabilities = null;
            
            int debugCode = 0;
            try
            {
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Actual number of threat units to work with:" ).Add( chunk.Entities.Count ).Add(" on ").Add(planet.Name);
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Actual threat strength to work with:" ).Add( chunk.TotalStrength.ReadableString );
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Target Faction Index:" ).Add( chunk.TargetFactionIndex );
                if ( tracing && chunk.TargetFactionIndex != -1 )
                {
                    Faction TargetFaction = World_AIW2.Instance.GetFactionByIndex( chunk.TargetFactionIndex );
                    tracingBuffer.Add( " type " + TargetFaction.Type );
                    if ( TargetFaction.Type == FactionType.SpecialFaction )
                        tracingBuffer.Add( " " ).Add( TargetFaction.GetDisplayNameWithoutPlayerNames() );
                }
                debugCode = 100;
                #endregion
                if ( chunk.TotalStrength <= 0 )
                    return;
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                    return;
                AITypeData aiType = this.BaseInfo.SentinelInfo.AIType;
                FInt maximumTolerableTraversalDifficulty = chunk.TotalStrength / aiType.RatioOfSuperiorityAtWhichThreatActuallyAttacks;
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Intrigued Query: Which parties can I crash? I'll travel across up to " ).Add( maximumTolerableTraversalDifficulty.ReadableString ).Add( " difficulty" );
                #endregion
                debugCode = 200;
                Faction targetFaction = World_AIW2.Instance.GetFactionByIndex( chunk.TargetFactionIndex );
                if ( targetFaction != null && targetFaction.Type == FactionType.Player )
                {
                    targetFaction = null; // just use the default logic, which will target the humans
                }
                debugCode = 300;
                foreach ( Planet otherPlanet in World_AIW2.Instance.Planets( false ) )
                {
                    otherPlanet.AIPlanning_CheapestRaidPathToHereComesFrom = null;
                    otherPlanet.AIPlanning_CheapestRaidPathToHereCost = 0;
                }

                working_potentialAttackTargets = Planet.GetTemporaryPlanetList( "AISent-Helper_SendThreatOnRaid-working_potentialAttackTargets", 10f );
                if ( working_potentialAttackTargets == null ) //blocked for teardown/shutdown; bail
                    return;
                working_highPriorityPotentialAttackTargets = Planet.GetTemporaryPlanetList( "AISent-Helper_SendThreatOnRaid-working_highPriorityPotentialAttackTargets", 10f );
                if ( working_highPriorityPotentialAttackTargets == null ) //blocked for teardown/shutdown; bail
                    return;
                working_overridinglyHighPriorityPotentialAttackTargets = Planet.GetTemporaryPlanetList( "AISent-Helper_SendThreatOnRaid-working_overridinglyHighPriorityPotentialAttackTargets", 10f );
                if ( working_overridinglyHighPriorityPotentialAttackTargets == null ) //blocked for teardown/shutdown; bail
                    return;
                working_planetsToCheckInFlood = Planet.GetTemporaryPlanetList( "AISent-Helper_SendThreatOnRaid-working_planetsToCheckInFlood", 10f );
                if ( working_planetsToCheckInFlood == null ) //blocked for teardown/shutdown; bail
                    return;

                //Note that this logic will be ignored if the AI King is under attack
                working_planetsToCheckInFlood.Add( planet );
                planet.AIPlanning_CheapestRaidPathToHereComesFrom = planet;
                for ( int k = 0; k < working_planetsToCheckInFlood.Count; k++ )
                {
                    debugCode = 400;
                    Planet floodPlanet = working_planetsToCheckInFlood[k];
                    if ( floodPlanet == null )
                        continue;
                    if ( floodPlanet.AIPlanning_CheapestRaidPathToHereCost > maximumTolerableTraversalDifficulty )
                    {
                        EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> factionData = floodPlanet.GetStanceDataForFaction( AttachedFaction );
                        if ( factionData == null )
                            continue;
                        StrengthData_PlanetFaction_Stance hostileStrengthData = factionData[FactionStance.Hostile];
                        if ( hostileStrengthData == null )
                            continue;
                        if ( hostileStrengthData.TotalStrength > 0 )
                        {
                            #region Tracing
                            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "not trying to flood-search through " ).Add( floodPlanet.Name ).Add( " due to cumulative difficulty " ).Add( floodPlanet.AIPlanning_CheapestRaidPathToHereCost );
                            #endregion
                            continue;
                        }
                    }
                    #region Tracing
                    //if ( tracing ) tracingBuffer.Add( "\n" ).Add( "flood-searching through " ).Add( floodPlanet.Name );
                    //if ( tracing ) tracingBuffer.Add( "(desirability=" ).Add( floodPlanet.LongRangePlanningData.RaidDesirabilityByFactionIndex[faction.FactionIndex].ReadableString );
                    //if ( tracing ) tracingBuffer.Add( ",difficulty=" ).Add( floodPlanet.LongRangePlanningData.RaidDifficultyByFactionIndex[faction.FactionIndex].ReadableString );
                    //if ( tracing ) tracingBuffer.Add( ",cost_to_get_here=" ).Add( floodPlanet.AIPlanning_CheapestRaidPathToHereCost.ReadableString ).Add( ")" );
                    #endregion
                    debugCode = 500;
                    foreach ( Planet neighbor in floodPlanet.LinkedNeighbors( false ) )
                    {
                        debugCode = 600;
                        EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> factionData = neighbor.GetStanceDataForFaction( AttachedFaction );
                        StrengthData_PlanetFaction_Stance myStrengthData = factionData[FactionStance.Self];
                        StrengthData_PlanetFaction_Stance friendlyStrengthData = factionData[FactionStance.Friendly];
                        StrengthData_PlanetFaction_Stance hostileStrengthData = factionData[FactionStance.Hostile];
                        int friendlyStrengthTotal = myStrengthData.TotalStrength + friendlyStrengthData.TotalStrength;
                        int totalCostFromOriginToNeighbor = floodPlanet.AIPlanning_CheapestRaidPathToHereCost + this.BaseInfo.GetRaidTraversalDifficulty( neighbor );
                        if ( !working_potentialAttackTargets.Contains( neighbor ) )
                        {
                            debugCode = 700;
                            if ( targetFaction != null )
                            {
                                // if we have a target faction, it's non-human and we're just trying to do something with these annoyed AI ships that isn't (necessarily) crashing human planets
                                EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> targetFactionData = neighbor.GetStanceDataForFaction( targetFaction.FactionIndex );
                                if ( targetFactionData[FactionStance.Self].TotalStrength <= 0 )
                                {
                                    #region Tracing
                                    //if ( tracing ) tracingBuffer.Add( "\n" ).Add( "refusing to attack " ).Add( neighbor.Name ).Add( " due to target faction " ).Add( targetFaction.SpecialFactionData.InternalName ).Add(" having no presence there" );
                                    #endregion
                                }
                                else
                                {
                                    working_potentialAttackTargets.Add( neighbor );
                                    #region Tracing
                                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "adding attack-target " ).Add( neighbor.Name ).Add( " because target faction " ).Add( targetFaction.SpecialFactionData.InternalName ).Add( " is present there" );
                                    #endregion
                                }
                            }
                            else
                            {
                                if ( this.BaseInfo.GetRaidDesirability( neighbor ) <= 0 )
                                {
                                    #region Tracing
                                    //if ( tracing ) tracingBuffer.Add( "\n" ).Add( "refusing to attack " ).Add( neighbor.Name ).Add( " due to desirability " ).Add( neighbor.LongRangePlanningData.RaidDesirabilityByFactionIndex[faction.FactionIndex].ReadableString );
                                    #endregion
                                }
                                else if ( !hostileStrengthData.HasKingUnitPresent &&
                                          (hostileStrengthData.TotalStrengthIncludingNonMilitary * aiType.RatioOfSuperiorityAtWhichThreatOverruns < friendlyStrengthTotal) )
                                {
                                    #region Tracing
                                    if ( tracing )
                                        tracingBuffer.Add( "\n" ).Add( "refusing to attack " ).Add( neighbor.Name )
                                            .Add( " due to local ai/infestation strength balance already in overrun status (" )
                                            .Add( hostileStrengthData.TotalStrength ).Add( " / " )
                                            .Add( friendlyStrengthTotal ).Add( ")" );
                                    #endregion
                                }
                                else
                                {
                                    if ( hostileStrengthData.HasKingUnitPresent )
                                        working_overridinglyHighPriorityPotentialAttackTargets.Add( neighbor );
                                    if ( this.Helper_HasValuablePlayerStructure( neighbor ) )
                                        working_highPriorityPotentialAttackTargets.Add( neighbor );
                                    working_potentialAttackTargets.Add( neighbor );
                                    #region Tracing
                                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "adding attack-target " ).Add( neighbor.Name );
                                    #endregion
                                }
                            }
                        }
                        if ( neighbor.AIPlanning_CheapestRaidPathToHereComesFrom != null &&
                             neighbor.AIPlanning_CheapestRaidPathToHereCost <= totalCostFromOriginToNeighbor )
                        {
                        #region Tracing
                        //if ( tracing ) tracingBuffer.Add( "\n" ).Add( "skipping re-checking neighbors of " ).Add( neighbor.Name ).Add( " because path is not cheaper this way" );
                        #endregion
                        continue;
                        }
                        neighbor.AIPlanning_CheapestRaidPathToHereComesFrom = floodPlanet;
                        neighbor.AIPlanning_CheapestRaidPathToHereCost = totalCostFromOriginToNeighbor;
                        working_planetsToCheckInFlood.Add( neighbor );
                    }
                }
                debugCode = 800;
                /* Override if AI King is under attack */
                bool hasKingGoneMobile = false;
                Planet KingPlanet = GetKingPlanet( out hasKingGoneMobile );
                debugCode = 801;
                bool KingUnderAttack = GetIsKingUnderAttack( KingPlanet ) || hasKingGoneMobile; //if we are in phase 2 of the overlord fight, then act like it's under attack at all times
                debugCode = 802;
                if ( KingUnderAttack )
                {
                    debugCode = 803;
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Override: AI King under attack, threat units converge" );
                    #endregion
                    working_potentialAttackTargets.Clear();
                    working_highPriorityPotentialAttackTargets.Clear();
                    working_overridinglyHighPriorityPotentialAttackTargets.Clear();
                    working_potentialAttackTargets.Add( KingPlanet );
                }                

                debugCode = 804;
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "potentialAttackTargets.Count:" ).Add( working_potentialAttackTargets.Count );
                #endregion
                if ( working_potentialAttackTargets.Count <= 0 )
                {
                    return;
                }
                if ( working_overridinglyHighPriorityPotentialAttackTargets.Count > 0 )
                {
                    debugCode = 900;
                    //In general we want to go for overridingly high priority targets (right now this is just King planets), but
                    //if there are some much weaker nearby targets it makes sense to take out
                    //the weaker planets first, then siege the enemy homeworld
                    int StrengthOfWeakestHighPriority = -1;
                    int StrengthOfWeakestNormalPriority = -1;
                    for ( int i = 0; i < working_overridinglyHighPriorityPotentialAttackTargets.Count; i++ )
                    {
                        var factionData = working_overridinglyHighPriorityPotentialAttackTargets[i].GetStanceDataForFaction( AttachedFaction );
                        StrengthData_PlanetFaction_Stance hostileStrengthData = factionData[FactionStance.Hostile];
                        if ( i == 0 || StrengthOfWeakestHighPriority > hostileStrengthData.TotalStrength )
                            StrengthOfWeakestHighPriority = hostileStrengthData.TotalStrength;
                    }
                    for ( int i = 0; i < working_potentialAttackTargets.Count; i++ )
                    {
                        var factionData = working_potentialAttackTargets[i].GetStanceDataForFaction( AttachedFaction );
                        StrengthData_PlanetFaction_Stance hostileStrengthData = factionData[FactionStance.Hostile];
                        if ( i == 0 || StrengthOfWeakestNormalPriority > hostileStrengthData.TotalStrength )
                            StrengthOfWeakestNormalPriority = hostileStrengthData.TotalStrength;
                    }

                    if ( Context.RandomToUse.Next( 0, 100 ) < 80 &&
                        StrengthOfWeakestNormalPriority * aiType.RatioForPrefeferringOverridingHighPriorityTargets > StrengthOfWeakestHighPriority )
                    {
                        working_potentialAttackTargets.Clear();
                        working_highPriorityPotentialAttackTargets.Clear();
                        working_potentialAttackTargets.AddRange( working_overridinglyHighPriorityPotentialAttackTargets );
                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "an overridingly-high-priority planet (i.e. with enemy king-unit) was available, and we passed the 80% random roll, and there are no other targets that are much more tempting, so ignore other planets" );
                        #endregion
                    }
                    else
                    {
                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "an overridingly-high-priority planet (i.e. with enemy king-unit) was available, but either we did not pass the 80% random roll or there are other tempting targets, so considering all possible targets (may still pick the king unit one)" );
                        #endregion
                    }
                }
                debugCode = 1000;
                if ( working_highPriorityPotentialAttackTargets.Count > 0 )
                {
                    debugCode = 1100;
                    //note this array gets cleared if we have chosen to use overridinglyHighPriorityPotentialAttackTargets
                    //This mirrors the above logic for overridingly high priority planets above
                    int StrengthOfWeakestHighPriority = -1;
                    int StrengthOfWeakestNormalPriority = -1;
                    for ( int i = 0; i < working_highPriorityPotentialAttackTargets.Count; i++ )
                    {
                        var factionData = working_highPriorityPotentialAttackTargets[i].GetStanceDataForFaction( AttachedFaction );
                        StrengthData_PlanetFaction_Stance hostileStrengthData = factionData[FactionStance.Hostile];
                        if ( i == 0 || StrengthOfWeakestHighPriority > hostileStrengthData.TotalStrength )
                            StrengthOfWeakestHighPriority = hostileStrengthData.TotalStrength;
                    }
                    for ( int i = 0; i < working_potentialAttackTargets.Count; i++ )
                    {
                        var factionData = working_potentialAttackTargets[i].GetStanceDataForFaction( AttachedFaction );
                        StrengthData_PlanetFaction_Stance hostileStrengthData = factionData[FactionStance.Hostile];
                        if ( i == 0 || StrengthOfWeakestNormalPriority > hostileStrengthData.TotalStrength )
                            StrengthOfWeakestNormalPriority = hostileStrengthData.TotalStrength;
                    }

                    if ( Context.RandomToUse.Next( 0, 100 ) < 80 &&
                        StrengthOfWeakestNormalPriority * aiType.RatioForPrefeferringHighPriorityTargets > StrengthOfWeakestHighPriority )
                    {
                        working_potentialAttackTargets.Clear();
                        working_potentialAttackTargets.AddRange( working_highPriorityPotentialAttackTargets );
                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "an overridingly-high-priority planet (i.e. with enemy king-unit) was available, and we passed the 80% random roll, and there are no other targets that are much more tempting, so ignore other planets" );
                        #endregion
                    }
                    else
                    {
                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "an overridingly-high-priority planet (i.e. with enemy king-unit) was available, but either we did not pass the 80% random roll or there are other tempting targets, so considering all possible targets (may still pick the king unit one)" );
                        #endregion
                    }
                }
                debugCode = 1200;
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "picking a target" );
                #endregion                
                working_potentialAttackTargets.Sort( static delegate ( Planet Left, Planet Right )
                {
                    return Left.AIPlanning_CheapestRaidPathToHereCost.CompareTo( Right.AIPlanning_CheapestRaidPathToHereCost );
                } );

                int lastIndexToRetain = working_potentialAttackTargets.Count / 4;
                for ( int k = lastIndexToRetain + 1; k < working_potentialAttackTargets.Count; k++ )
                    working_potentialAttackTargets.RemoveAt( k-- );

                if ( targetFaction == null )
                {
                    if ( working_potentialAttackTargets.Count > 1 )
                    {
                        //sort them if there are at least two!
                        working_potentialAttackTargetWithDesirabilities = Planet.GetTemporaryPlanetRefPairIntList( "AISent-Helper_SendThreatOnRaid-working_potentialAttackTargetWithDesirabilities", 10f );
                        if ( working_potentialAttackTargetWithDesirabilities == null ) //blocked for teardown/shutdown; bail
                            return;

                        foreach ( Planet plan in working_potentialAttackTargets )
                        {
                            //GetRaidDesirability() is to expensive to call every loop in a sort,
                            //AND it might have results that change, which would cause an exception.
                            //this is far more performant
                            RefPair<Planet, int> desirability = RefPair<Planet, int>.Create( plan, this.BaseInfo.GetRaidDesirability( plan ) );
                            working_potentialAttackTargetWithDesirabilities.Add( desirability );
                        }

                        working_potentialAttackTargetWithDesirabilities.Sort( static delegate ( RefPair<Planet, int> Left, RefPair<Planet, int> Right )
                        {
                            return Right.RightItem.CompareTo( Left.RightItem ); //descending by desirability
                        } );

                        //make room to add the sorted ones back in
                        working_potentialAttackTargets.Clear();
                        //now add them back
                        foreach ( RefPair<Planet, int> kv in working_potentialAttackTargetWithDesirabilities )
                        {
                            working_potentialAttackTargets.Add( kv.LeftItem );
                        }
                    }
                }
                debugCode = 1400;
                Planet threatTarget = null;
                for ( int k = 0; k < working_potentialAttackTargets.Count; k++ )
                {
                    if ( Context.RandomToUse.NextBool() )
                        continue;
                    threatTarget = working_potentialAttackTargets[k];
                    break;
                }
                debugCode = 1500;
                if ( threatTarget == null )
                {
                    debugCode = 1510;
                    threatTarget = working_potentialAttackTargets[0];
                }
                debugCode = 1520;
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "path from " ).Add( planet.Name ).Add( " to " ).Add( threatTarget.Name ).Add( ":" );
                #endregion

                pathFinal = Planet.GetTemporaryPlanetList( "AISent-Helper_SendThreatOnRaid-pathFinal", 10f );
                if ( pathFinal == null ) //blocked for teardown/shutdown; bail
                    return;

                Planet workingPlanet = threatTarget;
                int attemptsLeft = 1000;
                debugCode = 1530;
                string aiRaidType = "AI_GO_RAID_KING";
                Planet originPlanet = planet;
                if ( KingUnderAttack )
                {
                    //this is a bit of a hacky code path, so AIPlanning_CheapestRaidPathToHereComesFrom
                    //may well not be set for this. Just use a pathfinder
                    PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "SentinelsHelper_SendThreatOnRaid", 
                        originPlanet, threatTarget, PathingMode.Default, Context, PathCacheData );
                    if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                    {
                        pathFinal.AddRange( pathCache.PathToReadOnly );
                    }
                }
                else
                {
                    aiRaidType = "AI_GO_RAID_NOKING";
                    while ( workingPlanet != originPlanet && attemptsLeft > 0 && workingPlanet != null )
                    {
                        debugCode = 1540;
                        attemptsLeft--;
                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( workingPlanet.Name ).Add( "<=" );
                        #endregion
                        //we are writing to a fake readonly path here, but later we need to consider it readonly 
                        //because of using the PathBetweenPlanetsForFaction in another branch
                        pathFinal.Insert( 0, workingPlanet );
                        debugCode = 1550;
                        workingPlanet = workingPlanet.AIPlanning_CheapestRaidPathToHereComesFrom;
                    }
                }
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "path.Count:" ).Add( pathFinal.Count );
                #endregion
                debugCode = 1600;
                if ( pathFinal.Count > 0 )
                {
                    GameCommand wormholeCommand = null;
                    debugCode = 1610;
                    for ( int k = 0; k < chunk.Entities.Count; k++ )
                    {
                        if ( chunk.Entities[k].GetSquad() != null )
                        {
                            if ( chunk.Entities[k].Planet == originPlanet )
                            {
                                if ( wormholeCommand == null )
                                    wormholeCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_AIThreatRaid], GameCommandSource.AnythingElse );
                                wormholeCommand.RelatedFactionIndex = AttachedFaction.FactionIndex;
                                wormholeCommand.RelatedEntityIDs.Add( chunk.Entities[k].PrimaryKeyID );
                            }
                            else
                                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: ThreatChunk-1610 Trying to send AI ships from planet " + originPlanet.Name +
                                    " when ship was on " + chunk.Entities[k].GetPlanetName_Safe(), Verbosity.DoNotShow );
                        }
                    }
                    debugCode = 1620;
                    if ( wormholeCommand != null )
                    {
                        wormholeCommand.RelatedString = aiRaidType;
                        for ( int k = 0; k < pathFinal.Count; k++ )
                            wormholeCommand.RelatedIntegers.Add( pathFinal[k].Index );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, wormholeCommand, false );

                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "command queued:" ).Add( wormholeCommand.RelatedEntityIDs.Count ).Add( ":" ).Add( wormholeCommand.RelatedIntegers.Count );
                        #endregion
                    }
                    debugCode = 1630;
                    Planet firstPlanetInPath = pathFinal[0];
                    if ( this.BaseInfo.GetRaidTraversalDifficulty( firstPlanetInPath ) >= maximumTolerableTraversalDifficulty &&
                        chunk.Entities.Count > 0 ) //don't bother if no entities
                    {
                        GameCommand waitCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWaiting], GameCommandSource.AnythingElse );
                        waitCommand.RelatedIntegers.Add( firstPlanetInPath.Index );
                        for ( int k = 0; k < chunk.Entities.Count; k++ )
                            waitCommand.RelatedEntityIDs.Add( chunk.Entities[k].PrimaryKeyID );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, waitCommand, false );
                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "since first planet (" ).Add( firstPlanetInPath.Name ).Add( ") has relatively high difficulty " ).Add( this.BaseInfo.GetRaidTraversalDifficulty( firstPlanetInPath ) ).Add( " > " ).Add( maximumTolerableTraversalDifficulty.IntValue ).Add( ", also queued SetWaiting command to avoid BlunderBot syndrome" );
                        #endregion
                    }
                    debugCode = 1640;
                    PlanetFaction originalFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                    debugCode = 1650;
                    Int16 mostAnnoyingFactionIndex;
                    if (GameSettings.Current.GetBoolBySetting("Debug_ThreatRaidsUseChunkTarget")) {
                        if (targetFaction != null) {
                            mostAnnoyingFactionIndex = targetFaction.FactionIndex;
                        } else {
                            mostAnnoyingFactionIndex = -1;
                        }
                    } else {
                        mostAnnoyingFactionIndex = originalFaction.GetIndexOfMostAnnoyingFaction( Context );
                        if ( mostAnnoyingFactionIndex == -1 )
                        {
                            //if the start planet has no enemies, chech the end planet to see who we should be targeting
                            PlanetFaction targetPFaction = threatTarget.GetPlanetFactionForFaction( AttachedFaction );
                            mostAnnoyingFactionIndex = targetPFaction.GetIndexOfMostAnnoyingFaction( Context );
                        }
                    }
                    debugCode = 1660;

                    debugCode = 1670;
                    //Make sure to mark them as Threat
                    if ( chunk.Entities.Count > 0 )
                    {
                        GameCommand threatFightCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetBehavior_FromFaction_SendOnThreatRaid], GameCommandSource.AnythingElse );
                        threatFightCommand.RelatedMagnitude = (int)EntityBehaviorType.Attacker_Full;
                        threatFightCommand.RelatedFactionIndex = mostAnnoyingFactionIndex;

                        debugCode = 1680;
                        for ( int k = 0; k < chunk.Entities.Count; k++ )
                        {
                            if ( chunk.Entities[k].GetSquad() != null )
                                threatFightCommand.RelatedEntityIDs.Add( chunk.Entities[k].PrimaryKeyID );
                        }
                        if ( GameSettings.Current.GetBoolBySetting( "ThreatDebug" ) )
                        {
                            Faction logFaction = World_AIW2.Instance.GetFactionByIndex(mostAnnoyingFactionIndex);
                            if ( logFaction != null )
                                ArcenDebugging.ArcenDebugLogSingleLine(threatFightCommand.RelatedEntityIDs.Count + " ships on " + planet.Name + " are to go after " + logFaction.GetDisplayName() + " (target faction was " + (targetFaction != null ? targetFaction.GetDisplayName() : "players") + ")" + " in on-raid against " + threatTarget.Name, Verbosity.DoNotShow );
                            else
                                ArcenDebugging.ArcenDebugLogSingleLine(threatFightCommand.RelatedEntityIDs.Count + " ships on " + planet.Name + " are to go after players in on-raid against " + threatTarget.Name, Verbosity.DoNotShow );
                        }

                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, threatFightCommand, false );
                    }
                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Helper_SendThreatOnRaid debugCode " + debugCode + ": Hit exception " + e + " in Helper_SendThreatOnRaid.", Verbosity.DoNotShow );
            }
            finally
            {
                Planet.ReleaseTemporaryPlanetRefPairIntList( working_potentialAttackTargetWithDesirabilities );
                Planet.ReleaseTemporaryPlanetList( working_potentialAttackTargets );
                Planet.ReleaseTemporaryPlanetList( working_highPriorityPotentialAttackTargets );
                Planet.ReleaseTemporaryPlanetList( working_overridinglyHighPriorityPotentialAttackTargets );
                Planet.ReleaseTemporaryPlanetList( working_planetsToCheckInFlood );
                Planet.ReleaseTemporaryPlanetList( pathFinal );
            }
        }

        private bool Helper_HasValuablePlayerStructure(Planet planet)
        {
            Faction faction = planet.GetControllingFaction();
            if(faction.Type != FactionType.Player)
                return false;
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction(faction);
            bool foundStructure = false;
            foreach ( GameEntity_Squad entity in pFaction.Entities.Squads( EntityRollupType.Claimables ) )
            {
                if (entity.TypeData.IsMobile)
                    continue;
                //At the moment, anything Claimable but Immobile is something a player really wants, so flag it
                foundStructure = true;
                break;
            }
            return foundStructure;
        }

        /// <summary>
        /// This static variable will persist between savegames, but it is just for debugging and does not matter.
        /// </summary>
        public static int ASK_LOG_INDEX_FOR_DEBUGGING = 1;

        private void Helper_RetreatThreat(bool tracing, ArcenCharacterBuffer tracingBuffer, ThreatChunkCollection chunks,  Planet planet, ArcenLongTermIntermittentPlanningContext Context )
        {
            List<SafeSquadWrapper> threatShipsNotAssignedElsewhere = GameEntity_Squad.GetTemporarySquadList( "AISent-Helper_RetreatThreat-threatShipsNotAssignedElsewhere", 10f );
            if ( threatShipsNotAssignedElsewhere == null ) //blocked for teardown/shutdown; bail
                return;

            foreach ( KeyValuePair<short, ThreatChunk> kv in chunks.Chunks )
                threatShipsNotAssignedElsewhere.AddRange( kv.Value.Entities );
            #region Tracing
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Actual number of threat units to retreat:" ).Add( threatShipsNotAssignedElsewhere.Count );
            #endregion
            if ( threatShipsNotAssignedElsewhere.Count <= 0 )
            {
                GameEntity_Squad.ReleaseTemporarySquadList( threatShipsNotAssignedElsewhere );
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "aborting retreat because no units to command" );
                #endregion
                return;
            }
            #region Tracing
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Anxious Inquiry: where can I run?" );
            #endregion
            Planet bestEligiblePlanet = null;
            {
                int totalStrengthOfMyGroup = 0;
                for ( int i = 0; i < threatShipsNotAssignedElsewhere.Count; i++ )
                {
                    GameEntity_Squad entity = threatShipsNotAssignedElsewhere[i].GetSquad();
                    if ( entity == null )
                        continue;
                    totalStrengthOfMyGroup += entity.GetStrengthOfSelfAndContents();
                }
                Int64 totalDistanceToBestWormhole = 0;
                FInt friendlyStrengthMultiplier = FInt.FromParts( 0, 500 ); // strongly prefer a planet we outnumber the enemy at least two to one, but we'll relax that constraint later if necessary
                bool doNotPickShieldedWormholes = true;
                bool careAboutHowOutnumberedWeWouldBeOnOtherPlanet = true;

                Dictionary<Planet, Int64> savedTotalDistancesToWormholes = Planet.GetTemporaryPlanetDictOfInt64s( "AISent-Helper_RetreatThreat-savedTotalDistancesToWormholes", 10f );
                if ( savedTotalDistancesToWormholes == null ) //blocked for teardown/shutdown; bail
                {
                    GameEntity_Squad.ReleaseTemporarySquadList( threatShipsNotAssignedElsewhere );
                    return;
                }

                for ( int loopCount = 0; loopCount < 100; loopCount++ )
                {
                    bool somehowHadNoNeighbors = true;
                    foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                    {
                        somehowHadNoNeighbors = false;
                        if ( careAboutHowOutnumberedWeWouldBeOnOtherPlanet )
                        {
                            var neighborFactionData = neighbor.GetStanceDataForFaction( AttachedFaction );
                            int myStrengthOnNeighbor = neighborFactionData[FactionStance.Self].TotalStrength;
                            int alliedStrengthOnNeighbor = neighborFactionData[FactionStance.Friendly].TotalStrength;
                            int friendlyStrength = totalStrengthOfMyGroup + myStrengthOnNeighbor + alliedStrengthOnNeighbor;
                            int threshold = ( friendlyStrength * friendlyStrengthMultiplier ).IntValue;
                            int hostileStrength = neighborFactionData[FactionStance.Hostile].TotalStrength;
                            if ( hostileStrength >= threshold )
                            {
                                #region Tracing
                                if ( tracing )
                                {
                                    tracingBuffer.Add( "\n" ).Add( "not choosing " ).Add( neighbor.Name ).Add( " because friendly-to-hostile ratio worse than " ).Add( friendlyStrengthMultiplier.ReadableString );
                                    tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "totalStrengthOfMyGroup : " ).Add( totalStrengthOfMyGroup );
                                    tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "myStrengthOnNeighbor : " ).Add( myStrengthOnNeighbor );
                                    tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "alliedStrengthOnNeighbor : " ).Add( alliedStrengthOnNeighbor );
                                    tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "friendlyStrength = totalStrengthOfMyGroup + myStrengthOnNeighbor + alliedStrengthOnNeighbor : " ).Add( friendlyStrength );
                                    tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "threshold = friendlyStrength * friendlyStrengthMultiplier : " ).Add( threshold );
                                    tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "hostileStrength : " ).Add( hostileStrength );
                                }
                                #endregion
                                continue;
                            }
                        }
                        GameEntity_Other thisWormhole = planet.GetWormholeTo( neighbor );
                        if ( thisWormhole == null )
                            continue;
                        Int64 totalDistanceToThisWormhole = 0;

                        if ( savedTotalDistancesToWormholes.ContainsKey( neighbor ) )
                            totalDistanceToThisWormhole = savedTotalDistancesToWormholes[neighbor];
                        else
                        {
                            int distanceCheckLoopIncrement = 1;
                            int maxDistanceChecks = 50;
                            if ( threatShipsNotAssignedElsewhere.Count > maxDistanceChecks )
                                distanceCheckLoopIncrement = ( threatShipsNotAssignedElsewhere.Count / maxDistanceChecks ) + 1;
                            for ( int i = 0; i < threatShipsNotAssignedElsewhere.Count; i += distanceCheckLoopIncrement )
                                totalDistanceToThisWormhole += threatShipsNotAssignedElsewhere[i].WorldLocation.GetManhattanDistanceTo( thisWormhole.WorldLocation );
                        }

                        if ( bestEligiblePlanet != null && totalDistanceToBestWormhole <= totalDistanceToThisWormhole )
                        {
                            #region Tracing
                            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "not choosing " ).Add( neighbor.Name ).Add( " because the average distance to it is greater than to  " ).Add( bestEligiblePlanet.Name );
                            #endregion
                            continue;
                        }
                        if ( doNotPickShieldedWormholes )
                        {
                            bool foundBlockingShield = FactionUtilityMethods.Instance.IsShieldBlockingWormholeToPlanet ( planet, neighbor, AttachedFaction);
                            if(foundBlockingShield )
                            {
                                #region Tracing
                                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "not choosing " ).Add( neighbor.Name ).Add( " because the outgoing wormhole is blocked by a hostile shield" );
                                #endregion
                                continue;
                            }
                        }
                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "new best candidate: " ).Add( neighbor.Name );
                        #endregion
                        bestEligiblePlanet = neighbor;
                        totalDistanceToBestWormhole = totalDistanceToThisWormhole;
                    }
                    if ( somehowHadNoNeighbors )
                        break;
                    if ( bestEligiblePlanet != null )
                        break;
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "relaxing friendly-to-hostile ratio requirement" );
                    #endregion
                    friendlyStrengthMultiplier *= 2;
                    if ( friendlyStrengthMultiplier >= 4 )
                        doNotPickShieldedWormholes = false;
                    if ( friendlyStrengthMultiplier >= 16 )
                        careAboutHowOutnumberedWeWouldBeOnOtherPlanet = false;
                }

                Planet.ReleaseTemporaryPlanetDictOfInt64s( savedTotalDistancesToWormholes );
            }

            if ( bestEligiblePlanet == null )
            {
                GameEntity_Squad.ReleaseTemporarySquadList( threatShipsNotAssignedElsewhere );
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "aborting retreat because no eligible planet found (this should not happen unless it's a one-planet galaxy)" );
                #endregion
                return;
            }
            {
                GameEntity_Other wormholeToBest = planet.GetWormholeTo( bestEligiblePlanet );
                if ( wormholeToBest == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "BUG: AI_FLEE_THRT Trying to send AI ships from planet " + planet.Name +
                        " to 'best planet' " + bestEligiblePlanet.Name + ", which is not linked by a wormhole!", Verbosity.ShowAsError );
                }
            }
            #region Tracing
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "retreating to " ).Add( bestEligiblePlanet.Name ).Add(" after clearing SetWaiting orders");
            #endregion
            //These retreating threat ships may have been Waiting against another planet, so clear that Wait state.^M
            //Once retreated, the Threat can establish new Wait targets^M
            if ( threatShipsNotAssignedElsewhere.Count > 0 )
            {
                GameCommand clearWaitCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWaiting], GameCommandSource.AnythingElse );
                clearWaitCommand.RelatedIntegers.Add( -1 );
                for ( int k = 0; k < threatShipsNotAssignedElsewhere.Count; k++ )
                    clearWaitCommand.RelatedEntityIDs.Add( threatShipsNotAssignedElsewhere[k].PrimaryKeyID );
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, clearWaitCommand, false );
            }

            //Make sure to mark them as Threat
            if ( threatShipsNotAssignedElsewhere.Count > 0 )
            {
                GameCommand threatFightCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetBehavior_FromFaction_RetreatThreat], GameCommandSource.AnythingElse );
                threatFightCommand.RelatedMagnitude = (int)EntityBehaviorType.Attacker_Full;
                threatFightCommand.RelatedFactionIndex = (planet.GetPlanetFactionForFaction( AttachedFaction )).GetIndexOfMostAnnoyingFaction( Context );
                for ( int k = 0; k < threatShipsNotAssignedElsewhere.Count; k++ )
                    threatFightCommand.RelatedEntityIDs.Add( threatShipsNotAssignedElsewhere[k].PrimaryKeyID );
                if ( GameSettings.Current.GetBoolBySetting( "ThreatDebug" ) )
                {
                    Faction logFaction = World_AIW2.Instance.GetFactionByIndex(threatFightCommand.RelatedFactionIndex);
                    if ( logFaction != null )
                        ArcenDebugging.ArcenDebugLogSingleLine("retreat threat on " + threatShipsNotAssignedElsewhere[0].GetPlanetName_Safe() + " going after " + logFaction.GetDisplayName(), Verbosity.DoNotShow );
                    else
                        ArcenDebugging.ArcenDebugLogSingleLine("retreat threat on " + threatShipsNotAssignedElsewhere[0].GetPlanetName_Safe() + " going after  -1", Verbosity.DoNotShow );
                }
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, threatFightCommand, false );
            }

            if ( threatShipsNotAssignedElsewhere.Count > 0 )
            {
                GameCommand command = null;
                for ( int k = 0; k < threatShipsNotAssignedElsewhere.Count; k++ )
                {
                    if ( threatShipsNotAssignedElsewhere[k].Planet == planet )
                    {
                        if ( command == null )
                            command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_AIThreatRetreat], GameCommandSource.AnythingElse );
                        command.RelatedEntityIDs.Add( threatShipsNotAssignedElsewhere[k].PrimaryKeyID );
                    }
                    else
                        ArcenDebugging.ArcenDebugLogSingleLine( "BUG: AI_FLEE_THRT Trying to send AI ships from planet " + planet.Name +
                            " when ship was on " + threatShipsNotAssignedElsewhere[k].GetPlanetName_Safe(), Verbosity.DoNotShow );
                }
                if ( command != null )
                {
                    command.RelatedString = "AI_FLEE_THRT";
                    command.RelatedIntegers.Add( bestEligiblePlanet.Index );
                    command.RelatedMagnitude = ASK_LOG_INDEX_FOR_DEBUGGING++;
                    command.RelatedBool = true; //use a Refuse To Wait order to make sure we run
                    //ArcenDebugging.ArcenDebugLogSingleLine( "ASK LOG: AI_FLEE_THRT from planet " + planet.Name + " (IX" + planet.Index + 
                    //    ") to 'best planet' " + bestEligiblePlanet.Name + " (IX" + bestEligiblePlanet.Index + ") (ASK" + command.RelatedMagnitude + ")", Verbosity.DoNotShow );
                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                }
                //World_AIW2.Instance.QueueChatMessageOrCommand( "What a bore. Retreating from " + planet.Name + " to " + bestEligiblePlanet.Name, "Civilian_MoveOrder_3VeryAnnoyed", threatShipsNotAssignedElsewhere[0], Context );
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "command queued:" ).Add( command.RelatedEntityIDs.Count ).Add( ":" ).Add( command.RelatedIntegers.Count );
                #endregion
            }

            GameEntity_Squad.ReleaseTemporarySquadList( threatShipsNotAssignedElsewhere );
        }

        private static readonly ThreadingExchanger iscurrentlySpendingBudget = new ThreadingExchanger( "GlobalTryToSpendBudget", 30f );

        public override bool TryToSpendBudget(ArcenHostOnlySimContext Context,  AIBudgetType budget)
        {
            if ( iscurrentlySpendingBudget.IsBusy() )
                return false;

            if ( !iscurrentlySpendingBudget.DoNextOnlyIfNotAlreadyBusy( string.Empty ) )
                return false;

            bool successfullySpent = true;
            try
            {
                switch ( budget )
                {
                    case AIBudgetType.Reinforcement:
                        TryToSpendBudget_Reinforcement( Context );
                        break;
                    case AIBudgetType.Wave:
                        this.TryToSpendBudget_Wave( Context, 0, 0 );
                        break;
                    case AIBudgetType.Reconquest:
                        TryToSpendBudget_Reconquest( Context );
                        break;
                    case AIBudgetType.CPA:
                        TryToSpendBudget_CPA( Context, false );
                        break;
                    case AIBudgetType.Warden:
                        TryToSpendBudget_Warden( Context );
                        break;
                    case AIBudgetType.HunterFleet:
                        TryToSpendBudget_HunterFleet( Context );
                        break;
                    case AIBudgetType.PraetorianGuard:
                        TryToSpendBudget_PraetorianGuard( Context );
                        break;
                    case AIBudgetType.WormholeInvasion:
                        successfullySpent = TryToSpendBudget_WormholeInvasion( Context );
                        break;
                    case AIBudgetType.BorderAggression:
                        successfullySpent = TryToSpendBudget_BorderAggression( Context );
                        break;

                }
                iscurrentlySpendingBudget.MarkAsNoLongerBusy();
                return successfullySpent;
            }
            catch ( Exception e )
            {
                iscurrentlySpendingBudget.MarkAsNoLongerBusy();
                ArcenDebugging.ArcenDebugLogSingleLine( "TryToSpendBudget error: " + e, Verbosity.ShowAsError );
                return false;
            }
        }

        private readonly List<Planet> alertLevel4Planets = List<Planet>.Create_WillNeverBeGCed( 500, "AISentinelsFactionDeepInfo-alertLevel4Planets" );
        private readonly List<Planet> alertLevel3Planets = List<Planet>.Create_WillNeverBeGCed( 500, "AISentinelsFactionDeepInfo-alertLevel3Planets" );
        private readonly DrawBag<Planet> allAIPlanetsByAlertLevel = DrawBag<Planet>.Create_WillNeverBeGCed( 40, "AISentinelsFactionDeepInfo-allAIPlanetsByAlertLevel" );

        //Reusable per-planet-index strength accumulator for TryToSpendBudget_Reinforcement, indexed
        //by planet.Index. Instance field (one AI faction deep info per faction; its reinforcement
        //planning runs on a single background thread at a time), grown on demand and cleared per
        //run -- replaces a per-call new int[planetCount + 10]. Passed by value to the Reinforce*
        //helpers, which only read/write elements (never reassign), so no 'ref' is needed.
        private int[] strengthAddedByPlanetIndex = System.Array.Empty<int>();

        private void TryToSpendBudget_Reinforcement(ArcenHostOnlySimContext Context )
        {
            bool logReinforcementEventDetails = GameSettings.Current.GetBoolBySetting( "Debug_LogReinforcementEventDetails" );

            AIBudgetItem budgetItem = this.BaseInfo.SentinelInfo.AIType.BudgetItems[AIBudgetType.Reinforcement];

            //GetCountOfTotalPlanetsDestroyedAndOtherwise() is monotonic over a game, so the buffer
            //only ever grows. Clear the in-use prefix each run; the final tally loop below iterates
            //requiredStrengthArraySize (not the possibly-larger buffer length) so any stale tail is
            //never read.
            int requiredStrengthArraySize = World_AIW2.Instance.CurrentGalaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() + 10;
            if ( this.strengthAddedByPlanetIndex.Length < requiredStrengthArraySize )
                this.strengthAddedByPlanetIndex = new int[requiredStrengthArraySize];
            else
                System.Array.Clear( this.strengthAddedByPlanetIndex, 0, requiredStrengthArraySize );
            int[] strengthAddedByPlanetIndex = this.strengthAddedByPlanetIndex;

            alertLevel3Planets.Clear();
            alertLevel4Planets.Clear();
            allAIPlanetsByAlertLevel.Clear();
            foreach ( Planet planet in AttachedFaction.ControlledPlanetsSingleThread() )
            {
                if ( !Helper_GetCanFactionReasonablyReinforcePlanet( planet ) )
                    continue;

                AISentinelAlertLevel alertLevel = planet.SentinelsAlertLevel;
                if ( alertLevel == null )
                    continue;
                if ( alertLevel.Ordinal >= 4 )
                    alertLevel4Planets.Add( planet );
                else if ( alertLevel.Ordinal == 3 )
                    alertLevel3Planets.Add( planet );
                allAIPlanetsByAlertLevel.AddItem( planet, alertLevel.ReinforcementGrabBagTickets );
            }

            FInt totalBudget = this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.Reinforcement];
            FInt tier4Budget = totalBudget / 3;
            FInt tier3Budget = totalBudget / 10;

            int desiredRandomUniquePlanets = 10;

            #region Tier 4 Budget Limiting
            //if there are no alert-level-4 planets, then give no budget to them
            if ( alertLevel4Planets.Count <= 0 )
            {
                tier4Budget = FInt.Zero;
                desiredRandomUniquePlanets += 10;
            }
            //if there are fewer than 10 alert-level-4 planets, shrink their budget proportionately
            else if ( alertLevel4Planets.Count < 10 )
            {
                int effectivePlanetCount = alertLevel4Planets.Count;
                //never shrink below 20% of the alert-level 4 budget
                if ( effectivePlanetCount < 2 )
                    effectivePlanetCount = 2;
                FInt tier4BudgetMultiplier = (FInt)effectivePlanetCount / (FInt)10;
                tier4Budget *= tier4BudgetMultiplier;

                desiredRandomUniquePlanets += (alertLevel4Planets.Count - effectivePlanetCount);
            }
            #endregion

            #region Tier 3 Budget Limiting
            //if there are no alert-level-3 planets, then give no budget to them
            if ( alertLevel3Planets.Count <= 0 )
            {
                tier3Budget = FInt.Zero;
                desiredRandomUniquePlanets += 5;
            }
            //if there are fewer than 5 alert-level-3 planets, shrink their budget proportionately
            else if ( alertLevel3Planets.Count < 5 )
            {
                FInt tier3BudgetMultiplier = (FInt)alertLevel3Planets.Count / (FInt)5;
                tier3Budget *= tier3BudgetMultiplier;

                desiredRandomUniquePlanets += ( 5 - alertLevel3Planets.Count );
            }
            #endregion

            if ( logReinforcementEventDetails )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Reinforcement Event About To Start: " +
                    " totalBudget: " + totalBudget.IntValue +
                    " tier4Budget: " + tier4Budget.IntValue +
                    " tier3Budget: " + tier3Budget.IntValue,
                    Verbosity.DoNotShow );
            }

            //first handle those tier 4 planets that are on highest alert
            ReinforceListOfPlanets( alertLevel4Planets, Context, ref totalBudget, ref tier4Budget, 
                strengthAddedByPlanetIndex, logReinforcementEventDetails, "AlertLevel4" );
            //if there was any budget left over from that, add it to the tier 3 planet budget
            if ( tier4Budget > FInt.Zero )
                tier3Budget += tier4Budget;

            //now handle those tier 3 planets that are on general alert
            ReinforceListOfPlanets( alertLevel3Planets, Context, ref totalBudget, ref tier3Budget, 
                strengthAddedByPlanetIndex, logReinforcementEventDetails, "AlertLevel3" );

            //we will reuse the alertLevel4Planets just to save on RAM.
            //It's being used for general planets now, though
            alertLevel4Planets.Clear();
            int loopCount = 500;
            //try to fill that list with [desiredRandomUniquePlanets] random unique planets.  If it takes more than 500 tries, then just stop with whatever we get
            while ( alertLevel4Planets.Count < desiredRandomUniquePlanets && loopCount-- > 0 )
            {
                //draw from the weighted draw bag.
                Planet planet = allAIPlanetsByAlertLevel.PickRandomItemAndDoNotReplace( Context.RandomToUse );
            }
            //okay, now we hopefully have 10 random planets of whatever alert level, but it may be fewer
            //give reinforcements to them based on this budget
            FInt budgetForRemainingPlanets = totalBudget;
            ReinforceListOfPlanets( alertLevel4Planets, Context, ref totalBudget, ref budgetForRemainingPlanets, 
                strengthAddedByPlanetIndex, logReinforcementEventDetails, "RandomPlanetsByWeight" );

            if ( logReinforcementEventDetails )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Reinforcement Event Internals Finished: " +
                    " totalBudget left: " + totalBudget.IntValue,
                    Verbosity.DoNotShow );
            }

            //we are done actually doing the reinforcements, so set our working budget into long-term storage there
            //this basically lets us know how much we spent so that it goes away from our current ongoing budget
            this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.Reinforcement] = totalBudget;

            //now it's time to tell planets about whatever the aggregate result was from reinforcemenets
            for ( Int16 planetIndex = 0; planetIndex < requiredStrengthArraySize; planetIndex++ )
            {
                int addedStrengthThisEvent = strengthAddedByPlanetIndex[planetIndex];
                if ( addedStrengthThisEvent <= 0 )
                    continue; //skip planets we did nothing with

                Planet planetWeReinforced = World_AIW2.Instance.CurrentGalaxy.GetPlanetByIndex( planetIndex );
                if ( planetWeReinforced == null )
                    continue; //if an invalid planet for some reason, skip

                //log the info we want the players to know about this reinforcement event
                planetWeReinforced.NumberOfSentinelReinforcementsEvents++;
                planetWeReinforced.StrengthOfLastSentinelReinforcements = addedStrengthThisEvent;
                planetWeReinforced.TotalStrengthOfAllSentinelReinforcements += addedStrengthThisEvent;
                planetWeReinforced.TimeOfLastSentinelReinforcement = World_AIW2.Instance.GameSecond;

                if ( logReinforcementEventDetails )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Reinforcement Event For: " + planetWeReinforced.Name +
                        " strength added: " + (addedStrengthThisEvent / 1000f).ToString( "#,##0.00" ) +
                        " total events now: " + planetWeReinforced.NumberOfSentinelReinforcementsEvents +
                        " total strength ever added now: " + (planetWeReinforced.TotalStrengthOfAllSentinelReinforcements / 1000f).ToString( "#,##0.00" ),
                        Verbosity.DoNotShow );
                }
            }
        }

        private void ReinforceListOfPlanets( List<Planet> planetList,  ArcenHostOnlySimContext Context,
            ref FInt totalBudget, ref FInt subBudget, int[] strengthAddedByPlanetIndex, bool logReinforcementEventDetails, string PlanetListName )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //don't even try this on clients

            if ( logReinforcementEventDetails )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Reinforcement Loop For List Of Planets: " + PlanetListName +
                    " Count In List: " + planetList.Count +
                    " subBudget for this list: " + subBudget.IntValue +
                    " totalBudget now: " + totalBudget.IntValue,
                    Verbosity.DoNotShow );
            }
            if ( planetList.Count == 0 )
                return;

            FInt idealBudgetPerPlanet = subBudget / planetList.Count;
            FInt runningTallyOfExtraLeftoverBudget = FInt.Zero;
            for ( int i = 0; i < planetList.Count; i++ )
            {
                int remainingPlanets = planetList.Count - i;
                //evenly distribute the extra budget as much as we can!
                FInt extraBudgetToUseForMyself = remainingPlanets > 1 && runningTallyOfExtraLeftoverBudget > FInt.Zero ?
                    runningTallyOfExtraLeftoverBudget / remainingPlanets : runningTallyOfExtraLeftoverBudget;
                if ( extraBudgetToUseForMyself > FInt.Zero )
                {
                    //take out the part that is for me, if we have a positive extra budget
                    runningTallyOfExtraLeftoverBudget -= extraBudgetToUseForMyself;
                }
                else //if there was a mistake and we have negative extra budget, just zero that out.  Should not happen.
                    extraBudgetToUseForMyself = FInt.Zero;

                Planet planet = planetList[i];
                if ( planet == null )
                    continue;

                //do the reinforcement of this planet, and internally subtract from the sub and total budgets
                FInt amountSpentAgainstBudget, amountSpentInTotalForReal;
                FInt extraBudgetLeftOver = ReinforceSpecificPlanet( planet,
                    idealBudgetPerPlanet + extraBudgetToUseForMyself,
                    Context, ref totalBudget, ref subBudget, strengthAddedByPlanetIndex,
                    out amountSpentAgainstBudget, out amountSpentInTotalForReal
                    );
                //if we had budget left over, then add that into the running total for future planets in this list of planets
                if ( extraBudgetLeftOver > FInt.Zero )
                    runningTallyOfExtraLeftoverBudget += extraBudgetLeftOver;

                if ( logReinforcementEventDetails )
                {
                    //Helper_WriteLogOfBudgetCaps( planet, faction, factionExternal );
                    ArcenDebugging.ArcenDebugLogSingleLine( "Reinforcement PARTIAL Event For Planet: " + planet.Name +
                        " From List: " + PlanetListName +
                        "\n\tidealBudgetPerPlanet was: " + idealBudgetPerPlanet.IntValue +
                        " extraBudgetToUseForMyself was: " + extraBudgetToUseForMyself.IntValue +
                        " extraBudgetLeftOver was: " + extraBudgetLeftOver.IntValue +
                        "\n\tamountSpentAgainstBudget was: " + amountSpentAgainstBudget.IntValue +
                        " amountSpentInTotalForReal was: " + amountSpentInTotalForReal.IntValue +
                        "\n\trunningTallyOfExtraLeftoverBudget is now: " + runningTallyOfExtraLeftoverBudget.IntValue,
                        Verbosity.DoNotShow );
                }
            }
        }

        private FInt ReinforceSpecificPlanet( Planet planet,  FInt originalBudgetForThisPlanet, ArcenHostOnlySimContext Context, 
            ref FInt totalBudget, ref FInt subBudget, int[] strengthAddedByPlanetIndex,
            out FInt amountSpentAgainstBudget, out FInt amountSpentInTotalForReal )
        {
            if ( planet == null )
            {
                amountSpentAgainstBudget = FInt.Zero;
                amountSpentInTotalForReal = FInt.Zero;
                return FInt.Zero;
            }

            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
            {
                amountSpentAgainstBudget = FInt.Zero;
                amountSpentInTotalForReal = FInt.Zero;
                return FInt.Zero; //don't even try this on clients
            }

            int debugStage = 0;
            FInt budgetUsed = FInt.Zero;
            try
            {
                debugStage = 100;
                int addedStrength = 0;

                debugStage = 200;
                FInt boosterMultiplier = FInt.One;
                foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.ReinforcementBoosters ) )
                {
                    debugStage = 300;
                    FInt boost = entity.TypeData.AIReinforcementMultiplier - FInt.One;
                    boosterMultiplier += boost;
                }

                debugStage = 1000;
                FInt budgetForPlanetAfterBoosts = (originalBudgetForThisPlanet * boosterMultiplier);
                AIDefensePlacer placer = planet.GetCurrentDefensePlacer( Context );
                //thisPlanetBudget -= placer.Implementation.Reinforce( Context, planet, faction, thisPlanetBudget, ReinforcementType.Shield, false );

                debugStage = 1100;
                int startingBudget = budgetForPlanetAfterBoosts.IntValue;

                debugStage = 1200;
                //first give a bunch to strikecraft
                int budgetPercentageForStrikecraft = Context.RandomToUse.Next( 25, 60 );
                //then half the remainder of what could have been strikecraft to turrets, plus up to 15 percent
                int budgetPercentageForTurrets = ((60 - budgetPercentageForStrikecraft) / 2) + Context.RandomToUse.Next( 0, 15 );
                //then a quarter the remainder of what those two together could have been divided by two for NonTurretDefense
                int budgetPercentageForNonTurretDefenses = ((75 - (budgetPercentageForStrikecraft + budgetPercentageForTurrets)) / 4);
                //then everything else for guardians; bare minimum of 25 percent, but could be far more
                int budgetPercentageForGuardian = 100 - budgetPercentageForStrikecraft - budgetPercentageForTurrets - budgetPercentageForNonTurretDefenses;

                debugStage = 1300;
                //calculate these based on the percentages above
                int strikecraftBudget = (startingBudget * (FInt)budgetPercentageForStrikecraft / FInt.OneHundred).GetNearestIntPreferringHigher();
                int turretBudget = (startingBudget * (FInt)budgetPercentageForTurrets / FInt.OneHundred).GetNearestIntPreferringHigher();
                //Note from Chris:  Until 2024 we were not really having guardians appearing via the guardian budget, apparently?
                //                  So having them start appearing now is deadly for balance.
                //                  If some mods want to experiment with it, then that's fine.
                int guardianBudget = (startingBudget * (FInt)budgetPercentageForGuardian / FInt.OneHundred).GetNearestIntPreferringHigher();
                //calculate this one based on the remainder.  The budgetPercentageForNonTurretDefenses calculation was only for above to figure out guardian
                int nonTurretDefenseBudget = startingBudget - strikecraftBudget - turretBudget - guardianBudget;

                debugStage = 2000;
                int amountSpent = 0;
                //do strikecraft first, since they are more important
                amountSpent = placer.Implementation.Reinforce( Context, planet, AttachedFaction, strikecraftBudget, ReinforcementType.Strikecraft, false, false, ref addedStrength );
                debugStage = 2100;
                if ( amountSpent > 0 )
                {
                    debugStage = 2200;
                    budgetUsed += amountSpent;
                    int leftovers = strikecraftBudget - amountSpent;
                    if ( leftovers > 0 ) //if there are leftovers for any reason, give it to the turrets
                        turretBudget += leftovers;
                }
                else if ( amountSpent < 0 )
                    ArcenDebugging.ArcenDebugLogSingleLine( "What?  The AI got budget back from trying to seed strikecraft? " + amountSpent, Verbosity.ShowAsError );

                debugStage = 3000;
                //do turrets next, since they are important BUT if there happen to be leftovers it will be good to roll that into guardians later than sooner
                amountSpent = placer.Implementation.Reinforce( Context, planet, AttachedFaction, turretBudget, ReinforcementType.Turret, false, false, ref addedStrength );
                debugStage = 3100;
                if ( amountSpent > 0 )
                {
                    debugStage = 3200;
                    budgetUsed += amountSpent;
                    int leftovers = turretBudget - amountSpent;
                    if ( leftovers > 0 ) //if there are leftovers for any reason, give it to the guardians directly -- we don't care about NTDs
                        guardianBudget += leftovers;
                }
                else if ( amountSpent < 0 )
                    ArcenDebugging.ArcenDebugLogSingleLine( "What?  The AI got budget back from trying to seed turrets? " + amountSpent, Verbosity.ShowAsError );

                debugStage = 4000;
                //do NTD next, even though they are least important, BUT their potential for leaving leftovers is something we want for the guardians
                amountSpent = placer.Implementation.Reinforce( Context, planet, AttachedFaction, nonTurretDefenseBudget, ReinforcementType.NonTurretDefense, false, false, ref addedStrength );
                debugStage = 4200;
                if ( amountSpent > 0 )
                {
                    debugStage = 4300;
                    budgetUsed += amountSpent;
                    int leftovers = nonTurretDefenseBudget - amountSpent;
                    if ( leftovers > 0 ) //if there are leftovers for any reason, give it to the guardians again
                        guardianBudget += leftovers;
                }
                else if ( amountSpent < 0 )
                    ArcenDebugging.ArcenDebugLogSingleLine( "What?  The AI got budget back from trying to seed non-turret defenses? " + amountSpent, Verbosity.ShowAsError );

                debugStage = 5000;
                //do guardians last.  Any and all leftovers can make them afford something better, potentially
                amountSpent = placer.Implementation.Reinforce( Context, planet, AttachedFaction, guardianBudget, ReinforcementType.Guardian, false, false, ref addedStrength );
                debugStage = 5100;
                if ( amountSpent > 0 )
                    budgetUsed += amountSpent;                
                else if ( amountSpent < 0 )
                    ArcenDebugging.ArcenDebugLogSingleLine( "What?  The AI got budget back from trying to seed guardians? " + amountSpent, Verbosity.ShowAsError );

                debugStage = 6000;
                //If anything is left over, try again for some more little stuff with strikecraft
                if ( budgetUsed < startingBudget && budgetUsed > FInt.Zero )
                {
                    debugStage = 6100;
                    amountSpent = placer.Implementation.Reinforce( Context, planet, AttachedFaction, ( startingBudget - budgetUsed ).IntValue, ReinforcementType.Strikecraft, false, false, ref addedStrength );
                    if ( amountSpent > 0 )
                        budgetUsed += amountSpent;
                    else if ( amountSpent < 0 )
                        ArcenDebugging.ArcenDebugLogSingleLine( "What?  The AI got budget back from trying to seed strikecraft remainder? " + amountSpent, Verbosity.ShowAsError );
                }

                debugStage = 7000;
                //how much did we spend?

                FInt amountActuallySpent = (FInt)budgetUsed;
                amountSpentInTotalForReal = amountActuallySpent;
                //if it was more than the original budget, lie and say that's how much we spent
                //reinforcement boosters in particular, or rounding errors if those happen, are now allowed
                if ( amountActuallySpent > originalBudgetForThisPlanet )
                    amountActuallySpent = originalBudgetForThisPlanet;
                amountSpentAgainstBudget = amountActuallySpent;

                debugStage = 8000;
                //now handle our final tallies!
                strengthAddedByPlanetIndex[planet.Index] += addedStrength;
                debugStage = 8100;
                totalBudget -= amountActuallySpent;
                subBudget -= amountActuallySpent;

                //if there was extra budget, let us know
                FInt extraBudget = originalBudgetForThisPlanet - amountActuallySpent;
                return extraBudget;
            }
            catch ( Exception e )
            {
                amountSpentAgainstBudget = (FInt)budgetUsed;
                amountSpentInTotalForReal = (FInt)budgetUsed;
                ArcenDebugging.ArcenDebugLogSingleLine( "ReinforceSpecificPlanet error at debugStage " + debugStage + ".  Exception: " + e, Verbosity.ShowAsError );
            }
            return FInt.Zero;
        }

        public bool Helper_GetCanFactionReasonablyReinforcePlanet( Planet planet )
        {
            for ( ReinforcementType reinforcementType = ReinforcementType.None + 1; reinforcementType <= ReinforcementType.NonTurretDefense; reinforcementType++ )
            {
                switch (reinforcementType)
                {
                    case ReinforcementType.NonTurretDefense:
                    case ReinforcementType.Turret:
                        //you know what?  Don't do a reinforcement event if we can ONLY do turrets or non-turret defenses
                        //do those if there are also other things to reinforce, sure.
                        continue; 
                }
                int AICostPurchaseCap = AIUtilityMethods.GetAICostPurchaseCapForBudgetType( planet, AttachedFaction, reinforcementType, false, false, this.BaseInfo.SentinelInfo ).IntValue;
                int purchaseCostPresent = AIUtilityMethods.GetAIToPurchaseCostPresentForBudgetType( planet, AttachedFaction, reinforcementType );
                if ( purchaseCostPresent < AICostPurchaseCap )
                    return true;
            }
            return false; //all of them were full, apparently.
        }
        public void Helper_WriteLogOfBudgetCaps( Planet planet )
        {
            for ( ReinforcementType reinforcementType = ReinforcementType.None + 1; reinforcementType <= ReinforcementType.NonTurretDefense; reinforcementType++ )
            {
                AIUtilityMethods.GetAICostPurchaseCapForBudgetType( planet, AttachedFaction, reinforcementType, false, true, this.BaseInfo.SentinelInfo );
            }
        }

        private static readonly List<SafeSquadWrapper> PreferredWarpGates = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "AISentinelsFactionDeepInfo-PreferredWarpGates" );
        private static readonly List<SafeSquadWrapper> FallbackWarpGates = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "AISentinelsFactionDeepInfo-FallbackWarpGates" );
        private static readonly List<SafeSquadWrapper> WarpGatesToUse = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "AISentinelsFactionDeepInfo-WarpGatesToUse" );

        private bool TryToSpendBudget_BorderAggression(ArcenHostOnlySimContext Context)
        {
            PreferredWarpGates.Clear();
            FallbackWarpGates.Clear();
            WarpGatesToUse.Clear();

            int budget = this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.BorderAggression].IntValue;
            byte difficulty = this.BaseInfo.SentinelInfo.AIDifficulty.Difficulty;
            Faction factionToUse = this.BaseInfo.SubFac_BorderAggression;
            if ( factionToUse == null )
                return true;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.BudgetSpend );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AISent-TryToSpendBudget_BorderAggression-trace", 10f ) : null;

            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( EntityRollupType.WarpEntryPoints ) )
            {
                if ( entity.TypeData.IsWarpBeacon ) //no using exogalactic wormholes
                    continue;
                //if we are adjacent to a human planet, we are Preferred. Else we are Fallback
                if ( entity.Planet.IsAdjacentToHumanWorld )
                    PreferredWarpGates.Add(entity);
                else
                    FallbackWarpGates.Add(entity);
            }
            if ( PreferredWarpGates.Count == 0 && FallbackWarpGates.Count == 0 )
                return true; //no warp gates!
            //now choose the planets
            int planetsToUse = budget / 2000;
            if ( planetsToUse > 3 )
                planetsToUse = 3;
            if ( tracing )
                tracingBuffer.Add("Going to start some border aggression. Budget " + budget + " planets to use: " + planetsToUse +"\n");
            if ( FallbackWarpGates.Count > 0 )
            {
                WarpGatesToUse.Add(FallbackWarpGates[Context.RandomToUse.Next(0, FallbackWarpGates.Count)]);
                planetsToUse--;
            }
            while ( planetsToUse > 0 )
            {
                planetsToUse--;
                if ( PreferredWarpGates.Count > 0 )
                {
                    int index = Context.RandomToUse.Next( 0, PreferredWarpGates.Count );
                    GameEntity_Squad gate = PreferredWarpGates[index].GetSquad();
                    if ( gate != null )
                        WarpGatesToUse.Add(gate);
                    PreferredWarpGates.RemoveAt( index );
                }
            }
            if (WarpGatesToUse.Count == 0)
            {
                //this generally means the AI Overlord is dead
                return false;
            }
            int budgetPerPlanet = budget / WarpGatesToUse.Count;

            ThrowawayDrawBagCanMemLeak<GameEntityTypeData> borderAggroUnits = ThrowawayDrawBagCanMemLeak<GameEntityTypeData>.Create_WillActuallyBeGCed( 30 );
            ThrowawayDrawBagCanMemLeak<GameEntityTypeData> workingUnitsToSpawn = ThrowawayDrawBagCanMemLeak<GameEntityTypeData>.Create_WillActuallyBeGCed( 30 );
            if ( tracing )
                tracingBuffer.Add("TryToSpendBudget_BorderAggression: budget " + budget + " planets to spawn from: " + WarpGatesToUse.Count + " and budget per planet: " + budgetPerPlanet ).Add("\n");
            AITypeData aiType = AttachedFaction.TryGetAISentinelsCoreData().SentinelInfo.AIType;
            //AIBudgetItem reinforcementBudgetItem = aiType.BudgetItems[AIBudgetType.Reinforcement];
            AIBudgetItem aggroBudgetItem = aiType.BudgetItems[AIBudgetType.BorderAggression];
            for ( int i = 0; i < WarpGatesToUse.Count; i++ )
            {
                if ( tracing )
                    tracingBuffer.Add("Using warp gate " + WarpGatesToUse[i].ToStringWithPlanet() + "\n");
                int numUnitTypes = Context.RandomToUse.Next( 2, 3 );

                int maxGuardianStrength = 5000;

                borderAggroUnits.Clear();
                Planet planetToUseForSpawningTypes = WarpGatesToUse[i].Planet;
                AIShipGroup shipGroup = aggroBudgetItem.NormalAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                AIShipGroup guardianShipGroup = aggroBudgetItem.GuardianAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );

                if ( shipGroup != null )
                    borderAggroUnits.AddAllFromOtherToThis( shipGroup.DrawBag );
                if ( guardianShipGroup != null )
                    borderAggroUnits.AddAllFromOtherToThis( guardianShipGroup.DrawBag );
                if ( !borderAggroUnits.GetHasItems() )
                    continue;
                workingUnitsToSpawn.Clear();
                WavesHelper.Instance.Helper_RemoveWaveForbiddenItemsFromWorkingDrawBag( borderAggroUnits );
                GameEntityTypeData type;
                for ( int j = 0; j < numUnitTypes; j++ )
                {
                    type = borderAggroUnits.PickRandomItemAndDoNotReplace( Context.RandomToUse );
                    workingUnitsToSpawn.AddItem( type, 1 );
                }
                if ( AIShipGroupTable.EnabledReactiveShipGroups.Count > 0 )
                {
                    AIShipGroup venatorShipGroup = AIShipGroupTable.EnabledReactiveShipGroups[Context.RandomToUse.Next(0, AIShipGroupTable.EnabledReactiveShipGroups.Count)];
                    type = venatorShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    workingUnitsToSpawn.AddItem( type, 1);
                }

                Dictionary<GameEntityTypeData, int> workingSpendLookup = GameEntityTypeData.GetTemporaryGameEntityTypeDataIntDict( "AISent-TryToSpendBudget_BorderAggression-workingSpendLookup", 10f );
                if ( workingSpendLookup == null ) //blocked for teardown/shutdown; bail
                    return false;

                AttachedFaction.FillComposition( Context, budgetPerPlanet, maxGuardianStrength, workingSpendLookup, workingUnitsToSpawn, AttachedFaction.CurrentGeneralMarkLevel, 0);
                WavesHelper.Instance.DeployComposition(Context, factionToUse, WarpGatesToUse[i].GetSquad(), -1, workingSpendLookup, null, ArcenPoint.ZeroZeroPoint, null, false, false );

                GameEntityTypeData.ReleaseTemporaryGameEntityTypeDataIntDict( workingSpendLookup );
            }
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracingBuffer != null )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.BorderAggression] = FInt.Zero; //we've spent our budget!
            return true;
        }
        private bool TryToSpendBudget_WormholeInvasion(ArcenHostOnlySimContext Context)
        {
            bool debug = false;
            if ( Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.WormholeInvasion ) )
                debug = true;

            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( this.AttachedFaction.GetDisplayName() + " using wormhole invasion mode " +  World_AIW2.Instance.Setup.GetStringBySetting("WormholeInvasionMode"), Verbosity.DoNotShow );
            //CHRIS TODO: Changing this setting under Galaxy Settings options and hitting "Save" doesn't seem to work
            if ( World_AIW2.Instance.Setup.GetStringBySetting("WormholeInvasionMode") == "行星连接" )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("doing planet linking", Verbosity.DoNotShow );
                TryToSpendBudget_WormholeInvasion_PlanetLinking( Context );
            }
            else if ( World_AIW2.Instance.Setup.GetStringBySetting("WormholeInvasionMode") == "河外虫洞" )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("spawning Exogalactic Wormhole", Verbosity.DoNotShow );
                TryToSpendBudget_WormholeInvasion_ForExogalacticWormhole( Context );
            }
            else
            {
                int numWaves = Context.RandomToUse.Next(1, 5);
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("sending " + numWaves + " waves", Verbosity.DoNotShow );
                int budget = this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.WormholeInvasion].IntValue;
                for ( int i = 0; i < numWaves; i++ )
                    this.TryToSpendBudget_Wave( Context, budget/numWaves, 0 );
                this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.WormholeInvasion] = FInt.Zero;
            }

            return true;
        }

        private void  TryToSpendBudget_WormholeInvasion_PlanetLinking( ArcenHostOnlySimContext Context )
        {
            //call the wormhole invasion manager
            int budget = this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.WormholeInvasion].IntValue;
            if ( budget < 10000 )
                this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.WormholeInvasion] += 10 * 1000;
            WormholeInvasionOptions options = WormholeInvasionOptions.CreateWithDefaults( budget, this.AttachedFaction );
            bool wasLaunched = WormholeInvasionManager.LaunchWormholeInvasion( options, Context );

            if ( wasLaunched )
                this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.WormholeInvasion] = FInt.Zero;
        }

        private readonly DrawBag<Planet> budgetPotentialPlanets = DrawBag<Planet>.Create_WillNeverBeGCed( 30, "AISentinelsFactionDeepInfo-budgetPotentialPlanets" );
        private void  TryToSpendBudget_WormholeInvasion_ForExogalacticWormhole( ArcenHostOnlySimContext Context )
        {
            //This implements the Exogalactic Wormhole invasion style

            //This function will spawn an Exogalactic Wormhole and set up some required information for
            //the wormhole. The actual spawning is done in the DoPerSecond code calling SendExogalacticWormholeInvasionIfNecessary()
            int budget = this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.WormholeInvasion].IntValue;
            byte difficulty = this.BaseInfo.SentinelInfo.AIDifficulty.Difficulty;

            Planet spawnPlanet = null;
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.GetControllingFactionType() == FactionType.Player )
                {
                    //only allow human planets to be the target.
                    var pFaction = planet.GetStanceDataForFaction( AttachedFaction );

                    int defensiveStrength = pFaction[FactionStance.Hostile].TotalStrength;

                    //Don't pick heavily defended targets
                    if ( defensiveStrength > budget / 2 )
                        continue;
                    //don't spawn on the player king planet
                    if ( FactionUtilityMethods.Instance.HasPlayerKing( planet ) )
                        continue;
                    bool adjacentToNonPlayerPlanet = false;
                    bool adjacentToPlayerKing = false;

                    foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                    {
                        if ( neighbor == null )
                            continue;
                        if ( neighbor.GetControllingFactionType() != FactionType.Player )
                            adjacentToNonPlayerPlanet = true;
                        if ( FactionUtilityMethods.Instance.HasPlayerKing( neighbor ) )
                            adjacentToPlayerKing = true;
                    }
                    if ( difficulty < 9 && adjacentToPlayerKing )
                        continue;
                    //Strongly prefer planets that are undefended and on the inside of the player's defenses
                    if ( !adjacentToNonPlayerPlanet && defensiveStrength < budget / 10 )
                        budgetPotentialPlanets.AddItem( planet, 20 );
                    else if ( !adjacentToNonPlayerPlanet && defensiveStrength < budget / 5 )
                        budgetPotentialPlanets.AddItem( planet, 10 );
                    //slight preference to players on the inside of the player's defenses
                    else if ( !adjacentToNonPlayerPlanet )
                        budgetPotentialPlanets.AddItem( planet, 2 );
                    else
                    {
                        //and finally, a tiny chance for other planets
                        budgetPotentialPlanets.AddItem( planet, 1 );
                    }
                }
            }
            if ( !budgetPotentialPlanets.GetHasItems() )
                return;
            spawnPlanet = budgetPotentialPlanets.PickRandomItemAndDoNotReplace(Context.RandomToUse);
            if(spawnPlanet == null)
                return;
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ExogalacticWormhole" );
            if(entityData == null)
                throw new Exception ("No defined ExogalacticWormhole entity in XML");
            ArcenPoint spawnLocation = spawnPlanet.GetSafePlacementPointAroundPlanetCenter(Context, entityData, FInt.FromParts( 0, 250 ), FInt.FromParts( 0, 400 ) );
            PlanetFaction pFactionForSpawn = spawnPlanet.GetPlanetFactionForFaction( AttachedFaction );
            GameEntity_Squad wormhole = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFactionForSpawn, entityData, entityData.MarkFor( pFactionForSpawn ),
                AttachedFaction.LooseFleet, 0, spawnLocation, Context, "AISentinelsWHInvBudget" );
            if ( wormhole != null )
            {
                if ( ArcenNetworkAuthority.GetIsHostMode() )
                {
                    PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.PlanetToView = wormhole.Planet;

                    World_AIW2.Instance.QueueChatMessageOrCommand( "Wormhole Invasion detected on " + wormhole.GetPlanetName_Safe(), ChatType.LogToCentralChat, "WormholeInvasion", chatHandlerOrNull );
                }
                //Now set the parameters for this invasion.
                WormholeInvasionPerUnitBaseInfo data = wormhole.CreateExternalBaseInfo<WormholeInvasionPerUnitBaseInfo>( "WormholeInvasionPerUnitBaseInfo" );
                data.RemainingTimeForWormholeInvasion = ExternalConstants.Instance.SecondsForWormholeInvasion;
                data.WormholeInvasionRemainingAttacksToLaunch = (Int16)Context.RandomToUse.Next( ExternalConstants.Instance.MinAttacksPerWormholeInvasion, ExternalConstants.Instance.MaxAttacksPerWormholeInvasion );
                data.WormholeInvasionAttackInterval = (Int16)((data.RemainingTimeForWormholeInvasion - ExternalConstants.Instance.WormholeInvasionWarningTime) / data.WormholeInvasionRemainingAttacksToLaunch);

                data.TimeUntilNextAttack = ExternalConstants.Instance.WormholeInvasionWarningTime; //the first attack comes quickly
                data.AIPurchaseCostPerAttack = budget / data.WormholeInvasionRemainingAttacksToLaunch;
                this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.WormholeInvasion] -= data.AIPurchaseCostPerAttack * data.WormholeInvasionRemainingAttacksToLaunch;
            }
        }
        private void SendExogalacticWormholeInvasionIfNecessary( ArcenHostOnlySimContext Context)
        {
            //This implements the "Exogalactic Wormhole" wormhole invasion
            if ( AttachedFaction.FactionIsDefeated )
                return; //defeating this faction will disable its income
            GameEntity_Squad wormhole = null;
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "ExogalacticWormhole" ) )
            {
                wormhole = entity;
            }
            if(wormhole == null)
            {
                return;
            }

            WormholeInvasionPerUnitBaseInfo data = wormhole.GetExternalBaseInfoAs<WormholeInvasionPerUnitBaseInfo>();
            if ( data == null )
                return;
            data.RemainingTimeForWormholeInvasion--;
            if(data.RemainingTimeForWormholeInvasion <= -5) //give it a few extra seconds
            {
                wormhole.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut); //not sure if this is the right reason
                return;
            }
            data.TimeUntilNextAttack--;
            if(data.TimeUntilNextAttack <= 0)
            {
                data.TimeUntilNextAttack = data.WormholeInvasionAttackInterval;

                PlannedWaveOptions options = PlannedWaveOptions.CreateWithDefaultsAtEntity( wormhole, false, true );
                PlannedWave waveOrNull = this.BaseInfo.PlanWave_OrGetNull( Context, data.AIPurchaseCostPerAttack, options );
                options.ReturnToPool();
                if ( waveOrNull != null )
                {
                    int warningTimeForWormholeInvasionAttack = 1;
                    waveOrNull.secondsAdvanceWarningToGive = 0;
                    waveOrNull.gameTimeInSecondsForLaunchWave = World_AIW2.Instance.GameSecond + warningTimeForWormholeInvasionAttack;
                    this.BaseInfo.WaveList.Add( waveOrNull );
                    data.WormholeInvasionRemainingAttacksToLaunch--;
                }
            }
        }

        private void SpawnCPABunkersIfNecessary( ArcenHostOnlySimContext Context )
        {
            if ( !AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "DireCPA" ) )
                return;
            AIDifficulty difficulty = this.BaseInfo.SentinelInfo.AIDifficulty;
            
            if ( World_AIW2.Instance.GameSecond % difficulty.BaseBunkerSpawnInterval == 0 )
            {
                GameEntityTypeData entityData =  GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "CPABunker");
                if ( entityData == null )
                    throw new Exception("Could not find CPABunker in XML");
                Planet planet = GetBunkerPlanet( Context );
                if ( planet == null )
                    return;
                ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData,  FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 700 ) );
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( this.AttachedFaction );
                GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                    pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "AISentinelsOnePerPlanetStarting" ); //fine because mapgen
                if ( entity != null )
                {
                    if ( entity.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
                    {
                        SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                        if ( chatHandlerOrNull != null )
                            chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( entity );

                        World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "CPA Bunker</color> spawning on " +
                                                                       entity.GetPlanetName_Safe(), ChatType.LogToCentralChat, "", chatHandlerOrNull );
                    }
                    else
                        World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "CPA Bunker</color> spawning somewhere in the galaxy",
                                                                       ChatType.LogToCentralChat, "", null );
                }
            }
        }
        private Planet GetBunkerPlanet( ArcenHostOnlySimContext Context )
        {
            List<Planet> PotentialPlanets = Planet.GetTemporaryPlanetList( "AISent-GetBunkerPlanet-PotentialPlanets", 100f );
            if ( PotentialPlanets == null ) //blocked for teardown/shutdown; bail
                return null;
            FInt AIP = FactionUtilityMethods.Instance.GetCurrentAIP();
            Int16 minHopsFromHumanPlanet = -1;
            Int16 maxHopsFromHumanPlanet = -1;
            byte maxMarkLevel = 2;
            if ( AIP <= 100 )
            {
                minHopsFromHumanPlanet = 2;
                maxHopsFromHumanPlanet = 5;
                maxMarkLevel = 3;
            }
            else if ( AIP <= 200 )
            {
                minHopsFromHumanPlanet = 3;
                maxHopsFromHumanPlanet = 6;
                maxMarkLevel = 4;
            }
            else if ( AIP <= 400 )
            {
                minHopsFromHumanPlanet = 4;
                maxHopsFromHumanPlanet = 10;
                maxMarkLevel = 5;
            }
            else
            {
                minHopsFromHumanPlanet = 5;
                maxHopsFromHumanPlanet = 12;
                maxMarkLevel = 7;
            }
            bool debug = false;
            int allowedRetries = 6; //was 100, and that's likely to break the game in the late game.
            int retries = 0;
            do
            {
                if ( retries > 0 )
                {
                    //the previous attempt was too restrictive
                    minHopsFromHumanPlanet--;
                    maxHopsFromHumanPlanet++;
                    maxMarkLevel++;
                }
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( this.AttachedFaction );

                    if ( planet.PopulationType == PlanetPopulationType.AIHomeworld ||
                         planet.PopulationType == PlanetPopulationType.AIBastionWorld )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because it was an AI homeworld at some point.", Verbosity.DoNotShow );
                        continue;
                    }
                    if ( planet.GetControllingFaction() != this.AttachedFaction )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because it is not owned by this faction.", Verbosity.DoNotShow );
                        continue;
                    }
                    if ( planet.GetStanceDataForFaction( this.AttachedFaction )[FactionStance.Self].TotalStrength < 10 * 1000 )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because it has < 10 strength.", Verbosity.DoNotShow );
                        continue;
                    }
                    bool foundBunker = false;
                    foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "CPABunker" ) )
                    {
                        if ( entity.Planet == planet )
                        {
                            foundBunker = true;
                            break;
                        }
                    }
                    if ( foundBunker )
                        continue;
                    bool adjacentHomeworld = false;
                    foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                    {
                        if ( neighbor.PopulationType == PlanetPopulationType.AIHomeworld ||
                         neighbor.PopulationType == PlanetPopulationType.AIBastionWorld )
                        {
                            adjacentHomeworld = true;
                            break;
                        }
                    }
                    if ( adjacentHomeworld == true )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because its adjacent to n AI homeworld planet", Verbosity.DoNotShow );
                        continue;
                    }

                    //Planet must belong to an allied faction
                    if ( !planet.GetControllingFaction().GetIsFriendlyTowards( this.AttachedFaction ) )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because the planet isnt friendly", Verbosity.DoNotShow );
                        continue;
                    }
                    //planet must not be a King planet, or adjacent to a king planet
                    if ( planet.GetDataByStanceForFaction( this.AttachedFaction, FactionStance.Friendly ).HasKingUnitPresent )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because its a king planet", Verbosity.DoNotShow );
                        continue;
                    }
                    if ( planet.MarkLevelForAIOnly.Ordinal > maxMarkLevel )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because its mark level " + planet.MarkLevelForAIOnly.Ordinal + " > allowed mark level " + maxMarkLevel, Verbosity.DoNotShow );
                        continue;
                    }
                    bool adjacentKing = false;
                    foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                    {
                        if ( neighbor.GetDataByStanceForFaction( this.AttachedFaction, FactionStance.Friendly ).HasKingUnitPresent )
                        {
                            adjacentKing = true;
                            break;
                        }
                    }
                    if ( adjacentKing == true )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because its adjacent to a king planet", Verbosity.DoNotShow );
                        continue;
                    }

                    if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength > FInt.Zero )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because there are enemies on it", Verbosity.DoNotShow );

                        continue;
                    }
                    //Check if there actually are any players in the game, to see whether or not we should skip the distance condition for spawning bunkers
                    bool humanAmongUs = false;
                    for(int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                    {
                        if ( World_AIW2.Instance.Factions[i].Type == FactionType.Player && !World_AIW2.Instance.Factions[i].PlayerTypeDataOrNull_ModeratelyExpensive.IsSpectator )
                        {
                            humanAmongUs = true;
                            break;
                        }
                            
                    }
                    if( humanAmongUs )
                    {
                        if ( minHopsFromHumanPlanet > 1 )
                        {
                            //this planet must not be too close to a player planet
                            //to make the bounds listed above inclusive minHops - 1 must be used
                            bool foundPlayerPlanetWithinMinHops = false;
                            foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops_NoFilters( (Int16)(minHopsFromHumanPlanet - 1) ) )
                            {
                                Planet otherPlanet = _phd.Planet;
                                if ( otherPlanet.GetControllingFactionType() == FactionType.Player )
                                    foundPlayerPlanetWithinMinHops = true;
                            }
                            if ( foundPlayerPlanetWithinMinHops )
                                continue;
                        }
                        if ( maxHopsFromHumanPlanet > 0 )
                        {
                            //this planet can't be too far from a player planet
                            bool foundPlayerPlanetWithinMaxHops = false;
                            foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops_NoFilters( maxHopsFromHumanPlanet ) )
                            {
                                Planet otherPlanet = _phd.Planet;
                                if ( otherPlanet.GetControllingFactionType() == FactionType.Player )
                                    foundPlayerPlanetWithinMaxHops = true;
                            }
                            if ( !foundPlayerPlanetWithinMaxHops )
                                continue;
                        }
                    }
                    
                    PotentialPlanets.Add( planet );
                }
                retries++;
            } while ( PotentialPlanets.Count == 0 && retries < allowedRetries );
            if ( PotentialPlanets.Count == 0 )
            {
                Planet.ReleaseTemporaryPlanetList( PotentialPlanets );
                return null;
            }
            Planet ret = PotentialPlanets[Context.RandomToUse.Next(0, PotentialPlanets.Count)];
            Planet.ReleaseTemporaryPlanetList( PotentialPlanets );
            return ret;
        }

        //overrideBudget uses a different budget from the standard StoredStrengthByBudget (used for cancelled waves)
        private void TryToSpendBudget_Wave(ArcenHostOnlySimContext Context, int overrideBudget, int overrideLaunchTime)
        {
            //This function now calls PlanWave to queue a new wave

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Wave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AISent-TryToSpendBudget_Wave-trace", 10f ) : null;
            bool debug = false;

            int budget = this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.Wave].IntValue;
            if ( overrideBudget != 0 )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "override budget\n", Verbosity.DoNotShow );
                budget = overrideBudget;
            }

            PlannedWaveOptions options = PlannedWaveOptions.CreateWithDefaultsAtSpecificLaunchTime( overrideLaunchTime );
            options.spawnBonusShipFromAIType = true;
            PlannedWave wave = this.BaseInfo.PlanWave_OrGetNull( Context, budget, options );
            options.ReturnToPool();
            options = null;

            if ( wave == null )
            {
                tracingBuffer?.Add( "No valid wave targets or no Wave Types enabled. not sending a wave\n" );

                if ( overrideBudget != 0)
                {
                    //A wave was cancelled and couldn't find a suitable target (perhaps all targets are too heavily defended now?). Lets buff the next wave.
                    //NOTA BENE: If we start to use TryTospendbudget_wave anywhere else then this logic may need to be revisited
                    this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.Wave] += budget;
                }

                return;
            }

            tracingBuffer?.Add( "Spending wave budget; queueing a wave with budget " + budget + " (override: " + overrideBudget + ", previous: " + this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.Wave] + ")\n");

            if ( overrideBudget == 0 )
                this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.Wave] -= budget;

            if ( overrideLaunchTime != 0 )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "override launch time\n", Verbosity.DoNotShow );
                
                wave.gameTimeInSecondsForLaunchWave = overrideLaunchTime;
            }

            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }

            this.BaseInfo.WaveList.Add( wave ); 
        }

        private void TryToSpendBudget_Warden(ArcenHostOnlySimContext Context)
        {
            //The warden budget is distributed to the warden
            //for this faction. Any remainder is dumped to the Reinforcement budget

            AIBudgetType budgetType = AIBudgetType.Warden;

            FInt amountPerFaction = this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budgetType];
            AIWardenCoreData otherFactionExternal = this.BaseInfo.WardenInfo;
            if ( otherFactionExternal == null )
                return;
            FInt amountDonated = amountPerFaction - otherFactionExternal.ReceiveDonation( amountPerFaction, AttachedFaction, null );
            this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budgetType] -= amountDonated;

            if ( this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budgetType] > 0 )
            {
                //donate excess
                this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.Reinforcement] += this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budgetType];
                this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budgetType] = FInt.Zero;
            }
        }
        private void TryToSpendBudget_PraetorianGuard( ArcenHostOnlySimContext Context )
        {
            //The Praetorian Guard budget is given evenly across to the praetorian guard subfaction
            //for this faction. Any remainder is dumped to the Hunter budget
            AIBudgetType budgetType = AIBudgetType.PraetorianGuard;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.BudgetSpend );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AISent-HandleWormholeBorers_MainSim-trace", 10f ) : null;
            FInt amountPerFaction = this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budgetType];
            if ( !World_AIW2.Instance.GetIsTutorial() ) //don't give it to the PG during the tutorial
            {
                AIPraetorianGuardCoreData otherFactionExternal = this.BaseInfo.PraetorianInfo;
                if ( otherFactionExternal == null )
                    return;
                FInt amountDonated = amountPerFaction - otherFactionExternal.ReceiveDonation( amountPerFaction, AttachedFaction, null );
                this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budgetType] -= amountDonated;
                if ( tracing )
                    tracingBuffer.Add(budgetType + " amount is now " + this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budgetType]);

            }

            if ( this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budgetType] > 0 )
            {
                //donate excess
                this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.HunterFleet] += this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budgetType];
                this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budgetType] = FInt.Zero;
            }
            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracingBuffer != null )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion

        }

        private void TryToSpendBudget_HunterFleet(ArcenHostOnlySimContext Context)
        {
            //The Hunter Fleet budget is distributed to the hunter
            //for this faction. Any remainder is dumped to the Wave budget

            AIBudgetType budgetType = AIBudgetType.HunterFleet;
            FInt amountPerFaction = this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budgetType];
            AIHunterCoreData otherFactionExternal = this.BaseInfo.HunterInfo;
            if ( otherFactionExternal == null )
                return;
            FInt amountDonated = amountPerFaction - otherFactionExternal.ReceiveDonation( amountPerFaction, AttachedFaction, null );
            this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budgetType] -= amountDonated;

            if ( this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budgetType] > 0 )
            {
                //donate excess to waves
                this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.Wave] += this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budgetType];
                this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budgetType] = FInt.Zero;
            }
        }

        private readonly List<SafeSquadWrapper> possibleDeploymentPointsAtOrBelowMark = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "AISentinelsFactionDeepInfo-possibleDeploymentPointsAtOrBelowMark" );
        private readonly List<SafeSquadWrapper> possibleDeploymentPointsOneAboveMark = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "AISentinelsFactionDeepInfo-possibleDeploymentPointsOneAboveMark" );
        private readonly List<SafeSquadWrapper> possibleDeploymentPointsTwoAboveMark = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "AISentinelsFactionDeepInfo-possibleDeploymentPointsTwoAboveMark" );
        private readonly List<SafeSquadWrapper> possibleDeploymentPointsHigherAboveMark = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "AISentinelsFactionDeepInfo-possibleDeploymentPointsHigherAboveMark" );

        private void TryToSpendBudget_CPA(ArcenHostOnlySimContext Context, bool ActuallyDoLogic )
        {
            bool debug = false;
            if ( debug ) { }

            //ActuallyDoLogic is true when actually spawning units, and false when we are checking
            //whether to queue a wave. We queue a wave in order to give the player suitable warning
            if(!ActuallyDoLogic)
            {
                //When we launch a CPA, its still on the queuedWaves list (we don't remove entries until we've sent all the waves
                //we will send this Second). So don't bail out early in that case
                ProtectedList<PlannedWave> queuedWaves = this.BaseInfo.WaveList;
                if ( queuedWaves != null && queuedWaves.Count > 0 )
                {
                    PlannedWave wave;
                    for ( int i = 0; i < queuedWaves.Count; i++ )
                    {
                        wave = queuedWaves[i];
                        if ( wave.isActuallyACrossPlanetAttack )
                            return; //already have a CPA on the way; don't create another one!
                    }
                }
            }
            if( AISentinelsFactionBaseInfo.DebugWaveAndCPASpawns )
                ArcenDebugging.ArcenDebugLogSingleLine("Trying to spend CPA budget of " + this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.CPA] + "  at " + World_AIW2.Instance.GameSecond + ". ActuallyDoLogic " + ActuallyDoLogic, Verbosity.DoNotShow );

            bool launchedSomething = false;
            if ( !ActuallyDoLogic )
            {
                #region Queue CPA PlannedWave entry
                PlannedWave wave = PlannedWave.GetFromPoolOrCreate();
                
                wave.SendingFactionIndex = AttachedFaction.FactionIndex;
                string waveWarning = World_AIW2.Instance.Setup.GetStringBySetting( "WaveWarning" );
                Int16 secondsOfWarningToGive = 0;
                secondsOfWarningToGive = 600; //always give 10 minutes warning for CPAs, they can be super scary
                if ( AttachedFaction.Debug_ImmediatelyLaunchCPA )
                    secondsOfWarningToGive = 30;
                if(!ActuallyDoLogic)
                {
                    wave.gameTimeInSecondsForLaunchWave = secondsOfWarningToGive + World_AIW2.Instance.GameSecond;
                    wave.secondsAdvanceWarningToGive = secondsOfWarningToGive;
                    wave.playerBeingAlerted = true;
                    wave.isActuallyACrossPlanetAttack = true;
                    if ( AISentinelsFactionBaseInfo.DebugWaveAndCPASpawns )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Spawn CPA in " + secondsOfWarningToGive + " seconds", Verbosity.DoNotShow );
                    this.BaseInfo.WaveList.Add( wave );
                }
                #endregion
            }
            if(!ActuallyDoLogic)
                return;

            int currentGeneralMarkLevel = AttachedFaction.CurrentGeneralMarkLevel;
            if ( currentGeneralMarkLevel < 2 )
                currentGeneralMarkLevel = 2;

            possibleDeploymentPointsAtOrBelowMark.Clear();
            possibleDeploymentPointsOneAboveMark.Clear();
            possibleDeploymentPointsTwoAboveMark.Clear();
            possibleDeploymentPointsHigherAboveMark.Clear();
            foreach ( GameEntity_Squad guardPost in AttachedFaction.Squads( EntityRollupType.ReinforcementLocations ) )
            {
                if ( guardPost.AIReinforcementPointContents == null || guardPost.AIReinforcementPointContents.Count <= 0 )
                    continue;
                if ( guardPost.Planet == null || guardPost.Planet.PopulationType == PlanetPopulationType.AIHomeworld || 
                guardPost.Planet.PopulationType == PlanetPopulationType.AIBastionWorld )
                    continue;

                if ( guardPost.CurrentMarkLevel <= currentGeneralMarkLevel )
                    possibleDeploymentPointsAtOrBelowMark.Add( guardPost );
                else if ( guardPost.CurrentMarkLevel <= currentGeneralMarkLevel + 1 )
                    possibleDeploymentPointsOneAboveMark.Add( guardPost );
                else if ( guardPost.CurrentMarkLevel <= currentGeneralMarkLevel + 2 )
                    possibleDeploymentPointsTwoAboveMark.Add( guardPost );
                else
                    possibleDeploymentPointsHigherAboveMark.Add( guardPost );
            }

            float remainingBudgetForCPAToPullFromGuardPosts = this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.CPA].ToFloatNonSim();

            int[] countsByMarkLevel = new int[8];
            int totalCount = 0;
            int totalStrength = 0;

            GameCommand transferToCPALogic = null;
            bool useTsunami = World_AIW2.Instance.Setup.GetBoolBySetting( "TsunamiCPA" );
            Faction factionForBunkers = this.AttachedFaction;
            if ( useTsunami )
            {
                Faction cpaLogic = this.BaseInfo.SubFac_CPA;
                if ( cpaLogic != null )
                {
                    transferToCPALogic = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransferEntitiesToFaction], GameCommandSource.AnythingElse );
                    transferToCPALogic.RelatedFactionIndex = cpaLogic.FactionIndex;
                    factionForBunkers = cpaLogic;
                }
            }

            int bunkerStrength = 0;
            int numBunkers = 0;
            if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "DireCPA" ) )
            {
                //Handle CPA bunkers
                AIDifficulty difficulty = this.BaseInfo.SentinelInfo.AIDifficulty;
                int strengthPerBunker = this.BaseInfo.GetCPABunkerStrength();
                Dictionary<GameEntityTypeData, int> compositionToFill = GameEntityTypeData.GetTemporaryGameEntityTypeDataIntDict( "AISentinelsFactionDeepInfo-TryToSpendBudget_CPA-compositionToFill", 10f );
                if ( compositionToFill == null ) //blocked for teardown/shutdown; bail
                    return;
                ThrowawayDrawBagCanMemLeak<GameEntityTypeData> unitBagToFill = ThrowawayDrawBagCanMemLeak<GameEntityTypeData>.Create_WillActuallyBeGCed( 30 );

                AIShipGroupCategory shipGroupCat = null;
                AIShipGroup shipGroup = null;
                
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "CPABunker" ) )
                {
                    numBunkers++;
                    compositionToFill.Clear();
                    unitBagToFill.Clear();
                    AITypeData AIType = this.AttachedFaction.TryGetAISentinelsCoreData().SentinelInfo.AIType;
                    shipGroupCat = AIType.BudgetItems[AIBudgetType.Reinforcement].GuardianAIShipGroup;
                    if ( shipGroupCat != null )
                    {
                        shipGroup = shipGroupCat.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                        if ( shipGroup != null )
                            unitBagToFill.CopyFrom( shipGroup.DrawBag );
                    }
                    shipGroupCat = AIType.BudgetItems[AIBudgetType.Reinforcement].NormalAIShipGroup;
                    if ( shipGroupCat != null )
                    {
                        shipGroup = shipGroupCat.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                        if ( shipGroup != null )
                            unitBagToFill.AddAllFromOtherToThis( shipGroup.DrawBag );
                    }
                    
                    ArcenDebugging.ArcenDebugLogSingleLine("Spawning " + strengthPerBunker + " strength for " + entity.ToString(), Verbosity.DoNotShow );
                    int unusedParameter = -1;
                    int Spent = this.AttachedFaction.FillComposition( Context, strengthPerBunker, unusedParameter, compositionToFill, unitBagToFill,
                                                                      this.AttachedFaction.CurrentGeneralMarkLevel, 0 );
                    bunkerStrength += Spent;
                    foreach ( KeyValuePair<GameEntityTypeData, int> pair in compositionToFill )
                    {
                        int totalSquadsToSpawn = pair.Value;
                        GameEntityTypeData entityType = pair.Key;
                        int numStacksPerSquad = 0;
                        int separateSquadsToSpawn = totalSquadsToSpawn;
                        int remainder = 0;
                        int StackingCutoff = AIWar2GalaxySettingQuickAccess.StackingCutoffNPCs;
                        if ( StackingCutoff <= 0 )
                            throw new Exception( "Undefined StackingCutoffNPCs; this means that waves won't spawn" );
                        if ( totalSquadsToSpawn > StackingCutoff )
                        {
                            separateSquadsToSpawn = StackingCutoff;
                            numStacksPerSquad = totalSquadsToSpawn / separateSquadsToSpawn;
                            remainder = totalSquadsToSpawn % separateSquadsToSpawn;
                        }

                        for ( int j = 0; j < separateSquadsToSpawn; j++ )
                        {
                            ArcenPoint exitPoint = entity.WorldLocation;
                            Planet spawnPlanet = entity.Planet;
                            FInt minRadius = FInt.FromParts( 0, 005 );
                            FInt maxRadius = FInt.FromParts( 0, 060 );
                            if ( separateSquadsToSpawn > 60 )
                                maxRadius = FInt.FromParts( 0, 100 );
                            ArcenPoint spawnLocation = spawnPlanet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, entityType, exitPoint, minRadius, maxRadius );
                            
                            GameEntity_Squad newEntity = factionForBunkers.SpawnNewUnit_ReturnNullIfMPClient(
                              Context, spawnPlanet, spawnLocation, entityType, entityType.MarkFor( factionForBunkers.CurrentGeneralMarkLevel ),
                              factionForBunkers.LooseFleet, 0,
                              EntityBehaviorType.Attacker_Full, -1, null, "CPABUnkerDeployment" );
                            
                            if ( newEntity == null )
                                continue;

                            if ( numStacksPerSquad > 0 )
                            {
                                if ( totalSquadsToSpawn < numStacksPerSquad )
                                {
                                    entity.AddOrSetExtraStackedSquadsInThis( (Int16)totalSquadsToSpawn, true );
                                }
                                else
                                    entity.AddOrSetExtraStackedSquadsInThis( (Int16)(numStacksPerSquad - 1), true ); //don't count the original unit
                                if ( remainder > 0 )
                                {
                                    entity.AddOrSetExtraStackedSquadsInThis( 1, false );
                                    remainder--;
                                }
                                totalSquadsToSpawn -= entity.ExtraStackedSquadsInThis + 1;
                            }
                        }
                    }
                }
                GameEntityTypeData.ReleaseTemporaryGameEntityTypeDataIntDict( compositionToFill );
            }
            
            List<SafeSquadWrapper> possibleDeploymentPointsCurrent = possibleDeploymentPointsAtOrBelowMark;
            for ( int k = 0; k < 4; k++ )
            {
                if ( k == 1 )
                    possibleDeploymentPointsCurrent = possibleDeploymentPointsOneAboveMark;
                else if ( k == 2 )
                    possibleDeploymentPointsCurrent = possibleDeploymentPointsTwoAboveMark;
                else if ( k == 3 )
                    possibleDeploymentPointsCurrent = possibleDeploymentPointsHigherAboveMark;
                else
                    possibleDeploymentPointsCurrent = possibleDeploymentPointsAtOrBelowMark;

                //we randomize below already!
                //ArcenArrays.Randomize( possibleDeploymentPointsCurrent, Context.RandomToUse );
                //ArcenArrays.Randomize( possibleDeploymentPointsCurrent, Context.RandomToUse );
                //ArcenArrays.Randomize( possibleDeploymentPointsCurrent, Context.RandomToUse );

                int attemptsLeft = 8000;

                while ( remainingBudgetForCPAToPullFromGuardPosts > 0 && 
                        possibleDeploymentPointsCurrent.Count > 0 && 
                        attemptsLeft-- > 0 )
                {
                    GameEntity_Squad guardPost = possibleDeploymentPointsCurrent[Context.RandomToUse.Next( 0, possibleDeploymentPointsCurrent.Count )].GetSquad();
                    if ( guardPost == null )
                        continue;
                    RefPair<GameEntityTypeData, int> record = guardPost.AIReinforcementPointContents[Context.RandomToUse.Next( 0, guardPost.AIReinforcementPointContents.Count )];

                    float costToSetFree = (float)record.LeftItem.CostForAIToPurchase;
                    GameEntityTypeData.MarkLevelStats baselineMarkStats = record.LeftItem.MarkStatsFor( AttachedFaction.CurrentGeneralMarkLevel );
                    GameEntityTypeData.MarkLevelStats targetMarkStats = record.LeftItem.MarkStatsFor( guardPost.Planet.MarkLevelForAIOnly.Ordinal );
                    if ( baselineMarkStats.StrengthPerSquad_CalculatedWithNullFleetMembership != targetMarkStats.StrengthPerSquad_CalculatedWithNullFleetMembership && baselineMarkStats.StrengthPerSquad_CalculatedWithNullFleetMembership != 0 )
                    {
                        float ratio = (float)targetMarkStats.StrengthPerSquad_CalculatedWithNullFleetMembership / (float)baselineMarkStats.StrengthPerSquad_CalculatedWithNullFleetMembership;
                        costToSetFree *= ratio; //make us spend more or less based on the relative strengths
                    }
                    if ( useTsunami )
                    {
                        int multiplier= AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "TsunamiCostModifier" );
                        if ( multiplier == -1 )
                            multiplier = 40;
                        costToSetFree *= (1.0f * multiplier) / 100;
                    }

                    float totalCost = costToSetFree;
                    int numberToSetFree = 1;
                    
                    //max of 99 per stack
                    while ( numberToSetFree < record.RightItem && totalCost + costToSetFree < remainingBudgetForCPAToPullFromGuardPosts && numberToSetFree < 99 )
                    {
                        totalCost += costToSetFree;
                        numberToSetFree++;
                    }

                    GameEntity_Squad freedStack = AttachedFaction.SpawnNewUnit_ReturnNullIfMPClient( Context, guardPost.Planet, guardPost.WorldLocation, record.LeftItem, targetMarkStats.MarkLevel.Ordinal,
                        AttachedFaction.LooseFleet, 0, EntityBehaviorType.Attacker_Full, -1, null, "AISentinelsFreedForCPA" ); // to have it launch the CPA against something other than the humans, replace that -1 with the target index
                    if ( freedStack == null ) 
                        continue; //mp client
                    freedStack.AddOrSetExtraStackedSquadsInThis( (Int16)(numberToSetFree - 1), true );
                    totalCount += numberToSetFree;
                    countsByMarkLevel[targetMarkStats.MarkLevel.Ordinal] += numberToSetFree;
                    totalStrength += (targetMarkStats.StrengthPerSquad_CalculatedWithNullFleetMembership * numberToSetFree);

                    remainingBudgetForCPAToPullFromGuardPosts -= totalCost;
                    guardPost.AddToAIReinforcementPointContents( record.LeftItem, -numberToSetFree, "SetGuardsFree", string.Empty ); //we just removed ships from the guard post for the CPA, so take them out
                    if ( guardPost.AIReinforcementPointContents == null || guardPost.AIReinforcementPointContents.Count == 0 )
                        possibleDeploymentPointsCurrent.Remove( guardPost );
                    launchedSomething = true;
                    if ( transferToCPALogic != null )
                        transferToCPALogic.RelatedEntityIDs.Add( freedStack.PrimaryKeyID );
                }
            }
            if ( transferToCPALogic != null )
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, transferToCPALogic, false );
            
            this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.CPA] = FInt.CreateFromDoubleNonSim( remainingBudgetForCPAToPullFromGuardPosts );

            if ( AISentinelsFactionBaseInfo.DebugWaveAndCPASpawns )
                ArcenDebugging.ArcenDebugLogSingleLine("After launching CPA, my remaining budget is " + this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[AIBudgetType.CPA], Verbosity.DoNotShow );
            
            if ( launchedSomething )
            {
                StringBuilder builder = new StringBuilder();
                builder.Append( "Cross planet attack launched across the galaxy!  " );
                builder.Append("<color=#a1ffa1>").Append( totalCount.ToString( "#,##0" ) ).Append( "</color> former guard ships of total strength <color=#ffa1a1>" ).Append( ((float)(totalStrength + bunkerStrength) / 1000.0f).ToString( "#,##0.#" ) ).Append( "</color> are now hunting you." );
                if ( bunkerStrength > 0 )
                    builder.Append("\nThe Dire CPA strength of that total was <color=#ffa1a1>" ).Append( ((float)bunkerStrength / 1000.0f).ToString( "#,##0.#" ) ).Append( "</color> from <color=#a1a1bb>" + numBunkers + "</color> CPA Bunkers." );
                bool isFirst = true;
                for ( int i = 0; i < countsByMarkLevel.Length; i++ )
                {
                    int countHere = countsByMarkLevel[i];
                    if ( countHere <= 1 )
                        continue;
                    if ( isFirst )
                    {
                        builder.Append( "  By mark level, they number: " );
                        isFirst = false;
                    }
                    else
                        builder.Append( ", " );
                    builder.Append( countHere.ToString( "#,##0" ) );
                    if ( i == 0 )
                        builder.Append( " markless" );
                    else
                        builder.Append( " mk" ).Append( i );
                }
                if ( !isFirst )
                    builder.Append( "." );
                if ( World_AIW2.Instance.Setup.GetIntBySetting( "TsunamiCostModifier" ) < 30 )
                    builder.Append( "  <color=#ff3722>Your tsunami cost multiplier is set to the low value of " )
                        .Append( World_AIW2.Instance.Setup.GetIntBySetting( "TsunamiCostModifier" ) )
                        .Append( " -- enjoy death.</color>" );
                if ( ArcenNetworkAuthority.GetIsHostMode() )
                    World_AIW2.Instance.QueueChatMessageOrCommand( builder.ToString(), ChatType.LogToCentralChat, "CrossPlanetAttackStarted", null );
                //Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SFXItemType_NonPositional.CrossPlanetAttackStarted );
            }
        }

        private void TryToSpendBudget_Reconquest(ArcenHostOnlySimContext Context)
        {
            //Reconquest waves are only spawned if they think they can win
            bool debug = false;

            bool reconquestWavesOn = World_AIW2.Instance.Setup.GetBoolBySetting( "ReconquestWave" );
            if ( !reconquestWavesOn )
                return;

            AIBudgetType budgetType = AIBudgetType.Reconquest;
            if(debug)
                ArcenDebugging.ArcenDebugLogSingleLine("At " + World_AIW2.Instance.GameSecond + " consider sending a reconquest wave with strength  " + this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budgetType] + " strength", Verbosity.DoNotShow);
            int budget = this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budgetType].IntValue;

            PlannedWaveOptions options = PlannedWaveOptions.CreateReconquestWave();
            PlannedWave wave = this.BaseInfo.PlanWave_OrGetNull( Context, budget, options );
            options.ReturnToPool();

            if (wave != null)
            {
                if(debug)
                    ArcenDebugging.ArcenDebugLogSingleLine("Sending a reconquest wave with " + wave.aiCostBudgetForWave + " cost budget", Verbosity.DoNotShow );

                this.BaseInfo.WaveList.Add(wave);
                this.BaseInfo.SentinelInfo.StoredAIPurchaseCostByBudget[budgetType] -= wave.aiCostBudgetForWave;
                World_AIW2.Instance.GetPlanetByIndex(wave.targetPlanetIdx).LastReconquestAttempt = World_AIW2.Instance.GameSecond;
            }
        }
        
        //returns a list of all the created entities; many times we want to give these entities orders afterwards (like if this is an Exo or Cross Plane Wave)
        public int SendWave(ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData,  int Budget, GameEntity_Squad OverrideEntityToSpawnAt, GameEntityTypeData OverrideEntityTypeToSend, 
            Int16 TargetFactionIndex, bool allowGuardians, bool allowDireGuardians = false)
        {
            bool debug = false;

            Tutorial tutorialData = World_AIW2.Instance.TutorialOrNull;
            if ( tutorialData != null && tutorialData.SkipAllWaves )
                return 0;

            PlannedWaveOptions options = PlannedWaveOptions.CreateWithBasics( OverrideEntityToSpawnAt, OverrideEntityTypeToSend,
                World_AIW2.Instance.GetFactionByIndex( TargetFactionIndex ), allowGuardians, allowDireGuardians );
            options.spawnBonusShipFromAIType = false;
            PlannedWave wave = this.BaseInfo.PlanWave_OrGetNull( Context, Budget, options );
            options.ReturnToPool();

            if ( wave == null )
            {
                //note that one way this can be null is if the Dark Spire sets off a Raid Engine
                //TODO: in that case it should just make a Threat Wave
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "No valid wave possible for SendWave (hacking or raid engine)", Verbosity.DoNotShow );
                
                return 0;
            }
            
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Spending wave budget; queing a wave with budget " + Budget + ". wave being queued is ", Verbosity.DoNotShow );
            
            this.SpawnWave( Context, PathCacheData, wave, TargetFactionIndex, OverrideEntityToSpawnAt);
            
            //I'm not sure if it's better to use the SpawnWave mechanism or the AddWave mechanism, but definitely don't do both
//            wave.gameTimeInSecondsForLaunchWave = World_AIW2.Instance.GameSecond;
//            faction.AddWave( wave ); //store this in external data
            return Budget;
        }

        public Planet GetKingPlanet( out bool IsKingUnitMobile )
        {
            IsKingUnitMobile = false;
            GameEntity_Squad king = null;
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                king = entity;
                break;
            }
            if(king == null)
            {
                return null;
            }
            if ( king.TypeData.IsMobile )
                IsKingUnitMobile = true;
            return king.Planet;
        }
        public bool GetIsKingUnderAttack( Planet kingPlanet)
        {
            if(kingPlanet == null)
                return false;
            var kingFactionData = kingPlanet.GetStanceDataForFaction( AttachedFaction );
            StrengthData_PlanetFaction_Stance friendlyData = kingFactionData[FactionStance.Friendly];
            StrengthData_PlanetFaction_Stance selfData = kingFactionData[FactionStance.Self];
            StrengthData_PlanetFaction_Stance hostileData = kingFactionData[FactionStance.Hostile];
            if ( hostileData.TotalStrength >= (selfData.TotalStrength + friendlyData.TotalStrength)/10 ) //this is an attack of at least 1/10th of the homeworld's defenses
            {
                return true;
            }
            return false;
        }

        //Actually spawns the wave
        public void SpawnWave( ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData, PlannedWave wave, Int16 TargetFactionIndex, GameEntity_Squad overrideSpawnLocation = null )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //YIKES do not do this on a client!

            int debugCode = 0;
            try
            {
                debugCode = 100;
                if ( wave.isActuallyACrossPlanetAttack )
                {
                    if ( AISentinelsFactionBaseInfo.DebugWaveAndCPASpawns )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Spawn CPA Now!", Verbosity.DoNotShow );
                    
                    //create a CPA, not a wave!
                    TryToSpendBudget_CPA( Context, true );
                    return;
                }
                
                debugCode = 200;
                if ( AISentinelsFactionBaseInfo.DebugWaveAndCPASpawns )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Spawn regular wave now! Faction: " + AttachedFaction.GetDisplayName(), Verbosity.DoNotShow );
                
                bool debug = false;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "SpawnWave invoked, target faction index " + TargetFactionIndex + " Faction: " + AttachedFaction.GetDisplayName(), Verbosity.DoNotShow );
                
                GameEntity_Base entityToSpawnAt = overrideSpawnLocation;
                if ( entityToSpawnAt == null )
                {
                    if ( wave.overrideEntityToSpawnAt != null )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Spawning at override from Wave code", Verbosity.DoNotShow );
                        entityToSpawnAt = wave.overrideEntityToSpawnAt;
                    }
                }
                
                debugCode = 300;
                Planet targetPlanet = null;
                if ( entityToSpawnAt == null )
                {
                    debugCode = 400;
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Finding a suitable spawn entity", Verbosity.DoNotShow );
                    
                    Planet warpGatePlanet = World_AIW2.Instance.GetPlanetByIndex( wave.planetWithWarpGateIdx );
                    targetPlanet = World_AIW2.Instance.GetPlanetByIndex( wave.targetPlanetIdx );
                    
                    debugCode = 401;
                    Faction targetFaction = World_AIW2.Instance.GetFactionByIndex( TargetFactionIndex );
                    if ( targetFaction == null && targetPlanet != null )
                        targetFaction = targetPlanet.GetControllingFaction(); //target faction here is only used for the audio cue
                    
                    //no AI taunts here, either -- wait until we're ready to actually see the wave.
                    debugCode = 402;
                    if ( wave.spawnWaveDirectlyOnTarget )
                    {
                        debugCode = 410;
                        if ( wave.planetWithWarpGateIdx == wave.targetPlanetIdx )
                        {
                            debugCode = 411;
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Spawning wave on AI planet " + warpGatePlanet.Name + " at warp gate ", Verbosity.DoNotShow );
                            debugCode = 412;
                            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( EntityRollupType.WarpEntryPoints ) )
                            {
                                                                if ( entity.Planet == warpGatePlanet )
                                                                    entityToSpawnAt = entity;
                                                            }
                            debugCode = 413;
                        }
                        else
                        {
                            debugCode = 414;
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Spawning wave directly on target. find the wormhole from " + warpGatePlanet.Name + " to " + targetPlanet.Name, Verbosity.DoNotShow );
                            if ( targetPlanet == null )
                            {
                                //this seems to happen sometimes with wormhole invasions? I've put in a potential fix for it, but
                                //in the meantime lets make sure we don't crash anything
                                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( EntityRollupType.WarpEntryPoints ) )
                                {
                                                                    if ( entity.Planet == warpGatePlanet )
                                                                        entityToSpawnAt = entity;
                                                                }
                            }
                            else
                                entityToSpawnAt = targetPlanet.GetWormholeTo( warpGatePlanet );
                        }
                    }
                    else
                    {
                        debugCode = 420;
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Spawning wave on remote planet and travel to target planet\n", Verbosity.DoNotShow );
                        
                        //spawn this wave on the planet with a warp gate
                        //changed to checking the whole world to make it work in the scenario a wave from one AI would need to spawn on another AI's warpgate
                        foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.WarpEntryPoints ) )
                        {
                                                            if ( entity.Planet == warpGatePlanet )
                                                                entityToSpawnAt = entity;
                                                        }
                        if ( entityToSpawnAt == null )
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "The warp gate has been destroyed. Spawning wave instead at the Master Controller\n", Verbosity.DoNotShow );
                            
                            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                            {
                                                                   if ( entity.GetFactionTypeSafe() != FactionType.Player )
                                                                       entityToSpawnAt = entity;
                                                               }
                        }
                        
                        if ( entityToSpawnAt == null && debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "The AI Overlord is dead?\n", Verbosity.DoNotShow );
                    }
                }
                
                debugCode = 500;
                if ( entityToSpawnAt == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "BUG: entityToSpawnAt is null\n", Verbosity.DoNotShow );
                    return;
                }
                
                debugCode = 600;
                if ( targetPlanet == null )
                    targetPlanet = entityToSpawnAt.Planet;
                
                debugCode = 700;
                //don't give any text warnings to players this early.  Wait until the top notification would appear.
                //            Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SFXItemType_NonPositional.EnemyWaveArrived );
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Spawning " + wave.FinalComposition.Count + " different types of units.  Faction: " + AttachedFaction.GetDisplayName(), Verbosity.DoNotShow ); ;
                
                debugCode = 800;
                List<SafeSquadWrapper> waveEntities = GameEntity_Squad.GetTemporarySquadList( "AISent-SpawnWave-waveEntities", 10f );
                if ( waveEntities == null ) //blocked for teardown/shutdown; bail
                    return;

                bool canUseRelentlessAIWaveFaction = true;
                if ( wave.isReconquestWave || wave.isExogalacticWormholeWave )
                    canUseRelentlessAIWaveFaction = false;

                WavesHelper.Instance.DeployComposition( Context, AttachedFaction, entityToSpawnAt, TargetFactionIndex, wave.FinalComposition, waveEntities, ArcenPoint.ZeroZeroPoint, null, canUseRelentlessAIWaveFaction, debug );
                
                debugCode = 900;
                if ( !wave.spawnWaveDirectlyOnTarget )
                {
                    debugCode = 1000;
                    //wave is on warpGatePlanet right now and must be ordered to targetPlanet
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Giving units orders to travel to " + targetPlanet.Name + "\n", Verbosity.DoNotShow );

                    var pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "SentinelsSpawnWave", 
                            entityToSpawnAt.Planet, targetPlanet, PathingMode.Default, Context, PathCacheData );
                    
                    if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                    {
                        for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                        {
                            // Don't let the targeting behavior be overridden by the individual
                            // ship logic. The Relentless AI faction long-range planning code
                            // will override it if the target planet is no longer worth targeting.
                            bool shouldOverrideBehavior = true;
                            for ( int m = 0; m < waveEntities.Count; m++ )
                            {
                                GameEntity_Squad entity = waveEntities[m].GetSquad();
                                if ( entity == null )
                                    continue;
                                
                                EntityOrder newOrder = EntityOrder.Create_Wormhole( pathCache.PathToReadOnly[k].Index, shouldOverrideBehavior, true, OrderSource.Other, false );
                                if ( newOrder.TypeData == null )
                                    continue;
                                
                                if ( wave.TargetFactionIndex > 0 )
                                {
                                    Faction targetFaction = World_AIW2.Instance.GetFactionByIndex( wave.TargetFactionIndex );
                                    if ( targetFaction.Type != FactionType.Player &&
                                         targetFaction.BaseInfo.Allegiance != "Allied To Players" )
                                    {

                                        waveEntities[m].Orders.BehaviorRelatedFactionIndex = wave.TargetFactionIndex;
                                    }
                                }
                                
                                entity.Orders.QueueOrder( entity, newOrder );
                            }
                        }
                    }
                }

                GameEntity_Squad.ReleaseTemporarySquadList( waveEntities );
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "SpawnWave: debugCode " + debugCode + " exception " + e.ToString() + " wave was <>", Verbosity.DoNotShow );
            }
        }

        private static readonly DrawBag <GameEntityTypeData> ExtragalacticBag = DrawBag<GameEntityTypeData>.Create_WillNeverBeGCed( 200, "AISentinelsFactionDeepInfo-ExtragalacticBag" );

        public void ReactToPowerLevel_HostOnly( ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData )
        {
            int debugCode = 0;
            if ( World_AIW2.Instance.IsOutsideOfNormalGameplay )
                return;
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.BudgetSpend );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AISent-ReactToPowerLevel_HostOnly-trace", 10f ) : null;

            try
            {
                debugCode = 100;
                UpdateExtragalacticBudgets();
                ProtectedList<ExtragalacticBudget> budgets = this.BaseInfo.SentinelInfo.ExtragalacticBudgets;
                if ( tracing && budgets.Count > 0 )
                    tracingBuffer.Add("ReactToPowerLevel_HostOnly for " + this.AttachedFaction.GetDisplayName() ).Add("\n");
                for ( int i = 0; i < budgets.Count; i++ )
                {
                    debugCode = 200;
                    ExtragalacticBudget budget = budgets[i];
                    if ( budget.PowerLevel < FInt.One )
                        continue;
                    if ( AttachedFaction.FactionIsDefeated )
                    {
                        if ( tracingBuffer != null )
                        {
                            tracingBuffer.ReturnToPool();
                            tracingBuffer = null;
                        }

                        return; //if the faction is defeated, no more extragalactic war units
                    }

                    if ( budget.Target.AgainstFactionAllegiance == "对玩家友好" )
                    {
                        Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        if ( localFaction != null && ArcenNetworkAuthority.GetIsHostMode() )
                            World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Extragalactic_War_Start", string.Empty, localFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    }
                    if ( budget.NextExtragalacticUnitToBuy == null )
                    {
                        debugCode = 400;
                        //select next unit to buy. Note that we choose and then save for it, since
                        //we don't always want to pick the cheapest one
                        if (tracing)
                            tracingBuffer.Add("Picking new exogalactic unit for budget...\n");
                        FactionUtilityMethods.Instance.CalculateAvailableExogalacticUnits( budget.PowerLevel, ExtragalacticBag );
                        if (tracing)
                            tracingBuffer.Add("Available:\n   ").Add( ExtragalacticBag.ToString() );
                        if (tracing)
                            tracingBuffer.Add("RandomSeed: " + Context.RandomToUse.GetCurrentSeed() + "\n");
                        if ( !ExtragalacticBag.GetHasItems() )
                            throw new Exception( "Found no items for extragalactic war. threat level " + budget.PowerLevel );
                        budget.NextExtragalacticUnitToBuy = ExtragalacticBag.PickRandomItemAndDoNotReplace( Context.RandomToUse );
                    }
                    debugCode = 500;
                    budget.Budget += GetExtragalacticIncome( budget.PowerLevel );
                    if ( tracing )
                    {
                        tracingBuffer.Add( "\tEnemy:\t ");
                        if ( budget.Target.AgainstFactionAllegiance != "" )
                            tracingBuffer.Add( " alliance '" + budget.Target.AgainstFactionAllegiance + "'" );
                        if ( budget.Target.AgainstFaction != null )
                            tracingBuffer.Add( " faction '" + budget.Target.AgainstFaction.SpecialFactionData.ShortName + " idx " + budget.Target.AgainstFaction.FactionIndex + "'"  );

                        tracingBuffer.Add(", Threat Level " + budget.PowerLevel.ReadableString + ", " + budget.NextExtragalacticUnitToBuy.GetDisplayName() + " budget " + budget.Budget + "/" + budget.NextExtragalacticUnitToBuy.CostForAIToPurchase );
                        if ( i != budgets.Count - 1 )
                            tracingBuffer.Add("\n");
                    }
                    debugCode = 600;
                    if ( budget.Budget > budget.NextExtragalacticUnitToBuy.CostForAIToPurchase )
                    {
                        debugCode = 700;
                        GameEntity_Squad king = FactionUtilityMethods.Instance.findKing( AttachedFaction );
                        debugCode = 710;
                        Faction spawningFaction = this.BaseInfo.SubFac_Hunter;
                        debugCode = 720;
                        if ( spawningFaction == null )
                        {
                            if ( !World_AIW2.Instance.GetIsTutorial() )
                                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: could not find hunter faction for " + AttachedFaction.GetDisplayName(), Verbosity.DoNotShow );
                            spawningFaction = king.GetFactionOrNull_Safe();
                        }
                        debugCode = 730;
                        if ( king == null )
                        {
                            //Chris notes: very unlikely to actually be a bug.  In multi-AI games, one has been killed off.  Or you have killed one AI and are in the post-game.
                            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                            if ( tracingBuffer != null )
                            {
                                tracingBuffer.ReturnToPool();
                                tracingBuffer = null;
                            }

                            return;
                        }
                        //So we have a problem where Exo war units spawn at minor factions they can't actually get to. So lets make sure that
                        //we spawn our exogalactic war units on a reasonable planet (or directly on target)
                        //Note that this is a performance hit, but it only happens when an Exo unit spawns, which is rare
                        debugCode = 740;
                        List<Planet> targetPlanets = Planet.GetTemporaryPlanetList( "AISent-ReactToPowerLevel_HostOnly-targetPlanets", 10f );
                        if ( targetPlanets == null ) //blocked for teardown/shutdown; bail
                            return;

                        budget.GetPlanetsForExtragalacticBudget( targetPlanets, AttachedFaction );
                        if ( targetPlanets.Count == 0 )
                        {
                            Planet.ReleaseTemporaryPlanetList( targetPlanets );
                            #region Tracing
                            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                            if ( tracingBuffer != null )
                            {
                                tracingBuffer.ReturnToPool();
                                tracingBuffer = null;
                            }
                            #endregion
                            return; //Chris says: if this happens, it's not some crisis; the minor faction may just now own any planets.
                            //throw new Exception("Could not find any planets for budget " + budget.ToString() );
                        }
                        debugCode = 741;
                        Planet spawnPlanet = king.Planet;
                        debugCode = 742;
                        ArcenPoint spawnPoint = king.WorldLocation;
                        debugCode = 750;
                        if ( budget.Target.AgainstFactionAllegiance != "对玩家友好" )
                        {
                            debugCode = 755;
                            //if this is not against a player, lets see if we can get to any of the planets we'd like to
                            //from our default spot (the King)
                            bool foundSafePath = false;
                            for ( int j = 0; j < targetPlanets.Count; j++ )
                            {
                                foundSafePath = HasSafeExoPath( spawnPlanet, targetPlanets[j], Context, PathCacheData );
                                if ( foundSafePath )
                                    break;
                            }
                            debugCode = 756;
                            if ( !foundSafePath )
                            {
                                debugCode = 757;
                                //We couldn't get to our target from the King planet; lets try any warp gate
                                foreach ( GameEntity_Squad warpGate in AttachedFaction.Squads( EntityRollupType.WarpEntryPoints ) )
                                {
                                    debugCode = 758;
                                    //just find the first one that works
                                    for ( int j = 0; j < targetPlanets.Count; j++ )
                                    {
                                        debugCode = 759;
                                        foundSafePath = HasSafeExoPath( warpGate.Planet, targetPlanets[j], Context, PathCacheData );
                                        if ( foundSafePath )
                                        {
                                            spawnPlanet = warpGate.Planet;
                                            spawnPoint = warpGate.WorldLocation;
                                            break;
                                        }
                                    }
                                }
                            }
                            debugCode = 770;
                            if ( !foundSafePath )
                            {
                                debugCode = 775;
                                //we found no safe path from any warp gate, so just pick a random planet and spawn
                                spawnPlanet = targetPlanets[Context.RandomToUse.Next(0, targetPlanets.Count)];
                                debugCode = 776;
                                spawnPoint = spawnPlanet.GetSafePlacementPointAroundPlanetCenter(Context, budget.NextExtragalacticUnitToBuy, FInt.FromParts( 0, 600 ), FInt.FromParts( 0, 900 ) );
                            }
                        }
                        debugCode = 800;
                        PlanetFaction pFaction = spawnPlanet.GetPlanetFactionForFaction( spawningFaction );
                        debugCode = 810;
                        GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, budget.NextExtragalacticUnitToBuy,
                                                                           king.CurrentMarkLevel,
                                                                           pFaction.FleetUsedAtPlanet, 0,
                                                                           spawnPoint, Context, "AISentinelsReactToPowerLevel" );
                        debugCode = 900;

                        entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
                        if ( budget.Target.IsActive() && budget.Target.AgainstFactionAllegiance != "对玩家友好" )
                        {
                            //the budget can be inactive if this is an old save game. Stuff "against the player" can be used against anyone, like threat against -1
                            if ( entity.FireteamSpecificationOrNull == null ) //Chris notes: setup happens when it's pulled from the pool if this is null
                                entity.FireteamSpecificationOrNull = FireteamRequiredTarget.GetFromPoolOrCreate();
                            entity.FireteamSpecificationOrNull.CopyFrom( budget.Target );
                        }
                        if ( tracing )
                            tracingBuffer.Add("\t\tCreating a new " + entity.ToStringWithPlanetAndOwner() + " with Fireteam Target <" +  entity.FireteamSpecificationOrNull?.ToString() ?? "none" ).Add(">. Active? ").Add( budget.Target.IsActive() );

                        budget.NextExtragalacticUnitToBuy = null; //we will pick a new one next time
                        budget.Budget = FInt.Zero;
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            string name = entity.TypeData.GetDisplayName();

                            workingBuffer.Add( "AI ", AttachedFaction.FactionCenterColor.ColorHexBrighter ).Add( " is spawning ");
                            if ( ArcenStrings.DoesStringStartWithVowel(name) )
                                workingBuffer.Add(" an " );
                            else
                                workingBuffer.Add(" a " );

                            workingBuffer.Add( name, "dfaa32" );
                            if ( budget.Target.IsActive() && budget.Target.AgainstFactionAllegiance != "对玩家友好" )
                            {
                                workingBuffer.Add( " " );
                                budget.Target.ToDisplayString( workingBuffer );
                            }
                            else
                            {
                                Faction playerfaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                                workingBuffer.Add( " against " );
                                if ( playerfaction != null )
                                    workingBuffer.Add( "Humanity", playerfaction.FactionCenterColor.ColorHexBrighter );
                                else
                                    workingBuffer.Add( "Humanity" );
                                workingBuffer.Add( " and their allies" );
                            }
                            workingBuffer.Add( ". " );

                            SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( entity );

                            World_AIW2.Instance.QueueChatMessageOrCommand( workingBuffer.GetStringAndResetForNextUpdate(), ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                        }

                        Planet.ReleaseTemporaryPlanetList( targetPlanets );
                    }
                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in ReactToPowerLevel_HostOnly debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracingBuffer != null )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        private bool HasSafeExoPath( Planet Start, Planet End, ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData )
        {
            if ( Start == End )
                return true; //consider it safe it we are already there

            //returns true if there's a reasonably path for Exogalactic War Units
            PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "SentinelsHasSafeExoPath", Start, End, PathingMode.Default, Context, PathCacheData );
            if ( pathCache == null || pathCache.PathToReadOnly.Count == 0 )
                return false;
            // TODO: Should we consider if the spawn planet is safe?
            Faction targetFaction = End.GetControllingOrInfluencingFaction();
            for ( int j = 0; j < pathCache.PathToReadOnly.Count; j++ )
            {
                var pFaction = pathCache.PathToReadOnly[j].GetStanceDataForFaction( AttachedFaction );
                if ( pFaction[FactionStance.Hostile].TotalStrength > pFaction[FactionStance.Friendly].TotalStrength + pFaction[FactionStance.Self].TotalStrength )
                {
                    //there are lots of enemies here
                    bool targetOnPlanet = false;
                    if ( targetFaction != null )
                    {
                        var targetPFaction = pathCache.PathToReadOnly[j].GetStanceDataForFaction( targetFaction );
                        if ( targetPFaction != null &&
                             (targetPFaction[FactionStance.Self].TotalStrength > 1000 ) )
                        {
                            //The faction we want to kill has forces here, so this isn't some unrelated enemy faction
                            targetOnPlanet = true;
                        }
                    }
                    if ( !targetOnPlanet )
                        return false;
                }
            }
            return true;
        }
        private FInt GetExtragalacticIncome(FInt enemyPower )
        {
            AIDifficulty difficulty = this.BaseInfo.SentinelInfo.AIDifficulty;
            FInt income = difficulty.ExtragalacticWarIncomeByTier[0]; //default, tier 0
            if ( enemyPower >= 5 )
                income = difficulty.ExtragalacticWarIncomeByTier[4];
            else if ( enemyPower  >= 4 )
                income = difficulty.ExtragalacticWarIncomeByTier[3];
            else if ( enemyPower >= 3 )
                income = difficulty.ExtragalacticWarIncomeByTier[2];
            else if ( enemyPower >= 2 )
                income = difficulty.ExtragalacticWarIncomeByTier[1];
            FInt aipComponent = (FactionUtilityMethods.Instance.GetCurrentAIP() / 10) * difficulty.ExtragalacticIncomePer10AIP;
//            ArcenDebugging.ArcenDebugLogSingleLine("base income " + income.ReadableString + " aip " + aipComponent.ReadableString, Verbosity.DoNotShow );
            income += aipComponent;
            
            if ( World_AIW2.Instance.AIFactions.Count > 1 && !AttachedFaction.InCivilWarMode )
                income /= 2; //halve all extragalactic income if there are multiple AIs

            return income;
        }

        public void HandleWormholeBorers_MainSim( ref bool somethingToLog, ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.BudgetSpend );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AISent-HandleWormholeBorers_MainSim-trace", 10f ) : null;
            int debugCode = 0;
            try {
            AIDifficulty difficulty = this.BaseInfo.SentinelInfo.AIDifficulty;
            bool foundSomethingToLog = false; //i can't use a ref in an anonymous method
            debugCode = 100;
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( EntityRollupType.WormholeBorer ) )
            {
                debugCode = 200;
                if ( entity.TypeData.GetHasTag("MobileWormholeBorer") )
                {
                    debugCode = 300;
                    Planet startBoringPlanet = World_AIW2.Instance.GetPlanetByIndex(entity.WBStartPlanet);
                    if ( entity.Planet == startBoringPlanet && (World_AIW2.Instance.GameSecond - entity.GameSecondEnteredThisPlanet) > 10)
                    {
                        debugCode = 400;
                        foundSomethingToLog = true;
                        if ( tracing )
                            tracingBuffer.Add("Transforming a mobile wormhole borer on " + entity.GetPlanetName_Safe() );

                        //we're at our destination, transform!
                        GameEntityTypeData TypeData = GameEntityTypeDataTable.Instance.GetRowByName( "WormholeBorer" );
                        PlanetFaction pFaction = entity.Planet.GetPlanetFactionForFaction( AttachedFaction );

                        GameEntity_Squad newEntityOrNull = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, TypeData, AttachedFaction.CurrentGeneralMarkLevel,
                                                                                 pFaction.Faction.LooseFleet, 0, entity.WorldLocation, Context, "AISentinelsWHBorerTransform" );
                        if ( newEntityOrNull != null )
                        {
                            newEntityOrNull.WBStartPlanet = entity.WBStartPlanet;
                            newEntityOrNull.WBDestinationPlanet = entity.WBDestinationPlanet;
                        }
                        entity.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                    }
                    continue;
                }
                debugCode = 500;
                int timeSinceCreated = World_AIW2.Instance.GameSecond - entity.GameSecondCreated;
                if ( entity.TypeData.GetHasTag("WormholeBorer") && timeSinceCreated >= difficulty.WormholeBorerCompletionTime &&
                     ArcenNetworkAuthority.GetIsHostMode() ) //only the host creates the new wormhole
                {
                    //create the wormhole
                    debugCode = 600;
                    Planet destinationPlanet = World_AIW2.Instance.GetPlanetByIndex(entity.WBDestinationPlanet);
                    GameCommand createCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.LinkPlanets], GameCommandSource.AnythingElse );
                    createCommand.RelatedIntegers.Add(entity.WBStartPlanet);
                    createCommand.RelatedIntegers.Add(entity.WBDestinationPlanet);
                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, createCommand, false );
                    entity.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                    if ( ArcenNetworkAuthority.GetIsHostMode() )
                    {
                        workingBuffer.Add( "The Wormhole Borer on " ).Add( entity.GetPlanetName_Safe(), entity.Planet.GetControllingOrInfluencingFaction().FactionCenterColor.ColorHexBrighter )
                        .Add( " has just created a new wormhole to " ).Add( destinationPlanet.Name, destinationPlanet.GetControllingOrInfluencingFaction().FactionCenterColor.ColorHexBrighter );

                        PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                        if ( chatHandlerOrNull != null )
                            chatHandlerOrNull.PlanetToView = entity.Planet;

                        World_AIW2.Instance.QueueChatMessageOrCommand( workingBuffer.GetStringAndResetForNextUpdate(), ChatType.LogToCentralChat, chatHandlerOrNull );
                    }
                }
            }
            //Since it's possible for the AIP to be reduced after the borer was created,
            //we process the borers before looking at the AIP
            if ( GlobalAIWorldBaseInfo.Instance.AIProgress_Effective < difficulty.AIPUnlockWormholeBorer ||
                 difficulty.AIPUnlockWormholeBorer == -1 )
                return;
            if ( difficulty.WormholeBorerIncomePerMinute > FInt.Zero )
            {
                if ( World_AIW2.Instance.GameSecond % 60 == 0 )
                {
                    this.BaseInfo.SentinelInfo.WormholeBorerBudget += difficulty.WormholeBorerIncomePerMinute * this.BaseInfo.AttachedFaction.CustomData_WormholeBorerIncomeModifier( true );
                }
            }
            debugCode = 1000;
            int checkInterval = 10;
            GameEntityTypeData borerData = GameEntityTypeDataTable.Instance.GetRowByName( "MobileWormholeBorer" );
            if  ( this.BaseInfo.SentinelInfo.WormholeBorerBudget >= borerData.CostForAIToPurchase &&
                  World_AIW2.Instance.GameSecond % checkInterval == 0 )
            {
                debugCode = 1100;
                bool foundOtherWormholeBorer = false;
                for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
                {
                    debugCode = 1200;
                    Faction otherFaction = World_AIW2.Instance.AIFactions[i];
                    foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.WormholeBorer ) )
                    {
                        foundOtherWormholeBorer = true;
                        break;
                    }
                    if ( foundOtherWormholeBorer )
                        break;
                }
                debugCode = 1300;
                if ( foundOtherWormholeBorer )
                {
                    debugCode = 1400;
                    //there's another wormhole borer active, so just push this one back a bit. Having multiple borers active simultaneously is probably a bit much
                    this.BaseInfo.SentinelInfo.WormholeBorerBudget -= this.BaseInfo.SentinelInfo.WormholeBorerBudget/8;
                    return;
                }
                        
                somethingToLog = true;
                if ( tracing )
                    tracingBuffer.Add("Considering building a wormhole borer; current budget " + this.BaseInfo.SentinelInfo.WormholeBorerBudget + " and cost is " + borerData.CostForAIToPurchase ).Add("\n");
                //don't bother doing this too often; it's pretty expensive, and if we didn't find a good option last time then
                //it will probably take a bit before a good option opens up
                debugCode = 1500;
                GameEntity_Squad king = FactionUtilityMethods.Instance.findKing(AttachedFaction);
                if ( king == null )
                {
                    return; //somehow this AI doesn't have a king anymore? unclear how this can happen 
                }
                    
                Dictionary<Planet, Planet> borerTargets = Planet.GetTemporaryPlanetDictOfPlanets( "AISent-HandleWormholeBorers_MainSim-borerTargets", 10f );
                if ( borerTargets == null ) //blocked for teardown/shutdown; bail
                    return;
                debugCode = 1600;
                GetWormholeBorerTargets( borerTargets, king.Planet, Context);
                if ( borerTargets.Count == 0 && this.BaseInfo.SentinelInfo.WormholeBorerBudget >= borerData.CostForAIToPurchase * 2 )
                {
                    debugCode = 1700;
                    //We're just accumulating metal here; donate it away to the hunter fleet
                    Faction hunter = this.BaseInfo.SubFac_Hunter;
                    if ( hunter != null )
                    {
                        AIHunterCoreData hunterExternal = this.BaseInfo.HunterInfo;
                        FInt amountToDonate = this.BaseInfo.SentinelInfo.WormholeBorerBudget - borerData.CostForAIToPurchase;
                        hunterExternal.ReceiveDonation( amountToDonate, AttachedFaction, null );
                        this.BaseInfo.SentinelInfo.WormholeBorerBudget -= amountToDonate;
                    }
                }
                debugCode = 1800;
                if ( borerTargets.Count == 0 )
                {
                    Planet.ReleaseTemporaryPlanetDictOfPlanets( borerTargets );
                    //didn't find any useful places to build wormholes
                    return;
                }
                debugCode = 1900;
                GameEntity_Squad newEntityOrNull = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( king.PlanetFaction, borerData, AttachedFaction.CurrentGeneralMarkLevel,
                                                                         king.GetFactionLooseFleetOrNull_Safe(), 0, king.WorldLocation, Context, "AISentinelsWHBorer" );
                KeyValuePair <Planet, Planet> choice = new KeyValuePair<Planet, Planet>( null, null );
                foreach ( KeyValuePair<Planet, Planet> pair in borerTargets )
                {
//                    ArcenDebugging.ArcenDebugLogSingleLine("Wormhole option: " + pair.Key.Name + " --> " + pair.Value.Name, Verbosity.DoNotShow );
                    choice = pair;
                }

                Planet.ReleaseTemporaryPlanetDictOfPlanets( borerTargets );

                if ( newEntityOrNull != null )
                {
                    Planet startPlanet = choice.Key;
                    Planet destinationPlanet = choice.Value;
                    newEntityOrNull.WBStartPlanet = startPlanet.Index;
                    newEntityOrNull.WBDestinationPlanet = destinationPlanet.Index;
                }
                this.BaseInfo.SentinelInfo.WormholeBorerBudget -= borerData.CostForAIToPurchase;
            }

            if ( foundSomethingToLog )
                somethingToLog = foundSomethingToLog; 
            } // end try
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.LogSingleLine("Hit exception in HandleWormholeBorers_MainSim debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            // ...because this method can return before the bottom of this method
            finally
            {
                #region Tracing
                if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracingBuffer != null )
                {
                    tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                }
                #endregion
            }
        }

        private static readonly List<Planet> BorerStartPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "AISentinelsFactionDeepInfo-BorerStartPlanets" );

        private void GetWormholeBorerTargets( Dictionary<Planet, Planet> DictToFill, Planet spawnPlanet, ArcenHostOnlySimContext Context )
        {
            DictToFill.Clear();

            int debugCode = 0;
            try
            {
                debugCode = 100;
                GameEntity_Squad king = FactionUtilityMethods.Instance.findKing( AttachedFaction );
                bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.BudgetSpend );
                ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AISent-GetWormholeBorerTargets-trace", 10f ) : null;

                bool updateBorerStartPlanets = true;
                int planetsConnectedToKing = GetAlliedAIPlanetsConnectedToThis( king.Planet, updateBorerStartPlanets, Context );
                if ( tracing )
                    tracingBuffer.Add( "Getting Borer Targets: planets connected to king planet " + king.GetPlanetName_Safe() + " " + planetsConnectedToKing + ". We have " + BorerStartPlanets.Count + " planets to work with" ).Add( "\n" );
                debugCode = 200;
                //BorerStartPlanets are all the planets we can reach safely from the AI homeworld
                //We have a couple places we want to evaluate.
                //First, if we have relatively few planets (compared to galaxy size) then the player has probably cut off the AI homeworld,
                //so we want to make a path to more AI planets.
                //We need a "find reasonably linkable planets from this planet" function
                //which basically says "Could we put a link here w/o overlapping/going through other planets"
                //Then we need the ability to evaluate those planets, to say things like "Is this behind player front lines?"

                //Here are the types of targets. "Assault": tasty, poorly defended human planet
                //Second, a "reconnection" list, where we connect our chunk of the galaxy with another AI-controlled chunk of the galaxy.
                //Third, a "path to target" list, where the targets might be well defended, but they have a target of great worth (MDC?) that we don't already have a path to <=== TODO
                //Fourth, a "connect our chunk of the galaxy a bit better" <=== TODO
                Planet bestReconnectionPlanetSource = null;
                Planet bestReconnectionPlanetDest = null;
                int bestReconnectionCount = -1;
                int bestDistance = -1;
                for ( int i = 0; i < BorerStartPlanets.Count; i++ )
                {
                    debugCode = 300;
                    Planet planet = BorerStartPlanets[i];
                    if ( BorerStartPlanets.Count > 1 && planet == king.Planet )
                        continue; //don't link from the king unless you have to

                    //Nota Bene: the reconnection planet is the "start planet" (ie where the borer starts)
                    //and the assault planet is the other end of the wormhole
                    int planetsConnectedToThis = 0;
                    Planet reconnectionPlanet = GetWBReconnectionPlanet( planet, planetsConnectedToKing, ref planetsConnectedToThis, Context );
                    Planet assaultPlanet = GetWBAssaultPlanet( planet, reconnectionPlanet, Context );
                    if ( tracing )
                    {
                        tracingBuffer.Add( "\tGetting Borer Targets for " ).Add( planet.Name ).Add( ":  " );
                        if ( assaultPlanet == null )
                            tracingBuffer.Add( " assault planet: null" );
                        else
                            tracingBuffer.Add( " assault planet: " ).Add( assaultPlanet.Name );
                        if ( reconnectionPlanet == null )
                            tracingBuffer.Add( " reconnection planet:  null" );
                        else
                            tracingBuffer.Add( " reconnection planet: " + reconnectionPlanet.Name ).Add( ", connected " ).Add( planetsConnectedToThis );
                        tracingBuffer.Add( "\n" );
                    }
                    debugCode = 400;
                    if ( assaultPlanet != null )
                        DictToFill.AddPair( planet, assaultPlanet );

                    if ( reconnectionPlanet == null )
                        continue;
                    int distance = Mat.DistanceBetweenPointsImprecise( planet.GalaxyLocation, reconnectionPlanet.GalaxyLocation );

                    if ( planetsConnectedToThis > bestReconnectionCount ||
                         (planetsConnectedToThis == bestReconnectionCount &&
                          bestDistance > distance) )
                    {
                        debugCode = 500;
                        bestReconnectionPlanetSource = planet;
                        bestReconnectionPlanetDest = reconnectionPlanet;
                        bestReconnectionCount = planetsConnectedToThis;
                        bestDistance = distance;
                        if ( tracing )
                            tracingBuffer.Add( "\t\tFound best reconnection target: " ).Add( bestReconnectionPlanetDest.Name ).Add( " count " ).Add( bestReconnectionCount ).Add( "\n" );
                    }
                    debugCode = 600;

                }
                debugCode = 1000;
                if ( bestReconnectionPlanetDest != null )
                {
                    debugCode = 1100;
                    if ( bestReconnectionCount > planetsConnectedToKing )
                    {
                        debugCode = 1200;
                        if ( tracing )
                            tracingBuffer.Add( "\tClearing other targets; reconnect only" );

                        DictToFill.Clear();
                    }
                    if ( tracing )
                        tracingBuffer.Add( "\tAdding reconnection option " ).Add( bestReconnectionPlanetSource.Name ).Add( " --> " ).Add( bestReconnectionPlanetDest.Name );
                    debugCode = 1300;
                    DictToFill.AddPair( bestReconnectionPlanetSource, bestReconnectionPlanetDest );
                }
                else if ( tracing )
                {
                    tracingBuffer.Add( "\tNo reconnection option found\n" );
                }

                if ( tracing )
                {
                    tracingBuffer.Add( "\tFound " ).Add( DictToFill.Count ).Add( " borer options" );
                }
                #region Tracing
                if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracingBuffer != null )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in GetWormholeBorerTargets code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        private Planet GetWBAssaultPlanet(Planet planet, Planet sourcePlanet, ArcenHostOnlySimContext Context)
        {
            byte difficulty = this.BaseInfo.SentinelInfo.AIDifficulty.Difficulty;
            List<Planet> PotentialWBPlanets = Planet.GetTemporaryPlanetList( "AISent-GetWBAssaultPlanet-PotentialWBPlanets", 10f );
            if ( PotentialWBPlanets == null ) //blocked for teardown/shutdown; bail
                return null;

            foreach ( Planet target in World_AIW2.Instance.CurrentGalaxy.Planets( false ) )
            {
                //some of this logic is borrowed from the wormhole invasion logic
                if ( target.GetControllingOrInfluencingFaction().Type != FactionType.Player )
                    continue;
                if ( target == planet || target.GetHopsTo( planet ) <= 1 || target.GetIsDirectlyLinkedTo( false, planet ) )
                    continue;
                if ( target == sourcePlanet || target.GetHopsTo( sourcePlanet ) <= 1 || target.GetIsDirectlyLinkedTo( false, sourcePlanet ) )
                    continue;
                //its a player faction
                if ( FactionUtilityMethods.Instance.HasPlayerKing( planet ) )
                    continue;
                bool adjacentToPlayerKing = false;
                bool adjacentToNonPlayerPlanet = false;
                foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                {
                    if ( neighbor == null )
                        continue;
                    if ( neighbor.GetControllingFactionType() != FactionType.Player )
                        adjacentToNonPlayerPlanet = true;
                    if ( FactionUtilityMethods.Instance.HasPlayerKing( neighbor ) )
                        adjacentToPlayerKing = true;
                }
                if ( difficulty < 9 && adjacentToPlayerKing )
                    continue;
                if ( adjacentToNonPlayerPlanet )
                    continue;
                var pFaction = planet.GetStanceDataForFaction( AttachedFaction );

                int defensiveStrength = pFaction[FactionStance.Hostile].TotalStrength;
                if ( defensiveStrength > 10000 ) //TODO: maybe adjust this
                    continue;
                //this check is probably more expensive, so save to toward the end
                if ( WormholeInvasionManager.WouldLinkCrossOtherPlanets( planet, target ) )
                    continue;
                if ( planet.TypeData.Type == PlanetType.Nomad &&
                        !planet.IsDisabledNomad ) //no nomads (except disabled ones)
                    continue;
                PotentialWBPlanets.Add(planet);
            }
            if ( PotentialWBPlanets.Count == 0 )
            {
                Planet.ReleaseTemporaryPlanetList( PotentialWBPlanets );
                return null;
            }
            cb_aisWBSortPlanet = planet;
            PotentialWBPlanets.Sort( static delegate ( Planet Left, Planet Right )
            {
                int rDistance = Mat.DistanceBetweenPointsImprecise( Left.GalaxyLocation, cb_aisWBSortPlanet.GalaxyLocation );
                int lDistance = Mat.DistanceBetweenPointsImprecise( Right.GalaxyLocation, cb_aisWBSortPlanet.GalaxyLocation );
                return lDistance.CompareTo( rDistance );
            } );
            Planet ret = PotentialWBPlanets[0];
            Planet.ReleaseTemporaryPlanetList( PotentialWBPlanets );
            return ret;
        }
        private Planet GetWBReconnectionPlanet(Planet planet, int planetsConnectedToKing, ref int planetsConnectedToThis, ArcenHostOnlySimContext Context)
        {
            List<Planet> PotentialWBPlanets = Planet.GetTemporaryPlanetList( "AISent-GetWBAssaultPlanet-PotentialWBPlanets", 10f );
            if ( PotentialWBPlanets == null ) //blocked for teardown/shutdown; bail
                return null;
            Dictionary<Planet, int> connectedPlanetCount = Planet.GetTemporaryPlanetDictOfInts( "AIsent-GetWBReconnectionPlanet-connectedPlanetCount", 10f );
            if ( connectedPlanetCount == null ) //blocked for teardown/shutdown; bail
            {
                Planet.ReleaseTemporaryPlanetList( PotentialWBPlanets );
                return null;
            }

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.BudgetSpend );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AISent-GetWBReconnectionPlanet-trace", 10f ) : null;
            if ( tracing )
                tracingBuffer.Add("\t\tChecking reconnection planets for " + planet.Name +"\n");
            foreach ( Planet target in World_AIW2.Instance.CurrentGalaxy.Planets( false ) )
            {
                //some of this logic is borrowed from the wormhole invasion logic
                if ( target.GetControllingOrInfluencingFaction().GetIsHostileTowards(AttachedFaction) )
                    continue;
                if ( target == planet || target.GetIsDirectlyLinkedTo( false, planet ) )
                    continue;

                if ( BorerStartPlanets.Contains(target) )
                {
                    if ( tracing )
                        tracingBuffer.Add("\t\t\tskipping " + target.Name + " its a start planet\n");

                    continue;
                }
                if ( WormholeInvasionManager.WouldLinkCrossOtherPlanets( planet, target ) )
                {
                    if ( tracing )
                        tracingBuffer.Add("\t\t\tskipping " + target.Name + " link cross\n");

                    continue;
                }
                if ( planet.TypeData.Type == PlanetType.Nomad )
                    continue;

                bool updateBorerStartPlanets = true;
                int numConnectedPlanets = GetAlliedAIPlanetsConnectedToThis(target, !updateBorerStartPlanets, Context);
                if ( tracing )
                    tracingBuffer.Add("\t\t\tconnected planets to " + target.Name + " is " + numConnectedPlanets + " king connected planets: " + planetsConnectedToKing + "\n");

                if ( numConnectedPlanets > planetsConnectedToKing / 2 )
                {
                    PotentialWBPlanets.Add(target);
                    connectedPlanetCount.AddPair(target, numConnectedPlanets);
                }
            }
            if ( tracing )
                tracingBuffer.Add("\t\tpotential WB planets " + PotentialWBPlanets.Count +" presort\n");
            if ( PotentialWBPlanets.Count == 0 )
            {
                planetsConnectedToThis = -1;
                Planet.ReleaseTemporaryPlanetDictOfInts( connectedPlanetCount );
                Planet.ReleaseTemporaryPlanetList( PotentialWBPlanets );
                return null;
            }
            for ( int i = 0; i < PotentialWBPlanets.Count; i++ )
            {
                if ( tracing )
                    tracingBuffer.Add("\t\t\t").Add(i).Add(" " ).Add(PotentialWBPlanets[i].Name + ", " + connectedPlanetCount[PotentialWBPlanets[i]] +"\n");
            }

            cb_aisWBSortPlanet = planet;
            cb_aisConnectedCount = connectedPlanetCount;
            PotentialWBPlanets.Sort( static delegate ( Planet Left, Planet Right )
            {
                if ( cb_aisConnectedCount[Left] == cb_aisConnectedCount[Right] )
                {
                    int lDistance = Mat.DistanceBetweenPointsImprecise( Left.GalaxyLocation, cb_aisWBSortPlanet.GalaxyLocation );
                    int rDistance = Mat.DistanceBetweenPointsImprecise( Right.GalaxyLocation, cb_aisWBSortPlanet.GalaxyLocation );

                    return lDistance.CompareTo( rDistance );
                }
                return cb_aisConnectedCount[Right].CompareTo( cb_aisConnectedCount[Left] );

            } );
            if ( tracing )
                tracingBuffer.Add("\t\tpotential WB planets " + PotentialWBPlanets.Count +" postsort\n");

            for ( int i = 0; i < PotentialWBPlanets.Count; i++ )
            {
                if ( tracing )
                    tracingBuffer.Add("\t\t\t").Add(i).Add(" " ).Add(PotentialWBPlanets[i].Name + ", " + connectedPlanetCount[PotentialWBPlanets[i]] +"\n");
            }

            planetsConnectedToThis = connectedPlanetCount[PotentialWBPlanets[0]];
            Planet.ReleaseTemporaryPlanetDictOfInts( connectedPlanetCount );
            Planet ret = PotentialWBPlanets[0];
            Planet.ReleaseTemporaryPlanetList( PotentialWBPlanets );
            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracingBuffer != null )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion

            return ret;
        }

        private int GetAlliedAIPlanetsConnectedToThis(Planet planet, bool updateBorerPlanets, ArcenHostOnlySimContext Context)
        {
            int numPlanets = 0;
            if ( updateBorerPlanets )
                BorerStartPlanets.Clear();
            foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops( -1,
                delegate ( Planet secondaryPlanet )
                {
                    if ( secondaryPlanet.GetControllingOrInfluencingFaction().GetIsHostileTowards(AttachedFaction) )
                        return PropogationEvaluation.No;
                    var pFaction = secondaryPlanet.GetStanceDataForFaction( AttachedFaction);
                    if ( pFaction[FactionStance.Hostile].TotalStrength * 2 >=
                         pFaction[FactionStance.Self].TotalStrength + pFaction[FactionStance.Friendly].TotalStrength )
                        return PropogationEvaluation.No;
                    return PropogationEvaluation.Yes;
                } ) )
            {
                Planet otherPlanet = _phd.Planet;
                //This needs to be its own function, to find large pockets of connected AI planets for the Reconnection list
                //this is where we check whether this is a suitable planet
                var pFaction = otherPlanet.GetStanceDataForFaction( AttachedFaction);
                if ( otherPlanet.GetControllingOrInfluencingFaction().GetIsHostileTowards(AttachedFaction ) )
                    continue;
                if ( pFaction[FactionStance.Hostile].TotalStrength * 2 >=
                     pFaction[FactionStance.Self].TotalStrength + pFaction[FactionStance.Friendly].TotalStrength )
                    continue;
                numPlanets++;
                if ( updateBorerPlanets )
                    BorerStartPlanets.Add(otherPlanet);
            }
            return numPlanets;
        }
        
    }
}
