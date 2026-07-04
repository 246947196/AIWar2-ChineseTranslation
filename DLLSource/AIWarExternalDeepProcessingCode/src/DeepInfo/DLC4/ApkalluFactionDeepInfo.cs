using Arcen.AIW2.Core;
using System;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class ApkalluFactionDeepInfo : ExternalFactionDeepInfoRoot, IExternalDeepInfo_Singleton
    {
        public ApkalluFactionBaseInfo BaseInfo;
        public static ApkalluFactionDeepInfo Instance = null;

        private readonly List<SafeSquadWrapper> OutpostsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 50, "ApkalluFactionDeepInfo-OutpostsLRP" );
        private readonly List<SafeSquadWrapper> MobileShipsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ApkalluFactionDeepInfo-MobileShipsLRP" );
        private readonly List<SafeSquadWrapper> LocustsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ApkalluFactionDeepInfo-LocustsLRP" );
        private readonly List<SafeSquadWrapper> PilgrimsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 100, "ApkalluFactionDeepInfo-PilgrimsLRP" );
        private readonly List<SafeSquadWrapper> RangersLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 100, "ApkalluFactionDeepInfo-RangersLRP" );
        private readonly List<Planet> PilgrimCandidatePlanetsLRP = List<Planet>.Create_WillNeverBeGCed( 50, "ApkalluFactionDeepInfo-PilgrimCandidatePlanetsLRP" );
        private readonly BolsteringManager bolsteringManager = new BolsteringManager();
        private readonly List<string> LamassuTagScratch = List<string>.Create_WillNeverBeGCed( 4, "ApkalluFactionDeepInfo-LamassuTagScratch" );
        private bool debugPilgrimResourceConversion = false;
        private int consecutiveMetalGrantCount = 0;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            BaseInfo = AttachedFaction.GetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            Instance = this;
        }

        protected override void Cleanup()
        {
            Instance = null;
            BaseInfo = null;
            OutpostsLRP.Clear();
            MobileShipsLRP.Clear();
            LocustsLRP.Clear();
            PilgrimsLRP.Clear();
            RangersLRP.Clear();
            PilgrimCandidatePlanetsLRP.Clear();
            LamassuTagScratch.Clear();
            TemenSpreadScratch.Clear();
            KingUnitPlanetsScratch.Clear();
        }


        protected override int MinimumSecondsBetweenLongRangePlannings => 5;

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            Instance = this;
            int debugCode = 0;
            try
            {
                debugCode = 100;
                if ( BaseInfo.Durus.GetDisplayList().Count == 0 )
                {
                    //We need to spawn a Ziggurat
                    if ( BaseInfo.ImmediateDuruSpawn )
                        BaseInfo.DuruSpawnTime = World_AIW2.Instance.GameSecond;
                    if ( (World_AIW2.Instance.GameSecond <= BaseInfo.DuruSpawnTime) )
                    {
                        //Either its time for us to spawn, or we requested an early spawn
                        SeedStartingDuru(Context);
                    }
                }
                debugCode = 200;
                HandleJournals( Context );
                debugCode = 300;
                DropMetalGenerators(Context);
                debugCode = 400;
                HandleOutpostProduction( Context );
                debugCode = 600;
                HandleOutguardGranters( Context );
                if ( BaseInfo.LamassusNeedingModuleRefresh.Count > 0 )
                {
                    foreach ( GameEntity_Squad lamassu in BaseInfo.Lamassus.DisplaySquads() )
                    {
                        if ( lamassu != null && BaseInfo.LamassusNeedingModuleRefresh.Contains( lamassu.PrimaryKeyID ) )
                            ApplyLamassuModules( lamassu.Planet, lamassu );
                    }
                    BaseInfo.LamassusNeedingModuleRefresh.Clear();
                }
                RecalculateApkalluCityBuildingContents( Context );
                bolsteringManager.HandleBolstering( "BuiltByDuru", BaseInfo.Durus.GetDisplayList(), BaseInfo.Flagships.GetDisplayList(), Context );
                debugCode = 600;
                HandleMajorDuruFlagships( Context );
                SpawnRangers( Context );
                debugCode = 650;
                HandleLesserPilgrimProduction( Context );
                debugCode = 675;
                HandlePilgrimsSim( Context );
                HandleBonusBreaches( Context );
                debugCode = 700;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "ApkalluFactionDeepInfo Stage3 debugCode " + debugCode + " " + e, Verbosity.DoNotShow );
            }
        }
        private void HandleMajorDuruFlagships( ArcenHostOnlySimContext Context )
        {
            if ( !ArcenNetworkAuthority.GetIsHostMode() )
                return;
            foreach ( GameEntity_Squad duru in BaseInfo.Durus.DisplaySquads() )
            {
                if ( duru == null )
                    continue;
                if ( !duru.TypeData.GetHasTag( "MajorDuru" ) )
                    continue;
                ApkalluPerUnitBaseInfo duruData = duru.TryGetExternalBaseInfoAs<ApkalluPerUnitBaseInfo>();
                if ( duruData == null )
                    duruData = duru.CreateExternalBaseInfo<ApkalluPerUnitBaseInfo>( "ApkalluPerUnitBaseInfo" );
                if ( duruData.FlagshipEntityPrimaryKeyID != -1 )
                    continue;
                GameEntity_Squad flagship = SpawnApkalluFlagship( ArcenPoint.ZeroZeroPoint, duru.Planet, "ApkalluFlagship_Uanna", AttachedFaction, Context, false );
                if ( flagship == null )
                    continue;
                Fleet flagshipFleet = flagship.FleetMembership.Fleet;
                flagshipFleet.NameRaw = "Duru Fleet " + duru.Planet.Name;
                flagshipFleet.FleetQualifier = "Apkallu";
                Fleet duruFleet = duru.FleetMembership.Fleet;
                if ( duruFleet.TryGetExternalBaseInfoAs<ApkalluCityFleetBaseInfo>() == null )
                {
                    ApkalluCityFleetBaseInfo cityInfo = duruFleet.CreateExternalBaseInfo<ApkalluCityFleetBaseInfo>( "ApkalluCityFleetBaseInfo" );
                    cityInfo.CanChangeBolsteredFleet = true;
                }
                duruFleet.CityBolstersFleetID = flagshipFleet.FleetID;
                duruData.FlagshipEntityPrimaryKeyID = flagship.PrimaryKeyID;
            }
        }

        private bool CanBuildRanger( GameEntity_Squad duru, int cap )
        {
            Dictionary<SafeSquadWrapper, int> rangers = BaseInfo.RangersPerDuru.GetDisplayDict();
            int count = 0;
            foreach ( KeyValuePair<SafeSquadWrapper, int> pair in rangers )
            {
                if ( pair.Key.GetSquad() == duru )
                { count = pair.Value; break; }
            }
            return count < cap;
        }

        private bool CanBuildDireRanger( GameEntity_Squad duru, int cap )
        {
            Dictionary<SafeSquadWrapper, int> rangers = BaseInfo.DireRangersPerDuru.GetDisplayDict();
            int count = 0;
            foreach ( KeyValuePair<SafeSquadWrapper, int> pair in rangers )
            {
                if ( pair.Key.GetSquad() == duru )
                { count = pair.Value; break; }
            }
            return count < cap;
        }

        private void SpawnRangers( ArcenHostOnlySimContext Context )
        {
            if ( BaseInfo.Durus.GetDisplayList().Count == 0 )
                return;
            if ( BaseInfo.Difficulty == null )
                return;
            foreach ( GameEntity_Squad duru in BaseInfo.RangerProducers.DisplaySquads() )
            {
                if ( duru == null )
                    continue;
                ApkalluPerUnitBaseInfo data = duru.TryGetExternalBaseInfoAs<ApkalluPerUnitBaseInfo>();
                if ( data == null )
                    continue;
                if ( duru.GetIsCrippled() )
                    continue;

                // Count outpost boosts on the same planet
                int rangerIncrease = 0;
                int direRangerIncrease = 0;
                foreach ( GameEntity_Squad outpost in BaseInfo.RangerOutposts.DisplaySquads() )
                {
                    if ( outpost.Planet != duru.Planet )
                        continue;
                    rangerIncrease += BaseInfo.Difficulty.OutpostForRangerCap;
                }
                foreach ( GameEntity_Squad outpost in BaseInfo.DireRangerOutposts.DisplaySquads() )
                {
                    if ( outpost.Planet != duru.Planet )
                        continue;
                    direRangerIncrease += BaseInfo.Difficulty.OutpostForDireRangerCap;
                }

                // Only accumulate metal if the outpost infrastructure is present
                if ( rangerIncrease > 0 )
                    data.RangerMetal += BaseInfo.Difficulty.RangerIncomePerSecond;
                if ( direRangerIncrease > 0 )
                    data.DireRangerMetal += BaseInfo.Difficulty.DireRangerIncomePerSecond;

                if ( data.RangerMetal <= 0 && data.DireRangerMetal <= 0 )
                    continue;

                // Determine caps from Duru type
                bool isMajor = duru.TypeData.GetHasTag( "MajorDuru" );
                data.RangerCap    = ( isMajor ? BaseInfo.Difficulty.MaxRangersPerMajorDuru    : BaseInfo.Difficulty.MaxRangersPerMinorDuru )    + rangerIncrease;
                data.DireRangerCap = ( isMajor ? BaseInfo.Difficulty.MaxDireRangersPerMajorDuru : BaseInfo.Difficulty.MaxDireRangersPerMinorDuru ) + direRangerIncrease;

                // Spawn Rangers
                if ( data.RangerMetal > 0 )
                {
                    GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ApkalluRanger" );
                    if ( typeData != null && data.RangerMetal >= typeData.CostForAIToPurchase )
                    {
                        data.RangerMetal -= typeData.CostForAIToPurchase;
                        if ( CanBuildRanger( duru, data.RangerCap ) )
                        {
                            ArcenPoint spawnLocation = duru.Planet.GetSafePlacementPoint_AroundEntity( Context, typeData, duru, FInt.FromParts( 0, 005 ), FInt.FromParts( 0, 010 ) );
                            GameEntity_Squad ranger = AttachedFaction.SpawnNewUnit_ReturnNullIfMPClient(
                                Context, duru.Planet, spawnLocation, typeData, duru.CurrentMarkLevel,
                                AttachedFaction.LooseFleet, 0, EntityBehaviorType.Attacker_Full, -1, null, "Apkallu-Ranger" );
                            if ( ranger != null )
                            {
                                ApkalluPerUnitBaseInfo rangerData = ranger.CreateExternalBaseInfo<ApkalluPerUnitBaseInfo>( "ApkalluPerUnitBaseInfo" );
                                rangerData.HomeDuru = LazyLoadSquadWrapper.Create( duru );
                            }
                        }
                    }
                }

                // Spawn DireRangers
                if ( data.DireRangerMetal > 0 )
                {
                    GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ApkalluDireRanger" );
                    if ( typeData != null && data.DireRangerMetal >= typeData.CostForAIToPurchase )
                    {
                        data.DireRangerMetal -= typeData.CostForAIToPurchase;
                        if ( CanBuildDireRanger( duru, data.DireRangerCap ) )
                        {
                            ArcenPoint spawnLocation = duru.Planet.GetSafePlacementPoint_AroundEntity( Context, typeData, duru, FInt.FromParts( 0, 030 ), FInt.FromParts( 0, 050 ) );
                            GameEntity_Squad ranger = AttachedFaction.SpawnNewUnit_ReturnNullIfMPClient(
                                Context, duru.Planet, spawnLocation, typeData, duru.CurrentMarkLevel,
                                AttachedFaction.LooseFleet, 0, EntityBehaviorType.Attacker_Full, -1, null, "Apkallu-DireRanger" );
                            if ( ranger != null )
                            {
                                ApkalluPerUnitBaseInfo rangerData = ranger.CreateExternalBaseInfo<ApkalluPerUnitBaseInfo>( "ApkalluPerUnitBaseInfo" );
                                rangerData.HomeDuru = LazyLoadSquadWrapper.Create( duru );
                            }
                        }
                    }
                }

            }
        }
        private void HandleBonusBreaches( ArcenHostOnlySimContext Context )
        {
            if ( !BaseInfo.UnlockAllBreaches )
                return;
            if ( MalwareBreachTable.Instance == null )
                return;
            ProtectedList<MalwareBreach> allBreaches = MalwareBreachTable.Instance.Rows;
            for ( int i = 0; i < allBreaches.Count; i++ )
            {
                MalwareBreach breach = allBreaches[i];
                if ( breach == null )
                    continue;
                bool alreadyGranted = false;
                for ( int k = 0; k < BaseInfo.CompletedBreaches.Count; k++ )
                {
                    if ( BaseInfo.CompletedBreaches[k] == breach )
                    { alreadyGranted = true; break; }
                }
                if ( alreadyGranted )
                    continue;
                BaseInfo.CompletedBreaches.Add( breach );
                if ( breach.UnlockFactionResource != ApkalluFactionResource.None )
                    BaseInfo.UnlockResource( breach.UnlockFactionResource );
                for ( int j = 0; j < breach.UnlockDuruStructures.Count; j++ )
                    BaseInfo.UnlockedDuruStructures.AddIfNotAlreadyIn( breach.UnlockDuruStructures[j] );
                if ( breach.UnlockZigguratStructure != null )
                    BaseInfo.UnlockedZigguratStructures.AddIfNotAlreadyIn( breach.UnlockZigguratStructure );
                if ( breach.UnlockTemenStructure != null )
                    BaseInfo.UnlockedTemenStructures.AddIfNotAlreadyIn( breach.UnlockTemenStructure );
            }
        }
        int pilgrimDistance = 500;
        private void HandlePilgrimsSim( ArcenHostOnlySimContext Context )
        {
            //Handle the behaviour of a pilgrim on a given planet
            //The LRP code handles the movement
            foreach ( GameEntity_Squad pilgrim in BaseInfo.Pilgrims.DisplaySquads() )
            {
                if ( pilgrim == null )
                    continue;
                ApkalluPerUnitBaseInfo data = pilgrim.TryGetExternalBaseInfoAs<ApkalluPerUnitBaseInfo>();
                if ( data == null )
                    continue;

                Planet planet = pilgrim.Planet;
                bool isLesserPilgrim = pilgrim.TypeData.GetHasTag( "ApkalluLesserPilgrim" );

                // Check for Temens and Ziggurats
                GameEntity_Squad temen = null;
                foreach ( GameEntity_Squad temenEntity in planet.Squads( "ApkalluTemen" ) )
                {
                    if ( temenEntity.PlanetFaction.Faction != AttachedFaction )
                        continue;
                    temen = temenEntity;
                    break;
                }
                GameEntity_Squad ziggurat = BaseInfo.GetZigguratForPlanet( planet );
                if ( temen == null && ziggurat == null )
                    continue;

                // Skip planets already visited
                bool planetVisited = false;
                for ( int i = 0; i < data.PlanetsVisited.Count; i++ )
                {
                    if ( data.PlanetsVisited[i] == planet )
                    {
                        planetVisited = true;
                        break;
                    }
                }

                // Check proximity to a Temen — within 500 units counts as a visit
                if ( temen != null &&
                     pilgrim.WorldLocation.GetDistanceTo( temen.WorldLocation, false ) <= pilgrimDistance &&
                     !planetVisited)
                {
                    data.PlanetsVisitedIdx.Add( (Int16)planet.Index );
                    data.PlanetsVisited.Add( planet );
                    if ( isLesserPilgrim )
                        data.ResourcePoints += 20; //a lot less resources
                    else
                        data.ResourcePoints += 200;
                    if ( debugPilgrimResourceConversion )
                        ArcenDebugging.LogSingleLine( "[PilgrimDebug] " + pilgrim.TypeData.DisplayName + " visited Temen on " + planet.Name + ". RP +" + ( isLesserPilgrim ? 20 : 200 ) + " → total " + data.ResourcePoints + " (" + data.PlanetsVisited.Count + " planets visited)", Verbosity.DoNotShow );
                }
                if ( ziggurat != null &&
                     pilgrim.WorldLocation.GetDistanceTo( ziggurat.WorldLocation, false ) <= pilgrimDistance &&
                     data.ResourcePoints > 0 ) //don't bother if no resource points
                {
                    if ( isLesserPilgrim )
                    {
                        bool lrpForceNonMetal = consecutiveMetalGrantCount >= 3;
                        PickLesserPilgrimResource( data.PlanetsVisited, planet.Name, data.ResourcePoints, Context, lrpForceNonMetal,
                            out string chosenLRP, out FInt chosenConvLRP );
                        if ( data.ResourcePoints > 0 )
                        {
                            int lrpChatAmount = ( data.ResourcePoints * chosenConvLRP ).IntValue;
                            GrantPilgrimResource( chosenLRP, data.ResourcePoints, chosenConvLRP );
                            AttachedFaction.StoredFactionResourceOne += 1;
                            World_AIW2.Instance.QueueChatMessageOrCommand( "Lesser Pilgrim: " + GetPilgrimResourceIcon( chosenLRP, AttachedFaction ) + lrpChatAmount.ToString( "#,##0" ) + " +" + ( AttachedFaction.Resource1TextColorAndIcon.Length > 0 ? AttachedFaction.Resource1TextColorAndIcon : World_AIW2.Instance.Resource1TextColorAndIcon ) + "1", ChatType.ShowLocallyOnly, string.Empty, null, 15f );
                        }
                        consecutiveMetalGrantCount = ( chosenLRP == "Metal" ) ? consecutiveMetalGrantCount + 1 : 0;

                        // Count living lesser pilgrims for this producer to decide loop vs despawn
                        GameEntity_Squad producer = data.HomeDuru.GetSquad();
                        int lesserCount = 0;
                        if ( producer != null )
                        {
                            foreach ( GameEntity_Squad other in BaseInfo.Pilgrims.DisplaySquads() )
                            {
                                if ( other == pilgrim || !other.TypeData.GetHasTag( "ApkalluLesserPilgrim" ) )
                                    continue;
                                ApkalluPerUnitBaseInfo od = other.TryGetExternalBaseInfoAs<ApkalluPerUnitBaseInfo>();
                                if ( od != null && od.HomeDuru.GetSquad() == producer )
                                    lesserCount++;
                            }
                        }
                        int lesserPilgrimCap = BaseInfo.Temens.GetDisplayList().Count - 1;
                        if ( lesserCount < lesserPilgrimCap )
                        {
                            // Loop — reset journey state and wander again
                            data.PlanetsVisited.Clear();
                            data.PlanetsVisitedIdx.Clear();
                            data.ResourcePoints = 0;
                        }
                        else
                        {
                            // Producer is at cap; despawn this one to free a slot
                            pilgrim.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                        }
                        continue;
                    }

                    bool forceNonMetal = consecutiveMetalGrantCount >= 3;
                    CalculatePilgrimGrant( pilgrim, data, planet.Name, Context, forceNonMetal,
                        out string chosenResource, out int primaryPoints, out FInt chosenConversion,
                        out string secondaryResource, out int secondaryPoints, out FInt secondaryConversion,
                        out string guaranteedResource, out int guaranteedPoints, out FInt guaranteedConversion );

                    int primaryGrantAmount = ( primaryPoints * chosenConversion ).IntValue;
                    GrantPilgrimResource( chosenResource, primaryPoints, chosenConversion );
                    string pilgrimChatMsg = "Pilgrim: " + GetPilgrimResourceIcon( chosenResource, AttachedFaction ) + primaryGrantAmount.ToString( "#,##0" );
                    if ( secondaryPoints > 0 && !string.IsNullOrEmpty( secondaryResource ) )
                    {
                        int secondaryGrantAmount = ( secondaryPoints * secondaryConversion ).IntValue;
                        GrantPilgrimResource( secondaryResource, secondaryPoints, secondaryConversion );
                        pilgrimChatMsg += " + " + GetPilgrimResourceIcon( secondaryResource, AttachedFaction ) + secondaryGrantAmount.ToString( "#,##0" );
                    }
                    if ( guaranteedPoints > 0 )
                    {
                        int guaranteedGrantAmount = ( guaranteedPoints * guaranteedConversion ).IntValue;
                        GrantPilgrimResource( guaranteedResource, guaranteedPoints, guaranteedConversion );
                        pilgrimChatMsg += " + " + GetPilgrimResourceIcon( guaranteedResource, AttachedFaction ) + guaranteedGrantAmount.ToString( "#,##0" );
                    }
                    World_AIW2.Instance.QueueChatMessageOrCommand( pilgrimChatMsg, ChatType.ShowLocallyOnly, string.Empty, null, 15f );

                    for ( int fi = 0; fi < World_AIW2.Instance.Factions.Count; fi++ )
                    {
                        Faction otherFaction = World_AIW2.Instance.Factions[fi];
                        if ( otherFaction.Type != FactionType.Player )
                            continue;
                        if ( !otherFaction.GetIsFriendlyTowards( AttachedFaction ) )
                            continue;
                        PlayerTypeData ptd = otherFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
                        bool usesMetal = ptd == null || ptd.UsesMetal;
                        if ( usesMetal )
                            otherFaction.StoredMetal += 10 * 1000;
                        otherFaction.StoredScience += 2;
                    }

                    consecutiveMetalGrantCount = ( chosenResource == "Metal" ) ? consecutiveMetalGrantCount + 1 : 0;
                    pilgrim.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                    continue;
                }

            }
        }

        // Degree-2 Bernstein curve: Metal → Hacking → Science, weighted by journey length.
        private void PickLesserPilgrimResource( List<Planet> planetsVisited, string planetName, int resourcePoints,
            ArcenHostOnlySimContext Context, bool forceNonMetal, out string resource, out FInt conversion )
        {
            int totalHops = 0;
            for ( int i = 1; i < planetsVisited.Count; i++ )
                totalHops += planetsVisited[i - 1].GetHopsTo( planetsVisited[i] );
            float t = Math.Min( 1f, totalHops / (World_AIW2.Instance.CurrentGalaxy.GetTotalPlanetCount() * BaseInfo.Difficulty.PilgrimJourneyQualityScale.ToFloat()) );
            float q = 1f - t;
            float metalWt   = q * q;
            float hackingWt = 2f * t * q;
            float scienceWt = t * t;

            if ( forceNonMetal )
            {
                float totalNonMetal = hackingWt + scienceWt;
                if ( totalNonMetal > 0f )
                {
                    hackingWt /= totalNonMetal;
                    scienceWt /= totalNonMetal;
                }
                else
                {
                    hackingWt = 0.5f;
                    scienceWt = 0.5f;
                }
                metalWt = 0f;
            }

            FInt metalConv   = BaseInfo.Difficulty.LesserPilgrimMetalConversion;
            FInt hackingConv = BaseInfo.Difficulty.LesserPilgrimHackingConversion;
            FInt scienceConv = BaseInfo.Difficulty.LesserPilgrimScienceConversion;

            float roll = Context.RandomToUse.Next( 0, 10000 ) / 10000f;
            float cum = metalWt;
            if ( roll < cum )
            { resource = "Metal";   conversion = metalConv; }
            else
            {
                cum += hackingWt;
                if ( roll < cum )
                { resource = "Hacking"; conversion = hackingConv; }
                else
                { resource = "Science"; conversion = scienceConv; }
            }

            if ( debugPilgrimResourceConversion )
                ArcenDebugging.LogSingleLine( "[PilgrimDebug] LesserPilgrim at Ziggurat on " + planetName + ". Hops=" + totalHops + " t=" + t.ToString( "F3" ) + " weights: Metal=" + metalWt.ToString( "F3" ) + " Hacking=" + hackingWt.ToString( "F3" ) + " Science=" + scienceWt.ToString( "F3" ) + ". Roll=" + roll.ToString( "F3" ) + " → " + resource + ". RP=" + resourcePoints + " grant=" + ( resourcePoints * conversion ).IntValue, Verbosity.DoNotShow );
        }

        // Bernstein basis weights by tier, resource roll, and top-tier secondary split.
        // Tier 1 (degree 3): Metal → Hacking → Science → R1
        // Tier 2 (degree 4): Metal → Hacking → Science → R1 → R2
        // Tier 3 (degree 5): Metal → Hacking → Science → R1 → R2 → R3
        private void CalculatePilgrimGrant( GameEntity_Squad pilgrim, ApkalluPerUnitBaseInfo data, string planetName,
            ArcenHostOnlySimContext Context, bool forceNonMetal,
            out string primaryResource, out int primaryPoints, out FInt primaryConversion,
            out string secondaryResource, out int secondaryPoints, out FInt secondaryConversion,
            out string guaranteedResource, out int guaranteedPoints, out FInt guaranteedConversion )
        {
            int totalHops = 0;
            for ( int i = 1; i < data.PlanetsVisited.Count; i++ )
                totalHops += data.PlanetsVisited[i - 1].GetHopsTo( data.PlanetsVisited[i] );
            float t = Math.Min( 1f, totalHops / (World_AIW2.Instance.CurrentGalaxy.GetTotalPlanetCount() * BaseInfo.Difficulty.PilgrimJourneyQualityScale.ToFloat()) );

            bool isTierTwo   = pilgrim.TypeData.GetHasTag( "ApkalluPilgrimTierTwo" );
            bool isTierThree = pilgrim.TypeData.GetHasTag( "ApkalluPilgrimTierThree" );
            if ( debugPilgrimResourceConversion )
                ArcenDebugging.LogSingleLine( "[PilgrimDebug] Pilgrim (" + ( isTierThree ? "T3" : isTierTwo ? "T2" : "T1" ) + ") cashing in at Ziggurat on " + planetName + ". RP=" + data.ResourcePoints + " visits=" + data.PlanetsVisited.Count + " hops=" + totalHops + " t=" + t.ToString( "F3" ), Verbosity.DoNotShow );

            float metalWeight, hackingWeight, scienceWeight, r1Weight, r2Weight, r3Weight;
            r2Weight = 0f;
            r3Weight = 0f;
            if ( isTierThree )
            {
                float q = 1f - t;
                metalWeight   =        q * q * q * q * q;
                hackingWeight = 5f * t * q * q * q * q;
                scienceWeight = 10f * t * t * q * q * q;
                r1Weight      = 10f * t * t * t * q * q;
                r2Weight      =  5f * t * t * t * t * q;
                r3Weight      =       t * t * t * t * t;
            }
            else if ( isTierTwo )
            {
                float q = 1f - t;
                metalWeight   =       q * q * q * q;
                hackingWeight = 4f * t * q * q * q;
                scienceWeight = 6f * t * t * q * q;
                r1Weight      = 4f * t * t * t * q;
                r2Weight      =      t * t * t * t;
            }
            else
            {
                float q = 1f - t;
                metalWeight   = q * q * q;
                hackingWeight = 3f * t * q * q;
                scienceWeight = 3f * t * t * q;
                r1Weight      = t * t * t;
            }

            if ( forceNonMetal )
            {
                float totalNonMetal = hackingWeight + scienceWeight + r1Weight + r2Weight + r3Weight;
                if ( totalNonMetal > 0f )
                {
                    float inv = 1f / totalNonMetal;
                    hackingWeight *= inv;
                    scienceWeight *= inv;
                    r1Weight      *= inv;
                    r2Weight      *= inv;
                    r3Weight      *= inv;
                }
                else
                {
                    hackingWeight = 1f;
                }
                metalWeight = 0f;
            }

            if ( debugPilgrimResourceConversion )
            {
                string wStr = "Metal=" + metalWeight.ToString( "F3" ) + " Hacking=" + hackingWeight.ToString( "F3" ) + " Science=" + scienceWeight.ToString( "F3" ) + " R1=" + r1Weight.ToString( "F3" );
                if ( isTierTwo || isTierThree ) wStr += " R2=" + r2Weight.ToString( "F3" );
                if ( isTierThree ) wStr += " R3=" + r3Weight.ToString( "F3" );
                ArcenDebugging.LogSingleLine( "[PilgrimDebug] Weights: " + wStr, Verbosity.DoNotShow );
            }

            FInt metalConversion   = BaseInfo.Difficulty.PilgrimMetalConversion;
            FInt hackingConversion = BaseInfo.Difficulty.PilgrimHackingConversion;
            FInt scienceConversion = BaseInfo.Difficulty.PilgrimScienceConversion;
            FInt r1Conversion      = BaseInfo.Difficulty.PilgrimResourceOneConversion;
            FInt r2Conversion      = BaseInfo.Difficulty.PilgrimResourceTwoConversion;
            FInt r3Conversion      = BaseInfo.Difficulty.PilgrimResourceThreeConversion;

            // Guaranteed fraction always goes to the highest special resource this tier can grant.
            guaranteedPoints = (int)( data.ResourcePoints * BaseInfo.Difficulty.PilgrimGuaranteedSpecialFraction );
            int rolledPoints = data.ResourcePoints - guaranteedPoints;
            if ( isTierThree )
            { guaranteedResource = "ResourceThree"; guaranteedConversion = r3Conversion; }
            else if ( isTierTwo )
            { guaranteedResource = "ResourceTwo";   guaranteedConversion = r2Conversion; }
            else
            { guaranteedResource = "ResourceOne";   guaranteedConversion = r1Conversion; }

            if ( debugPilgrimResourceConversion )
                ArcenDebugging.LogSingleLine( "[PilgrimDebug] Guaranteed: " + guaranteedPoints + " RP → " + guaranteedResource + " (grant " + ( guaranteedPoints * guaranteedConversion ).IntValue + "). Rolled RP=" + rolledPoints, Verbosity.DoNotShow );

            float r = Context.RandomToUse.Next( 0, 10000 ) / 10000f;
            FInt chosenConversion = r1Conversion;
            float cumulative = metalWeight;
            if ( r < cumulative )
            { primaryResource = "Metal";        chosenConversion = metalConversion; }
            else
            {
                cumulative += hackingWeight;
                if ( r < cumulative )
                { primaryResource = "Hacking";      chosenConversion = hackingConversion; }
                else
                {
                    cumulative += scienceWeight;
                    if ( r < cumulative )
                    { primaryResource = "Science";      chosenConversion = scienceConversion; }
                    else
                    {
                        cumulative += r1Weight;
                        if ( r < cumulative )
                        { primaryResource = "ResourceOne";  chosenConversion = r1Conversion; }
                        else
                        {
                            cumulative += r2Weight;
                            if ( r < cumulative )
                            { primaryResource = "ResourceTwo";  chosenConversion = r2Conversion; }
                            else
                            { primaryResource = "ResourceThree"; chosenConversion = r3Conversion; }
                        }
                    }
                }
            }
            primaryConversion = chosenConversion;

            if ( debugPilgrimResourceConversion )
                ArcenDebugging.LogSingleLine( "[PilgrimDebug] Roll=" + r.ToString( "F4" ) + " → " + primaryResource, Verbosity.DoNotShow );

            // For top-tier outcomes, split 90% primary / 10% secondary going to whatever the player needs most.
            bool isTopTier = ( isTierThree && primaryResource == "ResourceThree" ) ||
                             ( isTierTwo   && primaryResource == "ResourceTwo" );
            primaryPoints   = rolledPoints;
            secondaryPoints = 0;
            secondaryResource  = string.Empty;
            secondaryConversion = r1Conversion;
            if ( isTopTier )
            {
                secondaryPoints = rolledPoints / 10;
                primaryPoints   = rolledPoints - secondaryPoints;

                if ( AttachedFaction.StoredMetal < 50000 )
                { secondaryResource = "Metal";       secondaryConversion = metalConversion; }
                else if ( AttachedFaction.StoredScience < FInt.FromParts( 500, 000 ) )
                { secondaryResource = "Science";     secondaryConversion = scienceConversion; }
                else if ( AttachedFaction.StoredHacking < FInt.FromParts( 5, 000 ) )
                { secondaryResource = "Hacking";     secondaryConversion = hackingConversion; }
                else if ( AttachedFaction.StoredFactionResourceOne < FInt.FromParts( 10, 000 ) )
                { secondaryResource = "ResourceOne"; secondaryConversion = r1Conversion; }
                else if ( isTierThree && AttachedFaction.StoredFactionResourceTwo < FInt.FromParts( 10, 000 ) )
                { secondaryResource = "ResourceTwo"; secondaryConversion = r2Conversion; }
                else
                {
                    int lowerCount = isTierThree ? 5 : 4;
                    switch ( Context.RandomToUse.Next( 0, lowerCount ) )
                    {
                        case 0:  secondaryResource = "Metal";       secondaryConversion = metalConversion;   break;
                        case 1:  secondaryResource = "Hacking";     secondaryConversion = hackingConversion; break;
                        case 2:  secondaryResource = "Science";     secondaryConversion = scienceConversion; break;
                        case 3:  secondaryResource = "ResourceOne"; secondaryConversion = r1Conversion;      break;
                        default: secondaryResource = "ResourceTwo"; secondaryConversion = r2Conversion;      break;
                    }
                }
            }

            if ( debugPilgrimResourceConversion )
            {
                if ( isTopTier )
                    ArcenDebugging.LogSingleLine( "[PilgrimDebug] Top-tier split: " + primaryPoints + " RP → " + primaryResource + " (grant " + ( primaryPoints * primaryConversion ).IntValue + ") + " + secondaryPoints + " RP → " + secondaryResource + " (grant " + ( secondaryPoints * secondaryConversion ).IntValue + ")", Verbosity.DoNotShow );
                else
                    ArcenDebugging.LogSingleLine( "[PilgrimDebug] Grant: " + rolledPoints + " RP → " + primaryResource + " (" + ( rolledPoints * primaryConversion ).IntValue + ")", Verbosity.DoNotShow );
            }
        }

        private void GrantPilgrimResource( string resource, int points, FInt conversion )
        {
            int amount = ( points * conversion ).IntValue;
            switch ( resource )
            {
                case "Metal":        AttachedFaction.StoredMetal                += amount; break;
                case "Science":      AttachedFaction.StoredScience              += amount; break;
                case "Hacking":      AttachedFaction.StoredHacking              += amount; break;
                case "ResourceOne":  AttachedFaction.StoredFactionResourceOne   += amount; break;
                case "ResourceTwo":  AttachedFaction.StoredFactionResourceTwo   += amount; break;
                case "ResourceThree":AttachedFaction.StoredFactionResourceThree += amount; break;
            }
        }

        private static string GetPilgrimResourceIcon( string resource, Faction faction )
        {
            switch ( resource )
            {
                case "Metal":         return ArcenExternalUIUtilities.MetalTextColorAndIcon;
                case "Hacking":       return ArcenExternalUIUtilities.HackingTextColorAndIcon;
                case "Science":       return ArcenExternalUIUtilities.ScienceTextColorAndIcon;
                case "ResourceOne":   return faction != null && faction.Resource1TextColorAndIcon.Length > 0 ? faction.Resource1TextColorAndIcon : World_AIW2.Instance.Resource1TextColorAndIcon;
                case "ResourceTwo":   return World_AIW2.Instance.Resource2TextColorAndIcon;
                case "ResourceThree": return World_AIW2.Instance.Resource3TextColorAndIcon;
                default:              return resource + " ";
            }
        }

        private void HandleJournals( ArcenHostOnlySimContext Context )
        {
            int debugcode = 0;
            try{

                debugcode = 100;
                if ( BaseInfo.Flagships.GetDisplayList().Count > 0 )
                {
                    debugcode = 200;
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Apkallu_Lore", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    debugcode = 300;
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Apkallu_Flagship_Arrival", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    debugcode = 400;
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Apkallu_Economy", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Apkallu_Ziggurat", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                }
            }catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in handleJournals debugCode " + debugcode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }

        protected virtual void DropMetalGenerators(ArcenHostOnlySimContext Context)
        {
            //at game start time, the apkallu may get the metal generators on its home planet
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
        protected bool DuruSeeded = false;
        protected virtual void SeedStartingDuru( ArcenHostOnlySimContext Context )
        {
            Planet start = FactionUtilityMethods.Instance.findHumanKing( true );
            if ( start == null )
                return;
            DuruSeeded = true;
            GameEntity_Squad duru = SpawnApkalluDuru( ArcenPoint.ZeroZeroPoint, start, "MajorDuru", AttachedFaction, Context, false );
            GameEntity_Squad startingFlagship = SpawnApkalluFlagship( ArcenPoint.ZeroZeroPoint, start, "ApkalluStartingFlagship", AttachedFaction, Context, false );
            if ( duru != null && startingFlagship != null )
            {
                ApkalluPerUnitBaseInfo duruData = duru.CreateExternalBaseInfo<ApkalluPerUnitBaseInfo>( "ApkalluPerUnitBaseInfo" );
                duruData.FlagshipEntityPrimaryKeyID = startingFlagship.PrimaryKeyID;
                startingFlagship.FleetMembership.Fleet.NameRaw = "Duru Fleet " + start.Name;
                startingFlagship.FleetMembership.Fleet.FleetQualifier = "Apkallu";
                duru.FleetMembership.Fleet.CityBolstersFleetID = startingFlagship.FleetMembership.Fleet.FleetID;
            }
            if ( duru != null )
            {
                GameEntityTypeData structureData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "StartingDuruStructure" );
                if ( structureData != null )
                {
                    ArcenPoint structureLocation = start.GetSafePlacementPoint_AroundEntity( Context, structureData, duru, FInt.FromParts( 0, 005 ), FInt.FromParts( 0, 010 ) );
                    PlanetFaction pFaction = start.GetPlanetFactionForFaction( AttachedFaction );
                    GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, structureData, 1, duru.FleetMembership.Fleet, 0, structureLocation, Context, "Apkallu-StartingDuruStructure" );
                }
            }
        }
        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            // StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.AI, SpecialEntityType.None, "AISpireCitadel", SeedingType.CapturableWeightsAndMax,
//                 1, MapGenCountPerPlanet.One, MapGenSeedStyle.BigGood, 4, 9, 2, 99, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal, null, -1 );
            //            int nearbyGolems = 1;
                

//            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, null, FactionType.NaturalObject, SpecialEntityType.None, "ApkalluTemen", SeedingType.CapturableWeightsAndMax,
  //               nearbyGolems, MapGenCountPerPlanet.One, MapGenSeedStyle.SmallGood, 2, 4, 2, -1, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ExpandPlayerMaxOnly, null, -1 );
 
             //Set our economy
            AttachedFaction.StoredScience = FInt.FromParts( 7500, 000 );
            AttachedFaction.StoredHacking = FInt.FromParts( 40, 000 );

            //For the Temen
            //One really close to the player
            Faction naturalFaction = World_AIW2.Instance.GetNaturalObjectFactionNeverNull();
            if ( naturalFaction == null )
                return;
            var argsClose = new SeedArgs()
            {
                FactionOrNull = naturalFaction,
                TagOrEmpty = "ApkalluTemen",
                Count = 1,
                CountPer = MapGenCountPerPlanet.One,
                SeedStyle = MapGenSeedStyle.NoChecks,
                MinDistanceFromHumanHomeworld = 1,
                MaxDistanceFromHumanHomeworld = 3,
                SeedingZone = PlanetSeedingZone.MostAnywhere,
                ExpansionStyle = SeedingExpansionType.ComplicatedOriginal,
                DisableDistanceRescaling = true,
            };
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, argsClose );

            //Two reasonably close to the player
            argsClose = new SeedArgs()
            {
                FactionOrNull = naturalFaction,
                TagOrEmpty = "ApkalluTemen",
                Count = 2,
                CountPer = MapGenCountPerPlanet.One,
                SeedStyle = MapGenSeedStyle.NoChecks,
                MinDistanceFromHumanHomeworld = 2,
                MaxDistanceFromHumanHomeworld = 5,
                SeedingZone = PlanetSeedingZone.MostAnywhere,
                ExpansionStyle = SeedingExpansionType.ComplicatedOriginal,
                DisableDistanceRescaling = true,
            };
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, argsClose );
            // 5 more Temen spread across the map with no real restrictions
            var argsSpread = new SeedArgs()
            {
                FactionOrNull = naturalFaction,
                TagOrEmpty = "ApkalluTemen",
                Count = 5,
                CountPer = MapGenCountPerPlanet.One,
                SeedStyle = MapGenSeedStyle.NoChecks,
                MinDistanceFromHumanHomeworld = 0,
                MaxDistanceFromHumanHomeworld = 99,
                SeedingZone = PlanetSeedingZone.MostAnywhere,
                ExpansionStyle = SeedingExpansionType.ComplicatedOriginal,
                DisableDistanceRescaling = true,
            };
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, argsSpread );

            // 2 Temen near an AI homeworld — deep in enemy territory
            var argsAI = new SeedArgs()
            {
                FactionOrNull = naturalFaction,
                TagOrEmpty = "ApkalluTemen",
                Count = 2,
                CountPer = MapGenCountPerPlanet.One,
                SeedStyle = MapGenSeedStyle.NoChecks,
                MinDistanceFromAIHomeworld = 3,
                MaxDistanceFromAIHomeworld = 6,
                SeedingZone = PlanetSeedingZone.MostAnywhere,
                ExpansionStyle = SeedingExpansionType.ComplicatedOriginal,
                DisableDistanceRescaling = true,
            };
            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, argsAI );

            EnsureTemensAreSpread( Context, galaxy );

            // R1/R2/R3 start at 0 and are unlocked via Corrupted Ziggurat capture breaches (C1-C3)

        }

        private readonly List<GameEntity_Squad> TemenSpreadScratch = List<GameEntity_Squad>.Create_WillNeverBeGCed( 20, "ApkalluFactionDeepInfo-TemenSpreadScratch" );
        private readonly List<Planet> KingUnitPlanetsScratch = List<Planet>.Create_WillNeverBeGCed( 8, "ApkalluFactionDeepInfo-KingUnitPlanetsScratch" );
        private const int MinHopsBetweenTemens = 2;

        private void EnsureTemensAreSpread( ArcenHostOnlySimContext Context, Galaxy galaxy )
        {
            Faction naturalFaction = World_AIW2.Instance.GetNaturalObjectFactionNeverNull();
            if ( naturalFaction == null )
                return;

            // Collect all placed Temens
            TemenSpreadScratch.Clear();
            foreach ( GameEntity_Squad temen in naturalFaction.Squads( "ApkalluTemen" ) )
            {
                TemenSpreadScratch.Add( temen );
            }

            // Pre-collect planets that host king units so we never relocate a Temen there
            KingUnitPlanetsScratch.Clear();
            foreach ( GameEntity_Squad king in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                if ( king.Planet != null && !KingUnitPlanetsScratch.Contains( king.Planet ) )
                    KingUnitPlanetsScratch.Add( king.Planet );
            }

            int maxPasses = TemenSpreadScratch.Count * TemenSpreadScratch.Count;
            for ( int pass = 0; pass < maxPasses; pass++ )
            {
                // Find the first pair of Temens that are too close
                GameEntity_Squad toMove = null;
                for ( int i = 0; i < TemenSpreadScratch.Count && toMove == null; i++ )
                    for ( int j = i + 1; j < TemenSpreadScratch.Count && toMove == null; j++ )
                        if ( TemenSpreadScratch[i].Planet.GetHopsTo( TemenSpreadScratch[j].Planet ) < MinHopsBetweenTemens )
                            toMove = TemenSpreadScratch[j];

                if ( toMove == null )
                    break; // all pairs are sufficiently spread

                // Find candidate planets at least MinHops from every other Temen, not a Bastion, and no King units
                Planet destination = null;
                foreach ( Planet candidate in galaxy.Planets( false ) )
                {
                    if ( candidate.PopulationType == PlanetPopulationType.AIBastionWorld )
                        continue;
                    if ( KingUnitPlanetsScratch.Contains( candidate ) )
                        continue;
                    bool tooClose = false;
                    for ( int k = 0; k < TemenSpreadScratch.Count && !tooClose; k++ )
                    {
                        if ( TemenSpreadScratch[k] == toMove )
                            continue;
                        if ( candidate.GetHopsTo( TemenSpreadScratch[k].Planet ) < MinHopsBetweenTemens )
                            tooClose = true;
                    }
                    if ( tooClose )
                        continue;
                    destination = candidate;
                    break;
                }

                if ( destination == null )
                    break; // no valid planet found; leave remaining pairs as-is

                GameEntityTypeData temenType = toMove.TypeData;
                byte mark = toMove.CurrentMarkLevel;
                toMove.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );

                PlanetFaction pFaction = destination.GetPlanetFactionForFaction( naturalFaction );
                ArcenPoint spawnPoint = destination.GetSafePlacementPointAroundPlanetCenter( Context, temenType, FInt.Zero, FInt.FromParts( 0, 300 ) );
                GameEntity_Squad newTemen = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, temenType, mark, null, 0, spawnPoint, Context, "Apkallu-TemenSpread" );

                // Update the scratch list so subsequent passes see the new location
                for ( int k = 0; k < TemenSpreadScratch.Count; k++ )
                {
                    if ( TemenSpreadScratch[k] != toMove )
                        continue;
                    if ( newTemen != null )
                        TemenSpreadScratch[k] = newTemen;
                    else
                        TemenSpreadScratch.RemoveAt( k );
                    break;
                }
            }
        }

        #region HandleOutpostProduction
        private void HandleOutpostProduction( ArcenHostOnlySimContext Context )
        {
            if ( BaseInfo.Difficulty == null )
                return;
            int aip = (int)FactionUtilityMethods.Instance.GetCurrentAIP();
            int metalIncome = BaseInfo.Difficulty.BaseMetalIncomePerSecond + ( aip * BaseInfo.Difficulty.MetalIncomePerSecondPer10AIP ) / 10;
            foreach ( GameEntity_Squad outpost in BaseInfo.Outposts.DisplaySquads() )
            {
                ApkalluPerUnitBaseInfo data = outpost.TryGetExternalBaseInfoAs<ApkalluPerUnitBaseInfo>();
                if ( data == null )
                    continue;
                data.MetalAccumulated += metalIncome;
                string shipTag = "ApkalluMobileShip";
                GameEntityTypeData shipType = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, shipTag );
                if ( shipType == null )
                    shipType = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ApkalluMobileShip" );
                if ( shipType == null )
                    continue;
                if ( data.MetalAccumulated >= shipType.CostForAIToPurchase )
                {
                    data.MetalAccumulated -= shipType.CostForAIToPurchase;
                    PlanetFaction pFaction = outpost.PlanetFaction;
                    GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, shipType, outpost.CurrentMarkLevel, null, 0, outpost.WorldLocation, Context, "Apkallu-OutpostSpawn" );
                }
            }
        }
        #endregion

        #region HandleLesserPilgrimProduction
        private void HandleLesserPilgrimProduction( ArcenHostOnlySimContext Context )
        {
            foreach ( GameEntity_Squad producer in BaseInfo.LesserPilgrimProducers.DisplaySquads() )
            {
                if ( producer == null || producer.GetIsCrippled() || producer.GetIsNonFunctional() )
                    continue;
                if ( producer.SelfBuildingMetalRemaining > 0 )
                    continue; // still under construction

                // Count living lesser pilgrims already homed to this producer
                int livingCount = 0;
                foreach ( GameEntity_Squad pilgrim in BaseInfo.Pilgrims.DisplaySquads() )
                {
                    if ( pilgrim == null || !pilgrim.TypeData.GetHasTag( "ApkalluLesserPilgrim" ) )
                        continue;
                    ApkalluPerUnitBaseInfo pd = pilgrim.TryGetExternalBaseInfoAs<ApkalluPerUnitBaseInfo>();
                    if ( pd != null && pd.HomeDuru.GetSquad() == producer )
                        livingCount++;
                }

                if ( livingCount >= 3 )
                    continue; // at cap — structure waits

                ApkalluPerUnitBaseInfo data = producer.TryGetExternalBaseInfoAs<ApkalluPerUnitBaseInfo>();
                if ( data == null )
                    continue;

                data.MetalAccumulated += BaseInfo.Difficulty.PilgrimIncome;

                GameEntityTypeData pilgrimType = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ApkalluLesserPilgrim" );
                if ( pilgrimType == null )
                    continue;

                if ( data.MetalAccumulated < pilgrimType.CostForAIToPurchase )
                    continue;

                data.MetalAccumulated -= pilgrimType.CostForAIToPurchase;
                GameEntity_Squad newPilgrim = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(
                    producer.PlanetFaction, pilgrimType, producer.CurrentMarkLevel, null, 0, producer.WorldLocation, Context, "Apkallu-LesserPilgrimSpawn" );
                if ( newPilgrim != null )
                {
                    ApkalluPerUnitBaseInfo pd = newPilgrim.CreateExternalBaseInfo<ApkalluPerUnitBaseInfo>( "ApkalluPerUnitBaseInfo" );
                    pd.ResourcePoints = 0;
                    pd.HomeDuru = LazyLoadSquadWrapper.Create( producer.PrimaryKeyID, true, "ApkalluLesserPilgrimProducer" );
                }
            }
        }
        #endregion

        private void HandleOutguardGranters( ArcenHostOnlySimContext Context )
        {
            // Rebuild which outguard groups are currently active from live granter structures
            BaseInfo.ActiveOutguardGroups.Clear();
            bool debug = false;
            foreach ( GameEntity_Squad granter in BaseInfo.OutguardGranters.DisplaySquads() )
            {
                if ( granter == null )
                    continue;
                if (debug)
                    ArcenDebugging.LogSingleLine("Updating outguard state for " + granter.ToString(), Verbosity.DoNotShow );
                List<string> tags = granter.TypeData.TagsList;
                if ( tags == null )
                    continue;
                if (debug)
                    ArcenDebugging.LogSingleLine("\titerating over the tags ", Verbosity.DoNotShow );
                for ( int i = 0; i < tags.Count; i++ )
                {
                    string tag = tags[i];
                    if (debug)
                        ArcenDebugging.LogSingleLine("\t\tTag: " + tag, Verbosity.DoNotShow );
                    if ( tag.StartsWith( "GrantsOutguardGroup_" ) )
                    {
                        string groupName = tag.Substring( "GrantsOutguardGroup_".Length );
                        BaseInfo.ActiveOutguardGroups.AddIfNotAlreadyIn( groupName );

                    }
                }
            }

            // Sync contact state for all ForApkallu outguard groups
            if ( OutguardFactionBaseInfo.Instance == null )
            {
                if ( debug )
                    ArcenDebugging.LogSingleLine("No outguard faction base info", Verbosity.DoNotShow );
                return;
            }
            ProtectedList<OutguardGroupData> allGroups = OutguardGroupDataTable.Instance.Rows;
            if ( debug )
                ArcenDebugging.LogSingleLine("Checking " +allGroups.Count + " outguard groups", Verbosity.DoNotShow );

            for ( int i = 0; i < allGroups.Count; i++ )
            {
                OutguardGroupData group = allGroups[i];

                if ( group.FactionAffinityTag != "ForApkallu" )
                    continue;
                if ( debug )
                    ArcenDebugging.LogSingleLine("\tGroup " +group + " apkallu? " + group.FactionAffinityTag, Verbosity.DoNotShow );

                if ( BaseInfo.ActiveOutguardGroups.Contains( group.InternalName ) )
                    OutguardFactionBaseInfo.Instance.ContactOutguardGroup_NonBeaconSource( group.InternalName );
                else
                    OutguardFactionBaseInfo.Instance.LoseContactWithOutguardGroup_NonBeaconSource( group.InternalName );
            }
        }
        
        #region Lamassu Modules
        private void ApplyLamassuModules( Planet planet, GameEntity_Squad lamassu )
        {
            bool debug = false;
            LamassuTagScratch.Clear();
            foreach ( GameEntity_Squad entity in planet.Squads() )
            {
                if ( entity.GetFactionOrNull_Safe() != this.AttachedFaction )
                    continue;
                if ( entity.TypeData.GetHasTag( "ApkalluZigguratSummoner" ) )
                {
                    // Each summoner structure has exactly one ApkalluSummonerXxx tag; collect them
                    List<string> tags = entity.TypeData.TagsList;
                    if ( tags != null )
                        for ( int i = 0; i < tags.Count; i++ )
                            if ( tags[i].StartsWith( "ApkalluSummoner" ) && !LamassuTagScratch.Contains( tags[i] ) )
                                LamassuTagScratch.Add( tags[i] );
                }
            }
            if ( debug )
            {
                for (int i = 0; i < LamassuTagScratch.Count; i++ )
                {
                    ArcenDebugging.LogSingleLine(lamassu.ToStringWithPlanet() + " has tag scratch " + LamassuTagScratch[i], Verbosity.DoNotShow );
                }

            }
            lamassu.SetEnabledSummonerTags( LamassuTagScratch );
        }
        #endregion

        #region RecalculateApkalluCityBuildingContents
        private void RecalculateApkalluCityBuildingContents( ArcenHostOnlySimContext Context )
        {
            int debugCode = 0;
            try
            {
                bool debug = false;
                // Duru garrison posts
                List<GameEntityTypeData> duruBuildings = GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( "BuiltByDuru" );
                debugCode = 100;
                if ( duruBuildings != null && duruBuildings.Count > 0 )
                {
                    foreach ( GameEntity_Squad duru in BaseInfo.Durus.DisplaySquads() )
                    {
                        if ( duru == null || duru.FleetMembership == null )
                            continue;
                        Fleet duruFleet = duru.GetFleetOrNull_Safe();
                        if ( duruFleet == null )
                            continue;
                        if ( duru.GetIsCrippled() || duru.GetIsNonFunctional() )
                        {
                            for ( int j = 0; j < duruBuildings.Count; j++ )
                            {
                                FleetMembership fleetMem = duruFleet.GetButDoNotAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( duruBuildings[j] );
                                if ( fleetMem != null )
                                    fleetMem.ExplicitBaseSquadCap = 0;
                            }
                            continue;
                        }
                        if ( debug )
                        {
                            ArcenDebugging.LogSingleLine("RecalculateApkalluCityBuildingContents; checking what " + duru.ToString() + " can build:", Verbosity.DoNotShow );
                        }
                        int duruMarkLevel = duru.CurrentMarkLevel;
                        for ( int j = 0; j < duruBuildings.Count; j++ )
                        {
                            GameEntityTypeData buildingInfo = duruBuildings[j];
                            if ( buildingInfo == null )
                                continue;
                            if ( debug )
                            {
                                ArcenDebugging.LogSingleLine("\tChecking if " + buildingInfo + " can be built; it is part of " + buildingInfo.BuildSidebarCategoriesIAmPartOf.Count + " build sidebar categories; this should be > 0 ", Verbosity.DoNotShow );
                                
                            }

                            if ( buildingInfo.MinimumRequiredCityLevelForConstruction > duruMarkLevel )
                            {
                                if ( debug )
                                {
                                    ArcenDebugging.LogSingleLine("\t\tNo, city too low level", Verbosity.DoNotShow );
                                }

                                FleetMembership fleetMem = duruFleet.GetButDoNotAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( buildingInfo );
                                if ( fleetMem != null )
                                    fleetMem.ExplicitBaseSquadCap = 0;
                                continue;
                            }
                            if ( buildingInfo.GetHasTag("UnlockedByBreach"))
                            {
                                bool buildingUnlocked = false;
                                if ( debug )
                                {
                                    ArcenDebugging.LogSingleLine("\t\tChecking if unlocked via breach", Verbosity.DoNotShow );
                                }

                                for ( int k = 0; k < BaseInfo.CompletedBreaches.Count; k++ )
                                {
                                    if ( debug )
                                    {
                                        ArcenDebugging.LogSingleLine("\t\t\tChecking breach " + BaseInfo.CompletedBreaches[k], Verbosity.DoNotShow );
                                    }

                                    if ( BaseInfo.CompletedBreaches[k].UnlockDuruStructures.Contains( buildingInfo ) )
                                    {
                                        if ( debug )
                                        {
                                            ArcenDebugging.LogSingleLine("\t\t\tBuilding Unlocked", Verbosity.DoNotShow );
                                        }
                                        buildingUnlocked = true;
                                    }
                                }
                                if ( !buildingUnlocked )
                                {
                                    FleetMembership fleetMem = duruFleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( buildingInfo );
                                    if ( fleetMem != null )
                                        fleetMem.ExplicitBaseSquadCap = 0;

                                    continue;
                                }
                            }

                            FleetMembership mem = duruFleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( buildingInfo );
                            if ( mem != null )
                            {
                                if ( debug )
                                {
                                    ArcenDebugging.LogSingleLine("\tCap for  " + buildingInfo + " is " + buildingInfo.BaseShipCapInCustomCity, Verbosity.DoNotShow );
                                }

                                mem.ExplicitBaseSquadCap = buildingInfo.BaseShipCapInCustomCity;
                            }
                        }
                    }
                }
                debugCode = 200;
                // Ziggurat summoner structures
                List<GameEntityTypeData> zigguratBuildings = GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( "ApkalluZigguratSummoner" );
                if ( zigguratBuildings != null && zigguratBuildings.Count > 0 )
                {
                    foreach ( GameEntity_Squad ziggurat in BaseInfo.Ziggurats.DisplaySquads() )
                    {
                        if ( ziggurat == null || ziggurat.FleetMembership == null )
                            continue;
                        Fleet zigguratFleet = ziggurat.GetFleetOrNull_Safe();
                        if ( zigguratFleet == null )
                            continue;
                        if ( ziggurat.GetIsCrippled() || ziggurat.GetIsNonFunctional() )
                        {
                            for ( int j = 0; j < zigguratBuildings.Count; j++ )
                            {
                                FleetMembership fleetMem = zigguratFleet.GetButDoNotAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( zigguratBuildings[j] );
                                if ( fleetMem != null )
                                    fleetMem.ExplicitBaseSquadCap = 0;
                            }
                            continue;
                        }
                        int zigguratMarkLevel = ziggurat.CurrentMarkLevel;
                        for ( int j = 0; j < zigguratBuildings.Count; j++ )
                        {
                            GameEntityTypeData buildingInfo = zigguratBuildings[j];
                            if ( buildingInfo == null )
                                continue;
                            if ( buildingInfo.GetHasTag("UnlockedByBreach"))
                            {
                                bool buildingUnlocked = false;
                                for ( int k = 0; k < BaseInfo.CompletedBreaches.Count; k++ )
                                {
                                    if ( BaseInfo.CompletedBreaches[k].UnlockZigguratStructure == buildingInfo )
                                    {
                                        buildingUnlocked = true;
                                    }
                                }
                                if ( !buildingUnlocked )
                                {
                                    FleetMembership fleetMem = zigguratFleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( buildingInfo );
                                    if ( fleetMem != null )
                                        fleetMem.ExplicitBaseSquadCap = 0;
                                    continue;
                                }
                            }
                            if ( buildingInfo.MinimumRequiredCityLevelForConstruction > zigguratMarkLevel )
                            {
                                FleetMembership fleetMem = zigguratFleet.GetButDoNotAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( buildingInfo );
                                if ( fleetMem != null )
                                    fleetMem.ExplicitBaseSquadCap = 0;
                                continue;
                            }
                            FleetMembership mem = zigguratFleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( buildingInfo );
                            if ( mem != null )
                                mem.ExplicitBaseSquadCap = buildingInfo.BaseShipCapInCustomCity;
                        }
                    }
                }
                debugCode = 250;
                // Temen shrine structures
                List<GameEntityTypeData> temenBuildings = GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( "BuiltByTemen" );
                if ( temenBuildings != null && temenBuildings.Count > 0 )
                {
                    foreach ( GameEntity_Squad temen in BaseInfo.Temens.DisplaySquads() )
                    {
                        if ( temen == null || temen.FleetMembership == null )
                            continue;
                        Fleet temenFleet = temen.GetFleetOrNull_Safe();
                        if ( temenFleet == null )
                            continue;
                        if ( temen.GetIsCrippled() || temen.GetIsNonFunctional() )
                        {
                            for ( int j = 0; j < temenBuildings.Count; j++ )
                            {
                                FleetMembership fleetMem = temenFleet.GetButDoNotAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( temenBuildings[j] );
                                if ( fleetMem != null )
                                    fleetMem.ExplicitBaseSquadCap = 0;
                            }
                            continue;
                        }
                        for ( int j = 0; j < temenBuildings.Count; j++ )
                        {
                            GameEntityTypeData buildingInfo = temenBuildings[j];
                            if ( buildingInfo == null )
                                continue;
                            if ( buildingInfo.GetHasTag( "UnlockedByBreach" ) )
                            {
                                bool buildingUnlocked = false;
                                for ( int k = 0; k < BaseInfo.CompletedBreaches.Count; k++ )
                                {
                                    if ( BaseInfo.CompletedBreaches[k].UnlockTemenStructure == buildingInfo )
                                    {
                                        buildingUnlocked = true;
                                        break;
                                    }
                                }
                                if ( !buildingUnlocked )
                                {
                                    FleetMembership fleetMem = temenFleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( buildingInfo );
                                    if ( fleetMem != null )
                                        fleetMem.ExplicitBaseSquadCap = 0;
                                    continue;
                                }
                            }
                            FleetMembership mem = temenFleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( buildingInfo );
                            if ( mem != null )
                                mem.ExplicitBaseSquadCap = buildingInfo.BaseShipCapInCustomCity;
                        }
                    }
                }
                debugCode = 300;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "ApkalluFactionDeepInfo.RecalculateApkalluCityBuildingContents debugCode " + debugCode + " " + e, Verbosity.DoNotShow );
            }
        }
        #endregion

        #region DoLongRangePlanning_OnBackgroundNonSimThread_Subclass
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            PerFactionPathCache pathingCache = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            int debugCode = 0;
            try
            {
                debugCode = 100;
                OutpostsLRP.Clear();
                foreach ( GameEntity_Squad outpost in BaseInfo.Outposts.DisplaySquads() )
                {
                    if ( outpost == null )
                        continue;
                    OutpostsLRP.Add( outpost );
                }
                debugCode = 200;
                MobileShipsLRP.Clear();
                foreach ( GameEntity_Squad ship in BaseInfo.MobileShips.DisplaySquads() )
                {
                    if ( ship == null )
                        continue;

                    MobileShipsLRP.Add( ship );
                }
                debugCode = 250;
                LocustsLRP.Clear();
                foreach ( GameEntity_Squad locust in BaseInfo.Locusts.DisplaySquads() )
                {
                    if ( locust == null )
                        continue;

                    LocustsLRP.Add( locust );
                }
                debugCode = 275;
                PilgrimsLRP.Clear();
                foreach ( GameEntity_Squad pilgrim in BaseInfo.Pilgrims.DisplaySquads() )
                {
                    if ( pilgrim == null )
                        continue;

                    PilgrimsLRP.Add( pilgrim );
                }
                RangersLRP.Clear();
                foreach ( GameEntity_Squad ranger in BaseInfo.Rangers.DisplaySquads() )
                {
                    RangersLRP.Add( ranger );
                }
                debugCode = 300;
                debugCode = 400;
                FleetBehaviorLRP.DoLRP( AttachedFaction, Context );

                debugCode = 500;
                HandleLocustsLRP( Context, pathingCache );
                HandlePilgrims_LRP( Context, pathingCache );
                HandleRangersLRP( Context, pathingCache );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "ApkalluFactionDeepInfo.DoLongRangePlanning debugCode " + debugCode + " " + e, Verbosity.DoNotShow );
            }
            finally
            {
                pathingCache.ReturnToPool();
            }
        }

        private void HandleLocustsLRP( ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathingCacheData )
        {
            int shipsThatCanMove = 40;
            for ( int i = 0; i < LocustsLRP.Count; i++ )
            {
                GameEntity_Squad locust = LocustsLRP[i].GetSquad();
                if ( locust == null )
                    continue;
                ApkalluPerUnitBaseInfo data = locust.TryGetExternalBaseInfoAs<ApkalluPerUnitBaseInfo>();
                if ( data == null )
                    continue;
                if ( locust.HasQueuedOrders() )
                    continue;
                if ( shipsThatCanMove <= 0 )
                    break;

                // Assign a destination if we don't have one yet (e.g. after a load)
                if ( data.LocustDestination == null )
                    data.LocustDestination = GetNewLocustDestination( locust, Context );
                if ( data.LocustDestination == null )
                    continue;

                // Already at the destination — re-evaluate if the fight is nearly won
                if ( locust.Planet == data.LocustDestination )
                {
                    if ( BaseInfo.DoesPlanetHaveZiggurat( locust.Planet ) )
                        continue; // guarding our Ziggurat, stay put
                    EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> stanceData = locust.Planet.GetStanceDataForFaction( AttachedFaction );
                    int hostileStrength = stanceData[FactionStance.Hostile].TotalStrength;
                    int alliedStrength  = stanceData[FactionStance.Friendly].TotalStrength + stanceData[FactionStance.Self].TotalStrength;
                    if ( hostileStrength < alliedStrength / 10 )
                        data.LocustDestination = GetNewLocustDestination( locust, Context );
                }

                if ( locust.Planet != data.LocustDestination && data.LocustDestination != null )
                {
                    AutoDefendUtility.GoToPlanet( locust, data.LocustDestination, Context, pathingCacheData );
                    shipsThatCanMove--;
                }
            }
        }

        private void HandlePilgrims_LRP( ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathingCacheData )
        {
            int shipsThatCanMove = 20;
            for ( int i = 0; i < PilgrimsLRP.Count; i++ )
            {
                GameEntity_Squad pilgrim = PilgrimsLRP[i].GetSquad();
                if ( pilgrim == null )
                    continue;
                ApkalluPerUnitBaseInfo data = pilgrim.TryGetExternalBaseInfoAs<ApkalluPerUnitBaseInfo>();
                if ( data == null )
                    continue;

                if ( pilgrim.HasQueuedOrders() )
                    continue;
                if ( shipsThatCanMove <= 0 )
                    break;


                int myStrength = pilgrim.GetStrengthOfSelfAndContents();
                bool isLesserPilgrimLRP = pilgrim.TypeData.GetHasTag( "ApkalluLesserPilgrim" );

                // Build a list of safe, unvisited Temen planets.
                PilgrimCandidatePlanetsLRP.Clear();
                bool pilgrimDispatched = false;
                foreach ( GameEntity_Squad temen in AttachedFaction.Squads( "ApkalluTemen" ) )
                {
                    Planet temenPlanet = temen.Planet;

                    // Skip planets the pilgrim has already visited
                    bool alreadyVisited = false;
                    for ( int v = 0; v < data.PlanetsVisited.Count; v++ )
                    {
                        if ( data.PlanetsVisited[v] == temenPlanet )
                        { alreadyVisited = true; break; }
                    }
                    if ( temenPlanet == pilgrim.Planet && !alreadyVisited )
                    {
                        AutoDefendUtility.GoToLocation( pilgrim, temen.WorldLocation, Context, pathingCacheData, AttachedFaction );
                        pilgrimDispatched = true;
                        break;
                    }
                    if ( alreadyVisited )
                        continue;

                    // Skip paths that are too dangerous
                    short hops = 0;
                    int danger = Fireteam.GetDangerOfPath( AttachedFaction, Context, pathingCacheData, pilgrim.Planet, temenPlanet, true, out hops );
                    if ( danger > myStrength )
                        continue;

                    PilgrimCandidatePlanetsLRP.Add( temenPlanet );
                }
                if ( pilgrimDispatched )
                    continue; //the Pilgrim already has orders

                Planet destination = null;
                if ( PilgrimCandidatePlanetsLRP.Count > 0 )
                {
                    // Pick a random safe, unvisited Temen
                    destination = PilgrimCandidatePlanetsLRP[Context.RandomToUse.Next( 0, PilgrimCandidatePlanetsLRP.Count )];
                }
                else
                {
                    // No safe unvisited Temen — head to the nearest safe Ziggurat instead
                    foreach ( GameEntity_Squad ziggurat in BaseInfo.Ziggurats.DisplaySquads() )
                    {
                        if ( ziggurat.Planet == pilgrim.Planet )
                        {
                            AutoDefendUtility.GoToLocation( pilgrim, ziggurat.WorldLocation, Context, pathingCacheData, AttachedFaction );
                            continue;
                        }
                        short hops = 0;
                        int danger = Fireteam.GetDangerOfPath( AttachedFaction, Context, pathingCacheData, pilgrim.Planet, ziggurat.Planet, true, out hops );
                        if ( danger > myStrength )
                            continue;
                        destination = ziggurat.Planet;
                        break;
                    }
                }

                if ( destination != null )
                {
                    AutoDefendUtility.GoToPlanet( pilgrim, destination, Context, pathingCacheData );
                    shipsThatCanMove--;
                }
            }
        }

        private void HandleRangersLRP( ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathingCacheData )
        {
            for ( int i = 0; i < RangersLRP.Count; i++ )
            {
                GameEntity_Squad ranger = RangersLRP[i].GetSquad();
                if ( ranger == null )
                    continue;
                if ( ranger.HasQueuedOrders() )
                    continue;

                ApkalluPerUnitBaseInfo data = ranger.TryGetExternalBaseInfoAs<ApkalluPerUnitBaseInfo>();
                if ( data == null )
                    continue;

                GameEntity_Squad homeDuru = data.HomeDuru.GetSquad();
                if ( homeDuru == null )
                    continue; // Stage2 will despawn it when Duru is gone

                bool goHome = false;
                int myStrength = ranger.GetStrengthOfSelfAndContents();

                if ( AutoDefendUtility.IsLosingBattle( ranger.Planet, myStrength, AttachedFaction ) )
                    goHome = true;

                if ( ranger.TypeData.AllowedHopsFromCenterpiece > -1 )
                {
                    if ( homeDuru.Planet.GetHopsTo( ranger.Planet ) > ranger.TypeData.AllowedHopsFromCenterpiece )
                        goHome = true;
                }

                if ( goHome )
                    AutoDefendUtility.GoToPlanet( ranger, homeDuru.Planet, Context, pathingCacheData );
            }
        }

        private Planet GetNewLocustDestination( GameEntity_Squad locust, ArcenLongTermIntermittentPlanningContext Context )
        {
            Planet output = null;
            Planet fallback = null;
            foreach ( Planet neighbor in locust.Planet.LinkedNeighbors( false ) )
            {
                PlanetFaction pFaction = neighbor.GetPlanetFactionForFaction( AttachedFaction );
                if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength > 10 * 1000 )
                {
                    fallback = neighbor;
                    if ( Context.RandomToUse.Next( 0, 100 ) < 60 )
                    {
                        output = neighbor;
                        break;
                    }
                }
                if ( fallback == null || Context.RandomToUse.Next( 0, 100 ) < 50 )
                    fallback = neighbor;
            }
            if ( output == null )
                output = fallback;
            return output;
        }
        #endregion
        public GameEntity_Squad SpawnApkalluFlagship( ArcenPoint spawnLocation, Planet planet, string TypeName, Faction faction, 
                                                            ArcenHostOnlySimContext Context, bool exactPlacement )
        
        {
            GameEntity_Squad apkalluFlagship = null;
            if ( !ArcenNetworkAuthority.GetIsHostMode() )
            {
                return null; //only for the host; clients will get this data sync'd to them later
            }
            GameEntityTypeData flagshipData = GameEntityTypeDataTable.Instance.GetRowByName( TypeName );
            if ( flagshipData == null ) {
                flagshipData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, TypeName );
                if ( flagshipData == null )
                    throw new Exception("Unable to find XML with name or tag " + TypeName);
            }
            if ( spawnLocation == ArcenPoint.ZeroZeroPoint )
            {
                spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, flagshipData, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 300 ) );
            }
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction(this.AttachedFaction);
            apkalluFlagship = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, flagshipData, 1,
                null, 0, spawnLocation, Context, "Apkallu-NewApkalluFlagship" );
            if ( apkalluFlagship != null )
            {
                Fleet apkalluFleet = apkalluFlagship.FleetMembership.Fleet;
                apkalluFleet.IsFleetFlagshipAllowedToUseMovementModes = true;
                if ( apkalluFleet.TryGetExternalBaseInfoAs<ApkalluMobileFleetBaseInfo>() == null )
                    apkalluFleet.CreateExternalBaseInfo<ApkalluMobileFleetBaseInfo>( "ApkalluMobileFleetBaseInfo" );
                GameCommand watchCmd = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                watchCmd.RelatedIntegers.Add( apkalluFleet.FleetID );
                watchCmd.RelatedIntegers.Add( PlayerAccount.Local.PlayerPrimaryKeyID );
                watchCmd.RelatedString = "ToggleIsFleetOnPlayerWatchlist";
                watchCmd.RelatedBool = true;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), watchCmd, true );
            }
            return apkalluFlagship;
        }
        public GameEntity_Squad SpawnApkalluZiggurat( ArcenPoint spawnLocation, Planet planet, string TypeName, Faction faction, 
                                                            ArcenHostOnlySimContext Context, out GameEntity_Squad ApkalluFlagship, bool exactPlacement )
        {
            if ( !ArcenNetworkAuthority.GetIsHostMode() )
            {
                ApkalluFlagship = null;
                return null; //only for the host; clients will get this data sync'd to them later
            }
            GameEntityTypeData zigguratData = GameEntityTypeDataTable.Instance.GetRowByName( TypeName );
            if ( zigguratData == null ) {
                zigguratData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, TypeName );
                if ( zigguratData == null )
                    throw new Exception("Unable to find XML with name or tag " + TypeName);
            }
            GameEntityTypeData flagshipEntityData = null;

            {
                // I haven't figured out how major ziggurats or new flagships work
                flagshipEntityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ApkalluLamassu" );

                if (flagshipEntityData == null)
                    throw new Exception("Couldn't figure out which flagship to spawn from " + zigguratData.GetDisplayName());
            }
            if (flagshipEntityData == null)
                ArcenDebugging.LogSingleLine("apkallu flagship is null", Verbosity.DoNotShow );
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
            if ( spawnLocation == ArcenPoint.ZeroZeroPoint )
            {
                spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, zigguratData, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 300 ) );
            }
            else if ( exactPlacement )
                spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, zigguratData, spawnLocation, FInt.FromParts( 0, 010 ), FInt.FromParts( 0, 020 ) );
            else
                spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, zigguratData, spawnLocation, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 250 ) );
            GameEntity_Squad ziggurat = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, zigguratData, 1,
                    null, 0, spawnLocation, Context, "Apkallu-NewZiggurat" );
            GameEntity_Squad apkalluFlagship = null;
            bool spawnsFlagship = flagshipEntityData != null;
            bool bolstersFlagship = zigguratData.GetHasTag("BolstersApkalluFleet");
            
            if ( spawnsFlagship )
            {
                spawnLocation = planet.GetSafePlacementPoint_AroundEntity( Context, flagshipEntityData, ziggurat, FInt.FromParts( 0, 005 ), FInt.FromParts( 0, 010 ) );

                apkalluFlagship = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, flagshipEntityData, 1,
                           null, 0, spawnLocation, Context, "Apkallu-NewApkalluFlagship" );
                ApkalluFlagship = apkalluFlagship;
                if ( apkalluFlagship == null )
                    return ziggurat;
                apkalluFlagship.FleetMembership.Fleet.IsFleetFlagshipAllowedToUseMovementModes = true;
                ApplyLamassuModules( ziggurat.Planet, apkalluFlagship );
                ApkalluPerUnitBaseInfo zigUnit = ziggurat.CreateExternalBaseInfo<ApkalluPerUnitBaseInfo>( "ApkalluPerUnitBaseInfo" );
                zigUnit.LamassuEntityPrimaryKeyID = apkalluFlagship.PrimaryKeyID;

                Fleet apkalluFleet = apkalluFlagship.FleetMembership.Fleet;
                GameCommand watchCmd = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                watchCmd.RelatedIntegers.Add( apkalluFleet.FleetID );
                watchCmd.RelatedIntegers.Add( PlayerAccount.Local.PlayerPrimaryKeyID );
                watchCmd.RelatedString = "ToggleIsFleetOnPlayerWatchlist";
                watchCmd.RelatedBool = true;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), watchCmd, true );

            }
            else
            {
                ApkalluFlagship = null;
            }



            Fleet newApkalluZigguratFleet = ziggurat.FleetMembership.Fleet;

            string nameBase = ziggurat.Planet.Name;

            newApkalluZigguratFleet.NameRaw = "Ziggurat " + nameBase;
            newApkalluZigguratFleet.FleetQualifier = "Apkallu";

            ApkalluCityFleetBaseInfo apkalluCityFleetInfo = newApkalluZigguratFleet.CreateExternalBaseInfo<ApkalluCityFleetBaseInfo>("ApkalluCityFleetBaseInfo");

            if ( bolstersFlagship )
            {
                apkalluCityFleetInfo.CanChangeBolsteredFleet = true;
            }
            
            if ( spawnsFlagship )
            {
                Fleet newApkalluMobileFleet = apkalluFlagship.FleetMembership.Fleet;
                newApkalluMobileFleet.NameRaw = "Apkallufleet " + nameBase;
                newApkalluMobileFleet.FleetQualifier = "Apkallu";
                newApkalluMobileFleet.CreateExternalBaseInfo<ApkalluMobileFleetBaseInfo>( "ApkalluMobileFleetBaseInfo" );
                newApkalluZigguratFleet.CityBolstersFleetID = newApkalluMobileFleet.FleetID;
            }
            //            ArcenDebugging.ArcenDebugLogSingleLine("Created a new fleet, " + newApkalluFleet.GetName() + " with flagship " + apkalluFlagship.ToString(), Verbosity.DoNotShow );
            // List<GameEntityTypeData> InitialShipsForFlagship = GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( "ApkalluSummons" );
            // for ( int i = 0; i < InitialShipsForFlagship.Count; i++ )
            // {
            //     GameEntityTypeData entitydata = InitialShipsForFlagship[i];
            //     int nextUniqueID = newApkalluFleet.GetNextUniqueIntToUseOfMatchingMembershipGroupsBasedOnSquadType( entitydata );
            //     FleetMembership mem = newApkalluFleet.GetOrAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates( entitydata, nextUniqueID );
            //     mem.ExplicitBaseSquadCap = 1;
            // }

            this.BaseInfo.Ziggurats.AddToDisplayList(ziggurat);
            return ziggurat;
        }

        public GameEntity_Squad SpawnApkalluDuru( ArcenPoint spawnLocation, Planet planet, string TypeName, Faction faction,
                                                   ArcenHostOnlySimContext Context, bool exactPlacement )
        {
            if ( !ArcenNetworkAuthority.GetIsHostMode() )
                return null;
            GameEntityTypeData duruData = GameEntityTypeDataTable.Instance.GetRowByName( TypeName );
            if ( duruData == null )
                throw new Exception( "Unable to find Duru XML with name " + TypeName );
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
            if ( spawnLocation == ArcenPoint.ZeroZeroPoint )
                spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, duruData, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 300 ) );
            else if ( exactPlacement )
                spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, duruData, spawnLocation, FInt.FromParts( 0, 010 ), FInt.FromParts( 0, 020 ) );
            else
                spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, duruData, spawnLocation, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 250 ) );
            GameEntity_Squad duru = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, duruData, 1,
                    null, 0, spawnLocation, Context, "Apkallu-NewDuru" );
            if ( duru == null )
                return null;
            Fleet duruFleet = duru.FleetMembership.Fleet;
            duruFleet.NameRaw = "Duru " + planet.Name;
            duruFleet.FleetQualifier = "Apkallu";
            ApkalluCityFleetBaseInfo duruCityInfo = duruFleet.CreateExternalBaseInfo<ApkalluCityFleetBaseInfo>( "ApkalluCityFleetBaseInfo" );
            duruCityInfo.CanChangeBolsteredFleet = true;
            this.BaseInfo.Durus.AddToDisplayList( duru );
            return duru;
        }

    }
}
