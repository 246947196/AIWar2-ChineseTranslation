using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    /*  Overview
        The Sappers have an economic mini-game and then build stuff

     */
    public sealed class SappersFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        //Set immediately before sortedFloweredCrystals.Sort(...) so the comparison can be a
        //non-capturing static delegate.  [ThreadStatic] for safety since this is faction planning code.
        [ThreadStatic] private static GameEntity_Squad cb_sappersSortSquad;
        public SappersFactionBaseInfo BaseInfo;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<SappersFactionBaseInfo>();
        }

        protected override void Cleanup()
        {
            BaseInfo = null;

            //not likely to matter much
            WorkingWormholeList.Clear();
            WorkingSquadList.Clear();
            WorkingPotentialPlanets.Clear();
            WorkingPreferredPotentialPlanets.Clear();
            SapperHabitatsLRP.Clear();
            SappersLRP.Clear();
            WatchtowersLRP.Clear();
            BeachheadersLRP.Clear();
            ConstructorsLRP.Clear();
            CombatShipsLRP.Clear();
            CombatShipsGoingBackToWatchtowerLRP.Clear();
            UnassignedShipsByPlanet.Clear();
            GoingHome.Clear();
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 5;

        private byte GetMarkLevelForStructure( GameEntity_Squad sapper )
        {
            if ( sapper.CurrentMarkLevel > (byte) 4)
                return (byte)(sapper.CurrentMarkLevel - 4);
            return (byte)1;
        }

        
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context)
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host. I think a lot of what the miners do is just not client 

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Sappers-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f ) : null;

            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                //If we have no presence on the map, periodically rejoin our allies
                JoinAlliesIfNecessary( Context );
                HandleJournals( Context );
                //Update our economy
                ProcessHabitats( Context );
                ProcessSappers( Context, pathingCacheData );
                UpdateCrystals( Context );
                //Update our military
                UpdateBeachheaders( Context );
                UpdateWatchtowers( Context );
                UpdateConstructors( Context );
                UpdateBasicStructures();
            }

            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Sappers Stage3 error: " + e, Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();

                #region Tracing
                if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
            }

        }
        public void UpdateBasicStructures()
        {
            List<SafeSquadWrapper> basicStructures = this.BaseInfo.BasicStructures.GetDisplayList();
            for ( int i = 0; i < basicStructures.Count; i++ )
            {
                GameEntity_Squad entity = basicStructures[i].GetSquad();
                if ( entity == null )
                    continue;
                if ( entity.CurrentMarkLevel < 7 )
                {
                    MarkUpIfAppropriate(entity);
                }
            }
        }
        private void MarkUpIfAppropriate( GameEntity_Squad entity )
        {
            if ( SappersFactionBaseInfo.GetSecondsTillMarkup( entity) <= 0 )
                entity.SetCurrentMarkLevel( (byte)(entity.CurrentMarkLevel + 1) );

        }
        public void ProcessHabitats( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Sappers-ProcessHabitats-trace", 10f ) : null;
            List<SafeSquadWrapper> sapperHabitats = this.BaseInfo.SapperHabitats.GetDisplayList();
            for ( int i = 0; i < sapperHabitats.Count; i++ )
            {
                GameEntity_Squad habitat = sapperHabitats[i].GetSquad();
                if ( habitat == null )
                    continue;
                SappersPerUnitBaseInfo data = habitat.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                data.MetalStored += BaseInfo.Difficulty.HabitatMetalPerSecond;
                if ( BaseInfo.ExtraStrongMode )
                    data.MetalStored += BaseInfo.Difficulty.HabitatMetalPerSecond; //double income for debug mode
                if ( BaseInfo.Sappers.Count == 0 && BaseInfo.SapperHabitats.Count == 1 &&
                     data.MetalStored < 2 * BaseInfo.Difficulty.CostForSapper / 3 )
                    data.MetalStored += 2 * BaseInfo.Difficulty.CostForSapper / 3; //if we have only one sapper habitat and no sapper, build the first sapper extra fast
                if ( data.MetalStored > BaseInfo.Difficulty.CostForSapper )
                {
                    //we could afford a new sapper
                    if ( BaseInfo.Sappers.Count >= BaseInfo.SapperHabitats.Count )
                         continue;
                    //we should build this new sapper
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "Sapper" );
                    data.MetalStored -= BaseInfo.Difficulty.CostForSapper;
                    if ( tracing )
                        tracingBuffer.Add("Creating a new sapper from " + habitat.ToStringWithPlanet() + "\n");
                    PlanetFaction pFaction = habitat.PlanetFaction;
                    Planet plan = habitat.Planet;

                    ArcenPoint finalPoint = habitat.Planet.GetSafePlacementPoint_AroundEntity( Context, entityData, habitat, FInt.FromParts( 0, 010 ), FInt.FromParts( 0, 050 ) );
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, (byte)1,
                                                                                                  pFaction.Faction.LooseFleet, 0, finalPoint, Context, "Sappers-FromHabitat" );
                    SappersPerUnitBaseInfo newData = newEntity.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                    data.MetalStored = 50;
                    if ( FactionUtilityMethods.Instance.OnlyNecromancerFactions() )
                    {
                        //Sappers need to start with more resources if they are helping a solo necromancer
                        if ( BaseInfo.Sappers.Count == 0 )
                        {
                            newData.BloodstoneStored = 1500;
                            newData.MoonstoneStored = 1500;
                            newData.MetalStored = 800;
                        }
                        else
                        {
                            newData.BloodstoneStored = 800;
                            newData.MoonstoneStored = 800;
                            newData.MetalStored = 400;
                        }
                    }
                    else
                    {
                        newData.MetalStored = 800;
                    }
                    //This is a DoubleBufferedConcurrentList thing.  It is threadsafe
                    BaseInfo.Sappers.AddToDisplayList(newEntity);
                    
                }
            }
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
        }

        public void ProcessSappers( ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Sappers-ProcessSappers-trace", 10f ) : null;
            int debugCode = 0;
            try
            {
                debugCode = 100;
                GameEntity_Squad weakestSapper = null; //if we have too few habitats, the lowest mark level/weakest sapper will attrition
                foreach ( GameEntity_Squad sapper in this.BaseInfo.Sappers.DisplaySquads() )
                {
                    debugCode = 200;
                    SappersPerUnitBaseInfo data = sapper.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                    UpdateNonSerializedFields( sapper, data );
                    debugCode = 300;
                    if ( tracing )
                    {
                        tracingBuffer.Add( sapper.ToStringWithPlanet() + " is being processed." );
                        if ( data.UnitToBuild != null )
                            tracingBuffer.Add( " It is off to build a " + data.UnitToBuild.GetDisplayName() );
                        GameEntity_Squad destination = data.DestinationForSappers.GetSquad();
                        if ( destination != null )
                            tracingBuffer.Add( " It is off to " + destination.ToStringWithPlanet() + " to collect resources." );
                        tracingBuffer.Add( "\n" );
                    }

                    data.MetalStored += BaseInfo.Difficulty.SapperMetalPerSecond;
                    if ( BaseInfo.ExtraStrongMode || BaseInfo.SapperHabitats.Count == 0 )
                        data.MetalStored += BaseInfo.Difficulty.SapperMetalPerSecond; //double income for debug or no-habitats mode

                    if ( weakestSapper == null )
                        weakestSapper = sapper;
                    else if ( sapper.CurrentMarkLevel < weakestSapper.CurrentMarkLevel ||
                              (sapper.CurrentMarkLevel == weakestSapper.CurrentMarkLevel &&
                               sapper.GetCurrentHullPoints() < weakestSapper.GetCurrentHullPoints()) )
                        weakestSapper = sapper;

                    if ( sapper.CurrentMarkLevel < 7 &&
                         sapper.CurrentMarkLevel * BaseInfo.Difficulty.SapperUpgradesAfterSpendingThisManyResources < data.TotalResourcesSpent )
                        sapper.SetCurrentMarkLevel( (byte)(sapper.CurrentMarkLevel + 1) );
                    debugCode = 400;
                    bool destinationReached = CollectResourcesFromDestinationIfPossible( sapper, data, Context );
                    if ( destinationReached )
                    {
                        if ( tracing )
                        {
                            GameEntity_Squad destination = data.DestinationForSappers.GetSquad();
                            tracingBuffer.Add( sapper.ToStringWithPlanet() + " has just reached its destination, " + (destination == null ? "null" : destination.ToString() ) ).Add( "\n" );
                        }
                        data.DestinationForSappers.Clear();
                        continue;
                    }
                    debugCode = 500;
                    bool buildDone = BuildStructureIfPossible( sapper, data, Context );
                    if ( buildDone )
                    {
                        debugCode = 600;
                        if ( data.UnitToBuild.GetHasTag( "SapperBaseCrystal" ) )
                        {
                            data.MetalStored -= BaseInfo.Difficulty.BaseCrystalCost;
                            data.TotalResourcesSpent += BaseInfo.Difficulty.BaseCrystalCost / 2; //metal matters less for upgrades
                        }
                        if ( data.UnitToBuild.MoonstoneCost > 0 )
                        {
                            data.MoonstoneStored -= data.UnitToBuild.MoonstoneCost;
                            data.TotalResourcesSpent += data.UnitToBuild.MoonstoneCost;
                        }
                        if ( data.UnitToBuild.BloodstoneCost > 0 )
                        {
                            data.BloodstoneStored -= data.UnitToBuild.BloodstoneCost;
                            data.TotalResourcesSpent += data.UnitToBuild.BloodstoneCost;
                        }
                        if ( data.UnitToBuild.GetHasTag( data.TagForUnitToBuildNext ) )
                            data.TagForUnitToBuildNext = "";
                        if ( tracing )
                            tracingBuffer.Add( sapper.ToStringWithPlanet() + " has built " + data.UnitToBuild.GetDisplayName() ).Add( "\n" );
                        debugCode = 800;
                        data.UnitToBuild = null;
                        data.PlanetIdx = -1;
                        data.LocationToBuild = ArcenPoint.ZeroZeroPoint;
                        data.PlanetToBuildOn = null;
                        continue;
                    }
                    debugCode = 900;
                    if ( data.DestinationForSappers.GetSquad() == null && data.UnitToBuild == null )
                    {
                        debugCode = 1000;
                        if ( tracing )
                            tracingBuffer.Add( sapper.ToStringWithPlanet() + " trying to find something to do" ).Add( "\n" );
                        bool foundOrders = DispatchToBuildDefensesIfPossible( sapper, data, Context );
                        if ( foundOrders )
                        {
                            if ( tracing )
                                tracingBuffer.Add( sapper.ToStringWithPlanet() + " is dispatched to build defeneses" ).Add( "\n" );

                            continue;
                        }
                        //We don't have any current orders
                        foundOrders = DispatchToBuildCrystalsIfPossible( sapper, data, Context );
                        if ( foundOrders )
                        {
                            if ( tracing )
                                tracingBuffer.Add( sapper.ToStringWithPlanet() + " is dispatched to build crystals" ).Add( "\n" );
                            continue;
                        }
                        foundOrders = DispatchToCollectResourcesIfPossible( sapper, data, Context, PathCacheData );
                        if ( foundOrders )
                        {
                            if ( tracing )
                                tracingBuffer.Add( sapper.ToStringWithPlanet() + " is dispatched to collect resources" ).Add( "\n" );

                            continue;
                        }
                    }
                }
                debugCode = 1200;
                if ( BaseInfo.Sappers.Count > 0 && BaseInfo.Sappers.Count > BaseInfo.SapperHabitats.Count &&
                     weakestSapper != null )
                {
                    debugCode = 1300;
                    int damageToTake = weakestSapper.GetMaxHullPoints() / 100;
                    weakestSapper.TakeDamageDirectly( damageToTake, null, null, DamageSource.BeingScrapped, Context );
                    weakestSapper.FlagAsNeedingFullSyncCheckIfInMultiplayerAndWeAreHost(); //let the client know extra fast
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in ProcessSappers debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer);
        }
        public void UpdateBeachheaders( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Sappers-UpdateBeachheaders-trace", 10f ) : null;
            int debugCode = 0;
            try{
                WorkingPotentialPlanets.Clear();
                debugCode = 100;
                List<SafeSquadWrapper> beachheaders = this.BaseInfo.Beachheaders.GetDisplayList();
                for ( int i = 0; i < beachheaders.Count; i++ )
                {
                    debugCode = 200;
                    GameEntity_Squad entity = beachheaders[i].GetSquad();
                    if ( entity == null )
                        continue;
                    SappersPerUnitBaseInfo data = entity.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                    //track last time we sent a constructor/turret
                    //If it's been too recent, do nothing
                    //If there are allies in combat nearby then dispatch a constructor to build a turret there

                    if ( entity.CurrentMarkLevel < 7 )
                    {
                        MarkUpIfAppropriate(entity);
                    }
                    //When creating the constructor, we set the apporpriate UnitToBuild, PlanetIdx and LocationToBuild
                    //then the constructor flies there and transforms (reuse the Sapper build code)
                    if ( World_AIW2.Instance.GameSecond - data.TimeLastMadeConstructor < BaseInfo.Difficulty.BeachheadTurretInterval )
                        continue;
                    debugCode = 300;
                    Planet planetToHelp = GetPlanetInNeedOfBeachheading( entity, data, Context );
                    if ( planetToHelp == null )
                        continue;
                    data.TimeLastMadeConstructor = World_AIW2.Instance.GameSecond;
                    if ( tracing )
                        tracingBuffer.Add(entity.ToStringWithPlanet() + " is spawning a constructor to build a turret on " + planetToHelp.Name );
                    debugCode = 400;
                    PlanetFaction pFaction = entity.PlanetFaction;
                    GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "SapperBeachheadConstructor" );
                    if ( typeData == null )
                        throw new Exception("Could not find unit with tag SapperBeachheadConstructor");
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typeData, entity.CurrentMarkLevel,
                                                                                                  pFaction.Faction.LooseFleet, 0, entity.WorldLocation, Context, "Sappers-Beachheader" );
                    debugCode = 500;
                    SappersPerUnitBaseInfo newData = newEntity.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                    newData.UnitToBuild = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "BeachheadTurret");
                    if ( newData.UnitToBuild == null )
                        throw new Exception("No units with BeachheadTurret tag defined");
                    newData.PlanetIdx = planetToHelp.Index;
                    newData.PlanetToBuildOn = planetToHelp;
                }
            } catch(Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in UpdateBeachheaders debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
        }
        public Planet GetPlanetInNeedOfBeachheading( GameEntity_Squad beachheader, SappersPerUnitBaseInfo data, ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Sappers-GetPlanetInNeedOfBeachheading-trace", 10f ) : null;
            bool verboseDebug = false;
            int debugCode = 0;
            try{
                debugCode = 100;
            foreach ( Planet.PlanetAtHopDistance _phd in beachheader.Planet.PlanetsWithinXHops( -1,
             delegate ( Planet secondaryPlanet )
             {
                 debugCode = 1100;
                 PlanetFaction spFaction = secondaryPlanet.GetPlanetFactionForFaction( AttachedFaction );
                 int hostileStrength = spFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                 int friendlystrength = spFaction.DataByStance[FactionStance.Friendly].TotalStrength + spFaction.DataByStance[FactionStance.Self].TotalStrength;
                 if ( hostileStrength > 10 * 1000 &&
                      hostileStrength > friendlystrength / 2 )
                     return PropogationEvaluation.SelfButNotNeighbors;

                 return PropogationEvaluation.Yes;
             } ) )
            {
                Planet planet = _phd.Planet;
                debugCode = 400;
                if ( WorkingPotentialPlanets.Count >= 4 )
                    break;

                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                if ( tracing && verboseDebug )
                    tracingBuffer.Add("\tChecking whether we should beahhead against " + planet.Name ).Add("\n");
                debugCode = 500;
                int friendlyStrength = pFaction.DataByStance[FactionStance.Friendly].TotalStrength + pFaction.DataByStance[FactionStance.Self].TotalStrength;
                int hostileStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                if ( BaseInfo.PlayerAllied )
                {
                    if ( FactionUtilityMethods.Instance.DoesPlanetHaveAIEye( planet ) )
                        continue;

                    //We need to prevent beachheading from triggering on loaded transports
                    //The rule here is "If there aren't player forces here, don't bother". So your sappers won't
                    //beachhead to support player-allied scourge, unfortunately.
                    if ( pFaction.DataByStance[FactionStance.Friendly].TotalPlayerStrengthNotInTransports < 4000 )
                        continue;
                }
                if ( hostileStrength > 4000 &&
                     friendlyStrength > 4000 )
                {
                    WorkingPotentialPlanets.Add( planet );
                    continue;
                }
            }
            if ( WorkingPotentialPlanets.Count == 0 )
                return null;
            debugCode = 1400;
            for ( int i = 0; i < WorkingPotentialPlanets.Count; i++ )
            {
                debugCode = 1500;
                //prefer closer planets but allow for some randomness
                if ( Context.RandomToUse.Next(0, 100) < 50 )
                    return WorkingPotentialPlanets[i];
            }
            debugCode = 1600;
            return WorkingPotentialPlanets[0];
            }catch(Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit error in GetPlanetInNeedOfBeachheading debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            return null;
        }
        public void UpdateConstructors( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Sappers-UpdateConstructors-trace", 10f ) : null;
            int debugCode = 0;
            try
            {
                debugCode = 100;
                List<SafeSquadWrapper> constructors = this.BaseInfo.Constructors.GetDisplayList();
                for ( int i = 0; i < constructors.Count; i++ )
                {
                    debugCode = 200;
                    GameEntity_Squad entity = constructors[i].GetSquad();
                    if ( entity == null )
                        continue;
                    debugCode = 210;
                    SappersPerUnitBaseInfo data = entity.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                    debugCode = 220;
                    UpdateNonSerializedFields( entity, data );
                    debugCode = 230;
                    if ( entity.Planet.Index == data.PlanetIdx && data.LocationToBuild == ArcenPoint.ZeroZeroPoint )
                    {
                        debugCode = 240;
                        //if we have just arrived at our planet but don't have a location to build, pick one
                        PlanetFaction pFaction = entity.PlanetFaction;
                        debugCode = 250;
                        data.LocationToBuild = entity.Planet.GetSafePlacementPoint_AroundEntity( Context, data.UnitToBuild, entity, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 150 ) );
                    }
                    debugCode = 260;
                    //If we are at our target location, transform into a suitable turret
                    bool buildDone = BuildStructureIfPossible( entity, data, Context );
                    debugCode = 270;
                    if ( buildDone )
                    {
                        entity.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in UpdateConstructors debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
        }
        public void UpdateWatchtowers( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Sappers-UpdateWatchtowers-trace", 10f ) : null;
            int debugCode = 0;
            try{
                debugCode = 100;
                bool localWatchActive = false;
                List<SafeSquadWrapper> watchtowers = this.BaseInfo.Watchtowers.GetDisplayList();
                for ( int i = 0; i < watchtowers.Count; i++ )
                {
                    debugCode = 200;
                    GameEntity_Squad entity = watchtowers[i].GetSquad();
                    if ( entity == null )
                        continue;
                    debugCode = 300;
                    SappersPerUnitBaseInfo data = entity.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                    if ( tracing )
                        tracingBuffer.Add("Updating " + entity.ToStringWithPlanet() ).Add("\n");
                    if ( entity.CurrentMarkLevel < 7 )
                    {
                        MarkUpIfAppropriate(entity);
                    }

                    debugCode = 500;
                    if ( data.GetStrengthInside(entity) < BaseInfo.MaxWatchtowerStrength )
                    {
                        debugCode = 600;
                        //lets buy some new ships
                        data.MetalStored += BaseInfo.Difficulty.WatchtowerIncome;
                        if ( tracing )
                            tracingBuffer.Add("\tWe have < " + BaseInfo.MaxWatchtowerStrength + " strength, so we get " + BaseInfo.Difficulty.WatchtowerIncome + " metal, giving us a total of " + data.MetalStored).Add("\n");

                        GameEntityTypeData entityData = null;
                        if ( entity.TypeData.GetHasTag("SpawnSapperTaupeShips" ) )
                            entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "SapperTaupeShips" );
                        if ( entity.TypeData.GetHasTag("SpawnSapperOpalShips" ) )
                            entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "SapperOpalShips" );
                        if ( entityData == null )
                            throw new Exception("Could not find tag for watchtower ship for " + entity.ToStringWithPlanet() );
                        if ( data.MetalStored >= entityData.MarkStatsFor( entity.CurrentMarkLevel ).MetalCost )
                        {
                            data.MetalStored -= entityData.MarkStatsFor( entity.CurrentMarkLevel ).MetalCost;
                            if ( data.ShipsInside[entityData] > 0 )
                                data.ShipsInside[entityData]++;
                            else
                                data.ShipsInside[entityData] = 1;
                            if ( tracing )
                                tracingBuffer.Add("\t\tAfter purchasing a " + entityData.GetDisplayName() + " we have " + data.MetalStored + " metal left\n");

                        }
                    }
                    debugCode = 1100;
                    data.UpdateShipsInside_ForUI(entity);
                    if ( tracing )
                        tracingBuffer.Add("\tShips inside: " + data.ShipsInside_ForUI + "\n");

                    debugCode = 1200;
                    //If there are enemies in range, spawn all ships inside us
                    Planet planetToHelp = FindEnemiesInWatchtowerRange( entity );
                    debugCode = 1300;
                    if ( planetToHelp != null )
                    {
                        debugCode = 1400;
                        if ( tracing )
                            tracingBuffer.Add("\t" + entity.ToStringWithPlanet() + " has detected nearby enemies on " + planetToHelp.Name +".\n");
                        localWatchActive = true;
                        data.PlanetWatchtowerWantsToHelp = planetToHelp;
                        PlanetFaction pFaction = entity.PlanetFaction;
                        debugCode = 2100;
                        foreach ( KeyValuePair<GameEntityTypeData, int> pair in data.ShipsInside )
                        {
                            debugCode = 2200;
                            for ( int j = 0; j < data.ShipsInside[pair.Key]; j++ )
                            {
                                debugCode = 2300;
                                ArcenPoint spawnLocation = entity.Planet.GetSafePlacementPoint_AroundEntity( Context, pair.Key, entity, FInt.FromParts( 0, 005 ), FInt.FromParts( 0, 030 ) );
                                GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, pair.Key, entity.CurrentMarkLevel,
                                       pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "Sappers-FromWatchtower" );  //is fine, main sim thread
                                debugCode = 2400;
                                if ( newEntity != null )
                                {
                                    newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true; //just in case
                                    newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                                }
                                debugCode = 2500;
                                SappersPerUnitBaseInfo newData = newEntity.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                                newData.HomeWatchtowerId = entity.PrimaryKeyID;
                            }
                        }

                        debugCode = 3100;
                        data.ShipsInside.Clear();
                    }
                    else
                    {
                        debugCode = 4100;
                        //If there are no enemies in range and we have a ship close to us, absorb it
                        //Note we can go over the limit here, or absorb random ships from other watchtowers; that's fine.
                        int range = 100;
                        List<SafeSquadWrapper> combatShips = this.BaseInfo.CombatShips.GetDisplayList();
                        debugCode = 4200;
                        for ( int j = 0; j < combatShips.Count; j++ )
                        {
                            debugCode = 4300;
                            GameEntity_Squad ship = combatShips[j].GetSquad();
                            if ( ship == null )
                                continue;
                            if ( ship.Planet != entity.Planet )
                                continue;
                            debugCode = 4400;
                            if ( Mat.DistanceBetweenPointsImprecise( entity.WorldLocation, ship.WorldLocation ) < range )
                            {
                                if ( data.ShipsInside[ship.TypeData] > 0 )
                                    data.ShipsInside[ship.TypeData]++;
                                else
                                    data.ShipsInside[ship.TypeData] = 1;
                                ship.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                            }
                        }
                    }

                    //In the LRP code, we check our home watchtower
                }
                debugCode = 5100;
                BaseInfo.AnyWatchtowersActive = localWatchActive;
            } catch(Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in UpdateWatchtowers debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
        }
        public Planet FindEnemiesInWatchtowerRange( GameEntity_Squad watchtower )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Sappers-FindEnemiesInWatchtowerRange-trace", 10f ) : null;

            //we can help on friendly or neutral planets with enemies and friends
            Planet foundPlanet = null;
            bool verboseDebug = true;
            foreach ( Planet.PlanetAtHopDistance _phd in watchtower.Planet.PlanetsWithinXHops_NoFilters( (Int16)BaseInfo.Difficulty.WatchtowerRange ) )
            {
                Planet planet = _phd.Planet;
                if ( verboseDebug && tracing )
                    tracingBuffer.Add("\t\tChecking whether there are enemies on " + planet.Name).Add("\n");

                if ( planet.GetControllingOrInfluencingFaction().GetIsHostileTowards(AttachedFaction) )
                    continue;
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                int hostileStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                if ( hostileStrength < 2000 )
                    continue;
                int friendlyStrength = pFaction.DataByStance[FactionStance.Friendly].TotalStrength + pFaction.DataByStance[FactionStance.Self].TotalStrength;
                if ( friendlyStrength < 2000 )
                    continue;
                foundPlanet = planet;
                break;
            }

            return foundPlanet;
        }
        private readonly int BuildRange = 100;
        public bool BuildStructureIfPossible( GameEntity_Squad sapper, SappersPerUnitBaseInfo data, ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Sappers-BuildStructureIfPossible-trace", 10f ) : null;
            int debugCode = 0;
            try
            {
                debugCode = 100;
                if ( data == null )
                    throw new Exception( "SappersPerUnitBaseInfo is null" );
                if ( data.UnitToBuild != null && data.PlanetToBuildOn != null
                     && data.LocationToBuild != ArcenPoint.ZeroZeroPoint )
                {
                    debugCode = 200;
                    int dist = -1;
                    if ( data.PlanetToBuildOn == sapper.Planet )
                    {
                        debugCode = 300;
                        if ( sapper.Planet.GetIsPointOutsideGravWell_SlowButCorrect( data.LocationToBuild ) )
                        {
                            debugCode = 400;
                            //There was a bug with variable gravwell sizes that could wind up with a LocationToBuild being
                            //outside the gravity well. This code will fix that bug. Remove it at some point
                            data.LocationToBuild = sapper.Planet.GetSafePlacementPointAroundPlanetCenter( Context, data.UnitToBuild, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 650 ) );
                            if ( sapper.Orders != null )
                                sapper.Orders.ClearOrders( ClearBehavior.DoNotClearBehaviors, ClearDecollisionOnParent.YesClear_AndAlsoClearDecollisionMoveOrders, ClearSource.YesClearAnyOrders_IncludingFromHumans, "Discarding bad orders" );
                        }
                        debugCode = 500;
                        dist = Mat.DistanceBetweenPointsImprecise( sapper.WorldLocation, data.LocationToBuild );
                    }
                    if ( tracing )
                        tracingBuffer.Add( sapper.ToString() + " at " + sapper.WorldLocation + " is going to build a " + data.UnitToBuild.GetDisplayName() + " at " + data.LocationToBuild + ". The distance to build location is " + dist + "\n" );
                    debugCode = 600;
                    //we have something to build
                    if ( dist != -1 &&
                         dist <= BuildRange )
                    {
                        debugCode = 700;
                        //And we're on the right planet and in the right place to build it!

                        PlanetFaction pFaction = sapper.PlanetFaction;
                        ArcenPoint finalPoint = sapper.Planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, data.UnitToBuild, data.LocationToBuild, FInt.FromParts( 0, 50 ), FInt.FromParts( 0, 100 ) );
                        byte markLevel = 1;
                        if ( data.UnitToBuild.GetHasTag( "SapperAdvancedDefensiveStructure" ) ||
                             data.UnitToBuild.GetHasTag( "SapperBasicDefensiveStructure" ) ||
                             data.UnitToBuild.GetHasTag( "SapperBeachheader" ) )
                            markLevel = GetMarkLevelForStructure( sapper );
                        debugCode = 800;
                        GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, data.UnitToBuild, markLevel,
                                                                                                      pFaction.Faction.LooseFleet, 0, finalPoint, Context, "Sappers-BuildStructure" );
                        SappersPerUnitBaseInfo newData = newEntity.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                        //TODO: lets set the amount of mmonstone/bloodstone/metal to get here, as well as the time for flowering
                        if ( sapper.TypeData.GetHasTag( "SapperBeachheadConstructor" ) )
                        {
                            newData.IsBeachheadTurret = true;
                        }
                        debugCode = 900;
                        if ( data.UnitToBuild.GetHasTag( "SapperBaseCrystal" ) )
                        {
                            int divisor = 1; //crystals flower faster when there's only one habitat, to let them get started faster
                            if ( BaseInfo.SapperHabitats.Count <= 1 )
                                divisor = 2;
                            newData.FloweringTime = World_AIW2.Instance.GameSecond + ( BaseInfo.Difficulty.TimeForCrystalToFlower + Context.RandomToUse.Next( 0, BaseInfo.Difficulty.TimeForCrystalToFlower / 10 ) ) / divisor;
                        }
                        if ( tracing )
                            tracingBuffer.Add( sapper.ToStringWithPlanet() + " has just built " + newEntity ).Add( "\n" );
                        FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
                        return true;
                    }
                    debugCode = 1000;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit an exception in BuildStructureIfPossible debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
            return false;
        }
        public bool DispatchToBuildCrystalsIfPossible( GameEntity_Squad sapper, SappersPerUnitBaseInfo data, ArcenHostOnlySimContext Context )
        {
            if ( Context.RandomToUse.Next(0, 100) < 50 )
                return false; //we don't always build crystals even if we can; sometimes we also want to check whether we can build something more interesting
            if ( data.UnitToBuild == null && data.MetalStored >= BaseInfo.Difficulty.BaseCrystalCost )
            {
                //Find a suitable planet/location to build
                Planet planet =  GetPlanetToBuildCrystal( sapper, Context );
                if ( planet != null )
                {
                    data.UnitToBuild = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "SapperBaseCrystal" );
                    data.PlanetIdx = planet.Index;
                    data.PlanetToBuildOn = planet;
                    data.LocationToBuild = planet.GetSafePlacementPointAroundPlanetCenter( Context, data.UnitToBuild, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 850 ) );
                    return true;
                }
            }
            
            return false;
        }
        private static readonly List<ArcenPoint> WorkingWormholeList = List<ArcenPoint>.Create_WillNeverBeGCed( 50, "SappersFactionDeepInfo-WorkingWormholeList" );
        public bool DispatchToBuildDefensesIfPossible( GameEntity_Squad sapper, SappersPerUnitBaseInfo data, ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Sappers-DispatchToBuildDefensesIfPossible-trace", 10f ) : null;
            GameEntityTypeData typeData = null;
            Planet planet = null;
            if ( tracing )
                tracingBuffer.Add("\tCan we build defenses?\n"); 
            //If we have a UnitToBuild then we are ready to build it immediately.
            if ( data.UnitToBuild == null )
            {
                int retries = 5;
                while ( retries-- > 0 )
                {
                    //Try to build a cheap turret. If we can afford it, and have a suitable planet, set the UnitToBuild and PlanetIdx, then figure out where to build the structure
                    string TagToTryToBuild = data.TagForUnitToBuildNext;
                    if ( TagToTryToBuild == "" )
                    {
                        int random = Context.RandomToUse.Next(0, 100);
                        if ( tracing )
                            tracingBuffer.Add("\tChoosing what type of defensive structure to build next, random " + random +". MaxHabitats " + BaseInfo.Difficulty.MaxHabitats +" current mark level " + sapper.CurrentMarkLevel + " mark level for advanced " + BaseInfo.Difficulty.SapperMarkLevelForAdvanced + "\n"); 
                        if ( BaseInfo.SapperHabitats.Count == 1 &&
                             BaseInfo.Difficulty.MaxHabitats > 1 &&
                             FactionUtilityMethods.Instance.GetNumPlanetsControlledByAllies( AttachedFaction ) >= 3 )
                        {
                            //make sure we get a second habitat early
                            TagToTryToBuild = "SapperHabitat";
                        }
                        else if ( random <= 50 )
                        {
                            TagToTryToBuild = "SapperBasicDefensiveStructure";
                        }
                        else if ( random <= 70 )
                        {
                            if ( sapper.CurrentMarkLevel >= BaseInfo.Difficulty.SapperMarkLevelForAdvanced )
                                TagToTryToBuild = "SapperAdvancedDefensiveStructure";
                        }
                        else if ( random <= 90 )
                        {
                            if ( sapper.CurrentMarkLevel >= BaseInfo.Difficulty.SapperMarkLevelForBeachheader )
                                TagToTryToBuild = "SapperBeachheader";
                        }
                        else { if ( BaseInfo.SapperHabitats.Count < BaseInfo.Difficulty.MaxHabitats )
                                TagToTryToBuild = "SapperHabitat";
                        }
                        if ( TagToTryToBuild == "" )
                            TagToTryToBuild = "SapperBasicDefensiveStructure";

                    }
                    else if ( tracing )
                        tracingBuffer.Add("\tWe have already chosen to build a " + TagToTryToBuild +"\n"); 
                    if ( tracing )
                        tracingBuffer.Add("\tWe are considering building a " + TagToTryToBuild +"\n"); 
                    planet = GetPlanetForCombatStructure( TagToTryToBuild, null, sapper, data, Context );
                    if ( planet == null )
                    {
                        if ( tracing )
                            tracingBuffer.Add("\t\tWe have no planets on which we could build a " + TagToTryToBuild + ".\n");
                        data.TagForUnitToBuildNext = "";
                        typeData = null;
                        continue;
                    }

                    typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, TagToTryToBuild );
                    if ( typeData == null )
                        throw new Exception("Could not find sapper unit with tag " + TagToTryToBuild );
                    if ( typeData.MoonstoneCost > data.MoonstoneStored || typeData.BloodstoneCost > data.BloodstoneStored ||
                         typeData.CostForAIToPurchase > data.MetalStored )
                    {
                        //we could build this but can't afford it yet. Sometimes we want to save up for something expensive, but not until we have a couple sappers
                        if ( BaseInfo.Sappers.Count > 1 )
                            data.TagForUnitToBuildNext = TagToTryToBuild;
                        if ( tracing )
                            tracingBuffer.Add("\tWe can build a " + typeData.GetDisplayName() + " on " + planet.Name +", but we can't afford it yet. We have " + data.MetalStored + " metal, " + data.MoonstoneStored + " moonstone and " + data.BloodstoneStored + " bloodstone. The cost for this is " + typeData.CostForAIToPurchase  + " metal, " + data.MoonstoneStored + " moonstone and " + typeData.BloodstoneCost + " bloodstone. So wait.\n");
                        typeData = null;
                        continue;
                    }
                }
                if ( typeData == null )
                {
                    if ( tracing )
                        tracingBuffer.Add("\tWe could not find anything to build\n");
                    FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
                    return false;
                }
                if ( tracing )
                    tracingBuffer.Add("\tWe are going to build a " + typeData.GetDisplayName() + " on " + planet.Name +"\n");
                data.UnitToBuild = typeData;
                data.PlanetIdx = planet.Index;
                data.PlanetToBuildOn = planet;

                //There are some rules about where to where we can place structures with certain tags, but for now just put them anywhere
                //eventually this following if() will be active again, since those are allowed to go anywhere
                if ( typeData.GetHasTag( "SapperBeachheader" ) || typeData.GetHasTag( "SapperAdvancedDefensiveStructure" ) ||
                     typeData.GetHasTag( "SapperHabitat" ))
                {
                    //just put someplace interestingly random
                    data.LocationToBuild = planet.GetSafePlacementPointAroundPlanetCenter( Context, typeData, FInt.FromParts( 0, 100 ), FInt.FromParts( 0, 650 ) );
                }
                if ( typeData.GetHasTag( "PlaceOnGravwellEdge" ) )
                {
                    AngleDegrees angle = AngleDegrees.Create( (float)Context.RandomToUse.Next( 1, 360 ) );
                    ArcenPoint center = Engine_AIW2.Instance.CombatCenter;
                    float warpInMultiplier = 0.95f;
                    data.LocationToBuild = center.GetPointAtAngleAndDistance( angle, (int)(planet.GravWellSize.DistanceScale_GravwellRadius * warpInMultiplier) );
                }

                //some structures have both of these, some have only one make sure we pick appropriately
                ArcenPoint wormholePoint = ArcenPoint.ZeroZeroPoint;
                ArcenPoint importantPoint = ArcenPoint.ZeroZeroPoint;
                if ( typeData.GetHasTag( "PlaceNearWormhole" ) )
                {
                    AngleDegrees angle = AngleDegrees.Create( (float)Context.RandomToUse.Next( 1, 360 ) );
                    GameEntityTypeData.MarkLevelStats markLevelData = typeData.MarkStatsFor( sapper.CurrentMarkLevel );
                    int range = markLevelData.Computed_BaseLongestWeaponRange;
                    wormholePoint = FactionUtilityMethods.Instance.GetRandomWormholeOnPlanet(planet, Context, WorkingWormholeList);
                }
                if ( typeData.GetHasTag("PlaceNearImportantStructure") )
                {
                    AngleDegrees angle = AngleDegrees.Create( (float)Context.RandomToUse.Next( 1, 360 ) );
                    GameEntityTypeData.MarkLevelStats markLevelData = typeData.MarkStatsFor( sapper.CurrentMarkLevel );
                    int range = markLevelData.Computed_BaseLongestWeaponRange;
                    importantPoint = GetSapperImportantPoint( planet, Context);
                }
                if ( wormholePoint != ArcenPoint.ZeroZeroPoint || importantPoint != ArcenPoint.ZeroZeroPoint )
                {
                    int random = Context.RandomToUse.Next( 0, 100 );
                    if ( importantPoint == ArcenPoint.ZeroZeroPoint )
                        data.LocationToBuild = wormholePoint;
                    else if ( wormholePoint == ArcenPoint.ZeroZeroPoint )
                        data.LocationToBuild = importantPoint;
                    else if ( random < 50 )
                        data.LocationToBuild = importantPoint;
                    else
                        data.LocationToBuild = wormholePoint;
                }
                if ( data.LocationToBuild == ArcenPoint.ZeroZeroPoint )
                {
                    throw new Exception("Could not find location on which to build a " + typeData.GetDisplayName() );
                }
                //TODO: add additional placement rules
                //Additional notes:

                //A bunch of this logic goes into its own function; check for the TODO
                
                //Check the GameEntityTypeData.MoonstoneCost and BloodstoneCost
                //to see whether we can afford this.
                //Note that if we choose to build a cheap defensive structure,
                //   First, we check whether we are allowed to build a new Sapper Habitat (and noone else is already going to build one)
                //      If so then some % of the time we will choose to build a new habitat (using TagForUnitToBuildNext)

                //   If we haven't picked a structure yet, we check if it seems reasonable for us to build a Strong defensive structure or an Offensive structure. These are more expensive
                //     If it seems reasonable then we set TagForUnitToBuildNext so we build a suitable unit

                //Rules for placing defensive structures
                //First, use the 'WithinXHops' logic from GetPlanetToBuildCrystal() to make sure we don't go anywhere scary
                //
                //The Sappers are allowed to build cheap defensive structures on planets owned by their allies or neutered/neutral planets immediately adjacent (and without no enemies)
                //  They have a preference for building on their allies planets though
                //  These structures have a cap based on BaseInfo.Difficulty and AIP and Spire (something like "3 structures base + 1 per 75 AIP + 3 per spire city")
                //    These structures include turrets and things like tractor beams
                //    Also add some logic for "Short/Medium/Sniper" turrets and some logic about where it would make sense to put them

                //Only sappers at Mark >= 3 can build defensive structures
                //   An additional restriction: no more than one Defensive structure per planet, and must be on a friendly planet

                //Only sappers at Mark >= 5 can build offensive structures, and there is a per-Difficulty/AIP/Spire cap on how many of structures can be built
                //   An additional restriction: no more than one Offensive structure per planet, and must be on a friendly planet
                
                //Offensive structures will build "Turret Spawners" which fly to a planet where the player is fighting and drop turrets as a beachhead

                //Strong Defensive Structures are Watchtowers, which spawn ships to go fight enemies on adjacent planets.
                //  I think 2 or 3 types of watchtowers (they spawn ships with different tags). These types are all intended to be equal in power.
                //  Maybe 2 groups of fleetships and one for frigates?
            }
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
            return false;
        }
        public void HandleJournals( ArcenHostOnlySimContext Context )
        {
            if ( BaseInfo.PlayerAllied && BaseInfo.Sappers.Count > 0 )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "EOTA_Sappers_Overview_PlayerAllied", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );       
        }
        public readonly List<SafeSquadWrapper> WorkingSquadList = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "SappersFactionDeepInfo-WorkingSquadList" );
        public ArcenPoint GetSapperImportantPoint( Planet planet, ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Sappers-GetSapperImportantPoint-trace", 10f ) : null;

            //returns the location of some point that the Sappers would like to defend
            WorkingSquadList.Clear();
            List<SafeSquadWrapper> sapperHabitats = this.BaseInfo.SapperHabitats.GetDisplayList();
            for ( int i = 0; i < sapperHabitats.Count; i++ )
            {
                if ( sapperHabitats[i].Planet == planet )
                    WorkingSquadList.Add( sapperHabitats[i] );
            }
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
            foreach ( PlanetFaction otherFaction in pFaction.RelatedFactions( FactionRelationship.FactionsThatAreFriendlyTowardsMe ) )
            {
                foreach ( GameEntity_Squad entity in otherFaction.Entities.Squads( EntityRollupType.CommandStation ) )
                {
                    WorkingSquadList.Add( entity );
                }
                foreach ( GameEntity_Squad entity in otherFaction.Entities.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    WorkingSquadList.Add( entity );
                }
                foreach ( GameEntity_Squad entity in otherFaction.Entities.Squads( EntityRollupType.AICounterattackEnablers ) )
                {
                    WorkingSquadList.Add( entity );
                }
                foreach ( GameEntity_Squad entity in otherFaction.Entities.Squads( "SapperImportantPoint" ) )
                {
                    WorkingSquadList.Add( entity );
                }
            }

            if ( WorkingSquadList.Count == 0 )
            {
                //this is unlikely but possible I suppose? Just use a wormhole
                return FactionUtilityMethods.Instance.GetRandomWormholeOnPlanet(planet, Context, WorkingWormholeList);
            }
            GameEntity_Squad importantPoint = WorkingSquadList[Context.RandomToUse.Next(0, WorkingSquadList.Count)].GetSquad();
            if ( importantPoint == null )
                return Engine_AIW2.Instance.CombatCenter;
            if ( tracing )
                tracingBuffer.Add("\tWe are going to build a defensive structure near " + importantPoint.ToStringWithPlanetAndOwner() ).Add("\n");
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
            return importantPoint.WorldLocation;
        }
        public void GetSappersAlreadyGoingToBuild( GameEntity_Squad thisSapper, Planet planet, ArcenHostOnlySimContext Context, out int beachheader, out int advanced, out int basic, out int habitat)
        {
            //we have these variables because you can't directly assign into out parameters from inside an anonymouse method like the below one.
            int basicLocal = 0;
            int advancedLocal = 0;
            int beachheaderLocal = 0;
            int habitatLocal = 0;
            foreach ( GameEntity_Squad sapper in this.BaseInfo.Sappers.DisplaySquads() )
            {
                if ( thisSapper == sapper )
                    continue;
                SappersPerUnitBaseInfo otherData = sapper.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                if ( otherData.PlanetToBuildOn != planet )
                    continue;
                GameEntityTypeData typeDataToCheck = otherData.UnitToBuild;
                if ( typeDataToCheck == null && !String.IsNullOrEmpty( otherData.TagForUnitToBuildNext ) )
                    typeDataToCheck = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, otherData.TagForUnitToBuildNext );

                if ( typeDataToCheck != null )
                {
                    if ( typeDataToCheck.GetHasTag( "SapperBasicDefensiveStructure" ) )
                        basicLocal++;
                    if ( typeDataToCheck.GetHasTag( "SapperBasicDefensiveStructure" ) )
                        advancedLocal++;
                    if ( typeDataToCheck.GetHasTag( "SapperBeachheader" ) )
                        beachheaderLocal++;
                    if ( typeDataToCheck.GetHasTag( "SapperHabitats" ) )
                        habitatLocal++;
                }
            }

            basic = basicLocal;
            advanced = advancedLocal;
            beachheader = beachheaderLocal;
            habitat = habitatLocal;
        }

        public bool DispatchToCollectResourcesIfPossible( GameEntity_Squad sapper, SappersPerUnitBaseInfo data, ArcenHostOnlySimContext Context, PerFactionPathCache PathCacheData )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Sappers-DispatchToCollectResourcesIfPossible-trace", 10f ) : null;
            if ( tracing )
                tracingBuffer.Add(sapper.ToStringWithPlanet() + " is trying to collect resources. There are " + BaseInfo.FloweredCrystals.Count + " flowered crystals.\n");

            if ( BaseInfo.FloweredCrystals.Count > 0 )
            {
                List<SafeSquadWrapper> sortedFloweredCrystals = GameEntity_Squad.GetTemporarySquadList( "Sappers-DispatchToCollectResourcesIfPossible-sortedFloweredCrystals", 10f );
                if ( sortedFloweredCrystals == null ) //blocked for teardown/shutdown; bail
                    return false;
                foreach ( GameEntity_Squad floweredCrystal in this.BaseInfo.FloweredCrystals.DisplaySquads() )
                {
                    sortedFloweredCrystals.Add( floweredCrystal );
                }
                cb_sappersSortSquad = sapper;
                sortedFloweredCrystals.Sort( static delegate ( SafeSquadWrapper L, SafeSquadWrapper R )
                {
                    int lHops = cb_sappersSortSquad.Planet.GetHopsTo( L.Planet );
                    int rHops = cb_sappersSortSquad.Planet.GetHopsTo( R.Planet );
                    return lHops.CompareTo( rHops );
                } );

                for ( int i = 0; i < sortedFloweredCrystals.Count; i++ )
                {
                    GameEntity_Squad flower = sortedFloweredCrystals[i].GetSquad();
                    if ( flower == null )
                        continue;
                    short hops;
                    int danger = Fireteam.GetDangerOfPath( sapper.PlanetFaction.Faction, Context, PathCacheData, sapper.Planet, flower.Planet, false, out hops );
                    if ( danger > 5000 )
                        continue;
                    bool anotherSapperIncoming = false;
                    foreach ( GameEntity_Squad otherSapper in this.BaseInfo.Sappers.DisplaySquads() )
                    {
                        if ( otherSapper == sapper )
                            continue;
                        SappersPerUnitBaseInfo otherData = otherSapper.GetExternalBaseInfoAs<SappersPerUnitBaseInfo>();
                        if ( otherData.DestinationForSappers.GetSquad() == flower )
                        {
                            anotherSapperIncoming = true;
                            break;
                        }
                    }
                    if ( anotherSapperIncoming )
                        continue;
                    data.DestinationForSappers = LazyLoadSquadWrapper.Create( flower );
                    GameEntity_Squad.ReleaseTemporarySquadList( sortedFloweredCrystals );
                    FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
                    return true;
                }
                GameEntity_Squad.ReleaseTemporarySquadList( sortedFloweredCrystals );
            }
            if ( BaseInfo.SapperHabitats.Count > 0 )
            {
                List<SafeSquadWrapper> sapperHabitats = this.BaseInfo.SapperHabitats.GetDisplayList();
                List<SafeSquadWrapper> sortedSapperHabitats = GameEntity_Squad.GetTemporarySquadList( "Sappers-DispatchToCollectResourcesIfPossible-sortedSapperHabitats", 10f );
                if ( sortedSapperHabitats == null ) //blocked for teardown/shutdown; bail
                    return false;
                //we can't sort the direct DisplayList without a cross-threading sync error
                sortedSapperHabitats.AddRange( sapperHabitats );
                sortedSapperHabitats.Sort( static delegate ( SafeSquadWrapper L, SafeSquadWrapper R )
                {
                    GameEntity_Squad lShip = L.GetSquad();
                    GameEntity_Squad rShip = R.GetSquad();
                    SappersPerUnitBaseInfo lData = lShip == null ? null : lShip.TryGetExternalBaseInfoAs<SappersPerUnitBaseInfo>();
                    SappersPerUnitBaseInfo rData = rShip == null ? null : rShip.TryGetExternalBaseInfoAs<SappersPerUnitBaseInfo>();
                    int lVal = lData == null ? 0 : lData.MetalStored;
                    int rVal = rData == null ? 0 : rData.MetalStored;
                    return lVal.CompareTo( rVal );
                } );

                for ( int i = 0; i < sortedSapperHabitats.Count; i++ )
                {
                    GameEntity_Squad habitat = sortedSapperHabitats[i].GetSquad();
                    if ( habitat == null )
                        continue;

                    short hops;
                    int danger = Fireteam.GetDangerOfPath( sapper.PlanetFaction.Faction, Context, PathCacheData, sapper.Planet, habitat.Planet, false, out hops );
                    if ( danger > 5000 )
                        continue;
                    bool anotherSapperIncoming = false;
                    foreach ( GameEntity_Squad otherSapper in this.BaseInfo.Sappers.DisplaySquads() )
                    {
                        if ( otherSapper == sapper )
                            continue;
                        SappersPerUnitBaseInfo otherData = otherSapper.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                        if ( otherData.DestinationForSappers.GetSquad() == habitat )
                        {
                            anotherSapperIncoming = true;
                            break;
                        }
                    }
                    if ( anotherSapperIncoming )
                        continue;
                    data.DestinationForSappers = LazyLoadSquadWrapper.Create( habitat );
                    if ( tracing )
                    {
                        GameEntity_Squad destination = data.DestinationForSappers.GetSquad();
                        tracingBuffer.Add( "\t" + sapper.ToStringWithPlanet() + " is off to " + (destination == null ? "null" : destination.ToStringWithPlanet() ) + "\n" );
                    }
                    GameEntity_Squad.ReleaseTemporarySquadList( sortedSapperHabitats );
                    FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
                    return true;
                }
                GameEntity_Squad.ReleaseTemporarySquadList( sortedSapperHabitats );
            }
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
            return false;
        }
        private Planet GetPlanetToBuildCrystal ( GameEntity_Squad sapper, ArcenHostOnlySimContext Context )
        {
            Planet output = null;
            List<SafeSquadWrapper> baseCrystals = this.BaseInfo.BaseCrystals.GetDisplayList();
            foreach ( Planet.PlanetAtHopDistance _phd in sapper.Planet.PlanetsWithinXHops( -1,
             delegate ( Planet secondaryPlanet )
             {
                 PlanetFaction spFaction = secondaryPlanet.GetPlanetFactionForFaction( AttachedFaction );
                 int friendlystrength = spFaction.DataByStance[FactionStance.Friendly].TotalStrength + spFaction.DataByStance[FactionStance.Self].TotalStrength;
                 if ( spFaction.DataByStance[FactionStance.Hostile].TotalStrength > friendlystrength / 2 )
                     return PropogationEvaluation.No;

                 return PropogationEvaluation.Yes;
             } ) )
            {
                Planet planet = _phd.Planet;
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                int friendlyStrength = pFaction.DataByStance[FactionStance.Friendly].TotalStrength + pFaction.DataByStance[FactionStance.Self].TotalStrength;
                int hostileStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                if ( hostileStrength > 2000 &&
                     friendlyStrength < hostileStrength )
                    continue;

                int crystalsForThisPlanet = 0;
                for ( int i = 0; i < baseCrystals.Count; i++ )
                {
                    if ( baseCrystals[i].Planet == planet )
                        crystalsForThisPlanet++;
                }
                foreach ( GameEntity_Squad floweredCrystal in this.BaseInfo.FloweredCrystals.DisplaySquads() )
                {
                    if ( floweredCrystal.Planet == planet )
                        crystalsForThisPlanet++;
                }

                if ( crystalsForThisPlanet > BaseInfo.Difficulty.CrystalsPerPlanet )
                    continue;
                output = planet;
                break;
            }
            return output;
        }
        private static readonly List<Planet> WorkingPotentialPlanets = List<Planet>.Create_WillNeverBeGCed( 100, "SappersFactionDeepInfo-WorkingPotentialPlanets" );
        private static readonly List<Planet> WorkingPreferredPotentialPlanets = List<Planet>.Create_WillNeverBeGCed( 100, "SappersFactionDeepInfo-WorkingPreferredPotentialPlanets" );
        private Planet GetPlanetForCombatStructure( string TagOrEmpty, GameEntityTypeData typeDataOrNull, GameEntity_Squad sapper, SappersPerUnitBaseInfo data, ArcenHostOnlySimContext Context )
        {
            int debugCode = 0;
            try
            {
                //This figures out whether we have a reasonable planet on which to build one of our combat structures
                //There are three types of structures when figuring out eligible planets; advanced and beahheader structures must be on friendly planets, basic can be adjacent to friendly planets
                //All types of structures have a per-planet cap.
                bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers );
                ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Sappers-GetPlanetForCombatStructure-trace", 10f ) : null;

                bool isBasicStructure = false;
                bool isAdvancedStructure = false;
                bool isBeachheaderStructure = false;
                bool isHabitatStructure = false;
                int numSet = 0;
                debugCode = 100;
                if ( TagOrEmpty == "SapperBasicDefensiveStructure" ||
                     (typeDataOrNull != null && typeDataOrNull.GetHasTag( "SapperBasicDefensiveStructure" )) )
                {
                    isBasicStructure = true;
                    TagOrEmpty = "SapperBasicDefensiveStructure";
                    numSet++;
                }
                if ( TagOrEmpty == "SapperAdvancedDefensiveStructure" ||
                     (typeDataOrNull != null && typeDataOrNull.GetHasTag( "SapperAdvancedDefensiveStructure" )) )
                {
                    isAdvancedStructure = true;
                    TagOrEmpty = "SapperAdvancedDefensiveStructure";
                    numSet++;
                }
                if ( TagOrEmpty == "SapperBeachheader" ||
                     (typeDataOrNull != null && typeDataOrNull.GetHasTag( "SapperBeachheader" )) )
                {
                    isBeachheaderStructure = true;
                    TagOrEmpty = "SapperBeachheader";
                    numSet++;
                }
                if ( TagOrEmpty == "SapperHabitat" ||
                     (typeDataOrNull != null && typeDataOrNull.GetHasTag( "SapperHabitat" )) )
                {
                    isHabitatStructure = true;
                    TagOrEmpty = "SapperHabitat";
                    numSet++;
                }
                debugCode = 200;
                if ( numSet != 1 )
                {
                    debugCode = 210;
                    //I could have used if/else for the above checks, but I do want to make sure there are no XML errors as I test
                    string typeD = "";
                    if ( typeDataOrNull != null )
                        typeD = typeDataOrNull.GetDisplayName();
                    throw new Exception( "Could not understand what intended planet options are for TagOrEmpty " + TagOrEmpty + " and entity " + typeD + "." );
                }
                debugCode = 220;
                if ( tracing )
                    tracingBuffer.Add( "\tChecking if " + sapper.ToStringWithPlanet() + " can build a " + TagOrEmpty ).Add( " isBasicStructure " + isBasicStructure + " isAdvancedStructure " + isAdvancedStructure + " isBeachheaderStructure " + isBeachheaderStructure + " isHabitatStructure " + isHabitatStructure ).Add( "\n" );
                debugCode = 230;
                WorkingPotentialPlanets.Clear();
                WorkingPreferredPotentialPlanets.Clear();
                debugCode = 300;
                bool verboseDebug = true;
                List<SafeSquadWrapper> sapperHabitats = this.BaseInfo.SapperHabitats.GetDisplayList();
                foreach ( Planet.PlanetAtHopDistance _phd in sapper.Planet.PlanetsWithinXHops( -1,
                 delegate ( Planet secondaryPlanet )
                 {
                     debugCode = 1100;
                     PlanetFaction spFaction = secondaryPlanet.GetPlanetFactionForFaction( AttachedFaction );
                     int friendlystrength = spFaction.DataByStance[FactionStance.Friendly].TotalStrength + spFaction.DataByStance[FactionStance.Self].TotalStrength;
                     if ( spFaction.DataByStance[FactionStance.Hostile].TotalStrength > 10000 &&
                          spFaction.DataByStance[FactionStance.Hostile].TotalStrength > friendlystrength / 2 )
                         return PropogationEvaluation.No;

                     return PropogationEvaluation.Yes;
                 } ) )
                {
                    Planet planet = _phd.Planet;
                    debugCode = 400;
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                    if ( tracing && verboseDebug )
                        tracingBuffer.Add( "\tChecking whether we can build on " + planet.Name ).Add( "\n" );
                    if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength > 0 )
                    {
                        if ( tracing && verboseDebug )
                            tracingBuffer.Add( "\t\tNo, hostile enemies" ).Add( "\n" );

                        continue;
                    }
                    if ( !planet.GetControllingOrInfluencingFaction().GetIsFriendlyTowards( AttachedFaction ) &&
                         (isAdvancedStructure || isBeachheaderStructure || isHabitatStructure) )
                    {
                        if ( tracing && verboseDebug )
                            tracingBuffer.Add( "\t\tNo, must be on a friendly planet" ).Add( "\n" );

                        continue; //advanced defensive structures and beahheaders must be on friendly planet
                    }
                    debugCode = 500;
                    //we are not on a friendly planet; are we adjacent to a friendly planet?
                    bool foundNearbyFriendly = false;
                    bool foundNearbyEnemy = false;
                    foreach ( Planet neighbor in planet.LinkedNeighborsAndSelf( false ) )
                    {
                    debugCode = 600;
                    if ( neighbor.GetControllingOrInfluencingFaction().GetIsFriendlyTowards( AttachedFaction ) )
                    {
                        foundNearbyFriendly = true;
                    }
                    }
                    foreach ( Planet neighbor in planet.LinkedNeighborsAndSelf( false ) )
                    {
                    debugCode = 600;
                    if ( !neighbor.GetControllingOrInfluencingFaction().GetIsFriendlyTowards( AttachedFaction ) )
                    {
                        foundNearbyEnemy = true;
                    }
                    }

                    if ( isBasicStructure && !foundNearbyFriendly )
                    {
                        if ( tracing && verboseDebug )
                            tracingBuffer.Add( "\t\tNo, this is a basic and not adjacent to a friendly" ).Add( "\n" );

                        continue;
                    }
                    debugCode = 700;
                    //Now check if we have exceeded our cap
                    int otherSappersGoingToBuildBasic;
                    int otherSappersGoingToBuildAdvanced;
                    int otherSappersGoingToBuildBeachheader;
                    int otherSappersGoingToBuildHabitats;
                    GetSappersAlreadyGoingToBuild( sapper, planet, Context, out otherSappersGoingToBuildBasic, out otherSappersGoingToBuildAdvanced, out otherSappersGoingToBuildBeachheader, out otherSappersGoingToBuildHabitats );
                    if ( isBasicStructure &&
                     (BaseInfo.BasicStructuresPerPlanet.Display[planet] + otherSappersGoingToBuildBasic >= BaseInfo.MaxBasicStructuresPerPlanet) )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\tNo, This is a basic structure, and we have exceeded the planet cap\n" );
                        continue;
                    }

                    if ( (isAdvancedStructure || isBeachheaderStructure) &&
                         BaseInfo.BasicStructuresPerPlanet.Display[planet] <= 2 )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\tNo, This is an advanced or beachheader structure, and we haven't build sufficient basic structures first\n" );
                        continue;
                    }
                    if ( isAdvancedStructure &&
                         BaseInfo.AdvancedStructuresPerPlanet.Display[planet] + otherSappersGoingToBuildAdvanced >= BaseInfo.MaxAdvancedStructuresPerPlanet)
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\tNo, This is an advanced structure, and we have exceeded the planet cap\n" );
                        continue;
                    }

                    if ( isBeachheaderStructure &&
                         BaseInfo.BeachheaderStructuresPerPlanet.Display[planet] + otherSappersGoingToBuildBeachheader >= BaseInfo.MaxBeachheaderStructuresPerPlanet)
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\tNo, This is a beachheader structure, and we have exceeded the planet cap\n" );

                        continue;
                    }

                    debugCode = 800;
                    if ( isHabitatStructure )
                    {
                        debugCode = 900;
                        //there are few habitats, so just brute force this check
                        bool habitatAlreadyHere = false;
                        for ( int i = 0; i < sapperHabitats.Count; i++ )
                        {
                            if ( sapperHabitats[i].Planet == planet )
                            {
                                if ( tracing && verboseDebug )
                                    tracingBuffer.Add( "\t\tNo, this is a habitat and we already have one on this planet" ).Add( "\n" );

                                habitatAlreadyHere = true;
                                break;
                            }
                        }
                        if ( habitatAlreadyHere )
                            continue;
                    }
                    debugCode = 1000;
                    if ( tracing )
                        tracingBuffer.Add( "\t\t" + planet.Name + " is an option\n" );
                    if ( (BaseInfo.PlayerAllied || BaseInfo.MinorFactionAllied) && (isBasicStructure) && foundNearbyEnemy &&
                     planet.GetControllingOrInfluencingFaction().GetIsFriendlyTowards( AttachedFaction ) )
                    {
                        if ( tracing && verboseDebug )
                            tracingBuffer.Add( "\t\tThis is a basic defensive structure, this planet is friendly and it's adjacent to a non-friendly. This is a preferred location." ).Add( "\n" );

                        WorkingPreferredPotentialPlanets.Add( planet );
                    }
                    else
                    {
                        if ( tracing && verboseDebug )
                            tracingBuffer.Add( "\t\tThis planet is on the normal path." ).Add( "\n" );
                        WorkingPotentialPlanets.Add( planet );
                    }
                    if ( WorkingPotentialPlanets.Count >= 4 && !BaseInfo.PlayerAllied )
                        break;
                    if ( WorkingPreferredPotentialPlanets.Count >= 2 && BaseInfo.PlayerAllied )
                        break;
                }
                debugCode = 1300;
                if ( WorkingPotentialPlanets.Count == 0 && WorkingPreferredPotentialPlanets.Count == 0 )
                {
                    FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
                    return null;
                }
                debugCode = 1400;
                for ( int i = 0; i < WorkingPreferredPotentialPlanets.Count; i++ )
                {
                    //Give the "Preferred" planets a bonus in priority, but not too high
                    if ( Context.RandomToUse.Next( 0, 100 ) < 30 )
                    {
                        FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
                        return WorkingPreferredPotentialPlanets[i];
                    }
                }
                for ( int i = 0; i < WorkingPotentialPlanets.Count; i++ )
                {
                    debugCode = 1500;
                    //prefer closer planets but allow for some randomness
                    if ( Context.RandomToUse.Next( 0, 100 ) < 50 )
                    {
                        FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
                        return WorkingPotentialPlanets[i];
                    }
                }
                debugCode = 1600;
                FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
                if ( WorkingPreferredPotentialPlanets.Count > 0 )
                    return WorkingPreferredPotentialPlanets[0];
                return WorkingPotentialPlanets[0];
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit an exception in GetPlanetForCombatStructure debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }

            return null;
        }
        private void UpdateNonSerializedFields( GameEntity_Squad sapper, SappersPerUnitBaseInfo data )
        {
            GameEntity_Squad destination = data.DestinationForSappers.GetSquad();
            if ( destination != null )
            {
                if ( destination.TypeData == null || destination.Planet == null )
                {
                    //our destination is gone; clear it
                    data.DestinationForSappers.Clear();
                }
                if ( destination.PlanetFaction.Faction.GetIsHostileTowards( sapper.PlanetFaction.Faction ) )
                {
                    //If our destination is actively hostile then something has gone wrong
                    data.DestinationForSappers.Clear();
                }
                if ( destination.GetHasBeenDestroyed() )
                {
                    //our destination has been destroyed; I believe there's a required "cooling off" period before
                    //a GameEntity_Squad is reused through pooling, so make sure to check for that
                    data.DestinationForSappers.Clear();
                }
            }
            if ( data.PlanetIdx != -1 && data.PlanetToBuildOn == null )
                data.PlanetToBuildOn = World_AIW2.Instance.GetPlanetByIndex( (short)data.PlanetIdx );
        }
        private const int HarvestRange = 250;
        public bool CollectResourcesFromDestinationIfPossible(GameEntity_Squad sapper, SappersPerUnitBaseInfo data, ArcenHostOnlySimContext Context )
        {
            if ( sapper == null )
                return false;

            GameEntity_Squad destination = data.DestinationForSappers.GetSquad();
            if ( destination == null )
                return false;
            if ( sapper.Planet != destination.Planet )
                return false;
            int distance = Mat.DistanceBetweenPointsImprecise( sapper.WorldLocation, destination.WorldLocation );
            if ( distance > HarvestRange )
                return false;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Sappers-CollectResourcesFromDestinationIfPossible-trace", 10f ) : null;

            int multiplier = 1;
            if ( BaseInfo.ExtraStrongMode )
                multiplier *= 2;
            if ( tracing )
                tracingBuffer.Add( sapper.ToStringWithPlanet() + " is collecting resources from " + destination ).Add("\n");
            if ( destination.TypeData.GetHasTag("SapperBloodstoneCrystal") )
            {
                data.BloodstoneStored += BaseInfo.Difficulty.ResourceFromHarvestingCrystal * multiplier;
                destination.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
            }
            else if ( destination.TypeData.GetHasTag("SapperMetallicCrystal") )
            {
                data.MetalStored += BaseInfo.Difficulty.ResourceFromHarvestingCrystal * multiplier;
                destination.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
            }
            else if ( destination.TypeData.GetHasTag("SapperMoonstoneCrystal") )
            {
                data.MoonstoneStored += BaseInfo.Difficulty.ResourceFromHarvestingCrystal * multiplier;
                destination.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
            }
            else if ( destination.TypeData.GetHasTag("SapperHabitat") )
            {
                SappersPerUnitBaseInfo habData = destination.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                if ( habData.MetalStored > 0 )
                {
                    data.MetalStored += habData.MetalStored;
                    habData.MetalStored = 0;
                }
            }
            else
                throw new Exception("Unknown sapper destination " + destination.ToStringWithPlanetAndOwner() + " was reached but the code did not know how to handle it.");
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
            return true;
        }

        public void UpdateCrystals( ArcenHostOnlySimContext Context )
        {

            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Sappers-UpdateCrystals-trace", 10f ) : null;

            List<SafeSquadWrapper> baseCrystals = this.BaseInfo.BaseCrystals.GetDisplayList();
            for ( int i = 0; i < baseCrystals.Count; i++ )
            {
                GameEntity_Squad crystal = baseCrystals[i].GetSquad();
                if ( crystal == null )
                    continue;
                SappersPerUnitBaseInfo data = crystal.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                if ( data.FloweringTime != -1 &&
                     data.FloweringTime <= World_AIW2.Instance.GameSecond )
                {
                    PlanetFaction pFaction = crystal.PlanetFaction;
                    GameEntityTypeData entityData = null;

                    //default is random crystal, but lets do a bit of logic to see if we can give a better option
                    bool usePreferred = false;
                    int metalNeeded = 0;
                    int bloodstoneNeeded = 0;
                    int moonstoneNeeded = 0;
                    bool buildingHabitat = false;
                    //ArcenDebugging.ArcenDebugLogSingleLine("Flowering " + crystal.ToStringWithPlanetAndOwner(), Verbosity.DoNotShow );
                    foreach ( GameEntity_Squad sapper in this.BaseInfo.Sappers.DisplaySquads() )
                    {
                        SappersPerUnitBaseInfo sapperBaseInfo = sapper.GetExternalBaseInfoAs<SappersPerUnitBaseInfo>();
                        if ( String.IsNullOrEmpty( sapperBaseInfo.TagForUnitToBuildNext ) )
                            continue;
                        GameEntityTypeData testUnit = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, sapperBaseInfo.TagForUnitToBuildNext );
                        if ( testUnit.GetHasTag( "SapperHabitat" ) )
                            buildingHabitat = true;
                        if ( testUnit.MoonstoneCost > 0 && testUnit.MoonstoneCost > sapperBaseInfo.MoonstoneStored )
                            moonstoneNeeded += testUnit.MoonstoneCost - sapperBaseInfo.MoonstoneStored;
                        if ( testUnit.BloodstoneCost > 0 && testUnit.BloodstoneCost > sapperBaseInfo.BloodstoneStored )
                            bloodstoneNeeded += testUnit.BloodstoneCost - sapperBaseInfo.BloodstoneStored;
                        if ( testUnit.CostForAIToPurchase > sapperBaseInfo.MetalStored )
                        {
                            metalNeeded += testUnit.CostForAIToPurchase - sapperBaseInfo.MetalStored;
                        }
                    }
                    //The rule is "We use a random crystal type sometimes and sometimes we pick what we need most"
                    //Note that we prioritize getting a second habitat early
                    if ( ( metalNeeded > 0 || bloodstoneNeeded > 0 || moonstoneNeeded > 0 ) &&
                         (Context.RandomToUse.Next(0, 100) < 80 || (BaseInfo.SapperHabitats.Count <= 1 && buildingHabitat) ) )
                         usePreferred = true; //get that second habitat fast
                    //ArcenDebugging.ArcenDebugLogSingleLine("\tNeeded resources: moon " + moonstoneNeeded + " blood " + bloodstoneNeeded + " metal " + metalNeeded + " use preferred? " + usePreferred, Verbosity.DoNotShow );
                    if (!usePreferred )
                    {
                        //  ArcenDebugging.ArcenDebugLogSingleLine("\tpath A; random", Verbosity.DoNotShow );
                        entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "SapperFloweredCrystal" );
                    }
                    else if ( metalNeeded > bloodstoneNeeded && metalNeeded > moonstoneNeeded )
                    {
                        //ArcenDebugging.ArcenDebugLogSingleLine("\tpath B; metal", Verbosity.DoNotShow );
                        entityData = GameEntityTypeDataTable.Instance.GetRowByName( "SapperMetallicCrystal" );
                    }
                    else if ( bloodstoneNeeded > moonstoneNeeded )
                    {
                        //ArcenDebugging.ArcenDebugLogSingleLine("\tpath C; blood", Verbosity.DoNotShow );
                        entityData = GameEntityTypeDataTable.Instance.GetRowByName( "SapperBloodstoneCrystal" );
                    }
                    else if ( moonstoneNeeded > bloodstoneNeeded )
                    {
                        //ArcenDebugging.ArcenDebugLogSingleLine("\tpath D; moon", Verbosity.DoNotShow );
                        entityData = GameEntityTypeDataTable.Instance.GetRowByName( "SapperMoonstoneCrystal" );
                    }
                    else
                    {
                        //ArcenDebugging.ArcenDebugLogSingleLine("\tpath E; random", Verbosity.DoNotShow );
                        entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "SapperFloweredCrystal" ); //fallback case is "just be random
                    }
                    //ArcenDebugging.ArcenDebugLogSingleLine("\tChose " + entityData.GetDisplayName(), Verbosity.DoNotShow );
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, crystal.CurrentMarkLevel,
                                                                                                  pFaction.Faction.LooseFleet, 0, crystal.WorldLocation, Context, "Sappers-UpdateCrystal" );
                    SappersPerUnitBaseInfo newData = newEntity.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                    BaseInfo.FloweredCrystals.AddToDisplayList(newEntity);
                    if ( tracing )
                        tracingBuffer.Add( crystal.ToStringWithPlanet() + " has just flowered into " + newEntity ).Add(".\n");
                    data.FloweringTime = -1; //sometimes it seems like we can double-flower wihch is weird. So make it harder for that to happen
                    crystal.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                }
            }
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
        }
        public void JoinAlliesIfNecessary(ArcenHostOnlySimContext Context )
        {
        
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //No client stuff

            if ( BaseInfo.SapperHabitats.Count != 0 || BaseInfo.Sappers.Count != 0)
                return; //we have a presence on the map
            if ( BaseInfo.TimeLastHadSappers != -1 &&
                 BaseInfo.TimeLastHadSappers + BaseInfo.Difficulty.TimeBetweenHabitatRespawns < World_AIW2.Instance.GameSecond )
                return; //we had a presence on the map recently
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Sappers-JoinAlliesIfNecessary-trace", 10f ) : null;

            if ( tracing )
                tracingBuffer.Add("Time to spawn some new sapper habitats. We have " + BaseInfo.SapperHabitats.Count + " now!\n");

            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "SapperHabitat" );
            if ( BaseInfo.PlayerAllied || BaseInfo.AIAllied )
            {
                foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    if ( entity.GetIsFriendlyTowards_Safe( AttachedFaction ) )
                    {
                        entity.Planet.Mapgen_SeedEntity( Context, AttachedFaction, entityData, PlanetSeedingZone.MostAnywhere );
                    }
                }
            }
            else
            {
                //We are minor faction allied, so see if our allies A. own a planet and B. there are no enemies 
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    Faction influencingFaction = World_AIW2.Instance.GetFactionByIndex( planet.PrimaryInfluencingFaction );
                    if ( influencingFaction == null || !influencingFaction.GetIsFriendlyTowards( AttachedFaction ) )
                        continue;

                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                    if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength == 0 )
                    {
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            if ( planet.IntelLevel > PlanetIntelLevel.Unexplored )
                            {
                                PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                                if ( chatHandlerOrNull != null )
                                    chatHandlerOrNull.PlanetToView = planet;

                                World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "Sappers</color> have joined the " + 
                                    influencingFaction.StartFactionColourForLog() + influencingFaction.GetDisplayName() + "</color> forces on " + planet.Name, ChatType.LogToCentralChat, chatHandlerOrNull );
                            }
                        }

                        planet.Mapgen_SeedEntity( Context, AttachedFaction, entityData, PlanetSeedingZone.MostAnywhere );

                        break;
                    }
                }
            }
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
        }


        //Long Range Planning (LRP) starts here. It currently doesn't do anything, but leaving it here just in case we want it someday
        //Note that the class itself says "Never calls LRP"

        public static readonly List<SafeSquadWrapper> SapperHabitatsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "SappersFactionDeepInfo-SapperHabitatsLRP" );
        public static readonly List<SafeSquadWrapper> SappersLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "SappersFactionDeepInfo-SappersLRP" );
        public static readonly List<SafeSquadWrapper> WatchtowersLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "SappersFactionDeepInfo-WatchtowersLRP" );
        public static readonly List<SafeSquadWrapper> BeachheadersLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "SappersFactionDeepInfo-BeachheadersLRP" );
        public static readonly List<SafeSquadWrapper> ConstructorsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "SappersFactionDeepInfo-ConstructorsLRP" );
        public readonly List<SafeSquadWrapper> CombatShipsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "SappersFactionDeepInfo-CombatShipsLRP" );
        public readonly List<SafeSquadWrapper> CombatShipsGoingBackToWatchtowerLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "SappersFactionDeepInfo-CombatShipsGoingBackToWatchtowerLRP" );
        public static readonly DictionaryOfLists<Planet, SafeSquadWrapper> UnassignedShipsByPlanet = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 60, "SappersFactionDeepInfo-UnassignedShipsByPlanet" );

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            #region Tracing
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Sappers-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            #endregion
            int debugCode = 0;

            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                debugCode = 100;
                SappersLRP.Clear();
                SapperHabitatsLRP.Clear();
                WatchtowersLRP.Clear();
                BeachheadersLRP.Clear();
                ConstructorsLRP.Clear();
                CombatShipsLRP.Clear();
                CombatShipsGoingBackToWatchtowerLRP.Clear();
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "Sapper" ) )
                {
                    SappersLRP.Add( entity );
                }
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "SapperHabitatsLRP" ) )
                {
                    SapperHabitatsLRP.Add( entity );
                }
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "SapperWatchtower" ) )
                {
                    WatchtowersLRP.Add( entity );
                }
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "SapperBeachheader" ) )
                {
                    BeachheadersLRP.Add( entity );
                }
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "SapperBeachheadConstructor" ) )
                {
                    ConstructorsLRP.Add( entity );
                }
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "SapperCombatShip" ) )
                {
                    CombatShipsLRP.Add( entity );
                }

                if ( tracing )
                    tracingBuffer.Add( "We have " + SappersLRP.Count + " sappers in LRP and " + CombatShipsLRP.Count + " combat ships active.\n" );
                debugCode = 200;
                for ( int i = 0; i < SappersLRP.Count; i++ )
                {
                    debugCode = 300;
                    GameEntity_Squad sapper = SappersLRP[i].GetSquad();
                    if ( sapper == null )
                        continue;
                    SappersPerUnitBaseInfo data = sapper.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                    Planet destPlanet = null;
                    if ( sapper.HasQueuedOrders() )
                        continue;

                    if ( tracing )
                        tracingBuffer.Add( "LRP processing " + sapper.ToStringWithPlanet() ).Add( "\n" );

                    GameEntity_Squad destination = data.DestinationForSappers.GetSquad();

                    //if we need to go someplace on this planet, go there
                    ArcenPoint destPoint = ArcenPoint.ZeroZeroPoint;
                    if ( data.PlanetToBuildOn != null && data.UnitToBuild != null &&
                         data.PlanetToBuildOn == sapper.Planet &&
                         data.LocationToBuild != ArcenPoint.ZeroZeroPoint )
                    {
                        debugCode = 510;
                        destPoint = data.LocationToBuild;
                        if ( tracing )
                            tracingBuffer.Add( "\tIssue orders to go to a specific location on " + sapper.Planet.Name ).Add( " to build a " + data.UnitToBuild.GetDisplayName() + "\n" );
                    }
                    else if ( destination != null && sapper.Planet == destination.Planet )
                    {
                        debugCode = 520;
                        destPoint = destination.WorldLocation;
                        if ( tracing )
                            tracingBuffer.Add( "\tIssue orders to go to a the location of  " + destination ).Add( "\n" );

                    }
                    debugCode = 600;
                    if ( destPoint != ArcenPoint.ZeroZeroPoint )
                    {
                        debugCode = 540;
                        if ( tracing )
                            tracingBuffer.Add( "\tship dispatched\n" );

                        SendShipToLocation( sapper, destPoint, Context );
                        debugCode = 610;
                        continue;
                    }
                    //if we need to go to a different planet, go there
                    if ( data.PlanetToBuildOn != null && data.UnitToBuild != null
                         && data.PlanetToBuildOn != sapper.Planet )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\tWe are going to build on " + data.PlanetToBuildOn.Name ).Add( "\n" );

                        destPlanet = data.PlanetToBuildOn;
                    }
                    else if ( destination != null && sapper.Planet != destination.Planet )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\tWe are going to " + destination.ToStringWithPlanet() ).Add( "\n" );

                        destPlanet = destination.Planet;
                    }
                    debugCode = 400;
                    if ( destPlanet != null )
                    {
                        // if ( tracing )
                        //     tracingBuffer.Add("\tIssue orders to go to planet " + destPlanet.Name ).Add("\n");

                        SendShipToPlanet( sapper, destPlanet, Context, pathingCacheData );//go to the planet
                        continue;
                    }
                    debugCode = 500;
                }
                for ( int i = 0; i < ConstructorsLRP.Count; i++ )
                {
                    debugCode = 300;
                    GameEntity_Squad constructor = ConstructorsLRP[i].GetSquad();
                    if ( constructor == null )
                        continue;
                    SappersPerUnitBaseInfo data = constructor.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                    Planet destPlanet = null;
                    if ( constructor.HasQueuedOrders() )
                        continue;
                    if ( tracing )
                        tracingBuffer.Add( "LRP processing " + constructor.ToStringWithPlanet() ).Add( "\n" );
                    //if we need to go someplace on this planet, go there
                    ArcenPoint destPoint = ArcenPoint.ZeroZeroPoint;
                    if ( data.PlanetToBuildOn != null && data.UnitToBuild != null &&
                         data.PlanetToBuildOn == constructor.Planet &&
                         data.LocationToBuild != ArcenPoint.ZeroZeroPoint )
                    {
                        debugCode = 510;
                        destPoint = data.LocationToBuild;
                        if ( tracing )
                            tracingBuffer.Add( "\tIssue orders to go to a specific location on " + constructor.Planet.Name ).Add( " to build a " + data.UnitToBuild.GetDisplayName() + "\n" );
                    }

                    debugCode = 600;
                    if ( destPoint != ArcenPoint.ZeroZeroPoint )
                    {
                        debugCode = 540;
                        if ( tracing )
                            tracingBuffer.Add( "\tconstructor dispatched\n" );

                        SendShipToLocation( constructor, destPoint, Context );
                        debugCode = 610;
                        continue;
                    }
                    //if we need to go to a different planet, go there
                    if ( data.PlanetToBuildOn != null && data.UnitToBuild != null
                         && data.PlanetToBuildOn != constructor.Planet )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\tWe are going to build on " + data.PlanetToBuildOn.Name ).Add( "\n" );

                        destPlanet = data.PlanetToBuildOn;
                    }
                    debugCode = 400;
                    if ( destPlanet != null )
                    {
                        // if ( tracing )
                        //     tracingBuffer.Add("\tIssue orders to go to planet " + destPlanet.Name ).Add("\n");

                        SendShipToPlanet( constructor, destPlanet, Context, pathingCacheData );//go to the planet
                        continue;
                    }
                    debugCode = 500;
                }
                if ( CombatShipsLRP.Count > 0 )
                {
                    if ( !BaseInfo.AnyWatchtowersActive )
                    {
                        CombatShipsGoingBackToWatchtowerLRP.Clear();
                        CombatShipsGoingBackToWatchtowerLRP.AddRange( CombatShipsLRP );
                    }
                    else
                    {
                        //Lets find some enemies to fight!
                        for ( int i = 0; i < CombatShipsLRP.Count; i++ )
                        {
                            debugCode = 300;
                            GameEntity_Squad entity = CombatShipsLRP[i].GetSquad();
                            if ( entity == null )
                                continue;
                            debugCode = 310;
                            SappersPerUnitBaseInfo data = entity.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                            if ( data == null )
                                continue;
                            debugCode = 320;
                            GameEntity_Squad watchtower = data.HomeWatchtower;
                            if ( watchtower == null )
                                continue; //we will be despawned by the sim code soon
                            debugCode = 330;
                            if ( entity.HasQueuedOrders() )
                                continue; //we're going someplace, so just go
                            debugCode = 400;
                            SappersPerUnitBaseInfo watchtowerData = watchtower.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                            Planet planetToHelp = watchtowerData.PlanetWatchtowerWantsToHelp;
                            if ( planetToHelp == null )
                            {
                                CombatShipsGoingBackToWatchtowerLRP.Add( entity );
                                continue;
                            }
                            debugCode = 500;
                            //we have a ship without orders that wants to go to a planet
                            if ( entity.Planet != planetToHelp )
                            {
                                debugCode = 600;
                                PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "SappersLRP", entity.Planet, planetToHelp, PathingMode.Default, Context, pathingCacheData );
                                if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                                {
                                    GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleSpecialMission], GameCommandSource.AnythingElse );
                                    debugCode = 700;
                                    command.RelatedString = "Spr_ToAttack";
                                    command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                                    command.ToBeQueued = false;
                                    for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                                        command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                                }
                            }
                        }
                    }
                    debugCode = 800;
                    if ( CombatShipsGoingBackToWatchtowerLRP.Count > 0 )
                        ReturnShipsToWatchtowers( CombatShipsGoingBackToWatchtowerLRP, Context, pathingCacheData );

                }

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception during sappers LRP debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            finally
            {
                pathingCacheData.ReturnToPool();

                #region Tracing
                if ( (tracing) && !tracingBuffer.GetIsEmpty() ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " " + AttachedFaction.FactionIndex ).Add( " LongRangePlanning trace ends at " ).Add( World_AIW2.Instance.GameSecond ).Add( "\n" );
                if ( (tracing) && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
            }
        }
        public static readonly DictionaryOfLists<SafeSquadWrapper, SafeSquadWrapper> GoingHome = DictionaryOfLists<SafeSquadWrapper, SafeSquadWrapper>.Create_WillNeverBeGCed( 40, 40, "SappersFactionDeepInfo-GoingHome" );
        public void ReturnShipsToWatchtowers( List<SafeSquadWrapper> ships, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Sappers );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Sappers-ReturnShipsToWatchtowers-trace", 10f ) : null;
            int debugCode = 0;
            try{
            debugCode = 100;
            GoingHome.Clear();
            if ( tracing )
                tracingBuffer.Add("We have " + ships.Count + " ships that need to return to watchtowers.\n");

            //This isn't the most efficient code, but it's hopefully run infrequently (only when under attack) and without too many ships
            for ( int i = 0; i < ships.Count; i++ )
            {
                debugCode = 200;
                
                GameEntity_Squad entity = ships[i].GetSquad();
                if ( entity == null )
                    continue;
                debugCode = 210;
                SappersPerUnitBaseInfo data = entity.CreateExternalBaseInfo<SappersPerUnitBaseInfo>( "SappersPerUnitBaseInfo" );
                if ( data == null )
                    throw new Exception("Could not make SappersPerUnitBaseInfo for " + entity.ToStringWithPlanetAndOwner());
                debugCode = 230;
                GameEntity_Squad watchtower = data.HomeWatchtower;
                if ( watchtower == null )
                    continue;
                debugCode = 240;
                GoingHome.Get( watchtower ).Add(entity);
            }
            debugCode = 300;
            foreach ( KeyValuePair<SafeSquadWrapper, List<SafeSquadWrapper>> pair in GoingHome )
            {
                debugCode = 400;
                if ( tracing )
                    tracingBuffer.Add("We have " + pair.Value.Count + " ships that need to return to " + pair.Key.ToStringWithPlanet() + ".\n");

                //can be batched with some work if necessary
                for ( int i = 0; i < pair.Value.Count; i++ )
                {
                    debugCode = 500;
                    GameEntity_Squad watchtower = pair.Key.GetSquad();
                    if ( watchtower == null )
                        break;
                    GameEntity_Squad entity = pair.Value[i].GetSquad();
                    if ( entity == null )
                        continue;
                    if ( entity.Planet == watchtower.Planet )
                    {
                        GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse );
                        moveCommand.PlanetOrderWasIssuedFrom = entity.Planet.Index;
                        moveCommand.RelatedPoints.Add( watchtower.WorldLocation );
                        moveCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );
                    }
                    else
                    {
                        PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "SappersReturnShipsToWatchtowers", 
                            entity.Planet, watchtower.Planet, PathingMode.Default, Context, PathCacheData );
                        if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                        {
                            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleSpecialMission], GameCommandSource.AnythingElse );
                            debugCode = 600;
                            command.RelatedString = "Spr_ToWatchtower";
                            command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                            command.ToBeQueued = false;
                            for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                                command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                        }
                    }
                }
            }
            debugCode = 700;
            }catch( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in ReturnShipsToWatchtowers debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            
        }
        public void SendShipToPlanet( GameEntity_Squad entity, Planet destination, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( entity.PlanetFaction.Faction, "SappersSendShipToPlanet", 
                entity.Planet, destination, PathingMode.Safest, Context, PathCacheData );
            if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
            {
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse );
                command.RelatedString = "Sapper_Dest";
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

        public override void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        {
            if ( entity == null )
                return;
            int debugStage = 0;
            try
            {
            
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in SpecialFaction_Sappers.DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly stage " + debugStage + "\n" + e, Verbosity.ShowAsError );
            }
        }
    }
}
