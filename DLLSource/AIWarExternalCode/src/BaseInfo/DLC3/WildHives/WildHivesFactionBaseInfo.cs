
using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public class WildHivesFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        public int Intensity;
        public WildHivesDifficulty Difficulty { get; private set; }
        public static WildHivesDifficulty GetHighestDifficulty()
        {
            int highestIntensity = 1;
            AllWildHiveFactions.ForEach( workingFaction =>
            {
                if ( workingFaction.Intensity > highestIntensity )
                {
                    highestIntensity = workingFaction.Intensity;
                }
            } );
            return WildHivesDifficultyTable.Instance.GetRowForIntensity( highestIntensity );
        }

        public Dictionary<Planet, int> lastAttacked = Dictionary<Planet, int>.Create_WillNeverBeGCed( 100, "WildHivesFactionBaseInfo-lastAttacked", 5 );
        public Dictionary<Planet, int> lastContested = Dictionary<Planet, int>.Create_WillNeverBeGCed( 100, "WildHivesFactionBaseInfo-lastContested", 5 );

        public void AttackedOn( Planet planet )
        {
            if ( lastAttacked.ContainsKey( planet ) )
                lastAttacked[planet] = World_AIW2.Instance.GameSecond;
            else
                lastAttacked.Add( planet, World_AIW2.Instance.GameSecond );
        }
        public int SecondsSinceLastAttackedOn( Planet planet ) => lastAttacked.GetHasKey( planet ) ? World_AIW2.Instance.GameSecond - lastAttacked[planet] : 9999;

        public void ContestedOn( Planet planet )
        {
            if ( lastContested.ContainsKey( planet ) )
                lastContested[planet] = World_AIW2.Instance.GameSecond;
            else
                lastContested.Add( planet, World_AIW2.Instance.GameSecond );
        }
        public int SecondsSinceLastContestedOn( Planet planet ) => lastContested.GetHasKey( planet ) ? World_AIW2.Instance.GameSecond - lastContested[planet] : 9999;

        public readonly AntiMinorFactionWaveData WaveData = new AntiMinorFactionWaveData();

        public WildHivesFactionBaseInfo() => Cleanup();

        protected override void Cleanup()
        {
            Intensity = 0;

            lastAttacked.Clear();
            lastContested.Clear();

            AllWildHiveFactions.Clear();

            workers.Clear();
            soldiers.Clear();
            hives.Clear();

            attackedPlanets.Clear();
            contestedPlanets.Clear();
            influencedPlanets.Clear();
            planetsToBeCheckedForAttackedOnLogic.Clear();

            WaveData.Cleanup();
        }

        #region (De)Serialization
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, lastAttacked.Count, "Last Attacked Pair Count" );
            foreach ( KeyValuePair<Planet, int> pair in lastAttacked )
            {
                Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, pair.Key.Index, "Last Attacked Planet Key" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, pair.Value, "Last Attacked Value" );
            }

            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, lastContested.Count, "Last Contested Pair Count" );
            foreach ( KeyValuePair<Planet, int> pair in lastContested )
            {
                Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, pair.Key.Index, "Last Contested Planet Key" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, pair.Value, "Last Contested Value" );
            }

            WaveData.SerializeTo( MetaData, Buffer, SerializationCmdType );
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            int count = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "Last Attacked Pair Count" );
            lastAttacked.Clear();
            for ( int x = 0; x < count; x++ )
                lastAttacked.Add( World_AIW2.Instance.GetPlanetByIndex( Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "Last Attacked Planet Key" ) ), Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "Last Attacked Value" ) );

            count = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "Last Contested Pair Count" );
            lastContested.Clear();
            for ( int x = 0; x < count; x++ )
                lastContested.Add( World_AIW2.Instance.GetPlanetByIndex( Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "Last Contested Planet Key" ) ), Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "Last Contested Value" ) );

            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 524 ) )
                WaveData.DeserializeIntoSelf( MetaData, Buffer, SerializationCmdType );
            else
                WaveData.Cleanup();
        }
        #endregion

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return Intensity;
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( "100 Load From Wild Hives" );
            return 100;
        }

        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            if ( !AllWildHiveFactions.Contains( this ) )
                AllWildHiveFactions.Add( this );
        }

        protected override void DoRefreshFromFactionSettings()
        {
            var config = this.AttachedFaction.Config;
            Intensity = config.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
            if ( Difficulty == null )
                Difficulty = WildHivesDifficultyTable.Instance.GetRowForFaction( AttachedFaction );
        }

        public override bool GetShouldAttackNormallyExcludedTarget( GameEntity_Squad Target )
        {
            if ( Target.Planet.GetControllingFactionType() == FactionType.Player )
                return false; // Pacified on player planets.
            if ( Target.TypeData.GetHasTag( WorkerTag ) || Target.TypeData.GetHasTag( NeinzulHiveTag ) )
                return true; // Hostile to others.
            if ( Target.TypeData.SpecialType == SpecialEntityType.AICommandStationOriginal || Target.TypeData.SpecialType == SpecialEntityType.AICommandStationReconquest || Target.TypeData.GrantsMinorFactionPlanetControl && contestedPlanets.DisplayContains( Target.Planet ) )
                return true; // Berserk on contested planets.
            return false;
        }

        #region Constants
        private static readonly string JournalBase = "NA_WildHives_";
        public static readonly string JournalDiscovered = JournalBase + "WildHiveDiscovered";
        public static readonly string JournalAttacked = JournalBase + "WildHiveAttacked";
        public static readonly string JournalFriendly = JournalBase + "WildHiveFriendly";
        public static readonly string JournalFriendlyMultiHive = JournalBase + "WildHiveFriendlyMultiHive";
        public static readonly string JournalFriendlyDefense = JournalBase + "WildHiveFriendlyDefense";
        public static readonly string JournalNonMetalGenerator = JournalBase + "WildHiveNonMetalGenerator";
        public static readonly string JournalContested = JournalBase + "WildHiveContested";
        public static readonly string JournalFriendlyBond = JournalBase + "WildHiveFriendlyBond";
        public static readonly string JournalWitheringWorker = JournalBase + "WildHiveWitheringWorker";

        public static readonly string WorkerTag = "NeinzulWildWorker";
        public static readonly string SoldierTag = "NeinzulClanling";

        public static readonly string WorkerName_Basic = "NeinzulWildHiveWorker";
        public static readonly string WorkerName_Agitated = "NeinzulWildHiveWorkerAgitated";

        public static readonly string MetalGeneratorTag = "MetalGenerator";
        public static readonly string NeinzulHiveTag = "WildHive";

        public static readonly string NeinzulHiveName_Basic = "NeinzulWildHiveBasic";
        public static readonly string NeinzulHiveName_Advanced = "NeinzulWildHiveAdvanced";

        public static readonly string NeinzulHiveName_Basic_Agitated = "NeinzulWildHiveBasicAgitated";
        public static readonly string NeinzulHiveName_Advanced_Agitated = "NeinzulWildHiveAdvancedAgitated";
        #endregion

        public static List<WildHivesFactionBaseInfo> AllWildHiveFactions = List<WildHivesFactionBaseInfo>.Create_WillNeverBeGCed( 10, "WildHivesFactionBaseInfo-AllWildHiveFactions" );

        public readonly DoubleBufferedList<SafeSquadWrapper> workers = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "WildHives-workers" );
        public readonly DoubleBufferedList<SafeSquadWrapper> soldiers = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "WildHives-soldiers" );
        public readonly DoubleBufferedList<SafeSquadWrapper> hives = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "WildHives-hives" );

        public readonly DoubleBufferedList<Planet> attackedPlanets = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 120, "WildHives-attackedPlanets" );
        public readonly DoubleBufferedList<Planet> contestedPlanets = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 120, "WildHives-contestedPlanets" );
        public readonly DoubleBufferedList<Planet> influencedPlanets = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 200, "WildHivesFactionBaseInfo-influencedPlanets" );

        // Learning note: As per Chris's explanation, we can gain some performance on anything that has a higher than normal amount of Contains calls.
        // In regular cSharp, you would use something like a HashMap, which only accepts unique keys, and is very fast at this.
        // Due to how rarely we'd use this however, we are instead suggested to use a Dictionary here, as it contains the same unique key property that speeds up Contains calls.
        // The overhead of being a dictionary with a single bool value for each entry is far outweighed by the speed improvements that can be noted once you get a late game hive infestation going.
        private readonly Dictionary<Planet, bool> planetsToBeCheckedForAttackedOnLogic = Dictionary<Planet, bool>.Create_WillNeverBeGCed( 200, "WildHivesFactionBaseInfo-attackedThisSecond" );

        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( Core.ArcenClientOrHostSimContextCore Context )
        {
            workers.ClearConstructionListForStartingConstruction();
            soldiers.ClearConstructionListForStartingConstruction();
            hives.ClearConstructionListForStartingConstruction();
            influencedPlanets.ClearConstructionListForStartingConstruction();
            planetsToBeCheckedForAttackedOnLogic.Clear();

            foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
            {
                if ( entity.TypeData.AlwaysSelfAttritions )
                    continue; // Skip other Younglings; no need for a tag check.
                else if ( entity.TypeData.GetHasTag( SoldierTag ) )
                    soldiers.AddToConstructionList( entity ); // If a soldier, only add to list and ignore lower logic.
                else
                {
                    if ( entity.TypeData.GetHasTag( WorkerTag ) )
                        workers.AddToConstructionList( entity );
                    else if ( entity.TypeData.GetHasTag( NeinzulHiveTag ) )
                    {
                        hives.AddToConstructionList( entity );

                        if ( ArcenNetworkAuthority.GetIsHostMode() && entity.Planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                            switch ( entity.TypeData.InternalName )
                            {
                                case "NeinzulWildHiveBasic":
                                case "NeinzulWildHiveBasicAgitated":
                                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( JournalDiscovered, string.Empty, AttachedFaction, null, entity.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                                    break;
                                case "NeinzulWildHiveAdvanced":
                                case "NeinzulWildHiveAdvancedAgitated":
                                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( JournalNonMetalGenerator, string.Empty, AttachedFaction, null, entity.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                                    break;
                                default:
                                    break;
                            }
                    }

                    if ( !planetsToBeCheckedForAttackedOnLogic.ContainsKey( entity.Planet ) )
                        if ( IsBeingAttacked( entity ) )
                            planetsToBeCheckedForAttackedOnLogic.Add( entity.Planet, true );
                        else if ( entity.Planet.GetControllingFactionType() == FactionType.Player )
                            WildHivesFriendlyFactionBaseInfo.Instance.hivesOnFriendlyPlanets.AddToConstructionList( entity );

                    // We exist here.
                    influencedPlanets.AddToConstructionListIfNotAlreadyIn( entity.Planet );
                }
            }

            // Update attack timers for those attacked this second, and alert the player if applicable.
            foreach ( KeyValuePair<Planet, bool> pair in planetsToBeCheckedForAttackedOnLogic )
            {
                Planet workingPlanet = pair.Key;
                AttackedOn( workingPlanet );

                if ( ArcenNetworkAuthority.GetIsHostMode() && workingPlanet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( JournalAttacked, string.Empty, AttachedFaction, null, workingPlanet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }

            // Add any of our currently attacked planets that aren't already in.
            // Don't add them above, to avoid resetting their timer.
            foreach ( Planet workingPlanet in attackedPlanets.GetDisplayList() )
            {
                planetsToBeCheckedForAttackedOnLogic.Add( workingPlanet, true );
            }

            // Fill our attackedPlanets list.
            attackedPlanets.ClearConstructionListForStartingConstruction();
            foreach ( KeyValuePair<Planet, bool> pair in planetsToBeCheckedForAttackedOnLogic )
            {
                Planet workingPlanet = pair.Key;

                if ( SecondsSinceLastAttackedOn( workingPlanet ) < 60 )
                    attackedPlanets.AddToConstructionList( workingPlanet );
            }

            workers.SwitchConstructionToDisplay();
            soldiers.SwitchConstructionToDisplay();
            hives.SwitchConstructionToDisplay();
            influencedPlanets.SwitchConstructionToDisplay();
            attackedPlanets.SwitchConstructionToDisplay();
        }

        public override void DoPerSecondLogic_Stage2APostAllFactionAggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            // Now that all of our Wild Hives have their territory setup, we can do some additional logic.
            // First off, if any other Wild Hive is influencing any planet we're also influencing, we'll be agitated.
            // Second off, if the AI has come knocking on a planet we consider ours, we should also get agitated.

            contestedPlanets.ClearConstructionListForStartingConstruction();

            foreach ( Planet planet in influencedPlanets.GetDisplayList() )
            {
                bool IsContested = false;

                if ( planet.GetControllingOrInfluencingFaction() == AttachedFaction )
                {
                    // This is our planet, respond to AI threat.
                    int totalAiThreat = 0;
                    int ourStrength = planet.GetDataByStanceForFaction( AttachedFaction, FactionStance.Self ).TotalStrength;
                    foreach ( Faction workingFaction in World_AIW2.Instance.Factions )
                    {
                        if ( IsContested )
                            break; // Only need to be mad once.

                        if ( workingFaction.Type != FactionType.AI )
                            continue; // Only check AI factions, we don't mind hanging out with other special factions. For now.

                        totalAiThreat += planet.GetDataByStanceForFaction( workingFaction, FactionStance.Self ).TotalStrength;

                        if ( totalAiThreat >= ourStrength )
                            IsContested = true;
                    }
                }

                // Respond to any other Hive threats, if they exist.
                for ( int factionI = 0; factionI < AllWildHiveFactions.Count && !IsContested; factionI++ )
                {
                    WildHivesFactionBaseInfo other = AllWildHiveFactions[factionI];
                    if ( this == other )
                        continue; // Skip self.

                    if ( other.influencedPlanets.DisplayContains( planet ) )
                        IsContested = true; // No hostile bugs allowed.
                }

                if ( IsContested )
                    ContestedOn( planet ); // Get angry immediately.

                if ( SecondsSinceLastContestedOn( planet ) < 60 )
                    contestedPlanets.AddToConstructionList( planet ); // Stay angry for a minute.
            }

            contestedPlanets.SwitchConstructionToDisplay();
        }

        #region Stage3
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            if ( workers.GetDisplayList().Count == 0 && hives.GetDisplayList().Count == 0 )
                return;

            DecayUnNeededSoldiers();
            RegenerateOrDecayHivesAndWorkers();
        }

        public void DecayUnNeededSoldiers()
        {
            foreach ( SafeSquadWrapper soldier in soldiers.GetDisplayList() )
            {
                GameEntity_Squad soldierSquad = soldier.GetSquad();
                if ( soldierSquad == null )
                    continue;
                GameEntity_Squad leader = World_AIW2.Instance.GetEntityByID_Squad( soldierSquad.MinorFactionStackingID );
                if ( leader == null || !attackedPlanets.DisplayContains( leader.Planet ) )
                {
                    soldierSquad.TakeHullRepair( -(soldierSquad.GetMaxHullPoints() / 30) );
                    soldierSquad.RepairImpossibleForSeconds += 2;
                }
            }
        }

        // If not contested, have hives regenerate. End wars fast.
        public void RegenerateOrDecayHivesAndWorkers()
        {
            foreach ( SafeSquadWrapper hive in hives.GetDisplayList() )
            {
                GameEntity_Squad hiveSquad = hive.GetSquad();
                if ( hiveSquad == null )
                    continue;
                if ( !contestedPlanets.DisplayContains( hive.Planet ) && hiveSquad.RepairImpossibleForSeconds <= 0 && hiveSquad.GetCurrentHullPoints() < hiveSquad.GetMaxHullPoints() )
                    hiveSquad.TakeHullRepair( hiveSquad.GetMaxHullPoints() / 30 );
            }
            foreach ( SafeSquadWrapper workerWrap in workers.GetDisplayList() )
            {
                GameEntity_Squad worker = workerWrap.GetSquad();
                if ( worker == null )
                    continue;
                if ( !contestedPlanets.DisplayContains( worker.Planet ) && worker.RepairImpossibleForSeconds <= 0 && worker.GetCurrentHullPoints() < worker.GetMaxHullPoints() )
                    worker.TakeHullRepair( worker.GetMaxHullPoints() / 30 );

                // If we've been stuck on a planet for a very long time, start taking damage.
                // This will slowly kill us, but also make us, and our fellow workers/hives, deploy soldiers for revenge.
                if ( worker.GetSecondsSinceEnteringThisPlanet() > 1200 )
                {
                    if ( worker.Planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( JournalWitheringWorker, string.Empty, AttachedFaction, worker.TypeData, worker.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );

                    worker.TakeHullRepair( -(worker.GetMaxHullPoints() / 1000) );
                }
                // Regen if we're on a non contested planet and we're able to be repaired.
                else if ( !contestedPlanets.DisplayContains( worker.Planet ) && worker.RepairImpossibleForSeconds <= 0 && worker.GetCurrentHullPoints() < worker.GetMaxHullPoints() )
                    worker.TakeHullRepair( worker.GetMaxHullPoints() / 30 );
            }
        }
        #endregion

        public override void DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost( ArcenClientOrHostSimContextCore Context, bool IsFirstCallToFactionOfThisTypeThisCycle )
        {
            //do this for the first faction of the type only, regardless of how many there are
            if ( !IsFirstCallToFactionOfThisTypeThisCycle )
                return;

            if ( AllWildHiveFactions.Count < 1 )
                return; // Wait if not entirely loaded.

            if ( !AttachedFaction.HasBeenSeenByPlayer )
                return; // Wait until seen.

            if ( hives.Count < 1 )
                return; // Don't display if no infection.

            int debugStage = 1;
            try
            {
                debugStage = 10;
                NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                // Get a count of infested, and total, generators in both Player and Galaxy space.
                short playerInfested = 0, playerTotal = 0, galaxyInfested = 0, galaxyTotal = 0;
                foreach ( GameEntity_Squad workingGenerator in World_AIW2.Instance.Squads( "MetalGenerator" ) )
                {
                    galaxyTotal++;

                    if ( workingGenerator.TypeData.GetHasTag( NeinzulHiveTag ) )
                        galaxyInfested++;

                    if ( workingGenerator.Planet.GetControllingFactionType() == FactionType.Player )
                    {
                        playerTotal++;
                        if ( workingGenerator.TypeData.GetHasTag( NeinzulHiveTag ) )
                            playerInfested++;
                    }
                }

                debugStage = 20;
                fillData.Int16List.Add( playerInfested );
                fillData.Int16List.Add( playerTotal );
                fillData.Int16List.Add( galaxyInfested );
                fillData.Int16List.Add( galaxyTotal );
                fillData.Int16List.Add( (short)WildHivesFriendlyFactionBaseInfo.Instance.soldiers.Count );
                fillData.Faction = AttachedFaction;

                debugStage = 30;
                NotificationNonSim notification = new NotificationNonSim();
                debugStage = 40;
                notification.Assign( WildHivesNotifier.Instance, fillData, "", 0, "Wild Hives", SortedNotificationPriorityLevel.Informational );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Wild Hives Notifier Error at debug stage " + debugStage + ":\n" + e, Verbosity.ShowAsError );
            }
        }

        private bool IsBeingAttacked( GameEntity_Squad entity ) => entity.GetCurrentShieldPoints() < entity.GetMaxShieldPoints() * 0.9 || entity.GetCurrentHullPoints() < entity.GetMaxHullPoints() * 0.9;

        public static bool HasStoredSoldiers( GameEntity_Squad entity, WildHivesFactionBaseInfo baseInfo, WildHivesPerUnitBaseInfo entityInfo, out int stored )
        {
            stored = 0;
            while ( entityInfo.SoldierBuildPoints > baseInfo.Difficulty.soldierCostBase + (baseInfo.Difficulty.soldierCostIncreasePerAlreadyBuilt * stored) )
                stored++;
            stored = Math.Min( stored, baseInfo.Difficulty.GetMaxSoldiersToSpawn( entity ) );
            return stored > 0;
        }
        public static int GetAverageStrengthOfSpawntSoldiers( int soldiersToSpawn, byte markLevel )
        {
            int lowestStrength = -1, highestStrength = -1;
            GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( WildHivesFactionBaseInfo.SoldierTag ).ForEach( soldierType =>
            {
                int strength = soldierType.GetForMark( markLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership;
                if ( lowestStrength == -1 || strength < lowestStrength )
                    lowestStrength = strength;

                if ( strength > highestStrength )
                    highestStrength = strength;
            } );
            return ((lowestStrength + highestStrength) / 2) * soldiersToSpawn;
        }
    }
}
