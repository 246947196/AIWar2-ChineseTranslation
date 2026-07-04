using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public sealed class MarauderFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        public MarauderFactionBaseInfo BaseInfo;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<MarauderFactionBaseInfo>();
        }

        protected override void Cleanup()
        {
            BaseInfo = null;

            idleRaidersPerPlanet.Clear();
            UnassignedShips.Clear();
            RaiderTargets.Clear();
            AdjacentAlliedPlanets.Clear();
            AdjacentNonMarauderPlanets.Clear();
            LRPOutposts.Clear();
            idleShipsPerPlanet.Clear();
            AvailableFireteams.Clear();
            outpostData.Clear();

            TeamsAimedAtPlanet.Clear();
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 2;
        public readonly ProtectedValDictionary<Planet, FireteamRegiment> TeamsAimedAtPlanet = ProtectedValDictionary<Planet, FireteamRegiment>.Create_WillNeverBeGCed( 100, "MarauderFactionDeepInfo-TeamsAimedAtPlanet" );

        //bool playWarpOutSoundVictory = false; //some ships have warped out this Second (I'm not sure if I can call Presentation from LongRangePlanning, so set this in LongRangePlanning and then play the sound effect in PerSecond
        //bool playWarpOutSoundDefeat = false; //some ships have warped out this Second (I'm not sure if I can call Presentation from LongRangePlanning, so set this in LongRangePlanning and then play the sound effect in PerSecond

        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
        }

        //used for Brute Force marauder mode
        public readonly DictionaryOfLists<Planet, SafeSquadWrapper> idleRaidersPerPlanet = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 60, "MarauderFactionDeepInfo-idleRaidersPerPlanet" );
        public readonly List<SafeSquadWrapper> UnassignedShips = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "MarauderFactionDeepInfo-UnassignedShips" );
        public readonly List<Planet> RaiderTargets = List<Planet>.Create_WillNeverBeGCed( 500, "MarauderFactionDeepInfo-RaiderTargets" );
        public readonly List<Planet> AdjacentAlliedPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "MarauderFactionDeepInfo-AdjacentAlliedPlanets" );
        public readonly List<Planet> AdjacentNonMarauderPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "MarauderFactionDeepInfo-AdjacentNonMarauderPlanets" );
        public readonly List<SafeSquadWrapper> LRPOutposts = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "MarauderFactionDeepInfo-LRPOutposts" );
        //for patrolling
        public readonly DictionaryOfLists<Planet, SafeSquadWrapper> idleShipsPerPlanet = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 60, "MarauderFactionDeepInfo-idleShipsPerPlanet" );
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            /* There are a few distinct pieces here.
               First, we figure out whether there are any planets that the HRF should send troops to.

               Second, handle the actual battle.

               Third, handle the post-battle, where the ships will either warp out or start patrolling.
               For the warp out, ships will fly to the edge of the gravity well then do the Spawn effect, but it will get bigger for a second and then vanish.*/
            /* Stage 1: check for eligible planets */

            this.TeamsAimedAtPlanet.Clear();
            LRPOutposts.Clear();
            bool stageOneDebug = false;
            bool stageTwoDebug = false;
            bool stageThreeDebug = false;
            #region Tracing
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.HumanMarauders );
            bool fireteamTracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Marauder-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            #endregion
            if ( tracing ) tracingBuffer.Add( this.TracingName ).Add( " LongRangePlanning trace begins for marauder faction " ).Add( AttachedFaction.FactionIndex ).Add( " at " ).Add( World_AIW2.Instance.GameSecond ).Add( "\n" );

            Galaxy galaxy = World_AIW2.Instance.CurrentGalaxy;
            idleRaidersPerPlanet.Clear();

            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {

                //for fireteams
                UnassignedShips.Clear();
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
                {
                    //first iterate over and clean up stale data (ships list and strength)
                    team.DeepInfo.Reset();
                }

                idleShipsPerPlanet.Clear();
                /* Stage 1: handle ships on combat and figure out Outpost stuff */
                if ( stageOneDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Loop over all the Marauder units to see what's happening", Verbosity.DoNotShow );
                FactionUtilityMethods.Instance.FlushUnitsFromReinforcementPointsOnAllRelevantPlanets( AttachedFaction, Context, 5f );
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    if ( entity.TypeData.GetHasTag( "WarpingInMarauderOutpost" ) )
                    {
                        continue;
                    }
                    if ( entity.TypeData.GetHasTag( "MarauderOutpost" ) )
                    {
                        LRPOutposts.Add( entity );
                        continue;
                    }
                    if ( !entity.TypeData.IsMobile || entity.TypeData.IsDrone )
                        continue;
                    Planet currentPlanet = entity.Planet;
                    if ( stageOneDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Entity " + entity.PrimaryKeyID + " on planet " + currentPlanet.Name + " friendly strength " + currentPlanet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Friendly].TotalStrength + " enemy strength " + currentPlanet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Hostile].MobileStrength, Verbosity.DoNotShow );
                    entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //definitely breaks in multiplayer because nonsim.  Should use SetBehavior_FromFaction

                    if ( currentPlanet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Hostile].TotalStrength < currentPlanet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Self].TotalStrength / 10 )
                    {
                        //Only count planets where we dramatically outnumber the enemies (or there are no enemies)
                        if ( entity.FireteamId < 0 )
                            UnassignedShips.Add( entity );
                        else
                        {
                            Fireteam team = FireteamBaseUtility.GetFireteamById( BaseInfo.Teams, entity.FireteamId );
                            if ( team == null )
                                entity.FireteamId = -1; //something happened to the fireteam, so lets find a new one next LRP stage
                            else
                                team.DeepInfo.AddUnit( entity );
                        }
                        if ( entity.Orders.GetQueuedOrderCount() == 0 && entity.TypeData.GetHasTag( "MarauderRaider" ) )
                        {
                            idleRaidersPerPlanet[entity.Planet].Add( entity );
                        }
                        else if ( entity.Orders.GetQueuedOrderCount() == 0 )
                        {
                            idleShipsPerPlanet[entity.Planet].Add( entity );
                        }
                    }
                    if ( currentPlanet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Hostile].MobileStrength > 0 )
                    {
                        //there are enemies! Fight them!
                        entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //definitely breaks in multiplayer because nonsim.  Should use SetBehavior_FromFaction
                    }

                }

                //Stage 2.5: order ships to start patrolling
                bool allowPatrolling = false; //patrolling is disabled so that drones will fly back into their flagships
                int pairCount = idleShipsPerPlanet.GetCountOfLists();
                foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in idleShipsPerPlanet )
                {
                    /* The Dyson Sphere ships generally want to patrol planets that they have already cleared out,
                       with a preference for planets controlled by allied factions. They will still sometimes try to attack enemy controlled planets though */
                    if ( allowPatrolling )
                        FactionUtilityMethods.Instance.patrolPlanet( this.AttachedFaction, pair.Value, Context, pair.Key.GravWellSize.DistanceScale_GravwellRadius / 4, 10f );
                }

                /* Step 3: allow Raiders to attack nearby planets */
                if ( this.BaseInfo.IsInFireteamMode )
                {
                    FireteamUtility.UpdateFireteams( AttachedFaction, Context, pathingCacheData, BaseInfo.Teams, TeamsAimedAtPlanet, tracingBuffer, BaseInfo.OverkillRaidersFireteam );
                    FireteamUtility.UpdateRegiments( AttachedFaction, Context, pathingCacheData, BaseInfo.Teams, TeamsAimedAtPlanet, tracingBuffer, AttachedFaction.MinFireteamStrength, true );

                    for ( int i = 0; i < UnassignedShips.Count; i++ )
                        AssignUnitToFireteam( AttachedFaction, UnassignedShips[i].GetSquad(), Context, pathingCacheData );
                    if ( AttachedFaction.NumFireteams != BaseInfo.Teams.GetItemCount() )
                        AttachedFaction.NumFireteams = BaseInfo.Teams.GetItemCount();

                }
                else
                {
                    if ( fireteamTracing )
                        tracingBuffer.Add( "Fireteams are not enabled" );
                    //old, Brute Force mode
                    int i = 0;
                    foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in idleRaidersPerPlanet )
                    {
                        //Check all neighboring planets to see if the Raiders are stronger than the defenses there. If so, attack the weakest
                        RaiderTargets.Clear();
                        AdjacentAlliedPlanets.Clear();
                        AdjacentNonMarauderPlanets.Clear();
                        if ( stageThreeDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Checking raider Planet " + i + " of " + idleRaidersPerPlanet.GetCountOfLists() + " planet " + pair.Key.Name + " numRaiders " + pair.Value.Count, Verbosity.DoNotShow );

                        Planet planet = pair.Key;
                        List<SafeSquadWrapper> list = pair.Value;
                        int raiderStrength = 0;
                        for ( int idx = 0; idx < list.Count; idx++ )
                        {
                            //we need to check each Raider in case it is stacked
                            raiderStrength += list[idx].GetStrengthOfSelfAndContents();
                        }
                        //int nonMarauderPlanets = 0;
                        foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                        {
                            if ( neighbor.GetFactionWithSpecialInfluenceHere() == AttachedFaction || neighbor.GetFactionWithSpecialInfluenceHere().GetIsFriendlyTowards( AttachedFaction ) ||
                               neighbor.GetControllingFaction().GetIsFriendlyTowards( AttachedFaction ) )
                            {
                                AdjacentAlliedPlanets.Add( neighbor );
                                continue;
                            }
                            else
                                AdjacentNonMarauderPlanets.Add( neighbor );
                            PlanetFaction pFaction = neighbor.GetPlanetFactionForFaction( AttachedFaction );

                            if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength < (int)raiderStrength * BaseInfo.OverkillRaidersNonFireteam )
                            {
                                if ( stageThreeDebug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( neighbor.Name + " has strength " + pFaction.DataByStance[FactionStance.Hostile].TotalStrength + " < " + raiderStrength + " so it is a valid target to attack", Verbosity.DoNotShow );
                                RaiderTargets.Add( neighbor );
                            }
                            else if ( stageThreeDebug )
                            {
                                ArcenDebugging.ArcenDebugLogSingleLine( neighbor.Name + " has strength " + pFaction.DataByStance[FactionStance.Hostile].TotalStrength + " > " + raiderStrength + " so it is too scary", Verbosity.DoNotShow );
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
                        //if there are no allied planets that need help, see if we have a suitable 
                        if ( attackTarget == null )
                        {
                            for ( int k = 0; k < RaiderTargets.Count; k++ )
                            {
                                if ( attackTarget == null )
                                    attackTarget = RaiderTargets[k];
                                else if ( RaiderTargets[k].GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Hostile].TotalStrength <
                                        attackTarget.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Self].TotalStrength + attackTarget.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Friendly].TotalStrength )
                                    attackTarget = RaiderTargets[k];
                            }
                        }
                        if ( attackTarget == null )
                        {
                            //No easily attacked targets. Check for the case where we are surrounded by all marauder planets,
                            //and if so go somewhere at random
                            if ( AdjacentNonMarauderPlanets.Count == 0 )
                            {
                                int randomNumber = Context.RandomToUse.Next( 0, AdjacentAlliedPlanets.Count );
                                if ( stageThreeDebug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "There are no adjacent non-raider planets to " + pair.Key.Name + ", so pick a random one from 0 to " + AdjacentAlliedPlanets.Count + ": " + randomNumber, Verbosity.DoNotShow );
                                attackTarget = AdjacentAlliedPlanets[randomNumber];
                            }
                        }
                        if ( attackTarget != null )
                        {
                            if ( stageThreeDebug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Sending Raiders from " + pair.Key.Name + " with strength " + raiderStrength + " to " + attackTarget.Name, Verbosity.DoNotShow );
                            FactionUtilityMethods.Instance.Helper_RaidSpecificPlanet( list, planet, AttachedFaction, World_AIW2.Instance.CurrentGalaxy, attackTarget, false, Context, pathingCacheData, 5f );
                        }
                        i++;
                    }
                }
                if ( stageTwoDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Faction " + AttachedFaction.FactionIndex + " Exiting long range planning", Verbosity.DoNotShow );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Marauder LRP error: " + e, Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();

                #region Tracing
                if ( tracing || fireteamTracing ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " " + AttachedFaction.FactionIndex + " " ).Add( " Long Range Planning trace ends at " ).Add( World_AIW2.Instance.GameSecond ).Add( "\n" );
                if ( tracing || fireteamTracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
                if ( tracingBuffer != null )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
            }
        }

        //prefer close ones that you can get to safely
        private readonly ArcenLessLinkedList<Fireteam> AvailableFireteams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "MarauderFactionDeepInfo-AvailableFireteams" ); 
        private void AssignUnitToFireteam( Faction faction, GameEntity_Squad entity, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( entity == null )
                return;

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam ) && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.HumanMarauders );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Marauder-AssignUnitToFireteam-trace", 10f ) : null;

            AvailableFireteams.Clear();
            //bool debug = false;
            if ( this.BaseInfo.Teams.GetItemCount() == 0 )
            {
                Fireteam team = Fireteam.CreateNewWithIDFromList( BaseInfo.Teams );
                team.MyStrengthMultiplierForStrengthCalculation = FInt.FromParts( 1, 00 );
                team.EnemyStrengthMultiplierForStrengthCalculation = FInt.FromParts( 1, 500 );
                team.StrengthToBringOnline = faction.MinFireteamStrength + Context.RandomToUse.Next( 0, faction.MaxFireteamStrength - faction.MinFireteamStrength );
                this.BaseInfo.Teams.AddIfNotAlreadyIn( team );
            }
            int maxHops = 10;
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
            {
                if ( team.status == FireteamStatus.Disbanded ||
                        team.status == FireteamStatus.ReadyToAttack ||
                        team.status == FireteamStatus.Attacking )
                    continue; //if a fireteam is ready to fight, don't send more ships to that team
                if ( team.status == FireteamStatus.Staging && //if a fireteam is already "pretty strong", don't give it more
                     team.DeepInfo.TeamStrength > faction.MaxFireteamStrength )
                    continue;
                if ( team.DeepInfo.TeamStrength > faction.MaxFireteamStrength * 2 && team.DeepInfo.ShipsInFireteam.Count > 20 ) //if this is much stronger than usual, don't make it even stronger
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
                    if ( tracing )
                        tracingBuffer.Add( "Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path A\n" );
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
                Fireteam team = Fireteam.CreateNewWithIDFromList( BaseInfo.Teams );
                team.MyStrengthMultiplierForStrengthCalculation = FInt.FromParts( 1, 00 );
                team.EnemyStrengthMultiplierForStrengthCalculation = FInt.FromParts( 1, 500 );
                team.StrengthToBringOnline = faction.MinFireteamStrength + Context.RandomToUse.Next( 0, faction.MaxFireteamStrength - faction.MinFireteamStrength );
                int numShipsForConcentratingEfforts = 5; //before we're too strong, best to concentrate our forces
                if ( BaseInfo.Teams.GetItemCount() < numShipsForConcentratingEfforts )
                    team.PercentBestTarget = 100;
                else if ( this.AttachedFaction.SpecialFactionData.FireteamPercentBestTarget > 0 )
                    team.PercentBestTarget = this.AttachedFaction.SpecialFactionData.FireteamPercentBestTarget;
                else
                    team.PercentBestTarget = 65;
                team.PercentDistanceBestTarget = 45; //marauders often get far-flung empires
                team.PreferredMaxDistance = 5;
                team.DeepInfo.AddUnit( entity );
                entity.FireteamId = team.FireTeamID;

                team.DeepInfo.IdentifyCurrentPlanet(); //in case this unit is the first unit
                BaseInfo.Teams.AddIfNotAlreadyIn( team );
                if ( tracing )
                    tracingBuffer.Add( "Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path B: new fireteam. Percent New Fireateam: " + percentNewTeam + "\n" );
                return;
            }

            {
                Fireteam team = AvailableFireteams.GetRandom( Context.RandomToUse );
                team.DeepInfo.AddUnit( entity );
                entity.FireteamId = team.FireTeamID;
                if ( tracing )
                    tracingBuffer.Add( "Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path C\n" );
                team.DeepInfo.IdentifyCurrentPlanet(); //just in case this unit is the first unit or something
            }
        }

        public override GameEntity_Squad GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            int currentDanger = -1;
            GameEntity_Squad retreatPoint = null;
            for ( int i = 0; i < LRPOutposts.Count; i++ )
            {
                GameEntity_Squad outpost = LRPOutposts[i].GetSquad();
                if ( outpost == null )
                    continue;
                Int16 hops = 0;
                int danger = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForFireteam, outpost.Planet, true, out hops );
                if ( danger < currentDanger || currentDanger == -1 )
                {
                    retreatPoint = outpost;
                    currentDanger = danger;
                }
                if ( danger == 0 )
                    break;
            }
            return retreatPoint;
        }

        public override void GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass( bool DefenseMode, Planet CurrentPlanetForFireteam, 
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, List<FireteamTarget> PreferredTargets, List<FireteamTarget> FallbackTargets, object TeamObj )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Marauder-GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            FInt falloffForDistance = FInt.FromParts( 0, 050 );
            GetPreferredMarauderTargets( PreferredTargets, AttachedFaction, Context );
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


            bool debug = true;
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
        public void GetPreferredMarauderTargets( List<FireteamTarget> ListToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context )
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
                    //can go after minor factions
                    if ( Fireteam.IsThisAWinningBattle( faction, Context, entity.Planet, 2 ) )
                         continue;  //if we are already attacking and comfortably winning, don't bother sending more units
                     ListToFill.Add( new FireteamTarget( entity ) );
                 }
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.CommandStation ) )
                {
                     if ( Fireteam.IsThisAWinningBattle( faction, Context, entity.Planet, 2 ) )
                         continue; //if we are already attacking and comfortably winning, don't bother sending more units
                     ListToFill.Add( new FireteamTarget( entity.Planet ) );
                 }
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                     ListToFill.Add( new FireteamTarget( entity ) );
                 }
                if ( otherFaction.SpecialFactionData.InternalName == "Instigators" )
                {
                    //marauders can target instigator bases
                    foreach ( GameEntity_Squad entity in otherFaction.Squads() )
                    {
                        if ( entity.TypeData.GetHasTag( "InstigatorBase" ) )
                            ListToFill.Add( new FireteamTarget( entity ) );
                    }
                }
            }
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( Fireteam.IsThisAWinningBattle( faction, Context, planet, 2 ) )
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
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Marauder-GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
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
                if ( totalDifficultyOfPathToLurkPlanet >= TeamStrength * 5 )//as long as they only outnumber us 5:1, let's go!
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
        public override void DoPerSimStepLogic_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
        }

        private static ArcenDoubleCharacterBuffer spawningBuffer = new ArcenDoubleCharacterBuffer( "MarauderFactionDeepInfo-spawningBuffer" );

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            bool veryVerboseDebug = false;
            bool localDebug = false;
            bool logging = false;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.HumanMarauders );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Marauder-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f ) : null;
            
            if ( BaseInfo.MaraudersAreSuppressed )
            {
                //either we are locked behind a beacon or set for delayed invasion
                return;
            }
            else if ( !AttachedFaction.HasDoneInvasionStyleAction )
                AttachedFaction.HasDoneInvasionStyleAction = true;

            updateBudget( AttachedFaction );
            int loggingInterval = 1;
            if ( World_AIW2.Instance.GameSecond % loggingInterval == 0 && logging )
                ArcenDebugging.ArcenDebugLogSingleLine( " Marauder Faction " + AttachedFaction.FactionIndex + " " + "Current Marauder Budget: " + this.BaseInfo.Budget + " for faction " + AttachedFaction.FactionIndex + " at " + World_AIW2.Instance.GameSecond, Verbosity.DoNotShow );
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( "Marauder Faction " + AttachedFaction.FactionIndex + " " + "Current Marauder Budget: " + this.BaseInfo.Budget + " for faction " + AttachedFaction.FactionIndex + " at " + World_AIW2.Instance.GameSecond + "\n" );

            int marauderUpdateInterval = 10;
            //iterate over ineligiblePlanets and remove any from the list that are now eligible
            int pairCount = this.BaseInfo.IneligiblePlanets.Count;
            if ( veryVerboseDebug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Marauder Faction " + AttachedFaction.FactionIndex + "Checking for ineligible planets. Current budget " + this.BaseInfo.Budget, Verbosity.DoNotShow );
            foreach ( KeyValuePair<Planet, int> pair in this.BaseInfo.IneligiblePlanets )
            {
                if ( pair.Value < World_AIW2.Instance.GameSecond )
                {
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Removing planet " + pair.Key + " from the Ineligible list. Ineligible until " + pair.Value + " < " + World_AIW2.Instance.GameSecond + " (current second)", Verbosity.DoNotShow );
                    this.BaseInfo.IneligiblePlanets.Remove( pair.Key );
                }
            }

            //Let's do these operations less frequently, since we have to iterate over all the marauder units
            if ( veryVerboseDebug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Add or upgrade outposts", Verbosity.DoNotShow );

            if ( World_AIW2.Instance.GameSecond % marauderUpdateInterval == 0 )
            {
                MaybeInvadePlanet(Context, tracingBuffer);
                SpawnInitialOutposts(Context);
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Attempting to update marauder outposts at " + World_AIW2.Instance.GameSecond, Verbosity.DoNotShow );
                AddOrUpgradeOutposts( AttachedFaction, Context, tracingBuffer );
                SpawnRaiders( AttachedFaction, Context, tracingBuffer );
            }

            if ( veryVerboseDebug )
                ArcenDebugging.ArcenDebugLogSingleLine( "setting influence", Verbosity.DoNotShow );

            updateWaveData( AttachedFaction, Context, tracingBuffer );

            if ( veryVerboseDebug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Marauder Faction " + AttachedFaction.FactionIndex + " Exiting perSecond loop", Verbosity.DoNotShow );
            #region Tracing
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " " + AttachedFaction.FactionIndex ).Add( " DoPerSecond trace ends at " ).Add( World_AIW2.Instance.GameSecond );
            if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
        }

        private void PopulateListOfPlanetsToInvade(List<Planet> listToFill, ArcenCharacterBufferBase tracingBuffer )
        {
            bool debug = false;
            bool tracing = tracingBuffer != null;
            if (tracing) {
                tracingBuffer.Add( "Looking for planet to invade:\n" );
            }
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                //FInt hostileMobileStrength = planet.GetPlanetFactionForFaction( faction ).BaseInfoByStance[FactionStance.Hostile].MobileStrength;
                //FInt hostileTotalStrength = planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Hostile].TotalStrength;
                //FInt friendlyTotalStrength = planet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Friendly].TotalStrength;
                //FInt friendlyPlusHRF = friendlyTotalStrength + this.BaseInfo.Budget * this.BaseInfo.Braveness_Constant;
                //For the Marauder, we want to target relatively weak planets
                //that are owned a player

                if ( this.BaseInfo.IneligiblePlanets.ContainsKey( planet ) )
                {
                    if ( tracing )
                        tracingBuffer.Add( $" - {planet.Name} is ineligible until {this.BaseInfo.IneligiblePlanets[planet]}. It is now {World_AIW2.Instance.GameSecond}\n" );
                    continue; //skip planets we have recently sent forces to
                }
                if ( BaseInfo.planetsWithMarauderOutposts.DisplayContainsKey( planet ) )
                {
                    if ( debug && tracing )
                        tracingBuffer.Add( $" - Skipping {planet.Name} because we have a marauder outpost already\n" );
                    continue;
                }
                int enemyStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                int alliedStrength = pFaction.DataByStance[FactionStance.Friendly].TotalStrength + pFaction.DataByStance[FactionStance.Self].TotalStrength;
                int selfStrength = pFaction.DataByStance[FactionStance.Self].TotalStrength;

                PlanetFaction controllingPFac = planet.GetControllingPlanetFaction();
                int controllerStrength = controllingPFac == null ? 0 : controllingPFac.DataByStance[FactionStance.Self].TotalStrength + controllingPFac.DataByStance[FactionStance.Friendly].TotalStrength;
                int controllersEnemiesStrength = controllingPFac == null ? 0 : controllingPFac.DataByStance[FactionStance.Hostile].TotalStrength;
                //int controllerEnemyStrength = planet.GetControllingPlanetFaction().DataByStance[FactionStance.Hostile].TotalStrength;
                //also get the ControllerFactionStrength and forcesHostileToControllerFactionStrength
                //If myAvailableStrength > both of those, we can attack there as well; this will allow the marauders
                //to turn up in the middle of a player vs AI battle, which is awesome
                if ( this.BaseInfo.aiAllied )
                {
                    //if the Marauders are allied with the AI, they aren't allowed to colonize AI planets
                    //since then they would instantly colonize the galaxy and kill you ASAP
                    if ( enemyStrength < alliedStrength )
                    {
                        enemyStrength = alliedStrength;
                        alliedStrength = 0;
                    }
                }
                int myAndAlliedForces = (this.BaseInfo.Budget * this.BaseInfo.Braveness_Constant + alliedStrength + selfStrength).IntValue;
                if ( enemyStrength < myAndAlliedForces )
                {
                    //handles the case where we are simply stronger than the forces on the planet
                    if ( tracing )
                        tracingBuffer.Add( planet.Name + " is eligible, total outnumbered path. Total enemy strength: " + enemyStrength + "  allied strength " + alliedStrength + " self strength " + selfStrength + " marauder budget: " + this.BaseInfo.Budget + " braveness " + this.BaseInfo.Braveness_Constant + " minBudgetForAttack " + BaseInfo.minBudgetForAttack ).Add( "\n" );
                    //if this is a poorly defended planet, it's eligible to be attacked
                    listToFill.Add( planet );
                    continue;
                }
                if ( planet.GetControllingFactionType() != FactionType.NaturalObject &&
                    controllerStrength < myAndAlliedForces && controllersEnemiesStrength < myAndAlliedForces &&
                    this.BaseInfo.Intensity > 5 )
                {
                    //Handle the case where there is a battle going on and we are stronger than each element
                    //of the fight separetely, but not together.
                    if ( tracing )
                        tracingBuffer.Add( planet.Name + " is eligible, individual outnumbered path. Total enemy strength: " + enemyStrength + "  allied strength " + alliedStrength + " self strength " + selfStrength + " controllerStrength " + controllerStrength + " contollersEnemiesStrength " + controllersEnemiesStrength + " marauder budget: " + this.BaseInfo.Budget + " braveness " + this.BaseInfo.Braveness_Constant + " minBudgetForAttack " + BaseInfo.minBudgetForAttack ).Add( "\n" );

                    listToFill.Add( planet );
                    continue;
                }
                if ( planet.GetControllingFactionType() != FactionType.NaturalObject &&
                    Math.Abs( controllerStrength - controllersEnemiesStrength ) < myAndAlliedForces &&
                    myAndAlliedForces > controllerStrength / 4 && //make sure we're strong enough to have a real chance though
                    this.BaseInfo.Intensity > 7 )
                {
                    //aggressive case: we are not stronger than either side, but if they continue to fight then
                    //we might be able to pick up the pieces. Only available on higher intensity
                    if ( tracing )
                        tracingBuffer.Add( planet.Name + " is eligible, combined outnumbered path. Total enemy strength: " + enemyStrength + "  allied strength " + alliedStrength + " self strength " + selfStrength + " marauder budget: " + this.BaseInfo.Budget + " braveness " + this.BaseInfo.Braveness_Constant + " minBudgetForAttack " + BaseInfo.minBudgetForAttack + " controllerStrength " + controllerStrength + " controllersEnemiesStrength " + controllersEnemiesStrength + " my and allied forces " + myAndAlliedForces ).Add( "\n" );

                    listToFill.Add( planet );
                    continue;
                }
            }
        }

        private void InvadePlanet(ArcenHostOnlySimContext Context, Planet targetPlanet, ArcenCharacterBufferBase tracingBuffer )
        {
            //Log a suitable message to the player
            if ( targetPlanet.IntelLevel > PlanetIntelLevel.Unexplored && ArcenNetworkAuthority.GetIsHostMode() )
                GenerateLogMessageForPlayer( targetPlanet, AttachedFaction, Context );

            //Now figure out where the units are going and spawn them
            AngleDegrees angle = AngleDegrees.Create( (float)Context.RandomToUse.Next( 1, 360 ) );
            ArcenPoint center = Engine_AIW2.Instance.CombatCenter;
            float warpInMultiplier = 0.9f;
            ArcenPoint spawnLocation = center.GetPointAtAngleAndDistance( angle, (int)(targetPlanet.GravWellSize.DistanceScale_GravwellRadius * warpInMultiplier) );
            ArcenPoint WarpInStart = center.GetPointAtAngleAndDistance( angle, targetPlanet.GravWellSize.DistanceScale_GravwellRadius );
            if ( tracingBuffer != null )
                tracingBuffer.Add( "Marauders faction " + AttachedFaction.FactionIndex + "  with budget " + this.BaseInfo.Budget + " spawning on planet " + targetPlanet.Name + "; it will remain ineligible until " + this.BaseInfo.IneligiblePlanets[targetPlanet]);
            while ( this.BaseInfo.Budget >= FInt.Zero )
            {
                GameEntityTypeData entityData = null;
                if ( this.BaseInfo.Intensity < 7 )
                {
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "LowHRFSpawn" );
                }
                else
                {
                    if ( Context.RandomToUse.Next( 0, 10 ) == 1 ) //sometimes spawn some stronger units
                        entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "HighHRFSpawn" );
                    else
                        entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "LowHRFSpawn" );
                }
                if ( entityData == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "No valid entities were found to spawn", Verbosity.DoNotShow );
                    break;
                }
                PlanetFaction pFaction = targetPlanet.GetPlanetFactionForFaction( AttachedFaction );
                GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                        //Chris notes: these marauder are not from an outpost I guess, so they are just thrown in the loose fleet of the maurauder faction
                        pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "Marauders-GeneralBudget" );  //is fine, main sim thread
                if ( entity != null )
                {
                    entity.spawnVis = SpawnVisualization.WarpIn;

                    entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                }
                this.BaseInfo.Budget -= entityData.MarkStatsFor( pFaction.Faction.CurrentGeneralMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership;
            }
        }

        /// <summary>figure out if we should attack any planet with new forces</summary>
        private void MaybeInvadePlanet(ArcenHostOnlySimContext Context, ArcenCharacterBufferBase tracingBuffer )
        {
            bool tracing = tracingBuffer != null;

            // If we don't have budget for an attack, do nothing
            if ( this.BaseInfo.Budget < BaseInfo.minBudgetForAttack ) {
                if ( tracing )
                    tracingBuffer.Add( $"Not enough budget to invade: {this.BaseInfo.Budget} < {BaseInfo.minBudgetForAttack}\n" );
                return;
            }

            List<Planet> potentialPlanets = Planet.GetTemporaryPlanetList( "MarauderFactionDeepInfo-MaybeInvadePlanet-potentialPlanets", 10f );
            if ( potentialPlanets == null ) //blocked for teardown/shutdown; bail
                return;

            PopulateListOfPlanetsToInvade(potentialPlanets, tracingBuffer);


            Planet targetPlanet = null;
            if ( potentialPlanets.Count == 0 ) {
                if ( tracing )
                    tracingBuffer.Add( "Found no planets to invade.\n" );
                Planet.ReleaseTemporaryPlanetList(potentialPlanets);
                return;
            }

            //Pick a random eligible planet, then decide whether to attack it
            //If we do attack it, spawn a bunch of ships on the planet
            //Whether we attack or not, add it to the IneligiblePlanets list
            targetPlanet = potentialPlanets[Context.RandomToUse.Next( 0, potentialPlanets.Count )];
            this.BaseInfo.IneligiblePlanets[targetPlanet] = World_AIW2.Instance.GameSecond + this.BaseInfo.IneligibleSeconds;
            if ( tracing )
                tracingBuffer.Add( $"Planet {targetPlanet.Name} chosen at random from a set of {potentialPlanets.Count} is now ineligible until {World_AIW2.Instance.GameSecond + this.BaseInfo.IneligibleSeconds} aka {this.BaseInfo.IneligibleSeconds} seconds\n" );

            Planet.ReleaseTemporaryPlanetList(potentialPlanets);

            int randomNum = Context.RandomToUse.Next( 0, 100 );
            if ( randomNum < this.BaseInfo.ChanceToAttack ) {
                //The Marauders only attack sometimes
                if ( tracing )
                    tracingBuffer.Add( $"- not attacking; {randomNum} out of {this.BaseInfo.ChanceToAttack}\n" );
                return;
            }
            if ( tracing ) {
                tracingBuffer.Add( $"- attacking; {randomNum} out of {this.BaseInfo.ChanceToAttack}\n" );
            }
            InvadePlanet(Context, targetPlanet, tracingBuffer);
        }

        private bool ShouldSpawnOutpost(Planet planet) {
            if ( BaseInfo.planetsWithMarauderOutposts.DisplayContainsKey(planet) ) {
                return false;
            }
            var stanceData = planet.GetStanceDataForFaction(this.AttachedFaction);
            int hostileStrength = stanceData[FactionStance.Hostile].TotalStrength;
            int myStrength = stanceData[FactionStance.Self].TotalStrength;

            if (hostileStrength >= myStrength / 10) {
                return false;
            }
            if (planet.GetControllingFaction().GetIsHostileTowards( AttachedFaction ) && stanceData[FactionStance.Hostile].NonGuardMobileStrength >= 100) {
                return false;
            }
            if ( planet.GetControllingFaction().SpecialFactionData.InternalName == "ZenithArchitrave" &&
                    planet.GetControllingFaction().GetIsFriendlyTowards( AttachedFaction ) )
            {
                //During Civil Wars (or player truces for player-allied marauders), ZAs will potentially be allied to the marauders temporarily. Don't let the marauders
                //build on a ZA planet unless they really are friendly
                if ( AttachedFaction.BaseInfo.Allegiance != planet.GetControllingFaction().BaseInfo.Allegiance )
                    return false;
            }
            return true;
        }

        /* If the marauders have conquered a planet then they will attempt to fortify it against you.
           Fortification works as follows. As long as the Marauders maintain an outpost then every
           outpostSpawnInterval a new Mark 1 outpost will be spawned, up to MaxOutpostsPerPlanet.
           If a Mark 1 outpost has been around timeForMarkIIUpgrade seconds then it upgrades to Mark 2,
           and similarly Mark 2 upgrades to Mark 3 */
        private void SpawnInitialOutposts(ArcenHostOnlySimContext Context)
        {
            List<Planet> potentialPlanets = Planet.GetTemporaryPlanetList( "MarauderFactionDeepInfo-SpawnIntialOutposts-potentialPlanets", 10f );
            if ( potentialPlanets == null ) //blocked for teardown/shutdown; bail
                return;
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads() )
            {
                if ( entity.TypeData.IsMobile && !entity.TypeData.IsDrone ) {
                    Planet planet = entity.Planet;
                    if (this.ShouldSpawnOutpost(planet)) {
                        potentialPlanets.AddIfNotAlreadyIn(planet);
                    }
                }
            }

            foreach (Planet spawnPlanet in potentialPlanets) {
                string voiceLine = "";
                if ( spawnPlanet.GetControllingFactionType() == FactionType.Player && this.BaseInfo.PlayerAllied )
                    spawningBuffer.Add( "Friendly Marauders", AttachedFaction.FactionCenterColor.ColorHexBrighter ).Add( " are fortifying an outpost on  " ).Add( spawnPlanet.Name, spawnPlanet.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( " to aid in its defense" );
                else if ( spawnPlanet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    spawningBuffer.Add( "Marauders", AttachedFaction.FactionCenterColor.ColorHexBrighter ).Add( " are fortifying an outpost on  " ).Add( spawnPlanet.Name, spawnPlanet.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( " after devastating its defenses" );

                PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                if ( chatHandlerOrNull != null )
                    chatHandlerOrNull.PlanetToView = spawnPlanet;

                World_AIW2.Instance.QueueChatMessageOrCommand( spawningBuffer.GetStringAndResetForNextUpdate(),
                        ChatType.LogToCentralChat, voiceLine, chatHandlerOrNull );

                GameEntity_Squad outpost = SpawnMark1Outpost_MayReturnNull( spawnPlanet, AttachedFaction, Context );
                if ( outpost != null )
                    spawnMarauderTurrets( AttachedFaction, Context, outpost, this.BaseInfo.TurretsPerMark1Outpost, "MarauderMark1TurretOutpost" );
                this.BaseInfo.TotalPlanetsCaptured++;
            }
            Planet.ReleaseTemporaryPlanetList(potentialPlanets);
        }

        private static ArcenDoubleCharacterBuffer generateLogMessageForPlayerBuffer = new ArcenDoubleCharacterBuffer( "MarauderFactionDeepInfo-generateLogMessageForPlayerBuffer" );
        private void GenerateLogMessageForPlayer( Planet targetPlanet, Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( !ArcenNetworkAuthority.GetIsHostMode() ) //only for host
                return; //only queue this on the host, since it seems to happen everywhere
            ArcenDoubleCharacterBuffer buffer = generateLogMessageForPlayerBuffer;
            Faction controllingFaction = targetPlanet.GetControllingFaction();
            Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
            PlanetFaction pFaction = targetPlanet.GetPlanetFactionForFaction( localFaction );

            if ( controllingFaction.Type == FactionType.Player )
            {
                PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                if ( chatHandlerOrNull != null )
                    chatHandlerOrNull.PlanetToView = targetPlanet;

                if ( this.BaseInfo.PlayerAllied )
                {
                    buffer.Add( "Friendly Marauders", faction.FactionCenterColor.ColorHexBrighter ).Add( " are reinforcing player planet " ).Add( targetPlanet.Name, controllingFaction.FactionCenterColor.ColorHexBrighter );
                    World_AIW2.Instance.QueueChatMessageOrCommand( buffer.GetStringAndResetForNextUpdate(), ChatType.LogToCentralChat, "ArkChiefOfStaff_FriendlyMaraudersReinforcingPlayer", chatHandlerOrNull );
                }
                else
                {
                    buffer.Add( "Hostile Marauders", faction.FactionCenterColor.ColorHexBrighter ).Add( " are attacking player planet " ).Add( targetPlanet.Name, controllingFaction.FactionCenterColor.ColorHexBrighter );
                    World_AIW2.Instance.QueueChatMessageOrCommand( buffer.GetStringAndResetForNextUpdate(), ChatType.LogToCentralChat, "ArkChiefOfStaff_MaraudersAttackingPlayerPlanet", chatHandlerOrNull );
                }
            }
            else if ( controllingFaction.GetIsFriendlyTowards( localFaction ) )
            {
                PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                if ( chatHandlerOrNull != null )
                    chatHandlerOrNull.PlanetToView = targetPlanet;

                buffer.Add( "Marauders", faction.FactionCenterColor.ColorHexBrighter ).Add( " are attacking friendly planet " ).Add( targetPlanet.Name, controllingFaction.FactionCenterColor.ColorHexBrighter );
                World_AIW2.Instance.QueueChatMessageOrCommand( buffer.GetStringAndResetForNextUpdate(), ChatType.LogToCentralChat, "ArkChiefOfStaff_MaraudersAttackingFriendlyPlanet", chatHandlerOrNull );
            }
            else if ( controllingFaction.GetIsHostileTowards( localFaction ) )
            {
                PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                if ( chatHandlerOrNull != null )
                    chatHandlerOrNull.PlanetToView = targetPlanet;

                if ( this.BaseInfo.PlayerAllied )
                    buffer.Add( "Friendly Marauders", faction.FactionCenterColor.ColorHexBrighter );
                else
                    buffer.Add( "Hostile Marauders", faction.FactionCenterColor.ColorHexBrighter );
                buffer.Add( " attacking " ).Add( targetPlanet.Name, controllingFaction.FactionCenterColor.ColorHexBrighter );

                if ( pFaction.DataByStance[FactionStance.Self].TotalStrength > 1000 )
                    World_AIW2.Instance.QueueChatMessageOrCommand( buffer.GetStringAndResetForNextUpdate(), ChatType.LogToCentralChat, "ArkChiefOfStaff_MaraudersAttackingPlanetPlayerIsAttacking", chatHandlerOrNull );
                else
                    World_AIW2.Instance.QueueChatMessageOrCommand( buffer.GetStringAndResetForNextUpdate(), ChatType.LogToCentralChat, "ArkChiefOfStaff_MaraudersAttackingEnemyPlanet", chatHandlerOrNull );
            }
            else if ( controllingFaction.Type == FactionType.NaturalObject && targetPlanet.IntelLevel > PlanetIntelLevel.Unexplored &&
                     !this.BaseInfo.PlayerAllied )
            {
                PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                if ( chatHandlerOrNull != null )
                    chatHandlerOrNull.PlanetToView = targetPlanet;

                //for non player-allied marauders, if they are attacking a neutral planet
                buffer.Add( "Marauders", faction.FactionCenterColor.ColorHexBrighter ).Add( " attacking neutral planet " ).Add( targetPlanet.Name ).Add( "." );
                World_AIW2.Instance.QueueChatMessageOrCommand( buffer.GetStringAndResetForNextUpdate(), ChatType.LogToCentralChat, "ArkChiefOfStaff_MaraudersPresentOnUnownedPlanet", chatHandlerOrNull );
            }
        }
        private void updateWaveData( Faction faction, ArcenHostOnlySimContext Context, ArcenCharacterBufferBase tracingBuffer )
        {
            //update the information about the next wave coming toward this faction
            bool debug = false;
            bool tracing = tracingBuffer != null;

            if ( BaseInfo.planetsWithMarauderOutposts.Count <= 0 )
            {
                //if the marauders don't have any planets, the AI doesn't worry about them
                return;
            }
            
            bool hasHostileAi = false;
            foreach ( Faction f in World_AIW2.Instance.AIFactions )
            {
                if ( f.GetIsHostileTowards( AttachedFaction ) )
                {
                    hasHostileAi = true;
                }
            }
            if (!hasHostileAi)
            {
                //if the marauders are allies, the AI doesn't worry about them
                return;
            }

            FInt budgetMultiplier = BaseInfo.GetBudgetMutliplier();
            if ( World_AIW2.Instance.GameSecond % 60 == 0 )
            {
                //use the smaller of "current AIP" and "marauder AIP"
                FInt aipToUse = FactionUtilityMethods.Instance.GetCurrentAIP();
                if ( aipToUse > BaseInfo.MarauderSpecificAIP )
                    aipToUse = BaseInfo.MarauderSpecificAIP;
                FInt increase = budgetMultiplier * aipToUse * BaseInfo.BaseWaveBudgetPerMinute;
                this.BaseInfo.WaveData.currentWaveBudget += increase;
                if ( tracing ) tracingBuffer.Add( "Marauder anti-self wave budget is " + this.BaseInfo.WaveData.currentWaveBudget + " and most recent increase was " + increase + "\n" );
            }
            if ( this.BaseInfo.WaveData.timeForNextWave <= World_AIW2.Instance.GameSecond && this.BaseInfo.WaveData.currentWaveBudget >= BaseInfo.MinWaveSize )
            {
                int budgetSpent = AntiMinorFactionWaveData.QueueWave( faction, Context, this.BaseInfo.WaveData.currentWaveBudget.GetNearestIntPreferringHigher() );
                if ( budgetSpent == FInt.Zero )
                {
                    //the wave wasn't actually sent; presumably there are no valid targets
                    //start checking every minute
                    this.BaseInfo.WaveData.timeForNextWave = World_AIW2.Instance.GameSecond + 60;
                }
                else
                    this.BaseInfo.WaveData.timeForNextWave = World_AIW2.Instance.GameSecond + this.BaseInfo.WaveInterval * 60;
                this.BaseInfo.WaveData.currentWaveBudget -= budgetSpent;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "launching wave at " + World_AIW2.Instance.GameSecond + " amd spent budget " + budgetSpent + " next wave at " + this.BaseInfo.WaveData.timeForNextWave, Verbosity.DoNotShow );

                if ( tracing ) tracingBuffer.Add( "Marauder requesting wave at " + World_AIW2.Instance.GameSecond + " with budget " + this.BaseInfo.WaveData.currentWaveBudget + " and next wave scheduled for " + this.BaseInfo.WaveData.timeForNextWave + "\n" );
            }
        }
        private void SpawnRaiders( Faction faction, ArcenHostOnlySimContext Context, ArcenCharacterBufferBase tracingBuffer )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            #region Tracing
            bool debug = false;
            bool tracing = tracingBuffer != null;
            #endregion
            //Due to stacking, we can't be precise about knowing which outposts have spawned which raiders.
            //So we also use a global cap
            if ( BaseInfo.numMark3Outposts.Display * BaseInfo.MaxRaidersPerMark3Outpost + 10 <= BaseInfo.TotalRaiders )
            {
                if ( tracing ) tracingBuffer.Add( "There are " + BaseInfo.numMark3Outposts + " outposts with " + BaseInfo.MaxRaidersPerMark3Outpost + " raiders supported per outpost. We have a total of " + BaseInfo.TotalRaiders + " raiders. This is too many, and probably caused by stacking. Do not make more raiders now.\n" );
                return;
            }

            List<SafeSquadWrapper> outposts = this.BaseInfo.Outposts.GetDisplayList();
            for ( int i = 0; i < outposts.Count; i++ )
            {
                GameEntity_Squad entity = outposts[i].GetSquad();
                if ( entity == null )
                    continue;
                if ( entity.TypeData.GetHasTag( "MarauderOutpost" ) && entity.CurrentMarkLevel == 3 )
                {
                    MarauderOutpostRaiderPerUnitBaseInfo data = entity.CreateExternalBaseInfo<MarauderOutpostRaiderPerUnitBaseInfo>( "MarauderOutpostRaiderPerUnitBaseInfo" );
                    if ( tracing && debug ) tracingBuffer.Add( "Mark 3 outpost " + entity.PrimaryKeyID + " on " + entity.GetPlanetName_Safe() + " has " + BaseInfo.RaidersPerOutpost.Display[entity.PrimaryKeyID] + " marauders now with a max of " + this.BaseInfo.MaxRaidersPerMark3Outpost + "\n" );
                    if ( BaseInfo.RaidersPerOutpost.Display[entity.PrimaryKeyID] >= this.BaseInfo.MaxRaidersPerMark3Outpost )
                        continue;

                    int actualRaiderSpawnInterval = (int)(this.BaseInfo.BaseRaiderSpawnInterval / this.BaseInfo.GetBudgetMutliplier());
                    if ( this.BaseInfo.PlayerAllied )
                    {
                        actualRaiderSpawnInterval = (int)(this.BaseInfo.BaseRaiderSpawnIntervalPlayerAllied / this.BaseInfo.GetBudgetMutliplier());
                    }
                    if ( World_AIW2.Instance.GameSecond >= data.LastRaiderSummonTime + actualRaiderSpawnInterval )
                    {
                        GameEntityTypeData entityData = null;
                        if ( faction.HasObtainedSpireDebris )
                            entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MarauderRaider" );
                        else
                            entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MarauderRaiderBasic" ); //doesn't include spire
                        if ( entityData == null )
                            throw new Exception( "No unit with MarauderRaider tag defined in the XML" );
                        PlanetFaction pFaction = entity.Planet.GetPlanetFactionForFaction( faction );
                        data.LastRaiderSummonTime = World_AIW2.Instance.GameSecond;

                        //Now spawn the new raider and set its data
                        GameEntity_Squad newRaider = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                            //this entity is the outpost doing the spawning!
                            //this will remember how many raiders are alive more directly
                            entity.GetFleetOrNull_Safe(), 0, entity.WorldLocation, Context, "Marauders-SpawnRaiders" );
                        if ( newRaider != null )
                        {
                            MarauderOutpostRaiderPerUnitBaseInfo newRaiderData = newRaider.CreateExternalBaseInfo<MarauderOutpostRaiderPerUnitBaseInfo>( "MarauderOutpostRaiderPerUnitBaseInfo" );
                            newRaiderData.OutpostId = entity.PrimaryKeyID;
                            newRaider.MinorFactionStackingID = entity.PrimaryKeyID;
                        }
                    }
                }

            }
        }
        private readonly Dictionary<Planet, MarauderOutpostData> outpostData = Dictionary<Planet, MarauderOutpostData>.Create_WillNeverBeGCed( 100, "MarauderFactionDeepInfo-outpostData" );
        private void AddOrUpgradeOutposts( Faction faction, ArcenHostOnlySimContext Context, ArcenCharacterBufferBase tracingBuffer )
        {
            //if a Marauder Outpost has been around long enough, upgrade it to the next highest version
            //so if you ignore marauders for long enough they will get more of them

            //Also add a new Mark1 Marauder outpost if necessary.
            //To do this, find every planet with MarauderOutposts; for each of those We need to know
            //A. how old the youngest Mark1 outpost spawned is
            //B. how many total outposts there on on the planet

            //Also sets the global value for numMark3Outposts;
            #region Tracing
            bool debug = false;
            bool tracing = tracingBuffer != null;
            #endregion

            BaseInfo.numMark3Outposts.ClearConstructionValueForStartingConstruction();
            bool localDebug = false;
            int secondsForMarkIIUpgrade = this.BaseInfo.timeForMarkIIUpgrade;
            int secondsForMarkIIIUpgrade = this.BaseInfo.timeForMarkIIIUpgrade;
            int outpostSpawnInterval = this.BaseInfo.outpostSpawnInterval;
            int MaxOutposts;
            if ( outpostSpawnInterval >= secondsForMarkIIUpgrade )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: for Human Marauders, outpostSpawnInterval must be < secondsForMarkIIUpgrade so the code can correctly spawn new outposts", Verbosity.DoNotShow ); //note that I could be more clever by timing how long a Mark II outpost has been around, but let's not bother with that for now
                outpostSpawnInterval = secondsForMarkIIUpgrade - 1;
            }
            outpostData.Clear();
            List<SafeSquadWrapper> outposts = this.BaseInfo.Outposts.GetDisplayList();
            for ( int i = 0; i < outposts.Count; i++ )
            {
                GameEntity_Squad entity = outposts[i].GetSquad();
                if ( entity == null )
                    continue;

                if ( entity.TypeData.GetHasTag( "WarpingInMarauderOutpost" ) )
                {
                    if ( !outpostData.ContainsKey( entity.Planet ) )
                    {
                        outpostData[entity.Planet] = new MarauderOutpostData( 0, 0 );
                    }
                    MarauderOutpostData outpost = outpostData[entity.Planet];
                    outpost.HasWarpingInOutposts = true;
                    outpostData[entity.Planet] = outpost;
                }
                if ( !entity.TypeData.GetHasTag( "MarauderOutpost" ) )
                    continue;

                if ( entity.CurrentMarkLevel == 1 )
                {
                    if ( !outpostData.ContainsKey( entity.Planet ) )
                    {
                        outpostData[entity.Planet] = new MarauderOutpostData( 1, entity.GetSecondsSinceCreation() );
                    }
                    else if ( outpostData.ContainsKey( entity.Planet ) )
                    {
                        MarauderOutpostData outpost = outpostData[entity.Planet];
                        outpost.numOutposts++;
                        if ( outpost.timeYoungestMark1HasExisted > entity.GetSecondsSinceCreation() || outpost.timeYoungestMark1HasExisted == -1 )
                            outpost.timeYoungestMark1HasExisted = entity.GetSecondsSinceCreation();
                        outpostData[entity.Planet] = outpost;
                    }
                    if ( tracing && debug ) tracingBuffer.Add( "Found Mark 1 marauder outpost on " + entity.GetPlanetName_Safe() + ". There are " + outpostData[entity.Planet].numOutposts + " outposts so far, and it is " + entity.GetSecondsSinceCreation() + " seconds old (upgrades at " + secondsForMarkIIUpgrade + ")" ).Add( "\n" );

                    if ( entity.GetSecondsSinceCreation() >= secondsForMarkIIUpgrade )
                    {
                        if ( tracing ) tracingBuffer.Add( "Upgrading a Mark 1 marauder outpost on " + entity.GetPlanetName_Safe() + " it has been around for " + entity.GetSecondsSinceCreation() + " seconds" ).Add( "\n" );

                        entity.SetCurrentMarkLevel( 2 );
                        spawnMarauderTurrets( faction, Context, entity, this.BaseInfo.TurretsPerMark2Outpost, "MarauderMark2TurretOutpost" );
                        continue;
                    }
                }
                if ( entity.CurrentMarkLevel == 2 )
                {
                    if ( !outpostData.ContainsKey( entity.Planet ) )
                    {
                        outpostData[entity.Planet] = new MarauderOutpostData( 1, -1 );
                    }
                    else if ( outpostData.ContainsKey( entity.Planet ) )
                    {
                        MarauderOutpostData outpost = outpostData[entity.Planet];
                        outpost.numOutposts++;
                        if ( outpost.timeYoungestMark1HasExisted > entity.GetSecondsSinceCreation() || outpost.timeYoungestMark1HasExisted == -1 )
                            outpost.timeYoungestMark1HasExisted = entity.GetSecondsSinceCreation();

                        outpostData[entity.Planet] = outpost;
                    }
                    if ( tracing && debug ) tracingBuffer.Add( "Found Mark 2 marauder outpost on " + entity.GetPlanetName_Safe() + ". There are " + outpostData[entity.Planet].numOutposts + " outposts so far, and it is " + entity.GetSecondsSinceCreation() + " seconds old (upgrades at " + secondsForMarkIIIUpgrade + ")\n" );
                    //Check whether we are allowed to upgrade this outpost
                    if ( this.BaseInfo.NoMark3Outposts || (this.BaseInfo.NoMark3OutpostsOnAlliedPlanets && entity.Planet.GetControllingFaction().GetIsFriendlyTowards( faction )) )
                        continue;

                    if ( entity.GetSecondsSinceCreation() >= secondsForMarkIIIUpgrade )
                    {
                        if ( tracing && debug ) tracingBuffer.Add( "Upgrading a Mark II1 marauder outpost on " + entity.GetPlanetName_Safe() + " it has been around for " + entity.GetSecondsSinceCreation() + " seconds\n" );
                        entity.SetCurrentMarkLevel( 3 );
                        spawnMarauderTurrets( faction, Context, entity, this.BaseInfo.TurretsPerMark3Outpost, "MarauderMark3TurretOutpost" );
                    }
                }
                if ( entity.CurrentMarkLevel == 3 )
                {
                    if ( !outpostData.ContainsKey( entity.Planet ) )
                    {
                        outpostData[entity.Planet] = new MarauderOutpostData( 1, -1 );
                    }
                    else if ( outpostData.ContainsKey( entity.Planet ) )
                    {
                        MarauderOutpostData outpost = outpostData[entity.Planet];
                        outpost.numOutposts++;
                        if ( outpost.timeYoungestMark1HasExisted > entity.GetSecondsSinceCreation() || outpost.timeYoungestMark1HasExisted == -1 )
                            outpost.timeYoungestMark1HasExisted = entity.GetSecondsSinceCreation();

                        outpostData[entity.Planet] = outpost;
                    }

                    if ( tracing && debug ) tracingBuffer.Add( "Found Mark 3 marauder outpost on " + entity.GetPlanetName_Safe() + ". There are " + outpostData[entity.Planet].numOutposts + " outposts so far, and it is " + entity.GetSecondsSinceCreation() + " seconds old (upgrades at " + secondsForMarkIIIUpgrade + ")\n" );

                    if ( this.BaseInfo.NoMark3OutpostsOnAlliedPlanets && entity.Planet.GetControllingFaction().GetIsFriendlyTowards( faction ) )
                    {
                        //Downgrade mark 3 outposts if there's an allied command station here.
                        //the thought is that you're now taking over a Friendly Marauder planet,
                        //that had some Mark 3 outposts
                        if ( tracing ) tracingBuffer.Add( "Downgrading Mark 3 marauder outpost on " + entity.GetPlanetName_Safe() + "\n" );
                        entity.SetCurrentMarkLevel( 2 );
                        continue;
                    }

                    BaseInfo.numMark3Outposts.Construction++;
                }
            }


            BaseInfo.numMark3Outposts.SwitchConstructionToDisplay();

            foreach ( KeyValuePair<Planet, MarauderOutpostData> data in outpostData )
            {
                if ( data.Key.GetControllingFaction().GetIsFriendlyTowards( faction ) )
                    MaxOutposts = this.BaseInfo.MaxOutpostsPerAlliedPlanet;
                else
                    MaxOutposts = this.BaseInfo.MaxOutpostsPerPlanet;
                if ( tracing ) tracingBuffer.Add( "Found outposts on " + data.Key.Name + " there are " + data.Value.numOutposts + " outposts and the youngest Mark 1 is " + data.Value.timeYoungestMark1HasExisted + ". Has Warping In Outposts: " + data.Value.HasWarpingInOutposts + " Max outposts for this planet are " + MaxOutposts ).Add( "\n" );
                if ( data.Value.numOutposts < MaxOutposts && data.Value.timeYoungestMark1HasExisted >= outpostSpawnInterval && !data.Value.HasWarpingInOutposts )
                {
                    if ( tracing ) tracingBuffer.Add( "Spawning a new outpost on " + data.Key.Name + " there are currently " + data.Value.numOutposts + " outposts and the youngest Mark 1 is " + data.Value.timeYoungestMark1HasExisted + " and no warping in outposts\n" );
                    GameEntity_Squad outpost = SpawnMark1Outpost_MayReturnNull( data.Key, faction, Context );
                    if ( outpost != null )
                    {
                        if ( localDebug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Mark 1 outpost spawned. Now spawn " + this.BaseInfo.TurretsPerMark1Outpost + " turrets to go with it", Verbosity.DoNotShow );
                        spawnMarauderTurrets( faction, Context, outpost, this.BaseInfo.TurretsPerMark1Outpost, "MarauderMark1TurretOutpost" );
                    }
                }
                else
                {
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Not Spawning a new outpost on " + data.Key.Name + " there are currently " + data.Value.numOutposts + " outposts and the youngest Mark 1 is " + data.Value.timeYoungestMark1HasExisted + " and the outposet spawn interval is " + outpostSpawnInterval, Verbosity.DoNotShow );
                }
            }
            if ( tracing ) tracingBuffer.Add( this.TracingName ).Add( " Marauder outpost spawn code ends\n" );
        }

        private GameEntity_Squad SpawnMark1Outpost_MayReturnNull( Planet planet, Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return null; //don't even try this on clients

            bool localDebug = false;
            if ( localDebug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Spawning a new Mark 1 outpost on " + planet.Name, Verbosity.DoNotShow );
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "WarpingInMarauderOutpost" );
            //AngleDegrees angle = AngleDegrees.Create( (FInt)Context.RandomToUse.Next( 1, 360 ) );
            ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 400 ) );
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );

            //we're going to create a new fleet just for this outpost, and assign ships to it from then on
            Fleet marauderOutpostFleet = Fleet.Create_CallFromHostOnly( FleetCategory.NPC, faction, null, null );

            GameEntity_Squad outpost = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                marauderOutpostFleet, 0, spawnLocation, Context, "Marauders-Mk1Outpost" );
            //mark this outpost as the centerpiece of the fleet, now that we've created it
            marauderOutpostFleet.Centerpiece = LazyLoadSquadWrapper.Create( outpost );
            outpost.TransformsIntoAfterTime = "MarauderOutpost";
            outpost.SecondsTillTransformation = (Int16)Context.RandomToUse.Next( 25, 100 );
            if ( this.BaseInfo.PlayerAllied )
            {
                MarauderOutpostRaiderPerUnitBaseInfo outpostdata = outpost.CreateExternalBaseInfo<MarauderOutpostRaiderPerUnitBaseInfo>( "MarauderOutpostRaiderPerUnitBaseInfo" );
                outpostdata.isHumanAligned = true;
            }

            return outpost;
        }

        private void spawnMarauderTurrets( Faction faction, ArcenHostOnlySimContext Context, GameEntity_Squad outpost, int numTurrets, string turretTag )
        {
            bool debug = false;
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Spawning " + numTurrets + " turrets around " + outpost.ToStringWithPlanet(), Verbosity.DoNotShow );
            for ( int i = 0; i < numTurrets; i++ )
            {
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, turretTag );
                if ( entityData == null )
                    throw new Exception( "HumanMarauders: could not find turret with the tag " + turretTag );
                ArcenPoint spawnLocation = outpost.Planet.GetSafePlacementPoint_AroundEntity( Context, entityData, outpost, FInt.FromParts( 0, 50 ), FInt.FromParts( 0, 100 ) );
                PlanetFaction pFaction = outpost.Planet.GetPlanetFactionForFaction( faction );
                // GameEntity_Squad.CreateNew( pFaction, entityData, entityData.MarkFor( pFaction ),
                //     //add these to the fleet of the outpost creating them!
                //     outpost.GetFleetOrNull_Safe(), 0, spawnLocation, Context );
                //Badger note: using the fleetmembership of the outpost seems to make the turrets despawn. Lets brute force this
                GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                                            outpost.PlanetFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "Marauders-Turrets" );
            }
        }
        
        private void updateBudget( Faction faction )
        {
            bool localdebug = false;

            FInt budgetPerSecond;
            if ( this.BaseInfo.Intensity < 4 )
                budgetPerSecond = this.BaseInfo.BudgetPerSecondLowIntensity;
            else if ( this.BaseInfo.Intensity < 7 )
                budgetPerSecond = this.BaseInfo.BudgetPerSecondMediumIntensity;
            else
                budgetPerSecond = this.BaseInfo.BudgetPerSecondHighIntensity;
            FInt BudgetIncreaseForPlanetCapture = this.BaseInfo.BudgetIncreaseForPlanetCapture;
            FInt BonusBudgetPerMark3Outpost = BaseInfo.BonusBudgetPerMarkIIIOutpost;
            if ( World_AIW2.Instance.GameSecond == 1 )
                this.BaseInfo.Budget = BaseInfo.StartingBudget; //the starting budget is mostly for testing

            if ( this.BaseInfo.Budget < FInt.Zero )
                this.BaseInfo.Budget = FInt.Zero; //it's possible to overspend if the last item purchased is pricey
            FInt multiplier = BaseInfo.GetBudgetMutliplier();
            FInt budgetIncrease = budgetPerSecond * multiplier + BonusBudgetPerMark3Outpost * BaseInfo.numMark3Outposts.Display;
            this.BaseInfo.Budget += budgetIncrease;
            int numHoursIntoGame = (World_AIW2.Instance.GameSecond / 3600) + 1;
            //the budget for the HRF BaseMaxBudget * number of hours into the game * MAX(intensity multiplier, 1)
            FInt MaxBudget = BaseInfo.BaseMaxBudget * numHoursIntoGame * multiplier + this.BaseInfo.TotalPlanetsCaptured * BudgetIncreaseForPlanetCapture;
            if ( this.BaseInfo.Budget > MaxBudget )
                this.BaseInfo.Budget = MaxBudget;
            if ( localdebug )
                ArcenDebugging.ArcenDebugLogSingleLine( "BaseBudget " + budgetPerSecond + " multiplier " + multiplier + " IncreaseThisSecond: " + budgetIncrease + " Current Budget: " + this.BaseInfo.Budget + " Max budget: " + MaxBudget + " total planet captured so far: " + this.BaseInfo.TotalPlanetsCaptured + " with a bonus of " + this.BaseInfo.TotalPlanetsCaptured * BudgetIncreaseForPlanetCapture, Verbosity.DoNotShow );

        }

        public override void UpdatePlanetInfluence_HostOnly( ArcenHostOnlySimContext Context )
        {
            List<Planet> planetsInfluenced = Planet.GetTemporaryPlanetList( "Marauder-UpdatePlanetInfluence_HostOnly-planetsInfluenced", 10f );
            if ( planetsInfluenced == null ) //blocked for teardown/shutdown; bail
                return;

            Dictionary<Planet,bool> planetsWithOutPosts = this.BaseInfo.planetsWithMarauderOutposts.GetDisplayDict();
            foreach ( KeyValuePair<Planet, bool> kv in planetsWithOutPosts )
            {
                planetsInfluenced.AddIfNotAlreadyIn( kv.Key );
                PlanetFaction pFaction = kv.Key.GetPlanetFactionForFaction( AttachedFaction );
                if ( pFaction.AIPLeftFromCommandStation != 0 )
                {
                    MinorFactionAIPEquivalentIncrease( (FInt)pFaction.AIPLeftFromCommandStation + pFaction.AIPLeftFromWarpGate );
                    pFaction.AIPLeftFromCommandStation = 0;
                    pFaction.AIPLeftFromWarpGate = 0;
                }
            }

            AttachedFaction.SetInfluenceForPlanetsToList( planetsInfluenced );
            Planet.ReleaseTemporaryPlanetList( planetsInfluenced );
        }

        public override void MinorFactionAIPEquivalentIncrease( FInt AIPEquivalent )
        {
            this.BaseInfo.MarauderSpecificAIP += AIPEquivalent;
        }
    }
}
