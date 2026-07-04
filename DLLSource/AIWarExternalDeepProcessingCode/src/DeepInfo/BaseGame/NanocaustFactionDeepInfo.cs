using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using UnityEngine;

namespace Arcen.AIW2.External
{
    /* Here are the tunables/globals for the Nanocaust faction.
             It is intended that all tuning of the Nanocaust could just be done here
             with no further code modification. I'd like to eventually put critical values
             into the XML so we can tune it without recompiling */
    public sealed class NanocaustFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        public NanocaustFactionBaseInfo BaseInfo;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<NanocaustFactionBaseInfo>();
        }

        protected override void Cleanup()
        {
            this.BaseInfo = null;

            //everything is reset every frame, we're good
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 2;

        #region ReactToHumansGettingFirstIntelOfPlanet
        public override void ReactToHumansGettingFirstIntelOfPlanet( Planet planet )
        {
            List<SafeSquadWrapper> centers = this.BaseInfo.NanobotCenters.GetDisplayList();
            for ( int i = 0; i < centers.Count; i++ )
            {
                if ( centers[i].Planet == planet )
                {
                    this.BaseInfo.humanVision = true;
                    Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.PlayerGainsNanocaustIntel );
                    return;
                }
            }
        }
        #endregion

        /* TEACHING_MOMENT: Modding note: For tags, they are set in the XML as comma seperated fields (tags="field1,field2")
           then you can reference these via the GameEntity.TypeData.Tags, which is a List<string> of the different fields.
           You then can use GetRandomRowWithTag or GetFirstWithTag and so on. Very handy. */

        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            // instead, do the logic in update which handles everything
            if (BaseInfo.SpawnPlanet)
                return;

            Tutorial tutorialData = World_AIW2.Instance.TutorialOrNull;
            if ( tutorialData != null && tutorialData.SkipNanocaustHivesAndBeacons )
                return;
            if ( AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "InvasionTime", true ) == "Immediate" )
            {
                AttachedFaction.HasDoneInvasionStyleAction = true;
                bool isSeeded = false;
                if ( AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "SpawnNearPlayer", true ) ||
                     AttachedFaction.IsVassal )
                {
                    //put me on the player homeworld
                    //TODO: this is probably not the right thing to do in MP, especially if you have a suzerain?
                    GameEntityTypeData entityData = null;
                    foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                    {
                        entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, NanocaustFactionBaseInfo.NANOCAUST_HIVE );
                        if ( entity.GetFactionTypeSafe() == FactionType.Player )
                        {
                            entity.Planet.Mapgen_SeedEntity( Context, AttachedFaction, entityData, PlanetSeedingZone.MostAnywhere );
                            isSeeded = true;
                            break;
                        }
                    }
                }
                if ( !isSeeded )
                {
                    StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, AttachedFaction, SpecialEntityType.None, NanocaustFactionBaseInfo.NANOCAUST_HIVE, SeedingType.HardcodedCount, 1,
                                                            MapGenCountPerPlanet.One, MapGenSeedStyle.FullUseByFaction, 3, 3, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal );
                }
            }

            // for(int i = 0; i < planetsSeededOn.Count; i++)
            // {
            //     //ArcenDebugging.ArcenDebugLogSingleLine("Nanocaust faction " + faction.FactionIndex + " hive seeded on " + planetsSeededOn[i].Name, Verbosity.DoNotShow );
            // }
            //StandardMapPopulator.ClearAllUnitsNotBelongingToThisFaction( planetsSeededOn, faction, true );
        }
        /* Fireteam stuff */
        public readonly ProtectedValDictionary<Planet, FireteamRegiment> TeamsAimedAtPlanet = ProtectedValDictionary<Planet, FireteamRegiment>.Create_WillNeverBeGCed( 100, "NanocaustFactionDeepInfo-TeamsAimedAtPlanet" );

        //these variables are only used in the lrp thread below, so are fine
        private readonly DictionaryOfLists<Planet, SafeSquadWrapper> unassignedThreatShipsByPlanet = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 30, "NanocaustFactionDeepInfo-unassignedThreatShipsByPlanet" );
        private readonly DictionaryOfLists<Planet, SafeSquadWrapper> shipsToStartPatrolling = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 30, "NanocaustFactionDeepInfo-shipsToStartPatrolling" );
        private readonly List<Planet> PotentialTargets = List<Planet>.Create_WillNeverBeGCed( 500, "NanocaustFactionDeepInfo-PotentialTargets" );
        private readonly List<Planet> AdjacentAlliedPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "NanocaustFactionDeepInfo-AdjacentAlliedPlanets" );
        private readonly List<Planet> AdjacentNonAlliedPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "NanocaustFactionDeepInfo-AdjacentNonAlliedPlanets" );
        private readonly List<Planet> HomeworldAdjacentPlanetsToClear = List<Planet>.Create_WillNeverBeGCed( 500, "NanocaustFactionDeepInfo-HomeworldAdjacentPlanetsToClear" );
        /* This function is used to determine where your ships should go */
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            bool localDebug = false;
            bool veryVerboseDebug = true;
            if ( AttachedFaction.TryGetExternalBaseInfoAs<NanocaustFactionBaseInfo>() == null )
            {
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "manager uninitialized, so don't do any LongRangePlanning until it gets initialized", Verbosity.DoNotShow );
                return;
            }
            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Nanocaust );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "NanoD-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            if ( tracing ) tracingBuffer.Add( this.TracingName ).Add( " DoLongRangePlanning trace begins for faction " ).Add( AttachedFaction.FactionIndex ).Add( "\n" );
            #endregion
            if ( !tracing )
                veryVerboseDebug = false;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                GameEntity_Squad hive = this.BaseInfo.Hive.Display.GetSquad();
                if ( hive != null )
                {
                    HomeworldAdjacentPlanetsToClear.Clear();
                    bool warnForAlliedNanocaustWaveSnipes = false;
                    int planetsConquered = 0;
                    int totalAdjacent = 0;
                    foreach ( Planet.PlanetAtHopDistance _phd in hive.Planet.PlanetsWithinXHops_NoFilters( 1 ) )
                    {
                        Planet planet = _phd.Planet;
                        totalAdjacent++;
                        EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( AttachedFaction );
                        if ( myFactionData[FactionStance.Hostile].TotalStrength >=
                             (myFactionData[FactionStance.Self].TotalStrength + myFactionData[FactionStance.Friendly].TotalStrength) / 2 )
                        {
                            //ArcenDebugging.ArcenDebugLogSingleLine("Adding planet to clear " + planet.Name + " path A; hostile strength " + myFactionData[FactionStance.Hostile].TotalStrength, Verbosity.DoNotShow );
                            HomeworldAdjacentPlanetsToClear.Add( planet );
                            continue;
                        }
                        else
                            planetsConquered++;
                        if ( this.BaseInfo.humanAllied && planet.GetControllingOrInfluencingFaction().GetIsHostileTowards( AttachedFaction ) &&
                             planet.GetControllingOrInfluencingFaction().Type == FactionType.AI && planet.PrimaryInfluencingFaction == AttachedFaction.FactionIndex )
                            warnForAlliedNanocaustWaveSnipes = true;
                        if ( planet.GetControllingOrInfluencingFaction().GetIsHostileTowards( AttachedFaction ) && !this.BaseInfo.humanAllied )
                        {
                            if ( !planet.UnderInfluenceOfFactionIndex.Contains ( AttachedFaction.FactionIndex ) ) //if we have influence here, don't count it
                            {
                                //ArcenDebugging.ArcenDebugLogSingleLine("Adding planet to clear " + planet.Name + " path B. Owning faction " + planet.GetControllingOrInfluencingFaction().GetDisplayName(), Verbosity.DoNotShow );
                                HomeworldAdjacentPlanetsToClear.Add( planet );
                            }
                        }
                    }

                    if ( warnForAlliedNanocaustWaveSnipes &&
                         !AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "MaraudersKillCommandStations" ) &&
                         planetsConquered > 0 &&
                         AttachedFaction.HasBeenSeenByPlayer &&
                         planetsConquered >= totalAdjacent - 1 )
                    {
                        //So if this is a player allied nanocaust with some AI planets next to its Hive,
                        //the player has found the Nanocaust, and the Nanocaust have conquered almost all its adjacent planets (but left the command stations)
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                            World_AIW2.Instance.QueueLogJournalEntryToSidebar( "FriendlyNanocaust_BeWaryOfWaveSnipes", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    }
                }

                FactionUtilityMethods.Instance.FlushUnitsFromReinforcementPointsOnAllRelevantPlanets( AttachedFaction, Context, 5f );
                if ( this.BaseInfo.IsInFireteamMode )
                {
                    DoFireteamLRP( AttachedFaction, Context, pathingCacheData );
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " DoLongRangePlanning trace ends (fireteam mode)" );
                    if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                    return;
                }
                //This is the non-Tactition LRP code
                //just in case we have some disbanded fire teams
                FireteamUtility.CleanUpDisbandedFireteams( this.BaseInfo.Teams );

                unassignedThreatShipsByPlanet.Clear();
                shipsToStartPatrolling.Clear();

                /* this delegate finds all unassigned ships */
                bool foundHive = false;
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    if ( entity.TypeData.GetHasTag( NanocaustFactionBaseInfo.NANOCAUST_TAG ) )
                    {
                        // if a nanobot center, skip
                        if ( entity.TypeData.GetHasTag( NanocaustFactionBaseInfo.NANOCAUST_HIVE ) || entity.TypeData.GetHasTag( NanocaustFactionBaseInfo.NANOCAUST_HACKED_HIVE ) )
                            foundHive = true;
                        continue;
                    }

                    Planet planet = entity.Planet;
                    {
                        if ( veryVerboseDebug && entity.TypeData != null )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Found ship " + entity.TypeData.InternalName + " " + entity.PrimaryKeyID + " on " + planet.Name, Verbosity.DoNotShow );


                        if ( entity.Planet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Hostile].MobileStrength > 0 )
                            continue;
                        if ( entity.Planet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Hostile].TotalStrength >
                             entity.Planet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Self].TotalStrength / 50 )
                            continue;
                        if ( entity.CalculateNextHopPlanetIndex_Safe() > 0 && entity.CalculateNextHopPlanetIndex_Safe() != entity.GetPlanetIndexSafe() )
                            continue;
                        if ( entity.Orders.GetQueuedOrderCount() == 0 )
                        {
                            //If this entity is idle, it needs to get some orders
                            shipsToStartPatrolling[planet].Add( entity );
                        }

                        unassignedThreatShipsByPlanet[planet].Add( entity );
                        if ( veryVerboseDebug && entity.TypeData != null )
                            ArcenDebugging.ArcenDebugLogSingleLine( " Ship is unassigned", Verbosity.DoNotShow );
                        continue;
                    }
                }
                if ( !foundHive )
                {
                    #region Tracing
                    if ( tracing )
                    {
                        tracingBuffer.Add( "The hive is dead, brute force LRP path\n" );
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " DoLongRangePlanning trace ends" );
                        if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                        if ( tracing )
                        {
                            tracingBuffer.ReturnToPool();
                            tracingBuffer = null;
                        }
                    }
                    #endregion
                    return;
                }
                /* Now that the ships have been processed (part of a fireteam or unassigned) */
                if ( veryVerboseDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Handle ships that should patrol", Verbosity.DoNotShow );
                /* First start ships patrolling if necessary so they aren't just sitting around on a planet.
                   Note that a ship might start patrolling then immediately be ordered to join a threat fleet and stop
                   patrolling, and that's fine */
                bool enablePatrolling = false;
                int patrolPairCount = shipsToStartPatrolling.GetCountOfLists();
                if ( enablePatrolling )
                {
                    foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in shipsToStartPatrolling )
                    {

                        /* Idle ships on a planet should patrol the planet so it looks more dynamic.
                           with a preference for planets controlled by allied factions. They will still sometimes try to attack enemy controlled planets though. */
                        int patrolRadius = pair.Key.GravWellSize.DistanceScale_GravwellRadius / 4;
                        FactionUtilityMethods.Instance.patrolPlanet( this.AttachedFaction, pair.Value, Context, patrolRadius, 10f );
                    }
                }
                if ( veryVerboseDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Iterate over all " + this.BaseInfo.Teams.GetItemCount() + " teams", Verbosity.DoNotShow );
                int secondsOfPlayerProtection = this.BaseInfo.earlyGamePlayerProtection * 60;
                int pairCount = unassignedThreatShipsByPlanet.GetCountOfLists();
                //For any idle ships, migrate to the edge of the empire and then strike
                foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in unassignedThreatShipsByPlanet )
                {
                    PotentialTargets.Clear();
                    AdjacentNonAlliedPlanets.Clear();
                    AdjacentAlliedPlanets.Clear();
                    Planet planet = pair.Key;
                    List<SafeSquadWrapper> list = pair.Value;
                    int strength = 0;
                    for ( int idx = 0; idx < list.Count; idx++ )
                    {
                        strength += list[idx].GetStrengthOfStack();
                    }
                    FInt overkillStrength = FInt.FromParts( 0, 900 );
                    foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                    {
                        if ( neighbor.GetFactionWithSpecialInfluenceHere() == AttachedFaction || neighbor.GetFactionWithSpecialInfluenceHere().GetIsFriendlyTowards( AttachedFaction ) ||
                           neighbor.GetControllingFaction().GetIsFriendlyTowards( AttachedFaction ) )
                        {
                            AdjacentAlliedPlanets.Add( neighbor );
                            continue;
                        }
                        else
                            AdjacentNonAlliedPlanets.Add( neighbor );
                        PlanetFaction pFaction = neighbor.GetPlanetFactionForFaction( AttachedFaction );
                        if ( (int)strength * overkillStrength > pFaction.DataByStance[FactionStance.Hostile].TotalStrength )
                        {
                            if ( neighbor.GetControllingFactionType() == FactionType.Player &&
                                 World_AIW2.Instance.GameSecond < secondsOfPlayerProtection )
                                continue; //the nanocaust isn't allowed to attack a player planet for "a while"
                            PotentialTargets.Add( neighbor );
                        }
                    }

                    Planet attackTarget = null;
                    //first go help adjacent allied planets if they are under attack
                    for ( int j = 0; j < AdjacentAlliedPlanets.Count; j++ )
                    {
                        PlanetFaction pFaction = AdjacentAlliedPlanets[j].GetPlanetFactionForFaction( AttachedFaction );
                        if ( pFaction.DataByStance[FactionStance.Hostile].MobileStrength > FInt.Zero )
                        {
                            attackTarget = AdjacentAlliedPlanets[j];
                        }
                    }
                    if ( attackTarget == null )
                    {
                        for ( int k = 0; k < PotentialTargets.Count; k++ )
                        {
                            if ( attackTarget == null )
                                attackTarget = PotentialTargets[k];
                            else if ( PotentialTargets[k].GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Hostile].TotalStrength < attackTarget.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Hostile].TotalStrength )
                                attackTarget = PotentialTargets[k];
                        }
                    }
                    if ( attackTarget == null && AdjacentNonAlliedPlanets.Count == 0 )
                    {
                        //We have no adjacent enemy planets, so lets split up to find some enemy planets
                        FactionUtilityMethods.Instance.Helper_DivideShipsAmongPlanets( list, planet, AttachedFaction, AdjacentAlliedPlanets, World_AIW2.Instance.CurrentGalaxy, Context, 5f );
                    }
                    //Note that attackTarget can be null here; this is the case where we have adjacent enemy planets but we are too weak to take any of them
                    if ( attackTarget != null )
                    {
                        FactionUtilityMethods.Instance.Helper_RaidSpecificPlanet( list, planet, AttachedFaction, World_AIW2.Instance.CurrentGalaxy, attackTarget, false, Context, pathingCacheData, 5f );
                    }

                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Nanocaust LRP error: " + e, Verbosity.ShowAsError );
            }
            finally
            {
                if ( pathingCacheData != null )
                    pathingCacheData.ReturnToPool();
                #region Tracing
                if ( tracingBuffer == null && tracing)
                    tracing = false; //we already handled the traces being cleaned up earlier in the function
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " DoLongRangePlanning trace ends" );
                if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
            }
        }

        //these variables are used only in the local method below, which is only called from this thread, so they are ok.
        private readonly List<SafeSquadWrapper> UnassignedShips = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "NanocaustFactionDeepInfo-UnassignedShips" );
        private readonly List<Planet> PlanetsToDefend = List<Planet>.Create_WillNeverBeGCed( 90, "NanocaustFactionDeepInfo-PlanetsToDefend" );
        public int numDefensiveFleets_LRP;
        public void DoFireteamLRP( Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Nanocaust );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "NanoD-DoFireteamLRP-trace", 10f ) : null;
            UnassignedShips.Clear();
            TeamsAimedAtPlanet.Clear();
            numDefensiveFleets_LRP = 0;
            int debugCode = 0;
            try
            {
                Galaxy galaxy = World_AIW2.Instance.CurrentGalaxy;
                debugCode = 100;
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
                {
                    team.DeepInfo.Reset();//reset team count information
                    if ( team.DefenseMode )
                        numDefensiveFleets_LRP++;
                }
                debugCode = 200;
                foreach ( GameEntity_Squad entity in faction.Squads() )
                {
                    if ( !entity.TypeData.IsMobile || entity.TypeData.IsDrone )
                        continue;
                    if ( entity.FireteamId < 0 )
                        UnassignedShips.Add( entity );
                    else
                    {
                        Fireteam team = this.BaseInfo.GetFireteamById( entity.FireteamId );
                        if ( team != null )
                            team.DeepInfo.AddUnit( entity );
                        else
                            entity.FireteamId = -1; //something happened to the fireteam, so lets find a new one next LRP stage
                    }
                }
                if ( this.BaseInfo.Hive.Display.GetSquad() == null )
                {
                    if ( tracing )
                        tracingBuffer.Add( "\nThe hive is dead; nanocaust is now broken\n" );
                    return;
                }
                debugCode = 300;
                FInt overkillRequired = FInt.FromParts( 0, 850 );
                PlanetsToDefend.Clear();
                List<SafeSquadWrapper> nanobotCenters = this.BaseInfo.NanobotCenters.GetDisplayList();
                for ( int i = 0; i < nanobotCenters.Count; i++ )
                    PlanetsToDefend.Add( nanobotCenters[i].Planet );
                FireteamUtility.UpdateFireteams( faction, Context, PathCacheData, this.BaseInfo.Teams, TeamsAimedAtPlanet, tracingBuffer, overkillRequired, PlanetsToDefend );
                debugCode = 400;
                FireteamUtility.UpdateRegiments( faction, Context, PathCacheData, this.BaseInfo.Teams, TeamsAimedAtPlanet, tracingBuffer, faction.MinFireteamStrength, false );
                debugCode = 500;
                for ( int i = 0; i < UnassignedShips.Count; i++ )
                    AssignNanocaustUnitToFireteam( faction, UnassignedShips[i].GetSquad(), Context, PathCacheData );
                if ( faction.NumFireteams != BaseInfo.Teams.GetItemCount() )
                    faction.NumFireteams = BaseInfo.Teams.GetItemCount();
            }
            catch ( ArcenPleaseStopThisThreadException )
            {
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "debug code " + debugCode + " during nanocaust fireteam logic. " + e.ToString(), Verbosity.DoNotShow );
            }
            if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }

        }

        //this is okay because it's only used for the nanocaust thread
        //prefer close ones that you can get to safely
        private ArcenLessLinkedList<Fireteam> AvailableFireteams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "NanocaustFactionDeepInfo-AvailableFireteams" );
        private void AssignNanocaustUnitToFireteam( Faction faction, GameEntity_Squad entity, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( entity == null )
                return;

            AvailableFireteams.Clear();
            bool debug = false;
            if ( this.BaseInfo.Teams.GetItemCount() == 0 )
            {
                Fireteam team = Fireteam.CreateNewWithIDFromList( this.BaseInfo.Teams );
                team.StrengthToBringOnline = faction.MinFireteamStrength + Context.RandomToUse.Next( 0, faction.MaxFireteamStrength - faction.MinFireteamStrength );
                team.MyStrengthMultiplierForStrengthCalculation = FInt.FromParts( 1, 00 );
                team.EnemyStrengthMultiplierForStrengthCalculation = FInt.FromParts( 1, 500 );
                team.NoDeathballing = false;
                this.BaseInfo.Teams.AddIfNotAlreadyIn( team );
            }
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( this.BaseInfo.Teams ) )
            {
                if ( team.status == FireteamStatus.Disbanded ||
                        team.status == FireteamStatus.ReadyToAttack ||
                        team.status == FireteamStatus.Attacking )
                    continue; //if a fireteam is ready to fight, don't send more ships to that team
                if ( team.status == FireteamStatus.Staging && //if a fireteam is already "pretty strong", don't give it more
                     team.DeepInfo.TeamStrength > faction.MaxFireteamStrength )
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
                int maxHops = 5;
                if ( dangerOfTeam < 20000 ) //let units wander through pretty dangerous spots (20 strength)
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
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path A", Verbosity.DoNotShow );
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
                Fireteam team = Fireteam.CreateNewWithIDFromList( this.BaseInfo.Teams );
                team.NoDeathballing = false;

                if ( faction.IsVassal && FactionUtilityMethods.Instance.GetActiveVassalMissionCount( faction, VassalMissionType.Combat ) > 0 )
                {
                    //if we have no assigned orders as a faction then we allocate our fireteams as "normal"
                    //If we have any orders then we only allocate as much defense as the player requests
                    if ( FactionUtilityMethods.Instance.GetActiveVassalMissionCount( faction, VassalMissionType.Defense ) > numDefensiveFleets_LRP )
                    {
                        //if we are a vassal and we've requested more defense, do it
                        team.DefenseMode = true;
                    }
                    else
                        team.DefenseMode = false;
                }

                else if ( AdjacentNonAlliedPlanets.Count == 0 && //we've takin our initial set of planets
                     numDefensiveFleets_LRP < BaseInfo.NanobotCenters.Count / 2 ) //half our nanobot centers will have a fireteam in defense
                    team.DefenseMode = true;

                team.MyStrengthMultiplierForStrengthCalculation = FInt.FromParts( 1, 00 );
                team.EnemyStrengthMultiplierForStrengthCalculation = FInt.FromParts( 1, 500 );
                team.StrengthToBringOnline = faction.MinFireteamStrength + Context.RandomToUse.Next( 0, faction.MaxFireteamStrength - faction.MinFireteamStrength );
                int numFireteamsForConcentratingEfforts = 3; //before we're too strong, best to concentrate our forces
                if ( this.BaseInfo.Teams.GetItemCount() < numFireteamsForConcentratingEfforts )
                    team.PercentBestTarget = 100;
                else if ( this.AttachedFaction.SpecialFactionData.FireteamPercentBestTarget > 0 )
                    team.PercentBestTarget = this.AttachedFaction.SpecialFactionData.FireteamPercentBestTarget;
                else
                    team.PercentBestTarget = 40;
                team.PercentDistanceBestTarget = 45; //marauders often get far-flung empires
                team.PreferredMaxDistance = 5;
                team.DeepInfo.AddUnit( entity );
                entity.FireteamId = team.FireTeamID;

                team.DeepInfo.IdentifyCurrentPlanet(); //in case this unit is the first unit
                this.BaseInfo.Teams.AddIfNotAlreadyIn( team );
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path B", Verbosity.DoNotShow );
                return;
            }

            {
                Fireteam team = AvailableFireteams.GetRandom( Context.RandomToUse );
                team.DeepInfo.AddUnit( entity );
                entity.FireteamId = team.FireTeamID;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path C", Verbosity.DoNotShow );
                team.DeepInfo.IdentifyCurrentPlanet(); //just in case this unit is the first unit or something
            }
        }

        public override GameEntity_Squad GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            int currentDanger = -1;
            GameEntity_Squad retreatPoint = null;
            List<SafeSquadWrapper> nanobotCenters = this.BaseInfo.NanobotCenters.GetDisplayList();
            for ( int i = 0; i < nanobotCenters.Count; i++ )
            {
                GameEntity_Squad outpost = nanobotCenters[i].GetSquad();
                if ( outpost == null )
                    continue;
                if ( outpost.Planet == CurrentPlanetForFireteam )
                    continue;
                Int16 hops = 0;
                int danger = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForFireteam, outpost.Planet, true, out hops );
                if ( danger < currentDanger || currentDanger == -1 )
                {
                    retreatPoint = outpost;
                    currentDanger = danger;
                }
            }
            return retreatPoint;
        }

        public override void GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass( bool DefenseMode, Planet CurrentPlanetForFireteam, 
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, List<FireteamTarget> PreferredTargets, List<FireteamTarget> FallbackTargets, object TeamObj )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "NanoD-GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            FInt falloffForDistance = FInt.FromParts( 0, 050 );
            GetPreferredNanocaustTargets( PreferredTargets, AttachedFaction, Context );

            bool debug = false;
            if ( debug && tracing )
            {
                tracingBuffer.Add( "Getting lurk/target Preferred Targets\n" );
                for ( int i = 0; i < PreferredTargets.Count; i++ )
                    tracingBuffer.Add( "\t" ).Add( PreferredTargets[i].GetPlanetName_Safe() ).Add( " difficulty " ).Add( PreferredTargets[i].dangerOfPath ).Add( " \n" );
                tracingBuffer.Add( "Getting lurk/target Fallback Targets\n" );
                for ( int i = 0; i < FallbackTargets.Count; i++ )
                    tracingBuffer.Add( "\t" ).Add( FallbackTargets[i].GetPlanetName_Safe() ).Add( "\n" );
            }
        }
        public void GetPreferredNanocaustTargets( List<FireteamTarget> ListToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            ListToFill.Clear();
            bool playerImmune = false;
            if ( this.BaseInfo.earlyGamePlayerProtection * 60 > World_AIW2.Instance.GameSecond )
                playerImmune = true;
            // if ( HomeworldAdjacentPlanetsToClear.Count > 0 )
            // {
            //     ArcenDebugging.ArcenDebugLogSingleLine("Homeworld adjacent planets:", Verbosity.DoNotShow );
            //     for ( int i = 0; i < HomeworldAdjacentPlanetsToClear.Count; i++ )
            //         ArcenDebugging.ArcenDebugLogSingleLine("\t" + HomeworldAdjacentPlanetsToClear[i].Name , Verbosity.DoNotShow );
            // }
            
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null )
                    continue;
                if ( !otherFaction.GetIsHostileTowards( faction ) )
                    continue;
                if ( otherFaction.Type == FactionType.Player && playerImmune )
                    continue;

                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.GrantsMinorFactionPlanetControl ) )
                {
                     if ( HomeworldAdjacentPlanetsToClear.Count > 0 && !HomeworldAdjacentPlanetsToClear.Contains( entity.Planet ) )
                         continue;
                     //allows the nanocaust to go after hostile minor factions
                     ListToFill.Add( new FireteamTarget( entity ) );
                 }
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.CommandStation ) )
                {
                     if ( HomeworldAdjacentPlanetsToClear.Count > 0 && !HomeworldAdjacentPlanetsToClear.Contains( entity.Planet ) )
                         continue;
                     if ( entity.SecondsSpentAsRemains > 0 )
                         continue;
                     ListToFill.Add( new FireteamTarget( entity.Planet ) );
                 }
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                     if ( HomeworldAdjacentPlanetsToClear.Count > 0 && !HomeworldAdjacentPlanetsToClear.Contains( entity.Planet ) )
                         continue;

                     ListToFill.Add( new FireteamTarget( entity ) );
                 }
            }
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( Fireteam.IsThisAWinningBattle( faction, Context, planet, 2 ) )
                    continue;
                if ( HomeworldAdjacentPlanetsToClear.Count > 0 && !HomeworldAdjacentPlanetsToClear.Contains( planet ) )
                    continue;

                if ( planet.GetControllingFactionType() == FactionType.NaturalObject )
                {
                    Faction influencingFaction = World_AIW2.Instance.GetFactionByIndex( planet.PrimaryInfluencingFaction );
                    if ( influencingFaction == null || !influencingFaction.GetIsFriendlyTowards( faction ) )
                        ListToFill.Add( new FireteamTarget( planet ) );
                }
                if ( planet.GetControllingOrInfluencingFaction() == faction )
                {
                    //are there enemies attacking us?
                    EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( faction );
                    int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                    int myStrength = myFactionData[FactionStance.Self].TotalStrength;
                    if ( hostileStrength > myStrength / 2 )
                        ListToFill.Add( new FireteamTarget( planet ) );
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
            //this logic partially cribbed from IndependentAIFleet.cs::Helper_DoTargetFindingSweep

            if ( TargetPlanet == null )
                throw new Exception( "No target planet set in get lurk planet?!" );
            //int debugCode = 0;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "NanoD-GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            bool preferUnwatchedPlanets = true;
            if ( tracing )
                tracingBuffer.Add( "Getting a lurk planet. Target planet " + TargetPlanet.Name ).Add( ". " ).Add( AttachedFaction.BaseInfo.Allegiance ).Add( "\n" );
            foreach ( Planet.PlanetAtHopDistance _phd in TargetPlanet.PlanetsWithinXHops_NoFilters( -1 ) )
            {
                Planet planet = _phd.Planet;
                Int16 Distance = _phd.Hops;
                if ( planet == TargetPlanet )
                    continue;

                int planetDefensiveStrength = Fireteam.GetPlanetDefensiveStrength( planet, AttachedFaction, true, ref unused,
                                                                          FInt.Zero, FInt.Zero );
                if ( planet.GetControllingFaction().GetIsHostileTowards( AttachedFaction ) && planetDefensiveStrength > TeamStrength / 10 )
                    continue;

                //Don't path through any particularly dangerous planets
                Int16 hops = 0;
                int totalDifficultyOfPathToLurkPlanet = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForTeam, planet, true, out hops );
                if ( totalDifficultyOfPathToLurkPlanet >= TeamStrength * 5 ) //as long as they only outnumber us 5:1, let's go!
                    continue;

                int totalDifficultyOfPathToTarget = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, planet, TargetPlanet, true, out hops );
                if ( preferUnwatchedPlanets && planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    totalDifficultyOfPathToTarget *= 2; //penalized watched planets if desired

                if ( totalDifficultyOfPathToTarget < 0 )
                    totalDifficultyOfPathToTarget = 0; //pathing through allied planets is basically the same

                // if ( tracing )
                //     tracingBuffer.Add("\tConsidering " + planet.Name + " danger of path " + totalDifficultyOfPathToTarget + " distance " + Distance + " intel " + planet.IntelLevel ).Add("\n");

                if ( dangerOfPathFromBestPlanet > totalDifficultyOfPathToTarget ||
                     dangerOfPathFromBestPlanet == -1 )
                {
                    if ( tracing )
                        tracingBuffer.Add( "\t" + planet.Name + " is now the lurk location; path A" ).Add( "\n" );
                    dangerOfPathFromBestPlanet = totalDifficultyOfPathToTarget;
                    distanceFromBestPlanet = Distance;
                    hopsFromBestPlanet = hops;
                    bestPlanet = planet;
                }

                if ( dangerOfPathFromBestPlanet == totalDifficultyOfPathToTarget &&
                     (distanceFromBestPlanet > Distance ||
                      hopsFromBestPlanet > hops) )
                {
                    if ( tracing )
                        tracingBuffer.Add( "\t" + planet.Name + " is now the lurk location; path B" ).Add( "\n" );
                    dangerOfPathFromBestPlanet = totalDifficultyOfPathToTarget;
                    distanceFromBestPlanet = Distance;
                    bestPlanet = planet;
                    hopsFromBestPlanet = hops;
                }
                if ( hopsFromBestPlanet <= 3 && dangerOfPathFromBestPlanet <= TeamStrength / 10 )
                    break; //we found a good lurk within easy striking distance of the planet, so exit now
            }
            return bestPlanet;
        }

        public override void MinorFactionAIPEquivalentIncrease( FInt AIPEquivalent )
        {
            if ( this.BaseInfo == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: could not update Minor Faction AIP without nanocaust mgr being initialized", Verbosity.DoNotShow );
                return;
            }
            this.BaseInfo.NanocaustSpecificAIP += (int)AIPEquivalent;
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Upping nanocaust progress by " + AIPEquivalent + " to " + this.BaseInfo.NanocaustSpecificAIP, Verbosity.DoNotShow );
        }

        public override void DoOnFirstSightingOfFactionByPlayer( bool IsFromBeacon, GameEntity_Squad SquadSeenOrNull, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return;
            if ( !IsFromBeacon )
            {
                PlanetViewChatHandlerBase chatHandlerOrNull = null;
                if ( SquadSeenOrNull != null && SquadSeenOrNull.Planet != null )
                {
                    chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.PlanetToView = SquadSeenOrNull.Planet;
                }

                //play journal entry
                //note must be different entry if beacon
                World_AIW2.Instance.QueueChatMessageOrCommand( "Commander, we've spotted the Nanocaust", ChatType.ShowLocallyOnly, "ArkChiefOfStaff_PlayerGainsNanocaustIntel", chatHandlerOrNull );
            }
        }

        private readonly List<Planet> workingAllowedSpawnPlanets = List<Planet>.Create_WillNeverBeGCed( 30, "NanocaustFactionDeepInfo-workingAllowedSpawnPlanets" );

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            bool localDebug = false;
            if ( Engine_AIW2.Instance.IsTestChamber )
                return;
            /* There are some potential weirdnesses I've seen once the human king is dead,
               so if the game seems to be over then the Nanocaust will stop playing. */
            bool isHumanKingDead = (FactionUtilityMethods.Instance.findHumanKing( false ) == null);
            // if ( isHumanKingDead == null )
            //   {
            //       this.BaseInfo.hasSimStepRun = false;
            //       return;
            //   }            

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Nanocaust );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "NanoD-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f ) : null;
            int debugCode = 0;
            try{
            debugCode = 100;
            string intelligence = AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "Intelligence", true );
            this.BaseInfo.IsInFireteamMode = intelligence != "Brute Force";
            UpdateAllegiance( AttachedFaction );
            if ( tracing )
            {
                ConfigurationForFaction cfg = this.AttachedFaction.Config;
                tracingBuffer.Add( this.TracingName ).Add( " DoPerSecond trace begins for faction " ).Add( AttachedFaction.FactionIndex ).Add( " at " + World_AIW2.Instance.GameSecond ).Add( ". Fireteams: " + this.BaseInfo.IsInFireteamMode + ", " + intelligence + ". My state is " + this.BaseInfo.state + ". My integer invasion time is " + AttachedFaction.InvasionTime +", and my setting invasion time is " + cfg.GetStringValueForCustomFieldOrDefaultValue( "InvasionTime", true ) +"\n" );
            }
            debugCode = 200;
            if ( this.BaseInfo.state == InvasionState.Suppressed )
            {
                debugCode = 300;
                //check if we should still be suppressed; if not then change the state
                bool isSuppressed = false;
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "NanocaustBeacon" ) )
                {
                    isSuppressed = true;
                }
                if ( World_AIW2.Instance.GameSecond < AttachedFaction.InvasionTime )
                    isSuppressed = true;
                if ( isSuppressed )
                {
                    #region Tracing
                    if ( tracing && World_AIW2.Instance.GameSecond % 30 == 0 ) tracingBuffer.Add( "The AI is suppressing the nanocaust\n" );
                    if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                    return;
                }
                debugCode = 400;
                if ( !isSuppressed && !AttachedFaction.HasDoneInvasionStyleAction &&
                     (AttachedFaction.InvasionTime > 0 && AttachedFaction.InvasionTime <= World_AIW2.Instance.GameSecond) )
                {
                    debugCode = 500;
                    //Lets default to just putting the nanocaust hive on a completely random non-player non-ai-king planet
                    //TODO: improve this
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, NanocaustFactionBaseInfo.NANOCAUST_HIVE );

                    Planet spawnPlanet = null;
                    {
                        workingAllowedSpawnPlanets.Clear();
                        int preferredHomeworldDistance = 10;
                        do
                        {
                            debugCode = 600;
                            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                            {
                                debugCode = 700;
                                if ( this.BaseInfo.SeedNearPlayer && planet.GetControllingFactionType() == FactionType.Player )
                                {
                                    workingAllowedSpawnPlanets.Add(planet);
                                    continue;
                                }
                                if ( planet.GetControllingFactionType() == FactionType.Player )
                                    continue;
                                if ( planet.GetFactionWithSpecialInfluenceHere().Type != FactionType.NaturalObject && preferredHomeworldDistance >= 6 ) //don't seed over a minor faction if we are finding good spots
                                {
                                    continue;
                                }
                                if ( planet.IsPlanetToBeDestroyed || planet.HasPlanetBeenDestroyed )
                                    continue;
                                if ( planet.PopulationType == PlanetPopulationType.AIBastionWorld ||
                                        planet.IsZenithArchitraveTerritory )
                                {
                                    continue;
                                }
                                debugCode = 800;
                                if ( planet.OriginalHopsToAIHomeworld >= preferredHomeworldDistance &&
                                        ( planet.OriginalHopsToHumanHomeworld == -1 ||
                                        planet.OriginalHopsToHumanHomeworld >= preferredHomeworldDistance ) )
                                    workingAllowedSpawnPlanets.Add( planet );
                            }

                            preferredHomeworldDistance--;
                            if ( preferredHomeworldDistance == 0 )
                                break;
                        } while ( workingAllowedSpawnPlanets.Count == 0 );
                        debugCode = 900;
                        if ( workingAllowedSpawnPlanets.Count == 0 )
                            throw new Exception("Unable to find a place to spawn the nanocaust");

                        // This is not actually random unless we set the seed ourselves.
                        // Since other processing happening before us tends to set the seed to the same value repeatedly.
                        Context.RandomToUse.ReinitializeWithSeed( World_AIW2.Instance.CurrentGalaxy.RandomSeedBase + AttachedFaction.FactionIndex + World_AIW2.Instance.GameSecond );
                        spawnPlanet = workingAllowedSpawnPlanets[Context.RandomToUse.Next( 0, workingAllowedSpawnPlanets.Count )];
                        
                        // instead of spawning on this planet, create a new planet linked to it
                        if (BaseInfo.SpawnPlanet)
                        {
                            spawnPlanet = CreateSpawnPlanet(Context, spawnPlanet);
                        }
                    }

                    PlanetFaction pFaction = spawnPlanet.GetPlanetFactionForFaction( AttachedFaction );
                    ArcenPoint spawnLocation = spawnPlanet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 200 ), FInt.FromParts( 0, 600 ) );

                    var hive = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                                                pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "Nanocaust-NewHive" );
                    AttachedFaction.HasDoneInvasionStyleAction = true;

                    SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( hive );

                    string planetStr = "";
                    if (BaseInfo.SpawnPlanet || spawnPlanet.GetDoHumansHaveVision())
                    {
                        planetStr = " from " + spawnPlanet.Name;
                    }

                    var str = string.Format("<color=#{0}>{1}</color> are invading{2}!", AttachedFaction.FactionCenterColor.ColorHexBrighter, AttachedFaction.GetDisplayName(), planetStr);
                    World_AIW2.Instance.QueueChatMessageOrCommand( str, ChatType.LogToCentralChat, chatHandlerOrNull );
                }
                debugCode = 1000;
                #region Tracing
                if ( tracing && World_AIW2.Instance.GameSecond % 30 == 0 ) tracingBuffer.Add( "The Nanocaust is beginning its invasion " );
                if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion

                this.BaseInfo.state = InvasionState.FirstStep;

                return;
            }
            debugCode = 1100;
            this.updateMetalBudget( AttachedFaction, Context );
            if ( (World_AIW2.Instance.GameSecond % this.BaseInfo.secondsBetweenSimUpdates == 0) ||
                 World_AIW2.Instance.GameSecond == 1 || !this.BaseInfo.HasPerSecondSimRunSinceGameLoad ) //Run this every secondsBetweenSimUpdates seconds, and the very first second
            {
                //update the various planet State lists
                debugCode = 1200;
                if ( this.BaseInfo.NanobotCenters.Count == 0 || this.BaseInfo.Hive == null )
                {
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( " Nanocaust defeated\n", Verbosity.DoNotShow );
                    return;
                }

                //The Nanocaust gets a bunch of extra power for its initial invasion,
                //since the player may have started this invasion some hours into the game
                if ( this.BaseInfo.state != InvasionState.Normal )
                    this.doInitialInvasionLogic_OnMainSimThreaDOnly( AttachedFaction, Context );

                this.addConstructorsToConqueredPlanets( AttachedFaction, Context );
                debugCode = 1300;
                //adjust nanobot lifespans when appropriate
                int numPlanetsOverMin = Math.Max( this.BaseInfo.NanobotCenters.Count - this.BaseInfo.NanocaustInitialInvasionPlanets, 0 );
                int lifespanFactor = 40; //chosen capriciously
                this.BaseInfo.nanobotLifespan = this.BaseInfo.maxNanobotLifespan - lifespanFactor * numPlanetsOverMin;
                if ( this.BaseInfo.nanobotLifespan < this.BaseInfo.minNanobotLifespan )
                    this.BaseInfo.nanobotLifespan = this.BaseInfo.minNanobotLifespan;
                
                this.spawnEntitiesForSimStep_OnMainSimThreadOnly( AttachedFaction, Context );
                this.checkForHumanVision();
                debugCode = 1400;
                this.updateWaveData( AttachedFaction, Context );
                //TODO: Add check for "if the humans are close then sometimes spawn a bunch of extra units for an anti-human
                //attack wave on higher intensities"
                //TODO: I'd love to also be able to check "are the humans close" and "Have the humans ever destroyed
                //a nanobot center" for some additional anti-human behaviour...
            }
            debugCode = 1500;
            this.RespondToAttack_OnMainSimThreadOnly( AttachedFaction, Context );
            } catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in nanocaust stage 3. debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            #region Tracing
            if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
        }
        private FInt getIntensityMultiplier( Faction faction )
        {
            FInt multiplier = FInt.Zero;
            switch ( this.BaseInfo.Intensity )
            {
                case 1:
                    multiplier = this.BaseInfo.multiplierIntensity1;
                    break;
                case 2:
                    multiplier = this.BaseInfo.multiplierIntensity2;
                    break;
                case 3:
                    multiplier = this.BaseInfo.multiplierIntensity3;
                    break;
                case 4:
                    multiplier = this.BaseInfo.multiplierIntensity4;
                    break;
                case 5:
                    multiplier = this.BaseInfo.multiplierIntensity5;
                    break;
                case 6:
                    multiplier = this.BaseInfo.multiplierIntensity6;
                    break;
                case 7:
                    multiplier = this.BaseInfo.multiplierIntensity7;
                    break;
                case 8:
                    multiplier = this.BaseInfo.multiplierIntensity8;
                    break;
                case 9:
                    multiplier = this.BaseInfo.multiplierIntensity9;
                    break;
                case 10:
                    multiplier = this.BaseInfo.multiplierIntensity10;
                    break;
                default:
                    ArcenDebugging.ArcenDebugLog( "Unexpected nanocaust intensity " + BaseInfo.Intensity + ", so automatically fixed it to intensity 5.", Verbosity.ShowAsError );
                    this.BaseInfo.Intensity = 5;
                    multiplier = this.BaseInfo.multiplierIntensity5;
                    break;
            }
            return multiplier;
        }
        private void updateMetalBudget( Faction faction, ArcenHostOnlySimContext Context )
        {
            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Nanocaust );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "NanoD-updateMetalBudget-trace", 10f ) : null;
            #endregion
            FInt BaseMetalIncome = FInt.Zero;
            FInt MetalIncome = FInt.Zero;
            FInt budgetMultiplier = getIntensityMultiplier( faction );
            foreach ( SafeSquadWrapper wrap in this.BaseInfo.NanobotCenters.GetDisplayList() )
            {
                GameEntity_Squad entity = wrap.GetSquad();
                if ( entity == null || entity.HasBeenRemovedFromSim )
                    continue;
                if ( entity.TypeData.GetHasTag( NanocaustFactionBaseInfo.NANOCAUST_HIVE ) ||
                      entity.TypeData.GetHasTag( NanocaustFactionBaseInfo.NANOCAUST_HACKED_HIVE ) )
                {
                    BaseMetalIncome += this.BaseInfo.MetalIncomePerSecondHive;
                }
                else
                {
                    if ( entity.CurrentMarkLevel == 1 )
                        BaseMetalIncome += this.BaseInfo.MetalIncomePerSecondMark1Center;
                    if ( entity.CurrentMarkLevel == 2 )
                        BaseMetalIncome += this.BaseInfo.MetalIncomePerSecondMark2Center;
                    if ( entity.CurrentMarkLevel == 3 )
                        BaseMetalIncome += this.BaseInfo.MetalIncomePerSecondMark3Center;
                }
            }
            MetalIncome = BaseMetalIncome * budgetMultiplier;
            this.BaseInfo.CurrentMetalStored += MetalIncome;
            FInt MaxMetal = this.BaseInfo.NanobotCenters.Count * this.BaseInfo.MaxMetalStoragePerNanobotCenter;
            if ( this.BaseInfo.CurrentMetalStored > MaxMetal )
                this.BaseInfo.CurrentMetalStored = MaxMetal;
            if ( tracing ) tracingBuffer.Add( " Metal income: " + MetalIncome + " ( " + BaseMetalIncome + " * " + budgetMultiplier + " ) currentMetalStored " + this.BaseInfo.CurrentMetalStored ).Add( "\n" );
            if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }

        }
        private void updateWaveData( Faction faction, ArcenHostOnlySimContext Context )
        {
            // This method actually requests a wave (from a random ai) be sent against it
            // with a budget accumulated in this method.
            // ...There is NO budget or wave to be sent if the nanos are friendly with the AI.

            bool hasHostileAi = false;
            foreach ( Faction f in World_AIW2.Instance.AIFactions )
            {
                if ( f.GetIsHostileTowards( AttachedFaction ) )
                {
                    hasHostileAi = true;
                }
            }

            if (!hasHostileAi)
                return;

            //update the information about the next wave coming toward this faction

            bool debug = false;
            FInt budgetMultiplier = getIntensityMultiplier( faction );
            if ( World_AIW2.Instance.GameSecond % 60 == 0 )
            {
                this.BaseInfo.WaveData.currentWaveBudget += budgetMultiplier * this.BaseInfo.NanocaustSpecificAIP * this.BaseInfo.BaseWaveBudgetPerMinute;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Current wave budget " + this.BaseInfo.WaveData.currentWaveBudget, Verbosity.DoNotShow );
            }
            if ( this.BaseInfo.WaveData.timeForNextWave <= World_AIW2.Instance.GameSecond && this.BaseInfo.WaveData.currentWaveBudget >= this.BaseInfo.MinWaveSize )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "launching wave at " + this.BaseInfo.WaveData.currentWaveBudget, Verbosity.DoNotShow );

                bool allowReconquest = false;
                if ( Context.RandomToUse.Next( 0, 10 ) % 2 == 0 && this.BaseInfo.NanocaustSpecificAIP > 100 )
                    allowReconquest = true;
                int budgetSpent = AntiMinorFactionWaveData.QueueWave( faction, Context, this.BaseInfo.WaveData.currentWaveBudget.GetNearestIntPreferringHigher(), allowReconquest );
                if ( budgetSpent == FInt.Zero )
                {
                    //the wave wasn't actually sent; presumably there are no valid targets
                    //start checking every minute
                    this.BaseInfo.WaveData.timeForNextWave = World_AIW2.Instance.GameSecond + 60;
                }
                else
                    this.BaseInfo.WaveData.timeForNextWave = World_AIW2.Instance.GameSecond + this.BaseInfo.WaveIntervalInMinutes * 60;
                this.BaseInfo.WaveData.currentWaveBudget -= budgetSpent;
            }


        }
        /* This is used to update Faction State */
        public override void DoPerSimStepLogic_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            //if ( this.BaseInfo == null )
            //    return;
        }

        public void doInitialInvasionLogic_OnMainSimThreaDOnly( Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //don't even try this on clients
            if ( !this.BaseInfo.EnableBonusShipsForEarlyInvasion )
                return; //if this has been disabled in XML
            //For the first planet, give us a few extra ships and destroy all the AI Guardians,
            //just so we get off on the right foot
            GameEntityTypeData aberrationData = GameEntityTypeDataTable.Instance.GetRowByName( "Aberration" );
            GameEntityTypeData abominationData = GameEntityTypeDataTable.Instance.GetRowByName( "Abomination" );

            foreach ( SafeSquadWrapper centerWrap in this.BaseInfo.NanobotCenters.GetDisplayList() )
            {
                GameEntity_Squad centerSquad = centerWrap.GetSquad();
                if ( centerSquad == null || centerSquad.HasBeenRemovedFromSim )
                    continue;
                PlanetFaction cFaction = centerSquad.Planet.GetPlanetFactionForFaction( faction );
                if ( this.BaseInfo.state == InvasionState.FirstStep )
                {
                    Faction localPlayer = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                    if ( faction.GetIsHostileTowards( localPlayer ) )
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Nanocaust_Unfriendly_InvasionStarts", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    else
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Nanocaust_Friendly_InvasionStarts", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    //Kill a lot of the defenses on this planet to make sure the Nanocaust doesn't get stuck early
                    
                    foreach ( PlanetFaction otherFaction in cFaction.RelatedFactions( FactionRelationship.FactionsThatAreHostileTowardsMe ) )
                    {
                        if ( this.BaseInfo.SeedNearPlayer && this.BaseInfo.humanAllied ) //don't bother killing things on the player's planet
                            break;
                        foreach ( GameEntity_Squad entity in otherFaction.Entities.Squads( EntityRollupType.BlocksEnemyClaimFlows ) )
                        {
                            //should prevent AIP from being incurred, or other on-death effects.  Just get rid of me!
                            if ( entity.GetFactionTypeSafe() != FactionType.Player )
                                entity.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.ClearingOtherNPCUnits );
                        }
                        foreach ( GameEntity_Squad entity in otherFaction.Entities.Squads( EntityRollupType.NormalPlanetNastyPick ) )
                        {
                            //should prevent AIP from being incurred, or other on-death effects.  Just get rid of me!
                            if ( entity.GetFactionTypeSafe() != FactionType.Player )
                                entity.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.ClearingOtherNPCUnits );
                        }
                        foreach ( GameEntity_Squad entity in otherFaction.Entities.Squads( EntityRollupType.TractorSource ) )
                        {
                            //should prevent AIP from being incurred, or other on-death effects.  Just get rid of me!
                            if ( entity.GetFactionTypeSafe() != FactionType.Player )
                                entity.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.ClearingOtherNPCUnits );
                        }
                        foreach ( GameEntity_Squad entity in otherFaction.Entities.Squads( EntityRollupType.ProjectsForcefield ) )
                        {
                            //should prevent AIP from being incurred, or other on-death effects.  Just get rid of me!
                            if ( entity.GetFactionTypeSafe() != FactionType.Player )
                                entity.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.ClearingOtherNPCUnits );
                        }
                        foreach ( GameEntity_Squad entity in otherFaction.Entities.Squads( EntityRollupType.GravitySource ) )
                        {
                            //should prevent AIP from being incurred, or other on-death effects.  Just get rid of me!
                            if ( entity.GetFactionTypeSafe() != FactionType.Player )
                                entity.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.ClearingOtherNPCUnits );
                        }
                        foreach ( GameEntity_Squad entity in otherFaction.Entities.Squads( EntityRollupType.MobileCombatants ) )
                        {
                            if ( Context.RandomToUse.NextBool() == true )
                            {
                                //should prevent AIP from being incurred, or other on-death effects.  Just get rid of me!
                                if ( entity.GetFactionTypeSafe() != FactionType.Player )
                                    entity.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.ClearingOtherNPCUnits );
                            }
                        }
                    }
                    this.BaseInfo.state = InvasionState.InProgress;
                    this.BaseInfo.NanocaustInitialInvasionPlanets = this.GetNumPlanetsForInvasion( faction );
                    //                    ArcenDebugging.ArcenDebugLogSingleLine("Initially invading " + this.BaseInfo.NanocaustInitialInvasionPlanets + " planets", Verbosity.DoNotShow );
                }
                if ( this.BaseInfo.state == InvasionState.InProgress )
                {
                    if ( this.BaseInfo.NanobotCenters.Count < this.BaseInfo.NanocaustInitialInvasionPlanets )
                    {
                        //Spawn some strength for the initial attack
                        FInt strengthSpawned = FInt.Zero;
                        ArcenPoint center = Engine_AIW2.Instance.CombatCenter;
                        FInt strengthToSpawn = FInt.Zero;
                        int interval = BaseInfo.secondsBetweenInitialInvasionSpawns_Otherwise;
                        if ( this.BaseInfo.NanobotCenters.Count == 1 )
                            interval = BaseInfo.secondsBetweenInitialInvasionSpawns_IfOnlyOnePlanet;
                        if ( interval > 0 && World_AIW2.Instance.GameSecond % interval == 0 )
                            strengthToSpawn = this.BaseInfo.initialAttackStrength;
                        while ( strengthSpawned < strengthToSpawn )
                        {
                            GameEntity_Squad ent1 = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( cFaction, aberrationData, aberrationData.MarkFor( cFaction ),
                                                                                cFaction.FleetUsedAtPlanet, 0, center, Context, "Nanocaust-Aberration" );
                            ent1.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //fine; from main thread
                            ent1.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                            if ( this.BaseInfo.Hive != null )
                                ent1.MinorFactionStackingID = this.BaseInfo.Hive.Display.PrimaryKeyID;
                            ArcenPoint spawnLocation = centerSquad.Planet.GetSafePlacementPoint_AroundEntity( Context, abominationData, centerSquad, FInt.FromParts( 0, 020 ), FInt.FromParts( 0, 040 ) );

                            GameEntity_Squad ent2 = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( cFaction, abominationData, abominationData.MarkFor( cFaction ),
                                                                                cFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "Nanocaust-Abomination" );
                            ent2.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //fine; from main thread
                            ent2.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                            if ( this.BaseInfo.Hive != null )
                                ent2.MinorFactionStackingID = this.BaseInfo.Hive.Display.PrimaryKeyID;

                            strengthSpawned += aberrationData.MarkStatsFor( cFaction.Faction.CurrentGeneralMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership;
                            strengthSpawned += abominationData.MarkStatsFor( cFaction.Faction.CurrentGeneralMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership;
                        }
                    }
                    else
                        this.BaseInfo.state = InvasionState.Normal;
                }
            }
        }

        public void RespondToAttack_OnMainSimThreadOnly( Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //don't even try this on clients

            //bool debug = false;
            foreach ( SafeSquadWrapper centerWrap in this.BaseInfo.NanobotCenters.GetDisplayList() )
            {
                GameEntity_Squad center = centerWrap.GetSquad();
                if ( center == null || center.HasBeenRemovedFromSim )
                    continue;
                Planet planet = center.Planet;
                //bool isValidTarget = false;
                //if we have a decent
                if ( planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Hostile].MobileStrength >
                   planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Self].MobileStrength / 2 )
                {
                    GameEntityTypeData entityData = null;
                    if ( center.TypeData.GetHasTag( NanocaustFactionBaseInfo.NANOCAUST_HIVE ) ||
                       center.TypeData.GetHasTag( NanocaustFactionBaseInfo.NANOCAUST_HACKED_HIVE ) )
                    {
                        if ( faction.HasObtainedSpireDebris )
                            entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ScaryNanobot" );
                        else
                            entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ScaryNanobotBasic" );
                    }
                    else
                        entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MarkBasedNanocaustShip" );
                    for ( int j = 0; j < 2; j++ )
                    {
                        PlanetFaction pFaction = center.Planet.GetPlanetFactionForFaction( faction );
                        ArcenPoint spawnLocation = center.Planet.GetSafePlacementPoint_AroundEntity( Context, entityData, center, FInt.FromParts( 0, 020 ), FInt.FromParts( 0, 040 ) );

                        GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData,
                                                                         //the ScaryNanobot ones are markless and will automatically have their stats the way they need to.
                                                                         //the other cases all set the mark level of the nanocaust ship directly when spawned
                                                                         center.CurrentMarkLevel,
                                                                         pFaction.FleetUsedAtPlanet, 0,
                                                                         spawnLocation, Context, "Nanocaust-RespondToAttack" );
                        entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, main thread
                        entity.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                    }
                }
            }
        }

        private readonly List<Planet> workingPlanetsToCapture = List<Planet>.Create_WillNeverBeGCed( 8, "NanocaustFactionDeepInfo-workingPlanetsToCapture" );
        public void addConstructorsToConqueredPlanets( Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //don't even try this on clients

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Nanocaust );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "NanoD-addConstructorsToConqueredPlanets-trace", 10f ) : null;

            workingPlanetsToCapture.Clear();
            //List<int> fleetIndex = null; //really, this should be some sort of tuple
            //process the fleets to see if we are allowed to capture anything
            if ( tracing )
                tracingBuffer.Add( "checking if we are allowed to capture planets\n" );
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( this.BaseInfo.nanocaustInfectedPlanet( planet ) )
                    continue;
                if ( this.allowedToCapture( faction, planet ) )
                {
                    if ( !FactionUtilityMethods.Instance.isPlanetOnList( workingPlanetsToCapture, planet ) )
                    {
                        //two fleets could be attacking the same target
                        workingPlanetsToCapture.Add( planet );
                    }
                }
            }
            if ( workingPlanetsToCapture.Count > 0 )
            {
                for ( int i = 0; i < workingPlanetsToCapture.Count; i++ )
                {
                    if ( tracing ) tracingBuffer.Add( "Adding constructor to  " + workingPlanetsToCapture[i].Name + "\n" );
                    //add a nanobot constructor
                    PlanetFaction cFaction = workingPlanetsToCapture[i].GetPlanetFactionForFaction( faction );
                    GameEntityTypeData nanobotData = GameEntityTypeDataTable.Instance.GetRowByName( "WarpingInNanobotCenter" );
                    ArcenPoint spawnLocation = cFaction.Planet.GetSafePlacementPointAroundPlanetCenter( Context, nanobotData, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 450 ) );
                    GameEntity_Squad newConstructor = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( cFaction, nanobotData, nanobotData.MarkFor( cFaction ),
                                                                                  cFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "Nanocaust-ConstOnConquered" );
                    newConstructor.TransformsIntoAfterTime = "NanobotCenter";
                    newConstructor.SecondsTillTransformation = (Int16)Context.RandomToUse.Next( 30, 120 );
                    //                  newConstructor.SelfBuildingMetalRemaining = (FInt)nanobotData.MetalCost; //self building doesn't seem to work right
                }
            }
            if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }

        }


        //checks whether the humans have seen the Nanocaust
        //I hope this will eventually give a nice gameplay popup saying
        //"Uh Commander, we've spotted something scary....."
        //also this will allow the Nanocaust to change it's behavour to make it more human focused
        public void checkForHumanVision()
        {
            if ( this.BaseInfo.humanVision == false )
            {
                foreach ( SafeSquadWrapper wrap in this.BaseInfo.NanobotCenters.GetDisplayList() )
                {
                    GameEntity_Squad squad = wrap.GetSquad();
                    if ( squad == null || squad.HasBeenRemovedFromSim )
                        continue;
                    Planet planet = squad.Planet;
                    if ( planet.IntelLevel > PlanetIntelLevel.Unexplored )
                    {
                        this.BaseInfo.humanVision = true;
                    }
                }
            }
        }

        public bool allowedToCapture( Faction faction, Planet planet )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Nanocaust );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "NanoD-allowedToCapture-trace", 10f ) : null;
            //helper function for figuring out if we can capture a planet
            //currently it is intended to say "Have all the defenses been destroyed"
            //unsure if it works properly for human planets, but I haven't been able to get a game
            //far enough in to test it....
            if ( planet == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: allowedToCapture null planet", Verbosity.DoNotShow );
                if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }

                return false;
            }

            if ( this.BaseInfo.LastTimePlanetHadCenter[planet] != 0 && //has definitely been captured before
                 (this.BaseInfo.LastTimePlanetHadCenter[planet] > World_AIW2.Instance.GameSecond - 600) ) //hasn't been owned by us very recently
            {
                if ( tracing ) tracingBuffer.Add( "allowedToCapture: can't capture " + planet.Name + " since too recently owned (" + this.BaseInfo.LastTimePlanetHadCenter[planet] + ")\n" );
                if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }

                return false; //if we have owned this planet recently
            }
            if ( planet.GetControllingFaction().SpecialFactionData.InternalName == "ZenithArchitrave" &&
                 planet.GetControllingFaction().GetIsFriendlyTowards( faction ) )
            {
                //During Civil Wars (or player truces for player allied nanocaust), ZAs will potentially be allied to the nanocaust temporarily. Don't let the scourge
                //The Nanocaust isn't allowed to build on ZA planets while allied to the ZA unless they are explicitly friendly
                if ( faction.BaseInfo.Allegiance != planet.GetControllingFaction().BaseInfo.Allegiance )
                {
                    if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    return false;
                }
            }
            int hostileStrength = planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Hostile].TotalStrength;
            //FInt friendlyTotalStrength = planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Friendly].TotalStrength;

            PlanetFaction myPlanetFaction = planet.GetPlanetFactionForFaction( faction );
            int myStrength = myPlanetFaction.DataByStance[FactionStance.Self].TotalStrength;

            if ( myStrength == 0 )
            {
                if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }

                return false;
            }

            if ( hostileStrength < myStrength / 5 )
            {
                if ( tracing ) tracingBuffer.Add( "allowedToCapture: currently allowed to capture " + planet.Name + " Nanocaust Strength " + myStrength + " enemy strength " + hostileStrength ).Add( "\n" );
                if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }

                return true;
            }
            else
            {
                if ( tracing ) tracingBuffer.Add( "allowedToCapture: currently can't capture " + planet.Name + " Nanocaust Strength " + myStrength + " hostileStrength " + hostileStrength + "\n" );
                if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }

                return false;
            }
        }

        public override void UpdatePlanetInfluence_HostOnly( ArcenHostOnlySimContext Context )
        {
            if ( this.BaseInfo == null )
                return;

            List<Planet> planetsInfluenced = Planet.GetTemporaryPlanetList( "Nanocaust-UpdatePlanetInfluence_HostOnly-planetsInfluenced", 10f );
            if ( planetsInfluenced == null ) //blocked for teardown/shutdown; bail
                return;

            foreach ( SafeSquadWrapper wrap in this.BaseInfo.NanobotCenters.GetDisplayList() )
            {
                GameEntity_Squad squad = wrap.GetSquad();
                if ( squad == null || squad.HasBeenRemovedFromSim )
                    continue;
                this.BaseInfo.LastTimePlanetHadCenter[squad.Planet] = World_AIW2.Instance.GameSecond;
                PlanetFaction pFaction = squad.Planet.GetPlanetFactionForFaction( AttachedFaction );
                if ( pFaction.AIPLeftFromCommandStation != 0 )
                {
                    MinorFactionAIPEquivalentIncrease( (FInt)pFaction.AIPLeftFromCommandStation + pFaction.AIPLeftFromWarpGate );
                    pFaction.AIPLeftFromCommandStation = 0;
                    pFaction.AIPLeftFromWarpGate = 0;
                }
                planetsInfluenced.AddIfNotAlreadyIn( squad.Planet );
            }

            AttachedFaction.SetInfluenceForPlanetsToList( planetsInfluenced );
            Planet.ReleaseTemporaryPlanetList( planetsInfluenced );
        }

        //returns a dictionary that maps from Planet to factionStrength on that planet
        public void factionStrengthPerPlanet( Dictionary<Planet, int> DictToFill, Faction faction )
        {
            foreach ( GameEntity_Squad entity in faction.Squads() )
            {
                Planet planet = entity.Planet;
                int entityStrength = (int)FactionUtilityMethods.Instance.strengthOfEntity( entity );
                DictToFill[planet] += entityStrength;
            }
        }

        private FInt getAllowedStrengthForCenter( GameEntity_Squad entity, Faction faction )
        {
            FInt multiplier = getIntensityMultiplier( faction );
            if ( entity.TypeData.GetHasTag( NanocaustFactionBaseInfo.NANOCAUST_HACKED_HIVE ) ||
               entity.TypeData.GetHasTag( NanocaustFactionBaseInfo.NANOCAUST_HIVE ) )
            {
                return this.BaseInfo.MaxStrengthHive * multiplier;
            }
            else
            {
                if ( entity.CurrentMarkLevel == 1 )
                    return this.BaseInfo.MaxStrengthMark1Center * multiplier;
                if ( entity.CurrentMarkLevel == 2 )
                    return this.BaseInfo.MaxStrengthMark2Center * multiplier;
                if ( entity.CurrentMarkLevel == 3 )
                    return this.BaseInfo.MaxStrengthMark3Center * multiplier;
            }
            return FInt.Zero;
        }
        private void spawnEntitiesForSimStep_OnMainSimThreadOnly( Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //don't even try this on clients

            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Nanocaust );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "NanoD-spawnEntitiesForSimStep_OnMainSimThreadOnly-trace", 10f ) : null;
            #endregion

            if ( World_AIW2.Instance.GameSecond >= this.BaseInfo.NextTimeToSpend )
            {
                //foreach nanobot constructor, see if it has used all of its allowed strength
                //if not, take some metal from the budget to spend on it
                if ( tracing ) tracingBuffer.Add( "Spending up to " + this.BaseInfo.CurrentMetalStored + " metal\n" );
                foreach ( SafeSquadWrapper centerWrap in this.BaseInfo.NanobotCenters.GetDisplayList() )
                {
                    GameEntity_Squad center = centerWrap.GetSquad();
                    if ( center == null || center.HasBeenRemovedFromSim )
                        continue;
                    if ( this.BaseInfo.CurrentMetalStored <= FInt.Zero )
                        break;
                    FInt currentStrengthForCenter = this.BaseInfo.StrengthPerNanobotCenter.Display[center.PrimaryKeyID];
                    FInt allowedStrength = getAllowedStrengthForCenter( center, faction );
                    if ( tracing ) tracingBuffer.Add( "Nanobot center on " + center.GetPlanetName_Safe() + " current strength " + currentStrengthForCenter + " allowed strength " + allowedStrength + " remaining metal " + this.BaseInfo.CurrentMetalStored + "\n" );

                    if ( FactionUtilityMethods.Instance.ShouldFactionsThrottle() )
                        allowedStrength /= 2; //for performance reasons, spawn fewer ships when the sim is slow

                    if ( currentStrengthForCenter < allowedStrength )
                    {
                        GameEntityTypeData entityData = null;
                        if ( center.TypeData.GetHasTag( NanocaustFactionBaseInfo.NANOCAUST_HIVE ) ||
                           center.TypeData.GetHasTag( NanocaustFactionBaseInfo.NANOCAUST_HACKED_HIVE ) )
                        {
                            if ( faction.HasObtainedSpireDebris )
                                entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ScaryNanobot" );
                            else
                                entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ScaryNanobotBasic" );
                        }
                        else
                            entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MarkBasedNanocaustShip" );
                        if ( entityData == null )
                            throw new Exception( "Could not find Nanocaust ships defined in XML" );

                        PlanetFaction pFaction = center.Planet.GetPlanetFactionForFaction( faction );
                        ArcenPoint spawnLocation = center.Planet.GetSafePlacementPoint_AroundEntity( Context, entityData, center, FInt.FromParts( 0, 020 ), FInt.FromParts( 0, 040 ) );

                        GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData,
                                                            //the ScaryNanobot ones are markless and will automatically have their stats the way they need to.
                                                            //the other cases all set the mark level of the nanocaust ship directly when spawned
                                                            center.CurrentMarkLevel,
                                                            pFaction.FleetUsedAtPlanet, 0,
                                                            center.WorldLocation, Context, "Nanocaust-GeneralSpawns" );
                        entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is okay, on main thread
                        entity.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                        entity.MinorFactionStackingID = center.PrimaryKeyID; //spawningNanobotCenter
                        this.BaseInfo.CurrentMetalStored -= entityData.MarkStatsFor( pFaction.Faction.CurrentGeneralMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership;
                    }
                }
                if ( this.BaseInfo.CurrentMetalStored < 0 )
                    this.BaseInfo.NextTimeToSpend = World_AIW2.Instance.GameSecond + Context.RandomToUse.Next( 5, 15 );
                else
                    this.BaseInfo.NextTimeToSpend = World_AIW2.Instance.GameSecond + 1;
            }
            if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }

        }
        private void UpdateAllegiance( Faction faction )
        {
            bool localDebug = false;
            if ( this.BaseInfo.hasBeenHacked || ArcenStrings.Equals( this.BaseInfo.Allegiance, "HostileToAI" ) ||
                 ArcenStrings.Equals( this.BaseInfo.Allegiance, "Friendly To Players" ) )
            {
                this.BaseInfo.humanAllied = true;
                AllegianceHelper.AllyThisFactionToHumans( faction );
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "This Nanocaust faction should be hostile to the AI and friendly to players", Verbosity.DoNotShow );
            }
            else if ( ArcenStrings.Equals( this.BaseInfo.Allegiance, "Hostile To All" ) ||
               ArcenStrings.Equals( this.BaseInfo.Allegiance, "HostileToAll" ) || //this line is for compatibility with older (pre-lobby rework) save games.
               string.IsNullOrEmpty( this.BaseInfo.Allegiance ) )
            {
                this.BaseInfo.humanAllied = false;
                this.BaseInfo.aiAllied = false;
                if ( string.IsNullOrEmpty( this.BaseInfo.Allegiance ) )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "empty Nanocaust allegiance, so fixing it to hate everyone", Verbosity.ShowAsError );
                    this.BaseInfo.SetNewAllegianceIntoCoreSettings( "Hostile To All" );
                }
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "This Nanocaust faction should be hostile to all (default)", Verbosity.DoNotShow );
                //make sure this isn't set wrong somehow
                AllegianceHelper.EnemyThisFactionToAll( faction );
            }
            else if ( ArcenStrings.Equals( this.BaseInfo.Allegiance, "Hostile To Players Only" ) ||
                    ArcenStrings.Equals( this.BaseInfo.Allegiance, "HostileToPlayers" ) )
            {
                this.BaseInfo.aiAllied = true;
                AllegianceHelper.AllyThisFactionToAI( faction );
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "This Nanocaust faction should be friendly to the AI and hostile to players", Verbosity.DoNotShow );
            }
            else if ( ArcenStrings.Equals( this.BaseInfo.Allegiance, "Minor Faction Team Red" ) )
            {
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "This Nanocaust faction is on team red", Verbosity.DoNotShow );
                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Red" );
            }
            else if ( ArcenStrings.Equals( this.BaseInfo.Allegiance, "Minor Faction Team Blue" ) )
            {
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "This Nanocaust faction is on team blue", Verbosity.DoNotShow );

                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Blue" );
            }
            else if ( ArcenStrings.Equals( this.BaseInfo.Allegiance, "Minor Faction Team Green" ) )
            {
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "This Nanocaust faction is on team green", Verbosity.DoNotShow );

                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Green" );
            }

            else
            {
                throw new Exception( "unknown Nanocaust allegiance '" + this.BaseInfo.Allegiance + "'" );
            }
        }
        private int GetNumPlanetsForInvasion( Faction faction )
        {
            FInt AIP = FactionUtilityMethods.Instance.GetCurrentAIP();
            int numPlanets = 2;
            if ( AIP.IntValue <= 50 )
                numPlanets = 2;
            else if ( AIP.IntValue <= 200 )
                numPlanets = 3;
            else
                numPlanets = 4;
            if ( this.BaseInfo.Intensity <= 5 )
                numPlanets += 0;
            else if ( this.BaseInfo.Intensity < 7 )
                numPlanets += 1;
            else
                numPlanets += 2;
            return numPlanets;
        }

        private Planet CreateSpawnPlanet(ArcenHostOnlySimContext hostCtx, Planet nearbyPlanet)
        {
            //ArcenDebugging.ArcenDebugLogSingleLine( string.Format("[nano] CreateSpawnPlanet"), Verbosity.DoNotShow );

            var galaxy = World_AIW2.Instance.CurrentGalaxy;
            var rand = hostCtx.RandomToUse;

            int galaxy_radius;
            ArcenPoint galaxy_centroid;
            GetGalaxyRadius(out galaxy_radius, out galaxy_centroid);

            ArcenPoint planet_pnt = ArcenPoint.ZeroZeroPoint;
            int cur_radius = galaxy_radius / 4;
            int radius_step = galaxy_radius / 4;
            while (true)
            {
                // try 10 times at this radius
                // then increase the radius
                bool success = false;
                for (int i = 0; i < 10; i++)
                {
                    var pnt = RandomPointInRadius(rand, nearbyPlanet.GalaxyLocation, cur_radius);
                    if (!galaxy.CheckForTooCloseToExistingNodes(pnt, PlanetType.Normal, true))
                    {
                        planet_pnt = pnt;
                        success = true;
                        break;
                    }
                }

                if (success)
                    break;

                cur_radius += radius_step;
            }

            var planet = galaxy.AddPlanet(PlanetType.Normal, planet_pnt, World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( rand, PlanetPopulationType.None ) );
            var playerLocalFaction = planet.GetFirstFactionOfType(FactionType.Player);
            playerLocalFaction.AIPLeftFromCommandStation = 0;
            playerLocalFaction.AIPLeftFromWarpGate = 0;

            var cmd = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.LinkPlanets], GameCommandSource.AnythingElse );
            cmd.RelatedIntegers.Add(planet.Index);
            cmd.RelatedIntegers.Add(nearbyPlanet.Index);
            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, cmd, false );
            
            return planet;
        }

        ArcenPoint RandomPointInRadius(RandomGenerator rand, ArcenPoint pos, int radius)
        {
            var vec = Vector2.zero;

            for (int i = 0; i < 100; i++)
            {
                vec = new Vector2()
                {
                    x = rand.NextFloat(radius*2) - radius,
                    y = rand.NextFloat(radius*2) - radius,
                };

                var len = vec.magnitude;
                if (len > radius)
                    continue;

                vec.x += pos.X;
                vec.y += pos.Y;

                break;
            }

            return vec.ToArcenPoint();
        }

        private void GetGalaxyRadius( out int radius, out ArcenPoint centroid )
        {
            var galaxy = World_AIW2.Instance.CurrentGalaxy;

            int totalX = 0;
            int totalY = 0;
            int numPlanets = 0;
            foreach ( Planet p in galaxy.Planets( false ) )
            {
                    totalX += p.GalaxyLocation.X;
                    totalY += p.GalaxyLocation.Y;
                    numPlanets++;
            }

            var center = ArcenPoint.Create(totalX/numPlanets, totalY/numPlanets);

            long maxLenSqr = 0;
            foreach ( Planet p in galaxy.Planets( false ) )
            {
                    var d = p.GalaxyLocation.GetSquareDistanceTo(center);
                    if (d > maxLenSqr)
                        maxLenSqr = d;
            }

            radius = (int)Math.Sqrt(maxLenSqr);
            centroid = center;
        }

        public override void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        {
            if (BaseInfo.ReinvadeFrequency == ReinvadeFrequencySetting.Never)
                return;

            if (entity.TypeData.InternalName == "NanobotCenter_Hive")
            {
                BaseInfo.ResetForNextInvasion();

                int time = 0;
                if (BaseInfo.ReinvadeFrequency == ReinvadeFrequencySetting.ApproxOneHour)
                    time = 3600 + Context.RandomToUse.Next(-1800, 1801);
                else
                    time = 1800 + Context.RandomToUse.Next(-900, 900);

                //ArcenDebugging.ArcenDebugLogSingleLine(string.Format("[nano] will reinvade in {0} seconds; defeated={1}", time, AttachedFaction.FactionIsDefeated), Verbosity.DoNotShow);

                AttachedFaction.InvasionTime = World_AIW2.Instance.GameSecond + time;
                AttachedFaction.HasDoneInvasionStyleAction = false;
                AttachedFaction.FactionIsDefeated = false;
            }
        }
    }
}
