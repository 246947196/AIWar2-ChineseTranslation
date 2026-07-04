using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    /*  Overview
       The Zenith Miners work like this. Every "so often" a Zenith Miner Probe appears on a random planet
       If the Probe exists for "long enough" then it summons the Zenith Miner. Probes are small and the AI doesn't bother to see them.
       The Zenith Miner is a giant hostile-to-all golem that shoots the hell out of everything
           All factions are hostile to the Zenith Miner and will gladly shoot at it; the AI will try to kill it too. The fact that the AI would ignore the miner in AIWC was immersion-breaking
           If the Miner exists long enough then the planet is destroyed

       The Zenith Miner Probe can be hacked to change what happens
       Eligible Hacks
           Disable Probe - probe dies, no miner appears
           Move Probe - probe goes to an adjacent planet (you don't want to fight that Mark VI planet alone? How about some chaos)
           Transform Miner - Instead of eating the planet, the Miner will Change the planet
                 Available changes: all ships on planet go faster
                                    all ships on planets go slower
                                    Make the planet Nomadic

     */

    public sealed class ZenithMinersFactionDeepInfo : ExternalFactionDeepInfoRoot, IExternalDeepInfo_Singleton
    {
        public ZenithMinersFactionBaseInfo BaseInfo;
        public static ZenithMinersFactionDeepInfo Instance = null;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<ZenithMinersFactionBaseInfo>();
            Instance = this;
        }

        protected override void Cleanup()
        {
            Instance = null;
            BaseInfo = null;

            TeamsAimedAtPlanet.Clear();

            MinorFactionAllied = false;

            UnassignedShips.Clear();
            UnassignedShipsByPlanet.Clear();
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 10;
        public readonly ProtectedValDictionary<Planet, FireteamRegiment> TeamsAimedAtPlanet = ProtectedValDictionary<Planet, FireteamRegiment>.Create_WillNeverBeGCed( 100, "ZenithMinersFactionDeepInfo-TeamsAimedAtPlanet" );


        public bool MinorFactionAllied = false;

        public readonly int MinFireteamStrength = 2000;
        public readonly int MaxFireteamStrength = 4000;

        public override void SeedStartingEntities_EarlyMajorFactionClaimsOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            //nothing for the miners
        }


        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host. I think a lot of what the miners do is just not client 

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithMiners );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZMin-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f ) : null;

            UpdateProbeSpawningTime( AttachedFaction, Context );
            HandleProbeSpawningIfNecessary( AttachedFaction, Context );
            SpawnMinersIfNecessary( AttachedFaction, Context );
            ModifyMinersIfNecessary( AttachedFaction, Context );
            UpdateMinerRavageOdds( AttachedFaction, Context );
            DoMinerEffects( AttachedFaction, Context ); //eat the planet, etc

            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion

        }
        public const int NumRavagesToPrecalculate = 10;
        public void UpdateMinerRavageOdds( Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( BaseInfo.NextMinerRavage.Count > 0 )
                return; //we already have our previous odds set
            for ( int i = 0; i < NumRavagesToPrecalculate; i++ )
            {
                if ( Context.RandomToUse.Next( 0, 100 ) < 50 )
                    BaseInfo.NextMinerRavage.Add( false );
                else
                    BaseInfo.NextMinerRavage.Add( true );
            }
        }

        public const int StrengthForTransformation = 2000;
        public const int DistanceForTransformation = 300;
        public const int MinerMaxWaitTime = 600;
        public void ModifyMinersIfNecessary( Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            //This is a pretty "miscellaneous" function.
            //It transforms miners from Mobile form to Stationary Form,
            //and modifies probes against minor factions to have the minor faction modifiers,
            List<SafeSquadWrapper> miners = this.BaseInfo.Miners.GetDisplayList();
            for ( int i = 0; i < miners.Count; i++ )
            {
                //transform miners to stationary mode
                GameEntity_Squad miner = miners[i].GetSquad();
                if ( miner == null )
                    continue;
                if ( miner.TypeData.GetHasTag( "ZenithMinerMobile" ) &&
                     (miner.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength < StrengthForTransformation ||
                      miner.GetSecondsSinceEnteringThisPlanet() > MinerMaxWaitTime) &&
                    Mat.DistanceBetweenPointsImprecise( miner.WorldLocation, Engine_AIW2.Instance.CombatCenter ) < DistanceForTransformation )
                {
                    string tag = "ZenithMinerStationaryTierZero"; //default
                    if ( BaseInfo.NumProbesSpawned > 3 )
                        tag = "ZenithMinerStationaryTierOne";
                    if ( BaseInfo.NumProbesSpawned > 6 )
                        tag = "ZenithMinerStationaryTierTwo";
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, tag );
                    if ( entityData == null )
                        throw new Exception( "No ZenithMiner tag " + tag + " defined in XML" );

                    GameEntity_Squad newMiner = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( miner.PlanetFaction, entityData, entityData.MarkFor( miner.PlanetFaction ),
                         miner.GetFactionLooseFleetOrNull_Safe(), 0, miner.WorldLocation, Context, "ZenithMiner_ModifyMiner" );  //is fine, main sim thread
                    ZenithMinersPerUnitBaseInfo newdata = newMiner.CreateExternalBaseInfo<ZenithMinersPerUnitBaseInfo>( "ZenithMinersPerUnitBaseInfo" );
                    ZenithMinersPerUnitBaseInfo olddata = miner.TryGetExternalBaseInfoAs<ZenithMinersPerUnitBaseInfo>();
                    miner.ShieldPointsLost = miner.GetMaxShieldPoints(); //shields go down for transformation
                    if ( miner.HullPointsLost > 0 )
                        newMiner.TakeDamageDirectly( miner.HullPointsLost, null, null, DamageSource.SelfDamageFromMyOwnWeapons, Context );
                    olddata.CopyTo( newdata );
                    newdata.InMiningMode = true;
                    newMiner.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                    newMiner.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                    newMiner.FlagAsNeedingFullSyncCheckIfInMultiplayerAndWeAreHost(); //let the client know extra fast, although probably this will be caught in fast-blast
                    miner.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
                    newMiner.FlagAsNeedingFullSyncCheckIfInMultiplayerAndWeAreHost(); //let the client know extra fast
                }
            }
            List<SafeSquadWrapper> probes = this.BaseInfo.Probes.GetDisplayList();
            for ( int i = 0; i < probes.Count; i++ )
            {
                //if we have a probe on a Dyson planet and we were going to destroy the planet,
                //instead swap over to destroying the dyson
                GameEntity_Squad entity = probes[i].GetSquad();
                if ( entity == null )
                    continue;
                ZenithMinersPerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<ZenithMinersPerUnitBaseInfo>();
                if ( data.Effect != ZenithMinerEffect.DestroyPlanet )
                    continue; //only affects DestroyPlanet effects

                if ( entity.Planet.GetFirstMatching( FactionType.SpecialFaction, SphereFactionBaseInfo.Tag_ZenithSphere, true, true ) != null )
                    data.Effect = ZenithMinerEffect.DestroyDysonSphere;

                if ( entity.Planet.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "ZenithArchitrave" &&
                     ZenithArchitraveFactionBaseInfo.IsPlanetInAnyZATerritory( entity.Planet ) )
                {
                    data.Effect = ZenithMinerEffect.DiminishZenithArchitrave;
                }
            }
        }
        public void UpdateProbeSpawningTime( Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( BaseInfo.TimeToSpawnNextProbe == -1 && BaseInfo.NumProbesSpawned == 0 )
            {
                //Initialize the first probe hit time
                if ( faction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) )
                    BaseInfo.TimeToSpawnNextProbe = 20;
                else
                    BaseInfo.TimeToSpawnNextProbe = BaseInfo.Difficulty.TimeForFirstProbe + Context.RandomToUse.Next( 0, BaseInfo.Difficulty.TimeForFirstProbe / 10 );
                return;
            }
            if ( BaseInfo.Probes.Count == 0 && BaseInfo.Miners.Count == 0 &&
                 BaseInfo.TimeToSpawnNextProbe == -1 )
            {
                //if we have no probes or miners on the map and we're ready, set the next time to spawn a probe
                BaseInfo.TimeToSpawnNextProbe = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.ProbeInterval + Context.RandomToUse.Next( 60, 360 );
            }
        }

        public void HandleProbeSpawningIfNecessary( Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( BaseInfo == null )
                ArcenDebugging.ArcenDebugLogSingleLine( "???", Verbosity.DoNotShow );
            //If it's time, spawn a new probe
            if ( BaseInfo.TimeToSpawnNextProbe == -1 )
                return; //we aren't even thinking about spawning a new probe right now
            if ( World_AIW2.Instance.GameSecond <= BaseInfo.TimeToSpawnNextProbe )
                return; //not time yet
            int probesToSpawn = 1;
            int random = Context.RandomToUse.Next( 0, 100 );
            if ( random < BaseInfo.Difficulty.PercentChanceTwoPlanets )
            {
                probesToSpawn++;
            }
            else if ( random < BaseInfo.Difficulty.PercentChanceThreePlanets + BaseInfo.Difficulty.PercentChanceTwoPlanets )
            {
                probesToSpawn += 2;
            }
            else if ( random < BaseInfo.Difficulty.PercentChanceFourPlanets + BaseInfo.Difficulty.PercentChanceThreePlanets + BaseInfo.Difficulty.PercentChanceTwoPlanets )
            {
                probesToSpawn += 3;
            }

            if ( BaseInfo.NumProbesSpawned == 0 && probesToSpawn > 2 )
                probesToSpawn = 2; //spawn a max of 2 probes the first time, to make sure not to overwhelm a new player

            if ( BaseInfo.NumProbesSpawned > BaseInfo.NumSuccessfulMinings + 7 &&
                 Context.RandomToUse.Next( 0, 100 ) > 50 ) //if we've had a lot of failures, sometimes spawn more miners
                probesToSpawn++;
            SpawnProbe( probesToSpawn, faction, Context );
            //if we've just spawned a new set of probes, wipe this list
            //so we can recalculate it. We precalculate this stuff to prevent
            //the players savescumming
            BaseInfo.NextMinerRavage.Clear();

        }

        private readonly List<Planet> possibleProbePlanets = List<Planet>.Create_WillNeverBeGCed( 10, "ZenithMinersFactionDeepInfo-possibleProbePlanets" );

        public void SpawnProbe( int probesToSpawn, Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //don't even try this on clients

            //a helper function for HandleProbeSpawningIfNecessary
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ZenithMinerProbe" );
            if ( entityData == null )
                throw new Exception( "No ZenithMinerProbe defined in XML" );
            GetPotentialProbePlanets( possibleProbePlanets, faction, Context );
            if ( possibleProbePlanets.Count == 0 )
                return; //no eligible planets
            ArcenArrays.Randomize( possibleProbePlanets, Context.RandomToUse, 3 );
            for ( int i = 0; i < probesToSpawn; i++ )
            {
                //Spawn the probes
                if ( possibleProbePlanets.Count <= i ) //we don't have any more valid places to put a probe
                    break;
                BaseInfo.TimeToSpawnNextProbe = -1;
                Planet planet = possibleProbePlanets[i]; //the list was previously randomly sorted
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );

                AngleDegrees angle = AngleDegrees.Create( (float)Context.RandomToUse.Next( 1, 360 ) );
                ArcenPoint center = Engine_AIW2.Instance.CombatCenter;
                float warpInMultiplier = 0.9f;
                ArcenPoint spawnLocation = center.GetPointAtAngleAndDistance( angle, (int)(planet.GravWellSize.DistanceScale_GravwellRadius * warpInMultiplier) );

                GameEntity_Squad probe = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                       pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "ZenithMiner_NewProbe" );  //is fine, main sim thread
                ZenithMinersPerUnitBaseInfo data = probe.CreateExternalBaseInfo<ZenithMinersPerUnitBaseInfo>( "ZenithMinersPerUnitBaseInfo" );

                //alternate between ravaging a planet and outright destroying it as the default, for some variety

                if ( i >= BaseInfo.NextMinerRavage.Count )
                    data.Effect = ZenithMinerEffect.RavagePlanet; //always ravage if we've spawned a ton; we shouldn't ever spawn this many though
                else
                {
                    if ( BaseInfo.NextMinerRavage[i] == true )
                        data.Effect = ZenithMinerEffect.RavagePlanet;
                    else
                        data.Effect = ZenithMinerEffect.DestroyPlanet;
                }

                data.RemainingDuration = BaseInfo.Difficulty.ProbeDuration + Context.RandomToUse.Next( BaseInfo.Difficulty.ProbeDuration / 100, BaseInfo.Difficulty.ProbeDuration / 7 );
                if ( faction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) )
                    data.RemainingDuration /= 15; //extra fast when in debug mode

                BaseInfo.NumProbesSpawned++;
                if ( BaseInfo.NumProbesSpawned == 1 && ArcenNetworkAuthority.GetIsHostMode() )
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_ZenithMiners_InitialProbe", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                if ( ArcenNetworkAuthority.GetIsHostMode() )
                {
                    if ( probe.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                        World_AIW2.Instance.QueueChatMessageOrCommand( "A " + faction.StartFactionColourForLog() + "Zenith Miner</color> probe has appeared somewhere in the galaxy. In " +
                            data.RemainingDuration + " seconds a Miner will appear on the planet", ChatType.LogToCentralChat, null );
                    else
                    {
                        SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                        if ( chatHandlerOrNull != null )
                            chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( probe );

                        World_AIW2.Instance.QueueChatMessageOrCommand( "A " + faction.StartFactionColourForLog() + "Zenith Miner</color> probe has appeared on " +
                            probe.GetPlanetName_Safe() + ". In " + data.RemainingDuration + " seconds a Miner will appear on the planet", ChatType.LogToCentralChat, chatHandlerOrNull );
                    }
                }
            }
        }
        public void GetPotentialProbePlanets( List<Planet> ListToFill, Faction faction, ArcenHostOnlySimContext Context )
        {
            //helper function for spawning the Probes
            ListToFill.Clear();
            int minHops = 2;
            int maxHops = 5;
            //if the player/AI does a good job of killing the miners, expand the range of places
            int modifier = BaseInfo.NumProbesSpawned - BaseInfo.NumSuccessfulMinings * 4;
            bool canEatPlayerPlanets = false;
            if ( BaseInfo.NumSuccessfulMinings < 6 || BaseInfo.NumProbesSpawned < 10 )
                canEatPlayerPlanets = true;
            else if ( modifier > 0 )
            {
                minHops += modifier;
                maxHops += modifier;
            }

            if ( faction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) )
            {
                minHops = 2;
                maxHops = 3;
            }
            int retries = 10;
            do
            {
                ListToFill.Clear();
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
                    if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                        continue; //no unexplored planets

                    if ( planet.PopulationType == PlanetPopulationType.AIHomeworld ||
                         planet.PopulationType == PlanetPopulationType.HumanHomeworld ||
                         planet.PopulationType == PlanetPopulationType.AIBastionWorld )
                        continue;

                    if ( planet.TypeData.Type == PlanetType.Nomad &&
                         BaseInfo.NumSuccessfulMinings < 4 ) //the miners won't go after nomads for "a while"
                        continue;

                    if ( planet.GetControllingFactionType() == FactionType.Player &&
                         !canEatPlayerPlanets )
                    {
                        //Miners can go after player planets after they've eaten a number of planets (so later into the game)
                        continue;
                    }

                    if ( BaseInfo.BlockedPlanets.Contains( planet ) )
                        continue;
                    if ( planet.IsRavaged )
                        continue; //no return visits
                    if ( !canEatPlayerPlanets )
                    {
                        Int16 hops = FactionUtilityMethods.Instance.GetHopsToPlayerPlanet( planet, Context );
                        if ( hops < minHops )
                            continue;
                        if ( hops > maxHops )
                            continue;
                    }
                    int copiesOfPlanetToAdd = 1;
                    //we are particularly likely to go after dyson spheres
                    if ( planet.GetFirstMatching( FactionType.SpecialFaction, SphereFactionBaseInfo.Tag_ZenithSphere, true, true ) != null )
                        copiesOfPlanetToAdd = 5;
                    for ( int i = 0; i < copiesOfPlanetToAdd; i++ )
                        ListToFill.Add( planet );
                }
                minHops--;
                maxHops++;
            } while ( ListToFill.Count == 0 && retries-- > 0 );
        }
        public void SpawnMinersIfNecessary( Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //don't even try this on clients

            //if there are any probes ready to summon a miner, do so
            List<SafeSquadWrapper> probes = this.BaseInfo.Probes.GetDisplayList();
            for ( int i = 0; i < probes.Count; i++ )
            {
                GameEntity_Squad probe = probes[i].GetSquad();
                if ( probe == null )
                    continue;
                ZenithMinersPerUnitBaseInfo probedata = probe.TryGetExternalBaseInfoAs<ZenithMinersPerUnitBaseInfo>();
                probedata.RemainingDuration--;

                PlanetFaction pFaction = probe.PlanetFaction;
                if ( probedata.RemainingDuration <= 0 )
                {
                    //spawn a miner, delete the probe. As the game goes on, spawn scarier versions of the Miner
                    string tag = "ZenithMinerMobileTierZero"; //default
                    if ( BaseInfo.NumProbesSpawned > 3 )
                        tag = "ZenithMinerMobileTierOne";
                    if ( BaseInfo.NumProbesSpawned > 6 )
                        tag = "ZenithMinerMobileTierTwo";
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, tag );

                    if ( entityData == null )
                        throw new Exception( "No ZenithMiner tag " + tag + " defined in XML" );
                    GameEntity_Squad miner = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                            pFaction.Faction.LooseFleet, 0, probe.WorldLocation, Context, "ZenithMiner_NewMiner" );  //is fine, main sim thread
                    ZenithMinersPerUnitBaseInfo minerdata = miner.CreateExternalBaseInfo<ZenithMinersPerUnitBaseInfo>( "ZenithMinersPerUnitBaseInfo" );
                    minerdata.Effect = probedata.Effect;
                    minerdata.RemainingDuration = BaseInfo.Difficulty.MinerDuration + Context.RandomToUse.Next( BaseInfo.Difficulty.MinerDuration / 100, BaseInfo.Difficulty.MinerDuration / 7 );
                    if ( faction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) )
                        minerdata.RemainingDuration /= 10; //extra fast when in debug mode
                    miner.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                    miner.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                    probe.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
                    BaseInfo.NumMinersSpawned++;
                    if ( ArcenNetworkAuthority.GetIsHostMode() )
                    {
                        if ( minerdata.Effect == ZenithMinerEffect.DestroyDysonSphere && miner.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
                            World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_ZenithMiners_AttackingDyson", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                        if ( BaseInfo.NumMinersSpawned == 1 )
                            World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_ZenithMiners_InitialMiner", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                        else
                        {
                            //don't play two messages
                            if ( miner.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                                World_AIW2.Instance.QueueChatMessageOrCommand( "A " + faction.StartFactionColourForLog() + "Zenith Miner</color> Golem has appeared somewhere in the galaxy. In " +
                                    minerdata.RemainingDuration + " seconds the Miner eat the planet", ChatType.LogToCentralChat, null );
                            else
                            {
                                SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                                if ( chatHandlerOrNull != null )
                                    chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( probe );

                                World_AIW2.Instance.QueueChatMessageOrCommand( "A " + faction.StartFactionColourForLog() + "Zenith Miner</color> Golem has appeared on " + probe.GetPlanetName_Safe() + ". In " +
                                    minerdata.RemainingDuration + " seconds the Miner will eat the planet", ChatType.LogToCentralChat, chatHandlerOrNull );
                            }
                        }

                    }

                }
            }
        }
        public void DoMinerEffects( Faction faction, ArcenHostOnlySimContext Context )
        {
            //This is where we handle the effects of the Miners;
            List<SafeSquadWrapper> miners = this.BaseInfo.Miners.GetDisplayList();
            for ( int i = 0; i < miners.Count; i++ )
            {
                GameEntity_Squad miner = miners[i].GetSquad();
                if ( miner == null )
                    continue;
                
                ZenithMinersPerUnitBaseInfo data = miner.TryGetExternalBaseInfoAs<ZenithMinersPerUnitBaseInfo>();
                
                if ( data.InMiningMode )
                {
                    data.RemainingDuration--;
                    if ( data.RemainingDuration < 30 ) //only when the timer gets to the last 30 seconds
                    {
                        Planet plan = miner.Planet;
                        if ( plan != null )
                        {
                            //show that the planet is suffering
                            plan.ShowPlanetAsSufferingNearCollapseUntilGameSecond = World_AIW2.Instance.GameSecond + 3;
                            plan.Network_HostOnly_NeedToSyncWormholesToClients = true;
                            plan.Network_HostOnly_NeedToSyncPlanetPositionToClients = true;
                        }
                    }
                }
                
                if ( data.RemainingDuration <= 0 )
                {
                    //Do whatever the ZenithMinerEffect says; delete the planet, change it, etc...
                    if ( data.Effect == ZenithMinerEffect.DestroyPlanet ||
                         data.Effect == ZenithMinerEffect.DiminishZenithArchitrave )
                    {
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = miner.Planet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( "A " + faction.StartFactionColourForLog() + "Zenith Miner</color> has eaten " + miner.GetPlanetName_Safe() + ".",
                                ChatType.LogToCentralChat, chatHandlerOrNull );
                        }
                        
                        if ( !miner.Planet.HasPlanetBeenDestroyed )
                        {
                            miner.Planet.IsPlanetToBeDestroyed = true;
                            miner.Planet.Network_HostOnly_NeedToSyncWormholesToClients = true;
                            miner.Planet.Network_HostOnly_NeedToSyncPlanetPositionToClients = true;
                        }
                    }
                    else 
                    if ( data.Effect == ZenithMinerEffect.SlowShipsOnPlanet )
                    {
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = miner.Planet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( "A " + faction.StartFactionColourForLog() + "Zenith Miner</color> has permanently slowed all ships on  " +
                                miner.GetPlanetName_Safe() + ".", ChatType.LogToCentralChat, chatHandlerOrNull );
                        }

                        miner.Planet.UnitSlowPercentage = 10;
                    }
                    else 
                    if ( data.Effect == ZenithMinerEffect.RavagePlanet )
                    {
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = miner.Planet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( "A " + faction.StartFactionColourForLog() + "Zenith Miner</color> has ravaged  " + miner.GetPlanetName_Safe() + ", destroying most of the available resources.",
                                ChatType.LogToCentralChat, chatHandlerOrNull );
                        }
                        foreach ( GameEntity_Squad generator in miner.Planet.Squads( "MetalGenerator" ) )
                        {
                            generator.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                        }
                        miner.Planet.NonSim_ShouldSetRavagedIfHostAndNoMetalHarvesters = true;
                        miner.Planet.IsRavaged = true;
                        miner.Planet.Network_HostOnly_NeedToSyncWormholesToClients = true;
                        miner.Planet.Network_HostOnly_NeedToSyncPlanetPositionToClients = true;
                    }

                    else 
                    if ( data.Effect == ZenithMinerEffect.SpeedupShipsOnPlanet )
                    {
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = miner.Planet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( "A " + faction.StartFactionColourForLog() + "Zenith Miner</color> has permanently sped up all ships on  " +
                                miner.GetPlanetName_Safe() + ".", ChatType.LogToCentralChat, chatHandlerOrNull );
                        }

                        miner.Planet.UnitSpeedupPercentage = 10;
                    }
                    else 
                    if ( data.Effect == ZenithMinerEffect.MakePlanetNomadic )
                    {
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = miner.Planet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( "A " + faction.StartFactionColourForLog() + "Zenith Miner</color> has unmoored " +
                                miner.GetPlanetName_Safe() + " from the galactic warp network, making it Nomadic.", ChatType.LogToCentralChat, chatHandlerOrNull );
                        }

                        miner.Planet.MakePlanetNomadic();
                    }
                    else 
                    if ( data.Effect == ZenithMinerEffect.DestroyDysonSphere )
                    {
                        //CHRIS TODO: change planet model (either here, or maybe by setting a flag on the planet?)
                        GameEntity_Squad sphere = miner.Planet.GetFirstMatching( FactionType.SpecialFaction, SphereFactionBaseInfo.Tag_ZenithSphere, true, true );
                        if ( sphere == null )
                            continue;
                        if ( sphere.Planet == miner.Planet )
                        {
                            BaseInfo.BlockedPlanets.Add( sphere.Planet );
                            sphere.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                            if ( ArcenNetworkAuthority.GetIsHostMode() )
                                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_ZenithMiners_DysonDestroyed", string.Empty, faction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );

                        }
                    }
                    else
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "BUG: zenith miner effect " + data.Effect + " has not been implemented\n", Verbosity.DoNotShow );
                    }
                    
                    //Having finished whatever it was it was doing, the Miner now vanishes
                    miner.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
                    BaseInfo.NumSuccessfulMinings++;
                }
            }
        }


        //Long Range Planning (LRP) starts here. It currently doesn't do anything, but leaving it here just in case we want it someday
        //Note that the class itself says "Never calls LRP"

        public static readonly List<SafeSquadWrapper> UnassignedShips = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ZenithMinersFactionDeepInfo-UnassignedShips" );
        public static readonly DictionaryOfLists<Planet, SafeSquadWrapper> UnassignedShipsByPlanet = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 60, "ZenithMinersFactionDeepInfo-UnassignedShipsByPlanet" );
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            //NOTE: The Zenith Miners right now don't actually use fireteams or LRP at all
            //code left just in case we need it someday
            /*
              My thought is "Miner flies around in mobile form and kills evertthing on the planet. It then flies to the middle of the planet (visually) and transforms into Stationary Form. If anything would be underneath the transformed miner, it gets "pushed out of the way" so it's still there on the planet and you can interact with it if necessary
              This will need to modify the Notification a bit to not starting the countdown until the Miner enters Stationary Form
             */
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "ZenithMinerMobile" ) )
            {
                var factionData = entity.Planet.GetStanceDataForFaction( AttachedFaction );
                if ( Context.RandomToUse.Next( 0, 100 ) < 50 )
                    FactionUtilityMethods.Instance.TachyonBlastPlanet( entity.Planet, AttachedFaction, Context, false ); //make sure we decloak anything
                if ( factionData[FactionStance.Hostile].TotalStrength > StrengthForTransformation &&
                     entity.GetSecondsSinceEnteringThisPlanet() < MinerMaxWaitTime )
                    continue;
                if ( entity.HasQueuedOrders() )
                    continue;
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCPatrol], GameCommandSource.AnythingElse );
                command.ToBeQueued = false;
                command.RelatedPoints.Add( Engine_AIW2.Instance.CombatCenter );
                command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                bool playAudioEffectForCommand = false;
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, playAudioEffectForCommand );
            }
        }

        public override GameEntity_Squad GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            //int currentDanger = -1;
            GameEntity_Squad retreatPoint = null;

            return retreatPoint;
        }

        public override void GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass( bool DefenseMode, Planet CurrentPlanetForFireteam,
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, List<FireteamTarget> PreferredTargets, List<FireteamTarget> FallbackTargets, object TeamObj )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZMin-GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            FInt falloffForDistance = FInt.FromParts( 0, 050 );
            Fireteam team = (Fireteam)TeamObj;
            GetPreferredZenithMinerTargets( PreferredTargets, AttachedFaction, Context );
            //We do a two-stage check here. First we cull the targets whose defenses are much stronger than usual.
            //then we sort targets by how hard it is to get there
            //Currently we don't do fallback targets
            if ( FallbackTargets != null || FallbackTargets.Count == 0 )
            {
                FallbackTargets.Sort( static delegate ( FireteamTarget Left, FireteamTarget Right )
                {
                    int lDifficulty = Left.dangerOfPath;
                    int rDifficulty = Right.dangerOfPath;
                    return lDifficulty.CompareTo( rDifficulty );
                } );
            }


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
        public void GetPreferredZenithMinerTargets( List<FireteamTarget> ListToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            ListToFill.Clear();
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
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZMin-GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
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
                int totalDifficultyOfPathToLurkPlanet = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForTeam, planet, true, out hops );
                if ( totalDifficultyOfPathToLurkPlanet >= TeamStrength * 5 ) //as long as they only outnumber us 5:1, let's go!
                    continue;

                int totalDifficultyOfPathToTarget = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, planet, TargetPlanet, true, out hops );
                if ( preferUnwatchedPlanets && planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    totalDifficultyOfPathToTarget *= 2; //penalized watched planets if desired

                if ( totalDifficultyOfPathToTarget < 0 )
                    totalDifficultyOfPathToTarget = 0; //pathing through allied planets is basically the same

                if ( tracing )
                    tracingBuffer.Add( "\tConsidering " + planet.Name + " danger of path " + totalDifficultyOfPathToTarget + " distance " + Distance + " intel " + planet.IntelLevel ).Add( "\n" );

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
                    hopsFromBestPlanet = hops;
                    bestPlanet = planet;
                }

                if ( hopsFromBestPlanet <= 3 && dangerOfPathFromBestPlanet <= TeamStrength / 10 )
                    break; //we found a good lurk within easy striking distance of the planet, so exit now
            }
            return bestPlanet;
        }

        public override void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        {
            if ( entity == null )
                return;
            int debugStage = 0;
            try
            {
                debugStage = 100;
                if ( !entity.TypeData.GetHasTag( "ZenithMinerMobile" ) && !entity.TypeData.GetHasTag( "ZenithMinerStationary" ) )
                    return;

                debugStage = 200;
                ZenithMinersPerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<ZenithMinersPerUnitBaseInfo>();
                debugStage = 300;
                if ( data == null )
                    return;

                debugStage = 400;
                if ( data.WasEffectDone )
                    return; //the miner succeeded in what it was doing and has left successfully

                debugStage = 500;
                //This is a Miner that was killed. Spawn a metal generator
                PlanetFaction neutralPFaction = entity.Planet.GetFirstFactionOfType( FactionType.NaturalObject );
                debugStage = 600;
                GameEntityTypeData metalGenerator = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ZMMetalGenerator" );
                if ( metalGenerator == null )
                    throw new Exception( "No ZMMetalGenerator XML defined\n" );
                debugStage = 700;
                ArcenPoint spawnLocation = entity.Planet.GetSafePlacementPoint_AroundEntity( Context, metalGenerator, entity, FInt.FromParts( 0, 005 ), FInt.FromParts( 0, 015 ) );

                debugStage = 800;
                GameEntity_Squad.CreateNew_ReturnNullIfMPClient( neutralPFaction, metalGenerator, 1,
                                            neutralPFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "ZenithMiner_MinerDeath" );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in SpecialFaction_ZenithMiners.DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly stage " + debugStage + "\n" + e, Verbosity.ShowAsError );
            }
        }
    }
}
