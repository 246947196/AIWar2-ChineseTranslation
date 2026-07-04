using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public sealed class NeinzulCustodiansFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        public NeinzulCustodiansFactionBaseInfo BaseInfo;

        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<NeinzulCustodiansFactionBaseInfo>();
        }

        protected override void Cleanup()
        {
            BaseInfo = null;
        }

        public override void SeedStartingEntities_EarlyMajorFactionClaimsOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            if ( BaseInfo.EnclaveName == string.Empty )
                PickEnclaveType( Context );
        }

        public void PickEnclaveType( ArcenHostOnlySimContext Context )
        {
            // Each Custodian faction gets their own unique Enclave; pick it here.
            #region Enclave Picking
            if ( BaseInfo.ParentInfo.enclaveTypes.Count == 0 )
            {
                // Ran out of valid ships in our list, refill it.
                GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( BaseInfo.EnclaveTag ).ForEach( _ => BaseInfo.ParentInfo.enclaveTypes.Add( _ ) );
            }

            BaseInfo.EnclaveName = BaseInfo.ParentInfo.enclaveTypes.RemoveAtAndReturn( Context.RandomToUse.Next( BaseInfo.ParentInfo.enclaveTypes.Count ) ).InternalName;
            #endregion
        }

        public override void DoOnFirstSightingOfFactionByPlayer( bool IsFromBeacon, GameEntity_Squad SquadSeenOrNull, ArcenHostOnlySimContext Context )
        {
            switch ( AttachedFaction.SpecialFactionData.InternalName )
            {
                case "NeinzulCustodiansPearl":
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Custodians_Introduction", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    break;
                case "NeinzulCustodiansDiamond":
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Custodians_Diamond", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    break;
                case "NeinzulCustodiansRuby":
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Custodians_Ruby", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    break;
                case "NeinzulCustodiansSaphire":
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Custodians_Sapphire", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    break;
                case "NeinzulCustodiansEmerald":
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Custodians_Emerald", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    break;
                case "NeinzulCustodiansOnyx":
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Custodians_Onyx", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    break;
                default:
                    break;
            }
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            if ( !BaseInfo.IsEnabled )
                return;

            HandleHiveDrifting( Context );
            HandleInflux( Context );
            HandleClanlingBuilding( Context );
            UpdateMarkLevels( Context );
        }

        #region Stage3
        public void HandleHiveDrifting( ArcenHostOnlySimContext Context )
        {
            foreach ( KeyValuePair<Planet, short> pair in BaseInfo.hivesByPlanet.GetDisplayDict() )
            {
                Planet planet = pair.Key;
                short hivesOnMain = pair.Value;
                if ( hivesOnMain < 4 )
                    continue; // Not enough hives, do nothing.

                // If there is an adjacent planet with no threat nearby that has half our hives or fewer, spread to it.
                foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                {
                    short hivesOnAdj = BaseInfo.hivesByPlanet.GetDisplayDict().GetEntryOrSpecifiedDefaultIfMissing<Planet, short>( neighbor, 0 );

                    if ( hivesOnAdj >= hivesOnMain / 2 )
                        continue; // Adj has too many hives for us to spread to

                    bool isThreatened = false;

                    foreach ( Planet workingPlanet in neighbor.LinkedNeighborsAndSelf( false ) )
                    {
                        if ( workingPlanet.GetDataByStanceForFaction( AttachedFaction, FactionStance.Hostile ).TotalStrength >= 100 )
                        {
                            isThreatened = true;
                            break;
                        }
                    }

                    if ( isThreatened )
                        continue; // Adj isn't safe.

                    // Planet has less than half our hives and it's safe enough. Move one of our hives over.
                    GameEntity_Squad hiveToMove = planet.GetFirstMatching( BaseInfo.HiveTag, AttachedFaction, true, true );
                    if ( hiveToMove == null )
                        break;

                    GameEntity_Squad connectedEnclave = World_AIW2.Instance.GetEntityByID_Squad( hiveToMove.MinorFactionStackingID );
                    GameEntity_Squad newHive = neighbor.Mapgen_SeedEntity( Context, AttachedFaction, hiveToMove.TypeData, PlanetSeedingZone.OuterSystem );

                    newHive.MinorFactionStackingID = connectedEnclave.PrimaryKeyID;
                    connectedEnclave.MinorFactionStackingID = newHive.PrimaryKeyID;

                    newHive.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );

                    hiveToMove.MinorFactionStackingID = -1;
                    hiveToMove.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );

                    break;
                }
            }
        }
        public void HandleInflux( ArcenHostOnlySimContext Context )
        {
            if ( BaseInfo.ShouldHaveInflux )
            {
                if ( BaseInfo.territory.GetDisplayList().Count > 0 )
                    InfluxRegular( Context );
                else
                    InfluxSpawn( Context );
            }
        }
        public void InfluxRegular( ArcenHostOnlySimContext Context )
        {
            List<Planet> potentialPlanets = Planet.GetTemporaryPlanetList( "NeinzulCustodian-InfluxRegular-potentialPlanets", 10f );
            if ( potentialPlanets == null ) //blocked for teardown/shutdown; bail
                return;

            foreach ( Planet planet in BaseInfo.territory.GetDisplayList() )
            {
                foreach ( Planet workingPlanet in planet.LinkedNeighbors( false ) )
                {
                    if ( !BaseInfo.territory.DisplayContains( workingPlanet ) && !potentialPlanets.Contains( workingPlanet ) )
                    {
                        var strengthData = workingPlanet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance;

                        // Consider valid if allies have over 5k strength and there is no enemy presence or
                        // allies have over 500% strength advantage.
                        bool canSpawnOn = (strengthData[FactionStance.Friendly].TotalStrength > 5000 &&
                        (strengthData[FactionStance.Hostile].TotalStrength < 1000) ||
                        strengthData[FactionStance.Friendly].TotalStrength >= strengthData[FactionStance.Hostile].TotalStrength * 5);

                        if ( canSpawnOn )
                            potentialPlanets.Add( workingPlanet );
                    }
                }
            }

            int canSpawn = BaseInfo.MaxCustodians - BaseInfo.enclaves.Count;
            int numberToExpandTo = BaseInfo.Difficulty.MaxExpansionPlanetsPerInflux;
            int numberToReinforce = BaseInfo.Difficulty.MaxReinforcementsPerInflux;

            // If we don't have enough expansion targets, convert those to reinforcements instead.
            while ( numberToExpandTo > potentialPlanets.Count )
            {
                numberToExpandTo--;
                numberToReinforce++;
            }

            for ( int x = 0; x < numberToReinforce && canSpawn > 0; x++, canSpawn-- )
                SpawnEnclaveHivePairOn( BaseInfo.territory.Display_GetRandomItem( Context.RandomToUse ), Context );

            for ( int x = 0; x < numberToExpandTo && canSpawn > 0; x++, canSpawn-- )
                SpawnEnclaveHivePairOn( potentialPlanets.RemoveAtAndReturn( Context.RandomToUse.Next( potentialPlanets.Count ) ), Context );

            Planet.ReleaseTemporaryPlanetList( potentialPlanets );
            BaseInfo.GameSecondOfLastInflux = World_AIW2.Instance.GameSecond;
        }
        public void InfluxSpawn( ArcenHostOnlySimContext Context )
        {
            // Find out where to spawn based on our allegiance.
            Planet spawnPlanet = null;
            switch ( BaseInfo.Allegiance )
            {
                case "Allied To AI":
                    spawnPlanet = FactionUtilityMethods.Instance.findFirstAIKing( false );
                    break;
                case "Friendly To Players":
                    spawnPlanet = FactionUtilityMethods.Instance.findHumanKing( false );
                    break;
                default:
                    // Join any allied planets that have a strength advantage on them.
                    if ( World_AIW2.Instance.GameSecond % 120 != 0 )
                        return; // Only check every few minutes.
                    List<Planet> potentialPlanets = Planet.GetTemporaryPlanetList( "NeinzulCustodians-InfluxSpawn-potentialPlanets", 10f );
                    if ( potentialPlanets == null ) //blocked for teardown/shutdown; bail
                        return;
                    foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                    {
                        foreach ( Faction alliedFaction in World_AIW2.Instance.Factions )
                        {
                            if ( alliedFaction.BaseInfo.Allegiance != BaseInfo.Allegiance )
                                continue; // Not an ally.

                            EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> strengthData = planet.GetPlanetFactionForFaction( alliedFaction ).DataByStance;
                            int alliedStrength = strengthData[FactionStance.Self].TotalStrength;
                            int hostileStrength = strengthData[FactionStance.Hostile].TotalStrength;

                            // If the ally has strength on this planet, and at least one stationary unit, we can spawn here.
                            if ( alliedStrength < Math.Max( 5000, hostileStrength ) )
                                continue; // Too weak.

                            bool hasStationary = false;

                            foreach ( GameEntity_Squad workingEntity in planet.GetPlanetFactionForFaction( alliedFaction ).Entities.Squads() )
                            {
                                if ( !workingEntity.TypeData.IsMobile )
                                {
                                    hasStationary = true;
                                    break;
                                }

                            }

                            if ( !hasStationary )
                                continue;

                            potentialPlanets.Add( planet );

                            break;
                        }
                    }
                    if ( potentialPlanets.Count > 0 )
                        spawnPlanet = potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];
                    Planet.ReleaseTemporaryPlanetList( potentialPlanets );
                    break;
            }

            if ( spawnPlanet == null )
                return; // Don't error out, just keep trying to find allies.

            for ( int x = 0; x < BaseInfo.Difficulty.MaxReinforcementsPerInflux * 2; x++ )
                SpawnEnclaveHivePairOn( spawnPlanet, Context );

            BaseInfo.GameSecondOfLastInflux = World_AIW2.Instance.GameSecond;
        }
        public void SpawnEnclaveHivePairOn( Planet planet, ArcenHostOnlySimContext Context )
        {
            if ( BaseInfo.EnclaveName == string.Empty )
            {
                // Sanity check. There are some unique edge cases, such as Beacons, that can result in Custodians not being properly seeded.
                // Fix it if needed.
                PickEnclaveType( Context );
            }

            GameEntity_Squad hive = planet.Mapgen_SeedEntity( Context, AttachedFaction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, BaseInfo.HiveTag ), PlanetSeedingZone.OuterSystem );
            GameEntity_Squad enclave = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( hive.PlanetFaction, GameEntityTypeDataTable.Instance.GetRowByName( BaseInfo.EnclaveName ), 1, hive.PlanetFaction.FleetUsedAtPlanet, 0,
                hive.WorldLocation, Context, "Custodians-EnclaveHivePair" );

            for ( int x = 0; x < 3; x++ )
            {
                GameEntity_Squad clanling = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( enclave.PlanetFaction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, BaseInfo.ClanlingTag ),
                    BaseInfo.FactionMarkLevel, enclave.PlanetFaction.FleetUsedAtPlanet, 0, enclave.WorldLocation, Context, "Custodians-EnclaveHivePair" );

                clanling.MinorFactionStackingID = enclave.PrimaryKeyID;
                clanling.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
            }

            hive.MinorFactionStackingID = enclave.PrimaryKeyID;
            hive.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
            enclave.MinorFactionStackingID = hive.PrimaryKeyID;
            enclave.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
        }
        public void HandleClanlingBuilding( ArcenHostOnlySimContext Context )
        {
            if ( BaseInfo.clanlings.Count > BaseInfo.enclaves.Count * BaseInfo.MaxClanlingsPerCustodian )
                return; // At capacity.

            foreach ( GameEntity_Squad hive in BaseInfo.hives.DisplaySquads() )
            {
                NeinzulCustodiansPerUnitBaseInfo buildInfo = hive.GetExternalBaseInfoAs<NeinzulCustodiansPerUnitBaseInfo>();

                buildInfo.Budget++;

                if ( buildInfo.Budget >= BaseInfo.Difficulty.SecondsPerClanlingConstruction )
                {
                    GameEntity_Squad enclave = World_AIW2.Instance.GetEntityByID_Squad( hive.MinorFactionStackingID );
                    if ( enclave == null )
                    {
                        hive.Die( Context, true );
                        continue; //was DelReturn.RemoveAndContinue, but Display_DF never honored that — always a no-op here
                    }

                    GameEntity_Squad clanling = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( hive.PlanetFaction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, BaseInfo.ClanlingTag ),
                        BaseInfo.FactionMarkLevel, hive.PlanetFaction.FleetUsedAtPlanet, 0, hive.WorldLocation, Context, "Custodians-ClanlingBuild" );

                    clanling.MinorFactionStackingID = enclave.PrimaryKeyID;
                    clanling.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );

                    buildInfo.Budget = 0;
                }
            }
        }
        public void UpdateMarkLevels( ArcenHostOnlySimContext Context )
        {
            foreach ( GameEntity_Squad enclave in BaseInfo.enclaves.DisplaySquads() )
            {
                if ( enclave.CurrentMarkLevel < 7 )
                    enclave.SetCurrentMarkLevelIfHigherThanCurrent( BaseInfo.GetMarkLevelFor( enclave ) );

            }
            foreach ( GameEntity_Squad clanling in BaseInfo.clanlings.DisplaySquads() )
            {
                if ( clanling.CurrentMarkLevel < 7 )
                    clanling.SetCurrentMarkLevelIfHigherThanCurrent( BaseInfo.GetMarkLevelFor( clanling ) );

            }
            foreach ( GameEntity_Squad hive in BaseInfo.hives.DisplaySquads() )
            {
                if ( hive.CurrentMarkLevel < 7 )
                    hive.SetCurrentMarkLevelIfHigherThanCurrent( BaseInfo.GetMarkLevelFor( hive ) );

            }
        }
        #endregion

        public readonly ProtectedValDictionary<Planet, FireteamRegiment> TeamsAimedAtPlanet = ProtectedValDictionary<Planet, FireteamRegiment>.Create_WillNeverBeGCed( 100, "NeinzulCustodiansFactionDeepInfo-TeamsAimedAtPlanet" );

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( !BaseInfo.IsEnabled )
                return;

            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            ArcenCharacterBuffer tracingBuffer = null; // Not quite sure how to set this up; look into this if needed.

            TeamsAimedAtPlanet.Clear();

            HandleEarlyRetreatAndBleedoffLogic( Context, pathingCacheData );
            FireteamUtility.CleanUpDisbandedFireteams( BaseInfo.Teams );

            HandleHurtRetreatLogic( Context, pathingCacheData );
            FireteamUtility.CleanUpDisbandedFireteams( BaseInfo.Teams );

            int fireteams = 0, defensiveFireteams = 0;
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
            {
                fireteams++;
                if ( team.DefenseMode )
                    defensiveFireteams++;

                team.NoDeathballing = true;
                team.DeathballingThreshold = BaseInfo.OverkillRatio;

                // Stop them from getting stuck on planets with reinforcement posts and cloaking.
                if ( team.status == FireteamStatus.Attacking && team.TargetPlanet != null )
                {
                    var strengthData = team.TargetPlanet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Hostile];

                    if ( strengthData.StrengthInReinforcementPoints > strengthData.TotalStrength / 2 )
                        FactionUtilityMethods.Instance.FlushUnitsFromReinforcementPoints( team.TargetPlanet, AttachedFaction, Context, 5f );

                    if ( strengthData.CloakedStrength > ((strengthData.TotalStrength - strengthData.StrengthInReinforcementPoints) / 2) )
                        FactionUtilityMethods.Instance.TachyonBlastPlanet( team.TargetPlanet, AttachedFaction, Context, false );
                }
            }

            CreateAnyFireteamsNeeded( Context, fireteams, defensiveFireteams );
            AssignClanlingsToFireteamsAsNeeded( Context );

            foreach ( GameEntity_Squad enclave in BaseInfo.enclaves.DisplaySquads() )
                HandleFireteamCatchupLogic( enclave, Context, pathingCacheData );
            foreach ( GameEntity_Squad clanling in BaseInfo.clanlings.DisplaySquads() )
                HandleFireteamCatchupLogic( clanling, Context, pathingCacheData );

            FireteamUtility.UpdateFireteams( AttachedFaction, Context, pathingCacheData, BaseInfo.Teams, TeamsAimedAtPlanet, tracingBuffer, FInt.One, BaseInfo.territory.GetDisplayList() );
            FireteamUtility.UpdateRegiments( AttachedFaction, Context, pathingCacheData, BaseInfo.Teams, TeamsAimedAtPlanet, tracingBuffer, 1000, true );

            pathingCacheData.ReturnToPool();
        }

        #region DoLongRangePlanning_OnBackgroundNonSimThread_Subclass
        private readonly DictionaryOfLists<Planet, Fireteam> fireteamsAttacking = DictionaryOfLists<Planet, Fireteam>.Create_WillNeverBeGCed( 10, 5, "NeinzulCustodians-fireteamsAttacking" );
        public void HandleEarlyRetreatAndBleedoffLogic( ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathingCacheData )
        {
            fireteamsAttacking.Clear();

            foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
            {
                if ( team.DeepInfo.ShipsInFireteam.Count == 0 )
                    team.Disband( this.AttachedFaction, Context );
                else if ( team.status == FireteamStatus.Attacking )
                {
                    if ( team.TargetPlanet != null && team.DeepInfo.CurrentPlanet == team.TargetPlanet )
                        fireteamsAttacking[team.TargetPlanet].Add( team );
                }
            }
            foreach ( KeyValuePair<Planet, List<Fireteam>> pair in fireteamsAttacking )
            {
                var stanceData = pair.Key.GetPlanetFactionForFaction( AttachedFaction ).DataByStance;

                int hostileStrength = stanceData[FactionStance.Hostile].TotalStrength;
                int friendlyStrength = stanceData[FactionStance.Friendly].TotalStrength;
                int ourStrength = stanceData[FactionStance.Self].TotalStrength;

                if ( hostileStrength < 1000 )
                {
                    // Clean up guard posts.
                    if ( pair.Key.GetFirstMatching( FactionType.AI, SpecialEntityType.GuardPost, false, false ) != null )
                        hostileStrength = 1000;
                }

                for ( int x = 0; x < pair.Value.Count; x++ )
                {
                    Fireteam team = pair.Value[x];
                    team.DeepInfo.BuildShipsLookup( true, null );
                    foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> ships in team.DeepInfo.shipsByPlanet )
                    {
                        if ( ships.Key == pair.Key )
                            continue;

                        for ( int y = 0; y < ships.Value.Count; y++ )
                            ourStrength += (ships.Value[y].GetStrengthPerSquad() * (1 + ships.Value[y].ExtraStackedSquadsInThis)) + ships.Value[y].AdditionalStrengthFromFactions;
                    }
                }

                GameEntity_Squad retreatPoint = GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( pair.Key, Context, pathingCacheData );
                if ( retreatPoint != null )
                {
                    // Run from a losing fight.
                    if ( hostileStrength > (friendlyStrength + ourStrength) * 2 )
                        for ( int x = 0; x < pair.Value.Count; x++ )
                            pair.Value[x].DeepInfo.DisbandAndRetreat( AttachedFaction, Context, pathingCacheData, retreatPoint );
                    else
                        // Bleed off Enclaves from winning fights.
                        while ( ourStrength + friendlyStrength > hostileStrength * 10 && pair.Value.Count > 2 )
                        {
                            ourStrength -= pair.Value[0].DeepInfo.TeamStrength;
                            pair.Value[0].DeepInfo.DisbandAndRetreat( AttachedFaction, Context, pathingCacheData, retreatPoint );
                            pair.Value.RemoveAt( 0 );
                        }
                }
            }
        }
        public void HandleHurtRetreatLogic( ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathingCacheData )
        {
            foreach ( GameEntity_Squad enclave in BaseInfo.enclaves.DisplaySquads() )
            {
                if ( enclave.GetCurrentHullPoints() < enclave.GetMaxHullPoints() * BaseInfo.HullRetreatRatio )
                {
                    Fireteam team = BaseInfo.GetFireteamById( enclave.FireteamId );
                    if ( team == null )
                    {
                        foreach ( GameEntity_Squad hive in BaseInfo.hives.DisplaySquads() )
                        {
                            if ( hive.MinorFactionStackingID == enclave.PrimaryKeyID )
                            {
                                if ( enclave.Planet != hive.Planet && enclave.Orders.GetFinalDestinationOrNull() != hive.Planet )
                                {
                                    PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "NeinzulCustodians_FireteamCatchup", enclave.Planet, hive.Planet, PathingMode.Shortest, Context, pathingCacheData );

                                    if ( pathCache == null || pathCache.PathToReadOnly.Count == 0 )
                                        continue;

                                    GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCFireTeams], GameCommandSource.AnythingElse );
                                    command.RelatedString = "NeinzulCustodians";
                                    command.RelatedEntityIDs.Add( enclave.PrimaryKeyID );

                                    for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                                        command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );

                                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                                }

                                break;
                            }

                        }
                    }
                    else
                        team.DeepInfo.DisbandAndRetreat( AttachedFaction, Context, pathingCacheData, GetNearestHive( enclave.Planet, Context ) );
                }

            }
        }
        public void CreateAnyFireteamsNeeded( ArcenLongTermIntermittentPlanningContext Context, int fireteamCount, int defensiveFireteams )
        {
            foreach ( GameEntity_Squad enclave in BaseInfo.enclaves.DisplaySquads() )
            {
                if ( BaseInfo.GetFireteamById( enclave.FireteamId ) != null )
                    continue; // Already has a team.

                if ( enclave.GetCurrentHullPoints() < enclave.GetMaxHullPoints() )
                    continue; // Skip if hurt.

                Fireteam team = Fireteam.CreateNewWithIDFromList( BaseInfo.Teams );

                team.MyStrengthMultiplierForStrengthCalculation = FInt.One;
                team.EnemyStrengthMultiplierForStrengthCalculation = FInt.One;
                team.StrengthToBringOnline = 500;

                fireteamCount++;
                if ( BaseInfo.EveryXFireteamAsDefense > 0 && BaseInfo.EveryXFireteamAsDefense * defensiveFireteams < fireteamCount )
                {
                    team.DefenseMode = true;
                    defensiveFireteams++;
                }

                team.DeepInfo.AddUnit( enclave );
                team.DeepInfo.IdentifyCurrentPlanet();

                BaseInfo.Teams.AddIfNotAlreadyIn( team );

            }
        }
        public void AssignClanlingsToFireteamsAsNeeded( ArcenLongTermIntermittentPlanningContext Context )
        {
            foreach ( GameEntity_Squad clanling in BaseInfo.clanlings.DisplaySquads() )
            {
                if ( BaseInfo.GetFireteamById( clanling.FireteamId ) != null )
                    continue; // Already in team.

                GameEntity_Squad owner = World_AIW2.Instance.GetEntityByID_Squad( clanling.MinorFactionStackingID );
                if ( owner == null )
                    continue; // Our papi is gone.

                Fireteam team = BaseInfo.GetFireteamById( owner.FireteamId );
                if ( team == null )
                    continue; // Our papi isn't in a team.

                team.DeepInfo.AddUnit( clanling );

            }
        }
        public void HandleFireteamCatchupLogic( GameEntity_Squad entity, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathingCacheData )
        {
            if ( entity == null )
                return;
            Fireteam team = BaseInfo.GetFireteamById( entity.FireteamId );
            if ( team == null )
                return; // No team.

            // Make sure clanlings reinforce their fleet leaders, no matter what.
            // Sometimes they can get stuck.
            switch ( team.status )
            {
                case FireteamStatus.Assembling:
                case FireteamStatus.Staging:
                case FireteamStatus.ReadyToAttack:
                    {
                        if ( entity.Planet == team.LurkPlanet || entity.Orders.GetFinalDestinationOrNull() == team.LurkPlanet )
                            return; // On planet, or heading there already.

                        PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "NeinzulCustodians_FireteamCatchup", entity.Planet, team.LurkPlanet, PathingMode.Shortest, Context, pathingCacheData );

                        if ( pathCache == null || pathCache.PathToReadOnly.Count == 0 )
                            return;

                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCFireTeams], GameCommandSource.AnythingElse );
                        command.RelatedString = "NeinzulCustodians";
                        command.RelatedEntityIDs.Add( entity.PrimaryKeyID );

                        for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                            command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );

                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                    }
                    break;
                case FireteamStatus.Attacking:
                    {
                        if ( entity.Planet == team.TargetPlanet || entity.Orders.GetFinalDestinationOrNull() == team.TargetPlanet )
                            return; // On planet, or heading there already.

                        PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "NeinzulCustodians_FireteamCatchup", entity.Planet, team.TargetPlanet, PathingMode.Shortest, Context, pathingCacheData );

                        if ( pathCache == null || pathCache.PathToReadOnly.Count == 0 )
                            return;

                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCFireTeams], GameCommandSource.AnythingElse );
                        command.RelatedString = "NeinzulCustodians";
                        command.RelatedEntityIDs.Add( entity.PrimaryKeyID );

                        for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                            command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );

                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                    }
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Fireteams
        private readonly DictionaryOfLists<int, Planet> planetsByEnclaveCount = DictionaryOfLists<int, Planet>.Create_WillNeverBeGCed( 10, 5, "NeinzulCustodians-planetsByEnclaveCount" );
        public override void GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass( bool DefenseMode, Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, List<FireteamTarget> PreferredTargets, List<FireteamTarget> FallbackTargets, object TeamObj )
        {
            PreferredTargets.Clear();
            FallbackTargets.Clear();
            planetsByEnclaveCount.Clear();

            List<Planet> planetsInDanger = Planet.GetTemporaryPlanetList( "NeinzulCutstodians-GetFireteamPreferredAndFallbackTargets-planetsInDanger", 10f );
            if ( planetsInDanger == null ) //blocked for teardown/shutdown; bail
                return;
            List<Planet> alliedAssaults = Planet.GetTemporaryPlanetList( "NeinzulCutstodians-GetFireteamPreferredAndFallbackTargets-alliedAssaults", 10f );
            if ( alliedAssaults == null ) //blocked for teardown/shutdown; bail
            {
                Planet.ReleaseTemporaryPlanetList( planetsInDanger );
                return;
            }
            List<Planet> planetsThreatened = Planet.GetTemporaryPlanetList( "NeinzulCutstodians-GetFireteamPreferredAndFallbackTargets-planetsThreatened", 10f );
            if ( planetsThreatened == null ) //blocked for teardown/shutdown; bail
            {
                Planet.ReleaseTemporaryPlanetList( planetsInDanger );
                Planet.ReleaseTemporaryPlanetList( alliedAssaults );
                return;
            }
            List<Planet> planetsToAttack = Planet.GetTemporaryPlanetList( "NeinzulCutstodians-GetFireteamPreferredAndFallbackTargets-planetsToAttack", 10f );
            if ( planetsToAttack == null ) //blocked for teardown/shutdown; bail
            {
                Planet.ReleaseTemporaryPlanetList( planetsInDanger );
                Planet.ReleaseTemporaryPlanetList( alliedAssaults );
                Planet.ReleaseTemporaryPlanetList( planetsThreatened );
                return;
            }

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                int hops = planet.GetHopsTo( GetNearestTerritory( planet, Context ) );

                int enclaveStrength = planet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Self].TotalStrength;
                int friendlyStrength = planet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Friendly].TotalStrength;
                int hostileStrength = planet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Hostile].TotalStrength;

                if ( hostileStrength < 1000 )
                {
                    // Clean up guard posts.
                    if ( planet.GetFirstMatching( FactionType.AI, SpecialEntityType.GuardPost, false, false ) != null )
                        hostileStrength = 1000;
                }

                if ( friendlyStrength > 2500 && hostileStrength > 2500 && enclaveStrength + friendlyStrength < hostileStrength * BaseInfo.OverkillRatio )
                {
                    if ( hops <= BaseInfo.AttackHops )
                        alliedAssaults.Add( planet );
                }
                if ( hops == 0 )
                {
                    if ( hostileStrength > 2500 && enclaveStrength + friendlyStrength < hostileStrength * BaseInfo.OverkillRatio )
                        planetsInDanger.Add( planet );

                    int enclaveOnPlanet = 0;

                    bool isBorder = false;
                    foreach ( Planet adjPlanet in planet.LinkedNeighbors( false ) )
                    {
                        if ( !BaseInfo.territory.DisplayContains( adjPlanet ) )
                        {
                            isBorder = true;
                            break;
                        }
                    }

                    if ( isBorder )
                    {
                        foreach ( GameEntity_Squad entity in planet.GetPlanetFactionForFaction( AttachedFaction ).Entities.Squads( BaseInfo.EnclaveTag ) )
                        {
                            if ( entity.PlanetFaction.Faction == AttachedFaction )
                                enclaveOnPlanet++;
                        }

                        planetsByEnclaveCount[enclaveOnPlanet].Add( planet ); //this will never fail, it handles this internally
                    }
                }
                else if ( hops == 1 && hostileStrength > 500 && enclaveStrength + friendlyStrength < hostileStrength * BaseInfo.OverkillRatio )
                {
                    planetsThreatened.Add( planet );
                }
                else if ( hops <= BaseInfo.AttackHops && hostileStrength > 500 && Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForFireteam, planet, false, out short _ ) < 1000 && enclaveStrength + friendlyStrength < hostileStrength * BaseInfo.OverkillRatio )
                {
                    planetsToAttack.Add( planet );
                }
            }

            var temp = planetsByEnclaveCount.SortIntoList( ( pair1, pair2 ) => pair1.Key.CompareTo( pair2.Key ) );

            if ( DefenseMode )
            {
                if ( planetsInDanger.Count > 0 )
                {
                    for ( int x = 0; x < planetsInDanger.Count; x++ )
                        PreferredTargets.Add( new FireteamTarget( planetsInDanger[x] ) );
                }
                if ( planetsThreatened.Count > 0 )
                {
                    if ( PreferredTargets.Count == 0 )
                        for ( int x = 0; x < planetsThreatened.Count; x++ )
                            PreferredTargets.Add( new FireteamTarget( planetsThreatened[x] ) );
                    else
                        for ( int x = 0; x < planetsThreatened.Count; x++ )
                            FallbackTargets.Add( new FireteamTarget( planetsThreatened[x] ) );
                }
                if ( temp.Count > 0 )
                {
                    if ( PreferredTargets.Count == 0 )
                        for ( int x = 0; x < temp[0].Value.Count; x++ )
                            PreferredTargets.Add( new FireteamTarget( temp[0].Value[x] ) );
                    else if ( FallbackTargets.Count == 0 )
                        for ( int x = 0; x < temp[0].Value.Count; x++ )
                            FallbackTargets.Add( new FireteamTarget( temp[0].Value[x] ) );
                }
            }
            else
            {
                if ( planetsInDanger.Count > 0 )
                {
                    for ( int x = 0; x < planetsInDanger.Count; x++ )
                        PreferredTargets.Add( new FireteamTarget( planetsInDanger[x] ) );
                }
                if ( alliedAssaults.Count > 0 )
                {
                    if ( PreferredTargets.Count == 0 )
                        for ( int x = 0; x < alliedAssaults.Count; x++ )
                            PreferredTargets.Add( new FireteamTarget( alliedAssaults[x] ) );
                    else
                        for ( int x = 0; x < alliedAssaults.Count; x++ )
                            FallbackTargets.Add( new FireteamTarget( alliedAssaults[x] ) );
                }
                if ( planetsThreatened.Count > 0 )
                {
                    if ( PreferredTargets.Count == 0 )
                        for ( int x = 0; x < planetsThreatened.Count; x++ )
                            PreferredTargets.Add( new FireteamTarget( planetsThreatened[x] ) );
                    else
                        for ( int x = 0; x < planetsThreatened.Count; x++ )
                            FallbackTargets.Add( new FireteamTarget( planetsThreatened[x] ) );
                }
                if ( planetsToAttack.Count > 0 )
                {
                    if ( PreferredTargets.Count == 0 )
                        for ( int x = 0; x < planetsToAttack.Count; x++ )
                            PreferredTargets.Add( new FireteamTarget( planetsToAttack[x] ) );
                    else if ( FallbackTargets.Count == 0 )
                        for ( int x = 0; x < planetsToAttack.Count; x++ )
                            FallbackTargets.Add( new FireteamTarget( planetsToAttack[x] ) );
                }
                if ( temp.Count > 0 )
                {
                    if ( PreferredTargets.Count == 0 )
                        for ( int x = 0; x < temp[0].Value.Count; x++ )
                            PreferredTargets.Add( new FireteamTarget( temp[0].Value[x] ) );
                    else if ( FallbackTargets.Count == 0 )
                        for ( int x = 0; x < temp[0].Value.Count; x++ )
                            FallbackTargets.Add( new FireteamTarget( temp[0].Value[x] ) );
                }
            }

            Planet.ReleaseTemporaryPlanetList( planetsInDanger );
            Planet.ReleaseTemporaryPlanetList( alliedAssaults );
            Planet.ReleaseTemporaryPlanetList( planetsThreatened );
            Planet.ReleaseTemporaryPlanetList( planetsToAttack );

            foreach ( KeyValuePair<int, List<Planet>> pair in planetsByEnclaveCount )
            {
                Planet.ReleaseTemporaryPlanetList( pair.Value );
            }
            planetsByEnclaveCount.Clear();
        }

        private GameEntity_Squad GetNearestHive( Planet planet, ArcenLongTermIntermittentPlanningContext Context )
        {
            int lowestHops = 999;
            List<SafeSquadWrapper> potentialHives = GameEntity_Squad.GetTemporarySquadList( "NeinzulCustodians-GetNearestTerritoryPlanet-potentialHives", 10f );
            if ( potentialHives == null ) //blocked for teardown/shutdown; bail
                return null;
            foreach ( GameEntity_Squad workingHive in BaseInfo.hives.DisplaySquads() )
            {
                if ( workingHive == null )
                    continue;
                int workingHops = planet.GetHopsTo( workingHive.Planet );
                if ( workingHops > lowestHops )
                    continue;

                if ( workingHops < lowestHops )
                {
                    lowestHops = workingHops;
                    potentialHives.Clear();
                }

                potentialHives.Add( workingHive );

            }
            GameEntity_Squad chosenHive = null;
            if ( potentialHives.Count > 0 )
            {
                chosenHive = potentialHives[Context.RandomToUse.Next( potentialHives.Count )].GetSquad();
            }
            GameEntity_Squad.ReleaseTemporarySquadList( potentialHives );
            return chosenHive;
        }
        private Planet GetNearestTerritory( Planet planet, ArcenLongTermIntermittentPlanningContext Context )
        {
            GameEntity_Squad hive = GetNearestHive( planet, Context );
            if ( hive != null )
                return hive.Planet;
            return null;
        }

        public override Planet GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass( Planet TargetPlanet, int TeamStrength, Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            return GetNearestTerritory( TargetPlanet, Context );
        }

        public override GameEntity_Squad GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            return GetNearestHive( CurrentPlanetForFireteam, Context );
        }
        #endregion

        public override void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        {
            if ( entity.TypeData.GetHasTag( BaseInfo.HiveTag ) )
            {
                // Hive died, purge the Enclave attached to it.
                GameEntity_Squad entityPair = World_AIW2.Instance.GetEntityByID_Squad( entity.MinorFactionStackingID );
                if ( entityPair != null )
                    entityPair.Die( Context, true );
            }
            else if ( entity.TypeData.GetHasTag( BaseInfo.EnclaveTag ) )
            {
                // Enclave died, purge the Hive attached to it.
                foreach ( GameEntity_Squad hive in BaseInfo.hives.DisplaySquads() )
                {
                    if ( hive.MinorFactionStackingID == entity.PrimaryKeyID )
                    {
                        hive.Die( Context, true );
                        break;
                    }

                }
            }
        }
    }
}
