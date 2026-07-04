using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{

    public class ArmadaFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        //Set immediately before WorkingPlanetsLRP.Sort(...) so the comparison can be a non-capturing
        //static delegate.  [ThreadStatic] because long-range planning runs on a background thread.
        [ThreadStatic] private static GameEntity_Squad cb_armadaProdSquad;
        public ArmadaFactionBaseInfo BaseInfo;
        public static ArmadaFactionDeepInfo Instance = null;
        private static readonly Dictionary<GameEntityTypeData, int> AttackComposition = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed(500, "ArmadaFactionDeepInfo-AttackComposition");
        private static readonly Dictionary<GameEntityTypeData, int> AttackCompositionForRavagerTroops = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed(500, "ArmadaFactionDeepInfo-ForRavagerTroops");
        private static readonly List<GameEntity_Squad> WorkingList = List<GameEntity_Squad>.Create_WillNeverBeGCed(100, "ArmadaFactionDeepInfo-WorkingList");
        private static readonly List<Planet> WorkingPlanetList = List<Planet>.Create_WillNeverBeGCed(100, "ArmadaFactionDeepInfo-WorkingPlanetList");
        public static readonly List<SafeSquadWrapper> ExoTargets = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ArmadaFactionDeepInfo-ExoTargets" );
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
            Instance = this;
        }

        protected override void Cleanup()
        {
            Instance = null;
            BaseInfo = null;

            //probably does not matter
            WorkingAvailableNames.Clear();
            StarbasesLRP.Clear();
            ShipyardsLRP.Clear();
            AutoDefendingShipsLRP.Clear();
            TransportsLRP.Clear();
            AttackComposition.Clear();
            AttackCompositionForRavagerTroops.Clear();
            WorkingList.Clear();
            ArmadaProductionLRP.Clear();
            ArmadaProductionNeedingOrdersLRP.Clear();
            ExoTargets.Clear();
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 3; //Should be responsive for the player

        private static readonly string[] armadaCityNames = new string[] {
            "Galileo", "Newton", "Franklin", "Euler", "Coulomb", "Volta", "Fourier", "Ampere", "Joule", "Avogadro", "Rontgen", "Curie", "Meitner", "Rosalind", "Dresselhaus", "Schrodinger", "Bothe", "Pauli", "Oppenheimer", "Cherenkov", "Feynmann", "Turing", "Godel", "Ada", "Lovelace", "von Neumann", "Alpha", "Beta", "Tau", "Ricci", "Delta", "Bohr", "Noether", "Germaine"    };
        // if ( World_AIW2.Instance.GameSecond > 60 )
        // {
        //     World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Armada_MarkUpgrades", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        //     World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Armada_Blueprints", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        //     World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Armada_Amplifiers", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        //     World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Armada_Defenses", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        // }
        // if ( World_AIW2.Instance.GameSecond > 80 )
        // {
        //     World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Armada_Skeletons", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        //     World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Armada_Wights", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        // }
        // if ( World_AIW2.Instance.GameSecond > 400 )
        // {
        //     World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Armada_ResourceMonitoring", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        // }
        // if ( World_AIW2.Instance.GameSecond > 360 )
        // {
        //     World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Armada_Towers", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        // }
        // if ( World_AIW2.Instance.GameSecond > 320 )
        // {
        //     World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Armada_Totems", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        // }

        private static readonly List<string> WorkingAvailableNames = List<string>.Create_WillNeverBeGCed(300, "ArmadaFactionDeepInfo-WorkingAvailableNames");
        private static string GetAvailableCityOrMoonName(GameEntity_Squad entity, Faction faction, ArcenHostOnlySimContext Context)
        {
            WorkingAvailableNames.Clear();
            int foundStructures = 0;
            if ( entity == null )
            {
                return "NullEntityThisIsBug";
            }
            if ( World_AIW2.Instance.Setup.GetBoolBySetting("BoringArmadaNames") && entity.TypeData.GetHasTag("ArmadaStarbase") )
            {
                return entity.Planet.Name;
            }
            for (int i = 0; i < armadaCityNames.Length; i++)
            {
                bool foundMatch = false;
                foundStructures = 0;
               foreach ( GameEntity_Squad starbase in faction.Squads( "ArmadaStarbase" ) )
               {
                   foundStructures++;
                   if (starbase.GetFleetName_Safe().Contains(armadaCityNames[i]))
                   {
                       foundMatch = true;
                       break;
                   }
               }
               foreach ( GameEntity_Squad moon in faction.Squads( "ArmadaMoon" ) )
               {
                   foundStructures++;
                   if (moon.GetFleetName_Safe().Contains(armadaCityNames[i]))
                   {
                       foundMatch = true;
                       break;
                   }
               }

                if (foundMatch)
                    continue;
                WorkingAvailableNames.Add(armadaCityNames[i]);
            }
            if (WorkingAvailableNames.Count > 0)
                return WorkingAvailableNames[Context.RandomToUse.Next(0, WorkingAvailableNames.Count)];
            else
                return armadaCityNames[Context.RandomToUse.Next(0, armadaCityNames.Length)] + " " + foundStructures; //give us a unique new name
        }

        private BolsteringManager bolsteringManger = new BolsteringManager();
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly(ArcenHostOnlySimContext Context)
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has(ArcenTracingFlags.Armada);
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate("ArmadamD-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f) : null;

            RecalculateArmadaStarbaseBuildingContents_MainThreadSimOnly(Context);
            this.bolsteringManger.HandleBolstering("ArmadaFeedsFleet", this.BaseInfo.Starbases.GetDisplayList(), this.BaseInfo.BolsterableFlagships.GetDisplayList(), Context);
            //HandleJournalsAndTips( Context );

            RemindAboutTipsSidebarIfNecessary(Context);
            ProcessMinesIfNecessary(Context);
            ProcessProducersIfNecessary(Context);
            // HandleRavagerAssaults(Context);
            // HandlePlanetDrillingAndOverloading(Context);
            // HandleSpheres(Context);
            SpawnRangers(Context);
            HandleSwarmLures(Context);
            HandleLocustsSim(Context);
            GetEnemyIncome( Context );
            UpdateFlagships( Context );
            SpawnCPA( Context );

            // HandleFlagshipUpgrades(Context);
            DropMetalGenerators(Context);
            // InitializeCuendillarAsteroidsAndPlanetoids( Context);
            // GiveCuendillarToPlanets( Context );
            #region Tracing
            if (tracing && !tracingBuffer.GetIsEmpty()) ArcenDebugging.ArcenDebugLogSingleLine(tracingBuffer.ToString(), Verbosity.DoNotShow);
            if (tracing)
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
        }
        
        private void ProcessMinesIfNecessary(ArcenHostOnlySimContext Context)
        {
            bool checkDuplicates = false;
            if ( World_AIW2.Instance.GameSecond % 10 == 1 )
                checkDuplicates = true;
            WorkingPlanetList.Clear();
            foreach ( GameEntity_Squad mine in this.BaseInfo.Mines.DisplaySquads() )
            {
                this.BaseInfo.MiningIneligiblePlanets[mine.Planet] = World_AIW2.Instance.GameSecond + this.BaseInfo.Income.IneligibleMiningInterval;
                ArmadaPerUnitBaseInfo data = mine.TryGetExternalBaseInfoAs<ArmadaPerUnitBaseInfo>();
                if ( checkDuplicates )
                {
                    //make sure we don't have multiple mines on the same planet
                    bool duplicateFound = false;
                    for ( int i = 0; i < WorkingPlanetList.Count; i++ )
                    {
                        if ( mine.Planet == WorkingPlanetList[i])
                        {
                            mine.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                            duplicateFound = true;
                            break;
                        }
                    }
                    if ( duplicateFound )
                        continue;
                    WorkingPlanetList.Add( mine.Planet );
                }
                if ( data.MineFinishTime <= World_AIW2.Instance.GameSecond )
                    TransformMine( mine, Context );
            }
            int pairCount = this.BaseInfo.MiningIneligiblePlanets.Count;
            foreach ( KeyValuePair<Planet, int> pair in this.BaseInfo.MiningIneligiblePlanets )
            {
                if ( pair.Value < World_AIW2.Instance.GameSecond )
                {
                    // if ( localDebug )
                    //     ArcenDebugging.ArcenDebugLogSingleLine( "Removing planet " + pair.Key + " from the Ineligible list. Ineligible until " + pair.Value + " < " + World_AIW2.Instance.GameSecond + " (current second)", Verbosity.DoNotShow );
                    this.BaseInfo.MiningIneligiblePlanets.Remove( pair.Key );
                }
            }
        }
        private void ProcessProducersIfNecessary(ArcenHostOnlySimContext Context)
        {
            WorkingPlanetList.Clear();
            foreach ( GameEntity_Squad producer in this.BaseInfo.Producers.DisplaySquads() )
            {
                ArmadaPerUnitBaseInfo data = producer.TryGetExternalBaseInfoAs<ArmadaPerUnitBaseInfo>();
                if ( data.ProducerNextTransportTime == -1 )
                {
                    data.ProducerNextTransportTime = World_AIW2.Instance.GameSecond + BaseInfo.Income.ProducerInterval;
                }
                bool metalOnlyMode = this.AttachedFaction.StoredMetal < 5000;
                if ( metalOnlyMode )
                {
                    //We are low on metal, so trigger the next Transport quickly
                    data.ProducerNextTransportTime--;
                    if ( World_AIW2.Instance.GameSecond % 6 == 0 ) //and just a bit more
                        data.ProducerNextTransportTime--;
                    //If we're really far underwater, go even faster
                    //This code is stolen from the resource bar "see how long till we have metal again" code
                    float secondsLeft = (AttachedFaction.LastFrame_TotalMetalFlowProjectedRequests / AttachedFaction.LastFrame_MetalProduced).GetNearestIntPreferringHigher() * World_AIW2.Instance.SimulationProfile.SecondsPerFrameNonSim;

                    data.ProducerNextTransportTime -= (int)secondsLeft / 15;
                }
                if ( data.ProducerNextTransportTime <= World_AIW2.Instance.GameSecond )
                {
                    data.ProducerNextTransportTime = World_AIW2.Instance.GameSecond + BaseInfo.Income.ProducerInterval + Context.RandomToUse.Next(0, 60);
                    //Make a transport
                    GameEntityTypeData transportEntData = GameEntityTypeDataTable.Instance.GetRowByName( "ArmadaTransport");
                    GameEntity_Squad transport = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( producer.PlanetFaction, transportEntData, 1,
                        producer.PlanetFaction.Faction.LooseFleet, 0, producer.WorldLocation, Context, "Armada-SpawnTransportProducer" );
                    ArmadaPerUnitBaseInfo transportData = transport.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>("ArmadaPerUnitBaseInfo");
                    if ( producer.TypeData.GetHasTag("MajorArmadaProducer"))
                    {
                        transportData.MetalTransported = BaseInfo.Income.BaseMetalProducedFromMine;
                        transportData.ScienceTransported = BaseInfo.Income.BaseScienceProducedFromMine / 10;
                        transportData.HackingTransported = BaseInfo.Income.BaseHackingProducedFromMine / 10;
                        transportData.TiberiumTransported = 1;
                    }
                    if ( producer.TypeData.GetHasTag("MinorArmadaProducer"))
                    {
                        transportData.MetalTransported = BaseInfo.Income.BaseMetalProducedFromMine / 4;
                        transportData.ScienceTransported = BaseInfo.Income.BaseScienceProducedFromMine / 14;
                        //Giving HaP makes it feel like you never need a hacking mine
                        //transportData.HackingTransported = BaseInfo.Income.BaseHackingProducedFromMine / 5;
                    }

                    if ( metalOnlyMode )
                    {
                        //Only send metal, we are low
                        transportData.MetalTransported *= 3;
                        transportData.ScienceTransported = 0;
                        transportData.HackingTransported = 0;
                        transportData.TiberiumTransported = 0;
                    }
                    else
                    {
                        //don't strengthen the enemies in Metal-Only Mode
                        BaseInfo.StrengthForNextEnemyAttack += BaseInfo.Difficulty.BonusEnemyResponsePerMine;
                    }


                }

            }
        }
        private void UpdateFlagships(ArcenHostOnlySimContext Context)
        {
            if ( World_AIW2.Instance.GameSecond % 5 != 0 )
                return; //only do this every so often
            foreach ( GameEntity_Squad flagship in this.BaseInfo.Flagships.DisplaySquads() )
            {
                if ( flagship == null )
                    continue;
                ArmadaPerUnitBaseInfo data = flagship.TryGetExternalBaseInfoAs<ArmadaPerUnitBaseInfo>();
                if ( data == null )
                    continue;
                if ( data.UnitsKilled < flagship.TypeData.KillsToTriggerTransformation)
                    continue;
                if ( flagship.ActiveHack != null )
                    continue; //don't transform while hacking
                if ( flagship.TypeData.TransformAfterKills != null )
                {
                    GameEntityTypeData entityData = flagship.TypeData.TransformAfterKills;
                    flagship.TransformInto(Context, entityData, 1, true);
                }
            }
        }

        private void TransformMine( GameEntity_Squad mine, ArcenHostOnlySimContext Context)
        {
            GameEntityTypeData transportData = GameEntityTypeDataTable.Instance.GetRowByName( "ArmadaTransport");
            GameEntity_Squad transport = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( mine.PlanetFaction, transportData, 1,
                mine.PlanetFaction.Faction.LooseFleet, 0, mine.WorldLocation, Context, "Armada-SpawnTransport" );
            mine.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );            

            ArmadaPerUnitBaseInfo data = transport.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>("ArmadaPerUnitBaseInfo");
            FInt tierMultiplier;
            if ( mine.TypeData.GetHasTag( "ArmadaMineT4" ) )
                tierMultiplier = FInt.FromParts( 2, 500 );
            else if ( mine.TypeData.GetHasTag( "ArmadaMineT3" ) )
                tierMultiplier = FInt.FromParts( 2, 000 );
            else if ( mine.TypeData.GetHasTag( "ArmadaMineT2" ) )
                tierMultiplier = FInt.FromParts( 1, 500 );
            else if ( mine.TypeData.GetHasTag( "ArmadaMineT1" ) )
                tierMultiplier = FInt.One;
            else
            {
                ArcenDebugging.LogSingleLine( "ArmadaEmpireFactionDeepInfo: mine " + mine.TypeData.InternalName + " has no ArmadaMineT1/T2/T3/T4 tag; defaulting to T1 multiplier", Verbosity.ShowAsError );
                tierMultiplier = FInt.One;
            }
            if ( mine.TypeData.GetHasTag("ArmadaGreaterMetalMine"))
            {
                data.MetalTransported = BaseInfo.Income.BaseMetalProducedFromMine + BaseInfo.Income.MetalIncomeIncreaseFromMarkLevel * mine.CurrentMarkLevel;
                data.MetalTransported *= 2;
                data.MetalTransported = (data.MetalTransported * tierMultiplier).IntValue;
            }
            if ( mine.TypeData.GetHasTag("ArmadaMetalMine"))
            {
                data.MetalTransported = BaseInfo.Income.BaseMetalProducedFromMine + BaseInfo.Income.MetalIncomeIncreaseFromMarkLevel * mine.CurrentMarkLevel;
                data.MetalTransported = (data.MetalTransported * tierMultiplier).IntValue;
            }
            if ( mine.TypeData.GetHasTag("ArmadaScienceMine"))
            {
                data.ScienceTransported = BaseInfo.Income.BaseScienceProducedFromMine + BaseInfo.Income.ScienceIncomeIncreaseFromMarkLevel * mine.CurrentMarkLevel;
                data.ScienceTransported = (data.ScienceTransported * tierMultiplier).IntValue;
            }
            if ( mine.TypeData.GetHasTag("ArmadaHackingMine"))
            {
                data.HackingTransported = BaseInfo.Income.BaseHackingProducedFromMine + BaseInfo.Income.HackingIncomeIncreaseFromMarkLevel * mine.CurrentMarkLevel;
                data.HackingTransported = (data.HackingTransported * tierMultiplier).IntValue;
            }
            if ( mine.TypeData.GetHasTag("ArmadaTiberiumMine"))
            {
                //not currently a feature
                data.TiberiumTransported = BaseInfo.Income.BaseTiberiumProducedFromMine + BaseInfo.Income.TiberiumIncomeIncreaseFromMarkLevel * mine.CurrentMarkLevel;
            }
            BaseInfo.PlanetMineCount[mine.Planet]++;
            BaseInfo.StrengthForNextEnemyAttack += BaseInfo.Difficulty.BonusEnemyResponsePerMine;
        }
        private void DropMetalGenerators(ArcenHostOnlySimContext Context)
        {
            //at game start time, the armada will get the metal generators on its home planet
            //this is not ideal, since we don't use them. So either give them to the human empire we started with, or
            //make them neutral
            if ( World_AIW2.Instance.GameSecond % 10 == 1 )
                return;
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
        
        private bool CanBuildRanger(GameEntity_Squad starbase, int cap)
        {
            Dictionary<SafeSquadWrapper, int> rangers = BaseInfo.RangersPerStarbase.GetDisplayDict();
            //ArcenDebugging.LogSingleLine("How many rangers does " + starbase.ToStringWithPlanet() + " have? We see " + rangers.GetPairCount() + " possibilities", Verbosity.DoNotShow );
            int guardCount = 0;
            foreach ( KeyValuePair<SafeSquadWrapper, int> guards in rangers )
            {
                //ArcenDebugging.LogSingleLine("Checking rangers to see if they match " + starbase.ToStringWithPlanet() + " checking against " + guards.Key.GetSquad().ToString() , Verbosity.DoNotShow );
                if ( guards.Key.GetSquad() == starbase)
                {
                    guardCount = guards.Value;
                    break;
                }
            }

            if ( guardCount >= cap )
                return false;
            return true;
        }
        private bool CanBuildDireRanger(GameEntity_Squad starbase, int cap)
        {
            Dictionary<SafeSquadWrapper, int> rangers = BaseInfo.DireRangersPerStarbase.GetDisplayDict();
            //ArcenDebugging.LogSingleLine("How many rangers does " + starbase.ToStringWithPlanet() + " have? We see " + rangers.GetPairCount() + " possibilities", Verbosity.DoNotShow );
            int guardCount = 0;
            foreach ( KeyValuePair<SafeSquadWrapper, int> guards in rangers )
            {
                //ArcenDebugging.LogSingleLine("Checking rangers to see if they match " + starbase.ToStringWithPlanet() + " checking against " + guards.Key.GetSquad().ToString() , Verbosity.DoNotShow );
                if ( guards.Key.GetSquad() == starbase)
                {
                    guardCount = guards.Value;
                    break;
                }
            }

            if ( guardCount >= cap )
                return false;
            return true;
        }
        private void HandleLocustsSim(ArcenHostOnlySimContext Context)
        {
            //If the Lure is gone, the locusts will begin to attrition
            FInt attritionPercent = FInt.FromParts(2, 000);
            foreach ( GameEntity_Squad locust in BaseInfo.Locusts.DisplaySquads() )
            {
                if ( locust == null )
                    continue;
                ArmadaPerUnitBaseInfo data = locust.TryGetExternalBaseInfoAs<ArmadaPerUnitBaseInfo>();
                if (data == null)
                {
                    continue;
                }
                if ( locust.HasQueuedOrders() )
                    continue; //this locust is probably en route to its lure
                if ( !BaseInfo.DoesPlanetHaveLure( data.LocustDestination ) )
                {
                    int damageToTake = ((attritionPercent * locust.GetMaxHullPoints()) / 100).IntValue;
                    locust.TakeDamageDirectly( damageToTake, null, null, DamageSource.BeingScrapped, Context);
                }
            }
        }
        private void HandleSwarmLures(ArcenHostOnlySimContext Context)
        {
            //the Swarm mechanic works like this.
            //The Armada player can build Swarm Launcher structures on their planets at the cost of
            //sockets. Maybe there's also a super launcher on the map?
            //Once the player builds a Swarm Lure on any planet, every so often all the
            //Swarm Launchers will all spawn a bunch of Locusts to attack the planet with the Lure
            //Lures attrition once they are winning the battle
            bool debug = false;
            if ( debug )
                ArcenDebugging.LogSingleLine("handle swarm lures", Verbosity.DoNotShow );
            foreach ( GameEntity_Squad lure in BaseInfo.SwarmLures.DisplaySquads() )
            {
                if ( debug )
                    ArcenDebugging.LogSingleLine("handling " + lure.ToStringWithPlanet() + " path A", Verbosity.DoNotShow );

                if (lure == null)
                    continue;
                ArmadaPerUnitBaseInfo data = lure.TryGetExternalBaseInfoAs<ArmadaPerUnitBaseInfo>();
                if (data == null)
                {
                    continue;
                }
                if ( debug )
                    ArcenDebugging.LogSingleLine("handling " + lure.ToStringWithPlanet() + " path B ", Verbosity.DoNotShow );

                if ( data.TimeForNextSwarmSummon <= World_AIW2.Instance.GameSecond )
                {
                    data.TimeForNextSwarmSummon = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.SwarmSpawnInterval;
                    SpawnLocusts( lure, Context);
                }
                {
                    //Lures will attrition when they are not in combat
                    EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> myFactionData = lure.Planet.GetStanceDataForFaction( AttachedFaction );
                    int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                    int alliedStrength = myFactionData[FactionStance.Friendly].TotalStrength + myFactionData[FactionStance.Self].TotalStrength;
                    FInt percent = FInt.Zero;
                    if ( hostileStrength > alliedStrength )
                        continue; //if we are outnumbered, no need to attrition
                    if ( hostileStrength <= 1 * 1000 )
                        percent = FInt.FromParts(1, 550);
                    else if ( hostileStrength < alliedStrength / 10 )
                        percent = FInt.FromParts(0, 700);
                    else
                    {
                        percent = FInt.FromParts(0, 150);
                    }

                    int bonus = lure.GetSecondsSinceCreation() / 120;
                    percent += bonus;
                    int damageToTake = ((percent * lure.GetMaxHullPoints()) / 100).GetNearestIntPreferringHigher();
                    lure.TakeDamageDirectly( damageToTake, null, null, DamageSource.BeingScrapped, Context);
                }

            }
        }
        private void SpawnLocusts(GameEntity_Squad lure, ArcenHostOnlySimContext Context)
        {
            foreach ( GameEntity_Squad launcher in BaseInfo.SwarmLaunchers.DisplaySquads() )
            {
                string tag = "";
                if ( launcher.TypeData.GetHasTag("SpawnsKilrathiLocusts"))
                    tag = "KilrathiLocust";
                else if ( launcher.TypeData.GetHasTag("SpawnsTerranLocusts"))
                {
                    tag = "TerranLocust";
                }
                else if ( launcher.TypeData.GetHasTag("SpawnsAprahantiLocusts"))
                    tag = "AprahantiLocust";
                else if ( launcher.TypeData.GetHasTag("SpawnsSkrinLocusts"))
                    tag = "SkrinLocust";
                else if ( launcher.TypeData.GetHasTag("SpawnsReaperLocusts"))
                    tag = "ReaperLocust";
                else if ( launcher.TypeData.GetHasTag("SpawnsAcutianLocusts"))
                    tag = "AcutianLocust";

                else
                    throw new Exception("launcher " + launcher.ToStringWithPlanet() + " does not have a locust tag");
                for ( int i = 0; i < BaseInfo.Income.LocustsSummoned; i++ )
                {
                    GameEntityTypeData typedata = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, tag);
                    PlanetFaction pFaction = launcher.PlanetFaction;
                    ArcenPoint spawnLocation = launcher.Planet.GetSafePlacementPoint_AroundEntity( Context, typedata, launcher, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 200 ) );
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typedata, 1,
                        pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "Armada-SpawnLocust" );
                    if ( newEntity != null )
                    {
                        newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
                        ArmadaPerUnitBaseInfo newData = newEntity.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>("ArmadaPerUnitBaseInfo");
                        if ( newData != null )
                            newData.LocustDestination = lure.Planet;
                    }
                }
            }
        }
        private void SpawnRangers(ArcenHostOnlySimContext Context)
        {
            if (BaseInfo.Starbases.Count == 0)
                return;
            bool debug = false;
            foreach ( GameEntity_Squad starbase in this.BaseInfo.RangerProducers.DisplaySquads() )
            {
                if (starbase == null)
                    continue;
                ArmadaPerUnitBaseInfo data = starbase.TryGetExternalBaseInfoAs<ArmadaPerUnitBaseInfo>();
                if (data == null)
                {
                    continue;
                }
                //ArcenDebugging.LogSingleLine("checking if we can build rangers for " + starbase.ToStringWithPlanet(), Verbosity.DoNotShow );
                if ( starbase.GetIsCrippled())
                    continue;
                int rangerIncrease = 0;
                int direRangerIncrease = 0;
                foreach ( GameEntity_Squad outpost in this.BaseInfo.RangerOutposts.DisplaySquads() )
                {
                    if ( outpost.Planet != starbase.Planet )
                        continue;
                    rangerIncrease += this.BaseInfo.Income.OutpostForRangerCap;
                }
                foreach ( GameEntity_Squad outpost in this.BaseInfo.DireRangerOutposts.DisplaySquads() )
                {
                    if ( outpost.Planet != starbase.Planet )
                        continue;
                    direRangerIncrease += this.BaseInfo.Income.OutpostForDireRangerCap;
                }
                //we only get income if we've specc'ed into these
                if ( rangerIncrease > 0 )
                {
                    data.RangerMetal += BaseInfo.Income.RangerIncomePerSecond;
                }
                if ( direRangerIncrease > 0 )
                {
                    data.DireRangerMetal += BaseInfo.Income.DireRangerIncomePerSecond;
                }

                if (data.RangerMetal <= 0 &&
                     data.DireRangerMetal <= 0)
                    continue;

                if ( starbase.TypeData.GetHasTag("MajorStarbase"))
                    data.RangerCap = this.BaseInfo.Difficulty.MaxRangersPerMajor;
                else
                    data.RangerCap = this.BaseInfo.Difficulty.MaxRangersPerMinor;

                if ( starbase.TypeData.GetHasTag("MajorStarbase"))
                    data.DireRangerCap = this.BaseInfo.Difficulty.MaxDireRangersPerMajor;
                else
                    data.DireRangerCap = this.BaseInfo.Difficulty.MaxDireRangersPerMinor;
                data.DireRangerCap += direRangerIncrease;
                data.RangerCap += rangerIncrease;

                string tag = "";
                GameEntityTypeData typeData;
                if (data.RangerMetal > 0)
                {
                    //try to build some rangers
                    if (starbase.TypeData.GetHasTag("GeneratesTerranRangers"))
                        tag = "TerranBaseRanger";
                    if (starbase.TypeData.GetHasTag("GeneratesKilrathiRangers"))
                        tag = "KilrathiBaseRanger";
                    if (starbase.TypeData.GetHasTag("GeneratesAprahantiRangers"))
                        tag = "AprahantiBaseRanger";
                    if (starbase.TypeData.GetHasTag("GeneratesSkrinRangers"))
                        tag = "SkrinBaseRanger";
                    if (starbase.TypeData.GetHasTag("GeneratesReaperRangers"))
                        tag = "ReaperBaseRanger";
                    if (starbase.TypeData.GetHasTag("GeneratesAcutianRangers"))
                        tag = "AcutianBaseRanger";

                    typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, tag);
                    if (typeData == null)
                        throw new Exception("Could not find ranger with tag " + tag + ".");
                    if (data.RangerMetal >= typeData.CostForAIToPurchase)
                    {
                        data.RangerMetal -= typeData.CostForAIToPurchase;
                        if (CanBuildRanger(starbase, data.RangerCap))
                        {
                            ArcenPoint spawnLocation = starbase.Planet.GetSafePlacementPoint_AroundEntity(Context, typeData, starbase, FInt.FromParts(0, 005), FInt.FromParts(0, 010));
                            GameEntity_Squad ranger = this.AttachedFaction.SpawnNewUnit_ReturnNullIfMPClient(
                              Context, starbase.Planet, spawnLocation, typeData, starbase.CurrentMarkLevel,
                              this.AttachedFaction.LooseFleet, 0,
                              EntityBehaviorType.Attacker_Full, -1, null, "Armada-Ranger");
                            ArmadaPerUnitBaseInfo newData = ranger.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>("ArmadaPerUnitBaseInfo");
                            newData.HomeStarbase = LazyLoadSquadWrapper.Create(starbase);
                        }
                        else
                        {
                            //ArcenDebugging.LogSingleLine("Skipping building a ranger from " + starbase.ToString() + " because we already have " + this.BaseInfo.RangerCount.Display + "  updated metal: " + data.RangerMetal, Verbosity.DoNotShow);
                        }
                    }
                }
                if (data.DireRangerMetal > 0)
                {
                    tag = "";
                    if (starbase.TypeData.GetHasTag("GeneratesTerranDireRangers"))
                        tag = "TerranDireRanger";
                    else if (starbase.TypeData.GetHasTag("GeneratesKilrathiDireRangers"))
                        tag = "KilrathiDireRanger";
                    else if (starbase.TypeData.GetHasTag("GeneratesAprahantiDireRangers"))
                        tag = "AprahantiDireRanger";
                    else if (starbase.TypeData.GetHasTag("GeneratesSkrinDireRangers"))
                        tag = "SkrinDireRanger";

                    else if (starbase.TypeData.GetHasTag("GeneratesReaperDireRangers"))
                        tag = "ReaperDireRanger";
                    else if (starbase.TypeData.GetHasTag("GeneratesAcutianDireRangers"))
                        tag = "AcutianDireRanger";

                    else
                        throw new Exception(starbase.ToStringWithPlanet() + " is missing tags to spawn dire rangers ");
                    typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, tag);
                    if (typeData == null)
                        throw new Exception("Could not find dire ranger with tag " + tag + " when considering " + starbase.ToStringWithPlanet());
                    if (data.DireRangerMetal >= typeData.CostForAIToPurchase)
                    {
                        data.DireRangerMetal -= typeData.CostForAIToPurchase;
                        if (CanBuildDireRanger(starbase, data.DireRangerCap))
                        {
                            ArcenPoint spawnLocation = starbase.Planet.GetSafePlacementPoint_AroundEntity(Context, typeData, starbase, FInt.FromParts(0, 030), FInt.FromParts(0, 050));
                            GameEntity_Squad ranger = this.AttachedFaction.SpawnNewUnit_ReturnNullIfMPClient(
                              Context, starbase.Planet, spawnLocation, typeData, starbase.CurrentMarkLevel,
                              this.AttachedFaction.LooseFleet, 0,
                              EntityBehaviorType.Attacker_Full, -1, null, "Armada-DireRanger");
                            ArmadaPerUnitBaseInfo newData = ranger.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>("ArmadaPerUnitBaseInfo");
                            newData.HomeStarbase = LazyLoadSquadWrapper.Create(starbase);
                            if ( debug && ranger != null )
                                ArcenDebugging.LogSingleLine("spawningRanger " + ranger.ToStringWithPlanet() + " tag " + tag, Verbosity.DoNotShow );

                        }
                    }
                }
            }
        }

        // public void HandleJournalsAndTips( ArcenHostOnlySimContext Context )
        // {
        //     //Some entries are time-related
        //     if ( World_AIW2.Instance.GameSecond == 1 && GameSettings.Current.GetBoolBySetting( "ArmadaTipReminders" ) )
        //     {
        //         if ( World_AIW2.Instance.CampaignType.HarshnessRating > 100 )
        //         {
        //             ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "Armada Overview",
        //                                                 "STOP! The Armada is an entirely different playstyle than the Human Empire. You must relearn your skills and change your expectations.\n\nREAD! There's a lot of how-to-play and advice in the Tips tab in the left hand menu.\n\nENJOY! As this is a whole new game experience, think of yourself as not being in AI War 2 anymore.\n\nWARNING! Armada is not really supported on any game mode above Humanity Ascendant. It shouldn't break, but there may be unexpect problems. Play at your own risk. ", "Ok" );
        //         }
        //         else
        //         {
        //             ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "Armada Overview",
        //                                "STOP! The Armada is an entirely different playstyle than the Human Empire. You must relearn your skills and change your expectations.\n\nREAD! There's a lot of how-to-play and advice in the Tips tab in the left hand menu.\n\nENJOY! As this is a whole new game experience, think of yourself as not being in AI War 2 anymore.", "Ok" );
        //         }
        //     }

        //     //others are related to gameplay triggers we detect here
        //     if ( this.BaseInfo.Starbases.Count >= 3 && this.BaseInfo.NumShipyards < 2 )
        //         World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DS_Armada_ShipyardsAreCritical", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );

        //     if ( World_AIW2.Instance.GameSecond > 30 &&
        //         World_AIW2.Instance.GameSecond % 65 == 0 )
        //     {
        //         //Check if we have unused starbase modules, and give a log if we do
        //         //We could also check flagships, but I'm concerned about the player who haven't used modules at all
        //         bool foundUnusedModules = false;
        //         this.BaseInfo.Starbases.Display_DF( delegate ( GameEntity_Squad city ) 
        //         {
        //             if ( city == null )
        //                 return DelReturn.Continue;
        //             if ( !city.TypeData.IsModular )
        //                 return DelReturn.Continue;
        //             if ( city.FleetMembership.ForMark.ModulePointsAvailable > 0 )
        //             {
        //                 foundUnusedModules = true;
        //                 return DelReturn.Break;
        //             }
        //             return DelReturn.Continue;
        //         } );
        //         if ( foundUnusedModules )
        //             World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DS_Armada_SpendModules", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
        //     }
        //     //TODO: checks for 'have we invested in totem tech' and 'have we invested in tower defense tech
        //}

        public void RemindAboutTipsSidebarIfNecessary( ArcenHostOnlySimContext Context )
        {
            // //Every 5 minutes, remind the player about the Tips sidebar if they haven't looked at any Tips
            // int reminderInterval = 300;
            // if ( ! GameSettings.Current.GetBoolBySetting( "ArmadaTipReminders" ) )
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

        #region RecalculateArmadaStarbaseBuildingContents_MainThreadSimOnly
        public void RecalculateArmadaStarbaseBuildingContents_MainThreadSimOnly( ArcenHostOnlySimContext Context )
        { 
            int debugCode = 0;
            bool debug = false;
            try
            {
                List<GameEntityTypeData> armadaStarbaseBuildings = GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( "ArmadaBuildMenu" );
                if ( armadaStarbaseBuildings == null || armadaStarbaseBuildings.Count <= 0 )
                    return;
                debugCode = 100;
                foreach ( GameEntity_Squad starbase in this.BaseInfo.Starbases.DisplaySquads() )
                {
                    debugCode = 200;
                    if ( starbase == null || starbase.FleetMembership == null )
                        continue;
                    debugCode = 300;
                    Fleet starbaseCityFleet = starbase.GetFleetOrNull_Safe();
                    if ( starbaseCityFleet == null ) {
                        continue;
                    }

                    debugCode = 350;
                    if ( starbase.GetIsCrippled() || starbase.GetIsNonFunctional() )
                    {
                        debugCode = 400;
                        for ( int j = 0; j < armadaStarbaseBuildings.Count; j++ )
                        {
                            FleetMembership fleetMem = starbaseCityFleet.GetButDoNotAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( armadaStarbaseBuildings[j] );
                            if ( fleetMem != null ) //can't build anything!
                                fleetMem.ExplicitBaseSquadCap = 0;
                        }
                        //ArcenDebugging.ArcenDebugLog( "Starbase for fleet " + starbase.GetFleetName_Safe() + " is disabled.", Verbosity.DoNotShow );
                        continue; //no upgrades for me right now, since I'm nonfunctional
                    }
                    debugCode = 500;
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("Figuring out what " + starbase.ToStringWithPlanet() + " can build", Verbosity.DoNotShow );
                    int starbaseMarkLevel = starbase.CurrentMarkLevel;
                    //okay, make sure we CAN build things based on the mark level of the starbase!
                    for ( int j = 0; j < armadaStarbaseBuildings.Count; j++ )
                    {
                        debugCode = 700;
                        GameEntityTypeData buildingInfo = armadaStarbaseBuildings[j];
                        if ( buildingInfo == null )
                            continue;
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tChecking if we can build " + buildingInfo.GetDisplayName(), Verbosity.DoNotShow );
                        //some building types are only available to some starbase types
                        if ( buildingInfo.GetHasTag("KilrathiBuildMenuOnly") && !starbase.TypeData.GetHasTag("Kilrathi"))
                            continue;
                        if ( buildingInfo.GetHasTag("TerranBuildMenuOnly") && !starbase.TypeData.GetHasTag("ArmadaTerran"))
                            continue;
                        if ( buildingInfo.GetHasTag("AprahantiBuildMenuOnly") && !starbase.TypeData.GetHasTag("ArmadaAprahanti"))
                        {
                            // if ( debug )
                            // ArcenDebugging.LogSingleLine("\t\tbuilding has AprahantiBuildMenuOnly but  starbase doenst have ArmadaAprahanti", Verbosity.DoNotShow );
                            continue;
                        }
                        if ( buildingInfo.GetHasTag("SkrinBuildMenuOnly") && !starbase.TypeData.GetHasTag("ArmadaSkrin"))
                        {
                            // if ( debug )
                            // ArcenDebugging.LogSingleLine("\t\tbuilding has SkrinBuildMenuOnly but  starbase doenst have ArmadaSkrin", Verbosity.DoNotShow );
                            continue;
                        }
                        if ( buildingInfo.GetHasTag("ReaperBuildMenuOnly") && !starbase.TypeData.GetHasTag("ArmadaReaper"))
                            continue;
                        if ( buildingInfo.GetHasTag("AcutianBuildMenuOnly") && !starbase.TypeData.GetHasTag("ArmadaAcutian"))
                            continue;

                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\t\tHas the correct race", Verbosity.DoNotShow );

                        //Some structures are gated by Tier
                        FleetMembership fleetMem = null;
                        //if the building type requires a higher mark level of starbase, don't show it.
                        if ( buildingInfo.MinimumRequiredCityLevelForConstruction > starbaseMarkLevel
                             && !BaseInfo.StartWithAllUpgrades )
                        {
                            fleetMem = starbaseCityFleet.GetButDoNotAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( buildingInfo );
                            if ( fleetMem != null ) //can't build this!
                                fleetMem.ExplicitBaseSquadCap = 0;
                             // ArcenDebugging.ArcenDebugLogSingleLine( "Starbase for fleet " + city.GetFleetName_Safe() + " is only mark level " + starbaseMarkLevel + 
                             //     ", so skipping " + buildingInfo.DisplayName + ".", Verbosity.DoNotShow );
                            continue;
                        }
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tHas the right starbase mark level", Verbosity.DoNotShow );

                        debugCode = 800;
                        int capWeShouldStartWith = buildingInfo.BaseShipCapInCustomCity;
                        if ( buildingInfo.AddedShipCapInCustomCityPerCityLevel > 0 )
                        {
                            int levelsAboveBase = 0;
                            if ( buildingInfo.AddedShipCapInCustomCityPerCityLevel_AboveLevel > 0 )
                                levelsAboveBase = starbaseMarkLevel - buildingInfo.AddedShipCapInCustomCityPerCityLevel_AboveLevel;
                            if ( levelsAboveBase > 0 )
                                capWeShouldStartWith += (levelsAboveBase * buildingInfo.AddedShipCapInCustomCityPerCityLevel);
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine("\t\tPotentially adding more mark levels above base: " + levelsAboveBase, Verbosity.DoNotShow );

                        }
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tAnd we have cap " + capWeShouldStartWith, Verbosity.DoNotShow );

                        debugCode = 900;
                        fleetMem = starbaseCityFleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( buildingInfo );
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
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in RecalculateArmadaStarbaseBuildingContents_MainThreadSimOnly debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion
        
        public GameEntity_Squad SpawnArmadaStarbase( ArcenPoint spawnLocation, Planet planet, string TypeName, Faction faction, 
                                                            ArcenHostOnlySimContext Context, out GameEntity_Squad ArmadaFlagship, bool exactPlacement )
        {
            if ( !ArcenNetworkAuthority.GetIsHostMode() )
            {
                ArmadaFlagship = null;
                return null; //only for the host; clients will get this data sync'd to them later
            }
            GameEntityTypeData starbaseData = GameEntityTypeDataTable.Instance.GetRowByName( TypeName );
            if ( starbaseData == null ) {
                starbaseData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, TypeName );
                if ( starbaseData == null )
                    throw new Exception("Unable to find XML with name or tag " + TypeName);
            }
            GameEntityTypeData flagshipEntityData = null;
            if (starbaseData.GetHasTag("ArmadaMajorStarbase"))
            {
                if (starbaseData.GetHasTag("SpawnsTerranFlagship"))
                {
                    flagshipEntityData = GameEntityTypeDataTable.Instance.GetRowByName("TerranFlagshipTierOne");
                }
                else if (starbaseData.GetHasTag("SpawnsKilrathiFlagship"))
                {
                    flagshipEntityData = GameEntityTypeDataTable.Instance.GetRowByName("KilrathiFlagshipTierOne");
                }

                if (flagshipEntityData == null)
                    throw new Exception("Couldn't figure out which flagship to spawn from " + starbaseData.GetDisplayName());
            }
            if (flagshipEntityData == null)
                ArcenDebugging.LogSingleLine("flagship is null", Verbosity.DoNotShow );
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
            if ( spawnLocation == ArcenPoint.ZeroZeroPoint )
            {
                spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, starbaseData, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 300 ) );
            }
            else if ( exactPlacement )
                spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, starbaseData, spawnLocation, FInt.FromParts( 0, 010 ), FInt.FromParts( 0, 020 ) );
            else
                spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, starbaseData, spawnLocation, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 250 ) );
            GameEntity_Squad starbase = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, starbaseData, 1,
                    null, 0, spawnLocation, Context, "Armada-NewStarbase" );
            if ( starbase != null ) { }
            //I'd like to say "If this is a safe placement point, put the unit here. Otherwise place it "very close by"  This is the best way.
            GameEntity_Squad armadaFlagship = null;
            bool spawnsFlagship = flagshipEntityData != null;
            bool bolstersFlagship = starbaseData.GetHasTag("BolstersArmadaFleet");
            
            if ( spawnsFlagship )
            {
                spawnLocation = planet.GetSafePlacementPoint_AroundEntity( Context, flagshipEntityData, starbase, FInt.FromParts( 0, 005 ), FInt.FromParts( 0, 010 ) );

                armadaFlagship = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, flagshipEntityData, 1,
                           null, 0, spawnLocation, Context, "Armada-NewArmadaFlagship" );
                ArmadaFlagship = armadaFlagship;
                if ( armadaFlagship == null )
                    return starbase;
                armadaFlagship.FleetMembership.Fleet.IsFleetFlagshipAllowedToUseMovementModes = true;
            }
            else
            {
                ArmadaFlagship = null;
            }



            Fleet newArmadaStarbaseFleet = starbase.FleetMembership.Fleet;

            string nameBase = GetAvailableCityOrMoonName( starbase, faction, Context );

            newArmadaStarbaseFleet.NameRaw = "Starbase " + nameBase;
            newArmadaStarbaseFleet.FleetQualifier = "Armada";

            ArmadaCityFleetBaseInfo armadaCityFleetInfo = newArmadaStarbaseFleet.CreateExternalBaseInfo<ArmadaCityFleetBaseInfo>("ArmadaCityFleetBaseInfo");

            if ( bolstersFlagship )
            {
                armadaCityFleetInfo.CanChangeBolsteredFleet = true;
            }
            
            if ( spawnsFlagship )
            {
                Fleet newArmadaMobileFleet = armadaFlagship.FleetMembership.Fleet;
                newArmadaMobileFleet.NameRaw = "Armadafleet " + nameBase;
                newArmadaMobileFleet.FleetQualifier = "Armada";
                newArmadaMobileFleet.CreateExternalBaseInfo<ArmadaMobileFleetBaseInfo>( "ArmadaMobileFleetBaseInfo" );
                newArmadaStarbaseFleet.CityBolstersFleetID = newArmadaMobileFleet.FleetID;
            }
            //            ArcenDebugging.ArcenDebugLogSingleLine("Created a new fleet, " + newArmadaFleet.GetName() + " with flagship " + armadaFlagship.ToString(), Verbosity.DoNotShow );
            // List<GameEntityTypeData> InitialShipsForFlagship = GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( "ArmadaSummons" );
            // for ( int i = 0; i < InitialShipsForFlagship.Count; i++ )
            // {
            //     GameEntityTypeData entitydata = InitialShipsForFlagship[i];
            //     int nextUniqueID = newArmadaFleet.GetNextUniqueIntToUseOfMatchingMembershipGroupsBasedOnSquadType( entitydata );
            //     FleetMembership mem = newArmadaFleet.GetOrAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates( entitydata, nextUniqueID );
            //     mem.ExplicitBaseSquadCap = 1;
            // }

            this.BaseInfo.Starbases.AddToDisplayList(starbase);
            return starbase;
        }
        
        public GameEntity_Squad SpawnArmadaDrill(ArcenPoint spawnLocation, Planet planet, string TypeName, Faction faction,
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
                    null, 0, spawnLocation, Context, "Armada-NewDrill" );
            return drill;
        }
        public GameEntity_Squad SpawnArmadaSphere(ArcenPoint spawnLocation, Planet planet, string TypeName, Faction faction,
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
                    null, 0, spawnLocation, Context, "Armada-NewSphere" );

            //And now spawn the flagship
            GameEntityTypeData flagshipEntityData = null;
            string name = "";
            if ( sphereData.GetHasTag("ZenithSidekickSphere") )
                name = "ArmadaSphereZenithFlagship";
            else if ( sphereData.GetHasTag("SpireSidekickSphere"))
                name = "ArmadaSphereSpireFlagship";
            flagshipEntityData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( name );
            if ( flagshipEntityData == null )
                throw new Exception("Could not find a flagship for " + sphere.ToStringWithPlanet() + " with name " + name );
            spawnLocation = planet.GetSafePlacementPoint_AroundEntity( Context, flagshipEntityData, sphere, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 100 ) );

            GameEntity_Squad armadaFlagship = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, flagshipEntityData, 1,
                                                                             null, 0, spawnLocation, Context, "Armada-NewArmadaFlagship" );
            if ( armadaFlagship != null )
            {
                armadaFlagship.FleetMembership.Fleet.IsFleetFlagshipAllowedToUseMovementModes = true;
                armadaFlagship.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
            }
            return sphere;
        }
        /* Long Range Planning Functions */
        private static readonly List<SafeSquadWrapper> AutoDefendingShipsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ArmadaFactionDeepInfo-AutoDefendingShipsLRP" );
        private static readonly List<SafeSquadWrapper> TransportsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ArmadaFactionDeepInfo-TransportsLRP" );
        private static readonly List<SafeSquadWrapper> ArmadaProductionLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ArmadaFactionDeepInfo-ArmadaProductionLRP" );
        private static readonly List<SafeSquadWrapper> ArmadaProductionNeedingOrdersLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ArmadaFactionDeepInfo-ArmadaProductionNeedingOrdersLRP" );
        private static readonly List<SafeSquadWrapper> ShipyardsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ArmadaFactionDeepInfo-ShipyardsLRP" );
        private static readonly List<SafeSquadWrapper> StarbasesLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ArmadaFactionDeepInfo-StarbasesLRP" );
        private static readonly List<SafeSquadWrapper> DestForTransportsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ArmadaFactionDeepInfo-DestForTransportsLRP" );
        private static readonly List<SafeSquadWrapper> LocustsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ArmadaFactionDeepInfo-LocustsLRP" );
        private static readonly List<Planet> WorkingPlanetsLRP = List<Planet>.Create_WillNeverBeGCed( 500, "ArmadaFactionDeepInfo-WorkingPlanetsLRP" );
        private static readonly List<SafeSquadWrapper> SabresLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ArmadaFactionDeepInfo-SabresLRP" );

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            ShipyardsLRP.Clear();
            AutoDefendingShipsLRP.Clear();
            TransportsLRP.Clear();
            ArmadaProductionLRP.Clear();
            ArmadaProductionNeedingOrdersLRP.Clear();
            WorkingPlanetsLRP.Clear();
            LocustsLRP.Clear();
            SabresLRP.Clear();
            DestForTransportsLRP.Clear();
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            int debugCode = 0;
            try {
                debugCode = 100;
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads() )
            {
                debugCode = 200;
                if ( entity == null )
                    continue;
                debugCode = 300;
                if (entity.TypeData.GetHasTag("ArmadaTransportDestination"))
                {
                    DestForTransportsLRP.Add( entity );
                    continue;
                }
                if (entity.TypeData.GetHasTag("ArmadaLocust"))
                {
                    LocustsLRP.Add( entity );
                    continue;
                }
                if (  entity.TypeData.GetHasTag("TerranSabre") )
                {
                    SabresLRP.Add( entity );
                    continue;
                }

                debugCode = 400;
                if (entity.TypeData.GetHasTag("ArmadaFlagship") ||
                    entity.TypeData.GetHasTag("ArmadaSphereFlagship" ) ||
                    entity.TypeData.GetHasTag("AutoDefenseShip") )
                {
                    AutoDefendingShipsLRP.Add(entity);
                    continue;
                }
                if (entity.TypeData.GetHasTag("ArmadaTransport"))
                {
                    TransportsLRP.Add(entity);
                    continue;
                }

                if (entity.TypeData.GetHasTag("ArmadaStarbase"))
                {
                    StarbasesLRP.Add(entity);
                    continue;
                }
                debugCode = 500;
                int hostileStrength = entity.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                debugCode = 600;
                if (entity.TypeData.GetHasTag("ArmadaShipyard") && // entity.SelfBuildingMetalRemaining <= 0 &&
                    // entity.SecondsSpentAsRemains <= 0 &&
                    hostileStrength < 3000 ) //Don't try to rebuild a potentially damaged fleet on top of enemies
                {
                    ShipyardsLRP.Add(entity);
                    continue;
                }
            }
            debugCode = 700;
            int maxShips = 10;
            for (int i = 0; i < ArmadaProductionNeedingOrdersLRP.Count; i++ )
            {
                //This is ships created by the armada sphere win condition
                GameEntity_Squad squad = ArmadaProductionNeedingOrdersLRP[i].GetSquad();
                if ( squad == null )
                    continue;
                Planet planet = GetNextPlanetForArmadaProduction(squad, Context);
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
                    ArcenDebugging.LogSingleLine("No transportation destinations", Verbosity.DoNotShow );
                    break; //this shouldn't really be possible
                }

                if ( !squad.HasQueuedOrders() )
                {
                    debugCode = 1200;
                    GameEntity_Squad Dest = GetClosestDestinationForTransport( squad, Context, pathingCacheData );
                    Planet destPlanet = Dest.Planet;
                    ArcenPoint destPoint = Dest.WorldLocation;
                    if ( Dest.TypeData.IsMobile )
                    {
                        //handle Arks a bit differently
                        destPlanet = Dest.GetDestinationPlanet();
                        destPoint = Dest.CalculateDestinationPoint_Safe();
                    }
                    if ( squad.Planet != destPlanet )
                    {
                        AutoDefendUtility.GoToPlanet( squad, destPlanet, Context, pathingCacheData );
                    }
                    else
                    {
                        GoToLocation( squad, destPoint, Context, pathingCacheData);
                    }
                }
            }
            HandleSabresLRP(Context, pathingCacheData);
            HandleLocustsLRP(Context, pathingCacheData);
            debugCode = 1300;
            //Below is the armada auto-defend mode
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
                if ( defenseShip.TypeData.GetHasTag("ArmadaSphereFlagship") ||
                     defenseShip.TypeData.GetHasTag("AutoDefenseShip"))
                {
                    //we are always in auto-defend mode, since this is a Sphere flagship or an auto-defense ship
                }
                else if ( World_AIW2.Instance.Setup.GetStringBySetting("ArmadaAutoDefend") == "Disabled" )
                          //|| this.AttachedFaction.UnderPlayerControl())
                    continue; //auto defend is explicitly disabled, or the player is controlling
                Fleet fleet = defenseShip.FleetMembership.Fleet;
                if (fleet == null)
                {
                    continue;
                }
                ArmadaPerUnitBaseInfo unitData = defenseShip.TryGetExternalBaseInfoAs<ArmadaPerUnitBaseInfo>();
                GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                ArmadaMobileFleetBaseInfo fleetInfo = null;
                if ( defenseShip == centerpiece )
                {
                    fleetInfo = fleet.TryGetExternalBaseInfoAs<ArmadaMobileFleetBaseInfo>();
                    //the Sim code hasn't run to set up the mobile fleet info
                    if (fleetInfo == null)
                    {
                        continue;
                    }
                }

                Planet dest = defenseShip.GetDestinationPlanet();
                bool goToShipyard = false; //these double as "retreat"
                bool goToHomeStarbase = false;
                debugCode = 1500;
                int myStrength;
                if ( defenseShip.TypeData.GetHasTag("ArmadaFlagship") )
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
                        goToHomeStarbase = true;
                    else
                        goToShipyard = true; 
                }
                GameEntity_Squad homeStarbase = null;
                if ( unitData != null &&
                     defenseShip.TypeData.AllowedHopsFromCenterpiece > -1 )
                {
                    homeStarbase = unitData.HomeStarbase.GetSquad();
                    if ( homeStarbase.Planet.GetHopsTo( defenseShip.Planet ) > defenseShip.TypeData.AllowedHopsFromCenterpiece )
                        goToHomeStarbase = true;
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
                if (goToHomeStarbase && homeStarbase != null )
                {
                    Planet starbasePlanet = homeStarbase.Planet;
                    if (starbasePlanet != null)
                    {
                        if (debug)
                            ArcenDebugging.ArcenDebugLogSingleLine("\tHeading to " + starbasePlanet.Name + ", its home starbase", Verbosity.DoNotShow);

                        AutoDefendUtility.GoToPlanet(defenseShip, starbasePlanet, Context, pathingCacheData);
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
                if ( homeStarbase != null ) 
                    AutoDefendUtility.GetPlanetsUnderAttack( defenseShip, myStrength, homeStarbase.Planet, WorkingPlanetsLRP); //we are defending around a specific starbase; Guardians
                else
                    AutoDefendUtility.GetPlanetsUnderAttack(defenseShip, myStrength, defenseShip.Planet, WorkingPlanetsLRP); //we are "generic defense"; defensive flagships
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
                AutoDefendUtility.GetThreatenedPlanets( myStrength, defenseShip.Planet, WorkingPlanetsLRP, this.AttachedFaction);
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
                    Planet shipyardPlanet = AutoDefendUtility.GetRandomPlanetForPatrol(defenseShip, StarbasesLRP, WorkingPlanetsLRP, Context, pathingCacheData);
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
                ArcenDebugging.LogSingleLine("Exception in armada LRP debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            pathingCacheData.ReturnToPool();
            FleetBehaviorLRP.DoLRP( AttachedFaction, Context );
        }
        private void HandleLocustsLRP( ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathingCacheData )
        {
            int shipsThatCanMove = 40;
            for (int i = 0; i < LocustsLRP.Count; i++ )
            {
                GameEntity_Squad locust = LocustsLRP[i].GetSquad();
                if ( locust == null )
                    continue;
                ArmadaPerUnitBaseInfo data = locust.TryGetExternalBaseInfoAs<ArmadaPerUnitBaseInfo>();
                if ( data == null )
                    continue;
                if ( locust.HasQueuedOrders() )
                    continue; //we're going somewhere
                if ( shipsThatCanMove <= 0 )
                    break; //no more move commands
                if ( locust.Planet == data.LocustDestination )
                {
                    if ( BaseInfo.DoesPlanetHaveLure(locust.Planet))
                        continue; //we are summoned to this lure!
                    EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> myFactionData = locust.Planet.GetStanceDataForFaction( AttachedFaction );
                    int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                    int alliedStrength = myFactionData[FactionStance.Friendly].TotalStrength + myFactionData[FactionStance.Self].TotalStrength;
                    if ( hostileStrength < alliedStrength / 10 )
                    {
                        data.LocustDestination = GetNewLocustDestination( locust, Context );
                    }
                }
                if ( locust.Planet != data.LocustDestination )
                {
                    AutoDefendUtility.GoToPlanet(locust, data.LocustDestination, Context, pathingCacheData);
                    shipsThatCanMove--;
                }
            }
        }
        private void HandleSabresLRP( ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathingCacheData )
        {
            int shipsThatCanMove = 40;
            for (int i = 0; i < SabresLRP.Count; i++ )
            {
                GameEntity_Squad sabre = SabresLRP[i].GetSquad();
                if ( sabre == null )
                    continue;
                if ( sabre.HasQueuedOrders() )
                    continue; //we're going somewhere
                if ( shipsThatCanMove <= 0 )
                    break; //no more move commands
                EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> myFactionData = sabre.Planet.GetStanceDataForFaction( AttachedFaction );
                int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                int alliedStrength = myFactionData[FactionStance.Friendly].TotalStrength + myFactionData[FactionStance.Self].TotalStrength;
                if ( hostileStrength < alliedStrength / 10 )
                {
                    Planet destPlanet = GetNewLocustDestination( sabre, Context );
                    AutoDefendUtility.GoToPlanet(sabre, destPlanet, Context, pathingCacheData);
                    shipsThatCanMove--;
                }
            }
        }
        private Planet GetNewLocustDestination(GameEntity_Squad locust, ArcenLongTermIntermittentPlanningContext Context )
        {
            //this code is also used for Sabres
            Planet output = null;
            Planet fallback = null;
            foreach ( Planet neighbor in locust.Planet.LinkedNeighbors( false ) )
            {
                PlanetFaction pFaction = neighbor.GetPlanetFactionForFaction( AttachedFaction );
                if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength > 10 * 1000 )
                {
                    fallback = neighbor;
                    if ( Context.RandomToUse.Next( 0, 100) < 60 )
                    {
                        output = neighbor;
                        break;
                    }
                }
                if ( fallback == null || Context.RandomToUse.Next( 0, 100) < 50 )
                {
                    fallback = neighbor;
                }
            }
            if ( output == null )
                output = fallback;
            return output;
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
        
        private Planet GetNextPlanetForArmadaProduction( GameEntity_Squad squad, ArcenLongTermIntermittentPlanningContext Context)
        {
            WorkingPlanetsLRP.Clear();
            AutoDefendUtility.GetPlanetsUnderAttack( squad, 10 * 1000, squad.Planet, WorkingPlanetsLRP);
            if ( WorkingPlanetsLRP.Count == 0 )
                return null;
            cb_armadaProdSquad = squad;
            WorkingPlanetsLRP.Sort(static delegate (Planet Left, Planet Right)
            {
                int leftHops = Left.GetHopsTo(cb_armadaProdSquad.Planet);
                int rightHops = Right.GetHopsTo(cb_armadaProdSquad.Planet);
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
            ArmadaPerUnitBaseInfo data = flagship.TryGetExternalBaseInfoAs<ArmadaPerUnitBaseInfo>();
            if ( data == null )
                return;
            data.UnitsKilled++;
            } catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly Armada Sidekick debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            // if ( entity == null )
            //     return;
            // GameEntityTypeData entityType = entity.TypeData;
            // if ( entityType == null )
            //     return;
            // switch ( entityType.SpecialType )
            // {
            //     case SpecialEntityType.AICommandStationOriginal:
            //       World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Armada_Starbase", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            //       break;
            //     case SpecialEntityType.GuardPost:
            //     case SpecialEntityType.DireGuardPost:
            //       World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Armada_GuardPosts", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
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
                Faction armadaFaction = pFaction.Faction;
                debugIndex = 15;

                ArmadaFactionDeepInfo armadaDeepInfo = pFaction.Faction.GetExternalDeepInfoAs<ArmadaFactionDeepInfo>();
                ArmadaFactionBaseInfo armadaBaseInfo = armadaDeepInfo.BaseInfo;

                debugIndex = 20;

                stillNeedsToSeedHumanHomeworldStuff = false;

                GameEntity_Squad armadaFlagship = null;
                GameEntity_Squad armadaStarbase = null;
                //GameEntity_Squad armadaMoon = null;
                PlanetFaction pf = StartingPlanet.GetPlanetFactionForFaction( AttachedFaction );
                pf.AIPLeftFromCommandStation = 0;
                pf.AIPLeftFromWarpGate = 0;


                string startingStarbaseType = pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue("ArmadaStartingStarbase", false);

                if (startingStarbaseType == "Random")
                {
                    int rand = Context.RandomToUse.Next(0, 100);
                    ArcenDebugging.LogSingleLine("got random type; rand " + rand, Verbosity.DoNotShow );
                    if (rand < 50)
                        startingStarbaseType = "Terran";
                    else 
                        startingStarbaseType = "Kilrathi";
                }
                debugIndex = 30;
                Fleet armadaFleet = null;
                FInt placementOffsetScale = FInt.FromParts( 1, 500 );
                ArcenPoint spawnLocation = ArcenPoint.ZeroZeroPoint; //use this for "default placement"
                string kingTag = "ArmadaTerranKing";
                if (startingStarbaseType == "Terran")
                {
                    debugIndex = 40;
                    armadaStarbase = armadaDeepInfo.SpawnArmadaStarbase(spawnLocation, StartingPlanet, "StartingTerranStarbase", armadaFaction, Context, out armadaFlagship, false);
                    armadaFleet = armadaStarbase.GetFleetOrNull_Safe();
                    StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, armadaStarbase.WorldLocation, pFaction, armadaFleet, placementOffsetScale, "SpawnsWithTerranStarbase", 2400, 600 );
                }
                else if (startingStarbaseType == "Kilrathi")
                {
                    debugIndex = 50;
                    armadaStarbase = armadaDeepInfo.SpawnArmadaStarbase(spawnLocation, StartingPlanet, "StartingKilrathiStarbase", armadaFaction, Context, out armadaFlagship, false);
                    armadaFleet = armadaStarbase.GetFleetOrNull_Safe();
                    StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, armadaStarbase.WorldLocation, pFaction, armadaFleet, placementOffsetScale, "SpawnsWithKilrathiStarbase", 2400, 600 );
                    kingTag = "ArmadaKilrathiKing";
                }
                if (armadaStarbase == null)
                    throw new Exception("Cound not spawn initial starbase; choice was " + startingStarbaseType);
                debugIndex = 60;
                StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, armadaStarbase.WorldLocation, pFaction, armadaFleet, placementOffsetScale, "ArmadaShipyard", 3600, 1600 );

                spawnLocation = StartingPlanet.GetSafePlacementPointAroundPlanetCenter( Context, armadaStarbase.TypeData, FInt.FromParts( 0, 400 ), FInt.FromParts( 0, 600 ) );
                
                StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, spawnLocation, pFaction, armadaFleet, placementOffsetScale, kingTag, 3800, 600 );
                debugIndex = 70;
                if ( armadaFlagship != null )
                {
                    debugIndex = 80;
                    Fleet armadaFlagshipFleet = armadaFlagship.GetFleetOrNull_Safe();
                    GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    command.RelatedIntegers.Add( armadaFlagshipFleet.FleetID ); //FleetID
                    command.RelatedIntegers.Add( PlayerAccount.Local.PlayerPrimaryKeyID );
                    command.RelatedString = "ToggleIsFleetOnPlayerWatchlist";
                    command.RelatedBool = true;
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                    armadaFlagshipFleet.IsFleetInTransportLoadMode = false;

                    //GameEntityTypeData spawnType;

                    //60 skeleton base
                    // spawnType = GameEntityTypeDataTable.Instance.GetRowByName( "BaseSkeleton" );
                    // for ( int i = 0; i < 60; i++ )
                    //     armadaFlagship.SpawnEntity_ReturnNullIfMPClient( spawnType, 1, armadaFleet, 0, 0, Context, "ArmadaStart", false );

                    // //18 wight base
                    // spawnType = GameEntityTypeDataTable.Instance.GetRowByName( "BaseWight" );
                    // for ( int i = 0; i < 18; i++ )
                    //     armadaFlagship.SpawnEntity_ReturnNullIfMPClient( spawnType, 1, armadaFleet, 0, 0, Context, "ArmadaStart", false );
                }

                debugIndex = 90;
                //ArcenDebugging.LogSingleLine("TODO: write journal", Verbosity.DoNotShow );
                // World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DS_Armada_Lore", string.Empty, armadaFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Armada_Introduction", string.Empty, armadaFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                // World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DS_Armada_Economy", string.Empty, armadaFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                // World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DS_Armada_Coalition", string.Empty, armadaFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                // World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DS_Armada_Enemies", string.Empty, armadaFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );

                debugIndex = 100;
                armadaFaction.StoredMetal = (FInt)(850 * 1000);
                armadaFaction.StoredScience = FInt.FromParts( 6000, 000 );
                armadaFaction.StoredHacking = FInt.FromParts( 60, 000 );
                armadaFaction.StoredFactionResourceOne = FInt.FromParts( 35, 000 ); //enough to get things started.
                if ( armadaBaseInfo.BonusStartingResources > 0 )
                {
                    armadaFaction.StoredScience += FInt.FromParts( 1000, 000 ) * armadaBaseInfo.BonusStartingResources;
                    armadaFaction.StoredHacking += FInt.FromParts( 20, 000 ) * armadaBaseInfo.BonusStartingResources;
                    armadaFaction.StoredFactionResourceOne += FInt.FromParts( 10, 000 ) * armadaBaseInfo.BonusStartingResources;
                };
            
                //At the beginning of the game the player gets one lowest-tier skeleton type and wight variant type
                //the goal of this is to let the player start out a bit stronger
                // debugIndex = 200;
                // string tagAndFieldName = "ArmadaSkeletonGroup";
                // ArmadaUpgrade upgrade = ArmadaUpgradeTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( tagAndFieldName, false ) );
                // if ( upgrade == null )
                // {
                //     upgrade = ArmadaUpgradeTable.Instance.GetRandomBonusStartingSkeletonType( Context );
                // }
                // necroBaseInfo.ArmadaCompletedUpgrades.Add( upgrade );

                // debugIndex = 400;
                // ArmadaUpgradeEvent thisEvent = ArmadaUpgradeEvent.Create( necroFaction.FactionIndex, -1, StartingPlanet.Index, upgrade.Index, -1, null );
                // necroBaseInfo.ArmadaHistory.Add( thisEvent );
                // StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, necropolis.WorldLocation, pFaction, necroCity, placementOffsetScale, upgrade.ShipForCapIncrease, 700, 600 );

                // debugIndex = 500;
                // tagAndFieldName = "ArmadaWightGroup";
                // upgrade = ArmadaUpgradeTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( tagAndFieldName, false ) );
                // if ( upgrade == null )
                // {
                //     upgrade = ArmadaUpgradeTable.Instance.GetRandomBonusStartingWightType( Context );
                // }
                // necroBaseInfo.ArmadaCompletedUpgrades.Add( upgrade );
                // debugIndex = 600;
                // thisEvent = ArmadaUpgradeEvent.Create( necroFaction.FactionIndex, -1, StartingPlanet.Index, upgrade.Index, -1, null );
                // necroBaseInfo.ArmadaHistory.Add( thisEvent );
                // StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, necropolis.WorldLocation, pFaction, necroCity, placementOffsetScale, upgrade.ShipForCapIncrease, 1600, 600 );

                // debugIndex = 650;
                // tagAndFieldName = "ArmadaUtilityGroup";
                // upgrade = ArmadaUpgradeTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( tagAndFieldName, false ) );
                // if ( upgrade == null )
                // {
                //     upgrade = ArmadaUpgradeTable.Instance.GetRandomBonusStartingUtilityType( Context );
                // }
                // necroBaseInfo.ArmadaCompletedUpgrades.Add( upgrade );
                // debugIndex = 600;
                // thisEvent = ArmadaUpgradeEvent.Create( necroFaction.FactionIndex, -1, StartingPlanet.Index, upgrade.Index, -1, null );
                // necroBaseInfo.ArmadaHistory.Add( thisEvent );
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
                //         upgrade = ArmadaUpgradeTable.Instance.GetRandomBonusStartingWightType( Context );
                //     } while ( necroBaseInfo.ArmadaCompletedUpgrades.Contains( upgrade ) );
                //     necroBaseInfo.ArmadaCompletedUpgrades.Add( upgrade );
                //     thisEvent = ArmadaUpgradeEvent.Create( necroFaction.FactionIndex, -1, StartingPlanet.Index, upgrade.Index, -1, null );
                //     necroBaseInfo.ArmadaHistory.Add( thisEvent );
                // }
                // if ( necroBaseInfo.StartWithAllUpgrades )
                // {
                //     while ( true )
                //     {
                //         upgrade = ArmadaUpgradeTable.Instance.GetNextFactionUpgrade( );
                //         if ( upgrade == null )
                //             break;
                //         necroBaseInfo.ArmadaCompletedUpgrades.Add( upgrade );
                //         thisEvent = ArmadaUpgradeEvent.Create( necroFaction.FactionIndex, -1, StartingPlanet.Index, upgrade.Index, -1, null );
                //         necroBaseInfo.ArmadaHistory.Add( thisEvent );
                //     }
                // }

                debugIndex = 2000;
            }
            catch ( Exception e )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "ArmadaSpecificCodeDeepInfo SeedUnitsOnStartingPlanetDuringMapGen error at debugIndex " + debugIndex + ": " + e );
                return false;
            }
            return true;
        }

        public override void SeedSpecialEntities_LateAfterAllFactionSeeding_CustomForPlayerType( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData MapData )
        {
            //IList<Planet> planetsSeeded;

            //I'd like to only seed two extra Starbase types per game
            int firstStarbase = Context.RandomToUse.Next(0, 4);
            int secondStarbase = Context.RandomToUse.Next(0, 4);
            int thirdStarbase = Context.RandomToUse.Next(0, 4);
            while ( secondStarbase == firstStarbase )
            {
                secondStarbase = Context.RandomToUse.Next(0, 4);
            }
            while ( thirdStarbase == firstStarbase || thirdStarbase == secondStarbase )
            {
                thirdStarbase = Context.RandomToUse.Next(0, 4);
            }
            string firstTag = "MinorAprahantiStarbase";
            string secondTag = "MinorReaperStarbase";
            string thirdTag = "MinorAcutianStarbase";
            if ( firstStarbase == 0 )
            {
                firstTag = "MinorAprahantiStarbase";
            }
            else if ( firstStarbase == 1)
            {
                firstTag = "MinorSkrinStarbase";
            }
            else if ( firstStarbase == 2 )
            {
                firstTag = "MinorReaperStarbase";
            }
            else if ( firstStarbase == 3 )
            {
                firstTag = "MinorAcutianStarbase";
            }
            if ( secondStarbase == 0 )
            {
                secondTag = "MinorAprahantiStarbase";
            }
            else if ( secondStarbase == 1)
            {
                secondTag = "MinorSkrinStarbase";
            }
            else if ( secondStarbase == 2 )
            {
                secondTag = "MinorReaperStarbase";
            }
            else if ( secondStarbase == 3 )
            {
                secondTag = "MinorAcutianStarbase";
            }
            if ( thirdStarbase == 0 )
            {
                thirdTag = "MinorAprahantiStarbase";
            }
            else if ( thirdStarbase == 1)
            {
                thirdTag = "MinorSkrinStarbase";
            }
            else if ( thirdStarbase == 2 )
            {
                thirdTag = "MinorReaperStarbase";
            }
            else if ( thirdStarbase == 3 )
            {
                thirdTag = "MinorAcutianStarbase";
            }
            
            foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
            {
                PlayerTypeData playerType = player.PlayerTypeDataOrNull_ModeratelyExpensive;
                if ( playerType == null )
                    continue;
                if ( playerType.GetHasTag("ArmadaEmpire") )
                {
                    //Ther armada gets some bonus capturable starbases
                    // StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "SeededArmadaStarbase", SeedingType.CapturableWeightsAndMax,
                        //     seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 2, 3, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
                    //one near the player

                    int seedThisMany = 1;

                    seedThisMany = 1;
                    StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, firstTag, SeedingType.CapturableWeightsAndMax,
                        seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 2, 4, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
                     seedThisMany = 1;
                     StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, secondTag, SeedingType.CapturableWeightsAndMax,
                         seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 2, 4, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );

                    //and some further away
                    seedThisMany = 3;
                    StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, firstTag, SeedingType.CapturableWeightsAndMax,
                        seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 4, 99, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
                    seedThisMany = 2;
                    StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, secondTag, SeedingType.CapturableWeightsAndMax,
                        seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 4, 99, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
                    seedThisMany = 2;
                    StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, thirdTag, SeedingType.CapturableWeightsAndMax,
                        seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 4, 99, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );

                    //Seed some Minor Producers
                    //Seed one right near the player
                    seedThisMany = 1;
                    string minorTag="MinorArmadaProducer";
                    StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, minorTag, SeedingType.CapturableWeightsAndMax,
                        seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 1, 2, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );

                    //And more through the galaxy
                    seedThisMany = 5;
                    StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, minorTag, SeedingType.CapturableWeightsAndMax,
                        seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 4, 99, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
                    //And Major Producers
                    string majorTag="MajorArmadaProducer";
                    seedThisMany = 2;
                    StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, majorTag, SeedingType.CapturableWeightsAndMax,
                        seedThisMany, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 4, 99, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );

                }
            }
        }
        public GameEntity_Squad SpawnArmadaMine(ArcenPoint spawnLocation, Planet planet, string TypeName, Faction faction,
                                                 ArcenHostOnlySimContext Context, bool exactPlacement)
        {
            if (!ArcenNetworkAuthority.GetIsHostMode())
            {
                return null; //only for the host; clients will get this data sync'd to them later
            }

            GameEntityTypeData mineData = GameEntityTypeDataTable.Instance.GetRowByName(TypeName);
            if (mineData == null)
            {
                mineData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, TypeName);
                if (mineData == null)
                    throw new Exception("Unable to find XML with name or tag " + TypeName);
            }

            if ( spawnLocation == ArcenPoint.ZeroZeroPoint )
            {
                spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, mineData, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 300 ) );
            }
            else if ( exactPlacement )
                spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, mineData, spawnLocation, FInt.FromParts( 0, 010 ), FInt.FromParts( 0, 020 ) );
            else
                spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, mineData, spawnLocation, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 250 ) );
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction(this.AttachedFaction);
            GameEntity_Squad mine = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, mineData, 1,
                null, 0, spawnLocation, Context, "Dyson-NewMine" );
            if ( mine != null )
            {
                ArmadaPerUnitBaseInfo data = mine.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>("ArmadaPerUnitBaseInfo");
                if ( data == null )
                    throw new Exception("Could not create PerUnit for mine");
                int mineTime = 0;
                if ( mineData.GetHasTag("ArmadaMetalMine") || mineData.GetHasTag("ArmadaGreaterMetalMine"))
                    mineTime = this.BaseInfo.Income.MetalMineTime;
                else if  ( mineData.GetHasTag("ArmadaHackingMine"))
                    mineTime = this.BaseInfo.Income.HaPMineTime;
                else if  ( mineData.GetHasTag("ArmadaScienceMine"))
                    mineTime = this.BaseInfo.Income.ScienceMineTime;
                else
                {
                    throw new Exception("Unable to figure out what mine type we have; " + TypeName);
                }

                int mineTier = 1;
                if ( mineData.GetHasTag( "ArmadaMineT4" ) ) mineTier = 4;
                else if ( mineData.GetHasTag( "ArmadaMineT3" ) ) mineTier = 3;
                else if ( mineData.GetHasTag( "ArmadaMineT2" ) ) mineTier = 2;
                mineTime = mineTime * ( 100 - ( mineTier - 1 ) * ArmadaIncome.PercentMineSpeedIncreasePerTier ) / 100;

                data.MineFinishTime = mineTime + World_AIW2.Instance.GameSecond;
            }
            return mine;
        }
        public GameEntity_Squad SpawnArmadaStructure(ArcenPoint spawnLocation, Planet planet, string TypeName, Faction faction,
                                                 ArcenHostOnlySimContext Context, bool exactPlacement)
        {
            GameEntity_Squad structure = null;
            int debugCode = 0;
            try{
                debugCode = 100;
                if (!ArcenNetworkAuthority.GetIsHostMode())
                {
                    return null; //only for the host; clients will get this data sync'd to them later
                }
                debugCode = 200;
                GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRowByName(TypeName);
                if (typeData == null)
                {
                    typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, TypeName);
                    if (typeData == null)
                        throw new Exception("Unable to find XML with name or tag " + TypeName);
                }
                debugCode = 300;
                if ( spawnLocation == ArcenPoint.ZeroZeroPoint )
                {
                    spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, typeData, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 300 ) );
                }
                else if ( exactPlacement )
                    spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, typeData, spawnLocation, FInt.FromParts( 0, 010 ), FInt.FromParts( 0, 020 ) );
                else
                    spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, typeData, spawnLocation, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 250 ) );
                debugCode = 400;
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction(this.AttachedFaction);
                structure = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typeData, 1,
                    null, 0, spawnLocation, Context, "Dyson-NewStructure" );
                debugCode = 500;
                if ( structure != null )
                {
                    debugCode = 600;
                    ArmadaPerUnitBaseInfo perUnitData = structure.CreateExternalBaseInfo<ArmadaPerUnitBaseInfo>("ArmadaPerUnitBaseInfo");
                    debugCode = 700;
                    if ( perUnitData != null )
                    {
                        ArcenDebugging.LogSingleLine("got perunit", Verbosity.DoNotShow );
                        debugCode = 800;
                        if ( typeData.GetHasTag("SwarmLure"))
                            perUnitData.TimeForNextSwarmSummon = World_AIW2.Instance.GameSecond;
                    }
                    else
                    ArcenDebugging.LogSingleLine("no perunit", Verbosity.DoNotShow );
                }
            } catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in SpawnArmadaStructure in DeepInfo. Debug code " + debugCode + " exception " + e.ToString(), Verbosity.DoNotShow );
            } 
            return structure;
        }
        private void GetEnemyIncome(ArcenHostOnlySimContext Context)
        {
            FInt income = BaseInfo.Difficulty.EnemyIncomePerSecond;
            FInt aipIncome = FactionUtilityMethods.Instance.GetCurrentAIP() / 10 * BaseInfo.Difficulty.EnemyPerSecondPer10AIP;
            BaseInfo.StrengthForNextEnemyAttack += (income + aipIncome).IntValue;
        }
        private void SpawnCPA(ArcenHostOnlySimContext Context)
        {
            bool debug = false;
            if ( BaseInfo.TimeForNextEnemyAttack <= 0 )
            {
                //first attack
                BaseInfo.TimeForNextEnemyAttack = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.AIResponseInterval;
            }
            if ( World_AIW2.Instance.GameSecond % 60 == 0 )
            {
                int time = BaseInfo.TimeForNextEnemyAttack - World_AIW2.Instance.GameSecond;
            }
            if ( World_AIW2.Instance.GameSecond < BaseInfo.TimeForNextEnemyAttack )
                return;
            Faction aiFaction = World_AIW2.GetRandomAIFaction( Context );
            if ( aiFaction == null )
                return;
            GameEntity_Squad king = FactionUtilityMethods.Instance.findKing( aiFaction );
            if ( king == null )
                return;
            //we've picked a king, now update the response time
            BaseInfo.TimeForNextEnemyAttack = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.AIResponseInterval + Context.RandomToUse.Next(60,600);
            AISentinelsFactionBaseInfo aiBaseInfo = aiFaction.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
            PlanetFaction pFaction = king.PlanetFaction;
            pFaction = king.Planet.GetPlanetFactionForFaction(aiBaseInfo.SubFac_CPA );
            //Bonus to CPA from existing tiberium veins
            Faction tiberiumFaction = FactionUtilityMethods.Instance.GetTiberiumFaction();
            if ( tiberiumFaction != null )
            {
                TiberiumFactionBaseInfo tiberiumBaseInfo = tiberiumFaction.GetExternalBaseInfoAs<TiberiumFactionBaseInfo>();
                if ( tiberiumBaseInfo != null )
                {
                    foreach ( GameEntity_Squad vein in tiberiumBaseInfo.Veins.DisplaySquads() )
                    {
                        if ( vein == null )
                            continue;
                        int markLevel = vein.CurrentMarkLevel;
                        this.BaseInfo.StrengthForNextEnemyAttack += BaseInfo.Difficulty.EnemyResponsePerVeinPerMarkLevel * markLevel;
                    }
                }
            }
            ArcenDebugging.LogSingleLine("Launching anti-Armada CPA with strength " + this.BaseInfo.StrengthForNextEnemyAttack, Verbosity.DoNotShow );
            int planetsToLaunchFrom = 10;
            int strengthPerPlanet = this.BaseInfo.StrengthForNextEnemyAttack / planetsToLaunchFrom;
            string enemyTag = "TiberiumCPATierOne";
            if ( FactionUtilityMethods.Instance.GetCurrentAIP() > BaseInfo.Difficulty.AIPForHarderCPAs )
                 enemyTag = "TiberiumCPATierTwo";
            if ( debug )
                ArcenDebugging.LogSingleLine("Now lets try to spawn our forces; " + strengthPerPlanet + " per planet", Verbosity.DoNotShow );
            {
                //Spawn a Tiberium Desolation at this AI king
                PlanetFaction pFactionCPA = king.Planet.GetPlanetFactionForFaction( aiBaseInfo.SubFac_CPA );
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "TiberiumDesolation" );
                ArcenPoint spawnLocation = king.Planet.GetSafePlacementPoint_AroundEntity( Context, entityData, king, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 250 ) );
                GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFactionCPA, entityData, (byte)7,
                    pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "SpawnAntiArmadaCPA" );
            }
            foreach ( GameEntity_Squad warpGate in aiFaction.Squads( EntityRollupType.WarpEntryPoints ) )
            {
                if ( warpGate.Planet.MarkLevelForAIOnly.Ordinal < 4 )
                    continue;
                int strengthSpent = 0;
                PlanetFaction pFactionCPA = warpGate.Planet.GetPlanetFactionForFaction( aiBaseInfo.SubFac_CPA );


                while ( strengthSpent < strengthPerPlanet  )
                {
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, enemyTag );
                    ArcenPoint spawnLocation = warpGate.Planet.GetSafePlacementPoint_AroundEntity( Context, entityData, warpGate, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 250 ) );
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFactionCPA, entityData, pFaction.Faction.CurrentGeneralMarkLevel,
                        pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "SpawnAntiArmadaCPA" );
                    if ( newEntity == null )
                        continue;
                    if ( debug )
                        ArcenDebugging.LogSingleLine("Spawned " + newEntity.ToStringWithPlanet(), Verbosity.DoNotShow );
                    strengthSpent += entityData.CostForAIToPurchase;
                    newEntity.HullPointsLost = 0;
                    newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                }
                {
                    //Also spawn a Tiberium Dire Guardian for the CPA
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "TiberiumDireGuardian" );
                    ArcenPoint spawnLocation = warpGate.Planet.GetSafePlacementPoint_AroundEntity( Context, entityData, warpGate, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 250 ) );
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFactionCPA, entityData, pFaction.Faction.CurrentGeneralMarkLevel,
                        pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "SpawnAntiArmadaCPA" );
                }
                
                this.BaseInfo.StrengthForNextEnemyAttack -= strengthSpent;
                if ( this.BaseInfo.StrengthForNextEnemyAttack <= 0 )
                {
                    this.BaseInfo.StrengthForNextEnemyAttack = 0;
                    break;
                }
            }
        }
    }
}
