using System;
using Arcen.AIW2.Core;
using Arcen.Universal;


namespace Arcen.AIW2.External
{
    public sealed class SplinteringSpireFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        public SplinteringSpireFactionBaseInfo BaseInfo;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<SplinteringSpireFactionBaseInfo>();
        }

        public readonly ProtectedValDictionary<Planet, FireteamRegiment> TeamsAimedAtPlanet = ProtectedValDictionary<Planet, FireteamRegiment>.Create_WillNeverBeGCed( 100, "SplinteringSpireDeepInfo-TeamsAimedAtPlanet" );

        protected override void Cleanup()
        {
            BaseInfo = null;
        }

        private int FireteamStrengthToBringOnline => Math.Max( 5000, BaseInfo.GetMaxStrength( null ).GetNearestIntPreferringLower() / 2 );
        private const string VGTag = "VengeanceGenerator";

        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType ) { }
        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType ) { }

        protected override int MinimumSecondsBetweenLongRangePlannings => 2;

        #region Stage3
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            SpawnCollectorsIfNeeded( Context );
            DecayCollectorsIfNeeded( Context );
            ConvertControlToCollectionPoints( Context );

            PlayerInteraction( Context );
            DarkSpireInteraction( Context );

            DespawnDerelictsAndPickWinnersAsNeeded( Context );

            SplinteringSpireFactionBaseInfo.CoalitionAwakenReason latestAwakeningReason = GetAwakeningStatus();
            if ( latestAwakeningReason > SplinteringSpireFactionBaseInfo.CoalitionAwakenReason.NotAwakened )
            {
                if ( !BaseInfo.SentAwakeningJournal )
                    SendAwakeningJournal( latestAwakeningReason, Context );
                else if ( BaseInfo.CurrentCoalitionAwakeningReason < SplinteringSpireFactionBaseInfo.CoalitionAwakenReason.DarkAlliance && latestAwakeningReason == SplinteringSpireFactionBaseInfo.CoalitionAwakenReason.DarkAlliance )
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_SplinteringSpire_TheCoalitionRebrand_DarkAlliance", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );

                if ( BaseInfo.CurrentCoalitionAwakeningReason < latestAwakeningReason )
                    BaseInfo.CurrentCoalitionAwakeningReason = latestAwakeningReason;

                UpdateCoalitionBudgets( Context );
                SpawnUnitsIfNeeded( Context );
            }
        }

        #region Awakening
        public bool CoalitionIsAwakenedFromDarkAlliance => ((DarkZenithFactionBaseInfo.Instance?.Allegiance ?? "none") == "黑暗同盟") && ((DarkZenithFactionBaseInfo.Instance?.Epistyles.GetDisplayList()?.Count ?? 0) > 0) && ((DarkZenithFactionBaseInfo.Instance?.WarpingInEpistyles.GetDisplayList()?.Count ?? 0) > 0);
        public bool CoalitionIsAwakenedFromConquestMode => DarkSpireFactionBaseInfo.Instance?.ConquestMode ?? false;
        public bool CoalitionIsAwakenedFromTiime => World_AIW2.Instance.GameSecond >= BaseInfo.CoalitionLateAwakeningSecond;

        private SplinteringSpireFactionBaseInfo.CoalitionAwakenReason GetAwakeningStatus()
        {
            if ( CoalitionIsAwakenedFromDarkAlliance )
                return SplinteringSpireFactionBaseInfo.CoalitionAwakenReason.DarkAlliance;
            else if ( CoalitionIsAwakenedFromConquestMode )
                return SplinteringSpireFactionBaseInfo.CoalitionAwakenReason.DarkSpire;
            else if ( CoalitionIsAwakenedFromTiime )
                return SplinteringSpireFactionBaseInfo.CoalitionAwakenReason.Time;
            else
                return SplinteringSpireFactionBaseInfo.CoalitionAwakenReason.NotAwakened;

        }
        private void SendAwakeningJournal( SplinteringSpireFactionBaseInfo.CoalitionAwakenReason reason, ArcenHostOnlySimContext context )
        {
            switch ( reason )
            {
                case SplinteringSpireFactionBaseInfo.CoalitionAwakenReason.Time:
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_SplinteringSpire_TheCoalitionArrives_LateToTheParty", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    break;
                case SplinteringSpireFactionBaseInfo.CoalitionAwakenReason.DarkSpire:
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_SplinteringSpire_TheCoalitionArrives_ConquestMode", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    break;
                case SplinteringSpireFactionBaseInfo.CoalitionAwakenReason.DarkAlliance:
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_SplinteringSpire_TheCoalitionArrives_DarkAlliance", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    break;
                default:
                    break;
            }
            BaseInfo.SentAwakeningJournal = true;
        }
        #endregion

        #region Collectors and Collection Points
        private void SpawnCollectorsIfNeeded( ArcenHostOnlySimContext Context )
        {
            int maxSpawnsPerDerelict = 25;
            foreach ( GameEntity_Squad derelict in BaseInfo.Derelicts.DisplaySquads() )
            {
                int toSpawn = maxSpawnsPerDerelict;
                if ( BaseInfo.CollectorsAssignedByPlanet.DisplayContainsKey( derelict.Planet ) )
                    toSpawn -= BaseInfo.CollectorsAssignedByPlanet.Display[derelict.Planet];

                if ( toSpawn == 0 )
                    break;

                foreach ( Faction workingFaction in BaseInfo.ActiveCoalitionFactions.GetDisplayList() )
                {
                    if ( toSpawn == 0 )
                        break;

                    GameEntity_Squad sphere = workingFaction.GetFirstMatching( SphereFactionBaseInfo.Tag_DysonSphere, true, true );
                    if ( sphere == null || sphere.GetSecondsSinceCreation() % 30 != 0 )
                        continue;

                    GameEntityTypeData sphereType = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, BaseInfo.GetCollectorTag( workingFaction ) );
                    GameEntity_Squad collector = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( sphere.PlanetFaction, sphereType, 1, sphere.PlanetFaction.FleetUsedAtPlanet, 0, sphere.WorldLocation, Context, "SplinteringSpireCollectorSpawn" );

                    if ( collector != null )
                    {
                        collector.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                        collector.CurrentStateOfMatter = StateOfMatterTypeDataTable.Instance.GetRowByName( SplinteringSpireFactionBaseInfo.StateOfMatter );
                        collector.GameSecondWillExitStateOfMatter = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.DurationOfDerelicts; // Last the whole event.
                        collector.MinorFactionStackingID = derelict.PrimaryKeyID; // Assign to the derelict.
                        toSpawn--;
                    }
                }
            }
        }

        private void DecayCollectorsIfNeeded( ArcenHostOnlySimContext Context )
        {
            // We know Debris doesn't exist at this point; so rapidly decay any left over collectors.
            foreach ( GameEntity_Squad entity in BaseInfo.CollectorsToDecay.DisplaySquads() )
            {
                entity.TakeHullRepair( -(entity.GetMaxHullPoints() / 5) );
            }
        }

        private void ConvertControlToCollectionPoints( ArcenHostOnlySimContext context )
        {
            BaseInfo.TotalCollectedFromDerelictPlanetByFaction.ClearConstructionDictForStartingConstruction();
            foreach ( GameEntity_Squad workingDerelict in BaseInfo.Derelicts.DisplaySquads() )
            {
                Planet workingPlanet = workingDerelict.Planet;
                if ( workingPlanet == null )
                    continue;

                foreach ( Faction workingFaction in BaseInfo.ActiveCoalitionFactions.GetDisplayList() )
                {
                    BaseInfo.GetTotalCollectedForFactionOnPlanet( workingFaction, workingPlanet );
                    BaseInfo.TotalCollectedFromDerelictPlanetByFaction.SetToInnerDictConstruction( workingFaction, workingPlanet,
                        BaseInfo.GetTotalCollectedForFactionOnPlanet( workingFaction, workingPlanet ) +
                        (BaseInfo.GetControlForFactionOnPlanet( workingFaction, workingPlanet ) * 100).GetNearestIntPreferringHigher() );
                }
            }
            BaseInfo.TotalCollectedFromDerelictPlanetByFaction.SwitchConstructionToDisplay();
        }

        private void DespawnDerelictsAndPickWinnersAsNeeded( ArcenHostOnlySimContext context )
        {
            foreach ( GameEntity_Squad derelict in BaseInfo.Derelicts.DisplaySquads() )
            {
                Planet derelictPlanet = derelict.Planet;
                if ( derelictPlanet == null )
                    continue;

                if ( derelict.GetSecondsSinceCreation() < BaseInfo.Difficulty.DurationOfDerelicts )
                    continue; // Not yet it's time.

                // Figure out the winner.
                Faction winningFaction = null;
                int winningPoints = 0;
                foreach ( Faction workingFaction in BaseInfo.ActiveCoalitionFactions.GetDisplayList() )
                {
                    if ( BaseInfo.TotalCollectedFromDerelictPlanetByFaction.CheckIfAlreadyHasKeyDisplay( workingFaction ) &&
                    BaseInfo.TotalCollectedFromDerelictPlanetByFaction.Display[workingFaction].ContainsKey( derelictPlanet ) )
                    {
                        int points = BaseInfo.TotalCollectedFromDerelictPlanetByFaction.Display[workingFaction][derelictPlanet];
                        if ( points > winningPoints )
                        {
                            winningFaction = workingFaction;
                            winningPoints = points;
                        }
                        // Remove the points.
                        BaseInfo.ResetTotalCollectedForFactionOnPlanet( workingFaction, derelictPlanet );
                    }
                }

                // Reward the winner.
                if ( winningFaction != null )
                    BaseInfo.UnclaimedVictoryPoints.AddOrUpdate( winningFaction, 1, ( existingFaction, existingPoints ) => { return (short)(existingPoints + 1); } );

                // Despawn the derelict.
                derelict.Despawn( context, true, InstancedRendererDeactivationReason.IFinishedMyJob );
            }
        }
        #endregion

        #region Other Faction Interactions
        private void PlayerInteraction( ArcenHostOnlySimContext context )
        {
            foreach ( GameEntity_Squad derelict in BaseInfo.Derelicts.DisplaySquads() )
            {
                Planet derelictPlanet = derelict.Planet;
                if ( derelictPlanet == null )
                    continue;

                foreach ( Faction workingFaction in World_AIW2.Instance.Factions )
                {
                    if ( workingFaction.Type != FactionType.Player )
                        continue;

                    PlanetFaction pFaction = derelictPlanet.GetPlanetFactionForFaction( workingFaction );

                    // Give some Sphere Bastions to battlestations on the planet.
                    foreach ( GameEntity_Squad fleetLeader in pFaction.Entities.Squads( EntityRollupType.FleetLeaders ) )
                    {
                        if ( !(fleetLeader?.TypeData.IsBattlestation ?? false) )
                            continue;

                        fleetLeader.FleetMembership.Fleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( GameEntityTypeDataTable.Instance.GetRandomRowWithTag( context, SplinteringSpireFactionBaseInfo.Tag_SphereBastion ) ).ExplicitBaseSquadCap = 2;
                    }

                    // Bolster any Sphere Bastions on derelict planets to shoot at Collectors
                    foreach ( GameEntity_Squad bastion in pFaction.Entities.Squads( SplinteringSpireFactionBaseInfo.Tag_SphereBastion ) )
                    {
                        if ( bastion == null )
                            continue;

                        // Last a few seconds, will run out when the derelict is no longer here.
                        bastion.CurrentStateOfMatter = StateOfMatterTypeDataTable.Instance.GetRowByName( SplinteringSpireFactionBaseInfo.StateOfMatter );
                        bastion.GameSecondWillExitStateOfMatter = World_AIW2.Instance.GameSecond + 10;
                    }
                }
            }

            // Find any Sphere Bastions owned by players on Derelict planets, and let them shoot at collectors.
            foreach ( Faction workingFaction in World_AIW2.Instance.Factions )
            {
                if ( workingFaction.Type != FactionType.Player )
                    continue;

                foreach ( GameEntity_Squad bastion in workingFaction.Squads( SplinteringSpireFactionBaseInfo.Tag_SphereBastion ) )
                {
                    if ( bastion == null )
                        continue;

                    if ( bastion.Planet.GetPlanetFactionForFaction( AttachedFaction ).Entities.GetFirstMatching( SplinteringSpireFactionBaseInfo.Tag_Derelict, true, true ) != null )
                    {
                        // On a derelict planet. Shoot at collectors.
                        bastion.CurrentStateOfMatter = StateOfMatterTypeDataTable.Instance.GetRowByName( SplinteringSpireFactionBaseInfo.StateOfMatter );
                        bastion.GameSecondWillExitStateOfMatter = World_AIW2.Instance.GameSecond + 10; // Last a few seconds, will run out when the derelict is no longer here.
                    }
                }
            }
        }

        private void DarkSpireInteraction( ArcenHostOnlySimContext context )
        {
            foreach ( GameEntity_Squad VG in DarkSpireFactionBaseInfo.Instance.AttachedFaction.Squads( VGTag ) )
            {
                if ( BaseInfo.GameSecondControlWasEstablishedOnPlanet.TryGetValue( VG.Planet, out int GameSecond ) )
                {
                    if ( World_AIW2.Instance.GameSecond - GameSecond < 30 )
                        continue; // Wait a bit before checking.

                    if ( (World_AIW2.Instance.GameSecond - GameSecond) % BaseInfo.Difficulty.DarkSpireResponseIntervalWhenBeingDismantled == 0 )
                    {
                        DarkSpireFactionBaseInfo dsdata = FactionUtilityMethods.Instance.GetDarkSpireFactionBaseInfo();
                        DarkSpirePerPlanet perPlanet = dsdata.PerPlanet.GetHasKey( VG.Planet.Index ) ? dsdata.PerPlanet[VG.Planet.Index] : null;
                        if ( perPlanet != null )
                            perPlanet.NetEnergy += perPlanet.EnergyThresholdForAttack * BaseInfo.Difficulty.DarkSpireDismantleIntervalResponseMultiplier;
                    }

                    if ( GameSecond > 60 && World_AIW2.Instance.GameSecond - GameSecond > BaseInfo.Difficulty.SecondsToDismantleDarkSpireVG )
                    {
                        // Blow up the dark spire VG, and sprinkle it's debris on nearby planets.
                        List<Planet> potentialPlanets = Planet.GetTemporaryPlanetList( "SplinteringSpireDeepInfo-DarkSpireInteraction-potentialPlanets", 10f );
                        if ( potentialPlanets == null ) //blocked for teardown/shutdown; bail
                            return;

                        foreach ( Planet.PlanetAtHopDistance _phd in VG.Planet.PlanetsWithinXHops_NoFilters( 3 ) )
                        {
                            Planet workingPlanet = _phd.Planet;
                            if ( workingPlanet.GetPlanetFactionForFaction( VG.PlanetFaction.Faction ).Entities.GetFirstMatching( VGTag, true, true ) != null )
                                continue; // VG planet.

                            potentialPlanets.Add( workingPlanet );
                        }

                        int toSpawn = 4;
                        if ( potentialPlanets.Count < 4 )
                            toSpawn = potentialPlanets.Count;

                        int startingIndex = 0;
                        if ( potentialPlanets.Count > 4 )
                            startingIndex = context.RandomToUse.Next( 0, potentialPlanets.Count - 3 );

                        for ( int x = startingIndex; x < potentialPlanets.Count && toSpawn > 0; x++, toSpawn-- )
                            potentialPlanets[x].Mapgen_SeedEntity( context, AttachedFaction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( context, SplinteringSpireFactionBaseInfo.Tag_Derelict ), PlanetSeedingZone.OuterSystem );

                        Planet.ReleaseTemporaryPlanetList( potentialPlanets );

                        World_AIW2.Instance.QueueChatMessageOrCommand( $"反黑暗尖塔联盟已成功炸毁了 {VG.Planet.Name} 上的复仇发生器。废弃的黑暗尖塔碎片已散落到邻近星系。黑暗尖塔正在做出严厉回应。", ChatType.LogToCentralChat, null );
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_SplinteringSpire_TheDismantlingSucceeds", string.Empty, AttachedFaction, null, VG.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DarkSpireConquestModeFromCoalition", string.Empty, AttachedFaction, null, VG.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );

                        // Good riddance VG. Nobody likes you.
                        BaseInfo.GameSecondControlWasEstablishedOnPlanet.TryRemove( VG.Planet, out int unused );
                        VG.Despawn( context, true, InstancedRendererDeactivationReason.IFinishedMyJob );

                        // Now they get REALLY angry.
                        FInt originalValue = DarkSpireFactionBaseInfo.VengeanceStrikeEnergyMultiplier;
                        DarkSpireFactionBaseInfo.VengeanceStrikeEnergyMultiplier *= BaseInfo.Difficulty.DarkSpireDismantleFinalResponseMultiplier;
                        DarkSpireFactionBaseInfo.Instance.PerformVengeanceStrike();
                        DarkSpireFactionBaseInfo.VengeanceStrikeEnergyMultiplier = originalValue;

                        // If they weren't for any other reason, now they are.
                        DarkSpireFactionBaseInfo.Instance.ConquestMode = true;
                    }
                }
            }
        }
        #endregion

        #region Coalition Budget and Spawning
        private void UpdateCoalitionBudgets( ArcenHostOnlySimContext Context )
        {
            foreach ( Faction workingFaction in BaseInfo.ActiveCoalitionFactions.GetDisplayList() )
            {
                SphereFactionBaseInfo sphereInfo = workingFaction.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();

                if ( BaseInfo.CoalitionStrengthByFactionIndex.DisplayContainsKey( workingFaction.FactionIndex ) &&
                BaseInfo.CoalitionStrengthByFactionIndex.Display[workingFaction.FactionIndex] >= BaseInfo.GetMaxStrength( sphereInfo ) )
                {
                    // We're over strength capacity. Empty our budget.
                    BaseInfo.StartOrClearStoredBudgetForFaction( workingFaction );
                }
                else
                {
                    // We're not yet at max strength, budget away.
                    FInt perSecondBudget = BaseInfo.GetPerSecondBudget( sphereInfo ) / 6;

                    BaseInfo.AddToStoredBudgetForTierForFaction( workingFaction, 0, perSecondBudget * 3 );
                    BaseInfo.AddToStoredBudgetForTierForFaction( workingFaction, 1, perSecondBudget * 2 );
                    BaseInfo.AddToStoredBudgetForTierForFaction( workingFaction, 2, perSecondBudget );
                }
            }
        }
        private void SpawnUnitsIfNeeded( ArcenHostOnlySimContext Context )
        {
            foreach ( Faction workingFaction in BaseInfo.ActiveCoalitionFactions.GetDisplayList() )
            {
                SphereFactionBaseInfo baseInfo = workingFaction?.TryGetExternalBaseInfoAs<SphereFactionBaseInfo>();
                if ( BaseInfo == null )
                    continue;

                GameEntity_Squad spawnPointBase = workingFaction.GetFirstMatching( SplinteringSpireFactionBaseInfo.Tag_CoalitionSpawner, true, true );
                if ( spawnPointBase == null )
                    continue;

                PlanetFaction pFaction = spawnPointBase.Planet.GetPlanetFactionForFaction( AttachedFaction );

                for ( byte x = 0; x < SplinteringSpireFactionBaseInfo.MaxUnitTier; x++ )
                {
                    FInt currentBudget = BaseInfo.GetStoredBudgetForTierForFaction( workingFaction, x );
                    GameEntityTypeData entityData = BaseInfo.GetCoalitionUnitForFactionForTier( workingFaction, x, Context );

                    int cost = Math.Max( entityData.CostForAIToPurchase, entityData.GetForMark( pFaction.Faction.GetGlobalMarkLevelForShipLine( entityData ) ).StrengthPerSquad_CalculatedWithNullFleetMembership );
                    int canAfford = (currentBudget / cost).GetNearestIntPreferringLower();

                    if ( canAfford < 1 )
                        continue;

                    int squadsToSpawn, stacksPerSquad = 0, extraUnitsToStack = 0;
                    if ( canAfford <= 10 )
                        squadsToSpawn = canAfford;
                    else
                    {
                        squadsToSpawn = 10;
                        stacksPerSquad = (canAfford - 10) / 10;
                        extraUnitsToStack = (canAfford - 10) % 10;
                    }

                    BaseInfo.OverrideStoredBudgetForTierForFaction( workingFaction, x, -(cost * canAfford) );

                    // Spawn them in.
                    for ( byte y = 0; y < squadsToSpawn; y++ )
                    {
                        ArcenPoint spawnPoint = spawnPointBase.WorldLocation.GetRandomPointWithinDistance( Context.RandomToUse, 0, 5000 );

                        GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, pFaction.Faction.GetGlobalMarkLevelForShipLine( entityData ),
                            pFaction.FleetUsedAtPlanet, 0, spawnPoint, Context, "SplinteringSpire-CoalitionSpawn" );

                        if ( entity != null )
                        {
                            entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );

                            if ( stacksPerSquad > 0 )
                                entity.AddOrSetExtraStackedSquadsInThis( (short)stacksPerSquad, false );
                            if ( extraUnitsToStack > 0 )
                            {
                                extraUnitsToStack--;
                                entity.AddOrSetExtraStackedSquadsInThis( 1, false );
                            }
                        }
                    }
                }
            }
        }
        #endregion
        #endregion

        #region Movement Planning
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            HandleCollectorMovement( Context );
            HandleCoalitionMovement( Context );
        }

        private void HandleCollectorMovement( ArcenLongTermIntermittentPlanningContext Context )
        {
            foreach ( GameEntity_Squad collector in World_AIW2.Instance.Squads( SplinteringSpireFactionBaseInfo.Tag_Collector ) )
            {
                if ( collector == null )
                    continue;

                if ( collector.Orders.GetHasAnyOrdersOfType( EntityOrderType.Wormhole ) )
                    continue;

                Planet targetPlanet = World_AIW2.Instance.GetEntityByID_Squad( collector.MinorFactionStackingID )?.Planet;
                if ( targetPlanet == null || collector.Planet == targetPlanet )
                    continue;

                Planet nextPlanet = null;
                foreach ( Planet workingPlanet in collector.Planet.LinkedNeighbors( false ) )
                {
                    if ( nextPlanet == null || workingPlanet.GetHopsTo( targetPlanet ) < nextPlanet.GetHopsTo( targetPlanet ) )
                        nextPlanet = workingPlanet;
                }

                GameCommand wormholeCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse );
                wormholeCommand.RelatedFactionIndex = collector.PlanetFaction.Faction.FactionIndex;
                wormholeCommand.RelatedEntityIDs.Add( collector.PrimaryKeyID );
                wormholeCommand.RelatedIntegers.Add( nextPlanet.Index );
                World_AIW2.Instance.QueueGameCommand( AttachedFaction, wormholeCommand, false );
            }
        }

        private void HandleCoalitionMovement( ArcenLongTermIntermittentPlanningContext Context )
        {
            TeamsAimedAtPlanet.Clear();

            // Fireteams Logic
            AttachedFaction.MinFireteamStrength = FireteamStrengthToBringOnline;
            AttachedFaction.MaxFireteamStrength = FireteamStrengthToBringOnline * 10;

            FireteamsSpecificLogic( Context );

            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            ArcenCharacterBuffer tracingBuffer = Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam ) ? ArcenCharacterBuffer.GetFromPoolOrCreate( "SplinteringSpireHandleCoalitionMovement" ) : null;
            FireteamUtility.UpdateFireteams( AttachedFaction, Context, pathingCacheData, BaseInfo.Teams, TeamsAimedAtPlanet, tracingBuffer, FInt.One / 2 );
            FireteamUtility.UpdateRegiments( AttachedFaction, Context, pathingCacheData, BaseInfo.Teams, TeamsAimedAtPlanet, tracingBuffer, AttachedFaction.MinFireteamStrength, true );

            pathingCacheData.ReturnToPool();
            if ( tracingBuffer != null )
                tracingBuffer.ReturnToPool();
        }

        #region FireteamsSpecificLogic
        private void FireteamsSpecificLogic( ArcenLongTermIntermittentPlanningContext context )
        {
            DictionaryOfLists<Planet, SafeSquadWrapper> LooseUnitsPerPlanet = Planet.GetTemporaryPlanetDictOfSquadLists( "SplinteringSpireDeepInfo-HandleLooseUnits", 10f );
            if ( LooseUnitsPerPlanet == null ) //blocked for teardown/shutdown; bail
                return;

            BuildLooseUnitsPerPlanetDictionary( LooseUnitsPerPlanet );

            foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in LooseUnitsPerPlanet )
            {
                if ( pair.Value.Count == 0 )
                    continue;
                Fireteam fireteam = TryFindExistingNearbyFireteam( pair );

                bool isAssemblingPlanet = pair.Key.GetFirstMatching( FactionType.SpecialFaction, SplinteringSpireFactionBaseInfo.Tag_CoalitionSpawner, true, true ) != null;

                // If we found a team, or we're on a planet that makes new teams, join a team.
                if ( fireteam != null || isAssemblingPlanet )
                {
                    // If no nearby Fireteam, make a new one.
                    if ( fireteam == null )
                        fireteam = CreateNewFireteam();

                    if ( fireteam != null )
                    {
                        fireteam.StrengthToBringOnline = FireteamStrengthToBringOnline;

                        AssignUnitsToFireteam( pair, fireteam );

                        fireteam.DeepInfo.IdentifyCurrentPlanet();
                        BaseInfo.Teams.AddIfNotAlreadyIn( fireteam );
                    }
                }
                else
                    // Fall back to the nearest one.
                    FallbackLogic( pair );
            }

            Planet.ReleaseTemporaryPlanetDictOfSquadLists( LooseUnitsPerPlanet );
        }

        private void BuildLooseUnitsPerPlanetDictionary( DictionaryOfLists<Planet, SafeSquadWrapper> LooseUnitsPerPlanet )
        {
            foreach ( GameEntity_Squad unit in AttachedFaction.Squads( SplinteringSpireFactionBaseInfo.Tag_CoalitionUnit ) )
            {
                if ( unit == null )
                    continue;

                if ( unit.FireteamId < 0 )
                    LooseUnitsPerPlanet.AddToList( unit.Planet, SafeSquadWrapper.Create( unit ) );
                else
                {
                    Fireteam team = FireteamBaseUtility.GetFireteamById( BaseInfo.Teams, unit.FireteamId );
                    if ( team == null )
                        unit.FireteamId = -1; //something happened to the fireteam, so lets find a new one next LRP stage
                    else if ( !team.DeepInfo.ShipsInFireteam.Contains( unit ) )
                        team.DeepInfo.AddUnit( unit ); //not in the team for some reason, join it now
                }
            }
        }

        private Fireteam TryFindExistingNearbyFireteam( KeyValuePair<Planet, List<SafeSquadWrapper>> pair )
        {
            Planet currentPlanet = pair.Key;
            Fireteam fireteam = null;
            // First, try to find a fireteam nearby that is accepting units.
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
            {
                if ( team != null && team.DeepInfo.CurrentPlanet != null && team.DeepInfo.CurrentPlanet.GetHopsTo( currentPlanet ) <= 2 )
                {
                    fireteam = team;
                    break;
                }
            }
            return fireteam;
        }

        private Fireteam CreateNewFireteam()
        {
            Fireteam fireteam = Fireteam.CreateNewWithIDFromList( BaseInfo.Teams );
            if ( fireteam != null )
            {
                fireteam.StrengthToBringOnline = FireteamStrengthToBringOnline;
                fireteam.NoDeathballing = false;
                fireteam.DeathballingThreshold = 100;
                fireteam.status = FireteamStatus.Assembling;
            }

            return fireteam;
        }

        private void AssignUnitsToFireteam( KeyValuePair<Planet, List<SafeSquadWrapper>> pair, Fireteam fireteam )
        {
            pair.Value.ForEach( unit =>
            {
                if ( unit.GetSquad() == null )
                    return; // We died before getting here due to long range planning being a background thread.

                unit.GetSquad().FireteamId = fireteam.FireTeamID;

                if ( !fireteam.DeepInfo.ShipsInFireteam.Contains( unit ) )
                    fireteam.DeepInfo.AddUnit( unit.GetSquad() );
            } );
        }

        private void FallbackLogic( KeyValuePair<Planet, List<SafeSquadWrapper>> pair )
        {
            Planet nearestSpawnerPlanet = null;
            foreach ( GameEntity_Squad spawner in BaseInfo.CoalitionSpawners.DisplaySquads() )
            {
                if ( nearestSpawnerPlanet == null || pair.Key.GetHopsTo( spawner.Planet ) < pair.Key.GetHopsTo( nearestSpawnerPlanet ) )
                    nearestSpawnerPlanet = spawner.Planet;
            }

            if ( nearestSpawnerPlanet == null )
                return;

            Planet nextPlanet = null;

            foreach ( Planet workingPlanet in pair.Key.LinkedNeighbors( false ) )
            {
                if ( nextPlanet == null || workingPlanet.GetHopsTo( nearestSpawnerPlanet ) < nextPlanet.GetHopsTo( nearestSpawnerPlanet ) )
                    nextPlanet = workingPlanet;
            }

            pair.Value.ForEach( unit =>
            {
                var squad = unit.GetSquad();
                if ( squad == null )
                    return;
                if ( squad.Orders.GetHasAnyOrdersOfType( EntityOrderType.Wormhole ) )
                    return;

                GameCommand wormholeCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse );
                wormholeCommand.RelatedFactionIndex = AttachedFaction.FactionIndex;
                wormholeCommand.RelatedEntityIDs.Add( unit.PrimaryKeyID );
                wormholeCommand.RelatedIntegers.Add( nextPlanet.Index );
                World_AIW2.Instance.QueueGameCommand( AttachedFaction, wormholeCommand, false );
            } );
        }
        #endregion

        #region Fireteams Implementations
        public override void GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass( bool DefenseMode, Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, List<FireteamTarget> PreferredTargets, List<FireteamTarget> FallbackTargets, object TeamObj )
        {
            // Find any Dark Spire VG's near our coalition homeworlds, slowly branching out.
            // Put down any planets with hostie strength as backup targets.
            for ( short x = 0; x < 20 && PreferredTargets.Count == 0; x += 2 )
                foreach ( Faction workingFaction in BaseInfo.ActiveCoalitionFactions.GetDisplayList() )
                {
                    Planet spawnerPlanet = workingFaction.GetFirstMatching( SplinteringSpireFactionBaseInfo.Tag_CoalitionSpawner, true, true )?.Planet;
                    if ( spawnerPlanet == null )
                        continue;

                    foreach ( Planet.PlanetAtHopDistance _phd in spawnerPlanet.PlanetsWithinXHops_NoFilters( x ) )
                    {
                        Planet planet = _phd.Planet;
                        PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                        GameEntity_Squad VG = planet.GetFirstMatching( FactionType.SpecialFaction, VGTag, true, true );

                        if ( VG != null )
                            PreferredTargets.AddIfNotAlreadyIn( new FireteamTarget( VG ) );
                        else if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength >= 2500 )
                            FallbackTargets.AddIfNotAlreadyIn( new FireteamTarget( planet ) );
                    }
                }
        }
        public override Planet GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass( Planet TargetPlanet, int TeamStrength, Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            // Find a random nearby planet that has no hostiles, and doesn't have a VG on it.
            Planet planetToReturn = null;
            int lowestDanger = 0;
            foreach ( Planet.PlanetAtHopDistance _phd in TargetPlanet.PlanetsWithinXHops_NoFilters( 2 ) )
            {
                Planet workingPlanet = _phd.Planet;
                if ( workingPlanet.GetFirstMatching( FactionType.SpecialFaction, VGTag, true, true ) != null )
                    continue; // VG

                int workingDanger = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForFireteam, workingPlanet, true, out short _ ) + Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, workingPlanet, TargetPlanet, false, out short __ );
                if ( planetToReturn == null || CurrentPlanetForFireteam.GetHopsTo( workingPlanet ) < CurrentPlanetForFireteam.GetHopsTo( planetToReturn ) || workingDanger < lowestDanger )
                {
                    planetToReturn = workingPlanet;
                    lowestDanger = workingDanger;
                }
            }
            return planetToReturn ?? TargetPlanet.GetRandomNeighbor( false, Context );
        }
        public override GameEntity_Squad GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            // Retreat to nearest Coalition homeworld.
            GameEntity_Squad nearestSpawner = null;
            foreach ( GameEntity_Squad spawner in BaseInfo.CoalitionSpawners.DisplaySquads() )
            {
                if ( nearestSpawner == null || CurrentPlanetForFireteam.GetHopsTo( spawner.Planet ) < CurrentPlanetForFireteam.GetHopsTo( nearestSpawner.Planet ) )
                    nearestSpawner = spawner;
            }

            return nearestSpawner;
        }
        #endregion
        #endregion

        public override void DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly( ref int debugStage, GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, Faction factionThatKilledEntity, Faction entityOwningFaction, int numExtraStacksKilled, ArcenHostOnlySimContext Context )
        {
            if ( !entity.TypeData.GetHasTag( SplinteringSpireFactionBaseInfo.Tag_Collector ) )
                return; // Not our business.

            Planet assignedPlanet = World_AIW2.Instance.GetEntityByID_Squad( entity.MinorFactionStackingID )?.Planet;
            if ( assignedPlanet == null )
                return; // Not assigned to derelict.

            if ( !BaseInfo.CollectorsAssignedByPlanet.DisplayContainsKey( assignedPlanet ) )
                return; // Derelict assignment no longer valid.

            // Collector died. Convert it to the owner and type of the nearest collector on the planet that isn't of their faction.
            Faction factionToConvertTo = null;
            int nearestEntity = 999999;
            foreach ( GameEntity_Squad collector in entity.Planet.Squads( SplinteringSpireFactionBaseInfo.Tag_Collector ) )
            {
                if ( collector.PlanetFaction.Faction == entityOwningFaction )
                    continue; // Same faction.

                int distance = collector.GetDistanceTo_VeryCheapButExtremelyRough( entity, RadiusCheck.AddRadiiToDistance );
                if ( distance < nearestEntity )
                {
                    nearestEntity = distance;
                    factionToConvertTo = collector.PlanetFaction.Faction;
                }
            }

            if ( factionToConvertTo == null )
                return;

            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, BaseInfo.GetCollectorTag( factionToConvertTo ) );

            PlanetFaction pFaction = entity.Planet.GetPlanetFactionForFaction( factionToConvertTo );

            GameEntity_Squad newCollector = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entity.CurrentMarkLevel, pFaction.FleetUsedAtPlanet, 0, entity.WorldLocation, Context, "SplinteringSpire-ConvertingCollectorOnDeath" );

            if ( newCollector != null )
            {
                newCollector.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                newCollector.CurrentStateOfMatter = StateOfMatterTypeDataTable.Instance.GetRowByName( SplinteringSpireFactionBaseInfo.StateOfMatter );
                newCollector.GameSecondWillExitStateOfMatter = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.DurationOfDerelicts; // Last the whole event.
                newCollector.MinorFactionStackingID = entity.MinorFactionStackingID; // Assign to the derelict.
            }
        }
    }
}
