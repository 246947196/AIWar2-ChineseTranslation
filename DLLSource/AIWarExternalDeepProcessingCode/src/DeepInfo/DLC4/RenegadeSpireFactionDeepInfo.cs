using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public class RenegadeSpireFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        public RenegadeSpireFactionBaseInfo BaseInfo;
        public static RenegadeSpireFactionDeepInfo Instance = null;

        private readonly List<SafeSquadWrapper> UnassignedShipsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "RenegadeSpireFactionDeepInfo-UnassignedShipsLRP" );
        private readonly List<Planet> WorkingPlanetList = List<Planet>.Create_WillNeverBeGCed( 200, "RenegadeSpireFactionDeepInfo-WorkingPlanetList" );

        // Fireteam infrastructure
        public readonly ProtectedValDictionary<Planet, FireteamRegiment> TeamsAimedAtPlanet = ProtectedValDictionary<Planet, FireteamRegiment>.Create_WillNeverBeGCed( 100, "RenegadeSpireFactionDeepInfo-TeamsAimedAtPlanet" );
        private readonly ArcenLessLinkedList<Fireteam> AvailableFireteams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "RenegadeSpireFactionDeepInfo-AvailableFireteams" );
        private static readonly FInt OverkillRaiders = FInt.FromParts( 0, 850 );

        private const int baseInvasionInterval = 1200;
        private const int baseInvasionVariance = 600;
        private const int subsequentInvasionInterval = 60;
        private const int subsequentInvasionCheckVariance = 60;

        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<RenegadeSpireFactionBaseInfo>();
            Instance = this;
        }

        protected override void Cleanup()
        {
            Instance = null;
            BaseInfo = null;
            TeamsAimedAtPlanet.Clear();
            UnassignedShipsLRP.Clear();
            WorkingPlanetList.Clear();
            AvailableFireteams.Clear();
            DefilersLRP.Clear();
            RelicsLRP.Clear();
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 5;

        #region SeedStartingEntities
        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            //Nothing to seed at start time; we'll join our allies later in Sim code
        }
        #endregion

        #region LongRangePlanning
        private static readonly List<SafeSquadWrapper> DefilersLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "RenegadeSpireFactionDeepInfo-DefilersLRP" );
        private static readonly List<SafeSquadWrapper> RelicsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "RenegadeSpireFactionDeepInfo-RelicsLRP" );
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            TeamsAimedAtPlanet.Clear();
            UnassignedShipsLRP.Clear();
            DefilersLRP.Clear();
            RelicsLRP.Clear();

            // Reset all fireteam deep info for this cycle
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
                team.DeepInfo.Reset();

            // Categorize combat ships for fireteam assignment
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
            {
                if ( entity.TypeData.GetHasTag( "RenegadeSpireDefiler" ) )
                {
                    DefilersLRP.Add( entity );
                    continue;
                }
                if ( entity.TypeData.GetHasTag( "RenegadeRelic" ) )
                {
                    RelicsLRP.Add( entity );
                    continue;
                }

                if ( entity.TypeData.GetHasTag( "FireteamEligible" ) )
                {
                    if ( entity.FireteamId < 0 )
                        UnassignedShipsLRP.Add( entity );
                    else
                    {
                        Fireteam team = FireteamBaseUtility.GetFireteamById( BaseInfo.Teams, entity.FireteamId );
                        if ( team == null )
                            entity.FireteamId = -1;
                        else
                            team.DeepInfo.AddUnit( entity );
                    }
                }
            }

            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                HandleDefilersLRP( Context, pathingCacheData );

                // Manage fireteams
                FireteamUtility.UpdateFireteams( AttachedFaction, Context, pathingCacheData, BaseInfo.Teams, TeamsAimedAtPlanet, null, OverkillRaiders );
                FireteamUtility.UpdateRegiments( AttachedFaction, Context, pathingCacheData, BaseInfo.Teams, TeamsAimedAtPlanet, null, AttachedFaction.MinFireteamStrength, true );

                for ( int i = 0; i < UnassignedShipsLRP.Count; i++ )
                {
                    GameEntity_Squad entity = UnassignedShipsLRP[i].GetSquad();
                    AssignUnitToFireteam( entity, Context, pathingCacheData );
                }

                if ( AttachedFaction.NumFireteams != BaseInfo.Teams.GetItemCount() )
                    AttachedFaction.NumFireteams = BaseInfo.Teams.GetItemCount();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "RenegadeSpire LRP error: " + e, Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();
            }
        }

        private void AssignUnitToFireteam( GameEntity_Squad entity, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( entity == null )
                return;

            AvailableFireteams.Clear();

            if ( BaseInfo.Teams.GetItemCount() == 0 )
            {
                Fireteam team = Fireteam.CreateNewWithIDFromList( BaseInfo.Teams );
                team.MyStrengthMultiplierForStrengthCalculation = FInt.One;
                team.EnemyStrengthMultiplierForStrengthCalculation = FInt.FromParts( 1, 500 );
                team.StrengthToBringOnline = AttachedFaction.MinFireteamStrength + Context.RandomToUse.Next( 0, AttachedFaction.MaxFireteamStrength - AttachedFaction.MinFireteamStrength );
                BaseInfo.Teams.AddIfNotAlreadyIn( team );
            }

            foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
            {
                if ( team.status == FireteamStatus.Disbanded ||
                     team.status == FireteamStatus.ReadyToAttack ||
                     team.status == FireteamStatus.Attacking )
                    continue;
                if ( team.status == FireteamStatus.Staging &&
                     team.DeepInfo.TeamStrength > AttachedFaction.MaxFireteamStrength )
                    continue;
                if ( team.DeepInfo.TeamStrength > AttachedFaction.MaxFireteamStrength * 2 &&
                     team.DeepInfo.ShipsInFireteam.Count > 20 )
                    continue;

                if ( team.DeepInfo.CurrentPlanet == null )
                {
                    AvailableFireteams.AddIfNotAlreadyIn( team );
                    continue;
                }
                if ( AvailableFireteams.GetItemCount() > 4 )
                    break;

                Int16 hops = 0;
                int danger = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, entity.Planet, team.DeepInfo.CurrentPlanet, true, out hops );
                if ( danger < 20000 && hops < 10 )
                    AvailableFireteams.AddIfNotAlreadyIn( team );
            }

            bool found = false;
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( AvailableFireteams ) )
            {
                if ( team.DeepInfo.TeamStrength < team.StrengthToBringOnline )
                {
                    team.DeepInfo.AddUnit( entity );
                    team.DeepInfo.IdentifyCurrentPlanet();
                    entity.FireteamId = team.FireTeamID;
                    found = true;
                    break;
                }
            }

            if ( !found )
            {
                if ( AvailableFireteams.GetItemCount() > 0 && Context.RandomToUse.Next( 0, 3 ) != 0 )
                {
                    Fireteam ft = AvailableFireteams.GetRandom( Context.RandomToUse );
                    if ( ft != null )
                    {
                        ft.DeepInfo.AddUnit( entity );
                        ft.DeepInfo.IdentifyCurrentPlanet();
                        entity.FireteamId = ft.FireTeamID;
                        found = true;
                    }
                }

                if ( !found )
                {
                    Fireteam newTeam = Fireteam.CreateNewWithIDFromList( BaseInfo.Teams );
                    newTeam.MyStrengthMultiplierForStrengthCalculation = FInt.One;
                    newTeam.EnemyStrengthMultiplierForStrengthCalculation = FInt.FromParts( 1, 500 );
                    newTeam.StrengthToBringOnline = AttachedFaction.MinFireteamStrength + Context.RandomToUse.Next( 0, AttachedFaction.MaxFireteamStrength - AttachedFaction.MinFireteamStrength );
                    BaseInfo.Teams.AddIfNotAlreadyIn( newTeam );
                    newTeam.DeepInfo.AddUnit( entity );
                    newTeam.DeepInfo.IdentifyCurrentPlanet();
                    entity.FireteamId = newTeam.FireTeamID;
                }
            }
        }
        //Set immediately before PreferredTargets.Sort(...) so the comparison can be a non-capturing
        //static delegate.  [ThreadStatic] because this runs on a background non-sim thread.
        [ThreadStatic] private static Planet cb_ftCurrentPlanet;
        [ThreadStatic] private static Faction cb_ftAttachedFaction;
        [ThreadStatic] private static FInt cb_ftFalloff;

        public override void GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass( bool DefenseMode, Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData,
            List<FireteamTarget> PreferredTargets, List<FireteamTarget> FallbackTargets, object TeamObj )
        {
            if ( DefenseMode )
            {
                GetDefensiveFireteamTargets( PreferredTargets, Context );
            }
            else
            {
                //The best defense is a good offense!
                GetPreferredRenegadeTargets( PreferredTargets, AttachedFaction, Context );
            }
            FInt falloffForDistance = FInt.FromParts( 0, 050 );

            for ( int i = 0; i < PreferredTargets.Count; i++ )
            {
                FireteamTarget target = PreferredTargets[i];
                Int16 hops = 0;
                target.dangerOfTarget = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForFireteam, PreferredTargets[i].planet, true, out hops );
                PreferredTargets[i] = target;
            }
            cb_ftCurrentPlanet = CurrentPlanetForFireteam;
            cb_ftAttachedFaction = AttachedFaction;
            cb_ftFalloff = falloffForDistance;
            PreferredTargets.Sort( static delegate ( FireteamTarget Left, FireteamTarget Right )
            {
                int lDistance = Left.planet.GetHopsTo( cb_ftCurrentPlanet );
                int rDistance = Right.planet.GetHopsTo( cb_ftCurrentPlanet );
                int lDanger = Left.dangerOfTarget;
                int rDanger = Right.dangerOfTarget;
                if ( Left.planet.GetControllingOrInfluencingFaction() == cb_ftAttachedFaction )
                    lDanger /= 2;
                if ( Right.planet.GetControllingOrInfluencingFaction() == cb_ftAttachedFaction )
                    rDanger /= 2;
                lDanger = lDanger + (cb_ftFalloff * lDistance).IntValue;
                rDanger = rDanger + (cb_ftFalloff * rDistance).IntValue;
                return lDanger.CompareTo( rDanger );
            } );
            for ( int i = PreferredTargets.Count - 1; i >= 0; i-- )
            {
                FireteamTarget target = PreferredTargets[i];
                if ( Fireteam.IsThisAWinningBattle( AttachedFaction, Context, target.planet, 4 ) || target.dangerOfTarget == -1 )
                    PreferredTargets.RemoveAt( i );
            }
            for ( int i = 0; i < PreferredTargets.Count; i++ )
            {
                if ( i == 0 ) continue;
                FireteamTarget target = PreferredTargets[i];
                FireteamTarget weakestTarget = PreferredTargets[0];
                FInt danger = (FInt)target.dangerOfTarget;
                FInt weakestDanger = (FInt)weakestTarget.dangerOfTarget;
                if ( target.planet.GetControllingOrInfluencingFaction() == AttachedFaction )
                    danger /= 2;
                if ( danger > weakestDanger * FInt.FromParts( 2, 000 ) )
                    PreferredTargets.RemoveRange( i, PreferredTargets.Count - i );
            }

            // Relics on hostile planets are always the highest priority — prepend them in reverse
            // so the first relic found ends up at index 0.
            for ( int i = RelicsLRP.Count - 1; i >= 0; i-- )
            {
                GameEntity_Squad relic = RelicsLRP[i].GetSquad();
                if ( relic == null )
                    continue;
                PlanetFaction relicPF = relic.Planet.GetPlanetFactionForFaction( AttachedFaction );
                if ( relicPF == null || relicPF.DataByStance[FactionStance.Hostile].TotalStrength <= 0 )
                    continue;
                PreferredTargets.Insert( 0, new FireteamTarget( relic.Planet ) );
            }
        }
        private void GetDefensiveFireteamTargets( List<FireteamTarget> listToFill, ArcenLongTermIntermittentPlanningContext Context )
        {
            listToFill.Clear();
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                PlanetFaction renegadePFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                if ( renegadePFaction == null || renegadePFaction.DataByStance[FactionStance.Self].TotalStrength <= 0 )
                    continue; // no friendly presence on this planet
                if ( renegadePFaction.DataByStance[FactionStance.Hostile].TotalStrength <= 0 )
                    continue; // no hostile units attacking this planet
                listToFill.Add( new FireteamTarget( planet ) );
            }
        }

        public void GetPreferredRenegadeTargets( List<FireteamTarget> listToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            listToFill.Clear();
            for ( int i = 0; i < RelicsLRP.Count; i++ )
            {
                GameEntity_Squad relic = RelicsLRP[i].GetSquad();
                if ( relic == null )
                    continue;
                PlanetFaction relicPF = relic.Planet.GetPlanetFactionForFaction( faction );
                if ( relicPF == null || relicPF.DataByStance[FactionStance.Hostile].TotalStrength <= 5 * 1000 )
                    continue;
                listToFill.Add( new FireteamTarget( relic.Planet ) );
            }
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null )
                    continue;
                if ( !otherFaction.GetIsHostileTowards( faction ) )
                    continue;
                bool foundDireGuardPost = false;
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.AIPOnDeath ) )
                {
                    //clear out dire guard posts
                    if ( entity.TypeData.SpecialType != SpecialEntityType.DireGuardPost )
                        continue;
                    listToFill.Add( new FireteamTarget( entity ) );
                    foundDireGuardPost = true;
                }

                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    if ( foundDireGuardPost )
                        break; //Kings with living Dire Guard Posts aren't valid targets
                    listToFill.Add( new FireteamTarget( entity ) );
                }
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.CommandStation ) )
                {
                    if ( Fireteam.IsThisAWinningBattle( faction, Context, entity.Planet, 2 ) )
                        continue;
                    listToFill.Add( new FireteamTarget( entity.Planet ) );
                }
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.GrantsMinorFactionPlanetControl ) )
                {
                    if ( Fireteam.IsThisAWinningBattle( faction, Context, entity.Planet, 2 ) )
                        continue;
                    listToFill.Add( new FireteamTarget( entity.Planet ) );
                }
            }
        }

        public override Planet GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass( Planet TargetPlanet, int TeamStrength, Planet CurrentPlanetForTeam,
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            Planet bestPlanet = null;
            int dangerOfPathFromBestPlanet = -1;
            int distanceFromBestPlanet = 9999;
            Int16 hopsFromBestPlanet = 9999;
            int unused = 0;

            if ( TargetPlanet == null )
                throw new Exception( "No target planet set in RenegadeSpire GetFireteamLurkPlanet" );

            foreach ( Planet.PlanetAtHopDistance _phd in TargetPlanet.PlanetsWithinXHops_NoFilters( -1 ) )
            {
                Planet planet = _phd.Planet;
                Int16 Distance = _phd.Hops;
                if ( planet == TargetPlanet )
                    continue;

                int planetDefensiveStrength = Fireteam.GetPlanetDefensiveStrength( planet, AttachedFaction, true, ref unused, FInt.Zero, FInt.Zero );
                if ( planet.GetControllingFaction().GetIsHostileTowards( AttachedFaction ) && planetDefensiveStrength > TeamStrength / 10 )
                    continue;

                Int16 hops = 0;
                int totalDifficultyOfPathToLurkPlanet = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForTeam, planet, true, out hops );
                if ( totalDifficultyOfPathToLurkPlanet >= TeamStrength * 5 )
                    continue;

                int totalDifficultyOfPathToTarget = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, planet, TargetPlanet, true, out hops );
                if ( totalDifficultyOfPathToTarget < 0 )
                    totalDifficultyOfPathToTarget = 0;

                if ( dangerOfPathFromBestPlanet > totalDifficultyOfPathToTarget || dangerOfPathFromBestPlanet == -1 )
                {
                    dangerOfPathFromBestPlanet = totalDifficultyOfPathToTarget;
                    distanceFromBestPlanet = Distance;
                    hopsFromBestPlanet = hops;
                    bestPlanet = planet;
                }
                else if ( dangerOfPathFromBestPlanet == totalDifficultyOfPathToTarget && ( distanceFromBestPlanet > Distance || hopsFromBestPlanet > hops ) )
                {
                    dangerOfPathFromBestPlanet = totalDifficultyOfPathToTarget;
                    distanceFromBestPlanet = Distance;
                    hopsFromBestPlanet = hops;
                    bestPlanet = planet;
                }

                if ( hopsFromBestPlanet <= 3 && dangerOfPathFromBestPlanet <= TeamStrength / 10 )
                    break;
            }
            return bestPlanet;
        }
        #endregion

        #region PerSecondLogic
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            if ( BaseInfo.Difficulty == null )
                return;
            HandleDefilersSim( Context );
            HandleFracturesSim( Context );
            MaybeSpawnRelic( Context );
            MaybeSpawnDefiler( Context, null );
            JoinAlliesIfNecessary( Context );
            MarkupShipsIfNecessary( Context );
        }
        public void MarkupShipsIfNecessary( ArcenHostOnlySimContext Context )
        {
            int interval = BaseInfo.Difficulty.ShipMarkupIntervalSeconds;
            foreach ( GameEntity_Squad ship in this.BaseInfo.CombatShips.DisplaySquads() )
            {
                if ( !ship.TypeData.GetHasTag( "UpgradeEligible" ) )
                    continue;

                int elapsed = World_AIW2.Instance.GameSecond - ship.GameSecondCreated;
                int targetMark = elapsed / interval + 1;
                if ( targetMark <= ship.CurrentMarkLevel )
                    continue;

                if ( ship.CurrentMarkLevel < 7 )
                {
                    ship.SetCurrentMarkLevel( (byte)Math.Min( targetMark, 7 ) );
                    continue;
                }

                // At mark 7: transform to next tier
                string nextTierTag = null;
                if ( ship.TypeData.GetHasTag( "RenegadeSpireTierOne" ) )
                    nextTierTag = "RenegadeSpireTierTwo";
                else if ( ship.TypeData.GetHasTag( "RenegadeSpireTierTwo" ) )
                    nextTierTag = "RenegadeSpireTierThree";

                if ( nextTierTag == null )
                {
                    // TierThree (or untagged) ships have no further tier — leave them at mark 7
                    continue;
                }

                GameEntityTypeData nextTypeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, nextTierTag );
                if ( nextTypeData == null )
                    throw new Exception( "RenegadeSpire MarkupShipsIfNecessary: can't find ship with tag " + nextTierTag );

                Planet spawnPlanet = ship.Planet;
                ArcenPoint spawnPoint = ship.WorldLocation;
                PlanetFaction pFaction = spawnPlanet.GetPlanetFactionForFaction( AttachedFaction );
                ship.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
                GameEntity_Squad newShip = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, nextTypeData, 1,
                    pFaction.Faction.LooseFleet, 0, spawnPoint, Context, "RenegadeSpire-TierTransform" );
                if ( newShip != null )
                {
                    newShip.spawnVis = SpawnVisualization.WarpIn;
                    newShip.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                }
            }
        }
        public void JoinAlliesIfNecessary( ArcenHostOnlySimContext Context )
        {
            if ( BaseInfo.Fractures.GetDisplayList().Count > 0 )
                return;
            if ( World_AIW2.Instance.GameSecond % 30 == 0 )
            {
                Planet friendlyPlanet = FindFriendlyPlanetToJoin( AttachedFaction, Context );
                if ( friendlyPlanet != null )
                    JoinFriendlyPlanet( friendlyPlanet, AttachedFaction, Context );
            }
            if ( BaseInfo.MinorFactionAllied )
            {
                if ( BaseInfo.TimeForNextRenegadeInvasionCheck == -1 )
                {
                    // Lost our fractures — arm the invasion timer but don't invade immediately
                    int timeToStartTracking = 600;
                    if ( World_AIW2.Instance.GameSecond > timeToStartTracking )
                        BaseInfo.TimeForNextRenegadeInvasionCheck = World_AIW2.Instance.GameSecond + baseInvasionInterval + Context.RandomToUse.Next( 0, baseInvasionVariance );
                    else
                        BaseInfo.TimeForNextRenegadeInvasionCheck = timeToStartTracking + baseInvasionInterval + Context.RandomToUse.Next( 0, baseInvasionVariance );
                }
                if ( BaseInfo.TimeForNextRenegadeInvasionCheck - World_AIW2.Instance.GameSecond < 600 )
                    UpdateRenegadeInvasionForce( AttachedFaction, Context );
                if ( BaseInfo.TimeForNextRenegadeInvasionCheck <= World_AIW2.Instance.GameSecond )
                {
                    Planet weakPlanet = FindWeakPlanetToInvade( AttachedFaction, Context );
                    if ( weakPlanet == null )
                        BaseInfo.TimeForNextRenegadeInvasionCheck = World_AIW2.Instance.GameSecond + subsequentInvasionInterval + Context.RandomToUse.Next( 0, subsequentInvasionCheckVariance );
                    else
                        InvadeWeakPlanet( weakPlanet, AttachedFaction, Context );
                }
            }
        }

        private int defilerRange = 400;
        private void HandleDefilersSim( ArcenHostOnlySimContext Context )
        {
            foreach ( GameEntity_Squad defiler in this.BaseInfo.Defilers.DisplaySquads() )
            {
                RenegadeSpirePerUnitBaseInfo mData = defiler.TryGetExternalBaseInfoAs<RenegadeSpirePerUnitBaseInfo>();
                if ( mData == null )
                    continue;
                if ( mData.DefensiveMetalToSpend <= 0 )
                {
                    defiler.Despawn( Context, true, InstancedRendererDeactivationReason.IFinishedMyJob );
                    continue;
                }
                // Always re-resolve Destination from DestinationId — a stale non-null reference to a
                // despawned relic would otherwise cause every nearby defiler to spawn a duplicate fracture.
                if ( mData.DestinationId != -1 )
                {
                    mData.Destination = World_AIW2.Instance.GetEntityByID_Squad( mData.DestinationId );
                    if ( mData.Destination == null )
                        mData.DestinationId = -1;
                }
                else
                    mData.Destination = null;

                if ( mData.Destination != null )
                {
                    if ( defiler.Planet == mData.Destination.Planet )
                    {
                        int distance = Mat.DistanceBetweenPointsImprecise( defiler.WorldLocation, mData.Destination.WorldLocation );
                        if ( distance < defilerRange )
                        {
                            Planet targetPlanet = mData.Destination.Planet;
                            ArcenPoint relicLocation = mData.Destination.WorldLocation;
                            mData.Destination.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
                            mData.Destination = null;
                            mData.DestinationId = -1;
                            SpawnFractureOnPlanet( targetPlanet, Context, relicLocation );
                        }
                    }
                    continue;
                }
                // No relic destination — target the nearest relic if any exist
                List<SafeSquadWrapper> relics = BaseInfo.Relics.GetDisplayList();
                if ( relics.Count > 0 )
                {
                    GameEntity_Squad nearestRelic = null;
                    int minHops = int.MaxValue;
                    for ( int i = 0; i < relics.Count; i++ )
                    {
                        GameEntity_Squad relic = relics[i].GetSquad();
                        if ( relic == null )
                            continue;
                        int hops = defiler.Planet.GetHopsTo( relic.Planet );
                        if ( hops < minHops )
                        {
                            minHops = hops;
                            nearestRelic = relic;
                        }
                    }
                    if ( nearestRelic != null )
                    {
                        mData.Destination = nearestRelic;
                        mData.DestinationId = nearestRelic.PrimaryKeyID;
                        mData.BuildPlanetIndex = -1;
                        mData.DestinationPoint = ArcenPoint.ZeroZeroPoint;
                        continue;
                    }
                }
                // No relics — if we've arrived at the build point, build a guard post
                if ( mData.BuildPlanetIndex != -1 )
                {
                    Planet buildPlanet = World_AIW2.Instance.GetPlanetByIndex( mData.BuildPlanetIndex );
                    if ( buildPlanet != null && defiler.Planet == buildPlanet )
                    {
                        int distance = Mat.DistanceBetweenPointsImprecise( defiler.WorldLocation, mData.DestinationPoint );
                        if ( distance < defilerRange )
                        {
                            GameEntityTypeData guardPostData = mData.NextShipToCreate;
                            if ( guardPostData != null )
                            {
                                PlanetFaction pFaction = buildPlanet.GetPlanetFactionForFaction( AttachedFaction );
                                GameEntity_Squad guardPost = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, guardPostData, 1,
                                    pFaction.Faction.LooseFleet, 0, mData.DestinationPoint, Context, "RenegadeSpire-BuildGuardPost" );
                                if ( guardPost != null )
                                    guardPost.spawnVis = SpawnVisualization.Normal;
                                mData.DefensiveMetalToSpend -= guardPostData.CostForAIToPurchase;
                            }
                            mData.BuildPlanetIndex = -1;
                            mData.DestinationPoint = ArcenPoint.ZeroZeroPoint;
                        }
                    }
                    continue;
                }

                // No destination at all — pick a build planet and point
                PickDefilerBuildTarget( defiler, mData, Context );
            }
        }

        private void PickDefilerBuildTarget( GameEntity_Squad defiler, RenegadeSpirePerUnitBaseInfo mData, ArcenHostOnlySimContext Context )
        {
            WorkingPlanetList.Clear();

            // Single BFS outward to 3 hops; far cheaper than calling GetHopsTo on every planet
            foreach ( Planet.PlanetAtHopDistance _phd in defiler.Planet.PlanetsWithinXHops_NoFilters( (Int16)3 ) )
            {
                Planet planet = _phd.Planet;
                if ( planet.GetControllingOrInfluencingFaction().GetIsFriendlyTowards( AttachedFaction ) )
                    WorkingPlanetList.Add( planet );
            }

            // If fewer than 5 options within 3 hops, expand outward until we have 5 or exhaust the galaxy
            if ( WorkingPlanetList.Count < 5 )
            {
                foreach ( Planet.PlanetAtHopDistance _phd in defiler.Planet.PlanetsWithinXHops_NoFilters( -1 ) )
                {
                    Planet planet = _phd.Planet;
                    if ( _phd.Hops <= 3 )
                        continue; // already collected in the first pass
                    if ( planet.GetControllingOrInfluencingFaction().GetIsFriendlyTowards( AttachedFaction ) )
                        WorkingPlanetList.Add( planet );
                    if ( WorkingPlanetList.Count >= 5 )
                        break;
                }
            }

            if ( WorkingPlanetList.Count == 0 )
                return;
            Planet buildPlanet = WorkingPlanetList[Context.RandomToUse.Next( 0, WorkingPlanetList.Count )];

            GameEntityTypeData guardPostData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "RenegadeDefenses" );
            if ( guardPostData == null )
                throw new Exception( "Could not find renegade defense entity" );

            ArcenPoint buildPoint = buildPlanet.GetSafePlacementPointAroundPlanetCenter( Context, guardPostData, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 200 ) );
            mData.BuildPlanetIndex = (Int16)buildPlanet.Index;
            mData.DestinationPoint = buildPoint;
            mData.NextShipToCreate = guardPostData;
        }

        private void HandleDefilersLRP( ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            for ( int i = 0; i < DefilersLRP.Count; i++ )
            {
                GameEntity_Squad defiler = DefilersLRP[i].GetSquad();
                if ( defiler == null )
                    continue;

                RenegadeSpirePerUnitBaseInfo mData = defiler.TryGetExternalBaseInfoAs<RenegadeSpirePerUnitBaseInfo>();
                if ( mData == null )
                    continue;

                if ( defiler.Orders.GetQueuedOrderCount() > 0 )
                    continue;

                if ( mData.Destination != null )
                    AutoDefendUtility.GoToPlanetThenLocation( defiler, mData.Destination.Planet, mData.Destination.WorldLocation, Context, PathCacheData );
                else if ( mData.BuildPlanetIndex != -1 )
                {
                    Planet buildPlanet = World_AIW2.Instance.GetPlanetByIndex( mData.BuildPlanetIndex );
                    if ( buildPlanet != null )
                        AutoDefendUtility.GoToPlanetThenLocation( defiler, buildPlanet, mData.DestinationPoint, Context, PathCacheData );
                }
            }
        }

        private void HandleFracturesSim( ArcenHostOnlySimContext Context )
        {
            List<SafeSquadWrapper> fractures = BaseInfo.Fractures.GetDisplayList();
            int markupInterval = BaseInfo.Difficulty.MarkupInterval;
            int aip = GlobalAIWorldBaseInfo.Instance.AIProgress_Effective.IntValue;
            int metalIncome = BaseInfo.Difficulty.BaseMetalIncome + ( aip / 10 ) * BaseInfo.Difficulty.BonusMetalIncomePer10AIP;

            for ( int i = 0; i < fractures.Count; i++ )
            {
                GameEntity_Squad fracture = fractures[i].GetSquad();
                if ( fracture == null )
                    continue;

                RenegadeSpirePerUnitBaseInfo data = fracture.TryGetExternalBaseInfoAs<RenegadeSpirePerUnitBaseInfo>();
                if ( data == null )
                    continue;

                // Markup
                if ( data.LastMarkupSecond == -1 )
                    data.LastMarkupSecond = World_AIW2.Instance.GameSecond;
                else if ( fracture.CurrentMarkLevel < 7 && World_AIW2.Instance.GameSecond - data.LastMarkupSecond >= markupInterval )
                {
                    byte newMark = (byte)( fracture.CurrentMarkLevel + 1 );
                    fracture.SetCurrentMarkLevel( newMark );
                    data.LastMarkupSecond = World_AIW2.Instance.GameSecond;
                    if ( newMark >= 7 )
                    {
                        MaybeSpawnRelic( Context, forceSpawn: true );
                        MaybeSpawnDefiler( Context, fracture );
                    }
                }

                // Metal income
                data.MetalStored += metalIncome;

                // Pick next ship if none queued
                if ( data.NextShipToCreate == null )
                    data.NextShipToCreate = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "RenegadeSpireTierOne" );

                // Spawn if we can afford it
                if ( data.NextShipToCreate != null && data.MetalStored >= data.NextShipToCreate.CostForAIToPurchase )
                {
                    data.MetalStored -= data.NextShipToCreate.CostForAIToPurchase;

                    PlanetFaction pFaction = fracture.Planet.GetPlanetFactionForFaction( AttachedFaction );
                    ArcenPoint spawnLocation = fracture.Planet.GetSafePlacementPoint_AroundEntity( Context, data.NextShipToCreate, fracture, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 150 ) );
                    GameEntity_Squad ship = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, data.NextShipToCreate, fracture.CurrentMarkLevel,
                        pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "RenegadeSpire-SpawnShip" );
                    if ( ship != null )
                    {
                        ship.spawnVis = SpawnVisualization.WarpIn;
                        ship.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                    }
                    data.NextShipToCreate = null;
                }
            }
        }

        private void MaybeSpawnRelic( ArcenHostOnlySimContext Context, bool forceSpawn = false )
        {
            int relicInterval = BaseInfo.Difficulty.RelicSpawnIntervalSeconds;
            if ( !forceSpawn && BaseInfo.LastRelicSpawnAttemptSecond != -1 &&
                 World_AIW2.Instance.GameSecond - BaseInfo.LastRelicSpawnAttemptSecond < relicInterval )
                return;

            BaseInfo.LastRelicSpawnAttemptSecond = World_AIW2.Instance.GameSecond;

            Planet relicPlanet = GetValidRelicPlanet( Context );
            if ( relicPlanet == null )
                return;

            GameEntityTypeData relicTypeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "RenegadeSpireRelic" );
            if ( relicTypeData == null )
                return;

            PlanetFaction relicPFaction = relicPlanet.GetPlanetFactionForFaction( AttachedFaction );
            ArcenPoint relicLocation = relicPlanet.GetSafePlacementPointAroundPlanetCenter( Context, relicTypeData, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 200 ) );
            GameEntity_Squad relic = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( relicPFaction, relicTypeData, 1,
                relicPFaction.Faction.LooseFleet, 0, relicLocation, Context, "RenegadeSpire-SpawnRelic" );
            if ( relic != null )
                relic.spawnVis = SpawnVisualization.WarpIn;
        }

        private void MaybeSpawnDefiler( ArcenHostOnlySimContext Context, GameEntity_Squad spawnFractureOrNull )
        {
            int aip = GlobalAIWorldBaseInfo.Instance.AIProgress_Effective.IntValue;
            int startingMetal = BaseInfo.Difficulty.BaseDefilerMetal + ( aip / 10 ) * BaseInfo.Difficulty.BonusDefilerMetalPer10AIP;
            if ( spawnFractureOrNull == null )
            {
                // Periodic path: initialize timer on first call, then wait for it
                if ( BaseInfo.TimeForNextDefilerSpawn == -1 )
                {
                    BaseInfo.TimeForNextDefilerSpawn = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.DefilerSpawnIntervalSeconds;
                    return;
                }
                if ( World_AIW2.Instance.GameSecond < BaseInfo.TimeForNextDefilerSpawn )
                    return;

                // Pick a random fracture as the spawn origin
                List<SafeSquadWrapper> fractures = BaseInfo.Fractures.GetDisplayList();
                if ( fractures.Count == 0 )
                    return;
                spawnFractureOrNull = fractures[Context.RandomToUse.Next( 0, fractures.Count )].GetSquad();
                if ( spawnFractureOrNull == null )
                    return;
            }
            GameEntityTypeData defilerTypeData = GameEntityTypeDataTable.Instance.GetRowByName( "RenegadeSpireDefiler" );
            if ( defilerTypeData == null )
                throw new Exception( "Could not find RenegadeDefiler" );
            PlanetFaction pFaction = spawnFractureOrNull.Planet.GetPlanetFactionForFaction( AttachedFaction );
            ArcenPoint spawnLocation = spawnFractureOrNull.Planet.GetSafePlacementPoint_AroundEntity( Context, defilerTypeData, spawnFractureOrNull, FInt.FromParts( 0, 020 ), FInt.FromParts( 0, 060 ) );
            GameEntity_Squad defiler = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, defilerTypeData, 1,
                pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "RenegadeSpire-SpawnDefiler" );
            if ( defiler == null )
                return;
            defiler.spawnVis = SpawnVisualization.WarpIn;

            defiler.CreateExternalBaseInfo<RenegadeSpirePerUnitBaseInfo>( "RenegadeSpirePerUnitBaseInfo" );
            RenegadeSpirePerUnitBaseInfo defilerInfo = defiler.TryGetExternalBaseInfoAs<RenegadeSpirePerUnitBaseInfo>();
            if ( defilerInfo != null )
                defilerInfo.DefensiveMetalToSpend = startingMetal;

            BaseInfo.TimeForNextDefilerSpawn = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.DefilerSpawnIntervalSeconds;
        }

        private Planet GetValidRelicPlanet( ArcenHostOnlySimContext Context )
        {
            List<SafeSquadWrapper> fractures = BaseInfo.Fractures.GetDisplayList();
            if ( fractures.Count == 0 )
                return null;

            WorkingPlanetList.Clear();
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                // Directly scan for fractures and existing relics — HandleDefilersSim runs earlier
                // this same Stage3 call and may have spawned a fracture not yet in the Stage2 list.
                bool blocked = false;
                foreach ( GameEntity_Squad entity in planet.Squads() )
                {
                    if ( entity.GetFactionOrNull_Safe() != AttachedFaction )
                        continue;
                    if ( entity.TypeData.GetHasTag( "SpireFracture" ) || entity.TypeData.GetHasTag( "RenegadeRelic" ) )
                    {
                        blocked = true;
                        break;
                    }
                }
                if ( blocked )
                    continue;

                int minHops = int.MaxValue;
                for ( int i = 0; i < fractures.Count; i++ )
                {
                    GameEntity_Squad fracture = fractures[i].GetSquad();
                    if ( fracture == null )
                        continue;
                    int hops = planet.GetHopsTo( fracture.Planet );
                    if ( hops < minHops )
                        minHops = hops;
                }
                // >= 2 hops from every fracture and at least one fracture is within < 6 hops
                if ( minHops >= 2 && minHops < 6 )
                    WorkingPlanetList.Add( planet );
            }

            if ( WorkingPlanetList.Count == 0 )
                return null;

            // Sort ascending by hostile strength so we bias toward safer planets
            WorkingPlanetList.Sort( delegate ( Planet a, Planet b )
            {
                PlanetFaction pfA = a.GetPlanetFactionForFaction( AttachedFaction );
                PlanetFaction pfB = b.GetPlanetFactionForFaction( AttachedFaction );
                int strA = pfA == null ? 0 : pfA.DataByStance[FactionStance.Hostile].TotalStrength;
                int strB = pfB == null ? 0 : pfB.DataByStance[FactionStance.Hostile].TotalStrength;
                return strA.CompareTo( strB );
            } );

            // 40% chance of taking each planet in order; fallback to the weakest if none chosen
            for ( int i = 0; i < WorkingPlanetList.Count; i++ )
            {
                if ( Context.RandomToUse.Next( 0, 100 ) < 40 )
                    return WorkingPlanetList[i];
            }
            return WorkingPlanetList[0];
        }

        #endregion
        public override void UpdatePlanetInfluence_HostOnly( ArcenHostOnlySimContext Context )
        {
            List<Planet> planetsInfluenced = Planet.GetTemporaryPlanetList( "RenegadeSpire-UpdatePlanetInfluence_HostOnly-planetsInfluenced", 10f );
            if ( planetsInfluenced == null )
                return;
            List<SafeSquadWrapper> fractures = BaseInfo.Fractures.GetDisplayList();
            for ( int i = 0; i < fractures.Count; i++ )
                planetsInfluenced.AddIfNotAlreadyIn( fractures[i].Planet );
            AttachedFaction.SetInfluenceForPlanetsToList( planetsInfluenced );
            Planet.ReleaseTemporaryPlanetList( planetsInfluenced );
        }

        #region Helpers for Join Allies If Necessary
        public void UpdateRenegadeInvasionForce(Faction faction, ArcenHostOnlySimContext Context )
        {
            int invasionIncomePerSecond = 2;
            BaseInfo.RenegadeInvasionBudget += invasionIncomePerSecond;
            int minToSpend = 1000;
            if ( World_AIW2.Instance.GameSecond % 25 == 0 && BaseInfo.RenegadeInvasionBudget > minToSpend )
            {
                int retries = 10;
                GameEntityTypeData entityData = null;
                do{
                     entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "RenegadeInvasionForce" );
                     if ( entityData == null || entityData.CostForAIToPurchase > BaseInfo.RenegadeInvasionBudget )
                     {
                         entityData = null; //retry
                         continue;
                     }
                     BaseInfo.RenegadeInvasionForceStrength += entityData.CostForAIToPurchase;
                     BaseInfo.RenegadeInvasionBudget -= entityData.CostForAIToPurchase;
                     BaseInfo.RenegadeInvasionForce[entityData] += 1;

                } while ( BaseInfo.RenegadeInvasionBudget > 0  && retries-- > 0 );
            }
        }
        public Planet FindFriendlyPlanetToJoin( Faction faction, ArcenHostOnlySimContext Context )
        {
            //finds whether there are any planets of ours that we could join
            Planet bestPlanet = null;
            int bestPlanetStr = 1000;
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
                int enemyStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                if ( enemyStrength > 0 )
                    continue;
                int myAndAlliedStrength = pFaction.DataByStance[FactionStance.Self].TotalStrength +
                    pFaction.DataByStance[FactionStance.Friendly].TotalStrength;
                if ( myAndAlliedStrength <= bestPlanetStr )
                    continue;

                bestPlanet = planet;
                bestPlanetStr = myAndAlliedStrength;
            }
            if ( bestPlanet != null )
                ArcenDebugging.ArcenDebugLogSingleLine( "Renegade found best friendly planet " + bestPlanet.Name, Verbosity.DoNotShow );
            return bestPlanet;
        }
        private static readonly List<Planet> WorkingWeakPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "RenegadeFactionDeepInfo-WorkingWeakPlanets" );
        public Planet FindWeakPlanetToInvade( Faction faction, ArcenHostOnlySimContext Context )
        {
            //finds whether there are any planets of ours that we could join
            WorkingWeakPlanets.Clear();
            if ( BaseInfo.RenegadeInvasionForceStrength < 1000 )
                return null; //we are too weak
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
                int enemyStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                int myAndAlliedStrength = pFaction.DataByStance[FactionStance.Self].TotalStrength +
                    pFaction.DataByStance[FactionStance.Friendly].TotalStrength;
                int scaledDifference = ((enemyStrength - myAndAlliedStrength) * FInt.FromParts(1, 250)).IntValue;
                if ( scaledDifference > BaseInfo.RenegadeInvasionForceStrength )
                    continue;
                WorkingWeakPlanets.Add(planet);
            }
            if ( WorkingWeakPlanets.Count == 0 )
                return null;
            Planet bestPlanet = WorkingWeakPlanets[Context.RandomToUse.Next(0, WorkingWeakPlanets.Count)];
            ArcenDebugging.ArcenDebugLogSingleLine( "Renegade found best weak planet " + bestPlanet.Name + " from " + WorkingWeakPlanets.Count + " options", Verbosity.DoNotShow );
            return bestPlanet;
        }
        public void JoinFriendlyPlanet( Planet planet, Faction faction, ArcenHostOnlySimContext Context )
        {
            bool hasAllies = false;
            if ( ArcenNetworkAuthority.GetIsHostMode() )
            {
                if ( planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                {
                    PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.PlanetToView = planet;

                    Faction influencingFaction = World_AIW2.Instance.GetFactionByIndex( planet.PrimaryInfluencingFaction );
                    if ( influencingFaction != null && influencingFaction.GetIsFriendlyTowards( faction ) )
                    {

                        World_AIW2.Instance.QueueChatMessageOrCommand( faction.StartFactionColourForLog() + "The Renegade</color> have joined the " + 
                            influencingFaction.StartFactionColourForLog() + influencingFaction.GetDisplayName() + "</color> invasion on " + planet.Name, ChatType.LogToCentralChat, chatHandlerOrNull );
                        hasAllies = true;
                    }
                    else
                        World_AIW2.Instance.QueueChatMessageOrCommand( faction.StartFactionColourForLog() + "The Renegade</color> is building infrastructure on " + planet.Name, 
                            ChatType.LogToCentralChat, chatHandlerOrNull );
                }
            }
            GameEntity_Squad createdUnit;
            SpawnFractureOnPlanet( planet, Context );
            CreateFortress( faction, planet, Context, out createdUnit );
            if ( !hasAllies )
                CreateFortress( faction, planet, Context, out createdUnit ); //a bonus fortress if we have no friends
        }
        public void InvadeWeakPlanet( Planet planet, Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( !ArcenNetworkAuthority.GetIsHostMode() )
                return; //invasion only done on 
            int minRadius = (planet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 050 )).IntValue;
            int maxRadius = (planet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 150 )).IntValue;

            float warpInMultiplier = 0.9f;
            AngleDegrees angle = AngleDegrees.Create( (float)Context.RandomToUse.Next( 1, 360 ) );
            ArcenPoint invasionPoint = Engine_AIW2.Instance.CombatCenter.GetPointAtAngleAndDistance( angle, (int)(planet.GravWellSize.DistanceScale_GravwellRadius * warpInMultiplier) );

            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
            foreach ( KeyValuePair<GameEntityTypeData, int> pair in BaseInfo.RenegadeInvasionForce )
            {
                for ( int i = 0; i < pair.Value; i+= 2 )
                {
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, pair.Key, 0,
                         pFaction.FleetUsedAtPlanet, 0, invasionPoint, Context, "Renegade-InvadeWeakPlanet" );
                    if ( i < pair.Value - 1 )
                        newEntity.AddOrSetExtraStackedSquadsInThis( 2, true ); //always dump things out in stacks of 2 if we can
                    newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;//this throws off the calculations
                    newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                }
            }
            BaseInfo.TimeForNextRenegadeInvasionCheck = World_AIW2.Instance.GameSecond + 600;
            BaseInfo.RenegadeInvasionForceStrength = 0;
            BaseInfo.RenegadeInvasionForce.Clear();
        }
        private void SpawnFractureOnPlanet( Planet planet, ArcenHostOnlySimContext Context, ArcenPoint? nearLocationOrNull = null )
        {
            GameEntityTypeData fractureData = GameEntityTypeDataTable.Instance.GetRowByName( "SpireFracture" );
            if ( fractureData == null )
                return;
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
            ArcenPoint spawnLocation = nearLocationOrNull.HasValue
                ? planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, fractureData, nearLocationOrNull.Value, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 150 ) )
                : planet.GetSafePlacementPointAroundPlanetCenter( Context, fractureData, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 200 ) );
            GameEntity_Squad newFracture = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, fractureData, 1, pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "RenegadeSpire-JoinFriendly" );
            if ( newFracture != null )
            {
                newFracture.spawnVis = SpawnVisualization.WarpIn;
                newFracture.CreateExternalBaseInfo<RenegadeSpirePerUnitBaseInfo>( "RenegadeSpirePerUnitBaseInfo" );
                BaseInfo.FractureSpawnOrder.Add( newFracture.PrimaryKeyID );
            }
        }
        private void CreateFortress( Faction faction, Planet planet, ArcenHostOnlySimContext Context, out GameEntity_Squad newEntity )
        {
            newEntity = null;
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( "RenegadeSpireFortress" );
            if ( entityData == null )
                return;
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
            ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 200 ) );
            newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1, pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "RenegadeSpire-CreateFortress" );
        }
        #endregion
    }
}
