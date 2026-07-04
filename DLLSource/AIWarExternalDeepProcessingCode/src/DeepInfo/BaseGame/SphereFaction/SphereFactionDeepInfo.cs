using Arcen.AIW2.Core;
using Arcen.AIW2.External.BulkPathfinding;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public sealed class SphereFactionDeepInfo : ExternalFactionDeepInfoRoot, IBulkPathfinding
    {
        public SphereFactionBaseInfo BaseInfo;

        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<SphereFactionBaseInfo>();
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            BaseInfo = null;
        }

        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType ) { }
        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType ) { }

        protected override int MinimumSecondsBetweenLongRangePlannings => 2;

        public override void SeedStartingEntities_EarlyMajorFactionClaimsOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            // Before any custom logic, if they requested us be on a nomad planet, we're going on a nomad planet.
            if ( AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "SeedOnNomadIfPossible", false ) ) //this field won't exist unless DLC2 is installed
            {
                //seed on a nomad if possible
                StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, AttachedFaction, SpecialEntityType.None, BaseInfo.GetSphereTag(), SeedingType.HardcodedCount, 1, MapGenCountPerPlanet.One, MapGenSeedStyle.FullUseByFactionOnNomadIfPossible, 3, 3, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal );
                return;
            }

            // Claimed Planets refers to Human/AI homeworlds, and other sphere planets.
            List<Planet> claimedPlanets = Planet.GetTemporaryPlanetList( "SphereFaction-SeedStartingEntities-claimedPlanets", 10f );
            if ( claimedPlanets == null ) //blocked for teardown/shutdown; bail
                return;
            List<Planet> potentialPlanets = Planet.GetTemporaryPlanetList( "SphereFaction-SeedStartingEntities-potentialPlanets", 10f );
            if ( potentialPlanets == null ) //blocked for teardown/shutdown; bail
            {
                Planet.ReleaseTemporaryPlanetList( claimedPlanets );
                return;
            }
            bool seededNearbyFriendlySphere = false;
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                GameEntity_Squad sphere = planet.GetFirstMatching( FactionType.SpecialFaction, SphereFactionBaseInfo.Tag_DysonSphere, true, true );
                if ( sphere == null )
                    continue;

                claimedPlanets.AddIfNotAlreadyIn( planet );

                if ( sphere.TypeData.GetCustomBool_Slow( "custom_ConsideredPlayerFriendly" ) && planet.OriginalHopsToHumanHomeworld <= 4 )
                    seededNearbyFriendlySphere = true;
            }

            foreach ( GameEntity_Squad kingUnit in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                claimedPlanets.AddIfNotAlreadyIn( kingUnit.Planet );
            }

            // Build our potential planets list.
            // Before anything else, if we didn't seed a friendly sphere yet, and we're trying to seed a friendly sphere now; put it somewhat near the player homeworld.
            if ( BaseInfo.CanSphereBeFriendly && !seededNearbyFriendlySphere )
            {
                foreach ( GameEntity_Squad kingUnit in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    if ( kingUnit.PlanetFaction.Faction.Type != FactionType.Player )
                        continue;

                    foreach ( Planet.PlanetAtHopDistance _phd in kingUnit.Planet.PlanetsWithinXHops_NoFilters( 4 ) )
                    {
                        Planet workingPlanet = _phd.Planet;
                        Int16 workingPlanetHops = _phd.Hops;
                        if ( workingPlanetHops < 3 || workingPlanet.MapGen_IsFullyUsedByAFaction )
                            continue;

                        potentialPlanets.Add( workingPlanet );
                    }
                }
            }

            if ( potentialPlanets.Count < 1 )
            {
                // We'll want to spread Dyson Spheres out equally in the galaxy. No overlapping. Equal spread from all homeworlds, and other spheres.
                // Obviously it's not going to be perfect; we aren't going to precalculate based on the number of sphere factions. We just want a bit of spread.
                // Start out requiring 6 hops, and decrease from there.
                for ( short x = 6; x > 0 && potentialPlanets.Count < 1; x-- )
                    foreach ( Planet workingPlanet in World_AIW2.Instance.Planets( false ) )
                    {
                        bool isValid = !workingPlanet.MapGen_IsFullyUsedByAFaction;

                        if ( isValid )
                            for ( int i = 0; i < claimedPlanets.Count; i++ )
                            {
                                if ( workingPlanet.GetHopsTo( claimedPlanets[i] ) <= x )
                                {
                                    isValid = false;
                                    break;
                                }
                            }

                        if ( isValid )
                            potentialPlanets.AddIfNotAlreadyIn( workingPlanet );
                    }
            }

            // Pick an actual planet, if possible. If not, just seed it normally with some generous restrictions.
            Planet planetToSeedOn = null;
            if ( potentialPlanets.Count > 0 )
                planetToSeedOn = potentialPlanets[Context.RandomToUse.Next( potentialPlanets.Count )];

            if ( planetToSeedOn != null )
            {
                var typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, BaseInfo.GetSphereTag() );
                var loc = Engine_AIW2.Instance.CombatCenter;
                loc = planetToSeedOn.GetSafePlacementPoint_AroundZone(Context, typeData, PlanetSeedingZone.OuterSystem);

                GameEntity_Squad sphere = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( planetToSeedOn.GetPlanetFactionForFaction( AttachedFaction ), typeData, 1, AttachedFaction.LooseFleet, 0, loc, Context, "SeedingDysonSphere" );

                planetToSeedOn.MapGen_IsFullyUsedByAFaction = true;
                planetToSeedOn.MapGen_FullyUsingFaction = AttachedFaction;
                planetToSeedOn.MapGen_MajorFactionsBlockedHereReason = sphere.TypeData.DisplayName;
            }
            else
                StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, AttachedFaction, SpecialEntityType.None,
                BaseInfo.GetSphereTag(), SeedingType.HardcodedCount, 1, MapGenCountPerPlanet.One, MapGenSeedStyle.FullUseByFaction, 3, 3,
                PlanetSeedingZone.InnerSystem, SeedingExpansionType.ComplicatedOriginal, null, -1 );

            Planet.ReleaseTemporaryPlanetList( claimedPlanets );
            Planet.ReleaseTemporaryPlanetList( potentialPlanets );
        }

        public override void UpdatePlanetInfluence_HostOnly( ArcenHostOnlySimContext Context )
        {
            List<Planet> influenced = Planet.GetTemporaryPlanetList( "SphereFaction-DeepInfo-UpdatePlanetInfluence", 10f );
            if ( influenced == null ) //blocked for teardown/shutdown; bail
                return;
            Planet spherePlanet = BaseInfo.Sphere.Display?.Planet;
            if ( spherePlanet != null )
                influenced.Add( spherePlanet );
            AttachedFaction.SetInfluenceForPlanetsToList( influenced );
            Planet.ReleaseTemporaryPlanetList( influenced );
        }

        #region Stage3
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            if ( BaseInfo.Sphere.Display != null )
            {
                if ( BaseInfo.Strength.Display >= BaseInfo.GetMaxStrength )
                    BaseInfo.ClearBudget(); // We're full.
                else
                {
                    IncreaseBudget( BaseInfo.GetPerSecondBudget );
                    SpawnUnitsIfAble( Context );
                }

                if ( BaseInfo.ShouldSpawnAntagonizer )
                    SpawnAntagonizer( Context );

                // Splintering Spire logic.
                if ( SplinteringSpireFactionBaseInfo.Instance != null )
                {
                    foreach ( KeyValuePair<Faction, Dictionary<Planet, FInt>> pair in SplinteringSpireFactionBaseInfo.Instance.ControlByFactionOnDerelictPlanet.GetDisplayDict() )
                    {
                        foreach ( KeyValuePair<Planet, FInt> subPair in pair.Value )
                        {
                            if ( subPair.Value > 0 )
                                HandleSplinteringSpirePerSecondState( subPair.Value );
                        }
                    }

                    if ( SplinteringSpireFactionBaseInfo.Instance.UnclaimedVictoryPoints.TryGetValue( AttachedFaction, out short wins ) && wins > 0 )
                    {
                        HandleSplinteringSpireVictoryPoint();
                        SplinteringSpireFactionBaseInfo.Instance.UnclaimedVictoryPoints.TryUpdate( AttachedFaction, (short)(wins - 1), wins );
                    }
                }
            }

            #region Old Zenith Dyson Sphere Compatibility
            if ( SphereFactionBaseInfo.OldDysonFactionsLeftToProcess > 0 )
            {
                // This is a bit tricky. Basically, if our Sphere doesn't exist, it means an old Anatagonizer faction out there has our sphere.
                // That is... somewhat easy to find, albeit if there are multiple Dyson we might be stealing one from another faction.
                // But we will still only end up with one Sphere per faction, and that is desired logic.
                if ( BaseInfo.Sphere.Display == null )
                    TakeSphereFromAntagonizedFaction( Context );

                // The more difficult part are all of the Antagonized units flying around. There is no practical way to take back what was ours.
                // So instead, we're just going to randomly split them up.
                TakeUnitsFromAntagonizedFaction( Context );

                // Lastly, blow up any Antagonizers not in an AI faction.
                foreach ( Faction workingFaction in World_AIW2.Instance.Factions )
                {
                    if ( workingFaction.Type != FactionType.AI )
                        continue;

                    foreach ( GameEntity_Squad antagonizer in workingFaction.Squads( SphereFactionBaseInfo.Tag_ZenithAntagonizer ) )
                    {
                        antagonizer.Despawn( Context, true, InstancedRendererDeactivationReason.IFinishedMyJob );
                    }

                    foreach ( GameEntity_Squad antagonizer in workingFaction.Squads( "WarpingInDysonAntagonizer" ) )
                    {
                        antagonizer.Despawn( Context, true, InstancedRendererDeactivationReason.IFinishedMyJob );
                    }
                }

                SphereFactionBaseInfo.OldDysonFactionsLeftToProcess--;
            }
            #endregion
        }

        private void SpawnAntagonizer( ArcenHostOnlySimContext Context )
        {
            bool isThereAPlayerKing = false;
            foreach ( GameEntity_Squad king in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                if ( king.PlanetFaction.Faction.Type == FactionType.Player )
                {
                    isThereAPlayerKing = true;
                    break;
                }
            }

            if ( !isThereAPlayerKing )
                return; // If the human doesn't exist, the ai has nobody to throw the sphere's anger at.

            // Converted from now Deprecated DysonUtilityMethods
            bool debug = false;
            Planet spawnPlanet = null;
            if ( spawnPlanet == null )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Picking random planet", Verbosity.DoNotShow );
                FInt AIP = FactionUtilityMethods.Instance.GetCurrentAIP();
                Int16 minHopsFromHumanPlanet = -1;
                Int16 maxHopsFromHumanPlanet = -1;
                byte preferredMarkUnderX = 0;
                if ( AIP <= 150 )
                {
                    minHopsFromHumanPlanet = 2;
                    maxHopsFromHumanPlanet = 3;
                    preferredMarkUnderX = 3;
                }
                else if ( AIP <= 300 )
                {
                    minHopsFromHumanPlanet = 3;
                    maxHopsFromHumanPlanet = 6;
                    preferredMarkUnderX = 4;
                }
                else
                {
                    minHopsFromHumanPlanet = 5;
                    preferredMarkUnderX = 7;
                }
                spawnPlanet = FactionUtilityMethods.FindSuitablePlanet( Context, minHopsFromHumanPlanet, maxHopsFromHumanPlanet, preferredMarkUnderX, true );
            }
            Planet targetPlanet = spawnPlanet;
            if ( targetPlanet == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Bug: Could not find planet to spawn antagonizer.", Verbosity.DoNotShow );
                return;
            }
            if ( ArcenNetworkAuthority.GetIsHostMode() )
            {
                if ( targetPlanet.IntelLevel > PlanetIntelLevel.Unexplored )
                {
                    PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.PlanetToView = targetPlanet;

                    World_AIW2.Instance.QueueChatMessageOrCommand( "Dyson Antagonizer spawning on  " + targetPlanet.Name, ChatType.LogToCentralChat, "ArkChiefOfStaff_DysonAntagonized", chatHandlerOrNull );
                }
                else
                    World_AIW2.Instance.QueueChatMessageOrCommand( "Dyson Antagonizer spawning somewhere in the galaxy", ChatType.LogToCentralChat, "ArkChiefOfStaff_DysonAntagonized", null );
            }
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, SphereFactionBaseInfo.Tag_ZenithAntagonizer );
            if ( entityData == null )
                throw new Exception( "No DysonAntagonizer defined in xml " );

            ArcenPoint spawnLocation = targetPlanet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 400 ) );
            Faction spawnFaction = targetPlanet.GetControllingFaction();

            PlanetFaction pFaction = targetPlanet.GetPlanetFactionForFaction( spawnFaction );
            GameEntity_Squad antagonizer = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "DysonAntagSpawn" );
            if ( antagonizer != null )
                antagonizer.MinorFactionStackingID = AttachedFaction.FactionIndex;
        }

        private void HandleSplinteringSpirePerSecondState( FInt percentage ) => IncreaseBudget( BaseInfo.GetPerSecondBudget * percentage );

        private void HandleSplinteringSpireVictoryPoint()
        {
            BaseInfo.AddedBudgetMultiplierFromExternalSources += SplinteringSpireFactionBaseInfo.Instance.Difficulty.RewardMultiplier;
            BaseInfo.AddedMaxStrengthMultiplierFromExternalSources += SplinteringSpireFactionBaseInfo.Instance.Difficulty.RewardMultiplier;
        }

        private void IncreaseBudget( FInt budgetToAdd )
        {
            if ( BaseInfo.Strength.Display < BaseInfo.GetMaxStrength / 10 ) // If we're super weak, due to early game or a recent loss, get mad.
                budgetToAdd *= BaseInfo.Difficulty.BudgetMultiplierWhenBelow10PercentCapacity;

            // Now, we split up our budget among our unit tiers. Lower tiers get more.
            budgetToAdd /= 6;

            BaseInfo.ChangeStoredBudgetForTier( 0, budgetToAdd * 3, true );
            BaseInfo.ChangeStoredBudgetForTier( 1, budgetToAdd * 2, true );
            BaseInfo.ChangeStoredBudgetForTier( 2, budgetToAdd, true );
        }

        private void SpawnUnitsIfAble( ArcenHostOnlySimContext context )
        {
            PlanetFaction pFaction = BaseInfo.Sphere.Display.PlanetFaction;

            // See if we can afford a unit for each tier.
            for ( byte x = 0; x < SphereFactionBaseInfo.MaxUnitTier; x++ )
            {
                FInt currentBudget = BaseInfo.GetStoredBudgetForTier( x );
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( context, BaseInfo.GetUnitTagForTier( x ) );

                int cost = Math.Max( entityData.CostForAIToPurchase, entityData.GetForMark( pFaction.Faction.GetGlobalMarkLevelForShipLine( entityData ) ).StrengthPerSquad_CalculatedWithNullFleetMembership );
                int canAfford = (currentBudget / cost).GetNearestIntPreferringLower();

                int squadsToSpawn, stacksPerSquad = 0, extraUnitsToStack = 0;
                if ( canAfford <= 10 )
                    squadsToSpawn = canAfford;
                else
                {
                    squadsToSpawn = 10;
                    stacksPerSquad = (canAfford - 10) / 10;
                    extraUnitsToStack = (canAfford - 10) % 10;
                }

                BaseInfo.ChangeStoredBudgetForTier( x, -(cost * canAfford), true );

                // Spawn them in.
                int strengthSpawned = 0;
                for ( byte y = 0; y < squadsToSpawn; y++ )
                {
                    // Don't let them overfill by TOO much.
                    if ( BaseInfo.Strength.Display + strengthSpawned >= BaseInfo.GetMaxStrength )
                        return;

                    ArcenPoint spawnPoint = BaseInfo.Sphere.Display.WorldLocation.GetRandomPointWithinDistance( context.RandomToUse, 0, 5000 );

                    GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, pFaction.Faction.GetGlobalMarkLevelForShipLine( entityData ),
                        pFaction.FleetUsedAtPlanet, 0, spawnPoint, context, "SphereFaction-MilitarySpawn" );

                    if ( entity != null )
                    {
                        entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.None );

                        entity.Orders.QueueOrder(entity, EntityOrder.Create_Move_Decollision(entity.WorldLocation, entity.Planet.Index, true, OrderSource.Other, false));
                        entity.Orders.QueueOrder(entity, EntityOrder.Create_SetBehavior_Attacker_Full(OrderSource.Other, false));

                        strengthSpawned += entity.GetStrengthPerSquad();

                        // Don't let them overfill by TOO much.
                        if ( BaseInfo.Strength.Display + strengthSpawned >= BaseInfo.GetMaxStrength )
                            return;

                        for ( int z = 0; z < stacksPerSquad; z++ )
                        {
                            // Don't let them overfill by TOO much.
                            if ( BaseInfo.Strength.Display + strengthSpawned >= BaseInfo.GetMaxStrength )
                                return;

                            entity.AddOrSetExtraStackedSquadsInThis( 1, false );

                            strengthSpawned += entity.GetStrengthPerSquad();
                        }

                        if ( extraUnitsToStack > 0 )
                        {
                            // Don't let them overfill by TOO much.
                            if ( BaseInfo.Strength.Display + strengthSpawned >= BaseInfo.GetMaxStrength )
                                return;

                            extraUnitsToStack--;
                            entity.AddOrSetExtraStackedSquadsInThis( 1, false );

                            strengthSpawned += entity.GetStrengthPerSquad();
                        }
                    }
                }
            }
        }

        #region Old Zenith Dyson Sphere Compatibility
        // These functions are not the most optimized
        private void TakeSphereFromAntagonizedFaction( ArcenHostOnlySimContext context )
        {
            GameEntity_Squad antagonizedSphere = DysonSphereAntagonizedFactionBaseInfo.Instance.AttachedFaction.GetFirstMatching( "AntagonizedDysonSphere", true, true );
            if ( antagonizedSphere != null )
            {
                PlanetFaction pFaction = antagonizedSphere.Planet.GetPlanetFactionForFaction( AttachedFaction );
                GameEntity_Squad newSphere = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, GameEntityTypeDataTable.Instance.GetRandomRowWithTag( context, SphereFactionBaseInfo.Tag_ZenithSphere ), 1,
                    pFaction.FleetUsedAtPlanet, 0, antagonizedSphere.WorldLocation, context, "ConvertingOldDysonSphere" );
                antagonizedSphere.Despawn( context, true, InstancedRendererDeactivationReason.IFinishedMyJob );
            }
        }

        private void TakeUnitsFromAntagonizedFaction( ArcenHostOnlySimContext context )
        {
            int entitiesToTake = SphereFactionBaseInfo.OldDysonFactionsLeftToProcess > 1 ? DysonSphereAntagonizedFactionBaseInfo.Instance.AttachedFaction.GetTotalSquadCount() / SphereFactionBaseInfo.OldDysonFactionsLeftToProcess : 99999999; // Make sure we clean them out at the end.
            foreach ( GameEntity_Squad entity in DysonSphereAntagonizedFactionBaseInfo.Instance.AttachedFaction.Squads( "DysonSpawn" ) )
            {
                if ( entitiesToTake <= 0 )
                    continue;

                EndpointFunctions.TransferEntityToFaction( entity, AttachedFaction, "ConvertingOldDysonUnit" );
                entitiesToTake--;
            }
        }
        #endregion
        #endregion

        public override void ReactToHacking_AsPartOfMainSim_HostOnly( GameEntity_Squad entityBeingHacked, FInt WaveMultiplier, ArcenHostOnlySimContext Context, HackingEvent Event, Faction overrideFaction = null )
        {
            BaseInfo.GameSecondLastHacked = World_AIW2.Instance.GameSecond;

            // Add bonus budget equal to roughly a minute's worth of budget times the multiplier.
            FInt budgetToAdd = BaseInfo.GetPerSecondBudget;
            budgetToAdd *= 60;
            budgetToAdd *= WaveMultiplier;

            IncreaseBudget( budgetToAdd );
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            Planet spherePlanet = BaseInfo.Sphere.Display?.Planet;
            if ( spherePlanet == null )
                return;

            if ( ConflictPlanets == null )
                this.RebuildConflictPlanetsList();

            ConflictPlanets.Clear();

            if ( BaseInfo.IsAntagonized && BaseInfo.CanBeAntagonizedByAI )
            {
                foreach ( GameEntity_Squad king in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    if ( king == null || king.PlanetFaction.Faction.Type != FactionType.Player )
                        continue;

                    ConflictPlanets.AddIfNotAlreadyIn( king.Planet );
                }
            }
            else if ( BaseInfo.IsAntagonized && BaseInfo.CanBeAntagonizedByHumans && BaseInfo.DysonAntagonizer.Display != null )
            {
                if ( BaseInfo.DysonAntagonizer.Display.Planet != null )
                    ConflictPlanets.Add( BaseInfo.DysonAntagonizer.Display.Planet );
            }
            else if ( BaseInfo.IsCurrentlyAngryDueToHack )
            {
                if ( spherePlanet != null )
                    ConflictPlanets.Add( spherePlanet );
            }

            // If we aren't being antagonized by something in particular, simply find any conflict planets to journey towards.
            // Make sure we don't consider any planets that are outside of our hop limit.
            if ( ConflictPlanets.Count == 0 )
                this.RebuildConflictPlanetsList( ( planet ) => planet.GetHopsTo( spherePlanet ) > BaseInfo.HopLimit );

            bool skipThisEntity(GameEntity_Squad e)
            {
                if (e.TypeData.GetHasTag( SplinteringSpireFactionBaseInfo.Tag_Collector ))
                    return true;
                if (e.Orders.GetHasAnyOrdersOfType(EntityOrderType.Move_Decollision))
                    return true;
                return false;
            }

            this.PrepareConflictPlanetMovementLogic( Context, 100, 60, skipThisEntity );

            this.ExecuteMovementCommands( Context );
            this.ExecuteWormholeCommands( Context );
        }
    }
}
