using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

/*
  Overall organization: there's a BaseInfo class (for scourge global stuff),
  a ScourgePerUnitBaseInfo (does what it says),
  a ScourgeDifficulty (which is used for balance stuff),
  the Scourge class.

  LongRangePlanning relies heavily on Fireteams, which is done in Fireteam.cs.
   */

namespace Arcen.AIW2.External
{
    public sealed class ScourgeVassalFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        //Set immediately before the sorts so the comparisons can be non-capturing static delegates.
        //[ThreadStatic] because LRP / fireteam planning runs on background threads.
        [ThreadStatic] private static Faction cb_svInfraFaction;
        [ThreadStatic] private static ScourgeVassalFactionDeepInfo cb_svThis;
        [ThreadStatic] private static ArcenLongTermIntermittentPlanningContext cb_svFtContext;
        [ThreadStatic] private static FInt cb_svFtMultiplier;
        public ScourgeVassalFactionBaseInfo BaseInfo;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<ScourgeVassalFactionBaseInfo>();
        }

        protected override void Cleanup()
        {
            BaseInfo = null;

            //probably does not matter
            WorkingWeakPlanets.Clear();
            PlanetsBeingAttackedByAllies_ScourgeOnly.Clear();
            WorkingPlanetsLRP.Clear();
            LongRangePlanningArmories.Clear();
            LongRangePlanningSpawners.Clear();
            LongRangePlanningFortresses.Clear();
            LongRangePlanningSeeds.Clear();
            LongRangePlanningWarpingInSpawners.Clear();
            LongRangePlanningWarpingInArmories.Clear();
            LongRangePlanningWarpingInFortresses.Clear();

            LongRangePlanningInfrastructurePlanets.Clear();
            LongRangePlanningInfrastructureUndefendedPlanets.Clear();

            UnassignedWarriors.Clear();
            TeamsThatNeedTargets.Clear();

            WorkingPlanetList.Clear();
            FortressOptions.Clear();

            AvailableFireteams.Clear();
            TeamsAimedAtPlanet.Clear();
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 2;

        // (used for deciding whether to attack)
        public readonly ProtectedValDictionary<Planet, FireteamRegiment> TeamsAimedAtPlanet = ProtectedValDictionary<Planet, FireteamRegiment>.Create_WillNeverBeGCed( 100, "ScourgeFactionDeepInfo-TeamsAimedAtPlanet" );

        public override void UpdatePlanetInfluence_HostOnly( ArcenHostOnlySimContext Context )
        {
            //reset the faction Influences for this one
            List<Planet> planetsInfluenced = Planet.GetTemporaryPlanetList( "Scourge-UpdatePlanetInfluence_HostOnly-planetsInfluenced", 10f );
            if ( planetsInfluenced == null ) //blocked for teardown/shutdown; bail
                return;

            List<SafeSquadWrapper> spawnersInGalaxy = this.BaseInfo.Spawners.GetDisplayList();
            for ( int i = 0; i < spawnersInGalaxy.Count; i++ )
                planetsInfluenced.AddIfNotAlreadyIn( spawnersInGalaxy[i].Planet );
            List<SafeSquadWrapper> armoriesInGalaxy = this.BaseInfo.ArmoriesInGalaxy.GetDisplayList();
            for ( int i = 0; i < armoriesInGalaxy.Count; i++ )
                planetsInfluenced.AddIfNotAlreadyIn( armoriesInGalaxy[i].Planet );

            AttachedFaction.SetInfluenceForPlanetsToList( planetsInfluenced );
            Planet.ReleaseTemporaryPlanetList( planetsInfluenced );
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            //this is the meat of the Sim-side code. In general, we iterate over a lot of stuff and update it if appropriate.
            if ( BaseInfo.ScourgeIsSuppressed.Display ) //this faction must be awakened by a beacon
            {
                return;
            }
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Scourge-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f ) : null;

            if ( BaseInfo.CountdownTimerForNemesis >= 0 )
                BaseInfo.CountdownTimerForNemesis--;

            if ( BaseInfo.Spawners.Count == 0 && BaseInfo.WarpingInSpawners.Count == 0 )
            {
                JoinAlliesIfNecessary ( Context );
                BaseInfo.TimeForNextScourgeInvasionCheck = -1; //we have invaded successfully
            }

            //iterate over all the units for upgrades and so on
            int overflowExperience = 0;
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "ScourgeSpawner" ) )
            {
                HandleSpawning_OnMainSimOnly( AttachedFaction, Context, entity, ref overflowExperience );
            }

            foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
            {
                updateExperienceAndMetal( entity, ref overflowExperience, AttachedFaction, Context ); //everybody
            }
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "ScourgeNeophyte" ) )
            {
                UpgradeNeophytesIfNecessary_OnMainSimOnly( AttachedFaction, Context, entity );
            }
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "ScourgeSummoner" ) )
            {
                UpdateSummonerIfNecessary_OnMainSimOnly( AttachedFaction, Context, entity );
            }

            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "DefendsRegion" ) )
            {
                UpdateDefendsRegionIfNecessary_OnMainSimOnly( AttachedFaction, Context, entity );
            }

            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "ScourgeDefender" ) )
            {
                UpdateScourgeDefender_OnMainSimOnly( AttachedFaction, Context, entity );
            }

            HandleSeeds_OnMainSimOnly( AttachedFaction, Context );
            HandleFlowers_OnMainSimOnly( AttachedFaction, Context );

            foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "ScourgeWarrior", "ScourgeSubjugator" ) )
            {
                HandleArmories_OnMainSimOnly( AttachedFaction, Context, entity ); //this upgrades units
            }

            HandleSubjugatorSpawning_OnMainSimOnly( AttachedFaction, Context );
            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
        }

        public void JoinAlliesIfNecessary( ArcenHostOnlySimContext Context )
        {
            if ( World_AIW2.Instance.GameSecond % 120 == 0 )
            {
                    GameEntityTypeData spawnerData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "ScourgeVassalSpawner");

                    foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                    {
                        if ( entity.PlanetFaction.Faction != this.BaseInfo.EmpireFaction )
                            continue; //only join our Empire
                        if (entity.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength > 10 * 000)
                            continue; //don't seed if we're under attack; this could mean that our spawners were all killed by the AI
                        //Seed a spawner
                        GameEntity_Squad squad = entity.Planet.Mapgen_SeedEntity(Context, AttachedFaction, spawnerData, PlanetSeedingZone.MostAnywhere);
                        ArcenDebugging.LogSingleLine("Got a " + squad.ToStringWithPlanetAndOwner(), Verbosity.DoNotShow );
                        ScourgePerUnitBaseInfo data = squad.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>("ScourgePerUnitBaseInfo");
                        data.ExperienceForNextLevel = (FInt)200; //level up pretty quick at first
                        data.FullyInitialized = true;

                        GameEntityTypeData armoryData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "ScourgeVassalArmory");
                        GameEntity_Squad armory = entity.Planet.Mapgen_SeedEntity(Context, AttachedFaction, armoryData, PlanetSeedingZone.MostAnywhere);
                        data = armory.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>("ScourgePerUnitBaseInfo");
                        data.ExperienceForNextLevel = (FInt)400; //we don't have XML at this point, so just hardcode something
                        string requestedArmory = BaseInfo.FirstArmory;
                        if (String.IsNullOrEmpty(requestedArmory)) //this can be empty for some old quickstarts
                            requestedArmory = "Random";
                        ScourgeTypeData typedata = null;

                        typedata = ScourgeTypeDataTable.Instance.GetRowByArmoryName(requestedArmory, Context);
                        if (typedata == null)
                            throw new Exception("Got null row from ScourgeTypeDataTable. Requested <" + requestedArmory + ">");
                        data.ScourgeTypeId = typedata.id;
                        data.FullyInitialized = true;
                    }
                }
        }

        public void UpdateScourgeInvasionForce(Faction faction, ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Scourge-UpdateScourgeInvasionForce-trace", 10f ) : null;

            int invasionIncomePerSecond = 2;
            BaseInfo.ScourgeInvasionBudget += invasionIncomePerSecond;
            int minToSpend = 1000;
            if ( World_AIW2.Instance.GameSecond % 25 == 0 && BaseInfo.ScourgeInvasionBudget > minToSpend )
            {
                int retries = 10;
                if ( tracing )
                    tracingBuffer.Add("Trying to spend ").Add(BaseInfo.ScourgeInvasionBudget ).Add(" on the invasion\n");
                GameEntityTypeData entityData = null;
                do{
                     entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "AllowedToInvade" );
                     if ( entityData.CostForAIToPurchase > BaseInfo.ScourgeInvasionBudget )
                     {
                         entityData = null; //retry
                         continue;
                     }
                     BaseInfo.ScourgeInvasionForceStrength += entityData.CostForAIToPurchase;
                     BaseInfo.ScourgeInvasionBudget -= entityData.CostForAIToPurchase;
                     BaseInfo.ScourgeInvasionForce[entityData] += 1;
                     if ( tracing )
                         tracingBuffer.Add("\tSpending ").Add(entityData.CostForAIToPurchase).Add(" of ").Add( BaseInfo.ScourgeInvasionBudget).Add(" for a new " ).Add( entityData.GetDisplayName() ).Add("\n");

                } while ( BaseInfo.ScourgeInvasionBudget > 0  && retries-- > 0 );
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
                ArcenDebugging.ArcenDebugLogSingleLine( "Scourge found best friendly planet " + bestPlanet.Name, Verbosity.DoNotShow );
            return bestPlanet;
        }
        private static readonly List<Planet> WorkingWeakPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "ScourgeFactionDeepInfo-WorkingWeakPlanets" );
        public Planet FindWeakPlanetToInvade( Faction faction, ArcenHostOnlySimContext Context )
        {
            //finds whether there are any planets of ours that we could join
            WorkingWeakPlanets.Clear();
            if ( BaseInfo.ScourgeInvasionForceStrength < 1000 )
                return null; //we are too weak
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
                int enemyStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                int myAndAlliedStrength = pFaction.DataByStance[FactionStance.Self].TotalStrength +
                    pFaction.DataByStance[FactionStance.Friendly].TotalStrength;
                int scaledDifference = ((enemyStrength - myAndAlliedStrength) * FInt.FromParts(1, 250)).IntValue;
                if ( scaledDifference > BaseInfo.ScourgeInvasionForceStrength )
                    continue;
                WorkingWeakPlanets.Add(planet);
            }
            if ( WorkingWeakPlanets.Count == 0 )
                return null;
            Planet bestPlanet = WorkingWeakPlanets[Context.RandomToUse.Next(0, WorkingWeakPlanets.Count)];
            ArcenDebugging.ArcenDebugLogSingleLine( "Scourge found best weak planet " + bestPlanet.Name + " from " + WorkingWeakPlanets.Count + " options", Verbosity.DoNotShow );
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

                        World_AIW2.Instance.QueueChatMessageOrCommand( faction.StartFactionColourForLog() + "The Scourge</color> have joined the " + 
                            influencingFaction.StartFactionColourForLog() + influencingFaction.GetDisplayName() + "</color> invasion on " + planet.Name, ChatType.LogToCentralChat, chatHandlerOrNull );
                        hasAllies = true;
                    }
                    else
                        World_AIW2.Instance.QueueChatMessageOrCommand( faction.StartFactionColourForLog() + "The Scourge</color> is building infrastructure on " + planet.Name, 
                            ChatType.LogToCentralChat, chatHandlerOrNull );
                }
            }
            GameEntity_Squad createdUnit;
            CreateSpawner( faction, planet, Context, out createdUnit );
            CreateArmory( faction, planet, Context, out createdUnit );
            CreateFortress( faction, planet, Context, out createdUnit );
            if ( !hasAllies )
                CreateFortress( faction, planet, Context, out createdUnit ); //a bonus fortress if we have no friends
        }
        public void InvadeWeakPlanet( Planet planet, Faction faction, ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Scourge-InvadeWeakPlanet-trace", 10f ) : null;
            if ( tracing )
                tracingBuffer.Add("Scourge is invading planet " + planet.Name );
            if ( !ArcenNetworkAuthority.GetIsHostMode() )
                return; //invasion only done on 
            int minRadius = (planet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 050 )).IntValue;
            int maxRadius = (planet.GravWellSize.DistanceScale_GravwellRadius * FInt.FromParts( 0, 150 )).IntValue;

            float warpInMultiplier = 0.9f;
            AngleDegrees angle = AngleDegrees.Create( (float)Context.RandomToUse.Next( 1, 360 ) );
            ArcenPoint invasionPoint = Engine_AIW2.Instance.CombatCenter.GetPointAtAngleAndDistance( angle, (int)(planet.GravWellSize.DistanceScale_GravwellRadius * warpInMultiplier) );

            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
            foreach ( KeyValuePair<GameEntityTypeData, int> pair in BaseInfo.ScourgeInvasionForce )
            {
                if ( tracing )
                    tracingBuffer.Add("\t").Add("Spawning ").Add(pair.Value).Add(" ").Add(pair.Key.GetDisplayName() );
                for ( int i = 0; i < pair.Value; i+= 2 )
                {
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, pair.Key, 0,
                         pFaction.FleetUsedAtPlanet, 0, invasionPoint, Context, "Scourge-InvadeWeakPlanet" );
                    if ( i < pair.Value - 1 )
                        newEntity.AddOrSetExtraStackedSquadsInThis( 2, true ); //always dump things out in stacks of 2 if we can
                    newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;//this throws off the calculations
                    newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                }
            }
            BaseInfo.TimeForNextScourgeInvasionCheck = World_AIW2.Instance.GameSecond + 600;
            BaseInfo.ScourgeInvasionForceStrength = 0;
            BaseInfo.ScourgeInvasionForce.Clear();
        }

    
    
        private void ParanoicallyCheckFireteamUnits( Faction faction, GameEntity_Squad entity, ScourgePerUnitBaseInfo data, ArcenLongTermIntermittentPlanningContext Context, string path )
        {
            //For debug purposes only. This is essentially replaced by PurgeDeadUnits()
            bool tracing = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            if ( !tracing )
                return;

            foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
            {
                for ( int j = 0; j < team.DeepInfo.ShipsInFireteam.Count; j++ )
                {
                    GameEntity_Squad otherEntity = team.DeepInfo.ShipsInFireteam[j].GetSquad();
                    if ( otherEntity == null )
                        continue;
                    if ( otherEntity.PrimaryKeyID == entity.PrimaryKeyID &&
                         team.FireTeamID != data.FireteamId )
                    {
                        ScourgePerUnitBaseInfo origData = entity.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                        throw new Exception( "Paranoically checking " + entity.ToStringWithPlanet() + " fireteam id " + data.FireteamId + " is found on team " + team.FireTeamID + " " + path + ". On disk fireteam id " + data.FireteamId + " gamesecond " + World_AIW2.Instance.GameSecond + "\n" );
                        //if ( !origData.FullyInitialized ) maybe needs to come back after the other exception is gone?
                        //    throw new Exception("Entity " + entity.ToStringWithPlanet() + " is not fully initialized, which doesn't make sense");
                    }
                }
            }
        }

        //this is only used from this one method, which is used only on this one thread
        //prefer close ones that you can get to safely
        private readonly ArcenLessLinkedList<Fireteam> AvailableFireteams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "ScourgeFactionDeepInfo-AvailableFireteams" ); 
        private void AssignUnitToFireteam_ScourgeOnly( Faction faction, GameEntity_Squad entity, ScourgePerUnitBaseInfo data, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( entity == null )
                return;

            //The general rules for assigning a unit to a fireteam:
            //Don't assign to a unit to a fireteam that's in combat or ready to be in combat
            //Don't assign a unit to a fireteam that's dangerous to get to (for eaxmple, if a spawner is cut off from the rest of the scourge)
            //Cloaked units are placed in their own firegroups
            //Once we have our list of "reasonable" firegroups, we prioritize any firegroups that are below a certain strength level
            //if all our fireteams have enough units, either pick an existing one at random or make a new firegroup
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Scourge-AssignUnitToFireteam_ScourgeOnly-trace", 10f ) : null;
            AvailableFireteams.Clear();
            if ( BaseInfo.Teams.GetItemCount() == 0 )
            {
                Fireteam team = Fireteam.CreateNewWithIDFromList( BaseInfo.Teams );
                team.IsAllowedToStack = false;
                team.StrengthToBringOnline = faction.MinFireteamStrength + Context.RandomToUse.Next( 0, faction.MaxFireteamStrength - faction.MinFireteamStrength );
                team.MyStrengthMultiplierForStrengthCalculation = FInt.FromParts( 1, 00 );
                team.EnemyStrengthMultiplierForStrengthCalculation = FInt.FromParts( 1, 500 );
                BaseInfo.Teams.AddIfNotAlreadyIn( team );
            }
            //find the "preferred" teams
            int numDefensiveFleets = 0;
            int numEscortingFleets = 0;
            int maxHopsForFireteam = 999;
            if ( this.BaseInfo.PlayerAllied || this.BaseInfo.AIAllied )
                maxHopsForFireteam = 6;
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
            {
                if ( team.DefenseMode )
                    numDefensiveFleets++;
                if ( team.status == FireteamStatus.Escorting )
                    numEscortingFleets++;
                if ( AvailableFireteams.GetItemCount() > 4 )
                    continue; //we already have enough fireteams, but keep counting so we can track the defensive and escorting fleets

                if ( team.status == FireteamStatus.Disbanded ||
                     team.status == FireteamStatus.ReadyToAttack ||
                     team.status == FireteamStatus.Attacking )
                    continue; //if a fireteam is ready to fight, don't send more ships to that team
                if ( team.status == FireteamStatus.Escorting && //if a fireteam is already "pretty strong", don't give it more
                     team.DeepInfo.TeamStrength > faction.MaxFireteamStrength )
                    continue;

                if ( !BaseInfo.PlayerAllied &&
                     team.DeepInfo.TeamStrength > faction.MaxFireteamStrength * 2 && team.DeepInfo.ShipsInFireteam.Count > 20 )
                    continue;//if this is much stronger than usual, don't make it even stronger (non-player case)

                if ( BaseInfo.PlayerAllied && (team.DefenseMode || team.status == FireteamStatus.Escorting ) &&
                     (team.DeepInfo.TeamStrength > faction.MaxFireteamStrength * 4 || team.DeepInfo.ShipsInFireteam.Count > 100 ) )
                    continue; //for player allied scourge, defensive or escorting fleets should not get too large.
                if ( BaseInfo.PlayerAllied &&
                     (team.DeepInfo.TeamStrength > faction.MaxFireteamStrength * 6 || team.DeepInfo.ShipsInFireteam.Count > 200 ) )
                    continue; //for player allied scourge, offensive fleets should not get too large (but can be pretty darn big)

                if ( team.DeepInfo.CurrentPlanet == null )
                {
                    //this is a brand new fireteam, so it's totally safe.
                    AvailableFireteams.AddIfNotAlreadyIn( team );
                    continue;
                }
                if ( team.DeepInfo.CurrentPlanet.GetHopsTo( entity.Planet ) >= maxHopsForFireteam )
                    continue;
                if ( entity.GetMaxCloakingPoints() > 0 )
                {
                    if ( !team.CloakedOnly )
                        continue;
                }
                if ( entity.TypeData.GetHasTag("NeverInDefensiveFireteam") && (team.DefenseMode || team.status == FireteamStatus.Escorting ) )
                    continue; //powerful ships don't go in defensive fleets
                if ( team.UpgradedOnly && !(data.IsHybrid || data.IsEvolved || data.IsSubjugator || data.IsNemesis) )
                    continue;

                Int16 hops = 0;
                int dangerOfTeam = Fireteam.GetDangerOfPath( faction, Context, PathCacheData, entity.Planet, team.DeepInfo.CurrentPlanet, true, out hops );
                if ( dangerOfTeam < 40000 ) //let units wander through pretty dangerous spots (40 strength)
                    AvailableFireteams.AddIfNotAlreadyIn( team );
            }

            //Base algorithm: if any of our "safe" fireteams are "below strength" then just add to one of those fireteams.
            //If all our fireteams are "Strong Enough" then randomly choose to reinforce an existing one or create a new one
            //We require "safe teams" for the case where it's an octopus map with a Spawner cut off from the rest of the galaxy

            bool stopProcesssing = false;
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( AvailableFireteams ) )
            {
                if ( team.DeepInfo.TeamStrength < team.StrengthToBringOnline )
                {
                     if ( tracing )
                         tracingBuffer.Add( "Assigning " + entity.ToStringWithMarkLevel() + " to Fireteam " + team.FireTeamID + " path A." );
                    team.DeepInfo.AddUnit( entity );
                    team.DeepInfo.IdentifyCurrentPlanet(); //just in case this unit is the first unit or something
                    data.FireteamId = team.FireTeamID;
                    entity.FireteamId = team.FireTeamID;
                    entity.MinorFactionStackingID = data.FireteamId;
                    stopProcesssing = true;
                    break;
                }
            }
            if ( stopProcesssing )
                return;

            //If we didn't assign the ship yet, either randomly assign to an existing fireteam or create a new fireteam
            //Prefer to reinforce existing teams
            int percentNewTeam = 40;
            int percentUpgradedOnlyFleet = 0;
            //change the percentages under some circumstances
            if ( this.BaseInfo.PlayerAllied )
                percentUpgradedOnlyFleet = 35;
            if ( AvailableFireteams.GetItemCount() == 0 )
                percentNewTeam = 100;
            if ( AvailableFireteams.GetItemCount() > 3 )
                percentNewTeam = 0;
            if ( (Context.RandomToUse.Next( 0, 100 ) < percentNewTeam) )
            {
                //Figure out what percentages we want to use for things
                int percentDefensiveFleet = 40;
                int infraToCount = LongRangePlanningInfrastructurePlanets.Count;
                if (!this.BaseInfo.AIAllied ) //non-ai allied scourge gets 2 infrastructure on starting planet
                    infraToCount--;

                int activeVassalMissions = FactionUtilityMethods.Instance.GetActiveVassalMissionCount( faction, VassalMissionType.Combat );
                int activeVassalDefenseMissions = FactionUtilityMethods.Instance.GetActiveVassalMissionCount( faction, VassalMissionType.Defense );
                if ( faction.IsVassal && activeVassalMissions > 0 )
                {
                    //if we have no assigned orders as a faction then we allocate our fireteams as "normal"
                    //If we have any orders then we only allocate as much defense as the player requests
                    if ( activeVassalDefenseMissions > numDefensiveFleets )
                    {
                        //if we are a vassal and we've requested more defense, do it
                        percentDefensiveFleet = 100;
                    }
                    else
                        percentDefensiveFleet = 0;
                    ArcenDebugging.ArcenDebugLogSingleLine("active combat missions for vassal " + faction.GetDisplayName() + ": " + activeVassalMissions + " total and " + activeVassalDefenseMissions + " defense. Resulting %defense: " + percentDefensiveFleet, Verbosity.DoNotShow );
                }
                else if ( numDefensiveFleets >= infraToCount || //we have all our spots defended
                     BaseInfo.Teams.GetItemCount() < 3 ) //always start with some offensive capability
                     percentDefensiveFleet = 0;
                else if ( numDefensiveFleets >= infraToCount  / 3 )
                    percentDefensiveFleet = 10; //now we send most of our forces to offense
                else
                {
                    //We need to make sure we have some forces available for offense first,
                    //but once we have a striking force lets do more for defense
                    if ( BaseInfo.Teams.GetItemCount() > 5 )
                        percentDefensiveFleet = 70;
                }

                if ( !data.IsHybrid && !data.IsEvolved )
                    percentUpgradedOnlyFleet = 0;

                if ( entity.TypeData.GetHasTag("NeverInDefensiveFireteam" ) )
                    percentDefensiveFleet = 0;

                Fireteam team = Fireteam.CreateNewWithIDFromList( BaseInfo.Teams );
                team.IsAllowedToStack = false;
                team.StrengthToBringOnline = faction.MinFireteamStrength + Context.RandomToUse.Next( 0, faction.MaxFireteamStrength - faction.MinFireteamStrength );
                team.MyStrengthMultiplierForStrengthCalculation = FInt.FromParts( 1, 00 );
                team.EnemyStrengthMultiplierForStrengthCalculation = FInt.FromParts( 1, 500 );
                if ( this.AttachedFaction.SpecialFactionData.FireteamPercentBestTarget > 0 )
                    team.PercentBestTarget = this.AttachedFaction.SpecialFactionData.FireteamPercentBestTarget;
                else if ( Context.RandomToUse.Next( 0, 100 ) < percentDefensiveFleet )
                    team.DefenseMode = true;
                else
                    team.PercentBestTarget = 60;

                if ( tracing )
                    tracingBuffer.Add( "Assigning " + entity.ToStringWithMarkLevel() + " to Fireteam " + team.FireTeamID + " path B (new team). " + team.DeepInfo.ShipsInFireteam.Count + " ships after add. Previous fireteam " + data.FireteamId + ". Percent defensive fleet " + percentDefensiveFleet + ", current number of defensive fleets " + numDefensiveFleets + " infra count " + LongRangePlanningInfrastructurePlanets.Count + " is entity cloaked " + entity.GetMaxCloakingPoints() );
                if ( entity.GetMaxCloakingPoints() > 0 )
                {
                    team.CloakedOnly = true;
                }
                if ( !team.DefenseMode && team.status != FireteamStatus.Escorting && (data.IsHybrid || data.IsEvolved || data.IsSubjugator || data.IsNemesis) &&
                    Context.RandomToUse.Next( 0, 100 ) < percentUpgradedOnlyFleet )
                    team.UpgradedOnly = true;
                team.DeepInfo.AddUnit( entity );
                data.FireteamId = team.FireTeamID;
                entity.FireteamId = team.FireTeamID;
                entity.MinorFactionStackingID = team.FireTeamID;
                team.DeepInfo.IdentifyCurrentPlanet(); //in case this unit is the first unit
                team.DeepInfo.TeamStrength += entity.GetStrengthOfSelfAndContents();
                BaseInfo.Teams.AddIfNotAlreadyIn( team );
                return;
            }

            {
                Fireteam team = AvailableFireteams.GetRandom( Context.RandomToUse );
                if ( tracing )
                    tracingBuffer.Add( "Assigning " + entity.ToStringWithMarkLevel() + " to Fireteam " + team.FireTeamID + " path C (random existing team). Defense? " + team.DefenseMode + " cloak? " + team.CloakedOnly + ". " + team.DeepInfo.ShipsInFireteam.Count + " ships after add. Previous fireteam " + data.FireteamId );

                team.DeepInfo.AddUnit( entity );
                team.DeepInfo.TeamStrength += entity.GetStrengthOfSelfAndContents();
                data.FireteamId = team.FireTeamID;
                entity.FireteamId = team.FireTeamID;
                team.DeepInfo.IdentifyCurrentPlanet(); //just in case this unit is the first unit or something
                entity.MinorFactionStackingID = team.FireTeamID;
            }
            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
        }

        private void HandleArmories_OnMainSimOnly( Faction faction, ArcenHostOnlySimContext Context, GameEntity_Squad entity )
        {
            if ( entity == null )
                return;
            //bool debug = true; //eventually this needs to be tracing
            //If we are near an Armory and have enough Experience, upgrade
            ScourgePerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
            if ( data == null || data.Experience < data.ExperienceForNextLevel || entity.CurrentMarkLevel >= 7 )
                return;
            GameEntity_Squad armory = null;
            List<SafeSquadWrapper> armoriesInGalaxy = this.BaseInfo.ArmoriesInGalaxy.GetDisplayList();
            for ( int i = 0; i < armoriesInGalaxy.Count; i++ )
            {
                if ( entity.Planet == armoriesInGalaxy[i].Planet )
                {
                    if ( Mat.DistanceBetweenPointsImprecise( entity.WorldLocation, armoriesInGalaxy[i].WorldLocation ) < BaseInfo.RangeForUpgrade )
                    {
                        armory = armoriesInGalaxy[i].GetSquad();
                        if ( armory != null )
                            break;
                    }
                    // else
                    //     ArcenDebugging.ArcenDebugLogSingleLine(entity.ToStringWithPlanet() + " is too far away to upgrade; " + Mat.DistanceBetweenPointsImprecise(entity.WorldLocation, ArmoriesInGalaxy[i].WorldLocation) + " range for upgrade " + BaseInfo.RangeForUpgrade , Verbosity.DoNotShow );
                }
            }
            if ( armory != null )
                upgradeThisWarriorOrBuilder_OnMainSimOnly( entity, armory, Context );
        }
    
        private void upgradeThisWarriorOrBuilder_OnMainSimOnly( GameEntity_Squad WarriorOrBuilder, GameEntity_Squad armory, ArcenHostOnlySimContext Context )
        {
            if ( armory == null || WarriorOrBuilder == null )
                return;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Scourge-upgradeThisWarriorOrBuilder_OnMainSimOnly-trace", 10f ) : null;
            int debugCode = 0;
            int currentMarkLevel = WarriorOrBuilder.CurrentMarkLevel;
            bool smoothUpgrade = true;

            ScourgePerUnitBaseInfo data = WarriorOrBuilder.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
            try
            {
                if ( data.Experience < data.ExperienceForNextLevel )
                {
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    return;
                }
                if ( data == null )
                    throw new Exception( "Null scourge per unit data" );
                if ( armory == null )
                    throw new Exception( "Null armory" );
                ScourgePerUnitBaseInfo armorydata = armory.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                ScourgeTypeData scourgeTypeData = ScourgeTypeDataTable.Instance.GetRowById( armorydata.ScourgeTypeId );
                if ( scourgeTypeData == null )
                {
                    //there's a window where if we try to upgrade a unit from an armory before the armory has figured out which typedata to use, we can null here.
                    //That's fine, the armory will be updated later in this sim step
                    #region Tracing
                    if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                    return;
                }
                debugCode = 100;
                if ( data.IsSubjugator && BaseInfo.NemesesInGalaxy.Count == 0 &&
                     WarriorOrBuilder.CurrentMarkLevel >= BaseInfo.Difficulty.RequiredLevelForSubjugatorToBecomeNemesis )
                {
                    //transform this unit into a Nemesis
                    bool nemesisSpawned = HandleNemesisSpawning_OnMainSimOnly( WarriorOrBuilder.GetFactionOrNull_Safe(), WarriorOrBuilder, Context );
                    if ( nemesisSpawned )
                    {
                        #region Tracing
                        if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                        if ( tracing )
                        {
                            tracingBuffer.ReturnToPool();
                            tracingBuffer = null;
                        }
                        #endregion
                        return;
                    }
                    //if we didn't make a nemesis (maybe it's too soon since the last one?) then use the default path
                }
                if ( WarriorOrBuilder.TypeData.GetHasTag( "ScourgeWarriorBase" ) &&
                     WarriorOrBuilder.CurrentMarkLevel >= BaseInfo.Difficulty.RequiredLevelForBaseWarriorsToEvolve &&
                     armory.CurrentMarkLevel >= BaseInfo.Difficulty.RequiredLevelForArmoriesToEvolveWarriors )
                {
                    debugCode = 200;
                    //This Base warrior is evolving into a higher level unit (but dropping back to level 1)
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, scourgeTypeData.TagForWarrior );
                    PlanetFaction pFaction = WarriorOrBuilder.Planet.GetPlanetFactionForFaction( WarriorOrBuilder.PlanetFaction.Faction );
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                          pFaction.FleetUsedAtPlanet, 0, WarriorOrBuilder.WorldLocation, Context, "Scourge-UpgradeWarrior" ); //is fine, main sim thread
                    if ( newEntity != null )
                    {
                        ScourgePerUnitBaseInfo newdata = newEntity.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                        debugCode = 210;
                        newdata.ExperienceForNextLevel = BaseInfo.Difficulty.BaseExperienceForLevelupWarriors;
                        newdata.TierLevel = 0;
                        newdata.IsEvolved = true;
                        newdata.FullyInitialized = true;
                        newdata.IsOffToUpgrade = false;
                        newEntity.FireteamId = WarriorOrBuilder.FireteamId;
                        newEntity.MinorFactionStackingID = WarriorOrBuilder.MinorFactionStackingID;
                        newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                        if ( smoothUpgrade )
                            newEntity.SetCurrentMarkLevel( WarriorOrBuilder.CurrentMarkLevel );
                        if ( tracing )
                            tracingBuffer.Add( WarriorOrBuilder.ToStringWithPlanet() + " fireteam " + data.FireteamId + " evolving into " + newEntity.ToStringWithPlanet() );
                        debugCode = 220;
                    }
                    WarriorOrBuilder.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
                    #region Tracing
                    if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                    return;
                }
                debugCode = 270;
                if ( scourgeTypeData.DoesThisArmoryUpgradeThisUnit( WarriorOrBuilder ) &&
                     WarriorOrBuilder.CurrentMarkLevel >= BaseInfo.Difficulty.RequiredLevelForWarriorsToHybridize &&
                     armory.CurrentMarkLevel >= BaseInfo.Difficulty.RequiredLevelForArmoriesToHybridizeWarriors &&
                     data.IsEvolved && CanBuildThisHybrid( armorydata ))
                {
                    debugCode = 300;
                    GameEntityTypeData entityData = scourgeTypeData.EntityUnitUpgradesInto( WarriorOrBuilder, Context );
                    PlanetFaction pFaction = WarriorOrBuilder.Planet.GetPlanetFactionForFaction( WarriorOrBuilder.PlanetFaction.Faction );
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                           pFaction.FleetUsedAtPlanet, 0, WarriorOrBuilder.WorldLocation, Context, "Scourge-UpgradeWarrior" ); //is fine, main sim thread
                    if ( newEntity != null )
                    {
                        ScourgePerUnitBaseInfo newdata = newEntity.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                        debugCode = 310;
                        newdata.ExperienceForNextLevel = BaseInfo.Difficulty.BaseExperienceForLevelupWarriors;
                        newdata.TierLevel = 0;
                        newdata.IsEvolved = true;
                        newdata.IsHybrid = true;
                        newdata.IsOffToUpgrade = false;
                        newdata.FullyInitialized = true;
                        newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                        newEntity.FireteamId = WarriorOrBuilder.FireteamId;
                        newEntity.MinorFactionStackingID = WarriorOrBuilder.MinorFactionStackingID;
                        if ( smoothUpgrade )
                            newEntity.SetCurrentMarkLevel( WarriorOrBuilder.CurrentMarkLevel );

                        debugCode = 320;
                        if ( tracing )
                            tracingBuffer.Add( WarriorOrBuilder.ToStringWithPlanet() + " fireteam " + data.FireteamId + "  hybridizing into " + newEntity.ToStringWithPlanet() + "\n" );
                    }
                    debugCode = 340;
                    WarriorOrBuilder.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
                    #region Tracing
                    if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                    return;
                }
                debugCode = 370;
                if ( WarriorOrBuilder.CurrentMarkLevel >= armory.CurrentMarkLevel ||  //armory is too low of level to upgrade further
                     WarriorOrBuilder.CurrentMarkLevel >= 7 ) //this unit is at the max level
                {
                    #region Tracing
                    if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                    return;
                }
                bool veryVerboseDebug = false;
                if ( tracing && veryVerboseDebug )
                    tracingBuffer.Add( WarriorOrBuilder.ToStringWithPlanet() + " fireteam " + data.FireteamId + "  upgrading to mark " + (currentMarkLevel + 1) + "\n" );
                debugCode = 400;
                byte newMarkLevel = (byte)(currentMarkLevel + 1);
                data.TierLevel++;
                if ( armory.CurrentMarkLevel >= 4 && WarriorOrBuilder.TypeData.GetHasTag( "ScourgeWarrior" ) && newMarkLevel < 7 &&
                     !data.IsHybrid && Context.RandomToUse.Next( 0, 100 ) < 70 &&
                     !smoothUpgrade )
                {
                    //hybrids don't get to fast-upgrade, but other units have a chance to upgrade extra fast
                    newMarkLevel++;
                    data.TierLevel++;
                }
                //This is a generic upgrade
                WarriorOrBuilder.SetCurrentMarkLevel( newMarkLevel );
                data.IsOffToUpgrade = false;
                data.Experience = FInt.Zero;
                debugCode = 410;
                if ( WarriorOrBuilder.TypeData.GetHasTag( "ScourgeBuilder" ) )
                    data.ExperienceForNextLevel = BaseInfo.Difficulty.BaseExperienceForLevelupBuilders + WarriorOrBuilder.CurrentMarkLevel * BaseInfo.Difficulty.ExperienceForLevelupBuildersPerMark;
                else
                    data.ExperienceForNextLevel = BaseInfo.Difficulty.BaseExperienceForLevelupWarriors + WarriorOrBuilder.CurrentMarkLevel * BaseInfo.Difficulty.ExperienceForLevelupWarriorsPerMark;
                debugCode = 420;
                if ( tracing )
                    tracingBuffer.Add( WarriorOrBuilder.ToStringWithPlanet() + " fireteam " + data.FireteamId + " upgraded to mark  " + WarriorOrBuilder.CurrentMarkLevel);
            }
            catch ( ArcenPleaseStopThisThreadException ) //this is fine
            {
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "upgradeThisWarriorOrBuilder: exception. debugCode " + debugCode + " upgrading " + WarriorOrBuilder.ToString() + " tier " + data.TierLevel + " " + e.ToString(), Verbosity.DoNotShow );
            }
            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
        }

        public void UpgradeArmory(  GameEntity_Squad armory, ArcenHostOnlySimContext Context )
        {
            //Call this when the player upgrades their ally's structures
            if ( armory == null )
                return;
            ScourgePerUnitBaseInfo armoryData = armory.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
            int currentMarkLevel = armory.CurrentMarkLevel;
            if ( armoryData.Experience < armoryData.ExperienceForNextLevel )
                return;
            armory.SetCurrentMarkLevel( (byte)(currentMarkLevel + 1) );
            armoryData.TierLevel++;
            armoryData.Experience = FInt.Zero;
            armoryData.ExperienceForNextLevel = BaseInfo.Difficulty.BaseExperienceForLevelupArmories + armory.CurrentMarkLevel * BaseInfo.Difficulty.ExperienceForLevelupArmoriesPerMark;

        }

        public void UpgradeSpawner( GameEntity_Squad spawner, ArcenHostOnlySimContext Context )
        {
            if ( spawner == null )
                return;
            ScourgePerUnitBaseInfo spawnerData = spawner.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
            int currentMarkLevel = spawner.CurrentMarkLevel;
            if ( spawnerData.Experience < spawnerData.ExperienceForNextLevel )
                return;
            spawner.SetCurrentMarkLevel( (byte)(currentMarkLevel + 1) );
            spawnerData.TierLevel++;
            spawnerData.Experience = FInt.Zero;
            spawnerData.ExperienceForNextLevel = BaseInfo.Difficulty.BaseExperienceForLevelupSpawners + spawner.CurrentMarkLevel * BaseInfo.Difficulty.ExperienceForLevelupSpawnersPerMark;
        }

        public void UpgradeFortress( GameEntity_Squad fortress, ArcenHostOnlySimContext Context )
        {
            if ( fortress == null )
                return;
            ScourgePerUnitBaseInfo fortressData = fortress.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
            int currentMarkLevel = fortress.CurrentMarkLevel;
            if ( fortressData.Experience < fortressData.ExperienceForNextLevel || fortress.CurrentMarkLevel >= BaseInfo.MaxFortressLevel )
                return;
            fortress.SetCurrentMarkLevel( (byte)(currentMarkLevel + 1) );
            fortressData.TierLevel++;
            fortressData.Experience = FInt.Zero;
            fortressData.ExperienceForNextLevel = BaseInfo.Difficulty.BaseExperienceForLevelupFortress + fortress.CurrentMarkLevel * BaseInfo.Difficulty.ExperienceForLevelupFortressPerMark;
        }
        public void UpgradeBestiary( GameEntity_Squad bestiary, ArcenHostOnlySimContext Context )
        {
            if ( bestiary == null )
                return;
            ScourgePerUnitBaseInfo bestiaryData = bestiary.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
            int currentMarkLevel = bestiary.CurrentMarkLevel;
            if ( bestiaryData.Experience < bestiaryData.ExperienceForNextLevel || bestiary.CurrentMarkLevel >= 7 )
                return;
            bestiary.SetCurrentMarkLevel( (byte)(currentMarkLevel + 1) );
            bestiaryData.TierLevel++;
            bestiaryData.Experience = FInt.Zero;
            //just treat these like armories
            bestiaryData.ExperienceForNextLevel = BaseInfo.Difficulty.BaseExperienceForLevelupArmories + bestiary.CurrentMarkLevel * BaseInfo.Difficulty.ExperienceForLevelupArmoriesPerMark;
        }

        public bool CanBuildThisHybrid( ScourgePerUnitBaseInfo data )
        {
            ScourgeTypeData typeData = ScourgeTypeDataTable.Instance.GetRowById( data.ScourgeTypeId );
            if ( typeData.name == "Burlust" )
            {
                return BaseInfo.EmpireBaseInfo.BurlustHybridUnlocked();
            }
            if ( typeData.name == "Evuck" )
            {
                return BaseInfo.EmpireBaseInfo.EvuckHybridUnlocked();
            }
            if ( typeData.name == "Thoraxian" )
            {
                return BaseInfo.EmpireBaseInfo.ThoraxianHybridUnlocked();
            }
            if ( typeData.name == "Peltian" )
            {
                return BaseInfo.EmpireBaseInfo.PeltianHybridUnlocked();
            }
            if ( typeData.name == "Neinzul" )
            {
                return BaseInfo.EmpireBaseInfo.NeinzulHybridUnlocked();
            }
            if ( typeData.name == "Spire" )
            {
                return BaseInfo.EmpireBaseInfo.SpireHybridUnlocked();
            }
            if ( typeData.name == "Zenith" )
            {
                return BaseInfo.EmpireBaseInfo.ZenithHybridUnlocked();
            }

            return false;
        }

        private void updateExperienceAndMetal( GameEntity_Squad entity, ref int overflowExperience, Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( entity == null )
                return;
            ScourgePerUnitBaseInfo data = entity.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
            if ( entity.TypeData.GetHasTag( "ScourgeBeacon" ) )
                return;
            if ( data.ExperienceForNextLevel == 0 )
            {
                //When a structure is created in the Mapgen_SeedEntity code path, it doesn't have the 'experience for next level' set,
                //since we haven't parsed the XML at that stage. So set that now. Necessary for the game start 
                if ( entity.TypeData.GetHasTag( "ScourgeSpawner" ) )
                    data.ExperienceForNextLevel = BaseInfo.Difficulty.BaseExperienceForLevelupSpawners;
                else
                    data.ExperienceForNextLevel = BaseInfo.Difficulty.BaseExperienceForLevelupArmories;
                data.FullyInitialized = true;
            }
            //some sanity checking
            //if ( !data.FullyInitialized )
            //    return;    //Chris says: I think that this can potentially be hit by armories that are warping in, as they are only marked as fully initialized below
            //                 that said, even for a BurlustWarrior like we found in the log on a host at one point, this isn't worth stopping the simulation of the scourge over.

            if ( !entity.ShouldNotBeConsideredAsThreatToHumanTeam )
                entity.ShouldNotBeConsideredAsThreatToHumanTeam = true;
            //Update the experience and metal
            FInt experienceIncrease = BaseInfo.Difficulty.ExperiencePerSecond;
            if ( data.IsHybrid || data.IsSubjugator ) //hybrids and subjugators level up more slowly
                experienceIncrease = BaseInfo.Difficulty.ExperiencePerSecond / 2;

            data.Experience += experienceIncrease;
            if ( data.ExperienceForNextLevel > data.Experience &&
                 overflowExperience > 0 )
            {
                //If we need more experience to level up and have some bonus experience
                //give a small amount of our bonus experience.
                //Note that overflow experience can only happen if there are
                //a ton of units, so we're not worried about having leftover experience
                int amountToDonate = overflowExperience / 20;
                if ( amountToDonate == 0 )
                    amountToDonate++;
                FInt debugPrevExt = data.Experience;
                data.Experience += amountToDonate;
                overflowExperience -= amountToDonate;
                //ArcenDebugging.ArcenDebugLogSingleLine( entity.ToStringWithPlanet() + " getting overflow Exp " + debugPrevExt + " -> " + data.Experience + " we have " + overflowExperience + " overflow left.", Verbosity.DoNotShow );
            }

            if ( entity.TypeData.GetHasTag( "ScourgeSpawner" ) )
            {
                int numTriggers = this.BaseInfo.HighestScienceEarnedByPlayer / BaseInfo.Difficulty.UnitForScienceScaling;
                FInt baseSpawnerIncome = BaseInfo.Difficulty.BaseSpawnerMetalIncome + numTriggers * BaseInfo.Difficulty.SpawnerIncomeIncreasePerScienceUnit;
                FInt metalIncome = baseSpawnerIncome + (entity.CurrentMarkLevel - 1) * BaseInfo.Difficulty.SpawnerMetalIncomeIncreasePerMark;

                data.StoredMetal += metalIncome;
                data.MetalIncomeLastSecond_ForUI = metalIncome;
            }
            if ( entity.TypeData.GetHasTag( "ScourgeArmory" ) )
            {
                if ( data.ScourgeTypeId == -1 )
                {
                    //when an armory has finished warping in, assign it a type ID
                    List <ScourgeTypeData> list = BaseInfo.EmpireBaseInfo.UnlockedRaces.GetDisplayList();
                    ScourgeTypeData typedata = list[Context.RandomToUse.Next(0, list.Count)];
                    if ( typedata == null )
                        throw new Exception( "Got null row from ScourgeTypeDataTable" );
                    data.ScourgeTypeId = typedata.id;
                    data.FullyInitialized = true;
                }
            }
        }

        private void UpdateScourgeDefender_OnMainSimOnly( Faction faction, ArcenHostOnlySimContext Context, GameEntity_Squad entity )
        {
            if ( entity == null )
                return;
            ScourgePerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
            if ( data == null )
                return;
            GameEntity_Squad homeStructure = data.HomeDefensiveStructure.GetSquad();
            if ( homeStructure == null )
            {
                FInt percent = FInt.FromParts(2, 000);
                int damageToTake = ((percent * entity.GetMaxHullPoints()) / 100).GetNearestIntPreferringHigher();
                entity.TakeDamageDirectly( damageToTake, null, null, DamageSource.BeingScrapped, Context);
                return;
            }
            if ( homeStructure.CurrentMarkLevel != entity.CurrentMarkLevel )
                entity.SetCurrentMarkLevel( homeStructure.CurrentMarkLevel );

        }
        private void UpdateDefendsRegionIfNecessary_OnMainSimOnly( Faction faction, ArcenHostOnlySimContext Context, GameEntity_Squad entity )
        {
            if ( entity == null )
                return;
            ScourgePerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
            if ( data == null )
                return;
            int tierOneCap = 10;
            int tierTwoCap = 4;
            int tierThreeCap = 1;
            int cap = 0;
            //ArcenDebugging.LogSingleLine("checking if " + entity.ToStringWithPlanet() + " should build shit. current metal " + data.StoredMetal, Verbosity.DoNotShow );
            if ( entity.TypeData.GetHasTag("TierOne"))
            {
                data.StoredMetal += 4;
                cap = tierOneCap;
            }
            if ( entity.TypeData.GetHasTag("TierTwo"))
            {
                data.StoredMetal += 5;
                cap = tierTwoCap;
            }
            if ( entity.TypeData.GetHasTag("TierThree"))
            {
                data.StoredMetal += 6;
                cap = tierThreeCap;
            }

            if ( data.StoredMetal <= 0 )
                return;
            string defenderTag = getDefenseTag(entity);
            GameEntityTypeData typeData =  GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, defenderTag);
            if ( typeData == null )
                throw new Exception("Could not find defender with tag " + defenderTag + ".");
            if ( !CanBuildDefender( entity, cap))
                return;
            if ( typeData.CostForAIToPurchase > data.StoredMetal )
                return;
            ArcenPoint spawnLocation = entity.Planet.GetSafePlacementPoint_AroundEntity(Context, typeData, entity, FInt.FromParts(0, 005), FInt.FromParts(0, 010));
            GameEntity_Squad defender = this.AttachedFaction.SpawnNewUnit_ReturnNullIfMPClient(
                Context, entity.Planet, spawnLocation, typeData, entity.CurrentMarkLevel,
                this.AttachedFaction.LooseFleet, 0,
                EntityBehaviorType.Attacker_Full, -1, null, "Scourge-Defender");
            ScourgePerUnitBaseInfo newData = defender.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>("ScourgePerUnitBaseInfo");
            newData.HomeDefensiveStructure = LazyLoadSquadWrapper.Create(entity);
            data.StoredMetal -= typeData.CostForAIToPurchase;
        }
        private bool CanBuildDefender(GameEntity_Squad structure, int cap)
        {
            Dictionary<SafeSquadWrapper, int> defenders = BaseInfo.DefendersPerStructure.GetDisplayDict();
            //ArcenDebugging.LogSingleLine("How many guardians does " + stronghold.ToStringWithPlanet() + " have? We see " + guardians.GetPairCount() + " possibilities", Verbosity.DoNotShow );
            int defenderCount = 0;
            foreach ( KeyValuePair<SafeSquadWrapper, int> defend in defenders )
            {
                //ArcenDebugging.LogSingleLine("Checking guardians to see if they match " + stronghold.ToStringWithPlanet() + " checking against " + guards.Key.GetSquad().ToString() , Verbosity.DoNotShow );
                if ( defend.Key.GetSquad() == structure)
                {
                    defenderCount = defend.Value;
                    break;
                }
            }

            if ( defenderCount >= cap )
                return false;
            return true;
        }
        private string getDefenseTag( GameEntity_Squad entity)
        {
            string defenderTag = "";
            if ( entity.TypeData.GetHasTag("BurlustDefenseTierOne"))
                defenderTag = "BurlustDefenderTierOne";
            if ( entity.TypeData.GetHasTag("NeinzulDefenseTierOne"))
                defenderTag = "NeinzulDefenderTierOne";
            if ( entity.TypeData.GetHasTag("PeltianDefenseTierOne"))
                defenderTag = "PeltianDefenderTierOne";
            if ( entity.TypeData.GetHasTag("EvuckDefenseTierOne"))
                defenderTag = "EvuckDefenderTierOne";
            if ( entity.TypeData.GetHasTag("ThoraxianDefenseTierOne"))
                defenderTag = "ThoraxianDefenderTierOne";
            if ( entity.TypeData.GetHasTag("BurlustDefenseTierOne"))
                defenderTag = "BurlustDefenderTierOne";
            if ( entity.TypeData.GetHasTag("ZenithDefenseTierOne"))
                defenderTag = "ZenithDefenderTierOne";
            if ( entity.TypeData.GetHasTag("SpireDefenseTierOne"))
                defenderTag = "SpireDefenderTierOne";

            if ( entity.TypeData.GetHasTag("BurlustDefenseTierTwo"))
                defenderTag = "BurlustDefenderTierTwo";
            if ( entity.TypeData.GetHasTag("NeinzulDefenseTierTwo"))
                defenderTag = "NeinzulDefenderTierTwo";
            if ( entity.TypeData.GetHasTag("PeltianDefenseTierTwo"))
                defenderTag = "PeltianDefenderTierTwo";
            if ( entity.TypeData.GetHasTag("EvuckDefenseTierTwo"))
                defenderTag = "EvuckDefenderTierTwo";
            if ( entity.TypeData.GetHasTag("ThoraxianDefenseTierTwo"))
                defenderTag = "ThoraxianDefenderTierTwo";
            if ( entity.TypeData.GetHasTag("BurlustDefenseTierTwo"))
                defenderTag = "BurlustDefenderTierTwo";
            if ( entity.TypeData.GetHasTag("ZenithDefenseTierTwo"))
                defenderTag = "ZenithDefenderTierTwo";
            if ( entity.TypeData.GetHasTag("SpireDefenseTierTwo"))
                defenderTag = "SpireDefenderTierTwo";

            if ( entity.TypeData.GetHasTag("BurlustDefenseTierThree"))
                defenderTag = "BurlustDefenderTierThree";
            if ( entity.TypeData.GetHasTag("NeinzulDefenseTierThree"))
                defenderTag = "NeinzulDefenderTierThree";
            if ( entity.TypeData.GetHasTag("PeltianDefenseTierThree"))
                defenderTag = "PeltianDefenderTierThree";
            if ( entity.TypeData.GetHasTag("EvuckDefenseTierThree"))
                defenderTag = "EvuckDefenderTierThree";
            if ( entity.TypeData.GetHasTag("ThoraxianDefenseTierThree"))
                defenderTag = "ThoraxianDefenderTierThree";
            if ( entity.TypeData.GetHasTag("BurlustDefenseTierThree"))
                defenderTag = "BurlustDefenderTierThree";
            if ( entity.TypeData.GetHasTag("ZenithDefenseTierThree"))
                defenderTag = "ZenithDefenderTierThree";
            if ( entity.TypeData.GetHasTag("SpireDefenseTierThree"))
                defenderTag = "SpireDefenderTierThree";

            if ( entity.TypeData.GetHasTag("DefenseTierThree"))
                defenderTag = "DefenderTierThree";

            if ( defenderTag == "" )
                throw new Exception("Could not find defenderTag for " + entity.ToStringWithPlanet());
            return defenderTag;
        }

        private void UpdateSummonerIfNecessary_OnMainSimOnly( Faction faction, ArcenHostOnlySimContext Context, GameEntity_Squad entity )
        {
            if ( entity == null )
                return;
            EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> myFactionData = entity.Planet.GetStanceDataForFaction( AttachedFaction );
            int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
            int alliedStrength = myFactionData[FactionStance.Friendly].TotalStrength + myFactionData[FactionStance.Self].TotalStrength;
            FInt percent = FInt.Zero;
            if ( hostileStrength > alliedStrength )
                return; //if we are outnumbered, no need to attrition
            if ( hostileStrength <= 1 * 1000 )
                percent = FInt.FromParts(1, 250);
            else if ( hostileStrength < alliedStrength / 10 )
                percent = FInt.FromParts(0, 750);
            else
            {
                percent = FInt.FromParts(0, 200);
            }

            int bonus = entity.GetSecondsSinceCreation() / 180;
            percent += bonus;
            int damageToTake = ((percent * entity.GetMaxHullPoints()) / 100).GetNearestIntPreferringHigher();
            entity.TakeDamageDirectly( damageToTake, null, null, DamageSource.BeingScrapped, Context);
        }
        private void UpgradeNeophytesIfNecessary_OnMainSimOnly( Faction faction, ArcenHostOnlySimContext Context, GameEntity_Squad entity )
        {
            if ( entity == null )
                return;

            ScourgePerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
            //note that eventually we need some logic to see whether it makes sense to create builders or warriors
            //but for now lets just hard code it
            if ( data == null )
                return;


            int percentMustEvolveBeforeJoiningFireteam = 20; //some units will wait to evolve before joining a fireteam
            if ( this.BaseInfo.Intensity >= 8 )
                percentMustEvolveBeforeJoiningFireteam = 35;
            bool mustEvolve = false;
            if ( data.Experience > data.ExperienceForNextLevel )
            {
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ScourgeWarriorBase" );
                FInt experienceRequired = BaseInfo.Difficulty.BaseExperienceForLevelupWarriors;
                if ( Context.RandomToUse.Next( 0, 100 ) < percentMustEvolveBeforeJoiningFireteam )
                    mustEvolve = true;

                PlanetFaction pFaction = entity.Planet.GetPlanetFactionForFaction( faction );
                GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                     pFaction.FleetUsedAtPlanet, 0, entity.WorldLocation, Context, "Scourge-UpgradeNeophyte" ); //is fine, main sim thread
                if ( newEntity != null )
                {
                    ScourgePerUnitBaseInfo newdata = newEntity.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                    newdata.ExperienceForNextLevel = experienceRequired;
                    newEntity.FireteamId = -1;
                    newdata.FireteamId = -1;
                    newdata.MustEvolveBeforeJoiningFireteam = mustEvolve;
                    newdata.FullyInitialized = true;
                    newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                }
                entity.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
            }
        }
        private bool HandleNemesisSpawning_OnMainSimOnly( Faction faction, GameEntity_Squad oldSubjugator, ArcenHostOnlySimContext Context )
        {
            //called from the upgrade warrior path;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Scourge-HandleNemesisSpawning_OnMainSimOnly-trace", 10f ) : null;
            if ( BaseInfo.LastTimeNemesisExisted >= World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.TimeForNemesisSpawning )
                return false; //nothing summoned
            BaseInfo.CountdownTimerForNemesis = BaseInfo.Difficulty.TimeForNemesisSpawning;
            // if ( !this.BaseInfo.AIAllied )
            //   return false; //only AI-allied scourge can summon a Nemesis
            //
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "WarpingInScourgeNemesis" );
            if ( entityData == null )
                throw new Exception( "No WarpingInScourgeNemesis unit defined in XML" );
            PlanetFaction pFaction = oldSubjugator.PlanetFaction;
            GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 0,
                 pFaction.FleetUsedAtPlanet, 0, oldSubjugator.WorldLocation, Context, "Scourge-Nemesis" ); //is fine, main sim thread
            if ( newEntity != null )
            {
                newEntity.TransformsIntoAfterTime = "Nemesis";
                newEntity.SecondsTillTransformation = BaseInfo.CountdownTimerForNemesis;

                ScourgePerUnitBaseInfo newdata = newEntity.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                newEntity.FireteamId = -1;
                newdata.FireteamId = -1;
                newdata.IsNemesis = true;
                newdata.FullyInitialized = true;

                newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
            }
            oldSubjugator.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.RemoveOldUnitWeNoLongerNeed ); //remove the old unit


            if ( ArcenNetworkAuthority.GetIsHostMode() )
            {
                SquadViewChatHandlerBase chatHandlerOrNull = null;
                if ( newEntity != null && newEntity.Planet != null && newEntity.Planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                {
                    chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( newEntity );
                }
                if ( this.BaseInfo.PlayerAllied )
                    World_AIW2.Instance.QueueChatMessageOrCommand( "An " + faction.StartFactionColourForLog() + "Allied Scourge</color> Nemesis is spawning in the galaxy!", ChatType.LogToCentralChat, chatHandlerOrNull );
                else
                    World_AIW2.Instance.QueueChatMessageOrCommand( "A " + faction.StartFactionColourForLog() + "Scourge</color> Nemesis is spawning in the galaxy!", ChatType.LogToCentralChat, chatHandlerOrNull );
            }
            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
            return true;
        }
        private void HandleSubjugatorSpawning_OnMainSimOnly( Faction faction, ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Scourge-HandleSubjugatorSpawning_OnMainSimOnly-trace", 10f ) : null;

            int numAllowedSubjugators = BaseInfo.NumTopTierSpawners.Display / this.BaseInfo.Difficulty.TopTierSpawnersRequiredPerSubjugator;
            if ( faction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) )
                numAllowedSubjugators *= 4;

            if ( numAllowedSubjugators > BaseInfo.SubjugatorsInGalaxy.Count )
            {
                if ( BaseInfo.CountdownTimerForNextSubjugator == -1 )
                {
                    if ( tracing )
                        tracingBuffer.Add( "Hit Subjugator Spawning Threshold " + numAllowedSubjugators + " > " + BaseInfo.SubjugatorsInGalaxy.Count + "; start the spawn timer\n" );
                    if ( BaseInfo.Difficulty.TimeForNemesisSpawning == -1 ) //subjugators and nemeses take the same amount of time to spawn; no need for another variable
                        throw new Exception( "XML data for subjugator is not synced to BaseInfo (this is needed to give data to the UI" ); //this Exception is just a careful mid-development check
                    BaseInfo.CountdownTimerForNextSubjugator = BaseInfo.Difficulty.TimeForNemesisSpawning;
                    #region Tracing
                    if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                    return;
                }
                else
                {
                    //Subjugator spawn inbound
                    BaseInfo.CountdownTimerForNextSubjugator--;
                    GameEntity_Squad spawnerForSummoning = null;
                    List<SafeSquadWrapper> spawnersInGalaxy = this.BaseInfo.Spawners.GetDisplayList();
                    for ( int i = 0; i < spawnersInGalaxy.Count; i++ )
                    {
                        //we could make this spawn at any random mark 7 spawner if that makes more sense?
                        if ( spawnersInGalaxy[i].CurrentMarkLevel == 7 )
                        {
                            spawnerForSummoning = spawnersInGalaxy[i].GetSquad();
                            if ( spawnerForSummoning != null )
                                break;
                        }
                    }
                    if ( spawnerForSummoning == null )
                        throw new Exception( "Could not find mark 7 spawner to summon Subjugator." );

                    if ( BaseInfo.CountdownTimerForNextSubjugator == 0 )
                    {
                        

                        BaseInfo.CountdownTimerForNextSubjugator = -1;
                        GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ScourgeSubjugator" );
                        if ( entityData == null )
                            throw new Exception( "No ScourgeSubjugator unit defined in XML" );
                        PlanetFaction pFaction = spawnerForSummoning.Planet.GetPlanetFactionForFaction( faction );
                        GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 0,
                              pFaction.FleetUsedAtPlanet, 0, spawnerForSummoning.WorldLocation, Context, "Scourge-Subjugator" ); //is fine, main sim thread
                        if ( newEntity != null )
                        {
                            ScourgePerUnitBaseInfo newdata = newEntity.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                            newEntity.FireteamId = -1;
                            newdata.FireteamId = -1;
                            newdata.IsSubjugator = true;
                            newdata.FullyInitialized = true;

                            newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread

                            if ( ArcenNetworkAuthority.GetIsHostMode() )
                            {
                                SquadViewChatHandlerBase chatHandlerOrNull = null;
                                if ( newEntity != null && newEntity.Planet != null && newEntity.Planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                                {
                                    chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                                    if ( chatHandlerOrNull != null )
                                        chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( newEntity );
                                }

                                if ( ArcenStrings.Equals( faction.BaseInfo.Allegiance, "Allied To Players" ) ||
                                     ArcenStrings.Equals( faction.BaseInfo.Allegiance, "Friendly To Players" ) )
                                    World_AIW2.Instance.QueueChatMessageOrCommand( "An " + faction.StartFactionColourForLog() + "Allied Scourge</color> Subjugator has spawned in the galaxy!", ChatType.LogToCentralChat, chatHandlerOrNull );
                                else if ( spawnerForSummoning.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
                                    World_AIW2.Instance.QueueChatMessageOrCommand( "A " + faction.StartFactionColourForLog() + "Scourge</color> Subjugator has spawned in the galaxy!", ChatType.LogToCentralChat, chatHandlerOrNull );
                            }
                        }
                    }
                }
            }
            if ( numAllowedSubjugators <= BaseInfo.SubjugatorsInGalaxy.Count &&
                 BaseInfo.CountdownTimerForNextSubjugator != -1 )
            {
                if ( tracing )
                    tracingBuffer.Add( "Dipped below Subjugator Spawning Threshold " + numAllowedSubjugators + " > " + BaseInfo.SubjugatorsInGalaxy.Count + "; unset the spawn timer\n" );
                BaseInfo.CountdownTimerForNextSubjugator = -1;
            }
            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
        }

        private void HandleSpawning_OnMainSimOnly( Faction faction, ArcenHostOnlySimContext Context, GameEntity_Squad entity, ref int OverflowExperience )
        {
            if ( entity == null )
                return;

            //This faction creates new strength for the Scourge. This is typically by spawning
            //neophytes. This faction handles the throttling of the Scourge.
            //We try not to create too many units, especially if we are over cap or the game is having performance problems
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Scourge-HandleSpawning_OnMainSimOnly-trace", 10f ) : null;

            bool debug = false; //this tracing is only needed for unit test (I hope)
            ScourgePerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
            if ( data == null )
                throw new Exception("Could not get ScourgePerUnitBaseInfo for " + entity.ToStringWithPlanetAndOwner() );
            bool neophyteThrottled = this.BaseInfo.NeophytesPerPlanet[entity.Planet] > 40;
            bool performanceThrottled = FactionUtilityMethods.Instance.ShouldFactionsThrottle();
            FInt throttleStartPoint = FInt.FromParts( 0, 700 ); //when we start throttling
            FInt throttleEndPoint = FInt.FromParts( 1, 300 ); //when we aren't producing more ships at all
            FInt throttleRange = throttleEndPoint - throttleStartPoint;
            FInt capToUse = this.BaseInfo.OverCap;
            if ( this.BaseInfo.OverCap < throttleEndPoint && performanceThrottled )
                capToUse = throttleEndPoint; //the game itself is struggling, so heavy throttle
            if ( this.BaseInfo.OverCap < throttleEndPoint && neophyteThrottled )
                capToUse = throttleEndPoint; //we have a lot of neophytes already for this spawner, so don't make more


            FInt throttleAmount = capToUse - throttleStartPoint;
            int actualCost = (BaseInfo.Difficulty.NeophyteCost * BaseInfo.NeophyteCostMultiplier).IntValue;
            int maxToSpawn = 4; //don't spawn too many at a time, it's crazy on higher intensities and we don't want to hog resources
            while ( data.StoredMetal > actualCost && maxToSpawn > 0 )
            {
                if ( throttleAmount > FInt.Zero ) //we are above our throttle start point
                {
                    //decide whether we are throttling; if we are then
                    //some of the metal becomes overflow experience
                    //This uses 0.1 of the range as the base unit (so if the range is 0.6 then every 0.1 we go up in cap, we increase the percentage)
                    int overflowPercent =  (throttleAmount * (100 / throttleRange) ).IntValue;
                    //ArcenDebugging.ArcenDebugLogSingleLine( entity.ToStringWithPlanetAndOwner() + " has " + throttleAmount + " of cap " + this.BaseInfo.OverCap + " (throttle start " + throttleStartPoint +"), and will donate exp at " + overflowPercent + "%. perf throttle: " + performanceThrottled + " neophyteThrottled " + neophyteThrottled , Verbosity.DoNotShow );
                    if ( Context.RandomToUse.Next(1, 100 ) < overflowPercent )
                    {
                        OverflowExperience += actualCost;
                        data.StoredMetal -= actualCost;
                        continue;
                    }
                }
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ScourgeNeophyte" );
                PlanetFaction pFaction = entity.Planet.GetPlanetFactionForFaction( faction );
                GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 0,
                          pFaction.FleetUsedAtPlanet, 0, entity.WorldLocation, Context, "Scourge-NewNeophyte" );
                if ( newEntity != null )
                {
                    ScourgePerUnitBaseInfo newdata = newEntity.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                    newdata.ExperienceForNextLevel = BaseInfo.Difficulty.NeophyteExperienceRequired;
                    newdata.Experience = FInt.Zero;

                    newdata.FullyInitialized = true;
                    if ( tracing && debug )
                        tracingBuffer.Add( "Spawning " + newEntity.ToStringWithPlanet() + "\n" );
                }

                data.StoredMetal -= actualCost;
                maxToSpawn--;
            }
            int directWarriorCost = actualCost * 20;
            if ( data.StoredMetal > directWarriorCost )
            {
                //We don't want Spawners to generate tons of metal they can't spend (it looks bad in the UI),
                //so if we can'spend our metal quickly enough, this is a safety valve.
                //The scourge gets a higher mark base warrior in exchange for wiping all excess metal
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ScourgeWarriorBase" );
                FInt experienceRequired = BaseInfo.Difficulty.BaseExperienceForLevelupWarriors;
                PlanetFaction pFaction = entity.PlanetFaction;
                GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 4,
                     pFaction.FleetUsedAtPlanet, 0, entity.WorldLocation, Context, "Scourge-WarriorDirectSpawn" ); //is fine, main sim thread
                if ( newEntity != null )
                {
                    ScourgePerUnitBaseInfo newdata = newEntity.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                    newdata.ExperienceForNextLevel = experienceRequired;
                    newEntity.FireteamId = -1;
                    newdata.FireteamId = -1;
                    newdata.MustEvolveBeforeJoiningFireteam = false;
                    newdata.FullyInitialized = true;
                    newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                }
                data.StoredMetal = FInt.Zero;
            }
            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
        }
        private void HandleSeeds_OnMainSimOnly( Faction faction, ArcenHostOnlySimContext Context )
        {
            //Seeds are spawned periodically from random spawners
            //If it's been a while, create a new seed
            int seedInterval = BaseInfo.Difficulty.SeedInterval;
            int seedVariance = BaseInfo.Difficulty.SeedVariance;
            if ( BaseInfo.TimeForNextSeed == -1 )
                BaseInfo.TimeForNextSeed = World_AIW2.Instance.GameSecond + seedInterval + Context.RandomToUse.Next(0, seedVariance);
            bool debug = false;
            if ( debug && World_AIW2.Instance.GameSecond % 10 == 0 )
                ArcenDebugging.LogSingleLine("time till next seed spawn " + (BaseInfo.TimeForNextSeed - World_AIW2.Instance.GameSecond ), Verbosity.DoNotShow );
            if ( BaseInfo.TimeForNextSeed < World_AIW2.Instance.GameSecond )
            {
                if ( debug )
                    ArcenDebugging.LogSingleLine("spawning a seed", Verbosity.DoNotShow );
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ScourgeSeed" );
                if ( entityData == null )
                    throw new Exception( "No ScourgeSeed unit defined in XML" );

                BaseInfo.TimeForNextSeed = World_AIW2.Instance.GameSecond + seedInterval + Context.RandomToUse.Next(0, seedVariance);
                List<SafeSquadWrapper> spawnersInGalaxy = this.BaseInfo.Spawners.GetDisplayList();
                if ( spawnersInGalaxy.Count > 0 )
                {
                    GameEntity_Squad spawner = spawnersInGalaxy[Context.RandomToUse.Next(0, spawnersInGalaxy.Count)].GetSquad();
                    if ( debug )
                        ArcenDebugging.LogSingleLine("spawning near " + spawner.ToStringWithPlanet(), Verbosity.DoNotShow );

                    PlanetFaction pFaction = spawner.PlanetFaction;
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 0,
                        pFaction.FleetUsedAtPlanet, 0, spawner.WorldLocation, Context, "Scourge-Seed" ); //is fine, main sim thread
                    if ( debug && newEntity != null )
                        ArcenDebugging.LogSingleLine("spawned " + newEntity.ToString(), Verbosity.DoNotShow );
                }
            }
            //if we are where we are going, transform into a flower
            foreach ( GameEntity_Squad seed in BaseInfo.Seeds.DisplaySquads() )
            {
                Faction controllingFaction = seed.Planet.GetControllingFaction();
                if ( controllingFaction.Type != FactionType.AI )
                    continue;
                if ( seed.HasQueuedOrders() )
                    continue;
                int timeOnPlanet = World_AIW2.Instance.GameSecond - seed.GameSecondEnteredThisPlanet;
                if ( timeOnPlanet < 50 )
                    continue;
                //we're on an AI planet, we're not going anywhere, and we've been there for a while. Turn into a flower
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ScourgeFlower" );
                if ( entityData == null )
                    throw new Exception( "No ScourgeFlower unit defined in XML" );
                PlanetFaction pFaction = seed.PlanetFaction;
                GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                    pFaction.Faction.LooseFleet, 0, seed.WorldLocation, Context, "Scourge-ScourgeFlower" );
                if ( debug && newEntity != null)
                    ArcenDebugging.LogSingleLine( newEntity.ToStringWithPlanetAndOwner() + " is becoming a flower ", Verbosity.DoNotShow );

                seed.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut);
            }
        }
                

        private void HandleFlowers_OnMainSimOnly( Faction faction, ArcenHostOnlySimContext Context )
        {
            //transform into a crystal if appropriate
            bool debug = false;
            foreach ( GameEntity_Squad flower in BaseInfo.Flowers.DisplaySquads() )
            {
                Faction controllingFaction = flower.Planet.GetControllingFaction();
                if ( controllingFaction.Type != FactionType.AI )
                {
                    //AI must control the planet
                    if ( debug )
                        ArcenDebugging.LogSingleLine(flower.ToStringWithPlanetAndOwner() + " is despawning; ai no longer owns planet", Verbosity.DoNotShow );
                    flower.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut);
                    continue;
                }
                if ( flower.GameSecondCreated < World_AIW2.Instance.GameSecond - BaseInfo.Difficulty.FlowerGrowTime )
                {
                    //transform into a crystal controlled by the AI
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "CorbomiteCrystal" );
                    if ( entityData == null )
                        throw new Exception( "No CorbomiteCrystal unit defined in XML" );

                    PlanetFaction pFaction = flower.Planet.GetPlanetFactionForFaction( controllingFaction );
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                        pFaction.Faction.LooseFleet, 0, flower.WorldLocation, Context, "Scourge-CorbomiteCrystal" );
                    if ( debug )
                        ArcenDebugging.LogSingleLine(flower.ToStringWithPlanetAndOwner() + " is becoming a crystal ", Verbosity.DoNotShow );

                    flower.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut);
                }
            }
        }

        //okay because this is only used locally one one thread
        private readonly List<Planet> PlanetsBeingAttackedByAllies_ScourgeOnly = List<Planet>.Create_WillNeverBeGCed( 500, "ScourgeFactionDeepInfo-PlanetsBeingAttackedByAllies_ScourgeOnly" );
        private readonly List<SafeSquadWrapper> LongRangePlanningArmories = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ScourgeFactionDeepInfo-LongRangePlanningArmories" ); //keep a LRP copy so we don't race with the sim copy
        private static readonly List<Planet> WorkingPlanetsLRP = List<Planet>.Create_WillNeverBeGCed( 500, "ScourgeVasssalFactionDeepInfo-WorkingPlanetsLRP" );
        private readonly List<SafeSquadWrapper> LongRangePlanningSpawners = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ScourgeFactionDeepInfo-LongRangePlanningSpawners" ); //keep a LRP copy so we don't race with the sim copy
        private readonly List<SafeSquadWrapper> LongRangePlanningBuilders = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ScourgeFactionDeepInfo-LongRangePlanningBuilders" ); //keep a LRP copy so we don't race with the sim copy
        private readonly List<SafeSquadWrapper> LongRangePlanningFortresses = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ScourgeFactionDeepInfo-LongRangePlanningFortresses" ); //keep a LRP copy so we don't race with the sim copy
        private readonly List<SafeSquadWrapper> LongRangePlanningSeeds = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ScourgeFactionDeepInfo-LongRangePlanningSeeds" ); //keep a LRP copy so we don't race with the sim copy
        private readonly List<SafeSquadWrapper> LongRangePlanningFlowers = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ScourgeFactionDeepInfo-LongRangePlanningFlowers" ); //keep a LRP copy so we don't race with the sim copy
        private readonly List<SafeSquadWrapper> LongRangePlanningWarpingInSpawners = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ScourgeFactionDeepInfo-LongRangePlanningWarpingInSpawners" ); //keep a LRP copy so we don't race with the sim copy
        private readonly List<SafeSquadWrapper> LongRangePlanningWarpingInArmories = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ScourgeFactionDeepInfo-LongRangePlanningWarpingInArmories" ); //keep a LRP copy so we don't race with the sim copy
        private readonly List<SafeSquadWrapper> LongRangePlanningWarpingInFortresses = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ScourgeFactionDeepInfo-LongRangePlanningWarpingInFortresses" ); //keep a LRP copy so we don't race with the sim copy

        private readonly List<Planet> LongRangePlanningInfrastructurePlanets = List<Planet>.Create_WillNeverBeGCed( 500, "ScourgeFactionDeepInfo-LongRangePlanningInfrastructurePlanets" ); //keep a LRP copy so we don't race with the sim copy
        private readonly List<Planet> LongRangePlanningInfrastructureUndefendedPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "ScourgeFactionDeepInfo-LongRangePlanningInfrastructureUndefendedPlanets" ); //keep a LRP copy so we don't race with the sim copy

        private readonly List<SafeSquadWrapper> UnassignedWarriors = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ScourgeFactionDeepInfo-UnassignedWarriors" );
        private readonly ArcenLessLinkedList<Fireteam> TeamsThatNeedTargets = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "ScourgeFactionDeepInfo-TeamsThatNeedTargets" );//multiple factions can be calling UpdateFireteams at once


        private List<Fireteam> lrp_escortingFireteams = List<Fireteam>.Create_WillNeverBeGCed( 200, "ScourgeFactionDeepInfo-lrp_escortingFireteams" );
        private static readonly List<SafeSquadWrapper> AutoDefendingShipsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ScourgeVassalFactionDeepInfo-AutoDefendingShipsLRP" );
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            /* Overall outline. 
               First, build some unit lists because we can't use the Sim-copies. This duplicates some work in Sim-Stage 2

               Then, move neophytes around.
               Then handle Builders to travel around and build stuff, or upgrade things

               Then warriors

               foreach idle warriors
                    Assign to a Fireteam or create a new Fireteam for it

               foreach warrior in a Fireteam, make sure the Fireteam has the ship in its list

               We iterate twice over the Fireteams. First is generic setup, second is the FireteamRegiment stuff.
               This is a very broad outline and might be mildly incorrect. Note that other factions that use Fireteams use the logic in Fireteam.cs;
               the scourge has its own rules though

               foreach fireteam
                   Update the non-serialized fields if necessary

                   If it has a target, see if that target still makes sense. If it doesn't make sense, pick a new target

                   Do you have a target? If not, find a target

                   If it has a target, do you have a lurk location? If you don't have a lurk location, make one and send your ships there

                   If it has a target and a lurk location, check if my ships are there. If not, keep waiting

                   If your ships are at the lurk location, set "Ready To Attack"


                   Then foreach fireteam that's ready to fight
                        Count the Fireteams ready to go for each target, see if we can win "handily". If we can crush, just go

                        If it's a reasonably close call, feel free to sacrifice another Fireteam to attack elsewhere as a distraction (ideally if the defenses are more mobile)
               */
            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Scourge-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            if ( tracing ) tracingBuffer.Add( this.TracingName ).Add( " DoLongRangePlanning trace begins for faction " ).Add( AttachedFaction.FactionIndex ).Add( "\n" );
            #endregion
            bool debug = false;
            if ( BaseInfo.ScourgeIsSuppressed.Display )
                return;
            int debugCode = 0;
            TeamsAimedAtPlanet.Clear();
            UnassignedWarriors.Clear();
            AutoDefendingShipsLRP.Clear();
            WorkingPlanetsLRP.Clear();
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                int maxNeophytesToMove = 100;
                debugCode = 1000;
                int warriorsAllowedToUpgrade = 200;
                int warriorsAllowedToGoToSummoner = 200;

                PrecalculatePlanetsUnderAttackByAllies_ScourgeOnly( AttachedFaction, Context );
                FireteamUtility.CleanUpDisbandedFireteams( BaseInfo.Teams );
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
                {
                    //first iterate over and clean up stale data (ships list and strength)
                    //I was seeing bizarre problems where sometimes ships would be on 2 fireteam lists simultaneously.
                    //A list would have been A, B, C, D. The game would have created a new unit X. At the next Long Range Planning step
                    //I would see A, X, C, D. So lets recalculate the list each time.
                    team.DeepInfo.Reset();
                }
                LongRangePlanningArmories.Clear();
                LongRangePlanningSpawners.Clear();
                LongRangePlanningBuilders.Clear();
                LongRangePlanningFortresses.Clear();
                LongRangePlanningSeeds.Clear();
                LongRangePlanningFlowers.Clear();
                LongRangePlanningWarpingInArmories.Clear();
                LongRangePlanningWarpingInSpawners.Clear();
                LongRangePlanningWarpingInFortresses.Clear();
                TeamsThatNeedTargets.Clear();
                LongRangePlanningInfrastructureUndefendedPlanets.Clear();
                LongRangePlanningInfrastructurePlanets.Clear();
                FactionUtilityMethods.Instance.FlushUnitsFromReinforcementPointsOnAllRelevantPlanets( AttachedFaction, Context, 5f );
                Planet scourgeSummonDestination = null;
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "WarpingInScourgeArmory" ) )
                {
                    //we need a list of various structure types, but can't share it with the Sim code lest it race
                    LongRangePlanningInfrastructurePlanets.Add( entity.Planet );
                    LongRangePlanningWarpingInArmories.Add( entity );
                }

                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "ScourgeBuilder" ) )
                {
                    //we need a list of various structure types, but can't share it with the Sim code lest it race
                    LongRangePlanningBuilders.Add( entity );
                }

                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "WarpingInScourgeSpawner" ) )
                {
                    //we need a list of various structure types, but can't share it with the Sim code lest it race
                    LongRangePlanningInfrastructurePlanets.Add( entity.Planet );
                    LongRangePlanningWarpingInSpawners.Add( entity );
                }
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "WarpingInScourgeFortress" ) )
                {
                    //we need a list of various structure types, but can't share it with the Sim code lest it race
                    LongRangePlanningWarpingInFortresses.Add( entity );
                }
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "ScourgeArmory" ) )
                {
                    //we need a list of various structure types, but can't share it with the Sim code lest it race
                    LongRangePlanningInfrastructurePlanets.Add( entity.Planet );
                    LongRangePlanningArmories.Add( entity );
                }
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "ScourgeSpawner" ) )
                {
                    //we need a list of various structure types, but can't share it with the Sim code lest it race
                    LongRangePlanningInfrastructurePlanets.Add( entity.Planet );
                    ScourgePerUnitBaseInfo spawnerData = entity.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                    if ( spawnerData != null )
                        LongRangePlanningSpawners.Add( entity ); //I've seen this be null sometimes, maybe a race?
                }
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "ScourgeFortress" ) )
                {
                    //we need a list of various structure types, but can't share it with the Sim code lest it race
                    LongRangePlanningFortresses.Add( entity );
                }
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "ScourgeSeed" ) )
                {
                    //we need a list of various structure types, but can't share it with the Sim code lest it race
                    LongRangePlanningSeeds.Add( entity );
                }
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "ScourgeFlower" ) )
                {
                    //we need a list of various structure types, but can't share it with the Sim code lest it race
                    LongRangePlanningFlowers.Add( entity );
                }

                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "ScourgeSummoner" ) )
                {
                    //we are summoned to this location
                    scourgeSummonDestination = entity.Planet;
                }

                debugCode = 1040;
                debugCode = 1041;
                cb_svInfraFaction = AttachedFaction;
                LongRangePlanningInfrastructurePlanets.Sort( static delegate ( Planet Left, Planet Right )
                {
                    var lFaction = Left.GetStanceDataForFaction( cb_svInfraFaction );
                    var rFaction = Right.GetStanceDataForFaction( cb_svInfraFaction );
                    int LDefenses = lFaction[FactionStance.Self].TotalStrength +
                        lFaction[FactionStance.Friendly].TotalStrength;
                    int RDefenses = rFaction[FactionStance.Self].TotalStrength +
                        rFaction[FactionStance.Friendly].TotalStrength;
                    return RDefenses.CompareTo( LDefenses );
                } );

                for ( int i = 0; i < LongRangePlanningInfrastructurePlanets.Count; i++ )
                {
                    //for defensive fireteams, to make sure we have a defensive fireteam for each infrastructure (ideally)
                    bool foundDefensiveFleet = false;
                    Planet planet = LongRangePlanningInfrastructurePlanets[i];
                    foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
                    {
                        if ( team.DefenseMode && (team.LurkPlanet == planet ||
                                                  team.TargetPlanet == planet) )
                        {
                            foundDefensiveFleet = true;
                            break;
                        }
                    }
                    if ( !foundDefensiveFleet )
                        LongRangePlanningInfrastructureUndefendedPlanets.Add( planet );
                }
                for ( int i = 0; i < LongRangePlanningSeeds.Count; i++ )
                {
                    GameEntity_Squad seed = LongRangePlanningSeeds[i].GetSquad();
                    if ( seed == null )
                        continue;
                    if ( seed.HasQueuedOrders() )
                        continue;
                    Faction controllingFaction = seed.Planet.GetControllingFaction();
                    if ( controllingFaction.Type != FactionType.AI )
                    {
                        //Go to a randomly chosen planet
                        bool allowHostilePlanet = true;
                        SendUnitToRandomAdjacentPlanet( AttachedFaction, Context, seed, allowHostilePlanet ); //go someplace random
                        continue;
                    }
                    for ( int j = 0; j < LongRangePlanningFlowers.Count; j++ )
                    {
                        GameEntity_Squad flower  = LongRangePlanningFlowers[j].GetSquad();
                        if ( flower == null )
                            continue;
                        if ( flower.Planet == seed.Planet )
                        {
                            //Go to a randomly chosen planet
                            bool allowHostilePlanet = true;
                            SendUnitToRandomAdjacentPlanet( AttachedFaction, Context, seed, allowHostilePlanet ); //go someplace random
                            continue;
                        }
                    }
                    for ( int k = 0; k < LongRangePlanningSeeds.Count; k++ )
                    {
                        GameEntity_Squad otherSeed  = LongRangePlanningSeeds[k].GetSquad();
                        if ( otherSeed == null )
                            continue;
                        if ( seed == otherSeed )
                            continue;
                        if ( seed.Planet == otherSeed.Planet && //another seed here
                             !otherSeed.HasQueuedOrders() && //and that other seed doesn't have orders
                             Context.RandomToUse.Next( 0, 100 ) < 50 ) //a bit of randomness
                        {
                            //Go to a randomly chosen planet
                            bool allowHostilePlanet = true;
                            SendUnitToRandomAdjacentPlanet( AttachedFaction, Context, seed, allowHostilePlanet ); //go someplace random
                            continue;
                        }
                    }

                    int timeOnPlanet = World_AIW2.Instance.GameSecond - seed.GameSecondEnteredThisPlanet;
                    if ( timeOnPlanet < 50 )
                    {
                        //go to a random location on a planet
                        FactionUtilityMethods.Instance.SendUnitToRandomLocation( AttachedFaction, Context, seed, 10f );
                        continue;
                    }
                }
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    if ( entity == null || entity.TypeData == null )
                        continue;
                    //iterate over all the units and give specific orders if necessary
                    ScourgePerUnitBaseInfo data = entity.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                    debugCode = 1050;
                    if ( !entity.TypeData.IsMobile )
                        continue;
                    if ( entity.TypeData.GetHasTag("ScourgeDefender"))
                    {
                        AutoDefendingShipsLRP.Add(entity);
                        continue; //these are handled elsewhere
                    }
                    debugCode = 1051;
                    if ( data == null || !data.FullyInitialized )
                    {
                        //                        ArcenDebugging.ArcenDebugLogSingleLine("Entity " + entity.ToString() + " is being skipped for uninitialized", Verbosity.DoNotShow );
                        continue; //has been created recently, skip
                    }
                    debugCode = 1052;
                    ParanoicallyCheckFireteamUnits( AttachedFaction, entity, data, Context, "A" );
                    debugCode = 1053;
                    if ( entity.TypeData.GetHasTag( "WarpingInScourgeNemesis" ) )
                        continue;
                    if ( entity.TypeData.GetHasTag( "ScourgeNeophyte" ) )
                    {
                        debugCode = 1060;
                        if ( !entity.IsNextOrderMovement() &&
                             maxNeophytesToMove > 0 &&
                             Context.RandomToUse.Next( 0, 100 ) < 20 )
                        {
                            if ( FactionUtilityMethods.Instance.SendUnitToRandomMetalGenerator( AttachedFaction, Context, entity, 10f ) )
                                maxNeophytesToMove--;
                        }
                        continue;
                    }
                    
                    if ( entity.TypeData.GetHasTag( "ScourgeWarrior" ) ||
                         entity.TypeData.GetHasTag( "ScourgeSubjugator" ) ||
                         entity.TypeData.GetHasTag( "ScourgeNemesis" ) )
                    {
                        debugCode = 1200;
                        if ( !data.FullyInitialized )
                        {
                            if ( tracing )
                                tracingBuffer.Add( entity.ToString() + " has uninitialized per unit data\n" );

                            continue;
                        }
                        if ( scourgeSummonDestination != null )
                        {
                            //Mod support; if we have built any summoners, all ships must go there
                            if ( entity.GetDestinationPlanet() != scourgeSummonDestination && warriorsAllowedToGoToSummoner-- > 0)
                                SendUnitToSpecificPlanet( this.AttachedFaction, entity, scourgeSummonDestination, Context, pathingCacheData );
                            continue;
                        }
                        ParanoicallyCheckFireteamUnits( AttachedFaction, entity, data, Context, "B" );
                        if ( FireteamBaseUtility.GetFireteamById( BaseInfo.Teams, data.FireteamId ) == null &&
                             entity.TypeData.GetHasTag( "ScourgeWarrior" ) ||
                             entity.TypeData.GetHasTag( "ScourgeSubjugator" ) ) //nemeses don't upgrade
                        {
                            debugCode = 1210;

                            if ( entity.CalculateNextHopPlanetIndex_Safe() > 0 && entity.CalculateNextHopPlanetIndex_Safe() != entity.GetPlanetIndexSafe() )
                                continue;  //this unit is currently going to another planet

                            //Units in a fireteam need to wait to upgrade until the fireteam either is used
                            //or disbands so its units can upgrade

                            //If we can upgrade ourselves, go to a planet with an armory.
                            //If we are at an armory planet, fly to the armory to upgrade
                            bool unitDispatched = false;
                            if ( warriorsAllowedToUpgrade > 0 )
                                unitDispatched = SendUnitToArmoryToUpgradeIfNecessary( AttachedFaction, Context, pathingCacheData,
                                                                                        entity, data );
                            debugCode = 1220;
                            if ( unitDispatched )
                            {
                                warriorsAllowedToUpgrade--;
                                continue;
                            }
                        }
                        debugCode = 1230;
                        if ( data.MustEvolveBeforeJoiningFireteam && !data.IsEvolved )
                            continue;

                        debugCode = 1300;
                        if ( data.FireteamId < 0 )
                        {
                            debugCode = 1400;
                            UnassignedWarriors.Add( entity );
                            debugCode = 1410;
                            ParanoicallyCheckFireteamUnits( AttachedFaction, entity, data, Context, "C" );
                            continue;
                        }
                        debugCode = 1500;

                        if ( data.FireteamId != entity.MinorFactionStackingID )
                            entity.MinorFactionStackingID = data.FireteamId;
                        Fireteam team = FireteamBaseUtility.GetFireteamById( BaseInfo.Teams, data.FireteamId );
                        debugCode = 1510;
                        if ( team == null )
                        {
                            entity.FireteamId = -1;
                            data.FireteamId = -1;
                            continue;
                        }
                        if ( team.status == FireteamStatus.Disbanded )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "unit " + entity.ToString() + " is in now-disbanded fireteam " + team.FireTeamID + ". This indicates a bug, but it can be bandaid'd here", Verbosity.DoNotShow );
                            entity.FireteamId = -1;
                            data.FireteamId = -1;
                            continue;
                        }
                        bool wasNecessary = team.DeepInfo.AddUnit( entity );
                        if ( wasNecessary && tracing && debug )
                            tracingBuffer.Add( "Unit " + entity.ToString() + " mark " + entity.CurrentMarkLevel + " is in fireteam " + data.FireteamId + " (first pass). " + data.ToString() + "\n" );
                        debugCode = 1600;
                        ParanoicallyCheckFireteamUnits( AttachedFaction, entity, data, Context, "D" );
                        continue;
                    }
                }
                debugCode = 2000;

                //The fireteams have been built above.
                //Iterate once over the teams to get everyone prepped and handle the various status possibilities
                //also generate the necessary data for choosing whether to attack
                lrp_escortingFireteams.Clear();
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
                {
                    if ( team.status == FireteamStatus.Escorting )
                        lrp_escortingFireteams.Add( team );
                }
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
                {
                    debugCode = 2100;
                    team.DeepInfo.UpdateNonSerializedFields_LRP( AttachedFaction, tracingBuffer, Context );
                    debugCode = 2101;

                    debugCode = 2105;
                    if ( team.DeepInfo.ShipsInFireteam.Count == 0 ) //all our ships are dead :-(. We probably got ambushed or something
                    {
                        debugCode = 1150;
                        if ( tracing )
                            tracingBuffer.Add( "Fireteam " + team.FireTeamID + " has no ships; disband" );
                        team.Disband( this.AttachedFaction, Context );
                        continue;
                    }
                    if ( team.DeepInfo.FireteamShouldDisband )
                    {
                        debugCode = 1051;
                        if ( tracing )
                            tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is should disband, something is wrong\n" );
                        team.Disband( this.AttachedFaction, Context );
                        continue;
                    }

                    if ( team.status == FireteamStatus.Assembling )
                    {
                        debugCode = 2110;
                        if ( team.DeepInfo.TeamStrength >= team.StrengthToBringOnline )
                        {
                            team.status = FireteamStatus.Staging;
                        }
                        else
                        {
                            if ( team.DeepInfo.ShouldFireteamDisbandSoItCanUpgrade( 40 ) )
                            {
                                if ( tracing )
                                    tracingBuffer.Add( "Fireteam " + team.FireTeamID + " was assembling, but is now dispanding for upgrade. That was boring\n" );
                                GameEntity_Squad retreatPoint = team.GetRetreatPoint( AttachedFaction, Context, pathingCacheData );
                                if ( retreatPoint != null )
                                    FireteamUtility.DisbandAndRetreatFireteam( BaseInfo.Teams, team, retreatPoint, AttachedFaction, Context, pathingCacheData );
                                else
                                    FireteamUtility.DisbandFireteam( this.AttachedFaction, BaseInfo.Teams, team, Context );
                                continue;
                            }
                            continue; //we're not strong enough yet
                        }
                    }
                    debugCode = 2200;
                    if ( team.status == FireteamStatus.Escorting )
                    {
                        if ( team.DeepInfo.ShouldFireteamDisbandSoItCanUpgrade( 80 ) )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "Fireteam " + team.FireTeamID + " was escorting, but is now disbanding so it can find upgrade\n" );
                            FireteamUtility.DisbandFireteam( this.AttachedFaction, BaseInfo.Teams, team, Context );
                            continue;
                        }
                        if ( team.Target == null )
                        {
                            team.FindEscortTargetIfPossible( lrp_escortingFireteams, LongRangePlanningBuilders, Context, pathingCacheData ); //attempt to find a target
                            if ( team.Target != null )
                            {
                                team.History.Add( Fireteam.HistoryItem.Create_EscortRelated( Fireteam.HistoryItemType.EscortShip, team.Target.TypeData.GetDisplayName() ) );
                                if ( tracing )
                                    tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is now escorting " + team.Target.ToStringWithPlanet() + "\n" );
                            }
                        }

                        if ( team.Target == null )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "Fireteam " + team.FireTeamID + " could not find anything to escort\n" );
                            continue; //nothing to escort right now, just chill
                        }
                        if ( !team.DeepInfo.CanISafelyGetToEscortPlanet( AttachedFaction, Context, pathingCacheData ) )
                        {
                            //I can no longer get to my escort target. Find new orders
                            if ( tracing )
                                tracingBuffer.Add( "Fireteam " + team.FireTeamID + " could not get to what it wants to escort. Disband\n" );

                            GameEntity_Squad retreatPoint = team.GetRetreatPoint( AttachedFaction, Context, pathingCacheData );
                            team.DeepInfo.DisbandAndRetreat( AttachedFaction, Context, pathingCacheData, retreatPoint );
                            continue;
                        }
                        team.HandleEscortDuties( AttachedFaction, Context, pathingCacheData, tracingBuffer, 5f ); //either go to your target or fight enemies on your target's planet
                        continue;
                    }
                    debugCode = 2210;
                    if ( team.TargetPlanet == null && team.LurkPlanet != null &&
                         !team.DefenseMode )
                    {
                        //a fireteam can have a lurk planet without a target, but not vice versa
                        throw new Exception( "Somehow team " + team.ToString() + " is an offensive fleet and has a lurk planet but no target. This is only valid for defensive fleets" );
                    }
                    if ( team.TargetPlanet == null || (team.LurkStartTime > 0 && World_AIW2.Instance.GameSecond - team.LurkStartTime > 180) )
                    {
                        debugCode = 2210;
                        if ( team.DeepInfo.ShouldFireteamDisbandSoItCanUpgrade( 70 ) )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "Fireteam " + team.FireTeamID + " either didn't have a target or had been lurking for a long time,, but is now dispanding for upgrade.\n" );
                            GameEntity_Squad retreatPoint = team.GetRetreatPoint( AttachedFaction, Context, pathingCacheData );
                            if ( retreatPoint != null )
                                FireteamUtility.DisbandAndRetreatFireteam( BaseInfo.Teams, team, retreatPoint, AttachedFaction, Context, pathingCacheData );
                            else
                                FireteamUtility.DisbandFireteam( this.AttachedFaction, BaseInfo.Teams, team, Context );

                            continue;
                        }

                        TeamsThatNeedTargets.AddIfNotAlreadyIn( team );
                        continue;
                    }

                    debugCode = 2300;
                    if ( team.DefenseMode && team.status == FireteamStatus.Staging &&
                         team.LurkPlanet == null )
                    {
                        //if this is a defensive fleet and it doesn't have anything to do right now, make sure it's lurking on a planet with a valuable target
                        if ( LongRangePlanningInfrastructureUndefendedPlanets.Count > 0 )
                        {
                            //prefer an infrastructure planet without any other defensive fleets
                            Planet targetPlanet = LongRangePlanningInfrastructureUndefendedPlanets[Context.RandomToUse.Next( 0, LongRangePlanningInfrastructureUndefendedPlanets.Count )];
                            team.LurkPlanet = targetPlanet;
                            team.History.Add( Fireteam.HistoryItem.Create_SinglePlanetRelated( Fireteam.HistoryItemType.DefensiveFleetWaitingWithInfrastructure, team.DefenseMode, targetPlanet ) );
                            team.SendFireteamToPlanet( AttachedFaction, targetPlanet, Context, pathingCacheData, tracingBuffer, 10f );
                        }
                        else
                        {
                            //but just pick one at random if everything is defended
                            Planet targetPlanet = LongRangePlanningInfrastructurePlanets[Context.RandomToUse.Next( 0, LongRangePlanningInfrastructurePlanets.Count )];
                            team.LurkPlanet = targetPlanet;
                            team.SendFireteamToPlanet( AttachedFaction, targetPlanet, Context, pathingCacheData, tracingBuffer, 10f );
                            team.History.Add( Fireteam.HistoryItem.Create_SinglePlanetRelated( Fireteam.HistoryItemType.DefensiveFleetWaitingInGeneral, team.DefenseMode, targetPlanet ) );
                        }
                        debugCode = 2310;
                    }

                    debugCode = 2400;
                    if ( team.status == FireteamStatus.Staging && team.LurkPlanet != null )
                    {
                        debugCode = 2405;
                        if ( tracing && tracingBuffer == null )
                            throw new Exception("tracing is enabled but tracing buffer is null?!?!");
                        debugCode = 2406;
                        if ( tracing && team.DeepInfo != null && team.DeepInfo.ShipsInFireteam != null )
                            tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is staging toward  " + team.LurkPlanet.Name + ". It has " + team.DeepInfo.ShipsInFireteam.Count + " ships" );
                        debugCode = 2407;
                        if ( tracing )
                            tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is staging toward  " + team.LurkPlanet.Name + ". It has a null DeepInfo so we can't check how many ships are in it." );

                        debugCode = 2410;

                        if ( team.TargetPlanet != null && Fireteam.IsThisAWinningBattle( AttachedFaction, Context, team.TargetPlanet, 5 ) )
                        {
                            //This is a winning battle (we outnumber the enemy 5 to 1), so no need to keep this target
                            team.DiscardCurrentObjectives();
                            continue;
                        }
                        if ( team.DeepInfo.ShouldFireteamDisbandSoItCanUpgrade( 80 ) )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "Fireteam " + team.FireTeamID + " was staging, but is now dispanding for upgrade\n" );
                            GameEntity_Squad retreatPoint = team.GetRetreatPoint( AttachedFaction, Context, pathingCacheData );
                            if ( retreatPoint != null )
                                FireteamUtility.DisbandAndRetreatFireteam( BaseInfo.Teams, team, retreatPoint, AttachedFaction, Context, pathingCacheData );
                            else
                                FireteamUtility.DisbandFireteam( this.AttachedFaction, BaseInfo.Teams, team, Context );
                            continue;
                        }
                        //Check if the lurk planet is no longer safe; if it's not safe anymore, flee and regroup
                        if ( !team.DeepInfo.IsLurkPlanetSafe( AttachedFaction ) )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "Fireteam" + team.FireTeamID + " no longer has a safe lurk planet; retreat to an armory" );

                            GameEntity_Squad retreatPoint = team.GetRetreatPoint( AttachedFaction, Context, pathingCacheData );
                            if ( retreatPoint != null )
                            {
                                team.History.Add( Fireteam.HistoryItem.Create_DoublePlanetRelated( Fireteam.HistoryItemType.RetreatFromLurkLocation, team.DefenseMode, team.LurkPlanet, retreatPoint.Planet ) );
                                FireteamUtility.DisbandAndRetreatFireteam( BaseInfo.Teams, team, retreatPoint, AttachedFaction, Context, pathingCacheData );
                                continue;
                            }
                        }
                        debugCode = 2430;
                        if ( !team.DeepInfo.CanISafelyGetToLurkPlanet( AttachedFaction, Context, pathingCacheData ) )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "Fireteam <color=#19ffdd>" + team.FireTeamID + "</color> can't safely get to the lurk planet; retreat " );
                            GameEntity_Squad retreatPoint = team.GetRetreatPoint( AttachedFaction, Context, pathingCacheData );
                            if ( retreatPoint != null )
                            {
                                team.History.Add( Fireteam.HistoryItem.Create_DoublePlanetRelated( Fireteam.HistoryItemType.RetreatFromTravelToLurkLocation, team.DefenseMode, team.LurkPlanet, retreatPoint.Planet ) );
                                FireteamUtility.DisbandAndRetreatFireteam( BaseInfo.Teams, team, retreatPoint, AttachedFaction, Context, pathingCacheData );
                                continue;
                            }
                        }
                        team.StageFireteamToLurkPlanet( AttachedFaction, Context, pathingCacheData, tracingBuffer, 10f );
                        if ( team.DeepInfo.CheckIfEnoughUnitsAreLurking() && team.TargetPlanet != null )
                        {
                            team.LurkStartTime = World_AIW2.Instance.GameSecond;
                            team.status = FireteamStatus.ReadyToAttack;
                            if ( tracing )
                                tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is ready to attack. Camp our units nicely on the map" + "\n" );
                            team.History.Add( Fireteam.HistoryItem.Create_DoublePlanetRelated( Fireteam.HistoryItemType.LurkingAgainstAnotherPlanet, team.DefenseMode, team.LurkPlanet, team.TargetPlanet ) );
                            team.CampUnitsOnPlanet( AttachedFaction, Context, 10f );
                        }
                        else
                        {
                            //add staging fireteams here; attacking/ready to attack fireteams are added below
                            if ( TeamsAimedAtPlanet[team.TargetPlanet] == null )
                            {
                                TeamsAimedAtPlanet[team.TargetPlanet] = FireteamRegiment.GetFromPoolOrCreate();
                                TeamsAimedAtPlanet[team.TargetPlanet].TargetPlanet = team.TargetPlanet;
                            }
                            TeamsAimedAtPlanet[team.TargetPlanet].Add( team );
                            TeamsAimedAtPlanet[team.TargetPlanet].TargetPlanet = team.TargetPlanet;
                        }
                    }
                    debugCode = 2500;
                    if ( team.SuicideMission && team.status == FireteamStatus.ReadyToAttack )
                    {
                        team.AttackTargetPlanet( AttachedFaction, Context, pathingCacheData, 2f );
                        team.status = FireteamStatus.Attacking;
                        team.History.Add( Fireteam.HistoryItem.Create_SinglePlanetRelated( Fireteam.HistoryItemType.SuicideAttack, team.DefenseMode, team.TargetPlanet ) );
                    }
                    if ( team.status == FireteamStatus.ReadyToAttack )
                    {
                        debugCode = 2600;
                        int percentUpgradeForDefense = 25;
                        int percentUpgradeForOffense = 50;

                        if ( team.LurkPlanet.GetDataByStanceForFaction( AttachedFaction, FactionStance.Hostile ).StrengthInReinforcementPoints > 0 )
                            FactionUtilityMethods.Instance.FlushUnitsFromReinforcementPoints( team.LurkPlanet, AttachedFaction, Context, 5f );

                        if ( team.DeepInfo.ShouldFireteamRetreatFromCurrentPlanet( AttachedFaction ) )
                        {
                            debugCode = 2610;
                            GameEntity_Squad retreatPoint = team.GetRetreatPoint( AttachedFaction, Context, pathingCacheData );
                            if ( retreatPoint != null )
                            {
                                FireteamUtility.DisbandAndRetreatFireteam( BaseInfo.Teams, team, retreatPoint, AttachedFaction, Context, pathingCacheData );
                                continue;
                            }
                        }
                        else if ( (team.DefenseMode && team.DeepInfo.ShouldFireteamDisbandSoItCanUpgrade( percentUpgradeForDefense ) ||
                                   team.DeepInfo.ShouldFireteamDisbandSoItCanUpgrade( percentUpgradeForOffense )) )
                        {
                            debugCode = 2620;
                            //this fireteam has a lot of units ready to upgrade, so let those units go upgrade,
                            //they will then join a new fireteam
                            if ( tracing )
                                tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is dispanding for upgrade\n" );
                            GameEntity_Squad retreatPoint = team.GetRetreatPoint( AttachedFaction, Context, pathingCacheData );
                            if ( retreatPoint != null )
                                FireteamUtility.DisbandAndRetreatFireteam( BaseInfo.Teams, team, retreatPoint, AttachedFaction, Context, pathingCacheData );
                            else
                                FireteamUtility.DisbandFireteam( this.AttachedFaction, BaseInfo.Teams, team, Context );
                            continue;
                        }
                        else if ( !team.DefenseMode )
                        {
                            //This fireteam is ready to attack but hasn't attacked yet.
                            //Check if there are planets that your allies are fighting on, and go help
                            if ( tracing )
                                tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is checking if any allied battles need help\n" );

                            Planet assistanceTarget = FindAlliesToHelpIfPossible_ScourgeOnly( team, AttachedFaction, Context, pathingCacheData );
                            if ( assistanceTarget != null && assistanceTarget != team.TargetPlanet )
                            {
                                if ( tracing )
                                    tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is being rerouted from " + team.TargetPlanet.Name + " to help allies on " + assistanceTarget.Name + "; status " + team.status + "\n" );
                                team.History.Add( Fireteam.HistoryItem.Create_DoublePlanetRelated( Fireteam.HistoryItemType.LeaveToHelpAllies, team.DefenseMode, team.DeepInfo.CurrentPlanet, assistanceTarget ) );
                                team.TargetPlanet = assistanceTarget;
                            }
                        }
                    }
                    debugCode = 2700;
                    if ( team.status == FireteamStatus.ReadyToAttack ||
                         team.status == FireteamStatus.Attacking )
                    {
                        if ( team.DeepInfo.ShipsInFireteam.Count == 0 ) //all our ships are dead :-(
                        {
                            FireteamUtility.DisbandFireteam( this.AttachedFaction, BaseInfo.Teams, team, Context );
                            continue;
                        }
                        debugCode = 2710;
                        if ( tracing )
                            tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is ready to attack (or attacking) " + team.TargetPlanet.Name + "; status " + team.status );

                        if ( TeamsAimedAtPlanet[team.TargetPlanet] == null )
                        {
                            TeamsAimedAtPlanet[team.TargetPlanet] = FireteamRegiment.GetFromPoolOrCreate();
                            TeamsAimedAtPlanet[team.TargetPlanet].TargetPlanet = team.TargetPlanet;
                        }
                        TeamsAimedAtPlanet[team.TargetPlanet].Add( team );
                        if ( team.DefenseMode )
                            TeamsAimedAtPlanet[team.TargetPlanet].hasDefensiveFleets = true;
                        if ( TeamsAimedAtPlanet[team.TargetPlanet].HighestMarkUnit < team.DeepInfo.HighestMarkUnit )
                            TeamsAimedAtPlanet[team.TargetPlanet].HighestMarkUnit = team.DeepInfo.HighestMarkUnit;
                    }
                }

                debugCode = 3000;
                int weakestStrength = 0;
                KeyValuePair<Planet, FireteamRegiment> weakestAttackingForce = new KeyValuePair<Planet, FireteamRegiment>( null, null );

                foreach ( KeyValuePair<Planet, FireteamRegiment> pair in TeamsAimedAtPlanet )
                {
                    //check over the Teams to do some calculations; first, see if we have a potential
                    //set of fireteams to use as a suicide distraction.
                    //Second, compute how many times mobile enemy defenses are counted

                    //int totalForThisPair = 0;
                    FireteamRegiment regiment = pair.Value;
                    Planet planet = pair.Key;
                    regiment.calculateAvailableStrength( AttachedFaction, FInt.One );

                    if ( weakestStrength < regiment.availableStrength &&
                         !regiment.hasDefensiveFleets )
                        weakestAttackingForce = pair; //the Weakest is used for suicide missions if necessary; if the target isn't hostile then no suicide
                    //int unused = 0;
                    regiment.calculateEnemyStrength( AttachedFaction, planet, Context, pathingCacheData, tracing, tracingBuffer );
                }
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( TeamsThatNeedTargets ) )
                {
                    //this function sets the preferred target and lurk data
                    int targetsForThisTeam = 0;
                    team.GetTargetAndLurkPlanets( AttachedFaction, TeamsAimedAtPlanet, out targetsForThisTeam, Context, pathingCacheData, tracingBuffer );
                    if ( team.TargetPlanet != null )
                    {
                        team.LurkStartTime = -1;
                        team.status = FireteamStatus.Staging;
                        team.History.Add( Fireteam.HistoryItem.Create_TriplePlanetRelated( Fireteam.HistoryItemType.StagingToLurkPlanet, team.DefenseMode, team.DeepInfo.CurrentPlanet, team.TargetPlanet, team.LurkPlanet ) );
                        if ( TeamsAimedAtPlanet[team.TargetPlanet] != null )
                        {
                            TeamsAimedAtPlanet[team.TargetPlanet].Add( team );
                            TeamsAimedAtPlanet[team.TargetPlanet].calculateAvailableStrength( AttachedFaction, FInt.One );
                        }
                    }
                    if ( tracing )
                    {
                        if ( team.TargetPlanet == null )
                            tracingBuffer.Add( "Fireteam " + team.FireTeamID + " could not find a good target. Try again next iteration\n" );
                        else
                            tracingBuffer.Add( "Fireteam " + team.FireTeamID + " now has target on planet  " + team.TargetPlanet.Name + " and lurk on " + team.LurkPlanet.Name + "\n" );
                    }
                }

                foreach ( KeyValuePair<Planet, FireteamRegiment> pair in TeamsAimedAtPlanet )
                {
                    debugCode = 3100;
                    Planet targetPlanet = pair.Key;
                    FireteamRegiment regiment = pair.Value;
                    ArcenLessLinkedList<Fireteam> teams = regiment.teams;
                    if ( teams.GetItemCount() == 0 )
                        continue; //we can have a fireteam regiment with only staging fleets
                    int defensiveEnemyStrength = regiment.mobileEnemyStrength;
                    Fireteam firstTeam = teams.GetFirst().Contained;
                    GameEntity_Squad targetOrNull = firstTeam.Target;
                    bool targetHasForcefield = firstTeam.DeepInfo.TargetProtectedByForcefield;

                    int availableStrength = regiment.availableStrength;
                    int enemyStrength = regiment.netEnemyStrength;

                    //some paranoid checking, debug only
                    foreach ( Fireteam team in Fireteam.LiveTeamsIn( teams ) )
                    {
                        if ( team.status == FireteamStatus.Assembling )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Bug: assembling fireteam found in regiment: " + regiment.ToString(), Verbosity.DoNotShow );
                    }
                    debugCode = 3105;
                    if ( tracing )
                        tracingBuffer.Add( "Regimental Status overview (Scourge -only path) for fireteams targeting " + pair.Key.Name + ". We have some teams with  total strength " + availableStrength + " and net enemy strength " + enemyStrength + " total enemy strength " + regiment.totalEnemyStrength + ". danger of path only: " + regiment.dangerOfPathOnly + ". Team 0 is in state " + firstTeam.status + "\n" );


                    if ( regiment.availableStrength < regiment.dangerOfPathOnly )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\tSkipped; it's too dangerous to go there" );
                        continue;
                    }

                    if ( regiment.HasWonBattle( AttachedFaction ) )
                    {
                        debugCode = 3200;
                        //We won! disband so we can upgrade/attack other things
                        if ( tracing )
                            tracingBuffer.Add( "Fireteams attacking " + pair.Key.Name + " are victorious!" );
                        foreach ( Fireteam team in Fireteam.LiveTeamsIn( teams ) )
                        {
                            debugCode = 3300;
                            if ( !team.SuicideMission && team.DeepInfo.TeamStrength > AttachedFaction.MinFireteamStrength &&
                                 !team.DeepInfo.ShouldFireteamDisbandSoItCanUpgrade( 25 ) )
                            {
                                //this fireteam can be reused for new duties
                                team.DiscardCurrentObjectives();
                            }
                            else if ( team.DeepInfo.ShouldFireteamDisbandSoItCanUpgrade( 25 ) )
                            {
                                GameEntity_Squad retreatPoint = team.GetRetreatPoint( AttachedFaction, Context, pathingCacheData );
                                if ( retreatPoint != null )
                                    FireteamUtility.DisbandAndRetreatFireteam( BaseInfo.Teams, team, retreatPoint, AttachedFaction, Context, pathingCacheData );
                                else
                                    FireteamUtility.DisbandFireteam( this.AttachedFaction, BaseInfo.Teams, team, Context );
                            }
                            else
                                FireteamUtility.DisbandFireteam( this.AttachedFaction, BaseInfo.Teams, team, Context );
                        }
                        continue;
                    }
                    else if ( availableStrength >= enemyStrength + regiment.dangerOfPathOnly / 10 )
                    {
                        debugCode = 3500;
                        //Generic attack path
                        debugCode = 3200;
                        //attack!
                        bool AttackAlreadyStarted = false;

                        foreach ( Fireteam team in Fireteam.LiveTeamsIn( teams ) )
                        {
                            debugCode = 3210;
                            if ( team.status == FireteamStatus.Attacking )
                                AttackAlreadyStarted = false;
                            team.AttackTargetPlanet( AttachedFaction, Context, pathingCacheData, 5f );
                            debugCode = 3220;
                            if ( team.status != FireteamStatus.Attacking )
                            {
                                debugCode = 3230;
                                team.status = FireteamStatus.Attacking;
                                Fireteam.HistoryItemType history = Fireteam.HistoryItemType.AttackFromPlanetToPlanet;
                                if ( team.DefenseMode )
                                    history = Fireteam.HistoryItemType.DefensiveFleetAttackFromPlanetToPlanet;
                                team.History.Add( Fireteam.HistoryItem.Create_DoublePlanetRelated( history, team.DefenseMode, team.DeepInfo.CurrentPlanet, team.TargetPlanet ) );
                                team.LurkStartTime = -1;
                                debugCode = 3240;
                            }
                        }

                        int percentSuicideHelp = 30;
                        if ( !this.BaseInfo.PlayerAllied &&
                             !AttackAlreadyStarted &&
                             TeamsAimedAtPlanet.Count > 4 &&
                             Context.RandomToUse.Next( 0, 100 ) < percentSuicideHelp && weakestAttackingForce.Value != null &&
                             targetPlanet.GetControllingFaction().GetIsHostileTowards( AttachedFaction ) )
                        {
                            //if we have a bunch of fleets, sometimes send some weak ships as an attack as a distraction
                            //don't run this if we are in suicide mode, since then we'd wind up sending in every attack
                            debugCode = 3250;
                            foreach ( Fireteam team in Fireteam.LiveTeamsIn( weakestAttackingForce.Value.teams ) )
                            {
                                if ( tracing )
                                    tracingBuffer.Add( "Fireteam " + team.FireTeamID + " is sent on distracting suicide mission" + "\n" );
                                team.SuicideMission = true;
                            }
                        }
                        if ( regiment.totalEnemyStrength < availableStrength / 3 )
                        {
                            debugCode = 3400;
                            if ( tracing )
                            {
                                if ( targetOrNull == null )
                                    tracingBuffer.Add( "tachyon blast " + pair.Key.Name + " if necessary. target is null. defensive strength " + regiment.totalEnemyStrength + " attacking strength " + availableStrength + "\n" );
                                else
                                    tracingBuffer.Add( "tachyon blast " + pair.Key.Name + " if necessary. target is " + targetOrNull.ToStringWithPlanet() + "\n" );
                            }

                            //if we are already massively winning, decloak cloaked enemies
                            FactionUtilityMethods.Instance.TachyonBlastPlanet( targetPlanet, AttachedFaction, Context );
                            if ( regiment.EnemyStrengthAllInReinforcementPoints )
                                FactionUtilityMethods.Instance.FlushUnitsFromReinforcementPoints( targetPlanet, AttachedFaction, Context, 5f );
                        }
                    }
                    else if ( !targetHasForcefield && !this.BaseInfo.PlayerAllied )
                    {
                        debugCode = 3600;
                        //if there is no forcefield protecting the target, a cloaked fireteam can suicide in to take it out
                        foreach ( Fireteam team in Fireteam.LiveTeamsIn( teams ) )
                        {
                            if ( team.CloakedOnly )
                            {
                                team.SuicideMission = true;
                                team.AttackTargetPlanet( AttachedFaction, Context, pathingCacheData, 5f );
                                team.status = FireteamStatus.Attacking;
                                team.LurkStartTime = -1;
                            }
                        }
                    }
                    else if ( availableStrength * 1.5f <= enemyStrength * 1.0f )
                    {
                        //lets wait till we're a bit more outnumbered to retreat (if indeed we are attacking)
                        debugCode = 3700;
                        foreach ( Fireteam team in Fireteam.LiveTeamsIn( teams ) )
                        {
                            debugCode = 3710;
                            if ( team == null ) //a dead fireteam might have already been disbanded
                                continue;
                            debugCode = 3720;
                            if ( availableStrength * 2 < enemyStrength &&
                                team.status == FireteamStatus.Attacking &&
                                !team.SuicideMission )
                            {
                                debugCode = 3730;
                                GameEntity_Squad retreatPoint = team.GetRetreatPoint( AttachedFaction, Context, pathingCacheData );
                                if ( retreatPoint != null )
                                    FireteamUtility.DisbandAndRetreatFireteam( BaseInfo.Teams, team, retreatPoint, AttachedFaction, Context, pathingCacheData );
                            }
                        }
                    }
                    //otherwise, chill; keep fighting, keep waiting, etc...
                }
                debugCode = 4000;
                for ( int i = 0; i < UnassignedWarriors.Count; i++ )
                {
                    GameEntity_Squad entity = UnassignedWarriors[i].GetSquad();
                    if ( entity == null )
                        continue;
                    debugCode = 4100;
                    //we need to have all the warrior locations so we can calculate which teams are nearby
                    ScourgePerUnitBaseInfo data = entity.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                    ParanoicallyCheckFireteamUnits( AttachedFaction, entity, data, Context, "E" );
                    AssignUnitToFireteam_ScourgeOnly( AttachedFaction, entity, data, Context, pathingCacheData );
                }
                if ( AttachedFaction.NumFireteams != BaseInfo.Teams.GetItemCount() )
                    AttachedFaction.NumFireteams = BaseInfo.Teams.GetItemCount();
                FireteamUtility.RemoveCompletedMissions( AttachedFaction, Context );
                for (int i = 0; i < AutoDefendingShipsLRP.Count; i++ )
                {
                    /* This is a very simple implementation. There's a ton of fancy things we could do, like 
                    being able to do multiple passes to find categories of battles (defensive/offensive), and prioritizing defense (or offense).
                    Another good improvement would be being able to make more informed choices that aren't "Just go to the closest battle".
                    Another good improvement would be letting fleets go to "threatened" planets (ie planets with incoming waves, or lots of hostile mobile forces nearby)

                   For that matter, there's no principled reason we couln't give a "Offensive" mode, where we could kill Instigator bases,
                   AIP reducers, or just neutering enemy planets.
                     */
                    GameEntity_Squad defenseShip = AutoDefendingShipsLRP[i].GetSquad();
                    if ( defenseShip == null )
                        continue;
                
                    ScourgePerUnitBaseInfo unitData = defenseShip.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                    
                    Planet dest = defenseShip.GetDestinationPlanet();
                    bool goToHomeStructure = false;
                    debugCode = 1500;
                    int myStrength;
                    myStrength = defenseShip.GetStrengthPerSquad();

                    debugCode = 1400;


                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("Doing LRP update for " + defenseShip.ToStringWithPlanet(), Verbosity.DoNotShow );

                    debugCode = 1600;
                    if (AutoDefendUtility.IsLosingBattle(defenseShip.Planet, myStrength, this.AttachedFaction ))
                    {
                        debugCode = 1700;
                        if ( defenseShip.TypeData.AllowedHopsFromCenterpiece > -1 )
                            goToHomeStructure = true;
                    }
                    GameEntity_Squad homeStructure = null;
                    if ( unitData != null &&
                         defenseShip.TypeData.AllowedHopsFromCenterpiece > -1 )
                    {
                        homeStructure = unitData.HomeDefensiveStructure.GetSquad();
                        if ( homeStructure == null )
                        {
                            continue;
                        }
                        if ( homeStructure.Planet.GetHopsTo( defenseShip.Planet ) > defenseShip.TypeData.AllowedHopsFromCenterpiece )
                            goToHomeStructure = true;
                    }
                    debugCode = 1900;
                    
                    if (goToHomeStructure && homeStructure != null )
                    {
                        Planet strongholdPlanet = homeStructure.Planet;
                        if (strongholdPlanet != null)
                        {
                            if (debug)
                                ArcenDebugging.ArcenDebugLogSingleLine("\tHeading to " + strongholdPlanet.Name + ", its home stronghold", Verbosity.DoNotShow);

                            AutoDefendUtility.GoToPlanet(defenseShip, strongholdPlanet, Context, pathingCacheData);
                        }
                    }

                    if ( defenseShip.GetIsCrippled() )
                        continue;
                    debugCode = 2100;
                    if ( AutoDefendUtility.IsPlanetUnderAttack(dest, this.AttachedFaction) )
                    {
                        debugCode = 2200;
                        if (debug)
                        {
                            if ( dest == defenseShip.Planet )
                                ArcenDebugging.ArcenDebugLogSingleLine("\tWe are already in a battle!", Verbosity.DoNotShow);
                            else
                                ArcenDebugging.ArcenDebugLogSingleLine("\tWe are en route to a battle at " + dest.Name, Verbosity.DoNotShow);
                        }

                        continue;
                    }
                    debugCode = 2300;
                    //Check if we have any battles to go to!
                    WorkingPlanetsLRP.Clear();
                    if ( homeStructure != null ) 
                        AutoDefendUtility.GetPlanetsUnderAttack( defenseShip, myStrength, homeStructure.Planet, WorkingPlanetsLRP); //we are defending around a specific stronghold; Guardians
                    else
                        AutoDefendUtility.GetPlanetsUnderAttack(defenseShip, myStrength, defenseShip.Planet, WorkingPlanetsLRP); //we are "generic defense"; defensive flagships
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\tWe have found " + WorkingPlanetsLRP.Count + " planets under attack", Verbosity.DoNotShow );
                    debugCode = 2400;
                    if ( WorkingPlanetsLRP.Count > 0 )
                    {
                        debugCode = 2500;
                        //The target's priority is chosen in GetPlanetsUnderAttack (so technically we could just pass back a single planet), but we use a List for future-proofing
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tWe are heading to " + WorkingPlanetsLRP[0].Name + " to help defend", Verbosity.DoNotShow );

                        AutoDefendUtility.GoToPlanet( defenseShip, WorkingPlanetsLRP[0], Context, pathingCacheData);
                        continue;
                    }
                    debugCode = 2600;
                    //We haven't found an ongoing battle to assist in, so see if we have a threatened planet we can defend
                    WorkingPlanetsLRP.Clear();
                    if ( AutoDefendUtility.IsPlanetThreatened( dest, this.AttachedFaction ))
                    {
                        debugCode = 2700;
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tWe are already on a threatened planet!", Verbosity.DoNotShow );
                        continue;
                    }
                    AutoDefendUtility.GetThreatenedPlanets( myStrength, defenseShip.Planet, WorkingPlanetsLRP, this.AttachedFaction);
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("\tWe have found " + WorkingPlanetsLRP.Count + " planets threatened", Verbosity.DoNotShow );
                    debugCode = 2800;
                    if ( WorkingPlanetsLRP.Count > 0 )
                    {
                        Planet threatenedPlanet = WorkingPlanetsLRP[Context.RandomToUse.Next(0, WorkingPlanetsLRP.Count)];
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("\tWe are heading to " + threatenedPlanet.Name + " because the planet is threatened", Verbosity.DoNotShow );
                    
                        AutoDefendUtility.GoToPlanet( defenseShip, threatenedPlanet, Context, pathingCacheData);
                        continue;
                    }
                    debugCode = 2900;
                    if ( dest != defenseShip.Planet )
                        continue; //we don't have any more urgent objectives, and we are en route someplace
                    if ( dest == defenseShip.Planet )
                    {
                        if ( defenseShip.HasQueuedOrders() )
                            continue;
                        debugCode = 3000;
                        //Patrolling: head to a randomly chosen other shipyard, or someplace on this planet
                        if ( Context.RandomToUse.Next(0, 100) < 20 )
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine("\tWe are heading to part of of " + defenseShip.Planet.Name + " to patrol", Verbosity.DoNotShow );
                            FInt minRadius = FInt.FromParts( 0, 100 );
                            FInt maxRadius = FInt.FromParts( 0, 900 );

                            ArcenPoint patrolPoint = defenseShip.Planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, defenseShip.TypeData, Engine_AIW2.Instance.CombatCenter, minRadius, maxRadius );
                            GoToLocation(defenseShip, patrolPoint, Context, pathingCacheData);
                            continue;
                        }
                        Planet shipyardPlanet = AutoDefendUtility.GetRandomPlanetForPatrol(defenseShip, LongRangePlanningSpawners, WorkingPlanetsLRP, Context, pathingCacheData);
                        if (shipyardPlanet != null)
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine("\tWe are heading to " + shipyardPlanet.Name + " to patrol", Verbosity.DoNotShow );

                            AutoDefendUtility.GoToPlanet(defenseShip, shipyardPlanet, Context, pathingCacheData);
                            continue;
                        }
                    }
                }
            }
            catch ( ArcenPleaseStopThisThreadException )
            {
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Scourge Long Range Planning Error " + AttachedFaction.FactionIndex + " playerAllied " + this.BaseInfo.PlayerAllied + " hit debugCode " + debugCode + ": " + e.ToString(), Verbosity.ShowAsError );
                if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( "traces generated up until exception was hit: " + tracingBuffer.ToString(), Verbosity.DoNotShow );
            }
            finally
            {
                pathingCacheData.ReturnToPool();
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( AttachedFaction.FactionIndex + " DoLongRangePlanning trace ends" );
                if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
            }
        }
        public override GameEntity_Squad GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( Planet CurrentPlanetForFireteam, 
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( LongRangePlanningArmories == null && LongRangePlanningSpawners == null )
                return null;
            List<SafeSquadWrapper> WorkingList = GameEntity_Squad.GetTemporarySquadList( "Scourge-GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass-WorkingList", 10f );
            if ( WorkingList == null ) //blocked for teardown/shutdown; bail
                return null;
            if ( LongRangePlanningArmories != null )
                WorkingList.AddRange( LongRangePlanningArmories );
            if ( LongRangePlanningSpawners != null )
                WorkingList.AddRange( LongRangePlanningSpawners );
            if ( WorkingList.Count == 0 )
            {
                GameEntity_Squad.ReleaseTemporarySquadList( WorkingList );
                return null;
            }
            GameEntity_Squad safestRetreatPoint = null;
            int dangerOfSafest = 99999999;
            for ( int i = 0; i < WorkingList.Count; i++ )
            {
                Int16 hops = 0;
                int danger = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForFireteam, WorkingList[i].Planet, true, out hops );
                if ( safestRetreatPoint == null || dangerOfSafest > danger )
                {
                    safestRetreatPoint = WorkingList[i].GetSquad();
                    if ( safestRetreatPoint == null )
                        continue;
                    dangerOfSafest = danger;
                }
                if ( danger == 0 )
                    break;
            }
            GameEntity_Squad.ReleaseTemporarySquadList( WorkingList );
            return safestRetreatPoint;
        }
        private void PrecalculatePlanetsUnderAttackByAllies_ScourgeOnly( Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            PlanetsBeingAttackedByAllies_ScourgeOnly.Clear();
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Checking for planets under attack by allies", Verbosity.DoNotShow );
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.GetControllingFaction().GetIsFriendlyTowards( faction ) )
                    continue;
                EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( faction );
                int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                int alliedStrength = myFactionData[FactionStance.Friendly].TotalStrength;
                if ( alliedStrength == 0 )
                    continue;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( planet.Name + " has some allied forces", Verbosity.DoNotShow );

                if ( hostileStrength < 2000 )
                    continue;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "\t" + planet.Name + " has some allied forces and some hostile forces. " + alliedStrength + ", hostile " + hostileStrength, Verbosity.DoNotShow );

                FInt closeFightMultiplier = FInt.FromParts( 1, 250 );
                if ( !this.BaseInfo.PlayerAllied )
                    closeFightMultiplier = FInt.FromParts( 0, 750 ); //ai or minor faction allied scourge are more cowardly

                if ( (alliedStrength * closeFightMultiplier).IntValue >= hostileStrength )
                {
                    PlanetsBeingAttackedByAllies_ScourgeOnly.Add( planet );
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "\t\t" + planet.Name + " is eligible", Verbosity.DoNotShow );
                }
            }
        }
        public Planet FindAlliesToHelpIfPossible_ScourgeOnly( Fireteam team, Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            Planet currentPlanet = team.LurkPlanet;
            if ( currentPlanet == null )
                return null;
            int maxHops = 5;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Scourge-FindAlliesToHelpIfPossible_ScourgeOnly-trace", 10f ) : null;
            if ( tracing )
                ArcenDebugging.ArcenDebugLogSingleLine( "Checking " + PlanetsBeingAttackedByAllies_ScourgeOnly.Count + " planets with allied combat\n", Verbosity.DoNotShow );
            for ( int i = 0; i < PlanetsBeingAttackedByAllies_ScourgeOnly.Count; i++ )
            {
                Planet potentialPlanet = PlanetsBeingAttackedByAllies_ScourgeOnly[i];
                if ( potentialPlanet.GetHopsTo( team.LurkPlanet ) > maxHops )
                    continue;
                Int16 hops = 0;
                int danger = Fireteam.GetDangerOfPath( faction, Context, PathCacheData, currentPlanet, potentialPlanet, false, out hops );
                if ( danger < team.DeepInfo.TeamStrength )
                    return potentialPlanet;
            }
            return null;
        }
        public bool SendUnitToCarryOutMissionIsPossible( Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData,
                                                         GameEntity_Squad builder, ScourgePerUnitBaseInfo data )
        {
            int debugCode = 0;
            try{
                debugCode = 100;
                VassalMission mission = data.MyMission;
                if ( ( mission.TypeDataToBuild != null && mission.TypeDataToBuild.GetHasTag( "ScourgeArmory " ) ) ||
                     mission.TagToBuild == "ScourgeArmory" )
                {
                    if ( data.StoredMetal < BaseInfo.Difficulty.MetalCostForBuildingArmory )
                        return false;
                }
                if ( ( mission.TypeDataToBuild != null && mission.TypeDataToBuild.GetHasTag( "ScourgeSpawner " ) ) ||
                     mission.TagToBuild == "ScourgeSpawner" )
                {
                    if ( data.StoredMetal < BaseInfo.Difficulty.MetalCostForBuildingSpawner )
                        return false;
                }

                if ( ( mission.TypeDataToBuild != null && mission.TypeDataToBuild.GetHasTag( "ScourgeArmory " ) ) ||
                     mission.TagToBuild == "ScourgeArmory" )
                {
                    if ( data.StoredMetal < BaseInfo.Difficulty.MetalCostForBuildingArmory )
                        return false;
                }


                Planet dest = mission.Planet;
                Int16 hops;
                int danger = Fireteam.GetDangerOfPath( this.AttachedFaction, Context, PathCacheData, dest, builder.Planet, true, out hops );
                if ( danger > 10 * 1000 )
                    return false; //too dangerous to get there!
                PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "ScourgeSendUnitToRandomBuilding", builder.Planet, dest, PathingMode.Safest, Context, PathCacheData );
                if ( pathCache == null || pathCache.PathToReadOnly.Count == 0 )
                    return false;

                debugCode = 400;
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleSpecialMission], GameCommandSource.AnythingElse );
                command.RelatedString = "Scrg_BuilderMission";
                command.RelatedEntityIDs.Add( builder.PrimaryKeyID );
                for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                    command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );

                return true;
            }
            catch(Exception e) {  ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in SendUnitToRandomBuilding at debugCode " + debugCode + " Exception: " + e, Verbosity.DoNotShow ); }
            return false;
        }

        public bool SendUnitToRandomBuilding( Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData,
                                                         GameEntity_Squad builder )
        {
            int debugCode = 0;
            try{
                debugCode = 100;
                GameEntity_Squad dest = null;
                if ( LongRangePlanningSpawners.Count > 0 )
                    dest = LongRangePlanningSpawners[Context.RandomToUse.Next(0, LongRangePlanningSpawners.Count)].GetSquad();
                else if ( LongRangePlanningArmories.Count > 0 )
                    dest = LongRangePlanningArmories[Context.RandomToUse.Next(0, LongRangePlanningArmories.Count)].GetSquad();
                debugCode = 200;
                if ( dest == null )
                    return false;
                debugCode = 300;
                PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "ScourgeSendUnitToRandomBuilding", builder.Planet, dest.Planet, PathingMode.Safest, Context, PathCacheData );
                if ( pathCache == null || pathCache.PathToReadOnly.Count == 0 )
                    return false;
                debugCode = 400;
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleSpecialMission], GameCommandSource.AnythingElse );
                command.RelatedString = "Scrg_BuilderFlee";
                command.RelatedEntityIDs.Add( builder.PrimaryKeyID );
                for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                    command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                
                return true;
            }
            catch(Exception e) {  ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in SendUnitToRandomBuilding at debugCode " + debugCode + " Exception: " + e, Verbosity.DoNotShow ); }
            return false;
        }

        public bool SendUnitToUpgradeStructureIfPossible( Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData,
                                                         GameEntity_Squad builder, bool MustBeOnThisPlanet, GameEntity_Squad unitToUpgrade = null )
        {
            if ( builder == null )
                return false;

            int debugCode = 0;
            List<SafeSquadWrapper> WorkingUpgradeList = GameEntity_Squad.GetTemporarySquadList( "Scourge-SendUnitToUpgradeStructureIfPossible-WorkingUpgradeList", 10f );
            if ( WorkingUpgradeList == null ) //blocked for teardown/shutdown; bail
                return false;
            try
            {
                debugCode = 100;
                bool tracing = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
                ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Scourge-SendUnitToUpgradeStructureIfPossible-trace", 10f ) : null;
                debugCode = 100;
                ScourgePerUnitBaseInfo builderData = builder.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                if ( tracing )
                    tracingBuffer.Add( "\t checking whether to send  " + builder.ToStringWithPlanet() + " metal " + builderData.StoredMetal ).Add( " to upgrade a structure\n" );
                debugCode = 200;
                //quick shortcut if we're very low on metal
                if ( builderData.StoredMetal < BaseInfo.Difficulty.MetalCostIncreaseForUpgradingArmoriesPerLevel &&
                     builderData.StoredMetal < BaseInfo.Difficulty.MetalCostIncreaseForUpgradingSpawnersPerLevel )
                {
                    if ( tracing )
                        tracingBuffer.Add( "\t\tInsufficient metal (base)\n" );
                    #region Tracing
                    if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion

                    return false;
                }
                debugCode = 500;

                //The logic for Armories and Spawners is basically identical at the moment
                //Note that we prefer to upgrade spawners, since armories tend to get upgraded when the builders need to upgrade themselves
                GameEntity_Squad destination = null;
                bool includeDestinationInDangerCalculation = true;
                for ( int i = 0; i < LongRangePlanningSpawners.Count; i++ )
                {
                    debugCode = 600;
                    GameEntity_Squad spawner = LongRangePlanningSpawners[i].GetSquad();
                    if ( spawner == null )
                        continue;
                    debugCode = 700;
                    Int16 hops = 0;
                    if ( builder.Planet != spawner.Planet && MustBeOnThisPlanet )
                        continue;
                    else if ( !MustBeOnThisPlanet && Fireteam.GetDangerOfPath( faction, Context, PathCacheData, builder.Planet, spawner.Planet, includeDestinationInDangerCalculation, out hops ) > 500 )
                        continue;
                    debugCode = 800;
                    if ( builder.CurrentMarkLevel < spawner.CurrentMarkLevel ||
                         spawner.CurrentMarkLevel >= 7 )
                        continue;
                    debugCode = 900;
                    ScourgePerUnitBaseInfo spawnerData = spawner.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                    int UpgradeCost = spawner.CurrentMarkLevel * BaseInfo.Difficulty.MetalCostIncreaseForUpgradingSpawnersPerLevel;
                    debugCode = 1000;
                    if ( spawnerData == null )
                        throw new Exception("Could not get ScourgePerUnitBaseInfo for " + spawner.ToStringWithPlanetAndOwner() );
                    if ( spawnerData.Experience < spawnerData.ExperienceForNextLevel ||
                         builderData.StoredMetal < UpgradeCost )
                        continue;
                    debugCode = 1200;
                    WorkingUpgradeList.Add( spawner );
                }
                debugCode = 2000;
                if ( WorkingUpgradeList.Count > 0 && Context.RandomToUse.Next( 0, 100 ) < 70 ) //decide whether to upgrade a spawner; prefer this
                    destination = WorkingUpgradeList[Context.RandomToUse.Next( 0, WorkingUpgradeList.Count )].GetSquad();

                debugCode = 3000;
                for ( int i = 0; i < LongRangePlanningArmories.Count; i++ )
                {
                    if ( destination != null )
                        break;

                    debugCode = 3000;
                    GameEntity_Squad armory = LongRangePlanningArmories[i].GetSquad();
                    if ( armory == null )
                        continue;
                    Int16 hops = 0;
                    if ( builder.Planet != armory.Planet && MustBeOnThisPlanet )
                        continue;
                    else if ( !MustBeOnThisPlanet && Fireteam.GetDangerOfPath( faction, Context, PathCacheData, builder.Planet, armory.Planet, true, out hops ) > 500 )
                        continue;

                    debugCode = 4000;
                    ScourgePerUnitBaseInfo armoryData = armory.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                    if ( armoryData.Experience < armoryData.ExperienceForNextLevel )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\tSkipping " + armory.ToStringWithPlanet() + " not enough exp\n" );

                        continue;
                    }
                    if ( builder.CurrentMarkLevel < armory.CurrentMarkLevel )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\tSkipping " + armory.ToStringWithPlanet() + " low level builder " + builder.CurrentMarkLevel + " < " + armory.CurrentMarkLevel + "\n" );
                        continue;
                    }

                    debugCode = 5000;
                    if ( armoryData.Experience < armoryData.ExperienceForNextLevel ||
                         armory.CurrentMarkLevel >= 7 )
                        continue;

                    debugCode = 6000;
                    int UpgradeCost = armory.CurrentMarkLevel * BaseInfo.Difficulty.MetalCostIncreaseForUpgradingArmoriesPerLevel;
                    if ( builderData.StoredMetal < UpgradeCost )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\t Insufficient Metal:" + armory.ToStringWithPlanet() + " upgrade cost " + UpgradeCost + "\n" );

                        continue;
                    }

                    WorkingUpgradeList.Add( armory );
                    break;
                }
                if ( WorkingUpgradeList.Count > 0 ) //decide whether to upgrade an armory
                    destination = WorkingUpgradeList[Context.RandomToUse.Next( 0, WorkingUpgradeList.Count )].GetSquad();

                debugCode = 12000;
                for ( int i = 0; i < LongRangePlanningFortresses.Count; i++ )
                {
                    if ( destination != null )
                        break;

                    debugCode = 13000;
                    GameEntity_Squad fortress = LongRangePlanningFortresses[i].GetSquad();
                    if ( fortress == null )
                        continue;
                    Int16 hops = 0;
                    if ( fortress.CurrentMarkLevel > 3 )
                        continue; //don't upgrade fortresses too high on this path; otherwise we never upgrade the infrastructure
                    debugCode = 14000;
                    if ( builder.Planet != fortress.Planet && MustBeOnThisPlanet )
                        continue;
                    else if ( !MustBeOnThisPlanet && Fireteam.GetDangerOfPath( faction, Context, PathCacheData, builder.Planet, fortress.Planet, true, out hops ) > 500 )
                        continue;

                    debugCode = 15000;
                    if ( builder.CurrentMarkLevel <= fortress.CurrentMarkLevel ) //fortresses need the builder to be at a higher mark level, to help force early armory upgrades
                        continue;

                    debugCode = 16000;
                    ScourgePerUnitBaseInfo fortressData = fortress.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                    if ( fortressData.Experience < fortressData.ExperienceForNextLevel ||
                         fortress.CurrentMarkLevel >= BaseInfo.MaxFortressLevel )
                        continue;

                    debugCode = 17000;
                    int UpgradeCost = fortress.CurrentMarkLevel * BaseInfo.Difficulty.MetalCostIncreaseForUpgradingFortressPerLevel;
                    if ( fortressData.Experience < fortressData.ExperienceForNextLevel ||
                         builderData.StoredMetal < UpgradeCost )
                        continue;
                    destination = fortress;
                    break;
                }

                debugCode = 21000;
                if ( destination == null )
                {
                    #region Tracing
                    if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion

                    return false;
                }
                if ( tracing )
                    tracingBuffer.Add( builder.ToStringWithPlanet() + " is dispatched to upgrade " + destination.ToStringWithPlanet() ).Add( "\n" );
                debugCode = 22000;
                if ( destination.Planet == builder.Planet )
                {
                    debugCode = 23000;
                    //We have the metal and the structure has the experience, so fly to it so we can upgrade it
                    GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse );
                    moveCommand.PlanetOrderWasIssuedFrom = builder.Planet.Index;
                    moveCommand.RelatedPoints.Add( destination.WorldLocation );
                    moveCommand.RelatedEntityIDs.Add( builder.PrimaryKeyID );
                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );
                }
                else
                {
                    debugCode = 24000;
                    //fly to destination planet
                    PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "ScourgeSendUnitToUpgradeStructureIfPossible", builder.Planet, destination.Planet, PathingMode.Safest, Context, PathCacheData );
                    if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                    {
                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleSpecialMission], GameCommandSource.AnythingElse );
                        command.RelatedString = "Scrg_GoUpgrade";
                        command.RelatedEntityIDs.Add( builder.PrimaryKeyID );
                        for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                            command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                    }
                }
                debugCode = 25000;
                unitToUpgrade = destination;
                #region Tracing
                if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion

                return true;
            }
            catch ( ArcenPleaseStopThisThreadException )
            {
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "SendUnitToUpgradeStructureIfPossible for Scourge hit debugCode " + debugCode + ": " + e.ToString(), Verbosity.ShowAsError );
            }
            finally
            {
                GameEntity_Squad.ReleaseTemporarySquadList( WorkingUpgradeList );
            }
            return false;
        }

        private void SendUnitToSpecificPlanet( Faction faction, GameEntity_Squad builder, Planet planet, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "ScourgeSendUnitToSpecificPlanet", builder.Planet, planet, PathingMode.Safest, Context, PathCacheData );
            if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
            {
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCDirectedMob], GameCommandSource.AnythingElse );
                command.RelatedString = "Scrg_GotoPlanet";
                command.RelatedEntityIDs.Add( builder.PrimaryKeyID );
                for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                    command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
            }
        }

        public bool SendUnitToArmoryToUpgradeIfNecessary( Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData,
                                                         GameEntity_Squad entity, ScourgePerUnitBaseInfo data )
        {

            if ( entity == null )
                return false;

            bool tracing = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Scourge-SendUnitToArmoryToUpgradeIfNecessary-trace", 10f ) : null;
            int debugCode = 0;
            List<SafeSquadWrapper> workingArmoryList = GameEntity_Squad.GetTemporarySquadList( "Scourge-SendUnitToArmoryToUpgradeIfNecessary-workingArmoryList", 10f );
            if ( workingArmoryList == null ) //blocked for teardown/shutdown; bail
                return false;
            List<SafeSquadWrapper> hybridizeArmoryList = GameEntity_Squad.GetTemporarySquadList( "Scourge-SendUnitToArmoryToUpgradeIfNecessary-hybridizeArmoryList", 10f );
            if ( hybridizeArmoryList == null ) //blocked for teardown/shutdown; bail
            {
                GameEntity_Squad.ReleaseTemporarySquadList( workingArmoryList );
                return false;
            }
            try
            {
                debugCode = 100;
                if ( BaseInfo.ArmoriesInGalaxy.Count == 0 )
                {
                    if ( tracing )
                        tracingBuffer.ReturnToPool();

                    return false;
                }
                debugCode = 200;
                if ( data.Experience < data.ExperienceForNextLevel ||
                     entity.CurrentMarkLevel >= 7 )
                {
                    if ( tracing )
                        tracingBuffer.ReturnToPool();

                    return false;
                }
                GameEntity_Squad armory = null;
                //Armories can only upgrade units up to the mark level of the armory.
                //Also only choose "safe" armories
                debugCode = 300;
                for ( int i = 0; i < LongRangePlanningArmories.Count; i++ )
                {
                    debugCode = 1000;
                    armory = LongRangePlanningArmories[i].GetSquad();
                    if ( armory == null )
                        continue;
                    if ( armory.CurrentMarkLevel <= entity.CurrentMarkLevel )
                        continue;

                    if ( armory.Planet == entity.Planet )
                    {
                        workingArmoryList.Clear();
                        workingArmoryList.Add( armory );
                        break;
                    }
                    debugCode = 1100;
                    Int16 hops = 0;
                    if ( Fireteam.GetDangerOfPath( faction, Context, PathCacheData, entity.Planet, armory.Planet, true, out hops ) < entity.GetStrengthOfSelfAndContents() )
                    {
                        debugCode = 1200;
                        ScourgePerUnitBaseInfo armorydata = armory.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                        if ( armorydata == null )
                            continue;
                        ScourgeTypeData scourgeTypeData = ScourgeTypeDataTable.Instance.GetRowById( armorydata.ScourgeTypeId );
                        if ( scourgeTypeData == null )
                            continue; //This doesn't seem have been initialized; perhaps it was just created?
                        workingArmoryList.Add( armory );
                        debugCode = 1300;
                        if ( scourgeTypeData.DoesThisArmoryUpgradeThisUnit( entity ) &&
                             entity.CurrentMarkLevel >= BaseInfo.Difficulty.RequiredLevelForWarriorsToHybridize &&
                             armory.CurrentMarkLevel >= BaseInfo.Difficulty.RequiredLevelForArmoriesToHybridizeWarriors &&
                             data.IsEvolved && !data.IsHybrid && CanBuildThisHybrid( armorydata ))
                            hybridizeArmoryList.Add( armory ); //if this warrior goes to this armory then it will become a hybrid
                    }
                }
                if ( workingArmoryList.Count == 0 ) //no armories for us to get to
                {
                    #region Tracing
                    if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion

                    return false;
                }
                debugCode = 1400;
                if ( entity.TypeData.GetHasTag( "ScourgeSubjugator" ) )
                {
                    debugCode = 1500;
                    //always go to a very strong armory
                    workingArmoryList.Sort( static delegate ( SafeSquadWrapper Left, SafeSquadWrapper Right )
                    {
                        return Left.CurrentMarkLevel.CompareTo( Right.CurrentMarkLevel );
                    } );
                    debugCode = 1600;
                    armory = workingArmoryList[0].GetSquad();
                }
                else if ( hybridizeArmoryList.Count > 0 && //if we can hybridize then see if we want to
                     Context.RandomToUse.Next( 0, 100 ) < BaseInfo.Difficulty.PercentForceWarriorToHybridizeIfPossible )
                {
                    debugCode = 1700;
                    armory = hybridizeArmoryList[Context.RandomToUse.Next( 0, hybridizeArmoryList.Count )].GetSquad(); //warrior must hybridize
                }
                else
                {
                    debugCode = 1800;
                    armory = workingArmoryList[Context.RandomToUse.Next( 0, workingArmoryList.Count )].GetSquad(); //pick an armory at random
                }
                if ( armory == null )
                {
                    #region Tracing
                    if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion

                    return false;
                }
                debugCode = 1900;
                data.IsOffToUpgrade = true;
                if ( entity.Planet == armory.Planet )
                {
                    debugCode = 2000;
                    //fly to the armory
                    if ( tracing )
                        tracingBuffer.Add( entity.ToStringWithPlanet() + " mark " + entity.CurrentMarkLevel + " is dispatched to armory on that planet. There were " + workingArmoryList.Count + " entries on the list" );
                    GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse );
                    moveCommand.PlanetOrderWasIssuedFrom = entity.Planet.Index;
                    moveCommand.RelatedPoints.Add( armory.WorldLocation );
                    moveCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );
                }
                else
                {
                    debugCode = 2100;
                    if ( tracing )
                        tracingBuffer.Add( entity.ToStringWithPlanet() + " mark " + entity.CurrentMarkLevel + " is dispatched to armory on planet " + armory.GetPlanetName_Safe() + ". There were " + workingArmoryList.Count + " entries on the list" );
                    //fly to the planet with the armory
                    PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "ScourgeSendUnitToArmoryToUpgradeIfNecessary", entity.Planet, armory.Planet, PathingMode.Safest, Context, PathCacheData );
                    if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                    {
                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleSpecialMission], GameCommandSource.AnythingElse );
                        command.RelatedString = "Scrg_GoAmory";
                        command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                        for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                            command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                    }
                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in SendUnitToArmoryToUpgradeIfNecessary debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            finally
            {
                #region Tracing
                if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion

                GameEntity_Squad.ReleaseTemporarySquadList( workingArmoryList );
                GameEntity_Squad.ReleaseTemporarySquadList( hybridizeArmoryList );
            }
            return true;
        }
        private static readonly List<Planet> WorkingPlanetList = List<Planet>.Create_WillNeverBeGCed( 300, "ScourgeFactionDeepInfo-WorkingPlanetList" );
        public bool SendUnitToBuildablePlanetIfPossible( Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData,
                                                         GameEntity_Squad entity, out Planet dest )
        {
            dest = null;
            WorkingPlanetList.Clear();
            foreach ( Planet.PlanetAtHopDistance _phd in entity.Planet.PlanetsWithinXHops( -1,
                delegate ( Planet secondaryPlanet )
                {
                    PlanetFaction pFaction = secondaryPlanet.GetPlanetFactionForFaction(faction);
                    if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength * 2 >=
                         pFaction.DataByStance[FactionStance.Self].TotalStrength + pFaction.DataByStance[FactionStance.Friendly].TotalStrength )
                        return PropogationEvaluation.No;
                    return PropogationEvaluation.Yes;
                } ) )
            {
                Planet otherPlanet = _phd.Planet;
                //This needs to be its own function, to find large pockets of connected AI planets for the Reconnection list
                //this is where we check whether this is a suitable planet
                if ( CanPlanetBuildInfrastructure( faction, otherPlanet, Context ) )
                {
                    WorkingPlanetList.Add(otherPlanet);
                    if ( WorkingPlanetList.Count > 5 )
                        break;
                }
            }
            if ( WorkingPlanetList.Count == 0 )
                return false;
            dest = WorkingPlanetList[Context.RandomToUse.Next(0, WorkingPlanetList.Count)];
            PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "ScourgeSendUnitToBuildablePlanetIfPossible", entity.Planet, dest, PathingMode.Safest, Context, PathCacheData );
            if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
            {
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleSpecialMission], GameCommandSource.AnythingElse );
                command.RelatedString = "Scrg_GotoBuildable";
                command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                    command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );

                return true;
            }
            return false;
        }
        private void GoToLocation( GameEntity_Squad squad, ArcenPoint location, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache pathCacheData )
        {
            //We have the metal and the structure has the experience, so fly to it so we can upgrade it
            GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse );
            moveCommand.PlanetOrderWasIssuedFrom = squad.Planet.Index;
            moveCommand.RelatedPoints.Add( location );
            moveCommand.RelatedEntityIDs.Add( squad.PrimaryKeyID );
            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );
        }
        public void SendUnitToRandomAdjacentPlanet( Faction faction, ArcenLongTermIntermittentPlanningContext Context,
                                                   GameEntity_Squad entity, bool AllowHostilePlanet )
        {
            Planet destination = entity.Planet.GetRandomNeighbor( false, Context );
            int maxRetries = 10;
            int numRetries = 0;
            while ( !AllowHostilePlanet && numRetries < maxRetries )
            {
                numRetries++;
                destination = entity.Planet.GetRandomNeighbor( false, Context );
                var pFaction = destination.GetStanceDataForFaction( faction );
                if ( pFaction[FactionStance.Hostile].TotalStrength > 1000 )
                    destination = null;
                else
                    break;
            }
            if ( destination == null )
                return;
            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse );
            command.RelatedString = "Scrg_RandAdj";
            command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
            command.RelatedIntegers.Add( destination.Index );
            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
        }
        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            if ( World_AIW2.Instance.GameSecond > 1 )
                return;
            Tutorial tutorialData = World_AIW2.Instance.TutorialOrNull;
            if ( tutorialData != null ) //no scourge in tutorials
                return;
        
            GameEntityTypeData spawnerData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ScourgeVassalSpawner" );
            //normal path. Seed a spawner on all friendly king planets
            int intensity = AttachedFaction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                if ( entity.GetIsFriendlyTowards_Safe( AttachedFaction ) )
                {
                    //First, make sure we only join a scourge infused empire faction
                    Faction kingFaction = entity.PlanetFaction.Faction;
                    if ( kingFaction.Type != FactionType.Player )
                        continue;
                    PlayerTypeData playerTypeData = kingFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
                    if ( playerTypeData == null )
                        continue;
                    if (playerTypeData.InternalName != "ScourgeInfusedHumanEmpire" )
                    {
                        continue;
                    }
                    //okay, now we have the empire faction
                    GameEntity_Squad squad = entity.Planet.Mapgen_SeedEntity( Context, AttachedFaction, spawnerData, PlanetSeedingZone.MostAnywhere );
                    ScourgePerUnitBaseInfo data = squad.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                    data.ExperienceForNextLevel = (FInt)900; //we don't have XML at this point, so just hardcode something
                    data.FullyInitialized = true;
                    GameEntityTypeData armoryData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ScourgeVassalArmory" );
                    GameEntity_Squad armory = entity.Planet.Mapgen_SeedEntity( Context, AttachedFaction, armoryData, PlanetSeedingZone.MostAnywhere );
                    armory.SetCurrentMarkLevel( 2 );
                    data = armory.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                    data.ExperienceForNextLevel = (FInt)900; //we don't have XML at this point, so just hardcode something
                    string requestedArmory = BaseInfo.FirstArmory;
                    if ( String.IsNullOrEmpty( requestedArmory ) ) //this can be empty for some old quickstarts
                        requestedArmory = "Random";
                    ScourgeTypeData typedata = null;

                    typedata = ScourgeTypeDataTable.Instance.GetRowByArmoryName( requestedArmory, Context );
                    if ( typedata == null )
                        throw new Exception( "Got null row from ScourgeTypeDataTable. Requested <" + requestedArmory + ">" );
                    data.ScourgeTypeId = typedata.id;
                    data.FullyInitialized = true;
                }
            }
        }

        private int CreateSpawner( Faction faction, Planet planet, ArcenHostOnlySimContext Context, out GameEntity_Squad newEntity )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Scourge-CreateSpawner-trace", 10f ) : null;

            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( "WarpingInScourgeSpawner" );
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
            ArcenPoint spawnerSpawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 300 ) );

            //buildings can sometimes start at mark > 1, to make late-game scaling easier
            byte startMarkLevel = 1;
            if ( this.BaseInfo.HighestScienceEarnedByPlayer >= BaseInfo.Difficulty.ScienceRequiredForAllBuildingsToSpawnMark2 &&
                 BaseInfo.Difficulty.ScienceRequiredForAllBuildingsToSpawnMark2 > 0 )
                startMarkLevel = 2;
            if ( this.BaseInfo.HighestScienceEarnedByPlayer >= BaseInfo.Difficulty.ScienceRequiredForAllBuildingsToSpawnMark3 &&
                 BaseInfo.Difficulty.ScienceRequiredForAllBuildingsToSpawnMark3 > 0 )
                startMarkLevel = 3;

            newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, startMarkLevel,
                pFaction.Faction.LooseFleet, 0, spawnerSpawnLocation, Context, "Scourge-NewSpawner" );
            if ( newEntity != null )
            {
                newEntity.TransformsIntoAfterTime = "ScourgeSpawner";
                newEntity.SecondsTillTransformation = (Int16)Context.RandomToUse.Next( 30, 120 );

                ScourgePerUnitBaseInfo newdata = newEntity.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                newdata.ExperienceForNextLevel = BaseInfo.Difficulty.BaseExperienceForLevelupSpawners;
                newdata.FullyInitialized = true;
                if ( tracing )
                    tracingBuffer.Add( "Spawning " + newEntity.ToStringWithPlanet() + " \n" );
                BaseInfo.WarpingInSpawners.AddToDisplayList( newEntity ); //keep the list up to date in case there are multiple builders on a planet (so we don't get multiple spawns)
            }
            RandomlySpawnAIStructure( planet, Context, spawnerSpawnLocation, 50 );
            return BaseInfo.Difficulty.MetalCostForBuildingSpawner;
        }

        private void RandomlySpawnAIStructure( Planet planet, ArcenHostOnlySimContext Context, ArcenPoint nearMe, int percentGuardPost )
        {
            //For AI-allied scourge at high enough intensity (and not on ai homeworld)
            if ( BaseInfo.AIAllied && planet.GetControllingFactionType() == FactionType.AI &&
                 BaseInfo.Intensity > 5 && planet.OriginalHopsToAIHomeworld > 0 )
            {
                //we can spawn a new guard post near the armory to make the planet more interesting to deal with

                if ( Context.RandomToUse.Next( 0, 100 ) < percentGuardPost )
                {
                    PlanetFaction pFaction = planet.GetControllingPlanetFaction();

                    AISentinelsCoreData sentinelsExternal = pFaction.Faction.TryGetAISentinelsCoreData()?.SentinelInfo;
                    if ( sentinelsExternal == null )
                        return;
                    AIBudgetItem reinforcementBudgetItem = sentinelsExternal.AIType.BudgetItems[AIBudgetType.Reinforcement];
                    if ( reinforcementBudgetItem == null )
                        return;

                    AIShipGroup guardPostsArmed = null;
                    AIShipGroup guardPostsUnarmed = null;

                    guardPostsArmed = reinforcementBudgetItem.GuardPostAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    guardPostsUnarmed = reinforcementBudgetItem.UnarmedGuardPostAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );

                    DrawBag<GameEntityTypeData> guardPostBag = null;
                    if ( guardPostsArmed != null && guardPostsArmed.DrawBag != null && guardPostsArmed.DrawBag.GetHasItems() )
                        guardPostBag = guardPostsArmed.DrawBag;
                    else if ( guardPostsUnarmed != null && guardPostsUnarmed.DrawBag != null && guardPostsUnarmed.DrawBag.GetHasItems() )
                        guardPostBag = guardPostsUnarmed.DrawBag;
                    if ( guardPostBag == null || !guardPostBag.GetHasItems() )
                        return;

                    GameEntityTypeData entityData = guardPostBag.PickRandomItemAndReplace( Context.RandomToUse );

                    ArcenPoint spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, entityData, nearMe, FInt.FromParts( 0, 25 ), FInt.FromParts( 0, 100 ) );
                    GameEntity_Squad.CreateNew_ReturnNullIfMPClient( planet.GetControllingPlanetFaction(), entityData, planet.MarkLevelForAIOnly.Ordinal,
                                                                             pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "Scourge-RandomAIStructure" );

                }
            }

        }
        private static readonly List<ArcenPoint> FortressOptions = List<ArcenPoint>.Create_WillNeverBeGCed( 30, "ScourgeFactionDeepInfo-FortressOptions" );
        private int CreateFortress( Faction faction, Planet planet, ArcenHostOnlySimContext Context, out GameEntity_Squad newEntity )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Scourge-CreateFortress-trace", 10f ) : null;

            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( "WarpingInScourgeFortress" );
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
            ArcenPoint baseLocation = Engine_AIW2.Instance.CombatCenter; //default location

            //put fortresses near other scourge infrastructure if possible
            FortressOptions.Clear();
            List<SafeSquadWrapper> spawnersInGalaxy = this.BaseInfo.Spawners.GetDisplayList();
            for ( int i = 0; i < spawnersInGalaxy.Count; i++ )
                if ( spawnersInGalaxy[i].Planet == planet )
                    FortressOptions.Add( spawnersInGalaxy[i].WorldLocation );
            foreach ( GameEntity_Squad entity in this.BaseInfo.WarpingInSpawners.DisplaySquads() )
            {
                if ( entity.Planet == planet )
                    FortressOptions.Add( entity.WorldLocation );
            }
            List<SafeSquadWrapper> armoriesInGalaxy = this.BaseInfo.ArmoriesInGalaxy.GetDisplayList();
            for ( int i = 0; i < armoriesInGalaxy.Count; i++ )
                if ( armoriesInGalaxy[i].Planet == planet )
                    FortressOptions.Add( armoriesInGalaxy[i].WorldLocation );
            foreach ( GameEntity_Squad entity in this.BaseInfo.WarpingInArmories.DisplaySquads() )
            {
                if ( entity.Planet == planet )
                    FortressOptions.Add( entity.WorldLocation );
            }
            if ( FortressOptions.Count > 0 )
            {
                baseLocation = FortressOptions[Context.RandomToUse.Next( 0, FortressOptions.Count )];
            }


            ArcenPoint spawnLocation = planet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, entityData, baseLocation, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 200 ) );

            //buildings can sometimes start at mark > 1, to make late-game scaling easier
            byte startMarkLevel = 1;
            if ( this.BaseInfo.HighestScienceEarnedByPlayer >= BaseInfo.Difficulty.ScienceRequiredForAllBuildingsToSpawnMark2 &&
                 BaseInfo.Difficulty.ScienceRequiredForAllBuildingsToSpawnMark2 > 0 )
                startMarkLevel = 2;
            if ( this.BaseInfo.HighestScienceEarnedByPlayer >= BaseInfo.Difficulty.ScienceRequiredForAllBuildingsToSpawnMark3 &&
                 BaseInfo.Difficulty.ScienceRequiredForAllBuildingsToSpawnMark3 > 0 )
                startMarkLevel = 3;

            newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, startMarkLevel,
                                                                     pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "Scourge-NewFortress" );
            if ( newEntity != null )
            {
                GameEntityTypeData fortressData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ScourgeFortress" );
                if ( !faction.HasObtainedSpireDebris )
                {
                    int count = 10;
                    while ( fortressData.InternalName == "ScourgeSpireFortress" && count-- > 0 )
                    {
                        fortressData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ScourgeFortress" );
                    }
                }
                newEntity.TransformsIntoAfterTime = fortressData.InternalName;
                newEntity.SecondsTillTransformation = (Int16)Context.RandomToUse.Next( 30, 120 );

                ScourgePerUnitBaseInfo newdata = newEntity.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                newdata.ExperienceForNextLevel = BaseInfo.Difficulty.BaseExperienceForLevelupFortress;
                newdata.FullyInitialized = true;
                if ( tracing )
                    tracingBuffer.Add( "Spawning " + newEntity.ToStringWithPlanet() + " \n" );
                BaseInfo.WarpingInFortresses.AddToDisplayList( newEntity ); //keep the list up to date in case there are multiple builders on a planet (so we don't get multiple spawns)
            }
            return BaseInfo.Difficulty.MetalCostForBuildingFortress;
        }
        private bool CanPlanetBuildDefenses( Faction faction, Planet planet, GameEntity_Squad entity )
        {
            bool debug = false;
            if ( this.BaseInfo.AIAllied && (BaseInfo.Difficulty.MaxMarkPlanetForBuilding > 0 && planet.MarkLevelForAIOnly.Ordinal > BaseInfo.Difficulty.MaxMarkPlanetForBuilding) )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "CanPlanetBuildDefenses: " + entity.ToStringWithPlanet() + " early exit 1", Verbosity.DoNotShow );

                return false;
            }
            if ( BaseInfo.Difficulty.BaseExperienceForLevelupFortress <= 0 )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "CanPlanetBuildDefenses: " + entity.ToStringWithPlanet() + " early exit 2. " + BaseInfo.Difficulty.BaseExperienceForLevelupFortress + ", " + BaseInfo.Difficulty.ToString(), Verbosity.DoNotShow );

                return false; //we're too low intensity to build
            }
            int maxAllowedFortresses = 2; //for the AI, max of 2
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "CanPlanetBuildDefenses: " + entity.ToStringWithPlanet() + " A", Verbosity.DoNotShow );
            if ( this.BaseInfo.MinorFactionAllied || this.BaseInfo.InCivilWar )
                maxAllowedFortresses++;
            if ( this.BaseInfo.PlayerAllied )
            {
                maxAllowedFortresses = 1;
                if ( this.BaseInfo.Intensity >= 6 )
                    maxAllowedFortresses++; //max of 2 for the player at higher intensities
            }

            int currentFortresses = 0;
            List<SafeSquadWrapper> fortressesInGalaxy = this.BaseInfo.FortressesInGalaxy.GetDisplayList();
            for ( int i = 0; i < fortressesInGalaxy.Count; i++ )
            {
                if ( fortressesInGalaxy[i].Planet == planet )
                    currentFortresses++;
            }
            foreach ( GameEntity_Squad fort in this.BaseInfo.WarpingInFortresses.DisplaySquads() )
            {
                if ( fort.Planet == planet )
                    currentFortresses++;
            }
            if ( currentFortresses >= maxAllowedFortresses )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "CanPlanetBuildDefenses: " + entity.ToStringWithPlanet() + " exit B", Verbosity.DoNotShow );

                return false;
            }
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
            //ai-allied scourge can't build fortresses anywhere there are already strong defenses
            if ( pFaction.DataByStance[FactionStance.Friendly].TurretStrength > 2000 && this.BaseInfo.AIAllied )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "CanPlanetBuildDefenses: " + entity.ToStringWithPlanet() + " exit C", Verbosity.DoNotShow );

                return false;
            }
            if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength > pFaction.DataByStance[FactionStance.Self].TotalStrength / 5 )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "CanPlanetBuildDefenses: " + entity.ToStringWithPlanet() + " exit D", Verbosity.DoNotShow );

                return false; //gotta kill the enemies first
            }
            if ( planet.GetControllingFaction().GetIsHostileTowards( faction ) )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "CanPlanetBuildDefenses: " + entity.ToStringWithPlanet() + " exit E", Verbosity.DoNotShow );

                return false;
            }
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "CanPlanetBuildDefenses: " + entity.ToStringWithPlanet() + " SUCCESS", Verbosity.DoNotShow );

            return true;
        }
        private bool CanPlanetBuildInfrastructure( Faction faction, Planet planet, ArcenSimContextAnyStatus Context )
        {
            //Whether it can build spawner or armory
            if ( BaseInfo.Difficulty.MaxMarkPlanetForBuilding > 0 && planet.MarkLevelForAIOnly.Ordinal > BaseInfo.Difficulty.MaxMarkPlanetForBuilding )
                return false;
            if ( BaseInfo.SecondsUntilCanRebuild.ContainsKey( planet.Index ) )
                return false; //we're on delay
            if ( planet.GetControllingFaction().SpecialFactionData.InternalName == "ZenithArchitrave" &&
                 planet.GetControllingFaction().GetIsFriendlyTowards( faction ) )
            {
                //During Civil Wars (or player truces for player-allied scourge), ZAs will potentially be allied to the scourge temporarily. Don't let the scourge
                //build on a ZA planet unless they really are friendly
                if ( faction.BaseInfo.Allegiance != planet.GetControllingFaction().BaseInfo.Allegiance )
                    return false;
            }
            if ( !Context.IsLongRangePlanning )
            {
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
                if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength > (pFaction.DataByStance[FactionStance.Self].TotalStrength + pFaction.DataByStance[FactionStance.Friendly].TotalStrength) / 10 )
                    return false; //if there are substantial enemy forces, no building infrastructure
                List<SafeSquadWrapper> spawnersInGalaxy = this.BaseInfo.Spawners.GetDisplayList();
                for ( int i = 0; i < spawnersInGalaxy.Count; i++ )
                {
                    if ( spawnersInGalaxy[i].Planet == planet )
                        return false;
                    if ( planet.GetHopsTo( spawnersInGalaxy[i].Planet ) < BaseInfo.RequiredHopsBetweenSpawners )
                        return false;
                }
                bool shouldReturnFalse = false;
                foreach ( GameEntity_Squad spawner in this.BaseInfo.WarpingInSpawners.DisplaySquads() )
                {
                    if ( spawner.Planet == planet )
                    {
                        shouldReturnFalse = true;
                        continue;
                    }
                    if ( planet.GetHopsTo( spawner.Planet ) < BaseInfo.RequiredHopsBetweenSpawners )
                    {
                        shouldReturnFalse = true;
                        continue;
                    }
                }
                if ( shouldReturnFalse )
                    return false;

                foreach ( GameEntity_Squad armory in this.BaseInfo.WarpingInArmories.DisplaySquads() )
                {
                    if ( armory.Planet == planet )
                    {
                        shouldReturnFalse = true;
                        continue;
                    }
                    if ( planet.GetHopsTo( armory.Planet ) < BaseInfo.RequiredHopsBetweenSpawners )
                    {
                        shouldReturnFalse = true;
                        continue;
                    }
                }
                if ( shouldReturnFalse )
                    return false;

                List<SafeSquadWrapper> armoriesInGalaxy = this.BaseInfo.ArmoriesInGalaxy.GetDisplayList();
                for ( int i = 0; i < armoriesInGalaxy.Count; i++ )
                {
                    GameEntity_Squad armory = armoriesInGalaxy[i].GetSquad();
                    if ( armory == null )
                        continue;
                    if ( armory.Planet == planet )
                        return false;
                    if ( planet.GetHopsTo( armory.Planet ) < BaseInfo.RequiredHopsBetweenArmories )
                        return false;
                }
            }
            else
            {
                //this is from the long range planning context
                var pFaction = planet.GetStanceDataForFaction( faction );
                if ( pFaction[FactionStance.Hostile].TotalStrength > (pFaction[FactionStance.Self].TotalStrength + pFaction[FactionStance.Friendly].TotalStrength) / 10 )
                    return false; //if there are substantial enemy forces, no building infrastructure
                for ( int i = 0; i < LongRangePlanningSpawners.Count; i++ )
                {
                    GameEntity_Squad spawner = LongRangePlanningSpawners[i].GetSquad();
                    if ( spawner == null )
                        continue;
                    if ( spawner.Planet == planet )
                        return false;
                    if ( planet.GetHopsTo( spawner.Planet ) < BaseInfo.RequiredHopsBetweenSpawners )
                        return false;
                }
                for ( int i = 0; i < LongRangePlanningArmories.Count; i++ )
                {
                    GameEntity_Squad armory = LongRangePlanningArmories[i].GetSquad();
                    if ( armory == null )
                        continue;
                    if ( armory.Planet == planet )
                        return false;
                    if ( planet.GetHopsTo( armory.Planet ) < BaseInfo.RequiredHopsBetweenArmories )
                        return false;
                }
                for ( int i = 0; i < LongRangePlanningWarpingInSpawners.Count; i++ )
                {
                    GameEntity_Squad spawner = LongRangePlanningWarpingInSpawners[i].GetSquad();
                    if ( spawner == null )
                        continue;
                    if ( spawner.Planet == planet )
                        return false;
                    if ( planet.GetHopsTo( spawner.Planet ) < BaseInfo.RequiredHopsBetweenSpawners )
                        return false;
                }
                for ( int i = 0; i < LongRangePlanningWarpingInArmories.Count; i++ )
                {
                    GameEntity_Squad armory = LongRangePlanningWarpingInArmories[i].GetSquad();
                    if ( armory == null )
                        continue;
                    if ( armory.Planet == planet )
                        return false;
                    if ( planet.GetHopsTo( armory.Planet ) < BaseInfo.RequiredHopsBetweenArmories )
                        return false;
                }
            }
            return true;
        }
        private int CreateArmory( Faction faction, Planet planet, ArcenHostOnlySimContext Context, out GameEntity_Squad newEntity )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Scourge-CreateArmory-trace", 10f ) : null;
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "WarpingInScourgeArmory" );

            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
            ArcenPoint armorySpawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 300 ) );

            //buildings can sometimes start at mark > 1, to make late-game scaling easier
            byte startMarkLevel = 1;

            if ( this.BaseInfo.HighestScienceEarnedByPlayer >= BaseInfo.Difficulty.ScienceRequiredForAllBuildingsToSpawnMark2 &&
                 BaseInfo.Difficulty.ScienceRequiredForAllBuildingsToSpawnMark2 > 0 )
                startMarkLevel = 2;
            if ( this.BaseInfo.HighestScienceEarnedByPlayer >= BaseInfo.Difficulty.ScienceRequiredForAllBuildingsToSpawnMark3 &&
                 BaseInfo.Difficulty.ScienceRequiredForAllBuildingsToSpawnMark3 > 0 )
                startMarkLevel = 3;

            newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, startMarkLevel,
                                                                     pFaction.Faction.LooseFleet, 0, armorySpawnLocation, Context, "Scourge-NewArmory" );
            if ( newEntity != null )
            {
                newEntity.TransformsIntoAfterTime = "ScourgeArmory";
                newEntity.SecondsTillTransformation = (Int16)Context.RandomToUse.Next( 30, 120 );

                ScourgePerUnitBaseInfo newdata = newEntity.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                newdata.ExperienceForNextLevel = BaseInfo.Difficulty.BaseExperienceForLevelupArmories;
                newdata.FullyInitialized = true;

                if ( tracing )
                    tracingBuffer.Add( "Spawning " + newEntity.ToStringWithPlanet() + "\n" );
                BaseInfo.WarpingInArmories.AddToDisplayList( newEntity ); //keep the list up to date in case there are multiple builders on a planet (so we don't get multiple structures)
            }

            RandomlySpawnAIStructure( planet, Context, armorySpawnLocation, 50 );
            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion

            return BaseInfo.Difficulty.MetalCostForBuildingArmory;
        }

        public override Planet GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass( Planet TargetPlanet, int TeamStrength, Planet CurrentPlanetForTeam, 
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            Planet bestPlanet = null;
            int dangerOfPathFromBestPlanet = -1;
            int distanceFromBestPlanet = 99999999;
            Int16 hopsFromBestPlanet = 9999;
            int unused = 0;
            //this logic partially cribbed from IndependentAIFleet.cs::Helper_DoTargetFindingSweep

            if ( TargetPlanet == null )
                throw new Exception( "No target planet set in get lurk planet?!" );
            //int debugCode = 0;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            tracing = false;
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Scourge-GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;

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
                if ( planet.GetControllingOrInfluencingFaction().GetIsHostileTowards( AttachedFaction ) && planetDefensiveStrength > TeamStrength / 10 )
                    continue;

                //Don't path through any particularly dangerous planets
                Int16 hops = 0;
                int totalDifficultyOfPathToLurkPlanet = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForTeam, planet, true, out hops );
                if ( totalDifficultyOfPathToLurkPlanet >= TeamStrength * 5 ) //as long as they only outnumber us 5:1, let's go!
                    continue;

                int totalDifficultyOfPathToTarget = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, planet, TargetPlanet, true, out hops );
                if ( this.BaseInfo.AIAllied && planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    totalDifficultyOfPathToTarget *= 2; //watched planets are penalized for non-player-allied scourge

                if ( totalDifficultyOfPathToTarget < 0 )
                    totalDifficultyOfPathToTarget = 0; //pathing through allied planets is basically the same

                if ( tracing )
                    tracingBuffer.Add( "\tConsidering " + planet.Name + " danger of path to target " + totalDifficultyOfPathToTarget + " danger of path to lurk planet " + totalDifficultyOfPathToLurkPlanet + " distance " + Distance + " intel " + planet.IntelLevel + " my strength " + TeamStrength ).Add( "\n" );

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
        public override void GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass( bool DefenseMode, Planet CurrentPlanetForFireteam, 
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, List<FireteamTarget> PreferredTargets, List<FireteamTarget> FallbackTargets, object TeamObj )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Scourge-GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            FInt multiplierForBuildablePlanet = FInt.FromParts( 0, 900 ); //planets that we can build on make better targets
            Fireteam team = (Fireteam)TeamObj;
            string log = "";
            if ( DefenseMode )
            {
                log = "(defensive)";
                //find allied command stations with enemies on them preferrably,
                AlliedPlanetsUnderAttack( PreferredTargets, AttachedFaction, Context );
                //or weak nearby planets they can take so the scourge can expand early
                //later in the game defensive fleets should only play defense though.
                //Note that only player-allied scourge will have this behaviour, since the AI has lots of space
                //to expand into
                if ( LongRangePlanningArmories.Count < 2 || LongRangePlanningArmories.Count < 2 )
                    GetExpansionTargets( FallbackTargets, AttachedFaction, Context );
                else
                    FallbackTargets.Clear();
            }
            else
            {
                log = "(player/minor faction)";
                PreferredScourgeAntiAITargets( PreferredTargets, AttachedFaction, Context, tracingBuffer, team.FireTeamID );
                FallbackScourgeAntiAITargets( FallbackTargets, AttachedFaction, Context );
            }

            if ( PreferredTargets.Count == 0 && FallbackTargets.Count == 0 &&
                 this.BaseInfo.MinorFactionAllied) //minor faction allied scourge can sometimes get stuck in the early game
                GetAdjacentTargets( FallbackTargets, AttachedFaction, CurrentPlanetForFireteam, Context );
            
            if ( tracing )
                tracingBuffer.Add( "GetPreferredAndFallbackTarghets: Finding targets for fireteam " + team.FireTeamID + " " + log + " whose current planet is " + CurrentPlanetForFireteam.Name + ", there are " + PreferredTargets.Count + " preferred and " + FallbackTargets.Count + " fallback targets\n" );
            //Note the scourge don't use the dangerOfTarget vs dangerOfPath code, since
            //we haven't observed any performance problems here. If we do feel like the Scourge
            //need to make fewer pathfinding calls then we should do that
            for ( int i = 0; i < PreferredTargets.Count; i++ )
            {
                FireteamTarget target = PreferredTargets[i];
                Int16 hops = 0;
                target.dangerOfPath = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForFireteam, PreferredTargets[i].planet, true, out hops );
                PreferredTargets[i] = target;
            }

            //sort targets by how hard it is to get there (with a preference for planets that can build new scourge infrastructure)
            cb_svThis = this;
            cb_svFtContext = Context;
            cb_svFtMultiplier = multiplierForBuildablePlanet;
            PreferredTargets.Sort( static delegate ( FireteamTarget Left, FireteamTarget Right )
            {
                FInt lDifficulty = (FInt)Left.dangerOfPath;
                FInt rDifficulty = (FInt)Right.dangerOfPath;
                if ( cb_svThis.CanPlanetBuildInfrastructure( cb_svThis.AttachedFaction, Left.planet, cb_svFtContext ) )
                    lDifficulty *= cb_svFtMultiplier;
                if ( cb_svThis.CanPlanetBuildInfrastructure( cb_svThis.AttachedFaction, Right.planet, cb_svFtContext ) )
                    rDifficulty *= cb_svFtMultiplier;

                return lDifficulty.CompareTo( rDifficulty );
            } );
            if ( FallbackTargets == null || FallbackTargets.Count == 0 )
            {
                #region Tracing
                if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion

                return;
            }
            for ( int i = 0; i < FallbackTargets.Count; i++ )
            {
                FireteamTarget target = FallbackTargets[i];
                Int16 hops = 0;
                target.dangerOfPath = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForFireteam, target.planet, true, out hops );
                FallbackTargets[i] = target;
            }

            FallbackTargets.Sort( static delegate ( FireteamTarget Left, FireteamTarget Right )
            {
                int lDifficulty = Left.dangerOfPath;
                int rDifficulty = Right.dangerOfPath;
                return lDifficulty.CompareTo( rDifficulty );
            } );

            bool debug = false;
            if ( tracing && debug )
            {
                tracingBuffer.Add( "For scourge, we have the following Preferred targets\n" );
                for ( int i = 0; i < PreferredTargets.Count; i++ )
                    tracingBuffer.Add( "\t" ).Add( PreferredTargets[i].GetPlanetName_Safe() ).Add( "\n" );
                tracingBuffer.Add( "For scourge, we have the following Fallback targets\n" );
                for ( int i = 0; i < FallbackTargets.Count; i++ )
                    tracingBuffer.Add( "\t" ).Add( FallbackTargets[i].GetPlanetName_Safe() ).Add( "\n" );
            }
            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
        }

        public void PreferredScourgeAntiAITargets( List<FireteamTarget> ListToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context, ArcenCharacterBuffer tracingBuffer, int fireteamIdForLogging )
        {
            ListToFill.Clear();
            bool tracing = ( tracingBuffer != null );

            Faction firstPlayerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            //in general we don't trace in here because it's so verbose, but it's now easy to do so
            bool extraDebug = false;
            if ( tracing && extraDebug )
                tracingBuffer.Add("Finding preferred targets for fireteam " + fireteamIdForLogging ).Add("\n");
            bool foundOverride = false;
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.PlanetImportanceUIOnly != PlanetImportance.Fire_High &&
                     planet.PlanetImportanceUIOnly != PlanetImportance.Fire_Override )
                    continue;
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction ( this.AttachedFaction );
                if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength < 10 * 1000 )
                    continue;
                if ( planet.PlanetImportanceUIOnly == PlanetImportance.Fire_Override )
                    foundOverride = true;
                ListToFill.Add( new FireteamTarget( planet ) );
            }

            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                if ( foundOverride )
                    break; //if the player has given us an override, don't bother
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null )
                    continue;

                if ( !otherFaction.GetIsHostileTowards( firstPlayerFaction ) )
                    continue;
                if ( !otherFaction.GetIsHostileTowards( faction ) )
                    continue;

                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.GrantsMinorFactionPlanetControl ) )
                {
                     //allows the scourge to go after hostile minor factions
                     if ( entity.Planet.IsZenithArchitraveTerritory )
                         continue;

                     ListToFill.Add( new FireteamTarget( entity ) );
                 }
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                     //scourge are always allowed to snipe anyone's king

                     ListToFill.Add( new FireteamTarget( entity ) );
                 }

                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.CommandStation ) )
                {
                    //if this is a neutered hostile planet, ignore it
                    if ( entity.SecondsSpentAsRemains > 0 )
                     {
                         continue;
                     }
                    //Note that we check FactionStance.Self because we care about the strength of that entity's units
                    if ( entity.PlanetFaction.DataByStance[FactionStance.Self].TotalStrength < 1000 ||
                          (entity.PlanetFaction.DataByStance[FactionStance.Self].TotalStrength == entity.PlanetFaction.DataByStance[FactionStance.Self].StrengthInReinforcementPoints &&
                           entity.PlanetFaction.DataByStance[FactionStance.Self].TotalStrength < 5000) )
                     {
                         continue;
                     }

                     if ( entity.PlanetFaction.DataByStance[FactionStance.Self].NumGuardPosts == 0 && faction.OverallPowerLevel > FInt.FromParts( 0, 500 ) &&
                          BaseInfo.PlayerAllied )
                     {
                         continue; //for player allied scourge, don't bother with neutered planets once the game is a good ways in
                    }

                     ListToFill.Add( new FireteamTarget( entity.Planet ) );
                 }
                if ( otherFaction.SpecialFactionData.InternalName == "Instigators" )
                {
                    //allied scourge should take out instigator bases
                    foreach ( GameEntity_Squad entity in otherFaction.Squads( "InstigatorBase" ) )
                    {
                        ListToFill.Add( new FireteamTarget( entity ) );
                    }
                }
            }
            for ( int i = 0; i < ListToFill.Count; i++ )
            {
                FireteamTarget target = ListToFill[i];
                int ignored = 0;
                target.dangerOfTarget = Fireteam.GetPlanetDefensiveStrength( target.planet, faction, true, ref ignored, FInt.Zero, FInt.Zero );
                ListToFill[i] = target;
            }

            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion

        }
        public void GetAdjacentTargets( List<FireteamTarget> ListToFill, Faction faction, Planet planet, ArcenLongTermIntermittentPlanningContext Context )
        {
            ListToFill.Clear();
            foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
            {
                var pFaction = neighbor.GetStanceDataForFaction( faction );
                if ( pFaction[FactionStance.Hostile].TotalStrength >
                     (pFaction[FactionStance.Self].TotalStrength + pFaction[FactionStance.Friendly].TotalStrength ) )
                    ListToFill.Add( new FireteamTarget( planet ) );
            }
        }
        public void GetExpansionTargets( List<FireteamTarget> ListToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            //the player-allied scourge is allowed to neuter neutral planets
            ListToFill.Clear();
            if ( this.BaseInfo.AIAllied ) 
                return; //only player or minor faction scourge do this
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                var pFaction = planet.GetStanceDataForFaction( faction );
                if ( CanPlanetBuildInfrastructure( faction, planet, Context ) && pFaction[FactionStance.Hostile].TotalStrength > 1000 &&
                     pFaction[FactionStance.Hostile].TotalStrength < 6000 )
                    ListToFill.Add( new FireteamTarget( planet ) );
            }
            for ( int i = 0; i < ListToFill.Count; i++ )
            {
                FireteamTarget target = ListToFill[i];
                int ignored = 0;
                target.dangerOfTarget = Fireteam.GetPlanetDefensiveStrength( target.planet, faction, true, ref ignored, FInt.Zero, FInt.Zero );
                ListToFill[i] = target;
            }
        }

        public void FallbackScourgeAntiAITargets( List<FireteamTarget> ListToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            //the player-allied scourge is allowed to neuter neutral planets
            ListToFill.Clear();

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction ( this.AttachedFaction );

                if ( planet.PlanetImportanceUIOnly == PlanetImportance.Fire_Low ||
                     planet.PlanetImportanceUIOnly == PlanetImportance.Fire_Med )
                {
                    if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength < 10 * 1000 )
                        continue;

                    ListToFill.Add( new FireteamTarget( planet ) );
                    continue;
                }

                if ( planet.GetControllingFactionType() != FactionType.NaturalObject )
                    continue;
                if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength < 4000 )
                    ListToFill.Add( new FireteamTarget( planet ) );
            }
            for ( int i = 0; i < ListToFill.Count; i++ )
            {
                FireteamTarget target = ListToFill[i];
                int ignored = 0;
                target.dangerOfTarget = Fireteam.GetPlanetDefensiveStrength( target.planet, faction, true, ref ignored, FInt.Zero, FInt.Zero );
                ListToFill[i] = target;
            }
        }
        public void AlliedPlanetsUnderAttack( List<FireteamTarget> ListToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context )
        {
            ListToFill.Clear();

            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null )
                    continue;
                if ( !otherFaction.GetIsFriendlyTowards( faction ) )
                    continue;
                //so this is a friendly faction
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.CommandStation ) )
                {
                     var pFaction = entity.Planet.GetStanceDataForFaction( faction );
                     if ( pFaction[FactionStance.Hostile].TotalStrength > 0 )
                         ListToFill.Add( new FireteamTarget( entity.Planet ) );
                 }
            }
        }        

        private bool GetShouldFireteamRetreatFromPrimeObjective( Faction faction, ArcenLessLinkedList<Fireteam> teams, ArcenLongTermIntermittentPlanningContext Context )
        {
            return false;
        }
        
        public override void ReactToHacking_AsPartOfMainSim_HostOnly( GameEntity_Squad entityBeingHacked, FInt WaveMultiplier, ArcenHostOnlySimContext Context, HackingEvent Event, Faction overrideFaction = null )
        {
            Faction aiFaction = World_AIW2.GetRandomAIFaction( Context );
            AISentinelsFactionBaseInfo aiBaseInfo = aiFaction.TryGetAISentinelsCoreData();
            if ( aiBaseInfo != null )
            {
                //First compute the base strength of the hacking response
                AISentinelsFactionDeepInfo aiDeepInfo = aiFaction.TryGetAISentinelsDeepLogic();
                int HackingWaveSize = aiDeepInfo.BaseInfo.SentinelInfo.AIDifficulty.BaseHackingWaveSize;
                int strength = (WaveMultiplier * HackingWaveSize).IntValue;

                //if there's an AIP multiplier, handle that now
                //If the AIP multiplier is .01 and the AIP is 200 then we do
                //newStrength = oldStrength + (oldStrength * AIPMultiplier*AIP)
                //a straight multiplier would allow the resulting waves to have too much variance
                FInt aipMultiplier = aiDeepInfo.BaseInfo.SentinelInfo.AIDifficulty.HackingAipMultiplier;
                int bonusStrength = 0;
                if ( aipMultiplier > FInt.Zero )
                {
                    FInt AIP = GlobalAIWorldBaseInfo.Instance.AIProgress_Effective;
                    bonusStrength = (aipMultiplier * AIP * strength).IntValue;
                    strength += bonusStrength;
                }
                if ( Event != null )
                    Event.ApproxResponseStrength += strength;
                bool allowedGuardians = true;
                PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
                aiDeepInfo.SendWave( Context, pathingCacheData, strength, entityBeingHacked, null, -1, allowedGuardians );
                pathingCacheData.ReturnToPool();
            }
        }
    }
}
