using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

//define new castle types; these castle types give different flagships

/*
  Design: The Necromancer is a new player faction. It is focused on controlling ships that it summons from killing enemies.

  The Necromancer builds necropoleis. Each necropolis has a bit of sim-city and comes with its own Flagship. It works similarly to the Spire in that regard.

  The Flagship and the ships you can build have Necromancy; when ships with necromancy kill enemies, they summon Skeletons, Wights and Mummies.
  The summoned ships have Metabolization, which is the Necromancer's primary source of income.

  There's an optional setting to cause Zombies created by regular human players to instead be donated to the Necromancer. This makes the game much easier.


TODO: Do Mummies
      Review what happens if your "get variant X" values go over 100
      Give a new utility unit and let the utility be chosen
*/


namespace Arcen.AIW2.External
{
    public enum NecromancyShipType {
        None,
        Skeleton,
        Wight,
        Mummy,
    }

    public class NecromancerEmpireFactionDeepInfo : ExternalFactionDeepInfoRoot, IExternalDeepInfo_Singleton
    {
        public NecromancerEmpireFactionBaseInfo BaseInfo;

        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
        }

        protected override void Cleanup()
        {
            BaseInfo = null;

            //probably does not matter
            WorkingAvailableNames.Clear();
            AllShipsLRP.Clear();

            FlagshipsLRP.Clear();
            ShipyardsLRP.Clear();
            NecropolisesLRP.Clear();
            WorkingPlanetsLRP.Clear();
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 10; //doesn't need to run often
        
        private static readonly string[] necromancerCityNames = new string[] {
            "Enroth", "Ironfist", "Eofol", "Nighon", "Steadwick", "Dol Guldur", "Coldsoul", "Grave Raven", "Shadow Keep", "Terminus", "Sanctum", "Haunt's Wind", "Cessacioun", "Coldreign", "Armitage", "Claxtone", "Annuvin", "Muspelheim", "Utumno", "Angband", "Tol in Gauroth", "Barrow", "Shadowend", "Calarook", "Tenebris", "Blood Gulch", "Blackwood", "Drach", "Naxxremis", "Thakan'dar", "Shayol", "Ghul", "Aridhol", "Shadar Logoth", "Blight", "Goryo", "Onryo", "Ubeme", "Seirei", "Abaddon", "Gehenna", "Tophet", "Carcosa", "Celephais", "Innsmouth", "Miskatonic", "Leng", "Dunwich", "R'lyeh", "Mnar", "Severn", "Tenoka", "Xuthal", "Xotalanc", "Zamboula", "Natohk", "Xapur", "Acheron", "Gwahlur", "Nergal", "Yimsha", "Caoranach", "Draugluin", "Carcharoth", "Anfauglin"  };
        
        private static readonly List<string> WorkingAvailableNames = List<string>.Create_WillNeverBeGCed( 300, "NecromancerEmpireFactionDeepInfo-WorkingAvailableNames" );
        private static string GetAvailableCityName( GameEntity_Squad necropolis, Faction faction, ArcenHostOnlySimContext Context )
        {
            WorkingAvailableNames.Clear();
            int foundNecropoleis = 0;
            if ( World_AIW2.Instance.Setup.GetBoolBySetting("BoringNecroNames") && necropolis != null )
            {
                return necropolis.Planet.Name;
            }

            for ( int i = 0; i < necromancerCityNames.Length; i++ )
            {
                bool foundMatch = false;
                foundNecropoleis = 0;
                foreach ( GameEntity_Squad entity in faction.Squads( "NecromancerNecropolis" ) )
                {
                    foundNecropoleis++;
                    if ( entity.GetFleetName_Safe().Contains( necromancerCityNames[i] ) )
                    {
                        foundMatch = true;
                        break;
                    }
                }
                if ( foundMatch )
                    continue;
                WorkingAvailableNames.Add( necromancerCityNames[i] );
            }
            if ( WorkingAvailableNames.Count > 0 )
                return WorkingAvailableNames[Context.RandomToUse.Next( 0, WorkingAvailableNames.Count )];
            else
                return necromancerCityNames[Context.RandomToUse.Next( 0, necromancerCityNames.Length )] + " " + foundNecropoleis; //give us a unique new name
        }

        private BolsteringManager bolsteringManger = new BolsteringManager();
        private static readonly List<SafeSquadWrapper> FlagshipsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "NecromancerEmpireFactionDeepInfo-FlagshipsLRP" );
        private static readonly List<SafeSquadWrapper> ShipyardsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "NecromancerEmpireFactionDeepInfo-ShipyardsLRP" );
        private static readonly List<SafeSquadWrapper> NecropolisesLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "NecromancerEmpireFactionDeepInfo-NecropolisesLRP" );
        private static readonly List<Planet> WorkingPlanetsLRP = List<Planet>.Create_WillNeverBeGCed( 500, "NecromancerEmpireFactionDeepInfo-WorkingPlanetsLRP" );
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Necromancer );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "NecromD-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f ) : null;

            this.bolsteringManger.HandleBolstering("NecromancerFeedsFleet", this.BaseInfo.Necropoleis.GetDisplayList(), this.BaseInfo.Flagships.GetDisplayList(), Context);
            
            HandleJournalsAndTips( Context );            
            RemindAboutTipsSidebarIfNecessary( Context );
            SwapAIDefenses( Context );
            //ClearExcessShips( Context );
            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
        }
        public void HandleNecromancerAutoDefend( ArcenLongTermIntermittentPlanningContext Context )
        {
            FlagshipsLRP.Clear();
            ShipyardsLRP.Clear();
            NecropolisesLRP.Clear();
            WorkingPlanetsLRP.Clear();
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            int debugCode = 0;
            try{
                debugCode = 100;
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads() )
                {
                    if ( entity == null )
                        continue;
                    if ( entity.TypeData.GetHasTag( "NecromancerNecropolis" ) )
                        NecropolisesLRP.Add( entity );
                    if ( entity.TypeData.GetHasTag( "NecromancerFlagship" ) )
                        FlagshipsLRP.Add( entity );
                    if ( entity.TypeData.GetHasTag( "NecromancerShipyard" ) )
                        ShipyardsLRP.Add( entity );

                }
                debugCode = 1300;
                //Below is the dyson auto-defend mode
                for (int i = 0; i < FlagshipsLRP.Count; i++ )
                {
                    if ( World_AIW2.Instance.Setup.GetStringBySetting("NecromancerAutoDefend") == "Disabled" ||
                         this.AttachedFaction.UnderPlayerControl() )
                        break;

                    /* This is a very simple implementation. There's a ton of fancy things we could do, like 
                       being able to do multiple passes to find categories of battles (defensive/offensive), and prioritizing defense (or offense).
                       Another good improvement would be being able to make more informed choices that aren't "Just go to the closest battle".
                       Another good improvement would be letting fleets go to "threatened" planets (ie planets with incoming waves, or lots of hostile mobile forces nearby)

                       For that matter, there's no principled reason we couln't give a "Offensive" mode, where we could kill Instigator bases,
                       AIP reducers, or just neutering enemy planets.
                    */
                    GameEntity_Squad flagship = FlagshipsLRP[i].GetSquad();
                    if ( flagship == null )
                        continue;

                    Fleet fleet = flagship.FleetMembership.Fleet;
                    if ( fleet == null )
                        continue;
                        NecromancerMobileFleetBaseInfo fleetInfo = fleet.TryGetExternalBaseInfoAs<NecromancerMobileFleetBaseInfo>();
                    if ( fleetInfo == null )
                    {
                        //the Sim code hasn't run to set up the mobile fleet info
                        continue;
                    }


                    Planet dest = flagship.GetDestinationPlanet();
                    bool goToShipyard = false; //this doubles as "retreat"
                    debugCode = 1500;
                    int myStrength = fleet.CalculateEffectiveCurrentFleetStrength_PlayerFleetsOnly();

                    debugCode = 1400;

                    bool debug = false;

                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("Doing LRP update for " + flagship.ToStringWithPlanet(), Verbosity.DoNotShow );

                    debugCode = 1600;
                    if (AutoDefendUtility.IsPlanetUnderAttack(dest, this.AttachedFaction) && AutoDefendUtility.IsLosingBattle(flagship.Planet, myStrength,this.AttachedFaction) )
                    {
                        debugCode = 1700;
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tWe need to retreat!", Verbosity.DoNotShow );
                        goToShipyard = true; //we are retreating from a losing battle so go someplace safe
                    }
                    debugCode = 1750;
                    if ( !goToShipyard &&
                         fleetInfo.NeedsToRebuild && AutoDefendUtility.CanISafelyLeavePlanet( flagship, myStrength, this.AttachedFaction ) )
                    {
                        debugCode = 1800;
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tNeeds to rebuild", Verbosity.DoNotShow );
                        //Find a planet with a shipyard and no enemies
                        if ( !AutoDefendUtility.DoesPlanetHaveShipyard( dest, ShipyardsLRP ) )
                            goToShipyard = true;
                    }
                    debugCode = 1900;
                    if ( goToShipyard )
                    {
                        debugCode = 2000;
                        //Note: we don't technically need to go to a shipyard to rebuild, but
                        //it's safer to chill on a friendly planet
                        Planet shipyardPlanet = AutoDefendUtility.GetNearestShipyardToMe(flagship.Planet, ShipyardsLRP, this.AttachedFaction, Context, pathingCacheData );
                        if (shipyardPlanet != null)
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine("\tHeading to " + shipyardPlanet.Name + " to rebuild", Verbosity.DoNotShow );

                            AutoDefendUtility.GoToPlanet(flagship, shipyardPlanet, Context, pathingCacheData );
                            continue;
                        }
                    }
                    if ( flagship.GetIsCrippled() )
                        continue;
                    debugCode = 2100;
                    if ( AutoDefendUtility.IsPlanetUnderAttack(dest, this.AttachedFaction ) )
                    {
                        debugCode = 2200;
                        if (debug)
                        {
                            if ( dest == flagship.Planet )
                                ArcenDebugging.ArcenDebugLogSingleLine("\tWe are already in a battle!", Verbosity.DoNotShow);
                            else
                                ArcenDebugging.ArcenDebugLogSingleLine("\tWe are en route to a battle at " + dest.Name, Verbosity.DoNotShow);
                        }

                        continue;
                    }
                    debugCode = 2300;
                    //Check if we have any battles to go to!
                    WorkingPlanetsLRP.Clear();
                    AutoDefendUtility.GetPlanetsUnderAttack( flagship, myStrength, flagship.Planet, WorkingPlanetsLRP);
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\tWe have found " + WorkingPlanetsLRP.Count + " planets under attack", Verbosity.DoNotShow );
                    debugCode = 2400;
                    if ( WorkingPlanetsLRP.Count > 0 )
                    {
                        debugCode = 2500;
                        //The target's priority is chosen in GetPlanetsUnderAttack (so technically we could just pass back a single planet), but we use a List for future-proofing
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tWe are heading to " + WorkingPlanetsLRP[0].Name + " to help defend", Verbosity.DoNotShow );

                        AutoDefendUtility.GoToPlanet( flagship, WorkingPlanetsLRP[0], Context, pathingCacheData);
                        continue;
                    }
                    debugCode = 2600;
                    //We haven't found an ongoing battle to assist in, so see if we have a threatened planet we can defend
                    WorkingPlanetsLRP.Clear();
                    if ( AutoDefendUtility.IsPlanetThreatened( dest, this.AttachedFaction ) )
                    {
                        debugCode = 2700;
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tWe are already on a threatened planet!", Verbosity.DoNotShow );
                        continue;
                    }
                    AutoDefendUtility.GetThreatenedPlanets( myStrength, flagship.Planet, WorkingPlanetsLRP, this.AttachedFaction );
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\tWe have found " + WorkingPlanetsLRP.Count + " planets threatened", Verbosity.DoNotShow );
                    debugCode = 2800;
                    if ( WorkingPlanetsLRP.Count > 0 )
                    {
                        Planet threatenedPlanet = WorkingPlanetsLRP[Context.RandomToUse.Next(0, WorkingPlanetsLRP.Count)];
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tWe are heading to " + threatenedPlanet.Name + " because the planet is threatened", Verbosity.DoNotShow );
                    
                        AutoDefendUtility.GoToPlanet( flagship, threatenedPlanet, Context, pathingCacheData);
                        continue;
                    }
                    debugCode = 2900;
                    if ( dest != flagship.Planet )
                        continue; //we don't have any more urgent objectives, and we are en route someplace
                    if ( dest == flagship.Planet )
                    {
                        debugCode = 3000;
                        //Patrolling: head to a randomly chosen other shipyard
                        Planet shipyardPlanet = AutoDefendUtility.GetRandomPlanetForPatrol(flagship, NecropolisesLRP, WorkingPlanetsLRP, Context, pathingCacheData);
                        if (shipyardPlanet != null)
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine("\tWe are heading to " + shipyardPlanet.Name + " to patrol", Verbosity.DoNotShow );

                            AutoDefendUtility.GoToPlanet(flagship, shipyardPlanet, Context, pathingCacheData);
                            continue;
                        }
                    }
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\tNothing to do?", Verbosity.DoNotShow );
                }
            } catch(Exception e )
            {
                ArcenDebugging.LogSingleLine("Exception in necromancer LRP debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            pathingCacheData.ReturnToPool();
        }
        public virtual void HandleJournalsAndTips( ArcenHostOnlySimContext Context )
        {
            //Some entries are time-related
            if ( World_AIW2.Instance.GameSecond == 1 && GameSettings.Current.GetBoolBySetting( "NecromancerTipReminders" ) )
            {
                if ( World_AIW2.Instance.CampaignType.HarshnessRating >= 500 )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "亡灵法师概览",
                                                        "停止！亡灵法师是一种与人类帝国完全不同的游戏风格。你必须重新学习技能并改变你的期望。\n\n阅读！左侧菜单的'提示'标签中有大量玩法和建议。\n\n享受！由于这是一个全新的游戏体验，请将自己视为不再是AI War 2中的玩家。\n\n警告！亡灵法师在任何高于'人类至上'的游戏模式中都不被真正支持。它不应该崩溃，但可能会出现意外问题。风险自负。初步变化：长者升级更快，圣殿骑士开始时拥有更多城堡。", "确定" );
                }
                else
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "亡灵法师概览",
                                       "停止！亡灵法师是一种与人类帝国完全不同的游戏风格。你必须重新学习技能并改变你的期望。\n\n阅读！左侧菜单的'提示'标签中有大量玩法和建议。\n\n享受！由于这是一个全新的游戏体验，请将自己视为不再是AI War 2中的玩家。", "确定" );
                }
            }
            if ( World_AIW2.Instance.GameSecond > 60 )
            {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_MarkUpgrades", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Blueprints", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Amplifiers", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Defenses", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
            if ( World_AIW2.Instance.GameSecond > 80 )
            {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Skeletons", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Wights", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
            if ( World_AIW2.Instance.GameSecond > 400 )
            {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_ResourceMonitoring", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
            if ( World_AIW2.Instance.GameSecond > 360 )
            {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Towers", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
            if ( World_AIW2.Instance.GameSecond > 320 )
            {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Totems", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }

            //others are related to gameplay triggers we detect here
            if ( this.BaseInfo.Necropoleis.Count >= 3 && this.BaseInfo.NumShipyards < 2 )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_ShipyardsAreCritical", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );

            if ( World_AIW2.Instance.GameSecond > 30 &&
                World_AIW2.Instance.GameSecond % 65 == 0 )
            {
                //Check if we have unused necropolis modules, and give a log if we do
                //We could also check flagships, but I'm concerned about the player who haven't used modules at all
                bool foundUnusedModules = false;
                foreach ( GameEntity_Squad city in this.BaseInfo.Necropoleis.DisplaySquads() )
                {
                    if ( city == null )
                        continue;
                    if ( !city.TypeData.IsModular )
                        continue;
                    if ( city.FleetMembership.ForMark.ModulePointsAvailable > 0 )
                    {
                        foundUnusedModules = true;
                        break;
                    }
                }
                if ( foundUnusedModules )
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_SpendModules", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
            //TODO: checks for 'have we invested in totem tech' and 'have we invested in tower defense tech
        }
        public virtual void RemindAboutTipsSidebarIfNecessary( ArcenHostOnlySimContext Context )
        {
            //Every 5 minutes, remind the player about the Tips sidebar if they haven't looked at any Tips
            int reminderInterval = 300;
            if ( ! GameSettings.Current.GetBoolBySetting( "NecromancerTipReminders" ) )
            {
                return;
            }
            if ( World_AIW2.Instance.GameSecond % reminderInterval != 0 )
                return;
            PlayerAccount localAccount = PlayerAccount.Local;
            if ( localAccount == null )
                return;
            bool foundOpenedTip = false;
            for ( int i = World_AIW2.Instance.JournalHistory.Count - 1; i >= 0; i-- )
            {
                JournalEntryInCampaign entry = World_AIW2.Instance.JournalHistory[i];
                if ( !entry.IsTipRatherThanJournalEntry )
                    continue;
                if ( entry.HasBeenViewedByPlayerAccountIDs.ContainsKey ( localAccount.PlayerPrimaryKeyID ) )
                {
                    foundOpenedTip = true;
                    break;
                }
            }
            if ( !foundOpenedTip )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "陛下，'提示'侧边栏中有新的治国建议。",
                                                               ChatType.LogToCentralChat, "", null );
            }
        }

        public override void DoOnAnyCrippleLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        {
            FInt phylacteryMoveCost = FInt.FromParts(10, 000);
            if ( entity.TypeData.GetHasTag( "DestructiblePhylactery") )
            {
                //for the necromancer phylactery move
                GameEntity_Squad newNecropolis = BaseInfo.GetRandomNecropolisForSwapOrNull( Context );

                if ( newNecropolis == null || entity.PlanetFaction.Faction.StoredFactionResourceOne < phylacteryMoveCost )
                {
                    //we can't move the phylactery
                    return;
                }
                BaseInfo.SwapNecropoleis( entity, newNecropolis, Context );
                entity.PlanetFaction.Faction.StoredFactionResourceOne -= phylacteryMoveCost;
                entity.HullPointsLost = 0; //uncripple the phylactery
                return;
            }

            if ( entity.TypeData.GetHasTag("NecromancerFlagship") )
            {
                if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "NecromancerFlagshipCrippleCost"  ) )
                {
                    int hackingPointsLost = 5;

                    //if there are not enough, take as many as you can
                    if ( hackingPointsLost > this.AttachedFaction.StoredHacking )
                        hackingPointsLost = this.AttachedFaction.StoredHacking.GetNearestIntPreferringHigher();

                    if ( hackingPointsLost > 0 )
                    {
                        this.AttachedFaction.StoredHacking -= hackingPointsLost;

                        HackingType hackToDo = HackingTypeTable.Instance.GetRowByName( "UnitWasCrippled" );
                        HackingEvent hackEvent = HackingEvent.Create( this.AttachedFaction.FactionIndex, this.AttachedFaction.FactionIndex, -1,
                                hackToDo, null, false, entity.TypeData.DisplayName + " of " + entity.GetFleetName_Safe() +
                                " on " + entity.GetPlanetName_Safe(), hackingPointsLost );
                        hackEvent.HackingPointsSpent = (FInt)hackingPointsLost;
                        this.AttachedFaction.HackingHistory.Add( hackEvent );
                    }
                }
            }
            if ( entity.TypeData.GetHasTag("NecromancerNecropolis" ) &&
                 AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "NecromancerSharkB"  ) )
            {
                Faction factionThatCrippledEntity = FiringSystemOrNull?.ParentEntity?.GetFactionOrNull_Safe();
                GlobalGeneralDeepInfoCommandHandler.TriggerSharkB( entity.PlanetFaction.Faction, factionThatCrippledEntity, entity, Context );
            }
        }

        #region SwapAIDefenses
        public void SwapAIDefenses( ArcenHostOnlySimContext Context )
        {
            //Some AI defensive strcutures are abusable for the Necromancer; anything that produces an infinite number of ships is going to be endlessly harvestable by the Necromancer
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "SwapOutForNecromancer" ) )
            {
                //just swap these for Fortresses
                GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRowByName("AIFortress");
                GameEntity_Squad fortress = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( entity.PlanetFaction, typeData, 7,
                    null, 0, entity.WorldLocation, Context, "Necromancer-SwapOut" );
                entity.Despawn( Context, true, InstancedRendererDeactivationReason.SelfDestructOnTooHighOfCap );
            }
        }
        #endregion

        #region ClearExcessShips
        public void ClearExcessShips( ArcenHostOnlySimContext Context )
        {
            //This is used to make sure that one can't use bolstering to bypass the unit limits. The exploit is
            //Bolster fleet 1. Build the ships from the bolstering. Bolster fleet 2. Build the ships.
            //Now both fleet 1 and fleet 2 have bonus ships
            //I think in theory DespawnAllContentsFromNoLongerBolstering is supposed to handle this,
            //but it doesn't seem to work and I'm not going to dig into that code at the moment; it's unclear
            //whether that ever worked
            bool debug = false;
            foreach ( Fleet fleet in World_AIW2.Instance.Fleets( this.AttachedFaction, FleetStatus.CenterpieceMustLiveOrLooseFleet ) )
            {
                foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                {
                    if ( mem.EntitiesOfFMem.GetItemCount() <= 0 )
                        continue; //there aren't any units, so nothing to do here

                    if ( !mem.TypeData.IsMobileCombatant ||
                         mem.TypeData.GetHasTag( "NecromancerBottomTier" ) ||
                         mem.TypeData.GetHasTag( "NecromancerMidTier" ) ||
                         mem.TypeData.GetHasTag( "NecromancerHighTier" ) ||
                         mem.TypeData.SpecialType == SpecialEntityType.MobileCustomCityFedFleetFlagship )
                        continue;

                    //specifically we care about the extra bodyguard/summons you can get from bolstering
                    if ( mem.TypeData.GetHasTag( "NecromancerBodyguard" ) ||
                         mem.TypeData.GetHasTag( "NecromancerSummon" ) )
                    {
                        int excessUnits = mem.EntitiesOfFMem.GetItemCount() - mem.ExplicitBaseSquadCap;
                        if ( excessUnits > 0 )
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine("Destroying " +  excessUnits + "  " + mem.TypeData.GetDisplayName() + " for fleet " + fleet.GetName() +". Base cap is " + mem.ExplicitBaseSquadCap + " and we have " + mem.EntitiesOfFMem.GetItemCount(), Verbosity.DoNotShow );
                            foreach ( GameEntity_Squad squad in mem.Entities )
                            {
                                if ( debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine("\tDespawning " + squad.ToStringWithPlanet(), Verbosity.DoNotShow );
                                squad.Despawn( Context, true, InstancedRendererDeactivationReason.SelfDestructOnTooHighOfCap );
                                excessUnits--;
                                if ( excessUnits <= 0 )
                                    break;
                            }
                        }
                    }
                }
            }
        }
        #endregion
        public GameEntity_Squad SpawnNecromancerNecropolis( ArcenPoint spawnLocation, Planet planet, string TypeName, Faction faction, 
                                                            ArcenHostOnlySimContext Context, out GameEntity_Squad NecroFlagship, bool exactPlacement )
        {
            if ( !ArcenNetworkAuthority.GetIsHostMode() )
            {
                NecroFlagship = null;
                return null; //only for the host; clients will get this data sync'd to them later
            }

            GameEntityTypeData necropolisData = GameEntityTypeDataTable.Instance.GetRowByName( TypeName );
            if ( necropolisData == null ) {
                throw new Exception("Unable to find XML with name " + TypeName);
            }
            GameEntityTypeData flagshipEntityData = GameEntityTypeDataTable.Instance.GetRowByName( "NecromancerBaseFlagship" );
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
            if ( spawnLocation == ArcenPoint.ZeroZeroPoint )
            {
                spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, necropolisData, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 300 ) );
            }
            else if ( exactPlacement )
                spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, necropolisData, spawnLocation, FInt.FromParts( 0, 010 ), FInt.FromParts( 0, 020 ) );
            else
                spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, necropolisData, spawnLocation, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 250 ) );
            GameEntity_Squad necropolis = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, necropolisData, 1,
                    null, 0, spawnLocation, Context, "Necromancer-NewNecropolis" );
            if ( necropolis != null ) { }
            //I'd like to say "If this is a safe placement point, put the unit here. Otherwise place it "very close by"  This is the best way.
            GameEntity_Squad necromancerFlagship = null;
            //TODO: I'm not sure how one handles bolstering fleets at the moment, so currently bolster doesn't work
            bool spawnsFlagship = necropolisData.GetHasTag("SpawnsNecromancerFleet");
            bool bolstersFlagship = necropolisData.GetHasTag("BolstersNecromancerFleet");

            if ( spawnsFlagship )
            {
                spawnLocation = planet.GetSafePlacementPoint_AroundEntity( Context, flagshipEntityData, necropolis, FInt.FromParts( 0, 005 ), FInt.FromParts( 0, 010 ) );

                necromancerFlagship = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, flagshipEntityData, 1,
                           null, 0, spawnLocation, Context, "Necromancer-NewNecropoFlagship" );
                NecroFlagship = necromancerFlagship;
                if ( necromancerFlagship == null )
                    return necropolis;
                necromancerFlagship.FleetMembership.Fleet.IsFleetFlagshipAllowedToUseMovementModes = true;
            }
            else
                NecroFlagship = null;



            Fleet newNecromancerCityFleet = necropolis.FleetMembership.Fleet;

            string nameBase = GetAvailableCityName( necropolis, faction, Context );

            newNecromancerCityFleet.NameRaw = "Necropolis " + nameBase;
            newNecromancerCityFleet.FleetQualifier = "Necro";

            NecromancerCityFleetBaseInfo necroCityFleetInfo = newNecromancerCityFleet.CreateExternalBaseInfo<NecromancerCityFleetBaseInfo>("NecromancerCityFleetBaseInfo");

            if ( bolstersFlagship )
            {
                necroCityFleetInfo.CanChangeBolsteredFleet = true;
            }
            
            if ( spawnsFlagship )
            {
                Fleet newNecromancerMobileFleet = necromancerFlagship.FleetMembership.Fleet;
                newNecromancerMobileFleet.NameRaw = "Necrofleet " + nameBase;
                newNecromancerMobileFleet.FleetQualifier = "Necro";
                newNecromancerMobileFleet.CreateExternalBaseInfo<NecromancerMobileFleetBaseInfo>( "NecromancerMobileFleetBaseInfo" );
                newNecromancerCityFleet.CityBolstersFleetID = newNecromancerMobileFleet.FleetID;
            }
            //            ArcenDebugging.ArcenDebugLogSingleLine("Created a new fleet, " + newNecromancerFleet.GetName() + " with flagship " + necromancerFlagship.ToString(), Verbosity.DoNotShow );
            // List<GameEntityTypeData> InitialShipsForFlagship = GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( "NecromancerSummons" );
            // for ( int i = 0; i < InitialShipsForFlagship.Count; i++ )
            // {
            //     GameEntityTypeData entitydata = InitialShipsForFlagship[i];
            //     int nextUniqueID = newNecromancerFleet.GetNextUniqueIntToUseOfMatchingMembershipGroupsBasedOnSquadType( entitydata );
            //     FleetMembership mem = newNecromancerFleet.GetOrAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates( entitydata, nextUniqueID );
            //     mem.ExplicitBaseSquadCap = 1;
            // }
            faction.StoredScience += 1000;

            this.BaseInfo.Necropoleis.AddToDisplayList(necropolis);
            return necropolis;
        }

        /* Long Range Planning Functions */
        private static readonly List<SafeSquadWrapper> AllShipsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "NecromancerEmpireFactionDeepInfo-AllShipsLRP" );
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            AllShipsLRP.Clear();
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads() )
            {
                if ( entity.TypeData.GetHasTag( "NecromancerNecropolis" ) ||
                     entity.TypeData.GetHasTag( "NecromancerFlagship" ) )
                    continue;
                AllShipsLRP.Add( entity );
            }
            if ( World_AIW2.Instance.Setup.GetStringBySetting("NecromancerAutoDefend") != "Disabled" )
            {
                HandleNecromancerAutoDefend( Context );
            }
            FleetBehaviorLRP.DoLRP( AttachedFaction, Context );
        }

        /* Death Effect related stuff */
        public static Fleet GetBestMobileFleetForShip( GameEntity_Squad oldShip, Faction factionOrNull, ArcenHostOnlySimContext Context )
        {
            int debugCode = 0;
            try{
                debugCode = 100;
                Fleet fallbackFleet = null;
                Fleet bestFleet = null;
                Faction faction = factionOrNull;
                if ( factionOrNull == null )
                    faction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( faction == null || !NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( faction ) )
                    faction = FactionUtilityMethods.Instance.GetStrongestNecromancerFactionOnPlanet( oldShip.Planet );
                if ( faction == null )
                    return null;
                bool tracing = false;
                if ( Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Necromancer ) )
                    tracing = true;

                int previousFleetDist = -1;
                int previousFleetHops = -1;
                debugCode = 200;
                if ( tracing )
                    ArcenDebugging.ArcenDebugLogSingleLine(oldShip.ToStringWithPlanetAndOwner() + " is vulnerable to necromancy!", Verbosity.DoNotShow );
                foreach ( Fleet fleet in World_AIW2.Instance.Fleets( faction, FleetStatus.CenterpieceMustLiveOrLooseFleet ) )
                {
                    debugCode = 300;
                    if ( fleet == null || fleet.Centerpiece.GetSquad() == null )
                        continue;
                    if ( !fleet.Centerpiece.GetSquad().TypeData.GetHasTag("NecromancerFlagship") )
                        continue;
                    if ( fleet.Category != FleetCategory.PlayerCustomCityFedMobile )
                        continue; //only mobile fleets
                    if ( !AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "CrippledFlagshipCanPracticeNecromancy"  ) &&
                         fleet.Centerpiece.GetSquad().GetIsCrippled() )
                        continue;
                    debugCode = 400;
                    if ( fleet.Centerpiece.GetSquad().Planet == oldShip.Planet )
                    {
                        debugCode = 500;
                        int newDistance = Mat.DistanceBetweenPointsImprecise( oldShip.WorldLocation, fleet.Centerpiece.GetSquad().WorldLocation );
                        if ( previousFleetDist == -1 || newDistance < previousFleetDist)
                        {
                            debugCode = 600;
                            bestFleet = fleet;
                            previousFleetDist = newDistance;
                        }
                        continue;
                    }
                    debugCode = 700;
                    if ( bestFleet == null )
                    {
                        debugCode = 800;
                        int maxHops = 1;
                        if ( AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "ForgivingNecromancy"  ))
                            maxHops = 999;
                        //if we haven't found a fleet on this planet, make sure we have a fallback (the fleet closest to the planet)
                        int hops = oldShip.Planet.GetHopsTo(fleet.Centerpiece.GetSquad().Planet);
                        if ( hops <= maxHops &&
                             (previousFleetHops == -1 || hops < previousFleetHops ) )
                        {
                            debugCode = 900;
                            fallbackFleet = fleet;
                            previousFleetHops = hops;
                        }
                    }
                    debugCode = 1000;
                }
                debugCode = 1100;
                if ( bestFleet != null )
                {
                    debugCode = 1200;
                    if ( tracing )
                        ArcenDebugging.ArcenDebugLogSingleLine("Transferring " + oldShip.ToStringWithPlanet() + " to best necromancer fleet " + bestFleet.GetName(), Verbosity.DoNotShow );
                    return bestFleet;
                }
                debugCode = 1300;
                if ( tracing )
                {
                    if ( fallbackFleet != null )
                        ArcenDebugging.ArcenDebugLogSingleLine("Transferring " + oldShip.ToStringWithPlanet() + " to fallback necromancer fleet " + fallbackFleet.GetName(), Verbosity.DoNotShow );
                    else
                        ArcenDebugging.ArcenDebugLogSingleLine( oldShip.ToStringWithPlanet() + " could not find a suitable necromancer fleet", Verbosity.DoNotShow );
                }
                return fallbackFleet;
            } catch(Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in Necromancer::GetBestFleetForShip debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            return null;
        }
        public static int GetNumShipsToCreate( GameEntityTypeData typeData, Faction faction, Fleet fleet, ArcenHostOnlySimContext Context )
        {
            int ships = 1;
            int debugCode = 100;
            try{
                if ( fleet == null )
                    return ships;
                debugCode = 200;
                NecromancerMobileFleetBaseInfo fleetInfo = fleet.TryGetExternalBaseInfoAs<NecromancerMobileFleetBaseInfo>();
                if ( fleetInfo != null )
                {
                    debugCode = 300;
                    int workingPercent = 0;
                    if ( typeData.GetHasTag("NecromancerBottomTier") )
                        workingPercent = fleetInfo.BonusSkeletonPercent.Display;
                    if ( typeData.GetHasTag("NecromancerMidTier") )
                        workingPercent = fleetInfo.BonusWightPercent.Display;
                    if ( typeData.GetHasTag("NecromancerHighTier") )
                        workingPercent = fleetInfo.BonusMummyPercent.Display;
                    debugCode = 400;
                    while ( workingPercent > 0 )
                    {
                        if ( Context.RandomToUse.Next( 0, 100 ) < workingPercent )
                            ships++;
                        workingPercent -= 100;
                    }
                }
            } catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in NecromancerEmpireFactionDeepInfo::GetNumShipsToCreate debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            return ships;
        }

        public static NecromancyShipType GetShipTypeToSummonViaNecromancy( GameEntityTypeData typeData )
        {
            if ( typeData.IsStrikecraft || typeData.GetHasTag( "BecomesNecromancySkeleton") )
            {
                return NecromancyShipType.Skeleton;
            }
            else if ( typeData.SpecialType == SpecialEntityType.Frigate ||
                      typeData.SpecialType == SpecialEntityType.AIGuardian ||
                      typeData.GetHasTag( "BecomesNecromancyWight") )
            {
                return NecromancyShipType.Wight;
            }
            else if ( typeData.SpecialType == SpecialEntityType.AIDireGuardian ||
                      typeData.GetHasTag( "BecomesNecromancyMummy" ) )
            {
                return NecromancyShipType.Mummy;
            } else
            {
                return NecromancyShipType.None;
            }
        }

        public static GameEntityTypeData GetShipToSummonViaNecromancy( GameEntity_Squad oldShip, Fleet mobileCityFedFleetForNewShip, ArcenHostOnlySimContext Context )
        {
            if ( mobileCityFedFleetForNewShip == null )
                return null;
            NecromancerMobileFleetBaseInfo mobileCityFleetInfo = mobileCityFedFleetForNewShip.TryGetExternalBaseInfoAs<NecromancerMobileFleetBaseInfo>();
            if ( mobileCityFleetInfo == null )
                return null;

            int debugCode = 0;
            try{
            //Perhaps individual ships should be allowed to add to their available ships?
            //like "You can take the Upgrade Skeleton enhancement, so your new skeletons will be stronger
            bool tracing = false;
            if ( Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Necromancer ) )
                tracing = true;
            string tag = "";
            string logStr = "";
            debugCode = 100;
            Faction faction = mobileCityFedFleetForNewShip.Faction;
            PlayerTypeData playerType = faction.PlayerTypeDataOrNull_ModeratelyExpensive;
            List<TechUpgrade> upgrades = playerType.TechUpgradesForThisPlayerType;
            //rework this; the player gets too many normal skeletons.
            //Thoughts: Techs for Increasing percentage of skeletons become stronger skeletons
            //like "10% of skeletons would become skeleton Warriors" or "20% of skeletons would become Skeleton Flayers"
            //then define several of these variants
            //Then you could spend more science to say "Skeleton Flayers are at a higher mark level" and "also spawn more skeleton flayers"
            //I think a base skeleton, then several variantes. Unclear if the variants should be in a power hierarchy, or all equally balanced choices
            //I think I should allow you to crank each variant up to 45%, but then prevent you from upgrading if your total percentages would exceed 100

            //another option: 50% of skeletons become Skeleton Grunts (tanky, short range) and 50% become Skeleton Archers (weaker, longer range)
            NecromancerEmpireFactionBaseInfo gData = faction.TryGetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
            debugCode = 200;
            switch ( GetShipTypeToSummonViaNecromancy( oldShip.TypeData ) ) {
                case NecromancyShipType.Skeleton: {
                    debugCode = 300;
                    tag = "NecromancerBaseSkeleton";
                    Dictionary<string, int> PercentSkeletonType = mobileCityFleetInfo.PercentSkeletonType.GetDisplayDict();
                    if ( oldShip.NumTimesZombified > 10 )
                    {
                        tag = "NecromancerOverLimitStrikecraftSummon";
                        logStr = "Choosing skeleton attritioner since " + oldShip.ToString() + " has been zombified a lot";
                    }
                    else if ( oldShip.TypeData.IsDrone )
                    {
                        tag = "NecromancerOverLimitStrikecraftSummon";
                        logStr = "Choosing skeleton attritioner since " + oldShip.ToString() + " is a drone";
                    }
                    else if ( mobileCityFleetInfo.NumSkeletonsInFleet.Display > mobileCityFleetInfo.SkeletonSoftCap.Display )
                    {
                        tag = "NecromancerOverLimitStrikecraftSummon";
                        logStr = "Choosing skeleton attritioner; we have " + mobileCityFleetInfo.NumSkeletonsInFleet.Display + " skeletons already, and our cap is " +mobileCityFleetInfo.SkeletonSoftCap;
                    }
                    else if ( PercentSkeletonType.Count != 0 )
                    {
                        debugCode = 400;
                        //There's a certain % that we will become a differnt type of skeleton this time!
                        int rand = Context.RandomToUse.Next(0, 100);
                        int sum = 0;
                            //                    ArcenDebugging.ArcenDebugLogSingleLine("We might get a different skeleton type. rand " + rand, Verbosity.DoNotShow );
                        foreach ( KeyValuePair<string, int> pair in PercentSkeletonType )
                        {
                            debugCode = 500;
                            sum += pair.Value;
                            if ( rand < sum )
                            {
                                //                            ArcenDebugging.ArcenDebugLogSingleLine("\tsum " + sum + " new tag " + tag, Verbosity.DoNotShow );
                                logStr = "Choosing variant with a " + pair.Value +"% chance";
                                tag = pair.Key;
                                break;
                            }
                            //                        ArcenDebugging.ArcenDebugLogSingleLine("\tsum " + sum + ", we did not choose " + tag, Verbosity.DoNotShow );
                        }
                    }
                    debugCode = 520;
                    break;
                }
                case NecromancyShipType.Wight: {
                    debugCode = 600;
                    tag = "NecromancerWight";
                    Dictionary<string, int> PercentWightType = mobileCityFleetInfo.PercentWightType.GetDisplayDict();
                    if ( oldShip.TypeData.IsDrone )
                    {
                        tag = "NecromancerOverLimitMidSummon";
                        logStr = "Choosing wight attritioner since " + oldShip.ToString() + " is a drone";
                    }
                    else if ( oldShip.NumTimesZombified > 10 )
                    {
                        tag = "NecromancerOverLimitMidSummon";
                        logStr = "Choosing wight attritioner since " + oldShip.ToString() + " has been zombified a lot";
                    }

                    else if ( mobileCityFleetInfo.NumWightsInFleet.Display > mobileCityFleetInfo.WightSoftCap.Display )
                    {
                        tag = "NecromancerOverLimitMidSummon";
                        logStr = "Choosing wight attritioner; we have " + mobileCityFleetInfo.NumWightsInFleet.Display + " wights already";
                    }
                    else if ( mobileCityFleetInfo.PercentWightType != null &&
                              mobileCityFleetInfo.PercentWightType.Count != 0 )
                    {
                        debugCode = 700;
                        //There's a certain % that we will become a differnt type of skeleton this time!
                        int rand = Context.RandomToUse.Next(0, 100);
                        int sum = 0;
                            //                    ArcenDebugging.ArcenDebugLogSingleLine("We might get a different wight type. rand " + rand, Verbosity.DoNotShow );
                        foreach ( KeyValuePair<string, int> pair in PercentWightType )
                        {
                            debugCode = 800;
                            sum += pair.Value;
                            if ( rand < sum )
                            {
                                //                            ArcenDebugging.ArcenDebugLogSingleLine("\tsum " + sum + " new tag " + tag, Verbosity.DoNotShow );
                                logStr = "Choosing variant with a " + pair.Value +"% chance";
                                tag = pair.Key;
                                break;
                            }
                            //                        ArcenDebugging.ArcenDebugLogSingleLine("\tsum " + sum + ", we did not choose " + tag, Verbosity.DoNotShow );
                        }
                    }
                    break;
                }
                case NecromancyShipType.Mummy: {
                    debugCode = 900;
                    tag = "NecromancerMummy";
                    Dictionary<string, int> PercentMummyType = mobileCityFleetInfo.PercentMummyType.GetDisplayDict();
                    if ( PercentMummyType.Count != 0 )
                    {
                        debugCode = 700;
                        //There's a certain % that we will become a differnt type of skeleton this time!
                        int rand = Context.RandomToUse.Next(0, 100);
                        int sum = 0;
                        //ArcenDebugging.ArcenDebugLogSingleLine("We might get a different mummy type. rand " + rand, Verbosity.DoNotShow );
                        foreach ( KeyValuePair<string, int> pair in PercentMummyType )
                        {
                            debugCode = 800;
                            sum += pair.Value;
                            if ( rand < sum )
                            {
                                logStr = "Choosing variant with a " + pair.Value +"% chance";
                                //ArcenDebugging.ArcenDebugLogSingleLine("\tsum " + sum + " new tag " + pair.Key, Verbosity.DoNotShow );
                                tag = pair.Key;
                                break;
                            }
                            //ArcenDebugging.ArcenDebugLogSingleLine("\tsum " + sum + ", we did not choose " + pair.Key, Verbosity.DoNotShow );
                        }
                    }
                    break;
                }
                case NecromancyShipType.None: {
                    debugCode = 1000;
                    tag = "NecromancerOverLimitStrikecraftSummon"; //default; something nice and weak
                    logStr = "On 'unclassified unit' path";
                    break;
                }
            }
            if ( tracing )
                ArcenDebugging.ArcenDebugLogSingleLine("When killing " + oldShip.ToStringWithPlanetAndOwner() + " we chose to transform it into " + tag + ". " + logStr +
                    ". Skeleton soft cap: " + mobileCityFleetInfo.SkeletonSoftCap.Display + " and current: " + mobileCityFleetInfo.NumSkeletonsInFleet.Display +
                    ". wight soft cap: " + mobileCityFleetInfo.WightSoftCap.Display + " and current: " + mobileCityFleetInfo.NumWightsInFleet.Display,  Verbosity.DoNotShow );
            debugCode = 100;
            return GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, tag );
            } catch(Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in GetShipToSummonViaNecromancy debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            return null;
        }
        #region
        public override void DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly( ref int debugStage, GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull,
              Faction factionThatKilledEntity, Faction entityOwningFaction, int numExtraStacksKilled, ArcenHostOnlySimContext Context )
        {
            if ( entity == null )
                return;
            GameEntityTypeData entityType = entity.TypeData;
            if ( entityType == null )
                return;
            switch ( entityType.SpecialType )
            {
                case SpecialEntityType.AICommandStationOriginal:
                  World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Necropolis", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                  break;
                case SpecialEntityType.GuardPost:
                case SpecialEntityType.DireGuardPost:
                {
                    var dlc3 = entityType.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );
                    if ( dlc3 != null )
                    {
                        if ( dlc3.ImmuneToNecromancy )
                            break;
                    }

                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_GuardPosts", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    this.ConsiderReanimatingGuardPost( entity, entityType, Context );
                    break;
                }
            }
        }
        #endregion
        private void ConsiderReanimatingGuardPost( GameEntity_Squad entity, GameEntityTypeData entityType, ArcenHostOnlySimContext Context )
        {
            Planet planet = entity.Planet;
            if ( planet == null )
                return; //can't reanimate if there's no place to reanimate at!

            if ( entity.GetFactionTypeSafe() == FactionType.Player ) {
                // This guard post has already been reanimated by a player, so don't reanimate a second copy.
                return;
            }

            if ( !entityType.IsCombatant )
                return; //if no weapons, don't convert

            Faction fac = this.AttachedFaction;
            Faction facOwning = planet.GetControllingFaction();
            if ( facOwning == fac )
            {
                //if we control this planet, then 100% we'll be the ones to reanimate it
                this.DefinitelyReanimateGuardPost( entity, entityType, planet, false, Context );
                return;
            }
            if ( facOwning != null && facOwning.Type == FactionType.Player )
            {
                //if another necromancer of any sort controls this planet, then definitely we don't reanimate this
                if ( facOwning.TryGetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>() != null )
                    return;
            }

            bool shouldReanimate = false;
            //if this planet isn't controlled by any necromancer, then...
            int countOfConvertersFromMyFaction = planet.GetPlanetFactionForFaction( fac ).Entities.GetCountFromListOfEntitiesByTag( "NecroGuardPostConverter" );
            shouldReanimate = countOfConvertersFromMyFaction > 0; //if we have any converters here, then we reanimate

            //we need to see what other necromancers have
            foreach ( Faction otherPlayer in World_AIW2.Instance.AllPlayerFactions )
            {
                if ( otherPlayer == fac )
                    continue; //skip ourselves
                if ( otherPlayer.TryGetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>() == null )
                    continue; //if the other faction isn't a necromancer, then skip it
                int countOfConvertersFromOtherFaction = planet.GetPlanetFactionForFaction( otherPlayer ).Entities.GetCountFromListOfEntitiesByTag( "NecroGuardPostConverter" );

                //if the other necromancer faction has more converters than me, then they will get this guard post, not me
                if ( countOfConvertersFromOtherFaction > countOfConvertersFromMyFaction )
                    return;

                //if they and we have equal number of converters, then...
                if ( countOfConvertersFromOtherFaction == countOfConvertersFromMyFaction )
                {
                    if ( otherPlayer.FactionIndex < fac.FactionIndex )
                        return; //...if they also have a lower faction index than us, then skip
                }
            }

            if ( shouldReanimate )
            {
                //okay, looks like this one is for us, then!
                this.DefinitelyReanimateGuardPost( entity, entityType, planet, false, Context );
            }
            else
            {
                if ( planet.GetPlanetFactionForFaction( fac ).Entities.SquadCount > 0 )
                {
                    //we won't do a normal reanimation, but since we have a unit here but no capturable, we'll put it to remains.
                    this.DefinitelyReanimateGuardPost( entity, entityType, planet, true, Context );
                }
            }
        }

        private void DefinitelyReanimateGuardPost( GameEntity_Squad entity, GameEntityTypeData entityType, Planet planet, bool ReanimateToRemains, ArcenHostOnlySimContext Context )
        {
            PlanetFaction pFac = planet.GetPlanetFactionForFaction( this.AttachedFaction );
            Fleet fleetForFactionAtPlanet = pFac.FleetUsedAtPlanet;
            
            GameEntity_Squad reanimatedGuard = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFac, entityType, entity.CurrentMarkLevel, fleetForFactionAtPlanet, 
                0, entity.WorldLocation, Context, "Necromancer-ReanimateGP" );
            
            if ( reanimatedGuard != null )
            {
                reanimatedGuard.Network_FrameToStartAnyProcessing = World_AIW2.Instance.Network_CurrentFrameNumber + 3;
                
                //the new reanimated guard post needs to be at 1 health and 0 shields
                reanimatedGuard.HullPointsLost = reanimatedGuard.GetMaxHullPoints() - 1;
                reanimatedGuard.ShieldPointsLost = reanimatedGuard.GetMaxShieldPoints();

                //it's going to be in self-building mode for a while
                FInt costToBuild = (FInt)(entityType.SpecialType == SpecialEntityType.DireGuardPost ? 40000 : 10000);
                if ( reanimatedGuard.CurrentMarkLevel > 0 )
                    costToBuild *= reanimatedGuard.CurrentMarkLevel;

                reanimatedGuard.SelfBuildingMetalRemaining = costToBuild;
                reanimatedGuard.FleetMembership.OverridingMetalCost = costToBuild.IntValue;
                reanimatedGuard.CustomBaseMark = entity.CurrentMarkLevel;

                if ( ReanimateToRemains )
                {
                    reanimatedGuard.SecondsSpentAsRemains = 0;
                }
            }
        }

        public override bool SeedUnitsOnStartingPlanetDuringMapGen( Planet StartingPlanet, ConfigurationForFaction factionConfig, PlanetFaction pFaction, 
            ref ArcenPoint commandStationPoint, ref bool stillNeedsToSeedHumanHomeworldStuff, 
            Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull, ArcenHostOnlySimContext Context )
        {
            int debugIndex = 0;
            try
            {
                debugIndex = 10;
                Faction necroFaction = pFaction.Faction;
                debugIndex = 15;

                NecromancerEmpireFactionDeepInfo necroDeepInfo = this;
                NecromancerEmpireFactionBaseInfo necroBaseInfo = necroDeepInfo.BaseInfo;

                debugIndex = 20;

                GameEntity_Squad necroFlagship;
                GameEntity_Squad necropolis = necroDeepInfo.SpawnNecromancerNecropolis( ArcenPoint.ZeroZeroPoint, StartingPlanet, "NecromancerPhylactery", necroFaction, Context, out necroFlagship, false );
                debugIndex = 30;
                Fleet necroCity = necropolis.GetFleetOrNull_Safe();
                
                FInt placementOffsetScale = FInt.FromParts( 1, 500 );
                debugIndex = 40;
                StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, necropolis.WorldLocation, pFaction, necroCity, placementOffsetScale, "NecromancerShipyard", 2400, 600 );
                debugIndex = 50;
                if ( necroFlagship != null )
                {
                    debugIndex = 60;
                    Fleet necroFleet = necroFlagship.GetFleetOrNull_Safe();
                    GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    command.RelatedIntegers.Add( necroFleet.FleetID ); //FleetID
                    command.RelatedIntegers.Add( PlayerAccount.Local.PlayerPrimaryKeyID );
                    command.RelatedString = "ToggleIsFleetOnPlayerWatchlist";
                    command.RelatedBool = true;
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                    debugIndex = 70;
                    necroFleet.IsFleetInTransportLoadMode = false;

                    GameEntityTypeData spawnType;

                    debugIndex = 80;

                    //60 skeleton base
                    spawnType = GameEntityTypeDataTable.Instance.GetRowByName( "BaseSkeleton" );
                    for ( int i = 0; i < 60; i++ )
                        necroFlagship.SpawnEntity_ReturnNullIfMPClient( spawnType, 1, necroFleet, 0, 0, Context, "NecroSidekickStart", false );

                    debugIndex = 90;

                    //18 wight base
                    spawnType = GameEntityTypeDataTable.Instance.GetRowByName( "BaseWight" );
                    for ( int i = 0; i < 18; i++ )
                        necroFlagship.SpawnEntity_ReturnNullIfMPClient( spawnType, 1, necroFleet, 0, 0, Context, "NecroEmpireStart", false );

                    debugIndex = 100;
                }

                stillNeedsToSeedHumanHomeworldStuff = false;

                debugIndex = 110;

                debugIndex = 120;
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Lore", string.Empty, necroFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Introduction", string.Empty, necroFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Resources", string.Empty, necroFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Phylactery", string.Empty, necroFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Hexes", string.Empty, necroFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                debugIndex = 200;
                necroFaction.StoredMetal = (FInt)(400 * 1000);
                necroFaction.StoredScience = FInt.FromParts( 3000, 000 );
                necroFaction.StoredHacking = FInt.FromParts( 45, 000 );
                necroFaction.StoredFactionResourceOne = FInt.FromParts( 50, 000 ); //enough to get things started.
                //At the beginning of the game the player gets one lowest-tier skeleton type and wight variant type
                //the goal of this is to let the player start out a bit stronger
                debugIndex = 300;
                //GameEntityTypeData newShipLineData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( tagAndfieldName, false ) );
                string tagAndFieldName = "NecromancerSkeletonGroup";
                NecromancerUpgrade upgrade = NecromancerUpgradeTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( tagAndFieldName, false ) );
                if ( upgrade == null )
                {

                    upgrade = NecromancerUpgradeTable.Instance.GetRandomBonusStartingSkeletonType( Context );
                }
                necroBaseInfo.NecromancerCompletedUpgrades.Add( upgrade );

                debugIndex = 400;
                NecromancerUpgradeEvent thisEvent = NecromancerUpgradeEvent.Create( necroFaction.FactionIndex, -1, StartingPlanet.Index, upgrade.Index, -1, null );
                necroBaseInfo.NecromancerHistory.Add( thisEvent );
                StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, necropolis.WorldLocation, pFaction, necroCity, placementOffsetScale, upgrade.ShipForCapIncrease, 700, 600 );

                debugIndex = 500;
                tagAndFieldName = "NecromancerWightGroup";
                upgrade = NecromancerUpgradeTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( tagAndFieldName, false ) );
                if ( upgrade == null )
                {
                    upgrade = NecromancerUpgradeTable.Instance.GetRandomBonusStartingWightType( Context );
                }
                necroBaseInfo.NecromancerCompletedUpgrades.Add( upgrade );
                debugIndex = 600;
                thisEvent = NecromancerUpgradeEvent.Create( necroFaction.FactionIndex, -1, StartingPlanet.Index, upgrade.Index, -1, null );
                necroBaseInfo.NecromancerHistory.Add( thisEvent );
                StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, necropolis.WorldLocation, pFaction, necroCity, placementOffsetScale, upgrade.ShipForCapIncrease, 1600, 600 );

                debugIndex = 650;
                tagAndFieldName = "NecromancerUtilityGroup";
                upgrade = NecromancerUpgradeTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( tagAndFieldName, false ) );
                if ( upgrade == null )
                {
                    upgrade = NecromancerUpgradeTable.Instance.GetRandomBonusStartingUtilityType( Context );
                }
                necroBaseInfo.NecromancerCompletedUpgrades.Add( upgrade );
                debugIndex = 600;
                thisEvent = NecromancerUpgradeEvent.Create( necroFaction.FactionIndex, -1, StartingPlanet.Index, upgrade.Index, -1, null );
                necroBaseInfo.NecromancerHistory.Add( thisEvent );
                StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, necropolis.WorldLocation, pFaction, necroCity, placementOffsetScale, upgrade.ShipForCapIncrease, 1200, -200 );

                debugIndex = 700;
                if ( necroBaseInfo.BonusStartingResources > 0 )
                {
                    necroFaction.StoredScience += FInt.FromParts( 1000, 000 ) * necroBaseInfo.BonusStartingResources;
                    necroFaction.StoredHacking += FInt.FromParts( 20, 000 ) * necroBaseInfo.BonusStartingResources;
                    necroFaction.StoredFactionResourceOne += FInt.FromParts( 10, 000 ) * necroBaseInfo.BonusStartingResources;
                }
                debugIndex = 800;
                if ( necroBaseInfo.BonusStartingWight )
                {
                    do
                    {
                        upgrade = NecromancerUpgradeTable.Instance.GetRandomBonusStartingWightType( Context );
                    } while ( necroBaseInfo.NecromancerCompletedUpgrades.Contains( upgrade ) );
                    necroBaseInfo.NecromancerCompletedUpgrades.Add( upgrade );
                    thisEvent = NecromancerUpgradeEvent.Create( necroFaction.FactionIndex, -1, StartingPlanet.Index, upgrade.Index, -1, null );
                    necroBaseInfo.NecromancerHistory.Add( thisEvent );
                }
                if ( necroBaseInfo.StartWithAllUpgrades )
                {
                    while ( true )
                    {
                        upgrade = NecromancerUpgradeTable.Instance.GetNextFactionUpgrade( );
                        if ( upgrade == null )
                            break;
                        necroBaseInfo.NecromancerCompletedUpgrades.Add( upgrade );
                        thisEvent = NecromancerUpgradeEvent.Create( necroFaction.FactionIndex, -1, StartingPlanet.Index, upgrade.Index, -1, null );
                        necroBaseInfo.NecromancerHistory.Add( thisEvent );
                    }
                }
                debugIndex = 2000;
            }
            catch ( Exception e )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "NecromancerEmpireFactionDeepInfo.SeedUnitsOnStartingPlanetDuringMapGen error at debugIndex " + debugIndex + ": " + e );
                return false;
            }
            return true;
        }

        public override void SeedSpecialEntities_LateAfterAllFactionSeeding_CustomForPlayerType( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData MapData )
        {
            int extraDistanceForAdajentSeededItems = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "ExtraDistanceForAdajentSeededItems" );
            int extraDistanceForMiddleDistanceItems = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "ExtraDistanceForMiddleDistanceItems" );
            int reducedDistanceRestrictionForAnyItems = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "ReducedDistanceRestrictionForAnyItems" );

            IList<Planet> planetsSeeded;
            //one Skeleton Amplifier and one Wight Amplifier near this necromancer
            int necromancersInGame = 0;
            foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
            {
                PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                if ( playerType == null )
                    continue; //only seed ARS if they say to
                if ( playerType.GetHasTag("NecromancerEmpire") )
                {
                    //each necromancer gets a skeleton and wight amplifier close to them
                    necromancersInGame++;
                    planetsSeeded = StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "SkeletonAmplifier", SeedingType.CapturableWeightsAndMax,
                        1, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 1, 1 + extraDistanceForAdajentSeededItems, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                                     player, (Int16)(1 + extraDistanceForAdajentSeededItems) );
                    planetsSeeded = StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "WightAmplifier", SeedingType.CapturableWeightsAndMax,
                        1, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 2 - reducedDistanceRestrictionForAnyItems, 3 + extraDistanceForAdajentSeededItems, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                                     player, (Int16)(3 + extraDistanceForAdajentSeededItems) );
                }
            }
            if ( necromancersInGame == 0 )
                return;
            int skeletonAmps = 4 + necromancersInGame * 2;
            int wightAmps = 2 + necromancersInGame * 2;
            int mummyAmps = 1 + necromancersInGame;
            //seed skeleton amps
            planetsSeeded = StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "SkeletonAmplifier", SeedingType.CapturableWeightsAndMax,
                                                        skeletonAmps, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 3 - reducedDistanceRestrictionForAnyItems, 99, 3, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
            //seed wight amps
            planetsSeeded = StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "WightAmplifier", SeedingType.CapturableWeightsAndMax,
                                                        wightAmps, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 4 - reducedDistanceRestrictionForAnyItems, 99, 3, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
            //And mummy amps
            planetsSeeded = StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "MummyAmplifier", SeedingType.CapturableWeightsAndMax,
                                                        mummyAmps, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 5 - reducedDistanceRestrictionForAnyItems, 99, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );

        }

    }
}
