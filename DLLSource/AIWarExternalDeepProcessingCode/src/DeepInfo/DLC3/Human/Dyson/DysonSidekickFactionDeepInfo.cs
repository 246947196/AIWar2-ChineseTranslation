using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{

    public class DysonSidekickFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        //Set immediately before WorkingPlanetsLRP.Sort(...) so the comparison can be a non-capturing
        //static delegate.  [ThreadStatic] because long-range planning runs on a background thread.
        [ThreadStatic] private static GameEntity_Squad cb_dysonProdSquad;
        public DysonSidekickFactionBaseInfo BaseInfo;
        public static DysonSidekickFactionDeepInfo Instance = null;
        private static readonly Dictionary<GameEntityTypeData, int> AttackComposition = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed(500, "DysonFactionDeepInfo-AttackComposition");
        private static readonly Dictionary<GameEntityTypeData, int> AttackCompositionForRavagerTroops = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed(500, "DysonFactionDeepInfo-ForRavagerTroops");
        private static readonly List<GameEntity_Squad> WorkingList = List<GameEntity_Squad>.Create_WillNeverBeGCed(100, "DysonFactionDeepInfo-WorkingList");
        private static readonly List<Planet> WorkingPlanetList = List<Planet>.Create_WillNeverBeGCed(100, "DysonFactionDeepInfo-WorkingPlanetList");
        public static readonly List<SafeSquadWrapper> ExoTargets = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DysonFactionDeepInfo-ExoTargets" );
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
            Instance = this;
        }

        protected override void Cleanup()
        {
            Instance = null;
            BaseInfo = null;

            //probably does not matter
            WorkingAvailableNames.Clear();
            StrongholdsLRP.Clear();
            ShipyardsLRP.Clear();
            AutoDefendingShipsLRP.Clear();
            TransportsLRP.Clear();
            AttackComposition.Clear();
            AttackCompositionForRavagerTroops.Clear();
            WorkingList.Clear();
            DysonProductionLRP.Clear();
            DysonProductionNeedingOrdersLRP.Clear();
            ExoTargets.Clear();
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 3; //Should be responsive for the player

        private static readonly string[] dysonCityNames = new string[] {
            "Galileo", "Newton", "Franklin", "Euler", "Coulomb", "Volta", "Fourier", "Ampere", "Joule", "Avogadro", "Rontgen", "Curie", "Meitner", "Rosalind", "Dresselhaus", "Schrodinger", "Bothe", "Pauli", "Oppenheimer", "Cherenkov", "Feynmann", "Turing", "Godel", "Ada", "Lovelace", "von Neumann", "Alpha", "Beta", "Tau", "Ricci", "Delta", "Bohr", "Noether", "Germaine"    };
        // if ( World_AIW2.Instance.GameSecond > 60 )
        // {
        //     World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Dyson_MarkUpgrades", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        //     World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Dyson_Blueprints", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        //     World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Dyson_Amplifiers", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        //     World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Dyson_Defenses", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        // }
        // if ( World_AIW2.Instance.GameSecond > 80 )
        // {
        //     World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Dyson_Skeletons", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        //     World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Dyson_Wights", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        // }
        // if ( World_AIW2.Instance.GameSecond > 400 )
        // {
        //     World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Dyson_ResourceMonitoring", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        // }
        // if ( World_AIW2.Instance.GameSecond > 360 )
        // {
        //     World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Dyson_Towers", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        // }
        // if ( World_AIW2.Instance.GameSecond > 320 )
        // {
        //     World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Dyson_Totems", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        // }

        private static readonly List<string> WorkingAvailableNames = List<string>.Create_WillNeverBeGCed(300, "DysonFactionDeepInfo-WorkingAvailableNames");
        private static string GetAvailableCityOrMoonName(GameEntity_Squad entity, Faction faction, ArcenHostOnlySimContext Context)
        {
            WorkingAvailableNames.Clear();
            int foundStructures = 0;
            if ( entity == null )
            {
                return "NullEntityThisIsBug";
            }
            if ( World_AIW2.Instance.Setup.GetBoolBySetting("BoringDysonNames") && entity.TypeData.GetHasTag("DysonStronghold") )
            {
                return entity.Planet.Name;
            }
            for (int i = 0; i < dysonCityNames.Length; i++)
            {
                bool foundMatch = false;
                foundStructures = 0;
               foreach ( GameEntity_Squad stronghold in faction.Squads( "DysonStronghold" ) )
               {
                   foundStructures++;
                   if (stronghold.GetFleetName_Safe().Contains(dysonCityNames[i]))
                   {
                       foundMatch = true;
                       break;
                   }
               }
               foreach ( GameEntity_Squad moon in faction.Squads( "DysonMoon" ) )
               {
                   foundStructures++;
                   if (moon.GetFleetName_Safe().Contains(dysonCityNames[i]))
                   {
                       foundMatch = true;
                       break;
                   }
               }

                if (foundMatch)
                    continue;
                WorkingAvailableNames.Add(dysonCityNames[i]);
            }
            if (WorkingAvailableNames.Count > 0)
                return WorkingAvailableNames[Context.RandomToUse.Next(0, WorkingAvailableNames.Count)];
            else
                return dysonCityNames[Context.RandomToUse.Next(0, dysonCityNames.Length)] + " " + foundStructures; //give us a unique new name
        }

        private BolsteringManager bolsteringManger = new BolsteringManager();
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly(ArcenHostOnlySimContext Context)
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has(ArcenTracingFlags.DysonSidekick);
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate("DysonmD-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f) : null;
            RecalculateDysonStrongholdBuildingContents_MainThreadSimOnly(Context);
            this.bolsteringManger.HandleBolstering("DysonFeedsFleet", this.BaseInfo.Strongholds.GetDisplayList(), this.BaseInfo.BolsterableFlagships.GetDisplayList(), Context);
            HandleJournalsAndTips( Context );

            RemindAboutTipsSidebarIfNecessary(Context);
            HandleRavagerAssaults(Context);
            HandlePlanetDrillingAndOverloading(Context);
            HandleSpheres(Context);
            SpawnGuardians(Context);
            HandleFlagshipUpgrades(Context);
            DropMetalGenerators(Context);
            InitializeCuendillarAsteroidsAndPlanetoids( Context);
            GiveCuendillarToPlanets( Context );
            #region Tracing
            if (tracing && !tracingBuffer.GetIsEmpty()) ArcenDebugging.ArcenDebugLogSingleLine(tracingBuffer.ToString(), Verbosity.DoNotShow);
            if (tracing)
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
        }
        private void DropMetalGenerators(ArcenHostOnlySimContext Context)
        {
            //at game start time, the dyson will get the metal generators on its home planet
            //this is not ideal, since we don't use them. So either give them to the human empire we started with, or
            //make them neutral
            GameCommand transferCommand = null;
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "MetalGenerator" ) )
            {
                if (transferCommand == null)
                {
                    transferCommand = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransferEntitiesToFaction], GameCommandSource.AnythingElse);
                }
                int destFactionId = 0;
                foreach ( GameEntity_Squad king in entity.Planet.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    if ( king.PlanetFaction.Faction.Type != FactionType.Player )
                        continue;
                    PlayerTypeData playerTypeData = king.PlanetFaction.Faction.PlayerTypeDataOrNull_ModeratelyExpensive;
                    if ( playerTypeData == null )
                        continue;
                    if ( !playerTypeData.UsesMetal )
                        continue;
                    if ( playerTypeData.GetHasTag("DoesNotGetMetalGenerators"))
                        continue;
                    if ( king.PlanetFaction.Faction == this.AttachedFaction)
                        continue;
                    destFactionId = king.PlanetFaction.Faction.FactionIndex;
                    break;
                }
                transferCommand.RelatedEntityIDs.Add(entity.PrimaryKeyID);
                transferCommand.RelatedFactionIndex = (short)destFactionId; //either neutral or a player who uses metal (this only seems to happen on homeworlds where the sidekick steals the metal generators)
            }
            if ( transferCommand != null )
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, transferCommand, false );
        }
        private void HandleFlagshipUpgrades(ArcenHostOnlySimContext Context)
        {
            List<SafeSquadWrapper> flagships = this.BaseInfo.Flagships.GetDisplayList();
            if ( World_AIW2.Instance.GameSecond % 3 != 0 )
                return;
            for (int i = 0; i < flagships.Count; i++)
            {
                GameEntity_Squad flagship = flagships[i].GetSquad();
                if (flagship == null)
                    continue;
                DysonSidekickPerUnitBaseInfo data = flagship.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                if (data == null)
                    continue;
                if ( flagship.TypeData.GetHasTag("DysonSphereFlagship")  && BaseInfo.SphereOnline )
                {
                    string tag = "";
                    if ( flagship.TypeData.GetHasTag("ZenithSphereFlagshipTierOne"))
                        tag = "ZenithSphereFlagshipTierTwo";
                    if ( flagship.TypeData.GetHasTag("SpireSphereFlagshipTierOne"))
                        tag = "SpireSphereFlagshipTierTwo";
                    if ( flagship.TypeData.GetHasTag("HQFlagshipTierOne"))
                        tag = "HQFlagshipTierTwo";
                    if ( tag == "" )
                        continue;
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, tag);
                    flagship.TransformInto(Context, entityData, 1, true);
                    continue;
                }
                if ( flagship.TypeData.KillsToTriggerTransformation <= 0 )
                    continue;
                if ( data.UnitsKilled < flagship.TypeData.KillsToTriggerTransformation)
                    continue;
                if ( flagship.TypeData.TransformAfterKills != null )
                {
                    GameEntityTypeData entityData = flagship.TypeData.TransformAfterKills;
                    flagship.TransformInto(Context, entityData, 1, true);
                }

            }
        }
        private bool CanBuildGuardian(GameEntity_Squad stronghold, int cap)
        {
            Dictionary<SafeSquadWrapper, int> guardians = BaseInfo.GuardiansPerStronghold.GetDisplayDict();
            //ArcenDebugging.LogSingleLine("How many guardians does " + stronghold.ToStringWithPlanet() + " have? We see " + guardians.GetPairCount() + " possibilities", Verbosity.DoNotShow );
            int guardCount = 0;
            foreach ( KeyValuePair<SafeSquadWrapper, int> guards in guardians )
            {
                //ArcenDebugging.LogSingleLine("Checking guardians to see if they match " + stronghold.ToStringWithPlanet() + " checking against " + guards.Key.GetSquad().ToString() , Verbosity.DoNotShow );
                if ( guards.Key.GetSquad() == stronghold)
                {
                    guardCount = guards.Value;
                    break;
                }
            }

            if ( guardCount >= cap )
                return false;
            return true;
        }
        private bool CanBuildDireGuardian(GameEntity_Squad stronghold, int cap)
        {
            Dictionary<SafeSquadWrapper, int> guardians = BaseInfo.DireGuardiansPerStronghold.GetDisplayDict();
            //ArcenDebugging.LogSingleLine("How many guardians does " + stronghold.ToStringWithPlanet() + " have? We see " + guardians.GetPairCount() + " possibilities", Verbosity.DoNotShow );
            int guardCount = 0;
            foreach ( KeyValuePair<SafeSquadWrapper, int> guards in guardians )
            {
                //ArcenDebugging.LogSingleLine("Checking guardians to see if they match " + stronghold.ToStringWithPlanet() + " checking against " + guards.Key.GetSquad().ToString() , Verbosity.DoNotShow );
                if ( guards.Key.GetSquad() == stronghold)
                {
                    guardCount = guards.Value;
                    break;
                }
            }

            if ( guardCount >= cap )
                return false;
            return true;
        }
        private void SpawnGuardians(ArcenHostOnlySimContext Context)
        {
            if (BaseInfo.Strongholds.Count == 0)
                return;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has(ArcenTracingFlags.DysonSidekick);
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate("DysonmD-SpawnGuardians-trace", 10f) : null;
            List<SafeSquadWrapper> transports = this.BaseInfo.Transports.GetDisplayList();
            foreach ( GameEntity_Squad stronghold in this.BaseInfo.GuardianProducers.DisplaySquads() )
            {
                if (stronghold == null)
                    continue;
                DysonSidekickPerUnitBaseInfo data = stronghold.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                if (data == null)
                {
                    continue;
                }
                //ArcenDebugging.LogSingleLine("checking if we can build guardians for " + stronghold.ToStringWithPlanet(), Verbosity.DoNotShow );
                if ( stronghold.GetIsCrippled())
                    continue;
                if (stronghold.TypeData.GetHasTag("GuardianIncome"))
                {
                    data.GuardianMetal += BaseInfo.Income.GuardianIncomePerSecond;
                }
                if (stronghold.TypeData.GetHasTag("DireGuardianIncome"))
                {
                    data.DireGuardianMetal += BaseInfo.Income.DireGuardianIncomePerSecond;
                }

                if (data.GuardianMetal <= 0 &&
                     data.DireGuardianMetal <= 0)
                    continue;
                if ( stronghold.TypeData.GetHasTag("MajorStronghold"))
                    data.GuardianCap = this.BaseInfo.Difficulty.MaxGuardiansPerMajor;
                else
                    data.GuardianCap = this.BaseInfo.Difficulty.MaxGuardiansPerMinor;

                if ( stronghold.TypeData.GetHasTag("MajorStronghold"))
                    data.DireGuardianCap = this.BaseInfo.Difficulty.MaxDireGuardiansPerMajor;
                else
                    data.DireGuardianCap = this.BaseInfo.Difficulty.MaxDireGuardiansPerMinor;

                foreach ( GameEntity_Squad booster in this.BaseInfo.GuardianBoosters.DisplaySquads() )
                {
                    if ( booster.Planet != stronghold.Planet )
                        continue;
                    data.GuardianCap += this.BaseInfo.Income.BoosterForGuardianCap;
                }
                foreach ( GameEntity_Squad booster in this.BaseInfo.DireGuardianBoosters.DisplaySquads() )
                {
                    if ( booster.Planet != stronghold.Planet )
                        continue;
                    data.DireGuardianCap += this.BaseInfo.Income.BoosterForDireGuardianCap;
                }

                string tag = "";
                GameEntityTypeData typeData;
                if (data.GuardianMetal > 0)
                {
                    //try to build some guardians
                    if (stronghold.TypeData.GetHasTag("GeneratesTemplarGuardians"))
                        tag = "TemplarBaseGuardian";
                    typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, tag);
                    if (typeData == null)
                        throw new Exception("Could not find guardian with tag " + tag + ".");
                    if (data.GuardianMetal >= typeData.CostForAIToPurchase)
                    {
                        data.GuardianMetal -= typeData.CostForAIToPurchase;
                        if (CanBuildGuardian(stronghold, data.GuardianCap))
                        {
                            ArcenPoint spawnLocation = stronghold.Planet.GetSafePlacementPoint_AroundEntity(Context, typeData, stronghold, FInt.FromParts(0, 005), FInt.FromParts(0, 010));
                            GameEntity_Squad guardian = this.AttachedFaction.SpawnNewUnit_ReturnNullIfMPClient(
                              Context, stronghold.Planet, spawnLocation, typeData, stronghold.CurrentMarkLevel,
                              this.AttachedFaction.LooseFleet, 0,
                              EntityBehaviorType.Attacker_Full, -1, null, "Dyson-Guardian");
                            DysonSidekickPerUnitBaseInfo newData = guardian.CreateExternalBaseInfo<DysonSidekickPerUnitBaseInfo>("DysonSidekickPerUnitBaseInfo");
                            newData.HomeStronghold = LazyLoadSquadWrapper.Create(stronghold);
                        }
                        else
                        {
                            //ArcenDebugging.LogSingleLine("Skipping building a guardian from " + stronghold.ToString() + " because we already have " + this.BaseInfo.GuardianCount.Display + "  updated metal: " + data.GuardianMetal, Verbosity.DoNotShow);
                        }
                    }
                }
                if (data.DireGuardianMetal > 0)
                {
                    tag = "";
                    if (stronghold.TypeData.GetHasTag("GeneratesTemplarDireGuardians"))
                        tag = "TemplarDireGuardian";
                    typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, tag);
                    if (typeData == null)
                        throw new Exception("Could not find dire guardian with tag " + tag + " when considering " + stronghold.ToStringWithPlanet());
                    if (data.DireGuardianMetal >= typeData.CostForAIToPurchase)
                    {
                        data.DireGuardianMetal -= typeData.CostForAIToPurchase;
                        if (CanBuildDireGuardian(stronghold, data.DireGuardianCap))
                        {
                            ArcenPoint spawnLocation = stronghold.Planet.GetSafePlacementPoint_AroundEntity(Context, typeData, stronghold, FInt.FromParts(0, 030), FInt.FromParts(0, 050));
                            GameEntity_Squad guardian = this.AttachedFaction.SpawnNewUnit_ReturnNullIfMPClient(
                              Context, stronghold.Planet, spawnLocation, typeData, stronghold.CurrentMarkLevel,
                              this.AttachedFaction.LooseFleet, 0,
                              EntityBehaviorType.Attacker_Full, -1, null, "Dyson-DireGuardian");
                            DysonSidekickPerUnitBaseInfo newData = guardian.CreateExternalBaseInfo<DysonSidekickPerUnitBaseInfo>("DysonSidekickPerUnitBaseInfo");
                            newData.HomeStronghold = LazyLoadSquadWrapper.Create(stronghold);
                        }
                    }
                }
            }
            FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
        }
        private void HandleSpheres(ArcenHostOnlySimContext Context)
        {
            if (BaseInfo.Spheres.Count == 0)
                return;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has(ArcenTracingFlags.DysonSidekick);
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate("DysonmD-HandleSphere-trace", 10f) : null;
            int interval = 5;
            List<SafeSquadWrapper> spheres = this.BaseInfo.Spheres.GetDisplayList();
            List<SafeSquadWrapper> flagships = this.BaseInfo.Flagships.GetDisplayList();
            if (World_AIW2.Instance.GameSecond % interval == 0)
            {
                for (int i = 0; i < spheres.Count; i++)
                {
                    GameEntity_Squad sphere = spheres[i].GetSquad();
                    if (sphere == null)
                        continue;

                    //this can happen sometimes when regenerating, and I'm not sure
                    //if this is a safe assumption to make for all squads
                    if ( sphere.HullPointsLost <= 0 &&
                         sphere.SecondsSpentAsRemains > 0 )
                        sphere.SecondsSpentAsRemains = -1;
                    
                    if (BaseInfo.SphereOnline && !sphere.GetIsCrippled())
                    {
                        //Ship spawning for the win condition ( if applicable )
                        GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "DysonSphereVictoryProduction");
                        GameEntity_Squad entity = this.AttachedFaction.SpawnNewUnit_ReturnNullIfMPClient(
                          Context, sphere.Planet, sphere.WorldLocation, typeData, 1,
                          this.AttachedFaction.LooseFleet, 0,
                          EntityBehaviorType.Attacker_Full, -1, null, "SphereProduction" );
                    }
                }
            }

            if ( BaseInfo.SphereOnline )
            {
                //we're done, no need to spawn nasty reapers or anything
                FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
                return;
            }
            bool triggerReaperAttack = false;
            bool triggerAIAttack = false;
            //Handle the Reaper response for the sphere win condition
            for (int i = 0; i < spheres.Count; i++)
            {
                GameEntity_Squad sphere = spheres[i].GetSquad();
                if (sphere == null)
                    continue;
                if ( sphere.CurrentMarkLevel < 7 )
                    continue; //only triggers for mark level 7, which is the endgame fight
                if (sphere.SelfBuildingMetalRemaining > 0)
                    continue;

                DysonSidekickPerUnitBaseInfo data = sphere.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                if (i == 0)
                {
                    triggerAIAttack = (sphere.GetSecondsSinceCreation() % this.BaseInfo.Difficulty.SphereWinAttackIntervalForAI == 0);
                    triggerReaperAttack = (sphere.GetSecondsSinceCreation() % this.BaseInfo.Difficulty.SphereWinAttackIntervalForReapers == 0);
                }

                if (BaseInfo.SphereActivationTriggered == false)
                {
                    BaseInfo.SphereActivationTriggered = true;
                    Faction reaperFaction = FactionUtilityMethods.Instance.GetReapersFaction();
                    AllegianceHelper.AllyThisFactionToAI(reaperFaction);
                    triggerAIAttack = true;
                    triggerReaperAttack = true;
                    World_AIW2.Instance.QueueChatMessageOrCommand( "The AI and Reapers are allying against you",
                        ChatType.LogToCentralChat, "", null );
                }


                if (data.TimeTillDysonSphereWin == -1)
                    data.TimeTillDysonSphereWin = BaseInfo.Difficulty.TimeForSphereWin;
                if (data.TimeTillDysonSphereWin > 0)
                    data.TimeTillDysonSphereWin--;
                if (data.TimeTillDysonSphereWin == 0)
                {
                    BaseInfo.SphereOnline = true;
                    DoSphereTransformation(sphere, Context);
                }
            }
            if (triggerReaperAttack)
            {
                int strength = FactionUtilityMethods.Instance.GetCurrentAIP().IntValue * this.BaseInfo.Difficulty.SphereWinResponsePerAIPForReapers;
                TriggerAttack(strength, 2, Context, true);
            }
            if (triggerAIAttack)
            {
                int strength = FactionUtilityMethods.Instance.GetCurrentAIP().IntValue * this.BaseInfo.Difficulty.SphereWinResponsePerAIPForAI;
                ExoTargets.Clear();
                //The AI will target spheres and human home command stations
                ExoGalacticAttackManager.GetAllHumanHomeCommandStations(ExoTargets);
                for (int j = 0; j < spheres.Count; j++)
                {
                    GameEntity_Squad sphereToAttack = spheres[j].GetSquad();
                    if (sphereToAttack == null)
                        continue;
                    if ( sphereToAttack.GetIsCrippled())
                        continue;
                    ExoTargets.Add(sphereToAttack);
                }

                ExoOptions options = ExoOptions.CreateWithDefaults( ExoTargets, strength, null, AttachedFaction );
                options.exoText = "The AI is launching an attack against the Dyson.";
                ExoGalacticAttackManager.SendExoGalacticAttack( options, Context );
            }

            if (World_AIW2.Instance.GameSecond % 10 == 0)
            {
                foreach ( GameEntity_Squad otherEntity in World_AIW2.Instance.Squads( EntityRollupType.ReinforcementLocations ) )
                    FactionUtilityMethods.Instance.TryDeployReinforcementContents( otherEntity, Context );
            }
            FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
        }

        private void DoSphereTransformation( GameEntity_Squad sphere, ArcenHostOnlySimContext Context )
        {
            string tag = "";

            if ( sphere.TypeData.InternalName == "DysonSidekickZenithSphere")
                tag = "DysonSidekickZenithSphereOnline";
            if ( sphere.TypeData.InternalName == "DysonSidekickSpireSphere")
                tag = "DysonSidekickSpireSphereOnline";
            if ( tag == "" )
                return;
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( tag);
            sphere.TransformInto(Context, entityData, 1, true);

        }
        private bool DoesPlanetHaveDrill( Planet planet )
        {
            List<SafeSquadWrapper> drillOrOverloader = this.BaseInfo.DrillsAndOverloaders.GetDisplayList();
            for (int i = 0; i < drillOrOverloader.Count; i++)
            {
                GameEntity_Squad drill = drillOrOverloader[i].GetSquad();
                if (drill == null)
                    continue;
                if ( drill.Planet == planet )
                    return true;
            }
            return false;
        }
        private void HandlePlanetDrillingAndOverloading(ArcenHostOnlySimContext Context)
        {
            if ( BaseInfo.DrillsAndOverloaders.Count == 0 )
                return;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has(ArcenTracingFlags.DysonSidekick);
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate("DysonmD-HandlePlanetDrillingAndOverloading-trace", 10f) : null;
            int debugCode = 0;
            try
            {
                debugCode = 100;
                List<SafeSquadWrapper> drillOrOverloader = this.BaseInfo.DrillsAndOverloaders.GetDisplayList();
                for (int i = 0; i < drillOrOverloader.Count; i++)
                {
                    debugCode = 200;
                    GameEntity_Squad drill = drillOrOverloader[i].GetSquad();
                    if (drill == null)
                        continue;
                    if (World_AIW2.Instance.GameSecond % 15 == 0)
                    {
                        debugCode = 300;
                        //check if we built a duplicate drill, this can happen if you click quickly
                        for (int j = i; j < drillOrOverloader.Count; j++)
                        {
                            if (j == i)
                                continue;
                            GameEntity_Squad secondary = drillOrOverloader[j].GetSquad();
                            if (secondary == null)
                                continue;
                            if (secondary.Planet == drill.Planet)
                            {
                                //we will process this under "secondary" later in this iteration
                                drill.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut);
                                continue;
                            }
                        }
                    }
                    debugCode = 400;
                    DysonSidekickPerUnitBaseInfo data = drill.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                    if (data == null)
                        continue;
                    if (drill.TypeData.GetHasTag("DysonAsteroidDrill") &&
                         FactionUtilityMethods.Instance.GetReaperChrysalisOnPlanetOrNull(drill.Planet) != null)
                        data.DrillingChrysalis = true; //if this is an asteroid drill on a chrysalis planet, we are doing a chrysalis

                    if (World_AIW2.Instance.GameSecond % 10 == 0)
                        drill.FlagAsNeedingFullSyncCheckIfInMultiplayerAndWeAreHost(); //Make sure we sync drills pretty regularly
                    debugCode = 500;
                    
                    if ( !drill.TypeData.GetHasTag("DysonOverloader"))
                    {
                        if (data.TimeForNextTransport == -1)
                        {
                            if (drill.TypeData.GetHasTag("DysonAsteroidDrill"))
                            {
                                if (data.DrillingChrysalis)
                                    data.TimeForNextTransport = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.ChrysalisTransportSpawnInterval;
                                else
                                    data.TimeForNextTransport = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.AsteroidTransportSpawnInterval;
                            }
                            else
                                data.TimeForNextTransport = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.PlanetTransportSpawnInterval;
                        }
                        if (data.TimeForNextTransport <= World_AIW2.Instance.GameSecond)
                        {
                            //spawn new transport
                            int cuendillarExtracted = SpawnTransportAndUpdateCuendillar(drill, Context);
                            data.TransportsSent++;
                            data.TotalCuendillarDrilled += cuendillarExtracted;
                            if (drill.TypeData.GetHasTag("DysonAsteroidDrill"))
                            {
                                if (data.DrillingChrysalis)
                                    data.TimeForNextTransport = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.ChrysalisTransportSpawnInterval;
                                else
                                    data.TimeForNextTransport = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.AsteroidTransportSpawnInterval;
                            }
                            else
                                data.TimeForNextTransport = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.PlanetTransportSpawnInterval;
                        }
                    }
                    else
                        data.TimeForNextTransport = -1; //we are an an overloader
                    debugCode = 600;

                    int responseStrength = 0;
                    int responseInterval = 0;
                    bool overloadsPlanet = false;
                    bool ravagesPlanet = false;

                    if (drill.TypeData.GetHasTag("DysonOverloader"))
                    {
                        //Note that the attack response starts once the drill is placed, not necessarily once it has finished being built
                        responseStrength = FactionUtilityMethods.Instance.GetCurrentAIP().IntValue * this.BaseInfo.Difficulty.OverloadResponsePerAIP;
                        responseInterval = this.BaseInfo.Difficulty.OverloadAttackInterval;
                        overloadsPlanet = true;
                    }
                    else if (drill.TypeData.GetHasTag("DysonPlanetaryDrill"))
                    {
                        responseStrength = FactionUtilityMethods.Instance.GetCurrentAIP().IntValue * this.BaseInfo.Difficulty.DrillResponsePerAIP;
                        responseStrength += data.TransportsSent * this.BaseInfo.Difficulty.DrillResponseIncreasePerTransport;
                        responseInterval = this.BaseInfo.Difficulty.DrillAttackInterval;
                        ravagesPlanet = true;
                    }
                    else
                    {
                        //asteroid drill
                        responseStrength = FactionUtilityMethods.Instance.GetCurrentAIP().IntValue * this.BaseInfo.Difficulty.DrillResponsePerAIP;
                        responseStrength += data.TransportsSent * this.BaseInfo.Difficulty.DrillResponseIncreasePerTransport;
                        responseInterval = this.BaseInfo.Difficulty.DrillAttackInterval;
                    }
                    debugCode = 700;
                    if (drill.GetSecondsSinceCreation() % responseInterval == 0 )
                    {
                        debugCode = 800;
                        if (tracing)
                            tracingBuffer.Add("Trigger reaper attack in response to drilling from " + drill.ToStringWithPlanet() + "\n");
                        TriggerAttack(drill.Planet, responseStrength, Context, false);
                        //Also trigger attacks against any Transports en route; we have a minor strike against all the
                        //planets with transports, but another full size strike against the planet with the fewest of our enemies, to maximize our chances
                        //of killing a transport
                        List<SafeSquadWrapper> transports = this.BaseInfo.Transports.GetDisplayList();
                        int bestTargetStrength = 0;
                        Planet bestTargetPlanet = null;
                        for (int j = 0; j < transports.Count; j++)
                        {
                            PlanetFaction pFaction = transports[j].PlanetFaction;
                            if (bestTargetStrength == 0 || pFaction.DataByStance[FactionStance.Hostile].TotalStrength < bestTargetStrength)
                            {
                                bestTargetStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                                bestTargetPlanet = transports[j].Planet;
                            }
                            TriggerAttack(transports[j].Planet, responseStrength / 10, Context, false);
                        }
                        if (bestTargetPlanet != null)
                        {

                            TriggerAttack(bestTargetPlanet, responseStrength, Context, false);
                        }
                    }
                    debugCode = 900;
                    if (drill.SelfBuildingMetalRemaining > 0)
                        continue; //don't update the time-till-done-drilling until we have finished building the drill
                    if (overloadsPlanet)
                    {
                        debugCode = 1000;
                        if (data.TimeTillPlanetOverloaded == -1)
                        {
                            data.TimeTillPlanetOverloaded = this.BaseInfo.Difficulty.PlanetOverloadTime;
                            continue;
                        }
                        data.TimeTillPlanetOverloaded--;
                        {
                            int totalAIP = this.BaseInfo.Difficulty.AIPForPlanetOverloading;
                            int totalTime = this.BaseInfo.Difficulty.PlanetOverloadTime;
                            if ( totalTime > 0 )
                            {
                                int elapsed = totalTime - data.TimeTillPlanetOverloaded;
                                int toGrant = elapsed * totalAIP / totalTime - data.AIPAlreadyGranted;
                                if ( toGrant > 0 )
                                {
                                    GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt)toGrant, AIPChangeReason.PlanetDrilling, drill.TypeData, this.AttachedFaction.FactionIndex, drill.Planet.Index, drill.GetFactionIndex_Safe() );
                                    data.AIPAlreadyGranted += toGrant;
                                }
                            }
                        }
                        if (data.TimeTillPlanetOverloaded < 30)
                        {
                            drill.Planet.ShowPlanetAsSufferingNearCollapseUntilGameSecond = World_AIW2.Instance.GameSecond + data.TimeTillPlanetOverloaded;
                        }
                        if (data.TimeTillPlanetOverloaded <= 0)
                        {
                            drill.Planet.IsPlanetToBeDestroyed = true;
                            drill.Planet.Network_HostOnly_NeedToSyncWormholesToClients = true;
                            drill.Planet.Network_HostOnly_NeedToSyncPlanetPositionToClients = true;
                            World_AIW2.Instance.QueueChatMessageOrCommand(this.AttachedFaction.StartFactionColourForLog() + "Dyson Sidekick</color> has destroyed  " + drill.GetPlanetName_Safe() + ".",
                                                                           ChatType.LogToCentralChat, null);
                            int overloadRemaining = this.BaseInfo.Difficulty.AIPForPlanetOverloading - data.AIPAlreadyGranted;
                            if ( overloadRemaining > 0 )
                                GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt)overloadRemaining, AIPChangeReason.PlanetDrilling, drill.TypeData, this.AttachedFaction.FactionIndex, drill.Planet.Index, drill.GetFactionIndex_Safe() );
                            drill.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut);
                        }
                    }
                    else if (ravagesPlanet)
                    {
                        debugCode = 1200;
                        int burningVisualsTime = DysonSidekickFactionBaseInfo.PlanetBurningTime;
                        if (data.TimeTillPlanetDrilled == -1)
                        {
                            data.TimeTillPlanetDrilled = this.BaseInfo.CalculateDrillTime(drill);
                            continue;
                        }
                        data.TimeTillPlanetDrilled--;
                        {
                            int totalAIP = this.BaseInfo.Difficulty.AIPForPlanetDrilling;
                            int totalTime = data.TimeTillPlanetDrilled + drill.GetSecondsSinceCreation();
                            if ( totalTime > 0 )
                            {
                                int elapsed = totalTime - data.TimeTillPlanetDrilled;
                                int toGrant = elapsed * totalAIP / totalTime - data.AIPAlreadyGranted;
                                if ( toGrant > 0 )
                                {
                                    GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt)toGrant, AIPChangeReason.PlanetDrilling, drill.TypeData, this.AttachedFaction.FactionIndex, drill.Planet.Index, drill.GetFactionIndex_Safe() );
                                    data.AIPAlreadyGranted += toGrant;
                                }
                            }
                        }
                        if (data.TimeTillPlanetDrilled < burningVisualsTime)
                        {
                            //the burning visuals start once all the cuendillar has been mined
                            drill.Planet.ShowPlanetAsSufferingNearCollapseUntilGameSecond = World_AIW2.Instance.GameSecond + data.TimeTillPlanetDrilled;
                        }

                        if (data.TimeTillPlanetDrilled <= 0)
                        {
                            foreach ( GameEntity_Squad generator in drill.Planet.Squads( "MetalGenerator" ) )
                            {
                                generator.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut);
                            }
                            drill.Planet.NonSim_ShouldSetRavagedIfHostAndNoMetalHarvesters = true;
                            drill.Planet.IsRavaged = true;
                            drill.Planet.Network_HostOnly_NeedToSyncWormholesToClients = true;
                            drill.Planet.Network_HostOnly_NeedToSyncPlanetPositionToClients = true;
                            foreach ( GameEntity_Squad entity in drill.Planet.Squads() )
                            {
                                //Destroy all non-flagship, non-mark7 structures
                                if (entity.PlanetFaction.Faction.Type == FactionType.Player)
                                {
                                    Fleet fleet = entity.FleetMembership.Fleet;
                                    GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                                    if (centerpiece == entity)
                                        continue;
                                }
                                if (entity.CurrentMarkLevel >= 7)
                                    continue;

                                entity.DoOnDeathInCombatLogic_OnlyAferFullStackDeath(null, DamageSource.SomeSortOfEnemy, 0, Context);
                                entity.Despawn(Context, true, InstancedRendererDeactivationReason.SelfDestructOnFiring);
                            }

                            this.BaseInfo.PlanetsDrilled++;
                            int drillRemaining = this.BaseInfo.Difficulty.AIPForPlanetDrilling - data.AIPAlreadyGranted;
                            if ( drillRemaining > 0 )
                                GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt)drillRemaining, AIPChangeReason.PlanetDrilling, drill.TypeData, this.AttachedFaction.FactionIndex, drill.Planet.Index, drill.GetFactionIndex_Safe() );

                            World_AIW2.Instance.QueueChatMessageOrCommand(this.AttachedFaction.StartFactionColourForLog() + "Dyson Sidekick</color> has finished mining  " + drill.GetPlanetName_Safe() + ".",
                                                                           ChatType.LogToCentralChat, null);
                            if (this.BaseInfo.RavagedPlanets.Count == 0)
                            {
                                if (tracing)
                                    tracingBuffer.Add("Triggering a small Reaper attack after the first planet was ravaged\n");
                                TriggerAttack(drill.Planet, 2 * 1000, Context, false); //trigger a small attack after ravaging the first planet
                            }

                            drill.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut);
                        }
                    }
                    else
                    {
                        debugCode = 1200;
                        if (data.TimeTillPlanetDrilled == -1)
                        {
                            data.TimeTillPlanetDrilled = this.BaseInfo.CalculateDrillTime(drill);
                            continue;
                        }

                        if (data.DrillingChrysalis && FactionUtilityMethods.Instance.GetReaperChrysalisOnPlanetOrNull(drill.Planet) == null)
                        {
                            //the chrysalis is gone; we can go away
                            drill.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut);
                            continue;
                        }
                        data.TimeTillPlanetDrilled--;
                        if (data.TimeTillPlanetDrilled <= 0)
                        {
                            if (FactionUtilityMethods.Instance.GetReaperChrysalisOnPlanetOrNull(drill.Planet) != null)
                            {
                                World_AIW2.Instance.QueueChatMessageOrCommand(this.AttachedFaction.StartFactionColourForLog() + "Dyson Sidekick</color> has finished mining a chrysalis on  " + drill.GetPlanetName_Safe() + ".",
                                                                              ChatType.LogToCentralChat, null);
                                //The chrysalis will self destruct in the reaper code
                            }
                            else
                            {
                                World_AIW2.Instance.QueueChatMessageOrCommand(this.AttachedFaction.StartFactionColourForLog() + "Dyson Sidekick</color> has finished mining an asteroid on  " + drill.GetPlanetName_Safe() + ".",
                                                                              ChatType.LogToCentralChat, null);
                                FactionUtilityMethods.Instance.DespawnCuendillarAsteroidOnPlanet(drill.Planet, Context);
                            }
                            drill.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut);
                        }
                    }
                }
            }catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in HandlePlanetDrillingAndOverloading debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
        }
        private int SpawnTransportAndUpdateCuendillar( GameEntity_Squad drill, ArcenHostOnlySimContext Context)
        {
            GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DysonSidekickTransport" );
            if ( typeData == null )
                throw new Exception("Could not find transport tag");
            ArcenPoint spawnLocation = drill.Planet.GetSafePlacementPoint_AroundEntity( Context, typeData, drill, FInt.FromParts( 0, 025 ), FInt.FromParts( 0, 150 ));
            GameEntity_Squad asteroid = null;
            GameEntity_Squad chrysalis = null;
            if ( drill.TypeData.GetHasTag("DysonAsteroidDrill"))
            {
                chrysalis = FactionUtilityMethods.Instance.GetReaperChrysalisOnPlanetOrNull( drill.Planet );
                asteroid = FactionUtilityMethods.Instance.GetAsteroidOnPlanetOrNull( drill.Planet );
                if ( asteroid == null )
                    asteroid = FactionUtilityMethods.Instance.GetPlanetoidOnPlanetOrNull( drill.Planet );
                if (asteroid == null && chrysalis == null)
                {
                    ArcenDebugging.LogSingleLine("Somehow there's no asteroid or chrysalis on " + drill.Planet.Name, Verbosity.DoNotShow);
                    return 0;
                }
                //Chrysalises can't spawn on planets with asteroids
                if ( chrysalis != null )
                    spawnLocation = chrysalis.Planet.GetSafePlacementPoint_AroundEntity( Context, typeData, asteroid, FInt.FromParts( 0, 025 ), FInt.FromParts( 0, 150 ));
                else
                    spawnLocation = asteroid.Planet.GetSafePlacementPoint_AroundEntity( Context, typeData, asteroid, FInt.FromParts( 0, 025 ), FInt.FromParts( 0, 150 ));
            }
            GameEntity_Squad transport = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( drill.PlanetFaction, typeData, 1,
                    null, 0, spawnLocation, Context, "Dyson-NewTransport" );
            if ( transport == null )
                return 0;
            DysonSidekickPerUnitBaseInfo data = transport.CreateExternalBaseInfo<DysonSidekickPerUnitBaseInfo>( "DysonSidekickPerUnitBaseInfo" );

            int amountMined = 0;
            if ( drill.TypeData.GetHasTag("DysonAsteroidDrill") )
            {
                if (chrysalis != null)
                {
                    ReapersPerUnitBaseInfo chrData = chrysalis.TryGetExternalBaseInfoAs<ReapersPerUnitBaseInfo>();
                    amountMined = Math.Min(chrData.CuendillarRemaining, BaseInfo.Difficulty.CuendillarMinedPerTransport);
                    chrData.CuendillarRemaining -= amountMined;
                }
                else
                {
                    DysonSidekickPerUnitBaseInfo astData = asteroid.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                    amountMined = Math.Min(astData.CuendillarRemaining, BaseInfo.Difficulty.CuendillarMinedPerTransport);
                    astData.CuendillarRemaining -= amountMined;
                }
            }
            else
            {
                Planet planet = drill.Planet;
                amountMined = Math.Min((int)planet.ResourceOneRemainingForAnyPlayer, BaseInfo.Difficulty.CuendillarMinedPerTransport);
                planet.ResourceOneRemainingForAnyPlayer -= amountMined;
            }
            data.CuendillarTransported = amountMined;
            return amountMined;
        }
        public static void SpawnRavagerForces( GameEntity_Squad ravager, ArcenHostOnlySimContext Context, bool fireteamShips, DysonSidekickDifficulty Difficulty )
        {
            Faction reaperFaction = FactionUtilityMethods.Instance.GetReapersFaction(); 
            if ( reaperFaction == null )
                throw new Exception("No reapers faction found");
            
            AttackCompositionForRavagerTroops.Clear();
            string tag = "UndeadRavagerTroopsTierOne";
            if (FactionUtilityMethods.Instance.GetCurrentAIP().IntValue < Difficulty.AIPForReaperTierOne)
            {
                tag = "UndeadRavagerTroopsTierOne";
                if ( fireteamShips )
                    tag = "GatewaySpawnTierOne";
            }
            else if (FactionUtilityMethods.Instance.GetCurrentAIP().IntValue < Difficulty.AIPForReaperTierTwo)
            {
                tag = "UndeadRavagerTroopsTierTwo";
                if ( fireteamShips )
                    tag = "GatewaySpawnTierTwo";

            }
            else if (FactionUtilityMethods.Instance.GetCurrentAIP().IntValue < Difficulty.AIPForReaperTierThree)
            {
                tag = "UndeadRavagerTroopsTierThree";
                if ( fireteamShips )
                    tag = "GatewaySpawnTierThree";

            }

            int strength = (Difficulty.ImmobileRavagerTroopSpawnStrengthPerAIP * FactionUtilityMethods.Instance.GetCurrentAIP()).IntValue;
            int origStr = strength;
            if ( FactionUtilityMethods.Instance.AnyDysonSidekickFactions())
            {
                Faction dysonSidekickFaction = FactionUtilityMethods.Instance.GetDysonSidekickFaction();
                if ( dysonSidekickFaction == null )
                    throw new Exception("No dyson faction found");
                DysonSidekickFactionBaseInfo dBaseInfo = dysonSidekickFaction.GetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
                if ( dBaseInfo.SphereOnline )
                {
                    strength /= 20;
                    tag = "UndeadRavagerTroopsTierOne";
                }
            }


            while ( strength > 0 )
            {
                GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, tag );
                if ( typeData == null )
                    throw new Exception("Could not find entity with tag " + tag + " in the SpawnRavagerForces code path");
                AttackCompositionForRavagerTroops[typeData]++;
                strength -= typeData.CostForAIToPurchase;
            }
            bool debug = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has(ArcenTracingFlags.DysonSidekick);
            if (debug)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Spawned troops for " + ravager.ToStringWithPlanet() + " strength " + origStr +" (" + Difficulty.ImmobileRavagerTroopSpawnStrengthPerAIP + " * " + FactionUtilityMethods.Instance.GetCurrentAIP() +")",  Verbosity.DoNotShow);

            }
            foreach ( KeyValuePair<GameEntityTypeData, int> pair in AttackCompositionForRavagerTroops )
            {
                int totalSquadsToSpawn = pair.Value;
                GameEntityTypeData entityType = pair.Key;
                int numStacksPerSquad = 0;
                int separateSquadsToSpawn = totalSquadsToSpawn;
                int remainder = 0;
                int StackingCutoff = AIWar2GalaxySettingQuickAccess.StackingCutoffNPCs;
                // if ( tracing )
                //     tracingBuffer.Add("\tSpawning " + totalSquadsToSpawn + " of " + pair.Key.GetDisplayName() + "\n" );

                if ( StackingCutoff <= 0 )
                    throw new Exception( "Undefined StackingCutoffNPCs; this means that waves won't spawn" );
                if ( totalSquadsToSpawn > StackingCutoff )
                {
                    separateSquadsToSpawn = StackingCutoff;
                    numStacksPerSquad = totalSquadsToSpawn / separateSquadsToSpawn;
                    remainder = totalSquadsToSpawn % separateSquadsToSpawn;
                }
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("\tSpawning " + totalSquadsToSpawn + " total (" + separateSquadsToSpawn + " separate) of " + pair.Key.GetDisplayName(), Verbosity.DoNotShow );

                for ( int j = 0; j < separateSquadsToSpawn; j++ )
                {
            
                    FInt minRadius = FInt.FromParts( 0, 050 );
                    FInt maxRadius = FInt.FromParts( 0, 120 );
                    ArcenPoint spawnLocation = ravager.Planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, entityType, ravager.WorldLocation, minRadius, maxRadius );
                    byte markLevel = (byte)(FactionUtilityMethods.Instance.GetCurrentAIP() / Difficulty.AIPForReaperMarkLevel);
                    if ( markLevel < 1 )
                        markLevel = 1;
                    if ( markLevel > 7 )
                        markLevel = 7;
                    GameEntity_Squad entity = reaperFaction.SpawnNewUnit_ReturnNullIfMPClient(
                        Context, ravager.Planet, spawnLocation, entityType, markLevel,
                        reaperFaction.LooseFleet, 0,
                        EntityBehaviorType.Attacker_Full, -1, null, "ReaperAttackDeployment" );

                    if (entity == null)
                    {
                        continue;
                    }
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
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\tWe have now spawned " + entity.ToStringWithPlanetAndOwner() , Verbosity.DoNotShow );

                }
            }
        }
        private void HandleRavagerAssaults( ArcenHostOnlySimContext Context )
        {
            if ( World_AIW2.Instance.GameSecond < BaseInfo.TimeForNextEnemyAttack )
                return;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has(ArcenTracingFlags.DysonSidekick);
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate("DysonmD-HandleRavagerAssaults-trace", 10f) : null;
            bool verboseDebug = false;
            if ( this.BaseInfo.RavagedPlanets.Count == 0 && FactionUtilityMethods.Instance.GetCurrentAIP() < 50 )
            {
                if ( tracing && verboseDebug)
                    tracingBuffer.Add("HandleRavagerAssaults: no ravaged planets/low AIP, early bail out \n" );
                BaseInfo.StrengthForNextEnemyAttack = 0; //reset
                BaseInfo.TimeForNextEnemyAttack = 0;
                FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
                return;
            }
            int update = this.BaseInfo.Difficulty.MinAttackInterval + Context.RandomToUse.Next( 0, this.BaseInfo.Difficulty.MaxAttackInterval - this.BaseInfo.Difficulty.MinAttackInterval ); //randomly in the next 20 minutes (but at least 8 minutes from now)
            BaseInfo.TimeForNextEnemyAttack = World_AIW2.Instance.GameSecond + update;
            
            int strengthToUse = BaseInfo.StrengthForNextEnemyAttack;
            if (strengthToUse < BaseInfo.Difficulty.MinAttackStrength)
                strengthToUse = this.BaseInfo.Difficulty.MinAttackStrength;

            if ( tracing )
                tracingBuffer.Add("HandleRavagerAssaults: calling TriggerAttack with strength " + strengthToUse + ". Next wave will be at " + BaseInfo.TimeForNextEnemyAttack + " (update: " + update).Add("\n");
            bool launched = TriggerAttack(strengthToUse, -1, Context, true);
            if ( launched )
                BaseInfo.StrengthForNextEnemyAttack = 0; //reset
            World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DS_Dyson_Enemies", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
        }
        public bool TriggerAttack( int Strength, int numPlanetsToSpawnOn, ArcenHostOnlySimContext Context, bool canRavagersSpawn )
        {
            int debugCode = 0;
            try{
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has(ArcenTracingFlags.DysonSidekick);
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate("DysonSidekick-TriggerAttack-trace", 10f) : null;
            debugCode = 100;
            WorkingPlanetList.Clear();
            if ( numPlanetsToSpawnOn == -1 )
            {
                numPlanetsToSpawnOn = 1 + this.BaseInfo.Strongholds.Count / 5;
            }
            debugCode = 200;
            int strengthToUsePerPlanet = Strength / numPlanetsToSpawnOn;
            if ( tracing )
                tracingBuffer.Add("TriggerAttack: trigger a reaper attack against " + numPlanetsToSpawnOn + " planets of strength " + Strength + " (" + strengthToUsePerPlanet + " per planet). ravaged planets (out of " + BaseInfo.RavagedPlanets.Count +" ravaged planets). Ravagers can spawn? " + canRavagersSpawn + "\n");
            GetPlanetsForRavagerAssaults( numPlanetsToSpawnOn, WorkingPlanetList, Context);
            debugCode = 300;
            for (int i = 0; i < numPlanetsToSpawnOn; i++)
            {
                debugCode = 400;
                Planet target = WorkingPlanetList[Context.RandomToUse.Next(0, WorkingPlanetList.Count)];
                bool spawnRavager = false;
                if (i == 0)
                {
                    spawnRavager = true;
                    if ( tracing )
                        tracingBuffer.Add("\tWe get a ravager on " + target.Name +"\n" );

                }
                debugCode = 500;
                TriggerAttack(target, strengthToUsePerPlanet, Context, spawnRavager );
            }
            FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
            }catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in Trigger Attack A debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            return true;
        }
        private void GetPlanetsForRavagerAssaults( int minToFill, List<Planet> ListToFill, ArcenHostOnlySimContext Context )
        {
            ListToFill.Clear();
            int strengthMin = 5000;
            int strengthMinIncrease = 5000;
            while ( ListToFill.Count < minToFill )
            {
                foreach ( Planet planet in World_AIW2.Instance.CurrentGalaxy.Planets( false ) )
                {
                    if ( ListToFill.Contains( planet ))
                        continue;
                    if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                        continue;
                    if ( planet.PopulationType == PlanetPopulationType.HumanHomeworld )
                        continue;
                    if ( planet.PopulationType == PlanetPopulationType.AIHomeworld )
                        continue;
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( this.AttachedFaction );
                    int hostileStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                    int selfStrength = pFaction.DataByStance[FactionStance.Self].TotalStrength;
                    int friendlyStrength = pFaction.DataByStance[FactionStance.Friendly].TotalStrength;
                    int totalStrength = hostileStrength + friendlyStrength + selfStrength;
                    if ( totalStrength < strengthMin )
                        ListToFill.Add( planet );
                }
                strengthMin += strengthMinIncrease;
            }
        }
        public bool TriggerAttack( Planet planet, int Strength, ArcenHostOnlySimContext Context, bool ShouldSpawnRavager )
        {
            ReapersFactionDeepInfo.TriggerRavagerOrUndeadAttack( Strength, ShouldSpawnRavager, planet, BaseInfo.Difficulty, Context, AttackCompositionForRavagerTroops);
            return true;
        }

        public void HandleJournalsAndTips( ArcenHostOnlySimContext Context )
        {
            //Some entries are time-related
            if ( World_AIW2.Instance.GameSecond == 1 && GameSettings.Current.GetBoolBySetting( "DysonTipReminders" ) )
            {
                if ( World_AIW2.Instance.CampaignType.HarshnessRating > 100 )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "戴森概览",
                        "停止！戴森是一种与人类帝国完全不同的游戏风格。你必须重新学习技能并改变你的期望。\n\n阅读！左侧菜单的'提示'标签中有大量玩法和建议。\n\n享受！由于这是一个全新的游戏体验，请将自己视为不再是AI War 2中的玩家。\n\n警告！戴森在任何高于'人类至上'的游戏模式中都不被真正支持。它不应该崩溃，但可能会出现意外问题。风险自负。", "确定" );
                }
                else
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "戴森概览",
                        "停止！戴森是一种与人类帝国完全不同的游戏风格。你必须重新学习技能并改变你的期望。\n\n阅读！左侧菜单的'提示'标签中有大量玩法和建议。\n\n享受！由于这是一个全新的游戏体验，请将自己视为不再是AI War 2中的玩家。", "确定" );
                }
            }

            //others are related to gameplay triggers we detect here
            if ( this.BaseInfo.Strongholds.Count >= 3 && this.BaseInfo.NumShipyards < 2 )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DS_Dyson_ShipyardsAreCritical", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );

            if ( World_AIW2.Instance.GameSecond > 30 &&
                World_AIW2.Instance.GameSecond % 65 == 0 )
            {
                //Check if we have unused stronghold modules, and give a log if we do
                //We could also check flagships, but I'm concerned about the player who haven't used modules at all
                bool foundUnusedModules = false;
                foreach ( GameEntity_Squad city in this.BaseInfo.Strongholds.DisplaySquads() )
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
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DS_Dyson_SpendModules", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
            //TODO: checks for 'have we invested in totem tech' and 'have we invested in tower defense tech
        }

        public void RemindAboutTipsSidebarIfNecessary( ArcenHostOnlySimContext Context )
        {
            // //Every 5 minutes, remind the player about the Tips sidebar if they haven't looked at any Tips
            // int reminderInterval = 300;
            // if ( ! GameSettings.Current.GetBoolBySetting( "DysonTipReminders" ) )
            // {
            //     return;
            // }
            // if ( World_AIW2.Instance.GameSecond % reminderInterval != 0 )
            //     return;
            // PlayerAccount localAccount = PlayerAccount.Local;
            // if ( localAccount == null )
            //     return;
            // bool foundOpenedTip = false;
            // for ( int i = World_AIW2.Instance.JournalHistory.Count - 1; i >= 0; i-- )
            // {
            //     JournalEntryInCampaign entry = World_AIW2.Instance.JournalHistory[i];
            //     if ( !entry.IsTipRatherThanJournalEntry )
            //         continue;
            //     if ( entry.HasBeenViewedByPlayerAccountIDs.ContainsKey ( localAccount.PlayerPrimaryKeyID ) )
            //     {
            //         foundOpenedTip = true;
            //         break;
            //     }
            // }
            // if ( !foundOpenedTip )
            // {
            //     World_AIW2.Instance.QueueChatMessageOrCommand( "My Liege, there is new advice for your governing your empire in the Tips sidebar.",
            //                                                    ChatType.LogToCentralChat, "", null );
            // }
        }

        #region RecalculateDysonStrongholdBuildingContents_MainThreadSimOnly
        public void RecalculateDysonStrongholdBuildingContents_MainThreadSimOnly( ArcenHostOnlySimContext Context )
        { 
            int debugCode = 0;
            bool debug = false;
            try
            {
                List<GameEntityTypeData> dysonStrongholdBuildings = GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( "DysonBuildMenu" );
                if ( dysonStrongholdBuildings == null || dysonStrongholdBuildings.Count <= 0 )
                    return;
                debugCode = 100;
                foreach ( GameEntity_Squad city in this.BaseInfo.Strongholds.DisplaySquads() )
                {
                    debugCode = 200;
                    if ( city == null || city.FleetMembership == null )
                        continue;
                    debugCode = 300;
                    Fleet strongholdCityFleet = city.GetFleetOrNull_Safe();
                    if ( strongholdCityFleet == null ) {
                        continue;
                    }

                    debugCode = 350;
                    if ( city.GetIsCrippled() || city.GetIsNonFunctional() )
                    {
                        debugCode = 400;
                        for ( int j = 0; j < dysonStrongholdBuildings.Count; j++ )
                        {
                            FleetMembership fleetMem = strongholdCityFleet.GetButDoNotAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( dysonStrongholdBuildings[j] );
                            if ( fleetMem != null ) //can't build anything!
                                fleetMem.ExplicitBaseSquadCap = 0;
                        }
                        //ArcenDebugging.ArcenDebugLog( "Stronghold for fleet " + stronghold.GetFleetName_Safe() + " is disabled.", Verbosity.DoNotShow );
                        continue; //no upgrades for me right now, since I'm nonfunctional
                    }
                    debugCode = 500;
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("Figuring out what " + city.ToStringWithPlanet() + " can build", Verbosity.DoNotShow );
                    int strongholdMarkLevel = city.CurrentMarkLevel;
                    //okay, make sure we CAN build things based on the mark level of the stronghold!
                    for ( int j = 0; j < dysonStrongholdBuildings.Count; j++ )
                    {
                        debugCode = 700;
                        GameEntityTypeData buildingInfo = dysonStrongholdBuildings[j];
                        if ( buildingInfo == null )
                            continue;
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tChecking if we can build " + buildingInfo.GetDisplayName(), Verbosity.DoNotShow );
                        //some building types are only available to some stronghold types
                        if (city.TypeData.GetHasTag("DysonSpire"))
                        {
                            if ( buildingInfo.GetHasTag("DysonTemplarBuildMenuOnly") ||
                                 buildingInfo.GetHasTag("DysonZenithBuildMenuOnly" ) ||
                                 buildingInfo.GetHasTag("DysonNeinzulBuildMenuOnly" ) )
                                continue;
                        }
                        if (city.TypeData.GetHasTag("DysonTemplar" ) )
                        {
                            if ( buildingInfo.GetHasTag("DysonSpireBuildMenuOnly" ) ||
                                 buildingInfo.GetHasTag("DysonZenithBuildMenuOnly" ) ||
                                 buildingInfo.GetHasTag("DysonNeinzulBuildMenuOnly" ) )
                                continue;
                        }
                        if (city.TypeData.GetHasTag("DysonZenithStronghold"))
                        {
                            if ( buildingInfo.GetHasTag("DysonTemplarBuildMenuOnly") ||
                                 buildingInfo.GetHasTag("DysonSpireBuildMenuOnly" ) ||
                                 buildingInfo.GetHasTag("DysonNeinzulBuildMenuOnly" ) )
                                continue;
                        }
                        if (city.TypeData.GetHasTag("DysonNeinzul"))
                        {
                            if ( buildingInfo.GetHasTag("DysonTemplarBuildMenuOnly") ||
                                 buildingInfo.GetHasTag("DysonZenithBuildMenuOnly" ) ||
                                 buildingInfo.GetHasTag("DysonSpireBuildMenuOnly" ) )
                                continue;
                        }
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tHas the correct race", Verbosity.DoNotShow );

                        //Some structures are gated by Tier
                        if (!BaseInfo.HasAdequateDistrictsToBuild(buildingInfo) &&
                            !BaseInfo.StartWithAllUpgrades)
                        {
                            continue;
                        }
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tHas the correct districts", Verbosity.DoNotShow );

                        if ( buildingInfo.GetHasTag("GatedByDrilling") &&
                             !BaseInfo.StartWithAllUpgrades)
                        {
                            if ( buildingInfo.GetHasTag("RequiresOnePlanetDrilled") && BaseInfo.PlanetsDrilled < 1)
                                continue;
                            if ( buildingInfo.GetHasTag("RequiresTwoPlanetsDrilled") && BaseInfo.PlanetsDrilled < 2)
                                continue;
                            if ( buildingInfo.GetHasTag("RequiresThreePlanetsDrilled") && BaseInfo.PlanetsDrilled < 3)
                                continue;
                            if ( buildingInfo.GetHasTag("RequiresFourPlanetsDrilled") && BaseInfo.PlanetsDrilled < 4)
                                continue;
                            if ( buildingInfo.GetHasTag("RequiresFivePlanetsDrilled") && BaseInfo.PlanetsDrilled < 5)
                                continue;
                            if ( buildingInfo.GetHasTag("RequiresSixPlanetsDrilled") && BaseInfo.PlanetsDrilled < 6)
                                continue;

                        }

                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tHas the correct drilling", Verbosity.DoNotShow );
                        FleetMembership fleetMem = null;
                        //if the building type requires a higher mark level of stronghold, don't show it.
                        if ( buildingInfo.MinimumRequiredCityLevelForConstruction > strongholdMarkLevel
                             && !BaseInfo.StartWithAllUpgrades )
                        {
                            fleetMem = strongholdCityFleet.GetButDoNotAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( buildingInfo );
                            if ( fleetMem != null ) //can't build this!
                                fleetMem.ExplicitBaseSquadCap = 0;
                             // ArcenDebugging.ArcenDebugLogSingleLine( "Stronghold for fleet " + city.GetFleetName_Safe() + " is only mark level " + strongholdMarkLevel + 
                             //     ", so skipping " + buildingInfo.DisplayName + ".", Verbosity.DoNotShow );
                            continue;
                        }
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tHas the right stronghole mark level", Verbosity.DoNotShow );

                        debugCode = 800;
                        int capWeShouldStartWith = buildingInfo.BaseShipCapInCustomCity;
                        if ( buildingInfo.AddedShipCapInCustomCityPerCityLevel > 0 )
                        {
                            int levelsAboveBase = 0;
                            if ( buildingInfo.AddedShipCapInCustomCityPerCityLevel_AboveLevel > 0 )
                                levelsAboveBase = strongholdMarkLevel - buildingInfo.AddedShipCapInCustomCityPerCityLevel_AboveLevel;
                            if ( levelsAboveBase > 0 )
                                capWeShouldStartWith += (levelsAboveBase * buildingInfo.AddedShipCapInCustomCityPerCityLevel);
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine("\t\tPotentially adding more mark levels above base: " + levelsAboveBase, Verbosity.DoNotShow );

                        }
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tAnd we have cap " + capWeShouldStartWith, Verbosity.DoNotShow );

                        debugCode = 900;
                        fleetMem = strongholdCityFleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( buildingInfo );
                        if ( fleetMem != null ) //set the cap based on the city level!
                        {
                            fleetMem.ExplicitBaseSquadCap = capWeShouldStartWith;
                            //ArcenDebugging.ArcenDebugLogSingleLine("City for fleet " + city.GetFleetName_Safe() + " is adding " + fleetMem.ExplicitBaseSquadCap +
                            //      " " + buildingInfo.DisplayName + " building cap.", Verbosity.DoNotShow );
                        }
                        // else
                        //     ArcenDebugging.ArcenDebugLog( "City for fleet " + city.GetFleetName_Safe() + " failed to add membership for building " + buildingInfo.DisplayName + "!", Verbosity.DoNotShow );
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in RecalculateDysonStrongholdBuildingContents_MainThreadSimOnly debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion
        
        public GameEntity_Squad SpawnDysonStronghold( ArcenPoint spawnLocation, Planet planet, string TypeName, Faction faction, 
                                                            ArcenHostOnlySimContext Context, out GameEntity_Squad DysonFlagship, bool exactPlacement )
        {
            if ( !ArcenNetworkAuthority.GetIsHostMode() )
            {
                DysonFlagship = null;
                return null; //only for the host; clients will get this data sync'd to them later
            }
            GameEntityTypeData strongholdData = GameEntityTypeDataTable.Instance.GetRowByName( TypeName );
            if ( strongholdData == null ) {
                strongholdData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, TypeName );
                if ( strongholdData == null )
                    throw new Exception("Unable to find XML with name or tag " + TypeName);
            }
            GameEntityTypeData flagshipEntityData = null;
            if (strongholdData.GetHasTag("DysonMajorStronghold"))
            {
                if (strongholdData.GetHasTag("SpawnsDysonSpireFleet"))
                {
                    flagshipEntityData = GameEntityTypeDataTable.Instance.GetRowByName("DysonSpireFlagshipTierOne");
                }
                else if (strongholdData.GetHasTag("SpawnsDysonNeinzulFleet"))
                {
                    flagshipEntityData = GameEntityTypeDataTable.Instance.GetRowByName("DysonNeinzulFlagshipTierOne");
                }
                else if (strongholdData.GetHasTag("SpawnsDysonZenithFleet"))
                {
                    flagshipEntityData = GameEntityTypeDataTable.Instance.GetRowByName("DysonZenithFlagshipTierOne");
                }
                else if (strongholdData.GetHasTag("SpawnsDysonTemplarFleet"))
                {
                    flagshipEntityData = GameEntityTypeDataTable.Instance.GetRowByName("DysonTemplarFlagshipTierOne");
                }

                if (flagshipEntityData == null)
                    throw new Exception("Couldn't figure out which flagship to spawn from " + strongholdData.GetDisplayName());
            }
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
            if ( spawnLocation == ArcenPoint.ZeroZeroPoint )
            {
                spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, strongholdData, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 300 ) );
            }
            else if ( exactPlacement )
                spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, strongholdData, spawnLocation, FInt.FromParts( 0, 010 ), FInt.FromParts( 0, 020 ) );
            else
                spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, strongholdData, spawnLocation, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 250 ) );
            GameEntity_Squad stronghold = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, strongholdData, 1,
                    null, 0, spawnLocation, Context, "Dyson-NewStronghold" );
            if ( stronghold != null ) { }
            //I'd like to say "If this is a safe placement point, put the unit here. Otherwise place it "very close by"  This is the best way.
            GameEntity_Squad dysonFlagship = null;
            bool spawnsFlagship = flagshipEntityData != null;
            bool bolstersFlagship = strongholdData.GetHasTag("BolstersDysonFleet");

            if ( spawnsFlagship )
            {
                spawnLocation = planet.GetSafePlacementPoint_AroundEntity( Context, flagshipEntityData, stronghold, FInt.FromParts( 0, 005 ), FInt.FromParts( 0, 010 ) );

                dysonFlagship = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, flagshipEntityData, 1,
                           null, 0, spawnLocation, Context, "Dyson-NewDysonFlagship" );
                DysonFlagship = dysonFlagship;
                if ( dysonFlagship == null )
                    return stronghold;
                dysonFlagship.FleetMembership.Fleet.IsFleetFlagshipAllowedToUseMovementModes = true;
            }
            else
                DysonFlagship = null;



            Fleet newDysonStrongholdFleet = stronghold.FleetMembership.Fleet;

            string nameBase = GetAvailableCityOrMoonName( stronghold, faction, Context );

            newDysonStrongholdFleet.NameRaw = "Stronghold " + nameBase;
            newDysonStrongholdFleet.FleetQualifier = "Dyson";

            DysonCityFleetBaseInfo dysonCityFleetInfo = newDysonStrongholdFleet.CreateExternalBaseInfo<DysonCityFleetBaseInfo>("DysonCityFleetBaseInfo");

            if ( bolstersFlagship )
            {
                dysonCityFleetInfo.CanChangeBolsteredFleet = true;
            }
            
            if ( spawnsFlagship )
            {
                Fleet newDysonMobileFleet = dysonFlagship.FleetMembership.Fleet;
                newDysonMobileFleet.NameRaw = "Dysonfleet " + nameBase;
                newDysonMobileFleet.FleetQualifier = "Dyson";
                newDysonMobileFleet.CreateExternalBaseInfo<DysonSidekickMobileFleetBaseInfo>( "DysonSidekickMobileFleetBaseInfo" );
                newDysonStrongholdFleet.CityBolstersFleetID = newDysonMobileFleet.FleetID;
            }
            //            ArcenDebugging.ArcenDebugLogSingleLine("Created a new fleet, " + newDysonFleet.GetName() + " with flagship " + dysonFlagship.ToString(), Verbosity.DoNotShow );
            // List<GameEntityTypeData> InitialShipsForFlagship = GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( "DysonSummons" );
            // for ( int i = 0; i < InitialShipsForFlagship.Count; i++ )
            // {
            //     GameEntityTypeData entitydata = InitialShipsForFlagship[i];
            //     int nextUniqueID = newDysonFleet.GetNextUniqueIntToUseOfMatchingMembershipGroupsBasedOnSquadType( entitydata );
            //     FleetMembership mem = newDysonFleet.GetOrAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates( entitydata, nextUniqueID );
            //     mem.ExplicitBaseSquadCap = 1;
            // }

            this.BaseInfo.Strongholds.AddToDisplayList(stronghold);
            return stronghold;
        }
        public GameEntity_Squad SpawnDysonMoon( ArcenPoint spawnLocation, Planet planet, string TypeName, Faction faction, 
                                                            ArcenHostOnlySimContext Context, bool exactPlacement )
        {
            if ( !ArcenNetworkAuthority.GetIsHostMode() )
            {
                return null; //only for the host; clients will get this data sync'd to them later
            }
            GameEntityTypeData moonData = GameEntityTypeDataTable.Instance.GetRowByName( TypeName );
            if ( moonData == null ) {
                moonData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, TypeName );
                if ( moonData == null )
                    throw new Exception("Unable to find XML with name or tag " + TypeName);
            }
            //GameEntityTypeData flagshipEntityData = null;
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
            if ( spawnLocation == ArcenPoint.ZeroZeroPoint )
            {
                spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, moonData, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 300 ) );
            }
            else if ( exactPlacement )
                spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, moonData, spawnLocation, FInt.FromParts( 0, 010 ), FInt.FromParts( 0, 020 ) );
            else
                spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, moonData, spawnLocation, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 250 ) );
            GameEntity_Squad moon = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, moonData, 1,
                    null, 0, spawnLocation, Context, "Dyson-NewMoon" );
            if ( moon != null ) { }
            //I'd like to say "If this is a safe placement point, put the unit here. Otherwise place it "very close by"  This is the best way.

            Fleet newDysonMoonFleet = moon.FleetMembership.Fleet;

            string nameBase = GetAvailableCityOrMoonName( moon, faction, Context );

            newDysonMoonFleet.NameRaw = "Moon " + nameBase;
            newDysonMoonFleet.FleetQualifier = "Dyson";

            DysonCityFleetBaseInfo dysonCityFleetInfo = newDysonMoonFleet.CreateExternalBaseInfo<DysonCityFleetBaseInfo>("DysonCityFleetBaseInfo");

            this.BaseInfo.Moons.AddToDisplayList(moon);
            return moon;
        }
        
        public GameEntity_Squad SpawnDysonDrill(ArcenPoint spawnLocation, Planet planet, string TypeName, Faction faction,
                                                 ArcenHostOnlySimContext Context, bool exactPlacement)
        {
            if (!ArcenNetworkAuthority.GetIsHostMode())
            {
                return null; //only for the host; clients will get this data sync'd to them later
            }

            GameEntityTypeData drillData = GameEntityTypeDataTable.Instance.GetRowByName(TypeName);
            if (drillData == null)
            {
                drillData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, TypeName);
                if (drillData == null)
                    throw new Exception("Unable to find XML with name or tag " + TypeName);
            }

            if ( spawnLocation == ArcenPoint.ZeroZeroPoint )
            {
                spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, drillData, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 300 ) );
            }
            else if ( exactPlacement )
                spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, drillData, spawnLocation, FInt.FromParts( 0, 010 ), FInt.FromParts( 0, 020 ) );
            else
                spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, drillData, spawnLocation, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 250 ) );
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction(this.AttachedFaction);
            GameEntity_Squad drill = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, drillData, 1,
                    null, 0, spawnLocation, Context, "Dyson-NewDrill" );
            return drill;
        }
        public GameEntity_Squad SpawnDysonSphere(ArcenPoint spawnLocation, Planet planet, string TypeName, Faction faction,
                                                 ArcenHostOnlySimContext Context, bool exactPlacement)
        {
            if (!ArcenNetworkAuthority.GetIsHostMode())
            {
                return null; //only for the host; clients will get this data sync'd to them later
            }

            GameEntityTypeData sphereData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound(TypeName);
            if (sphereData == null)
            {
                sphereData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, TypeName);
                if (sphereData == null)
                    throw new Exception("Unable to find XML with name or tag " + TypeName);
            }

            if ( spawnLocation == ArcenPoint.ZeroZeroPoint )
            {
                spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, sphereData, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 300 ) );
            }
            else if ( exactPlacement )
                spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, sphereData, spawnLocation, FInt.FromParts( 0, 010 ), FInt.FromParts( 0, 020 ) );
            else
                spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, sphereData, spawnLocation, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 250 ) );
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction(this.AttachedFaction);
            GameEntity_Squad sphere = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, sphereData, 1,
                    null, 0, spawnLocation, Context, "Dyson-NewSphere" );

            //And now spawn the flagship
            GameEntityTypeData flagshipEntityData = null;
            string name = "";
            if ( sphereData.GetHasTag("ZenithSidekickSphere") )
                name = "DysonSphereZenithFlagship";
            else if ( sphereData.GetHasTag("SpireSidekickSphere"))
                name = "DysonSphereSpireFlagship";
            flagshipEntityData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( name );
            if ( flagshipEntityData == null )
                throw new Exception("Could not find a flagship for " + sphere.ToStringWithPlanet() + " with name " + name );
            spawnLocation = planet.GetSafePlacementPoint_AroundEntity( Context, flagshipEntityData, sphere, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 100 ) );

            GameEntity_Squad dysonFlagship = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, flagshipEntityData, 1,
                                                                             null, 0, spawnLocation, Context, "Dyson-NewDysonFlagship" );
            if ( dysonFlagship != null )
            {
                dysonFlagship.FleetMembership.Fleet.IsFleetFlagshipAllowedToUseMovementModes = true;
                dysonFlagship.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
            }
            return sphere;
        }
        /* Long Range Planning Functions */
        private static readonly List<SafeSquadWrapper> AutoDefendingShipsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DysonFactionDeepInfo-AutoDefendingShipsLRP" );
        private static readonly List<SafeSquadWrapper> TransportsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DysonFactionDeepInfo-TransportsLRP" );
        private static readonly List<SafeSquadWrapper> DysonProductionLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DysonFactionDeepInfo-DysonProductionLRP" );
        private static readonly List<SafeSquadWrapper> DysonProductionNeedingOrdersLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DysonFactionDeepInfo-DysonProductionNeedingOrdersLRP" );
        private static readonly List<SafeSquadWrapper> ShipyardsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DysonFactionDeepInfo-ShipyardsLRP" );
        private static readonly List<SafeSquadWrapper> StrongholdsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DysonFactionDeepInfo-StrongholdsLRP" );
        private static readonly List<SafeSquadWrapper> DestForTransportsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DysonFactionDeepInfo-DestForTransportsLRP" );
        private static readonly List<Planet> WorkingPlanetsLRP = List<Planet>.Create_WillNeverBeGCed( 500, "DysonFactionDeepInfo-WorkingPlanetsLRP" );

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            ShipyardsLRP.Clear();
            AutoDefendingShipsLRP.Clear();
            TransportsLRP.Clear();
            DysonProductionLRP.Clear();
            DysonProductionNeedingOrdersLRP.Clear();
            WorkingPlanetsLRP.Clear();
            DestForTransportsLRP.Clear();
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            int debugCode = 0;
            try{
                debugCode = 100;
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads() )
            {
                debugCode = 200;
                if ( entity == null )
                    continue;
                debugCode = 300;
                if (entity.TypeData.GetHasTag("TransportDestination"))
                {
                    DestForTransportsLRP.Add( entity );
                    continue;
                }
                debugCode = 400;
                if (entity.TypeData.GetHasTag("DysonFlagship") ||
                    entity.TypeData.GetHasTag("DysonSphereFlagship" ) ||
                    entity.TypeData.GetHasTag("AutoDefenseShip") )
                {
                    AutoDefendingShipsLRP.Add(entity);
                    continue;
                }
                if (entity.TypeData.GetHasTag("DysonSidekickTransport"))
                {
                    TransportsLRP.Add(entity);
                    continue;
                }

                if (entity.TypeData.GetHasTag("DysonStronghold"))
                {
                    StrongholdsLRP.Add(entity);
                    continue;
                }
                debugCode = 500;
                int hostileStrength = entity.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                if (entity.TypeData.GetHasTag("DysonSphereVictoryProduction"))
                {
                    DysonProductionLRP.Add(entity);
                    if ( entity.Planet.IsEligibleForDeepStrike &&
                         entity.GetDestinationPlanet().IsEligibleForDeepStrike )
                    {
                        //if we are are deepstriking right now, retreat
                        DysonProductionNeedingOrdersLRP.Add(entity);
                        continue;
                    }
                    if ( !entity.HasQueuedOrders() &&
                         hostileStrength < 5 * 1000 )
                        DysonProductionNeedingOrdersLRP.Add(entity);
                    continue;
                }
                debugCode = 600;
                if (entity.TypeData.GetHasTag("DysonShipyard") && entity.SelfBuildingMetalRemaining <= 0 &&
                    entity.SecondsSpentAsRemains <= 0 &&
                    hostileStrength < 3000 ) //Don't try to rebuild a potentially damaged fleet on top of enemies
                {
                    ShipyardsLRP.Add(entity);
                    continue;
                }
            }
            debugCode = 700;
            int maxShips = 10;
            for (int i = 0; i < DysonProductionNeedingOrdersLRP.Count; i++ )
            {
                //This is ships created by the dyson sphere win condition
                GameEntity_Squad squad = DysonProductionNeedingOrdersLRP[i].GetSquad();
                if ( squad == null )
                    continue;
                Planet planet = GetNextPlanetForDysonProduction(squad, Context);
                if ( planet == null )
                {
                    continue;
                }
                AutoDefendUtility.GoToPlanet(squad, planet, Context, pathingCacheData);

                if ( maxShips-- < 0 )
                     break; //Don't try to give too many orders at once
            }
            debugCode = 800;
            for ( int i = 0; i < TransportsLRP.Count; i++ )
            {
                GameEntity_Squad squad = TransportsLRP[i].GetSquad();
                if ( squad == null )
                    continue;
                debugCode = 900;
                if ( DestForTransportsLRP.Count == 0 )
                {
                    ArcenDebugging.LogSingleLine("?", Verbosity.DoNotShow );
                    break; //this shouldn't really be possible
                }

                if ( !squad.HasQueuedOrders() )
                {
                    debugCode = 1200;
                    GameEntity_Squad Dest = GetClosestDestinationForTransport( squad, Context, pathingCacheData );
                    if ( squad.Planet != Dest.Planet )
                    {
                        AutoDefendUtility.GoToPlanet( squad, Dest.Planet, Context, pathingCacheData );
                    }
                    else
                    {
                        GoToLocation( squad, Dest.WorldLocation, Context, pathingCacheData);
                    }
                }
            }
            debugCode = 1300;
            //Below is the dyson auto-defend mode
            for (int i = 0; i < AutoDefendingShipsLRP.Count; i++ )
            {
                /* This is a very simple implementation. There's a ton of fancy things we could do, like 
                   being able to do multiple passes to find categories of battles (defensive/offensive), and prioritizing defense (or offense).
                   Another good improvement would be being able to make more informed choices that aren't "Just go to the closest battle".
                   Another good improvement would be letting fleets go to "threatened" planets (ie planets with incoming waves, or lots of hostile mobile forces nearby)

                   For that matter, there's no principled reason we couln't give a "Offensive" mode, where we could kill Instigator bases,
                   AIP reducers, or just neutering enemy planets.
                 */
                GameEntity_Squad defenseShip = AutoDefendingShipsLRP[i].GetSquad();
                if ( defenseShip == null )
                    continue;
                if ( defenseShip.TypeData.GetHasTag("DysonSphereFlagship") ||
                     defenseShip.TypeData.GetHasTag("AutoDefenseShip"))
                {
                    //we are always in auto-defend mode, since this is a Sphere flagship or an auto-defense ship
                }
                else if ( World_AIW2.Instance.Setup.GetStringBySetting("DysonAutoDefend") == "Disabled" ||
                          this.AttachedFaction.UnderPlayerControl())
                    continue; //auto defend is explicitly disabled, or the player is controlling
                Fleet fleet = defenseShip.FleetMembership.Fleet;
                if (fleet == null)
                {
                    continue;
                }
                bool fleetInWardenMode = fleet.Behavior == FleetBehavior.WardenMode;
                DysonSidekickPerUnitBaseInfo unitData = defenseShip.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                DysonSidekickMobileFleetBaseInfo fleetInfo = null;
                if ( defenseShip == centerpiece )
                {
                    fleetInfo = fleet.TryGetExternalBaseInfoAs<DysonSidekickMobileFleetBaseInfo>();
                    //the Sim code hasn't run to set up the mobile fleet info
                    if (fleetInfo == null)
                    {
                        continue;
                    }
                }

                Planet dest = defenseShip.GetDestinationPlanet();
                bool goToShipyard = false; //these double as "retreat"
                bool goToHomeStronghold = false;
                debugCode = 1500;
                int myStrength;
                if ( defenseShip.TypeData.GetHasTag("DysonFlagship") )
                    myStrength = fleet.CalculateEffectiveCurrentFleetStrength_PlayerFleetsOnly();
                else
                    myStrength = defenseShip.GetStrengthPerSquad();

                debugCode = 1400;

                bool debug = false;

                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("Doing LRP update for " + defenseShip.ToStringWithPlanet(), Verbosity.DoNotShow );

                debugCode = 1600;
                if (AutoDefendUtility.IsLosingBattle(defenseShip.Planet, myStrength, this.AttachedFaction ))
                {
                    debugCode = 1700;
                    if ( defenseShip.TypeData.AllowedHopsFromCenterpiece > -1 )
                        goToHomeStronghold = true;
                    else
                        goToShipyard = true; 
                }
                GameEntity_Squad homeStronghold = null;
                if ( unitData != null &&
                     defenseShip.TypeData.AllowedHopsFromCenterpiece > -1 )
                {
                    homeStronghold = unitData.HomeStronghold.GetSquad();
                    if ( homeStronghold.Planet.GetHopsTo( defenseShip.Planet ) > defenseShip.TypeData.AllowedHopsFromCenterpiece )
                        goToHomeStronghold = true;
                }
                debugCode = 1750;
                if ( !goToShipyard && fleetInfo != null && 
                     fleetInfo.NeedsToRebuild && AutoDefendUtility.CanISafelyLeavePlanet( defenseShip, myStrength, this.AttachedFaction ))
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
                    Planet shipyardPlanet = AutoDefendUtility.GetNearestShipyardToMe(defenseShip.Planet, ShipyardsLRP, this.AttachedFaction, Context, pathingCacheData);
                    if (shipyardPlanet != null)
                    {
                        if ( debug )
                             ArcenDebugging.ArcenDebugLogSingleLine("\tRetreating to " + shipyardPlanet.Name + " (goToShipyard path)", Verbosity.DoNotShow );

                        AutoDefendUtility.GoToPlanet(defenseShip, shipyardPlanet, Context, pathingCacheData);
                        continue;
                    }
                }
                if (goToHomeStronghold && homeStronghold != null )
                {
                    Planet strongholdPlanet = homeStronghold.Planet;
                    if (strongholdPlanet != null)
                    {
                        if (debug)
                            ArcenDebugging.ArcenDebugLogSingleLine("\tHeading to " + strongholdPlanet.Name + ", its home stronghold", Verbosity.DoNotShow);

                        AutoDefendUtility.GoToPlanet(defenseShip, strongholdPlanet, Context, pathingCacheData);
                    }
                }

                if ( defenseShip.GetIsCrippled() )
                    continue;
                debugCode = 2100;
                if ( AutoDefendUtility.IsPlanetUnderAttack(dest, this.AttachedFaction) )
                {
                    debugCode = 2200;
                    if (debug)
                    {
                        if ( dest == defenseShip.Planet )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tWe are already in a battle!", Verbosity.DoNotShow);
                        else
                            ArcenDebugging.ArcenDebugLogSingleLine("\tWe are en route to a battle at " + dest.Name, Verbosity.DoNotShow);
                    }

                    continue;
                }
                debugCode = 2300;
                //Check if we have any battles to go to!
                WorkingPlanetsLRP.Clear();
                if ( homeStronghold != null ) 
                    AutoDefendUtility.GetPlanetsUnderAttack( defenseShip, myStrength, homeStronghold.Planet, WorkingPlanetsLRP, fleetInWardenMode); //we are defending around a specific stronghold; Guardians
                else
                    AutoDefendUtility.GetPlanetsUnderAttack(defenseShip, myStrength, defenseShip.Planet, WorkingPlanetsLRP, fleetInWardenMode); //we are "generic defense"; defensive flagships
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("\tWe have found " + WorkingPlanetsLRP.Count + " planets under attack", Verbosity.DoNotShow );
                debugCode = 2400;
                if ( WorkingPlanetsLRP.Count > 0 )
                {
                    debugCode = 2500;
                    //The target's priority is chosen in GetPlanetsUnderAttack (so technically we could just pass back a single planet), but we use a List for future-proofing
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\tWe are heading to " + WorkingPlanetsLRP[0].Name + " to help defend", Verbosity.DoNotShow );

                    AutoDefendUtility.GoToPlanet( defenseShip, WorkingPlanetsLRP[0], Context, pathingCacheData);
                    continue;
                }
                debugCode = 2600;
                //We haven't found an ongoing battle to assist in, so see if we have a threatened planet we can defend
                WorkingPlanetsLRP.Clear();
                if ( AutoDefendUtility.IsPlanetThreatened( dest, this.AttachedFaction ))
                {
                    debugCode = 2700;
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\tWe are already on a threatened planet!", Verbosity.DoNotShow );
                    continue;
                }
                AutoDefendUtility.GetThreatenedPlanets( myStrength, defenseShip.Planet, WorkingPlanetsLRP, this.AttachedFaction, fleetInWardenMode);
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("\tWe have found " + WorkingPlanetsLRP.Count + " planets threatened", Verbosity.DoNotShow );
                debugCode = 2800;
                if ( WorkingPlanetsLRP.Count > 0 )
                {
                    Planet threatenedPlanet = WorkingPlanetsLRP[Context.RandomToUse.Next(0, WorkingPlanetsLRP.Count)];
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\tWe are heading to " + threatenedPlanet.Name + " because the planet is threatened", Verbosity.DoNotShow );
                    
                    AutoDefendUtility.GoToPlanet( defenseShip, threatenedPlanet, Context, pathingCacheData);
                    continue;
                }
                debugCode = 2900;
                if ( dest != defenseShip.Planet )
                    continue; //we don't have any more urgent objectives, and we are en route someplace
                if ( dest == defenseShip.Planet )
                {
                    if ( defenseShip.HasQueuedOrders() )
                        continue;
                    debugCode = 3000;
                    //Patrolling: head to a randomly chosen other shipyard, or someplace on this planet
                    if ( Context.RandomToUse.Next(0, 100) < 20 )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tWe are heading to part of of " + defenseShip.Planet.Name + " to patrol", Verbosity.DoNotShow );
                        FInt minRadius = FInt.FromParts( 0, 100 );
                        FInt maxRadius = FInt.FromParts( 0, 900 );

                        ArcenPoint patrolPoint = defenseShip.Planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, defenseShip.TypeData, Engine_AIW2.Instance.CombatCenter, minRadius, maxRadius );
                        GoToLocation(defenseShip, patrolPoint, Context, pathingCacheData);
                        continue;
                    }
                    Planet shipyardPlanet = AutoDefendUtility.GetRandomPlanetForPatrol(defenseShip, StrongholdsLRP, WorkingPlanetsLRP, Context, pathingCacheData);
                    if (shipyardPlanet != null)
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tWe are heading to " + shipyardPlanet.Name + " to patrol", Verbosity.DoNotShow );

                        AutoDefendUtility.GoToPlanet(defenseShip, shipyardPlanet, Context, pathingCacheData);
                        continue;
                    }
                }
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("\tNothing to do?", Verbosity.DoNotShow );
            }
            } catch(Exception e )
            {
                ArcenDebugging.LogSingleLine("Exception in dyson LRP debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            pathingCacheData.ReturnToPool();
            FleetBehaviorLRP.DoLRP( AttachedFaction, Context );
        }
        private void GoToLocation( GameEntity_Squad squad, ArcenPoint location, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathCacheData )
        {
            //We have the metal and the structure has the experience, so fly to it so we can upgrade it
            GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse );
            moveCommand.PlanetOrderWasIssuedFrom = squad.Planet.Index;
            moveCommand.RelatedPoints.Add( location );
            moveCommand.RelatedEntityIDs.Add( squad.PrimaryKeyID );
            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );
        }
        private GameEntity_Squad GetClosestDestinationForTransport( GameEntity_Squad transport, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathCacheData )
        {
            GameEntity_Squad closest = null;
            int closestHops = 0;
            for ( int i = 0; i < DestForTransportsLRP.Count; i++ )
            {
                GameEntity_Squad dest = DestForTransportsLRP[i].GetSquad();
                if ( dest == null )
                    continue;
                if ( closest == null || dest.Planet.GetHopsTo( transport.Planet ) < closestHops )
                {
                    closest = dest;
                    closestHops = dest.Planet.GetHopsTo( transport.Planet );
                }
            }
            return closest;
        }
        
        private Planet GetNextPlanetForDysonProduction( GameEntity_Squad squad, ArcenLongTermIntermittentPlanningContext Context)
        {
            WorkingPlanetsLRP.Clear();
            AutoDefendUtility.GetPlanetsUnderAttack( squad, 10 * 1000, squad.Planet, WorkingPlanetsLRP);
            if ( WorkingPlanetsLRP.Count == 0 )
                return null;
            cb_dysonProdSquad = squad;
            WorkingPlanetsLRP.Sort(static delegate (Planet Left, Planet Right)
            {
                int leftHops = Left.GetHopsTo(cb_dysonProdSquad.Planet);
                int rightHops = Right.GetHopsTo(cb_dysonProdSquad.Planet);
                return leftHops.CompareTo(rightHops);
            });

            for (int i = 0; i < WorkingPlanetsLRP.Count; i++ )
            {
                if ( WorkingPlanetsLRP[i].IsEligibleForDeepStrike )
                    continue; //don't go to planets that would trigger deepstrike
                if ( Context.RandomToUse.Next(0, 100) < 50 )
                    return WorkingPlanetsLRP[i];
            }
            return WorkingPlanetsLRP[0];
        }

        
        public void InitializeCuendillarAsteroidsAndPlanetoids( ArcenHostOnlySimContext Context )
        {
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "CuendillarAsteroid" ) )
            {
                DysonSidekickPerUnitBaseInfo data = entity.CreateExternalBaseInfo<DysonSidekickPerUnitBaseInfo>( "DysonSidekickPerUnitBaseInfo" );
                if ( data.CuendillarRemaining > 0 )
                    continue;
                data.CuendillarRemaining = this.BaseInfo.Income.StartingCuendillarForAsteroid + Context.RandomToUse.Next(0, this.BaseInfo.Income.CuendillarAsteroidVariance);
            }
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "CuendillarPlanetoid" ) )
            {
                DysonSidekickPerUnitBaseInfo data = entity.CreateExternalBaseInfo<DysonSidekickPerUnitBaseInfo>( "DysonSidekickPerUnitBaseInfo" );
                if ( data.CuendillarRemaining > 0 )
                    continue;
                data.CuendillarRemaining = this.BaseInfo.Income.StartingCuendillarForPlanetoid + Context.RandomToUse.Next(0, this.BaseInfo.Income.CuendillarPlanetoidVariance);
            }

        }
        public void GiveCuendillarToPlanets( ArcenHostOnlySimContext Context )
        {
            foreach ( Planet planet in World_AIW2.Instance.CurrentGalaxy.Planets( false ) )
            {
                if ( planet.IsRavaged )
                    continue;
                if ( DoesPlanetHaveDrill( planet ))
                    continue;
                if ( planet.ResourceOneRemainingForAnyPlayer > 0 )
                    continue;
                int variance = this.BaseInfo.Income.CuendillarPlanetaryVariance;
                int minForNonHomeworld = this.BaseInfo.Income.CuendillarMinForNonHomeworld;
                int minForHomeworld = this.BaseInfo.Income.CuendillarMinForHomeworldOrBastion;

                if ( planet.PopulationType == PlanetPopulationType.NonHomeworld )
                    planet.ResourceOneRemainingForAnyPlayer = (FInt)Context.RandomToUse.Next(variance ) + minForNonHomeworld;
                if ( planet.PopulationType == PlanetPopulationType.AIBastionWorld ||
                     planet.PopulationType == PlanetPopulationType.AIHomeworld )
                    planet.ResourceOneRemainingForAnyPlayer = (FInt)Context.RandomToUse.Next(variance ) + minForHomeworld;
            }
        }

        public static void SeedCuendillarFortresses(Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData MapData )
        {
            int seedThisMany = 12;
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "CuendillarFortress", SeedingType.CapturableWeightsAndMax,
            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 3, 99, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
            seedThisMany = 15;
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "CuendillarAsteroid", SeedingType.CapturableWeightsAndMax,
            seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 3, 99, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );

        }

        public override void DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly( ref int debugStage, GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull,
              Faction factionThatKilledEntity, Faction entityOwningFaction, int numExtraStacksKilled, ArcenHostOnlySimContext Context )
        {
            //We attribute all kills to the flagship
            int debugCode = 0;
            try{
                debugCode = 100;
            GameEntity_Squad killingSquad = null;
            if ( FiringSystemOrNull != null )
            {
                killingSquad = FiringSystemOrNull.ParentEntity;
            }
            debugCode = 200;
            if ( killingSquad == null )
                return;
            debugCode = 300;
            FleetMembership fMem = killingSquad.FleetMembership;
            if ( fMem == null )
                return;
            debugCode = 400;
            Fleet fleet = fMem.Fleet;
            if ( fleet == null )
                return;
            debugCode = 500;
            GameEntity_Squad flagship = fleet.Centerpiece.GetSquad();
            if ( flagship == null )
                return;
            debugCode = 600;
            if ( flagship.TypeData.KillsToTriggerTransformation <= 0 ||
                 flagship.TypeData.TransformAfterKills == null )
                return;
            debugCode = 700;
            DysonSidekickPerUnitBaseInfo data = flagship.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
            if ( data == null )
                return;
            data.UnitsKilled++;
            } catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly Dyson Sidekick debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            // if ( entity == null )
            //     return;
            // GameEntityTypeData entityType = entity.TypeData;
            // if ( entityType == null )
            //     return;
            // switch ( entityType.SpecialType )
            // {
            //     case SpecialEntityType.AICommandStationOriginal:
            //       World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Dyson_Stronghold", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            //       break;
            //     case SpecialEntityType.GuardPost:
            //     case SpecialEntityType.DireGuardPost:
            //       World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Dyson_GuardPosts", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            //         this.ConsiderReanimatingGuardPost( entity, entityType, Context );
            //         break;
            // }
        }
        
        public override bool SeedUnitsOnStartingPlanetDuringMapGen( Planet StartingPlanet, ConfigurationForFaction factionConfig, PlanetFaction pFaction, 
            ref ArcenPoint commandStationPoint, ref bool stillNeedsToSeedHumanHomeworldStuff, 
            Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull, ArcenHostOnlySimContext Context )
        {
            int debugIndex = 0;
            try
            {
                debugIndex = 10;
                Faction dysonFaction = pFaction.Faction;
                debugIndex = 15;

                DysonSidekickFactionDeepInfo dysonDeepInfo = pFaction.Faction.GetExternalDeepInfoAs<DysonSidekickFactionDeepInfo>();
                DysonSidekickFactionBaseInfo dysonBaseInfo = dysonDeepInfo.BaseInfo;

                debugIndex = 20;

                stillNeedsToSeedHumanHomeworldStuff = false;

                GameEntity_Squad dysonFlagship = null;
                GameEntity_Squad dysonStronghold = null;
                GameEntity_Squad dysonMoon = null;

                string startingMoonType = pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue("DysonStartingMoon", false);
                if (startingMoonType == "Random")
                {
                    int rand = Context.RandomToUse.Next(0, 100);
                    ArcenDebugging.LogSingleLine("random moon: " + rand, Verbosity.DoNotShow );
                    if (rand < 25)
                        startingMoonType = "Spire";
                    else if (rand < 50)
                        startingMoonType = "Zenith";
                    else if (rand < 75)
                        startingMoonType = "Neinzul";
                    else
                        startingMoonType = "Templar";
                }
                ArcenDebugging.LogSingleLine("got moon " + startingMoonType, Verbosity.DoNotShow );
                if (startingMoonType == "Spire")
                { 
                    dysonMoon = dysonDeepInfo.SpawnDysonMoon(ArcenPoint.ZeroZeroPoint, StartingPlanet, "SpireMoon", dysonFaction, Context, false);
                }
                else if (startingMoonType == "Neinzul")
                {
                    dysonMoon = dysonDeepInfo.SpawnDysonMoon(ArcenPoint.ZeroZeroPoint, StartingPlanet, "NeinzulMoon", dysonFaction, Context, false);
                }
                else if (startingMoonType == "Zenith")
                {
                    dysonMoon = dysonDeepInfo.SpawnDysonMoon(ArcenPoint.ZeroZeroPoint, StartingPlanet, "ZenithMoon", dysonFaction, Context, false);
                }
                else if (startingMoonType == "Templar")
                {
                    dysonMoon = dysonDeepInfo.SpawnDysonMoon(ArcenPoint.ZeroZeroPoint, StartingPlanet, "TemplarMoon", dysonFaction, Context, false);
                }
                if (dysonMoon == null)
                    throw new Exception("Cound not spawn initial moon; choice was " + startingMoonType);


                string startingStrongholdType = pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue("DysonStartingStronghold", false);
                if (startingStrongholdType == "Random")
                {
                    int rand = Context.RandomToUse.Next(0, 100);
                    if (rand < 25)
                        startingStrongholdType = "Spire";
                    else if (rand < 50)
                        startingStrongholdType = "Zenith";
                    else if (rand < 75)
                        startingStrongholdType = "Neinzul";
                    else
                        startingStrongholdType = "Zenith";//starting with Templar kinda sucks, since they don't have economic bonuses.
                }
                Fleet dysonFleet = null;
                FInt placementOffsetScale = FInt.FromParts( 1, 500 );
                if (startingStrongholdType == "Spire")
                { 
                    dysonStronghold = dysonDeepInfo.SpawnDysonStronghold(ArcenPoint.ZeroZeroPoint, StartingPlanet, "StartingSpireStronghold", dysonFaction, Context, out dysonFlagship, false);
                    dysonFleet = dysonStronghold.GetFleetOrNull_Safe();
                    StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, dysonStronghold.WorldLocation, pFaction, dysonFleet, placementOffsetScale, "SpawnsWithSpireStronghold", 2400, 600 );
                }
                else if (startingStrongholdType == "Neinzul")
                {
                    dysonStronghold = dysonDeepInfo.SpawnDysonStronghold(ArcenPoint.ZeroZeroPoint, StartingPlanet, "StartingNeinzulStronghold", dysonFaction, Context, out dysonFlagship, false);
                    dysonFleet = dysonStronghold.GetFleetOrNull_Safe();
                    StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, dysonStronghold.WorldLocation, pFaction, dysonFleet, placementOffsetScale, "SpawnsWithNeinzulStronghold", 2400, 600 );
                }
                else if (startingStrongholdType == "Zenith")
                {
                    dysonStronghold = dysonDeepInfo.SpawnDysonStronghold(ArcenPoint.ZeroZeroPoint, StartingPlanet, "StartingZenithStronghold", dysonFaction, Context, out dysonFlagship, false);
                    dysonFleet = dysonStronghold.GetFleetOrNull_Safe();
                    StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, dysonStronghold.WorldLocation, pFaction, dysonFleet, placementOffsetScale, "SpawnsWithZenithStronghold", 2400, 600 );
                }
                else if (startingStrongholdType == "Templar")
                {
                    dysonStronghold = dysonDeepInfo.SpawnDysonStronghold(ArcenPoint.ZeroZeroPoint, StartingPlanet, "StartingTemplarStronghold", dysonFaction, Context, out dysonFlagship, false);
                    dysonFleet = dysonStronghold.GetFleetOrNull_Safe();
                    StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, dysonStronghold.WorldLocation, pFaction, dysonFleet, placementOffsetScale, "SpawnsWithTemplarStronghold", 2400, 600 );
                }
                if (dysonStronghold == null)
                    throw new Exception("Cound not spawn initial stronghold; choice was " + startingStrongholdType);
                
                StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, dysonStronghold.WorldLocation, pFaction, dysonFleet, placementOffsetScale, "DysonShipyard", 3600, 1600 );
                PlayerTypeData dysonPlayerType = pFaction.Faction.PlayerTypeDataOrNull_ModeratelyExpensive;
                //string kingTag = "DysonEmpireKing";
                if ( dysonPlayerType != null && dysonPlayerType.GetHasTag( "DysonEmpire" ) )
                    StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, dysonStronghold.WorldLocation, pFaction, dysonFleet, placementOffsetScale, "DysonEmpireKing", 3800, 600 );
                else
                    StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, dysonStronghold.WorldLocation, pFaction, dysonFleet, placementOffsetScale, "DysonSidekickKing", 3800, 600 );
                StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, dysonStronghold.WorldLocation, pFaction, dysonFleet, placementOffsetScale, "HQFlagship", 3800, 600 );
                if ( dysonFlagship != null )
                {
                    Fleet dysonFlagshipFleet = dysonFlagship.GetFleetOrNull_Safe();
                    GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    command.RelatedIntegers.Add( dysonFlagshipFleet.FleetID ); //FleetID
                    command.RelatedIntegers.Add( PlayerAccount.Local.PlayerPrimaryKeyID );
                    command.RelatedString = "ToggleIsFleetOnPlayerWatchlist";
                    command.RelatedBool = true;
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                    dysonFlagshipFleet.IsFleetInTransportLoadMode = false;

                    //GameEntityTypeData spawnType;

                    //60 skeleton base
                    // spawnType = GameEntityTypeDataTable.Instance.GetRowByName( "BaseSkeleton" );
                    // for ( int i = 0; i < 60; i++ )
                    //     dysonFlagship.SpawnEntity_ReturnNullIfMPClient( spawnType, 1, dysonFleet, 0, 0, Context, "DysonSidekickStart", false );

                    // //18 wight base
                    // spawnType = GameEntityTypeDataTable.Instance.GetRowByName( "BaseWight" );
                    // for ( int i = 0; i < 18; i++ )
                    //     dysonFlagship.SpawnEntity_ReturnNullIfMPClient( spawnType, 1, dysonFleet, 0, 0, Context, "DysonSidekickStart", false );
                }

                debugIndex = 50;
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DS_Dyson_Lore", string.Empty, dysonFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DS_Dyson_Introduction", string.Empty, dysonFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DS_Dyson_Economy", string.Empty, dysonFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DS_Dyson_Coalition", string.Empty, dysonFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DS_Dyson_Enemies", string.Empty, dysonFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DS_Dyson_Hybrids", string.Empty, dysonFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );

                debugIndex = 100;
                dysonFaction.StoredMetal = (FInt)(400 * 1000);
                dysonFaction.StoredScience = FInt.FromParts( 3000, 000 );
                dysonFaction.StoredHacking = FInt.FromParts( 45, 000 );
                dysonFaction.StoredFactionResourceOne = FInt.FromParts( 50, 000 ); //enough to get things started.
                if ( dysonBaseInfo.BonusStartingResources > 0 )
                {
                    dysonFaction.StoredScience += FInt.FromParts( 1000, 000 ) * dysonBaseInfo.BonusStartingResources;
                    //dysonFaction.StoredHacking += FInt.FromParts( 20, 000 ) * necroBaseInfo.BonusStartingResources;
                    dysonFaction.StoredFactionResourceOne += FInt.FromParts( 10, 000 ) * dysonBaseInfo.BonusStartingResources;
                };
            
                //At the beginning of the game the player gets one lowest-tier skeleton type and wight variant type
                //the goal of this is to let the player start out a bit stronger
                // debugIndex = 200;
                // string tagAndFieldName = "DysonSkeletonGroup";
                // DysonUpgrade upgrade = DysonUpgradeTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( tagAndFieldName, false ) );
                // if ( upgrade == null )
                // {
                //     upgrade = DysonUpgradeTable.Instance.GetRandomBonusStartingSkeletonType( Context );
                // }
                // necroBaseInfo.DysonCompletedUpgrades.Add( upgrade );

                // debugIndex = 400;
                // DysonUpgradeEvent thisEvent = DysonUpgradeEvent.Create( necroFaction.FactionIndex, -1, StartingPlanet.Index, upgrade.Index, -1, null );
                // necroBaseInfo.DysonHistory.Add( thisEvent );
                // StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, necropolis.WorldLocation, pFaction, necroCity, placementOffsetScale, upgrade.ShipForCapIncrease, 700, 600 );

                // debugIndex = 500;
                // tagAndFieldName = "DysonWightGroup";
                // upgrade = DysonUpgradeTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( tagAndFieldName, false ) );
                // if ( upgrade == null )
                // {
                //     upgrade = DysonUpgradeTable.Instance.GetRandomBonusStartingWightType( Context );
                // }
                // necroBaseInfo.DysonCompletedUpgrades.Add( upgrade );
                // debugIndex = 600;
                // thisEvent = DysonUpgradeEvent.Create( necroFaction.FactionIndex, -1, StartingPlanet.Index, upgrade.Index, -1, null );
                // necroBaseInfo.DysonHistory.Add( thisEvent );
                // StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, necropolis.WorldLocation, pFaction, necroCity, placementOffsetScale, upgrade.ShipForCapIncrease, 1600, 600 );

                // debugIndex = 650;
                // tagAndFieldName = "DysonUtilityGroup";
                // upgrade = DysonUpgradeTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( tagAndFieldName, false ) );
                // if ( upgrade == null )
                // {
                //     upgrade = DysonUpgradeTable.Instance.GetRandomBonusStartingUtilityType( Context );
                // }
                // necroBaseInfo.DysonCompletedUpgrades.Add( upgrade );
                // debugIndex = 600;
                // thisEvent = DysonUpgradeEvent.Create( necroFaction.FactionIndex, -1, StartingPlanet.Index, upgrade.Index, -1, null );
                // necroBaseInfo.DysonHistory.Add( thisEvent );
                // StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, necropolis.WorldLocation, pFaction, necroCity, placementOffsetScale, upgrade.ShipForCapIncrease, 1200, -200 );

                // debugIndex = 700;
                // if ( necroBaseInfo.BonusStartingResources > 0 )
                // {
                //     necroFaction.StoredScience += FInt.FromParts( 1000, 000 ) * necroBaseInfo.BonusStartingResources;
                //     necroFaction.StoredHacking += FInt.FromParts( 20, 000 ) * necroBaseInfo.BonusStartingResources;
                //     necroFaction.StoredFactionResourceOne += FInt.FromParts( 10, 000 ) * necroBaseInfo.BonusStartingResources;
                // }
                // debugIndex = 800;
                // if ( necroBaseInfo.BonusStartingWight )
                // {
                //     do
                //     {
                //         upgrade = DysonUpgradeTable.Instance.GetRandomBonusStartingWightType( Context );
                //     } while ( necroBaseInfo.DysonCompletedUpgrades.Contains( upgrade ) );
                //     necroBaseInfo.DysonCompletedUpgrades.Add( upgrade );
                //     thisEvent = DysonUpgradeEvent.Create( necroFaction.FactionIndex, -1, StartingPlanet.Index, upgrade.Index, -1, null );
                //     necroBaseInfo.DysonHistory.Add( thisEvent );
                // }
                // if ( necroBaseInfo.StartWithAllUpgrades )
                // {
                //     while ( true )
                //     {
                //         upgrade = DysonUpgradeTable.Instance.GetNextFactionUpgrade( );
                //         if ( upgrade == null )
                //             break;
                //         necroBaseInfo.DysonCompletedUpgrades.Add( upgrade );
                //         thisEvent = DysonUpgradeEvent.Create( necroFaction.FactionIndex, -1, StartingPlanet.Index, upgrade.Index, -1, null );
                //         necroBaseInfo.DysonHistory.Add( thisEvent );
                //     }
                // }

                debugIndex = 2000;
            }
            catch ( Exception e )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "DysonSidekickSpecificCodeDeepInfo SeedUnitsOnStartingPlanetDuringMapGen error at debugIndex " + debugIndex + ": " + e );
                return false;
            }
            return true;
        }

        public override void SeedSpecialEntities_LateAfterAllFactionSeeding_CustomForPlayerType( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData MapData )
        {
            IList<Planet> planetsSeeded;
            DysonSidekickFactionDeepInfo.SeedCuendillarFortresses( galaxy, Context, MapData );
            //This puts an extra distribution node in the game, but the actual reason is to force the game to check whether the starting planet is shared with a player empire

             foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
             {
                 PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                 if ( playerType == null )
                     continue;
                 if ( playerType.GetHasTag("DysonSidekick") )
                 {
                     //each dyson gets a skeleton and wight amplifier close to them
                     planetsSeeded = StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "WeakCuendillarFortress", SeedingType.CapturableWeightsAndMax,
                         2, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallBad, 1, 3, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                                      player, 1 );
                     planetsSeeded = StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "CuendillarAsteroid", SeedingType.CapturableWeightsAndMax,
                         2, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallBad, 1, 3, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly,
                                      player, 1 );
                 }
             }
        }

    }
}
