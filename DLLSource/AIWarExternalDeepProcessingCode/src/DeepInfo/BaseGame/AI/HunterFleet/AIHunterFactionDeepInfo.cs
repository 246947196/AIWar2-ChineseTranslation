using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public sealed class AIHunterFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        public AIHunterFactionBaseInfo BaseInfo;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<AIHunterFactionBaseInfo>();
        }

        protected override void Cleanup() 
        {
            this.BaseInfo = null;

            this.TeamsAimedAtPlanet.Clear(); //this may matter

            //these are almost certainly not a problem
            UnitsGeneratingBonusShips.Clear();
            UnassignedShips.Clear();
            AlliedCommandStations.Clear();
            AvailableFireteams.Clear();
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 2;

        public readonly ProtectedValDictionary<Planet,FireteamRegiment> TeamsAimedAtPlanet = ProtectedValDictionary<Planet, FireteamRegiment>.Create_WillNeverBeGCed( 100, "AIHunterFactionDeepInfo-TeamsAimedAtPlanet" );        

        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType)
        {
            AIHunterCoreData factionExternal = AttachedFaction.GetAISentinelsCoreData().HunterInfo;
            if ( factionExternal == null )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "SeedStartingEntities_LaterEverythingElse: GetHunterFleetExternal was null on faction " +
                    AttachedFaction.GetDisplayName() + " (index " + AttachedFaction.FactionIndex + ")" );
                return;
            }
            factionExternal.DoGameStartLogic( Context );
        }

        public override void DoPerSimStepLogic_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
        }

        private static readonly List<SafeSquadWrapper> UnitsGeneratingBonusShips = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "AIHunterFactionDeepInfo-UnitsGeneratingBonusShips" );
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            AIHunterCoreData factionExternal = AttachedFaction.GetAISentinelsCoreData().HunterInfo;
            if ( AttachedFaction.MinFireteamStrength == -1 ||
                 AttachedFaction.MaxFireteamStrength > 3 * 1000 * 1000 && AttachedFaction.NumFireteams < 30 ) //this is some sort of bug where we can get the wrong sizes
                AttachedFaction.MinFireteamStrength = 2000;
            if ( AttachedFaction.MaxFireteamStrength == -1 ||
                 AttachedFaction.MaxFireteamStrength > 3 * 1000 * 1000 && AttachedFaction.NumFireteams < 30) //this is some sort of bug where we can get the wrong sizes
                AttachedFaction.MaxFireteamStrength = 8000;
            if ( AttachedFaction.HasBeenSeenByPlayer )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Base_Lore_Hunter", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );


            //Asynchronously run the really heavy logic.  We want it to happen once per second, not in the LRP, but this only runs on the host anyhow
            //We don't want the game to be slowed down by this.
            ArcenThreading.RunTaskOnBackgroundThread( "_PSec.AIHunter.Stage3Logic", false, false, delegate
            {
                Stage3_CheckForInitialDonation( factionExternal, AttachedFaction, Context );
                Stage3_DoDonationsPerMinute( factionExternal, AttachedFaction, Context );
                factionExternal.DoPerSecondLogic_OnMainThreadAndPartOfSim_HostOnly( Context );
                Stage3_CheckForWarpHomeEvery20Seconds( factionExternal, AttachedFaction, Context );
                //Stage3_CheckForAntiTaggedUnitIncome( factionExternal, AttachedFaction, Context );
            } );
        }

        private void Stage3_CheckForInitialDonation( AIHunterCoreData factionExternal, Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( !factionExternal.HaveCheckedForInitialDonation )
            {
                factionExternal.HaveCheckedForInitialDonation = true;
                FInt startingAIPurchaseCost = (FInt)factionExternal.AIDifficulty.HunterStartingBudget;
                if ( startingAIPurchaseCost > 0 )
                    factionExternal.ReceiveDonation( startingAIPurchaseCost, faction, null );
            }
        }

        private void Stage3_DoDonationsPerMinute( AIHunterCoreData factionExternal, Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( World_AIW2.Instance.GameSecond % 60 == 0 && factionExternal.AIDifficulty.HunterBonusIncomePerMinute > 0 )
            {
                FInt income = (FInt)factionExternal.AIDifficulty.HunterBonusIncomePerMinute;
                if ( income > 0 )
                {
                    factionExternal.ReceiveDonation( income, faction, null );
                }
            }
        }

        private void Stage3_CheckForWarpHomeEvery20Seconds( AIHunterCoreData factionExternal, Faction faction, ArcenHostOnlySimContext Context )
        {
            if ( World_AIW2.Instance.GameSecond % 20 == 0 )
            {
                //Check over all our Extragalactic units and see if any of them are against specific factions/allegiances
                //  If that faction in particular is dead then we should let those units "warp home"
                //  This isn't the most efficient code in the world, and is only intended for small numbers of units
                //  If more units are added then it will need to be modified
                FInt powerBreakPoint = FInt.FromParts( 0, 100 );
                foreach ( GameEntity_Squad entity in faction.Squads( "ExtragalacticWar" ) )
                {
                   if ( entity.FireteamSpecificationOrNull == null )
                       continue;
                   if ( !entity.FireteamSpecificationOrNull.IsActive() )
                       continue;
                   if ( entity.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength > 0 )
                       continue; //no warping out until their current planet is clear of hostiles
                   if ( entity.FireteamSpecificationOrNull.AgainstFaction != null )
                   {
                       if ( entity.FireteamSpecificationOrNull.AgainstFaction.OverallPowerLevel < powerBreakPoint )
                       {
                           entity.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                       }
                   }
                   if ( !String.IsNullOrEmpty( entity.FireteamSpecificationOrNull.AgainstFactionAllegiance ) )
                   {
                       FInt totalPowerLevel = FInt.Zero;
                       for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                       {
                           Faction otherFaction = World_AIW2.Instance.Factions[i];
                           if ( otherFaction == null )
                               continue;
                           if ( !otherFaction.GetIsHostileTowards( faction ) )
                               continue;
                           if ( otherFaction.BaseInfo.Allegiance != entity.FireteamSpecificationOrNull.AgainstFactionAllegiance )
                               continue;
                           totalPowerLevel += otherFaction.OverallPowerLevel;
                           if ( totalPowerLevel > powerBreakPoint )
                               break;
                       }
                       if ( totalPowerLevel < powerBreakPoint )
                       {
                           entity.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                       }
                   }
                }
            }
        }

        public static readonly List<SafeSquadWrapper> UnassignedShips = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "AIHunterFactionDeepInfo-UnassignedShips" );
        public static readonly List<SafeSquadWrapper> AlliedCommandStations = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "AIHunterFactionDeepInfo-AlliedCommandStations" );
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            AIHunterCoreData factionExternal = AttachedFaction.GetAISentinelsCoreData().HunterInfo;
            if (factionExternal.DisableLongRangePlanning)
                return;

            if ( !factionExternal.SubType.UseFireteams )
            {
                factionExternal.DoLongRangePlanning( Context );
                return;
            }
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam ) && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Hunter );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AIHunt-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            int debugCode = 0;
            UnassignedShips.Clear();
            TeamsAimedAtPlanet.Clear();
            AlliedCommandStations.Clear();
            int totalStrengthOfUnits = 0;

            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                debugCode = 100;
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( factionExternal.Teams ) )
                    team.DeepInfo.Reset(); //reset team count information
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( planet == null )
                        continue;
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
                    totalStrengthOfUnits += entity.GetStrengthOfSelfAndContents();
                    if ( entity.FireteamId < 0 )
                        UnassignedShips.Add( entity );
                    else
                    {
                        Fireteam team = FireteamBaseUtility.GetFireteamById( factionExternal.Teams, entity.FireteamId );
                        if ( team != null )
                        {
                            team.DeepInfo.AddUnit( entity );
                            if ( team.StrengthToBringOnline > AttachedFaction.MaxFireteamStrength )
                                team.StrengthToBringOnline = AttachedFaction.MaxFireteamStrength; //this is a workaround for a bug where we had too high of Strength to bring online
                        }
                        else
                            entity.FireteamId = -1; //something happened to the fireteam, so lets find a new one next LRP stage
                    }
                }

                debugCode = 300;
                FInt overkillRequired = FInt.FromParts( 0, 850 );
                FireteamUtility.UpdateFireteams( AttachedFaction, Context, pathingCacheData, factionExternal.Teams, TeamsAimedAtPlanet, tracingBuffer, overkillRequired );
                debugCode = 400;
                FireteamUtility.UpdateRegiments( AttachedFaction, Context, pathingCacheData, factionExternal.Teams, TeamsAimedAtPlanet, tracingBuffer, AttachedFaction.MinFireteamStrength, true );
                debugCode = 500;
                for ( int i = 0; i < UnassignedShips.Count; i++ )
                    AssignUnitToFireteam( AttachedFaction, UnassignedShips[i].GetSquad(), Context, pathingCacheData );
                if ( AttachedFaction.NumFireteams != factionExternal.Teams.GetItemCount() )
                    AttachedFaction.NumFireteams = factionExternal.Teams.GetItemCount();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "debug code " + debugCode + " during hunter fireteam logic. " + e.ToString(), Verbosity.DoNotShow );
            }
            finally
            {
                pathingCacheData.ReturnToPool();

                if ( tracing && !tracingBuffer.GetIsEmpty() )
                {
                    tracingBuffer.Add( this.TracingName ).Add( " Long Range Planning with fireteams concludes at " ).Add( Engine_Universal.ToHoursAndMinutesString( World_AIW2.Instance.GameSecond ) + " (" + World_AIW2.Instance.GameSecond + "). Total strength of hunter: " + totalStrengthOfUnits + " \n" );
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                }
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
            }
        }

        private static readonly List<Fireteam> AvailableFireteams = List<Fireteam>.Create_WillNeverBeGCed( 200, "AIHunterFactionDeepInfo-AvailableFireteams" ); //we use a regular List so we can sort (if doing deletions, use a LessLinkedList)
        //Set immediately before AvailableFireteams.Sort(...) so the comparison can be a non-capturing
        //static delegate.  [ThreadStatic] because long-range planning runs on a background thread.
        [ThreadStatic] private static GameEntity_Squad cb_hunterSortEntity;

        private void AssignUnitToFireteam(Faction faction, GameEntity_Squad entity, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( entity == null )
                return;
            //Hunter fleets now have a "Max" of fireteams to make them behave a bit differently
            
            AvailableFireteams.Clear();
            AIHunterCoreData factionExternal = faction.GetAISentinelsCoreData().HunterInfo;
            bool debug = false;

            int maxPreferredFireteams = factionExternal.AIDifficulty.Difficulty;
            if ( factionExternal.Teams.GetItemCount() == 0 )
            {
                Fireteam team = Fireteam.CreateNewWithIDFromList( factionExternal.Teams );
                team.MyStrengthMultiplierForStrengthCalculation = factionExternal.SubType.MyStrengthMultiplier;
                team.EnemyStrengthMultiplierForStrengthCalculation = factionExternal.SubType.EnemyStrengthMultiplier;
                team.StrengthToBringOnline = faction.MinFireteamStrength + Context.RandomToUse.Next( 0, faction.MaxFireteamStrength - faction.MinFireteamStrength );
                team.DeathballingThreshold = 4;
                team.DeepInfo.CurrentPlanet = entity.Planet;
                if ( factionExternal.AIDifficulty.Difficulty >= 8 )
                    team.ExtraCautiousAgainstPlayers = true;

                factionExternal.Teams.AddIfNotAlreadyIn( team );
            }
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( factionExternal.Teams ) )
            {
                if ( team.status == FireteamStatus.Disbanded )
                    continue; //if a fireteam is ready to fight, don't send more ships to that team

                if ( entity.FireteamSpecificationOrNull != null )
                {
                    if ( entity.FireteamSpecificationOrNull.IsActive() && !entity.FireteamSpecificationOrNull.IsEqualTo( team.SpecificationOrNull ) )
                        continue;
                }
                if ( entity.FireteamSpecificationOrNull == null &&
                     team.SpecificationOrNull != null )
                    continue; //if we don't have an explicit, specific target then don't join a fireteam with such a target (instead this unit should be used for "general AI purposes"
                if ( team.DeepInfo.CurrentPlanet == null )
                {
                    //this is a brand new fireteam, so it's totally safe.
                    AvailableFireteams.Add( team );
                    continue;
                }

                Int16 hops = 0;
                int dangerOfTeam = Fireteam.GetDangerOfPath( faction, Context, PathCacheData, entity.Planet, team.DeepInfo.CurrentPlanet, true, out hops );
                if ( dangerOfTeam >= 40000 ) //let units wander through pretty dangerous spots (40 strength)
                    continue;
                AvailableFireteams.Add( team );
            }

            cb_hunterSortEntity = entity;
            AvailableFireteams.Sort( static delegate (Fireteam L, Fireteam R)
            {
                int lHops = cb_hunterSortEntity.Planet.GetHopsTo(L.DeepInfo.CurrentPlanet );
                int rHops = cb_hunterSortEntity.Planet.GetHopsTo(R.DeepInfo.CurrentPlanet );
                return lHops.CompareTo(rHops);
            } );
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
            if ( AvailableFireteams.Count >= maxPreferredFireteams )
                percentNewTeam = 0; //note we can still start a new fireteam if we are really cut off from all other fireteams

            if ( Context.RandomToUse.Next( 0, 100 ) < percentNewTeam || AvailableFireteams.Count == 0 )
            {
                Fireteam team = Fireteam.CreateNewWithIDFromList( factionExternal.Teams );
                if ( entity.FireteamSpecificationOrNull != null )
                {
                    team.SpecificationOrNull = FireteamRequiredTarget.GetFromPoolOrCreate();
                    team.SpecificationOrNull.CopyFrom( entity.FireteamSpecificationOrNull );
                }
                if ( factionExternal.AIDifficulty.Difficulty >= 8 )
                    team.ExtraCautiousAgainstPlayers = true;
                team.DeathballingThreshold = 4;
                team.MyStrengthMultiplierForStrengthCalculation = factionExternal.SubType.MyStrengthMultiplier;
                team.EnemyStrengthMultiplierForStrengthCalculation = factionExternal.SubType.EnemyStrengthMultiplier;
                team.StrengthToBringOnline = faction.MinFireteamStrength + Context.RandomToUse.Next( 0, faction.MaxFireteamStrength - faction.MinFireteamStrength );
                if ( this.AttachedFaction.SpecialFactionData.FireteamPercentBestTarget > 0 )
                    team.PercentBestTarget = this.AttachedFaction.SpecialFactionData.FireteamPercentBestTarget;
                else
                    team.PercentBestTarget = 60;
                team.DeepInfo.AddUnit(entity);
                team.DeepInfo.CurrentPlanet = entity.Planet;
                entity.FireteamId = team.FireTeamID;
                team.DeepInfo.IdentifyCurrentPlanet(); //in case this unit is the first unit
                factionExternal.Teams.AddIfNotAlreadyIn( team );
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path B (there were " + AvailableFireteams.Count + " available and percentNewTeam " + percentNewTeam, Verbosity.DoNotShow );
                return;
            }


            {
                Fireteam team = AvailableFireteams[Context.RandomToUse.Next(0, AvailableFireteams.Count)];
                team.DeepInfo.AddUnit( entity );
                entity.FireteamId = team.FireTeamID;
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path C", Verbosity.DoNotShow );
                team.DeepInfo.IdentifyCurrentPlanet(); //just in case this unit is the first unit or something
            }
        }

        public override GameEntity_Squad  GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( Planet CurrentPlanetForFireteam,  ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
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
                int danger = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForFireteam, outpost.Planet, true, out hops);
                if ( danger < currentDanger || currentDanger == -1)
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
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, 
            List<FireteamTarget> PreferredTargets, List<FireteamTarget> FallbackTargets, object TeamObj )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AIHunt-GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            FInt falloffForDistance = FInt.FromParts (0, 050);
            Fireteam team = (Fireteam)TeamObj;
            bool honorFireteamSpecification = true;

            GetPreferredHunterTargets( PreferredTargets, AttachedFaction, Context, PathCacheData, team, honorFireteamSpecification);
            GetFallbackHunterTargets( FallbackTargets, AttachedFaction, Context, team, honorFireteamSpecification);
            if ( team.SpecificationOrNull != null &&
                 team.SpecificationOrNull.IsActive() )
            {
                //if we have required targets, sometimes we can't get to our preferred targets
                GetPlanetsEnRouteToTargets( FallbackTargets, AttachedFaction, Context, PathCacheData, team, PreferredTargets);
            }
            bool debug = false;
            if ( debug && tracing )
            {
                tracingBuffer.Add("Getting lurk/target Preferred Targets for " + team.FireTeamID + "\n");
                for ( int i = 0; i < PreferredTargets.Count; i++ )
                    tracingBuffer.Add("\t").Add(PreferredTargets[i].GetPlanetName_Safe()).Add(" difficulty ").Add(PreferredTargets[i].dangerOfPath).Add(" \n");
                tracingBuffer.Add("Getting lurk/target Fallback Targets\n");
                for ( int i = 0; i < FallbackTargets.Count; i++ )
                    tracingBuffer.Add("\t").Add(FallbackTargets[i].GetPlanetName_Safe()).Add("\n");   
            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }

        public void GetPreferredHunterTargets( List<FireteamTarget> listToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, Fireteam team, bool HonorSpecification)
        {
            var factionCommonExternal = faction.BaseInfo;
            AIHunterCoreData factionExternal = faction.GetAISentinelsCoreData().HunterInfo;
            listToFill.Clear();

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            if ( team.SpecificationOrNull == null || !team.SpecificationOrNull.IsActive() )
                tracing = false;

            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AIHunt-GetPreferredHunterTargets-trace", 20f ) : null;
            if ( tracing )
            {
                tracingBuffer.Add("Looking for a target for " + team.FireTeamID + " spec " );
                team.SpecificationOrNull.ToDebugString( tracingBuffer );
                tracingBuffer.Add("\n");
            }
            //TODO: make ZA territory less of a valuable target
            if ( team.AgainstTarget != null && HonorSpecification )
            {
                //if we have a required target (for example, hunter ships required to go after MDCs), that target and any planets en route are an eligible target
                if ( tracing )
                    tracingBuffer.Add("Path A (unexpected!)").Add("\n");
                PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "GetPreferredHunterTargets", 
                    team.DeepInfo.CurrentPlanet, team.AgainstTarget.Planet, PathingMode.Default, Context, PathCacheData );
                if ( pathCache != null )
                {
                    for ( int i = 0; i < pathCache.PathToReadOnly.Count; i++ )
                    {
                        if ( pathCache.PathToReadOnly[i].GetControllingFaction().GetIsHostileTowards( faction ) )
                        {
                            listToFill.Add( new FireteamTarget( pathCache.PathToReadOnly[i] ) );
                            continue;
                        }
                        EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = pathCache.PathToReadOnly[i].GetStanceDataForFaction( faction );
                        int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                        int myStrength = myFactionData[FactionStance.Self].TotalStrength + myFactionData[FactionStance.Friendly].TotalStrength;
                        if ( hostileStrength > myStrength / 2 )
                            listToFill.Add( new FireteamTarget( pathCache.PathToReadOnly[i] ) );
                    }
                }
                
                //if there are enemy planets on the way to our target, take them out first. Trying to go straight to the target is a bad idea
                if ( listToFill.Count == 0 )
                    listToFill.Add(new FireteamTarget(team.AgainstTarget));

                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return;
            }
            bool countAllPlayerFactionsAsValidTargets = false;
            if ( HonorSpecification &&
                 team.SpecificationOrNull != null &&
                 team.SpecificationOrNull.IsActive() )
            {
                //we can wind up with fireteams that are just agsint the Fallen Spire,
                //so make sure they can go after players
                if ( team.AgainstFaction != null &&
                     team.AgainstFaction.SpecialFactionData.AlwaysFriendlyToPlayers )
                    countAllPlayerFactionsAsValidTargets = true;
            }
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                Faction thisFactionMustBeTargetForBonusPlanetOrNull = null;
                if (otherFaction == null)
                {
                    continue;
                }

                if (!otherFaction.GetIsHostileTowards(faction))
                {
                    continue;
                }
                if ( team.AgainstFaction != null &&
                     otherFaction != team.AgainstFaction )
                {
                    //NOTA BENE: We still honor the AgainstFaction
                    //except if it's an always-friendly-to-players faction
                    if ((countAllPlayerFactionsAsValidTargets && otherFaction.Type != FactionType.Player) ||
                        !countAllPlayerFactionsAsValidTargets)
                    {
                        continue;
                    }
                }
                if ( HonorSpecification &&
                     team.SpecificationOrNull != null &&
                     team.SpecificationOrNull.IsActive() )
                {
                    //check whether this faction is a valid target for a fireteam with an active Specification
                    bool foundValidReason = false;

                    if ( team.SpecificationOrNull.AgainstFaction == otherFaction )
                    {
                        thisFactionMustBeTargetForBonusPlanetOrNull = otherFaction;
                        foundValidReason = true;
                    }
                    if ( !String.IsNullOrEmpty( team.SpecificationOrNull.AgainstFactionAllegiance ) &&
                         otherFaction.BaseInfo.Allegiance == team.SpecificationOrNull.AgainstFactionAllegiance )
                    {
                        thisFactionMustBeTargetForBonusPlanetOrNull = otherFaction;
                        foundValidReason = true;
                    }
                    if ( !String.IsNullOrEmpty(team.SpecificationOrNull.RequiredTag ) )
                    {
                        foundValidReason = true;
                    }
                    if ( countAllPlayerFactionsAsValidTargets && otherFaction.Type == FactionType.Player )
                        foundValidReason = true;
                    if ( !foundValidReason )
                        continue;

                    if ( tracing )
                        tracingBuffer.Add("\t " + otherFaction.GetDisplayName() + " is valid").Add(". countAllPlayerFactionsAsValidTargets " + countAllPlayerFactionsAsValidTargets +". AlwaysFriendlyToPlayers " + otherFaction.SpecialFactionData.AlwaysFriendlyToPlayers +" \n");


                    if ( !String.IsNullOrEmpty(team.SpecificationOrNull.RequiredTag ) )
                    {
                        foreach ( GameEntity_Squad entity in otherFaction.Squads( team.SpecificationOrNull.RequiredTag ) )
                        {
                            listToFill.Add(new FireteamTarget(entity));
                            //when choosing targets, also pick the first heavily defended enemy planet on the way as a bonus target
                            //this allows the AI to attack other factions en-route to their preferred target
                            Planet potentialBonus = GetPlanetEnRouteToTarget(faction, Context, PathCacheData,  team, entity.Planet, thisFactionMustBeTargetForBonusPlanetOrNull);
                            if ( potentialBonus != null )
                                listToFill.Add(new FireteamTarget(potentialBonus));
                        }
                        continue;
                    }
                }

                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.GrantsMinorFactionPlanetControl ) )
                {
                    //allows the scourge to go after hostile minor factions
                    if ( entity.SecondsSpentAsRemains > 0 )
                        continue;
                    if ( tracing )
                        tracingBuffer.Add("\tTarget: Grants Minor Faction Control: " + entity.ToStringWithPlanetAndOwner()).Add("\n");
                    if ( entity.Planet.IsZenithArchitraveTerritory )
                        continue; //Don't attack the ZA core territory as a Preferred target unless we have specific orders to do so

                    listToFill.Add(new FireteamTarget(entity));
                    Planet potentialBonus = GetPlanetEnRouteToTarget(faction, Context, PathCacheData,  team, entity.Planet, thisFactionMustBeTargetForBonusPlanetOrNull);
                    if ( potentialBonus != null )
                    {
                        if ( tracing )
                            tracingBuffer.Add("\tTarget: Grants Minor Faction Control bonus planet: " + potentialBonus.Name).Add("\n");

                        listToFill.Add(new FireteamTarget(potentialBonus));
                    }

                }

                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.CommandStation ) )
                {
                    if(entity.SecondsSpentAsRemains > 0)
                        continue;
                    EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = entity.Planet.GetStanceDataForFaction( faction );
                    int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                    //on higher difficulties, the hunter skips generically attacking player planets (except weak ones)
                    //to focus on critical targets like GCAs and stuff
                    int maxHostileStrength = - 1;
                    if ( factionExternal.AIDifficulty.Difficulty >= 6 )
                        maxHostileStrength = 300 * 1000;
                    if ( factionExternal.AIDifficulty.Difficulty >= 7 )
                        maxHostileStrength = 100 * 1000;
                    if ( factionExternal.AIDifficulty.Difficulty >= 8 )
                        maxHostileStrength = 40 * 1000;
                    if ( factionExternal.AIDifficulty.Difficulty >= 9 )
                        maxHostileStrength = 20 * 1000;
                    if ( factionExternal.AIDifficulty.Difficulty >= 10 )
                        maxHostileStrength = 10 * 1000;

                    if ( maxHostileStrength > -1 && hostileStrength >= maxHostileStrength )
                    {
                        continue; //the preferred path only blindly goes after tasty command stations
                    }
                    if ( tracing )
                        tracingBuffer.Add("\tTarget: Command Station" + entity.ToStringWithPlanetAndOwner()).Add("\n");
                    listToFill.Add( new FireteamTarget(entity.Planet) );
                    Planet potentialBonus = GetPlanetEnRouteToTarget(faction, Context, PathCacheData,  team, entity.Planet, thisFactionMustBeTargetForBonusPlanetOrNull);
                    if ( potentialBonus != null )
                    {
                        if ( tracing )
                            tracingBuffer.Add("\tTarget: Command Station bonus" + potentialBonus.Name).Add("\n");

                        listToFill.Add(new FireteamTarget(potentialBonus));
                    }

                }

                //don't look for command stations here; the economic and logistical ones
                //will be caught under Energy Producers
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    if ( tracing )
                        tracingBuffer.Add("\tTarget: King" + entity.ToStringWithPlanetAndOwner()).Add("\n");

                    listToFill.Add( new FireteamTarget(entity) );
                }
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.AIPOnDeath ) )
                {
                    //get GCAs
                    if ( entity.TypeData.AIPOnDeath > 0 )
                    {
                        if ( tracing )
                            tracingBuffer.Add("\tTarget: AIOOnDeath" + entity.ToStringWithPlanetAndOwner()).Add("\n");

                        listToFill.Add( new FireteamTarget(entity) );
                    }
                }
                foreach ( GameEntity_Squad entity in otherFaction.Squads( "MajorFuel" ) )
                {
                    if ( tracing )
                        tracingBuffer.Add("\tTarget: Fuel Station " + entity.ToStringWithPlanetAndOwner()).Add("\n");
                    listToFill.Add( new FireteamTarget(entity) );
                }
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.CriticalInfrastructure ) )
                {
                    if ( tracing )
                        tracingBuffer.Add( "\tTarget: Critical Infrastructure " + entity.ToStringWithPlanetAndOwner() ).Add( "\n" );
                    listToFill.Add( new FireteamTarget( entity ) );
                }
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.EnergyProducers ) )
                {
                    if ( entity.TypeData.IsMobile || entity.SecondsSpentAsRemains > 0) //target immobile things (and things that are alive)
                        continue;
                    if ( entity.TypeData.IsCommandStation )
                    {
                        //this check allows high-difficulty scourge to only prioritize player command stations
                        //that are necessary to keep the player out of brownout
                        if ( factionExternal.AIDifficulty.Difficulty >= 7 && entity.GetFactionTypeSafe() == FactionType.Player )
                        {
                            //"Easy command station targets" are covered above. This check says "Go for a command station if it would put the player into brownout"
                            //note that in civil war, hunter will just happily go for AI command stations
                            FInt energyProduced = entity.GetFullyMultipliedEnergyToProduce();
                            Faction facOrNull = entity.GetFactionOrNull_Safe();
                            if ( facOrNull != null )
                            {
                                if ( energyProduced < facOrNull.NetEnergy )
                                    continue;
                            }
                        }
                    }
                    //                    tracingBuffer.Add("\t " + entity.ToStringWithPlanetAndOwner() + " energy").Add("\n");
                    if ( tracing )
                        tracingBuffer.Add("\tTarget: EnergyProducers" + entity.ToStringWithPlanetAndOwner()).Add("\n");

                    listToFill.Add( new FireteamTarget(entity) );
                }
            }

            //end finding listToFill

            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        public void GetFallbackHunterTargets( List<FireteamTarget> listToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context, Fireteam team, bool HonorSpecification)
        {
            listToFill.Clear();

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.GetControllingOrInfluencingFaction().FactionIndex == faction.FactionIndexOfMyParentIfIHaveOne )
                {
                    EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( faction );
                    int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                    int myStrength = myFactionData[FactionStance.Self].TotalStrength + myFactionData[FactionStance.Friendly].TotalStrength;
                    if ( hostileStrength > myStrength / 2 )
                    {
                        if ( team.SpecificationOrNull != null && team.SpecificationOrNull.IsActive() && team.SpecificationOrNull.AgainstFaction != null )
                        {
                            //make sure the target faction has units here
                            EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> targetFactionData = planet.GetStanceDataForFaction( team.SpecificationOrNull.AgainstFaction );
                            if ( targetFactionData[FactionStance.Self].TotalStrength <= 0 )
                                continue;
                        }
                        if ( HonorSpecification &&
                             team.SpecificationOrNull != null && team.SpecificationOrNull.IsActive() && !String.IsNullOrEmpty(team.SpecificationOrNull.AgainstFactionAllegiance) )
                        {
                            //iterate over all the enemy factions on the planet and see if any of them are part of the bad alliance
                            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                            {
                                Faction otherFaction = World_AIW2.Instance.Factions[i];
                                if ( otherFaction == faction || !otherFaction.GetIsHostileTowards(faction) )
                                    continue;
                                if ( team.SpecificationOrNull.AgainstFactionAllegiance != faction.BaseInfo.Allegiance )
                                    continue;
                                EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> targetFactionData = planet.GetStanceDataForFaction( otherFaction );
                                if ( targetFactionData[FactionStance.Self].TotalStrength <= 0 )
                                    continue;
                            }
                        }
                        listToFill.Add( new FireteamTarget( planet ) );
                    }
                }
            }
            //end finding listToFill
        }
        public Planet GetPlanetEnRouteToTarget( Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, Fireteam team, Planet targetPlanet, Faction factionWhoMustOwnPlanetOrNull )
        {
            PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "GetPlanetEnRouteToTarget", team.DeepInfo.CurrentPlanet, targetPlanet, PathingMode.Default, Context, PathCacheData );
            if ( pathCache == null || pathCache.PathToReadOnly.Count <= 3 )
                return null;
            for ( int j = 0; j < pathCache.PathToReadOnly.Count; j++ )
            {
                //look down the path till we find a hostile planet; that planet is now an eligible target
                EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = pathCache.PathToReadOnly[j].GetStanceDataForFaction( faction );
                int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                int myStrength = myFactionData[FactionStance.Self].TotalStrength +
                    myFactionData[FactionStance.Friendly].TotalStrength;
                if ( factionWhoMustOwnPlanetOrNull != null )
                {
                    //this planet must be owned by the given faction (or one of that faction's allies)
                    Faction owningFaction = pathCache.PathToReadOnly[j].GetControllingFaction();
                    bool toSkip = true;
                    if ( owningFaction == factionWhoMustOwnPlanetOrNull )
                        toSkip = false;
                    //and check allegiance
                    if ( !String.IsNullOrEmpty(factionWhoMustOwnPlanetOrNull.BaseInfo.Allegiance ) &&
                         factionWhoMustOwnPlanetOrNull.BaseInfo.Allegiance == owningFaction.BaseInfo.Allegiance )
                        toSkip = false;
                    if ( toSkip )
                        continue;
                }
                if ( hostileStrength > myStrength / 2 && pathCache.PathToReadOnly[j] != targetPlanet )
                    return pathCache.PathToReadOnly[j];
            }
            return null;
        }
        public void GetPlanetsEnRouteToTargets( List<FireteamTarget> listToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, Fireteam team, List<FireteamTarget> existingTargets )
        {
            //If we have a fireteam that must go after a specific faction, they can now go through targets on the way if necessary
            //So if the player has prevented the AI from going after the Dark Zenith, AI hunter ships required to go for the DZ can now take out the player
            //planets en route
            listToFill.Clear();
            Planet startPlanet = team.DeepInfo.CurrentPlanet;
            for ( int i = 0; i < existingTargets.Count; i++ )
            {
                FireteamTarget target = existingTargets[i];
                Planet targetPlanet = target.planet;
                if ( targetPlanet == null && target.targetSquad != null )
                    targetPlanet = target.targetSquad.Planet;
                if ( targetPlanet == null )
                    continue;
                PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "GetPlanetsEnRouteToTargets", startPlanet, targetPlanet, PathingMode.Default, Context, PathCacheData );
                if ( pathCache != null )
                {
                    for ( int j = 0; j < pathCache.PathToReadOnly.Count; j++ )
                    {
                        //look down the path till we find a hostile planet; that planet is now an eligible target
                        EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = pathCache.PathToReadOnly[j].GetStanceDataForFaction( faction );
                        int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                        int myStrength = myFactionData[FactionStance.Self].TotalStrength +
                            myFactionData[FactionStance.Friendly].TotalStrength;
                        if ( hostileStrength > myStrength / 2 )
                        {
                            listToFill.Add( new FireteamTarget( pathCache.PathToReadOnly[j] ) );
                            break;
                        }
                    }
                }
            }
            //end finding listToFill
        }
        public override Planet GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass( Planet TargetPlanet, int TeamStrength, Planet CurrentPlanetForTeam, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            Planet bestPlanet = null;
            int dangerOfPathFromBestPlanet = -1;
            int distanceFromBestPlanet = -1;
            Int16 hopsFromBestPlanet = 9999;
            int unused = 0;
            //this logic partially cribbed from IndependentAIFleet.cs::Helper_DoTargetFindingSweep

            if ( TargetPlanet == null )
                throw new Exception ("No target planet set in get lurk planet?!");
            //int debugCode = 0;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            //tracing = true; //NOTE: this is very performance-heavy, so only log if necessary
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AIHunt-GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            //bool preferUnwatchedPlanets = true;
            if ( tracing )
                tracingBuffer.Add("Getting a lurk planet, heading from " + CurrentPlanetForTeam.Name + " Target Planet " + TargetPlanet.Name).Add(". ").Add(AttachedFaction.BaseInfo.Allegiance).Add("\n");
            int targetDefensiveStrength = Fireteam.GetPlanetDefensiveStrength( TargetPlanet, AttachedFaction, true, ref unused,
                                                                               FInt.Zero, FInt.Zero );

            foreach ( Planet.PlanetAtHopDistance _phd in TargetPlanet.PlanetsWithinXHops_NoFilters( -1 ) )
            {
                Planet planet = _phd.Planet;
                Int16 Distance = _phd.Hops;
                if ( planet == TargetPlanet )
                    continue;

                int planetDefensiveStrength = Fireteam.GetPlanetDefensiveStrength( planet, AttachedFaction, true, ref unused,
                                                                          FInt.Zero, FInt.Zero );
                if ( planet.GetControllingFaction().GetIsHostileTowards( AttachedFaction ) && planetDefensiveStrength > TeamStrength / 10 )
                    continue; //don't include hostile defended planets

                //Don't path through any particularly dangerous planets
                Int16 hops = 0;
                bool includeDestination = true;
                int totalDifficultyOfPathToLurkPlanet = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForTeam, planet, includeDestination, out hops );
                if ( totalDifficultyOfPathToLurkPlanet >= TeamStrength * 3 ||
                     (hops > 0 && totalDifficultyOfPathToLurkPlanet / hops >= TeamStrength * 2 ) ) //as long as the path there doesn't outnumber us too badly. We count both the total danger and the average danger (so a fireteam of strength 10 doesn't decide to path through a single strength 30 planet, since people complain about that
                    continue;

                int totalDifficultyOfPathToTarget = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, planet, TargetPlanet, !includeDestination, out hops );

                if ( totalDifficultyOfPathToTarget < 0 )
                    totalDifficultyOfPathToTarget = 0; //pathing through allied planets is basically the same

                if ( tracing )
                    tracingBuffer.Add( "\tConsidering " + planet.Name + " danger of path from " + planet.Name + " to target " + TargetPlanet.Name + " is " + totalDifficultyOfPathToTarget + " distance " + Distance + " intel " + planet.IntelLevel ).Add( "\n" );

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

                if ( dangerOfPathFromBestPlanet == totalDifficultyOfPathToTarget )
                {
                    //if we have two equal planets, sometimes allow a tiebreaker
                    //first, if the planet we can hide on
                    if ( bestPlanet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched &&
                         planet.IntelLevel <  PlanetIntelLevel.CurrentlyWatched )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t" + planet.Name + " is now the lurk location; path B (avoid watched planets)" ).Add( "\n" );
                        dangerOfPathFromBestPlanet = totalDifficultyOfPathToTarget;
                        distanceFromBestPlanet = Distance;
                        hopsFromBestPlanet = hops;
                        bestPlanet = planet;
                    }
                    else if (distanceFromBestPlanet > Distance ||
                        hopsFromBestPlanet > hops)
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t" + planet.Name + " is now the lurk location; path C (found closer planet)" ).Add( "\n" );
                        dangerOfPathFromBestPlanet = totalDifficultyOfPathToTarget;
                        distanceFromBestPlanet = Distance;
                        hopsFromBestPlanet = hops;
                        bestPlanet = planet;
                    }
                }

                if ( hopsFromBestPlanet <= 3 && dangerOfPathFromBestPlanet <= TeamStrength / 10 &&
                     bestPlanet.IntelLevel < PlanetIntelLevel.CurrentlyWatched )
                {
                    break; //we found a good lurk within easy striking distance of the planet, so exit now
                }
                if ( (hops + 6) < hopsFromBestPlanet )
                {
                    break; //don't keep looking forever. This is mostly a performance optimization
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
    }
}
