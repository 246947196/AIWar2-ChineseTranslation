using Arcen.AIW2.Core;
using Arcen.AIW2.External.BulkPathfinding;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public abstract class ExternalFactionDeepInfoRoot : ExternalFactionDeepInfo, IBulkPathfinding
    {
        protected virtual int MinimumSecondsBetweenLongRangePlannings => 1;

        //TEACHING_MOMENT: Any two factions that share this same processing group won't run their LRP threads at the same time.
        //This is handy when you have, for instance, two human empires.  They use a lot of the same static variables,
        //and in general are not threadsafe with one another.  It's even MORE handy when it comes to the Macrophage.
        //The macrophage do have a root class, but there are regular, enraged, and tamed versions.  Theys should ALL
        //have the same basic macrophage UniqueNameForFactionToAvoidThreadConflicts set.  Why?  Because several methods
        //are called from them which are not threadsafe.  We don't want regular and tamed and enraged running at the same
        //time, or we'll get threading race conditions.  Please remember that this only applies to LRP threads.
        //This does not affect Stage3 logic, or Stage2 aggregation, or anything like that.  All of that in-sim logic
        //already happens on a shared thread, so we don't have to worry about racing there, either.
        public SpecialFactionProcessingGroup FactionProcessingGroupToAvoidThreadConflicts
        {
            get
            {
                Faction fac = this.AttachedFaction;
                if ( fac == null )
                    return null;
                return fac.SpecialFactionData.ProcessingGroup;
            }
        }

        #region BulkPathfinding
        public Faction FactionForBulkPathfinding => AttachedFaction;
        public DictionaryOfDictionaryOfLists<Planet, Planet, SafeSquadWrapper> WormholeCommands { get; set; }
        public DictionaryOfDictionaryOfLists<Planet, ArcenPoint, SafeSquadWrapper> MovementCommands { get; set; }
        public List<Planet> ConflictPlanets { get; set; }
        #endregion

        protected override void Cleanup()
        {
            if ( WormholeCommands != null )
                WormholeCommands.Clear();
            if ( MovementCommands != null )
                MovementCommands.Clear();
            if ( ConflictPlanets != null )
                ConflictPlanets.Clear();
        }

        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
        }

        #region AssignDefenseValuesTo_HostOnly
        public override void AssignDefenseValuesTo_HostOnly( List<Planet> planets, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return;
            if ( planets.Count == 0 )
                return;
            if ( MapgenLogger.IsActive )
            {
                MapgenLogger.Log( "AssignDefenseValuesTo_HostOnly: faction: " + this.AttachedFaction.Type + " (" + this.AttachedFaction.FactionIndex + ")" );
                MapgenLogger.Log( "AssignDefenseValuesTo_HostOnly: SentinelsExternalIsFilled: " + (this.AttachedFaction.TryGetAISentinelsCoreData() != null) );
            }
            AISentinelsCoreData factionExternal = this.AttachedFaction.TryGetAISentinelsCoreData()?.SentinelInfo;
            AITypeData aiType = factionExternal.AIType;
            if ( aiType == null )
            {
                MapgenLogger.Log( "AI type is null" );
                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: during AssignDefenseValuesTo_HostOnly the ai type was null; set it to default. This is because the default name in xml is set to something that doesn't exist.", Verbosity.DoNotShow );
                aiType = AITypeDataTable.Instance.DefaultRow;
                factionExternal.AIType = aiType;
            }
            IAITypeImplementation implementation = aiType.Implementation;
            implementation.AssignDefenseValuesTo_HostOnly( planets, Context );
        }
        #endregion

        #region CheckIfPlayerHasSeenFaction_HostOnly
        public override void CheckIfPlayerHasSeenFaction_HostOnly( ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return;

            //The goal of this is to A. if this faction was chosen randomly, once it's visible then we can display the name in the Esc Menu to the player
            // and B. Have a means to play a "Commander, we've just spotted this faction" journal entry in a consistent fashion

            //This is complicated enough to not let factions override this.
            //Factions should instead override DoOnFirstSightingOfFactionByPlayer if they have something they want to do.
            if ( this.AttachedFaction.HasBeenSeenByPlayer )
                return;
            if ( this.AttachedFaction.SpecialFactionData.IsConsideredPlayerAlliedForFilterPurposes )
            {
                this.AttachedFaction.HasBeenSeenByPlayer = true;
                this.DoOnFirstSightingOfFactionByPlayer( false, null, Context );
                return;
            }
            if ( this.AttachedFaction.Type == FactionType.NaturalObject )
            {
                this.AttachedFaction.HasBeenSeenByPlayer = true;
                this.DoOnFirstSightingOfFactionByPlayer( false, null, Context );
                return;
            }
            if ( this.AttachedFaction.Type == FactionType.Player )
            {
                this.AttachedFaction.HasBeenSeenByPlayer = true;
                this.DoOnFirstSightingOfFactionByPlayer( false, null, Context );
                return;
            }

            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads() )
            {
                if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                {
                    this.AttachedFaction.HasBeenSeenByPlayer = true;
                    this.DoOnFirstSightingOfFactionByPlayer( false, entity, Context );
                    break;
                }
            }
        }
        #endregion

        public virtual void DoOnFirstSightingOfFactionByPlayer( bool IsFromBeacon, GameEntity_Squad SquadSeenOrNull, ArcenHostOnlySimContext Context )
        {

        }

        public override void DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly( ref int debugStage, GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, Faction factionThatKilledEntity, Faction entityOwningFaction, int numExtraStacksKilled, ArcenHostOnlySimContext Context )
        {
        }

        public override void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        {
        }

        public override void DoOnFirstDeathLogic_OnlyAferFullStackDeath_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        {
            //NOTE that this only happens after a FULL stack dies.  Individual items off a stack that die don't call this.
            //     We can change that if we need to at some point, but for now this makes the most sense.
        }

        public override void DoOnAnyCrippleLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        {
        }

        public override void DoOnInternalConstructionCompleteLogic_HostOnly( GameEntity_Squad entity, ArcenHostOnlySimContext Context )
        {
        }

        public override void DoOnSelfBuildingCompleteLogic_HostOnly( GameEntity_Squad entity, ArcenHostOnlySimContext Context )
        {
        }

        public override void DoOnSpawnsOnDeath_AfterFullDeathOrPartOfStackDeath_HostOnly( GameEntity_Squad dyingEntity, GameEntity_Squad oneOfTheSpawningEntities, ArcenHostOnlySimContext Context )
        {
        }

        public override void DoPerSimStepLogic_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
        }

        #region GetAIDefensePlacerForPlanet
        public override AIDefensePlacer GetAIDefensePlacerForPlanet( Planet planet, ArcenHostOnlySimContext Context )
        {
            try
            {
                //note from Chris: this should be something that comes back for ANY planet, or else it's going to break.  Non-AI planets also use this.
                if ( this.AttachedFaction.Type != FactionType.AI )
                {
                    if ( planet.InitialOwningAIFactionIndex != -1 ) // Has been previous owned by an ai, so use that ai's information.
                        return World_AIW2.Instance.GetFactionByIndex( planet.InitialOwningAIFactionIndex ).GetAISentinelsCoreData().SentinelInfo.AIType.Implementation.GetAIDefensePlacerForPlanet( this.AttachedFaction, planet, Context );
                    else // Seemingly never owned by an ai, so use the primary ai's information.
                        return World_AIW2.Instance.AIFactions[0].GetAISentinelsCoreData().SentinelInfo.AIType.Implementation.GetAIDefensePlacerForPlanet( this.AttachedFaction, planet, Context );
                }
                // If currently owned, nice. Just return data from it.
                return this.AttachedFaction.GetAISentinelsCoreData().SentinelInfo.AIType.Implementation.GetAIDefensePlacerForPlanet( this.AttachedFaction, planet, Context );
            }
            catch
            {
                //pick at random if the above failed
                Context.RandomToUse.ReinitializeWithSeed( planet.Index + World_AIW2.Instance.Setup.MapConfig.Seed );
                return AIDefensePlacerTable.Instance.Rows[Context.RandomToUse.Next( 0, AIDefensePlacerTable.Instance.Rows.Count )];
            }
        }
        #endregion

        #region GetFireteamLurkPlanet_OnBackgroundNonSimThread
        public override Planet GetFireteamLurkPlanet_OnBackgroundNonSimThread( Planet TargetPlanet, int TeamStrength, Planet CurrentPlanetForFireteam,
            ILongRangePlanningHostContext Context, IPerFactionPathCache PathCacheData )
        {
            if ( Context == null ) //client
                return null;
            ArcenLongTermIntermittentPlanningContext context = (ArcenLongTermIntermittentPlanningContext)Context;
            return this.GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass( TargetPlanet, TeamStrength, CurrentPlanetForFireteam, context, PathCacheData as PerFactionPathCache );
        }
        public virtual Planet GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass( Planet TargetPlanet, int TeamStrength, Planet CurrentPlanetForFireteam,
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            return null;
        }
        #endregion

        #region GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread
        public override void GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread( bool DefenseMode, Planet CurrentPlanetForFireteam, ILongRangePlanningHostContext Context, IPerFactionPathCache PathCacheData,
            List<FireteamTarget> PreferredTargets, List<FireteamTarget> FallbackTargets, object TeamObj )
        {
            if ( Context == null ) //client
                return;
            ArcenLongTermIntermittentPlanningContext context = (ArcenLongTermIntermittentPlanningContext)Context;
            this.GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass( DefenseMode, CurrentPlanetForFireteam, context, PathCacheData as PerFactionPathCache, PreferredTargets, FallbackTargets, TeamObj );
        }
        public virtual void GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass( bool DefenseMode, Planet CurrentPlanetForFireteam,
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData,
            List<FireteamTarget> PreferredTargets, List<FireteamTarget> FallbackTargets, object TeamObj )
        {

        }
        #endregion

        #region GetFireteamRetreatPoint_OnBackgroundNonSimThread
        public override GameEntity_Squad GetFireteamRetreatPoint_OnBackgroundNonSimThread( Planet CurrentPlanetForFireteam, ILongRangePlanningHostContext Context, IPerFactionPathCache PathCacheData )
        {
            if ( Context == null ) //client
                return null;
            ArcenLongTermIntermittentPlanningContext context = (ArcenLongTermIntermittentPlanningContext)Context;
            return this.GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( CurrentPlanetForFireteam, context, PathCacheData as PerFactionPathCache );
        }
        public virtual GameEntity_Squad GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            return null;
        }
        #endregion

        #region GetSpendingRatios
        public virtual void GetSpendingRatios( EnumIndexedArray<AIBudgetType, FInt> BudgetToFill, Faction faction )
        {
            BudgetToFill.Clear();
            BudgetToFill[AIBudgetType.Reinforcement] = FInt.One;
        }
        #endregion

        public virtual bool TryToSpendBudget( ArcenHostOnlySimContext Context, AIBudgetType BudgetType ) { return true; }

        #region GetHackingInternalEventFrequencyMultiplier
        public override FInt GetHackingInternalEventFrequencyMultiplier()
        {
            if ( this.AttachedFaction.Type == FactionType.AI )
            {
                AISentinelsCoreData sentinels = this.AttachedFaction.TryGetAISentinelsCoreData()?.SentinelInfo;
                if ( sentinels != null && sentinels.AIDifficulty != null )
                    return sentinels.AIDifficulty.MultiplierToGlobalHackingEventIntervals;
            }
            return FInt.One;
        }
        #endregion

        #region GetNeedsToRunLongRangePlanning
        public override bool GetNeedsToRunLongRangePlanning( float TimeFLastRun, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return false;

            int secondsElapsed = UnityEngine.Mathf.FloorToInt( ArcenTime.TimeSinceStartF - TimeFLastRun );

            bool debugging = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Thread );
            if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "\t" + "GetNeedsToRunLongRangePlanning(" + this.AttachedFaction.SpecialFactionData.InternalName + "," + secondsElapsed + ")", Verbosity.Chat );

            if ( this.MinimumSecondsBetweenLongRangePlannings > 0 && this.MinimumSecondsBetweenLongRangePlannings > secondsElapsed )
            {
                if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "\t\t" + "returning false because this.MinimumSecondsBetweenLongRangePlannings " + this.MinimumSecondsBetweenLongRangePlannings + " >  SecondsSinceLastRun " + secondsElapsed, Verbosity.Chat );
                return false;
            }
            if ( debugging ) ArcenDebugging.ArcenDebugLogSingleLine( "\t\t" + "returning true", Verbosity.Chat );
            return true;
        }
        #endregion

        #region DoLongRangePlanning_OnBackgroundNonSimThread_HostOnly
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_HostOnly( ILongRangePlanningHostContext Context )
        {
            if ( Context == null ) //client
                return;

            #region Tracing
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Independents );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ExternalFacDeepRoot-DoLongRangePlanning_OnBackgroundNonSimThread_HostOnly-trace", 10f ) : null;
            if ( tracing ) tracingBuffer.Add( this.TracingName ).Add( " DoLongRangePlanning trace begins for faction " ).Add( this.AttachedFaction.FactionIndex );
            #endregion

            ArcenLongTermIntermittentPlanningContext context = (ArcenLongTermIntermittentPlanningContext)Context;
            this.DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( context );
            #region Tracing
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " DoLongRangePlanning trace ends" );
            if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToStringAndReturnToPool(), Verbosity.Chat );
            #endregion

            System.Threading.Interlocked.Exchange( ref this.AttachedFaction.LastDoLongRangePlanningEndedAtUnpausedTimeSinceLastRestart_NonSim, ArcenTime.NonSetupNetworkReadyGameTimeSinceLastLoadOrStartF );
            //ArcenDebugging.ArcenDebugLogSingleLine( faction.GetDisplayName() + " " + this.MySpecialFactionImplementationIndexNotForSim, Verbosity.DoNotShow );
        }
        public virtual void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( AttachedFaction.Type == FactionType.SpecialFaction )
            {
                this.RebuildConflictPlanetsList();
                this.PrepareConflictPlanetMovementLogic( Context );
                this.ExecuteWormholeCommands( Context );
                this.ExecuteMovementCommands( Context );
            }
        }
        #endregion

        public override void MinorFactionAIPEquivalentIncrease( FInt AIPEquivalent )
        {
            //This is used by minor factions like the Nanocaust that want to track their own equivalent
            //of AIP
        }

        #region ReactToHacking_AsPartOfMainSim_HostOnly
        public override void ReactToHacking_AsPartOfMainSim_HostOnly( GameEntity_Squad entityBeingHacked, FInt WaveMultiplier, ArcenHostOnlySimContext Context, HackingEvent Event, Faction overrideFaction = null )
        {
            if ( Context == null ) //client
                return;

            //this happening in the base virtual method makes good sense, because it could be several factions
            bool debug = GameSettings.Current.GetBoolBySetting( "HackingDebug" );
            Faction faction = overrideFaction == null ? this.AttachedFaction : overrideFaction;
            AISentinelsFactionDeepInfo aiSentinelsDeepInfo = faction.TryGetAISentinelsDeepLogic();

            if ( debug && entityBeingHacked != null )
                ArcenDebugging.ArcenDebugLogSingleLine( "got request to react to hacking of " + entityBeingHacked.TypeData.GetDisplayName() + " with multiplier " + WaveMultiplier + " ", Verbosity.DoNotShow );
            else if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "got request to react to hacking without a target (presumably an anti-Planet hack with multiplier " + WaveMultiplier, Verbosity.DoNotShow );
            if ( aiSentinelsDeepInfo != null )
            {
                //First compute the base strength of the hacking response
                AISentinelsCoreData factionExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;
                int HackingWaveSize = factionExternal.AIDifficulty.BaseHackingWaveSize;
                int strength = (WaveMultiplier * HackingWaveSize).IntValue;

                //if there's an AIP multiplier, handle that now
                //If the AIP multiplier is .01 and the AIP is 200 then we do
                //newStrength = oldStrength + (oldStrength * AIPMultiplier*AIP)
                //a straight multiplier would allow the resulting waves to have too much variance
                FInt aipMultiplier = factionExternal.AIDifficulty.HackingAipMultiplier;
                int bonusStrength = 0;
                if ( aipMultiplier > FInt.Zero )
                {
                    FInt AIP = GlobalAIWorldBaseInfo.Instance.AIProgress_Effective;
                    bonusStrength = (aipMultiplier * AIP * strength).IntValue;
                    strength += bonusStrength;
                }
                FInt fallenSpireMultiplier = FallenSpireFactionBaseInfo.GetFallenSpireGeneralAIResponseMultiplier();
                int fallenSpireBonusStrength = (strength * fallenSpireMultiplier).IntValue;
                strength += fallenSpireBonusStrength;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "base wave size: " + HackingWaveSize + " * multiplier " + WaveMultiplier + " bonus AIP strength " + bonusStrength + " bonus fallen spire strength " + fallenSpireBonusStrength + " for total strength " + strength, Verbosity.DoNotShow );
                bool allowedGuardians = true;

                bool allowedDireGuardians = false;
                if ( strength > 40000 && faction.CurrentGeneralMarkLevel >= 2 ) //minimum requirements for Dire guardians in hacking response wave
                    allowedDireGuardians = true;
                if ( Event != null )
                    Event.ApproxResponseStrength += strength;
                PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
                aiSentinelsDeepInfo.SendWave( Context, pathingCacheData, strength, entityBeingHacked, null, -1, allowedGuardians, allowedDireGuardians );
                pathingCacheData.ReturnToPool();
            }
        }
        #endregion

        public override void SeedStartingEntities_EarlyMajorFactionClaimsOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
        }

        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
        }

        #region UpdateInvasionTime_HostOnly
        public override void UpdateInvasionTime_HostOnly( ArcenHostOnlySimContext Context )
        {
            //For factions with a set Invasion Time, this time is based on "early/mid/lategame".
            //So once the player is in the mid-game, we start accelerating those times (by making the invasion happen sooner)
            int interval = 4;
            if ( World_AIW2.Instance.GameSecond % interval != 0 )
                return; //only run this check every so often
            if ( this.AttachedFaction.InvasionTime <= 0 )
                return; //
            if ( this.AttachedFaction.InvasionTime <= World_AIW2.Instance.GameSecond - 600 )
                return; //the player probably has a notification about the invasion now
            int highestMarklevelForAI = 1;
            int mostOfficersPerPlayer = 0;
            int mostRegularFleetsPerPlayer = 0;
            int highestMarkOfOfficer = 0;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction.Type == FactionType.AI )
                {
                    if ( (int)otherFaction.CurrentGeneralMarkLevel > highestMarklevelForAI )
                        highestMarklevelForAI = (int)otherFaction.CurrentGeneralMarkLevel;
                }
                if ( otherFaction.Type == FactionType.Player )
                {
                    int regularFleetsForFaction = 0;
                    int officerFleetsForFaction = 0;
                    foreach ( Fleet fleet in World_AIW2.Instance.Fleets( otherFaction, FleetStatus.CenterpieceMustLiveOrLooseFleet ) )
                    {
                        GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                        if ( centerpiece == null )
                            continue;
                        if ( centerpiece.PlanetFaction.Faction != otherFaction )
                            continue;
                        if ( centerpiece.GetMatches_SemiSlow( EntityRollupType.MobileFleetFlagships ) ||
                             centerpiece.GetMatches_SemiSlow( EntityRollupType.MobileCombatFlagships ) )
                            regularFleetsForFaction++;
                        if ( centerpiece.TypeData.SpecialType == SpecialEntityType.MobileOfficerCombatFleetFlagship )
                        {
                            officerFleetsForFaction++;
                            if ( centerpiece.CurrentMarkLevel > (byte)highestMarkOfOfficer )
                                highestMarkOfOfficer = (int)centerpiece.CurrentMarkLevel;
                        }
                    }
                    if ( regularFleetsForFaction > mostRegularFleetsPerPlayer )
                        mostRegularFleetsPerPlayer = regularFleetsForFaction;
                    if ( officerFleetsForFaction > mostOfficersPerPlayer )
                        mostOfficersPerPlayer = officerFleetsForFaction;
                }
            }
            bool allPlanetsExplored = true;
            foreach ( Planet planet in World_AIW2.Instance.CurrentGalaxy.Planets( false ) )
            {
                if ( planet.IntelLevel == PlanetIntelLevel.Unexplored )
                {
                    allPlanetsExplored = false;
                    break;
                }
            }

            int modifier = 0;
            if ( highestMarklevelForAI - 1 > 0 ) //invasion timer counts extra for higher mark levels. If you are at mark 3 then it takes 2 extra seconds off
                modifier += (highestMarklevelForAI - 1);
            if ( mostOfficersPerPlayer >= 2 )
                modifier++;
            if ( allPlanetsExplored )
                modifier++;
            if ( highestMarkOfOfficer >= 5 ) //high mark officers
                modifier++;
            if ( mostRegularFleetsPerPlayer >= 5 )
                modifier++;
            this.AttachedFaction.InvasionTime -= modifier;
            if ( this.AttachedFaction.InvasionTime < 0 )
                this.AttachedFaction.InvasionTime = -1;
        }
        #endregion end UpdateInvasionTime_HostOnly

        public override void UpdatePlanetInfluence_HostOnly( ArcenHostOnlySimContext Context )
        {
            //Used by minor factions to say "I have influence on this planet".
            //this is called after stage2 sim. You add your faction index to each planet.UnderInfluenceOfFactionIndex list.
        }

        public override void ModdableGameCommandExecution( string ModdableCommandCode, string RelatedString, ChainList<int> RelatedIntegers, ArcenHostOnlySimContext Context )
        {
            //TEACHING_MOMENT: the ModdableCommandCode is a custom code that should be short and mod-specific
            //that said, factions themselves can also do it internally.
            //we use these in several places, not because we have to, but because it's handy to show how they work
            //just search for the name of this faction can you can see all the examples of us using it.
            //
            //TEACHING_MOMENT: While we're here, let's talk about how data gets from MP hosts to clients.
            //We are running these through DeepInfo, which means host-only.
            //Anything you change on BaseInfo for a faction will go to the client automatically within a few seconds at most.
            //Anything you change on the AIWar2Core stuff for faction (like the Faction class itself) will do the same.
            //Anything you change on a Squad will go... when relevant.  Either after it minorly desyncs (this is fine, this is normal),
            //or after the client hovers their mouse over the unit in question.  That data is not relevant to the client "until it is," generally.
            //
            //Now, THAT said, a few things:
            //1. If you have the host create a new squad or fire a shot, that gets sent to the client asap.
            //2. If a unit dies, the client will find out within about 2 seconds at most.  Same if you move a unit or warp them.
            //3. If the data you are changing would affect notifications or the intel tab on the client, and you want them to know NOW,
            //then there's a function for that.   On the squad, just call squad.FlagForForcedFullSyncToClients_FromHost().
            //The data from the host for that squad will get to all clients within the next 400ms, give or take, in that case.
            //Note that if you call that method multiple times within that timespan it won't spam the clients with extra data.

            var hostCtx = Context.GetHostOnlyContext();
            if ( hostCtx == null )
                return;

            switch ( ModdableCommandCode )
            {
                case "CMP_AutoKite":
                {
                        int _ri0 = 0, _ri1 = 0;
                        int _ri_i = 0;
                        foreach ( var _ri_v in RelatedIntegers )
                        {
                            if ( _ri_i == 0 ) _ri0 = _ri_v;
                            else if ( _ri_i == 1 ) { _ri1 = _ri_v; break; }
                            _ri_i++;
                        }
                        Faction faction = World_AIW2.Instance.GetFactionByIndex( _ri1 );
                        faction.UnitsAutoKite_NeverSetDirectlyOrItBreaksEverything = (_ri0 == 1);
                }
                break;
            }
        }

        public override bool SeedUnitsOnStartingPlanetDuringMapGen( Planet StartingPlanet, ConfigurationForFaction factionConfig, PlanetFaction pFaction,
            ref ArcenPoint commandStationPoint, ref bool stillNeedsToSeedHumanHomeworldStuff, 
            Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull, ArcenHostOnlySimContext Context )
        {
            // true because no error occured
            // false would be reported
            return true;
        }

        public override void SeedSpecialEntities_LateAfterAllFactionSeeding_CustomForPlayerType( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData MapData )
        {

        }
    }
}
