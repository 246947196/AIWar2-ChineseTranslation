using System;
using Arcen.AIW2.Core;
using Arcen.Universal;



namespace Arcen.AIW2.External
{

    public sealed class SpireInfusedHumanEmpireFactionDeepInfo : ExternalFactionDeepInfoRoot, IExternalDeepInfo_Singleton
    {
        public FallenSpireFactionBaseInfo BaseInfo;

        public readonly List<Faction> AIFactionsForDebris = List<Faction>.Create_WillNeverBeGCed( 30, "FallenSpireFactionDeepInfo-AIFactionsForDebris" );
        public readonly List<Faction> OtherFactionsForDebris = List<Faction>.Create_WillNeverBeGCed( 30, "FallenSpireFactionDeepInfo-OtherFactionsForDebris" );
        private static readonly List<int> chokeStrengths = List<int>.Create_WillNeverBeGCed( 300, "FallenSpireFactionDeepInfo-chokeStrengths" );
        //fireteam stuff, for the imperial spire
        public readonly List<SafeSquadWrapper> UnassignedShips = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "FallenSpireFactionDeepInfo-UnassignedShips" );
        public static readonly DictionaryOfLists<Planet, SafeSquadWrapper> KingKillersByPlanet = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 10, "FallenSpireFactionDeepInfo-KingKillersByPlanet" );
        public readonly ProtectedValDictionary<Planet, FireteamRegiment> TeamsAimedAtPlanet = ProtectedValDictionary<Planet, FireteamRegiment>.Create_WillNeverBeGCed( 100, "FallenSpireFactionDeepInfo-TeamsAimedAtPlanet" );
        public static readonly List<SafeSquadWrapper> AlliedCommandStations = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 90, "FallenSpireFactionDeepInfo-AlliedCommandStations" );
        private static readonly ArcenLessLinkedList<Fireteam> AvailableFireteams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "FallenSpireFactionDeepInfo-AvailableFireteams" );
        private static readonly Dictionary<Planet,bool> PlanetsWithCities = Dictionary<Planet,bool>.Create_WillNeverBeGCed( 10, "FallenSpireFactionDeepInfo-PlanetsWithCities" );

        public readonly int MinFireteamStrength = 5000;
        public readonly int MaxFireteamStrength = 20000;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<FallenSpireFactionBaseInfo>();
        }

        protected override void Cleanup()
        {
            BaseInfo = null;

            //may or may not matter
            AIFactionsForDebris.Clear();
            OtherFactionsForDebris.Clear();
            chokeStrengths.Clear();
            AlliedCommandStations.Clear();
            AvailableFireteams.Clear();
            PlanetsWithCities.Clear();

            //matters some, probably
            UnassignedShips.Clear();
            KingKillersByPlanet.Clear();
            TeamsAimedAtPlanet.Clear();

        }
        public override void DoOnSelfBuildingCompleteLogic_HostOnly( GameEntity_Squad entity, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return;
            if ( !entity.TypeData.GetHasTag( "SpireCity" ) )
                return;
            ArcenDebugging.ArcenDebugLogSingleLine("built a city", Verbosity.DoNotShow );
            FallenSpireSharedDeepInfo.Instance.HandleSelfBuildingCompleteLogic( entity, this.AttachedFaction, Context );

            Fleet cityFleet = entity.FleetMembership.Fleet;

            cityFleet.NameRaw = "Spire City '" + FallenSpireSharedDeepInfo.Instance.RandomSpireCityName(Context, entity ) + "'";
            cityFleet.CreateExternalBaseInfo<FallenSpireCityFleetBaseInfo>("FallenSpireCityFleetBaseInfo");

            FallenSpireSharedDeepInfo.Instance.SpawnDragons( Context );
            for ( int k = 0; k < BaseInfo.Difficulty.DebrisToSpawnPerRelic; k++ )
                BaseInfo.TimesForNextSpireDebris.Add( BaseInfo.DebrisSpawnDelay + Context.RandomToUse.Next( BaseInfo.DebrisSpawnDelayRandomness / 10, BaseInfo.DebrisSpawnDelayRandomness ) );

            //Since we don't have relic chases anymore, we send a new Exo when the player builds a Spire City
            int strengthForPerCityExo = (2 * (this.BaseInfo.exoData.StrengthRequiredForNextExo)).IntValue;
            if ( this.AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) ) //debug mode is easy so you can play through it quickly for testing
                strengthForPerCityExo /= 15;
            ArcenDebugging.LogSingleLine("exo strength: " + strengthForPerCityExo + " (would have been " + (this.BaseInfo.exoData.StrengthRequiredForNextExo / 2) +")", Verbosity.DoNotShow );
            List<SafeSquadWrapper> workingTargets = GameEntity_Squad.GetTemporarySquadList( "FallenSpire-UpdateExoData-workingTargets", 10f );
            if ( workingTargets == null ) //blocked for teardown/shutdown; bail
                return;
            FactionUtilityMethods.Instance.findAllHumanKings( workingTargets );
            BaseInfo.exoData.ResetSync();

            //This exo goes against the player homeworld, the new city and maybe other stuff
            foreach ( GameEntity_Squad city in BaseInfo.SpireCities.DisplaySquads() )
            {
                //For each city, there is a chance of targeting it
                int percentAdditionalTarget = 20;
                if ( Context.RandomToUse.Next( 0, 100 ) < percentAdditionalTarget )
                    workingTargets.Add( city );
            }
            if ( !workingTargets.Contains( entity ) )
                workingTargets.Add( entity );
            
            ExoOptions options = ExoOptions.CreateWithDefaults( workingTargets, strengthForPerCityExo, World_AIW2.Instance.GetFactionByIndex( BaseInfo.exoData.FactionIndexOfExoSpawnFaction ), this.AttachedFaction );
            options.UnitBlocksToUse.Clear();
            options.UnitBlocksToUse.Add(ExoUnitType.Guardians);
            options.UnitBlocksToUse.Add(ExoUnitType.DireGuardians);
            options.UnitBlocksToUse.Add(ExoUnitType.ExoLeaders);
            if ( BaseInfo.SpireCities.Count > 3 )
                options.newExoLeaderTag="ExtragalacticWar";

            GameEntity_Squad.ReleaseTemporarySquadList( workingTargets );
            options.exoText = "AI 正在因你的新尖塔城市派遣银河外打击部队！";
            ExoGalacticAttackManager.SendExoGalacticAttack( options, Context );
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.FallenSpire );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "FallenSpire-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f ) : null;
            int debugCode = 0;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                debugCode = 1000;
                this.AttachedFaction.HasBeenSeenByPlayer = true;
                debugCode = 1010;
                debugCode = 1020;
                debugCode = 1030;
                FallenSpireSharedDeepInfo.Instance.DoInitializationIfNecessary( this.AttachedFaction, this.BaseInfo, Context, tracing );

                debugCode = 1100;

                debugCode = 2000;
                FallenSpireSharedDeepInfo.Instance.UpdateExoData( AttachedFaction, BaseInfo, Context, tracing );

                /* Spire debris comes in a few mechanisms. First, spawn it. We will need some specific spawning code to place it "close enough" to the player, Then see whether it's been long enough to despawn it. If despawning
                   then it gives buffs to non-player-allied minor factions. Priority: minor faction, then AI. */
                debugCode = 3000;
                FallenSpireSharedDeepInfo.Instance.HandleSpireDebris( AIFactionsForDebris, OtherFactionsForDebris, this.AttachedFaction, this.BaseInfo, Context, tracing );
                debugCode = 5050;
                //get any attached fleets set up, if we need to.
                FallenSpireSharedDeepInfo.Instance.RecalculateSpireFleetsAndFlagships_MainThreadSimOnly( this.AttachedFaction, this.BaseInfo, Context );
                debugCode = 5200;
                //and what the contents of each mobile fleet should be, after THAT
                FallenSpireSharedDeepInfo.Instance.RecalculateSpireCityMobileFleetContents_MainThreadSimOnly( this.AttachedFaction, this.BaseInfo, Context );
                debugCode = 6000;

                debugCode = 7000;
                FallenSpireSharedDeepInfo.Instance.HandleImperialSpire( AttachedFaction, chokeStrengths, BaseInfo, Context, pathingCacheData );

                HandleJournal( AttachedFaction, Context );
                CleanupCitiesIfNecessary( AttachedFaction, Context );
                //For FleetMetrics
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( EntityRollupType.MobileFleetFlagships ) )
                {
                    Fleet fleet = entity.GetFleetOrNull_Safe();
                    if ( fleet != null && !(fleet.BaseInfo is HumanMobileFleetBaseInfo) )
                        fleet.CreateExternalBaseInfo<HumanMobileFleetBaseInfo>( "HumanMobileFleetBaseInfo" );
                }

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Hit exception in fallen spire stage3 sim. debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();

                #region Tracing
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
            }
        }
        #region CleanupCitiesIfNecessary
        private void CleanupCitiesIfNecessary( Faction faction, ArcenHostOnlySimContext Context )
        {
            //its possible for the player to spam-click cities and build too many, so clean up if they have done so
            PlanetsWithCities.Clear();
            foreach ( GameEntity_Squad city in BaseInfo.SpireCities.DisplaySquads() )
            {
                if ( PlanetsWithCities[city.Planet] )
                {
                    //we already have a city here! blow up the new one
                    city.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                }
                PlanetsWithCities[city.Planet] = true;
            }
        }
        #endregion
        #region SpireJournal
        private void HandleJournal( Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( World_AIW2.Instance.GameSecond < 1 ||
                 World_AIW2.Instance.GameSecond % 10 == 0 ) //This line supports save games from the pre-release beta period; it can be removed
            {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_InfusedEmpireIntroduction", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_SimCityOverview", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "TSR_Spire_InfusedEmpireModules", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
        }
        #endregion



        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.FallenSpire );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "FallenSpire-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            int debugCode = 0;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                if ( BaseInfo == null || BaseInfo.Teams == null )
                    return;
                UnassignedShips.Clear();
                KingKillersByPlanet.Clear();
                TeamsAimedAtPlanet.Clear();
                AlliedCommandStations.Clear();
                debugCode = 20;
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
                {
                    debugCode = 40;
                    if ( team != null )
                        team.DeepInfo.Reset(); //reset team count information (I think this can be null right after game load?)
                }
                debugCode = 100;
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( planet.GetControllingFaction().GetIsFriendlyTowards( AttachedFaction ) )
                    {
                        GameEntity_Squad commandStation = planet.GetCommandStationOrNull();
                        if ( commandStation != null )
                            AlliedCommandStations.Add( commandStation );
                    }
                }
                debugCode = 200;
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    debugCode = 300;
                    if ( entity.TypeData.GetHasTag( "SpireDebris" ) )
                        continue;
                    if ( entity.TypeData.GetHasTag( "KingKiller" ) )
                    {
                        //TODO: remove this and check the actual fireteam logic, since they shouldn't be so cowardly.
                        //I might need to let factions set their aggressiveness tunables, or to make factions
                        //happier to attack non-players.
                        //Then add this back in to test the new logic
                        if ( entity.HasQueuedOrders() )
                            continue; //if we are going somewhere, don't get new orders
                        KingKillersByPlanet[entity.Planet].Add( entity );
                        continue;
                    }
                    if ( !entity.TypeData.GetHasTag( "ImperialSpire" ) )
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
                debugCode = 400;

                if ( BaseInfo.ImperialFleetActive )
                    FactionUtilityMethods.Instance.FlushUnitsFromReinforcementPointsOnAllRelevantPlanets( AttachedFaction, Context, 5f );
                debugCode = 700;
                FInt overkillRequired = FInt.FromParts( 0, 850 );
                FireteamUtility.UpdateFireteams( AttachedFaction, Context, pathingCacheData, BaseInfo.Teams, TeamsAimedAtPlanet, tracingBuffer, overkillRequired );
                debugCode = 800;
                FireteamUtility.UpdateRegiments( AttachedFaction, Context, pathingCacheData, BaseInfo.Teams, TeamsAimedAtPlanet, tracingBuffer, this.MinFireteamStrength, true );
                debugCode = 900;
                for ( int i = 0; i < UnassignedShips.Count; i++ )
                    AssignUnitToFireteam( AttachedFaction, UnassignedShips[i].GetSquad(), Context, pathingCacheData );
                debugCode = 1000;
                //And now handle KingKillers; these units will just try to kill an AI king
                //so we'll have one set of units spreading out and generally attacking, and others going directly for a king. Should be interesting
                foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in KingKillersByPlanet )
                {
                     debugCode = 1100;
                     Planet planet = pair.Key;
                     if ( tracing )
                         tracingBuffer.Add( "We have " + pair.Value.Count + " spire infused king killers on " + planet.Name ).Add( "\n" );

                     debugCode = 1200;
                     var factionData = planet.GetStanceDataForFaction( AttachedFaction );
                     if ( factionData[FactionStance.Hostile].TotalStrength > 100 * 1000 )
                     {
                         if ( tracing )
                             tracingBuffer.Add( "\tThere are too many enemies here (strength " + factionData[FactionStance.Hostile].TotalStrength + " opposed to " + this.AttachedFaction.ToString() + "); stay and fight\n" );
                         continue; //if there are a reasonable number of enemies, fight them. Smaller numbers can be ignored
                     }
                     debugCode = 1400;
                     Planet dest = FactionUtilityMethods.Instance.GetKingKillerTarget( planet, Context );
                     if ( dest == null)
                     {
                         if ( tracing )
                             tracingBuffer.Add( "\tno ai factions found\n" );

                         continue;
                     }
                     debugCode = 1500;
                     PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "FallenSpireKingKillersLRP", planet, dest, PathingMode.Shortest, Context, pathingCacheData );
                     debugCode = 1600;
                     if ( pathCache != null && pathCache.PathToReadOnly.Count >= 1 )
                     {
                         if ( tracing )
                         {
                             if ( pathCache.PathToReadOnly.Count > 1 )
                                 tracingBuffer.Add( "\tfinding a path to " + dest.Name + ". 1 " + pathCache.PathToReadOnly[0].Name + " 2 " + pathCache.PathToReadOnly[1].Name + "\n" );
                             else
                                 tracingBuffer.Add( "\tfinding a path to " + dest.Name + ". 1 " + pathCache.PathToReadOnly[0].Name + "\n" );
                         }
                         debugCode = 1700;
                         Planet nextPlanet = pathCache.PathToReadOnly[1];
                         debugCode = 1800;
                         for ( int i = 0; i < pathCache.PathToReadOnly.Count; i++ )
                         {
                             debugCode = 1900;
                             //find the next enemy planet on the way to this king, then go there
                             nextPlanet = pathCache.PathToReadOnly[i];
                             factionData = nextPlanet.GetStanceDataForFaction( AttachedFaction );
                             if ( factionData[FactionStance.Hostile].TotalStrength > 100 * 1000 )
                                 break;
                         }
                         debugCode = 2000;
                         if ( tracing )
                             tracingBuffer.Add( "\tHeading to " + dest.Name + " but next stop, " + nextPlanet.Name +"\n");
                         FactionUtilityMethods.Instance.Helper_RaidSpecificPlanet( pair.Value, planet, AttachedFaction,
                                                                               World_AIW2.Instance.CurrentGalaxy, nextPlanet, true, Context, pathingCacheData, 5f );
                     }
                     else
                     {
                         debugCode = 2100;
                         if ( tracing )
                             tracingBuffer.Add( "\tConfused code path\n");
                     }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in spire infused empire LRP, debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();

                #region Tracing
                if ( tracing && !tracingBuffer.GetIsEmpty() ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " " + AttachedFaction.FactionIndex + " DoLongRangePlanning trace ends" );
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
            }
            FleetBehaviorLRP.DoLRP( AttachedFaction, Context );
        }

        //prefer close ones that you can get to safely
        private void AssignUnitToFireteam( Faction faction, GameEntity_Squad entity, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( entity == null )
                return;

            AvailableFireteams.Clear();
            bool debug = false;
            if ( BaseInfo.Teams.GetItemCount() == 0 )
            {
                Fireteam team = Fireteam.CreateNewWithIDFromList( BaseInfo.Teams );
                team.StrengthToBringOnline = this.MinFireteamStrength + Context.RandomToUse.Next( 0, MaxFireteamStrength - MinFireteamStrength );
                team.MyStrengthMultiplierForStrengthCalculation = FInt.FromParts( 1, 100 );
                team.EnemyStrengthMultiplierForStrengthCalculation = FInt.FromParts( 1, 000 );
                BaseInfo.Teams.AddIfNotAlreadyIn( team );
            }
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
            {
                if ( team.status == FireteamStatus.Disbanded ||
                        team.status == FireteamStatus.ReadyToAttack ||
                        team.status == FireteamStatus.Attacking )
                    continue; //if a fireteam is ready to fight, don't send more ships to that team

                if ( team.DeepInfo.CurrentPlanet == null )
                {
                    //this is a brand new fireteam, so it's totally safe.
                    AvailableFireteams.AddIfNotAlreadyIn( team );
                    continue;
                }
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
            if ( AvailableFireteams.GetItemCount() > 5 )
                percentNewTeam = 0;

            if ( Context.RandomToUse.Next( 0, 100 ) < percentNewTeam || AvailableFireteams.GetItemCount() == 0 )
            {
                Fireteam team = Fireteam.CreateNewWithIDFromList( BaseInfo.Teams );
                team.MyStrengthMultiplierForStrengthCalculation = FInt.FromParts( 1, 100 );
                team.EnemyStrengthMultiplierForStrengthCalculation = FInt.FromParts( 1, 000 );

                team.StrengthToBringOnline = this.MinFireteamStrength + Context.RandomToUse.Next( 0, MaxFireteamStrength - MinFireteamStrength );
                team.PercentDistanceBestTarget = 45; //split up a lot
                team.PreferredMaxDistance = 5;
                team.DeepInfo.AddUnit( entity );
                entity.FireteamId = team.FireTeamID;

                team.DeepInfo.IdentifyCurrentPlanet(); //in case this unit is the first unit
                BaseInfo.Teams.AddIfNotAlreadyIn( team );
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
            for ( int i = 0; i < AlliedCommandStations.Count; i++ )
            {
                GameEntity_Squad outpost = AlliedCommandStations[i].GetSquad();
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

        //Set immediately before PreferredTargets.Sort(...) so the comparison can be a non-capturing
        //static delegate.  [ThreadStatic] because this runs on a background non-sim thread.
        [ThreadStatic] private static Planet cb_ftCurrentPlanet;
        [ThreadStatic] private static Faction cb_ftAttachedFaction;
        [ThreadStatic] private static FInt cb_ftFalloff;

        public override void GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass( bool DefenseMode, Planet CurrentPlanetForFireteam,
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, List<FireteamTarget> PreferredTargets, List<FireteamTarget> FallbackTargets, object TeamObj )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "FallenSpire-GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            FInt falloffForDistance = FInt.FromParts( 0, 050 );
            GetPreferredImperialSpireTargets( PreferredTargets, AttachedFaction, Context );
            GetFallbackImperialSpireTargets( FallbackTargets, AttachedFaction, Context ); //this is used so the imperial spire can also go after minor factions
            //sort targets by how hard it is to get there
            for ( int i = 0; i < PreferredTargets.Count; i++ )
            {
                FireteamTarget target = PreferredTargets[i];
                Int16 hops = 0;
                target.dangerOfTarget = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForFireteam, PreferredTargets[i].planet, true, out hops );
                PreferredTargets[i] = target;
            }
            for ( int i = PreferredTargets.Count - 1; i >= 0; i-- )
            {
                FireteamTarget target = PreferredTargets[i];
                if ( Fireteam.IsThisAWinningBattle( AttachedFaction, Context, target.planet, 4 ) || target.dangerOfTarget == -1 )
                    PreferredTargets.Remove( target );
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

                return rDanger.CompareTo( lDanger ); //prefer stronger targets
            } );



            // for ( int i = 0; i < PreferredTargets.Count; i++ )
            // {
            //     //discard things that are enough more dangerous than other things on the list
            //     if ( i == 0 ) continue;
            //     FireteamTarget target = PreferredTargets[i];
            //     FireteamTarget weakestTarget = PreferredTargets[0];
            //     FInt danger = (FInt)target.dangerOfTarget;
            //     FInt weakestDanger = (FInt)weakestTarget.dangerOfTarget;
            //     if ( target.planet.GetControllingOrInfluencingFaction() == faction )
            //         danger /= 2;
            //     if ( danger > weakestDanger * FInt.FromParts( 2, 000 ) )
            //     {
            //         //this is way more dangerous than earlier targets on the list, so ignore it
            //         PreferredTargets.RemoveRange( i, PreferredTargets.Count - i );
            //     }

            // }
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


            bool debug = false;
            if ( debug && tracing )
            {
                tracingBuffer.Add( "Getting lurk/target Preferred Targets\n" );
                for ( int i = 0; i < PreferredTargets.Count; i++ )
                    tracingBuffer.Add( "\t" ).Add( PreferredTargets[i].GetPlanetName_Safe() ).Add( " difficulty " ).Add( PreferredTargets[i].dangerOfTarget ).Add( " \n" );
                tracingBuffer.Add( "Getting lurk/target Fallback Targets\n" );
                for ( int i = 0; i < FallbackTargets.Count; i++ )
                    tracingBuffer.Add( "\t" ).Add( FallbackTargets[i].GetPlanetName_Safe() ).Add( "\n" );
            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        public void GetPreferredImperialSpireTargets( List<FireteamTarget> ListToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            ListToFill.Clear();
            bool foundKingUnderAttack = false;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null )
                    continue;
                if ( otherFaction.GetIsFriendlyTowards( faction ) )
                {
                    foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.KingUnitsOnly ) )
                    {
                        //Check if the king here is under attack; if so, help it
                        var pFaction = entity.Planet.GetStanceDataForFaction( faction );
                        if ( pFaction[FactionStance.Hostile].TotalStrength > 0 )
                        {
                            ListToFill.Add( new FireteamTarget( entity.Planet ) );
                            foundKingUnderAttack = true;
                        }
                    }
                }
                if ( foundKingUnderAttack )
                    continue;
                if ( !otherFaction.GetIsHostileTowards( faction ) )
                    continue;
                bool foundGuardpost = false;
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.AIPOnDeath ) )
                {
                    //take out dire guard posts in particular
                    if ( entity.TypeData.SpecialType != SpecialEntityType.DireGuardPost )
                        continue;
                    ListToFill.Add( new FireteamTarget( entity ) );
                    foundGuardpost = true;
                }

                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    if ( foundGuardpost )
                        break; //not until all the guard posts are dead
                    ListToFill.Add( new FireteamTarget( entity ) );
                }

            }
            for ( int i = 0; i < ListToFill.Count; i++ )
            {
                FireteamTarget target = ListToFill[i];
                int ignored = 0;
                target.dangerOfTarget = Fireteam.GetPlanetDefensiveStrength( target.planet, faction, true, ref ignored, FInt.Zero, FInt.Zero );
            }
        }
        public void GetFallbackImperialSpireTargets( List<FireteamTarget> ListToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            ListToFill.Clear();
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null )
                    continue;

                if ( !otherFaction.GetIsHostileTowards( faction ) )
                    continue;
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.GrantsMinorFactionPlanetControl ) )
                {
                    //allows the imperial spire to go after hostile minor factions
                    ListToFill.Add( new FireteamTarget( entity ) );
                }
            }

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( faction );
                int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                int myStrength = myFactionData[FactionStance.Self].TotalStrength + myFactionData[FactionStance.Friendly].TotalStrength;
                if ( hostileStrength > 10000 )
                    ListToFill.Add( new FireteamTarget( planet ) );
            }

            for ( int i = 0; i < ListToFill.Count; i++ )
            {
                FireteamTarget target = ListToFill[i];
                int ignored = 0;
                target.dangerOfTarget = Fireteam.GetPlanetDefensiveStrength( target.planet, faction, true, ref ignored, FInt.Zero, FInt.Zero );
            }
        }
        public override Planet GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass( Planet TargetPlanet, int TeamStrength, Planet CurrentPlanetForTeam,
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            Planet bestPlanet = null;
            int dangerOfPathFromBestPlanet = -1;
            //int dangerOfPathToBestPlanet = 999999;
            int distanceFromBestPlanet = 9999;
            Int16 hopsFromBestPlanet = 9999;
            int unused = 0;
            //this logic partially cribbed from IndependentAIFleet.cs::Helper_DoTargetFindingSweep

            if ( TargetPlanet == null )
                throw new Exception( "No target planet set in get lurk planet?!" );
            if ( TargetPlanet == CurrentPlanetForTeam )
                return TargetPlanet;
            //int debugCode = 0;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "FallenSpire-GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            bool preferUnwatchedPlanets = false;
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
                //the Imperial spire doesn't care about the difficulty of getting to a target
                //                int totalDifficultyOfPathToLurkPlanet = Fireteam.GetDangerOfPath( faction, Context, CurrentPlanetForTeam, planet, true, out hops );
                //                if ( totalDifficultyOfPathToLurkPlanet >= TeamStrength * 5 ) //as long as they only outnumber us 5:1, let's go!
                //                    continue;
                int totalDifficultyOfPathToTarget = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, planet, TargetPlanet, true, out hops );
                if ( preferUnwatchedPlanets && planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    totalDifficultyOfPathToTarget *= 2; //penalized watched planets if desired

                if ( totalDifficultyOfPathToTarget < 0 )
                    totalDifficultyOfPathToTarget = 0; //pathing through allied planets is basically the same

                if ( tracing )
                    tracingBuffer.Add( "\tConsidering " + planet.Name + " danger of path " + totalDifficultyOfPathToTarget + " distance " + Distance + " intel " + planet.IntelLevel ).Add( "\n" );

                if ( dangerOfPathFromBestPlanet == -1 || dangerOfPathFromBestPlanet > totalDifficultyOfPathToTarget )
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
            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            return bestPlanet;
        }

        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            FallenSpireSharedDeepInfo.Instance.SpawnDragons( Context );
        }

        public override void DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly( ref int debugStage, GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull,
              Faction factionThatKilledEntity, Faction entityOwningFaction, int numExtraStacksKilled, ArcenHostOnlySimContext Context )
        {
            base.DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly( ref debugStage, entity, Damage, FiringSystemOrNull, factionThatKilledEntity, entityOwningFaction, numExtraStacksKilled, Context);
            HumanFactionSharedDeep.CheckAndHandleStationDeath( AttachedFaction, ref debugStage, entity, Damage, FiringSystemOrNull, factionThatKilledEntity, entityOwningFaction, numExtraStacksKilled, Context );
        }

        public override bool SeedUnitsOnStartingPlanetDuringMapGen( Planet StartingPlanet, ConfigurationForFaction factionConfig, PlanetFaction pFaction, 
            ref ArcenPoint commandStationPoint, ref bool stillNeedsToSeedHumanHomeworldStuff, 
            Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull, ArcenHostOnlySimContext Context )
        {
            int debugIndex = 0;
            try
            {
                debugIndex = 10;

                GameEntityTypeData humanKingUnitData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "NormalHumanEmpireHumanHomeCommand" );

                if ( humanKingUnitData == null )
                    throw new Exception( "No humanKingUnitData could be found!  Evidently nothing has the tag of NormalHumanEmpireHumanHomeCommand." );

                debugIndex = 100;
                int offsetDistanceMin = humanKingUnitData.ForMark[Balance_MarkLevelTable.Instance.MaxOrdinal].Radius * 2;
                int offsetDistanceMax = offsetDistanceMin * 3;
                int minDistance = 30000;
                int loop = 0;

                debugIndex = 200;

                commandStationPoint = ArcenPoint.OutOfRange;
                do
                {
                    if ( commandStationPoint == ArcenPoint.OutOfRange )
                        commandStationPoint = StandardMapPopulator.EnsureFarFromWormholesIfPossible( offsetDistanceMin, offsetDistanceMax, minDistance,
                            30, Engine_AIW2.Instance.CombatCenter, StartingPlanet, Context );

                    minDistance -= 1000;
                }
                while ( loop++ < 30 && commandStationPoint == ArcenPoint.OutOfRange );
                
                GameEntity_Squad king = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, humanKingUnitData, humanKingUnitData.MarkFor( pFaction ),
                               pFaction.FleetUsedAtPlanet, 0, commandStationPoint, Context, "SpireInfusedEmpireStartSpawn" );

                stillNeedsToSeedHumanHomeworldStuff = false;
                debugIndex = 500;
                pFaction.SetPlanetFactionBooleanFlag( PlanetFactionBooleanFlag.TryToCapture, true );
                debugIndex = 600;

                FInt placementOffsetScale = FInt.FromParts( 1, 500 );

                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeForcefield )
                    StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, commandStationPoint, pFaction, placementOffsetScale, "StartingForcefieldGenerator", -400, 0 );
                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeEngineers )
                {
                    StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, commandStationPoint, pFaction, placementOffsetScale, "Engineer", -750, 400 );
                    StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, commandStationPoint, pFaction, placementOffsetScale, "Engineer", -600, 400 );
                }
                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeFactory )
                    StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, commandStationPoint, pFaction, placementOffsetScale, "Factory", 700, 0 );

                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeHumanSettlement )
                {
                    int numberOfHomeHumanSettlements = factionConfig.GetIntValueForCustomFieldOrDefaultValue( "HomeHumanSettlementsToStartWith", true );

                    for ( int index = 0; index < numberOfHomeHumanSettlements; index++ )
                        StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, commandStationPoint, pFaction, placementOffsetScale, "HomeHumanSettlement", -1000 + (200 * index), -400 );
                }

                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeHumanCryogenicPods )
                {
                    int numberOfHumanCryoPods = factionConfig.GetIntValueForCustomFieldOrDefaultValue( "HumanCryogenicPodsToStartWith", true );
                    int cryoPodsSoFarThisRow = 0;
                    int cryoPodYoffsetDistanceMax = -600;
                    for ( int index = 0; index < numberOfHumanCryoPods; index++ )
                    {
                        if ( cryoPodsSoFarThisRow >= 10 )
                        {
                            cryoPodsSoFarThisRow = 0;
                            cryoPodYoffsetDistanceMax -= 120;
                        }
                        StandardMapPopulator.Helper_SeedStartingBaseUnit( Context, commandStationPoint, pFaction, placementOffsetScale, "HumanCryogenicPod", -1000 + (120 * cryoPodsSoFarThisRow), cryoPodYoffsetDistanceMax );
                        cryoPodsSoFarThisRow++;
                    }
                }
                debugIndex = 2000;
                int innerSystemMinimumRadius = (StartingPlanet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 150 )).IntValue;
                int innerSystemMaximumRadius = (StartingPlanet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 300 )).IntValue;

                debugIndex = 2100;

                if ( !World_AIW2.Instance.GetIsTutorial() ) //test ships only seed on the human homeworld now in non-tutorials.
                {
                    IList<GameEntityTypeData> testShipDatas = GameEntityTypeDataTable.Instance.RowsByRollup[EntityRollupType.PlayerTestShip];
                    for ( int j = 0; j < testShipDatas.Count; j++ )
                    {
                        GameEntityTypeData testShipData = testShipDatas[j];
                        //int distanceToTestUnit = Context.RandomToUse.Next( innerSystemMinimumRadius, innerSystemMaximumRadius );
                        ArcenPoint otherPoint = Engine_AIW2.Instance.CombatCenter.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                        GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, testShipData, testShipData.MarkFor( pFaction ),
                            pFaction.FleetUsedAtPlanet, 0, otherPoint, Context, "SpireInfusedEmpireStartSpawn" );
                    }
                }
                debugIndex = 3000;
                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeInitialCombatFleet )
                {
                    FleetDesignTemplate initialPlayerFleet = FleetDesignTemplateTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( "StartingFleet", false ) );
                    if ( TutorialPlanetOrNull != null && TutorialPlanetOrNull.InitialPlayerCombatFleet != null )
                        initialPlayerFleet = TutorialPlanetOrNull.InitialPlayerCombatFleet;
                    if ( initialPlayerFleet == null ) //the "Random" name won't be found, so this will suffice to say "or random!"
                    {
                        //if was null or random, choose one at random.
                        initialPlayerFleet = (FleetDesignTemplate)FleetDesignTemplateTable.Instance.InitialPlayerFleetsAsBaseRows[Engine_Universal.PermanentQualityRandom.Next( 0, FleetDesignTemplateTable.Instance.InitialPlayerFleetsAsBaseRows.Count )];
                    }

                    FleetItem centerpiece = initialPlayerFleet.GetCategory( FleetItemDrawBagCategory.Centerpiece ).DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( centerpiece == null || centerpiece.TypeData == null )
                        Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null centerpiece found in InitialPlayerFleet " + initialPlayerFleet.InternalName );
                    else
                    {
                        debugIndex = 3500;
                        ArcenPoint entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                        entityPoint = StartingPlanet.GetSafePlacementPoint_SpecificPoint( Context, centerpiece.TypeData, entityPoint, 200, 1000 );
                        GameEntity_Squad actualCenterpiece = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, centerpiece.TypeData, 1, null, 0, entityPoint, Context, "SpireInfusedEmpireStartSpawn" );
                        if ( actualCenterpiece == null )
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null actualCenterpiece found in InitialPlayerFleet FleetItem " + centerpiece.TypeData.InternalName );
                        else
                        {
                            Fleet centerpieceFleet = actualCenterpiece.GetFleetOrNull_Safe();
                            if ( centerpieceFleet != null )
                            {
                                centerpieceFleet.ClearAllMembershipsExceptForCenterpiece();
                                centerpieceFleet.SetAllMembershipsUpFromDesignTemplates_HostOnly( initialPlayerFleet, FleetDesignLogic.Unused, null );
                            }
                        }
                    }
                }

                bool isExpertMode = World_AIW2.Instance.CampaignType.HarshnessRating >= 200;

                debugIndex = 4000;
                //Battlestation1
                bool wasRandomBattle1 = false;
                FleetDesignTemplate itemToAvoidForBattle2 = null;
                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeInitialBattlestation )
                {
                    FleetDesignTemplate initialPlayerBattlestation1 = FleetDesignTemplateTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( "StartingBattlestation1", false ) );
                    if ( initialPlayerBattlestation1 == null ) //the "Random" name won't be found, so this will suffice to say "or random!"
                    {
                        wasRandomBattle1 = true;
                        //if was null or random, choose one at random.
                        retry:
                        initialPlayerBattlestation1 = (FleetDesignTemplate)FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows[Engine_Universal.PermanentQualityRandom.Next( 0, FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows.Count )];
                        if ( initialPlayerBattlestation1.WeightInDrawBags == 0 )
                            goto retry;
                    }
                    if ( TutorialPlanetOrNull != null && TutorialPlanetOrNull.InitialPlayerBattlestationFleet != null )
                        initialPlayerBattlestation1 = TutorialPlanetOrNull.InitialPlayerBattlestationFleet;

                    if ( !wasRandomBattle1 )
                    {
                        //if they choose the turtle option, then yell at them
                        if ( isExpertMode && initialPlayerBattlestation1.InternalName == "Mod_TurtleDefenses" )
                        {
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Whoah, apologies!  The starting battlestation of " + initialPlayerBattlestation1.DisplayName + " is too powerful.  Please choose another." );
                            return false;
                        }
                    }
                    else
                    {
                        if ( isExpertMode ) //if they were given the turtle option randomly, give them something else in expert mode
                        {
                            while ( initialPlayerBattlestation1.InternalName == "Mod_TurtleDefenses" || initialPlayerBattlestation1.WeightInDrawBags == 0)
                                initialPlayerBattlestation1 = (FleetDesignTemplate)FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows[Engine_Universal.PermanentQualityRandom.Next( 0, FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows.Count )];
                        }
                    }

                    itemToAvoidForBattle2 = initialPlayerBattlestation1;

                    FleetItem centerpiece = initialPlayerBattlestation1.GetCategory( FleetItemDrawBagCategory.Centerpiece ).DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( centerpiece == null || centerpiece.TypeData == null )
                        Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null centerpiece found in InitialPlayerBattlestation " + initialPlayerBattlestation1.InternalName );
                    else
                    {
                        debugIndex = 4500;
                        ArcenPoint entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                        entityPoint = StartingPlanet.GetSafePlacementPoint_SpecificPoint( Context, centerpiece.TypeData, entityPoint, 200, 1000 );
                        GameEntity_Squad actualCenterpiece = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, centerpiece.TypeData, 1, null, 0, entityPoint, Context, "SpireInfusedEmpireStartSpawn" );
                        if ( actualCenterpiece == null )
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null actualCenterpiece found in InitialPlayerBattlestation FleetItem " + centerpiece.TypeData.InternalName );
                        else
                        {
                            Fleet centerpieceFleet = actualCenterpiece.GetFleetOrNull_Safe();
                            if ( centerpieceFleet != null )
                            {
                                centerpieceFleet.ClearAllMembershipsExceptForCenterpiece();
                                centerpieceFleet.SetAllMembershipsUpFromDesignTemplates_HostOnly( initialPlayerBattlestation1, FleetDesignLogic.Unused, null );

                                GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                                command.RelatedIntegers.Add( centerpieceFleet.FleetID ); //FleetID
                                command.RelatedIntegers.Add( PlayerAccount.Local.PlayerPrimaryKeyID );
                                command.RelatedString = "ToggleIsFleetOnPlayerWatchlist";
                                command.RelatedBool = true;
                                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                            }
                        }
                    }
                }

                debugIndex = 5000;
                //Battlestation2
                if ( TutorialPlanetOrNull == null && !isExpertMode ) //aka "don't give me a second battlestation in expert mode!"
                {
                    FleetDesignTemplate initialPlayerBattlestation2 = FleetDesignTemplateTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( "StartingBattlestation2", false ) );
                    bool wasRandomBattle2 = false;
                    if ( initialPlayerBattlestation2 == null || initialPlayerBattlestation2 == itemToAvoidForBattle2 ) //the "Random" name won't be found, so this will suffice to say "or random!"
                    {
                        wasRandomBattle2 = true;
                        //if was null or random, choose one at random.
                        retry:
                        initialPlayerBattlestation2 = (FleetDesignTemplate)FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows[Engine_Universal.PermanentQualityRandom.Next( 0, FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows.Count )];
                        if ( initialPlayerBattlestation2.WeightInDrawBags == 0 )
                            goto retry;
                    }
                    int loopCount = 0;
                    while ( (wasRandomBattle2 || wasRandomBattle1) && initialPlayerBattlestation2 == itemToAvoidForBattle2 && loopCount++ < 1000 )
                    {
                        //if either was random, make sure that the two are not identical
                        retry:
                        initialPlayerBattlestation2 = (FleetDesignTemplate)FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows[Engine_Universal.PermanentQualityRandom.Next( 0, FleetDesignTemplateTable.Instance.InitialPlayerBattlestationsAsBaseRows.Count )];
                        if ( initialPlayerBattlestation2.WeightInDrawBags == 0 )
                            goto retry;
                    }
                    
                    FleetItem centerpiece = initialPlayerBattlestation2.GetCategory( FleetItemDrawBagCategory.Centerpiece ).DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( centerpiece == null || centerpiece.TypeData == null )
                        Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null centerpiece found in InitialPlayerBattlestation " + initialPlayerBattlestation2.InternalName );
                    else
                    {
                        debugIndex = 5500;
                        ArcenPoint entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                        entityPoint = StartingPlanet.GetSafePlacementPoint_SpecificPoint( Context, centerpiece.TypeData, entityPoint, 200, 1000 );
                        GameEntity_Squad actualCenterpiece = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, centerpiece.TypeData, 1, null, 0, entityPoint, Context, "SpireInfusedEmpireStartSpawn" );
                        if ( actualCenterpiece == null )
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null actualCenterpiece found in InitialPlayerBattlestation FleetItem " + centerpiece.TypeData.InternalName );
                        else
                        {
                            Fleet centerpieceFleet = actualCenterpiece.GetFleetOrNull_Safe();
                            if ( centerpieceFleet != null )
                            {
                                centerpieceFleet.ClearAllMembershipsExceptForCenterpiece();
                                centerpieceFleet.SetAllMembershipsUpFromDesignTemplates_HostOnly( initialPlayerBattlestation2, FleetDesignLogic.Unused, null );
                            }
                        }
                    }
                }

                debugIndex = 6000;
                if ( TutorialPlanetOrNull == null || !TutorialPlanetOrNull.SkipPlayerHomeInitialSupportFleet )
                {
                    FleetDesignTemplate initialPlayerSupportFleet = FleetDesignTemplateTable.Instance.GetRowByNameOrNullIfNotFound( pFaction.Faction.GetStringValueForCustomFieldOrDefaultValue( "StartingSupportFleet", false ) );
                    if ( initialPlayerSupportFleet == null ) //the "Random" name won't be found, so this will suffice to say "or random!"
                    {
                        //if was null or random, choose one at random.
                        initialPlayerSupportFleet = (FleetDesignTemplate)FleetDesignTemplateTable.Instance.InitialPlayerSupportFleetsAsBaseRows[Engine_Universal.PermanentQualityRandom.Next( 0, FleetDesignTemplateTable.Instance.InitialPlayerSupportFleetsAsBaseRows.Count )];
                    }
                    if ( TutorialPlanetOrNull != null && TutorialPlanetOrNull.InitialPlayerSupportFleet != null )
                        initialPlayerSupportFleet = TutorialPlanetOrNull.InitialPlayerSupportFleet;

                    FleetItem centerpiece = initialPlayerSupportFleet.GetCategory( FleetItemDrawBagCategory.Centerpiece ).DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    if ( centerpiece == null || centerpiece.TypeData == null )
                        Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null centerpiece found in InitialPlayerSupportFleets " + initialPlayerSupportFleet.InternalName );
                    else
                    {
                        debugIndex = 6500;
                        ArcenPoint entityPoint = commandStationPoint.GetRandomPointWithinDistance( Context.RandomToUse, innerSystemMinimumRadius, innerSystemMaximumRadius );
                        entityPoint = StartingPlanet.GetSafePlacementPoint_SpecificPoint( Context, centerpiece.TypeData, entityPoint, 200, 1000 );
                        GameEntity_Squad actualCenterpiece = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, centerpiece.TypeData, 1, null, 0, entityPoint, Context, "SpireInfusedEmpireStartSpawn" );
                        if ( actualCenterpiece == null )
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "Null actualCenterpiece found in InitialPlayerSupportFleets FleetItem " + centerpiece.TypeData.InternalName );
                        else
                        {
                            Fleet centerpieceFleet = actualCenterpiece.GetFleetOrNull_Safe();
                            if ( centerpieceFleet != null )
                            {
                                centerpieceFleet.ClearAllMembershipsExceptForCenterpiece();
                                centerpieceFleet.SetAllMembershipsUpFromDesignTemplates_HostOnly( initialPlayerSupportFleet, FleetDesignLogic.Unused, null );
                            }
                        }
                    }
                }
                if ( TutorialPlanetOrNull == null && king != null )
                {
                    bool ExpertMode = pFaction.Faction.GetBoolValueForCustomFieldOrDefaultValue( "ExpertMode", false );
                    GameEntityTypeData galacticCapitalData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, ExpertMode ? "CapitolForSpireInfusedEmpire_Expert" :  "CapitolForSpireInfusedEmpire" );
                    ArcenPoint destinationPoint = king.Planet.GetSafePlacementPoint_AroundEntity( Context, galacticCapitalData, king, FInt.FromParts( 0, 025 ), FInt.FromParts( 0, 150 ) );
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( king.PlanetFaction, galacticCapitalData, 1,
                                                                  king.PlanetFaction.Faction.LooseFleet, 0, destinationPoint, Context, "SpireInfusedEmpireStartSpawn" );

                    Fleet cityFleet = newEntity.FleetMembership.Fleet;

                    cityFleet.NameRaw = "Spire City '" + FallenSpireSharedDeepInfo.Instance.RandomSpireCityName(Context, newEntity) + "'";
                    cityFleet.CreateExternalBaseInfo<FallenSpireCityFleetBaseInfo>("FallenSpireCityFleetBaseInfo");
                    
                }

            }
            catch ( Exception e)
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "HumanSpireInfusedEmpireSpecificCodeDeepInfo SeedUnitsOnStartingPlanetDuringMapGen error at debugIndex " + debugIndex + ": " + e );
                return false;
            }
            return true;
        }
    }
}
