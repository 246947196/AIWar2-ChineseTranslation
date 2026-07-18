using Arcen.AIW2.Core;
using Arcen.Universal;
using System;


namespace Arcen.AIW2.External
{
    public enum MigrantFleetEscortSize
    {
        Large,
        Medium,
        Small
    }
    public class MigrantFleetsFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        //serialized
        private readonly Dictionary<MigrantFleetEscortSize, int> escortTriggerTimers = Dictionary<MigrantFleetEscortSize, int>.Create_WillNeverBeGCed( 600, "MigrantFleetsFactionBaseInfo-escortTriggerTimers" );
        private readonly Dictionary<MigrantFleetEscortSize, short> escortsTriggered = Dictionary<MigrantFleetEscortSize, short>.Create_WillNeverBeGCed( 600, "MigrantFleetsFactionBaseInfo-escortsTriggered" );

        public short KnownMigrantsOutsideOurGalaxy { get; set; }

        public readonly Dictionary<int, bool> migrantsThatHaveBeenHereBefore = Dictionary<int, bool>.Create_WillNeverBeGCed( 600, "MigrantFleetsFactionBaseInfo-migrantsThatHaveBeenHereBefore" );
        public readonly Dictionary<short, int> secondsSinceWormholesAppeared = Dictionary<short, int>.Create_WillNeverBeGCed( 600, "MigrantFleetsFactionBaseInfo-secondsSinceWormholesAppeared" );
        public readonly Dictionary<int, int> secondsSinceMigrantsSafe = Dictionary<int, int>.Create_WillNeverBeGCed( 60, "MigrantFleetsFactionBaseInfo-secondsSinceMigrantsSafe" );
        public readonly Dictionary<int, short> migrantOriginPlanets = Dictionary<int, short>.Create_WillNeverBeGCed( 60, "MigrantFleetsFactionBaseInfo-migrantOriginPlanets" );

        //not serialized
        public int Intensity = 0;
        public bool humanAlly;

        public readonly DoubleBufferedValue<int> clanlingCount = new DoubleBufferedValue<int>( 0 );

        public MigrantFleetsDifficulty Difficulty { get; private set; }

        public DoubleBufferedList<Planet> migrantWormholePlanets = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 300, "MigrantFleets-migrantWormholePlanets" );
        public DoubleBufferedList<SafeSquadWrapper> migrants = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "MigrantFleets-migrants" );
        public DoubleBufferedList<SafeSquadWrapper> clanlings = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "MigrantFleets-clanlings" );
        public DoubleBufferedList<SafeSquadWrapper> clanlingsWithoutParent = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "MigrantFleets-clanlingsWithoutParent" );

        #region Constants
        //constants
        public readonly string NeinzulMigrantTag = "MigrantEnclave";
        public readonly string NeinzulMigrantWormholeTag = "MigrantWormhole";

        public readonly string NeinzulChamberTag = "NeinzulChamber";
        public readonly string NeinzulClanlingTag = "NeinzulClanling";

        public readonly short Migrant_WormholeWarpInTimer = 300;
        public readonly short Migrant_SafetyTimer = 180;
        public readonly short Migrant_ChamberTimer = 300;
        #endregion
        #region Journal Entries
        private readonly string Journal_Base = "NA_MigrantFleet_";

        public string Journal_Wormhole_Unknown => Journal_Base + "UnknownWormhole";
        public string Journal_MigrantArrives_Unknown => Journal_Base + "UnknownMigrantArrives";
        public string Journal_MigrantEscorted_Unknown => Journal_Base + "UnknownMigrantEscorted";
        public string Journal_MigrantLeaves_Unknown => Journal_Base + "UnknownMigrantLeaves";

        public string Journal_Wormhole_Known => Journal_Base + "KnownWormhole";
        public string Journal_MigrantArrives_Known => Journal_Base + "KnownMigrantArrives";
        public string Journal_MigrantEscorted_Known => Journal_Base + "KnownMigrantEscorted";
        public string Journal_MigrantLeaves_Known => Journal_Base + "KnownMigrantLeaves";

        public string Journal_ClanlingBond => Journal_Base + "ClanlingBond";
        #endregion

        public MigrantFleetsFactionBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            escortTriggerTimers.Clear();
            escortsTriggered.Clear();

            KnownMigrantsOutsideOurGalaxy = 0;

            clanlingCount.Clear();

            migrantsThatHaveBeenHereBefore.Clear();
            secondsSinceWormholesAppeared.Clear();
            secondsSinceMigrantsSafe.Clear();
            migrantOriginPlanets.Clear();

            Intensity = 0;
        }

        #region Serialization and Deserialization
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, escortTriggerTimers.Count, "Migrant Trigger Times Collection Size" );
            foreach ( KeyValuePair<MigrantFleetEscortSize, int> item in escortTriggerTimers )
            {
                Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)item.Key, "Migrant Trigger Times Key" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, item.Value, "Migrant Trigger Times Value" );
            }

            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, escortsTriggered.Count, "Migrant Trigger Count Collection Size" );
            foreach ( KeyValuePair<MigrantFleetEscortSize, short> item in escortsTriggered )
            {
                Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)item.Key, "Migrant Trigger Count Key" );
                Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, item.Value, "Migrant Trigger Count Value" );
            }

            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, KnownMigrantsOutsideOurGalaxy, "Known Migrants Outside Our Galaxy" );

            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, migrantsThatHaveBeenHereBefore.Count, "Migrants That Have Been Here Before Size" );
            foreach ( KeyValuePair<int, bool> item in migrantsThatHaveBeenHereBefore )
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, item.Key, "Migrants That Have Been Here Before Value" );

            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, secondsSinceWormholesAppeared.Count, "Wormhole Apperance Collection Size" );
            foreach ( KeyValuePair<short, int> item in secondsSinceWormholesAppeared )
            {
                Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, item.Key, "Wormhole Apperance Key" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, item.Value, "Wormhole Apperance Value" );
            }

            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, secondsSinceMigrantsSafe.Count, "Migrant Safety Timer Collection Size" );
            foreach ( KeyValuePair<int, int> pair in secondsSinceMigrantsSafe )
            {
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, pair.Key, "Migrant Safety Timer Key" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, pair.Value, "Migrant Safety Timer Value" );
            }

            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, migrantOriginPlanets.Count, "Migrant Origin Planets Collection Size" );
            foreach ( KeyValuePair<int, short> pair in migrantOriginPlanets )
            {
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, pair.Key, "Migrant Origin Planets Key" );
                Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, pair.Value, "Migrant Origin Planets Value" );
            }
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            int count = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "Migrant Trigger Times Collection Size" );
            escortTriggerTimers.Clear();
            for ( int x = 0; x < count; x++ )
                escortTriggerTimers.Add( (MigrantFleetEscortSize)Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "Migrant Trigger Times Key" ), Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "Migrant Trigger Times Value" ) );

            count = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "Migrant Trigger Count Collection Size" );
            escortsTriggered.Clear();
            for ( int x = 0; x < count; x++ )
                escortsTriggered.Add( (MigrantFleetEscortSize)Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "Migrant Trigger Count Key" ), Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "Migrant Trigger Count Value" ) );

            KnownMigrantsOutsideOurGalaxy = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "Known Migrants Outside Our Galaxy" );

            count = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "Migrants That Have Been Here Before Size" );
            migrantsThatHaveBeenHereBefore.Clear();
            for ( int x = 0; x < count; x++ )
                migrantsThatHaveBeenHereBefore[Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "Migrants That Have Been Here Before Value" )] = true;

            count = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "Wormhole Apperance Collection Size" );
            secondsSinceWormholesAppeared.Clear();
            for ( int x = 0; x < count; x++ )
                secondsSinceWormholesAppeared.Add( Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "Wormhole Apperance Key" ), Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "Wormhole Apperance Value" ) );

            count = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "Migrant Safety Timer Collection Size" );
            secondsSinceMigrantsSafe.Clear();
            for ( int x = 0; x < count; x++ )
                secondsSinceMigrantsSafe.AddPair( Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "Migrant Safety Timer Key" ), Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "Migrant Safety Timer Value" ) );

            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 4, 022 ) )
            {
                count = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "Migrant Origin Planets Collection Size" );
                migrantOriginPlanets.Clear();
                for ( int x = 0; x < count; x++ )
                    migrantOriginPlanets.AddPair( Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "Migrant Origin Planets Key" ), Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "Migrant Origin Planets Value" ) );
            }
        }
        #endregion end Serialization and Deserialization

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return Intensity;
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            DoRefreshFromFactionSettings();

            int load = 50 + (Intensity * 8);

            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( load ).Add( " Load From Migrant Fleets" );
            return load;
        }

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
        }
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            ConfigurationForFaction cfg = this.AttachedFaction.Config;
            Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
            if ( Difficulty == null )
                Difficulty = MigrantFleetsDifficultyTable.Instance.GetRowForFaction( this.AttachedFaction );
        }
        #endregion

        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            clanlingCount.ClearConstructionValueForStartingConstruction();
            migrantWormholePlanets.ClearConstructionListForStartingConstruction();
            migrants.ClearConstructionListForStartingConstruction();
            clanlingsWithoutParent.ClearConstructionListForStartingConstruction();
            clanlings.ClearConstructionListForStartingConstruction();

            foreach ( GameEntity_Squad wormhole in this.AttachedFaction.Squads( NeinzulMigrantWormholeTag ) )
            {
                migrantWormholePlanets.AddToConstructionList( wormhole.Planet );
            }

            foreach ( GameEntity_Squad migrant in this.AttachedFaction.Squads( NeinzulMigrantTag ) )
            {
                migrants.AddToConstructionList( migrant );
            }

            foreach ( GameEntity_Squad clanling in this.AttachedFaction.Squads( NeinzulClanlingTag ) )
            {
                if ( World_AIW2.Instance.GetEntityByID_Squad( clanling.MinorFactionStackingID ) == null )
                    clanlingsWithoutParent.AddToConstructionList( clanling );

                clanlings.AddToConstructionList( clanling );

                clanlingCount.Construction += (1 + clanling.ExtraStackedSquadsInThis);

                clanling.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); // Being cheeky.
            }

            clanlingCount.SwitchConstructionToDisplay();
            migrantWormholePlanets.SwitchConstructionToDisplay();
            migrants.SwitchConstructionToDisplay();
            clanlingsWithoutParent.SwitchConstructionToDisplay();
            clanlings.SwitchConstructionToDisplay();

            UpdateAllegiance();
        }
        #endregion

        private void UpdateAllegiance()
        {
            string allegiance = Allegiance;
            switch ( allegiance )
            {
                case "对玩家友好":
                    AllegianceHelper.AllyThisFactionToHumans( AttachedFaction );
                    humanAlly = true;
                    break;
                default:
                    AllegianceHelper.AllyThisFactionToMinorFactionTeam( AttachedFaction, allegiance );
                    humanAlly = false;
                    break;
            }
        }

        public override void DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost( ArcenClientOrHostSimContextCore Context, bool IsFirstCallToFactionOfThisTypeThisCycle )
        {
            if ( !humanAlly )
                return; // Human friends only.

            //do this for the first faction of the type only, regardless of how many there are
            if ( !IsFirstCallToFactionOfThisTypeThisCycle )
                return;

            if ( migrantWormholePlanets.Count < 1 && migrants.Count < 1 && clanlings.Count < 1 )
                return; // No migrants.

            int debugStage = 1;
            try
            {
                NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                debugStage = 10;
                fillData.Faction = AttachedFaction;
                debugStage = 20;
                foreach ( Planet workingPlanet in migrantWormholePlanets.GetDisplayList() )
                {
                    debugStage = 21;
                    foreach ( GameEntity_Squad wormhole in workingPlanet.GetPlanetFactionForFaction( AttachedFaction ).Entities.Squads( NeinzulMigrantWormholeTag ) )
                    {
                        debugStage = 22;
                        fillData.EntityList.AddIfNotAlreadyIn( SafeSquadWrapper.Create( wormhole ) );
                    }
                }
                debugStage = 30;
                foreach ( SafeSquadWrapper migrant in migrants.GetDisplayList() )
                {
                    debugStage = 31;
                    if ( !(migrant.GetSquad()?.Planet.GetControllingOrInfluencingFaction()?.GetIsFriendlyTowards( AttachedFaction ) ?? true) )
                        fillData.EntityList2.AddIfNotAlreadyIn( migrant );
                }
                debugStage = 40;
                List<SafeSquadWrapper> counting = GameEntity_Squad.GetTemporarySquadList( "MigrantFleets-PerSecondNonSimNotificationUpdates-counting", 10f );
                if ( counting == null ) //blocked for teardown/shutdown; bail
                    return;
                debugStage = 50;
                foreach ( SafeSquadWrapper clanling in clanlings.GetDisplayList() )
                {
                    debugStage = 51;
                    if ( clanling.GetSquad() != null )
                        counting.AddIfNotAlreadyIn( clanling );
                }
                debugStage = 60;
                fillData.Int16List.Add( (short)counting.Count );
                debugStage = 70;
                GameEntity_Squad.ReleaseTemporarySquadList( counting );
                debugStage = 80;
                NotificationNonSim notification = new NotificationNonSim();
                debugStage = 90;
                notification.Assign( MigrantFleetsNotifier.Instance, fillData, "", 0, "Migrant Fleets", SortedNotificationPriorityLevel.Minor );
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLog( "Migrant Fleets Notifier Error at debug stage " + debugStage + ":\n" + e, Verbosity.ShowAsError );
            }
        }

        public int SecondsSinceTrigger( MigrantFleetEscortSize type ) => (escortTriggerTimers?.ContainsKey( type ) ?? false) ? World_AIW2.Instance.GameSecond - escortTriggerTimers[type] : -1;
        public int TimesTriggered( MigrantFleetEscortSize type ) => (escortsTriggered?.ContainsKey( type ) ?? false) ? escortsTriggered[type] : 0;

        public void SetMigrantHasBeenHereBefore( GameEntity_Squad escort ) => migrantsThatHaveBeenHereBefore[escort.PrimaryKeyID] = true;
        public bool GetMigrantHasBeenHereBefore( GameEntity_Squad escort ) => migrantsThatHaveBeenHereBefore.ContainsKey( escort.PrimaryKeyID );
        public bool MigrantKnowsWhereToGoNext( GameEntity_Squad migrant ) => GetMigrantHasBeenHereBefore( migrant ) || migrant.PlanetFaction.DataByStance[FactionStance.Friendly].TotalStrength - migrant.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength > 1000;

        #region Trigger
        public void Trigger( MigrantFleetEscortSize type )
        {
            if ( escortTriggerTimers.ContainsKey( type ) )
                escortTriggerTimers[type] = World_AIW2.Instance.GameSecond;
            else
                escortTriggerTimers.Add( type, World_AIW2.Instance.GameSecond );
            if ( escortsTriggered.ContainsKey( type ) )
                escortsTriggered[type]++;
            else
                escortsTriggered.Add( type, 1 );
        }
        #endregion

        #region CanTrigger_Saved
        public bool CanTrigger_Saved( MigrantFleetEscortSize type, MigrantFleetsDifficulty difficulty )
        {
            switch ( type )
            {
                case MigrantFleetEscortSize.Large:
                    return KnownMigrantsOutsideOurGalaxy >= difficulty.MigrantsPerMigration_Large && SecondsSinceTrigger( type ) >= difficulty.SecondsBetweenMigration_Large_Saved;
                case MigrantFleetEscortSize.Medium:
                    return KnownMigrantsOutsideOurGalaxy >= difficulty.MigrantsPerMigration_Medium && SecondsSinceTrigger( type ) >= difficulty.SecondsBetweenMigration_Medium_Saved;
                case MigrantFleetEscortSize.Small:
                    return KnownMigrantsOutsideOurGalaxy >= difficulty.MigrantsPerMigration_Small && SecondsSinceTrigger( type ) >= difficulty.SecondsBetweenMigration_Small_Saved;
                default:
                    return false;
            }
        }
        #endregion

        #region CanTrigger_New
        public bool CanTrigger_New( MigrantFleetEscortSize type, MigrantFleetsDifficulty difficulty )
        {
            switch ( type )
            {
                case MigrantFleetEscortSize.Large:
                    return SecondsSinceTrigger( type ) >= difficulty.SecondsBetweenMigration_Large_New;
                case MigrantFleetEscortSize.Medium:
                    return SecondsSinceTrigger( type ) >= difficulty.SecondsBetweenMigration_Medium_New;
                case MigrantFleetEscortSize.Small:
                    return SecondsSinceTrigger( type ) >= difficulty.SecondsBetweenMigration_Small_New;
                default:
                    return false;
            }
        }
        #endregion

        #region GetOrStartWormholeTimer
        public int GetOrStartWormholeTimer( Planet planet )
        {
            if ( !secondsSinceWormholesAppeared.ContainsKey( planet.Index ) )
                secondsSinceWormholesAppeared.Add( planet.Index, World_AIW2.Instance.GameSecond );
            return World_AIW2.Instance.GameSecond - secondsSinceWormholesAppeared[planet.Index];
        }
        #endregion

        #region GetButDoNotStartWormholeTimer
        public int GetButDoNotStartWormholeTimer( Planet planet )
        {
            if ( !secondsSinceWormholesAppeared.ContainsKey( planet.Index ) )
                return 0;
            return World_AIW2.Instance.GameSecond - secondsSinceWormholesAppeared[planet.Index];
        }
        #endregion

        #region StopWormholeTimer
        public void StopWormholeTimer( Planet planet )
        {
            if ( secondsSinceWormholesAppeared.ContainsKey( planet.Index ) )
                secondsSinceWormholesAppeared.Remove( planet.Index );
        }
        #endregion

        #region GetOrStartMigrantSafetyTimer
        public int GetOrStartMigrantSafetyTimer( GameEntity_Squad migrant )
        {
            if ( !secondsSinceMigrantsSafe.ContainsKey( migrant.PrimaryKeyID ) )
                secondsSinceMigrantsSafe.AddPair( migrant.PrimaryKeyID, World_AIW2.Instance.GameSecond );
            if ( humanAlly )
                if ( GetMigrantHasBeenHereBefore( migrant ) )
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( Journal_MigrantEscorted_Known, string.Empty, AttachedFaction, null, migrant.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                else
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( Journal_MigrantEscorted_Unknown, string.Empty, AttachedFaction, null, migrant.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            return World_AIW2.Instance.GameSecond - secondsSinceMigrantsSafe[migrant.PrimaryKeyID];
        }
        #endregion

        #region GetButDoNotStartMigrantSafetyTimer
        public int GetButDoNotStartMigrantSafetyTimer( GameEntity_Squad migrant )
        {
            if ( !secondsSinceMigrantsSafe.ContainsKey( migrant.PrimaryKeyID ) )
                return 0;
            return World_AIW2.Instance.GameSecond - secondsSinceMigrantsSafe[migrant.PrimaryKeyID];
        }
        #endregion

        #region StopMigrantSafetyTimer
        public void StopMigrantSafetyTimer( GameEntity_Squad migrant )
        {
            if ( secondsSinceMigrantsSafe.ContainsKey( migrant.PrimaryKeyID ) )
                secondsSinceMigrantsSafe.Remove( migrant.PrimaryKeyID );
        }
        #endregion

        #region PurgeOldEntries
        public void PurgeOldEntries()
        {
            List<int> tempList = Mat.GetTemporaryIntList( "Migrants-PurgeOldEntries", 10f );
            if ( tempList == null ) //blocked for teardown/shutdown; bail
                return;
            migrantsThatHaveBeenHereBefore.RemoveWhere( tempList, kv =>
            {
                return World_AIW2.Instance.GetEntityByID_Squad( kv.Key ) == null;
            } );
            migrantOriginPlanets.RemoveWhere( tempList, kv =>
            {
                return World_AIW2.Instance.GetEntityByID_Squad( kv.Key ) == null;
            } );
            Mat.ReleaseTemporaryIntList( tempList );

            foreach ( KeyValuePair<int, int> pair in secondsSinceMigrantsSafe )
            {
                if ( World_AIW2.Instance.GetEntityByID_Squad( pair.Key ) == null )
                    continue;
            }
        }
        #endregion

        #region UpdatePowerLevel
        public override void UpdatePowerLevel()
        {
            FInt power = FInt.FromParts( 0, 001 ) * clanlingCount.Display * (1 + (Difficulty.GetMarkLevel( this ) / 2));

            if ( power > 1 )
                power = FInt.FromParts( 1, 000 );

            this.AttachedFaction.OverallPowerLevel = power;
        }
        #endregion

        #region GetFriendlyTerritory
        public void GetFriendlyTerritory( List<Planet> ListToFill )
        {
            foreach ( Planet workingPlanet in World_AIW2.Instance.Planets( false ) )
            {
                // If a friendly faction owns the planet, or we're not a human ally and friendly forces have strength here, consider it territory.
                if ( workingPlanet.GetControllingOrInfluencingFaction().GetIsFriendlyTowards( AttachedFaction ) || (!humanAlly &&
                    workingPlanet.GetPlanetFactionForFaction( AttachedFaction ).DataByStance[FactionStance.Friendly].TotalStrength >= 5000) )
                    ListToFill.Add( workingPlanet );
            }
        }
        #endregion

        #region GetNextPlanetToMoveToToReachFriendlies
        public ConcurrentDictionary<Planet, Planet> cachedMovementTargets = ConcurrentDictionary<Planet, Planet>.Create_WillNeverBeGCed( Engine_Universal.DEFAULT_CONCURRENCY_LEVEL, 900, "MigrantFleetsFactionBaseInfo-cachedMovementTargets" );
        public Planet GetNextPlanetToMoveToToReachFriendlies( GameEntity_Squad migrant, List<Planet> friendlyTerritory )
        {
            // If we can, use our cache.
            if ( cachedMovementTargets.TryGetValue( migrant.Planet, out Planet result ) )
                return result;

            Planet nearest = friendlyTerritory[0];
            friendlyTerritory.ForEach( workingPlanet =>
            {
                int nearestHops = migrant.Planet.GetHopsTo( nearest ), workingHops = migrant.Planet.GetHopsTo( workingPlanet );
                if ( workingHops < nearestHops || (workingHops == nearestHops && workingPlanet.Index < nearest.Index) )
                    nearest = workingPlanet;
            } );

            Planet next = null;
            foreach ( Planet workingPlanet in migrant.Planet.LinkedNeighbors( false ) )
            {
                if ( next == null )
                    next = workingPlanet;

                int workingHops = workingPlanet.GetHopsTo( nearest ), nextHops = next.GetHopsTo( nearest );

                if ( workingHops < nextHops || (workingHops == nextHops && workingPlanet.Index < next.Index) )
                    next = workingPlanet;
            }

            // Save to cache.
            cachedMovementTargets.TryAdd( migrant.Planet, next );

            return next;
        }
        #endregion

        #region MigrantSafeTimeLeftUntilWarpingOutOfGalaxy
        public int MigrantSafeTimeLeftUntilWarpingOutOfGalaxy( GameEntity_Squad migrant, int secondsSafe ) => (Migrant_SafetyTimer + migrant.PrimaryKeyID % Migrant_SafetyTimer) - secondsSafe;
        #endregion

        #region MigrantSafeLongEnoughToWarpOutOfGalaxy
        // We'll use the unit's PrimaryKey as a pseudo random value to make it feel like Neinzul are staying around at random instead of being uniform.
        public bool MigrantSafeLongEnoughToWarpOutOfGalaxy( GameEntity_Squad migrant, int secondsSafe ) => secondsSafe >= (Migrant_SafetyTimer + migrant.PrimaryKeyID % Migrant_SafetyTimer);
        #endregion

        #region MigrantOriginPlanet
        public Planet GetMigrantOriginPlanetOrNull( GameEntity_Squad migrant )
        {
            if ( !migrantOriginPlanets.ContainsKey( migrant.PrimaryKeyID ) )
                return null;
            return World_AIW2.Instance.GetPlanetByIndex( migrantOriginPlanets[migrant.PrimaryKeyID] );
        }
        public void SetMigrantOriginPlanet( GameEntity_Squad migrant, Planet planet ) => migrantOriginPlanets.Add( migrant.PrimaryKeyID, planet.Index );
        #endregion

        #region MigrantShouldStayOnHostilePlanet
        public int MigrantShouldStayOnHostilePlanetForThisMuchLonger( GameEntity_Squad migrant ) => (120 + migrant.PrimaryKeyID % 60) - migrant.GetSecondsSinceEnteringThisPlanet();
        public bool MigrantShouldStayOnHostilePlanet( GameEntity_Squad migrant ) => migrant.GetSecondsSinceEnteringThisPlanet() < (120 + migrant.PrimaryKeyID % 60);
        #endregion
    }
}
