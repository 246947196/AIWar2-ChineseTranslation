using Arcen.Universal;
using System;

using System.Diagnostics;
using System.Threading;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public sealed class PerSecondSimRepair : ArcenLongTermContinuousPlanningClientOrHostContext, IBetweenMapGenPoolable<PerSecondSimRepair>
    {
        public override int GetSecondsToLiveEachCycle()
        {
            return 10;
        }

        public override int GetSecondsAfterWhichToWarnInOneCycle()
        {
            return 2;
        }

        public override float GetTimeAfterWhichToWarnOfNotRunning()
        {
            return 5f;
        }

        private PerSecondSimRepair()
            : base( "_PSec.PerSecondSimRepair", ArcenSimContextType.LongTermContinuous )
        {
        }

        #region Pooling
        public void WipeForReuseAsNewObject()
        {
            this.DoNotRunAgainUntilTime = 0;
            CleanupBase();
        }

        private static readonly BetweenMapGenPool<PerSecondSimRepair> Pool = BetweenMapGenPool<PerSecondSimRepair>.Create_WillNeverBeGCed( "PerSecondSimRepair", 30, 10,
            PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new PerSecondSimRepair(); } );

        public static PerSecondSimRepair GetFromPoolOrCreate()
        {
            PerSecondSimRepair context = Pool.GetFromPoolOrCreate();
            return context;
        }
        #endregion

        private float DoNotRunAgainUntilTime;
        public override bool GetNeedsToRun()
        {
            return this.GetIsWorkDone() && this.DoNotRunAgainUntilTime < ArcenTime.TimeSinceStartF;
        }

        private ThreadingExchanger wasAlreadyRunning = new ThreadingExchanger( "PerSecondSimRepair", 300f );

        protected override void Execute()
        {
            if ( wasAlreadyRunning.IsBusy() )
                return;
            if ( !wasAlreadyRunning.DoNextOnlyIfNotAlreadyBusy( string.Empty ) )
                return;

            float startTime = ArcenTime.TimeSinceStartF;
            try
            {
                this.DoCentralLoop_ClientAndHost();
            }
            catch ( ArcenPleaseStopThisThreadException )
            {
                wasAlreadyRunning.MarkAsNoLongerBusy();
            }
            catch ( Exception e )
            {
                wasAlreadyRunning.MarkAsNoLongerBusy();
                ArcenDebugging.ArcenDebugLogSingleLine( "PerSecondSimRepair Error: " + e, Verbosity.ShowAsError );
            }

            wasAlreadyRunning.MarkAsNoLongerBusy();
            //hey, we finished; we do that every time, actually
            {
                float finishedTime = ArcenTime.TimeSinceStartF;

                //try to run this every 1 seconds, or with 0.5 second gaps, whichever is slower
                float timeToWaitBeforeNextStart = 1f - (finishedTime - startTime);
                if ( timeToWaitBeforeNextStart < 0.5f )
                    timeToWaitBeforeNextStart = 0.5f;
                this.DoNotRunAgainUntilTime = ArcenTime.TimeSinceStartF + timeToWaitBeforeNextStart;
            }
        }


        private readonly Dictionary<int, GameEntity_Squad> squadDictionaryCheck = Dictionary<int, GameEntity_Squad>.Create_WillNeverBeGCed( 40000, "PerSecondSimRepair-squadDictionaryCheck", 40000 );

        private Dictionary<GameEntity_Squad, int> squadMissesCountOld = Dictionary<GameEntity_Squad, int>.Create_WillNeverBeGCed( 300, "PerSecondSimRepair-squadMissesCount", 300 );
        private Dictionary<GameEntity_Squad, int> squadMissesCountNew = Dictionary<GameEntity_Squad, int>.Create_WillNeverBeGCed( 300, "PerSecondSimRepair-squadMissesCount", 300 );

        /// <summary>
        /// This is happening on a background thread that is not sim-blocking in any way.
        /// </summary>
        private void DoCentralLoop_ClientAndHost()
        {
            int debugStage = 0;
            try
            {
                int squadsCleanedUp = 0;

                debugStage = 4000;

                squadDictionaryCheck.Clear();

                foreach ( Planet planet in World_AIW2.Instance.Planets( true ) )
                {
                    debugStage = 4200;
                    for ( int i = 0; i < planet.Factions.Count; i++ )
                    {
                        debugStage = 4300;
                        PlanetFaction pFac = planet.Factions[i];
                        foreach ( GameEntity_Squad squad in pFac.Entities.Squads() )
                        {
                            if ( squad == null )
                                continue;

                            if ( squad.IsInQuarantine || squad.IsInPoolAtAll )
                            {
                                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client) //only complain about this if this is not an MP client
                                    ArcenDebugging.ArcenDebugLogSingleLine( "Found a squad still in a PlanetFaction list after it was already in quarantine, yikes!", Verbosity.ShowAsError );
                                continue; // was RemoveAndContinue (no-op) //hopefully this fixes it at least
                            }

                            debugStage = 5100;
                            //check for duplicates
                            if ( squadDictionaryCheck.ContainsKey( squad.PrimaryKeyID ) )
                            {
                                debugStage = 5200;
                                GameEntity_Squad otherSquad = squadDictionaryCheck[squad.PrimaryKeyID];
                                debugStage = 5210;
                                if ( otherSquad != squad )
                                {
                                    debugStage = 5300;
                                    if ( World_AIW2.Instance.GameSecond - squad.GameSecondEnteredThisPlanet > 5 )
                                    {
                                        debugStage = 5400;

                                        GameEntity_Squad squadToDelete = null;
                                        GameEntity_Squad squadToKeep = null;
                                        bool tellThePlayerAboutThis = true;
                                        if ( squad.Planet == null || squad.PlanetFaction == null )
                                        {
                                            //one of these is bogus, so just get rid of it silently
                                            tellThePlayerAboutThis = false;
                                            squadToDelete = squad;
                                            squadToKeep = otherSquad;
                                        }
                                        else if ( otherSquad.Planet == null || otherSquad.PlanetFaction == null )
                                        {
                                            //the other of these is bogus, so just get rid of it silently
                                            tellThePlayerAboutThis = false;
                                            squadToDelete = otherSquad;
                                            squadToKeep = squad;
                                        }
                                        else
                                        {
                                            //dang, they both look valid, so just delete the newer one
                                            //that's when we need to tell the player
                                            squadToDelete = squad.GameSecondCreated < otherSquad.GameSecondCreated ? otherSquad : squad;
                                            squadToKeep = squad.GameSecondCreated < otherSquad.GameSecondCreated ? squad : otherSquad;
                                        }

                                        if ( tellThePlayerAboutThis )
                                            ArcenDebugging.ArcenDebugLogSingleLine( "Found PKID twice: " + squad.PrimaryKeyID + " and Squad 1 was on planet " +
                                                squad.GetPlanetNameSafe() + " for faction" + squad.GetFactionDisplayNameSafe() +
                                                " while squad2 was on planet " + otherSquad.GetPlanetNameSafe() +
                                                " for faction" + otherSquad.GetFactionDisplayNameSafe() + " . " + squad.TypeData.DisplayName, Verbosity.ShowAsError );

                                        squadToDelete.OnlyInMapgenOrInActuallyGettingRidOfEntities_ImmediatelyRemoveFromSim( InstancedRendererDeactivationReason.RemoveOldUnitWeNoLongerNeed );
                                        World_AIW2.Instance.TryCorrectEntityStatusAndReference( squadToKeep.PrimaryKeyID, squadToKeep, InstancedRendererDeactivationReason.IAmAliveAndFineActually );

                                        continue;
                                    }
                                }
                            }
                            else
                            {
                                debugStage = 5500;
                                squadDictionaryCheck[squad.PrimaryKeyID] = squad;
                            }

                            debugStage = 6100;
                            SquadRegistryInfo registryInfo = World_AIW2.Instance.GetEntityRegistryInfo_Squad( squad.PrimaryKeyID );
                            debugStage = 6200;
                            if ( registryInfo != null )
                            {
                                debugStage = 6300;
                                if ( registryInfo.Squad != squad )
                                {
                                    debugStage = 6400;
                                    if ( registryInfo.Squad == null )
                                    {
                                        debugStage = 6500;
                                        if ( squad.HasBeenRemovedFromSim || squad.IsInPoolAtAll )
                                        {
                                            debugStage = 6600;
                                            //ArcenDebugging.ArcenDebugLogSingleLine( "squad still existed in planetfaction despite being removed: " + squad.PrimaryKeyID, Verbosity.DoNotShow );
                                            squadsCleanedUp++;
                                            pFac.Entities.RemoveSquad( squad );
                                        }
                                        else
                                        {
                                            debugStage = 6700;
                                            InstancedRendererDeactivationReason deactivated = registryInfo.CurrentStatus;
                                            if ( deactivated < InstancedRendererDeactivationReason.IAmAliveAndFineActually )
                                            {
                                                debugStage = 6800;
                                                //ArcenDebugging.ArcenDebugLogSingleLine( "squad still existed in planetfaction despite being in death registry: " + squad.PrimaryKeyID +
                                                //    " (" + deactivated + ")", Verbosity.DoNotShow );
                                                squadsCleanedUp++;
                                                pFac.Entities.RemoveSquad( squad );
                                            }
                                            else if ( deactivated == InstancedRendererDeactivationReason.IAmAliveAndFineActually )
                                            {
                                                debugStage = 6900;
                                                //ArcenDebugging.ArcenDebugLogSingleLine( "null squad fixed in central registry: " + squad.PrimaryKeyID, Verbosity.DoNotShow );
                                                squadsCleanedUp++;
                                                registryInfo.SetSquad( squad, "PerSecondSimRepair-A" );
                                                registryInfo.Faction = squad.GetFactionOrNull_Safe();
                                                registryInfo.Fleet = squad.GetFleetOrNull_Safe();
                                            }
                                        }
                                    }
                                    else if ( registryInfo.Squad != null )
                                    {
                                        debugStage = 8100;
                                        if ( registryInfo.Squad.FleetMembership == squad.FleetMembership )
                                        {
                                            debugStage = 8200;
                                            //ArcenDebugging.ArcenDebugLogSingleLine( "Sim repair: removing duplicate copy of squad: " + squad.PrimaryKeyID + ", a " + registeredSquadInfo.LeftItem?.TypeData?.InternalName, Verbosity.ShowAsError );
                                            squadsCleanedUp++;
                                            pFac.Entities.RemoveSquad( squad );
                                            squad.ReturnToPool();
                                        }
                                        else
                                        {
                                            debugStage = 8400;
                                            if ( registryInfo.Squad.HasBeenRemovedFromSim )
                                            {
                                                debugStage = 8500;
                                                //ArcenDebugging.ArcenDebugLogSingleLine( "Sim repair: replacing dead squad with ID in central registry: " + squad.PrimaryKeyID + " with living " +
                                                //    registeredSquadInfo.LeftItem.PrimaryKeyID + " (" + registeredSquadInfo.LeftItem?.TypeData?.InternalName + ") vs the normal " +
                                                //    squad?.TypeData?.InternalName, Verbosity.ShowAsError );
                                                squadsCleanedUp++;
                                                registryInfo.SetSquad( squad, "PerSecondSimRepair-B" );
                                                registryInfo.Faction = squad.GetFactionOrNull_Safe();
                                                registryInfo.Fleet = squad.GetFleetOrNull_Safe();
                                            }
                                            else
                                            {
                                                debugStage = 8600;
                                                //ArcenDebugging.ArcenDebugLogSingleLine( "Sim repair: removing duplicate squad with ID in central registry: " + squad.PrimaryKeyID + " was actually " +
                                                //    registeredSquadInfo.LeftItem.PrimaryKeyID + " (" + registeredSquadInfo.LeftItem?.TypeData?.InternalName + ") vs the normal " +
                                                //    squad?.TypeData?.InternalName, Verbosity.ShowAsError );
                                                squadsCleanedUp++;
                                                pFac.Entities.RemoveSquad( squad );
                                                squad.ReturnToPool();
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                debugStage = 9100;
                                World_AIW2.Instance.TryCorrectEntityStatusAndReference( squad.PrimaryKeyID, squad, InstancedRendererDeactivationReason.IAmAliveAndFineActually );
                            }

                            debugStage = 12100;
                            if ( squadsCleanedUp > 0 )
                            {
                                //This is extraneous information that gets to be quite large on MP clients apparently.
                                //ArcenDebugging.ArcenDebugLogSingleLine( "Sim repair: cleaned up " + squadsCleanedUp + " squads that were divergent in the central registry compared to planetfactions.  No problems now.", Verbosity.DoNotShow );
                            }
                        }
                    }
                }

                debugStage = 32100;

                {
                    Dictionary<GameEntity_Squad, int> squadMisses = squadMissesCountOld;
                    squadMissesCountOld = squadMissesCountNew;
                    squadMissesCountNew = squadMisses;

                    squadMissesCountNew.Clear();
                }

                debugStage = 33100;

                float timeFromWhichToRemove = ArcenTime.UnpausedTimeSinceStartForVisualEffectsF - 600f; //the amount of unpaued time since this died.  Wait for 10 minutes before doing any deleting

                //now that we looped over all the planets, loop over the central dict
                ConcurrentDictionary<int, SquadRegistryInfo> centralDict = World_AIW2.Instance.GetSquadCentralLookup_OnlyDoThisIfYouReallyKnowWhatYouAreDoing();
                foreach ( KeyValuePair<int, SquadRegistryInfo> kv in centralDict )
                {
                    debugStage = 34100;
                    if ( timeFromWhichToRemove > 0 )
                    {
                        float timeLastDied = kv.Value.LastUnpausedTimeUnregistered;
                        if ( timeLastDied  > 0 && timeLastDied < timeFromWhichToRemove )
                        {
                            GameEntity_Squad squad = kv.Value.Squad;
                            if ( squad == null )
                            {
                                //it has been 10 unpaused minutes since it was marked as dead, and it's still dead.
                                //so... get rid of this excess data.
                                centralDict.TryRemove( kv.Key, 3 );
                            }
                        }
                    }

                    debugStage = 34200;
                    switch ( kv.Value.CurrentStatus )
                    {
                        case InstancedRendererDeactivationReason.Unknown:
                        case InstancedRendererDeactivationReason.IAmAliveAndFineActually:
                        case InstancedRendererDeactivationReason.IsActuallyReversalOfDeath:
                        case InstancedRendererDeactivationReason.SwitchedPlanetOnClientBecauseOfDesync:
                        case InstancedRendererDeactivationReason.ExplodeOnClientBecauseGhostTimeout:
                        case InstancedRendererDeactivationReason.ExplodeOnClientBecauseGhostSeparateFromRealEntity:
                        case InstancedRendererDeactivationReason.HadToBeCleanedFromCentralRegistry:
                        case InstancedRendererDeactivationReason.DiedOnTimeFromZeroHealth:
                        case InstancedRendererDeactivationReason.DiedBelatedlyFromZeroHealth:
                        case InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType:
                        case InstancedRendererDeactivationReason.RemovedToBeAddedToStack:
                        case InstancedRendererDeactivationReason.LastMinuteWasUnknownButToldToRemoveSoExplode:
                            debugStage = 35100;
                            GameEntity_Squad squad = kv.Value.Squad;
                            if ( squad == null )
                            {
                                if ( kv.Value.LastUnpausedTimeUnregistered <= 0 )
                                    kv.Value.LastUnpausedTimeUnregistered = ArcenTime.TimeSinceStartF;
                                continue;
                            }
                            
                            debugStage = 35200;
                            if ( squad.IsInQuarantine )
                            {
                                if ( kv.Value.LastUnpausedTimeUnregistered <= 0 )
                                    kv.Value.LastUnpausedTimeUnregistered = ArcenTime.TimeSinceStartF;
                                //holy crap, this is a squad that has died and we should not be talking about
                                //forget immediately that this exists.
                                //Because of how quarantine and retrieval works, checking IsInQuarantine rather than IsInPoolAtAll is deemed safer
                                kv.Value.SetSquad( null, "PerSecondSimRepair:BecauseSquadIsInQuarantine!" );
                                continue;
                            }
                            
                            debugStage = 35300;

                            if ( kv.Value.LastUnpausedTimeUnregistered > 0 )
                                kv.Value.LastUnpausedTimeUnregistered = 0;

                            //don't yell about this until in any way until this thing has existed for at least two game seconds.  Otherwise we get false alarms
                            if ( World_AIW2.Instance.GameSecond - squad.GameSecondCreated < 2 )
                                continue;
                            
                            //don't yell about this until in any way until this thing has been on a given planet for at least two game seconds.  Otherwise we get false alarms
                            if ( World_AIW2.Instance.GameSecond - squad.GameSecondEnteredThisPlanet < 2 )
                                continue;

                            debugStage = 35500;

                            if ( !squadDictionaryCheck.ContainsKey( kv.Key ) )
                            {
                                debugStage = 36100;
                                int missCount = squadMissesCountOld[squad] + 1;
                                squadMissesCountNew[squad] = missCount;
                                if ( missCount >= 5 )
                                {
                                    debugStage = 36200;
                                    PlanetFaction pFaction = squad.PlanetFaction;
                                    if ( missCount >= 10 )
                                    {
                                        if ( pFaction != null )
                                            pFaction.RemoveEntity( squad );
                                        kv.Value.CurrentStatus = InstancedRendererDeactivationReason.HadToBeCleanedFromCentralRegistry;
                                        kv.Value.SetSquad( null, "PerSecondSimRepair-D" );
                                        squad.ReturnToPool();
                                    }
                                    else if ( pFaction != null )
                                        pFaction.AddEntity( squad, "Sim Repair Thinks Squad Was Added Back To Its Existing PFaction" );
                                    else
                                    {
                                        if ( pFaction != null )
                                            pFaction.RemoveEntity( squad );
                                        kv.Value.CurrentStatus = InstancedRendererDeactivationReason.HadToBeCleanedFromCentralRegistry;
                                        kv.Value.SetSquad( null, "PerSecondSimRepair-E" );
                                        squad.ReturnToPool();
                                    }
                                }
                            }
                            else //okay, things seem to be fine
                            {
                                debugStage = 37100;
                                Faction fac = squad.GetFactionOrNull_Safe();
                                Fleet fleet = squad.GetFleetOrNull_Safe();

                                debugStage = 37200;
                                if ( fac != null && kv.Value.Faction != fac )
                                {
                                    debugStage = 37300;
                                    if ( kv.Value.Faction != null )
                                    {
                                        debugStage = 37400;
                                        ArcenDebugging.ArcenDebugLogSingleLine( "Ship " + squad.TypeData.DisplayName + " - " + squad.NonSimTrueUniqueID + " was part of faction " + kv.Value.Faction?.GetDisplayName() +
                                            " but has now somehow jumped to " + fac?.GetDisplayName() + ".  Yikes!  Anything of note just happen in the game in terms of actions taken? \nSquad Faction ReasonCode: " +
                                            squad.LastReasonsForPlanetFactionChange.GetAllContentsAsCommaSeparatedStringForDebugging() + "  \nSquad Planet ReasonCode: " +
                                            squad.LastReasonsForPlanetChange.GetAllContentsAsCommaSeparatedStringForDebugging() + "  \nSquad Fleet ReasonCode List: " +
                                            squad.LastReasonsForFleetMembershipChange.GetAllContentsAsCommaSeparatedStringForDebugging() +
                                            "  \nSquad Pool status history: " +
                                            squad.Debug_GetCondensedHistoryOfPoolStatus(), Verbosity.ShowAsError );

                                        //go ahead and set it to the new faction, even though that's wrong, just to keep it from spamming us errors over one unit
                                        kv.Value.Faction = fac;

                                        //we have a problem case, so let's see what else we can find
                                        foreach ( GameEntity_Squad otherSquad in World_AIW2.Instance.Squads() )
                                        {
                                            if ( otherSquad.NonSimTrueUniqueID == squad.NonSimTrueUniqueID )
                                            {
                                                ArcenDebugging.ArcenDebugLogSingleLine( "(After report) NSTUID-A Match: " + otherSquad.TypeData.DisplayName + " - " + otherSquad.NonSimTrueUniqueID +
                                                    " pkid " + otherSquad.PrimaryKeyID + " vs " + squad.PrimaryKeyID +
                                                    " faction " + otherSquad.GetFactionOrNull_Safe()?.GetDisplayName() + " vs " + squad.GetFactionOrNull_Safe()?.GetDisplayName() +
                                                    " fleet " + otherSquad.GetFleetExtendedDebugInfo_Safe() + " vs " + squad.GetFleetExtendedDebugInfo_Safe() +
                                                    " planet " + otherSquad.Planet?.Name + " vs " + squad.Planet?.Name +
                                                    ".  \nOtherSquad Faction ReasonCode: " +
                                                    otherSquad.LastReasonsForPlanetFactionChange.GetAllContentsAsCommaSeparatedStringForDebugging() + "  \nOtherSquad Planet ReasonCode: " +
                                                    otherSquad.LastReasonsForPlanetChange.GetAllContentsAsCommaSeparatedStringForDebugging() + "  \nOtherSquad Fleet ReasonCode List: " +
                                                    otherSquad.LastReasonsForFleetMembershipChange.GetAllContentsAsCommaSeparatedStringForDebugging() +
                                                    "  \nOtherSquad Pool status history: " +
                                                    otherSquad.Debug_GetCondensedHistoryOfPoolStatus(), Verbosity.DoNotShow );
                                            }
                                            else if ( otherSquad.PrimaryKeyID == squad.PrimaryKeyID )
                                            {
                                                ArcenDebugging.ArcenDebugLogSingleLine( "(After report) PKID Match: " + otherSquad.TypeData.DisplayName + " - " + otherSquad.NonSimTrueUniqueID +
                                                    " nstuid " + otherSquad.NonSimTrueUniqueID + " vs " + squad.NonSimTrueUniqueID +
                                                    " faction " + otherSquad.GetFactionOrNull_Safe()?.GetDisplayName() + " vs " + squad.GetFactionOrNull_Safe()?.GetDisplayName() +
                                                    " fleet " + otherSquad.GetFleetExtendedDebugInfo_Safe() + " vs " + squad.GetFleetExtendedDebugInfo_Safe() +
                                                    " planet " + otherSquad.Planet?.Name + " vs " + squad.Planet?.Name +
                                                    ".  \nOtherSquad Faction ReasonCode: " +
                                                    otherSquad.LastReasonsForPlanetFactionChange.GetAllContentsAsCommaSeparatedStringForDebugging() + "  \nOtherSquad Planet ReasonCode: " +
                                                    otherSquad.LastReasonsForPlanetChange.GetAllContentsAsCommaSeparatedStringForDebugging() + "  \nOtherSquad Fleet ReasonCode List: " +
                                                    otherSquad.LastReasonsForFleetMembershipChange.GetAllContentsAsCommaSeparatedStringForDebugging() +
                                                    "  \nOtherSquad Pool status history: " +
                                                    otherSquad.Debug_GetCondensedHistoryOfPoolStatus(), Verbosity.DoNotShow );
                                            }

                                        }

                                        ArcenDebugging.ArcenDebugLogSingleLine( "(After report) that's all the matches we found", Verbosity.DoNotShow );
                                    }
                                    else
                                        kv.Value.Faction = fac; //correct a null faction
                                }

                                debugStage = 42100;
                                if ( fleet != null && kv.Value.Fleet != fleet )
                                {
                                    debugStage = 43100;
                                    if ( kv.Value.Fleet != null )
                                    {
                                        debugStage = 44100;

                                        Faction facOfNewFleet = fleet.Faction;
                                        Faction facOfOldFleet = kv.Value.Fleet.Faction;
                                        if ( facOfOldFleet == null || facOfOldFleet.Type != FactionType.Player )
                                        {
                                            //if there was no fleet, or it was not a player fleet in general, then just update to the new version rather than complaining... maybe
                                            if ( facOfOldFleet == null || facOfOldFleet == facOfNewFleet )
                                            {
                                                //okay, this faction is null, or the faction is the same one owning the new fleet.  So don't complain, just update the data.
                                                kv.Value.Fleet = fleet;
                                                continue;
                                            }
                                            else if ( facOfNewFleet != null && facOfOldFleet != null && facOfNewFleet.SpecialFactionData.InternalName == facOfOldFleet.SpecialFactionData.InternalName )
                                            {
                                                //okay, this was a transfer between two fleets of different factions, but the same faction type, and it's not player stuff, so just allow it
                                                kv.Value.Fleet = fleet;
                                                continue;
                                            }
                                            else if ( facOfNewFleet != null && facOfOldFleet != null && 
                                                ( facOfNewFleet.SpecialFactionData.IsConsideredPartOfTheAIFactionForFilterPurposes || facOfNewFleet.Type == FactionType.AI) &&
                                                (facOfOldFleet.SpecialFactionData.IsConsideredPartOfTheAIFactionForFilterPurposes || facOfOldFleet.Type == FactionType.AI) )
                                            {
                                                //okay, this was a transfer between two parts of an AI, so allow that, too
                                                kv.Value.Fleet = fleet;
                                                continue;
                                            }
                                        }

                                        debugStage = 44200;
                                        if ( facOfOldFleet == facOfNewFleet )
                                        {
                                            //we'll consider this fine
                                        }
                                        else if ( facOfOldFleet != null && facOfOldFleet.Type == FactionType.Player && facOfNewFleet != null && facOfNewFleet.Type == FactionType.Player )
                                        {
                                            //this is also fine
                                        }
                                        else
                                        {
                                            if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client ) //this is okay to move fleets
                                                ArcenDebugging.ArcenDebugLogSingleLine( "Ship " + squad.TypeData.DisplayName + " - " + squad.NonSimTrueUniqueID +
                                                    " was part of fleet " + kv.Value.Fleet?.GetName() + " of faction " + facOfOldFleet?.GetDisplayName() +
                                                    " but has now somehow jumped to " + fleet?.GetName() + " of faction " + facOfNewFleet?.GetDisplayName() + ".  Yikes!  Anything of note just happen in the game in terms of actions taken? \nSquad Faction ReasonCode: " +
                                                    squad.LastReasonsForPlanetFactionChange.GetAllContentsAsCommaSeparatedStringForDebugging() + "  \nSquad Planet ReasonCode: " +
                                                    squad.LastReasonsForPlanetChange.GetAllContentsAsCommaSeparatedStringForDebugging() + "  \nSquad Fleet Squad Fleet ReasonCode List: " +
                                                    squad.LastReasonsForFleetMembershipChange.GetAllContentsAsCommaSeparatedStringForDebugging() +
                                                    "  \nSquad Pool status history: " +
                                                    squad.Debug_GetCondensedHistoryOfPoolStatus(), Verbosity.ShowAsError );
                                        }

                                        //go ahead and set it to the new fleet, even though that's wrong, just to keep it from spamming us errors over one unit
                                        kv.Value.Fleet = fleet;

                                        if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                                            continue; //ignore further errors on the client

                                        debugStage = 45100;
                                        
                                        //we have a problem case, so let's see what else we can find
                                        foreach ( GameEntity_Squad otherSquad in World_AIW2.Instance.Squads() )
                                        {
                                            debugStage = 45200;
                                            if ( otherSquad.NonSimTrueUniqueID == squad.NonSimTrueUniqueID )
                                            {
                                                debugStage = 45300;
                                                ArcenDebugging.ArcenDebugLogSingleLine( "(After report) NSTUID-B Match: " + otherSquad.TypeData.DisplayName + " - " + otherSquad.NonSimTrueUniqueID +
                                                    " pkid " + otherSquad.PrimaryKeyID + " vs " + squad.PrimaryKeyID +
                                                    " faction " + otherSquad.GetFactionOrNull_Safe()?.GetDisplayName() + " vs " + squad.GetFactionOrNull_Safe()?.GetDisplayName() +
                                                    " fleet " + otherSquad.GetFleetExtendedDebugInfo_Safe() + " vs " + squad.GetFleetExtendedDebugInfo_Safe() +
                                                    " planet " + otherSquad.Planet?.Name + " vs " + squad.Planet?.Name +
                                                    ".  \nOtherSquad Faction ReasonCode: " +
                                                    otherSquad.LastReasonsForPlanetFactionChange.GetAllContentsAsCommaSeparatedStringForDebugging() + "  \nOtherSquad Planet ReasonCode: " +
                                                    otherSquad.LastReasonsForPlanetChange.GetAllContentsAsCommaSeparatedStringForDebugging() + "  \nOtherSquad Fleet ReasonCode List: " +
                                                    otherSquad.LastReasonsForFleetMembershipChange.GetAllContentsAsCommaSeparatedStringForDebugging() +
                                                    "  \nOtherSquad Pool status history: " +
                                                    otherSquad.Debug_GetCondensedHistoryOfPoolStatus(), Verbosity.DoNotShow );
                                            }
                                            else if ( otherSquad.PrimaryKeyID == squad.PrimaryKeyID )
                                            {
                                                debugStage = 45400;
                                                ArcenDebugging.ArcenDebugLogSingleLine( "(After report) PKID Match: " + otherSquad.TypeData.DisplayName + " - " + otherSquad.NonSimTrueUniqueID +
                                                    " nstuid " + otherSquad.NonSimTrueUniqueID + " vs " + squad.NonSimTrueUniqueID +
                                                    " faction " + otherSquad.GetFactionOrNull_Safe()?.GetDisplayName() + " vs " + squad.GetFactionOrNull_Safe()?.GetDisplayName() +
                                                    " fleet " + otherSquad.GetFleetExtendedDebugInfo_Safe() + " vs " + squad.GetFleetExtendedDebugInfo_Safe() +
                                                    " planet " + otherSquad.Planet?.Name + " vs " + squad.Planet?.Name +
                                                    ".  \nOtherSquad Faction ReasonCode: " +
                                                    otherSquad.LastReasonsForPlanetFactionChange.GetAllContentsAsCommaSeparatedStringForDebugging() + "  \nOtherSquad Planet ReasonCode: " +
                                                    otherSquad.LastReasonsForPlanetChange.GetAllContentsAsCommaSeparatedStringForDebugging() + "  \nOtherSquad Fleet ReasonCode List: " +
                                                    otherSquad.LastReasonsForFleetMembershipChange.GetAllContentsAsCommaSeparatedStringForDebugging() +
                                                    "  \nOtherSquad Pool status history: " +
                                                    otherSquad.Debug_GetCondensedHistoryOfPoolStatus(), Verbosity.DoNotShow );
                                            }

                                        }

                                        ArcenDebugging.ArcenDebugLogSingleLine( "(After report) that's all the matches we found", Verbosity.DoNotShow );

                                    }
                                    else
                                        kv.Value.Fleet = fleet; //correct a null fleet
                                }
                            }
                            break;
                    }
                }

                debugStage = 62100;

                //now that we looped over the central dict, loop again over our full list from the planet factions
                foreach ( KeyValuePair<int, GameEntity_Squad> kv in squadDictionaryCheck )
                {
                    GameEntity_Squad kvValueSquad = kv.Value;
                    if ( kvValueSquad == null || kvValueSquad.PrimaryKeyID != kv.Key || kvValueSquad.IsInPoolAtAll || kvValueSquad.IsInQuarantine || kvValueSquad.HasBeenRemovedFromSim )
                        continue; //these are all cases of it having been destroyed, so ignore it

                    debugStage = 63100;
                    SquadRegistryInfo registryInfo = World_AIW2.Instance.GetEntityRegistryInfo_Squad( kv.Key );
                    if ( registryInfo != null )
                    {
                        debugStage = 64100;
                        if ( registryInfo.Squad == null )
                        {
                            debugStage = 64200;
                            registryInfo.SetSquad( kvValueSquad, "PerSecondSimRepair-F" ) ;
                            int missCount = squadMissesCountOld[kvValueSquad] + 1;
                            squadMissesCountNew[kvValueSquad] = missCount;
                            if ( missCount >= 5 )
                            {
                                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client) //we don't care on the client
                                    ArcenDebugging.ArcenDebugLogSingleLine( "Found PKID in planetfaction lists but and central lists BUT it was null in the central registry: " + kv.Key + " at planet " +
                                        kvValueSquad.GetPlanetNameSafe() + " for faction " + kvValueSquad.GetFactionDisplayNameSafe() + " . " + kvValueSquad.TypeData.DisplayName +
                                        " (missed at least 5 times)", Verbosity.ShowAsError );
                            }
                        }
                        debugStage = 65100;
                        switch ( registryInfo.CurrentStatus )
                        {
                            case InstancedRendererDeactivationReason.Unknown:
                            case InstancedRendererDeactivationReason.IAmAliveAndFineActually:
                                break; //these are fine
                            default:
                                registryInfo.CurrentStatus = InstancedRendererDeactivationReason.IAmAliveAndFineActually;
                                break; //go ahead and fix  anything else, don't complain
                        }
                    }
                    else
                    {
                        debugStage = 66100;

                        int missCount = squadMissesCountOld[kvValueSquad] + 1;
                        squadMissesCountNew[kvValueSquad] = missCount;
                        if ( missCount >= 5 )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "Found PKID in planetfaction lists but not central lists: " + kv.Key + " at planet " +
                                kvValueSquad.GetPlanetNameSafe() + " for faction " + kvValueSquad.GetFactionDisplayNameSafe() + " . " + kvValueSquad.TypeData.DisplayName +
                                " (missed at least 5 times)", Verbosity.ShowAsError );
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "PerSecondSimRepair-DoCentralLoop_ClientAndHost at debugStage " + debugStage + ". \nError: " + e, Verbosity.ShowAsError );
            }
        }
    }
}