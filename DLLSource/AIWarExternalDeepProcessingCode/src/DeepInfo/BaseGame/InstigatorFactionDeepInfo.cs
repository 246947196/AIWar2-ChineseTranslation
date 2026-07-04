using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public sealed class InstigatorFactionDeepInfo : ExternalFactionDeepInfoRoot, IExternalDeepInfo_Singleton
    {
        public InstigatorFactionBaseInfo BaseInfo;
        public static InstigatorFactionDeepInfo Instance = null;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<InstigatorFactionBaseInfo>();
            Instance = this;
        }

        protected override void Cleanup() 
        {
            //matters a huge amount
            HasCorrectedAIAllegiance = false;

            //might matter
            DifficultyOfStrongestAIFaction = 0;

            //probably does not matter
            unassignedThreatShipsByPlanet.Clear();
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 1;
        private int DifficultyOfStrongestAIFaction = 0;

        //PERF FIX (2026-05-29): how long to wait before retrying instigator-base placement after a failed
        //(no-valid-planet) attempt, so the expensive GetPlanetForInstigatorBase scan cannot run every single second.
        private const int SecondsToWaitAfterFailedBaseSpawnPlacement = 30;

        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
        }
        private readonly DictionaryOfLists<Planet, SafeSquadWrapper> unassignedThreatShipsByPlanet = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 60, "InstigatorFactionDeepInfo-unassignedThreatShipsByPlanet" );
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            #region Tracing
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Independents );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Instigator-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            #endregion
            unassignedThreatShipsByPlanet.Clear();
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            PlanetPathfinder pathfinderConservative = null;
            try
            {
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    if ( entity.TypeData.GetHasTag( "InstigatorBase" ) )
                        continue;
                    if ( entity.CalculateFinalDestinationPlanetIndex_Safe() != -1 &&
                         entity.CalculateFinalDestinationPlanetIndex_Safe() != entity.GetPlanetIndexSafe() )
                        continue; // if heading somewhere else, skip

                    InstigatorPerUnitBaseInfo localData = entity.CreateExternalBaseInfo<InstigatorPerUnitBaseInfo>( "InstigatorPerUnitBaseInfo" );
                    if ( !localData.UnitGoesForHumanKing )
                        continue;

                    Planet currentPlanet = entity.Planet;
                    int myCurrentStrength = currentPlanet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Self].TotalStrength;
                    int hostileStrength = currentPlanet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Hostile].TotalStrength;
                    if ( hostileStrength > myCurrentStrength / 10 )
                    {
                        //wait till the hostile strength is 1/10th the size of my forces to move on
                        entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //definitely breaks in multiplayer, because nonsim thread
                        continue;
                    }
                    unassignedThreatShipsByPlanet[entity.Planet].Add( entity );

                }
                Planet kingPlanet = FactionUtilityMethods.Instance.findHumanKing( false );
                if ( kingPlanet == null )
                {
                    // if(debug)
                    //     ArcenDebugging.ArcenDebugLogSingleLine("The human king is dead, so just chill", Verbosity.DoNotShow );
                    //The human king is dead? Just sit here now. I can probably do something more interesting later
                    return;
                }
                ExternalFactionBaseInfoRoot externalRoot = AttachedFaction.BaseInfo as ExternalFactionBaseInfoRoot;
                if ( externalRoot == null )
                {
                    ArcenDebugging.ArcenDebugLog( "Error! Could not get BaseInfo as ExternalFactionBaseInfoRoot for faction " + AttachedFaction.GetDisplayName(), Verbosity.ShowAsError );
                    return;
                }

                int pairCount = unassignedThreatShipsByPlanet.GetCountOfLists();
                foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in unassignedThreatShipsByPlanet )
                {
                    //Go to the next planet on the way. Note that we go only one planet at a time
                    //so we can correctly fight the defenses on that planet
                    if ( pair.Value.Count <= 0 )
                        continue;
                    Planet nextPlanet;

                    if ( pathfinderConservative == null )
                    {
                        pathfinderConservative = externalRoot.GetConservativePathfinderThatMustBeReleasedOrNull();
                        if ( pathfinderConservative == null )
                            pathfinderConservative = externalRoot.GetNormalPathfinderThatMustBeReleased();
                    }

                    PathBetweenPlanetsForFaction pathCache = PathingHelper.InnerFindPath_RawSinglePathfinder( pathfinderConservative, AttachedFaction, "InstigatorsLRP", pair.Key, kingPlanet, 
                        PathingMode.SomeRandomRequest, Context, pathingCacheData );
                    if ( pathCache == null || pathCache.PathToReadOnly.Count == 0 )
                    {
                        //We must already be on the human king planet, or there's no way to get there, so just chill I guess
                        continue;
                    }
                    nextPlanet = pathCache.PathToReadOnly[0];
                    GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_OtherRaidKing], GameCommandSource.AnythingElse );
                    command.RelatedString = "Instigator";
                    for ( int j = 0; j < pair.Value.Count; j++ )
                        command.RelatedEntityIDs.Add( pair.Value[j].PrimaryKeyID );
                    command.RelatedIntegers.Add( nextPlanet.Index );
                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Instigator LRP error: " + e, Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();
                if ( pathfinderConservative != null )
                    pathfinderConservative.ReturnToPool();

                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " Long Range Planning trace ends" );
                if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                #endregion
            }
        }

        private bool HasCorrectedAIAllegiance = false; //this runs once at game load time

        

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            if ( World_AIW2.Instance.GameSecond <= 1 )
                return;
            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Independents );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Instigator-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f ) : null;
            #endregion

            if ( this.DifficultyOfStrongestAIFaction == 0 )
            {
                int highestDifficulty = FactionUtilityMethods.Instance.GetHighestAIDifficulty();
                this.DifficultyOfStrongestAIFaction = highestDifficulty;
                if ( this.DifficultyOfStrongestAIFaction <= 5 )
                    this.BaseInfo.IntervalBetweenBaseSpawns = this.BaseInfo.IntervalBetweenBaseSpawnsLow;
                else if ( this.DifficultyOfStrongestAIFaction <= 7 )
                    this.BaseInfo.IntervalBetweenBaseSpawns = this.BaseInfo.IntervalBetweenBaseSpawnsMed;
                else
                    this.BaseInfo.IntervalBetweenBaseSpawns = this.BaseInfo.IntervalBetweenBaseSpawnsHigh;
            }
            if ( this.BaseInfo.IntervalBetweenBaseSpawns == 0 )
                throw new Exception( "Instigators unable to figure out how often they should spawn" );

            if ( this.BaseInfo.TimeForNextInstigatorBaseSpawn == -1 || World_AIW2.Instance.GameSecond == 1 )
            {
                this.BaseInfo.TimeForNextInstigatorBaseSpawn = this.BaseInfo.TimeForInitialBaseSpawn;
            }
            if ( this.BaseInfo.AIFactionIndexForNextSpawn == -1 ) //figure out which ai faction to help next
                this.BaseInfo.AIFactionIndexForNextSpawn = GetRandomAIFaction( Context ).FactionIndex;
            int anyHumanKings = FactionUtilityMethods.Instance.countHumanKings();
            if ( anyHumanKings == 0 )
                return; //there are no human kings, so nothing for us to do. This can happen in spectator mode
            if ( World_AIW2.Instance.GameSecond >= this.BaseInfo.TimeForNextInstigatorBaseSpawn && AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "EnableInstigators" ) )
            {
                if ( tracing )
                    tracingBuffer.Add( "Spawning new instigator base at " + World_AIW2.Instance.GameSecond );
                //Spawn a new InstigatorBase
                //First pick a reasonably defended AI planet
                //So iterate over planets, find one owned by an allied faction with reasonable defenses and then
                //drop an Instigator Base on it

                Planet planet = GetPlanetForInstigatorBase( AttachedFaction, Context );
                if ( planet == null )
                {
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "No suitable planet to spawn an instigator base " );
                    //PERF FIX (2026-05-29): GetPlanetForInstigatorBase is an expensive (up to 100-retry, galaxy-wide)
                    //scan. Previously this returned without advancing TimeForNextInstigatorBaseSpawn, so once the galaxy
                    //reached a state with no valid placement planet (e.g. some hostile strength on every AI-friendly
                    //planet), the full scan ran again every single second for the rest of the game, silently dragging
                    //sim speed to ~40%. Push the next attempt out so a failed placement costs at most one scan per
                    //retry delay instead of one per second.
                    this.BaseInfo.TimeForNextInstigatorBaseSpawn = World_AIW2.Instance.GameSecond + SecondsToWaitAfterFailedBaseSpawnPlacement;
                    return;
                }

                //Then add an Instigator base and choose a random Effect for it from the table
                GameEntity_Squad instigatorBase = SpawnInstigatorBase_ReturnNullIfMPClient( AttachedFaction, Context, planet );
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Spawning Instigator Base on " + planet.Name + ".\n" ).Add( "Spawn next base at " + this.BaseInfo.TimeForNextInstigatorBaseSpawn );
                this.BaseInfo.TimeForNextInstigatorBaseSpawn = World_AIW2.Instance.GameSecond + this.BaseInfo.IntervalBetweenBaseSpawns + Context.RandomToUse.Next( 0, 360 );
            }

            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "InstigatorBase" ) )
            {
                {
                    InstigatorPerUnitBaseInfo localData = entity.TryGetExternalBaseInfoAs<InstigatorPerUnitBaseInfo>();

                    if ( World_AIW2.Instance.GameSecond < localData.TimeForNextEffect )
                        continue;
                    InstigatorEffectData rowEntry = InstigatorDataTable.Instance.GetRowById( localData.InstigatorEffectIndex );
                    if ( rowEntry == null )
                        throw new Exception( "Could not find Instigator Effect with id " + localData.InstigatorEffectIndex );

                    localData.TimeForNextEffect = World_AIW2.Instance.GameSecond + rowEntry.IntervalInSeconds;
                    localData.CumulativeEffectSoFar += DoInstigatorEffect_AsPartOfMainSim( AttachedFaction, Context, entity, rowEntry );
                    localData.NumTimesEffectHappened++;
                    entity.FlagForForcedFullSyncToClients_FromHost();
                    this.BaseInfo.AIFactionIndexForNextSpawn = GetRandomAIFaction( Context ).FactionIndex;
                }
            }

            //There's a bug we've seen where sometimes an AI winds up hostile toward the instigators
            //I believe I've fixed it, but I want to correct any existing games with this problem.
            //This code added 8/9/2019 and should be removable eventually
            if ( !HasCorrectedAIAllegiance )
            {
                HasCorrectedAIAllegiance = true;
                for ( int j = 0; j < World_AIW2.Instance.Factions.Count; j++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[j];
                    if ( otherFaction.Type == FactionType.AI || otherFaction.SpecialFactionData.AlliedToAIByDefault )
                    {
                        AttachedFaction.MakeFriendlyTo( otherFaction );
                        otherFaction.MakeFriendlyTo( AttachedFaction );
                    }
                }
            }
            #region Tracing
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " PerSecondLogic ends" );
            if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            #endregion
        }
        //returns the strength of the effect
        public int DoInstigatorEffect_AsPartOfMainSim( Faction faction, ArcenHostOnlySimContext Context, GameEntity_Squad entity, InstigatorEffectData rowEntry )
        {
            #region Tracing
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Independents );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Instigator-DoInstigatorEffect_AsPartOfMainSim-trace", 10f ) : null;
            #endregion
            Faction targetFaction = null;
            if ( this.BaseInfo.AIFactionIndexForNextSpawn == -1 )
                targetFaction = GetRandomAIFaction( Context );
            else
                targetFaction = World_AIW2.Instance.GetFactionByIndex( this.BaseInfo.AIFactionIndexForNextSpawn );
            AISentinelsCoreData factionExternal = targetFaction.TryGetAISentinelsCoreData()?.SentinelInfo;
            if ( factionExternal == null )
                ArcenDebugging.ArcenDebugLogSingleLine( "could not find factionExternal for random AI faction", Verbosity.DoNotShow );
            FInt difficultyMultiplier;
            if ( this.DifficultyOfStrongestAIFaction <= 3 )
                difficultyMultiplier = rowEntry.EffectMultiplierLowDifficulty;
            else if ( this.DifficultyOfStrongestAIFaction <= 6 )
                difficultyMultiplier = rowEntry.EffectMultiplierMediumDifficulty;
            else
                difficultyMultiplier = rowEntry.EffectMultiplierHighDifficulty;

            if ( difficultyMultiplier == FInt.Zero )
                difficultyMultiplier = FInt.One; //if the multiplier is not set at all, do nothing

            FInt StrengthAssociated = rowEntry.BaseStrengthPerInterval + rowEntry.BonusStrengthPerAIP * GlobalAIWorldBaseInfo.Instance.AIProgress_Effective;
            StrengthAssociated *= difficultyMultiplier;
            int returnVal = StrengthAssociated.IntValue;
            if ( rowEntry.BudgetToBoost != AIBudgetType.None )
            {
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Adding " + StrengthAssociated + " to " + rowEntry.BudgetToBoost + " of AI faction " + targetFaction.FactionIndex );
                factionExternal.StoredAIPurchaseCostByBudget[rowEntry.BudgetToBoost] += StrengthAssociated;
            }
            if ( !string.IsNullOrEmpty( rowEntry.UnitTagsToSpawn ) )
            {
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Spawning " + StrengthAssociated + " worth of units with tag " + rowEntry.UnitTagsToSpawn );
                while ( StrengthAssociated > FInt.Zero )
                {
                    GameEntityTypeData entityData = null;
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, rowEntry.UnitTagsToSpawn );
                    if ( entityData == null )
                        throw new Exception( "Could not find any units with tag " + rowEntry.UnitTagsToSpawn );
                    PlanetFaction pFaction = entity.Planet.GetPlanetFactionForFaction( faction );
                    GameEntity_Squad newSpawn = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( targetFaction.CurrentGeneralMarkLevel ),
                        pFaction.FleetUsedAtPlanet, 0, entity.WorldLocation, Context, "Instigators-GeneralSpawn" );
                    if ( newSpawn != null )
                    {
                        InstigatorPerUnitBaseInfo localData = newSpawn.CreateExternalBaseInfo<InstigatorPerUnitBaseInfo>( "InstigatorPerUnitBaseInfo" );
                        localData.UnitGoesForHumanKing = true;
                        newSpawn.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //part of main sim, all good
                    }
                    StrengthAssociated -= entityData.MarkStatsFor( pFaction.Faction.CurrentGeneralMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership;
                }
            }
            if ( rowEntry.AIPIncrease > 0 )
            {
                //this is important enough that we want to warn the player
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Increasing AIP by " + rowEntry.AIPIncrease );
                if ( ArcenNetworkAuthority.GetIsHostMode() )
                {
                    SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( entity );

                    World_AIW2.Instance.QueueChatMessageOrCommand( "Instigator Base scanning is increasing AIP by " + rowEntry.AIPIncrease, ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                }
                GlobalAIWorldBaseInfo.Instance.ChangeAIP( rowEntry.AIPIncrease, AIPChangeReason.FactionEscalation, entity.TypeData, entity.GetFactionIndex_Safe(), entity.Planet.Index, -1 );
            }
            return returnVal;
        }
        private Faction GetRandomAIFaction( ArcenSimContextAnyStatus Context )
        {
            return World_AIW2.Instance.AIFactions[Context.RandomToUse.Next( 0, World_AIW2.Instance.AIFactions.Count )];
        }
        public GameEntity_Squad SpawnInstigatorBase_ReturnNullIfMPClient( Faction faction, ArcenHostOnlySimContext Context, Planet planet )
        {
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "InstigatorBase" );
            if ( entityData == null )
                throw new Exception( "Could not find InstigatorBase tag in XML" );
            //AngleDegrees angle = AngleDegrees.Create( (FInt)Context.RandomToUse.Next( 1, 360 ) );
            ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 300 ) );
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
            var instigatorBase = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "Instigators-NewBase" );

            InstigatorEffectData rowEntry = InstigatorDataTable.Instance.GetRandomRow( Context );
            if ( rowEntry == null )
                throw new Exception( "Could not find instigator effect row" );

            if ( instigatorBase == null )
                return null;

            if ( ArcenNetworkAuthority.GetIsHostMode() )
            {
                if ( instigatorBase.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
                {
                    SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( instigatorBase );

                    World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "Instigator Base</color> spawning on " +
                        instigatorBase.GetPlanetName_Safe(), ChatType.LogToCentralChat, "ArkChiefOfStaff_InstigatorBaseConstructed", chatHandlerOrNull );
                }
                else
                    World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "Instigator Base</color> spawning somewhere in the galaxy", 
                        ChatType.LogToCentralChat, "ArkChiefOfStaff_InstigatorBaseConstructed", null );
            }
            InstigatorPerUnitBaseInfo bData = instigatorBase.CreateExternalBaseInfo<InstigatorPerUnitBaseInfo>( "InstigatorPerUnitBaseInfo" );
            bData.InstigatorEffectIndex = rowEntry.id;
            bData.TimeForNextEffect = rowEntry.IntervalInSeconds + World_AIW2.Instance.GameSecond;
            instigatorBase.FlagForForcedFullSyncToClients_FromHost();

            return instigatorBase;
        }

        public Planet GetPlanetForInstigatorBase( Faction faction, ArcenHostOnlySimContext Context )
        {
            bool debug = false;
            var WorkingPlanetList = Planet.GetTemporaryPlanetList("InstigatorFactionDeepInfo-WorkingPlanetList",10.0f);
            if ( WorkingPlanetList == null ) //blocked for teardown/shutdown; bail
                return null;
            var WorkingPlanetListExploredOnly = Planet.GetTemporaryPlanetList("InstigatorFactionDeepInfo-WorkingPlanetListExploredOnly",10.0f);
            if ( WorkingPlanetListExploredOnly == null ) //blocked for teardown/shutdown; bail
            {
                Planet.ReleaseTemporaryPlanetList( WorkingPlanetList );
                return null;
            }

            FInt AIP = FactionUtilityMethods.Instance.GetCurrentAIP();
            Int16 minHopsFromHumanPlanet = -1;
            Int16 maxHopsFromHumanPlanet = -1;
            byte maxMarkLevel = 2;
            if ( AIP <= 150 )
            {
                minHopsFromHumanPlanet = 2;
                maxHopsFromHumanPlanet = 3;
                maxMarkLevel = 2;
            }
            else if ( AIP <= 300 )
            {
                minHopsFromHumanPlanet = 3;
                maxHopsFromHumanPlanet = 4;
                maxMarkLevel = 3;
            }
            else if ( AIP <= 600 )
            {
                minHopsFromHumanPlanet = 4;
                maxHopsFromHumanPlanet = 7;
                maxMarkLevel = 5;
            }
            else
            {
                minHopsFromHumanPlanet = 5;
                maxHopsFromHumanPlanet = 9;
                maxMarkLevel = 7;
            }
            int allowedRetries = 6; //was 100, and that breaks the game in the late-game.
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
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );

                    if ( planet.PopulationType == PlanetPopulationType.AIHomeworld ||
                         planet.PopulationType == PlanetPopulationType.AIBastionWorld )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because it was an AI homeworld at some point.", Verbosity.DoNotShow );
                        continue;
                    }

                    bool adjacentHomeworld = false;
                    foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                    {
                        if ( neighbor.PopulationType == PlanetPopulationType.AIHomeworld ||
                         neighbor.PopulationType == PlanetPopulationType.AIBastionWorld )
                        {
                            adjacentHomeworld = true;
                            break;
                        }
                    }
                    if ( adjacentHomeworld == true )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because its adjacent to n AI homeworld planet", Verbosity.DoNotShow );
                        continue;
                    }

                    //Planet must belong to an allied faction
                    if ( !planet.GetControllingFaction().GetIsFriendlyTowards( faction ) )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because the planet isnt friendly", Verbosity.DoNotShow );
                        continue;
                    }
                    //planet must not be a King planet, or adjacent to a king planet
                    if ( planet.GetDataByStanceForFaction( faction, FactionStance.Friendly ).HasKingUnitPresent )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because its a king planet", Verbosity.DoNotShow );
                        continue;
                    }
                    if ( planet.MarkLevelForAIOnly.Ordinal > maxMarkLevel )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because its mark level " + planet.MarkLevelForAIOnly.Ordinal + " > allowed mark level " + maxMarkLevel, Verbosity.DoNotShow );
                        continue;
                    }
                    bool adjacentKing = false;
                    foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                    {
                        if ( neighbor.GetDataByStanceForFaction( faction, FactionStance.Friendly ).HasKingUnitPresent )
                        {
                            adjacentKing = true;
                            break;
                        }
                    }
                    if ( adjacentKing == true )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because its adjacent to a king planet", Verbosity.DoNotShow );
                        continue;
                    }

                    if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength > FInt.Zero )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because there are enemies on it", Verbosity.DoNotShow );

                        continue;
                    }
                    if ( minHopsFromHumanPlanet > 1 )
                    {
                        //this planet must not be too close to a player planet
                        //to make the bounds listed above inclusive minHops - 1 must be used
                        bool foundPlayerPlanetWithinMinHops = false;
                        foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops_NoFilters( (Int16)(minHopsFromHumanPlanet - 1) ) )
                        {
                            Planet otherPlanet = _phd.Planet;
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
                        
                        foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops( maxHopsFromHumanPlanet,
                            delegate ( Planet source, Planet destination )
                            {
                                /*
                                if ( source.TypeData.Type == PlanetType.Nomad && !World_AIW2.Instance.CurrentGalaxy.IsNomadGalaxy )
                                {
                                    return false;
                                }
                                */

                                var wh = source.GetWormholeTo(destination.Index);
                                if (wh.TypeData.SpecialWormholeLogic != null)
                                {
                                    if (wh.TypeData.SpecialWormholeLogic.WormholeTrafficFilter(wh, destination.GetWormholeTo(source), "InstigatorPlacement_MeasureHopsToHumanPlanet") == WormholeTraffic.Dissallowed)
                                    {
                                        return false;
                                    }
                                }

                                return true;
                            },
                            (Planet.EvaluatorDelegate)null ) )
                        {
                            Planet otherPlanet = _phd.Planet;
                            if ( otherPlanet.GetControllingFactionType() == FactionType.Player )
                                foundPlayerPlanetWithinMaxHops = true;
                        }
                        if ( !foundPlayerPlanetWithinMaxHops )
                            continue;
                    }
                    WorkingPlanetList.Add( planet );
                    if ( planet.IntelLevel > PlanetIntelLevel.Unexplored )
                        WorkingPlanetListExploredOnly.Add( planet );
                }
                retries++;
            } while ( WorkingPlanetList.Count == 0 && retries < allowedRetries );
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Choosing from " + WorkingPlanetList.Count + " planets to spawn instigator base", Verbosity.DoNotShow );

            //if we have a valid planet, first check if we have a valid planet that's also explored. If so, prefer the explored planets
            //if not, use the unexplored planet.

            Planet returnResult = null;
            if ( WorkingPlanetList.Count <= 0 )
            {
                returnResult = null;
            }
            else if ( WorkingPlanetListExploredOnly.Count > 0 )
            {
                returnResult = WorkingPlanetListExploredOnly[Context.RandomToUse.Next( 0, WorkingPlanetListExploredOnly.Count )];
            }
            else
            {
                returnResult = WorkingPlanetList[Context.RandomToUse.Next( 0, WorkingPlanetList.Count )];
            }

            Planet.ReleaseTemporaryPlanetList( WorkingPlanetList );
            Planet.ReleaseTemporaryPlanetList( WorkingPlanetListExploredOnly );

            return returnResult;
        }
    }

}
