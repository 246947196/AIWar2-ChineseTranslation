using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    /// <summary>
    //Handles the logic for the Reapers. Reapers are the enemies of the Necromancer. They have defensive structures that act like Sapper Watchtowers,
    //and also just sometimes spawn units to go after the players.
    /// </summary>
    public sealed class ReapersFactionDeepInfo : ExternalFactionDeepInfoRoot, IExternalDeepInfo_Singleton
    {
        //Set immediately before the target sorts so the comparisons can be non-capturing static
        //delegates.  [ThreadStatic] because faction planning runs on background threads.
        [ThreadStatic] private static Faction cb_reapersFaction;
        [ThreadStatic] private static Planet cb_reapersStartPlanet;
        [ThreadStatic] private static Planet cb_reapersFtCurrentPlanet;
        [ThreadStatic] private static FInt cb_reapersFtFalloff;
        public ReapersFactionBaseInfo BaseInfo;
        public Faction DysonFaction;
        public static ReapersFactionDeepInfo Instance = null;
        public readonly List<SafeSquadWrapper> MobileRavagersLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "Reapers-MobileRavagersLRP" );
        public readonly List<SafeSquadWrapper> LarvaeLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "Reapers-LarvaeLRP" );
        public readonly List<SafeSquadWrapper> ImmobileRavagersLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "Reapers-ImmobileRavagersLRP" );
        private static readonly Dictionary<GameEntityTypeData, int> AttackComposition = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed(500, "DysonFactionDeepInfo-AttackComposition");
        public readonly ProtectedValDictionary<Planet, FireteamRegiment> TeamsAimedAtPlanet = ProtectedValDictionary<Planet, FireteamRegiment>.Create_WillNeverBeGCed( 100, "DarkSpireFactionDeepInfo-TeamsAimedAtPlanet" );
        public static readonly List<SafeSquadWrapper> ExoTargets = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "DysonFactionDeepInfo-ExoTargets" );
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<ReapersFactionBaseInfo>();
            Instance = this;
        }

        protected override void Cleanup()
        {
            Instance = null;
            BaseInfo = null;
            DysonFaction = null;

            //probably does not matter
            InCombatShipsNeedingOrdersLRP.Clear();
            MobileRavagersLRP.Clear();
            ImmobileRavagersLRP.Clear();
            AttackComposition.Clear();
            TeamsAimedAtPlanet.Clear();
            LarvaeLRP.Clear();
            playAudioEffectForCommand = false;
            ExoTargets.Clear();
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 8; //they aren't in a rush
        private bool playAudioEffectForCommand = false;

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                    return; //only on host (don't bother doing anything as a client)
                //initialize the dysonFaction and dsBaseInfo fields immediately

                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DS_Reapers_Lore", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                if ( DysonFaction == null )
                {
                    DysonFaction = FactionUtilityMethods.Instance.GetDysonSidekickFaction(); 
                }
                debugCode = 1000;
                if ( BaseInfo.MobileRavagers.Count > 0 )
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DS_Reapers_Ravager", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                HandleMobileRavagers(Context);
                HandleImmobileRavagers(Context);
                SpawnChrysalisesAtInterval(Context);
                HandleChrysalises(Context);
                HandlePlanetoids(Context);
                SpawnAIPlanetoidDrills(Context);
                HandleGatewaysSim(Context);
                HandleLarvaeSim(Context);
                HandleLunarInvasionSim(Context);
                debugCode = 1000;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in Reapers Stage 3 debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        private void SpawnAIPlanetoidDrills( ArcenHostOnlySimContext Context )
        {
            if (BaseInfo.TimeForNextAIPlanetoidDrill <= 0 )
            {
                BaseInfo.TimeForNextAIPlanetoidDrill = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.AIPlanetoidDrillSpawnInterval + Context.RandomToUse.Next(0, 300);
                return;
            }
            if ( BaseInfo.TimeForNextAIPlanetoidDrill > World_AIW2.Instance.GameSecond )
                return;

            GameEntity_Squad planetoid = FactionUtilityMethods.Instance.GetRandomPlanetoidOrNull(Context);
            if (planetoid == null)
            {
                BaseInfo.TimeForNextAIPlanetoidDrill = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.AIPlanetoidDrillSpawnInterval + Context.RandomToUse.Next(0, 300);
                return;
            }
            if ( FactionUtilityMethods.Instance.GetAICuendillarDrillOnPlanetOrNull( planetoid.Planet) != null )
            {
                //we already have a drill here, carry on. We don't really want multiple drills at once very often
                BaseInfo.TimeForNextAIPlanetoidDrill = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.AIPlanetoidDrillSpawnInterval + Context.RandomToUse.Next(0, 300);
                return;
            }
            FInt AIP = FactionUtilityMethods.Instance.GetCurrentAIP();
            Planet planetToSpawn = planetoid.Planet;
            Faction controllingFaction = planetToSpawn.GetControllingOrInfluencingFaction();
            if ( controllingFaction.Type == FactionType.AI &&
                 !controllingFaction.FactionIsDefeated)
            {
                //Also make an AI cuendillar drill
                spawningBuffer.Add("AI 正在 ").Add(planetToSpawn.Name, planetToSpawn.GetControllingFaction().FactionCenterColor.ColorHexBrighter).Add(" 上钻探库恩达小行星。");
                GameEntityTypeData drillTypeData = GameEntityTypeDataTable.Instance.GetRowByName( "AICuendillarDrill" );
                ArcenPoint spawnLocation = planetoid.Planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, drillTypeData, planetoid.WorldLocation, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 150 ) );
                PlanetFaction pFaction = planetToSpawn.GetPlanetFactionForFaction( controllingFaction );
                GameEntity_Squad drill = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, drillTypeData, 1,
                                                                                              pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "Reaper-SpawnAIDrill" );
                DysonSidekickPerUnitBaseInfo data = drill.CreateExternalBaseInfo<DysonSidekickPerUnitBaseInfo>( "DysonSidekickPerUnitBaseInfo" );

                int nTurrets = 6 + AIP.IntValue / 50;
                for (int i = 0; i < nTurrets; i++ )
                {
                    GameEntityTypeData turretTypeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "WormholeInvasionDefense" );
                    spawnLocation = planetoid.Planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, drillTypeData, drill.WorldLocation, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 150 ) );
                    GameEntity_Squad turret = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, turretTypeData, controllingFaction.CurrentGeneralMarkLevel,
                                                                                              pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "Reaper-SpawnAIDrill" );
                    
                }
            }

        }
        private void HandlePlanetoids( ArcenHostOnlySimContext Context )
        {
            if (!FactionUtilityMethods.Instance.AnyDysonSidekickFactions())
                return;

            if (BaseInfo.TimeForNextPlanetoid <= 0 )
            {
                //BaseInfo.TimeForNextPlanetoid = World_AIW2.Instance.GameSecond + BaseInfo.Income.PlanetoidSpawnInterval + Context.RandomToUse.Next(0, 300);
                BaseInfo.TimeForNextPlanetoid = World_AIW2.Instance.GameSecond + 60;
                return;
            }
            if ( BaseInfo.TimeForNextPlanetoid > World_AIW2.Instance.GameSecond )
                return;
            Planet planetToSpawn = GetPlanetForPlanetoid(Context, -1, -1, false);
            if ( planetToSpawn == null )
            {
                ArcenDebugging.LogSingleLine("Couldn't find a planetoid planet; retry soon", Verbosity.DoNotShow );
                BaseInfo.TimeForNextPlanetoid = World_AIW2.Instance.GameSecond + 30;
                return;
            }

            GameEntityTypeData planetoidTypeData = GameEntityTypeDataTable.Instance.GetRowByName( "CuendillarPlanetoid" );
            ArcenPoint spawnLocation = planetToSpawn.GetSafePlacementPointAroundPlanetCenter( Context, planetoidTypeData, FInt.FromParts( 0, 200 ), FInt.FromParts( 0, 500 ) );
            PlanetFaction pFaction = planetToSpawn.GetPlanetFactionForFaction( World_AIW2.Instance.GetNeutralFaction() );
            GameEntity_Squad planetoid = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, planetoidTypeData, 1,
                                                                                     pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "Reaper-SpawnPlanetoides" );
            spawningBuffer.Clear();
            spawningBuffer.Add("一个新的库恩达小行星正在 ").Add(planetToSpawn.Name, planetToSpawn.GetControllingFaction().FactionCenterColor.ColorHexBrighter).Add(" 上出现。");

            World_AIW2.Instance.QueueChatMessageOrCommand( spawningBuffer.GetStringAndResetForNextUpdate(),
                        ChatType.LogToCentralChat, "", null );
            Faction dysonFaction = FactionUtilityMethods.Instance.GetDysonSidekickFaction();
            DysonSidekickFactionBaseInfo dsBaseInfo = dysonFaction.GetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();

            BaseInfo.TimeForNextPlanetoid = World_AIW2.Instance.GameSecond + dsBaseInfo.Income.PlanetoidSpawnInterval + Context.RandomToUse.Next(0, 300);
        }
        private void HandleLunarInvasionSim(ArcenHostOnlySimContext Context)
        {
            //Lunar invasions are A. the moon + gateway, also a couple Ravagers, and also a mean AI Exostrike 30 seconds before the Lunar Invasion starts
            //to push the player back on defense, so the Reapers have a good chance of getting started
            //aimed at the player at the beginning to pull player attention away from the Reapers
            int debugCode = 0;
            if (!FactionUtilityMethods.Instance.AnyDysonSidekickFactions())
                return;//only if dyson sidekick exists
            try{
                bool debug = false;
                bool debugEarlyInvasion = false;
                if (BaseInfo.TimeForNextLunarInvasion == -1)
                {
                    BaseInfo.TimeForNextLunarInvasion = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.LunarInvasionInterval + Context.RandomToUse.Next(-300,300);
                    if ( debugEarlyInvasion )
                    {
                        BaseInfo.TimeForNextLunarInvasion = World_AIW2.Instance.GameSecond + 300;
                    }
                    return;
                }
                debugCode = 100;
                DysonSidekickFactionBaseInfo dsBaseInfo = DysonFaction.GetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
                if ( dsBaseInfo == null )
                {
                    throw new Exception("Could not find dyson faction base info for dysonFaction");
                }
                debugCode = 300;
                if ( BaseInfo.TimeForNextLunarInvasion == World_AIW2.Instance.GameSecond + 30 )
                {
                    //trigger the exo
                    debugCode = 300;
                    ExoTargets.Clear();
                    ExoGalacticAttackManager.GetAllHumanHomeCommandStations(ExoTargets);
                    int exoStrength = (FactionUtilityMethods.Instance.GetCurrentAIP() * this.BaseInfo.Difficulty.LunarInvasionExoStrengthPerAIP).IntValue;
                    if ( debug )
                    ArcenDebugging.LogSingleLine("exo strength " + exoStrength, Verbosity.DoNotShow );
                    ExoOptions options = ExoOptions.CreateWithDefaults( ExoTargets, exoStrength, null, AttachedFaction );
                    options.exoText = "AI 已探测到即将到来的月球入侵。";
                    ExoGalacticAttackManager.SendExoGalacticAttack( options, Context );

                }
                debugCode = 400;
                if ( BaseInfo.TimeForNextLunarInvasion <= World_AIW2.Instance.GameSecond)
                {
                    debugCode = 500;
                    AttackComposition.Clear();
                    BaseInfo.TimeForNextLunarInvasion = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.LunarInvasionInterval + Context.RandomToUse.Next(-300,300);
                    bool mustBeAdjacentToRavagedPlanet = true;
                    Planet planetToSpawn = GetPlanetForChrysalis(Context, -1, -1, false, !mustBeAdjacentToRavagedPlanet);
                    int aipStrength = (FactionUtilityMethods.Instance.GetCurrentAIP() * this.BaseInfo.Difficulty.LunarInvasionStrengthPerAIP).IntValue;
                    int sidekickStrength = this.BaseInfo.Difficulty.LunarInvasionStrengthPerStronghold * dsBaseInfo.Strongholds.Count;
                    int strength = aipStrength + sidekickStrength;
                    debugCode = 600;
                    if ( debug )
                    {
                        ArcenDebugging.LogSingleLine("invasion strength: " + strength + ", " + aipStrength + " + " + sidekickStrength, Verbosity.DoNotShow );
                    }
                    debugCode = 700;
                    SpawnMoonAtPlanet( planetToSpawn, ArcenPoint.ZeroZeroPoint, Context );
                    SpawnChrysalisAtPlanet(planetToSpawn, ArcenPoint.ZeroZeroPoint, Context); //Enable this when we're ready
                    TriggerRavagerOrUndeadAttack( strength, true, planetToSpawn, this.BaseInfo.Difficulty, Context, AttackComposition); //we also get a ravager from the chrysalis, so we'll get 2
                    FInt minRadius = FInt.FromParts( 0, 050 );
                    FInt maxRadius = FInt.FromParts( 0, 120 );
                    debugCode = 800;
                    for (int i = 0; i < BaseInfo.Difficulty.LunarInvasionSovereignsToSpawn; i++)
                    {
                        debugCode = 900;
                        GameEntityTypeData sovereignData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "ReaperSovereign");
                        ArcenPoint spawnLocation = planetToSpawn.GetSafePlacementPoint_AroundDesiredPointVicinity(Context, sovereignData, Engine_AIW2.Instance.CombatCenter, minRadius, maxRadius);
                        GameEntity_Squad entity = this.AttachedFaction.SpawnNewUnit_ReturnNullIfMPClient(
                            Context, planetToSpawn, spawnLocation, sovereignData, (byte)1,
                            this.AttachedFaction.LooseFleet, 0,
                            EntityBehaviorType.Attacker_Full, -1, null, "ChrysalisHatch");
                    }
                }
            } catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in HandleLunarInvasionSim debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        private static ArcenDoubleCharacterBuffer spawningBuffer = new ArcenDoubleCharacterBuffer( "ReaperFactionDeepInfo-spawningBuffer" );
        private void SpawnChrysalisesAtInterval(ArcenHostOnlySimContext Context)
        {
            bool fastSpawn = AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame("ReaperRapidSpawn");
            List<SafeSquadWrapper> chrysalises = this.BaseInfo.Chrysalises.GetDisplayList();
            List<SafeSquadWrapper> gateways = this.BaseInfo.Gateways.GetDisplayList();
            if ( chrysalises.Count > 0 || gateways.Count > 0)
                fastSpawn = false;
            if (BaseInfo.TimeForNextChrysalis == -1)
            {
                BaseInfo.TimeForNextChrysalis = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.ChrysalisSpawnInterval + Context.RandomToUse.Next(0, 300);
                if ( fastSpawn )
                {
                    BaseInfo.TimeForNextChrysalis = World_AIW2.Instance.GameSecond + 30;
                }
                return;
            }

            if ( fastSpawn && chrysalises.Count == 0 &&
                (BaseInfo.TimeForNextChrysalis - World_AIW2.Instance.GameSecond > 30))
            {
                BaseInfo.TimeForNextChrysalis = World_AIW2.Instance.GameSecond + 10;
            }
            if (BaseInfo.TimeForNextChrysalis > World_AIW2.Instance.GameSecond)
                return;
            bool mustBeAdjacentToRavagedPlanet = true;
            Planet planetToSpawn = GetPlanetForChrysalis(Context, -1, -1, false, !mustBeAdjacentToRavagedPlanet);
            if (planetToSpawn == null)
            {
                ArcenDebugging.LogSingleLine("Couldn't find a chrysalis planet; retry soon", Verbosity.DoNotShow);
                BaseInfo.TimeForNextChrysalis = World_AIW2.Instance.GameSecond + 30;
                return;
            }
            SpawnChrysalisAtPlanet(planetToSpawn, ArcenPoint.ZeroZeroPoint, Context);
            if (BaseInfo.RavagedPlanets.Count > 0)
            {
                int totalPercent = BaseInfo.RavagedPlanets.Count * BaseInfo.Difficulty.BonusChrysalisChancePerRavagedPlanet;
                ArcenDebugging.LogSingleLine("total percentage for extra chrysalis: " + totalPercent , Verbosity.DoNotShow );
                while (totalPercent > 0)
                {
                    int percentToUse = Math.Min(totalPercent, 100);
                    ArcenDebugging.LogSingleLine("using " + percentToUse, Verbosity.DoNotShow );
                    if (percentToUse >= Context.RandomToUse.Next(0, 100))
                    {
                        planetToSpawn = GetPlanetForChrysalis(Context, -1, -1, false, mustBeAdjacentToRavagedPlanet);
                        if (planetToSpawn == null)
                            break;
                        SpawnChrysalisAtPlanet(planetToSpawn, ArcenPoint.ZeroZeroPoint, Context);
                    }
                    totalPercent -= 100;
                }
            }
        }
        private void SpawnChrysalisAtPlanet ( Planet planetToSpawn, ArcenPoint spawnLocation, ArcenHostOnlySimContext Context, bool noAIDrill = false )
        {
            GameEntityTypeData chrysalisTypeData = GameEntityTypeDataTable.Instance.GetRowByName( "ReaperChrysalis" );
            if ( spawnLocation == ArcenPoint.ZeroZeroPoint )
                spawnLocation = planetToSpawn.GetSafePlacementPointAroundPlanetCenter( Context, chrysalisTypeData, FInt.FromParts( 0, 200 ), FInt.FromParts( 0, 500 ) );
            PlanetFaction pFaction = planetToSpawn.GetPlanetFactionForFaction(this.AttachedFaction);
            GameEntity_Squad chrysalis = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, chrysalisTypeData, 1,
                                                                                     pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "Reaper-SpawnChrysalises" );
            spawningBuffer.Clear();
            spawningBuffer.Add("收割者", AttachedFaction.FactionCenterColor.ColorHexBrighter).Add("正在").Add( planetToSpawn.Name, planetToSpawn.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( "上生成虫蛹。" );
            World_AIW2.Instance.QueueChatMessageOrCommand( spawningBuffer.GetStringAndResetForNextUpdate(),
                        ChatType.LogToCentralChat, "", null );
            BaseInfo.TimeForNextChrysalis = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.ChrysalisSpawnInterval + Context.RandomToUse.Next(0, 300);
            World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DS_Reapers_Chrysalis", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            Faction controllingFaction = planetToSpawn.GetControllingOrInfluencingFaction();
            if ( controllingFaction.Type == FactionType.AI &&
                 !controllingFaction.FactionIsDefeated &&
                 !noAIDrill )
            {
                //Also make an AI cuendillar drill
                GameEntityTypeData drillTypeData = GameEntityTypeDataTable.Instance.GetRowByName( "AICuendillarDrill" );
                spawnLocation = chrysalis.Planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, drillTypeData, chrysalis.WorldLocation, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 150 ) );
                pFaction = planetToSpawn.GetPlanetFactionForFaction( controllingFaction );
                GameEntity_Squad drill = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, drillTypeData, 1,
                                                                                              pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "Reaper-SpawnAIDrill" );
                DysonSidekickPerUnitBaseInfo data = drill.CreateExternalBaseInfo<DysonSidekickPerUnitBaseInfo>( "DysonSidekickPerUnitBaseInfo" );
            }

        }
        private void SpawnMoonAtPlanet ( Planet planetToSpawn, ArcenPoint spawnLocation, ArcenHostOnlySimContext Context )
        {
            GameEntityTypeData moonTypeData = GameEntityTypeDataTable.Instance.GetRowByName( "ReaperMoon" );
            if ( spawnLocation == ArcenPoint.ZeroZeroPoint )
                spawnLocation = planetToSpawn.GetSafePlacementPointAroundPlanetCenter( Context, moonTypeData, FInt.FromParts( 0, 200 ), FInt.FromParts( 0, 500 ) );
            byte markLevel = (byte)(FactionUtilityMethods.Instance.GetCurrentAIP() / BaseInfo.Difficulty.AIPForReaperMarkLevel);
            if (markLevel < 1)
                markLevel = 1;
            if (markLevel > 7)
                markLevel = 7;
            PlanetFaction pFaction = planetToSpawn.GetPlanetFactionForFaction(this.AttachedFaction);
            GameEntity_Squad Moon = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, moonTypeData, markLevel,
                pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "Reaper-SpawnMoon" );
            Moon.HullPointsLost = 0;
            spawningBuffer.Clear();
            spawningBuffer.Add("收割者", AttachedFaction.FactionCenterColor.ColorHexBrighter).Add("正在").Add( planetToSpawn.Name, planetToSpawn.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( "上生成卫星。" );
            World_AIW2.Instance.QueueChatMessageOrCommand( spawningBuffer.GetStringAndResetForNextUpdate(),
                ChatType.LogToCentralChat, "", null );
            ArcenDebugging.LogSingleLine("Spawned " + Moon.ToStringWithPlanetAndOwner() + ". The moon's current health is " + Moon.GetCurrentHullPoints(), Verbosity.DoNotShow );
            Moon.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
            Moon.HasNotYetBeenFullyClaimed = false;
        }
        
        private void HandleGatewaysSim(ArcenHostOnlySimContext Context)
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has(ArcenTracingFlags.DysonSidekick);
                ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate("Reapers-HandleGatewaysSim-trace", 10f) : null;

                List<SafeSquadWrapper> gateways = this.BaseInfo.Gateways.GetDisplayList();
                for (int i = 0; i < gateways.Count; i++)
                {
                    GameEntity_Squad squad = gateways[i].GetSquad();
                    if (squad == null)
                        continue;
                    ReapersPerUnitBaseInfo data = squad.TryGetExternalBaseInfoAs<ReapersPerUnitBaseInfo>();
                    if (data == null)
                        continue;
                    this.BaseInfo.LastTimeHadGateway = World_AIW2.Instance.GameSecond;
                    if (tracing)
                        tracingBuffer.Add("HandleGateways: Checking on " + squad.ToStringWithPlanet() + ".\n");
                    {
                        //Handle Gatewawy Markup and sovereign spawn
                        if (data.GatewayNextMarkupTime == -1)
                        {
                            data.GatewayNextMarkupTime = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.GatewayMarkupInterval;
                        }
                        if (data.GatewayNextMarkupTime <= World_AIW2.Instance.GameSecond &&
                             (int)squad.CurrentMarkLevel < 7)
                        {
                            squad.SetCurrentMarkLevel((byte)(squad.CurrentMarkLevel + 1));
                            data.GatewayNextMarkupTime = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.GatewayMarkupInterval;
                            FInt minRadius = FInt.FromParts(0, 050);
                            FInt maxRadius = FInt.FromParts(0, 120);

                            GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "ReaperSovereign");
                            ArcenPoint spawnLocation = squad.Planet.GetSafePlacementPoint_AroundDesiredPointVicinity(Context, typeData, squad.WorldLocation, minRadius, maxRadius);
                            GameEntity_Squad entity = this.AttachedFaction.SpawnNewUnit_ReturnNullIfMPClient(
                              Context, squad.Planet, spawnLocation, typeData, squad.CurrentMarkLevel,
                              this.AttachedFaction.LooseFleet, 0,
                                EntityBehaviorType.Attacker_Full, -1, null, "ChrysalisHatch");
                            entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
                        }
                    }
                    {
                        //Handle Gateway Reinforcements

                        if ( data.GatewayNextReinforcementTime == -1 )
                        {
                            data.GatewayNextReinforcementTime = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.GatewayTroopSpawnInterval;
                        }
                        if (data.GatewayNextReinforcementTime < World_AIW2.Instance.GameSecond)
                        {
                            AttackComposition.Clear();
                            data.GatewayNextReinforcementTime = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.GatewayTroopSpawnInterval;
                            string tag = "UndeadRavagerTroopsTierOne";
                            if (FactionUtilityMethods.Instance.GetCurrentAIP().IntValue < BaseInfo.Difficulty.AIPForReaperTierOne)
                                tag = "GatewaySpawnTierOne";
                            else if (FactionUtilityMethods.Instance.GetCurrentAIP().IntValue < BaseInfo.Difficulty.AIPForReaperTierTwo)
                                tag = "GatewaySpawnTierTwo";
                            else if (FactionUtilityMethods.Instance.GetCurrentAIP().IntValue < BaseInfo.Difficulty.AIPForReaperTierThree)
                                tag = "GatewaySpawnTierThree";
                            int strength = (BaseInfo.Difficulty.GatewayTroopSpawnStrengthPerAIP * FactionUtilityMethods.Instance.GetCurrentAIP()).IntValue;
                            int origStr = strength;
                            if (FactionUtilityMethods.Instance.AnyDysonSidekickFactions())
                            {
                                DysonSidekickFactionBaseInfo dsBaseInfo = DysonFaction.GetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();

                                if (dsBaseInfo.SphereOnline)
                                {
                                    strength /= 20;
                                    tag = "GatewaySpawnTierOne";
                                }
                            }
                            int attempts = 100;
                            
                            while ( strength > 0 && attempts > 0)
                            {
                                GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, tag );
                                if ( typeData == null )
                                    throw new Exception("Could not find entity with tag " + tag + " in the SpawnRavagerForces code path");
                                if ( strength - typeData.CostForAIToPurchase < 0 )
                                {
                                    attempts--;
                                    continue;
                                }
                                strength -= typeData.CostForAIToPurchase;
                                AttackComposition[typeData]++;
                            }
                            foreach ( KeyValuePair<GameEntityTypeData, int> pair in AttackComposition )
                            {
                                debugCode = 800;
                                int totalSquadsToSpawn = pair.Value;
                                GameEntityTypeData entityType = pair.Key;
                                int numStacksPerSquad = 0;
                                int separateSquadsToSpawn = totalSquadsToSpawn;
                                int remainder = 0;
                                int StackingCutoff = AIWar2GalaxySettingQuickAccess.StackingCutoffNPCs;
                                if (tracing)
                                    tracingBuffer.Add("\tSpawning " + totalSquadsToSpawn + " of " + pair.Key.GetDisplayName() + "\n");

                                if (StackingCutoff <= 0)
                                    throw new Exception("Undefined StackingCutoffNPCs; this means that waves won't spawn");
                                if (totalSquadsToSpawn > StackingCutoff)
                                {
                                    separateSquadsToSpawn = StackingCutoff;
                                    numStacksPerSquad = totalSquadsToSpawn / separateSquadsToSpawn;
                                    remainder = totalSquadsToSpawn % separateSquadsToSpawn;
                                }
                                debugCode = 900;
                                for (int j = 0; j < separateSquadsToSpawn; j++)
                                {
                                    debugCode = 1000;
                                    FInt minRadius = FInt.FromParts(0, 050);
                                    FInt maxRadius = FInt.FromParts(0, 100);

                                    ArcenPoint spawnLocation = squad.WorldLocation;
                                    spawnLocation = squad.Planet.GetSafePlacementPoint_AroundDesiredPointVicinity(Context, entityType, spawnLocation, minRadius, maxRadius);
                                    byte markLevel = (byte)(FactionUtilityMethods.Instance.GetCurrentAIP() / BaseInfo.Difficulty.AIPForReaperMarkLevel);
                                    if (markLevel < 1)
                                        markLevel = 1;
                                    if (markLevel > 7)
                                        markLevel = 7;
                                    GameEntity_Squad entity = this.AttachedFaction.SpawnNewUnit_ReturnNullIfMPClient(
                                        Context, squad.Planet, spawnLocation, entityType, markLevel,
                                        this.AttachedFaction.LooseFleet, 0,
                                        EntityBehaviorType.Attacker_Full, -1, null, "ReaperAttackDeployment");

                                    if (entity == null)
                                        continue;

                                    if (numStacksPerSquad > 0)
                                    {
                                        if (totalSquadsToSpawn < numStacksPerSquad)
                                        {
                                            entity.AddOrSetExtraStackedSquadsInThis((Int16)totalSquadsToSpawn, true);
                                        }
                                        else
                                            entity.AddOrSetExtraStackedSquadsInThis((Int16)(numStacksPerSquad - 1), true); //don't count the original unit
                                        if (remainder > 0)
                                        {
                                            entity.AddOrSetExtraStackedSquadsInThis(1, false);
                                            remainder--;
                                        }
                                        totalSquadsToSpawn -= entity.ExtraStackedSquadsInThis + 1;
                                    }
                                }
                            };
                        }

                    }
                    debugCode = 1100;
                    {
                        //Handle Larva spawning
                        if (data.GatewayNextLarvaTime == -1)
                        {
                            data.GatewayNextLarvaTime = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.GatewayLarvaInterval;
                        }
                        if (data.GatewayNextLarvaTime <= World_AIW2.Instance.GameSecond )
                        {
                            debugCode = 1200;
                            data.GatewayNextLarvaTime = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.GatewayLarvaInterval;
                            FInt minRadius = FInt.FromParts(0, 050);
                            FInt maxRadius = FInt.FromParts(0, 120);

                            GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "ReaperLarva");
                            ArcenPoint spawnLocation = squad.Planet.GetSafePlacementPoint_AroundDesiredPointVicinity(Context, typeData, squad.WorldLocation, minRadius, maxRadius);
                            GameEntity_Squad entity = this.AttachedFaction.SpawnNewUnit_ReturnNullIfMPClient(
                              Context, squad.Planet, spawnLocation, typeData, squad.CurrentMarkLevel,
                              this.AttachedFaction.LooseFleet, 0,
                              EntityBehaviorType.Attacker_Full, -1, null, "LarvaSpawn");
                        }
                    }
                }
                FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
            } catch ( Exception e)
            {
                ArcenDebugging.LogSingleLine("Hit crash in HandleGatewaysSim debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }

        }
        private void HandleChrysalises( ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DysonSidekick );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Reapers-HatchChrysalis-trace", 10f ) : null;
            List<SafeSquadWrapper> moons = this.BaseInfo.Moons.GetDisplayList();
            List<SafeSquadWrapper> chrysalises = this.BaseInfo.Chrysalises.GetDisplayList();
            for (int i = 0; i < chrysalises.Count; i++)
            {
                GameEntity_Squad squad = chrysalises[i].GetSquad();
                if (squad == null)
                    continue;
                ReapersPerUnitBaseInfo data = squad.TryGetExternalBaseInfoAs<ReapersPerUnitBaseInfo>();
                if ( data == null )
                    continue;

                if (tracing)
                    tracingBuffer.Add("HandleChrysalises: Checking on " + squad.ToStringWithPlanet() + ".\n");
                if ( data.ChrysalisHatchTime < 0 &&
                     squad.GetSecondsSinceCreation() < 10 )
                {
                    //initialize all the data!
                    data.ChrysalisHatchTime = BaseInfo.Difficulty.ChrysalisHatchTime + World_AIW2.Instance.GameSecond;
                    if (AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "ReaperRapidSpawn"))
                        data.ChrysalisHatchTime = 20 + World_AIW2.Instance.GameSecond;

                    if ( BaseInfo.LastTimeHadGateway == -1 || //weight the dice toward getting a chrysalis to hatch if it's been a while
                         World_AIW2.Instance.GameSecond - BaseInfo.LastTimeHadGateway > 3600 )
                        data.ChrysalisHatchTime -= (60 + Context.RandomToUse.Next(0, 60));
                    for ( int j = 0; j < moons.Count; j++ )
                    {
                        //Moons cause chrysalises to hatch much faster. This is theoretically for the initial chrysalis + moon attack,
                        //but if it applies to other chrysalises, that seems fun too
                        GameEntity_Squad moon = moons[i].GetSquad();
                        if (moon == null)
                             continue;
                        if ( moon.Planet == squad.Planet )
                        {
                            data.ChrysalisHatchTime = 2 + World_AIW2.Instance.GameSecond;
                        }
                    }
                    data.CuendillarRemaining = BaseInfo.Difficulty.ChrysalisCuendillar + Context.RandomToUse.Next(0, BaseInfo.Difficulty.ChrysalisCuendillarVariance);
                    continue;
                }
                if ( data.CuendillarRemaining == 0 )
                {
                    //out of cuendillar, just despawn.
                    World_AIW2.Instance.QueueChatMessageOrCommand( squad.Planet.Name + " 上的茧因缺乏库恩达而失稳，已崩溃。",
                        ChatType.LogToCentralChat, "", null );

                    squad.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut);
                }
                if ( data.ChrysalisHatchTime <= World_AIW2.Instance.GameSecond )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( squad.Planet.Name + " 上的茧已孵化！",
                        ChatType.LogToCentralChat, "", null );

                    HatchChrysalis ( squad, data, BaseInfo.Difficulty, Context );
                    squad.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut);
                }
            }
            FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
        }
        private void HandleLarvaeSim( ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DysonSidekick );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Reapers-HandleLarvaSim-trace", 10f ) : null;
            List<SafeSquadWrapper> larvae = this.BaseInfo.Larvae.GetDisplayList();
            if ( World_AIW2.Instance.GameSecond - BaseInfo.LastTimeSpawnedLarva > BaseInfo.Difficulty.LarvaSpawnInterval &&
                 BaseInfo.RavagedPlanets.Count > 0 && larvae.Count == 0 )
            {
                //we are allowed to spawn extra larvae on ravaged planets periodically
                Planet planet = this.GetWeakestRavagedPlanet(Context);
                if ( planet != null )
                {
                    BaseInfo.LastTimeSpawnedLarva = World_AIW2.Instance.GameSecond;
                    FInt minRadius = FInt.FromParts(0, 500);
                    FInt maxRadius = FInt.FromParts(0, 800);

                    GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "ReaperLarva");
                    ArcenPoint spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity(Context, typeData, Engine_AIW2.Instance.CombatCenter, minRadius, maxRadius);
                    GameEntity_Squad entity = this.AttachedFaction.SpawnNewUnit_ReturnNullIfMPClient(
                      Context, planet, spawnLocation, typeData, (byte)1,
                      this.AttachedFaction.LooseFleet, 0,
                      EntityBehaviorType.Attacker_Full, -1, null, "LarvaSpawn");
                }
            }

            for (int i = 0; i < larvae.Count; i++)
            {
                GameEntity_Squad squad = larvae[i].GetSquad();
                if (squad == null)
                    continue;
                ReapersPerUnitBaseInfo data = squad.TryGetExternalBaseInfoAs<ReapersPerUnitBaseInfo>();
                if ( data == null )
                    continue;
                Planet planet = squad.Planet;
                if ( FactionUtilityMethods.Instance.GetReaperChrysalisOnPlanetOrNull( planet ) != null ||
                     FactionUtilityMethods.Instance.GetReaperGatewayOnPlanetOrNull( planet ) != null ||
                     FactionUtilityMethods.Instance.GetCuendillarDrillOnPlanetOrNull( planet ) != null )
                {
                    continue; //we are on a planet with a drill, chrysalis or gateway already, so don't spawn anything
                }

                if (tracing)
                    tracingBuffer.Add("HandleLarvaeSim: Checking on " + squad.ToStringWithPlanet() + ".\n");
                int distance = Mat.DistanceBetweenPointsImprecise(squad.WorldLocation, Engine_AIW2.Instance.CombatCenter);
                if ( distance < 200 )
                {
                    squad.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut);
                    bool noAIDrill = true;
                    SpawnChrysalisAtPlanet(squad.Planet, Engine_AIW2.Instance.CombatCenter, Context, noAIDrill);
                }   
            }
            FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
        }
        private void HatchChrysalis( GameEntity_Squad chrysalis, ReapersPerUnitBaseInfo data, DysonSidekickDifficulty dsDifficulty, ArcenHostOnlySimContext Context )
        {
            ArcenDebugging.LogSingleLine(chrysalis.ToStringWithPlanet() + " is hatching", Verbosity.DoNotShow );
            int strength = dsDifficulty.ChrysalisHatchBaseStrength;
            int cuendillarIncrease = data.CuendillarRemaining * dsDifficulty.ChrysalisHatchStrengthIncreasePerCuendillarRemaining;
            FInt AIP = FactionUtilityMethods.Instance.GetCurrentAIP();
            byte maxMarkLevel = (byte)(AIP / dsDifficulty.ChrysalisHatchAIPPerMarkLevel);
            byte minMarkLevel = (byte)1;
            if ( maxMarkLevel < 1 )
            {
                maxMarkLevel = 1;
            }
            if ( maxMarkLevel > 5 )
                minMarkLevel = (byte)(maxMarkLevel - (byte)3);
            string tag = "GatewaySpawnTierOne";
            if ( FactionUtilityMethods.Instance.GetCurrentAIP().IntValue < dsDifficulty.AIPForReaperTierTwo )
                tag = "GatewaySpawnTierTwo";
            else if ( FactionUtilityMethods.Instance.GetCurrentAIP().IntValue < dsDifficulty.AIPForReaperTierThree )
                tag = "GatewaySpawnTierThree";

            int origStr = strength;
            AttackComposition.Clear();

            GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ReaperGateway" );
            if ( typeData == null )
                throw new Exception("Could not find entity with tag " + tag + " in the SpawnRavagerForces code path");
            GameEntity_Squad gateway = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(chrysalis.PlanetFaction, typeData, 1,
                                                                                         this.AttachedFaction.LooseFleet, 0, chrysalis.WorldLocation, Context, "DeathEffect-GatewaySpawn");
            if (gateway != null)
            {
                ReapersPerUnitBaseInfo rData = gateway.CreateExternalBaseInfo<ReapersPerUnitBaseInfo>( "ReapersPerUnitBaseInfo" );
                FInt minRadius = FInt.FromParts( 0, 050 );
                FInt maxRadius = FInt.FromParts( 0, 120 );
                for (int i = 0; i < BaseInfo.Difficulty.ChrysalisHatchSovereignsToSpawn; i++)
                {
                    typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "ReaperSovereign");
                    ArcenPoint spawnLocation = gateway.Planet.GetSafePlacementPoint_AroundDesiredPointVicinity(Context, typeData, chrysalis.WorldLocation, minRadius, maxRadius);
                    GameEntity_Squad entity = this.AttachedFaction.SpawnNewUnit_ReturnNullIfMPClient(
                      Context, chrysalis.Planet, spawnLocation, typeData, (byte)1,
                        this.AttachedFaction.LooseFleet, 0,
                        EntityBehaviorType.Attacker_Full, -1, null, "ChrysalisHatch");
                }
            }
            while ( strength > 0 )
            {
                typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, tag );
                if ( typeData == null )
                    throw new Exception("Could not find entity with tag " + tag + " in the SpawnRavagerForces code path");
                AttackComposition[typeData]++;
                strength -= typeData.CostForAIToPurchase;
            }

            bool debug = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has(ArcenTracingFlags.DysonSidekick);
            if (debug)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Spawned troops for " + chrysalis.ToStringWithPlanet() + " strength " + origStr +" (" + origStr + " * " + FactionUtilityMethods.Instance.GetCurrentAIP() +")",  Verbosity.DoNotShow);
            }
            foreach ( KeyValuePair<GameEntityTypeData, int> pair in AttackComposition )
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
                    ArcenPoint spawnLocation = chrysalis.Planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, entityType, chrysalis.WorldLocation, minRadius, maxRadius );
                    byte markLevel = (byte)Context.RandomToUse.Next(minMarkLevel, maxMarkLevel);
                    GameEntity_Squad entity = this.AttachedFaction.SpawnNewUnit_ReturnNullIfMPClient(
                        Context, chrysalis.Planet, spawnLocation, entityType, markLevel,
                        this.AttachedFaction.LooseFleet, 0,
                        EntityBehaviorType.Attacker_Full, -1, null, "ChrysalisHatch" );

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
            ArcenDebugging.LogSingleLine("also trigger some ravagers along with the chrysalis hatch", Verbosity.DoNotShow );
            ReapersFactionDeepInfo.TriggerRavagerOrUndeadAttack( BaseInfo.Difficulty.ChrysalisHatchRavagerStrength, true, chrysalis.Planet, BaseInfo.Difficulty, Context, AttackComposition);
        }
        public static void TriggerRavagerOrUndeadAttack( int undeadStrength, bool useRavager, Planet planet, DysonSidekickDifficulty Difficulty, ArcenHostOnlySimContext Context, Dictionary<GameEntityTypeData, int> AttackCompositionToFill )
        {
         int debugCode = 0;
            try{
                Faction reaperFaction = FactionUtilityMethods.Instance.GetReapersFaction();
                if ( reaperFaction == null )
                    throw new Exception("No reapers faction found");
                if ( planet == null )
                    throw new Exception("Tried to trigger an attack with a null planet");
                GameEntityTypeData typeData = null;
                AttackCompositionToFill.Clear();
                bool tracing = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has(ArcenTracingFlags.DysonSidekick);
                ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate("DysonmD-TriggerAttack-SpecifiedPlanet-trace", 10f) : null;
                debugCode = 200;
                if ( tracing )
                    tracingBuffer.Add("\tTriggerAttack: spawning " + undeadStrength + " strength of units on " + planet.Name +". Min value: " + Difficulty.MinAttackStrength ).Add("\n");
                string tag = "UndeadAssaultTierOne";
                if ( FactionUtilityMethods.Instance.GetCurrentAIP().IntValue < Difficulty.AIPForReaperTierOne )
                    tag = "UndeadAssaultTierOne";
                else if ( FactionUtilityMethods.Instance.GetCurrentAIP().IntValue < Difficulty.AIPForReaperTierTwo )
                    tag = "UndeadAssaultTierTwo";
                else if ( FactionUtilityMethods.Instance.GetCurrentAIP().IntValue < Difficulty.AIPForReaperTierThree )
                    tag = "UndeadAssaultTierThree";
                debugCode = 300;

                if (FactionUtilityMethods.Instance.AnyDysonSidekickFactions())
                {
                    Faction dysonFaction = FactionUtilityMethods.Instance.GetDysonSidekickFaction(); 
                    DysonSidekickFactionBaseInfo dsBaseInfo = dysonFaction.GetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
                    if (dsBaseInfo.SphereOnline)
                    {
                        undeadStrength /= 20;
                        tag = "UndeadAssaultTierOne"; //always lowest tier
                    }
                }
                while (undeadStrength > 0)
                {
                    debugCode = 400;
                    typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, tag);
                    if (typeData == null)
                        throw new Exception("Could not find game entity with tag " + tag);
                    AttackCompositionToFill[typeData]++;
                    undeadStrength -= typeData.CostForAIToPurchase;
                }

                debugCode = 500;
                
                if ( useRavager )
                {
                    debugCode = 600;
                    typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MobileRavager" );
                    if ( typeData == null )
                        throw new Exception("Could not find MobileRavager in the XML");
                    AttackCompositionToFill[typeData] = 1;
                }
                debugCode = 700;
                ArcenPoint overrideSpawnSpot = ArcenPoint.ZeroZeroPoint;
                if ( Context.RandomToUse.Next(0, 100) < 50 )
                {
                    FInt minRadius = FInt.FromParts( 0, 900 );
                    FInt maxRadius = FInt.FromParts( 0, 990 );
                    if ( typeData == null )
                        typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MobileRavager" );
                    overrideSpawnSpot = planet.GetSafePlacementPoint_AroundDesiredPointVicinity(Context, typeData, Engine_AIW2.Instance.CombatCenter, minRadius, maxRadius);                        
                }

                foreach ( KeyValuePair<GameEntityTypeData, int> pair in AttackCompositionToFill )
                {
                    debugCode = 800;
                    int totalSquadsToSpawn = pair.Value;
                    GameEntityTypeData entityType = pair.Key;
                    int numStacksPerSquad = 0;
                    int separateSquadsToSpawn = totalSquadsToSpawn;
                    int remainder = 0;
                    int StackingCutoff = AIWar2GalaxySettingQuickAccess.StackingCutoffNPCs;
                    if ( tracing )
                        tracingBuffer.Add("\tSpawning " + totalSquadsToSpawn + " of " + pair.Key.GetDisplayName() + "\n" );

                    if ( StackingCutoff <= 0 )
                        throw new Exception( "Undefined StackingCutoffNPCs; this means that waves won't spawn" );
                    if ( totalSquadsToSpawn > StackingCutoff )
                    {
                        separateSquadsToSpawn = StackingCutoff;
                        numStacksPerSquad = totalSquadsToSpawn / separateSquadsToSpawn;
                        remainder = totalSquadsToSpawn % separateSquadsToSpawn;
                    }
                    debugCode = 900;
                    for ( int j = 0; j < separateSquadsToSpawn; j++ )
                    {
                        debugCode = 1000;
                        FInt minRadius = FInt.FromParts( 0, 900 );
                        FInt maxRadius = FInt.FromParts( 0, 990 );

                        ArcenPoint spawnLocation;
                        if ( overrideSpawnSpot != ArcenPoint.ZeroZeroPoint)
                            spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity(Context, entityType, overrideSpawnSpot, FInt.FromParts(0, 10), FInt.FromParts(0, 060));
                        else
                            spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity(Context, entityType, Engine_AIW2.Instance.CombatCenter, minRadius, maxRadius);
                        byte markLevel = (byte)(FactionUtilityMethods.Instance.GetCurrentAIP() / Difficulty.AIPForReaperMarkLevel);
                        if ( markLevel < 1 )
                            markLevel = 1;
                        if ( markLevel > 7 )
                            markLevel = 7;
                        GameEntity_Squad entity = reaperFaction.SpawnNewUnit_ReturnNullIfMPClient(
                          Context, planet, spawnLocation, entityType, markLevel,
                          reaperFaction.LooseFleet, 0,
                          EntityBehaviorType.Attacker_Full, -1, null, "ReaperAttackDeployment" );

                        if ( entity == null )
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
                FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
            }catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in TriggerAttack B debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            return;
        }
        private void HandleMobileRavagers( ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DysonSidekick );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Reapers-HandleMobileRavagers-trace", 10f ) : null;
            if ( World_AIW2.Instance.GameSecond % 10 != 0 && tracing)
                tracing = false; //this tracing can be kinda verbose
            List<SafeSquadWrapper> mobileRavagers = this.BaseInfo.MobileRavagers.GetDisplayList();
            List<SafeSquadWrapper> immobileRavagers = this.BaseInfo.ImmobileRavagers.GetDisplayList();
            for (int i = 0; i < mobileRavagers.Count; i++ )
            {
                GameEntity_Squad squad = mobileRavagers[i].GetSquad();
                if ( squad == null )
                    continue;
                if ( tracing )
                    tracingBuffer.Add("HandleMobileRavagers: Checking on " + squad.ToStringWithPlanet() + ". We have " + immobileRavagers.Count + " immobile ravagers\n");
                if (!IsPlanetRavagable(squad.Planet, immobileRavagers))
                {
                    if ( tracing )
                        tracingBuffer.Add("HandleMobileRavagers: we can't ravage " + squad.ToStringWithPlanet() + ". we should go somewhere else\n");
                    
                    continue;
                }
                if ( tracing )
                    tracingBuffer.Add("\t: We can ravage " + squad.Planet.Name + "!\n");

                var factionData = squad.Planet.GetStanceDataForFaction( AttachedFaction );
                if (factionData[FactionStance.Hostile].TotalStrength > 20 * 1000)
                {
                    if ( tracing )
                        tracingBuffer.Add("\t: too many enemies :-(\n");

                    continue; //no transforming with lots of enemies!
                }
                int distance = Mat.DistanceBetweenPointsImprecise(squad.WorldLocation, Engine_AIW2.Instance.CombatCenter);
                if ( distance < 100 )
                {
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "ImmobileRavager");
                    if (entityData == null)
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine("Could not find entityData for immobile ravager", Verbosity.DoNotShow);
                        continue;
                    }
                    if ( tracing )
                        tracingBuffer.Add("\t: transforming!\n");

                    squad.TransformInto(Context, entityData, 1, true);
                }
            }
            FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
        }
        private bool IsPlanetRavagable( Planet planet, List<SafeSquadWrapper> immobileRavagers )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DysonSidekick );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Reapers-IsPlanetRavagable-trace", 10f ) : null;
            bool debug = false;
            if (planet.IsRavaged)
            {
                if ( tracing && debug )
                    tracingBuffer.Add("\t\tCan't ravage " + planet.Name + ", already ravaged\n");
                FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
                return false;
            }
            List<SafeSquadWrapper> gateways = this.BaseInfo.Gateways.GetDisplayList();
            for (int i = 0; i < gateways.Count; i++)
            {
                GameEntity_Squad squad = gateways[i].GetSquad();
                if (squad == null)
                    continue;
                if (squad.Planet == planet)
                {
                    FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
                    return false; //don't drill where we currently have gateways
                }
            }
            for (int j = 0; j < immobileRavagers.Count; j++)
            {
                GameEntity_Squad immobile = immobileRavagers[j].GetSquad();
                if ( immobile == null )
                    continue;
                if (immobile.Planet == planet)
                {
                    if ( tracing && debug )
                        tracingBuffer.Add("\t\tCan't ravage " + planet.Name + ", it is already being ravaged\n");
                    FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
                    return false;
                }
            }
            FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
            return true;
        }
        public int TimeForRavagerToRavage = 90; //should be 900, but for debugging
        private void HandleImmobileRavagers( ArcenHostOnlySimContext Context )
        {
            int debugCode = 0;
            List<Planet> ravagedPlanets = this.BaseInfo.RavagedPlanets.GetDisplayList();
            List<SafeSquadWrapper> immobileRavagers = this.BaseInfo.ImmobileRavagers.GetDisplayList();
            try{
                bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has(ArcenTracingFlags.DysonSidekick);
                ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate("Reapers-HandleImmobileRavagers-trace", 10f) : null;

                for (int i = 0; i < immobileRavagers.Count; i++ )
                {
                    debugCode = 100;
                    GameEntity_Squad squad = immobileRavagers[i].GetSquad();
                    if ( squad == null )
                        continue;
                    ReapersPerUnitBaseInfo data = squad.TryGetExternalBaseInfoAs<ReapersPerUnitBaseInfo>();
                    if ( data == null )
                        continue;
                    debugCode = 200;
                    if (data.SecondsTillRavage == -1)
                    {
                        data.SecondsTillRavage = BaseInfo.Difficulty.ImmobileRavagerTimeToRavage;
                        data.SecondsTillTroopSpawn = BaseInfo.Difficulty.ImmobileRavagerTroopSpawnInterval;
                    }
                    data.SecondsTillRavage--;
                    data.SecondsTillTroopSpawn--;
                    debugCode = 300;
                    if ( data.SecondsTillTroopSpawn <= 0 )
                    {
                        debugCode = 400;
                        data.SecondsTillTroopSpawn = BaseInfo.Difficulty.ImmobileRavagerTroopSpawnInterval;
                        List<SafeSquadWrapper> gateways = this.BaseInfo.Gateways.GetDisplayList();
                        bool fireteamShips = false;
                        if ( gateways.Count > 0 )
                            fireteamShips = true;
                        DysonSidekickFactionDeepInfo.SpawnRavagerForces(squad, Context, fireteamShips, BaseInfo.Difficulty );
                    }
                    debugCode = 500;
                    if (data.SecondsTillRavage == 0)
                    {
                        debugCode = 600;
                        foreach ( GameEntity_Squad generator in squad.Planet.Squads( "MetalGenerator" ) )
                        {
                            generator.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut);
                        }
                        debugCode = 700;
                        squad.Planet.NonSim_ShouldSetRavagedIfHostAndNoMetalHarvesters = true;
                        squad.Planet.IsRavaged = true;
                        squad.Planet.Network_HostOnly_NeedToSyncWormholesToClients = false;
                        squad.Planet.Network_HostOnly_NeedToSyncPlanetPositionToClients = false;
                        foreach ( GameEntity_Squad entityToDestroy in squad.Planet.Squads() )
                        {
                           debugCode = 800;
                           //Destroy all non-flagship, non-mark7 structures
                           if (entityToDestroy.PlanetFaction == squad.PlanetFaction)
                               continue;
                           if (entityToDestroy.PlanetFaction.Faction.Type == FactionType.Player)
                           {
                               Fleet fleet = entityToDestroy.FleetMembership.Fleet;
                               GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                               if (centerpiece == entityToDestroy)
                                   continue;
                           }
                           if (entityToDestroy.CurrentMarkLevel >= 7)
                               continue;

                           entityToDestroy.DoOnDeathInCombatLogic_OnlyAferFullStackDeath(null, DamageSource.SomeSortOfEnemy, 0, Context);
                           entityToDestroy.Despawn(Context, true, InstancedRendererDeactivationReason.SelfDestructOnFiring);
                        }
                        debugCode = 1000;
                        //and now we transform back into a mobile ravager....
                        string tag = "MobileRavager";
                        GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, tag);
                        if ( entityData == null )
                            throw new Exception("Could not find mobile ravager with tag " + tag);
                        debugCode = 1200;
                        squad.TransformInto(Context, entityData, 1, true);
                        //and spawn a Larva
                        entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ReaperLarva" );
                        FInt minRadius = FInt.FromParts(0, 050);
                        FInt maxRadius = FInt.FromParts(0, 120);

                        ArcenPoint spawnLocation = squad.Planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, entityData, squad.WorldLocation, minRadius, maxRadius );
                        GameEntity_Squad entity = this.AttachedFaction.SpawnNewUnit_ReturnNullIfMPClient(
                          Context, squad.Planet, spawnLocation, entityData, (byte)5,
                          this.AttachedFaction.LooseFleet, 0,
                          EntityBehaviorType.Attacker_Full, -1, null, "DrillLarvacd" );

                    }
                }
                FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
            }catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit an exception in HandleImmobileRavagers debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        public override void DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly( ref int debugStage, GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull,
              Faction factionThatKilledEntity, Faction entityOwningFaction, int numExtraStacksKilled, ArcenHostOnlySimContext Context )
        {
            // This seems to be happening somewhere else, but I can't be bothered to figure out where
            // if ( entity.TypeData.GetHasTag("ReaperMoon"))
            // {
            //     GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName("ReaperShatteredMoon");
            //     if ( entityData == null )
            //         throw new Exception("Could not spawn shattered moon");
            //     Faction neutralFaction = World_AIW2.Instance.GetNaturalObjectFactionNeverNull();
            //     GameEntity_Squad shatteredEntity = neutralFaction.SpawnNewUnit_ReturnNullIfMPClient(
            //               Context, entity.Planet, entity.WorldLocation, entityData, 1,
            //               neutralFaction.LooseFleet, 0,
            //         EntityBehaviorType.Attacker_Full, -1, null, "ShatteredMoon" );
            //     ArcenDebugging.LogSingleLine("spawning a new " + shatteredEntity.ToStringWithPlanet() + " from " + entity.ToStringWithPlanet(), Verbosity.DoNotShow );
            // }
        }
        public override void UpdatePlanetInfluence_HostOnly( ArcenHostOnlySimContext Context )
        {
            List<Planet> planetsInfluenced = Planet.GetTemporaryPlanetList( "Reapers-UpdatePlanetInfluence_HostOnly-planetsInfluenced", 10f );
            if ( planetsInfluenced == null ) //blocked for teardown/shutdown; bail
                return;

            List<SafeSquadWrapper> gateways = this.BaseInfo.Gateways.GetDisplayList();
            for (int i = 0; i < gateways.Count; i++)
            {
                GameEntity_Squad squad = gateways[i].GetSquad();
                if (squad == null)
                    continue;
                ReapersPerUnitBaseInfo data = squad.TryGetExternalBaseInfoAs<ReapersPerUnitBaseInfo>();
                if (data == null)
                    continue;

                planetsInfluenced.AddIfNotAlreadyIn( squad.Planet );
                PlanetFaction pFaction = squad.PlanetFaction;
                if ( pFaction.AIPLeftFromCommandStation != 0 )
                {
                    MinorFactionAIPEquivalentIncrease( (FInt)pFaction.AIPLeftFromCommandStation + pFaction.AIPLeftFromWarpGate );
                    pFaction.AIPLeftFromCommandStation = 0;
                    pFaction.AIPLeftFromWarpGate = 0;
                }
            }

            AttachedFaction.SetInfluenceForPlanetsToList( planetsInfluenced );
            Planet.ReleaseTemporaryPlanetList( planetsInfluenced );
        }
        #region GetPlanetForChrysalis
        private readonly DrawBag<Planet> WorkingPlanetBag = DrawBag<Planet>.Create_WillNeverBeGCed( 30, "TemplarFactionDeepInfo-WorkingPlanetBag" );
        public Planet GetPlanetForChrysalis( ArcenHostOnlySimContext Context, Int16 minHopsFromHumanPlanet, Int16 maxHopsFromHumanPlanet, bool allowUnexplored, bool mustBeAdjacentToRavaged )
        {
            WorkingPlanetBag.Clear();
            FInt AIP = FactionUtilityMethods.Instance.GetCurrentAIP();
            if ( minHopsFromHumanPlanet == -1 )
                minHopsFromHumanPlanet = (Int16)2;
            if ( maxHopsFromHumanPlanet == -1 )
                maxHopsFromHumanPlanet = (Int16)(5 + (int)(AIP/150));
            byte maxMarkLevel = (byte)(3 + (byte)(AIP/150));
            int allowedRetries = 6; //was 100, and that's likely to break the game in the late game.
            int retries = 0;
            do
            {
                if ( retries % 5 == 0 )
                {
                    //the previous attempt was too restrictive
                    minHopsFromHumanPlanet--;
                    maxHopsFromHumanPlanet++;
                    maxMarkLevel++;
                }
                retries++;
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    if (!allowUnexplored &&
                         planet.IntelLevel == PlanetIntelLevel.Unexplored)
                        continue;
                    if ( planet.GetControllingOrInfluencingFaction().Type == FactionType.AI &&
                         planet.MarkLevelForAIOnly.Ordinal > maxMarkLevel)
                        continue; //honour the maxMarkLevel where appropriate
                    if (maxMarkLevel < 7 &&
                        planet.GetControllingOrInfluencingFaction().Type == FactionType.AI &&
                         planet.PopulationType == PlanetPopulationType.AIBastionWorld)
                        continue; //try not to seed on bastion worlds
                    if (FactionUtilityMethods.Instance.GetPlanetoidOnPlanetOrNull(planet) != null)
                        continue; //never on a planet with an asteroid, since we use asteroid drills for chrysalises as well, and you can't choose which

                    if (FactionUtilityMethods.Instance.GetAsteroidOnPlanetOrNull(planet) != null)
                        continue; //never on a planet with an asteroid, since we use asteroid drills for chrysalises as well, and you can't choose which
                    if (FactionUtilityMethods.Instance.GetReaperChrysalisOnPlanetOrNull(planet) != null)
                        continue; //never on a planet with a gateway or a chrysalis, we can't double up
                    if (FactionUtilityMethods.Instance.GetReaperGatewayOnPlanetOrNull(planet) != null)
                        continue; //never on a planet with a gateway or a chrysalis, we can't double up

                    if (FactionUtilityMethods.Instance.GetDysonSphereOnPlanetOrNull(planet) != null)
                        continue; //never on a planet with Dyson Sphere, gateways use the same visual tech as dyson spheres, so it looks super glitchy

                    if (FactionUtilityMethods.Instance.DoesPlanetHaveVengeanceGenerator(planet))
                        continue; //never on a planet with a VG, it makes it hard for them to get established

                    if (FactionUtilityMethods.Instance.GetCuendillarDrillOnPlanetOrNull(planet) != null)
                        continue; //never on a planet with an active drill, that could be a problem

                    if (mustBeAdjacentToRavaged)
                    {
                        bool nextToRavaged = false;
                        foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                        {
                            if ( neighbor.IsRavaged )
                            {
                                nextToRavaged = true;
                                break;
                            }
                        }
                        if ( !nextToRavaged )
                            continue;
                    }

                    if ( planet.IsRavaged )
                        continue;

                    var pFaction = planet.GetStanceDataForFaction( this.AttachedFaction );
                    if ( planet.GetControllingOrInfluencingFaction().Type == FactionType.Player )
                        continue;
                    int hopsToPlayerPlanet = 999;
                    if ( minHopsFromHumanPlanet > 1 )
                    {
                        //this planet must not be too close to a player planet
                        //to make the bounds listed above inclusive minHops - 1 must be used
                        bool foundPlayerPlanetWithinMinHops = false;
                        foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops_NoFilters( (Int16)(minHopsFromHumanPlanet - 1) ) )
                        {
                            Planet otherPlanet = _phd.Planet;
                            Int16 distance = _phd.Hops;
                            if ( distance < hopsToPlayerPlanet )
                                hopsToPlayerPlanet = distance;
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
                    //Prefer closer planets
                    int copiesForBag = 1;
                    if ( hopsToPlayerPlanet <= 2 )
                        copiesForBag += 1;
                    else if ( hopsToPlayerPlanet <= 5 )
                        copiesForBag += 2;
                    else
                        copiesForBag = 1;
                    var factionData = planet.GetStanceDataForFaction(AttachedFaction);
                    if (factionData[FactionStance.Hostile].TotalStrength > 10 * 1000 )
                        copiesForBag += 4; //if we happen to have strength there already, that's great
                    if ( planet.GetControllingFactionType() == FactionType.NaturalObject )
                        copiesForBag += 4; //strongly prefer neutral planets

                    WorkingPlanetBag.AddItem( planet, copiesForBag);
                }
            }  while ( WorkingPlanetBag.InternalListSize == 0 && retries < allowedRetries );
            return WorkingPlanetBag.PickRandomItemAndDoNotReplace( Context.RandomToUse );
        }
        #endregion
        #region GetWeakestRavagedPlanet
        public Planet GetWeakestRavagedPlanet(ArcenHostOnlySimContext Context)
        {
            WorkingPlanetBag.Clear();
            for (int i = 0; i < this.BaseInfo.RavagedPlanets.Count; i++)
            {
                Planet planet = this.BaseInfo.RavagedPlanets.GetDisplayList()[i];
                if ( planet.GetControllingFactionType() == FactionType.Player )
                    continue; //not on player planets
                bool foundSphere = false;
                foreach ( GameEntity_Squad e in World_AIW2.Instance.Squads( "DysonSidekickSphere" ) )
                {
                    if ( e.Planet == planet )
                    {
                        foundSphere = true;
                        break;
                    }
                }
                if ( foundSphere )
                    continue;
                WorkingPlanetBag.AddItem(planet, 1);
                var factionData = planet.GetStanceDataForFaction( AttachedFaction );
                if (factionData[FactionStance.Hostile].TotalStrength < 5 * 1000 )
                {
                    WorkingPlanetBag.AddItem(planet, 4);
                }
            }
            return WorkingPlanetBag.PickRandomItemAndDoNotReplace( Context.RandomToUse );
        }

        #endregion
        #region GetPlanetForPlanetoid
        public Planet GetPlanetForPlanetoid( ArcenHostOnlySimContext Context, Int16 minHopsFromHumanPlanet, Int16 maxHopsFromHumanPlanet, bool allowUnexplored )
        {
            WorkingPlanetBag.Clear();
            FInt AIP = FactionUtilityMethods.Instance.GetCurrentAIP();
            if ( minHopsFromHumanPlanet == -1 )
                minHopsFromHumanPlanet = (Int16)2;
            if ( maxHopsFromHumanPlanet == -1 )
                maxHopsFromHumanPlanet = (Int16)(5 + (int)(AIP/150));
            byte maxMarkLevel = (byte)(3 + (byte)(AIP/150));
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
                retries++;
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( !allowUnexplored &&
                         planet.IntelLevel == PlanetIntelLevel.Unexplored )
                        continue;
                    if ( planet.MarkLevelForAIOnly.Ordinal > maxMarkLevel )
                        continue; //honour the maxMarkLevel
                    if ( maxMarkLevel < 7 &&
                         planet.PopulationType == PlanetPopulationType.AIBastionWorld )
                        continue; //try not to seed on bastion worlds
                    if ( planet.IsRavaged )
                        continue;
                    if ( FactionUtilityMethods.Instance.GetAsteroidOnPlanetOrNull( planet ) != null )
                        continue; //never on a planet with an asteroid, since we use asteroid drills for chrysalises as well, and you can't choose which
                    if ( FactionUtilityMethods.Instance.GetPlanetoidOnPlanetOrNull( planet ) != null )
                        continue; //never on a planet with an asteroid, since we use asteroid drills for chrysalises as well, and you can't choose which

                    var pFaction = planet.GetStanceDataForFaction( this.AttachedFaction );
                    if ( planet.GetControllingOrInfluencingFaction().Type == FactionType.Player )
                        continue;
                    int hopsToPlayerPlanet = 999;
                    if ( minHopsFromHumanPlanet > 1 )
                    {
                        //this planet must not be too close to a player planet
                        //to make the bounds listed above inclusive minHops - 1 must be used
                        bool foundPlayerPlanetWithinMinHops = false;
                        foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops_NoFilters( (Int16)(minHopsFromHumanPlanet - 1) ) )
                        {
                            Planet otherPlanet = _phd.Planet;
                            Int16 distance = _phd.Hops;
                            if ( distance < hopsToPlayerPlanet )
                                hopsToPlayerPlanet = distance;
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
                    bool foundChrysalisAlready = false;

                    foreach ( GameEntity_Squad chrysalis in BaseInfo.Chrysalises.DisplaySquads() )
                    {
                        if ( chrysalis.Planet == planet )
                        {
                            foundChrysalisAlready = true;
                            break;
                        }
                    }
                    if ( foundChrysalisAlready )
                        continue;
                    
                    //Prefer closer planets
                    int copiesForBag = 1;
                    if ( hopsToPlayerPlanet <= 2 )
                        copiesForBag = 2;
                    else if ( hopsToPlayerPlanet <= 5 )
                        copiesForBag = 4;
                    else
                        copiesForBag = 1;
                    if ( planet.GetControllingFactionType() != FactionType.AI && copiesForBag > 1 )
                        copiesForBag = 1; //strongly prefer to be on AI planets

                    WorkingPlanetBag.AddItem( planet, copiesForBag);
                }
            }  while ( WorkingPlanetBag.InternalListSize == 0 && retries < allowedRetries );

            return WorkingPlanetBag.PickRandomItemAndDoNotReplace( Context.RandomToUse );
        }
        #endregion

        public static void HandleReaperNecromancy(GameEntity_Squad entity,  ArcenHostOnlySimContext Context)
        {
            //Make a new version of 'entity' for faction
            int debugCode = 0;
            try
            {
                debugCode = 100;
                if (entity == null )
                    return;
                debugCode = 200;
                Faction faction = FactionUtilityMethods.Instance.GetReapersFaction();
                GameEntityTypeData typeData = entity.TypeData;
                GameEntityTypeData newTypeData = null;
                debugCode = 300;
                if (typeData.IsStrikecraft || typeData.GetHasTag("BecomesNecromancySkeleton"))
                {
                    debugCode = 400;
                    newTypeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "UndeadFleetshipSummon");
                }
                else if (typeData.SpecialType == SpecialEntityType.Frigate ||
                          typeData.SpecialType == SpecialEntityType.AIGuardian ||
                          typeData.GetHasTag("BecomesNecromancyWight"))
                {
                    debugCode = 500;
                    newTypeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "UndeadGuardianSummon");
                }
                else if (typeData.SpecialType == SpecialEntityType.AIDireGuardian ||
                          typeData.GetHasTag("BecomesNecromancyMummy"))
                {
                    debugCode = 600;
                    newTypeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "UndeadMummySummon");
                }
                debugCode = 700;
                if (newTypeData == null)
                {
                    //this is some other sort of unit (perhaps a drone?). Do nothing
                    debugCode = 800;
                    return;
                }
                debugCode = 900;
                if (newTypeData != null)
                {
                    debugCode = 1000;
                    PlanetFaction pFactionForNewEntity = entity.Planet.GetPlanetFactionForFaction(faction);
                    debugCode = 1100;
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(pFactionForNewEntity, newTypeData, entity.CurrentMarkLevel,
                                                                                                 faction.LooseFleet, 0, entity.WorldLocation, Context, "DeathEffect-ReaperNecromancy");
                    newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
                    debugCode = 1200;
                    if (newEntity != null)
                    {
                        debugCode = 1300;
                        int antiPingPongDamage = entity.NumTimesZombified * (newEntity.GetMaxHullPoints() / 7);
                        newEntity.TakeDamageDirectly(antiPingPongDamage, null, null, DamageSource.BeingScrapped, Context); //this is okay because it will get caught in the fast-blast sync of being new
                        newEntity.NumTimesZombified++;
                    }
                }
            } catch (Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in Reaper Necromancer, debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        public static readonly List<SafeSquadWrapper> UnassignedShipsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 150, "ReapersFactionDeepInfo-UnassignedShipsLRP" );
        public static readonly DictionaryOfLists<Planet, SafeSquadWrapper> UnassignedShipsByPlanetLRP = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 60, "ReapersFactionDeepInfo-UnassignedShipsByPlanetLRP" );
        public static readonly DictionaryOfLists<Planet, SafeSquadWrapper> InCombatShipsNeedingOrdersLRP = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 60, "ReapersFactionDeepInfo-InCombatShipsNeedingOrdersLRP" );
        public static readonly List<SafeSquadWrapper> UnassignedFireteamShips = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ReapersFactionDeepInfo-UnassignedFireteamShips" );
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DysonSidekick );

            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Reapers-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            int debugCode = 0;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                debugCode = 100;
                UnassignedShipsLRP.Clear();
                UnassignedShipsByPlanetLRP.Clear();
                InCombatShipsNeedingOrdersLRP.Clear();
                MobileRavagersLRP.Clear();
                ImmobileRavagersLRP.Clear();
                LarvaeLRP.Clear();

                UnassignedFireteamShips.Clear();
                TeamsAimedAtPlanet.Clear();
                int totalShips = 0;
                int totalStrength = 0;

                foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
                {
                    team.IsAllowedToStack = true; //this is added to help existing saves; it can be removed later
                    team.DeepInfo.Reset(); //reset team count information
                }

                //Iterate over all our units to figure out if any need orders
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    debugCode = 200;
                    if (entity == null)
                        continue;

                    debugCode = 101;
                    if (entity.TypeData.GetHasTag("MobileRavager"))
                    {
                        //these are handled separately
                        debugCode = 300;
                        MobileRavagersLRP.Add(entity);
                        continue;
                    }
                    if (entity.TypeData.GetHasTag("ReaperLarva"))
                    {
                        //these are handled separately
                        debugCode = 300;
                        LarvaeLRP.Add(entity);
                        continue;
                    }

                    if (entity.TypeData.GetHasTag("ImmobileRavager"))
                    {
                        //these are handled separately
                        debugCode = 300;
                        ImmobileRavagersLRP.Add(entity);
                        continue;
                    }

                    if (!entity.TypeData.IsMobileCombatant)
                        continue;

                    if (entity.TypeData.GetHasTag("FireteamForces"))
                    {
                        if (entity.FireteamId < 0)
                            UnassignedFireteamShips.Add(entity);
                        else
                        {
                            Fireteam team = FireteamBaseUtility.GetFireteamById(BaseInfo.Teams, entity.FireteamId);
                            if (team != null)
                            {
                                team.DeepInfo.AddUnit(entity);
                            }
                            else
                                entity.FireteamId = -1; //something happened to the fireteam, so lets find a new one next LRP stage
                        }
                        entity.MinorFactionStackingID = -1; //this is added to help existing saves; it can be removed later
                        continue;
                    }

                    totalShips++;
                    totalStrength += entity.GetStrengthOfSelfAndContents();
                    Planet eventualDestinationOrNull = entity.Orders.GetFinalDestinationOrNull();
                    if (entity.HasExplicitOrders())
                    {
                        debugCode = 600;
                        //entity is doing something already (either attacking a target or en route somewhere)
                        if (eventualDestinationOrNull == null && entity.HasExplicitOrders())
                            continue; //we are going somewhere on this planet, that's not an FRD order (like we have specifically chosen a target), let this ship keep doing what it's doing
                        if (eventualDestinationOrNull != null)
                        {
                            var factionData = eventualDestinationOrNull.GetStanceDataForFaction(AttachedFaction);
                            if (factionData[FactionStance.Hostile].TotalStrength > 0 ||
                                 eventualDestinationOrNull.GetControllingFaction().GetIsHostileTowards(AttachedFaction))
                                continue; //if the planet we are going has enemies, keep going there
                        }
                    }

                    debugCode = 700;

                    if (entity.Planet.GetControllingOrInfluencingFaction().GetIsHostileTowards(AttachedFaction))
                    {
                        //This entity is on an enemy planet but doesn't have orders to attack a specific valuable target
                        //This means we will always kill a command stations before moving on
                        debugCode = 800;
                        var factionData = entity.Planet.GetStanceDataForFaction(AttachedFaction);
                        if (factionData[FactionStance.Hostile].TotalStrength > 0)
                        {
                            InCombatShipsNeedingOrdersLRP[entity.Planet].Add(entity);
                            continue;
                        }
                    }
                    GameEntity_Squad potentialTarget = entity.Planet.GetAnyCityCenterOrNull();
                    if ( potentialTarget != null )
                    {
                        InCombatShipsNeedingOrdersLRP[entity.Planet].Add(entity);
                        continue;
                    }
                    if (entity.Planet.GetControllingOrInfluencingFaction().GetIsFriendlyTowards(AttachedFaction))
                    {
                        //on a friendly planet, just fight
                        var factionData = entity.Planet.GetStanceDataForFaction(AttachedFaction);
                        if (factionData[FactionStance.Hostile].TotalStrength > 3000) //if there is at least 3 strength there that we are enemies to
                        {
                            //This entity is on an allied planet with at least 3 enemy strength there, but doesn't have orders to attack a specific valuable target
                            //So go kill some dudes
                            debugCode = 900;
                            InCombatShipsNeedingOrdersLRP[entity.Planet].Add(entity);

                            continue;
                        }
                    }
                    //This unit doesn't have any active orders and isn't on an enemy planet. Find an enemy planet
                    debugCode = 1000;
                    UnassignedShipsLRP.Add(entity);
                    UnassignedShipsByPlanetLRP[entity.Planet].Add(entity);
                }
                debugCode = 1100;
                HandleMobileRavagersLRP(Context, pathingCacheData);
                HandleLarvaeLRP(Context, pathingCacheData);

                debugCode = 1300;
                if (tracing && totalShips > 0)
                    tracingBuffer.Add(totalShips + " with strength " + (totalStrength / 1000) + " in the Reapers Assault right now.\n");

                //First, handle the ships not in combat
                //Here's the rule. First we pick our preferred planets (player or civil war AI planets), then we sort them
                //We prefer close and weak player planets.
                List<Planet> preferredTargets = Planet.GetTemporaryPlanetList("Reapers-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-preferredTargets", 10f);
                if ( preferredTargets == null ) //blocked for teardown/shutdown; bail
                    return;
                List<Planet> fallbackTargets = Planet.GetTemporaryPlanetList("Reapers-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-fallbackTargets", 10f);
                if ( fallbackTargets == null ) //blocked for teardown/shutdown; bail
                {
                    Planet.ReleaseTemporaryPlanetList(preferredTargets);
                    return;
                }

                foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in UnassignedShipsByPlanetLRP )
                {
                    debugCode = 1400;
                    Planet startPlanet = pair.Key;
                    if (tracing)
                        tracingBuffer.Add(pair.Value.Count + " ships on " + startPlanet.Name + " are looking for a target\n");
                    preferredTargets.Clear();
                    fallbackTargets.Clear();
                    debugCode = 1500;
                    foreach ( Planet.PlanetAtHopDistance _phd in startPlanet.PlanetsWithinXHops( -1,
                        delegate (Planet secondaryPlanet)
                        {
                            debugCode = 1700;
                            //don't path through hostile planets.
                            var factionData = secondaryPlanet.GetStanceDataForFaction(AttachedFaction);
                            int enemyStrength = factionData[FactionStance.Hostile].TotalStrength;
                            int friendlyStrength = factionData[FactionStance.Self].TotalStrength + factionData[FactionStance.Friendly].TotalStrength;
                            if (enemyStrength < 1 * 1000)
                                return PropogationEvaluation.Yes;
                            if (enemyStrength > friendlyStrength + 10 * 1000)
                            {
                                if (tracing)
                                    tracingBuffer.Add("We will check " + secondaryPlanet.Name + " but not neighbors. Enemy strength " + enemyStrength + " friendlyStrength " + friendlyStrength + ". \n");
                                return PropogationEvaluation.SelfButNotNeighbors;
                            }
                            return PropogationEvaluation.Yes;
                        }) )
                    {
                        Planet planet = _phd.Planet;
                        debugCode = 1600;
                        if (tracing)
                            tracingBuffer.Add("Checking " + planet.Name + " to see if its a target\n");

                        if (IsPreferredTarget(planet, Context))
                        {
                            preferredTargets.Add(planet);
                            continue;
                        }

                        if (IsFallbackTarget(planet, Context))
                            fallbackTargets.Add(planet);
                        else if (tracing)
                            tracingBuffer.Add("Not a fallback\n");
                    }
                    debugCode = 1800;

                    if (tracing)
                    {
                        tracingBuffer.Add("\tWe have " + preferredTargets.Count + " preferred targets\n");
                        for (int i = 0; i < preferredTargets.Count; i++)
                            tracingBuffer.Add("\t\t" + preferredTargets[i].Name).Add("\n");
                        if (fallbackTargets.Count > 0)
                        {
                            tracingBuffer.Add("\tWe have " + fallbackTargets.Count + " fallback targets\n");
                            for (int i = 0; i < fallbackTargets.Count; i++)
                                tracingBuffer.Add("\t\t" + fallbackTargets[i].Name).Add("\n");
                        }
                    }
                    debugCode = 1900;
                    //choose the target. First try to get a preferred planet
                    Planet target = null;
                    if (preferredTargets.Count > 0)
                    {
                        debugCode = 2000;
                        if (tracing)
                            tracingBuffer.Add("\tAttempting to pick a preferredTarget\n");
                        cb_reapersFaction = AttachedFaction;
                        cb_reapersStartPlanet = startPlanet;
                        debugCode = 2100;
                        preferredTargets.Sort(static delegate (Planet L, Planet R)
                        {
                            //To sort the planets, we factor how scary a planet is and how far it is away. We prefer nearer and weaker targets
                            //TODO if desired: also say "if this has an AIP increaser, want to attack it a bit more"
                            //that would make it a tad more evil
                            var lFactionData = L.GetStanceDataForFaction(cb_reapersFaction);
                            var rFactionData = R.GetStanceDataForFaction(cb_reapersFaction);
                            int lhops = cb_reapersStartPlanet.GetHopsTo(L);
                            int rhops = cb_reapersStartPlanet.GetHopsTo(R);
                            int lEnemyStrength = lFactionData[FactionStance.Hostile].TotalStrength;
                            int rEnemyStrength = rFactionData[FactionStance.Hostile].TotalStrength;
                            int lFriendlyStrength = lFactionData[FactionStance.Friendly].TotalStrength +
                                lFactionData[FactionStance.Self].TotalStrength;
                            int rFriendlyStrength = rFactionData[FactionStance.Friendly].TotalStrength +
                                rFactionData[FactionStance.Self].TotalStrength;

                            int lstrength = lEnemyStrength - lFriendlyStrength;
                            int rstrength = rEnemyStrength - rFriendlyStrength;
                            FInt factorPerHop = FInt.FromParts(1, 500);
                            int lVal = (lhops * factorPerHop).IntValue * lstrength;
                            int rVal = (rhops * factorPerHop).IntValue * rstrength;

                            return lVal.CompareTo(rVal);
                        });
                        debugCode = 2200;
                        for (int i = 0; i < preferredTargets.Count; i++)
                        {
                            int percentToUse = 50;
                            if (Context.RandomToUse.Next(0, 100) < percentToUse)
                            {
                                //weighted choice
                                target = preferredTargets[i];
                                break;
                            }
                        }
                        if (target == null) //if we didn't pick one randomly, just take the best
                            target = preferredTargets[0];
                    }

                    debugCode = 2300;
                    if (target == null && fallbackTargets.Count > 0)
                    {
                        debugCode = 2400;
                        //this is very similar to the preferredTargets code above
                        if (tracing)
                            tracingBuffer.Add("\tAttempting to pick a fallbackTarget\n");
                        cb_reapersFaction = AttachedFaction;
                        cb_reapersStartPlanet = startPlanet;
                        fallbackTargets.Sort(static delegate (Planet L, Planet R)
                        {
                            var lFactionData = L.GetStanceDataForFaction(cb_reapersFaction);
                            var rFactionData = R.GetStanceDataForFaction(cb_reapersFaction);
                            int lhops = cb_reapersStartPlanet.GetHopsTo(L);
                            int rhops = cb_reapersStartPlanet.GetHopsTo(R);
                            int lEnemyStrength = lFactionData[FactionStance.Hostile].TotalStrength;
                            int rEnemyStrength = rFactionData[FactionStance.Hostile].TotalStrength;
                            int lFriendlyStrength = lFactionData[FactionStance.Friendly].TotalStrength +
                                lFactionData[FactionStance.Self].TotalStrength;
                            int rFriendlyStrength = rFactionData[FactionStance.Friendly].TotalStrength +
                                rFactionData[FactionStance.Self].TotalStrength;

                            int lstrength = lEnemyStrength - lFriendlyStrength;
                            int rstrength = rEnemyStrength - rFriendlyStrength;
                            FInt factorPerHop = FInt.FromParts(1, 000);

                            int lVal = (lhops / factorPerHop).IntValue * lstrength;
                            int rVal = (rhops / factorPerHop).IntValue * rstrength;

                            return lVal.CompareTo(rVal);
                        });
                        for (int i = 0; i < fallbackTargets.Count; i++)
                        {
                            if (Context.RandomToUse.Next(0, 100) < 50)
                            {
                                target = fallbackTargets[i];
                                break;
                            }
                        }
                        if (target == null) //if we didn't pick one randomly, just take the best
                            target = fallbackTargets[0];
                    }


                    debugCode = 2500;
                    if (target == null)
                        continue;
                    if (tracing)
                    {
                        tracingBuffer.Add("\tSending " + pair.Value.Count + " ships from " + startPlanet.Name + " to attack " + target.Name).Add("\n");
                    }
                    debugCode = 2600;
                    AttackTargetPlanet(startPlanet, target, pair.Value, Context, pathingCacheData);
                }

                Planet.ReleaseTemporaryPlanetList(preferredTargets);
                Planet.ReleaseTemporaryPlanetList(fallbackTargets);

                debugCode = 3000;
                //This is a ship on a planet controlled by our enemies. Pick a target and go after it.
                List<SafeSquadWrapper> targetSquads = GameEntity_Squad.GetTemporarySquadList("Reapers-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-targetSquads", 10f);
                if ( targetSquads == null ) //blocked for teardown/shutdown; bail
                    return;

                debugCode = 3100;
                foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in InCombatShipsNeedingOrdersLRP )
                {
                    debugCode = 3200;
                    PlanetFaction pFaction = pair.Key.GetControllingPlanetFaction();
                    Planet planet = pair.Key;
                    targetSquads.Clear();
                    if (tracing)
                        tracingBuffer.Add("Finding a target for " + pair.Value.Count + " ships on " + pair.Key.Name).Add(" if necessary\n");

                    int rand = Context.RandomToUse.Next(0, 100);
                    var factionData = planet.GetStanceDataForFaction(AttachedFaction);
                    int enemyStrength = factionData[FactionStance.Hostile].TotalStrength;
                    int friendlyStrength = factionData[FactionStance.Self].TotalStrength + factionData[FactionStance.Friendly].TotalStrength;

                    if (enemyStrength < friendlyStrength / 10) //Reapers can tachyon blast planets to prevent a small number of cloaked units from disrupting things. This is particularly possible with civilian industry cloaked defensive structures
                        FactionUtilityMethods.Instance.TachyonBlastPlanet(planet, AttachedFaction, Context);


                    if (planet.GetControllingFactionType() == FactionType.Player)
                    {
                        //This is a player planet, so lets see if we can do anything clever/sneaky

                        Planet kingPlanet = null;
                        if (FactionUtilityMethods.Instance.IsPlanetAdjacentToPlayerKing(planet, out kingPlanet))
                        {
                            //First, if we are adjacent to a player homeworld then go for that if we can
                            var neighborFactionData = kingPlanet.GetStanceDataForFaction(AttachedFaction);
                            StrengthData_PlanetFaction_Stance neighborHostileStrengthData = neighborFactionData[FactionStance.Hostile];
                            int neighborHostileStrengthTotal = neighborHostileStrengthData.TotalStrength;
                            if (neighborHostileStrengthTotal < (friendlyStrength) / 2)
                            {
                                //if we are too much weaker than the AI homeworld, don't bother. Only if we might make things interesting
                                GameEntity_Other thisWormhole = planet.GetWormholeTo(kingPlanet);
                                bool foundBlockingShield = FactionUtilityMethods.Instance.IsShieldBlockingWormholeToPlanet(planet, kingPlanet, AttachedFaction);
                                if (!foundBlockingShield)
                                {
                                    if (tracing)
                                        tracingBuffer.Add("\tWe are going to attack the player king on ").Add(kingPlanet.Name).Add("\n");

                                    GameCommand sneakCommand = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_AIRaidKing], GameCommandSource.AnythingElse);
                                    debugCode = 3300;
                                    for (int j = 0; j < pair.Value.Count; j++)
                                    {
                                        sneakCommand.RelatedEntityIDs.Add(pair.Value[j].PrimaryKeyID);
                                    }

                                    debugCode = 3400;
                                    if (sneakCommand != null && sneakCommand.RelatedEntityIDs.Count > 0)
                                    {
                                        sneakCommand.RelatedString = "RPR_WAVE_GOKING";
                                        sneakCommand.ToBeQueued = false;
                                        sneakCommand.RelatedIntegers.Add(kingPlanet.Index);
                                        World_AIW2.Instance.QueueGameCommand(this.AttachedFaction, sneakCommand, playAudioEffectForCommand);
                                        continue;
                                    }
                                }
                            }
                        }

                    if (Context.RandomToUse.Next(0, 100) < 75)
                    {
                        debugCode = 3500;
                        GameEntity_Squad target = planet.GetAnyCityCenterOrNull() ?? planet.GetCommandStationOrNull();
                        if (target != null &&
                            (this.BaseInfo.Gateways.GetDisplayList().Count > 0 || target.PlanetFaction.Faction.Type == FactionType.Player ) )
                        {
                                debugCode = 3600;
                                GameCommand commandStationAttackCommand = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.Attack], GameCommandSource.AnythingElse);
                                commandStationAttackCommand.ToBeQueued = false;

                                commandStationAttackCommand.RelatedIntegers4.Add(target.PrimaryKeyID);
                                debugCode = 3700;
                                for (int j = 0; j < pair.Value.Count; j++)
                                {
                                    commandStationAttackCommand.RelatedEntityIDs.Add(pair.Value[j].PrimaryKeyID);
                                }
                                debugCode = 3800;
                                World_AIW2.Instance.QueueGameCommand(this.AttachedFaction, commandStationAttackCommand, playAudioEffectForCommand);
                                if (tracing) tracingBuffer.Add("\n").Add("Threat of " + commandStationAttackCommand.RelatedEntityIDs.Count + " units attacking command station");
                                commandStationAttackCommand = null;
                                continue;
                            }
                        }
                        if (enemyStrength > friendlyStrength)
                        {
                            //if we don't think we can comfortably win this (and remember, we are at a disadvantage when attacking a player)
                            //then see if we can bypass and find a weaker target
                            debugCode = 3900;
                            rand = Context.RandomToUse.Next(0, 100); //recalculate the random number
                            if (rand < 50)
                            {
                                debugCode = 4000;
                                //If this planet seems pretty tough for me, see if there are any adjacent weaker player planets and go for those
                                //TODO: we should actually use a List here and select randomly in case there are multiple good options
                                //We might also want to enhance Helper_RetreatThreat to incorporate this style of 'sneaking past player defenses'
                                Planet newTarget = null;
                                int weakestPlanetNeighborStrength = 0;
                                foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                                {
                                    if (neighbor.GetControllingFactionType() != FactionType.Player)
                                        continue;
                                    debugCode = 4100;
                                    var neighborFactionData = neighbor.GetStanceDataForFaction(AttachedFaction);
                                    StrengthData_PlanetFaction_Stance neighborHostileStrengthData = neighborFactionData[FactionStance.Hostile];
                                    int neighborHostileStrengthTotal = neighborHostileStrengthData.TotalStrength - friendlyStrength;
                                    if (neighborHostileStrengthTotal > enemyStrength)
                                        continue; //not interested in more heavily defeneded planets

                                    debugCode = 4200;
                                    if (neighborHostileStrengthTotal < weakestPlanetNeighborStrength && !FactionUtilityMethods.Instance.IsShieldBlockingWormholeToPlanet(planet, neighbor, AttachedFaction))
                                    {
                                        weakestPlanetNeighborStrength = neighborHostileStrengthTotal;
                                        newTarget = neighbor;
                                    }
                                }
                                debugCode = 4300;
                                if (newTarget != null)
                                {
                                    debugCode = 4400;
                                    if (tracing) tracingBuffer.Add("\n").Add("Threat bypass to weaker planet: " + newTarget.Name).Add("\n");
                                    GameCommand sneakCommand = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_AIRaidKing], GameCommandSource.AnythingElse);
                                    for (int j = 0; j < pair.Value.Count; j++)
                                    {
                                        sneakCommand.RelatedEntityIDs.Add(pair.Value[j].PrimaryKeyID);
                                    }

                                    debugCode = 4500;
                                    if (sneakCommand.RelatedEntityIDs.Count > 0)
                                    {
                                        sneakCommand.RelatedString = "RPR_T_CHUNKS";
                                        sneakCommand.ToBeQueued = false;
                                        sneakCommand.RelatedIntegers.Add(newTarget.Index);
                                        World_AIW2.Instance.QueueGameCommand(this.AttachedFaction, sneakCommand, playAudioEffectForCommand);
                                        sneakCommand = null;
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                    if (tracing)
                        tracingBuffer.Add("No fancy behaviours. Find some targets\n");
                    debugCode = 4600;
                    //We haven't done any of the fancy behaviours, so the default behaviour is "Just fight"
                    //Exception: if the player has anything particularly toothsome on this planet, always kill that first
                    foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.KingUnitsOnly ) )
                    {
                        debugCode = 4610;
                        if (!entity.PlanetFaction.Faction.GetIsHostileTowards(AttachedFaction))
                            continue;
                        targetSquads.Add(entity);
                        break; ;
                    }
                    debugCode = 4620;
                    if (targetSquads.Count == 0)
                    {
                        debugCode = 4630;
                        if (planet == null)
                            ArcenDebugging.ArcenDebugLogSingleLine("??", Verbosity.DoNotShow);
                        //if we already had a target then it's a king, so don't bother
                        foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.CityCenter ) )
                        {
                            debugCode = 4640;
                            if (!entity.PlanetFaction.Faction.GetIsHostileTowards(AttachedFaction))
                                continue;
                            if ( entity.GetIsCrippled() || entity.SelfBuildingMetalRemaining > 0 )
                                continue;
                            targetSquads.Add(entity);
                        }
                    }
                    if (targetSquads.Count > 0)
                    {
                        //we know we have a tasty target
                        debugCode = 4700;
                        GameEntity_Squad target = targetSquads[Context.RandomToUse.Next(0, targetSquads.Count)].GetSquad();
                        if (target != null)
                        {
                            if (tracing)
                            {
                                tracingBuffer.Add("We are attacking " + target.ToStringWithPlanet() + ", one of " + targetSquads.Count + " target(s).\n");
                                bool debug = false;
                                if (tracing && debug)
                                {
                                    for (int i = 0; i < targetSquads.Count; i++)
                                    {
                                        tracingBuffer.Add(i + ": " + targetSquads[i].ToStringWithPlanet() + ".\n");
                                    }
                                }
                            }

                            GameCommand attackCommand = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.Attack], GameCommandSource.AnythingElse);
                            attackCommand.RelatedIntegers4.Add(target.PrimaryKeyID);
                            for (int j = 0; j < pair.Value.Count; j++)
                            {
                                attackCommand.RelatedEntityIDs.Add(pair.Value[j].PrimaryKeyID);
                            }

                            World_AIW2.Instance.QueueGameCommand(this.AttachedFaction, attackCommand, false);
                            continue;
                        }
                    }

                    if (tracing)
                        tracingBuffer.Add("Very boring; just fight very genericallys\n");

                    {
                        GameCommand attackCommand = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetBehavior_FromFaction_NoPraetorianGuard], GameCommandSource.AnythingElse);
                        attackCommand.RelatedMagnitude = (int)EntityBehaviorType.Attacker_Full;
                        for (int j = 0; j < pair.Value.Count; j++)
                        {
                            attackCommand.RelatedEntityIDs.Add(pair.Value[j].PrimaryKeyID);
                        }
                    }
                }

                GameEntity_Squad.ReleaseTemporarySquadList(targetSquads);

                {
                    //Fireteam code
                    FInt overkillRequired = FInt.FromParts( 1, 100 );
                    FireteamUtility.UpdateFireteams( AttachedFaction, Context, pathingCacheData, BaseInfo.Teams, TeamsAimedAtPlanet, tracingBuffer, overkillRequired );
                    FireteamUtility.UpdateRegiments( AttachedFaction, Context, pathingCacheData, BaseInfo.Teams, TeamsAimedAtPlanet, tracingBuffer, AttachedFaction.MinFireteamStrength, true );
                    int maxUnitsPerLRPToAdd = 1000;
                    if ( tracing )
                        tracingBuffer.Add( "There are " + UnassignedFireteamShips.Count + " ships needing fireteams; limit ourselves to the first " + maxUnitsPerLRPToAdd + ". Max fireteam strength: " + AttachedFaction.MaxFireteamStrength + "\n" );
                    for ( int i = 0; (i < UnassignedFireteamShips.Count && i < maxUnitsPerLRPToAdd); i++ )
                        AssignUnitToFireteam( AttachedFaction, UnassignedFireteamShips[i].GetSquad(), Context, pathingCacheData );
                    if ( AttachedFaction.NumFireteams != BaseInfo.Teams.GetItemCount() )
                        AttachedFaction.NumFireteams = BaseInfo.Teams.GetItemCount();

                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in ReapersLogic LRP. debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();
                FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
            }
        }
        public void HandleMobileRavagersLRP(ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData)
        {
            int debugCode = 0;
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DysonSidekick );

            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Reapers-HandleMobileRavagersLRP-trace", 10f ) : null;

            try
            {
                debugCode = 100;
                List<Planet> usedPlanets = Planet.GetTemporaryPlanetList("Reapers-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-HandleMobileRavagersLRP", 10f);
                if ( usedPlanets == null ) //blocked for teardown/shutdown; bail
                    return;
                for (int i = 0; i < MobileRavagersLRP.Count; i++)
                {
                    debugCode = 200;
                    GameEntity_Squad entity = MobileRavagersLRP[i].GetSquad();
                    if (entity == null)
                        continue;
                    if (entity.HasQueuedOrders())
                        continue;
                    debugCode = 300;
                    var factionData = entity.Planet.GetStanceDataForFaction( AttachedFaction );
                    if (IsPlanetRavagable(entity.Planet, ImmobileRavagersLRP) &&
                        factionData[FactionStance.Hostile].TotalStrength < 20 * 1000)
                    {
                        SendShipToLocation(entity, Engine_AIW2.Instance.CombatCenter, Context);
                        continue;
                    }
                    debugCode = 400;
                    Planet nextPlanet = GetNextRavagerPlanet(entity, ImmobileRavagersLRP, Context);
                    if (nextPlanet != null)
                    {
                        if ( tracing )
                            tracingBuffer.Add(entity.ToStringWithPlanet() + " is being dispatched to ravage " + nextPlanet.Name);
                        SendShipToPlanet(entity, nextPlanet, Context, PathCacheData);
                    }
                }
                Planet.ReleaseTemporaryPlanetList(usedPlanets);
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch (Exception e)
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine("hit exception in handlemobileravagerslrp debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow);
            }
            FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
        }
        public void HandleLarvaeLRP(ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData)
        {
            int debugCode = 0;
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DysonSidekick );

            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Reapers-HandleLarvaeLRP-trace", 10f ) : null;
            try
            {
                debugCode = 100;
                List<Planet> usedPlanets = Planet.GetTemporaryPlanetList("Reapers-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-HandleLarvaeLRP", 10f);
                if ( usedPlanets == null ) //blocked for teardown/shutdown; bail
                    return;
                for (int i = 0; i < LarvaeLRP.Count; i++)
                {
                    debugCode = 200;
                    GameEntity_Squad entity = LarvaeLRP[i].GetSquad();
                    if (entity == null)
                        continue;
                    bool newPlanetNeeded = false;

                    if (entity.HasQueuedOrders())
                    {
                        Planet destination = entity.GetDestinationPlanet();

                        if ( FactionUtilityMethods.Instance.GetReaperChrysalisOnPlanetOrNull( destination ) == null &&
                             FactionUtilityMethods.Instance.GetReaperGatewayOnPlanetOrNull( destination ) == null &&
                             FactionUtilityMethods.Instance.GetCuendillarDrillOnPlanetOrNull( destination ) == null )
                        {
                            continue;
                        }

                        newPlanetNeeded = true;
                    }
                    debugCode = 300;
                    var factionData = entity.Planet.GetStanceDataForFaction( AttachedFaction );
                    Planet planet = entity.Planet;

                    if ( FactionUtilityMethods.Instance.GetReaperChrysalisOnPlanetOrNull( planet ) != null ||
                         FactionUtilityMethods.Instance.GetReaperGatewayOnPlanetOrNull( planet ) != null ||
                         FactionUtilityMethods.Instance.GetCuendillarDrillOnPlanetOrNull( planet ) != null )
                        newPlanetNeeded = true;
                    for (int j = 0; j < ImmobileRavagersLRP.Count; j++)
                    {
                        if ( ImmobileRavagersLRP[j].Planet == planet )
                            newPlanetNeeded = true;
                    }
                    if (!newPlanetNeeded)
                    {
                        SendShipToLocation(entity, Engine_AIW2.Instance.CombatCenter, Context);
                        continue;
                    }
                    debugCode = 400;
                    Planet nextPlanet = GetNextLarvaPlanet(entity, Context);
                    if (nextPlanet != null)
                    {
                        if ( tracing )
                            tracingBuffer.Add(entity.ToStringWithPlanet() + " is being dispatched to ravage " + nextPlanet.Name);
                        SendShipToPlanet(entity, nextPlanet, Context, PathCacheData);
                    }
                }
                Planet.ReleaseTemporaryPlanetList(usedPlanets);
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch (Exception e)
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine("hit exception in handlemobileravagerslrp debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow);
            }
            FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
        }
        private Planet GetNextLarvaPlanet( GameEntity_Squad entity, ArcenLongTermIntermittentPlanningContext Context )
        {
            int debugCode = 0;
            Planet startPlanet = entity.Planet;
            Planet target = null;
            List<Planet> preferredTargets = Planet.GetTemporaryPlanetList( "Reapers-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-RavagerTargets", 10f );
            if ( preferredTargets == null ) //blocked for teardown/shutdown; bail
                return null;
            try{
                bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DysonSidekick );
                ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Reapers-GetNextLarval-trace", 10f ) : null;

                debugCode = 100;
                if ( tracing )
                tracingBuffer.Add("Finding a target for " + entity.ToStringWithPlanet() + " to spawn a chrysalis.\n");
                foreach ( Planet.PlanetAtHopDistance _phd in startPlanet.PlanetsWithinXHops( -1,
                    delegate ( Planet secondaryPlanet )
                    {
                        debugCode = 700;
                        //don't path through hostile planets.

                        // if ( secondaryPlanet.IsRavaged )
                        //     return PropogationEvaluation.Yes;

                        var factionData = secondaryPlanet.GetStanceDataForFaction( AttachedFaction );
                        if ( tracing )
                            tracingBuffer.Add("Considering " + secondaryPlanet.Name + " for potentially becoming a destionation. hostile strength: " + factionData[FactionStance.Hostile].TotalStrength +"\n");

                        if (factionData[FactionStance.Hostile].TotalStrength > 10 * 1000)
                        {
                            if ( tracing )
                                tracingBuffer.Add("\tDue to the strength on this planet, don't path through " + secondaryPlanet.Name + " hostile strength: " + factionData[FactionStance.Hostile].TotalStrength +" > " + (10 * 1000) +"\n");

                            return PropogationEvaluation.SelfButNotNeighbors;
                        }
                        if ( tracing )
                            tracingBuffer.Add("Considering " + secondaryPlanet.Name + " for potentially becoming a destionation. SUCCESS\n");

                        return PropogationEvaluation.Yes;
                    } ) )
                {
                    Planet planet = _phd.Planet;
                    debugCode = 200;
                    if ( tracing )
                        tracingBuffer.Add("Considering " + planet.Name + " for a larval destionation\n");
                    if ( FactionUtilityMethods.Instance.GetReaperChrysalisOnPlanetOrNull( planet ) != null )
                        continue; //never on a planet with an asteroid, since we use asteroid drills for chrysalises as well, and you can't choose which
                    if ( FactionUtilityMethods.Instance.GetReaperGatewayOnPlanetOrNull( planet ) != null )
                        continue; //never on a planet with an asteroid, since we use asteroid drills for chrysalises as well, and you can't choose which
                    if (FactionUtilityMethods.Instance.GetDysonSphereOnPlanetOrNull(planet) != null)
                        continue; //never on a planet with Dyson Sphere, gateways use the same visual tech as dyson spheres, so it looks super glitchy
                    if (FactionUtilityMethods.Instance.GetPlanetoidOnPlanetOrNull(planet) != null)
                        continue; //never on a planet with an asteroid, since we use asteroid drills for chrysalises as well, and you can't choose which

                    if (FactionUtilityMethods.Instance.GetAsteroidOnPlanetOrNull(planet) != null)
                        continue; //never on a planet with an asteroid, since we use asteroid drills for chrysalises as well, and you can't choose which
                    if ( planet.IsRavaged )
                        continue;
                    if (FactionUtilityMethods.Instance.GetDysonSphereOnPlanetOrNull(planet) != null)
                        continue; //never on a planet with Dyson Sphere, gateways use the same visual tech as dyson spheres, so it looks super glitchy

                    bool foundImmobileRavager = false;
                    for (int j = 0; j < ImmobileRavagersLRP.Count; j++)
                    {
                        if ( ImmobileRavagersLRP[j].Planet == planet )
                        {
                            foundImmobileRavager = true;
                            break;
                        }
                    }
                    if ( foundImmobileRavager )
                        continue;

                    debugCode = 300;
                    if ( tracing )
                        tracingBuffer.Add("\tAdding " + planet.Name + " As a potential for larva\n");

                    preferredTargets.Add(planet);
                }
                debugCode = 800;
                if (preferredTargets.Count > 0)
                {
                    debugCode = 900;
                    cb_reapersFaction = AttachedFaction;
                    preferredTargets.Sort(static delegate (Planet L, Planet R)
                    {
                        var lData = L.GetStanceDataForFaction(cb_reapersFaction);
                        var rData = R.GetStanceDataForFaction(cb_reapersFaction);
                        return lData[FactionStance.Hostile].TotalStrength.CompareTo(rData[FactionStance.Hostile].TotalStrength);
                    } );
                    debugCode = 1000;
                    if (tracing)
                    {
                        tracingBuffer.Add("\tPossible ravage options, hopefully sorted with the weakest at the top:\n");
                        for (int i = 0; i < preferredTargets.Count; i++)
                            tracingBuffer.Add("\t\t").Add(preferredTargets[i].Name).Add("\n");
                    }

                    target = preferredTargets[0];
                }
                else
                {
                    if (tracing)
                        tracingBuffer.Add("\tNo Larva options found for " + entity.ToStringWithPlanet() + "\n");
                }

                FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit ecxeption in GetNextRavagerPlanet debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            Planet.ReleaseTemporaryPlanetList(preferredTargets);
            return target;
        }
        private Planet GetNextRavagerPlanet( GameEntity_Squad entity, List<SafeSquadWrapper> immobileRavagers, ArcenLongTermIntermittentPlanningContext Context )
        {
            int debugCode = 0;
            Planet startPlanet = entity.Planet;
            Planet target = null;
            List<Planet> preferredTargets = Planet.GetTemporaryPlanetList( "Reapers-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-RavagerTargets", 10f );
            if ( preferredTargets == null ) //blocked for teardown/shutdown; bail
                return null;
            try{
                bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DysonSidekick );
                ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Reapers-GetNextRavagerPlanet-trace", 10f ) : null;

                debugCode = 100;
                if ( tracing )
                tracingBuffer.Add("Finding a target for " + entity.ToStringWithPlanet() + " to ravage.\n");
                bool bonusDebug = false;
                foreach ( Planet.PlanetAtHopDistance _phd in startPlanet.PlanetsWithinXHops( -1,
                    delegate ( Planet secondaryPlanet )
                    {
                        debugCode = 700;
                        //don't path through hostile planets.

                        // if ( secondaryPlanet.IsRavaged )
                        //     return PropogationEvaluation.Yes;

                        var factionData = secondaryPlanet.GetStanceDataForFaction( AttachedFaction );
                        if ( factionData[FactionStance.Hostile].TotalStrength > 10 * 1000 )
                            return PropogationEvaluation.SelfButNotNeighbors;
                        return PropogationEvaluation.Yes;
                    } ) )
                {
                    Planet planet = _phd.Planet;
                    debugCode = 200;
                    if (planet.IsRavaged)
                    {
                        if ( tracing && bonusDebug )
                            tracingBuffer.Add("\tSkipping " + planet.Name + " it is already ravaged\n");
                        continue;
                    }
                    debugCode = 300;
                    bool skipPlanet = false;
                    for (int j = 0; j < immobileRavagers.Count; j++)
                    {
                        debugCode = 400;
                        GameEntity_Squad immobile = immobileRavagers[j].GetSquad();
                        if ( immobile == null )
                            continue;
                        debugCode = 500;
                        if (immobile.Planet == planet)
                        {
                            if ( tracing && bonusDebug )
                                tracingBuffer.Add("\tSkipping " + planet.Name + " it has an immobile ravager\n");
                            skipPlanet = true;
                            break;
                        }
                    }
                    if ( skipPlanet )
                        continue;
                    debugCode = 600;
                    List<SafeSquadWrapper> gateways = this.BaseInfo.Gateways.GetDisplayList();
                    for (int i = 0; i < gateways.Count; i++)
                    {
                        GameEntity_Squad squad = gateways[i].GetSquad();
                        if (squad == null)
                            continue;
                        if (squad.Planet == planet)
                        {
                            if ( tracing && bonusDebug )
                                tracingBuffer.Add("\tSkipping " + planet.Name + " it has a gateway\n");
                            skipPlanet = true;
                            break;
                        }
                    }
                    if ( skipPlanet )
                        continue;
                    if ( tracing )
                        tracingBuffer.Add("\tAdding " + planet.Name + " As a potential for ravaging\n");

                    preferredTargets.Add(planet);
                }
                debugCode = 800;
                if (preferredTargets.Count > 0)
                {
                    debugCode = 900;
                    cb_reapersFaction = AttachedFaction;
                    preferredTargets.Sort(static delegate (Planet L, Planet R)
                    {
                        var lData = L.GetStanceDataForFaction(cb_reapersFaction);
                        var rData = R.GetStanceDataForFaction(cb_reapersFaction);
                        return lData[FactionStance.Hostile].TotalStrength.CompareTo(rData[FactionStance.Hostile].TotalStrength);
                    } );
                    debugCode = 1000;
                    if (tracing)
                    {
                        tracingBuffer.Add("\tPossible ravage options, hopefully sorted with the weakest at the top:\n");
                        for (int i = 0; i < preferredTargets.Count; i++)
                            tracingBuffer.Add("\t\t").Add(preferredTargets[i].Name).Add("\n");
                    }

                    target = preferredTargets[0];
                }
                
                FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit ecxeption in GetNextRavagerPlanet debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            Planet.ReleaseTemporaryPlanetList(preferredTargets);
            return target;
        }
        public void SendShipToPlanet( GameEntity_Squad entity, Planet destination, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( entity.PlanetFaction.Faction, "ReaperssSendShipToPlanet", entity.Planet, destination, PathingMode.Safest, Context, PathCacheData );
            if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
            {
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse );
                command.RelatedString = "Reapers_Dest";
                command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                    command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
            }
        }
        public void SendShipToLocation( GameEntity_Squad entity, ArcenPoint dest, ArcenLongTermIntermittentPlanningContext Context )
        {
            GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse );
            moveCommand.PlanetOrderWasIssuedFrom = entity.Planet.Index;
            moveCommand.RelatedPoints.Add( dest );
            moveCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );
            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );
        }
        public bool IsPreferredTarget(Planet planet, ArcenLongTermIntermittentPlanningContext Context)
        {
            //Whether this is an enemy controlled planet. Note that we try not to overkill planets too badly
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DysonSidekick );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Reapers-IsPreferredTarget-trace", 10f ) : null;

            bool debug = false;
            if ( tracing && debug )
                tracingBuffer.Add("\tchecking " + planet.Name + " for preferredness\n");
            Faction controllingFaction = planet.GetControllingFaction();
            bool foundStronghold = false;
            bool foundNecropolis = false;

            foreach ( GameEntity_Squad ds in planet.Squads( "DysonStronghold" ) )
            {
                if ( ds != null )
                {
                    foundStronghold = true;
                    break;
                }
            }
            foreach ( GameEntity_Squad ds in planet.Squads( "NecromancerNecropolis" ) )
            {
                if ( ds != null )
                {
                    foundNecropolis = true;
                    break;
                }
            }
            bool playerOwned = controllingFaction.Type == FactionType.Player || foundStronghold || foundNecropolis;
            if ( !playerOwned )
            {
                if ( tracing && debug )
                    tracingBuffer.Add("\t\tNot owned by primary enemy (ie player). Owned by " + controllingFaction.GetDisplayName() +"\n");
                FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
                return false;
            }

            if ( !controllingFaction.GetIsHostileTowards(AttachedFaction) && !foundStronghold && !foundNecropolis)
            {
                if ( tracing && debug )
                    tracingBuffer.Add("\t\t" + controllingFaction.GetDisplayName() + " is friendly\n");
                FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
                return false;
            }
            var factionData = planet.GetStanceDataForFaction( AttachedFaction );
            int enemyStrength = factionData[FactionStance.Hostile].TotalStrength;
            int friendlyStrength = factionData[FactionStance.Friendly].TotalStrength + factionData[FactionStance.Self].TotalStrength ;
            if ( enemyStrength * 5 < friendlyStrength )
            {
                if ( tracing && debug )
                    tracingBuffer.Add("\t\t" + enemyStrength + " < " + (friendlyStrength * 5)+ ", so discard\n");
                FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
                return false; //if we outnumber 7 to 1, don't bother attacking
            }
            FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
            return true;
        }
        public bool IsFallbackTarget(Planet planet, ArcenLongTermIntermittentPlanningContext Context)
        {
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DysonSidekick );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Reapers-IsFallbackTarget-trace", 10f ) : null;
            Faction controllingFaction = planet.GetControllingOrInfluencingFaction();

            if ( controllingFaction.GetIsHostileTowards( this.AttachedFaction ))
                return true;
            var factionData = planet.GetStanceDataForFaction( AttachedFaction );
            int enemyStrength = factionData[FactionStance.Hostile].TotalStrength;
            int friendlyStrength = factionData[FactionStance.Friendly].TotalStrength + factionData[FactionStance.Self].TotalStrength ;
            #region Tracing
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
            #endregion
            if ( enemyStrength < 5 * 1000 )
                return false;
            if ( enemyStrength < friendlyStrength / 7 )
            {
                return false; //if we outnumber 7 to 1, don't bother attacking
            }

            return true;
        }
        public int CalculateSpeed(List<SafeSquadWrapper> ships, ArcenLongTermIntermittentPlanningContext Context)
        {
            //Return the speed we want these ships to use. It's "a bit faster than the average speed, and at least 500".
            //We make the speeds all a little bit different to make it less obvious to the player that we are using speed groups (since
            //ships from multiple planets will be going by at the same time, and moving at different speeds)
            int debugCode = 0;
            try{
                debugCode = 100;
                if ( ships == null || ships.Count == 0 )
                    return 0;
                int maxSpeed = 0;
                Int64 totalSpeed = 0;
                debugCode = 200;
                for ( int i = 0; i < ships.Count; i++ )
                {
                    debugCode = 300;
                    GameEntity_Squad squad = ships[i].GetSquad();
                    if ( squad == null )
                        continue;
                    debugCode = 400;
                    int newSpeed = squad.DataForMark.Speed;
                    totalSpeed += newSpeed;
                    if ( maxSpeed < newSpeed )
                        maxSpeed = newSpeed;
                }
                debugCode = 500;
                int average = (Int32)( totalSpeed / (Int64)ships.Count );

                average += average / 9; //a bit faster than average
                if ( average < 500 )
                    average = 500;
                debugCode = 600;
                average += Context.RandomToUse.Next(0, 50); //a little more randomness

                //if we only have one ship type, then they just go the speed the go, for example
                if ( average > maxSpeed )
                    average = maxSpeed;

                return average;
            }catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in ReapersFactionDeepInfo::CalculateSpeed debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            return 0;
        }
        public void AttackTargetPlanet(Planet start, Planet destination, List<SafeSquadWrapper> ships, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( ships.Count <= 0 )
                return;
            int debugCode = 0;
            try{
                bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DysonSidekick );
                ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Reapers-AttackTargetPlanet-trace", 10f ) : null;
                debugCode = 100;
                PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "ReaperssAttackTargetPlanet", start, destination, PathingMode.Safest, Context, PathCacheData );
                int gatheringDistance = -1;
                debugCode = 200;
                if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                {
                    debugCode = 400;
                    debugCode = 500;
                    if ( pathCache.PathToReadOnly.Count > gatheringDistance || gatheringDistance == -1)
                    {
                        debugCode = 600;
                        //if we are coming from a long ways away, fly a little faster so the Reapers are less spread out
                        int speedToUse = CalculateSpeed(ships, Context);
                        GameCommand speedCommand = GameCommand.Create ( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.CreateSpeedGroup_FireteamAttack],  GameCommandSource.AnythingElse);
                        for (int j = 0; j < ships.Count; j++)
                            speedCommand.RelatedEntityIDs.Add(ships[j].PrimaryKeyID);
                        int exoGroupSpeed = speedToUse;
                        speedCommand.RelatedBool = true;
                        speedCommand.RelatedIntegers.Add(exoGroupSpeed);
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, speedCommand, false );
                    }
                    debugCode = 700;
                    GameCommand command = GameCommand.Create ( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_UtilRaidSpecific], GameCommandSource.AnythingElse );
                    command.RelatedString = "Reapers_Planetary_Movement";
                    for ( int k = 0; k < ships.Count; k++ )
                        command.RelatedEntityIDs.Add( ships[k].PrimaryKeyID );
                    //determine whether we are actually going to the target, or just getting close so we are ready to strike
                    debugCode = 800;
                    if ( gatheringDistance == -1 )
                    {
                        debugCode = 900;
                        for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                            command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                    }
                    else
                    {
                        debugCode = 1000;
                        int hopsForwardToMove = pathCache.PathToReadOnly.Count - gatheringDistance;
                        if ( hopsForwardToMove >= pathCache.PathToReadOnly.Count || hopsForwardToMove < 0 )
                            throw new Exception("huh?");
                        debugCode = 1100;
                        for ( int k = 0; k < hopsForwardToMove; k++ )
                            command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                    }

                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                }
                #region Tracing
                if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion

            } catch(Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in Reapers AttackTargetPlanet debugCode " + debugCode + " " + e.ToString() , Verbosity.ShowAsError );
            }
        }
        /* Begin Fireteam Stuff */

        //prefer close ones that you can get to safely
        private readonly ArcenLessLinkedList<Fireteam> AvailableFireteams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "DarkSpireFactionDeepInfo-AvailableFireteams" );
        private void AssignUnitToFireteam(Faction faction, GameEntity_Squad entity, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( entity == null )
                return;

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DarkS-AssignUnitToFireteam-trace", 10f ) : null;

            AvailableFireteams.Clear();
            //bool debug = false;
            if ( BaseInfo.Teams.GetItemCount() == 0 )
            {
                Fireteam team = Fireteam.CreateNewWithIDFromList( BaseInfo.Teams );
                team.MyStrengthMultiplierForStrengthCalculation = FInt.FromParts(1, 00);
                team.EnemyStrengthMultiplierForStrengthCalculation = FInt.FromParts(1, 500);
                team.StrengthToBringOnline = faction.MinFireteamStrength + Context.RandomToUse.Next( 0, faction.MaxFireteamStrength - faction.MinFireteamStrength );
                BaseInfo.Teams.AddIfNotAlreadyIn( team );
            }
            int maxHops = 10;
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
            {
                if ( team.status == FireteamStatus.Disbanded ||
                        team.status == FireteamStatus.ReadyToAttack ||
                        team.status == FireteamStatus.Attacking )
                    continue; //if a fireteam is ready to fight, don't send more ships to that team
                if ( team.status == FireteamStatus.Staging && //if a fireteam is already "pretty strong", don't give it more
                     team.DeepInfo.TeamStrength > faction.MaxFireteamStrength )
                    continue;
                if ( team.DeepInfo.TeamStrength > faction.MaxFireteamStrength * 2 && team.DeepInfo.ShipsInFireteam.Count > 20 ) //if this is much stronger than usual, don't make it even stronger
                    continue;

                if ( team.DeepInfo.CurrentPlanet == null )
                {
                    //this is a brand new fireteam, so it's totally safe.
                    AvailableFireteams.AddIfNotAlreadyIn( team );
                    continue;
                }
                if ( AvailableFireteams.GetItemCount() > 4 ) //if we already have a lot of possible fireteams to use, don't keep looking
                    break;

                Int16 hops = 0;
                int dangerOfTeam = Fireteam.GetDangerOfPath( faction, Context, PathCacheData, entity.Planet, team.DeepInfo.CurrentPlanet, true, out hops );
                if ( dangerOfTeam < 40000 ) //let units wander through pretty dangerous spots (40 strength)
                {
                    if ( hops < maxHops )
                        AvailableFireteams.AddIfNotAlreadyIn( team );
                }
            }

            //Base algorithm: if any of our "safe" fireteams are "below strength" then just add to one of those fireteams.
            //If all our fireteams are "Strong Enough" then randomly choose to reinforce an existing one or create a new one
            //We require "safe teams" for the case where it's an octopus map with a Spawner cut off from the rest of the galaxy

            bool stopProcesssing = false;
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( AvailableFireteams ) )
            {
                if ( team.DeepInfo.TeamStrength < team.StrengthToBringOnline )
                {
                    team.DeepInfo.AddUnit( entity );
                    team.DeepInfo.IdentifyCurrentPlanet(); //just in case this unit is the first unit or something
                    entity.FireteamId = team.FireTeamID;
                    if ( tracing )
                        tracingBuffer.Add( "Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path A\n" );
                    stopProcesssing = true;
                    break;
                }
            }
            if ( stopProcesssing )
                return;

            int percentNewTeam = 40;
            if ( AvailableFireteams.GetItemCount() >= 4 )
                percentNewTeam = 0;
            if ( Context.RandomToUse.Next( 0, 100 ) < percentNewTeam || AvailableFireteams.GetItemCount() == 0 )
            {
                Fireteam team = Fireteam.CreateNewWithIDFromList( BaseInfo.Teams );
                team.MyStrengthMultiplierForStrengthCalculation = FInt.FromParts(1, 00);
                team.EnemyStrengthMultiplierForStrengthCalculation = FInt.FromParts(1, 500);
                team.StrengthToBringOnline = faction.MinFireteamStrength + Context.RandomToUse.Next( 0, faction.MaxFireteamStrength - faction.MinFireteamStrength );
                int numShipsForConcentratingEfforts = 5; //before we're too strong, best to concentrate our forces
                if ( BaseInfo.Teams.GetItemCount() < numShipsForConcentratingEfforts )
                    team.PercentBestTarget = 100;
                else if ( this.AttachedFaction.SpecialFactionData.FireteamPercentBestTarget > 0 )
                    team.PercentBestTarget = this.AttachedFaction.SpecialFactionData.FireteamPercentBestTarget;
                else
                    team.PercentBestTarget = 65;
                team.PercentDistanceBestTarget = 45; //marauders often get far-flung empires
                team.PreferredMaxDistance = 5;
                team.DeepInfo.AddUnit(entity);
                entity.FireteamId = team.FireTeamID;

                team.DeepInfo.IdentifyCurrentPlanet(); //in case this unit is the first unit
                BaseInfo.Teams.AddIfNotAlreadyIn( team );
                if ( tracing )
                    tracingBuffer.Add("Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path B: new fireteam. Percent New Fireateam: " + percentNewTeam + "\n");
                return;
            }

            {
                Fireteam team = AvailableFireteams.GetRandom( Context.RandomToUse );
                team.DeepInfo.AddUnit( entity );
                entity.FireteamId = team.FireTeamID;
                if ( tracing )
                    tracingBuffer.Add( "Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path C\n" );
                team.DeepInfo.IdentifyCurrentPlanet(); //just in case this unit is the first unit or something
            }
        }

        public override GameEntity_Squad GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( Planet CurrentPlanetForFireteam,  ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            int currentDanger = -1;
            GameEntity_Squad retreatPoint = null;
            List<SafeSquadWrapper> gateways = this.BaseInfo.Gateways.GetDisplayList();
            for ( int i = 0; i < gateways.Count; i++ )
            {
                GameEntity_Squad outpost = gateways[i].GetSquad();
                if ( outpost == null )
                    continue;
                Int16 hops = 0;
                int danger = Fireteam.GetDangerOfPath( this.AttachedFaction, Context, PathCacheData, CurrentPlanetForFireteam, outpost.Planet, true, out hops);
                if ( danger < currentDanger || currentDanger == -1)
                {
                    retreatPoint = outpost;
                    currentDanger = danger;
                }
                if ( danger == 0 )
                    break;
            }
            return retreatPoint;
        }

        public override void GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass( bool DefenseMode, Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, 
            List<FireteamTarget> PreferredTargets, List<FireteamTarget> FallbackTargets, object TeamObj )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DarkS-GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            FInt falloffForDistance = FInt.FromParts (0, 050);
            GetPreferredReaperTargets( PreferredTargets, AttachedFaction, Context);
            //We do a two-stage check here. First we cull the targets whose defenses are much stronger than usual.
            //then we sort targets by how hard it is to get there
            //sort targets by how hard it is to get there
            for ( int i = 0; i < PreferredTargets.Count; i++ )
            {
                FireteamTarget target = PreferredTargets[i];
                Int16 hops = 0;
                target.dangerOfTarget = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForFireteam, PreferredTargets[i].planet, true, out hops );
                PreferredTargets[i] = target;
            }

            cb_reapersFtCurrentPlanet = CurrentPlanetForFireteam;
            cb_reapersFaction = AttachedFaction;
            cb_reapersFtFalloff = falloffForDistance;
            PreferredTargets.Sort( static delegate ( FireteamTarget Left, FireteamTarget Right )
            {
                int lDistance = Left.planet.GetHopsTo( cb_reapersFtCurrentPlanet );
                int rDistance = Right.planet.GetHopsTo( cb_reapersFtCurrentPlanet );
                int lDanger = Left.dangerOfTarget;
                int rDanger = Right.dangerOfTarget;
                if ( Left.planet.GetControllingOrInfluencingFaction() == cb_reapersFaction )
                    lDanger /= 2;
                if ( Right.planet.GetControllingOrInfluencingFaction() == cb_reapersFaction )
                    rDanger /= 2;

                lDanger = lDanger + (cb_reapersFtFalloff * lDistance).IntValue;
                rDanger = rDanger + (cb_reapersFtFalloff * rDistance).IntValue;

                return lDanger.CompareTo( rDanger );
            } );
            for ( int i = PreferredTargets.Count - 1; i >= 0; i-- )
            {
                FireteamTarget target = PreferredTargets[i];
                if ( Fireteam.IsThisAWinningBattle( AttachedFaction, Context, target.planet, 4 ) || target.dangerOfTarget == -1 )
                    PreferredTargets.Remove( target );
            }

            for ( int i = 0; i < PreferredTargets.Count; i++ )
            {
                //discard things that are enough more dangerous than other things on the list
                if ( i == 0 ) continue;
                FireteamTarget target = PreferredTargets[i];
                FireteamTarget weakestTarget = PreferredTargets[0];
                FInt danger = (FInt)target.dangerOfTarget;
                FInt weakestDanger = (FInt)weakestTarget.dangerOfTarget;
                if ( target.planet.GetControllingOrInfluencingFaction() == AttachedFaction )
                    danger /= 2;
                if ( danger > weakestDanger * FInt.FromParts( 2, 000 ) )
                {
                    //this is way more dangerous than earlier targets on the list, so ignore it
                    PreferredTargets.RemoveRange( i, PreferredTargets.Count - i );
                }

            }
            GetFallbackReaperTargets( FallbackTargets, AttachedFaction, Context);
            //Currently we don't do fallback targets.
            if ( FallbackTargets != null || FallbackTargets.Count == 0 )
            {
                FallbackTargets.Sort( static delegate ( FireteamTarget Left, FireteamTarget Right )
                {
                    int lDifficulty = Left.dangerOfTarget;
                    int rDifficulty = Right.dangerOfTarget;
                    return lDifficulty.CompareTo( rDifficulty );
                } );
            }


            bool debug = true;
            if ( debug && tracing )
            {
                tracingBuffer.Add("Getting lurk/target Preferred Targets\n");
                for ( int i = 0; i < PreferredTargets.Count; i++ )
                    tracingBuffer.Add("\t").Add(PreferredTargets[i].GetPlanetName_Safe()).Add(" difficulty ").Add( PreferredTargets[i].dangerOfPath).Add(" \n");
                tracingBuffer.Add("Getting lurk/target Fallback Targets\n");
                for ( int i = 0; i < FallbackTargets.Count; i++ )
                    tracingBuffer.Add("\t").Add(FallbackTargets[i].GetPlanetName_Safe()).Add("\n");   
            }
        }
        public void GetPreferredReaperTargets( List<FireteamTarget> listToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context)
        {
            listToFill.Clear();
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null )
                    continue;
                if(! otherFaction.GetIsHostileTowards(faction) )
                    continue;
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    listToFill.Add(new FireteamTarget(entity));
                }
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.GrantsMinorFactionPlanetControl ) )
                {
                    //can go after minor factions
                    if ( Fireteam.IsThisAWinningBattle(faction, Context, entity.Planet, 2) )
                        continue;  //if we are already attacking and comfortably winning, don't bother sending more units
                    listToFill.Add(new FireteamTarget(entity));
                }
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.CommandStation ) )
                {
                    //I  guess we kill command stations now?
                    if ( entity.PlanetFaction.Faction.Type != FactionType.Player &&
                         this.BaseInfo.Gateways.GetDisplayList().Count < 0 )
                        continue;
                    if ( Fireteam.IsThisAWinningBattle(faction, Context, entity.Planet, 2) )
                        continue; //if we are already attacking and comfortably winning, don't bother sending more units
                    listToFill.Add( new FireteamTarget(entity.Planet) );
                }
            }
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( Fireteam.IsThisAWinningBattle( faction, Context, planet, 2 ) )
                    continue;

                if ( planet.GetControllingFactionType() == FactionType.NaturalObject )
                {
                    Faction influencingFaction = World_AIW2.Instance.GetFactionByIndex( planet.PrimaryInfluencingFaction );
                    if ( influencingFaction == null || !influencingFaction.GetIsFriendlyTowards( faction ) )
                        listToFill.Add( new FireteamTarget( planet ) );
                }
                if ( planet.GetControllingOrInfluencingFaction() == faction )
                {
                    //are there enemies attacking us?
                    EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( faction );
                    int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                    int myStrength = myFactionData[FactionStance.Self].TotalStrength;
                    if ( hostileStrength > myStrength / 2 )
                        listToFill.Add( new FireteamTarget( planet ) );
                }
            }
        }
        public void GetFallbackReaperTargets( List<FireteamTarget> listToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context)
        {
            listToFill.Clear();
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.GetControllingFactionType() == FactionType.NaturalObject )
                {
                    if ( Fireteam.IsThisAWinningBattle( faction, Context, planet, 2 ) )
                        continue;
                    EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( faction );
                    int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                    int myStrength = myFactionData[FactionStance.Self].TotalStrength;
                    if ( hostileStrength > 2000 )
                        listToFill.Add( new FireteamTarget( planet ) );
                }
            }
        }
        public override Planet GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass( Planet TargetPlanet, int TeamStrength, Planet CurrentPlanetForTeam, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            Planet bestPlanet = null;
            int dangerOfPathFromBestPlanet = -1;
            //int dangerOfPathToBestPlanet = 999999;
            int distanceFromBestPlanet = 9999;
            Int16 hopsFromBestPlanet = 9999;
            int unused = 0;
            //this logic partially cribbed from IndependentAIFleet.cs::Helper_DoTargetFindingSweep

            if ( TargetPlanet == null )
                throw new Exception ("No target planet set in get lurk planet?!");
            //int debugCode = 0;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DarkS-GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            bool preferUnwatchedPlanets = false;
            if ( tracing )
                tracingBuffer.Add("Getting a lurk planet. Target planet " + TargetPlanet.Name).Add(". ").Add(AttachedFaction.BaseInfo.Allegiance).Add("\n");
            foreach ( Planet.PlanetAtHopDistance _phd in TargetPlanet.PlanetsWithinXHops_NoFilters( -1 ) )
            {
                Planet planet = _phd.Planet;
                Int16 Distance = _phd.Hops;
                if ( planet == TargetPlanet )
                    continue;

                int planetDefensiveStrength = Fireteam.GetPlanetDefensiveStrength( planet, AttachedFaction, true, ref unused,
                                                                          FInt.Zero, FInt.Zero);
                if ( planet.GetControllingFaction().GetIsHostileTowards(AttachedFaction) && planetDefensiveStrength > TeamStrength / 10 )
                    continue;

                //Don't path through any particularly dangerous planets
                Int16 hops = 0;
                int totalDifficultyOfPathToLurkPlanet = Fireteam.GetDangerOfPath(AttachedFaction, Context, PathCacheData, CurrentPlanetForTeam, planet, true, out hops);
                if ( totalDifficultyOfPathToLurkPlanet >= TeamStrength * 5 ) //as long as they only outnumber us 5:1, let's go!
                    continue;

                int totalDifficultyOfPathToTarget = Fireteam.GetDangerOfPath(AttachedFaction, Context, PathCacheData, planet, TargetPlanet, true, out hops);
                if ( preferUnwatchedPlanets && planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    totalDifficultyOfPathToTarget *= 2; //penalized watched planets if desired

                if ( totalDifficultyOfPathToTarget < 0 )
                    totalDifficultyOfPathToTarget = 0; //pathing through allied planets is basically the same

                if ( tracing )
                    tracingBuffer.Add("\tConsidering " + planet.Name + " danger of path " + totalDifficultyOfPathToTarget + " distance " + Distance + " intel " + planet.IntelLevel ).Add("\n");

                if ( dangerOfPathFromBestPlanet > totalDifficultyOfPathToTarget ||
                     dangerOfPathFromBestPlanet == -1 )
                {
                    if ( tracing )
                        tracingBuffer.Add("\t" + planet.Name + " is now the lurk location; path A" ).Add("\n");
                    dangerOfPathFromBestPlanet = totalDifficultyOfPathToTarget;
                    distanceFromBestPlanet = Distance;
                    hopsFromBestPlanet = hops;
                    bestPlanet = planet;
                }

                if ( dangerOfPathFromBestPlanet == totalDifficultyOfPathToTarget &&
                     ( distanceFromBestPlanet > Distance ||
                       hopsFromBestPlanet > hops ) )
                {
                    if ( tracing )
                        tracingBuffer.Add("\t" + planet.Name + " is now the lurk location; path B" ).Add("\n");
                    dangerOfPathFromBestPlanet = totalDifficultyOfPathToTarget;
                    distanceFromBestPlanet = Distance;
                    hopsFromBestPlanet = hops;
                    bestPlanet = planet;
                }

                if ( hopsFromBestPlanet <= 3 && dangerOfPathFromBestPlanet <= TeamStrength / 10 )
                    break; //we found a good lurk within easy striking distance of the planet, so exit now
            }
            return bestPlanet;
        }

        /* End Fireteam Stuff */

    }
}
