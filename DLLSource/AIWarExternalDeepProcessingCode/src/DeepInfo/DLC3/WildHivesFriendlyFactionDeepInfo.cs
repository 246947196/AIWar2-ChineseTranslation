using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class WildHivesFriendlyFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        #region Required Data
        public WildHivesFriendlyFactionBaseInfo BaseInfo;
        public WildHivesFriendlyFactionDeepInfo() => Cleanup();
        protected override void Cleanup()
        {
            BaseInfo = null;
        }
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            BaseInfo = AttachedFaction.GetExternalBaseInfoAs<WildHivesFriendlyFactionBaseInfo>();
        }
        protected override int MinimumSecondsBetweenLongRangePlannings => 3;
        #endregion

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            BuildSoldiersIfAble( Context );
            BondSoldiersIfAble( Context );
        }

        public void BuildSoldiersIfAble( ArcenHostOnlySimContext Context )
        {
            foreach ( KeyValuePair<GameEntity_Squad, bool> _kv in BaseInfo.hivesOnFriendlyPlanets.GetDisplayList() )
            {
                GameEntity_Squad workingHive = _kv.Key;
                if ( BaseInfo.SecondsUntilNextSoldier( workingHive ) == 0 )
                    WildHivesFactionDeepInfo.DeploySoldiers( workingHive, 1, true, Context );
            }
        }

        public void BondSoldiersIfAble( ArcenHostOnlySimContext Context )
        {
            List<SafeSquadWrapper> potentialLeaders = GameEntity_Squad.GetTemporarySquadList( "WildHivesFriendlyFactionDeepInfo-BondSoldiersIfAble-potentialLeaders", 10f );
            if ( potentialLeaders == null ) //blocked for teardown/shutdown; bail
                return;

            // As the ally of a human, we simply find fleet leaders.
            foreach ( Faction workingFaction in World_AIW2.Instance.Factions )
            {
                if ( workingFaction.Type != FactionType.Player || !AttachedFaction.GetIsFriendlyTowards( workingFaction ) )
                    continue;

                foreach ( GameEntity_Squad leader in workingFaction.Squads( EntityRollupType.FleetLeaders ) )
                {
                    int fleetLines = 0;
                    foreach ( FleetMembership mem in leader.FleetMembership.Fleet.MemberGroupsUnsorted_Sim )
                    {
                        fleetLines++;
                        if ( fleetLines > 1 )
                            break;

                    }
                    if ( fleetLines > 1 )
                        potentialLeaders.Add( leader );
                }
            }

            if ( potentialLeaders.Count > 0 )
                foreach ( GameEntity_Squad workingSoldier in BaseInfo.soldiers.DisplaySquads() )
                {
                    if ( workingSoldier.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength > 1000 )
                        continue; // Wait until no hostiles.

                    GameEntity_Squad leader = World_AIW2.Instance.GetEntityByID_Squad( workingSoldier.MinorFactionStackingID );
                    if ( leader == null || leader.PlanetFaction.Faction.Type != FactionType.Player )
                    {
                        // Not bonded, or bonded unit died. Bond.
                        // Attach a clanling to each leader at random.
                        // Right now this is entirely random. Completly possible to have that one Battlestation you control become the hub of Clanling life.
                        // I personally enjoy this random aspect, but testing should determine if people want it to be more uniformly distributed.
                        workingSoldier.MinorFactionStackingID = potentialLeaders[Context.RandomToUse.Next( potentialLeaders.Count )].PrimaryKeyID;

                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                            World_AIW2.Instance.QueueLogJournalEntryToSidebar( WildHivesFactionBaseInfo.JournalFriendlyBond, string.Empty, AttachedFaction, null, workingSoldier.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    }

                }

            GameEntity_Squad.ReleaseTemporarySquadList( potentialLeaders );
        }

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();

            MoveClanlings_LRP( Context, pathingCacheData );

            pathingCacheData.ReturnToPool();
        }

        public void MoveClanlings_LRP( ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            foreach ( GameEntity_Squad clanling in AttachedFaction.Squads( BaseInfo.SoldierTag ) )
            {
                int debugCode = 0;
                try
                {
                    debugCode = 10;
                    GameEntity_Squad leader = World_AIW2.Instance.GetEntityByID_Squad( clanling.MinorFactionStackingID );

                    debugCode = 20;
                    if ( leader == null )
                        continue;

                    debugCode = 30;
                    if ( clanling.Planet != leader.Planet )
                    {
                        debugCode = 40;
                        // follow the leader
                        PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "WildHivesMoveClanlings", clanling.Planet, leader.Planet, PathingMode.Default, Context, PathCacheData );

                        debugCode = 50;
                        if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                        {
                            debugCode = 60;
                            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse );
                            command.RelatedString = "WildHivesClanlings_FollowLeaderToNewPlanet";

                            debugCode = 70;
                            command.RelatedEntityIDs.Add( clanling.PrimaryKeyID );

                            debugCode = 80;
                            for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                                command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );

                            debugCode = 90;
                            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                        }
                    }
                    else if ( clanling.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength < 1000 )
                    {
                        debugCode = 100;
                        // hug the leader
                        if ( clanling.GetDistanceTo_VeryCheapButExtremelyRough( leader.WorldLocation, RadiusCheck.SubtractRadiiFromDistance ) > 3000 )
                        {
                            debugCode = 110;
                            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCFollowGuardedUnit], GameCommandSource.AnythingElse );
                            command.RelatedString = "WildHivesClanlings_HugLeader";

                            debugCode = 120;
                            command.RelatedEntityIDs.Add( clanling.PrimaryKeyID );

                            debugCode = 130;
                            command.RelatedPoints.Add( leader.WorldLocation );

                            debugCode = 140;
                            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                        }
                    }
                }
                catch ( System.Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "Friendly Wild Hives MoveClanlings_LRP Error at debug stage " + debugCode + ":\n" + e, Verbosity.ShowAsError );
                }
            }
        }
    }
}
