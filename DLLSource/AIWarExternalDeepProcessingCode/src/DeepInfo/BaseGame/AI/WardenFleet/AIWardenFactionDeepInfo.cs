using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public sealed class AIWardenFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        public AIWardenFactionBaseInfo BaseInfo;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<AIWardenFactionBaseInfo>();
        }

        protected override void Cleanup()
        {
            this.BaseInfo = null;

            //may matter
            TeamsAimedAtPlanet.Clear();

            //probably does not matter, but why not
            IndexForLastAudio = -1;
            UnassignedShips.Clear();
            AlliedCommandStations.Clear();
            AvailableFireteams.Clear();
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 2;

        private int IndexForLastAudio = -1;
        public readonly ProtectedValDictionary<Planet,FireteamRegiment> TeamsAimedAtPlanet = ProtectedValDictionary<Planet, FireteamRegiment>.Create_WillNeverBeGCed( 100, "AIWardenFactionDeepInfo-TeamsAimedAtPlanet" );
        public readonly int MinFireteamStrength = 2000;
        public readonly int MaxFireteamStrength = 3000;

        public override void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context)
        {
            if ( entity == null )
                return;
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //don't do this one client

            int debugStage = 0;
            try
            {
                debugStage = 100;
                try
                {
                    if ( entity.TypeData.GetHasTag( "WardenSecretNinjaHideout" ) // thing dying is a Warden Fleet Base
                        && FiringSystemOrNull != null // the game knows who killed it
                        && FiringSystemOrNull.ParentEntity.GetIsFactionControlledByLocalPlayerAccount_Safe() )// the local player killed it
                    {
                        debugStage = 200;
                        Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.PlayerDestroysWardenSecretNinjaHideout );
                    }
                }
                catch { } //we don't actually care if this errors, because it's just cosmetic.  And things do die.
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in SpecialFaction_AIWarden.DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly stage " + debugStage + "\n" + e, Verbosity.ShowAsError );
            }
        }

        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType)
        {
            AIWardenCoreData factionExternal = AttachedFaction.GetAISentinelsCoreData().WardenInfo;
            if ( factionExternal == null )
            {
                Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "SeedStartingEntities_LaterEverythingElse: GetWardenExternal was null on faction " + 
                    AttachedFaction.GetDisplayName() + " (index " + AttachedFaction.FactionIndex + ")" );
                return;
            }
            factionExternal.DoGameStartLogic( Context );
        }
        
        public override void DoPerSimStepLogic_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            AIWardenCoreData factionExternal = AttachedFaction.GetAISentinelsCoreData().WardenInfo;

            if ( !factionExternal.HaveCheckedForInitialDonation )
            {
                factionExternal.HaveCheckedForInitialDonation = true;
                FInt donation = (FInt)factionExternal.AIDifficulty.WardenStartingBudget;
                if ( donation > 0 )
                    factionExternal.ReceiveDonation( donation, AttachedFaction, null );
            }
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context)
        {
            if ( AttachedFaction.HasBeenSeenByPlayer )
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Base_Lore_Warden", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );

            Planet targetPlanet = World_AIW2.Instance.GetPlanetByIndex( (Int16)this.BaseInfo.ParentBaseInfo.WardenInfo.CurrentTargetPlanetIndex );
            if ( targetPlanet != null )
            {
                PlanetFaction planetFaction = targetPlanet.GetPlanetFactionForFaction( AttachedFaction );
                int hostileStrength = planetFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                int hostileAdvantage = hostileStrength - planetFaction.DataByStance[FactionStance.Friendly].TotalStrength;
                if ( hostileAdvantage > 0 )
                {
                    int wardenThreshold = Math.Max( hostileAdvantage * 3, hostileStrength >> 1 );
                    int wardenStrength = planetFaction.DataByStance[FactionStance.Self].TotalStrength + planetFaction.DataByStance[FactionStance.Self].UnengagedMobileStrengthByHopCount[1];
                    if ( wardenStrength >= wardenThreshold )
                    {
                        bool didIReachThisBranchLastTime = false; // TODO: set this accurately (Badger 7/27: maybe it works now?)
                        if(targetPlanet.Index == this.IndexForLastAudio)
                        {
                            didIReachThisBranchLastTime = true;
                        }

                        if ( !didIReachThisBranchLastTime )
                        {
                            if(targetPlanet.IntelLevel > PlanetIntelLevel.Unexplored)
                            {
                                if ( ArcenNetworkAuthority.GetIsHostMode() )
                                {
                                    PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                                    if ( chatHandlerOrNull != null )
                                        chatHandlerOrNull.PlanetToView = targetPlanet;

                                    World_AIW2.Instance.QueueChatMessageOrCommand( "Warden fleet heading toward " + targetPlanet.Name, ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                                }
                            }
                            this.IndexForLastAudio = targetPlanet.Index;
                        }
                    }
                }
            }
            AIWardenCoreData factionExternal = AttachedFaction.GetAISentinelsCoreData().WardenInfo;
            factionExternal.DoPerSecondLogic_OnMainThreadAndPartOfSim_HostOnly( Context );
        }

        public static readonly List<SafeSquadWrapper> UnassignedShips = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 30, "AIWardenFactionDeepInfo-UnassignedShips" );
        public static readonly List<SafeSquadWrapper> AlliedCommandStations = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 15, "AIWardenFactionDeepInfo-AlliedCommandStations" );
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            AIWardenCoreData factionExternal = AttachedFaction.GetAISentinelsCoreData().WardenInfo;
            if (factionExternal.DisableLongRangePlanning)
                return;
            //factionExternal.SubType.Implementation.DoLongRangePlanning( faction, Context ); //this will use the old logic, which is less flexible than fireteams
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam ) && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Warden );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AIWarden-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            int debugCode = 0;
            UnassignedShips.Clear();
            TeamsAimedAtPlanet.Clear();
            AlliedCommandStations.Clear();
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                debugCode = 100;
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( factionExternal.Teams ) )
                {
                    team.IsAllowedToStack = true;  //this is added to help existing saves (as of 2.022). It can be removed later
                    team.DeepInfo.Reset(); //reset team count information
                }
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
                    if ( !entity.TypeData.IsMobile )
                        continue;
                    if ( entity.FireteamId < 0 )
                        UnassignedShips.Add( entity );
                    else
                    {
                        Fireteam team = FireteamBaseUtility.GetFireteamById( factionExternal.Teams, entity.FireteamId );
                        if ( team != null )
                        {
                            team.DeepInfo.AddUnit( entity );
                        }
                        else
                        {
                            entity.FireteamId = -1; //something happened to the fireteam, so lets find a new one next LRP stage
                        }
                        entity.MinorFactionStackingID = -1; //this is added to help existing saves (as of 2.022). It can be removed later
                    }
                }
                debugCode = 300;
                FInt overkillRequired = FInt.FromParts( 0, 850 );

                FireteamUtility.UpdateFireteams( AttachedFaction, Context, pathingCacheData, factionExternal.Teams, TeamsAimedAtPlanet, tracingBuffer, overkillRequired );
                debugCode = 400;

                FireteamUtility.UpdateRegiments( AttachedFaction, Context, pathingCacheData, factionExternal.Teams, TeamsAimedAtPlanet, tracingBuffer, this.MinFireteamStrength, false );

                debugCode = 500;
                for ( int i = 0; i < UnassignedShips.Count; i++ )
                {
                    AssignUnitToFireteam( UnassignedShips[i].GetSquad(), Context, pathingCacheData );
                }
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
                    tracingBuffer.Add( this.TracingName ).Add( " Long Range Planning with fireteams concludes at " ).Add( Engine_Universal.ToHoursAndMinutesString( World_AIW2.Instance.GameSecond ) + " (" + World_AIW2.Instance.GameSecond + ") \n" );
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                }
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
            }
        }

        //prefer close ones that you can get to safely
        private static readonly ArcenLessLinkedList<Fireteam> AvailableFireteams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "AIWardenFactionDeepInfo-AvailableFireteams" ); 
        private void AssignUnitToFireteam(GameEntity_Squad entity, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( entity == null )
                return;

            AvailableFireteams.Clear();
            bool debug = false;
            AIWardenCoreData factionExternal = this.BaseInfo.ParentBaseInfo.WardenInfo;
            if ( factionExternal.Teams.GetItemCount() == 0 )
            {
                Fireteam team = Fireteam.CreateNewWithIDFromList( factionExternal.Teams );
                team.MyStrengthMultiplierForStrengthCalculation = factionExternal.SubType.MyStrengthMultiplier;
                team.EnemyStrengthMultiplierForStrengthCalculation = factionExternal.SubType.EnemyStrengthMultiplier;
                team.MustCampOnWardenFleetBase = factionExternal.SubType.MustCampOnNinjaBases;
                team.StrengthToBringOnline = this.MinFireteamStrength + Context.RandomToUse.Next( 0, MaxFireteamStrength - MinFireteamStrength );
                factionExternal.Teams.AddIfNotAlreadyIn( team );
            }

            foreach ( Fireteam team in Fireteam.LiveTeamsIn( factionExternal.Teams ) )
            {
                if ( team.status == FireteamStatus.Disbanded ||
                        team.status == FireteamStatus.ReadyToAttack ||
                        team.status == FireteamStatus.Attacking )
                    continue; //if a fireteam is ready to fight, don't send more ships to that team
                if ( team.status == FireteamStatus.Staging && //if a fireteam is already "pretty strong", don't give it more
                     team.DeepInfo.TeamStrength > MaxFireteamStrength )
                    continue;

                if ( team.DeepInfo.CurrentPlanet == null )
                {
                    //this is a brand new fireteam, so it's totally safe.
                    AvailableFireteams.AddIfNotAlreadyIn( team );
                    continue;
                }
                if ( AvailableFireteams.GetItemCount() > 6 ) //if we already have a lot of possible fireteams to use, don't keep looking
                    break;

                Int16 hops = 0;
                int dangerOfTeam = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, entity.Planet, team.DeepInfo.CurrentPlanet, true, out hops );
                Int16 maxHops = 5;
                if ( dangerOfTeam < 80000 ) //let units wander through pretty dangerous spots (80 strength)
                {
                    if ( hops < maxHops )
                        AvailableFireteams.AddIfNotAlreadyIn( team );
                }
            }
            //Base algorithm: if any of our "safe" fireteams are "below strength" then just add to one of those fireteams.
            //If all our fireteams are "Strong Enough" then randomly choose to reinforce an existing one or create a new one
            //We require "safe teams" for the case where it's an octopus map with a Spawner cut off from the rest of the galaxy
            //int teamsUnderStrength = 0;
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
                Fireteam team = Fireteam.CreateNewWithIDFromList( factionExternal.Teams );
                team.MyStrengthMultiplierForStrengthCalculation = factionExternal.SubType.MyStrengthMultiplier;
                team.EnemyStrengthMultiplierForStrengthCalculation = factionExternal.SubType.EnemyStrengthMultiplier;
                team.MustCampOnWardenFleetBase = factionExternal.SubType.MustCampOnNinjaBases;
                team.StrengthToBringOnline = this.MinFireteamStrength + Context.RandomToUse.Next( 0, MaxFireteamStrength - MinFireteamStrength );
                int numShipsForConcentratingEfforts = 5; //before we're too strong, best to concentrate our forces
                if ( factionExternal.Teams.GetItemCount() < numShipsForConcentratingEfforts )
                    team.PercentBestTarget = 100;
                else if ( this.AttachedFaction.SpecialFactionData.FireteamPercentBestTarget > 0 )
                    team.PercentBestTarget = this.AttachedFaction.SpecialFactionData.FireteamPercentBestTarget;
                else
                    team.PercentBestTarget = 85;
                team.DeepInfo.AddUnit(entity);
                entity.FireteamId = team.FireTeamID;
                
                team.DeepInfo.IdentifyCurrentPlanet(); //in case this unit is the first unit
                factionExternal.Teams.AddIfNotAlreadyIn( team );
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path B. There were " + AvailableFireteams.GetItemCount() + " available fireteams, percent " + percentNewTeam, Verbosity.DoNotShow );
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

        public override GameEntity_Squad  GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
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
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, List<FireteamTarget> PreferredTargets, List<FireteamTarget> FallbackTargets, object TeamObj )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AIWarden-GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            FInt falloffForDistance = FInt.FromParts (0, 050);
            Fireteam team = (Fireteam)TeamObj;
            GetPreferredWardenTargets( PreferredTargets, AttachedFaction, Context);
            GetFallbackWardenTargets( FallbackTargets, AttachedFaction, Context);

            bool debug = false;
            if ( debug && tracing )
            {
                tracingBuffer.Add("Getting lurk/target Preferred Targets\n");
                for ( int i = 0; i < PreferredTargets.Count; i++ )
                    tracingBuffer.Add("\t").Add(PreferredTargets[i].GetPlanetName_Safe()).Add(" difficulty ").Add(PreferredTargets[i].dangerOfPath).Add(" \n");
                tracingBuffer.Add("Getting lurk/target Fallback Targets\n");
                for ( int i = 0; i < FallbackTargets.Count; i++ )
                    tracingBuffer.Add("\t").Add(FallbackTargets[i].GetPlanetName_Safe()).Add("\n");   
            }
            FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
        }

        public void GetFallbackWardenTargets( List<FireteamTarget> listToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context)
        {
            var factionCommonExternal = faction.BaseInfo;
            listToFill.Clear();

            foreach ( GameEntity_Squad e in World_AIW2.Instance.Squads( EntityRollupType.WardenFleetLurkLocations ) )
            {
                if (e.Planet.GetIsEitherControllerOrInfluencerHostileTo(faction))
                    continue;

                var item = new FireteamTarget(e);
                listToFill.Add(item);
            }
        }

        public void GetPreferredWardenTargets( List<FireteamTarget> listToFill, Faction faction, ArcenLongTermIntermittentPlanningContext Context)
        {
            var factionCommonExternal = faction.BaseInfo;
            listToFill.Clear();

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.GetControllingFactionType() == FactionType.AI ||
                        planet.GetControllingFactionType() == FactionType.NaturalObject )
                {
                    EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( faction );
                    int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                    if ( hostileStrength < 2000 )
                        continue;
                    if ( planet.PopulationType != PlanetPopulationType.AIHomeworld &&
                        planet.PopulationType != PlanetPopulationType.AIBastionWorld )
                    {
                        if ( myFactionData[FactionStance.Friendly].TotalStrength < 6000 )
                            continue;
                    }
                    int myStrength = myFactionData[FactionStance.Self].TotalStrength + myFactionData[FactionStance.Friendly].TotalStrength;

                    if ( hostileStrength > myStrength / 15 ) //if there is an appreciable enemy force that's remotely dangerous to the planet
                        listToFill.Add( new FireteamTarget( planet ) );
                }
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
                throw new Exception ("No target planet set in get lurk planet?!");

            //int debugCode = 0;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AIWarden-GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            bool preferUnwatchedPlanets = true;
            if ( tracing )
                tracingBuffer.Add("Getting a lurk planet. Target planet " + TargetPlanet.Name).Add(". ").Add(AttachedFaction.BaseInfo.Allegiance).Add("\n");

            // jcf: idea
            /*
            if ( tracing )
                tracingBuffer.Add("Wait we are warden, we don't lurk, we go straight there damnit!\n");
            return TargetPlanet;
            */

            AIWardenCoreData wardenCoreData = this.BaseInfo.ParentBaseInfo.WardenInfo;

            foreach ( Planet.PlanetAtHopDistance _phd in TargetPlanet.PlanetsWithinXHops_NoFilters( -1 ) )
            {
                Planet planet = _phd.Planet;
                Int16 Distance = _phd.Hops;
                if ( planet == TargetPlanet )
                    continue;
                if ( planet.GetControllingOrInfluencingFaction().GetIsHostileTowards(AttachedFaction) )
                    continue; //can't camp on an enemy planet

                // also cant camp anywhere, if its not a warden base
                if (wardenCoreData.SubType.MustCampOnNinjaBases)
                {
                    bool found = false;
                    foreach ( GameEntity_Squad e in planet.Squads( EntityRollupType.WardenFleetLurkLocations ) )
                    {
                        found = true;
                        break;
                    }

                    if (!found)
                    {
                        if ( tracing )
                            tracingBuffer.Add("Can't lurk at ").Add(planet.Name).Add(" because no warden base there.\n");

                        continue;
                    }
                }

                int planetDefensiveStrength = Fireteam.GetPlanetDefensiveStrength( planet, AttachedFaction, true, ref unused,
                                                                          FInt.Zero, FInt.Zero);
                if ( planet.GetControllingFaction().GetIsHostileTowards(AttachedFaction) && planetDefensiveStrength > TeamStrength / 10 )
                    continue;

                //Don't path through any particularly dangerous planets
                Int16 hops = 0;
                //actually go ahead and path through whatever!
                //int totalDifficultyOfPathToLurkPlanet = Fireteam.GetDangerOfPath(faction, Context, CurrentPlanetForTeam, planet, true, out hops);
                //if ( totalDifficultyOfPathToLurkPlanet >= TeamStrength / 10 )
                //    continue;

                if ( Fireteam.DoesPathIncludePlanet(AttachedFaction, Context, PathCacheData, CurrentPlanetForTeam, planet, TargetPlanet) )
                    continue; //don't allow us to path through the target planet on the way to the lurk planet

                int totalDifficultyOfPathToTarget = Fireteam.GetDangerOfPath(AttachedFaction, Context, PathCacheData, planet, TargetPlanet, true, out hops);
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
                        tracingBuffer.Add("\t" + planet.Name + " is now the lurk location; path A" ).Add("\n");
                    dangerOfPathFromBestPlanet = totalDifficultyOfPathToTarget;
                    distanceFromBestPlanet = Distance;
                    hopsFromBestPlanet = hops;
                    bestPlanet = planet;
                }

                if ( dangerOfPathFromBestPlanet == totalDifficultyOfPathToTarget &&
                     (distanceFromBestPlanet > Distance ||
                      hopsFromBestPlanet > hops ) )
                {
                    if ( tracing )
                        tracingBuffer.Add("\t" + planet.Name + " is now the lurk location; path B" ).Add("\n");
                    dangerOfPathFromBestPlanet = totalDifficultyOfPathToTarget;
                    distanceFromBestPlanet = Distance;
                    bestPlanet = planet;
                    hopsFromBestPlanet = hops;
                }
                if ( hopsFromBestPlanet <= 3 && dangerOfPathFromBestPlanet <= TeamStrength * 30 ) //so long as we are only outnumbered 30:1, go there!
                    break; //we found a good lurk within easy striking distance of the planet, so exit now
            }
            if ( tracing && !tracingBuffer.GetIsEmpty() )
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            
            return bestPlanet;
        }
    }
}
